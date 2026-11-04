// RewardServicePartial_B142E54.cs — 第 ③ 处断点：**非卡奖励（资源 / 装饰）领了不到手**（B142 · 执行代理 E54 · 2026-11-04）
//
// ============================ 这条补的是什么 ============================
//  `Shell/CampaignTabPartial_C9.cs` 把 J4 的发奖循环接上了（`GrantCampaignRewards → CollectSingleReward`），
//  `Shell/RewardServicePartial_C9.cs` 把**卡**那一支接上了。但 `CollectItemRewardById` 的解析口只认
//  「货币背包 + 卡池」两档 ⇒ 战役 89 条奖励里的**货币 / 万能卡 / 装饰**全落到 `Note("rs-item:unresolved")`
//  （出声、不发）⇒ 玩家领了战役奖励，只有卡进收藏，资源与装饰一个都没到手。
//
// ============================ 判据：原版逐类型分派（两源现读）============================
//  · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/RewardService.cs:641-700`（`CollectSingleReward` 的类型分派，IL 级可读）
//  · `d:/2/tools/decomp_full/RewardService__CollectSingleReward.c`（同一段的机器码复核：三处 `typeof(...)` 比较）
//  原版五支（**互斥**，末尾统一 `item.HandleOnCollect(quantity)`，`:182`）：
//    ① `item is Currency`  ⇒ `Energy` 子类走 `EnergyContext.GrantEnergy(qty)`，其余 `InventoryManager.Add<Currency>`
//    ② `item is Points`    ⇒ 虚方法 `+0x1e8(qty)` = `Points<T>.Collect` → `IPointReceiver.CollectPoints`
//    ③ `item is PrebuiltDeck` ⇒ `CollectDeckReward`（加卡组 + 它的卡 + 卡背 + 督军）
//    ④ `item is AllianceTrophyData` ⇒ 奖杯三格（`Shell/AllianceTrophyModel.cs` 已落）
//    ⑤ **其余** ⇒ `CollectItemReward<T>(item, instanceID, qty)` ⇒ `InventoryManager.Add<T>`，**T = 物品的运行时类型**
//  ⇒ **「按类型落哪个背包」是原版的行为**，不是新设计；我们缺的只是「id → 类型」那一层
//     （本工程没有 `ObtainableItem` 那套类层次，奖励条目只有 `RewardInfo.Id` 一个字符串）。
//
// ============================ 「id → 类型」用哪个判据 ============================
//  与 C9 的卡那一支**同一个口径**：`ItemDrawer.Spec(id)`。它自己就是**从原版 SO 实读**出来的
//  （`Shell/ItemDrawer.cs` 的 `IdTypes`：`WildcardUltramarines1..4` 的 SO `m_ClassName` = `Wildcard` ·
//   `Booster Pack …` = `Everguild.LiveOps.ShopContainer` · `DT Ultramarines All(R2+)` = `DropTableItem`），
//  另加 `Wildcard<阵营><1..4>` 那条只认实读过的形状的**模式**。
//  ⛔ **不按 id 前缀推广**（铁律 2）：`Spec` 认不出的 id 一律**不猜**，落到 `Unknown` 出声。
//
// ============================ 逐类型落地（本工程的等价物）============================
//  ✅ **货币**（`Currency`）：本工程**没有登记 `Inventory<ICurrency>`**（生产货币正本 = `Shell/DailyData.cs` 的
//     `Wallet`，顶栏读数 `MainMenuRuntime.OwnedOf` 也读它 —— 两处各写一份 = 迟早不一致，所以**不新开第二个货币库**）。
//     ⇒ 落地口 = `Wallet.Grant(WalletKey, qty)`；`WalletKey` **取自 `MainMenuRuntime.CounterSpecs`**（⛔ 不另开映射表）。
//     ⚠️ `C3`（黑石）/`C5`（票据）那张表的 `WalletKey` 现在是 `null`（我们只给金/水晶/活动点记了账）
//        ⇒ 这两档**出声、不发**（如实说，⛔ 不假装发过）。
//  ✅ **万能卡**（`Wildcard`）：原版 `InventoryManager.Add<Wildcard>`；本工程数量正本是 `CardProgress`
//     （按 `army|rarity` 分桶，与「扣一张」`TrySpendWildcard` 同一份账）⇒ `CardProgress.GrantWildcards`。
//     ⚠️ 出厂值 99/档是**我们的全解锁口径**（原版由服务端发），加量仍按原版语义真实累加。
//  ✅ **装饰**（avatar / title / border）：原版 `InventoryManager.Add<装饰类型>`。本工程装饰**全解锁**
//     （用户 2026-09-17 边界「头像全解锁」；`Shell/CollectionWindow.cs` 的 `Owned only` 在单机下恒真）
//     ⇒ 玩家**本来就拥有** ⇒ **净零**，出声说明（⛔ 不假装发了一份，也⛔ 不吞成「查不到」）。
//  ⛔ **Points**：原版那是一支独立分派（`item.Collect(qty)` → `Points<T>.Collect` → `IPointReceiver.CollectPoints`）。
//     战役 89 条奖励里**一条 Points 都没有**，而本机**没有**「按 id 认得出 Points」的判据（那要 `PointsConfig` 的 SO 名册）
//     ⇒ 今天**不建这一档**（落到 `Unknown` 出声、不发）。将来出现 Points 奖励时按 `Shell/Points.cs` 那条链接。
//  ⛔ **容器 / 掉落表 / 预组卡组**：本工程**没有对应背包**
//     （没有「未开的卡包计数」：`BoosterPackOpenWindow` 是买完直接开、掉落表是我们自拟的；
//       没有卡组背包：预组卡组全部可用）。⇒ 认得出这一类、**出声说明无处可落**，**不落账为已发**。
//     ⛔ 别为此新造一个「拥有的卡包」机制（那是原版 `ShopContainerInventory` 的整套语义，超出本件）。
//
// ============================ 落在哪（零行数变化）============================
//  `Shell/Inventory.cs` 的 `CollectItemRewardById` 里，`CollectItemRewardCard(...)` 那一句**同一物理行**追加
//  `if (CollectItemRewardNonCard(id, quantity)) return;`（**落在行末 `//` 注释之前** —— 铁律 18·b / B102 那 6 次坑）。
//  该行下游钉着账的 A 落点（`Shell/Inventory.cs:2058/2092/2152`）⇒ 只能同物理行追加，⛔ 不许插行。
//
// ============================ 灭自证（断言 `Editor/B142_E54_Assert.cs`）============================
//  · 摘掉 `CollectItemRewardNonCard` 那一句 ⇒ 货币/万能卡/装饰三组的「+N」全变 0 ⇒ 红（今天改前就是这个状态）。
//  · 把 `CollectItemRewardNonCard` 改成「任何 id 都 return true」⇒ 反例组（`UM_SK_Starter` / 卡 id / 空串）红。

