// ============================================================================ `ItemSpec` 的第二部（施工单 §四-2「`ItemSpec` 缺字段」）
//
//  为什么单开一份：`Shell/ItemDrawer.cs:123-145` 那个 `ItemSpec` 本体被 `账/*.tsv` 的 A 落点钉着行号
//  ⇒ **往本体里插字段会让下游整体漂移**（铁律 18·b）。本体早已声明成 `partial`（`:123`）⇒ 字段追加在这里。
//  ⛔ 别把这些字段挪回本体；也⛔ 别在本体里再写一遍（同一个 partial 类型只能有一份声明）。
//
//  ── 每个字段「原版叫什么 / 我们的数据从哪来」──────────────────────────────────
//    · `CardId`      = `RawCardScript` 的资产 id（B6 `CardDrawer` / B7 `CardAlternateArtDrawer` 用；**本轮未实现**，见报告）
//    · `Tier`        = `AllianceTrophyData` 那一档（B18 的 `SetTierFrame(quantity)` 用它）
//    · `Title`       = 原版 `CosmeticItemTitle.LocalizedName`（B9 `title.text = item.LocalizedName`）
//    · `BadgeId`     = 原版 `AllianceBadge.uniqueId`（B18 `item.reward.Reference.badgeImage`）
//    · `BackgroundArt` = 原版 `PremiumItem.BackgroundSprite`（B13 `PremiumIconDrawer` 的第二张图）
//    · `SecondArt`   = 「同一件物品上的**第二张图**」的通用口（本轮只有 B13 用到；给 `PremiumItem` 那张
//                      `BackgroundSprite` 用 —— 我们的数据层把每张图都压成**一个图名**，没有 SO 字段）
using UnityEngine;      // `ItemSpec` 本身不用，但同文件的自检辅助可能用；保持与同族文件一致

namespace CardPresentation
{
    public partial struct ItemSpec
    {
        /// <summary>原版那件物品的**卡 id**（`RawCardScript` / 克隆卡）。`null` = 不是卡类物品。
        /// <para>⚠️ 本轮 `CardDrawer` / `CardAlternateArtDrawer` **未实现**（卡在「本机没有 `BasicCardUI` 等价件」
        /// —— 施工单 B6「还差什么」第 4 条）⇒ 该字段**今天没有读者**，留着是为了按施工单把数据位补齐，
        /// 下一轮做 B6/B7 时不用再改结构体。</para></summary>
        public string CardId;

        /// <summary>称号的**显示名**（原版 `CosmeticItemTitle.LocalizedName`）。空 ⇒ 回落 <see cref="Label"/>。</summary>
        public string Title;

        /// <summary>联盟徽章 id（原版 `AllianceBadge.uniqueId`）—— 查 `AllianceBadgeArt` 那张 id → 图名表。</summary>
        public string BadgeId;

        /// <summary>第二张图（原版 `PremiumItem.BackgroundSprite`；B13 `PremiumIconDrawer` 的 `background` 层）。</summary>
        public string BackgroundArt;
    }
}
