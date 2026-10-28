// RuleCorePartial_B93F7.cs — 第九十三会话 · 文件所有者代理 `B93_F7` 的 `RuleCore` partial 扩展
//
// 🔴 **为什么单开一个文件**：`RuleEngine/Core/RuleCore.cs` 被账按 `文件:行号` 引用 **365+ 处**
//   （`资料/完全复刻/账/*.tsv`）⇒ 往它中间插成员会让下游落点整体下移、`check_ledger_points` 大批报漂移。
//   本批的做法与 `RuleCorePartial_B88E14.cs` 一致：**源文件只做「零净行数」的就地编辑**，
//   新成员一律落在本文件 —— 它没有任何账的落点。
//
// 本批（B93-F7）落在本文件的东西：
//   · `TriggerSacrificeOnDeath(ctx, dead, owner, slot)` ← 账 **559 / 634**
//     （原版 `CardScript__ResolveDeadCard.c` 末尾那一支 `HasCurrentTrait(sacrifice)` → `TriggerSacrifice`）
//     🔴 **2026-10-28 `B93_FX1`：口已建、当前【没有正确调用点】** —— `B93_F7` 原本把它挂在
//     `RuleCore.CleanupDeaths` 的 `leftPlay`（真死）支上，那一路对原版那一支**不可达**（判据见下面
//     方法 doc 里的「可达性」那一段）⇒ 调用已撤回，方法本体保留待将来用正确输入接上。
//   · 下面那段长注释 = 账 **634 `BattleManagerSupport.BroadcastDeadUnit`** 的**逐跳核对表**
//     （六跳全部落到我们哪一处代码、哪一条是结构差异），给下一个接手的人省一遍重读。
//   · 同一段里还订正了一条**过期口径**：账 876 交接单写的「池内 106 张卡带 trigger 605」
//     —— 现读 **26 张**，且**两条入口早就接好了**（不是缺口）。证据写在那个小节里。
//
// ⚠️ 判据出处写在方法头上（铁律 2/3：没有出处就写「查不到」）。

