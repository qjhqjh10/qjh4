// CardCriteria.cs — 卡面**筛选条件**（我们的 `TargetCriteria`）
//
// **为什么单独一个类**：原版把「这类效果作用在哪种卡上」做成**数据**（`TargetCriteria`），
// 而不是写死在每张卡的效果里。我们照这个来 —— 一份判据，两个地方用：
//   ① 部署时触发（<see cref="RuleCore.ResolveDeploy"/>）：刚部署的那张牌符不符合
//   ② 持续改费（<see cref="CostMod.Criteria"/> → <see cref="RuleCore.CostOf"/>）：手里的牌符不符合
//
// **权威语义来源（2026-09-13 反编译查证）**：
//   · 原版 `TargetCriteria` 是**普通类**（不是枚举），维度有 8 类，见 `TargetCriteria.cs:6-91`
//   · 其中「兵种/种族」走 `keywordFilter`，枚举 `Keyword`
//     （`Keyword.cs:1-12`：`Infantry=5 Vehicle=10 Monster=15 Building=20 Battlesuit=25 Drone=30 Beast=35 Daemon=40`）
//   · 「哪种牌」走 `includeHero/includeMinions/includeSpells/...`（`TargetCriteria.cs:15-29`），
//     底层 `CardTypeOptions { Minion=0, Hero=10, Tactic=20, Whispers=40 }` —— **「部队」就是 `Minion`**
//   · 「具体哪张卡」走 `filterSpecificCards`（`TargetCriteria.cs:87`）
//   · 「特性/关键词」走 `traitsFilter`（`DefinedTrait`，130 个值）+ `excludeTrait`
//   · 匹配函数是 `FilterMethods.CheckIfMeetsCriteria(thisCard, evaluatedCard, targetCard, criteria)`
//     （`FilterMethods.cs:5`），调用点 4 处：`PlayerHand__SetupCardInHand.c:60` ·
//     `PlayerHand__CheckEffectsOnNewCard.c:33` · `PlayerHand__AddHandEffect.c:128` ·
//     `CardScript__NeedsToChooseFromPool.c:89`
//
// ⚠️ **查不到的**：`FilterMethods.*` 的方法体**没被反编译出来**（1800 个 `.c` 里没有这个类），
//    所以「Drone 具体怎么和 `Keyword` 枚举比」看不到；`EntityScript.get_CurrentCost()`
//    的方法体同样没有。**这几处是我们按字段语义推的，不是逐字段抄的。**
//
// ⚠️ **兵种这一维不另写判据** —— 直接调 <see cref="CreatePool.MatchesKind"/>。那是全仓唯一的
//    「这张卡算不算某一类」判据（候选池筛、定向翻找也共用它）。再写一份迟早不一致。

