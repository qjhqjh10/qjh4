// BoosterInventory.cs —— **「已买未开的卡包」与「未展开的掉落表」两份背包**（B143 · `B142-REWARD-INVENTORY` Step 1）
//
// ============================ 判据（原版逐条，⛔ 别在别处再抄一份）============================
//  · 原版三类背包的真身 = `InventoryManager` 构造器里那张类型表
//    （`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/InventoryManager.cs:1686-1712`）：
//      `RawCardScript → CardInventory` · `Wildcard → WildcardInventory` ·
//      **`ShopContainerBase → ShopContainerInventory`** · **`CardDeck → DeckInventory`** · `ShopOfferData` · `AlternateArtCard`。
//    `DropTableItem` **不在表里** ⇒ 走 `CreateInventory(Type)`（`:626-736`）现造 `Inventory<DropTableItem>`。
//  · 发放口 = `InventoryManager.Add<T>(item, amount, instanceId)`（`:1372-1400`）；奖励那一跳是
//    `RewardService.CollectSingleReward` 的第 ⑤ 支（`decomp_full/RewardService__CollectSingleReward.c:182`）。
//  · 持久化 = **PlayFab 玩家库存**（`InventoryManager.DeserializeInventory`，唯一生产点
//    `PlayerDataManager__UnpackInventory.c` / `PlayerDataManager__UpdateCardCollection.c`）。关服 ⇒ 不可用。
//  · 徽章：`ShopContainerInventory.OnItemCountModify`（`cpp2il/out_cpp2il_dev_ilspy/ShopContainerInventory.cs:111-118`）
//    只在 **id 含 `Booster`** 时 `UiBadgeNotificationManager.Refresh<BoosterPackBadge>()` —— 掉落表**不刷**（照抄）。
//
// ============================ 自己设计（非原版）两处 ============================
//  🔶 自己设计（非原版）：**这两份背包的持久化落在本地文件**（`InventoryStore` → `SaveCenter`）
//     · 为什么必须自己定：原版这两份背包的唯一真源是 PlayFab 用户库存，关服后判据在远端、本地永远拿不到。
//       我们的等价物 = `InventoryStore`（`Shell/Inventory.cs:374-470`，与 `Inventories.Shop` 同一套）。
//     · 调度台 2026-11-05 裁定（`B143-R4` 报告）。
//  🔶 自己设计（非原版）：**全解锁口径下不做「重复卡 spares / 卖卡」**
//     · 原版：开包多出来的张数记 `spares`（`CardCounterDisplay.SetDuplicateCounter`，`CardCounterDisplay.cs:91-108`），
//       玩家再 `PlayerDataManager.SellSpareCards`（`PlayerDataManager.cs:13776-13796`）→ `SellBulkCard` → PlayFab 换币。
//     · 为什么不做：用户 2026-09-17 边界「卡牌全解锁」⇒ `CardProgress.Owned` 恒给足，开出的卡**永远不是新卡**，
//       spares 恒等于开出的张数、卖它也没有意义（而且卖卡本身就是服务器调用）。
//     · 落地：本层**不建 spares 计数、不接卖卡**；开包窗也不画重复标记。
//     · 调度台 2026-11-05 裁定（`B143-R4` 报告）。
using System;

namespace CardPresentation
{
    /// <summary>「已买未开的一个卡包」（原版 `ShopContainerBase` 在 `Inventory<ShopContainerBase>` 里那一条）。
    /// 只存 **key + 展示名**；内容（5 个卡位指向哪些掉落表）在 `booster_tables.json` 里。</summary>
    public sealed class ContainerItem
    {
        public string Id;
        public string Name;
        public ContainerItem() { }
        public ContainerItem(string id, string name) { Id = id; Name = name; }
        public override string ToString() { return Name ?? Id; }
    }

    /// <summary>「一张未展开的掉落表」（原版 `DropTableItem` 在 `Inventory<DropTableItem>` 里那一条）。</summary>
    public sealed class DropTableItemRef
    {
        public string Id;
        public string Name;
        public DropTableItemRef() { }
        public DropTableItemRef(string id, string name) { Id = id; Name = name; }
        public override string ToString() { return Name ?? Id; }
    }

    public static partial class Inventories
    {
        /// <summary>原版 `Inventory<ShopContainerBase>`（`ShopContainerInventory`）。
        /// ⚠️ 与 <see cref="Shop"/>（`Inventory<ShopItem>` = **商店货架那一格的限购计数**）是**两份不同的账**，⛔ 别合并。</summary>
        public static readonly Inventory<ContainerItem> Containers =
            new Inventory<ContainerItem>(it => it != null ? it.Id : null);

