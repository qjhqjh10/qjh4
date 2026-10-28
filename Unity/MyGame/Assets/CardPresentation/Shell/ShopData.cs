// ShopData.cs — 商店的**本地数据源**
//
// ============================ 这一层为什么全是「我们挑的」 ============================
// 原版的商品是**服务端给的三件事**：
//   ① `ShopWindow.SetupTabs` 按**存档里的 store 列表**逐个建页签（`ShopWindow__SetupTabs.c`）；
//   ② 每个 store 有哪些报价、报什么价、拥有几张 —— 全在 PlayFab 的 store / inventory 里；
//   ③ 哪些能买（`Available: 1/5` 那种限购计数）也在服务端。
// 本地能拿到的**只有界面骨架与模板**（三个页签 prefab + `Catalog Item Shop Container` 模板 + 图标）。
// ⇒ 按用户 2026-09-17 的边界②（**不做真实经济**：资源固定 2000（用户 2026-10-24 从 9999 改）、商店与卡包照常能买、全解锁），
//    下面这张表是**我们编的**，逐条标明；**别当成原版数字**。
//
// ⚠️ **价格与「拥有数」都是我们挑的**：原版价格是本地化词条 + 服务端数字（`1800` / `300,00` 那种），
//    拥有数走 `CatalogItemContainer.ownedCount`。这里给的是**能让人把界面点通**的一组值。
using System.Collections.Generic;
using UnityEngine;      // 🆕 2026-10-25：拥有数落盘要用 `JsonUtility` / `Debug` / `Application.persistentDataPath`

namespace CardPresentation
{
    /// <summary>商店的一件商品。**字段名照原版 `CatalogItemContainer` 用到的那些概念**
    /// （`availableCount` / `ownedCount` / `priceDisplayButton`），值是我们编的。</summary>
    public struct ShopOffer
    {
        /// <summary>商品名（原版是本地化词条 `name`）。</summary>
        public string Name;
        /// <summary>副标题 / 类型行（原版 `type` 那行，例：`Booster Pack`）。</summary>
        public string Type;
        /// <summary>**商品主图**的 sprite 名（原版 `background` 槽运行期赋 `40K_shop_offer_bg_*`）。
        /// 传 `null` = 只画 `UI_Deck_Selection_Back_simple` 底板（图没导到时也是这个效果，且会出声）。</summary>
        public string Art;
        /// <summary>价格（画在 `Price Display Button` 上）。**我们挑的**。</summary>
        public string Price;
        /// <summary>拥有数（`Counter` 上的 `x14`）。**我们挑的**。</summary>
        public int Owned;
        /// <summary>限购计数（`Available Counter` 的 `Available: 1/5`）。0 = 这一件没有限购信息（该件出厂就是有的，我们按数据给）。</summary>
        public int Available, AvailableMax;
        /// <summary>有没有「限时」标（`TimedOffer`：`Limited time offer!!` + 倒计时）。</summary>
        public bool Timed;
        /// <summary>`New` 标上的折扣串（`-30%`）；空 = 不显示。</summary>
        public string NewDiscount;
        /// <summary>🆕 2026-10-03：**稀有度**（原版 `CatalogItemContainer.Item.Rarity`，**4 = legendary**）。
        /// 它驱动那条**传奇重复购买确认框**（判据 → `CatalogItemContainer__TryPurchase.c`：
        /// `Item.Rarity == 4 &amp;&amp; GetOwnedCount(Item) == 1` ⇒ 先弹 `MenuShop/ExtraLegendaryWarning`）。
        /// 🔴 **我们的商品表里本来没有这一档**（原版从服务端的 item 拿）⇒ **这里的值是我们挑的**，
        /// 只为把那条链跑通（0 = 不走确认）。</summary>
        public int Rarity;

        /// <summary>🆕 **2026-10-12（A439）：买到手之后画进【领奖窗】的那几条奖励**
        /// （对位 = 原版 `ShopOfferDataV2.offerItems` 那个 `RewardInfo[]`；我们的类型 = `CampaignData.RewardSpec`）。
        /// <para>**形状**照原版 —— 本地有 4 份 `ShopOfferDataV2` SO 可作模板
        /// （`d:/2/新解包资源/assets_full/bundle_cosmeticsso_assets_all/MonoBehaviour/`
        /// `{Expansion Orks1,Expansion UM1,Release Emperors Children,Release Space Wolves} Premium Item Offer.json`
        /// —— 逐张实读 `showRewardOnPurchase: 1` + `offerItems[]{item.targetId, quantity}`；
        /// ⚠️ 那四份全是**过期活动**，只当**形状判据**）。**值是我们挑的**（本店三页的 store/offer SO
        /// 本地一个都没导出 —— 见文件头 §九，别再重查），逐条的 id 来源写在 `_daily` / `_items` 那两段。</para>
        /// <para>⚠️ **只有「商品档」才有意义**：容器档（`Type == "Booster Pack"`，卡包那四件）原版就
        /// **不开**领奖窗（`ContainerOfferData.ShowRewardOnPurchase` 反编译出来是**常量 false**）⇒ 那四件
        /// **故意留 `null`**（填了没有任何消费点 = 死数据）。`null` 的另一个来源 = **越界下标**。</para>
        /// <para>读它的**唯一**入口 = `ShopData.GrantsOf`（⛔ 别在这儿用 cref 交叉引用 —— `Grants` 在 `ShopOffer` 里、那个口在 `ShopData` 上，跨类的 cref 解不出来、只会多一条 CS1574）；**开窗不在这一层**（在 `Shell/ShopWindow.DoBuy`）。</para></summary>
        public CampaignData.RewardSpec[] Grants;

        public ShopOffer(string name, string type, string art, string price, int owned,
                         int avail, int availMax, bool timed, string newDiscount, int rarity = 0)
        {
            Name = name; Type = type; Art = art; Price = price; Owned = owned;
            Available = avail; AvailableMax = availMax; Timed = timed; NewDiscount = newDiscount;
            Rarity = rarity; Grants = null;      // struct 的 ctor 必须把**每个**字段都赋上
            StrikeThrough = false; Free = false;
        }

        /// <summary>🆕 **2026-10-25（`C` 商店域）**：价格文本要不要打**删除线**
        /// （原版 `PriceDisplay.useStrikeThrough` 那个**序列化 bool**：为真时 `Setup(string)` 会
        /// `text.Insert(0, "<s>")` —— 判据 `PriceDisplay.cs:20-50,88-100`，TMP 的 `<s>` 是**开关式**、
        /// 原版**只插开头那一个标签**、没有闭合标签 ⇒ 我们照抄）。
        /// <para>⚠️ **今天的商品表里没有一件设它**（我们没有「原价 + 折扣价」两行那种展示）——
        /// 这一档是**组件补齐**（铁律 11：差异都要做，只有先后），数据侧谁能用上就打开它。</para></summary>
        public bool StrikeThrough;

        /// <summary>🆕 同上：这一件是**免费** ⇒ 价格文本走 `Loc.T("MainMenu/Free")`
        /// （原版 `PriceDisplay.SetupFree`，`PRICE_FREE = "MainMenu/Free"`，判据 `PriceDisplay.cs:70-78`）。
        /// ⚠️ 今天同样没有一件设它（我们店里的商品都有价）。</summary>
        public bool Free;
    }

