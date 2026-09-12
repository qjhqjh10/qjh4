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
        public bool HasShield;        // 抵挡下一次伤害后失去

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

            // 部署当回合不可行动 —— 除非带**迅捷 / 侧翼**。
            // 规则书 :98「部署当回合不能行动（除非注明，如迅捷/侧翼/狂暴）」、
            // :187「侧翼：打出当回合可攻击任意敌方部队」；原版 `rule_core.gd:2248`
            // 把这两个写在同一句里（`fast` / `flank` → `exhausted = false`）。
            Exhausted = !(Has("fast") || Has("flank"));
            HasShield = Has(KeywordTable.Shield);
        }

        public bool Has(string keyword) { return _keywords.ContainsKey(keyword); }

        /// <summary>场上单位的**主动技能**（来自卡的 `Ability:`）。没有就是 null</summary>
        public EffectSpec Ability { get { return Card != null ? Card.Ability : null; } }

        public bool HasAbility { get { return Ability != null; } }

        /// <summary>这个关键词对应的**触发效果**。没有返回 null —— 调用方必须判</summary>
        public EffectSpec Effect(string keyword) { return Card != null ? Card.Effect(keyword) : null; }

        /// <summary>带了这个关键词、且它带的效果文字解析不出来 → 卡面要标 `*`</summary>
        public bool EffectUnparsed(string keyword)
        {
            if (Card == null || !Card.Has(keyword)) return false;
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
        /// </summary>
        public void RemoveAll(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return;
            _keywords.Remove(keyword);
            if (keyword == KeywordTable.Armour) Armor = 0;
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
