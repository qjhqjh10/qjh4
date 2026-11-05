// PlayEventDeckSource.cs — `IPlayEventDeckSource` 的**本仓实现**（事件窗口/活动基类取卡组的那条口）
//
// ============================ 它是什么 ============================
// 原版 `PlayEvent.GetEventDeck()` / `GetEventDeck(CardArmy)` / `ValidateEventDeck` 直接从
// `InventoryManager.GetInventory<CardDeck>()`（+ `AssetLocator.GetAsset<CardDeck>(id)`）取卡组对象。
// 我们这一层没有 `AssetLocator` / `InventoryManager`，卡组住 `Shell/CollectionData`（`static DeckLibrary`）
// ⇒ 由 `Shell/PlayEvent.cs` 的 `IPlayEventDeckSource` 注入点接上；本文件就是那个注入点**唯一**的实现。
//
// 🔴 **认卡组一律走 id，⛔ 不走名字**：`CollectionData.cs:181` 自己写着「同名卡组按名字认会选错」
//    （`Shell/LiveOpsEventWindow.EventDeckIndex` 的旧档回落就是那条老账）。
//    判据 → `判据/B146_对账_B界面卡组收藏.md` 的 `B142-PLAYEVENT-REACH` 节。
//
// ✅ 已复刻（2026-11-05 · B146-E8 · 判据 `B146_对账_B界面卡组收藏.md` `B142-PLAYEVENT-REACH`
//   ① = 新建 `PlayEventDeckSource`，`Count/IdAt/ArmyAt/GameModeAt/IsPlayableAt/IndexOfId` 六个成员；
//   注入点在 `CollectionData` 的静态构造（唯一卡组库宿主）；原版对应物 = `InventoryManager` 单例）

using RuleEngine;

namespace CardPresentation
{
    /// <summary>`IPlayEventDeckSource` 的实现：把 <see cref="CollectionData"/> 的卡组库摊成事件要的六格。
    /// <para>⚠️ 每个成员都**现读**（不缓存）：`CollectionData.InvalidateLibrary()` 之后本类跟着看到新的那一份。</para></summary>
    public sealed class PlayEventDeckSource : IPlayEventDeckSource
    {
        /// <summary>库里现在有几套卡组（= 原版 `InventoryManager.GetInventory&lt;CardDeck&gt;().Count`）。</summary>
        public int Count { get { return CollectionData.DeckCount(); } }

        /// <summary>第 <paramref name="i"/> 套的**身份 id**。
        /// <para>⚠️ 旧存档的卡组可能还没有 id（`DeckLibrary.DeckIdAt` 那时给 `null`）——
        /// 原版 `CardDeck.deckId` 由构造函数**必写**、不存在这一档 ⇒ 这里用 `EnsureDeckId` **现发并落盘**
        /// （与 `DeckLibrary` 类尾注「取用一律走 `EnsureDeckId`」同一口径），否则事件会「有卡组却取不到」。</para></summary>
        public string IdAt(int i)
        {
            var id = CollectionData.DeckIdAt(i);
            return string.IsNullOrEmpty(id) ? CollectionData.EnsureDeckId(i) : id;
        }

        /// <summary>第 <paramref name="i"/> 套的阵营（原版 `CardDeck.Army` = `deckHero.cardArmy`）。
        /// 查不到 / 中立 ⇒ `Neutral`（`IPlayEventDeckSource` 的约定）。</summary>
        public CardArmy ArmyAt(int i)
        {
            int army = ArmyIcons.Id(CollectionData.DeckAt(i).Faction);   // 13 档表；认不出 ⇒ -1
            return army <= 0 ? CardArmy.Neutral : (CardArmy)army;
        }

        /// <summary>第 <paramref name="i"/> 套的模式（原版 `CardDeck.gameMode`；`0` 经典 / `13` 冲突）。</summary>
        public PlayModes GameModeAt(int i)
        {
            var deck = CollectionData.Raw(i);
            return deck == null ? PlayModes.Classic : (PlayModes)deck.GameMode;
        }

        /// <summary>第 <paramref name="i"/> 套能不能拿出去打 = 原版 `DeckUtility.ValidateDeck(deck, out _)`。
        /// <para>我们 = `DeckRules.Validate(...)` 通过（张数/阵营/同名上限/准入全在里面；⛔ 别在这里再抄一条）。
        /// 解析卡引用走 `CardDatabase.DeckLookup`（**唯一**解析口：先稳定 id、再旧存档的卡名）。</para></summary>
        public bool IsPlayableAt(int i)
        {
            var deck = CollectionData.Raw(i);
            if (deck == null) return false;
            var lookup = CardDatabase.DeckLookup(CardDatabase.Load(), CollectionData.DeckAt(i).Faction);
            return DeckRules.Validate(deck, lookup, deck.GameMode == (int)PlayModes.Skirmish) == DeckError.None;
        }

        /// <summary>按 id 找下标；没有 ⇒ `-1`（同名卡组之间**不歧义**）。</summary>
        public int IndexOfId(string deckId) { return CollectionData.IndexOfDeckId(deckId); }
    }
}
