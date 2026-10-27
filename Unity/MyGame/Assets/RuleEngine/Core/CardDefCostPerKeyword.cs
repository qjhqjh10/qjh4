// CardDefCostPerKeyword.cs — 「这张卡每有 X 就自降 N 费」的数据半边（`CardDef` 的 partial）
//
// 🔴 **为什么单独开一个文件，而不是写进 `CardDef.cs` 类尾**（与 `CardDefActiveAbility.cs` 同一条纪律）：
//   `CardDef.cs` 类尾之后还有 `KeywordTable` / `TraitReachability` 两个类，账里有一批按【行号】的落点
//   （现读最大 = `CardDef.cs:7864`）⇒ 在 `CardDef` 类尾插成员会让它们整体漂移。
//   ⇒ `CardDef` 这一半走新文件（**零行号漂移**）；`Core/*.cs` 是**通配**编进 `RuleCore/ruleprobe` 的
//     ⇒ 新文件自动被带上（`Data/CardDatabase.cs` 才是显式列出的那一个）。
//
// ==================================================================================================
//  账 669 `BroadcastWhileInPlay` 的**手牌半**：`This card costs N less for each <X>`
// ==================================================================================================
//  **它是什么**：卡面 `This card costs N less for each <关键词> on <谁> <units/troops>`
//  —— 这张卡**在手牌里**就随盘面状态**自己变便宜**（不是「打出去时降一次」）。
//
//  **为什么必须单开一格**（账 669 / `报告/A6_B03_动作泵与AI.md` §四·① 的判据）：
//    · 原版等价物 = `BroadcastWhileInPlay` 的**手牌扫描** → `UpdateWhileInPlay` → `AddHandEffect`
//      （`Core/Aura.cs:180-187` 有同源说明）；
//    · 我们的 `lowercost` op **唯一消费点**是 `EffectResolver.DoLowerCost`，而它由 `ResolveOps`
//      在**付费之后**才跑（`Core/EffectResolver.cs:1996` 判价 → `:2532` 扣费 → 之后才结算 ops）
//      ⇒ **手牌价永远不减**。所以要一条**每次现算**的数据，交给 `RuleCore.CostOf` 读。
//
//  🔴 **与已有三格的边界**（⛔ 别合并、别互相顶）：
//    · `CardDef.CostIfControls`（`This costs N less if you control a unit with <词>`）= **有没有**这类单位，布尔档；
//    · `CardDef.CostPerEnemyHandCard`（`Costs 1 less for each card in enemy hand`）= **数手牌张数**，非关键词；
//    · `BattleContext.CostMods`（`DoLowerCost` 登记）= **一次性、可过期**的快照，不是常驻；
//    · **本格** = **数盘面上某个关键词的层数**（`UnitState.KwValue`，可叠加），侧 + 兵种可筛。
//
//  **现池实读（2026-10-27 · `RuleEngine/Resources/cards_engine.json` v35 / 1129 张）**：
//    这个句式只有两张 ——
//      · `BL52 Unholy Smite`   `1 less for each Dark Pact on friendly units`
//      · `SW58 Fenrisian Wolfpack` `1 less for each Hunt Mark on enemy troops`
//    ⚠️ `GSC37 Patriarch` 的 `Costs 1 less for each card in enemy hand` 走的是 `CostPerEnemyHandCard`
//      （**句子是 `Costs …`、没有 `this card`**）⇒ 本格的形状正则**不吃它**，不会重复降费。
//
//  ⚠️ **解析为什么不复用 `EffectText.ClassifyCount`**：那条路把 `Hunt Mark on enemy troops`
//    归成 `countRef=enemy|troop|all` + `countScope=board` —— **关键词（Hunt Mark）丢掉了**
//    （`ruleprobe card "Fenrisian Wolfpack"` 现读；`ClassifyCount` 的 board 档不记关键词）。
//    只有 `dark pact` 因为 `ClassifyCount` 里那条**专用分支**才保留成 `countScope=darkpact`。
//    ⇒ 本格按**卡面原文**自己认「关键词 + 侧 + troops」三样（⛔ `EffectText.cs` 不属本批白名单）。
//
//  🔴 **消费方（本文件不做）**：`RuleCore.CostOf` 里、**在 `CostMods.Count == 0` 早退之前**加一段
//    `for (int i = 0; i < c.CostPerKeywords.Count; i++) { var r = c.CostPerKeywords[i]; v += r.Delta * r.Count(ctx, owner); }`
//    （与 `CostIfControls` / `CostPerEnemyHandCard` 并排、同一条纪律：**每次现算**、**不登记 `CostMods`**）。
//    ⛔ `CostOf` 在 `Core/RuleCore.cs`（执行代理 E1 的文件），本批不动。
//
//  **自检/探针**：`CardDef.UnmodeledCostPerCountBodies` 列出「认得出句式、但认不出关键词」的整句
//    （正常应为空）—— 给「不许静默失败」一个口子。
//
using System.Collections.Generic;