    /// <summary>商店的**本地**状态。纯内存 + 可复现。</summary>
    public static class ShopData
    {
        // ============================================================ 三个页签
        //
        // 🔴 **页签表是我们定的** —— 2026-09-23 已把「本地到底有没有」查到底（**正本 §九**，别再重查）：
        //    · 原版的 tab 列表由**服务端 store 列表**驱动：`ShopWindow.SetupTabs` 遍历
        //      `LiveOpsManager.GetHandler<Shops>().GetAllStores()`，逐个 `CreateStoreTab(store)`
        //      （标签取 `store.drawData.EventLabels[key = 0].mTerm`、图标取 `LiveOpsAssetUtility.LoadAsset(store, 0)`）。
        //    · **本地一共 16 个 LiveOps SO，其中真 store 只有 1 个**：`Premium Campaign Store`
        //      （`grep -rl "extraNotificationsToWatch\|transactionLocation"` 全库只命中它）。
        //      其余 15 个是 `Expansion */Release *` 那些**过期活动**（用户裁决 #2：不做）。
        //    · ⇒ **我们三个页（Cards/Daily/Items）对应的 store SO 一个都没导出** —— 页签名与图标的配对**判不出来**。
        //    · prefab 里**只序列化了一个母版键**（GO 名 `Shop Icon`，label TMP `Pacotes`（葡语占位），
        //      图标 `40K_shop_bt_boosters`）—— 那是**运行期克隆的母版**，不是「第 1 个页签」。
        // ⇒ 我们**按本地有的三个页 prefab** 建三个键，**图标从那 7 张 `*_shop_bt_*` 里挑**（逐条标明「配图是我们挑的」）。

        /// <summary>一页。`Icon` 是 `bundle_liveopsicons_assets_all` 里 `*_shop_bt_*` 的一张。</summary>
        public struct PageSpec
        {
            public string Key, Label, Icon;
            /// <summary>`daily shop header/TimeCounter` 的**第一个子件**：`true` = 文字（`Refreshes in:`）、
            /// `false` = 时钟图标（`WF_icon_clock`）。**这是三页唯一的结构差**（实测）。</summary>
            public bool TimerAsText;
            /// <summary>页签 prefab 的原名（给诊断用）。</summary>
            public string Prefab;
            public PageSpec(string key, string label, string icon, bool timerAsText, string prefab)
            { Key = key; Label = label; Icon = icon; TimerAsText = timerAsText; Prefab = prefab; }
        }

        public static readonly PageSpec[] Pages =
        {
            // `Cards`  —— 原版 `Card Shop Tab`（脚本 `CardShopTab`，`itemOrder = 10`）
            new PageSpec("cards", "Cards", "40K_shop_bt_cards", true,  "Card Shop Tab"),
            // `Daily`  —— 原版 `Daily Shop Tab`（prefab 根**没有脚本**；那套子树与 `Shop Tab` 逐名相同）。
            //              ⚠️ 它的 `RefreshText` 文案在 prefab 里是**葡语占位** `Atualiza em:`，
            //                 而 `Card Shop Tab` 那份是英文 `Refreshes in:`（两预制文本不同，别混抄）。
            //                 ⇒ 我们统一用英文那句，并标明「Daily 那份原版是葡语占位」。
            new PageSpec("daily", "Daily", "40k_shop_bt_gold",  true,  "Daily Shop Tab"),
            // `Items`  —— 原版 `Item Shop Tab`（脚本 `ItemShopTab`，`itemOrder` 决定重排；No-Auto 变体是 0）
            new PageSpec("items", "Items", "40k_shop_bt_ticket", false, "Item Shop Tab"),
            // 🆕 **2026-10-27（B88_E12）：第 4 页 = VIP**（原版 `Card Shop VIP Tab Variant`，脚本 `VIPCardShopTab : CardShopTab`）。
            //   · 图标 `40K_shop_bt_cards_vip` —— 原版 `bundle_liveopsicons_assets_all` 里那张（同族 7 张 `*_shop_bt_*` 之一，
            //     名字里就带 vip ⇒ **不是我们挑的**；另 3 页那三张才是挑的）；
            //   · 页签文案走官方词条 `MenuShop/VIPCardStore/Title`（en `VIP Card store` / zh `VIP 张牌商店`）；
            //   · `TimerAsText = true`：本页 `TimeCounter` 的第一颗子件在 prefab 里就是 TMP `Refreshes in:`（实读）。
            //   ⚠️ 这一页的商品见 `_vip`；VIP 态见 `IsVip`（都是**我们编的**，原版在服务端）。
            new PageSpec("vip", "VIP", "40K_shop_bt_cards_vip", true, "Card Shop VIP Tab Variant"),
            // 🆕 **2026-10-28（`B91_W5`）第 5 页 = Boosters/Crates**（原版 `BoosterShopTab : ShopTab`，`ShopTabType.CratesShop = 0`）：图标 `40K_shop_bt_boosters`（同族 + 母版键那张）· 文案走官方词条 `MenuShop/ShopItemType/Boosters` · 计时器**恒隐藏**（`BoosterShopTab` 不 override `CheckTimer` ⇒ 基类 false）· 商品 = 卡包四件（`Offers(4)` 与 Cards 页共用 `_cards`，⛔ 不复制）。⚠️ `Booster Shop Tab` **这个 prefab 本地不存在**（`assets_full` 按名 0 命中）—— 原版的每个 store 页签都是**同一个 `tabPrefab`** 实例化 + 挂不同 `ShopTab` 子类（`ShopWindow__CreateStoreTab.c`），而 `Shop Menu Variant/Tabs` 里序列化的那一个就是 **`Shop Tab`**（现读 `Tabs` 1752.83×1009.06 撑满、其 `daily shop header` = **167.2,70.9 → 1920.0,155.9**，与 `ShopTabPage.Header` 逐字相同）⇒ **本页沿用共用几何是对的**，`Prefab` 这一格只是诊断名。
            new PageSpec("crates", "Boosters", "40K_shop_bt_boosters", true, "Booster Shop Tab"),
        };

        /// <summary>🆕 第 4 键是 `TabButtons.tabButtonPrefab` 的**母版**（照原版 `Initialize` 一进来就关掉）。
        /// 它借 `Buttons` 表里最后一项 —— 与商店这一层无关，但键表必须给它留一格。</summary>
        public const string MasterKeyIcon = "40K_shop_bt_boosters";

