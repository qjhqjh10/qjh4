// ============================================================================ 货币的显示名
//
//  原版那一跳 = `CurrencyDrawer.GetItemName(Currency item)` ⇒ `item.DisplayName` ⇒ 一个 `I2.Loc.LocalizedString`。
//  我们这一侧没有 `Currency` 这个类型（它只在服务端模型里），而 `ObtainableItem.GetItemName` 的落点 =
//  `ItemDrawer` 那颗抽屉的名字文字 ⇒ 本类补的正是**那一段文字**（`ItemSpec.Label` 的那一格）。
//
//  判据（两源一致，2026-10-28 B89_E17 现读）：
//   ① `d:/2/tools/decomp_full/CurrencyDrawer__GetItemName.c` 全文 12 行 = 空判后**直接转调**
//      `Currency__get_DisplayName(param_2)`（没有任何拼装 / 没有任何前缀）。
//   ② `d:/2/tools/decomp_full/Currency__get_DisplayName.c` = 把 SO `+0x38` 那 24 字节
//      （`I2.Loc.LocalizedString`，头一格就是 `mTerm`）交给 `I2_Loc_LocalizedString__op_Implicit`
//      ⇒ **就是一条本地化词条**，客户端不拼字符串。
//
//  本地能读到 `displayName` 的 `Currency` SO **只有三份**（`rg -l --glob "*.json" "bigIcon"` 全库 3 命中）：
//    · `bundle_duplicateassetisolationso_assets_all/MonoBehaviour/Crystals.json`     `code: C2` ⇒ `MenuShop/ShopItemType/Crystals`
//    · `bundle_cosmeticsso_assets_all/MonoBehaviour/Blackstone.json`                  `code: C3` ⇒ `MenuShop/ShopItemType/Blackstone`
//    · `bundle_cosmeticsso_assets_all/MonoBehaviour/Gacha tickets.json`               `code: C5` ⇒ `MenuShop/ShopItemType/GachaTicket`
//  词条真值在 `CardPresentation/Resources/official_loc_13lang.json`
//  （EN `Crystal` / `Blackstone` / `Ticket`；ZH「水晶」/「黑石」/「票据」）⇒ **一个字都不用编**。
//
//  ⚠️ 三条边界（别越界）：
//   ① 只有 **id 恰好等于货币 code**（`C2`/`C3`/`C5`）才命中。战役奖励表用的就是 `C2`
//      （`Shell/CampaignData.cs` 的 UM5/UM6/UM12/UM22/UM28/UM29/UM39）；商店那几件的 id 是**图名**
//      （`40k_general_icon_currency_*`）⇒ 落到 `null`、调用方照旧兜底，行为一字不变。
//   ② ⛔ **别把这条并进 `CampaignData.ItemShortName`** —— 那条同时被当作**节点名**用
//      （`CampaignRewardWindow.cs` 的 `"Item_" + ItemShortName(...)`），而 `Editor/RewardsScene.cs:9223`
//      有一条**逐字**断 `Item_Booster Pack Ultramarines|Item_WildcardUltramarines1|Item_C2|Item_DT Ultramarines All`
//      的**排序**断言 ⇒ 名字改了那条会红（而且它断的是排序，不该被名字改动牵连）。
//   ③ **图标那一半（2026-10-28 B89_E17b 补完，走的是 guid 链、不是名字约定）**：
//      `guid_map.tsv` 对这三个 GUID 0 命中 —— 原因是**那张表本身只收卡**（4,998 行，`uniqueId/cardName/
//      cardImageReference`，见其表头），**不是漏解**。真正的 guid → 资产表在 **AssetRipper 导出工程**里
//      （`.meta` 的 `guid:` 与 `.asset` 的 `m_Name` 逐字对得上，`StreamingAssets/aa/catalog.bin` 里也有这三条）。
//      三段链逐条现读（`d:/2/tools/assetripper_out/ExportedProject/Assets/`）：
//        · `MonoBehaviour/Crystals.asset`（`code C2`）`bigIcon.m_AssetGUID = 65ed84b9693e142caa3bf45bda952471`
//          → `40k_general_icon_currency_crystal.asset`（`m_Name` 逐字）
//          · small `1eaee2ed6b7e34146a92a2d886660889` → `40k_topmarquee_currency_crystal.asset`
//        · `MonoBehaviour/Blackstone.asset`（`C3`）big `e70ffaeb2b6324bfda7d25e7608a908e`
//          → `40K_general_icon_currency blackstone.asset`（⚠️ 原名里是**空格**；本工程导入时归一成下划线
//          `40K_general_icon_currency_blackstone`，那张 png 在 `Resources/Art/ui_menu/`）
//          · small `e7af3237ed78b3c44a4ad914a7bafc8d` → `40k_topmarquee_currency_blackstone`
//        · `MonoBehaviour/Gacha tickets.asset`（`C5`）big `024f9d0226abf40de8d3902412da08b2`
//          → **`40K_icon_ticket_bundle`**（不是 `…currency_ticket`！）
//          · small `a6372675b606846798904ab2a1b63a13` → `40k_topmarquee_currency_ticket`
//      为什么取 **big**：`CurrencyDrawer.Draw` 的主图 = `item.GetIcon(IconSize.Large)` ⇒ `bigIcon`
//      （同族判据早就在 `Shell/ShopData.cs` 的 A544 那一段写过，⛔ 别按 small 那一档接）。
//      三张 big 图**都在工程里**（`Resources/Art/ui_menu/`：`40k_general_icon_currency_crystal` ·
//      `40K_general_icon_currency_blackstone` · `40K_icon_ticket_bundle`）⇒ 不需要再导入。
//   ④ 仍差的一跳：原版 `CurrencyDrawer.Draw` 还会在 `icon`（那个小图标件）存在时补 `GetIcon(Small)`；
//      我们的通用 `Icon` 抽屉**只有主图 + 数量**、没有小图标槽 ⇒ 这一档没接（不影响主图）。
using System;

