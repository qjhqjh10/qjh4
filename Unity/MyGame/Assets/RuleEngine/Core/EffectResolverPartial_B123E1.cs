// EffectResolverPartial_B123E1.cs — 批次 **B123** · 执行代理 `E1`（附魔 / 引擎主路 · `EffectResolver`）
//
// ── 本文件装什么 ─────────────────────────────────────────────────────────────────────────────
//   三件「判据已定、只差落代码」的共用件 + 它们的自检门：
//     ① `ApplyStunTo`        —— `DoStun`（`EffectResolver.cs:4373`）的**施加段**抽出来的唯一实现
//                               （`B121-D5-04` A 半）。附魔路 `ca 130 stun` 要绕开
//                               `AddEffectCasterGates`（原版 `AddStun` 没有那道 jam 闸）。
//     ② `TryResolveCriteriaValue203` —— `valueOptions 203` = `min + thisCard.CurrentShuriken × max`
//                               （`B121-D1B-03` ①，判据 `AbilityLogic__GetValueFromCriteria.c:320-325`）。
//     ③ `ResolveTargetsTa230`        —— `ta 230` = 两位督军里**当前生命更高**的那一个
//                               （`B121-D1B-03` ②，判据 `AbilityLogic__GetTargets.c:809-833`）。
//
//   ⚠️ ②③ 的**开关仍在** `EnchantmentExecutor_B115.cs` 的两个 `switch` 里（本批那个文件由 `E2` 独占）
//      ⇒ 本文件只交**可直接调用的实现 + 自检**，接线补丁见报告
//      `资料/完全复刻/报告/B123_E1_EffectResolver.md` §3「可粘贴补丁」。
//
// ── 为什么新开一个 partial 文件，而不是插进 `EffectResolver.cs` ────────────────────────────────
//   `EffectResolver.cs` 有 **368 处账落点按 `文件:行号` 引用**（现读）⇒ 往里插成员会让整片账漂移
//   （铁律 18·b）。本文件是**新文件、零账落点**，新成员一律放这里。
// ==============================================================================================
using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==========================================================================================
        //  ① `B121-D5-04` A 半：`DoStun` 的**施加段**共用件
        // ==========================================================================================

        /// <summary>**把「眩晕」施加到某一个单位上**（`DoStun` 循环体的唯一实现）。
        ///
        /// <para>判据（`d:/2/tools/decomp_full/CardScript__Stun.c` 逐句现读）：
        /// `:44-45` 的 `cardState ∈ {2, 0xf, 3, 0x11}`（= `IsInPlay()`）闸 · `:46-47` 的
        /// `HasCurrentTrait(0xfa unstunnable)` 跳过 · `:48` `AddTraitSilently(param_1, 100, …)` ·
        /// `:98` 清 `+0x55`（`StunnedAtStartOfTurn`） · `:125` `DisplayStatusAnim`。</para>
        ///
        /// <para>🔴 **`EffectOp` 是必须传的、⛔ 别换成 `string source`**：闸③
        /// <see cref="AddEffectRemnantAllowsOp"/> 收 `op`（它只用 `op.Source` 记日志，但**收的是 `EffectOp`**）。
        /// 附魔路（`EnchantmentExecutor_B115.RunStunRowOfEnchantment`）自己会造一个最小 `EffectOp`
        /// ⇒ 两路的残骸门语义逐字相同。若改成只传字符串，`op` 为 `null` 时那条路的残骸门就**静默放宽**。</para>
        ///
        /// <para>🔴 **闸② jam / 闸④ 施放者不在场不在这里**（`AddEffectCasterGates`，留在 `DoStun`）：
        /// 原版 `PlayAbility.c:642-657` 的 `ca 130` 逐目标 `AddStun` **没有**那道 jam 闸。</para>
        ///
        /// <para>⚠️ 语句顺序**不能换**：`!IsAlive` → 闸① → 闸③ → unstunnable → 已晕跳过 → 挂关键词 →
        /// 演出 → 清回合初闸门 → 广播。逐条理由见方法体内那段 `A1166 / A1224 / A1134 / A1116 / A1121` 注释。</para>
        /// </summary>
        /// <param name="t">目标单位（`null` / 已死不处理 —— 与改前 `!t.IsAlive` 逐字同义）。</param>
        /// <param name="by">日志标签（调用点的 `by`，⛔ 不是卡名）。</param>
        /// <param name="op">这一下眩晕的 `EffectOp`（**闸③ 要用**；`null` ⇒ `op.Source` 取不到，走 `by`）。</param>
        internal static void ApplyStunTo(BattleContext ctx, UnitState t, string by, EffectOp op)
        {
            if (ctx == null || t == null || !t.IsAlive) return;
            string source = op != null ? op.Source : by;

            // 🔴 `AddEffect` 闸①（卡状态）+ 闸③（残骸门）—— `Stun` 在原版是 `addTrait(3)`，
            //    ⇒ 残骸**一律不收**（只有 `addStats` 的生命项与 `addTraitRemnant(90)` 放行）。
            if (!AddEffectCardStateAllows(ctx, t, by)) return;
            if (!AddEffectRemnantAllowsOp(ctx, t, op, by)) return;

            // 🆕 **2026-10-09（`A1166`）**：上面那句 `!IsAlive` 对应原版 `CardScript__Stun.c:44-45`
            //    那道 `cardState ∈ {2, 0xf, 3, 0x11}`（= `CardScript.IsInPlay()`）闸。
            //    🔴 **2026-10-11（`A1224`）就地订正：这一段原来写着「已判等价、⛔ 不另加判据」——
            //    那句话【错了一档】，别再照它判「不用改」。** 错在哪：
            //      · 原来那句的前提是「目标一律来自 `ResolveTargets` → `AddSide`（**只遍历 `ps.Board[s]`**）⇒ 按构造就在棋盘上」；
            //      · 但**代词支根本不走 `AddSide`**（`ResolveTargets` 的 `prev` 支就 `return` 了）⇒ 那句前提对代词目标**不成立**；
            //      · 而且 `!IsAlive`（= `Health > 0`）**比 `IsInPlay()` 收得更紧**：
            //        「血 ≤ 0、但要**翻面成残骸**」那一档原版**在场上、会晕**，我们**不晕**。
            //    ⇒ `A1176` 已把「等价」那个结论推翻（判定口 = `StunStillOnBoard` / `BoardOccupantByInstance`）；
            //      `DoStun` 里那段**代词映射**就是这同一个结论在效果 op 这条路的上半。
            //    ⚠️ 这一句 `!IsAlive` **留着**（放开它反而会把**真死**的目标混进来）。
            //    逐档对照（18 档 `CardStateOptions`）见 `StunBlockedByTraits` 下方那段 `A1166` 注释。

            // 🔴 **2026-10-09（`A1134`）：`unstunnable`（原版 `HasCurrentTrait(0xfa)`）⇒ 整个施加段跳过。**
            //    判据 = 原版 `CardScript__Stun.c:46-47`：
            //      `cVar13 = EntityScript__HasCurrentTrait(param_1, 0xfa, 0); if (cVar13 == '\0') { …施加… }`
            //    —— 非假时那一支只打一条 `LogWarning` + `ActivateUnstunnable` 音效，**不加 trait**。
            //    ⚠️ **是「当前关键词」（`UnitState.Has` = 可被运行时授予），⛔ 不是印刷关键词。**
            //    判定口共用 `RuleCore.StunBlockedByTraits`（震荡那一格、`SimpleAI.ScoreStun` 同一条）。
            if (RuleCore.StunBlockedByTraits(t))
            {
                ctx.Log($"{by}：「{source}」想眩晕 {t.Name}，但它有 `unstunnable`"
                      + "（原版 `CardScript__Stun.c:46` 直接跳过施加）");
                return;
            }
            if (t.IsStunned) return;      // 已经是了：见下面那条「广播不能重复」

            // 🔴 **2026-10-09（`A1116`）**：原来是 `t.IsStunned = true;`（**只写字段、不挂关键词**）
            //     ⇒ 改成**挂 `stun` trait**。判据 = 原版 `CardScript__Stun.c:48`
            //     `AddTraitSilently(param_1, 100, …)` —— 全反编译里施加眩晕**只此一条路**；
            //     读点全是 `HasCurrentTrait(100)`（`CheckStun` / `CanAttackCard` /
            //     `IsValidAttackTarget` / `AllowResolveAttack` / `get_mightAct` / `ActivateMinion` …）
            //     ⇒ **原版只有一份表示**，`IsStunned` 现在的定义就是它。
            t.AddKeyword(KeywordTable.Stun, 1);
            // 🆕 **2026-10-24（引擎线 `G1b` · `C2` §6）**：原版 `CardScript__Stun.c:125` **每次被晕**
            //   都调 `DisplayStatusAnim`。本行前面已有 `if (t.IsStunned) return;` ⇒ 走到这里
            //   一定**首次**（`Amount = 0` ⇒ 由 `SyncKeywords` 的「新出现」路演，不在这里重复演）。
            EmitStatusApplied(ctx, t, KeywordTable.Stun, reapply: false);
            // 🆕 2026-10-09（`A1121`）：**施加时清「回合开始时就在这个状态」那个闸门** ——
            //    判据 = `CardScript__Stun.c:98`（`*(undefined1 *)(card + 0x55) = 0;`）。
            //    ⇒ 这一下眩晕**不会**在**本回合末**被 `EndTurn` 摘掉
            //    （它是「回合中途挂上的」，不是「回合开始时就在的」）——
            //    要撑到它自己的下个回合末才摘 ⇒ 「只废一个回合」。
            //    ⚠️ 原版那一句还带一个 `+0x108 != 0`（「activeEffects 列表非空」）的守卫，
            //       那是表现层的容器、对我们没有对应物 ⇒ 无条件执行，如实标着。
            t.StunnedAtStartOfTurn = false;
            // 🆕 `When an enemy receives a Stun, …`（2026-09-13 第三十四轮）。
            // ⚠️ `!t.IsStunned` 那道守卫是**行为保持**的 —— 重复眩晕也只是把关键词叠一层
            //    （`Has` 一样为真），但**广播不能重复**：卡面写的是「**收到**一次眩晕」。
            BroadcastKeywordEvent(ctx, WhenEventKind.GetsStun, t);
        }

        /// <summary>自检门：<see cref="ApplyStunTo"/>（纪律同 `FireTriggerRowsByChosenAbilityForTest`：
        /// **不改任何行为**，只让 Editor 程序集调得到。⛔ 生产侧别调）。</summary>
        public static void ApplyStunToForTest(BattleContext ctx, UnitState t, string by, EffectOp op)
        {
            ApplyStunTo(ctx, t, by, op);
        }

        /// <summary>自检门：`EffectResolver.FireTriggerByNumberWithSink`（生产侧唯一调用点 = 引擎内部）。
        /// <para>⚠️ 它内部就是那道「**正文 op 为空才按 `chosenAbility` 兜底**」的闸
        /// （`EffectResolver.cs:21078` 的 `if (ops == null || ops.Count == 0)`）—— 本门只让自检能
        /// **跑那条闸本身**（`B121-D4-03` 的「不双发」就是靠它证的），⛔ 不改任何行为。</para></summary>
        public static bool FireTriggerByNumberWithSinkForTest(BattleContext ctx, UnitState u, int triggerNumber,
                                                              int owner, int slot,
                                                              UnitState actingCard, UnitState targetCard)
        {
            return FireTriggerByNumberWithSink(ctx, u, triggerNumber, owner, slot, actingCard, targetCard, null);
        }

        // ==========================================================================================
        //  ② `B121-D1B-03` ①：`valueOptions 203`（`min + thisCard.CurrentShuriken × max`）
        // ==========================================================================================

        /// <summary>`AbilityLogic.GetValueFromCriteria` 的 **`case 0xcb (203)`**。
        ///
        /// <para>判据（`d:/2/tools/decomp_full/AbilityLogic__GetValueFromCriteria.c:320-325` 逐句现读）：
        /// <c>case 0xcb: if (param_5 == 0) goto ERR; iVar4 = EntityScript__get_CurrentShuriken(param_5);</c>
        /// <c>uVar6 = param_3 + iVar4 * param_4;</c>（`param_3` = `min` · `param_4` = `max` · `param_5` = `thisCard`）。</para>
        ///
        /// <para>🔴 **与 40/41/50 那一族不是同一个算式**：那族是 `min == 0 ? value : min × value`
        /// （`.c:110-128`），203 是 **`min + value × max`**（加法，不是乘法）。照 40/41/50 的写法会算错。</para>
        ///
        /// <para>`param_5 == 0` 在原版走 `LAB_18081b8c4`（错误出口）⇒ 我们出声 + `false`（调用方不结算）。</para>
        /// </summary>
        /// <returns>`false` = 这一档读不出来（`gap` 说清缺什么）。</returns>
        internal static bool TryResolveCriteriaValue203(BattleContext ctx, UnitState thisCard,
                                                        int min, int max, out int value, out string gap)
        {
            value = 0; gap = null;
            if (thisCard == null)
            {
                gap = "`valueOptions = 203` 要读**载体**的 `CurrentShuriken`，本刻取不到载体"
                    + "（原版 `GetValueFromCriteria.c:321` 的 `param_5 == 0` 走错误出口）";
                return false;
            }
            value = min + thisCard.KwValue("shuriken") * max;
            return true;
        }

        /// <summary>自检门：<see cref="TryResolveCriteriaValue203"/>（⛔ 生产侧别调）。</summary>
        public static bool TryResolveCriteriaValue203ForTest(BattleContext ctx, UnitState thisCard,
                                                             int min, int max, out int value, out string gap)
        {
            return TryResolveCriteriaValue203(ctx, thisCard, min, max, out value, out gap);
        }

        // ==========================================================================================
        //  ③ `B121-D1B-03` ②：`ta 230`（两位督军里**当前生命更高**的那个）
        // ==========================================================================================

        /// <summary>`AbilityLogic.GetTargets` 的 **`case 0xe6 (230)`**。
        ///
        /// <para>判据（`d:/2/tools/decomp_full/AbilityLogic__GetTargets.c:809-833` 逐句现读）：
        /// <c>if (iVar6 == 0xe6) { uVar8 = BattleManager__GetHighestHealthWarlord(manager);
        /// if (uVar8 == null) uVar8 = BattleManager__GetHero(manager, 1); … goto LAB_180818485; }</c>
        /// —— 建完池**直接返回**（`LAB_180818485`）⇒ **不再过 36 格 criteria、也不按 `maxTargets` 裁**。</para>
        ///
        /// <para>🔴 **平局这一档今天取到了直接判据**（判据原表 §5「未取到」已结案）：
        /// `BattleManager__GetHighestHealthWarlord.c` 在两边相等时**返回 `null`**（`iVar4 &lt;= iVar3 ⇒ return 0`），
        /// 调用方回落 `BattleManager__GetHero(manager, 1)`；而 `BattleManager__GetHero.c` 全文两句
        /// —— `param_2 == 0 ⇒ +0xC8`、否则 `+0xC0` —— 且 `GetTargets.c:721` / `:738` 那两档把
        /// **`actingCard.isPlayer` 原样当实参**传进去 ⇒ 实参就是**座位号**（`isPlayer` 0/1）。
        /// ⇒ `GetHero(manager, 1)` = **座位 1** 的督军，与「谁施放」无关。
        /// （`GetHighestHealthWarlord` 比的 A 侧正是 `+0xC0` = 座位 1、B 侧 `+0xC8` = 座位 0，
        /// 所以「取高者 / 平局给座位 1」两句自洽。）</para>
        ///
        /// <para>⚠️ **判「当前生命」而不是「生命上限」**：`.c` 读的是督军 `+0x68` 的 `ObscuredInt`，
        /// 与 `GetValueFromCriteria` 的 `case 0x1e (30 = targetCard 当前生命)` **同址** ⇒ 自洽，
        /// 但这一条是**由同址推出来的**，不是直接读到的字段名（如实标着）。我们这边 = `UnitState.Health`（当前生命）。</para>
        /// </summary>
        /// <returns>`null` = 判不了（`gap` 说清缺什么）；否则**恰好一个**元素（平局给座位 1）。</returns>
        internal static List<UnitState> ResolveTargetsTa230(BattleContext ctx, out string gap)
        {
            gap = null;
            if (ctx == null || ctx.Players == null || ctx.Players.Length < 2)
            {
                gap = "`ta 230` 要比两位督军的当前生命，本刻对局没有两侧玩家";
                return null;
            }
            // 原版 `+0xC0`（= `GetHero(manager,1)` 那一侧）= 座位 1；`+0xC8` = 座位 0。
            var w1 = WarlordOfSide(ctx, 1);
            var w0 = WarlordOfSide(ctx, 0);
            if (w1 == null || w0 == null)
            {
                // 原版这里 `GetHighestHealthWarlord` 会走 `FUN_1803f47a0()`（抛）⇒ 我们没有对应物，出声不结算。
                gap = "`ta 230` 要读两位督军的当前生命，而" + (w1 == null ? "座位 1" : "座位 0")
                    + " 的督军取不到 ⇒ 判不了（⛔ 不拿某一侧硬顶）";
                return null;
            }
            if (w1.Health > w0.Health) return new List<UnitState>(1) { w1 };
            if (w0.Health > w1.Health) return new List<UnitState>(1) { w0 };
            // 平局：原版 `GetHighestHealthWarlord` 返回 null ⇒ 调用方回落 `GetHero(manager, 1)` = 座位 1。
            return new List<UnitState>(1) { w1 };
        }

        /// <summary>`ta 230` 在原版 `GetTargets` 里**会不会**再走末尾那段
        /// `CheckIfMeetsCriteria`（`:1086-1095`）+ `maxTargets` 裁剪。
        /// <para>判据 = 分支出口：`case 0xe6` 的两条支路都 `goto LAB_180818485`（= 建完池直接 `return`）
        /// ⇒ **不筛不裁**。⇒ 必须把 `230` 加进 `EnterTaGoesThroughCriteriaFilter`
        /// （`EnchantmentExecutor_B115.cs:1089 TaGoesThroughCriteriaFilter` 的 `false` 那一组），
        /// 否则 `ResolveEnchantmentTargets` 会用 36 格把这一格再筛一遍 ⇒ **可能筛空**。</para>
        /// <para>本属性是**判据的可执行拷贝**：接线补丁落地后，自检可以拿它和那张表对账
        /// （⛔ 别只改一处）。</para></summary>
        public static bool Ta230SkipsCriteriaFilter { get { return true; } }

        /// <summary>自检门：<see cref="ResolveTargetsTa230"/>（⛔ 生产侧别调）。</summary>
        public static List<UnitState> ResolveTargetsTa230ForTest(BattleContext ctx, out string gap)
        {
            return ResolveTargetsTa230(ctx, out gap);
        }

        /// <summary>自检门：**直接转调** <see cref="TaGoesThroughCriteriaFilter"/>
        /// （它在 `EnchantmentExecutor_B115.cs:1089`，是同**一个** `RuleCore` 类的 private 成员
        /// ⇒ 同一程序集 / 同一类里直接调得到；⛔ **别在本文件再抄一份 switch** —— 两处写同一条规则迟早分叉）。
        /// <para>⚠️ 它现在**没有** `230`（本批那个文件由 `E2` 独占）⇒ 补丁落地前本门返回 `true`
        /// （= 会被 36 格再筛一遍）。自检那条断言就是「补丁落地后才绿」，⛔ 别把它当实现缺陷。</para></summary>
        public static bool TaGoesThroughCriteriaFilterForTest(int ta)
        {
            return TaGoesThroughCriteriaFilter(ta);
        }
    }
}