        // ============================================================ 三个页各有什么（🔴 **全是我们编的**）
        //
        // **原版的商品数据长什么样**（2026-09-23 从本地唯一那个真 store 读出来的，正本 §九·一）：
        //   `Premium Campaign Store.json` 的 `items[]`（13 条）每条 =
        //       { "item": { "targetId": "premium_campaign_um" },
        //         "price": { "Currency": 30, "Amount": 1999 },
        //         "availability": -1,
        //         "sku": "premium_campaign" }
        //   ⇒ 真形状是 **{物品 id, 价格(币种枚举 + 数额), 可购次数, sku}**；
        //     **商品名 / 类型行 / 主图 都不在数据里** —— 它们由 `CatalogItemContainer.OnInitialize`
        //     按 `Item` 去 `ItemDrawer` 那套画出来。
        //     🔴 **2026-10-03 就地更正（铁律 5 · A86）**：本行原来接着写「（我们还没有那套，见 §六 第 4 条）」
        //     —— **假的**：抽屉库 `Shell/ItemDrawer.cs` 已建、A8 把商店格里那条链**接上了**
        //     （`ItemDrawer.Draw(..., DrawerOverride.Shop, ...)`，见 `ShopWindow.BuildCell`）；
        //     连「那套」背后的判据也不缺 —— 「类型 → 抽屉 prefab」那张表 **2026-10-04 已整张解出**
        //     （`资料/普查产出_1004/ItemDrawerConfig_映射表.md` + `ItemDrawer.ItemTypeSets`）。
        //     **真正的原因**是：我们编的商品表里只有一个**显示名**（`ShopOffer.Name`）⇒ 类型判据空
        //     ⇒ 名字/类型那两行只能自己画（见 `ShopWindow.BuildCell` 的注释）。
        //   ⚠️ 那个 store 属于 `PremiumCampaignStore`（战役高级轨），**与我们这三个页无关**；
        //      其余 15 个 LiveOps SO 全是**过期活动** ⇒ **我们三个页的商品只能自己编**。
        //
        // 🔶 自己设计（非原版）：商店商品表（名字 / 类型 / 价格 / 拥有数 / 限购数） · 原版商品数据在服务端，本地唯一 store 属战役高级轨 · TODO(谁定的)｜原注：⇒ 下面这些名字 / 类型 / 价格 / 拥有数 / 限购数**都是我们挑的**，只为「把界面点通」。

        static ShopOffer[] _cards =
        {
            // 🔴 **2026-10-12（A439）：卡包这四件【故意不填 `Grants`】** —— 它们是**容器档**
            //   （`Type == "Booster Pack"`）：原版那一档的 `ShowRewardOnPurchase` 反编译出来是**常量 false**
            //   （`ContainerOfferData__get_ShowRewardOnPurchase.c` 那条 `return 0;`；旁证 = `ContainerService`
            //   那条通用路 `Collect(param_2, offer.<虚属性>, …)`），买完走**开包窗**
            //   （`ShopWindow.DoBuy` 里那句 `OpenBoosterPack`），**不开**领奖窗。
            //   ⇒ 这里填一张**没人读**的奖励表 = 死数据（本仓刚立过这条账）⇒ 留 `null` 并写明为什么。
            // ⚠️ `Art` 写的是**导入后的文件名**（`CardArt.MenuUi` 不做「空格 → 下划线」转换）。
            //    原版这两个槽的**真值在运行期由服务端给**（prefab 里 `background` 的 `m_Sprite` 是 0）
            //    ⇒ 这里用的是**同族里查得到的那张**（`bundle_boosterpacks_assets_all/Sprite/40K_shop_offer_booster_*`）。
            new ShopOffer("Ultramarines Booster",  "Booster Pack", "40K_shop_offer_booster_UM",
                          "1 800", 3, 1, 5, false, "-30%"),
            new ShopOffer("Sautekh Booster",       "Booster Pack", "40K_shop_offer_booster_Sautekh",
                          "2 000", 0, 2, 5, true,  null),
            new ShopOffer("Space Wolves Booster",  "Booster Pack", "40K_shop_offer_booster_Space_Wolves",
                          "2 000", 1, 0, 0, false, null, 4),
            new ShopOffer("Leviathan Booster",     "Booster Pack", "40K_shop_offer_booster_leviathan",
                          "2 200", 0, 0, 0, false, null),
        };

        // ============================================================ 🔴 奖励表（`Grants` · 2026-10-12 · A439）
        //
        // **值全是我们挑的**（理由与出处总述见 `ShopOffer.Grants` 的注释）—— 逐条的 **id 来源**：
        //   · 唯一**有原版判据**的一条 = `WildcardUltramarines2`：真 SO 里的 item id，
        //     `CampaignData.ItemIcon` 认得它（战役奖励表里也在用）；
        //   · 其余五条 = **按菜单图名当 id** —— 走 `RewardWindow.ArtOf` 现成的兜底
        //     （id 认不出时再认「id 本身就是一张菜单图名」），与日常线 `Wallet.Grant(art, n)` **同一口径**；
        //     这五张图**逐张核过工程里存在**。
        //   · **数量没有原版可对** ⇒ 挑的整数（150 / 1 / 5 / 100 / 1 / 1）。
        // 🔴 **2026-10-15 就地更正（A544 · 铁律 5）**：上面那句「这五张图逐张核过工程里存在」
        //    对 `_daily[0]` / `_items[0]` / `_items[1]` **三条已经不成立** —— 那三张（币种的 **big 族**）
        //    是这一轮新登记的，**要先跑一次导入腿**（`工具/import_original_art.py --only-menu`）才落到
        //    `Resources/Art/ui_menu/`。跑之前 `RewardWindow.NoIconItems` 会把它们算成**占位板**
        //    （不是静默：会 `LogWarning`），`Editor/ShopScene.cs` 的 A544 那两条断言钉的正是这一格。
        // ⚠️ 件的 `Tier` 一律 `TierBasic` —— 商店这一档**没有基础/高级轨之分**（那是战役奖励的概念），
        //    填它只为满足 `RewardSpec` 的三参 ctor；`RewardWindow` 只拿它做 `TogglePremiumHighlight` 那三跳。

        static ShopOffer[] _daily =
        {
            // 金 —— 🔴 **2026-10-15（A544）：小族 `40k_topmarquee_currency_gold` 换成 big 族**
            //   `40k_general_icon_currency_gold`。理由 = 本表**统一到一族**（R6 §六·2）：同一张表原来
            //   两族并存（这里小族、`_items[0]` 大族）⇒ 谁看都会再问一次「到底哪一档」。
            //   **档位判据**（R6 §二 · 三条互证）：原版货币抽屉的**主图**取 `Currency.GetIcon(item, Large)`
            //   （`CurrencyDrawer__Draw.c` + `Currency__GetIcon.c` 的 `+0x58` + `dump.cs` 的
            //   `bigIcon // 0x58` / `enum IconSize{Large=1}`）⇒ **主图 = `bigIcon`**。
            //   ⚠️ `gold`(0) 那个 SO **本地没有** ⇒ 「`40k_general_icon_currency_gold` 就是它的 `bigIcon`」
            //   是**同族 + 该 sprite 在真包里存在**推的（R6 §二 / §五·4 已如实标），⛔ 别当字段直读值引用。
            new ShopOffer("Daily Gold Cache",      "Gold Item",    null, "1 200", 0, 1, 1, true,  null)
            { Grants = new[] { new CampaignData.RewardSpec("40k_general_icon_currency_gold", 150, CampaignData.TierBasic) } },
            // 野牌（**这条有判据**：`WildcardUltramarines2` 是真 SO 里的 item id）
            new ShopOffer("Daily Wildcard",        "Wildcard",     null, "800",   2, 1, 3, false, "-50%")
            { Grants = new[] { new CampaignData.RewardSpec("WildcardUltramarines2", 1, CampaignData.TierBasic) } },
            // 骷髅（`40K_missions_icon_Daily_skulls` = 菜单图名，日常任务线在用）
            new ShopOffer("Daily Skulls",          "Currency",     null, "600",   0, 0, 0, false, null)
            { Grants = new[] { new CampaignData.RewardSpec("40K_missions_icon_Daily_skulls", 5, CampaignData.TierBasic) } },
        };

