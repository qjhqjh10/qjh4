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
        /// 别的触发词（`Talent` 83 条 · `Pray` 22 · `Duty` 17 · `Agenda` 8 · `Artifice` 8 …
        /// 实测 212 条）**引擎里根本没有那个时机**，收了也没人消费 ——
        /// 注册一条永远不会被消费的效果 = 骗玩家（本工程的静默失败红线）。
        /// ⇒ 它们**照旧判「不认识」**，卡面标 `*`。
        ///
        /// ✅ 2026-09-13 第三十四轮：`Mob` 与 `Regiment` 从「没有时机」那一栏**搬到了这里** ——
        /// 时机接在 `RuleCore.DeclareAttack` 攻击后那一段（近战 / 远程各一条）。
        /// 同一轮还收了 `Cruelty`（时机在 `RuleCore.Hurt`）。
        /// ⚠️ **`Unstable` 不在这里** —— 它**卡面没有正文**（只写裸关键词），效果完全由规则定义，
        ///    没有「触发式正文」可收。它的机制在 `RuleCore.CleanupDeaths`。
        /// ⚠️ **`Ecstasy` 也不在这里** —— 它**连正文都收不下来**（`AddTriggerOp` 是整词相等比对，
        ///    `Ecstasy 2:` 的 `"ecstasy 2"` 对不上 `"ecstasy"`）。见那个常量的注释。
        /// </summary>
        public static readonly string[] RoutableTriggers = {
            KeywordTable.Rally, KeywordTable.Strike, KeywordTable.Slay,
            KeywordTable.Backlash, KeywordTable.Penitence,
            KeywordTable.Mob, KeywordTable.Regiment,
            KeywordTable.Cruelty, KeywordTable.Artifice,
            // 替代行动那一族（2026-09-13 A2）：引擎在「玩家主动使用」那条路上调它们的正文
            // （`RuleCore.UseAlternative`）。收在这里 = `Duty:` / `Ferocity:` 的正文收得到。
            KeywordTable.Duty, KeywordTable.Pray, KeywordTable.Ferocity, KeywordTable.Agenda,
            // 起义（2026-09-13 A2）：**之后每部署一个部队时**触发（`RuleCore.FireUprising`）
            KeywordTable.Uprising,
            // 激励（2026-09-13 A2）：**被战术选中时、结算前**触发（`EffectResolver.PlayTactic` 里）
            KeywordTable.Stimulation,
            // 传送（2026-09-13 A2）：**当回合从牌库抽到即打出时**触发（`RuleCore.PlayCard` 里）
            KeywordTable.Teleport,
            // 伏击（2026-09-13 A2）：**面朝下打出**，窗口到期时翻开（`RuleCore.RevealAmbush` / `ApplyDamage`）
            KeywordTable.Ambush,
            // 🔴 典籍（2026-09-14 A4 批 4）：`Codex: <正文>` 的正文要收得下来，
            //    否则 `TriggerOps("codex")` **恒为 null** ⇒ 强行触发它（`Author of the Codex`）
            //    和「能量归零自动触发」**两条路都是空转**。
            // ⚠️ **收下来 ≠ 去掉那个条件**：`EffectText` 给 `Codex:` 的每个 op 挂了
            //    `ConditionKind = EnergyZero`（规则书 `:175`），**强制触发时要显式绕过**它
            //    （`RuleCore.BeginForcedTrigger`）—— 不绕就「等于没做」。
            KeywordTable.Codex };

        /// <summary>
        /// **带正文**的触发关键词 —— 卡面写 `关键词: &lt;效果&gt;` 时正文挂在冒号后。
        /// 这个名单决定「**没有 `关键词:` 前缀**时，整条 `desc` 该算谁的正文」
        /// （见 <see cref="CollectBareKeywordBody"/>）。
        ///
        /// ⚠️ **`remnant` / `destroyer` / `swarm` / `tide` / `companion` / `synapse` 不在这里** ——
        ///    它们的效果**不是卡面正文**（是规则写死的机制，或者带的是数字/卡名，
        ///    如 `Tide 2` / `Companion 2: Missile Drone`），拿整条 desc 当它们的正文会张冠李戴。
        /// </summary>
        public static readonly string[] BodyKeywords = {
            KeywordTable.Rally, KeywordTable.Strike, KeywordTable.Slay,
            KeywordTable.Backlash, KeywordTable.Penitence, KeywordTable.Mob,
            KeywordTable.Regiment, KeywordTable.Cruelty, KeywordTable.Artifice,
            KeywordTable.Agenda, KeywordTable.Ferocity, KeywordTable.Pray,
            KeywordTable.Duty, KeywordTable.Uprising, KeywordTable.Teleport,
            KeywordTable.Stimulation, KeywordTable.Ambush,
            // 🔴 典籍（2026-09-14 A5）：`Codex` **漏在这儿**了 —— 它 2026-09-14 A4 批 4
            //    刚进 `RoutableTriggers`，但没进 `BodyKeywords` ⇒ **卡面裸写正文的 10 张
            //    Ultramarines**（`Epistolary Librarian` / `Inceptor Sergeant` / `Primaris Chaplain` /
            //    `Primaris Judiciar` / `Primaris Techmarine` / `Sergeant Telion` / `Sergeant Allectius` /
            //    `Redemptor Dreadnought` / `Stormtalon` / `Predator Annihilator`）
            //    `TriggerOps("codex")` **恒为 null** ⇒ 效果静默不发生、`Author of the Codex` 对它们空转。
            //    ✅ 实测口径（不是抄文档）：`cards_engine.json` 里声明 `Codex` 的 20 张中，
            //    `desc` **无前缀**的正好 10 张（其余 10 张写的是 `Codex:` / `[Codex]` 前缀，
            //    走 `AddTriggerOp` 那条路）。⚠️ 文档原写「9 张」**漏了 `Sergeant Allectius`**（已更正）。
            KeywordTable.Codex,
        };

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
            // ④ **天赋名**（`Talent: <名字>`）—— 第三十四轮。见 <see cref="TalentName"/>。
            CollectTalent(keywords);
            // ⑤ 🆕 **裸写正文**（卡面没有 `关键词:` 前缀时，整条 `desc` 就是那个关键词的正文）
            //    —— 2026-09-13 A2。见 <see cref="CollectBareKeywordBody"/>。
            CollectBareKeywordBody(keywords);
            // ⑥ **伴生部队的名字**（`Companion 2: Missile Drone`）—— 2026-09-13 A2。
            CollectCompanionName(keywords);
            // ⑦ 🆕 **静态条件降费**（`This costs N less if you control a unit with Stealth`）
            //    —— 2026-09-13 A4 批 1。见 <see cref="CostIfControl"/>。
            CollectCostIfControl(keywords);
            // ⑧ 🆕 **事件型手牌陷阱**（`When you play a Stratagem, …`，非单位卡）
            //    —— 2026-09-14 A4 批 4。见 <see cref="HandTrapWhens"/>。
            CollectHandTrapWhens();
            // ⑨ 🆕 **「被这一下打到的那个」**（`Destroy any troop attacked by this unit`）
            //    —— 2026-09-14 A5 批 3。见 <see cref="CollectAttackedBody"/>。
            CollectAttackedBody();
        }

        /// <summary>
        /// **「被这一下打到的那个」那一族**的正文 —— 卡面 `<动词> … attacked [by this unit]`。
        /// 全池 **6 张，且一个关键词都没有**：`Venomthrope`（`Destroy any troop attacked by this unit`）·
        /// `Blastmaster Noise Marine`（`… with Armour …`）· `Sonic Blaster Noise Marine`
        /// （`Stun enemy troops attacked and give them -1 [armor] and -1 [attack]`）·
        /// `Stikkbomb Boy`（`Stun enemies attacked`）· `Snakebite Grot`（`Tide 1. Stun troops attacked.`）·
        /// `Arjac Rockfist`（`Destroys any enemy troop with Hunt Mark attacked.`）。
        ///
        /// **为什么不走 `TriggerOps` 那个字典**：那本字典是**按关键词**存的（`Strike:` / `Rally:` …），
        /// 而这 6 张卡面**没有任何关键词** —— 触发点是照原版反编译认出来的
        /// （`AbilityTrigger.UnitAttack = 50`，`CardScript__ResolveUnitAttacked.c:30` 传 `0x32`，
        /// 和 Slay/Strike/Mob/Regiment 在**同一个函数**里），不是卡面写的。
        /// 硬塞一个假关键词进去，会让「按触发点统计」那张报表多出一个不存在的关键词（假数据）。
        ///
        /// ⚠️ **必须至少有一条 op 带着 `Target.AttackedBySelf`** 才收 —— 卡池里还有别的句子
        ///    带 `attacked` 字样（`When a friendly unit is attacked, …` 是**事件短语**），
        ///    没有这道门会把它们误收进来。
        /// </summary>
        void CollectAttackedBody()
        {
            if (string.IsNullOrWhiteSpace(Desc)) return;
            foreach (string seg in EffectText.Split(Desc))
            {
                if (string.IsNullOrEmpty(seg)) continue;
                if (seg.IndexOf(" attacked", System.StringComparison.OrdinalIgnoreCase) < 0) continue;

                var ops = EffectText.Parse(seg, out _, out _);
                if (ops == null || ops.Count == 0) continue;

                bool hit = false;
                foreach (var o in ops)
                    if (o.Target != null && o.Target.AttackedBySelf) { hit = true; break; }
                if (!hit) continue;

                foreach (var o in ops) if (o.Source == null) o.Source = Name + "：" + seg;
                foreach (var o in ops) _attackedOps.Add(o);
                if (_attackedText == null) _attackedText = seg.Trim();
            }
        }

        /// <summary>「被这一下打到的那个」那一族的正文 op（没有返回 null）。见 <see cref="CollectAttackedBody"/>。</summary>
        public IReadOnlyList<EffectOp> AttackedOps
        {
            get { return _attackedOps.Count == 0 ? null : _attackedOps; }
        }

        /// <summary>上面那一族的**卡面原文**（日志用）。没有返回 null。</summary>
        public string AttackedText { get { return _attackedText; } }

        readonly List<EffectOp> _attackedOps = new List<EffectOp>();
        string _attackedText;

        /// <summary>
        /// **卡面没写 `关键词:` 前缀时，整条 `desc` 就是那个关键词的正文**（2026-09-13 A2）。
        ///
        /// **为什么需要它**：实测全卡池 **200 张**卡的触发/行动关键词**只在 `keywords` 数组里**、
        /// 正文里**没有前缀** —— 例如
        ///   · `Dark Apostle`（`kw=['Artifice']`，`desc='Give a random Dark Pact to each friendly troop'`）
        ///   · `Blood Claw`（`kw=['Ferocity']`，`desc='Deal 3 damage to an enemy'`）
        ///   · `Hybrid Metamorph`（`kw=['Blast 1','Uprising']`，`desc='Gain +2 Attack and +1 Health'`）
        /// 而 `AddTriggerOp` **要求冒号** ⇒ 这些卡的正文**一条都收不到**
        /// ⇒ 效果**静默不发生**（卡面还打着 `*`，玩家只看到「这卡有关键词但没反应」）。
        ///
        /// **判据（两条，都从严）**：
        ///   ① 正文里**已经有**任何一个带正文关键词的 `X:` 前缀 ⇒ 不归这条管（`AddTriggerOp` 收过了）；
        ///   ② 本卡声明的带正文关键词**必须唯一**（见 <see cref="BodyKeywords"/>）——
        ///      两个以上就是「这句正文归谁」有歧义（`Morkai Eliminator` = `Rally` + `Ferocity`
        ///      两个都带正文），**宁可不动**：猜错 = 在错的时机放效果，比不触发更难查。
        /// 不满足就**什么都不做**（如实：那几张卡的效果仍然收不到，由覆盖率报告看着）。
        /// </summary>
        void CollectBareKeywordBody(IEnumerable<string> keywords)
        {
            if (string.IsNullOrWhiteSpace(Desc) || keywords == null) return;

            // ① 已经有 `X:` 前缀（X 是带正文的关键词）⇒ 不归这条管
            // ⚠️ 判据要走 `EffectText.NormalizeIconPrefix`：`[Codex] …` 那种**图标写法**
            //    语义等于 `Codex:`（`Death from Above`），不归一就看不见它 ⇒
            //    会把整条 desc（含不属于 Codex 的正文）当成 Codex 的正文（实测撞到过）。
            foreach (string seg in EffectText.Split(Desc))
            {
                string s = EffectText.NormalizeIconPrefix(seg);
                int c = s.IndexOf(':');
                if (c > 0 && IsBodyKeyword(s.Substring(0, c).Trim().ToLowerInvariant())) return;
            }

            // ② 带正文的关键词**唯一**才敢认
            string only = null;
            foreach (string item in keywords)
            {
                string k = NormalizeKeywordName(item);
                if (!IsBodyKeyword(k)) continue;
                if (only == null) only = k;
                else if (only != k) return;          // 歧义 ⇒ 不动
            }
            if (only == null) return;
            if (_triggerOps.ContainsKey(only)) return;   // 前面几支已经收过了

            // 正文交给战术卡那个解析器；解析不出来就**不收**（保持「卡面标 `*`」的诚实）
            var ops = EffectText.Parse(Desc, out _, out _);
            if (ops == null || ops.Count == 0) return;
            // 🔴 **`codex` 的正文必须带上「你的能量为 0」那个条件**（2026-09-14 A5）——
            //    判据**转调** `EffectText.MarkCodexCondition`（全仓只此一处），
            //    和带 `Codex:` 前缀那条路**同一份**。不挂的话：等能量归零才该触发的东西
            //    会变成「随时触发」，而且**没有任何报错**（静默打错时机）。
            if (only == KeywordTable.Codex) EffectText.MarkCodexCondition(ops, Desc.Trim());
            _triggerOps[only] = ops;
            _triggerText[only] = Desc.Trim();
        }

        /// <summary>关键词条目 → 规范名：`"Teleport: Gain Vanguard and attack by itself"` → `teleport` ·
        /// `"Tide 2"` → `tide` · `"Remnant."` → `remnant`。</summary>
        static string NormalizeKeywordName(string item)
        {
            string k = (item ?? "").Trim().ToLowerInvariant();
            int cut = k.IndexOfAny(new[] { ':', ' ' });
            if (cut > 0) k = k.Substring(0, cut);
            return k.TrimEnd('.').Trim();
        }

        static bool IsBodyKeyword(string k)
        {
            foreach (string t in BodyKeywords) if (t == k) return true;
            return false;
        }

        /// <summary>
        /// **伴生部队的名字** —— `Companion 2: Missile Drone` 里的 `Missile Drone`。
        /// 没有 = null。
        ///
        /// ⚠️ **两个来源都要扫**（和 <see cref="TalentName"/> 同一个道理）：
        ///   · `keywords` 里带全的：`Companion 2: Missile Drone`（`Broadside Battlesuit`）
        ///   · **只有裸 `Companion`、名字在 `desc` 里**：`Enforcer Battlesuit` 的
        ///     `keywords` 是 `['Vanguard','Companion']`，而名字写在 `desc` 的
        ///     `Companion 2: Guardian Drone` 那一段
        /// ⚠️ 名字到 `.` 为止（有的卡后面还接着别的关键词）。
        /// </summary>
        public string CompanionName
        {
            get { return _companionName; }
        }

        /// <summary>`CollectTriggerOps` 里从 `keywords` 原文里抽出来的伴生名（`desc` 优先）</summary>
        string _companionName;

        static string ExtractCompanionName(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var m = System.Text.RegularExpressions.Regex.Match(
                text, @"companion\s*\d*\s*:\s*([^.,]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            string name = m.Success ? m.Groups[1].Value.Trim() : null;
            return string.IsNullOrEmpty(name) ? null : name;
        }

        void CollectCompanionName(IEnumerable<string> keywords)
        {
            _companionName = ExtractCompanionName(Desc);
            if (_companionName != null) return;
            if (keywords == null) return;
            foreach (string item in keywords)
            {
                _companionName = ExtractCompanionName(item);
                if (_companionName != null) return;
            }
        }

        /// <summary>
        /// 本卡的**天赋名** —— `Talent: Witchfire` 里的那个 `Witchfire`。没有就是 null。
        ///
        /// **为什么要单独存它**：`talent` 和别的关键词不一样 —— 它的效果**不是卡面正文**，
        /// 而是「**去卡池里找一张同名的战术卡**，回合开始时塞进手牌」（规则书 `:218`
        /// 「回合开始时在手牌中生成临时战术」）。实测 **80 个天赋名里 72 个在卡池里查得到同名卡，
        /// 而且**全是 `tactic`**（`Witchfire` / `Path of the Seer` / `Flickerjump` …）。
        ///
        /// ⚠️ **名字到 `.` 或 `,` 为止**：实测 `Mekboy Gazmek` 的卡面是
        ///    `Talent: Mekaniak. Mob: Lower the cost…`（后面还接着别的关键词），
        ///    不切的话整串取下来会带上 `Mob:` 那一段。
        /// ⚠️ **查不到同名卡的 8 个**（遇上了**如实打日志**，不静默）：
        ///    `Duelist's Hubris` · `Excessive Vigour` · `Dok's Toolz` · `Da Bigger Dey Iz` ·
        ///    `Waaagh` · `'Uge Choppa` · `Euphoric Strike` ·
        ///    `A random Black Legion Psychic Power`（最后这个是**短语**不是名字，正则误抓的）。
        /// </summary>
        public string TalentName;

        void CollectTalent(IEnumerable<string> keywords)
        {
            TalentName = ExtractTalent(Desc);
            if (TalentName != null) return;
            if (keywords == null) return;
            foreach (string item in keywords)
            {
                TalentName = ExtractTalent(item);
                if (TalentName != null) return;
            }
        }

        static string ExtractTalent(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            // ⚠️ **方括号写法要认**（2026-09-14 A5 批 3）：`[Talent]: Catechism of Death`
            //    （`Chaplain Cassius`）—— 卡面把关键词印成图标，OCR 出来就是方括号。
            //    不归一的话 `IndexOf("Talent:")` 找不到（中间隔着 `]`）⇒ 这一句一直挂在
            //    「完全不认识」里，而 `Talent:` 本来是**已经实现**过的一族（`TalentName`）。
            string t2 = System.Text.RegularExpressions.Regex.Replace(
                text, @"\[\s*talent\s*\]", "Talent",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            int i = t2.IndexOf("Talent:", System.StringComparison.OrdinalIgnoreCase);
            if (i < 0) return null;
            string rest = t2.Substring(i + 7);
            int end = rest.Length;
            for (int k = 0; k < rest.Length; k++)
                if (rest[k] == '.' || rest[k] == ',') { end = k; break; }
            string name = rest.Substring(0, end).Trim();
            return name.Length == 0 ? null : name;
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

        /// <summary>
        /// **本卡在「手里」就要监听的事件** —— **事件型手牌陷阱**（2026-09-14 A4 批 4）。
        ///
        /// 出处：`Jammed Communications`（Genestealers 的破坏卡）：`When you play a Stratagem,
        /// your Warlord takes 1 damage`（卡图 `Genestealer Cult/6破坏卡/IMG_3817.jpg`，
        /// 橙字兵种行就是 `Sabotage`）。**「你」= 持有者** —— 破坏卡是塞进**对手**手牌的
        /// （规则书 `:204`），和同族 `Poisoned Supplies` 的口径一致。
        ///
        /// 和 <see cref="WhenTriggers"/> **分开**，为什么：
        ///   · 那一族挂在**场上那张牌**上（`CanListenForEvents => IsUnit`），广播器只扫棋盘；
        ///   · 这一族挂着的是**一张躺在手牌里的非单位卡** —— 它**根本不在棋盘上**。
        ///     ⇒ 判据不同（<see cref="EffectText.SplitHandTrapWhen"/> 要 `CardDef` 才知道卡类）、
        ///       消费点不同（`EffectResolver.BroadcastHandTrapWhen` 扫双方手牌）。
        /// ⚠️ 和 <see cref="CostWhens"/> 同为「手里监听」那一类，但那一个是**降费**、
        ///    这个真的**结算效果**，所以是两个列表不是一个。
        /// </summary>
        readonly List<WhenTrigger> _handTrapWhens = new List<WhenTrigger>();
        public IReadOnlyList<WhenTrigger> HandTrapWhens { get { return _handTrapWhens; } }

        /// <summary>
        /// 扫 `desc`：非单位卡 + 整条就是一句 `When &lt;事件&gt;, &lt;正文&gt;` ⇒ 收成手牌陷阱监听器。
        /// 两条判据（卡类 / 形状 / 事件认得 / 正文解得出来）**全在 `EffectText` 那一处**，
        /// 这里只负责「两边都成功才收」，与 <see cref="AddWhenTrigger"/> 同一条纪律。
        /// </summary>
        void CollectHandTrapWhens()
        {
            var split = EffectText.SplitHandTrapWhen(this);
            if (split == null) return;

            var evs = WhenEvents.ParseAll(split[0]);
            bool evHasTarget = false;
            foreach (var e in evs)
                if (e.TargetCriteria != null || e.TargetOwnerIs != -1) { evHasTarget = true; break; }
            var ops = EffectText.Parse(split[1], out _, out _, evHasTarget);
            // ⚠️ **两边都要成功才收**（理由同 `WhenEvent.cs` 文件头 ⚠️①）：
            //    注册一条永远不会被消费、或者认错时机的效果 = 骗玩家。
            if (evs.Count == 0 || ops == null || ops.Count == 0) return;

            foreach (var o in ops) if (o.Source == null) o.Source = Name + "：" + split[1];
            foreach (var e in evs)
                _handTrapWhens.Add(new WhenTrigger { Ev = e, Ops = ops, Body = split[1] });
        }

        /// <summary>
        /// 这张卡**在持有者手里**时，<paramref name="kind"/> 这一类事件命中的 op。没有就是 null。
        ///
        /// ⚠️ **`listenerUnit` 一律传 `null`**：手牌里的牌**没有 `UnitState`**（它不在棋盘上）
        ///    ⇒ `WhenEvent.SelfOnly` / `ActorSelf` 那两族**天然不触发** —— 这正是要的
        ///    （「收不到」比「乱触发」安全，见 `WhenEvents.Matches` 那两条守卫）。
        /// </summary>
        public List<EffectOp> FireHandTrapWhen(string kind, int listener, int who, CardDef card,
                                               UnitState subject = null, UnitState actor = null,
                                               UnitState target = null, int targetWho = -1)
        {
            if (kind == null || _handTrapWhens.Count == 0) return null;
            List<EffectOp> acc = null;
            foreach (var t in _handTrapWhens)
            {
                if (t.Ev == null || t.Ev.Kind != kind) continue;
                if (!WhenEvents.Matches(t.Ev, listener, who, card, null, subject, actor, target, targetWho))
                    continue;
                if (acc == null) acc = new List<EffectOp>();
                acc.AddRange(t.Ops);
            }
            return acc;
        }

        /// <summary>
        /// `This costs N less if you control a unit with &lt;关键词&gt;` —— **静态条件降费**（2026-09-13 A4 批 1）。
        ///
        /// 出处：`Fate Inescapable`（SaimHann）· 规格书 `rule_core.gd:2112`。
        /// 和 <see cref="CostWhen"/> **不是一件事**：那个是**事件**触发的一次性降价，
        /// 这个是**常驻条件** —— 场上还站着那样的单位就便宜，人一没价就回去。
        /// ⇒ 它不登记 `ctx.CostMods`（那是「一次性、可过期」的语义），
        ///    由 <see cref="RuleCore.CostOf"/> **每次现算**。**判据只有那一处。**
        /// ⚠️ 解析那半边在 `EffectText.TryCostIfControl`（**两处各管一半**，同 `CostWhens`）。
        /// </summary>
        public class CostIfControl
        {
            /// <summary>降多少（卡面 `This costs N less` 的 N，负数 = 降价）</summary>
            public int Delta;
            /// <summary>要控制的关键词（`stealth`）</summary>
            public string Keyword;
            public string Body;
            public override string ToString() { return "控制 " + Keyword + " → 费用 " + Delta; }
        }

        readonly List<CostIfControl> _costIfControls = new List<CostIfControl>();
        public IReadOnlyList<CostIfControl> CostIfControls { get { return _costIfControls; } }

        /// <summary>
        /// 扫描卡面里的 `This costs N less if you control a unit with &lt;关键词&gt;`。
        ///
        /// ⚠️ **必须能在**「这句话解析得出来」**之外独立成立** —— 两边都扫（`Desc` + `keywords`），
        ///    理由同 <see cref="CollectWhenTriggers"/>：有卡把正文写在 `keywords` 数组里。
        /// </summary>
        void CollectCostIfControl(IEnumerable<string> keywords)
        {
            foreach (string seg in EffectText.Split(Desc)) AddCostIfControl(seg);
            if (keywords != null)
                foreach (string item in keywords)
                    foreach (string seg in EffectText.Split(item)) AddCostIfControl(seg);
        }

        void AddCostIfControl(string seg)
        {
            if (string.IsNullOrEmpty(seg)) return;
            var m = System.Text.RegularExpressions.Regex.Match(seg.Trim(),
                @"^this\s+costs?\s+(\d+)\s+less\s+if\s+you\s+control\s+(?:a|an)?\s*(?:unit|troop|card)?\s*with\s+([a-z][a-z0-9' \-]*)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!m.Success) return;
            _costIfControls.Add(new CostIfControl
            {
                Delta = -int.Parse(m.Groups[1].Value),
                Keyword = m.Groups[2].Value.Trim().ToLowerInvariant(),
                Body = seg.Trim(),
            });
        }

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

            // ---- ① `When <事件>, <正文>`（`Whenever …` 是同一条，见 `TryParseWhenSentence`）----
            if ((s.StartsWith("When ", System.StringComparison.OrdinalIgnoreCase)
                 || s.StartsWith("Whenever ", System.StringComparison.OrdinalIgnoreCase))
                && CanListenForEvents)
            {
                if (TryParseWhenSentence(s, out var evs, out var body, out var ops))
                {
                    foreach (var o in ops) if (o.Source == null) o.Source = Name + "：" + body;
                    // 一句话两条事件时：**共用一个 `ops` 列表** —— 结算层只读它、
                    // 要改数值时**一律 `Clone()`**（见 `EffectResolver.ResolveOps` 那两条注释），
                    // 所以共享是安全的（不共享反而要解析两遍、两份可能漂移）。
                    foreach (var e in evs)
                        _whenTriggers.Add(new WhenTrigger { Ev = e, Ops = ops, Body = body });
                }
                return;     // `When …` 开头的那段**不会再是降费句式**，收完就走
            }
            // 🔴 **`When …` 开头、但这张卡不是单位**（2026-09-13 候选 E）：
            //    **不注册监听器**（判据与理由见 `CanListenForEvents`）——
            //    一句话：`BroadcastWhen` 只扫**棋盘上的单位**，非单位卡的监听器**没人消费**，
            //    而它照样会把短语写进报告桶 ⇒ 自检的「认不出的短语」里多出一条**根本不该有的**项
            //    （实测就是 `played`）。
            //    ⚠️ 这里**故意不 `return`**，让它落到下面 ② 再试一次降费句式 ——
            //      `When …` 开头的那段本来不会是降费句式，但**万一以后有，丢了就是静默漏**，
            //      而多试一次的代价是零。

            // ---- ①-b 🆕 `After receiving a Dark Pact, <正文>`（**没有 `When` 前缀**）----
            //   2026-09-14 A5 批 2。真卡 3 张：`Chaos Legionary`（deal 2 damage to a random enemy）·
            //   `Meltagun Legionary`（deal 3）· `Aspiring Champion`（gain +1 Ranged Attack and +1 Health）。
            //   语义 = **这张卡自己**收到黑暗契约时触发 ⇒ 走事件层那条 `GetsDarkPact` +
            //   `WhenEvent.SelfOnly`（自指判据在 `WhenEvents.Matches`：`listenerUnit == subject`）。
            //   广播方（`EffectResolver` 里发契约那处）**传了 `subject`**，所以这条挂得上。
            if (CanListenForEvents)
            {
                WhenEvent dpEv; string dpBody; List<EffectOp> dpOps;
                if (TryParseAfterDarkPact(s, out dpEv, out dpBody, out dpOps))
                {
                    foreach (var o in dpOps) if (o.Source == null) o.Source = Name + "：" + dpBody;
                    _whenTriggers.Add(new WhenTrigger { Ev = dpEv, Ops = dpOps, Body = dpBody });
                    return;
                }
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
        /// `When &lt;事件&gt;, &lt;正文&gt;` → 事件表 + 正文原文 + 正文 op。**两边都成功才算**（否则 false）。
        ///
        /// **抽出来只有一个理由**（2026-09-14 A5 批 1）：`EffectText.Coverage` 要问
        /// 「这一句是不是已经由**事件层**接手了」—— 那必须用**同一个判据**，
        /// 不能在那儿写第二份（写两份 = 报告说「不认识」而机制其实在跑，正是 A5 那个坑）。
        ///
        /// ⚠️ 与 <see cref="AddWhenTrigger"/> 的调用方约定一致：
        /// **事件认不出（`evs` 空）或正文解析不出（`ops` 为 null/空）都算没收** ——
        /// 理由见 `WhenEvent.cs` 文件头 ⚠️①：注册一条永远不会被消费（或认错时机）的效果 = 骗玩家。
        /// </summary>
        static bool TryParseWhenSentence(string seg, out List<WhenEvent> evs,
                                         out string body, out List<EffectOp> ops)
        {
            evs = null; body = null; ops = null;
            if (string.IsNullOrEmpty(seg)) return false;
            string s = seg.Trim();

            // ⚠️ **`Whenever` 就是 `When`**（2026-09-14 A5 批 2）：卡面两种写法都有，
            //    `Neurotyrant` 用的就是 `Whenever you play a non-Ephemeral Stratagem, …`。
            //    参考实现的 `WHEN_CONDS` 也是同一个正则里 `when(ever)?` 一起收的。
            int off;
            if (s.StartsWith("Whenever ", System.StringComparison.OrdinalIgnoreCase)) off = 9;
            else if (s.StartsWith("When ", System.StringComparison.OrdinalIgnoreCase)) off = 5;
            else return false;

            // ---- ① 正常形状：`When <事件>, <正文>` ----
            int comma = s.IndexOf(',');
            if (comma > off)
                return FinishWhenSentence(s.Substring(off, comma - off), s.Substring(comma + 1).Trim(),
                                          out evs, out body, out ops);

            // ---- ② 🆕 **缺逗号**的形状（2026-09-14 A5 批 2）----
            // 全池只有 1 条：`Eliminator Sergeant` 的 `When you deploy an Eliminator give it Stealth`
            // （OCR/排版丢了那个逗号）。没有分隔符就只能**试切点**：从左往右在每个词边界切一刀，
            // **事件短语与正文两边都解析得出**才认 —— 判据交给两个现成解析器
            // （`WhenEvents.ParseAll` / `EffectText.Parse`），**一行新文法都不写**。
            // ⚠️ 先切出来的那些（`you` / `you deploy`…）会各有一边解析失败，所以不会误收；
            //    都切不出来就照旧判不认识（诚实优先）。
            for (int i = off + 1; i < s.Length - 1; i++)
            {
                if (s[i] != ' ') continue;
                if (FinishWhenSentence(s.Substring(off, i - off), s.Substring(i + 1).Trim(),
                                       out evs, out body, out ops)) return true;
            }
            evs = null; body = null; ops = null;
            return false;
        }

        /// <summary>
        /// `TryParseWhenSentence` 的后半段：**事件短语与正文两边都解析得出**才算成立。
        /// 抽出来只为一件事：<see cref="TryParseWhenSentence"/> 的「正常形状」与「缺逗号试切点」
        /// 两条路要**共用同一份判据**（写两份必然漂移）。
        /// </summary>
        static bool FinishWhenSentence(string evPhrase, string bodyText,
                                       out List<WhenEvent> evs, out string body, out List<EffectOp> ops)
        {
            evs = null; body = null; ops = null;
            body = bodyText;
            // ⚠️ **用 `ParseAll`，不用 `Parse`**：卡面有一句话点名两件事的写法
            //    （`When you create **or play** a Secret, …`），`Parse` 只回第一条 ⇒ 只接半边。
            evs = WhenEvents.ParseAll(evPhrase);
            if (evs == null) { body = null; return false; }
            // 🆕 2026-09-13 A3：事件**带宾语**时告诉解析器一句 —— 正文里**裸写的 `adjacent`**
            //    锚点要定成**那个宾语**（`Long Fang` 的 `deal 3 damage to adjacent enemies`），
            //    不是「自己」。不传的话它会走 `FillAdjacentAnchors` 的 ④ = 相对自己 ⇒ **静默错打**。
            bool evHasTarget = false;
            foreach (var e in evs)
                if (e.TargetCriteria != null || e.TargetOwnerIs != -1) { evHasTarget = true; break; }
            ops = body.Length > 0 ? EffectText.Parse(body, out _, out _, evHasTarget) : null;

            if (evs.Count == 0 || ops == null || ops.Count == 0) { evs = null; body = null; ops = null; return false; }
            return true;
        }

        /// <summary>
        /// `After receiving a Dark Pact, &lt;正文&gt;`（**没有 `When` 前缀**）→ 事件条件 + 正文 + op。
        /// 2026-09-14 A5 批 2。**只此一处**：`AddWhenTrigger`（注册）与 <see cref="HandledByOtherLayer"/>
        /// （覆盖率判据）都转调它，免得写两份、报告和机制打架。
        ///
        /// **为什么这族要单开一个抽取函数**：它不是 `When &lt;事件&gt;, …` 形状 ——
        /// 卡面把事件写成了**介词短语**（`After receiving a Dark Pact`），主语省略。
        /// 省略的主语在卡面上就是**这张卡自己** ⇒ `SelfOnly`（和 `When deployed, …` 同一条路）。
        ///
        /// ⚠️ **事件名必须仍然是「解析得出」的那一个**（`GetsDarkPact`）——
        ///    `EffectResolver` 发契约那处已经在广播它了，这里只是**换个写法挂上去**。
        ///    认不出的（正文解析失败）一律 `return false`，保持「认不出就认不出」的诚实。
        /// </summary>
        static bool TryParseAfterDarkPact(string seg, out WhenEvent ev, out string body,
                                          out List<EffectOp> ops)
        {
            const string head = "after receiving a dark pact";
            ev = null; body = null; ops = null;
            if (string.IsNullOrEmpty(seg)) return false;
            string s = seg.Trim();
            if (s.Length <= head.Length) return false;
            if (!s.StartsWith(head, System.StringComparison.OrdinalIgnoreCase)) return false;

            string rest = s.Substring(head.Length).TrimStart();
            if (rest.StartsWith(",")) rest = rest.Substring(1).TrimStart();
            if (rest.Length == 0) return false;

            body = rest;
            ops = EffectText.Parse(body, out _, out _);
            if (ops == null || ops.Count == 0) { ev = null; body = null; ops = null; return false; }
            ev = new WhenEvent { Kind = WhenEvent.GetsDarkPact, SelfOnly = true };
            return true;
        }

        /// <summary>
        /// 这一句**是不是已经由另一个层接手了** —— 所以 `EffectText` 的覆盖率**不该**把它报成
        /// 「完全不认识 / 半懂」。返回接手的那个层的名字（报告里要显示），没有则 null。
        ///
        /// 🆕 2026-09-14 A5 批 1。**为什么需要它**：单位卡有三族句子**机制一直在跑**，
        /// 而 `EffectText` 认不出「那句壳」，于是它们一直挂在
        /// `_tmp_view/unit_desc_unparsed.txt` 的 ① 栏里 ⇒ **报表虚低、下一个人会去做已经做完的事**：
        ///   · `When &lt;事件&gt;, &lt;正文&gt;` —— 走**事件层**（`AddWhenTrigger` → `WhenTriggers` →
        ///     `BroadcastWhen`）。⚠️ 它**只看正文那半句**，从不看 `When` 这个壳。
        ///   · `Talent: &lt;名&gt;` —— 走 `TalentName` + `RuleCore.SpawnTalents`（规则书 `:218`）。
        ///   · `Companion N: &lt;卡名&gt;` —— 走 `CompanionName` + `RuleCore.PlayCard`。
        ///
        /// ⚠️ **判据全部转调现有的抽取函数**（`TryParseWhenSentence` / `ExtractTalent` /
        ///    `ExtractCompanionName`），**一行新文法都不写** —— 否则又是「两处写同一条规则」。
        /// ⚠️ 事件那一族**只对单位卡成立**（`CanListenForEvents => IsUnit`）；非单位卡的那句
        ///    `When …` 是**手牌陷阱**，走 `EffectText.SplitHandTrapWhen` 那条路（另一层）。
        /// </summary>
        public static string HandledByOtherLayer(CardDef c, string seg)
        {
            if (c == null || string.IsNullOrEmpty(seg)) return null;

            if (c.CanListenForEvents)
            {
                List<WhenEvent> evs; string body; List<EffectOp> ops;
                if (TryParseWhenSentence(seg, out evs, out body, out ops)) return "事件层（WhenTriggers）";
                // 🆕 `After receiving a Dark Pact, …`（无 `When` 前缀，2026-09-14 A5 批 2）——
                //    同一层（`WhenTriggers`），只是卡面把事件写成介词短语。判据转调同一个抽取函数。
                WhenEvent dpEv; string dpBody; List<EffectOp> dpOps;
                if (TryParseAfterDarkPact(seg, out dpEv, out dpBody, out dpOps)) return "事件层（WhenTriggers）";
            }
            if (ExtractTalent(seg) != null) return "天赋（TalentName）";
            if (ExtractCompanionName(seg) != null) return "伴生（CompanionName）";
            return null;
        }

        /// <summary>
        /// 本卡**当某件事发生时**该结算的那串 op。没有就是 null。
        ///
        /// <paramref name="who"/> = 发生事件的那个单位归谁（`-1` 无归属）；
        /// <paramref name="card"/> = 那个单位的卡。
        /// <paramref name="listenerUnit"/> / <paramref name="subject"/> = 判**自指**
        /// （`When deployed, …`）用的两头，见 <see cref="WhenEvent.SelfOnly"/>。
        /// 判据全在 <see cref="WhenEvents.Matches"/>（**只此一份**，见那个文件的文件头 ⚠️②）。
        /// </summary>
        public List<EffectOp> FireWhen(string kind, int listener, int who, CardDef card,
                                       UnitState listenerUnit = null, UnitState subject = null,
                                       UnitState actor = null, UnitState target = null, int targetWho = -1)
        {
            if (kind == null || _whenTriggers.Count == 0) return null;
            List<EffectOp> acc = null;
            foreach (var t in _whenTriggers)
            {
                if (t.Ev == null || t.Ev.Kind != kind) continue;
                if (!WhenEvents.Matches(t.Ev, listener, who, card, listenerUnit, subject,
                                        actor, target, targetWho)) continue;
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

        /// <summary>
        /// 这张卡**能不能当事件监听者** —— 也就是 `When &lt;事件&gt;, …` 收下来的监听器**有没有人会消费它**。
        ///
        /// 🔴 **2026-09-13 候选 E 加**。判据不是「卡面写没写 `When`」，而是
        /// **「它会不会作为 <see cref="UnitState"/> 出现在棋盘上」** —— 因为
        /// <c>EffectResolver.BroadcastWhen</c> 收集监听者时**只扫棋盘上的单位**
        /// （`ctx.Players[p].Board[s]`），别处一概不看。
        /// ⇒ **战术卡 / 防御卡 / 督军以外的一切非单位卡**，它的 `WhenTriggers` 是**一条永远没人消费的监听器**。
        ///
        /// 后果不只是浪费：它会进 <see cref="WhenEvents.UnknownPhrases"/> 那个报告桶，
        /// 于是**自检的「认不出的短语」清单里多出一条根本不该存在的项** ——
        /// 实测就是 `played`（`Reconnaissance Mission`，DarkAngels **战术卡**）：
        /// 它的卡面是 `Choose a card in your opponent's hand. When played, gain 3 Quest Points`，
        /// 而那句 `When played, …` **本来就已经会被结算**（`EffectText` 按 `.` 切句 →
        /// `Re gain` 认得「gain 3 Quest Points」→ 主语 `when played,` 根本不参与）。
        /// ⇒ 它**不是漏做，是误报**：报告让我们去补一条根本不该有的监听器。
        ///
        /// ⚠️ **为什么不给 `played` 单独开一个解析分支**：那会**正中陷阱** ——
        ///   认出来了、监听器也注册了，而这张卡永远不在棋盘上 ⇒ 又是一条永远不响的监听器，
        ///   而且**卡面不打 `*`、不报错**（正是本工程红线里的静默失效）。
        ///   `WhenEvent.SelfOnly` 的注释里早就把这条坑写死过。
        /// ⚠️ 所以正确的做法是**在源头判卡类**（本属性），让它**既不注册、也不进报告桶** ——
        ///   这是**结构性**的修，不是一个会随时间失效的白名单。
        /// </summary>
        public bool CanListenForEvents { get { return IsUnit; } }
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
        /// **群体**（兽人 Goff 一族，2026-09-13 第三十四轮）：**本单位执行近战攻击后**触发。
        ///
        /// 规则书 `:193`「友方部队执行近战攻击后触发效果」。实测卡面全是 `Mob: &lt;效果&gt;` 挂在
        /// **近战单位自己身上**（`Skarboy Nob` = `Mob: Gain +1 Attack` ·
        /// `Shoota Boy` = `Mob: Deal 1-2 damage to a random enemy`，Goff 一族 15 张），
        /// 语义就是「**这张卡出手打人之后**做一件事」—— 和 <see cref="Strike"/> 同形，
        /// 差别只在**限定近战**（远程不算）。
        ///
        /// **原版出处**：`CardScript__ResolveUnitAttacked`（`param_4 == AttackTypes.Melee`）——
        /// 攻击者有 trait **920**(mob) 时 ①对**攻击者自身**发触发 **300**(Mob)；
        /// ② `BroadcastUnitMob` 再通知**除攻击者外**的所有卡（发触发 **645** OtherCardMob）。
        /// ⚠️ **我们只做第 ① 支**：第 ② 支的听众实测只有一张（`Big Choppa Nob` 的
        ///    `When a friendly unit triggers Mob, it triggers an additional time`），
        ///    而「**再触发一次**」这种语义我们的效果文法表达不了（正文解析不出就不会注册）。
        /// </summary>
        public const string Mob = "mob";

        /// <summary>
        /// **团**（星界军 AstraMilitarum，2026-09-13 第三十四轮）：**本单位执行远程攻击后**触发。
        ///
        /// 规则书 `:202`「友方单位执行**远程**攻击时触发效果」—— 和 <see cref="Mob"/>（`:193`，
        /// 「友方部队执行**近战**攻击后触发效果」）**是成对的两条**，差别只在近战 / 远程。
        /// 实测卡面同样是 `Regiment: &lt;效果&gt;` 挂在自己身上（`Kasrkin` =
        /// `Regiment: Deal 1 damage to a random enemy. If it dies, draw a card` ·
        /// `Autocannon Squad` = `Regiment: Gain +1 Ranged Attack`，AstraMilitarum 一族 14 张）。
        ///
        /// ⚠️ **规则书一条写「后」一条写「时」**（`:193` 是「后」、`:202` 是「时」）——
        ///    我们没有能分辨这个差别的依据 ⇒ **两条都放在同一个位置**（伤害结算完、`Slay`/`Strike` 之后）。
        ///    这是**我们挑的**，如实标着。
        /// </summary>
        public const string Regiment = "regiment";

        /// <summary>
        /// **不稳定**（兽人 Goff 一族，2026-09-13 第三十四轮）：**本单位死亡时对随机单位造成 1-3 伤害**。
        ///
        /// 规则书 `:221`「本单位死亡时：对随机单位造成 1-3 伤害」。
        /// ⚠️ **它没有卡面正文** —— 实测卡面只写裸关键词（`Unstable. Blast 3` / `kw=['Unstable']`），
        ///    所以**效果完全由规则定义**，不进 `RoutableTriggers`（没有 `Unstable:` 那种正文可收）。
        /// 目标池与伤害范围照 `rule_core.gd:4578 _unstable_blast`：**场上随机单位（含双方）**、`randi_range(1,3)`。
        /// 结算在 `RuleCore.CleanupDeaths`，**排在 `Backlash` 之前**（`rule_core.gd:4529` 就是这个顺序）。
        /// </summary>
        public const string Unstable = "unstable";

        /// <summary>
        /// **狂喜 X**（帝皇之子 EmperorsChildren）—— ⏸ **2026-09-13 第三十四轮评估后**推迟**，没做**。
        ///
        /// 规则书 `:182`「狂喜 X：本单位生命降至 X 或以下未死亡时触发效果」，
        /// 卡面写法是 `Ecstasy N: &lt;效果&gt;`（`Terminator` = `Ecstasy 2: Gain +2 Attack`）。
        /// **机制本身好写**（时机点 `RuleCore.Hurt` 里就有），卡住的是 **X 这个数拿不到**：
        ///
        ///   ① **卡表里的关键词值不可靠** —— 实测 `Terminator` 的 `keywords` 是**裸 `Ecstasy`**
        ///      （值会被当成 1），而卡面写的是 **2**；`Maulerfiend` 更连 `Ecstasy` 都不在 `keywords` 里、
        ///      只在 `desc` 有 `Ecstasy 5`。⇒ 直接读 `KwValue` 会**静默用错阈值**。
        ///   ② **`AddTriggerOp` 收不下这种正文**：它拿 `:` 前面那整段和触发名**严格相等**比对，
        ///      `"ecstasy 2"` 对不上 `"ecstasy"` ⇒ `Ecstasy N:` 的正文**根本不会被收**。
        ///
        /// ⚠️ **要做得先解这两条**（改卡表数据，或者让 `AddTriggerOp` 认「名字 + 可选数值后缀」
        /// 并把数值取回来）。**半做 = 静默用错阈值**，所以这一版宁可标成没实现（卡面照旧打 `*`）。
        /// 见 `资料/阵营推进_清单与交接.md` §五。
        /// </summary>
        public const string Ecstasy = "ecstasy";

        /// <summary>
        /// **残忍**（帝皇之子 EmperorsChildren，2026-09-13 第三十四轮）：
        /// **你的回合里、敌方单位受到伤害但未死亡时**激活效果。
        ///
        /// 规则书 `:178`「你的回合敌方单位受伤害未死亡时激活效果」。
        /// 卡面写法 `Cruelty: &lt;效果&gt;`（`Slaanesh's Spawn` = `Cruelty: Gain +1 and Flank`）。
        /// ⚠️ 触发的是**己方**带 `Cruelty` 的牌（挨打的是对面那个），所以它**不是**
        ///    挨打单位自己的触发 —— 和 `Penitence`（挨打那个自己触发）是**两条**，别合并。
        /// </summary>
        public const string Cruelty = "cruelty";
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

        /// <summary>
        /// **巧技**（`Artifice`，2026-09-13 A2）：**每次你打出战术卡时**触发额外效果。
        ///
        /// 规则书 `:168`「每次打出战术时触发额外效果」。听众是**你那一排**带该关键词的单位
        /// ⇒ 走 `FireTriggerOnSide`（和 <see cref="Cruelty"/> 同一支）。
        ///
        /// 卡面**两种写法都有**（实测 15 张：`Artifice: …` 6 张、**正文裸写** 9 张）：
        ///   · 有前缀：`Nexos` = `Artifice: Your units gain +1 Attack until your next turn`
        ///   · 裸写：`Dark Apostle` = `Give a random Dark Pact to each friendly troop`（`kw=['Artifice']`）
        /// ⇒ 「没有前缀时整条 desc 就是正文」那条规则（<see cref="CollectBareKeywordBody"/>）
        ///    就是为这一族加的。
        ///
        /// ⚠️ **触发点在我们这边是挑的**：原版反编译里 `BroadcastTacticPlayed` 在效果**之前**、
        ///    `BroadcastUnitSynapse` 在之后，而巧技该挂哪一处**没有依据** ⇒ 我们排在
        ///    「效果结算 + 进弃牌堆」**之后**，如实标着。
        /// </summary>
        public const string Artifice = "artifice";

        // ---- 2026-09-13 A2：**一整族「替代行动 / 别处触发」的关键词** ----
        // 名字先集中在这里（卡面写法是 `关键词: 正文`，或者**裸写正文**），机制逐个做。
        // 「已实现」以 `Implemented` 为准 —— 这几个常量存在**不代表机制做完**。

        /// <summary>**议程**（暗黑天使）：`以触发效果代替攻击`（规则书 `:165`）。</summary>
        public const string Agenda = "agenda";
        /// <summary>**狂暴**（太空野狼）：`快速非战斗行动；执行时触发效果，然后洗回牌库`（规则书 `:186`）。
        /// ⚠️ 卡面核对：13 张里**只有 9 张真带这个词**，另 4 张只是正文里提到它（见 §一之三·候选 E）。</summary>
        public const string Ferocity = "ferocity";
        /// <summary>**祈祷**（修女会）：`缓慢非战斗行动`（规则书 `:198`）。
        /// ⚠️ 规则书对「缓慢」**没有定义段**（全书没有），唯一已知差别是「本回合能不能动」那条（`:98`）。</summary>
        public const string Pray = "pray";
        /// <summary>**职责**（星界军）：`一次性能力；可由其他卡牌效果"装填"再次使用`（规则书 `:181`）。
        /// ⚠️ 规则书**没说是本局一次还是每回合一次**；`Duty:` 在卡池里 0 张（方括号是我们数据层的占位）。</summary>
        public const string Duty = "duty";
        /// <summary>**典籍**（极限战士）：`你的能量为 0 时触发效果`（规则书 `:175`）。
        ///
        /// 🆕 2026-09-14 A4 批 4 补上这个常量 —— 原来它**只作为一个字面量**住在
        /// <see cref="Implemented"/> 里（`"codex"`），<see cref="CardDef.RoutableTriggers"/> 里
        /// **没有它** ⇒ `TriggerOps("codex")` **恒为 null**（正文压根没被收）。
        /// 见 `资料/战术卡剩余7条_语义查证.md` §八。</summary>
        public const string Codex = "codex";
        /// <summary>**起义**（基因窃取者）：`本单位之后部署的部队，在其部署当回合触发能力`（规则书 `:222`）。</summary>
        public const string Uprising = "uprising";
        /// <summary>**传送**（暗黑天使）：`当回合从牌库抽到即打出时触发能力`（规则书 `:219`）。</summary>
        public const string Teleport = "teleport";
        /// <summary>**激励**（帝皇之子）：`被战术选中时、结算前：触发能力`（规则书 `:212`）。</summary>
        public const string Stimulation = "stimulation";
        /// <summary>**伏击**（基因窃取者）：`面朝下打出；下次回合前若被伤害：翻开无效果；若未被伤害：翻开并触发效果`（规则书 `:166`）。</summary>
        public const string Ambush = "ambush";

        /// <summary>
        /// **伴生 X**（`Companion X`，2026-09-13 A2）：**从手牌打出时，可打出至多 X 张其伴生部队**
        /// （规则书 `:176`）。
        ///
        /// 卡面写法是 `Companion N: &lt;部队名&gt;`（`Companion 2: Missile Drone` /
        /// `Companion 1: Gun Drone` / `Companion 2: Guardian Drone` …），
        /// ⚠️ **名字有时只在 `desc` 里**（`Enforcer Battlesuit` 的 `keywords` 只有裸 `Companion`，
        ///    数字和名字都在 `desc`）⇒ <see cref="CompanionName"/> 两个来源都扫。
        /// ⚠️ 实测只有 7 张（TauEmpire 一族）。
        /// </summary>
        public const string Companion = "companion";

        /// <summary>
        /// **虫群**（`Swarm`，2026-09-13 A2）：**打出在同名部队左侧时合并**（置于其下、攻击生命相加）。
        ///
        /// 规则书 `:216`。语义照原版反编译 `decomp_out/CardScript__ResolveCardPlayed.c`：
        ///   · 只看**右边的紧邻格**（`BattleManager.GetAdjacentUnitRight`）—— 不是全盘找同名；
        ///   · 判据是**卡名全等**（`System_String__op_Equality`）；
        ///   · 合并走 `BattleManager.AddExecuteSwarm`，**位置在召唤触发之后**
        ///     （同一个函数里 `ResolveUnitSummoned` 在前）⇒ 新来那张自己的 `Rally` 照常触发。
        /// 🔴 **卡面核对：真带这个关键词的是 11 张**（另 2 张只是在正文里提到它 ——
        ///    `Norn Emissary` 卡面只印 `Armour 1. Blast 3.`；`Swarming Masses` 是战术卡。
        ///    OCR 把「提到」当成了「拥有」，见 §一之三·候选 E）。
        /// </summary>
        public const string Swarm = "swarm";

        /// <summary>
        /// **突触**（`Synapse`，2026-09-13 A2）：**被友方战术选中时，对相邻部队/单位重复效果**。
        ///
        /// 规则书 `:217`。五条语义照原版反编译
        /// （`decomp_out/CardScript__TargetedSpellPlayed.c:55-75`），逐条写在
        /// `EffectResolver.PlayTactic` 的调用点 + `RepeatTacticOnAdjacent` 上。
        /// 要点：**只有友方战术**（施放者与目标同一方）· 相邻用 `BoardSpec.AdjacentSlots` ·
        /// 邻居要**再过一次这张战术自己的目标筛选**（`IsLegalPick`）· 然后**把这张战术再跑一遍**。
        ///
        /// 🔴 **原版没有 `CardTraitSynapse`** —— 它走的是硬编码机制路线
        /// （`DefinedTrait:113` + `AbilityTrigger:74-75` + `CardScript.synapseCard`），
        /// 和 `swarm` 一样（`CardTrait` 的子类只有 Duty/Ferocity/Oath 三个）。
        /// 🔴 **卡面核对：真带这个关键词的是 13 张**（OCR 多算 1 张，见 §一之三·候选 E）。
        /// </summary>
        public const string Synapse = "synapse";

        /// <summary>
        /// **潮涌 X**（`Tide X`，2026-09-13 A2）：**从手牌打出时，本回合可再打出 X 张复制**
        /// （费用与首张相同）。
        ///
        /// 规则书 `:220` + 英文原版 `:369`。⚠️ **复制品是临时卡** —— 规则书 `:229` 把
        /// 「带潮涌的复制」与天赋/伴生**并列**，回合结束未打出即从游戏中移除 ⇒ 每张都要
        /// `MarkEphemeral`（**卡面没印 `Ephemeral`**，不标就会赖在手里）。
        /// ⚠️ 它**没有卡面正文**（卡上只印 `Tide 2` 这种）⇒ 不进 `RoutableTriggers`。
        /// 🔴 **取值口径**：X 在关键词后面（`Tide 1` / `Tide 2` …），用 `KwValue` 读。
        /// </summary>
        public const string Tide = "tide";

        /// <summary>
        /// **残骸**（`Remnant`，2026-09-13 A2）：**本部队死亡时翻面表示残骸；残骸受伤害或
        /// 控制者回合结束时被摧毁**（规则书 `:203`）。
        ///
        /// 原版出处：残骸在场上是一个**独立的 3D 体**
        /// （`BattleCardUI.CreateRemnantBody` / `DestroyRemnantBody` / `RemnantBody3D`、
        /// `BattleManager.AddTransformIntoRemnant` / `AddTransformFromRemnant`），
        /// 而且**算「场上」** —— `BattleManager.GetEnemyMinionsAndRemnantInPlay` 拿它当目标候选。
        ///
        /// 我们这边 = `UnitState.IsRemnant`（留在格位、攻 0、1 血、挨一下就碎）。
        /// 配套：`RuleCore.CleanupDeaths`（翻面 / 残骸被摧毁）、`RuleCore.EndTurn`（回合结束摧毁）、
        /// `EffectResolver.DoReanimate`（**从场上的残骸翻回来** —— 卡面全写
        /// `Reanimate a friendly Remnant`，见那 16 张）。
        /// 🔴 卡面核对：**28 张**真带这个词（OCR 表把「提到」也算上了，见关键词频次表的口径）。
        /// </summary>
        public const string Remnant = "remnant";

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
            // 群体（2026-09-13 第三十四轮）：**近战攻击后**触发自己那条 `Mob:` 正文。
            // ⚠️ 原版还有「通知除攻击者外的所有卡」那一支（触发 645），**没做** —— 见 `Mob` 的注释。
            Mob,
            // 团（2026-09-13 第三十四轮）：和 `Mob` **成对**，差别只在**远程**。
            Regiment,
            // 不稳定 / 残忍（2026-09-13 第三十四轮）—— 都是「**在已有的时机点读一个关键词**」：
            //   · `Unstable` → `RuleCore.CleanupDeaths`（死亡时 1-3 随机伤害；**卡面没有正文**，纯规则）
            //   · `Cruelty`  → `RuleCore.Hurt`（**己方回合**、**敌方**挨打未死 → **己方**带该词的牌触发）
            // ⚠️ **`Ecstasy` 不在这里** —— 同一轮评估后**推迟了**：机制好写，但阈值 X 拿不到
            //    （卡表的关键词值不可靠 + `AddTriggerOp` 收不下 `Ecstasy N:` 这种正文）。
            //    原因逐条写在 `Ecstasy` 那个常量上。**半做会静默用错阈值**，所以宁可不做。
            Unstable, Cruelty,
            // 巧技（2026-09-13 A2）：**每次你打出战术时**触发自己那条正文。
            // 时机点在 `EffectResolver.PlayTactic` 末尾（效果结算 + 进弃牌堆之后，**我们挑的**）。
            Artifice,
            // 替代行动那一族（2026-09-13 A2）：`Duty` / `Pray` / `Ferocity` / `Agenda` ——
            // **花掉本单位一次行动**换一个效果（规则书 `:150`），走 `RuleCore.UseAlternative`。
            // 逐关键词的差别（部署当回合能不能动 / 一次性 / 洗回牌库）写在 `AlternativeActions` 上面那张表。
            // ⚠️ 以前这四条是**用自定的 `Ability:` 关键词近似的** —— 那条近似会让
            //    `When a friendly unit prays` 在放 Duty 时也响。现在拆开了，`Ability` 仍保留
            //    （有卡在用），但**两者不再互相冒充**。
            Duty, Pray, Ferocity, Agenda,
            // 虫群（2026-09-13 A2）：**打出在右侧同名部队旁边时合并**（攻击生命相加、新来的压在下面）。
            // 时机点在 `RuleCore.PlayCard` 末尾（召唤触发之后 —— 照原版同一函数里的先后）。
            // ⚠️ 它**没有卡面正文**（卡上只印裸关键词），所以不进 `RoutableTriggers`；
            //    但会广播 `When a friendly unit triggers Swarm`（那一族卡要它）——
            //    广播走 `BroadcastKeywordEvent`（体无正文也能发），不是 `FireTriggerAt`。
            Swarm,
            // 突触（2026-09-13 A2）：**被友方战术选中时，对相邻单位把那张战术再跑一遍**。
            // 时机点在 `EffectResolver.PlayTactic`（效果之后、进弃牌堆之前 —— 照原版同一函数里的先后）。
            Synapse,
            // 起义（2026-09-13 A2）：**之后每部署一个部队时**触发自己那条 `Uprising:` 正文。
            // 时机点在 `RuleCore.PlayCard`（`ResolveDeploy` 之后、`Rally` 之前）。
            // ⚠️ 卡面**两种写法都有**：带前缀（`Neophyte Initiate` = `Uprising: Deal 1 damage…`，
            //    卡图核对过）和正文裸写（`Hybrid Metamorph` 的 OCR 掉了前缀）—— 后者走
            //    `CollectBareKeywordBody`。**裸写那种的正文归属是「本卡唯一的带正文关键词」**，
            //    所以 `Uprising` 必须在 `BodyKeywords` 里。
            Uprising,
            // 激励（2026-09-13 A2）：**被战术选中时、结算前**触发自己那条正文。
            // 时机点在 `EffectResolver.PlayTactic`（效果结算**之前** —— 规则书 `:212` 写死了「结算前」）。
            Stimulation,
            // 潮涌（2026-09-13 A2）：**从手牌打出时往手里塞 X 张复制**（本回合可打、回合末消失）。
            // 时机点在 `RuleCore.PlayCard`（部署之后、和别的部署钩子排在一起）。
            Tide,
            // 残骸（2026-09-13 A2）：**死亡时翻面留在格位上**；受伤害或控制者回合结束时被摧毁；
            // `reanimate` 从**场上的残骸**翻回来。落点：`RuleCore.CleanupDeaths`（翻面 / 被摧毁）·
            // `RuleCore.DestroyRemnants`（回合结束）· `EffectResolver.DoReanimate`（翻回来）。
            Remnant,
            // 传送（2026-09-13 A2）：**当回合从牌库抽到的**那张，打出来时触发自己那条 `Teleport:` 正文。
            // 判据 = `PlayerState.DrawnThisTurn`（`Draw` 记账、`PlayCard` 扣一份、`BeginTurn` 清零）。
            // ⚠️ 卡面**两种写法都有**：带前缀（`Teleport: Gain Vanguard and attack by itself`）和
            //    关键词条目里自带正文（`Deathwing Knight` 的 `keywords` 就是 `Teleport: …`）—— 后者
            //    `AddTriggerOp` 收得到（它扫 `keywords` 每一条）。裸写那种走 `CollectBareKeywordBody`。
            Teleport,
            // 伏击（2026-09-13 A2）：**面朝下打出**；挨到伤害就翻开（无效果），
            // 撑到控制者下个回合就翻开**并触发**自己那条 `Ambush:` 正文。
            Ambush,
            // 伴生（2026-09-13 A2）：**从手牌打出时带出至多 X 张同名伴生部队**（`RuleCore.PlayCompanions`）。
            // ⚠️ 原版是「**可**打出」（玩家选），我们**自动带满** —— 近似，见那个函数的注释。
            // ⚠️ 它**没有卡面正文**（`Companion 2: Missile Drone` 只是个名字）⇒ 不进 `RoutableTriggers`。
            Companion,

            // ---- 2026-09-13 第三十四轮：**名字挂在「未实现」名单上、其实早就有机制**的三个 ----
            // 派子代理逐条核了那 23 个「未实现」关键词的代码，查出这三个是**误报** ——
            // 机制一直在，只是**没登记进这个集合** ⇒ 名单多报 3 个、卡面还白打 `*`。
            // ⚠️ 判据是「**代码在那个时机真的读了/做了那件事**」——
            //    「`Prefixes` 认得出」和「`GivePayload` 能把它写进单位」**都不算**。
            /// <summary>典籍：**你的能量为 0 时**触发效果（规则书 `:175`）。
            /// 机制在 `EffectText` 的 `Codex:` 前缀分支（给后续 op 挂 `EnergyZero` 条件）
            /// + `EffectResolver` 的消费点（判结算瞬间能量 == 0）。自检里一直有一条在过。
            /// ⚠️ 2026-09-14 A4 批 4：常量搬去 <see cref="Codex"/>（`RoutableTriggers` 要引用它）。</summary>
            Codex,
            /// <summary>誓言：**部署时支付 X 能量以触发效果**（规则书 `:194`）。
            /// 实现成**付费前缀** `Oath N:`（`EffectText` 认它并记 `CostKind="oath"`，
            /// 结算层判够不够 —— 不够**整条不生效**、够则扣能量）。
            /// ⚠️ **只管效果句那一半**；单位「部署时」那一半没接。</summary>
            "oath",
            /// <summary>眩晕：**无法行动**。两条路都在 ——
            /// ① **效果** `Stun an enemy` → `EffectResolver.DoStun`（置 `IsStunned` + 广播）；
            /// ② **关键词授予** → `UnitState.AddKeyword("stun")` 里同步状态位（`:144`）。
            /// 禁行动判在 `RuleCore.DeclareAttack` / `CanUseAbility`。
            /// ⚠️ **卡面上那 5 个 `Stun` 关键词是数据误抽**（从 `Deal 3 damage to an enemy and Stun it`
            ///    里抽出来的），而卡面关键词是**构造函数直接灌字典、不走 `AddKeyword`** ⇒ 那个本身是死的。
            ///    那 5 张是 `Banshee Mask` / `Hektor Thenmann` / `Malicious Volleys` /
            ///    `Snakebite Grot` / `Toxic Bonfire`。见 `资料/阵营推进_清单与交接.md` §五。</summary>
            "stun",
            /// <summary>天赋：**回合开始时在手牌中生成临时战术**（规则书 `:218`）。
            /// 实测 **80 个天赋名里 72 个在卡池里查得到同名卡**（全是 `tactic`，如 `Witchfire` /
            /// `Path of the Seer` / `Flickerjump`）⇒ 机制 = **去卡池找同名卡塞进手牌 + `MarkEphemeral`**，
            /// 结算在 `RuleCore.SpawnTalents`。
            /// ⚠️ 查不到同名卡的那 8 个会**如实打日志**（名单见 `CardDef.TalentName` 的注释），不静默。</summary>
            "talent",
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
            // 🔴 2026-09-13 A3 补（`资料/关键词三列对账.md` §三 查出来的漏网）：
            //    规则书有、原版 `DefinedTrait` 里**没有**同名的三个词（`:204` 破坏 / `:210` 灵魂石 /
            //    `:184` 信仰）—— 它们**连 `Prefixes` 都不在**，于是 `Normalize` 返回 null
            //    ⇒ `KeywordTable.Parse` 丢弃（`:1178`）、构造函数也丢弃 ⇒ **两张诊断单都看不见**
            //    （`UnimplementedKeywords` 报不出、卡面也不打 `*`），正是本工程红线里的静默失败。
            //    ⚠️ **这次补的只是「认得出 + 报得出」**，三个词**仍然没有机制**（`Implemented` 里没有）——
            //      现在的行为是「如实报成未实现关键词」，这才是对的。
            //    ⚠️ **量过影响面才加的**（2026-09-13）：卡表里**只有 5 条 `keywords` 条目**会新命中
            //      （`Neophyte Specialist` / `Cult Propaganda` / `Improvised Barricade` /
            //       `Poisoned Supplies` / `Atalan Jackal`，全是 `Sabotage`），
            //      **卡面 `desc` 分句 0 条**会因此被当成「纯关键词声明」跳过 —— 即不改动任何现有解析。
            //      （`spirit stone` / `faith` 目前 0 命中，是**保险**：它们主要是**付费前缀**，
            //        由 `EffectText.CostKindOf` 认，不靠这张表。）
            new[] { "sabotage", "sabotage" },
            new[] { "spirit stone", "spiritstone" }, new[] { "spiritstone", "spiritstone" },
            new[] { "faith", "faith" },
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
