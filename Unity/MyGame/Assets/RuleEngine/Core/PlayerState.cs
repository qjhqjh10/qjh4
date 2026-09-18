// PlayerState.cs — 一方玩家的全部状态
using System.Collections.Generic;

namespace RuleEngine
{
    public class PlayerState
    {
        public string Name = "Player";

        // 🔴 **2026-09-18（待办第 7 行 · 第 2 步）：三个区域的元素类型从 `CardDef` 换成了
        //    `CardInstance`** —— 手牌/牌库/弃牌堆里存的是**具体的那一份**，不再是卡模板。
        //    这样「只手牌里这一张降费」「同名两张里只有这一份临时」才做得到。
        //    · **「新造一张」**（初始牌库 / `create a copy` / 造衍生物 / 天赋生成）→
        //      必须 `ctx.NewInstance(card)` 发**新的一份**；
        //    · **「挪一份」**（抽牌 / 打出 / 回手 / 洗回牌库 / 复活的还是那一张）→
        //      **把同一个 `CardInstance` 搬过去**，不要再发新的。
        //    用户 2026-09-18 拍的口径：回手 / 洗回牌库**默认保留实例态**、变身另发新实例
        //    （见 `CardInstance.cs` 文件头）。
        public readonly List<CardInstance> Deck = new List<CardInstance>();
        public readonly List<CardInstance> Hand = new List<CardInstance>();
        public readonly List<CardInstance> Discard = new List<CardInstance>();

        /// <summary>
        /// **「这一回合从牌库抽到的牌」的账记在哪儿**（待办第 7 行 · 第 3 步，2026-09-18 搬完）。
        ///
        /// 🔴 **这里原来有一个 `Dictionary&lt;CardDef,int&gt; DrawnThisTurn` 字段 —— 已删。**
        ///    它就是「没有卡实例身份」时代的近似：原名注释自认
        ///    「同名两张里抽到一张、打出另一张也会算抽到的那张」。
        ///    现在这个位是 <see cref="CardInstance.DrawnThisTurn"/>（**一份一个布尔**）：
        ///    `RuleCore.Draw` 置位 · `BeginTurn` 按本方区域清 · `PlayCard` 判传送（`Teleport`）时就地清。
        ///
        /// ⚠️ **更正痕迹**（同一天早些时候这里写过一句反话）：2026-09-18 上午这一格曾写着
        ///    「`DrawnThisTurn` **已删**、现在是 `CardInstance.DrawnThisTurn`」而**当时那句话是错的**
        ///    —— 那天先试了一版实例化、写完注释后**整版回退**（爆炸半径太大），注释没跟着回退。
        ///    ⇒ 下午真正做完之后，这句才成立。**留个痕：同一句话在一天里既假又真，别只信文字、要看字段。**
        /// </summary>

        /// <summary>战场 9 格。<see cref="BoardSpec.WarlordSlot"/> 上永远是督军，其余为 null 或单位</summary>
        public readonly UnitState[] Board = new UnitState[BoardSpec.Size];

        /// <summary>和 <c>Board[BoardSpec.WarlordSlot]</c> 是**同一个对象**（便于直接取用，别写成两份）</summary>
        public UnitState Warlord;

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
