// UnitState.cs — 场上的一个单位（可变实例状态）
//
// 和 `CardDef`（不可变的卡牌定义）分开：同一张卡多次上场是两个独立的 UnitState，
// 掉血/疲劳/增益互不影响。
//
// 字段语义对齐 `rule_core.gd` 的 `_make_unit`。
using System.Collections.Generic;

namespace RuleEngine
{
    public class UnitState
    {
        public readonly CardDef Card;
        public readonly string Name;
        public readonly bool IsWarlord;

        public int Attack;            // 当前近战攻击力
        public int RangedAttack;      // 当前远程攻击力
        public int Armor;             // 伤害减免（最低 1）。**只有 Armour X 关键词给护甲**
        public int Health;
        public int MaxHealth;

        public bool Exhausted;        // 本回合是否已行动（部署当回合 = true）
        public bool IsStunned;
        /// <summary>
        /// **职责已经用过了**（2026-09-13 A2）—— 规则书 `:181`「职责：**一次性能力**；
        /// 可由其他卡牌效果**装填**再次使用」。
        ///
        /// ⚠️ 它**不随回合重置**（和 <see cref="Exhausted"/> 正好相反）—— 这是本局一次的标记。
        /// 装填（`Reload the Duty abilities of all your units`）把它清回 false，
        /// 但那个动词**还没实现**（在 A4 的「不认识的句子」清单里，如实标着）。
        /// </summary>
        public bool DutyUsed;

        /// <summary>
        /// **压在下面那几张牌**（虫群合并来的，2026-09-13 A2）。规则书 `:216`「置于其下」。
        /// 宿主进弃牌堆时它们**一起进**（`RuleCore.CleanupDeaths`）—— 物理上就是「压在下面」。
        /// </summary>
        public readonly List<CardDef> SwarmUnder = new List<CardDef>();

        /// <summary>
        /// **这是一具残骸**（`Remnant`，2026-09-13 A2）—— 规则书 `:203`
        /// 「本部队死亡时**翻面**表示残骸；残骸**受伤害或控制者回合结束时被摧毁**」。
        ///
        /// 残骸是**留在格位上的一个单位**（原版在场上是一个独立的 3D 体：
        /// `BattleCardUI.CreateRemnantBody` / `RemnantBody3D`，`BattleManager.AddTransformIntoRemnant`），
        /// 所以：占着格位、攻 0、**1 点生命**（挨任何一下就没）、不能行动。
        /// ⚠️ 它是**背面朝上的牌**，没有任何能力 ⇒ 被摧毁时**不再触发**它自己的
        ///    `Backlash` / `Unstable`（那些在它「死」的那一次已经触发过了）。
        /// </summary>
        public bool IsRemnant;

        public bool HasShield;        // 抵挡下一次伤害后失去

        /// <summary>
        /// 失明（规则书 :166「本单位失明期间**远程攻击设为 0**」）。
        ///
        /// ⚠️ 和 `stun` 一样，原版是**独立的布尔状态字段**（`rule_core.gd:239` 的 `blind`），
        ///    不是关键词 —— 它靠 `_damage_unit` 那族置位（`:2814`）、`field_attack` 之外读它。
        ///    出处：原版 `:4212` `if is_ranged and bool(attacker.get("blind", false)): return ERR_NO_ATTACK`。
        /// </summary>
        public bool IsBlind;

        /// <summary>
        /// 失明的**到期回合**（`blind_turn_end`，原版 `rule_core.gd:2815`）。
        /// `-1` = 没有失明。语义是「到**施放者自己的下个回合开始**时清」——
        /// 也就是撑过对手的一整个回合（卡面写 `until your next turn`）。
        /// ⚠️ 原版清除时读的是**另一个字段名**（`:1962` 读 `blind_turn`、`:2815` 写 `blind_turn_end`），
        ///    所以原版这条清除**从来没生效过**（`blind` 一旦中上就永久）。我们按卡面语义实现，不照抄这个笔误。
        /// </summary>
        public int BlindTurnEnd = -1;

