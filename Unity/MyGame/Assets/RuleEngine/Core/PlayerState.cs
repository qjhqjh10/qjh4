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

        /// <summary>
        /// **这一回合从牌库抽到过的牌**（按张数记；`BeginTurn` 清零）。
        /// 用处：**传送（`Teleport`）** —— 规则书 `:219`「**当回合从牌库抽到即打出时**触发能力」。
        ///
        /// ⚠️ 用**计数**而不是布尔：手里可能有两张同名卡，只有被抽到的那一份算数。
        /// ⚠️ 这是「按张数记账」的近似：同名两张里抽到一张、打出另一张也会算「抽到的那张」。
        ///    根因是**我们没有卡实例身份**（见 `资料/卡实例身份_爆炸半径.md`）。
        /// </summary>
        public readonly Dictionary<CardDef, int> DrawnThisTurn = new Dictionary<CardDef, int>();

        /// <summary>战场 9 格。<see cref="BoardSpec.WarlordSlot"/> 上永远是督军，其余为 null 或单位</summary>
        public readonly UnitState[] Board = new UnitState[BoardSpec.Size];

        /// <summary>和 <c>Board[BoardSpec.WarlordSlot]</c> 是**同一个对象**（便于直接取用，别写成两份）</summary>
        public UnitState Warlord;

        // 🔴 **2026-09-18：`DrawnThisTurn`（`Dictionary<CardDef,int>` 计数）已删** ——
        //    它就是「没有卡实例身份」时代的近似（原注释：「同名两张里抽到一张、打出另一张
        //    也会算抽到的那张」）。现在是 `CardInstance.DrawnThisTurn`，**一份一个布尔**，
        //    判据仍在 `RuleCore`（`Draw` 记、`PlayCard` 清、`BeginTurn` 清）。

        public int Energy;
        public int MaxEnergy;

        // ---- 两套阵营资源（2026-09-13 第三十三轮）------------------------------------
        // 原版里它们都是 `PlayerManager` 上的**每玩家一个 int**：
        //   · 信仰 `faithMana`（`PlayerManager.cs:87` / `GetCurrentFaith():147` / `AddFaithMana(int):212`）
        //   · 灵魂石 `spiritStoneMana`（`:85` / `GetCurrentSpiritStone():142` / `AddSpiritStoneMana(int):208`）
        //     —— 它其实是**第二种能量货币**（`ManaType{ Normal=0, SpiritStone=5 }`）。
        //
        // ⚠️ **数值口径是用户 2026-09-12 亲口定的，别再去找「初始值 / 上限 / 增长表」**（那是白找）：
        //   · **信仰**：**没有上限、不会衰减**；只由「触发效果累加」涨，卡面在**达到阈值**（3 / 5 / 9 这种）
        //     时给更强的效果（规则书 `:184`「总信仰达到指定值时触发效果」）。⚠️ **不是货币，不花掉** ——
        //     `ResetMana(...)` 里**没有** `initialFaith` 参数，`ManaType` 里也没有 Faith。
        //   · **灵魂石**：**没有初始值、没有上限、没有每回合增长**；只由「路标石单位死亡 +1」
        //     （规则书 `:210`/`:225`）和卡面效果（`Gain N Spirit Stones`）增减，**扣减完全由效果决定**。
        //     ⚠️ 它**是**货币（可以 `Spend all your Spirit Stones`），所以和信仰在这一点上不一样。
        public int Faith;
        public int SpiritStones;

        /// <summary>
        /// **任务点**（暗黑天使的阵营资源，2026-09-13 第三十三轮）。
        ///
        /// 怎么查出来的：那批 DarkAngels 卡（`Ravenwing Champion` / `The Rock` / `Techmarine` /
        /// `Wages of Retribution` / `Reconnaissance Mission` / `Watcher in the Dark` …）的卡面
        /// 在「Gain N」后面画的都是**同一个锯齿圆环＋数字的图标**（图集 sprite `questPointsN`），
        /// 而 OCR 把图标丢了、只留下 `Gain 1` / `Gain 3` —— 有的还被误标成 `[Energy]`。
        /// 这**正好解释**了「为什么原版只给暗黑天使显示任务点图标」
        /// （`BattleDriver.ShowsQuestPoints` 记的机器码级出处：`cmp [督军+0x2c],0x6e` = DarkAngels）。
        ///
        /// 规则书 `:199`：「每获得 **3** 点任务：向牌库加入 1 张隐秘并洗牌」—— 判据见
        /// `RuleCore.DoFactionResource`（**只此一处**）。所以 HUD 上显示的是 `X/3`。
        /// </summary>
        public int QuestPoints;
        /// <summary>任务点**已经结算过几次**阈值（每满 3 点一次）。只由
        /// `RuleCore.QuestPointThreshold` 读写 —— **阈值判据只此一处**。</summary>
        public int QuestMilestone;

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