namespace CardPresentation
{
    public static partial class RewardService
    {
        /// <summary>奖励 id 的**本地可判定类别**（原版那一层是 `ObtainableItem` 的运行时类型；我们只有 id）。
        /// <para>⚠️ 这里**不报 `Card`** —— 卡由 `CollectItemRewardCard`（C9 那一支，`Shell/RewardServicePartial_C9.cs`）先判，
        /// 两边判据同一个取卡口 `CollectionData.Card`，重复判会得到同一个答案、没有意义。</para></summary>
        public enum RewardKind
        {
            Unknown = 0,
            Currency = 1,        // `Currency`（含 `Energy` 子类）
            Wildcard = 2,        // `Wildcard`
            Cosmetic = 4,        // avatar / title / border（原版 `CosmeticItem` 一族）
            ShopContainer = 5,   // `Everguild.LiveOps.ShopContainer`（卡包）
            DropTable = 6,       // `DropTableItem`
            PrebuiltDeck = 7,    // `PrebuiltDeck`
        }

        /// <summary>货币 code → `GameCurrency`。**三对判据 = 三份 SO 的 `code` 字段实读**
        /// （`Shell/CurrencyNames.cs` 文件头 ①：`Crystals.json` `code: C2` · `Blackstone.json` `code: C3` ·
        /// `Gacha tickets.json` `code: C5`）—— ⛔ 这里不是新判据，只是把那三条搬到发放侧。
        /// <para>⚠️ 战役奖励表只用 `C2`（8 条），`C3`/`C5` 今天 0 条命中，一起接上以免将来漏。</para></summary>
        static bool TryCurrencyTypeOf(string id, out int gameCurrency)
        {
            switch (id)
            {
                case "C2": gameCurrency = MainMenuRuntime.CurCrystals; return true;
                case "C3": gameCurrency = MainMenuRuntime.CurBlackStones; return true;
                case "C5": gameCurrency = MainMenuRuntime.CurGachaTickets; return true;
                default: gameCurrency = 0; return false;
            }
        }

