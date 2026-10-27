// ============================================================================
//  活动状态机 + EventFilter 的六种条件（原版判据的离线等价物）
//  创建：第八十八会话 · 批次 B88_J3（2026-10-27）
//
//  这一份只装两件事（⛔ 别再往这里塞别的）：
//   ① `LiveOpsLocalEvent` 的**状态那半**：`ActivationType` / `Previewable` / `PreviewDateMs` 三格 +
//      `GetState(now)` 六档 + `IsActive(now, includePlanned)`。
//      判据 = `decomp_full/Everguild.LiveOps.LiveOpsEventData__GetState.c`（权威）
//             + `decomp_full/Everguild.LiveOps.LiveOpsEventData__IsActive.c`（权威）
//             + 可读版 `out_cpp2il_dev_ilspy/Everguild.LiveOps/LiveOpsEventData.cs:98-180`。
//      与 `LiveOpsEventData.GetState/IsActive`（`Shell/LiveOpsEvent.cs`）**同一套语义**，那两处已经逐句照抄过，
//      这里是把同一套判据落到**离线目录模型**上（⛔ 别在别处再写第三份 —— 铁律 6）。
//   ② `EventFilter` 的六种条件类（原版全是 `EventCondition` 的 ScriptableObject 子类）：
//      `LevelCondition` / `CurrencyCondition` / `InventoryCondition` / `HasDeckCondition` /
//      `TagCondition` / `AttributeCondition`。抽象基类 `EventCondition` 与
//      `LiveOpsEventFilter.Validate/CheckEventGroups` 早已在 `Shell/LiveOpsFilter.cs`，这里只补子类。
//
//  ⚠️ 数据现状（现读，2026-10-27）：本 build 16 份活动 SO 的 `filter.eventGroups` / `Triggers` **全空**、
//     `Conditions` 只有 2 份非空且各引同一个 `rid 4942887995584020480`（该资产本机未定位）
//     ⇒ 六种条件**今天没有任何一条会被跑**（不是「已实现所以看得见」，是「判据齐了但数据不触发」）。
//     `previewable` 16/16 = 0 ⇒ `OnPreview` 档同样**今天不可达**。
// ============================================================================

using System;
using UnityEngine;

namespace CardPresentation
{
    // ============================================================ 六档判据的【唯一实现】
    /// <summary>原版 `LiveOpsEventData.GetState` / `.IsActive` 的**唯一实现**（两个载体现共用它）：
    /// ① `LiveOpsEventData`（`Shell/LiveOpsEvent.cs`，SO 等价物 / 服务端数据面）② `LiveOpsLocalEvent`（离线目录模型）。
    /// <para>🔴 铁律 6（两处写同一条规则 = 迟早不一致）⇒ 六档的**判定顺序 / 严格小于 / activationType 短路**只写在这里，
    /// ⛔ 别在任何一个载体里再抄一份。判据 = `decomp_full/Everguild.LiveOps.LiveOpsEventData__GetState.c` +
    /// `…__IsActive.c`（两份都是真体）。</para>
    /// <para>⚠️ 原版 `dates == null`（OnDate 事件）时会解引用抛 NRE；我们的两个载体各自保留「出声 + 降级」那一跳
    /// （那是对坏数据的处理选择，不属于六档判据本身）。</para></summary>
    public static class LiveOpsStateMachine
    {
        /// <summary>六档判定（顺序即原版顺序，全部**严格小于**）：
        /// `!enabled ⇒ NotAvailable` → `activationType ∈ {Permanent, OneTime} ⇒ Active`（短路，不看日期）
        /// → `FinalDate &lt; now ⇒ Expired` → `endDate &lt; now ⇒ Finished` → `startDate &lt; now ⇒ Active`
        /// → `InitialDate &lt; now ? OnPreview : Planned`。</summary>
        public static LiveOpState Compute(bool enabled, LiveOpsActivationType activationType,
                                          long startMs, long endMs, bool expirable, long expireMs,
                                          bool previewable, long previewMs, DateTime utcNow)
        {
            if (!enabled) return LiveOpState.NotAvailable;
            if (activationType == LiveOpsActivationType.Permanent || activationType == LiveOpsActivationType.OneTime)
                return LiveOpState.Active;
            long finalMs = expirable ? expireMs : endMs;
            if (FromMs(finalMs) < utcNow) return LiveOpState.Expired;
            if (FromMs(endMs) < utcNow) return LiveOpState.Finished;
            if (FromMs(startMs) < utcNow) return LiveOpState.Active;
            long initialMs = previewable ? previewMs : startMs;
            return FromMs(initialMs) < utcNow ? LiveOpState.OnPreview : LiveOpState.Planned;
        }

