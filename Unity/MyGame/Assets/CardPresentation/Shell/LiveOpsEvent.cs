// LiveOpsEvent.cs —— **活动事件层**（原版 `LiveOpsEvent` / `LiveOpsEvent<TSave,TData>` / `LiveOpsEventData` /
//                   `LiveOpsEventPersistence` 的等价实现 + 我们自己的每事件存档落盘）
//
// ============================ 这是什么 / 不是什么 ============================
// 判据（唯一来源）：
//   · `资料/完全复刻/判据/泛型与启动链.md` §三 #2「活动存档层」+ §四 `LiveOpsEvent<TSave,` 的逐方法表
//   · `资料/完全复刻/判据/日常与活动.md` §二 Q1/Q2（`blockedEvents` / `sharedData` / 存档归并）
// 原版真身（反编译，机器可读）：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/LiveOpsEvent.cs`
//   `GetBaseSave():652` · `Initialize():671` · `LoadEventData():701` · `ValidateEventData():727` ·
//   `CreateNewSave():882` · `UpdateData():903` · `UpdateSaveGame():944` · `ResetEventProgress():980`；
//   类型 `LiveOpsEventData`（`EventTimeData` / `GetState` / `IsActive`）与 `LiveOpsEventPersistence` 的字段表逐字段抄自
//   同目录 `LiveOpsEventData.cs` / `EventTimeData.cs` / `LiveOpsEventPersistence.cs`。
//
// 🔴 **本类【只建框架、不接窗】**（用户/调度台口径：接窗是 C1-W3 / C3 的事）：
//   5 扇活动窗（`Ranked`/`Skirmish`/`Raid`/`ExpansionPass`/`Energy`）现在仍走 `LiveOpsLocalCatalog` 的离线词条，
//   **一行都没改**。本类提供的是「**每事件一份 TSave + 刷新/过期状态机 + 本地落盘**」这一层，
//   将来把窗里的 `LiveOpsLocalEvent` 换成 `LiveOpsEvent` 实例时，窗的参数来源不用再动第二遍。
//
// ⚠️ **与原版的三处如实差异**（⛔ 不是「已经一样」，逐条写在这里）：
//   ① **没有 Newtonsoft**（`Packages/manifest.json` 里没有 `com.unity.nuget.newtonsoft-json`）
//      ⇒ 原版 `SetupEvents(events, blockedEvents, **JObject** sharedData)` 的 `JObject` 换成我们自己的
//      `LiveOpsJson`（最小 JSON 对象，见 `LiveOpsSharedData.cs`）。**语义照抄、类型换名字**。
//   ② **原版存档走服务器**（PlayFab / CCD，`SaveEventProgress` 由子类写云）；本地没有服务器
//      ⇒ 我们的 `LocalLiveOpsEvent.SaveEventProgress` 写 `SaveCenter` 的 `SaveKeys.LiveOps` 键（**一个 key 装全部事件**，
//      每事件一份 payload）。键用的就是原版 `PlayerDataKey.LiveOps`（`SaveKeys.cs:176`，值 = `"LiveOps"`）。
//   ③ ✅ **`Initialize()` 的调度算式已于 2026-10-25 坐实**（原「两个字段读不出来」那条作废）：
//      `if (!Data.filter.hasDuration) return;` 然后 `milis = Data.filter.eventDuration(小时) * 3600000L + SaveGame.triggerTime`
//      → `Handler.TryScheduleHandlerRefresh(milis)`。判据 → `资料/完全复刻/判据/LiveOpsHandler_11方法_第2轮.md` §二 Q1
//      （VA `0x181016E70`）。⇒ 本类为此补了最小的 `LiveOpsEventFilter`（只有 `hasDuration` / `eventDuration` 两格，
//      其余 filter 字段属 C1-W2，⛔ 没搬就是没搬）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    // ============================================================ 活动状态（原版 `LiveOpState`）
    /// <summary>原版 `LiveOpState`（`LiveOpState.cs`，显式值逐个抄）：`NotAvailable=-1 · Planned=0 ·
    /// OnPreview=1 · Active=2 · Finished=3 · Expired=4`。</summary>
    public enum LiveOpState
    {
        NotAvailable = -1,
        Planned = 0,
        OnPreview = 1,
        Active = 2,
        Finished = 3,
        Expired = 4,
    }

    /// <summary>原版 `LiveOpsEventData.ActivationType`（嵌套枚举）：`Permanent=0 · OnDate=1 · OneTime=2`。</summary>
    public enum LiveOpsActivationType
    {
        Permanent = 0,
        OnDate = 1,
        OneTime = 2,
    }

    // ============================================================ 时间块（原版 `EventTimeData`）
    /// <summary>原版 `EventTimeData`（字段逐个抄：`startDate` / `endDate` / `previewable` / `previewDate` /
    /// `expirable` / `expireDate` / `eventStages`；两个派生读口 `FinalDate` / `InitialDate` 照抄）。
    /// <para>⚠️ `eventStages`（`List&lt;long&gt;`，分段活动的档位时刻）**本地 13 份 SO 全是空表**，我们仍保留字段。</para></summary>
    [Serializable]
    public class LiveOpsEventTimeData
    {
        public long startDate;
        public long endDate;
        public bool previewable;
        public long previewDate;
        public bool expirable;
        public long expireDate;
        public List<long> eventStages = new List<long>();

        /// <summary>原版 `FinalDate`：`expirable ? expireDate : endDate`。</summary>
        public long FinalDate { get { return expirable ? expireDate : endDate; } }
        /// <summary>原版 `InitialDate`：`previewable ? previewDate : startDate`。</summary>
        public long InitialDate { get { return previewable ? previewDate : startDate; } }

        /// <summary>毫秒时间戳 → UTC（原版 `SupportMethods.MillisSinceEpochToDateTime`）。</summary>
        public static DateTime FromMs(long ms) { return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime; }
        /// <summary>UTC → 毫秒时间戳（上一条的反向；`LiveOpsLocalCatalog.FromMs` 同一条换算）。</summary>
        public static long ToMs(DateTime utc) { return new DateTimeOffset(utc.ToUniversalTime()).ToUnixTimeMilliseconds(); }
    }

    // ============================================================ 事件过滤器（原版 `EventFilter`）
    /// <summary>原版 `EventFilter`（`EventFilter.cs`，`[Serializable]`）—— **字段表逐个照抄**（偏移 = 括号里那列）：
    /// `army`(+0x10) · `expansion`(+0x14) · `hasDuration`(+0x18，默认 **false**) · `eventDuration`(+0x1C，默认 **24**，单位**小时**) ·
    /// `hasCooldown`(+0x20，默认 false) · `eventCooldown`(+0x24，默认 **168**，单位**小时**) ·
    /// `eventGroups`(+0x28) · `Triggers`(+0x30) · `Conditions`(+0x38)。
    /// <para>🔴 判据（第一权威 = Ghidra 真身）：
    /// `d:/2/tools/decomp_full/Everguild.LiveOps.EventFilter__.ctor.c`
    /// （`*(this+0x1c) = 0x18`（24）· `*(this+0x24) = 0xa8`（168）· `+0x28` = `List&lt;LoadableReference&lt;EventGroup&gt;&gt;` ·
    /// `+0x30` = `DynamicList&lt;EventTrigger&gt;` · `+0x38` = `DynamicList&lt;EventCondition&gt;`）+
    /// 骨架 `out_cpp2il_dev_ilspy/Everguild.LiveOps/EventFilter.cs:25-41`；偏移表 → `判据/LiveOpsHandler_11方法_第2轮.md` §0·5。</para>
    /// <para>🆕 **2026-10-26（W8P1 / U01）本条已订正**：原来这里写「**本批只搬 `Initialize` 用得到的那两格**，
    /// `army` / `expansion` / `hasCooldown` / `eventCooldown` / `eventGroups` / `Triggers` / `Conditions` 属 C1-W2、
    /// **本批没搬**，⛔ 没搬就是没搬」——**那一批已经过去，七格现在都在**（本类就是它们）。
    /// **行为**（`Validate` / `CheckEventGroups` / `AddOrRemoveFromGroups` / `RegisterTriggers` / `CleanupTriggers` /
    /// `TryActivateEvent` / `GetExpirationTime` / `HasTriggers`）**写在 `Shell/LiveOpsFilter.cs` 的 `partial` 另一半**，
    /// ⛔ 别在别处再声明一次同名类。</para>
    /// <para>⚠️ **数据缺口的现状（如实）**：本地 13 份 SO 的 `filter.army` / `expansion` / `eventGroups` **有值**
    /// （判据 → `报告/R12_LiveOps判据落地_第2轮.md` §五·5），但**目录层没抄** ⇒ `eventGroupIds` 现在恒 `null`；
    /// `Triggers` / `Conditions` 是**抽象多态表**，`JsonUtility` 也序列化不了 ⇒ 只能由代码/自检注入。⛔ 别编数据。</para></summary>
    [Serializable]
    public partial class LiveOpsEventFilter
    {
        /// <summary>原版 `EventFilter.army`（+0x10，`CardArmy`）。⚠️ **本包没有任何读者**（原版 `Validate` /
        /// `CheckEventGroups` / `AddOrRemoveFromGroups` / `RegisterTriggers` / `TryActivateEvent` 全不读它）——
        /// 留字段是为了 `CheckEventFilter` 将来接「按阵营过滤」时不用再改一次形状。
        /// ⚠️ 类型用 `int`（= 原版枚举的值）：我们工程里有多个 `CardArmy` 同名类型/枚举，⛔ 别在这里绑死某一个。</summary>
        public int army;
        /// <summary>原版 `EventFilter.expansion`（+0x14，`CardExpansionType`）。同 `army`：**本包零读者**，`int` 存值。</summary>
        public int expansion;
        /// <summary>原版 `EventFilter.hasDuration`（+0x18）。`false` ⇒ `Initialize()` **不排期**（唯一早退条件）。</summary>
        public bool hasDuration;
        /// <summary>原版 `EventFilter.eventDuration`（+0x1C），**单位 = 小时**，默认 **24**。</summary>
        public int eventDuration = 24;
        /// <summary>原版 `EventFilter.hasCooldown`（+0x20）。`true` ⇒ `LiveOpsEvent.CanActivate()` 走冷却门
        /// （`now - save.triggerTime &lt; eventCooldown*3600000` ⇒ 拒）。</summary>
        public bool hasCooldown;
        /// <summary>原版 `EventFilter.eventCooldown`（+0x24），**单位 = 小时**，默认 **168**（`0xa8` = 一周）。
        /// 🔴 原版算式是 **32 位乘法**（`eventCooldown * 3600000`，`imul`）⇒ `eventCooldown &gt; 596` 会溢出成负数；
        /// `CanActivate` 照抄那个 32 位乘法（见 `<see cref="LiveOpsEvent.CanActivate"/>` 的注释）。</summary>
        public int eventCooldown = 168;
        /// <summary>原版 `EventFilter.eventGroups`（+0x28，`List&lt;LoadableReference&lt;EventGroup&gt;&gt;`）的**等价物**：
        /// 预计算的 `eventGroups.Select(r =&gt; r.TargetId).ToArray()` —— `LiveOpsHandler.QueueEventPopup` 用的正是这个投影
        /// （判据 = `LiveOpsHandler&lt;object&gt;$$QueueEventPopup` 的 `Enumerable.Select(... TargetId).ToArray()`）。
        /// <para>🔴 组实例由 <see cref="LiveOpsEventFilter.ResolveGroups"/> 查 <see cref="EventGroupConfig"/> 解析
        /// （`Shell/LiveOpsFilter.cs`）；解析不到 id ⇒ 出声（每 id 一次）后跳过。
        /// 数据源 = 13 份 SO 的 `filter.eventGroups`（有值）**但目录层没抄** ⇒ 现在恒 `null`，⛔ 别塞假组名。</para></summary>
        public string[] eventGroupIds;
        /// <summary>原版 `EventFilter.Triggers`（+0x30，`DynamicList&lt;EventTrigger&gt;`）：信号触发器表。
        /// 由 `RegisterTriggers` 逐条挂上总线、`CleanupTriggers` 逐条摘掉（`Shell/LiveOpsFilter.cs`）。
        /// 🔴 **原版 ctor 里就 `new` 出来了 ⇒ 永不为 null**（`EventFilter__.ctor.c` 的 `+0x30`），我们同样给初始化器。
        /// ⚠️ 抽象多态表 ⇒ **`JsonUtility` 序列化不了**（原版也不走 Json，走 Unity 序列化）⇒ 只能代码/自检注入。</summary>
        public DynamicList<LiveOpsEventTrigger> Triggers = new DynamicList<LiveOpsEventTrigger>();
        /// <summary>原版 `EventFilter.Conditions`（+0x38，`DynamicList&lt;EventCondition&gt;`，**私有**）：额外条件，
        /// `Validate` 里「组门过后逐条 `Validate(liveOps)`，任一 false 即 false」。**原版 ctor 里也是 `new` 出来的**（永不为 null）。
        /// 同 `Triggers`：POCO 装不进 Json。</summary>
        public DynamicList<EventCondition> Conditions = new DynamicList<EventCondition>();
    }

    // ============================================================ 弹窗选项（原版 `PopupOptions`）
    /// <summary>原版 `Everguild.LiveOps.PopupOptions`（字段逐个抄，偏移 = `LiveOpsHandler_11方法_第2轮.md` §0·5）：
    /// `PopupLabel`(+0x10) / `ActionRule`(+0x14) / `TriggerBeforeOnboardingEnds`(+0x18) / `MarkOnScreenClose`(+0x19，默认 **true**)。
    /// <para>13 份 SO 的两组取值（逐份相同，判据 → R14 报告 §五·3）：
    /// `TriggerPopupOptions = { PopupLabel 20, ActionRule 1, TriggerBeforeOnboardingEnds 0, MarkOnScreenClose 1 }` ·
    /// `ExpirePopupOptions  = { PopupLabel 21, 同上三项 }`。</para>
    /// <para>`PopupLabel` 是 `EventComponentReferenceType` 的一个值（原版用它去 `LiveOpsAssetUtility.GetComponentReference(ev, label)`
    /// 解析出弹窗 prefab）；我们**没有**那份 GUID→prefab 映射 ⇒ 目前只存值、由 `LiveOpsPopupPresenter` 那一跳兜底。</para></summary>
    [Serializable]
    public class PopupOptions
    {
        /// <summary>原版 `PopupOptions.PopupLabel`（+0x10）：弹窗组件引用键（13 份 SO：触发 20 / 过期 21）。</summary>
        public int PopupLabel;
        /// <summary>原版 `PopupOptions.ActionRule`（+0x14）：见 <see cref="ActionRule"/>。13 份 SO 全是 `1`。</summary>
        public ActionRule ActionRule;
        /// <summary>原版 `PopupOptions.TriggerBeforeOnboardingEnds`（+0x18）：13 份 SO 全是 `false`。</summary>
        public bool TriggerBeforeOnboardingEnds;
        /// <summary>原版 `PopupOptions.MarkOnScreenClose`（+0x19，**默认 true**）：窗口关掉时给事件打 `MarkAsSeen`。</summary>
        public bool MarkOnScreenClose = true;
    }

    // ============================================================ 事件绘制元素（原版 `EventElements`）
    /// <summary>原版 `Everguild.LiveOps.EventElements`（+0x10..+0x49）里**排期/弹窗真正用到的那半**：
    /// `ShowWindowOnTrigger`(+0x28) / `TriggerPopupOptions`(+0x30) / `ShowWindowOnExpire`(+0x38) /
    /// `ExpirePopupOptions`(+0x40) / `HideContainer`(+0x48) / `ForceRun`(+0x49)。
    /// <para>⚠️ **没搬的三格**：`EventAssets`(+0x10) / `EventComponents`(+0x18) / `EventLabels`(+0x20) —— 它们各自对应
    /// `LiveOpsLocalEvent` 的 `Assets` / `Components` / `Labels`（本地目录已有，见 `Shell/LiveOpsLocalCatalog.cs`）；
    /// 弹窗排期用不到它们，⛔ **别在这里建第二份表**（铁律 6）。</para>
    /// <para>🔴 数据源 = 13 份 SO 的 `drawData.*`，已由 `LiveOpsLocalEvent` 的 4 个同名字段承载；
    /// `LocalLiveOpsEvent.MapData` 在这里装配（4 bool + 两组 PopupOptions）。</para></summary>
    [Serializable]
    public class EventElements
    {
        /// <summary>原版 `EventElements.ShowWindowOnTrigger`（+0x28）。13 份 SO：`Parent` 事件 = true。</summary>
        public bool ShowWindowOnTrigger;
        /// <summary>原版 `EventElements.TriggerPopupOptions`（+0x30）。</summary>
        public PopupOptions TriggerPopupOptions;
        /// <summary>原版 `EventElements.ShowWindowOnExpire`（+0x38）。13 份 SO：`Scoring` 事件 = true。</summary>
        public bool ShowWindowOnExpire;
        /// <summary>原版 `EventElements.ExpirePopupOptions`（+0x40）。</summary>
        public PopupOptions ExpirePopupOptions;
        /// <summary>原版 `EventElements.HideContainer`（+0x48）。13 份 SO 全 false。</summary>
        public bool HideContainer;
        /// <summary>原版 `EventElements.ForceRun`（+0x49）。13 份 SO 全 false。`QueueEventPopup` 会传给 `MenuActions`。</summary>
        public bool ForceRun;
    }

    // ============================================================ 活动数据（原版 `LiveOpsEventData`）
    /// <summary>原版 `LiveOpsEventData : IdentifiableSO` 的**可序列化字段那半**（字段表逐个抄：
    /// `eventId` / `uploadToLive`(私有无序列化标注，默认 true) / `uploadToPreprod` / `enabled`(默认 true) /
    /// `activationType` / `dates` / `filter` / `drawData` / `priority` / `location` / `showInMainMenu` / `mainMenuOptions`）。
    /// <para>⚠️ `mainMenuOptions` 本地**没有数据源** ⇒ **留 null，不假装有**。
    /// `drawData`(`EventElements`)：🔴 **2026-10-28 B93·N2 就地订正（铁律 5）**：原来这里写「**我们没有 `EventElements` 这个类**」——
    /// **不成立**，本文件 `:172` 就定义着它（下面那段注释早于类、没跟着改）。它里面真正被 `QueueEventPopup` 读的 4 个 bool
    /// （`ForceRun` / `ShowWindowOnTrigger` / `ShowWindowOnExpire` / `HideContainer`）已**逐份照 SO 抄进** `LiveOpsLocalEvent`
    /// 的同名字段（🆕 2026-10-25 R14，数据源 = 每份 SO JSON 的 `drawData.*`）；`EventLabels` / `EventAssets` /
    /// `EventComponents` 则对应 `LiveOpsLocalEvent` 的 `Labels` / `Assets` / `Components`。
    /// ⇒ **C1-W2 接弹窗时从那里读，⛔ 别新建第二份表。**
    /// `filter` 只建了「`Initialize` 用得到的两格」那个最小等价物（见 <see cref="LiveOpsEventFilter"/>）。</para></summary>
    [Serializable]
    public partial class LiveOpsEventData
    {
        public string eventId;
        public bool uploadToLive = true;
        public bool uploadToPreprod;
        public bool enabled = true;
        public LiveOpsActivationType activationType;
        public LiveOpsEventTimeData dates;
        /// <summary>原版 `EventFilter filter`（+0x30）。原版靠 Unity 序列化保证**非 null**；
        /// 我们是 POCO，用字段初始化器给出等价物（⛔ 别让它 null —— `Initialize()` 会读 `filter.hasDuration`）。</summary>
        public LiveOpsEventFilter filter = new LiveOpsEventFilter();
        /// <summary>原版 `LiveOpsEventData.drawData`（`EventElements`，+0x38）。排期/弹窗的判据都住在这里
        /// （`ScheduleEventPopups` / `QueueEventPopup` 逐条读）。由 `LocalLiveOpsEvent.MapData` 从
        /// `LiveOpsLocalEvent` 的 4 个 bool + 两组固定 PopupOptions 装配。⚠️ 原版靠 Unity 序列化保证非 null；
        /// 我们自己造 `LiveOpsEventData` 时**要么给非 null、要么读之前判空**（`ScheduleEventPopups` 已判空并出声）。</summary>
        public EventElements drawData;
        public int priority;
        /// <summary>原版的 `EventLocation` **整体**（`{ type, parent{…} }`）我们**没有等价类**；
        /// 这里只存它的 `type` 那半（= `EventLocationType` 的 int：`Main = 0` / `None = 1` / `Parent = 2`）。
        /// 🆕 **2026-10-25（R14）**：13 份 SO 的取值已逐份抄进 `LiveOpsLocalEvent.LocationType` 并在
        /// `LocalLiveOpsEvent.MapData` 里映射；`parent` 那半（`LoadableReference.targetId`）也在
        /// `LiveOpsLocalEvent.LocationParentId`，**没有搬到这里**（原版 parent 是资产引用，我们无对应资产）。</summary>
        public int location;
        public bool showInMainMenu;

        /// <summary>原版 `LiveOpsEventData.GetID()`（`IIdentifiable`）。</summary>
        public virtual string GetID() { return eventId; }

        /// <summary>原版 `LiveOpsEventData.GetState(currentTime)`（`LiveOpsEventData.cs:96` 附近，**逐条照抄**）：
        /// <code>
        /// if (!enabled) return NotAvailable;
        /// if (activationType==Permanent || activationType==OneTime) return Active;
        /// if (FinalDate &lt; now) return Expired;
        /// if (endDate  &lt; now) return Finished;
        /// if (startDate&lt; now) return Active;
        /// return (InitialDate &lt; now) ? OnPreview : Planned;
        /// </code>
        /// 🔴 **三处是「严格小于」不是「≤」**（`dateTime &lt; currentTime`），照抄。</summary>
        public LiveOpState GetState(DateTime utcNow)
        {
            if (!enabled) return LiveOpState.NotAvailable;
            if (activationType == LiveOpsActivationType.Permanent || activationType == LiveOpsActivationType.OneTime)
                return LiveOpState.Active;
            var d = dates;
            if (d == null)
            {
                // 原版这里直接解引用 ⇒ 抛 NRE。我们**出声并降级**（游戏不该因为一份坏数据崩）。
                Debug.LogError("[LiveOps] 活动 '" + eventId + "' 是 OnDate 但没有 dates ⇒ 原版会 NRE，"
                             + "我们按 NotAvailable 处理（判据 `LiveOpsEventData.GetState`）");
                return LiveOpState.NotAvailable;
            }
            // 🔴 六档只在 `LiveOpsStateMachine.Compute` 一处实现（⛔ 别在这里再写一遍）。顺序：
            // NotAvailable → Permanent/OneTime 短路 Active → Expired(FinalDate) → Finished(endDate) → Active(startDate) → OnPreview/Planned；全严格小于。
            return LiveOpsStateMachine.Compute(enabled, activationType, d.startDate, d.endDate,
                                               d.expirable, d.expireDate, d.previewable, d.previewDate, utcNow);
        }

        /// <summary>原版 `LiveOpsEventData.IsActive(currentDatetime, includePlanned=false)`（**逐条照抄**）：
        /// <code>
        /// if (!enabled) return includePlanned;
        /// var s = GetState(now);
        /// if (!includePlanned || s != Planned) { if ((uint)(s-2) &gt; 1) return s == OnPreview; }
        /// return true;
        /// </code>
        /// 🔴 **反直觉但照抄**：`Finished(3)` 也算 active（`(uint)(3-2)=1` 不大于 1 ⇒ 落到 `return true`）；
        /// `Expired(4)` / `NotAvailable(-1)` 不算；`Planned` 只在 `includePlanned` 时算。
        /// ⚠️ 我们旧口径 `LiveOpsLocalEvent.IsInPeriod` = `start ≤ now &lt; end`，**与这条不等价**（见账里差异列）。</summary>
        public bool IsActive(DateTime utcNow, bool includePlanned = false)
        {
            // 🔴 「Finished 也算 active」那条只在 `LiveOpsStateMachine.ComputeIsActive` 一处实现：
            //   `!enabled ⇒ includePlanned`；`(includePlanned && s == Planned) ⇒ true`；
            //   `s ∈ {Active, Finished} ⇒ true`（`(uint)(s-2) > 1u` 为假 ⇒ 落到 return true）；
            //   `s == OnPreview ⇒ true`；`Expired` / `NotAvailable` ⇒ false。
            // 消费者（原版）：`ShopStore.cs:372` 用 `(now,false)`；`FixedStoreData.cs:72,139` 用 `(now,true)`。
            // ⚠️ 与 `LiveOpsLocalEvent.IsActive` 共用同一实现（`LiveOpsStateMachine.ComputeIsActive`）。
            return LiveOpsStateMachine.ComputeIsActive(enabled, includePlanned, GetState(utcNow));
        }
    }

    // ============================================================ 每事件存档（原版 `LiveOpsEventPersistence`）
    /// <summary>原版 `LiveOpsEventPersistence`（`[Serializable]`，字段逐个抄）：`eventId` / `IsNew`(默认 **true**) /
    /// `WasSeen` / `IsActive` / `triggerTime` / `expiredTime` + `virtual bool IsCompleted =&gt; false`。
    /// <para>⚠️ `JsonUtility` 不吃属性 ⇒ `IsCompleted` 只是 C# 侧读口，**不落盘**（原版也不落：它是 virtual 计算属性）。</para></summary>
    [Serializable]
    public class LiveOpsSave
    {
        public string eventId;
        public bool IsNew = true;
        public bool WasSeen;
        public bool IsActive;
        public long triggerTime;
        public long expiredTime;

        /// <summary>原版 `LiveOpsEventPersistence.IsCompleted`（virtual，基类恒 false；子类按自己的进度覆盖）。</summary>
        public virtual bool IsCompleted { get { return false; } }
    }

    // ============================================================ 事件接口（原版 `ILiveOps`）
    /// <summary>原版 `ILiveOps` 里**本批（C1-W1）用得到的那部分**。
    /// ⚠️ 原版还有 `Expire` / `SelectEvent` / `GetMainMenuData` / `OpenWindow` / `GetTimeDescriptionText` 等 ——
    /// 那些属 C1-W2（弹窗/过期）· C1-W4（主菜单容器）· C1-W5（倒计时文案），**本批不假装已实现**。</summary>
    public partial interface ILiveOps
    {
        Type SaveType { get; }
        Type DataType { get; }
        bool IsSegmented { get; }
        ILiveOpsHandler Handler { get; }
        LiveOpsEventData GetBaseData();
        LiveOpsSave GetBaseSave();
        string GetID();
        long GetEndDate(bool useExpiration = false);
        void Initialize();
        void LoadEventData(LiveOpsEventData data, LiveOpsSave saveGame, ILiveOpsHandler handler);
        void UpdateSaveGame(LiveOpsSave saveGame);
        void ResetEventProgress();
        void SaveEventProgress(Action onSuccess, Action onFail);
        bool IsActive();
        /// <summary>原版 `ILiveOps.CanActivate()`（方法表槽 **5**）：冷却门（`hasCooldown` ⇒ `now-triggerTime &lt; eventCooldown*3600000` 就拒）。
        /// 判据 = `LiveOpsEvent__CanActivate.c`。🆕 2026-10-26（W8P1）补。</summary>
        bool CanActivate();
        void Dispose();
        /// <summary>原版 `ILiveOps.RegisterEventTriggers()`（槽 **8**）：把 `data.filter.Triggers` 逐条挂上信号总线。
        /// 判据 = `LiveOpsEvent__RegisterEventTriggers.c`。🆕 2026-10-26（W8P1）补。</summary>
        void RegisterEventTriggers();
        /// <summary>原版 `ILiveOps.CheckEventFilter()`（槽 **9**）：`data.filter == null ⇒ true`，否则 `filter.Validate(this)`。
        /// 判据 = `LiveOpsEvent__CheckEventFilter.c`。🆕 2026-10-26（W8P1）补。</summary>
        bool CheckEventFilter();
        /// <summary>原版 `ILiveOps.RefreshExpire(Action callback)`（槽 **10**）：`EventState` 是 `Finished`/`Expired` ⇒
        /// `Expire(callback)`。判据 = `LiveOpsEvent__RefreshExpire.c`。🆕 2026-10-26（W8P1）补。</summary>
        void RefreshExpire(Action callback);
        /// <summary>原版 `ILiveOps.MarkAsOld()`（槽 **11**）：`IsNew ⇒ IsNew=false + WasSeen=true + 上报 0x405`。
        /// 判据 = `LiveOpsEvent__MarkAsOld.c`。🆕 2026-10-26（W8P1）补。</summary>
        void MarkAsOld();
        /// <summary>原版 `ILiveOps.OnEventTriggered()`（槽 **14**）：**基类空方法**（真身 = `LiveOpsEvent__OnEventTriggered.c`
        /// 这个共享空桩），由具体子类覆盖。`EventFilter.TryActivateEvent` 激活成功后调它。🆕 2026-10-26（W8P1）补。</summary>
        void OnEventTriggered();
        /// <summary>原版 `ILiveOps.get_EventState()`（slot 23）：`GetBaseData().GetState(CurrentServerDateTime())`。
        /// 判据 = `LiveOpsEvent__get_EventState.c`（`GetBaseData()` → `LiveOpsEventData.GetState(ServerDateTime())`）。
        /// `ScheduleEventPopups` 的过期支读它（`== LiveOpState.Finished`）。</summary>
        LiveOpState EventState { get; }
        /// <summary>原版 `ILiveOps.Expire(Action onComplete)`（slot 2）：记遥测 → `ToggleEventActivation(false, true, true)`
        /// → `onComplete?.Invoke()`。判据 = `LiveOpsEvent__Expire.c`。</summary>
        void Expire(Action onComplete);
        /// <summary>原版 `ILiveOps.MarkAsSeen()`（slot 12）：`save.WasSeen` 已是 true 就直接返回；否则置 true 并上传。
        /// 判据 = `LiveOpsEvent__MarkAsSeen.c`。</summary>
        void MarkAsSeen();
        /// <summary>原版 `ILiveOps.ToggleEventActivation(bool active, bool forceReload, bool saveOnServer)`（slot 13）。
        /// 判据 = `LiveOpsEvent__ToggleEventActivation.c`。</summary>
        void ToggleEventActivation(bool active, bool forceReload, bool saveOnServer);
    }

    // ============================================================ 非泛型基类（原版 `LiveOpsEvent`）
    /// <summary>原版 `LiveOpsEvent`（非泛型那半：`Handler` 回指 + `EventState` / `ShowInMainMenu` / `Priority`
    /// 三个从 data 派生的读口 + 抽象契约）。</summary>
    public abstract partial class LiveOpsEvent : ILiveOps
    {
        /// <summary>原版 `LiveOpsEvent.Handler`（`ILiveOpsHandler Handler { get; protected set; }`，字段 +0x10）。
        /// 事件靠它喊「我要刷新」（`Initialize` → `Handler.TryScheduleHandlerRefresh`）。</summary>
        public ILiveOpsHandler Handler { get; protected set; }

        public abstract Type SaveType { get; }
        public abstract Type DataType { get; }
        public abstract bool IsSegmented { get; }

        /// <summary>原版 `LiveOpsEvent.EventState`：`GetBaseData().GetState(CurrentServerDateTime())`。
        /// 我们的「服务器时间」= <see cref="LiveOpsClock.NowUtc"/>（可注入，自检用）。</summary>
        public LiveOpState EventState
        {
            get
            {
                var d = GetBaseData();
                return d == null ? LiveOpState.NotAvailable : d.GetState(LiveOpsClock.NowUtc);
            }
        }

        /// <summary>原版 `LiveOpsEvent.ShowInMainMenu`（virtual）：`GetBaseData().showInMainMenu`。
        /// ⚠️ 消费方是 C1-W4（主菜单活动容器），本批只给出读口。</summary>
        public virtual bool ShowInMainMenu
        {
            get { var d = GetBaseData(); return d != null && d.showInMainMenu; }
        }

        /// <summary>原版 `LiveOpsEvent.Priority`：`GetBaseData().priority`。</summary>
        public int Priority { get { var d = GetBaseData(); return d == null ? 0 : d.priority; } }

        public abstract LiveOpsEventData GetBaseData();
        public abstract LiveOpsSave GetBaseSave();
        public abstract string GetID();
        public abstract long GetEndDate(bool useExpiration = false);
        public abstract void Initialize();
        public abstract void LoadEventData(LiveOpsEventData data, LiveOpsSave saveGame, ILiveOpsHandler handler);
        public abstract void UpdateSaveGame(LiveOpsSave saveGame);
        public abstract void ResetEventProgress();

        /// <summary>原版 `SaveEventProgress(Action onSuccess, Action onFail)`（`LiveOpsEvent.cs:1005`，**virtual，基类空**）：
        /// 「子类把进度写回服务器」。基类这里什么都不做；`LiveOpsEvent&lt;TSave,TData&gt;` 保持不覆盖
        /// （它没有「写到哪儿」的知识），由**具体子类**覆盖 —— 例：`LocalLiveOpsEvent` 写 `SaveCenter`。
        /// <para>⚠️ 放在非泛型基类上（原版也是这样）：`ResetEventProgress` 的三跳里最后一跳就是它。</para></summary>
        public virtual void SaveEventProgress(Action onSuccess, Action onFail) { }

        /// <summary>原版 `LiveOpsEvent.IsActive()`（virtual，无参版）：**读存档位** `save.IsActive`。
        /// 判据 = `Everguild.LiveOps.LiveOpsEvent__IsActive.c` / `LiveOpsEvent.cs:150-165`：`OneTime && save.IsCompleted ⇒ false`，否则 `return save.IsActive`（⚠️**不是**日期窗）。</summary>
        public virtual bool IsActive()
        {
            var d = GetBaseData(); var s = GetBaseSave();
            if (d == null || s == null) { Debug.LogError("[LiveOps] 活动 '" + GetID() + "' 的 IsActive()：没有 data 或 save（原版会 NRE）⇒ 返回 false"); return false; } else return s.IsActive && !(d.activationType == LiveOpsActivationType.OneTime && s.IsCompleted);
        }

        /// <summary>原版 `LiveOpsEvent.Dispose()`（`Everguild.LiveOps.LiveOpsEvent__Dispose.c`，**逐句**）：
        /// <code>
        /// var data = GetBaseData();                        // 槽 15；null ⇒ NRE（原版无空判）
        /// if (data.filter != null) data.filter.CleanupTriggers(this);   // EventFilter.CleanupTriggers
        /// </code>
        /// 🆕 **2026-10-26（W8P1）本条已从「默认空」改成真身**：`CleanupTriggers` = ① 从**每个组**里摘掉本事件
        /// ② 逐条 `Trigger.Unregister()`（见 `Shell/LiveOpsFilter.cs`）。调用点 = `LiveOpsHandler.ClearEvents`
        /// 里的 `foreach (var e in events) e.Dispose();`（`Shell/LiveOpsHandler.cs:598` 那一带）。
        /// ⚠️ `data == null` 原版 NRE；我们出声后返回。</summary>
        public virtual void Dispose()
        {
            var d = GetBaseData();
            if (d == null)
            {
                Debug.LogError("[LiveOps] 活动 '" + (GetID() ?? "<null>") + "' 的 Dispose()：没有 data（原版会 NRE）"
                             + "⇒ 触发器/组归属都**没清理**（原版这一跳走 `filter.CleanupTriggers`）");
                return;
            }
            if (d.filter != null) d.filter.CleanupTriggers(this);
        }

        // ── 状态 / 过期 / 已读（原版 `ILiveOps` slot 2 / 12 / 13，逐条照抄）──────────────────────
        /// <summary>原版 `LiveOpsEvent.Expire(Action onComplete)`（`Everguild.LiveOps.LiveOpsEvent__Expire.c`）：
        /// <code>
        /// var id = GetID();
        /// var data = GetBaseData();
        /// var state = data.GetState(TimeHelper.ServerDateTime());
        /// PlayfabWrapper.LogActionToPlaystream(…id, state, serverTime…);   // ← 服务器遥测
        /// ToggleEventActivation(active:false, forceReload:true, saveOnServer:true);
        /// if (onComplete != null) onComplete();
        /// </code>
        /// <para>⚠️ 遥测那段（PlayFab）离线没有等价物 ⇒ **跳过并出声一次**（不静默）。
        /// ⚠️ `ToggleEventActivation` 的 `saveOnServer:true` 在离线等价于写本地存档（见该方法）。</para></summary>
        public virtual void Expire(Action onComplete)
        {
            var d = GetBaseData();
            if (d == null)
            {
                Debug.LogError("[LiveOps] 活动 '" + GetID() + "' 的 Expire()：没有 data（原版会 NRE）⇒ 仍走停用并回调");
            }
            // 原版这里 LogActionToPlaystream（服务器遥测：事件状态 / 服务器时间 / id）—— 离线无等价物。
            if (LiveOpsSignals.Verbose) Debug.Log("[LiveOps] 活动 '" + GetID() + "' 过期：遥感上报已跳过（离线）");
            ToggleEventActivation(false, true, true);
            if (onComplete != null) onComplete();
        }

        /// <summary>原版 `LiveOpsEvent.MarkAsSeen()`（`Everguild.LiveOps.LiveOpsEvent__MarkAsSeen.c`）：
        /// <code>
        /// var save = GetBaseSave();
        /// if (save.WasSeen) return;                 // 已读过 ⇒ 直接返回（不重复上报）
        /// save.WasSeen = true;
        /// if (Handler != null) PlayfabWrapper.GenericCloudScriptHandler(0x404, {id});   // ← 服务器上传
        /// </code>
        /// <para>⚠️ 有意的离线等价：原版只上传服务器；离线我们把 `WasSeen` **写回本地存档**（否则重启又变未读）。
        /// 保留 `Handler != null` 那道闸（原版 Handler 为 null 时只置位、不上报）。</para></summary>
        public virtual void MarkAsSeen()
        {
            var save = GetBaseSave();
            if (save == null)
            {
                Debug.LogError("[LiveOps] 活动 '" + GetID() + "' 的 MarkAsSeen()：没有存档（原版会 NRE）");
                return;
            }
            if (save.WasSeen) return;
            save.WasSeen = true;
            if (Handler != null) SaveEventProgress(null, null);   // 原版：GenericCloudScriptHandler(0x404) 上传
        }

        /// <summary>原版 `LiveOpsEvent.ToggleEventActivation(bool active, bool forceReload, bool saveOnServer)`
        /// （`Everguild.LiveOps.LiveOpsEvent__ToggleEventActivation.c`）：
        /// <code>
        /// save.IsActive = active;                                   // +0x1A
        /// if (!active)                            save.expiredTime = now;    // +0x28
        /// else if (save.triggerTime &lt; 1 || saveOnServer) save.triggerTime = now;   // +0x20
        /// if (data.filter != null) EventFilter.AddOrRemoveFromGroups(data.filter, this, active);
        /// if (forceReload &amp;&amp; Handler != null) Handler.RefreshHandlerLocally();     // ILiveOpsHandler slot 9
        /// if (saveOnServer) GenericCloudScriptHandler(0x403, {save, active, state, time});   // ← 服务器上传
        /// </code>
        /// <para>⚠️ `saveOnServer` 在离线等价于写本地存档（`SaveEventProgress`）。
        /// 🆕 **2026-10-26（W8P1）**：`filter.eventGroups` 那半**已接上**（组注册表现在有了 ——
        /// `EventGroup` / `EventGroupConfig` / `EventFilter.AddOrRemoveFromGroups` 在 `Shell/LiveOpsFilter.cs`）⇒
        /// 原来那句「本地没有组注册表等价物 ⇒ 组归属未更新」的出声**已删**。`eventGroupIds == null` 时
        /// `AddOrRemoveFromGroups` 自己早退（原版 `if (eventGroups == null) return;` 同义）。</para></summary>
        public virtual void ToggleEventActivation(bool active, bool forceReload, bool saveOnServer)
        {
            var save = GetBaseSave();
            if (save == null)
            {
                Debug.LogError("[LiveOps] 活动 '" + GetID() + "' 的 ToggleEventActivation()：没有存档（原版会 NRE）");
                return;
            }
            save.IsActive = active;
            long nowMs = LiveOpsEventTimeData.ToMs(LiveOpsClock.NowUtc);
            if (!active)
            {
                save.expiredTime = nowMs;
            }
            else if (save.triggerTime < 1L || saveOnServer)
            {
                save.triggerTime = nowMs;
            }
            var d = GetBaseData();
            // 原版：`if (data.filter != null) EventFilter.AddOrRemoveFromGroups(data.filter, this, active);`
            if (d != null && d.filter != null) d.filter.AddOrRemoveFromGroups(this, active);
            if (forceReload && Handler != null) Handler.RefreshHandlerLocally();
            if (saveOnServer) SaveEventProgress(null, null);   // 原版：GenericCloudScriptHandler(0x403) 上传
        }

        // ── 🆕 2026-10-26（W8P1 / U01）：`ILiveOps` 槽 5 / 8 / 9 / 10 / 11 / 14 的六个方法（**追加在类尾**）──────
        // 🔴 全部 virtual + 有实现 ⇒ 具体子类（`LocalLiveOpsEvent` 等）**不用改一行**（`Shell/LiveOpsHandler.cs` 不在本包白名单）。
        // 🔴 判据逐个写在方法头上，全部来自 Ghidra 真身（`decomp_full/Everguild.LiveOps.LiveOpsEvent__*.c`）。

        /// <summary>原版 `LiveOpsEvent.CanActivate()`（`LiveOpsEvent__CanActivate.c`，**逐句**）：
        /// <code>
        /// var data   = GetBaseData();            // 槽 15；null ⇒ NRE
        /// var filter = data.filter;              // +0x30；null ⇒ NRE
        /// if (!filter.hasCooldown) return true;  // +0x20
        /// long now   = PlayerDataManager.currentServerTime;
        /// var save   = GetBaseSave();            // 槽 16；null ⇒ NRE
        /// long elapsed = now - save.triggerTime; // save+0x20
        /// int cms    = filter.eventCooldown * 3600000;   // +0x24；★ 32 位乘法
        /// if (elapsed &lt; (int)cms) { Log(string.Format(…elapsed, cms)); return false; }
        /// return true;
        /// </code>
        /// 🔴 **两处照抄**：① 乘法在 **32 位 int** 里做（`imul eax,[filter+0x24],0x36EE80`）⇒ `eventCooldown &gt; 596` 会溢出成负数，
        /// 那时「永远冷却未到」；② 判据是**严格小于**（`elapsed == cooldown` ⇒ 放行）。
        /// ⚠️ `triggerTime == 0`（从没触发过）⇒ `elapsed` 极大 ⇒ 放行（没有「首次特判」）。
        /// ⚠️ 原版时间源是服务器时间；我们用 `LiveOpsClock.NowUtc`（可注入，自检钉死时间）。</summary>
        public virtual bool CanActivate()
        {
            var d = GetBaseData();
            if (d == null)
            {
                Debug.LogError("[LiveOps] 活动 '" + (GetID() ?? "<null>") + "' 的 CanActivate()：没有 data（原版会 NRE）⇒ 按「不可激活」处理");
                return false;
            }
            var filter = d.filter;
            if (filter == null)
            {
                Debug.LogError("[LiveOps] 活动 '" + (GetID() ?? "<null>") + "' 的 CanActivate()：`data.filter` 是 null（原版会 NRE）⇒ 按「不可激活」处理");
                return false;
            }
            if (!filter.hasCooldown) return true;                       // ★ 唯一放行早退
            var save = GetBaseSave();
            if (save == null)
            {
                Debug.LogError("[LiveOps] 活动 '" + (GetID() ?? "<null>") + "' 的 CanActivate()：没有存档（原版会 NRE）⇒ 按「不可激活」处理");
                return false;
            }
            long nowMs = LiveOpsEventTimeData.ToMs(LiveOpsClock.NowUtc);
            long elapsed = nowMs - save.triggerTime;
            int cooldownMs = unchecked(filter.eventCooldown * 3600000);   // 原版 32 位乘法（照抄，见注释 ①）
            if (elapsed < cooldownMs)
            {
                if (LiveOpsSignals.Verbose)
                    Debug.Log("[LiveOps] 活动 '" + (GetID() ?? "<null>") + "' 冷却未到：已过 " + elapsed
                            + " ms &lt; 冷却 " + cooldownMs + " ms（`eventCooldown` = " + filter.eventCooldown + " 小时）");
                return false;
            }
            return true;
        }

        /// <summary>原版 `LiveOpsEvent.CheckEventFilter()`（`LiveOpsEvent__CheckEventFilter.c`，**逐句**）：
        /// <code>
        /// var data = GetBaseData();              // 槽 15；null ⇒ NRE
        /// if (data.filter == null) return true;  // ★ filter 缺席 = 通过
        /// return data.filter.Validate(this);     // EventFilter.Validate（组门 && 条件全过）
        /// </code></summary>
        public virtual bool CheckEventFilter()
        {
            var d = GetBaseData();
            if (d == null)
            {
                Debug.LogError("[LiveOps] 活动 '" + (GetID() ?? "<null>") + "' 的 CheckEventFilter()：没有 data（原版会 NRE）⇒ 按「不过」处理");
                return false;
            }
            if (d.filter == null) return true;
            return d.filter.Validate(this);
        }

        /// <summary>原版 `LiveOpsEvent.RegisterEventTriggers()`（`LiveOpsEvent__RegisterEventTriggers.c`，**逐句**）：
        /// <code>
        /// var data = GetBaseData();                          // 槽 15；null ⇒ NRE
        /// if (data.filter != null) data.filter.RegisterTriggers(this);
        /// </code>
        /// ⇒ 把每条 `Trigger` 挂到信号总线上（信号到 ⇒ `EventFilter.TryActivateEvent(this)`）。</summary>
        public virtual void RegisterEventTriggers()
        {
            var d = GetBaseData();
            if (d == null)
            {
                Debug.LogError("[LiveOps] 活动 '" + (GetID() ?? "<null>") + "' 的 RegisterEventTriggers()：没有 data（原版会 NRE）⇒ 什么都不做");
                return;
            }
            if (d.filter != null) d.filter.RegisterTriggers(this);
        }

        /// <summary>原版 `LiveOpsEvent.RefreshExpire(Action callback)`（`LiveOpsEvent__RefreshExpire.c`，**逐句**）：
        /// <code>
        /// var closure = new &lt;&gt;c__DisplayClass38_0 { callback = callback };
        /// var data  = GetBaseData();                        // 槽 15；null ⇒ NRE
        /// var state = data.GetState(TimeHelper.ServerDateTime());
        /// if (state == Finished(3) || state == Expired(4))
        ///     this.Expire(closure.&lt;RefreshExpire&gt;b__0);     // 槽 2；lambda 体 = `callback?.Invoke()`
        /// </code>
        /// 🔴 **注意判据是 `Finished(3)` 【或】 `Expired(4)`**（与 `ScheduleEventPopups` 只认 `Finished(3)` **不同**，⛔ 别统一）。
        /// ⚠️ `state == Active/OnPreview/Planned/NotAvailable` ⇒ **什么都不做**（不排期、不回调）。
        /// ⚠️ 它**不**自己定时 —— 原版由 `LiveOpsEvent.Initialize()` 排的刷新在到点时调它（那一跳已在泛型基类落地）。</summary>
        public virtual void RefreshExpire(Action callback)
        {
            var d = GetBaseData();
            if (d == null)
            {
                Debug.LogError("[LiveOps] 活动 '" + (GetID() ?? "<null>") + "' 的 RefreshExpire()：没有 data（原版会 NRE）⇒ 不回调");
                return;
            }
            var state = d.GetState(LiveOpsClock.NowUtc);
            if (state != LiveOpState.Finished && state != LiveOpState.Expired) return;
            Expire(delegate { if (callback != null) callback(); });
        }

        /// <summary>原版 `LiveOpsEvent.MarkAsOld()`（`LiveOpsEvent__MarkAsOld.c`，**逐句**）：
        /// <code>
        /// var save = GetBaseSave();              // 槽 16；null ⇒ NRE
        /// if (!save.IsNew) return;               // +0x18
        /// save.IsNew   = false;
        /// save.WasSeen = true;                   // +0x19
        /// var id = GetID();                      // 槽 0
        /// if (Handler != null) PlayfabWrapper.GenericCloudScriptHandler(0x405, {id});   // ← 服务器上传
        /// </code>
        /// 🔴 **两处照抄**：① 判据是 `IsNew`（**不是** `WasSeen`）⇒ 第二次调用直接返回（**幂等**）；
        /// ② `MarkAsOld` 会**顺手把 `WasSeen` 也置真**（所以「旧了」必然「已读」）。
        /// ⚠️ 0x405 的上传离线等价于写本地存档（与 `MarkAsSeen` 的 0x404 同一口径，见那条注释）。</summary>
        public virtual void MarkAsOld()
        {
            var save = GetBaseSave();
            if (save == null)
            {
                Debug.LogError("[LiveOps] 活动 '" + (GetID() ?? "<null>") + "' 的 MarkAsOld()：没有存档（原版会 NRE）");
                return;
            }
            if (!save.IsNew) return;
            save.IsNew = false;
            save.WasSeen = true;
            if (Handler != null) SaveEventProgress(null, null);   // 原版：GenericCloudScriptHandler(0x405) 上传
        }

        /// <summary>原版 `LiveOpsEvent.OnEventTriggered()`（槽 **14**）：**基类空方法**
        /// （真身 = `LiveOpsEvent__OnEventTriggered.c`，内容就是「共享空桩 `ret`」那一类；子类各自覆盖，
        /// 例：`RaidEvent` 的触发后处理）。调用点 = `EventFilter.TryActivateEvent` 激活成功后。
        /// ⛔ 别在这里加逻辑 —— 它在本类就该是空的。</summary>
        public virtual void OnEventTriggered() { }

        /// <summary>原版 `LiveOpsEvent.OpenWindow()`（`LiveOpsEvent__OpenWindow.c`，**逐句**）：
        /// <code>
        /// var data  = GetBaseData();                              // 槽 15；null ⇒ NRE
        /// var state = data.GetState(TimeHelper.ServerDateTime());
        /// int label = state switch { OnPreview(1) =&gt; 20, Finished(3) =&gt; 21, _ =&gt; 10 };
        /// var reference = LiveOpsAssetUtility.GetComponentReference(this, label);
        /// if (reference == null || &lt;reference 无效&gt;) {
        ///     LogError(string.Format(…, GetID(), label));         // 槽 0 + label
        ///     return;
        /// }
        /// WindowsManager.Instance.OpenWindow(reference, this, closeAll: false, data.mainMenuOptions);
        /// </code>
        /// 🔴 **三档分派照抄**：`OnPreview ⇒ 20`（触发弹窗）· `Finished ⇒ 21`（过期弹窗）· **其余一律 10**（主窗）。
        /// 注意 `Expired(4)` / `Planned(0)` / `NotAvailable(-1)` 都落到 **10**（不是 21）—— 判据就是 `switch` 的 `default` 支。
        /// <para>⚠️ 我们这一跳的**落地缝** = `LiveOpsPopupPresenter.Open(this, options)`（与 `ShowTriggerEventPopup` /
        /// `ExpireEvent` 同一个缝，⛔ 不新开第二个）：`PopupOptions.PopupLabel` 装的就是上面那个 `label`
        /// （20/21 直接复用 `drawData.TriggerPopupOptions` / `ExpirePopupOptions` —— 13 份 SO 里这两个值 13/13 相同；
        /// 10 则就地合成一个只带 `PopupLabel=10` 的 options）。
        /// **缺的那一半**：原版还把 `data.mainMenuOptions` 传给 `WindowsManager.OpenWindow`；我们**没有**
        /// `LiveopsMainMenuData` 等价类（账 row 331/334 属 P8）⇒ 这一格**传不了**，如实记在账的差异列。</para>
        /// <para>🔴 **不挂 `MarkAsSeen`**：原版 `OpenWindow` 这一跳**没有**挂 `OnCloseWindow`（挂它的是
        /// `ShowTriggerEventPopup` 里 `PopupOptions.MarkOnScreenClose` 那一支）⇒ 这里也不挂
        /// （`drawData` 的 `PopupOptions` 只是被借来装 label，别顺手用它的 `MarkOnScreenClose`）。</para></summary>
        public virtual void OpenWindow()
        {
            var d = GetBaseData();
            if (d == null)
            {
                Debug.LogError("[LiveOps] 活动 '" + (GetID() ?? "<null>") + "' 的 OpenWindow()：没有 data（原版会 NRE）⇒ 不开窗");
                return;
            }
            var state = d.GetState(LiveOpsClock.NowUtc);
            int label;
            PopupOptions opts = null;
            switch (state)
            {
                case LiveOpState.OnPreview:
                    label = 20;                                                     // = TriggerPopupOptions.PopupLabel
                    if (d.drawData != null) opts = d.drawData.TriggerPopupOptions;
                    break;
                case LiveOpState.Finished:
                    label = 21;                                                     // = ExpirePopupOptions.PopupLabel
                    if (d.drawData != null) opts = d.drawData.ExpirePopupOptions;
                    break;
                default:
                    label = 10;                                                     // MainWindow
                    break;
            }
            if (opts == null) opts = new PopupOptions { PopupLabel = label };
            GameWindow win = LiveOpsPopupPresenter.Open != null ? LiveOpsPopupPresenter.Open(this, opts) : null;
            if (win == null)
            {
                // 原版这一跳在「GetComponentReference 拿不到 / 引用无效」时 LogError 后 return（不开窗）。同义。
                Debug.LogError("[LiveOps] 活动 '" + (GetID() ?? "<null>") + "' 的 OpenWindow()：label=" + label
                             + " 的组件引用解析不到（⚠️ 2026-10-27 更正：GUID→prefab 名本地**能解**，解析表在 `LiveOpsLocalBoot`；走到这里 = 该事件**确实没有这个 label 的组件**，或它的 prefab 不在本地表）"
                             + "或 `LiveOpsPopupPresenter` 没装 ⇒ **不开窗**（原版此支 = `CustomDebug.LogError` 后 return）");
            }
        }
    }

    // ============================================================ 泛型基类（原版 `LiveOpsEvent<TSave,TData>`）
    /// <summary>原版 `LiveOpsEvent&lt;TSave,TData&gt;` —— **每个活动一份存档类型**（`TSave`）**和一份数据类型**（`TData`）。
    /// <para>状态在 `Data` / `SaveGame` 两个字段上（原版字段偏移 +0x18 / +0x20，与 `GetBaseData()/GetBaseSave()` 一致）。</para></summary>
    public abstract class LiveOpsEvent<TSave, TData> : LiveOpsEvent
        where TSave : LiveOpsSave
        where TData : LiveOpsEventData
    {
        /// <summary>本事件的数据（原版 `Data` 字段，protected setter）。</summary>
        public TData Data { get; protected set; }
        /// <summary>本事件的存档（原版 `SaveGame` 字段，protected setter）。**这是「进度」的唯一正本**。</summary>
        public TSave SaveGame { get; protected set; }

        public override Type SaveType { get { return typeof(TSave); } }
        public override Type DataType { get { return typeof(TData); } }

        /// <summary>原版 `IsSegmented`：`dates.eventStages` 非空 ⇒ 这是一个**分段**活动（多档进度）。
        /// 判据 = `LiveOpsEvent.cs:565`（读 `Data.dates.eventStages` 的 Count）。</summary>
        public override bool IsSegmented
        {
            get { return Data != null && Data.dates != null && Data.dates.eventStages != null && Data.dates.eventStages.Count > 0; }
        }

        /// <summary>原版 `GetID()`（`LiveOpsEvent.cs:631`）：`Data == null ? null : Data.eventId`。</summary>
        public override string GetID() { return Data == null ? null : Data.GetID(); }

        /// <summary>原版 `GetBaseData()`（`:645`）。</summary>
        public override LiveOpsEventData GetBaseData() { return Data; }

        /// <summary>原版 `GetBaseSave()`（`:652`）。</summary>
        public override LiveOpsSave GetBaseSave() { return SaveGame; }

        /// <summary>原版 `GetEndDate(bool useExpiration)`（`:1009`）：`useExpiration ? expireDate : FinalDate`（毫秒）。
        /// ⚠️ 已知缺口：原版这一跳还夹了 cooldown 的 `Math.Min`（账 row 330 的差异列写着）——
        /// cooldown 在 `EventFilter` 那一层，本地无 filter 数据 ⇒ **本批不含**，如实记 B。</summary>
        public override long GetEndDate(bool useExpiration = false)
        {
            var d = Data == null ? null : Data.dates;
            if (d == null) return 0L;
            return useExpiration ? d.expireDate : d.FinalDate;
        }

        /// <summary>原版 `Initialize()`（`:671` / VA `0x181016E70`）—— **2026-10-25 已全部坐实**（逐条照抄）：
        /// <code>
        /// if (!Data.filter.hasDuration) return;                                  // ★ 唯一早退条件（时长型活动才排期）
        /// long milis = (long)Data.filter.eventDuration * 3600000L + SaveGame.triggerTime;
        /// Handler.TryScheduleHandlerRefresh(milis);
        /// </code>
        /// <para>🔴 **判据**（`资料/完全复刻/判据/LiveOpsHandler_11方法_第2轮.md` §二 Q1）：
        /// `0x181016EAF cmp byte ptr [filter+0x18],0`（`hasDuration`）· `0x181016ED7 imul eax,[filter+0x1C],0x36EE80`
        /// （`eventDuration` × 3,600,000 ⇒ **单位是小时**）· `0x181016EF8 add rdi,[save+0x20]`（`SaveGame.triggerTime`）。
        /// 同一算式在 `EventFilter.GetExpirationTime(ILiveOps)` 里**逐字重复** ⇒ 全工程「到期/下次刷新时刻」只有这一条算式。</para>
        /// <para>⚠️ 原文件头差异 ③ 写的「两个字段读不出、按 `SaveGame.triggerTime` 近似」**已作废**（本条已坐实并改掉）。
        /// ⚠️ **有意差异**：原版 `Data` / `Data.filter` / `SaveGame` / `Handler` 任一为 null ⇒ 隐式 **NRE**；
        /// 我们逐个出声后跳过（坏数据不让游戏崩）。本地 13 份 SO 的 `hasDuration` **全是 0** ⇒ 实际**不排期**（原版同义）。</para></summary>
        public override void Initialize()
        {
            if (Data == null || SaveGame == null) return;
            if (Data.filter == null || !Data.filter.hasDuration) return;   // ★ 原版唯一早退条件
            if (Handler == null)
            {
                Debug.LogWarning("[LiveOps] 活动 '" + GetID() + "' 的 Initialize()：没有 handler ⇒ 无法调度刷新"
                               + "（原版这一跳是 `Handler.TryScheduleHandlerRefresh`；事件应由工厂带着 handler 造出来）");
                return;
            }
            long milis = (long)Data.filter.eventDuration * 3600000L + SaveGame.triggerTime;
            Handler.TryScheduleHandlerRefresh(milis);
        }

        /// <summary>原版 `LoadEventData(data, saveGame, handler)`（`:701`，**逐条照抄**）：
        /// <code>
        /// Data     = (TData)data;      // 原版是 isinst 强转，失败走 throw
        /// SaveGame = (TSave)saveGame;
        /// Handler  = handler;
        /// if (Handler != null) Handler.UpdateCachedSave(SaveGame);   // ← handler 侧缓存回写
        /// </code>
        /// ⚠️ 强转失败：原版**抛**（`il2cpp_vm_object_is_inst` 之后 `throw`）。我们**出声 + 不写字段**，
        /// 理由同 `GetState`：坏数据不该让游戏崩。这是**有意差异**，写在账的差异列。</summary>
        public override void LoadEventData(LiveOpsEventData data, LiveOpsSave saveGame, ILiveOpsHandler handler)
        {
            var d = data as TData;
            if (d == null)
            {
                Debug.LogError("[LiveOps] LoadEventData：data 不是 " + typeof(TData).Name
                             + "（实际 " + (data == null ? "<null>" : data.GetType().Name) + "）⇒ 原版会抛 InvalidCastException，"
                             + "我们出声并放弃本次装载。eventId = " + (data == null ? "<null>" : data.eventId));
                return;
            }
            var s = saveGame as TSave;
            if (saveGame != null && s == null)
            {
                Debug.LogError("[LiveOps] LoadEventData：saveGame 不是 " + typeof(TSave).Name
                             + "（实际 " + saveGame.GetType().Name + "）⇒ 原版会抛，我们出声并把存档当「没有」重建。");
            }
            Data = d;
            SaveGame = s;
            Handler = handler;
            if (Handler != null) Handler.UpdateCachedSave(SaveGame);
        }

        /// <summary>原版 `ValidateEventData(data, save, out formatedData, out formatedSave)`（`:727`，**逐条照抄**）：
        /// <code>
        /// var d = (TData)data; if (d == null) return false;      // 类型不对 ⇒ false（不抛）
        /// formatedData = d;
        /// if (save != null) { var s = (TSave)save; if (s != null &amp;&amp; Validate(s)) { formatedSave = s; return true; } }
        /// formatedSave = CreateNewSave(formatedData);             // 没有 / 校验不过 ⇒ 新建
        /// return true;
        /// </code>
        /// ⚠️ 「`save` 有值但类型不对」原版是 `isinst` 得到 null 后**落到 CreateNewSave**（不是 false），照抄。</summary>
        public bool ValidateEventData(LiveOpsEventData data, LiveOpsSave save,
                                      out TData formatedData, out TSave formatedSave)
        {
            formatedData = null;
            formatedSave = null;
            var d = data as TData;
            if (d == null) return false;
            formatedData = d;
            if (save != null)
            {
                var s = save as TSave;
                if (s != null && Validate(s)) { formatedSave = s; return true; }
            }
            formatedSave = CreateNewSave(formatedData);
            return true;
        }

        /// <summary>原版 `Validate(TSave save)`（`virtual`，`:877`）：基类恒 true，子类按自己的存档结构判。</summary>
        protected virtual bool Validate(TSave save) { return true; }

        /// <summary>原版 `CreateNewSave(TData data)`（`:882` / VA `0x181016CA0`）—— **2026-10-25 已坐实**（逐条照抄）：
        /// <code>
        /// TSave save = Activator.CreateInstance&lt;TSave&gt;();   // 默认构造 ⇒ 字段初始化器 IsNew = true 已生效
        /// save.eventId = data.GetID();                      // ← `GetID()` 实体 = `return [this+0x18]`（= LiveOpsEventData.eventId）
        /// return save;                                      // 其余（WasSeen/IsActive/triggerTime/expiredTime）全是默认值
        /// </code>
        /// 🔴 **判据**（`资料/完全复刻/判据/LiveOpsHandler_11方法_第2轮.md` §二 Q2）：`0x181016CBC Activator.CreateInstance`
        /// → `0x181016CCF/0x181016CDD` 取 TData 的虚方法（= `GetID()`）→ `0x181016CEC mov [rcx], rax` 写 `+0x10`（`eventId`）。
        /// <para>🔴 **`triggerTime` 不在这里写**（新存档 = 0）；写它的是 `RefreshExpire` / `UpdateSaveGame` 那一路。
        /// ⛔ 别把 `dates.startDate` 塞进来 —— 那是旧的「未坐实」猜测，已被反汇编否掉。</para>
        /// <para>⚠️ 冗余但保留：`s.IsNew = true` 与字段初始化器重复（原版只靠初始化器）；写出来是为了「读代码就能看出初值」。</para></summary>
        public TSave CreateNewSave(TData data)
        {
            var s = Activator.CreateInstance<TSave>();
            if (s == null)
            {
                Debug.LogError("[LiveOps] CreateNewSave：Activator 返回 null（" + typeof(TSave).Name + " 没有无参构造？）");
                return null;
            }
            s.eventId = data == null ? null : data.eventId;
            s.IsNew = true;
            return s;
        }

        /// <summary>原版 `UpdateData(LiveOpsEventData data)`（`:903` / VA `0x181017250`）：「换一份 data」。
        /// <code>
        /// if (data.GetType() != typeof(TData)) return;    // ★ 【类型必须完全相等】—— 子类也会被**忽略**
        /// Data = (TData)data;
        /// </code>
        /// 🔴 **不是 `as TData`**（判据 → §三 #9）：`0x181017250` 是 `GetType()` → `Type.op_Inequality(typeof(TData))`
        /// → `jne &lt;return&gt;` ⇒ 用 `as` 会把**子类当成合法**，与原文口径不同。
        /// <para>⚠️ 有意差异：原版 `data == null` ⇒ `GetType()` 上 **NRE**，我们出声后返回。</para></summary>
        public void UpdateData(LiveOpsEventData data)
        {
            if (data == null || data.GetType() != typeof(TData))
            {
                Debug.LogError("[LiveOps] UpdateData：data 的类型必须**恰好**是 " + typeof(TData).Name
                             + "（实际 " + (data == null ? "<null>" : data.GetType().Name) + "）"
                             + "⇒ 原版会 NRE 或直接 return（⛔ 子类也不接受），我们忽略本次更新（旧 data 保留）");
                return;
            }
            Data = (TData)data;
        }

        /// <summary>原版 `UpdateSaveGame(LiveOpsEventPersistence saveGame)`（`:944`，**逐条照抄**）：
        /// <code>
        /// var s = saveGame as TSave;
        /// if (s != null) { SaveGame = s; LoadEventData(Data, SaveGame, Handler); }
        /// </code>
        /// ⇒ 收到新存档后**重走一遍 LoadEventData**（这样 handler 侧缓存也跟着刷新）。</summary>
        public override void UpdateSaveGame(LiveOpsSave saveGame)
        {
            var s = saveGame as TSave;
            if (s == null)
            {
                Debug.LogError("[LiveOps] UpdateSaveGame：saveGame 不是 " + typeof(TSave).Name + " ⇒ 原版会静默跳过，我们同样不写（并出声）");
                return;
            }
            SaveGame = s;
            LoadEventData(Data, SaveGame, Handler);
        }

        /// <summary>原版 `ResetEventProgress()`（`:980`，**三跳照抄**）：
        /// `new save`（= `CreateNewSave(Data)`）→ `LoadEventData(Data, newSave, Handler)` → `SaveEventProgress(...)`。</summary>
        public override void ResetEventProgress()
        {
            if (Data == null)
            {
                Debug.LogError("[LiveOps] ResetEventProgress：没有 Data ⇒ 无法重建存档（原版同样会 NRE）");
                return;
            }
            var fresh = CreateNewSave(Data);
            LoadEventData(Data, fresh, Handler);
            SaveEventProgress(null, null);
        }

        /// <summary>原版 `SaveEventProgress(Action, Action)` 在泛型层**不覆盖** —— 「写到哪儿」由具体子类决定
        /// （见非泛型基类上那条注释）。⚠️ 别在这里加 override，那会让具体子类再覆盖时多一跳且没有语义。</summary>
    }

    // ============================================================ 存档落盘（我们的，原版没有这一层）
    /// <summary>一个 key 装全部活动存档（原版是「一个活动一份 server persistence」；本地归并成一个文件）。
    /// <para>🔴 **key = `SaveKeys.LiveOps`（`"LiveOps"`）** —— 那是原版 `PlayerDataKey` 第 41 项
    /// （`SaveKeys.cs:176` 的注释就写着「原版 LiveOps。【我们】空缺」）。**本批把它填上了**，
    /// 将来 E3 的存档归并 Step 4 直接就能接管（同一个 key，不用再迁一次）。</para></summary>
    [Serializable]
    public sealed class LiveOpsSaveBlob
    {
        public int ver = 1;
        public List<LiveOpsSaveEntry> events = new List<LiveOpsSaveEntry>();
    }

    /// <summary>一条事件存档。`json` = 该事件 `TSave` 的一份 JSON（类型在编译期未知 ⇒ 只能存串）。</summary>
    [Serializable]
    public sealed class LiveOpsSaveEntry
    {
        public string eventId;
        public string json;
    }

    /// <summary>活动存档的**唯一读写口**（⛔ 别在别处再开一份 json 文件 —— `判据/日常与活动.md` §二 Q2 记的
    /// 「6+ 份各自为政」就是那么来的）。</summary>
    public static class LiveOpsSaveStore
    {
        /// <summary>存档键（原版 `PlayerDataKey.LiveOps`）。</summary>
        public const string Key = SaveKeys.LiveOps;

        /// <summary>上一次读/写失败的原因（成功时 null）。日志之外也能被自检读。</summary>
        public static string LastError { get; private set; }

        /// <summary>读一个事件的存档。没有 / 坏档 ⇒ `null`（**⛔ 不静默给默认值**：调用方要能区分
        /// 「第一次跑」和「档丢了」，与原版 `No player data found` 同一条口径）。</summary>
        public static LiveOpsSave Load(string eventId, Type saveType)
        {
            LastError = null;
            if (string.IsNullOrEmpty(eventId)) { LastError = "eventId 是空的"; return null; }
            if (saveType == null) { LastError = "saveType 是 null"; return null; }
            var blob = SaveCenter.Get<LiveOpsSaveBlob>(Key);
            if (blob == null || blob.events == null) return null;      // 第一次跑（SaveCenter 已经出过声）
            for (int i = 0; i < blob.events.Count; i++)
            {
                var e = blob.events[i];
                if (e == null || e.eventId != eventId) continue;
                if (string.IsNullOrEmpty(e.json)) { LastError = "'" + eventId + "' 的存档里 json 是空的"; return null; }
                try { return (LiveOpsSave)JsonUtility.FromJson(e.json, saveType); }
                catch (Exception ex)
                {
                    LastError = "反序列化 '" + eventId + "' 失败：" + ex.Message;
                    Debug.LogError("[LiveOps] " + LastError + " ⇒ 返回 null，**文件没动**");
                    return null;
                }
            }
            return null;
        }

        /// <summary>写一个事件的存档（整份 blob 覆盖写）。返回是否成功。</summary>
        public static bool Save(LiveOpsSave save)
        {
            LastError = null;
            if (save == null) { LastError = "save 是 null"; return false; }
            if (string.IsNullOrEmpty(save.eventId)) { LastError = "save.eventId 是空的"; return false; }
            string json;
            try { json = JsonUtility.ToJson(save); }
            catch (Exception ex) { LastError = "序列化 '" + save.eventId + "' 失败：" + ex.Message; return false; }

            var blob = SaveCenter.Get<LiveOpsSaveBlob>(Key) ?? new LiveOpsSaveBlob();
            if (blob.events == null) blob.events = new List<LiveOpsSaveEntry>();
            LiveOpsSaveEntry slot = null;
            for (int i = 0; i < blob.events.Count; i++)
                if (blob.events[i] != null && blob.events[i].eventId == save.eventId) { slot = blob.events[i]; break; }
            if (slot == null) { slot = new LiveOpsSaveEntry { eventId = save.eventId }; blob.events.Add(slot); }
            slot.json = json;
            bool ok = SaveCenter.Set(Key, blob);
            if (!ok) LastError = SaveCenter.LastWriteError;
            return ok;
        }

        /// <summary>自检口：把本键整个删掉（⚠️ 只给断言用；⛔ 正式代码别调）。</summary>
        public static void ResetForTest()
        {
            LastError = null;
            SaveCenter.Delete(Key);
        }
    }
}

