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

        /// <summary>
        /// **自指型**：卡面说的是**这张卡自己**（`Has Flying during your turn` / `Flying during your turn`）。
        ///
        /// 🆕 2026-09-14 A7（「一回合到期」那一族并进来的）。实测**督军 3 处**（全在 hero 卡上）。
        /// 它和同族的光环**是同一件事**：一条**只在拥有者回合有效**的持续加成 ⇒
        /// 只是「筛出来的对象」退化成**只有自己**，所以用 <see cref="Duration"/> 那一套时长限定就够了。
        /// ⚠️ 写成 `X have Y` 的**主语是自己的**那种不存在 —— 卡面就是裸的 `Has Flying …`。
        /// </summary>
        public bool Self;

        /// <summary>
        /// **改残骸寿命**（`Adjacent Remnants do not disappear at the end of your turn`，
        /// 全池 **1 张**：`Nemesor Zahndrekh`，Necron 督军）。
        ///
        /// ⚠️ **它不是属性、也不是关键词** —— 载荷那一套（`GivePayload`）对它完全不适用
        /// （卡面连 `have` 都没有），所以单开一栏，由 `Recompose` 直接置
        /// <see cref="UnitState.AuraRemnantStay"/>，读点在 `RuleCore.DestroyRemnants`。
        /// ⚠️ 卡面写的是 `Adjacent **Remnants**`，但 `Remnants` **不是兵种词**（残骸是**状态**不是卡种）
        /// ⇒ 不需要筛选条件：那个标记**只在残骸身上被读**（`DestroyRemnants` 本来就只扫残骸）。
        /// </summary>
        public bool RemnantStay;

        public override string ToString()
        {
            if (RemnantStay) return "相邻残骸**不在回合结束时消失**";
            string who = Self ? "自己" : (Adjacent ? "相邻格" : (Enemy ? "敌方" : "己方"));
            if (!Self && Filter != null && !Filter.IsEmpty) who += "·" + Filter;
            if (ExcludeSelf) who += "·排除自己";
            string pay = Payload;
            if (CostLess > 0) pay = $"费用 -{CostLess} 且 " + pay;
            if (Duration == "duringyourturn") pay += "（只在拥有者回合）";
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
    ///   ⚠️ 放宽锚点词前**先跑 `Unity/工具/aura_dryrun.py`**（把这几条正则拿全卡池跑一遍，
    ///      看有没有从句子中间被吃掉的；⚠️ 它和本文件的正则是**两份**，改这边要同步改它）。
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
        /// **自指型**：`Has Flying during your turn` / `Flying during your turn`（督军 3 处）。
        /// 语义 = 「**这张卡自己**在拥有者回合里有 Flying」—— 见 <see cref="AuraSpec.Self"/>。
        /// ⚠️ **必须锚定 `^` 且吃满整句**：`Flying` 是个常见词，松一点会吃到正文里的飞行从句。
        /// </summary>
        static readonly Regex ReSelfTurn = new Regex(
            @"^(?:has\s+)?flying\s+during\s+your\s+turn$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// **改残骸寿命**：`Adjacent Remnants do not disappear at the end of your turn`（1 张）。
        /// 见 <see cref="AuraSpec.RemnantStay"/>。
        /// </summary>
        static readonly Regex ReRemnantStay = new Regex(
            @"^adjacent\s+remnants?\s+do\s+not\s+disappear\s+at\s+the\s+end\s+of\s+your\s+turn$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
            return ReCostCombo.IsMatch(s) || ReAura.IsMatch(s)
                || ReRemnantStay.IsMatch(s.TrimEnd('.').Trim()) || ReSelfTurn.IsMatch(s.TrimEnd('.').Trim());
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
            string s = seg.Trim().TrimEnd('.').Trim();
            if (s.Length == 0) return false;

            // ---- 两种**特殊形状**先认（它们连 `have` 都没有，走不到下面的正则）----
            if (ReRemnantStay.IsMatch(s))
            {
                spec = new AuraSpec
                {
                    Head = "adjacent", Adjacent = true, RemnantStay = true,
                    Filter = new CardCriteria(), Source = s,
                };
                return true;
            }
            if (ReSelfTurn.IsMatch(s))
            {
                spec = new AuraSpec
                {
                    Head = "self", Self = true, Duration = "duringyourturn",
                    Payload = "Flying", Filter = new CardCriteria(), Source = s,
                };
                return true;
            }

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
        ///    这两张原来是「照旧挂在『完全不认识』清单里、卡面照旧如实」，
        ///    2026-09-14 **已从数据侧修掉**：`cardface_fixes.json` 的 **`desc`** 列给
        ///    `Genestealer Familiar` 补成 `+1 Attack`、给 `Cadre Fireblade` 补成 `+2 Ranged Attack`
        ///    （两张卡图逐张亲读：前者粉圈白拳、后者紫圈枪）⇒ 池里**没有**裸 `+N` 的光环句了。
        ///    ⚠️ **这道闸仍然留着**：防的是**将来**再出现裸 `+N`。真遇到了 **修法同上**
        ///    （补 `desc` 的**规范写法**），**不是**在这里放宽词表 —— 那是猜。
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

        // ==================================================================
        //  结算：**整份摘掉再重加**（A7 第 3 步）
        // ==================================================================

        /// <summary>
        /// 光环加成的**来源标记**。三处共用这一个串（**判据只此一处**）：
        ///   · 属性增减益 → <see cref="UnitState.RecordGrant"/> 的 `source`（靠 `RevertGrantsFrom` 整份收）
        ///   · 费用那半   → <see cref="CostMod.Tag"/>（重算时按它撤）
        ///   · 关键词那本账 → <see cref="UnitState.ClearAuraGrants"/> 自己管（不走这个串）
        /// </summary>
        public const string GrantTag = "光环";

        /// <summary>
        /// **重算全场光环** —— 幂等：先**整份摘掉**上一次算出来的，再按当前棋盘**重加一遍**。
        ///
        /// 🔴 **形状照原版**：`CardScript__UpdateWhileInPlay.c` 就是「摘掉此前加的全部 + 重加一份」
        /// （判据 `+0x3c == 0x262`，见设计稿 §8.2）。**不新开读点**（关键词全仓 100 处读点，覆盖不全）。
        ///
        /// 🔴 **但缓存键不照抄**：原版只比「己方单位数」，**同数换位不会刷新**
        ///    （`UpdateWhileInPlay` 拿缓存 `+0x358` 一比就 return）—— 那是它的**缺陷**。
        ///    我们**每次棋盘变动都整份重算**（棋盘最大 9 格 × 2 方，成本可以忽略），
        ///    所以「换个位置站」也会正确刷新。⚠️ 要不要跟原版这条**得跑实况定**（铁律 4），
        ///    设计稿 §8.9 记着这个悬案 —— **在跑到实况之前，按「修得比原版对」做**，
        ///    因为原版那条是**可证的实现缺陷**（同数换位时光环挂在错的人身上）。
        ///
        /// **调用点**（挂在这些地方，因为它们是棋盘**唯一**会变动的几处）。
        /// 🔴 **2026-09-19 按代码重数过一遍**（原先这一行列了 7 个方法名、与代码对不上）：
        /// **12 处棋盘写入点**各有一次，外加 3 处兜底重算。逐处对账表（含判定与找法）在
        /// `资料/普查产出_0919/光环写入点_钩子对账.md`：
        ///   · `RuleCore`：`PlayCard:873`（部署）· `DeployFree:1150` · `TrySwarmMerge:1101` ·
        ///     `CleanupDeaths:2147`（死亡/翻面成残骸）**+ `CleanupDeaths:2035` 那一支
        ///     残骸被摧毁（2026-09-19 补：它 `return` 得早，原来把这次漏掉了）**·
        ///     `EndTurn:610`（归还抢来的单位）· `UseAlternative:2654`（狂暴洗回牌库）·
        ///     `BeginTurn:512`（`during your turn` 那一族要跟着回合亮/灭）
        ///   · `EffectResolver`：`DoReturn:2939`（回手）· `DoReanimate:5826`（残骸翻回来）·
        ///     `DoTakeControl:3547`（抢控制权）· `ResolveSpiritAbility:3354` · `ResolveOathAbility:3435`
        ///   ⚠️ **棋盘是裸数组**（`PlayerState.Board`，`readonly` 字段、没有 setter、没有索引器）
        ///   ⇒ **不存在「写入即触发」这种钩子**，这正是漏了不报错的原因
        ///   ⇒ **新增任何往 `Board[..]` 写的地方，都要顺手调一次这里**。
        ///   自检里有一条断言专门盯这件事（部署 → 加成在 · 来源离场 → 收回）。
        ///   ⚠️ **走了 `return` 的分支要单独查**：`CleanupDeaths` 那个洞就是「早退跳过了末尾那次兜底」。
        /// </summary>
        public static void Recompose(BattleContext ctx)
        {
            if (ctx == null || ctx.Players[0] == null || ctx.Players[1] == null) return;

            // ---- ① 整份摘掉 ----
            for (int p = 0; p < 2; p++)
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = ctx.Players[p].Board[s];
                    if (u == null) continue;
                    u.ClearAuraGrants();          // 关键词（只减光环那一份）+ 属性（RevertGrantsFrom）
                }
            for (int i = ctx.CostMods.Count - 1; i >= 0; i--)
                if (ctx.CostMods[i].Tag == GrantTag) ctx.CostMods.RemoveAt(i);

            // ---- ② 费用那半（`Other friendly Daemons cost 2 less`，实测 2 张）----
            //
            // ⚠️ **一次算出「有没有符合条件的牌」是做不到的**（手牌一直在变），所以照
            //    `CostIfControls` 那条路：**登记一条筛选条件，由 `RuleCore.CostOf` 每次现算**。
            // ⚠️ 登记成 `Tag = GrantTag`，重算时整份撤 —— 来源离场就**立刻**失效
            //    （不用 `ExpireTurn`，那个是按回合数过期的）。
            // ⚠️ 这一段**不参与下面的迭代**：它筛的是**手牌里的卡**，与光环之间没有依赖。
            for (int p = 0; p < 2; p++)
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var src = ctx.Players[p].Board[s];
                    if (src == null || src.Card == null) continue;
                    var list = src.Card.AuraSpecs;
                    if (list == null) continue;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].CostLess <= 0) continue;
                        if (list[i].Duration == "duringyourturn" && ctx.Active != p) continue;
                        ctx.CostMods.Add(new CostMod
                        {
                            Player = p,
                            Key = "*",
                            Delta = -list[i].CostLess,
                            Criteria = list[i].Filter,
                            Tag = GrantTag,
                        });
                    }
                }

            // ---- ③ 按当前棋盘重加（**迭代到不再新增**）----
            //
            // 🔴 **为什么要迭代**（2026-09-14 实测抓出来的）：**光环之间会互相引用** ——
            //    `Wolf Guard Battle Leader` 给相邻单位发 `Pack`，而 `Fyrri Askar` 的光环筛的
            //    正是「带 `Pack` 的友方单位」。只扫一遍的话，**结果取决于扫描顺序**
            //    （格号小的先扫：Fyrri 在 0 号格 ⇒ 轮到它时邻居身上还没有 `Pack` ⇒ 整条静默不生效）。
            //    ⚠️ **光「多扫几遍」不行**：每遍开头都清空 ⇒ 每遍的中间状态完全一样，永远停在同一个结果。
            //    ⇒ 正确形状是「**清空一次，然后每轮只补没挂过的**」，直到某一轮一条都没补。
            //    三元组 (来源格, 光环序号, 目标格) 就是「补过没有」的键。
            // ⚠️ 上限 4 轮：正常最多两轮（一层依赖）；到上限还没停说明**光环成环了**（A 要 B、B 要 A），
            //    那时按最后一轮的结果停下并**如实打一行日志**，不静默。
            var done = new HashSet<long>();
            for (int pass = 0; pass < 4; pass++)
            {
                bool added = false;
                for (int p = 0; p < 2; p++)
                {
                    // `during your turn` 那一族（`Fyrri Askar` 的 `Invulnerable during your turn`）
                    // **只在自己的回合亮** —— 所以重算钩子必须挂在回合开始处。
                    bool myTurn = ctx.Active == p;
                    var board = ctx.Players[p].Board;
                    for (int s = 0; s < BoardSpec.Size; s++)
                    {
                        var src = board[s];
                        if (src == null || src.Card == null) continue;
                        var auras = src.Card.AuraSpecs;
                        if (auras == null || auras.Count == 0) continue;
                        for (int i = 0; i < auras.Count; i++)
                        {
                            var a = auras[i];
                            if (a.Duration == "duringyourturn" && !myTurn) continue;
                            if (ApplyAura(ctx, a, src, p, s, i, done)) added = true;
                        }
                    }
                }
                if (!added) return;
            }
            ctx.Log("光环：重算 4 轮还有新增 —— 疑似**光环互相引用成环**，已按最后一轮的结果停下");
        }

        /// <summary>「这一条（来源 + 目标）挂过没有」的键 —— 见 <see cref="Recompose"/> 的迭代说明。</summary>
        static long PairKey(int srcOwner, int srcSlot, int auraIdx, int tgtOwner, int tgtSlot)
        {
            return (((long)srcOwner * 9 + srcSlot) * 8 + auraIdx) * 18 + (tgtOwner * 9 + tgtSlot);
        }

        /// <summary>
        /// 把一条光环施加到它筛选出来的每一个单位上。**返回这一轮有没有新挂上东西**
        /// （迭代的终止条件，见 <see cref="Recompose"/>）。
        /// </summary>
        static bool ApplyAura(BattleContext ctx, AuraSpec a, UnitState src, int srcOwner, int srcSlot,
                              int auraIdx, HashSet<long> done)
        {
            bool added = false;
            int hits = 0;

            // ---- 特殊形状：**改残骸寿命**（`Nemeser Zahndrekh`）—— 没有载荷，只置一个标记 ----
            if (a.RemnantStay)
            {
                ForEachTarget(ctx, a, srcOwner, srcSlot, delegate (int tgtOwner, int tgtSlot, UnitState t)
                {
                    if (!t.IsRemnant) return;      // 那个标记只在残骸身上被读，非残骸不标记（省得日志骗人）
                    hits++;
                    if (!done.Add(PairKey(srcOwner, srcSlot, auraIdx, tgtOwner, tgtSlot))) return;
                    t.AuraRemnantStay = true;
                    added = true;
                });
                if (added)
                    ctx.Log($"光环：{src.Name} 的「{a.Source}」→ {a}（罩住 {hits} 个残骸）");
                return added;
            }

            var ops = GivePayload.Parse(a.Payload);
            if (ops == null || ops.Count == 0)
            {
                // `TryParse` 已经验过载荷解释得了，走到这儿说明两边不一致 —— **如实报**，不静默
                ctx.Log($"光环：`{a.Source}`（{src.Name}）的载荷「{a.Payload}」"
                      + "这时候解释不出来 —— **这条没生效**");
                return false;
            }

            ForEachTarget(ctx, a, srcOwner, srcSlot, delegate (int tgtOwner, int tgtSlot, UnitState t)
            {
                if (!a.Filter.Matches(t)) return;
                hits++;
                if (!done.Add(PairKey(srcOwner, srcSlot, auraIdx, tgtOwner, tgtSlot))) return;
                added = true;
                foreach (var op in ops)
                {
                    if (op.IsEmbedded) { GrantEmbeddedAura(ctx, t, op.Embedded, src); continue; }
                    // ⚠️ 属性词**必须过 `RuleCore.NormalizeAttr`**（`melee` → `attack`）——
                    //    不过的话 `+1 Melee` 会**静静丢掉**（`GivePayload` 认得那个词、
                    //    但 `UnitState.ApplyGrant` 的 switch 里没有 `melee`）。
                    //    实测踩到：`Company Ancient` 的 `+1 Melee and +1 Ranged Attack` 一开始只加了远程。
                    if (op.Attr != null)
                    {
                        t.RecordGrant(RuleCore.NormalizeAttr(op.Attr), op.Value, GrantTag);
                        continue;
                    }
                    if (op.Keyword != null)
                        t.AddAuraKeyword(op.Keyword, op.Value <= 0 ? 1 : op.Value);
                }
            });

            // 只在**第一次真的挂上人**时打一行（迭代会重跑，别刷屏）
            if (added)
                ctx.Log($"光环：{src.Name} 的「{a.Source}」→ {a}（命中 {hits} 个单位）");
            return added;
        }

        /// <summary>
        /// 一条光环**作用在哪些格子上**。
        ///
        /// 🔴 **相邻型只作用于「来源那一方」的左右紧邻格** —— 原版
        /// `BattleManager.GetAdjacentUnits` 按**所属方**取行内左右格 ⇒ **不跨排**、不跨方。
        /// 「谁算相邻」**只读** <see cref="BoardSpec.AdjacentSlots"/>（全仓唯一判据）。
        /// ⚠️ 相邻型**天然不含自己**（`AdjacentSlots` 不含本格）—— 用户 2026-09-14 拍板
        ///    「`Makari the Grot` 不吃自己的光环」，与现状一致。
        /// </summary>
        static void ForEachTarget(BattleContext ctx, AuraSpec a, int srcOwner, int srcSlot,
                                  System.Action<int, int, UnitState> fn)
        {
            // **自指型**（`Has Flying during your turn`）—— 对象就是来源自己
            if (a.Self)
            {
                var me = ctx.Players[srcOwner].Board[srcSlot];
                if (me != null) fn(srcOwner, srcSlot, me);
                return;
            }

            if (a.Adjacent)
            {
                var slots = new List<int>();
                BoardSpec.AdjacentSlots(srcSlot, slots);
                var own = ctx.Players[srcOwner].Board;
                foreach (int s in slots)
                {
                    var t = own[s];
                    if (t != null) fn(srcOwner, s, t);
                }
                return;
            }

            int owner = a.Enemy ? 1 - srcOwner : srcOwner;
            var board = ctx.Players[owner].Board;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                if (!a.Enemy && a.ExcludeSelf && s == srcSlot) continue;
                var t = board[s];
                if (t != null) fn(owner, s, t);
            }
        }

        /// <summary>
        /// 光环载荷里**带正文的关键词**（`Slay: Gain Blood Thirst this turn`，全池 1 张：
        /// `Beastboss on Squigosaur` 给友方野兽挂 `Slay`）。
        ///
        /// **判据转调** `EffectResolver.cs` 里的 `RuleCore.GrantEmbeddedAbilityFromAura`（**只此一处**）——
        /// 它管着「拆关键词 / 词表归一 / 解析正文 / 挂上」那几步，以及
        /// 「解析不了就**整条不挂**并如实报」。这里只负责把**光环来源**传下去。
        /// ⚠️ 那个文件的名字叫 `EffectResolver.cs`，但**里面的类其实是 `RuleCore`**
        ///    （`public static partial class RuleCore`）—— 照文件名写会编译不过。
        /// </summary>
        static void GrantEmbeddedAura(BattleContext ctx, UnitState t, string embedded, UnitState src)
        {
            RuleCore.GrantEmbeddedAbilityFromAura(ctx, t, embedded, src);
        }
    }
}