        /// <summary>原版 `IsActive(now, includePlanned)` 的**唯一实现**（反直觉但照抄）：
        /// `!enabled ⇒ includePlanned`；`(includePlanned &amp;&amp; s == Planned) ⇒ true`；
        /// `s ∈ {Active, Finished} ⇒ true`；`s == OnPreview ⇒ true`；其余 false。
        /// 🔴 **`Finished` 也算 active**（`Expired` 不算）—— 那是「该刷新/该收尾」的口径，
        /// ⛔ 别拿它当「现在开不开」（那个用 <see cref="LiveOpState.Active"/>）。</summary>
        public static bool ComputeIsActive(bool enabled, bool includePlanned, LiveOpState state)
        {
            if (!enabled) return includePlanned;
            if (!includePlanned || state != LiveOpState.Planned)
            {
                if ((uint)((int)state - 2) > 1u) return state == LiveOpState.OnPreview;
            }
            return true;
        }

        /// <summary>毫秒时间戳 → UTC（与原版 `SupportMethods.MillisSinceEpochToDateTime` 同一条换算；
        /// 与 `LiveOpsEventTimeData.FromMs` / `LiveOpsLocalEvent.FromMs` 保持一致）。</summary>
        public static DateTime FromMs(long ms) { return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime; }
    }

    public sealed partial class LiveOpsLocalEvent
    {
        // ------------------------------------------------------------ 三格新数据（原版 `LiveOpsEventData` 的同名字段）
        /// <summary>原版 `LiveOpsEventData.activationType`（+0x24）。
        /// 🥇 **逐值照 SO**（`d:/2/新解包资源/assets_full/bundle_cosmeticsso_assets_all/MonoBehaviour/*.json` 的
        /// `activationType`）：本地目录这 **13 份全是 1 = `OnDate`** ⇒ 默认值就取 `OnDate`（⛔ 别取枚举 0，那会变成 Permanent）。
        /// <para>⚠️ 另有 **3 份 SO 是 0 = `Permanent`** —— `PremiumCampaignStore` / `RewardsMenu` / `UMCampaign`
        /// （17 份里除这 3 份与 `DarkZealots` 外，其余全是 OnDate；判据见报告 `B88_J3_活动线判据.md` §2·5）。
        /// 它们**目前不在 `LiveOpsLocalCatalog.Events` 这 13 份里** ⇒ 一旦有人把它们加进目录，
        /// 必须显式写 `ActivationType = LiveOpsActivationType.Permanent`（否则会被当成 OnDate 按日期过期）。</para></summary>
        public LiveOpsActivationType ActivationType = LiveOpsActivationType.OnDate;

        /// <summary>原版 `EventTimeData.previewable`（+0x20）。**本 build 16/16 = false**（含目录里的 13 份）
        /// ⇒ `OnPreview` 档今天不可达；字段留着是为了把六档写全（⛔ 别删「不可达」的那两档代码）。</summary>
        public bool Previewable;

        /// <summary>原版 `EventTimeData.previewDate`（+0x28），毫秒。`Previewable == false` 时原版根本不读它。</summary>
        public long PreviewDateMs;

