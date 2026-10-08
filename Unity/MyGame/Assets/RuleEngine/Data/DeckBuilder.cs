// DeckBuilder.cs — 凑一副能打的牌组
//
// 玩法线的目标之一是「打完一局分出胜负」，那就得先有牌组。
// 这里只做**最小可用**的构造：一个督军 + 一堆单位卡，够打就行。
// 真正的构筑（费用曲线、卡组模板、稀有度限制的完整规则）不在 v1 范围内 ——
// `数据/游戏数据/prebuilt_decks_full.json` 里有原版 **236** 副模板，将来可以直接用。
//   ⚠️ 2026-09-24 更正：原文写「472 副」——**实测 236**（`bundle_prebuiltdecks_assets_all/MonoBehaviour/` 正好 236 个文件）。
//   另注：运行时集合是 **224**（`PrebuiltDeckCollection.AddDeck` 把名字含 "Tutorial" 的 12 副丢掉），
//   其中 `isPractice=1` 的练习池 **103** 副（= 原版 Deck Selection「Prebuilt」页签那一池）。
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
            var defences = new List<CardDef>();      // 🆕 2026-09-29：只喂「自动凑的牌补一张防御卡」那一处
            foreach (var c in pool)
            {
                if (c == null || c.Faction != faction) continue;
                if (c.Type == "hero") heroes.Add(c);
                else if (c.Type == "unit") units.Add(c);
                // 🆕 2026-09-29：防御卡**是独立的一格**（不进那 `size` 张），单独收着，最后再挂上去。
                else if (c.Type == "defence") defences.Add(c);
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

            // 🆕 2026-09-29：**自动凑的牌也要带一张防御卡**（用户 2026-09-28 拍板「自动补上」）。
            //   为什么：防御卡是**后手补偿**（用户原话「如果不放防御卡也可以游戏，但是后手就没有这个
            //   防御卡的后手补偿了」）⇒ 自动凑的牌不带它 = 自动凑出来的那局**没有后手补偿**。
            //   只走**第二条**来源（本阵营 `defence` 池随机一张）—— 与 `FromDeck` 那条兜底**同一个函数**
            //   （`PickRandomDefence`，按 `Id` 排序保证可复现），**别在这儿另写一份挑法**。
            //   ⚠️ **独立的一格、不算进那 `size` 张**（`DeckRules.Validate` ⑥ 禁止它混进普通卡位）；
            //     挂到**最后** —— `RuleCore.BuildPlayer` 认「第一个 `hero` 当督军」，位置不影响它。
            //   ⚠️ 原版第三来源（读督军自己的 `goSecondCardInHand`）我们**走不了**：那份数据本地零命中
            //      （`预组卡组_原版规格.md` §五之六 + `项目任务.md` §〇 B）。
            //   🔴 🆕 **2026-10-19（`A1070`）：这一张**只有玩家侧会读**。** 电脑（AI）那一方原版
            //      **从不从卡组取**（预组牌的 `DeckAndWarlordData.defensiveCard` 恒 null，两条反汇编硬证：
            //      `DeckBasicSetup` 第 5 实参 `R9=0`）⇒ 引擎侧 `RuleCore.GoesSecondCard` 会把电脑侧
            //      分流出来的这张**丢掉**，改走「后手方阵营防御池随机」或「督军自带 `goSecondCardInHand`」。
            //      ⇒ **别把这张当成「电脑也有防御卡」**（那正是 `A1070` 修掉的那份「看起来能用」）。
            var dfc = PickRandomDefence(defences, faction, rng);
            if (dfc != null)
            {
                deck.Add(dfc);
                UnityEngine.Debug.Log($"[RuleEngine] 凑牌组补一张防御卡：「{dfc.Name}」({dfc.Id})"
                                    + "（后手补偿；原版 `AddGoesSecondCardToDeck` 的兜底那条）");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[RuleEngine] 阵营 `{faction}` 没有防御卡 ⇒ 自动凑的这局**没有后手补偿**"
                                           + "（不静默：正常每个阵营都该有一张）");
            }
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
        /// <param name="faction">
        /// **卡组的阵营**（= 督军的阵营）。给了它，**同名卡就按阵营解析**。
        ///
        /// 🔴 **为什么必须有这个参数**（2026-09-13 第三十二轮）：这个索引是**按 `Id` 建的，
        ///    而我们的 `Id` 就是卡名**（`CardDatabase.Parse` 拿名字当 id）——
        ///    于是 `!index.ContainsKey` 那句在遇到**跨阵营同名卡**时，**后一张把前一张挤掉**
        ///    （卡池顺序决定谁赢）。实测：卡组里存的是 Ultramarines 的 `Bladeguard Veteran`，
        ///    索引里留下的却是 DarkAngels 那张同名卡 ⇒ **展开出一副别的阵营的牌**。
        ///    ⚠️ 这和 `DeckRules.Validate` 那个 `WrongFaction` 是**同一个根病**
        ///      （「卡名当 id」，见 `项目任务.md` 悬案），必须**一起**修 ——
        ///      只修一边的话，校验过了、展开仍然是错的。
        ///
        /// ⚠️ **2026-10-17 订正两处**（都是上面这段的历史描述，行为一个字没动）：
        ///   ① 「我们的 `Id` 就是卡名」**只在卡表 v6 之前成立** —— 2026-09-13 第三十三轮起
        ///      `Id` 是卡表里的**稳定 id**（`CardDatabase.Parse` 只在那一行缺 `id` 时才退回卡名，
        ///      并且会 `LogWarning` 报 `noId` 张数。见 `CardDatabase.cs` 的 `noId` 那一段）
        ///      ⇒ 下面那段「同名撞车」分支现在只在**卡表缺 id** 时才可能命中（防御性保留）。
        ///   ② 例子里的 `Bladeguard Veteran`（Ultramarines 那张）是 `UM34` 2026-09-13 被**改错**的
        ///      名字（照 PnP 卡图文件名改的），2026-10-17 已撤回 ⇒ 现在叫 `Bladeguard Lieutenant`。
        ///      判据 `资料/普查产出_1017/W_B16_教程数据缺口.md` §①。
        /// </param>
        /// <param name="rng">**只用于「卡组没带防御卡时随机补一张」那一处**（原版 `AddGoesSecondCardToDeck` 的兜底，
        /// 见下面防御卡那一段）。不传 = `new System.Random(0)`（同一副牌每次补到同一张 ⇒ 对局可复现）。
        /// ⚠️ 它**不参与**其它任何取舍 —— 免得「同一副牌两局不一样」这种不可复现的事从这儿漏进来。</param>
        public static List<CardDef> FromDeck(IEnumerable<CardDef> pool, PlayerDeck deck,
                                             List<string> skipped = null, string faction = null,
                                             System.Random rng = null)
        {
            var index = new Dictionary<string, CardDef>();
            var defencePool = new List<CardDef>();      // 🆕 只喂「没带防御卡时随机补一张」那一处
            foreach (var c in pool)
            {
                if (c == null) continue;
                if (c.Type == "defence") defencePool.Add(c);
                CardDef prev;
                if (!index.TryGetValue(c.Id, out prev)) { index[c.Id] = c; continue; }
                // ---- 同名撞车（卡名当 id 的后果）----
                // 没给 faction：**保持旧行为**（先来的赢 = 卡池顺序），一个字都不改。
                if (string.IsNullOrEmpty(faction)) continue;
                // 给了 faction：**本阵营那张赢**。已经赢的就是本阵营的 → 不再换。
                if (DeckRules.SameFaction(prev.Faction, faction)) continue;
                if (DeckRules.SameFaction(c.Faction, faction)) index[c.Id] = c;
            }

            var list = new List<CardDef>();
            if (deck == null) return list;

            var warlord = Lookup(index, deck.WarlordId);
            if (warlord == null || warlord.Type != "hero")
                UnityEngine.Debug.LogError($"[RuleEngine] 卡组的督军 `{deck.WarlordId}` 在卡池里找不到"
                                         + "（或不是 hero）—— 引擎会退回默认督军，这局打得不是你要的那套");
            list.Add(warlord);      // 可能是 null，`BuildPlayer` 会退回默认督军

            // 防御卡：✅ **2026-09-13 第三十三轮起真的进对局**（原来只有一行 `Note(skipped, …, "防御卡")`）。
            // 规则书 `:45`：一副合法卡组 = 1 督军 + **1 防御卡** + 30 张阵营卡 ⇒ 它是**独立的一格**，
            // 不属于那 30 张（`DeckRules.Validate` ⑥ 也禁止它混进普通卡位）。
            // ⇒ 这里按**和督军同等的独立项**处理；它进的是**手牌**不是牌库 ——
            //    分流判据在 `RuleCore.BuildPlayer`（按 `Type == "defence"`，**只此一处**）。
            if (!string.IsNullOrEmpty(deck.DefensiveId))
            {
                var dfc = Lookup(index, deck.DefensiveId);
                if (dfc == null)
                    UnityEngine.Debug.LogError($"[RuleEngine] 卡组的防御卡 `{deck.DefensiveId}` "
                                             + "在卡池里找不到 —— 这张牌被丢了");
                else if (!TacticPlayable(dfc))
                    Note(skipped, deck.DefensiveId, "防御卡（效果本版解析不了）");
                else list.Add(dfc);
            }
            else
            {
                // 🆕 2026-09-26：**卡组没带防御卡 ⇒ 从本阵营的防御卡池随机补一张**。
                //   判据（唯一）→ `资料/加时与冲突模式_原版规格.md` §2.7c：
                //   原版 `DeckUtility.ValidateDeck` **不校验防御卡**（0 张合法，见 `DeckRules.Validate` ③），
                //   兜底在开局：`BattleManager.AddGoesSecondCardToDeck.c:145-184` 从防御卡池
                //   `GetEnvEffectCards` **随机抽一张** → `AddNewCardToHand` ⇒ **进手牌**；
                //   而 `:154-166` 写着「**卡组里有防御卡就用卡组那张**，随机只在没有时生效」——
                //   所以我们这条**只在 `DefensiveId` 为空时**走，与它逐字同构。
                //   ✅ **⚠️ 那条「我们两边都给」的偏离已作废（2026-09-28 订正本条注释）**：
                //      现在**只给后手**（判据 = `RuleCore.NewBattle` 里 `getsDefenceCard: ctx.FirstSeat == 1/0` 那两行，
                //      **只此一处**）。原来「两边都给」的理由是「玩家恒先手 ⇒ 只给后手的话玩家永远看不到」——
                //      那个前提 2026-09-26 起也不成立了：**先手现在是掷硬币决定的**，玩家会轮到后手。
                //   ⚠️ 原版那个防御卡池**是不是按阵营筛的，没查实**（`EnviromentalEffectCardsSO.defensiveCards`
                //      的逐项结构没读）；我们**按本阵营筛** —— 理由是我们引擎里跨阵营的牌上不了场（
                //      `DeckRules.Validate` ⑤ 与手牌归属都要求同阵营），拿一张外阵营的等于白给。**这条是我们的选择。**
                //   🔴 🆕 **2026-10-19（`A1070`）：这张**只有玩家侧会读**。** 电脑那一方原版**从不从卡组取**
                //      （预组牌的 `DeckAndWarlordData.defensiveCard` 恒 null：`CardDeck.DeckBasicSetup`
                //      第 5 实参 `R9=0` 写进 `CardDeck+0x48`）⇒ `RuleCore.GoesSecondCard` 会把电脑侧这张
                //      **丢掉**，改走「后手方阵营防御池随机」或「督军 `goSecondCardInHand`」。
                //      ⛔ 别把这里补的这张当成「电脑也有防御卡」。
                var pick = PickRandomDefence(defencePool, faction, rng);
                if (pick != null)
                {
                    list.Add(pick);
                    UnityEngine.Debug.Log($"[RuleEngine] 卡组「{deck.Name}」**没带防御卡** ⇒ 照原版补一张"
                                        + $"本阵营（{faction}）的：「{pick.Name}」({pick.Id})"
                                        + "（原版 `BattleManager.AddGoesSecondCardToDeck` 的兜底；"
                                        + "卡组带了就用卡组那张，这条只在没带时走）");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[RuleEngine] 卡组「{deck.Name}」没带防御卡，"
                                               + $"而本阵营（{faction}）的防御卡池**是空的** ⇒ 这一局没有防御卡"
                                               + "（不静默：正常应该补得上一张）");
                }
            }

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

        /// <summary>🆕 2026-09-26：**卡组没带防御卡时，从本阵营的防御卡池里随机挑一张** ——
        /// 原版 `BattleManager.AddGoesSecondCardToDeck` 那一步的兜底（判据 → `资料/加时与冲突模式_原版规格.md` §2.7c）。
        ///
        /// 🔴 **`OrderBy(Id)` 是必须的，不是风格**：挑中哪一张取决于**输入顺序**，
        ///    而卡池顺序哪天变了（`CardDatabase` 解析顺序 / 卡表重排），同一副牌就会补到**另一张** ——
        ///    那会让「同一份存档两局不一样」。按 Id 排序把这件事**钉死**（对局可复现是项目红线）。
        /// ⚠️ 原版那个池子**是不是按阵营筛的没查实**；我们按本阵营筛（理由写在调用点那段注释里）。
        /// 🔴 🆕 **2026-10-19（`A1070`）：这里补出来的那张**只有玩家侧会读**。**
        ///    电脑（AI）那一方原版**从不从卡组取**（预组牌 `DeckAndWarlordData.defensiveCard` 恒 null，
        ///    硬证 = `CardDeck.DeckBasicSetup` 第 5 实参 `R9=0`）⇒ 引擎侧 `RuleCore.GoesSecondCard`
        ///    会把电脑侧那张**丢掉**、按 `matchType` 改走 ②（后手方阵营防御池随机）/ ③（督军自带）。
        ///    ⛔ 别拿它当「电脑也有防御卡」的依据 —— 那正是 `A1070` 修掉的那份「看起来能用」。
        ///    （删掉它也不行：**玩家**也可能用一副预组/凑出来的牌，那时这张就是他那一侧的那张。）
        /// </summary>
        static CardDef PickRandomDefence(List<CardDef> defencePool, string faction, System.Random rng)
        {
            if (defencePool == null || defencePool.Count == 0) return null;
            var cand = new List<CardDef>();
            foreach (var c in defencePool)
                if (string.IsNullOrEmpty(faction) || DeckRules.SameFaction(c.Faction, faction)) cand.Add(c);
            if (cand.Count == 0) return null;
            cand.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return cand[(rng ?? new System.Random(0)).Next(cand.Count)];
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
            // ✅ 2026-09-13 第三十三轮：**收防御卡了**（原来这里 `c.Type != "tactic"` 直接返 false，
            //    外加一条 `if (c.Type == "defence") return false;` 的死代码）。撤掉的理由同
            //    `RuleCore.CanPlayTactic`：39 张防御卡 **39/39 解析得出**，动词都是已实现的那批，
            //    规则书 `:105` 也把防御卡归在战术大类里。
            if (c == null || (c.Type != "tactic" && c.Type != "defence")) return false;
            // ⚠️ **手牌陷阱卡（`At the end of your turn, …`）不进自动牌组** ——
            //    这是**和「打不打得出」不同的另一个判据**，所以在这里显式加一条，不算「另写一份」：
            //    陷阱卡是**塞给对手**的破坏卡（规则书 :204），自己牌组里放一张只会**每回合坑自己**
            //    （`Poisoned Supplies`：`At the end of your turn, your troops take 1 damage`）。
            //    判据走 `EffectText.IsHandTrap` —— **只此一处**定义「什么算陷阱卡」。
            //    🆕 2026-09-14 A4 批 4：**改吃 `CardDef` 而不是 `Desc`** —— 手牌陷阱现在有**两族**，
            //      其中事件型（`When you play a Stratagem, …`，`Jammed Communications`）必须
            //      **看卡类**才知道算不算（单位卡的 `When …` 是事件层、不是陷阱）——
            //      判据在 `EffectText.IsHandTrap(CardDef)` 一处。
            if (EffectText.IsHandTrap(c)) return false;
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