// ============================================================ 活动基类 · 主菜单 / 容器 / 倒计时（B87-C1 · 2026-10-27）
// 🔴 本段是 `LiveOpsEvent` / `ILiveOps` 的**第二个 partial 段**，追加在文件末尾：
//    **不改上半段任何行号**（账按 `文件:行号` 引用，插在类中间会让下游整体漂移 —— CLAUDE.md 那条纪律）。
//
// 判据（唯一来源）：
//   · `资料/完全复刻/判据/泛型与启动链.md` §四 `LiveOpsEvent<TSave,`（存档层那 9 条住在上半段，本段不含）
//   · `资料/完全复刻/判据/B9_日常C_施工单.tsv` —— 日常_奖励里属于本类的另外 6 条：
//     `GetContainer` / `ReleaseContainer` / `GetEventPriorityForMainMenu` / `GetMainMenuData` /
//     `GetTimeDescriptionText` / `GetTimeDescriptionTextKey`
//   · 原版真身：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/LiveOpsEvent.cs`
//     `:97`(GetMainMenuData) `:169`(GetContainer) `:179`(ReleaseContainer) `:444`(GetTimeDescriptionText)
//     `:480`(GetTimeDescriptionTextKey) `:508`(GetEventPriorityForMainMenu)
//   · `GetTimeDescriptionTextKey` 的 switch 三档 = `d:/2/tools/decomp_full/Everguild.LiveOps.LiveOpsEvent__GetTimeDescriptionTextKey.c`
//     （`case 1 ⇒ DAT_1842aaa88` / `case 2 ⇒ DAT_1842aa898` / `case 3 ⇒ DAT_1842aa990` / 其余 ⇒ 空串）
namespace CardPresentation
{
    /// <summary>原版 `LiveopsMainMenuData`（`GetMainMenuData()` 的产物）—— 只装三格：
    /// 事件本体 / `priority` / 组件引用类型（`EventComponentReferenceType.MainMenuContainer = 35`）。
    /// <para>判据 = `LiveOpsEvent.cs:97`：`new LiveopsMainMenuData(this, baseData.priority, EventComponentReferenceType.MainMenuContainer)`。
    /// 原版消费方 = `WindowsManager.OpenWindow(reference, this, …, data.mainMenuOptions)`（`OpenWindow()` 那一跳）。</para>
    /// <para>⚠️ 差异：原版这一格还牵着「主菜单容器 prefab 引用」（`MainMenuContainer` = 35 号的
    /// `EventComponents` 引用）；⚠️ 2026-10-27 更正：原来这里写「我们只解到 GUID、没解到 prefab 名」——**不成立**，GUID→prefab 名本地能解（`LiveOpsLocalCatalog.cs` 头部 + `判据/B88_P1两条判据.md` §3.3）
    /// ⇒ 这里只留**引用类型号**，没有可解析的 prefab。
    /// 🔶 自己设计（非原版）：把「引用类型号」存成 `int ComponentKey` 而不是 `EventComponentReferenceType` 枚举，
    /// 是因为那个枚举在我们仓库里叫 `LiveOpsComponentRef`（`LiveOpsLocalCatalog.cs:136`，逐值照官方枚举）
    /// 且它在 `Shell/LiveOpsLocalCatalog.cs` 里 —— 本批的独占文件不含它，⛔ 不跨文件加依赖。
    /// 调度台 C1 2026-10-27 定。</para></summary>
    public sealed class LiveopsMainMenuData
    {
        public ILiveOps Event;
        public int Priority;
        /// <summary>`LiveOpsComponentRef.MainMenuContainer` = **35**。</summary>
        public int ComponentKey;
        public LiveopsMainMenuData(ILiveOps ev, int priority, int componentKey)
        {
            Event = ev; Priority = priority; ComponentKey = componentKey;
        }
    }

