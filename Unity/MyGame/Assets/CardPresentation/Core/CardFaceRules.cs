// CardFaceRules.cs — **卡面「这一段要不要印」的判据**（原版三个方法的等价物）
//
// ============================ 判据（唯一一份，逐句亲读） ============================
// 原版这三个方法分别在**两个类**里（不是同族类），但它们是同一件事的三个面：
// 「这张卡的卡面/提示上要不要印『特殊小描述』/『目标可用数』」。
// 本工程把它们**收在一处**，理由 = 判据共用一份（`CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」）。
//
// ① `RawCardScript.ShouldShowSpecialMiniDesc()`（🥈 `cpp2il/out_cpp2il_dev_ilspy/RawCardScript.cs:3896`，
//    🥇 `decomp_full/RawCardScript__ShouldShowSpecialMiniDesc.c`）：
//      `if (cardType == 40 /*Whispers*/) { idx = spellType - 120; if (idx < 31 && bitmask_0x40100001 >> idx & 1) return true; }`
//      `return false;`
//    ⇒ `cardType == Whispers(40)` **且** `spellType ∈ {120 Quest, 140 Wing, 150 Secret}`。
//    （位掩码 `0x40100001` = 第 **0 / 20 / 30** 位 ⇒ `spellType` = 120 / 140 / 150；三个名字见 `SpellType` 枚举。）
//
// ② `RawCardScript.ShouldShowSpecialMiniDescCounter()`（`:3914` / `RawCardScript__ShouldShowSpecialMiniDescCounter.c`）：
//      `var tc = this.targetsAvailableCounter(+0x298); return tc != null && tc.targetsAffected(+0x10) != 0;`
//    ⇒ 有 `targetsAvailableCounter` **且** `TargetsAffected != any(0)`。
//
// ③ `CardScript.ShouldShowTargetsAvailable(bool inPlay = false)`（🥈 `CardScript.cs:8310`）：
//      `var raw = this.rawCardData;`
//      `if (raw.targetsAvailableCounter != null && raw.targetsAvailableCounter.targetsAffected != any)`
//      `    return inPlay == raw.showTargetsAvailableInPlay;`
//      `return false;`
//    ⇒ ②那条 **加**一道 `inPlay == showTargetsAvailableInPlay(+0x2A8)`。
//
// ============================ 现读数据（2026-10-26，⛔ 张数现读、别抄死）============================
//   · `Whispers`(cardType 40)：当前 build 的 **1,351 张 SO 里 0 张**（只有 0 Minion / 10 Hero / 20 Tactic）
//     ⇒ **`ShouldShowSpecialMiniDesc` 在本 build 恒 false**。逻辑照原版实现 ——
//       将来池子里一有 Whispers 卡（Quest / Wing / Secret 三种 spellType）它会自动生效。
//   · `showTargetsAvailableInPlay`：**1,351/1,351 全是 0**（`普查产出_1018/allcards_1350/cards_raw.json`
//     逐卡数）⇒ ③ 的 `inPlay == show` 那一半**在本 build 等价于 `inPlay == false`**。
//     ⚠️ 这里**没有**把它写死成常量：`ShowTargetsAvailableInPlay` 是一个可改的口，
//        卡数据侧一补上真值就换掉（见报告 §⑥）。
//   · 池内（`cards_engine.json` 1,128 张）带 `role == "avail"` 行且 `targetsAffected != any` 的
//     **17 张**（`availSet.targetsAffected` 分布 = `65×6 · 60×5 · 10×5 · 245×1`）⇒
//     ②③ 在本池里对**这 17 张**为真。⚠️ 池内 `avail` 行**总数也是 17** ⇒ 今天「有 avail 行」与
//     「`targetsAffected != any`」等价；⛔ 别因此把判据简化成「有没有 avail 行」（原版两道闸都在）。
//
// ============================ 「谁在用」与「我们怎么用」 ============================
//   · ① 的消费者：`RawCardScript` 内部（卡面描述布局）。我们**零消费者** —— 卡面描述排版在
//     `Core/CardView.cs`（本批不可改，见报告 §⑥）；今天恒 false 所以没有可见差异。
//   · ②③ 的消费者：`BattleCardUI.ShowAvailableTargets`（手牌里那张卡下方的「N 个可用目标」）。
//     我们**已有一条**等价链：`Battle/SkillPanel.cs` 的 `SetTargetsSuffix(..., targetsAny)` 里那道
//     `targetsAny ? "" : …` 就是原版 `targetsAffected != any` 那道闸（2026-10-26 批次 5 `G7` 接的）。
//     ⚠️ 它今天**由调用方传 bool**（`BattleDriver.ShowSkillPanel` 没传 ⇒ 恒 false）。
//     ⇒ **本文件是这三条判据的唯一正本**；`SetTargetsSuffix` 那一路将来应改成调
//     <see cref="ShouldShowTargetsAvailable"/>（改点在 `BattleDriver`，本批不可动）。
using RuleEngine;

