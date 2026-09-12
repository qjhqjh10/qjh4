// CreatePool.cs — `create` 造牌的**候选池**怎么算（`EffectResolver.DoCreate` 的选卡来源）
//
// **权威语义来源**：
//   · 规则书 **附录 B「生成/复制卡的阵营指南」**（`资料/规则书/…_中文翻译.md` `:254-287`）
//     —— 每张生成器卡「生成哪个阵营的什么兵种、几种、各几张、上限多少」
//   · 规则书 **附录 C「骰子查找表」**（同文件 `:289-320`）—— 实体版的候选**名单**，
//     我们录进来只做**对账**（见 <see cref="DiceTables"/>），不拿它当运行时数据源：
//     按名字造卡得先有 `CardDef`，而名单里的名字和数字版数据**常常对不上**（差标点/多一个词/换个写法）。
//     对账结果由 `RuleEngineTest.CheckDiceTables` 每次跑出来（差集两个方向都报）。
//
// 为什么池子**动态算**而不是硬编码名单：附录 B 给的是「几种」这个数，
// 实测卡池算出来的数和它逐条对得上 —— Ultramarines 载具 18 种、SaimHann 载具 20 种、
// Goff 载具 14 种、次元裂隙 2 费 6 张、召唤教派 2 费 5 张。
// 对不上的少数几处（`Tyranid Prime` / `Shivversplint` / `Daemonette` 的 subtype）逐条钉在自检里。
// 数字版卡池会长（补丁/新卡），名单是死的。
//
// ⚠️ 本文件属于 `Core/` —— **不允许依赖 UnityEngine**。卡池由调用方从 `BattleContext.CardPool`
//    传进来；**没有卡池就如实报「这局没有卡池」，绝不退化成「从双方牌库里抽」** ——
//    那会悄悄造出一张卡面上没有的牌。
using System;
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>一次 `create` 的候选池。</summary>
    public class CreatePoolResult
    {
        public readonly List<CardDef> Cards = new List<CardDef>();

        /// <summary>null = 池子算出来了；否则是**人话原因**（调用方照原样播给玩家）</summary>
        public string Why;

        /// <summary>诊断串（「按 X 筛出 N 张」），写进日志好排查</summary>
        public string Detail;

        /// <summary>`Create a copy of **it**` —— 指代上一条效果的目标，不是池子查询。
        /// 结算层据此去取 `ctx.LastTarget` 那张卡。</summary>
        public bool CopyOfPrev;

        public bool Ok { get { return Why == null && Cards.Count > 0; } }
    }

    public static class CreatePool
    {
        // ==================================================================
        //  入口
        // ==================================================================

        /// <summary>
        /// 「造什么」→ 候选卡列表。**算不出来返回 <see cref="CreatePoolResult.Why"/>**，
        /// 调用方必须如实播报（红线：不许静默失败）。
        /// </summary>
        /// <param name="pool">全卡池（`BattleContext.CardPool`）。**null = 这一局没给卡池**</param>
        /// <param name="payload">`EffectOp.Payload` —— 「造什么」原文（`ultramarines vehicles` / `copy of no respite`）</param>
        /// <param name="casterFaction">施放者阵营（`Warlord.Card.Faction`）。卡面**没写**阵营时用它</param>
        /// <param name="unitsOnly">只要**单位卡**（`deploy` 用 —— 部署一张战术卡是没有意义的事）</param>
        /// <param name="costMin">费用下界（0 = 不限）</param>
        /// <param name="costMax">费用上界（0 = 不限）</param>
        public static CreatePoolResult Resolve(IReadOnlyList<CardDef> pool, string payload,
                                               string casterFaction,
                                               bool unitsOnly = false, int costMin = 0, int costMax = 0)
        {
            var r = new CreatePoolResult();
            if (string.IsNullOrEmpty(payload)) { r.Why = "卡面没写造什么"; return r; }

            string what = payload.Trim().ToLowerInvariant();

            // `random` 只是选法说明，不是名字的一部分（池子在结算层用 `ctx.Rng` 抽）
            while (what.StartsWith("random ")) what = what.Substring(7).Trim();
            if (what.Length == 0) { r.Why = "卡面没写造什么"; return r; }

            bool isCopy = false;
            if (what.StartsWith("copies of ")) { isCopy = true; what = what.Substring(10).Trim(); }
            else if (what.StartsWith("copy of ")) { isCopy = true; what = what.Substring(8).Trim(); }

            if (what.Length == 0)
            {
                // `Create two copies in your hand` —— 「复制**哪张**」写在上一句里（`Choose a …`），
                // 本版没有 choose 那一段，所以这里**没有可复制的对象**，如实报。
                r.Why = "卡面没写复制哪一张（本版还没有 `Choose a …` 那一段）";
                return r;
            }

            // `Create a copy of **it**` —— 指代上一条效果的目标（原版 `it_target`），不是卡名
            if (isCopy && IsPronoun(what)) { r.CopyOfPrev = true; r.Detail = "复制上一条效果的目标那张卡"; return r; }

            if (pool == null)
            {
                r.Why = "这一局没有卡池（`RuleCore.NewBattle` 没传 `cardPool`）";
                return r;
            }

            // ---- ① 候选名单：`a Gun Drone, Guardian Drone or Marker Drone` ----
            if (what.IndexOf(" or ", StringComparison.Ordinal) >= 0)
            {
                foreach (string piece in SplitList(what))
                {
                    var c = FindByName(pool, piece);
                    if (c != null) r.Cards.Add(c);
                    else r.Detail = AppendDetail(r.Detail, "名单里的「" + piece + "」原版数据里没有这张卡");
                }
                if (r.Cards.Count == 0) r.Why = "名单里的卡原版数据里一张都没有";
                else SortByName(r.Cards);
                return r;
            }

            // ---- ② 兵种池：`[阵营] <兵种词> [with <关键词>]` ----
            // ⚠️ **要先于按名字查**：`Combat Elixir` / `Sabotage` 在原版数据里是**兵种**
            //    （`subtype`），不是卡名 —— 见本文件 `KindWords` 表。
            string withWord = null;
            int wi = what.IndexOf(" with ", StringComparison.Ordinal);
            if (wi > 0) { withWord = what.Substring(wi + 6).Trim(); what = what.Substring(0, wi).Trim(); }

            string factionWord, kindPhrase;
            var kind = MatchKindWord(pool, what, out factionWord, out kindPhrase);
            if (kind != null)
            {
                string faction = ResolveFaction(pool, factionWord, casterFaction, r);
                foreach (var c in pool)
                {
                    if (c == null) continue;
                    if (unitsOnly && !c.IsUnit) continue;
                    if (kind[1] == "type" && c.Type != kind[2]) continue;
                    if (kind[1] == "subtype" && !SubtypeIn(c, kind, 2)) continue;
                    if (!string.IsNullOrEmpty(faction) && !SameFaction(c.Faction, faction)) continue;
                    if (withWord != null && !HasKeyword(c, withWord)) continue;
                    if (costMin > 0 && c.Cost < costMin) continue;
                    if (costMax > 0 && c.Cost > costMax) continue;
                    r.Cards.Add(c);
                }
                string scope = (factionWord.Length > 0 ? "阵营「" + factionWord + "」+ " : "")
                             + "兵种「" + kindPhrase + "」"
                             + (withWord != null ? " + 关键词「" + withWord + "」" : "")
                             + CostScope(costMin, costMax);
                r.Detail = AppendDetail(r.Detail, scope + " → " + r.Cards.Count + " 张");
                if (r.Cards.Count == 0) r.Why = "按" + scope + "在卡池里一张都没筛到";
                else SortByName(r.Cards);
                return r;
            }

            // ---- ③ 具名卡：`a Termagant` / `copy of No Respite` ----
            // `kindPhrase` 是原样返回的（没命中兵种词）—— 用**整段**当卡名查，别只取最后一个词。
            var named = FindByName(pool, kindPhrase);
            if (named != null)
            {
                if (unitsOnly && !named.IsUnit)
                {
                    r.Why = "「" + named.Name + "」是" + named.Type + "卡，**不是单位**，部署不了";
                    return r;
                }
                r.Cards.Add(named);
                r.Detail = "具名卡「" + named.Name + "」";
                return r;
            }
            if (unitsOnly)
            {
                // 「部署」要的是**场上的单位**。兵种词认不出时，检查一下是不是名字对不上
                var nearU = FindNearMiss(pool, kindPhrase);
                if (nearU != null && FindByName(pool, nearU) != null && !FindByName(pool, nearU).IsUnit)
                {
                    r.Why = "「" + nearU + "」不是单位卡，部署不了";
                    return r;
                }
            }

            r.Why = "「" + kindPhrase + "」既不是原版卡名、也不是我们认识的兵种词"
                  + "（原版数据里没有这张卡，不造效果）";
            // 顺便报**最接近的那个名字** —— 原版数据是 OCR + 手抄来的，名字常年对不上：
            //   `Sergeant Taaman`（实体卡）↔ 数据里写 `Sergeant Naaman`
            //   `Extermination Protocol`（实体卡）↔ 数据里写 `Extermination Protocols`
            // ⚠️ **只报，不替**。名字对不上就当成另一张卡去造，是「猜」——红线。
            string near = FindNearMiss(pool, kindPhrase);
            if (near != null) r.Why += $"；最接近的是「{near}」，但名字对不上，**没有**拿它顶替";
            return r;
        }

        /// <summary>
        /// 名字写岔了的**最近似卡**（只用来在报错里说一句人话）。判据两条，先严后宽：
        ///   ① 归一化后一方包含另一方（`exterminationprotocol` ⊂ `exterminationprotocols`）
        ///   ② 编辑距离 ≤ 2（`sergeanttaaman` ↔ `sergeantnaaman` 差 1）
        /// 找不到返回 null。**绝不用它替代查不到的那张卡**。
        /// </summary>
        public static string FindNearMiss(IReadOnlyList<CardDef> pool, string name)
        {
            if (pool == null || string.IsNullOrEmpty(name)) return null;
            string want = Norm(name);
            if (want.Length < 4) return null;

            string best = null; int bestDist = 3;
            foreach (var c in pool)
            {
                if (c == null) continue;
                string have = Norm(c.Name);
                if (have.Length == 0) continue;
                if (have.Contains(want) || want.Contains(have)) return c.Name;
                if (WordsSubset(name, c.Name)) return c.Name;
                int d = EditDistance(want, have);
                if (d < bestDist) { bestDist = d; best = c.Name; }
            }
            return best;                 // bestDist 初值 3 → 只有 ≤2 的才会被选中
        }

        /// <summary>
        /// **词的有序子集**：`a` 的每个词按顺序都能在 `b` 里找到（中间可以插别的词）。
        ///
        /// 为什么需要这一档：实体卡表和数字版数据对同一张卡常常是**缩写 vs 全称**，
        /// 差的是中间插进去的一个词，前后缀和编辑距离都抓不到 —— 实测三例：
        ///   `Gauss Warrior`         ↔ `**Gauss Reaper** Warrior`
        ///   `Tesla Immortal`        ↔ `**Tesla Carbine** Immortal`
        ///   `Threnodic Noise Marine` ↔ `**Threnodic Choir** Noise Marine`
        /// ⚠️ **只用来在报错里说一句人话**，绝不拿它顶替查不到的那张卡（见 `FindNearMiss`）。
        /// </summary>
        static bool WordsSubset(string a, string b)
        {
            var wa = a.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var wb = b.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (wa.Length == 0 || wa.Length > wb.Length) return false;
            int j = 0;
            foreach (var w in wa)
            {
                while (j < wb.Length && wb[j] != w) j++;
                if (j >= wb.Length) return false;
                j++;
            }
            return true;
        }

        /// <summary>编辑距离（短字符串，滚动一行就够）</summary>
        static int EditDistance(string a, string b)
        {
            var prev = new int[b.Length + 1];
            var cur = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) prev[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                cur[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }
                var t = prev; prev = cur; cur = t;
            }
            return prev[b.Length];
        }

        /// <summary>
        /// 一张具名卡按**归一化名字**查（原版数据里 `Abaddon's Chosen` 写成 `Abaddons Chosen`、
        /// 中文卡面 `Saim-Hann` 写成 `SaimHann` —— 撇号/连字符全被剥了）。
        /// **查不到返回 null**（调用方要知道是哪一张缺了，好如实报）。
        /// </summary>
        public static CardDef FindByName(IReadOnlyList<CardDef> pool, string name)
        {
            if (pool == null || string.IsNullOrEmpty(name)) return null;
            string want = Norm(name);
            if (want.Length == 0) return null;
            foreach (var c in pool)
            {
                if (c == null) continue;
                if (Norm(c.Name) == want) return c;
            }
            return null;
        }

        /// <summary>名字归一化：小写 + 只留字母数字。
        /// 原版数据里 `Abaddon's Chosen` → `Abaddons Chosen`、`Von Ryan's Leaper` → `Von Ryans Leaper`，
        /// 而卡面写的是带撇号的原名 —— 不归一化就永远查不到。</summary>
        public static string Norm(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char ch in s)
            {
                char c = char.ToLowerInvariant(ch);
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) sb.Append(c);
            }
            return sb.ToString();
        }

        static void SortByName(List<CardDef> list)
        {
            // 池子顺序**必须定死**：结算层用 `ctx.Rng` 按下标抽，顺序一变同一局就不一样了
            list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        static string AppendDetail(string a, string b)
        {
            return string.IsNullOrEmpty(a) ? b : a + "；" + b;
        }

        static bool IsPronoun(string t)
        {
            t = t.Trim();
            return t == "it" || t == "them" || t == "the target" || t == "this unit" || t == "this troop";
        }

        /// <summary>`a Gun Drone, Guardian Drone or Marker Drone` → 三段</summary>
        static IEnumerable<string> SplitList(string s)
        {
            foreach (string p in s.Split(new[] { " or " }, StringSplitOptions.None))
                foreach (string q in p.Split(new[] { ", " }, StringSplitOptions.None))
                {
                    string t = q.Trim();
                    if (t.Length > 0) yield return t;
                }
        }

        static bool SubtypeIn(CardDef c, string[] kind, int from)
        {
            if (string.IsNullOrEmpty(c.Subtype)) return false;      // 原版没给兵种的**不猜**，排除
            for (int i = from; i < kind.Length; i++)
                if (string.Equals(c.Subtype, kind[i], StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        static bool HasKeyword(CardDef c, string kw)
        {
            string norm = KeywordTable.Normalize(kw.ToLowerInvariant());
            if (norm == null) return false;
            return c.Has(norm);
        }

        // ==================================================================
        //  兵种词表
        // ==================================================================

        // 行格式：{ 卡面词, 匹配方式, 值1, 值2, … }
        //   `type`    —— 比 `CardDef.Type`（`unit` / `hero` / `tactic` / `defence`）
        //   `subtype` —— 比 `CardDef.Subtype`（原版 `card_stats.json` 的 `subtype`），可给多个候选值
        // ⚠️ 顺序：**多词词条排在单词前面**（`combat elixir` 要在 `elixir` 前被匹配到）
        static readonly string[][] KindWords =
        {
            // `troop` / `unit` 是**通称**，指卡池里的单位卡（督军是 `type=hero`，不在内）
            new[] { "troop",  "type", "unit" },
            new[] { "unit",   "type", "unit" },
            // 原版 `subtype` 取值的实测分布见 `card_stats.json`（1707/… 见 `CardDef.Subtype` 注释）
            new[] { "vehicle",    "subtype", "Vehicle" },
            new[] { "infantry",   "subtype", "Infantry" },
            new[] { "battlesuit", "subtype", "Battlesuit" },
            new[] { "beast",      "subtype", "Beast" },
            new[] { "drone",      "subtype", "Drone" },
            new[] { "monster",    "subtype", "Monster" },
            new[] { "daemon",     "subtype", "Daemon" },
            new[] { "structure",  "subtype", "Structure" },
            new[] { "spell",      "subtype", "Spell" },
            new[] { "tactic",     "type",    "tactic" },
            // 具体到卡牌类型的几个（原版把它们放在 `subtype` 里，不是 `type`）：
            //   战斗药剂 —— 附录 C「战斗药剂（1d6）」那 6 张；实测数据里 3 张写 `Elixir`、
            //              2 张写 `Combat Elixir`、1 张（Shivversplint）错写成 `Upgrade`
            new[] { "combat elixir", "subtype", "Combat Elixir", "Elixir" },
            new[] { "elixir",        "subtype", "Combat Elixir", "Elixir" },
            //   破坏 —— 基因窃取者的特殊战术（`Improvised Barricade` / `Poisoned Supplies`）
            new[] { "sabotage",   "subtype", "Sabotage" },
            //   隐秘 —— 暗黑天使（附录 C「暗黑天使隐秘（1d5）」）
            new[] { "secret",     "subtype", "Secret" },
        };

        /// <summary>
        /// 「<em>修饰词…</em> + 兵种词」→ 词表行（外加阵营词）。**认不出兵种词返回 null**
        /// （调用方会退回按卡名查）。
        ///
        /// 为什么要**逐个 token 扫**而不是只看最后一个词：卡面写的是
        /// `friendly **Infantry** troops` / `**Ork** **Infantry**` / `**Emperor's Children** **Daemons**`
        /// —— 修饰词既可能是**阵营**也可能是**兵种**，而且 `Infantry troops` 里
        /// **两个词都是兵种词**（`troops` 是通称、`Infantry` 才是筛选条件）。
        ///
        /// 老写法只试「最后 1–2 个词」当兵种词、其余一律当阵营词，于是
        /// `Infantry troops` 会把 `Infantry` 当成阵营名（对不上 → 落回施放者阵营），
        /// **兵种筛选静默丢掉** —— 2026-09-12 做 `deploy` 时撞到。
        ///
        /// 逐个 token：先试**两词窗口**（`combat elixir` / `genestealer cults` 这类要黏在一起），
        /// 再试单词；**兵种词优先于阵营词**（`Infantry troops` 里 `Infantry` 是兵种不是阵营）。
        /// subtype 比 type 更具体，所以同时命中时 **subtype 赢**。
        /// </summary>
        static string[] MatchKindWord(IReadOnlyList<CardDef> pool, string phrase,
                                      out string factionWord, out string kindPhrase)
        {
            factionWord = ""; kindPhrase = phrase;
            if (string.IsNullOrEmpty(phrase)) return null;

            string[] toks = phrase.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] kindRow = null;
            var factions = new List<string>();
            var unknown = new List<string>();

            for (int i = 0; i < toks.Length; )
            {
                string two = (i + 1 < toks.Length) ? toks[i] + " " + toks[i + 1] : null;

                // ① 两词窗口当兵种词（`combat elixir`）
                int n = 1;
                string[] row = null;
                if (two != null) row = LookupKind(Singular(two));
                if (row != null) n = 2;
                else
                {
                    // ② 单词当兵种词
                    row = LookupKind(Singular(toks[i]));
                }
                if (row != null)
                {
                    // subtype 比 type 具体 —— 同时命中时 subtype 赢（`Infantry troops`）
                    if (kindRow == null || (row[1] == "subtype" && kindRow[1] != "subtype")) kindRow = row;
                    i += n;
                    continue;
                }

                // ③ 阵营词（两词窗口 `genestealer cults` / `emperor's children`，再单词）
                string fac;
                if (two != null && TryResolveFaction(pool, two, out fac)) { factions.Add(two); i += 2; continue; }
                if (TryResolveFaction(pool, toks[i], out fac)) { factions.Add(toks[i]); i += 1; continue; }

                unknown.Add(toks[i]);
                i++;
            }

            if (kindRow == null) return null;         // 没命中兵种词 → 交给按卡名查
            factionWord = string.Join(" ", factions.ToArray());
            kindPhrase = string.Join(" ", unknown.ToArray());
            if (kindPhrase.Length == 0) kindPhrase = phrase;
            return kindRow;
        }

        /// <summary>这个词是不是我们认识的**兵种词**（`vehicles` / `beasts` / `troops` …）。
        /// `lowercost` 判「`Lower the cost of all Vehicles` 里的 `Vehicles` 是兵种还是卡名」用它。</summary>
        public static bool IsKindWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            string w = word.Trim().ToLowerInvariant();
            if (LookupKind(Singular(w)) != null) return true;
            int sp = w.IndexOf(' ');
            return sp > 0 && LookupKind(Singular(w.Substring(sp + 1))) != null;
        }

        static string[] LookupKind(string canon)
        {
            foreach (var row in KindWords) if (row[0] == canon) return row;
            return null;
        }

        /// <summary>
        /// 这张卡算不算<b>某一类</b>（`troop` / `vehicle` / `spell` / `sabotage` …）——
        /// **判据只此一份**，候选池筛（`Resolve`）和定向翻找（`RuleCore.DoDrawType`）共用它。
        ///
        /// 切词和前缀同 `MatchKindWord`：先试两词（`combat elixir`），再试一词，剥一个尾 `s`。
        /// </summary>
        public static bool MatchesKind(CardDef c, string kindWord)
        {
            if (c == null || string.IsNullOrEmpty(kindWord)) return false;
            string w = kindWord.Trim().ToLowerInvariant();
            var row = LookupKind(Singular(w));
            if (row == null)
            {
                int sp = w.IndexOf(' ');
                if (sp > 0) row = LookupKind(Singular(w.Substring(sp + 1)));
            }
            if (row == null) return false;
            if (row[1] == "type") return c.Type == row[2];
            return SubtypeIn(c, row, 2);
        }

        /// <summary>只剥**一个**结尾的 `s`（`vehicles` → `vehicle`）。`battlesuit` 这种自带 s 的词
        /// 在词表里对不上时，`Singular` 不剥，所以不会被误伤。</summary>
        static string Singular(string w)
        {
            w = w.Trim().ToLowerInvariant();
            if (w.Length > 2 && w.EndsWith("s") && !w.EndsWith("ss")) w = w.Substring(0, w.Length - 1);
            return w;
        }

        // ==================================================================
        //  阵营词
        // ==================================================================

        /// <summary>
        /// 卡面写的**种族名** → 卡池里的**阵营名**。只有实测有据的才收进来。
        ///
        /// `ork → Goff`（兽人 → 高夫兽人）的证据链有三条，缺一条都不敢收：
        ///   ① 规则书附录 B 把 `比你们快（Fasta Than Yooz）`列在**「高夫兽人」**那一行；
        ///   ② 该卡的 `faction` 就是 `Goff`，卡面写 `Create three random Ork Vehicles`；
        ///   ③ 附录 B 说它生成「**14 种**」载具，而 `Goff` 的载具实测**正好 14 张**。
        /// 剩余 12 个阵营名在卡池里都能**直接**对上（`Saim-Hann` → `SaimHann` 只差连字符）。
        /// </summary>
        static readonly string[][] FactionAliases =
        {
            new[] { "ork", "Goff" },
            new[] { "orks", "Goff" },
            // `Genestealer Cults`（基因窃取者教派）→ 卡池里的 `Genestealers`。
            // 证据：附录 B 把「召唤教派（Summon the Cult）」列在**基因窃取者**那一行，写「2 个 2 费部队
            // （**5 种**）」；`Deploy 2 random 2-cost Genestealer Cults troops` 按卡池算**正好 5 张**，
            // 且与附录 C「召唤教派/变形偶像持旗者（1d5）」的 5 个名字逐个对上。
            new[] { "genestealer cults", "Genestealers" },
            new[] { "genestealer cult", "Genestealers" },
        };

        /// <summary>
        /// 阵营词 → 卡池里的阵营名。**对不上返回 false**（调用方决定是落回施放者阵营还是当成认不出的词）。
        ///
        /// 查法：① 与卡池里出现过的阵营名**归一化后相等**（`Saim-Hann` → `SaimHann`、
        /// `Emperor's Children` → `EmperorsChildren`，只差标点）；② 别名表（有据的种族名 → 阵营名）。
        /// </summary>
        static bool TryResolveFaction(IReadOnlyList<CardDef> pool, string word, out string faction)
        {
            faction = null;
            if (string.IsNullOrEmpty(word)) return false;
            string want = Norm(word);
            if (want.Length == 0) return false;

            if (pool != null)
                for (int i = 0; i < pool.Count; i++)
                {
                    var c = pool[i];
                    if (c == null || string.IsNullOrEmpty(c.Faction)) continue;
                    if (Norm(c.Faction) == want) { faction = c.Faction; return true; }
                }
            foreach (var pair in FactionAliases)
                if (Norm(pair[0]) == want) { faction = pair[1]; return true; }
            return false;
        }

        /// <summary>
        /// 阵营词 → 阵营名。**空串 = 按施放者自己的阵营**（规则书附录 B：每个生成器生成自己阵营的卡）。
        /// 词认不出来时也落回施放者阵营，但**会写进 `Detail`**（不静默）。
        /// </summary>
        static string ResolveFaction(IReadOnlyList<CardDef> pool, string word, string casterFaction, CreatePoolResult r)
        {
            if (string.IsNullOrEmpty(word)) return casterFaction;
            string f;
            if (TryResolveFaction(pool, word, out f)) return f;

            r.Detail = AppendDetail(r.Detail,
                "阵营词「" + word + "」在卡池里对不上 → 按施放者阵营「" + casterFaction + "」算");
            return casterFaction;
        }

        /// <summary>费用区间写进日志（`that cost 4 or less` / `2-cost`）</summary>
        static string CostScope(int min, int max)
        {
            if (min == 0 && max == 0) return "";
            if (min == max) return " + 恰好 " + min + " 费";
            if (max > 0) return " + " + max + " 费及以下";
            return " + " + min + " 费及以上";
        }

        static bool SameFaction(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        // ==================================================================
        //  附录 C 的骰子查找表 —— **只用来对账**，不是运行时数据源
        // ==================================================================
        //
        // 出处：`资料/规则书/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md` `:289-320`。
        // 为什么不当数据源：它是**实体版**的候选名单，里面有点名了但数字版卡池里没有的卡
        // （`Gauss Warrior` / `Tesla Immortal` / `Threnodic Noise Marine` / `Icon of Excess`）；
        // 而按名字造卡必须先有 `CardDef`。所以拿它**对账**：卡池算出来的池子该和它一致。
        //
        // 只收了**背后真的有 `Create …` 分句**的那 4 张表 —— 别的表（放牧者/纵欲狂欢/
        // 星界军战术…）对应的是单位卡上的生成效果，战术卡文本里没有，收进来没人用。

        /// <summary>表名 → 候选名单（英文卡名）。表名里的 `(1dN)` 是实体版掷骰的骰子。</summary>
        public static readonly string[][] DiceTables =
        {
            //  战斗药剂（1d6）—— 背后：`Create 3 random Combat Elixir in your hand`（Mercurial Host）等 5 张
            new[] { "战斗药剂(1d6)",
                    "Skorflense", "Xylocil", "Shivversplint", "Heliotrophos", "Quail", "Antrak Silk" },
            //  钛无人机·崇高牺牲（1d7）—— 背后：`Create in your hand a Gun Drone, Guardian Drone or Marker Drone`
            new[] { "钛无人机(1d7)",
                    "Guardian Drone", "Missile Drone", "Gun Drone", "Marker Drone",
                    "Stealth Drone", "Sniper Drone", "DS8 Support Turret" },
            //  虫群大军（1d10）—— 背后：`Create 3 random Leviathan troops with Swarm in your hand`
            new[] { "虫群大军(1d10)",
                    "Ripper Swarm", "Neurogaunt", "Neurogaunt Nodebeast", "Termagant", "Hormagaunt",
                    "Genestealer", "Gargoyle", "Barbgaunt", "Von Ryan's Leaper", "Termagant Brood" },
            //  锈蚀通风口（1d11）—— 背后：`Create a random troop with Ambush in your hand`
            new[] { "锈蚀通风口(1d11)",
                    "Concealed Explosives", "Neophyte Hybrid", "Neophyte Heavy", "Acolyte Hybrid",
                    "Jackal Outrider", "Acolyte Heavy", "Acolyte Specialist", "Sanctus",
                    "Hulking Aberrant", "Abominant", "Patriarch" },
            //  狂野宿主·灵族载具（1d20）—— 背后：`Create two random Saim-Hann Vehicles in your hand`
            new[] { "狂野宿主(1d20)",
                    "Weapons Platform", "Windrider", "Shroud Runner", "Shining Spear", "Vyper",
                    "Nuadhu Fireheart", "Shining Spear Exarch", "Bright Lance Vyper", "Warlock Skyrunner",
                    "War Walker", "Hornet", "Farseer Skyrunner", "Crimson Hunter", "Falcon",
                    "Fire Prism", "Wave Serpent", "Night Spinner", "Hemlock Wraithfighter",
                    "Wraithlord", "Wraithknight" },
            //  次元裂隙（1d6）—— 背后：`Deploy 3 random 2-cost Sautekh troops`
            //  ⚠️ 书里的 `Gauss Warrior` / `Tesla Immortal` 在卡池里叫
            //     `Gauss Reaper Warrior` / `Tesla Carbine Immortal`（中间多一个词）—— 名字对不上，如实报
            new[] { "次元裂隙(1d6)",
                    "Flayed One", "Gauss Warrior", "Necron Warrior", "Canoptek Plasmacyte",
                    "Tesla Immortal", "Damaged Plasmacyte" },
            //  纵欲狂欢·恶魔（1d5）—— 背后：`Deploy three random Emperor's Children Daemons that cost 5 or less`
            //  ⚠️ 书里这 5 张都算恶魔，但卡池里 `Daemonette` / `Alluress` 的 subtype 写的是 `Troop`
            //     —— 按 `subtype == Daemon` 筛只有 3 张。数据标得对不对**没核**，如实报。
            new[] { "纵欲狂欢(1d5)",
                    "Daemonette", "Alluress", "Slaanesh's Spawn", "Fiend", "Blissbringer" },
            //  空中播种/泰伦入侵（1d5）—— 背后：`Deploy 4 random 2-cost Leviathan troops`
            new[] { "空中播种(1d5)",
                    "Neurogaunt Nodebeast", "Hormagaunt", "Termagant", "Stranded Termagant",
                    "Mucolid Spore" },
        };

        // ⚠️ **2026-09-12 更正**：这里原来还有一张 `DiceGaps` 表，手写着「骰子表点名了、但卡池里没有」
        //    的卡（`Gauss Warrior` / `Tesla Immortal` / `Icon of Excess` / `Threnodic Noise Marine`）。
        //    **那四条有三条是错的** —— 卡都在，只是数字版名字更长：
        //    `Icon of Excess` → `Icon of Excess Infractor`、`Gauss Warrior` → `Gauss Reaper Warrior`、
        //    `Tesla Immortal` → `Tesla Carbine Immortal`、`Threnodic Noise Marine` → `Threnodic Choir Noise Marine`。
        //    错因和上一轮那张「原版数据里没有这张卡」的名单**一模一样**：拿短名字做精确匹配，
        //    对不上就当成「没有」。**手写的缺口清单一定会漂** —— 所以删掉，
        //    改成每次自检**算**出来（`RuleEngineTest.CheckDiceTables`，差集两个方向都报）。
    }
}
