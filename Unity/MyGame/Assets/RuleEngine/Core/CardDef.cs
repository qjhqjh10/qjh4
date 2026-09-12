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
