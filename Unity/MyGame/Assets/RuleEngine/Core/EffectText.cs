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
        /// <summary>包含相邻格（`adjacent units` / `its adjacent units`）</summary>
        public bool Adjacent;
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

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(Side).Append('/').Append(Kind);
            if (Count == 0) sb.Append(" 全部");
            else if (Count > 1) sb.Append(' ').Append(Count).Append(" 个");
            if (Random) sb.Append(" 随机");
            if (Adjacent) sb.Append(" 相邻");
            if (Each) sb.Append(" 每个");
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
            foreach (var raw in desc.Split('.'))
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
        public static EffectTargetSpec PickTarget(List<EffectOp> ops)
        {
            if (ops == null) return null;
            foreach (var op in ops)
            {
                var t = op.Target;
                if (t == null || t.Auto || t.Random || t.Count != 1) continue;
                if (t.Side == "prev" || t.Kind == "prev") continue;
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
        /// 解析整条 `desc`。
        /// </summary>
        /// <param name="unparsed">认不出来的分句（原文），按出现顺序</param>
        /// <param name="partial">句型认了、但目标/载荷词表里没有的分句（原文）</param>
        /// <returns>解析出来的操作；**空表不等于失败**（可能整句都是关键词声明）</returns>
        public static List<EffectOp> Parse(string desc, out List<string> unparsed, out List<string> partial)
        {
            unparsed = new List<string>();
            partial = new List<string>();
            var ops = new List<EffectOp>();

            foreach (string seg in Split(desc))
            {
                var r = ParseSegment(seg);
                if (r.Ops != null) ops.AddRange(r.Ops);
                if (r.Kind == SegKind.Unknown) unparsed.Add(seg);
                else if (r.Kind == SegKind.Partial) partial.Add(seg);
            }

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
            return ops;
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
            string t = Regex.Replace(seg.Trim(), @"[\s\d\.\-,;]+$", "").Trim().ToLowerInvariant();
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
        /// 量一批卡的文本解析覆盖（**只解析、不动状态**）。默认只看战术卡。
        /// </summary>
        /// <param name="createPool">
        /// **全卡池** —— 用来验「造牌」那一条的候选池算不算得出来（`CreatePool.Resolve`）。
        /// 不传就**不验造牌**（造牌仍算「有机制」，只是没人核过池子）。
        /// 自检里传 `CardDatabase.Load()`；`Core/` 不认识 UnityEngine，所以只能由调用方给。
        /// </param>
        public static TextCoverage Coverage(IEnumerable<CardDef> cards, string type = "tactic",
                                            IReadOnlyList<CardDef> createPool = null)
        {
            var cov = new TextCoverage();
            if (cards == null) return cov;
            var seenUnknown = new HashSet<string>();
            foreach (var c in cards)
            {
                if (c == null) continue;
                if (type != null && c.Type != type) continue;
                cov.Cards++;

                var unparsed = new List<string>();
                var partial = new List<string>();
                var ops = Parse(c.Desc, out unparsed, out partial);

                int kwOnly = 0, ok = 0, bad = 0;
                foreach (string seg in Split(c.Desc))
                {
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
                        // **动词本版没实现** —— 解析得出来，但 `ResolveOne` 里没有分支，
                        // 打出去什么都不发生。必须报，不然它们冒充「有机制」（见 `ImplementedEffectVerbs`）。
                        if (!RuleCore.ImplementedEffectVerbs.Contains(op.Verb))
                        {
                            allMech = false;
                            Bump(cov.NoMechFreq, "动词「" + op.Verb + "」本版没实现  ← " + op.Source);
                            continue;
                        }
                        // **造牌的候选池**：解析出来容易，**池子算不算得出来**是另一回事
                        // （具名卡原版数据里有没有、兵种词认不认得）。有卡池就真算一遍。
                        if (op.Verb == "create" && createPool != null)
                        {
                            var cr = CreatePool.Resolve(createPool, op.Payload, c.Faction);
                            if (cr.CopyOfPrev || cr.Ok) continue;
                            allMech = false;
                            Bump(cov.NoMechFreq, "造牌池： " + cr.Why + "  ← " + op.Payload);
                            continue;
                        }
                        // **兵种词过滤不了** —— 解析是通过了，但打的是整个目标池。
                        // 单独一栏（**不是**「没机制」：这些卡会生效，只是打得比卡面宽）
                        if (op.Target != null && op.Target.KindUnfilterable)
                        {
                            allMech = false;
                            Bump(cov.ImpreciseFreq, op.Target.Raw);
                            continue;
                        }
                        // 条件判不了 = 没机制。**当成「条件成立」会让它每次无条件触发**，比不实现更糟。
                        if (!string.IsNullOrEmpty(op.Condition) && op.ConditionKind.Length == 0)
                        {
                            allMech = false;
                            Bump(cov.NoMechFreq, "条件判不了  ← " + op.Condition);
                            continue;
                        }
                        // ⚠️ **2026-09-12 更正**：这里原来有一条「付费激活没接结算」的检查。
                        //    它是**过期的误报** —— `ResolveOne` 开头早就实现了付费分支
                        //    （付得起才结算、付不起整条不生效，照 `rule_core.gd:2529`）。
                        //    留着它会把 `Oath N:` / `4 [Energy]:` 那一族卡**错报成没机制**。
                        //    ⇒ 删掉。付费走不走得通由结算层的断言管（`TestTacticPlay` 里有）。
                        if (string.IsNullOrEmpty(op.Payload)) continue;
                        // ⚠️ **只有 `give`/`gain`/`lose` 的载荷才归 `GivePayload` 管**。
                        //    别的动词也带 `Payload`（`deploy` 的目标名、`drawtype` 的类型词、`repeat` 的条件），
                        //    拿它们去问 `GivePayload` 只会得到一堆假的「载荷词表里没有」
                        //    （2026-09-12 撞到：`← troop` ×5 其实是 `Deploy a troop` 的目标名）。
                        if (op.Verb != "give" && op.Verb != "gain" && op.Verb != "lose") continue;
                        string why;
                        if (GivePayload.Mechanized(op.Payload, out why)) continue;
                        allMech = false;
                        Bump(cov.NoMechFreq, why + "  ← " + op.Payload);
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
            string s0 = Regex.Replace(seg.Trim(),
                @"^\[\s*(codex|mob|oath|strike|slay|rally|backlash|penitence)\s*\]\s*",
                "$1: ", RegexOptions.IgnoreCase);
            string s = s0.Replace("[", "").Replace("]", "").Trim();
            // 圈码（`① ② ③`）是**次数标记**，不是句型的一部分 —— 原版靠 `contains("repeat this effect")`
            // 直接绕过它，我们把它剥掉，好让「不认识的句子」按频次排名时 key 是干净的
            // （不然 `② Repeat this effect` 和 `Repeat this effect` 会被算成两句）。
            s = StripCircled(s).Trim();

            // 句首的**语气词**，不是句型的一部分：`Also destroy all damaged enemy troops`（Oath 4 那段）/
            // `Then, if it has 0, destroy it`。不剥的话句子不以动词开头，所有 handler 都失配。
            // ⚠️ **只剥句首、只剥这两个词** —— 松一点就会吃到有意义的内容。
            s = Regex.Replace(s, @"^(?:also|then)\s*,?\s+", "", RegexOptions.IgnoreCase).Trim();
            if (s.Length == 0) { r.Kind = SegKind.KeywordOnly; return r; }
            string low = s.ToLowerInvariant();

            // ---- 付费激活前缀 `12 [Energy]: …` / `4 : …` / `8 [Faith]: …` ----
            // `rule_core.gd:2529` 那族（原版是「付不起就**整段不激活**」）。前缀在这里剥掉、
            // 代价记进 op，正文照常往下走各个 handler。
            int paidCost = 0; string paidKind = null;
            var mp = RePaid.Match(low);
            if (mp.Success)
            {
                paidCost = int.Parse(mp.Groups[1].Value);
                paidKind = mp.Groups[2].Success ? mp.Groups[2].Value : "";
                low = mp.Groups[3].Value.Trim();
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
                s = low;
                if (low.Length == 0) { r.Kind = SegKind.Unknown; r.Ops = null; return r; }
            }

            // ---- 纯关键词声明（`Ephemeral` / `Flying` / `Blast 3`…）----
            // 卡的关键词由 `CardDef` 从 `keywords` 字段单独解析，这里再声明一次是冗余 —— 跳过，不算失败。
            // ⚠️ **必须确认前缀吃满整句**：`KeywordTable.Normalize` 是前缀匹配，
            //    `"Stun a random enemy"` 也会命中 `stun` —— 只看非 null 会把整句效果跳过去（静默失效）。
            if (IsKeywordOnly(s)) { r.Kind = SegKind.KeywordOnly; return r; }

            r = Dispatch(low, s);
            // 付费代价记到**这一句产出的每条 op** 上（`12 [Energy]: Draw 3 cards`）
            if (paidCost > 0 && r.Ops != null)
                foreach (var op in r.Ops) { op.Cost = paidCost; op.CostKind = paidKind; }
            return r;
        }

        /// <summary>handler 管线（付费前缀已由调用方剥掉、记在 op.Cost 上）</summary>
        static SegResult Dispatch(string low, string src)
        {
            var r = new SegResult { Ops = new List<EffectOp>() };
            EffectOp op;

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
                foreach (var o in inner.Ops)
                {
                    o.Condition = "your energy is 0";
                    o.ConditionKind = EffectCondition.EnergyZero;
                    o.Source = src;
                }
                r.Ops.AddRange(inner.Ops);
                r.Kind = inner.Kind == SegKind.Ok ? SegKind.Ok : SegKind.Partial;
                return r;
            }

            // ---- 0c) 三选一 `Choose one: A; B or C` ----
            // ⚠️ **必须在 `if` 之后、其余 handler 之前**：原版 `_resolve_choose`（`:1161`）
            //    头一句就是 `if desc.contains("choose one") … return false` —— 把它挡在外面走特例路径。
            if (TryChooseOne(low, src, r)) return r;

            // ---- 0d) 选牌 `Choose a <筛选> and <动词>` ----
            // ⚠️ **紧跟 `Choose one` 之后**：两者都以 `choose` 开头，靠 `TryChooseOne` 先把它自己
            //    那族（`choose one:`）领走。原版 `_resolve_choose:1163` 的顺序也是这个。
            if (TryChooseCard(low, src, r)) return r;

            // ---- 1) Deal N damage [to X]   (`:2672`) ----
            op = TryDeal(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- 1b) `<谁> take(s) N damage` —— **反语序**的伤害句 ----
            //      实测 3 条，其中 `Poisoned Supplies` 的正文就是它
            //      （`At the end of your turn, your troops take 1 damage`）。
            op = TryTakeDamage(low, src);
            if (op != null) return Finish(r, op, src);

            // ---- 2) Stun   (`:2755`) ----
            op = TryStun(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

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
            op = TryLowerCost(low, src);
            if (op != null) return Finish(r, op, src);   // `and give it X` 的尾巴靠 Finish 解

            // ---- 8) Refill energy   (`:2952`) ----
            op = TryRefill(low, src);
            if (op != null) { r.Ops.Add(op); r.Kind = SegKind.Ok; return r; }

            // ---- 9) Deploy X   (`:2970`) ----
            // ⚠️ 走 `Finish` 而不是直接 `r.Ops.Add` —— `Deploy X and give it Flank` 的尾句
            //    要靠 `Finish` 递归解出来；不过 Finish 的话尾句**静默丢掉**（2026-09-12 撞到）。
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

            r.Kind = SegKind.Unknown;
            r.Ops = null;
            return r;
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
                if (tail.Ops != null) r.Ops.AddRange(tail.Ops);
                // 尾句没认出来 / 只有半懂 → 整句降级成半懂
                if (tail.Kind != SegKind.Ok && tail.Kind != SegKind.KeywordOnly) r.Kind = SegKind.Partial;
            }
            return r;
        }

        /// <summary>
        /// `X and <另一句>` 的切分。**判据是「and 后面那截像不像一个新效果」**：
        ///   · `an enemy **and** stun it`            → 后面是动词 `stun` → 切，尾句递归
        ///   · `an enemy **and** its adjacent units` → 后面是目标词的延续 → **不切**，整段当目标
        ///
        /// ⚠️ 这里**故意和 `rule_core.gd:2685` 不一样**：原版无条件在第一个 ` and ` 切，
        ///    于是 `…and its adjacent units` 的「相邻」会被切掉、当成一句解不出来的尾句丢掉。
        ///    我们按「后面是不是动词」判 —— 是**超集**（原版能解的我们都能解，且不丢相邻）。
        /// </summary>
        static void SplitAndTail(string tok, out string targetPart, out string tail)
        {
            tail = "";
            targetPart = tok;
            if (string.IsNullOrEmpty(tok)) return;
            int idx = tok.IndexOf(" and ");
            if (idx < 0) return;
            string t = tok.Substring(idx + 5).Trim();
            if (t.Length == 0) return;
            if (IsVerbWord(FirstWord(t))) { targetPart = tok.Substring(0, idx).Trim(); tail = t; }
        }

        static string FirstWord(string s)
        {
            int i = s.IndexOf(' ');
            return (i < 0 ? s : s.Substring(0, i)).Trim().TrimEnd(',', ':');
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
            foreach (string w in new[] { "troop", "vehicle", "infantry", "daemon", "beast", "drone" })
                if (c.Contains(w)) { kind = w; break; }
            // `unit` / `units` 是**通称**，但卡面写 `friendly unit` 时（对 `any enemy`）语境就是部队，
            // 而且 `for each friendly unit` 原版 `_fe_count` 也把它当 troop 基数处理。
            // ⚠️ 没写名词时（`for each damaged enemy`）**不缩小到 troop** —— 那时该数督军。
            if (kind == "any" && (c.Contains("unit") || c.Contains("troop"))) kind = "troop";

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
            var at = SplitAtTurn(low);
            if (at == null) return false;
            var inner = Dispatch(at[1], src);
            if (inner.Ops == null) return false;

            r.Ops.Add(new EffectOp
            {
                Verb = "atturn",
                Source = src,
                AtTurnPhase = at[0],
                AtTurnOps = inner.Ops,
                Payload = at[1],
            });
            r.Kind = inner.Kind == SegKind.Ok ? SegKind.Ok : SegKind.Partial;
            return true;
        }

        /// <summary>
        /// `At the start|end of your turn, &lt;正文&gt;` → `[phase, 正文]`；不是这个形状返回 null。
        ///
        /// **只此一处**判「这句话是不是回合起止形态」—— `TryAtTurn`（解析）、
        /// `IsHandTrap`（给 `DeckBuilder` 排除陷阱卡）、`RuleCore.ResolveAtTurn`（手牌扫描）
        /// 三处都走它，免得写三份判据迟早不一致。
        /// </summary>
        public static string[] SplitAtTurn(string text)
        {
            var m = ReAtTurn.Match((text ?? "").Trim());
            if (!m.Success) return null;
            string body = m.Groups[2].Value.Trim();
            if (body.Length == 0) return null;
            return new[] { m.Groups[1].Value.ToLowerInvariant() == "start" ? "turn_start" : "turn_end", body };
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

        /// <summary>`For the rest of this battle|the match, at the start|end of your turn, …`
        /// —— 1=start/end · 2=正文。</summary>
        static readonly Regex RePersistAtTurn = new Regex(
            @"^for the rest of (?:this battle|the match)\s*,\s*at the (start|end) of your turn\s*,\s*(.+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// `At the start|end of your turn, …` —— 1=start/end · 2=正文。
        ///
        /// ⚠️ **带 `IgnoreCase`**：这条正则会**直接作用在 `CardDef.Desc` 上**
        ///    （`IsHandTrap` / `ResolveAtTurn` 都拿原大小写的卡面文字来问），
        ///    而卡面写的是 `At the end of your turn, …`（大写 A）——
        ///    只匹配小写的话这两处会**静默不生效**（2026-09-13 自检抓住过）。
        /// </summary>
        static readonly Regex ReAtTurn = new Regex(
            @"^at the (start|end) of your turn\s*,\s*(.+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
        static bool TryChooseOne(string low, string src, SegResult r)
        {
            if (!low.StartsWith("choose one")) return false;
            int colon = low.IndexOf(':');
            if (colon < 0) return false;

            string body = low.Substring(colon + 1).Trim();
            if (body.Length == 0) return false;

            // 分隔：先用 `;` 切，再在每一段里按 ` or ` 切（`A; B or C` → 三段）
            var opts = new List<string>();
            foreach (string part in body.Split(';'))
            {
                string p = part.Trim();
                if (p.Length == 0) continue;
                int or = p.IndexOf(" or ");
                if (or >= 0)
                {
                    string a = p.Substring(0, or).Trim();
                    string b = p.Substring(or + 4).Trim();
                    if (a.Length > 0) opts.Add(a);
                    if (b.Length > 0) opts.Add(b);
                }
                else opts.Add(p);
            }
            if (opts.Count < 2) return false;

            // 每一段都要能**独立解析**，否则整句判半懂（别只认第一段就装作认了全部）
            var parsed = new List<string>();
            bool allOk = true;
            foreach (string o in opts)
            {
                var sub = ParseSegment(o);
                if (sub.Kind == SegKind.Ok || sub.Kind == SegKind.KeywordOnly) { parsed.Add(o); continue; }
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
        ///   ④ `Choose an effect and give it …`（Leviathan 2 张）**判为不认识**，不硬塞。
        ///      那是**另一套机制**（选的是效果不是牌），候选效果池不在任何文本里 ——
        ///      卡面 · `cards_engine.json` · 反编译 1800 个带方法体文件三处零命中。
        ///      原版在这里会默认成 `to_hand`，那是**静默的错误语义**，我们宁可报不认识。
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
        static readonly Regex ReDeal = new Regex(
            @"deals?\s+(?:(\d+)(?:-(\d+))?\s+)?(?:additional\s+)?damage(?:\s+to\s+(.+?))?$",
            RegexOptions.Compiled);

        /// <summary>
        /// `&lt;谁&gt; take(s) N damage` —— **反语序**的伤害句（`Deal N damage to X` 是正语序）。
        ///
        /// 实测全仓 **3 条**含 `take(s) N damage`，本 handler 只认**主语写全**的那两条：
        ///   · `At the end of your turn, your troops take 1 damage`（Genestealers `Poisoned Supplies`）
        ///   · `When you play a Stratagem, your Warlord takes 1 damage`（`Jammed Communications`）
        /// 认不了的两条**照旧判不认识**（别硬塞）：
        ///   · `Takes 1 damage at the start of your turn`（`Concealed Explosives`：主语省略 + 带时机后缀）
        ///   · 上面第二条的**条件部分** —— 见下面的拦截。
        ///
        /// 🔑 **单复数从动词看**（不是靠猜名词）：
        ///   `your troops **take**` → **全体**（`Count = 0`）；`your Warlord **takes**` → 一个。
        ///   英语的动词形态把这件事说清楚了，比「名词是不是复数」可靠。
        /// </summary>
        static EffectOp TryTakeDamage(string low, string src)
        {
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

        /// <summary>`Deal X damage [to Y]` —— `rule_core.gd:2672`。
        /// 目标词缺省（`Deal 3 damage`）= 原版自动选敌方最弱单位（`:2692`）。</summary>
        static EffectOp TryDeal(string low, string src)
        {
            var m = ReDeal.Match(low);
            if (!m.Success) return null;
            var op = new EffectOp { Verb = "deal", Source = src };
            if (m.Groups[1].Success) op.Amount = int.Parse(m.Groups[1].Value);
            if (m.Groups[2].Success) op.AmountMax = int.Parse(m.Groups[2].Value);
            string tok = m.Groups[3].Success ? m.Groups[3].Value.Trim() : "";

            // `Deal X to Y and <另一句>` —— 原版在这里递归解尾句（`rule_core.gd:2751`），
            // 但它是**无条件**在第一个 ` and ` 切；我们按「后面是不是动词」判（见 `SplitAndTail`）。
            SplitAndTail(tok, out tok, out op.Tail);

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

        /// <summary>`Stun [a|an|the|it] [enemy] [unit|troop]` —— `rule_core.gd:2755`。
        /// `Stun N random enemies` 的 N 在 `random` 那条分支里取（`:2759`）。</summary>
        static EffectOp TryStun(string low, string src)
        {
            if (!ReStun.IsMatch(low)) return null;
            var op = new EffectOp { Verb = "stun", Amount = 1, Source = src };
            var m = Regex.Match(low, @"stun\s+(\d+|two|three)\s+");
            if (m.Success) op.Amount = CountWord(m.Groups[1].Value);
            return op;
        }
        static readonly Regex ReStun = new Regex(@"\bstun\b", RegexOptions.Compiled);

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
            var m = Regex.Match(low, @"^destroy\s+(.+)$");
            if (m.Success)
            {
                string tok = m.Groups[1].Value.Trim();
                // `it` / `the target` / `them` 指代**上一条效果的目标**（原版用 `it_target` 记着，`:2730`），
                // 不是新目标 —— `ParseTarget` 会给它一个 `prev` 规格。
                op.Target = ParseTarget(tok);
            }
            return op;
        }
        static readonly Regex ReDestroy = new Regex(@"\bdestroy\b", RegexOptions.Compiled);

        /// <summary>`Heal N [them|<目标>]` —— `rule_core.gd:2839`：**没写目标 = 治己方督军**（`:2842`）。</summary>
        static EffectOp TryHeal(string low, string src)
        {
            var m = ReHeal.Match(low);
            if (!m.Success) return null;
            var op = new EffectOp { Verb = "heal", Source = src };
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
            op.Target = tok.Length == 0 ? null : ParseTarget(tok);
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
                // `persist` / `atturn` 是**注册**和**标记**，正文里的 op 各自在触发时才要目标
                case "persist": case "atturn":
                    return false;
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
        /// `Draw N [cards]`（`rule_core.gd:2863`）与 **5a 定向翻找** `Draw a <类型> [from your deck]`（`:2867`）。
        ///
        /// ⚠️ 先试**常规**那条：`Draw a card` 走常规；只有常规整句匹配不上、且剩下一个**类型词**时，
        ///    才当定向翻找。反过来会把 `Draw a card` 认成「翻找 card 类型」。
        /// </summary>
        static EffectOp TryDraw(string low, string src)
        {
            // ⚠️ **先把 `and <另一句>` 的尾巴切下来**再匹配 —— 两条正则都锚了 `$`，
            //    尾巴一出现就整句失配。实测 3 个分句栽在这儿：
            //      `Draw a Vehicle and give it Armour 2` / `Draw a troop and give it +1` /
            //      `Draw two troops and give them +1`（2026-09-12）
            //    切下来的尾巴由调用方的 `Finish` 递归解（本函数不自己加 op）。
            string tail = null;
            SplitAndTail(low, out low, out tail);

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
            @"^draws?\s+(?:(\d+|two|three|four|five)\s+)?(?:a\s+|an\s+|the\s+)?([a-z]+)\s*"
            + @"(?:cards?)?\s*(?:from (?:your|the) deck)?$",
            RegexOptions.Compiled);

        /// <summary>
        /// 降费 —— `rule_core.gd:2920`（`Lower the cost of (all )?&lt;X&gt; in your hand( and deck)? by N`）
        /// 与 `:2927`（`(it|they) cost(s)? N less`，指代前面刚回手/刚生成的那张）。
        /// </summary>
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
                var op = new EffectOp { Verb = "lowercost", Source = src, Payload = m1.Groups[1].Value.Trim() };
                op.Amount = m1.Groups[2].Success ? int.Parse(m1.Groups[2].Value) : 1;
                op.Duration = Dur(m1.Groups[3]);
                op.Tail = tail;
                return op;
            }
            // ② `Lower its cost by N` —— 指代**前一句刚回手/刚造出来**的那张
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
            // ④ `Your troops cost N less this turn` —— 主语是**一类牌**，不是指代
            var m4 = ReSubjectCostLess.Match(low);
            if (m4.Success)
            {
                return new EffectOp
                {
                    Verb = "lowercost", Source = src,
                    Amount = int.Parse(m4.Groups[2].Value),
                    Payload = m4.Groups[1].Value.Trim(),
                    Duration = Dur(m4.Groups[3]),
                };
            }
            return null;
        }

        /// <summary>时长后缀 → `EffectOp.Duration`。
        /// ⚠️ `for the rest of this battle` 是**永久**，不是「本回合」—— 两者都非空，
        ///    只看「有没有值」会把永久当成限时（第一次写就是这么错的）。</summary>
        static string Dur(System.Text.RegularExpressions.Group g)
        {
            if (!g.Success || g.Value.Length == 0) return "";
            return g.Value.IndexOf("this turn", System.StringComparison.Ordinal) >= 0 ? "turn" : "";
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
        /// <summary>`Lower its cost by N` —— 1=几费 · 2=时长（指代上一张）</summary>
        static readonly Regex ReLowerItsCost = new Regex(
            @"^lower\s+(?:its|their|the)\s+cost\s+by\s+(\d+)"
            + @"(?:\s+(this turn|for the rest of this battle))?$", RegexOptions.Compiled);
        /// <summary>`(it|they) cost(s) N less` —— 1=几费 · 2=时长</summary>
        static readonly Regex ReCostLess = new Regex(
            @"^(?:it|they)\s+costs?\s+(\d+)\s+less"
            + @"(?:\s+(this turn|for the rest of this battle))?$", RegexOptions.Compiled);
        /// <summary>`Your troops cost 1 less this turn` —— 1=主语 · 2=几费 · 3=时长</summary>
        static readonly Regex ReSubjectCostLess = new Regex(
            @"^(?:your\s+)?(.+?)\s+costs?\s+(\d+)\s+less"
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
        /// **付费激活前缀** `12 [Energy]: …` / `4 : …` / `8 [Faith]: …`（`rule_core.gd:2529` 那族）。
        /// 组 3 = 正文（前缀剥掉后继续走 handler 管线）。
        /// </summary>
        static readonly Regex RePaid = new Regex(
            @"^(\d+)\s*(?:\[\s*(energy|faith|spirit stones?|might|attack|health|icon)\s*\]\s*|\s+)?:\s*(.+)$",
            RegexOptions.Compiled);

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
            var m = ReReturn.Match(low);
            if (!m.Success) return null;

            string who = m.Groups[2].Value.Trim();
            string destText = m.Groups[3].Value.Trim().TrimEnd('.', ' ');
            if (who.Length == 0) return null;

            string dest = null;
            foreach (var pair in ReturnDests)
                if (destText == pair[0]) { dest = pair[1]; break; }
            if (dest == null) return null;              // 目的地不纯 → 判不认识，绝不猜

            var op = new EffectOp { Verb = "return", Source = src, Payload = who, Dest = dest };
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
            var m = ReGive.Match(low);
            if (!m.Success) return null;
            var op = new EffectOp { Verb = "give", Source = src };

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
            }
            else                              // ③ Give it/them <内容>
            {
                // ⚠️ **代词要留着当目标**（`prev`），不能吃掉 ——
                //    吃掉了 `op.Target` 就是 null，`DoGive` 只好落到「己方全体」那个近似，
                //    于是 `Deploy 3 Grot and give **them** Vanguard` 给全体加、而不是给刚部署的 3 个。
                //    2026-09-12 撞到：`Finish` 也因此把整句判成「半懂」（没目标），
                //    六个 `Deploy … and give it/them X` 的卡白白掉出「完全解析」。
                payload = m.Groups[6].Value.Trim();
                targetText = m.Groups[5].Value.Trim();
            }

            // ⚠️ **`and <另一句>` 也可能挂在载荷里**（不是挂在目标后面）：
            //    `Give it Armour 1 and heal 2 to it` —— 不切的话 `heal 2 to it` 会被当成
            //    载荷的一部分，`GivePayload` 认不出 → 整条「载荷词表里没有」（2026-09-12 撞到）。
            //    判据借 `SplitAndTail`：`and` 后面那截是不是**动词**开头。
            //    这条判据**正好**不会误伤 `Give +1 Attack and +1 Health to a friendly troop`
            //    —— 那儿的 `and` 后面是 `+1`，不是动词。
            string giveTail = null;
            SplitAndTail(payload, out payload, out giveTail);
            op.Tail = giveTail;

            // `Give it +2 this turn as well` —— `as well` 是「也」的语气词，不是载荷的一部分
            payload = Regex.Replace(payload, @"\s+as well$", "").Trim();

            // 时长修饰：原文里跟着「内容」或「目标」，两边都要看
            op.Duration = ExtractDuration(ref payload, ref targetText);
            op.Payload = payload;
            op.Target = targetText.Length == 0 ? null : ParseTarget(targetText);
            return op;
        }
        static readonly Regex ReGive = new Regex(
            @"^gives?\s+(?:to\s+(.+?)\s+(.+)" +                       // ②
            @"|(.+?)\s+to\s+(.+)" +                                   // ①
            @"|(it|this unit|this troop|them)\s+(.+))$",              // ③ 代词 + 内容
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
            var m = ReGain.Match(low);
            if (!m.Success) return null;
            var op = new EffectOp { Verb = "gain", Source = src };
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

            // 主语是真实在场目标才用主语（`Your Warlord gains X`）；`the next Beast you play` 这类
            // 挂起型主语落回无主语。**无主语 → 友方全体** —— 这是原版自己标为「既有近似」的取舍
            // （`rule_core.gd:3137`），照抄，并在注释里如实标明它是近似。
            if (subj.Length == 0 || IsSuspendedSubject(subj))
                op.Target = new EffectTargetSpec
                {
                    Raw = "(未写主语：按原版落到己方全体 —— 原版自标为近似)",
                    Side = "own", Kind = "unit", Count = 0, Auto = true,
                };
            else op.Target = ParseTarget(subj);
            return op;
        }
        // `Your Warlord becomes Invulnerable until your next turn` —— `becomes` 和 `gains` 是同一种
        // （`Only in Death Does Duty End` 那一族用的写法）。2026-09-12 补。
        static readonly Regex ReGain = new Regex(
            @"^(.+?)\s+(?:gains?|becomes?)\s+(.+)$|^(?:gains?|becomes?)\s+(.+)$", RegexOptions.Compiled);

        // ==================================================================
        //  目标短语 → EffectTargetSpec
        // ==================================================================

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

            if (t.Contains("adjacent")) spec.Adjacent = true;
            if (t.Contains("random")) spec.Random = true;

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
            if (t.StartsWith("all ") || t.Contains(" all ")) spec.Count = 0;
            else
            {
                var mc = Regex.Match(t, @"\b(\d+|two|three|four|five)\b");
                if (mc.Success) spec.Count = CountWord(mc.Groups[1].Value);
                else spec.Count = 1;
            }

            if (t.Contains("for each") || t.Contains("for every")) spec.Each = true;
            return spec;
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
            // `If the target has Armour` / `If it has Armour`（Leontus 那条）
            if (c.Contains("armour") || c.Contains("armor"))
                return "targethasarmour";
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
    }
}