    /// <summary>原版 `IUiContainer`（`GetContainer()` 的返回类型）。
    /// 🔶 自己设计（非原版）：`UiContainer` 整族（`IUiContainer` / `UiContainer&lt;T&gt;` / `ContainerBuilder&lt;T&gt;`）
    /// 在我们仓库里**还是 C**（属别的批：*容器/窗口*线），本批的独占文件里没有它们 ⇒
    /// 这里只留一个「拿到的是什么」的最小读口（`ComponentId` = `EventComponents` 的 GUID）。
    /// ⛔ 别把它当掉 `IUiContainer` 的复刻。调度台 C1 2026-10-27 定。</summary>
    public interface IUiContainer
    {
        /// <summary>这个容器的来源引用（原版 `LoadableReference.targetId`；我们 = `EventComponents` 的 GUID）。</summary>
        string ComponentId { get; }
    }

    /// <summary>「按 `Container`（组件引用类型 0）装载/卸载 UiContainer」那两条原版调用
    /// （`LiveOpsAssetUtility.LoadComponent&lt;UiContainer&gt;(data, Container)` / `LiveOpsAssetUtility.Unload(data, Container)`）
    /// 的注入点。
    /// <para>🔶 自己设计（非原版）：`LiveOpsAssetUtility` 在原生仓库里没有等价物（我们只有
    /// `LiveOpsPopupPresenter` 那一个开窗口），而容器线不属本批 ⇒ 留注入点 + **没装就出声**。
    /// 调度台 C1 2026-10-27 定。</para></summary>
    public static partial class LiveOpsContainers   // partial：`ReleaseEventAssets` 那半已于 2026-10-29（B94_T1）搬去 `LiveOpsAssetUtility`（见文件尾注记）
    {
        /// <summary>参数 = (事件, 组件引用类型)。返回 null = 装载失败（原版 `LoadComponent` 拿不到引用时也返回 null）。</summary>
        public static Func<ILiveOps, int, IUiContainer> Load = LiveOpsAssetUtility.LoadContainer;   // 🔴 2026-10-29（B95_RD）：**默认已接真实现**（原版 `LoadComponent<T>` 那份），不再是 null ⇒ 原样返回 null 只是「装不出来」，⛔ 不是「没接」
        /// <summary>参数 = (事件, 组件引用类型)。原版 `Unload` 在没装过时是 no-op。</summary>
        public static Action<ILiveOps, int> Unload = LiveOpsAssetUtility.UnloadContainer;