        static ShopOffer[] _items =
        {
            // 🔴 **2026-10-15（A544）**：这三件的 `Grants` id 原来有两处「图标发错」——
            //   ① 件名叫 **Blackstone**，发的却是**水晶**（`40k_general_icon_currency_crystal` = `Crystals`
            //      SO 的 `bigIcon`）；② 票券发的是**商店 Items 页签那颗钮的图**（`40k_shop_bt_ticket`，
            //      本文件的 `Pages[2].Icon` 就是它）。
            //   判据（原版侧）→ `资料/普查产出_1015/R6_A544商品图标判据.md` §二：三个币种 SO 的
            //   `smallIcon` / `bigIcon` 两列**逐字实读**（`bundle_cosmeticsso_assets_all` 的 `Blackstone.json` /
            //   `Gacha tickets.json` + `bundle_duplicateassetisolationso_assets_all` 的 `Crystals.json`）+
            //   「哪一档进哪个槽」三条互证（`CurrencyDrawer__Draw.c` / `Currency__GetIcon.c` / `dump.cs`）。
            //   **口径 = 路 B**：一格只用一张图 ⇒ 取**主图那一档 = `bigIcon`**。
            //   ⚠️ **填的是【落盘名】**（`CardArt.MenuUi` **不做**「空格 → 下划线」转换）——
            //      原版那张黑石 big 图的名字**带空格**（`40K_general_icon_currency blackstone`），
            //      而导入器按本表既有惯例落成下划线 ⇒ 这里必须写 `…_blackstone`。
            // 黑石（`40K_general_icon_currency_blackstone` = 菜单图名；原版 `Blackstone` SO 的 `bigIcon` 原名叫
            //   `40K_general_icon_currency blackstone`，**带空格**，落盘时空格换下划线）
            new ShopOffer("Blackstone Bundle",     "Currency",     null, "300,00", 1, 0, 0, false, null)
            { Grants = new[] { new CampaignData.RewardSpec("40K_general_icon_currency_blackstone", 100, CampaignData.TierBasic) } },
            // 票（`40K_icon_ticket_bundle` = 菜单图名；原版 `Gacha tickets` SO 的 `bigIcon`）
            new ShopOffer("War Chest Ticket",      "Ticket",       null, "1 000",  0, 0, 0, false, null)
            { Grants = new[] { new CampaignData.RewardSpec("40K_icon_ticket_bundle", 1, CampaignData.TierBasic) } },
            // 方框（`40k_square_border` = 菜单图名）
            new ShopOffer("Avatar Border: Servo",  "Cosmetic",     null, "600",    0, 0, 0, false, null)
            { Grants = new[] { new CampaignData.RewardSpec("40k_square_border", 1, CampaignData.TierBasic) } },
        };

        public static ShopOffer[] Offers(int pageIndex)
        {
            switch (pageIndex)
            {
                case 0: return _cards;
                case 1: return _daily;
                case 3: return _vip;   case 4: return _cards;   // case3 = 🆕 VIP 页（见 `_vip` 的注释；原版这一页的报价同样在服务端）· case4 = 🆕 2026-10-28（B91_W5）第 5 页 Crates/Boosters：与 Cards 页**共用同一份数组**（⛔ 不复制第二份）；⚠️ 因此**限购 `Available` 两页共享**（它存在数组那一格里），而**拥有数按页号各记**（`KeyOf(pageIndex, name)` = 一个 store 一份 TSave 的等价物）
                default: return _items;    // 第 2 页 = Items；以及任何越界页（见 `Buy`/`GrantsOf` 的越界判据）
            }
        }

        public static string PageLabel(int pageIndex)
        {
            return (pageIndex >= 0 && pageIndex < Pages.Length) ? Pages[pageIndex].Label : "?";
        }

        /// <summary>页签头那句「刷新倒计时」。**我们挑的**（原版是 `TimerDisplay` + 服务端到期时间 +
        /// `MenuShop/RefreshCounter` 词条；`CardShopTab.CheckTimer` 里那个「9 点」分支的常量没解出）。</summary>
        public static string RefreshText { get { return "Refreshes in:"; } }
        /// <summary>刷新倒计时的值。🆕 **2026-10-17（W_A_TimerDisplay）：不再是死串** ——
        /// 走全工程唯一的倒计时件 <see cref="TimerDisplay.ShopRefreshText"/>：
        /// 目标时刻 = `CardShopTab.CheckTimer` 的 **9 点规则**（`< 9 点取当天、否则次日`，再 `+9h`；
        /// 判据 `decomp_full/CardShopTab__CheckTimer.c:20-35`），格式化 = `SupportMethods.GetTimeString`。
        /// ⚠️ **本机时钟**（原版是 `PlayerDataManager.CurrentServerDateTime`；我们没有服务器 —— 本项目允许的偏离）。
        /// <para>⚠️ 这里返回的是**调用那一刻**的文本；`ShopTabPage` 的 `Time` 那一格另挂了
        /// <see cref="TimerDisplay"/> 组件，运行期每秒自己重算（批处理里没有帧循环 ⇒ 取这一刻的值就够）。</para></summary>
        public static string RefreshTime { get { return TimerDisplay.ShopRefreshText(); } }

        /// <summary>商品名的**短名**（没有主图时写在占位板上；太长的折成两行）。
        /// ⚠️ 这是我们为了「占位板别太空」加的，**原版没有这一层**。</summary>
        public static string ShortName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            if (name.Length <= 14) return name;
            int sp = name.IndexOf(' ');
            return sp > 0 ? name.Substring(0, sp) + "\n" + name.Substring(sp + 1) : name;
        }

        // ============================================================ 拥有 / 购买
        //
        // 🔴 **2026-10-25：拥有数已迁进背包框架层**（`Shell/Inventory.cs` 的 `Inventory<T>` / `ItemInfo<T>`；
        //    判据 = `资料/完全复刻/判据/泛型与启动链.md` §三 #3）。**这里不再自己存一份字典** ——
        //    拥有数的**正本 = `Inventories.Shop`**，落盘 = `InventoryStore`（`WarpforgeInventory/shop.json`）。
        //
        //    原版对位：拥有数在 `PlayerDataManager` 的**每活动一份 `TSave`** 里，读走
        //    `InventoryManager.GetOwnedCount(Item)`（`ILiveOpsHandler.cs:71` 的
        //    `SetupEvents(events, blockedEvents, sharedData)` + `LiveOpsEvent<TSave,TData>` 的每事件存档；
        //    见 `判据/泛型与启动链.md` §二）。
        //    ⚠️ **`InventoryStore` 是「`PlayerDataManager` 落地之前的临时本地等价物」** —— 与
        //    `DailyData` / `DraftRunSave` / `CardProgress` 那几份**同一套路**；将来一起并进 `PlayerDataManager`，
        //    迁移路径写在 `InventoryStore` 的类注释里。⛔ 别把它当成原版做法。
        //
        //    ⚠️ **限购 `Available` 不在背包里**（原版它在 store 的 `TSave`、不在 `ItemInfo`）——
        //    我们暂时**寄存在同一份文件**的 `extra` 那一格（`InventoryStore.Save(..., extras)`），
        //    等 store 的存档落地时搬回去。⛔ 别把 `Available` 塞进 `ItemInfo`（那会污染框架层）。

