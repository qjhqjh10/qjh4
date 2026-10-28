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
        /// （⛔ 本地没有 `PlayerTag` 枚举类，只有注释里那几条取值，别在这里编一个）。🆕 2026-10-28（B92·N1）：**默认接本地口**（改前裸 null ⇒ `TagCondition` 恒 `Missing→false`）。</summary>
        public static Func<int, bool> CheckTag = SegmentationControllerLocal.CheckTagPort;
        /// <summary>原版 `SegmentationController.GetRawAttribute(PlayerTag)`（`AttributeCondition` 用）。🆕 2026-10-28（B92·N1）：**默认接本地口**。</summary>
        public static Func<int, string> GetRawAttribute = SegmentationControllerLocal.GetRawAttributePort;
        /// <summary>原版 `Evaluation.Check(object x, object y, Operation operation)` —— **实参序就是 (x, y, operation)**（判据 = `AttributeCondition__Validate.c:56`：`Evaluation__Check(rawAttribute, option.Value(+0x18), (Operation)option.Operation(+0x14))`）。
        /// 🆕 2026-10-28（B92·N1）：签名从「(raw, op, value)」**就地订正为原版序**「(x, y, operation)」，并**默认接本地口**（改前裸 null ⇒ `AttributeCondition` 恒 `Missing→false`）。</summary>
        public static Func<string, string, int, bool> EvaluationCheck = EvaluationLocal.Check;
        /// <summary>原版 `PlayerDataManager.singletonManager.CurrentLevel`。🆕 2026-10-28（B90·N3）：**默认接本地口**（改前裸 null + 全仓 0 处赋值 ⇒ 恒 `Missing→false`）。</summary>
        public static Func<int> PlayerLevel = LiveOpsConditionBindingsLocal.PlayerLevel;
        /// <summary>原版 `InventoryManager.HasItem(item)` —— 按**物品 id** 查（类型无关的等价物；
        /// 原版按 `LoadableReference&lt;ObtainableItem&gt;` 的 runtime type 选背包）。🆕 2026-10-28（B90·N3）：默认接上。</summary>
        public static Func<string, bool> HasItemById = LiveOpsConditionBindingsLocal.HasItemById;
        /// <summary>原版 `InventoryManager.GetInventory&lt;CardDeck&gt;().GetWhere(d =&gt; d.prebuiltDeckId == targetId).Any()`。🆕 2026-10-28 同上：默认接上。</summary>
        public static Func<string, bool> HasDeckByPrebuiltId = LiveOpsConditionBindingsLocal.HasDeckByPrebuiltId;

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
                // 🔴 判据 = `decomp_full/Everguild.LiveOps.TagCondition__Validate.c:64-65`：
                //   循环条件 = `while (CheckTag(tag) == (requirement == include))` ⇒ 不等就**跳出并返回 0**。
                bool on = LiveOpsConditionBindings.CheckTag(o.tag);
                bool want = o.requirement == TagRequirements.include;
                if (on != want)
                {
                    // 原版在 `liveOps != null` 时**先打一条日志再返回 false**（`.c:66-78`，
                    // 文案 = `$"{(liveOps 侧的某个名字)} failed tag validation {tag} - {requirement}"`）。
                    // ⚠️ 差异：我们把 `{tag}` 印成 **int**（本地没有 `PlayerTag` 枚举类），`{requirement}` 印成枚举名。
                    Debug.Log((liveOps != null ? liveOps.GetID() : "<null>")
                            + " failed tag validation " + o.tag + " - " + o.requirement);
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>原版 `AttributeCondition`（`AttributeCondition.cs`）：每个 option 里
    /// `if (!Evaluation.Check(GetRawAttribute(option.Tag), option.Value, option.Operation)) return false;`
    /// ⇒ **全部 option 都要为真**。
    /// 🔴 2026-10-28（B92·N1）**推定已作废**：`decomp_full/Everguild.LiveOps.AttributeCondition__Validate.c` 是
    /// **可读真体**（61 行），逐实参对过 —— `.c:55` `GetRawAttribute(this+0x10 的 option 表项 + 0x10)`、
    /// `.c:56` `Evaluation.Check(raw, option+0x18(=Value), (Operation)option+0x14)`；循环条件是
    /// `while (check != 0)`（有一条不成立就返回 0）；**失败支不打日志**（与 `TagCondition` 不同，⛔ 别给它加）。</summary>
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
                // 🔴 实参序 = 原版 `Evaluation.Check(rawAttribute, option.Value, (Operation)option.Operation)`
                // （判据 = `decomp_full/Everguild.LiveOps.AttributeCondition__Validate.c:56`，逐实参对过）。
                if (!LiveOpsConditionBindings.EvaluationCheck(raw, o.value, o.operation)) return false;
            }
            return true;
        }
    }
}