        /// <summary>币种 → 顶栏那张规格表里的那一条（`Shell/MainMenuRuntime.cs` 的 `CounterSpecs` / `EnergySpec`）。
        /// **⛔ 不另开一张「code → Wallet 键」的表** —— `WalletKey` 是那张表自己的字段。</summary>
        static MainMenuRuntime.CounterSpec CurrencyCounterSpec(int currency)
        {
            if (currency == MainMenuRuntime.CurEnergy) return MainMenuRuntime.EnergySpec;
            var all = MainMenuRuntime.CounterSpecs;
            for (int i = 0; i < all.Length; i++) if (all[i] != null && all[i].Currency == currency) return all[i];
            return null;
        }

        /// <summary>这个 id 是不是一件装饰（avatar / title / border / **卡背**）。**判据 = 本机四张装饰清单**
        /// （`Resources/profile_cosmetics.json`，`Shell/ProfileData.cs` 的 `Avatars` / `Titles` / `Borders`）
        /// ＋ 卡背表 `Resources/Cardbacks.json` 的 `uniqueId`（`Core/CardbackTable.FindByUniqueId`）。
        /// <para>✅ 2026-11-04（`B142-E73`）：**卡背那一档已接上** —— 那 2 条战役奖励（`CampaignData.cs:439` 的
        /// 两个 32 位 hex id）带的是 **uniqueId**，此前 `CardbackTable` 只有按图名查的口 ⇒ 落 `rs-item:unresolved`
        /// （原句「卡背不在其中 / 没有按 uniqueId 查的口」**已过期**，铁律 5 留痕）。</para></summary>
        static bool IsCosmeticId(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (CardbackTable.FindByUniqueId(id) != null) return true;   // 卡背：判据 = `Resources/Cardbacks.json` 的 uniqueId
            var avatars = ProfileData.Avatars;
            if (avatars != null) for (int i = 0; i < avatars.Count; i++) if (avatars[i] != null && avatars[i].Id == id) return true;
            var titles = ProfileData.Titles;
            if (titles != null) for (int i = 0; i < titles.Count; i++) if (titles[i] != null && titles[i].Id == id) return true;
            var borders = ProfileData.Borders;
            if (borders != null) for (int i = 0; i < borders.Count; i++) if (borders[i] != null && borders[i].id == id) return true;
            return false;
        }

        /// <summary>`id` → <see cref="RewardKind"/>（见文件头「id → 类型用哪个判据」）。判不出的给 `Unknown`，⛔ 不猜。</summary>
        public static RewardKind RewardKindOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return RewardKind.Unknown;

            int cur;
            if (TryCurrencyTypeOf(id, out cur)) return RewardKind.Currency;