        /// <summary>`LiveOpsEventData` 那一层的那一对（原版两层各有一份，见 `LiveOpsEventData.cs:162-170`）。</summary>
        public static Func<LiveOpsEventData, int, IUiContainer> LoadForData = LiveOpsAssetUtility.LoadContainerForData;
        public static Action<LiveOpsEventData, int> UnloadForData = LiveOpsAssetUtility.UnloadContainerForData;

        /// <summary>`LiveOpsComponentRef.Container`（判据 `LiveOpsLocalCatalog.cs:139`：`Container = 0`）。</summary>
        public const int ComponentContainer = 0;
        /// <summary>`LiveOpsComponentRef.MainMenuContainer`（同上：**35**）。</summary>
        public const int ComponentMainMenuContainer = 35;

        /// <summary>自检口（⚠️ 只给断言用）。</summary>
        public static void ResetForTest()
        {
            Load = LiveOpsAssetUtility.LoadContainer; Unload = LiveOpsAssetUtility.UnloadContainer; LoadForData = LiveOpsAssetUtility.LoadContainerForData; UnloadForData = LiveOpsAssetUtility.UnloadContainerForData; LiveOpsAssetUtility.ResetCacheForTest();   // 🔴 2026-10-29（B95_RD）：还原成**默认真实现**（原来置 null ⇒ 断言里「没接」与「装不出来」混成一档）；要「没接」请显式赋 null
        }
    }

