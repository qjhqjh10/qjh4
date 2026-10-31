// UnitSwarmSignal.cs — 原版 `UnitSwarmSignal : IBattleSignal, ISignal`（**虫群合并**的通用广播）
//
// 🆕 2026-11-01 · 第 111 会话 · 执行代理 `B111_E3B` · 账 `B-06b #35`（原 `B93_Q3_无落点220.tsv` C-35）。
//
// ============================ 判据（三源交叉，2026-11-01 现读）============================
// ① 形状 —— `cpp2il/out_cpp2il_dev_ilspy/UnitSwarmSignal.cs`（全文 20 行，🥇 可读）：
//    <code>
//    public class UnitSwarmSignal : IBattleSignal, ISignal {
//        public CardScript PlayedTroop;   // +0x10
//        public CardScript BuffedTroop;   // +0x18
//        public UnitSwarmSignal(CardScript playedTroop, CardScript buffedTroop) { … }
//    }
//    </code>
// ② 唯一生产者 —— `cpp2il/out_cpp2il_dev_ilspy/BattleManagerSupport.cs:1385-1388`
//    `BroadcastUnitSwarm(BattleManager manager, CardScript playedUnit, CardScript buffedUnit)`：
//    **第一句**就是 `new UnitSwarmSignal(playedUnit, buffedUnit)` + `Signal.Raise(signal)`，
//    之后才对场上所有卡 / 双方手牌逐个 `TriggeredSwarm`。
//    我们这一跳 = `RuleCore.SwarmMergeNow`（`RuleEngine/Core/RuleCore.cs`，出队执行体）——
//    它已经在同一拍里做了 `ctx.Tally.AddSwarmMerge(p)` + `FireTriggerAlways(...Swarm...)`，
//    本总线就挂在那一行的**同一物理行**上（⛔ 不新增行 —— 见 `CLAUDE.md` 18·b：`RuleCore.cs` 有大量账落点）。
// ③ 唯一消费者 —— `cpp2il/out_cpp2il_dev_ilspy/UnitSwarmCount.cs`：
//    `OnInitialize` ⇒ `Signal.Register(OnUnitSwarm)`；`OnReset` ⇒ `Unregister`；
//    `OnUnitSwarm(UnitSwarmSignal obj)` 的**全身**只有一句判断：
//    `if (((EntityScript)obj.PlayedTroop).isPlayer__BackingField) UpdateProgress(1);`
//    （`decomp_full/UnitSwarmCount__OnUnitSwarm.c` 的文件名与内容不符 —— 那份装的是
//      `UnitReanimated__OnUnitReanimated`，**以 ILSpy 这份为准**；`AchievementTally.cs:141` 早就记过这条撞名。）
//
// ============================ 为什么「两个 CardScript 引用」不建（照先例，不是省事）============================
// 原版那两格（`PlayedTroop` / `BuffedTroop`）**唯一消费者也不读它们** —— `UnitSwarmCount` 只读
// `PlayedTroop.isPlayer`，`BuffedTroop` 全树 0 读者。同族先例 = `RuleEngine/Core/CardPlayedSignal.cs`
// 文件头 ③：`CurrentCost(+0x28)` **原版零消费方 ⇒ 不建那一格**（⛔ 别为了「形状像」补死格，
// 那会诱导后人去接一条假通道）。⇒ 本仓收窄成**一位** `IsPlayer`，**结构不同、语义相同**
// （`CLAUDE.md` 18·0「⛔ 不许打标记」第 ② 类：实现风格，不是自设计）。
//
// ============================ 为什么总线开在引擎侧 ============================
// 生产者 `RuleCore.SwarmMergeNow` 在 `RuleEngine`，而 `RuleEngine` **不许反向依赖 `CardPresentation`**
// （`Shell/LiveOpsFilter.cs` 的 `LiveOpsSignal` 在表现层）⇒ 与 `CardPlayedSignals` 同一个理由、
// 同一个形状。**⛔ 不是第二套挑战总线**：它是同一个 `Scaffold.Core.Events.Signal` 角色在引擎侧的第二个落点。
//
// ============================ `IsPlayer` 怎么来（如实记：引擎自己判不出来）============================
// 原版是 `EntityScript.isPlayer`（「这张卡属于本地玩家」）。我们的 `BattleContext` **没有「本地座位」**
// 这个概念（`BotSeat` 的 doc：单机时座位 1 由 `SimpleAI` 驱动、联机时是真人 ⇒ 引擎判不出来）。
// ⇒ **复用 `CardPlayedSignals.LocalSeat`**（缺省 0）—— 同一个全局事实（谁是本地玩家）**只存一处**，
//    ⛔ 别再开第二份 `LocalSeat`（两份迟早不一致）。
// ⚠️ **已知还差**（与 `CardPlayedSignal` 同一条）：联机客机今天没人把 `LocalSeat` 设成 1 ⇒
//    客机侧 `IsPlayer` 恒 false ⇒ 挑战不推进。补点在 `BattleDriver.SetMySeat`（表现层文件，不在本代理白名单）。
using System;
using System.Collections.Generic;
// （Core 不许 `using UnityEngine` —— 架构护栏；这里用全限定 `UnityEngine.Debug`，同 `CardPlayedSignal.cs` 的写法）

