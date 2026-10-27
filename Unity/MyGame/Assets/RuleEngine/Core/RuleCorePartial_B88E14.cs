// RuleCorePartial_B88E14.cs — 第八十八会话 · 执行代理 `E14` 的 `RuleCore` partial 扩展
//
// 🔴 **为什么单开一个文件**：`RuleEngine/Core/RuleCore.cs` 被账按 `文件:行号` 引用 **365+ 处**
//   （`资料/完全复刻/账/*.tsv`）⇒ 往它中间插成员会让下游落点整体下移、`check_ledger_points` 大批报漂移。
//   本批的做法：**源文件只做「零净行数」的就地编辑**（把新实参接在已有行的行尾），
//   新成员一律落在本文件 —— 它没有任何账的落点。
//
// 本批（B88-E14）落在本文件的东西：
//   · `AlternativeAbilityTriggerPriority(ctx, card, keyword)`
//     ← 施工单 `资料/完全复刻/判据/B88_E5_AbilityPriority_发侧施工单.md` §二 第 ③ 条
//       （`UseAlternativeCore` 那一条 `EvtKind.Ability` 的 `priority`）。
//
// ⚠️ 判据出处写在方法头上（铁律 2/3：没有出处就写「查不到」）。

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==================================================================
        //  施工单 §二 ③ —— 替代行动（duty / pray / ferocity / agenda）的 priority
        // ==================================================================
        //
        // **为什么要查表而不是写死 1**：施工单 §二 ③ 要求「按这条替代能力的原版 trigger 名查
        //   `CardAbility.priority`」，而不是照抄主动技那条固定 1。
        //
        // **原版 trigger 名**：四族在 `AbilityTrigger` 枚举里**各有自己的名字**
        //   （`Pray` 655 / `Duty` 662 / `Agenda` 705 / `Ferocity` 725 —— 转写表
        //   `CardDef.AbilityTriggerNumberOfName`，判据 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs` 97 条）。
        //   ⚠️ **别用 `Triggered*` 那三个**（`TriggeredPray` 660 / `TriggeredDuty` 661 /
        //   `TriggeredFerocity` 726）—— 那是「**被别人触发**」的家系，与本条（玩家主动用）不是一回事。
        //
        // **数据现读（2026-10-27，池内 1,128 张）**：`cards_engine.json` 的 `abilityTriggerPriority`
        //   列只有 **7 张卡 / 6 个键**（`Artifice` · `UseSpiritStone` · `UnitReceivesMarkOfChaos` ·
        //   `OnCollectQuestPoints` · `FollowUpResolution` · `Codex`）⇒ 四族今天**全部落回缺省 1**
        //   （= `CardAbility__.ctor.c:5` 的缺省，施工单 §一 第 4 条）⇒ **本批行为与「写死 1」相同**，
        //   但走的是查表这条路，将来数据补上就自动生效（⛔ 不留一个写死的常量等以后返工）。
        //
        // ⚠️ **认不出关键词时出声**（施工单要求）：本条**理论上不可达** —— 调用点上游
        //   `CanUseAlternative` 已经用 `IsAlternativeAction(keyword)` 拦过一遍。真走到了，
        //   说明两处口径打架，这时**出声 + 回缺省 1**，⛔ 不猜。
        //   ⚠️ `ctx.Log` 会改 `ctx.Events.Count`（⇒ 动 `NetProtocol.Fingerprint`），所以只有
        //   「口径打架」这一种**本来就该红**的情况才写它。

        /// <summary>替代行动关键词（`KeywordTable.Duty` 等小写引擎词）→ 原版 `AbilityTrigger` **枚举名**。
        /// 认不出 ⇒ <c>null</c>（⛔ 不猜）。</summary>
        static string TriggerNameOfAlternativeKeyword(string keyword)
        {
            switch (keyword)
            {
                case "duty": return "Duty";
                case "pray": return "Pray";
                case "ferocity": return "Ferocity";
                case "agenda": return "Agenda";
                default: return null;
            }
        }

        /// <summary>
        /// 替代行动那一条 `EvtKind.Ability` 应带的原版 `CardAbility.priority`
        /// （`ActionPriority`：0 Player / 1 Instant / 2 Interrupt / 3 DelayedInstant）。
        /// 认不出 / 卡为空 ⇒ <see cref="CardDef.DefaultTriggerPriority"/>（1），并在认不出时出声一次。
        /// 🔴 **⛔ 别拿它当「这次替代行动会不会结算」的判据** —— 那个看 `CanUseAlternative`。
        /// </summary>
        public static int AlternativeAbilityTriggerPriority(BattleContext ctx, CardDef card, string keyword)
        {
            string triggerName = TriggerNameOfAlternativeKeyword(keyword);
            if (triggerName == null)
            {
                ctx.Log($"⚠️ 替代行动关键词「{keyword}」认不出对应的原版 `AbilityTrigger` 名"
                      + $" ⇒ priority 回缺省 {CardDef.DefaultTriggerPriority}（如实的「认不出」，⛔ 不是静默猜）");
                return CardDef.DefaultTriggerPriority;
            }
            // 卡为空只可能是调用点写错（那个单位不在棋盘上）—— 同样回缺省而不是抛。
            if (card == null) return CardDef.DefaultTriggerPriority;
            // 缺键 = 缺省 1，契约写在 `CardDef.AbilityTriggerPriority` 的 doc 里（⛔ 这里不重复一份缺省逻辑）。
            return card.AbilityTriggerPriority(triggerName);
        }
    }
}
