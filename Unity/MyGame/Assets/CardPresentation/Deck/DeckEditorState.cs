// DeckEditorState.cs — 卡组编辑界面的**状态核心**（不碰绘制，能单测）
//
// 为什么把状态和绘制分开：绘制要靠 ImageQuad/Label 摆一堆世界空间 quad，在批处理里
// 只能靠截图验收；而「筛选对不对、加牌删牌守不守规则、卡组合不合法」这些是**纯逻辑**，
// 可以断言。分开之后自检能覆盖绝大部分行为，截图只用来验收观感。
//
// 界面结构照原版的 prefab 节点树（见 `资料/卡组编辑_原版数值与实现方案.md` 第三节）：
//   Deck Editing Menu > Card Display > Scroll View（卡列表）
//   Deck Editing Menu > Card Filters > Name/Army/Rarity/Cost/Type（筛选）
//   Deck Editing Menu > Sidebar > Deck Details > Deck List drawer（卡组内容）
//
// ⚠️ 原版筛选项里有 `Owned Toggle` / `Upgradable Toggle`（`DeckEditingPanel.showOnlyOwnedCards`）——
//    **我们不做**：单机没有开包/合成，1131 张全算已拥有（见方案文档第六节）。
using System;
using System.Collections.Generic;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>卡列表的筛选条件。空/负值表示「不限」。</summary>
    [Serializable]
    public struct DeckFilter
    {
        public string Name;      // 卡名子串，大小写不敏感
        public string Faction;   // "" = 不限
        public string Rarity;    // "" = 不限
        public string Type;      // "" = 不限；unit / tactic / hero / defence
        public int Cost;         // -1 = 不限

        public static DeckFilter None
        {
            get { return new DeckFilter { Name = "", Faction = "", Rarity = "", Type = "", Cost = -1 }; }
        }

        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Faction)
                    && string.IsNullOrEmpty(Rarity) && string.IsNullOrEmpty(Type) && Cost < 0;
            }
        }
    }

    /// <summary>
    /// 卡组编辑器的状态。**改这个类就要加断言**（`DeckScene.Run` 里那一段）。
    /// </summary>
    public class DeckEditorState
    {
        public const int AnyCost = -1;

        readonly List<CardDef> _pool = new List<CardDef>();
        readonly Dictionary<string, CardDef> _byId = new Dictionary<string, CardDef>();

        public DeckEditorState(IEnumerable<CardDef> pool)
        {
            if (pool != null)
                foreach (var c in pool)
                {
                    if (c == null || string.IsNullOrEmpty(c.Id)) continue;
                    if (_byId.ContainsKey(c.Id)) continue;      // 卡池里真有重名就取第一张
                    _byId[c.Id] = c;
                    _pool.Add(c);
                }
            Deck = new PlayerDeck();
            // ⚠️ 必须显式初始化 —— `default(DeckFilter)` 的 `Cost` 是 0，而 0 是个**合法费用**，
            //    不初始化的话筛选默认变成「只看 0 费卡」（自检抓到过：1127 张池子筛成 103 张）
            Filter = DeckFilter.None;
        }

        public IReadOnlyList<CardDef> Pool { get { return _pool; } }
        public int PoolCount { get { return _pool.Count; } }

        /// <summary>正在编辑的卡组。</summary>
        public PlayerDeck Deck { get; private set; }

        public DeckFilter Filter { get; set; }

        /// <summary>是否处于「遭遇模式」规则下（12 张）。默认经典。</summary>
        public bool Skirmish;

        public CardDef Find(string id)
        {
            CardDef c;
            return (id != null && _byId.TryGetValue(id, out c)) ? c : null;
        }

        // ------------------------------------------------------------ 卡组

        public void NewDeck(string name)
        {
            Deck = new PlayerDeck(string.IsNullOrEmpty(name) ? "新卡组" : name, null, null, null);
        }

        public void LoadDeck(PlayerDeck deck)
        {
            Deck = deck ?? new PlayerDeck();
            if (Deck.CardIds == null) Deck.CardIds = new List<string>();
        }

        public DeckError Validate()
        {
            return DeckRules.Validate(Deck, Find, Skirmish);
        }

        /// <summary>卡组还差几张（负数=超了）。UI 上「12/30」那个计数用它。</summary>
        public int SlotsLeft { get { return DeckRules.CardCount(Skirmish) - Deck.CardIds.Count; } }

        public int DeckCount { get { return Deck.CardIds.Count; } }
        public int MaxDeckCount { get { return DeckRules.CardCount(Skirmish); } }

        // ------------------------------------------------------------ 加/删

        /// <summary>
        /// 能不能把这张加进卡组。**判据全部转发给 `DeckRules`**，这里只做「单张牌」这一层的检查 ——
        /// 整副牌的合法性另有 <see cref="Validate"/>，两件事别混。
        /// </summary>
        public DeckError CanAdd(CardDef c)
        {
            if (c == null) return DeckError.UnknownCard;

            // 督军和防御卡走各自的位子，不占普通卡位，也各只能有一个。
            // 换督军不走这里 —— 走 SetWarlord（它会把不合阵营的卡一并清掉）。
            if (c.Type == "hero")
                return Deck.WarlordId == null ? DeckError.None : DeckError.WarlordAlreadySet;
            if (c.Type == "defence")
                return Deck.DefensiveId == null ? DeckError.None : DeckError.DefensiveAlreadySet;

            if (Deck.CardIds.Count >= MaxDeckCount) return DeckError.TooManyCards;

            // 阵营：以督军为准；还没选督军时先按卡自己的阵营（选督军时会清掉不合的）
            if (!string.IsNullOrEmpty(Deck.WarlordId))
            {
                var w = Find(Deck.WarlordId);
                if (w != null && !DeckRules.SameFaction(w.Faction, c.Faction)) return DeckError.WrongFaction;
            }

            if (Deck.CountOf(c.Id) >= DeckRules.CopyLimit(c.Rarity)) return DeckError.CopyLimitExceeded;
            return DeckError.None;
        }

        /// <summary>加一张牌。<paramref name="why"/> 拿不合法原因（合法时为空串）。</summary>
        public bool TryAdd(CardDef c, out string why)
        {
            var e = CanAdd(c);
            why = DeckRules.Describe(e);
            if (e != DeckError.None) return false;

            if (c.Type == "hero") Deck.WarlordId = c.Id;
            else if (c.Type == "defence") Deck.DefensiveId = c.Id;
            else Deck.CardIds.Add(c.Id);
            return true;
        }

        public bool TryAdd(string id, out string why) { return TryAdd(Find(id), out why); }

        /// <summary>
        /// 选督军。**会把不合阵营的卡清掉** —— 规则书 :53「所有卡必须与督军同阵营」，
        /// 留着它们的话卡组一直是非法的，不如直接清并让 UI 报一声。
        /// 返回被清掉的张数。
        /// </summary>
        public int SetWarlord(string warlordId)
        {
            var w = Find(warlordId);
            if (w == null || w.Type != "hero") return 0;
            Deck.WarlordId = w.Id;

            int removed = 0;
            for (int i = Deck.CardIds.Count - 1; i >= 0; i--)
            {
                var c = Find(Deck.CardIds[i]);
                if (c == null || !DeckRules.SameFaction(c.Faction, w.Faction)) { Deck.CardIds.RemoveAt(i); removed++; }
            }
            var d = Find(Deck.DefensiveId);
            if (d != null && !DeckRules.SameFaction(d.Faction, w.Faction)) { Deck.DefensiveId = null; removed++; }
            return removed;
        }

        /// <summary>从卡组拿走一张（普通卡位；督军/防御卡要单独清）。</summary>
        public bool TryRemove(CardDef c)
        {
            if (c == null) return false;
            if (c.Type == "hero") return Deck.WarlordId == c.Id && ClearWarlord();
            if (c.Type == "defence") return Deck.DefensiveId == c.Id && ClearDefensive();
            return Deck.RemoveCard(c.Id);
        }

        public bool ClearWarlord() { if (Deck.WarlordId == null) return false; Deck.WarlordId = null; return true; }
        public bool ClearDefensive() { if (Deck.DefensiveId == null) return false; Deck.DefensiveId = null; return true; }

        /// <summary>清空整副（保留名字，换督军时用得上）。</summary>
        public void ClearCards()
        {
            Deck.CardIds.Clear();
            Deck.WarlordId = null;
            Deck.DefensiveId = null;
        }

        // ------------------------------------------------------------ 筛选

        /// <summary>按当前筛选条件过一遍卡池。顺序保持卡池原序 —— **不排序**，
        /// 原版有没有排序规则没查到（见方案文档；`DeckCollectionDisplay.SaveDeckOrder` 只管卡组列表的拖拽序）。</summary>
        public List<CardDef> VisibleCards()
        {
            var outList = new List<CardDef>();
            var f = Filter;
            string needle = string.IsNullOrEmpty(f.Name) ? null : f.Name.ToLowerInvariant();

            foreach (var c in _pool)
            {
                if (needle != null && (c.Name == null || c.Name.ToLowerInvariant().IndexOf(needle, StringComparison.Ordinal) < 0))
                    continue;
                if (!string.IsNullOrEmpty(f.Faction) && !DeckRules.SameFaction(c.Faction, f.Faction)) continue;
                if (!string.IsNullOrEmpty(f.Rarity) && !string.Equals(c.Rarity ?? "", f.Rarity, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrEmpty(f.Type) && c.Type != f.Type) continue;
                if (f.Cost >= 0 && c.Cost != f.Cost) continue;
                outList.Add(c);
            }
            return outList;
        }

        /// <summary>卡池里出现过的阵营（给筛选下拉用），按出现顺序。</summary>
        public List<string> Factions()
        {
            var seen = new List<string>();
            foreach (var c in _pool)
                if (!string.IsNullOrEmpty(c.Faction) && !seen.Contains(c.Faction)) seen.Add(c.Faction);
            seen.Sort(StringComparer.Ordinal);
            return seen;
        }

        /// <summary>卡池里出现过的费用值（给筛选下拉用），升序。</summary>
        public List<int> Costs()
        {
            var seen = new List<int>();
            foreach (var c in _pool) if (!seen.Contains(c.Cost)) seen.Add(c.Cost);
            seen.Sort();
            return seen;
        }

        // ------------------------------------------------------------ 翻页

        /// <summary>一屏放几张。网格 = 4 列 × 3 行，和原版 `Collection Card` 350×512 在 1920 宽下排得下的张数一致。
        /// ⚠️ 原版是**回收滚动列表**（`RecyclableScrollRect`），我们 v1 用翻页 —— 差别在滚动手感，不在功能。</summary>
        public const int PageSize = 12;

        int _page;

        /// <summary>当前页（0 基）。筛选条件一变由 <see cref="ResetPage"/> 归零。</summary>
        public int Page { get { return _page; } }

        /// <summary>筛选命中多少张</summary>
        public int MatchCount { get { return VisibleCards().Count; } }

        public int PageCount
        {
            get
            {
                int n = MatchCount;
                return n == 0 ? 1 : (n + PageSize - 1) / PageSize;
            }
        }

        /// <summary>把页码夹回合法范围。翻页/改筛选之后都要调 —— 筛选后张数变少时页码可能越界。</summary>
        public void ClampPage()
        {
            int pc = PageCount;
            if (_page < 0) _page = 0;
            if (_page >= pc) _page = pc - 1;
            if (_page < 0) _page = 0;
        }

        public void ResetPage() { _page = 0; }

        public bool NextPage()
        {
            if (_page + 1 >= PageCount) return false;
            _page++;
            return true;
        }

        public bool PrevPage()
        {
            if (_page <= 0) return false;
            _page--;
            return true;
        }

        /// <summary>当前页要显示的卡（已经过筛选）。</summary>
        public List<CardDef> PageCards()
        {
            var all = VisibleCards();
            var outp = new List<CardDef>(PageSize);
            int start = _page * PageSize;
            for (int i = start; i < start + PageSize && i < all.Count; i++) outp.Add(all[i]);
            return outp;
        }

        /// <summary>改筛选条件（会自动归零页码 —— 不归零的话会停在一个筛选后不存在的页上）。</summary>
        public void SetFilter(DeckFilter f)
        {
            Filter = f;
            ResetPage();
        }

        /// <summary>当前卡组的费用曲线：费用 → 张数（UI 上那排柱子用它）。
        /// 对照原版 `DeckEnergyCostDrawer`（`NUMBER_OF_CARDS_FOR_NORMALIZATION = 10`，柱子按 10 张归一化）。</summary>
        public int[] CostCurve()
        {
            const int maxCost = 20;                  // 卡池里费用没超过 20 的
            var curve = new int[maxCost + 1];
            foreach (var id in Deck.CardIds)
            {
                var c = Find(id);
                if (c == null) continue;
                int k = c.Cost < 0 ? 0 : (c.Cost > maxCost ? maxCost : c.Cost);
                curve[k]++;
            }
            return curve;
        }
    }
}
