// CardInstance.cs — **一张具体的牌**（手牌 / 牌库 / 弃牌堆 / 移出游戏 / 死单位表里存的应该是它）
//
// 🔴 **2026-09-18 进度：第 1 步已做，第 2/3/4 步还没做。**
//    · ✅ **第 1 步（本日）**：`UnitState` 带实例（`UnitState.Instance`，`Card` 改成属性转发）·
//      `BattleContext.NewInstance` 发号（每局从 1 开始）· 五个真构造点都改成发/沿用实例。
//      **行为零变化**，自检 `TestCardInstanceStep1` 量的是结构（见那条注释）。
//    · ❌ **第 2 步**：`PlayerState.Deck/Hand/Discard` 仍是 `List<CardDef>` ——
//      **手牌那一侧还没有实例**，所以「手牌 → 场上」目前是**新发一份**而不是沿用同一份
//      （`RuleCore.PlayCard` 里那行有注释标着）。换类型时编译器会报 **120 个去重错误点**，
//      而且**不全是机械替换**（`ps.Hand.Add(pick)` 要先判断「已存在的一份 / 新造的一张」）。
//    · ❌ **第 3 步**：`PlayerState.DrawnThisTurn` / `BattleContext._markedEphemeral` /
//      `HandBuff` / `CostMod.Key` 四处仍按**卡模板/份数**记账。
//    · ❌ **第 4 步**：出口层（`BattleDriver.SyncHand` / `HandIndexOf` / `AiAction.HandIdx`）。
//    ⇒ 完整计划（A/B/C/D 四类爆炸半径 + 5 步顺序）见 `资料/卡实例身份_爆炸半径.md`
//      （§六 = 实测补充、§七 = 用户拍板的语义）。
//
// 为什么没一次切完（上一轮试了一版、**已回退**）：
//      · `Deck/Hand/Discard` 换成它 → 编译器立刻报 **120 个去重错误点**
//        （`EffectResolver` 68 · `RuleCore` 38 · `SimpleAI` 7 · `BattleDriver` 7）；
//      · **`UnitState` 只存 `CardDef`** ⇒ 「手牌 → 场上」那一跳会把实例丢掉 ——
//        **这一条第 1 步已经解决**（`UnitState.Instance`）；
//      · 而且那 120 处**不全是机械替换**：`ps.Hand.Add(pick)` 这种要先判断 `pick`
//        是「已经存在的一份」还是「新造的一张」（后者要由 `BattleContext` 发新实例）——
//        **猜错就是一个静默的语义 bug**，正是本项目最忌讳的那类。
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
// 🔴 **实例态怎么走 —— 用户 2026-09-18 拍了：由效果自己决定**。引擎的默认是：
//    · **回手 / 洗回牌库** → **同一个 `CardInstance`**（不新建），实例态**默认保留**；
//      效果的分句可以在之后显式改它（例：`Master of Manoeuvre` 的 `It costs 4 less`）。
//    · **变身**（`become a …`，全卡池只有 `Hrolf the Ironhowl`）→ **另发新实例、状态不跟**
//      （卡都换了；改前那套按 `CardDef` 键，换 def 之后键就变了 ⇒ 旧行为本来也是「不跟」）。
//    · 「新造一张」（初始牌库 / `create a copy` / 造衍生物）→ 当然发新实例。
//    ⚠️ 默认「保留」的理由：改前「按份数记账」的键是 `CardDef`，回手后卡还是那个 def
//       ⇒ **效果默认就是跟着走的**。选它 = 与改前行为一致，只是从「按份数」变精确。
//    🎯 **一号验收靶**：`Master of Manoeuvre`（`Return a friendly Vehicle to your hand. It costs 4 less`）——
//       现在 `EffectResolver.DoReturn` 末尾用 `ctx.LastCreated`（**存 `CardDef`**）当「它」的引用位，
//       **手里两张同名载具时两条都会降价**；做完实例身份之后必须变成「只有那一份」。
//       口径与验收集见 `资料/卡实例身份_爆炸半径.md` §七。
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

        // ---- 谁来发 id：真对局 vs 没有对局的场合 -------------------------------
        //
        // 🔴 **真对局只有一处发号**：`BattleContext.NewInstance`（`_nextInstanceId++`）。
        //    它跟着 `BattleContext` 走 ⇒ **每一局都从 1 开始**、同种子必然同序号（对局可复现）。
        //
        // 下面这个 `Detached` 是给**没有 `BattleContext` 的场合**兜底的：
        // 测试自己拼棋盘（`new UnitState(card, false)`）、演示场景塞探针、卡池预览 ——
        // 那些地方拿不到 ctx。它发的是**负数 id**，所以**永远不会**和对局发的正数撞号。
        // ⚠️ 别拿它当「真实例」用：它没有对局里那份「手牌 → 场上 → 弃牌堆」的连续身份，
        //    只是「一个够用的身份位」。真对局的部署一律走 `UnitState(CardInstance)`。

        static int _detachedNext = 0;

        /// <summary>没有 `BattleContext` 时用的实例（负数 id）。语义见上面那段注释。</summary>
        public static CardInstance Detached(CardDef card)
        {
            return new CardInstance(card, --_detachedNext);
        }

        /// <summary>是不是「没有对局上下文」的分离实例（报表 / 断言用）。</summary>
        public bool IsDetached { get { return Id < 0; } }

        public override string ToString()
        {
            return (Card != null ? Card.Name : "?") + "#" + Id;
        }
    }
}
