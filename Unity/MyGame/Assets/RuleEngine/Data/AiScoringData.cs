// AiScoringData.cs — 原版 AI 逐卡评分表（T06 / A1797）的**数据装载层**
//
// 数据源：`Assets/RuleEngine/Resources/ai_scoring.json`（生成器 = `工具/gen_ai_scoring.py`）
//   ← `cards_truth.json`（SO 真值，1,351 张）切出的 AI 专用小片：
//     · `nonTargetScoringCriteria`   —— **所有卡都跑**（387 张 / 390 条）
//     · `cardTargetScoringCriteria`  —— **只有 `targeted=1` 且动作带目标时**才跑（367 张 / 373 条）
//     · `targeted` / `hasActiveAbility` / `cardType` / `spellType` / `keyword`
//       （后三个是 `FilterMethods.CheckIfMeetsCriteria` 要用的字段）
//
// ⛔ **`attackScoringCriteria` 不在里面，也别加回来** —— 判据见
//   `资料/普查产出_1018/AUD_T06_AI评分口径.md` §二：
//   1,351/1,351 是同一份序列化默认模板，且 `specialAttackScoringCriteria` 全 0
//   ⇒ 原版 `AI.ScoreFromAttack` 里那两处读取**运行时一次都进不去**（双重失效）。
//
// 🔴 **为什么放 Data/ 而不是 Core/**：`CardDef` 要加字段得动 `Core/`（本批由 E3 独占）；
//   这张表只服务 AI 打分，按 id 旁挂查询即可（同 `LegacyCardIds` / `OffensiveCardMap` 的形状）。
//   `SimpleAI` / `AiScoring` / `AiTargets` 都从 `AiScoringData.Get(card.Id)` 取。
//
// ⚠️ `Data/` 允许碰 `UnityEngine`（`Core/` 不许）—— 本文件用 `Resources` + `JsonUtility`，
//   与 `CardDatabase.cs` 同一条路。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuleEngine
{
    // ======================================================================
    //  DTO —— 字段名必须与 `ai_scoring.json` 的键**逐字相同**（JsonUtility 按名字映射）
    //  缺键 = 默认值（0 / false / null）；`cards_truth.json` 的生成口径就是「默认值省略」。
    // ======================================================================

    /// <summary>原版 `TargetCriteria`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/TargetCriteria.cs`）。
    /// 字段顺序/默认值照原版（bool 缺省 false；`targetsAffected=0` = `any`，**忽略其余全部筛选**）。</summary>
    [Serializable]
    public class AiTargetCriteriaDto
    {
        public int targetsAffected;
        public bool friendlyUnits;
        public bool enemyUnits;
        public bool includeHero;
        public bool includeMinions;
        public bool includeSpells;
        public bool includeWhispers;
        public bool includeWeapons;
        public bool includeRemnants;
        public bool includeHeroParts;
        public bool includeSelf;
        public bool excludeNotSelf;
        public bool excludeSimilarCards;
        public bool excludeTargetted;
        public bool excludeFriendlyHero;
        public bool excludeEnemyHero;
        public int chosenArmy;
        public int armyFilter;
        public int keywordFilter;
        public bool excludeRace;
        public int traitsFilter;
        public int conditionsFilter;
        public bool excludeTrait;
        public bool excludeCondition;
        public int maxTargets;
        public int targetSelectMethod;
        public int attackFilter;
        public int attackFilterOption;
        public int currentHealthFilter;
        public int currentHealthFilterOption;
        public int manaCostFilter;
        public int manaCostFilterOption;
        public int rarityFilter;
        public int rarityFilterOption;
        public int spellType;
        public string[] filterSpecificCards;
        public string[] excludeEnchantments;
        public bool selectFromChooseResolution;
    }

    /// <summary>原版 `DamageCriteria`（只用到 min/max/type/valueOptions；`valueOptions` 非 0 的
    /// 全池只有 3 条，见 `AiScoring` 的注释）。</summary>
    [Serializable]
    public class AiDamageCriteriaDto
    {
        public int minDamage;
        public int maxDamage;
        public int damageType;
        public int valueOptions;
    }

    /// <summary>原版 `HealCriteria`（池内 577 条**全是** `valueOptions=0`）。</summary>
    [Serializable]
    public class AiHealCriteriaDto
    {
        public int minValue;
        public int maxValue;
        public int valueOptions;
    }

    /// <summary>原版 `ScoringCriteria`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/ScoringCriteria.cs`）。
    /// `scoringOption` 取值见那个类的 `ScoringOption` 枚举（0/1/2/3/4/5/6/…/35）。</summary>
    [Serializable]
    public class AiScoringEntryDto
    {
        public float baseValueFriend;
        public float baseValueEnemy;
        public float multiplyByFriend;
        public float multiplyByEnemy;
        public int scoringOption;
        public AiTargetCriteriaDto targetCriteria;
        public AiTargetCriteriaDto multiplierCriteria;
        public AiDamageCriteriaDto damage;
        public AiHealCriteriaDto heal;
        /// <summary>`CardEffect`（option 5/11 的 buff / 12 的 handEffect.cardEffect）。只保留本批要读的判据字段。</summary>
        public AiBuffDto buff;
        public AiHandEffectDto handEffect;
        public bool codexEffect;
    }

    /// <summary>原版 `HandEffect`（`dump.cs:119795-119802`）：
    /// `cardEffect(+0x10)` / `playersAffected(+0x18)` / `targetCriteria(+0x20)` /
    /// `limitedUses(+0x28)` / `numberOfUses(+0x2C)`。
    /// 🔴 **`null` 与「全默认对象」在 `ScoreFromHandBuff` 里结果不同**（第一句判 null ⇒ 直接 0）——
    /// 生成器从 `cards_raw.json` 把「存在性」补回来（truth 会省略全默认对象）。</summary>
    [Serializable]
    public class AiHandEffectDto
    {
        public AiBuffDto cardEffect;
        public int playersAffected;
        public AiTargetCriteriaDto targetCriteria;
        public bool limitedUses;
        public int numberOfUses;
    }

    /// <summary>原版 `ActiveAbility` 的两列评分（`AI.GetActionScore` case 5 读 `+0x50/+0x58`；
    /// 字段偏移 = `dump.cs:23766-23767`）。⚠️ 这两列**只在 `cards_raw.json` 里**，
    /// `cards_truth.json` 没有落 —— 生成器单独从 raw 取。</summary>
    [Serializable]
    public class AiActiveAbilityDto
    {
        public AiScoringEntryDto[] cardTargetScoringCriteria;
        public AiScoringEntryDto[] nonTargetScoringCriteria;
    }

    /// <summary>原版 `CardEffect` 里本批要读的那几个字段（Trait/生效时长等其余字段不影响打分）。</summary>
    [Serializable]
    public class AiBuffDto
    {
        public int buffType;
        public int trait;
        public int traitValue;
        public int newRace;
        public int meleeAttackChange;
        public int meleeAttackValueOptions;
        public int rangeAttackChange;
        public int rangeAttackValueOptions;
        public int armourChange;
        public int healthChange;
        public int costChange;
        public int randomValueChange;
        /// <summary>`CardEffect.untilEndOfTurn // +0x32` —— `ScoreFromHandBuff` 的附加项用它当闸
        /// （为真 ⇒ **不加**后面的 counts 项）。</summary>
        public bool untilEndOfTurn;
    }

    /// <summary>一张卡的 AI 评分数据。`cardType` / `spellType` / `keyword` 是
    /// `FilterMethods.CheckIfMeetsCriteria` 的筛选输入（与 `CardDef` 的字符串字段**不是**同一套）。</summary>
    [Serializable]
    public class AiCardScoringDto
    {
        public string id;
        public string name;
        public int cardType;
        public int spellType;
        public int keyword;
        public int targeted;
        public int hasActiveAbility;
        public int manaCost;
        public AiScoringEntryDto[] nonTargetScoringCriteria;
        public AiScoringEntryDto[] cardTargetScoringCriteria;
        /// <summary>主动技自己的两列评分（`AI.GetActionScore` case 5）。没有 = 原版 `GetActiveAbility()==null`。</summary>
        public AiActiveAbilityDto activeAbility;
    }

    [Serializable]
    public class AiScoringFileDto
    {
        public int version;
        public string source;
        public string note;
        public int count;
        public AiCardScoringDto[] cards;
    }

    // ======================================================================
    //  装载 + 查询
    // ======================================================================

    /// <summary>
    /// `ai_scoring.json` 的运行期查询口。**整份只装一次**（首次用到时懒加载）。
    /// ⚠️ 找不到资源时 `LogError`（**不静默**）并返回空表 —— 调用方会退化成「没有任何
    /// scoring criteria」= 原版对**缺列卡**的口径（单位仍有 `ValueInPlay`，战术卡 = 0）。
    /// </summary>
    public static class AiScoringData
    {
        public const string ResourcePath = "ai_scoring";

        static Dictionary<string, AiCardScoringDto> _byId;
        static bool _loaded;

        /// <summary>这张卡的评分数据；表里没有就返回 null（= 原版「缺列」）。</summary>
        public static AiCardScoringDto Get(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;
            EnsureLoaded();
            AiCardScoringDto c;
            return _byId != null && _byId.TryGetValue(cardId, out c) ? c : null;
        }

        /// <summary>`CardDef` 版本（按 `Id` 查）。</summary>
        public static AiCardScoringDto Get(CardDef card)
        {
            return card == null ? null : Get(card.Id);
        }

        public static int Count
        {
            get { EnsureLoaded(); return _byId == null ? 0 : _byId.Count; }
        }

        /// <summary>显式重装（自检 / 换数据集时用；产品路径不需要）。</summary>
        public static void Reload()
        {
            _loaded = false;
            _byId = null;
            EnsureLoaded();
        }

        static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogError("[AiScoring] 找不到 Resources/" + ResourcePath + ".json —— "
                             + "AI 将退化为「没有任何逐卡评分表」（原版对缺列卡的口径）。"
                             + " 生成器：工具/gen_ai_scoring.py");
                _byId = new Dictionary<string, AiCardScoringDto>();
                return;
            }

            var file = JsonUtility.FromJson<AiScoringFileDto>(asset.text);
            if (file == null || file.cards == null)
            {
                Debug.LogError("[AiScoring] " + ResourcePath + ".json 解析失败（cards 为空）—— "
                             + "同样的退化：AI 只剩 ValueInPlay / 0 分。");
                _byId = new Dictionary<string, AiCardScoringDto>();
                return;
            }

            _byId = new Dictionary<string, AiCardScoringDto>(file.cards.Length);
            for (int i = 0; i < file.cards.Length; i++)
            {
                var c = file.cards[i];
                if (c == null || string.IsNullOrEmpty(c.id)) continue;
                _byId[c.id] = c;
            }
        }

        /// <summary>空的评分数组当「没有」。两列缺键 / 空数组都走这里。</summary>
        public static bool HasAny(AiScoringEntryDto[] list) { return list != null && list.Length > 0; }
    }
}