    // ── 接口那半：原版 `ILiveOps` 有这 6 个槽，本段补齐（抽象类给虚实现 ⇒ 现有实现者零改动）
    public partial interface ILiveOps
    {
        /// <summary>原版 `ILiveOps.GetMainMenuData()`（槽 17 一族）。</summary>
        LiveopsMainMenuData GetMainMenuData();
        /// <summary>原版 `ILiveOps.GetEventPriorityForMainMenu()`。</summary>
        int GetEventPriorityForMainMenu();
        /// <summary>原版 `ILiveOps.GetContainer()`。</summary>
        IUiContainer GetContainer();
        /// <summary>原版 `ILiveOps.ReleaseContainer()`。</summary>
        void ReleaseContainer();
        /// <summary>原版 `ILiveOps.GetTimeDescriptionText()`：状态 → 已本地化的倒计时文案。</summary>
        string GetTimeDescriptionText();
        /// <summary>原版 `ILiveOps.GetTimeDescriptionTextKey()`：状态 → 词条键（空串 = 这一档不显示）。</summary>
        string GetTimeDescriptionTextKey();
    }

    // ── 类那半：6 个方法的实现
    public abstract partial class LiveOpsEvent
    {
        /// <summary>原版 `GetEventPriorityForMainMenu()`（`LiveOpsEvent.cs:508`）：
        /// <code>return GetBaseData().priority;</code>
        /// （`data == null` ⇒ 原版 NRE；我们出声 + 返回 0。）
        /// <para>⚠️ 与上半段的 <see cref="Priority"/> 属性**同一格**（都读 `data.priority`）—— 保留两个名字是因为
        /// 原版就有这两个入口（`Priority` 属性 / `GetEventPriorityForMainMenu()` 方法）。⛔ 别让它们读不同的字段。</para></summary>
        public int GetEventPriorityForMainMenu()
        {
            var d = GetBaseData();
            if (d == null)
            {
                Debug.LogError("[LiveOps] GetEventPriorityForMainMenu：活动 '" + (GetID() ?? "<null>")
                             + "' 没有 data（原版 NRE）⇒ 返回 0");
                return 0;
            }
            return d.priority;
        }