// ============================================================ 🆕 2026-10-28（B90 · N3，追加在文件尾）
//   `LiveOpsConditionBindings` 那三个「本地有数据源」的口的**默认实现**。
//   🔴 为什么要有它（铁律 5·b / 判据见账）：`收藏.tsv` 的 `InventoryCondition.Validate` 与
//      `卡组编辑.tsv` 的 `HasDeckCondition.Validate` 方法体早就逐句落地了，但**输入口从未被注入**
//      （全仓 `grep LiveOpsConditionBindings` 只有定义与 `Missing(...)`，**0 处赋值**）⇒ 生产上这两个条件
//      **恒 fail-closed**，与「原版会真判」不一致（B88-F8 因此把两条从 A 降成 B）。
//   做法：不新开注入总线（本批白名单里没有启动点），把默认实现直接挂在字段初始化上
//      （`LiveOpsConditionBindings.PlayerLevel = LiveOpsConditionBindingsLocal.PlayerLevel` …）——
//      仍然是**可注入**的（谁都可以再赋一个覆盖），但「什么都不做」时**已经是接上的**。
//   ⚠️ ~~另外三个口（`CheckTag` / `GetRawAttribute` / `EvaluationCheck`）**故意不接**~~ ——
//      🔴 **2026-10-28（B92·N1）改口径：已接**（见文件尾 `SegmentationControllerLocal` / `EvaluationLocal`）。
//      依据：`SegmentationController` / `Evaluation` 原版**真身都在**（`decomp_full/SegmentationController__*.c` +
//      `Evaluation__Check.c` 123 行可读），所以不再是「判据拿不到」，而是「**数据在远端**」
//      （`SegmentationController.Unpack(GetPlayerCombinedInfoResultPayload)` = PlayFab 玩家数据）
//      ⇒ 按铁律 18·0「只有服务器相关的才允许自己设计」给了本地模拟。
namespace CardPresentation
{
    /// <summary>三个「本地有数据源」的条件输入口的默认实现。判据与差异逐条写在每个成员上。</summary>
    public static class LiveOpsConditionBindingsLocal
    {
        /// <summary>原版 `PlayerDataManager.singletonManager.CurrentLevel`（= 玩家收藏等级）。
        /// 本地等价物 = `CardTierConfig.PlayerLevel`（`Shell/CardTierConfig.cs:203`，顶栏 / 档案页在用的那一个口）。
        /// <para>⚠️ **不是**「账号等级」（原版那个也来自服务器）；取值口径 = 同一份本地算出来的收藏等级。</para></summary>
        public static int PlayerLevel() { return CardTierConfig.PlayerLevel; }

        /// <summary>原版 `InventoryManager.HasItem(item)`：按 **`ObtainableItem` 的 runtime type** 选背包再查 id。
        /// 我们的等价物 = **把四份已登记背包逐个探一遍**（id 在各背包间唯一，逐探与「按类型选」同解）。
        /// <para>⚠️ 先摸一下 <see cref="Inventories.Cards"/>：四份背包是在那个类的**静态构造**里登记的
        /// （`Inventory.cs` 的 `static Inventories()`）—— 不先摸，`GetInventory&lt;T&gt;()` 会说「没登记」并报 Error。</para></summary>
        public static bool HasItemById(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            var _ = Inventories.Cards;                       // 触发静态构造 ⇒ 四份背包登记进 `InventoryManager`
            return InventoryManager.HasItemByID<RuleEngine.CardDef>(id)
                || InventoryManager.HasItemByID<ShopItem>(id)
                || InventoryManager.HasItemByID<AltArtUnlock>(id)
                || InventoryManager.HasItemByID<InventoryWildcard>(id);
        }

