// ShopData.cs — 商店的**本地数据源**
//
// ============================ 这一层为什么全是「我们挑的」 ============================
// 原版的商品是**服务端给的三件事**：
//   ① `ShopWindow.SetupTabs` 按**存档里的 store 列表**逐个建页签（`ShopWindow__SetupTabs.c`）；
//   ② 每个 store 有哪些报价、报什么价、拥有几张 —— 全在 PlayFab 的 store / inventory 里；
//   ③ 哪些能买（`Available: 1/5` 那种限购计数）也在服务端。
// 本地能拿到的**只有界面骨架与模板**（三个页签 prefab + `Catalog Item Shop Container` 模板 + 图标）。
// ⇒ 按用户 2026-09-17 的边界②（**不做真实经济**：资源固定 9999、商店与卡包照常能买、全解锁），
//    下面这张表是**我们编的**，逐条标明；**别当成原版数字**。
//
// ⚠️ **价格与「拥有数」都是我们挑的**：原版价格是本地化词条 + 服务端数字（`1800` / `300,00` 那种），
//    拥有数走 `CatalogItemContainer.ownedCount`。这里给的是**能让人把界面点通**的一组值。
using System.Collections.Generic;

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
        /// `Item.Rarity == 4 && GetOwnedCount(Item) == 1` ⇒ 先弹 `MenuShop/ExtraLegendaryWarning`）。
        /// 🔴 **我们的商品表里本来没有这一档**（原版从服务端的 item 拿）⇒ **这里的值是我们挑的**，
        /// 只为把那条链跑通（0 = 不走确认）。</summary>
        public int Rarity;

        public ShopOffer(string name, string type, string art, string price, int owned,
                         int avail, int availMax, bool timed, string newDiscount, int rarity = 0)
        {
            Name = name; Type = type; Art = art; Price = price; Owned = owned;
            Available = avail; AvailableMax = availMax; Timed = timed; NewDiscount = newDiscount;
            Rarity = rarity;
        }
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
        // ⇒ 下面这些名字 / 类型 / 价格 / 拥有数 / 限购数**都是我们挑的**，只为「把界面点通」。

        static readonly ShopOffer[] _cards =
        {
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

        static readonly ShopOffer[] _daily =
        {
            new ShopOffer("Daily Gold Cache",      "Gold Item",    null, "1 200", 0, 1, 1, true,  null),
            new ShopOffer("Daily Wildcard",        "Wildcard",     null, "800",   2, 1, 3, false, "-50%"),
            new ShopOffer("Daily Skulls",          "Currency",     null, "600",   0, 0, 0, false, null),
        };

        static readonly ShopOffer[] _items =
        {
            new ShopOffer("Blackstone Bundle",     "Currency",     null, "300,00", 1, 0, 0, false, null),
            new ShopOffer("War Chest Ticket",      "Ticket",       null, "1 000",  0, 0, 0, false, null),
            new ShopOffer("Avatar Border: Servo",  "Cosmetic",     null, "600",    0, 0, 0, false, null),
        };

        public static ShopOffer[] Offers(int pageIndex)
        {
            switch (pageIndex)
            {
                case 0: return _cards;
                case 1: return _daily;
                default: return _items;
            }
        }

        public static string PageLabel(int pageIndex)
        {
            return (pageIndex >= 0 && pageIndex < Pages.Length) ? Pages[pageIndex].Label : "?";
        }

        /// <summary>页签头那句「刷新倒计时」。**我们挑的**（原版是 `TimerDisplay` + 服务端到期时间 +
        /// `MenuShop/RefreshCounter` 词条；`CardShopTab.CheckTimer` 里那个「9 点」分支的常量没解出）。</summary>
        public static string RefreshText { get { return "Refreshes in:"; } }
        /// <summary>刷新倒计时的值。**我们挑的固定串**（原版是本地时间的真倒计时）。</summary>
        public static string RefreshTime { get { return "12h 24m"; } }

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

        static readonly Dictionary<string, int> _owned = new Dictionary<string, int>();

        /// <summary>这一件现在拥有几张。初值取表里的 `Owned`（= 那件出厂是不是已经攥着几张）。
        /// ⚠️ **「全解锁」边界**：这里只管**显示**，不参与任何解锁判定（卡池本来就全解锁）。</summary>
        public static int OwnedOf(int pageIndex, int i)
        {
            var o = Offers(pageIndex);
            if (i < 0 || i >= o.Length) return 0;
            string k = pageIndex + "/" + o[i].Name;
            int v;
            return _owned.TryGetValue(k, out v) ? v : o[i].Owned;
        }

        /// <summary>限购还剩几次（`Available Counter` 的 `1/5`）。0/0 = 这一件没有限购信息 ⇒ **不画那个件**。</summary>
        public static int AvailableOf(int pageIndex, int i)
        {
            var o = Offers(pageIndex);
            if (i < 0 || i >= o.Length) return 0;
            return o[i].Available;
        }

        /// <summary>买一件。**照用户边界②：不做真实经济** ⇒ 不扣钱、不判定余额，
        /// 只做三件事：**拥有数 +1、限购 -1（有的话）、打一条日志**（红线：点了必须有反应，且**出声**）。
        /// 返回一句「买到了什么」给自检/日志用。</summary>
        /// <summary>🆕 2026-10-03：**要不要先弹「传奇重复购买」确认框**。
        /// 判据 = `d:/2/tools/decomp_full/CatalogItemContainer__TryPurchase.c`：
        /// `Item != null && Item.Rarity == 4 && InventoryManager.GetOwnedCount(Item) == 1`
        /// ⇒ `WindowsManager.ShowPopUp(`**`MenuShop/ExtraLegendaryWarning`**`, 取消=`MainMenu/General/Cancel`,
        /// 确认=`MainMenu/General/OK`)`，**确认回调（`<TryPurchase>b__8_0`）才走真正的购买**；
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

        /// <summary>`MenuShop/ExtraLegendaryWarning` 那句的**文案**。
        /// 🔴 **词条在远端语言表里** —— 本地只有 key（`stringliteral.json` 里 `0x42D24E0` 就是它），
        /// **没有英文原文** ⇒ 下面这句是**我们写的**，如实标（铁律 3）。</summary>
        public const string LegendaryWarnText =
            "你已经有 1 张传奇品质的这一件了。\n确定还要再买一张吗？";

        public static string Buy(int pageIndex, int i)
        {
            var o = Offers(pageIndex);
            if (i < 0 || i >= o.Length) return "越界";
            string k = pageIndex + "/" + o[i].Name;
            _owned[k] = OwnedOf(pageIndex, i) + 1;
            if (o[i].Available > 0) o[i].Available--;      // struct 存在数组里 ⇒ 改的是数组那一格（`Offers` 返回的就是它）
            return o[i].Name + " ×" + _owned[k];
        }

        /// <summary>自检用：把拥有数与限购推回初值。</summary>
        public static void ResetForTest()
        {
            _owned.Clear();
            _cards[0].Available = 1; _cards[1].Available = 2;
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
            return sb.ToString();
        }
    }
}
