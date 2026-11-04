// ============================================================================ 货币名册的播种 + 「货币 → Wallet」的唯一映射
//
//  写手 `B142-E65`（2026-11-04）· 对应 `判据/B142_R18_CHK05块6.md` §二·1（🟠 商店#L55，连带 #L62）
//  ＋ `报告/B142_E54.md` §六·3（货币出现「两处读数」）。
//
// ============================ ① 本地三份货币 SO 的播种（复刻，不是自设计）============================
//  原版 `ToCurrency(GameCurrency)` = `ConfigManager.GetConfig<CurrencyConfig>().currencies.FirstOrDefault(c => c.type == 目标)`
//  （`decomp_full/CurrencyExtensions__ToCurrency.c`）。`CurrencyConfig` 是服务端下发的配置 ⇒ 客户端只有**三份货币 SO**。
//  三份 SO 逐字段现读（`d:/2/新解包资源/assets_full/…/MonoBehaviour/`）：
//    · `Crystals`      （`bundle_duplicateassetisolationso_assets_all`）：`code = C2` · `type = 10` = `GameCurrency.crystals`
//    · `Blackstone`    （`bundle_cosmeticsso_assets_all`）：            `code = C3` · `type = 20` = `GameCurrency.blackStones`
//    · `Gacha tickets` （`bundle_cosmeticsso_assets_all`）：            `code = C5` · `type = 70` = `GameCurrency.gachaTickets`
//  ⚠️ 三份 SO **没有** `maxAmount` 字段（同一份 json 里没有该键）⇒ 取 `Currency.maxAmount` 的默认 **-1**（= 不封顶；
//     这正是原版 `ICurrency.get_MaxAmount` 的默认实现，见 `Shell/Inventory.cs` 那段 `ICurrency` 注释）。
//  图标 / 显示词条那一半不在这里 —— 见 `Shell/CurrencyNames.cs` 的文件头 ① 与 ③（同一批 SO 的 `bigIcon`/`smallIcon`/`displayName`）。
//
//  🔴 **播种时机 = `CurrencyCatalog._all` 的字段初始化器**（`Shell/Inventory.cs:1697` 一行、零行数变化）。
//    为什么不用「启动时显式调一次」：那样就**多了一个可能被漏掉的调用点**（本工程已经吃过这种亏 ——
//    这条缺口本身就是因为 `Register` 一个生产调用点都没有）。字段初始化器保证「第一次碰货币时名册就已经有 C2/C3/C5」。
//    ⚠️ `CurrencyCatalog.ClearForTest()` 会把它们清掉（自检用）；需要重播时调本类的 `EnsureSeeded()`。
//
// ============================ ② 「货币 → Wallet」的唯一映射（🔶 自己设计，非原版）============================
//  🔶 自己设计（非原版）：原版 `GetOwnedCount` 读的是 `InventoryManager` 里那份**由服务器解包灌进来的** `Inventory<ICurrency>`
//     （`CurrencyExtensions__GetOwnedCount.c` 末跳）。原版已关服 ⇒ 那份背包**没有任何来源**，本工程也从未登记过它
//     （`Inventories` 的静态构造只登记 `Cards / Shop / AltArts / Wildcards`）。
//     ⇒ 本地唯一的生产货币账 = **`Wallet`**（用户 2026-09-17 / 2026-10-24 定的「资源固定 2000」就落在 `Wallet.StartAmount`，
//     而且 `Shell/DailyData.cs:2306` 与 `Shell/Inventory.cs:1952` 两处自己写着「全工程唯一发奖口 = `Wallet.Grant`」）。
//     **为什么必须自己定**：判据在远端（服务器背包），本地永远拿不到；用户已裁定「资源固定 2000」⇒ 不能另开第二个钱包。
//     **谁定的**：调度台 2026-11-04（`B142-E65` 简报：「让 `GetOwnedCount` 能读到同一账 …… ⛔ 别两处记账」）。
//
//  🔴 **键只取一处**：`MainMenuRuntime.CounterSpecs` 的 `WalletKey`（⛔ 本类不另开第二张「货币 → 键」表）。
//     读数也只转调 `MainMenuRuntime.OwnedOf` —— 它本来就被标成「原版 `CurrencyExtensions.GetOwnedCount(c)` 在我们这边的等价物」
//     （`Shell/MainMenuRuntime.cs:2169`）⇒ 顶栏与本读口**读同一账、同一张表**，这才是 E54 §6·3 那条「两处读数」的收口。
//     ⚠️ 今天 `CounterSpecs` 只给 **`campaignPoints` / `crystals` / `gold`** 配了 `WalletKey`；
//     `blackStones(C3)` / `gachaTickets(C5)` / `raidMedals` 是 `null`（我们从来没有那两路记账）
//     ⇒ 这两档 `GetOwnedCount` **与顶栏一致地给 0**（不是「查不到货币」—— 名册里查得到，见上面 `c != null` 那一跳）。
//     要真给它们记账，改的是 `CounterSpecs` 那一张表（⛔ 不是在本类里另抄一张）。
using System.Collections.Generic;

namespace CardPresentation
{
    /// <summary>货币名册的播种（本地三份 Currency SO）+ 生产货币账口（`Wallet`）的唯一映射。
    /// <para>判据与三条边界 → 本文件头 ① / ②。</para></summary>
    public static class CurrencyCatalogSeed
    {
        /// <summary>名册初值 —— `CurrencyCatalog._all` 的**字段初始化器**调这里（见文件头 ①）。
        /// <para>⚠️ 三对 `code ↔ type` 是 SO 字段实读，⛔ 别按枚举名猜（`crystals=10` / `blackStones=20` / `gachaTickets=70` 不是连号）。</para></summary>
        public static List<ICurrency> LocalDefaults()
        {
            return new List<ICurrency>
            {
                new Currency("C2", GameCurrency.crystals),
                new Currency("C3", GameCurrency.blackStones),
                new Currency("C5", GameCurrency.gachaTickets),
            };
        }

        /// <summary>幂等补种：名册里**已经查得到这一种**就不动，查不到才登记一份。
        /// <para>生产**不需要**调（字段初始化器已经播过）—— 只有自检在 `ClearForTest()` 之后要重播时用它。</para></summary>
        public static void EnsureSeeded()
        {
            List<ICurrency> d = LocalDefaults();
            for (int i = 0; i < d.Count; i++)
                if (CurrencyCatalog.Find(d[i].Type) == null) CurrencyCatalog.Register(d[i]);
        }

        /// <summary>货币 ⇒ `Wallet` 记账键。**唯一来源 = `MainMenuRuntime.CounterSpecs`**（见文件头 ②）。
        /// <para>`null` = 我们没有这一路记账（原版那一格由服务器背包提供，本地没有来源）。</para></summary>
        public static string WalletKeyOf(GameCurrency type)
        {
            MainMenuRuntime.CounterSpec[] all = MainMenuRuntime.CounterSpecs;
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].Currency == (int)type) return all[i].WalletKey;
            if (type == GameCurrency.energy) return MainMenuRuntime.EnergySpec.WalletKey;
            return null;
        }

        /// <summary>货币 ⇒ 拥有数。**只转调 `MainMenuRuntime.OwnedOf`**（顶栏同一个口，见文件头 ②）——
        /// ⛔ 别在这里再写第三份「键 → 值」的读法。</summary>
        public static int WalletOf(GameCurrency type) { return MainMenuRuntime.OwnedOf((int)type); }
    }
}