        /// <summary>原版 `Inventory<DropTableItem>`（反射现造的通用背包，`InventoryManager.cs:626-736`）。</summary>
        public static readonly Inventory<DropTableItemRef> DropTables =
            new Inventory<DropTableItemRef>(it => it != null ? it.Id : null);
    }

    /// <summary>两份背包的**装载 / 落盘**门面（原版没有这一层：它由 PlayFab 直接灌 `DeserializeInventory`）。</summary>
    public static class BoosterInventoryStore
    {
        /// <summary>卡包背包的存档名（→ `InventoryStore` 的 `<name>.json`）。</summary>
        public const string ContainersName = "containers";
        /// <summary>掉落表背包的存档名。</summary>
        public const string DropTablesName = "droptables";

        static bool _registered;
        static bool _loaded;

        /// <summary>把两份背包登记进 `InventoryManager`（原版靠反射扫全类型；我们**显式登记**，同 `Inventories` 其余四份）。
        /// 幂等（`InventoryManager.Register` 重复登记 = 后者覆盖前者，见 `Shell/Inventory.cs:900-916`）。</summary>
        public static void EnsureRegistered()
        {
            if (_registered) return;
            InventoryManager.Register(Inventories.Containers);
            InventoryManager.Register(Inventories.DropTables);
            _registered = true;
        }

        /// <summary>建条目（数量 0）+ 读盘。**幂等**（第二次起直接返回）。</summary>
        /// <remarks>
        /// ⚠️ 必须先 `Create` 出条目再 `Load` —— `InventoryStore.Load` 的契约是
        /// 「已在的改、不在的跳过」（`Shell/Inventory.cs:455-470`），条目不存在的话盘上那个数量会被丢掉。
        /// ⚠️ 盘上若出现**表里没有的 id**（原版新卡包 / 退役卡包），`Load` 会跳过它 —— 已知限制，
        ///    见 `判据/B143_奖励背包与待裁四项.md` 的「还差什么」。
        /// </remarks>
        public static void EnsureLoaded()
        {
            EnsureRegistered();
            if (_loaded) return;
            _loaded = true;

            BoosterTables.EnsureLoaded();
            var cs = BoosterTables.Containers;
            if (cs != null)
                for (int i = 0; i < cs.Length; i++)
                {
                    var r = cs[i];
                    if (r == null || string.IsNullOrEmpty(r.id)) continue;
                    Inventories.Containers.Create(new ContainerItem(r.id, r.id));
                }
            var ds = BoosterTables.DropTables;
            if (ds != null)
                for (int i = 0; i < ds.Length; i++)
                {
                    var r = ds[i];
                    if (r == null || string.IsNullOrEmpty(r.id)) continue;
                    Inventories.DropTables.Create(new DropTableItemRef(r.id, r.id));
                }

            InventoryStore.Load(ContainersName, Inventories.Containers);
            InventoryStore.Load(DropTablesName, Inventories.DropTables);
        }

        /// <summary>卡包背包落盘。返回 true = 成功（或落盘门关着 —— 批处理 / 自检不算失败）。</summary>
        public static bool SaveContainers()
        {
            bool ok = InventoryStore.Save(ContainersName, Inventories.Containers);
            if (!ok && InventoryStore.StoreOn)
                InventoryManager.Note("save-containers-fail", "卡包背包**没能落盘**：" + (InventoryStore.LastStoreError ?? "未知原因"));
            return ok || !InventoryStore.StoreOn;
        }

        /// <summary>掉落表背包落盘。返回 true = 成功（或落盘门关着）。</summary>
        public static bool SaveDropTables()
        {
            bool ok = InventoryStore.Save(DropTablesName, Inventories.DropTables);
            if (!ok && InventoryStore.StoreOn)
                InventoryManager.Note("save-droptables-fail", "掉落表背包**没能落盘**：" + (InventoryStore.LastStoreError ?? "未知原因"));
            return ok || !InventoryStore.StoreOn;
        }

        /// <summary>自检收尾：清空两份背包（**不落盘**；要连盘一起清用 `InventoryStore.Delete`）。
        /// 同时把 `<c>_loaded</c>` 复位 —— 否则下一次 `EnsureLoaded` 不再补建表里的条目（自检之间会互相影响）。</summary>
        public static void ResetForTest()
        {
            Inventories.Containers.ClearForTest();
            Inventories.DropTables.ClearForTest();
            _loaded = false;
        }
    }
}