        /// <summary>原版 `EventTimeData.FinalDate`：`expirable ? expireDate : endDate`。
        /// 🔴 这是 `Expired` 档用的那个日期，**不是** `EndMs` —— 两者在 `Expirable == true` 时不同。</summary>
        public long FinalDateMs { get { return Expirable ? ExpireMs : EndMs; } }

        /// <summary>原版 `EventTimeData.InitialDate`：`previewable ? previewDate : startDate`。
        /// 这是 `OnPreview` 档用的那个日期。</summary>
        public long InitialDateMs { get { return Previewable ? PreviewDateMs : StartMs; } }

        // ------------------------------------------------------------ 六档
        /// <summary>原版 `LiveOpsEventData.GetState(DateTime currentTime)`（**逐条照抄**，判定顺序即下列顺序）：
        /// <code>
        /// if (!enabled)                                   return NotAvailable;   // -1
        /// if (activationType is Permanent or OneTime)      return Active;         //  2（短路，不看日期）
        /// if (FinalDate  &lt; now)                          return Expired;        //  4
        /// if (endDate    &lt; now)                          return Finished;       //  3
        /// if (startDate  &lt; now)                          return Active;         //  2
        /// return (InitialDate &lt; now) ? OnPreview : Planned;                     //  1 / 0
        /// </code>
        /// 🔴 **全部是「严格小于」**（`System.DateTime.op_LessThan`），⛔ 不是 `≤`。
        /// 旧口径 `IsInPeriod` 用的是 `utc &gt;= StartUtc &amp;&amp; utc &lt; EndUtc`（含等号、且完全没有
        /// `activationType` / `expirable` / `previewable` 三档）⇒ **与这条不等价**，见 <see cref="IsInPeriod"/> 的注记。
        /// <para>⚠️ `Expirable == true` 但 `ExpireMs == 0`（数据坏）时，原版会把 1970 当 FinalDate ⇒ 恒 `Expired`；
        /// 我们照抄（不「兜底」成 EndMs —— 那会与二进制不符）。</para></summary>
        public LiveOpState GetState(DateTime utcNow)
        {
            // 🔴 六档只在 `LiveOpsStateMachine.Compute` 一处实现（⛔ 别在这里再写一遍）
            return LiveOpsStateMachine.Compute(Enabled, ActivationType, StartMs, EndMs,
                                               Expirable, ExpireMs, Previewable, PreviewDateMs, utcNow);
        }

        /// <summary>原版 `LiveOpsEventData.IsActive(DateTime, bool includePlanned = false)`（**逐条照抄**）：
        /// <code>
        /// if (!enabled) return includePlanned;
        /// var s = GetState(now);
        /// if (!includePlanned || s != Planned) { if ((uint)(s - 2) &gt; 1u) return s == OnPreview; }
        /// return true;
        /// </code>
        /// 🔴 **反直觉但照抄**：`Finished(3)` **也算 active**（`(uint)(3-2)=1` 不大于 1 ⇒ 落到 `return true`）；
        /// `Expired(4)` / `NotAvailable(-1)` 不算；`Planned` 只在 `includePlanned` 时算。
        /// 消费者实例（原版）：`ShopStore.cs:372` 用 `IsActive(now, false)`；`FixedStoreData.cs:72,139` 用 `IsActive(now, true)`。</summary>
        public bool IsActive(DateTime utcNow, bool includePlanned = false)
        {
            return LiveOpsStateMachine.ComputeIsActive(Enabled, includePlanned, GetState(utcNow));
        }

        /// <summary>「现在能不能被选中」= 原版 UI 建按钮那一档：**`GetState(now) == Active`**（严格）。
        /// 判据 = `out_cpp2il_dev_ilspy/NavigationPanelController.cs:433`（`eventState != LiveOpState.Active` ⇒ 不建按钮）；
        /// ⛔ 别用 <see cref="IsActive"/>（它把 `Finished` 也算 true，那是「该不该刷新/该不该收尾」的口径）。</summary>
        public bool IsCurrentlyActive(DateTime utcNow) { return GetState(utcNow) == LiveOpState.Active; }

