// CampaignTabPartial_C9.cs — 第 ①③ 处断点：**领取那一刻「一条奖励都没发」**（第一百零九会话 · 执行代理 `C9`）
//
// ============================ 这条补的是什么 ============================
//  原版 11 跳（判据逐跳见 `资料/完全复刻/判据/B109_R22_卡奖励收藏链.md` §1）：
//    J1 `CampaignWindowTab.TryCollect` → J2 `Campaign.CollectRewards`（**CloudScript 0x372 ⇒ 服务端**）
//    → J3 `OnSuccess` → **J4 `RewardService.Collect`**：`for each reward → CollectSingleReward(reward)`（**发奖就这一步**，
//      与开不开窗无关，`RewardService__Collect.c:117`）→ ⑤ `showAnimation=1` 才开 `RewardWindow`（`:202`）
//    → J5..J8 `CollectSingleReward → CollectItemReward<T> → InventoryManager.Add<T> → CardInventory.Add`
//
//  我们这一侧 `ClaimForTest` 只做了 `Claim`（= 服务端节点状态的本地等价）+ `ShowCollected`（= 开窗那一半）
//  ⇒ **J4 的发奖循环一个字都没有** ⇒ `RewardService.CollectSingleReward`（`Shell/Inventory.cs:2058`）
//     在全仓**零个生产调用点**（只有自检调）⇒ 「领到的卡进不了收藏」。
//  `SetFirstTutorialTrackArmyNodeDone`（J11）同病：原版末尾有 `PlayfabWrapper.CollectFirstNodeFix`
//  （CloudScript `0x45e`）**替玩家补发首节点奖励**，我们只翻了个标记。
//
// ============================ 🔶 服务端边界（铁律 18·0）============================
//  原版这两条链的「发哪几条」由**服务端回包**给（`CollectedCampaignRewardsResult.rewards` / `0x45e`）。
//  本地等价物 = `CampaignData.RewardsOf(i, tier)`（生成器从 `Campaign Node Data*.json` 的 `Rewards[]` 按 `rewardTier`
//  过滤 —— 与原版 `CampaignRewardsWindow.Open` 那个谓词同一口径）。
//  ⇒ **只有「服务端返回哪几条」这一格是自设计**；**发奖本身（J5-J8）是纯客户端代码，照原版做**。
//
// ============================ 次序（照原版，⛔ 别倒）============================
//  `Collect` 里**先发奖、后开窗**（`:117` 的 for 循环在 `:202` 的开窗之前；`showAnimation=0` 那一支更是只发不开）。
//  ⇒ 调用点必须是 `GrantCampaignRewards(...)` **在前**、`RewardWindow.ShowCollected(...)` **在后**。
//  节点状态（`CampaignData.Claim`）现已**晚于**两者 —— `Claim` 跑在领奖窗的关窗回调 `FinishClaim()` 里（`Shell/CampaignTab.cs:1339-1347`），与原版「节点状态在 `onClose`」一致。（2026-11-01 `B-89` 订正：原句写「仍早于两者」，那是 `B-15` 修复前的状态。）
using UnityEngine;

namespace CardPresentation
{
    public partial class CampaignTab
    {
        // 🔶 自己设计（非原版）：本地模拟发奖 · 原版走 CloudScript 0x372（关服）· 调度台 2026-10-31 裁定
        //   —— 自设计的是「服务端返回哪几条」这一格（`CampaignData.RewardsOf`）；下面的逐条发放是 J5-J8 的照抄。
        /// <summary>原版 `RewardService.Collect(rewards, showAnimation:1, …)` 里**发奖那一半**（`RewardService__Collect.c:117`）。
        /// 逐条走 <see cref="RewardService.CollectSingleReward"/>，与开窗无关 —— 调用点负责在它之后再开窗。</summary>
        internal void GrantCampaignRewards(int i, int tier)
        {
            CampaignData.RewardSpec[] rewards = CampaignData.RewardsOf(i, tier);
            if (rewards == null || rewards.Length == 0) return;
            for (int k = 0; k < rewards.Length; k++)
            {
                // 与「服务端回包 → `CollectSingleReward`」同一形状：原版拿到的就是 `RewardInfo` 列表。
                RewardService.CollectSingleReward(RewardInfo.FromSpec(rewards[k]));
            }
        }
    }
}
