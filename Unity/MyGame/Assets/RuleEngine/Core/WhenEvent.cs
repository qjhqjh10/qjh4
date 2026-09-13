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
        public const string GainQuest = "gainquest";
        public const string Play = WhenEvent.Play;
        public const string Draw = WhenEvent.Draw;
        public const string Reanimated = WhenEvent.Reanimated;
        public const string Prays = WhenEvent.Prays;
        public const string GetsDarkPact = WhenEvent.GetsDarkPact;
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
            // ⚠️ 和 `When deployed` 那种「省主语 = 就是它自己、但我们不敢收」不同：
            //    这个短语**没有别的读法**（不存在「别的单位被再造」这种写法），所以收。
            if (s == "reanimated" || s == "reanimates" || s.StartsWith("reanimated "))
            {
                ev.Kind = WhenEvent.Reanimated; return;
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

            // ❗ 认不出：**故意不写**「兜底成 Deploy/Die」那种分支 —— 见文件头 ⚠️①。
            //    走到这里 `Kind` 保持 null，由 `Parse` 作废并记进 `UnknownPhrases`。
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
        public static bool Matches(WhenEvent ev, int listener, int who, CardDef card)
        {
            if (ev == null || ev.Kind == null) return false;

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
                if (card == null) return false;
                if (!ev.Criteria.Matches(card)) return false;
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