        /// <summary>档位名（日志/自检文案用；⛔ 别拿它当判据）。</summary>
        public static string StateName(LiveOpState s)
        {
            switch (s)
            {
                case LiveOpState.NotAvailable: return "NotAvailable";
                case LiveOpState.Planned: return "Planned";
                case LiveOpState.OnPreview: return "OnPreview";
                case LiveOpState.Active: return "Active";
                case LiveOpState.Finished: return "Finished";
                case LiveOpState.Expired: return "Expired";
                default: return "?" + (int)s;
            }
        }
    }

    // ============================================================ EventFilter 六种条件的「数据口」
    /// <summary>六种 `EventCondition` 子类需要的**外部查询口**。
    /// <para>🔶 自己设计（非原版）：原版这六个条件各自 `SingletonBehaviour&lt;X&gt;.Instance` 直连
    /// （`InventoryManager` / `SegmentationController` / `PlayerDataManager`）；我们本地**没有**
    /// `SegmentationController`（全仓 0 个类）、也没有「玩家等级」这一层 ⇒ 把查询口做成可注入的静态委托
    /// （与 `InventoryManager.CurrencyExtensions.CampaignPointsOf` 同一种做法）。为什么必须自己定：
    /// 判据（原版方法体）拿得到，但**被查的那几个单例本地不存在**（判据 → 报告 `B88_J3_活动线判据.md` §2·3）。
    /// 谁定的 + 日期：调度台 2026-10-27（B88_J3 批次）。</para>
    /// <para>⛔ **没注入 = 出声 + 该条条件判 false**（fail-closed）。这是有意的：静默 `true` 会让一道闸
    /// 「看起来永远通过」（本工程踩过 —— 见 CLAUDE.md「自检里的 if (x != null) Check 静默跳过」）。</para></summary>
    public static class LiveOpsConditionBindings
    {
        /// <summary>原版 `SegmentationController.CheckTag(PlayerTag)`。参数 = `PlayerTag` 的 int 值
        /// （⛔ 本地没有 `PlayerTag` 枚举类，只有注释里那几条取值，别在这里编一个）。</summary>
        public static Func<int, bool> CheckTag;
        /// <summary>原版 `SegmentationController.GetRawAttribute(PlayerTag)`（`AttributeCondition` 用）。</summary>
        public static Func<int, string> GetRawAttribute;
        /// <summary>原版 `Evaluation.Check(string raw, Evaluation.Operation op, string value)`。
        /// 参数 = (原值, 运算的 int 值, 目标值)。</summary>
        public static Func<string, int, string, bool> EvaluationCheck;
        /// <summary>原版 `PlayerDataManager.singletonManager.CurrentLevel`。</summary>
        public static Func<int> PlayerLevel;
        /// <summary>原版 `InventoryManager.HasItem(item)` —— 按**物品 id** 查（类型无关的等价物；
        /// 原版按 `LoadableReference&lt;ObtainableItem&gt;` 的 runtime type 选背包）。</summary>
        public static Func<string, bool> HasItemById;
        /// <summary>原版 `InventoryManager.GetInventory&lt;CardDeck&gt;().GetWhere(d =&gt; d.prebuiltDeckId == targetId).Any()`。</summary>
        public static Func<string, bool> HasDeckByPrebuiltId;

        static readonly System.Collections.Generic.HashSet<string> _warned =
            new System.Collections.Generic.HashSet<string>();

        /// <summary>取口失败时统一走这里：**每 (口名) 只出声一次**，然后返回 `fallback`。</summary>
        internal static T Missing<T>(string port, T fallback)
        {
            if (_warned.Add(port))
                Debug.LogError("[LiveOps] 条件判据缺「" + port + "」的查询口（`LiveOpsConditionBindings."
                             + port + "` 未注入）⇒ 该条件判 false（fail-closed）。"
                             + "原版这里是 `SingletonBehaviour<…>.Instance` 直连，本地没有那个件");
            return fallback;
        }

