// CardDef.cs — 静态卡牌定义
//
// 数据来源：`d:/4/Unity/数据/游戏数据/card_stats.json`（1212 张，OCR + 数值合并的产物）
// 语义来源：`d:/warpforge/scripts/rule_core.gd` 的 `_parse_keywords` / `KW_MATCH`
//
// ⚠️ 本文件属于 `Core/` —— **不允许依赖 UnityEngine**（见 资料/规则引擎_设计.md 第二节）。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>
    /// 卡牌定义（不可变）。表现层通过 <see cref="ICardProvider"/> 消费它。
    ///
    /// 除了关键词，卡上还能带**效果**：`"Rally: Damage 1 EnemyWarlord"` 这种
    /// `关键词: 效果文字`。效果文字走 <see cref="EffectSpec"/> 那个封闭文法，
    /// 解析不出来的会被记进 <see cref="UnparsedEffects"/> —— 卡面上标 `*`，不装作能跑。
    /// </summary>
    public class CardDef : ICardProvider
    {
        // 用只读自动属性而不是字段 —— 接口成员是属性，字段实现不了（会报 CS0535）
        public string Id { get; }
        public string Name { get; }
        public string Type { get; }        // unit / tactic / hero / defence
        public string Desc { get; }
        public string Rarity { get; }
        public string Faction { get; }

        /// <summary>中文卡名。**空串 = 这张卡没有翻译** —— 卡面回英文名（不静默）。
        /// 只有原版那 1131 张才有值（来自 `数据/卡牌翻译/zh_cards.json`）；
        /// 我们自己设计的 26 张卡走 `CardText` 那张表，这里留空。</summary>
        public string NameZh { get; }

        /// <summary>中文效果文字。空串 = 没有（原因同 <see cref="NameZh"/>）。</summary>
        public string DescZh { get; }

        /// <summary>
        /// **兵种**（`Infantry` / `Vehicle` / `Drone` / `Beast` / `Daemon` / `Elixir` / `Secret` …）。
        ///
        /// 原版数据（`card_stats.json` 的 `subtype`）里**一直都有**，我们 2026-09-12 才发现自己在
        /// 生成卡表时把它丢了。补回来的用处有两个，都是硬需求：
        ///   ① **目标过滤** —— 卡面写 `a friendly Vehicle` 时能真正筛出载具。
        ///      此前只能按整个目标池打，12 张卡挂在「打得比卡面宽」那一栏里当已知缺陷
        ///      （见 `EffectTargetSpec.KindUnfilterable`）。
        ///   ② **造牌候选池** —— `Create three Ultramarines Vehicles`、
        ///      `Create a random Combat Elixir` 全靠它筛。规则书附录 C 的骰子查找表就是按兵种分组的。
        ///
        /// 空串 = 原版数据里就没有（1212 张里剩 95 张，多半是 token / 未实装卡）。
        /// **不许猜**：空串一律按「不过滤」处理，并如实报出来。
        /// </summary>
        public string Subtype { get; }

        /// <summary>这张卡是不是来自**原版卡池**（`cards_engine.json`，由
        /// <see cref="CardDatabase.Parse"/> 置 true）。我们自己设计的那 26 张是 false。
        /// <para>用处：卡面文字怎么取。原版卡的卡面写的是**它自己的效果原文**
        /// （`卡面.png` 上就是那段字，见 `Desc`），而我们自设计的卡卡面写的是
        /// **引擎结算得到的关键词**（`Desc` 对我们那 26 张是风味文字，不是效果）。</para></summary>
        public bool FromOriginalPool { get; }
        public int Cost { get; }
        public int Attack { get; }
        public int Health { get; }
        /// <summary>卡面左下紫圆（子弹图标）。注意：JSON 里的 `armor` 字段经核实实为远程攻击，已迁移到这个名字下</summary>
        public int RangedAttack { get; }

        readonly Dictionary<string, int> _keywords;
        readonly Dictionary<string, EffectSpec> _effects = new Dictionary<string, EffectSpec>();
        readonly List<string> _unparsed = new List<string>();

        /// <summary>主动技能（`Ability:` 那一条）。没有就是 null</summary>
        public EffectSpec Ability { get; private set; }

        public CardDef(string id, string name, string type, string desc, string rarity, string faction,
                       int cost, int attack, int health, int rangedAttack,
                       IEnumerable<string> keywords,
                       string nameZh = null, string descZh = null, bool fromOriginalPool = false,
                       string subtype = null)
        {
            Id = id; Name = name; Type = type ?? ""; Desc = desc ?? ""; Rarity = rarity; Faction = faction;
            NameZh = nameZh ?? ""; DescZh = descZh ?? "";
            Subtype = subtype ?? "";
            FromOriginalPool = fromOriginalPool;
            Cost = cost; Attack = attack; Health = health; RangedAttack = rangedAttack;
            _keywords = KeywordTable.Parse(keywords);

            // 效果文字在 ':' 后面。**关键词和效果分开解析**：
            //   `KeywordTable` 只管「这行是哪个关键词、值几」（照抄 rule_core 的语义，别动）
            //   `EffectSpec`   只管「':' 后面那句话是什么效果」（本工程自己定的封闭文法）
            if (keywords != null)
            {
                foreach (var item in keywords)
                {
                    string name_ = KeywordTable.Normalize(item);
                    if (name_ == null) continue;

                    string text = KeywordTable.EffectText(item);
                    if (text == null) continue;               // 这个关键词不带效果文字，正常

                    var spec = EffectSpec.Parse(text);
                    if (spec == null)
                    {
                        _unparsed.Add(name_ + ": " + text);
                        continue;
                    }

                    if (name_ == KeywordTable.Ability)
                    {
                        if (Ability == null) Ability = spec;
                        else _unparsed.Add(name_ + ": " + text);   // 一张卡只能有一个主动技能
                    }
                    else
                    {
                        _effects[name_] = spec;
                    }
                }
            }

            CollectTriggerOps(keywords);
        }

        // ==================================================================
        //  触发式效果的**正文**（`Rally:` / `Strike:` / … 后面那段）
        // ==================================================================

        /// <summary>
        /// **本版有触发时机**的那几个关键词 —— 只有它们才值得收正文。
        ///
        /// 别的触发词（`Talent` 83 条 · `Pray` 22 · `Duty` 17 · `Agenda` 8 · `Artifice` 8 · `Mob` 9 …
        /// 实测 212 条）**引擎里根本没有那个时机**，收了也没人消费 ——
        /// 注册一条永远不会被消费的效果 = 骗玩家（本工程的静默失败红线）。
        /// ⇒ 它们**照旧判「不认识」**，卡面标 `*`。
        /// </summary>
        public static readonly string[] RoutableTriggers = {
            KeywordTable.Rally, KeywordTable.Strike, KeywordTable.Slay,
            KeywordTable.Backlash, KeywordTable.Penitence };

        /// <summary>
        /// 触发关键词 → 正文解析出来的 op。**没有就是 null**（调用方要判）。
        ///
        /// 🔴 **为什么要有这个东西**（2026-09-13 第三十一轮）：原来触发式效果**只**走
        /// <see cref="EffectSpec"/> 那个**封闭文法**（只有 Damage / Heal / Draw），
        /// 而原版卡面写的是 `Rally: Stun an enemy` / `Strike: Return this troop to your hand`
        /// —— 于是**89 张卡**「`keywords` 里登记了触发、正文却解析不出来」：
        /// 卡面标 `*`、打起来**静默不动**。
        ///
        /// 现在正文交给 <see cref="EffectText"/>（**战术卡用的同一个解析器**）——
        /// 卡面本来就是同一套语法，没有理由分两条路。
        /// 两个来源都收：① `Desc`（卡面原文，**优先**）② `keywords` 里带触发前缀的条目
        /// （有 35 张卡只在 `keywords` 里写了正文，`Desc` 里没有）。
        /// </summary>
        readonly Dictionary<string, List<EffectOp>> _triggerOps =
            new Dictionary<string, List<EffectOp>>();

        /// <summary>这个触发关键词的正文 op。**没有返回 null**（不是空列表 —— 调用方要能区分）。</summary>
        public IReadOnlyList<EffectOp> TriggerOps(string keyword)
        {
            List<EffectOp> v;
            return (keyword != null && _triggerOps.TryGetValue(keyword, out v)) ? v : null;
        }

        /// <summary>触发关键词 → 正文原文（日志与卡面用）</summary>
        readonly Dictionary<string, string> _triggerText = new Dictionary<string, string>();
        public string TriggerText(string keyword)
        {
            string v;
            return (keyword != null && _triggerText.TryGetValue(keyword, out v)) ? v : null;
        }

        void CollectTriggerOps(IEnumerable<string> keywords)
        {
            // ① 卡面 `Desc` —— 权威来源，先收（同一条触发**先到先得**）
            foreach (string seg in EffectText.Split(Desc)) AddTriggerOp(seg);
            // ② `keywords` 里带触发前缀的条目（`Rally: …` 这种）
            if (keywords != null) foreach (string item in keywords) AddTriggerOp(item);
            // ③ **事件层**（`When <事件>, …`）—— 第三十二轮新增，见 <see cref="WhenTrigger"/>。
            //    ⚠️ 它和上面那条**不是同一族**：上面是「时机在代码里」，这一族是「时机在卡面文字里」。
            CollectWhenTriggers(keywords);
        }

        void AddTriggerOp(string seg)
        {
            if (string.IsNullOrEmpty(seg)) return;
            int c = seg.IndexOf(':');
            if (c <= 0) return;

            // ⚠️ 这里**只小写后直接比**，不借 `KeywordTable.Normalize` ——
            //    那个是**前缀匹配**，`Pray: …` 会被它当地图炮；而且我们要的是「整词就是触发名」。
            string head = seg.Substring(0, c).Trim().ToLowerInvariant();
            bool routable = false;
            foreach (string t in RoutableTriggers) if (t == head) { routable = true; break; }
            if (!routable) return;
            if (_triggerOps.ContainsKey(head)) return;      // 先到先得（① 在 ② 前面）

            string body = seg.Substring(c + 1).Trim();
            if (body.Length == 0) return;

            // 正文交给**战术卡那个解析器**；解析不出来就**不收**（保持「卡面标 *」的诚实）
            var ops = EffectText.Parse(body, out _, out _);
            if (ops == null || ops.Count == 0) return;

            _triggerOps[head] = ops;
            _triggerText[head] = body;
        }

        /// <summary>本卡**收到正文**的触发（自检与卡面用）</summary>
        public IReadOnlyDictionary<string, string> TriggerTexts { get { return _triggerText; } }

        // ==================================================================
        //  🆕 事件层：`When <事件>, <效果>`（2026-09-13 第三十二轮）
        // ==================================================================
        //
        // 和上面那一族的**根本区别**：
        //   · 上面（Rally/Strike/…）的「时机」是**代码里的固定调用点**（`FireTriggerAt`），
        //     卡面只是给那个时机配的正文 —— 所以要的只是一个存放位置。
        //   · 这一族的「时机」是**卡面文字本身**（`When <事件>`），
        //     引擎得先认得出那个事件短语（<see cref="WhenEvents.Parse"/>），
        //     再在**那件事真的发生时**判它符不符合（<see cref="WhenEvents.Matches"/>）。
        //
        // 两族**都挂在 `CardDef` 上**（不是挂在棋子上）：卡表是共享的、一局里同一张卡可以有多份，
        // 挂在 `CardDef` 上天然一致；棋子上那份状态由 `UnitState` 管，事件监听不需要它。
        // ⚠️ 代价：**同名同卡的两份**会各触发一次 —— 这是对的（场上就是有两个），
        //    但**换人要注意**：`foreach (var u in 场上)` 会把同一张卡的监听器跑两遍。
        //    所以下面用 <see cref="FireWhen"/> 而不是直接遍历 <see cref="_whenTriggers"/>。

        /// <summary>
        /// 一条「事件 → 效果」监听器：**什么事**（<see cref="Ev"/>）+ **发生时要结算什么**（<see cref="Ops"/>）。
        /// </summary>
        public class WhenTrigger
        {
            /// <summary>事件种类 + 筛选条件（见 <see cref="WhenEvents.Parse"/>）</summary>
            public WhenEvent Ev;
            /// <summary>事件发生时要结算的 op（**解析一次存下来**，和 `TriggerOps` 同一套）</summary>
            public List<EffectOp> Ops;
            /// <summary>正文原文（日志与卡面用 —— 卡面上那半句就是这么写的）</summary>
            public string Body;

            public override string ToString() { return Ev + " → " + Body; }
        }

        readonly List<WhenTrigger> _whenTriggers = new List<WhenTrigger>();

        /// <summary>本卡登记的**事件监听器**（可能为空表 —— 调用方别假设一定有）</summary>
        public IReadOnlyList<WhenTrigger> WhenTriggers { get { return _whenTriggers; } }

        /// <summary>
        /// **本卡在「手里」就要监听的事件** —— `Lower cost by 1 when <事件>` 那一族。
        ///
        /// 和 <see cref="WhenTriggers"/> 分开，是因为**挂的地方不一样**：
        /// 那一族挂在**场上那张牌**上（牌不在场就不该触发），
        /// 这一族挂在**玩家身上**、且**牌离手之后还留着** —— 原版 `PlayerHand.SetupCardInHand`
        /// （`PlayerHand__SetupCardInHand.c:60-73`）在**牌入手牌那一刻**把效果挂到那张牌上，
        /// 而费用修正一旦登记就跟着对局走（我们把修正挪到 `ctx.CostMods`，见 `EffectResolver.DoCostMore`）。
        /// </summary>
        public class CostWhen
        {
            public WhenEvent Ev;
            /// <summary>降多少（卡面 `Lower cost by N` 的 N）</summary>
            public int Delta;
            public string Body;
            public override string ToString() { return Ev + " → 费用 " + Delta; }
        }

        readonly List<CostWhen> _costWhens = new List<CostWhen>();
        public IReadOnlyList<CostWhen> CostWhens { get { return _costWhens; } }

        void CollectWhenTriggers(IEnumerable<string> keywords)
        {
            // ⚠️ **两个来源都扫**，理由和 `CollectTriggerOps` 一样：
            //    有 35 张卡把正文写在 `keywords` 数组里、`Desc` 里没有。
            //    两边都**按 `Split` 切分句**再送进 `AddWhenTrigger` ——
            //    `keywords` 的条目也可能是多句拼在一起的
            //    （实测 `Jain Zar`：`"… Talent: Storm of Silence"`），
            //    整条送进去会因为**最后一个逗号**在别处而切出错误的「事件短语」。
            foreach (string seg in EffectText.Split(Desc)) AddWhenTrigger(seg);
            if (keywords != null)
                foreach (string item in keywords)
                    foreach (string seg in EffectText.Split(item)) AddWhenTrigger(seg);
        }

        /// <summary>
        /// 一段卡面文字 → 事件监听器（`When …`）**和/或** 事件触发式降费（`… when …`）。
        ///
        /// ⚠️ 一段里**两种都可能有**（`Draw 3 cards. Lower cost by 1 when an enemy dies` 是两段，
        ///    但 `When X, Y` 只有一种）。所以这里不是 if/else，是**两次独立的尝试**。
        /// </summary>
        void AddWhenTrigger(string seg)
        {
            if (string.IsNullOrEmpty(seg)) return;
            string s = seg.Trim();

            // ---- ① `When <事件>, <正文>` ----
            if (s.StartsWith("When ", System.StringComparison.OrdinalIgnoreCase))
            {
                int comma = s.IndexOf(',');
                if (comma > 5)
                {
                    string evPhrase = s.Substring(5, comma - 5);
                    string body = s.Substring(comma + 1).Trim();
                    var ev = WhenEvents.Parse(evPhrase);
                    var ops = body.Length > 0 ? EffectText.Parse(body, out _, out _) : null;
                    // ⚠️ **两边都要成功才收**：事件认不出（`ev == null`）或正文解析不出（`ops == null`）
                    //    都**不注册**。理由见 `WhenEvent.cs` 文件头 ⚠️① ——
                    //    注册一条永远不会被消费（或认错时机）的效果 = 骗玩家。
                    if (ev != null && ops != null && ops.Count > 0)
                    {
                        foreach (var o in ops) if (o.Source == null) o.Source = Name + "：" + body;
                        _whenTriggers.Add(new WhenTrigger { Ev = ev, Ops = ops, Body = body });
                    }
                }
                return;     // `When …` 开头的那段**不会再是降费句式**，收完就走
            }

            // ---- ② `… Lower cost by N when <事件>`（句尾）----
            //    ⚠️ 用 `LastIndexOf(" when ")` 而不是 `IndexOf`：事件短语自己可能带 `when`
            //       （`When you play a Stratagem, …` 那种不会走到这里，但万一以后有嵌套）。
            string low = s.ToLowerInvariant();
            int w = low.LastIndexOf(" when ");
            if (w < 0) return;
            string head = s.Substring(0, w).Trim();
            string evTail = s.Substring(w + 6).Trim();

            // 前缀必须是降费句式（`Lower cost by 1` / `Costs 1 less` 那些在别处解析，这里只管卡面这一种）
            var m = System.Text.RegularExpressions.Regex.Match(
                head, @"lower\s+cost\s+by\s+(\d+)\s*$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!m.Success) return;

            var cwev = WhenEvents.Parse(evTail);
            if (cwev == null) return;   // 事件认不出 → 不收（同 ⚠️①）
            _costWhens.Add(new CostWhen
            {
                Ev = cwev,
                Delta = int.Parse(m.Groups[1].Value),
                Body = s,
            });
        }

        /// <summary>
        /// 本卡**当某件事发生时**该结算的那串 op。没有就是 null。
        ///
        /// <paramref name="who"/> = 发生事件的那个单位归谁（`-1` 无归属）；
        /// <paramref name="card"/> = 那个单位的卡。
        /// 判据全在 <see cref="WhenEvents.Matches"/>（**只此一份**，见那个文件的文件头 ⚠️②）。
        /// </summary>
        public List<EffectOp> FireWhen(string kind, int listener, int who, CardDef card)
        {
            if (kind == null || _whenTriggers.Count == 0) return null;
            List<EffectOp> acc = null;
            foreach (var t in _whenTriggers)
            {
                if (t.Ev == null || t.Ev.Kind != kind) continue;
                if (!WhenEvents.Matches(t.Ev, listener, who, card)) continue;
                if (acc == null) acc = new List<EffectOp>();
                acc.AddRange(t.Ops);
            }
            return acc;
        }

        /// <summary>
        /// 本卡在**手里时**监听的事件里，<paramref name="kind"/> 这一类命中的那些（降费族）。
        /// 没有就是 null。
        /// </summary>
        public List<CostWhen> FireCostWhen(string kind, int listener, int who, CardDef card)
        {
            if (kind == null || _costWhens.Count == 0) return null;
            List<CostWhen> acc = null;
            foreach (var t in _costWhens)
            {
                if (t.Ev == null || t.Ev.Kind != kind) continue;
                if (!WhenEvents.Matches(t.Ev, listener, who, card)) continue;
                if (acc == null) acc = new List<CostWhen>();
                acc.Add(t);
            }
            return acc;
        }

        // ---- ICardProvider ----
        public string Title { get { return Name; } }
        public bool IsUnit { get { return Type == "unit" || Type == "hero"; } }
        public IReadOnlyDictionary<string, int> Keywords { get { return _keywords; } }

        /// <summary>关键词 → 效果（只有带效果文字、且解析成功的才有）</summary>
        public IReadOnlyDictionary<string, EffectSpec> Effects { get { return _effects; } }

        /// <summary>带了效果文字但**解析不出来**的那些（卡面标 `*`，别装作能跑）</summary>
        public IReadOnlyList<string> UnparsedEffects { get { return _unparsed; } }

        public bool HasAbility { get { return Ability != null; } }

        /// <summary>这个关键词对应的效果。没有返回 null —— 调用方必须判</summary>
        public EffectSpec Effect(string keyword)
        {
            EffectSpec s;
            return (keyword != null && _effects.TryGetValue(keyword, out s)) ? s : null;
        }

        public bool Has(string keyword)
        {
            return _keywords.ContainsKey(keyword);
        }

        /// <summary>关键词的 X 值。无数字的关键词（Shield/Flying…）按「存在即真」取 1</summary>
        public int KwValue(string keyword)
        {
            int v;
            return _keywords.TryGetValue(keyword, out v) ? v : 0;
        }

        public override string ToString()
        {
            return IsUnit ? $"{Name}({Cost}费 {Attack}/{Health})" : $"{Name}({Cost}费 {Type})";
        }
    }

    /// <summary>
    /// 卡面关键词串 → 规范化字典。
    ///
    /// 卡面写法很脏（实测 146 种不同串），元素格式是
    /// `"Waystone"` / `"Shuriken 1"` / `"Rally: 效果文字"` / `"Can't Attack"` / `"Armour 2."`。
    /// 规则（照抄 `rule_core._parse_keywords`，**别改**）：
    ///   1. 先按 `:` 切，只取前半段（后半段是效果文本，不是关键词）
    ///   2. 小写后按**前缀**匹配表 → 规范化名（多词变体在表里必须排在前面）
    ///   3. 值 = **整串里第一个数字**；没有数字就是 1（「存在即真」）
    /// </summary>
    public static class KeywordTable
    {
        /// <summary>规范化名。哪些算「已实现」见 <see cref="Implemented"/>。</summary>
        public const string Vanguard = "vanguard";
        public const string Stealth = "stealth";
        public const string Flying = "flying";
        public const string Armour = "armour";
        public const string Shield = "shield";
        public const string CantAttack = "cantattack";
        public const string LongRange = "longrange";
        /// <summary>黑暗契约：可带变体（`of blood` / `of excess` / `of fate` / `of resilience`），
        /// 四种效果不同 —— 变体记在载荷里，见 <see cref="GivePayload"/> 的 `Variant`</summary>
        public const string DarkPact = "darkpact";

        // ---- 2026-09-12 新增：触发类 + 主动技能（都靠 `EffectSpec` 那个封闭文法结算）----
        //
        // 时机全部照抄规则书 :161 那张 61 关键词表（`资料/规则书/…_中文翻译.md`）：
        public const string Rally = "rally";              // 集结：从手牌部署后触发
        public const string Strike = "strike";            // 猛击：攻击后触发（本单位存活）
        public const string Slay = "slay";                // 斩杀：攻击并摧毁单位后触发（本单位存活）
        public const string Backlash = "backlash";        // 反噬：单位死亡时触发
        public const string Penitence = "penitence";      // 忏悔：受到伤害但未死亡时触发
        /// <summary>
        /// 主动技能：**消耗本单位的一次行动**发动效果。
        /// 这是本工程自己定的关键词 —— 原版的「替代行动」（职责 Duty / 狂暴 Ferocity / 祈祷 Pray /
        /// 议程 Agenda）是一族用法各异的东西，v1 先收成一条：「花一次行动换一个效果」。
        /// 不是原版关键词，所以卡面上按本工程的名字显示。
        /// </summary>
        public const string Ability = "ability";

        /// <summary>
        /// **路标石**（灵族，2026-09-13 第三十三轮）：本单位死亡时给控制者 **+1 灵魂石**。
        /// 出处：规则书 `:225`「本单位死亡时翻面表示生成 1 颗灵魂石」+ `:210`
        /// 「携带路标石的灵族单位**被摧毁时生成**；控制者回合可收集」。
        /// ⚠️ **我们简化了一段**：规则书是「死亡 → 翻面 → 之后被摧毁才生成」两段式，
        ///    原版还有一个 `useWaystone` 主动行动（`BattleActionType.cs:79 = 76`，
        ///    `BattleManager.TryUsingWaystone:6967`）——这些**没做**，我们一死就直接生成。
        ///    要精确复刻得给单位加「翻面」这个棋盘状态，是独立的一轮。
        /// </summary>
        public const string Waystone = "waystone";

        /// <summary>本版**真正生效**的关键词。其余关键词会被解析出来但并不参与结算 —— 见 <see cref="RuleCore.UnimplementedKeywords"/>。</summary>
        public static readonly HashSet<string> Implemented = new HashSet<string>
        {
            Vanguard, Stealth, Flying, Armour, Shield,
            // 这两个也有真实结算，只是不体现在 UnitState 的字段上：
            //   CantAttack → IsValidTarget / DeclareAttack 里直接拒绝
            //   LongRange  → 远程攻击免反击
            CantAttack, LongRange,
            // 触发类 + 主动技能：结算全部走 `RuleCore.FireUnitTrigger` / `UseAbility`。
            // ⚠️ 「关键词已实现」≠「这张卡的效果能跑」—— 效果文字解析不出来的，
            //    由 `CardDef.UnparsedEffects` 单独标出来（卡面照旧打 `*`）。
            Rally, Strike, Slay, Backlash, Penitence, Ability,
            // 路标石（2026-09-13 第三十三轮）：死亡时给控制者 +1 灵魂石
            // —— 结算在 `RuleCore.KillUnit` 里（和 `Backlash` 同一个时机点上）。
            Waystone,

            // ---- 2026-09-12 补的四个（都是战术卡高频载荷）----
            // 出处：规则书 :187/:190 与 :98；结算照 `rule_core.gd` 的 `_damage_unit:4406` / 部署段 `:2248`
            /// <summary>侧翼：**打出当回合可以行动**（规则书 :187「打出当回合可攻击任意敌方部队」；
            /// :98「部署当回合不能行动，除非注明，如迅捷/侧翼/狂暴」）。
            /// 结算在 `UnitState` 构造里（部署时不解疲劳）。</summary>
            "flank",
            /// <summary>迅捷：同侧翼 —— 部署当回合不疲劳（原版 `:2248` 把两者写在一起）</summary>
            "fast",
            /// <summary>无敌：**免疫伤害，也不能被摧毁**（规则书 :190「无法被伤害或摧毁」；
            /// 原版 `_damage_unit:4414` 返回 0、摧毁分支 `:2833` 直接忽略）。</summary>
            "invulnerable",
            /// <summary>易伤 X：**受到伤害 +X**（原版 `_damage_unit:4418`：`actual += kw_val("vulnerable")`）。
            /// ⚠️ 名字容易看反 —— 它是**加伤**，不是减伤。</summary>
            "vulnerable",

            // ---- 2026-09-12 第二批：攻击时机上的五个（全是战术卡 `give` 的载荷）----
            // 都在 `RuleCore.DeclareAttack` 里，顺序照原版 `rule_core.gd` 的攻击段
            /// <summary>星镖 X：**攻击伤害之前**先对目标追加 X 点（规则书 :207；原版 `:4285`）；
            /// 目标被这 X 点打死就跳过攻击伤害</summary>
            "shuriken",
            /// <summary>爆裂 X：攻击时对目标**相邻的敌方部队**造成 X 伤害（规则书 :170；原版 `:4348`）。
            /// 不溅射督军</summary>
            "blast",
            /// <summary>震荡：被本单位攻击的单位获得**眩晕**（规则书 :177；原版 `:4363`）</summary>
            "concussion",
            /// <summary>嗜血：每回合可攻击**至多 2 次**（规则书 :172；原版 `:4205`）。
            /// 实现在攻击配额那两处 —— **达到上限才疲劳**</summary>
            "bloodthirst",
            /// <summary>标记光 X：受到**远程**攻击伤害 +X，受远程伤害后移除全部标记光
            /// （规则书 :192；原版 `:4296` 一带）</summary>
            "markerlight",
            /// <summary>伪装：**攻击前**不能被敌方战术/效果选中（规则书 :173）。
            /// 选中拦截在 `EffectResolver.AddSide`；攻击后失去（`DeclareAttack`）</summary>
            "camouflage",

            // ---- 2026-09-12 第三批：战场事件系（每条都有规则书明文 + 原版实现位置）----
            /// <summary>猎杀标记：**可叠加**。带标记的敌方部队被摧毁时，对敌方督军造成 X 伤害、
            /// 治疗击杀者督军 X（X = 标记数）。规则书 :189；原版 `rule_core.gd:4562`。</summary>
            "huntmark",
            /// <summary>黑暗契约：四种（鲜血/纵欲/命运/韧性），效果见规则书 :179 与
            /// `rule_core.gd:1655` 的 `DARK_PACT_FX`。**变体记在 `PayloadOp.Variant`**</summary>
            DarkPact,
            /// <summary>兽群：场上每有 1 个友方部队 +1 近战 +1 远程（规则书 :195；
            /// 原版 `:4172` `field_attack`）。实现在 `RuleCore.FieldAttack`</summary>
            "pack",
            /// <summary>哨戒 X：被攻击时对攻击者先造成 X 伤害，「然后照常结算攻击」
            /// （规则书 :205；原版 `:4280`）。实现在 `DeclareAttack` 第 0 步</summary>
            "sentry",
            /// <summary>狙击：**远程**攻击会摧毁目标时，不承受反击伤害（规则书 :209；原版 `:4312`）</summary>
            "sniper",
            /// <summary>再生 X：每回合结束时治疗 X（规则书 :201；原版 `:4478`）。
            /// 实现在 `RuleCore.EndTurn`</summary>
            "regeneration",
            /// <summary>失明：期间**远程攻击力设为 0**（规则书 :166；原版 `:4212` 直接拒绝远程攻击）。
            /// 实现在 `RuleCore.FieldAttack`（数值层）+ `EndTurn`（到期清）。</summary>
            "blind",
            /// <summary>压制：无法执行**近战**攻击（规则书 :194；原版 `:4209` 直接拒绝近战）。
            /// ⚠️ 只禁近战 —— 远程照常</summary>
            "pindown",
            /// <summary>践踏：**溢出伤害**对目标相邻随机一个敌方单位造成（规则书 :213；
            /// 原版 `:4372` + `_stomp_splash:4489`）。实现在 `RuleCore.DeclareAttack`
            /// ⚠️ 只在**目标被打死**时才算「溢出」—— 没死就没有溢出</summary>
            "stomp",

            // ---- 🆕 2026-09-13 第三十二轮：临时（Ephemeral），98 张 ----
            /// <summary>
            /// 临时：**回合结束时若在手牌则移除**（规则书 `:183`），而且
            /// **「从游戏中移除（非弃置）」**（`:229`）—— 不进弃牌堆，进 `ctx.Removed`。
            ///
            /// ⚠️ **本关键词的机制和别人不一样**：它不是「一个单位身上的一条属性」，
            ///    而是**手牌里那张牌的处置规则** —— 所以结算**不在 `UnitState` 上**，
            ///    在 `RuleCore.EndTurn` 的 `SweepEphemeral` 段。
            ///
            /// ⚠️ **判据不只看这个关键词**：规则书 `:229` 点名的三族里，
            ///    伴生生成的部队与潮涌的复制**卡面并没有印 `Ephemeral`**
            ///    —— 它们靠**对局上的标记**（原版是 `BuffType.ephemeralCopy = 25`）。
            ///    ⇒ 两边都算，判据**只有一处**：`BattleContext.IsEphemeral`。
            ///
            /// ⚠️ **这个关键词只管自己**：`Talent`（生成临时战术）/ `Companion` / `Tide X`
            ///    三个**来源**还没做，它们生成的牌要自己调 `MarkEphemeral`。
            /// </summary>
            "ephemeral",
        };

        // 前缀匹配表 —— 顺序有意义：**多词变体必须排在单词前面**
        // （"can't attack" 要排在……其实前缀匹配下 "vanguard" 之类不会互相吃掉，
        //   但 "long range"/"longrange" 和将来的扩展要保持这个约定）
        static readonly string[][] Prefixes =
        {
            new[] { "can't attack", CantAttack }, new[] { "cant attack", CantAttack },
            new[] { "blood thirst", "bloodthirst" }, new[] { "long range", LongRange },
            new[] { "hunt mark", "huntmark" }, new[] { "dark pact", "darkpact" },
            new[] { "vanguard", Vanguard }, new[] { "stealth", Stealth }, new[] { "flying", Flying },
            new[] { "rally", Rally }, new[] { "backlash", Backlash }, new[] { "slay", Slay },
            new[] { "blast", "blast" }, new[] { "armour", Armour }, new[] { "armor", Armour },
            new[] { "shield", Shield }, new[] { "regeneration", "regeneration" }, new[] { "stun", "stun" },
            new[] { "fast", "fast" }, new[] { "flank", "flank" },
            new[] { "ambush", "ambush" }, new[] { "artifice", "artifice" }, new[] { "ephemeral", "ephemeral" },
            new[] { "concussive", "concussion" }, new[] { "concussion", "concussion" },
            new[] { "remnant", "remnant" }, new[] { "longrange", LongRange },
            new[] { "invulnerable", "invulnerable" }, new[] { "pindown", "pindown" },
            new[] { "blind", "blind" }, new[] { "camouflage", "camouflage" },
            new[] { "vulnerable", "vulnerable" }, new[] { "strike", Strike }, new[] { "stomp", "stomp" },
            new[] { "unstable", "unstable" }, new[] { "bloodthirst", "bloodthirst" },
            new[] { "cruelty", "cruelty" },
            new[] { "shuriken", "shuriken" }, new[] { "sniper", "sniper" }, new[] { "waystone", "waystone" },
            new[] { "oath", "oath" }, new[] { "codex", "codex" }, new[] { "uprising", "uprising" },
            new[] { "teleport", "teleport" }, new[] { "pack", "pack" }, new[] { "tide", "tide" },
            new[] { "swarm", "swarm" }, new[] { "agenda", "agenda" }, new[] { "destroyer", "destroyer" },
            new[] { "duty", "duty" }, new[] { "mob", "mob" }, new[] { "synapse", "synapse" },
            new[] { "regiment", "regiment" },
            new[] { "pray", "pray" }, new[] { "penitence", Penitence }, new[] { "ecstasy", "ecstasy" },
            new[] { "sentry", "sentry" }, new[] { "markerlight", "markerlight" },
            new[] { "companion", "companion" },
            new[] { "stimulation", "stimulation" },
            // 🔴 2026-09-13 补（派子代理做「关键词三列对账」时查出）：这三个词**根本不在表里**，
            //    于是 `Normalize` 返回 null → `Parse`/构造函数**双双丢弃**
            //    ⇒ 它们既不出现在 `UnimplementedKeywords()` 单上，**卡面也不打 `*`**
            //    —— 两张诊断单都看不见，正好踩本工程「静默失败」红线。
            //    实测命中：`Talent` **91 张**（SaimHann 一族的关键词，`Avatar of Khaine` 的
            //    `Talent: Wrath of Khaine`、`Wave Serpent` 的 `Talent: Skyborne Deployment` …）·
            //    `Ferocity` **13 张**（SpaceWolves 的替代行动族）。`Quest` 本卡池 0 张，但规则书收了
            //    （`:199`）、原版 `DefinedTrait.quest :74` 有 —— 一起补上，免得以后撞见又要查一遍。
            //    ⚠️ **补的只是「认得出」这一层**，三者仍然**没有机制**（`Implemented` 里没有）——
            //    现在的行为是「如实报成未实现 + 卡面打 `*`」，这才是对的。
            //    出处：`资料/关键词三列对账.md` §不一致·3。
            new[] { "talent", "talent" }, new[] { "ferocity", "ferocity" },
            new[] { "quest", "quest" },
            // 🆕 2026-09-13 第三十二轮：**临时（Ephemeral）** —— 卡池里出现**最多**的关键词（98 张）。
            //    规则书 `:183`「回合结束时若在手牌则移除」、`:229`「**从游戏中移除（非弃置）**」。
            //    ⚠️ 补这里只是「认得出」；机制在 `RuleCore.EndTurn` 的清扫段 +
            //      `BattleContext.IsEphemeral`（判据只此一处），见 `资料/临时卡Ephemeral_设计与实现计划.md`。
            new[] { "ephemeral", "ephemeral" },
            // ⚠️ 本工程自定（原版 61 个里没有）—— 放最后，免得吃掉将来可能加进来的同前缀词
            new[] { "ability", Ability },
        };

        /// <summary>
        /// 一条卡面关键词串 → 规范名。**认不出来返回 null**（不是空串 —— 空串会被误当成一个关键词）。
        /// 规矩照抄 `rule_core._parse_keywords`，见本类文件头，**别改**。
        /// </summary>
        public static string Normalize(string item)
        {
            int ignored;
            return Normalize(item, out ignored);
        }

        /// <summary>
        /// 同 <see cref="Normalize(string)"/>，但**把命中的前缀长度带出来**。
        ///
        /// 为什么要它：`Normalize` 是**前缀**匹配，`"Stun a random enemy"` 也会命中 `stun`。
        /// `EffectText` 判「这一句是不是**纯**关键词声明」时必须知道前缀有没有吃满整句 ——
        /// 只看返回非 null 的话，**整句眩晕效果会被当成关键词声明跳过**（静默失效）。
        /// </summary>
        public static string Normalize(string item, out int matchedLength)
        {
            matchedLength = 0;
            if (string.IsNullOrEmpty(item)) return null;

            // 1) 按 ':' 切，只取前半段（后半段是效果文本，不是关键词）
            string s = item;
            int colon = s.IndexOf(':');
            if (colon >= 0) s = s.Substring(0, colon);
            s = s.Trim().ToLowerInvariant();
            if (s.Length == 0) return null;

            // 2) 前缀匹配
            foreach (var pair in Prefixes)
                if (s.StartsWith(pair[0])) { matchedLength = pair[0].Length; return pair[1]; }

            return null;
        }

        /// <summary>`':'` **后面**那半段（效果原文，例 `"Damage 2 EnemyUnit"`）。没有或为空返回 null。</summary>
        public static string EffectText(string item)
        {
            if (string.IsNullOrEmpty(item)) return null;
            int colon = item.IndexOf(':');
            if (colon < 0) return null;
            string s = item.Substring(colon + 1).Trim();
            return s.Length == 0 ? null : s;
        }

        public static Dictionary<string, int> Parse(IEnumerable<string> raw)
        {
            var kws = new Dictionary<string, int>();
            if (raw == null) return kws;
            foreach (var item in raw)
            {
                string name = Normalize(item);
                if (name == null) continue;

                // 值 = 整串里第一个数字；没有就是 1
                // ⚠️ 找的是**原始串**里的第一个数字，不是切完 ':' 之后的 —— rule_core 就是这么做的
                kws[name] = FirstNumber(item);
            }
            return kws;
        }

        static int FirstNumber(string s)
        {
            int i = 0;
            while (i < s.Length)
            {
                if (char.IsDigit(s[i]))
                {
                    int v = 0;
                    while (i < s.Length && char.IsDigit(s[i]))
                    {
                        v = v * 10 + (s[i] - '0');
                        i++;
                    }
                    return v;
                }
                i++;
            }
            return 1;
        }
    }
}
