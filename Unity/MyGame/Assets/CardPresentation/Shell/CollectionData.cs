// CollectionData.cs — 收藏线（`Collection Menu Variant`）的**本地数据源**
//
// ============================ 原版是什么（A4 实证） ============================
// 原版这几页的数据**全是本地单例**、**没有 `handler.`**（这一点和锻造厂/商店不同）：
//   · Deck 页 = 卡组列表（我们已有 `DeckLibrary` / `DeckStore` ✓ 直接搬）
//   · Cards 页 = `CollectionManager.AllCardCollection`（我们 = `CardDatabase.Load()` ✓）
//   · CardBacks 页 = `CollectionManager.Instance.FullCosmeticCollection.OfType<CosmeticItemCardback>()`
//     （图在 `bundle_cosmeticscardbacksimages_assets_all/Texture2D/` **233 张 —— 还没导进工程**）
//   · Alternate Art 页 = `AllCardCollection` 按 `alternateArtStyles[currentStyleIndex]` 过滤（**待查**）
//
// ⚠️ **`PlayerDeck` 没有 id 字段**（`RuleEngine/Core/DeckRules.cs:223`：只有 Name/WarlordId/DefensiveId/CardIds）
//    ⇒ 本轮拿 **Name** 当稳定标识（重命名会让选中态丢，**如实记**；将来加 id 要动存档格式）。
using System.Collections.Generic;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>收藏线要用的那几样数据（纯静态、可 Reset，自检之间互不影响）。</summary>
    public static class CollectionData
    {
        /// <summary>一格卡组要显示的东西。</summary>
        public struct DeckInfo
        {
            public string Name;        // 卡组名（**本轮兼作 id** —— `PlayerDeck` 没有 id 字段）
            public string WarlordId;
            public string Faction;     // 从督军卡推（`CardDef.Faction`），查不到就是空串
            public int Count;          // 普通卡张数（不含督军/防御卡）
            /// <summary>这副卡组选的卡背（空 = 没选过，用该阵营默认）。
            /// 取值一律走 `CardArt.DeckCardback(CardbackId, Faction)` —— 判据只那一处。</summary>
            public string CardbackId;
        }

        static DeckLibrary _lib;
        static DeckEditorState _lookup;      // 只借它做 id → `CardDef`（`Find`）

        static DeckLibrary Lib { get { if (_lib == null) _lib = DeckLibrary.Load(); return _lib; } }
        static DeckEditorState Lookup
        {
            get { if (_lookup == null) _lookup = new DeckEditorState(CardDatabase.Load()); return _lookup; }
        }

        public static int DeckCount() { return Lib.Decks.Count; }
        public static int CurrentIndex() { return Lib.CurrentIndex; }
        public static PlayerDeck Raw(int i)
        {
            return (i >= 0 && i < Lib.Decks.Count) ? Lib.Decks[i] : null;
        }

        public static DeckInfo DeckAt(int i)
        {
            var info = new DeckInfo();
            var d = Raw(i);
            if (d == null) return info;
            info.Name = d.Name ?? "";
            info.WarlordId = d.WarlordId;
            info.Count = d.CardIds != null ? d.CardIds.Count : 0;
            info.CardbackId = d.CardbackId;
            var hero = string.IsNullOrEmpty(d.WarlordId) ? null : Lookup.Find(d.WarlordId);
            info.Faction = hero != null ? hero.Faction : "";
            return info;
        }

        public static void Select(int i) { Lib.Select(i); }

        /// <summary>按**卡组名**反查下标（查不到给 −1）。
        /// ⚠️ 又是「名字当 id」那条老账（`PlayerDeck` 没有 id 字段，见文件头）——
        /// 同名卡组**取第一个**；重命名会让外面拿着的名字失效，**如实记**。
        /// 用途：`Deck Selection Popup` 的回调只给一份 `DeckInfo`，要拿回下标。</summary>
        public static int IndexOf(string deckName)
        {
            if (string.IsNullOrEmpty(deckName)) return -1;
            for (int i = 0; i < Lib.Decks.Count; i++)
                if (Lib.Decks[i].Name == deckName) return i;
            return -1;
        }

        /// <summary>卡 id → 卡定义（`Deck info Popup` 要把卡组里的 id 列表画出来）。查不到给 null。</summary>
        public static CardDef Card(string id) { return string.IsNullOrEmpty(id) ? null : Lookup.Find(id); }
        /// <summary>督军卡（没有 / 查不到给 null）。</summary>
        public static CardDef Warlord(int i)
        {
            var d = Raw(i);
            return d == null ? null : Card(d.WarlordId);
        }
        /// <summary>删一套卡组。**只剩一套时不许删**（`DeckLibrary.Delete` 的规矩）。返回删没删成。</summary>
        public static bool DeleteDeck(int i) { bool ok = Lib.Delete(i); if (ok) Lib.Save(); return ok; }
        /// <summary>复制一套卡组，返回新卡组名（失败给空串）。</summary>
        public static string DuplicateDeck(int i)
        {
            var d = Lib.Duplicate(i);
            if (d == null) return "";
            Lib.Save();
            return d.Name;
        }

        /// <summary>导入一条卡组串（原版 `MenuDeck/Share/*` 那套）。成功返回新卡组名；失败返回空串并给**人话**原因。
        /// 🔴 **判据与错误文案与卡组编辑那边逐字一致**（`DeckRuntime.TryImport`）——
        /// 两处各写一套迟早不一致（CLAUDE.md §三）。</summary>
        public static string ImportDeck(string s, out string why)
        {
            why = "";
            if (string.IsNullOrWhiteSpace(s)) { why = "先粘贴卡组串"; return ""; }
            var deck = DeckLibrary.ImportString(s, Card);
            if (deck == null) { why = "这不是一条合法的卡组串"; return ""; }
            Lib.Add(deck);
            Lib.Save();
            return deck.Name;
        }

        /// <summary>新建一套卡组（原版走 `Deck Editing Menu` 的「Create」，本轮只建卡组、不进编辑）。</summary>
        public static string CreateDeck()
        {
            string name = Lib.UniqueName("新卡组");
            var d = Lib.Create(name);
            Lib.Save();
            return d != null ? d.Name : name;
        }

        /// <summary>自检用：把缓存丢掉，下次重新从存档读。</summary>
        /// <summary>「从收藏进编辑」的**交接**：收藏窗点「编辑」时把这一套的下标写在这里，
        /// `DeckRuntime.Build` 开局读它、读完清成 −1。
        /// ⚠️ **静态字段跨场景存活**（`DeckEditor` 是**另一个场景**）—— 这是最小代价的做法
        /// （原版在同一扇窗里换页、不需要交接；我们的编辑器是独立场景，见正本 §七 的那处偏离）。
        /// ⚠️ 批处理下收藏窗**只交接、不切场景** ⇒ 自检验的就是这个下标。</summary>
        public static int PendingEditDeck = -1;

        public static void ResetForTest() { _lib = null; _lookup = null; PendingEditDeck = -1; }
    }
}
