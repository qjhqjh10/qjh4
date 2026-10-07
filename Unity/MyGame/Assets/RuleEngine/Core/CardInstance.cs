// CardInstance.cs — **一张具体的牌**（手牌 / 牌库 / 弃牌堆 / 移出游戏 / 死单位表里存的应该是它）
//
// ✅ **2026-10-17 订正（铁律 5）：这一节原来写「第 1 步已做，**第 2/3/4 步还没做**」—— 【整段过期】。**
//    `资料/卡实例身份_爆炸半径.md` §八 记着 **2026-09-18 当天五步全部做完**（`RuleEngineTest` **2928/2928** 全过、
//    一号验收靶 `Master of Manoeuvre` 绿）；本轮现核也对得上：`PlayerState.Deck/Hand/Discard`
//    **三处都已经是 `List<CardInstance>`**、`DrawnThisTurn` 已挪进 `CardInstance`
//    （`PlayerState.cs` 里那几行也留着同样的订正痕迹）。⇒ **⛔ 别照下面那段去改类型。**
//
// ✅ **2026-10-18（`A885` + `A886`）：手牌效果的存放处也搬到【实例】上了**（原来那张对局级的
//    `BattleContext.HandBuffs` 已删）—— 手牌效果**挂在 `CardInstance` 自己身上**
//    （= 原版 `CardScript +0x108` 那个 `List<CardEffect>`）：`HandBuffOps` / `HandBuffSource` /
//    `HandBuffExpire` / `HandBuffExpireTurn` / `HandBuffUsesRef` 就是那一格。
//    三个登记点与兑现点见 `RuleCore.AttachHandEffect` / `RuleCore.ApplyHandBuffs`，
//    到期清扫见 `RuleCore.ExpireHandBuffs`（原版 `PlayerHand.UpdateCardEffects`）。
//    ⚠️ **仍然没有消费者的**：「在手里就看得见」（卡面 / 查询）—— 我们不做假装有。
//
// 📌 **历史留痕（2026-09-18 之前的状态，只留结论、正文已删）**：那一步之所以没一次切完 ——
//    `Deck/Hand/Discard` 换类型会报 **120 个去重错误点**（`EffectResolver` 68 · `RuleCore` 38 · `SimpleAI` 7 ·
//    `BattleDriver` 7），而且**不全是机械替换**（`ps.Hand.Add(pick)` 要先判「已存在的一份 / 新造的一张」，
//    猜错就是一个静默的语义 bug）。⇒ 完整计划与爆炸半径 → `资料/卡实例身份_爆炸半径.md`。
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
        ///
        /// ✅ **2026-10-18（`A885`）：这里就是原来的那个存放处** ——
        /// 替掉了 `BattleContext.HandBuff{ CardDef Card; int Count; }`，
        /// 再在 2026-10-18 替掉了它自己那一版对局表 `BattleContext.HandBuffs`。
        ///
        /// 🔴 **为什么搬上来**：原版这套效果**长在牌自己身上** ——
        /// `CardScript.AddEffect(那张牌, handEffect.cardEffect)`（`PlayerHand__SetupCardInHand.c:71` ·
        /// `PlayerHand__AddHandEffect.c:126-135`），存在 `CardScript` 的 `+0x108`（`List<CardEffect>`，
        /// `d:/2/tools/il2cpp_out/dump.cs` 核过偏移）。挂了之后：
        ///   · **在手里那一段就属于那张牌**（`EntityScript.HasCurrentTrait` 同时读 `+0x128` 与 `+0x108`）；
        ///   · **打出时跟着上场** —— `PlayerHand.RemoveCardFromHand`（`…:20`）**只把这张牌从
        ///     `currentHand` 里 `Remove`**，不销毁、不重建 ⇒ 手上那张牌打到场上**还是同一个对象**。
        /// ⇒ 所以它必须挂在 `CardInstance` 上，**不是**一张对局级的表：
        ///   对局表那版「同名两张各挂各的」只是**近似**，且「打出的那一份」得靠
        ///   `ReferenceEquals(h.Instance, inst)` 去对局表里回查 —— 表一被别处清掉就静默丢。
        ///
        /// ⚠️ 载荷在这一刻**不解析**（只留原文），兑现时交给 `give` 那条路
        ///    （词表只有 `GivePayload` 一份判据，见 `EffectResolver.AttachHandEffect`）。
        /// </summary>
        public readonly List<EffectOp> HandBuffOps = new List<EffectOp>();

        /// <summary>手牌加成的来源卡名（日志用）。多条来源时记**最后**挂上的那一条。</summary>
        public string HandBuffSource;

        /// <summary>
        /// **这份手牌效果什么时候到期**（`BattleContext.Turn` 的回合号；配合 <see cref="HandBuffExpire"/>）。
        /// 对应原版 `CardEffect` 上那四个 `until*` 标志位里**我们解析层能产出的两档**
        /// （偏移都在 `d:/2/tools/il2cpp_out/dump.cs` 核过）：
        ///   · 卡面 `this turn` ⇒ `untilEndOfTurn // +0x32`
        ///   · 卡面 `until your next turn` ⇒ `untilPlayerTurnStart // +0x33`
        ///     / `untilFollowingPlayerTurnStart // +0x35`（原版把这两个放在**同一条判断**里）
        /// ⚠️ 剩下两档（`untilEnemyTurnStart // +0x34`）**我们的解析层产不出来** ⇒ 没有写点，
        ///    清扫器里如实标着「没做」；⛔ 别以为它做了。
        /// </summary>
        public int HandBuffExpireTurn;

        /// <summary>到期方式 —— 照原版 `PlayerHand.UpdateCardEffects` 的那几条支线。</summary>
        public HandBuffExpiry HandBuffExpire = HandBuffExpiry.Never;

        /// <summary>这份手牌效果怎么到期（原版 `PlayerHand__UpdateCardEffects.c` 的三条支线）。</summary>
        public enum HandBuffExpiry
        {
            /// <summary>不过期 —— 原版四个 `until*` 全为假的那种
            /// （`Beast Snagga Nob` 的 `At the end of your turn, give +1 …` 那一族：**每回合结束再挂一份**、
            /// 一直攒着，见 `RuleEngineTest.TestBeastbossAndPayloadSegments` ④）。</summary>
            Never = 0,

            /// <summary>`CardEffect.untilEndOfTurn // +0x32` —— 回合结束时**无条件**摘
            /// （`PlayerHand__UpdateCardEffects.c:100` 那条**不比较谁的回合**）。</summary>
            EndOfTurn = 1,

            /// <summary>`untilPlayerTurnStart // +0x33` / `untilFollowingPlayerTurnStart // +0x35` ——
            /// **不是拥有者的回合**结束时摘（那一刻的下一回合就轮到拥有者了）。
            /// 判据 = `PlayerHand__UpdateCardEffects.c:119-137`（`IsPlayerTurn() != originalIsPlayer`）。</summary>
            OwnerTurnStart = 2,
        }

        /// <summary>
        /// 一条手牌效果「**还能打出几次**」的**共享计数盒**（原版 `HandEffect.numberOfUses // +0x2c`，
        /// 开关 `limitedUses // +0x28`）。
        ///
        /// 🔴 **额度是【共享】的，不是一份一份** —— 与原版那两张表的结构一致：
        ///   · `PlayerHand.activeEffects // +0x48` = `List<HandEffect>`（**记录**，计数器长在这上面）；
        ///   · `CardScript.activeEffects`/`+0x108` = `List<CardEffect>`（**逐张牌挂的那份**）。
        ///   一条 `HandEffect` 发给 N 张牌（`PlayerHand__AddHandEffect.c:126-135`）⇒
        ///   **N 张共用同一个 `numberOfUses`**。判据：
        ///   · `PlayerHand__CardPlayedWithEffects.c:28`（`limitedUses` 是开关）· `:33-35`（打出的那张牌
        ///     身上有没有这条效果）· `:39 numberOfUses--` · `:43-45 < 1 ⇒ RemoveHandEffectAt`；
        ///   · `PlayerHand__RemoveHandEffectAt.c:30-40` 遍历**整只手牌**逐张 `CardScript.RemoveEffect`
        ///     ⇒ 摘的是**所有还带着它的牌**（不是打出的那一张 —— 那张已经上场了）。
        /// ⇒ 所以存的是**一个对象**：同一次挂载发给的每一份**引用同一个盒子**（<see cref="HandBuffUsesRef"/>）。
        ///
        /// ⚠️ 与 `RuleCore.ApplyHandBuffs` 那条既有口径「**兑现即摘 —— 额度钉在这一份上**」
        ///    **并存、不互相取代**，哪条优先写在那个方法的注释里。
        /// </summary>
        public sealed class HandBuffUses
        {
            /// <summary>原版 `HandEffect.limitedUses // +0x28`：这条效果**限次**（`false` = 不限）。</summary>
            public bool Limited;
            /// <summary>原版 `HandEffect.numberOfUses // +0x2c`：还剩几次。</summary>
            public int Left;
            /// <summary>⚠️ **我们加的**（原版没有这个字段）：起始次数，只用于日志与断言。</summary>
            public int Max;
        }

        /// <summary>这一份共用的「还能打出几次」盒子（`null` = 不限次）。见 <see cref="HandBuffUses"/>。</summary>
        public HandBuffUses HandBuffUsesRef;

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
