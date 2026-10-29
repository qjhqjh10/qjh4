// TroopsKilledCount.cs — 原版 `TroopsKilledCount : MissionChallenge`（「击杀 N 个部队」这类任务挑战的**消费方**）
// 🆕 2026-10-29 · 执行代理 `B98_A11` · 账 `战斗逻辑驱动:1360`（`TroopsKilledCount.OnTroopKilled`）
//   · 判据总表 → `资料/完全复刻/判据/B98_R7_消费方两条.md` §一·1360（本文件只做它派下来的 A2 那一块）
//
// ============================ 判据（逐条现读 2026-10-29）============================
// ① 消费者本体 = **任务子类实例自己**（不是全局单例）
//    `cpp2il/out_il2csharp/Assembly-CSharp/TroopsKilledCount.cs:7-29`（结构最可读）：
//    <code>
//    [Serializable] class TroopsKilledCount : MissionChallenge
//    protected override void OnInitialize() { Signal.Register&lt;TroopKilledSignal&gt;(OnTroopKilled); }   // Slot 9
//    protected override void OnReset()      { Signal.Unregister&lt;TroopKilledSignal&gt;(OnTroopKilled); } // Slot 8
//    </code>
// ② handler 真身 = `decomp_full/TroopsKilledCount__OnTroopKilled.c`（**全文 11 行**，🥇）：
//    <code>
//    if (signal == 0) throw;                    // `param_2 == 0`                            ⇒ FUN_1803f47a0()（抛）
//    if (signal.killer(+0x18) == 0) throw;      // 凶手空                                    ⇒ 抛（⛔ 不是 return）
//    if (killer.isPlayer(+0x40) == 0) return;   // ★ 只数「我方击杀」
//    MissionChallenge.UpdateProgress(this, 1, shouldOverride: false);   // ★ +1，不是覆盖
//    </code>
//    它的 `OnInitialize` 产物（`decomp_full/TroopsKilledCount__OnInitialize.c`）= `Signal.Register&lt;TroopKilledSignal&gt;`
//    一跳；`TroopsKilledCount__.ctor.c` 的首行印成 `AddSabotageCard___ctor`，但体内是 `MissionChallenge___ctor`
//    ⇒ **ICF 折叠**（不是「没解出父类」）。
// ③ 信号生产者 = `ChallengeLogMgr.TroopKilled(BattleAction)`
//    （`out_il2csharp/Assembly-CSharp/ChallengeLogMgr.cs:893-894`：`new TroopKilledSignal(troop, killer)` + `Signal.Raise`）。
//    全库**只有一个消费者** = 本类（`grep -rl TroopKilledSignal` 只命中「生产者 + 信号定义 + 本类」三份）。
//
// ============================ 与我们的架构的对接 ============================
// · `Signal`（`Scaffold.Core.Events`）= `Shell/LiveOpsFilter.cs` 的 `LiveOpsSignal`（同角色；`MissionCompleteSignal`
//   走的就是它）⇒ 这里注册/摘都用它，⛔ 不为挑战层另造第二条总线（两处写同一条规则 = 迟早不一致）。
// · 信号载荷：原版两格都是 `CardScript`（`troop +0x10` / `killer +0x18`）。本仓 `Shell/` 这一层拿不到
//   `CardScript`（那是 `RuleEngine` 的战斗对象），而**消费者只读 `killer.isPlayer` 这一位**（`.c` 全文就这一处读 payload）
//   ⇒ 收窄成 `bool killerIsPlayer`。**结构不同、语义相同**（按 `CLAUDE.md` 18·0「⛔ 不许打标记」第②类，不挂 `🔶`）。
//   ⛔ 别为了「形状像」把引擎对象塞进来链（那会把 Shell 层焊死在引擎上）。
// · 🔴 **喂入点不在这里**：本类只负责「收到信号 ⇒ 进度 +1」。谁在什么时候发信号 = `CardPresentation/Battle/BattleDriver.cs`
//   的结算拍（账 `B98_A11` 报告 §B4 给了可粘贴指引）。本批**没有**接（那个文件归 `B98_G4`）。
// · 🔴 **别用 `ChallengeEngine.TroopKilled(bool isTroop)` 当喂入源**：它照的是 `ChallengeLogMgr.TroopKilled` 里
//   **按【死者】`isPlayer` 计 `PlayerBattleStats`** 的那一半，而本类看的是**【凶手】`isPlayer`** ⇒ 直接串起来会把
//   「敌方杀我方」也算进「我方击杀」（静默错账）。正确读数口 = `AchievementTally.TroopsKilledOf(座位)`。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `TroopKilledSignal : IBattleSignal, ISignal`（`out_il2csharp/TroopKilledSignal.cs`）：
    /// 载荷两格 `troop(+0x10)` / `killer(+0x18)`，**都是 `CardScript`**。
    /// <para>⚠️ 我们收窄成**一格 `killerIsPlayer`** —— 理由见文件头「与我们的架构的对接」第二条。
    /// 唯一消费者（<see cref="TroopsKilledCount.OnTroopKilled"/>）读的就是这一位。</para></summary>
    public class TroopKilledSignal : ILiveOpsSignal
    {
        /// <summary>凶手是不是**本地玩家**（原版 `killer.isPlayer`，`CardScript +0x40`）。</summary>
        public readonly bool killerIsPlayer;

        public TroopKilledSignal(bool killerIsPlayer) { this.killerIsPlayer = killerIsPlayer; }
    }

    /// <summary>原版 `TroopsKilledCount`（「击杀 N 个部队」这类挑战）。行为只有三跳，见文件头判据 ①②。</summary>
    [Serializable]
    public class TroopsKilledCount : MissionChallenge
    {
        /// <summary>原版 `protected override void OnInitialize()`（Slot 9）= `Signal.Register&lt;TroopKilledSignal&gt;(OnTroopKilled)`。
        /// ⚠️ 原版 `Signal.Register` 允许重复注册（`List.Add` 不去重）⇒ 这里同样不去重；`MissionChallenge.Initialize()`
        /// 自带一次性闩（`initialized`，见 `MissionEvent.cs:70`）⇒ 正常路径不会重复注册。</summary>
        protected override void OnInitialize()
        {
            LiveOpsSignal.Register<TroopKilledSignal>(OnTroopKilled);
        }

        /// <summary>原版 `protected override void OnReset()`（Slot 8）= `Signal.Unregister&lt;TroopKilledSignal&gt;(OnTroopKilled)`。
        /// <para>⚠️ 本仓基类 `MissionChallenge` 只把 `OnInitialize` 声明成 `abstract`（`Shell/MissionEvent.cs:554`），
        /// **没有** `OnReset` 的抽象位 ⇒ 这里不是 `override`，是「同名方法，由调用方显式调」
        /// （与 `Shell/RewardCollected.cs:109` 的 `OnReset` 同一处置）。</para></summary>
        public void OnReset()
        {
            LiveOpsSignal.Unregister<TroopKilledSignal>(OnTroopKilled);
        }

        /// <summary>原版 `private void OnTroopKilled(TroopKilledSignal obj)` —— 全文 11 行，逐句见文件头判据 ②。
        /// <para>⚠️ 原版两道空闸都是**抛**（`FUN_1803f47a0`），全工程口径改成「出声 + 不算」（⛔ 不让游戏崩）；
        /// 效果上与原版一致（抛出去的那一次也不计数）。</para>
        /// <para>⚠️ 「凶手是 null」在我们这一层**表示不出来**：载荷已经折成 `killerIsPlayer` 一位。
        /// 若将来喂入方「不知道凶手是谁」，**不要**默认当 `true`（那会多算）—— 干脆**别发**这条信号
        /// （原版那一档是抛异常 ⇒ 不计数）。</para></summary>
        public void OnTroopKilled(TroopKilledSignal obj)
        {
            if (obj == null)
            {
                Debug.LogError("[TroopsKilledCount] OnTroopKilled：信号是 null（原版这一跳 NRE）⇒ 不推进");
                return;
            }
            if (!obj.killerIsPlayer) return;                 // ★ 只数「我方击杀」（`.c:5-8`）
            UpdateProgress(1, false);                        // ★ +1（`shouldOverride: false`，`.c:9`）
        }

        // ============================================================ 喂入桥（本仓新增，非原版）
        /// <summary>🔶 自己设计（非原版）：**按增量补发 N 条 `TroopKilledSignal`** ·
        /// 为什么必须自己定 —— 原版是 `ChallengeLogMgr.TroopKilled` **逐杀**发一条信号，而我们的引擎侧
        /// 只有「累计击杀数」一个读数口（`RuleEngine/Core/AchievementTally.TroopsKilledOf(seat)`），
        /// 且 `EvtKind.Death`（`RuleEngine/Core/BattleEvent.cs:510-543`）**不带凶手** ⇒ 拿不到「每一条的 killer.isPlayer」。
        /// 于是喂入端只能「结算拍算差值、按差值补发 N 条」—— 补发的**条数与每条内容**都与原版逐杀一致，
        /// 差的只是「时机」（局内没有任何读者，挑战进度只在菜单里看 ⇒ 行为等价）。
        /// 调度台 2026-10-29 判（`资料/完全复刻/判据/B98_R7_消费方两条.md` §一·1360 块 B）。
        /// <para>⛔ 这不是「原版的调用口」：原版没有这个方法，喂入由 `ChallengeLogMgr` 逐杀驱动。</para></summary>
        public static int RaiseKilledForPlayer(int count)
        {
            if (count <= 0) return 0;
            for (int i = 0; i < count; i++) LiveOpsSignal.Raise(new TroopKilledSignal(true));
            return count;
        }
    }
}