        /// <summary>失明是谁施放的（0/1）—— 到期按**他的**回合算（见 <see cref="BlindTurnEnd"/>）。
        /// ⚠️ 不能靠「回合号的奇偶」推施放者：那种假设在本工程里没有依据（回合所有权是可变的）。</summary>
        public int BlindOwner = -1;

        public int AttacksThisTurn;   // 重置于回合开始

        readonly Dictionary<string, int> _keywords;

        public UnitState(CardDef card, bool isWarlord)
        {
            Card = card;
            Name = card != null ? card.Name : "?";
            IsWarlord = isWarlord;

            Attack = card != null ? card.Attack : 0;
            RangedAttack = card != null ? card.RangedAttack : 0;
            Health = card != null ? card.Health : 0;
            MaxHealth = Health;

            _keywords = new Dictionary<string, int>();
            if (card != null)
            {
                foreach (var kv in card.Keywords) _keywords[kv.Key] = kv.Value;
            }
            // 护甲只有 Armour X 关键词这一个来源。
            // ⚠️ JSON 里的 `armor` 字段经核实实为**远程攻击**（OCR 误读），已在数据层迁移到 RangedAttack
            Armor = KwValue(KeywordTable.Armour);

            // 部署当回合不可行动 —— 除非带**迅捷 / 侧翼 / 狂暴**。
            // 规则书 :98「部署当回合不能行动（除非注明，如迅捷/侧翼/狂暴）」、
            // :187「侧翼：打出当回合可攻击任意敌方部队」；原版 `rule_core.gd:2248`
            // 把这两个写在同一句里（`fast` / `flank` → `exhausted = false`）。
            // ⚠️ **狂暴（`ferocity`）是 2026-09-13 A2 补进来的** —— `:98` 那句话里
            //    本来就点着它（「如迅捷/侧翼/狂暴」），原版同一处也把它和 fast/flank 并列，
            //    只是我们先前没实现这个关键词。慢的 `pray` **不在**这一行（`:198`）。
            Exhausted = !(Has("fast") || Has("flank") || Has(KeywordTable.Ferocity));
            HasShield = Has(KeywordTable.Shield);
        }

        public bool Has(string keyword) { return _keywords.ContainsKey(keyword); }

        /// <summary>场上单位的**主动技能**（来自卡的 `Ability:`）。没有就是 null</summary>
        public EffectSpec Ability { get { return Card != null ? Card.Ability : null; } }

        public bool HasAbility { get { return Ability != null; } }

        /// <summary>
        /// 这个关键词对应的**触发效果**。没有返回 null —— 调用方必须判。
        /// 卡上原生的那条优先（那是这张卡自己的设计），没有才用运行时挂上去的
        /// （`Give "💀 Backlash: …" to a friendly troop` 那种，见 <see cref="GrantEffect"/>）。
        /// </summary>
        public EffectSpec Effect(string keyword)
        {
            if (string.IsNullOrEmpty(keyword) || Card == null) return null;
            var own = Card.Effect(keyword);
            if (own != null) return own;
            EffectSpec granted;
            return _grantedFx.TryGetValue(keyword, out granted) ? granted : null;
        }

        // ---- 运行时补上的「触发效果」（`Give "💀 Backlash: …" to a friendly troop`）----
        //
        // 原版把这种段落当**卡片自带的一段效果文字**存着，靠 `desc.contains(...)` 现搜现解；
        // 我们没有卡片定义可写（那是不可变的 `CardDef`），所以在单位身上挂一份。
        // 判据：**卡上原生的那条优先**（那是这张卡自己的设计），没有才用后挂上去的。
        readonly Dictionary<string, EffectSpec> _grantedFx = new Dictionary<string, EffectSpec>();

        public void GrantEffect(string keyword, EffectSpec spec)
        {
            if (string.IsNullOrEmpty(keyword) || spec == null) return;
            _grantedFx[keyword] = spec;
        }

