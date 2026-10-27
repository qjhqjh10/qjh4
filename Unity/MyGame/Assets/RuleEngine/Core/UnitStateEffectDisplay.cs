// UnitStateEffectDisplay.cs — `TempBuff` 的「效果展示」那几格（`UnitState.TempBuff` 的 partial 半边）
//
// 🔴 **为什么单独开一个文件，而不是写进 `UnitState.cs` 的 `TempBuff` 里**：
//   账里有一大批按【行号】的落点指向 `UnitState.cs`（现读最大 = `:3135`），而 `TempBuff` 在
//   `:1637` —— 在它里面插成员会让后面**所有**落点整体漂移。
//   ⇒ 走 partial：`UnitState` 与 `TempBuff` 的声明各加一个 `partial` 关键字（**不改行数**），
//     新成员落本文件（零行号漂移）。`Core/*.cs` 是**通配**编进 `RuleCore` / `ruleprobe` 的。
//
// ==================================================================================================
//  账 `特效动画:195 CardEffectItem.LoadEffect` —— 数据侧（原版 `CardEffect` 那几格）
//  账 `特效动画:198 CardEffectsService.EnableEffects` —— 输入选择的过滤（`buffType` + `HasQuests`）
// ==================================================================================================
//  判据（第一权威 = `d:/2/tools/il2cpp_out/dump.cs` 的 `CardEffect` 字段表 + `.c`）：
//    · `CardEffect.effectName   (+0x20)` —— 「Leave empty to use name of card」
//      （`CardEffectItem__LoadEffect.c:110-140`：`EffectName` 空 ⇒ 退 `enchantingCard` 的本地化卡名）；
//    · `CardEffect.buffType    (+0x38)` —— 分派依据（`LoadEffect.c:53` 起按它分派：
//      `8 markOfChaos` / `0x11 lastMarkOfChaos` ⇒ 双行合成；`10 changeAbilityCost` / `0x14 doubleAttack` /
//      `0x1e addRace` / `0x28 replaceAbility` ⇒ 专名）；
//    · `CardEffect.traitValue  (+0x40)` —— 追数值（`LoadEffect.c:96-99`：`> 1` 时在专名后追加「 N」）；
//    · `CardEffect.secretBuff  (+0x90)` / `CardEffect.questBuff (+0x91)` —— **可见性掩码**
//      （`LoadEffect.c:36-52`：`(secretBuff & isPlayer) == 0 && (questBuff == 0 || isPlayer != 0)
//       && (isPlayer & questBuff) == 0` ⇒ 走「隐藏/密文」那一支，否则印真文案）。
//    · `EnableEffects` 的**输入过滤**（`CardEffectsService__EnableEffects.c`，见施工单 198 ③）：
//      进槽前先 `RemoveAt` 擦掉 `buffType ∈ {6 changeRace, 0x23 daemonHost(35), 0x1e addRace(30)}`
//      或（`0x10 choiceEnchantment(16)` 且 `!HasQuests`）的条目。
//      `BuffType` 名值 → `dump.cs:45430-45462`。
//
//  ⚠️ **我们的数据从哪来（`BuffType` / `TraitValue` 已经能填上）**：
//    · 非关键词那条限时增益 → `TempBuff.Mod`（`StatMod.BuffType +0x38`，`UnitState.cs:1899`）；
//    · 关键词那条限时增益 → `TempBuff.TraitRec`（`TraitEffectRecord.BuffType/TraitValue`，`:2920`）。
//    ⇒ 本文件把这两条**派生**成 `TempBuff.BuffType` / `TempBuff.TraitValue`，E6 侧直接读。
//  🔴 **`SecretBuff` / `QuestBuff` 目前没有生产者，如实标着**：我们的 `TempBuff` 是**文本派生**的
//    （`EffectResolver.ApplyOneGain`），不携带原版 `CardEffect.secretBuff/questBuff` 那两位；
//    要真填，得由结算层在**施加那一刻**从来源（密令/任务附魔）带下来 —— 那在 `EffectResolver.cs`
//    （E2 的文件），⛔ 本批不动。本处只把字段与语义钉住，等那条路接上。
//
namespace RuleEngine
{
    public partial class UnitState
    {
        /// <summary>`CardEffect.buffType` 的「未知 / 不是从 CardEffect 来的」哨兵（与
        /// `StatMod.BuffType` 的 `-1` 同一口径）。</summary>
        public const int EffectBuffTypeUnknown = -1;

