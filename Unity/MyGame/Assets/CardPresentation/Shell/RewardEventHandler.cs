// RewardEventHandler.cs — 原版 `RewardEventHandler : LiveOpsHandler<RewardEvent>`（奖励事件的 handler）
// 🆕 2026-10-28 · 执行代理 `B91_W7` · 账 `日常_奖励` 666 / 667（G1 奖励事件层，裁定 1：按原版类型名独立成文件）
//
// ── 判据（逐条现读）──────────────────────────────────────────────────────────
//   · `get_HandlerKey` = `cpp2il/out_il2csharp/.../RewardEventHandler.cs:17` —— **字面量 `"Rewards"`**。
//   · `ApplyExtraRewards(ILiveOps evt, IEnumerable<RewardInfo> rewards)`（同文件 `:23`）
//     = **全部内容** `GetExtraRewardsFunction(evt).Invoke(rewards)`（一句转发）。
//   · `GetExtraRewardsFunction(ILiveOps evt)`（`:38`）返回的闭包（`__c__DisplayClass3_0._b__0`，`:67`）：
//     <code>
//     id = evt.GetID();
//     return rewards.Concat(
//         GetEvents(false)
//             .Where(x =&gt; x.Data.EventIds.Contains(id))                      // b__1
//             .SelectMany(x =&gt; x.Data.Type != 0                            // b__2
//                              ? x.Data.Function.Preview(evt, rewards)
//                              : (IEnumerable&lt;RewardInfo&gt;)x.Data.Rewards));
//     </code>
//     ⇒ 语义：**别的奖励事件若把这条事件 id 列进自己的 `EventIds`**，它就把自己的奖励「追加」给这条事件
//       （有函数 ⇒ 现算；否则 ⇒ 直接给静态表）。
//
// ── 与我们的架构的对接 ─────────────────────────────────────────────────────────
//   🔶 自己设计（非原版）：原版 `GetEvents(false)` 是 handler 里那批 `RewardEvent`（它们实现 `ILiveOps`）；
//     本工程的 `RewardEvent`（见 `Shell/RewardEvent.cs`）**不派生 `LiveOpsEvent`** ⇒ 拿不到
//     `GetEvents&lt;RewardEvent&gt;(bool)` 那层泛型过滤。⇒ 本类改为**持有宿主登记进来的 `RewardEvent` 表**
//     （<see cref="RegisterEvent"/> / <see cref="UnregisterEvent"/>），语义与原版逐句一致。
//     为什么必须自己定：`RewardEvent` 在本批是「判定器」形态（裁定 1：本批不做生命周期）·
//     调度台 2026-10-28 定。
//     ⚠️ 表为空时**出声一次**（⛔ 不静默返空 —— 那会让「没有额外奖励」与「没登记事件」长得一样）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `RewardEventHandler` 的本地等价物。
    /// <para>⚠️ 它**只管「追加额外奖励」这一件事**；`LiveOpsHandler` 基类那一整套（Refresh / SetupEvents /
    /// 过期排期）由 `Shell/LiveOpsHandler.cs` 的通用基类承担 —— 本类不覆盖任何一条。</para></summary>
    public class RewardEventHandler : LiveOpsHandler
    {
        /// <summary>原版 `get_HandlerKey` ⇒ 字面量 **`"Rewards"`**（`RewardEventHandler.cs:17`）。</summary>
        public override string HandlerKey { get { return "Rewards"; } }

        readonly List<RewardEvent> _events = new List<RewardEvent>();

        /// <summary>宿主把「本 handler 管的那批奖励事件」登记进来（等价于原版 handler 里那份事件表）。</summary>
        public void RegisterEvent(RewardEvent evt)
        {
            if (evt == null) return;
            if (!_events.Contains(evt)) _events.Add(evt);
        }

        /// <summary>摘掉一条（原版 `SetupEvents` 重建整表时的那一跳）。</summary>
        public void UnregisterEvent(RewardEvent evt)
        {
            if (evt != null) _events.Remove(evt);
        }

        /// <summary>当前登记的事件表（只读视图；自检/诊断用）。</summary>
        public IReadOnlyList<RewardEvent> Events { get { return _events; } }

        /// <summary>清空（自检收尾；也是原版 `SetupEvents` 重建前的 `Clear`）。</summary>
        public void ClearEvents() { _events.Clear(); }

        /// <summary>原版 `IEnumerable&lt;RewardInfo&gt; ApplyExtraRewards(ILiveOps evt, IEnumerable&lt;RewardInfo&gt; rewards)`
        /// （`RewardEventHandler.cs:23`，**全部内容**是一句 `GetExtraRewardsFunction(evt).Invoke(rewards)`）。</summary>
        public IEnumerable<RewardInfo> ApplyExtraRewards(ILiveOps evt, IEnumerable<RewardInfo> rewards)
        {
            return GetExtraRewardsFunction(evt)(rewards);
        }

        /// <summary>原版 `Func&lt;IEnumerable&lt;RewardInfo&gt;, IEnumerable&lt;RewardInfo&gt;&gt; GetExtraRewardsFunction(ILiveOps evt)`
        /// （`RewardEventHandler.cs:38` + 闭包 `b__0/b__1/b__2`，逐句见文件头）。</summary>
        public Func<IEnumerable<RewardInfo>, IEnumerable<RewardInfo>> GetExtraRewardsFunction(ILiveOps evt)
        {
            if (evt == null)
            {
                Debug.LogError("[RewardEventHandler] GetExtraRewardsFunction(null) ⇒ 原版会在 `evt.GetID()` 处 NRE；"
                             + "我们返一个「原样返回」的函数并出声");
                return rewards => rewards;
            }
            string id = evt.GetID();
            return rewards =>
            {
                var result = new List<RewardInfo>();
                if (rewards != null) result.AddRange(rewards);              // 原版 `rewards.Concat(...)`

                for (int i = 0; i < _events.Count; i++)
                {
                    var x = _events[i];
                    if (x == null) continue;
                    // b__1：这条事件把目标 id 列进了自己的 EventIds（原版是 `Data.EventIds.Contains(id)`，⛔ 不做额外判空分支）
                    if (x.Data == null || x.Data.EventIds == null || !x.Data.EventIds.Contains(id)) continue;

                    // b__2：Type != 0 ⇒ 走函数现算；否则直接给静态奖励表
                    if (x.Data.Type != RewardType.Static)
                    {
                        var fn = x.Data.Function;
                        if (fn == null)
                        {
                            Debug.LogWarning("[RewardEventHandler] 事件 '" + x.GetID() + "' 是函数型奖励但 `Function` 为 null "
                                           + "⇒ 这条事件不贡献额外奖励（原版这一档 NRE）");
                            continue;
                        }
                        var extra = fn.Preview(evt, rewards);
                        if (extra != null)
                            foreach (var r in extra)
                            {
                                if (r != null) { r.IsExtra = true; r.ExtraRewardProvider = HandlerKey; }
                                result.Add(r);
                            }
                    }
                    else if (x.Data.Rewards != null)
                    {
                        foreach (var r in x.Data.Rewards)
                        {
                            if (r != null) { r.IsExtra = true; r.ExtraRewardProvider = HandlerKey; }
                            result.Add(r);
                        }
                    }
                }
                return result;
            };
        }

        // ============================================================ 一次性出声（⛔ 不静默返空）
        bool _warnedEmpty;
        /// <summary>原版这一跳靠 `GetEvents(false)` 拿表；我们靠登记 ⇒ 表空时出声一次，
        /// 好让「没有额外奖励」与「没人登记事件」区分得开。</summary>
        public void WarnIfNoEventsOnce()
        {
            if (_warnedEmpty || _events.Count > 0) return;
            _warnedEmpty = true;
            Debug.Log("[RewardEventHandler] 还没有任何 `RewardEvent` 被 `RegisterEvent` 登记 ⇒ 额外奖励表恒为空"
                    + "（原版这里读的是 handler 自己的事件表，语义相同）");
        }

        /// <summary>自检收尾。</summary>
        public void ResetForTest() { _events.Clear(); _warnedEmpty = false; }
    }
}
