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
        MenuCheck.Section(_sink, "B94_Z1 · 活 4：登录卡口径（记录；回源结论 = 不改）");
        SectionLogin();
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
            C(want, (hasContent && claimable) ? 1 : 0,
              "④d `CampaignNotificationProvider` 发 **" + want + "** 条（判据：有内容 ∧ 有可领 ⇒ 恰好 1 条；"
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

            // 灭自证：换成**别的阵营**的过滤条件 ⇒ 一条都不中（证明 ④e 不是「什么定义都算」）
            var wrong = new BadgeNotificationDefinition(typeof(CampaignBadge), useFilter: true,
                                                        filter: new CampaignBadge { army = "Goff" });
            int wrongHit = 0;
            foreach (var n in new CampaignNotificationProvider().CheckNotification())
            {
                if (n == null) continue;
                if (wrong.Validate(n)) wrongHit++;
                break;
            }
            C(wrongHit, 0,
              "④f 🧨 灭自证：拿 `CampaignBadge{army=\"Goff\"}` 去过滤 ⇒ **0**（当前选中 `" + CampaignData.Selected
            + "`）⇒ ④e 的正数才有意义");
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
}
