// Aura.cs — 「光环」：卡面 `X have Y` 那一族（A7，2026-09-14）
//
// **它是什么**：`Adjacent units have Armour 1` · `Your other units have +2 Ranged Attack` ·
// `Friendly Vehicles have Flank` · `Enemies have Vulnerable 1` —— 给**一组单位挂一个持续加成**，
// 来源在场就有效、来源离场就没了。语义与结算形状的权威在
// `资料/常驻效果_数据与设计.md` §八（**别在这儿抄第二份**），本文件只管**把句子认下来**。
//
// **为什么单开一个文件**（而不是塞进 `EffectText` / 做成一条 op）：
//   · 光环**不是一条「结算得了的效果」** —— 它没有触发时机，是**状态的生命周期**（谁在什么时候收回）。
//     做成 op 的话，打出这张单位卡时会去「结算」它，那是把常驻当成一次性的。
//   · 所以形状照 **`CardDef.WhenTriggers`（事件层）** 与 **`MatchStaticBattleRule`（静态改战斗规则，
//     A5 批 4）**：本文件**只负责认句子 + 出数据**，`CardDef` 收到 `AuraSpecs` 上，
//     真正的读点在结算层（A7 第 3 步）。
//
// 🔴 **不产生 op、不进 `EffectDispatch`**（和 `costwhen` 那种「标记 op」**不同**）——
//   标记 op 是为了让战术卡「解析得出来、打得出去」；光环句**全在单位卡/督军卡上**
//   （全池实测 30 张，`[tactic]`/`[defence]` **0 张**），而单位卡**不经过 `IsFullyParsed` 那三道闸**
//   （三处消费点都带 `c.Type == "tactic"`）⇒ **没有「必须让它解析得出来」的压力**，
//   硬造一条 op 只会给结算层添一条永远空跑的动词。
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RuleEngine
{
    /// <summary>
    /// 一条光环：**谁**（<see cref="Filter"/> / <see cref="Adjacent"/> / <see cref="Enemy"/>）
    /// **得到什么**（<see cref="Payload"/>）。由 <see cref="Auras.TryParse"/> 产出。
    /// </summary>
    public class AuraSpec
    {
        /// <summary>锚点原文（`adjacent` / `your other` / `friendly` / `enemies` …）—— 日志要打人话</summary>
        public string Head;

        /// <summary>
        /// **相邻型**（`Adjacent units have X`，9 张）—— 作用在**来源自己那一方**的左右紧邻格上。
        ///
        /// 出处：原版 `BattleManager.GetAdjacentUnits` 按**所属方**取那一方行内的左右格 ⇒ **不跨排**、
        /// **只作用于己方相邻格**（原版 `GetAdjacentUnits` 的调用点只传自己的棋盘）。
        /// ⚠️ 「谁算相邻」**只读** <see cref="BoardSpec.AdjacentSlots"/>（全仓唯一判据），别在这儿另写。
        /// ⚠️ 相邻型**天然不含自己** —— `AdjacentSlots` 本来就不含自己这一格
        /// （2026-09-14 用户拍板：`Makari the Grot` **不吃自己的光环**，与现状一致，不用改）。
        /// </summary>
        public bool Adjacent;

        /// <summary>作用在**敌方**（`Enemies have Vulnerable 1` / `Enemy troops have -2 Attack`）。
        /// 假 = 己方（`friendly` / `your` / `your other` / `other friendly`）。</summary>
        public bool Enemy;

        /// <summary>
        /// 卡面写了 **`other`** ⇒ **排除光环来源自己**（`Your other units …` / `Other friendly units …`）。
        ///
        /// ⚠️ 只有**写出来**才算 —— `Friendly Vehicles have …` 是不排除的（`Iron Priest` 不是 Vehicle，
        /// 实测也撞不上；但**别把「没写」猜成「排除」**，那是两回事）。
        /// ⚠️ 和 <see cref="Adjacent"/> 的区别：相邻型靠「相邻格不含自己」天然排除，
        /// `other` 是靠**筛选条件**排除 —— 结算层两处都要判。
        /// </summary>
        public bool ExcludeSelf;

        /// <summary>
        /// **筛选条件**（兵种 / 关键词 / 卡名）—— 直接复用 <see cref="CardCriteria"/>，
        /// 那是全仓的「这类效果作用在哪种卡上」（部署时给 / 持续改费已经在用）。
        /// ⚠️ **别在光环里另写一套筛选**：`SubjectOf` 那条「先是兵种词、不是就按卡名」的判据已经有一份。
        /// </summary>
        public CardCriteria Filter;

        /// <summary>
        /// 载荷原文（`Armour 1` / `+2 Ranged Attack` / `Flank` / `Invulnerable during your turn`）。
        /// **解释交给 <see cref="GivePayload.Parse"/>**（和 `give`/`gain` 同一张词表），
        /// 本文件**只做完整性检查**，不另解释一遍。
        /// </summary>
        public string Payload;

        /// <summary>
        /// 载荷里的**时长限定**：`during your turn` ⇒ <c>"duringyourturn"</c>；空 = 不限。
        ///
        /// 实测 1 处：`Friendly units with Pack have Invulnerable during your turn`（`Fyrri Askar`）。
        /// 🔴 **它不是废话** —— 飞行/无敌只在自己回合有效，剥掉就变成「永久」，是**静默放宽**。
        /// ⚠️ 和 <see cref="EffectOp.Duration"/> 的三个取值（`""` / `turn` / `nextturn`）**不是一套**：
        /// 那个说的是「这条效果到什么时候撤」，这个说的是「光环**什么时候亮**」。
        /// </summary>
        public string Duration;

        /// <summary>
        /// 卡面把**费用**和属性写在同一条光环里时，费用那半的值：
        /// `Other friendly Daemons **cost 2 less** and have +2 [attack]` ⇒ <c>2</c>（0 = 没有这半）。
        ///
        /// 🔴 实测**两张**（不是一张）：`Winged Daemon Prince`（-2）·
        /// `Beastboss on Squigosaur`（`Friendly Beasts cost 1 less and have Slay: …`，-1）。
        /// ⚠️ `资料/单位卡desc与光环_批次划分.md` §6.4 原来写「29 张里唯一一条费用+属性合体」——
        ///    **错**，那是照 §三 的表抄的，而那张表漏了 `Beastboss on Squigosaur`。
        ///    实据：`_tmp_view/aura_dryrun.txt` A) 栏（全池正则干跑）+ 逐卡核对**两条独立路径都收敛到 2 张**。
        ///
        /// ⚠️ 费用那半若已被「持续改费」层吃掉（`RuleCore.CostOf`），**属性那半别重复计**。
        /// </summary>
        public int CostLess;

        /// <summary>原句（日志与卡面用 —— 保留原文才好排查「到底写了什么」）</summary>
        public string Source;

        public override string ToString()
        {
            string who = Adjacent ? "相邻格" : (Enemy ? "敌方" : "己方");
            if (Filter != null && !Filter.IsEmpty) who += "·" + Filter;
            if (ExcludeSelf) who += "·排除自己";
            string pay = Payload;
            if (CostLess > 0) pay = $"费用 -{CostLess} 且 " + pay;
            return who + " 得到 " + pay;
        }
    }

    /// <summary>
    /// 光环句的**认句判据**（全仓只此一处）。
    ///
    /// 🔑 **锚定必须精确**：这一族以 `<锚点词> … have/has <载荷>` 为形，但它**跟两类句子长得极像**，
    /// 必须一条都不吃：
    ///   · **条件从句**：`If it has Flying, deal 6 damage instead` ·
    ///     `If the target has Armour, deal 8 damage instead`（全池 6 处）
    ///   · **时长限定的裸关键词**：`Has Flying during your turn`（督军 2 处，A7 第 5 步那一族）
    ///   ⇒ 所以判据是 **`^` 锚定 + 锚点词白名单**，不是「含 `have`」。
    ///   实测（`_tmp_view/aura_dryrun.txt` D 栏）：上面两类**一条都没被吃掉**。
    ///   ⚠️ 放宽锚点词前**先跑一遍那个干跑脚本**（`资料/单位卡desc与光环_批次划分.md` §六 有做法）。
    /// </summary>
    public static class Auras
    {
        /// <summary>锚点词白名单 —— **只有这 7 个**（实测全池 30 张只用到这 7 个）。</summary>
        const string Heads = @"adjacent|your other|other friendly|friendly|your|enemy|enemies";

        /// <summary>
        /// **费用 + 属性合体**那条路：`<锚点> <主语> cost(s) N less and have <载荷>`（2 张）。
        /// ⚠️ 必须**先**试它 —— 不然 `Friendly Beasts cost 1 less and have Slay: …` 的「主语」
        ///    会被当成 `Beasts cost 1 less and`（多出来的一截进了筛选条件，静默筛错）。
        /// </summary>
        static readonly Regex ReCostCombo = new Regex(
            @"^(?<head>" + Heads + @")\s+(?<subject>.+?)\s+costs?\s+(?<n>\d+)\s+(?:less|more)\s+and\s+have\s+(?<payload>.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 标准形：`<锚点> [主语] have|has <载荷>`。
        /// ⚠️ **主语可以空** —— `Enemies have Vulnerable 1`（没有名词）。
        ///    要求「至少一个词」的正则会把这一张**整张漏掉**（2026-09-14 干跑第一版就是这么漏的）。
        /// </summary>
        static readonly Regex ReAura = new Regex(
            @"^(?<head>" + Heads + @")\s*(?<subject>.*?)\s*(?:have|has)\s+(?<payload>.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>载荷里的**时长限定**（`Invulnerable **during your turn**`）—— 摘出来、从载荷里删掉。</summary>
        static readonly Regex ReDuringTurn = new Regex(
            @"\s*\bduring\s+your\s+turn\b\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// **形状**：这一句是不是 `&lt;锚点词&gt; … have|has …`（含「费用 + 属性合体」那条）。
        ///
        /// ⚠️ **只判形状、不判认不认得出** —— 这两件事在报表里要分开：
        /// 形状对、但载荷判不出来（卡面裸 `+N`）的句子**必须留在「不认识的句子」清单里**
        /// （宁可认不出，也别静默错一张）。判据与 <see cref="TryParse"/> **共用同一对正则**，
        /// 不另写一份。
        /// </summary>
        public static bool LooksLikeAura(string seg)
        {
            if (string.IsNullOrEmpty(seg)) return false;
            string s = seg.Trim();
            return ReCostCombo.IsMatch(s) || ReAura.IsMatch(s);
        }

        /// <summary>
        /// 认一句光环。**认得出才返回 true** —— 认不出就让调用方如实报（红线：不许静默失败）。
        ///
        /// **四道闸，缺一不可**（任何一道不过 ⇒ 返回 false，句子照旧留在「完全不认识」清单里）：
        ///   ① 形状锚定（见类注释）
        ///   ② 主语解得出来（<see cref="SubjectFilters"/>）
        ///   ③ **载荷完整解释得了**（<see cref="PayloadUnderstood"/>）
        ///   ④ **没有裸 `+N` 载荷**（<see cref="BareSignedAmbiguous"/>）
        /// </summary>
        public static bool TryParse(string seg, out AuraSpec spec)
        {
            spec = null;
            if (string.IsNullOrEmpty(seg)) return false;
            string s = seg.Trim();
            if (s.Length == 0) return false;

            int costLess = 0;
            Match m = ReCostCombo.Match(s);
            if (m.Success) costLess = int.Parse(m.Groups["n"].Value);
            else
            {
                m = ReAura.Match(s);
                if (!m.Success) return false;
            }

            string head = m.Groups["head"].Value.ToLowerInvariant();
            string subject = m.Groups["subject"].Value.Trim();
            string payload = m.Groups["payload"].Value.Trim().TrimEnd('.').Trim();
            if (payload.Length == 0) return false;

            var a = new AuraSpec
            {
                Head = head,
                Source = s,
                Adjacent = head == "adjacent",
                Enemy = head == "enemy" || head == "enemies",
                ExcludeSelf = head.Contains("other"),
                CostLess = costLess,
            };

            // ---- 时长限定先摘掉（`Invulnerable during your turn`）----
            var md = ReDuringTurn.Match(payload);
            if (md.Success)
            {
                a.Duration = "duringyourturn";
                payload = payload.Substring(0, md.Index).Trim();
                if (payload.Length == 0) return false;
            }

            // ---- ④ 裸 `+N`：**文本层判不出是近战还是远程** ⇒ 整条不认 ----
            if (BareSignedAmbiguous(payload)) return false;

            // ---- ③ 载荷必须**整条**解释得了（`GivePayload` 对「部分认了」是返回非 null 的）----
            if (!PayloadUnderstood(payload)) return false;

            // ---- ② 筛选条件 ----
            a.Filter = SubjectFilters(subject, out bool subjectOk);
            if (!subjectOk) return false;

            a.Payload = payload;
            spec = a;
            return true;
        }

        /// <summary>
        /// **裸 `+N` 一律不认**（2026-09-14 实测，两张卡的卡面亲读）。
        ///
        /// 数据管线的方括号图标被剥掉之后，`+1` 后面**本来是什么图标无从得知**，而实测两张
        /// **恰好不是同一个属性**：
        ///   · `Genestealer Familiar`（`Genestealer Cult/3部队/Warpforge_07_…png`）→ 粉圈**拳** = **近战**
        ///   · `Cadre Fireblade`（`Tau/3部队/Warpforge_35_…png`）→ 紫圈**枪** = **远程**
        /// 两张的 `desc` 与 `descZh` 都是光秃秃的 `+2` / `+2`，**中文也没有救**。
        ///
        /// 🔴 **兜底成近战 = 静默错一张**（`GivePayload` 的裸 `+N` 支就是按近战补的，它有它的出处：
        ///    那 12 张卡面确实是拳头）。所以这一族**宁可认不出**：
        ///    这两张会照旧挂在「完全不认识」清单里，卡面照旧如实，等人把卡面属性补进
        ///    `cardface_fixes.json` 的 `_manual_*` 列（那是数据侧的活，**不是解析层能猜的**）。
        /// </summary>
        static bool BareSignedAmbiguous(string payload)
        {
            return Regex.IsMatch(payload.Trim(), @"^[+-]\d+$");
        }

        /// <summary>
        /// 载荷是不是**整条**都解释得了。
        ///
        /// 🔴 **为什么要逐段查**：`GivePayload.Parse` 走的是「任一段认出来就返回非 null」
        ///   （`ParseInto` 的 `any`）——`+1 [attack] and +1 [weapon]` 在 `weapon` 没进词表时
        ///   会**静静返回半条**（只有近战那半）。半条也算「认了」⇒ 玩家拿到半份加成、日志看不出错。
        ///   这正是本工程的红线。所以这里按**同一套拆分规则**（先 `, ` 再 ` and `）逐段验。
        /// </summary>
        static bool PayloadUnderstood(string payload)
        {
            var parts = SplitPayload(payload);
            if (parts.Count == 0) return false;
            foreach (string part in parts)
                if (GivePayload.Parse(part) == null) return false;
            return true;
        }

        /// <summary>拆分规则**照抄** `GivePayload.ParseInto`（`:198`）：先按 `, ` 再按 ` and `。</summary>
        static List<string> SplitPayload(string payload)
        {
            var list = new List<string>();
            foreach (string s1 in payload.Split(new[] { ", " }, System.StringSplitOptions.None))
                foreach (string s2 in s1.Split(new[] { " and " }, System.StringSplitOptions.None))
                {
                    string t = s2.Trim();
                    if (t.Length > 0) list.Add(t);
                }
            return list;
        }

        /// <summary>
        /// 主语 → <see cref="CardCriteria"/>。`subjectOk = false` 表示**有认不出的词**（整条光环不认）。
        ///
        /// 逐词过三张表，**顺序就是判据**：
        ///   ① **兵种词** → `KindWord` / `KindAnyOf`（判据只此一份：<see cref="CreatePool.IsKindWord"/>）
        ///   ② **关键词** → `Keyword`（`Flying` / `Destroyer` / `Pack` / `Tide` —— 判据只此一份：
        ///      <see cref="KeywordTable.Normalize"/>）
        ///   ③ 都不是 → **卡名**（`Friendly Daemonette have Flank` 的 `Daemonette`：
        ///      实测它是**一张卡的卡名**（`EC7 Daemonette`，subtype 是 `Daemon`），
        ///      卡面拿单数专名当集合用（主谓还不一致）。这是第 7 个筛选维度，不是兵种词。
        ///      判据**转调** <see cref="EffectText.SubjectOf"/>（它的注释里记着同一个坑的另一个例子：
        ///      `Deffkopta` / `Mega Blasta Deffkopta` 撞名）。）
        ///
        /// 🔴 **两条与 `EffectText.ParseTarget` 不一样的地方**（照它走会静默筛错，所以这里自己判）：
        ///   · **多兵种词**：`Infantry and Drones` / `Infantry and Battlesuit troops`
        ///     —— `ParseTarget` 只留**第一个**（`FirstKindWord` 返回单个词）。
        ///     卡面那个 `and` 是**并集** ⇒ 其余进 <see cref="CardCriteria.KindAnyOf"/>。
        ///   · **`unit` / `units` 一律不筛**：🔴 `CreatePool.KindWords` 表里 `unit` 与 `troop`
        ///     两行**都映射到 `type == "unit"`**（那张表是**卡池**筛选用，卡池里本来没有督军）。
        ///     但光环是在**棋盘**上筛的，那里**有督军**（`type == "hero"`）⇒ 拿 `unit` 当筛选词
        ///     会把督军**静静筛掉**。规则书 `:70-75`：作用于「**单位**」的效果**可以**影响督军。
        ///     ⇒ `unit`/`units` 当噪声词丢掉（= 不筛）；**只有 `troop`/`troops` 是真筛选**
        ///     （它把督军排掉，正是卡面 `Adjacent **troops** have Vanguard` 的意思）。
        ///     ⚠️ 这个坑本工程**踩过两次**（14 张卡静默少算一个督军），别再踩。
        /// </summary>
        static CardCriteria SubjectFilters(string subject, out bool subjectOk)
        {
            subjectOk = true;
            var c = new CardCriteria();
            if (subject.Length == 0) return c;         // `Enemies have Vulnerable 1` —— 不筛

            // 先摘 `with <关键词>`（`units with Pack` / `troops with Flying` / `troops with Tide`）
            var mw = Regex.Match(subject, @"\bwith\s+([A-Za-z][A-Za-z' ]*)$", RegexOptions.IgnoreCase);
            if (mw.Success) subject = subject.Substring(0, mw.Index).Trim();

            var kinds = new List<string>();
            // `and` 分开的是**并集**（`Infantry and Drones`）；同一段里并列的是**交集**
            // （`Infantry troops` = 步兵部队 ⇒ 只留 `Infantry`，`troops` 那半只贡献「排督军」）。
            foreach (string part in Regex.Split(subject, @"\s+and\s+"))
            {
                var inSeg = new List<string>();
                foreach (string w in part.Split(' '))
                {
                    string t = w.Trim().Trim(',', '.').ToLowerInvariant();
                    if (t.Length == 0 || Ignorable(t)) continue;
                    if (CreatePool.IsKindWord(t)) { inSeg.Add(t); continue; }
                    string kw = KeywordTable.Normalize(t);
                    if (!string.IsNullOrEmpty(kw)) { c.Keyword = kw; continue; }
                    // ③ 卡名 —— `SubjectOf` 是唯一判据（它内部也是「先兵种词、否则卡名」）
                    var byName = EffectText.SubjectOf(t);
                    if (byName == null || string.IsNullOrEmpty(byName.Name)) { subjectOk = false; return null; }
                    c.Name = byName.Name;
                }
                // 同一段里有**具体兵种**时，泛称 `troops` 只是那个名词头，不另算一个维度
                string concrete = null;
                foreach (string k in inSeg) if (k != "troop" && k != "troops") { concrete = k; break; }
                if (concrete != null) { if (inSeg.Contains(concrete)) kinds.Add(concrete); }
                else if (inSeg.Count > 0) kinds.Add(inSeg[0]);
            }

            if (mw.Success)
            {
                string kw = KeywordTable.Normalize(mw.Groups[1].Value.Trim().ToLowerInvariant());
                if (string.IsNullOrEmpty(kw)) { subjectOk = false; return null; }
                c.Keyword = kw;
            }

            if (kinds.Count > 0)
            {
                c.KindWord = kinds[0];
                if (kinds.Count > 1)
                {
                    c.KindAnyOf = new List<string>();
                    for (int i = 1; i < kinds.Count; i++)
                        if (!c.KindAnyOf.Contains(kinds[i])) c.KindAnyOf.Add(kinds[i]);
                }
            }
            return c;
        }

        /// <summary>
        /// 主语里**不算筛选维度**的词。
        /// ⚠️ **`troop` / `troops` 不在这里** —— 它们是这一段里**唯一**能把督军排掉的判据
        ///    （见 <see cref="SubjectFilters"/> 的 🔴 那两条）。
        /// </summary>
        static bool Ignorable(string w)
        {
            switch (w)
            {
                case "the": case "a": case "an": case "all": case "other": case "your":
                case "friendly": case "enemy": case "enemies":
                case "unit": case "units":      // 🔴 不筛（含督军）—— 见 SubjectFilters 的说明
                case "card": case "cards":
                    return true;
            }
            return false;
        }

        /// <summary>这张卡身上有没有**认得出的光环句**（自检与报表用）。</summary>
        public static bool AnyIn(string text, out AuraSpec first)
        {
            first = null;
            if (string.IsNullOrEmpty(text)) return false;
            foreach (string seg in EffectText.Split(text))
            {
                AuraSpec a;
                if (!TryParse(seg, out a)) continue;
                if (first == null) first = a;
                return true;
            }
            return false;
        }
    }
}
