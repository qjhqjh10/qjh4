// SimpleAI.ActiveEntry.cs — `SimpleAI` 的兄弟分册：**第 ④ 档主动能力（原版通用 `activeAbilityLogic`）
// 的 AI 候选枚举**（🆕 2026-10-27 · 执行代理 `B88_E3` · 第八十八会话波 1）
//
// 为什么单开一个文件：`SimpleAI` 已改 `public static partial class`（`Data/SimpleAI.cs:123`，1→1），
// 因为那份文件 **2,228 行、被账按 `文件:行号` 引用 41 处** —— 往中间或类尾插一块就是一片漂移；
// 拆成 partial 则 `Data/SimpleAI.cs` **零行数变化**（唯一改动 = 第 421 行 1→1 的调用）。
//
// 判据（⛔ 不要重新论证）：
//   · 原版 `AI.GetAvailableActions` 对 `activeAbility`(5) **产一条候选**；打分只读 `ActiveAbility`
//     自己的两列 criteria —— `AI.GetActionScore` case 5 先 `nonTargetScoringCriteria(+0x58)`、
//     再（`targetCard != null` 时）`cardTargetScoringCriteria(+0x50)`。
//     全文 → `资料/完全复刻/判据/AI打分与动作选择_第2轮.md` §2·1 的 `activeAbility` 支。
//   · 我们的等价打分**早就有了**：`SimpleAI.ScoreAbility`（`Data/SimpleAI.cs:721-730`）走
//     `AiScoring.ScoreActiveAbility`（`Data/AiScoring.cs:670-698`，次序与原版同形）。
//     ⇒ 缺的**只是候选产出**：枚举口原来只有 `oath`（`SimpleAI.cs:392`）/ 替代行动（`:409`/`:415`）/
//     `Ability:` 自定文法（`:422` 起）三条 ⇒ 池内唯一「五缺」的第 ④ 档卡 `SOR66 Armorium Cherub`
//     在 AI 手里**永远不会发动**。
//     基线读数（2026-10-27 本代理现跑）：`bash 工具/ruleprobe.sh ai actions SOR66` 的动作表
//     `[按 Kind 计数] attackMelee 6 · attackRanged 6 · playCard 4 · endTurn 1` —— **0 条 `activeAbility`**。
//   · 来历（两条「顺手发现」）：`资料/完全复刻/报告/A11_联机与回放路由.md` §④·2
//     （「将来 AI 若枚举第 ④ 档，打分那支会读空表」）＋ `A9_主动能力按钮.md` §3·2
//     （「AI 不会用这颗钮」）。
//
// 🔴 入门谓词 = 表现层 `BattleDriver.IsActiveAbilityEntry`（`CardPresentation/Battle/BattleDriver.cs:36148`）
//    那份判据的**第二份副本**。为什么必须复制：`BattleDriver.cs` 是表现层、本代理的**白名单之外**
//    （独占者 `E5`）。⚠️ 已记账：正确收口是 `RuleCore` 开一个 public 谓词、两边都转发
//    （「两处写同一条规则 = 迟早不一致」那一铁律）；本批**不动** `RuleCore.cs`（独占者 `E1`）。
//
// 🔴 不许静默：判不出来时（`ResolveActiveAbilityRows` 返回 null）**照旧产一条候选**，由结算侧
//    `RuleCore.UseActiveAbility` 出声 + `ErrUnimplemented` —— 与表现层「钮给不给」同一条口径
//    （枚举口不用一个猜出来的闸把卡挡死）。
using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class SimpleAI
    {
        /// <summary>第 ④ 档主动能力进 AI 动作表（原版 `activeAbility`(5) 那一支）。
        /// <para>**调用点唯一** = `Data/SimpleAI.cs:421`：那个位置是「`u.Ability == null`（`Ability:` 自定文法
        /// 那三档都不适用）」的分叉点 —— ⛔ 别在别处再调一次（会产出第二条同形动作）。</para>
        /// <para>⚠️ **载体侧的条件一条都不能少**：`HasActiveAbility == 1` ∧ 前三条等价入口在**数据上**
        /// 都不存在（`Ability:` 正文 / 替代行动关键词 / 誓约 `OathOps`）。少一条就会给 86 张
        /// 「已经有入口」的卡**多产一条动作**（`SOR66` 之外那 86 张的 `HasActiveAbility` 也是 1）。</para></summary>
        static void AddActiveAbilityEntry(BattleContext ctx, int me, int slot, List<AiAction> list)
        {
            if (ctx == null || list == null) return;
            var u = ctx.Players[me].Board[slot];
            if (u == null || u.Card == null) return;

            // ── 入门谓词：与 `BattleDriver.IsActiveAbilityEntry` 逐条同形（那条 doc 是判据正本）──
            //  ⚠️ 判别力如实标：第 1 条（`HasActiveAbility != 1`）与第 2 条（`u.HasAbility`）在**本调用点**都是冗余的
            //     —— 第 1 条 `RuleCore.CanStartActiveAbility` 也判（`ErrNoAbility`）；第 2 条被调用点
            //     （`u.Ability == null`）已经保证恒假。**保留**是为了与那条谓词逐条同形（将来调用点若换地方，
            //     这里不会静默放宽）；⛔ 别拿它们当断言判据（删掉不会有任何断言变红 —— 这是诚实的说明）。
            //     真有判别力的是第 3、4 条（替代行动 / 誓约）：现池 86 张卡同时有 `hasActiveAbility=1` 与它们之一。
            if (u.Card.HasActiveAbility != 1) return;              // `CardScript.hasActiveAbility` `+0x220`
            if (u.HasAbility) return;                              // `Ability:` 自定文法那一档
            if (RuleCore.AlternativeActionKeyword(u) != null) return;   // `duty`/`pray`/`ferocity`/`agenda`
            if (u.Card.OathOps.Count > 0) return;                  // 誓约

            // ── 这一格现在能不能发动（`BattleManager__CanUseActiveAbility` 的六道闸，⛔ 这里不重写）──
            if (RuleCore.CanStartActiveAbility(ctx, me, slot) != RuleCodes.OK) return;

            // 要选目标吗 —— **不另写一份判据**：`CanUseActiveAbility(…, -1)` 只有「要选目标」那一支回
            // `ErrTarget`（`ActiveAbilityNeedsPick` 之后 `BoardSpec.IsValid(-1)` 不成立；与表现层
            // `BattleDriver.ActiveAbilitySideOr` 同一个读法）。
            if (RuleCore.CanUseActiveAbility(ctx, me, slot, -1) == RuleCodes.ErrTarget)
            {
                // 候选侧 = **己方**：引擎那一口 `CanUseActiveAbility` 的 `targetOwner` 写死成 `p`
                // （`Core/RuleCore.cs:15341` 的 `CanUseAbilityTarget(ctx, p, u, p, targetSlot)`）
                // ⇒ 只扫自己的棋盘，⛔ 别照「普通主动技」那一支扫敌方（会一条候选也产不出来）。
                var mine = ctx.Players[me].Board;
                for (int t = 0; t < BoardSpec.Size; t++)
                {
                    if (mine[t] == null) continue;
                    if (RuleCore.CanUseActiveAbility(ctx, me, slot, t) != RuleCodes.OK) continue;
                    list.Add(new AiAction
                    {
                        Kind = AiActionKind.ActiveAbility, Slot = slot,
                        AltKeyword = AltActiveAbility, TargetP = me, TargetSlot = t,
                    });
                }
                return;
            }

            // 不用点目标（`targetsAffected` 里没有 `30 target` / `220`）⇒ 一条不带目标的候选。
            list.Add(new AiAction
            {
                Kind = AiActionKind.ActiveAbility, Slot = slot, AltKeyword = AltActiveAbility,
            });
        }
    }
}
