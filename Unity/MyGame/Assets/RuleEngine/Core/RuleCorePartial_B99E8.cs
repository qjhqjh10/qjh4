// RuleCorePartial_B99E8.cs — 第九十九会话 · 执行代理 `B99_E8` 的 `RuleCore` partial 扩展
//
// 🔴 **为什么单开一个文件**：`RuleEngine/Core/RuleCore.cs` 被账按 `文件:行号` 引用 **365+ 处**
//   （`资料/完全复刻/账/*.tsv`）⇒ 往它中间插成员会让下游落点整体下移、`check_ledger_points` 大批报漂移。
//   本文件与 `RuleCorePartial_B93F7.cs` 同形：**源文件只做「零净行数」的就地编辑**，
//   新成员一律落在本文件 —— 它没有任何账的落点。
//
// 本批（B99_E8）落在本文件的东西（账 **870 `CardScript.ResolveDeadCard`** 的**真缺口** ——
// `chosenAbility 390 = AddTriggerRequiem` 那条 430/435 的派发；判据由 `B99_E1` 回源反证后定位到下面两处）：
//   · `RunTriggerRequiemRow(ctx, p, u, row, ta, chosen)` ← **(b)** `EffectResolver.RunActiveRowOpsOrGrantedTrigger`
//     的 `ca == 390` 执行器（那一行加 `if (ca == 390) return RunTriggerRequiemRow(...)`）。
//   · `FireObserverRequiemOnDeath(...)` ← **(c)** `ResolveDeadCard` 里「**相邻观察者**看着(死者)死 → 跑自己的 430」
//     那一支（`CardScript__ResolveDeadCard.c:264-283`）；由 `CleanupDeaths` 的 `leftPlay` 支接线。
//   · `RunTriggerRequiemRowForTest` / `FireObserverRequiemOnDeathForTest` = 自检门（只转调，不改行为）。
//
// ⚠️ 判据出处写在方法头上（铁律 2/3：没有出处就写「查不到」）。

