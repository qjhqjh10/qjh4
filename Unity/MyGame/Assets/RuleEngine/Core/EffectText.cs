// EffectText.cs — 原版效果文本 → 结构化效果（战术卡 `desc` / 单位能力的解析器）
//
// **权威语义来源**：`d:/warpforge/scripts/rule_core.gd` 的 `_resolve_text`（`:2513`，672 行）。
// 那是逐条对着原版卡面修出来的规格书 —— **handler 顺序、正则、目标词都照它抄**，
// 不一致就是移植错了，而不是「我的实现更合理」。
//
// 和 `EffectSpec` 的分工：
//   · `EffectSpec`  —— **我们自己设计的 26 张卡**用的封闭文法（`Damage 2 EnemyUnit` 那种），
//     小而严，写错了自检抓得到。**不动它。**
//   · `EffectText`  —— **原版那 448 张战术卡的脏数据**用的解析器（本文件）。
//     原版卡面是英文自然语言，只能按句型试。
//
// 为什么「解析」和「结算」要分开（原版是混在一起边解边改 ctx）：
//   ① 覆盖率必须能**只解析、不动状态**地量出来 —— 「能解析 N/448」是这一轮的进度条；
//   ② 自检只验解析结果，不必造一局棋；
//   ③ 「半懂的卡比不懂更危险」—— 要能单独report「这句认了、那句没认」。
//
// ⚠️ 本文件属于 `Core/` —— **不允许依赖 UnityEngine**。解析失败**不打日志**（Core 里没有 Debug），
//    由调用方（自检 / 卡面的 `*` 标记）去暴露。
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RuleEngine
{
    // ==================================================================
    //  解析结果
    // ==================================================================

    /// <summary>一条解析出来的效果操作（一句话可以拆出多条）。</summary>
    public class EffectOp
    {
        /// <summary>`deal` / `heal` / `draw` / `drawtype` / `drawref` / `destroy` / `stun` / `give` /
        /// `gain` / `lose` / `deploy` / `return` / `refill` / `lowercost` / `create` / `repeat` /
        /// `chooseone`（三选一）/ `choosecard`（选牌）/ `gainenergy` / `blind`</summary>
        public string Verb;
        /// <summary>数值。没有数字的（如 `Destroy`）= 0</summary>
        public int Amount;
        /// <summary>`1-3` 这种区间的上界；没有区间 = 0</summary>
        public int AmountMax;
        /// <summary>`give` / `gain` / `lose` 的**载荷原文**（`armour 2` / `+1 attack` / `flank`）。
        /// 由 <see cref="GivePayload"/> 再解释成属性增减益或关键词。</summary>
        public string Payload;
        /// <summary>解析出来的目标。`give`/`gain` 之外的动作才有意义</summary>
        public EffectTargetSpec Target;
        /// <summary>`""` 永久 / `turn` 本回合 / `nextturn` 到你下个回合开始
        /// （原版 `_resolve_text:3018`：`this turn` → 回合结束移除；`until your next turn` → 下回合开始移除）</summary>
        public string Duration;

        /// <summary>
        /// `lowercost` 的**第二种语义：把费用设成 N**（`reduce its cost **to** 1`，2026-09-13 A4 加）。
        /// **`-1` = 没写这一支**（那时 `Amount` 表示「降多少」）。
        ///
        /// 🔴 **和 `Amount` 不是一回事**：`Lower its cost by 2` 是「降 2」，
        ///    `reduce its cost to 1` 是「**变成 1**」—— 一张 5 费的牌要降 **4**，
        ///    降多少取决于**当时那张牌的真费用**，解析期根本不知道。
        ///    ⇒ 存目标值，**差值由结算层算**（`EffectResolver.DoLowerCost`）。
        ///
        /// ⚠️ **2026-09-14 哨兵从 `0` 改成 `-1`**：`Your next Stratagem this turn costs **0**`
        ///    （`Winged Tyrant`）是「**变成 0 费**」，而旧的判据 `CostSetTo > 0` 认不出 0，
        ///    会把整句当成「没写这一支」。改成 `-1` 之后 `>= 0` 就是「写了」——
        ///    **改的是判据的取值域，不是语义**；写入侧（解析器）现在会给 0。
        /// </summary>
        public int CostSetTo = -1;
        /// <summary>原文分句（日志与卡面用）——**保留原文**才好排查「到底写了什么」</summary>
        public string Source;
        /// <summary>
        /// `Deal 3 damage to an enemy **and stun it**` —— `and` 后面那半句。
        /// 原版是递归回 `_resolve_text` 再解一次（`rule_core.gd:2751`）。
        /// ⚠️ **绝不能丢**：丢了就是「打伤害但不眩晕」的静默失效。
        /// </summary>
        public string Tail;
        /// <summary>
        /// **付费激活**的代价（`rule_core.gd:2529` 那族「`N Choose` / `N [Energy]:`」）：
        /// 文本前面写着 `4 [Energy]:` / `12 [Energy]:` / `8 [Faith]:` / `2 :`，
        /// 意思是「付这么多才生效」。`0` = 不需要付费。
        /// ⚠️ 原版是**付不起就整段不激活**（不是「付了但效果减半」），结算层必须照这个来。
        /// </summary>
        public int Cost;
        /// <summary>付费资源名（`energy` / `faith` / `""`=没写）。只有 <see cref="Cost"/> &gt; 0 时有意义</summary>
        public string CostKind;

        /// <summary>
        /// **数值取自某个阵营资源**（2026-09-13 第三十三轮）：`faith` / `spirit`，空 = 不用。
        ///
        /// 出处：卡面 `Refill Energy **equal to your Faith**`（`Emperor's Judgement`）·
        ///       `Deal damage to an enemy troop **equal to your Faith**`（`Amalia Novena`）。
        /// ⚠️ 为什么单开一个字段而不是复用 `CountRef`：`CountRef` 那条路是「**重复 N 遍**」
        ///    或者「基础 + N×增量」，语义都不同；这两个是「**数值本身就等于它**」。
        ///    结算层在 `ResolveOps` 开头按它算出 `Amount`（**一处**），下游 verb 全不用改。
        /// ⚠️ **认不出的资源名不许静默当 0** —— 结算层会如实报「本版不认识这个资源」。
        /// </summary>
        public string AmountRef;
        /// <summary>
        /// 这句话的**前置条件**原文（`If the target has Armour, deal 8 damage instead` 里
        /// `the target has armour`）。空 = 无条件。
        ///
        /// ⚠️ 原版是**在每个 handler 内部**判条件的（`:2635` 条件伤害 / `:2652` `already had X`）；
        ///    我们提到管线最前面统一剥壳（见 <see cref="EffectCondition"/>），每个 handler 就都自动带上条件。
        ///    **条件判不了就不许当成「成立」** —— 那会让卡看起来能跑、实际每次都触发。
        /// </summary>
        public string Condition;
        /// <summary>条件的规范名（<see cref="EffectCondition.Normalize"/>）。**认不出来是空串**，
        /// 调用方据此报「这条卡有条件、但我们判不了」，别静默当成立。</summary>
        public string ConditionKind;

        /// <summary>
        /// 这一条是**替换**型的（原文结尾的 `instead`）。
        ///
        /// `Deal 2 damage to an enemy troop. If it has Armour, deal 8 damage instead`
        /// —— 条件成立时是**用 8 换掉 2**，不是「2 之后再打 8」。
        /// 由 <see cref="EffectResolver"/> 在结算前成对消掉前面那条同动词的无条件 op。
        ///
        /// ⚠️ 原版在这一点上是**做坏的**：`rule_core.gd:2635` 的条件伤害分支判了条件，
        ///    但 `already_cond_kw` 只会被 `already had` 那支赋值，于是「8 伤」那段
        ///    **每次都会执行**（条件白判），而且前面那句 2 伤**照样打**。
        ///    我们按卡面文字实现替换语义 —— 这是**补原版的漏**，不是抄错。
        /// </summary>
        public bool Instead;

        /// <summary>
        /// `create` 的**目的地**：`hand`（自己手牌）/ `enemyhand`（对手手牌）/ `decktop`（自己牌库顶）。
        /// 别的动词为空串。造出来的卡放哪儿是效果的一部分，不能默认成手牌 ——
        /// `Create a random Sabotage in the enemy hand` 送到自己手上就是另一张卡了。
        ///
        /// 三种写法的实测出处（全卡池 28 个 `Create` 分句 / 20 张卡）：
        /// `in your hand` / `in the enemy hand`（也写 `in your opponent's hand`）/ `at the top of your deck`。
        /// </summary>
        public string Dest;

        /// <summary>
        /// `deploy` 的**来源**：`pool`（全卡池，默认）/ `deck`（自己牌库）/ `graveyard`（本局阵亡的部队）。
        ///
        /// 实测 50 个 `Deploy` 分句里：`from your deck` 4 个、`that died this game` 1 个。
        /// ⚠️ **`Choose a … and deploy it` 那一族（4 个）不归这里管** —— 它要「挑一张」，
        ///    是另一个 handler。
        /// </summary>
        public string DeployFrom = "pool";

        /// <summary>
        /// `Each **player** deploys N …` —— **双方各部署一次**（2026-09-13 A4 批 1）。
        ///
        /// 出处：`Birth of a Saga`（SpaceWolves）「`Each player deploys 3 troops from their deck.
        /// Your troops deployed this way gain Flank and Armour 3 this turn`」。
        /// ⚠️ 和「己方部署 N」的唯一区别就是**对手也来一遍**（各从**自己的**牌库）。
        ///    规格书 `rule_core.gd:2975` 的 `from your deck` 支**只给自己**（那是给
        ///    `Mechanised Infantry` 用的），所以这一支必须单独标出来，**不能靠 `DeployFrom` 表达**。
        /// </summary>
        public bool EachPlayer;

        /// <summary>
        /// `deploy` 的**费用区间**（0 = 不限）。实测三种写法：
        ///   · `that cost 4 or less` → 上界 4
        ///   · `that costs 6 or more` → 下界 6
        ///   · `2-cost` → **恰好 2 费**（不是「≤2」）—— 见 `TryDeploy` 的实证
        /// </summary>
        public int CostMin, CostMax;

        /// <summary>`deploy` 用：卡面写了 `random`（只影响日志措辞，选法都由 `ctx.Rng` 掷）</summary>
        public bool Random;

        /// <summary>
        /// `deploy` 用：卡面写的是 `up to N`（**至多** N 张）。
        /// 实测 1 处：`Deploy up to 5 friendly Infantry troops that died this game`。
        /// ⚠️ 和裸 `N` 的差别在**牌不够时怎么办**：裸 `N` 是「有多少凑多少、不够就重头再来」
        ///   （附录 B 的「各 N 张」是同一副牌堆的循环用法），`up to N` 是**有几张算几张**。
        /// </summary>
        public bool UpTo;

        /// <summary>
        /// `lowercost` 用：卡面写的是 **`a random <兵种词>`** ⇒ **从匹配到的那些里随机挑一张**，
        /// 不是全部一起降。
        ///
        /// 实测 4 张（2026-09-14 A5 批 3 第 6 条）：`Sergeant Telion`（Codex）·
        /// `Mekboy Gazmek`（Mob）· `Battlewagon`（Mob）· `Living Icon`（Strike）。
        ///
        /// 🔴 **为什么必须单开一栏**：`a random Infantry` 的 `a`（单数）与 `random` 都是**语义**，
        /// 不是噪声 —— 原来的解析把整串 `a random infantry in your hand` 原样塞进 `Payload`，
        /// 结算层 `IsKindWord` 只剥首词、于是拿 `random infantry` 去查 ⇒ 查不到 ⇒
        /// 退化到按卡名找 ⇒ 报「没生效」（`EffectResolver.cs` 的 `DoLowerCost`）。
        /// **就算只把冠词剥掉也还是错的** —— 那会走「手牌里所有 Infantry 一起降」那一支。
        /// </summary>
        public bool PickOne;

        /// <summary>
        /// `lowercost` 用：卡面写的是 **`next`**（`Your next Stratagem this turn costs 0`）
        /// ⇒ 这条费用修正**用完即销** —— 打出下一张符合筛选的牌之后就撤掉。
        ///
        /// 🆕 2026-09-14 A5 批 3 第 2 条。实测**这一族共 8 句**（见 `TestNextOnlyCost` 的清单）：
        /// `Your next Stratagem costs 1 less`（`Divination Menhir`）·
        /// `Your next Stratagem this turn costs 0`（`Winged Tyrant`）·
        /// `Your next troop this turn costs 1/2 less`（`Brood Progenitor` / `Digestion Pool`）·
        /// `Your next card this turn costs 1 less`（`Hive Factory`）·
        /// `Your next Vehicle costs 2 less`（`Extractor Rig`）·
        /// `Your next Infantry costs 2 less`（`Guidance of the Saints`）·
        /// `The next Vehicle you play this turn costs 2 less`（`Full Throttle`）。
        ///
        /// 🔴 **不加这一栏 = 静默地降多了**：`Your next Stratagem …` 会变成「本回合**所有**
        ///    战略卡都便宜」。判据落在 `CostMod.Once`，由 `RuleCore.ConsumeOnceCostMods`
        ///    在**真的付完费之后**撤掉（不能在 `CostOf` 那种**查询**里撤 —— 那样看一眼就烧掉了）。
        /// </summary>
        public bool NextOnly;

        // ==================================================================
        //  选牌（`Choose a … and <动词>` 一族）—— `rule_core.gd:1157 _resolve_choose`
        // ==================================================================
        //
        // 三个维度：**来源 × 筛选 × 动作**。动词用 `choosecard`（`chooseone` 是三选一，别混）。
        //
        // ⚠️ **动作经常在「下一句」里**（`Choose a troop from your deck. Draw it and create a
        //    copy of it in your hand`）—— 因为 `Split` 只按 `.` 切，那是**另一段**，它自己就能
        //    解析。本 handler 只负责「挑出那张卡」并把引用位写好，后续句自动接上。
        //    所以 <see cref="ChooseAct"/> 是**空串也是合法的**（= 只挑，不做动作）。

        /// <summary>
        /// 候选来源：`deck`（自己牌库）/ `hand`（自己手牌）/ `enemyhand`（对手手牌）/
        /// `dead`（本局阵亡的部队）/ `pool`（全卡池，**默认** —— 卡面没写「从哪来」就是生成一张）。
        /// 出处 `rule_core.gd:1185-1207`。
        /// </summary>
        public string ChooseSrc = "pool";

        /// <summary>
        /// **筛选原文**（小写），如 `troop` / `non-legendary ultramarines card` /
        /// `2-cost leviathan troop` / `friendly infantry`。
        /// 由 <see cref="CreatePool"/> 的词表 + `EffectCondition` 那套阵营/稀有度词去解释。
        /// 空串 = `Choose a card`（不筛）。
        /// </summary>
        public string ChooseWhat = "";

        /// <summary>
        /// 挑出来之后**对它做什么**（`rule_core.gd:1208-1236` 的动作表）：
        ///   · `""`       —— 只挑，不做动作（动作在下一句）
        ///   · `hand`     —— 进自己手牌（`put it in your hand` / `add it …` / `create it in your hand`）
        ///   · `draw`     —— 抽上手（`draw it`；记进 `DrawnThisResolve`，和 `Draw` 同一条路）
        ///   · `deploy`   —— 部署到场上（`deploy it`）
        ///   · `decktop`  —— 放到自己牌库顶（`at the top of your deck`）
        ///   · `todeck`   —— 洗进自己牌库（`add it to your deck`）
        ///   · `return`   —— 从手牌洗回自己牌库（`return it to your deck`）
        ///   · `shuffle`  —— 从对手手牌洗进对手牌库（`shuffle it into their deck`）
        ///   · `enemyhand`—— 进**对手**手牌（`to the enemy hand` / `to your opponent's hand`）
        ///   · `copies`   —— 复制 <see cref="ChooseCopies"/> 张进手牌（`create two copies in your hand`）
        /// </summary>
        public string ChooseAct = "";

        /// <summary>`dead` 来源的**范围**：`since_last_turn`（你上个回合之后死的）/
        /// `all`（本局死的 —— 卡面 `this battle` 与 `this game` 是**同一个意思**，
        /// 照 `rule_core.gd:1199-1204` 合并）。别的来源为空串。</summary>
        public string ChooseDeadScope = "";

        /// <summary>`ChooseAct == "copies"` 时复制几张。实测只有 1 处：`create two copies`。
        /// 别的动作恒为 0。</summary>
        public int ChooseCopies;

        // ==================================================================
        //  常驻效果 / 手牌陷阱（**回合起止触发**）
        //  规则书英文版 `:39-41`「Persistent Effects」= 写「For the rest of this battle」的卡
        //  **被弃置后依然生效**，要单独放一摞备查 —— 也就是说**卡本身是常驻效果的来源**。
        // ==================================================================

        /// <summary>
        /// 触发时机：`turn_start`（你自己的回合开始）/ `turn_end`（回合结束）。
        /// 只有 <see cref="Verb"/> 是 `persist` / `atturn` 时非空。
        /// </summary>
        public string AtTurnPhase;

        /// <summary>
        /// **触发时要结算的正文**（已经解析好的 op）。
        ///
        /// 为什么存解析结果、而不是每次触发现解：触发点在 `BeginTurn` / `EndTurn` 里，
        /// 现解等于把解析器拖进回合循环；而且**同一段文字每次解出来的必须一样**（对局要可复现）。
        /// </summary>
        public List<EffectOp> AtTurnOps;

        /// <summary>
        /// 常驻效果**作用在哪种卡上**（`Trigger == "deploy"` / 持续改费时用）。
        /// **空 = 不筛**。见 <see cref="CardCriteria"/>。
        ///
        /// 它是从目标短语里**翻译**过来的（`give Shield to all Drones you deploy` →
        /// `KindWord = "drone"`），所以和 <see cref="Target"/> 是同一件事的两种表示：
        /// 普通效果看 <see cref="Target"/>，常驻效果看这个（因为目标不是**现在**场上的谁，
        /// 而是「以后每次符合条件的那张牌」）。
        /// </summary>
        public CardCriteria Filter;

        /// <summary>
        /// **主语筛选** —— 「这句话里**谁**去做这件事」。
        ///
        /// 与 <see cref="Filter"/> 的分工：那个是「常驻效果以后作用在哪张牌上」，
        /// 这个是**当下这一句的主语**。目前只有一个动词用它：`eachunitdeal`
        /// （`Other friendly Deffkopta deal 2 damage to a random enemy` —— 主语是
        /// **别的** Deffkopta，`Target` 才是「打在谁身上」）。
        /// **空 = 己方场上全体单位**（`Each of your units deals …` 那一支）。
        ///
        /// 🆕 2026-09-14 A5 批 3 第 5 条。⚠️ **A7 光环族要的也是这一维**
        /// （`Your other units have +2` / `Other friendly Daemons cost 2 less`），
        /// 到时候**加宽这里**，别另开一栏。
        /// </summary>
        public CardCriteria Subject;

        /// <summary>
        /// 主语里**要不要排除施放者自己** —— 卡面写了 `Other`（`Other friendly Deffkopta …`）。
        ///
        /// 为什么单开一栏：`Other` 是一个**词**，不是一个筛选项 ——
        /// `Other friendly Deffkopta` 里「别的 Deffkopta」既靠 <see cref="Subject"/> 筛兵种/卡名、
        /// 又靠这一栏排掉**正在结算的那一个**。少了它，施放者自己也会打一下
        /// （`Deffkopta` 自己就是 Deffkopta）。</summary>
        public bool OtherThanSelf;

        /// <summary>
        /// 🆕 2026-09-14 A4 批 3：**常驻效果的「当……时」事件**（`Trigger == "when"` 时用）。
        ///
        /// 卡面：`For the rest of this battle, when a friendly troop uses Ferocity, deal 2 damage
        /// to the enemy Warlord`（`Raid Tactics`，SpaceWolves，4 费）。
        ///
        /// ⚠️ 和 <see cref="Filter"/> 的分工：那个是「作用在**哪种卡**上」（部署时给那一族），
        ///    这个是「**哪件事发生**时」。两者可以并存。
        /// ⚠️ 解析**复用 `WhenEvents.Parse`**（`uses &lt;关键词&gt;` 那一族早在
        ///    `WhenEvent.TryParseKeywordTrigger` 里了，注释里点名的就是这张卡）—— **别另写一份**。
        /// </summary>
        public WhenEvent When;

        /// <summary>
        /// **`for each …` 计数**：这一条要额外重复几次。
        ///
        /// 卡面三种写法实测（448 张里 38 个分句），语义是**同一个** ——
        /// 规则书 :233「选择」那节：`随机选出 N 张候选，玩家选 1 张保留/抽取/结算`，
        /// 也就是**按这个数把效果结算 N 遍**：
        ///   · 前导：`For each friendly unit, deal 1 damage to an enemy` → 重复版
        ///   · 后置：`Deal 1 damage to an enemy for each friendly unit` → 重复版
        ///   · 追加：`Give +2 Attack …, and an additional +2 Attack for each Dark Pact on it` → 加法版
        /// 前两种都归到「重复」这一个字段；第三种（`additional`）走 <see cref="Amount"/> 的加法，
        /// 由解析器当场算好期望值 —— 那是**加法**不是重复，别混。
        ///
        /// 解析器把 `for each …` 从句剥下来放这儿 —— **剥了才算解析干净**
        /// （不剥就只能判半懂，而半懂比不懂更危险）。
        /// </summary>
        public string CountRef;

        /// <summary>
        /// 计数从哪儿来：
        ///   `board`  —— 盘面（谁的、什么兵种、是否受损，全在 <see cref="CountRef"/> 里）
        ///   `draw`   —— 本次结算抽到的牌（`For each troop drawn`）
        ///   `died`   —— 本回合阵亡的单位（`For each one that dies`）
        ///   `played` —— 本局打出过的某类牌（`For each Secret you played this game`）
        /// 空 = 不计数。
        /// </summary>
        public string CountScope;

        /// <summary>
        /// **`for each` 的增量**：每个计数单位给这一条多加多少（`and an additional +2 for each X`）。
        /// `0` = 不是追加型。只有**追加型**会填这个 —— 前导/后置型填 <see cref="CountRef"/>（重复 N 遍）。
        /// ⚠️ 两者语义不同，别混：重复是「同一效果来 N 次」，增量是「一次效果、数值翻 N 倍」。
        /// </summary>
        public int PerCount;

        /// <summary>
        /// `repeat` 专用：**这一条要重放的那些 op**（本句之前的效果）。
        ///
        /// 这是 `EffectOp` 里**第一种「op 之间的引用」**。此前只有 `instead` 靠一个**下标**
        /// （`insteadBase[i]`）硬撑，`repeat` 再也撑不住 —— 它要的是「把前面那一段原样再来一遍」。
        /// 与其再加一种下标技巧，不如让 op **直接带着**它要操作的 op 列表。
        /// 由 <see cref="EffectText.Parse"/> 在整条 desc 解析完之后统一挂上（不是逐句挂 ——
        /// `Repeat this effect` 在很多张卡上是**下一个句子**，指代前一句）。
        /// </summary>
        public List<EffectOp> RepeatOps;

        /// <summary>
        /// **付费修饰型激活**（`Verb == "paidmod"`）要改的那批 op —— 就是**本卡在它之前**的效果。
        ///
        /// 出处：`rule_core.gd:1588 _energy_act_prep` 的 `replay` 栏（基础效果文本）。
        /// 由 <see cref="EffectText.Parse"/> 在整条 desc 解析完之后统一回填
        /// （逐句解析时看不到「之前」），和 <see cref="RepeatOps"/> 是同一个套路。
        /// `Payload` 记的是哪一种修饰：`extend`（`this turn` → `until your next turn`）/
        /// `permanent`（去掉时长）。**真正的结算在 `EffectResolver.DoPaidMod`。**
        /// </summary>
        public List<EffectOp> BaseOps;

        /// <summary>
        /// **第二个目标**。目前只有一个消费者：`forceattack`（强制攻击）——
        /// <see cref="Target"/> 是**攻击者**、这一栏是**被打的那个**（2026-09-13 A4 批 2）。
        ///
        /// ⚠️ 为什么不塞进 `Tail`：这两个目标在**同一句**里（`Target friendly unit attacks
        /// the enemy with highest attack`），不是「前半句 + 尾句」的关系 —— 塞 `Tail` 会
        /// 把「谁去打」和「打谁」揉成一个，那正是「打自己人」那类错的温床。
        /// </summary>
        public EffectTargetSpec Target2;

        /// <summary>
        /// 浅拷贝。**结算层算「按 for each 放大的数值」时用** ——
        /// `EffectOp` 列表是**解析产物、会被复用**（同一张卡第二次打出是同一份对象），
        /// 直接改 `Amount` 会一次比一次高（2026-09-12 撞到过 `Daemonio Frenzy` 变 +4）。
        /// </summary>
        public EffectOp Clone()
        {
            return (EffectOp)MemberwiseClone();
        }

        public override string ToString()
        {
            string a = AmountMax > 0 ? Amount + "-" + AmountMax : (Amount > 0 ? Amount.ToString() : "");
            string p = string.IsNullOrEmpty(Payload) ? "" : " " + Payload;
            string t = Target != null ? " → " + Target.Raw : "";
            string d = string.IsNullOrEmpty(Duration) ? "" : " [" + Duration + "]";
            return (Verb + " " + a + p).Trim() + t + d;
        }
    }

    /// <summary>
    /// 「相邻」的**锚点** —— 相对**谁**的相邻格。
    ///
    /// 🔴 **照抄原版枚举，别自创**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/TargetsAffected.cs:17-22`）。
    /// 原来那个 `bool Adjacent` **比原版粗一档**：五种语义都只置同一个 `true`，
    /// 于是「目标相邻」和「自己相邻」分不开 —— 那正是工程红线里的**静默错打**。
    /// 下面的数值**就是原版的枚举值**（`adjacentToSelfIfMeetsCriteria=100` 等），不是随便编的。
    ///
    /// 原版里锚点的**唯一入口**是 `TargetCriteria.targetsAffected`（`TargetCriteria.cs:9`），
    /// 它**没有任何** `Adjacent` 布尔字段 —— 所以这里也照它的形状，只留枚举。
    ///
    /// **原版怎么算相邻**（反编译 `decomp_out/BattleManager__GetAdjacentUnits.c`）：
    /// 按单位的**所属方**取那一方的 `MinionManager.GetAdjacentUnits` ⇒ **同一方棋盘行内**的左右紧邻格。
    /// 我们这一侧的判据收在 `BoardSpec.AdjacentSlots` 一处。
    /// </summary>
    public enum AdjacentAnchor
    {
        /// <summary>没定下来 —— **认不出**。结算时**空过并如实报**，绝不退回「全池」</summary>
        Unset = 0,
        /// <summary>原版 `adjacentToSelfIfMeetsCriteria = 100` —— 相对**施放者自己**（`Strike: … adjacent troops`）</summary>
        Self = 100,
        /// <summary>原版 `adjacentToPreviousTarget = 105` —— 相对**本句点名 / 上一条效果选中的那个目标**</summary>
        PreviousTarget = 105,
        /// <summary>原版 `adjacentToActingCard = 106`</summary>
        ActingCard = 106,
        /// <summary>原版 `adjacentToTargetIfMeetsCriteria = 110`</summary>
        TargetIfMeetsCriteria = 110,
        /// <summary>原版 `adjacentToFriendlyWarlordIfMeetsCriteria = 113` —— `your Warlord and adjacent …`</summary>
        FriendlyWarlord = 113,
        /// <summary>原版 `adjacentToEnemyWarlordIfMeetsCriteria = 115`</summary>
        EnemyWarlord = 115,
    }

    /// <summary>
    /// 目标短语的**组合**解释（不是 58 条硬编码）。
    ///
    /// 实测 448 张战术卡：`deal … to X` 有 38 个不同 X、`give X to Y` 有 58 个不同 Y，
    /// 但它们都是「**谁的** + **哪种** + **几个** + **时长**」的组合：
    /// `a friendly troop` / `your units this turn` / `three random enemies` / `all enemy troops` …
    /// 所以拆成字段而不是查表 —— 组合出来能覆盖的词比枚举多得多。
    /// </summary>
    public class EffectTargetSpec
    {
        /// <summary>原短语（排查用，务必留着）</summary>
        public string Raw;
        /// <summary>`self` / `own` / `enemy` / `any`</summary>
        public string Side;
        /// <summary>`unit` / `troop` / `vehicle` / `infantry` / `warlord` / `any`
        /// （原版还有更细的兵种词，遇到不认识的**原样留着**，别猜成 unit）</summary>
        public string Kind;
        /// <summary>要打几个。`0` = 全部（`all`）</summary>
        public int Count;
        /// <summary>`random` 选，还是玩家/自动规则挑</summary>
        public bool Random;
        /// <summary>
        /// 目标短语里写了「相邻」（`adjacent units` / `its adjacent units`）。
        ///
        /// ⚠️ **光有这个 bool 不够用** —— 它只说「写了」，不说**相对谁**。锚点是
        /// <see cref="Anchor"/>；这里保留 `Adjacent` 是因为**「写了 ≠ 认得出」**：
        /// `Adjacent && Anchor == Unset` 表示**还没定下来**（`Parse` 的回填会处理，
        /// 处理不了就置 <see cref="AdjacentFailed"/>）。
        /// </summary>
        public bool Adjacent;

        /// <summary>
        /// 「相邻」的**锚点** —— 相对谁的相邻格。见 <see cref="AdjacentAnchor"/>。
        /// `Unset` = 要么这句没写相邻，要么写了但**锚点还没定/认不出**（看 <see cref="Adjacent"/>
        /// 与 <see cref="AdjacentFailed"/> 分辨）。**别再拿一个 bool 表达它**。
        /// </summary>
        public AdjacentAnchor Anchor;

        /// <summary>
        /// 短语里**点名了锚点本体**，所以它**自己也进目标集**。
        ///   · `Deal 3 damage to an enemy **and its** adjacent units` → 那个 enemy 也要挨打 ⇒ true
        ///   · `Stun adjacent units` / `Give Invulnerable to adjacent troops` → 只打邻居 ⇒ false
        /// 卡面判据：`X and (its) adjacent …` 这种**并列**写法。
        /// </summary>
        public bool AnchorInSet;

        /// <summary>
        /// 卡面**根本没写主语**（`Gain +2 Attack` / `Gain Shield` 这种），是解析器兜出来的
        /// —— 2026-09-13 A3 加。`Side`/`Kind`/`Count` 只是兜底值，**真正的目标由结算层定**：
        ///   · **有施放者**（单位卡的触发式正文：`Strike:` / `Rally:` / `When …`）⇒ **就是它自己**；
        ///   · **没有施放者**（战术卡 / 防御卡那条路 `source == null`）⇒ 落到**己方全体**（既有近似）。
        ///
        /// 🔴 为什么要分开：把「没写主语」和「写了 `your units`」当成同一件事，
        ///    会让 `When you collect a Spirit Stone, gain Shield`（`Farseer`）
        ///    给**全队**上盾、`Penitence: Gain +2 Attack`（`Sisters Repentia`）给**全队** +2 攻 ——
        ///    卡面写的是这张卡自己，那是「打得比卡面宽」，而且**看不出来**（没人会去数队友的数值）。
        ///    `rule_core.gd:3167` 那句「无主语 → 友方全体」自己标着「**既有近似**」，
        ///    不是原版语义 ⇒ 我们按卡面收窄。
        /// </summary>
        public bool Subjectless;

        /// <summary>
        /// 「相邻那一圈**全部**都要」—— 卡面写的是复数（`adjacent units` / `adjacent troops` /
        /// `units adjacent to the target`）。
        ///
        /// ⚠️ **和 <see cref="Count"/> 分工**：`Count` 管的是**锚点**选几个（`PickTarget` 靠
        /// `Count == 1` 判「这张卡要不要玩家点目标」），这里管**锚点周围那一圈**取几个。
        /// 合成一个字段的话，`Cleansing Flames`（`an enemy and its adjacent units`）会变成
        /// 「不用玩家选目标」—— 锚点直接落空。
        /// </summary>
        public bool AdjacentAll;

        /// <summary>
        /// 🔴 **写了相邻、但锚点认不出** —— 整句按「半懂」处理（卡面打 `*`），**不静默按老路子打**。
        ///
        /// 为什么单开一个标记而不是让它悄悄退化成「没有相邻」：那正是原来那个 bug 的形态 ——
        /// `deal 3 damage to an enemy and its adjacent units` 被当成「打一个敌方单位」，
        /// **卡面还打着绿灯**（解析是成功的），玩家完全看不出少打了一半。
        /// 工程红线：**宁可认不出，也别静默错打**。
        /// </summary>
        public bool AdjacentFailed;
        /// <summary>「每一个」——`for each friendly unit` 那种计数层（见 <see cref="EffectOp.Verb"/> 的 for-each）</summary>
        public bool Each;
        /// <summary>原文**没写**目标词，按原版的**定死规则**自动挑（如裸 `Deal N damage` → 敌方最弱单位，
        /// `rule_core.gd:2692`）。⚠️ 这不是「不知道打谁」—— 是规则明确、只是不在文本里。
        /// **不能掷骰**：同一局必须永远可复现（见 `RuleCore.ResolveTarget` 的注释）。</summary>
        public bool Auto;
        /// <summary>
        /// 目标带了**我们过滤不了的兵种词**（`a friendly Vehicle` / `every Beast`）。
        /// 解析是通过了，但结算时**只能按整个目标池打** —— 和原版一样（它也过滤不了，
        /// 注释写着「类型过滤精度 P2」）。**必须报出来**，不然它冒充「精确打击」没人知道。
        /// </summary>
        public bool KindUnfilterable;

        /// <summary>
        /// 目标里的**兵种词**（`vehicle` / `infantry` / `beast` …），可以**真正过滤**。
        ///
        /// 2026-09-12 起 `CardDef.Subtype` 有了原版的兵种字段，所以这一类不再只能「按整个目标池打」——
        /// 结算时按 `Subtype` 精确筛（`EffectResolver.AddSide`）。
        /// ⚠️ 和 <see cref="KindUnfilterable"/> 的分工：
        ///   · 这里 —— **认得出、也过滤得了**（`a friendly Vehicle` → `vehicle`）
        ///   · `KindUnfilterable` —— **认得出但过滤不了**（卡表里查不到的兵种词，如实报出来）
        /// </summary>
        public string SubtypeFilter;

        /// <summary>
        /// **只对「刚部署上场的那个」生效** —— 卡面写 `… you deploy` / `… you put in play` 时置真。
        ///
        /// 例：`For the rest of this battle, give Shield to all Drones **you deploy**`。
        /// 结算时不查场上（那样会把**已经躺在场上**的 Drone 也加一遍），而是取
        /// <see cref="BattleContext.LastTargets"/> —— 部署那一刻刚好放进去的那一个。
        ///
        /// **原版出处**：部署走 `OnTrigger(AbilityTrigger.OtherUnitSummoned = 190)`，
        /// 事件参数里带着**被召唤的那张牌**（`CardScript__ResolveUnitSummoned.c:33` 第 5 个实参）。
        /// </summary>
        public bool Deployed;

        /// <summary>
        /// **只对这一下的「被打者」生效** —— 卡面写 `… attacked [by this unit]` 时置真
        /// （2026-09-14 A5 批 3）。
        ///
        /// 例：`Destroy any troop attacked by this unit`（`Venomthrope`）·
        /// `Destroy any enemy troop with Armour attacked by this unit`（`Blastmaster Noise Marine`）·
        /// `Stun enemy troops attacked`（`Stun` 那一族）· `Destroys any enemy troop with Hunt Mark attacked`
        /// （`Arjac Rockfist`，省略了 `by this unit`）—— **全池 6 张**。
        ///
        /// 结算时**不查场上**：那个单位由**触发方**种进 `BattleContext.LastTargets`
        /// （`RuleCore.DeclareAttack` 攻击结算之后那一块，`seed:` 参数），这里只负责
        /// **把卡面写的筛选条件补上**（`with Armour` / `with Hunt Mark` 照样要判）。
        ///
        /// **原版出处**：`AbilityTrigger.UnitAttack = 50`（`CardScript__ResolveUnitAttacked.c:30`
        /// 传 `0x32`）—— 和 `Slay` / `Strike` / `Mob` / `Regiment` **同一个函数**里，所以触发点也排在那儿。
        /// </summary>
        public bool AttackedBySelf;

        /// <summary>
        /// 目标里的**关键词**（`a troop with Destroyer` → `destroyer`）。
        /// 与 <see cref="SubtypeFilter"/> 正交：一个筛兵种、一个筛关键词。
        /// 判据走 <see cref="CardDef.Has"/>。
        /// </summary>
        public string KeywordFilter;

        /// <summary>
        /// 目标里写的是**具体某张卡的名字**（`a Stormboy` / `an Eliminator`）。
        /// ⚠️ 全等匹配，见 <see cref="CardCriteria.Name"/> 的注释（`Eliminator Sergeant` 会撞名）。
        ///
        /// 🔴 **解析器现在还没往里填**（2026-09-13）：`ParseTarget` 认不出的词仍然按老规矩
        ///    `return null` → 整句判「半懂」，**不许猜成卡名**。要填它得先有卡池才能核实
        ///    「这个名字真的存在」—— 那是**下一轮**做单位卡 `desc` 那族（`When you deploy a Stormboy…`
        ///    等 10 张）时的事。字段先留着，让 `CardCriteria` 的接口是完整的。
        /// </summary>
        public string NameFilter;

        /// <summary>
        /// 目标限定**受过伤**的（`all damaged enemy troops`）。
        ///
        /// 出处：`Oath of the Throne`（Ultramarines）的 `Oath 4: Also destroy all damaged enemy troops`。
        /// 判据照规格书 `rule_core.gd:1415` —— **`health &lt; max_health`**（不是「被标了个 damaged 标记」）。
        /// ⚠️ 它和 `CountRef` 里那个 `damaged` 是**两件事**：那个是「**数**几个受伤的」，
        ///    这个是「**打/杀**受伤的那些」。写法同形，别合并。
        /// </summary>
        public bool DamagedOnly;

        /// <summary>
        /// 目标限定**正在祈祷**的（`Each friendly unit that is Praying heals 3`）。
        ///
        /// 出处：`Devout Serenity`（Sororitas）。判据 = <see cref="UnitState.Prayed"/>
        /// （规格书 `rule_core.gd:610` 那句 `If any friendly unit is Praying` 读的也是它）。
        /// </summary>
        public bool PrayedOnly;

        /// <summary>
        /// **挑属性最高/最低的那一个**（不是「随机」也不是「卡面点名」）。
        ///
        /// 取值：`"+attack"` = 攻击力最高 · `"-health"` = 生命最低（`+`/`-` 是最高/最低）。
        /// 出处：`Target friendly unit attacks **the enemy with highest attack**`
        /// （`Peerless Bladesmen`）—— 这是**唯一的实例**（2026-09-13 A4 批 2 普查全卡池）。
        ///
        /// ⚠️ 解析时必须**抢在 `ReTargetWith` 前面**：那个正则会把 `highest attack`
        ///    当成 `with &lt;关键词&gt;` 收进 `KeywordFilter` ⇒ 按 `Has("highest attack")` 筛
        ///    ⇒ **一个都不剩、空过**（而且句子解析得干干净净）。
        /// </summary>
        public string PickMost;

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(Side).Append('/').Append(Kind);
            if (Count == 0) sb.Append(" 全部");
            else if (Count > 1) sb.Append(' ').Append(Count).Append(" 个");
            if (Random) sb.Append(" 随机");
            if (Adjacent) sb.Append(Anchor != AdjacentAnchor.Unset
                                    ? " 相邻(" + Anchor + (AnchorInSet ? "+锚点" : "") + ")"
                                    : " 相邻?（锚点认不出）");
            if (Each) sb.Append(" 每个");
            if (Deployed) sb.Append(" 刚部署的");
            if (!string.IsNullOrEmpty(KeywordFilter)) sb.Append(" 带").Append(KeywordFilter);
            if (!string.IsNullOrEmpty(NameFilter)) sb.Append(" 名为").Append(NameFilter);
            return sb.ToString();
        }
    }

    // ==================================================================
    //  解析器
    // ==================================================================

    public static class EffectText
    {
        /// <summary>一条分句的解析结果</summary>
        public enum SegKind
        {
            /// <summary>认出来了，产出了效果</summary>
            Ok,
            /// <summary>是**纯关键词声明**（`Ephemeral` / `Flying.` …）—— 关键词系统自己管，这里跳过不算失败</summary>
            KeywordOnly,
            /// <summary>认不出来</summary>
            Unknown,
            /// <summary>句型认了，但**目标或载荷的词表里没有**（半懂 —— 单独报，别混进「能解析」）</summary>
            Partial,
        }

        // ------------------------------------------------------------------
        //  分句
        // ------------------------------------------------------------------

        /// <summary>
        /// 按 `.` 分句 —— 和原版 `_resolve_text:2571` 的 `desc.split(".")` 一致。
        /// ⚠️ **照抄不要「改良」**：原版就是朴素按句点切，多句卡（448 张里 193 张）全靠这一层拆开。
        /// </summary>
        public static List<string> Split(string desc)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(desc)) return list;
            // 🆕 **换行也是句子边界**（2026-09-13 A4）：卡面排版**印不下**时会把后一句挪到下一行，
            //   而这一族**不一定带句号** —— 最典型的 `Fire and Fade`：
            //     `Return a friendly troop to your hand` ⏎ `Lower its cost by 2`
            //   只按 `.` 切的话这两句是**一句话**，整条 desc 判不认识 ⇒ **卡打不出去**（`ErrUnimplemented`）。
            // ⚠️ 加之前**逐张看过**卡池里 24 张带换行的卡（脚本扫的）：**每一处换行都是句子边界**，
            //   没有「一句话被折行」的情况 ⇒ 这次放宽不会把半句切坏。
            foreach (var raw in desc.Split(new[] { '.', '\n', '\r' }))
            {
                string s = raw.Trim();
                if (s.Length > 0) list.Add(s);
            }
            return list;
        }

        // ------------------------------------------------------------------
        //  对外入口
        // ------------------------------------------------------------------

        /// <summary>
        /// 整条 `desc` 能不能解析干净（没有不认识的句子、也没有半懂的句子）。
        ///
        /// **判据只此一处** —— `RuleCore.CanPlayTactic`（能不能打）、`DeckBuilder`（收不收进牌组）、
        /// 卡面的 `*` 标记，三处都用它。写三份早晚不一致。
        /// </summary>
        public static bool IsFullyParsed(string desc)
        {
            List<string> un, pa;
            Parse(desc, out un, out pa);
            return un.Count == 0 && pa.Count == 0;
        }

        /// <summary>
        /// 这张卡的**第一条「要玩家选一个目标」的规格**；不需要选返回 null。
        /// 表现层要拿它去问「这一格能不能选」——所以不能只给个 `"enemy"`，得给整条规格。
        /// </summary>
        public static EffectTargetSpec PickTarget(IReadOnlyList<EffectOp> ops)
        {
            if (ops == null) return null;
            foreach (var op in ops)
            {
                var t = op.Target;
                if (t == null || t.Auto || t.Random || t.Count != 1) continue;
                if (t.Side == "prev" || t.Kind == "prev") continue;
                // 🆕 **督军永远不用玩家选**（2026-09-13 A4 批 2）：一方只有一个督军，
                //    `Your Warlord gains Concussive … and heals 5`（`Da Irongob`）原来会被判成
                //    「需要点一个目标」⇒ `CanPlayTactic` 要求给格位 ⇒ **整张卡打不出去**（`ErrSlot`）。
                if (t.Kind == "warlord") continue;
                // 🆕 `… attacked [by this unit]`（2026-09-14 A5 批 3）：锚在**这一下的被打者**上，
                //    不是「让玩家点一个」—— 不排掉的话 `any enemy troop … attacked`（`Side="enemy"`）
                //    会被判成「需要选目标」。
                if (t.AttackedBySelf) continue;
                if (t.Side == "enemy" || t.Side == "own") return t;
            }
            return null;
        }

        /// <summary>
        /// 这张卡**要不要玩家选目标、选哪一侧**（表现层据此决定高亮哪边棋盘、拖拽往哪边落）。
        ///
        /// 返回 `"enemy"` / `"own"` / `""`（不需要选）。判据：第一条「只选一个、且不是随机/自动」的
        /// 目标短语属于谁 —— 和 `RuleCore.ResolveTargets` 用的 `Side` 是同一套词。
        /// </summary>
        public static string PickSide(List<EffectOp> ops)
        {
            var t = PickTarget(ops);
            return t == null ? "" : t.Side;
        }

        /// <summary>
        /// 句首的**图标前缀** `[Codex] …` → `Codex: …`（`Death from Above` 卡面就是这么印的）。
        /// 语义**等于 `Codex:`**（规则书那张触发时机表把它们当同一个东西）。
        ///
        /// 🔑 **判据只此一处**（2026-09-14 A5）：`ParseSegment`（解析）与
        /// `CardDef.CollectBareKeywordBody` 的「已经有前缀了 ⇒ 不归裸写那条管」都读它。
        /// 原来只有 `ParseSegment` 认方括号 ⇒ 裸写那条守卫**看不见** `[Codex] …`，
        /// 于是把 `Death from Above` 的**整条 desc**（含不属于 Codex 的 `Deal 4 damage`）
        /// 当成 Codex 正文收了进去 —— 静默、错、而且报表上看不出来。
        /// </summary>
        public static string NormalizeIconPrefix(string seg)
        {
            if (string.IsNullOrEmpty(seg)) return seg;
            return Regex.Replace(seg.Trim(),
                @"^\[\s*(codex|mob|oath|strike|slay|rally|backlash|penitence)\s*\]\s*",
                "$1: ", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// **`Codex:` 的语义** = 给这几个 op 挂上「**你的能量为 0 时**」的条件（规则书 `:175`）。
        ///
        /// 🔑 **全仓只此一处判据**（2026-09-14 A5 · 铁律「两处写同一条规则 = 迟早不一致」）：
        ///   ① `EffectText` 自己的 `Codex:` **前缀分支**（`ParseSegment` 的 0b2）；
        ///   ② `CardDef.CollectBareKeywordBody` 的**裸写正文**那一支
        ///      （卡面没写 `Codex:` 前缀、只在 `keywords` 里声明 `Codex` 的 10 张）。
        /// 原来只有①有这三行 ⇒ ②那 10 张的正文**挂了条件却没有条件**（等能量归零才该触发的东西
        /// 会变成「随时触发」），而且**没有任何报错**。
        /// </summary>
        public static void MarkCodexCondition(List<EffectOp> ops, string src)
        {
            if (ops == null) return;
            foreach (var o in ops)
            {
                o.Condition = "your energy is 0";
                o.ConditionKind = EffectCondition.EnergyZero;
                o.Source = src;
            }
        }

        /// <summary>
        /// 解析整条 `desc`。
        /// </summary>
        /// <param name="unparsed">认不出来的分句（原文），按出现顺序</param>
        /// <param name="partial">句型认了、但目标/载荷词表里没有的分句（原文）</param>
        /// <param name="eventHasTarget">
        /// 这段话是**某条 `When …` 监听器的正文**、而且那个事件的**宾语是个单位**
        /// （2026-09-13 A3 加）。只影响一件事：正文里**裸写的 `adjacent`** 锚点定成谁
        /// （见 <see cref="FillAdjacentAnchors"/> ⑤·二 —— 定成**事件宾语**，而不是「自己」）。
        ///
        /// ⚠️ **只传「事件确实有宾语」那一种**，不是「这段是 When 正文」就传 ——
        ///    没有宾语的那种（`When a friendly troop dies, …adjacent…`）锚点仍然**认不出**
        ///    （宁可打 `*`，也别拿「自己」去顶）。
        /// </param>
        /// <returns>解析出来的操作；**空表不等于失败**（可能整句都是关键词声明）</returns>
        public static List<EffectOp> Parse(string desc, out List<string> unparsed, out List<string> partial,
                                           bool eventHasTarget = false)
        {
            unparsed = new List<string>();
            partial = new List<string>();
            var ops = new List<EffectOp>();

            // 每句产出的 op / 原始文本 / 判定（和 `Split(desc)` 一一对应）——
            // 「相邻」锚点回填要看**前后句**，所以顺手记下来（`Parse` 之外没人需要它）
            var segOps = new List<List<EffectOp>>();
            var segText = new List<string>();
            var segKind = new List<SegKind>();

            foreach (string seg in Split(desc))
            {
                var r = ParseSegment(seg);
                segText.Add(seg);
                segOps.Add(r.Ops);
                segKind.Add(r.Kind);
                if (r.Ops != null) ops.AddRange(r.Ops);
                if (r.Kind == SegKind.Unknown) unparsed.Add(seg);
                else if (r.Kind == SegKind.Partial) partial.Add(seg);
            }

            // ---- 「相邻」锚点回填（2026-09-13 候选 F）----
            // 逐句解析看不到「前面点过谁」，所以等整条 desc 拼完再统一定锚点 ——
            // 和下面 `Repeat this effect` 的回填同一个道理。定不下来的**如实降级成半懂**。
            FillAdjacentAnchors(segText, segOps, segKind, unparsed, partial, eventHasTarget);

            // `Repeat this effect` = **把本句之前的效果原样再来一遍**。
            // 逐句解析时看不到「之前」，所以在整条 desc 拼完之后统一回填
            // （`Repeat this effect` 在很多张卡上是**下一个句子**——`Stormhawk Interception` 就是）。
            // ⚠️ 只回填**没有** RepeatOps 的：`Parse` 可能被同一份 desc 反复调用，
            //    重复回填会把引用一层层套起来（外面那层不空就跳过）。
            for (int i = 0; i < ops.Count; i++)
            {
                if (ops[i].Verb != "repeat" || ops[i].RepeatOps != null) continue;
                var prev = new List<EffectOp>();
                for (int j = 0; j < i; j++)
                    if (ops[j].Verb != "repeat") prev.Add(ops[j]);   // 不把别的 repeat 再包进来
                ops[i].RepeatOps = prev;
            }

            // `6 [Energy]: Extend effect …` / `8 [Energy]: Give it permanently` ——
            // **付费修饰型激活**要把「本卡在它之前的那批效果」带上（2026-09-13 A4 批 1）。
            // 和上面的 `repeat` 同一个道理：逐句解析时看不到「之前」。
            // ⚠️ 同样只在**没回填过**时填（`Parse` 会被同一份 desc 反复调用）。
            for (int i = 0; i < ops.Count; i++)
            {
                if (ops[i].Verb != "paidmod" || ops[i].BaseOps != null) continue;
                var prev = new List<EffectOp>();
                for (int j = 0; j < i; j++)
                    if (ops[j].Verb != "paidmod") prev.Add(ops[j]);
                ops[i].BaseOps = prev;
            }
            return ops;
        }

        /// <summary>
        /// 把**锚点还没定**的「相邻」目标按上下文定下来；定不下来就如实标成**认不出**。
        ///
        /// 只有**裸写法**会走到这里 —— 明写的（`its adjacent` / `to the target` / `your Warlord and` /
        /// `this troop and`）在 `ParseTarget` 里就定完了。
        ///
        /// 判据（**把 36 张卡面逐张看过**总结的，例卡写在每支后面；不是拍脑袋）：
        ///   ① **同一句里、在它之前**点过别的目标 → `PreviousTarget`
        ///   ② **上一句**点过目标 → `PreviousTarget`（`Destroy an enemy troop. Stun adjacent units`）
        ///   ③ 同一句里出现过 `the target` → `PreviousTarget`（`If the target dies, …adjacent units`）
        ///   ④ 都不是 → 相对**施放者自己**（`Heal 1 to adjacent units` · `Give Invulnerable to adjacent troops`）
        ///   ⑤ 🔴 这句是 `When …` 触发的 ⇒ 锚点在**事件参数**里，文本层定不出来
        ///      （`When this unit attacks an enemy with Hunt Mark, deal 3 damage to adjacent enemies`）
        ///      → 标 `AdjacentFailed`、整句降级成**半懂**、卡面打 `*`。
        ///      **宁可认不出，也别拿「自己」去顶** —— 那就是静默错打。
        ///   ⑤·二 🆕 2026-09-13 A3：**事件确实带宾语**时（`eventHasTarget`），裸 `adjacent`
        ///      定成 <see cref="AdjacentAnchor.TargetIfMeetsCriteria"/> = **相对被攻击的那个**。
        ///      依据：原版 `TargetsAffected.target = 30` 就是「事件的目标」，
        ///      而 `adjacentToTargetIfMeetsCriteria = 110` 是它配套的相邻锚点。
        ///      ⚠️ **只在「事件有宾语」时走这一支**（卡池实测只有 `Long Fang` 一张是这个形状）——
        ///        没有宾语的事件（`When a friendly troop dies, …adjacent…`）继续按 ⑤ 认不出。
        /// </summary>
        static void FillAdjacentAnchors(List<string> segText, List<List<EffectOp>> segOps,
                                        List<SegKind> segKind,
                                        List<string> unparsed, List<string> partial,
                                        bool eventHasTarget = false)
        {
            EffectTargetSpec prevSegNamed = null;      // 上一句**点名**的那个目标（用来分辨「督军」那一支）
            for (int i = 0; i < segText.Count; i++)
            {
                var list = segOps[i];
                string low = (segText[i] ?? "").ToLowerInvariant();
                EffectTargetSpec segNamed = null;      // 本句里**在它之前**点过名的目标
                bool adjFailed = false;

                if (list != null)
                    foreach (var op in list) FillOne(op);

                // ⑤ 认不出的**整句降级**：`IsFullyParsed` 因此判它半懂 ⇒ **卡面打 `*`**、不进牌组。
                //    这就是「宁可认不出」的落地 —— 不降级的话它会带着一个错的锚点照常跑。
                if (adjFailed && segKind[i] == SegKind.Ok)
                {
                    segKind[i] = SegKind.Partial;
                    unparsed.Remove(segText[i]);
                    if (!partial.Contains(segText[i])) partial.Add(segText[i]);
                }
                prevSegNamed = segNamed;   // 只带**紧邻的上一句**（`Destroy an enemy troop. Stun adjacent units`）

                // 一条 op：定锚点；**并且递归进它挂着的子 op 表**
                // （`At the start|end of your turn, <正文>` 的正文在 `AtTurnOps` 里 ——
                //  `Stealth Drone` 的 `give Stealth to an adjacent troop` 就挂在那儿；
                //  不递归的话那些 op 的锚点会**永远停在「待定」**，结算时空过）
                void FillOne(EffectOp op)
                {
                    var t = op.Target;
                    if (t != null)
                    {
                        if (t.Adjacent && t.Anchor == AdjacentAnchor.Unset)
                        {
                            var named = segNamed ?? prevSegNamed;
                            if (named != null)
                            {
                                // 点名的是**督军**时走原版那一支（`your Warlord and adjacent …`）
                                t.Anchor = named.Kind == "warlord"
                                    ? (named.Side == "enemy" ? AdjacentAnchor.EnemyWarlord
                                                             : AdjacentAnchor.FriendlyWarlord)
                                    : AdjacentAnchor.PreviousTarget;                   // ① ②
                            }
                            else if (low.Contains("the target"))
                                t.Anchor = AdjacentAnchor.PreviousTarget;              // ③
                            else if (low.StartsWith("when ") || low.StartsWith("whenever "))
                            {
                                t.AdjacentFailed = true;                               // ⑤
                                adjFailed = true;
                            }
                            // ⑤·二 事件带宾语 ⇒ 相对**被攻击的那个**（`Long Fang`）。
                            //   判据是**事件**那一侧的（`eventHasTarget`），不是这句文本自己的 ——
                            //   文本层看不出宾语是谁（那正是 A1 把它标成「认不出」的原因）。
                            else if (eventHasTarget) t.Anchor = AdjacentAnchor.TargetIfMeetsCriteria;
                            else t.Anchor = AdjacentAnchor.Self;                       // ④
                        }

                        // 「在这之前点过具体目标了吗」—— 指代（`it` / `them`）不算：那是**回指**，
                        // 不是新点名的目标（拿它当锚点会把「上上条」的效果也算进来）
                        if (!t.Adjacent && t.Side != "prev" && t.Kind != "prev") segNamed = t;
                    }
                    if (op.AtTurnOps != null) foreach (var o in op.AtTurnOps) FillOne(o);
                }
            }
        }

        public class SegResult
        {
            public SegKind Kind = SegKind.Unknown;
            public List<EffectOp> Ops;
        }

        /// <summary>
        /// 这一句是不是**纯关键词声明**（`Ephemeral` / `Blast 3` / `Blood Thirst`）。
        ///
        /// 判据：去掉结尾的数字/标点后，**关键词前缀要吃满整句** ——
        /// `KeywordTable.Normalize` 只做前缀匹配，单看「非 null」会把 `Stun a random enemy` 也认成关键词。
        /// </summary>
        public static bool IsKeywordOnly(string seg)
        {
            if (string.IsNullOrEmpty(seg)) return false;
            string t = seg.Trim();
            // 🆕 2026-09-14 A7：**括号写法的关键词值**（`Stealth (1)`）。
            //    方括号那一版（`Stealth [1]`）上面那条一直剥得掉，**圆括号版剥不掉** ⇒
            //    整句判「完全不认识」（`Stealth (1)` 实测全池 4 处，一直在 ① 栏里挂着）。
            //    ⚠️ 只剥**句尾**那种「(纯数字)」：别的括号（`(this turn)`、`(see below)`）不碰 ——
            //    剥完认不出关键词的，`Normalize` 那一关照样拦得住。
            t = Regex.Replace(t, @"\(\s*\d+\s*\)\s*$", "");
            t = Regex.Replace(t, @"[\s\d\.\-,;]+$", "").Trim().ToLowerInvariant();
            if (t.Length == 0) return false;
            if (t.Contains(":")) return false;              // `Talent: …` / `Rally: …` 是效果段落，不是纯关键词
            int len;
            if (KeywordTable.Normalize(t, out len) == null) return false;
            return len == t.Length;                          // 吃满整句才算
        }

        // ------------------------------------------------------------------
        //  覆盖率（这一轮的进度条）
        // ------------------------------------------------------------------

        /// <summary>
        /// 一批卡的文本解析覆盖情况。
        ///
        /// **为什么要分三个数**：「能解析」和「解析得对」不是一件事 ——
        /// 一句话认了句型、但目标词表里没有（`units adjacent to the target`），
        /// 那是**半懂**，比完全不懂更危险（卡看起来能跑、实际少打一半）。所以单独报。
        /// </summary>
        public class TextCoverage
        {
            public int Cards;
            /// <summary>整条 desc 都解析干净了的卡数（**这才是「能打」的口径**）</summary>
            public int Full;
            /// <summary>有话认了、也有话认不出来的卡数</summary>
            public int Partial;
            /// <summary>一句都没认出来的卡数</summary>
            public int None;

            /// <summary>
            /// **载荷有机制**的卡数（在 <see cref="Full"/> 的基础上再过一层）。
            /// `give Flank to a friendly troop` 解析得出来，但 `Flank` 没机制 → 「能打但没用」。
            /// ⚠️ 两个数**必须分开报**：只报「能解析」会掩盖这类**静默失效**。
            /// </summary>
            public int FullAndMechanized;
            /// <summary>解析得出来、但载荷没机制的卡名（卡面该打 `*`）</summary>
            public readonly List<string> NoMechCards = new List<string>();
            /// <summary>缺机制的关键词 → 出现次数（按频次排，决定下一个补哪个关键词）</summary>
            public readonly Dictionary<string, int> NoMechFreq = new Dictionary<string, int>();

            /// <summary>
            /// **打得比卡面宽**的卡：目标带兵种词（`a friendly Vehicle`）而我们过滤不了，
            /// 结算时只能按整个目标池打（原版也一样）。
            /// ⚠️ 和「没机制」是**两回事**，措辞别混：这些卡**会生效**，只是**打多/打错人**。
            /// </summary>
            public readonly List<string> ImpreciseCards = new List<string>();
            public readonly Dictionary<string, int> ImpreciseFreq = new Dictionary<string, int>();

            public int SegTotal, SegKeyword, SegOk, SegPartial, SegUnknown;

            public readonly List<string> UnknownExamples = new List<string>();
            public readonly List<string> PartialExamples = new List<string>();
            /// <summary>分句 → 出现次数（**按频次排**才知道下一个该实现哪个 handler，
            /// 比「前 40 个不认识的」有用得多）</summary>
            public readonly Dictionary<string, int> UnknownFreq = new Dictionary<string, int>();
            public readonly Dictionary<string, int> PartialFreq = new Dictionary<string, int>();
            /// <summary>完全解析不了的卡名（卡面该打 `*`）</summary>
            public readonly List<string> NoneCards = new List<string>();

            public string Summary()
            {
                return $"战术卡文本解析：完全解析 {Full}/{Cards}"
                     + $"，其中**载荷有机制** {FullAndMechanized}"
                     + $"，部分 {Partial}，完全不懂 {None}"
                     + $"（分句 {SegTotal}：关键词声明 {SegKeyword} / 认了 {SegOk}"
                     + $" / 半懂 {SegPartial} / 不认 {SegUnknown}）";
            }
        }

        /// <summary>频次表加一（None 安全 —— 表是 readonly 的字段，但里面可变）</summary>
        static void Bump(Dictionary<string, int> freq, string key)
        {
            int n;
            freq[key] = freq.TryGetValue(key, out n) ? n + 1 : 1;
        }

        /// <summary>
        /// **这一条 op 有没有机制** —— 「解析通过」≠「打出去有反应」。
        /// 返回 `true` = 有机制；`false` 时 <paramref name="why"/> 给一句人话，
        /// 且 <paramref name="imprecise"/> 区分两类：
        ///   · `false` = **真没机制**（这些卡卡面该打 `*`）
        ///   · `true` = 「**打得比卡面宽**」—— **会生效**，只是打多/打错人。**两回事，别混。**
        ///
        /// 🔴 **只此一份判据**：<see cref="Coverage"/>（全局统计）与
        ///    `RuleEngineTest.ReportFactionCoverage`（逐阵营表）**都读它**。
        ///    2026-09-14 发现两处各写了一份 —— 逐阵营那份少了 `create` 的池子检查与
        ///    `chooseeffect` 的手牌作用域，于是**同一张卡在两张报表里一个算「有机制」一个算「没机制」**
        ///    （本工程反复强调的那类分叉：两处写同一条规则 = 迟早不一致）。
        /// </summary>
        public static bool OpHasMechanism(EffectOp op, string faction, IReadOnlyList<CardDef> createPool,
                                          out string why, out bool imprecise)
        {
            why = null; imprecise = false;
            // **动词本版没实现** —— 解析得出来，但 `ResolveOne` 里没有分支，
            // 打出去什么都不发生。必须报，不然它们冒充「有机制」（见 `RuleCore.ImplementedEffectVerbs`）。
            if (!RuleCore.ImplementedEffectVerbs.Contains(op.Verb))
            { why = "动词「" + op.Verb + "」本版没实现  ← " + op.Source; return false; }

            // **选效果：作用域「给手牌」本版没做**（🆕 2026-09-14 T3）——
            // 手牌里放的是**共享不可变的 `CardDef`**，没有卡实例可以挂一份加成
            // （手里两张同名卡是**同一个对象**）。解析得出来，但打出去什么都不发生。
            if (op.Verb == "chooseeffect" && op.Payload == "hand")
            { why = "选效果的作用域「给手牌」本版没做  ← " + op.Source; return false; }

            // **造牌的候选池**：解析出来容易，**池子算不算得出来**是另一回事
            // （具名卡原版数据里有没有、兵种词认不认得）。有卡池就真算一遍。
            if (op.Verb == "create" && createPool != null)
            {
                var cr = CreatePool.Resolve(createPool, op.Payload, faction);
                if (!cr.CopyOfPrev && !cr.Ok)
                { why = "造牌池： " + cr.Why + "  ← " + op.Payload; return false; }
            }

            // **兵种词过滤不了** —— 会生效，只是打得比卡面宽（单独一栏）
            if (op.Target != null && op.Target.KindUnfilterable)
            { why = op.Target.Raw; imprecise = true; return false; }

            // 条件判不了 = 没机制。**当成「条件成立」会让它每次无条件触发**，比不实现更糟。
            if (!string.IsNullOrEmpty(op.Condition) && op.ConditionKind.Length == 0)
            { why = "条件判不了  ← " + op.Condition; return false; }

            // ⚠️ **2026-09-12 更正**：这里原来有一条「付费激活没接结算」的检查，是**过期的误报**
            //    （`ResolveOne` 开头早就实现了付费分支，付不起整条不生效，照 `rule_core.gd:2529`）。
            //    留着它会把 `Oath N:` / `4 [Energy]:` 那一族卡错报成没机制。**别加回来。**
            if (string.IsNullOrEmpty(op.Payload)) return true;

            // ⚠️ **只有 `give`/`gain`/`lose` 的载荷才归 `GivePayload` 管**。
            //    别的动词也带 `Payload`（`deploy` 的目标名、`drawtype` 的类型词、`repeat` 的条件），
            //    拿它们去问 `GivePayload` 只会得到一堆假的「载荷词表里没有」
            //    （2026-09-12 撞到：`← troop` ×5 其实是 `Deploy a troop` 的目标名）。
            if (op.Verb != "give" && op.Verb != "gain" && op.Verb != "lose") return true;
            string w;
            if (GivePayload.Mechanized(op.Payload, out w)) return true;
            why = w + "  ← " + op.Payload;
            return false;
        }

        /// <summary>
        /// 量一批卡的文本解析覆盖（**只解析、不动状态**）。默认只看战术卡。
        /// </summary>
        /// <param name="createPool">
        /// **全卡池** —— 用来验「造牌」那一条的候选池算不算得出来（`CreatePool.Resolve`）。
        /// 不传就**不验造牌**（造牌仍算「有机制」，只是没人核过池子）。
        /// 自检里传 `CardDatabase.Load()`；`Core/` 不认识 UnityEngine，所以只能由调用方给。
        /// </param>
        public static TextCoverage Coverage(IEnumerable<CardDef> cards, string type = "tactic",
                                            IReadOnlyList<CardDef> createPool = null)
        {            var cov = new TextCoverage();
            if (cards == null) return cov;
            var seenUnknown = new HashSet<string>();
            foreach (var c in cards)
            {
                if (c == null) continue;
                if (type != null && c.Type != type) continue;
                cov.Cards++;

                // 🆕 2026-09-14 A4 批 4：**手牌陷阱**的 desc **不是「打出时结算的效果」** ——
                //    它躺在**持有者手里**监听事件（`Jammed Communications`）或回合起止
                //    （`Poisoned Supplies` / `Cult Propaganda`），由 `EffectResolver` 那两条
                //    手牌扫描结算，**不走 `EffectText.Parse` 这条路**（也**不该**走）。
                //    ⇒ 拿「打出时解析得出来吗」当尺子量它，量出来的是**假账**：
                //      `Jammed Communications` 在这句话被收成手牌陷阱监听器之后**已经能用**了，
                //      而报告还把它列在「完全解析不了（卡面该打 `*`）」里、还判它**进不了牌组**。
                //    ⚠️ 判据只有一处（<see cref="IsHandTrap(CardDef)"/>）——
                //       `CanPlayTactic` / `DeckBuilder` / 那两条扫描读的是同一份。
                if (IsHandTrap(c)) { cov.Full++; cov.FullAndMechanized++; continue; }

                var unparsed = new List<string>();
                var partial = new List<string>();
                var ops = Parse(c.Desc, out unparsed, out partial);

                // 🆕 2026-09-14 A5 批 1：**已经由别的层接手的句子，不算「不认识」**。
                //   实测（逐句探针 + `cardface_fixes` 都没错时才敢这么写）：单位卡有三族句子
                //   **机制一直在跑**，而 `EffectText` 认不出「那句壳」——
                //   它们一直挂在 `_tmp_view/unit_desc_unparsed.txt` 的 ① 栏里，**报表虚低**，
                //   下一个人照着做就会去重做已经做完的事（这正是 `资料/单位卡desc与光环_批次划分.md`
                //   §一⑤ 记的那个坑）。三族：
                //     · `When <事件>, <正文>`（≈21 次）—— 走**事件层**（`CardDef.AddWhenTrigger`，
                //       它只解析**正文那半句**）；实据：`when_unparsed.md` 认不出的事件短语 **0 种**、
                //       自检 `TestWhenEvents` 有**结算级**断言。
                //     · `Talent: <名>`（35 次）—— 走 `TalentName` + `RuleCore.SpawnTalents`。
                //     · `Companion N: <卡名>`（8 次）—— 走 `CompanionName` + `RuleCore.PlayCard`。
                //   ⚠️ 判据**全部转调 `CardDef.HandledByOtherLayer`**（它再转调那三个采集器用的
                //      抽取函数）—— **一行新文法都不写**，否则就是「两处写同一条规则」。
                //   ⚠️ 卡面**不会**因此骗人：`*` 那条装饰只打在**战术卡**上
                //      （`BattleDriver.cs:2222` 明写 `c.Type == "tactic"`），单位卡不受这条影响。
                for (int i = unparsed.Count - 1; i >= 0; i--)
                    if (CardDef.HandledByOtherLayer(c, unparsed[i]) != null) unparsed.RemoveAt(i);
                for (int i = partial.Count - 1; i >= 0; i--)
                    if (CardDef.HandledByOtherLayer(c, partial[i]) != null) partial.RemoveAt(i);

                int kwOnly = 0, ok = 0, bad = 0;
                foreach (string seg in Split(c.Desc))
                {
                    // 🆕 2026-09-14 A5 批 1：**已由别的层接手**的句子算「认了」（见上面那一段的说明）。
                    if (CardDef.HandledByOtherLayer(c, seg) != null) { ok++; continue; }
                    var r = ParseSegment(seg);
                    switch (r.Kind)
                    {
                        case SegKind.KeywordOnly: kwOnly++; break;
                        case SegKind.Ok: ok++; break;
                        case SegKind.Partial: bad++; break;
                        default: bad++; break;
                    }
                }
                cov.SegKeyword += kwOnly;
                cov.SegOk += ok;
                cov.SegPartial += partial.Count;
                cov.SegUnknown += unparsed.Count;

                if (unparsed.Count == 0 && partial.Count == 0) cov.Full++;
                else if (unparsed.Count > 0 && ok + kwOnly == 0) { cov.None++; cov.NoneCards.Add(c.Name); }
                else cov.Partial++;

                // ---- 第二层：载荷**有机制**吗 ----
                if (unparsed.Count == 0 && partial.Count == 0)
                {
                    bool allMech = true;
                    foreach (var op in ops)
                    {
                        // 判据**只此一份**（`OpHasMechanism`）—— 逐阵营那张表读同一个。
                        string why; bool imprecise;
                        if (OpHasMechanism(op, c.Faction, createPool, out why, out imprecise)) continue;
                        allMech = false;
                        if (imprecise) Bump(cov.ImpreciseFreq, why);
                        else Bump(cov.NoMechFreq, why);
                    }
                    if (allMech) cov.FullAndMechanized++;
                    else if (cov.NoMechCards.Count < 30) cov.NoMechCards.Add(c.Name);
                }

                foreach (string s in unparsed)
                {
                    if (cov.UnknownExamples.Count < 40 && seenUnknown.Add(s))
                        cov.UnknownExamples.Add(c.Name + " · " + s);
                    int n0;
                    cov.UnknownFreq[s] = cov.UnknownFreq.TryGetValue(s, out n0) ? n0 + 1 : 1;
                }
                foreach (string s in partial)
                {
                    if (cov.PartialExamples.Count < 25) cov.PartialExamples.Add(c.Name + " · " + s);
                    int n1;
                    cov.PartialFreq[s] = cov.PartialFreq.TryGetValue(s, out n1) ? n1 + 1 : 1;
                }

                cov.SegTotal += kwOnly + ok + bad;
            }
            return cov;
        }

        // ------------------------------------------------------------------
        //  逐分句：handler 管线
        // ------------------------------------------------------------------

        /// <summary>
        /// 一条分句 → 效果操作。
        ///
        /// **handler 顺序照抄 `rule_core._resolve_text`（`:2516` 的函数头写着）**：
        ///   deal → stun → destroy → heal → draw → return → refill → deploy → give/lose/gain
        /// ⚠️ **顺序本身是语义的一部分**（先具体后一般），别按字母序重排。
        /// </summary>
        public static SegResult ParseSegment(string seg)
        {
            var r = new SegResult { Ops = new List<EffectOp>() };
            if (string.IsNullOrEmpty(seg)) { r.Kind = SegKind.KeywordOnly; return r; }

            // `[Codex] Deal 1 additional damage` —— 方括号里是**图标**，语义等于 `Codex: …`
            // （`Death from Above` 卡面就是这么印的）。先把带图标的触发前缀换成 `x:` 再往下走，
            // 不然方括号一剥就变成 `Codex Deal 1 additional damage`，谁都不认识。
            // ⚠️ 归一那一行**转调** `NormalizeIconPrefix`（`CardDef` 的裸写守卫读同一份）。
            string s0 = NormalizeIconPrefix(seg);
            string s = s0.Replace("[", "").Replace("]", "").Trim();
            // 圈码（`① ② ③`）是**次数标记**，不是句型的一部分 —— 原版靠 `contains("repeat this effect")`
            // 直接绕过它，我们把它剥掉，好让「不认识的句子」按频次排名时 key 是干净的
            // （不然 `② Repeat this effect` 和 `Repeat this effect` 会被算成两句）。
            s = StripCircled(s).Trim();

            // 句首的**语气词**，不是句型的一部分：`Also destroy all damaged enemy troops`（Oath 4 那段）/
            // `Then, if it has 0, destroy it`。不剥的话句子不以动词开头，所有 handler 都失配。
            // ⚠️ **只剥句首、只剥这两个词** —— 松一点就会吃到有意义的内容。
            s = StripVocative(s);
            // 句首的**图标字形**（`⚡ Rally: …` —— 卡面把触发关键词印成图标，OCR 出来就是那个符号）。
            // ⚠️ **只剥 `UnicodeCategory.OtherSymbol` 与代理对**（`⚡`/`💀`/`⭐` 这类），
            //    不碰 `MathSymbol`（`+`/`-`）—— 有句子真的以 `+` 开头（`+1 Attack to …`）。
            //    不剥的话 `^([a-z]+)\s*:` 那条触发前缀匹配看不到 `rally`，
            //    整句会被后面的 give 系 handler 从中间截走（实测 `Hrolf the Ironhowl` 那句）。
            s = StripLeadingIcons(s).Trim();
            if (s.Length == 0) { r.Kind = SegKind.KeywordOnly; return r; }
            string low = s.ToLowerInvariant();

            // ---- 付费激活前缀 `12 [Energy]: …` / `4 : …` / `8 [Faith]: …` ----
            // `rule_core.gd:2529` 那族（原版是「付不起就**整段不激活**」）。前缀在这里剥掉、
            // 代价记进 op，正文照常往下走各个 handler。
            int paidCost = 0; string paidKind = null;
            // ⚠️ **先试无冒号那条**（`1 Also give it Shield` / `(1) Draw a card`）——
            //    它有「后面必须是动词」的判据，比带冒号那条**更窄**，先试不会吃掉老句子。
            var mp = RePaidBare.Match(low);
            if (!mp.Success) mp = RePaid.Match(low);
            if (mp.Success)
            {
                paidCost = int.Parse(mp.Groups["nParen"].Success ? mp.Groups["nParen"].Value
                                                                 : mp.Groups["n"].Value);
                paidKind = CostKindOf(mp.Groups["curB"].Success ? mp.Groups["curB"].Value
                                   : mp.Groups["curW"].Success ? mp.Groups["curW"].Value : "");
                low = mp.Groups["body"].Value.Trim();
                // 🔴 **付费前缀之后还要再剥一次语气词**（2026-09-13 A4 批 1）——
                //    `Oath 4: Also destroy all damaged enemy troops`：`Oath 4:` 剥掉之后
                //    剩下的是 `Also destroy …`，句首那个 `Also` 在**整段开头那次剥**里
                //    已经过去了（它在冒号后面），不补这一次整句就不认。
                low = StripVocative(low);
                s = low;
                if (low.Length == 0) { r.Kind = SegKind.Unknown; r.Ops = null; return r; }
            }

            // ---- 数值 = 你的阵营资源：`… equal to your Faith`（2026-09-13 第三十三轮）----
            // 实测两处：`Emperor's Judgement`「Refill Energy **equal to your Faith**」·
            //           `Amalia Novena`「Rally: Deal damage to an enemy troop **equal to your Faith**」。
            // 从句在句尾 ⇒ 先剥掉、记在 `op.AmountRef` 上，**数值由结算层算**（`EffectOp.AmountRef` 的注释）。
            string amountRef = null;
            var mres = ReEqualResource.Match(low);
            if (mres.Success)
            {
                amountRef = mres.Groups[1].Value.ToLowerInvariant().StartsWith("faith") ? "faith" : "spirit";
                low = low.Substring(0, mres.Index).Trim();
                s = low;
                if (low.Length == 0) { r.Kind = SegKind.Unknown; r.Ops = null; return r; }
            }

            // ---- 🆕 `Oath N: …`（极限战士）= **付费激活**，只是写法用了阵营词 ----
            // 出处：规则书 :194「誓言 X：部署时支付 X 能量以触发效果」；
            //       `rule_core.gd:869` 那条 `oath (\d+):?\s*repeat this effect` 就是同一件事的特例。
            // ⇒ 和 `12 [Energy]: …` 复用**同一条**付费路径（`op.Cost`），不另开一套。
            var mo = ReOathPaid.Match(low);
            if (mo.Success)
            {
                paidCost = int.Parse(mo.Groups[1].Value);
                paidKind = "oath";
                low = mo.Groups[2].Value.Trim();
                low = StripVocative(low);      // 同上：`Oath 4: **Also** destroy …`
                s = low;
                if (low.Length == 0) { r.Kind = SegKind.Unknown; r.Ops = null; return r; }
            }

            // ---- 🆕 可路由的**触发前缀**（`Rally: …` / `Strike: …` / `Slay: …` …）----
            // 🔴 单位卡的触发正文**在引擎里不是按整句解析的** —— 走的是 `CardDef.AddTriggerOp`：
            //    冒号后那段**单独**送进 `EffectText.Parse(body)`（`CardDef.cs:251`，判据是
            //    `CardDef.RoutableTriggers` 那 8 个）。所以这里也照那条路走。
            //
            // 为什么必须剥（2026-09-13 候选 F 顺手修）：不剥的话整句
            // `Rally: Deal 3 damage to an enemy` 会**从中间的动词开始被匹配**（那时 `ReDeal`
            // 还没锚定 `^`），报出来是「干净」—— 而那是**假干净**：它量的是**另一条路**。
            // 实测 `[unit]` 这一层因此虚高 73 张（344 → 271 才是真实值）。
            //
            // ⚠️ 只在**整段开头就是触发名**时剥。`Your Warlord gains "Penitence: …"` 那种**不算**
            //    （冒号前的 head 不等于触发名，归 `ReWarlordGains` 管）。
            // ⚠️ 战术卡里**一句都没有**这种前缀（实测 0 条），所以这一步不会影响战术卡的覆盖率。
            var mtr = Regex.Match(low, @"^([a-z]+)\s*:\s*(.+)$");
            if (mtr.Success && IsRoutableTrigger(mtr.Groups[1].Value))
            {
                var inner = Dispatch(mtr.Groups[2].Value.Trim(), s);
                // 前缀上挂的付费代价 / `equal to your Faith` 照旧记到正文的 op 上
                if (inner.Ops != null)
                {
                    if (paidCost > 0)
                        foreach (var o in inner.Ops) { o.Cost = paidCost; o.CostKind = paidKind; }
                    if (amountRef != null)
                        foreach (var o in inner.Ops) o.AmountRef = amountRef;
                }
                return inner;
            }

            // ---- 纯关键词声明（`Ephemeral` / `Flying` / `Blast 3`…）----
            // 卡的关键词由 `CardDef` 从 `keywords` 字段单独解析，这里再声明一次是冗余 —— 跳过，不算失败。
            // ⚠️ **必须确认前缀吃满整句**：`KeywordTable.Normalize` 是前缀匹配，
            //    `"Stun a random enemy"` 也会命中 `stun` —— 只看非 null 会把整句效果跳过去（静默失效）。
            if (IsKeywordOnly(s)) { r.Kind = SegKind.KeywordOnly; return r; }

            // ---- 🆕 付费**修饰型**激活（2026-09-13 A4 批 1）----
            //   `6 [Energy]: Extend effect until your next turn`（`Miraculous Feat`）·
            //   `8 [Energy]: Give it permanently`（`Daemonbreaker`）。
            //
            // 🔴 这一族**不是新效果**，是**改前面那句效果的时长**。出处：`rule_core.gd:1588`
            //    `_energy_act_prep` 的 `undo`/`replay` 两栏 ——
            //      · `extend effect` → **撤销基础效果**，再用 `this turn` → `until your next turn` 重结算
            //      · `permanently`   → **撤销基础效果**，再**去掉时长**重结算（永久版）
            //    （付费是**可选**的：不付就保持基础效果 —— `rule_core.gd:1799` 明写「false → 放弃（基础已结算）」）。
            //
            // ⇒ 判据：**只在有付费前缀时**才当修饰（`Give it permanently` 没有前缀时是别的卡的事）。
            //    产出**没有自己效果的标记 op**，正文那批由 `Parse` 回填进 `EffectOp.BaseOps`，
            //    结算层 `DoPaidMod` 回头改它们。**两处各管一半，同 `costwhen` 那条纪律。**
            // 全卡池实测**只有这 2 张**（`permanently` / `extend effect` 各一处），别为它放宽。
            if (paidCost > 0)
            {
                string modKind = null;
                if (low.Contains("extend effect")) modKind = "extend";
                else if (low.Contains("permanently")) modKind = "permanent";
                if (modKind != null)
                {
                    r.Ops = new List<EffectOp>
                    {
                        new EffectOp { Verb = "paidmod", Source = s, Payload = modKind,
                                       Cost = paidCost, CostKind = paidKind },
                    };
                    r.Kind = SegKind.Ok;
                    return r;
                }
            }

            r = Dispatch(low, s);
            // 付费代价记到**这一句产出的每条 op** 上（`12 [Energy]: Draw 3 cards`）
            if (paidCost > 0 && r.Ops != null)
                foreach (var op in r.Ops) { op.Cost = paidCost; op.CostKind = paidKind; }
            // `… equal to your Faith` 同理：记在每条 op 上，数值留给结算层算
            if (amountRef != null && r.Ops != null)
                foreach (var op in r.Ops) op.AmountRef = amountRef;
            return r;
        }

        /// <summary>handler 管线（付费前缀已由调用方剥掉、记在 op.Cost 上）</summary>
        static SegResult Dispatch(string low, string src)
        {
            var r = new SegResult { Ops = new List<EffectOp>() };
            EffectOp op;

            // ---- 0·0) 句首**同义写法归一**（2026-09-13 A4）----
            //   `put in play X` = `deploy X`。卡面实例：`If target dies, put in play one "Infractor"`
            //   （`Rapid Evisceration`，规则书附录 C `:310` 明列「违规者 Infractor」是那个生成器的产物）。
            //   ⚠️ **只归一「句首」那种**：`… to Vehicles **you put in play**` 是**目标短语**、
            //      由 `ParseTarget` 自己认（`spec.Deployed`），句子中间那种归一不到这儿，也不会被误伤。
            //   放在 `Dispatch` 里（不是 `ParseSegment`）是因为它必须对**递归进来的正文**同样生效
            //   （`If …, <正文>` 的正文就是递归进来的）。
            if (low.StartsWith("put in play ", System.StringComparison.Ordinal))
                low = "deploy " + low.Substring("put in play ".Length);

            // ---- 0·0b) `Each friendly unit that is Praying heals N` → 归一成「治那些祈祷的」（2026-09-13 A4 批 1）----
            // 出处：`Devout Serenity`（Sororitas）。卡面把**筛选条件写成了主语**，动词在**末尾** ——
            // 所有 handler 都以动词开头，直接送进去谁都不认。
            // 归一到「`heal N to all friendly units that are praying`」之后，
            // 目标由 `ParseTarget` 照常解（`friendly`→己方 / `units`→单位 / `all`→全部 /
            // `praying`→`EffectTargetSpec.PrayedOnly`），**不另写一套目标词表**。
            // ⚠️ 只认这一个形状（`heals` + 数字），别放宽 —— 放宽会吃到别的「Each …」句式。
            var mEph = Regex.Match(low, @"^each\s+friendly\s+unit\s+that\s+is\s+praying\s+heals?\s+(\d+)$");
            if (mEph.Success)
                low = $"heal {mEph.Groups[1].Value} to all friendly units that are praying";

            // ---- 0a) `for each …` 计数层（规则书 :233；原版 `_resolve_for_each:2524` + `_fe_count`）----
            // 三种写法语义不同，**分开处理**：
            //   前导 / 后置 → 这一条**重复** N 遍（`CountRef`）
            //   `and an additional … for each …` → 基础值**加上** N × 增量（既不是重复也不是忽略）
            // ⚠️ 必须在串味之前判 —— `Give +2 Attack to X for each Y` 里的 `for each` 从句
            //    紧跟目标短语，`ParseTarget` 会看到一串它不认识的词。
            // 拆出来的正文**回递归**进管线（那时从句已经不在串里了，不会再进来）。
            if (TryForEach(low, src, r, out low)) return r;

            // ---- 0) 条件句 `If <条件>, <效果>`（`:2635` / `:2652`）----
            // 原版把条件**判在每个 handler 内部**；我们统一在最前面剥壳，条件记进 op，
            // 正文回递归进管线 —— 这样每个 handler 都自动有条件支持，不用逐个改。
            if (TryIf(low, src, r)) return r;

            // ---- 0a) 常驻效果 / 手牌陷阱（**回合起止触发**）----
            //   · `For the rest of this battle, at the start of your turn, <正文>` —— 打出去**注册**，
            //     之后每个自己的回合开始结算一次（`Cadia Stands`）
            //   · `At the end of your turn, <正文>` —— **手牌陷阱卡自己的文本**（`Poisoned Supplies` /
            //     `Cult Propaganda`，`keywords` 带 `Sabotage`）：被塞进对手手牌后，
            //     在**持有者**的回合结束触发（`rule_core.gd:409-421` ①）
            // ⚠️ **必须排在前面**：这两族都以 `For the rest of` / `At the …` 开头，
            //    掉到后面会被 `for each` / 别的 handler 抢走或整句失配。
            if (TryPersistent(low, src, r)) return r;
            if (TryAtTurn(low, src, r)) return r;

            // ---- 🆕 0b2) `Codex: <效果>`（极限战士）= **条件「你的能量为 0 时」** ----
            // 出处：规则书 :175「典籍（Codex）：**你的能量为 0 时**触发效果」；
            //       `rule_core.gd:2397 _check_codex` 判的就是 `energy == 0`。
            // 借**条件层**实现（`EffectCondition`）而不是新开一种动词 ——
            // 这样 `Codex:` 后面的正文照常走管线，后面加什么动词它都自动支持。
            if (low.StartsWith("codex:"))
            {
                string body = low.Substring(6).Trim();
                if (body.Length == 0) { r.Kind = SegKind.Unknown; r.Ops = null; return r; }
                var inner = Dispatch(body, src);
                if (inner.Ops == null) { r.Kind = SegKind.Unknown; r.Ops = null; return r; }
                MarkCodexCondition(inner.Ops, src);       // 判据只此一处，见那个方法
                r.Ops.AddRange(inner.Ops);
                r.Kind = inner.Kind == SegKind.Ok ? SegKind.Ok : SegKind.Partial;
                return r;
            }

            // ---- 0·0c) 🆕 `Choose and gain a bonus (A, B or C)` → **归一成三选一** ----
            //   出处：`Carnifex`（Tyranid）`Rally: Choose and gain a bonus (+2 Melee, +2 Ranged or Armour 1)`，
            //   全池**只 1 处**（2026-09-14 A5 批 3 第 3 条）。
            //   🔴 卡面把三个选项写在**括号里**、用 `,` 与 ` or ` 分隔，而 `TryChooseOne` 只认
            //      `Choose one: A; B or C` 那种冒号写法 ⇒ 整句不认。
            //   ⇒ **归一**成它认得的形状（`choose one: gain A; gain B; gain C`）——
            //      三个选项各是一句 `gain <载荷>`，**不另写一套「选项表」文法**
            //      （三选一的判据继续只留在 `TryChooseOne` 一处）。
            //   ⚠️ 正则锚死 `^choose and gain a bonus (...)$`；放宽会吃到别的括号用法。
            var mBonus = ReChooseBonus.Match(low);
            if (mBonus.Success)
            {
                var parts = new List<string>();
                foreach (string piece in mBonus.Groups[1].Value.Split(','))
                {
                    string p = piece.Trim();
                    if (p.Length == 0) continue;
                    int or = p.IndexOf(" or ", System.StringComparison.Ordinal);
                    if (or >= 0)
                    {
                        string a = p.Substring(0, or).Trim();
                        string b = p.Substring(or + 4).Trim();
                        if (a.Length > 0) parts.Add(a);
                        if (b.Length > 0) parts.Add(b);
                    }
                    else parts.Add(p);
                }
                if (parts.Count >= 2)
                {
                    var sb = new System.Text.StringBuilder("choose one: ");
                    for (int i = 0; i < parts.Count; i++)
                    {
                        if (i > 0) sb.Append("; ");
                        sb.Append("gain ").Append(parts[i]);
                    }
                    low = sb.ToString();
                }
            }

            // ---- 0·0d) 🆕 `Stratagems in your hand become A or B`（2026-09-14 A5 批 4）----
            //   出处：`Hrolf the Ironhowl`（SpaceWolves），全池**只 1 处**。
            //   ⚠️ **必须排在 `TryGive` 之前**：不拦它的话，`become a hunting wolf or fenrisian wolf`
            //      会被某个 give 系 handler 从中间截走，产出一条**载荷是两个卡名的假 `gain`**
            //      ——判「半懂」、卡面打 `*`，而实际上它**什么都不会做**（典型的假解析）。
            var mBecome = ReBecome.Match(low);
            if (mBecome.Success)
            {
                var names = new List<string>();
                foreach (string piece in mBecome.Groups[1].Value.Split(new[] { " or " },
                                                                       System.StringSplitOptions.None))
                {
                    string p = piece.Trim().TrimEnd('.', ' ').Trim();
                    if (p.Length > 0) names.Add(p);
                }
                if (names.Count >= 2)
                {
                    r.Ops.Add(new EffectOp
                    {
                        Verb = "become", Source = src,
                        Payload = string.Join("|", names),
                        Amount = names.Count,
                    });
                    r.Kind = SegKind.Ok;
                    return r;
                }
            }

            // ---- 0c) 三选一 `Choose one: A; B or C` ----
            // ⚠️ **必须在 `if` 之后、其余 handler 之前**：原版 `_resolve_choose`（`:1161`）
            //    头一句就是 `if desc.contains("choose one") … return false` —— 把它挡在外面走特例路径。
            // ⚠️ 第二个实参传的是 `src` —— 在 `Dispatch` 里它就是**原始大小写**那句原文
            //    （调用方 `Dispatch(low, s)` 传进来的 `s`，全程没被改写过）。
            if (TryChooseOne(low, src, src, r)) return r;

            // ---- 0d) 选牌 `Choose a <筛选> and <动词>` ----
            // ⚠️ **紧跟 `Choose one` 之后**：两者都以 `choose` 开头，靠 `TryChooseOne` 先把它自己
            //    那族（`choose one:`）领走。原版 `_resolve_choose:1163` 的顺序也是这个。
            if (TryChooseCard(low, src, r)) return r;

            // ---- 0d-bis) 选**效果** `Choose an effect and give it to <目标>` / `… and chooses an effect` ----
            //   🆕 2026-09-14 T3。**紧跟在 `TryChooseCard` 之后**：两者都是 `choose` 开头，
            //   而 `TryChooseCard` 里已有一条「`what == "effect"` ⇒ 判不认识」把这一族让出来
            //   （那句注释写的就是「『选一个效果』不是选牌」）。
            //   ⚠️ 与 `choosecard` **不是一件事**：那个从**卡池/牌库/手牌**里筛**卡**，
            //      这个是从**登记好的固定几项**里挑 —— 候选项来源根本不同
            //      （见 `资料/选牌Choose_数据与设计.md` §六 与 `EffectResolver.ChooseEffectPools`）。
            if (TryChooseEffect(low, src, r)) return r;

            // ---- 0d-ter) `Trigger the <关键词> ability/abilities of <目标>`（2026-09-14 A4 批 4）----
            //   `Author of the Codex`（UM）· `Duty's End`（GS）· `Atalan Leader`（GS）·
            //   以及 `Regimental Doctrine` / `Deathwing Assault` / `Open Insurrection` /
            //   `Stomp Em` / `Codex Discipline` 的**尾句** `… and trigger their <X> abilities`。
            //   ⚠️ **必须排在 `TryGive` / `TryHeal` 之前**：尾句形态是那两句的 ` and ` 尾巴，
            //      排在后面不要紧（`Finish` 负责递归），但带目标的那种**整句以 `Trigger` 开头**，
            //      任何别的 handler 都不该有机会从中间截走它。
            //   ⚠️ 走 `Finish`（不是直接置 Ok）：尾句 `and choose a Codicil …` 要靠它递归解出来。
            op = TryTriggerAbility(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- 0e) `Each of your units deals damage equal to its <关键词> to <目标>`（2026-09-13 A4 批 1）----
            //   出处：`Sudden Assault`（SaimHann）「`Each of your units deals damage equal to its
            //   Shuriken to a random enemy`」· 规则书 `:207`「星镖 X：攻击时对目标额外造成 X 伤害」。
            //   ⚠️ **要排在 `TryDeal` 前面**：整句里也有 `deals … damage`，落到 `TryDeal` 会被它
            //      从中间截走（`each of your units` 当成目标），**数值 `equal to its Shuriken` 整段丢掉**。
            op = TryEachUnitDeal(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- 0f) 强制攻击族（`Make … attack by itself` / `Target … attacks …` / 裸 `Attack …`）----
            //   出处：`Murderous Desires` · `Peerless Bladesmen` · `Let Loose` 尾句 · `Damaged Hexmark`。
            //   ⚠️ **要排在 `TryDeal` / `TryTakeDamage` 前面** —— `attacks …` 开头的句子不能被它们
            //      从中间截走（`ReDeal` 是锚定 `^` 的，但 `TryTakeDamage` 那类的句型更松）。
            op = TryForceAttack(low, src);
            if (op != null) return Finish(r, op, src);   // 走 Finish：尾句形态要靠它继承攻击者

            // ---- 1) Deal N damage [to X]   (`:2672`) ----
            op = TryDeal(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- 1b) `<谁> take(s) N damage` —— **反语序**的伤害句 ----
            //      实测 3 条，其中 `Poisoned Supplies` 的正文就是它
            //      （`At the end of your turn, your troops take 1 damage`）。
            op = TryTakeDamage(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- 2) Stun   (`:2755`) ----
            // ⚠️ 走 `Finish`（不是直接置 Ok）：目标没解出来时要**报半懂**，别让
            //    `Stun <不认识的词>` 冒充「解析干净、打一个敌方单位」（2026-09-13 候选 F 修）。
            op = TryStun(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- 2b) Blind   (`:2786`) ----
            op = TryBlind(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- 3) Destroy   (`:2818`) ----
            op = TryDestroy(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- 4) Heal N   (`:2839`) ----
            op = TryHeal(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- 5) Draw N / Draw a <类型>   (`:2863` / 5a `:2867`) ----
            op = TryDraw(low, src);
            if (op != null) return Finish(r, op, src);   // 走 Finish —— `and give it X` 的尾巴靠它解

            // ---- 7b/7c) 降费：`Lower the cost of X by N` / `(it|they) cost(s) N less` ----
            //      （`:2920` / `:2927`）
            // ---- 🆕 7b-bis) **这张卡自己**加价：`costs N more [this turn]`（2026-09-14 A5 批 3 第 1 条）----
            //   全池实测**只 1 处**：`Makari the Grot`（Goff）的
            //   `Backlash: Returns to your hand and costs 2 more this turn`。
            //   另一处含 `costs N more` 的是 `Underground Network` 的
            //   `For the rest of this battle, Sabotage cards in the enemy hand cost 1 more` ——
            //   **有主语、以 `for the rest of this battle` 开头**，被 `TryPersistent` 先领走
            //   （`ReCostMore` 那条）；本正则锚了 `^cost`，两者不会互相截胡。
            //   ⚠️ **要排在 `TryLowerCost` 前面**：`costs N more` 里也有 `cost` 字样，
            //      `ReSubjectCostLess` 要求 `N less`，本来不会撞，但顺序写死更省心。
            op = TrySelfCostMore(low, src);
            if (op != null) return Finish(r, op, src);
            op = TryLowerCost(low, src);
            if (op != null) return Finish(r, op, src);   // `and give it X` 的尾巴靠 Finish 解

            // ---- 7d) `Lower its Health to 1`（2026-09-13 A4）----
            //   出处：`Eternal Servitude`（Sautekh）。原版是 `ResolveChangeMaxHealth` 那一支，
            //   卡面语义是「把**当前生命**降到 N」—— 不是「改生命上限」。
            op = TrySetHealth(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- 7e) `Double the Melee Attack and Health of a friendly troop`（2026-09-14 A4 批 3）----
            //   出处：`Possession`（Black Legion，8 费）。⚠️ 判据见 `ReDouble`（**组 1 卡死字面**）——
            //   全池另一句 `Double` 是 `Maulerfiend` 的 `Ecstasy 5: Double this troop's [Melee] and [Ranged]`，
            //   **翻的是近战+远程、没有生命**，而且属未实现的 `ecstasy` 那一族 ⇒ **不许被这条领走**。
            op = TryDouble(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- 7f) `The next time it uses Ferocity this turn, it stays in play`（2026-09-14 A4 批 3）----
            //   出处：`Bjorn's Shrine`（SpaceWolves，1 费）。**反编译里有完整体**
            //   （`CardScript__UsedActiveAbility.c:52-64` 的 `dontReturnFerocity` 豁免 + `:75-88` 用完即撤）
            //   —— 见 `UnitState.FerocityStay` 的注释。
            //   ⚠️ 判据卡住整句式，别把 `Bjorn the Fell-Handed` 的常驻版一起收进来。
            op = TryFerocityStay(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- 8) Refill energy   (`:2952`) ----
            op = TryRefill(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- 8b) `Does nothing` / `Reload the Duty abilities …`（2026-09-13 A4 加的两个小原子）----
            op = TryNoEffect(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }
            op = TryReloadDuty(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- 8c) `Lower cost by 1 when <事件>`（**事件触发式降费**，2026-09-13 A4）----
            //   🔴 这一族的**机制早就在**：`CardDef.CostWhens` 收下监听器（实测 4 张）、
            //      `EffectResolver.BroadcastCostWhen` 在事件发生时登记 `ctx.CostMods`。
            //      **卡住的是这句话本身** —— `ParseSegment` 不认它 ⇒ `IsFullyParsed` 为假
            //      ⇒ **卡打不出去**（`ErrUnimplemented`）、进不了卡组、卡面还打 `*`。
            //      ⇒ 这里给它一个**标记 op**：解析得出（于是上面三件事都对了），
            //        但结算时它**什么都不用做** —— 降费在牌还在手上时就登记好了。
            //      ⚠️ 结算时**打一行日志说明**（红线：不许静默），别让它看起来像"没实现"。
            op = TryCostWhenStub(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- 8d) `This costs N less if you control a unit with <关键词>`（**静态条件降费**，2026-09-13 A4 批 1）----
            //   和 8c 同一条纪律：这句话**没有可结算的东西**，只要「解析得出来」；
            //   真正的判据在 `RuleCore.CostOf` 读 `CardDef.CostIfControl`（见那个字段的注释）。
            //   ⚠️ 别把它并进 8c —— 那个是**事件**触发一次，这个是**常驻条件**（人走价回）。
            op = TryCostIfControl(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- 9) Deploy X   (`:2970`) ----
            // ⚠️ 走 `Finish` 而不是直接 `r.Ops.Add` —— `Deploy X and give it Flank` 的尾句
            //    要靠 `Finish` 递归解出来；不过 Finish 的话尾句**静默丢掉**（2026-09-12 撞到）。
            op = TryDeployEachPlayer(low, src);      // 对称部署（`Each player deploys …`）先试
            if (op != null) return Finish(r, op, src);
            op = TryDeploy(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- 9b) Create …（造牌进手牌/牌库）----
            //     原版是数据驱动的生成器（`cardEffect.spawn` 一族）；我们按卡面文本还原，
            //     候选池的算法在 `CreatePool`。规则书附录 B/C 是它的规格书。
            op = TryCreate(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- 9c) Return …（场上的卡挪回手牌 / 牌库）----
            //     规则书英文原版 `:455-457`：回牌库要**洗入**，除非卡面写明「顶」。
            op = TryReturn(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- Reanimate（打捞墓地，原版在 `_resolve_tactic` 族里）----
            op = TryReanimate(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- Repeat this effect   (`:2542` → `_resolve_repeat`) ----
            op = TryRepeat(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- 6) Give X to Y   (`:3010`，三种语序) ----
            op = TryGive(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- 6b) Lose X   (`:3082`) ----
            op = TryLose(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- 6c) Gain X   (`:3101`) ----
            op = TryGain(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- 9d) Spend all your Spirit Stones（灵族；2026-09-13 第三十三轮）----
            //     `Hosts of the Dead`：`Spend all your Spirit Stones. For each one, deploy a Wraithguard`
            //     —— 花掉的数量由 `ctx.LastSpentSpirit` 记，紧接着的 `For each one, …` 读它。
            op = TrySpendSpirit(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            r.Kind = SegKind.Unknown;
            r.Ops = null;
            return r;
        }

        /// <summary>
        /// `Spend all your Spirit Stones`（灵族）—— 2026-09-13 第三十三轮。
        ///
        /// 只认「全部花光」这一种：规则书 `:225` 说路标石/灵魂石「收集后用于抵扣或触发」，
        /// 而卡面实测只有 `Hosts of the Dead` 一条是**花光**（其余是 `Gain N` / `abilities requiring`）。
        /// 认不出的写法返回 null（**不猜**，由调用方判 Unknown）。
        /// </summary>
        static EffectOp TrySpendSpirit(string low, string src)
        {
            if (!Regex.IsMatch(low, @"^spend\s+all\s+your\s+spirit\s+stones?\b", RegexOptions.IgnoreCase))
                return null;
            return new EffectOp
            {
                Verb = "spendspirit",
                Source = src,
                Target = new EffectTargetSpec
                {
                    Raw = "(玩家灵魂石)", Side = "own", Kind = "player", Count = 1, Auto = true,
                },
            };
        }

        /// <summary>
        /// 收尾：把一条 op 记下来，**并把 `and` 尾句也解出来**。
        ///
        /// `Deal 3 damage to an enemy and stun it` —— 尾句必须解；解不出来就整句判
        /// <see cref="SegKind.Partial"/>（**半懂要报出来**，绝不能「打了伤害就装作没这回事」）。
        /// </summary>
        static SegResult Finish(SegResult r, EffectOp op, string src)
        {
            r.Ops.Add(op);
            r.Kind = NeedsTarget(op) && op.Target == null ? SegKind.Partial : SegKind.Ok;

            if (!string.IsNullOrEmpty(op.Tail))
            {
                var tail = ParseSegment(op.Tail);
                if (tail.Ops != null)
                {
                    // 🆕 **共用一个目标**（2026-09-13 A4）：`Heal 5 **and give Camouflage to a friendly unit**`
                    //    （`Evasive Manoeuvre`）—— 卡面只有**一个**目标（「对那个友方单位：治疗 5、再给它伪装」），
                    //    而目标短语只写在**后半句**上。本句没写目标、尾句写了 ⇒ 继承它。
                    //   ⚠️ **收得很紧**：只在「本句**需要**目标却没有」+「尾句**恰好一条** op 且
                    //     那条是 `give`/`gain`/`lose`、而且它真有目标」时继承 —— 松一点就会把
                    //     「两句各有各的目标」揉成一个（那正是本工程最怕的**静默**错打）。
                    //   🆕 2026-09-14 A5 批 2：**把 `gain` / `lose` 一并收进来** ——
                    //     三者本来就是**同一套载荷机制**（`EffectResolver` 里 `give` 与 `gain`
                    //     都指向 `DoGive(..., +1)`）。实测撞到的两句：
                    //       · `Slay: Heal 2 and gain Blood Thirst this turn`
                    //       · `Codex: Heal 5 and gain Vanguard until your next turn`（`Captain Sicarius`）
                    //     原来只有 `give` ⇒ 这两句的 `heal` **接不到目标** ⇒ 整句判「半懂」、
                    //     卡面打 `*`，而它们本来就该是「治自己 + 给自己加关键词」。
                    //     ⚠️ 判据没放宽：仍然要求**尾句恰好一条 op**、**它有目标**、**本句要目标却没有**。
                    if (op.Target == null && NeedsTarget(op) && tail.Ops.Count == 1
                        && (tail.Ops[0].Verb == "give" || tail.Ops[0].Verb == "gain"
                            || tail.Ops[0].Verb == "lose")
                        && tail.Ops[0].Target != null)
                    {
                        op.Target = tail.Ops[0].Target;
                        r.Kind = SegKind.Ok;
                    }

                    // 🆕 **反向共用目标**（2026-09-13 A4 批 2）：尾句**写了动词却没写目标**时，
                    //    主语沿用**本句的目标** —— 这是英语并列句的常态：
                    //      `Your Warlord gains Concussive until your next turn **and heals 5**`
                    //      （`Da Irongob` —— 治的是**那个督军**，不是 `Heal N` 的默认「己方督军」）；
                    //      `All friendly units gain Armour 2 until your next turn **and heal 2**`
                    //      （`Conviction of Faith`）· `Pray: Gain +1 Attack and +1 Ranged Attack
                    //      **and heal 1**`（`Battle Sister`）· `Ecstasy 2: Gain Blood Thirst **and Heal 1**`
                    //      （`Threnodic Choir Noise Marine`）· `Stimulation: Gain +1 [Attack], +1 [Ranged]
                    //      **and heal 1**`（`Glittering Myriad Infractor`）。
                    //      —— 全卡池这个形状实测 **5 句，5 句都该继承**（普查过，不是拍脑袋）。
                    //
                    //    ⚠️ **判据收得很紧**：只在「尾句**需要**目标（`NeedsTarget`）却**没有**」+
                    //       「本句**有**目标」时才接。尾句自己写了目标（`… and deal 2 damage to
                    //       adjacent units`）一律不动；`lowercost` / `draw` 那类**本来就不需要目标**
                    //       （`NeedsTarget` 为假）的也不动。
                    //    ⚠️ `forceattack` 单列：它的 `Target` 是**攻击者**，为空是**语法错误**
                    //       而不是「卡面没写」⇒ 不能靠 `NeedsTarget` 判（那个是给表现层用的，
                    //       裸形态本来就**不该**让玩家选攻击者）。
                    if (op.Target != null)
                        foreach (var t in tail.Ops)
                            if (t.Target == null && (NeedsTarget(t) || t.Verb == "forceattack"))
                                t.Target = op.Target;
                    r.Ops.AddRange(tail.Ops);

                    // 继承之后**重判**尾句还缺不缺目标 —— 缺了才算半懂。
                    // ⚠️ 只在「尾句是**半懂**、而且**解析出了 op**」时才升级（`Unknown` 的 `Ops` 是 null，
                    //    那种不能误判成 Ok）。2026-09-13 A4 批 2：不加这一步，上面那 5 句会因为
                    //    **尾句先被判了半懂**而整句降级 —— 明明继承到了目标。
                    if (tail.Kind == SegKind.Partial && tail.Ops != null && tail.Ops.Count > 0)
                    {
                        bool stillMissing = false;
                        foreach (var t in tail.Ops)
                            if (t.Target == null && (NeedsTarget(t) || t.Verb == "forceattack"))
                            { stillMissing = true; break; }
                        if (!stillMissing) { tail.Kind = SegKind.Ok; r.Kind = SegKind.Ok; }
                    }
                }
                // 尾句没认出来 / 只有半懂 → 整句降级成半懂
                if (tail.Kind != SegKind.Ok && tail.Kind != SegKind.KeywordOnly) r.Kind = SegKind.Partial;
            }
            return r;
        }

        /// <summary>
        /// 引号掩码：`mask[i] == true` 表示 `s[i]` 落在**一段引号里**。
        /// **没有引号就返回 null**（调用方按「一个都不在引号里」处理，省掉一次分配）。
        ///
        /// **为什么要有它**（2026-09-14 修两条**静默错打**）：`give` 的载荷里嵌的那整段
        /// 效果文字是**用引号包起来的一整段**，它里面的 ` and ` / ` to ` **都不是这句的语法成分**：
        ///   · `Give "Slay: Heal 2 **and** gain a Dark Pact of Excess" to a friendly troop`
        ///     —— `SplitAndTail` 在 `and gain` 处切尾句 ⇒ 载荷只剩 `"slay: heal 2`，
        ///        尾句目标退化成「己方全体」（实测 `Pledge to the Dark Prince`）。
        ///   · `Give "💀 Backlash: Return **to** your hand" to a friendly troop`
        ///     —— `ReGive` 那条非贪婪的 ` to ` 挑**最左**一个 ⇒ 载荷 = `"💀 backlash: return`、
        ///        目标 = `your hand" to a friendly troop`（实测 `Graceful Avoidance`）。
        /// 两条都是**卡面不打 `*`、结算打错人**（本工程最忌讳的形态），探针量出来的。
        ///
        /// ⚠️ **单引号只有当左边不是字母数字时才算开引号** —— 卡面里有 `troop's` 这种撇号
        ///    （`Strike: Trigger this troop's Codex ability`），不加这条判据会把半个卡面吞进引号。
        /// </summary>
        static bool[] QuoteMask(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            if (s.IndexOf('"') < 0 && s.IndexOf('\'') < 0) return null;

            var m = new bool[s.Length];
            bool inD = false, inS = false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (inD) { m[i] = true; if (c == '"') inD = false; continue; }
                if (inS) { m[i] = true; if (c == '\'') inS = false; continue; }
                if (c == '"') { inD = true; m[i] = true; continue; }
                if (c == '\'')
                {
                    bool prevAlnum = i > 0 && char.IsLetterOrDigit(s[i - 1]);
                    if (!prevAlnum) { inS = true; m[i] = true; }     // 撇号（`troop's`）不算开引号
                }
            }
            return m;
        }

        /// <summary>`s` 里从 <paramref name="from"/> 起、**第一个引号外**的 `" to "` 的下标
        /// （返回那个空格的下标）。没有返回 -1。判据只此一处，见 <see cref="QuoteMask"/>。</summary>
        static int TopLevelToAt(string s, bool[] mask, int from)
        {
            if (string.IsNullOrEmpty(s)) return -1;
            for (int i = (from < 0 ? 0 : from); i + 4 <= s.Length; i++)
            {
                if (mask != null && mask[i]) continue;
                if (s[i] == ' ' && s[i + 1] == 't' && s[i + 2] == 'o' && s[i + 3] == ' '
                    && (mask == null || !mask[i + 1]))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// `X and <另一句>` 的切分。**判据是「and 后面那截像不像一个新效果」**：
        ///   · `an enemy **and** stun it`            → 后面是动词 `stun` → 切，尾句递归
        ///   · `an enemy **and** its adjacent units` → 后面是目标词的延续 → **不切**，整段当目标
        ///
        /// ⚠️ 这里**故意和 `rule_core.gd:2685` 不一样**：原版无条件在第一个 ` and ` 切，
        ///    于是 `…and its adjacent units` 的「相邻」会被切掉、当成一句解不出来的尾句丢掉。
        ///    我们按「后面是不是动词」判 —— 是**超集**（原版能解的我们都能解，且不丢相邻）。
        ///
        /// 🆕 **2026-09-14：引号里的 ` and ` 不算** —— 见 <see cref="QuoteMask"/> 那两条静默错打。
        /// </summary>
        static void SplitAndTail(string tok, out string targetPart, out string tail)
        {
            tail = "";
            targetPart = tok;
            if (string.IsNullOrEmpty(tok)) return;

            // 🔴 **2026-09-14 A4 批 4：往后扫到第一个「动词开头」的 ` and `，不是只看第一个 ` and `。**
            //
            // 为什么改：载荷里**自己就带 ` and `** 的时候，第一个 ` and ` 后面跟着的是数值不是动词
            // ⇒ 原来的写法切不开 ⇒ **后半句被当成目标短语的一部分吞掉**。
            // 实测（逐句探针，全池只有 **4 句**受这次改动影响，4 句全是**修好**）：
            //   · `Give +2 Attack, +2 Armor **and** +2 Health to your troops **and trigger their
            //     Teleport abilities**`（`Deathwing Assault`）—— 原来尾句被吞进目标
            //     （`目标[your troops and trigger their teleport abilities]`）。
            //   · `Pray: Gain +1 Attack **and** +1 Ranged Attack **and heal 1**`（`Battle Sister`）
            //     —— 原来整个塞进载荷（`+1 attack and +1 ranged attack and heal 1`），**heal 静默不发生**。
            //   · `4 Energy: Give +2 Attack **and** +2 Health instead **and Heal them 1`（`Righteous Repugnance`）
            //     —— 同上。
            //   · `Agenda: Trigger the Teleport and Slay effects of a friendly unit **and gain 1
            //     Quest Point**`（`Master Lazarus`）—— 同上。
            //   ⚠️ **第一个 ` and ` 后面就是动词时，行为一字不变** —— 只有那 4 句走到新逻辑。
            var qm = QuoteMask(tok);
            for (int idx = tok.IndexOf(" and "); idx >= 0; idx = tok.IndexOf(" and ", idx + 1))
            {
                if (qm != null && qm[idx]) continue;      // 引号里的 ` and ` 是嵌入正文的一部分
                string t = tok.Substring(idx + 5).Trim();
                if (t.Length == 0) return;
                if (!IsVerbWord(FirstWord(t))) continue;
                targetPart = tok.Substring(0, idx).Trim();
                tail = t;
                return;
            }
        }

        /// <summary>剥掉句首的**图标字形**（`⚡` / `💀` / `⭐` …）—— 卡面把触发关键词印成图标，
        /// OCR 出来就是那个符号，挡在前面会让 `Rally:` 这类前缀匹配看不到触发名。
        ///
        /// ⚠️ **判据收得很紧**：只剥 <see cref="System.Globalization.UnicodeCategory.OtherSymbol"/>
        ///    与代理对（`💀` U+1F480 这类是代理对，`char.IsSymbol` 对单半个返回 false）。
        ///    `+` / `-` 是 `MathSymbol`，**不剥** —— 有句子真的以 `+` 开头（`+1 Attack to …`）。
        ///
        /// ⚠️ **两个调用点，判据只此一份**：
        ///   ① `ParseSegment`（句首那一次）；
        ///   ② 🆕 **`CardDef.AddTriggerOp`** —— 2026-09-14 补：那边原来**没剥**，
        ///      于是 `⚡ Rally: …` 的 `head` 成了 `⚡ rally`、和 `Rally` 对不上
        ///      ⇒ **整张卡的触发正文一条都收不到**（`Hrolf the Ironhowl` 的 `Rally` 从来没触发过，
        ///      而卡面明印着 —— 本工程的静默失败红线）。全池**只有这 1 张**是这个形状。</summary>
        public static string StripLeadingIcons(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            int i = 0;
            while (i < s.Length)
            {
                if (char.IsSurrogate(s[i])) { i++; continue; }
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(s, i);
                if (cat != System.Globalization.UnicodeCategory.OtherSymbol
                    && cat != System.Globalization.UnicodeCategory.OtherNotAssigned) break;
                i++;
            }
            return i > 0 ? s.Substring(i).TrimStart() : s;
        }

        /// <summary>
        /// 剥掉句首的**语气词**（`Also` / `Then`），**不是句型的一部分**。
        ///
        /// 两种写法都要剥：
        ///   · `Also destroy all damaged enemy troops`（`Oath 4:` 后面那半句）
        ///   · `1 Also give it Shield`（`Forewarned` 卡面印的是 `❶ Also give it Shield`）
        ///      —— 数字后面那个不能一起剥掉，它是**付费前缀的 N**，剥了 `RePaidBare` 就认不出。
        ///
        /// 🔴 **要剥两次**（2026-09-13 A4 批 1）：一次在整段开头，一次在**付费前缀剥掉之后**。
        ///    原来只剥一次 ⇒ `Oath 4: Also destroy …` 剩下的 `Also …` 没人管，整句不认
        ///    （`Oath of the Throne` 那张卡因此一直打不出去）。判据就这么两条，**别放松**。
        /// </summary>
        static string StripVocative(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = Regex.Replace(s, @"^(?:also|then)\s*,?\s+", "", RegexOptions.IgnoreCase).Trim();
            s = Regex.Replace(s, @"^(?<num>\(?\d+\)?)\s+(?:also|then)\s*,?\s+", "${num} ",
                              RegexOptions.IgnoreCase).Trim();
            return s;
        }

        static string FirstWord(string s)
        {
            int i = s.IndexOf(' ');
            return (i < 0 ? s : s.Substring(0, i)).Trim().TrimEnd(',', ':');
        }

        /// <summary>
        /// **前缀自己带语义、不许在这里被剥掉**的触发名。
        ///
        /// 🔴 `codex` 是**唯一一个**，为什么：`Codex: &lt;正文&gt;` 的语义**就在前缀上** ——
        ///    「你的能量为 0 时触发效果」（规则书 `:175`）那条条件，是 `Dispatch` 里那个
        ///    `codex:` 分支挂到正文**每个 op** 上的。前缀一旦在这儿被剥掉，**那个分支永远进不去**，
        ///    条件随之丢失 ⇒ **Codex 不等能量归零就触发**。
        ///    2026-09-14 A4 批 4 实测撞到：把 `codex` 加进 `RoutableTriggers` 之后自检两条红 ——
        ///    `能量不为 0 → Codex **不**触发` 与 `卡池里真的有 Codex: 的战术卡（0 条）`。
        ///
        /// ⚠️ 它**必须**留在 `RoutableTriggers` 里（否则 `CardDef.AddTriggerOp` 收不到正文，
        ///    `TriggerOps("codex")` 恒为 null ⇒ 强行触发那条路是空的）。
        /// ⇒ **两个名单的用途不同**，是**子集**关系不是同一张表：
        ///    `RoutableTriggers` 管「引擎会不会在某个时机调这个关键词的正文」；
        ///    本集合管「整段**开头**的这个前缀要不要剥掉」。
        /// </summary>
        static readonly string[] PrefixKeepsMeaning = { KeywordTable.Codex };

        /// <summary>这个冒号前的词是不是**该在 `ParseSegment` 里剥掉的触发前缀**（`Rally` / `Strike` / …）。
        /// 判据只有一处：<see cref="CardDef.RoutableTriggers"/> **减去**
        /// <see cref="PrefixKeepsMeaning"/>（见那一份的注释）。</summary>
        static bool IsRoutableTrigger(string head)
        {
            foreach (string t in CardDef.RoutableTriggers)
            {
                if (!string.Equals(t, head, System.StringComparison.OrdinalIgnoreCase)) continue;
                foreach (string keep in PrefixKeepsMeaning)
                    if (string.Equals(keep, t, System.StringComparison.OrdinalIgnoreCase)) return false;
                return true;
            }
            return false;
        }

        /// <summary>`Oath N: …` —— 付 N 能量才生效（组 1 = N，组 2 = 正文）</summary>
        static readonly Regex ReOathPaid = new Regex(@"^oath\s+(\d+)\s*:\s*(.+)$", RegexOptions.Compiled);

        /// <summary>剥掉圈码次数标记（`①`–`⑳`）—— 它们不是句型的一部分</summary>
        static string StripCircled(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s)
                if (c >= '①' && c <= '⑳') continue;   // ①..⑳
                else sb.Append(c);
            return sb.ToString();
        }

        /// <summary>句首是不是一个**效果动词**（决定 `and` 后面算新效果还是目标的延续）</summary>
        static bool IsVerbWord(string w)
        {
            switch ((w ?? "").ToLowerInvariant())
            {
                case "deal": case "deals": case "stun": case "stuns":
                case "destroy": case "destroys": case "heal": case "heals":
                case "draw": case "draws": case "give": case "gives":
                case "gain": case "gains": case "lose": case "loses":
                case "deploy": case "deploys": case "return": case "returns":
                case "refill": case "create": case "creates": case "reanimate":
                // ⚠️ 2026-09-13 补 `lower` / `raise`：**全仓只有 5 处分句含 ` and lower`，
                //    5 处全都该切**（实测过，不是拍脑袋）：
                //      `Draw 3 cards and lower their cost by 3` / `Draw it and lower its cost by 3` /
                //      `Draw a Stratagem and lower its cost by 2` /
                //      `Return up to 5 friendly troops … to your deck and lower their cost by 3` /
                //      `Repeat for each Vehicle in your hand and lower their cost by 1`
                //    之前没这两个词 → `SplitAndTail` 不切 → `ReDraw` 锚了 `$` 整条失配
                //    → 这 5 张一直躺在「完全不认识的句子」里。
                case "lower": case "lowers": case "raise": case "raises":
                // ⚠️ 2026-09-13 A4 补 `reduce`：**和 `lower` 是同一个动词的两种写法**
                //    （卡面 `reduce its cost to 1` / `reduce its cost by 3` / `Reduce their cost by 5`）。
                //    不加这个词，`Return a friendly troop to your hand **and reduce** its cost to 1`
                //    就切不开 —— 那两句会一直躺在「完全不认识」里（实测 `Lying in Wait` 整张卡打不出去）。
                case "reduce": case "reduces":
                // ⚠️ 2026-09-14 A5 批 3 第 1 条补 `cost` / `costs`：**全卡池含 ` and cost(s)`
                //    的分句实测只有 1 处**（`Makari the Grot` 的
                //    `Backlash: Returns to your hand **and costs 2 more this turn**`），
                //    而且这一处就该切 —— 不切的话 `ReReturnBare` 的目的地读到
                //    `your hand and costs 2 more this turn`、全等匹配对不上 ⇒ 整句不认。
                //    （普查脚本与判据：这一条和 `lower` / `reduce` 那两条是同一套做法。）
                case "cost": case "costs":
                // ⚠️ 2026-09-13 A4 批 2 补 `attack` / `attacks`：全卡池含 ` and attack(s)` 的分句
                //    **只有 2 句，两句都该切**（实测过）：
                //      `Each friendly Beast gets +1 [fist] this turn **and attacks** a random enemy`（`Let Loose`）
                //      `Teleport: Gain Vanguard **and attack** by itself`（`Deathwing Knight`）
                //    不加这个词 → `SplitAndTail` 不切 → `and attacks …` **被当成载荷的一部分**，
                //    而整句仍判「认了」= 卡面不打 `*` 的**假干净**（那半句永远不发生）。
                case "attack": case "attacks":
                // ⚠️ 2026-09-14 A4 批 4 补 `trigger` / `choose`（两条都是**实测量过**的）：
                //    · `trigger` —— 全池含 ` and trigger ` 的分句 **6 句，6 句都该切**：
                //      `Heal 2 to all your units and trigger their Regiment abilities`
                //      （`Regimental Doctrine`）· `Give +2 Attack, +2 Armor and +2 Health to your troops
                //      and trigger their Teleport abilities`（`Deathwing Assault`）·
                //      `Give Invulnerable to your troops this turn and trigger their Uprising abilities`
                //      （`Open Insurrection`）· `Give +1 [attack] to your units this turn and trigger
                //      their [Mob] Mob abilities`（`Stomp Em`）· `Give +2 Ranged Attack to your troops,
                //      and trigger their Codex abilities`（`Codex Discipline`）。
                //      🔴 **不加这个词 = 那半句被吞进目标短语里**，实测（逐句探针）就是
                //      `heal 目标[own/unit 「all your units and trigger their regiment abilities」]`
                //      —— 整句**判「认了」、卡面不打 `*`，而那半句永远不发生**（红线里的静默失效）。
                //    · `choose` —— 全池含 ` and choose ` 的分句 **只有 1 句**（`Author of the Codex`），
                //      它必须切开才能把 `choose a Codicil …` 交给 `TryChooseCard`。
                case "trigger": case "triggers": case "choose": case "chooses":
                    return true;
                default: return false;
            }
        }

        // ==================================================================
        //  各个 handler（正则出处写在每条的注释里）
        // ==================================================================

        /// <summary>
        /// 把 `for each …` 从句从一句里剥下来，记进产出的 op。<see cref="EffectOp.CountRef"/> 有完整说明。
        ///
        /// 判据按三种实测写法分开（448 张战术卡里 `for each/every` 共 38 个分句）：
        ///   · **前导** `For each X, <效果>` —— 从句在句首，逗号后是正文
        ///   · **后置** `<效果> … for each X` —— 从句在句尾（它前面是目标短语）
        ///   · **追加** `<效果> …, and an additional <数值> for each X` —— **加法**，见下
        ///
        /// ⚠️ 第三种**不能当重复**：`Daemonic Frenzy` 是「+2 近战，每有一份契约**再** +2」，
        ///    而 `Give +2 Attack to X, and +2 Attack for each Y` 这种写法拆出来是**同一段目标**，
        ///    重复结算会把增量当成整体再加一遍（+2 变成 +6）。所以按「基础 + N × 增量」算期望值，
        ///    在解析阶段就折进 `Amount`，`CountRef` 留空。
        /// </summary>
        static bool TryForEach(string low, string src, SegResult r, out string rest)
        {
            rest = low;

            // ---- 追加型：`<基础>, and an additional <增量> for each <计数>` ----
            // 基础 + 计数 × 增量 —— 解析阶段折好，结算层当普通一条办。
            var add = ReAdditionalForEach.Match(low);
            if (add.Success)
            {
                string countRef = add.Groups[3].Value.Trim();
                string scope, reference;
                if (!ClassifyCount(countRef, out scope, out reference)) return false;

                string basePart = low.Substring(0, add.Index).Trim().TrimEnd(',');
                var inner = Dispatch(basePart, src);
                if (inner.Ops == null || inner.Ops.Count == 0) return false;

                int inc = int.Parse(add.Groups[1].Value);
                string unitWord = add.Groups[2].Value.Trim();
                foreach (var o in inner.Ops)
                {
                    // 与基础同类型的载荷才加（`+2 Melee Attack` 的增量也写 `Melee Attack`）
                    if (o.Verb != "give" && o.Verb != "gain" && o.Verb != "lose") continue;
                    if (!PayloadMatchesUnit(o.Payload, unitWord)) continue;
                    o.PerCount = inc;
                    o.CountRef = reference;
                    o.CountScope = scope;
                    o.Source = src;
                    // 基础值同样要从载荷里抠（`+2 melee attack` 的 2；`Amount` 对 give 是 0）
                    o.Amount = BaseAmountOf(o);
                }
                r.Ops.AddRange(inner.Ops);
                r.Kind = SegKind.Ok;
                rest = "";
                return true;
            }

            // ---- 前导型：`For each <计数>, <正文>` ----
            if (low.StartsWith("for each ") || low.StartsWith("for every "))
            {
                int comma = low.IndexOf(',');
                if (comma > 0)
                {
                    string countRef = low.Substring("for each ".Length, comma - "for each ".Length).Trim();
                    string body = low.Substring(comma + 1).Trim();
                    string scope, reference;
                    if (ClassifyCount(countRef, out scope, out reference) && body.Length > 0)
                    {
                        var inner = Dispatch(body, src);
                        if (inner.Ops == null || inner.Ops.Count == 0) return false;
                        foreach (var o in inner.Ops)
                        {
                            o.CountRef = reference;
                            o.CountScope = scope;
                            o.Source = src;
                            o.Amount = BaseAmountOf(o);
                        }
                        r.Ops.AddRange(inner.Ops);
                        r.Kind = SegKind.Ok;
                        rest = "";
                        return true;
                    }
                }
            }

            // ---- 后置型：`<正文> … for each <计数>` ----
            // 从句一定在句尾，而且前面隔着一个空格 —— 用 LastIndexOf 才不会被
            // `… for each friendly unit in play` 里更靠前的 `for ` 骗到。
            int at = low.LastIndexOf(" for each ");
            int at2 = low.LastIndexOf(" for every ");
            if (at2 > at) at = at2;
            if (at > 0)
            {
                string countRef = low.Substring(at + 10).Trim();
                string body = low.Substring(0, at).Trim();

                // ⚠️ **`for each` 从句后面还可能挂着 `, and <另一句>`** ——
                //    实测 `Deploy one Kroot Hound for each damaged enemy, and give them Flank`。
                //    不先切出来的话，`and give them flank` 会被当成计数对象的一部分
                //    （`ClassifyCount` 照样能返回 true，于是**尾句被静默吞掉** —— 部署了但没给侧翼）。
                //    这里用和 `SplitAndTail` 一样的判据：`and` 后面那截是不是动词开头。
                string tail = null;
                int ta = countRef.IndexOf(", and ", System.StringComparison.Ordinal);
                int skip = 6;
                if (ta < 0) { ta = countRef.IndexOf(" and ", System.StringComparison.Ordinal); skip = 5; }
                if (ta > 0 && IsVerbWord(FirstWord(countRef.Substring(ta + skip))))
                {
                    tail = countRef.Substring(ta + skip).Trim();
                    countRef = countRef.Substring(0, ta).Trim();
                }

                string scope, reference;
                if (ClassifyCount(countRef, out scope, out reference) && body.Length > 0)
                {
                    var inner = Dispatch(body, src);
                    if (inner.Ops == null || inner.Ops.Count == 0) return false;
                    foreach (var o in inner.Ops)
                    {
                        o.CountRef = reference;
                        o.CountScope = scope;
                        o.Source = src;
                        o.Amount = BaseAmountOf(o);
                    }
                    r.Ops.AddRange(inner.Ops);
                    r.Kind = SegKind.Ok;

                    // 尾句**只结算一次**（`them` 指受 for-each 影响的那批，不是每份再来一遍）
                    if (tail != null)
                    {
                        var sub = ParseSegment(tail);
                        if (sub.Ops != null) r.Ops.AddRange(sub.Ops);
                        if (sub.Kind != SegKind.Ok && sub.Kind != SegKind.KeywordOnly) r.Kind = SegKind.Partial;
                    }
                    rest = "";
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// `give` / `gain` / `lose` 的**基础值不在 `Amount` 里，在载荷原文里**
        /// （`Give +2 Melee Attack …` → 载荷 `+2 melee attack`，`Amount` 是 0）。
        /// `for each` 的增量要把基础值算进去，所以这里把它抠出来补进 `Amount`。
        /// **只对 give/gain/lose 做** —— 别的动词的数值本来就在 `Amount` 里（`deal 3 damage`）。
        /// </summary>
        static int BaseAmountOf(EffectOp o)
        {
            if (o == null) return 0;
            if (o.Verb != "give" && o.Verb != "gain" && o.Verb != "lose") return o.Amount;
            if (string.IsNullOrEmpty(o.Payload)) return o.Amount;
            var m = Regex.Match(o.Payload, @"[+-]?\d+");
            return m.Success ? int.Parse(m.Value) : o.Amount;
        }

        /// <summary>`and an additional +2 Melee Attack for each Dark Pact on it`</summary>
        static readonly Regex ReAdditionalForEach = new Regex(
            @",?\s*and an additional\s+\+?(\d+)\s+(.+?)\s+for (?:each|every)\s+(.+)$",
            RegexOptions.Compiled);

        /// <summary>
        /// 增量那一条的载荷词和基础的对不对得上 —— `+2 Melee Attack` 的增量也写 `Melee Attack`。
        /// 判据故意宽松：两边都含同一个属性词就算（`armour 1` / `Armour 1` 是同一件事）。
        /// </summary>
        static bool PayloadMatchesUnit(string payload, string unitWord)
        {
            if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(unitWord)) return false;
            string p = payload.ToLowerInvariant(), u = unitWord.ToLowerInvariant();
            foreach (string w in new[] { "melee", "ranged", "attack", "health", "armour", "armor" })
                if (p.Contains(w) && u.Contains(w)) return true;
            return false;
        }

        /// <summary>
        /// `for each` 从句 →（计数范围, 计数对象）。**认不出来返回 false** ——
        /// 宁可让整句判半懂，也不能猜一个计数对象（猜错就是「打多/打少」）。
        ///
        /// 语义出处：原版 `rule_core.gd:1347` 的 `_fe_count`（分支顺序照抄）。
        /// </summary>
        static bool ClassifyCount(string countRef, out string scope, out string reference)
        {
            scope = null; reference = null;
            if (string.IsNullOrEmpty(countRef)) return false;
            string c = countRef.Trim().ToLowerInvariant()
                .Replace(" in play", "").Replace(" in your hand", "").Replace(" this game", "").Trim();

            // ⓪ **`For each one`** —— 指**前一句刚花掉的灵魂石颗数**（2026-09-13 第三十三轮）。
            //    正主只有一张：`Hosts of the Dead` = `Spend all your Spirit Stones.
            //    **For each one, deploy a Wraithguard**`。数量由 `ctx.LastSpentSpirit` 提供。
            //    ⚠️ 这是个**窄判据**：`one` 单独出现时没有别的含义，而 `LastSpentSpirit` 只在
            //    「花掉全部灵魂石」之后非 0 ⇒ 不会误伤（认错的话那张卡会重复错次数，测试里有断言）。
            if (c == "one") { scope = "spiritspent"; reference = "one"; return true; }

            // ① 黑暗契约的**层数**（不是单位数）——
            //    `for each Dark Pact on it` / `for each Dark Pact on friendly units`。
            //    出处：原版 `_fe_count:1349` 的 `dark pact` 那一支（它是第一个判的分支）。
            //    ⚠️ 契约**可叠加**（`kws.darkpact` 是个计数），所以不能当成「有没有契约」的布尔。
            if (c.Contains("dark pact"))
            {
                scope = "darkpact";
                reference = c.Contains("on it") ? "it" : "board";
                return true;
            }
            // ② 本次结算抽到的牌：`for each troop drawn` / `for every infantry drawn`
            var md = Regex.Match(c, @"^(?:troop|troops|unit|units|vehicle|infantry|card|counter)?\s*drawn$");
            if (md.Success)
            {
                scope = "draw";
                reference = md.Groups[1].Success ? md.Groups[1].Value : "any";
                return true;
            }
            // ② 本回合阵亡：`for each one that dies` / `for each one destroyed`
            if (c.Contains("that dies") || c.Contains("destroyed") || c.Contains("that died"))
            {
                scope = "died"; reference = "unit"; return true;
            }
            // ③ 本局打出过的：`for each Secret you played this game`
            if (c.Contains("played this game") || c.Contains("you played"))
            {
                scope = "played";
                reference = c.Contains("secret") ? "secret" : (c.Contains("sabotage") ? "sabotage" : "any");
                return true;
            }
            // ④ 盘面：`for each enemy in play` / `for each friendly unit` / `for each damaged enemy`
            bool own = c.Contains("friendly") || c.Contains("friendly unit") || c.Contains("your");
            bool foe = c.Contains("enemy");
            if (!own && !foe)
            {
                // 没写谁 —— 原版 `_fe_count` 默认**本方**（`:1347` 起那段 `board_p := p`）
                own = true;
            }
            bool damaged = c.Contains("damaged");
            string kind = "any";
            // ⚠️ **`unit` 要单列成 `"unit"`，不能落到 `"any"`、更不能归成 `"troop"`**：
            //    `any` 与 `unit` 在本仓的 `CountFor` 里**行为相同**（都只对 `troop` 排督军），
            //    但 `CountRef` 会进日志与断言 —— 写清楚才查得动。
            foreach (string w in new[] { "troop", "vehicle", "infantry", "daemon", "beast", "drone", "unit" })
                if (c.Contains(w)) { kind = w; break; }
            // 🔴 **`unit` 不许归成 `troop`**（2026-09-14 用户指正后照三层权威核过）——
            //    这两句原来写着「`friendly unit` 语境就是部队，原版 `_fe_count` 也把它当 troop 基数」，
            //    **是错的**。三层证据：
            //      · 规则书中文版 `:70-75`：**单位含督军，部队不含**；作用于「单位」的效果**可以**影响督军；
            //      · 参考实现 `d:/warpforge/scripts/rule_core.gd` 的 `_fe_count` 里写得明明白白：
            //        `var troop_only := s.contains("troop")` —— **只有 `troop` 这个词才排督军**；
            //      · 我们自己的消费端也是这么判的（`EffectResolver.CountFor`：`kind == "troop" && u.IsWarlord`）。
            //    ⇒ 解析层把它改成 `troop` 就等于**两处判据不一致**，而这里错的那一处**不会报错**，
            //      只会让「每个己方单位」少数一个督军（静默少数）。
            //    ⚠️ 没写名词时（`for each damaged enemy`）仍然 `any` —— 那时该数督军。

            scope = "board";
            reference = (foe ? "enemy" : "own") + "|" + kind + "|" + (damaged ? "damaged" : "all");
            return true;
        }

        /// <summary>
        /// 条件句 `If &lt;条件&gt;, &lt;效果&gt;`。把条件剥出来记进每条 op，正文递归回管线。
        ///
        /// 处理两件小事：
        ///   · 结尾的 `instead`（`If the target has Armour, deal 8 damage instead`）是条件语义的一部分，
        ///     解析正文时剥掉（`instead` 意味着**替换**默认效果，不是叠加）；
        ///   · 条件判不了时 <see cref="EffectCondition.Normalize"/> 返回 null —— 如实留给上层报，
        ///     **不许当成条件成立**。
        /// </summary>
        static bool TryIf(string low, string src, SegResult r)
        {
            if (!low.StartsWith("if ")) return false;
            int comma = low.IndexOf(',');
            if (comma < 4) return false;
            string cond = low.Substring(3, comma - 3).Trim();
            string rest = low.Substring(comma + 1).Trim();
            if (cond.Length == 0 || rest.Length == 0) return false;

            bool instead = rest.EndsWith(" instead");
            if (instead) rest = rest.Substring(0, rest.Length - " instead".Length).Trim();

            var inner = Dispatch(rest, src);
            if (inner.Ops == null) return false;

            string kind = EffectCondition.Normalize(cond);
            foreach (var op in inner.Ops)
            {
                op.Condition = cond;
                op.ConditionKind = kind ?? "";
                op.Instead = instead;
                op.Source = src;                    // 日志要看到整句（含条件），不是剥完的半句
            }
            r.Ops.AddRange(inner.Ops);
            r.Kind = inner.Kind == SegKind.Ok ? SegKind.Ok : SegKind.Partial;
            return true;
        }

        /// <summary>
        /// `For the rest of this battle|the match, at the start|end of your turn, &lt;正文&gt;`
        /// —— **注册一条常驻效果**。
        ///
        /// **语义出处（权威）**：规则书英文版 `:39-41`「Persistent Effects」（中文版 `:32`）——
        ///   &gt; Some cards have persistent effects indicated by "For the rest of this battle"
        ///   &gt; that remain in effect after the card is discarded. Place these cards in a
        ///   &gt; separate pile next to the discard pile for reference.
        /// ⇒ **卡本身是常驻效果的来源**，所以 `BattleContext.PersistentEffects` 存的是
        ///   「谁 + 来源卡 + 正文」，而不是把效果烘进某个全局数值。
        ///
        /// 实测「回合起止」型只有 **1 条**：AstraMilitarum `Cadia Stands`
        /// （`For the rest of this battle, at the start of your turn, deploy a Shock Trooper`）。
        /// 同族的另外 3 条是**别的触发点**，**不在这个 handler 里**、现在仍然判不认识：
        ///   `… give Shield to all Drones you deploy`（部署时给）·
        ///   `… give Armour 1 to Vehicles you put in play`（部署时给）·
        ///   `… Sabotage cards in the enemy hand cost 1 more`（持续改费）
        /// —— **宁可报「不认识」，也不注册一条不会生效的**。
        /// </summary>
        static bool TryPersistent(string low, string src, SegResult r)
        {
            var m = RePersistAtTurn.Match(low);
            if (m.Success)
            {
                string body = m.Groups[2].Value.Trim();
                if (body.Length == 0) return false;
                var inner = Dispatch(body, src);
                if (inner.Ops == null) return false;          // 正文认不出 → 整句判不认识，别注册半个

                r.Ops.Add(new EffectOp
                {
                    Verb = "persist",
                    Source = src,
                    AtTurnPhase = m.Groups[1].Value.ToLowerInvariant() == "start" ? "turn_start" : "turn_end",
                    AtTurnOps = inner.Ops,
                    // 正文原文留着 —— 日志和 `PersistentEffect.Body` 都要能打印人话
                    Payload = body,
                });
                r.Kind = inner.Kind == SegKind.Ok ? SegKind.Ok : SegKind.Partial;
                return true;
            }

            // ---- 同一族、**别的触发点**的写法（2026-09-13 加）----
            // 前缀一模一样（`For the rest of this battle|the match,`），但后面既不是
            // `at the start|end of your turn`（上面那条），也不是回合起止。实测两种：
            //   · `give Shield to all Drones you deploy`            —— **部署时给**（TauEmpire）
            //   · `give Armour 1 to Vehicles you put in play`       —— 同上（Ultramarines）
            //   · `Sabotage cards in the enemy hand cost 1 more`    —— **持续改费**（Genestealers）
            // ⚠️ 认不出正文就 `return false` → 整句 `Unknown`。**决不能注册一条不会被消费的效果。**
            var any = RePersistAny.Match(low);
            if (any.Success)
            {
                string body = any.Groups[1].Value.Trim();
                if (body.Length == 0) return false;

                // ① 持续改费 —— 单独成一条 op（不走 Dispatch：它不是「对谁做什么」，
                //    而是给自己的费用系统挂一条筛选条件，见 EffectResolver.DoCostMore）
                var cm = ReCostMore.Match(body);
                if (cm.Success)
                {
                    string kw = cm.Groups[1].Value.Trim().ToLowerInvariant();
                    if (!CreatePool.IsKindWord(kw)) return false;   // 兵种词表里没有 → 不猜
                    r.Ops.Add(new EffectOp
                    {
                        Verb = "costmore",
                        Source = src,
                        Amount = int.Parse(cm.Groups[3].Value),
                        // `the enemy hand` = 加在**对手**手里那批牌上；`your hand` = 自己手里
                        Target = new EffectTargetSpec
                        {
                            Raw = body,
                            Side = cm.Groups[2].Value.ToLowerInvariant() == "the enemy" ? "enemy" : "own",
                            Kind = kw,
                            SubtypeFilter = MapToSubtype(kw),
                        },
                        Payload = body,
                    });
                    r.Kind = SegKind.Ok;
                    return true;
                }

                // ③ 🆕 2026-09-14 A4 批 3：`For the rest of this battle, **when <事件>**, <正文>`
                //    （`Raid Tactics`：`… when a friendly troop uses Ferocity, deal 2 damage to the
                //     enemy Warlord`，SpaceWolves）
                //    ⚠️ 事件那半句**复用 `WhenEvents.Parse`** —— `uses <关键词>` 那一族早在
                //       `WhenEvent.TryParseKeywordTrigger` 里了（那条注释点名的就是这张卡），
                //       **别在这儿另写一份事件短语解析**。
                //    ⚠️ 正文认不出就 `return false`（整句不认识）—— **决不能注册一条不会被消费的效果**。
                if (body.StartsWith("when "))
                {
                    int comma = body.IndexOf(',');
                    if (comma <= 5) return false;                    // `when ` 后面没有 `,` ⇒ 不是这个形状
                    var ev = WhenEvents.Parse(body.Substring(5, comma - 5).Trim());
                    if (ev == null || string.IsNullOrEmpty(ev.Kind)) return false;
                    var inner3 = Dispatch(body.Substring(comma + 1).Trim(), src);
                    if (inner3.Ops == null) return false;
                    r.Ops.Add(new EffectOp
                    {
                        Verb = "persist",
                        Source = src,
                        AtTurnPhase = ev.Kind,     // 事件种类（`triggers:<关键词>` 这种）—— 日志与排查用
                        AtTurnOps = inner3.Ops,
                        When = ev,
                        Payload = body,
                    });
                    r.Kind = inner3.Kind == SegKind.Ok ? SegKind.Ok : SegKind.Partial;
                    return true;
                }

                // ② 部署时给 —— 交给正常管线解，**正文里必须真有一条「打在刚部署那个身上」的目标**
                var inner2 = Dispatch(body, src);
                if (inner2.Ops == null) return false;
                EffectOp dep = null;
                foreach (var o in inner2.Ops)
                    if (o.Target != null && o.Target.Deployed) { dep = o; break; }
                // 正文解析得出、却没有「部署时」这个落点 → 这不是我们认得的句型，别硬认
                if (dep == null) return false;

                r.Ops.Add(new EffectOp
                {
                    Verb = "persist",
                    Source = src,
                    AtTurnPhase = "deploy",
                    AtTurnOps = inner2.Ops,
                    Filter = CardCriteria.FromTarget(dep.Target),
                    Payload = body,
                });
                r.Kind = inner2.Kind == SegKind.Ok ? SegKind.Ok : SegKind.Partial;
                return true;
            }

            // `Your Warlord gains: "At the start of your turn, …"` —— EmperorsChildren
            // `Coterie of the Conceit`。原版是**挂到督军身上**（`rule_core.gd:2486-2496` 写
            // `wl["fx"]["turn_start"]`）；我们让督军**所在的那一方**注册同一条常驻效果，
            // 触发时机完全一样（督军的回合就是那一方的回合），少一个存放位置。
            var w = ReWarlordGains.Match(low);
            if (w.Success)
            {
                string inner2 = w.Groups[1].Value.Trim().Trim('"', '「', '」').Trim();
                var at = SplitAtTurn(inner2);
                if (at == null) return false;
                var ops2 = Dispatch(at[1], src);
                if (ops2.Ops == null) return false;

                r.Ops.Add(new EffectOp
                {
                    Verb = "persist",
                    Source = src,
                    AtTurnPhase = at[0],
                    AtTurnOps = ops2.Ops,
                    Payload = at[1],
                });
                r.Kind = ops2.Kind == SegKind.Ok ? SegKind.Ok : SegKind.Partial;
                return true;
            }
            return false;
        }

        /// <summary>
        /// `At the start|end of your turn, &lt;正文&gt;` —— **手牌陷阱卡自己的文本**。
        ///
        /// 实测 2 张，都是 Genestealers 的 `Sabotage` 卡（规则书 :204「破坏 | 创造 1 张破坏卡
        /// 放入对手手牌」）：
        ///   `Poisoned Supplies`「At the end of your turn, your troops take 1 damage」·
        ///   `Cult Propaganda`「At the end of your turn, create a Neophyte Initiate in your opponent's hand」
        ///
        /// ⚠️ 这类卡**不是打出去生效的**，它躺在**持有者**手牌里、在**持有者**回合结束触发
        /// （`rule_core.gd:409-421` ①「当前玩家手牌中 sabotage 卡……持有者回合生效」）。
        /// 所以这里的 op 在**打出时什么都不做**（`DoAtTurn` 只写一行日志说明），
        /// 真正的结算在 `RuleCore.ResolveAtTurn` 里按手牌扫。
        /// </summary>
        static bool TryAtTurn(string low, string src, SegResult r)
        {
            // ⚠️ **这里用宽判据 `AtTurnClauses`，不是窄的 `SplitAtTurn`**（2026-09-14 A5 批 2）：
            //    这一支只管「**这句话解析得出来吗**」—— 宽判据才能把 `each turn` 与后缀式
            //    （`Takes 1 damage at the start of your turn`）如实算进覆盖率。
            //    窄判据是**手牌陷阱**那个问题（「这张卡躺手里时会不会自己响」），见 `SplitAtTurn`。
            var cs = AtTurnClauses(low);
            if (cs.Count != 1) return false;
            var cl = cs[0];
            if (cl.Body.Length == 0) return false;
            var inner = Dispatch(cl.Body, src);
            if (inner.Ops == null) return false;

            r.Ops.Add(new EffectOp
            {
                Verb = "atturn",
                Source = src,
                AtTurnPhase = cl.Phase,
                AtTurnOps = inner.Ops,
                Payload = cl.Body,
            });
            r.Kind = inner.Kind == SegKind.Ok ? SegKind.Ok : SegKind.Partial;
            return true;
        }

        /// <summary>
        /// 一条 **at-turn 从句**（`At the start|end of … turn, …`）。
        /// </summary>
        public struct AtTurnClause
        {
            /// <summary>`turn_start` / `turn_end`</summary>
            public string Phase;
            /// <summary>从句正文</summary>
            public string Body;
            /// <summary>`you` = **只在自己的回合**触发 · `each` = **双方回合都触发**（`each`/`every`）</summary>
            public string View;
            /// <summary>`pre` = 前缀式（`At the …` 开头）· `suf` = 后缀式（`… at the …` 结尾）</summary>
            public string Form;
        }

        /// <summary>
        /// 逐分句抽 at-turn 从句。**两种句式、两种视角都认** —— 照原版参考实现
        /// `d:/warpforge/scripts/rule_core.gd:341 _extract_at_turn` 的 `re_pre` / `re_suf`：
        ///   · **前缀式** `At the start|end of (your|each|every) turn[,:] &lt;正文&gt;`
        ///   · **后缀式** `&lt;正文&gt; at the start|end of (your|each) turn`
        ///     （`Concealed Explosives` = `Takes 1 damage at the start of your turn`）
        ///   · `every` ≡ `each`（那边就是这么归一的），`each` = **双方回合都触发**。
        ///
        /// 🔑 **为什么要它**（2026-09-14 A5 批 2）：**棋盘上的单位自己的**回合起止正文
        /// （`Chronomancer` / `Grot Orderly` / `Beast Snagga Nob` / `Aquilon Servo-Sentry` /
        /// `Unleashed TramplaSquig` / `Concealed Explosives` …）原来**没有任何消费点** ——
        /// `ResolveAtTurn` 只扫「当前行动方的手牌」与「已登记的常驻效果」，
        /// 而参考实现 `_at_turn_effects` 的**触发源②**就是「双方棋盘 at-turn 单位」。
        ///
        /// ⚠️ **前缀式锚在分句开头**（`^`）—— 那边的 `re_pre` 用的是不锚的 `search`，
        ///    照抄的话 `For the rest of this battle, at the start of your turn, …`（`Cadia Stands`）
        ///    会被这里再收一次、和「常驻效果」那条路**重复触发**
        ///    ⇒ 我们锚开头，并显式跳过 `For the rest of …` 开头的分句（它们归 `TryPersistent`）。
        /// </summary>
        public static List<AtTurnClause> AtTurnClauses(string desc)
        {
            var res = new List<AtTurnClause>();
            if (string.IsNullOrWhiteSpace(desc)) return res;
            foreach (string raw in Split(desc))
            {
                string seg = (raw ?? "").Trim();
                if (seg.Length == 0) continue;
                if (seg.StartsWith("for the rest of", System.StringComparison.OrdinalIgnoreCase)) continue;

                var pre = ReAtTurnPre.Match(seg);
                if (pre.Success && pre.Groups[3].Value.Trim().Length > 0)
                {
                    res.Add(new AtTurnClause {
                        Phase = pre.Groups[1].Value.ToLowerInvariant() == "start" ? "turn_start" : "turn_end",
                        Body = pre.Groups[3].Value.Trim(),
                        View = AtTurnView(pre.Groups[2].Value),
                        Form = "pre" });
                    continue;
                }
                var suf = ReAtTurnSuf.Match(seg);
                if (suf.Success && suf.Groups[1].Value.Trim().Length > 0)
                {
                    res.Add(new AtTurnClause {
                        Phase = suf.Groups[2].Value.ToLowerInvariant() == "start" ? "turn_start" : "turn_end",
                        Body = suf.Groups[1].Value.Trim(),
                        View = AtTurnView(suf.Groups[3].Value),
                        Form = "suf" });
                }
            }
            return res;
        }

        /// <summary>`your` → `you` · `each|every` → `each`（`rule_core.gd:362` 同口径）。</summary>
        static string AtTurnView(string word)
        {
            string w = (word ?? "").ToLowerInvariant();
            return (w == "each" || w == "every") ? "each" : "you";
        }

        /// <summary>`At the start|end of (your|each|every) turn[,:] &lt;正文&gt;` —— 前缀式。</summary>
        static readonly Regex ReAtTurnPre = new Regex(
            @"^at the (start|end) of (your|each|every) turn\s*[,:]?\s*(.+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>`&lt;正文&gt; at the start|end of (your|each|every) turn` —— 后缀式
        /// （`Concealed Explosives`）。`{2,80}?` 照那边 `re_suf`，防把整段长句吞进正文。</summary>
        static readonly Regex ReAtTurnSuf = new Regex(
            @"^(.{2,80}?)\s+at the (start|end) of (your|each|every) turn\.?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// `At the start|end of your turn, &lt;正文&gt;` → `[phase, 正文]`；不是这个形状返回 null。
        ///
        /// **只此一处**判「这句话是**手牌陷阱**形态的回合起止」—— `TryAtTurn`（解析）、
        /// `IsHandTrap`（给 `DeckBuilder` 排除陷阱卡）、`RuleCore.ResolveAtTurn`（手牌扫描）
        /// 三处都走它，免得写三份判据迟早不一致。
        ///
        /// ⚠️ **比 <see cref="AtTurnClauses"/> 窄，而且必须窄**（2026-09-14 A5 批 2 查实）：
        ///    · 只要**整条 desc 就是这一句**（多句的不认 —— `Grot Orderly` 是
        ///      `Rally: … . At the start of your turn, …`，它是**单位卡自己的**回合效果，
        ///      不是躺在手里的陷阱）；
        ///    · 只要**前缀式**（后缀式的 `Concealed Explosives`「Takes 1 damage at the start of your turn」
        ///      同样是**棋盘上的**单位效果）；
        ///    · 只要 `you` 视角（`Aquilon Servo-Sentry` 的 `each turn` 也是棋盘上的）。
        ///    放宽任一条，这些**单位卡**就会被 `DeckBuilder` 当成陷阱卡**排除出牌组**（静默）。
        /// </summary>
        public static string[] SplitAtTurn(string text)
        {
            if (Split(text ?? "").Count != 1) return null;
            foreach (var c in AtTurnClauses(text))
                if (c.Form == "pre" && c.View == "you") return new[] { c.Phase, c.Body };
            return null;
        }

        /// <summary>
        /// 这张卡是不是**手牌陷阱**（desc 是 `At the start|end of your turn, …` 形态）。
        ///
        /// 用处：`DeckBuilder` 要把它**排除在自动牌组之外** ——
        /// 陷阱卡是塞给**对手**的，自己牌组里放一张只会坑自己。
        /// 判据走 <see cref="SplitAtTurn"/>，不另写一份。
        /// </summary>
        public static bool IsHandTrap(string desc)
        {
            return SplitAtTurn(desc) != null;
        }

        /// <summary>
        /// `When &lt;事件&gt;, &lt;正文&gt;` —— **非单位卡躺在持有者手里监听事件**的那一类手牌陷阱
        /// （2026-09-14 A4 批 4 新增）。返回 `[事件短语, 正文]`；不是这个形状返回 null。
        ///
        /// 出处：`Jammed Communications`（Genestealers 的**破坏卡** —— 卡面橙字是 `Sabotage`，
        /// 见 `d:/2/Warpforge部队卡片/Genestealer Cult/6破坏卡/IMG_3817.jpg`）：
        /// `When you play a Stratagem, your Warlord takes 1 damage`。
        /// 「你」= **持有者**（被塞牌的那个人），与同族 `Poisoned Supplies` 已经定过并实现的口径一致。
        ///
        /// 🔴 **为什么单开一个判据、不让 `EffectText.Parse` 无条件认它**：
        ///    `When &lt;事件&gt;, …` 在**单位卡**上是**事件层**（`CardDef.WhenTriggers`）的地盘，
        ///    而单位卡的事件层**只在它站在场上时**才响（`CanListenForEvents`）。若解析层无条件认它，
        ///    **所有单位卡的 `When …` 会一并变成「解析得了」** ⇒ 覆盖率**虚报**
        ///    （报成「有机制」，而那条机制根本不在这个 op 上）。⇒ **判据必须在知道卡类的地方**，
        ///    所以这个函数吃 `CardDef` 而不是 `string`。
        ///
        /// 🔴 **两个「收得紧」的条件**（都是为了让判据不误伤，实测过）：
        ///   ① **整条 desc 就是一句话** —— 多句的不认。`Reconnaissance Mission`（DarkAngels 战术卡）
        ///      的 `Choose a card in your opponent's hand. When played, gain 3 Quest Points` 是**两句**，
        ///      第二句本来就已经会被结算（见 `CanListenForEvents` 的注释）—— 认了它会把它**排除出牌组**。
        ///   ② **事件短语必须解析得出**（`WhenEvents.ParseAll`）—— `When played, …` 那种我们
        ///      **没有**对应的广播点，认了就是一条**永远不响**的监听器（红线里的静默失效）。
        /// </summary>
        public static string[] SplitHandTrapWhen(CardDef c)
        {
            if (c == null || c.IsUnit) return null;      // 单位卡走事件层，不走这条
            var segs = Split(c.Desc);
            if (segs.Count != 1) return null;            // 条件 ①

            var m = ReWhenClause.Match(segs[0]);
            if (!m.Success) return null;
            string evPhrase = m.Groups[1].Value.Trim();
            string body = m.Groups[2].Value.Trim();
            if (evPhrase.Length == 0 || body.Length == 0) return null;

            if (WhenEvents.ParseAll(evPhrase).Count == 0) return null;   // 条件 ②
            return new[] { evPhrase, body };
        }

        /// <summary>`When &lt;事件&gt;, &lt;正文&gt;` —— 组 1 = 事件短语 · 组 2 = 正文。</summary>
        static readonly Regex ReWhenClause = new Regex(
            @"^when\s+(.+?),\s*(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 这张卡是不是**手牌陷阱**（躺在持有者手里生效、不是打出去生效）。
        ///
        /// 一族两种写法，**判据是本函数一处**：
        ///   · **回合起止型** `At the start|end of your turn, …`（`Poisoned Supplies` / `Cult Propaganda`）
        ///     —— 结算在 `EffectResolver.ResolveAtTurn` 的手牌扫描里（每回合各一次）。
        ///   · **事件型** `When &lt;事件&gt;, …`（`Jammed Communications`）—— 结算在
        ///     `EffectResolver.BroadcastHandTrapWhen`（每次事件广播时扫双方手牌）。
        ///
        /// 用处三处：`DeckBuilder`（陷阱是塞给**对手**的，自己牌组里放一张只会坑自己）·
        /// `CanPlayTactic`（陷阱**能打出去**，打出去 = 丢弃它）· 广播时扫手牌。
        /// </summary>
        public static bool IsHandTrap(CardDef c)
        {
            if (c == null) return false;
            return SplitAtTurn(c.Desc) != null || SplitHandTrapWhen(c) != null;
        }

        /// <summary>`For the rest of this battle|the match, at the start|end of your turn, …`
        /// —— 1=start/end · 2=正文。</summary>
        static readonly Regex RePersistAtTurn = new Regex(
            @"^for the rest of (?:this battle|the match)\s*,\s*at the (start|end) of your turn\s*,\s*(.+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// `For the rest of this battle|the match, &lt;正文&gt;` —— 1=正文。**兜底**：
        /// <see cref="RePersistAtTurn"/> 认不了时用它，正文交给 <see cref="Dispatch"/> 自己认。
        ///
        /// 2026-09-13 加。为什么原来没有：上一轮只做「回合起止」那一族，`at the start|end of your turn`
        /// 是那个 handler 的**前提**。但同一族还有**别的触发点**的句子，前缀一模一样：
        ///   · `… give Shield to all Drones you deploy`（**部署时**给 → <see cref="CardCriteria"/>）
        ///   · `… give Armour 1 to Vehicles you put in play`（同上）
        /// 现在先按 `RePersistAtTurn` 判，判不到再落到这里。
        ///
        /// ⚠️ **正文认不出就整句判不认识**（`return false` → `Unknown`），
        ///    绝不注册一条**永远不会被消费**的常驻效果 —— 那比「报不认识」糟得多（骗玩家）。
        /// </summary>
        static readonly Regex RePersistAny = new Regex(
            @"^for the rest of (?:this battle|the match)\s*,\s*(.+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// `Sabotage cards in the enemy hand cost 1 more` —— **持续改费**。
        /// 1=兵种/类别词 · 2=`the enemy`/`your` · 3=加多少。
        ///
        /// **实测只有 1 条**（全量卡池）：`Underground Network`（Genestealers，
        /// `Create a random Sabotage in the enemy hand. For the rest of this battle,
        /// Sabotage cards in the enemy hand cost 1 more`）。
        ///
        /// **原版出处（2026-09-13 查证）**：走 `HandEffect` ——
        /// `AbilityLogic.handBuff`（`AbilityLogic.cs:37`）+ `AbilityEffect.buffHand = 50`
        /// （`AbilityEffect.cs:13`），内容是
        /// `cardEffect{ buffType = changeCost(2), costChange = +1 }` +
        /// `playersAffected = Enemy` + `targetCriteria{ spellType = Sabotage(230) }`。
        /// 挂载 `PlayerHand.AddHandEffect`（`PlayerHand__AddHandEffect.c:93-104`），
        /// 算费用时**现查**（`EntityScript.CurrentCost`，`EntityScript.cs:74`）。
        ///
        /// ⚠️ **卡资产（ScriptableObject）不在本地**（远程 CCD 下发）—— 上面这组字段值是
        ///    子代理**按字段语义拼出来的**，不是逐字段抄来的。已如实标在
        ///    `资料/常驻效果_数据与设计.md`，别把它当成有卡数据佐证的结论。
        /// </summary>
        static readonly Regex ReCostMore = new Regex(
            @"^(.+?)\s+cards?\s+in\s+(the enemy|your)\s+hand\s+costs?\s+(\d+)\s+more$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>`a troop with Destroyer` 里的 `with <关键词>` —— 1=关键词。</summary>
        static readonly Regex ReTargetWith = new Regex(
            @"\s+with\s+([a-z][a-z' ]*)$", RegexOptions.Compiled);

        /// <summary>`Your Warlord gains: "…"` —— 1=引号里的效果原文。</summary>
        static readonly Regex ReWarlordGains = new Regex(
            @"^your warlord gains\s*:\s*(.+)$", RegexOptions.Compiled);

        /// <summary>
        /// 三选一：`Choose one: A; B or C`。
        ///
        /// **6 张战术卡**用这个写法（实测全量卡池）：Craftworld Convergence / The Rock /
        /// Duelist's Hubris / Inscrutable Cunning / Hymn of Battle / The Fang。
        /// 分隔符实测有两种：`;` 分前两项、最后一项前面是 ` or `。
        ///
        /// ⚠️ **原版在这里是「单机自动选 1」**（`rule_core.gd:1159` 的函数头：
        ///    「从句提取 → 候选域 → 3 候选 → **单机自动选 1**（battle.gd 弹窗后接）」）。
        ///    我们照这个行为：**用 `ctx.Rng` 掷一个**，但**解析阶段不掷**（见下）。
        ///
        /// ⚠️ 解析与应用分离带来的一个取舍：`EffectOp` 里没有随机源，所以这里
        ///    **三条选项都解析出来放在 `Payload` 里（用 `|` 隔开）**，
        ///    真正的挑选在结算层 `EffectResolver.DoChooseOne` 做（那里有 `ctx.Rng`）。
        ///    这样「覆盖率」量的是「三个选项的语法我们认不认得」，与掷骰无关，数字才稳定。
        /// </summary>
        static bool TryChooseOne(string low, string orig, string src, SegResult r)
        {
            if (!low.StartsWith("choose one")) return false;
            int colon = low.IndexOf(':');
            if (colon < 0) return false;

            string body = low.Substring(colon + 1).Trim();
            if (body.Length == 0) return false;

            // 分隔：先用 `;` 切，再在每一段里按 ` or ` 切（`A; B or C` → 三段）
            // 🆕 2026-09-14：**同一套切法跑两遍** —— 一遍跑小写那份（结构性判据用），
            //    一遍跑**原始大小写**那份（`orig`，**印在卡面上的就是它**）。
            //    只差大小写 ⇒ 切出来的段数一样时按**下标**一一对应，用原文当选项文字；
            //    对不上（`low` 被前面某支归一/剥过，如 `Carnifex` 那条）就退回小写那份 ——
            //    **判定结果一个字都不变**，只有「印出来的字」不同。
            //    🔴 为什么要原文：选牌面板把选项**画成一张卡**，小写的
            //    `deploy a grey hunter` 印在卡面上是看得见的粗糙（原版候选是真卡面）。
            var opts = SplitChooseOne(body);
            var optsOrig = (orig != null && low == orig.ToLowerInvariant())
                         ? SplitChooseOne(orig.Substring(colon + 1).Trim()) : null;
            if (optsOrig == null || optsOrig.Count != opts.Count) optsOrig = opts;
            if (opts.Count < 2) return false;

            // 每一段都要能**独立解析**，否则整句判半懂（别只认第一段就装作认了全部）
            var parsed = new List<string>();
            bool allOk = true;
            for (int i = 0; i < opts.Count; i++)
            {
                var sub = ParseSegment(opts[i]);
                if (sub.Kind == SegKind.Ok || sub.Kind == SegKind.KeywordOnly) { parsed.Add(optsOrig[i]); continue; }
                allOk = false;
                break;
            }
            if (!allOk) return false;

            r.Ops.Add(new EffectOp
            {
                Verb = "chooseone",
                Source = src,
                Amount = parsed.Count,
                Payload = string.Join("|", parsed),
            });
            r.Kind = SegKind.Ok;
            return true;
        }

        /// <summary>`choose one:` 后面那串选项 → 逐项文本（先按 `;` 切，再在每段里按 ` or ` 切）。
        /// **判据只此一份** —— 小写那份和原文那份都走它（见 `TryChooseOne`）。</summary>
        static List<string> SplitChooseOne(string body)
        {
            var opts = new List<string>();
            if (string.IsNullOrEmpty(body)) return opts;
            foreach (string part in body.Split(';'))
            {
                string p = part.Trim();
                if (p.Length == 0) continue;
                int or = p.IndexOf(" or ", System.StringComparison.Ordinal);
                if (or >= 0)
                {
                    string a = p.Substring(0, or).Trim();
                    string b = p.Substring(or + 4).Trim();
                    if (a.Length > 0) opts.Add(a);
                    if (b.Length > 0) opts.Add(b);
                }
                else opts.Add(p);
            }
            return opts;
        }

        /// <summary>
        /// `choose [a|an|the] &lt;筛选&gt;` —— 组 1 = **筛选原文**。
        /// 前瞻在 `and` / `from` / `in` **之前**收住，所以 `Choose a troop **from your deck**
        /// and draw it` 取到的是 `troop`，不是整句。
        /// 与 `rule_core.gd:1167` 逐字相同（连字符类里的 `[` `]` `.` `,` `;` 都一样）。
        /// </summary>
        static readonly Regex ReChooseWhat = new Regex(
            @"choose (?:an? |the )?([^.,;\[\]]+?)(?=\s+(?:and|from|in)\s|$)", RegexOptions.Compiled);

        /// <summary>`create N copies`（N 可是数字或 `two`/`three`）—— 组 1 = 张数。
        /// 实测只有一处：`Choose a non-Legendary Ultramarines card and create two copies in your hand`。</summary>
        static readonly Regex ReChooseCopies = new Regex(
            @"create (\d+|two|three) copies", RegexOptions.Compiled);

        /// <summary>🆕 `Choose and gain a bonus (A, B or C)` —— 组 1 = 括号里的**选项表**
        /// （`Carnifex`，全池只 1 处）。归一成 `choose one: …` 之后交给 `TryChooseOne`。
        /// 见 `Dispatch` 里 0·0c 那一段的说明。</summary>
        static readonly Regex ReChooseBonus = new Regex(
            @"^choose\s+and\s+gain\s+a\s+bonus\s*\((.+)\)$", RegexOptions.Compiled);

        /// <summary>🆕 `Stratagems in your hand become <A> or <B>` —— 组 1 = `A or B`
        /// （`Hrolf the Ironhowl`，SpaceWolves，全池只 1 处）。见 `Dispatch` 里 0·0d 那一段。</summary>
        static readonly Regex ReBecome = new Regex(
            @"^stratagems?\s+in\s+your\s+hand\s+become\s+(?:an?\s+)?(.+)$", RegexOptions.Compiled);

        /// <summary>
        /// 🆕 2026-09-14 T3：**「选一个效果」** —— 三种写法一次收掉。
        ///
        /// | 卡面 | 卡 | 池子里的条目是什么 |
        /// |---|---|---|
        /// | `Your Warlord heals 1 and chooses an effect` | `Exemplary Warrior`（UM 天赋） | **三张卡** |
        /// | `Choose an effect and give it to a friendly troop` | `Hyper-adaptation`（Leviathan） | **三段载荷** |
        /// | `Choose an effect and give it to all troops in your hand` | `Infinite Biomorphologies` | 同上（**作用域不同**） |
        ///
        /// ⚠️ **池子不写在句子里** —— 按「**正在结算的那张卡的名字**」查
        /// （`ctx.PlayingCard`，见 <see cref="EffectResolver"/> 的 `ChooseEffectPools`）。
        /// 这样同一套机制给三张卡共用，而效果文字只有**一份来源**（是卡就直接读那张卡的 `desc`）。
        ///
        /// ⚠️ 「给手牌」那一支**这一版没做**（手牌卡没有实例身份，加成无处可存 ——
        /// 见 `资料/选牌Choose_数据与设计.md` §六）⇒ 仍然产出 op，由**结算层如实报**，不静默。
        /// </summary>
        static bool TryChooseEffect(string low, string src, SegResult r)
        {
            // ---- ① `… and chooses an effect`（`Exemplary Warrior`）----
            //  前半句照常解释（`Your Warlord heals 1`），后面追一条 `chooseeffect`。
            //  ⚠️ 前半句**必须认得出**才收整句 —— 头都认不出还硬收，就成了「半懂装懂」。
            var mSelf = ReChooseEffectTail.Match(low);
            if (mSelf.Success)
            {
                var pre = Dispatch(mSelf.Groups[1].Value.Trim(), src);
                if (pre.Ops == null || pre.Ops.Count == 0) return false;
                r.Ops.AddRange(pre.Ops);
                r.Ops.Add(new EffectOp { Verb = "chooseeffect", Source = src, Payload = "self" });
                r.Kind = SegKind.Ok;
                return true;
            }

            // ---- ② `Choose an effect and give it to <目标>` ----
            var mGive = ReChooseEffectGive.Match(low);
            if (mGive.Success)
            {
                string t = mGive.Groups[1].Value.Trim();
                // 「给手牌里的部队」是**作用域**、不是场上的目标 ⇒ 不交给 `ParseTarget`
                // （它会把 `in your hand` 那截当噪声，解出一个**错的目标**）。
                bool hand = t.Contains("in your hand");
                var spec = hand ? null : ParseTarget(t);
                if (!hand && spec == null) return false;      // 目标词不认识 ⇒ 整句不认识
                r.Ops.Add(new EffectOp
                {
                    Verb = "chooseeffect",
                    Source = src,
                    Payload = hand ? "hand" : "give",
                    Target = spec,
                });
                r.Kind = SegKind.Ok;
                return true;
            }
            return false;
        }

        /// <summary>`<前半句> and chooses an effect` —— 组 1 = 前半句。
        /// ⚠️ 非贪婪 + `$` 锚定：`Heals 1 … and chooses an effect` 那种整段都算前半句。</summary>
        static readonly Regex ReChooseEffectTail = new Regex(
            @"^(.+?)\s+and\s+chooses? an effect\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>`choose an effect and give it to <目标>` —— 组 1 = 目标短语原文。</summary>
        static readonly Regex ReChooseEffectGive = new Regex(
            @"^choose an effect and give it to (.+?)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 选牌：`Choose a &lt;筛选&gt; [from/in &lt;来源&gt;] [and &lt;动词&gt;]` ——
        /// 权威源 `rule_core.gd:1157 _resolve_choose`（+ `:925` 候选匹配 · `:991` 候选收集）。
        ///
        /// **本 handler 只做两件事**：① 从句里剥出「来源 / 筛选 / 动作」记进 <see cref="EffectOp"/>
        /// ② 剩下的交给**已有的后续句解析**。
        ///
        /// 为什么②成立：`Split` **只按 `.` 切**，而这一族的动作常常写在**下一句**——
        /// `Choose a troop from your deck.` + `Draw it and create a copy of it in your hand`。
        /// 后一句**本来就已经能解析**（实测：`Draw it and create a copy…` / `It costs 2 less` /
        /// `Lower its cost by 1` / `Give it Stealth` 全不在未覆盖清单里）。
        /// 结算层只要把选中的卡写进 `ctx.LastCreated`，`(指代上一张)` 那一整套就自动接上。
        /// 所以 <see cref="EffectOp.ChooseAct"/> **空串是合法的** —— 那表示「只挑，动作在下一句」。
        ///
        /// ⚠️ **四处按我们的数据改过原版**（详见 `资料/选牌Choose_数据与设计.md` §二）：
        ///   ① 动作**只在本段里找**，不搜全 desc。原版搜全 desc（`:1208`）是为了接跨句动作，
        ///      而我们的 `Split` 按 `.` 切、跨句本来就是两段 —— 搜全 desc 反而会把
        ///      `Choose a troop in your hand` 误判成后一句的 `lower_cost`。
        ///   ② **不认** `" in your hand"` 这个兜底（原版 `:1231` 有）——
        ///      `Choose a card in **your opponent's** hand` 会被它误判成「进自己手牌」。
        ///   ③ 动作判不出来**不默认 `to_hand`**（原版 `:1193` 默认）—— 空串是合法的（见上）。
        ///   ④ `Choose an effect and give it …`（Leviathan 2 张）**判为不认识**，不硬塞
        ///      —— 这一条 **2026-09-14 已兑现**：用户把池子给了，机制落在
        ///      <see cref="TryChooseEffect"/>（紧跟在本函数之后）+ `EffectResolver.ChooseEffectPools`。
        ///      ⚠️ **本函数仍然不认它**（`what == "effect"` 直接返 false，见下面那一行）——
        ///      那是**故意**的，把这一族让给那个 handler。**别把这条删掉。**
        ///      ⚠️ 旧文写「候选效果池不在任何文本里（卡面 · `cards_engine.json` · 反编译三处零命中）」
        ///      —— 那是**当时的实况**；现在池子是**用户给的**（来源见 `ChooseEffectPools` 的注释）。
        /// </summary>
        static bool TryChooseCard(string low, string src, SegResult r)
        {
            // `choose one:` 已被上一步 `TryChooseOne` 领走；`choose a <筛选>` 才是本族
            if (!low.StartsWith("choose ")) return false;

            var m = ReChooseWhat.Match(low);
            if (!m.Success) return false;
            string what = m.Groups[1].Value.Trim();

            // 「选一个效果」不是选牌 —— 判不认识（见上面 ④）。别的 `what` 都当**卡的筛选词**
            if (what.Length == 0 || what == "effect") return false;

            // ---- 来源：只看「choose …」到第一个 ` and ` **之前**那一截 ----
            // （原版 `:1182`。防动作词把它带偏：`Choose a Sabotage and add it to the
            //   **enemy hand**` 里的 `enemy hand` 是**去处**，不是来源。）
            string head = low;
            int ap = low.IndexOf(" and ");
            if (ap >= 0) head = low.Substring(0, ap);
            head = head.TrimEnd('.', ' ', ':');

            string srcKind = "pool", deadScope = "";
            if (head.Contains("died since your last turn")) { srcKind = "dead"; deadScope = "since_last_turn"; }
            // 卡面 `this battle` 与 `this game` 是**同一个意思** —— 照 `rule_core.gd:1199-1204` 合并
            else if (head.Contains("died this battle") || head.Contains("died this game"))
            { srcKind = "dead"; deadScope = "all"; }
            else if (head.Contains("from your deck") || head.Contains("in your deck")) srcKind = "deck";
            // ⚠️ 对手手牌要**先于**自己手牌判：`in your opponent's hand` 里也含 `your`+`hand`
            else if (head.Contains("enemy hand") || head.Contains("opponent's hand")) srcKind = "enemyhand";
            else if (head.Contains("in your hand")) srcKind = "hand";

            // ---- 筛选词：剥掉方位那截、以及 `that …` 后面那半句（那是**条件**不是筛选）----
            foreach (string junk in new[] { " from your deck", " in your deck", " in your hand",
                                            " in the enemy hand", " in your opponent's hand" })
                what = what.Replace(junk, "");
            int ti = what.IndexOf(" that ");
            if (ti >= 0) what = what.Substring(0, ti);
            what = what.Trim();

            // ---- 动作：**只在本段里**找（见上面 ①）----
            // 顺序照 `rule_core.gd:1208-1236`：先具体后一般，`return it to your deck` 必须
            // 先于 ` to your deck`，否则「洗回牌库」会被判成「从别处拿一张放进牌库」。
            string act = ""; int copies = 0;
            if (low.Contains("return it to your deck")) act = "return";
            else if (low.Contains("shuffle it into their deck")) act = "shuffle";
            else if (low.Contains("draw it")) act = "draw";
            else if (low.Contains("at the top of your deck")) act = "decktop";
            else if (low.Contains(" to the enemy hand") || low.Contains(" to your opponent's hand")) act = "enemyhand";
            else if (low.Contains(" to your deck")) act = "todeck";
            else
            {
                var mc = ReChooseCopies.Match(low);
                if (mc.Success) { act = "copies"; copies = CountWord(mc.Groups[1].Value); }
                else if (low.Contains(" deploy it")) act = "deploy";
                // ⚠️ 这四种**都是显式写法**。**故意不收** 裸的 `" in your hand"`（见上面 ②）——
                //    `Choose a card in your opponent's hand` 会被它误判。
                else if (low.Contains("put it in your hand") || low.Contains("add it to your hand")
                      || low.Contains("add it your hand")      // ← 原版数据里真有这个少 `to` 的写法
                      || low.Contains("create it in your hand")) act = "hand";
            }

            r.Ops.Add(new EffectOp
            {
                Verb = "choosecard",
                Source = src,
                ChooseSrc = srcKind,
                ChooseWhat = what,
                ChooseAct = act,
                ChooseDeadScope = deadScope,
                ChooseCopies = copies,
            });
            r.Kind = SegKind.Ok;
            return true;
        }

        // `Deal 1 additional damage`（`Death from Above` 的 `[Codex]` 段）—— `additional` 是语气词。
        // 不剥的话「数字」和「damage」之间隔着一个词，整条正则失配。
        //
        // 🔴 **必须 `^` 锚定**（2026-09-13 候选 F 修）：原来**没有** `^`，于是
        //    `Destroy an enemy troop and deal 2-3 damage to adjacent units`（`Methodical Destruction`）
        //    会**从中间那个 `deal` 开始匹配**，整条只解出「造成 2-3 伤害」——
        //    **前面那句 Destroy 被静静吃掉**，而卡面报「解析干净」（卡面不打 `*`）。
        //    同类的还有 `ReStun` / `ReDestroy`（都补了 `^`）。
        //    ⚠️ 触发式正文（`Rally: Deal …`）走的是**冒号后那段单独解析**（`CardDef.AddTriggerOp`），
        //    所以锚定不会影响它们。
        static readonly Regex ReDeal = new Regex(
            @"^deals?\s+(?:(\d+)(?:-(\d+))?\s+)?(?:additional\s+)?damage(?:\s+to\s+(.+?))?$",
            RegexOptions.Compiled);

        /// <summary>
        /// `&lt;谁&gt; take(s) N damage` —— **反语序**的伤害句（`Deal N damage to X` 是正语序）。
        ///
        /// 实测全仓 **3 条**含 `take(s) N damage`：
        ///   · `At the end of your turn, your troops take 1 damage`（Genestealers `Poisoned Supplies`）
        ///   · `When you play a Stratagem, your Warlord takes 1 damage`（`Jammed Communications`）
        ///   · `Takes 1 damage at the start of your turn`（`Concealed Explosives`：**主语省略**）
        /// 前两条**主语写全**；第三条 2026-09-14 A5 批 2 起也收（见下面那条裸分支）。
        /// ⚠️ 还有一条**照旧判不认识**：上面第二条的**条件部分**（`When …` 从句不许当主语）。
        ///
        /// 🔑 **单复数从动词看**（不是靠猜名词）：
        ///   `your troops **take**` → **全体**（`Count = 0`）；`your Warlord **takes**` → 一个。
        ///   英语的动词形态把这件事说清楚了，比「名词是不是复数」可靠。
        /// </summary>
        static EffectOp TryTakeDamage(string low, string src)
        {
            // 🆕 **没写主语的 `Takes N damage` = 这张卡自己**（2026-09-14 A5 批 2）。
            // 口径和 `Heal N` 没写目标 = 自愈（A5 批 1）**完全一致**，判据也走同一个
            // `EffectTargetSpec.Subjectless`：**有施放者就是它自己**，没有施放者（战术卡）才落到己方全体。
            // ⚠️ 原来这里是「主语省略 ⇒ 判不认识」（见上面的注释），于是
            //    `Concealed Explosives` 那句**永远不生效**、只在报表里挂着 —— 而它的机制一直都在。
            var bare = ReTakeDamageBare.Match(low);
            if (bare.Success)
                return new EffectOp
                {
                    Verb = "deal", Source = src,
                    Amount = int.Parse(bare.Groups[1].Value),
                    Target = new EffectTargetSpec
                    {
                        Raw = "(未写主语：有施放者就是施放者自己，否则己方全体)",
                        Side = "own", Kind = "unit", Count = 0, Auto = true, Subjectless = true,
                    },
                };

            var m = ReTakeDamage.Match(low);
            if (!m.Success) return null;

            string who = m.Groups[1].Value.Trim();
            // ⚠️ **「什么时候」的从句不许当主语**：`When you play a Stratagem, your Warlord takes 1 damage`
            //    不挡的话 `(.+?)` 会把条件整段吃成目标、然后在**任何**时候都结算 ——
            //    那是「把有条件的卡当成无条件」，典型的静默错语义。
            if (who.StartsWith("when ") || who.StartsWith("if ")
                || who.Contains(", when ") || who.Contains(", if ")) return null;

            var spec = ParseTarget(who);
            if (spec == null) return null;                  // 主语认不出 → 不猜
            if (m.Groups[2].Value.ToLowerInvariant() == "take") spec.Count = 0;   // 复数动词 = 全体

            return new EffectOp
            {
                Verb = "deal", Source = src,
                Amount = int.Parse(m.Groups[3].Value),
                Target = spec,
            };
        }

        /// <summary>`&lt;谁&gt; take(s) N damage` —— 1=谁 · 2=take/takes · 3=几点。</summary>
        static readonly Regex ReTakeDamage = new Regex(
            @"^(.+?)\s+(takes?)\s+(\d+)\s+damage$", RegexOptions.Compiled);

        /// <summary>**没写主语**的 `Takes N damage` —— 1=几点。全仓只此 1 条
        /// （`Concealed Explosives`），见 <see cref="TryTakeDamage"/>。</summary>
        static readonly Regex ReTakeDamageBare = new Regex(
            @"^takes?\s+(\d+)\s+damage$", RegexOptions.Compiled);

        /// <summary>`Deal X damage [to Y]` —— `rule_core.gd:2672`。
        /// 目标词缺省（`Deal 3 damage`）= 原版自动选敌方最弱单位（`:2692`）。</summary>
        static EffectOp TryDeal(string low, string src)
        {
            // 🔴 **先切 ` and <动词>` 的尾巴，再匹配**（2026-09-14 A5 批 4）——
            //    `ReDeal` 锚了 `$`，而**没写目标的** `deal 5 damage` 后面接不了尾巴：
            //    `deal 5 damage and gain 1 Armour`（`Nephilim Jetfighter` 那句条件句的正文）
            //    整句失配 ⇒ 一直挂在「完全不认识」里。
            //    原来是在**匹配之后**对**目标组**切（`SplitAndTail(tok, …)`），那只照顾得到
            //    「有目标」的写法（`deal 3 damage to an enemy and give it Flank`）。
            //    ⚠️ 切在**前面**对已有写法**行为完全一致** —— `SplitAndTail` 只认
            //      「` and ` 后面**跟一个动词词**」那一种（`and an additional …` 不会被切走），
            //      而它在 `low` 上扫到第一个就停 ⇒ `low` 里不会再剩可切的 ` and <动词>`，
            //      下面那次对 `tok` 的切因此永远切不出东西（留着无害，两条路互斥）。
            string headTail, head;
            SplitAndTail(low, out head, out headTail);
            low = head;

            var m = ReDeal.Match(low);
            if (!m.Success) return null;
            var op = new EffectOp { Verb = "deal", Source = src };
            if (m.Groups[1].Success) op.Amount = int.Parse(m.Groups[1].Value);
            if (m.Groups[2].Success) op.AmountMax = int.Parse(m.Groups[2].Value);
            string tok = m.Groups[3].Success ? m.Groups[3].Value.Trim() : "";

            // `Deal X to Y and <另一句>` —— 原版在这里递归解尾句（`rule_core.gd:2751`），
            // 但它是**无条件**在第一个 ` and ` 切；我们按「后面是不是动词」判（见 `SplitAndTail`）。
            SplitAndTail(tok, out tok, out op.Tail);
            if (headTail.Length > 0) op.Tail = headTail;

            if (tok.Length == 0)
            {
                // 裸 `Deal N damage`（如 Fire prism）—— 原版是**定死的规则**：自动选敌方最弱单位
                // （`:2692` → `_auto_fx_target(ctx, p, TACTIC_ENEMY_PICK)`）。**不是**「不知道打谁」。
                op.Target = new EffectTargetSpec
                {
                    Raw = "(未写目标：按原版规则自动选敌方最弱单位)",
                    Side = "enemy", Kind = "any", Count = 1, Auto = true,
                };
            }
            else op.Target = ParseTarget(tok);
            return op;
        }

        /// <summary>
        /// `Each of your units deals damage equal to its &lt;关键词&gt; to &lt;目标&gt;`（2026-09-13 A4 批 1）。
        ///
        /// 出处：`Sudden Assault`（SaimHann）——「**你的每个单位**各对一个随机敌人造成
        /// **等于它自己星镖值**的伤害」。规则书 `:207` 的「星镖 X」是关键词值，不是固定数。
        ///
        /// 🔴 **为什么不能落进 `TryDeal`**：整句里也有 `deals … damage`，`ReDeal` 会从中间匹配，
        ///    把 `each of your units` 当成**目标短语**（那是 ParseTarget 认不出的词）⇒ 整句半懂；
        ///    更糟的是 `equal to its Shuriken` 这一段**没有任何 handler 认**，
        ///    于是「每单位打各自星镖值」会退化成「打 0 点」——**卡面不打 `*`、静默失效**。
        ///
        /// ⚠️ 数值取自**每一个单位自己**（不是我方全体求和、也不是施放者的），
        ///    这是这句话与 `equal to your Faith` 那族的根本区别（结算层 `DoEachUnitDeal`）。
        /// </summary>
        static EffectOp TryEachUnitDeal(string low, string src)
        {
            // ① `Each of your units deals damage equal to its <关键词> to <目标>`（`Sudden Assault`）
            var m = Regex.Match(low,
                @"^each\s+of\s+your\s+units?\s+deals?\s+damage\s+equal\s+to\s+its\s+([a-z][a-z ]*?)\s+to\s+(.+)$",
                RegexOptions.IgnoreCase);
            if (m.Success)
                return new EffectOp
                {
                    Verb = "eachunitdeal", Source = src,
                    Payload = m.Groups[1].Value.Trim().ToLowerInvariant(),
                    Target = ParseTarget(m.Groups[2].Value.Trim()),
                };

            // ② `Each of your units deals N[-M] damage to <目标>`（`Sergeant Gadriel`，Codex）
            //   🆕 2026-09-14 A5 批 3 第 4 条。和①**只差数值怎么写**：①是「按每个单位自己的关键词值」，
            //   ②是卡面写死的 `1-2`。主语同为**己方场上全体单位** ⇒ `Subject` 留空。
            //   ⚠️ 要**紧跟①之后**：①的正则要求 `damage equal to its`，②要是写在前面，
            //      `deals damage equal to its X` 里那个 `damage` 会被②当成数值而失配 —— 顺序写死更省心。
            m = Regex.Match(low,
                @"^each\s+of\s+your\s+units?\s+deals?\s+(\d+)(?:\s*-\s*(\d+))?\s+damage\s+to\s+(.+)$",
                RegexOptions.IgnoreCase);
            if (m.Success)
                return new EffectOp
                {
                    Verb = "eachunitdeal", Source = src,
                    Amount = int.Parse(m.Groups[1].Value),
                    AmountMax = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0,
                    Target = ParseTarget(m.Groups[3].Value.Trim()),
                };

            // ③ `Other friendly <筛选> deal N[-M] damage to <目标>`（`Deffkopta`，Rally）
            //   🆕 2026-09-14 A5 批 3 第 5 条。三处与①②不同，都要显式表达：
            //     · 主语**不是全体**，是 `Other friendly <X>` ⇒ `Subject`
            //     · **排除施放者自己**（`Other`）⇒ `OtherThanSelf`（`Deffkopta` 自己就是 Deffkopta）
            //     · 数值写死
            m = Regex.Match(low,
                @"^other\s+friendly\s+(.+?)\s+deals?\s+(\d+)(?:\s*-\s*(\d+))?\s+damage\s+to\s+(.+)$",
                RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var subj = SubjectOf(m.Groups[1].Value);
                if (subj == null) return null;      // 筛选词认不出 ⇒ **不猜**，交给后面判不知道
                return new EffectOp
                {
                    Verb = "eachunitdeal", Source = src,
                    Amount = int.Parse(m.Groups[2].Value),
                    AmountMax = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0,
                    Subject = subj,
                    OtherThanSelf = true,
                    Target = ParseTarget(m.Groups[4].Value.Trim()),
                };
            }
            return null;
        }

        /// <summary>
        /// `Other friendly <X>` 里的 `<X>` → <see cref="CardCriteria"/>。
        ///
        /// **先看是不是兵种词**（判据只此一份：<see cref="CreatePool.IsKindWord"/>），
        /// 不是就按**卡名**。
        /// ⚠️ 卡名那条**必须全等**（<see cref="CreatePool.Norm"/> 归一后比），
        ///    不许「包含」—— `CardCriteria.Name` 的注释点名的就是这个坑：
        ///    按包含匹配的话 `Mega Blasta Deffkopta` 也会被当成 `Deffkopta` 一起算进来。
        ///    （2026-09-14 实测：卡池里名字**含** `Deffkopta` 的有两张 —— `Deffkopta` 与
        ///      `Mega Blasta Deffkopta`；`Deffkopta` **不是** subtype、原版也**没有**这个 trait，
        ///      所以只能按卡名筛。）
        ///
        /// 🆕 2026-09-14（A7 光环）：可见性从 `private` 提到 `internal` ——
        /// <see cref="Auras.SubjectFilters"/> 是**第二个**调用点（光环的「主语 → 筛选条件」）。
        /// 提可见性而不是复制一份，是因为**这一族只有一份判据**（工程红线：两处写同一条规则 = 迟早不一致）。
        /// </summary>
        internal static CardCriteria SubjectOf(string text)
        {
            string w = (text ?? "").Trim().TrimEnd('.', ' ').Trim();
            if (w.Length == 0) return null;
            if (CreatePool.IsKindWord(w)) return new CardCriteria { KindWord = w.ToLowerInvariant() };
            return new CardCriteria { Name = CreatePool.Norm(w) };
        }

        /// <summary>
        /// **强制攻击族**（2026-09-13 A4 批 2）—— 让某个单位真的去打一下。
        ///
        /// 三种写法（全卡池实测就这几张）：
        ///   · `Make a damaged friendly unit attack by itself`（`Murderous Desires`）
        ///     —— **攻击者写在卡面上**（受伤的友方部队，由玩家点一个），打谁**没写**（`by itself`）
        ///   · `Target friendly unit attacks the enemy with highest attack`（`Peerless Bladesmen`）
        ///     —— 攻击者由玩家点，打**攻击力最高**的敌人（`Target2.PickMost = "+attack"`）
        ///   · 裸 `Attack(s) &lt;被打的&gt;`（`Let Loose` 的**尾句** · `Damaged Hexmark` 的 `Artifice`
        ///     正文 · `Morkai Eliminator`）—— 攻击者是**本卡自己**，所以 `Target` **留空**，
        ///     由调用方填（尾句形态由 `Finish` 反向继承前半句的目标；单位正文形态由结算层用施放者）
        ///
        /// 产物：`Target` = **攻击者**规格（可空）· `Target2` = **被打的**规格（可空 = 自动挑）·
        ///       `Payload` = 选择模式（`byitself`，语义待原版查实）。
        /// ⚠️ 「谁去打」和「打谁」在 `EffectOp` 里**是两栏**，别揉成一个 —— 揉了就是「打自己人」那类错。
        /// ⚠️ 要排在 `TryDeal` / `TryTakeDamage` 之前（`attack` 开头的句子不能被它们从中间截走）。
        /// </summary>
        static EffectOp TryForceAttack(string low, string src)
        {
            // ① `Make <攻击者> attack by itself`
            var m = Regex.Match(low, @"^make\s+(.+?)\s+attacks?\s+by\s+itself$", RegexOptions.IgnoreCase);
            if (m.Success)
                return new EffectOp
                {
                    Verb = "forceattack", Source = src, Payload = "byitself",
                    Target = ParseTarget(m.Groups[1].Value.Trim()),
                };

            // ② `Target <攻击者> attacks <被打的>`
            m = Regex.Match(low, @"^target\s+(.+?)\s+attacks?\s+(.+)$", RegexOptions.IgnoreCase);
            if (m.Success)
                return new EffectOp
                {
                    Verb = "forceattack", Source = src,
                    Target = ParseTarget(m.Groups[1].Value.Trim()),
                    Target2 = ParseTarget(m.Groups[2].Value.Trim()),
                };

            // ③ 裸 `Attack(s) by itself` —— 攻击者是本卡自己，打谁**也没写**（原版自动挑）
            //    （`Deathwing Knight` 的 `Teleport:` 正文 `Gain Vanguard and attack by itself` 尾句）。
            m = Regex.Match(low, @"^attacks?\s+by\s+itself$", RegexOptions.IgnoreCase);
            if (m.Success)
                return new EffectOp { Verb = "forceattack", Source = src, Payload = "byitself" };

            // ⑤ `This troop attacks <被打的>` —— 攻击者是**本卡自己**（2026-09-14 A5 批 4）。
            //    全池只 1 处：`Hunta Rig`（Goff）的 Rally 尾句
            //    `Choose a troop in the enemy hand and deploy it. **This troop attacks it.**`
            //    —— 跨句那一层已经把它并进 `Rally:` 的正文（见 `CardDef.TriggerBodyAt`）。
            //    ⚠️ 和 ④ 的分工：④ 是**裸** `Attacks <被打的>`（主语不在句子里）；
            //       这一条把主语写出来了（`This troop`），语义上仍是**本卡自己**
            //       ⇒ 两者产物一样（`Target` 留空、打谁在 `Target2`），只是入口写法不同。
            m = Regex.Match(low, @"^this\s+(?:troop|unit|card)\s+attacks?\s+(.+)$", RegexOptions.IgnoreCase);
            if (m.Success)
                return new EffectOp
                {
                    Verb = "forceattack", Source = src,
                    Target2 = ParseTarget(m.Groups[1].Value.Trim()),
                };

            // ④ 裸形态：`Attack(s) <被打的>` —— 攻击者**不在句子里**（是本卡自己 / 前半句那个目标）
            m = Regex.Match(low, @"^attacks?\s+(.+)$", RegexOptions.IgnoreCase);
            if (m.Success)
                return new EffectOp
                {
                    Verb = "forceattack", Source = src,
                    Target2 = ParseTarget(m.Groups[1].Value.Trim()),
                };

            return null;
        }

        /// <summary>`Stun [a|an|the|it] [enemy] [unit|troop]` —— `rule_core.gd:2755`。
        /// `Stun N random enemies` 的 N 在 `random` 那条分支里取（`:2759`）。
        /// 🔴 **`^` 锚定**（2026-09-13 候选 F 修）：见 `ReDeal` 那段注释 ——
        /// 不锚定的话 `X and stun Y` 这类句子会从中间的 `stun` 开始匹配、把前面那句吃掉。</summary>
        static EffectOp TryStun(string low, string src)
        {
            if (!ReStun.IsMatch(low)) return null;

            // ⚠️ **`and <另一句>` 的尾巴先切下来**（2026-09-14 A5 批 3）：
            //    `Stun enemy troops attacked **and give them -1 [armor] and -1 [attack]**`
            //    （`Sonic Blaster Noise Marine`）—— 不切的话整条尾巴被**吞进目标短语**，
            //    目标成了「enemy troops attacked and give them …」，
            //    **`give` 那半句静默不发生**（卡面不打 `*`，句子还报「认了」）。
            //    切法照 `TryReturn` / `TryDestroy` / `TryHeal` 那一族（切完由调用方的 `Finish` 递归解）。
            string tail = null;
            SplitAndTail(low, out low, out tail);

            var op = new EffectOp { Verb = "stun", Amount = 1, Source = src, Tail = tail };
            var m = Regex.Match(low, @"stun\s+(\d+|two|three)\s+");
            if (m.Success) op.Amount = CountWord(m.Groups[1].Value);

            // 🔴 **目标短语**（2026-09-13 候选 F 补）：这一段原来**一个字都不解**，`op.Target` 恒为 null ⇒
            //    结算层退回 `DoStun` 的兜底「**一个敌方单位（槽号最小）**」。于是
            //    `Stun adjacent units`（`None Must Know`）**眩晕的是敌方最左边那个**，
            //    而不是「刚被毁掉那个的邻居」；更糟的是这一支原来**直接置 `SegKind.Ok`**，
            //    所以整句还报「解析干净」—— 卡面不打 `*`，玩家完全看不出打错了人。
            //    照 `TryBlind` 的做法把目标解出来，并改走 `Finish`（半懂要报出来）。
            var mt = Regex.Match(low, @"^stuns?\s+(.+)$");
            if (mt.Success)
            {
                string tok = mt.Groups[1].Value.Trim();
                var md = Regex.Match(tok, @"^(\d+|two|three|four|five)\b");
                if (md.Success) tok = tok.Substring(md.Groups[1].Length).Trim();
                if (tok.Length > 0) op.Target = ParseTarget(tok);
                // 数量在动词后面（`Stun two random enemies`）：目标短语里没有数字，把 `Amount` 带过去
                if (op.Target != null && op.Target.Count == 1 && op.Amount > 1) op.Target.Count = op.Amount;
            }
            return op;
        }
        static readonly Regex ReStun = new Regex(@"^stuns?\b", RegexOptions.Compiled);

        /// <summary>
        /// `Blind a random enemy` / `Blind two random enemies` —— `rule_core.gd:2786` 那一族。
        /// ⚠️ **必须排在 `give` 前面**：卡面还有 `give them Blind until your next turn` 这种写法，
        ///    那一条走 `give`（载荷 `Blind` 命中 `GIVE_KW`），不动词开头的不归这里管。
        /// 目标短语照常用 `ParseTarget` 解 —— `a random enemy` / `two random enemies` 它都认。
        /// </summary>
        static EffectOp TryBlind(string low, string src)
        {
            if (!ReBlind.IsMatch(low)) return null;
            var op = new EffectOp { Verb = "blind", Amount = 1, Source = src };
            var m = Regex.Match(low, @"^blinds?\s+(.+)$");
            if (m.Success)
            {
                string tok = m.Groups[1].Value.Trim();
                var md = Regex.Match(tok, @"^(\d+|two|three)\b");
                if (md.Success) { op.Amount = CountWord(md.Groups[1].Value); tok = tok.Substring(md.Groups[1].Length).Trim(); }
                op.Target = ParseTarget(tok);
            }
            return op;
        }
        static readonly Regex ReBlind = new Regex(@"^blinds?\s", RegexOptions.Compiled);

        /// <summary>`Destroy`（含 `destroy it instead` 的条件形式 —— 条件由上层判，这里只认动词）。
        /// `rule_core.gd:2818`；⚠️ 原版 `Invulnerable` 挡得住（`:2820`），那在结算层。</summary>
        static EffectOp TryDestroy(string low, string src)
        {
            if (!ReDestroy.IsMatch(low)) return null;
            var op = new EffectOp { Verb = "destroy", Source = src };
            // `Destroy a random enemy troop` / `Destroy an enemy` —— 动词后面就是目标
            // ⚠️ **`destroys?` 两个都要**（2026-09-14 A5 批 3）：门 (`ReDestroy`) 一直都认
            //    `destroys?`，而这条取目标的正则只写了 `destroy` ⇒ **第三人称那半一律取不到目标**
            //    （`op.Target` 为 null ⇒ 整句半懂）。实测撞到的是 `Arjac Rockfist`：
            //    `Destroys any enemy troop with Hunt Mark attacked`。两处判据不一致 = 静默半懂。
            var m = Regex.Match(low, @"^destroys?\s+(.+)$");
            if (m.Success)
            {
                string tok = m.Groups[1].Value.Trim();
                // 🔴 `Destroy X and <另一句>` —— **尾句要解出来**（2026-09-13 候选 F 补）。
                //    原来这里**不切尾句**，于是 `Destroy an enemy troop and deal 2-3 damage to
                //    adjacent units`（`Methodical Destruction`）把后半句整个当成**目标短语** ——
                //    解出来变成「毁掉那个敌方部队**和它相邻的单位**」，而「造成 2-3 伤害」**整条没发生**。
                //    （`ReDeal` 锚定之前更糟：反被 deal 抢先匹配、**Destroy 那半句被吃掉**。
                //     两边都是「静默少做一半」，这一修两个方向都堵上。）
                //    切法照 `TryDeal` 一致：` and ` 后面是**动词**才切（`SplitAndTail`）。
                SplitAndTail(tok, out tok, out op.Tail);
                // `it` / `the target` / `them` 指代**上一条效果的目标**（原版用 `it_target` 记着，`:2730`），
                // 不是新目标 —— `ParseTarget` 会给它一个 `prev` 规格。
                op.Target = ParseTarget(tok);
            }
            return op;
        }
        static readonly Regex ReDestroy = new Regex(@"^destroys?\b", RegexOptions.Compiled);

        /// <summary>`Heal N [them|<目标>]` —— `rule_core.gd:2839`：**没写目标 = 治己方督军**（`:2842`）。</summary>
        static EffectOp TryHeal(string low, string src)
        {
            // ⚠️ **`and <另一句>` 的尾巴先切下来**（2026-09-13 A4）：
            //    `Heal 5 **and give Camouflage to a friendly unit**`（`Evasive Manoeuvre`）——
            //    不切的话 `ReHeal` 锚了 `$` 整条失配 ⇒ 整句认不出。
            //    切下来之后本句没有目标（目标写在尾句上），由 `Finish` 那条**共用目标**的规则接上。
            string tail = null;
            SplitAndTail(low, out low, out tail);

            var m = ReHeal.Match(low);
            if (!m.Success)
            {
                // 🆕 2026-09-14 A5 批 3：**代词当主语** `it heals 2`（`Apothecary` 的
                // `When a friendly unit obtains [Shield], **it** heals 2` —— 治的是**拿到盾的那个**）。
                // 主语是指代 ⇒ 交给 `ParseTarget` 走 `prev`（= `LastTargets`）；在**事件层**的正文里
                // `BroadcastWhen` 会把事件主语从 `seed` 种进去 ⇒ 指的正是「发生那件事的单位」。
                // ⚠️ **只放行代词**（`it` / `this troop` / `this unit` / `the target`）——
                //    下面那条注释点名的坑（**条件从句当主语**：`When another troop dies, heals 2`）
                //    靠的正是这条边界：一般名词短语一律不放行。
                var mp = ReHealPronounSubj.Match(low);
                if (mp.Success)
                    return new EffectOp
                    {
                        Verb = "heal", Source = src, Tail = tail,
                        Amount = int.Parse(mp.Groups[2].Value),
                        Target = ParseTarget(mp.Groups[1].Value),
                    };

                // 🆕 2026-09-14 T3：**反语序** `Your Warlord heals N`（主语在前）。
                // 实测全卡池**只有这 1 条** —— `Exemplary Warrior` 的
                // `Your Warlord heals 1 and chooses an effect`（`Da Irongob` 那条的 `heals 5`
                // 是**尾句**，`ReHeal` 本来就吃）。所以判据收得很死：**主语必须正好是 `(your|the) warlord`**。
                // ⚠️ **不开一般的「名词短语 + heals N」** —— 那会踩到本工程明确记过的坑
                //    「**条件从句当主语**」：`When another troop dies, heals 2`（`Pyrovore`）/
                //    `When a friendly unit obtains Shield, **it** heals 2`（`Apothecary`）会被
                //    吃成「任何时候都治疗某个单位」的静默错语义。
                var mr = ReHealReverse.Match(low);
                if (!mr.Success) return null;
                return new EffectOp
                {
                    Verb = "heal", Source = src, Tail = tail,
                    Amount = int.Parse(mr.Groups[2].Value),
                    Target = ParseTarget(mr.Groups[1].Value),
                };
            }
            var op = new EffectOp { Verb = "heal", Source = src, Tail = tail };
            op.Amount = m.Groups[1].Success ? int.Parse(m.Groups[1].Value) : 0;
            if (m.Groups[2].Success) op.AmountMax = int.Parse(m.Groups[2].Value);   // `1-5`
            if (op.Amount == 0) op.Amount = CountWordOrZero(m.Groups[3].Value);
            string tok = m.Groups[4].Success ? m.Groups[4].Value.Trim() : "";
            // `Heal them N` 里的 **`them` 是特例**：`rule_core.gd:2842` 明确把它定成
            // 「治疗己方全体」（Righteous Repugnance 那个激活用）。
            // ⚠️ 只有**复数**的 `them` 是特例 —— 单数的 `it` / `this unit` / `the target`
            //    是**指代上一条效果打中的那个单位**，交给 `ParseTarget` 走 `prev` 那条路。
            //    2026-09-12 撞到：原来把两者一勺烩，`If the target survives, heal 3 to it`
            //    变成「治疗**己方全体** 3 点」—— 该治的没治，不该治的全治了。
            if (IsPluralPronoun(tok))
            {
                op.Target = new EffectTargetSpec
                {
                    Raw = "(them：按原版 = 己方全体)", Side = "own", Kind = "unit", Count = 0, Auto = true,
                };
                return op;
            }
            // 🆕 2026-09-14 A5 批 1：**`Heal N` 没写目标 = 治疗自己**。
            // 实测（逐句探针）：`Heals 3` / `Strike: Heals 4` / `Codex: Heal 1` 原来都是
            // **半懂** —— `heal n=N 目标[（没写）]`（`NeedsTarget("heal")` 为真、`Target` 为 null）
            // ⇒ 卡面打 `*`、掉出「完全解析」，而机制本来就有（自愈）。
            // 判据走 `EffectTargetSpec.Subjectless`（和 `Give <内容>` 没写目标**同一条路**）：
            // **有施放者（单位触发正文 / `Codex:` 正文）⇒ 就是它自己**；没有施放者（战术卡）才落到己方全体。
            //
            // ⚠️ **只在「本句没有尾句」时套这个默认** —— 有尾句时 `Target` 必须留 null，
            //    好让 `Finish` 那条**共用目标**的规则去接（`Heal 5 **and give Camouflage to a
            //    friendly unit**`，`Evasive Manoeuvre`：治的是**那个友方单位**、不是自己）。
            //    不这么写就会把那张卡从「治它」变成「治自己」—— 静默错打。
            //    ⚠️ `SplitAndTail` 给 `tail` 的初值是**空串不是 null**（实测踩到：写 `tail == null`
            //       永远不成立，改动看起来「装上了」其实没生效）。
            op.Target = (tok.Length == 0 && string.IsNullOrEmpty(tail))
                      ? new EffectTargetSpec
                        {
                            Raw = "(未写目标：有施放者就是施放者自己，否则己方全体)",
                            Side = "own", Kind = "unit", Count = 0, Auto = true, Subjectless = true,
                        }
                      : (tok.Length == 0 ? null : ParseTarget(tok));
            return op;
        }

        /// <summary>
        /// 这个动词**是不是必须有目标**（没有就算半懂）。
        ///
        /// ⚠️ 2026-09-12 从 `Finish` 里提出来的：原来写的是 `op.Verb != "draw"` —— 只有 `draw` 被豁免，
        /// 于是 `Deploy …` / `Create …` 这些**本来就不带目标**的动词一进 `Finish` 就被判成半懂。
        /// 加一个动词就要来改一次这个判断，所以收成一处：**没有目标也成立的动词列在这儿**。
        /// </summary>
        static bool NeedsTarget(EffectOp op)
        {
            switch (op.Verb)
            {
                case "draw": case "drawtype": case "create": case "deploy": case "refill":
                case "lowercost":
                case "gainenergy": case "chooseone": case "reanimate":
                // `choosecard` 自己挑候选、`return` 的目标在 `Payload` 里自己拆、
                // `drawref` 指代的是**已经定好的那张卡** —— 三者都不靠表现层选目标。
                // 缺了这几条会被 `Finish` 判成 `Partial`（「半懂」），明明解析对了却报半懂。
                case "choosecard": case "return": case "drawref":
                // `become`（`Stratagems in your hand become A or B`）自己筛自己换，
                // 卡面没有任何「选一个目标」的位置 —— 不加这一条会被 `Finish` 判成「半懂」。
                case "become":
                // `persist` / `atturn` 是**注册**和**标记**，正文里的 op 各自在触发时才要目标
                case "persist": case "atturn":
                // 🆕 `forceattack` 的 `Target` 是**攻击者**：只有卡面**点名了让玩家选一个**
                //    （`Count == 1`）时才需要选。裸形态（`Attacks a random enemy` /
                //    `attack by itself`）的攻击者是**本卡自己**，`Target` 是空的 ⇒ **不需要选**。
                //    （2026-09-13 A4 批 2 —— 不加这一条，裸形态会被 `Finish` 判成「半懂」。）
                case "forceattack": return op.Target != null && op.Target.Count == 1;
                // 🆕 `costmore` 有**两种**：加给「某一类牌」的那支**要**目标（`Target.Kind` 说加给谁），
                //    加给「**这张卡自己**」的那支（`Payload == SelfCostMoreMarker`）**不要** ——
                //    卡面写的就是这张卡，没什么可选的。
                //    （2026-09-14 A5 批 3 第 1 条。不加这一条，`Finish` 会把解析正确的
                //     自加价判成「半懂」⇒ 卡面打 `*`、进不了卡组。）
                case "costmore": return op.Payload != SelfCostMoreMarker;
                default:
                    return true;
            }
        }

        /// <summary>复数的「他们」—— 原版把 `them` 定成己方全体，单数的 `it` 不是</summary>
        static bool IsPluralPronoun(string t)
        {
            t = (t ?? "").Trim().ToLowerInvariant();
            return t == "them" || t == "all of them";
        }
        // ⚠️ 组号：1 = 定值 · 2 = 区间上界 · 3 = 英文数字 · 4 = 目标。
        //    区间（`heal 1-5`）是 2026-09-12 补的 —— `deal` 一直支持 `1-3`，`heal` 却只吃单个数，
        //    于是 `Sawbonez` 的 `If the target survives, heal 1-5 to it` 整句判「不认识」。
        static readonly Regex ReHeal = new Regex(
            @"^heals?\s+(?:(\d+)(?:\s*-\s*(\d+))?|(two|three|four|five))?"
            + @"(?:\s*(?:points? of )?(?:health|damage)?)?\s*(?:to\s+(.+))?$",
            RegexOptions.Compiled);

        /// <summary>
        /// **反语序**的 `heal`：`Your Warlord heals N`（主语在动词**前面**）。
        /// 组 1 = 主语原文 · 组 2 = 数值。
        ///
        /// ⚠️ **主语卡死在 `(your|the) warlord`**：实测全卡池只有 **1 条**
        /// （`Exemplary Warrior` 的 `Your Warlord heals 1 and chooses an effect`，2026-09-14 T3）。
        /// 放开成一般的「名词短语 + `heals N`」会**吃掉条件从句** ——
        /// `When another troop dies, heals 2`（`Pyrovore`）· `…, it heals 2`（`Apothecary`）
        /// 会被当成「任何时候都治疗」的静默错语义（本工程记过的坑：条件从句不许当主语）。
        /// 真要放开，先按铁律把全池同形状的句子普查找出来、逐条确认该不该收。
        /// </summary>
        static readonly Regex ReHealReverse = new Regex(
            @"^((?:your|the)\s+warlord)\s+heals?\s+(\d+)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>**代词当主语**的 `it heals N` —— 1=代词（交给 `ParseTarget` 走 `prev`）· 2=点数。
        /// 全池 1 条（`Apothecary`），见 <see cref="TryHeal"/>：只放行代词，绝不放行一般名词短语。</summary>
        static readonly Regex ReHealPronounSubj = new Regex(
            @"^(it|this\s+troop|this\s+unit|the\s+target)\s+heals?\s+(\d+)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// `Double the Melee Attack and Health of a friendly troop`（`Possession`，Black Legion，8 费）
        /// —— 🆕 2026-09-14 A4 批 3。组 1 = 「什么」· 组 2 = 「谁」。
        ///
        /// ⚠️ **组 1 卡死在 `melee attack and health` 这一个字面**（不是 `(.+)`）——
        ///    全卡池 `Double` 只有 **2 句**，另一句是 `Maulerfiend` 的
        ///    `Ecstasy 5: Double this troop's [Melee] and [Ranged]`：**翻的是近战+远程、没有生命**，
        ///    而且它属于 **`ecstasy`（尚未实现的关键词）**那一族。
        ///    用 `(.+)` 会让那句也被这条 handler 领走、然后**按「近战+生命」翻倍** = 静默算错。
        /// </summary>
        static readonly Regex ReDouble = new Regex(
            @"^double the (melee attack and health) of (.+?)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>`Double the <什么> of <谁>` → `double` 动词（见 <see cref="ReDouble"/>）。</summary>
        static EffectOp TryDouble(string low, string src)
        {
            var m = ReDouble.Match(low);
            if (!m.Success) return null;
            var spec = ParseTarget(m.Groups[2].Value.Trim());
            if (spec == null) return null;        // 目标词不认识 ⇒ **整句不认识，不猜**
            return new EffectOp
            {
                Verb = "double", Source = src,
                Payload = m.Groups[1].Value.Trim().ToLowerInvariant(),
                Target = spec,
            };
        }

        // ==================================================================
        //  `Trigger the <关键词> ability of <目标>` —— 强行触发（2026-09-14 A4 批 4）
        // ==================================================================

        /// <summary>组 1 = 关键词（可以多个，`and` 连） · 组 2 = 目标。</summary>
        static readonly Regex ReTriggerAbilityOf = new Regex(
            @"^trigger\s+(?:the\s+)?(.+?)\s+(?:ability|abilities|effect|effects)\s+of\s+(.+?)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>组 1 = 关键词（`and trigger their Teleport abilities`）—— **不写目标**。</summary>
        static readonly Regex ReTriggerTheirAbility = new Regex(
            @"^trigger\s+their\s+(.+?)\s+(?:ability|abilities|effect|effects)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>组 1 = 关键词 —— **所有格写法**：`Trigger this troop's Codex ability`
        /// （`Oath of Moment` 的嵌入正文）。目标没有显式写，指的是**这个单位自己** ⇒ 走 `Subjectless`。</summary>
        static readonly Regex ReTriggerPossessiveAbility = new Regex(
            @"^trigger\s+(?:this\s+troop's|this\s+unit's|its)\s+(.+?)\s+(?:ability|abilities|effect|effects)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// `Trigger the &lt;关键词&gt; ability/abilities of &lt;目标&gt;` —— **把那个关键词的正文
        /// 现在结算一遍**（不等它自己的时机）。以及尾句形态 `… and trigger their &lt;关键词&gt; abilities`。
        ///
        /// 🔴 **实测量出来的规模**（2026-09-14 A4 批 4，逐句探针跑了全池**含 `trigger` 的 22 个分句**）：
        ///   · 带目标：`Author of the Codex`（UM）`Trigger the Codex ability of a friendly unit
        ///     and choose a Codicil and put it in your hand` ·
        ///     `Duty's End`（GS）`💀 backlash: trigger the codex ability of all friendly units` ·
        ///     `Atalan Leader`（GS）`Strike: Trigger the Ambush abilities of all friendly troops`
        ///   · 尾句（无目标）：`Regimental Doctrine` / `Deathwing Assault` / `Open Insurrection` /
        ///     `Stomp Em` / `Codex Discipline` 的 `… and trigger their &lt;X&gt; abilities`
        ///
        /// 🔴 **两种语序是同一件事**：`of &lt;谁&gt;` 写了目标；`their &lt;X&gt; abilities` 没写 ——
        ///    由 `Finish` 的**反向共用目标**继承前半句的目标（那条 `NeedsTarget` 规则）。
        ///    ⚠️ 所以尾句形态**必须**让 `Target` 留空，别在这里自己猜「他们」是谁。
        ///
        /// ⚠️ **关键词必须在 `CardDef.RoutableTriggers` 里**（= 引擎真的会在那个时机收正文）——
        ///    否则就是「触发了、但正文压根没收下来」的**静默空转** ⇒ 这里**直接判不认识**
        ///    （宁可卡面打 `*`，也不做一条永远不响的动词，见 `CanListenForEvents` 的同一条纪律）。
        ///
        /// ⚠️ `Trigger the abilities requiring Spirit Stones of all your troops`（`Cosmic Serpent`）
        ///    **认不出是有意的** —— 它那个「关键词」位置上是 `abilities requiring spirit stones`，
        ///    规范化不出任何关键词（而且卡池里 `N [Spirit Stone]:` 一张都没有 = 触发源为空）。
        ///    别为了让覆盖率好看硬塞一条正则。
        /// ✅ `Trigger the Teleport **and Slay** effects of a friendly unit`（`Master Lazarus`）
        ///    **收** —— 一句话点名两个关键词，`Payload` 里存成逗号分隔的两个（结算层逐个触发）。
        ///    ⚠️ 2026-09-14 改这一条的理由：**不收它就等于把那半句静默吞掉**（原来只有
        ///    `and gain 1 Quest Point` 被解出来，议程照样能用、但传送/斩杀那半永远不发生）；
        ///    收不了时判**半懂/不认识**是对的，但「一句话两个关键词」本身收得下来，没理由不收。
        /// </summary>
        static EffectOp TryTriggerAbility(string low, string src)
        {
            var m = ReTriggerAbilityOf.Match(low);
            bool hasTarget = m.Success;
            if (!hasTarget) m = ReTriggerTheirAbility.Match(low);
            // 🆕 **所有格写法**：`Trigger this troop's Codex ability`（`Oath of Momentum` 的嵌入正文，
            //    2026-09-14 族 A 收尾）。目标**就是**挂着这条能力的那个单位 ——
            //    用 `Subjectless`（= 有施放者就是施放者自己），和 `Give <内容>` 没写目标**同一条路**。
            bool possessive = false;
            if (!m.Success) { m = ReTriggerPossessiveAbility.Match(low); possessive = m.Success; }
            if (!m.Success) return null;

            // 卡面用**方括号表示图标**（`[Mob] Mob abilities` / `[Codex icon] Codex`）——
            // 先剥掉方括号那截再规范化（`Normalize` 是**前缀匹配**，`mob mob` 会命中 `mob`）。
            string phrase = Regex.Replace(m.Groups[1].Value, @"\[[^\]]*\]", " ");

            // 一句话可以点名**多个**关键词：`Trigger the Teleport and Slay effects of …`
            // ⚠️ **每一个都要能规范化 + 在 `RoutableTriggers` 里**，有一个不合格就**整句不收**
            //    （部分收 = 「打得比卡面窄」且卡面不打 `*`，见 `WhenEvents.ParseAll` 的同一条纪律）。
            var kws = new List<string>();
            foreach (string tok in Regex.Split(phrase, @"\s+and\s+|\s*,\s*"))
            {
                string one = tok.Trim();
                if (one.Length == 0) continue;
                string kw = KeywordTable.Normalize(one);
                if (kw == null) return null;
                bool routable = false;
                foreach (string t in CardDef.RoutableTriggers) if (t == kw) { routable = true; break; }
                if (!routable) return null;
                if (!kws.Contains(kw)) kws.Add(kw);
            }
            if (kws.Count == 0) return null;

            var op = new EffectOp
            {
                Verb = "triggerability", Source = src,
                Payload = string.Join(",", kws.ToArray()),
            };
            if (hasTarget)
            {
                // `… ability of a friendly unit **and choose a Codicil and put it in your hand**`
                // —— 尾句交给 `Finish` 递归解（`choose` 已进 `IsVerbWord`，见那条注释）。
                string tp = m.Groups[2].Value.Trim();
                SplitAndTail(tp, out tp, out op.Tail);
                op.Target = ParseTarget(tp);
            }
            else if (possessive)
            {
                // 「所有格」写法（`Trigger this troop's Codex ability`）：目标 = **挂着这条能力的那个单位**。
                // 判据走 `EffectTargetSpec.Subjectless`（和 `Give <内容>` 没写目标**同一条路**：
                // 有施放者就是施放者自己）—— ⚠️ **不能留 null**：`ResolveTargets` 对 null 返回空表，
                // 那会变成「触发了一件事、谁也没动」的静默空转。
                op.Target = new EffectTargetSpec
                {
                    Raw = "this troop's（它自己）",
                    Side = "own", Kind = "unit", Count = 0, Auto = true, Subjectless = true,
                };
            }
            return op;
        }

        /// <summary>
        /// `The next time it uses Ferocity this turn, it stays in play`（`Bjorn's Shrine`，SpaceWolves）
        /// —— 🆕 2026-09-14 A4 批 3。组 1 = 主语（`it` = 上一句那个友方单位）。
        ///
        /// ⚠️ **判据要卡住 `the next time … this turn` 这个整句式** ——
        ///    同阵营另有一张 `Bjorn the Fell-Handed`（`SW42`）写的是
        ///    `When a friendly unit uses Ferocity, it stays in play`：**常驻、无 `next time`、无 `this turn`**，
        ///    和这张**不是同一条语义**（那条归事件层）。收宽了就会把那张也按「一次性」处理。
        /// </summary>
        static readonly Regex ReFerocityStay = new Regex(
            @"^the next time (.+?) uses ferocity this turn,?\s*it stays in play\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>`The next time it uses Ferocity this turn, it stays in play` → `ferocitystay`。</summary>
        static EffectOp TryFerocityStay(string low, string src)
        {
            var m = ReFerocityStay.Match(low);
            if (!m.Success) return null;
            // 主语 `it` 走 `prev`（指代上一句那个单位）；认不出就**整句不认识**，不猜
            var spec = ParseTarget(m.Groups[1].Value.Trim());
            if (spec == null) return null;
            return new EffectOp { Verb = "ferocitystay", Source = src, Target = spec };
        }

        /// <summary>
        /// `Draw N [cards]`（`rule_core.gd:2863`）与 **5a 定向翻找** `Draw a <类型> [from your deck]`（`:2867`）。
        ///
        /// ⚠️ 先试**常规**那条：`Draw a card` 走常规；只有常规整句匹配不上、且剩下一个**类型词**时，
        ///    才当定向翻找。反过来会把 `Draw a card` 认成「翻找 card 类型」。
        /// </summary>
        /// <summary>
        /// `Draw an additional &lt;类型&gt;` → `Draw a &lt;类型&gt;`。
        ///
        /// 🔴 **2026-09-13 第三十二轮查那 47 条「完全不认识」时定位出来的**：
        ///    `ReDrawType` 的量词位只放行 `a/an/the` 与数字，于是 `Draw an additional troop`
        ///    **整句失配**。而那句前面是 `2 : `（付费前缀）—— `RePaid` 把前缀**成功剥掉了**，
        ///    正文却没人认 ⇒ **最后整段判 `Unknown`，付费前缀那条路白走**。
        ///    （`Devout Warriors` 的 `2 : Draw an additional troop`。）
        ///
        /// ⚠️ **这是「我们抄窄了」，不是 `rule_core` 的缺口** —— 派出去查证的子代理把这句
        ///    归到了 `rule_core.gd:1567-1573` 那条安全校验上；**主对话复核后更正**：
        ///    那条是**付费前缀**的校验（另一回事），而**正文解析**是它写得对、我们抄窄。
        ///    ⇒ 按「补我们的缺口」处理，别去改 `rule_core` 的语义。
        ///
        /// **语义**：卡面 `Draw a troop. 2 : Draw an additional troop` 合起来是
        ///    「付 2 费**再**抽一张 troop」—— 所以 `additional` **不改变数量**，只是措辞。
        ///    剥掉它之后 `Draw a troop` 正好被原有正则接住，**数量仍是 1**（对）。
        ///
        /// ⚠️ **只在 `draw` 这一族剥，不做全局剥** —— 全局剥会伤到
        ///    `…, and an additional +2 X for each …` 那种**追加**语义（那由 `for each` 层管）。
        ///    实测卡池里这个形状只有 **1 条**（普查：含 `additional` 的 17 条分句里，
        ///    只有它和 `4: Give an additional +1 Health` 两条判不认识，其余早就能解）。
        /// </summary>
        static string StripAdditionalForDraw(string low)
        {
            if (string.IsNullOrEmpty(low) || !low.StartsWith("draw")) return low;
            const string withArticle = " an additional ";
            int i = low.IndexOf(withArticle);
            if (i >= 0) return low.Substring(0, i) + " a " + low.Substring(i + withArticle.Length);
            const string bare = " additional ";
            i = low.IndexOf(bare);
            if (i >= 0) return low.Substring(0, i) + " " + low.Substring(i + bare.Length);
            return low;
        }

        static EffectOp TryDraw(string low, string src)
        {
            // ⚠️ **先把 `and <另一句>` 的尾巴切下来**再匹配 —— 两条正则都锚了 `$`，
            //    尾巴一出现就整句失配。实测 3 个分句栽在这儿：
            //      `Draw a Vehicle and give it Armour 2` / `Draw a troop and give it +1` /
            //      `Draw two troops and give them +1`（2026-09-12）
            //    切下来的尾巴由调用方的 `Finish` 递归解（本函数不自己加 op）。
            string tail = null;
            SplitAndTail(low, out low, out tail);

            // ⚠️ **`Draw an additional <类型>`** —— 见下面那段长注释。
            //    在**切完尾巴之后、匹配之前**剥掉 `additional`（它不改变数量，只是措辞）。
            low = StripAdditionalForDraw(low);

            var m = ReDraw.Match(low);
            if (m.Success)
            {
                var op0 = new EffectOp { Verb = "draw", Source = src, Amount = 1 };
                if (m.Groups[1].Success) op0.Amount = int.Parse(m.Groups[1].Value);
                else if (m.Groups[2].Success) op0.Amount = CountWord(m.Groups[2].Value);
                op0.Tail = tail;
                return op0;
            }

            // `Draw it` / `Draw them` —— 指代前面那条效果指到的那张卡（见 ReDrawRef 的注释）。
            // **必须排在 ReDrawType 前面**。
            var mr = ReDrawRef.Match(low);
            if (mr.Success)
                return new EffectOp { Verb = "drawref", Source = src, Amount = 1, Tail = tail };

            var mt = ReDrawType.Match(low);
            if (mt.Success)
            {
                // 📌 2026-09-13 查过一次「这里会不会把不是兵种的词当兵种」，**结论是不会**，记下来免得再查：
                //   · `Draw a card or Deploy a Crusader` / `Draw a Stratagem or Heal 1 to your units`
                //     —— 这两条**不是独立句子**，是 `Choose one: A; B or C` 里的**选项**，
                //        早在 `TryChooseOne`（0c）就按 `;` 和 ` or ` 拆成三项了，**走不到这儿**。
                //   · `Draw a card for each Dark Pact on it` —— `for each` 层（0a）先把从句剥掉，
                //        剩下 `Draw a card` 由上面的 `ReDraw` 接走。
                //   · `Draw the next Stratagem in your deck and gain 1` —— 它是**单位卡**的 desc，
                //        本正则锚了 `$`（只放行 `from your deck`），**匹配不上**；
                //        而且单位 desc 本来就不进这个解析器（见「266 张单位」那笔账）。
                //   ⇒ 现状**无需**特殊处理。真要加，得先有一条能走到这儿的句子当证据。
                var op = new EffectOp { Verb = "drawtype", Source = src, Amount = 1 };
                if (mt.Groups[1].Success) op.Amount = CountWord(mt.Groups[1].Value);
                op.Payload = mt.Groups[2].Value.Trim();     // 类型词（troop / vehicle…）
                op.Tail = tail;
                return op;
            }
            return null;
        }

        /// <summary>
        /// `Draw it` / `Draw them` —— 把**前面那条效果（通常是选牌）指到的那张卡**抽上手。
        ///
        /// ⚠️ **2026-09-13 修的一个静默错解析**：在此之前它掉进 <see cref="ReDrawType"/>，
        ///    被当成「抽一张**叫 `it` 的兵种**」（`Payload = "it"`）——
        ///    `CreatePool.MatchesKind(c, "it")` 恒为 false，于是翻遍牌库一张都找不到、
        ///    只写一行日志就当无事发生。**而覆盖率的「完全解析」和「载荷有机制」两栏都算它通过**。
        ///    本工程最忌讳的就是这种「看着能跑、其实什么都没做」。
        ///    实测 2 条：`Choose a troop in your deck. Draw it and create a copy of it in your hand`
        ///    （Sautekh `Dimensional Corridor`）、`… Draw it and lower its cost by 3`
        ///    （TauEmpire `Emergency Dispensation`）。
        ///
        /// ⚠️ 本分支必须排在 `ReDrawType` **前面**：`ReDrawType` 的 `([a-z]+)` 会把
        ///    `it` / `them` 一并吃成类型词。
        /// </summary>
        static readonly Regex ReDrawRef = new Regex(
            @"^draws?\s+(it|them)\s*$", RegexOptions.Compiled);
        static readonly Regex ReDraw = new Regex(
            @"^draws?\s+(?:(\d+)|(a|an|two|three))?\s*(?:cards?|card)?\s*$", RegexOptions.Compiled);
        /// <summary>5a 定向翻找：`Draw (N)? (a|an|the)? <类型> (from your deck)?`</summary>
        static readonly Regex ReDrawType = new Regex(
            // 冠词后面的空格要**一起匹配**（`a\s+`）。原来写成 `(?:a|an|the\s+)?` 时只有 `the` 能吃空格，
            // `Draw a troop` 因为 `a` 后面那个空格没人匹配而**整条失配** —— 2026-09-12 撞到：
            // 6 张卡一直躺在「不认识的句子」里，覆盖率白少一截。
            // ⚠️ 数量词也要吃**英文数字**：`Draw two troops and give them +1`（Tide of Muscle）
            //    只认 `(\d+)` 的话，`two` 会被当成类型词，整句失配。2026-09-12 补。
            // 🆕 2026-09-14 A5 批 3：**`the next <类型> in your deck`** 这一式（`Librarian` 的
            //    `Agenda: Draw the next Stratagem in your deck and gain 1 Quest Point` ·
            //    `Slay: Draw the next troop in your deck`）—— 原来 `next` 会被当成类型词、
            //    `in your deck` 又不在后缀表里 ⇒ **整句失配**。
            //    ⚠️ 那一支原来的注释写着「本正则匹配不上，而且单位 desc 本来就不进这个解析器」
            //       —— **后半句是错的**：`Agenda:` / `Slay:` 的**正文**走的就是这个解析器
            //       （`CardDef.AddTriggerOp` → `EffectText.Parse(body)`）。
            @"^draws?\s+(?:(\d+|two|three|four|five)\s+)?(?:a\s+|an\s+|the\s+)?(?:next\s+)?([a-z]+)\s*"
            + @"(?:cards?)?\s*(?:from (?:your|the) deck|in (?:your|the) deck)?$",
            RegexOptions.Compiled);

        /// <summary>
        /// 降费 —— `rule_core.gd:2920`（`Lower the cost of (all )?&lt;X&gt; in your hand( and deck)? by N`）
        /// 与 `:2927`（`(it|they) cost(s)? N less`，指代前面刚回手/刚生成的那张）。
        /// </summary>
        /// <summary>
        /// `costs N more [this turn]` —— **这张卡自己**加价（2026-09-14 A5 批 3 第 1 条）。
        ///
        /// 全池实测**只 1 处**：`Makari the Grot`（Goff）的
        /// `Backlash: Returns to your hand and costs 2 more this turn`。
        /// （另一处含 `costs N more` 的是 `Underground Network`，**有主语**、走 `TryPersistent`
        /// 的 `ReCostMore` 那条，见上面的分支注释。）
        ///
        /// 走 `costmore` 动词，靠 <see cref="SelfCostMoreMarker"/> 与「加给某一类牌」那一支区分
        /// （结算层 `EffectResolver.DoCostMore` 判它）。
        /// </summary>
        static EffectOp TrySelfCostMore(string low, string src)
        {
            var m = ReSelfCostMore.Match(low);
            if (!m.Success) return null;
            return new EffectOp
            {
                Verb = "costmore", Source = src,
                Amount = int.Parse(m.Groups[1].Value),
                Payload = SelfCostMoreMarker,
                Duration = Dur(m.Groups[2]),
                // ⚠️ `Target` **故意留空** —— `DoCostMore` 靠「Payload 是不是哨兵」分流，
                //    带 `Target` 的那一支才是「加给某一类牌」。
            };
        }

        /// <summary>`costs N more [this turn]` —— 组 1 = 几费 · 组 2 = 时长（锚 `^cost`，全池只 1 处）。</summary>
        static readonly Regex ReSelfCostMore = new Regex(
            @"^costs?\s+(\d+)\s+more(?:\s+(this turn|for the rest of this battle))?$",
            RegexOptions.Compiled);

        /// <summary>
        /// <see cref="EffectOp.Payload"/> 的**哨兵值**：这张卡**自己**加价（不是「某一类牌」）。
        /// 与 `(指代上一张)` 同一个套路 —— 用一个不可能和卡名撞车的串当标记。
        /// ⚠️ `public` 是因为结算层 `EffectResolver.DoCostMore` 要拿它分流。
        /// </summary>
        public const string SelfCostMoreMarker = "(这张卡自己)";

        static EffectOp TryLowerCost(string low, string src)
        {
            // `Lower its cost by 2 **and give it Flank**` —— 四条正则都锚了 `$`，
            // 尾巴一出现就整条失配（`Oath 2:` 那句就是这么掉出去的）。先切尾巴。
            string tail = null;
            SplitAndTail(low, out low, out tail);

            // ① `Lower the cost of <谁> by N`
            var m1 = ReLowerCostOf.Match(low);
            if (m1.Success)
            {
                string who = m1.Groups[1].Value.Trim();
                // `Lower the cost of **a random Infantry** in your hand by 1` ——
                // 冠词 `a` 与 `random` 都是**语义**（单数 + 随机挑一张），必须在**解析层**剥掉：
                // 留到结算层就只剩 `IsKindWord` 的「剥首词」那一招，剥完是 `random infantry`，查不到。
                // ⚠️ 只认 `a|an random ` 这一个形状，别放宽 —— 放宽会把 `a random enemy`（打谁）
                //    那类目标短语也吃进来。
                bool pickOne = false;
                var mrand = Regex.Match(who, @"^(?:a|an)\s+random\s+(.+)$", RegexOptions.IgnoreCase);
                if (mrand.Success) { who = mrand.Groups[1].Value.Trim(); pickOne = true; }

                var op = new EffectOp { Verb = "lowercost", Source = src, Payload = who, PickOne = pickOne };
                op.Amount = m1.Groups[2].Success ? int.Parse(m1.Groups[2].Value) : 1;
                op.Duration = Dur(m1.Groups[3]);
                op.Tail = tail;
                return op;
            }
            // ②a `Lower/Reduce its cost **to** N` —— **设为** N 费（2026-09-13 A4）
            //     出处：`Lying in Wait`（Genestealers）`Return a friendly troop to your hand
            //     and reduce its cost to 1`。⚠️ **和「降 N 费」是两回事**：
            //     设为 1 费时，一张 5 费的牌要降 4 —— 用 `Amount` 表达不了（它记的是「降多少」），
            //     所以走新字段 `CostSetTo`，**差值由结算层按当时那张牌的真费用算**。
            var m2a = ReLowerItsCostTo.Match(low);
            if (m2a.Success)
            {
                return new EffectOp
                {
                    Verb = "lowercost", Source = src, CostSetTo = int.Parse(m2a.Groups[1].Value),
                    Payload = "(指代上一张)", Duration = Dur(m2a.Groups[2]), Tail = tail,
                };
            }
            // ② `Lower/Reduce its cost by N` —— 指代**前一句刚回手/刚造出来**的那张
            var m2 = ReLowerItsCost.Match(low);
            if (m2.Success)
            {
                return new EffectOp
                {
                    Verb = "lowercost", Source = src, Amount = int.Parse(m2.Groups[1].Value),
                    Payload = "(指代上一张)", Duration = Dur(m2.Groups[2]), Tail = tail,
                };
            }
            // ③ `(it|they) cost(s) N less [this turn]` —— 同样指代上一张
            var m3 = ReCostLess.Match(low);
            if (m3.Success)
            {
                return new EffectOp
                {
                    Verb = "lowercost", Source = src, Amount = int.Parse(m3.Groups[1].Value),
                    Payload = "(指代上一张)", Duration = Dur(m3.Groups[2]),
                };
            }
            // ④ `Your troops cost N less [this turn]` —— 主语是**一类牌**，不是指代
            var m4 = ReSubjectCostLess.Match(low);
            if (m4.Success)
                return SubjectCostOp(m4.Groups[1].Value, int.Parse(m4.Groups[2].Value), m4.Groups[3].Value, src);

            // ⑤ 🆕 2026-09-14 A5 批 3 第 2 条：`Your next Stratagem this turn **costs 0**`
            //   （`Winged Tyrant`）—— **变成 0 费**，不是「降 0 费」。
            //   和④只差「写 `less` 还是写 `0`」，主语/`next`/时长的剥法**完全同一套**
            //   （都走 `SubjectCostOp`，判据只此一处）。
            var m5 = ReSubjectCostZero.Match(low);
            if (m5.Success)
            {
                var z = SubjectCostOp(m5.Groups[1].Value, 0, m5.Groups[2].Value, src);
                z.CostSetTo = 0;                 // 「变成 0 费」（哨兵是 -1，见 `EffectOp.CostSetTo`）
                return z;
            }
            return null;
        }

        /// <summary>
        /// `ReSubjectCostLess` / `ReSubjectCostZero` 的**共同后半段** —— 主语那一串要剥三样东西：
        ///
        /// 1. **时长写在 `costs` 前面**（`Your next Stratagem **this turn** costs 1 less`）——
        ///    正则的时长组只收「写在 `less` 之后」那种写法。🔴 实测这一族 8 句里**有 5 句**
        ///    是前置写法，而旧代码会把整串 `next troop this turn` 当载荷 ⇒ `MatchesKind`
        ///    （靠「剥首词」）查不到 ⇒ **那 5 句一条都没生效**。这就是第 2 条真正的卡点。
        /// 2. **`next`** ⇒ <see cref="EffectOp.NextOnly"/>（用完即销），并从主语里剥掉 ——
        ///    不剥也能靠「剥首词」凑对，但那就把「下一张」这个语义**静默丢了**
        ///    （结果变成「本回合所有符合条件的牌都便宜」）。
        /// 3. **` you play`**（`The next Vehicle **you play** this turn costs 2 less`）。
        ///
        /// ⚠️ **剥的顺序**：先尾部的时长 → 再头部的 `the`/`your` → 再 `next` → 最后 ` you play`。
        ///    反过来的话 `you play this turn` 会先被时长规则切掉一半。
        /// </summary>
        static EffectOp SubjectCostOp(string subject, int amount, string durGroup, string src)
        {
            string s = (subject ?? "").Trim();
            string dur = DurText(durGroup);

            // ① 时长写在动词**前面**（正则那一组只收「`less` 之后」的写法）
            foreach (var pair in CostDurationSuffixes)
                if (s.EndsWith(pair[0], System.StringComparison.OrdinalIgnoreCase))
                {
                    s = s.Substring(0, s.Length - pair[0].Length).Trim();
                    if (dur.Length == 0) dur = pair[1];
                    break;
                }
            // ② 主语开头残留的 `the` / `your`（`The next Vehicle you play …`；
            //    `your` 大多数时候已被正则的 `(?:your\s+)?` 剥掉，这里兜个底）
            if (s.StartsWith("the ", System.StringComparison.OrdinalIgnoreCase)) s = s.Substring(4).Trim();
            else if (s.StartsWith("your ", System.StringComparison.OrdinalIgnoreCase)) s = s.Substring(5).Trim();
            // ③ `next` ⇒ 一次性
            bool next = false;
            if (s.StartsWith("next ", System.StringComparison.OrdinalIgnoreCase))
            { next = true; s = s.Substring(5).Trim(); }
            // ④ `The next Vehicle **you play** …`
            if (s.EndsWith(" you play", System.StringComparison.OrdinalIgnoreCase))
                s = s.Substring(0, s.Length - " you play".Length).Trim();

            return new EffectOp
            {
                Verb = "lowercost", Source = src,
                Amount = amount,
                Payload = s,
                Duration = dur,
                NextOnly = next,
            };
        }

        /// <summary>时长后缀 → <see cref="EffectOp.Duration"/>。`Dur` 只认「写在 `less` 之后」那一组，
        /// 这一组补「**写在动词前面**」的写法（`… this turn costs 1 less`）。</summary>
        static readonly string[][] CostDurationSuffixes =
        {
            new[] { " this turn", "turn" },
            new[] { " for the rest of this battle", "" },
        };

        /// <summary>
        /// `Lower its Health to N` —— **把当前生命设成 N**（2026-09-13 A4）。
        /// 出处：`Eternal Servitude`（Sautekh）。原版那一支是 `ResolveChangeMaxHealth`，
        /// 但卡面写的是 `Lower **its Health** to 1`（不是「生命上限」）⇒ 我们改**当前生命**。
        /// ⚠️ 只**降**不升（动词是 `lower`）：已经 ≤ N 的**不动**，并如实打日志。
        /// </summary>
        static EffectOp TrySetHealth(string low, string src)
        {
            var m = ReSetHealth.Match(low);
            if (!m.Success) return null;
            return new EffectOp
            {
                Verb = "sethealth", Source = src, Amount = int.Parse(m.Groups[1].Value),
                // 指代**上一条效果打中的那个**（和 `Lower its cost by N` 同一套引用位）
                Target = new EffectTargetSpec { Raw = "(指代上一张)", Side = "prev", Kind = "prev", Count = 1 },
            };
        }
        /// <summary>`Lower its Health to N` —— 组 1 = N</summary>
        static readonly Regex ReSetHealth = new Regex(
            @"^lowers?\s+(?:its|their|the|this unit's|this troop's)\s+health\s+to\s+(\d+)\s*$",
            RegexOptions.Compiled);

        /// <summary>时长后缀 → `EffectOp.Duration`。
        /// ⚠️ `for the rest of this battle` 是**永久**，不是「本回合」—— 两者都非空，
        ///    只看「有没有值」会把永久当成限时（第一次写就是这么错的）。</summary>
        static string Dur(System.Text.RegularExpressions.Group g)
        {
            return g.Success ? DurText(g.Value) : "";
        }

        /// <summary>同上，但收**字符串** —— `SubjectCostOp` 那条路拿不到 `Group`
        /// （时长可能写在 `costs` **前面**）。判据仍然只此一处。</summary>
        static string DurText(string v)
        {
            if (string.IsNullOrEmpty(v)) return "";
            return v.IndexOf("this turn", System.StringComparison.Ordinal) >= 0 ? "turn" : "";
        }
        // ⚠️ **四条分开写，每条各自 anchored**，别合并成一条大正则。
        //    第一版把 `of?` 当成「`of` 里的 f 可选」写进一条大正则，于是
        //    `Lower the cost of all Vehicles…` 的 payload 被切成了 `f all vehicles…`
        //    —— 而且**照样算解析成功**（静默的错解析，本工程最忌讳的那种）。
        //    下面每条的组 1/2/3 含义写在各自注释里；`TestLowerCostParsing` 逐条钉着。
        /// <summary>`Lower the cost of <谁> [by N] [this turn]` —— 1=谁 · 2=几费 · 3=时长</summary>
        static readonly Regex ReLowerCostOf = new Regex(
            @"^lower\s+(?:the\s+)?cost\s+of\s+(.+?)(?:\s+by\s+(\d+))?"
            + @"(?:\s+(this turn|for the rest of this battle))?$", RegexOptions.Compiled);
        /// <summary>`Lower/Reduce its cost by N` —— 1=几费 · 2=时长（指代上一张）</summary>
        static readonly Regex ReLowerItsCost = new Regex(
            @"^(?:lower|reduce)s?\s+(?:its|their|the)\s+cost\s+by\s+(\d+)"
            + @"(?:\s+(this turn|for the rest of this battle))?$", RegexOptions.Compiled);
        /// <summary>`Lower/Reduce its cost **to** N` —— 1=N · 2=时长（指代上一张，2026-09-13 A4 加）</summary>
        static readonly Regex ReLowerItsCostTo = new Regex(
            @"^(?:lower|reduce)s?\s+(?:its|their|the)\s+cost\s+to\s+(\d+)"
            + @"(?:\s+(this turn|for the rest of this battle))?$", RegexOptions.Compiled);
        /// <summary>`(it|they) cost(s) N less` —— 1=几费 · 2=时长</summary>
        static readonly Regex ReCostLess = new Regex(
            @"^(?:it|they)\s+costs?\s+(\d+)\s+less"
            + @"(?:\s+(this turn|for the rest of this battle))?$", RegexOptions.Compiled);
        /// <summary>`Your troops cost 1 less this turn` —— 1=主语 · 2=几费 · 3=时长
        /// ⚠️ 主语里可能**还带着时长和 `next`**（`Your next Stratagem **this turn** costs 1 less`）——
        /// 那两样由 <see cref="SubjectCostOp"/> 剥，正则管不着。</summary>
        static readonly Regex ReSubjectCostLess = new Regex(
            @"^(?:your\s+)?(.+?)\s+costs?\s+(\d+)\s+less"
            + @"(?:\s+(this turn|for the rest of this battle))?$", RegexOptions.Compiled);

        /// <summary>🆕 `Your next Stratagem this turn **costs 0**`（2026-09-14 A5 批 3 第 2 条）——
        /// 1=主语 · 2=时长。**变成 0 费**，与 `… costs N less` 分开写一条正则
        /// （`less` 那一条的组 2 是数字、这条没有数字，硬合并会把「几费」这个语义搅乱）。</summary>
        static readonly Regex ReSubjectCostZero = new Regex(
            @"^(?:your\s+)?(.+?)\s+costs?\s+0"
            + @"(?:\s+(this turn|for the rest of this battle))?$", RegexOptions.Compiled);

        /// <summary>`Reanimate a friendly Remnant` —— 从墓地把单位捞回场上（原版在 `_resolve_tactic` 族）。</summary>
        static EffectOp TryReanimate(string low, string src)
        {
            var m = ReReanimate.Match(low);
            if (!m.Success) return null;
            var op = new EffectOp { Verb = "reanimate", Source = src };
            string tok = m.Groups[1].Value.Trim();
            op.Target = IsPronoun(tok) ? null : ParseTarget(tok);
            return op;
        }
        static readonly Regex ReReanimate = new Regex(@"^reanimates?\s+(.+)$", RegexOptions.Compiled);

        /// <summary>
        /// `Repeat this effect`（`rule_core.gd:2542` → `_resolve_repeat`）：把**本句之前的效果**再来一遍。
        /// 三种变体：纯重复 / 付费重复 / 条件重复（`If any troop dies, repeat this effect`）。
        /// `Payload` 存重复的条件原文（空 = 无条件）。
        /// </summary>
        static EffectOp TryRepeat(string low, string src)
        {
            int idx = low.IndexOf("repeat this effect");
            if (idx < 0)
            {
                // `Repeat for each enemy unit`（`Stormboyz Strike`）—— 裸 `Repeat` 也是同一个意思：
                // 这个动词只有一种含义（把前面那段再来一遍），所以认它不冒险。
                // `for each` 从句由上层 `TryForEach` 先剥走了，这里拿到的就是光秃秃的 `Repeat`。
                string t = low.Trim().TrimEnd('.');
                if (t != "repeat" && t != "repeat this" && t != "repeat it") return null;
                return new EffectOp { Verb = "repeat", Source = src, Amount = 1 };
            }
            var op = new EffectOp { Verb = "repeat", Source = src, Amount = 1 };
            string before = low.Substring(0, idx).Trim().TrimEnd(',', ';');
            if (before.Length > 0) op.Payload = before;    // 条件（`If any troop dies` / `for every friendly Vehicle`）
            return op;
        }

        /// <summary>
        /// **付费激活前缀**（`rule_core.gd:1549` 那族：「付不起就**整段不激活**」）。
        /// 组 3 = 正文（前缀剥掉后继续走 handler 管线）。
        ///
        /// 三种写法（都对着卡池里的真卡核过）：
        ///   · `12 [Energy]: Draw 3 cards` —— 带方括号 + 货币词（`Relics of Saint Katherine`）
        ///   · `5 Energy: Draw a card`      —— **无方括号**（`Moment of Grace`）
        ///   · `4 : Give +2 …`              —— 无货币词，只按能量算（`Devout Warriors`）
        ///
        /// 🔴 **2026-09-13 第三十二轮扩宽**（派子代理查 47 条「完全不认识」时定位出来的）：
        ///    这个正则原来把**方括号写成必选**（`\[\s*energy\s*\]`），而 `rule_core.gd:1549` 里
        ///    是 `\[?[Ee]nergy\]?` —— **括号可选**。⇒ `5 Energy: Draw a card`（无括号）被整句判不认识。
        ///    `rule_core` 那边**早就写对了**，是我们抄窄了。
        ///
        /// ⚠️ **组号用命名分组锁死**（`n` / `cur` / `body`）：这份正则被后续改动扩过好几次，
        ///    每加一个捕获括号，后面所有 `Groups[i]` 就**整体错位** —— 而且**编译得过**，
        ///    只有运行时取到错的那一组才看得出来。命名分组把这件事钉死。
        ///
        /// ⚠️ **`(N) …` 那种写法仍不收**（`(5) Reduce their cost by 5` / `(1) Draw a card`）：
        ///    `rule_core.gd:1549` 确实有 `\((\d+)\)` 分支，但那只认**紧跟着货币词**的括号式
        ///    （`(1) [Energy]: X`）。裸的 `(5) Reduce…` 判不出是「费用」还是「序数/编号」——
        ///    **判不出就不猜**（本工程的规矩）。等有第三张证据再说。
        /// </summary>
        // ⚠️ **2026-09-13 第三十三轮扩过一次**（原来只认光秃秃的单词）：
        //   ① **方括号里可以带修饰词** —— 实测卡面有 `[Faith Icon]`（`Paragon Warsuit`），
        //      原来的 `faith` 后面紧跟 `]` 才认，于是整段失配。
        //   ② **要认图标字形 `☀`** —— 修女会的信仰在卡面上就是那个太阳，实测有 `2 ☀:` / `6☀:` / `4 ☀:`
        //      共 3 处（`Canoness` / `Celestian Superior` / `Divine Intervention`），原来一律不认。
        //   ⇒ 改成「方括号里任意词」或「已知单词/图标」，**归一化交给 `CostKindOf`**（判据只那一处）。
        // ⚠️ **2026-09-13 A4 又扩一次：`(5)` 也算数**（`(?<nParen>)` 那一支）——
        //    卡图实据（主对话亲读，铁律 7）：`Reclaim the Stars` 卡面印的是
        //    `Draw 5 cards. ❺ Reduce their cost by 5`，**那个 `(5)` 是绿圈形能量图标**，
        //    不是括号数字。`Storm of Silence` 的 `(1) Energy: …` 是同一个图标的另一种 OCR 写法。
        //    ⇒ 原来那句「裸的 `(5)` 判不出是费用还是序数 ⇒ 判不出就不猜」**已经解了**（有卡图证据）。
        //    ⚠️ 无冒号那种（`1 Also give it Shield`）走下面的 `RePaidBare`，**不在这条正则里** ——
        //       无冒号必须要求后面紧跟**动词**，否则 `Draw 5 cards` 这种会被吃掉（见那条注释）。
        //    ⚠️ 2026-09-14 A5 批 4：补 `faith icon` 这一支 —— `Paragon Warsuit` 卡面写的是
        //       `6 [Faith Icon]: Gain Vanguard`，而 `[` `]` 在 `ParseSegment` 开头就被剥掉了
        //       （`s.Replace("[","").Replace("]","")`）⇒ 送进来的是 `6 faith icon: gain vanguard`，
        //       只写到 `faith` 的话后面那个 ` Icon` 会让整条正则失配 ⇒
        //       **前缀没剥掉、付费也没记上**，句子退化成一条「凭空给 Vanguard」的假解析（半懂）。
        //       ⚠️ 必须排在 `faith` **前面**（`faith\s+icon` 更长、更具体）。
        static readonly Regex RePaid = new Regex(
            @"^(?:\((?<nParen>\d+)\)|(?<n>\d+))\s*(?:\[\s*(?<curB>[^\]\n]{1,16})\s*\]|" +
            @"(?<curW>energy|faith\s+icon|faith|spirit(?:\s+stones?)?|might|attack|health|icon|☀|⚔|🛡|🔫))?\s*" +
            @":\s*(?<body>.+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// **没有冒号**的付费前缀：`1 Also give it Shield` · `(1) Draw a card` · `6 ☀ Give them Flank`。
        ///
        /// 出处（**卡图实据**，2026-09-13 A4 亲读）：
        ///   · `Forewarned` 卡面 `… this turn. **❶** Also give it 👁Shield`（`Aeldari/4计策/…18.49.42.png`）
        ///   · `Will of Asuryan` 卡面 `Give 🛡Vanguard to a friendly troop. **❶** Draw a card`
        ///   · `Sacred Rose` 卡面 `Deploy two Sacred Rose Sister. **6 ☀** Give them ⤴Flank`（太阳=信仰）
        ///   ⇒ 那个数字是**绿圈（能量）或太阳（信仰）图标**，OCR 抄成了裸数字/括号数字。
        ///   原版**印得下就不印冒号**，所以「有冒号才认」会漏掉一整族。
        ///
        /// 🔴 **要求后面紧跟动词**（<see cref="PaidVerbs"/>）：无冒号的形式**没有分隔符**，
        ///   宽松匹配会吃掉正常句子 —— 例如 `Draw **5 cards**` 里的 `5 cards`、
        ///   `Deploy 3 Grot` 的 `3 Grot`。判据是「数字后面那个词**是动词**」，别的一律不认。
        ///   全池仿真（2026-09-13）：这条规则只多吃 11 条句子，**全是合法写法**。
        /// </summary>
        static readonly Regex RePaidBare = new Regex(
            @"^(?:\((?<nParen>\d+)\)|(?<n>\d+))\s+" +
            @"(?:(?<curB>\[\s*[^\]\n]{1,16}\s*\]|energy|faith|spirit(?:\s+stones?)?|icon|☀|might)\s+)?" +
            // ⚠️ **动词表必须包在 `(?:…)` 里**：不包的话 `\b.+` 只绑到**最后一个**分支
            //    （`|` 优先级最低）⇒ 只有 `attack` 后面容许有别的词，其余动词一律整条失配。
            //    第一版就是这么写的，表现是「`1 Repeat this effect` 仍然认不出、而且不带 cost」
            //    —— 2026-09-13 用探针逐条量出来的，别把括号去掉。
            @"(?<body>(?:" + PaidVerbs + @")\b.+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>付费前缀后面**必须**是这些动词之一（见 <see cref="RePaidBare"/> 的注释）。</summary>
        const string PaidVerbs =
            @"give|draw|deal|heal|deploy|create|return|reduce|destroy|stun|gain|discard|take|lower"
            + @"|choose|put|add|repeat|refill|spend|increase|reload|shuffle|make|double|target|attack";

        /// <summary>
        /// 付费前缀里的「货币名」→ **规范取值**。**判据只此一处**（解析与结算都问它）。
        ///
        /// 规范取值：`energy` / `faith` / `spirit` / `oath` / `"""`（空 = 没写货币，按能量算）。
        ///
        /// 🔴 **2026-09-14：这张词表和上面两条正则的「货币词」必须同步 —— 它们是两个地方，**
        ///   **而不同步的表现是静默的。** 实测踩到：`RePaid` 原来只写 `spirit stones?`，
        ///   而这里认的是 `Contains("spirit")` ⇒ 只写 `[spirit]` 时**正则整条失配**、
        ///   前缀**根本没被捕获**：轻则整句判「不认」，重则被后一条正则吃掉当成**载荷**
        ///   （`2 [spirit]: Repeat this effect` → `载荷「2 spirit:」`，还报「认了」）。
        ///   ⚠️ 而且 `[` `]` 在 `ParseSegment` 里**先被剥掉**才送进来（见 `RePaid` 上面那段），
        ///      所以这里要写**剥掉括号之后的裸词** —— `[Spirit Stone]` 送进来是 `Spirit Stone`。
        ///   ⇒ **往这里加一种货币时，顺手把 `RePaid` / `RePaidBare` 两个可选分支一起加**。
        /// 出处：`ManaType`（原版只有 `Normal` 与 `SpiritStone` 两种**货币**）+ 规则书 `:184`
        /// （信仰是**阈值**不是货币，但卡面 `8 [Faith]: …` 确实是「付 8 点信仰才激活」的写法 ——
        /// 用户口径「达到阈值时部分卡牌获得更强的效果」说的就是它）。
        ///
        /// ⚠️ **`icon` → `faith`**（2026-09-13 A4）：`[Icon]` 是 OCR **认不出图标**时的占位。
        ///   实测卡池**只有 3 处**，**三处全是修女会**：`Sister Novitiate` 的 `3 [Icon]: Gain +1 Health` ·
        ///   `Blade of Faith` 的 `4 [Icon]: Repeat this effect` · `Sacred Rose` 的 `6 [icon] Give them Flank`。
        ///   卡图实据：`Sister Novitiate` / `Sacred Rose` 那两处印的都是**太阳**（修女会的信仰）⇒ 判成 `faith`。
        ///   ⚠️ **这是「按证据收窄」不是通例**：哪天出现**非修女会**的 `[icon]`，这条映射要重核
        ///      （别的阵营的绿色圆框就是能量）。
        /// </summary>
        public static string CostKindOf(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string s = raw.Trim().ToLowerInvariant().Replace("]", "").Trim();
            if (s.Length == 0) return "";
            if (s.Contains("faith") || s == "☀") return "faith";
            if (s.Contains("icon")) return "faith";        // ⚠️ 见上面那段实据，只对那三张成立
            if (s.Contains("spirit")) return "spirit";
            if (s.Contains("energy")) return "energy";
            return s;              // `might` / `attack` / `health` …原样留着（结算层按未知处理）
        }

        /// <summary>`… equal to your Faith` / `… equal to your Spirit Stones`（句尾）。组 1 = 资源名。</summary>
        static readonly Regex ReEqualResource = new Regex(
            @"\s+equal\s+to\s+your\s+(faith|spirit\s+stones?)\s*[.!]?\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>`Refill N Energy` / `Refill Energy` —— `rule_core.gd:2952`、`:2956`。</summary>
        static EffectOp TryRefill(string low, string src)
        {
            if (!ReRefill.IsMatch(low)) return null;
            var op = new EffectOp { Verb = "refill", Source = src };
            var m = Regex.Match(low, @"refills?\s+(\d+)");
            op.Amount = m.Success ? int.Parse(m.Groups[1].Value) : 0;   // 0 = 回满
            return op;
        }
        static readonly Regex ReRefill = new Regex(@"^refill", RegexOptions.Compiled);

        /// <summary>
        /// `Lower cost by 1 when &lt;事件&gt;` —— **事件触发式降费**的那一句（2026-09-13 A4）。
        ///
        /// 这一族**卡牌在手上时就开始监听**（原版 `PlayerHand.SetupCardInHand`，
        /// 见 `CardDef.CostWhens`），所以这句话在这里**没有可结算的东西** ——
        /// 它只需要「解析得出来」（`IsFullyParsed` 判它，三处共用：能不能打 / 能不能进卡组 / 卡面打不打 `*`）。
        /// 注册监听器本身由 `CardDef.AddWhenTrigger` 的 ② 支在卡表加载时做完（**两处各管一半**）。
        /// </summary>
        static EffectOp TryCostWhenStub(string low, string src)
        {
            // 判据和 `CardDef.AddWhenTrigger` 的 ② 支**同形**（那句正则）：`lower cost by N when <事件>`
            var m = Regex.Match(low, @"^lower\s+cost\s+by\s+(\d+)\s+when\s+(.+)$",
                                RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            return new EffectOp { Verb = "costwhen", Source = src, Amount = int.Parse(m.Groups[1].Value) };
        }

        /// <summary>
        /// `This costs N less if you control a unit with &lt;关键词&gt;` —— **静态条件降费**（2026-09-13 A4 批 1）。
        ///
        /// 出处：`Fate Inescapable`（SaimHann）· 规格书 `rule_core.gd:2112` 的 `re_this_less`
        /// （`"this costs (\\d+) less if you control (?:a|an)? ?(?:unit|troop)? with ([a-z]+)"`）。
        ///
        /// 和 `Lower cost by N when &lt;事件&gt;`（`Verb = "costwhen"`）**不是一件事**，别合并：
        ///   · 那个是**事件**触发的一次性降价（发生那一下登记一条修正）；
        ///   · 这个是**常驻条件**（只要场上还站着那样的单位就一直便宜，人没了就恢复）。
        /// ⇒ 它**没有可结算的东西**，和 `costwhen` 一样只要「解析得出来」；
        ///    真正的判据挂在 `RuleCore.CostOf`（读 `CardDef.CostIfControl`）。
        /// ⚠️ 折扣与关键词都记在 op 上，好让 `CardDef` 那半边**不用再解一遍文本**。
        /// </summary>
        static EffectOp TryCostIfControl(string low, string src)
        {
            var m = Regex.Match(low,
                @"^this\s+costs?\s+(\d+)\s+less\s+if\s+you\s+control\s+(?:a|an)?\s*(?:unit|troop|card)?\s*with\s+([a-z][a-z0-9' \-]*)$",
                RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            return new EffectOp
            {
                Verb = "costifcontrol", Source = src,
                Amount = int.Parse(m.Groups[1].Value),
                Payload = m.Groups[2].Value.Trim().ToLowerInvariant(),
            };
        }

        /// <summary>
        /// `Does nothing` —— **原版设计就是「什么都不做」**（2026-09-13 A4）。
        /// 出处：`Improvised Barricade`（Genestealers）—— 规则书 `:204` 说「破坏」是塞进对手手牌的假卡，
        /// 这张就是纯空效果。**没收下来时它报「不认识」**，而它其实是**故意的空**，
        /// 两种状态在诊断单上长得一样 ⇒ 收下来，让「认识但没事可做」和「不认识」分开。
        /// ⚠️ 红线（不许静默失败）仍然照办：结算时**打一行日志**说明这张卡本来就什么都不做。
        /// </summary>
        static EffectOp TryNoEffect(string low, string src)
        {
            if (low != "does nothing") return null;
            return new EffectOp { Verb = "noeffect", Source = src };
        }

        /// <summary>
        /// `Reload the Duty abilities of all your units` —— 2026-09-13 A4。
        /// 规则书 `:181`「职责（Duty）：**一次性能力，可被「装填」再次使用**」。
        /// 语义照 `rule_core.gd:2912-2931`（一字不差）：把己方带 `duty` 的单位的 `DutyUsed` 复位。
        /// ⚠️ 两种宾语：`all your units`（全体带 duty 的己方单位）与省略宾语的（指代前一句那个目标，
        /// 由 `DoReloadDuty` 从 `LastTarget` 取）。判据写在 `DoReloadDuty` 里，只此一处。
        /// </summary>
        static EffectOp TryReloadDuty(string low, string src)
        {
            if (!low.StartsWith("reload")) return null;
            if (low.IndexOf("duty", System.StringComparison.Ordinal) < 0) return null;
            bool all = low.Contains("all your") || low.Contains("all friendly");
            return new EffectOp
            {
                Verb = "reloadduty", Source = src,
                Payload = all ? "all" : "prev",     // 判据给结算层用（别去 match `Raw` 那句人话）
                Target = new EffectTargetSpec
                {
                    Raw = all ? "(所有带 duty 的己方单位)" : "(指代上一张)",
                    Side = "own", Kind = "unit", Count = 0, Auto = true,
                },
            };
        }

        /// <summary>
        /// `Deploy …` —— **免费把单位放进场上**（不花能量、不占手牌）。`rule_core.gd:2970` 那一支。
        ///
        /// 实测全卡池 **50 个分句 / 49 种写法**，形如：
        ///   · `Deploy a Battle Sister` / `Deploy two Storm Guardian`        具名卡
        ///   · `Deploy 8 random Ork Infantry that cost 4 or less`           阵营 + 兵种 + 费用
        ///   · `Deploy 4 random troops from your deck`                      从牌库
        ///   · `Deploy up to 5 friendly Infantry troops that died this game` 从弃牌堆
        ///   · `Deploy 3 Grot and give them Vanguard`                       接 `and` 尾句
        ///   · `Deploy one Battle Sister for each enemy unit`               `for each`（上层已剥）
        ///
        /// 产物：`Amount` 张数 · `Payload` 部署什么 · `DeployFrom` 从哪儿 ·
        /// `CostMin`/`CostMax` 费用区间 · `Tail` 后续分句。
        ///
        /// **`2-cost` 是「恰好 2 费」，不是「≤2 费」** —— 三条实证，不是猜的：
        ///   ① `Deploy 2 random 2-cost Genestealer Cults troops`：恰好 2 费 = **5 张**，
        ///      对上附录 C「召唤教派/变形偶像持旗者（**1d5**）」的 5 个名字；≤2 费会多出 3 张 1 费卡。
        ///   ② `Deploy 3 random 2-cost Sautekh troops`：恰好 2 费 = **6 张**，
        ///      对上附录 B「次元裂隙：3 个随机 2 费部队（**6 种**，各 3 张，上限 36）」；≤2 费是 7 张。
        ///   ③ `Deploy 4 random 2-cost Leviathan troops`：恰好 2 费 = 6 张，对上附录 C
        ///      「空中播种/泰伦入侵（1d5）」那 5 个名字 + 多一个 `Neurogaunt`（同「虫群大军」那类的差）。
        /// ⇒ `that cost N or less` / `that costs N or more` 才是显式的区间写法，两种分开处理。
        /// </summary>
        /// <summary>
        /// `Each player deploys N &lt;什么&gt; from their deck` —— **双方各部署一次**
        /// （`Birth of a Saga`，SpaceWolves；2026-09-13 A4 批 1）。
        ///
        /// 做法：把 `their` 换回 `your` **复用 `TryDeploy` 那一份解析**（张数 / 兵种 / 费用区间
        /// 全都自动支持），再打上 <see cref="EffectOp.EachPlayer"/> 标记。
        /// **一卡一行**，别为它另写一套词表 —— 两处写同一条规则迟早不一致。
        /// </summary>
        static EffectOp TryDeployEachPlayer(string low, string src)
        {
            var m = Regex.Match(low,
                @"^each\s+player\s+deploys?\s+(.+?)\s+from\s+(?:their|his|her)\s+deck$",
                RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            var op = TryDeploy("deploy " + m.Groups[1].Value.Trim() + " from your deck", src);
            if (op == null) return null;
            op.EachPlayer = true;
            return op;
        }

        static EffectOp TryDeploy(string low, string src)
        {
            var m = ReDeploy.Match(low);
            if (!m.Success) return null;
            var op = new EffectOp { Verb = "deploy", Source = src, Amount = 1 };
            string body = m.Groups[1].Value.Trim();

            // ---- `and <另一句>` 尾句（`Deploy 3 Grot and give them Vanguard`）----
            // 走和 deal/give 同一个判据（`and` 后面那截是不是动词开头）——
            // ⚠️ 不能丢：丢了就是「部署了但没给 Vanguard」的静默失效。
            SplitAndTail(body, out body, out op.Tail);

            // ---- `up to N`（`Deploy up to 5 friendly Infantry troops…`）----
            // 「至多」= 牌不够就有几张算几张，结算层按池子大小收口（**不循环重来**）
            if (body.StartsWith("up to ")) { op.UpTo = true; body = body.Substring(6).Trim(); }

            // ---- 数量前缀 ----
            var mc = Regex.Match(body, @"^(\d+|one|two|three|four|five|six|seven|eight|a|an)\b");
            if (mc.Success)
            {
                op.Amount = CountWord(mc.Groups[1].Value);
                body = body.Substring(mc.Groups[1].Length).Trim();
            }
            if (body.Length == 0) return null;

            if (body.StartsWith("random ")) { op.Random = true; body = body.Substring(7).Trim(); }
            if (body.Length == 0) return null;

            // ---- 费用区间 ----
            // `that cost 4 or less` / `that costs 6 or more`（显式区间）
            var lim = ReCostLimit.Match(body);
            if (lim.Success)
            {
                int v = int.Parse(lim.Groups[1].Value);
                if (lim.Groups[2].Value == "less") op.CostMax = v; else op.CostMin = v;
                body = body.Substring(0, lim.Index).Trim();
            }
            // `2-cost`（**恰好** N 费 —— 实证见函数头）
            var pre = ReCostExactly.Match(body);
            if (pre.Success)
            {
                int v = int.Parse(pre.Groups[1].Value);
                op.CostMin = v; op.CostMax = v;
                body = body.Substring(pre.Length).Trim();
            }
            if (body.Length == 0) return null;

            // ---- 从哪儿来 ----
            if (body.EndsWith(" from your deck"))
            {
                op.DeployFrom = "deck";
                body = body.Substring(0, body.Length - " from your deck".Length).Trim();
            }
            else if (body.EndsWith(" that died this game"))
            {
                op.DeployFrom = "graveyard";
                body = body.Substring(0, body.Length - " that died this game".Length).Trim();
            }
            // `friendly` 只是「自己这边」的措辞（部署永远是自己的），不参与筛卡
            body = Regex.Replace(body, @"^friendly\s+", "").Trim();
            if (body.Length == 0) return null;

            op.Payload = body;
            return op;
        }
        static readonly Regex ReDeploy = new Regex(@"^deploys?\s+(.+)$", RegexOptions.Compiled);
        /// <summary>`that cost 4 or less` / `that costs 6 or more`</summary>
        static readonly Regex ReCostLimit = new Regex(
            @"\s*that costs?\s+(\d+)\s+or\s+(less|more)$", RegexOptions.Compiled);
        /// <summary>`2-cost ` 前缀（**恰好** N 费）</summary>
        static readonly Regex ReCostExactly = new Regex(@"^(\d+)-cost\s+", RegexOptions.Compiled);

        /// <summary>
        /// `Create …` —— **造牌**：凭空把卡放进手牌 / 牌库顶（不是从牌库抽，也不是从墓地捞）。
        ///
        /// 实测全卡池 **28 个分句 / 20 张卡**（`_tmp_view/tactic_unparsed.txt` 第 ① 栏最大的一块）。
        /// 三种写法：
        ///   ① `Create <数量> [random] <造什么> in your hand`      ← 绝大多数
        ///   ② `Create in your hand a Gun Drone, Guardian Drone or Marker Drone`  ← 目的地**前置**
        ///   ③ `Create a copy of <卡名|it> at the top of your deck` ← 复制
        ///
        /// 产物：`Amount` = 张数、`Payload` = 「造什么」原文（小写）、<see cref="EffectOp.Dest"/> = 去哪儿。
        ///
        /// ⚠️ **这里不查卡池**。「造什么」是不是真存在、候选池有几张，都要等结算层
        ///    （`ctx.CardPool` 在那儿）—— 解析器是纯函数，`EffectText.Coverage` 靠这一点
        ///    才能「只解析、不动状态」地量覆盖率。查池子是 `CreatePool.Resolve` 的事。
        ///
        /// ⚠️ **没写目的地就判失败**（不默认成手牌）—— 猜错目的地 = 把牌送错人，那是最难查的一类。
        /// </summary>
        static EffectOp TryCreate(string low, string src)
        {
            var m = ReCreate.Match(low);
            if (!m.Success) return null;

            string body = m.Groups[1].Value.Trim();

            // ---- 目的地（三种写法实测见函数头）----
            // 取**最先出现**的那一个：一个分句里只有一个目的地，
            // 写法 ② 的目的地在「造什么」前面，取最后一个反而会漏。
            string dest = null;
            int at = -1, len = 0;
            foreach (var pair in DestPhrases)
            {
                int i = body.IndexOf(pair[0], System.StringComparison.Ordinal);
                if (i < 0) continue;
                if (at < 0 || i < at) { at = i; len = pair[0].Length; dest = pair[1]; }
            }
            if (dest == null) return null;              // 不知道送哪儿 —— 判失败，别猜

            string what = (body.Substring(0, at) + " " + body.Substring(at + len)).Trim();
            if (what.Length == 0) return null;

            // ---- 数量前缀 ----
            // `one` / `a` / `an` / `two` / `three` / `3` …
            // ⚠️ 必须在剥目的地**之后**剥数量：写法 ② 是 `create in your hand a Gun Drone…`。
            int amount = 1;
            var mc = Regex.Match(what, @"^(\d+|one|two|three|four|five|a|an)\b");
            if (mc.Success)
            {
                amount = CountWord(mc.Groups[1].Value);
                what = what.Substring(mc.Groups[1].Length).Trim();
            }
            if (what.Length == 0) return null;

            return new EffectOp
            {
                Verb = "create",
                Source = src,
                Amount = amount,
                Payload = what,
                Dest = dest,
            };
        }
        /// <summary>
        /// `Return X to your hand / to the top of their deck / to your deck` —— 把**场上**的卡
        /// 挪回手牌或牌库。
        ///
        /// **语义出处（权威）**：规则书**英文原版 `:455-457`**「Cards Sent into the Deck」——
        ///   &gt; Cards returned to the deck from the battlefield or generated and added to the deck
        ///   &gt; should be **shuffled in unless otherwise stated by a card effect.**
        /// ⇒ 写 `to the top of their deck` 属于「另有说明」→ **放牌库顶、不洗**；
        ///   写 `to your deck` 没写「顶」→ **洗入**。`DoReturn` 照这个来。
        ///
        /// 实测全仓只有 **6 个分句**用这个动词，目的地写法 4 种：
        ///   `Return a friendly troop to your hand`（UM `Fall Back` · SaimHann `Fire and Fade`）·
        ///   `Return a friendly Vehicle to your hand`（DarkAngels `Master of Manoeuvre`）·
        ///   `Return a friendly troop and a random enemy troop to the top of their deck`
        ///   （DarkAngels `Covert Operation`）·
        ///   另两条 Genestealers 的后面还挂着半句（见下）。
        ///
        /// ⚠️ **目的地要求「全等」，不是「包含」**：
        ///    `Return a friendly troop to your hand **and reduce its cost to 1**`（Genestealers）
        ///    这种整句**如实判不认识**。按「包含」匹配会把后半句**静默吞掉** ——
        ///    那张卡现在确实还没做，宁可报出「不认识」，也不能装作只做了一半。
        /// </summary>
        static EffectOp TryReturn(string low, string src)
        {
            // ⚠️ **`and <另一句>` 的尾巴先切下来**（2026-09-13 A4）：
            //    `Return a friendly troop to your hand **and reduce its cost to 1**`（`Lying in Wait`）——
            //    不切的话目的地那一栏读到的是 `your hand and reduce its cost to 1`，
            //    全等匹配对不上 ⇒ 整句认不出 ⇒ **整张卡打不出去**（`ErrUnimplemented`）。
            //    尾巴由调用方的 `Finish` 递归解（和 `give` / `draw` / `lowercost` 同一套）。
            string tail = null;
            SplitAndTail(low, out low, out tail);

            // ---- 🆕 **没写主语的 `Returns to <目的地>` = 这张卡自己回手**（2026-09-14 A5 批 3）----
            //   全池 2 处：`Grot Orderly` 的 `At the start of your turn, return to your hand` ·
            //   `Backlash: Returns to your hand and costs 2 more this turn`。
            //   口径与 `Heal N` 没写目标 = 自愈 / `Takes N damage` 没写主语 = 就是它自己
            //   **完全同一条**（`EffectTargetSpec.Subjectless`；结算层 `DoReturn` 直接读 `op.Target`）。
            //   ⚠️ 要排在 `ReReturn` **之前**：那条要求 `<谁> to <目的地>`，这里的「谁」压根没写。
            var mb = ReReturnBare.Match(low);
            if (mb.Success)
            {
                string destTextB = mb.Groups[1].Value.Trim().TrimEnd('.', ' ');
                string destB = null;
                foreach (var pair in ReturnDests)
                    if (destTextB == pair[0]) { destB = pair[1]; break; }
                if (destB != null)
                    return new EffectOp
                    {
                        Verb = "return", Source = src, Dest = destB, Tail = tail,
                        Target = new EffectTargetSpec
                        {
                            Raw = "(未写主语：有施放者就是施放者自己，否则己方全体)",
                            Side = "own", Kind = "unit", Count = 0, Auto = true, Subjectless = true,
                        },
                    };
            }

            var m = ReReturn.Match(low);
            if (!m.Success) return null;

            string who = m.Groups[2].Value.Trim();
            string destText = m.Groups[3].Value.Trim().TrimEnd('.', ' ');
            if (who.Length == 0) return null;

            string dest = null;
            foreach (var pair in ReturnDests)
                if (destText == pair[0]) { dest = pair[1]; break; }
            if (dest == null) return null;              // 目的地不纯 → 判不认识，绝不猜

            var op = new EffectOp { Verb = "return", Source = src, Payload = who, Dest = dest, Tail = tail };
            if (m.Groups[1].Success) { op.Amount = int.Parse(m.Groups[1].Value); op.UpTo = true; }
            // 单目标时把 `Target` 也填上 —— 表现层靠它决定高亮哪边棋盘（`EffectText.PickSide`）。
            // 两个目标的（`… and a random enemy troop …`）留空，由 `DoReturn` 自己拆 Payload。
            if (who.IndexOf(" and ", System.StringComparison.Ordinal) < 0)
                op.Target = ParseTarget(who);
            return op;
        }

        /// <summary>`return [up to N] &lt;谁&gt; to &lt;目的地&gt;` —— 1=至多几张 · 2=谁 · 3=目的地。</summary>
        static readonly Regex ReReturn = new Regex(
            @"^returns?\s+(?:up to\s+(\d+)\s+)?(.+?)\s+to\s+(.+)$", RegexOptions.Compiled);

        /// <summary>**没写主语**的 `Returns to &lt;目的地&gt;` —— 1=目的地。
        /// 全池 2 处（`Grot Orderly` / `Backlash:` 正文），见 <see cref="TryReturn"/>。</summary>
        static readonly Regex ReReturnBare = new Regex(
            @"^returns?\s+to\s+(.+)$", RegexOptions.Compiled);

        /// <summary>目的地原文 → 规范名。**全等匹配**（不是包含）：漏掉一个半句就判不认识。</summary>
        static readonly string[][] ReturnDests =
        {
            new[] { "your hand",             "hand" },
            new[] { "their hand",            "hand" },
            new[] { "its owner's hand",      "hand" },
            new[] { "the top of their deck", "decktop" },
            new[] { "the top of your deck",  "decktop" },
            new[] { "their deck",            "deck" },
            new[] { "your deck",             "deck" },
        };

        /// <summary>
        /// `create` 造牌 —— 两条动词：**`Create …`** 和 **`Add …`**（后者只出现在
        /// `If &lt;条件&gt;, …` 的正文里，实测全仓 **0 条**独立分句以 `add` 开头，
        /// 所以扩这条不会碰到别的句型）。
        ///
        /// ⚠️ 2026-09-13 补 `adds?`：`If target dies, add Extermination Protocol to your hand`
        /// （Sautekh `Awakening Obelisk`）之前整句解析不了 —— `TryIf` 把条件剥得干干净净，
        /// 卡在正文那个 `add` 上（`create` 才认）。
        /// </summary>
        static readonly Regex ReCreate = new Regex(@"^(?:creates?|adds?)\s+(.+)$", RegexOptions.Compiled);

        /// <summary>目的地写法 → 规范名。**顺序无关**（取最先出现的那个）。</summary>
        static readonly string[][] DestPhrases =
        {
            new[] { "in your opponent's hand", "enemyhand" },
            new[] { "in the enemy hand",       "enemyhand" },
            // ⚠️ 2026-09-13 补 `to …` 这一组：`add` 那族动词后面接的是 `to`，不是 `in`
            // （`If target dies, add Extermination Protocol to your hand`，Sautekh `Awakening Obelisk`）。
            // 顺序要紧：`to your opponent's hand` 里**不含** `to your hand`，但长的那条先试更稳。
            new[] { "to your opponent's hand", "enemyhand" },
            new[] { "to the enemy hand",       "enemyhand" },
            new[] { "to your hand",            "hand" },
            new[] { "in your hand",            "hand" },
            // 🆕 2026-09-14 A5 批 3：**省略 `your`** 的写法（`Slay: Create a random Ultramarines
            //    card in hand`）—— 少了这条整句判不认识 ⇒ `Slay:` 那半句**永远不结算**。
            // ⚠️ 位置在 `in your hand` **之后**：`in your hand` 里不含子串 `in hand`，两条不会打架。
            new[] { "in hand",                 "hand" },
            new[] { "at the top of your deck", "decktop" },
        };


        /// <summary>
        /// `Give X to Y` —— `rule_core.gd:3010`，**三种语序**：
        ///   ① `Give <内容> to <目标>`（常规）
        ///   ② `Give to <目标> <内容>`（Enhanced Aggression 卡面原文）
        ///   ③ `Give it <内容>` / `Give this unit <内容>`（无 `to`）
        /// 时长修饰 `this turn` / `until your next turn` 在内容或目标里都可能出现（`:3018`）。
        /// </summary>
        static EffectOp TryGive(string low, string src)
        {
            // 🔴 **尾句必须在挑 `to` 之前切**（2026-09-13 A4 批 1 修正）。
            //
            // 原来是在**载荷**上切尾句的，于是 `Give it Armour 1 and heal 2 to it` 走成：
            //   ① `ReGive` 的 ① 支 `(.+?)\s+to\s+(.+)` 是**非贪婪**的 ⇒ 它挑**最左**那个 `to`：
            //      载荷 = `it armour 1 and heal 2`、目标 = `it`；
            //   ② 再在载荷上切尾句 ⇒ 尾句只剩 `heal 2`，**它自己的 ` to it` 被落在目标那一侧**；
            //   ③ 尾句因此**没有目标** ⇒ 整句判「半懂」，结算时落到「己方全体」那个近似 ——
            //      该治一个人却治了全队，而且**卡面不打 `*`**（静默治错人）。
            // 先在整句上切，`heal 2 to it` 就整段进尾句、由它自己解目标。
            //
            // 这条判据**正好**不会误伤 `Give +1 Attack and +1 Health to a friendly troop`
            // —— 那儿的 `and` 后面是 `+1`，不是动词（`SplitAndTail` 的 `IsVerbWord`）。
            string giveTail = null;
            SplitAndTail(low, out low, out giveTail);

            // ---- ② `Give to <目标> <内容>` —— **先用窄的那条**（2026-09-14 修静默错打）----
            // 出处：`rule_core.gd:3012` 的 `re_g2` —— 目标短语是**写死的一小组**
            // （`an? (friendly|enemy) (troop|unit)s?` / `your units?` / `your troops?` / `it` / `the target`）。
            // 🔴 我们原来是 `^gives?\s+to\s+(.+?)\s+(.+)$`，目标**非贪婪** ⇒ 只吃一个字：
            //    `Give to a friendly troop Flank and 'Strike: Draw a card'` 被切成
            //    **目标 = `a`**、载荷 = `friendly troop flank and '…'` ⇒ `ParseTarget("a")` 返回 null，
            //    目标整个丢掉（实测 `Enhanced Aggression`，探针量到「目标[（没写）]」）。
            //    窄的那条匹配不上才退回原来的宽松写法。
            var mn = ReGiveToNarrow.Match(low);
            if (mn.Success)
            {
                var opn = new EffectOp
                {
                    Verb = "give", Source = src, Tail = giveTail,
                    Payload = mn.Groups[2].Value.Trim(),
                };
                string tgtN = mn.Groups[1].Value.Trim();
                var pn = opn.Payload;
                opn.Duration = ExtractDuration(ref pn, ref tgtN);
                opn.Payload = pn;
                opn.Target = ParseTarget(tgtN);
                return opn;
            }

            var m = ReGive.Match(low);
            if (!m.Success) return null;
            var op = new EffectOp { Verb = "give", Source = src, Tail = giveTail };

            string payload, targetText;
            if (m.Groups[1].Success)          // ② Give to <目标> <内容>
            {
                targetText = m.Groups[1].Value.Trim();
                payload = m.Groups[2].Value.Trim();
            }
            else if (m.Groups[4].Success)     // ① Give <内容> to <目标>
            {
                payload = m.Groups[3].Value.Trim();
                targetText = m.Groups[4].Value.Trim();

                // 🔴 **这个 ` to ` 必须在引号外**（2026-09-14 修静默错打）——
                //    载荷里嵌的整段效果文字自带 ` to `（`"💀 Backlash: Return to your hand"`），
                //    非贪婪的 `(.+?)\s+to\s+` 挑的是**最左**那个 ⇒ 载荷被切成 `"💀 backlash: return`、
                //    目标成了 `your hand" to a friendly troop`（实测 `Graceful Avoidance`，
                //    卡面不打 `*`、结算打错人）。引号里那份不算，换到下一个引号外的 ` to `。
                var qm = QuoteMask(low);
                if (qm != null)
                {
                    int pEnd = m.Groups[3].Index + m.Groups[3].Length;
                    int rxTo = pEnd;
                    while (rxTo < low.Length && char.IsWhiteSpace(low[rxTo])) rxTo++;
                    if (rxTo < low.Length && qm[rxTo])
                    {
                        int vEnd = m.Groups[3].Index;
                        int sep = TopLevelToAt(low, qm, rxTo + 2);
                        if (sep > vEnd)
                        {
                            payload = low.Substring(vEnd, sep - vEnd).Trim();
                            targetText = low.Substring(sep + 4).Trim();
                        }
                    }
                }
            }
            else if (m.Groups[5].Success)     // ③ Give it/them <内容>
            {
                // ⚠️ **代词要留着当目标**（`prev`），不能吃掉 ——
                //    吃掉了 `op.Target` 就是 null，`DoGive` 只好落到「己方全体」那个近似，
                //    于是 `Deploy 3 Grot and give **them** Vanguard` 给全体加、而不是给刚部署的 3 个。
                //    2026-09-12 撞到：`Finish` 也因此把整句判成「半懂」（没目标），
                //    六个 `Deploy … and give it/them X` 的卡白白掉出「完全解析」。
                payload = m.Groups[6].Value.Trim();
                targetText = m.Groups[5].Value.Trim();
            }
            else                              // ④ Give <内容>（**没写目标**，2026-09-13 A4）
            {
                // 出处：`Beacon of Faith` 的 `4: Give an additional +1 Health` ·
                //       `Fiery Conviction` 的 `4 [Energy]: Give an additional +1 [Might]`。
                // **卡面没写给谁** ⇒ 走 `Subjectless`（和 `gain` 同一条路，判据写在
                // `EffectTargetSpec.Subjectless`）：有施放者就是施放者自己，否则己方全体。
                // ⚠️ **`Dark Pact of Fate` 那四张也长这个样子，但它们故意不靠这条路结算**
                //    （`Give +2 Health and Camouflage` —— 走 `DarkPactFx` 那条通道，见
                //    `资料/战术卡47条_语义查证.md` #18-21）⇒ 那四张的 desc 至今**判不认识**，
                //    是**故意的**（卡面那个 `*` 是**误报**，属显示层口径问题，没做）。
                //    这两条路不冲突：那四张是 `tactic`，这里解析出来的 op 只会落在「打成一张战术」
                //    那条路上 —— 而它们的真效果由授予时的 pact 通道走。
                payload = m.Groups[7].Value.Trim();
                targetText = "";
            }

            // ⚠️ 尾句已经在函数开头切过了（见那里的注释）——**这里不要再切一次**，
            //    在载荷上切会挑错 `to`（那正是上面记的那个 bug）。
            // `Give it +2 this turn as well` —— `as well` 是「也」的语气词，不是载荷的一部分
            payload = Regex.Replace(payload, @"\s+as well$", "").Trim();

            // 时长修饰：原文里跟着「内容」或「目标」，两边都要看
            op.Duration = ExtractDuration(ref payload, ref targetText);
            op.Payload = payload;
            op.Target = targetText.Length == 0
                ? new EffectTargetSpec
                  {
                      // ④ 那条路：卡面**没写目标** —— 交给结算层定（见 `Subjectless` 的注释）
                      Raw = "(未写目标：有施放者就是施放者自己，否则己方全体)",
                      Side = "own", Kind = "unit", Count = 0, Auto = true, Subjectless = true,
                  }
                : ParseTarget(targetText);
            return op;
        }
        static readonly Regex ReGive = new Regex(
            @"^gives?\s+(?:to\s+(.+?)\s+(.+)" +                       // ②
            @"|(.+?)\s+to\s+(.+)" +                                   // ①
            @"|(it|this unit|this troop|them)\s+(.+)" +               // ③ 代词 + 内容
            @"|(.+))$",                                               // ④ **没写目标**（2026-09-13 A4）
            RegexOptions.Compiled);

        /// <summary>② `Give to &lt;目标&gt; &lt;内容&gt;` 的**窄写法** —— 目标短语照抄
        /// `rule_core.gd:3012` 的 `re_g2`。见 `TryGive` 里那段「先用窄的那条」。</summary>
        static readonly Regex ReGiveToNarrow = new Regex(
            @"^gives?\s+to\s+(an? (?:friendly|enemy) (?:troop|unit)s?|your units?|your troops?|it|the target)\s+(.+)$",
            RegexOptions.Compiled);

        /// <summary>`All enemies lose Stealth` / `Lose X` —— `rule_core.gd:3082`。</summary>
        static EffectOp TryLose(string low, string src)
        {
            var m = ReLose.Match(low);
            if (!m.Success) return null;
            var op = new EffectOp { Verb = "lose", Source = src };
            string subj = m.Groups[1].Success ? m.Groups[1].Value.Trim() : "";
            op.Payload = m.Groups[2].Value.Trim();
            op.Duration = ExtractDuration(ref op.Payload, ref subj);
            op.Target = subj.Length == 0 ? null : ParseTarget(subj);
            return op;
        }
        static readonly Regex ReLose = new Regex(
            @"^(?:all\s+)?(.+?)\s+loses?\s+(.+)$|^(?:all\s+)?loses?\s+(.+)$", RegexOptions.Compiled);

        /// <summary>`Gain X` —— `rule_core.gd:3101`。三种：属性增减益 / 关键词 / **裸数字 = 1 能量**（`:3114`）。</summary>
        static EffectOp TryGain(string low, string src)
        {
            // 🔴 **尾句要先切下来**（2026-09-13 A4 批 2 加）——
            //    `Each friendly Beast gets +1 [fist] this turn **and attacks a random enemy**`
            //    （`Let Loose`）：不切的话 `and attacks a random enemy` 会**被当成载荷的一部分**
            //    （`GivePayload` 认不出 ⇒ 只报一句「载荷不认识」），而**整句仍然判「认了」**
            //    ⇒ 卡面不打 `*`、覆盖率把它算进「完全解析」= 标准的**假干净**。
            //    判据借 `SplitAndTail`（`and` 后面是不是**动词**开头）——
            //    `Gain +2 Attack and +1 Health` 那类不会误伤：`and` 后面是 `+1`，不是动词。
            string tail = null;
            SplitAndTail(low, out low, out tail);

            var m = ReGain.Match(low);
            if (!m.Success) return null;
            var op = new EffectOp { Verb = "gain", Source = src, Tail = tail };
            string subj = m.Groups[1].Success ? m.Groups[1].Value.Trim() : "";
            string what = m.Groups[2].Success ? m.Groups[2].Value.Trim() : m.Groups[3].Value.Trim();
            op.Duration = ExtractDuration(ref what, ref subj);
            op.Payload = what;

            // `Gain 1` 裸数字（没有属性词）= **1 点能量** —— `rule_core.gd:3114`。
            // ⚠️ 原版那里用 `\s*` 而不是 `\s+`：句尾的 `gain 1` 后面没有空格，用 `\s+` **永远失配**。
            if (Regex.IsMatch(op.Payload, @"^\d+$"))
            {
                op.Payload = op.Payload + " energy";
                op.Verb = "gainenergy";
                op.Target = new EffectTargetSpec { Raw = "(自己的能量)", Side = "own", Kind = "player", Count = 1, Auto = true };
                return op;
            }

            // ---- 阵营资源：`Gain 2 Spirit Stones` / `Gain 1 ☀` / `Gain 1 [Faith]` / `Gain 2 任务点` ----
            // 这三样**不是「给谁加什么」**，而是给**玩家自己**的一个计数器（见 `PlayerState` 的三个字段）
            // ⇒ 单独几个动词，**不走 `give`/载荷那条路**（走那条会把 `2 spirit stones` 当成关键词塞给单位）。
            // 出处：`Infinity Circuit`「Gain 5 Spirit Stones」· `Aspect Shrine`「Gain 2 Spirit Stones」·
            //       `Missionary`「Strike: Gain +1☀」· `Sacred Rose Sister`「+1 [Faith]」·
            //       `The Rock`「Choose one: Gain 2 任务点」· `Reconnaissance Mission`「gain 3 任务点」。
            // ⚠️ **任务点那条是 2026-09-13 查出来的**：DarkAngels 那批卡的卡面图标是
            //    `questPointsN`（锯齿圆环+数字），OCR 丢图标后只剩 `Gain 1` —— 其中有几张还被
            //    误标成 `[Energy]`。见 `PlayerState.QuestPoints` 的注释。
            var res = Regex.Match(op.Payload,
                @"^\+?(\d+)\s*(?:spirit stones?|waystones?|☀|\[faith(?: icon)?\]|faith|" +
                @"quest\s*points?|任务点|\[quest\]|✦)$",
                RegexOptions.IgnoreCase);
            if (res.Success)
            {
                bool spirit = Regex.IsMatch(op.Payload, @"spirit|waystone", RegexOptions.IgnoreCase);
                bool quest = Regex.IsMatch(op.Payload, @"quest|任务点|✦", RegexOptions.IgnoreCase);
                op.Amount = int.Parse(res.Groups[1].Value);
                op.Verb = quest ? "gainquest" : (spirit ? "gainspirit" : "gainfaith");
                op.Payload = "";
                op.Target = new EffectTargetSpec
                {
                    Raw = quest ? "(玩家任务点)" : (spirit ? "(玩家灵魂石)" : "(玩家信仰)"),
                    Side = "own", Kind = "player", Count = 1, Auto = true,
                };
                return op;
            }

            // 主语是真实在场目标才用主语（`Your Warlord gains X`）；`the next Beast you play` 这类
            // 挂起型主语落回无主语。**无主语 → 有施放者就是施放者自己，没有才落到友方全体** ——
            // 这是原版自己标为「既有近似」的取舍（`rule_core.gd:3167` 那句注释写的是
            // 「无主语 → 友方全体 (**既有近似**)」，**不是**原版语义）；
            // ⚠️ **2026-09-13 A3 收窄**：单位卡的**触发式正文**（`Strike: Gain +2 Attack` ·
            //    `When you collect a Spirit Stone, gain Shield`）里，没有主语的那个 `gain`
            //    说的就是**这张卡自己** —— 落成「己方全体」会**给全队各加一份**，
    //    是「打得比卡面宽」（实测 40 处 `When` 正文里，`give it X` 那半靠代词是对的，
    //    裸 `gain X` 那一批全落错）。判据见 `EffectTargetSpec.Subjectless`。
            //    战术卡那一路**没有施放者**，仍然落到友方全体（行为不变）。
            if (subj.Length == 0 || IsSuspendedSubject(subj))
                op.Target = new EffectTargetSpec
                {
                    Raw = "(未写主语：有施放者就是施放者自己，否则己方全体)",
                    Side = "own", Kind = "unit", Count = 0, Auto = true, Subjectless = true,
                };
            else op.Target = ParseTarget(subj);
            return op;
        }
        // `Your Warlord becomes Invulnerable until your next turn` —— `becomes` 和 `gains` 是同一种
        // （`Only in Death Does Duty End` 那一族用的写法）。2026-09-12 补。
        static readonly Regex ReGain = new Regex(
            @"^(.+?)\s+(?:gains?|gets?|becomes?)\s+(.+)$|^(?:gains?|gets?|becomes?)\s+(.+)$", RegexOptions.Compiled);

        // ==================================================================
        //  目标短语 → EffectTargetSpec
        // ==================================================================

        /// <summary>
        /// `head`（`adjacent` **之前**那半截）里是不是**点名了一个目标** ——
        /// `an enemy and its ` / `a friendly troop and ` / `your Warlord and `。
        ///
        /// 只认**并列写法**（`X and …`）：`units adjacent to the target` 的 `units` 是
        /// **后置写法**的一部分，不是「前面点过的目标」，不能算（那支由 `to the target` 认）。
        /// </summary>
        static bool HasTargetNoun(string head)
        {
            if (string.IsNullOrEmpty(head)) return false;
            string h = " " + head.Trim() + " ";
            if (h.IndexOf(" and ", System.StringComparison.Ordinal) < 0) return false;
            foreach (var w in TargetNouns)
                if (h.IndexOf(w, System.StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        /// <summary>`HasTargetNoun` 用的单位名词（**含复数** —— 英文里 `enemies` 不含 `enemy`）</summary>
        static readonly string[] TargetNouns =
        {
            "unit", "troop", "enemy", "enemies", "friend", "warlord", "hero", "card",
        };

        /// <summary>
        /// 目标短语里是不是「**复数**的己方部队」（`your troops` / `your units` /
        /// `friendly Infantry troops` / `their troops` …）—— 命中就是**全体**，不用玩家挑。
        ///
        /// 🔴 **出处：原版 `rule_core.gd:3966-3980`**（那一支的注释就写着「**复数全体**」）：
        ///    `if (t.contains("your") or t.contains("friendly")) and (t.contains("units") or
        ///     t.contains("troops") or …)` ⇒ 逐格收集**本方全体**。
        ///
        /// ⚠️ **`\btroops\b` 只匹复数**：`troop` 后面接 `s` 时 `\b` 不成立，所以单数的
        ///    `to a friendly troop` 天然不命中（原版也是「挑一个」）—— **单复数就是判据**。
        /// ⚠️ **别跨子句乱配**：`[^,;]*` 把跨度限制在同一分句内，
        ///    免得 `… your Warlord, … units …` 这种被连起来（原版按分句传进来的，我们按短语）。
        /// </summary>
        static readonly Regex RePluralOwn = new Regex(
            @"\b(?:your|friendly)\b[^,;]*\b(?:troops|units)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 目标短语的组合解释。返回 `null` = **词表里没有这个词**（调用方要报 Partial，不许当成 unit）。
        ///
        /// ⚠️ 认不出来**宁可返回 null**：把 `units adjacent to the target` 猜成「一个单位」会让卡
        ///    看起来能跑、实际少打一半 —— 比不认识更糟（本工程的静默失败红线）。
        /// </summary>
        public static EffectTargetSpec ParseTarget(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            string t = text.Trim().ToLowerInvariant();
            var spec = new EffectTargetSpec { Raw = text.Trim(), Count = 1, Kind = "any", Side = "any" };

            // `it` / `them` / `the target` —— 指代**上一条效果的目标**，不是新目标。
            // 原版用 `it_target` 记着（`:2730`），我们让调用方按顺序接。
            // ⚠️ **单复数要分开**：`it` / `the target` 是**一个**，`them` 是**一批**
            //    （`Deal 2 damage to all units and give **them** Blind` 要给全体）。
            //    不分开的话「给谁」就成了近似（见 `BattleContext.LastTargets`）。
            if (IsPronoun(t))
            {
                spec.Side = "prev"; spec.Kind = "prev";
                spec.Count = (t == "them") ? 0 : 1;
                return spec;
            }

            // 🔴 `the target of the attack` —— **事件的宾语**，不是「上一条效果的目标」（2026-09-13 A3）。
            //    实测一张：`Valtus` 的 `When a friendly unit attacks, deal 3 damage to the target of the attack`。
            //    原版叫 `TargetsAffected.target = 30`（`TargetsAffected.cs`）—— 和 `attacker = 40`
            //    是**两个不同的角色**，攻击事件两个都传（`BattleManagerSupport.BroadcastUnitAttacked`
            //    的 `actingCard` / `targetCard`）。
            //    ⚠️ **不能落进上面的 `IsPronoun`**：那一族取的是 `ctx.LastTarget`，而在攻击事件里
            //       `LastTarget` 被种成了**攻击者**（监听正文的 `it` 要指它，见 `Doomstalker`）——
            //       落进去就会「打自己人」，而且**卡面不打 `*`**（句子解析得好好的）。
            //    ⚠️ **也不能判成「一个任意单位」**（`Side`/`Kind` 都不写）：那是现在实际发生的事
            //       （`Side="any"`/`Kind="any"`），会从**全场**pick 一个 —— 真正的静默错打。
            if (t == "the target of the attack" || t == "the target of that attack"
                || t == "the target of this attack" || t == "the attacked unit"
                || t == "the target of the attacks")
            {
                spec.Side = "eventtarget"; spec.Kind = "eventtarget"; spec.Count = 1;
                return spec;
            }

            // ---- `… attacked [by this unit]` —— **被这一下打到的那个**（2026-09-14 A5 批 3）----
            //   例：`Destroy any troop attacked by this unit`（`Venomthrope`）·
            //       `Destroy any enemy troop with Armour attacked by this unit`（`Blastmaster Noise Marine`）·
            //       `Stun enemies attacked`（`Stikkbomb Boy`，**省略了 `by this unit`**）。
            //   做法与下面那条 `… you deploy` 完全同形：**把后缀剥掉**，剩下的
            //   （`any troop` / `any enemy troop with Armour`）照常往下解析 ——
            //   卡面写的筛选条件（`with Armour` / `with Hunt Mark`）**一个都不丢**，
            //   只多记一个「锚在被打者身上」的标记。
            //   ⚠️ 后缀剥完什么都不剩时（只写 `attacked by this unit`）判成
            //      「**那个被打者、不筛**」—— 绝不落回下面的「任意单位」（那是静默错打）。
            {
                int ai = t.IndexOf(" attacked by this unit");
                if (ai < 0) ai = t.IndexOf(" attacked");
                if (ai > 0)
                {
                    spec.AttackedBySelf = true;
                    t = t.Substring(0, ai).Trim();
                    if (t.Length == 0)
                    {
                        spec.Side = "eventtarget"; spec.Kind = "eventtarget"; spec.Count = 1;
                        return spec;
                    }
                }
            }

            // ---- `… you deploy` / `… you put in play` —— **部署时给**（2026-09-13）----
            //   `For the rest of this battle, give Shield to all Drones you deploy`
            //   `For the rest of the match, give Armour 1 to Vehicles you put in play`
            //
            // 做法：**把后缀剥掉**，剩下的 `all Drones` 交回下面正常的目标解析 ——
            // 这样兵种 / 关键词 / 卡名几个维度**自动都支持**，不用为这个句型另写一份词表。
            //
            // **原版出处**：部署走 `CardScript.ResolveUnitSummoned`
            // （`CardScript__ResolveUnitSummoned.c:33`）→ `OnTrigger(OtherUnitSummoned = 190)`；
            // 事件参数里带着**被召唤的那张牌**（第 5 个实参），我们记成 `Deployed`。
            //
            // ⚠️ **必须在下面的 `all` 判定之前剥** —— `all Drones you deploy` 里的 `all`
            //    属于**目标**（全部 Drone），不属于那个从句。
            if (t.EndsWith(" you deploy") || t.EndsWith(" you put in play"))
            {
                spec.Deployed = true;
                t = t.Substring(0, t.LastIndexOf(" you ", System.StringComparison.Ordinal)).Trim();
                // 部署出来的永远是自己这边的（卡面从不写 `enemy Drones you deploy`）
                spec.Side = "own";
            }

            // ---- `… **deployed this way**` —— **本卡上一句刚部署的那批**（2026-09-13 A4 批 1）----
            //   `Birth of a Saga`：「`Each player deploys 3 troops from their deck.
            //   **Your troops deployed this way** gain Flank and Armour 3 this turn`」。
            //   判据与上面那条**共用同一个 `spec.Deployed`**（结算层两处都是读 `ctx.LastTargets`），
            //   不另开第三个标记。
            //   ⚠️ **张数必须收成「全部」**：`Your troops` 里没有 `all`，按默认会解成 `Count = 1`
            //      ⇒ 只给**一个**单位加，其余的静默漏掉（卡面写的是一整批）。
            if (t.EndsWith(" deployed this way"))
            {
                spec.Deployed = true;
                spec.Side = "own";
                t = t.Substring(0, t.Length - " deployed this way".Length).Trim();
                if (t.Length == 0) t = "units";
                // ⚠️ **补一个 `all ` 前缀**：张数的判据在下面「几个」那一段，它按
                //    `StartsWith("all ")` / `Contains(" all ")` 认「全部」，认不出就落回 `Count = 1`。
                //    在这里直接写 `spec.Count = 0` **没用**（下面那段会覆盖掉）—— 2026-09-13 撞到。
                if (!t.StartsWith("all ")) t = "all " + t;
            }

            // ---- `the enemy with **highest attack**` —— **挑属性最高的那个**（2026-09-13 A4 批 2）----
            // 出处：`Target friendly unit attacks the enemy with highest attack`（`Peerless Bladesmen`）。
            // ⚠️ **必须抢在下面 `ReTargetWith` 前面**：那个正则会把 `highest attack` 当成
            //    `with <关键词>` 收进 `KeywordFilter` ⇒ 按 `Has("highest attack")` 筛 ⇒
            //    **一个都不剩、空过**，而且句子解析得干干净净（静默）。
            var mHi = Regex.Match(t,
                @"\s+with\s+(highest|lowest)\s+(attack|health|melee attack|ranged attack)\s*$");
            if (mHi.Success)
            {
                spec.PickMost = (mHi.Groups[1].Value == "highest" ? "+" : "-") + mHi.Groups[2].Value;
                t = t.Substring(0, mHi.Index).Trim();
                if (t.Length == 0) t = "units";
            }

            // ---- `a troop with Destroyer` —— **关键词**筛选（原版 `TargetCriteria.traitsFilter`）----
            // 剥出来单独记，**别让它干扰下面的兵种词判定**（`troop` 才是兵种词）。
            // 实测带这个写法的只有 `Canoptek Plasmacyte`（Sautekh）：
            // `When you deploy a troop with Destroyer, give it Regeneration 1` ——
            // `Destroyer` 在卡表里是**关键词**（10 张 Sautekh 单位），不是兵种。
            var mw = ReTargetWith.Match(t);
            if (mw.Success)
            {
                spec.KeywordFilter = mw.Groups[1].Value.Trim().ToLowerInvariant();
                t = t.Substring(0, mw.Index).Trim();
            }

            // ---- 「相邻」的**锚点**（2026-09-13 候选 F）----
            // 这里以前只有一句 `spec.Adjacent = true` —— 五种语义全挤进一个 bool，结算层分不清
            // 「相对被打目标的相邻」和「相对自己的相邻」，于是**干脆不看**（静默失效）。
            // 下面这几支是把 **36 张卡面逐张看过**总结出来的（对账表 `_tmp_view/adjacent_report.md`），
            // 每支都带例卡；**认不出来的宁可标成认不出**（留 `Unset` → `Parse` 回填 → 兜底 `AdjacentFailed`）。
            if (t.Contains("adjacent"))
            {
                spec.Adjacent = true;
                int ai = t.IndexOf("adjacent", System.StringComparison.Ordinal);
                string head = t.Substring(0, ai);      // `adjacent` **之前**那半截 = 锚点候选
                string tail = t.Substring(ai);         // `adjacent` 及之后
                var anc = AdjacentAnchor.Unset;

                if (tail.Contains("to the target") || tail.Contains("to it"))
                {
                    // `Deal 4 damage to units adjacent to **the target**`（Wailing Doom）
                    anc = AdjacentAnchor.PreviousTarget;
                }
                else if (head.Contains("warlord"))
                {
                    // `Give +1 Health to **your Warlord** and adjacent units`（Ethereal Supreme）
                    // `Give +2 Attack to your Warlord and +1 Attack to adjacent troops`（Holy Fire）
                    anc = head.Contains("enemy") ? AdjacentAnchor.EnemyWarlord
                                                 : AdjacentAnchor.FriendlyWarlord;
                    spec.AnchorInSet = true;           // 督军自己也吃这个效果
                }
                else if (head.Contains("this "))
                {
                    // `Heal 2 to **this troop** and its adjacent units`（Apothecary Polixis）
                    anc = AdjacentAnchor.Self;
                    spec.AnchorInSet = true;
                }
                else if (head.Contains(" and its ") || head.Contains(" and their ")
                      || head.Contains(" and it "))
                {
                    // `Deal 3 damage to an enemy **and its** adjacent units`（Cleansing Flames）·
                    // `deal 1-2 damage **to it and its** adjacent units`（Venerable Dreadnought）
                    // —— 锚点是**并列在它前面**的那个目标。
                    anc = AdjacentAnchor.PreviousTarget;
                    spec.AnchorInSet = true;
                }
                else if (HasTargetNoun(head))
                {
                    // `Give +2 Attack to a friendly troop **and** adjacent troops`（Forged Killers）·
                    // `Deal 2 damage to an enemy unit **and** adjacent units`（Stormhawk Interception）
                    // —— 卡面**没写 `its`**，靠「前面刚点过目标」认。
                    anc = AdjacentAnchor.PreviousTarget;
                    spec.AnchorInSet = true;
                }
                // 其余（裸 `adjacent units` / `a friendly troop and adjacent …` 之外的后置写法）：
                // 锚点在**上下文**里 —— 由 `Parse()` 的回填定（只有它看得见前后句）。

                if (anc != AdjacentAnchor.Unset) spec.Anchor = anc;
            }
            if (t.Contains("random")) spec.Random = true;

            // ---- `all **damaged** enemy troops` —— 只挑**受过伤**的（2026-09-13 A4 批 1）----
            // `Oath of the Throne` 的 `Oath 4: Also destroy all damaged enemy troops`。
            // ⚠️ 判据在**结算层**（`EffectResolver` 的目标池过滤），这里只记「卡面写了这个词」——
            //    漏了它就会**把满血的也一起毁掉**，而且卡面不打 `*`（句子解析得好好的）。
            if (t.Contains("damaged")) spec.DamagedOnly = true;

            // ---- `… that is Praying` —— 只挑**正在祈祷**的（2026-09-13 A4 批 1）----
            // 由 `Dispatch` 开头那条归一送进来的（`Each friendly unit that is Praying heals 3`
            // → `heal 3 to all friendly units that are praying`）。判据在结算层（`UnitState.Prayed`）。
            if (t.Contains("praying") || t.Contains("is praying")) spec.PrayedOnly = true;

            // ---- 谁的 ----
            if (t.Contains("your warlord") || t.Contains("friendly warlord")) { spec.Side = "own"; spec.Kind = "warlord"; }
            else if (t.Contains("enemy warlord")) { spec.Side = "enemy"; spec.Kind = "warlord"; }
            else if (t.Contains("your") || t.Contains("friendly")) spec.Side = "own";
            else if (t.Contains("enemy") || t.Contains("enemies")) spec.Side = "enemy";
            else if (t.Contains("all units") || t.Contains("all troops")) spec.Side = "any";

            // ---- 哪种 ----
            if (spec.Kind != "warlord")
            {
                if (t.Contains("warlord")) spec.Kind = "warlord";
                else if (t.Contains("troop")) spec.Kind = "troop";
                else if (t.Contains("unit")) spec.Kind = "unit";
                else if (t.Contains("enem")) spec.Kind = "any";     // `an enemy` = 任意敌方单位
                else if (t.Contains("target")) spec.Kind = "prev";
                else
                {
                    // 原版还有一大串**兵种词**（`a friendly Vehicle` / `every Beast` / `Battlesuit`…）。
                    //
                    // ⚠️ **我们的卡表里没有兵种字段**（`CardDef.Type` 只有 unit/tactic/hero/defence），
                    //    所以这些词**过滤不了** —— 收下就等于「按整个阵营的目标池打」。
                    //    原版也过滤不了（它在 `_collect_tactic_targets` 里写着「类型词则整体收集
                    //    = 类型过滤精度 P2」）。**照原版收下**（不收下整张卡就变成「半懂」），
                    //    但如实记进 <see cref="EffectTargetSpec.KindUnfilterable"/>，
                    //    别让它悄悄冒充「精确打击」。
                    string kw = FirstKindWord(t);
                    if (kw == null) return null;                    // 真不认识的词 → 不猜
                    spec.Kind = kw;
                    // 2026-09-12：原版数据里有 `subtype` 兵种字段（`CardDef.Subtype`），
                    // 所以这个兵种词是**筛得了的** —— 交给 `EffectResolver.AddSide` 精确过滤。
                    // 仍然保留 `KindUnfilterable` 这个名字不用了？不 —— 见下面的分叉：
                    //   能映射到 subtype 的兵种词 → `SubtypeFilter`（真过滤）
                    //   映射不了的（`canoptek scarab` 这种细到型号的）→ 仍然如实报「过滤不了」
                    string mapped = MapToSubtype(kw);
                    if (mapped != null) spec.SubtypeFilter = mapped;
                    else spec.KindUnfilterable = true;
                }
            }

            // ---- 几个 ----
            // 🔴 **复数的己方部队 = 全体**（2026-09-14 T2b 补）：
            //    出处：原版 `rule_core.gd:3966-3980` 那一支自己的注释就是「**复数全体**」——
            //    条件是 `(your|friendly)` 且含**复数**的 `troops`/`units`，命中即**自动全体**
            //    （`_apply_with_filter` + 逐格收集，**不经过玩家挑**）。
            //    ⚠️ 判据是**名词的单复数**，**不是 `all` 这个词**：`Give +1 to your troops` 里
            //    没有 `all`，但它就是全体。我们原来只认 `all`/`each` ⇒ 这类句子落回 `Count = 1`
            //    ⇒ **只给一个单位加**、而且还要玩家点一个（实测全卡池 **44 句**，见 `TestTargetPluralOwn`）。
            //    ⚠️ **单数不碰**：`to a friendly troop` 就是「挑一个」（原版同）。
            //    ⚠️ **相邻短语不碰**：那一族的「取几个」由下面的 `AdjacentAll` 管 ——
            //    在这儿把 `Count` 改成 0 会让**锚点**挑不出来（`Count == 1` 正是
            //    `PickTarget` 判「要不要玩家点目标」的依据）。
            // ⚠️ 句首的 `Each ` 和 `all ` **同义**（2026-09-13 A4 批 2 加）：
            //    `Each friendly Beast gets +1 [fist] this turn …`（`Let Loose`）——
            //    「每个」当然就是**全部**。不认它的话会落回 `Count = 1` ⇒ **只给一个单位加**
            //    （卡面写的是一整批，而且是**静默少给**）。
            //    实测全卡池以 `Each ` 开头的分句只有 **4 句**，另 3 句各自有专门的 handler 在更前面领走
            //    （`eachunitdeal` / 「正在祈祷」归一 / `Each player deploys`）⇒ 这一条**只对 `Let Loose` 生效**。
            bool pluralOwn = !spec.Adjacent && RePluralOwn.IsMatch(t);
            if (pluralOwn || t.StartsWith("all ") || t.Contains(" all ") || t.StartsWith("each ")) spec.Count = 0;
            else
            {
                var mc = Regex.Match(t, @"\b(\d+|two|three|four|five)\b");
                if (mc.Success) spec.Count = CountWord(mc.Groups[1].Value);
                else spec.Count = 1;
            }

            // ---- 「相邻」的目标集 = **锚点周围那一圈**，不是「池子里挑几个」 ----
            // 卡面写复数的（`adjacent units` / `adjacent troops` / `units adjacent to the target`）
            // 就是「那一圈**全部**」；按 `Count = 1` 挑一个的话每张卡都会**少打一半**。
            // ⚠️ **不动 `Count`** —— 它管的是**锚点**选几个（`PickTarget` 靠 `Count == 1` 决定
            //    「这张卡要不要玩家点目标」，把它改成 0 会让 `Cleansing Flames` 那族**不再要玩家选目标**，
            //    锚点就落空了）。「那一圈全要」另记一个标记，由结算层读。
            // ⚠️ **单数要留着**：`Stealth Drone` 的 `give Stealth to **an** adjacent troop` 是「挑一个」。
            if (spec.Adjacent && IsPluralAdjacent(t)) spec.AdjacentAll = true;

            if (t.Contains("for each") || t.Contains("for every")) spec.Each = true;
            return spec;
        }

        /// <summary>
        /// 「相邻」那半截是**复数**吗 —— 决定「那一圈全部」还是「挑一个」。
        ///
        /// 只看 `adjacent` **之后**那截（`adjacent units` / `adjacent friendly troops`）；
        /// 后置写法（`units adjacent to **the target**`）的复数名词在 `adjacent` **之前**，
        /// 所以那一支单独判（`to the target` = 那一圈，复数语义）。
        /// ⚠️ 别用「有没有 s」一刀切：`its` / `this` / `Dark Pact of Excess` 都不算复数。
        /// </summary>
        static bool IsPluralAdjacent(string t)
        {
            int ai = t.IndexOf("adjacent", System.StringComparison.Ordinal);
            if (ai < 0) return false;
            string tail = t.Substring(ai + "adjacent".Length).Trim();
            if (tail.StartsWith("to the target") || tail.StartsWith("to it")) return true;
            foreach (var w in tail.Split(' '))
            {
                string x = w.Trim(',', '.', ':', ';');
                if (x.Length > 2 && x.EndsWith("s") && !x.EndsWith("ss") && x != "its" && x != "this")
                    return true;
            }
            return false;
        }

        static bool IsPronoun(string t)
        {
            t = t.Trim().ToLowerInvariant();
            return t == "it" || t == "them" || t == "the target" || t == "this unit"
                || t == "that unit" || t == "the unit" || t == "this troop" || t == "that troop";
        }

        /// <summary>
        /// 能认出来的**兵种词**（`a friendly Vehicle` / `every Beast` / `a Battlesuit`…）。
        /// 认出来只为了让整句解析通过 —— **我们过滤不了**（卡表里没有兵种字段），
        /// 调用方会把它记进 `KindUnfilterable` 并报出来。见 <see cref="ParseTarget"/> 里那段注释。
        /// </summary>
        static readonly string[] KindWords =
        {
            "vehicle", "infantry", "battlesuit", "beast", "drone", "monster", "character",
            "psyker", "swarm", "walker", "daemon", "terminator", "biker", "artillery",
            "scarab", "aircraft", "titanic",
        };

        /// <summary>
        /// 卡面兵种词 → 原版 `subtype` 字段的取值。**查不到返回 null**（那种仍然报「过滤不了」）。
        ///
        /// `subtype` 的实测取值见 `card_stats.json` 的分布（1117/1212 有值）：
        /// Infantry · Vehicle · Monster · Beast · Drone · Battlesuit · Daemon · Structure ·
        /// Warlord · Infantry/Vehicle 之外的 `Troop`（17 张）…
        /// ⚠️ 表里**没有** `canoptek scarab` / `terminator` / `biker` 这类细到型号的词 ——
        ///    那些卡面写的太细，原版数据只到兵种这一级，所以仍然过滤不了，如实报。
        /// </summary>
        static string MapToSubtype(string kindWord)
        {
            if (string.IsNullOrEmpty(kindWord)) return null;
            switch (kindWord.ToLowerInvariant())
            {
                case "vehicle": return "Vehicle";
                case "infantry": return "Infantry";
                case "battlesuit": return "Battlesuit";
                case "beast": return "Beast";
                case "drone": return "Drone";
                case "monster": return "Monster";
                case "daemon": return "Daemon";
                case "character": return null;      // 原版数据里没有这个兵种
                case "psyker": return null;
                case "swarm": return null;          // `Swarm` 是关键词不是兵种
                case "walker": return null;
                case "terminator": return null;
                case "biker": return null;
                case "artillery": return null;
                case "scarab": return null;
                case "aircraft": return null;
                case "titanic": return null;
                default: return null;
            }
        }

        static string FirstKindWord(string t)
        {
            foreach (var w in KindWords) if (t.Contains(w)) return w;
            return null;
        }

        static bool IsSuspendedSubject(string t)
        {
            t = t.Trim().ToLowerInvariant();
            return t.StartsWith("the next ") || t.StartsWith("the first ");
        }

        /// <summary>`this turn` / `until your next turn` 从文本里摘出来并**从原串里删掉**
        /// （原版 `:3018`、`:3019`）</summary>
        static string ExtractDuration(ref string a, ref string b)
        {
            string dur = "";
            if (a != null && a.Contains("until your next turn"))
            { dur = "nextturn"; a = a.Replace("until your next turn", "").Trim(); }
            else if (b != null && b.Contains("until your next turn"))
            { dur = "nextturn"; b = b.Replace("until your next turn", "").Trim(); }
            if (dur.Length == 0)
            {
                if (a != null && Regex.IsMatch(a, @"\bthis turn\b"))
                { dur = "turn"; a = Regex.Replace(a, @"\bthis turn\b", "").Trim(); }
                else if (b != null && Regex.IsMatch(b, @"\bthis turn\b"))
                { dur = "turn"; b = Regex.Replace(b, @"\bthis turn\b", "").Trim(); }
            }
            return dur;
        }

        static int CountWord(string w)
        {
            switch ((w ?? "").Trim().ToLowerInvariant())
            {
                case "a": case "an": return 1;
                case "two": return 2;
                case "three": return 3;
                case "four": return 4;
                case "five": return 5;
                default:
                    int n;
                    return int.TryParse(w, out n) && n > 0 ? n : 1;
            }
        }

        static int CountWordOrZero(string w)
        {
            if (string.IsNullOrEmpty(w)) return 0;
            return CountWord(w);
        }
    }

    /// <summary>
    /// 条件句里的**条件**认不认得（`If &lt;这里&gt;, &lt;效果&gt;`）。
    ///
    /// 为什么要单独一层：条件判不了的卡，如果当成「条件成立」去结算，
    /// 表现是**每回合都无条件触发** —— 比不实现更糟（静默失效）。
    /// 所以：**认不出来返回 null**，由上层报成「解析了但没机制」。
    ///
    /// 规范名的语义出处：`rule_core.gd:2635`（条件伤害）、`:2652`（`already had X → instead`）、
    /// `:2737`（`if the target dies, gain N energy`）。
    /// </summary>
    public static class EffectCondition
    {
        /// <summary>`Codex:` 的条件 —— 你的能量为 0（规则书 :175）。由 `EffectText` 直接置上，
        /// 不走 `Normalize`（它是**语法糖**，不是从英文文本里认出来的）。</summary>
        public const string EnergyZero = "energyzero";

        /// <summary>条件 → 规范名；**认不出来返回 null**</summary>
        public static string Normalize(string cond)
        {
            if (string.IsNullOrEmpty(cond)) return null;
            string c = cond.Trim().ToLowerInvariant();

            // `If the target dies` / `If it dies` —— 判「上一条效果的目标还在不在场上」
            if (c.Contains("target dies") || c.Contains("it dies") || c.Contains("the unit dies"))
                return "targetdies";

            // `If it already had Hunt Mark` —— 判目标身上有没有这个关键词（`:2652`）
            // ⚠️ 必须排在下面「有没有护甲」**前面**：`already has Armour` 两条都命中，
            //    具体的那条（哪个关键词）才是能判的。
            if (c.Contains("already had") || c.Contains("already has"))
                return "alreadyhas";

            // ---- 2026-09-12 补：双向 + 属性 + 空场（原先这几类只会判出 null）----
            //
            // ⚠️ `the target survives` 和 `the target dies` 是**两条不同的卡**，
            //    别用 `Contains("target")` 一勺烩 —— 判反了就是「该活的时候当死了处理」。
            if (c.Contains("target survives") || c.Contains("it survives"))
                return "targetsurvives";
            // `If it has Armour` / `If it has Armour`（Leontus 那条）
            if (c.Contains("armour") || c.Contains("armor"))
                return "targethasarmour";

            // ---- 🆕 2026-09-14 A6 族 B：四条实测「判不了」的条件（逐句探针量出来的 6 张卡）----
            //
            // ⚠️ **顺序有意义**：下面那三条都比 `targethaskw` **更具体**，必须排在它前面
            //    （它们都以 `it has` 开头）。
            // `Then, if it has 0 Ranged Attack, destroy it`（`Terrifying Crescendo`）
            if (c.Contains("0 ranged attack") || c.Contains("0 range attack")) return "rangedzero";
            // `If your Warlord has 10 or less Health, lower their cost by 4`（`At All Costs`）
            if (c.Contains("warlord has") && c.Contains("or less")) return "warlordlowhp";
            // `If a friendly troop died this turn, give them Flank`（`Vengeful Surge`）
            // ⚠️ 这一条原来在 `deaths` 那一支里被判「本版没有死亡流水」—— **现在有了**
            //    （`BattleContext.DeadUnits` 记着 `DeathTurn`，A4 批 1 为「选阵亡卡」建的）。
            if (c.Contains("died this turn")) return "deaths";
            // `<代词> has/have/is/are <词>` —— **那个词我们认识**（关键词或兵种）才认：
            //   `If it has Stealth, give it Flank`（`Flickerjump`）·
            //   `If it is [Destroyer], give it Armour 1`（`Hardwired Destruction`）·
            //   `If they are Battlesuits, give them Flank`（`Dynamic Offensive`）。
            // 🔴 **不认识的词一律往下走**（不在这里判死）—— 否则 `If it is damaged` 那种
            //    后面才判的条件会被这条先吃掉（`is damaged` 走下面那条 `damaged`）。
            {
                string w = IsClauseWord(cond);
                if (w != null && (IsKnownWord(w))) return "targethaskw";
            }
            // `if any friendly unit is Praying` —— 己方场上有没有**正在祈祷**的单位（2026-09-13 A4 批 1）。
            // 出处：`Sororitas Rhino`「`At the end of your turn, if any friendly unit is Praying, …`」·
            //       `Devout Serenity`「`Each friendly unit that is Praying heals 3`」（同一族）。
            // 规格书 `rule_core.gd:604` 那条 `re_pr` 从句只认「有没有」，我们的 `Each …` 那条走目标筛
            // （`EffectTargetSpec.PrayedOnly`）—— **两处都读同一个 `UnitState.Prayed`**，判据只有一份。
            // ⚠️ 判的是**状态**（回合开始会掉），不是「这回合祈祷过」的事件。
            if (c.Contains("praying")) return "anypraying";
            // `If you don't control any, …`（Heavy Intercessor 的补牌条件）
            if (c.Contains("control any") || c.Contains("control a ") || c.Contains("control an "))
                return "controlcount";
            // `If no enemy is affected, …`（Liutgard 那类）
            if (c.Contains("no enemy") || c.Contains("no unit is affected"))
                return "noeffect";
            // `For each Dark Pact on your troops` 那类计数（`:2524` 的 for-each 层）
            if (c.Contains("for each") || c.Contains("for every"))
                return "foreach";
            // `If you control 3 or more troops` / `If you control no other troops`
            if (c.Contains("you control"))
                return "controlcount";
            // `If any troop dies` / `when an enemy dies`
            if (c.Contains("troop dies") || c.Contains("enemy dies") || c.Contains("unit dies"))
                return "deaths";
            // `If it is damaged`
            if (c.Contains("is damaged")) return "damaged";
            // `If you have less than N Energy`
            if (c.Contains("you have") && c.Contains("energy")) return "energycheck";
            // `If it's a troop, give it Hunt Mark`
            if (c.StartsWith("it's a ") || c.StartsWith("it is a ") || c.StartsWith("it is an ")
                || c.StartsWith("it's an "))
                return "istype";
            return null;
        }

        /// <summary>
        /// `<u>代词</u> has/have/is/are **&lt;词&gt;**` 里的那个词（剥掉方括号、冠词，只取第一个词）。
        /// 不是这个形状返回 null。
        /// 🔴 **判据只此一处** —— `Normalize`（决定算什么条件）与结算层 `ConditionHolds`
        ///    （真去判那个词）**读同一份**，两处各切一次迟早不一致。
        /// </summary>
        public static string IsClauseWord(string cond)
        {
            if (string.IsNullOrEmpty(cond)) return null;
            var m = System.Text.RegularExpressions.Regex.Match(
                cond.Trim().ToLowerInvariant(),
                @"^(?:it|they|the target|this troop|this unit)\s+(?:has|have|is|are|was|were)\s+(.+)$");
            if (!m.Success) return null;
            string w = m.Groups[1].Value.Replace("[", "").Replace("]", "").Trim();  // `[Destroyer]` 的方括号是**图标标记**
            w = System.Text.RegularExpressions.Regex.Replace(w, @"^(?:a|an|the)\s+", "").Trim();  // `it is a troop`
            int sp = w.IndexOf(' ');
            if (sp > 0) w = w.Substring(0, sp);
            w = w.Trim().TrimEnd('.', ',', ';', ':');
            return w.Length > 0 ? w : null;
        }

        /// <summary>
        /// 这个词我们认识吗 —— **整词**命中关键词表，**或者**是认识的兵种词。
        /// 两个命名空间都要查：卡池里 `Stealth`/`Destroyer` 是**关键词**、`Battlesuit` 是 **`subtype`**，
        /// 而卡面写法一模一样（`If it has Stealth` / `If it is [Destroyer]` / `If they are Battlesuits`）。
        /// ⚠️ 关键词那条要**整词相等**（`KeywordTable.Normalize` 是前缀匹配，不卡长度会把
        ///    `praying` 场成 `pray`）。
        /// </summary>
        public static bool IsKnownWord(string w)
        {
            if (string.IsNullOrEmpty(w)) return false;
            int len;
            string kw = KeywordTable.Normalize(w, out len);
            if (kw != null && len == w.Length) return true;
            return CreatePool.IsKnownKind(w);
        }
    }
}
