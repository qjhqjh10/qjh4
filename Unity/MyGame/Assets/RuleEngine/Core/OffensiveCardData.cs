// OffensiveCardData.cs — **进攻卡（环境卡）的 `cardAbilities` 真值表**（`Core/` 那一半）· 账 `A1547`
//
// ── 这是什么 ────────────────────────────────────────────────────────────
// 原版把「进攻卡」也做成 `RawCardScript`（`spellType = 200 OffensiveCard`、`cardType = 20 Tactic`），
// 它的能力挂在 `cardAbilities[].trigger` 上。全快照里 `trigger == 15`
// （= `AbilityTrigger.PlayedThirdCardInTurn`）**只有 2 张卡**：
//   · `Z-DA-Asteroid Zone_enchantment`（`uniqueId db34f054e134c094aaf92e7a6ae124c0`，1 条能力 / 1 条 logic）
//   · `Z-EmperorsChildren-Empyric Rift_enchantment`（`430c733f7e6de72489e2f0586ccc8d3d`，2 条能力 / 共 4 条 logic）
// 判据 = `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（1195 张全字段 SO 快照；
//   `"trigger"` 全文 1310 次，逐张 == 15 的只有上面这 2 张）。
//
// 🔴 **这两张卡不在 `cards_engine.json` 里**（2026-10-17 现读 1127 张，按 `name` 扫
//    `Asteroid Zone` / `Empyric Rift` **各 0 命中**）—— 它们是**进攻卡**，引擎里由
//    `BattleContext.OffensiveSlotIdx / OffensiveEnvSO` 表示，**不走卡池**。
//    ⇒ 本表按 **`envSO`** 建键（= `CardPresentation/Resources/OffensiveCards.json` 的 `envSO`，
//      也是 `RuleCore.ChooseOffensiveCard` 的第 4 实参 / `BattleDriver.EnvSOFor` 的返回值）
//      —— 正好接在现成那条链上，**不需要动 `BattleDriver`**。
//
// ── 触发时机（原版第一权威）────────────────────────────────────────────
// `d:/2/tools/decomp_full/RawCardScript__OnCardPlayedWithTarget.c:105-112`：
//   `if (thisCard != cardPlayed && BattleManager.CardsPlayedInTurn(bm) == 3)
//        TriggerAbilities(this, 0xf, bm, thisCard, cardPlayed, targetCard, …)`
// ⇒ **本回合第 3 张牌打出时，在进攻卡自己身上**跑它 `trigger == 15` 的能力。
// ⚠️ `HasPlayedThirdCardInTurn`（`RawCardScript__HasPlayedThirdCardInTurn.c:3-56`）读的正是
//    `RawCardScript + 0x290 cardAbilities[].trigger == 0xf` —— ⛔ **不是**「打过三张牌」的计数。
//    本文件的 `ThirdCardAbilities` 就是那一句的移植（判据只此一处）。
//
// ── 为什么 `Core/` 这一半只有 API、没有装载 ──────────────────────────
// 与 `Core/TalentCardData.cs` / `Core/RelatedCardData.cs` 同一条纪律：`Core/` 不许碰
// `Resources` / `JsonUtility`（离屏探针 `工具/ruleprobe` 只编 `Core/*.cs`）。
// 装载那一半在 `Data/OffensiveCardMap.cs`（同一个 `partial class` 的另一半）。
//
// ⚠️ **卡面文案不在本地**：两条 SO 的 `cardDescriptionTextReferenceId` /
//    `activeAbilityTextReferenceId` 都为空，本地化表在远端（`铁律 11 例外②`）⇒ 本表**没有 desc**。
//    `nameZh` 取**官方** `Card_Name/DA62` / `Card_Name/EC64` 词条（真实值，不是编的）。
//    游戏里要显示「这句能力做什么」时**只能先按逻辑/占位**，见 `PlayedThirdCardInTurn.cs` 的日志。
using System;
using System.Collections.Generic;

namespace RuleEngine
{
    /// <summary>一条 `AbilityLogic` —— 只留结算要用的那几格（字段名沿用原版，方便回查）。</summary>
    [Serializable]
    public sealed class OffensiveLogic
    {
        /// <summary>`AbilityEffect` 的**名字**（`doDamage` = 10 / `drawCard` = 0 …）。
        /// ⛔ 存名字不存数字 —— 数字要再查一次枚举表，而 `AbilityEffect.cs` 就在本地。</summary>
        public string effect;

        /// <summary>`PlayersAffected`：`Both` / `Self` / `Enemy`。
        /// 🔴 **只对 `drawCard` 有意义** —— 原版 `AbilityLogic.cs` 的 Tooltip 写的是
        /// 「DrawCard, DiscardWholeHand」；`doDamage` 打哪一侧由 `friendlyUnits` / `enemyUnits` 决定。</summary>
        public string playersAffected;

