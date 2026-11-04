// BoosterOpenOwned.cs —— **从背包里开一个已拥有的卡包 / 掉落表**（B143 · `B142-REWARD-INVENTORY` Step 3 的**数据层**）
//
// ============================ 原版链（本轮实读，结论与「购买改进背包」相反）============================
//  ① **商店买卡包 = 买完【立刻开】**，中间没有「先进背包、以后再说」这条路：
//     `ShopContainer.__OnAfterPurchase`（`decomp_full/Everguild.LiveOps.ShopContainer__OnAfterPurchase.c:73-83`）、
//     `ShopContainerV2.__OnAfterPurchase`（`…ShopContainerV2__OnAfterPurchase.c:78-88`）、
//     `DropTableContainer__OnAfterPurchase`（`…DropTableContainer__OnAfterPurchase.c:40-48`）
//     —— 三个容器族**同一形状**：买到 → `ContainerService.OpenContainer(...)`（`ContainerService__OpenContainer.c:64-69`）
//     → 打开包窗。⇒ **购买链不需要改成「先进背包」**（改成那样反而是偏离原版）。
//  ② 「已拥有的容器」这条路**另有入口**：`ShopOfferContainer.OpenContainer`（`decomp_full/ShopOfferContainer__OpenContainer.c:17-23`）
//     —— 先问那张商品卡的 offer 能不能开（槽 `0x248`，= 「这个 `instanceId` 我有没有」），能开就
//     `WindowsManager.OpenWindow(BoosterPackOpenWindow, …)` 并把 `instanceId`（`this+0x90`）塞进去；
//     不能开才退到 `PurchaseableOfferContainer.OnClick`（= 购买）。
//     ⇒ 原版对「奖励发来的卡包」的入口 = **在商店卡上显示「Open (n)」，点它开一个实例**（服务端消耗那个 ItemInstance）。
//  ③ 我们的等价物 = 本文件：`TryOpen(containerId, seed, out cards, out reason)` —— 扣 1 + 摇 5 张。
//     「扣 1」对应原版 `UnlockContainer`（云函数 `0xf1`，`PlayfabWrapper__UnlockContainer.c:40-42`）消耗实例；
//     「摇」在 `BoosterTables.RollPackIds`（权重/卡位照抄 SO，只有随机数序列是我们定的 —— 见那个文件的 `🔶`）。
//
// ============================ 为什么单独一个文件 ============================
//  UI 那两处（`Shell/ShopTabPage.cs` 的 `IsOwnedCell` / `OpenOwnedOffer`、`Shell/BoosterInfoPopup.cs` 的 `Buy`）
//  **不在本批白名单**（`ShopWindow.cs` 里没有购买代码）⇒ 本文件只提供**可以直接调的一口**，
//  补丁（4 行）写在 `资料/完全复刻/判据/B143_奖励背包与待裁四项.md` §6·7，由调度台转派。
//
// ============================ 自己设计（非原版）============================
//  🔶 自己设计（非原版）：**「扣一个实例」的落点**
//     · 原版是 PlayFab `UnlockContainer`（云函数 `0xf1`）在服务端消耗 `ItemInstance`；关服 ⇒ 本地没有这一跳。
//       我们的等价物 = 从 `Inventories.Containers` / `.DropTables` 扣 1 并落盘（失败**回滚**，同 `BoosterRewards`）。
//     · 调度台 2026-11-05 裁定（`B143-R4` 报告）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>从背包里开**已拥有**的卡包 / 掉落表（原版 `ShopOfferContainer.OpenContainer` 的本地等价物）。</summary>
    public static class BoosterOpenOwned
    {
        /// <summary>这个卡包现在有几个（= `Inventories.Containers` 的数量）。</summary>
        public static int OwnedCount(string containerId)
        {
            if (string.IsNullOrEmpty(containerId)) return 0;
            BoosterInventoryStore.EnsureLoaded();
            var info = Inventories.Containers.Get(containerId);
            return info != null ? info.Amount : 0;
        }

        /// <summary>这张掉落表现有几张。</summary>
        public static int OwnedDropTableCount(string dropTableId)
        {
            if (string.IsNullOrEmpty(dropTableId)) return 0;
            BoosterInventoryStore.EnsureLoaded();
            var info = Inventories.DropTables.Get(dropTableId);
            return info != null ? info.Amount : 0;
        }

        /// <summary>开一个**已拥有**的卡包：扣 1 → 按 5 个卡位摇出卡 id。
        /// <para>返回 false 时 <paramref name="reason"/> **一定非空**（红线：不许静默失败）。</para></summary>
        /// <param name="seed">`System.Random` 的种子（可复现；调用方一般传 `Time.frameCount` 之类）。</param>
        public static bool TryOpen(string containerId, int seed, out string[] cardIds, out string reason)
        {
            cardIds = null;
            reason = null;
            if (string.IsNullOrEmpty(containerId)) { reason = "containerId 为空"; return false; }

            int owned = OwnedCount(containerId);
            if (owned <= 0)
            {
                reason = "`" + containerId + "` 现在**一个都没有**（原版这一档会让 `ShopOfferContainer` 退回购买）";
                return false;
            }

            // ① 扣 1（原版 = `UnlockContainer` 消耗那个 ItemInstance）
            var item = new ContainerItem(containerId, containerId);
            Inventories.Containers.Add(item, -1);
            if (!BoosterInventoryStore.SaveContainers())
            {
                Inventories.Containers.Add(item, 1);          // 回滚
                reason = "扣减 `" + containerId + "` **没能落盘** ⇒ 这次开包不算数（已退回）";
                return false;
            }

            // ② 摇（原版 = 服务端按 dropTable 展开）
            var ids = BoosterTables.RollPackIds(containerId, 0, seed);
            if (ids == null || ids.Length == 0)
            {
                Inventories.Containers.Add(item, 1);          // 回滚：一包都摇不出来就别扣
                // 🔴 2026-11-05（B143-R4 复核）：这里是**内存 +1 / 盘 −1 的唯一路径** —— 前面那次扣减已经落盘成功，
                //    回滚这次若再落盘失败，盘上就停在「少 1 个」而内存是「没扣」。**必须出声**（⛔ 不许静默）。
                if (!BoosterInventoryStore.SaveContainers())
                {
                    RollbackSaveFailCount++;
                    Debug.LogWarning("[BoosterOpenOwned] ⚠️ 回滚 `" + containerId + "` 的**落盘也失败**："
                                   + "内存已退回（没扣），但盘上仍是扣减后的值（" + (InventoryStore.LastStoreError ?? "未知原因")
                                   + "）⇒ 下次启动会少 1 个。这是**内存与盘不一致**的唯一路径，已记 `RollbackSaveFailCount`");
                    reason = "`" + containerId + "` 的卡位摇不出任何卡（`booster_tables.json` 缺它 / 卡位引用坏的掉落表）"
                           + " ⇒ 已回滚内存，**但回滚落盘失败**：盘上仍停在扣减后的值（" + (InventoryStore.LastStoreError ?? "未知") + "）";
                    return false;
                }
                reason = "`" + containerId + "` 的卡位摇不出任何卡（`booster_tables.json` 缺它 / 卡位引用坏的掉落表）⇒ 已退回";
                return false;
            }
            cardIds = ids;
            Debug.Log("[BoosterOpenOwned] 开包 `" + containerId + "` ⇒ 扣 1（" + owned + " → " + OwnedCount(containerId)
                    + "），摇出 " + ids.Length + " 格");
            return true;
        }

        /// <summary>展开**一张**已拥有的掉落表：扣 1 → 摇出一张卡 id（原版 `DropTableContainer` / `DT` 那一族）。</summary>
        public static bool TryOpenDropTable(string dropTableId, System.Random rnd, out string cardId, out string reason)
        {
            cardId = null;
            reason = null;
            if (string.IsNullOrEmpty(dropTableId)) { reason = "dropTableId 为空"; return false; }
            if (rnd == null) { reason = "`rnd` 为 null（调用方必须给一个可复现的 `System.Random`）"; return false; }

            int owned = OwnedDropTableCount(dropTableId);
            if (owned <= 0)
            {
                reason = "掉落表 `" + dropTableId + "` 现在**一张都没有**";
                return false;
            }

            var item = new DropTableItemRef(dropTableId, dropTableId);
            Inventories.DropTables.Add(item, -1);
            if (!BoosterInventoryStore.SaveDropTables())
            {
                Inventories.DropTables.Add(item, 1);
                reason = "扣减掉落表 `" + dropTableId + "` **没能落盘** ⇒ 这次展开不算数（已退回）";
                return false;
            }

            string got = BoosterTables.RollDropTable(dropTableId, rnd);
            if (string.IsNullOrEmpty(got))
            {
                Inventories.DropTables.Add(item, 1);
                // 🔴 同上：扣减已落盘、回滚再落盘失败 ⇒ 内存 +1 / 盘 −1，必须出声（B143-R4 复核第 1 条）
                if (!BoosterInventoryStore.SaveDropTables())
                {
                    RollbackSaveFailCount++;
                    Debug.LogWarning("[BoosterOpenOwned] ⚠️ 回滚掉落表 `" + dropTableId + "` 的**落盘也失败**："
                                   + "内存已退回，盘上仍是扣减后的值（" + (InventoryStore.LastStoreError ?? "未知原因")
                                   + "）⇒ 下次启动会少 1 张。已记 `RollbackSaveFailCount`");
                    reason = "掉落表 `" + dropTableId + "` 摇不出卡（表缺 / 候选为空）⇒ 已回滚内存，"
                           + "**但回滚落盘失败**：盘上仍停在扣减后的值（" + (InventoryStore.LastStoreError ?? "未知") + "）";
                    return false;
                }
                reason = "掉落表 `" + dropTableId + "` 摇不出卡（表缺 / 候选为空）⇒ 已退回";
                return false;
            }
            cardId = got;
            Debug.Log("[BoosterOpenOwned] 展开掉落表 `" + dropTableId + "` ⇒ 扣 1（" + owned + " → "
                    + OwnedDropTableCount(dropTableId) + "），得到 `" + got + "`");
            return true;
        }

        // ============================================================ 与商店格挂钩的三个口（B143 · Step 3 UI 前置）

        /// <summary>这一格商店报价卖的容器 `uniqueId`（`null` = 本地没有对应容器 SO）。
        /// <para>数据来自 `ShopOffer.ContainerId`（`Shell/ShopData.cs`；🔶 自己设计，理由见那一行上方的标）。</para></summary>
        public static string ContainerIdOf(int pageIndex, int index)
        {
            var offers = ShopData.Offers(pageIndex);
            if (offers == null || index < 0 || index >= offers.Length) return null;
            return offers[index].ContainerId;
        }

        /// <summary>「未映射」出声过几次（自检读口；键去重 ⇒ 每个格子最多 1 次）。</summary>
        public static int UnmappedNoteCount { get; private set; }

        /// <summary>**回滚时落盘也失败**的次数（= 内存与盘不一致的唯一路径；⛔ 出现就说明盘上少了一个）。
        /// <para>🆕 2026-11-05（B143-R4 复核第 1 条）：这个计数器让「无声的数据损坏」变成**可观测**。</para></summary>
        public static int RollbackSaveFailCount { get; private set; }

        /// <summary>自检收尾：把 <see cref="UnmappedNoteCount"/> 清零。</summary>
        public static void ResetUnmappedNotesForTest() { UnmappedNoteCount = 0; }

        /// <summary>自检收尾：把 <see cref="RollbackSaveFailCount"/> 清零。</summary>
        public static void ResetRollbackSaveFailForTest() { RollbackSaveFailCount = 0; }

        /// <summary>这一格现在能不能走「已拥有 ⇒ 开」。</summary>
        public static bool CanOpenOwned(int pageIndex, int index)
        {
            string cid = ContainerIdOf(pageIndex, index);
            return !string.IsNullOrEmpty(cid) && OwnedCount(cid) > 0;
        }

        /// <summary>**只摇不扣**：按容器摇一包（供「先确认能解出卡、再真扣」的两步式用）。
        /// <returns>false = 这一格不该走「开已拥有」（没有容器 id / 背包里没有 / 摇不出），
        /// <paramref name="reason"/> 为 `null` 表示**正常回退**（例如刚买完、背包里本来就没有）。</returns></summary>
        public static bool TryRollOwned(string containerId, int seed, out string[] cardIds, out string reason)
        {
            cardIds = null; reason = null;
            if (string.IsNullOrEmpty(containerId)) return false;
            if (OwnedCount(containerId) <= 0) return false;          // 正常回退：没有存货 ⇒ 走原本「买完即开」
            var ids = BoosterTables.RollPackIds(containerId, 0, seed);
            if (ids == null || ids.Length == 0)
            {
                reason = "`" + containerId + "` 的卡位摇不出卡（`booster_tables.json` 缺它 / 卡位引用坏的掉落表）";
                return false;
            }
            cardIds = ids;
            return true;
        }

        /// <summary>**只扣**一个（<see cref="TryRollOwned"/> 之后调；失败会出声并返回 false）。</summary>
        public static bool TryConsumeOwned(string containerId, out string reason)
        {
            reason = null;
            if (string.IsNullOrEmpty(containerId)) { reason = "containerId 为空"; return false; }
            if (OwnedCount(containerId) <= 0) { reason = "`" + containerId + "` 已经没有存货了"; return false; }
            var item = new ContainerItem(containerId, containerId);
            Inventories.Containers.Add(item, -1);
            if (!BoosterInventoryStore.SaveContainers())
            {
                Inventories.Containers.Add(item, 1);
                reason = "扣减 `" + containerId + "` **没能落盘** ⇒ 这次开包不算数（已退回）";
                return false;
            }
            return true;
        }

        /// <summary>开包窗那一跳：**已拥有就开背包里的**、否则返回 false 让调用方走原来的 `RollPack`。
        /// <para>两步式（先摇→再解卡→再扣）：任何一步失败都**不扣**，保证「开了就一定拿到 5 张」。</para>
        /// <para>⚠️ `containerId == null`（本地没有该族的容器 SO）⇒ 出声一次并返回 false（⛔ 不静默）。</para></summary>
        public static bool TryTakeOwnedCards(string containerId, int seed, out RuleEngine.CardDef[] cards, out string reason)
        {
            cards = null; reason = null;
            if (string.IsNullOrEmpty(containerId))
            {
                InventoryManager.Note("booster-open:no-container-id",
                    "这一格商店报价**没有容器 `uniqueId`**（本地没有对应容器 SO：Sautekh / Leviathan / VIP 三格）"
                    + " ⇒ 「已拥有 ⇒ 开」这一档不适用，仍按原版「买完即开」走 `RollPack`（如实说一次，⛔ 不静默）");
                UnmappedNoteCount++;
                return false;
            }

            string[] ids;
            if (!TryRollOwned(containerId, seed, out ids, out reason)) return false;

            var defs = new RuleEngine.CardDef[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                defs[i] = CollectionData.Card(ids[i]);
                if (defs[i] == null)
                {
                    // 一包都解不出来就别扣：原版这一档是服务端直接给 RewardInfo，不会出现「卡 id 认不出」
                    reason = "容器 `" + containerId + "` 摇出了卡池里没有的 id `" + ids[i] + "` ⇒ 走原路 `RollPack`（不扣）";
                    Debug.LogWarning("[BoosterOpenOwned] " + reason);
                    return false;
                }
            }

            if (!TryConsumeOwned(containerId, out reason))
            {
                Debug.LogWarning("[BoosterOpenOwned] `" + containerId + "` 扣减失败 ⇒ 走原路 `RollPack`：" + reason);
                return false;
            }
            cards = defs;
            return true;
        }
    }
}
