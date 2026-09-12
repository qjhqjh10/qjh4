// DeckRules.cs — 卡组构筑规则 + 卡组数据模型（**纯逻辑，不依赖 UnityEngine**）
//
// 数字**全部出自原版离线规则书**：
//   `d:/4/Unity/资料/规则书/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md`
//   「游戏模式」那张表（:43-66）。行号写在每个常量后面。
//
// ⚠️ **为什么只能照规则书**：客户端代码里也有这些规则，但都拿不到数 ——
//   · `GameplayVariablesData.GetMaxCopiesInDeck(rarity, type)` 是 LiveOps 服务器下发的
//   · `GameStaticData.maxCardsCopiesInDeck` / `maxLegendaryCopiesInDeck` 是 `static readonly`，
//     值来自配置资源，**DLL 里没有字面量**（只有 `deckSize = 30` 是 const 写死的）
//   规则书是本地唯一的权威来源，和它冲突的一切以它为准。
using System;
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>卡组构筑/校验的失败原因。`None` = 合法。</summary>
    public enum DeckError
    {
        None = 0,
        UnknownCard,        // 卡组里有个 id 不在卡池里
        NoWarlord,
        WarlordNotHero,     // 督军位放了非督军
        DefensiveMissing,   // 经典/遭遇模式要 1 张防御卡
        DefensiveNotDefence,
        WrongFaction,       // 卡与督军不同阵营（规则书:53「所有卡必须与督军同阵营」）
        TooManyCards,       // 超过 30（或遭遇模式 12）
        TooFewCards,        // 不足
        CopyLimitExceeded,  // 某张卡超过同名上限
        WarlordInCards,     // 督军/防御卡又混进了普通卡位
        WarlordAlreadySet,  // 已经有督军了，再选就是换 —— 换要走 SetWarlord（会清掉不合阵营的卡）
        DefensiveAlreadySet,
    }

    public static class DeckRules
    {
        // ── 经典模式（规则书:47-53）──────────────────────────────────────────
        public const int ClassicCards = 30;         // :47「1 督军 + 1 防御卡 + 30 张阵营卡」
        public const int ClassicHandStart = 3;      // :48「起手 3 张」
        public const int ClassicHandLimit = 10;     // :48「上限 10」
        public const int DrawPerTurn = 1;           // :48「每回合抽 1 张」
        public const int OvertimeEnergy = 10;       // :51「后手玩家最大能量达 10 时进入」
        public const int OvertimeDrawPerTurn = 2;   // :51「双方每回合抽 2 张」

        // ── 遭遇模式 Skirmish（规则书:57-64）─────────────────────────────────
        public const int SkirmishCards = 12;            // :57
        public const int SkirmishHandStart = 4;         // :58
        public const int SkirmishHandLimit = 8;         // :58
        public const int SkirmishWarlordHealthPenalty = 10;  // :62「督军初始生命少 10」
        public const int SkirmishEnergyPerTurn = 2;     // :60「每回合最大能量 +2」
        public const int SkirmishLegendaryLimit = 4;    // :64「传说卡最多 4 张（不含督军）」

        /// <summary>骷髅头：把敌方督军生命削到这些值时各得 1 个（规则书:36）
        /// —— 对局结算界面那三个骷髅就是这么来的（运行时 dump 里 `skull1..3`）</summary>
        public static readonly int[] SkullThresholds = { 20, 10, 0 };

        /// <summary>
        /// 同名卡在卡组里的上限（规则书:53）：
        /// **普通 / 稀有 / 史诗 最多 2 张；传说 最多 1 张**。
        ///
        /// 遭遇模式另有一条「传说卡最多 4 张（不含督军）」—— 那条是**整副牌里传说卡的总数**，
        /// 不是单卡同名上限，含义在规则书里没有更细的说明，所以只在
        /// <see cref="LegendaryTotalLimit"/> 里单独暴露，不混进这里。
        /// </summary>
        public static int CopyLimit(string rarity)
        {
            return IsLegendary(rarity) ? 1 : 2;
        }

        /// <summary>遭遇模式：整副牌最多几张传说（规则书:64）。经典模式返回 int.MaxValue（无此限制）。</summary>
        public static int LegendaryTotalLimit(bool skirmish)
        {
            return skirmish ? SkirmishLegendaryLimit : int.MaxValue;
        }

        /// <summary>标准模式要几张阵营卡</summary>
        public static int CardCount(bool skirmish)
        {
            return skirmish ? SkirmishCards : ClassicCards;
        }

        public static bool IsLegendary(string rarity)
        {
            return string.Equals(rarity, "legendary", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 校验一副卡组。`lookup` 把卡 id 映射成卡（取不到返回 null）。
        ///
        /// 顺序有意为之：**先查卡在不在池子里，再查数量，最后查上限** ——
        /// 和 `rule_core.play_card` 的「先费用后格位」是同一个思路：
        /// 后面的判据依赖前面的结果，顺序反了会给出误导性的错误码。
        /// </summary>
        public static DeckError Validate(PlayerDeck deck, Func<string, CardDef> lookup, bool skirmish = false)
        {
            if (deck == null) return DeckError.NoWarlord;
            if (lookup == null) throw new ArgumentNullException(nameof(lookup));

            // ① 全部 id 都要能在卡池里找到
            var warlord = deck.WarlordId == null ? null : lookup(deck.WarlordId);
            var defensive = deck.DefensiveId == null ? null : lookup(deck.DefensiveId);
            var cards = new List<CardDef>();
            foreach (var id in deck.CardIds)
            {
                var c = lookup(id);
                if (c == null) return DeckError.UnknownCard;
                cards.Add(c);
            }

            // ② 督军位
            if (warlord == null) return DeckError.NoWarlord;
            if (warlord.Type != "hero") return DeckError.WarlordNotHero;

            // ③ 防御卡位（两种模式都要求 1 张，规则书 :47 / :57）
            if (defensive == null) return DeckError.DefensiveMissing;
            if (defensive.Type != "defence") return DeckError.DefensiveNotDefence;

            // ④ 张数
            int want = CardCount(skirmish);
            if (cards.Count > want) return DeckError.TooManyCards;
            if (cards.Count < want) return DeckError.TooFewCards;

            // ⑤ 阵营：所有卡必须与督军同阵营（规则书:53）
            foreach (var c in cards)
                if (!SameFaction(c.Faction, warlord.Faction)) return DeckError.WrongFaction;

            // ⑥ 督军/防御卡不能又混进普通卡位
            foreach (var c in cards)
                if (c.Type == "hero" || c.Type == "defence") return DeckError.WarlordInCards;

            // ⑦ 同名上限
            var copies = new Dictionary<string, int>();
            foreach (var c in cards)
            {
                int n;
                copies.TryGetValue(c.Id, out n);
                copies[c.Id] = n + 1;
            }
            foreach (var kv in copies)
            {
                var c = lookup(kv.Key);
                if (kv.Value > CopyLimit(c.Rarity)) return DeckError.CopyLimitExceeded;
            }

            // ⑧ 遭遇模式的传说卡总数
            int legendaries = 0;
            foreach (var c in cards) if (IsLegendary(c.Rarity)) legendaries++;
            if (legendaries > LegendaryTotalLimit(skirmish)) return DeckError.CopyLimitExceeded;

            return DeckError.None;
        }

        /// <summary>
        /// 阵营名比较。卡池里的阵营是 `SaimHann` / `Ultramarines` 这种，大小写统一按不敏感比。
        /// 留这个函数是为了**只有一处**在做比较 —— 将来要加别名表也只改这里。
        /// </summary>
        public static bool SameFaction(string a, string b)
        {
            return string.Equals(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>给 UI 用的人话。`None` 返回空串。</summary>
        public static string Describe(DeckError e)
        {
            switch (e)
            {
                case DeckError.None: return "";
                case DeckError.UnknownCard: return "卡组里有卡不在卡池中";
                case DeckError.NoWarlord: return "还没有选督军";
                case DeckError.WarlordNotHero: return "督军位放的不是督军";
                case DeckError.DefensiveMissing: return "还缺 1 张防御卡";
                case DeckError.DefensiveNotDefence: return "防御卡位放的不是防御卡";
                case DeckError.WrongFaction: return "有卡和督军不同阵营";
                case DeckError.TooManyCards: return "卡组张数超了";
                case DeckError.TooFewCards: return "卡组张数不够";
                case DeckError.CopyLimitExceeded: return "有卡超过了同名上限（传说 1 张，其余 2 张）";
                case DeckError.WarlordInCards: return "督军/防御卡不能放在普通卡位里";
                case DeckError.WarlordAlreadySet: return "已经选过督军了（换督军要先把原来的撤掉）";
                case DeckError.DefensiveAlreadySet: return "已经有防御卡了（不能带两张）";
                default: return e.ToString();
            }
        }
    }

    /// <summary>
    /// 一副卡组。**只存 id，不存 CardDef** —— 存对象的话卡池一变就对不上了，
    /// 而且序列化出来是一大坨。UI 和引擎都通过 id 去卡池查。
    /// </summary>
    [Serializable]
    public class PlayerDeck
    {
        public string Name = "新卡组";
        public string WarlordId;
        public string DefensiveId;
        public List<string> CardIds = new List<string>();

        public PlayerDeck() { }

        public PlayerDeck(string name, string warlordId, string defensiveId, IEnumerable<string> cards)
        {
            Name = string.IsNullOrEmpty(name) ? "新卡组" : name;
            WarlordId = warlordId;
            DefensiveId = defensiveId;
            CardIds = cards == null ? new List<string>() : new List<string>(cards);
        }

        public PlayerDeck Clone()
        {
            return new PlayerDeck(Name, WarlordId, DefensiveId, CardIds);
        }

        /// <summary>放进/拿走一张普通卡。返回是否真的变了（UI 用来决定要不要重排）。</summary>
        public bool AddCard(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            CardIds.Add(id);
            return true;
        }

        public bool RemoveCard(string id)
        {
            int i = CardIds.LastIndexOf(id);
            if (i < 0) return false;
            CardIds.RemoveAt(i);
            return true;
        }

        public int CountOf(string id)
        {
            int n = 0;
            foreach (var c in CardIds) if (c == id) n++;
            return n;
        }
    }
}