        /// <summary>每个商品一个**稳定实例**（背包按 id 认条目 ⇒ 同一个 key 必须恒给同一个 `ShopItem`）。🔴 2026-10-27（B1UI）：**这本字典不承担顺序职责** —— 全仓只有 `TryGetValue`/`Add`/`Clear` 三处用法、**没有一处 `foreach`**；货架画出来的顺序 = `Offers(pageIndex)` 那三个**定长数组的字面量次序**（⇒ 账上「字典 ⇒ 丢顺序」那条**前提不成立**）。⚠️ 但原版 `FixedStoreData.get_Items`「`items` 在前 + `offerLists` 按列表序展开」那一层我们本来就没有（那三份 store SO **没导出**）⇒ **数组次序本身的对错仍无判据**，⛔ 别把这条当「顺序已验证」。</summary>
        static readonly Dictionary<string, ShopItem> _shopItems = new Dictionary<string, ShopItem>();

        static string KeyOf(int pageIndex, string name) { return pageIndex + "/" + name; }

        static ShopItem ItemFor(int pageIndex, int i)
        {
            var o = Offers(pageIndex);
            if (i < 0 || i >= o.Length) return null;
            string k = KeyOf(pageIndex, o[i].Name);
            ShopItem it;
            if (_shopItems.TryGetValue(k, out it) && it != null) return it;
            it = new ShopItem(k, o[i].Name);
            _shopItems[k] = it;
            return it;
        }

        /// <summary>背包存档名（`WarpforgeInventory/shop.json`）。</summary>
        public const string InventoryName = "shop";

        static bool _seeded;

        /// <summary>建条目 + 用出厂值播种 + 读盘覆盖。**读口/写口都会先走它**（幂等）。</summary>
        static void EnsureSeeded()
        {
            if (_seeded) return;
            _seeded = true;
            var inv = Inventories.Shop;
            for (int p = 0; p < Pages.Length; p++)
            {
                var arr = Offers(p);
                for (int j = 0; j < arr.Length; j++)
                {
                    var it = ItemFor(p, j);
                    if (it == null) continue;
                    // 出厂值 = 表里的 `Owned`（那件出厂是不是已经攥着几张）
                    if (inv.Get(it) == null) inv.Add(it, arr[j].Owned);
                }
            }
            var extras = InventoryStore.Load(InventoryName, inv);
            // 限购：盘上有就盖回静态表（`Available` 是结构体数组里的字段 ⇒ 改的就是那一格）
            if (extras != null && extras.Count > 0)
            {
                for (int p = 0; p < Pages.Length; p++)
                {
                    var arr = Offers(p);
                    for (int j = 0; j < arr.Length; j++)
                    {
                        int av;
                        if (extras.TryGetValue(KeyOf(p, arr[j].Name), out av)) arr[j].Available = av;
                    }
                }
            }
        }

        /// <summary>把「拥有数（背包）+ 限购（extra）」一起落盘。</summary>
        static void PersistShop()
        {
            var extras = new Dictionary<string, int>();
            for (int p = 0; p < Pages.Length; p++)
            {
                var arr = Offers(p);
                for (int j = 0; j < arr.Length; j++) extras[KeyOf(p, arr[j].Name)] = arr[j].Available;
            }
            InventoryStore.Save(InventoryName, Inventories.Shop, extras);
        }

        /// <summary>自检用的目录改写口（转给 <see cref="InventoryStore.OverrideDir"/>）。</summary>
        public static string OverrideDir
        {
            get { return InventoryStore.OverrideDir; }
            set { InventoryStore.OverrideDir = value; }
        }
        /// <summary>最近一次落盘去的文件（自检读它做「真写下去了」的判据）。</summary>
        public static string LastStorePath { get { return InventoryStore.LastStorePath; } }
        /// <summary>最近一次落盘失败的原因（成功时 `null`）。</summary>
        public static string LastStoreError { get { return InventoryStore.LastStoreError; } }
        /// <summary>存档完整路径（日志/自检用）。</summary>
        public static string StorePath { get { return InventoryStore.StorePath(InventoryName); } }

        /// <summary>自检用：删掉存档文件（**自检必须调它**，否则会读到上一趟的残留）。</summary>
        public static void DeleteStoreForTest() { InventoryStore.Delete(InventoryName); }

        /// <summary>自检用：重读盘（先清背包 → 重新播种 → 读盘）。</summary>
        public static void ReloadForTest()
        {
            Inventories.Shop.ClearForTest();
            _seeded = false;
            EnsureSeeded();
        }

        /// <summary>这一件现在拥有几张。**正本 = 背包**（`Inventories.Shop`）；条目还没有时回表里的出厂值。
        /// ⚠️ **「全解锁」边界**：这里只管**显示**，不参与任何解锁判定（卡池本来就全解锁）。</summary>
        public static int OwnedOf(int pageIndex, int i)
        {
            EnsureSeeded();
            var o = Offers(pageIndex);
            if (i < 0 || i >= o.Length) return 0;
            var it = ItemFor(pageIndex, i);
            var info = it != null ? Inventories.Shop.Get(it) : null;
            return info != null ? info.Amount : o[i].Owned;
        }

        /// <summary>限购还剩几次（`Available Counter` 的 `1/5`）。0/0 = 这一件没有限购信息 ⇒ **不画那个件**。</summary>
        public static int AvailableOf(int pageIndex, int i)
        {
            EnsureSeeded();
            var o = Offers(pageIndex);
            if (i < 0 || i >= o.Length) return 0;
            return o[i].Available;
        }

        /// <summary>🆕 2026-10-03：**要不要先弹「传奇重复购买」确认框**。
        /// 判据 = `d:/2/tools/decomp_full/CatalogItemContainer__TryPurchase.c`：
        /// `Item != null &amp;&amp; Item.Rarity == 4 &amp;&amp; InventoryManager.GetOwnedCount(Item) == 1`
        /// ⇒ `WindowsManager.ShowPopUp(`**`MenuShop/ExtraLegendaryWarning`**`, 取消=`MainMenu/General/Cancel`,
        /// 确认=`MainMenu/General/OK`)`，**确认回调（`&lt;TryPurchase>b__8_0`）才走真正的购买**；
        /// 其余情况**直接买**（那一段的 `LAB_18078d9f8`）。
        /// 🔴 **`Rarity` 的值是我们挑的**（商品表本来就是我们的，原版从服务端 item 拿）——
        /// 见 `ShopOffer.Rarity` 的注释。⚠️ `AvailableMax &gt; 0` 且 `Available == 0` 时**原版也拦**
        /// （那是限购售罄，另一条），我们这里**只做稀有度这一条**（限购由 `ShopData.Buy` 自己如实记）。</summary>
        public static bool NeedsLegendaryConfirm(int pageIndex, int i)
        {
            var o = Offers(pageIndex);
            if (i < 0 || i >= o.Length) return false;
            return o[i].Rarity == 4 && OwnedOf(pageIndex, i) == 1;
        }

