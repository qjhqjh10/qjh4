// CardInstance.cs — **一张具体的牌**（手牌 / 牌库 / 弃牌堆 / 移出游戏 / 死单位表里存的应该是它）
//
// 🔴 **2026-09-18：这个类型已经写好，但引擎还没切过来 —— 目前全工程没有一处使用它。**
//    为什么没切：试了一版，实测**爆炸半径比预估大得多** ——
//      · `PlayerState.Deck/Hand/Discard` 换成它 → 编译器立刻报 **120 个去重错误点**
//        （`EffectResolver` 68 · `RuleCore` 38 · `SimpleAI` 7 · `BattleDriver` 7）；
//      · 更要命的是 **`UnitState` 只存 `CardDef`** —— 「手牌 → 场上」那一跳会把实例**丢掉**，
//        单位死了就再也放不回那一份（`RuleCore.cs` 的 `new UnitState(card, false)` /
//        `ps.Discard.Add(u.Card)`）。要根治得让 `UnitState` 也带实例，
//        而 `u.Card` 全工程约 **200 处**。
//      · 而且那 120 处**不全是机械替换**：`ps.Hand.Add(pick)` 这种要先判断 `pick`
//        是「已经存在的一份」还是「新造的一张」（后者要 `NewGame` 里发新实例）——
//        **猜错就是一个静默的语义 bug**，正是本项目最忌讳的那类。
//    ⇒ 按「宁可认不出，别静默错一张」的规矩**先回退**，留给独立的一轮。
//    **完整计划（A/B/C/D 四类爆炸半径 + 5 步改动顺序）见 `资料/卡实例身份_爆炸半径.md`。**
//
// 为什么要有它（= 待办第 7 行「卡实例身份」）：
//   手牌/牌库/弃牌堆存的是 `CardDef`（**卡模板**）—— 同名两张是**同一个对象**，
//   于是「只降**这一张**复制品」「只把**这一份**变成临时卡」做不到，
//   只能靠**按份数记账**近似（`PlayerState.DrawnThisTurn` / `BattleContext._markedEphemeral` /
//   `HandBuff.Count` / `CostMod.Key` 四处，注释里都写着「我们没有卡实例身份」）。
//
// 和 `UnitState` 的关系（它是现成范式，**别去动棋盘那套**）：
//   `CardDef`（不可变定义） → `CardInstance`（手里的那一份） → `UnitState`（场上的那个单位）
//   `UnitState` 本来就是「定义 + 实例态」；缺的只是中间这一层。
//   ⚠️ 但**接过来的时候 `UnitState` 也要带上实例引用**，否则手牌→场上那一跳会断链（见上）。
//
// 🔴 **实例态跟着实例走**：一张牌从手牌 → 场上 → 弃牌堆 → 回手，**还是同一个 `CardInstance`**。
//    只有「**新造一张**」才发新实例（初始牌库 / `create a copy` / 造衍生物）。
//    ⚠️ 这条是**我们定的**：原版对「回手之后加成/减费还跟不跟着」查不到明确说法，
//       而改成实例之前「按份数记账」的效果默认就是跟着走的（键是 CardDef）
//       ⇒ 选「跟着走」= **行为与改前一致**，只是变精确。
//
// 🔴 **实例 id 必须可复现**：由 `BattleContext` 的计数器发（`NextInstanceId++`），
//    **不用 Guid / 不用 `UnityEngine.Random`** —— 同种子必须得到同一局（见 `BattleContext` 头注释）。
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>一张**具体的**牌。字段语义见文件头。</summary>
    public sealed class CardInstance
    {
        /// <summary>它是哪张卡（不可变定义，全场同名卡共享同一个对象）。</summary>
        public readonly CardDef Card;

        /// <summary>本局唯一、**可复现**。排查日志 / 表现层配对用。</summary>
        public readonly int Id;

        // ---- 实例态：原来靠「按份数记账」近似的那几样，现在一人一份 ----

        /// <summary>
        /// **这一份**是本回合从牌库抽到的（`Teleport` 判据）。
        /// 替掉原来的 `PlayerState.DrawnThisTurn`（`Dictionary&lt;CardDef,int&gt;` 计数）。
        /// 规则书 `:219`「当回合从牌库抽到即打出时触发能力」。
        /// </summary>
        public bool DrawnThisTurn;

        /// <summary>
        /// **这一份**被标记成临时卡（回合结束未打出即移出游戏）。
        /// 替掉原来的 `BattleContext._markedEphemeral`（计数）。
        /// ⚠️ 和「卡自带 `Ephemeral` 关键词」不是一回事：那个是**这张卡的所有份**都临时。
        ///    判据合并在一处：`BattleContext.IsEphemeral`。
        /// </summary>
        public bool EphemeralMarked;

        /// <summary>
        /// 挂在这一份上的**手牌加成**（打出时以新上场的单位为靶跑一遍）。
        /// 替掉原来的 `BattleContext.HandBuff{ CardDef Card; int Count; }`。
        /// </summary>
        public readonly List<EffectOp> HandBuffOps = new List<EffectOp>();

        /// <summary>手牌加成的来源卡名（日志用）。</summary>
        public string HandBuffSource;

        /// <summary>
        /// **只作用在这一份上的费用修正**（`CostMod.HandInstanceId` 指向它）。
        /// 「只降这一张复制品」靠它 —— 这是本行的**主要靶子**。
        /// ⚠️ 它是**累计**的：多条修正命中同一份时累加（原版 `AddEffect` 也是累加）。
        /// </summary>
        public int CostDelta;

        public CardInstance(CardDef card, int id)
        {
            Card = card;
            Id = id;
        }

        /// <summary>是不是某张卡（按**定义**比，不是按份）。</summary>
        public bool Is(CardDef c) { return ReferenceEquals(Card, c); }

        public override string ToString()
        {
            return (Card != null ? Card.Name : "?") + "#" + Id;
        }
    }
}
