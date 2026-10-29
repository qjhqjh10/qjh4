// ShellScene_B94S2SelfCheck.cs — 第九十四会话 · 波 1 · 执行代理 S2（日常/奖励线）的**自检断言集**。
//
// 覆盖的三条账（都落在本代理独占的文件里）：
//   · `日常_奖励:104 / :107`（`DailyRewardTrackPanel.HandlePurchase` / `<HandlePurchase>b__6_0`）——
//     🆕 **2026-10-29 修的是一个真缺陷**：`_rewardClaimed[]` 原来**两条轨共用一格**，而
//     `RewardStateOf(premium:true)` 把「普通轨已领」映射成 `Unlocked` ⇒ **Premium 抽屉可以无限领**
//     （状态永不前进，每点一次都再发一次奖励）。原版判据 = `DailyRewardItemContainer__GetCurrentState.c:18-46`
//     的两格计数（`param_3+0x10` 普通 / `*(int*)((longlong)param_3+0x14)` Premium）。
//   · `日常_奖励:508`（`MissionRewardsDisplay.Setup`）—— 把原来**休眠**的 `Setup` 接进生产路径
//     （`DailyData.LoginRewardCellList` 转调它；内部仍是 `MergeSameItem` 唯一实现）。
//   · `日常_奖励:745`（`ToastNotificationMission.Initialize`）—— `ShowMission` 现在按原版形状塞
//     `Context.Data = MissionsData` 再走 `InitializeMissionToast`（生产接线从「没有生产者」变成「有」）。
//
// 跑法（与 `LiveOpsFilterSelfCheck` 同一套形状，接线在 `Editor/ShellScene.cs` 合计行之前）：
//     Debug.Log(P + "B94S2SelfCheck 跑 " + B94S2SelfCheck.Run((ok, msg) => CheckTrue(ok, msg)) + " 条断言");
//
// 期望值一律来自**原版判据**（`.c` 两格计数 / prefab），⛔ 不取我们自己的常量当期望。
// 每族至少一条**灭自证**（改回旧写法就红），见各段注释的「🧨」。
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>S2 的断言集。返回**实际执行的断言条数**（调用方据此自证真被跑到）。</summary>
    public static class B94S2SelfCheck
    {
        public static int Run(Action<bool, string> check)
        {
            if (check == null) return 0;
            int n = 0;
            Action<bool, string> C = (ok, msg) => { n++; check(ok, msg); };

            PremiumTwoTrack(C);
            LoginRewardMergeEntry(C);
            MissionToastDataShape(C);
            return n;
        }

        // ============================================================ ① 账 104 / 107：Premium 轨「已领」必须各自记账
        //   判据：`decomp_full/DailyRewardItemContainer__GetCurrentState.c:18-46`
        //     `if (cVar4 == 2 && playerData+0x14 <= index) cVar2 = 1;`
        //     ⇒ 普通轨 Collected **且** Premium 轨这一格还没领 ⇒ `Unlocked(1)`；领过 ⇒ 保持 `Collected(2)`。
        //   `RewardsCollected = 2`（我们挑的「开机已领到第几天」）⇒ 第 0 格普通轨恒 `Collected`。
        static void PremiumTwoTrack(Action<bool, string> C)
        {
            const int D = 0;
            DailyData.ForcePremiumOwnedForTest(true);

            // 前提（硬断言：先断前提，再断结论 —— ⛔ 不用 `if (前提) Check(结论)`）
            DailyData.ForceRewardClaimedForTest(D, true);            // 普通轨该格已领
            C(DailyData.RewardStateOf(D, false) == RewardState.Collected,
              "前提：普通轨第 0 格 = Collected（`RewardsCollected = 2`）");

            DailyData.ForceRewardPremiumClaimedForTest(D, false);    // Premium 轨该格未领
            C(DailyData.RewardStateOf(D, true) == RewardState.Unlocked,
              "Premium 轨未领 ⇒ Unlocked（原版 `cVar4==2 && prem<=idx ⇒ 1`；可领）");

            // 🔴 真缺陷的判据：Premium 轨领过之后必须前进到 Collected。
            DailyData.ForceRewardPremiumClaimedForTest(D, true);
            C(DailyData.RewardStateOf(D, true) == RewardState.Collected,
              "Premium 轨已领 ⇒ Collected —— 🧨 改回「两轨共用 `_rewardClaimed[]`」⇒ 这里回 Unlocked（= 可无限领），立刻红");

            // 守卫那一半：已领的格再领必须被挡（且**不重复发奖**：`CollectReward` 在守卫前就 return，不碰 Wallet / 不开窗）。
            C(!DailyData.CollectReward(D, true), "已领的 Premium 格再领 ⇒ false（⛔ 不重复发奖）");

            // 灭自证：两轨**互相独立**（普通轨已领 ≠ Premium 已领，反之亦然）。同一格两侧同时断言，挡「两边一起改」。
            // 🔴 2026-10-29（B95_A2P）就地订正一条 α：原来作「普通轨未领 + Premium 已领 ⇒ Premium 侧 Collected」，
            //   与 `DailyRewardItemContainer__GetCurrentState.c:40-44` 相反 —— 原版 `cVar2 = cVar4`（Premium **派生**自普通轨），
            //   只有 `cVar4 == 2 (Collected)` 那一支才读 Premium 记账位 ⇒ 普通轨 `Unlocked` 时 Premium 不可能是 Collected。
            DailyData.ForceRewardClaimedForTest(2, false);
            DailyData.ForceRewardPremiumClaimedForTest(2, true);
            C(DailyData.RewardStateOf(2, false) == RewardState.Unlocked,
              "普通轨未领 ⇒ Unlocked（Premium 侧写 true 不得写脏普通轨）");
            C(DailyData.RewardStateOf(2, true) == RewardState.Unlocked,
              "普通轨未领 ⇒ Premium 侧**也只能是 Unlocked**（`.c:43` `cVar2 = cVar4` 那一支）—— 灭自证：把 Premium 做成与普通轨无关的第二条轨 ⇒ 这里回 Collected、红");
            DailyData.ForceRewardClaimedForTest(2, true);          // 普通轨已领 ⇒ 进原版唯一会读 Premium 记账位的那一支
            C(DailyData.RewardStateOf(2, true) == RewardState.Collected,
              "普通轨已领 + Premium 已领 ⇒ Collected（`.c:44` 的 `premClaimed <= index` 为假 ⇒ 保持 cVar4）");
            DailyData.ForceRewardPremiumClaimedForTest(2, false);
            C(DailyData.RewardStateOf(2, true) == RewardState.Unlocked,
              "普通轨已领 + Premium **未领** ⇒ Unlocked（`.c:44` 那一支）—— 🧨 Premium 与普通轨共用一格 ⇒ 这里回 Collected、红");

            // 还原（自检之间不靠隐式状态）
            for (int i = 0; i < DailyData.RewardDays; i++)
            {
                DailyData.ForceRewardClaimedForTest(i, false);
                DailyData.ForceRewardPremiumClaimedForTest(i, false);
            }
            DailyData.ForcePremiumOwnedForTest(false);
        }

        // ============================================================ ② 账 508：登录卡奖励归并走原版同名入口 `Setup`
        //   原版 `MissionRewardsDisplay.Setup` 的第 ③ 段 = `GroupBy(r => r.ItemId)` + 同 id 求和；
        //   这条**行为**由 `MergeSameItem`（唯一实现）给，`Setup` 只是那唯一的调用口。
        //   ⚠️ 如实标：`Setup → MergeSameItem` 是**纯转发**（行为等价）⇒ 本族断言**验的是归并结果不变**，
        //      「生产路径改走 `Setup`」这一跳只能靠它**仍存在且被引用**来间接体现（🧨 把 `Setup` 删掉/改成返回空表 ⇒ 红）。
        //   ⚠️ `LoginRewardCellList` / `Setup` 都是 `internal`（Editor 程序集访问不到）⇒ 走反射。
        static void LoginRewardMergeEntry(Action<bool, string> C)
        {
            var mr = FindType("CardPresentation.MissionRewardsDisplay");
            C(mr != null, "前提：`MissionRewardsDisplay` 类型在（原版 `Setup` 的宿主）");
            MethodInfo setup = null;
            if (mr != null)
            {
                // 🔴 **按「名称 + 参数个数」取，⛔ 别用无参 `GetMethod(名字)`** —— 一旦有人给
                //   `MissionRewardsDisplay` 加**同名重载**（例如带 `ILiveOps` 的两参版 `SetupWithExtraRewards`
                //   当初差点就叫 `Setup`），无参 `GetMethod` 会抛 `AmbiguousMatchException`，**炸掉整条
                //   `ShellScene.Run`**（本项目已因同类问题炸过整月）。
                //   也**不走 `FirstOrDefault`** —— 那会把「有歧义」静默当成「找到了」，本断言就不再挡错。
                //   ⚠️ 不按 `typeof(List<Cell>)` 精确取签名，是因为 `Cell` 是 `MissionRewardsDisplay` 里的
                //   `internal struct`，Editor 程序集**拿不到那个类型**（跨程序集）⇒ 只能用参数个数这一档。
                var cands = new List<MethodInfo>();
                foreach (var mi in mr.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    if (mi.Name == "Setup" && mi.GetParameters().Length == 1) cands.Add(mi);
                C(cands.Count == 1,
                  "前提：`MissionRewardsDisplay.Setup` **恰好一条**单参重载（现命中 " + cands.Count + " 条）—— "
                  + "按「名称 + 参数个数」取，⛔ 别用无参 `GetMethod(名字)`（加同名重载会 `AmbiguousMatchException` 炸整条 Run）");
                if (cands.Count >= 1) setup = cands[0];
            }
            C(setup != null, "前提：`MissionRewardsDisplay.Setup` 存在（原版同名入口；`DailyData.LoginRewardCellList` 现在转调它）");
            if (setup == null) return;

            var dd = FindType("CardPresentation.DailyData");
            C(dd != null, "前提：`DailyData` 类型在");
            if (dd == null) return;
            var m = dd.GetMethod("LoginRewardCellList", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            C(m != null, "前提：`DailyData.LoginRewardCellList` 存在（生产侧的归并口）");
            if (m == null) return;

            object list = null;
            try { list = m.Invoke(null, null); }
            catch (Exception e) { C(false, "`LoginRewardCellList()` 抛了：" + e.GetType().Name + " —— 🧨 归并链路坏了"); return; }
            C(list != null, "`LoginRewardCellList()` 非 null");

            var l = list as System.Collections.IEnumerable;
            C(l != null, "前提：返回的是可枚举表");
            if (l == null) return;

            int count = 0; int sum = 0; bool anyNeg = false; System.Type cellType = null;
            foreach (var cell in l)
            {
                count++;
                if (cell == null) continue;
                if (cellType == null) cellType = cell.GetType();
                var fq = cellType.GetField("Quantity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                int q = fq != null ? Convert.ToInt32(fq.GetValue(cell)) : 0;
                if (q < 0) anyNeg = true;
                sum += q;
            }
            // 🔴 2026-10-29（B95_A2P）就地订正一条 α：`MissionRewardsDisplay.Cell.Quantity` 是 **`internal`** 字段
            //   （`Shell/MissionInfoDisplays.cs:382-386`），而 `GetField(裸名)` 默认只找 `Public` ⇒ 恒返 null ⇒ 恒读 0
            //   ⇒ 「合计 > 0」那条**不管实现对错都红**。本行是**独立断言**（⛔ 不是 `if (前提) 断言` 的静默跳过写法）。
            C(cellType != null && cellType.GetField("Quantity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null,
              "前提：`MissionRewardsDisplay.Cell.Quantity` 反射读得到（它是 **`internal`** 字段 ⇒ `GetField` 必须带 `NonPublic`；不带就恒读 0 ⇒ 下一条变成假红）");
            C(count > 0, "归并后至少一格（🧨 `Setup` 改成返回空表 ⇒ 这里红）");
            C(sum > 0, "归并后数量合计 > 0（同 id 求和那一半仍在）");
            C(!anyNeg, "归并后没有负数数量（`Sum` 那一段没写成减）");
        }

        // ============================================================ ③ 账 745：任务飘窗的 `Data` 形状
        //   原版 `ToastNotificationMissionsData` 两格（`mission` / `progress`）；我们的等价 DTO 三格。
        //   ⚠️ 如实标：**正例（真弹）这一格本族不覆盖** —— `ShowNotification` 要整棵 Toast 控制器在树上，
        //      在 ShellScene 这个宿主里建它成本高且会污染后面的段；负例在 `ShellScene_B93D4SelfCheck.cs` 的
        //      `MissionToast` 已覆盖四条。这里只断「DTO 形状 + 生产入口真的塞了 Data」这两条结构前提。
        static void MissionToastDataShape(Action<bool, string> C)
        {
            var t = FindType("CardPresentation.ToastNotifications");
            C(t != null, "前提：`ToastNotifications` 类型在");
            if (t == null) return;
            var md = t.GetNestedType("MissionsData", BindingFlags.Public | BindingFlags.NonPublic);
            C(md != null, "前提：`ToastNotifications.MissionsData`（原版 `ToastNotificationMissionsData` 的等价物）");
            if (md == null) return;
            C(md.GetField("ProgressText") != null, "`MissionsData.ProgressText` 存在（原版 `progress.GetLocalizedDescription()` 那一格）");
            C(md.GetField("Progress01") != null, "`MissionsData.Progress01` 存在");
            C(md.GetField("MissionId") != null, "`MissionsData.MissionId` 存在（原版 `mission` 那一格的本地等价）");

            // 生产入口 `ShowMission` 存在且**不再**直接 `ShowNotification` —— 🧨 改回旧写法（不塞 Data）虽然这里断不到，
            // 但负例 `InitializeMissionToast(Context with Data != MissionsData)` 那条会照常红（见 B93D4 那份）。
            var sm = t.GetMethod("ShowMission", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            C(sm != null, "前提：`ShowMission(int)` 生产入口在（现在按原版形状走 `InitializeMissionToast`）");
        }

        static Type FindType(string fullName)
        {
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asms.Length; i++)
            {
                var ty = asms[i].GetType(fullName, false);
                if (ty != null) return ty;
            }
            return null;
        }
    }
}