using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>
    /// 「这类效果作用在**哪种卡**上」。三个维度可单独用、也可叠加（叠加 = 全部满足）。
    ///
    /// 三个字段各自对应原版 `TargetCriteria` 的一个维度，见文件头的出处表。
    /// **空 = 不筛**（不是「筛不出」）—— <see cref="IsEmpty"/> 为真时 <see cref="Matches"/> 恒真。
    /// </summary>
    public class CardCriteria
    {
        /// <summary>
        /// 兵种词 / 牌类词（`drone` / `vehicle` / `beast` / `sabotage` / `troop` …）。
        /// 对应原版的 `keywordFilter`（`Keyword` 枚举）+ `CardTypeOptions`（部队/法术）。
        /// 判据交给 <see cref="CreatePool.MatchesKind"/> —— **不另写一份**。
        /// </summary>
        public string KindWord;

        /// <summary>
        /// 兵种这一维的**其余可选词**（`Friendly **Infantry and Drones** have Flank`）。
        /// 非空时，兵种这一维 = 「匹配 <see cref="KindWord"/> **或** <see cref="KindAnyOf"/> 里任一个」
        /// —— 卡面那个 `and` 是**并集**，不是交集。
        ///
        /// 🆕 2026-09-14（A7 光环）加。**为什么要它**：`EffectText.ParseTarget` 只留**第一个**兵种词
        /// （`FirstKindWord` 返回单个词）⇒ 照它走，`Friendly Infantry and Drones` 会
        /// **静静只给 Infantry**（Drone 一个都拿不到，而且没有任何报错）。
        /// 实测这一族两张：`Devilfish`（`Infantry and Drones`）·
        /// `Cadre Fireblade`（`Infantry and Battlesuit troops`）。
        ///
        /// ⚠️ **注意同一段里的并列不是并集**：`Infantry troops` 是「步兵部队」= **交集**，
        ///    所以那种写法**不会**进这一栏（解析侧只留 `Infantry`，`troops` 只贡献「排督军」那一层）。
        /// </summary>
        public List<string> KindAnyOf;

        /// <summary>
        /// 关键词（`destroyer` …）。对应原版的 `traitsFilter`（`DefinedTrait`）。
        /// 例：`When you deploy a troop with Destroyer, give it Regeneration 1` ——
        /// `Destroyer` 在卡表里是**关键词**不是兵种（Sautekh 10 张，见 `cards_engine.json`）。
        /// </summary>
        public string Keyword;

        /// <summary>
        /// **卡名**（`stormboy` / `eliminator` …）。对应原版的 `filterSpecificCards`。
        ///
        /// ⚠️ **必须全等，不许「包含」**：`Eliminator Sergeant` 的名字里也有 `Eliminator`，
        ///    按包含匹配会让它**自己触发自己**。比较走 <see cref="CreatePool.Norm"/>（撇号/大小写归一）。
        ///    实测这两个词在卡表里**不是 subtype**（`Stormboy` sub=Infantry、`Eliminator` sub=Infantry），
        ///    只有**同名的卡** —— 所以卡面写的 `a Stormboy` 只能按卡名筛。
        /// </summary>
        public string Name;

        /// <summary>
        /// **只筛督军**（`Your Warlord has Armour 2`）。
        ///
        /// 🆕 2026-10-19（第五十五会话 E10 · `SW38 Land Raider`）：光环层的**第 4 个维度**。
        /// 为什么不复用 <see cref="KindWord"/>：`CreatePool.IsKindWord("warlord")` **为假**
        /// （`KindWords` 是**卡池**筛选用表，卡池里没有督军）⇒ 塞进去会变成「什么都筛不中」的静默空过。
        /// 判据：`ctx.Players[x].Warlord` 在 `BoardSpec.WarlordSlot`、`UnitState.IsWarlord` 为真
        /// （`RuleCore.NewBattle` 那一行 `p.Board[BoardSpec.WarlordSlot] = p.Warlord`）。
        /// ⚠️ 只看 `IsWarlord`，**不**额外判阵营 —— 阵营那半由 `AuraSpec` 的 `Enemy`/`Head` 决定
        /// （`ForEachTarget` 已经选好了 owner）。
        /// </summary>
        public bool WarlordOnly;

        /// <summary>
        /// 🆕 2026-10-23（引擎线 `新账-97` ⑥）：原版 `TargetCriteria.spellType`（`SpellType` 枚举）。
        /// **`-1` = 不筛**（注意：`0 = Normal` 是**合法筛选值**，不能拿 0 当「不筛」）。
        ///
        /// <para>判据 = `FilterMethods__CheckIfMeetsCriteria.c:286-300` 那一段之后，
        /// `spellType` 是**独立一格**（`:305-308`：`spellType != 0` 且与候选的 `+0x90` 不等 ⇒ 淘汰）。
        /// 数据列 → `CardDef.SpellType`（v19 卡表的 `spellType`，真值直搬）。</para>
        ///
        /// <para>⚠️ **今天没有生产者**：`cards_engine.json` 里**没有** `targetCriteria` 列
        /// （只把 `conditionsFilter` 那几格摊成了 `unitConditions`）⇒ 这一维**结构性就位、零行为变化**。
        /// 全池普查（2026-10-23）里唯一用到 `spellType` 的两处
        /// （`DA66` 的 `actingCardCriteria.spellType = 150`）**已由正文的 `a Secret` 覆盖** ——
        /// `CreatePool.KindWords` 的 `secret → subtype Secret`，与 `spellType == 150` 的 5 张**一一对应**。
        /// 数据侧真要接这一维时，把 `targetCriteria.spellType` 落进卡表、在构造处赋值即可，
        /// 判据已在 <see cref="Matches(CardDef)"/> / <see cref="Matches(UnitState, BattleContext)"/> 里。</para>
        /// </summary>
        public int SpellType = -1;

        /// <summary>
        /// 🆕 2026-10-23（`新账-97` ⑥）：原版 `TargetCriteria.conditionsFilter`（`UnitConditions` 数值）。
        /// **`-1` = 不筛**；`0`（`none`）= 恒真（与 `ConditionEval` 的口径一致）。
        /// 语义 = `FilterMethods__MeetsConditionCriteria`（唯一求值器 = <see cref="ConditionEval"/>）。
        ///
        /// <para>⚠️ **今天没有生产者**（同 <see cref="SpellType"/>）。全池那一条用得上的
        /// （`DA22` 的 `actingCardCriteria.conditionsFilter = 220` ＋ `excludeCondition = 1`）
        /// **已由正文覆盖**：卡面写的是 `If it survives, heal 3 to it` ⇒ `targetsurvives` 条件
        /// （`EffectResolver.ConditionHolds` 那条走 `ConditionEval` 的 220）。
        /// 它在这里的价值是**结构**：以后 `targetCriteria` 落列时，筛目标池这一层不用再改模型。</para>
        /// </summary>
        public int Condition = -1;

        /// <summary>原版 `TargetCriteria.excludeCondition`：真 = 把 <see cref="Condition"/> **取反**
        /// （`MeetsConditionCriteria.c:384-389` 的唯一取反处，与 `ConditionEval` 同一口径）。</summary>
        public bool ExcludeCondition;

        /// <summary>三个维度都没写 = 不筛（任何卡都算符合）</summary>
        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(KindWord)
                    && (KindAnyOf == null || KindAnyOf.Count == 0)
                    && string.IsNullOrEmpty(Keyword)
                    && string.IsNullOrEmpty(Name)
                    && !WarlordOnly
                    && SpellType < 0
                    && Condition < 0;
            }
        }

        /// <summary>
        /// 兵种这一维。**判据只此一处**（两个 <c>Matches</c> 都走它）——
        /// <see cref="KindAnyOf"/> 非空时是**并集**（任一个命中即可），见那一栏的注释。
        /// </summary>
        bool KindMatches(CardDef c)
        {
            if (string.IsNullOrEmpty(KindWord) && (KindAnyOf == null || KindAnyOf.Count == 0)) return true;
            if (!string.IsNullOrEmpty(KindWord) && CreatePool.MatchesKind(c, KindWord)) return true;
            if (KindAnyOf != null)
                foreach (string k in KindAnyOf)
                    if (CreatePool.MatchesKind(c, k)) return true;
            return false;
        }

        /// <summary>
        /// 从**目标短语**（<see cref="EffectTargetSpec"/>）翻过来。
        ///
        /// 为什么要这一层：普通效果写「打在**现在**场上的谁」（`Target`），
        /// 常驻效果写「以后符合条件的那张牌」（`Criteria`）—— 同一句话，两种表示。
        /// 解析器只管产出 `Target`，由这里翻译，**两处（部署时给 / 持续改费）共用一份翻译**。
        /// </summary>
        public static CardCriteria FromTarget(EffectTargetSpec spec)
        {
            if (spec == null) return null;
            // ⚠️ `Kind` 默认是 `any`（`ParseTarget` 开头就填的），还有 `prev` / `warlord` 这类
            //    **不是兵种词**的取值 —— 直接拿来当 `KindWord` 会变成「什么都筛不中」的静默空过。
            //    判据交给 <see cref="CreatePool.IsKindWord"/>（**只此一份**），认不出就当没写。
            string kind = CreatePool.IsKindWord(spec.Kind) ? spec.Kind : null;
            var c = new CardCriteria
            {
                KindWord = kind,
                // 🆕 2026-10-19（第五十五会话 E10）：`warlord` 不是兵种词（`IsKindWord` 为假），
                //   原来会**当没写**⇒ 一条「只给督军」的常驻/光环条件退化成「谁都给」。
                //   现在落到独立维度（判据见 <see cref="WarlordOnly"/>）。
                WarlordOnly = spec.Kind == "warlord",
                Keyword = spec.KeywordFilter,
                Name = spec.NameFilter,
            };
            return c.IsEmpty ? null : c;
        }

        /// <summary>
        /// 筛选条件里的关键词 → 卡表用的那个**规范键**（`huntmark` / `darkpact` …）。
        ///
        /// 🔴 **必须归一化，这是第二层静默失效**（2026-09-13 第三十四轮查出来的）：
        ///    判据这一侧存的是**卡面原话** —— <see cref="WhenEvents.Subjects.Parse"/> 把
        ///    `enemy with Hunt Mark` 的后半段**原样**存进 <see cref="Keyword"/>（`"hunt mark"`，带空格）；
        ///    而卡表那一侧存的是 <see cref="KeywordTable.Normalize"/> 产出的规范键（`"huntmark"`）。
        ///    两边直接比**永远不相等**，而 `Has()` 只返回 false、**不报错**
        ///    （`Stormwolf` / `Wolf Priest` 就是这么「点亮了却一次都不响」的）。
        ///    ⇒ 只把 `CardDef` 换成 `UnitState` 是**不够的**，这一层也得有。
        ///
        /// ⚠️ `Normalize` 认不出时返回 null ⇒ **原样退回**。不这么写的话，一个认得出的普通词
        ///    会因为归一化失败而变成**永不匹配** —— 那是把一个静默失效换成另一个。
        /// </summary>
        static string NormKeyword(string kw)
        {
            if (string.IsNullOrEmpty(kw)) return kw;
            string k = KeywordTable.Normalize(kw);
            return string.IsNullOrEmpty(k) ? kw : k;
        }

        /// <summary>这张卡符不符合。<see cref="IsEmpty"/> 时恒真（不筛 = 都算）。</summary>
        public bool Matches(CardDef c)
        {
            if (c == null) return false;
            if (WarlordOnly && !DeckRules.IsWarlordCardType(c.Type)) return false;
            if (!KindMatches(c)) return false;
            if (!string.IsNullOrEmpty(Keyword) && !c.Has(NormKeyword(Keyword))) return false;
            if (!string.IsNullOrEmpty(Name) && CreatePool.Norm(c.Name) != CreatePool.Norm(Name)) return false;
            if (SpellType >= 0)
            {
                // 🆕 v19（`新账-97` ⑥）：原版 `TargetCriteria.spellType`（`+0x90`）。
                // ⚠️ 卡表没这一列（旧表）时 `CardDef.SpellType = -1` = **判不了** ⇒ 保守判不匹配
                //    （⛔ 别把「不知道」当 `Normal(0)` —— 那会让筛 Secret 的条件把普通卡也放进来）。
                if (c.SpellType < 0 || c.SpellType != SpellType) return false;
            }
            if (Condition >= 0 && !ConditionOk(null, c, null)) return false;
            return true;
        }

        /// <summary>
        /// **场上的那个单位**符不符合。和上面那条**只差关键词那一维**：
        /// 这里问的是 <see cref="UnitState.Has"/> —— 那是「**卡面印的 + 运行时加的**」的**全集**
        /// （`UnitState` 构造时从 `card.Keywords` 种进去，效果再 `AddKeyword` 往上加）。
        ///
        /// 🔴 **为什么要分开**：`Hunt Mark` / `Dark Pact` 这些是**运行时**由效果加上去的
        ///    （`EffectResolver.ApplyOneGain` → `UnitState.AddKeyword`），**卡面上根本没有印**。
        ///    拿 <see cref="Matches(CardDef)"/> 去判 `When an enemy with Hunt Mark dies`
        ///    ⇒ **永远判不中，而且不报错**，卡面也不打 `*`（正文是好的）。
        ///    2026-09-13 第三十四轮派子代理逐条核 `When` 短语时查出来的
        ///    （`Stormwolf` / `Wolf Priest` 两张**已经点亮**的卡其实一次都不会响）。
        ///
        /// ⚠️ 兵种与卡名两维**照旧问卡面** —— 那两维本来就是卡的属性，运行时不会变。
        /// ⚠️ **有 `UnitState` 就别用 `CardDef` 那条**：后者是前者的真子集，只会更漏。
        /// </summary>
        public bool Matches(UnitState u)
        {
            return Matches(u, null);
        }

        /// <summary>同上，外加 `ctx`（🆕 2026-10-23 `新账-97` ⑥：`Condition` 那一维里
        /// `220` / `221` / `222` / `260` 要上下文才判得准；<paramref name="ctx"/> 为 null 时
        /// 那些格子会判不了 ⇒ **本方法判不匹配**，与 `Matches(CardDef)` 同一条保守口径）。</summary>
        public bool Matches(UnitState u, BattleContext ctx)
        {
            if (u == null || u.Card == null) return false;
            if (WarlordOnly && !u.IsWarlord) return false;
            if (!KindMatches(u.Card)) return false;
            if (!string.IsNullOrEmpty(Keyword) && !u.Has(NormKeyword(Keyword))) return false;
            if (!string.IsNullOrEmpty(Name) && CreatePool.Norm(u.Card.Name) != CreatePool.Norm(Name)) return false;
            if (SpellType >= 0 && (u.Card.SpellType < 0 || u.Card.SpellType != SpellType)) return false;
            if (Condition >= 0 && !ConditionOk(ctx, u.Card, u)) return false;
            return true;
        }

        /// <summary>`TargetCriteria.conditionsFilter` 那一维 —— **求值器只此一处**
        /// （<see cref="ConditionEval.MeetsUnit"/> / <see cref="ConditionEval.MeetsCard"/>）。
        /// 返回 `false` = 不匹配 **或** 本版判不了（`null`）—— 保守方向 = 不把候选放进来。</summary>
        bool ConditionOk(BattleContext ctx, CardDef card, UnitState u)
        {
            bool? v = u != null
                ? ConditionEval.MeetsUnit(ctx, u, Condition, ExcludeCondition)
                : ConditionEval.MeetsCard(ctx, card, -1, Condition, ExcludeCondition);
            return v == true;
        }

        /// <summary>日志要打人话（`所有 drone` / `带 destroyer 的` / `名为 stormboy 的`）</summary>
        public override string ToString()
        {
            var parts = new List<string>();
            if (WarlordOnly) parts.Add("督军");
            if (!string.IsNullOrEmpty(KindWord)) parts.Add(KindWord);
            if (KindAnyOf != null)
                foreach (string k in KindAnyOf) parts.Add(k);
            if (!string.IsNullOrEmpty(Keyword)) parts.Add("带 " + Keyword);
            if (!string.IsNullOrEmpty(Name)) parts.Add("名为 " + Name);
            if (SpellType >= 0) parts.Add("spellType=" + SpellType);
            if (Condition >= 0) parts.Add((ExcludeCondition ? "不满足 " : "满足 ") + "cond=" + Condition);
            if (parts.Count == 0) return "（不限）";
            return string.Join(" + ", parts.ToArray());
        }
        // ======================================================================================
        //  🆕 2026-10-26（批次 9 · 执行代理 `W6K11` · 账 `收藏:189` / `收藏:201`）
        //  卡面级（raw card）criteria 的两个公开入口 —— **实现只有一份**，在
        //  `TargetCriteriaEval.MatchesRawCard` / `RawCardMeetsTraitCriteria`（那里同时放着
        //  `MeetsStatCriteria` / `ArmyPassesFor` / `TypeGatePasses` / `TraitNameOf` 那批分项，
        //  棋盘路与卡面路共用）。本类对外的口统一叫 `CardCriteria`，调用点不必去摸 `TargetCriteriaEval`。
        // ======================================================================================

        /// <summary>原版 `FilterMethods.CheckIfRawCardMeetsCriteria`（账 `收藏:189`）：
        /// 一张**卡面**（`CardDef`）过不过这份 36 格 criteria。
        /// <para>判据全文与逐格次序 → <see cref="TargetCriteriaEval.MatchesRawCard"/>（唯一实现）。
        /// ⚠️ 它读**卡面值**（`Attack` / `Health` / `Cost` / `Rarity` / `CardTraits`），与
        /// <see cref="Matches(UnitState, BattleContext)"/>（**当前值** + 运行时 traits）**不是同一个口**。</para>
        /// <para>`null` = 某一格判不了（`gap` 写明是哪一格）；`criteria == null` ⇒ `true`（没有 criteria 行 = 不筛）。</para></summary>
        public static bool? MatchesRawCard(CardDef card, CardDef.CountSetSpec criteria, out string gap)
            => TargetCriteriaEval.MatchesRawCard(card, criteria, out gap);

        /// <summary>原版 `FilterMethods.RawCardMeetsTraitCriteria`（账 `收藏:201`）：
        /// `traitsFilter` / `excludeTrait` / **summonRift 门** 三句。
        /// <para>判据全文 → <see cref="TargetCriteriaEval.RawCardMeetsTraitCriteria"/>（唯一实现）。
        /// ⚠️ 这里读**默认 traits**（`CardDef.CardTraits`，原版 `+0x230`）；
        /// 棋盘那一支读的是**当前** traits（`+0x128` 的等价物）—— ⛔ 两个口别混用。</para></summary>
        public static bool? RawCardMeetsTraitCriteria(CardDef card, CardDef.CountSetSpec criteria, out string gap)
            => TargetCriteriaEval.RawCardMeetsTraitCriteria(card, criteria, out gap);
    }
}
