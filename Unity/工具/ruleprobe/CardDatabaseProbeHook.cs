// CardDatabaseProbeHook.cs — 探针侧对 `CardDatabase` **两个私有映射函数**的公开转发
//   写手 `W-25` · 2026-11-01 · 活状态表 `B-257`（同族：`A1911` / `A1959` / `B8_AV`）
//
// 🔴 为什么需要它：
//   `Program.LoadPool` 是 `System.Text.Json` **手读**卡表，不走 `Data/CardDatabase.cs`
//   ⇒ 引擎 `CardDatabase.cs:1762` 那两句（`AttachTriggerAbilityIndices(MapTriggerAbilityIndices(...))`
//   ＋ `AttachTriggerOpsByNumber(MapTriggerOpsByNumber(def))`）在探针里**从来没被执行过**。
//   后果：探针池里每张卡 `HasTriggerOpsByNumber == false` ⇒ `CardDef.TriggerOpsByNumber(int)`
//   会**静默落进「卡面关键词 == 触发词名」退回档**（`Core/CardDefPartial_B96CA.cs:102` 的第二级）
//   ⇒ 任何拿 `Program.PoolList` 量 `TriggerOpsByNumber` 的读数（`play` 那条链上的 `EffectResolver`
//   全都在用它）**看着像产品口径、其实不是同一条路**。
//
// 🔑 为什么是「partial 转发」而不是反射 / 抄一份：
//   ① **C# 语言规则**：`partial class` 的所有 part 是**同一个类**，`private` 成员**跨 part 可见**；
//      而本文件与 `Assets/RuleEngine/Data/CardDatabase*.cs` 被编进**同一个程序集**
//      （`ruleprobe.csproj` 显式列了两者）⇒ 这里能直接调那两个 private mapper。
//   ② **不许抄一份**：这两个映射里有「行级坏值丢弃 / `(ab,lg)` 撞键去重 / 按 `ab` 归位 /
//      同一 ability 的多条 logic 行只算一次 / 三态『不挂·挂空表·挂非空』」 —— 抄进探针 =
//      两处写同一条规则（迟早不一致，**正是本账要治的病**）。
//   ③ **不用反射**：反射在方法改名时**静默落空**（= 又退回退回档，缺陷原样复发）；
//      partial 转发是**编译期**绑定 —— 引擎一改名，`ruleprobe.sh` 当场编不过。
//      （`Assets/**` 不在本写手白名单 ⇒ ⛔ 不许把那两个方法改成 `public`。）
//
// ⚠️ 本文件**不在 `Assets/` 下** ⇒ 不会进 Unity 工程，只在 `ruleprobe.csproj` 的显式编译清单里。
// ⛔ 别把这两个转发扩散成「探针能调引擎一切私有方法」的口子 —— 只服务 `LoadPool` 这一处口径对齐。
using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class CardDatabase
    {
        /// <summary>转发到私有 `MapTriggerAbilityIndices(CardDto, int)`（`Data/CardDatabase.cs:4813`）——
        /// `triggerAbilityIndices` 列 → `CardDef.TriggerAbilityIndexSpec[]`。
        /// 三态同引擎：`null` = 缺列（未知）· `[]` = 明确没有 · 非空 = 有行。</summary>
        public static CardDef.TriggerAbilityIndexSpec[] ProbeMapTriggerAbilityIndices(CardDto card, int version)
            => MapTriggerAbilityIndices(card, version);

        /// <summary>转发到私有 `MapTriggerOpsByNumber(CardDef)`（`Data/CardDatabasePartial_B98H7.cs:134`）——
        /// `{触发数值 → 正文 op}` 表。`null` = **不挂**（列未知 / 一条都归不了位）⇒ 消费口保留退回档；
        /// 空表 = 挂上（这张卡的触发数值都没有可跑的正文）。
        /// ⚠️ **必须排在 `AttachTriggerAbilityIndices` 之后**（它读那一列）。</summary>
        public static Dictionary<int, List<EffectOp>> ProbeMapTriggerOpsByNumber(CardDef def)
            => MapTriggerOpsByNumber(def);

        // ==================================================================================================
        //  🆕 2026-11-03（执行代理 `B123_E3` · 账 `B121-D4-01`）：**「构造后挂载」那一段余下的 mapper 转发**
        //
        //  🔴 为什么还要这些：`Program.LoadPool` 是 `System.Text.Json` 手读卡表，引擎
        //     `Data/CardDatabase.cs:1762` 那一整行「构造后挂载」里，探针此前只补了三句
        //     （`AttachAbilityDamage` / `AttachTriggerAbilityAndOpsByNumber` / `AttachCardTraits` / `AttachChosenAbilities`）。
        //     余下的 `AttachWhileInPlayAnim` / `AttachPermanentBuffs` / `AttachAbilitySummon` / `RecordAbilityBuffs` /
        //     `AttachAbilityMutations` / `AttachStartingCardInHand` / `AttachAbilityEffectTexts` / `AttachCardTargetCriteria` /
        //     `AttachTransformData` / `AttachNotDeckable` / `AttachAbilityHeal` / `AttachAbilityManaBuff`
        //     **一条都没挂** ⇒ 探针池里 `HasAbilityHeal` / `HasAbilityManaBuff` / `AbilitySummons` /
        //     `AbilityBuffsOf` 全恒空 —— 那几列上任何「`play check` 绿」都**不构成证据**。
        //
        //  🔑 口径与上面两条转发完全相同：**转发到同一个私有 mapper，⛔ 不另写一份解析**
        //     （抄一份 = 两处写同一条规则 = 迟早不一致，正是 `B121-D4-01` 要治的病）。
        //     ⚠️ 这些都是**编译期绑定** —— 引擎一改名，`ruleprobe.sh` 当场编不过（反射会静默落空）。
        //  ⛔ 仍然只服务 `LoadPool` 这一处口径对齐，别扩散成「探针能调引擎一切私有方法」的口子。
        // ==================================================================================================

        /// <summary>转发到私有 `MapAbilityHeal(CardDto)`（`Data/CardDatabase.cs:2204`）—— `abilityHeal` 列。
        /// 三态同引擎：`null` = 缺列（未知）· `[]` = 明确没有 · 非空 = 有行。</summary>
        public static CardDef.AbilityHealSpec[] ProbeMapAbilityHeal(CardDto card)
            => MapAbilityHeal(card);

        /// <summary>转发到私有 `MapAbilityMana(CardDto)`（`Data/CardDatabase.cs:2236`）—— `abilityMana` 列。</summary>
        public static CardDef.AbilityManaBuffSpec[] ProbeMapAbilityMana(CardDto card)
            => MapAbilityMana(card);

        /// <summary>转发到私有 `MapAbilitySummon(CardDto)`（`Data/CardDatabase.cs:3999`）—— `abilitySummon` 列。</summary>
        public static CardDef.AbilitySummonSpec[] ProbeMapAbilitySummon(CardDto card)
            => MapAbilitySummon(card);

        /// <summary>转发到私有 `RecordAbilityBuffs(CardDef, CardDto)`（`Data/CardDatabase.cs:4846`）——
        /// 🔴 它写的是**旁表** `_abilityBuffsById`（查询口 `CardDatabase.AbilityBuffsOf`），**不是** `CardDef` 上的字段。
        /// `null`（旧表）⇒ 不写进表 ⇒ `AbilityBuffsOf` 返 `null`（= 未知，⛔ 不是「没有行」）。</summary>
        public static void ProbeRecordAbilityBuffs(CardDef def, CardDto card)
            => RecordAbilityBuffs(def, card);

        /// <summary>转发到私有 `MapPermanentBuffs(CardDto)`（`Data/CardDatabase.cs:3591`）—— `permanentBuffs` 列。</summary>
        public static CardDef.PermanentBuffSpec[] ProbeMapPermanentBuffs(CardDto card)
            => MapPermanentBuffs(card);

        /// <summary>转发到私有 `MapAbilityMutations(CardDto)`（`Data/CardDatabase.cs:4910`）—— `abilityMutations` 列。</summary>
        public static CardDef.AbilityMutationSpec[] ProbeMapAbilityMutations(CardDto card)
            => MapAbilityMutations(card);

        /// <summary>转发到私有 `MapStartingCardInHand(CardDto)`（`Data/CardDatabasePartial_B97W5.cs:75`）——
        /// `startingCardInHand` 列。`null` = 整列未知 · `""` = 明确没有 · 其它 = 卡名。</summary>
        public static string ProbeMapStartingCardInHand(CardDto card)
            => MapStartingCardInHand(card);

        /// <summary>转发到私有 `MapAbilityEffectTexts(CardDto)`（`Data/CardDatabase.cs:5104`）—— `abilityEffectTexts` 列。</summary>
        public static CardDef.AbilityEffectTextSpec[] ProbeMapAbilityEffectTexts(CardDto card)
            => MapAbilityEffectTexts(card);

        /// <summary>转发到私有 `MapCardTargetCriteriaSet(CardDto)`（`Data/CardDatabase.cs:5209`）——
        /// 卡级 `cardTargetCriteria` 的 36 格（v46）。</summary>
        public static CardDef.CountSetSpec ProbeMapCardTargetCriteriaSet(CardDto card)
            => MapCardTargetCriteriaSet(card);

        /// <summary>转发到私有 `MapDeckCriteria(CardDto)`（`Data/CardDatabase.cs:5277`）—— 督军变身用的 `deckCriteria`。</summary>
        public static DeckCriteriaSpec ProbeMapDeckCriteria(CardDto card)
            => MapDeckCriteria(card);
    }
}
