// BoosterRewards.cs —— 奖励里的**卡包 / 掉落表**两档真正落到背包（B143 · `B142-REWARD-INVENTORY` Step 2）
//
// ============================ 这条补的是什么 ============================
//  `Shell/RewardServicePartial_B142E54.cs` 已把 `CollectSingleReward` 的类型分派接到「id → 类型」那一层，
//  但 **卡包 / 掉落表 / 预组** 三档当时只 `NoteContainerReward(...)` 出声（本工程没有对应背包）。
//  本件把**前两档**接上（预组卡组那一档仍留出声：`DeckLibrary` 那条另批做，见报告 Step 4）。
//
// ============================ 判据（原版逐条）============================
//  · 卡包 / 掉落表的落库口 = `InventoryManager.Add<T>(item, amount, instanceId)`，
//    T = 物品**运行时类型**（`cpp2il/out_cpp2il_dev_ilspy/RewardService.cs:890-946`；
//    机器码复核 `decomp_full/RewardService__CollectSingleReward.c:182` 的统一 `HandleOnCollect`）。
//  · 徽章刷新 = `ShopContainerInventory.OnItemCountModify`：
//    `if (item.GetID().Contains("Booster", StringComparison.InvariantCultureIgnoreCase)) UiBadgeNotificationManager.Refresh<BoosterPackBadge>();`
//    （`cpp2il/out_cpp2il_dev_ilspy/ShopContainerInventory.cs:111-118`，**只有卡包刷、掉落表不刷**）。
//    ⚠️ 我们**没有** `BoosterPackBadge` 这个类（原版 `BoosterPackBadge : InventoryBadge`）⇒ 退到基类
//    `Refresh(typeof(InventoryBadge))`（同一条 `IsRelated` 关系），并如实记在这里。
//  · 数量 <= 0：原版 `Add<Currency>` 加 0 也等于不发 ⇒ 我们出声、不动背包（同 `CollectCurrencyRewardByCode` 的口径）。
//
// ============================ 自己设计（非原版）============================
//  🔶 自己设计（非原版）：**落盘失败就回滚内存改动**
//     · 为什么必须自己定：原版的 `Add` 是对 PlayFab 库存的乐观写，失败由回包驱动；我们没有服务器。
//       口径照 `CardProgress.GrantWildcards`（`Shell/CardProgressPartial_B142E54.cs`）：写盘失败 ⇒ 退回内存，
//       并让这一条**不算发过**（出声）。
//     · 调度台 2026-11-05 裁定（`B143-R4` 报告）。
namespace CardPresentation
{
    public static partial class RewardService
    {
        /// <summary>卡包那一档：`InventoryManager.Add<ShopContainerBase>` 的本地等价物
        /// ⇒ `Inventories.Containers`（`Shell/BoosterInventory.cs`）。</summary>
        /// <returns>true = 「这个 id 我认得出、这条到此为止」（与 <see cref="CollectItemRewardNonCard"/> 的契约一致）。</returns>
        internal static bool GrantContainerReward(string id, int quantity)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (quantity <= 0)
            {
                InventoryManager.Note("rs-item:container-nonpositive:" + id,
                    "卡包奖励 `" + id + "` 的数量是 " + quantity + " ⇒ 零/负不发（原版 `Add<T>` 加 0 也等于不发）");
                return true;
            }

            BoosterInventoryStore.EnsureLoaded();
            BoosterContainerRow row;
            bool known = BoosterTables.TryGetContainer(id, out row) && row != null;
            // ⚠️ 不因为「表里没有」就拒发：奖励表可能引用我们尚未收录的卡包。
            //    照发 + 出声，理由同 `BoosterInventoryStore.EnsureLoaded` 的 known limitation。
            if (!known)
                InventoryManager.Note("rs-item:container-unknown:" + id,
                    "卡包奖励 `" + id + "` 在 `booster_tables.json` 里查不到（原版这份表来自远端商店目录）"
                    + " ⇒ 仍按原版语义**记进背包**，只是今天打不开它（⛔ 不静默丢掉）");

            var item = new ContainerItem(id, id);
            int before = AmountOf(Inventories.Containers, id);
            Inventories.Containers.Add(item, quantity);
            if (!BoosterInventoryStore.SaveContainers())
            {
                Inventories.Containers.Add(item, -quantity);      // 回滚（`ItemInfo.Add` 会把数量夹到 >= 0）
                InventoryManager.Note("rs-item:container-save-fail:" + id,
                    "卡包奖励 `" + id + "` 加 " + quantity + " 个**没能落盘** ⇒ 这次发放不算数（已退回内存改动）");
                return true;
            }

            UnityEngine.Debug.Log("[RewardService] 卡包奖励 `" + id + "` +" + quantity
                                + " ⇒ `Inventories.Containers[" + id + "]` " + before + " → "
                                + AmountOf(Inventories.Containers, id));

            // 原版 `ShopContainerInventory.OnItemCountModify`：**id 含 `Booster` 才刷徽章**（掉落表不刷）
            if (id.IndexOf("Booster", System.StringComparison.OrdinalIgnoreCase) >= 0)
                UiBadgeNotificationManager.Refresh(typeof(InventoryBadge));
            return true;
        }

        /// <summary>掉落表那一档：`InventoryManager.Add<DropTableItem>` 的本地等价物 ⇒ `Inventories.DropTables`。</summary>
        internal static bool GrantDropTableReward(string id, int quantity)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (quantity <= 0)
            {
                InventoryManager.Note("rs-item:droptable-nonpositive:" + id,
                    "掉落表奖励 `" + id + "` 的数量是 " + quantity + " ⇒ 零/负不发");
                return true;
            }

            BoosterInventoryStore.EnsureLoaded();
            BoosterDropTableRow row;
            bool known = BoosterTables.TryGetDropTable(id, out row) && row != null;
            if (!known)
                InventoryManager.Note("rs-item:droptable-unknown:" + id,
                    "掉落表奖励 `" + id + "` 在 `booster_tables.json` 里查不到 ⇒ 仍记进背包、今天打不开（⛔ 不静默丢掉）");

            var item = new DropTableItemRef(id, id);
            int before = AmountOf(Inventories.DropTables, id);
            Inventories.DropTables.Add(item, quantity);
            if (!BoosterInventoryStore.SaveDropTables())
            {
                Inventories.DropTables.Add(item, -quantity);
                InventoryManager.Note("rs-item:droptable-save-fail:" + id,
                    "掉落表奖励 `" + id + "` 加 " + quantity + " 张**没能落盘** ⇒ 这次发放不算数（已退回内存改动）");
                return true;
            }

            UnityEngine.Debug.Log("[RewardService] 掉落表奖励 `" + id + "` +" + quantity
                                + " ⇒ `Inventories.DropTables[" + id + "]` " + before + " → "
                                + AmountOf(Inventories.DropTables, id));
            // 原版：掉落表**不刷**徽章（`ShopContainerInventory` 那条只看 `Booster`）⇒ 这里刻意什么都不做
            return true;
        }

        static int AmountOf<T>(Inventory<T> inv, string id) where T : class
        {
            if (inv == null || string.IsNullOrEmpty(id)) return 0;
            var info = inv.Get(id);
            return info != null ? info.Amount : 0;
        }
    }
}