namespace RuleEngine
{
    public partial class CardDef
    {
        /// <summary>`This card costs N less for each &lt;关键词&gt; on &lt;谁&gt; &lt;units/troops&gt;` 的一条。</summary>
        public sealed class CostPerKeyword
        {
            /// <summary>**每个计数单位降多少**（负数 = 降价，与 `CostIfControl.Delta` 同号约定）
            /// ⇒ 消费者写 `v += Delta * Count(ctx, owner)`（**别自己再取负**）。</summary>
            public int Delta;

            /// <summary>被数的关键词（`KeywordTable` 的规范化名，如 `darkpact` / `huntmark`）——
            /// 按 **`UnitState.KwValue` 求和**（层数可叠加），⛔ 不是「有没有」的布尔。</summary>
            public string Keyword;

            /// <summary>`own` = 自己那一侧（卡面 `friendly` / `your`）；`enemy` = 对手那一侧。</summary>
            public string Side;

            /// <summary>卡面写 `troops` ⇒ true（**督军不算**，判据同 `EffectResolver.CountFor` 的
            /// `kind == "troop" &amp;&amp; u.IsWarlord`）；写 `units` ⇒ false（督军算）。
            /// 判据首选 = 成品卡图卡面英文原文（`d:/2/Warpforge部队卡片/`）。</summary>
            public bool TroopsOnly;

            /// <summary>整句原文（对账 / 日志用）。</summary>
            public string Body;

            /// <summary>
            /// **数出几个**（= 该侧棋盘上带 <see cref="Keyword"/> 的单位，其 `KwValue` 之和）。
            /// 侧 / 兵种两道筛见各字段。`ctx == null` ⇒ 0（**不是**「判不了」——本格没有「未知」档）。
            /// </summary>
            public int Count(BattleContext ctx, int owner)
            {
                if (ctx == null || string.IsNullOrEmpty(Keyword)) return 0;
                int side = Side == "enemy" ? 1 - owner : owner;
                if (side < 0 || side > 1) return 0;
                var p = ctx.Players[side];
                if (p == null) return 0;
                int n = 0;
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = p.Board[s];
                    if (u == null) continue;
                    if (TroopsOnly && u.IsWarlord) continue;
                    n += u.KwValue(Keyword);
                }
                return n;
            }

            public override string ToString()
            {
                return (Side == "enemy" ? "敌方" : "我方") + (TroopsOnly ? "部队" : "单位")
                     + "每有 1 层 " + Keyword + " → 费用 " + Delta;
            }
        }

        /// <summary>`huntmark` 的关键词名 —— `KeywordTable` 里**还没有**这个常量（`Keywords` 数组与
        /// `Prefixes` 用的是字面量 `"huntmark"`），本格自带一份；⛔ 别处要用请走 `KeywordTable`（铁律 6）。</summary>
        const string KwHuntMarkName = "huntmark";