        /// <summary>`MenuShop/ExtraLegendaryWarning` 那句的**文案**（**每次现查** `Loc` —— `Loc` 不发事件，
        /// 取词条一律走这里，换语言之后同一次开窗能拿到新的一行）。
        /// 🔴 **2026-10-24（`A11` · 铁律 5 就地订正）：原来这里写「本地没有英文原文 ⇒ 表里那两列是我们写的」——
        /// 【错，且与事实相反】**：**官方本地化表两列都有**，且与 `Core/Loc.cs` 那条 Entry **逐字相同**
        /// （`数据/游戏数据/官方本地化_en+zh-CN.json` 的 `terms["MenuShop/ExtraLegendaryWarning"]`，现读）。
        /// 错因 = 「本地全库扫这个键 0 命中」被读成「原版没有」—— 它其实由**远端 Addressables 的本地化包**
        /// 下发、不在客户端安装目录里（同族教训 → `CLAUDE.md` 铁律 2 的 2026-10-14 更正）。
        /// 键名有出处 = 原版 `CatalogItemContainer__TryPurchase` 的 `0x42D24E0`（地址表实读）。
        /// 🆕 **2026-10-18（波 1 · P3）**：原来这里是 `public const string` 写死的中文 ⇒ **英文档也印中文**；
        /// 改成读词条之后中文列与改前那句**逐字相同** ⇒ 中文档零变化。
        /// ⚠️ **`const` → 属性** ⇒ 不能再当编译期常量用（`case` 标签 / 特性实参 / 别的 `const` 初值）；
        /// 全仓消费点只有 `Shell/ShopWindow.cs` 一个普通实参（现读 `grep`）⇒ 安全。</summary>
        public static string LegendaryWarnText { get { return Loc.T("MenuShop/ExtraLegendaryWarning"); } }

        /// <summary>🆕 **2026-10-12（A439）：这一件买到手要画进【领奖窗】的那几条奖励**
        /// （对位 = 原版 `ShopOfferBase.Items` / `ShopOfferDataV2.offerItems`）。
        /// <para>⚠️ **`null` 有两个来源，调用方必须**分开**处理**：① **越界**（`pageIndex` / `i` 不在表里）；
        /// ② **这一件本来就没有奖励表** —— 今天**只有卡包那四件**（容器档 ⇒ 走开包窗，见 `_cards` 的注释）。
        /// `Shell/ShopWindow.DoBuy` 那两路正是**先按 `Type == "Booster Pack"` 分档、再读本口**
        /// （⛔ 别拿 `null` 当分档判据 —— 那样「越界」与「容器档」两件事会混在一起）。</para>
        /// <para>⛔ **别在窗那边再写一份奖励表**（判据只此一处 —— 同仓「两处写同一条规则 = 迟早不一致」）。</para></summary>
        public static CampaignData.RewardSpec[] GrantsOf(int pageIndex, int i)
        {
            var o = Offers(pageIndex);
            if (i < 0 || i >= o.Length) return null;
            return o[i].Grants;
        }

        /// <summary>买一件。**照用户边界②：不做真实经济** ⇒ 不扣钱、不判定余额，
        /// 只做三件事：**拥有数 +1、限购 -1（有的话）、打一条日志**（红线：点了必须有反应，且**出声**）。
        /// 返回一句「买到了什么」给自检/日志用。
        /// <para>🆕 **2026-10-12（A439）**：本函数**只记账**（到手的那几条奖励读
        /// <see cref="GrantsOf"/>，**开窗在 `Shell/ShopWindow.DoBuy`** —— 两件事分家，同原版：
        /// 原版是服务端回包之后才 `RewardService.Collect(...)`）。</para>
        /// <para>⚠️ **本件没往 `Wallet` 记一笔**（商品档买了不会让 HUD 上的资源数变多）：我们这一侧的「到手」
        /// 就是 `Owned + 1` + 限购 −1（用户 2026-09-17 边界②：**不做真实经济**）；
        /// 真去调 `Wallet.Grant` 会让 `Shell/DailyData.cs` 那句「全工程唯一发奖口、6 个调用点全在本文件」
        /// **当场变错**（而那个文件不在本件白名单）⇒ **要不要把商店也接成 `Wallet.Grant` 的调用方，
        /// 留给调度台裁**（接的话得同批改那句注释）。</para>
        /// <para>⚠️ **2026-10-12 顺手订正**：本段原来（错误地）挂在 `NeedsLegendaryConfirm` 头上
        /// —— 它写的却是 `Buy` 的事（`Buy` 挪到下面之后文档没跟着走）。现在挪回它该在的地方。</para></summary>
        public static string Buy(int pageIndex, int i)
        {
            var o = Offers(pageIndex);
            if (i < 0 || i >= o.Length) return "越界";
            string k = pageIndex + "/" + o[i].Name;
            // 🆕 2026-10-25：**拥有数的正本 = 背包**（`Inventories.Shop`）—— 不再自己记字典
            var it = ItemFor(pageIndex, i);
            if (it != null) Inventories.Shop.Add(it, 1);
            if (o[i].Available > 0) o[i].Available--;      // struct 存在数组里 ⇒ 改的是数组那一格（`Offers` 返回的就是它）
            PersistShop();                                  // 写点必须落盘（否则重启清零）
            return o[i].Name + " ×" + OwnedOf(pageIndex, i);
        }

        /// <summary>自检用：把拥有数（背包）与限购推回初值，**并把存档文件一起删掉**
        /// （不删的话下次播种会读到上一趟的值 ⇒ 自检之间互相污染）。</summary>
        public static void ResetForTest()
        {
            Inventories.Shop.ClearForTest();
            _shopItems.Clear();
            _seeded = false;                                 // 下次读口重新播种（出厂值）
            _cards[0].Available = 1; _cards[1].Available = 2;
            DeleteStoreForTest();
        }

        // ============================================================ 诊断

