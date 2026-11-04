// BoosterPrebuiltDeck.cs —— **预组卡组奖励**（B143 · `B142-REWARD-INVENTORY` Step 4）
//
// ============================ 判据（原版逐条，⛔ 别在别处再抄一份）============================
//  · `RewardService.CollectDeckReward`（`d:/2/tools/decomp_full/RewardService__CollectDeckReward.c:35-73`），逐句：
//      ① `new CardDeck(deck, PlayerDataManager.allCardCollection)`（`:33`，构造器体
//         `CardDeck__.ctor.c:23` = `DeckBasicSetup(this, name, deck, 0,0,0)`）
//      ② `cardDeck.syncedToServer = false`（`:36`）
//      ③ `InventoryManager.Add<CardDeck>(cardDeck, 1)`（`:41`）—— 原版 `CardDeck` 在 `InventoryManager` 的
//         类型表里映射到 **`DeckInventory`**（`cpp2il/out_cpp2il_dev_ilspy/InventoryManager.cs:1700-1703`）
//      ④ `foreach (card in deck.GetCardLibrary()) CollectItemReward(card, null, 1)`（`:44-60`）
//      ⑤ `CollectItemReward(deck.GetDeckCardback(), null, 1)`（`:62`）
//      ⑥ `CollectItemReward(deck.DeckHero, null, 1)`（`:64`）
//    ⇒ 「发一副预组」= **进卡组库** + **它的每一张卡 / 卡背 / 督军各发 1**。不是「解锁」——原版卡组是**新的一份**。
//  · 数据面：预组 SO（`bundle_prebuiltdecks_assets_all/MonoBehaviour/UM_SK_Starter.json`）字段实读
//    `deckId` / `deckName` / `cardbackId` / `deckHero.targetId` / `cardLibraryIds[12]` / `gameMode` / `isPracticeDeck=0`
//    ⇒ 这是**突袭（Skirmish）池**的预组，不在 `prebuilt_decks.json`（那 103 副是 `isPracticeDeck=1`）里，
//      而在 `Resources/prebuilt_deck_lists.json` 里（已逐字段解析完：12 张卡 / 督军 `UM47` / 卡背 `Cardback_All_Starter`）。
//
// ============================ 为什么另建一个类，而不是改 `PrebuiltDecks` ============================
//  `Shell/PrebuiltDecks.cs:39` 是 `public static class PrebuiltDecks`（**不是 partial**）⇒ 无法在别的文件里扩。
//  ⛔ 直接改那个文件不在本批白名单（且它被预组页的 `All` 列表依赖，混进突袭池会多出一行）。
//  ⇒ 本类只做**只读查找**（`ById`），不往 `PrebuiltDecks.All` 里塞任何东西。
//
// ============================ 自己设计（非原版）============================
//  🔶 自己设计（非原版）：**`PlayerDeck.DeckId` 不复制原版预组的 `deckId`**
//     · 原版 `CardDeck.deckId` 是「这副牌的身份」；我们的 `PlayerDeck.DeckId`（`RuleEngine/Data/DeckLibrary.cs:882`）
//       是**卡组库的本地身份**，`DeckLibrary.EnsureDeckId` 按它去重。同一副预组被发两次 ⇒ 复制同一个 id 会撞 key。
//     · 所以这里留空，交给 `EnsureDeckId` 现发；名字由 `DeckLibrary.Add` 去重（第二次 ⇒ `名字 2`）。
//     · 调度台 2026-11-05 裁定（`B143-R4` 报告 §6·4）。
//  🔶 自己设计（非原版）：**卡背不做「拥有制」**
//     · 原版 `CollectItemReward(deck.GetDeckCardback(), null, 1)` 是往装饰背包里加一条；我们装饰**全解锁**
//       （用户 2026-09-17 边界）⇒ 走 `CollectCosmeticRewardById`（净零 + 出声）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`Resources/prebuilt_deck_lists.json` 的 DTO（只取本件要的三格；
    /// `decks[]` 逐字段复用 <see cref="PrebuiltDecks.Deck"/> —— 那份 DTO 与 `prebuilt_decks.json` 同构）。</summary>
    [Serializable]
    public class PrebuiltDeckListDoc
    {
        public int format;
        public int deckCount;
        public PrebuiltDecks.Deck[] decks;
    }

    /// <summary>预组卡组的**只读查找**（含突袭池那 78 副；`prebuilt_decks.json` 只收练习池 103 副）。</summary>
    public static class BoosterPrebuiltDecks
    {
        /// <summary>`Assets/RuleEngine/Resources/prebuilt_deck_lists.json`。</summary>
        public const string ResourcePath = "prebuilt_deck_lists";

        static Dictionary<string, PrebuiltDecks.Deck> _byId;
        static string _loadError;

        /// <summary>最近一次加载失败的原因（`null` = 成功或还没加载）。</summary>
        public static string LoadError { get { return _loadError; } }

        /// <summary>已装表的副数（未加载 = 0）。</summary>
        public static int Count { get { return _byId != null ? _byId.Count : 0; } }

        /// <summary>按 `deckId` 取一副（懒加载 + 缓存）。</summary>
        public static PrebuiltDecks.Deck ById(string deckId)
        {
            if (string.IsNullOrEmpty(deckId)) return null;
            EnsureLoaded();
            PrebuiltDecks.Deck d;
            return _byId != null && _byId.TryGetValue(deckId, out d) ? d : null;
        }

        /// <summary>自检收尾：丢回「未加载」。</summary>
        public static void ResetForTest() { _byId = null; _loadError = null; }

        static void EnsureLoaded()
        {
            if (_byId != null || _loadError != null) return;
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                _loadError = "`Resources/" + ResourcePath + ".json` 取不到";
                Debug.LogWarning("[BoosterPrebuiltDecks] " + _loadError + " ⇒ 预组奖励一律当「查不到」");
                return;
            }
            try
            {
                var doc = JsonUtility.FromJson<PrebuiltDeckListDoc>(ta.text);
                var map = new Dictionary<string, PrebuiltDecks.Deck>(StringComparer.Ordinal);
                if (doc != null && doc.decks != null)
                    for (int i = 0; i < doc.decks.Length; i++)
                    {
                        var d = doc.decks[i];
                        if (d == null || string.IsNullOrEmpty(d.deckId)) continue;
                        map[d.deckId] = d;
                    }
                _byId = map;
            }
            catch (Exception e)
            {
                _loadError = "解析失败：" + e.Message;
                Debug.LogWarning("[BoosterPrebuiltDecks] " + _loadError);
            }
        }
    }

    public static partial class RewardService
    {
        /// <summary>预组卡组那一档：原版 `CollectDeckReward`（见文件头六句）的本地等价物。
        /// <para>落点：卡组 → `DeckRuntime.SharedLibrary`（= 原版 `DeckInventory` 的等价物）；
        /// 卡 / 督军 / 卡背 → 与 `CollectSingleReward` 同一条发放链（全解锁 ⇒ 净零，但结构照原版）。</para></summary>
        /// <returns>true = 「这个 id 我认得出，这一条到此为止」。</returns>
        internal static bool GrantPrebuiltDeckReward(string id, int quantity)
        {
            if (string.IsNullOrEmpty(id)) return false;

            var spec = BoosterPrebuiltDecks.ById(id);
            if (spec == null)
            {
                // `RewardKindOf` 只有认得出才走到这里；认得出却没数据 ⇒ 如实出声（⛔ 不静默吞掉）
                InventoryManager.Note("rs-item:prebuiltdeck-nodata:" + id,
                    "预组卡组奖励 `" + id + "` 在 `prebuilt_deck_lists.json` 里查不到 ⇒ 这一条**不发**"
                    + "（原版这份表来自预组 SO，本地缺它 = 缺数据，不是「原版没有」）");
                return true;
            }
            if (quantity <= 0)
            {
                InventoryManager.Note("rs-item:prebuiltdeck-nonpositive:" + id,
                    "预组卡组奖励 `" + id + "` 的数量是 " + quantity + " ⇒ 零/负不发（原版 `Add<CardDeck>(deck, 1)`）");
                return true;
            }

            var lib = DeckRuntime.SharedLibrary;
            if (lib == null)
            {
                InventoryManager.Note("rs-item:prebuiltdeck-nolib:" + id,
                    "预组卡组奖励 `" + id + "`：`DeckRuntime.SharedLibrary` 取不到（卡组库没装载）⇒ 这一条**不发**");
                return true;
            }

            int before = lib.Decks.Count;
            for (int n = 0; n < quantity; n++)
            {
                // ① `new CardDeck(prebuiltDeck, allCardCollection)` —— 名字 / 督军 / 防御卡 / 卡表 / 模式
                var deck = new RuleEngine.PlayerDeck(spec.name, spec.heroId, spec.defensiveId,
                                                     spec.cardIds, spec.gameMode);
                deck.CardbackId = spec.cardback;      // 原版 `CardDeck.deckCardback`（我们的读口 = `CardArt.DeckCardback(CardbackId, faction)`）
                // ② `syncedToServer = false` —— 我们没有服务器这一位 ⇒ 不设（本地库天然就是「未同步」）
                // ③ `InventoryManager.Add<CardDeck>(deck, 1)` ⇒ 卡组库
                lib.Add(deck);
                // ④ 逐张卡 ×1（原版 `CollectDeckReward.c:44-60`）
                if (spec.cardIds != null)
                    for (int i = 0; i < spec.cardIds.Length; i++)
                        CollectItemRewardCard(spec.cardIds[i], null, 1);
                // ⑤ 卡背 ×1（`:62`）
                if (!string.IsNullOrEmpty(spec.cardback)) CollectCosmeticRewardById(spec.cardback, 1);
                // ⑥ 督军 ×1（`:64`）
                if (!string.IsNullOrEmpty(spec.heroId)) CollectItemRewardCard(spec.heroId, null, 1);
            }

            Debug.Log("[RewardService] 预组卡组奖励 `" + id + "` +" + quantity
                    + " ⇒ `DeckRuntime.SharedLibrary` 卡组数 " + before + " → " + lib.Decks.Count
                    + "（" + (spec.name ?? id) + " · " + (spec.cardIds != null ? spec.cardIds.Length : 0) + " 张 · 督军 "
                    + (spec.heroId ?? "<无>") + "）");
            return true;
        }
    }
}