            var spec = ItemDrawer.Spec(id, null, null);
            if (spec.Kind == ItemKind.Wildcard) return RewardKind.Wildcard;
            // 下面两条字符串 = `ItemDrawer.IdTypes` 的**右值**（那是原版 SO 的 `m_ClassName` 实读表）。
            // ⚠️ 断言 `Editor/B142_E54_Assert.cs` 的 E4b/E5b **独立重读那张表**来核这两个值 —— ⛔ 不是自证。
            if (spec.Type == "Everguild.LiveOps.ShopContainer") return RewardKind.ShopContainer;
            if (spec.Type == "DropTableItem") return RewardKind.DropTable;
            if (PrebuiltDecks.ById(id) != null) return RewardKind.PrebuiltDeck;
            if (IsCosmeticId(id)) return RewardKind.Cosmetic;
            return RewardKind.Unknown;
        }

        /// <summary>`CollectItemRewardById` 的**非卡**解析口（原版 `CollectSingleReward` ③/④ 那几支；卡那一支在 C9 文件里）。
        /// <returns>true = 「这个 id 我认得出是哪一类，这一条到此为止」；false = 判不出 ⇒ 调用方落回
        /// `rs-item:unresolved`（出声、不发）。</returns></summary>
        internal static bool CollectItemRewardNonCard(string id, int quantity)
        {
            switch (RewardKindOf(id))
            {
                case RewardKind.Currency: return CollectCurrencyRewardByCode(id, quantity);
                case RewardKind.Wildcard: return CollectWildcardRewardById(id, quantity);
                case RewardKind.Cosmetic: return CollectCosmeticRewardById(id, quantity);
                case RewardKind.ShopContainer: return NoteContainerReward(id, quantity, "`Everguild.LiveOps.ShopContainer`（卡包）");
                case RewardKind.DropTable: return NoteContainerReward(id, quantity, "`DropTableItem`（掉落表）");
                case RewardKind.PrebuiltDeck: return NoteContainerReward(id, quantity, "`PrebuiltDeck`（预组卡组）");
                default: return false;
            }
        }

        /// <summary>货币那一支 = 原版 `CollectCurrencyReward`（`RewardService__CollectCurrencyReward.c:22-44`：
        /// `energy` ⇒ `EnergyContext.GrantEnergy`；其余 ⇒ `InventoryManager.Add<Currency>`）。
        /// 我们的落点是 `Wallet`（理由见文件头「逐类型落地」），⛔ 不假装有 `Inventory<ICurrency>`。</summary>
        static bool CollectCurrencyRewardByCode(string id, int quantity)
        {
            int currency;
            if (!TryCurrencyTypeOf(id, out currency)) return false;      // 不该发生（调用方已按 kind 分派）

            if (quantity <= 0)
            {
                InventoryManager.Note("rs-item:currency-nonpositive:" + id,
                    "货币奖励 `" + id + "` 的数量是 " + quantity + " ⇒ 零/负不发（原版这一档 `Add<Currency>` 加 0 也等于不发）");
                return true;
            }

            var spec = CurrencyCounterSpec(currency);
            if (spec == null || string.IsNullOrEmpty(spec.WalletKey))
            {
                InventoryManager.Note("rs-item:currency-no-wallet:" + currency,
                    "货币奖励 `" + id + "`（GameCurrency " + currency + "）认得出是货币，但本工程**没有它的记账落地**"
                    + "（`MainMenuRuntime.CounterSpecs` 里这一种的 `WalletKey` 为空）⇒ **不发**"
                    + "（原版走 `InventoryManager.Add<Currency>`；我们有 `Inventory<ICurrency>` 的那一天再接回来）");
                return true;
            }

            int before = Wallet.Of(spec.WalletKey);
            Wallet.Grant(spec.WalletKey, quantity);
            UnityEngine.Debug.Log("[RewardService] 货币奖励 `" + id + "` +" + quantity
                                + " ⇒ `Wallet[" + spec.WalletKey + "]` " + before + " → " + Wallet.Of(spec.WalletKey));
            return true;
        }