        public static string Dump()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("Shop：");
            for (int p = 0; p < Pages.Length; p++)
            {
                if (p > 0) sb.Append(" · ");
                sb.Append(Pages[p].Label).Append(" ").Append(Offers(p).Length).Append(" 件");
            }
            // 🆕 **2026-10-12（A439）**：把奖励表那一列摆成**可观测口径**（⛔ 不是静默数据）——
            //   ⚠️ **2026-10-27（B88_E12）就地订正**：原来这里写「今天 = `6/10`」与「`Editor/ShopScene.cs` 只是 `Log` 它、**没有断言**」—— **两句都过期**：加了 VIP 页那 2 件（1 件有 `Grants`）之后是 **`7/12`**；而 `Editor/ShopScene.cs:2077` 那条 **`CheckTrue(dump.Contains("有奖励表 6/10 件"))` 是有断言的**（本批加页后它会红，已写进交接）。每页件数变了这一格要跟着变。
            int withGrants = 0, total = 0;
            for (int p = 0; p < Pages.Length; p++)
            {
                var o = Offers(p);
                total += o.Length;
                for (int i = 0; i < o.Length; i++) if (o[i].Grants != null) withGrants++;
            }
            sb.Append(" · 有奖励表 ").Append(withGrants).Append("/").Append(total).Append(" 件");
            return sb.ToString();
        }

        // ============================================================ 🆕 VIP 页（第 4 页 · 原版 `VIPCardShopTab : CardShopTab`）
        //
        // ---- 这一页我们有什么、缺什么（判据全在反编译里，别再重查）----
        // · **有**：页 prefab（`Card Shop VIP Tab Variant` —— 几何 / 字段 / 词条键全部实读得出）、
        //   生命周期（`VIPCardShopTab__{OnOpen,OnClose,ToFocus,VipTimerOnOnRunOutOfTime,CheckVIPState}.c`）；
        // · **两个「以为要服务端」的其实是本地逻辑**：`Store.CanPurchase` = `DailyStore.get_CanPurchase`
        //   （RVA `0x8CC740`：`requiredItem == null` ⇒ true；否则背包里有它）·
        //   `InventoryManager.GetExpirationDate(RequiredItem)` = 背包里那条实例的 `EphemeralState.EphemeralEndTime`。
        //   🔴 **2026-10-28（B88_B5 · 铁律 5 就地订正）**：这两句原来写的是「都在**服务端**」—— **错**。
        //   真正缺的只有**一格输入** `DailyStoreData.requiredItem`（那份 store SO 未导出）⇒ 见 `CanPurchase()`。
        // ⇒ 输入为空时按原版**条件①**返回「可买」并**出声**，⛔ 不许编一个真值冒充原版。

        /// <summary>本机 VIP 态 = 原版 `DailyStore.CanPurchase` 那一条（**本地逻辑**，见 <see cref="CanPurchase"/>）。
        /// <para>取值：① 自检 override 过 ⇒ 用它（<see cref="SetVipForTest"/>）；② 否则**按原版公式算**。</para>
        /// <para>🔴 **2026-10-28（B88_B5 · 铁律 5 就地订正）**：原来这里写「原版 = **服务端** `Store.CanPurchase`」——
        /// **那是错的**（旧记录把「输入取不到」写成了「语义在服务端」）。判据 → `判据/B88_R6_ShopStore购买链_VA判定.md` §三。</para>
        /// <para>两态都要能走一遍（原版 `CheckVIPState` 本来就是两条分支）⇒ 自检用 <see cref="SetVipForTest"/>。</para></summary>
        public static bool IsVip { get { return _vipForTest ?? CanPurchase(); } set { _vipForTest = value; } }

        /// <summary>VIP 到期时刻 = 原版 `InventoryManager.GetExpirationDate(Store.RequiredItem)` ——
        /// **本地可算**（背包里那条实例的 `EphemeralState.EphemeralEndTime`），缺的只是输入 `RequiredItem`
        /// （`DailyStoreData.requiredItem`，那份 store SO 未导出）⇒ 输入为空时给 <c>null</c>
        /// ⇒ 页头那颗倒计时显示 `--` 并由 `TimerDisplay` **出声一次**（⛔ 不编日期）。
        /// <para>🔴 **2026-10-28（B88_B5 · 铁律 5 就地订正）**：原来这里写「**服务端值**，本机取不到」——
        /// **错**，机制是本地（判据 = `InventoryManager__GetExpirationDate_object_.c` 逐句，见 R6 §3·3）。</para></summary>
        public static System.DateTime? VipExpiresAt { get { return null; } }

        /// <summary>`TimerDisplay` 在「目标取不到」时要打出来的那句原因（按 `what` 去重，只响一次）。</summary>
        public const string VipExpiryUnknownReason =
            "原版 VIP 到期时刻 = InventoryManager.GetExpirationDate(Store.RequiredItem)（LiveOps store 的 RequiredItem，本地没有那份 SO）";

        /// <summary>自检用：切 VIP 态（`true` = 原版 `Store.CanPurchase` 为真那一条分支）。⚠️ 切完要**重跑 `ShopTabPage.Setup()`**（或切一次页签 = `OnOpen`）才会重判——原版也是 `CheckVIPState()` 那一刻才切节点。</summary>
        public static void SetVipForTest(bool vip) { IsVip = vip; }

        /// <summary>🔴 **VIP 页的货架 —— 也是我们编的**（原版这一页是 `VIPCardShopTab` 自己那份 `DailyStore` 的报价，
        /// 与另外三页一样在服务端；那份 store SO 本地没导出）。
        /// <para>只为把界面与<b>两条购买路</b>点通：第 1 件是**容器档**（`Type == "Booster Pack"` ⇒ 买完开包窗，
        /// 见 `ShopTabPage.DoBuy`），第 2 件是**商品档**（有 `Grants` ⇒ 买完开领奖窗）。主图用同族里查得到的那张
        /// `40K_shop_offer_booster_generic`（原版这两个槽的图在运行期由服务端给，prefab 里 `m_Sprite` 是 0）。</para>
        /// <para>🔶 自己设计（非原版）：VIP 页商品表 · 原版报价在服务端（`VIPCardShopTab` 的 `DailyStore`，SO 未导出）·
        /// 调度台 2026-10-27 裁定（与另外三页同一处置：本文件文件头那句「下面这张表是**我们编的**」一并覆盖本表）。</para></summary>
        static ShopOffer[] _vip =
        {
            new ShopOffer("VIP Booster Cache", "Booster Pack", "40K_shop_offer_booster_generic",
                          "2 400", 0, 1, 3, true, null),
            new ShopOffer("VIP Wildcard Bundle", "Wildcard", null, "1 600", 0, 0, 0, false, null)
            { Grants = new[] { new CampaignData.RewardSpec("WildcardUltramarines2", 2, CampaignData.TierBasic) } },
        };

        // ============================================================ 原版 `DailyStore.CanPurchase`（类尾 · 新成员一律追加在这里）
        /// <summary>VIP 页那份 store 的 `requiredItem` id（= 原版 `DailyStoreData.requiredItem` 那个
        /// `LoadableReference<ObtainableItem>` 解出来的 id）。
        /// <para>🔴 **本地取不到**（那份 store SO 由远端 LiveOps 下发）。本批已逐处搜空：
        /// ① `d:/2/新解包资源/assets_full/**/*.json` —— `requiredItem` / `DailyStore` 全 **0 命中**；
        /// ② 安卓 1.35.0 `UnityDataAssetPack.apk`（`D:/2/tools/xapk_remote/`，186 条目 / 85 个 Addressables 束）——
        ///    **束清单里没有 store / liveops 这一类**（最接近的 `specialofferscontentlocal` 也 0 命中），
        ///    并对 `menus` / `boosterpacks` / `duplicateassetisolationso` / `specialofferscontentlocal` /
        ///    `Waprforge_monoscripts` 五个候选束逐字节搜 `requiredItem` / `DailyStore` —— 全 0；
        /// ③ `D:/2/关服前存档/…/Everguild_Warpforge_Unity_Cache` 那 3 个缓存包（allcards / I2Languages / 两张贴图）。
        /// ⇒ 保持空串；**空串 = 原版条件①**（`requiredItem == null` ⇒ `CanPurchase` 为真）。</para></summary>
        public static string RequiredItemId = "";

        /// <summary>自检 override 位（`null` = 没 override，按 <see cref="CanPurchase"/> 算）。</summary>
        static bool? _vipForTest;

        /// <summary>= 原版 `DailyStore.get_CanPurchase`（**纯本地逻辑**，⛔ 不是服务端语义）：
        /// ① `RequiredItem == null` ⇒ **true**（谁都算可买）；② 否则 = 背包里持有该 id 且**数量 &gt; 0**。
        /// <para>判据（VA 级逐指令，2026-10-27 `R6`）：`0x1808CC740` —— `mov rcx,[rbx+0x70]`（`requiredItem`）
        /// → `get_Reference` → `op_Equality(x, null)` 为真则 `mov al,1; ret`；否则
        /// `InventoryManager.Instance.HasItem(x)`（`0xCF7F30` → `HasItemByID` → `0: x.Amount`）。
        /// ⛔ 这一格**没有**货币够不够 / 等级 / 限购 / VIP 档位（逐条排除过）。
        /// 全文 → `资料/完全复刻/判据/B88_R6_ShopStore购买链_VA判定.md` §3·2。</para>
        /// <para>🔶 自己设计（非原版）：**查哪一份背包** —— 原版查 `InventoryManager` 里 `ObtainableItem`
        /// 那一份（远端物品目录）；本仓没有 `ObtainableItem` 这个类型，「按 id 记持有数」的正本是
        /// `Inventories.Shop`（`Inventory&lt;ShopItem&gt;`，`Id = pageIndex + "/" + 名字`，由 `ShopData.Buy` 写）。
        /// 为什么只能这样：`requiredItem` 指向的是远端物品 id，本地既没有那份物品目录、也没有对应背包类型 ·
        /// 调度台 2026-10-28 裁定（该分支今天走不到：`RequiredItemId` 为空 ⇒ 条件① 直接返回 true）。</para></summary>
        public static bool CanPurchase()
        {
            if (string.IsNullOrEmpty(RequiredItemId)) return true;     // 条件① = 原版 `requiredItem == null`
            var inv = InventoryManager.GetInventory<ShopItem>();       // 条件②
            if (inv == null)
            {
                // 红线「不许静默失败」：原版这里是 `InventoryManager.Instance` 单例（为 null 会 NRE）
                Debug.LogWarning("[Shop] `CanPurchase`：`ShopItem` 那份背包没登记 ⇒ 按「买不了」返回"
                               + "（原版走 `InventoryManager.Instance.HasItem`，单例为 null 会 NRE）");
                return false;
            }
            var info = inv.Get(RequiredItemId);
            return info != null && info.Amount > 0;
        }

        // ============================================================ 原版 `ShopStore.RemoveFinishedOffer()`（类尾 · 新成员一律追加在这里）
        /// <summary>= 原版 `ShopStore&lt;TSave,TData&gt;.RemoveFinishedOffer()`（判据
        /// `out_cpp2il_dev_ilspy/Everguild.LiveOps/ShopStore.cs:589-640` 现读）：**倒序遍历**页内报价表，
        /// `!offer.IsActive()` 的**摘掉**（`List.RemoveAt(i)`），遍历到表头为止。
        /// <para>🔴 **`IsActive()` 的语义** = `LiveOpsEventData.IsActive(now, includePlanned: false)`，
        /// 即「`now` 落在 `[start, end]` 内」。我们这一侧**没有报价的起止时刻**（`ShopStore`/`LiveOps
        /// store` SO 本地一份都没导出，同 <see cref="RequiredItemId"/> 那段），⇒ 活跃性判据**由调用方注入**
        /// （`Func&lt;ShopOffer,bool&gt;`）。</para>
        /// <para>⚠️ 今天**没有任何调用方**（同原版：这个方法由 LiveOps 刷新链在拿到新 store 之后调）。
        /// 形状先补齐，判据一旦有了数据就能一条语句接上（铁律 11：差异要做，只有先后）。</para>
        /// <param name="isActive">`null` = 全部算活跃（= 今天：静态表没有结束态）⇒ **一个都不摘**。</param>
        /// <returns>被摘掉的条数。</returns></summary>
        public static int RemoveFinishedOffer(System.Func<ShopOffer, bool> isActive)
        {
            if (isActive == null) return 0;
            int removed = 0;
            for (int p = 0; p < Pages.Length + 1; p++)   // Pages 之外的越界页在 `Offers` 里回落到 `_items`
            {
                var page = PageArray(p);
                if (page == null) continue;
                var keep = new System.Collections.Generic.List<ShopOffer>(page.Length);
                // 原版**倒序**遍历（`for (i = Count-1; i >= 0; i--)`），保留顺序不变 ⇒ 正序筛出的结果与之一致
                for (int i = page.Length - 1; i >= 0; i--)
                {
                    if (isActive(page[i])) keep.Add(page[i]);
                    else removed++;
                }
                if (keep.Count == page.Length) continue;            // 一条没摘 ⇒ 不动这一页
                keep.Reverse();                                     // 倒序收集 ⇒ 翻回原顺序
                SetPageArray(p, keep.ToArray());
            }
            return removed;
        }

        /// <summary>纯函数版（原版那个循环的「筛」那一半）：返回筛完的新数组，`removed` = 摘掉几条。
        /// <para>🆕 2026-10-28（B93_S1）：给自检用 —— 它不碰静态表，所以断言可以随便造两件报价来试。</para></summary>
        public static ShopOffer[] PruneFinished(ShopOffer[] page, System.Func<ShopOffer, bool> isActive, out int removed)
        {
            removed = 0;
            if (page == null) return null;
            if (isActive == null) return page;
            var keep = new System.Collections.Generic.List<ShopOffer>(page.Length);
            for (int i = 0; i < page.Length; i++)
            {
                if (isActive(page[i])) keep.Add(page[i]);
                else removed++;
            }
            return keep.ToArray();
        }

        /// <summary>页号 → 那一页的数组（**越界页**照 `Offers` 的回落规则 = `_items`；`Pages.Length` 本身不存在 ⇒ null）。</summary>
        static ShopOffer[] PageArray(int pageIndex)
        {
            switch (pageIndex)
            {
                case 0: return _cards;
                case 1: return _daily;
                case 2: return _items;
                case 3: return _vip;
                case 4: return _cards;
                default: return pageIndex == Pages.Length ? null : _items;
            }
        }

        /// <summary>把筛完的数组写回那一页（`Offers` 的回落规则同样适用于越界页）。</summary>
        static void SetPageArray(int pageIndex, ShopOffer[] page)
        {
            switch (pageIndex)
            {
                case 0: _cards = page; break;
                case 1: _daily = page; break;
                case 2: _items = page; break;
                case 3: _vip = page; break;
                case 4: _cards = page; break;
                default: if (pageIndex != Pages.Length) _items = page; break;
            }
        }
    }
}