        /// <summary>原版 `GetMainMenuData()`（`LiveOpsEvent.cs:97`）：
        /// <code>var d = GetBaseData(); return new LiveopsMainMenuData(this, d.priority, MainMenuContainer(35));</code>
        /// <para>⚠️ 差异：原版这一跳还把 `data.mainMenuOptions`（`PopupOptions` 一族）带出去，供
        /// `WindowsManager.OpenWindow` 用；我们的 `OpenWindow()` 走 `LiveOpsPopupPresenter` 那条缝
        /// （见上半段 `OpenWindow()` 的注释），这一格**没搬**。</para></summary>
        public virtual LiveopsMainMenuData GetMainMenuData()
        {
            var d = GetBaseData();
            if (d == null)
            {
                Debug.LogError("[LiveOps] GetMainMenuData：活动 '" + (GetID() ?? "<null>") + "' 没有 data（原版 NRE）⇒ 返回 null");
                return null;
            }
            return new LiveopsMainMenuData(this, d.priority, LiveOpsContainers.ComponentMainMenuContainer);
        }

        /// <summary>原版 `GetContainer()`（`LiveOpsEvent.cs:169`）：
        /// <code>return LiveOpsAssetUtility.LoadComponent&lt;UiContainer&gt;(GetBaseData(), Container /*0*/);</code>
        /// <para>⚠️ 差异：`LiveOpsAssetUtility` 我们没有 ⇒ 走 <see cref="LiveOpsContainers.Load"/> 注入点；
        /// 没装 ⇒ **出声并返回 null**（原版拿不到引用时也是 null，但**不**打这条日志 —— 我们的更吵，如实记）。</para></summary>
        public virtual IUiContainer GetContainer()
        {
            var d = GetBaseData();
            if (d == null)
            {
                Debug.LogError("[LiveOps] GetContainer：活动 '" + (GetID() ?? "<null>") + "' 没有 data（原版 NRE）⇒ null");
                return null;
            }
            if (LiveOpsContainers.Load == null)
            {
                Debug.LogError("[LiveOps] GetContainer：没有接 LiveOpsContainers.Load（原版 = "
                             + "LiveOpsAssetUtility.LoadComponent<UiContainer>(data, Container)）⇒ 返回 null。"
                             + " eventId = " + (GetID() ?? "<null>"));
                return null;
            }
            return LiveOpsContainers.Load(this, LiveOpsContainers.ComponentContainer);
        }