namespace RuleEngine
{
    public static partial class RuleCore
    {
        // ==================================================================
        //  账 634 `BattleManagerSupport.BroadcastDeadUnit(manager, battleAction)` —— 逐跳核对
        // ==================================================================
        //
        // **第一权威**（本笔逐行现读）：
        //   · `d:/2/tools/decomp_full/BattleManagerSupport__BroadcastDeadUnit.c`（382 行；其中 250 行是
        //     `BattleAction` 256 字节**结构体拷贝**的伪码，真正的逻辑只有下面六跳）
        //   · 接收端 = `decomp_full/CardScript__ResolveDeadCard.c`（326 行）
        //   · 死因三族 = `decomp_full/RawCardScript__OnMinionDeath.c`（40 行，🥇 干净）
        //   · `BattleAction` 字段偏移 = `d:/2/tools/il2cpp_out/dump.cs:118958`（struct，256 字节）
        //     ⇒ `+0x8 actingCard` · `+0x10 targetCard` · `+0xF0 deathType` · `+0xF5 preventActionResolution`
        //
        // **原版六跳**（`unitsInPlay` = `manager + 0x470`，由 `ILSpy BattleManagerSupport.cs:501` 直读坐实）：
        //   ① 场上（**带 `CardScript.IsInPlay()` 闸**）每一张 → `ResolveDeadCard`
        //   ② 当前回合方手牌 → `ResolveDeadCard`
        //   ③ 另一方手牌 → `ResolveDeadCard`
        //   ④ `action.actingCard`（**非 null ∧ ≠ targetCard ∧ 不在场**）→ `ResolveDeadCard`
        //   —— `ResolveDeadCard` 体内再分五支（`:24-56` 督军死 / `:231-236` `TriggerOnMinionDeath` /
        //   `:242-244` `RemoveExtrinsicEffectsFrom(观察者, 死者)` / `:245-259` 邻接 → `Requiem(430)` /
        //   `:266-278` `targetCard == this` ∧ `HasCurrentTrait(sacrifice=0x1d6)` → `TriggerSacrifice`）
        //
        // **我们这边落到哪**（每一跳都要能指名）：
        //   · 总入口 = `RuleCore.CleanupDeaths`（原版那条广播**唯一**的等价物；我们没有独立的
        //     `BroadcastDeadUnit` 函数，形状是「每个死者一次、就地广播」= **结构性差异，保持**）
        //   · `TriggerOnMinionDeath` 那一趟 = 上面 `RawCardScript__OnMinionDeath.c` 的三段：
        //       死者自己 → `Backlash(80)` + `Unstable(81)`（`RuleCore.cs:10017` / `:10029`）
        //       观察者   → `OtherUnitDeath(90)`（`RuleCore.cs:10055` 的 `BroadcastWhen(WhenEventKind.Die, …)`，
        //                  它**带手牌两跳**：`EffectResolver.BroadcastHandWhen` / `BroadcastCostWhen`）
        //       死因族   → `175/180/170`（`RuleCore.cs:10076-10088` → `EffectResolver.EmitDeath*Triggers`）
        //   · `RemoveExtrinsicEffectsFrom` = `SweepExtrinsicEffectsOnSourceDeath`（`RuleCore.cs:10109`；
        //     手牌那半走惰性判据 `HandEffectExpired`，见该方法的 doc —— 与原版两条代码路一一对应）
        //   · `Slay(120)` = `TryCreditKill`（`RuleCore.cs:6195`，六道闸逐条对上 `ResolveDeadCard.c:245-259`）
        //   · `Requiem(430)` = `ResolveTriggerRequiem`（`RuleCore.cs:14285`）—— **但那一支是「自己用安魂」**，
        //     原版这里还有一条「**邻接者**看着它死 → 跑自己的 430」；池内 `trigger == 430` 的卡 **0 张**
        //     ⇒ **休眠**，本笔如实记着、不造新行为（见报告 §没查清）
        //   · `TargetSacrifice`（`sacrifice` trait 那一支）= **本文件** `TriggerSacrificeOnDeath`
        //     （改前**整条没有**：我们只有 `CleanupDeaths` 里「幸存者救回来」那一支，见 `RuleCore.cs:9576`）
        //
        // ⚠️ **哪几条今天不可观测**（如实记，别当成「已验收」）：
        //   · 池内带 `trait sacrifice(470)` 的卡 **0 张**、用 `chosenAbility 375 (TriggerSacrifice)` 的 **0 张**
        //     ⇒ 本文件这个新成员**生产不可观测**，自检用合成夹具（`AddKeyword`）锁实现性质。
        //   · 池内 `trigger 430/435/175/170` 各 **0 张**；`90` **31 张**、`120` **22 张**、`180` **4 张**。
        //
        // ⚠️ **可达性存疑那一格（如实标，不猜）**：原版把这一支放在 `ResolveDeadCard` 里，
        //   而那张卡要进得了①的循环得先过 `IsInPlay()`（= `cardState ∈ {2,15,3,17}`，见
        //   `CardScript__IsInPlay.c`）—— 而濒死态是 `5`。⇒ **「真死那一刻它还在不在那张场上列表里」
        //   本笔没坐实**。我们的落点选在 `leftPlay`（= **确实离场**）那一支上，闸照抄
        //   `HasCurrentTrait(sacrifice) ∧ IsPlayerTurn(bm) == card.isPlayer`（与幸存者那一支**同一套闸**，
        //   判据 `CheckIfDead.c:152-158`）。**它休眠 ⇒ 口径差异今天不可观测**；哪天池内出现带该 trait 的卡，
        //   要先回读 `ResolveDeadCard.c:266-278` 与 `_ResolveBacklash_d__451__MoveNext.c:144` 的交待。
        //
        // ⚠️ **顺手订正的过期口径（铁律 5）** —— 账 **876 / trigger 605 `UseFaith`**：
        //   交接单写「`CardScript__ResolveUnitSummoned.c:38` 的 605 是全批最大的潜在缺口（池内 **106** 张）」。
        //   **两句都不成立**（本笔现读）：
        //     ① 池内带 `trigger 605` 的卡 = **26 张**（`cards_truth.json` 逐卡 walk；
        //        `cards_engine.json` 的 `abilityTriggers` 交叉核也是 26；且 26/26 都在池内、
        //        26/26 的 `desc` 都含 `☀` ⇒ `CardDef.FaithOps` 非空 ⇒ 描述解析这条路全覆盖）。
        //        「106」出处已不可考（疑似把 `cards_raw.json` 的 `full_raw` 双份或 `animTimes` 行一起数了）。
        //     ② **两条召唤入口早就接好了**：`PlayCard`（`RuleCore.cs:4213`）与
        //        `DeployFreeCore`（`RuleCore.cs:4950`），且次序 = 195(Rally) → 605，与
        //        `ResolveUnitSummoned.c:44-51` 一致；`reanimated` 那一档两边都挡掉（判据同文件 :7-31）。
        //    ⇒ 605 **不是缺口**，账 876 保持 B。