        /// <summary>自检用：清空「已经出过声」的去重表。</summary>
        public static void ResetWarnedForTest() { _warned.Clear(); }
    }

    // ============================================================ 六种条件（原版 EventCondition 的六个子类）
    /// <summary>原版 `LevelCondition`（`LevelCondition.cs`，全文 4 行）：
    /// `return PlayerDataManager.singletonManager.CurrentLevel &gt;= requiredLevel;`
    /// （`.c` 是标准 `&gt;=` 的符号位解码）⇒ **大于等于**才过。</summary>
    public sealed class LevelCondition : EventCondition
    {
        /// <summary>原版 `LevelCondition.requiredLevel`（int）。</summary>
        public int requiredLevel;

        public override bool Validate(ILiveOps liveOps)
        {
            if (LiveOpsConditionBindings.PlayerLevel == null)
                return LiveOpsConditionBindings.Missing("PlayerLevel", false);
            return LiveOpsConditionBindings.PlayerLevel() >= requiredLevel;
        }
    }

    // 🔴 `CurrencyCondition` **不在这里**：它早已存在 = `InventoryManager.CurrencyExtensions.CurrencyCondition`
    //    （`Shell/Inventory.cs:1809`，`LoadableRef<Currency> targetCurrency` + `HasEnough<Currency>(...)`，逐句照原版）。
    //    ⛔ 2026-10-27 本文件曾重复定义一次 ⇒ CS0101（与 `ScoringDataInfo` 那次同型）。**别再加第二份**。
    //    ⚠️ 形状偏差（留给 Inventory.cs 的归属人）：原版它是**全局命名空间顶层类**，我们这份嵌在 `CurrencyExtensions` 里。

    /// <summary>原版 `InventoryCondition`（`InventoryCondition.cs`）：
    /// `bool has = instance.HasItem(targetItem); return (has ? 1 : 0) - (required ? 1 : 0) == 0;`
    /// ⇒ 🔴 **`has == required`**：`required == false` 表示「**必须没有**这件东西」（⛔ 不是「有就行」）。
    /// 同一个形状也用在 <see cref="HasDeckCondition"/> 上。</summary>
    public sealed class InventoryCondition : EventCondition
    {
        /// <summary>原版 `InventoryCondition.targetItem`（`LoadableReference&lt;ObtainableItem&gt;`）解出来的 id。</summary>
        public string targetItemId;
        /// <summary>原版 `InventoryCondition.required`。🔴 `false` = 必须**没有**。</summary>
        public bool required;

        public override bool Validate(ILiveOps liveOps)
        {
            if (LiveOpsConditionBindings.HasItemById == null)
                return LiveOpsConditionBindings.Missing("HasItemById", false);   // fail-closed（⛔ 别写成 `== required`，那会让 required=false 反而变 true）
            bool has = LiveOpsConditionBindings.HasItemById(targetItemId);
            return has == required;
        }
    }

    /// <summary>原版 `HasDeckCondition`（`HasDeckCondition.cs`）：
    /// `var any = inventory.GetWhere(d =&gt; d.prebuiltDeckId == targetDeck.TargetId).Any();`
    /// `return (any ? 1 : 0) - (required ? 1 : 0) == 0;` ⇒ 🔴 同 <see cref="InventoryCondition"/>：`required == false` = 必须没有。</summary>
    public sealed class HasDeckCondition : EventCondition
    {
        /// <summary>原版 `HasDeckCondition.targetDeck`（`LoadableReference&lt;PrebuiltDeck&gt;`）的 `TargetId`。</summary>
        public string targetDeckId;
        /// <summary>原版 `HasDeckCondition.required`。🔴 `false` = 必须**没有**。</summary>
        public bool required;