        public bool HasGrantedEffect(string keyword)
        {
            return !string.IsNullOrEmpty(keyword) && _grantedFx.ContainsKey(keyword);
        }

        /// <summary>带了这个关键词、且它带的效果文字解析不出来 → 卡面要标 `*`</summary>
        public bool EffectUnparsed(string keyword)
        {
            if (Card == null || !Card.Has(keyword)) return HasGrantedEffect(keyword) == false;
            if (keyword == KeywordTable.Ability) return Card.HasAbility == false;
            return Card.Effect(keyword) == null;
        }

        public int KwValue(string keyword)
        {
            int v;
            return _keywords.TryGetValue(keyword, out v) ? v : 0;
        }

        /// <summary>关键词授予/叠加（`rule_core._apply_gain:3292`：`kws[name] += val`）。
        /// ⚠️ `armour`/`shield`/`stun` 三个还要**同步状态字段** —— 引擎别处是按字段结算的，
        /// 只加 kws 不改字段 = 给了护甲却不减伤（原版 `:3293-3299` 专门补过这个 bug）。</summary>
        public void AddKeyword(string keyword, int value)
        {
            if (string.IsNullOrEmpty(keyword)) return;
            _keywords[keyword] = KwValue(keyword) + value;

            if (keyword == KeywordTable.Armour) Armor += value;
            if (keyword == KeywordTable.Shield && value > 0) HasShield = true;
            if (keyword == "stun" && value > 0) IsStunned = true;
        }

        /// <summary>移除关键词（`lose X` 用）。**值降到 0 以下就摘掉**</summary>
        public void RemoveKeyword(string keyword, int value = 1)
        {
            if (string.IsNullOrEmpty(keyword)) return;
            int now = KwValue(keyword) - value;
            if (now > 0) _keywords[keyword] = now;
            else RemoveAll(keyword);
        }

        /// <summary>
        /// **整个摘掉**（不管叠了几层）。
        /// 规则书里明确说「移除全部」的地方必须用这个 —— 例：`Markerlight X`
        /// 「受远程伤害后**移除全部**标记光」（:192）；用 `RemoveKeyword` 只会减一层。
        ///
        /// ⚠️ 摘掉关键词时**连带撤掉它当初授予的属性增益** —— 见 <see cref="PendingGrants"/>。
        /// </summary>
        public void RemoveAll(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return;
            _keywords.Remove(keyword);
            if (keyword == KeywordTable.Armour) Armor = 0;
            RevertGrantsOf(keyword);
        }

        // ---- 关键词「带出来的」属性增益 ----------------------------------
        //
        // 有些关键词一旦授予就会**顺手改属性**（原版 `KW_GRANT` 那类）。
        // 记下来是为了「关键词被移除时能把增益一起撤走」——
        // 否则 `+2 Melee Attack` 会永远留在身上（标掉了、值还在 = 静默不一致）。
        public class GrantRecord
        {
            public string Keyword;
            public string Attr;        // attack / ranged / health / armour
            public int Value;
            /// <summary>谁给的（黑暗契约的种类名 / 卡名）。用来「整份收回」——见 <see cref="RevertGrantsFrom"/></summary>
            public string Source;
        }

        readonly List<GrantRecord> _grants = new List<GrantRecord>();
        public IReadOnlyList<GrantRecord> PendingGrants { get { return _grants; } }

        /// <summary>
        /// 记一条「这个属性增益是谁给的」，**并立刻加上去**。
        /// `keyword` 非空 = 是某个关键词带出来的（关键词被移除时一起撤）；
        /// `source` = 给予者（比如黑暗契约的种类名），用来在来源被替换时整份收回。
        /// </summary>
        public void RecordGrant(string attr, int value, string source = null, string keyword = null)
        {
            var g = new GrantRecord { Keyword = keyword, Attr = attr, Value = value, Source = source };
            _grants.Add(g);
            ApplyGrant(g, +1);
        }

