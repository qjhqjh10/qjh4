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

            Exhausted = true;                              // 部署当回合不可行动（v1 没有 Fast/Flank）
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

        public void RemoveKeyword(string keyword) { _keywords.Remove(keyword); }

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