        /// <summary>原版 `ReleaseContainer()`（`LiveOpsEvent.cs:179`）：
        /// <code>LiveOpsAssetUtility.Unload(GetBaseData(), Container);</code>
        /// ⚠️ 差异同 <see cref="GetContainer"/>（注入点）；没装时**只记日志**（原版没装过时本来就是 no-op）。</summary>
        public virtual void ReleaseContainer()
        {
            var d = GetBaseData();
            if (d == null) return;                       // 原版这一跳对 null data 调 Unload（内部判空），语义等价
            if (LiveOpsContainers.Unload == null)
            {
                Debug.Log("[LiveOps] ReleaseContainer：没有接 LiveOpsContainers.Unload ⇒ 无容器可卸"
                        + "（原版 = LiveOpsAssetUtility.Unload(data, Container)，没装过时同样是 no-op）");
                return;
            }
            LiveOpsContainers.Unload(this, LiveOpsContainers.ComponentContainer);
        }

        /// <summary>原版 `GetTimeDescriptionTextKey()`（`LiveOpsEvent.cs:480`；switch 三档的**机器码**见
        /// `decomp_full/Everguild.LiveOps.LiveOpsEvent__GetTimeDescriptionTextKey.c`）：
        /// <code>
        /// var d = GetBaseData();
        /// switch (d.GetState(TimeHelper.ServerDateTime)) {
        ///     case OnPreview(1): return &lt;键 A&gt;;      // DAT_1842aaa88
        ///     case Active(2):    return &lt;键 B&gt;;      // DAT_1842aa898
        ///     case Finished(3):  return &lt;键 C&gt;;      // DAT_1842aa990
        ///     default:           return "";           // DAT_1842b80e0（空串）
        /// }
        /// </code>
        /// <para>🔴 **2026-10-28 B93·N2 已【坐实】键名**（原来是按语义推断的，现改为**字面量直读**）：那四个 `DAT_` 的地址
        /// 在 `d:/2/tools/il2cpp_out/stringliteral.json` 里逐条可查 —— 它的 `address` 字段就是 `0x42AAA88` 这种，
        /// 与 Ghidra 的 `DAT_1842aaa88` **同一地址**（⚠️ 别再说「stringliteral.json 不带地址」，那是错的）⇒
        /// `0x42AAA88 = "Event_Description/General/TimeStartsIn"` · `0x42AA898 = "…/TimeEndsIn"` ·
        /// `0x42AA990 = "…/TimeFinished"` · `0x42B80E0 = ""`（空串）。② 与本地化表的交叉印证也仍成立
        /// （`Event_Description/General/` 下就这三兄弟 + `EndsIn`/`GracePeriod` 那一族）。③ 枚举值也对上：`LiveOpState`
        /// `OnPreview=1 / Active=2 / Finished=3`，`.c` 的 `switch` 就是 `case 1/2/3`。</para>
        /// <para>⚠️ 我们这条不变量仍在：**本地 13 份 SO 没有在期事件** ⇒ 这条今天看不到效果
        /// （`判据/日常与活动.md` 那一段「Timer 前缀在调用点写死」同样成立：`ParentEventWindow` /
        /// `RankedEventWindow` 的 `TimerDisplay` 调用点传的仍是 `null` target）。</para></summary>
        public string GetTimeDescriptionTextKey()
        {
            var d = GetBaseData();
            if (d == null)
            {
                Debug.LogError("[LiveOps] GetTimeDescriptionTextKey：活动 '" + (GetID() ?? "<null>")
                             + "' 没有 data（原版 NRE）⇒ 返回空串");
                return "";
            }
            switch (d.GetState(LiveOpsClock.NowUtc))
            {
                case LiveOpState.OnPreview: return "Event_Description/General/TimeStartsIn";
                case LiveOpState.Active: return "Event_Description/General/TimeEndsIn";
                case LiveOpState.Finished: return "Event_Description/General/TimeFinished";
                default: return "";                    // Planned / Expired / NotAvailable 都不显示
            }
        }