        /// <summary>原版 `InventoryManager.GetInventory&lt;CardDeck&gt;().GetWhere(d =&gt; d.prebuiltDeckId == targetId).Any()`
        /// —— 「玩家**拥有**这副预组卡组吗」。
        /// 🔶 自己设计（非原版）：本地判据 = 「`targetId` 在**本地预组目录**里」（`PrebuiltDecks.All` 的 `deckId`）。
        /// 为什么必须自己定：原版那一格是「活动发过给你的那副预组卡组实例」（`CardDeck.prebuiltDeckId`），
        /// 我们**没有预组卡组的持有/发放链** —— 项目边界是「卡池/预组全解锁」（CLAUDE.md §四 ②）⇒
        /// 「目录里有」= 「玩家拿得到」。⚠️ 差异：原版可以「目录里有但你这号没拿到」⇒ 本口会把那种档判成 true。
        /// 谁定的 + 日期：调度台 B90 2026-10-28。</summary>
        public static bool HasDeckByPrebuiltId(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return false;
            var all = PrebuiltDecks.All;
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].deckId == targetId) return true;
            return false;
        }
    }
}

// ============================================================ 🆕 2026-10-28（B90 · N3b，追加在文件尾）
//   `LiveOpsLocalEvent.GetTimeDescriptionTextKey()` —— 原版 `LiveOpsEvent.GetTimeDescriptionTextKey()`
//   （`d:/2/tools/decomp_full/Everguild.LiveOps.LiveOpsEvent__GetTimeDescriptionTextKey.c`：
//    `case 1 ⇒ TimeStartsIn 键 / case 2 ⇒ TimeEndsIn 键 / case 3 ⇒ TimeFinished 键 / 其余 ⇒ 空串`）。
//
//   为什么需要它：`LiveopMenuContainer.OnInitialize` 现在**写死「结束于」前缀**，原版按**状态**三档取前缀
//   ⇒ `OnPreview` 状态的活动卡会印错（应为「开始于」）。有了这一跳，调用点就能按状态取键。
//
//   ⚠️ **不另造键名**：三个键与本仓已有的 `LiveOpsEvent.GetTimeDescriptionTextKey()`
//   （`Shell/LiveOpsEvent.cs:1208`，另一条类层次 `LiveOpsEvent`）**逐字相同** ——
//   `Event_Description/General/{TimeStartsIn,TimeEndsIn,TimeFinished}`。
//   ⛔ 别在两份 partial 里各写一套键名（铁律 6：两处写同一条规则 = 迟早不一致）。
//   🔶 那份实现里已如实标注：「三个键名是按语义对位出来的、不是从字面量读出来的」（判据链见那边的 doc）。
//
//   ⚠️ 本方法与 `LiveOpsEvent` 那份的**唯一差别**：数据源是离线目录模型（`GetState` 已在本类，`Shell/LiveOpsState.cs:120`），
//   `LiveOpState` 的枚举值与原版逐值一致 ⇒ switch 可以逐档照抄。
namespace CardPresentation
{
    public sealed partial class LiveOpsLocalEvent
    {
        /// <summary>原版 `ILiveOps.GetTimeDescriptionTextKey()`：状态 → 词条键（**空串 = 这一档不显示前缀**）。
        /// <para>三档与键：`OnPreview(1)` → `Event_Description/General/TimeStartsIn` · `Active(2)` → `…/TimeEndsIn` ·
        /// `Finished(3)` → `…/TimeFinished` · `Planned(0)` / `Expired(4)` / `NotAvailable(-1)` → `""`。</para>
        /// <para>⚠️ 时钟走 <see cref="LiveOpsClock.NowUtc"/>（= 原版 `DateTime.Now` 的那个口，自检可用
        /// `LiveOpsClock.NowProvider` 钉死；⛔ 别在这类纯函数里直接读 `DateTime.UtcNow`，那样自检钉不住）。</para></summary>
        public string GetTimeDescriptionTextKey()
        {
            switch (GetState(LiveOpsClock.NowUtc))
            {
                case LiveOpState.OnPreview: return "Event_Description/General/TimeStartsIn";
                case LiveOpState.Active: return "Event_Description/General/TimeEndsIn";
                case LiveOpState.Finished: return "Event_Description/General/TimeFinished";
                default: return "";        // Planned / Expired / NotAvailable 都不显示前缀（原版同一句）
            }
        }
    }
}

