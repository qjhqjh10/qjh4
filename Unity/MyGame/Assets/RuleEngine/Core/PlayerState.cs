// PlayerState.cs — 一方玩家的全部状态
using System.Collections.Generic;

namespace RuleEngine
{
    public class PlayerState
    {
        public string Name = "Player";

        public readonly List<CardDef> Deck = new List<CardDef>();
        public readonly List<CardDef> Hand = new List<CardDef>();
        public readonly List<CardDef> Discard = new List<CardDef>();

        /// <summary>战场 9 格。<see cref="BoardSpec.WarlordSlot"/> 上永远是督军，其余为 null 或单位</summary>
        public readonly UnitState[] Board = new UnitState[BoardSpec.Size];

        /// <summary>和 <c>Board[BoardSpec.WarlordSlot]</c> 是**同一个对象**（便于直接取用，别写成两份）</summary>
        public UnitState Warlord;

        public int Energy;
        public int MaxEnergy;

        /// <summary>本方自己的回合计数（能量 = 它 + 1）。**不是全局回合数** —— 见 RuleCore.BeginTurn</summary>
        public int TurnCount;

        /// <summary>
        /// 本方**最近一次回合开始时**的全局回合号（由 `RuleCore.BeginTurn` 写）。
        ///
        /// 用途只有一个：划出 `Choose a friendly troop that died **since your last turn**`
        /// 的窗口 —— 候选 = `DeadUnits` 里 `Owner == 自己 && DeathTurn >= LastTurnStartMark`。
        /// 这样「自己回合里死的」和「对手回合里死的」都落在窗口内，而**上一个回合之前死的**被排除。
        /// 原版 `rule_core.gd:1006-1010` 用「`_died_base_prev` 计数 + 切片」记同一件事。
        /// </summary>
        public int LastTurnStartMark;

        /// <summary>牌库抽空后每抽一次 +1，并让督军挨这么多伤害</summary>
        public int Fatigue;

        public bool IsDefeated { get { return Warlord == null || Warlord.Health <= 0; } }

        public UnitState At(int slot)
        {
            return BoardSpec.IsValid(slot) ? Board[slot] : null;
        }

        /// <summary>场上活着的单位（不含督军）</summary>
        public IEnumerable<UnitState> Units()
        {
            for (int i = 0; i < Board.Length; i++)
            {
                var u = Board[i];
                if (u != null && !u.IsWarlord) yield return u;
            }
        }

        /// <summary>可部署的空格数</summary>
        public int FreeSlots()
        {
            int n = 0;
            for (int i = 0; i < Board.Length; i++)
                if (BoardSpec.IsDeployable(i) && Board[i] == null) n++;
            return n;
        }

        /// <summary>有空的部署格吗</summary>
        public bool HasFreeSlot()
        {
            for (int i = 0; i < Board.Length; i++)
                if (BoardSpec.IsDeployable(i) && Board[i] == null) return true;
            return false;
        }

        public override string ToString()
        {
            return $"{Name} 能量 {Energy}/{MaxEnergy} 手牌 {Hand.Count} 牌库 {Deck.Count} 场 {Board.Length - FreeSlots() - 1}";
        }
    }
}
