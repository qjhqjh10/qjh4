// CardPlayedSignal.cs — 原版 `CardPlayedSignal : IBattleSignal, ISignal`（「任意一张卡打出去」的通用广播）
// 🆕 2026-10-30 · 执行代理 `B103_W1` · 账 `日常/奖励:39`（`ChallengeLogMgr.LogPlayTactic` 那一跳的下游）·
//   账 `日常/奖励:786`（`CardPlayedCount.OnCardPlayed` 的前置件 —— `CardPlayedSignal` 本仓**从零不存在**）
//   · 施工单正本 → `资料/完全复刻/判据/B102_R2_日常奖励B类五条.md` §一（逐句 `.c:行号` 都在那份里）
//
// ============================ 判据（2026-10-30 现读，三源）============================
// ① 结构（逐字）：`dump.cs:58900-58911`
//    <code>
//    public bool IsPlayer;          // +0x10
//    public RawCardScript RawCard;  // +0x18
//    public CardScript CurrentCard; // +0x20
//    public int CurrentCost;        // +0x28   ← ★
//    </code>
// ② 生产者只有两个（三源同证：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/**` 2,294 个签名桩 ·
//    `cpp2il/out_cpp2il_dev_ilspy/` 全树 · `decomp_full/*.c`）：
//    `ChallengeLogMgr.LogPlayTactic`（`ChallengeLogMgr__LogPlayTactic.c:347-350`，**战术卡**那条）
//    与 `ChallengeLogMgr.LogPlayTroop`（`ChallengeLogMgr__LogPlayTroop.c` 尾段，**单位卡**那条）。
//    两条都带 `EntityScript.get_CurrentCost(card)`（`.c:288`）—— 但**只有 LogPlayTactic 会顺手给
//    `PlayerBattleStats.tacticsPlayed(+0x14)` +1**（`.c:396-403`）；LogPlayTroop 不碰它。
// ③ 🔴 **`CurrentCost(+0x28)` 在原版里零消费方**（施工单 §一·b 三源普查）⇒ 我们**只照抄结构、
//    不建 cost 字段**：唯一消费者 `CardPlayedCount.OnCardPlayed` 读的是 **`RawCard.manaCost`
//    （`RawCardScript +0x78` = 卡面费用）**，不是实付费用（`CardPlayedCount.cs:97-102` 逐字）。
//    ⛔ 别为了「形状像」补一个没人读的死格（那会与原版形状不符、还会诱导后人去接一条假通道）。
// ④ `CurrentCard(+0x20)` 只被 `onlyCountCompanions` 那道闸读 `isCompanion`
//    （`CardPlayedCount__OnCardPlayed.c:53-66`：`obj.CurrentCard == null` ⇒ return，`+0x64 != 0` ⇒ 过）
//    ⇒ 我们收窄成一位 `CurrentCardIsCompanion`。**结构不同、语义相同**
//    （`CLAUDE.md` 18·0「⛔ 不许打标记」第 ② 类：实现风格，不是自设计）。
//
// ============================ 为什么总线开在引擎侧（⛔ 不是第二套挑战总线）============================
// · 原版两个发起源都在 `ChallengeLogMgr`（战斗侧），消费者 `CardPlayedCount` 也在战斗侧 ⇒ 同一层。
// · 我们的发起源必须落在 `RuleEngine`（`EffectResolver.PlayTacticCore` / `RuleCore.PlayCardCore` ——
//   与 `ctx.Tally.AddCardPlayed` 同一拍），而 `RuleEngine` **不许反向依赖 `CardPresentation`**
//   （`Shell/LiveOpsFilter.cs` 的 `LiveOpsSignal` 在表现层）⇒ 这座总线只能开在引擎侧。
// · 形状**逐条对齐** `LiveOpsSignal`：`Register` 允许重复（原版 `List.Add` 不去重）· `Unregister` 只摘第一条
//   · `Raise` 期间用**副本**派发（否则监听器在回调里退订会抛 `InvalidOperationException`）。
//   ⛔ 这不是「另造一条规则」：它是同一个 `Scaffold.Core.Events.Signal` 角色在引擎侧的落点，
//     消费者 `CardPlayedCount` 只订它一条。
//
// ============================ `IsPlayer` 怎么来（如实记：它不是引擎能判的）============================
// 原版是 `BattleAction.isPlayer`（`BattleAction` **struct** 的 `+4` 字节，`ChallengeLogMgr__LogPlayTactic.c:396`
// 与 `:349` 是同一个字节）—— 那是**发起动作那一侧**记下来的「是不是本地玩家」。
// 我们的 `BattleContext` **没有「本地座位」这个概念**（`BattleContext.BotSeat` 的 doc 原话：
// 「单机时座位 1 由 `SimpleAI` 驱动、联机时座位 1 是真人 —— **引擎自己判不出来**，只能由调用方给」）。
// ⇒ 这里用 <see cref="CardPlayedSignals.LocalSeat"/>（缺省 **0** = 单机恒定的本地座位）承载那一格。
// ⚠️ **还差**：联机客机（`BattleDriver.SetMySeat(1)`）今天没人把它同步过来 ⇒ 客机侧会被判成
//    `IsPlayer = false`（挑战不推进）。补法在 `BattleDriver.SetMySeat` 里加一行
//    `CardPlayedSignals.LocalSeat = seat;` —— 那是表现层文件，**不在本代理白名单**，已写进报告「还差什么」。
//    单机（`me` 恒 0）**行为完全正确**，⛔ 别因此把这格改成一个「猜」的默认值。
using System;
using System.Collections.Generic;
// （Core 不许 `using UnityEngine` —— 架构护栏；这里用全限定 `UnityEngine.Debug`，同 `ActionPump.cs` 的写法）

