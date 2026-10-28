// RewardEvent.cs — 原版 `RewardEvent : LiveOpsEvent<RewardEventPersistence, RewardEventData>`（登录奖励类活动）
// 🆕 2026-10-28 · 执行代理 `B91_W7` · 账 `日常_奖励` 660 / 661 / 662 / 663（G1 奖励事件层，裁定 1：按原版类型名独立成文件）
//
// ── 判据（全部逐条现读，⛔ 不要重新论证）────────────────────────────────────────
//   · 字段/偏移：`cpp2il/out_il2csharp/Assembly-CSharp/Everguild/LiveOps/RewardEventData.cs`
//       `+0x60 Price currencyRequirement` · `+0x68 bool spendOnReceive` · `+0x6c RewardFilter Filter`
//       `+0x70 string LiveOpsHandlerKey` · `+0x78 List<string> EventIds` · `+0x80 CardArmy[] armyAllowList`
//       `+0x88 CardArmy[] armyBlacklist` · `+0x90 RewardType Type` · `+0x98 RewardInfo[] Rewards`
//       `+0xa0 EventFunctionBase Function`（同一份文件给了每个成员的 `// 0xNN`）
//   · `IsTargetTo(ILiveOps)` = 同文件 `RewardEvent.cs:60`：
//       取 `Data.Function`；是 `EventScoringFunction` ⇒ 取它的 `TargetEvent.GetID()`；
//       是 `GenericEventFunction` ⇒ 取 `Args.Select(a => a.Arg).LastOrDefault() as ILoadableReference` 的 `TargetId`；
//       **两者都不是 ⇒ false**；最后 `targetId == evt.GetID()`。
//   · `AppliesTo(ILiveOps)` = 同文件 `:122`：按 `Data.Filter` 三档（0 ById / 1 ByHandler / 2 ByType）。
//   · `IsArmyValid(CardArmy)` = 同文件 `:167`：`allowList` 非空 ⇒ 只看它；否则 `blacklist` 非空 ⇒ 取反；都没有 ⇒ true。
//   · `HandleRewardDisplay()` = 同文件 `:197`：`text = string.Format("+{0}", r.Quantity)` →
//       `new ToastNotificationContext(text, icon:0, onClick:0, data:r, controllerID:GetID(), type:0)` → `ScheduleNotification`。
//       ⚠️ **Icon 实参是 0**（图不走 Icon 字段），`data` = 奖励对象本身 —— 见账 661 的现读。
//
// ── 与我们的架构的对接 ─────────────────────────────────────────────────────────
//   · 本类**暂不派生** `LiveOpsEvent<TSave,TData>`：那条基类要 `TSave : LiveOpsSave` + 一整套 `Initialize/LoadEventData/
//     存档/过期` 生命周期，而本账这四条要的是**四个纯判定**（零状态机）。
//     🔶 自己设计（非原版）：`RewardEvent` 先做成「持有一份 `RewardEventData` 的判定器」；
//       为什么必须自己定：那套生命周期在本工程由 `Shell/LiveOpsEvent.cs` 的泛型基类承担，而 `RewardEventPersistence`
//       在本机没有任何字段（`out_il2csharp/.../RewardEventPersistence.cs` = 空类）⇒ 派生只会多一层空壳 ·
//       调度台 2026-10-28（裁定 1）定。
//   · 原版 `Price`（`currencyRequirement`）本工程没有对应类型 ⇒ `CurrencyRequirement` 返 null 并如实记（那两条
//     属性**不在本例这四条里**，账里也没单独立行）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `RewardEventData.RewardFilter`：这条奖励事件「指向谁」的三种判法（`AppliesTo` 的三档）。</summary>
    public enum RewardFilter
    {
        /// <summary>0：按**事件 id 列表**命中（`Data.EventIds.Contains(evt.GetID())`）。</summary>
        ById = 0,
        /// <summary>1：按 **handler key** 命中（`evt.Handler.HandlerKey == Data.LiveOpsHandlerKey`）。</summary>
        ByHandler = 1,
        /// <summary>2：按**事件类型名**命中（`Data.EventIds.Contains(类型名)`）。</summary>
        ByType = 2,
    }

    /// <summary>原版 `RewardEventData.RewardType`：这条事件的奖励从哪来（`RewardEventHandler` 的 `b__2` 读它）。</summary>
    public enum RewardType
    {
        /// <summary>0：用 `Rewards` 这张**静态表**。</summary>
        Static = 0,
        /// <summary>非 0：交给 `Function.Preview(evt, 已领到的奖励)` 现算。</summary>
        Function = 1,
    }

    /// <summary>原版 `RewardEventData : LiveOpsEventData`（判据 = 文件头那串字段 + 偏移，逐个照抄）。
    /// <para>⚠️ 序列化用本工程统一的显式 DTO 口径（`JsonUtility`；⛔ 不引 Newtonsoft）—— 原版那几个
    /// `[JsonIgnore]` 特性在这里就是**不参与序列化**的普通字段，由调用方决定灌不灌。</para></summary>
    [Serializable]
    public class RewardEventData
    {
        // +0x60 / +0x68：原版 `Price currencyRequirement` + `bool spendOnReceive`。
        // ⚠️ 本工程没有 `Price` 类型 ⇒ 位置留着、值是 null，⛔ 不编价格（账 660-663 四条都不读它）。
        public object CurrencyRequirement;          // 原版 +0x60（Price）
        public bool SpendOnReceive;                 // 原版 +0x68
        /// <summary>原版 `+0x6c RewardFilter Filter`。</summary>
        public RewardFilter Filter;
        /// <summary>原版 `+0x70 string LiveOpsHandlerKey`。</summary>
        public string LiveOpsHandlerKey;
        /// <summary>原版 `+0x78 List&lt;string&gt; EventIds`（ById 档 = 事件 id；ByType 档 = 类型名）。</summary>
        public List<string> EventIds = new List<string>();
        /// <summary>原版 `+0x80 CardArmy[] armyAllowList`（非空 ⇒ 只认这里面的阵营）。</summary>
        public CardArmy[] ArmyAllowList;
        /// <summary>原版 `+0x88 CardArmy[] armyBlacklist`（`allowList` 为空时才看它）。</summary>
        public CardArmy[] ArmyBlacklist;
        /// <summary>原版 `+0x90 RewardType Type`（`b__2` 据此决定「静态表」还是「函数现算」）。</summary>
        public RewardType Type;
        /// <summary>原版 `+0x98 RewardInfo[] Rewards`（静态奖励表）。</summary>
        public RewardInfo[] Rewards;
        /// <summary>原版 `+0xa0 EventFunctionBase Function`（奖励变换函数；本工程用 <see cref="IRewardFunction"/>）。</summary>
        public IRewardFunction Function;
    }

    /// <summary>原版 `EventFunctionBase` 里**只有那两个派生类才有**的一格：这条函数指向哪个事件。
    /// <para>判据 = `RewardEvent.IsTargetTo` 的两支：`EventScoringFunction.TargetEvent` /
    /// `GenericEventFunction.Args.LastOrDefault() as ILoadableReference&lt;IIdentifiable&gt;` 的 `TargetId`。</para>
    /// <para>🔴 **不实现本接口 = 原版的「两者都不是」⇒ `IsTargetTo` 返 false**（例如
    /// <see cref="MultiplyRewardsFunction"/> 在原版就落这一档）。</para>
    /// <para>🔶 自己设计（非原版）：两种原版函数类在本工程都不存在 ⇒ 收成一个「能不能报出目标事件 id」的接口；
    /// 为什么必须自己定：判据件只给了那两个类各自的字段，没有共同基类形状 · 调度台 2026-10-28 定。</para></summary>
    public interface IRewardFunctionTarget
    {
        /// <summary>这条函数指向的事件 id（null = 没有目标）。</summary>
        string TargetEventId { get; }
    }

    /// <summary>原版 `RewardEvent`（登录奖励类活动）里**与 UI / 存档无关的那四个判定 + 一条飘窗**。
    /// <para>⚠️ 它**不做生命周期**（`Initialize` / `LoadEventData` / 存档 / 过期都不在这里）——
    /// 理由见文件头「与我们的架构的对接」。</para></summary>
    public class RewardEvent
    {
        /// <summary>这条事件的数据（原版 `LiveOpsEvent&lt;,&gt;.Data` 的直读等价物）。</summary>
        public RewardEventData Data;

        public RewardEvent() { }
        public RewardEvent(RewardEventData data) { Data = data; }

        /// <summary>事件 id。⚠️ 原版 `GetID()` 读 `Data.eventId`（`LiveOpsEventData` 侧）—— 本类的 `Data` 只放
        /// 奖励那半 ⇒ 由宿主填 <see cref="EventId"/>（没填 ⇒ 返空串并出声一次，⛔ 不静默）。</summary>
        public string EventId;

        public string GetID()
        {
            if (string.IsNullOrEmpty(EventId))
            {
                Debug.LogWarning("[RewardEvent] GetID()：`EventId` 没填（原版读 `Data.eventId`）⇒ 返空串");
                return "";
            }
            return EventId;
        }

        /// <summary>原版 `bool HasRequirements`＝「`currencyRequirement` 有货币且够付」。
        /// ⚠️ 本工程没有 `Price`/`CurrencyValue` 那一层 ⇒ 恒 false 并出声（⛔ 不假装能付费）。</summary>
        public bool HasRequirements
        {
            get
            {
                if (Data != null && Data.CurrencyRequirement != null)
                    Debug.LogWarning("[RewardEvent] HasRequirements：本工程没有 `Price`/`CurrencyValue` 类型 ⇒ 判不了「够不够付」，返 false");
                return false;
            }
        }

        /// <summary>原版 `EventFunctionBase RewardFunction => Data.Function`。</summary>
        public IRewardFunction RewardFunction { get { return Data == null ? null : Data.Function; } }

        /// <summary>原版 `bool AppliesTo(ILiveOps evt)`（`RewardEvent.cs:122`，三档，⛔ 别加第四档）。
        /// <code>
        /// if (Filter == 0) return Data.EventIds.Contains(evt.GetID());
        /// if (Filter == 1) return evt.Handler != null &amp;&amp; evt.Handler.HandlerKey == Data.LiveOpsHandlerKey;
        /// if (Filter == 2) return Data.EventIds.Contains(RewardEventData.GetTypeName(evt.GetType()));
        /// return false;
        /// </code>
        /// <para>⚠️ `GetTypeName` 那一句（账 664 判 D = 序列化辅助）在这里只用来**比较**：原版实现是
        /// 「同程序集 ⇒ `Type.FullName`，否则 `AssemblyQualifiedName`」；奖励事件与消费方都在 `Assembly-CSharp`
        /// ⇒ 同程序集那一支 ⇒ 这里就用 `FullName`（⛔ 不为它再造一个序列化用的重载）。</para></summary>
        public bool AppliesTo(ILiveOps evt)
        {
            if (Data == null)
            {
                Debug.LogWarning("[RewardEvent] AppliesTo：没有 data（原版这一档 NRE）⇒ false");
                return false;
            }
            if (evt == null) { Debug.LogWarning("[RewardEvent] AppliesTo(null) ⇒ false"); return false; }

            switch (Data.Filter)
            {
                case RewardFilter.ById:
                    return Data.EventIds != null && Data.EventIds.Contains(evt.GetID());
                case RewardFilter.ByHandler:
                    return evt.Handler != null && evt.Handler.HandlerKey == Data.LiveOpsHandlerKey;
                case RewardFilter.ByType:
                    {
                        var t = evt.GetType();
                        return Data.EventIds != null && Data.EventIds.Contains(t.FullName);
                    }
                default:
                    return false;
            }
        }

        /// <summary>原版 `bool IsArmyValid(CardArmy army)`（`RewardEvent.cs:167`）：
        /// `allowList` 非空 ⇒ **只**看它（在里面才有效）；否则 `blacklist` 非空 ⇒ 不在里面才有效；都没有 ⇒ true。</summary>
        public bool IsArmyValid(CardArmy army)
        {
            if (Data == null) return true;                                  // 原版这一档会 NRE；坏数据不让它崩
            if (Data.ArmyAllowList != null && Data.ArmyAllowList.Length > 0)
                return Array.IndexOf(Data.ArmyAllowList, army) >= 0;
            if (Data.ArmyBlacklist != null && Data.ArmyBlacklist.Length > 0)
                return Array.IndexOf(Data.ArmyBlacklist, army) < 0;
            return true;
        }

        /// <summary>原版 `bool IsTargetTo(ILiveOps evt)`（`RewardEvent.cs:60`）：
        /// 取 `Data.Function` 报出的目标事件 id（`EventScoringFunction` / `GenericEventFunction` 两支），
        /// **两者都不是 ⇒ false**；最后与 `evt.GetID()` 比。
        /// <para>⚠️ 本工程用 <see cref="IRewardFunctionTarget"/> 顶那两个派生类的分支 ——
        /// `MultiplyRewardsFunction` 等**不实现**它 ⇒ 与「两者都不是 ⇒ false」完全一致。</para></summary>
        public bool IsTargetTo(ILiveOps evt)
        {
            if (evt == null) return false;
            var fn = RewardFunction;
            if (fn == null) return false;
            var withTarget = fn as IRewardFunctionTarget;
            if (withTarget == null) return false;                            // 原版：既非 scoring 也非 generic
            string targetId = withTarget.TargetEventId;
            return targetId != null && targetId == evt.GetID();
        }

        /// <summary>原版 `private void HandleRewardDisplay()`（`RewardEvent.cs:197`）：逐条奖励弹一条飘窗。
        /// <code>
        /// foreach (r in Data.Rewards) {
        ///     text = String.Format("+{0}", r.Quantity);
        ///     ScheduleNotification(new ToastNotificationContext(text, icon:0, onClick:0, data:r, controllerID:GetID(), type:0));
        /// }
        /// </code>
        /// 🔴 判据（账 661 现读）：**图标实参是 0**（图不走 `Icon` 字段）、`data` = 奖励对象本身、
        /// `type` = 0（本工程 `ToastNotificationType.Achievement`）、**每条一个**（推迟一拍由总线做）。
        /// <para>✅ **2026-10-28（B93_D4）**：本工程 `ToastNotifications.Context` 已补 `object Data` 槽
        /// ⇒ 奖励对象照原版第 4 实参传进去（`ctx.Data = r`），`EventBadge.SetNotificationData` 第 ③ 段的
        /// `Data is RewardInfo` 那一支因此接通（账 158 / 662 同一处上游）。</para></summary>
        public void HandleRewardDisplay()
        {
            if (Data == null || Data.Rewards == null)
            {
                Debug.LogWarning("[RewardEvent] HandleRewardDisplay：没有奖励表（原版这一档 NRE）⇒ 不弹");
                return;
            }
            string controllerId = GetID();
            for (int i = 0; i < Data.Rewards.Length; i++)
            {
                var r = Data.Rewards[i];
                if (r == null) continue;                                     // 原版会 NRE；跳过并出声一次更安全
                string text = string.Format("+{0}", r.Quantity);
                var ctx = new ToastNotifications.Context(text, ToastNotifications.ToastNotificationType.Achievement);
                ctx.ControllerId = controllerId;                             // 原版实参 5 = GetID()
                ctx.Data = r;                                                // 原版实参 4 = `data: r`（账 662 · 2026-10-28 B93_D4 补：`Context` 已有 `object Data` 槽）
                if (!ToastNotifications.ScheduleNotification(ctx))
                    Debug.LogWarning("[RewardEvent] HandleRewardDisplay：奖励 '" + r.Id + "' 的飘窗没排进队列（壳里没有控制器）");
            }
        }
    }
}
