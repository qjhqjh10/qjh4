// UnitSwarmCount.cs — 原版 `UnitSwarmCount : MissionChallenge`（「虫群合并 N 次」这类任务挑战的**消费方**）
//
// 🆕 2026-11-01 · 第 111 会话 · 执行代理 `B111_E3B` · 账 `B-06b #35`（原 `B93_Q3_无落点220.tsv` C-35）。
//
// ============================ 判据（三源交叉，2026-11-01 现读；⛔ 不要重新论证）============================
// 🥇 `cpp2il/out_cpp2il_dev_ilspy/UnitSwarmCount.cs`（全文 30 行，**可读，以此为准**）：
//   <code>
//   [Serializable] public class UnitSwarmCount : MissionChallenge {
//       public override CardArmy Army => CardArmy.Leviathan;          // 0x3C = 60
//       protected override void OnReset()      { Signal.Unregister(OnUnitSwarm); }
//       protected override void OnInitialize() { Signal.Register(OnUnitSwarm); }
//       private void OnUnitSwarm(UnitSwarmSignal obj) {
//           if (((EntityScript)obj.PlayedTroop).isPlayer__BackingField) UpdateProgress(1);
//       }
//   }
//   </code>
// ⚠️ **`decomp_full/UnitSwarmCount__OnUnitSwarm.c` 的文件名与内容不符** —— 那份装的是
//    `UnitReanimated__OnUnitReanimated`（同族已记录的「撞名丢失」，见 `RuleEngine/Core/AchievementTally.cs:141`）。
//    ⛔ 别拿它当判据；`UnitSwarmCount__get_Army.c` 那一份倒是对的（`return 0x3c` = `CardArmy.Leviathan` = 60）。
// 生产者 = `BattleManagerSupport.BroadcastUnitSwarm` 的第一句（见 `RuleEngine/Core/UnitSwarmSignal.cs` 文件头 ②）。
//
// ============================ 与我们的架构的对接（三处如实记）============================
// ① `Signal.Register/Unregister` → <see cref="RuleEngine.UnitSwarmSignals"/>（发起源在引擎侧，见那个文件的文件头）。
// ② 载荷收窄：`PlayedTroop.isPlayer` → `UnitSwarmSignal.IsPlayer`（原版两个 `CardScript` 引用**零消费方**
//    ⇒ 不建死格，口径同 `CardPlayedSignal.CurrentCost`）。
// ③ 🔶 **`Army => CardArmy.Leviathan` 没搬**：本仓 `MissionChallenge` 基类**没有 `Army` 这一位**
//    （`CardPlayedCount` 也按同一条记过）。它不参与 `OnUnitSwarm` 的判断，故不影响行为；
//    但**将来若有人给基类加 `Army`，这里要补一句 `public override CardArmy Army => CardArmy.Leviathan;`**。
//
// ⚠️ **本类今天没有实例**：它由远端 `MissionsConfig`（本机 0 份 SO）经 `MissionChallengeProvider` 造出来，
//    与 `CardPlayedCount` / `EnergySpent` / `MissionComplete` 完全同一个处境 ——
//    **「形状与行为在、数据在远端」**（`CLAUDE.md` 铁律 11：照建，休眠）。
// ⚠️ 文件名 == 类名（本仓不成文规矩）。本类是纯 `[Serializable]` 类、不是 `MonoBehaviour`/`ScriptableObject`
//    ⇒ 不吃「类名==文件名否则烘焙成 `!u!115`」那条后果（同 `Shell/CardPlayedCount.cs` 的说明），
//    但保持同名仍然是对的（别名会让人找不到）。
using System;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>原版 `UnitSwarmCount`（「触发 N 次虫群合并」这类挑战）。行为 = **一道闸** + 一次 `UpdateProgress(1)`，
    /// 逐句判据见文件头。</summary>
    [Serializable]
    public class UnitSwarmCount : MissionChallenge
    {
        /// <summary>原版 `protected override void OnInitialize()` = `Signal.Register(OnUnitSwarm)`
        /// （`out_cpp2il_dev_ilspy/UnitSwarmCount.cs:15-19`）。</summary>
        protected override void OnInitialize()
        {
            UnitSwarmSignals.Register(OnUnitSwarm);
        }

        /// <summary>原版 `protected override void OnReset()` = `Signal.Unregister(OnUnitSwarm)`
        /// （同上 `:9-13`）。
        /// <para>⚠️ 本仓基类 `MissionChallenge` 没有 `OnReset` 的抽象位 ⇒ 这里不是 `override`，
        /// 是「同名方法、由调用方显式调」（与 `Shell/CardPlayedCount.cs` / `Shell/EnergySpent.cs` 同一处置）。</para></summary>
        public void OnReset()
        {
            UnitSwarmSignals.Unregister(OnUnitSwarm);
        }

        /// <summary>原版 `private void OnUnitSwarm(UnitSwarmSignal obj)` 的**全身**：
        /// `if (obj.PlayedTroop.isPlayer) UpdateProgress(1);`
        /// <para>⚠️ 原版 `obj == null` 那一跳是 **NRE**（`PlayedTroop.isPlayer` 直接解引用）——
        /// 全工程口径改成「出声 + 不推进」（同 `CardPlayedCount.OnCardPlayed` 的处置，⛔ 不让游戏崩）。</para></summary>
        public void OnUnitSwarm(UnitSwarmSignal obj)
        {
            if (obj == null)
            {
                UnityEngine.Debug.LogError("[UnitSwarmCount] OnUnitSwarm：信号是 null（原版这一跳 NRE）⇒ 不推进");
                return;
            }
            if (!obj.IsPlayer) return;      // ① 原版唯一那道闸：只看本地玩家的合并
            UpdateProgress(1);              // ② 推进 1（不是覆盖）
        }
    }
}
