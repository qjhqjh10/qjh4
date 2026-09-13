// WhenEvent.cs — **事件层**：卡面 `When <事件>, <效果>` 的解析与匹配（2026-09-13 第三十二轮）
//
// ⚠️ 本文件属于 `Core/` —— **不允许依赖 UnityEngine**。
//
// ==================================================================
//  这一层解决什么
// ==================================================================
//
// 卡面有两族「等某件事发生才生效」的写法，它们**要的是同一层**：
//
//   · **单位卡/督军卡**：`When a friendly troop dies, gain +2 Attack and Armour 1`
//     —— 分句头是 `When <事件>`，正文写在**逗号后面**。挂在**场上那张牌**上。
//   · **战术卡的事件触发式降费**：`Draw 3 cards. Lower cost by 1 when an enemy dies`
//     —— 降费部分写在**句尾**，事件在 `when` 后面。
//     ⚠️ 这一族**牌在手上就要开始监听**：原版 `PlayerHand.SetupCardInHand`
//     （`PlayerHand__SetupCardInHand.c:60-73`）在**牌入手牌那一刻**按 `TargetCriteria`
//     挑出该挂的效果，挂到**那张牌自己身上**。见 `CardDef.CostWhens`。
//
// **共同点**：都是「**事件** → 一个筛选条件 → 一串 op」。所以本文件只做两件事：
//   ① <see cref="Parse"/>    —— 事件短语 → <see cref="WhenEvent"/>（事件种类 + 筛选）
//   ② <see cref="Matches"/>  —— 真的发生了那件事时，判它符不符合
//
// **谁负责广播、谁负责结算**：`RuleCore` / `EffectResolver`。
//   本文件是**纯数据 + 纯判据**，不碰 ctx、不改状态 —— 和 `CardCriteria` 分工一样。
//
// ==================================================================
//  为什么要有它（而不是在调用点里写 if）
// ==================================================================
//
// 第三十一轮给触发式正文（Rally/Strike/Slay/Backlash/Penitence）开了 `CardDef.TriggerOps`。
// 那一族的「时机」是**代码里的固定调用点**（`FireTriggerAt`），要的只是一个存放位置。
// 这一族不一样：**时机本身是数据** —— 卡面写了什么事件，就得监听什么事件。
// 所以必须先有一张「事件短语 → 事件种类」的表。
//
// 实测规模（本轮探针）：**82 条 / 82 张卡 / 53 种事件名**，
// 玩家级 54 条 · 单位级（正文带 `it`/`its`/`this troop` 这类自指代词）28 条；
// 正文可解析 77/82（**正文不是瓶颈，事件名才是**）。
// 事件名分布**长尾极重**：最大的一族才 5 条，一半以上只出现 1 次。
//
// ==================================================================
//  ⚠️ 三条「照直觉做会错」的规矩
// ==================================================================
//
// ① **「认不出的事件」不许注册。** 正文能解析率很高，所以风险全在事件名那一侧 ——
//    认错了事件，正文照样是合法的 op，打起来**看不出哪里不对**
//    （和第三十一轮 `this troop` 被当 `prev` 是同一类静默错打）。
//    ⇒ <see cref="Parse"/> 认不出就返回 null，**绝不降级成「任意事件」**。
//
// ② **筛选条件的极性必须显式。** `When an enemy dies` 与 `When a friendly troop dies`
//    的**事件种类完全一样**（都是「有单位死了」），差别只在 <see cref="WhenEvent.OwnerIs"/>。
//    极性写反 = 整整一档效果反着触发，而且**不报错**。
//    原版对应的是 `TargetCriteria` + `HandEffect.playersAffected`
//    （`PlayerHand__UpdateCardEffects.c:267` 判自己 / `:303` 判敌方）——
//    那边也是显式写两遍。⇒ <see cref="Matches"/> 是极性判据的**唯一一处**。
//
// ③ **相对词不在解析期换算成阵营。** `friendly` / `enemy` 是**相对监听者**说的；
//    解析发生在一局棋之前（卡表加载时），那时没有监听者。
//    ⇒ <see cref="Parse"/> 只记方向（负数哨兵），换算留给 <see cref="Matches"/> 拿到
//    `listener` 之后做。在解析期换算 = 把判据劈成两处。**同一张卡在任何一局里解析结果一致。**