        // `EnableEffects` 输入过滤用到的三个 buffType（名值 → `dump.cs:45430-45462`）
        /// <summary>原版 `BuffType.changeRace = 6`（`dump.cs:45443`）。</summary>
        public const int BuffTypeChangeRace = 6;
        /// <summary>原版 `BuffType.addRace = 30`（`dump.cs:45456`）。</summary>
        public const int BuffTypeAddRace = 30;
        /// <summary>原版 `BuffType.choiceEnchantment = 16`（`dump.cs:45450`）。</summary>
        public const int BuffTypeChoiceEnchantment = 16;
        /// <summary>原版 `BuffType.daemonHost = 35`（`dump.cs:45457`）。</summary>
        public const int BuffTypeDaemonHost = 35;

        /// <summary>
        /// **这条效果会不会被列进「受到以下影响」**（原版 `CardEffectsService.EnableEffects` 的输入过滤）。
        /// `false` ⇒ 原版在进槽前就 `RemoveAt` 掉了（我们照此在铸行前跳过）。
        /// ⚠️ 判据只此一处（账 `特效动画:198` ③）；`hasQuests` = 原版 `HasQuests(entity) > 0`
        /// （我们 = `Enchantments.CountQuests(carrier) > 0`）。
        /// </summary>
        public static bool IsEffectListEntry(int buffType, bool hasQuests)
        {
            if (buffType == BuffTypeChangeRace) return false;
            if (buffType == BuffTypeAddRace) return false;
            if (buffType == BuffTypeDaemonHost) return false;
            if (buffType == BuffTypeChoiceEnchantment && !hasQuests) return false;
            return true;
        }

        public partial class TempBuff
        {
            /// <summary>
            /// 原版 `CardEffect.effectName(+0x20)` —— **空 ⇒ 用施加它的那张卡的名字**
            /// （`CardEffectItem__LoadEffect.c:110-140`）。⛔ 别和 <see cref="Src"/> 混：`Src` 是结算层的
            /// `by`（战术卡那条路恒为「战术卡」），而本格是**这条效果自己的名字**。
            /// </summary>
            public string EffectName;

            /// <summary>原版 `CardEffect.secretBuff(+0x90)` —— 可见性掩码之一（见本文件头）。
            /// 🔴 今天**没有生产者**（见文件头末段）；默认 `false` = 不隐藏。</summary>
            public bool SecretBuff;

            /// <summary>原版 `CardEffect.questBuff(+0x91)` —— 可见性掩码之二。
            /// 🔴 今天**没有生产者**（见文件头末段）；默认 `false`。</summary>
            public bool QuestBuff;

            /// <summary>
            /// 原版 `CardEffect.buffType(+0x38)` —— **派生**：非关键词取 `Mod.BuffType`、关键词取
            /// `TraitRec.BuffType`；两者都没有 ⇒ <see cref="EffectBuffTypeUnknown"/>。
            /// （这两个子对象在施加时已由 `AddStatMod` / `ApplyTraitEffect` 填好 —— 数据源已就位。）
            /// </summary>
            public int BuffType
            {
                get
                {
                    if (IsKeyword) return TraitRec != null ? TraitRec.BuffType : EffectBuffTypeUnknown;
                    return Mod != null ? Mod.BuffType : EffectBuffTypeUnknown;
                }
            }

            /// <summary>
            /// 原版 `CardEffect.traitValue(+0x40)` —— **派生**：关键词取 `TraitRec.TraitValue`（没有记录时退
            /// <see cref="Value"/>）；非关键词（不是 trait 那条）⇒ 0。
            /// </summary>
            public int TraitValue
            {
                get { return IsKeyword ? (TraitRec != null ? TraitRec.TraitValue : Value) : 0; }
            }

            /// <summary>
            /// 展示时「谁给的」那一段 —— 原版 `LoadEffect` 的取法：`EffectName` 非空用它，否则用
            /// 施加卡的名字（我们 = <see cref="SourceCard"/>）。判据 `CardEffectItem__LoadEffect.c:110-140`。
            /// </summary>
            public string DisplaySourceName
            {
                get { return string.IsNullOrEmpty(EffectName) ? SourceCard : EffectName; }
            }
        }
    }
}
