// ==============================================================================================
//  🆕 2026-11-01（写手 `W-30` · 账 `B-245`）：`B-177` 刻意没接的**三处**，回源判定之后的落点
//  —— 判据一律现读 `d:/2/tools/decomp_full/`，逐条写在下面两个 helper 上。
//  报告（逐处：原版判据 → 我们现状 → 做/不做）→ `资料/完全复刻/报告/B114_W30_criteria三处.md`。
//
//  ① **`Synapse`（`RepeatTacticOnAdjacent`）那条 `IsLegalPick` 现在传 `casterCard`**
//     （改动落点 = `EffectResolver.cs:2965`）。
//     判据 = `CardScript__TargetedSpellPlayed.c:51-69`：对每个相邻都重跑 vtable `+0x318` =
//     `RawCardScript.IsValidTarget(rawOf这张战术, 这张战术, 相邻)` —— 与「玩家点目标」那一支
//     **同一个谓词**（`BattleManager__IsValidSpellTarget.c:59-62` 是同一个 vtable 槽，
//     形参序 = `(raw, thisCard, target)`，来源 = `Deck` 状态机 `_ResolvePlayCardFromHand_d__447:1263`
//     把这张战术**自己**当 `param_2` 传进 `TargetedSpellPlayed`）。
//     ⇒ 顺带把 `AddSide` 的 `tacticCaster`（**伪装**闸，`IsValidTarget.c:30-36` 只在
//     `卡种 == 0x14(tactic) ∧ 异侧` 时挡）也一起对上。
//     ⚠️ `TargetedSpellPlayed` 里**没有** `get_targeted` 那道闸，但 `targeted == 0` 的战术
//     结构上进不了这一支（它必须是被点过目标的法术）⇒ `ApplyCardTargetCriteriaFilter` 的
//     `targeted == 0 ⇒ 不筛` 与它**等价**（不是「多了一道闸」）。
//
//  ② **`excludeNotSelf` 在点选路 = 整池淘汰**（`CardCriteriaExcludeNotSelfEmptiesPool`）。
//     判据 = `FilterMethods__CheckIfMeetsCriteria.c:239-243` 逐句：
//         `候选 != thisCard ∧ excludeNotSelf(+0x1e) != 0 ⇒ return 0`。
//     点选路的 `thisCard` 是**打出的这张牌自己**（`IsValidSpellTarget.c:61-62` 传 `param_2` =
//     这张法术卡；`GetValidTargets.c:31-40` 传的是那张单位卡）—— 它**永远不是**棋盘上的候选对象
//     ⇒ `候选 != thisCard` 恒真 ⇒ 原版把**所有候选**淘汰。
//     ⚠️ `includeSelf`（`:236-238`）是**反向**的：`候选 == thisCard ∧ includeSelf == 0 ⇒ return 0`，
//     同一个前提让 `候选 == thisCard` 恒假 ⇒ 那一格**永不命中** ⇒ 我们原来的「跳过」是**等价**的，
//     ⛔ 别照着补（补了会把一批卡凭空打死）。
//     ⚠️ 只在 `targetsAffected == 10` 时成立：别的档在原版里走**更早的 switch 短路**
//     （`CheckIfMeetsCriteria.c:56-160`）⇒ 根本走不到这两格。
//     📌 现读全池 `excludeNotSelf` **1,129/1,129 张全是 0** ⇒ 这条是**忠实但零影响**的补齐。
//
//  ③ **`includeSelf` 对「待落地那张自己」可以脱离棋盘判**（`PreviewSelfExcludedByCardCriteria`）。
//     判据 = `_ResolvePlayCardFromHand_d__447__MoveNext.c:849-860` → `BattleManager__IsThereValidUnitTarget`
//     → `RawCardScript__GetValidTargets.c:31-45`（扫**落位之后**的棋盘，逐候选 `IsValidTarget`）
//     ⇒ 那一刻**待落地那张自己就在候选池里**，且 `thisCard` 就是它 ⇒ `候选 == thisCard` 恒真。
//     ⇒ `includeSelf == 0` 的卡**不能把自己当目标**（现读 **14 张单位卡**属于这一类，
//     例：`AM70 Scion Medic`「Rally: Heal 2 to a friendly unit」· `DA5 Apothecary`）。
//     ⚠️ 这是**唯一**能脱离棋盘判出来的一格；其余 35 格要 `SideOf`（预览不在棋盘 ⇒ -1 ⇒ 判不了）
//     ⇒ 那一层要 `TargetCriteriaEval` 开一个 side 钩子，**本轮不做**（补丁见报告 §四）。
//     ⛔ 别把这条读成「预览已经过了整道 criteria 闸」。
//
//  ⛔ 本块追加在**文件尾**（`EffectResolver.cs` 有账落点按行号引用 ⇒ 只有尾部零漂移，铁律 18·b）。
// ==============================================================================================
namespace RuleEngine
{
    public static partial class RuleCore
    {
        /// <summary>🆕 `B-245`：卡级 `cardTargetCriteria.ExcludeNotSelf != 0` ⇒ 点选池**整池淘汰**。
        /// <para>判据与三条前提见本文件头 ②。现读全池 **0 张** ⇒ 今天零行为变化；
        /// `includeSelf` 那一格**刻意不补**（`候选 == thisCard` 恒假 ⇒ 跳过即等价）。</para>
        /// <para>⚠️ 这条对**单位卡的棋盘那批**也对：原版 `thisCard` = 待落地那张自己（它在落位后的池里，
        /// 不在我们这份「棋盘那批」里）⇒ 棋盘那批同样全淘汰；「它自己」由
        /// <see cref="PreviewSelfExcludedByCardCriteria"/> 那一支按 `includeSelf` 单独判。</para></summary>
        static bool CardCriteriaExcludeNotSelfEmptiesPool(CardDef card)
        {
            if (card == null || card.CardTargetCriteria == null) return false;
            var set = card.CardTargetCriteria;
            if (set.TargetsAffected != 10) return false;                            // 别的 ta 档原版更早就短路了（见头注释 ②）
            if (set.ExcludeNotSelf == CardDef.CountSetSpec.Unknown) return false;   // 判不了 ⇒ 不筛（与 `Matches` 同一保守方向）
            return set.ExcludeNotSelf != 0;
        }