        /// <summary>本格识别的那条句式（只认「这张卡」当主语 —— `Costs 1 less …` 归 `CostPerEnemyHandCard`）。</summary>
        static readonly System.Text.RegularExpressions.Regex ReSelfCostPerKeyword =
            new System.Text.RegularExpressions.Regex(
                @"^this\s+card\s+costs?\s+(\d+)\s+less\s+for\s+each\s+(.+)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.Compiled);

        List<CostPerKeyword> _costPerKeywords;
        List<string> _unmodeledCostPerCount;
        bool _costPerKeywordsBuilt;

        /// <summary>本卡的「每有 X 自降费」列表（**懒算一次** —— ⛔ 不在构造里算，避免改
        /// `CollectTriggerOps` 的调用序；`Desc`/`_keywords` 构造完即不变，懒算安全）。
        /// 消费点见本文件头（`RuleCore.CostOf`，本批不动）。</summary>
        public IReadOnlyList<CostPerKeyword> CostPerKeywords
        {
            get { BuildCostPerKeywords(); return _costPerKeywords; }
        }

        /// <summary>
        /// **认得出句式、但认不出「关键词 + 侧」的那些整句**（正常应为空）。给探针/自检一个
        /// 「有没有被静默漏掉」的口子（铁律：不许静默失败）。
        /// </summary>
        public IReadOnlyList<string> UnmodeledCostPerCountBodies
        {
            get { BuildCostPerKeywords(); return _unmodeledCostPerCount; }
        }

        void BuildCostPerKeywords()
        {
            if (_costPerKeywordsBuilt) return;
            _costPerKeywordsBuilt = true;
            var acc = new List<CostPerKeyword>();
            var un = new List<string>();
            foreach (string seg in EffectText.Split(Desc)) AddCostPerKeyword(seg, acc, un);
            // 卡面正文也可能整条写在 `keywords` 里（与 `CollectCostIfControl` 同一条纪律）
            foreach (string kw in _keywords.Keys) AddCostPerKeyword(kw, acc, un);
            _costPerKeywords = acc;
            _unmodeledCostPerCount = un;
        }

        void AddCostPerKeyword(string seg, List<CostPerKeyword> acc, List<string> un)
        {
            if (string.IsNullOrEmpty(seg)) return;
            string s = seg.Trim();
            var m = ReSelfCostPerKeyword.Match(s);
            if (!m.Success) return;

            int n = int.Parse(m.Groups[1].Value);
            string tail = m.Groups[2].Value.Trim().TrimEnd('.').Trim();
            string low = tail.ToLowerInvariant();

            string keyword = null;
            if (low.Contains("dark pact") || low.Contains("darkpact")) keyword = KeywordTable.DarkPact;
            else if (low.Contains("hunt mark") || low.Contains("huntmark")) keyword = KwHuntMarkName;

            if (keyword == null)
            {
                // 认不出被数的关键词 ⇒ **如实登记**（不改价，也⛔ 不猜一个）
                if (un != null && !un.Contains(s)) un.Add(s);
                return;
            }

            var rec = new CostPerKeyword
            {
                Delta = -n,
                Keyword = keyword,
                Side = low.Contains("enemy") ? "enemy" : "own",
                TroopsOnly = low.Contains("troop"),
                Body = s,
            };
            // `Desc` 与 `keywords` 可能都写了同一句 ⇒ 按 (Delta,Keyword,Side,TroopsOnly) 去重
            foreach (var e in acc)
                if (e.Delta == rec.Delta && e.Keyword == rec.Keyword
                    && e.Side == rec.Side && e.TroopsOnly == rec.TroopsOnly)
                    return;
            acc.Add(rec);
        }

        // ==================================================================
        //  🆕 2026-10-27（B88_E4 · 引擎·小件 · 账 993）：**按阵营推导「主动能力对应的那条 trait」**
        // ==================================================================
        //
        //  **原版**：`CardTraitCollection.GetTraitForActiveAbilityRawCard(CardArmy)` —— 整函数 27 行、
        //  纯映射（`decomp_full/CardTraitCollection__GetTraitForActiveAbilityRawCard.c:5-24`，逐句读过）：
        //    · `10 Ultramarines`   → `0x4fb = 1275`（oath）
        //    · `80 Sororitas`      → `0x4a1 = 1185`（pray）
        //    · `100 AstraMilitarum`→ `0x4c4 = 1220`（duty）
        //    · `110 DarkAngels`    → `0x4d3 = 1235`（agenda）
        //    · `130 SpaceWolves`   → `0x4f1 = 1265`（ferocity）
        //    · 其余（含 `0 Neutral`）→ `400`（`activeAbility`，泛称、不是卡面印的 trait）
        //  三个**活调用点全在表现层**（`CardScript__ActivateIconAnim.c` ·
        //  `CardScript__ActivateTriggerTraitAnim.c` · `UnitOnBoardAttackTypeSelector__Toggle.c`）——
        //  ⇒ 它是「**显示**主动能力的图标/动画时，按阵营补哪条 trait」，**不是**真的往卡上加 trait。
        //
        //  🔴 **为什么加在 `CardDef` 侧**：池内 `hasActiveAbility == 1` 的 **77 张**里 **76 张**
        //  自带阵营档那条 trait，**唯一反例 `SOR66 Armorium Cherub`**（Sororitas，卡面 trait 只有
        //  rally / flank）⇒ 原版运行时会给它补 `pray(1185)`、我们不会，图标/动画今天就差一条。
        //  ⚠️ **⛔ 别只把 SOR66 写进卡表**（那会把「原版有推导」降级成「我们手填」），
        //  也⛔ **别把推导结果塞进 `_keywords`**（原版没往卡上加，塞了就改变所有 `Has(…)` 读点）。
        //
        //  **消费者（本批不动）**：三处表现层调用点（`CardPresentation/`，非本批白名单）。
        //  本处只给「数据 + 推导」，谁接谁读 <see cref="ActiveAbilityTrait"/> 即可。

        /// <summary>一条「主动能力 trait」的推导结果。</summary>
        public sealed class ActiveAbilityTraitSpec
        {
            /// <summary>原版 `DefinedTrait` 数值（`1275/1185/1220/1235/1265/400`）。</summary>
            public int TraitValue;
            /// <summary>我们 `KeywordTable` 的规范化名（`oath`/`pray`/`duty`/`agenda`/`ferocity`）；
            /// `null` = 原版那档是 `400 activeAbility`（泛称，没有对应的卡面关键词）。</summary>
            public string Keyword;
            /// <summary>卡面**是不是已经带了**这条 trait（带 ⇒ 表现层别重复补）。</summary>
            public bool AlreadyOnCard;
            public override string ToString()
            {
                return "主动能力 trait " + TraitValue + (Keyword == null ? "(activeAbility)" : "(" + Keyword + ")")
                     + (AlreadyOnCard ? " · 卡面已有" : " · 卡面没有、按阵营补");
            }
        }

        /// <summary>
        /// **纯映射**（照 `.c` 六档；不做任何闸）—— 表现层/探针想要原始结果时用它。
        /// </summary>
        public static ActiveAbilityTraitSpec ActiveAbilityTraitForFaction(string faction)
        {
            int value;
            string keyword;
            switch (faction)
            {
                case "Ultramarines":    value = 1275; keyword = KeywordTable.Oath;     break;
                case "Sororitas":       value = 1185; keyword = KeywordTable.Pray;     break;
                case "AstraMilitarum":  value = 1220; keyword = KeywordTable.Duty;     break;
                case "DarkAngels":      value = 1235; keyword = KeywordTable.Agenda;   break;
                case "SpaceWolves":     value = 1265; keyword = KeywordTable.Ferocity; break;
                default:                value = 400;  keyword = null;                  break;
            }
            return new ActiveAbilityTraitSpec { TraitValue = value, Keyword = keyword };
        }

        /// <summary>
        /// **卡级结果**：`HasActiveAbility != 1` ⇒ `null`（这张卡没有主动能力，谈不上「主动能力 trait」——
        /// 这一步闸是 B9 施工单 993 的步骤②，原版函数本身没有，由调用点承担）。
        /// 否则给纯映射结果，并把 `AlreadyOnCard` 按卡面 `Has(Keyword)` 填好。
        /// </summary>
        public ActiveAbilityTraitSpec ActiveAbilityTrait
        {
            get
            {
                if (HasActiveAbility != 1) return null;
                var spec = ActiveAbilityTraitForFaction(Faction);
                spec.AlreadyOnCard = spec.Keyword != null && Has(spec.Keyword);
                return spec;
            }
        }
    }
}
