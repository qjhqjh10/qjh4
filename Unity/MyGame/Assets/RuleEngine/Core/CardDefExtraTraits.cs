// CardDefExtraTraits.cs — 原版 `RawCardScript.GetExtraAbilityTraits()` 的照搬（`CardDef` 的 partial 半边）
//
// 🔴 **为什么单独开一个文件**（同 `CardDefActiveAbility.cs` / `CardDefCostPerKeyword.cs` 的纪律）：
//   `CardDef.cs` 类尾之后还有 `KeywordTable` / `TraitReachability`，账里有一批按【行号】的落点
//   ⇒ 在 `CardDef` 类尾插成员会让它们整体漂移。本文件走 partial（零行号漂移）。
//   `Core/*.cs` 是**通配**编进 `RuleCore` / `ruleprobe` 的。
//
// ==================================================================================================
//  账 `战斗逻辑驱动:1228 RawCardScript.GetExtraAbilityTraits()`
// ==================================================================================================
//  判据（第一权威 = `d:/2/tools/decomp_full/`，全文逐句读过）：
//    · `RawCardScript__GetExtraAbilityTraits.c:1-120` —— 两步：
//        ① `CardTraitCollection.GetTraitsFromAbilitiesRawCard(cardArmy(+0x94))` 取一张 **25 对 (trigger→trait)** 表，
//           逐对看这张卡的 `cardAbilities(+0x290)` 里有没有 `trigger == 该 trigger` 的 ability，命中就
//           `CardTraitData___ctor(traitValue)` 收进结果；
//        ② 若 `hasActiveAbility(+0x2a9)` 为真 ⇒ 再按阵营档补一条
//           `GetTraitForActiveAbilityRawCard(cardArmy)`（1275 oath / 1185 pray / 1220 duty / 1235 agenda / 1265 ferocity / 其余 400）。
//    · `CardTraitCollection__GetTraitsFromAbilitiesRawCard.c:1-56` —— **25 对表**（打包成 `trait<<32 | trigger`
//      的 25 个常量，逐条解码）；**阵营 20 Goff / 40 Sautekh 直接返回空表**（`param_1 == 0x14 || param_1 == 0x28`）。
//    · `RawCardScript__CheckCurrentTraits.c:53-95` —— 唯一调用者，把派生 trait **追加进 SO 的 CardTraits**
//      （`if (CardTraits.All(x => x != T)) CardTraits.Add(T)`）。⚠️ 该调用者自身**全树 0 调用点**
//      （`E8`/`B87-R21` 三源确认）⇒ 属「原版出货 0 入口」，但按调度台 2026-10-26 裁定「死代码不足以判 D」⇒ 照搬。
//
//  **验收尺子（账 1228 备注）**：池内 77 张 `hasActiveAbility` 卡里 76 张卡 SO 本来就带那条阵营 trait；
//  按本语义算出「原版运行时会补、而卡面本来没有」的集合 = **508 张 / 514 条**（绝大多数 `rally`，
//  trigger 0 `ThisCardPlayed`）。详见 `资料/完全复刻/报告/B8_AX_残骸trait与卡定义.md` §四。
//
//  ⚠️ **本 API 只出「派生集合」，不往 `_keywords` 写**：原版是追加进 SO 的 `CardTraits`，而我们的
//    `_keywords` 是**卡面解析结果**（改了会污染 `Has(…)` 的所有读点）。消费者（表现层 / 将来的
//    `CheckCurrentTraits` 等价物）读本属性即可。
//
//  ⚠️ **`AbilityTriggerPresence == -1`（旧表/缺列）时本对不判「命中」**（宁可少补，也不猜「有」）——
//    现读 v17 卡表 `abilityTriggers` 列 1129/1129 都在 ⇒ 池内是 0/1、无 -1 档。
using System.Collections.Generic;

namespace RuleEngine
{
    public partial class CardDef
    {
        /// <summary>一条「派生 trait」（原版 `CardTraitData`）：数值照搬 + 我们的 `KeywordTable` 规范化名。</summary>
        public sealed class ExtraAbilityTrait
        {
            /// <summary>原版 `DefinedTrait` 数值（逐字照 `.c`）。</summary>
            public int TraitValue;
            /// <summary>我们 `KeywordTable` 的规范化名（`rally` / `backlash` …；`rage` 在我们的词表里归
            /// `penitence`，见 `KeywordTable.Prefixes`）—— 方便消费者与 `Has(…)` 对齐。
            /// ⚠️ 只有 ② 那一档的 `400 activeAbility` 为 `null`（泛称、没有对应卡面关键词）。</summary>
            public string Keyword;
            /// <summary>命中它的那个 trigger 的原版名（`ThisCardPlayed` / `ThisUnitDeath` …）。
            /// `null` = ② 那一步（`hasActiveAbility` 补一条）来的（不对应任何 trigger）。</summary>
            public string TriggerName;
            public override string ToString()
            {
                return Keyword + "(" + TraitValue + ")" + (TriggerName == null ? "[activeAbility]" : "<-" + TriggerName);
            }
        }