// ============================================================ 🆕 2026-10-28（B92 · N1，追加在文件尾）
//   把 `LiveOpsConditionBindings` 最后三个口（`CheckTag` / `GetRawAttribute` / `EvaluationCheck`）接过本地数据源。
//
//   判据（**原版真身，全部现读，不是推定**）：
//     · `decomp_full/SegmentationController__CheckTag.c`（19 行）
//         ⇒ `return segmentation.tags(+0x10).Contains(tag)` —— 数据是**玩家 tag 集合**。
//     · `decomp_full/SegmentationController__GetRawAttribute.c`（50 行）
//         ⇒ 查 `segmentation.attributes(+0x18)` 那个字典；**取不到 ⇒ `Debug.LogError` + 返回 `string.Empty`**（照抄）。
//     · `decomp_full/SegmentationController__Unpack.c` 一族 / ILSpy `SegmentationController.cs`
//         ⇒ 数据源 = `Unpack(GetPlayerCombinedInfoResultPayload userInfo)`（**PlayFab 玩家数据**）
//           + `AddLocalAttributes(userInfo)`（tag 1 `ValueSpent` / 2 `DaysSinceLastLogin` / 9 `PlayerAgeInDays` /
//           10 `MatchesWon` —— 四格的输入**全部**来自 `userInfo` 的服务器字段）。
//     · `decomp_full/Evaluation__Check.c`（123 行，真体）—— 见 `EvaluationLocal.Check` 的逐 case 注记。
//
//   🔶 自己设计（非原版）：本地 `SegmentationController` 的**数据填充**。为什么必须自己定：
//      原版那两格（tags / attributes）唯一的**生产者**是 PlayFab 返回值（`Unpack` 的形参），**服务器已关服**
//      ⇒ 判据在远端、本地永远拿不到（铁律 18·0 允许自设计的唯一门类）。**机制**（contains / lookup / 缺格返回空串）
//      照抄原版，**数据**只能本地模拟。
//      谁定的 + 日期：调度台 B92 2026-10-28。
//   ⚠️ 今天本地**只注册得出一个 tag**：`CollectedFirstDeck(0)`（教程线 E01 `SelectArmy` 完成位 ——
//      判据与 `MenuTutorialManager.ShouldGetInitialDeck()` 用的是**同一条**，⛔ 别另开一份存档）。
//      其余 tag / 四个 attribute 的输入都是服务器字段 ⇒ **不注册**（= 原版「PlayFab 里没有这个 tag」时的同一结果）。
namespace CardPresentation
{
    /// <summary>原版 `SegmentationController`（`SingletonBehaviour&lt;SegmentationController&gt;`）的本地等价物。
    /// <para>🔴 只有**机制**照抄原版：「tag 集合 + 属性表」，`CheckTag` = 集合包含、`GetRawAttribute` = 查表
    /// （缺格 ⇒ 出声 + `string.Empty`）。**数据填充是本地模拟**（见文件尾大注释的 🔶）。</para>
    /// <para>⚠️ 原版 `tags` / `attributes` 的键是 `PlayerTag` 枚举；本地**没有那个枚举类**（全仓 0 个）
    /// ⇒ 一律用 `int`（枚举值逐值来自 `cpp2il/out_cpp2il_dev_ilspy/PlayerTag.cs`：
    /// `CollectedFirstDeck=0 · ValueSpent=1 · DaysSinceLastLogin=2 · Spender=3 · Mobile=4 · IOSDevice=5 ·
    /// AndroidDevice=6 · playfab=7 · steam=8 · PlayerAgeInDays=9 · MatchesWon=10 · CompletedDailyLogin=11 ·
    /// FinishedFTUETutorial=20 · FinishedOnboarding=50 · ConvertedSkirmishDeck=55 · mobile=99`）。</para></summary>
    public static class SegmentationControllerLocal
    {
        /// <summary>原版 `PlayerTag.CollectedFirstDeck = 0`（`PlayerTag.cs:3`）。</summary>
        public const int TagCollectedFirstDeck = 0;
        /// <summary>原版 `PlayerTag.ValueSpent = 1`。</summary>
        public const int TagValueSpent = 1;
        /// <summary>原版 `PlayerTag.DaysSinceLastLogin = 2`。</summary>
        public const int TagDaysSinceLastLogin = 2;
        /// <summary>原版 `PlayerTag.PlayerAgeInDays = 9`。</summary>
        public const int TagPlayerAgeInDays = 9;
        /// <summary>原版 `PlayerTag.MatchesWon = 10`。</summary>
        public const int TagMatchesWon = 10;

        static readonly System.Collections.Generic.HashSet<int> _tags = new System.Collections.Generic.HashSet<int>();
        static readonly System.Collections.Generic.Dictionary<int, string> _attributes =
            new System.Collections.Generic.Dictionary<int, string>();