        /// <summary>万能卡那一支 = 原版 `CollectItemReward<Wildcard>` ⇒ `InventoryManager.Add<Wildcard>`。
        /// 我们的数量正本是 `CardProgress`（见文件头）。`id` 的形状由 `ItemDrawer.Spec` 判过（`Wildcard&lt;阵营&gt;&lt;1..4&gt;`）⇒
        /// 这里剥前缀、尾字符就是档位，⛔ 不做第二套解析。</summary>
        static bool CollectWildcardRewardById(string id, int quantity)
        {
            var spec = ItemDrawer.Spec(id, null, null);
            if (spec.Kind != ItemKind.Wildcard) return false;           // 不该发生（调用方已按 kind 分派）

            if (quantity <= 0)
            {
                InventoryManager.Note("rs-item:wildcard-nonpositive:" + id,
                    "万能卡奖励 `" + id + "` 的数量是 " + quantity + " ⇒ 零/负不发");
                return true;
            }

            string rest = id.Substring("Wildcard".Length);
            string army = rest.Substring(0, rest.Length - 1);           // 尾字符 = 档位（`Spec` 已保证是 '1'..'4'）
            int idx = spec.Rarity - 1;
            if (idx < 0 || idx >= CardDetailPopup.WildcardRarities.Length)
            {
                InventoryManager.Note("rs-item:wildcard-no-rarity:" + id,
                    "万能卡奖励 `" + id + "` 的档位 " + spec.Rarity + " 不在 `CardDetailPopup.WildcardRarities` 里 ⇒ 不发（⛔ 不猜一档）");
                return true;
            }

            string rarity = CardDetailPopup.WildcardRarities[idx];
            int before = CardProgress.Wildcards(army, rarity);
            if (!CardProgress.GrantWildcards(army, rarity, quantity))
            {
                InventoryManager.Note("rs-item:wildcard-save-fail:" + id,
                    "万能卡奖励 `" + id + "` 加 " + quantity + " 张**没能落盘** ⇒ 这次的发放不算数（`CardProgress.GrantWildcards` 已退回）");
                return true;
            }
            UnityEngine.Debug.Log("[RewardService] 万能卡奖励 `" + id + "` +" + quantity
                                + " ⇒ `CardProgress[" + army + "/" + rarity + "]` " + before + " → "
                                + CardProgress.Wildcards(army, rarity));
            return true;
        }

        /// <summary>装饰那一支：原版 `InventoryManager.Add&lt;装饰类型&gt;`；本工程装饰**全解锁**（见文件头）⇒ 净零。
        /// ⚠️ **必须出声**：没有这一步，日志会把它报成「查不到解析口」，读日志的人会以为「忘了做」。</summary>
        static bool CollectCosmeticRewardById(string id, int quantity)
        {
            InventoryManager.Note("rs-item:cosmetic-owned:" + id,
                "装饰类奖励 `" + id + "`（avatar / title / border / **卡背**）在本工程按**全解锁**口径**本来就拥有**"
                + "（用户 2026-09-17 边界「头像全解锁」；收藏窗的 `Owned only` 在单机下恒真；卡背同样全量可选）⇒ 发放**净零**"
                + "（原版走 `InventoryManager.Add<装饰类型>`，我们没有那套装饰背包）");
            return true;
        }

        /// <summary>容器 / 掉落表 / 预组卡组：认得出是哪一类，但本工程**没有对应背包** ⇒ 出声、不落账为已发。
        /// ⛔ 别在这里新造「拥有的卡包计数」—— 那是原版 `ShopContainerInventory` 整套语义（含开包与掉落表），超出本件。</summary>
        static bool NoteContainerReward(string id, int quantity, string typeName)
        {
            InventoryManager.Note("rs-item:no-inventory-for:" + typeName,
                "奖励 `" + id + "` 的原版类型是 " + typeName + "（数量 " + quantity + "）—— 本工程**没有这一类的背包** ⇒ **不发**。"
                + "缺口与要做的东西写在 `资料/完全复刻/报告/B142_E54.md`（⛔ 不是「原版没有」）");
            return true;
        }
    }
}
