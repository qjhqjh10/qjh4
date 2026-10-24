// CounterAttack.cs — 「这一记攻击会不会吃反击」的**唯一判据**
//
// 原版四条豁免通道（第一权威，现读 `d:/2/tools/decomp_full/`）：
//   `BattleManager._ResolveAttack_d__438__MoveNext.c:871-895` 是**一条链**：
//     `!HasCurrentTrait(攻方, 0x14 /* sneakAttack = 20 */)`
//     → `EntityScript__ActivatesTerror(攻方, 守方, 0)`
//     → `EntityScript__ActivatesNoReturnSniperAttack(攻方, 守方, 1, attackType)`
//     → `EntityScript__ActivatesNoReturnLongRangeAttack(攻方, 守方, 1, attackType)`
//     → 四道全假才 `ResolveAttackDamage(守方 → 攻方, …)`（= 反击落血）。
//   ⇒ **四道是 OR 关系**（任一道为真 ⇒ 免反击）；这个文件把它们收成一处。
//
//   · **sneakAttack(20)**：无条件（`EntityScript__HasCurrentTrait(攻方, 0x14)`）。
//   · **terror(520)**：攻方带 trait ∧ 守方 `cardType == 0`（**Minion；不含 Hero/督军**）
//     ∧ `守方.currentCombatAttack < 攻方.currentCombatAttack`
//     （`EntityScript__ActivatesTerror.c` 全文）。
//   · **sniper(990)**：`attackType == 2`（远程）∧ 攻方带 trait ∧ 守方 `IsUnit`
//     ∧ `DamageKillsTarget(攻方, 守方, 远程攻击力)` ∧ **守方未被保护 / 没有 survivor**
//     （`EntityScript__ActivatesNoReturnSniperAttack.c`：`IsProtectedFromDamageOrSurvivor` 为假才放行）。
//   · **longrange(1160)**：`attackType == 2`（远程）∧ 攻方带 trait ∧ 守方 `IsUnit`
//     （`EntityScript__ActivatesNoReturnLongRangeAttack.c`）。
//
// `currentCombatAttack`（`EntityScript__get_currentCombatAttack.c`）：
//   · 残骸（`+0x65` = `isRemnant` 且 `+0x66` = `isLeavingRemnant` 为假）⇒ **0**；
//   · 否则按 `currentAttackType(+0x120)`：`2` = 远程取 `CurrentRangeAttack`，其余取 `CurrentMeleeAttack`；
//   · `dropPod(230)` 那一支读 `+0xe0` 的存档值 —— 我们**没建**那一格，池内 **0 张**带 `dropPod`
//     ⇒ 按普通档算；如实标着，⛔ 别拿「猜一个字段」冒充原版。
//
// ⚠️ **这个文件是「同一条规则的唯一写法」**：`RuleCore` 的两处（实际反击 / Strike 预判）与
//    `Data/SimpleAI` 的评分都应当转调这里。2026-10-23 起 `RuleCore` 两处已接；
//    `SimpleAI` 那一处等 `新账-88` 代理交回文件后接（它当前另有整文件写权）。
//    ⛔ 别在别处再写一份 `ranged && Has(LongRange)` —— 那正是本条要治的「只做了一半」。

namespace RuleEngine
{
    public static class CounterAttack
    {
        /// <summary>
        /// 攻方这一记**是否免反击**。四道通道逐条照原版（见文件头）；`targetWillDie` =
        /// 「这一记的伤害会不会摧毁守方」（Sniper 那道用）—— 调用方各自用自己的预测口径：
        /// 实际结算看 `targetDied`；Strike 预判看 `DamageAfterReduction(...) >= Health`；
        /// AI 评分看 `RuleCore.WouldKill(...)`。
        /// </summary>
        public static bool NoCounter(BattleContext ctx, int attackerOwner, UnitState attacker,
                                     int targetOwner, UnitState target, bool ranged, bool targetWillDie)
        {
            if (attacker == null || target == null) return false;

            // ① sneakAttack(20)：**无条件**（原版连守方是什么都不看）
            if (attacker.Has(KeywordTable.SneakAttack)) return true;

            // ② terror(520)：守方必须是 **Minion**（`get_cardType == 0`；Hero/督军不算），
            //    且守方的当前战斗攻击力 **严格小于** 攻方。
            if (attacker.Has(KeywordTable.Terror) && !target.IsWarlord
                && CurrentCombatAttack(ctx, targetOwner, target) < CurrentCombatAttack(ctx, attackerOwner, attacker))
                return true;

            if (!ranged) return false;      // 后两道只对**远程打法**说话

            // ③ LongRange(1160)：远程 + 守方是单位（不要求击杀）
            if (attacker.Has(KeywordTable.LongRange)) return true;

            // ④ Sniper(990)：远程 + 这一记会摧毁守方 + 守方**没有** dodge/shield/invulnerable/dropPod/survivor
            //    （原版 `IsProtectedFromDamageOrSurvivor`；我们收在 `ConditionEval` 的 120 那一格）
            if (attacker.Has(KeywordTable.Sniper) && targetWillDie
                && ConditionEval.MeetsUnit(ctx, target, 120) != true)
                return true;

            return false;
        }

        /// <summary>
        /// 原版 `EntityScript.currentCombatAttack`：按**当前攻击型**取近战/远程那一档；
        /// 残骸 = 0（`+0x65 && !+0x66`）。`dropPod` 的存档值那一支没建模（池内 0 张，见文件头）。
        ///
        /// 🔴🆕 **2026-10-24（`C1` §5）订正**：原来这里只判 `u.IsRemnant` ⇒ 少了
        /// `!isLeavingRemnant(+0x66)` 那一维。原版 `get_currentCombatAttack.c:40-47`：
        /// **`(+0x65 != 0) && (+0x66 == 0)` ⇒ return 0**；「正在离场的残骸」**落回**正常攻击值。
        /// 语义已按判据补齐（`UnitState.IsLeavingRemnant`）；⚠️ **写入点未查清 ⇒ 今天不可达**，
        /// 由合成夹具断言（`RuleEngineTest_EngineB3`）。
        /// </summary>
        public static int CurrentCombatAttack(BattleContext ctx, int owner, UnitState u)
        {
            if (u == null) return 0;
            // 🔴 `+0x65 && !+0x66` ⇒ 0（见上面那段 2026-10-24 订正）
            if (u.IsRemnant && !u.IsLeavingRemnant) return 0;
            bool ranged = u.CurrentAttackType == UnitState.AttackTypeRanged;
            return RuleCore.FieldAttack(ctx, owner, u, ranged);
        }
    }
}
