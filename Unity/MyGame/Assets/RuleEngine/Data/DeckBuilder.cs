// DeckBuilder.cs — 凑一副能打的牌组
//
// 玩法线的目标之一是「打完一局分出胜负」，那就得先有牌组。
// 这里只做**最小可用**的构造：一个督军 + 一堆单位卡，够打就行。
// 真正的构筑（费用曲线、卡组模板、稀有度限制的完整规则）不在 v1 范围内 ——
// `数据/游戏数据/prebuilt_decks_full.json` 里有原版 472 副模板，将来可以直接用。
using System;
using System.Collections.Generic;

namespace RuleEngine
{
    public static class DeckBuilder
    {
        /// <summary>经典模式卡组张数（设计文档 §2.4 MODE_RULES）</summary>
        public const int ClassicDeckSize = DeckRules.ClassicCards;

        /// <summary>
        /// 同名单卡在卡组里的上限。
        /// ⚠️ **判据只有一处** —— 转发给 `Core/DeckRules.CopyLimit`（照规则书:53 写的）。
        ///    这里保留同名方法只是为了不改动既有调用点；**别在这里另写一套**。
        /// </summary>
        public static int DeckLimit(string rarity)
        {
            return DeckRules.CopyLimit(rarity);
        }

        /// <summary>
        /// 从卡池凑一副起始牌组：**第 0 张是督军**（`type == "hero"`），其余是单位卡。
        ///
        /// ⚠️ **只收单位卡**：v1 的战术卡效果还没实现（`RuleCodes.ErrUnimplemented`），
        ///    掺进来只会让玩家白白浪费能量。等战术做完了把 `unitsOnly` 去掉即可。
        /// </summary>
        public static List<CardDef> StarterDeck(IEnumerable<CardDef> pool, string faction,
                                                int size = ClassicDeckSize, Random rng = null,
                                                bool unitsOnly = true)
        {
            rng = rng ?? new Random(0);

            var heroes = new List<CardDef>();
            var units = new List<CardDef>();
            foreach (var c in pool)
            {
                if (c == null || c.Faction != faction) continue;
                if (c.Type == "hero") heroes.Add(c);
                else if (c.Type == "unit") units.Add(c);
            }
            if (unitsOnly)
            {
                // 已经是只收 unit 了 —— 这个开关留着是为了将来放战术进来
            }

            var deck = new List<CardDef>();
            if (heroes.Count == 0)
            {
                // 没督军的阵营凑不出合法牌组 —— 用默认督军兜底（RuleCore 会自动补）
                UnityEngine.Debug.LogWarning($"[RuleEngine] 阵营 {faction} 没有 hero 卡，牌组用默认督军");
            }
            else
            {
                deck.Add(heroes[rng.Next(heroes.Count)]);
            }

            Shuffle(units, rng);

            // 先按稀有度上限收一轮，不够再放宽
            var used = new Dictionary<string, int>();
            foreach (var c in units)
            {
                if (deck.Count >= size) break;
                int n;
                used.TryGetValue(c.Name, out n);
                if (n >= DeckLimit(c.Rarity)) continue;
                used[c.Name] = n + 1;
                deck.Add(c);
            }
            if (deck.Count < size)
            {
                foreach (var c in units)
                {
                    if (deck.Count >= size) break;
                    int n;
                    used.TryGetValue(c.Name, out n);
                    if (n >= DeckLimit(c.Rarity) + 2) continue;   // 放宽到 +2，还不至于全是同一张
                    used[c.Name] = n + 1;
                    deck.Add(c);
                }
            }
            if (deck.Count < size)
            {
                foreach (var c in units)   // 实在不够就无限制地塞满（小阵营会遇到）
                {
                    if (deck.Count >= size) break;
                    deck.Add(c);
                }
            }

            if (deck.Count == 0)
            {
                // ⚠️ 原来的 `deck[0].Name` 在这种情况下直接抛 IndexOutOfRange ——
                //    而「阵营名传错」（比如拿自设计阵营的名字去查原版卡池）正是最常见的触发方式。
                UnityEngine.Debug.LogError($"[RuleEngine] 阵营 `{faction}` 在卡池里一张卡都没有"
                                         + "（既没有 hero 也没有 unit）—— 凑不出牌组，返回空表。"
                                         + "多半是阵营名不对：自设计的两套在 `StarterCards`，"
                                         + "原版 13 个在 `cards_engine.json`");
                return deck;
            }
            UnityEngine.Debug.Log($"[RuleEngine] 凑牌组：{faction} {deck.Count} 张"
                                + $"（督军 {deck[0].Name}，单位池 {units.Count} 张，只会用上 {used.Count} 种）");
            return deck;
        }