        /// <summary>
        /// **死者自己带 `sacrifice`(trait 470) 时的「献祭」那一跳** —— 原版
        /// `CardScript__ResolveDeadCard.c:266-278`（本笔逐行现读）：
        /// <code>
        /// cVar13 = Object.op_Equality(targetCard /*+0x10*/, this);
        /// if (cVar13 != 0) {                       // ← 【我】就是这次动作的目标（= 死的是我）
        ///     cVar13 = EntityScript.HasCurrentTrait(this, 0x1d6 /* sacrifice */);
        ///     if (cVar13 != 0 &amp;&amp; IsPlayerTurn(manager) == this.isPlayer /*+0x40*/) {
        ///         CardScript.TriggerSacrifice(this);   // → OnTrigger(0xdc = 220 Sacrifice)
        ///         bVar21 = 1;                          // → 整个 ResolveDeadCard 返回 true
        ///     }
        /// }
        /// </code>
        /// 而 `TriggerSacrifice` 本体（`decomp_full/CardScript__TriggerSacrifice.c:26-33`）=
        /// `DisplayTriggerAnim(0x1d6)` + `OnTrigger(0xdc = 220 Sacrifice)` +
        /// `BattleManager.BroadcastSacrificeResolved(this)` —— 后者那半（= 别人的 `225 OtherCardSacrifice`）
        /// 我们的等价物是 `FireTriggerAlways` 尾部的 `BroadcastWhen(WhenEventKind.Triggers("sacrifice"))`
        /// （**手牌两跳也覆盖**，见 `RuleCore.cs:9587-9613` 那一大段裁定），⛔ 别在这里再发一次。
        ///
        /// ⚠️ **闸只有两道，一条都不多**：① **当前**带 `sacrifice`（`u.Has`，不是 `u.Card.Has`）·
        /// ② **是本方回合**（`ctx.Active == owner`，与 `TryCreditKill` 第 ⑥ 条 / 幸存者那一支同一个口径）。
        /// ⚠️ **`IsStunned` / 残骸 / 督军都不另判** —— 原版这一支里没有那些闸（督军走的是
        /// `ResolveDeadCard.c:24-56` 的另一支，那条不在本方法职责内）。
        ///
        /// 🔴 **2026-10-28（`B93_FX1`）：本方法【口已建、当前没有正确调用点】—— 调用已从
        /// `RuleCore.CleanupDeaths` 撤回**（`RuleCore.cs` 的 `leftPlay` 支现在只剩
        /// `SweepExtrinsicEffectsOnSourceDeath`）。理由（逐句现读，第一权威）：
        ///   · 本方法对应的原版分支要求**观察者就是本次 action 的 `targetCard`**
        ///     （`ResolveDeadCard.c:242` 的 `op_Equality(targetCard, this)` 为真才走 `:286-292`）；
        ///   · 观察者只能从 `BattleManagerSupport__BroadcastDeadUnit.c:82-90` 的一个入口来 ——
        ///     **遍历 `unitsInPlay`、每张先过 `CardScript__IsInPlay()`**（`CardScript__IsInPlay.c:5-9`：
        ///     只认状态 **2 / 15 / 3 / 17**）；
        ///   · 而「真的死了」那张卡的状态是 **`waitingToDie = 5`**
        ///     （`CardStateOptions.cs:9` · `CardScript__CheckIfDead.c:124` 写 `+0x228 = 5`）
        ///     ⇒ **一张真死的卡进不了那个循环** ⇒ 原版那一支对「真死」形态**不可达**；
        ///   · 它能被走到的**唯一**形态 = 「**仍在场（2/15/3/17）、且是本次 action 的 `targetCard`、但没死**」的观察者。
        /// ⇒ 正确接法要 `action.targetCard` 这个输入，而 `CleanupDeaths(ctx, p, slot, deathType)`
        ///   **没有这一路** ⇒ 撤回调用、⛔ 不改 `CleanupDeaths` 的位次、⛔ 不加闸把它「救活」。
        /// 生产影响 = **零**：池内带 `sacrifice`(trait 470) 的卡 **0 张**（本文件下面那段已记）。
        /// 账 → `资料/完全复刻/待接线与排队项.md`。
        ///
        /// ⚠️ **与 `RuleCore.cs:9576`（幸存者那一支）是互斥的两条路**：那一支是「被打到 0 血、
        /// 但 `Survivor` 把它救回来」；本方法是「**真的离场**」。原版也是两处：
        /// `CheckIfDead.c:152-158`（幸存者）与 `ResolveDeadCard.c:266-278`（死亡广播）⇒ ⛔ 别合并，
        /// 也别把本方法挪进 `maySurvive` 那一支（那样「被救回来」会响两次）。
        /// </summary>
        /// <returns>1 = 真的触发了；0 = 闸没过。</returns>
        static int TriggerSacrificeOnDeath(BattleContext ctx, UnitState dead, int owner, int slot)
        {
            if (ctx == null || dead == null) return 0;
            if (dead.Card == null) return 0;
            if (owner != 0 && owner != 1) return 0;
            // ① 当前带 `sacrifice`（原版 `HasCurrentTrait(0x1d6)`）
            if (!dead.Has(KeywordTable.Sacrifice)) return 0;
            // ② 本方回合（原版 `IsPlayerTurn(bm) == card.isPlayer`）
            if (ctx.Active != owner) return 0;
            // 结算口与幸存者那一支**完全同一个**（有正文才跑结算；没正文也发 `EvtKind.Trigger` + 广播 225）
            FireTriggerAlways(ctx, dead, KeywordTable.Sacrifice, owner, slot);
            ctx.Log($"{dead.Name} 阵亡 ⇒ 触发**献祭**（`sacrifice`）能力"
                  + "（原版 `CardScript__ResolveDeadCard.c:266-278` → `TriggerSacrifice` → `OnTrigger(220)`）");
            return 1;
        }
    }
}
