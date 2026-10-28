// BattleContext.cs — 一局对战的完整状态
//
// 随机数用 `System.Random(seed)` 而**不是** `UnityEngine.Random` —— 后者有全局状态、
// 会被别处悄悄消耗，对局就没法复现了。同一个种子必须永远得到同一局。
// （特效线吃过这个亏：粒子随机种子没钉死，两遍扫描 60% 的行会变，噪声和要测的改动一样大。）
//
// 🔴 **本文件里那些 `rule_core.gd:<行>` 是什么**：`d:/warpforge/scripts/rule_core.gd`
//    = **我们自己的上一版 Godot 复刻**（70 个 `.gd`），**只作对照/旁证**，**不是原版语义判据**。
//    判据顺序 = ① 原版全量反编译 `D:/2/tools/decomp_full/` → ② 解包资源字段 → ③ 成品卡图卡面文字；
//    粉丝实体规则书（《Warpforge Offline Rulebook》，非官方）与那份 `.gd` 都只作**第二来源/旁证**。
//    ⚠️ **2026-10-18 更正（铁律 5）**：本文件下面原来有 9 处把那些行号写成「**原版**
//    `rule_core.gd:NNNN`」—— **那是错的**（出处 = `CLAUDE.md` 铁律 2 的 2026-09-18 更正），
//    已逐处改口径；行号保留（它是可复查的坐标）。
using System;
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>
    /// 🔴🆕 **2026-10-24（引擎线 `T03·c` · `C1` §3）：原版 `UnitDeathType` 的数值表**
    /// （照抄 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/UnitDeathType.cs:3-9`）。
    ///
    /// <para>**为什么现在才需要它**：原版 `RawCardScript__OnMinionDeath.c:25-37` 按**死因**分派
    /// 三条触发 —— `10 combatAttacker → 175 UnitDeathAttacking` · `20 ability → 180 UnitDeathByAbility`
    /// · `40 poison → 170 UnitDeathByPoison`，其余（`15 combatDefender` / `30 tactic` / `50 collectWaystone`）
    /// **一条都不发**。我们以前只有「`ActingCard != null` = 记击杀」这一维，
    /// 它把 `15` 与 `20` 混在一起 ⇒ 分不出「能不能发 180」。</para>
    ///
    /// <para>⚠️ **只作死因标签用**，⛔ 别拿它去当「记不记击杀」的判据 ——
    /// 那一条仍然只在 `RuleCore.TryCreditKill` 一处（「`ActingCard != null`」）。
    /// `Unknown(0)` = 我们没钉死的那几路（进攻卡 / 不稳定 / 回合开始清死人…），
    /// **不许当 `ability` 用**（铁律 2：查不到就说查不到）。</para>
    /// </summary>
    public static class UnitDeathType
    {
        public const int Unknown = 0;
        public const int CombatAttacker = 10;
        public const int CombatDefender = 15;
        public const int Ability = 20;
        public const int Tactic = 30;
        public const int Poison = 40;
        public const int CollectWaystone = 50;
    }

    /// <summary>
    /// 一条**常驻效果**（`BattleContext.PersistentEffects` 的一项）。
    ///
    /// <see cref="Source"/> 记着是哪张卡、<see cref="Owner"/> 记着是谁打出的 ——
    /// 这一摞是**我们自设计的替代结构**（原版没有对应类型；判据见 <see cref="PersistentEffects"/>
    /// 的说明）。⚠️ 2026-10-19（`A1955`）订正：原来这里引粉丝规则书 `:39-41` 的
    /// 「单独放一摞在弃牌堆旁」当理由 —— 那是**实体台面**的备忘办法，数字版没有对应物。
    /// </summary>
    public class PersistentEffect
    {
        /// <summary>谁打出的 —— 只有**他自己的回合**才触发（我们上一版复刻 `rule_core.gd:434`，⚠️ **旁证**）</summary>
        public int Owner;
        /// <summary>来源卡（日志与排查用 —— 这一摞是自设计结构，不是原版的建模方式）</summary>
        public CardDef Source;
        /// <summary>`turn_start` / `turn_end` / `deploy`（见 <see cref="Trigger"/>）</summary>
        public string Phase;
        /// <summary>触发时要结算的 op（**解析一次存下来**，见 `EffectOp.AtTurnOps`）</summary>
        public List<EffectOp> Ops;
        /// <summary>正文原文（日志要打人话）</summary>
        public string Body;

        /// <summary>
        /// **什么事件让它触发**。`Phase` 与它配对：`Trigger` 说「哪一类事件」，
        /// `Phase` 说「这一类里的哪一刻」。
        ///
        /// · `"turn"`（默认，`turn_start` / `turn_end`）—— 回合起止段，见 <see cref="RuleCore.ResolveAtTurn"/>
        /// · `"deploy"` —— **某个单位被部署上场时**，见 <see cref="RuleCore.ResolveDeploy"/>
        /// · 🆕 `"when"`（2026-09-14 A4 批 3）—— **`For the rest of this battle, when &lt;事件>, &lt;正文>`**
        ///   （`Raid Tactics`）；`Phase` 闲置，事件判据在 <see cref="Ev"/>，
        ///   由 `RuleCore.BroadcastPersistentWhen` 在**每次事件广播**时扫。这张卡是**战术卡**，
        ///   打出后自己进弃牌堆 ⇒ 没有「实体监听者」可挂，所以必须单独扫这一摞。
        ///
        /// **原版出处**：部署那条路走 `CardScript.ResolveUnitSummoned`（`CardScript__ResolveUnitSummoned.c:33`）
        /// → `OnTrigger(AbilityTrigger.OtherUnitSummoned = 190, …)`；广播在
        /// `BattleManagerSupport__BroadcastUnitSummoned.c` —— **先自己 `:23`，再场上每张牌 `:41`，
        /// 再当前回合方手牌 `:53`，再另一方手牌 `:65`**（手牌也会收到，所以「手里的牌盯着部署事件」是原版就有的）。
        ///
        /// ⚠️ **不是 `HandEffect`**：`HandEffect` 是 `AbilityLogic.handBuff` 的字段、配
        ///    `AbilityEffect.buffHand = 50`，那是**技能**用的容器（2026-09-13 查证，见 `资料/常驻效果_数据与设计.md`）。
        /// </summary>
        public string Trigger = "turn";

        /// <summary>
        /// 这条常驻效果**作用在哪种卡上**（`Trigger == "deploy"` 时用）。
        /// `null` / 空 = 不筛。见 <see cref="CardCriteria"/>。
        /// </summary>
        public CardCriteria Criteria;

        /// <summary>
        /// 🆕 2026-09-14 A4 批 3：**`Trigger == "when"` 时的事件判据**
        /// （`Raid Tactics` 的 `when a friendly troop uses Ferocity`）。
        /// **空 = 这条不是事件型**。判据共用 `WhenEvents.Matches`，不另写一套。
        /// </summary>
        public WhenEvent Ev;

        public override string ToString()
        {
            return (Source != null ? Source.Name : "?") + "@" + Phase;
        }
    }

    /// <summary>
    /// 一个**阵亡的部队**（`BattleContext.DeadUnits` 的一项）。
    ///
    /// 为什么要单独一张表而不是查 `Discard`：见 `BattleContext.DeadUnits` 的注释 ——
    /// `Discard` 混装了打出的战术卡，而且它没有**死亡时间**。
    /// 「自你上个回合之后死的」这个窗口全靠 <see cref="DeathTurn"/> 划。
    /// </summary>
    public class DeadUnit
    {
        /// <summary>死掉的那个单位是哪张卡（`UnitState.Card`，和牌库/弃牌堆里那份是**同一个 `CardDef` 对象**）。
        /// ⚠️ **这里仍然是卡模板、不是 `CardInstance`**（2026-09-18 定的边界）：待办第 7 行要的是
        /// 「**手牌/牌库/弃牌堆**存实例」，而这张表要回答的是「哪些部队死过、死于第几回合」，
        /// 它的消歧走 `TakeFromGraveyard`（已改成**只取走一张**，见那里）。
        /// 🆕 **2026-10-23（引擎线 `新账-97`）**：旁边加了 <see cref="Instance"/> ——
        /// 「精确到某一次的死亡」不再只靠模板；`ConditionEval` 的 221/222 与墓地取件都可用它。
        /// `Card` 保留给只认模板的老路径（不删）。</summary>
        public CardDef Card;
        /// <summary>🆕 2026-10-23：死掉的那一份**实例**（= `u.Instance`，与 `Discard` 里那一份同引用）。
        /// 同一张卡死了两次是两条不同的记录，各带自己的实例号。旧数据/手搓夹具可能为 `null`
        /// ⇒ 所有读点必须回退到模板匹配（见 <see cref="InstanceId"/>）。</summary>
        public CardInstance Instance;
        /// <summary>实例号（`Instance.Id`；没有实例 = 0）。匹配「是哪一份」时优先用它。</summary>
        public int InstanceId { get { return Instance != null ? Instance.Id : 0; } }
        /// <summary>它属于哪一方（`Choose a **friendly** troop that died …` 只在自己这边挑）</summary>
        public int Owner;
        /// <summary>死在第几回合（全局 `ctx.Turn`）。与 <see cref="PlayerState.LastTurnStartMark"/> 比出窗口</summary>
        public int DeathTurn;   public bool IsRemnant;   // 🔴 2026-10-28（`B89-E2` · 账 `L336`）：**`IsRemnant`** = 原版 `CardScript.isRemnant`（`+0x65`）的等价物 ——「**死的那一刻**是不是一具残骸」的**冻结快照**。判据（逐句实读）：① 写点 `CardScript__TransformIntoRemnant.c` 翻面时 `*(undefined2*)(card+0x65)=1`、`CardScript__TransformFromRemnant.c:10` 变回来时清 0；② `CemeteryManager__AddCardToCemetery.c` 里 `0x65` **0 命中** ⇒ 那张卡进墓地时**带着当时的值**；③ 唯一消费者 `BattleManager__GetCardsInCemetery.c:60` **无条件排除** `+0x65 != 0` 的牌（与 cardType 那几格并列、**不受** `incl*` 影响）⇒「残骸被摧毁」进墓的那张在原版**从墓地查询里查不出来**（我们原来分不出 ⇒ 多算）。⚠️ 与 `UnitState.IsRemnant`（「**现在**是不是残骸」）不是一回事：残骸事后被 `reanimate` 变回来**不改历史**。⚠️ 分不出时默认 `false` = 照旧查得到（与改前逐字相同，⛔ 别反过来默认 `true`）。读点 = `RuleCore.GetCardsInCemetery`（⚠️ `EffectResolver.ReturnFromCemetery` 仍自己筛、**还没切过去** ⇒ 那一支的残骸闸还没生效）。⛔ **别把它拆成单独一行** —— 本文件下面还有 30+ 处账的落点，插一行就整体漂移

        public override string ToString() { return (Card != null ? Card.Name : "?") + "@T" + DeathTurn; }
    }

    /// <summary>
    /// 一张**本局打出过的牌**（<see cref="BattleContext.PlayedCards"/> 的一项）。
    ///
    /// **为什么要单独一张表而不是查 `Discard`**：① `Discard` 是**每个玩家各自的弃牌堆**，
    /// 而「本局打出过」是一条**按时间排的全对局流水**；② 打出的**单位**根本不进弃牌堆
    /// （它在场上）；③ 弃牌堆里也分不出「打出的」和「被弃置/磨掉的」。
    ///
    /// **原版出处**（判据 = 反编译；细节与出处行号见
    /// `资料/普查产出_1017/W_B19_本局打出过的牌.md` §一）：
    ///   · 「本局打出过」在原版**只有一处实现** —— `TargetsAffected.lastPlayedIfMeetsCriteria(190)`
    ///     （`TargetsAffected.cs`）→ `AbilityLogic.GetTargets` 的 `0xbe` 支
    ///     （`AbilityLogic__GetTargets.c:173`）→ `CemeteryManager.GetCardsPlayed`（VA `0x18061BC70`）；
    ///   · 它扫的是**坟墓动作日志**里 `actionType == CemeteryActionType.playCard(10)` 的条目
    ///     （`CemeteryActionType.cs`），日志**全对局不清**（`Setup` 之外没有写入点）⇒「this game」= 整局；
    ///   · 日志的唯一写入点 = `BattleManager._ResolvePlayCardFromHand` 里的
    ///     `CemeteryManager.AddPlayCardAction` —— **打出一张牌 = 一条**，单位与战术卡同路。
    /// </summary>
    public class PlayedCard
    {
        /// <summary>打出的**那一份**（`UnitState.Card` / 手牌 / 弃牌堆里是**同一个 `CardInstance`**）。
        /// 原版是拿 `actingCardUniqueGID` 回查 `CardScript` —— 同一件事。</summary>
        public CardInstance Instance;
        /// <summary>哪张卡（= `Instance.Card`，属性转发）</summary>
        public CardDef Card { get { return Instance != null ? Instance.Card : null; } }
        /// <summary>**谁**打的（`Choose a … **you** played this game` 只在自己这边挑）</summary>
        public int Owner;
        /// <summary>打在第几回合（全局 `ctx.Turn`）—— 只用来查询/排查；**别拿它判「谁更近」**</summary>
        public int Turn;
        /// <summary>是不是**战术卡**（单位卡为 `false`）。原版日志不分这一维，它是我们按份记下来的</summary>
        public bool Tactic;
        /// <summary>全局递增流水号 —— **「最近打出」的唯一判据**（一回合里能打好几张，`Turn` 分不开）</summary>
        public int Seq;

        public override string ToString()
        {
            return (Card != null ? Card.Name : "?") + "@T" + Turn + "#" + Seq + (Tactic ? ":tactic" : ":unit");
        }
    }

    /// <summary>
    /// 一张**被「移出游戏」的卡**（<see cref="BattleContext.Removed"/> 的一项）。
    ///
    /// **规则依据**：规则书 `:229` —— 临时卡「回合结束未打出即消失，**从游戏中移除（非弃置）**」。
    /// 英文原版 `:421-427` 说得更直白：实体版要「place these cards to the side
    /// **away from** the [deck/discard]」。⇒ **它不是第五张弃牌堆，是另一个区域。**
    ///
    /// ⚠️ **为什么不直接丢掉（连记录都不留）**：
    ///   ① 日志与排查要能回答「我刚才那张牌呢」—— 玩家最容易被「牌凭空没了」吓到；
    ///   ② 照 <see cref="DeadUnit"/> 的先例，这一类需求**会长出第二批**
    ///      （「本局移出过什么」/「本回合移出过几张」），所以一次做成能查询的结构；
    ///   ③ 表现层要从这里知道「该给哪张卡播消失动画」。
    /// </summary>
    public class RemovedCard
    {
        /// <summary>**哪一份**被移出了游戏（2026-09-18 第 7 行第 2 步起带实例 —— 表现层要靠它
        /// 认出「该给哪张卡播消失动画」，以前只有卡模板、只能按名字猜）。</summary>
        public CardInstance Instance;
        /// <summary>哪张卡（= `Instance.Card`，属性转发）。</summary>
        public CardDef Card { get { return Instance != null ? Instance.Card : null; } }
        /// <summary>谁的手牌里被移出的</summary>
        public int Owner;
        /// <summary>第几回合移出的（全局 `ctx.Turn`）。将来「本回合移出过几张」靠它划窗口</summary>
        public int Turn;
        /// <summary>为什么移出（目前只有 `ephemeral`；留字段是为了以后别的原因也走这个区域）</summary>
        public string Reason;

        public override string ToString()
        {
            return (Card != null ? Card.Name : "?") + "@T" + Turn + ":" + (Reason ?? "?");
        }
    }

    /// <summary>一条费用修正。`Key` = **卡 id**（`CardDef.Id`，2026-09-13 第三十三轮起；以前是卡名归一化），
    /// `"*"` = 不限卡名。</summary>
    public class CostMod
    {
        public int Player;
        public string Key;
        /// <summary>负数 = 降价</summary>
        public int Delta;

        /// <summary>
        /// 🆕 **2026-10-25（`K8` · 执行代理 N11）：这条修正是不是「**设为** N 费」**
        /// （原版 `CardEffect.buffType == 95 setCost`）。
        ///
        /// · `-1`（默认）= 普通增减 —— 加在 <see cref="Delta"/> 上（`buffType == 2 changeCost` 那一族）；
        /// · `>= 0` = **目标绝对费用**，此时 <see cref="Delta"/> **无意义**（恒 0）。
        ///
        /// **判据**：有 `setCost` 记录时
        /// `CurrentCost = clamp0(setCost.costChange + Σ 全部 changeCost)` —— 🔴 **不含 base**：
        /// `EntityScript__get_CurrentCost.c:52-79` 那条支在 `:73` 就 `clamp0` **直接 `return`**，
        /// `+ rawCardData.manaCost` 在 `:84-91` 的**正常出口**（只在**没有** `setCost` 时才走）。
        /// `setCost.costChange` 存的是**目标绝对值 N**（数据现证：`cards_truth.json` 里 `buffType==95`
        /// 3 条、全是 1，而三张卡 `manaCost` = 2/3/6，卡面写 `reduce its cost to 1`）。
        /// ⚠️ 旧口径把「设为 N 费」落成一条**快照 delta**（`d = N − 当时现价`）——
        /// 那会把「排在 `setCost` **之前**的 `changeCost`」吃掉（判据 §3·3·4/§3·3·5 记的就是这条）。
        /// </summary>
        public int SetTo = -1;
        /// <summary>到哪一回合为止；`-1` = 永久</summary>
        public int ExpireTurn = -1;

        /// <summary>
        /// **作用在哪些卡上**（`null` / 空 = 只看 <see cref="Key"/> 的卡 id）。
        ///
        /// 原版没有「费用修正列表」这种东西 —— 费用修正就是挂在卡上的一个 `CardEffect`：
        /// `CoreEffect.costChange`（`CardEffect.cs:64`，内存 `+0x68`）+ `buffType = changeCost(2)`
        /// （`BuffType.cs`）。挂载时**累加**（`CardScript__AddEffect.c:403`），算费用时由
        /// `EntityScript.CurrentCost` **现算**（`EntityScript.cs:74`）—— **不改卡上的费用字段**。
        /// 我们这边对应物就是 `RuleCore.CostOf` 现算，判据挂在这个字段上。
        ///
        /// 例：`Sabotage cards in the enemy hand cost 1 more`（`Underground Network`）——
        /// 原版那个 `HandEffect` 的 `targetCriteria` 带 `spellType = Sabotage(230)`
        /// （`SpellType.cs`），对应这里 `Criteria.KindWord = "sabotage"`。
        /// </summary>
        public CardCriteria Criteria;

        /// <summary>
        /// 这条修正作用在**哪一方的手牌**上；`-1` = 不限（只看 <see cref="Player"/>）。
        ///
        /// 为什么要单开一维：`Sabotage cards in **the enemy hand** cost 1 more` ——
        /// 打出者是 P1，被加价的却是 **P2 手里**的破坏卡。原版靠 `HandEffect.playersAffected`
        /// 表达（`PlayerHand__UpdateCardEffects.c:267` 判 `==10` 是自己、`:303` 判 `!=0x14` 是敌方，
        /// `HandEffect.cs` 的字段之一），**极性写反了整整一档效果就没了**。
        /// 我们这里 `HandOf = 1 - 打出者`。
        ///
        /// ⚠️ 旧的降费（`DoLowerCost`）不设这个字段，走 `-1` —— 行为与改动前完全一致。
        /// </summary>
        public int HandOf = -1;

        /// <summary>
        /// 🆕 **用完即销**（2026-09-14 A5 批 3 第 2 条）。卡面写了 `next`
        /// （`Your next Stratagem this turn costs 0`）时置真 —— 打出一张**符合筛选**的牌、
        /// 且**费用真的付掉之后**，由 `RuleCore.ConsumeOnceCostMods` 把这条撤掉。
        ///
        /// 🔴 **不设它 = 静默地降多了**：`next` 会被当成「本回合所有符合条件的牌都便宜」。
        /// ⚠️ 撤销**只能在付费之后**做 —— `RuleCore.CostOf` 是纯查询、被反复调用
        /// （能不能打得起、表现层显示多少费），在那里撤等于「看一眼就烧掉了」。
        /// </summary>
        public bool Once;

        /// <summary>
        /// 🆕 **这条修正是谁登记的**（`null` = 一次性效果登记的、不用管）。
        ///
        /// 现在**只有光环**用（2026-09-14 A7）：`Other friendly Daemons cost 2 less` 这类
        /// **常驻改费**，它的生命周期跟着**来源在不在场上**走 ——
        /// `Auras.Recompose` 先把 <see cref="Auras.GrantTag"/> 标记的这些整份撤掉、再按当前棋盘重加。
        ///
        /// ⚠️ 为什么不复用 <see cref="ExpireTurn"/>：那是**按回合数过期**，
        ///   而光环要的是「**来源死了就立刻撤**」—— 一个回合内可能换好几茬单位。
        /// ⚠️ 为什么不复用 <see cref="Key"/>：那一栏是**卡 id**（或 `*`），
        ///   光环这条是「所有符合筛选条件的牌」，`Key` 得留 `*`，需要一个**单独的**归属标记。
        /// </summary>
        public string Tag;

        /// <summary>
        /// 🔴 **这条修正钉在「哪一份手牌」上**（`CardInstance.Id`）；`0` = 不钉、按 <see cref="Key"/> 匹配全部副本。
        ///
        /// **待办第 7 行第 3 步（2026-09-18）**：加这一维之前，「**只降这一张复制品**」做不到 ——
        /// `Key = 卡 id` 会命中手里**所有**同名副本（D-1 / D-2 / D-3 / D-8 / D-11 那一族，
        /// 约 92 张卡）。原版是能区分的：费用修正挂在**那一张牌的实体**上
        /// （`CoreEffect.costChange` + `buffType = changeCost(2)`，`CardScript__AddEffect.c:403`
        /// 往**那一个** `EntityScript` 上加），不是挂在卡的模板上。
        ///
        /// **配方**：钉实例时写 `Key = "*"` + `HandInstanceId = &lt;那一份>.Id`
        /// —— ⚠️ **别同时把 `Key` 设成那张卡的 id**，那样别的副本会被 `Key` 命中，等于白钉。
        /// `CostOf(ctx, owner, 卡模板)`（拿不到「哪一份」的场合，比如卡面预览）**跳过**按份的修正。
        /// </summary>
        public int HandInstanceId;

        public override string ToString()
        {
            return (Delta >= 0 ? "+" : "") + Delta + (Key == "*" ? " 所有牌" : " " + Key)
                 + (Criteria != null && !Criteria.IsEmpty ? "（" + Criteria + "）" : "")
                 + (HandOf >= 0 ? $"（只算 P{HandOf + 1} 手里的）" : "")
                 + (ExpireTurn >= 0 ? $"（到回合 {ExpireTurn}）" : "")
                 + (Once ? "（**用完即销**）" : "")
                 + (Tag != null ? $"（{Tag}）" : "");
        }
    }

    public partial class BattleContext
    {
        public readonly PlayerState[] Players = new PlayerState[2];

        /// <summary>
        /// 🆕 2026-09-23：**本局的战果累计**，给「日常任务」那一层读（它自己不知道战果怎么来的）。
        /// 下标 = 玩家号。两边都记 —— 表现层只取自己那一格。
        /// 🔴 **只在这里累加、不参与任何规则判定**（引擎的胜负与它们无关）。
        /// · `DamageToEnemy[i]`：**i 对「敌方单位」造成的实际伤害**（`ApplyDamage` 里按「被打的那个单位属于谁」反推，
        ///   所以「打自己人」不会记进去 —— 正合日常任务那句 `enemy units`）；
        /// · `TroopsPlayed[i]`：i **打出的部队卡张数**（`PlayCard` 落位那一步数；督军不走那条路，不会被算进去）。
        /// </summary>
        public readonly int[] DamageToEnemy = new int[2];
        public readonly int[] TroopsPlayed = new int[2];
        /// <summary>🆕 **2026-10-17（界面线 `N32`）**：日常任务卡「真口径」要的三格计数（下标 = 玩家号）。
        /// <para>判据 = 用户实拍的三条日常文案（`A2070③`）：
        /// ① `Deal 45 damage to enemy warlords` ⇒ **只算打到敌方【督军】的伤害**（打小兵不算）；
        /// ② `Attack with units 15 times` ⇒ **成功宣告的攻击次数**（被取消 / 被挡在闸外的不算）；
        /// ③ `Attack with ranged units 15 times` ⇒ 其中**远程**那一档。</para>
        /// <para>⚠️ 与 <see cref="DamageToEnemy"/> 的分工：那个是「对敌方**任意单位**的伤害」（旧口径），
        /// 这三个是**任务卡真正要的口径** —— 别互相顶替（`DailyData.OnBattleEnd` 用的是新的三个）。</para>
        /// <para>🔴 只累加、不参与任何规则判定（同 `DamageToEnemy` 那条口径）。</para></summary>
        public readonly int[] DamageToEnemyWarlord = new int[2];
        public readonly int[] AttacksDeclared = new int[2];
        public readonly int[] RangedAttacksDeclared = new int[2];

        /// <summary>🆕 **2026-10-25（执行代理 `X2`）**：**本局的成就计数**（伤害分类 / 能力触发 / 出牌标签）。
        /// <para>口径 = 上面 <see cref="DamageToEnemy"/> 那一族**同一条**：**只累加、不参与任何规则判定**。
        /// 读点只有结算那一处（`BattleDriver.AwardAchievements` → `ProfileData.ApplyAchievementTally`）。</para>
        /// <para>判据与施工单 → `资料/待办判据_成就计数器_1025b.md` §B-2 / §B-3；
        /// 形状与每类的「谁记给谁」→ `Core/AchievementTally.cs` 的文件头。</para></summary>
        public readonly AchievementTally Tally = new AchievementTally();

        /// <summary>全局回合序号（从 1 开始，每换一次边 +1）</summary>
        public int Turn;

        /// <summary>当前行动方：0 / 1</summary>
        public int Active;

        /// <summary>🆕 2026-09-26：**这一局谁先手**（0 / 1）—— 默认 0 保持老行为。
        ///
        /// 🔴 **为什么必须是一个字段**：原版 `BattleManager.playerGoesFirst`（**+0x247**）是**真的一回事** ——
        /// 它决定**起始能量基数**（`SetupInitialMana` 在 `startingMana`/`startingManaSecond` 之间二选一 ·
        /// `:20,28,46`）、**防御卡发给谁**（`AddGoesSecondCardToDeck.c:108-166`，只有**后手**那一方拿）、
        /// 以及**加时判哪一边的能量**（我们那条：「**后手方**最大能量 ≥ 阈值」）。
        /// ⚠️ 我们原来把这三处**全部写死成「座位 1 = 后手」**（见 `RuleCore` 里那几条注释）——
        ///    那是「本作玩家恒先手」时代的写法；**先手是座位 0 并不是必然的**。
        /// </summary>
        public int FirstSeat;

        /// <summary>后手的座位号（<see cref="FirstSeat"/> 的另一边）。**判「后手」一律走它，别再写 `Players[1]`**。</summary>
        public int SecondSeat { get { return FirstSeat == 0 ? 1 : 0; } }

        /// <summary>0 = 进行中，1/2 = 该方胜，3 = 平局</summary>
        public int Winner;

        /// <summary>是谁投降的（0/1）。`-1` = 没人投降（正常分出胜负）。
        /// 结算面板要按它换一句话 —— 原版 `BattleResult.Forfeit` 和「督军倒下」是两种结局。</summary>
        public int ForfeitedBy = -1;

        /// <summary>种子流 —— **只用来烧随机表**（`InitRandomNumbers`，见 `BattleContextRandom.cs`）；结算随机一律走 `ctx.GetRandomInt`。⚠️ 2026-10-27（`E1RNG`）：原 doc 写「洗牌用它、引擎里只有这一处随机」—— 已过期。</summary>
        public readonly Random Rng;

        /// <summary>
        /// 🆕 2026-09-14：**只给表现层抽「给你看哪几张」用**的随机源，与 <see cref="Rng"/> 分开。
        ///
        /// **为什么分开**：规则书英文版 `:475` —— `choose` 的候选要**先随机抽 3 张**再让玩家挑
        /// （见 <see cref="ChooseCardIds"/>）。这件事发生在**打出之前**（表现层开面板那一刻）、
        /// 而且**只有玩家回合有**（AI 回合没有面板）。若共用 `Rng`，同一个动作在
        /// 「有没有面板」两种情况下会得到**不同的随机数序列** —— 对局虽然仍可复现，
        /// 但自检里想「跳过面板直接跑引擎」就对不上了。
        /// ⚠️ 它**同样是种子化的**（从同一个 `seed` 派生）⇒ **同一局照样可复现**。
        /// </summary>
        public readonly Random ShowRng;

        /// <summary>
        /// 🆕 2026-09-17：**只给对手 AI 的「跳动作」掷骰用**的随机源（原版 `TweakAvailableActions`
        /// 拿 `BattleManager.GetRandomInt` 掷，见 `资料/AI_原版反编译_0917.md` §三）。
        ///
        /// **为什么又开一路**（和 <see cref="ShowRng"/> 同一个理由）：AI 每步要掷 0~4 次骰，
        /// 若共用 <see cref="Rng"/>，**同一副牌在有 AI 掷骰 / 没掷骰时洗出的序列就不一样** ——
        /// 那些按种子写死期望值的自检会集体漂移（而且看不出是「AI 多掷了一次」造成的）。
        /// ⚠️ **同样是种子派生的** ⇒ **同一局照样可复现**；`System.Random` 而不是
        /// `UnityEngine.Random`（工程铁律：一局必须可复现）。
        /// </summary>
        public readonly Random AiRng;

        /// <summary>事件日志。自检断言、调试、将来接 UI 都读它</summary>
        public readonly List<string> Events = new List<string>();

        /// <summary>
        /// **结构化事件流**（谁在哪个格位做了什么）。和 <see cref="Events"/> 的分工：
        ///   · `Events`  —— 给人看的字符串日志，只增不减，用来读和排查
        ///   · `Signals` —— 给**表现层**看的，带格位/卡名/数值，用来决定「在哪播什么特效」
        /// 表现层用 <see cref="DrainSignals"/> 搬走（搬完即空，不用记游标）。
        /// </summary>
        public readonly List<BattleEvent> Signals = new List<BattleEvent>();

        /// <summary>
        /// 效果链深度。Rally 打死人 → Backlash → 又打到带忏悔的单位 → 再触发……
        /// 这类环**天生存在**（规则书 :237「同时触发」那节就是讲它的），
        /// 所以靠深度上限截断，而不是靠「保证不会发生」。超过 <see cref="MaxEffectChain"/> 就不再连锁。
        /// </summary>
        public int EffectChain;

        /// <summary>效果链最多连锁几层</summary>
        public const int MaxEffectChain = 8;

        /// <summary>
        /// 🔴 **四条动作队列**（2026-10-21 引擎线 `T03·c` 批 B 接线 · 原版
        /// `BattleManager` 的 `actionQueue(+0x350)` / `instantQueue(+0x358)` /
        /// `delayedInstantQueue(+0x360)` / `interruptQueue(+0x368)`）。
        ///
        /// 语义与判据 → `Core/ActionQueue.cs`（原语）与 `Core/ActionPump.cs`（泵点 / 动作外壳）。
        /// 🔴 **一局一份**（挂在对局上而不是静态）—— 同一进程里可以并行跑多局
        ///    （`RuleEngineTest` 的夹具就是几十个独立 `BattleContext`），静态会把它们的触发串在一起。
        /// ⚠️ `ActionQueue.Resolving` 就是原版 `resolvingActionFlag(+0x280)`：**正在结算一条动作**。
        /// </summary>
        public readonly ActionQueue ActionQueue = new ActionQueue();

        /// <summary>🔴 2026-10-21（引擎线 `T03·c` 批 A · `W59`）：**进场序号发号器** —— 单调递增，
        /// 一个 `BattleContext` 一份。语义与判据见 <see cref="UnitState.PlayOrder"/>。
        /// ⚠️ **只在「新单位真的落到棋盘上」那一刻**取号（`RuleCore.PlayCard` / `DeployFree` /
        /// `BuildPlayer`（督军）/ `EffectResolver.DoReanimate` / `TutorialScript.PlaceTroops` /
        /// 控制权归还）；⛔ 别在「算目标 / 造牌进手牌」这类**没入场**的地方顺手取 ——
        /// 那会把「进场序」稀释成一堆没人用的号（不影响正确性，但会让它与原版列表序对不上，
        /// 以后想按号反查「这是第几个进场的」就没法信）。
        /// </summary>
        int _playOrderSeq;

        /// <summary>取下一个**进场序号**。⚠️ 每调一次就前进一格 —— 只在确认要落场时调。</summary>
        public int TakePlayOrder() => _playOrderSeq++;

        /// <summary>
        /// **「强行触发」的深度**（2026-09-14 A4 批 4）。`&gt; 0` 时，
        /// <see cref="EffectCondition.EnergyZero"/> 那条条件**一律判成立**。
        ///
        /// **为什么需要它**：`Codex:` 的每个 op 都被 `EffectText` 挂上了
        /// `ConditionKind = EnergyZero`（规则书 `:175`「你的能量为 0 时触发效果」），
        /// 而「**强行**触发某个关键词的正文」（`Trigger the Codex ability of a friendly unit`，
        /// `Author of the Codex`）的**全部意义**就是「不等那个时机、现在就结算一遍」——
        /// 条件不绕过去 ⇒ 这个动词**等于没做**（`资料/战术卡剩余7条_语义查证.md` §八 点名的那个坑）。
        ///
        /// **✅ 2026-09-18 全量反编译复核：这条不再是「查不到」，而是「有据」**（原文只有半条）：
        ///   · `CardScript.TriggerOtherCardCodex` 整个方法体**只有**发一个 `AbilityTrigger.TriggeredCodex = 650`，
        ///     **没有任何能量判据**；
        ///   · 🆕 `AbilityLogic.CanPlayAbility`（`decomp_full/AbilityLogic__CanPlayAbility.c`，76 行）
        ///     **通篇没有能量判据** —— 它只判「有没有合法目标」（`AbilityLogic.GetTargets(…) > 0`）。
        ///   ⇒ **受力那条链上没有任何一处再判能量** ⇒ 我们「强行触发时绕过 `EnergyZero`」**与原版一致**，
        ///     不再是「我们挑的」。
        /// ⚠️ 顺带查清一条**原版真有、我们没建**的闸（如实记，别当成漏做）：
        ///   `CardScript.CanPlayWithCodexNow` —— 该单位**有默认 `codex` trait** 且**不带 `jam`** 时，
        ///   判据是 `BoardAnalysis.GetManaLeft == EntityScript.CurrentCost`（**剩余能量 == 这张卡的代价**，
        ///   即「付完正好到 0」）⇒ 与我们「打完恰好 0」**等价**，所以那半条也**对得上**。
        ///   `jam` = `DefinedTrait.jam = 130`。**不建模**：全池 **0 张卡**提到它、规则书里也没有 ⇒ 这道闸打不开。
        ///
        /// ⚠️ 用**计数器**不用 `bool`：强行触发结算途中可能又触发一次强行触发（`Duty's End`
        ///    的 `Backlash:` 正文本身就是一句强行触发），嵌套时不能提前复位。
        /// 用法：<see cref="RuleCore.BeginForcedTrigger"/> / <see cref="RuleCore.EndForcedTrigger"/>
        /// （**别直接改这个字段** —— 忘了配对就等于给后面所有效果开了后门）。
        /// </summary>
        public int ForcedTriggerDepth;

        /// <summary>🆕 2026-09-16 **免付费深度** —— 「触发某个能力、但**不花资源**」那一条路。
        ///
        /// 唯一用例：`ASH52 Cosmic Serpent` 的 `Trigger the abilities requiring Spirit Stones
        /// of all your troops`（中文「触发你所有部队需要灵魂石的能力」）。
        /// 卡面上这一族能力平时是 `N [Spirit Stone]: 正文`（**玩家主动激活时**要花 N 颗石头），
        /// 而这张卡是**触发**它们 —— 两者是不是同一件事，靠证据分：
        ///
        /// **原版出处（正面证据）**：触发路径的整个方法体里**没有付费、也没有余额检查** ——
        ///   `BattleActionType.triggerSpiritStone = 77` 的派发体（`BattleManager__ResolveAction.c`
        ///   的 `case 0x4d`）只有 4 个调用，无 `UseMana`/`UseSpiritStoneEnergy`；
        ///   `CardScript.TriggerSpiritStone`（`decomp_out/CardScript__TriggerSpiritStone.c:18`）
        ///   只做 `OnTrigger(600)`。**唯一的余额闸门**是「玩家从手牌主动打出」那条
        ///   （`CardScript__CanUseSpiritStone.c:32-39`），**不在这条链路上**。
        /// 🔴 **2026-10-07 更正（A120）：「三层都读不到」那套说法作废 —— 扣石点已查实。**
        ///   原文：「那玩家主动激活时到底在哪扣石」**三层都读不到**（`UseSpiritStoneEnergy` 全量
        ///   反编译里 0 个调用者、方法体是签名桩）—— 错了两处：
        ///     · 「**方法体是签名桩**」说的是**另一份东西** —— `d:/2/Warpforge_code/Scripts/Assembly-CSharp/`
        ///       那份**空体**签名桩集；`decomp_full/PlayerManager__UseSpiritStoneEnergy.c` **有 93 行真体**
        ///       ⇒ 它是**内联残留 / 死函数**（「0 个调用者」这半句仍成立），不是「这条链读不到」。
        ///     · 「**三层**」**全仓没有定义**（这个词组只此一处），而且**扣石那一步本来就查得到**：
        ///       **全库唯一**一处扣灵魂石 = `decomp_full/RawCardScript__TriggerAbility.c:42-53` ——
        ///       该分支 = `*(int*)(ability + 0x10) == 600 /*UseSpiritStone*/`（`:42`）
        ///       且 `op_Equality(thisCard, cardPlayed)` 成立（`:46`）⇒
        ///       `PlayerManager.UseMana(pm = GetPlayerManager(card.isPlayer /*+0x40*/), ability+0x20 /*数额*/,
        ///       5 /*ManaType.SpiritStone*/, 0, 0)`（`:49`/`:52`）。
        ///       · 全库 `UseMana(…, 5, …)` **只有这一处**（其余 4 个调用点传的都是 0）；
        ///       · 数额读的是 `ability+0x20` —— 与余额闸门 `CardScript__CanUseSpiritStone.c:52`
        ///         读的是**同一个字段**，自洽。
        ///       ⇒ 原版的**付费判据 = 「触发者就是这张卡自己」**（`thisCard == cardPlayed`）。
        ///     · 我们这边**语义等价**（都是「卡自己激活时扣、不够不动」，差别只在落点名字）：
        ///       扣款 = `RuleEngine/Core/RuleCore.cs` 的 `DoCostMore`（`ps.SpiritStones -= op.Cost`；不够则**不扣、不结算**，
        ///       记日志 + `unresolved`）· 触发 = `RuleEngine/Core/RuleCore.cs` 的 `PlayCard`（单位部署时，排在 `Rally` 之前）。
        ///   ⚠️ **只剩「原版实机跑一局看数字怎么变」核不了**（原版已关服 + 手牌注入 `inj FAIL: empty Data`）
        ///     —— 那**不是判据缺口**（铁律 2：反编译方法体排第一权威）。
        ///   ⇒ 「**触发不付费**」这条照旧**成立**；「**主动激活在哪里扣**」**已有据**。仍是**两件事**，别混。
        ///   判据全文 → `资料/普查产出_1007/波7判据核查.md` §A120。
        ///
        /// ⚠️ 同样是**计数器**（理由与 `ForcedTriggerDepth` 一致：结算途中可能又触发一次）。
        /// 用法见 <see cref="RuleCore.ResolveSpiritAbilityForced"/> —— **别直接改这个字段**。
        /// </summary>
        public int SuppressCostDepth;

        /// <summary>🆕 2026-09-16 **「再触发一次」正在跑**的深度（> 0 = 我们是那次额外触发）。
        ///
        /// 为什么要它：`extratrigger` 的额度是**监听者在广播里挂给事件主语**的，而那次额外触发
        /// **又会广播同一件事** ⇒ 监听者再挂一次 ⇒ 额度永远留着，下一次攻击白捡一次
        /// （静默、且看不出来）。所以 `DoExtraTrigger` 在这个深度 > 0 时**不挂额度**（只记一行日志）。
        /// 用法见 <see cref="RuleCore.TakeExtraTrigger"/> —— **别直接改这个字段**。
        /// </summary>
        public int ExtraTriggerDepth;

        /// <summary>🆕 2026-09-16 **本回合被「抢过来」的单位**（`takecontrol`）—— 回合末归还。
        ///
        /// 卡面只有一张：`GSC_Telephatic_Domination` 的
        /// `Take control of an enemy troop this turn and give it Fast`（中文「本回合控制一个敌方部队，
        /// 并给予它迅捷」）。
        /// **归属在我们这儿 = 这个对象待在谁的 `PlayerState.Board[]` 里**（原版就是一张牌上
        /// 可翻转的 bool，机器码 `+0x40`：`CardScript__SetAsPlayer.c:19` / `ChangeOwner.c:2-5`）
        /// ⇒ 「抢」= 换数组、「还」= 换回去。**时长**照卡面写死的 `this turn`；
        /// 原版的限时容器是 `CardEffect.untilEndOfTurn`（`0x32`）+ 回合末 `ClearEndOfTurnEffects`。
        /// ⚠️ 原版把这三件事串起来的调用点在 `ResolveStealMinion` 那个协程里、**本次导出没有** ⇒
        ///    「本回合末归还」是**照卡面 + 我们挑的**，别写成「原版就是这样」。
        /// </summary>
        public class TempControl
        {
            public UnitState Unit;
            /// <summary>原主（还给谁）</summary>
            public int Owner;
            /// <summary>抢来的那一回合（`ctx.Turn`）</summary>
            public int Turn;
            /// <summary>它原来在哪个格位。⚠️ **2026-10-01 起归还落点不再读它** —— 改成原版那条
            /// `GetNextSlotWithoutDisplacing`（人少的一侧最外一格、平手走右，见 `RuleCore.EndTurn`
            /// 的归还段）。留着只用于排查时看「它当初是从哪儿被抢走的」。</summary>
            public int Slot;
        }

        /// <summary>「抢来的单位」清单 —— 消费点只此一处：`RuleCore.EndTurn` 的归还那一段。</summary>
        public readonly List<TempControl> TempControls = new List<TempControl>();

        /// <summary>🆕 2026-09-16 **「它本回合内死了，就把这条效果转给另一个」**的监听。
        ///
        /// 卡面只有一张：`BL74 Spreading Corruption` 的
        /// `Give Vulnerable 4 to an enemy troop. If it dies this turn, apply this effect to
        /// another random enemy troop`（中文「给予一个敌方部队脆弱 4。若其本回合死亡，
        /// 将此效果施加于另一个随机敌方部队」）。
        ///
        /// **登记**在 `EffectResolver.DoGive`（那条 `give` 真打中的时候），
        /// **消费**在 `RuleCore.FlushDeathWatches`（死亡那一刻）。
        /// ⚠️ 盯的是**对象**（`UnitState`）不是卡名 —— 同名两张分得开（本版没有卡实例身份，
        ///    但对**场上**的牌来说对象身份就是身份）。
        /// ⚠️ 递归语义**三层零依据** ⇒ 我们定：**只转一次**（见 `FlushDeathWatches`）。
        /// ⚠️ 到期**不收表**（只在死亡/回合结束时清）—— 表很小，收表反而容易漏。
        /// </summary>
        public class DeathWatch
        {
            /// <summary>盯着谁（`give` 实际打中的那个对象）</summary>
            public UnitState Target;
            /// <summary>谁盯的（复述时按他的阵营算极性）</summary>
            public int Owner;
            /// <summary>只在**这一回合**内有效（卡面 `this turn`）</summary>
            public int ExpireTurn;
            public string Source;
            /// <summary>复述哪条效果</summary>
            public List<EffectOp> Ops;
        }

        /// <summary>「死了转给另一个」的监听清单 —— 消费点只此一处：`RuleCore.FlushDeathWatches`。</summary>
        public readonly List<DeathWatch> DeathWatches = new List<DeathWatch>();

        /// <summary>🆕 2026-09-16 **手牌上的「打出时才兑现」的加成**。
        ///
        /// 卡面只有一张：`TL53 Infinite Biomorphologies` 的
        /// `Choose an effect and give it to all troops in your hand`
        /// （中文「选择一个效果，给予你手牌中的所有部队」）。
        /// **登记**在 `EffectResolver.AttachHandEffect`（三个登记点共用它），
        /// **兑现**在 `RuleCore.PlayCard`（那张牌真打出来的时候，加成随它上场）。
        ///
        /// 🔴 **2026-10-18（`A885`）· 搬走了（铁律 5 的「就地改掉」）**：
        ///    原来这里放的是 `class HandBuff { Instance; Ops; Source; }` + `List&lt;HandBuff&gt; HandBuffs`
        ///    —— 一张**对局级**的表，兑现时要 `ReferenceEquals(h.Instance, inst)` 回查。
        ///    原版不是这个形状：效果**长在牌自己身上**（`CardScript.AddEffect(那张牌, cardEffect)`，
        ///    `PlayerHand__SetupCardInHand.c:71` · `PlayerHand__AddHandEffect.c:126-135`，
        ///    存在 `CardScript +0x108` 的 `List&lt;CardEffect&gt;`），打出时**跟着那张牌上场**
        ///    （`RemoveCardFromHand.c:20` 只把牌从 `currentHand` 里摘掉，不销毁不重建）。
        ///    ⇒ 存放处搬到 **`CardInstance.HandEffects`**（每个实例一份；旧名 `HandBuffOps` 等
        ///      四个标量现在是**只读兼容视图**），**这里不再有那张表**。
        ///    ⛔ **别再在对局表上重建一份** —— 两处各存一份 = 迟早不一致（铁律 §三）。
        ///    ⚠️ 过期与次数上限（原版 `PlayerHand.UpdateCardEffects` / `CardPlayedWithEffects`）
        ///      也落在实例那几个字段上，清扫点 = `RuleCore.ExpireHandBuffs`（`EndTurn` 里调）。
        ///    🆕 **2026-10-18（`A885` ②④）再订正两处**：
        ///      · **每条效果各带各的到期位**（`CardInstance.HandEffect`，一份牌可挂多条 ——
        ///        原版一条 `CardEffect` 一个到期位）；那四个标量视图只反映**最后挂上那条**。
        ///      · **「后进手牌的牌也吃上既有手牌效果」那一层做了**
        ///        （`RuleCore.SetupCardInHand`，原版 `PlayerHand__SetupCardInHand.c`）——
        ///        登记表是**现场扫手牌实例**扫出来的，⛔ **仍然没有对局级的表**。
        ///
        /// 📌 历史留痕（2026-09-18 当时的状态，只留结论）：原来按「卡 + 份数」记
        ///    （`CardDef Card; int Count;`）—— 那是「没有卡实例身份」时代的近似。</summary>

        /// <summary>只留最近 N 条，防长对局把内存吃满</summary>
        public int EventCapacity = 2000;

        /// <summary>
        /// **本回合阵亡了几个单位**。规则书 :189 / :201 那一族「每有 1 个阵亡就…」按它计数
        /// （`For each one that dies, heal 2 …`）。回合开始时清零。
        /// 我们上一版复刻 `rule_core.gd` 里没有这个字段，是照着**卡面语义**补的（卡面 = 判据③，⚠️ gd 只是旁证）。
        /// </summary>
        public int DiedThisTurn;

        /// <summary>
        /// 🆕 **2026-10-24（引擎线 `A1` · `UM77`）：上一个回合号里阵亡了几个单位**。
        ///
        /// <para>**为什么必须单独开一格（⛔ 不是改 `DiedThisTurn`）**：
        /// `cond 222 diedSinceLastTurn` 的原版判据是
        /// `turnDied == turnCounter || turnDied + 1 == turnCounter` ——
        /// **本回合或上一回合**（`CardScript__HasDiedSinceLastTurn.c:26-47`；
        /// `ConditionEval` 的 222 也是这么实现的）。而 `ctx.DiedThisTurn` 只覆盖**本回合**那一半。
        /// 消费者是 `for each … that died since your last turn`（今天只有 `UM77 Chairon`）——
        /// 少算那一半的表现 = **对手回合末被打死的兵不算**（少打一遍）。
        /// `DiedThisTurn` 自己的两份消费者（反噬 `TestDiedThisTurnDuringBacklash` /
        /// `for each one that dies`）要的仍然是「**本回合**」⇒ 语义一个字都没动。</para>
        ///
        /// <para>**写点唯一** = `RuleCore.BeginTurnCore`：`ctx.DiedLastTurn = ctx.DiedThisTurn;`
        /// 排在 `ctx.DiedThisTurn = 0;` **之前**（每开一个回合号滚动一次 ⇒
        /// 「上一回合」= `ctx.Turn - 1` 那个回合号里死的）。</para>
        /// </summary>
        public int DiedLastTurn;

        /// <summary>
        /// **本次结算**（一次 `ResolveOps`）里抽到的牌。`For each troop drawn …`
        /// 这种「前一句抽了牌、后一句按张数翻倍」的写法靠它。
        ///
        /// 为什么按「本次结算」而不是「本次抽牌」：卡面写的是
        /// `Draw 2 troops.` + `For each troop drawn …` 两条**分句**，
        /// 中间还隔着别的处理 —— 计数窗口得把整张卡罩住。
        /// 由 `ResolveOps` 在入口清零（一张卡的结算就是一次窗口）。
        ///
        /// 🔴 **2026-09-18 第 7 行第 3 步：元素类型 `CardDef` → `CardInstance`。**
        ///    这些是**指代槽**（`Draw it and lower its cost by 3` 的「它」）——
        ///    指代必须能定位到**具体哪一份**，否则手里两张同名卡会一起被降价/加价。
        /// </summary>
        public readonly List<CardInstance> DrawnThisResolve = new List<CardInstance>();

        /// <summary>
        /// **全卡池** —— `create` 造牌的候选来源（见 <see cref="CreatePool"/>）。
        ///
        /// 为什么要单独传一份：一局对战只看得到双方**牌库**，而造牌是按**阵营 + 兵种**从
        /// **整个卡池**里筛（`Create three Ultramarines Vehicles` —— 那 18 张载具不可能都在牌库里）。
        ///
        /// ⚠️ **null = 这一局没给卡池**。此时造牌**如实报「没有卡池」并什么都不做**，
        ///    绝不退化成「从双方牌库里抽」—— 那会悄悄造出卡面上没有的牌（红线：不许静默失败）。
        /// </summary>
        public IReadOnlyList<CardDef> CardPool;

        public BattleContext(int seed)
        {
            Rng = new Random(seed);
            // ⚠️ 派生种：**同一个 `seed` ⇒ 同一个派生种**（可复现），但与 `Rng` **互不干扰**
            //    （`ShowRng` 花掉多少个数都不会挪动 `Rng` 的序列）。
            ShowRng = new Random(seed ^ 0x5EED5EED);
            // 🆕 2026-09-17：AI 掷骰那一路（见 `AiRng` 的注释）—— 同样派生、同样可复现、同样互不干扰
            AiRng = new Random(seed ^ 0x0A1A1A1A); InitRandomNumbers();   // 🔴 2026-10-27（`E1RNG`）原版开局预烧 100 个整数 —— 见类尾 `InitRandomNumbers`（同一条语句，⛔ 别拆成新行：本文件被账按行号引用）
        }

        // ---- 卡实例身份（待办第 7 行，2026-09-18 第 1 步）---------------------------
        //
        // 🔴 **全对局只有这一处发实例号**（`CardInstance.Id`）。口径：
        //   · **每局从 1 开始**（计数器挂在 `BattleContext` 上，不是静态的）⇒ 同一个种子跑两次，
        //     实例序号序列**逐位相同** —— 日志/报表里出现 `#id` 也不会让「同种子同一局」失效。
        //   · 用**普通整数计数器**，**不用 `Guid`、不用 `UnityEngine.Random`**（红线：可复现）。
        //   · 没有 `BattleContext` 的场合（测试自建棋盘 / 演示探针）走 `CardInstance.Detached`
        //     （**负数 id**，永远不和这里的正数撞号）—— 见那个方法的注释。
        int _nextInstanceId = 1;

        /// <summary>
        /// 发**一份新的牌**（手牌/牌库/弃牌堆/场上存的就是它）。
        /// 每一次「新造一张」都要调它一次：初始牌库 · 抽牌那一刻 · `create a copy` · 造衍生物 · 天赋生成。
        /// ⚠️ **「把手上的这一份挪到别处」不调它** —— 那种情况**沿用同一个 `CardInstance`**
        ///    （用户 2026-09-18 拍的口径：回手 / 洗回牌库默认保留实例态，见 `CardInstance.cs` 文件头）。
        /// </summary>
        public CardInstance NewInstance(CardDef card)
        {
            var inst = new CardInstance(card, _nextInstanceId++);

            // 🆕 2026-10-18（`A985④`）**「这份牌是谁造出来的」** —— 见下面 `_createdBy` 那一段。
            //   `ActingUnit` 优先（每次 op 都 save/restore，出了结算必为 `null` ⇒ 它是**结算中**的
            //   可靠信号；这个「先 `ActingUnit` 再回落」的次序是照本仓既有口径 ——
            //   `EffectResolver` 里那两处 `ActingUnit != null ? ActingUnit.Card : PlayingCard` 就是同一个写法）；
            //   战术卡那条路 `source` 是 `null`（`EffectResolver` 里那处传的是
            //   `ResolveOps(ctx, p, null, …)`）⇒ 靠 <see cref="CreationSource"/>（出牌 / 结算作用域里
            //   save/restore 的那张来源卡，见那个字段的 doc）。
            //   ⚠️ 只在**有来源**时盖章：开局那几处（初始牌库 / 督军 / 起手 / 防御卡）跑在任何
            //   出牌作用域之前 ⇒ 来源是 `null` ⇒ 不盖章（与原版一致：那几条路从不设 `createdBy`）。
            //   🔴 **2026-10-22（`W62/D5②`）就地订正 —— 原来这里回落到 `PlayingCard`**：
            //      那个字段**从不清空**（见 <see cref="PlayingCard"/> 与 <see cref="CardPlaySource"/> 的 doc：
            //      写点只有 `NotePlayed` 与 `ResolveOps` 的出牌重载，出完结算**留在原地**）
            //      ⇒ 任何**不在结算期**新造的实例（抽牌 / 回合起补牌 / 天赋生成…）都会被盖上
            //      「本局最后打出那张卡」的名字 —— 真机上就是「萨姆-罕的牌上写着兽人 Goff 计策的名字」。
            //      `CreationSource` 是 save/restore 的（出了作用域一定还原成 `null`）⇒ 结构上不可能泄漏。
            var creator = ActingUnit != null ? ActingUnit.Card : CreationSource;
            if (creator != null) _createdBy[inst.Id] = creator;

            return inst;
        }

        /// <summary>本局已经发出去几份（报表 / 断言用；它等于「发过的最大编号」）。</summary>
        public int InstanceCount { get { return _nextInstanceId - 1; } }

        // ---- 🆕 2026-10-18（`A985④`）「这张牌是谁造出来的」 -----------------------------
        //
        //  原版把这件事记在**牌自己身上**：`CardScript.createdBy { get; set; }`
        //  （`Assembly-CSharp/CardScript.cs:1518`，实例字段 = `+0x268`，类型 = **`CardScript`**）。
        //  写点**只有三处**，全是「有东西把它造出来」的那一刻：
        //    · `BattleManager__AddNewCardToHand.c` —— 效果造一张牌**进手牌**（`param_3` = 造它那张卡）
        //    · `BattleManager__AddNewCardToDeck.c` —— 效果洗一张牌**进牌库**（同上）
        //    · `BattleManager._ResolveSummonUnit_d__510__MoveNext.c:112` —— **召唤**出来的单位
        //      （`*(summoned + 0x268) = *(param_1 + 0xf8)`）
        //  ⇒ **三处的来源都是「正在结算的那张卡」**（`AbilityLogic__PlayAbility.c` 传的是技能属主
        //    那张卡；`CardScript__ProcessCompanion.c` 传的是它自己）—— 这就是本条取
        //    `ActingUnit`（正在结算的那个单位）＋ <see cref="CreationSource"/>（正在跑的那次出牌/结算
        //    的来源卡）当依据的原因。🔴 **两者都是 save/restore 作用域内的量**；
        //    ⛔ 别再用 `PlayingCard`（它**从不清空** ⇒ 会盖错名字，见 <see cref="NewInstance"/> 那段）。
        //
        //  🔴 **消费面只有一处**：`BattleCardUI.DisplayCreatedByText`（卡面那行小字）→
        //    `SupportMethods.GetCreatedByText(创建者卡名)` =
        //    `GetTranslation("Battle/HUD/CreatedBy").Replace("{0}", 名字)`
        //    （`SupportMethods__GetCreatedByText.c`）⇒ 显示层要的只是**创建者的本地化卡名**。
        //
        //  🔴 **为什么记在对局表上、而不是 `CardInstance` 上加一位**（形态是这一条的选择，理由写死）：
        //    本笔的白名单只开到 `BattleContext.cs` / `UnitState.cs`（`Core/CardInstance.cs`
        //    不在这笔活的范围内，那边 2026-10-18 刚被 `A885`/`A886` 动过）。键 = `CardInstance.Id`
        //    （**每局从 1 起、同种子同序号、可复现** ⇒ 这张表不引入新的不确定性）。
        //    ⚠️ 与「别在对局表上重建一份」那条教训**不冲突**：那条说的是**已有住所**的字段
        //       （手牌效果在 `CardInstance.HandEffects`）不许再抄一份；这一项**本来就没有住所**。
        //    ⛔ 将来若把这一位搬上 `CardInstance`（更贴原版），**要连同这一段一起搬**，别两处各存一份。
        readonly Dictionary<int, CardDef> _createdBy = new Dictionary<int, CardDef>();

        /// <summary>这一份牌是**谁造出来的** —— 原版 `CardScript.createdBy` 的对应物。
        /// `null` = 不是被造出来的（初始牌库 / 抽到的牌 / 督军…）。</summary>
        public CardDef CreatedByOf(CardInstance inst)
        {
            if (inst == null) return null;
            CardDef src;
            return _createdBy.TryGetValue(inst.Id, out src) ? src : null;
        }

        /// <summary>这一份牌是不是**被造出来的**（显示层判据用；原版是 `creator != null`）。</summary>
        public bool HasCreatedBy(CardInstance inst) { return CreatedByOf(inst) != null; }

        /// <summary>这局一共有几份牌「有来源」（自检 / 排查用；这张表本身不参与指纹与录像）。</summary>
        public int CreatedByCount { get { return _createdBy.Count; } }

        /// <summary>
        /// 🆕 **2026-10-22（`W62/D5②`）「这次出牌 / 这次结算」的来源卡** ——
        /// <see cref="NewInstance"/> 盖章时的**第二顺位**（第一顺位 = <see cref="ActingUnit"/>）。
        /// `null` = **不在任何出牌 / 结算作用域里**。
        ///
        /// <para>**为什么必须有它、又为什么不能拿 `PlayingCard` 顶替**：`PlayingCard` 那个字段
        /// **从不清空**（本来只是「常驻效果登记」记的来源，写点 = `NotePlayed` 与 `ResolveOps` 的出牌重载）
        /// ⇒ 任何**不在结算期**新造的实例（抽牌 / 补牌 / 天赋生成…）都会读到「本局最后打出那张卡」，
        /// 把它当成创建者盖上去 —— 真机就是「萨摩-罕的牌上写着兽人 Goff 计策的名字」
        /// （`资料/普查产出_1022/W62_缺陷诊断.md` §D5·3）。本字段**只在作用域内非 null**，
        /// 结构上不可能泄漏到作用域外。</para>
        ///
        /// <para>**写点（全部 save/restore —— `try { … } finally { CreationSource = prev; }`，⛔ 别在别处写）**：
        /// ① `RuleCore.PlayCard`（单位卡打出整段：覆盖 `SpawnTideCopies` / `PlayCompanions` 那两条
        ///    「紧接着造牌、但还没进任何 `ResolveOps`」的路 —— 原版 `ProcessTide` / `ProcessCompanion`
        ///    正是把**打出的那张牌**当 `param_3` 传给 `AddNewCardToHand`）；
        /// ② `EffectResolver.PlayTactic`（战术卡整条结算，含它在 `ActionScope` 里泵出去的队列体）；
        /// ③ `EffectResolver.PersistentListenerDeferred`（常驻效果 / 手牌监听：来源 = 写出这条效果的卡
        ///    `PersistentEffect.Source`，与它那边 `CardPlaySource` 同一份判据、同一段 `try/finally`）。</para>
        ///
        /// <para>⚠️ **优先级仍是 `ActingUnit` 更高**（原版 `createdBy` 吃的是 `AbilityLogic.PlayAbility`
        /// 里的 `AbilityData.thisCard` = **能力属主**）：一次出牌期间别的单位触发自家能力造牌时，
        /// `ActingUnit` 是那个单位（= 它的能力属主）⇒ 必须压过本字段（否则会盖成「被打出的那张」）。</para>
        /// <para>⚠️ **已知残差（如实标着，未修）**：`EffectResolver.ResolveDeploy` / `ResolveAtTurn` 那几条
        /// 常驻作业把「刚部署的那个单位」当 `source` 传给 `ResolveOne`（那是本工程为代词/相邻锚点做的
        /// 结构选择，见 `FactionSourceCard` 的 doc）⇒ 那些路上 `ActingUnit` **是事件宾语、不是能力属主**，
        /// 由常驻效果造出来的牌仍会盖成「被部署的那个单位」。修它要动那三条路的 `ResolveOne` 入口，
        /// 与 `FactionSourceCard` 同一笔活（不在本笔范围）—— 记在报告的「没查清」。}</para>
        /// </summary>
        public CardDef CreationSource;

        /// <summary>把「当前来源卡」压进去，**返回上一份** —— 调用点必须
        /// `try { … } finally { ctx.CreationSource = prev; }`（见 <see cref="CreationSource"/> 的写点清单）。
        /// ⚠️ 单独包一个方法是**为了不留「改了没还原」的半成品**：读点只有 `NewInstance` 一处。</summary>
        public CardDef PushCreationSource(CardDef src)
        {
            var prev = CreationSource;
            CreationSource = src;
            return prev;
        }

        /// <summary>安全地取「第 <paramref name="p"/> 方的第 <paramref name="handIdx"/> 张手牌是哪张卡」
        /// （越界 / 空位 ⇒ `null`，⛔ 不抛）。两个出牌入口（`RuleCore.PlayCard` / `EffectResolver.PlayTactic`）
        /// 用它给 <see cref="CreationSource"/> 种值 —— ⛔ 它**不参与任何规则判定**（判定在各自核心里）。</summary>
        public CardDef HandCardOrNull(int p, int handIdx)
        {
            if (Players == null || p < 0 || p >= Players.Length) return null;
            var hand = Players[p].Hand;
            if (hand == null || handIdx < 0 || handIdx >= hand.Count) return null;
            var inst = hand[handIdx];
            return inst != null ? inst.Card : null;
        }

        // ---- 🆕 2026-10-18（`W4` 整改 · 审查问题 6）「同一次手牌效果挂载」的记录号 ----

        int _nextHandEffectRecordId = 1;

        /// <summary>
        /// 发一个**「同一次手牌效果挂载」的记录号** —— 一次调用发给 N 张牌，N 张身上那 N 条
        /// 共用这一个号（原版是**一个 `HandEffect` 记录 / 一个 `CardEffect` 对象**发给 N 张牌，
        /// `PlayerHand__AddHandEffect.c:112-139`）。用途与该号为什么必须在：
        /// <see cref="CardInstance.HandEffect.RecordId"/>。
        /// ⚠️ **跟着 `BattleContext` 走**（每局从 1 开始）⇒ 同种子必然同序号（对局可复现）；
        ///    ⛔ 别用 `Guid` / `UnityEngine.Random`。
        /// </summary>
        public int NextHandEffectRecord() { return _nextHandEffectRecordId++; }

        // ---- 🆕 2026-10-19（第五十五会话 · `A1567`）本局已挂过几件附魔 ----------------
        //
        // 原版 `BattleManager.enchatmentsUsed // +0x4E4`（`dump.cs`；`BattleManager__get_enchatmentsUsed.c`
        // 读它、`BattleManager__UsedEnchantment.c` 就是 `+0x4e4 += 1`）。
        // 写点只有一个：`CardScript__AddEnchantment.c:44` —— 新附魔的 `enchantmentCardID`
        // **取的是这一刻的值**，紧接着 `:48` 调 `UsedEnchantment` 自增
        // ⇒ 本局第 1 件附魔的 id 是 **0**。
        // 跟着 `BattleContext` 走 ⇒ 每局从 0 开始、同种子同序号（对局可复现，⛔ 不用 Guid / 随机数）。
        // ⚠️ 它**不是**「卡 id」—— 别拿它当 `CardDef.Id` 用（见 `ActiveEnchantment.EnchantmentCardId`）。
        public int EnchantmentsUsed;

        public PlayerState ActivePlayer { get { return Players[Active]; } }
        public PlayerState Opponent { get { return Players[1 - Active]; } }

        public bool IsOver { get { return Winner != 0; } }

        /// <summary>
        /// **加时（Overtime）**。
        /// 判据（第一权威）= 原版 `BattleManager._NextTurn_d__395__MoveNext.c:191-207`：
        /// `overtimeTurn.HasValue && overtimeTurn &lt;= turnCounter`，其中 `turnCounter`
        /// = **全局回合计数器**（`+0x3F8`，两人共用、每回合 +1，`dump.cs:30871`）。
        /// ⇒ **每回合开始判一次 `ctx.Turn &gt;= Vars.overtimeTurn`，判过就不再判**。
        ///
        /// 🔴 **2026-10-20 就地订正（铁律 5 · `W58-TURN-B`）**：这段注释原来写的是
        ///    「每回合开始判一次 `Players[SecondSeat].MaxEnergy >= Vars.overtimeTurn`」——
        ///    **那是已被推翻的旧实现口径**（按能量）。`W58-TURN-A` 已把实现改回原版的**全局回合数**
        ///    （`RuleCore.BeginTurn`：`ctx.Turn >= overtimeTurn`），注释同批订正。
        ///    ⚠️ 附带的两句旧话也一并作废：①「遭遇模式取 `null`（没有加时）」——
        ///    遭遇今天也是 10；②「判据是用户 2026-09-17 给的『后手最大能量达 10』」——
        ///    那是**当时的用户口径**，现行实现与判据都按原版全局回合数（口径历史与订正留痕
        ///    → `GameplayVariables.overtimeTurn` 的字段注释）。
        /// ⚠️ **`null` ⇒ 这一局永不进加时**（原版拿 `null` 表达「服务器不下发」）。
        /// ⚠️ **与回合时钟没有任何关系** —— 原版那一段里一个 `ClockManager` 调用都没有
        /// （`BattleManager._NextTurn`，见 `资料/加时与冲突模式_原版规格.md` §1.1）。
        /// 唯一写入点 = `RuleCore.BeginTurn`。
        /// </summary>
        public bool IsOvertime;

        /// <summary>
        /// 🆕 2026-09-26：**这一局的参数**（经典 / 遭遇…）—— 逐字段见 <see cref="GameplayVariables"/>。
        /// 🔴 **所有「按模式会变」的判据都从它读**（起手张数 · 手牌上限 · 能量增长 · 加时阈值 ·
        /// 督军生命增减 · 换牌开关…）—— **不许在别处再写一份按模式分支的 `if`**
        /// （两处写同一条规则 = 迟早不一致，这是这工程的旧账）。
        /// 默认 = 经典 ⇒ 老的调用方（`RuleEngineTest` 那些）**一个字都不用改**。
        /// 🔴 **2026-09-26：默认值也存「副本」**（`.Clone()`）—— 见 `RuleCore.NewBattle` 那段：
        ///    `Vars` 是 public 可变字段，而 `GameplayVariables.Classic` 是**缓存单例**
        ///    ⇒ 直接存它的话，谁改一下 `ctx.Vars` 就**污染之后每一局**。**别把它改回共用那份。**
        /// </summary>
        public GameplayVariables Vars = GameplayVariables.Classic.Clone();

        /// <summary>
        /// 🆕 2026-10-17（B29）：**教程关卡执行器**（原版 `BattleManager.aiScriptedmanager`，字段 **+0x250**）。
        /// `null` = **不是教程局** —— 单机普通对局 / 遭遇 / 联机 / 回放**一律是 null**，
        /// 所有读它的地方都必须按「null ⇒ 老行为」写（本工程那条纪律：换上来不许改变任何既有行为）。
        ///
        /// 它同时是**两份东西的载体**：
        ///   · **关卡数据**（`TutorialScript.Stage`：先手 / 起始单位 / 起始手牌 / 初始法力 / `playerAlwaysWins`…）；
        ///   · **本局的指针状态**（`Turn` / `ActionCounter`）—— **一局一个实例**，不进任何静态缓存。
        ///
        /// 🔴 **谁写它**：只有 `RuleCore.NewBattle(…, tutorial:)`（**一处**）。
        /// ⛔ **别在 `BattleContext` 的构造函数里建**（那是「每个 ctx 都有教程」的意思，反了）。
        /// </summary>
        public TutorialScript Tutorial;

        /// <summary>
        /// 🆕 2026-10-19（T11 · G-4）：**战役关卡数据**（原版 `PlayerDataManager.currentCampaignStage`）。
        ///
        /// 🔴 **类型与教程关是同一个**（原版就是这样）：`PlayerDataManager.campaignSets` 里的
        ///    `CampaignSet.campaignStages` 是 `AssetReferenceTyped<TutorialStage>[]`
        ///    （`dump.cs:119050`），与 `tutorialStages` 同型 ⇒ 这里也用 <see cref="TutorialStageData"/>。
        ///
        /// 唯一读点 = <see cref="RuleCore.ResolveEndTurnEnchantment"/>（原版
        /// `BattleManager__ResolveEndTurnEnchantment.c`：读关卡 `+0x78 playerTurnEndEnchantment` /
        /// `+0x80 enemyTurnEndEnchantment`）。
        ///
        /// 🔴 **今天恒 `null`**：本工程**没有战役关卡数据源**（与 `Enchantments.SetupInitial` 那条
        ///    「没有战役关卡数据」是同一件事；`GameMode.Campaign` / `CutScene` 全工程只有枚举与派发表，
        ///    战役页 `CampaignTab` 是奖励页、不开战）⇒ 唯一的输入口留在这里，将来接战役时**只从这里进**，
        ///    ⛔ 别在别处另开一条「回合末挂附魔」的路（两处写同一条规则迟早不一致）。
        /// ⚠️ **教程局不走这一格**：原版那条调用被 `IsCampaignLike`（`matchType ∈ {130,190}`）挡在外面
        ///    （`BattleManager__ResolveEndTurn.c:434` 的 `if`），教程关的两个 PPtr 实测也全空
        ///    （`tutorial_stages.json` 6/6 = `m_PathID:0`）。
        /// </summary>
        public TutorialStageData CampaignStage;

        // ==================================================================
        //  🆕 2026-10-15（A383）：**这一局真正的模式号**（原版 `MatchData.playMode`）+
        //  由它派生的 `MatchType`（原版 `MatchData.matchType`）
        // ==================================================================
        //  原版这两个字段**并排**躺在 `MatchData` 上（`dump.cs` 字段序：`playMode` @0x18 ·
        //  `matchType` @0x1C），而 `matchType` **不是调用方给的** —— `MatchData..ctor`
        //  （RVA 0x736B70）里拿 `playMode` 去**查一张 14 项跳表**（跳转点 VA 0x180736CAE，
        //  表在 RVA 0x736FE0）算出来。判据全文 → `资料/普查产出_1014/RO_战场与窗口判据三件.md` §二。
        //
        //  🔴 **为什么要搬进来**：在此之前本局模式**只有二值**（`Ctx.Vars.IsSkirmish ? Skirmish : Classic`）
        //  —— 那是「用经典参数还是遭遇参数」那一问，**不是**「这一局是哪个模式」。
        //  真正知道模式的是**入口窗**（原版 `IPlayEvent.EventPlayMode`，`MatchMakerManager.FindMatch`
        //  从 `currentEvent` 取它），而它此前**一个字都没传进对局**。
        //  ⛔ **别把 `PlayMode` 与 `Vars` 混成一个**：`Vars` = 数值参数（30/12 张那一套，判据是
        //  **这副牌自己**的 `gameMode`，见 `BattleDriver.BeginFromDeckLibrary` 那段）；`PlayMode` =
        //  入口窗的模式号（原版两个字段也是分开的）。14 档里只有 `Skirmish 13` 用遭遇那套参数
        //  （`GameplayVariables.For` 是「模式号 → 参数」的唯一一处）。

        /// <summary>本局**入口的模式号**（原版 `MatchData.playMode` @0x18）。15 档见
        /// <see cref="RuleEngine.GameMode"/>。由入口窗声明、经 `BattleDriver` 进来；
        /// **没声明时**退回「这副牌自己带的模式」（判据与 <see cref="Vars"/> 同源，见 `BattleDriver.Begin`）。</summary>
        public GameMode PlayMode = GameMode.Classic;

        /// <summary>本局的 <see cref="MatchType"/> —— **由 <see cref="PlayMode"/> 派生**（只读，别自己赋）。
        /// 与 <see cref="PlayMode"/> 是**两个字段、两张表**（原版也是两个字段），⛔ 别拿它当模式号用。
        /// ⚠️ **这一格是「没翻转」那个值**（原版 `MatchData.matchType` 在 `SetBotOpponent` 里被就地改过）
        ///    —— 判「是不是 bot」那几档要读 <see cref="RuleEngine.MatchTypes.Effective"/>。</summary>
        public MatchType MatchType { get { return RuleEngine.MatchTypes.For(PlayMode); } }

        /// <summary>🔴 🆕 **2026-10-19（`A1070`）：本局哪一个座位是电脑**（`-1` = 这局没有电脑 ⇒
        /// **老行为**：双方都按真人算）。
        ///
        /// **为什么要有这个字段**：原版给「后手那一方」发防御卡时（`BattleManager.AddGoesSecondCardToDeck`）
        ///   **电脑侧恒不从电脑卡组取**（预组牌的 `DeckAndWarlordData.defensiveCard` 恒 null，两条反汇编硬证），
        ///   改从「后手方阵营防御池随机」（`matchType ∈ {50,80,110,120}`）或「督军自己的
        ///   `goSecondCardInHand`」拿 —— **分成哪一路全看 `matchType`**，而 `matchType` 又取决于
        ///   「这局有没有电脑」（`MatchData.SetBotOpponent` 会把它翻转一次）。
        ///   而引擎里此前**根本没有「这方是电脑」这个概念**（`NewBattle` 只是 `BuildPlayer(…, "P1"/"P2")`）。
        ///
        /// **谁写它**：入口窗 —— 经 `RuleCore.NewBattle(…, botSeat:)`（**开局就给**），或之后直接赋本字段
        ///   （⚠️ 那样只对**换牌阶段开着**的对局有效：防御卡要等 `EndMulligan` 才发）。
        ///   单机时座位 1 由 `SimpleAI` 驱动、联机时座位 1 是真人 —— **引擎自己判不出来**，只能由调用方给。
        ///
        /// 🔴 **「这一方是不是电脑」的唯一判据是 <see cref="IsBotSeat"/>** —— ⛔ 别在别处再写一遍比较
        ///   （两处写同一条规则 = 迟早不一致）。</summary>
        public int BotSeat
        {
            get { return _botSeat; }
            set
            {
                // 🔴 **后设的护栏**（不许静默失败）：换牌阶段**已经关了** ⇒ 后手那张防御卡按原版时机
                //    （`StartBattlePhase` → `AddGoesSecondCardToDeck`）**早就发过了**，此刻才改这一格
                //    **改不动它** ⇒ 电脑侧会**静默留着旧口径**（用卡组里那张）。出声，别让它悄悄发生。
                //    判据条件：这局换牌已关 + 该座位手里已经有一张 `defence` ⇒ 那一张多半就是这么来的。
                if (value >= 0 && _botSeat < 0 && !MulliganOpen && value < Players.Length
                    && Players[value] != null)
                    for (int i = 0; i < Players[value].Hand.Count; i++)
                        if (Players[value].Hand[i] != null && Players[value].Hand[i].Card != null
                            && Players[value].Hand[i].Card.Type == "defence")
                        {
                            UnityEngine.Debug.LogWarning("[Rule] **现在才**设 `ctx.BotSeat` —— 这一局"
                                + "换牌阶段已经关了，后手那张防御卡**已经按旧口径发过**"
                                + "（从卡组取的那张）⇒ 改不动它了。要让它生效，得在建对局时就给"
                                + "（`RuleCore.NewBattle(…, botSeat:)`）。⛔ 别以为设一下就好了");
                            break;
                        }
                _botSeat = value;
            }
        }
        int _botSeat = -1;

        /// <summary>这个座位是不是电脑（判据只此一处，见 <see cref="BotSeat"/> 的注释）。</summary>
        public bool IsBotSeat(int seat) { return BotSeat >= 0 && seat == BotSeat; }

        /// <summary>
        /// **开局换牌阶段**（原版 `MulliganManager` + `PlayerHand.AddCardsToMulligan`；
        /// 规则书 :46「换牌（Mulligan）| 可弃回任意起手牌后重洗补抽」）。
        ///
        /// `NewBattle(openMulligan: true)` 时开、`RuleCore.EndMulligan` 关。
        /// ⚠️ **换牌只能在这个阶段做**（`RuleCore.Mulligan` 会检查它）—— 对局开打之后再换就是改牌堆。
        /// </summary>
        public bool MulliganOpen;

        // ==================================================================
        //  🆕 2026-09-29（§25）：**进攻卡 / 防御卡的选择**
        // ==================================================================
        //  原版位置：`matchData.playerEnviromentalEffect(+0x90)` / `enemyEnviromentalEffect(+0x98)`
        //  （`dump.cs:78153-78154`）。时机 = **换牌之后、战斗开始之前**
        //  （`FinishMulliganFirstPhase` → `SetupEnviromentalEffectPhase` → `ChooseCardMenu.Setup`）。
        //  · **先手方**选**进攻卡**，列表 = `[空卡, 进攻1..3]`（空卡固定在**下标 0**）
        //  · **后手方**选一张**防御卡**（3 张、**没有空卡**）
        //  · AI 那侧 = `AI.GetAiEnvEffectCard(list)`：**均匀随机**，**可能抽到空卡**
        //    （`AI__GetAiEnvEffectCard.c:15-36`）
        //  🔴 **生效时机 = 每场只跑一次**：`_ApplyOffensiveAndDefensiveEffects` 的唯一调用点是
        //  `FinishMulliganFinalPhase`（战斗开始、洗牌之前），**不在回合结算里** ⇒ 别写成「每回合结算时」。
        //  ⚠️ 卡面上那个 `PlayedThirdCardInTurn` 是**卡的静态能力**（`AbilityTrigger==15`），
        //     由 `OnCardPlayedWithTarget` 在打牌时触发 —— **每回合可以再触发**，与上面那一次无关。

        /// <summary>进攻卡选的**槽号**：**-1 = 「不使用进攻卡」**（那张 `Normal Conditions` 一族）。
        /// 原版 `BattleHud.isEmptyOffensiveCard` 判的就是「id == 空卡的 id」。</summary>
        public int OffensiveSlotIdx = -1;
        /// <summary>那张卡对应的**环境 SO 名**（战场侧 `ApplyEnvironment` 要的那一条）。
        /// **空串 = 不改环境**（原版 `d__337:93-96` 早退那条路）。空卡那一路用的是本阵营的
        /// `defaultEnviromentalEffectVFX`，由调用方填进来。</summary>
        public string OffensiveEnvSO = "";
        /// <summary>谁选的（= **先手方**；原版两个字段按 `playerGoesFirst` 取谁的那一份）。</summary>
        public int OffensiveSeat = -1;
        /// <summary>后手方选的防御卡槽号（-1 = 没选 / 没有）。</summary>
        public int DefensiveSlotIdx = -1;
        /// <summary>进攻卡这一段**选完了吗**（原版那一段是异步的：面板关上才算数）。</summary>
        public bool OffensiveChosen;

        /// <summary>
        /// **本回合已打出的牌张数** —— 原版 `BattleManager + 0x3D0` 那个 `ObscuredInt`
        /// （`d:/2/tools/decomp_full/BattleManager__CardsPlayedInTurn.c:16-25` 读的就是它；
        ///  `_ResolvePlayCardFromHand_d__447__MoveNext.c:1390-1406` 是唯一的 `op_Increment` 写点）。
        ///
        /// 写点 = `RuleCore.PlayCard`（单位支）/ `RuleCore.PlayTactic`（战术支）各一处 ——
        ///   都在「校验过、费用付掉、手牌已拿走」之后 ⇒ **打不出去的牌一张都不记**；
        /// 清点 = `RuleCore.BeginTurn`（原版 `_NextTurn_d__395__MoveNext.c:231` 写 0）；
        /// 读点 = `AbilityTrigger.PlayedThirdCardInTurn`（`RuleCore.FirePlayedThirdCardInTurn`，
        ///   **只认 `== 3`**，见那边文件头）。
        /// 🔴 这张计数只数**从手牌打出的牌**（每张成功结算的单位/战术 +1），
        ///   与「本局打出过的牌」(`PlayedCards` / `NotePlayed`)**不是同一件事**。
        /// </summary>
        public int CardsPlayedInTurn;

        /// <summary>
        /// **上一句效果打中的那个单位** —— 供文本里的 `it` / `them` / `the target` 指代。
        ///
        /// 为什么放在 ctx 上：这是**跨分句**的上下文（`Deal 3 damage to an enemy. If it dies, draw a card.`
        /// 两个分句要串起来），不是某一个单位的属性。上一版复刻用 `rule_core` 的 `it_target` / `_last_pick`
        /// 两个变量记同一件事（`:2730`），我们合成一个。
        /// ⚠️ 它**可能已经死了/已经不在场上**（`If the target dies` 就是要判这个），所以取用方必须判活。
        /// </summary>
        public UnitState LastTarget;

        /// <summary>
        /// **上一条效果影响到的**那一批单位**（`Deal 2 damage to all units and give **them** Blind`）。
        ///
        /// 和 <see cref="LastTarget"/> 的分工：`it` / `the target` 指**一个**，`them` 指**一批**。
        /// 只记 `LastTarget` 会让 `give them X` 落到「己方全体」那个近似上（上一版复刻 `rule_core.gd:3137`
        /// 自己标着「近似」）—— `Deploy 3 Grot and give **them** Vanguard` 就会给错人。
        /// 由 <see cref="RuleCore.ResolveOps"/> 那条路（`ResolveTargets` / `DoDeploy`）写。
        /// </summary>
        public readonly List<UnitState> LastTargets = new List<UnitState>();

        /// <summary>
        /// 🔴🆕 **2026-10-25（执行代理 `E4` · `E4-1`）：一条已经发出去的 `EvtKind.Trigger`，
        /// 它的**目标**要等这条 ability 真的选出目标之后才填得出来。**
        ///
        /// <para><b>为什么需要「事后再填」而不是发的时候就知道</b>：原版
        /// `RawCardScript.TriggerAbility` → `AbilityLogic.PlayAbility` **自己就算目标**
        /// （`AbilityLogic__PlayAbility.c:257-264`：`abilityLogic.targetCriteria(+0x28)` →
        /// `AbilityLogic.GetTargets(...)` → 存进 `possibleTargets(+0x78)` →
        /// `PlayAnim(logic, manager, abilityData, possibleTargets)`），
        /// 而 `PlayAnim.c:171-248` 是**按这个列表逐目标**各 `AddCardAnim` 一次
        /// （`AbilityLogic__GetPos(manager, animData, 目标, 位置选项)` 算落点）。
        /// 我们这边「广播（发 `EvtKind.Trigger`）」和「结算（`ResolveOps`）」是**两拍**
        /// （`EmitAtTurnTriggers` 整趟发完 → 再 `EnqueueAbility` 入队），发的那一刻目标还没选。</para>
        ///
        /// <para><b>做法</b>：发射体把**它自己刚发的那个 `BattleEvent` 对象**放进这一格（arm），
        /// 结算那一拍由 <see cref="EffectResolver.ResolveTargets"/> 的**唯一收口**
        /// 记下**第一个**落到的目标（<see cref="TriggerTargetSinkFirst"/>），收尾再写回事件。
        /// 因为 `BattleContext.Signals` 存的就是**同一个对象引用**、表现层要等整段引擎调用返回
        /// 才 `DrainSignals`，所以事后填的字段**一定能被表现层看到**（不重排、不多发一条事件）。</para>
        ///
        /// <para>⚠️ **原版是「每个目标各播一份」，我们的事件只能带 1 个**（`BattleEvent` 的
        /// `TargetPlayer/TargetSlot/TargetCardId` 是标量；表现层也是按 0/1 个目标处理的，
        /// 见 `BattleDriver.ExpandAbilityBeats` 的多目标拍注释）⇒ **如实记：取第一个**。</para>
        ///
        /// <para>⚠️ 只在**有延后结算体**的那几条路上 arm（今天是回合起止的棋盘单位那一趟）；
        /// 其余发射点（监听者 / 天赋 / 605 / 410 …）**保持 `-1`**，理由见
        /// `资料/普查产出_1025/E4_trigger目标与activeAbility目标池.md`。</para>
        /// </summary>
        public BattleEvent TriggerTargetSink;

        /// <summary>配合 <see cref="TriggerTargetSink"/>：这条事件的目标**已经填好了**（`true` ⇒ 后面的目标不再覆盖）。
        /// 落在 `ResolveTargets` 那一个收口里，判据见 `EffectResolver.NoteTriggerTargetSink`。</summary>
        public bool TriggerTargetSinkDone;

        /// <summary>
        /// 🆕 **2026-10-24（引擎线 `A1` 第 1 件）：上一条能力留下的目标集合** —
        /// 原版 `AbilityLogic` 的**静态表**（反编译里是 `pooledPossibleTargets`，函数名
        /// `SetPreviousAbilityTargets`）：`AbilityLogic__SetPreviousAbilityTargets.c:19-28`
        /// = `Array.Clear` + `AddRange`（**整表覆盖**），调用点 = `AbilityLogic__PlayAbility.c:2743-2745`
        /// （**这条能力结算完就把槽覆盖掉**）。
        ///
        /// <para>**消费者** = `TargetsAffected.previousAbilityFiltered = 150`：
        /// `AbilityLogic__GetTargets.c:867-882` 把它 `AddRange` 成**候选池**，
        /// `FilterMethods__CheckIfMeetsCriteria.c:172-183` 再闸一次「候选必须在表里」，
        /// 之后照常走阵营 / 卡种 / 条件筛。`CountEval` 的 `ta == 150` 分支读的就是本表。</para>
        ///
        /// <para>⚠️ **本版的两处如实标注（⛔ 不拿猜的顶）**：
        /// ① 🔴 **2026-10-24（执行代理 `E2`）就地订正（铁律 5）：写点不再是「只在出口一次」。**
        ///    原来这里写「写点 = `ResolveOps` 两条重载的出口，取的是该次结算**最后**解出的那批目标
        ///    （`ctx.LastTargets`）…… 少了并集那一半（近似）」——**那正是 `E8` 顺手发现 2 那条残值缺陷**：
        ///    同一次结算里**后面的 op** 读到的是**上一张卡**的值。原版是**每条效果出口**都整表覆盖
        ///    （`AbilityLogic__PlayAbility.c` 的 41 个 `case` / 48 处 `LAB_18081cc87`）。
        ///    ⇒ 现在 = **每条 ability 起手清空的并集表**（`AbilityTargetsSaved`，原版 `+0x80` 的对应物）
        ///    **逐 op 同步**（`NotePreviousAbilityTargets`，两个主循环 4 个 `ResolveOne` 点 + 出口幂等一次）。
        ///    **近似只剩「出口那次也幂等调一遍」**，语义上与原版逐效果覆盖一致；
        ///    今天池内 `ta = 150` 的真实消费行 **0 条**（`SW51` 的 `multiSet` 因 `conditionsFilter = 0`
        ///    未落行，见 `N2_八个role.md` §五·⑤）⇒ 本件**零可观测差异**（回归由自检钉住）；
        /// ② 本版只覆盖**场上单位**候选。原版表里还可能有**手牌 / 牌库的卡**（`SW51` 的上一跳
        ///    正是 `Draw 2 cards`）⇒ 那种候选落到 `CountEval` 会**返回 null 并出声**
        ///    （⛔ 不静默少数）；那条路今天由正文的 `countScope == "draw"` 覆盖（`SW51` 等价）。</para>
        /// </summary>
        public readonly List<UnitState> PreviousAbilityTargets = new List<UnitState>();

        /// <summary>
        /// 🆕 **2026-10-24（执行代理 `E2` · `E8` 顺手发现 2 的根治）：本条 ability **至今**解出的目标**并集**
        /// —— 原版 `AbilityLogic.abilityTargetsToSave`（`+0x80`）的对应物。
        ///
        /// <para>**它为什么必须存在**：`PreviousAbilityTargets`（上面那一格）是**对外发布**的视图，
        /// 而原版那个静态表是「**每一条效果出口**整表覆盖成 `abilityTargetsToSave` 的当前值」
        /// （`AbilityLogic__PlayAbility.c` 的每个 `case` 出口 →
        /// `AbilityLogic__SetPreviousAbilityTargets`；41 个 `case` / 48 处引用 `LAB_18081cc87`）。
        /// 所以发布视图 = **本条 ability 里已经解出的所有目标**（只增不减），**不是**「最后一条 op 的目标」。
        /// ⛔ 只在 `ResolveOps` 出口同步一次会让同一次结算里**后面的 op** 读到**上一张卡**的残值
        /// （`E8` 那条：静默多数）。</para>
        ///
        /// <para>**生命周期**：两条 `ResolveOps` 入口 `Clear()`（原版每次 play 都是新的表 ——
        /// `AbilityLogic__DeepCopy.c` **不拷贝**这一格）；每条 op 结算完由
        /// `EffectResolver.NotePreviousAbilityTargets` 并进来并**重新发布**。</para>
        ///
        /// <para>⚠️ 只装**场上单位**（`UnitState`）；原版表里还能有手牌/牌库的卡（抽牌那一跳）——
        /// 那半边由 `CountEval` 的 `DrawnThisResolve` 分支单独建模（判据 → `PreviousAbilityTargets` 的 doc）。</para>
        /// </summary>
        public readonly List<UnitState> AbilityTargetsSaved = new List<UnitState>();

        /// <summary>
        /// **事件里那个宾语**（`target`）—— 2026-09-13 A3 加。目前只有一个来源：
        /// `When … attacks …, &lt;正文>` 的正文里，**被攻击的那个单位**。
        ///
        /// 为什么不能拿 <see cref="LastTarget"/> 顶替：那条路已经被**代词**占着
        /// （`ResolveOps` 把**监听者自己 / 事件主体**种进去），而同一句里两个指代**同时存在**：
        ///   · `Doomstalker`：`When an enemy attacks, deal 2-3 damage to **it**`   → it = **攻击者**
        ///   · `Valtus`：`… deal 3 damage to **the target of the attack**`        → = **被打的那个**
        /// 拿一个槽位表示两件事，总有一张卡会**静默打错人**。
        ///
        /// 由 <see cref="EffectResolver.BroadcastWhen"/> 在广播事件时设、广播完恢复
        /// （和 `LastTargets` 同一套保存/还原，理由相同：监听器的效果里还会再广播事件）。
        /// ⚠️ 取用方**必须判活** —— 攻击广播在伤害**之前**，但结算时它可能已经被打死了。
        /// </summary>
        public UnitState EventTarget;

        /// <summary>
        /// 🆕 **2026-09-16：发生这件事的「那张卡」**（不是单位）—— 给 `create a copy of **it**` 用。
        ///
        /// 出处：`Neurotyrant`（`TL69` · Leviathan）卡面
        ///   `Whenever you play a non-Ephemeral Stratagem, create an Ephemeral copy of **it** in your hand`
        ///   —— 那个 `it` 指的是**刚打出的那张战术卡**（一张 `CardDef`），
        ///   **不是**场上的单位。而 `LastTarget` 装的是 `UnitState`、`LastChosenCard` 是选牌那条路的
        ///   ⇒ 原来那个 `it` **没有通道**（`ctx.PlayingCard` 在事件触发路径里是 null）。
        ///
        /// 由 <see cref="EffectResolver.BroadcastWhen"/> 在广播时设、广播完恢复
        /// （和 `EventTarget` / `LastTargets` 同一套保存/还原）。
        /// 取用顺序见 `EffectResolver.DoCreate` 的 `CopyOfPrev` 那一支。
        /// </summary>
        public CardDef EventCard;

        /// <summary>
        /// **正在结算效果的那个单位**（`null` = 没有施放者，比如战术卡）。
        ///
        /// 为什么要有它：「相邻」的 `Self` 锚点（`Strike: Give Invulnerable to **adjacent troops**`）
        /// 要知道**是谁在施放**，而结算函数是一张**统一签名的表**（`EffectResolver.EffectDispatch`，
        /// 28 个 handler 都没有 `source` 参数）。为这一个字段去改 28 个签名 + 整张表不合算，
        /// 所以由 `EffectResolver.ResolveOne` 在结算每条效果**之前**设、结算完**恢复**
        /// （效果会嵌套 —— `Repeat this effect` / `Choose one` 里会再调 `ResolveOps`）。
        ///
        /// ⚠️ 和 <see cref="PlayingCard"/> 的分工：那个是**卡牌定义**（记录用），这个是**场上单位**。
        /// </summary>
        public UnitState ActingUnit;

        /// <summary>
        /// **正在重复突触效果**（`Synapse`，2026-09-13 A2）—— 递归保护。
        ///
        /// 为什么要有它：突触是「被友方战术选中时，**对相邻单位把这张战术再跑一遍**」，
        /// 而邻居自己也可能带 `synapse` ⇒ 那个邻居再被同一张战术「选中」就会**无限转**。
        /// 由 `EffectResolver.RepeatTacticOnAdjacent` 置位、`finally` 复位。
        /// </summary>
        public bool SynapseBusy;

        /// <summary>
        /// **费用修正**（`Lower the cost of … by N` / `They cost N less`）。
        ///
        /// 为什么要有它：`CardDef` 是**不可变的共享对象**（整个卡池共用一份），
        /// 把费用改在它身上会污染所有同名卡、连卡组编辑器都跟着变。
        /// 所以费用修正挂在对局上，由 <see cref="RuleCore.CostOf"/> 现算。
        ///
        /// ⚠️ **按卡名匹配，不是按卡实例**：手里两张同名卡会**一起**降价。
        ///    原版能区分实例（每张卡有 GID），我们手里只有 `CardDef` 引用 —— 如实标着这个近似。
        /// </summary>
        public readonly List<CostMod> CostMods = new List<CostMod>();

        /// <summary>
        /// **上一条效果造出来的那些卡** —— 供 `They cost 1 less` / `It costs 2 less` 指代
        /// （上一版复刻用 `ctx.last_created` 记同一件事，见 `rule_core.gd:3002`；⚠️ **旁证，非原版**）。
        ///
        /// 🔴 **2026-09-18 第 7 行第 3 步：元素类型 `CardDef` → `CardInstance`。**
        ///    「指代」必须落到**具体哪一份**上 —— `Master of Manoeuvre` 的
        ///    `Return a friendly Vehicle to your hand. **It** costs 4 less` 就是标准例子：
        ///    手里两张同名载具时，按模板记会**两张一起降价**（改之前就是这样）。
        /// </summary>
        public readonly List<CardInstance> LastCreated = new List<CardInstance>();

        /// <summary>
        /// **上一次「花掉全部灵魂石」花掉了几颗**（2026-09-13 第三十三轮）。
        ///
        /// 为什么要有它：`Hosts of the Dead` 的卡面是
        /// `Deploy a Wraithguard. **Spend all your Spirit Stones. For each one, deploy a Wraithguard**`——
        /// 后面那句的**重复次数**就是这里花掉的数量。次数只在这里算一次，
        /// `repeat` 读它（**别在两处各算一遍**，那是不一致的经典来源）。
        /// 每次 `Spend all` 都重写（一次结算里可能花多次）。
        /// </summary>
        public int LastSpentSpirit;

        /// <summary>
        /// **上一次选牌挑中的那张卡**（`Choose a …`）—— 我们上一版复刻 `rule_core.gd:1152` 的 `ctx["_chosen_card"]`（⚠️ **旁证，非原版**）。
        ///
        /// 和 <see cref="LastCreated"/> 的分工：选牌时**两个都写**（那份 `.gd` 的 `:1151-1152` 就是同时写
        /// `last_created` 和 `_chosen_card`），所以 `Lower its cost by N` / `It costs N less`
        /// 那条走 `LastCreated` 的 `(指代上一张)` 路径**不用改**就通了。
        /// 这个字段单独留一份，是给**「复制选中那张」**（`create a copy of it`）用的 ——
        /// 那种句子的指代对象在**手牌/牌库里**，不在场上，`LastTarget`（`UnitState`）够不着。
        ///
        /// 🔴 **2026-09-18 第 7 行第 3 步：`CardDef` → `CardInstance`**（指代要落到具体哪一份）。
        /// </summary>
        public CardInstance LastChosenCard;

        /// <summary>
        /// **手牌那一侧的指代槽（复数）** —— `give **them** X` 里的「它们」指的是**手里的那几份**时的存放位置。
        ///
        /// 与 <see cref="LastCreated"/> / <see cref="LastChosenCard"/> 并列，但**指的不是同一件事**：
        /// 那两个槽装的是「上一张造出来的 / 上一次选牌选中的」牌（在牌库、手牌、场上都可能），
        /// 本槽只有一个意思 —— 「**这一步落到手牌里的那几份**」。
        ///
        /// 🔴 **为什么要有它**（2026-10-18 · 待办 `A888`）：在此之前
        /// `EffectResolver.HandReferents` **只**认 <see cref="DrawnThisResolve"/> 当兜底 ——
        /// 那本来是「本次结算抽到了哪几张」的**计数账**（`For each troop drawn …` 用它），
        /// 拿它当代词槽是**推断**：只有「先抽、后指代」这一种卡面形状兜得住。
        /// 而 `draw` 之外**没有任何写点**（`give … in hand` 那条路也一样），
        /// 于是「槽」永远等于「这次抽到的那几张」。
        /// ⇒ 现在补成**真的槽**：**先读槽、槽空才退回 `DrawnThisResolve`**（兜底保留，
        ///   `GOF50` / `TAU54` / `GOF_Da_Red_Waaagh` / `UM23` 那四张靠它活着）。
        ///
        /// **谁写**：`EffectResolver.DoDraw`（抽上手的那几份）·
        ///   `EffectResolver.AttachEffectToHandInstances`（刚把效果挂上去的那几份）。
        /// **谁读**：`EffectResolver.HandReferents`（两个调用点：`DoGive` 的 `prev` 支 ·
        ///   `ConditionHolds` 的 `targethaskw` 手牌支）。
        /// **什么时候清**：`RuleCore.ResolveOps` 的入口 —— 与 <see cref="DrawnThisResolve"/>
        ///   **同一个窗口**（一条能力一个窗口）。
        /// 🔴 **这条清空必须留着** —— 不清的话上一张卡留下的那几份会**抢班**：
        ///   `GOF_Da_Red_Waaagh` / `UM23` 那两张**单数 `it`** 走的是同一条路（`give it …`），
        ///   它们自己**不写槽** ⇒ 全靠兜底；槽里一旦有残值就会被顶掉（静默错打）。
        /// </summary>
        public readonly List<CardInstance> LastHandTargets = new List<CardInstance>();

        /// <summary>
        /// **手牌那一侧的指代槽（单数）** —— `give **it** X` 指的是手里那一份时的存放位置。
        ///
        /// 与 <see cref="LastHandTargets"/> 是**一对**（和 <see cref="LastTargets"/> / `LastTarget`
        /// 那一对同构）：复数读列表、单数读这一个。
        ///
        /// ⚠️ **只在「候选恰好一个」时写**：卡池里没有 `Draw two troops and give it …` 这种写法，
        ///    而单数 `it` 从一堆里**挑一个** = 静默猜（本工程红线）⇒ 写点拿不准时**留 `null`**，
        ///    由 `HandReferents` 退回兜底，别在这儿替卡面做决定。
        /// </summary>
        public CardInstance LastHandTarget;

        /// <summary>
        /// **本局阵亡的部队**（`Choose a friendly troop that died this game / this battle /
        /// since your last turn` 的候选来源）—— 上一版复刻 `rule_core.gd:1004-1024` 的 `dead` 域（⚠️ **旁证**）。
        ///
        /// ⚠️ **为什么不复用 `PlayerState.Discard`**：`Discard` 是**单位与战术混装**的
        ///    （打出的战术卡也进那儿，见 `RuleCore.PlayTactic`），拿它当墓地会挑出「已经打掉的战术卡」。
        ///    而且它还缺**死亡发生在第几回合**这个信息 ——
        ///    `since your last turn` 的窗口正是靠它划的。
        ///
        /// 督军**不进这张表**（`RuleCore.KillUnit` 对督军提前 return）—— 督军不能复活。
        /// </summary>
        public readonly List<DeadUnit> DeadUnits = new List<DeadUnit>();

        /// <summary>
        /// **本局打出过的牌** —— `Choose a … **you played this game**` 的候选域
        /// （实测 1 处：`Suppressor` 的 `Oath 1:`）。表项的来历见 <see cref="PlayedCard"/>。
        ///
        /// **谁写的**：**只有出牌那两个口** —— `RuleCore.PlayCard`（单位卡）与
        ///   `EffectResolver.PlayTactic`（战术卡），两处都调 <see cref="NotePlayed"/>。
        ///   ⛔ **别在别处写**（这个工程最怕「写入点漏挂钩子」那一类静默缺陷）：
        ///   `DeployFree`（免费部署）/ `SpawnTideCopies`（潮涌把复制品放进手牌）/
        ///   `PlayCompanions`（伴生从手牌直接上场）**都不是「打出」** ⇒ 不进这张表
        ///   （潮涌的复制品**后来被正常打出**时才进）。经效果从牌库/弃牌堆直接上场或放进手牌的，同理。
        /// **谁读的**：`EffectResolver.ChooseCardCandidates` 的 `case "played"`
        ///   （以及认「哪一份」的 `FindZoneInstance` / `RemoveFromSource` 的 `case "played"`）。
        /// **什么时候清**：**不清** —— 原版那条日志也是全对局不清（见 <see cref="PlayedCard"/>）。
        ///   表跟着 `BattleContext` 走，一局一个（`RuleCore.NewBattle` 造的）。
        /// </summary>
        public readonly List<PlayedCard> PlayedCards = new List<PlayedCard>();

        /// <summary>出牌流水号（只增不减）—— <see cref="PlayedCard.Seq"/> 由它发号。
        /// ⚠️ **「谁更近」只看它**：`PlayedCard.Turn` 分不开同一回合里打的好几张。</summary>
        public int PlayedSeq;

        /// <summary>
        /// 记一笔「**某一方打出了一张牌**」。**只有出牌那两个口调**（见 <see cref="PlayedCards"/>）。
        /// `inst` 传的是**打出的那一份实例**（不是 `CardDef` 模板）—— 原版按
        /// `actingCardUniqueGID` 回查 `CardScript`，同一件事。
        /// </summary>
        public void NotePlayed(int owner, CardInstance inst, bool tactic)
        {
            PlayedSeq++;
            PlayedCards.Add(new PlayedCard
            {
                Instance = inst,
                Owner = owner,
                Turn = this.Turn,
                Tactic = tactic,
                Seq = PlayedSeq,
            });

            // 🆕 2026-10-18（`A985④`）**顺手把 `PlayingCard` 刷成刚打出的这张** ——
            //  理由：打出**单位卡**时，`RuleCore.PlayCard` 里紧接着就造东西
            //  （`SpawnTideCopies` 造潮涌复制品 / `PlayCompanions` 造伴生卡），而**那一刻还没进任何
            //  `ResolveOps`** ⇒ 不在这儿刷的话 `PlayingCard` 还停在**上一张**结算的卡上
            //  （它是「常驻效果登记 / 日志」那一族读的「最近打出的牌」）。
            //  🔴 **原版就是这么传的**：`BattleManager.AddNewCardToHand(…, param_3 = creator, …)`
            //  的调用者里，`CardScript__ProcessCompanion.c` 传**它自己**、`AbilityLogic__PlayAbility.c`
            //  传**技能属主那张卡** ⇒ 「刚打出/正在结算的那张卡」正是同一个东西。
            //  ⚠️ **不改变任何既有读者**：`PlayingCard` 现在的读点全在 op 里，而 `ResolveOps`
            //  入口**每次都覆写**它（含写成 `null`）⇒ 这一句在它们之前就被顶掉了
            //  （`EffectResolver` 里 `ctx.PlayingCard = sourceCard` 那一句是唯一另一处写点，语义不变）。
            //  🔴 **2026-10-22（`W62/D5②`）就地订正**：上面「这张牌是谁造出来的要它」**已不成立** ——
            //  `CreatedBy` 那条链改吃 <see cref="CreationSource"/>（save/restore 的作用域量），
            //  因为本字段**从不清空**、在非结算期造牌会盖错名字（见 <see cref="NewInstance"/> 那段）。
            //  ⛔ 别把本字段再挂回 `CreatedByOf`。
            PlayingCard = inst != null ? inst.Card : null;
        }

        /// <summary>
        /// **常驻效果**（卡面写 `For the rest of this battle, …` 的那一族）。
        ///
        /// 🔴 **2026-10-19（`A1955`）：判据换成原版、原句就地订正。** 原来这里写
        /// 「规则书英文版 `:39-41`：这类卡**被弃置后依然生效**……**卡本身就是效果来源**」
        /// —— 那两句**都不成立**：规则书是**粉丝实体版**（`Not official. Fan project.`），
        /// 而原版（`d:/2/tools/decomp_full/`）**没有**「弃置后由那张牌自己继续生效」这条路：
        /// 效果搬去**一张还活着的载体牌**（附魔 / `replaceAbility`），载体不在场就没了；
        /// 弃牌堆**不在任何触发广播的遍历范围**里。完整判据 →
        /// `EffectResolver.cs` 的 `DoPersist` 上方那段块注释（`A1950` 已逐条换过）。
        /// ⇒ 我们这摞「谁 + 来源卡 + 触发时机 + 正文」是**自设计的替代结构**，
        ///   与「没绑定的效果随载体继续有效」那一支结果一致。
        ///
        /// 由 `EffectResolver.DoPersist` 在**打出时**登记，`RuleCore.ResolveAtTurn` 在每个
        /// 回合的起/止按 `Owner == 当前行动方` 消费（我们上一版复刻 `rule_core.gd:431-435` 同一口径，⚠️ **旁证**）。
        /// </summary>
        public readonly List<PersistentEffect> PersistentEffects = new List<PersistentEffect>();

        /// <summary>
        /// **正在结算的是哪张卡**（由 `ResolveOps` 在入口写）。
        /// 只有「常驻效果登记」用它 —— 得记来源（日志/排查）。别处不看。
        /// </summary>
        public CardDef PlayingCard;

        /// <summary>
        /// 🆕 **2026-10-19（第五十七会话 `W57FIX2` · P3）：当前这次【出牌结算】的来源卡**；
        /// `null` = **不在任何一次出牌结算里**（事件触发 / 常驻效果 / 回合起止 …）。
        ///
        /// <para>**为什么不能拿 `PlayingCard` 顶替**：那个字段**从不清空**（写点只有
        /// `NotePlayed` 与 `ResolveOps` 的出牌重载）⇒ 在**事件触发**那条路上它停在
        /// 「本局最后打出的一张牌」上。拿它当来源卡会让 `When … obtains Quest Point`
        /// 的监听器**按很久以前那张牌**误触发（`DA55` 常驻路径 · 审计 P3）。</para>
        ///
        /// <para>**写点（全部 save/restore，别在别处写）**：
        /// ① `EffectResolver.PlayTactic` —— 战术卡整条结算（含突触重复）；
        /// ② `EffectResolver.BroadcastPersistentWhen` —— 事件型常驻效果，来源卡 =
        ///    `PersistentEffect.Source`（原版那条 ability 的来源卡就是写出这张常驻效果的卡）。
        /// ⛔ 别在别处写它 —— 写宽了就回到「陈旧来源卡」的老毛病。</para>
        ///
        /// <para>⚠️ **和 <see cref="FactionSourceCard"/> 的分工**：这一条是「**这次出牌**的卡」；
        /// 那一条是「**常驻作业**的真实来源卡」，且**优先级更高**（见那边的 doc）。
        /// 两条都只喂 `EffectResolver.DoFactionResource`，不参与代词/kill credit。</para>
        /// </summary>
        public CardDef CardPlaySource;

        /// <summary>
        /// 🆕 **2026-10-20（`W57FIX2` 续 · 审计 P3 的两条旁路）：常驻作业的【真实来源卡】覆写。**
        ///
        /// <para>**为什么不能靠 `ActingUnit`**：`ResolveDeploy` 把「刚部署的那个单位」当 `source`
        /// 传给 `ResolveOne`（那是本工程的结构选择，服务代词/相邻锚点/kill credit），
        /// 但**原版那条 ability 的属主不是它**：
        /// `ProcessManaBuff.c` 的 `param_4` = `BattleAction.casterCard`
        /// （偏移已核：`BattleAction` 结构体 +0xD8 = `casterCard`，用 `manaBuff`(+0x78→拷贝 +0x90)
        /// 与 `isPlayer`(+0x04→拷贝 +0x1c) 两格交叉验证过）；
        /// 而 `AbilityLogic__PlayAbility.c:713` 建 mana 动作时传的 `casterCard` =
        /// `AbilityData.thisCard`（`*(param_3 + 0x10)`）= **能力属主那张卡**
        /// （`RawCardScript__TriggerAbility.c:33` 的 `SetNewData(thisCard=param_4, …)`）。
        /// ⇒ 常驻效果（部署型 / 回合型 / 手牌陷阱）的阵营资源来源卡 =
        /// **写出这条效果的那张卡**（`PersistentEffect.Source` / 手牌那份 `CardDef`），
        /// **不是**被部署的单位、也不是「没有」。</para>
        ///
        /// <para>**写点（全部 save/restore）**：
        /// ① `EffectResolver.ResolveDeploy` —— 部署型常驻作业（`pe.Source`）；
        /// ② `EffectResolver.ResolveAtTurn` 的 persistJobs（`j.Card` = `pe.Source`）与
        ///    handJobs（`j.Card` = 那张手牌陷阱）。**unitJobs 不写** —— 那里 `ActingUnit`
        ///    本来就是能力属主。</para>
        ///
        /// <para>**优先级**：`DoFactionResource` 里它 **&gt; `ActingUnit` &gt; `CardPlaySource`**。
        /// 只有这里能压过 `ActingUnit`，因为只有这三条路的 `ActingUnit` 是「事件宾语」而非「能力属主」。</para>
        ///
        /// <para>⚠️ **今天零可观测差异**：全池 10 张「For the rest of this battle」里
        /// 没有一张的常驻正文产阵营资源（`AM51`/`BL75`/`DA55`/`GSC45`/`SAU55`/`SW46`/
        /// `TAU42`/`TAU51`/`TL47`/`SAU53` 逐张看过）—— 补它是为了「机制按原版站住」（铁律 11）。</para>
        /// </summary>
        public CardDef FactionSourceCard;

        // ==================================================================
        //  🆕 2026-09-14：**玩家在面板上的选择**（选牌 / 选效果 / 三选一）
        // ==================================================================
        //
        // **为什么是「先填好再结算」，而不是「结算到一半停下来问」**：
        //   引擎是**同步**的（`ResolveOps` 一口气跑完），而表现层的面板要点一下才有人回答
        //   —— 想「停下来等」就得把整条调用链做成可恢复的（协程/续延），那要动
        //   `ResolveOps` + `ResolveOne` + `RuleCore` 的每一个入口。**这一版不做那个**，如实标着。
        //   现在的形状：表现层**发起动作之前**先把每个「本该问玩家」的点的答案算好、按**结算顺序**
        //   排进这个队列；引擎结算到那一步就出队用它。
        //
        // ⚠️ **队列里是「相对于各自候选表的下标」** ⇒ **顺序必须和结算顺序一致**，
        //   否则会**选错一张**（而且不报错）。表现层那边靠 `RuleCore.PlayerChooseOps` 保证同序。
        // ⚠️ **空队列 = 没人问过** ⇒ 退回 `ctx.GetRandomInt`（2026-10-27 `E1RNG`：原写 `ctx.Rng`，已随随机表落地改口）。
        // ⚠️ **`ChooseSites` / `ChooseAnswered` 两个数都要看**：前者是「引擎本该问几次」，
        //   后者是「其中几次真的用了面板的答案」—— 差着就说明**有几次是引擎替玩家挑的**，
        //   而那种事**不报错**（本工程的静默失败红线）。
        public readonly Queue<int> ChoosePicks = new Queue<int>();

        /// <summary>
        /// 🆕 2026-09-14：**选牌**那一族的面板答案，存的是**选中那张卡的 `CardDef.Id`**
        /// （与 <see cref="ChoosePicks"/> **同序、同长**，一格对一格）。
        ///
        /// **为什么还要一份 Id**：规则书英文版 `:475` 写的是
        /// 「Whenever a card uses the word "choose", **randomly select 3 cards** from the set of
        /// possibilities and the player chooses which one」—— 玩家看到的候选是**随机抽出的 3 张**，
        /// 不是全部。而 `ChoosePicks` 存的是「相对于**候选表**的下标」，两张表不一样长
        /// ⇒ 用下标对不上。⇒ 面板**自己抽 3 张、把选中的那张的 `Id` 报回来**，
        /// 引擎按 `Id` 在**完整候选表**里找到它。
        ///
        /// ⚠️ **两件事必须守住**：
        ///   ① 结算时**照样要出队一个 `ChoosePicks` 下标** —— 不出队会让后面每一处**全部错位**（静默选错）；
        ///   ② 没用到 `Id` 的分支（AI 回合、面板没答）**分布不变**：
        ///      「从 N 张里随机抽 3、再挑 1」与「从 N 张里等概率挑 1」**是同一个分布**
        ///      ⇒ 引擎侧**不需要**真的先抽 3 张（见 `DoChooseCard` 的注释）。
        /// </summary>
        public readonly Queue<string> ChooseCardIds = new Queue<string>();

        /// <summary>本次动作里「本该问玩家」的次数（三个 ask 点各加一次）。</summary>
        public int ChooseSites;
        /// <summary>其中**用了面板答案**的次数。`&lt; ChooseSites` 就代表有几次是引擎替玩家挑的。</summary>
        public int ChooseAnswered;

        /// <summary>清空选择队列与计数（表现层**每次动作之前**调）。</summary>
        public void ResetChoices()
        {
            ChoosePicks.Clear();
            ChooseCardIds.Clear();
            ChooseSites = 0;
            ChooseAnswered = 0;
        }

        // ==================================================================
        //  🆕 「移出游戏」区域 + 临时卡（Ephemeral）—— 2026-09-13 第三十二轮
        // ==================================================================
        //
        // **为什么单开一个区域**：规则书 `:229` 明写临时卡「**从游戏中移除（非弃置）**」——
        // 它**不进弃牌堆**。所以 `Hand + Deck + Discard + Board` 四个区域之外还有第五个。
        // 英文原版 `:421-427` 那段（标题就叫 Ephemeral Cards）说得更直白：
        // 实体版要「place these cards to the side away from the [deck/discard]」。
        //
        // ⚠️ **为什么不是「一个集合了事」**：照 `DeadUnits` 的先例 —— 那张表当初就是因为
        //    `Discard` 挑不出「本局阵亡的部队」才单开的，后来长出了
        //    `died this game / this battle / since your last turn` 三种窗口查询。
        //    **同一类需求会长出第二批**（「本局移出过什么」），所以一次做成能查询的结构。

        /// <summary>
        /// **本局被「移出游戏」的卡**（规则书 `:229`；目前唯一来源是临时卡回合结束时消失）。
        ///
        /// ⚠️ **和 `PlayerState.Discard` 是两回事** —— 移出的卡**不在弃牌堆里**，
        ///    所以任何「从弃牌堆拿一张」的效果（`Deploy … from your discard pile` /
        ///    `Reanimate`）**都看不到它们**。这正是规则书要的效果。
        /// </summary>
        public readonly List<RemovedCard> Removed = new List<RemovedCard>();

        /// <summary>
        /// **「临时卡」的标记住在哪儿**（待办第 7 行 · 第 3 步，2026-09-18 搬完）。
        ///
        /// 🔑 **为什么不能只看关键词**（2026-09-13 最容易做错的一处）：
        ///    规则书 `:229` 点名三族临时卡 —— 天赋 / **伴生生成的部队** / **潮涌的复制**，
        ///    而**后两族卡面并没有印 `Ephemeral` 关键词**。
        ///    原版对这件事的答案是 `BuffType.ephemeralCopy = 25` / `tideCopy = 26`
        ///    （`BuffType.cs:20-21`）—— **buff 挂在「这一张牌」上，不是挂在卡的模板上**。
        ///
        /// 🔴 **2026-09-18 起：标记就是 <see cref="CardInstance.EphemeralMarked"/> 这一个布尔。**
        ///    改之前这里是 `Dictionary&lt;CardDef,int&gt;`（**按卡模板记份数**）—— 那是
        ///    「没有卡实例身份」时代的近似：造出来的复制**和原件是同一个 `CardDef` 对象**，
        ///    只能按份数记，于是「销标记时认领到哪一份」只能靠遍历顺序猜。
        ///    现在一份一个布尔，「这一份带不带标记」是**直接问出来的**，同名两张互不影响。
        ///    （原版那两条 `BuffType` 也就是这个意思 —— 挂在牌上，不挂在模板上。）
        ///
        /// ⇒ 判据仍然只有一处：<see cref="IsEphemeral"/>。
        /// </summary>

        /// <summary>
        /// **这一份**是不是临时卡（规则书 `:183` + `:229`）。
        ///
        /// 两个来源（`∪`）：
        ///   ① **卡自己带 `Ephemeral` 关键词**（98 张）—— 那张卡的**所有份**都临时
        ///   ② **这一份被标记**（<see cref="MarkEphemeral"/>）
        /// </summary>
        public bool IsEphemeral(CardInstance inst)
        {
            if (inst == null || inst.Card == null) return false;
            return inst.Card.Has("ephemeral") || inst.EphemeralMarked;
        }

        /// <summary>把**这一份**牌标成临时（造复制 / 天赋生成时调）。**幂等**（布尔，不是计数）。</summary>
        public void MarkEphemeral(CardInstance inst)
        {
            if (inst != null) inst.EphemeralMarked = true;
        }

        /// <summary>
        /// **这一份**是不是「**凭空生成**的」—— **只数标记**，
        /// **不含**卡面自带 `Ephemeral` 关键词的那 98 张（那些不是凭空生成的）。
        ///
        /// **为什么需要它**：自检要能算「**这一局凭空生成了几张牌**」——
        /// 天赋（`RuleCore.SpawnTalents`）生成的战术卡**不属于卡组**，
        /// 会把「手牌 + 牌库 = 卡组张数」这类不变量**顶掉一张**，断言必须把这部分减掉。
        /// </summary>
        public int MarkedCount(CardInstance inst)
        {
            return (inst != null && inst.EphemeralMarked) ? 1 : 0;
        }

        /// <summary>
        /// **这一份**该不该在回合结束时被移出；是的话**销掉标记并返回 true**。
        ///
        /// 🔴 **为什么不能写成「`foreach (手牌) if (带关键词) 移除`」**
        ///    —— 2026-09-13 自检抓出来的真 bug：
        ///    `CardDef` 是**共享不可变**的模板，所以「标记」只能**按卡记份数**，
        ///    而「带关键词」对**同名的每一份**都成立。
        ///    于是手里有两张同名卡、只标了其中一张时，`foreach` 会把**两张都移走** ——
        ///    原件被当成复制一起消失（静默，且要两轮之后才看得出来）。
        ///
        /// **正确顺序**（先关键词、后标记）：<see cref="CardInstance.EphemeralMarked"/> 现在是**每份一个**，
        /// 所以「认领」这一步不再需要猜 —— 销掉的**就是问的这一份**。
        /// </summary>
        public bool TryTakeOneEphemeral(CardInstance inst)
        {
            if (inst == null || inst.Card == null) return false;
            if (inst.Card.Has("ephemeral")) return true;      // 关键词那条路：这一份必然走
            if (!inst.EphemeralMarked) return false;
            inst.EphemeralMarked = false;                     // 销标记（`SweepEphemeral` 靠它「认领」）
            return true;
        }

        // ==================================================================
        //  🆕 「同时伤害 → 死了的处理延后」—— 2026-09-13 第三十二轮
        // ==================================================================
        //
        // **规则依据**（`资料/规则书/…_中文翻译.md`）：
        //   · `:145`「伤害按声明的攻击类型**同时结算**」——
        //     例子：「兽人小子造成 3 点伤害，**同时**受到 1 点反击。初生者（0 生命）进入弃牌堆」
        //   · `:238`「序列：攻击 → **双方结算伤害** → 生命归 0 方触发效果 → 摧毁方触发效果」
        //
        // 🔴 **修之前错在哪**：`Hurt` 一边扣血、一边**当场**就把死亡处理掉了
        //    （离场 → 进弃牌堆 → 发 Death → 放 Backlash）。而攻击是
        //    「`Hurt(目标)` → 再 `Hurt(攻击者)`（反击）」⇒
        //    **目标一死，它的 Backlash 当场就放掉了**，比反击还早。
        //    后果：被攻击方的 Backlash 若把攻击者打死/移走，**反击整下被跳过**
        //    （`Hurt` 见已经死了就 `return 0`）。⇒ **每一局带 Backlash/Penitence 的攻击都算错。**
        //
        // **做法**：攻击段**先声明一个「批」**（<see cref="DeferDeaths"/> 进来、出去时 <see cref="FlushDeaths"/>），
        //    批内所有死亡**只入队**、不处理；两边伤害都结算完再统一处理 —— 顺序就是规则书那句
        //    「双方结算伤害 **→** 生命归 0 方触发效果」。
        // ⚠️ 名字叫 `Defer`/`Flush` 而不是 `Begin`/`End`，是因为**批会嵌套**（见 <see cref="FlushDeaths"/>）。
        //
        // ⚠️ 数值的比较口径**不动**：我们上一版复刻 `rule_core.gd:4310` 有明确修正记录（⚠️ **旁证，非原版**）
        //    「反击**不**因目标死亡而跳过」⇒ 被攻击方的反击值取**受伤之前**的
        //    （`DeclareAttack` 里已经存了 `counterAtk`）。这条是 2026-08-21 修的，**别退回**。

        /// <summary>「同时伤害」批的嵌套深度。`> 0` = 现在死亡延后处理。</summary>
        int _deferDeaths;

        /// <summary>
        /// 批内攒下的待处理死亡。
        ///
        /// 🔴 **2026-10-01：从「(方, 格号, 击杀者)」改成「**记那个单位**」** —— 棋盘改成连续无洞模型
        /// 之后（`RuleEngine/Core/BoardSlots.cs`），**前一条死亡会让外侧的人整体内移一格**，
        /// 于是先记下的格号到处理时**已经指错人**：同一批里死两个（5 号格与 6 号格），
        /// 处理完 5 号之后 6 号那个已经挪到 5 号格 ⇒ 按 `(6)` 去找是**空的** ⇒ 它**永远不会被清理**
        /// （留下一个 0 血的单位站在场上，静默）。
        /// ⇒ **格号只在排序时用**（= 它死的那一刻在哪一格），处理时按**身份**现查它在哪。
        /// 🔴 **2026-10-20（`W58-INTERLEAVE` · 铁律 5 就地订正）**：出队**不再排序**了
        /// （`W58-SLAYFIX` 改按**死亡入队顺序** = 原版 death action 的 FIFO）⇒
        /// `SlotAtDeath` 现在**只作诊断留痕**、不参与任何判定；⛔ 别拿它去定位或排序。
        /// </summary>
        public struct PendingDeath
        {
            public int Player;
            public UnitState Unit;
            public int Killer;
            /// <summary>它**死的那一刻**在哪一格。🔴 **2026-10-20（`W58-REVIEWFIX-B1` · 铁律 5）
            /// 就地订正**：出队**不再排序**（`W58-SLAYFIX` 改按死亡入队顺序 = 原版 FIFO）
            /// ⇒ 本字段**只作诊断留痕**，不参与定位、也不参与排序。</summary>
            public int SlotAtDeath;
            /// <summary>
            /// 🆕 **2026-10-20（`W58-INTERLEAVE`）：致死伤害的来源卡**（= 原版 `DamageInfo.actingCard`
            /// / 死亡 action 上的 `actingCard`，`CardScript__TriggerUnitBacklashActions.c:44-50`）。
            ///
            /// **为什么要有它**：原版的死亡链路是**逐死者交错**的 ——
            /// `TriggerUnitBacklashActions.c:181` 在判死那一刻把「死者 + actingCard」包成一条
            /// `backlash` action 入队（FIFO）；`_ResolveBacklash_d__451__MoveNext.c:88/144` 逐条取：
            /// 先跑**这个死者**的死亡触发（`ResolveDeadCard` / `TriggerUnitDeathActions`），
            /// 再 `BroadcastDeadUnit` —— 广播时**凶手那张牌**按 `actingCard == this`
            /// 命中 `ResolveDeadCard.c:248` 的 Slay 闸。
            /// ⇒ 顺序是「死者1 反噬 → 死者1 的 Slay → 死者2 反噬 → 死者2 的 Slay」。
            /// 我们以前把所有死者的反噬跑完再统一 Slay ⇒ **少了这层交错**：
            /// 死者1 的 Slay 若改了盘面（打死人 / 部署 / 计数），死者2 的判定就看不见它。
            ///
            /// **`null` 的含义 = 这一条死亡不记击杀**（= 原版 deathType 不在
            /// `combatDefender(15)` / `ability(20)` 两档，例如**反击**打死攻击者 = `combatAttacker(10)`；
            /// **不稳定** / 毒那几档也没有来源卡）。
            /// 🔴 **2026-10-20（`W58-REVIEWFIX-B1` · P2-3）就地订正（铁律 5）**：原来这里写着
            /// 「**哨戒**那一下的 actingCard 在反编译里没钉死 ⇒ 沿用旧口径『不记』」—— **那是错的**：
            /// `BattleManager._ResolveAttack_d__438__MoveNext.c:1421`
            /// `ReceiveDamage(攻击者, 0x14 /* ability(20) */, DamageInfo(source = 哨戒单位))`，
            /// `DamageInfo.__ctor` 的 `source` 实参就是哨戒单位（同文件 `:772-778`）⇒ deathType 在放行档里、
            /// `actingCard` = 哨戒单位 ⇒ **哨戒击杀要记账**。`RuleCore.DeclareAttack` 已改为
            /// `Hurt(..., actingCard: target)`。
            /// ⛔ **不要为了「有来源就填」往下填** —— 填了就会在 `FlushDeaths` 里多触发一次 Slay。
            /// </summary>
            public UnitState ActingCard;

            /// <summary>
            /// 🆕 **2026-10-22（`W63` · `RuleEngineTest` #5–#8）：这一条死亡是不是【伤害】造成的**
            /// —— = <see cref="RuleCore.CleanupDeaths"/> 的 `maySurvive` 形参，**原样带回放**。
            ///
            /// 🔴 **为什么必须随条目走**：`CleanupDeaths(maySurvive: false)`（= **摧毁**那条路，
            /// 判据见那个形参的 doc：原版 `BattleManager__ResolveDestroyUnit` 全文没有
            /// `UseSurvivor` / `CurrentSurvivor` / `CheckIfDead`）现在也会在「同时伤害 / 队列动作批」
            /// 里走到「只入队、不处理」那一跳。改前那个形参在入队时**被丢掉**，出队时一律按
            /// `true` 回放 ⇒ **摧毁打不穿 Survivor**（幸存者把单位救回来、`survivor` 被清 0、
            /// `survivorspent` 被挂上）—— 4 条断言的同一个根因。
            /// ⇒ 现在**只此一处**决定「要不要问幸存者」，入队/出队两跳都不再自己写死。
            /// ⚠️ 缺省 `false` = 摧毁那一档（⛔ 别改成 `true`）。
            /// </summary>
            public bool MaySurvive;

            /// <summary>
            /// 🔴🆕 **2026-10-24（引擎线 `T03·c` · `C1` §3）：这一条死亡的**死因**（<see cref="UnitDeathType"/>）。
            ///
            /// <para>**为什么必须随条目走**：原版 `RawCardScript__OnMinionDeath.c:25-37` 按死因分派
            /// `175 / 180 / 170` 三条触发；而死因是在**判死那一刻**定下的（`DamageInfo.deathType`），
            /// 死亡被延后进「同时伤害批」后，出批时已经拿不到那一刻的上下文了
            /// ⇒ 和 <see cref="MaySurvive"/> 同一条纪律：**入队时带走、出队时原样回放**。</para>
            ///
            /// <para>`Unknown(0)`（缺省）= 这一路我们没钉死死因 —— **不发 180**（⛔ 别猜成 ability）。</para>
            /// </summary>
            public int DeathType;
        }

        readonly List<PendingDeath> _pendingDeaths = new List<PendingDeath>();

        /// <summary>现在是不是在「同时伤害」批里（<see cref="Hurt"/> / <see cref="CleanupDeaths"/> 看它）</summary>
        public bool DeathsDeferred { get { return _deferDeaths > 0; } }

        /// <summary>攒下了几条待处理的死亡（自检用）</summary>
        public int PendingDeathCount { get { return _pendingDeaths.Count; } }

        /// <summary>
        /// 进「同时伤害」批 —— **配对着用**：`DeferDeaths(ctx); try { … } finally { FlushDeaths(ctx); }`。
        /// ⚠️ **用 `try/finally`**：批里任何一条效果抛出/提前 `return`，不 `finally` 收尾的话
        ///    这个计数就永远回不到 0，**整局剩下的死亡全部延后、再也没人处理**（静默，且极难查）。
        /// </summary>
        public void DeferDeaths() { _deferDeaths++; }

        /// <summary>
        /// 攒一条待处理的死亡。**去重**：同一格被记两次只留第一次。
        ///
        /// ⚠️ 为什么要去重：一次攻击里目标可能被**两处**打到（星镖先打一下、主伤害再打一下），
        ///    两处都可能把它打到 0 —— 不去重的话 `FlushDeaths` 会对**同一格**跑两遍
        ///    `CleanupDeaths`（第二遍 `u == null` 直接返回，看着无害），
        ///    但**猎杀标记**那一段在整个 `CleanupDeaths` 的**最前面**，
        ///    第二次进来时 `u` 还是那个死单位（棋盘上还没清）⇒ **标记会结算两遍**。
        /// </summary>
        public void AddPendingDeath(int p, int slot, int killer, UnitState actingCard = null,
                                    bool maySurvive = false, int deathType = UnitDeathType.Unknown)
        {
            if (p < 0 || p >= Players.Length) return;
            var u = Players[p].Board[slot];
            if (u == null) return;                   // 那一格已经空了（同一批里前一条已经处理过它）
            // 去重按**身份**（见下面那段注释：以前按格号）
            for (int i = 0; i < _pendingDeaths.Count; i++)
                if (ReferenceEquals(_pendingDeaths[i].Unit, u)) return;
            _pendingDeaths.Add(new PendingDeath
            {
                Player = p, Unit = u, Killer = killer, SlotAtDeath = slot, ActingCard = actingCard,
                // 🆕 2026-10-22（`W63` #5–#8）：**原样带走**，出队时原样回放（见 `PendingDeath.MaySurvive`）。
                MaySurvive = maySurvive,
                // 🔴 2026-10-24（`C1` §3）：死因同上「入队带走、出队回放」（见 `PendingDeath.DeathType`）。
                DeathType = deathType,
            });
        }

        /// <summary>**这个单位是不是已进「将死」队列**（仍在棋盘上、但已判死）—— 2026-10-12 加（账 `A1600`）。
        /// ⚠️ 判「它还在不在场上」⛔ **别拿 `TakePendingDeaths()` 代替** —— 那一口**会把队列清空**。
        /// 口径 = 原版 `CardStateOptions.waitingToDie = 5` —— 它**不在** `CardScript.IsInPlay()` 的四档里。
        /// </summary>
        public bool IsPendingDeath(UnitState u)
        {
            if (u == null) return false;
            for (int i = 0; i < _pendingDeaths.Count; i++)
                if (ReferenceEquals(_pendingDeaths[i].Unit, u)) return true;
            return false;
        }

        /// <summary>
        /// 🆕 **2026-10-22（`W62` · R2 · 「被哨戒打死的攻击者」那一格）：本条顶层动作里已经离场（判死）的单位。**
        ///
        /// 🔴 **为什么需要**：原版 `CardScript.IsInPlayOrDying`（`CardScript__IsInPlayOrDying.c:8-13`）
        /// 把 **`waitingToDie(5)` 也算在场**，而那张牌在 `CardScript._UnitDeath` 协程跑完
        /// （两拍 `WaitForSeconds`）之前**一直留在棋盘的连续列表里**。
        /// 我们这边 `CleanupDeaths` **当场**就 `BoardSlots.RemoveAt` ⇒ 等到队列里的监听器结算时，
        /// 那个单位「既不在场、也不活着」，于是 `DoGive` 的「活着 ∨ 还在棋盘上」闸把它**静默丢掉**
        /// （实测：`When this unit attacks, gain +1 Attack` 的攻击者被哨戒打死后，加成不落地）。
        ///
        /// ⇒ 判死时在这里记一笔，`ActionScope` 在**最外层动作退出（泵完队之后）**清空 ——
        ///   生命期 = **一条顶层动作**，正好是原版「死亡协程还没跑完」那个窗口的等价物。
        /// ⚠️ 只用于「还能不能对它加效果 / 指代它」这类**在场判据**（`IsUnitInPlayOrDying`），
        ///   ⛔ **不能**拿它当「还活着」（`IsAlive` 仍是 `Health &gt; 0`，别改那个）。
        /// </summary>
        public readonly HashSet<UnitState> DyingThisAction = new HashSet<UnitState>();

        internal List<PendingDeath> TakePendingDeaths()        {
            var copy = new List<PendingDeath>(_pendingDeaths);
            _pendingDeaths.Clear();
            return copy;
        }

        /// <summary>出批（见 <see cref="DeferDeaths"/> 的用法）。**不清队列** —— 清理由 <see cref="FlushDeaths"/> 做。</summary>
        public void ReleaseDeferDeaths() { if (_deferDeaths > 0) _deferDeaths--; }

        /// <summary>
        /// 从墓地取走一张（`Choose a … that died … and deploy/hand/deck it`）。
        ///
        /// **同一条规则只写这一处**：`Discard`（供 `DeployFrom == "graveyard"` 那条老路）与
        /// `DeadUnits`（本表）**必须一起移除** —— 只移一边的话，同一张卡能被复活两次。
        /// **返回摘走的那一份实例**（没有可取的就 null）。
        /// </summary>
        public CardInstance TakeFromGraveyard(int owner, CardDef card)
        {
            if (card == null) return null;
            // 🔴 **2026-09-18 修**：这两个循环原来**都没有 `break`**，而手牌/弃牌堆/死单位表里
            //    存的是**共享的 `CardDef` 对象**（同名两张 `ReferenceEquals` 为真）⇒
            //    弃牌堆里同名 2 张、复活其中 1 张，**两张都会被删掉**（`DeadUnits` 同样）。
            //    上面那句注释要防的是「只移一边 → 同一张卡被复活两次」，是**跨两张表**的事；
            //    漏 `break` 是把「移走一张」做成了「移走全部」，方向反了。
            //    同文件里 `EffectResolver.RemoveRef` 是对的写法（有 `return`）——
            //    **同一件事两份实现，一份漏了**，这次收口。
            // 🔴 **2026-09-18 第 7 行第 2 步**：弃牌堆现在存**实例**了 ——
            //    所以「取走哪一份」终于是真的按份取（返回摘下来的那一份给调用方沿用）。
            // 🆕 **2026-10-23（引擎线 `新账-97`）**：`DeadUnits` 现在也存 `Instance` ——
            //    这里先按**实例**在 `Discard` 里找那一份（同名两份不再摘错），
            //    找不到才退回旧的「按卡模板、从末尾找」那条路（老夹具/老记录 `Instance == null`）。
            int deadIdx = -1;
            CardInstance deadInst = null;
            for (int i = DeadUnits.Count - 1; i >= 0; i--)
            {
                var d = DeadUnits[i];
                if (d == null || d.Owner != owner || !ReferenceEquals(d.Card, card)) continue;
                deadIdx = i; deadInst = d.Instance; break;   // 最近一条死亡
            }
            var disc = Players[owner].Discard;
            if (deadInst != null)
            {
                for (int i = disc.Count - 1; i >= 0; i--)
                    if (ReferenceEquals(disc[i], deadInst))
                    {
                        disc.RemoveAt(i);
                        if (deadIdx >= 0) DeadUnits.RemoveAt(deadIdx);
                        return deadInst;
                    }
            }
            // 老路：按模板摘（`Instance == null` 的记录、或实例已不在弃牌堆里）
            if (deadIdx >= 0) DeadUnits.RemoveAt(deadIdx);
            for (int i = disc.Count - 1; i >= 0; i--)
                if (ReferenceEquals(disc[i].Card, card))
                {
                    var taken = disc[i];
                    disc.RemoveAt(i);
                    return taken;
                }
            return null;
        }

        /// <summary>
        /// 从墓地取走**指定的那一份**（调用方手上已经有实例时走这条 —— 比按模板找更精确）。
        /// 两张表**一起**移（同上）。
        /// </summary>
        public CardInstance TakeFromGraveyard(int owner, CardInstance inst)
        {
            if (inst == null) return null;
            var disc = Players[owner].Discard;
            for (int i = disc.Count - 1; i >= 0; i--)
                if (ReferenceEquals(disc[i], inst))
                {
                    disc.RemoveAt(i);
                    // 🆕 2026-10-23（引擎线 `新账-97`）：先按**实例**匹配；老记录（`Instance == null`）
                    //    才退回模板匹配 —— 同名两份时不再删错那一条。
                    for (int j = DeadUnits.Count - 1; j >= 0; j--)
                    {
                        var d = DeadUnits[j];
                        if (d == null || d.Owner != owner) continue;
                        if (ReferenceEquals(d.Instance, inst)
                            || (d.Instance == null && ReferenceEquals(d.Card, inst.Card)))
                        { DeadUnits.RemoveAt(j); break; }
                    }
                    return inst;
                }
            return null;
        }

        public void Log(string message)
        {
            Events.Add(message);
            if (Events.Count > EventCapacity) Events.RemoveRange(0, Events.Count - EventCapacity);
        }

        // ==================================================================
        //  事件流
        // ==================================================================

        /// <summary>发一条事件。引擎里**所有** <see cref="Signals"/> 都从这儿进。</summary>
        public void Emit(BattleEvent e)
        {
            if (e == null) return;
            e.Turn = Turn;
            // 🆕 **2026-10-25（执行代理 `X2`）**：`EvtKind.Trigger` **单口计数**（成就 `Ach_5/8/9/11/13/64/66/67/72/73/79/80/81/88/90/91/98`）。
            //   判据 = `资料/待办判据_成就计数器_1025b.md` §B-3 #2：**全引擎的触发事件都从这一个口进**
            //   ⇒ 在这里数一处分，不可能漏（⛔ 别去各个 `EmitCardTrigger` 调用点各数一遍）。
            //   · `e.Keyword` 有两种形态，都要认：
            //     ① **已经是原版 trigger 名**（`EmitCardTrigger` 发的 `KeywordWithAbility(triggerName, …)`，
            //        形如 `Slay` / `Slay#2`）—— 先带 `#i` 后缀剥掉；
            //     ② **引擎关键词**（`SceneSig(EvtKind.Trigger, "landing")` 那一族，小写）——
            //        走 `RuleCore.TriggerNameOfKeyword` 那张反向表（正本 = 生成器 `ABILITY_TRIGGER_KEYWORD`）。
            //   ⚠️ `e.Player` 是**归属座位**（0/1，不是「是不是本人」）；越界就不记。
            if (e.Kind == EvtKind.Trigger) CountTriggerForAchievements(e.Player, e.Keyword);
            Signals.Add(e);
            AppendLog(e);
        }

        /// <summary><see cref="Emit"/> 里那一笔成就触发计数（判据与两种 `Keyword` 形态见那里）。
        /// ⚠️ 认不出 ⇒ **不记**（⛔ 不猜成 0 = `ThisCardPlayed`，那会静默错算 `Ach_*`）。</summary>
        void CountTriggerForAchievements(int seat, string keyword)
        {
            if (seat != 0 && seat != 1) return;
            if (string.IsNullOrEmpty(keyword)) return;
            // ① 先剥 `#<能力序号>` 后缀（`BattleEvent.SplitKeywordAbility` 是**唯一**解码处）
            int abilityIndex;
            string word = BattleEvent.SplitKeywordAbility(keyword, out abilityIndex);
            int ability = CardDef.AbilityTriggerNumber(word);
            if (ability < 0)
            {
                // ② 引擎关键词 → 原版 trigger 名（认不出 ⇒ `null`）
                string tn = RuleCore.TriggerNameOfKeyword(word);
                if (tn != null) ability = CardDef.AbilityTriggerNumber(tn);
            }
            if (ability >= 0) Tally.AddTrigger(seat, ability);
        }

        /// <summary>
        /// **留档**的战斗日志：一行 = 一个动作。和 <see cref="Signals"/> 的区别是
        /// **它不会被搬走** —— `Signals` 是给表现层「这一刻播什么特效」的，`DrainSignals` 一搬就空；
        /// 日志面板要的是「这一局都发生过什么」，所以在这儿再存一份。
        /// ⚠️ 来源就是 <see cref="Emit"/> —— **同一份事件流**，不另写一套（两处写同一条规则迟早不一致）。
        /// </summary>
        public readonly List<BattleEvent> ActionLog = new List<BattleEvent>();

        /// <summary>日志最多留多少行（长对局别把内存吃满）</summary>
        public int LogCapacity = 400;

        /// <summary>把事件收进留档日志。两条合并规则：
        /// ① **出单位卡会连着发 `Play` + `Deploy`** —— 只留 `Play`（不然每张单位卡都重复一行）；
        ///    单独发的 `Deploy`（效果召唤来的）照留。
        /// ② 连续同一条 `Hit` 不合并（打了几次就是几次，原版日志也是逐条列）。</summary>
        void AppendLog(BattleEvent e)
        {
            if (e.Kind == EvtKind.Deploy && ActionLog.Count > 0)
            {
                var last = ActionLog[ActionLog.Count - 1];
                if (last.Kind == EvtKind.Play && last.Player == e.Player && last.CardId == e.CardId) return;
            }
            ActionLog.Add(e);
            if (ActionLog.Count > LogCapacity) ActionLog.RemoveRange(0, ActionLog.Count - LogCapacity);
        }

        /// <summary>按字段发一条（不传的字段保持默认值）</summary>
        /// <param name="deployFrom">🆕 **`Deploy` 专用**：这张牌怎么进场的（见 <see cref="DeploySource"/>）。
        /// 其余种类保持默认 <see cref="DeploySource.Unknown"/>；⛔ 别拿它当「部署了」的判据。</param>
        /// <param name="instanceId">🆕 **单位事件的稳定实例号**（= `UnitState.Instance.Id`；`0` = 没写）。
        /// 判据与「为什么不能用座位 + 格号」见 <see cref="BattleEvent.InstanceId"/>。</param>
        /// <param name="targetInstanceId">🆕 **被指向那一份牌**的实例号（今天只有 `Attack` 填；`0` = 没写）。</param>
        /// <param name="abilityIndex">🆕 **这一拍是哪一条原版 `CardAbility`**（`cardAbilities[i]` 的 0 基下标；
        /// `-1` = 没写）。判据与「为什么需要它」见 <see cref="BattleEvent.AbilityIndex"/>；
        /// 今天只有 605（`UseFaith`）那条路填。</param>
        public void Emit(EvtKind kind, int player, int slot, string cardId,
                         string keyword = null, string effect = null,
                         int amount = 0, bool ranged = false,
                         int targetPlayer = -1, int targetSlot = -1,
                         string targetCardId = null,
                         DeploySource deployFrom = DeploySource.Unknown,
                         int instanceId = 0, int targetInstanceId = 0,
                         int abilityIndex = -1)
        {
            Emit(new BattleEvent
            {
                Kind = kind,
                Player = player,
                Slot = slot,
                CardId = cardId,
                Keyword = keyword,
                Effect = effect,
                Amount = amount,
                Ranged = ranged,
                TargetPlayer = targetPlayer,
                TargetSlot = targetSlot,
                TargetCardId = targetCardId,
                DeployFrom = deployFrom,
                InstanceId = instanceId,
                TargetInstanceId = targetInstanceId,
                AbilityIndex = abilityIndex,
            });
        }

        /// <summary>
        /// 把待播的事件**搬走并清空**（不是拷贝 —— 搬完引擎这边就没了，不会重复播）。
        /// 返回搬走了几条。
        /// </summary>
        public int DrainSignals(List<BattleEvent> into)
        {
            if (into == null) { int n = Signals.Count; Signals.Clear(); return n; }
            int count = Signals.Count;
            into.AddRange(Signals);
            Signals.Clear();
            return count;
        }

        /// <summary>丢弃待播事件（不给表现层的路径用，比如「一口气跑完 200 回合」的脚本推进）</summary>
        public void ClearSignals() { Signals.Clear(); }

        /// <summary>诊断用：两边的简况</summary>
        public string Describe()
        {
            return $"回合 {Turn}（P{Active + 1} 行动）  "
                 + $"P1 {Players[0]}  |  P2 {Players[1]}";
        }

        // ============================================================================================
        //  🆕 2026-10-26（完全复刻批次 3 · 执行代理 E1）：「开局那份」名单 + 它的记口
        //  ⚠️ 位置说明：**加在本类尾部**（本文件后面还有 `MatchType` / `MatchTypes` 两个类型，
        //     本条**不是**本文件的末尾）。选在这里是因为账里引用本文件的最大行号是 `:1868`
        //     ⇒ 在这里插入**不动任何已引用的行号**。
        // ============================================================================================

        /// <summary>
        /// 🆕 2026-10-26（完全复刻批次 3 · E1 · 账行 `BattleManager.ResolveDrawCard` 空牌库回填支）：
        /// **开局组牌时进过牌库的那些 `CardInstance.Id`** —— 原版
        /// `CardScript.createdInInitialDeck` 的等价物。
        ///
        /// <para>**判据（第一权威 · 现读）**：`d:/2/tools/il2cpp_out/dump.cs:35450`
        /// `private bool &lt;createdInInitialDeck&gt;k__BackingField; // 0x223`（`CardScript` 上的自动属性
        /// `CreatedInInitialDeck`）。读点 = `decomp_full/BattleManager._ResolveDrawCard_d__432__MoveNext.c:89`
        /// （`if (*(char *)(lVar5 + 0x223) != '\0')` ⇒ 那一段「搬回牌库」的协程）。</para>
        ///
        /// <para>**语义**：`shouldReshuffle == true` 且牌库抽空时，墓地按「从末尾往前」逐个看 ——
        /// **在这张名单里**的卡搬回牌库（落位随机）；**不在**的（战斗中造出来的副本 / 衍生物）
        /// **只从墓地删掉、不回牌库**（判据 `:169-173`）。</para>
        ///
        /// <para>⚠️ **为什么是这个形状而不是 `CardInstance` 上一个 bool**：本批白名单不含
        /// `Core/CardInstance.cs`（铁律 13·3 单写手）⇒ 先在 `ctx` 上收一份「编号名单」，
        /// 语义等价（**只判成员、从不遍历** ⇒ 不影响对局可复现；`HashSet` 的枚举序从不被读）。
        /// 📌 若将来 `CardInstance` 可改，正解是并成实例字段、并让本名单只留一个指针。</para>
        ///
        /// <para>⚠️ **写点只此一处** = <see cref="MarkInitialDeckCard"/>（唯一调用方
        /// `RuleCore.BuildPlayer`）。⛔ 别在「洗回牌库 / 造副本 / 衍生物」那些路上补写
        /// —— 原版那个 bool 也只在初始组牌时置位。</para>
        /// </summary>
        public readonly HashSet<int> InitialDeckInstanceIds = new HashSet<int>();

        /// <summary>
        /// 记下「这一份是开局牌库里那张」（原版 `CardScript.createdInInitialDeck` 的写口）。**原样返回实参**，
        /// 便于写成 `Deck.Add(ctx.MarkInitialDeckCard(ctx.NewInstance(c)))` —— 一个表达式、不加行
        /// （`RuleCore.BuildPlayer` 那一处**刻意**不增行，免得全仓账的行号整体漂）。
        ///
        /// ⚠️ `Id &lt; 0`（`CardInstance.Detached`，自检 / 演示用的分离实例）**不登记**：
        ///   那些号是**重复**的负数，登记进来会让「是不是开局那张」按号误判。
        /// </summary>
        public CardInstance MarkInitialDeckCard(CardInstance inst)
        {
            if (inst != null && inst.Id >= 0) InitialDeckInstanceIds.Add(inst.Id);
            return inst;
        }

        // ==================================================================
        //  🆕 2026-10-28（第八十八会话 · 执行代理 `A1`）：**战术卡 oath「平 / 誓二选一」的答案格**
        // ==================================================================
        //  判据 → `资料/完全复刻/判据/B88_R3_oath二选一_判据.md` §一（两个变体 SO）/ §三（时机）。
        //  🔶 自己设计（非原版）：这里存的是一个**布尔答案**（`true` = 付誓约、走完整效果），
        //  不是原版的「选哪一个 `RawCardScript` 变体」。为什么必须自己定：我们池里是**一张合并卡**
        //  （oath 效果并进 `desc`，两个变体 id 只是元数据、都不在池内）⇒ 照原版「选变体 SO」落地
        //  会动 `cards_engine` / 卡号 / 池口径（判据 §5·0 口径 B：不推荐、面广）。调度台 2026-10-28 裁定。
        //  ⚠️ **带座位号**：单机对局里两边共用这一个 `BattleContext` ⇒ 不带座位的话，
        //     玩家答的那一格会被**对手（AI / 远端）**的下一次 oath 出牌吃掉（静默错结算）。
        //  ⚠️ **一次一取**（`TakeOathAnswer` 取完即清）：一格答案只对应「那一次出牌」。
        //  ⚠️ 表现层**只在真的要问的时候**写它（`BattleDriver.ShowAsk` 的 `chooseoath` 分支）；
        //     引擎**只在原版那道闸成立时**读它（`RuleCore.DecideOathChoice`）⇒ 两边同条件。
        int _oathAnswerSeat = -1;
        bool _oathAnswerPay;
        bool _oathAnswerHas;

        /// <summary>表现层：玩家在「平 / 誓」面板上答了一条。`seat` = 答话的那个座位。</summary>
        public void SetOathAnswer(int seat, bool pay)
        {
            _oathAnswerSeat = seat; _oathAnswerPay = pay; _oathAnswerHas = true;
        }

        /// <summary>引擎：取这一次出牌的答案（**只有座位对得上才算**，取完即清）。没有答案 ⇒ false。</summary>
        public bool TakeOathAnswer(int seat, out bool pay)
        {
            pay = false;
            if (!_oathAnswerHas || _oathAnswerSeat != seat) return false;
            pay = _oathAnswerPay;
            ClearOathAnswer();
            return true;
        }

        /// <summary>还有没有没人取的答案（自检 / 防守性清理用）。</summary>
        public bool HasOathAnswer { get { return _oathAnswerHas; } }

        /// <summary>清掉这一格（表现层每次出牌前 / 判「付不起」时作废用）。</summary>
        public void ClearOathAnswer()
        {
            _oathAnswerHas = false; _oathAnswerSeat = -1; _oathAnswerPay = false;
        }

        /// <summary>
        /// 🆕 **2026-10-28（`B89-E14` · 账 L64）：「当前正在结算的这条效果是不是 extrinsic」的作用域量。**
        ///
        /// <para>**它是什么**：原版 `CardEffect.extrinsic // +0x30`（= `AbilityData.whileInPlay // +0x40`）
        /// —— 「这条增益是**持续型来源**给的，来源一离场就该被整份收回」。</para>
        ///
        /// <para>**为什么需要它**：本仓原有的两个 extrinsic 载体
        /// （① `CardInstance.HandEffect.Extrinsic` ② 光环 `Auras.GrantTag` → `UnitState.IsExtrinsicSource`）
        /// **都不经过** `EffectResolver.DoGive → ApplyOneGain`；而这个作用域量就是给那条链补的那一格。</para>
        ///
        /// <para>**为什么是「作用域量」而不是形参**：`RuleCore.ApplyHandBuffs` 把 N 条
        /// `CardInstance.HandEffect.Op` **合并成一个列表**再 `ResolveOps` —— 每条 op 带不带 extrinsic
        /// 只有 `HandEffect` 那一层知道，而 `ResolveOps → DoGive → ApplyOneGain` 这条链上
        /// **没有**任何一个格子能带它（`EffectOp` / `PayloadOp` 都没有这一列，给它们加列会波及
        /// 解析层与全部生产者）。形态照 <see cref="CardPlaySource"/>：**只在一次结算窗口内有效、save/restore**。</para>
        ///
        /// <para>**写点（全部 save/restore，别在别处写）**：`RuleCore.ResolveHandBuffOps`
        /// （`ApplyHandBuffs` 的唯一出口）—— 它把合并后的 op 列表按「extrinsic 相同」切成**连续段**，
        /// 每段设一次本量再 `ResolveOps`。**读点唯一** = `EffectResolver.ApplyOneGain` 落关键词时
        /// 传 `extrinsic: ctx.ResolvingExtrinsic`（原版合并键的一格，判据
        /// `decomp_full/CardScript__AddEffect.c:469-568`）。</para>
        ///
        /// <para>⚠️ **今天零行为变化**：`HandEffect.Extrinsic` **没有生产者**
        /// （`EffectResolver.AttachHandEffect` 的全部调用点都传 `false`）⇒ 恒 `false` ⇒ 只有一段
        /// ⇒ 与改前**逐字节相同**。</para>
        /// </summary>
        public bool ResolvingExtrinsic;
    }

    /// <summary>对局**类型** = 原版 `MatchType`（`d:/2/tools/il2cpp_out/dump.cs:27369-27391`，
    /// TypeDefIndex 563 —— **本枚举逐字照抄，19 档**）。
    /// <para>🔴 **它不是模式号**（`PlayModes` 是另一张表、另一组数字）—— 两者别混。
    /// 本局取哪一档**只由** <see cref="MatchTypes.For"/>（= `playMode` 那张 14 项派发表）决定，
    /// 而 `playMode` 就是 <see cref="BattleContext.PlayMode"/>。</para>
    /// <para>⚠️ `RankedBot 20` / `ClosedDeckBot 120` / `UnrankedBot 180` / `FastModeBot 210` 这四档
    /// **不要拿 <see cref="MatchTypes.For"/> 去取** —— 原版那张 14 项跳表确实没给它们出口，
    /// 它们是**对手是电脑时**由 `MatchData.SetBotOpponent` **就地翻转**出来的
    /// （判据 = `MatchData__SetBotOpponent.c:27-41`，逐句：`10 → 20` · `110 → 120` · `170 → 180` ·
    /// `200 → 210`，**其余档原样**）⇒ 那一次翻转收在 <see cref="MatchTypes.Effective"/>（**只此一处**）。</para>
    /// <para>🔴 **2026-10-19（`A1070`）就地订正（铁律 5）**：这一段原来写着「Bot 那几档应该是
    /// `MatchOnlineStatus` 那条链另外改的，**本地判不出**」—— **判据已经查到，是 `SetBotOpponent`**。
    /// 仍然查不到的只有 `FastModeRanked 211`（全仓没有它的写入点）。</para></summary>
    public enum MatchType
    {
        Undefined = 0,
        Ranked = 10,
        RankedBot = 20,
        PracticeOffline = 50,
        EventAI = 80,
        Practice = 90,
        Tutorial = 100,
        ClosedDeck = 110,
        ClosedDeckBot = 120,
        Campaign = 130,
        TutorialReplay = 140,
        Duel = 150,
        Replay = 160,
        Unranked = 170,
        UnrankedBot = 180,
        Scene = 190,
        FastMode = 200,
        FastModeBot = 210,
        FastModeRanked = 211,
    }

    /// <summary>`playMode` → `matchType` 那张**14 项派发表**（唯一权威）。
    ///
    /// <para>判据 = 原版 `MatchData..ctor`（RVA 0x736B70）：拿 `playMode`（字段 0x18）查表
    /// （跳转点 VA `0x180736CAE`、表在 RVA `0x736FE0`）算出 `matchType`（字段 0x1C）。
    /// 逐项读数 → `资料/普查产出_1014/RO_战场与窗口判据三件.md` §二 ①·B·5 那张表。</para>
    ///
    /// 🔴 **这一处是「模式号 → MatchType」的唯一出处**：⛔ 别在别处再写一遍 switch
    /// （两处写同一条规则 = 迟早不一致）。要加模式号就先回来补这张表。</summary>
    public static class MatchTypes
    {
        /// <summary>派生规则（逐项照原版那张表的 14 行）。</summary>
        public static MatchType For(GameMode playMode)
        {
            switch (playMode)
            {
                case GameMode.Classic:          return MatchType.Ranked;          //  0 →  10
                case GameMode.Duel:             return MatchType.Duel;            //  1 → 150
                case GameMode.PracticeLodge:    return MatchType.Practice;        //  2 →  90
                case GameMode.Dungeon:          return MatchType.EventAI;         //  3 →  80
                case GameMode.Tutorial:         return MatchType.Tutorial;        //  4 → 100
                case GameMode.CutScene:         return MatchType.Scene;           //  5 → 190
                case GameMode.OfflinePractice:  return MatchType.PracticeOffline; //  6 →  50
                case GameMode.ClosedDeck:       return MatchType.ClosedDeck;      //  7 → 110
                case GameMode.Campaign:         return MatchType.Campaign;        //  8 → 130
                case GameMode.TutorialReplay:   return MatchType.TutorialReplay;  //  9 → 140
                case GameMode.Replay:           return MatchType.Replay;          // 10 → 160
                case GameMode.RankedFriendly:   return MatchType.Unranked;        // 11 → 170
                case GameMode.OwnDeckTraining:  return MatchType.PracticeOffline; // 12 →  50（与 6 同值）
                case GameMode.Skirmish:         return MatchType.FastMode;        // 13 → 200
                default:
                    // 原版那张表**只有 14 项**（0~13），`Battle4Warpforge 14` 走不到那一行 ⇒
                    // 本地判不出它对应什么。**不编一个值**：出声 + 给 `Undefined`（红线：不许静默失败）。
                    UnityEngine.Debug.LogWarning("[Rule] `MatchTypes.For` 没有 " + playMode
                                                 + " 这一档的派生平（原版那张表只有 0~13 共 14 项）"
                                                 + " ⇒ `MatchType.Undefined`。要用它先把表补上。");
                    return MatchType.Undefined;
            }
        }

        /// <summary>🆕 2026-10-19（T11 · G-4）：原版 `BattleManager.IsCampaignLike`
        /// （`BattleManager__IsCampaignLike.c:11-29`）—— **只有 `130 Campaign` 与 `190 Scene`** 为真。
        /// 调用点（两处，都只读本方法）：`ResolveEndTurn` 的回合末附魔（`RuleCore.ResolveEndTurnEnchantment`）
        /// · `CheckWinner` 的 `playerAlwaysWins`。
        /// ⚠️ 它**不包含 140 TutorialReplay** —— `FirstSeatRules.cs` 里那个 `{130,140,190}` 是**另一个函数**的
        ///    口径（`GetPlayerGoesFirst` 那一支），⛔ 别把两者合并。
        /// </summary>
        public static bool IsCampaignLike(MatchType t)
        {
            return t == MatchType.Campaign || t == MatchType.Scene;
        }

        /// <summary>🆕 2026-10-19（T11 · G-3）：原版 `BattleManager.IsTutorialMatch`
        /// （`BattleManager__IsTutorialMatch.c:14-22`）—— `matchType ∈ {100 Tutorial, 140 TutorialReplay}`。
        /// 调用点 = 回合开始抽牌 / 天赋那两条闸的**不对称**守卫（`dontDrawCard` 不查它、`dontDrawTalent` 查）。
        /// </summary>
        public static bool IsTutorialMatch(MatchType t)
        {
            return t == MatchType.Tutorial || t == MatchType.TutorialReplay;
        }

        /// <summary>🔴 🆕 **2026-10-19（`A1070`）：本局**真正生效**的 `matchType`** —— 对手是电脑时，
        /// 原版会把它**就地翻转**一次（`MatchData.SetBotOpponent`）。
        ///
        /// **判据**（逐句可查）= `d:/2/tools/decomp_full/MatchData__SetBotOpponent.c:27-41`：
        /// <code>
        ///   iVar3 = *(int*)(matchData + 0x1C);        // = MatchData.matchType
        ///   if (iVar3 == 10)  *(…+0x1C) = 0x14;       // Ranked     10 → RankedBot     20
        ///   if (iVar3 == 110) *(…+0x1C) = 0x78;       // ClosedDeck 110 → ClosedDeckBot 120
        ///   if (iVar3 == 170) *(…+0x1C) = 0xB4;       // Unranked   170 → UnrankedBot   180
        ///   if (iVar3 == 200) *(…+0x1C) = 0xD2;       // FastMode   200 → FastModeBot   210
        ///   // 其余档**一个都不改**（50 PracticeOffline / 80 EventAI / 100 Tutorial … 本来就是 AI 档）
        /// </code>
        /// 时机 = **开局组 `MatchData` 时**（`MatchData__SetBotOpponent.c:50-80` 建对手那副牌），
        /// 比战斗期读它的 `BattleManager.AddGoesSecondCardToDeck` **早** ⇒ 那一处看到的已是翻转后的号
        /// （这就是「经典 / 遭遇这两条主路都落不到 `{50,80,110,120}` 那四档」的原因）。
        ///
        /// ⚠️ **翻转表只此一处** —— 与 <see cref="For"/> 同一条纪律：⛔ 别在别处再写一遍这张表。
        /// ⚠️ `ctx == null` 或 `ctx.BotSeat &lt; 0`（这局没有电脑）⇒ **原样返回**（= 老行为）。
        /// </summary>
        public static MatchType Effective(BattleContext ctx)
        {
            MatchType t = For(ctx == null ? GameMode.Classic : ctx.PlayMode);
            if (ctx == null || ctx.BotSeat < 0) return t;
            switch (t)
            {
                case MatchType.Ranked:     return MatchType.RankedBot;      //  10 →  20
                case MatchType.ClosedDeck: return MatchType.ClosedDeckBot;  // 110 → 120
                case MatchType.Unranked:   return MatchType.UnrankedBot;    // 170 → 180
                case MatchType.FastMode:   return MatchType.FastModeBot;    // 200 → 210
                default:                   return t;
            }
        }

        /// <summary>🔴 🆕 **2026-10-19（`A1070`）：原版「后手那张防御卡」的**随机池那一支**的模式闸**
        /// —— `matchType ∈ {50 PracticeOffline · 80 EventAI · 110 ClosedDeck · 120 ClosedDeckBot}`。
        ///
        /// **判据** = `d:/2/tools/decomp_full/BattleManager__AddGoesSecondCardToDeck.c:98-153`
        /// （`iVar2 = MatchData.matchType`；落在这一支时 `uVar9 = list[GetRandomInt(0,count)]`，
        /// 而这一支**排在那两条兜底之后** ⇒ **它会覆盖**前面算出来的那张 —— 那一处只有
        /// `playerGoesFirst == 0` 的「覆盖回玩家卡组那张」守卫能盖回来）。
        /// 注意这四个号要拿 <see cref="Effective"/>（**翻转后**的）去比 —— 原版比的也是翻转后的。
        ///
        /// ⚠️ **只此一处**：`RuleCore` 只通过它分档，⛔ 别在别处再列一遍这四个数。
        /// </summary>
        public static bool UsesDefencePool(MatchType t)
        {
            return t == MatchType.PracticeOffline      //  50（`GameMode.OfflinePractice` / `OwnDeckTraining`）
                || t == MatchType.EventAI              //  80（`GameMode.Dungeon`）
                || t == MatchType.ClosedDeck           // 110
                || t == MatchType.ClosedDeckBot;       // 120
        }

        // ============================================================================================
        //  🆕 2026-10-27（执行代理 B9_W68ENG3 · 引擎真缺口第一批 G08–G12）
        //  原版 `BattleManager` 的五个 `matchType` / 对局态谓词
        //  （判据 → `资料/完全复刻/判据/引擎真缺口_施工判据.md` §二 G08–G12 + §三 第一批）。
        //  ⚠️ 全部 `static` 纯函数（⛔ 不读 `BattleManager` 单例 —— 原版那个单例只是取值渠道），可完全单测。
        //  ⚠️ 加在本类尾部 ⇒ 不动上面任何已引用行号（账里引用到 `UsesDefencePool` 那一行）。
        // ============================================================================================

        /// <summary>原版 `BattleManager.IsAgainstAI`（`decomp_full/BattleManager__IsAgainstAI.c`，43 行，
        /// `matchType = matchData+0x1c`）。二进制是一串位测试，**逐值还原**后为真集：
        /// `{20 RankedBot, 50 PracticeOffline, 80 EventAI, 100 Tutorial, 120 ClosedDeckBot, 130 Campaign,
        /// 140 TutorialReplay, 180 UnrankedBot, 190 Scene, 210 FastModeBot}`。
        /// <para>⚠️ 这是**白名单**（⛔ 别改写成「!= Ranked」那类黑名单）—— 原版对
        /// `Undefined / Practice(90) / ClosedDeck(110) / Duel(150) / Replay(160) / Unranked(170) /
        /// FastMode(200) / FastModeRanked(211)` **一律为假**。
        /// 逐值还原见 `资料/完全复刻/判据/引擎真缺口_施工判据.md` §二 G08。</para></summary>
        public static bool IsAgainstAI(MatchType t)
        {
            switch (t)
            {
                case MatchType.RankedBot:       //  20
                case MatchType.PracticeOffline: //  50
                case MatchType.EventAI:         //  80
                case MatchType.Tutorial:        // 100
                case MatchType.ClosedDeckBot:   // 120
                case MatchType.Campaign:        // 130
                case MatchType.TutorialReplay:  // 140
                case MatchType.UnrankedBot:     // 180
                case MatchType.Scene:           // 190
                case MatchType.FastModeBot:     // 210
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>原版 `BattleManager.IsAgainstHuman`（`out_cpp2il_dev_ilspy/BattleManager.cs`，全文 2 行）=
        /// `return !IsAgainstAI();`。⛔ 别复制一份白名单（两处写同一条规则 = 迟早不一致）。</summary>
        public static bool IsAgainstHuman(MatchType t) { return !IsAgainstAI(t); }

        /// <summary>原版 `BattleManager.IsAgainstOnlineBot(bm, 0)`（`decomp_full/BattleManager__IsAgainstOnlineBot.c`，
        /// 45 行）：先判「bot 候选集」，再 `return !IsNetworkedGame()`。
        /// <para>bot 候选集（逐值还原）= `{10 Ranked, 20 RankedBot, 90 Practice, 110 ClosedDeck, 120 ClosedDeckBot,
        /// 150 Duel, 170 Unranked, 180 UnrankedBot, 200 FastMode, 210 FastModeBot, 211 FastModeRanked}`。</para>
        /// <para>🔴 **`IsNetworkedGame` 是第二个输入** —— 原版从 `BattleManager` 现取；我们引擎层**今天没有这个权威位**
        /// （`§四·D` 未闭合：唯一来源是表现层 `BattleDriver._net != null`）⇒ 这里把它**作为形参 `isNetworkedGame` 传入**，
        /// 由调用方（表现层）提供，⛔ 不在引擎里凭单例/静态猜。`isNetworkedGame == true` ⇒ 一律 `false`（联机局不叫 bot）。</para></summary>
        public static bool IsAgainstOnlineBot(MatchType t, bool isNetworkedGame)
        {
            if (isNetworkedGame) return false;   // 原版 `return !IsNetworkedGame()`
            switch (t)
            {
                case MatchType.Ranked:          //  10
                case MatchType.RankedBot:       //  20
                case MatchType.Practice:        //  90
                case MatchType.ClosedDeck:      // 110
                case MatchType.ClosedDeckBot:   // 120
                case MatchType.Duel:            // 150
                case MatchType.Unranked:        // 170
                case MatchType.UnrankedBot:     // 180
                case MatchType.FastMode:        // 200
                case MatchType.FastModeBot:     // 210
                case MatchType.FastModeRanked:  // 211
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>原版 `BattleManager.IsDuringBattle(bool includeMulligan)`（`decomp_full/BattleManager__IsDuringBattle.c`，
        /// 19 行，`UIstate = +0x290`）：
        /// `UIstate == 0` ⇒ false · `UIstate ∈ {7 finishBattleSequence, 8 postBattle}` ⇒ false ·
        /// `UIstate ∈ {11 mulligan, 12 mulliganFinished}` ⇒ **`includeMulligan`** · 其余 ⇒ true。
        /// <para>我们没有单一 `UIstate` 枚举 ⇒ 按判据文件给的映射落到两个字段：
        /// `!isOver &amp;&amp; (includeMulligan || !mulliganOpen)`（`isOver` = `Winner != 0`、`mulliganOpen` = `ctx.MulliganOpen`）。
        /// 15 档对应：`{7,8}` → `isOver`；`{11,12}` → `mulliganOpen`；其余战斗态 → 两者皆假 ⇒ true。</para>
        /// <para>⚠️ **已知近似（如实标注）**：`UIstate == 0`（inactive，**开打前**）与「正常战斗态」在
        /// `isOver/mulliganOpen` 两字段下**不可区分**（都不是 `isOver`）⇒ 本式对 `UIstate==0` 会判 true
        /// （原版判 false）。今天**0 消费者**、且开打前没人问这个谓词 ⇒ 先按判据映射实现并标注；
        /// 真用到 `0` 那一档时再补一个 `started` 位（别在这里凭空加字段猜）。</para></summary>
        public static bool IsDuringBattle(bool isOver, bool mulliganOpen, bool includeMulligan)
        {
            return !isOver && (includeMulligan || !mulliganOpen);
        }

        /// <summary>原版 `BattleManager.IsReplayMatch(bm, 0)`（`decomp_full/BattleManager__IsReplayMatch.c`，28 行）=
        /// `matchType == 140 TutorialReplay || matchType == 160 Replay`。</summary>
        public static bool IsReplayMatch(MatchType t)
        {
            return t == MatchType.TutorialReplay || t == MatchType.Replay;
        }

    }

    // ==========================================================================================
    //  🆕 B89 / E6d：`BattleContext` 的**另一半**（第二张表 = 附魔模板的注入格）
    //     写在这里（文件末）而不是类体里：`BattleContext.cs` 有 40+ 处账落点按【行号】引用，
    //     而 `MatchTypes`（`:2151`-`:2397`）里就有 `:2119`…`:2314` 一整片（含 A 行）
    //     ⇒ 类体里插成员会让它们**整体漂移**。`partial` 追加在**全部落点之后** = 零漂移。
    // ==========================================================================================
    public partial class BattleContext
    {
        // ==========================================================================================
        //  🆕 B89 / E6d（2026-10-28 · 执行代理 `E6d`）：**第二张表（附魔模板）的注入格**
        // ==========================================================================================
        //  🔴 **为什么是「静态钩子 + 懒物化」，而不是在这里直接 `CardDatabase.LoadEnchantmentTemplates()`**：
        //     `Core/` **不引用 `Data/`**（方向固定：Data → Core。`Enchantment.cs` 那条「Core 不能引用
        //     `Data/CardDatabase`」的口径就写在这里的兄弟文件里）⇒ `CardDatabase` 在**静态构造**里把
        //     自己的加载器登记进 <see cref="EnchantmentTemplatesProvider"/>（见 `Data/CardDatabase.cs` 文件末那段）。
        //     ⇒ 本文件**零外部依赖**，`ruleprobe`（只编 `Core/*.cs` + `Data/CardDatabase.cs`）与 Unity 都能编。
        //
        //  📌 与 <see cref="CardPool"/> 的分工（⛔ 别混）：
        //     · `CardPool` = **可玩卡池**（1,129 张；造牌、收藏、卡组都看它）；
        //     · `EnchantmentTemplates` = **第二张表**（85 张附魔模板；⛔ **不进** `CardPool`、不进 `count`）。
        //     判据 → `资料/完全复刻/报告/B89_E6c_附魔模板表.md` §1③ / `B89_E6d_附魔模板注入与消费.md`。
        // ==========================================================================================

        /// <summary>第二张表的**加载器钩子**（由 `Data/CardDatabase` 的静态构造登记；`null` = 没人注册 ⇒ 判不了）。
        /// <para>⚠️ 它是**进程级静态**（不是每局一份）：加载器本身是纯读表，物化出来的列表由每个
        /// <see cref="BattleContext"/> 各自持有一份引用。</para></summary>
        public static Func<List<CardDef>> EnchantmentTemplatesProvider;

        /// <summary>本局可查的**附魔模板**（第二张表）。`null` = **还没物化**（懒加载）或没人注册。</summary>
        /// <para>⛔ 与 <see cref="CardPool"/> **不是一类东西**：模板不是可玩卡（`inventoryOptions = 10` /
        /// `showInCollectionOptions = 0`），只服务「按 id 查模板」这一条读路。</para>
        public List<CardDef> EnchantmentTemplates;

        /// <summary>把附魔模板**物化一次**（幂等）。没人注册 ⇒ 保持 `null`（= 判不了，⛔ 不造假空表）。</summary>
        public void EnsureEnchantmentTemplates()
        {
            if (EnchantmentTemplates != null) return;
            var p = EnchantmentTemplatesProvider;
            if (p == null) return;
            EnchantmentTemplates = p() ?? new List<CardDef>();
        }

        /// <summary>按 id 查附魔模板：**先 <see cref="CardPool"/>（可玩池，兼容历史）→ 再第二张表**。
        /// <para>`null` = 两张表都没有它（调用方必须如实出声，⛔ 别当「这条链不适用」）。</para></summary>
        public CardDef FindEnchantmentTemplate(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var pool = CardPool;
            if (pool != null)
                for (int i = 0; i < pool.Count; i++)
                    if (pool[i] != null && pool[i].Id == id) return pool[i];
            EnsureEnchantmentTemplates();
            var tpl = EnchantmentTemplates;
            if (tpl != null)
                for (int i = 0; i < tpl.Count; i++)
                    if (tpl[i] != null && tpl[i].Id == id) return tpl[i];
            return null;
        }
    }

    // ==========================================================================================
    //  🆕 2026-10-28（B92 波 4 · 执行代理 `B7`）：**bot 对手名字的接入格**
    //  为什么追加在文件末：`BattleContext.cs` 有 40+ 处账落点按【行号】引用（含 `MatchTypes` 那一整片）
    //  ⇒ 类体里插成员会让它们整体漂移；`partial` 追加在**全部落点之后** = 零漂移（先例 = B89/E6d 那块）。
    //  判据 → `资料/完全复刻/判据/B92_战斗判据_B7.md` §3·7（`MatchData.SetBotOpponent`）；报告 → `报告/B92_B7b.md`
    // ==========================================================================================
    public partial class BattleContext
    {
        /// <summary>本局**电脑对手**的名字（原版写进对手那副牌的 `DeckAndWarlordData.deckName(+0x10)`）。
        /// <para>🔴 **判据（2026-10-28 现读）**：`decomp_full/MatchData__SetBotOpponent.c:88-101` —— 当
        /// `FeatureConfig.ShouldDisableNewMatchMaking()` 为假、且 matchType 不在那张白名单里时，
        /// 名字 = `NameGenerator.GenerateName(4, UnityEngine.Random.Range(0,10) + 4)`（4 个词、语料下标落在 [4,14)），
        /// 写进 `*(对手 DeckAndWarlordData + 0x10)`。</para>
        /// <para>⚠️ **原版那一跳用的是 `UnityEngine.Random`**（不是 `ctx.GetRandomInt`）⇒ bot 名字**本来就不参与
        /// 可复现性**、也不进 `NetProtocol.Fingerprint` / `StateHash`。所以引擎侧**不生成名字、不烧随机**，
        /// 只留这一格给表现层填：⛔ 别在 `RuleEngine/` 里引 `UnityEngine.Random`（污染可复现纪律），
        /// 也⛔ 别拿 `ctx.AiRng` / `ctx.GetRandomInt` 去抽它（会**推进**引擎随机流、改残局指纹）。</para>
        /// <para>`null` = 还没填。单机局表现层现在走的兜底见 `BattleDriver.AlliancePanelName` 的 doc
        /// （「我们**没有 bot 的名字数据源** ⇒ 取玩家看到的那一串、⛔ 不编造」—— 本格就是那个缺的数据源）。</para></summary>
        public string BotOpponentName;

        /// <summary>把 bot 对手名字一次写进两处（`BotOpponentName` + 该座位 `PlayerState.Name`）。
        /// <para>为什么两处：原版那一格是**对手那副牌的名字**（`DeckAndWarlordData.deckName`），而我们的等价载体是
        /// `PlayerState.Name`（`RuleCore.BuildPlayer` 用**卡组名**填它，现读 `RuleCore.cs:609`）。⇒ 表现层拿到名字后
        /// 调本方法，对手名牌 / 联盟面板 / HUD 三处就同源。</para>
        /// <para>接线配方（B3 的 `BattleDriver`，一行）：`ctx.ApplyBotOpponentName(
        /// NameGenerator.GenerateName(4, UnityEngine.Random.Range(0, 10) + 4));` —— `NameGenerator` 在
        /// `CardPresentation/Shell/NameGenerator.cs`（已存在；⛔ 本路没碰它）。`BotSeat &lt; 0`（这局没有电脑）
        /// ⇒ 只记 <see cref="BotOpponentName"/>、不碰座位名。</para></summary>
        public void ApplyBotOpponentName(string name)
        {
            if (string.IsNullOrEmpty(name)) return;      // 空串 = 不写（原版那一跳不会写空名）
            BotOpponentName = name;
            int b = BotSeat;
            if (b < 0 || b > 1) return;
            var p = Players[b];
            if (p != null) p.Name = name;
        }
    }
}
