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
            var tactics = new List<CardDef>();
            foreach (var c in pool)
            {
                if (c == null || c.Faction != faction) continue;
                if (c.Type == "hero") heroes.Add(c);
                else if (c.Type == "unit") units.Add(c);
                // 战术卡：只收**引擎真的打得出去**的 —— 判据和 `CanPlayTactic` / 卡面的 `*`
                // 是**同一份**（`TacticPlayable` → `EffectText.IsFullyParsed`）。
                // 收进来打不出去 = 死牌，那正是当年「只放单位卡」的原因；
                // 现在能打的战术有 354 张，这个理由不成立了。
                else if (c.Type == "tactic" && TacticPlayable(c)) tactics.Add(c);
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
            Shuffle(tactics, rng);

            // ⚠️ **2026-09-12**：这个开关以前是**没实现的**（函数体里写着「已经是只收 unit 了」），
            //    所以自动凑出来的牌组**一张战术卡都没有** —— 实战里永远看不到战术。
            //    现在按它说的做：`unitsOnly == false` 时混进**能打的**战术卡，约占 1/3。
            //    比例是**我们挑的**（原版没有「自动凑牌」这回事，玩家自己编）；
            //    选 1/3 是为了「每局都能摸到几张」，同时不至于一手全是战术卡打不出场面。
            int tacticQuota = unitsOnly ? 0 : size / 3;

            // 先按稀有度上限收一轮，不够再放宽
            var used = new Dictionary<string, int>();
            foreach (var c in units)
            {
                if (deck.Count >= size - tacticQuota) break;
                int n;
                used.TryGetValue(c.Name, out n);
                if (n >= DeckLimit(c.Rarity)) continue;
                used[c.Name] = n + 1;
                deck.Add(c);
            }
            if (tacticQuota > 0)
            {
                foreach (var c in tactics)
                {
                    if (tacticQuota <= 0) break;
                    int n;
                    used.TryGetValue(c.Name, out n);
                    if (n >= DeckLimit(c.Rarity)) continue;
                    used[c.Name] = n + 1;
                    deck.Add(c);
                    tacticQuota--;
                }
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
            if (deck.Count < size && !unitsOnly)
            {
                foreach (var c in tactics)   // 单位卡不够就拿能打的战术卡顶上
                {
                    if (deck.Count >= size) break;
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
        /// ⚠️ **2026-09-12 起收战术卡**（原来一律丢）。判据：`DeckBuilder.TacticPlayable` ——
        ///    效果文本能被 `EffectText` **完整解析**的才收（解析不了的放进来就是打不出的死牌，
        ///    `CanPlayTactic` 会拒绝它，玩家看到的是「拖上去没反应」）。
        ///    防御卡仍然不收：`RuleCore` 里除了 `DeckRules` 的合法性校验，
        ///    没有任何地方认识 `defence`。
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
                if (c.Type == "tactic")
                {
                    // 战术卡：**能完整解析的才收**。解析不了的放进来就是「打不出的死牌」——
                    // `CanPlayTactic` 会拒绝它，玩家看到的是「拖上去没反应」，不如一开始就不给。
                    // 判据共用 `EffectText.IsFullyParsed`（和 `CanPlayTactic` 是同一份）。
                    if (!TacticPlayable(c)) { Note(skipped, id, "战术卡（效果本版解析不了）"); continue; }
                    list.Add(c);
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

        /// <summary>
        /// 这张战术卡**能不能真的打出去** —— 判据就是 `EffectText.IsFullyParsed`
        /// （整条 `desc` 没有不认识的句子、也没有半懂的句子）。
        /// ⚠️ **不要在这儿另写一份判据**：`RuleCore.CanPlayTactic` 用的是同一份，
        ///    两处不一致会出现「牌组收下了、出牌时又被拒」这种最难查的不一致。
        /// </summary>
        public static bool TacticPlayable(CardDef c)
        {
            if (c == null || c.Type != "tactic") return false;
            if (c.Type == "defence") return false;
            // ⚠️ **手牌陷阱卡（`At the end of your turn, …`）不进自动牌组** ——
            //    这是**和「打不打得出」不同的另一个判据**，所以在这里显式加一条，不算「另写一份」：
            //    陷阱卡是**塞给对手**的破坏卡（规则书 :204），自己牌组里放一张只会**每回合坑自己**
            //    （`Poisoned Supplies`：`At the end of your turn, your troops take 1 damage`）。
            //    判据走 `EffectText.IsHandTrap` —— **只此一处**定义「什么算陷阱卡」。
            if (EffectText.IsHandTrap(c.Desc)) return false;
            return EffectText.IsFullyParsed(c.Desc);
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