namespace RuleEngine
{
    /// <summary>原版 `UnitSwarmSignal`（`out_cpp2il_dev_ilspy/UnitSwarmSignal.cs`）的**本仓收窄版**：
    /// 只留唯一被读的那一位（见文件头「两个 CardScript 引用不建」）。</summary>
    public sealed class UnitSwarmSignal
    {
        /// <summary>原版 `PlayedTroop.isPlayer`（`EntityScript +0x40` 的 backing field）：
        /// 这一下**合并**是本地玩家打出来的吗。</summary>
        public readonly bool IsPlayer;

        public UnitSwarmSignal(bool isPlayer) { IsPlayer = isPlayer; }
    }

    /// <summary>原版 `Scaffold.Core.Events.Signal` 在**虫群**这一路的落点（形状逐条对齐
    /// <see cref="CardPlayedSignals"/>：Register 允许重复 / Unregister 只摘第一条 / Raise 用副本派发）。</summary>
    public static class UnitSwarmSignals
    {
        static readonly List<Action<UnitSwarmSignal>> _listeners = new List<Action<UnitSwarmSignal>>();

        /// <summary>原版 `Signal.Register(Action&lt;UnitSwarmSignal&gt;)`：**允许重复注册**（原版 `List.Add` 不去重）。
        /// <para>`MissionChallenge.Initialize()` 自带一次性闩（`initialized`）⇒ 正常路径不会重复注册。</para></summary>
        public static void Register(Action<UnitSwarmSignal> callback)
        {
            if (callback == null)
            {
                UnityEngine.Debug.LogError("[UnitSwarmSignals] Register(null) ⇒ 忽略（原版会加进表里、然后 Raise 时 NRE）");
                return;
            }
            _listeners.Add(callback);
        }

        /// <summary>原版 `Signal.Unregister(...)`：`List.Remove`（**只摘第一条**）。摘不到时静默（同原版返回 false）。</summary>
        public static void Unregister(Action<UnitSwarmSignal> callback)
        {
            if (callback == null) return;
            _listeners.Remove(callback);
        }

        /// <summary>原版 `Signal.Raise(signal)`：逐个调。**没有监听器 ⇒ 什么都不做**（不是失败）。</summary>
        public static void Raise(UnitSwarmSignal signal)
        {
            if (signal == null) { UnityEngine.Debug.LogError("[UnitSwarmSignals] Raise(null) ⇒ 忽略（原版这一跳 NRE；本仓口径 = 出声 + 不推进）"); return; }
            if (_listeners.Count == 0) return;
            var snapshot = _listeners.ToArray();       // 副本：回调里退订/新订不许改正在派发的表
            for (int i = 0; i < snapshot.Length; i++)
            {
                var cb = snapshot[i];
                if (cb == null) continue;
                try { cb(signal); }
                catch (Exception e) { UnityEngine.Debug.LogError("[UnitSwarmSignals] 监听器抛异常（吞掉、继续派发）⇒ " + e); }
            }
        }

        /// <summary>**唯一发点**的那一跳：按座位算出 `IsPlayer` 再发一条。
        /// <para>⚠️ 两侧都会发（原版 `BroadcastUnitSwarm` 对双方的合并都广播，`IsPlayer` 只是**载荷**）
        /// —— ⛔ 别在这里提前按座位过滤：那道闸在消费者（`UnitSwarmCount.OnUnitSwarm`）里。</para>
        /// <para>🔴 **这里不许写 `ctx.Log` / `ctx.Emit`** —— 那会改 `ctx.Events.Count`，让 `play check` 指纹分叉
        /// （同族：`CLAUDE.md`「被拒的动作别写诊断日志」那条）。</para></summary>
        public static void RaiseFromSwarmMerge(int seat)
        {
            Raise(new UnitSwarmSignal(seat == CardPlayedSignals.LocalSeat));
        }

        /// <summary>自检口：当前监听器条数。</summary>
        public static int ListenerCount { get { return _listeners.Count; } }

        /// <summary>自检口：清空监听器（**只给断言用**，⛔ 产品路径别调）。
        /// ⚠️ **不清 `LocalSeat`** —— 那一格属于 `CardPlayedSignals`（唯一持有者），由它的 `ResetForTest` 管。</summary>
        public static void ResetForTest()
        {
            _listeners.Clear();
        }
    }
}
