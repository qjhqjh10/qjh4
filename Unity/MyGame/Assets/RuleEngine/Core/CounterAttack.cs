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

        // ==========================================================================================
        //  🆕 2026-10-26（执行代理 `B8_G` · 批次 8）：同族另外四个原版判据
        //  🔴 **一律追加在类尾**（本文件被 6 条账行按 `CounterAttack.cs:<行号>` 引用；
        //     在类中间插成员会让下面每一行整体下移 ⇒ 账里的落点全部漂）。
        // ==========================================================================================

        /// <summary>原版 `EntityScript.currentBastion`（`decomp_full/EntityScript__get_CurrentBastion.c:5`，🥇）
        /// = `GetCurrentTraitValueWithModifiers(0x352)`；`0x352 = 850 = DefinedTrait.bastion`
        /// ⇒ 我们的 `UnitState.Bastion`（同一格：`KwValue(KeywordTable.Bastion)`）。</summary>
        public static int CurrentBastion(UnitState u) { return u == null ? 0 : u.Bastion; }

        /// <summary>原版 `CardScript.HasBastionHigherThanAttack(CardScript attackingCard)`
        /// （`decomp_full/CardScript__HasBastionHigherThanAttack.c:1-29`，🥇 全文逐句）：
        /// `attackingCard == null` ⇒ **false**（`.c:16-17`）；否则
        /// **`attackingCard.currentCombatAttack <= this.currentBastion`**（`.c:18-21`）。
        /// <para>🔴 **是 `<=`**（「护甲**不小于**对方的当前战斗攻击」）。账上原来写「当前护甲 **>**
        /// 对方当前战斗攻击」—— **方向反了**，2026-10-26 现读订正（铁律 5）。</para>
        /// <para>⚠️ 本版把「对方当前战斗攻击」做成**显式实参**：调用方（`ConditionEval` 的 110 档）
        /// 手里有施法者 `UnitState` 却**没有它的 owner**，而 <see cref="CurrentCombatAttack"/> 要
        /// owner 才算得出 `pack` 那一类按方计的加成 ⇒ 让调用方算好再传。⛔ 别在本方法里改用
        /// `u.Attack` 顶替（那会静默漏掉 owner 相关的加成）。</para>
        /// <para>`self == null` ⇒ false（原版那一支会**抛异常**；我们不复制崩溃语义）。</para>
        /// </summary>
        public static bool HasBastionHigherThanAttack(UnitState self, int attackingCombatAttack)
        {
            if (self == null) return false;
            return attackingCombatAttack <= CurrentBastion(self);
        }

        /// <summary>原版 `EntityScript.GetCombatDamage(AttackTypes attackType)`
        /// （`decomp_full/EntityScript__GetCombatDamage.c:1-46`，🥇 全文逐句）：
        /// ① 带 `dropPod(0xe6)` ⇒ 读 **`+0xe0` 那一格存档攻击力**（ObscuredInt，`.c:24-38`）；
        /// ② 否则 残骸（`+0x65 != 0 && +0x66 == 0`）⇒ **0**（`.c:15-17`）；
        /// ③ 否则 `attackType == 2`（远程）⇒ `CurrentRangeAttack`，其余 ⇒ `CurrentMeleeAttack`（`.c:18-23`）。
        /// <para>与 <see cref="CurrentCombatAttack"/> 的差别**只两点**：打法由**实参**给（那边读
        /// `+0x120`）；`dropPod` 那一格**我们没建** —— 那边如实按普通档算（池内 0 张），
        /// 这里**返回 `null`**（判不了），⛔ 不拿普通档冒充空投舱攻击。</para>
        /// <para>可达性：池内带 `droppod` 的卡 **0 张**（2026-10-26 现读）⇒ 今天恒走 ②③。</para>
        /// </summary>
        public static int? CombatDamage(UnitState u, int attackType)
        {
            if (u == null) return 0;
            if (u.Has(KeywordTable.DropPod)) return null;         // 原版 `.c:24-38` 的存档值我们没建
            if (u.IsRemnant && !u.IsLeavingRemnant) return 0;     // 原版 `.c:15-17`
            return attackType == 2 ? u.RangedAttack : u.Attack;   // 原版 `.c:18-23`
        }

        /// <summary>原版 `EntityScript.DamageKillsTarget(CardScript target, int damage)`
        /// （`decomp_full/EntityScript__DamageKillsTarget.c:1-40`，🥇 全文逐句）：
        /// **`Max(1, damage − target.currentArmour(+0xb8)) >= target.currentHealth(+0x68)`**。
        /// <para>🔴 它**只减护甲** —— 不看 shield / vulnerable / bastion / 其它减伤（那些在原版是别的跳）。
        /// ⛔ 别拿 <see cref="RuleCore.DamageAfterReduction"/> 顶替（那一个带易伤与堡垒）。</para>
        /// </summary>
        public static bool DamageKillsTarget(UnitState target, int damage)
        {
            if (target == null) return false;
            int dmg = System.Math.Max(1, damage - target.Armor);
            return target.Health <= dmg;
        }

        /// <summary>原版 `EntityScript.EnemyReturnsAttack(CardScript attackedCard, bool isAttacker,
        /// AttackTypes attackType)`（`decomp_full/EntityScript__EnemyReturnsAttack.c:1-82`，🥇 全文逐句）
        /// —— **「这一记会不会吃反击」的【预测】版**：`true` = 敌方会反击。
        ///
        /// <para>🔴 **原版有【两个】同族判据，⛔ 别混**：</para>
        /// <list type="number">
        /// <item>**实际结算** = `BattleManager._ResolveAttack_d__438__MoveNext.c:871-895` 那条链 ——
        ///   sneakAttack(0x14) → ActivatesTerror(0x208) → ActivatesNoReturnSniperAttack(0x3de) →
        ///   ActivatesNoReturnLongRangeAttack(0x488)，**四道** ⇒ 就是本文件的
        ///   <see cref="NoCounter"/>（文件头那段判据仍是它的）。</item>
        /// <item>**预测**（本方法）= 那四道 **＋ 第 5 道 `shuriken(0x460 = 1120)`**（`.c:60-73`）：
        ///   `GetCurrentTraitValueWithModifiers(0x460)` 的值 `DamageKillsTarget` 守方、且守方
        ///   **未被保护** ⇒ 免反击。语义上说得通：星镖伤害在**主伤害之前**结算
        ///   （`_ResolveAttack…:686`），它把人打死就没反击。
        ///   ⚠️ 第 5 道**不要求远程**（它只要求 `IsUnit(守方)`）—— 与 sniper / longRange 那两道不同。</item>
        /// </list>
        /// <para>原版三个调用点 = `BattleManager__RecordAttackPendingDamage.c:84`（登记待结算伤害）·
        /// `AI__ScoreFromBuffedAttack.c:77`（AI 估值）· `CardHighlight__ToggleCombatPreviewHighlight.c:53`
        /// （战斗预览高亮）。</para>
        /// <para>⚠️ **本版尚未接线**（如实标）：我们的引擎**没有「待结算伤害队列」**（伤害逐段现结），
        /// 原版那三个调用点在 <c>Core/</c> 里只对应 `Data/AiScoring.cs:578` 与
        /// `Data/SimpleAI.cs:604`（两个文件都不在本代理白名单）⇒ 本方法目前**没有调用方**。
        /// ⛔ 别把它塞进 <see cref="NoCounter"/> —— 那会改掉**实际结算**那条链（原版那里没有第 5 道）。</para>
        /// <para>`IsUnit(守方)` 在原版 = `cardType == 10(Hero) || 0(Minion)`（`RawCardScript__IsUnit.c:3-8`）
        /// ⇒ **督军也算**；我们棋盘上的候选恒是单位/督军 ⇒ 这一道恒真，不另判。</para>
        /// </summary>
        public static bool EnemyReturnsAttack(BattleContext ctx, int attackerOwner, UnitState attacker,
                                              int targetOwner, UnitState target, bool ranged)
        {
            if (attacker == null) return false;
            // 前四道与实际结算同源；`targetWillDie` 原版预测侧用的就是 `GetCombatDamage(attackType)`。
            int? main = CombatDamage(attacker, ranged ? 2 : 1);
            bool targetWillDie = main.HasValue && DamageKillsTarget(target, main.Value);
            if (NoCounter(ctx, attackerOwner, attacker, targetOwner, target, ranged, targetWillDie))
                return false;

            // 第 5 道：`shuriken(1120)`（`.c:60-73`）。⚠️ 不要求远程（见方法 doc）。
            if (attacker.Has("shuriken"))
            {
                int sh = attacker.KwValue("shuriken");
                if (DamageKillsTarget(target, sh) && ConditionEval.MeetsUnit(ctx, target, 120) != true)
                    return false;
            }
            return true;
        }
    }
}
