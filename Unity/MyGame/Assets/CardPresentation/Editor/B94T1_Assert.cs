// B94T1_Assert.cs — 执行代理 `B94_T1`（第九十四会话 · 波 1.5）两件活的断言
//
// ============================ 它盯的是什么 ============================
//   活 **N**：`ReleaseEventAssets` 搬回 `LiveOpsAssetUtility`（原版那个类）。
//     · 原版 `LiveOpsAssetUtility.ReleaseEventAssets(ILiveOps)`（`decomp_full/Everguild.LiveOps.LiveOpsAssetUtility__ReleaseEventAssets.c`）。
//     · 本波把它从 `LiveOpsContainers` 的 partial（B93·N2 当时的临时落点）搬进 `Shell/LiveOpsLocalCatalog.cs`
//       文件尾的 `LiveOpsAssetUtility` partial 段。**逻辑逐字未改**，本组断言盯的是
//       「在新家 / 旧家没了 / 行为逐条不变 / 调用点跟着改」。
//   活 **L**：内容页页签红点（Missions / Campaign / Forge / DailyStreak）+ 阵营格那条路的
//     「红点常亮 / 该亮不亮」。本波真改的两件：
//       (a) `RewardsNotificationProvider` 的通知类型 `RewardsBadge`（基类）→ **`MissionsBadge`**（派生，
//           = 原版 `Missions : INotificationProvider<MissionsBadge>`）；
//       (b) `UiBadgeNotification` 变成红点 alpha 的**唯一写手**（`ReassertAlpha` + `Attach` 装的回读口）。
// 逐条判据 / 为什么某些格仍停在 B → `资料/完全复刻/报告/B94_T1.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么用（宿主）============================
//   `B94T1_Assert.Run();`
//   + 宿主里两行（照 `ShopFamilyAssertB94S1` 的先例）：
//     `_sink.Pass += B94T1_Assert.Pass; _sink.Fail += B94T1_Assert.Fail;`
//   **建议宿主 = `CardPresentation/Editor/MainMenuScene.cs`**（红点/通知系统那一节：`BadgeWiring.RegisterDefaults()`
//   ／`UiBadgeNotificationManager` 已就绪；四颗内容页签的红点也由该宿主直接查）。
//   备选 = `Editor/RewardsScene.cs`（`RewardsTabButton_*` 就在那棵树里）。
//   ⚠️ 本组**不需要**建整窗（除 ②b/②c 读全局 provider 表外都是纯静态/纯组件级），也不建持久资产。
//
// ============================ 🔴 灭自证 ============================
//   每条「正向」都配一条**结构上不可能同时满足**的反向（见断言文案里的「🧨 改坏法」）。
//   ⛔ 期望值全部是**字面量**或**独立数出来**的数，**不从被测实现里读**。
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B94_T1`：`ReleaseEventAssets` 搬迁（活 N）＋ 内容页签红点口径与唯一写手（活 L）。</summary>
public static class B94T1_Assert
{
    static readonly CheckSink _sink = new CheckSink("[T1] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    const BindingFlags PubStat = BindingFlags.Public | BindingFlags.Static;

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B94_T1 · N：`ReleaseEventAssets` 搬回 `LiveOpsAssetUtility`");
        SectionN();
        MenuCheck.Section(_sink, "B94_T1 · L：内容页页签红点（计数口径 + 唯一写手）");
        SectionL();
    }

    // ------------------------------------------------------------ N `ReleaseEventAssets` 搬迁
    static void SectionN()
    {
        // ① 位置：新家在 `LiveOpsAssetUtility`（原版那个类），旧家 `LiveOpsContainers` 必须**没有**了
        var onUtility = typeof(LiveOpsAssetUtility).GetMethod("ReleaseEventAssets", PubStat, null,
                                                             new[] { typeof(ILiveOps) }, null);
        Tr(onUtility != null,
           "①a 新家：`LiveOpsAssetUtility.ReleaseEventAssets(ILiveOps)` 存在"
         + "（原版 `Everguild.LiveOps.LiveOpsAssetUtility__ReleaseEventAssets.c`；账 row 的落点就是这个类）");

        var onContainers = typeof(LiveOpsContainers).GetMethod("ReleaseEventAssets", PubStat);
        Tr(onContainers == null,
           "①b 旧家：`LiveOpsContainers.ReleaseEventAssets` **已不存在**"
         + "（🧨 改坏法：把旧段原样留一份 ⇒ 立刻红 —— 两处各一份 = 迟早不一致，铁律「两处写同一条规则」）");

        var releaseAsset = typeof(LiveOpsAssetUtility).GetField("ReleaseAsset", PubStat);
        Tr(releaseAsset != null && releaseAsset.FieldType == typeof(Action<string>),
           "①c 注入点 `LiveOpsAssetUtility.ReleaseAsset`（`Action<string>`）也在新家 —— 释放动作的**唯一**出口");
        Tr(typeof(LiveOpsContainers).GetField("ReleaseAsset", PubStat) == null,
           "①d 旧家不再有 `ReleaseAsset`（🧨 改坏法：两份注入点 ⇒ 自检台只装了一边，另一半静默不释放）");

        // ② 行为：逐条与搬迁前**同一份**（`.c` 三句：null 直接返回 · 两遍遍历 · 空引用报错不释放）
        var oldHook = LiveOpsAssetUtility.ReleaseAsset;
        try
        {
            int calls = 0;
            string lastGuid = null;
            LiveOpsAssetUtility.ReleaseAsset = delegate (string g) { calls++; lastGuid = g; };

            // ②a 原版 `.c` 第一句：`if (param_1 == 0) return;`（**不是** NRE）
            calls = 0;
            bool threw = false;
            try { LiveOpsAssetUtility.ReleaseEventAssets(null); }
            catch (Exception e) { threw = true; Debug.LogError("[T1] ReleaseEventAssets(null) 抛了：" + e.Message); }
            Tr(!threw, "②a 前提：`ReleaseEventAssets(null)` **不抛**（原版 `.c` 第一句是 `return`，不是 NRE）");
            C(calls, 0, "②b `ReleaseEventAssets(null)` ⇒ 释放 **0** 次");

            // ②c 本地目录里真有一条事件 ⇒ 释放次数 = Components 条数 + Assets 条数（**自己独立数一遍**）
            var cat = LiveOpsLocalCatalog.Events;
            Tr(cat != null && cat.Count > 0, "②c 前提：本地活动目录非空（否则下面两条无从谈起）");
            if (cat != null && cat.Count > 0)
            {
                // 挑**第一条真有引用**的（`Events[0]` 不保证非空；为 0 会让 ②e 空转绿）
                LiveOpsLocalEvent any = cat[0];
                for (int i = 0; i < cat.Count; i++)
                {
                    var c0 = cat[i];
                    if (c0 == null) continue;
                    int n0 = (c0.Components != null ? c0.Components.Length : 0)
                           + (c0.Assets != null ? c0.Assets.Length : 0);
                    if (n0 > 0) { any = c0; break; }
                }
                int expect = (any.Components != null ? any.Components.Length : 0)
                           + (any.Assets != null ? any.Assets.Length : 0);
                C(expect > 0, true,
                  "②d 前提：'" + any.EventId + "' 的引用表非空（= " + expect + "；为 0 会让 ②e 空转绿）");
                calls = 0; lastGuid = null;
                var inst = new FixtureEvent(any.EventId);
                LiveOpsAssetUtility.ReleaseEventAssets(inst);
                C(calls, expect,
                  "②e 释放次数 = Components + Assets = " + expect
                + "（🧨 改坏法：删掉 `EventAssets` 那半遍循环 ⇒ 少放一半，立刻红）");
            }

            // ②f 没接注入点 ⇒ **一次都不释放**、且**不抛**（= 「没装就出声、⛔ 不假装释放过」）
            LiveOpsAssetUtility.ReleaseAsset = null;
            calls = 0;
            threw = false;
            try { if (cat != null && cat.Count > 0) LiveOpsAssetUtility.ReleaseEventAssets(new FixtureEvent(cat[0].EventId)); }
            catch (Exception e) { threw = true; Debug.LogError("[T1] 无注入点时抛了：" + e.Message); }
            Tr(!threw && calls == 0, "②f 没装 `ReleaseAsset` ⇒ 释放 0 次且不抛（出声不静默）");
        }
        finally { LiveOpsAssetUtility.ReleaseAsset = oldHook; }

        // ③ 调用点跟着搬（结构断言：源码扫描 —— 唯一的生产态自检调用点在 `LiveOpsFilter.cs`）
        string filterSrc = ReadShellSource("LiveOpsFilter.cs");
        Tr(filterSrc != null, "③a 前提：读得到 `Shell/LiveOpsFilter.cs` 源码（读不到 ⇒ 下面两条无意义）");
        if (filterSrc != null)
        {
            Tr(filterSrc.Contains("LiveOpsAssetUtility.ReleaseEventAssets"),
               "③b 自检调用点已改指 **新位置**（`LiveOpsAssetUtility.ReleaseEventAssets`）");
            Tr(filterSrc.IndexOf("LiveOpsContainers.ReleaseEventAssets", StringComparison.Ordinal) < 0,
               "③c 且**不再**出现 `LiveOpsContainers.ReleaseEventAssets`（🧨 改坏法：改回旧名 ⇒ 编不过/本条红）");
        }

        // ③d 两个原版生产态调用方的**文档指针**也必须指新位置（它们今天只出声、不真释放 —— 见报告 §没查清）
        foreach (var f in new[] { "EnergySinglePlayerOnlyEventWindow.cs", "ExpansionPassEventWindow.cs" })
        {
            string s2 = ReadShellSource(f);
            Tr(s2 != null && s2.Contains("LiveOpsAssetUtility.ReleaseEventAssets"),
               "③d `" + f + "` 里那句原版调用指针指向 `LiveOpsAssetUtility.ReleaseEventAssets`"
             + "（原版 `OnDestroy` / `OnClose` / `Reset` 就是这一跳）");
        }
    }

    // ------------------------------------------------------------ L 内容页页签红点
    static void SectionL()
    {
        // ① 通知类型：原版 `Missions : LiveOpsHandler<IMission>, INotificationProvider<MissionsBadge>`
        //    （ILSpy `Everguild.LiveOps/Missions.cs:14`；`CheckNotification()` 第一句 = `new MissionsBadge()`，`:2139`）
        var prov = new RewardsNotificationProvider();
        Type t = prov.NotificationType;
        C(t, typeof(MissionsBadge),
          "①a 任务红点 provider 的通知类型 = **`MissionsBadge`**（派生类）"
        + "。🧨 改坏法：改回基类 `RewardsBadge` ⇒ 立刻红 —— `BadgeNotificationDefinition.Validate` 的不过滤那支是"
        + " `notificationType.IsInstanceOfType(item)`（`BadgeNotificationDefinition__Validate.c` + `ReflectionHelper__Is.c`），"
        + "发基类时 `MissionsBadge.IsInstanceOfType` **恒 false** ⇒ Missions 页签计数恒 0");
        Tr(typeof(MissionsBadge).IsSubclassOf(typeof(RewardsBadge)),
           "①b `MissionsBadge : RewardsBadge`（派生关系是「层级传递」那一支，见 `BadgeNotifications.cs` 文件头 ⑤）");

        // ② 一条 MissionsBadge **同时**算中两颗红点：导航「Rewards」那颗（`RewardsBadge` + filter parentId=""）
        //    与 Missions 页签那颗（`MissionsBadge`、不过滤）—— 原版就是同一个 provider 喂这两处。
        BadgeWiring.RegisterDefaults();          // 幂等（`ProviderCount > 0` 直接返回）
        var missionsDefs = BadgeWiring.DefinitionsForTab("RewardsMenu_MissionsButton");
        var navDefs = BadgeWiring.DefinitionsFor("Rewards");
        C(missionsDefs != null ? missionsDefs.Count : -1, 1,
          "②a 前提：`RewardsMenu_MissionsButton` 的红点定义 = 1 条（`MissionsBadge`，判据 `MonoBehaviour_1604463168230790911.json`）");
        C(navDefs != null ? navDefs.Count : -1, 1,
          "②b 前提：导航「Rewards」= 1 条（`RewardsBadge` + useFilter `parentId=\"\"`，判据 `BadgeWiring.DefinitionsFor`）");

        try
        {
            // 造「有可领任务」这一态（出厂态登录未领 ⇒ `CollectableMissionCount() >= 1`，见 `DailyData.CanCollectLogin`）
            DailyData.ForceCollectableForTest();
            int onMissions = UiBadgeNotificationManager.GetCount(missionsDefs);
            int onNav = UiBadgeNotificationManager.GetCount(navDefs);
            Tr(onMissions > 0,
               "②c 造出「可领」之后，**Missions 页签**那颗算得出计数（= " + onMissions + "）"
             + "。🧨 改坏法：provider 发基类 ⇒ 这里恒 0（而旧代码里那颗红点靠 `RefreshBadges` 的遗留布尔点着 = 两条写手）");
            Tr(onNav > 0,
               "②d **同一批**通知也算中导航「Rewards」那颗（= " + onNav + "）—— 原版一个 provider 喂两处，⛔ 别为页签另造一份");
            C(onMissions, onNav,
              "②e 两颗**同源同数**（`MissionsBadge` 在 `RewardsBadge` 的 filter `parentId=\"\"` 下照样成立）");

            // ②f 灭自证：换成「另一类」定义 ⇒ 0（证明它不是「什么定义都算」）
            // 🔴 R7 订正（2026-10-29 FX5 · 判据 `B94_DG2_battle_menu.md` §二·#12）：原来拿 `CampaignBadge` 当「另一类」，
            //   而 `CampaignNotificationProvider`（`BadgeNotifications.cs:335`，`RegisterDefaults` 已登记）在
            //   「战役根节点可领」时**真发一条** `CampaignBadge`（`:344` `CampaignData.Claimable`）⇒ 那 1 是**真命中**。
            //   ⛔ 也别换 `ForgeBadge`（锻造同样有 provider）。唯一安全写法 = **自造一个没有 provider 的私有类型**。
            var wrong = new List<BadgeNotificationDefinition> { new BadgeNotificationDefinition(typeof(B94T1NoBadge), false) };
            C(UiBadgeNotificationManager.GetCount(wrong), 0,
              "②f 🧨 灭自证：拿一个**没有任何 provider** 的私有通知类型去数 ⇒ **0**（否则上面的正数什么也证明不了）");

            // ②g 反过来：全部领完 ⇒ 0（`RefreshBadges` 那条遗留布尔也是这个口径，两条写手此后再也不打架）
            // 🔴 R7 订正（2026-10-29 FX5 · §二·#13）：`GetCount` 只读 `_notifications` **缓存快照**
            //   （`BadgeNotifications.cs:206-224`），而 `DailyData.Force*ForTest` 那几个口都**不**调 `Refresh`
            //   ⇒ 原来那条是在数**改状态之前的旧快照**（假红）。每次状态变更后补一句 `Refresh(notify:false)`
            //   （= 文件头「读时刷新」那一档；⛔ 别用 notify:true —— 会 `Recount → Refresh → OnNotify` 递归）。
            DailyData.ResetAllStatesForTest();
            UiBadgeNotificationManager.Refresh(false);
            DailyData.ForceLoginStateForTest(DailyData.State.Claimed);
            UiBadgeNotificationManager.Refresh(false);
            DailyData.ForceSkullsStateForTest(DailyData.State.Claimed);
            UiBadgeNotificationManager.Refresh(false);
            C(UiBadgeNotificationManager.GetCount(missionsDefs), 0,
              "②g 四类源全不可领（登录/骷髅已领、其余 `InProgress`）⇒ Missions 页签计数 = **0**（红点该灭就灭）");
        }
        finally
        {
            DailyData.ResetAllStatesForTest();    // 回到出厂（全量还原口，见 `DailyData.cs:2266`）
            UiBadgeNotificationManager.Refresh(false);   // 缓存也刷回出厂态，⛔ 别把「全已领」的快照漏给别的段/宿主
        }

        // ③ 唯一写手：组件自持（`UiBadgeNotification.ReassertAlpha`）
        var go = new GameObject("B94T1_BadgeLead");
        var comp = go.AddComponent<UiBadgeNotification>();
        try
        {
            Tr(comp != null, "③a 前提：`UiBadgeNotification` 挂得上去（否则下面三条空转）");
            if (comp != null)
            {
                float quadAlpha = 0f;      // 模拟「红点 quad 材质色现在的 alpha」（= `GetAlpha` 的返回值）
                float wrote = float.NaN;
                comp.SetAlpha = delegate (float a) { wrote = a; quadAlpha = a; };
                comp.GetAlpha = delegate { return quadAlpha; };

                comp.Hide(); comp.Step(1f);                       // 目标 0、批处理里贴值
                C(quadAlpha, 0f, "③b 目标 alpha = 0 ⇒ quad 材质色被写成 0（`Hide()` + `Step(1)`）");

                quadAlpha = 1f;                                   // 🧨 模拟遗留通道直写（`RewardsWindow.RefreshBadges` 那条路）
                wrote = float.NaN;
                comp.ReassertForTest();
                C(wrote, 0f, "③c 外部把 alpha 偷改成 1 ⇒ 组件**下一帧纠回 0**"
                           + "（这条就是「内容页页签红点常亮」的止血：`Update` 里同一份逻辑）");
                C(quadAlpha, 0f, "③d 纠回之后 quad 材质色真的回到 0（不是只记在字段里）");

                comp.Show(); comp.Step(1f);                       // 目标 1
                quadAlpha = 0f; wrote = float.NaN;
                comp.ReassertForTest();
                C(wrote, 1f, "③e 反向：目标 1 时被偷改成 0 ⇒ 也纠回 1（🧨 恒写 0 的实现过不了这一条）");

                // ③f 灭自证：不装回读口 ⇒ 组件**不写**（证明这条闸真的靠 `GetAlpha`，不是无条件写）
                comp.GetAlpha = null;
                quadAlpha = 0f; wrote = float.NaN;
                comp.ReassertForTest();
                Tr(float.IsNaN(wrote), "③f 🧨 灭自证：没有回读口 ⇒ `ReassertForTest()` 一次都不写（否则上面两条是空的）");
                C(quadAlpha, 0f, "③g 且 quad 保持外部那个值（组件不凭空造写）");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }

        // ④ 两面覆盖（结构断言）：**页签**与**阵营格**两条路共用同一个 choke point `BadgeWiring.Attach` ——
        //    所以 ③ 那条「唯一写手」对两面都成立，⛔ 不需要各修一遍（修两遍 = 同一条规则两处写）。
        string badgeSrc = ReadShellSource("BadgeNotifications.cs");
        Tr(badgeSrc != null && badgeSrc.Contains("comp.GetAlpha"),
           "④a `BadgeWiring.Attach` 里给组件装了回读口 `comp.GetAlpha`（唯一的一处接线）");
        foreach (var f in new[] { "MainMenuSubmenuWindow.cs", "CampaignTab.cs", "ForgeTab.cs" })
        {
            string s2 = ReadShellSource(f);
            Tr(s2 != null && s2.Contains("BadgeWiring.Attach"),
               "④b `" + f + "` 的红点走 `BadgeWiring.Attach`（页签 / 阵营格两条路都收在这一口 ⇒ ③ 的修正对两面同时生效）");
        }
    }

    // ------------------------------------------------------------ 工具
    static string ReadShellSource(string file)
    {
        try
        {
            string p = System.IO.Path.Combine(Application.dataPath, "CardPresentation", "Shell", file);
            return System.IO.File.Exists(p) ? System.IO.File.ReadAllText(p) : null;
        }
        catch (Exception e) { Debug.LogError("[T1] 读源码失败：" + e.Message); return null; }
    }

    /// <summary>②f 的「另一类」定义类型：**自造的私有通知类型，全工程没有任何 provider 会发它**
    /// （⇒ `UiBadgeNotificationManager.GetCount` 对着它必数 0）。
    /// 判据：`B94_DG2_battle_menu.md` §二·#12 —— ⛔ 别再改回任何**真类型**（`CampaignBadge` / `ForgeBadge` 都有 provider）。</summary>
    sealed class B94T1NoBadge : BadgeNotification { }

    /// <summary>`ILiveOps` 夹具（同 `LiveOpsFilter` 里那份的形状）：只为 `ReleaseEventAssets` 提供 `GetID()`。</summary>
    sealed class FixtureEvent : LiveOpsEvent<LiveOpsSave, LiveOpsEventData>
    {
        public FixtureEvent(string id)
        {
            var d = new LiveOpsEventData { eventId = id, activationType = LiveOpsActivationType.Permanent };
            LoadEventData(d, new LiveOpsSave { eventId = id }, null);
        }
    }
}