        /// <summary>`TargetsAffected`：`any` / `allFiltered` …（`TargetsAffected.cs`）。</summary>
        public string targetsAffected;

        public bool friendlyUnits, enemyUnits;
        public bool includeHero, includeMinions, includeRemnants, includeSelf;
        public int maxTargets;
        public int minDamage, maxDamage;
        /// <summary>`DamageType`：`Normal` = 0 / `Random` = 5 …（`DamageType.cs`）。</summary>
        public string damageType;
        /// <summary>`ChooseValueOptions`：`Normal` = 0 / `RandomizedOnResolution` = 90 …（`ChooseValueOptions.cs`）。</summary>
        public string valueOptions;
        /// <summary>原版 `cardAnimations[0]` 的 GUID（**只落值、引擎不播**：演出那一跳归特效/动画线）。</summary>
        public string anim;

        public bool IsDamage { get { return effect == "doDamage"; } }
        public bool IsDraw { get { return effect == "drawCard"; } }
    }

    [Serializable]
    public sealed class OffensiveAbility
    {
        /// <summary>15 = `AbilityTrigger.PlayedThirdCardInTurn`（原版枚举）。</summary>
        public int trigger;
        public int priority;
        public int triggerValue;
        public OffensiveLogic[] logic;
    }

    [Serializable]
    public sealed class OffensiveCardEntry
    {
        /// <summary>键：`OffensiveCards.json` 的 `envSO` / `ChooseOffensiveCard` 的第 4 实参。</summary>
        public string envSO;
        /// <summary>原版 SO 的 `m_Name`（可复查；形如 `Z-DA-Asteroid Zone_enchantment`）。</summary>
        public string soName;
        public string uniqueId;
        public string name;
        /// <summary>官方中文名（`Card_Name/DA62` = 小行星带 / `Card_Name/EC64` = 帝王裂隙）。</summary>
        public string nameZh;
        /// <summary>`cardArmy` 枚举名（110 = DarkAngels / 120 = EmperorsChildren）。</summary>
        public string army;
        public OffensiveAbility[] abilities;
    }

    /// <summary>
    /// `envSO → 进攻卡的 cardAbilities`。**没载到 / 没这条键一律返回 `null`**（调用方什么都不做，
    /// 原版 1195 张里也只有 2 张带 `trigger == 15`）。
    /// </summary>
    public static partial class OffensiveCardData
    {
        /// <summary>15 = `AbilityTrigger.PlayedThirdCardInTurn`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs`）。</summary>
        public const int AbilityTriggerPlayedThirdCardInTurn = 15;

        /// <summary>运行时那份 JSON 的 `Resources` 路径（**没有扩展名**）。</summary>
        public const string ResourcePath = "offensive_card_abilities";

        /// <summary>`Data/OffensiveCardMap.cs` 里那一半的实现（`Resources.Load` + `JsonUtility`）。
        /// 离屏探针里没有实现 ⇒ 调用被抹掉 ⇒ 永远「没载到」（**故意**的，同 `TalentCardData`）。</summary>
        static partial void LoadFromUnity();

        static Dictionary<string, OffensiveCardEntry> _map = null;

        /// <summary>表里有多少张（0 = 没载到 —— 那不是「原版没有」，是**我们没取到**，别混）。</summary>
        public static int TableCards { get { LoadFromUnity(); return _map == null ? 0 : _map.Count; } }

        /// <summary>按 `envSO` 取那张进攻卡。**`null` = 没载到 / 这张不是带能力的进攻卡**。</summary>
        public static OffensiveCardEntry Get(string envSO)
        {
            LoadFromUnity();
            if (_map == null || string.IsNullOrEmpty(envSO)) return null;
            OffensiveCardEntry e;
            return _map.TryGetValue(envSO, out e) ? e : null;
        }

        /// <summary>
        /// 这张进攻卡上 `trigger == 15` 的能力 —— **判据 = 原版 `HasPlayedThirdCardInTurn` 那一句
        /// `cardAbilities[].trigger == 0xf`**（`RawCardScript__HasPlayedThirdCardInTurn.c:29-31`）。
        /// 没有 ⇒ 空表（⛔ 不返回 null，调用方不用另判）。
        /// </summary>
        public static List<OffensiveAbility> ThirdCardAbilities(OffensiveCardEntry entry)
        {
            var outp = new List<OffensiveAbility>();
            if (entry == null || entry.abilities == null) return outp;
            for (int i = 0; i < entry.abilities.Length; i++)
            {
                var a = entry.abilities[i];
                if (a != null && a.trigger == AbilityTriggerPlayedThirdCardInTurn) outp.Add(a);
            }
            return outp;
        }
    }
}