        // ---- 25 对 (trigger, trait)。打包值 = trait<<32 | trigger（照 `.c` 的常量逐条解码） ----
        // 三列平行数组（trigger 数值 · trait 数值 · trigger 名 · 我们的关键词名）。
        static readonly int[] ExtraTraitTriggers =
        {
            0, 80, 81, 120, 430, 440, 145, 130, 230, 45, 46, 220, 300, 301, 550, 560, 580, 590, 655, 670, 690, 680, 710, 715, 720,
        };
        static readonly int[] ExtraTraitValues =
        {
            10, 120, 1010, 260, 760, 770, 1000, 280, 450, 840, 845, 470, 920, 1225, 960, 1040, 1110, 1130, 1185, 1200, 1230, 1210, 1245, 1250, 1255,
        };
        static readonly string[] ExtraTraitTriggerNames =
        {
            "ThisCardPlayed", "ThisUnitDeath", "Unstable", "Slay", "Requiem", "Landing", "Codex", "Rage", "Relentless",
            "Resolution", "Talent", "Sacrifice", "Mob", "Regiment", "Support", "Artifice", "Strike", "Combo", "Pray",
            "Ambush", "Teleport", "Uprising", "Stimulation", "Ecstasy", "Cruelty",
        };
        static readonly string[] ExtraTraitKeywords =
        {
            "rally", "backlash", "unstable", "slay", "requiem", "landing", "codex", "penitence", "relentless",
            "resolution", "talent", "sacrifice", "mob", "regiment", "support", "artifice", "strike", "combo", "pray",
            "ambush", "teleport", "uprising", "stimulation", "ecstasy", "cruelty",
        };

        /// <summary>卡面阵营 → 原版 `CardArmy` 数值（判据 = `dump.cs:45634-45652`；未知/中立 ⇒ 0）。</summary>
        public static int FactionArmyValue(string faction)
        {
            switch (faction)
            {
                case "Ultramarines":    return 10;
                case "Goff":            return 20;
                case "SaimHann":        return 30;
                case "Sautekh":         return 40;
                case "BlackLegion":     return 50;
                case "Leviathan":       return 60;
                case "TauEmpire":       return 70;
                case "Sororitas":       return 80;
                case "Genestealers":    return 90;
                case "AstraMilitarum":  return 100;
                case "DarkAngels":      return 110;
                case "EmperorsChildren": return 120;
                case "SpaceWolves":     return 130;
                default:                return 0;
            }
        }

        List<ExtraAbilityTrait> _extraAbilityTraits;

        /// <summary>
        /// 原版 `RawCardScript.GetExtraAbilityTraits()`：按阵营取 25 对 (trigger→trait) 表、命中就收，
        /// `hasActiveAbility` 为真再补一条阵营档主动能力 trait。懒算一次。
        /// ⚠️ 阵营 20 Goff / 40 Sautekh ⇒ **25 对表为空**（照 `.c` 原样；`hasActiveAbility` 那条仍会加）。
        /// </summary>
        public IReadOnlyList<ExtraAbilityTrait> ExtraAbilityTraits
        {
            get
            {
                if (_extraAbilityTraits != null) return _extraAbilityTraits;
                var acc = new List<ExtraAbilityTrait>();
                int army = FactionArmyValue(Faction);

                // ① 25 对表（Goff / Sautekh 两档直接空表 —— 照 `.c:14-16`）
                if (army != 20 && army != 40)
                {
                    for (int i = 0; i < ExtraTraitTriggers.Length; i++)
                    {
                        if (AbilityTriggerPresence(ExtraTraitTriggers[i]) != 1) continue;
                        acc.Add(new ExtraAbilityTrait
                        {
                            TraitValue = ExtraTraitValues[i],
                            Keyword = ExtraTraitKeywords[i],
                            TriggerName = ExtraTraitTriggerNames[i],
                        });
                    }
                }

                // ② hasActiveAbility ⇒ 再按阵营档补一条（判据 = `.c` 末段 + `GetTraitForActiveAbilityRawCard.c`）
                if (HasActiveAbility == 1)
                {
                    var spec = ActiveAbilityTraitForFaction(Faction);
                    acc.Add(new ExtraAbilityTrait
                    {
                        TraitValue = spec.TraitValue,
                        Keyword = spec.Keyword,     // 400 activeAbility 那一档为 null（泛称，无对应卡面关键词）
                        TriggerName = null,
                    });
                }

                _extraAbilityTraits = acc;
                return acc;
            }
        }
    }
}