using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>
    /// `When &lt;事件&gt;, …` 里的那个「事件」：**什么事** + **什么条件才算**。
    /// 一张牌可以有多条（`When a friendly troop dies, … . When an enemy dies, …`）。
    /// </summary>
    public class WhenEvent
    {
        // ---- 事件种类（取值见 <see cref="WhenEventKind"/>；存字符串是为了日志好读）----

        /// <summary>某个单位被放上场（打出 / 效果免费部署 / 造出来）。</summary>
        public const string Deploy = "deploy";
        /// <summary>某个单位阵亡离场。</summary>
        public const string Die = "die";
        /// <summary>某个单位宣言攻击。</summary>
        public const string Attack = "attack";
        /// <summary>某个单位受到伤害（**含被护盾全挡下** —— 那是「被打了一下」，见 `EvtKind.Hit`）。</summary>
        public const string Damaged = "damaged";

        /// <summary>
        /// **玩家获得灵魂石**（2026-09-13 第三十三轮）。卡面原话是
        /// `When you collect a Spirit Stone, …`（`Farseer` / `Spiritseer Qelenaris` /
        /// `Warp Spider Exarch` / `Warlock Skyrunner` 四张灵族单位）。
        ///
        /// ⚠️ 卡面用 `collect`（收集），引擎侧发生的是「灵魂石 +N」——
        ///    规则书 `:210` 说「**控制者回合可收集**」，即收集是一个主动动作；
        ///    我们**没有**那个动作（见 `KeywordTable.Waystone` 的简化说明），
        ///    所以**把「拿到灵魂石」当作「收集」**。这是简化，已标在代码与文档里。
        /// </summary>
        public const string GainSpirit = "gainspirit";
        /// <summary>
        /// **玩家获得信仰**（2026-09-13 第三十三轮）。卡面原话
        /// `When you gain Faith, deal 4 damage to the enemy warlord`（`Paragon Warsuit`）。
        /// </summary>
        public const string GainFaith = "gainfaith";

        /// <summary>
        /// **玩家获得任务点**（2026-09-13 候选 E 补）。卡面原话：
        /// `When you gain [honour], deal 2 damage to a random enemy`（`Unforgiven Redemptor`，DarkAngels）。
        ///
        /// 🔴 **`[honour]` 不是「荣誉」，就是「任务点」** —— 2026-09-13 查实（三处证据）：
        ///   ① **我们自己的中文译文写死了**：`数据/本地化/i18n/zh_CN.csv:578` 把这张卡译成
        ///      「护甲 1。当你获得**任务点**时，对随机一个敌人造成 2 点伤害」（`cards_engine.json` 的 `descZh` 同）；
        ///   ② `资料/关键词图标/关键词与图标_对照表.md:49,51` 把卡面那枚「深绿尖刺环徽章 + 数字」
        ///      定成 `icons/questPoints1/2/3.png` —— **正是暗黑天使的阵营资源**（别的阵营没有）；
        ///   ③ 卡池里**没有任何别的卡提到 honour**。
        ///   ⇒ `资料/事件层_数据与设计.md:138` 那句「荣誉 `honour` 资源（1 张）—— 照三件套复制即可」
        ///   **是错的，别照做**（已就地更正）。
        ///
        /// ⚠️ 这个常量**原来只存在于 <see cref="WhenEventKind"/> 里、写成一个孤立字面量**
        ///    （`= "gainquest"`），`WhenEvent` 类里**没有**对应成员 ——
        ///    于是「解析里没人产出它、广播里也没人发它」，两半都缺。
        ///    现在补全：常量在这里、<see cref="WhenEventKind.GainQuest"/> 改成引用它。
        /// </summary>
        public const string GainQuest = "gainquest";
        /// <summary>
        /// **打出一张牌**（2026-09-13 第三十三轮）。卡面：`When you play a troop, …` ·
        /// `When your opponent plays a Stratagem, your Warlord takes 1 damage`。
        /// 筛什么由宾语决定（`a troop` / `a Stratagem`），走 <see cref="Subjects.Parse"/>。
        /// </summary>
        public const string Play = "play";
        /// <summary>**抽一张牌**（2026-09-13 第三十三轮）。卡面：`When you draw a card, …`。</summary>
        public const string Draw = "draw";
        /// <summary>**被再造**（2026-09-13 第三十三轮）。卡面：`When reanimated, …`（Sautekh 的 `reanimate` 那一族）。</summary>
        public const string Reanimated = "reanimated";
        /// <summary>**某个单位开始祈祷**（2026-09-13 第三十三轮）。卡面：`When a friendly unit prays, …`。</summary>
        public const string Prays = "prays";
        /// <summary>**某个单位被给予黑暗契约**（2026-09-13 第三十三轮）。
        /// 卡面：`When a friendly troop receives a Dark Pact, …`。</summary>
        public const string GetsDarkPact = "darkpact";

        // ---- 「关键词被**给予 / 失去**」族（2026-09-13 第三十四轮）----
        // ⚠️ 和「关键词被**触发**」那一族（`swarm` / `synapse` / `mob` / `ferocity`）**不是一回事**：
        //    那一族要等关键词的**机制**本身做完；这一族只要「**授予 / 移除**这个动作发生」就成立，
        //    而授予点引擎里**早就有**（见各自的发生点），只是**原来没人广播**。

        /// <summary>**某个单位被给予猎杀标记**。卡面：`When an enemy gets Hunt Mark, …`
        /// （`Venerable Dreadnought`）· `When an enemy gets a hunt mark, …`（`Thunderwolf Pack Leader`）。
        /// 发生点：`EffectResolver.ApplyOneGain` 的 `AddKeyword("huntmark")`。</summary>
        public const string GetsHuntMark = "gethuntmark";
        /// <summary>**获得护盾**。卡面：`When you gain [Shield], …`（`Company Veteran`）·
        /// `When a friendly unit obtains [Shield], …`（`Apothecary`）。发生点同上（`AddKeyword("shield")`）。</summary>
        public const string GetsShield = "getsshield";
        /// <summary>**被眩晕**。卡面：`When an enemy receives a Stun, …`（`Jain Zar`）·
        /// `When an enemy receives stun, …`（`Yrlla the Huntress`）。
        /// 发生点：`EffectResolver.DoStun` 与 `Concussion`（`RuleCore.DeclareAttack` 里那句）。</summary>
        public const string GetsStun = "getsstun";
        /// <summary>**失去潜行**。卡面：`When a friendly unit loses Stealth, …`（`Orian Laratharjos`）。
        /// 发生点：`RuleCore.DeclareAttack` 里攻击之后的 `RemoveKeyword(Stealth)`。</summary>
        public const string LosesStealth = "losestealth";
        /// <summary>**造出一张「隐秘」**。卡面：`When you create a Secret, …`（`Ravenwing Champion`，DarkAngels）。
        /// 发生点：`EffectResolver.DoCreate` 的 `hand` / `enemyhand` 支。
        ///
        /// ⚠️ **实测：卡池里没有任何一张卡会「造隐秘」** —— 18 张提到 secret/sabotage 的卡里，
        ///    造的全是 **sabotage**（`Create a random Sabotage in the enemy hand`，Genestealers 一族 10 张）；
        ///    提到 secret 的那几张（`Secret Agenda` / `Ravenwing Ballistus Dreadnought` / `Relic Munitions`）
        ///    走的是「**选择**一张隐秘加入牌库」和「**打出**过几张」。
        ///    ⇒ `Ravenwing Champion` 这条会**点亮但一次都不会响**。**这条广播留着是对的**
        ///      （真有造隐秘的效果时它就该响），但**别把它算进「已铺完」** —— 记为「点亮但暂时打不出来」。</summary>
        public const string CreatesSecret = "createsecret";
        /// <summary>**造出一张「破坏」**。卡面：`When you create a Sabotage, …`（`Atalan Jackal`，Genestealers）。
        /// 发生点同上。</summary>
        public const string CreatesSabotage = "createsabotage";

        /// <summary>见 <see cref="WhenEventKind"/>。**认不出的不注册**。</summary>
        public string Kind;

        // ---- 归属：相对方向哨兵（负数）与绝对阵营（0/1）共用一个字段 ----

        /// <summary>`friendly` —— 和监听者同一方</summary>
        public const int RelFriendly = -2;
        /// <summary>`enemy` —— 和监听者不同方</summary>
        public const int RelEnemy = -3;
        /// <summary>`opponent` —— 用法同 enemy（`your opponent plays a Stratagem`）</summary>
        public const int RelOpponent = -4;

        /// <summary>
        /// **发生这件事的那个单位归谁**：`0`/`1` = 指定阵营 · <see cref="RelFriendly"/> /
        /// <see cref="RelEnemy"/> / <see cref="RelOpponent"/> = 相对监听者 · `-1` = 不限。
        ///
        /// ⚠️ **不是「谁监听」，是「事件发生在谁身上」**。
        /// </summary>
        public int OwnerIs = -1;

        /// <summary>
        /// **自指**：这件事必须**发生在监听者自己身上**才触发（`When deployed, …`）。
        ///
        /// 2026-09-13 第三十四轮加。在这之前 `When deployed` 这类**省主语**的写法被判「认不出」——
        /// 因为按「任何单位」收就是**打得比卡面宽**（别的单位被部署时它也会响），而卡面不打 `*`。
        /// 现在有了这个标记，就能在**不放宽**的前提下把它收下来：
        /// <see cref="WhenEvents.Matches"/> 会要求 `subject` 和监听者是**同一个对象**。
        ///
        /// ⚠️ **只有语义唯一的那几条才配 `SelfOnly`**（`deployed`）。`When played`（省主语）
        ///    看起来同形，但实测那一张（`Reconnaissance Mission`）是**战术卡**，
        ///    而监听器目前只从**场上单位**收集 ⇒ 收下来也是一条**没人消费**的监听器
        ///    （本工程红线）。它真正的形状是「打出这张牌时顺带做 X」，该在
        ///    `CanPlayTactic` 那条路上补，**不是**事件层的事。见 `资料/事件层_数据与设计.md` §三·③。
        /// </summary>
        public bool SelfOnly;

        /// <summary>
        /// 发生事件的那个单位**还得符合什么**（兵种 / 关键词 / 卡名，见 <see cref="CardCriteria"/>）。
        /// `null` / 空 = 不筛。例：`you deploy a Vehicle` → 兵种 Vehicle；
        /// `a troop with Destroyer` → 兵种 Troop + 关键词 Destroyer。
        /// </summary>
        public CardCriteria Criteria;

        /// <summary>原始的事件短语（已归一化），**留着给日志和自检** ——
        /// 报错时要能打印「卡面写的是哪句话」，不然排查得回去翻卡图。</summary>
        public string Raw;

        public override string ToString()
        {
            string who = OwnerIs > -2 ? (OwnerIs < 0 ? "" : "P" + OwnerIs + " ")
                       : OwnerIs == RelFriendly ? "友方 " : "敌方 ";
            string what = Criteria != null && !Criteria.IsEmpty ? " " + Criteria : "";
            return who + Kind + what;
        }
    }

    /// <summary>事件种类的取值（想用枚举，但 <see cref="WhenEvent.Kind"/> 存字符串更好读日志）。</summary>
    public static class WhenEventKind
    {
        public const string Deploy = WhenEvent.Deploy;
        public const string Die = WhenEvent.Die;
        public const string Attack = WhenEvent.Attack;
        public const string Damaged = WhenEvent.Damaged;
        public const string GainSpirit = WhenEvent.GainSpirit;
        public const string GainFaith = WhenEvent.GainFaith;
        public const string GainQuest = WhenEvent.GainQuest;
        public const string Play = WhenEvent.Play;
        public const string Draw = WhenEvent.Draw;
        public const string Reanimated = WhenEvent.Reanimated;
        public const string Prays = WhenEvent.Prays;
        public const string GetsDarkPact = WhenEvent.GetsDarkPact;
        public const string GetsHuntMark = WhenEvent.GetsHuntMark;
        public const string GetsShield = WhenEvent.GetsShield;
        public const string GetsStun = WhenEvent.GetsStun;
        public const string LosesStealth = WhenEvent.LosesStealth;
        public const string CreatesSecret = WhenEvent.CreatesSecret;
        public const string CreatesSabotage = WhenEvent.CreatesSabotage;

        // ==================================================================
        //  🆕 带参数的 Kind：`triggers:<关键词>`（2026-09-13 候选 E）
        // ==================================================================

        /// <summary>
        /// 「**关键词被触发**」族的 `Kind` 前缀。完整形态是 `triggers:swarm` / `triggers:mob` …
        ///
        /// **为什么要带参数**：这一族的事件名**不是固定几个** —— 卡面写的是
        /// `When a friendly unit triggers **Swarm**` / `… uses **Ferocity**` /
        /// `… triggers **Duty**`，换一个关键词就是**另一件事**。
        /// 若给每个关键词各写一个常量 + 各写一条广播，就得在**两处**同步维护
        /// （这里一处、`RuleCore.FireTriggerAt` 一处），迟早在其中一边漏掉 ——
        /// 而漏掉的形式是「监听器注册了但永远不响」，正是本工程红线里的静默失效。
        /// ⇒ 改成**一个前缀 + 关键词当参数**，两边都只写一次。
        ///
        /// ⚠️ 因此 `WhenEventK ind` 里**没有**对应的 `const string` —— 请用
        /// <see cref="Triggers"/> 生成，别手拼字符串。
        /// </summary>
        public const string TriggersPrefix = "triggers:";

        /// <summary>
        /// 生成「关键词被触发」事件的 <see cref="WhenEvent.Kind"/>。
        /// <paramref name="keyword"/> 一律**小写、去空白**（和 <see cref="KeywordTable.Implemented"/> 同一套写法）。
        /// </summary>
        public static string Triggers(string keyword)
        {
            return TriggersPrefix + (keyword == null ? "" : keyword.Trim().ToLowerInvariant());
        }
    }

    /// <summary>**事件短语 → <see cref="WhenEvent"/>**，以及「真发生了那件事时它算不算」。纯判据、无状态。</summary>
    public static class WhenEvents
    {
        // ==================================================================
        //  ⓪ 认不出的事件名（自检要能报出来 —— 见 §①的规矩）
        // ==================================================================

        /// <summary>
        /// 解析时**见过但认不出**的事件短语（去重）。`Parse` 往这里写，自检读完就清。
        ///
        /// ⚠️ 只是个**报告用的桶**，不参与任何判据。生产路径上它也会被写、但从没人读，
        /// 代价是几十个短字符串，可以接受；**别在判据里读它**。
        /// </summary>
        public static readonly SortedSet<string> UnknownPhrases = new SortedSet<string>();

        // ==================================================================
        //  ① 解析
        // ==================================================================

        /// <summary>
        /// 事件短语 → <see cref="WhenEvent"/>。**认不出返回 null**。
        ///
        /// 传进来的应该是**已经切掉 `When` 和逗号**的中间那段，例如
        /// `"a friendly troop dies"` / `"you deploy a Vehicle"` / `"an enemy with Hunt Mark dies"`。
        /// （切法见 <see cref="CardDef.AddTriggerOp"/> —— 它按第一个 `,` 切。）
        /// </summary>
        public static WhenEvent Parse(string phrase)
        {
            if (string.IsNullOrEmpty(phrase)) return null;
            string s = Clean(phrase);

            var ev = new WhenEvent { Raw = s };

            // ---- 「你」→ **本方**（2026-09-13 第三十三轮修）----
            // ⚠️ `Clean` 会把 `you` 抹掉（`s.Replace(" you ", " ")`），**极性信息就丢了** ——
            //    于是 `When **you** deploy a Vehicle` 的 `OwnerIs` 停在 -1（不限），
            //    **对手部署载具时它也会触发**。这是「打得比卡面宽」，而且不报错。
            // ⇒ 在 Clean **之前**的原文里看一眼：卡面拿 `you` / `your` 当主语的一律 = **本方**。
            //    ⚠️ `your opponent` 不算（`Clean` 已经把它归一成 `opponent`，那条走 RelOpponent）。
            string lowRaw = " " + Collapse(phrase.ToLowerInvariant()) + " ";
            bool youSubject = lowRaw.Contains(" you ") || lowRaw.StartsWith(" you ")
                              || lowRaw.Contains(" your ") || lowRaw.StartsWith(" your ");
            bool yourOpponent = lowRaw.Contains(" your opponent ") || lowRaw.StartsWith(" your opponent ");
            if (youSubject && !yourOpponent) ev.OwnerIs = WhenEvent.RelFriendly;

            ParsePredicate(s, ev);

            // ⚠️ 认不出种类 → **整条作废**，并记进报告桶。绝不降级成「任意事件」——见文件头 ⚠️①。
            if (ev.Kind == null) { UnknownPhrases.Add(s); return null; }
            return ev;
        }

        /// <summary>
        /// 归一化：小写、去方括号与撇号、收空白、**去冠词与所有格**、剥掉开头的 `you`。
        ///
        /// 去冠词是必须的 —— 同一件事卡面有四五种写法：
        /// `a friendly troop dies` / `an enemy dies` / `another troop dies` / `an enemy with Hunt Mark dies`。
        /// 不去冠词就得给每种写一条正则，迟早漏。
        /// </summary>
        static string Clean(string phrase)
        {
            var sb = new System.Text.StringBuilder(phrase.Length);
            foreach (char ch in phrase)
            {
                if (ch == '[' || ch == ']') continue;               // `[Shield]` → `shield`
                if (ch == '\'') continue;                            // `opponent's` → `opponents`
                sb.Append(ch);
            }

            string s = " " + Collapse(sb.ToString().ToLowerInvariant()) + " ";
            // 整词替换（两边带空格）—— 别用裸 Replace：`another` 里的 `an` 会被吃掉
            string[] drop = { " a ", " an ", " the ", " your ", " their ", " its " };
            foreach (string d in drop) s = s.Replace(d, " ");
            s = s.Replace(" you ", " ");
            s = s.Replace(" your opponent ", " opponent ");
            return Collapse(s).Trim();
        }

        /// <summary>多个空格/制表/换行收成一个空格</summary>
        static string Collapse(string s)
        {
            var sb = new System.Text.StringBuilder(s.Length);
            bool prevSpace = false;
            foreach (char ch in s)
            {
                bool sp = ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r';
                if (sp) { if (!prevSpace) sb.Append(' '); }
                else sb.Append(ch);
                prevSpace = sp;
            }
            return sb.ToString();
        }

        /// <summary>
        /// 整段（**已经过 <see cref="Clean"/>**）→ Kind + OwnerIs + Criteria。
        ///
        /// 🔴 **动词驱动，不要「先剥谁、再判事」**（2026-09-13 自检抓出来的**静默放宽**）：
        ///    第一版是两段式 —— <c>ParseWho</c> 先吃掉开头的 `enemy `/`friendly `，
        ///    剩下那段再判是不是 `dies`。那样 `an enemy dies` 会被剥成 **`dies`**，
        ///    而 `" dies"` 前面**没有主语**、尾巴又对不上（`dies` 不是 `" dies"`），
        ///    于是判成「认不出」；更糟的是**先剥掉 `enemy` 就等于丢掉了极性** ——
        ///    就算尾巴侥幸对上，`When an enemy dies` 也会退化成「**任何**单位死」。
        ///    ⇒ 现在**先认动词，再看它前面剩什么**：剩下的那半段才决定「谁 + 什么条件」。
        /// </summary>
        static void ParsePredicate(string s, WhenEvent ev)
        {
            // ---- 死亡族：`… dies` / `… die` / `… is destroyed` / `… is killed` ----
            if (StripFirst(s, out string subj,
                    " dies", " die", " is destroyed", " are destroyed", " is killed", " are killed"))
            {
                ev.Kind = WhenEvent.Die; SetWho(subj, ev); return;
            }

            // ---- 攻击族：`… attacks` ----
            if (StripFirst(s, out subj, " attacks", " attack"))
            {
                ev.Kind = WhenEvent.Attack; SetWho(subj, ev); return;
            }

            // ---- 受伤族：`… receives damage` / `… takes damage` / `… is damaged` ----
            if (StripFirst(s, out subj,
                    " receives damage", " receive damage", " takes damage", " take damage", " is damaged"))
            {
                ev.Kind = WhenEvent.Damaged; SetWho(subj, ev); return;
            }

            // ---- 部署族 ----
            // 🆕 `When deployed, …` —— **省主语 = 就是它自己**（2026-09-13 第三十四轮）。
            // ⚠️ 这条**不能**走下面的 `StripFirst`：剥掉 `deployed` 之后主语是空串，
            //    而空主语一律判认不出（见 `StripFirst` 那条注释）。但它的语义是**唯一**的 ——
            //    卡面写 `When deployed` 只可能是「**它自己**被部署」，没有别的读法
            //    （别的单位被部署会写 `When a friendly troop is deployed`）。
            //    ⇒ 记成 `SelfOnly`，由 `Matches` 要求「发生事件的那个单位**就是**监听者」。
            //    实测就一张：`Grey Hunter`（SpaceWolves 单位）
            //    `When deployed, give Hunt Mark to a random enemy troop`。
            if (s == "deployed")
            {
                ev.Kind = WhenEvent.Deploy; ev.SelfOnly = true; return;
            }

            // `you deploy a Vehicle` 在 `Clean` 之后是 `deploy vehicle`（`you ` 被剥掉了）
            if (s.StartsWith("deploy "))
            {
                ev.Kind = WhenEvent.Deploy; SetWho(s.Substring(7), ev); return;
            }
            if (StripFirst(s, out subj, " is deployed", " are deployed", " deployed",
                                       " is put in play", " put in play"))
            {
                ev.Kind = WhenEvent.Deploy; SetWho(subj, ev); return;
            }

            // ---- 阵营资源族（2026-09-13 第三十三轮）----
            // `When you collect a Spirit Stone, gain Shield` —— `Clean` 之后是 `collect spirit stone`。
            // `When you gain Faith, deal 4 damage …` → `gain faith`。
            // ⚠️ 这两族**没有「谁的单位」那半段**（事件发生在**玩家**身上，不是某个单位），
            //    所以不走 `SetWho`，`OwnerIs` 保持 -1（不限）—— 极性由 `RelFriendly` 那条隐式成立。
            if (s.StartsWith("collect spirit stone") || s.StartsWith("collects spirit stone")
                || s.StartsWith("collect a spirit stone"))
            {
                ev.Kind = WhenEvent.GainSpirit; return;
            }
            if (s.StartsWith("gain faith") || s.StartsWith("gains faith")
                || s.StartsWith("gain a faith"))
            {
                ev.Kind = WhenEvent.GainFaith; return;
            }
            // `When you gain [honour], …`（`Unforgiven Redemptor`，DarkAngels）
            //   → `Clean` 去掉方括号与 `you` 之后是 `gain honour`。
            // 🔴 **`[honour]` 就是「任务点」**（证据写在 `WhenEvent.GainQuest` 的注释里）——
            //    所以它走的是**已有的**任务点资源，**不是**一种新资源。
            //    ⚠️ 卡池实测只有 `honour` 这一种写法（`quest point` 那几条是保险，
            //       防的是以后有卡面直写全名；多认不算放宽，因为两者指的是同一个东西）。
            if (s.StartsWith("gain honour") || s.StartsWith("gains honour")
                || s.StartsWith("gain a honour")
                || s.StartsWith("gain quest point") || s.StartsWith("gains quest point")
                || s.StartsWith("gain a quest point"))
            {
                ev.Kind = WhenEvent.GainQuest; return;
            }

            // ---- 打出族（2026-09-13 第三十三轮）----
            // `When you play a troop, …` · `When your opponent plays a Stratagem, …`
            // ⚠️ 动词在**中间**，不是 `StripFirst` 那种「尾巴匹配」能办的 —— 显式切。
            // `Clean` 之后分别是 `play troop` / `opponent plays stratagem`。
            {
                string who = null, what = null;
                if (s.StartsWith("play ")) { who = ""; what = s.Substring(5); }
                else
                {
                    int pi = s.IndexOf(" plays ");
                    if (pi > 0) { who = s.Substring(0, pi); what = s.Substring(pi + 7); }
                }
                if (what != null && what.Length > 0)
                {
                    ev.Kind = WhenEvent.Play;
                    SetWho(who, ev);
                    // ⚠️ 筛选来源是**宾语**（`a troop` / `a Stratagem`）—— 主语那半段管的是「谁」
                    ev.Criteria = Subjects.Parse(what);
                    return;
                }
            }

            // ---- 抽牌族：`When you draw a card, …` → `draw card` ----
            if (s.StartsWith("draw card") || s.StartsWith("draws card")
                || s.StartsWith("draw a card") || s.StartsWith("you draw card"))
            {
                ev.Kind = WhenEvent.Draw; return;
            }

            // ---- `When reanimated, …`（Sautekh）—— 省主语的写法，但**语义明确**：就是它自己被再造。
            // ⚠️ 和 `When deployed` 那种「省主语 = 就是它自己」是同一条路 —— 所以**必须带 `SelfOnly`**。
            // 🔴 2026-09-13 第三十四轮修：这条**原来只设了 `Kind` 就 return**，等于按
            //    「**任何单位**被再造」收 —— 场上 4 张 Sautekh 只要有一张在场，
            //    别人（**包括敌方**）被再造时它的监听器就会响，而 `subject` 指向的是**被再造的那个**，
            //    于是 `SAU72 Immortals Phalanx` 的 `deploy a copy of this troop` 会去复制**别人**。
            //    这正是本工程红线里的「打得比卡面宽」，而且**卡面不打 `*`**（正文是好的）。
            //    现有用例（`RuleEngineTest.cs` 的「从残骸翻回来」一节）只量了「自己翻自己」，
            //    所以一直没露头 —— 现在那一节补了反例。
            //    卡池实测 4 张：`SAU9 Flayed One` · `SAU10 Gauss Reaper Warrior` ·
            //    `SAU72 Immortals Phalanx` · `SAU29 Lokhust Heavy Destroyer`。
            if (s == "reanimated" || s == "reanimates" || s.StartsWith("reanimated "))
            {
                ev.Kind = WhenEvent.Reanimated; ev.SelfOnly = true; return;
            }

            // `When you reanimate a Remnant, …`（`Diviner`，Sautekh **督军**）→ `reanimate a remnant`
            // 🔴 **这条和上面那条只差一个字母，但语义完全是两回事，别合并**：
            //    「When Reanimated」 = **它自己**被再造（自指，`SelfOnly`）；
            //    「When **you** reanimate a Remnant」 = **你这一方**做了「再造」这个动作 ——
            //    监听者是**你这边**的牌（实测那张是督军，它自己并没有被再造），所以**没有 `SelfOnly`**。
            //    同理 `Kind` 复用 `Reanimated`：**发事件的地方还是同一处**
            //    （`EffectResolver` 里 reanimate 成功之后那条广播），区别只在 `who` 怎么被解释 ——
            //    `who` = **做再造的那一方**，配上 `OwnerIs = RelFriendly` 正好就是「你这一方」。
            if (s.StartsWith("reanimate a remnant") || s.StartsWith("reanimates a remnant")
                || s.StartsWith("reanimate remnant"))
            {
                ev.Kind = WhenEvent.Reanimated;
                // 卡面写的是 `you` → `Parse` 开头一般已经设成 RelFriendly 了；这里兜一手，
                // 免得哪天出现省略 `you` 的写法就退化成「任何一方再造都触发」（静默放宽）。
                if (ev.OwnerIs == -1) ev.OwnerIs = WhenEvent.RelFriendly;
                return;
            }

            // ---- `When a friendly unit prays, …` → `friendly unit prays` ----
            if (StripFirst(s, out subj, " prays", " pray", " is praying"))
            {
                ev.Kind = WhenEvent.Prays; SetWho(subj, ev); return;
            }

            // ---- `When a friendly troop receives a Dark Pact, …` ----
            //      → `friendly troop receives dark pact`
            if (StripFirst(s, out subj, " receives a dark pact", " receives dark pact",
                                       " receive a dark pact", " receive dark pact",
                                       " gets a dark pact", " gets dark pact"))
            {
                ev.Kind = WhenEvent.GetsDarkPact; SetWho(subj, ev); return;
            }

            // ---- 「关键词被**给予 / 失去**」族（2026-09-13 第三十四轮）----
            // ⚠️ 这一族和「关键词被**触发**」那族（swarm / synapse / mob / ferocity）**不是一回事**：
            //    那族要等机制本身做完；这族只要**授予 / 移除这个动作发生**就成立，
            //    而动作的发生点引擎里**早就有**（见各条注释），只是**没人广播**。
            // ⇒ 每条都只差「加词表 + 一行广播」，是这一轮里性价比最高的一批。

            // `When an enemy gets Hunt Mark, …` → `enemy gets hunt mark`
            if (StripFirst(s, out subj, " gets hunt mark", " gets a hunt mark",
                                       " receives hunt mark", " receives a hunt mark"))
            {
                ev.Kind = WhenEvent.GetsHuntMark; SetWho(subj, ev); return;
            }

            // `When you gain [Shield], …` —— `Clean` 去掉方括号、剥掉 `you ` 之后是 `gain shield`，
            // 是**动词开头**的形状（和 `gain faith` 那条同形）⇒ 走 `StartsWith`，没有主语可筛。
            if (s.StartsWith("gain shield") || s.StartsWith("gains shield")
                || s.StartsWith("gain a shield"))
            {
                ev.Kind = WhenEvent.GetsShield; return;
            }
            // `When a friendly unit obtains [Shield], …` → `friendly unit obtains shield`
            if (StripFirst(s, out subj, " obtains shield", " obtains a shield",
                                       " gains shield", " gains a shield", " receives shield"))
            {
                ev.Kind = WhenEvent.GetsShield; SetWho(subj, ev); return;
            }

            // `When an enemy receives a Stun, …` → `enemy receives stun`
            if (StripFirst(s, out subj, " receives stun", " receives a stun", " is stunned"))
            {
                ev.Kind = WhenEvent.GetsStun; SetWho(subj, ev); return;
            }

            // `When a friendly unit loses Stealth, …` → `friendly unit loses stealth`
            if (StripFirst(s, out subj, " loses stealth", " lose stealth"))
            {
                ev.Kind = WhenEvent.LosesStealth; SetWho(subj, ev); return;
            }

            // `When you create a Secret, …` → `create secret`（`you ` 被 `Clean` 剥掉）—— **动词开头**。
            // ⚠️ `you create **or play** a secret` 落不到这里（多一个 `or play`）——
            //    那个短语要一次产出**两条**事件，而 `Parse` 的签名只回一条，**故意不收**
            //    （宁可认不出，也别只接半边）。
            if (s.StartsWith("create a secret") || s.StartsWith("creates a secret")
                || s.StartsWith("create secret"))
            {
                ev.Kind = WhenEvent.CreatesSecret; return;
            }
            if (s.StartsWith("create a sabotage") || s.StartsWith("creates a sabotage")
                || s.StartsWith("create sabotage"))
            {
                ev.Kind = WhenEvent.CreatesSabotage; return;
            }

            // ---- 🆕「关键词被**触发**」族：`… triggers <关键词>` / `… uses <关键词>` ----
            //     （2026-09-13 候选 E · 清单 `_tmp_view/when_unparsed.md`）
            //
            //   卡面实测（一共 8 条短语，占那 14 条的一大半）：
            //     `When a friendly unit triggers Swarm, gain +1 Ranged Attack`（Termagant Brood）
            //     `When this unit triggers Synapse, it applies the effect twice`（Broodlord）
            //     `When a friendly troop uses Ferocity, deal 2 damage to the enemy Warlord`（Raid Tactics）
            //     `When you trigger Ferocity, High Rune Priest costs 1 less this turn`（Njal Stormcaller）
            //     `When a friendly troop triggers Duty, give it Armour 1`（Commissar）
            //     `When a friendly unit triggers Mob, it triggers an additional time`（Big Choppa Nob）
            //   ⇒ **一条通用规则收下整族**，而不是一个关键词写一条分支 ——
            //     卡面换哪个关键词都是**同一个形状**（`<谁> triggers/uses <词>`），写死了迟早漏。
            //
            //   🔴 **只在那个关键词「已经实现」时才收**（<see cref="KeywordTable.Implemented"/>）：
            //     否则会注册一条**永远不会响**的监听器 —— 卡面不打 `*`、玩家却看不到任何效果，
            //     正是本工程红线里的**静默失效**（`When <事件>` 这一层最容易出这个：
            //     认得出 ≠ 那件事发得出来）。
            //     不收的代价只是「这条继续认不出」：它照旧进 <see cref="UnknownPhrases"/>、
            //     自检照旧把它报出来、卡面照旧打 `*` —— **那是诚实的那一侧**。
            //   ✅ 顺带的好处：以后每落地一个关键词（`swarm` / `synapse` / `ferocity` / `duty` …），
            //     它的 `When … triggers X` 会**自动跟着亮**，不用回这里改一行。
            if (TryParseKeywordTrigger(s, out string kwSubj, out string kwName, out bool kwSelf)
                && KeywordTable.Implemented.Contains(kwName))
            {
                ev.Kind = WhenEventKind.Triggers(kwName);
                // ⚠️ `this unit triggers X` 是**自指**（实测 `Broodlord` 写的就是「本」单位触发突触时）。
                //    不设 `SelfOnly` 的话，**任何一个友方单位**触发都会把它叫醒 ——
                //    正是「打得比卡面宽」而且不报错。<see cref="SetWho"/> 不设这个位
                //    （它服务的是「正文里的代词会接住」那条老路），所以这里自己判。
                if (kwSelf) ev.SelfOnly = true;
                else SetWho(kwSubj, ev);
                return;
            }

            // ❗ 认不出：**故意不写**「兜底成 Deploy/Die」那种分支 —— 见文件头 ⚠️①。
            //    走到这里 `Kind` 保持 null，由 `Parse` 作废并记进 `UnknownPhrases`。
        }

        /// <summary>
        /// `… triggers &lt;关键词&gt;` / `… uses &lt;关键词&gt;` → 拆成「主语 / 关键词 / 是不是自指」。
        ///
        /// ⚠️ 传进来的 <paramref name="s"/> 必须是**已经过 <see cref="Clean"/>** 的
        ///    （冠词与 `you` 都被剥掉了），于是卡面的
        ///    `a friendly unit triggers swarm` → `friendly unit triggers swarm`、
        ///    `you trigger ferocity` → `trigger ferocity`。
        ///
        /// ⚠️ **关键词要归一成卡表那一侧的规范键**：小写、**去掉空格与连字符**
        ///    （`Hunt Mark` → `huntmark`）—— 和 <see cref="KeywordTable.Implemented"/> 里存的一致，
        ///    也和 `CardCriteria` 那边同一套写法。
        ///
        /// ⚠️ **尾巴上还挂着别的东西就不收**（`ferocity this turn` 这种半截）：
        ///    宁可这条继续认不出，也别把一句话的开头当关键词收下 ——
        ///    收错了会变成「注册了一条永远不响的监听器」，比认不出更难查。
        /// </summary>
        static bool TryParseKeywordTrigger(string s, out string subject, out string keyword, out bool self)
        {
            subject = null; keyword = null; self = false;

            const string Triggers = " triggers ";
            const string Uses = " uses ";
            int at = s.IndexOf(Triggers, System.StringComparison.Ordinal);
            int len = Triggers.Length;
            if (at < 0) { at = s.IndexOf(Uses, System.StringComparison.Ordinal); len = Uses.Length; }

            if (at >= 0)
            {
                subject = s.Substring(0, at).Trim();
                keyword = s.Substring(at + len).Trim();
            }
            // 省主语的写法：`you trigger ferocity`（`Clean` 剥掉 `you` 之后动词跑到最前面）。
            // 极性由 `Parse` 开头那句 `youSubject` 判过，这里不用再管。
            else if (s.StartsWith("trigger ", System.StringComparison.Ordinal)) keyword = s.Substring(8).Trim();
            else if (s.StartsWith("use ", System.StringComparison.Ordinal)) keyword = s.Substring(4).Trim();
            else return false;

            if (keyword == null || keyword.Length == 0) return false;

            var norm = new System.Text.StringBuilder(keyword.Length);
            foreach (char ch in keyword)
            {
                if (char.IsLetterOrDigit(ch)) norm.Append(char.ToLowerInvariant(ch));
                else if (ch == ' ' || ch == '-' || ch == '\'') continue;   // 分隔符：去掉（`hunt mark` → `huntmark`）
                else return false;                                        // 别的字符 = 不是这一族
            }
            if (norm.Length == 0) return false;
            keyword = norm.ToString();

            // `this unit` / `this troop` → 自指（和 `When deployed` 走同一条 `SelfOnly` 的路）
            if (subject == "this unit" || subject == "this troop") { self = true; subject = ""; }
            return true;
        }

        /// <summary>
        /// 动词**前面**剩下那半段 → <see cref="WhenEvent.OwnerIs"/> + <see cref="WhenEvent.Criteria"/>。
        ///
        /// 两种形状：
        ///   · **带方向词** —— `enemy dies` / `friendly troop dies` / `opponent plays a Stratagem`。
        ///     剥掉方向词，剩下的当主语（`troop` → 不筛）。
        ///   · **不带方向词** —— `dies`（`When deployed` / `When played` / `When reanimated` 这种**省主语**的写法）。
        ///     方向不限（`-1`），**不筛**。
        ///     ⚠️ 这几条的**正确语义是「就是它自己」**，我们现在按「任何单位」处理 ——
        ///        **这是我们挑的近似**，会在自检里如实报出来。
        ///        要做到精确，得给 `WhenEvent` 加一个「自指」标记、并在广播时把触发者自己传进去 ——
        ///        留到下一轮（见 `资料/卡牌效果管线_计划与交接.md` §一·七）。
        ///
        /// ⚠️ 只记方向、**不换算成阵营** —— 见文件头 ⚠️③。
        /// </summary>
        static void SetWho(string subj, WhenEvent ev)
        {
            subj = subj == null ? "" : subj.Trim();
            if (subj.Length == 0) return;                       // 省主语：方向不限、不筛

            if (subj.StartsWith("opponent"))
            {
                ev.OwnerIs = WhenEvent.RelOpponent;
                subj = subj.Substring("opponent".Length).Trim();
            }
            else if (subj.StartsWith("enemy"))
            {
                ev.OwnerIs = WhenEvent.RelEnemy;
                subj = subj.Substring("enemy".Length).Trim();
            }
            else if (subj.StartsWith("friendly"))
            {
                ev.OwnerIs = WhenEvent.RelFriendly;
                subj = subj.Substring("friendly".Length).Trim();
            }
            // `another troop dies` —— 「另一个」= 自己这边除它之外的那张 ⇒ 监听者同一方
            else if (subj.StartsWith("another"))
            {
                ev.OwnerIs = WhenEvent.RelFriendly;
                subj = subj.Substring("another".Length).Trim();
            }
            // `this unit …` / `this troop …` —— 自指；方向不限（正文里的代词会接住「就是它」）
            else if (subj.StartsWith("this unit"))  subj = subj.Substring("this unit".Length).Trim();
            else if (subj.StartsWith("this troop")) subj = subj.Substring("this troop".Length).Trim();

            ev.Criteria = Subjects.Parse(subj);
        }

        /// <summary>
        /// 串尾命中任一个 tail → 剥掉它、把前面那段作为主语吐出、返回 true。
        ///
        /// 🔴 **主语为空时返回 false**（`2026-09-13 自检抓出来的**静默放宽**`）：
        ///    卡面有 `When deployed, give Hunt Mark to a random enemy troop`（Grey Hunter）这种
        ///    **省掉主语的写法** —— 剥掉 `deployed` 之后主语是空串，
        ///    若照旧返回 true，<see cref="Subjects.Parse"/> 拿到空串会判成「不筛」，
        ///    于是这条监听器会在**任何一个单位部署时**触发，
        ///    而不是只在「**它自己**被部署时」触发 —— 正是本工程红线禁止的「打得比卡面宽」，
        ///    而且**不报错**（卡面照旧打不出 `*`，因为正文是好的）。
        ///    ⇒ 空主语一律判**认不出**，由 <see cref="Parse"/> 作废并记进报告桶。
        ///    ⚠️ 代价：`When deployed` / `When played` 这几条**暂时收不到**
        ///    （它们的正确语义是「就是它自己被部署/打出」，要另开一条自指的路）。
        ///    与「注册一条会乱触发的效果」相比，**收不到**是更安全的失败方式。
        /// </summary>
        static bool StripFirst(string s, out string subject, params string[] tails)
        {
            foreach (string t in tails)
            {
                if (!s.EndsWith(t)) continue;
                string sub = s.Substring(0, s.Length - t.Length).Trim();
                if (sub.Length == 0) break;      // ← 空主语：别认，交给调用方判失败
                subject = sub;
                return true;
            }
            subject = null;
            return false;
        }

        // ==================================================================
        //  ② 匹配（**极性判据的唯一一处** —— 见文件头 ⚠️②）
        // ==================================================================

        /// <summary>
        /// 真的发生了 <paramref name="ev"/> 描述的那种事时，**它算不算**。
        /// </summary>
        /// <param name="ev">监听器上的事件条件</param>
        /// <param name="listener">**监听方的阵营**（0/1）—— 相对词在这里换算</param>
        /// <param name="who">**发生这件事的单位归谁**；`-1` = 无归属（例：疲劳伤害没有来源单位）</param>
        /// <param name="card">那个单位的卡（判兵种/关键词用）；`null` = 卡本身不参与筛选</param>
        /// <param name="listenerUnit">**监听者自己那个单位**（判 <see cref="WhenEvent.SelfOnly"/> 用）；
        /// `null` = 手牌那一族（降费），它们没有「自己在场上」这回事</param>
        /// <param name="subject">**发生这件事的那个单位**；`null` = 事件没有具体单位
        /// （例：`When you draw a card` 是玩家的事）。只喂给自指判据，别的维度不看它</param>
        public static bool Matches(WhenEvent ev, int listener, int who, CardDef card,
                                   UnitState listenerUnit = null, UnitState subject = null)
        {
            if (ev == null || ev.Kind == null) return false;

            // ---- 自指：这件事必须发生在**监听者自己**身上（`When deployed, …`）----
            // ⚠️ 判据是**对象同一性**，不是「卡名相同」—— 同名两张是两张不同的牌。
            // ⚠️ 两头缺任何一个都判**不触发**：拿不到事实就别乱放，
            //    「收不到」比「乱触发」安全（文件头 ⚠️①）。这条同时也是
            //    「监管听不到自己的部署」之外那半边的守卫 —— 广播没传 `subject` 时，
            //    `SelfOnly` 的监听器会**静默地一次都不响**，而不是响得比卡面宽。
            if (ev.SelfOnly)
            {
                if (listenerUnit == null || subject == null) return false;
                if (!ReferenceEquals(listenerUnit, subject)) return false;
            }

            // ---- 归属 ----
            if (ev.OwnerIs != -1)
            {
                if (who < 0) return false;                  // 事件没有归属方 → 带方向的监听器一律不触发
                if (ev.OwnerIs >= 0)
                {
                    if (ev.OwnerIs != who) return false;    // 解析期就定死了阵营
                }
                else if (ev.OwnerIs == WhenEvent.RelFriendly)
                {
                    // ⚠️ **故意不判「现在是不是监听方的回合」**。原版广播器分四支
                    //    （自己 `/` 场上每张牌 `/` 当前回合方手牌 `/` 另一方手牌），
                    //    我们只做「场上每张牌」这一支，不分回合方 —— 规则书 `:238`
                    //    「被攻击方优先结算」讲的正是**非**主动方要触发。**这是我们挑的**。
                    if (who != listener) return false;
                }
                else if (who == listener) return false;     // RelEnemy / RelOpponent
            }

            // ---- 卡本身符不符合（兵种 / 关键词 / 卡名）----
            if (ev.Criteria != null && !ev.Criteria.IsEmpty)
            {
                // 🔴 **有 `subject`（发生事件的那个单位在场上）就一定要问它，别问卡面**：
                //    关键词那一维的运行时部分（`Hunt Mark` / `Dark Pact` …）**卡面上没有印**，
                //    问卡面就是「永远判不中且不报错」（`CardCriteria.Matches(UnitState)` 有详注）。
                //    `card` 只在拿不到 `subject` 时兜底（例：手牌那一族的降费）。
                if (subject != null)
                {
                    if (!ev.Criteria.Matches(subject)) return false;
                }
                else
                {
                    if (card == null) return false;
                    if (!ev.Criteria.Matches(card)) return false;
                }
            }

            return true;
        }

        // ==================================================================
        //  ③ 主语短语 → CardCriteria
        // ==================================================================

        /// <summary>
        /// `vehicle` / `troop` / `troop with destroyer` / `beast` / `eliminator` …
        /// → <see cref="CardCriteria"/>。
        ///
        /// **三级判**（顺序有意义）：
        ///   ① 空的 / `unit` / `troop` —— 那两个词是「单位」这个大类的叫法 ⇒ **不筛**
        ///   ② `X with Y` —— `X` 按兵种词判、`Y` 当关键词（`troop with Destroyer`）
        ///   ③ 剩下的当兵种词；**兵种词表里没有的，再试一次卡名**
        ///      （`a Stormboy` 的 `Stormboy` 在原版卡表里**不是 subtype**，只有同名的卡 ——
        ///        见 `CardCriteria.Name` 的注释）
        ///
        /// ⚠️ **实在认不出就返回 null（= 不筛），而不是造一个「永远假」的条件**：
        ///    兵种词表里没有的词当卡名查也查不到时，返回空条件的结果是
        ///    「**卡面照旧如实显示效果原文、但这条按「任何单位」算**」——
        ///    比「永远不触发」更接近卡面（卡面确实写了这条），而且 `RuleEngineTest`
        ///    有一条**只报数**的清单盯着它，不会悄悄变多。
        /// </summary>
        public static class Subjects
        {
            public static CardCriteria Parse(string subj)
            {
                if (string.IsNullOrEmpty(subj)) return null;
                string s = subj.Trim();

                // ⚠️ **`troop` / `unit` 是真筛选，不是「不限」**（2026-09-13 自检抓出来的错）：
                //    第一版把它们当「单位」这个大类直接 `return null`（不筛）——
                //    那样 `When a friendly **troop** dies` 会退化成「友方**任何**东西死」，
                //    督军倒下也算，**而卡面写的是 troop**（督军是 `type=hero`，不是 troop）。
                //    `CreatePool.KindWords` 里**本来就有**这两行（`{"troop","type","unit"}` /
                //    `{"unit","type","unit"}`），所以直接交给它判就行 —— 判据只有一处。
                if (s == "any") return null;

                // `X with Y` —— `X` 按兵种词判、`Y` 当关键词（`troop with Destroyer`）
                string kw = null;
                int wi = s.IndexOf(" with ");
                if (wi >= 0)
                {
                    kw = s.Substring(wi + 6).Trim();
                    s = s.Substring(0, wi).Trim();
                }

                var c = new CardCriteria();
                if (s.Length > 0)
                {
                    if (CreatePool.IsKindWord(s)) c.KindWord = s;   // 兵种/牌类词（唯一判据）
                    else c.Name = s;                                // 否则当卡名（全等匹配，见 CardCriteria.Name）
                }
                if (!string.IsNullOrEmpty(kw)) c.Keyword = kw;
                return c.IsEmpty ? null : c;
            }
        }
    }
}