namespace CardPresentation
{
    /// <summary>卡面/提示「这一段要不要印」的三条判据（原版 `RawCardScript` / `CardScript` 各一份）。
    /// 判据与现读数据见本文件头。</summary>
    public static class CardFaceRules
    {
        /// <summary>`SpellType.Quest` —— 原版位掩码里的第 **0** 位。</summary>
        public const int SpellTypeQuest = 120;
        /// <summary>`SpellType.Wing` —— 位掩码里的第 **20** 位。</summary>
        public const int SpellTypeWing = 140;
        /// <summary>`SpellType.Secret` —— 位掩码里的第 **30** 位。</summary>
        public const int SpellTypeSecret = 150;

        /// <summary>原版 `CardTypeOptions.Whispers`（`dump.cs` 的 `CardTypeOptions`）。</summary>
        public const int CardTypeWhispers = 40;

        /// <summary>原版 `TargetsAffected.any`（`dump.cs`；该枚举里只有它是 0）。</summary>
        public const int TargetsAffectedAny = 0;

        /// <summary>原版 `CardDef.Type` 里代表 `Whispers` 的字符串（引擎线 `CardDef.Type` 的取值口径）。
        /// ⚠️ 现读池内 **0 张**（1,351 张 SO 里 cardType 只有 0/10/20）⇒ 本常量今天没有被命中。</summary>
        public const string TypeWhispers = "whispers";

        /// <summary>🔶 自己设计（非原版）：`RawCardScript.showTargetsAvailableInPlay`（`+0x2A8`）的**当前值** ·
        /// 原版这个位是**逐卡的序列化字段**，而我们的卡表没有落它 ⇒ 只能给一个常量顶上 ·
        /// 调度台 2026-10-26 按现读数据裁定（`普查产出_1018/allcards_1350/cards_raw.json`：
        /// **1,351/1,351 全 = 0**）。⚠️ 引擎线把这一位落进卡表后**必须**改成逐卡读。</summary>
        public const bool ShowTargetsAvailableInPlay = false;

        /// <summary>原版 `RawCardScript.ShouldShowSpecialMiniDesc()`：
        /// `cardType == Whispers` **且** `spellType ∈ {Quest, Wing, Secret}`。
        /// <para>⚠️ 现读池内 **0 张 Whispers** ⇒ 恒 false（见文件头）。</para></summary>
        public static bool ShouldShowSpecialMiniDesc(CardDef card)
        {
            if (card == null) return false;
            if (!string.Equals(card.Type, TypeWhispers, System.StringComparison.Ordinal)) return false;
            int st = card.SpellType;
            return st == SpellTypeQuest || st == SpellTypeWing || st == SpellTypeSecret;
        }

        /// <summary>原版 `RawCardScript.ShouldShowSpecialMiniDescCounter()`：
        /// 「有 `targetsAvailableCounter` 且 `targetsAffected != any`」。
        /// <para>⚠️ 与原版 `ShouldShowTargetsAvailable(false)` 在**本 build 逐字等价**
        /// （`showTargetsAvailableInPlay` 全 0）—— 但两者在原版是**两个方法**、消费者也不同：
        /// 这一个喂「特殊小描述 **+ 计数**」，那一个喂手牌下方的「可用目标」。⛔ 别合成一个。</para></summary>
        public static bool ShouldShowSpecialMiniDescCounter(CardDef card)
        {
            return HasTargetsAvailableCounter(card);
        }

        /// <summary>原版 `CardScript.ShouldShowTargetsAvailable(bool inPlay = false)`。
        /// <paramref name="inPlay"/> = 「这张卡此刻在场上吗」（原版唯一调用点传默认值 `false`
        /// = 手牌里正在展示那张卡）。</summary>
        public static bool ShouldShowTargetsAvailable(CardDef card, bool inPlay = false)
        {
            if (inPlay != ShowTargetsAvailableInPlay) return false;
            return HasTargetsAvailableCounter(card);
        }

        /// <summary>②③ 共用的那一半：卡上有 `targetsAvailableCounter` **且** `targetsAffected != any`。
        /// <para>读数口径：`CardDef.Conditions` 里 `Role == "avail"` 的那一行
        /// （= 原版 `RawCardScript.targetsAvailableCounter(+0x298)` 整条 criteria，
        /// 由 `CardDatabase.MapUnitConditions` 的 `RoleSet(... "avail" ...)` 落成 `ConditionSpec.AvailSet`）。</para>
        /// <para>⚠️ `AvailSet == null` = **这一格没落 / 未知** ⇒ 按原版「字段为 null」处理 = **false**
        /// （⛔ 不猜成 any）。`TargetsAffected == Unknown(-1)` 同样是「缺列」⇒ false。</para></summary>
        public static bool HasTargetsAvailableCounter(CardDef card)
        {
            if (card == null) return false;
            var conds = card.Conditions;
            if (conds == null) return false;
            for (int i = 0; i < conds.Count; i++)
            {
                var spec = conds[i];
                if (spec == null) continue;
                if (!string.Equals(spec.Role, "avail", System.StringComparison.Ordinal)) continue;
                var set = spec.AvailSet;
                if (set == null) continue;
                if (set.TargetsAffected < 0) continue;                 // Unknown = 缺列
                if (set.TargetsAffected != TargetsAffectedAny) return true;
            }
            return false;
        }
    }
}