        /// <summary>原版 `SegmentationController.CheckTag(PlayerTag)`：`tags.Contains(tag)`（`.c` 逐句对过）。
        /// ⚠️ 原版 `segmentation == null` ⇒ NRE；本地没有「还没 Unpack」这一档（集合恒存在，空集合 = 没标签）。</summary>
        public static bool CheckTag(int tag) { return _tags.Contains(tag); }

        /// <summary>原版 `SegmentationController.GetRawAttribute(PlayerTag)`（`.c` 逐句对过）：
        /// 查属性字典；**取不到 ⇒ `Debug.LogError` 后返回 `string.Empty`** —— 照抄（⛔ 别改成 null）。
        /// ⚠️ 差异：原版返回 `object`（数字也原样返回），本地只有 `string` 这一个载体
        /// ⇒ 「原版返回 boxed int / 我们返回它的十进制串」在 `Evaluation.Check` 里等价（那边一律 `TryParse`）。</summary>
        public static string GetRawAttribute(int tag)
        {
            string v;
            if (_attributes.TryGetValue(tag, out v)) return v;
            Debug.LogError("[Segmentation] GetRawAttribute：没有 tag " + tag + " 的属性"
                         + "（原版 `SegmentationController__GetRawAttribute.c:23-32`：字典取不到 ⇒ LogError + 返回空串）");
            return string.Empty;
        }

        /// <summary>原版 `SegmentationController.AddTag(PlayerTag)`：加进 tag 集合。
        /// ⚠️ 原版第二参（`AddTag` 的 `.c` 里那个 int）我们没有对应语义，只留 tag 一个参数。</summary>
        public static void AddTag(int tag) { _tags.Add(tag); }

        /// <summary>原版 `SegmentationController.RemoveTag(PlayerTag)`。</summary>
        public static void RemoveTag(int tag) { _tags.Remove(tag); }

        /// <summary>原版 `SegmentationController.SetAttribute(PlayerTag, object)`。</summary>
        public static void SetAttribute(int tag, string value)
        {
            if (value == null) { _attributes.Remove(tag); return; }
            _attributes[tag] = value;
        }

        /// <summary>原版 `SegmentationController.IncreaseAttribute`：数值累加（`int.TryParse` 不计入 ⇒ 出声）。</summary>
        public static void IncreaseAttribute(int tag, int delta)
        {
            string cur;
            int v = 0;
            if (!_attributes.TryGetValue(tag, out cur) || !int.TryParse(cur, out v)) v = 0;
            _attributes[tag] = (v + delta).ToString();
        }

        /// <summary>本地能推出来的那些 tag / attribute（见文件尾 🔶 那段的清单）。
        /// 🔴 由 <see cref="CheckTag"/> / <see cref="GetRawAttribute"/> **首次调用时懒执行一次**
        /// （原版同样是「登录时 `Unpack` 算一次」，不是每次查询重算）。改本地状态后要它跟着变就显式再调一次。</summary>
        public static void AddLocalTags()
        {
            // 原版 `PlayerTag.CollectedFirstDeck`：唯一的本地等价物 = 教程线 E01 `SelectArmy` 完成位
            // （`MenuTutorialManager.ShouldGetInitialDeck()` = `!SegmentationController.CheckTag(CollectedFirstDeck)`，
            //  见 `Shell/MenuTutorialManager.cs:317-336` —— 两处**必须同源**，⛔ 别另开一份存档）。
            if (!MenuTutorialManager.ShouldGetInitialDeck()) AddTag(TagCollectedFirstDeck);
            else RemoveTag(TagCollectedFirstDeck);
        }

        /// <summary>自检口：清空（并把「懒执行」标记复位）。</summary>
        public static void ResetForTest()
        {
            _tags.Clear(); _attributes.Clear(); _localApplied = false;
        }

        static bool _localApplied;

        /// <summary>懒执行 `AddLocalTags()`（只跑一次）。</summary>
        internal static void EnsureLocal()
        {
            if (_localApplied) return;
            _localApplied = true;
            AddLocalTags();
        }

        /// <summary>`LiveOpsConditionBindings.CheckTag` 的默认实现（外面套一层懒执行）。</summary>
        public static bool CheckTagPort(int tag) { EnsureLocal(); return CheckTag(tag); }

