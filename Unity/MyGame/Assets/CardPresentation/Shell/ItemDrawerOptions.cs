// ============================================================================ 原版 `ItemDrawerReference.options`（`ItemDrawerOptions`）
//
//  这是原版**每个 `ObtainableItem` 子类型**上挂的那三格选项：
//    `stackable`（这一档要不要画数量）· `showName`（要不要画名字）· `typeString`（类型行那条**可能**是词条 key 的串）。
//  它和「用哪个抽屉 prefab」是**两张表**：抽屉那张 = `ItemDrawerConfig.drawers`（20 条 + 19 档 override，
//  映射表 → `资料/普查产出_1004/ItemDrawerConfig_映射表.md`），本文件只是它旁边那张 `options` 表。
//
//  ── 判据（三源，逐条现读，2026-10-28 B89_E17c）────────────────────────────────
//  ① **数据**：`sharedassets0.assets` 的 MonoBehaviour **pathID 453**（`ItemDrawerConfig`），
//     记录起点与字段顺序逐条解出 → `资料/普查产出_1004/ItemDrawerConfig_映射表.md` §「`options`」那张表
//     （20 行，`stackable/showName/typeString` 三列全有值，0 条未解出；表自己也带了字节偏移可复查）。
//  ② **消费点**：`d:/2/tools/decomp_full/…__Draw.c` 一族里 `ItemDrawer<T>.Draw` 在算完 `GetItemName` 之后
//     用这两格决定「画不画」；`GeneralOfferPopupDrawer__DrawRewards.c:319` 传 `0x1e`(=30=`OfferPopups`) 说明
//     奖励弹窗走 **override 30**，而**全表只有 3 个类型写了 30**。
//  ③ **prefab 上有没承载件**（这条是本轮新查的，`d:/2/tools/assetripper_out/ExportedProject/Assets/*.prefab`
//     的 `ItemDrawerComponents` 字段引用）：
//       · `Currency Drawer`（Currency 基础档）：`image` ✅ · `label` **{fileID: 0}（null）** · `quantity` ✅ · `icon` ✅
//       · `Icon Currency Drawer Variant`（override 10，**商店里 Currency 用的就是它**）：`label` null · `quantity` **null**
//       · `Premium Drawer`（PremiumItem 基础档）：`image` ✅ · `label` **非空** · `quantity` **null**
//       · `Icon Premium Campaign Drawer Variant`（override 10）：`label` null · `quantity` null
//     ⇒ **「画不画」= `options` 那格 ∧ prefab 上有没有承载件**（两个闸是**与**关系）。
//     这就是为什么 `Currency` 的 `showName = 1` 却**看不到名字**：它的 `label` 是 null，没有地方写。
//
//  ── 本文件对外只暴露三件事，且**只接能观察到差异的那一条** ──────────────────────
//    · `Stackable(item)` → **已接**（`ItemDrawer` 的 `Icon` / `Placeholder` 两个数量口）。
//      可见差异：`PremiumItem`(0) / `VIPPremiumItem`(0) / `RawCardScript`(0) 三档**不再画数量**
//      —— 与 prefab 的 `quantity: {fileID: 0}` **互相印证**（两源一致，不是我们挑的）。
//    · `ShowName(item)` → **表里有值、今天没有消费者**，理由写死在下面 `ShowName` 的注释里
//      （我们的抽屉里**没有任何 `label` 槽**；而原版唯一有 `label` 的 `Premium Drawer` 恰好 `showName = 0`
//       ⇒ 原版里这一格的输出**也不显示**）⇒ ⛔ 不为了让函数有调用方去造一个名字 UI（那是编）。
//    · `TypeString(item)` → **只读用途**：4 条非空（`Currency` / `PlayerAvatar` / `CosmeticItemCardback` /
//      `CosmeticItemTitle`）。⚠️ 它**不是文案**（普查 §⑤ 第 6 条：`CosmeticItemTitle` 是 `showName=0` 却也有
//      `typeString` ⇒ 两个字段不同义；而且没验证过它是否就是最终显示串）⇒ ⛔ 别拿它当文字印。
using System;

namespace CardPresentation
{
    /// <summary>原版 `ItemDrawerOptions`（`ItemDrawerReference.options`）那张表的只读副本。
    /// <para>⛔ 这张表**只放原版值**：key = 原版 `ObtainableItem` 子类型名（逐字，含命名空间），
    /// 一个字段都不许自己编 —— 判据与本轮新增的 prefab 证据全在文件头。</para></summary>
    internal static class ItemDrawerOptions
    {
        // ---- 20 条，`stackable` / `showName` / `typeString` 逐条照普查表抄（顺序也照它的记录顺序）----
        struct Row
        {
            public string Type, TypeString;
            public bool Stackable, ShowName;
            public Row(string type, bool stackable, bool showName, string typeString)
            { Type = type; Stackable = stackable; ShowName = showName; TypeString = typeString; }
        }

