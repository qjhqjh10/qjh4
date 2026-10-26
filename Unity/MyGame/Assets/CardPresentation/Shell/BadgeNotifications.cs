// BadgeNotifications.cs — 红点 / 通知徽记（原版 `Everguild.UI.Notifications` + `UiBadgeNotification` 的复刻）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【这一层原来完全没有】我们以前只有 `MainMenuRuntime.BadgeAlpha(bool)`（alpha 0/1 **瞬切**）
//   + `DailyData.InboxHasBadge / RewardsHasBadge` 两个 bool。原版是**一整套**：
//
//   ① **通知（Notification）**：`Everguild.UI.Notifications` 命名空间下一族 record
//      （`InboxBadge` / `CollectionBadge` / `ShopBadge` / `InventoryBadge` / `RewardsBadge` /
//       `SocialBadge` …），`RewardsBadge` 带 `parentId`，`MissionsBadge : RewardsBadge`（**派生**）。
//   ② **提供者（INotificationProvider）**：`NotificationType` + `CheckNotification()`（返回 0..N 条）
//      + `ChildProviders`（递归）。真身 = `Inbox` / `Missions` / `Shops` / `InventoryManager` /
//      `CampaignHandler` / `ForgeHandler` / `AlliancesManager` / `ChatGlobalManager` 等。
//   ③ **聚合器（UiBadgeNotificationManager）**：把全部 provider（含子 provider）的结果收进
//      `Dictionary<Type, IReadOnlyList<Notification>> Notifications`，再**按类型**广播 `OnNotify`。
//   ④ **徽记（UiBadgeNotification）**：挂在一个 35×35 的红点节点上，持有若干
//      `BadgeNotificationDefinition{ notificationType, useFilter, filterCriteria }`；
//      计数 = Σ over `Notifications` 里**与该类型 `IsRelated`** 的那些、再过 `def.Validate(n)`；
//      `count > 0` ⇒ `Show()`（文本框 = 计数、alpha → 1）；否则 `Hide()`（文本清空、alpha → 0）。
//      alpha 以 **3/s** 逼近，差 ≤ **0.01** 时贴到目标并停更（`Update` 里 `enabled = false`）。
//   ⑤ **类型继承就是「层级怎么传」**：定义写 `RewardsBadge`、provider 发 `MissionsBadge`（派生）
//      ⇒ `RewardsBadge.IsAssignableFrom(MissionsBadge) == true` ⇒ **同一条红点也算它**。
//
// 【原版场景里的**硬接线**（逐份现读 `bundle_scenes_scenes_mainmenuwarpforge`，⛔ 别猜）】
//   | 红点节点 | 父节点 | notificationDefinitions |
//   |---|---|---|
//   | `Badge Highlight`(455) | `Main Menu Navigation Button - Home` | **空**（永不亮） |
//   | `Badge Highlight`(567) | `… - Collection` | `CollectionBadge` |
//   | `Badge Highlight`(568) | `… - Shop` | `ShopBadge` + `InventoryBadge` |
//   | `Badge Highlight`(569) | `… - Rewards` | `RewardsBadge`（**useFilter=1**，filter.parentId = **""**） |
//   | `Badge Highlight`(570) | `… - Social` | `SocialBadge` |
//   | `Badge Highlight`(506) | `SettingsBtn`（顶栏齿轮） | **空**（永不亮） |
//   | `Badge Highlight`(26) | 顶栏信封（Inbox） | `InboxBadge` |
//   （出处：各 MB 的 `notificationDefinitions`，`MonoBehaviour_2400.json` 里还能看到
//    `references.RefIds` 的 `RewardsBadge{parentId:""}` 预设 —— 这就是 filter 的真值。）
//
// 【计数那行字（原版 `OneText`，判据逐字段现读）】
//   `RectTransform` 35×35 · `ap (0, −0.94)` · pivot 居中；
//   TMP：`m_text ""` · `m_fontSize 34` · `m_fontSizeBase 36` · `m_fontColor (1,1,1,1)` ·
//   H=**2（居中）** · V=**512（Middle）** · autoSizing **1** · `[18, 34]` · `m_RaycastTarget 1`。
//   ⚠️ 我们这块 `Label` 是「以 anchor 定位的**居中网格**」（`Battle/Label.cs` 的 `RefreshBounds`）
//   ⇒ 把 35×35 的 rect 交给 `MenuDraw.Text` 就等价于原版的 H=2/V=512，**不需要**再设 TMP alignment。
//
// 【与原版**有意不同**的一处（如实记）】
//   **刷新时机**：原版由各 provider 在自己的数据变化时**主动**调 `UiBadgeNotificationManager.Refresh()`；
//   我们的数据源（`DailyData` 等）属于**用户已暂停的日常线、只读** ⇒ 不能去加推送点。
//   ⇒ 改成「**读时刷新**」：`UiBadgeNotification.Recount()` 先 `Refresh()` 再从缓存数（幂等、便宜）。
//   语义等价（数据变了，下一次 UI 问它就正确），但**没有**「数据一变、屏幕上的红点立刻自己消失」这条推送。
//   将来日常线恢复、能改 provider 时，把 `Refresh()` 的调用点挪回 provider 即可（`ShellRuntime.RefreshBadges()` 已留口）。
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    // ================================================================ ① 通知（类型即语义）

    /// <summary>通知基类（原版 `Everguild.UI.Notifications.Notification`，abstract record）。</summary>
    public abstract class BadgeNotification
    {
        /// <summary>原版 `Validate(Notification other)`：默认 = **同类型**。</summary>
        public virtual bool Validate(BadgeNotification other)
        {
            return other != null && other.GetType() == GetType();
        }
    }

    public sealed class InboxBadge : BadgeNotification { }
    public sealed class CollectionBadge : BadgeNotification { }
    public sealed class ShopBadge : BadgeNotification { }
    public sealed class InventoryBadge : BadgeNotification { }
    public sealed class SocialBadge : BadgeNotification { }
    public sealed class ForgeBadge : BadgeNotification { }
    public sealed class CampaignBadge : BadgeNotification { }
    public sealed class FriendsBadge : BadgeNotification { }
    public sealed class AlliancesBadge : BadgeNotification { }
    public sealed class SupportBadge : BadgeNotification { }

    /// <summary>原版 `RewardsBadge`：带 `parentId`，**过滤语义 = parentId 相等**
    /// （`RewardsBadge__Validate.c`：`return this.parentId == ((RewardsBadge)other).parentId`）。</summary>
    public class RewardsBadge : BadgeNotification
    {
        public string parentId = "";

        public override bool Validate(BadgeNotification other)
        {
            var r = other as RewardsBadge;      // ⚠️ 派生类（`MissionsBadge`）也走这一支
            if (r == null) return false;
            return string.Equals(r.parentId, parentId, StringComparison.Ordinal);
        }
    }

    /// <summary>原版 `MissionsBadge : RewardsBadge` —— **派生关系就是「层级传递」那一条**（见文件头 ⑤）。</summary>
    public sealed class MissionsBadge : RewardsBadge { }

    // ================================================================ ② 提供者

    /// <summary>原版 `INotificationProvider&lt;out T&gt;`。</summary>
    public interface INotificationProvider
    {
        Type NotificationType { get; }
        IEnumerable<BadgeNotification> CheckNotification();
        IEnumerable<INotificationProvider> ChildProviders { get; }
    }

    // ================================================================ ③ 聚合器

    /// <summary>原版 `UiBadgeNotificationManager`（静态聚合 + 按类型广播）。</summary>
    public static class UiBadgeNotificationManager
    {
        static readonly Dictionary<Type, List<BadgeNotification>> _notifications = new Dictionary<Type, List<BadgeNotification>>();
        static readonly List<INotificationProvider> _providers = new List<INotificationProvider>();

        /// <summary>原版 `OnNotify`（`Action&lt;Type&gt;`）：某一类通知重算完就广播一次。</summary>
        public static event Action<Type> OnNotify;

        /// <summary>自检读口：当前聚合表（原版 `Notifications`）。</summary>
        public static IReadOnlyDictionary<Type, List<BadgeNotification>> Notifications { get { return _notifications; } }

        /// <summary>自检读口：已登记 provider 数。</summary>
        public static int ProviderCount { get { return _providers.Count; } }

        public static void Register(INotificationProvider provider)
        {
            if (provider == null) return;
            if (!_providers.Contains(provider)) _providers.Add(provider);
            Refresh(provider.NotificationType);
        }
        public static void Unregister(INotificationProvider provider)
        {
            if (provider == null) return;
            _providers.Remove(provider);
        }

        /// <summary>原版 `Refresh()`：清表 → 逐 provider（含子 provider）重收 → 每个类型广播一次。
        /// <para>⚠️ `notify:false` = **只重算、不广播** —— 给「读时刷新」用（见文件头「有意不同」那一节：
        /// 若这里也广播，`UiBadgeNotification.Recount → Refresh → OnNotify → Recount` 会**无限递归**）。</para></summary>
        public static void Refresh(bool notify = true)
        {
            _notifications.Clear();
            for (int i = 0; i < _providers.Count; i++) Collect(_providers[i]);
            if (!notify || OnNotify == null) return;
            var keys = new List<Type>(_notifications.Keys);
            for (int i = 0; i < keys.Count; i++) { var h = OnNotify; if (h != null) h(keys[i]); }
        }

        /// <summary>原版 `Refresh(Type)`：只重算「与 `type` 相关」的那几栏。</summary>
        public static void Refresh(Type type)
        {
            if (type == null) return;
            // 相关 = 任意方向可赋值（provider 发派生类、定义写基类，或反过来）
            var related = new List<INotificationProvider>();
            for (int i = 0; i < _providers.Count; i++)
            {
                var p = _providers[i];
                if (p != null && IsRelated(p.NotificationType, type)) related.Add(p);
            }
            if (related.Count == 0) { if (OnNotify != null) { var h = OnNotify; if (h != null) h(type); } return; }
            for (int i = 0; i < related.Count; i++)
            {
                var p = related[i];
                if (p == null) continue;
                _notifications[p.NotificationType] = Snapshot(p);
            }
            if (OnNotify != null) { var h = OnNotify; if (h != null) h(type); }
        }

        /// <summary>原版 `CleanUp()`：清聚合表（**不动 provider 名单**）。</summary>
        public static void CleanUp() { _notifications.Clear(); }

        /// <summary>原版 `GetCount(List&lt;BadgeNotificationDefinition&gt;)`：见文件头 ④。</summary>
        public static int GetCount(List<BadgeNotificationDefinition> definitions)
        {
            if (definitions == null || definitions.Count == 0) return 0;
            int n = 0;
            foreach (var kv in _notifications)
            {
                if (kv.Key == null || kv.Value == null) continue;
                for (int d = 0; d < definitions.Count; d++)
                {
                    var def = definitions[d];
                    if (def == null || def.Type == null) continue;
                    if (!IsRelated(def.Type, kv.Key)) continue;
                    for (int i = 0; i < kv.Value.Count; i++)
                    {
                        var item = kv.Value[i];
                        if (item != null && def.Validate(item)) n++;
                    }
                }
            }
            return n;
        }

        /// <summary>原版 `ReflectionHelper.IsRelated`：**任意方向可赋值**（派生类通知算进基类定义）。</summary>
        public static bool IsRelated(Type a, Type b)
        {
            if (a == null || b == null) return false;
            return a.IsAssignableFrom(b) || b.IsAssignableFrom(a);
        }

        static void Collect(INotificationProvider p)
        {
            if (p == null) return;
            _notifications[p.NotificationType] = Snapshot(p);
            var children = p.ChildProviders;
            if (children == null) return;
            foreach (var c in children) Collect(c);       // 递归（原版 `SelectMany` 展开子 provider）
        }

        static List<BadgeNotification> Snapshot(INotificationProvider p)
        {
            var list = new List<BadgeNotification>();
            var items = p.CheckNotification();
            if (items == null) return list;
            foreach (var it in items) if (it != null) list.Add(it);
            return list;
        }

        /// <summary>自检用：清空 provider 与缓存（跨用例隔离；原版无此口）。</summary>
        public static void ResetForTest()
        {
            _providers.Clear();
            _notifications.Clear();
            OnNotify = null;
        }
    }

    // ============ ④ 徽记定义（组件 `UiBadgeNotification` 见同目录 `UiBadgeNotification.cs`）

    /// <summary>原版 `BadgeNotificationDefinition`：一条定义 = 一类通知 + 可选过滤器。</summary>
    public sealed class BadgeNotificationDefinition
    {
        public Type notificationType;
        public bool useFilter;
        public BadgeNotification filterCriteria;

        public Type Type { get { return notificationType; } }

        public BadgeNotificationDefinition() { }
        public BadgeNotificationDefinition(Type type, bool useFilter = false, BadgeNotification filter = null)
        { notificationType = type; this.useFilter = useFilter; filterCriteria = filter; }

        /// <summary>原版 `Validate(Notification n)`：不带过滤 ⇒ 同类型；带过滤 ⇒ `n.Validate(filterCriteria)`。</summary>
        public bool Validate(BadgeNotification n)
        {
            if (n == null || notificationType == null) return false;
            if (!useFilter) return notificationType.IsInstanceOfType(n);
            if (filterCriteria == null) return notificationType.IsInstanceOfType(n);
            return n.Validate(filterCriteria);
        }
    }

    // ================================================================ ⑤ 我们的 provider（数据源）

    /// <summary>收件箱（原版 `Inbox.CheckNotification` = **未读条数**）。
    /// 🔴 **2026-10-25（N17）就地订正（铁律 5）**：这里原来写「单机恒 0 ⇒ 永不亮，**不是省略**」——
    /// **已过期**：收件箱现在有本地消息源（`Shell/InboxData.cs`），`DailyData.InboxCount` = 未读条数
    /// （`InboxData.UnreadCount()`）⇒ 有未读就亮、点开即灭（`InboxData.MarkRead`）。</summary>
    public sealed class InboxNotificationProvider : INotificationProvider
    {
        public Type NotificationType { get { return typeof(InboxBadge); } }
        public IEnumerable<INotificationProvider> ChildProviders { get { return null; } }
        public IEnumerable<BadgeNotification> CheckNotification()
        {
            int n = DailyData.InboxCount;
            for (int i = 0; i < n; i++) yield return new InboxBadge();
        }
    }

    /// <summary>奖励（原版 `Missions.CheckNotification` → **每一条可领取的 mission 发一条** `RewardsBadge`；
    /// 菜单那颗红点的定义再按 `parentId == ""` 过滤 —— 见文件头那张接线表）。
    /// <para>🔴 **2026-10-25（N17）**：计数改成**一处** —— `DailyData.CollectableMissionCount()`。
    /// 旧写法是在这里 `for (i &lt; DailyTaskCount) CanCollectDaily(i)`（只数三条每日）⇒ 周常 / 骷髅 / 登录
    /// 可领时红点不亮；而且那个 `DailyTaskCount = 3` 是**写死的**（`DailyData._daily` 私有）。
    /// 现在两半都消掉：条数由 `DailyData.DailyTaskCount` 现读，口径由 `CollectableMissionCount` 与
    /// `DailyData.RewardsHasBadge` **共用一份**（⛔ 别再在这里另写一遍循环）。</para></summary>
    public sealed class RewardsNotificationProvider : INotificationProvider
    {
        static int _lastSeenCollectable = -1;

        public Type NotificationType { get { return typeof(RewardsBadge); } }
        public IEnumerable<INotificationProvider> ChildProviders { get { return null; } }

        public IEnumerable<BadgeNotification> CheckNotification()
        {
            int n = DailyData.CollectableMissionCount();
            for (int i = 0; i < n; i++) yield return new RewardsBadge { parentId = "" };
            if (_lastSeenCollectable >= 0 && n != _lastSeenCollectable)
                Debug.Log("[Badges] 可领任务数变了：" + _lastSeenCollectable + " → " + n
                        + "（" + DailyData.DailyTaskCount + " 条每日 + 周常/骷髅/登录；按读时刷新，界面下次问就更新）");
            _lastSeenCollectable = n;
        }
    }

    /// <summary>战役红点（原版 `CampaignHandler.CheckNotification`，`Everguild.LiveOps/CampaignHandler.cs:151-230`）：
    /// 取当前所选阵营的战役（取不到再试 `army + 10`）；只要它有**可用且可领**的节点 ⇒ **发【一条】** `CampaignBadge`
    /// （数量恒 1，⛔ 不是每节点一条 —— 原版就是 `new CampaignBadge(SelectedArmy)` 一次）。
    /// <para>⚠️ **战役进度现在不落盘**（重开回「只解锁根节点」）⇒ 本次会话里领完根节点后红点会灭，重开又会亮；
    /// 这是**存档策略**造成的，不是红点本身的错（如实记，别为它编一个假源）。</para>
    /// <para>⚠️ `parentId = "CampaignMission"` 的那条 `MissionsBadge`（战役任务）**今天没有源** ⇒ 不登记假 provider。</para></summary>
    public sealed class CampaignNotificationProvider : INotificationProvider
    {
        public Type NotificationType { get { return typeof(CampaignBadge); } }
        public IEnumerable<INotificationProvider> ChildProviders { get { return null; } }
        public IEnumerable<BadgeNotification> CheckNotification()
        {
            if (!CampaignData.HasContent(CampaignData.Selected)) yield break;
            for (int i = 0; i < CampaignData.NodeCount; i++)
            {
                if (CampaignData.Claimable(i))
                {
                    yield return new CampaignBadge();
                    yield break;                 // 原版：一个战役只发一条
                }
            }
        }
    }

    /// <summary>锻造红点（原版 `ForgeHandler.CheckNotification`，`Everguild.LiveOps/ForgeHandler.cs:486` +
    /// `_<>c__DisplayClass9_0.<CheckNotification>b__9_0`）：把**每个阵营**的 `GetRewardsToCollect()` 数量摊平
    /// ⇒ **一个可领奖励 = 一条 `ForgeBadge`**（所以计数 = **全部阵营可领奖励总数**）。
    /// <para>我们：`ForgeData.StateAt(army, level) == ForgeData.ToCollect` 逐格数。</para></summary>
    public sealed class ForgeNotificationProvider : INotificationProvider
    {
        public Type NotificationType { get { return typeof(ForgeBadge); } }
        public IEnumerable<INotificationProvider> ChildProviders { get { return null; } }
        public IEnumerable<BadgeNotification> CheckNotification()
        {
            var armies = ForgeData.Armies;
            for (int a = 0; a < armies.Length; a++)
            {
                string army = armies[a];
                for (int lv = 0; lv < ForgeData.MaxLevel; lv++)
                    if (ForgeData.StateAt(army, lv) == ForgeData.ToCollect)
                        yield return new ForgeBadge();
            }
        }
    }

    // ================================================================ ⑥ 接线

    /// <summary>红点接线：provider 登记 + 把 `UiBadgeNotification` 挂到我们那颗 35×35 quad 上。</summary>
    public static class BadgeWiring
    {
        /// <summary>登记本工程**有真实数据源**的 provider（幂等；壳重建时不会重复登记）。
        /// <para>⚠️ 没有源的四类（`CollectionBadge` / `ShopBadge`+`InventoryBadge` / `SocialBadge` /
        /// `InboxBadge` 的计数）**不登记假 provider**：定义照原版挂上 ⇒ 计数恒 0 ⇒ 红点恒隐藏
        /// （= 我们今天的行为；原版这四类由服务端/本地「新卡/新商品/好友请求」驱动，本地没有那些源）。</para></summary>
        public static void RegisterDefaults()
        {
            if (UiBadgeNotificationManager.ProviderCount > 0) return;
            UiBadgeNotificationManager.Register(new InboxNotificationProvider());
            UiBadgeNotificationManager.Register(new RewardsNotificationProvider());
            // 🆕 2026-10-25（H · 第 2 轮）：战役那颗（`CampaignRewardsButton` 的 `Badge Highlight`，判据见 §接线表）
            UiBadgeNotificationManager.Register(new CampaignNotificationProvider());
            // 🆕 2026-10-25（H · 锻造账）：锻造那颗（`Forge Button` 的 `Badge Highlight`）
            UiBadgeNotificationManager.Register(new ForgeNotificationProvider());
        }

        /// <summary>按原版接线表建一条定义列表（场景里逐份现读，见文件头那张表）。</summary>
        public static List<BadgeNotificationDefinition> DefinitionsFor(string nav)
        {
            var defs = new List<BadgeNotificationDefinition>();
            switch (nav)
            {
                case "Home": break;                                             // 空（永不亮）
                case "Collection": defs.Add(new BadgeNotificationDefinition(typeof(CollectionBadge))); break;
                case "Shop":
                    defs.Add(new BadgeNotificationDefinition(typeof(ShopBadge)));
                    defs.Add(new BadgeNotificationDefinition(typeof(InventoryBadge)));
                    break;
                case "Rewards":
                    // useFilter = 1，filter = `RewardsBadge { parentId = "" }`（MB_2400 的 references.RefIds 实读）
                    defs.Add(new BadgeNotificationDefinition(typeof(RewardsBadge), useFilter: true,
                                                             filter: new RewardsBadge { parentId = "" }));
                    break;
                case "Social": defs.Add(new BadgeNotificationDefinition(typeof(SocialBadge))); break;
                case "Settings": break;                                         // 空（永不亮）
                case "Inbox": defs.Add(new BadgeNotificationDefinition(typeof(InboxBadge))); break;
            }
            return defs;
        }

        /// <summary>🆕 **2026-10-25（H · 第 2 轮）：左栏页签按钮上的红点定义**（逐份现读 `bundle_menus_assets_all`）。
        /// <para>判据（MB → 父节点，全是 `…/Content Area/Tab Buttons/<键>`）：
        /// · `CampaignRewardsButton` → **`CampaignBadge`（无过滤）+ `MissionsBadge`（useFilter=1 · `parentId="CampaignMission"`）**
        ///   （`MonoBehaviour_-287428095701543169.json` 的 `references.RefIds` 里就是那两个预设）；
        /// · `MissionsRewardsButton` → `MissionsBadge`（`MonoBehaviour_1604463168230790911.json`）；
        /// · `Forge Button` → `ForgeBadge`（`MonoBehaviour_8050322982303438591.json`）；
        /// · `Friends Tab Button` → `FriendsBadge`；`Alliances Tab Button` → `AlliancesNotificationBadge`。
        /// 其余键原版**没有**红点定义 ⇒ 传空表（调用方据此决定要不要挂）。</para>
        /// <para>⚠️ **2026-10-25 就地订正（铁律 5）**：原文写「我们只登记了 Campaign 的 provider；Missions/Forge/
        /// Friends/Alliances 四类本地没有数据源」—— **Forge 那一半已过期**：锻造账那一轮补了
        /// `ForgeNotificationProvider`（`ForgeData` 有真实状态，逐阵营数 `ToCollect`），`RegisterDefaults` 已登记。
        /// 仍然**没有数据源**的是 Missions / Friends / Alliances 三类（定义照挂、计数恒 0 ⇒ 恒隐藏；⛔ 不登记假 provider）。</para></summary>
        public static List<BadgeNotificationDefinition> DefinitionsForTab(string instId)
        {
            var defs = new List<BadgeNotificationDefinition>();
            if (string.IsNullOrEmpty(instId)) return defs;
            if (instId.IndexOf("CampaignButton", System.StringComparison.Ordinal) >= 0)
            {
                defs.Add(new BadgeNotificationDefinition(typeof(CampaignBadge)));
                defs.Add(new BadgeNotificationDefinition(typeof(MissionsBadge), useFilter: true,
                                                         filter: new MissionsBadge { parentId = "CampaignMission" }));
            }
            else if (instId.IndexOf("MissionsButton", System.StringComparison.Ordinal) >= 0)
                defs.Add(new BadgeNotificationDefinition(typeof(MissionsBadge)));
            else if (instId.IndexOf("ForgeButton", System.StringComparison.Ordinal) >= 0
                  || instId.IndexOf("Forge Button", System.StringComparison.Ordinal) >= 0)
                defs.Add(new BadgeNotificationDefinition(typeof(ForgeBadge)));
            else if (instId.IndexOf("Friends", System.StringComparison.Ordinal) >= 0)
                defs.Add(new BadgeNotificationDefinition(typeof(FriendsBadge)));
            else if (instId.IndexOf("Alliances", System.StringComparison.Ordinal) >= 0)
                defs.Add(new BadgeNotificationDefinition(typeof(AlliancesBadge)));
            return defs;
        }

        /// <summary>
        /// 把徽记挂到一颗红点 quad 上，并建原版那行**计数文字**（`OneText`，判据见文件头）。
        /// <para>🔴 **2026-10-25（复刻第 1 轮 · 代理 H）：`OneText` 的父级与原版【不同】，这是有意的。**
        /// 原版它是 `Badge Highlight` 的**子件**；我们把它建在**调用方给的那个父节点**下（导航钮 / 顶栏格），
        /// 与红点 quad **平级、同一 35×35 矩形** ⇒ **画面等价**（`Label` 是「以 anchor 定位的居中网格」，
        /// 矩形相同就落点相同）。
        /// <para>⛔ **别「照着原版层级改回子件」而不改坐标**：红点 quad 自己不在父原点（导航那颗在
        /// `(117.8..152.8, 91.7..126.7)` 那一带）⇒ 把字挂到它下面、`localPosition` 还是按页面坐标算的话，
        /// 字会**整体再偏一个红点中心**（静默、批处理里也看不出来）。
        /// 这条差异由 `Editor/MainMenuScene.cs` 的 `★ OneText 结构` 断言挡着（比「字世界中心 == 红点世界中心」）。</para></para>
        /// <para>⚠️ 顺序：先建字、再挂组件、最后 `Recount()` —— `Recount()` 会调 `Show/Hide` 写文本。</para>
        /// <para>⚠️ 批处理/编辑器建场时 `Update` 不跑 ⇒ 末尾 `Step(1f)` 把 alpha 直接贴到目标，存进场景的
        /// 红点才是**正确显隐**；Play 里不贴（保留原版那 0.33 s 淡入）。</para></summary>
        public static UiBadgeNotification Attach(ImageQuad quad, PxRect rect, Transform textParent, int qText,
                                                 Color tint, List<BadgeNotificationDefinition> definitions,
                                                 string nameForLog = null)
        {
            if (quad == null) return null;

            Label counter = null;
            if (textParent != null)
            {
                // 原版 `OneText`：35×35 · fs 34 · auto[18,34] · base 36 · 纯白 · 居中（我们的 `Label` 天然居中）
                counter = MenuDraw.Text(textParent, rect, "", Color.white, "OneText", 34f, qText,
                                        wrapPx: rect.W, autoMinPx: 18f, autoMaxPx: 34f, autoBasePx: 36f);
                if (counter == null)
                    Debug.LogWarning("[Badges] `" + (nameForLog ?? "Badge") + "` 的计数文字 `OneText` 没建出来"
                                   + "（`MenuDraw.Text` 返回 null）⇒ 红点只有底色、没有数字（出声，不静默）");
            }

            var comp = quad.gameObject.GetComponent<UiBadgeNotification>();
            if (comp == null) comp = quad.gameObject.AddComponent<UiBadgeNotification>();
            comp.notificationDefinitions = definitions ?? new List<BadgeNotificationDefinition>();
            comp.showCounter = true;

            var baseColor = new Color(tint.r, tint.g, tint.b, 1f);
            comp.SetAlpha = a =>
            {
                quad.SetTint(new Color(baseColor.r, baseColor.g, baseColor.b, a));
                if (counter != null) counter.SetColor(new Color(1f, 1f, 1f, a));
            };
            comp.SetCountText = s =>
            {
                if (counter != null) counter.SetText(s ?? "");
            };

            comp.SetAlpha(0f);          // 原版 `Awake` 第一句：`canvasGroup.alpha = 0`
            comp.Recount();
            if (!Application.isPlaying) comp.Step(1f);   // 批处理：直接贴到目标（见上面那条）
            return comp;
        }
    }
}