namespace RuleEngine
{
    /// <summary>原版 `CardPlayedSignal`（`dump.cs:58900-58911`）的**本仓收窄版**：去掉零消费方的
    /// `CurrentCost(+0x28)`、把 `CurrentCard` 折成它唯一被读的那一位（见文件头 ③④）。</summary>
    public sealed class CardPlayedSignal
    {
        /// <summary>原版 `+0x10`：这一张是**本地玩家**打出去的吗。</summary>
        public readonly bool IsPlayer;

        /// <summary>原版 `+0x18`：打出的那张卡的 **raw 定义**（`RawCardScript`）⇒ 我们这一层 = <see cref="CardDef"/>。
        /// <para>它是 `CardPlayedCount` 六道闸里 **五道**的数据源：`cardType(+0x94)` / `defaultTraits` /
        /// `keyword(+0x98)` / `manaCost(+0x78)` / `spellType(+0x90)`。</para></summary>
        public readonly CardDef RawCard;

        /// <summary>原版 `+0x20` 只被 `onlyCountCompanions` 那一道闸读 `isCompanion(+0x64)`
        /// （`CardPlayedCount__OnCardPlayed.c:53-66`）⇒ 折成这一位。</summary>
        public readonly bool CurrentCardIsCompanion;

        public CardPlayedSignal(bool isPlayer, CardDef rawCard, bool currentCardIsCompanion)
        {
            IsPlayer = isPlayer;
            RawCard = rawCard;
            CurrentCardIsCompanion = currentCardIsCompanion;
        }
    }

