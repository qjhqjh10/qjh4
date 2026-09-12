// EffectText.cs — 原版效果文本 → 结构化效果（战术卡 `desc` / 单位能力的解析器）
//
// **权威语义来源**：`d:/warpforge/scripts/rule_core.gd` 的 `_resolve_text`（`:2513`，672 行）。
// 那是逐条对着原版卡面修出来的规格书 —— **handler 顺序、正则、目标词都照它抄**，
// 不一致就是移植错了，而不是「我的实现更合理」。
//
// 和 `EffectSpec` 的分工：
//   · `EffectSpec`  —— **我们自己设计的 26 张卡**用的封闭文法（`Damage 2 EnemyUnit` 那种），
//     小而严，写错了自检抓得到。**不动它。**
//   · `EffectText`  —— **原版那 448 张战术卡的脏数据**用的解析器（本文件）。
//     原版卡面是英文自然语言，只能按句型试。
//
// 为什么「解析」和「结算」要分开（原版是混在一起边解边改 ctx）：
//   ① 覆盖率必须能**只解析、不动状态**地量出来 —— 「能解析 N/448」是这一轮的进度条；
//   ② 自检只验解析结果，不必造一局棋；
//   ③ 「半懂的卡比不懂更危险」—— 要能单独report「这句认了、那句没认」。
//
// ⚠️ 本文件属于 `Core/` —— **不允许依赖 UnityEngine**。解析失败**不打日志**（Core 里没有 Debug），
//    由调用方（自检 / 卡面的 `*` 标记）去暴露。
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RuleEngine
{
    // ==================================================================
    //  解析结果
    // ==================================================================

    /// <summary>一条解析出来的效果操作（一句话可以拆出多条）。</summary>
    public class EffectOp
    {
        /// <summary>`deal` / `heal` / `draw` / `destroy` / `stun` / `give` / `gain` / `lose` /
        /// `deploy` / `return` / `refill` / `lower` / `create` / `repeat` / `choose`</summary>
        public string Verb;
        /// <summary>数值。没有数字的（如 `Destroy`）= 0</summary>
        public int Amount;
        /// <summary>`1-3` 这种区间的上界；没有区间 = 0</summary>
        public int AmountMax;
        /// <summary>`give` / `gain` / `lose` 的**载荷原文**（`armour 2` / `+1 attack` / `flank`）。
        /// 由 <see cref="GivePayload"/> 再解释成属性增减益或关键词。</summary>
        public string Payload;
        /// <summary>解析出来的目标。`give`/`gain` 之外的动作才有意义</summary>
        public EffectTargetSpec Target;
        /// <summary>`""` 永久 / `turn` 本回合 / `nextturn` 到你下个回合开始
        /// （原版 `_resolve_text:3018`：`this turn` → 回合结束移除；`until your next turn` → 下回合开始移除）</summary>
        public string Duration;
        /// <summary>原文分句（日志与卡面用）——**保留原文**才好排查「到底写了什么」</summary>
        public string Source;
        /// <summary>
        /// `Deal 3 damage to an enemy **and stun it**` —— `and` 后面那半句。
        /// 原版是递归回 `_resolve_text` 再解一次（`rule_core.gd:2751`）。
        /// ⚠️ **绝不能丢**：丢了就是「打伤害但不眩晕」的静默失效。
        /// </summary>
        public string Tail;
        /// <summary>
        /// **付费激活**的代价（`rule_core.gd:2529` 那族「`N Choose` / `N [Energy]:`」）：
        /// 文本前面写着 `4 [Energy]:` / `12 [Energy]:` / `8 [Faith]:` / `2 :`，
        /// 意思是「付这么多才生效」。`0` = 不需要付费。
        /// ⚠️ 原版是**付不起就整段不激活**（不是「付了但效果减半」），结算层必须照这个来。
        /// </summary>
        public int Cost;
        /// <summary>付费资源名（`energy` / `faith` / `""`=没写）。只有 <see cref="Cost"/> &gt; 0 时有意义</summary>
        public string CostKind;

        public override string ToString()
        {
            string a = AmountMax > 0 ? Amount + "-" + AmountMax : (Amount > 0 ? Amount.ToString() : "");
            string p = string.IsNullOrEmpty(Payload) ? "" : " " + Payload;
            string t = Target != null ? " → " + Target.Raw : "";
            string d = string.IsNullOrEmpty(Duration) ? "" : " [" + Duration + "]";
            return (Verb + " " + a + p).Trim() + t + d;
        }
    }

    /// <summary>
    /// 目标短语的**组合**解释（不是 58 条硬编码）。
    ///
    /// 实测 448 张战术卡：`deal … to X` 有 38 个不同 X、`give X to Y` 有 58 个不同 Y，
    /// 但它们都是「**谁的** + **哪种** + **几个** + **时长**」的组合：
    /// `a friendly troop` / `your units this turn` / `three random enemies` / `all enemy troops` …
    /// 所以拆成字段而不是查表 —— 组合出来能覆盖的词比枚举多得多。
    /// </summary>
    public class EffectTargetSpec
    {
        /// <summary>原短语（排查用，务必留着）</summary>
        public string Raw;
        /// <summary>`self` / `own` / `enemy` / `any`</summary>
        public string Side;
        /// <summary>`unit` / `troop` / `vehicle` / `infantry` / `warlord` / `any`
        /// （原版还有更细的兵种词，遇到不认识的**原样留着**，别猜成 unit）</summary>
        public string Kind;
        /// <summary>要打几个。`0` = 全部（`all`）</summary>
        public int Count;
        /// <summary>`random` 选，还是玩家/自动规则挑</summary>
        public bool Random;
        /// <summary>包含相邻格（`adjacent units` / `its adjacent units`）</summary>
        public bool Adjacent;
        /// <summary>「每一个」——`for each friendly unit` 那种计数层（见 <see cref="EffectOp.Verb"/> 的 for-each）</summary>
        public bool Each;
        /// <summary>原文**没写**目标词，按原版的**定死规则**自动挑（如裸 `Deal N damage` → 敌方最弱单位，
        /// `rule_core.gd:2692`）。⚠️ 这不是「不知道打谁」—— 是规则明确、只是不在文本里。
        /// **不能掷骰**：同一局必须永远可复现（见 `RuleCore.ResolveTarget` 的注释）。</summary>
        public bool Auto;

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(Side).Append('/').Append(Kind);
            if (Count == 0) sb.Append(" 全部");
            else if (Count > 1) sb.Append(' ').Append(Count).Append(" 个");
            if (Random) sb.Append(" 随机");
            if (Adjacent) sb.Append(" 相邻");
            if (Each) sb.Append(" 每个");
            return sb.ToString();
        }
    }

    // ==================================================================
    //  解析器
    // ==================================================================

    public static class EffectText
    {
        /// <summary>一条分句的解析结果</summary>
        public enum SegKind
        {
            /// <summary>认出来了，产出了效果</summary>
            Ok,
            /// <summary>是**纯关键词声明**（`Ephemeral` / `Flying.` …）—— 关键词系统自己管，这里跳过不算失败</summary>
            KeywordOnly,
            /// <summary>认不出来</summary>
            Unknown,
            /// <summary>句型认了，但**目标或载荷的词表里没有**（半懂 —— 单独报，别混进「能解析」）</summary>
            Partial,
        }

        // ------------------------------------------------------------------
        //  分句
        // ------------------------------------------------------------------

        /// <summary>
        /// 按 `.` 分句 —— 和原版 `_resolve_text:2571` 的 `desc.split(".")` 一致。
        /// ⚠️ **照抄不要「改良」**：原版就是朴素按句点切，多句卡（448 张里 193 张）全靠这一层拆开。
        /// </summary>
        public static List<string> Split(string desc)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(desc)) return list;
            foreach (var raw in desc.Split('.'))
            {
                string s = raw.Trim();
                if (s.Length > 0) list.Add(s);
            }
            return list;
        }

        // ------------------------------------------------------------------
        //  对外入口
        // ------------------------------------------------------------------

        /// <summary>
        /// 解析整条 `desc`。
        /// </summary>
        /// <param name="unparsed">认不出来的分句（原文），按出现顺序</param>
        /// <param name="partial">句型认了、但目标/载荷词表里没有的分句（原文）</param>
        /// <returns>解析出来的操作；**空表不等于失败**（可能整句都是关键词声明）</returns>
        public static List<EffectOp> Parse(string desc, out List<string> unparsed, out List<string> partial)
        {
            unparsed = new List<string>();
            partial = new List<string>();
            var ops = new List<EffectOp>();

            foreach (string seg in Split(desc))
            {
                var r = ParseSegment(seg);
                if (r.Ops != null) ops.AddRange(r.Ops);
                if (r.Kind == SegKind.Unknown) unparsed.Add(seg);
                else if (r.Kind == SegKind.Partial) partial.Add(seg);
            }
            return ops;
        }

        public class SegResult
        {
            public SegKind Kind = SegKind.Unknown;
            public List<EffectOp> Ops;
        }

        /// <summary>
        /// 这一句是不是**纯关键词声明**（`Ephemeral` / `Blast 3` / `Blood Thirst`）。
        ///
        /// 判据：去掉结尾的数字/标点后，**关键词前缀要吃满整句** ——
        /// `KeywordTable.Normalize` 只做前缀匹配，单看「非 null」会把 `Stun a random enemy` 也认成关键词。
        /// </summary>
        public static bool IsKeywordOnly(string seg)
        {
            if (string.IsNullOrEmpty(seg)) return false;
            string t = Regex.Replace(seg.Trim(), @"[\s\d\.\-,;]+$", "").Trim().ToLowerInvariant();
            if (t.Length == 0) return false;
            if (t.Contains(":")) return false;              // `Talent: …` / `Rally: …` 是效果段落，不是纯关键词
            int len;
            if (KeywordTable.Normalize(t, out len) == null) return false;
            return len == t.Length;                          // 吃满整句才算
        }

        // ------------------------------------------------------------------
        //  覆盖率（这一轮的进度条）
        // ------------------------------------------------------------------

        /// <summary>
        /// 一批卡的文本解析覆盖情况。
        ///
        /// **为什么要分三个数**：「能解析」和「解析得对」不是一件事 ——
        /// 一句话认了句型、但目标词表里没有（`units adjacent to the target`），
        /// 那是**半懂**，比完全不懂更危险（卡看起来能跑、实际少打一半）。所以单独报。
        /// </summary>
        public class TextCoverage
        {
            public int Cards;
            /// <summary>整条 desc 都解析干净了的卡数（**这才是「能打」的口径**）</summary>
            public int Full;
            /// <summary>有话认了、也有话认不出来的卡数</summary>
            public int Partial;
            /// <summary>一句都没认出来的卡数</summary>
            public int None;

            /// <summary>
            /// **载荷有机制**的卡数（在 <see cref="Full"/> 的基础上再过一层）。
            /// `give Flank to a friendly troop` 解析得出来，但 `Flank` 没机制 → 「能打但没用」。
            /// ⚠️ 两个数**必须分开报**：只报「能解析」会掩盖这类**静默失效**。
            /// </summary>
            public int FullAndMechanized;
            /// <summary>解析得出来、但载荷没机制的卡名（卡面该打 `*`）</summary>
            public readonly List<string> NoMechCards = new List<string>();
            /// <summary>缺机制的关键词 → 出现次数（按频次排，决定下一个补哪个关键词）</summary>
            public readonly Dictionary<string, int> NoMechFreq = new Dictionary<string, int>();

            public int SegTotal, SegKeyword, SegOk, SegPartial, SegUnknown;

            public readonly List<string> UnknownExamples = new List<string>();
            public readonly List<string> PartialExamples = new List<string>();
            /// <summary>分句 → 出现次数（**按频次排**才知道下一个该实现哪个 handler，
            /// 比「前 40 个不认识的」有用得多）</summary>
            public readonly Dictionary<string, int> UnknownFreq = new Dictionary<string, int>();
            public readonly Dictionary<string, int> PartialFreq = new Dictionary<string, int>();
            /// <summary>完全解析不了的卡名（卡面该打 `*`）</summary>
            public readonly List<string> NoneCards = new List<string>();

            public string Summary()
            {
                return $"战术卡文本解析：完全解析 {Full}/{Cards}"
                     + $"，其中**载荷有机制** {FullAndMechanized}"
                     + $"，部分 {Partial}，完全不懂 {None}"
                     + $"（分句 {SegTotal}：关键词声明 {SegKeyword} / 认了 {SegOk}"
                     + $" / 半懂 {SegPartial} / 不认 {SegUnknown}）";
            }
        }

        /// <summary>量一批卡的文本解析覆盖（**只解析、不动状态**）。默认只看战术卡。</summary>
        public static TextCoverage Coverage(IEnumerable<CardDef> cards, string type = "tactic")
        {
            var cov = new TextCoverage();
            if (cards == null) return cov;
            var seenUnknown = new HashSet<string>();
            foreach (var c in cards)
            {
                if (c == null) continue;
                if (type != null && c.Type != type) continue;
                cov.Cards++;

                var unparsed = new List<string>();
                var partial = new List<string>();
                var ops = Parse(c.Desc, out unparsed, out partial);

                int kwOnly = 0, ok = 0, bad = 0;
                foreach (string seg in Split(c.Desc))
                {
                    var r = ParseSegment(seg);
                    switch (r.Kind)
                    {
                        case SegKind.KeywordOnly: kwOnly++; break;
                        case SegKind.Ok: ok++; break;
                        case SegKind.Partial: bad++; break;
                        default: bad++; break;
                    }
                }
                cov.SegKeyword += kwOnly;
                cov.SegOk += ok;
                cov.SegPartial += partial.Count;
                cov.SegUnknown += unparsed.Count;

                if (unparsed.Count == 0 && partial.Count == 0) cov.Full++;
                else if (unparsed.Count > 0 && ok + kwOnly == 0) { cov.None++; cov.NoneCards.Add(c.Name); }
                else cov.Partial++;

                // ---- 第二层：载荷**有机制**吗 ----
                if (unparsed.Count == 0 && partial.Count == 0)
                {
                    bool allMech = true;
                    foreach (var op in ops)
                    {
                        if (string.IsNullOrEmpty(op.Payload)) continue;
                        string why;
                        if (GivePayload.Mechanized(op.Payload, out why)) continue;
                        allMech = false;
                        string key = why + "  ← " + op.Payload;
                        int n2;
                        cov.NoMechFreq[key] = cov.NoMechFreq.TryGetValue(key, out n2) ? n2 + 1 : 1;
                    }
                    if (allMech) cov.FullAndMechanized++;
                    else if (cov.NoMechCards.Count < 30) cov.NoMechCards.Add(c.Name);
                }

                foreach (string s in unparsed)
                {
                    if (cov.UnknownExamples.Count < 40 && seenUnknown.Add(s))
                        cov.UnknownExamples.Add(c.Name + " · " + s);
                    int n0;
                    cov.UnknownFreq[s] = cov.UnknownFreq.TryGetValue(s, out n0) ? n0 + 1 : 1;
                }
                foreach (string s in partial)
                {
                    if (cov.PartialExamples.Count < 25) cov.PartialExamples.Add(c.Name + " · " + s);
                    int n1;
                    cov.PartialFreq[s] = cov.PartialFreq.TryGetValue(s, out n1) ? n1 + 1 : 1;
                }

                cov.SegTotal += kwOnly + ok + bad;
            }
            return cov;
        }

        // ------------------------------------------------------------------
        //  逐分句：handler 管线
        // ------------------------------------------------------------------

        /// <summary>
        /// 一条分句 → 效果操作。
        ///
        /// **handler 顺序照抄 `rule_core._resolve_text`（`:2516` 的函数头写着）**：
        ///   deal → stun → destroy → heal → draw → return → refill → deploy → give/lose/gain
        /// ⚠️ **顺序本身是语义的一部分**（先具体后一般），别按字母序重排。
        /// </summary>
        public static SegResult ParseSegment(string seg)
        {
            var r = new SegResult { Ops = new List<EffectOp>() };
            if (string.IsNullOrEmpty(seg)) { r.Kind = SegKind.KeywordOnly; return r; }

            string s = seg.Replace("[", "").Replace("]", "").Trim();
            // 圈码（`① ② ③`）是**次数标记**，不是句型的一部分 —— 原版靠 `contains("repeat this effect")`
            // 直接绕过它，我们把它剥掉，好让「不认识的句子」按频次排名时 key 是干净的
            // （不然 `② Repeat this effect` 和 `Repeat this effect` 会被算成两句）。
            s = StripCircled(s).Trim();
            if (s.Length == 0) { r.Kind = SegKind.KeywordOnly; return r; }
            string low = s.ToLowerInvariant();

            // ---- 付费激活前缀 `12 [Energy]: …` / `4 : …` / `8 [Faith]: …` ----
            // `rule_core.gd:2529` 那族（原版是「付不起就**整段不激活**」）。前缀在这里剥掉、
            // 代价记进 op，正文照常往下走各个 handler。
            int paidCost = 0; string paidKind = null;
            var mp = RePaid.Match(low);
            if (mp.Success)
            {
                paidCost = int.Parse(mp.Groups[1].Value);
                paidKind = mp.Groups[2].Success ? mp.Groups[2].Value : "";
                low = mp.Groups[3].Value.Trim();
                s = low;
                if (low.Length == 0) { r.Kind = SegKind.Unknown; r.Ops = null; return r; }
            }

            // ---- 纯关键词声明（`Ephemeral` / `Flying` / `Blast 3`…）----
            // 卡的关键词由 `CardDef` 从 `keywords` 字段单独解析，这里再声明一次是冗余 —— 跳过，不算失败。
            // ⚠️ **必须确认前缀吃满整句**：`KeywordTable.Normalize` 是前缀匹配，
            //    `"Stun a random enemy"` 也会命中 `stun` —— 只看非 null 会把整句效果跳过去（静默失效）。
            if (IsKeywordOnly(s)) { r.Kind = SegKind.KeywordOnly; return r; }

            r = Dispatch(low, s);
            // 付费代价记到**这一句产出的每条 op** 上（`12 [Energy]: Draw 3 cards`）
            if (paidCost > 0 && r.Ops != null)
                foreach (var op in r.Ops) { op.Cost = paidCost; op.CostKind = paidKind; }
            return r;
        }

        /// <summary>handler 管线（付费前缀已由调用方剥掉、记在 op.Cost 上）</summary>
        static SegResult Dispatch(string low, string src)
        {
            var r = new SegResult { Ops = new List<EffectOp>() };
            EffectOp op;

            // ---- 1) Deal N damage [to X]   (`:2672`) ----
            op = TryDeal(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- 2) Stun   (`:2755`) ----
            op = TryStun(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- 3) Destroy   (`:2818`) ----
            op = TryDestroy(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- 4) Heal N   (`:2839`) ----
            op = TryHeal(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- 5) Draw N / Draw a <类型>   (`:2863` / 5a `:2867`) ----
            op = TryDraw(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- 7b/7c) 降费：`Lower the cost of X by N` / `(it|they) cost(s) N less` ----
            //      （`:2920` / `:2927`）
            op = TryLowerCost(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- 8) Refill energy   (`:2952`) ----
            op = TryRefill(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- 9) Deploy X   (`:2970`) ----
            op = TryDeploy(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- Reanimate（打捞墓地，原版在 `_resolve_tactic` 族里）----
            op = TryReanimate(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- Repeat this effect   (`:2542` → `_resolve_repeat`) ----
            op = TryRepeat(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- 6) Give X to Y   (`:3010`，三种语序) ----
            op = TryGive(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- 6b) Lose X   (`:3082`) ----
            op = TryLose(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- 6c) Gain X   (`:3101`) ----
            op = TryGain(low, src);
            if (op != null) return Finish(r, op, src);

            r.Kind = SegKind.Unknown;
            r.Ops = null;
            return r;
        }

        /// <summary>
        /// 收尾：把一条 op 记下来，**并把 `and` 尾句也解出来**。
        ///
        /// `Deal 3 damage to an enemy and stun it` —— 尾句必须解；解不出来就整句判
        /// <see cref="SegKind.Partial"/>（**半懂要报出来**，绝不能「打了伤害就装作没这回事」）。
        /// </summary>
        static SegResult Finish(SegResult r, EffectOp op, string src)
        {
            r.Ops.Add(op);
            r.Kind = op.Target == null && op.Verb != "draw" ? SegKind.Partial : SegKind.Ok;

            if (!string.IsNullOrEmpty(op.Tail))
            {
                var tail = ParseSegment(op.Tail);
                if (tail.Ops != null) r.Ops.AddRange(tail.Ops);
                // 尾句没认出来 / 只有半懂 → 整句降级成半懂
                if (tail.Kind != SegKind.Ok && tail.Kind != SegKind.KeywordOnly) r.Kind = SegKind.Partial;
            }
            return r;
        }

        /// <summary>
        /// `X and <另一句>` 的切分。**判据是「and 后面那截像不像一个新效果」**：
        ///   · `an enemy **and** stun it`            → 后面是动词 `stun` → 切，尾句递归
        ///   · `an enemy **and** its adjacent units` → 后面是目标词的延续 → **不切**，整段当目标
        ///
        /// ⚠️ 这里**故意和 `rule_core.gd:2685` 不一样**：原版无条件在第一个 ` and ` 切，
        ///    于是 `…and its adjacent units` 的「相邻」会被切掉、当成一句解不出来的尾句丢掉。
        ///    我们按「后面是不是动词」判 —— 是**超集**（原版能解的我们都能解，且不丢相邻）。
        /// </summary>
        static void SplitAndTail(string tok, out string targetPart, out string tail)
        {
            tail = "";
            targetPart = tok;
            if (string.IsNullOrEmpty(tok)) return;
            int idx = tok.IndexOf(" and ");
            if (idx < 0) return;
            string t = tok.Substring(idx + 5).Trim();
            if (t.Length == 0) return;
            if (IsVerbWord(FirstWord(t))) { targetPart = tok.Substring(0, idx).Trim(); tail = t; }
        }

        static string FirstWord(string s)
        {
            int i = s.IndexOf(' ');
            return (i < 0 ? s : s.Substring(0, i)).Trim().TrimEnd(',', ':');
        }

        /// <summary>剥掉圈码次数标记（`①`–`⑳`）—— 它们不是句型的一部分</summary>
        static string StripCircled(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s)
                if (c >= '①' && c <= '⑳') continue;   // ①..⑳
                else sb.Append(c);
            return sb.ToString();
        }

        /// <summary>句首是不是一个**效果动词**（决定 `and` 后面算新效果还是目标的延续）</summary>
        static bool IsVerbWord(string w)
        {
            switch ((w ?? "").ToLowerInvariant())
            {
                case "deal": case "deals": case "stun": case "stuns":
                case "destroy": case "destroys": case "heal": case "heals":
                case "draw": case "draws": case "give": case "gives":
                case "gain": case "gains": case "lose": case "loses":
                case "deploy": case "deploys": case "return": case "returns":
                case "refill": case "create": case "creates": case "reanimate":
                    return true;
                default: return false;
            }
        }

        // ==================================================================
        //  各个 handler（正则出处写在每条的注释里）
        // ==================================================================

        static readonly Regex ReDeal = new Regex(
            @"deals?\s+(?:(\d+)(?:-(\d+))?\s+)?damage(?:\s+to\s+(.+?))?$",
            RegexOptions.Compiled);

        /// <summary>`Deal X damage [to Y]` —— `rule_core.gd:2672`。
        /// 目标词缺省（`Deal 3 damage`）= 原版自动选敌方最弱单位（`:2692`）。</summary>
        static EffectOp TryDeal(string low, string src)
        {
            var m = ReDeal.Match(low);
            if (!m.Success) return null;
            var op = new EffectOp { Verb = "deal", Source = src };
            if (m.Groups[1].Success) op.Amount = int.Parse(m.Groups[1].Value);
            if (m.Groups[2].Success) op.AmountMax = int.Parse(m.Groups[2].Value);
            string tok = m.Groups[3].Success ? m.Groups[3].Value.Trim() : "";

            // `Deal X to Y and <另一句>` —— 原版在这里递归解尾句（`rule_core.gd:2751`），
            // 但它是**无条件**在第一个 ` and ` 切；我们按「后面是不是动词」判（见 `SplitAndTail`）。
            SplitAndTail(tok, out tok, out op.Tail);

            if (tok.Length == 0)
            {
                // 裸 `Deal N damage`（如 Fire prism）—— 原版是**定死的规则**：自动选敌方最弱单位
                // （`:2692` → `_auto_fx_target(ctx, p, TACTIC_ENEMY_PICK)`）。**不是**「不知道打谁」。
                op.Target = new EffectTargetSpec
                {
                    Raw = "(未写目标：按原版规则自动选敌方最弱单位)",
                    Side = "enemy", Kind = "any", Count = 1, Auto = true,
                };
            }
            else op.Target = ParseTarget(tok);
            return op;
        }

        /// <summary>`Stun [a|an|the|it] [enemy] [unit|troop]` —— `rule_core.gd:2755`。
        /// `Stun N random enemies` 的 N 在 `random` 那条分支里取（`:2759`）。</summary>
        static EffectOp TryStun(string low, string src)
        {
            if (!ReStun.IsMatch(low)) return null;
            var op = new EffectOp { Verb = "stun", Amount = 1, Source = src };
            var m = Regex.Match(low, @"stun\s+(\d+|two|three)\s+");
            if (m.Success) op.Amount = CountWord(m.Groups[1].Value);
            return op;
        }
        static readonly Regex ReStun = new Regex(@"\bstun\b", RegexOptions.Compiled);

        /// <summary>`Destroy`（含 `destroy it instead` 的条件形式 —— 条件由上层判，这里只认动词）。
        /// `rule_core.gd:2818`；⚠️ 原版 `Invulnerable` 挡得住（`:2820`），那在结算层。</summary>
        static EffectOp TryDestroy(string low, string src)
        {
            if (!ReDestroy.IsMatch(low)) return null;
            var op = new EffectOp { Verb = "destroy", Source = src };
            // `Destroy a random enemy troop` / `Destroy an enemy` —— 动词后面就是目标
            var m = Regex.Match(low, @"^destroy\s+(.+)$");
            if (m.Success)
            {
                string tok = m.Groups[1].Value.Trim();
                // `it` / `the target` / `them` 指代前面的目标，不是新目标
                if (!IsPronoun(tok)) op.Target = ParseTarget(tok);
            }
            return op;
        }
        static readonly Regex ReDestroy = new Regex(@"\bdestroy\b", RegexOptions.Compiled);

        /// <summary>`Heal N [them|<目标>]` —— `rule_core.gd:2839`：**没写目标 = 治己方督军**（`:2842`）。</summary>
        static EffectOp TryHeal(string low, string src)
        {
            var m = ReHeal.Match(low);
            if (!m.Success) return null;
            var op = new EffectOp { Verb = "heal", Source = src };
            op.Amount = m.Groups[1].Success ? int.Parse(m.Groups[1].Value) : 0;
            if (op.Amount == 0) op.Amount = CountWordOrZero(m.Groups[2].Value);
            string tok = m.Groups[3].Success ? m.Groups[3].Value.Trim() : "";
            if (tok.Length > 0 && IsPronoun(tok)) tok = "";       // `Heal them N` = 己方全体
            op.Target = tok.Length == 0 ? null : ParseTarget(tok);
            return op;
        }
        static readonly Regex ReHeal = new Regex(
            @"^heals?\s+(?:(\d+)|(two|three|four|five))?(?:\s*(?:points? of )?(?:health|damage)?)?\s*(?:to\s+(.+))?$",
            RegexOptions.Compiled);

        /// <summary>
        /// `Draw N [cards]`（`rule_core.gd:2863`）与 **5a 定向翻找** `Draw a <类型> [from your deck]`（`:2867`）。
        ///
        /// ⚠️ 先试**常规**那条：`Draw a card` 走常规；只有常规整句匹配不上、且剩下一个**类型词**时，
        ///    才当定向翻找。反过来会把 `Draw a card` 认成「翻找 card 类型」。
        /// </summary>
        static EffectOp TryDraw(string low, string src)
        {
            var m = ReDraw.Match(low);
            if (m.Success)
            {
                var op0 = new EffectOp { Verb = "draw", Source = src, Amount = 1 };
                if (m.Groups[1].Success) op0.Amount = int.Parse(m.Groups[1].Value);
                else if (m.Groups[2].Success) op0.Amount = CountWord(m.Groups[2].Value);
                return op0;
            }

            var mt = ReDrawType.Match(low);
            if (mt.Success)
            {
                var op = new EffectOp { Verb = "drawtype", Source = src, Amount = 1 };
                if (mt.Groups[1].Success) op.Amount = int.Parse(mt.Groups[1].Value);
                op.Payload = mt.Groups[2].Value.Trim();     // 类型词（troop / vehicle…）
                return op;
            }
            return null;
        }
        static readonly Regex ReDraw = new Regex(
            @"^draws?\s+(?:(\d+)|(a|an|two|three))?\s*(?:cards?|card)?\s*$", RegexOptions.Compiled);
        /// <summary>5a 定向翻找：`Draw (N)? (a|an|the)? <类型> (from your deck)?`</summary>
        static readonly Regex ReDrawType = new Regex(
            @"^draws?\s+(?:(\d+)\s+)?(?:a|an|the\s+)?([a-z]+)\s*(?:cards?)?\s*(?:from (?:your|the) deck)?$",
            RegexOptions.Compiled);

        /// <summary>
        /// 降费 —— `rule_core.gd:2920`（`Lower the cost of (all )?&lt;X&gt; in your hand( and deck)? by N`）
        /// 与 `:2927`（`(it|they) cost(s)? N less`，指代前面刚回手/刚生成的那张）。
        /// </summary>
        static EffectOp TryLowerCost(string low, string src)
        {
            var m1 = ReLowerCost.Match(low);
            if (m1.Success)
            {
                var op = new EffectOp { Verb = "lowercost", Source = src, Payload = m1.Groups[1].Value.Trim() };
                op.Amount = m1.Groups[2].Success ? int.Parse(m1.Groups[2].Value) : 1;
                return op;
            }
            var m2 = ReCostLess.Match(low);
            if (m2.Success)
            {
                return new EffectOp
                {
                    Verb = "lowercost", Source = src, Amount = int.Parse(m2.Groups[1].Value),
                    Payload = "(指代上一张)",     // `It costs 1 less` —— 作用于前一句提到的那张
                };
            }
            return null;
        }
        static readonly Regex ReLowerCost = new Regex(
            @"^lower the cost of (.+?)(?:\s+by\s+(\d+))?$", RegexOptions.Compiled);
        static readonly Regex ReCostLess = new Regex(
            @"^(?:it|they)\s+costs?\s+(\d+)\s+less$", RegexOptions.Compiled);

        /// <summary>`Reanimate a friendly Remnant` —— 从墓地把单位捞回场上（原版在 `_resolve_tactic` 族）。</summary>
        static EffectOp TryReanimate(string low, string src)
        {
            var m = ReReanimate.Match(low);
            if (!m.Success) return null;
            var op = new EffectOp { Verb = "reanimate", Source = src };
            string tok = m.Groups[1].Value.Trim();
            op.Target = IsPronoun(tok) ? null : ParseTarget(tok);
            return op;
        }
        static readonly Regex ReReanimate = new Regex(@"^reanimates?\s+(.+)$", RegexOptions.Compiled);

        /// <summary>
        /// `Repeat this effect`（`rule_core.gd:2542` → `_resolve_repeat`）：把**本句之前的效果**再来一遍。
        /// 三种变体：纯重复 / 付费重复 / 条件重复（`If any troop dies, repeat this effect`）。
        /// `Payload` 存重复的条件原文（空 = 无条件）。
        /// </summary>
        static EffectOp TryRepeat(string low, string src)
        {
            int idx = low.IndexOf("repeat this effect");
            if (idx < 0) return null;
            var op = new EffectOp { Verb = "repeat", Source = src, Amount = 1 };
            string before = low.Substring(0, idx).Trim().TrimEnd(',', ';');
            if (before.Length > 0) op.Payload = before;    // 条件（`If any troop dies` / `for every friendly Vehicle`）
            return op;
        }

        /// <summary>
        /// **付费激活前缀** `12 [Energy]: …` / `4 : …` / `8 [Faith]: …`（`rule_core.gd:2529` 那族）。
        /// 组 3 = 正文（前缀剥掉后继续走 handler 管线）。
        /// </summary>
        static readonly Regex RePaid = new Regex(
            @"^(\d+)\s*(?:\[\s*(energy|faith|spirit stones?|might|attack|health|icon)\s*\]\s*|\s+)?:\s*(.+)$",
            RegexOptions.Compiled);

        /// <summary>`Refill N Energy` / `Refill Energy` —— `rule_core.gd:2952`、`:2956`。</summary>
        static EffectOp TryRefill(string low, string src)
        {
            if (!ReRefill.IsMatch(low)) return null;
            var op = new EffectOp { Verb = "refill", Source = src };
            var m = Regex.Match(low, @"refills?\s+(\d+)");
            op.Amount = m.Success ? int.Parse(m.Groups[1].Value) : 0;   // 0 = 回满
            return op;
        }
        static readonly Regex ReRefill = new Regex(@"^refill", RegexOptions.Compiled);

        /// <summary>`Deploy <卡名>` / `Deploy N random units` —— `rule_core.gd:2970`。</summary>
        static EffectOp TryDeploy(string low, string src)
        {
            var m = ReDeploy.Match(low);
            if (!m.Success) return null;
            var op = new EffectOp { Verb = "deploy", Source = src };
            op.Payload = m.Groups[1].Success ? m.Groups[1].Value.Trim() : "";
            return op;
        }
        static readonly Regex ReDeploy = new Regex(@"^deploys?\s+(.+)$", RegexOptions.Compiled);

        /// <summary>
        /// `Give X to Y` —— `rule_core.gd:3010`，**三种语序**：
        ///   ① `Give <内容> to <目标>`（常规）
        ///   ② `Give to <目标> <内容>`（Enhanced Aggression 卡面原文）
        ///   ③ `Give it <内容>` / `Give this unit <内容>`（无 `to`）
        /// 时长修饰 `this turn` / `until your next turn` 在内容或目标里都可能出现（`:3018`）。
        /// </summary>
        static EffectOp TryGive(string low, string src)
        {
            var m = ReGive.Match(low);
            if (!m.Success) return null;
            var op = new EffectOp { Verb = "give", Source = src };

            string payload, targetText;
            if (m.Groups[1].Success)          // ② Give to <目标> <内容>
            {
                targetText = m.Groups[1].Value.Trim();
                payload = m.Groups[2].Value.Trim();
            }
            else if (m.Groups[4].Success)     // ① Give <内容> to <目标>
            {
                payload = m.Groups[3].Value.Trim();
                targetText = m.Groups[4].Value.Trim();
            }
            else                              // ③ Give it <内容>
            {
                payload = m.Groups[5].Value.Trim();
                targetText = "";
            }

            // 时长修饰：原文里跟着「内容」或「目标」，两边都要看
            op.Duration = ExtractDuration(ref payload, ref targetText);
            op.Payload = payload;
            op.Target = targetText.Length == 0 ? null : ParseTarget(targetText);
            return op;
        }
        static readonly Regex ReGive = new Regex(
            @"^gives?\s+(?:to\s+(.+?)\s+(.+)" +                       // ②
            @"|(.+?)\s+to\s+(.+)" +                                   // ①
            @"|(?:it|this unit|this troop|them)\s+(.+))$",            // ③
            RegexOptions.Compiled);

        /// <summary>`All enemies lose Stealth` / `Lose X` —— `rule_core.gd:3082`。</summary>
        static EffectOp TryLose(string low, string src)
        {
            var m = ReLose.Match(low);
            if (!m.Success) return null;
            var op = new EffectOp { Verb = "lose", Source = src };
            string subj = m.Groups[1].Success ? m.Groups[1].Value.Trim() : "";
            op.Payload = m.Groups[2].Value.Trim();
            op.Duration = ExtractDuration(ref op.Payload, ref subj);
            op.Target = subj.Length == 0 ? null : ParseTarget(subj);
            return op;
        }
        static readonly Regex ReLose = new Regex(
            @"^(?:all\s+)?(.+?)\s+loses?\s+(.+)$|^(?:all\s+)?loses?\s+(.+)$", RegexOptions.Compiled);

        /// <summary>`Gain X` —— `rule_core.gd:3101`。三种：属性增减益 / 关键词 / **裸数字 = 1 能量**（`:3114`）。</summary>
        static EffectOp TryGain(string low, string src)
        {
            var m = ReGain.Match(low);
            if (!m.Success) return null;
            var op = new EffectOp { Verb = "gain", Source = src };
            string subj = m.Groups[1].Success ? m.Groups[1].Value.Trim() : "";
            string what = m.Groups[2].Success ? m.Groups[2].Value.Trim() : m.Groups[3].Value.Trim();
            op.Duration = ExtractDuration(ref what, ref subj);
            op.Payload = what;

            // `Gain 1` 裸数字（没有属性词）= **1 点能量** —— `rule_core.gd:3114`。
            // ⚠️ 原版那里用 `\s*` 而不是 `\s+`：句尾的 `gain 1` 后面没有空格，用 `\s+` **永远失配**。
            if (Regex.IsMatch(op.Payload, @"^\d+$"))
            {
                op.Payload = op.Payload + " energy";
                op.Verb = "gainenergy";
                op.Target = new EffectTargetSpec { Raw = "(自己的能量)", Side = "own", Kind = "player", Count = 1, Auto = true };
                return op;
            }

            // 主语是真实在场目标才用主语（`Your Warlord gains X`）；`the next Beast you play` 这类
            // 挂起型主语落回无主语。**无主语 → 友方全体** —— 这是原版自己标为「既有近似」的取舍
            // （`rule_core.gd:3137`），照抄，并在注释里如实标明它是近似。
            if (subj.Length == 0 || IsSuspendedSubject(subj))
                op.Target = new EffectTargetSpec
                {
                    Raw = "(未写主语：按原版落到己方全体 —— 原版自标为近似)",
                    Side = "own", Kind = "unit", Count = 0, Auto = true,
                };
            else op.Target = ParseTarget(subj);
            return op;
        }
        static readonly Regex ReGain = new Regex(
            @"^(.+?)\s+gains?\s+(.+)$|^gains?\s+(.+)$", RegexOptions.Compiled);

        // ==================================================================
        //  目标短语 → EffectTargetSpec
        // ==================================================================

        /// <summary>
        /// 目标短语的组合解释。返回 `null` = **词表里没有这个词**（调用方要报 Partial，不许当成 unit）。
        ///
        /// ⚠️ 认不出来**宁可返回 null**：把 `units adjacent to the target` 猜成「一个单位」会让卡
        ///    看起来能跑、实际少打一半 —— 比不认识更糟（本工程的静默失败红线）。
        /// </summary>
        public static EffectTargetSpec ParseTarget(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            string t = text.Trim().ToLowerInvariant();
            var spec = new EffectTargetSpec { Raw = text.Trim(), Count = 1, Kind = "any", Side = "any" };

            // `it` / `them` / `the target` —— 指代**上一条效果的目标**，不是新目标。
            // 原版用 `it_target` 记着（`:2730`），我们让调用方按顺序接。
            if (IsPronoun(t)) { spec.Side = "prev"; spec.Kind = "prev"; return spec; }

            if (t.Contains("adjacent")) spec.Adjacent = true;
            if (t.Contains("random")) spec.Random = true;

            // ---- 谁的 ----
            if (t.Contains("your warlord") || t.Contains("friendly warlord")) { spec.Side = "own"; spec.Kind = "warlord"; }
            else if (t.Contains("enemy warlord")) { spec.Side = "enemy"; spec.Kind = "warlord"; }
            else if (t.Contains("your") || t.Contains("friendly")) spec.Side = "own";
            else if (t.Contains("enemy") || t.Contains("enemies")) spec.Side = "enemy";
            else if (t.Contains("all units") || t.Contains("all troops")) spec.Side = "any";

            // ---- 哪种 ----
            if (spec.Kind != "warlord")
            {
                if (t.Contains("warlord")) spec.Kind = "warlord";
                else if (t.Contains("troop")) spec.Kind = "troop";
                else if (t.Contains("vehicle")) spec.Kind = "vehicle";
                else if (t.Contains("infantry")) spec.Kind = "infantry";
                else if (t.Contains("unit")) spec.Kind = "unit";
                else if (t.Contains("enem")) spec.Kind = "any";     // `an enemy` = 任意敌方单位
                else if (t.Contains("target")) spec.Kind = "prev";
                else return null;                                   // 不认识的兵种词 → 不猜
            }

            // ---- 几个 ----
            if (t.StartsWith("all ") || t.Contains(" all ")) spec.Count = 0;
            else
            {
                var mc = Regex.Match(t, @"\b(\d+|two|three|four|five)\b");
                if (mc.Success) spec.Count = CountWord(mc.Groups[1].Value);
                else spec.Count = 1;
            }

            if (t.Contains("for each") || t.Contains("for every")) spec.Each = true;
            return spec;
        }

        static bool IsPronoun(string t)
        {
            t = t.Trim().ToLowerInvariant();
            return t == "it" || t == "them" || t == "the target" || t == "this unit"
                || t == "that unit" || t == "the unit" || t == "this troop" || t == "that troop";
        }

        static bool IsSuspendedSubject(string t)
        {
            t = t.Trim().ToLowerInvariant();
            return t.StartsWith("the next ") || t.StartsWith("the first ");
        }

        /// <summary>`this turn` / `until your next turn` 从文本里摘出来并**从原串里删掉**
        /// （原版 `:3018`、`:3019`）</summary>
        static string ExtractDuration(ref string a, ref string b)
        {
            string dur = "";
            if (a != null && a.Contains("until your next turn"))
            { dur = "nextturn"; a = a.Replace("until your next turn", "").Trim(); }
            else if (b != null && b.Contains("until your next turn"))
            { dur = "nextturn"; b = b.Replace("until your next turn", "").Trim(); }
            if (dur.Length == 0)
            {
                if (a != null && Regex.IsMatch(a, @"\bthis turn\b"))
                { dur = "turn"; a = Regex.Replace(a, @"\bthis turn\b", "").Trim(); }
                else if (b != null && Regex.IsMatch(b, @"\bthis turn\b"))
                { dur = "turn"; b = Regex.Replace(b, @"\bthis turn\b", "").Trim(); }
            }
            return dur;
        }

        static int CountWord(string w)
        {
            switch ((w ?? "").Trim().ToLowerInvariant())
            {
                case "a": case "an": return 1;
                case "two": return 2;
                case "three": return 3;
                case "four": return 4;
                case "five": return 5;
                default:
                    int n;
                    return int.TryParse(w, out n) && n > 0 ? n : 1;
            }
        }

        static int CountWordOrZero(string w)
        {
            if (string.IsNullOrEmpty(w)) return 0;
            return CountWord(w);
        }
    }
}