        /// <summary>
        /// 把**玩家存档里的卡组**（`PlayerDeck`，存的是 id 字符串）展开成引擎要的卡表。
        ///
        /// **第 0 张是督军** —— 和 <see cref="StarterDeck"/> 同一个约定（`RuleCore.BuildPlayer` 认这个：
        /// 它把碰到的**第一个 `hero`** 当督军、其余全塞进抽牌堆）。
        ///
        /// ⚠️ **只收单位卡**：v1 的战术卡（448 张）效果是 `ErrUnimplemented`、
        ///    防御卡引擎**根本没有对应机制**（`RuleCore` 里除了 `DeckRules` 的合法性校验，
        ///    没有任何地方认识 `defence`）。放进来就是一堆打不出、也结算不了的死牌。
        ///    **但绝不静默丢** —— 丢了几张、什么类型，这里会打出来，调用方也拿得到。
        ///
        /// 合法性判定**不在这儿**：先跑 `DeckRules.Validate`，这里只负责展开。
        /// </summary>
        /// <param name="skipped">被丢掉的卡（类型引擎还不支持）。可以为 null。</param>
        public static List<CardDef> FromDeck(IEnumerable<CardDef> pool, PlayerDeck deck,
                                             List<string> skipped = null)
        {
            var index = new Dictionary<string, CardDef>();
            foreach (var c in pool)
                if (c != null && !index.ContainsKey(c.Id)) index[c.Id] = c;

            var list = new List<CardDef>();
            if (deck == null) return list;

            var warlord = Lookup(index, deck.WarlordId);
            if (warlord == null || warlord.Type != "hero")
                UnityEngine.Debug.LogError($"[RuleEngine] 卡组的督军 `{deck.WarlordId}` 在卡池里找不到"
                                         + "（或不是 hero）—— 引擎会退回默认督军，这局打得不是你要的那套");
            list.Add(warlord);      // 可能是 null，`BuildPlayer` 会退回默认督军

            if (!string.IsNullOrEmpty(deck.DefensiveId))
                Note(skipped, deck.DefensiveId, "防御卡");

            foreach (var id in deck.CardIds)
            {
                var c = Lookup(index, id);
                if (c == null)
                {
                    UnityEngine.Debug.LogError($"[RuleEngine] 卡组里的 `{id}` 在卡池里找不到 —— 这张牌被丢了");
                    continue;
                }
                if (c.Type != "unit") { Note(skipped, id, c.Type); continue; }
                list.Add(c);
            }
            return list;
        }

        static CardDef Lookup(Dictionary<string, CardDef> index, string id)
        {
            CardDef c;
            return (id != null && index.TryGetValue(id, out c)) ? c : null;
        }

        static void Note(List<string> skipped, string id, string type)
        {
            if (skipped != null) skipped.Add(id + "(" + type + ")");
        }

        /// <summary>法术力曲线诊断 —— 自检里打出来看牌组是不是全是大费卡</summary>
        public static int[] CostCurve(IEnumerable<CardDef> deck, int maxCost = 10)
        {
            var curve = new int[maxCost];
            foreach (var c in deck)
            {
                if (c == null || c.Type == "hero") continue;
                int i = Math.Min(Math.Max(c.Cost, 0), maxCost - 1);
                curve[i]++;
            }
            return curve;
        }

        static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var t = list[i]; list[i] = list[j]; list[j] = t;
            }
        }
    }
}