        /// <summary>🆕 `B-245`：`PlayTargetPool` 的预览单位（= **待落地那张自己**）能不能留在候选池里。
        /// <para>只判 `includeSelf` 那一格（判据见本文件头 ③）—— 它**不依赖棋盘**：
        /// 预览对象就是这张牌落地后的单位 ⇒ `候选 == thisCard` 恒真 ⇒ 原版 `includeSelf == 0` 时把它淘汰。</para>
        /// <para>⚠️ 只对**单位卡**成立（`asIfOnBoard` 的语义 = 「这张牌落地后的单位」）。
        /// 其余格（阵营 / 兵种 / 关键词 / stat / trait …）仍要棋盘 ⇒ 这里**不判**。
        /// ⛔ 别把本函数读成「预览已经过闸」。</para></summary>
        static bool PreviewSelfExcludedByCardCriteria(CardDef card)
        {
            if (card == null || !card.IsUnit) return false;
            if (!card.CardTargetCriteriaVersioned || card.CardTargetCriteria == null) return false;
            if (card.Targeted == 0) return false;                                   // 原版 `get_targeted` 闸先挡（同 `ApplyCardTargetCriteriaFilter`）
            var set = card.CardTargetCriteria;
            if (set.TargetsAffected != 10) return false;
            if (set.IncludeSelf == CardDef.CountSetSpec.Unknown) return false;      // 判不了 ⇒ 保留（保守方向）
            return set.IncludeSelf == 0;
        }
    }
}