        /// <summary>撤掉某个关键词带出来的全部属性增益</summary>
        void RevertGrantsOf(string keyword)
        {
            for (int i = _grants.Count - 1; i >= 0; i--)
            {
                var g = _grants[i];
                if (g.Keyword != keyword) continue;
                _grants.RemoveAt(i);
                ApplyGrant(g, -1);
            }
        }

        /// <summary>撤掉**某一个来源**（例如被替换掉的那份黑暗契约）留下的全部属性增益</summary>
        public void RevertGrantsFrom(string source)
        {
            if (string.IsNullOrEmpty(source)) return;
            for (int i = _grants.Count - 1; i >= 0; i--)
            {
                var g = _grants[i];
                if (g.Source != source) continue;
                _grants.RemoveAt(i);
                ApplyGrant(g, -1);
            }
        }

        void ApplyGrant(GrantRecord g, int sign)
        {
            int v = g.Value * sign;
            switch (g.Attr)
            {
                case "attack": Attack += v; break;
                case "ranged": RangedAttack += v; break;
                case "health":
                    Health += v;
                    MaxHealth = System.Math.Max(1, MaxHealth + v);
                    break;
                case "armour": Armor = System.Math.Max(0, Armor + v); break;
            }
        }

        // ---- 限时增益（原版 `temp_buffs`，`rule_core.gd:3300`）----
        //
        // 一条 = 一次**带时长**的施加。到期按两条规则撤：
        //   · `this turn`            → **本回合结束时**撤（不管谁的回合）
        //   · `until your next turn` → **施放者自己的下个回合开始时**撤
        // 出处：`_resolve_text:3018-3019`。
        public class TempBuff
        {
            public bool IsKeyword;
            public string Name;              // 属性名（attack/health/armour/ranged）或关键词名
            public int Value;
            public int Owner;                // 施放者玩家号（`until your next turn` 按他的回合算）
            public bool UntilMyNextTurn;     // true = 施放者的下回合开始撤；false = 本回合结束撤
            public string Src;               // 来源卡名（日志用）
        }

        readonly List<TempBuff> _buffs = new List<TempBuff>();
        public IReadOnlyList<TempBuff> TempBuffs { get { return _buffs; } }
        public void AddTempBuff(TempBuff b) { _buffs.Add(b); }

        /// <summary>
        /// 撤掉到期的限时增益。
        /// </summary>
        /// <param name="atTurnEnd">true = 正在「回合结束」；false = 正在「某玩家回合开始」</param>
        /// <param name="player">回合开始/结束时，是**哪个玩家**的回合</param>
        /// <returns>撤掉了几条</returns>
        public int RevertBuffs(bool atTurnEnd, int player)
        {
            int n = 0;
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                var b = _buffs[i];
                bool due = atTurnEnd ? !b.UntilMyNextTurn : (b.UntilMyNextTurn && b.Owner == player);
                if (!due) continue;
                _buffs.RemoveAt(i);
                if (b.IsKeyword) RemoveKeyword(b.Name, b.Value);
                else
                {
                    switch (b.Name)
                    {
                        case "attack": Attack -= b.Value; break;
                        case "ranged": RangedAttack -= b.Value; break;
                        // 生命是**上限也跟着变**的（原版 `:3317` 同步 max_health），撤的时候一起回
                        case "health": Health -= b.Value; MaxHealth -= b.Value; break;
                        case "armour": Armor = System.Math.Max(0, Armor - b.Value); break;
                    }
                }
                n++;
            }
            return n;
        }

        public bool IsAlive { get { return Health > 0; } }

        /// <summary>每回合开始时调用</summary>
        public void RefreshForNewTurn()
        {
            Exhausted = false;
            AttacksThisTurn = 0;
        }

        public override string ToString()
        {
            return $"{Name} {Attack}/{Health}{(IsWarlord ? " (督军)" : "")}{(Exhausted ? " 疲劳" : "")}";
        }
    }
}
