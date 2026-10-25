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
    /// <summary>原版 `EventFilter`（`EventFilter.cs`，`[Serializable]`）—— **本批只搬 `Initialize` 用得到的那两格**：
    /// `hasDuration`（默认 **false**）与 `eventDuration`（默认 **24**，单位 = **小时**）。
    /// <para>🔴 判据（2026-10-25 坐实）：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/EventFilter.cs`
    /// 的字段初始化器 `public int eventDuration = 24;`；字段偏移（`hasDuration` +0x18 / `eventDuration` +0x1C）
    /// 见 `资料/完全复刻/判据/LiveOpsHandler_11方法_第2轮.md` §0·5。</para>
    /// <para>⚠️ **原版还有 `army` / `expansion` / `hasCooldown` / `eventCooldown` / `eventGroups` / `Triggers` /
    /// `Conditions`** —— 那些属 C1-W2（`CheckEventFilter` / `RegisterEventTriggers` / 冷却门）**本批没搬**，
    /// ⛔ 没搬就是没搬，别以为 `EventFilter` 已经整个有了。本地 13 份 SO 的 `hasDuration` **全是 0**、
    /// `eventDuration` 全是 24（逐份核过）⇒ 这里保持默认值即可，无需从目录映射。</para></summary>
    [Serializable]
    public class LiveOpsEventFilter
    {
        /// <summary>原版 `EventFilter.hasDuration`（+0x18）。`false` ⇒ `Initialize()` **不排期**。</summary>
        public bool hasDuration;
        /// <summary>原版 `EventFilter.eventDuration`（+0x1C），**单位 = 小时**，默认 **24**。</summary>
        public int eventDuration = 24;
    }

    // ============================================================ 活动数据（原版 `LiveOpsEventData`）
    /// <summary>原版 `LiveOpsEventData : IdentifiableSO` 的**可序列化字段那半**（字段表逐个抄：
    /// `eventId` / `uploadToLive`(私有无序列化标注，默认 true) / `uploadToPreprod` / `enabled`(默认 true) /
    /// `activationType` / `dates` / `filter` / `drawData` / `priority` / `location` / `showInMainMenu` / `mainMenuOptions`）。
    /// <para>⚠️ `mainMenuOptions` 本地**没有数据源** ⇒ **留 null，不假装有**。
    /// `drawData`(`EventElements`)：**我们没有 `EventElements` 这个类**（⛔ 不编一个假的）——
    /// 它里面真正被 `QueueEventPopup` 读的 4 个 bool（`ForceRun` / `ShowWindowOnTrigger` /
    /// `ShowWindowOnExpire` / `HideContainer`）已**逐份照 SO 抄进** `LiveOpsLocalEvent` 的同名字段
    /// （🆕 2026-10-25 R14，数据源 = 每份 SO JSON 的 `drawData.*`）；`EventLabels` / `EventAssets` /
    /// `EventComponents` 则对应 `LiveOpsLocalEvent` 的 `Labels` / `Assets` / `Components`。
    /// ⇒ **C1-W2 接弹窗时从那里读，⛔ 别新建第二份表。**
    /// `filter` 只建了「`Initialize` 用得到的两格」那个最小等价物（见 <see cref="LiveOpsEventFilter"/>）。</para></summary>
    [Serializable]
    public class LiveOpsEventData
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
            if (LiveOpsEventTimeData.FromMs(d.FinalDate) < utcNow) return LiveOpState.Expired;
            if (LiveOpsEventTimeData.FromMs(d.endDate) < utcNow) return LiveOpState.Finished;
            if (LiveOpsEventTimeData.FromMs(d.startDate) < utcNow) return LiveOpState.Active;
            return LiveOpsEventTimeData.FromMs(d.InitialDate) < utcNow ? LiveOpState.OnPreview : LiveOpState.Planned;
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
            if (!enabled) return includePlanned;
            LiveOpState s = GetState(utcNow);
            if (!includePlanned || s != LiveOpState.Planned)
            {
                if ((uint)(s - 2) > 1u) return s == LiveOpState.OnPreview;
            }
            return true;
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
    public interface ILiveOps
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
        void Dispose();
    }

    // ============================================================ 非泛型基类（原版 `LiveOpsEvent`）
    /// <summary>原版 `LiveOpsEvent`（非泛型那半：`Handler` 回指 + `EventState` / `ShowInMainMenu` / `Priority`
    /// 三个从 data 派生的读口 + 抽象契约）。</summary>
    public abstract class LiveOpsEvent : ILiveOps
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

        /// <summary>原版 `LiveOpsEvent.IsActive()`（virtual，无参版）：按**当前时间**问 data。
        /// 判据 = `LiveOpsEvent.cs:150`；语义 = `GetBaseData().IsActive(now)`（`includePlanned = false`）。</summary>
        public virtual bool IsActive()
        {
            var d = GetBaseData();
            return d != null && d.IsActive(LiveOpsClock.NowUtc, false);
        }

        /// <summary>原版 `LiveOpsEvent.Dispose()`（virtual）：默认空（子类释放自己的容器/句柄）。</summary>
        public virtual void Dispose() { }
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
