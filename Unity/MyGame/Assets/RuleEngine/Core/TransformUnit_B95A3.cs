// TransformUnit_B95A3.cs — 第九十五会话 · 执行代理 `B95_A3`
//
// 本批补的是 `ResolveTransformUnit` 第 ⑥ 跳里**被整段略过的那一次调用**（`B94_B12` 只写了调用点之前的部分）：
//   `d:/2/tools/decomp_full/BattleManager__ResolveTransformUnit.c:1160` + `:1204-1208`（现读，逐行）：
//     `:1160` `uVar2 = *(undefined4 *)(local_108 + 0x44);`            // ← 第 3 实参 unitInPlayCounter
//     `:1204` `if (local_208 == 0) goto LAB_1809820aa;`              // ← 目标卡为 null ⇒ 整段跳过
//     `:1205` `in_stack_fffffffffffffcd8 = 0;`                       // ← 第 7 实参（那个 `0`）
//     `:1206` `uVar23 = CONCAT71((int7)(uVar23 >> 8), local_2c);`    // ← 第 5 实参 keepStatus（= keepEffects）
//     `:1207` `uVar15 = CardScript__MinionPlayedIntoField(local_208, 0 /*delay*/,
//                                uVar2 /*unitInPlayCounter*/, 0 /*doTransformAnim*/,
//                                uVar23 /*keepStatus*/, 1 /*shouldTriggerSummon*/, 0);`
//     `:1208` `UnityEngine_MonoBehaviour__StartCoroutine(param_1, uVar15, 0);`
//
// 🔴 为什么单开一个文件：`TransformUnit_B94B12.cs` 已被账按 `文件:行号` 引用
//   （`战斗逻辑驱动.tsv` 第 172/548 行：`:178` / `:301` / `:330` / `:363` / `:388`）——
//   往它中间插成员会让下游落点整体下移（`check_ledger_points` 立刻报漂移，见 CLAUDE.md 18·b）。
//   本文件是**纯追加**；那边只把 `:275` 那一行**就地换成**调用本方法的壳（⛔ 行数零变化）。
//
// ════════════════════════════════════════════════════════════════════════════════════════════
//  一、这个协程逐状态做了什么（判据 = `CardScript._MinionPlayedIntoField_d__314__MoveNext.c` 256 行，本笔现读）
// ════════════════════════════════════════════════════════════════════════════════════════════
//   字段序（`CardScript.cs:523-560` 的 `_003CMinionPlayedIntoField_003Ed__314`，`UnitState.SummonSickness`
//   的 doc 也引过同一份；工厂 `CardScript__MinionPlayedIntoField.c:16-22` 六条 store 逐条印证）：
//     `doTransformAnim +0x20` · `<>4__this +0x28`(= 那张卡) · `keepStatus +0x30` ·
//     `unitInPlayCounter +0x34` · `delay +0x38` · `shouldTriggerSummon +0x3c` · `minionManager +0x40`
//   ⚠️ 工厂只有**六**条 store（`:16`/`:18`/`:19`/`:20`/`:21`/`:22`）⇒ **第 7 实参 `param_7` 从头到尾没被存**
//      （那个 `0` 是调用约定的填充位，不是状态）。
//
//   **state 0**（`.c:33-83`）—— 只有两条是状态：
//     `:37`    `if (!doTransformAnim) LandOnGround(card)`                    ← 表现层（落地姿态）
//     `:39`    `card.ClearPendingDamage()`                                  ← 状态层，**我们没有那个缓冲**（见 §三·①）
//     `:42-52` `card.playedIntoFieldTurnCounter(+0x2E0) = BattleManager.turnCounter(+0x3F8)`
//              ← 🟢 **状态层，本批补的就是这一格**（等价物 = `UnitState.DeployedTurn` ⇐ `ctx.Turn`）
//     `:69-71` `if (!keepStatus) card.summonSickness(+0x58) = 1`             ← 状态层（已有唯一写点，见 §三·②）
//     `:72`    `card.unitInPlayCounter(+0x44) = 形参 +0x34`                  ← 状态层，**来源读不出**（见 §三·③）
//     `:73-75` `WaitForSeconds(delay +0x38)`                                ← 表现层
//   **state 1**（`.c:84-134`）：`SetUnitInPlayLayer` / 存 `localPosition` / `GetUnitSizeInPlay`→`DefaultScale`
//     / `MinionManager.HideCardShadow` / `WaitForSeconds(card+0x154)` —— **摆放与尺寸，全是表现层**。
//   **state 2**（`.c:135-154`）：`MinionManager.GetOwnPositionInBoard` → `UpdateMinionInPlayPosition`
//     / `WaitForSeconds(card+0x150)` —— **表现层**。
//   **state 3**（`.c:185-250`）—— 🔴 **`shouldTriggerSummon` 在整个 build 里只在这里被读**
//     （`:188` 与 `:194` 两处，都是 `cVar8 = *(char *)(param_1 + 0x3c)`）：
//       `if (shouldTriggerSummon != 0  &&  !HasCurrentTrait(5 /*ephemeral*/)  &&  !HasCurrentTrait(0x4b0 /*ambush*/))`
//         `{ 调 card+0x138 那个委托（`:207-210`）; Load 卡上的 AssetReference(card+0x28+0xb0) 并在卡位
//           `Instantiate` 一份（`:211-249`，`:245` 是那个 `Object.Instantiate`）}`
//       否则跳过。
//     🔴 **两支都 `goto LAB_1806125e0`（`:198` / `:215` / `:223` / `:246`）⇒ 都进 state 4** ⇒
//       **`shouldTriggerSummon` 只 gate「落地那一瞬的特效/委托」，不 gate 任何状态**。
//       （两条排除条件也印证这一点：`ephemeral` 与 `ambush` 都是「这类单位不放落地特效」。）
//   **state 4**（`.c:155-181`）—— 状态：
//     `:165`   `card.cardState(+0x228) = 2 /*InPlay*/`                        ← 我们**没有 `cardState` 这个字段**
//     `:179`   `ActivateTraitsOnSummonOrEnchantment(card, 1, 1, 0)`           ← 状态（`fast`/`flank`/`ferocity`/`oath` 那两个位）
//     `:180`   `UpdateSummonSicknessAnims(card)`（体内 `:18-20` 转调 `ActivateMinion`，而它 `:39-42` 才是
//              那两个位的写点）← 状态（`canAct`/`canAttack` 重算；`:10-17` 只是一对动画开关 = 表现层）
//     `:166-176` 关掉 `card+0x290`（`BattleCardUI`）下面那颗子物件 —— 表现层。
//
//   ⇒ **状态层合计四处**：落地回合号（🟢 本批补）· 召唤病（已有）· 两个 `canAct`/`canAttack` 位（已有）·
//     `cardState = InPlay`（我们没有这个字段）。**没有第五处**。
//
// ════════════════════════════════════════════════════════════════════════════════════════════
//  二、🔴 「变身要补召唤广播（Rally / 信仰 / 起义 / 旁观 190）」—— **判据不成立，本方法一行都不发**
// ════════════════════════════════════════════════════════════════════════════════════════════
//   判据 = **`ResolveTransformUnit` 的完整调用清单**（本笔 2026-10-29 现跑：
//     `grep -oE "\b(BattleManager|BattleManagerSupport|CardScript|PlayerHand|MinionManager|EntityScript)__[A-Za-z0-9_]+"
//      BattleManager__ResolveTransformUnit.c | sort | uniq -c`）：
//     里面**没有** `BattleManagerSupport__BroadcastUnitSummoned`、**没有** `CardScript__ResolveUnitSummoned`
//     （`Broadcast*` 只有 `BroadcastRemoveFromGame` 与 `BroadcastWhileInPlay` 两条 —— 都已接线）。
//   真正会发那两条函数的是**另外两个入口**，它们是**显式**调的：
//     · `BattleManager._ResolveCardAutoSummoned_d__511__MoveNext.c`（`BattleManagerSupport__BroadcastUnitSummoned`
//       + `MinionPlayedIntoField(…, 1)`）—— 从手牌自动上场那一路；
//     · `BattleManager._ResolveSummonUnit_d__510__MoveNext.c`（同上 + `MinionPlayedIntoField(…, 0)`）—— 效果召唤那一路。
//   ⇒ **变身不走召唤触发器**：`ThisUnitSummoned(195)` / `OtherUnitSummoned(190)` / `UseFaith(605)` /
//     `Uprising(680)` **一个都不发**，因为原版那条链压根没调 `ResolveUnitSummoned`。
//     ⛔ 别「顺手补」—— 那会造出原版没有的触发（铁律 11 例外① 的反面：这是原版**本身就没有**的）。
//
//   🔴 **但本批补的那一格有下游消费者，别当装饰**：落地回合号被写成**当前回合**之后，**同一回合内**
//     别的单位再落地时，`Uprising(680)` 那条「本回合上场的监听者」扫描**会把这次变身的单位算进去** ——
//     判据 = 原版 `CardScript__ResolveUnitSummoned.c:82-83`（`iVar4 = 被召唤者.playedIntoFieldTurnCounter(+0x2E0)`、
//     `iVar5 = BattleManager.turnCounter(+0x3F8)`，`iVar4 == iVar5` 才继续），我们的等价物 =
//     `RuleCore.FireTriggerOnSide(..., requireDeployedThisTurn: true)`（`RuleCore.cs:13436` 读 `t.DeployedTurn != ctx.Turn`）。
//     另外两个读者也会跟着变（都**照原版**）：誓约闸 `RuleCore.cs:11752`（`DeployedTurn != ctx.Turn ⇒ ErrExhausted`）、
//     `RuleCore.cs:16011` 的 `IsTheSameTurnPlayed` 等价物。
//
// ════════════════════════════════════════════════════════════════════════════════════════════
//  三、如实标着的三格（⛔ 不许拿猜测填）
// ════════════════════════════════════════════════════════════════════════════════════════════
//   ① `ClearPendingDamage`（`.c:39`）—— **我们没有那个缓冲**：本仓伤害**逐段现结**，
//      判据与结论已写在 `RuleCore.cs:16679`（「②③ 的 `ClearPendingDamage` 我们**没有那个缓冲**」）。
//      原版那是一个「待结算伤害数组」（`card+0xf8`：`count +0x18` / 数组 `+0x10`，`CardScript__ClearPendingDamage.c:15-35`
//      现读 = 记一条 `Log` + 把 count 归零 + `Array.Clear`）⇒ **结构上没有对应物 ⇒ 无代码可写**（不是"没做"）。
//   ② 召唤病 + 两个 `canAct`/`canAttack` 位（`.c:69-71` / `:179` / `:180`）—— 本仓的**唯一写点**是
//      `RuleCore.ApplyDeployTurnState(u, keepStatus)`（它的 doc 逐句说明自己 = 那三条的和）。
//      ⇒ 本方法**转调它、⛔ 不另写第二遍**（「两处写同一条规则 = 迟早不一致」）。
//   ③ `unitInPlayCounter`（`.c:72`）—— 形参来源是 `*(undefined4 *)(local_108 + 0x44)`（`:1160`），
//      而 `local_108` 是 `ResolveTransformUnit` 里**六个无赋值点局部量**之一（`local_90`/`local_108`/`local_308`/
//      `local_a8`/`local_2c`/`local_22c`，Ghidra 丢句、ILSpy 整段 `NoteCpp2ILIssue` 噪声；见
//      `TransformUnit_B94B12.cs` 文件头 §三）。⇒ **这一格读不出**，本批**不动它**：
//      我们的 `UnitState.PlayOrder` 继续**沿用旧单位的号**（= `CarryTurnState` 已搬），
//      这与「原版取回同一个对象的 `+0x44` 再写回去」在 `local_108 == 目标卡` 时**完全等价**，
//      而「`local_108` 是别的卡」这一档无法证伪 ⇒ 如实标着，⛔ 别凭猜改 `PlayOrder`。
//
// ════════════════════════════════════════════════════════════════════════════════════════════
//  四、不搬的（表现层 · 本仓没有对应层）
// ════════════════════════════════════════════════════════════════════════════════════════════
//   `LandOnGround` / `SetUnitInPlayLayer` / `DefaultScale` / `HideCardShadow` / `UpdateMinionInPlayPosition`
//   / `WaitForSeconds`（四个）/ 落地特效与那个 `card+0x138` 委托 / 关 `BattleCardUI` 子物件
//   ⇒ 全在**表现层**。本仓 Core 没有动画时钟、也没有 `BattleCardUI`；对应的播放器在
//   `CardPresentation` 那一侧（`DeploySequence`，判据见 `调度台日志` 的 `804 MinionPlayedIntoField` 那条）。
//   ⛔ 别把「等 0.65 秒」之类搬进 `Core/`。
//
//   `cardState(+0x228) = 2`：本仓没有 `cardState` 字段 —— 「在场」由「`PlayerState.Board[slot]` 上有这个实例」
//   表达（`IsInPlay` 那一族谓词就按棋盘判）。如实标着，⛔ 别为此新造一个状态机字段。

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==================================================================
        //  原版 `CardScript.MinionPlayedIntoField` 的【状态层】等价物
        // ==================================================================

        /// <summary>
        /// **一张卡「落到场上」那一刻的钩子** —— 原版 `CardScript.MinionPlayedIntoField` 协程的**状态层**
        /// 等价物（判据逐状态见本文件头 §一；⛔ 别在别处另写第二份）。
        ///
        /// <para>**本方法只做三件**（其余见文件头 §一/§三/§四）：
        /// ① 写**落地回合号** `u.DeployedTurn = ctx.Turn`（原版 `.c:42-52`
        ///    `card.playedIntoFieldTurnCounter(+0x2E0) = BattleManager.turnCounter(+0x3F8)`，**两支都写**）；
        /// ② 召唤病 + 两个 `canAct`/`canAttack` 位 —— 转调**唯一写点**
        ///    <see cref="ApplyDeployTurnState"/>(<c>keepStatus</c>)，它 = 原版 `.c:69-71`（`+0x58`）
        ///    + `.c:179`（`ActivateTraitsOnSummonOrEnchantment`）+ `.c:180`（`UpdateSummonSicknessAnims`→`ActivateMinion`）
        ///    三条的和；
        /// ③ 出声：原版 `ClearPendingDamage` / `cardState = InPlay` / `unitInPlayCounter` 三格在本仓没有对应物
        ///    （§三），**不猜、不伪造**。</para>
        ///
        /// <para>🔴 **本方法【不发】任何召唤广播**（Rally / 信仰 / 起义 / 旁观 190 / 195 / 605 / 680）——
        /// 原版 `ResolveTransformUnit` 的调用清单里没有 `BroadcastUnitSummoned` / `ResolveUnitSummoned`
        /// （判据与反证见文件头 §二）。⛔ 别把 `DeployFree` 那条链的广播搬过来。</para>
        ///
        /// <para>**调用点**：`RuleCore.TransformUnit` 的 ⑦（原版 `:1207` 那一跳的唯一位置）。
        /// ⚠️ 别把它接到别的部署入口上 —— `PlayCard` / `DeployFree` / `PlaceTroops` 各自已经写过
        /// `DeployedTurn` 与 `ApplyDeployTurnState`（同一个判据的第二次求值不算错，但会重复搬状态）。</para>
        /// </summary>
        /// <param name="ctx">对局上下文（读 `ctx.Turn` = 原版 `BattleManager.turnCounter +0x3F8`）。</param>
        /// <param name="u">刚落地的那个单位（原版那个 `CardScript`）。</param>
        /// <param name="keepStatus">原版第 5 实参 `keepStatus`（= `BattleAction.keepEffects`，
        /// `_MinionPlayedIntoField…MoveNext.c:69` 那个 `if (!keepStatus)`）。
        /// `true` ⇒ **不写**召唤病；`false` ⇒ 写。⚠️ 与「撤不撤队列」「撤不撤自身效果」是**同一个位**
        /// （见 `TransformUnit` 的 doc），⛔ 别拆成三个形参。</param>
        public static void MinionPlayedIntoField(BattleContext ctx, UnitState u, bool keepStatus)
        {
            if (ctx == null || u == null) return;

            // ---- ① 原版 `.c:42-52`：落地回合号 = 当前回合（**两支都写**，与 keepStatus 无关）----
            //   `card.playedIntoFieldTurnCounter(+0x2E0) = BattleManager.turnCounter(+0x3F8)`
            //   ⇒ 等价物 = `UnitState.DeployedTurn = ctx.Turn`。
            //   ⚠️ 这一句会**覆盖** `CarryTurnState` 从那一个旧单位搬来的 `DeployedTurn`
            //      （`TransformUnit_B94B12.cs` 的 `to.DeployedTurn = from.DeployedTurn`）——
            //      原版就是无条件覆盖（`.c:42-52` 在 state 0 的最前面，两支都过），所以这里**必须**写在后面。
            u.DeployedTurn = ctx.Turn;

            // ---- ② 原版 `.c:69-71` + `:179` + `:180`：召唤病 + 两个 canAct/canAttack 位 ----
            //   转调唯一写点（⛔ 别在这里再写一遍 `SummonSickness = true` / `Exhausted = …`）。
            ApplyDeployTurnState(u, keepStatus);

            // ---- ③ 原版 `.c:39` / `:165` / `:72`：本仓无对应物（如实标，⛔ 不猜）----
            //   · `ClearPendingDamage`：我们的伤害**逐段现结**、没有待结算缓冲（`RuleCore.cs:16679`）；
            //   · `cardState = 2`：本仓没有 `cardState`，「在场」= `PlayerState.Board[slot]` 上有这个实例；
            //   · `unitInPlayCounter(+0x44)`：形参来源 `local_108` 是**无赋值点**局部量（文件头 §三·③）⇒ 读不出；
            //     我们继续沿用旧的 `PlayOrder`（= `CarryTurnState` 已搬），⛔ 别凭猜改。
        }
    }
}