        /// <summary>原版 `GetTimeDescriptionText()`（`LiveOpsEvent.cs:444`）：
        /// 算 key（同一个 switch），key 非空 ⇒ `LocalizationManager.GetTranslation(key, FixForRTL:true, …)`，否则 `""`。
        /// <para>我们这一跳走 <see cref="Loc.T"/>（工程里唯一的取词口；它自带官方全表 + 缺键档，
        /// 判据见 CLAUDE.md「判会不会把键名印到屏幕上必须三档一起算」那条）。</para>
        /// ⚠️ 差异：原版 `GetTranslation` 那几个 `FixForRTL` / `applyParameters` 参数我们没有对应开关
        /// （RTL 支持本工程未做）⇒ 如实记。</summary>
        public string GetTimeDescriptionText()
        {
            string key = GetTimeDescriptionTextKey();
            if (string.IsNullOrEmpty(key)) return "";
            return Loc.T(key);
        }
    }
}

// ============================================================ 活动**数据**的容器两跳（B87-C1 · 2026-10-27）
// 判据：原版 `Everguild.LiveOps/LiveOpsEventData.cs:162-170`
//   `GetContainer()`  = `LiveOpsAssetUtility.LoadComponent<UiContainer>(this, EventComponentReferenceType.Container)`
//   `ReleaseContainer()` = `LiveOpsAssetUtility.Unload(this, EventComponentReferenceType.Container)`
// 🔴 与上面 `LiveOpsEvent` 那两个同名方法**是一对**（原版两层各有一份）：事件层那份传 `this`（ILiveOps），
//    data 层这份传 `this`（LiveOpsEventData）。⛔ 别只做一边。
// 🔴 又是**文件末尾的 partial 段**：不插进上面任何类体，避免行号漂移（账按 `文件:行号` 引用）。
namespace CardPresentation
{
    public partial class LiveOpsEventData
    {
        /// <summary>原版 `LiveOpsEventData.GetContainer()`（`LiveOpsEventData.cs:162`）。
        /// ⚠️ 差异：`LiveOpsAssetUtility` 我们没有 ⇒ 走 <see cref="LiveOpsContainers.LoadForData"/> 注入点，
        /// 没装 ⇒ 出声 + null（原版拿不到引用时也是 null）。</summary>
        public IUiContainer GetContainer()
        {
            if (LiveOpsContainers.LoadForData == null)
            {
                Debug.LogError("[LiveOps] LiveOpsEventData.GetContainer：没有接 LiveOpsContainers.LoadForData"
                             + "（原版 = LiveOpsAssetUtility.LoadComponent<UiContainer>(data, Container)）⇒ null。"
                             + " eventId = " + (eventId ?? "<null>"));
                return null;
            }
            return LiveOpsContainers.LoadForData(this, LiveOpsContainers.ComponentContainer);
        }

        /// <summary>原版 `LiveOpsEventData.ReleaseContainer()`（`LiveOpsEventData.cs:167`）。
        /// ⚠️ 差异同 <see cref="GetContainer"/>；没装时只记日志（原版没装过时同样是 no-op）。</summary>
        public void ReleaseContainer()
        {
            if (LiveOpsContainers.UnloadForData == null)
            {
                Debug.Log("[LiveOps] LiveOpsEventData.ReleaseContainer：没有接 LiveOpsContainers.UnloadForData ⇒ 无容器可卸");
                return;
            }
            LiveOpsContainers.UnloadForData(this, LiveOpsContainers.ComponentContainer);
        }
    }
}

// ============================================================ `ILiveOps` 的两个缺声明补齐（B92·N1 · 2026-10-28）
// 判据 = `cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/ILiveOps.cs` 的**声明序**（逐行现读）：
//   `Type SaveType`(7) / `Type DataType`(9) / **`int Priority`(11)** / `bool IsSegmented`(13) /
//   `LiveOpState EventState`(15) / `ILiveOpsHandler Handler`(17) / **`bool ShowInMainMenu`(19)** / …
// 我们原来把这两格只放在 `LiveOpsEvent` **类**上（`:379` / `:385`），接口里没有 ⇒
// 任何「按接口取优先级 / 问该不该上主菜单」的代码只能 downcast 或拿 `GetEventPriorityForMainMenu()` 顶替
// （账 217/218 的排序键就是这么绕的；菜单容器 C1-W4 接线时会被迫 cast）。
// 🔴 补这一条**零行为变化、零改调用点**：类上那两个成员早就是 `public`，接口声明自动被满足。
// 🔴 追加在文件末尾（⛔ 不插进上面任何类体）—— 账按 `文件:行号` 引用，插中间会让下游落点整体漂移。
namespace CardPresentation
{
    public partial interface ILiveOps
    {
        /// <summary>原版 `ILiveOps.Priority`（`ILiveOps.cs:11`，声明序第 3）：`int Priority { get; }`。
        /// 实现判据 = `LiveOpsEvent.cs:385`（`GetBaseData().priority`）—— 与 `GetEventPriorityForMainMenu()`
        /// **同一格**（原版就有这两个入口，⛔ 别让它们读不同的字段）。</summary>
        int Priority { get; }
        /// <summary>原版 `ILiveOps.ShowInMainMenu`（`ILiveOps.cs:19`，声明序第 7）：`bool ShowInMainMenu { get; }`。
        /// 实现判据 = `LiveOpsEvent.cs:379`（`GetBaseData().showInMainMenu`）；`PlayEvent&lt;TSave,TData&gt;`
        /// 还多一道 `EventState == Active` 闸（`PlayEvent.cs:420`，原版 `PlayEvent.cs:260`）。</summary>
        bool ShowInMainMenu { get; }

        /// <summary>原版 `ILiveOps.SelectEvent()`（`ILiveOps` 槽 **6**，`dump.cs:126037`）：「点这颗活动钮」。
        /// 实现 = <see cref="LiveOpsEvent.SelectEvent"/>（本文件末尾的 partial 段）。🆕 2026-10-28（B93_E12 · 账 row 355）。</summary>
        void SelectEvent();
    }

    // ============================================================ 🆕 2026-10-28（B93_E12 · 账 row 355）
    //   `LiveOpsEvent.SelectEvent()` —— 原来**整条缺**（`LiveOpsEvent.cs:301` 的「还没有」清单里挂着；
    //   `Shell/GameNewsEvent.cs:140-143` 也写着「等 B09 补上基类 `SelectEvent`」）。
    //   ⚠️ 追加在**文件末尾**（partial 类）：本文件多条账按 `文件:行号` 钉着（686 / 777 / 862 / 1138 / 1208 / 1232），
    //      插进上面任何类体都会让下游落点整体漂移。
    /// <summary>原版 `LiveOpsEvent&lt;TSave,TData&gt;.SelectEvent()`（`cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/LiveOpsEvent.cs:659-669`）：
    /// <code>
    /// if (Data == null) → NRE                       // `.c`/ILSpy 读 `this + 0x18`（= `Data`）
    /// var reference = LiveOpsAssetUtility.GetComponentReference(this, EventComponentReferenceType.MainWindow /*10*/);
    /// WindowsManager.Instance.OpenWindow(reference, this, closeAll: false, options);
    /// </code>
    /// 🔴 **与 `OpenWindow()` 的区别**（⛔ 别把两者合并）：`OpenWindow()` 按**状态**选 label（OnPreview⇒20 / Finished⇒21 / 其余⇒10），
    /// `SelectEvent()` **恒用 `MainWindow`(10)** —— 它是「用户点了那颗钮」，跟活动处于哪一档无关。
    /// <para>⚠️ 等价缝：原版那句 `WindowsManager.OpenWindow(reference, this, closeAll:false, options)` 在我们这边走同一个
    /// <see cref="LiveOpsPopupPresenter.Open"/>（`options.PopupLabel = 10`，与 `OpenWindow()` 的 default 支同形）。</para></summary>
    public abstract partial class LiveOpsEvent
    {
        /// <summary>见上面那段的判据。数据为 null 时原版 NRE，我们出声后不开窗（同本类其余方法的差异口径）。</summary>
        public virtual void SelectEvent()
        {
            var d = GetBaseData();
            if (d == null)
            {
                Debug.LogError("[LiveOps] 活动 '" + (GetID() ?? "<null>") + "' 的 SelectEvent()：没有 data（原版会 NRE）⇒ 不开窗");
                return;
            }
            var opts = new PopupOptions { PopupLabel = (int)LiveOpsComponentRef.MainWindow };   // = 10
            GameWindow win = LiveOpsPopupPresenter.Open != null ? LiveOpsPopupPresenter.Open(this, opts) : null;
            if (win == null)
            {
                Debug.LogError("[LiveOps] 活动 '" + (GetID() ?? "<null>") + "' 的 SelectEvent()：`MainWindow`(10) 的组件引用解析不到"
                             + "（或 `LiveOpsPopupPresenter` 没装）⇒ **不开窗**（原版此支 = `WindowsManager.OpenWindow` 拿不到引用时的空跳）");
            }
        }
    }

    // ============================================================ 原版 `LiveOpsAssetUtility.ReleaseEventAssets`（B93·N2 · 2026-10-28 追加）
    // 🔴 **2026-10-29（B94_T1）已搬走**：那两个成员（`ReleaseAsset` / `ReleaseOneAssetRef` / `ReleaseEventAssets`）
    //   原挂在上面那个 `LiveOpsContainers` partial 上，现按 B93·N2 留下的交接搬进**原版那个类**
    //   `LiveOpsAssetUtility`（落点 = `Shell/LiveOpsLocalCatalog.cs` 文件尾的第二段 `partial`；逻辑逐字未改）。
    //   ⇒ ⛔ 别在这里再建第二份（会 CS0111 重复定义）；`LiveOpsContainers` 只剩 `Load/Unload` 那一族注入点。
}