    /// <summary>`CardPlayedSignal` 的注册表 + 发点（形状照 `Shell/LiveOpsFilter.cs` 的 `LiveOpsSignal`）。
    /// <para>🔴 **两个发点（= 原版两个生产者）**：`EffectResolver.PlayTacticCore`（战术卡，≈ `LogPlayTactic`）
    /// 与 `RuleCore.PlayCardCore`（单位卡，≈ `LogPlayTroop`）—— 都在 `ctx.Tally.AddCardPlayed(...)` 的**同一拍**。</para></summary>
    public static class CardPlayedSignals
    {
        /// <summary>本地玩家是几号座位 —— 承载原版 `BattleAction.isPlayer` 那一格（见文件头末段）。
        /// <para>缺省 **0**（单机 `BattleDriver.SetMySeat(0)` 恒 0）。⚠️ 联机客机需要调用方设成 1，
        /// 今天**没人设**（`BattleDriver.SetMySeat` 不在本代理白名单）⇒ 已记进报告的「还差什么」。</para></summary>
        public static int LocalSeat = 0;

        static readonly List<Action<CardPlayedSignal>> _listeners = new List<Action<CardPlayedSignal>>();

        /// <summary>原版 `Signal.Register(Action&lt;CardPlayedSignal&gt;)`：**允许重复注册**（原版 `List.Add` 不去重）。
        /// <para>`MissionChallenge.Initialize()` 自带一次性闩（`initialized`）⇒ 正常路径不会重复注册。</para></summary>
        public static void Register(Action<CardPlayedSignal> callback)
        {
            if (callback == null)
            {
                UnityEngine.Debug.LogError("[CardPlayedSignals] Register(null) ⇒ 忽略（原版会加进表里、然后 Raise 时 NRE）");
                return;
            }
            _listeners.Add(callback);
        }

        /// <summary>原版 `Signal.Unregister(...)`：`List.Remove`（**只摘第一条**，与原版 `Remove` 同义）。</summary>
        public static void Unregister(Action<CardPlayedSignal> callback)
        {
            if (callback == null) return;
            _listeners.Remove(callback);   // 摘不到时静默 —— 原版 `List.Remove` 返回 false 同样不出声
        }

        /// <summary>原版 `Signal.Raise(signal)`：逐个调。**没有监听器 ⇒ 什么都不做**（不是失败）。</summary>
        public static void Raise(CardPlayedSignal signal)
        {
            if (signal == null) return;
            if (_listeners.Count == 0) return;
            var snapshot = _listeners.ToArray();       // 副本：回调里退订/新订不许改正在派发的表
            for (int i = 0; i < snapshot.Length; i++)
            {
                var cb = snapshot[i];
                if (cb == null) continue;
                try { cb(signal); }
                catch (Exception e) { UnityEngine.Debug.LogError("[CardPlayedSignals] 监听器抛异常（吞掉、继续派发）⇒ " + e); }
            }
        }

        /// <summary>**两个发点共用**的那一跳：按座位算出 `IsPlayer` 再发一条。
        /// <para>⚠️ 两侧都会发（原版 `LogPlayTactic` / `LogPlayTroop` 对双方的动作都发，`IsPlayer` 只是**载荷**）
        /// —— ⛔ 别在这里提前按座位过滤：那道闸在消费者（`CardPlayedCount.OnCardPlayed` 第一句）里。</para>
        /// <para>🔴 **这里不许写 `ctx.Log` / `ctx.Emit`** —— 那会改 `ctx.Events.Count`，让 `play check` 指纹分叉
        /// （见 `CLAUDE.md` 里「被拒的动作别写诊断日志」那条同族）。</para></summary>
        public static void RaiseFromCardPlayed(int seat, CardDef card, CardInstance inst)
        {
            if (card == null) return;                 // 原版 `RawCard` 为 null 时消费者会 NRE；我们干脆不发（不出声：调用点拿不到卡片本身就是别的 bug）
            Raise(new CardPlayedSignal(seat == LocalSeat, card, inst != null && inst.IsCompanion));
        }

        /// <summary>自检口：当前监听器条数。</summary>
        public static int ListenerCount { get { return _listeners.Count; } }

        /// <summary>自检口：清空监听器与本地座位（**只给断言用**，⛔ 产品路径别调）。</summary>
        public static void ResetForTest()
        {
            _listeners.Clear();
            LocalSeat = 0;
        }
    }
}
