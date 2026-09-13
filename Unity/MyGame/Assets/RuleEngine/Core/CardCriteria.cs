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

        /// <summary>三个维度都没写 = 不筛（任何卡都算符合）</summary>
        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(KindWord)
                    && string.IsNullOrEmpty(Keyword)
                    && string.IsNullOrEmpty(Name);
            }
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
            if (!string.IsNullOrEmpty(KindWord) && !CreatePool.MatchesKind(c, KindWord)) return false;
            if (!string.IsNullOrEmpty(Keyword) && !c.Has(NormKeyword(Keyword))) return false;
            if (!string.IsNullOrEmpty(Name) && CreatePool.Norm(c.Name) != CreatePool.Norm(Name)) return false;
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
            if (u == null || u.Card == null) return false;
            if (!string.IsNullOrEmpty(KindWord) && !CreatePool.MatchesKind(u.Card, KindWord)) return false;
            if (!string.IsNullOrEmpty(Keyword) && !u.Has(NormKeyword(Keyword))) return false;
            if (!string.IsNullOrEmpty(Name) && CreatePool.Norm(u.Card.Name) != CreatePool.Norm(Name)) return false;
            return true;
        }

        /// <summary>日志要打人话（`所有 drone` / `带 destroyer 的` / `名为 stormboy 的`）</summary>
        public override string ToString()
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(KindWord)) parts.Add(KindWord);
            if (!string.IsNullOrEmpty(Keyword)) parts.Add("带 " + Keyword);
            if (!string.IsNullOrEmpty(Name)) parts.Add("名为 " + Name);
            if (parts.Count == 0) return "（不限）";
            return string.Join(" + ", parts.ToArray());
        }
    }
}