        static readonly Row[] Rows =
        {
            new Row("Currency",                                   true,  true,  "MenuShop/ShopItemType/Currency"),
            new Row("PlayerAvatar",                               true,  true,  "PlayerProfile/Avatar"),
            new Row("Everguild.LiveOps.ShopContainer",             true,  false, null),
            new Row("RawCardScript",                              false, false, null),
            new Row("DropTableItem",                              true,  false, null),
            new Row("CosmeticItemCardback",                       true,  true,  "Item_Description/Cardback"),
            new Row("Wildcard",                                   true,  false, null),
            new Row("CampaignPoints",                             true,  false, null),
            new Row("ForgePoints",                                true,  false, null),
            new Row("Everguild.LiveOps.ExpansionPassPoints",       true,  false, null),
            new Row("PrebuiltDeck",                               true,  false, null),
            new Row("CosmeticItemTitle",                          true,  false, "PlayerProfile/Title"),
            new Row("PremiumItem",                                false, false, null),
            new Row("VIPPremiumItem",                             false, false, null),
            new Row("ExpansionPremiumItem",                       true,  false, null),
            new Row("AllianceTrophyData",                         true,  false, null),
            new Row("CosmeticItemAvatarBorder",                   true,  false, null),
            new Row("XSollaBundleItem",                           true,  true,  null),
            new Row("AlternateArtCard",                           true,  false, null),
            new Row("GenericArmyItem",                            true,  false, null),
        };

        /// <summary>求这一件在原版里是哪个 `ObtainableItem` 子类型。
        /// <para>顺序：① `ItemSpec.Type`（`ItemDrawer.IdTypes` 那张实据表已经填好的）② 货币 code（`C2/C3/C5`
        /// —— 三份 `Currency` SO 的 `code` 字段实读，见 `CurrencyNames.cs`）⇒ `Currency`
        /// ③ `premium_campaign_*` ⇒ `PremiumItem`（**不是按前缀推的**：走 `CampaignData.PremiumAll` 那 13 条实读表，
        /// 单一来源、⛔ 不在这里再抄一份 id 清单）④ 都没有 ⇒ `null`（= 判据空，调用方走「原版默认」）。</para></summary>
        internal static string TypeOf(ItemSpec item)
        {
            if (!string.IsNullOrEmpty(item.Type)) return item.Type;
            if (CurrencyNames.Of(item.Id) != null) return "Currency";
            if (!string.IsNullOrEmpty(item.Id))
            {
                var prem = CampaignData.PremiumAll;
                for (int i = 0; i < prem.Length; i++)
                    if (prem[i].Id == item.Id) return "PremiumItem";
            }
            return null;
        }

        static Row? Find(string type)
        {
            if (string.IsNullOrEmpty(type)) return null;
            for (int i = 0; i < Rows.Length; i++) if (Rows[i].Type == type) return Rows[i];
            return null;
        }

        /// <summary>这一档**要不要画数量**（原版 `stackable`）。
        /// <para>判据空（`TypeOf` 求不出类型）时返回 **true** —— 这是**原版的多数档**（20 条里 17 条是 1），
        /// 也是我们改之前的现值 ⇒ 认不出类型时**行为不变**（⛔ 不是我们挑的新规则；表里 `stackable=0` 的只有
        /// `RawCardScript` / `PremiumItem` / `VIPPremiumItem` 三档，见文件头）。</para></summary>
        public static bool Stackable(ItemSpec item)
        {
            var r = Find(TypeOf(item));
            return r.HasValue ? r.Value.Stackable : true;
        }

        /// <summary>这一档**要不要画名字**（原版 `showName`）。
        /// <para>🔴 **今天没有消费者，这是查明的结果、不是漏接**：要画名字得先有「名字槽」，
        /// 而原版四个抽屉 prefab 的 `ItemDrawerComponents.label` 里**只有 `Premium Drawer`（基础档）非空**，
        /// 偏偏 `PremiumItem.showName = 0` ⇒ **原版这一格的输出不显示**；`Currency` 是 `showName = 1` 但
        /// `label = {fileID: 0}`（没有地方写）⇒ 同样不显示。我们的 `Icon` 抽屉本来就没有 label 槽 ⇒ 与之一致。
        /// ⇒ ⛔ 别为了让这个函数有调用方去造一个名字 UI（那是编一个原版没有的件）。
        /// 判据空时返回 **false**（= 不画）—— 这一档刻意与 <see cref="Stackable"/> 的兜底相反：
        /// 画名字是**多出**一个原版可能没有的件，宁可少画（红线：不许静默多画）。</para></summary>
        public static bool ShowName(ItemSpec item)
        {
            var r = Find(TypeOf(item));
            return r.HasValue ? r.Value.ShowName : false;
        }

        /// <summary>原版 `typeString`（4 条非空）。⚠️ **不是文案** —— 只作诊断/对账用，⛔ 别印到屏幕上（见文件头）。</summary>
        public static string TypeString(ItemSpec item)
        {
            var r = Find(TypeOf(item));
            return r.HasValue ? r.Value.TypeString : null;
        }

        /// <summary>诊断读口：表里有没有这一条（自检用来断「20 条一条不少」）。</summary>
        public static int Count { get { return Rows.Length; } }

        /// <summary>诊断读口：按原版类型名取那三格（表里没有 ⇒ `false`）。给自检/对账用，⛔ 别在画路上用。</summary>
        public static bool TryGet(string type, out bool stackable, out bool showName, out string typeString)
        {
            var r = Find(type);
            stackable = r.HasValue ? r.Value.Stackable : true;
            showName = r.HasValue ? r.Value.ShowName : false;
            typeString = r.HasValue ? r.Value.TypeString : null;
            return r.HasValue;
        }
    }
}
