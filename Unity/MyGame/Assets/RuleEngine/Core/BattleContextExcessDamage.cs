// BattleContextExcessDamage.cs — `BattleContext` 的「上一次伤害的**超额量**」那一格（`partial`，与 `BattleContext.cs` 同一个类）
//
// 🔴 2026-10-27（B88 · 波 3 · 执行代理 `E2` · 账 77 的 `210 TargetExcessDamage`）新开这个文件的原因
//   （与 `BattleContextRandom.cs` / `BattleContextTurnCounter.cs` 同一条纪律）：
//   `BattleContext.cs` 被账按 `文件:行号` 引用 —— 往它中间插字段会让下游落点整体下移
//   ⇒ 新字段放进**同一个类的另一个 partial 文件**，主文件一行不动。
//
// 判据（原版）：`ChooseValueOptions.TargetExcessDamage = 210`
//   （`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ChooseValueOptions.cs:28`）；
//   池内唯一消费者 = `Beautiful Death`（`Deal 10 damage to an enemy troop.
//   **For each excess damage, heal 1 to your Warlord**`，`cards_engine.json` 现读
//   `valueOptions / heal` 那一对里出现 210 —— 权威表见
//   `资料/完全复刻/判据/ValueOptions异常值_判定.md` §一 的分布）。
//
namespace RuleEngine
{
    public partial class BattleContext
    {
        /// <summary>
        /// **上一次「造成伤害」那一下的【超额量】**（原版 `ChooseValueOptions.TargetExcessDamage = 210`
        /// 读的那一格）—— 「打出去的点数**超过目标当前生命**的那部分」。
        ///
        /// <para>**默认值语义（三条，⛔ 别按别的理解）**：</para>
        /// <list type="number">
        /// <item>`0` = **没有超额**，或**本条 ability 还没有造成过伤害**。两种情况**故意同值** ——
        ///   原版这一位来自那一次 `DamageInfo`，没有 `DamageInfo` 时它也读不到超额。</item>
        /// <item>**每条 ability 起手清零**：两个 `RuleCore.ResolveOps` 入口各调一次
        ///   <see cref="ResetDamageExcess"/>（= 原版每次 `PlayAbility` 新建 `DamageInfo` 那一拍的等价物）
        ///   ⇒ **⛔ 不会读到上一张卡的残值**（这正是 `LastCreated` / `LastTarget` 那一族踩过的坑：
        ///   跨卡残值会静默多数几遍）。</item>
        /// <item>**一条 ability 内逐次覆盖、不累加** —— 同一段正文里打了两次，读到的是**后一次**。
        ///   ⚠️ 池内唯一消费者（`Beautiful Death`）是「打一次、然后按超额治疗」，不落在这一格里。</item>
        /// </list>
        ///
        /// <para>**写点（唯一）**：`EffectResolver.DoDeal` 的 `Hurt(...)` 之前那一拍
        /// （超额 = `DamageAfterReduction(目标, 点数) − 目标当前血`，取正部）。
        /// **读点（唯一）**：`EffectResolver.CountFor` 的 `CountScope == "excess"` 那一支。</para>
        ///
        /// <para>🔴 **已知近似（如实标）**：我们用 `RuleCore.DamageAfterReduction` 算「打完多少」，
        /// 它**不含** `ignoreBastion` 那一档（堡垒）与「多目标时取哪一个」的判据 ——
        /// 多目标时我们取**最后一个目标**的超额。今天唯一消费者是**单目标**卡，两处近似都零可观察。</para>
        /// </summary>
        public int LastDamageExcess;

        /// <summary>把 <see cref="LastDamageExcess"/> 归零（每个 `ResolveOps` 入口各调一次 —— 见字段 doc）。</summary>
        public void ResetDamageExcess()
        {
            LastDamageExcess = 0;
        }
    }
}
