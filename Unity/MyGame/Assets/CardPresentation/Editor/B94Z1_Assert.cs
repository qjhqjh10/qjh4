// B94Z1_Assert.cs — 执行代理 `B94_Z1`（第九十四会话 · 波 1.5）的断言
//
// ============================ 它盯的是什么 ============================
//   活 **1**：`Shell/RewardsWindow.RefreshBadges()` **删掉直写 quad 材质色**那条遗留通道
//            （改为「取组件 → `Recount()` → 批处理 `SnapToTargetForTest()`」）。
//            · 旧写法两个后果：① `idx==0 && RewardsHasBadge` 用一条布尔点着 Missions 那颗（绕开通知系统 +
//              组件补间一停就不再写 ⇒ **那个 1 永久留屏** = 用户看到的「页签红点常亮」）；
//              ② 同一个循环把 idx 1..3 **强写 0** ⇒ 压灭 Campaign / Forge 的真红点。
//   活 **2**：`Shell/SocialWindow.RefreshBadges()` 同病同形（今天无源 ⇒ 看不出；有源那天会压灭真红点）。
//   活 **3**：`Editor/RewardsScene.cs` 那条「第 2 键（Campaign）红点 alpha = 0（**没有通知源**）」的**前提已过期**
//            （2026-10-25 起 `CampaignNotificationProvider` 已登记）⇒ 改成「从数据侧独立算期望」。
//   活 **4**：`DailyData.CanCollectLogin()` 是否太宽 —— 回源结论 = **不改**（见 `报告/B94_Z1.md` §4）。
//            本文件只锁「登录卡现在是单奖励模型」这一条**事实**，用它解释 Missions 红点为什么每天都亮
//            （= 原版行为，不是缺陷）。
//   活 **5**（🆕 **2026-10-31（B106_W37）**，§⑥a~⑥t）：`CampaignBadge` / `ForgeBadge` 按原版 : `RewardsBadge`
//            —— `Validate` 两支（不同类型只比 `parentId` / 同类型走 record 相等性含 `army`）
//            ＋「战役/锻造有可领 ⇒ 导航 REWARDS 那颗也亮」。判据/取舍 → `报告/B106_W37.md`。
// 逐条判据 / 取舍 → `资料/完全复刻/报告/B94_Z1.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么用（宿主）============================
//   `B94Z1_Assert.Run();`
//   + 宿主里两行（照 `B94T1_Assert` 的先例）：
//     `_sink.Pass += B94Z1_Assert.Pass; _sink.Fail += B94Z1_Assert.Fail;`
//   **建议宿主 = `Editor/RewardsScene.cs`**（`win` 就是 `RewardsWindow`，活 3 的宿主也在那儿）。
//   ⚠️ 本组**不建整窗**（活 1/2 是源码结构断言 + 组件级两态）、不建持久资产；临时 GameObject 收尾 `DestroyImmediate`。
//   ⚠️ 会临时改 `DailyData` 四类源的状态 ⇒ `finally` 里一律 `DailyData.ResetAllStatesForTest()` 还原。
//
// ============================ 🔴 灭自证 ============================
//   每条「正向」都配一条**结构上不可能同时满足**的反向（见断言文案里的「🧨 改坏法」）。
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B94_Z1`：红点遗留写手（活 1/2）＋ Campaign 断言的真判据（活 3）＋ 登录卡口径（活 4，记录用）。</summary>
public static class B94Z1_Assert
{
    static readonly CheckSink _sink = new CheckSink("[Z1] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B94_Z1 · 活 1/2：红点的**唯一写手**是 `UiBadgeNotification`（遗留直写通道已删）");
        SectionRewards();
        SectionSocial();
        MenuCheck.Section(_sink, "B94_Z1 · 活 1 行为：`Recount()` + `SnapToTargetForTest()` 按**计重**定 alpha（两态）");
        SectionLedgerBehavior();
        MenuCheck.Section(_sink, "B94_Z1 · 活 3：Campaign 那颗的**真判据**（provider → 定义表 → 过滤）");
        SectionCampaignJudge();
        MenuCheck.Section(_sink, "B94_Z1 · 活 5（B106_W37）：`CampaignBadge` / `ForgeBadge` : `RewardsBadge` ⇒ 导航 REWARDS 也数它们");
        SectionRewardsDerived();
        MenuCheck.Section(_sink, "B94_Z1 · 活 4：登录卡口径（记录；回源结论 = 不改）");
        SectionLogin();
        MenuCheck.Section(_sink, "B94_Z1 · 活 6（B106_W54/W60）：战役红点回退 Ultramarines + 点数闸");
        SectionW54();
    }

    // ------------------------------------------------------------ 活 1：RewardsWindow
    static void SectionRewards()
    {
        string src = ReadShellSource("RewardsWindow.cs");
        Tr(src != null, "①a 前提：读得到 `Shell/RewardsWindow.cs` 源码（读不到 ⇒ 下面四条无意义）");
        if (src == null) return;

        string body = SliceRefreshBadges(src);
        Tr(body != null, "①b 前提：在源码里定位到 `public void RefreshBadges()` 的方法体（⛔ 定位不到就不用谈下面几条）");
        if (body == null) return;

        Tr(body.IndexOf("SetTint(", StringComparison.Ordinal) < 0,
           "①c `RewardsWindow.RefreshBadges()` **不再直写 quad 材质色**（方法体里没有 `SetTint(`）"
         + "。🧨 改坏法：把那条遗留写手加回去 ⇒ 立刻红 —— 它绕开组件、补间一停就不再写 ⇒ **写进去的 alpha 永久留屏**"
         + "（= 用户看到的「页签红点常亮」）");
        Tr(body.IndexOf("DailyData.RewardsHasBadge", StringComparison.Ordinal) < 0,
           "①d 那条**遗留布尔判据**（`idx == 0 && DailyData.RewardsHasBadge`）已从本方法体消失"
         + "（它把 Missions 那颗由一条布尔点着 ⇒ 计数与显示两个来源）");
        Tr(body.IndexOf("GetComponent<UiBadgeNotification>()", StringComparison.Ordinal) >= 0,
           "①e …改为**取组件**（`GetComponent<UiBadgeNotification>()`）—— 红点显隐的唯一写手");
        Tr(body.IndexOf("Recount()", StringComparison.Ordinal) >= 0
           && body.IndexOf("SnapToTargetForTest()", StringComparison.Ordinal) >= 0,
           "①f …并调 `Recount()`（按计重重算）+ 批处理 `SnapToTargetForTest()`（没有帧循环 ⇒ 直接贴目标）"
         + "。🧨 改坏法：只 `Recount()` 不贴值 ⇒ 批处理里 alpha 停在旧值 ⇒ 宿主那两条两态断言红");
        Tr(src.IndexOf("SetTint(", StringComparison.Ordinal) < 0,
           "①g 整份文件都不再直写 tint（⛔ 别在别处又开第二条通道 —— 同一条规则两处写 = 迟早不一致）");
    }

    // ------------------------------------------------------------ 活 2：SocialWindow
    static void SectionSocial()
    {
        string src = ReadShellSource("SocialWindow.cs");
        Tr(src != null, "②a 前提：读得到 `Shell/SocialWindow.cs` 源码");
        if (src == null) return;
        string body = SliceRefreshBadges(src);
        Tr(body != null, "②b 前提：定位到 `SocialWindow.RefreshBadges()` 的方法体");
        if (body == null) return;

        Tr(body.IndexOf("SetTint(", StringComparison.Ordinal) < 0,
           "②c `SocialWindow.RefreshBadges()` **不再无条件直写 0**"
         + "。🧨 改坏法：加回去 ⇒ 今天看不出（两个 provider 都没源），但**好友/联盟有源那天**它会压灭真红点");
        Tr(body.IndexOf("GetComponent<UiBadgeNotification>()", StringComparison.Ordinal) >= 0
           && body.IndexOf("Recount()", StringComparison.Ordinal) >= 0,
           "②d …改为与奖励窗**逐字同形**（取组件 → `Recount()`）—— 同一条规则只写一处形状");
    }

    // ------------------------------------------------------------ 活 1 行为：按计重定 alpha（两态）
    static void SectionLedgerBehavior()
    {
        // 组件级两态：走**真** `UiBadgeNotificationManager` + 真 `RewardsNotificationProvider`，
        // 但 SetAlpha 换成捕获口（不建 quad，也就不依赖美术资源）。
        BadgeWiring.RegisterDefaults();        // 幂等（`ProviderCount > 0` 直接返回）
        var defs = BadgeWiring.DefinitionsForTab("RewardsMenu_MissionsButton");
        Tr(defs != null && defs.Count == 1,
           "③a 前提：Missions 页签红点有 1 条定义（`MissionsBadge`）—— 没有定义下面两条空转绿");

        var go = new GameObject("B94Z1_LedgerProbe");
        var comp = go.AddComponent<UiBadgeNotification>();
        try
        {
            Tr(comp != null, "③b 前提：`UiBadgeNotification` 挂得上去");
            if (comp == null || defs == null || defs.Count == 0) return;

            comp.notificationDefinitions = defs;
            comp.showCounter = true;
            float wrote = float.NaN;
            comp.SetAlpha = delegate (float a) { wrote = a; };

            // 第一态：造出「可领」⇒ 计数 > 0 ⇒ alpha 应为 1
            DailyData.ForceCollectableForTest();
            comp.Recount();
            comp.SnapToTargetForTest();
            Tr(comp.CurrentCount > 0,
               "③c 前提：造出「可领」之后组件算得出计数（= " + comp.CurrentCount + "）");
            C(wrote, 1f,
              "③d 计数 > 0 ⇒ `Recount()+SnapToTargetForTest()` 把 alpha 写成 **1**（原版 `Show()`）"
            + "。🧨 改坏法：实现成「无条件写 0」（= 被删掉的那条遗留通道）⇒ 这里必红");

            // 第二态：四类源全不可领 ⇒ 计数 0 ⇒ alpha 0（该灭就灭）
            DailyData.ResetAllStatesForTest();
            DailyData.ForceLoginStateForTest(DailyData.State.Claimed);
            DailyData.ForceSkullsStateForTest(DailyData.State.Claimed);
            wrote = float.NaN;
            comp.Recount();
            comp.SnapToTargetForTest();
            C(comp.CurrentCount, 0,
              "③e 前提：四类源全不可领 ⇒ 计数 = 0（§见 `RewardsNotificationProvider` 的口径）");
            C(wrote, 0f, "③f 计数 = 0 ⇒ alpha 写成 **0**（原版 `Hide()`）—— 两态都过才说明这条链真的跟着计数走");
        }
        finally
        {
            DailyData.ResetAllStatesForTest();     // 全量还原口（见 `DailyData.cs` 的 `ResetAllStatesForTest`）
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ------------------------------------------------------------ 活 3：Campaign 那颗的真判据
    static void SectionCampaignJudge()
    {
        // 判据链（都可复核）：
        //   · 原版 `CampaignHandler.CheckNotification`（`decomp_full/Everguild.LiveOps.CampaignHandler._CheckNotification_d__21__MoveNext.c`）
        //     = 取当前所选阵营的战役、只要有节点 `Claimable` 就 **发一条** `CampaignBadge{army=SelectedArmy}`。
        //   · 我们 = `Shell/BadgeNotifications.cs` 的 `CampaignNotificationProvider.CheckNotification()`（同一口径）。
        //   · 第 2 键（Campaign）那颗的定义表 = `BadgeWiring.DefinitionsForTab("RewardsMenu_CampaignButton")`
        //     = `CampaignBadge`（无过滤）+ `MissionsBadge(parentId="CampaignMission")`（判据 = 原版 prefab
        //       `MonoBehaviour_-287428095701543169.json` 的 `references.RefIds`）。
        BadgeWiring.RegisterDefaults();
        var campDefs = BadgeWiring.DefinitionsForTab("RewardsMenu_CampaignButton");
        C(campDefs != null ? campDefs.Count : -1, 2,
          "④a 前提：第 2 键（Campaign）的红点定义 = 2 条（`CampaignBadge` + `MissionsBadge(CampaignMission)`）");

        CampaignData.ResetForTest();       // 归零 ⇒ 只解锁根节点（= 「有可领」这一态，可复现）
        try
        {
            bool hasContent = CampaignData.HasContent(CampaignData.Selected);
            bool claimable = false;
            for (int i = 0; i < CampaignData.NodeCount; i++) if (CampaignData.Claimable(i)) { claimable = true; break; }
            Tr(hasContent, "④b 前提：当前阵营 `" + (CampaignData.Selected ?? "?") + "` 本地**有战役内容**");
            Tr(claimable, "④c 前提：`ResetForTest()` 之后**至少有一个可领节点**（根节点出厂就开着）");

            // 数据侧独立算：provider 该发几条
            int want = 0;
            foreach (var n in new CampaignNotificationProvider().CheckNotification()) if (n != null) want++;
            C(want, claimable ? 1 : 0,
              "④d `CampaignNotificationProvider` 发 **" + want + "** 条（判据：有可领 ⇒ 恰好 1 条（🔴 2026-10-31 B106_W60：原版**不看**「本地有战役」—— `GetCampaign(SelectedArmy) ?? GetCampaign(10)`）；"
            + "原版一个战役只发一条，⛔ 不是每节点一条）");

            // 这条通知**算中**第 2 键那颗的定义表（= `GetCount` 的「过定义」那一半，逐条 `Validate`）
            int hit = 0;
            foreach (var n in new CampaignNotificationProvider().CheckNotification())
            {
                if (n == null) continue;
                for (int d = 0; d < campDefs.Count; d++) if (campDefs[d].Validate(n)) hit++;
                break;      // provider 只发一条
            }
            C(hit, 1, "④e 那条通知**算中**第 2 键的定义表 1 次（⇒ 这颗红点该亮 —— 旧断言「恒 0」的前提已过期）");

            // 灭自证（🔴 **2026-10-31（B106_W37）重写** —— 旧写法用 `new CampaignBadge{army="Goff"}` 当反例，
            //   那是**旧语义**（`CampaignBadge` 自己 override `Validate` 比 `army`）；改基类之后
            //   `Validate` 只剩 `RewardsBadge` 的两支：**不同类型比 `parentId`** / **同类型比 record 相等性（含 `army`）**。
            //   ⇒ 两支各打一条反例，缺一条就会出现「随便换个过滤器也 0」的假绿。
            // ① parentId 不同 ⇒ 0（打「不同类型只比 parentId」那一支）
            var wrongById = new BadgeNotificationDefinition(typeof(RewardsBadge), useFilter: true,
                                                            filter: new RewardsBadge { parentId = "NotACampaignMission" });
            // ② army 不同 ⇒ 0（打「同类型比 record 相等性（含 army）」那一支 —— 13 颗阵营格的红点全靠它）
            string otherArmy = null;
            for (int i = 0; i < CampaignData.Armies.Length; i++)
                if (!string.Equals(CampaignData.Armies[i], CampaignData.Selected, StringComparison.Ordinal))
                { otherArmy = CampaignData.Armies[i]; break; }
            Tr(otherArmy != null,
               "④f 前提：13 个阵营里找得到一个 ≠ 当前选中（`" + CampaignData.Selected + "`）的用于反例"
             + "（找不到 ⇒ ④h 空转，所以这条本身就是硬前提）");
            var wrongByArmy = otherArmy == null ? null
                : new BadgeNotificationDefinition(typeof(CampaignBadge), useFilter: true,
                                                  filter: new CampaignBadge { army = otherArmy });
            int wrongByIdHit = 0, wrongByArmyHit = 0;
            foreach (var n in new CampaignNotificationProvider().CheckNotification())
            {
                if (n == null) continue;
                if (wrongById.Validate(n)) wrongByIdHit++;
                if (wrongByArmy != null && wrongByArmy.Validate(n)) wrongByArmyHit++;
                break;
            }
            C(wrongByIdHit, 0,
              "④g 🧨 灭自证①：`RewardsBadge{parentId=\"NotACampaignMission\"}` 过滤 ⇒ **0**"
            + "（不同类型只比 `parentId`；把它改成「类型对上就算」⇒ 这里必红）");
            C(wrongByArmyHit, 0,
              "④h 🧨 灭自证②：`CampaignBadge{army=\"" + (otherArmy ?? "?") + "\"}` 过滤 ⇒ **0**"
            + "（同类型走 record 相等性 ⇒ `army` 参与；删掉 `army` 比较退回「同类型即匹配」⇒ 这里必红）");
        }
        finally { CampaignData.ResetForTest(); }
    }

    // ------------------------------------------------------------ 活 4：登录卡口径（记录用）
    static void SectionLogin()
    {
        // 回源结论（`报告/B94_Z1.md` §4）：原版谓词 = `x.canCollect && !x.IsComplete()`
        //   （`cpp2il/out_cpp2il_dev_ilspy/Missions.cs:787`，即 `CheckNotification` 里的 `_003C_003Eb__18_1`），
        //   而 `canCollect` **客户端只有一个写点**（`DailyRewardTrackPanel.cs:81/93`，买 Premium 的回调），
        //   其余来自**服务端下发**（`decomp_full/Missions__UpdateMissionProgress.c` 走 `PlayfabWrapper.GenericCloudScriptHandler(0x38c,…)`）
        //   ⇒ 「什么时候算可领」是**服务端判据**，本机拿不到 ⇒ 自造一条闸 = 发明原版没有的东西（红线）。
        // 本段只锁**事实**：我们的登录卡是**单奖励模型**（`MissionProgressView(max=1)`），
        //   `CanCollectLogin()` = `_loginState != Claimed` 在那一模型下**等价于**原版的 `canCollect && !IsComplete()`。
        DailyData.ResetAllStatesForTest();      // 出厂态：登录 = InProgress
        try
        {
            Tr(DailyData.CanCollectLogin(), "⑤a 出厂态（`_loginState = InProgress`）⇒ `CanCollectLogin()` 为真"
              + "（= 原版「今天登录过、奖励还没领」）—— 这就是 Missions / 导航 Rewards 两颗红点**每天都会亮**的来源");
            DailyData.ForceLoginStateForTest(DailyData.State.Claimed);
            Tr(!DailyData.CanCollectLogin(), "⑤b 领过（`Claimed`）⇒ 为假（= 原版 `!IsComplete()` 那一半）"
              + "。🧨 改坏法：把闸写成恒真 ⇒ 领完之后红点永不灭 ⇒ 红");
            int n0;
            DailyData.ResetAllStatesForTest();
            n0 = DailyData.CollectableMissionCount();
            Tr(n0 >= 1, "⑤c 出厂态 `CollectableMissionCount() >= 1`（= " + n0 + "）—— 如实锁住「登录卡算一条可领」这一事实；"
              + "⚠️ 若将来按原版 7 天登录轨重做，这条要跟着改（本函数的「不改」结论写在报告 §4）");
        }
        finally { DailyData.ResetAllStatesForTest(); }
    }

    // ------------------------------------------------------------ 活 5：`RewardsBadge` 派生语义（2026-10-31 · B106_W37）
    static void SectionRewardsDerived()
    {
        // 判据（三源逐文件现读，⛔ 不是转述）：
        //   · `cpp2il/out_cpp2il_dev_ilspy/Everguild.UI.Notifications/CampaignBadge.cs:8` `public record CampaignBadge : RewardsBadge`
        //   · 同上 `ForgeBadge.cs:8` `public record ForgeBadge : RewardsBadge`；`il2cpp_out/dump.cs:123684 / :123752`（第三源）
        //   · `cpp2il/out_il2csharp/…/RewardsBadge.cs:24-44` 的 `Validate` **两支**：
        //       ① `other is RewardsBadge && other.GetType() != this.GetType()` ⇒ 只比 `parentId`
        //       ② 其余 ⇒ `this.Equals(other)` = record 相等性（同类型 ∧ parentId ∧ 派生字段）
        //     · 第二源同形：`decomp_full/Everguild.UI.Notifications.RewardsBadge__Validate.c`
        //   · ctor 把继承来的 `parentId` 写 `""`：`CampaignBadge.cs:26-32` / `ForgeBadge.cs:26-32`
        //   · record 相等性含 `army`：`decomp_full/…CampaignBadge__Equals.c`（先调 `RewardsBadge__Equals`，再比 `+0x18`）

        // ---- ⑥a~⑥d 继承链与 ctor ----
        Tr(typeof(RewardsBadge).IsAssignableFrom(typeof(CampaignBadge)),
           "⑥a ★ `CampaignBadge : RewardsBadge`（原版 record 链）。🧨 改回 `: BadgeNotification` ⇒ 红 ——"
         + " `IsRelated(RewardsBadge, CampaignBadge)` 随之变假（兄弟类）⇒ 导航 REWARDS 少算战役");
        Tr(typeof(RewardsBadge).IsAssignableFrom(typeof(ForgeBadge)),
           "⑥b ★ `ForgeBadge : RewardsBadge`（同上）。🧨 改回 ⇒ 导航 REWARDS 少算锻造");
        C(new CampaignBadge().parentId, "",
          "⑥c `CampaignBadge()` 的 `parentId = \"\"`（原版 ctor 第一句：`this+16 = \"\"`）");
        C(new ForgeBadge().parentId, "",
          "⑥d `ForgeBadge()` 的 `parentId = \"\"`（同上）");

        // ---- ⑥e~⑥k `Validate` 两支（灭自证 = 两支各一条结构上不可能同时成立的组合）----
        Tr(new CampaignBadge { parentId = "" }.Validate(new ForgeBadge { parentId = "" }),
           "⑥e ★ 同属 `RewardsBadge`、**运行时类型不同**、`parentId` 相同 ⇒ 算中（第一支）");
        Tr(!new CampaignBadge { parentId = "A" }.Validate(new ForgeBadge { parentId = "B" }),
           "⑥f 🧨 …`parentId` 不同 ⇒ 不算中（第一支真的在比 `parentId`）");
        Tr(!new CampaignBadge { parentId = "A" }.Validate(new CampaignBadge { parentId = "B" }),
           "⑥g ★ **同类型**却 `parentId` 不同 ⇒ 不算中（走 record 相等性，⛔ 不是「同类型即匹配」）");
        Tr(new CampaignBadge { army = "Goff" }.Validate(new CampaignBadge { army = "Goff" }),
           "⑥h 同类型 ∧ 同 `parentId` ∧ 同 `army` ⇒ 算中（record 相等性全等）");
        Tr(!new CampaignBadge { army = "Goff" }.Validate(new CampaignBadge { army = "Ultramarines" }),
           "⑥i 🧨 同类型 ∧ 同 `parentId` ∧ **`army` 不同** ⇒ 不算中 —— 13 颗阵营格的红点全靠这一条"
         + "（删掉 `army` 比较 ⇒ 每颗都数到同一条 ⇒ 12 颗假亮）");
        Tr(!new ForgeBadge { army = "Goff" }.Validate(new ForgeBadge { army = "Necrons" }),
           "⑥j 🧨 同上，`ForgeBadge` 的 `army` 也仍参与");
        Tr(!new CampaignBadge().Validate(new InboxBadge()),
           "⑥k 🧨 不是 `RewardsBadge` 的通知 ⇒ 不算中（⛔ 别把「任意通知都能过」写成默认支）");

        // ---- ⑥l~⑥t 导航 REWARDS：战役 / 锻造有可领 ⇒ 那颗也亮 ----
        BadgeWiring.RegisterDefaults();          // 幂等（`ProviderCount > 0` 直接返回）
        var navDefs = BadgeWiring.DefinitionsFor("Rewards");
        C(navDefs != null ? navDefs.Count : -1, 1,
          "⑥l 前提：导航「Rewards」= 1 条定义");
        Tr(navDefs != null && navDefs.Count == 1 && navDefs[0] != null && navDefs[0].useFilter
           && navDefs[0].filterCriteria is RewardsBadge
           && ((RewardsBadge)navDefs[0].filterCriteria).parentId == "",
           "⑥m 前提：那条 = `useFilter=1` ∧ `filter is RewardsBadge{parentId=\"\"}`（场景 MB 的 `references.RefIds`）");
        var missionsDefs = BadgeWiring.DefinitionsForTab("RewardsMenu_MissionsButton");
        C(missionsDefs != null ? missionsDefs.Count : -1, 1,
          "⑥n 前提：Missions 页签 = 1 条（`MissionsBadge`，无过滤）—— 下面拿它当「只数任务」的对照组");

        var campItems = new List<BadgeNotification>(new CampaignNotificationProvider().CheckNotification());
        var forgeItems = new List<BadgeNotification>(new ForgeNotificationProvider().CheckNotification());
        Tr(campItems.Count >= 1,
           "⑥o 前提：此刻战役 provider 至少发 1 条（现读 " + campItems.Count + " 条；"
         + "`HasContent(" + CampaignData.Selected + ") ∧ 有可领节点` 由 ④b/④c 与本段共用同一个前提）");
        Tr(forgeItems.Count >= 1,
           "⑥p 前提：此刻锻造 provider 至少发 1 条（现读 " + forgeItems.Count + " 条）—— 0 条时下面两条空转");

        int campHit = 0, forgeHit = 0;
        for (int i = 0; i < campItems.Count; i++) if (navDefs[0].Validate(campItems[i])) campHit++;
        for (int i = 0; i < forgeItems.Count; i++) if (navDefs[0].Validate(forgeItems[i])) forgeHit++;
        C(campHit, campItems.Count,
          "⑥q ★ 每一条战役通知都**算中**导航 REWARDS 那条定义（原版：战役有可领 ⇒ REWARDS 也亮）");
        C(forgeHit, forgeItems.Count, "⑥r ★ 每一条锻造通知都算中（同上）");

        UiBadgeNotificationManager.Refresh(false);     // 读时刷新（⛔ `notify:true` 会 `Recount → OnNotify` 递归）
        int nav = UiBadgeNotificationManager.GetCount(navDefs);
        int onlyMissions = UiBadgeNotificationManager.GetCount(missionsDefs);
        Tr(nav - onlyMissions >= campItems.Count + forgeItems.Count,
           "⑥s ★★ **管理器级**：REWARDS 导航钮计数 **" + nav + "**，比「只数任务」的 " + onlyMissions + " **多出** "
         + (campItems.Count + forgeItems.Count) + "（= 战役 " + campItems.Count + " + 锻造 " + forgeItems.Count
         + "）⇒ **战役/锻造有可领 ⇒ REWARDS 导航钮计数 > 0**"
         + "。🧨 把基类改回 `: BadgeNotification` ⇒ 外闸 `IsRelated` 跳过这两类 ⇒ 差值变 0 ⇒ 红");

        // 灭自证：换一个 `parentId` 的过滤器 ⇒ 战役/锻造都不再算中（证明 ⑥s 的差值来自 `parentId` 比较）
        var otherParent = new BadgeNotificationDefinition(typeof(RewardsBadge), useFilter: true,
                                                          filter: new RewardsBadge { parentId = "CampaignMission" });
        int campOther = 0;
        for (int i = 0; i < campItems.Count; i++) if (otherParent.Validate(campItems[i])) campOther++;
        C(campOther, 0,
          "⑥t 🧨 灭自证：`RewardsBadge{parentId=\"CampaignMission\"}` 过滤 ⇒ 战役一条都不算中"
        + "（证明 ⑥s 的正数来自 `parentId` 比较，⛔ 不是「类型对上就算」）");

        UiBadgeNotificationManager.Refresh(false);     // 缓存刷回（本段没改数据，但上面的 Refresh 已把快照重建过一次）
    }

    // ------------------------------------------------------------ 工具
    static string ReadShellSource(string file)
    {
        try
        {
            string p = System.IO.Path.Combine(Application.dataPath, "CardPresentation", "Shell", file);
            return System.IO.File.Exists(p) ? System.IO.File.ReadAllText(p) : null;
        }
        catch (Exception e) { Debug.LogError("[Z1] 读源码失败：" + e.Message); return null; }
    }

    /// <summary>取出 `public void RefreshBadges()` 的**方法体**（从签名到配对的右花括号）。
    /// 找不到返回 null —— ⛔ 不做「退回整份源码」的兜底（那会让「方法没了」也通过）。</summary>
    static string SliceRefreshBadges(string src)
    {
        const string sig = "public void RefreshBadges()";
        int i = src.IndexOf(sig, StringComparison.Ordinal);
        if (i < 0) return null;
        int open = src.IndexOf('{', i + sig.Length);
        if (open < 0) return null;
        int depth = 0;
        for (int k = open; k < src.Length; k++)
        {
            if (src[k] == '{') depth++;
            else if (src[k] == '}')
            {
                depth--;
                if (depth == 0) return src.Substring(open, k - open + 1);
            }
        }
        return null;
    }

    // ------------------------------------------------------------ 活 6（B106_W54 / B106_W60）
    // 判据（一手，2026-10-31 现读复核，⛔ 不是转述）：
    //  · 红点回退 UM：`decomp_full/Everguild.LiveOps.CampaignHandler__GetCurrentCampaign.c:7-11`
    //    + `…_CheckNotification_d__21__MoveNext.c:25-28`（`GetCampaign(SelectedArmy) ?? GetCampaign(10)`）；
    //    `CardArmy.Ultramarines = 10`。⇒ 选中「本地无战役」阵营时**回退 Ultramarines**，红点照发。
    //  · 点数闸：`decomp_full/CampaignWindowTab__OnNodeClicked.c:36-46`（`bVar11`）
    //    + `cpp2il/out_il2csharp/…/Everguild/LiveOps/Campaign.cs:153-160`（`_get_AvailableNodes_b__7_1`）。
    static void SectionW54()
    {
        // ---- W54-1：选中「本地无战役」阵营 ⇒ 红点**仍亮**（原版回退 Ultramarines）
        CampaignData.ResetForTest();                       // 起手只有根 UM0 可领
        string w54Sel0 = CampaignData.Selected;
        try
        {
            string w54Other = null;
            for (int i = 0; i < CampaignData.Armies.Length; i++)
                if (!string.Equals(CampaignData.Armies[i], CampaignData.ArmiesWithContent, StringComparison.Ordinal))
                { w54Other = CampaignData.Armies[i]; break; }
            Tr(w54Other != null, "W54-1 前提：找得到一个 ≠ Ultramarines 的阵营（造「本地无战役」那一态）");
            Tr(CampaignData.HasContent(CampaignData.ArmiesWithContent), "W54-1 前提：Ultramarines 本地**有**战役内容");
            if (w54Other == null) return;

            CampaignData.Select(w54Other);                  // 切到一个本地没有节点表的阵营
            C(CampaignData.Selected, w54Other, "W54-1 前提：`Select` 真的换了阵营");
            Tr(!CampaignData.HasContent(w54Other), "W54-1 前提：新选中的阵营确实 `HasContent == false`");
            Tr(CampaignData.Claimable(0), "W54-1（前提）UM 那套的根节点此刻可领（`ResetForTest` 后 UM0 = Unlocked、Cost 100）");

            int w54N1 = 0; string w54Army1 = null;
            foreach (var n in new CampaignNotificationProvider().CheckNotification())
                if (n != null) { w54N1++; var cb = n as CampaignBadge; if (cb != null) w54Army1 = cb.army; }
            C(w54N1, 1, "★★ W54-1：切到**本地无战役**的阵营 ⇒ 战役红点**仍然发 1 条**"
                      + "（原版：`GetCampaign(SelectedArmy)` 取不到 ⇒ 回退 `GetCampaign(10)`=Ultramarines，"
                      + "它还有可领节点 ⇒ 发一条）"
                      + "｜🧨 加回 `HasContent` 闸 ⇒ 0 ⇒ 红");
            C(w54Army1, w54Other, "★★ W54-1：那条 `CampaignBadge.army` 恒 = **SelectedArmy**（⛔ 不是回退后的 UM）"
                               + "（判据：`_CheckNotification_d__21__MoveNext.c:52` 取 `handler + 0x7c` = SelectedArmy）");
        }
        finally
        {
            CampaignData.Select(w54Sel0);
            CampaignData.ResetForTest();
        }

        // ---- W54-2：点数 < 节点 Cost ⇒ 不可领、红点灭
        CampaignData.ResetForTest();                        // UM0 = Unlocked(10)，Cost = 100
        try
        {
            int w54Cost = CampaignData.At(0).Cost;
            Tr(CampaignData.HasContent(CampaignData.Selected),
               "W54-2 前提：当前阵营本地有战役（红点这一半要先有 provider 会遍历的那套节点）");
            C(CampaignData.AvailablePoints, CampaignData.Points,
              "W54-2 前提：可花点数默认 = `Points`（" + CampaignData.Points + "；原版那数来自服务端存档，本地是 🔶 近似）");
            Tr(CampaignData.Claimable(0), "W54-2（前提）点数足（" + CampaignData.AvailablePoints + " ≥ " + w54Cost + "）⇒ 根节点可领");

            CampaignData.SetPointsForTest(w54Cost - 1);
            Tr(!CampaignData.Claimable(0),
               "★★ W54-2：点数**差 1 点**（" + (w54Cost - 1) + " < " + w54Cost + "）⇒ 该节点**不可领**"
             + "（原版 `CampaignWindowTab__OnNodeClicked.c:38` `if (AvailablePoints < Cost) bVar11 = false;`）"
             + "｜🧨 删掉 `&& AvailablePoints >= Nodes[i].Cost` ⇒ 仍可领 ⇒ 红");

            int w54Low = 0;
            foreach (var n in new CampaignNotificationProvider().CheckNotification()) if (n != null) w54Low++;
            C(w54Low, 0, "★★ W54-2：点数不够 ⇒ 战役红点**一条都不发**（= 原版「红点灭」的情形①）"
                       + "｜🧨 删掉点数闸 ⇒ 1 ⇒ 红");

            CampaignData.SetPointsForTest(w54Cost);
            Tr(CampaignData.Claimable(0), "★★ W54-2：点数**恰好等于** `Cost` ⇒ **可领**"
                                       + "（原版是 `Cost <= AvailablePoints`，等号算够 —— 写成 `>` 这里就红）");

            string w54Why = null;
            CampaignData.SetPointsForTest(0);
            Tr(!CampaignData.Claim(0, CampaignData.TierBasic, out w54Why), "W54-2：点数 0 ⇒ `Claim` 被拒");
            Tr(w54Why != null && w54Why.Contains("战役点不够"),
               "★★ W54-2：被拒时 `why` **说对原因**（含「战役点不够」），⛔ 不是那句「基础档已经领过了」"
             + "｜🧨 把 `why` 的三元支删掉 ⇒ 红（这是「不许静默失败」红线的守卫）｜why=[" + w54Why + "]");
        }
        finally
        {
            CampaignData.ResetPointsForTest();
            CampaignData.ResetForTest();
        }
    }
}