using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==================================================================
        //  (b) `chosenAbility 390 = AddTriggerRequiem` 的执行器
        // ==================================================================
        //
        // **判据链（第一权威 = `d:/2/tools/decomp_full/` 逐句现读）**：
        //   ① 生产：`AbilityLogic__PlayAbility.c:2073-2088`（`if (iVar8 == 0x186 /* 390 */)`）——
        //      逐目标 `BattleManager__AddTriggerRequiem(bm, targetUnit, casterCard, priority, 1, 0)`。
        //      `out_il2csharp/Assembly-CSharp/BattleManager.cs:17546` 的**签名**坐实了形参身份：
        //      `AddTriggerRequiem(CardScript targetCard, CardScript actingCard, int priority, bool blockPlayerActions)`
        //      ⇒ `targetCard = targetUnit`、`actingCard = casterCard`（= 用这条主动能力的**那个单位**）。
        //   ② 出队：`BattleManager__ResolveAction.c:5574`（`case 0x40 = triggerRequiem` 的**末尾**）
        //      → `BattleManager__ResolveTriggerRequiem(bm, &battleAction)`。
        //   ③ 结算：`BattleManager__ResolveTriggerRequiem.c`：
        //      · **入口闸** `:29` `if (param_2[2] /* +0x10 = targetCard */ != 0)` 才往下；
        //        `IsInPlayOrDying(targetCard)` 不满足 ⇒ 只 `FinishResolvingAction`、**什么都不做**。
        //      · `local_f8`（= `actingCard`，`BattleAction` 字段偏移 `+0x08`，见 `dump.cs:118958`）
        //        ⇒ `RawCardScript__OnTrigger(rawOf(actingCard), 0x1ae /* 430 Requiem */, bm, …)`
        //        ⇒ 紧接着 `BattleManagerSupport__BroadcastUnitRequiem(bm, actingCard)`（435）。
        //      ⇒ **跑 430/435 的是 `actingCard`（= caster）、不是 `targetCard`**；`targetCard` 只当**入口闸**。
        //
        // **我们的落点**：`u` = 用这条主动能力的那个单位（= caster = 原版 `actingCard`）；
        //   目标由本行正文的 target spec 或玩家点定的 `chosen` 给出（照 `RunGrantedTriggerRow` 同形）。
        //   430/435 两跳复用既有 `RuleCore.ResolveTriggerRequiem(owner, slot)`（**一条规则只此一处**）。
        //
        // ⚠️ **可观测面 = 0**：`cards_engine.json` v43 现扫 1,129 张 —— `chosenAbility 390` **0 张**、
        //   `abilityTriggers` 含 430 / 435 **各 0 张**、卡面 `requiem` **0 张** ⇒ 本格是**休眠件**（铁律 11 照建），
        //   `play check` 指纹预期**不变**。数据一出现即自动生效。
        /// <summary>见文件头 (b) 段：`chosenAbility 390` 的 `ca` 分派执行器。</summary>
        /// <returns>1 = 已尝试触发（入口闸通过）；0 = 闸没过 / 解不出目标（原版那一支什么都不做）。</returns>
        internal static int RunTriggerRequiemRow(BattleContext ctx, int p, UnitState u,
                                                 int row, int ta, UnitState chosen)
        {
            if (ctx == null || u == null || (p != 0 && p != 1)) return 0;
            // 目标：本行正文的 target spec 优先，否则用玩家点定的 chosen（照 RunGrantedTriggerRow 的同形解析）。
            var ops = ActiveRowOpsOf(u, row);
            var spec = (ops != null && ops.Count > 0) ? ops[0].Target : null;
            UnitState target = chosen;
            if (spec != null)
            {
                var ts = ResolveTargets(ctx, p, spec, u, chosen, null, ops[0]);
                target = (ts != null && ts.Count > 0) ? ts[0] : null;
            }
            // 入口闸（原版 `ResolveTriggerRequiem.c:29`）：`targetCard` 必须存在且 `IsInPlayOrDying`
            // —— 不满足就**什么都不做**（⛔ 不是「照样跑一次无目标的 430」）。
            if (target == null || !IsUnitInPlayOrDying(ctx, target))
            {
                ctx.Log($"{u.Name} 的主动能力 `chosenAbility 390`（AddTriggerRequiem）："
                      + "目标不在场/将死 ⇒ 本支不结算（原版 `ResolveTriggerRequiem.c:29` 的入口闸）");
                return 0;
            }
            if (!FindSlot(ctx, u, out int owner, out int slot)) return 0;
            // 430（自己那条正文，走 `FireTriggerAt`：无正文不凭空发 `EvtKind.Trigger`）+ 435（别人）。
            return ResolveTriggerRequiem(ctx, owner, slot) ? 1 : 0;
        }

        /// <summary>自检门：只转调 <see cref="RunTriggerRequiemRow"/>（**不改任何行为**）。⛔ 产品路径别调。</summary>
        public static int RunTriggerRequiemRowForTest(BattleContext ctx, int p, UnitState u,
                                                      int row, int ta, UnitState chosen)
        {
            return RunTriggerRequiemRow(ctx, p, u, row, ta, chosen);
        }

        // ==================================================================
        //  (c) 观察者侧 430 —— 「相邻的观察者看着(死者)死 → 跑它自己的 430 + 广播 435」
        // ==================================================================
        //
        // **判据（第一权威 · `d:/2/tools/decomp_full/CardScript__ResolveDeadCard.c:242-283` 逐句现读）**：
        //   · `:242` `if (op_Equality(local_308 /*死者*/, param_1 /*观察者*/) == 0)` —— **观察者不是死者**。
        //   · `:264-266` `lVar19 = GetAdjacentUnits(bm, param_1 /*观察者*/)`、`Contains(lVar19, local_308)`
        //     ⇒ **死者是观察者的相邻单位**（`BattleManager__GetAdjacentUnits.c` → `MinionManager__GetAdjacentUnits.c`
        //     按**同侧列表**的 `i±1`，**最内侧小兵的邻位是督军** —— `GetAdjacentUnits.c` 的 `iVar7 == 0`
        //     支取 `uVar4 = GetHero(...)`）。
        //   · `:266` `*(int*)(param_1 + 0x228) != 5` —— **观察者本身不处于「将死」态**。
        //   · `:270` `get_cardType(local_308) != 10` —— **死者不是督军**（`CardTypeOptions.Hero = 10`）。
        //   · `:277-279` `RawCardScript__OnTrigger(rawOf(观察者), 0x1ae /* 430 */, bm,
        //        card=观察者, actingCard=uStack_210, targetCard=local_308 /*死者*/, 0)`。
        //      🔴 **`uStack_210` 的身份（本代理回源核实 = `B99_E1` 没查清的那一格）**：
        //        `uStack_210` 是 `BattleAction` 的 `+0x08 = actingCard`（字段偏移见 `dump.cs:118958`），
        //        **= 凶手 / 死亡来源**。两条独立证据：
        //        ① 同一函数 `:236` `TriggerOnMinionDeath(param_1, uStack_210, local_308, local_28, …)`
        //           对照 `il2cpp_out/dump.cs:23526` 的签名
        //           `OnMinionDeath(manager, thisCard, **actingCard**, deadCard, deathType, skipBacklash)`
        //           ⇒ `uStack_210` = `actingCard`、`local_308` = `deadCard`；
        //        ② 同一函数 `:248` 的 Slay 闸 `op_Equality(uStack_210, param_1)`（凶手 == 观察者才响 Slay）
        //           只有「`uStack_210` = 凶手」才自洽。⇒ **`FireTriggerAt(观察者, Requiem, …, subject: 凶手)`**。
        //        ⚠️ 凶手可能为 null（毒 / 反噬 / 反击 / 回合开始清场这些没有来源的死）——
        //           我们的 `FireTriggerAt` 把 `subject == null` 当「就是 `u` 自己」（那条口的口径），
        //           与原版「actingCard 传 null」在 ability 级 `actingCardCriteria` 那道闸上**可能有偏差**；
        //           池内 430 正文 0 张 ⇒ 今天零可观测（如实标着，⛔ 不猜）。
        //   · `:281` `BattleManagerSupport__BroadcastUnitRequiem(bm, param_1 /*观察者*/, 0, 0)`
        //     ⇒ **广播「这个观察者用了安魂」**（435，接收方体内自排除，见 `EffectResolver.BroadcastUnitRequiem`）。
        //
        // **邻接的格号换算（🔴 关键：`CleanupDeaths` 调本方法前**已经**跑过 `BoardSlots.RemoveAt(ps, slot)`）**：
        //   棋盘是「连续无洞」模型（见 `BoardSlots.cs` 文件头）⇒ 摘掉死者之后**外侧单位整体内移一格**。
        //   死者在 `slot`（某侧第 `index` 格），它原来的两个邻里有：
        //     · **朝督军那一侧**（`slot + warlordDir`）—— 序号 < index、**不移动** ⇒ 仍在 `slot + warlordDir`；
        //     · **外侧那一个**（原 `slot - warlordDir`，序号 index+1）—— 内移进 `slot`。
        //     ⇒ 摘除后的「死者原邻里」在 `{ slot, slot + warlordDir }` 两格里（`warlordDir`：
        //       左侧 `Left(-1)` 时**朝督军是 +1**、右侧 `Right(+1)` 时**朝督军是 -1**）。
        //     ⚠️ 若棋盘当时非连续（`BoardSlots.WarnHoley` 的降级路径，摘除只置空不补位）⇒ 这条换算不成立；
        //        生产路径恒连续（有告警兜底），如实标着。
        //
        // **可观测面 = 0**（同 (b)）：池内 430/435 正文各 0 张 ⇒ 本方法今天**只做「判定 + 调用」却不产生任何
        //   规则副作用**（`FireTriggerAt` 在「无正文」时**提前返回、连 `EvtKind.Trigger` 都不发** —— 这正是
        //   `B99_E1` 那条红线「⛔ 别用 `FireTriggerAlways`」的落点）。`play check` 指纹预期**不变**。
        /// <summary>见文件头 (c) 段：死者相邻观察者各跑一次 430 + 广播 435。</summary>
        /// <param name="probe">自检用：把**实际被点到的观察者**按处理顺序收进来（产品路径传 `null`）。</param>
        /// <returns>实际派发的观察者数（无正文也计数；自检用它验邻接选人）。</returns>
        internal static int FireObserverRequiemOnDeath(BattleContext ctx, int p, int slot,
                                                       UnitState dead, UnitState killer,
                                                       List<UnitState> probe = null)
        {
            if (ctx == null || dead == null || (p != 0 && p != 1)) return 0;
            // `:270` 死者 `cardType != 10`（督军）：督军死走 `ResolveDeadCard.c:24-56` 的另一支，⛔ 不在这。
            if (dead.IsWarlord) return 0;
            int side = BoardSlots.SideOf(slot);
            if (side == 0) return 0;                       // 督军格（`slot == 4`）走另一支
            var board = ctx.Players[p].Board;
            if (board == null) return 0;
            int warlordDir = side == BoardSlots.Left ? 1 : -1;   // 朝督军那一侧的格号方向
            // 摘除后的「死者原邻里」= `{ slot, slot + warlordDir }`（见文件头换算）。
            int a = slot, b = slot + warlordDir;
            int fired = 0;                                    // 真正结算了 430 正文的观察者数（无正文不计 —— 不加噪声日志）
            for (int i = 0; i < 2; i++)
            {
                int s = (i == 0) ? a : b;
                if (i == 1 && b == a) continue;
                if (!BoardSpec.IsValid(s)) continue;
                var obs = board[s];
                if (obs == null || ReferenceEquals(obs, dead) || obs.Card == null) continue;
                if (!obs.IsAlive) continue;               // 原版 `:266` 观察者 cardState != 5（将死）
                if (probe != null) probe.Add(obs);        // 自检：记录「被选中要跑 430」的观察者（与有无正文无关）
                // 430（观察者自己那条正文；走 `FireTriggerAt` —— 无正文提前 return、不发事件、不改指纹）
                if (FireTriggerAt(ctx, obs, KeywordTable.Requiem, p, s, chosen: dead, subject: killer)) fired++;
                // 435（「这个观察者用了安魂」；原版无条件广播，接收方体内自排除；无 435 监听者则空转）
                BroadcastUnitRequiem(ctx, p, obs);
            }
            if (fired > 0)
                ctx.Log($"{dead.Name} 阵亡 ⇒ 相邻的 {fired} 个观察者各跑一次安魂(430)"
                      + "（原版 `ResolveDeadCard.c:264-283`：`GetAdjacentUnits(观察者)` 含死者 ∧ 死者非督军"
                      + " ∧ 观察者非将死 ⇒ 430 + `BroadcastUnitRequiem`）");
            return fired;
        }

        /// <summary>自检门：只转调 <see cref="FireObserverRequiemOnDeath"/>（**不改任何行为**）。⛔ 产品路径别调。</summary>
        public static int FireObserverRequiemOnDeathForTest(BattleContext ctx, int p, int slot,
                                                            UnitState dead, UnitState killer,
                                                            List<UnitState> probe)
        {
            return FireObserverRequiemOnDeath(ctx, p, slot, dead, killer, probe);
        }
    }
}
