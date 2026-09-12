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
        public const int ClassicDeckSize = 30;

        /// <summary>
        /// 同名单卡在卡组里的上限（设计文档 §2.4）。
        /// 数据里 `rarity` 有几种脏值 —— `''`（239 张）和 `'defence'`（102 张，OCR 把类型写进了稀有度）
        /// 都按 common 处理，不然会上限为 0 直接把卡排掉。
        /// </summary>
        public static int DeckLimit(string rarity)
        {
            switch (rarity)
            {
                case "legendary": return 1;
                case "epic": return 2;
                case "rare": return 2;
                default: return 2;      // common / '' / defence
            }
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

            UnityEngine.Debug.Log($"[RuleEngine] 凑牌组：{faction} {deck.Count} 张"
                                + $"（督军 {deck[0].Name}，单位池 {units.Count} 张，只会用上 {used.Count} 种）");
            return deck;
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