namespace CardPresentation
{
    /// <summary>货币 code ⇒ 官方显示名（原版 `CurrencyDrawer.GetItemName` 那一跳的落点）。
    /// <para>判据与三条边界 → 本文件头。本地没有这份 SO 的 code 返回 <c>null</c>（调用方兜底，⛔ 不猜）。</para></summary>
    internal static class CurrencyNames
    {
        /// <summary>`C2` = Crystals · `C3` = Blackstone · `C5` = Gacha tickets（三份 SO 的 `code` 字段实读）。</summary>
        public static string Of(string currencyCode)
        {
            if (string.IsNullOrEmpty(currencyCode)) return null;
            switch (currencyCode)
            {
                case "C2": return Loc.T("MenuShop/ShopItemType/Crystals");
                case "C3": return Loc.T("MenuShop/ShopItemType/Blackstone");
                case "C5": return Loc.T("MenuShop/ShopItemType/GachaTicket");
                default: return null;
            }
        }

        /// <summary>货币 code ⇒ **小图标**名（= 原版 `Currency.GetIcon(item, IconSize.Small)` ⇒ SO 的 `smallIcon`）。
        /// <para>三段 guid 链逐条现读（`assetripper_out/ExportedProject/Assets/`）：
        /// `Crystals.asset` 的 `smallIcon` `1eaee2ed6b7e34146a92a2d886660889` → `40k_topmarquee_currency_crystal` ·
        /// `Blackstone.asset` `e7af3237ed78b3c44a4ad914a7bafc8d` → `40k_topmarquee_currency_blackstone` ·
        /// `Gacha tickets.asset` `a6372675b606846798904ab2a1b63a13` → `40k_topmarquee_currency_ticket`。
        /// 三张都在 `Resources/Art/ui_menu/`（2026-10-28 现读 `ls`）⇒ 不需要导入。</para>
        /// <para>⚠️ 谁用它：只有原版 `Currency Drawer`（**基础档**）的 `ItemDrawerComponents.icon` 非空；
        /// `Icon Currency Drawer Variant`（商店那档）的 `icon` 是 `{fileID: 0}` ⇒ 那一档**没有小图标**（见 `ItemDrawerOptions.cs` 文件头）。</para></summary>
        public static string Small(string currencyCode)
        {
            if (string.IsNullOrEmpty(currencyCode)) return null;
            switch (currencyCode)
            {
                case "C2": return "40k_topmarquee_currency_crystal";
                case "C3": return "40k_topmarquee_currency_blackstone";
                case "C5": return "40k_topmarquee_currency_ticket";
                default: return null;
            }
        }

        /// <summary>货币 code ⇒ 主图名（= 原版 `Currency.GetIcon(item, IconSize.Large)` ⇒ SO 的 `bigIcon`）。
        /// <para>三段 guid 链逐条现读 → 本文件头 ③（`C2`→`40k_general_icon_currency_crystal` ·
        /// `C3`→`40K_general_icon_currency_blackstone` · `C5`→**`40K_icon_ticket_bundle`**）。
        /// 三张图都在 `Resources/Art/ui_menu/` ⇒ `CardArt.MenuUi` 一次就取到（⛔ 不需要再导入）。</para>
        /// <para>⚠️ 传进来的仍然是**货币 code**（`C2/C3/C5`），不是图名 —— 商店那几件的 id 是图名本身，
        /// 落进 `default` 返回 null、由调用方照旧兜底（行为一字不变）。</para></summary>
        public static string Icon(string currencyCode)
        {
            if (string.IsNullOrEmpty(currencyCode)) return null;
            switch (currencyCode)
            {
                case "C2": return "40k_general_icon_currency_crystal";
                case "C3": return "40K_general_icon_currency_blackstone";   // 原 SO 引用名里是空格，导入时归一成下划线
                case "C5": return "40K_icon_ticket_bundle";
                default: return null;
            }
        }
    }
}
