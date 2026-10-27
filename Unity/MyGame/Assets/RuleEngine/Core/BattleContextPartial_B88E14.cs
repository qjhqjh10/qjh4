// BattleContextPartial_B88E14.cs — `BattleContext.Emit(...)` 的**带 `priority` 重载**（`partial`，与 `BattleContext.cs` 同一个类）
//
// 🔴 2026-10-27（B88 · 执行代理 `E14` · 施工单 `资料/完全复刻/判据/B88_E5_AbilityPriority_发侧施工单.md`）
//   新开这个文件的原因（与 `BattleContextRandom.cs` / `BattleContextTurnCounter.cs` / `BattleContextExcessDamage.cs`
//   同一条纪律）：`BattleContext.cs` 被账按 `文件:行号` 引用，而且**同一批还有别的写手**在改它
//   ⇒ **主文件一行不动**，新重载放进**同一个类的另一个 partial 文件**。
//
// 它做的事：把原版 `CardAbility.priority`（`ActionPriority`：0 Player / 1 Instant / 2 Interrupt /
//   3 DelayedInstant）带到 `BattleEvent.Priority` 上。表现侧（`BattleDriver` 的三处排期）
//   **已经**在读那一列（`BattleDriver.cs:19558` / `:19724` / `:19745`），缺的只是「发」这一半。
//
// 判据（逐条，施工单 §一，第一权威 = 全量反编译）：
//   1. `BattleManager.AddCardAnim(…, int priority, …)` 的两处 `AbilityLogic.PlayAnim` 调用点传的是
//      `AbilityData.priority`（`decomp_full/AbilityLogic__PlayAnim.c:223`，`param_3 + 0x44`）；
//   2. `AbilityData.priority` 由 `RawCardScript.TriggerAbility` 从 `CardAbility.priority(+0x18)` 灌入
//      （`decomp_full/RawCardScript__TriggerAbility.c:32`）；
//   3. 主动技那一支**固定 1**（`decomp_full/RawCardScript__ResolveActiveAbility.c:47`）；
//   4. 缺省 = **1**（`CardAbility__.ctor.c:5` 写 `+0x18 = 1`）⇒ 我们 `CardDef.DefaultTriggerPriority = 1` 口径对。
//
// 🔴🔴 **为什么 `priority` 排在【第 5 位】而不是排在最后**（2026-10-27 当场修的 20 条 `CS0121`）：
//   第一版写成「尾部多一个 `int priority = 0`（带默认值）」⇒ **所有既有 15 实参的调用同时匹配两个重载**
//   ⇒ 全仓二义（`TutorialScript.cs:1496` / `Enchantment.cs:360` / `EffectResolver.cs` 多处 /
//   `RuleCore.cs:2947` 等 **20 处 `CS0121`**）。
//   而 C# **不允许「必填形参排在可选形参之后」**（`CS1737`）⇒ 没法只把**最后**那个 `priority` 变成必填。
//   ⇒ 本重载把 `priority` 放在**第 5 位、必填、且后面全是可选**（合法）：
//     · 既有调用（4 个位置实参 + 具名实参）**少了这个位置实参** ⇒ 本重载不适用 ⇒ 只匹配主文件那一版；
//     · 新调用点写 `priority: 1`（**具名实参**，C# 7.2 起可出现在位置实参之后）⇒ 只有本重载适用。
//   ⚠️ 这两条**互相排斥**，所以不会再有二义；也**不必**让调用点把 16 个实参全写出来。
//   ⚠️ **`priority` 必须显式传** —— 「填 1 ≠ 不填」：不传就走主文件那一版（`Priority = 0` = `Player` 道
//   = 消费序**最后**）；原版缺省是 1（`Instant`）⇒ 该按原版分道的地方**必须显式填**。
namespace RuleEngine
{
    public partial class BattleContext
    {
        /// <summary>
        /// 同主文件那个 <c>Emit(EvtKind, int, int, string, …)</c>，**多一个必填的 `priority`**（第 5 位，理由见文件头）。
        /// <paramref name="priority"/> = 原版 `CardAbility.priority`（`ActionPriority`：
        /// <c>0</c> Player / <c>1</c> Instant / <c>2</c> Interrupt / <c>3</c> DelayedInstant；
        /// ⛔ 别把 <c>0</c> 当「没填」—— 原版缺省是 1，见 <see cref="CardDef.DefaultTriggerPriority"/>）。
        /// 调用一律写成 `ctx.Emit(kind, p, slot, name, priority: 1, keyword: …)`（具名，⛔ 别裸传那个 `1`）。
        /// </summary>
        /// <remarks>
        /// 实现上**不复制字段清单**（铁律「两处写同一条规则 = 迟早不一致」）：先走主文件那条唯一实现，
        /// 再把 `Priority` 补到**刚发出去的那一条**上。
        /// ⚠️ 内部那条调用是 **15 个位置实参**（第 5 个是 `string keyword`）⇒ 本重载要的第 5 个是 `int`
        /// ⇒ 不适用 ⇒ 只会匹配主文件那一版（⛔ 不会递归）。
        /// </remarks>
        public void Emit(EvtKind kind, int player, int slot, string cardId, int priority,
                         string keyword = null, string effect = null,
                         int amount = 0, bool ranged = false,
                         int targetPlayer = -1, int targetSlot = -1,
                         string targetCardId = null,
                         DeploySource deployFrom = DeploySource.Unknown,
                         int instanceId = 0, int targetInstanceId = 0,
                         int abilityIndex = -1)
        {
            int before = Signals.Count;
            Emit(kind, player, slot, cardId, keyword, effect, amount, ranged,
                 targetPlayer, targetSlot, targetCardId, deployFrom,
                 instanceId, targetInstanceId, abilityIndex);
            if (priority == 0) return;                 // 0 = 改前行为：一个字节都不动
            // `Emit(BattleEvent)` 把**同一个对象**同时放进 `Signals` 与 `ActionLog`
            // （`BattleContext.cs:1880-1881`：`Signals.Add(e); AppendLog(e);`）⇒ 改一处两边都对。
            if (Signals.Count > before)
            {
                BattleEvent e = Signals[Signals.Count - 1];
                if (e != null && e.Kind == kind) e.Priority = priority;
            }
        }
    }
}