        /// <summary>`LiveOpsConditionBindings.GetRawAttribute` 的默认实现（外面套一层懒执行）。</summary>
        public static string GetRawAttributePort(int tag) { EnsureLocal(); return GetRawAttribute(tag); }
    }

    /// <summary>原版 `Evaluation.Check(object x, object y, Operation operation)` 的等价实现。
    /// <para>判据 = `decomp_full/Evaluation__Check.c`（**123 行真体**，逐 case 对过）＋
    /// ILSpy `Evaluation.cs` 的 `Operation` 枚举（`LessThan=0 · LessThanEqualTo=1 · Equals=2 · NotEquals=3 ·
    /// GreaterThan=4 · GreaterThanEqualTo=5 · Contains=6 · DontContain=7`）。</para>
    /// <para>🔴 **三处反直觉，全部照抄**：
    ///  ① `Equals`/`NotEquals` 判的是 **引用相等**（`.c:55` `bVar9 = param_1 == param_2`）—— 两个内容相同的
    ///    不同字符串对象在原版里判 **false**。⛔ 别「修好」成 `string.Equals`。
    ///  ② 只有 0/1/4/5 比数值，且**两侧都 `float.TryParse`**（`Single.TryParse`，当前区域性）；
    ///    任一侧解析失败 ⇒ `CustomDebug.LogError("Cannot evaluate objects of type {0} and {1}")` + **false**（`.c:98-103`）。
    ///  ③ `Contains`/`DontContain` 走 `string.Contains`；区别只由最后那句 `return has ^ (op != 6)` 决定。</para></summary>
    public static class EvaluationLocal
    {
        /// <summary>`Operation.LessThan`</summary>
        public const int OpLessThan = 0;
        /// <summary>`Operation.LessThanEqualTo`</summary>
        public const int OpLessThanEqualTo = 1;
        /// <summary>`Operation.Equals`</summary>
        public const int OpEquals = 2;
        /// <summary>`Operation.NotEquals`</summary>
        public const int OpNotEquals = 3;
        /// <summary>`Operation.GreaterThan`</summary>
        public const int OpGreaterThan = 4;
        /// <summary>`Operation.GreaterThanEqualTo`</summary>
        public const int OpGreaterThanEqualTo = 5;
        /// <summary>`Operation.Contains`</summary>
        public const int OpContains = 6;
        /// <summary>`Operation.DontContain`</summary>
        public const int OpDontContain = 7;

        /// <summary>原版 `Evaluation.Check(x, y, operation)`（`.c` 逐 case 对过）。
        /// `x` / `y` 在原版是 `object`；本地载体统一是 `string`（见 `GetRawAttribute` 的差异注记）。</summary>
        public static bool Check(string x, string y, int operation)
        {
            if (operation == OpContains || operation == OpDontContain)
            {
                // .c:25-52 —— 两个都非 null 才走；原版 null ⇒ 抛 NRE。
                if (x == null || y == null)
                {
                    Debug.LogError("[Evaluation] Check：Contains/DontContain 的实参是 null（原版这一跳 NRE）⇒ false");
                    return false;
                }
                bool has = x.Contains(y);
                return has ^ (operation != OpContains);
            }
            if (operation == OpEquals || operation == OpNotEquals)
            {
                // .c:54-59 —— 🔴 **引用相等**，不是值相等（照抄，⛔ 别修）。
                bool same = ReferenceEquals(x, y);
                return same ^ (operation != OpEquals);
            }
            // .c:60-118 —— 数值档：两侧都 Single.TryParse，任一侧失败 ⇒ LogError + false。
            float fx, fy;
            bool okx = float.TryParse(x, out fx);
            bool oky = float.TryParse(y, out fy);
            if (!okx || !oky)
            {
                Debug.LogError("[Evaluation] Cannot evaluate objects of type "
                             + (x == null ? "<null>" : "System.String") + " and "
                             + (y == null ? "<null>" : "System.String")
                             + "（原版 `Evaluation__Check.c:98-103`：TryParse 失败就 LogError + false）");
                return false;
            }
            switch (operation)
            {
                case OpLessThan: return fx < fy;
                case OpLessThanEqualTo: return fx <= fy;
                case OpGreaterThan: return fy < fx;            // .c:110-111 —— 注意是 y < x
                case OpGreaterThanEqualTo: return fy <= fx;    // .c:112-113 —— 注意是 y <= x
            }
            return false;                                       // 未知 operation ⇒ 原版也走 `return false`（.c:116）
        }
    }
}
