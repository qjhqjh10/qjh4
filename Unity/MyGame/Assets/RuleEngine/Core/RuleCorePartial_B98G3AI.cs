// RuleCorePartial_B98G3AI.cs — 第九十八会话 · 波 5b · 执行代理 `B98_G3` 的 `RuleCore` partial 扩展
//
// 🔴 为什么单开文件：`RuleEngine/Core/RuleCore.cs` 被 `账/*.tsv` 按【行号】引用 400+ 处 ⇒
//   往它中间插成员会让下游落点整体下移。本批与 `RuleCorePartial_B97W34.cs` / `RuleCorePartial_B88E1.cs`
//   同形：**源文件只做「行数不变」的就地改写**，新成员一律落本文件。
//   ⛔ 本笔**一个字都没动 `RuleCore.cs`**（那是 `B98_G1` 的独占写手范围）。
//
// 本批（B98_G3）落在这里的只有一件：账 `战斗逻辑驱动 :324 / :325` 的
//   **「AI 攻击动作表」的【攻击目标】判据** —— `AttackTargetGate`。
//
// ============================ 判据（🥇 第一权威，`decomp_full` 现读）============================
//   `d:/2/tools/decomp_full/BattleManager__GetAvailableAttackActions.c`
//     · 近战那一支（`:126` 判目标 → `:128-140` 的三个合取项）
//     · 远程那一支（`:152` 判目标 → `:154-166` 的两个合取项）
//   两支的**目标侧**各有一句 `CardScript__EnoughPendingDamageToDie(目标)`：
//     `cVar2 = CardScript__EnoughPendingDamageToDie(lVar6,0); if (cVar2 == '\0') { …才排动作… }`
//     ⇒ **目标已经带着致死待结算伤害 ⇒ 近战/远程【两档都不排】**（不是只不排其中一档）。
//   **攻方侧**那两道（`0 < get_CurrentMeleeAttack` / `!HasCurrentTrait(0x3ca pindown)` / `0 < get_CurrentRangeAttack`）
//   的判据**已经有一份**：`RuleCorePartial_B97W34.cs:46` 的 `AttackActionPowerGate`（⛔ 本文件**不复制第二份**，
//   只转调 —— 工程铁律「两处写同一条规则 = 迟早不一致」）。
//
// ============================ 为什么要有这个函数（结构说明，不是自设计）============================
//   原版那几条闸同时服务两件事：①「这一格能不能打」（`IsValidAttackTarget` / `CanAttackNow`）；
//   ②「动作表排不排这一条」（`GetAvailableAttackActions`）。我们这一层**没有** `AvailableAction` 中间层，
//   而 `RuleCore.IsValidTarget`（`RuleCore.cs:5402`）是**共用**口（它同时服务**技能目标**）：
//   2026-10-29 实测把这两道闸落进 `IsValidTarget` ⇒ `play check` ①-b 由 **50 张变 414 张**（技能目标被误挡）
//   ⇒ 已回退（`RuleCore.cs:5625` 那段「调度台就地回退」注释），只保留 `CanAttackNow`（`RuleCore.cs:5770`）那一处。
//   ⇒ **动作表那一侧**需要一份**单独的「攻击目标」判据** = 本函数。
//   这是**实现结构不同、语义等价**（与账 `:193` ③ 同一裁定口径），§18 不要求打 `🔶`。
//
// ============================ 谁调用（全仓**唯一**）============================
//   `RuleEngine/Data/SimpleAI.cs:374` —— 攻击枚举那一格，**同一物理行**追加（零行数变化）：
//     `if (RuleCore.IsValidTarget(…) != RuleCodes.OK || RuleCore.AttackTargetGate(…) != RuleCodes.OK) continue;`
//   ⛔ **玩家路径不加**：原版那两条闸只作用于动作表；玩家点目标走 `DeclareAttackCore`（`RuleCore.cs:6295`）
//      → `CanAttackNow`（`:6304`），攻方那两道闸已经在那儿；目标侧那一半原版本来就不在 `IsValidAttackTarget` 里。
//   ⛔ `RuleEngine/Data/AiScoring.cs:564` **没加**（核过原版那条路 ⇒ 见报告 `B98_G3.md` §三）。
namespace RuleEngine
{
    public static partial class RuleCore
    {
        /// <summary>
        /// 🔴 **2026-10-29（`B98_G3` · 账 `:324 / :325`）**：「AI 动作表排不排得出**这一条攻击**」的**唯一判据**。
        ///
        /// <para>**逐句对应原版 `BattleManager__GetAvailableAttackActions`**（`decomp_full/…c` 现读）：
        /// <list type="number">
        ///   <item>`:126`（近战）/ `:152`（远程）：`CardScript__EnoughPendingDamageToDie(目标)` 为真 ⇒ **两档都不排**；
        ///         目标为 null 时同走「不排」（原版走到 `System_Collections_Generic_List…__get_Item` 取不到就被
        ///         `UnityEngine_Object__op_Equality` 那一路挡掉；我们这里给它 `ErrNotUnit`，与 `IsValidTarget` 同码）。</item>
        ///   <item>`:128-140` / `:154-166`：攻方的**攻击力 > 0** 与 **近战专属 `pindown(0x3ca)`** ——
        ///         逐字转调 <see cref="AttackActionPowerGate"/>（判据只一处，⛔ 不复制第二份）。</item>
        /// </list></para>
        ///
        /// <para>⚠️ **`EnoughPendingDamageToDie(null)` 返 `true`**（`RuleCore.cs:7739` 的 `if (u == null) return true;`）
        /// ⇒ `target == null` **必须单独判**：否则「没有目标」会被当成「目标将死」，
        /// 虽然这里返回码相同（`ErrNotUnit`），但语义要写清楚（将来换了返回码就会分叉）。</para>
        ///
        /// <para>⚠️ **次序照原版**：先判目标（`:126` 在 `:128` 之前），再转调攻方那两道闸。</para>
        /// </summary>
        /// <param name="ctx">对局上下文（`AttackActionPowerGate` 算当前攻击力要用）。</param>
        /// <param name="p">攻方座位（`0` / `1`）。</param>
        /// <param name="attacker">攻方单位（枚举里已经滤过 null / 不在场，这里仍按原版挡一次）。</param>
        /// <param name="target">目标单位（可能为 null）。</param>
        /// <param name="ranged">`true` = 远程那一档；`false` = 近战那一档。</param>
        /// <returns>`RuleCodes.OK` = 这一档排得出；否则 = 原版那一支会拒绝的返回码（与 `CanAttackNow` / `IsValidTarget` 同码系）。</returns>
        internal static int AttackTargetGate(BattleContext ctx, int p, UnitState attacker, UnitState target, bool ranged)
        {
            if (attacker == null) return RuleCodes.ErrNotUnit;
            // 目标侧（判据 `.c:126` / `:152`）：没有目标 / 目标已带致死待结算伤害 ⇒ 近战远程两档都不排
            if (target == null || EnoughPendingDamageToDie(target)) return RuleCodes.ErrNotUnit;
            // 攻方侧（判据 `.c:128-140` / `:154-166`）：转调共用那一份（⛔ 别在这儿再写一遍攻击力/pindown）
            return AttackActionPowerGate(ctx, p, attacker, ranged);
        }
    }
}