        public override bool Validate(ILiveOps liveOps)
        {
            if (LiveOpsConditionBindings.HasDeckByPrebuiltId == null)
                return LiveOpsConditionBindings.Missing("HasDeckByPrebuiltId", false);   // fail-closed（同 InventoryCondition）
            bool any = LiveOpsConditionBindings.HasDeckByPrebuiltId(targetDeckId);
            return any == required;
        }
    }

    /// <summary>原版 `TagCondition`（`TagCondition.cs`）：每个 option 里
    /// `if (SegmentationController.CheckTag(option.Tag) != (option.Requirement == include)) return false;`
    /// ⇒ **全部 option 都要满足**；`Requirement == exclude` 的那条要求「CheckTag 为 false」。</summary>
    public sealed class TagCondition : EventCondition
    {
        /// <summary>原版嵌套枚举 `TagCondition.TagRequirements`：`include = 0` / `exclude = 1`。</summary>
        public enum TagRequirements { include = 0, exclude = 1 }

        /// <summary>原版嵌套类 `TagCondition.TagOption`。</summary>
        [Serializable]
        public class TagOption
        {
            /// <summary>`PlayerTag` 的 int 值（本地没有该枚举类 ⇒ 存值）。</summary>
            public int tag;
            public TagRequirements requirement;
        }

        /// <summary>原版 `TagCondition.options`（List）。</summary>
        public System.Collections.Generic.List<TagOption> options =
            new System.Collections.Generic.List<TagOption>();

        public override bool Validate(ILiveOps liveOps)
        {
            for (int i = 0; i < options.Count; i++)
            {
                var o = options[i];
                if (o == null) continue;
                if (LiveOpsConditionBindings.CheckTag == null)
                {
                    // 一条都判不了 ⇒ 整条条件 fail-closed（原版这里 NRE，我们出声 + false）
                    return LiveOpsConditionBindings.Missing("CheckTag", false);
                }
                bool on = LiveOpsConditionBindings.CheckTag(o.tag);
                bool want = o.requirement == TagRequirements.include;
                if (on != want) return false;
            }
            return true;
        }
    }

    /// <summary>原版 `AttributeCondition`（`AttributeCondition.cs`）：每个 option 里
    /// `if (!Evaluation.Check(SegmentationController.GetRawAttribute(option.Tag), option.Operation, option.Value)) return false;`
    /// ⇒ **全部 option 都要为真**（⚠️ `AttributeCondition.cs` 的方法体被混淆只剩间接调用，
    /// 「`Evaluation.Check` 的三实参顺序」按 ILSpy 里能读到的 `rawAttribute / operation / value` 三处装载推定；
    /// 结论方向（全真才过）与 `TagCondition` 同形，可信）。</summary>
    public sealed class AttributeCondition : EventCondition
    {
        /// <summary>原版嵌套类 `AttributeCondition.AttributeOption`。</summary>
        [Serializable]
        public class AttributeOption
        {
            /// <summary>`PlayerTag` 的 int 值。</summary>
            public int tag;
            /// <summary>原版 `Evaluation.Operation` 的 int 值（本地没有该枚举类 ⇒ 存值）。</summary>
            public int operation;
            /// <summary>原版 `AttributeOption.Value`（字符串形式的比较值）。</summary>
            public string value;
        }

        public System.Collections.Generic.List<AttributeOption> options =
            new System.Collections.Generic.List<AttributeOption>();

        public override bool Validate(ILiveOps liveOps)
        {
            for (int i = 0; i < options.Count; i++)
            {
                var o = options[i];
                if (o == null) continue;
                if (LiveOpsConditionBindings.GetRawAttribute == null || LiveOpsConditionBindings.EvaluationCheck == null)
                    return LiveOpsConditionBindings.Missing("GetRawAttribute/EvaluationCheck", false);
                string raw = LiveOpsConditionBindings.GetRawAttribute(o.tag);
                if (!LiveOpsConditionBindings.EvaluationCheck(raw, o.operation, o.value)) return false;
            }
            return true;
        }
    }
}
