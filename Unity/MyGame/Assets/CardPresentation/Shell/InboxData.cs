// InboxData.cs — 「收件箱」这一层的**本地消息源**（单机等价物）
//
// ============================ 这是**我们自建**的，不是原版的做法 ============================
// 🔴 原版这一整套是**服务端**的：消息由 `LiveOps` 的 `Inbox` handler（`Everguild.LiveOps/Inbox.cs`）
//    从远端事件表里取 `OfType<IInboxEvent>()` 得到；单机没有服务器 ⇒ **消息表由我们自建**。
//    `资料/完全复刻/判据/日常与活动.md` 与 `资料/日常_原版规格.md` §六 已定：本机只建空态；
//    2026-10-25 用户开线（「日常/活动线 779 条…开吧」）⇒ 本条把**本地等价数据源**补上。
//
// ✅ **照原版的（逐条，判据在下面每个方法上）**：
//   · 消息模型 = `BaseInboxEvent`（`GetTitle` / `GetMessage` / `GetRewards` / `IsRead` / `StartDate` /
//     `RedeemableAt` / `ClaimRewards`）—— 字段对位见 `Msg`。
//   · 排序 = `Inbox.InitializeHandler` 的 **`OrderBy(StartDate)`**（升序；列表那侧再按
//     `VerticalLayoutGroup.reverse=1` 倒排到界面上）。
//   · 已读 = `GameMessagePersistence.Openned`（`BaseInboxEvent.IsRead` 读的就是它）。
//   · 可领判据 = `MessageDisplay.CheckMessageCollectable`（逐支：`claimedAt > 0` / `!hasReward` ⇒ 否；
//     `RedeemableAt() > 服务器现在` ⇒ 否；否则是）。
//   · 领奖完成 = `BaseInboxEvent.<ClaimRewards>g__OnComplete|7_0` 的 `RewardService.Collect(rewards, …)`
//     ⇒ 我们的唯一发奖口 = `Wallet.Grant`（与 `DailyData` 那六条同一条路）+ `RewardWindow.ShowCollected`。
//   · 红点 = `Inbox.CheckNotification` = **未读条数**（`Repeat(new InboxBadge(), Count(!IsRead))`）
//     ⇒ 我们的落点 = `DailyData.InboxCount`（`InboxNotificationProvider` 读它）。
//   · 消息上限 = `Inbox.maxMessages`（`.ctor` 立即数 **15**）⇒ 超过就丢最旧的。
// ❌ **我们挑的**：出厂两条消息的**文案与奖励**（见 `_seed`）；原版内容由服务端下发，
//    本地拿不到 ⇒ 按铁律 3 如实标成「我们挑的」，⛔ 别把下面那两条当成原版公告。
//
// ⚠️ **落盘**：只落「已读 / 已领 / 领取时刻」三样（= 原版 `GameMessagePersistence` 的
//    `Openned` / `claimedAt`），**不落消息内容**（内容在代码里，是出厂表）。
//    形状照 `DailyData`：`OverrideDir` 注入临时档（自检一律走临时档）、`version` 不认识就出声回出厂、
//    tmp + 替换的原子写。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>收件箱的**本地消息源**。原版对照：`Everguild.LiveOps.Inbox`（handler）+ `BaseInboxEvent`（消息）。</summary>
    public static class InboxData
    {
        // ============================================================ 模型

        /// <summary>原版 `InboxMessageOrigin`（`News = 0` / `Lodge = 1`）。</summary>
        public enum Origin { News = 0, Lodge = 1 }

        /// <summary>一条消息从哪来（只影响我们这边的出处标注，不参与显示）。</summary>
        public enum Kind
        {
            /// <summary>`LocalInboxMessage`（离线唯一有内容的一路）。</summary>
            Local = 0,
            /// <summary>`AnnouncementEvent`（公告：标题/正文各取一条 `Event_Description` 词条）。</summary>
            Announcement = 1,
            /// <summary>`InboxMessage`（服务端投递的普通消息：本地没有实例）。</summary>
            Server = 2,
        }

        /// <summary>一条奖励（原版 `RewardInfo` 的最小形态：物品 id + 数量）。
        /// ⚠️ 我们只用到「币」这一档（`Wallet.Grant` 的键 = 图名），原因见 `Wallet` 的头注释。</summary>
        public sealed class Reward
        {
            public string Art;          // 原版 `ObtainableItem` 的 id（我们的钱包键 = 图名）
            public int Qty;
            public Reward(string art, int qty) { Art = art; Qty = qty; }
        }

        /// <summary>一条消息。字段对位 `BaseInboxEvent` / `GameMessageData` / `GameMessagePersistence`：
        /// <list type="bullet">
        /// <item>`Id` = `GetID()`（原版 `ILiveOps` 的 eventId）</item>
        /// <item>`TitleTerm`/`TitleText` = `GetTitle()`（`GetMessageContent().title`，含 `[[RewardName]]` 等替换口）</item>
        /// <item>`BodyTerm`/`BodyText` = `GetMessage()`（`GetMessageContent().message`）</item>
        /// <item>`StartMs` = `StartDate()`（毫秒）</item>
        /// <item>`RedeemableAtMs` = `RedeemableAt()`（毫秒；`0` = 立即可领）</item>
        /// <item>`ExpireMs` = 数据里的到期时刻（`0` = 不过期）；⚠️ 原版 `CheckMessageCollectable` 里
        ///       只拿 `RedeemableAt` 和服务器时间比，**没有**单独的到期门 ⇒ 我们也不额外加门。</item>
        /// <item>`Rewards` = `GetRewards()`（空 = 无奖可领）</item>
        /// <item>`Read` = `IsRead()`（= 存档里的 `Openned`）· `ClaimedAtMs` = 存档里的 `claimedAt`</item>
        /// </list></summary>
        public sealed class Msg
        {
            public string Id;
            public string TitleTerm, TitleText;
            public string BodyTerm, BodyText;
            public long StartMs;
            public long RedeemableAtMs;
            public long ExpireMs;
            public Reward[] Rewards;
            public Origin From;
            public Kind Kind;
            /// <summary>原版 `HeaderImage` / `FooterImage`（`EventAssetReferenceType` 3 / 4）。
            /// 本地 13 份 SO 的这两张图**没有导入**（判据 §五·1 的同一族）⇒ 恒 null ⇒ 那两件不建/关着，
            /// 但**保留字段**：将来导入了只填这里，视图那边不用改。</summary>
            public string HeaderArt, FooterArt;

            // ---- 存档态（只在 `EnsureLoaded` 之后有效）----
            public bool Read;
            public long ClaimedAtMs;         // > 0 = 已领

            public bool HasReward { get { return Rewards != null && Rewards.Length > 0; } }
            public bool IsClaimed { get { return ClaimedAtMs > 0; } }
        }

        // ============================================================ 出厂消息表（= 我们挑的）

        /// <summary>⚠️ **我们挑的**（判据为空：原版由服务端下发，本地没有那份数据）。
        /// 两条分别覆盖原版**两条真实来路**：
        /// · `LocalInboxMessage`（离线唯一有内容的一路，方法体极短、直接转 base）⇒ `local_welcome`（带奖励）
        /// · `AnnouncementEvent`（`GetTitle`/`GetMessage` 各取一条 `Event_Description` 词条）⇒ 最近一次
        ///   enabled 活动的公告（**无奖励**）。
        /// 其余 11 份 SO 的公告与第二条同标题同正文（Expansion Pass 一族）⇒ 不重复铺 11 条同名消息。</summary>
        static List<Msg> BuildSeed()
        {
            var list = new List<Msg>();

            // ---- ① 本地欢迎信（`LocalInboxMessage`）----
            // 判据：`LocalInboxMessage.cs:6` `ClaimRewards()` 直接 `base.ClaimRewards()` ⇒ 与普通消息同一条领取链。
            // 🔶 自己设计（非原版）：本地欢迎信的文案与 300 点奖励 · 原版这两项在服务端 / 词条表，本地没有 · TODO(谁定的)｜原注：⚠️ 文案与 300 点奖励都是**我们挑的**；奖励键 = 战役点图（与 `DailyData` 的三条日常同一张图）。
            list.Add(new Msg
            {
                Id = "local_welcome",
                TitleText = "Welcome to Warpforge",
                BodyText = "Welcome, Commander!\n\n"
                         + "Your campaign begins here. Claim your campaign points below, then open the "
                         + "Missions tab to see today's tasks and the weekly track.\n\n"
                         + "— The Warpforge team",
                StartMs = 1735689600000L,          // 2025-01-01 00:00 UTC（固定值：出厂消息不随时间漂）
                RedeemableAtMs = 0,
                ExpireMs = 0,
                Rewards = new[] { new Reward("40K_genearl_icon_Campaign_points_big", 300) },
                From = Origin.News,
                Kind = Kind.Local,
            });

            // ---- ② 公告（`AnnouncementEvent`）----
            // 判据：`AnnouncementEvent.GetTitle()` = `GetLabel(ev, Title)`、
            //       `GetMessage()` = `GetLabel(ev, Description)`（`AnnouncementEvent.cs:8,16`）。
            // 取**最近一次** enabled 活动（`StartMs` 最大者）当代表；它正是本地 13 份 SO 里最新的那一族。
            LiveOpsLocalEvent pick = null;
            var evs = LiveOpsLocalCatalog.Events;
            for (int i = 0; i < evs.Count; i++)
            {
                var e = evs[i];
                if (e == null || !e.Enabled) continue;
                LiveOpsLocalLabel tl;
                if (!e.TryGetLabel((int)LiveOpsLabelRef.Title, out tl) || tl == null) continue;
                if (string.IsNullOrEmpty(tl.Term)) continue;
                if (pick == null || e.StartMs > pick.StartMs) pick = e;
            }
            if (pick != null)
            {
                LiveOpsLocalLabel tl, dl;
                pick.TryGetLabel((int)LiveOpsLabelRef.Title, out tl);
                pick.TryGetLabel((int)LiveOpsLabelRef.Description, out dl);
                list.Add(new Msg
                {
                    Id = "announce_" + pick.EventId,
                    TitleTerm = tl != null ? tl.Term : null,
                    TitleText = tl != null ? LiveOpsLocalCatalog.FillArgs(tl.Term, LiveOpsLocalCatalog.ResolveArgs(tl.Args)) : "",
                    BodyTerm = dl != null ? dl.Term : null,
                    BodyText = dl != null ? LiveOpsLocalCatalog.FillArgs(dl.Term, LiveOpsLocalCatalog.ResolveArgs(dl.Args)) : "",
                    StartMs = pick.StartMs,
                    RedeemableAtMs = 0,
                    ExpireMs = 0,
                    Rewards = null,                        // 公告无奖励（`BaseInboxEvent.GetRewards()` 默认空数组）
                    From = Origin.News,
                    Kind = Kind.Announcement,
                });
            }

            // 原版 `Inbox.InitializeHandler` = **`OrderBy(StartDate)`**（`Inbox.cs` 的 MoveNext 里那条
            // `Enumerable.OrderBy(source, keySelector)`）⇒ 出厂表也按它排一次，别指望手写顺序。
            list.Sort((a, b) => a.StartMs.CompareTo(b.StartMs));
            return list;
        }

        // ============================================================ 运行时状态

        static readonly List<Msg> _messages = BuildSeed();
        static readonly Dictionary<string, Msg> _byId = new Dictionary<string, Msg>();

        static InboxData()
        {
            for (int i = 0; i < _messages.Count; i++)
                if (!string.IsNullOrEmpty(_messages[i].Id)) _byId[_messages[i].Id] = _messages[i];
            EnsureLoaded();
        }

        /// <summary>原版 `Inbox.Messages`（`OfType<IInboxEvent>()`）—— 已按 `StartDate` 升序。
        /// 视图那一侧**只读**。</summary>
        public static IReadOnlyList<Msg> Messages { get { return _messages; } }
        public static int Count { get { return _messages.Count; } }

        /// <summary>原版 `Inbox.CheckNotification()` = `Count(x =&gt; !x.IsRead())`（未读条数）。
        /// 🔴 **红点的唯一计数口**：`DailyData.InboxCount` 转发到这里，`InboxNotificationProvider` 再读它。
        /// ⚠️ 写成**方法**（不是属性）：账本 A 档的机检按「声明行」认名字，属性那一行它认不出。</summary>
        public static int UnreadCount()
        {
            int n = 0;
            for (int i = 0; i < _messages.Count; i++) if (!_messages[i].Read) n++;
            return n;
        }

        public static Msg Find(string id)
        {
            Msg m;
            return !string.IsNullOrEmpty(id) && _byId.TryGetValue(id, out m) ? m : null;
        }

        /// <summary>原版 `BaseInboxEvent.GetTitle()`（真体 `out_b94/BaseInboxEvent_object__object_.GetTitle__1589550.c`
        /// / ILSpy `BaseInboxEvent.cs:152`）：**不是** 直接把 `GetMessageContent().title` 吐出来，
        /// 而是再跑**三次占位符替换**（顺序照抄：`[[RewardName]]` → `[[PlayerName]]` → `[[AllianceName]]`）。
        /// 🔴 **旧判据（B90-N3）写「三处替换在出厂表里已做完」是错的**：玩家名 / 联盟名是**运行时动态**的
        /// （原版读 `PlayerDataManager.singletonManager.playerName` / `AlliancesManager.Instance.CurrentPlayerAllianceName`），
        /// 烘进出厂表里不可能对 ⇒ 替换**必须**在运行时做（铁律 5 就地订正）。
        /// 两段口径不变：先 `GetMessageContent` 拿已解析文本，再套三次替换（见 <see cref="FillPlaceholders"/>）。</summary>
        public static string Title(Msg m)
        {
            var c = GetMessageContent(m);
            return c != null ? FillPlaceholders(m, c.Title) : "";
        }

        /// <summary>原版 `BaseInboxEvent.GetMessage()`（真体 `…GetMessage__15892b0.c` / ILSpy `BaseInboxEvent.cs:218`）：
        /// 与 <see cref="Title"/> **同构**，只是取 `content.message` 那一格，三次替换一模一样
        /// （`[[RewardName]]` → `[[PlayerName]]` → `[[AllianceName]]`）。</summary>
        public static string Body(Msg m)
        {
            var c = GetMessageContent(m);
            return c != null ? FillPlaceholders(m, c.Body) : "";
        }

        /// <summary>这条消息带不带奖励（= 原版 `MessageDisplay.hasReward`）。</summary>
        public static bool HasReward(string id)
        {
            var m = Find(id);
            return m != null && m.HasReward;
        }

        /// <summary>原版 `BaseInboxEvent.IsRead()`（读 `GameMessagePersistence.Openned`）。
        /// ⚠️ 原版那句是 `new` 遮蔽基类同名方法 —— 语义就是「这条消息的存档里那个已读位」。</summary>
        public static bool IsRead(string id)
        {
            var m = Find(id);
            return m != null && m.Read;
        }

        /// <summary>原版 `BaseInboxEvent.StartDate()`（`TData.StartDate`，毫秒）。</summary>
        public static long StartDate(string id)
        {
            var m = Find(id);
            return m != null ? m.StartMs : 0L;
        }

        /// <summary>原版 `BaseInboxEvent.RedeemableAt()`（基类默认 `DateTime.MinValue` ⇒ 立即可领）。</summary>
        public static long RedeemableAt(string id)
        {
            var m = Find(id);
            return m != null ? m.RedeemableAtMs : 0L;
        }

        /// <summary>原版存档里的 `claimedAt`（`> 0` = 已领）。</summary>
        public static long ClaimedAt(string id)
        {
            var m = Find(id);
            return m != null ? m.ClaimedAtMs : 0L;
        }

        /// <summary>原版 `InboxWindow.GetCurrentMessage()` =
        /// `messages.LastOrDefault(m =&gt; m.IsRead()) ?? messages.FirstOrDefault()`
        /// （判据 = `InboxWindow.cs:293-317`：`Enumerable.LastOrDefault(messages, predicate)` 的那条谓词是
        /// `IsRead`，取不到再 `FirstOrDefault`）。
        /// ⚠️ 「最后一条**已读**」听着反直觉，但那是原版逐句读出来的 —— ⛔ 别按直觉改成「最新未读」。</summary>
        public static Msg CurrentMessage
        {
            get
            {
                for (int i = _messages.Count - 1; i >= 0; i--) if (_messages[i].Read) return _messages[i];
                return _messages.Count > 0 ? _messages[0] : null;
            }
        }

        /// <summary>原版 `MessageDisplay.CheckMessageCollectable()`（`MessageDisplay.cs:200-289`）逐支：
        /// <list type="number">
        /// <item>`save == null` ⇒ 原版抛 NRE（我们**不照抄这个缺陷**：返回 false + 出声，见红线）。</item>
        /// <item>`claimedAt &gt; 0 || !hasReward` ⇒ **false**。</item>
        /// <item>`RedeemableAt() &gt; 服务器现在` ⇒ **false**（倒计时那一支）。</item>
        /// <item>`FeatureConfig.IsArmyHidden / IsExpansionHidden` ⇒ 开 `rewardWarningText` + false
        ///       —— 我们**全解锁**（用户 2026-09-17 边界②）⇒ 这两支恒不命中。</item>
        /// <item>否则 **true**。</item>
        /// </list>
        /// ⚠️ 原版**没有**「消息过期就不能领」这一支 ⇒ 我们也不加（`ExpireMs` 只留给将来做显示用）。</summary>
        public static bool CanCollect(Msg m)
        {
            if (m == null) return false;
            if (m.IsClaimed || !m.HasReward) return false;
            if (m.RedeemableAtMs > 0 && NowMs() < m.RedeemableAtMs) return false;
            return true;
        }

        /// <summary>「还没到可领时刻」= 原版 `MessageDisplay.Update` 里那个倒计时分支的判据。</summary>
        public static bool IsWaitingRedeemable(Msg m)
        {
            return m != null && m.HasReward && !m.IsClaimed && m.RedeemableAtMs > NowMs();
        }

        /// <summary>原版 `BaseInboxEvent.ClaimRewards()` + `&lt;ClaimRewards&gt;g__OnComplete|7_0`：
        /// 云脚本发奖 → 回调里 `RewardService.Collect(rewards, showAnimation: true, …)` → 置已领。
        /// 本地等价：`Wallet.Grant` 逐条 + `RewardWindow.ShowCollected` 一扇窗（与 `DailyData.CollectLogin`
        /// 的两格同窗同形）+ 置 `claimedAt` + 落盘。
        /// <para>⚠️ **不静的失败**：不可领时**出声说明原因**（原版那侧是服务端回错，我们回 `why`）。</para></summary>
        public static bool TryClaim(Msg m, out string why)
        {
            if (m == null) { why = "没有选中消息"; return false; }
            if (m.IsClaimed) { why = "这条消息的奖励已经领过了（`claimedAt > 0`）"; return false; }
            if (!m.HasReward) { why = "这条消息没有附带奖励（`hasReward = false`）"; return false; }
            if (m.RedeemableAtMs > 0 && NowMs() < m.RedeemableAtMs)
            {
                why = "还没到可领取时刻（`RedeemableAt` 未到）";
                return false;
            }

            m.ClaimedAtMs = NowMs();
            for (int i = 0; i < m.Rewards.Length; i++)
                Wallet.Grant(m.Rewards[i].Art, m.Rewards[i].Qty);
            Persist("领收件箱奖励 `" + m.Id + "`");

            var specs = new CampaignData.RewardSpec[m.Rewards.Length];
            for (int i = 0; i < specs.Length; i++)
                specs[i] = new CampaignData.RewardSpec(m.Rewards[i].Art, m.Rewards[i].Qty, CampaignData.TierBasic);
            RewardWindow.ShowCollected(specs);

            why = null;
            Debug.Log("[Inbox] 领取 `" + m.Id + "` ⇒ " + m.Rewards.Length + " 条奖励已入账（`Wallet.Grant`；"
                    + "原版走 CloudScript，单机本地兑现）");
            return true;
        }

        /// <summary>原版 `GameNewsEvent.SelectEvent()` = `PlayfabWrapper.GenericCloudScriptHandler(
        /// CloudHandle.OpenInbox, {eventId})` + `UiBadgeNotificationManager.Refresh&lt;InboxBadge&gt;()`
        /// （读已读标记 + 刷红点）。本地等价：置 `Read` + 落盘；**红点由 `UnreadCount` 现算**，
        /// 下一次 `UiBadgeNotificationManager` 刷新就读到新值。</summary>
        public static void MarkRead(Msg m)
        {
            if (m == null || m.Read) return;
            m.Read = true;
            Persist("标记已读 `" + m.Id + "`");
        }

        /// <summary>毫秒 → 本地时刻串。**逐字照原版** `MessageContainer.GetCreatedTimeLocal(long millis)` =
        /// `new DateTime(1970,1,1).AddMilliseconds(millis)`，再由 `OnInitialize` 用
        /// **`ToString("MM/dd/yyyy HH:mm")`** 印出来（`MessageContainer.cs:84-104`）。
        /// ⚠️ 方法名里那个 `Local` **与实现不符**（没有 `ToLocalTime`）—— 我们照实现，不照名字。</summary>
        public static string FormatDate(long millis)
        {
            if (millis <= 0) return "";
            return new DateTime(1970, 1, 1).AddMilliseconds(millis).ToString("MM/dd/yyyy HH:mm");
        }

        /// <summary>毫秒 → `DateTime`（本地 kind，同 `FormatDate` 的口径）。倒计时那一支要用。</summary>
        public static DateTime ToLocalTime(long millis)
        {
            return new DateTime(1970, 1, 1).AddMilliseconds(millis);
        }

        static long NowMs()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        // ============================================================ 落盘（形状照 `DailyData`）

        /// <summary>自检用的**目录改写口**（与 `DailyData.OverrideDir` / `ReplayStore.OverrideDir` 同一口径）。</summary>
        public static string OverrideDir;

        static string StoreDir
        {
            get
            {
                return string.IsNullOrEmpty(OverrideDir)
                     ? System.IO.Path.Combine(Application.persistentDataPath, "WarpforgeInbox")
                     : OverrideDir;
            }
        }
        public static string StorePath { get { return System.IO.Path.Combine(StoreDir, "inbox.json"); } }
        public static string LastStorePath { get; private set; }

        /// <summary>`OverrideDir` 设了 ⇒ 允许；否则**只有真在一局里**才碰玩家目录
        /// （`-batchmode` 下一律不碰 —— 与 `DailyData.StoreOn` 同一条硬挡）。</summary>
        static bool StoreOn
        {
            get { return !string.IsNullOrEmpty(OverrideDir) || (Application.isPlaying && !Application.isBatchMode); }
        }

        static bool _loaded, _storeOffSaid;

        /// <summary>盘上那一份：**只放存档态**（消息内容在代码里）。
        /// ⚠️ 与 `DailyData` 同一条版本口径：读到不认识的 `version` ⇒ 出声 + 按出厂走，⛔ 别硬解。</summary>
        [Serializable]
        class StoreDto
        {
            public int version = 1;
            public MsgStateDto[] states;
        }

        [Serializable]
        class MsgStateDto
        {
            public string id;
            public bool read;
            public long claimedAt;
        }

        /// <summary>读一次盘（只能读一次；要重读走 `ReloadForTest()`）。</summary>
        static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            if (!StoreOn)
            {
                if (!_storeOffSaid)
                {
                    _storeOffSaid = true;
                    Debug.Log("[Inbox] 落盘**关着**（没设 `OverrideDir`，且这一趟不在真的一局里）⇒ 本进程不碰玩家真档，"
                            + "已读/已领只活在内存里。自检要走盘上那一份：先 `InboxData.OverrideDir = <临时目录>` "
                            + "再 `DeleteStoreForTest()` + `ReloadForTest()`。");
                }
                return;
            }
            var path = StorePath;
            if (!System.IO.File.Exists(path))
            {
                Debug.Log("[Inbox] 还没有存档（`" + path + "`）⇒ 按出厂值走");
                return;
            }
            try
            {
                var dto = JsonUtility.FromJson<StoreDto>(System.IO.File.ReadAllText(path));
                if (dto == null)
                {
                    Debug.LogWarning("[Inbox] 存档解不出内容（`" + path + "`）⇒ 按出厂值走，下一拍落盘会**盖掉**它");
                    return;
                }
                if (dto.version != new StoreDto().version)
                {
                    Debug.LogWarning("[Inbox] 存档版本 " + dto.version + " 不认识（本代码只认 "
                                   + new StoreDto().version + "）⇒ 按出厂值走（`" + path + "`）");
                    return;
                }
                ApplyDto(dto);
                Debug.Log("[Inbox] 已从 `" + path + "` 读回：" + Brief());
            }
            catch (Exception e)
            {
                Debug.LogError("[Inbox] 读盘失败：" + e.Message + "（`" + path + "`）⇒ 本进程按出厂值走"
                             + "（红线：不许静默失败 —— 这一句就是「已读/已领看起来没了」的原因）");
            }
        }

        static void ApplyDto(StoreDto dto)
        {
            if (dto.states == null) return;
            for (int i = 0; i < dto.states.Length; i++)
            {
                var s = dto.states[i];
                if (s == null || string.IsNullOrEmpty(s.id)) continue;
                var m = Find(s.id);
                if (m == null) continue;            // 老档里那条消息已经不在出厂表里 ⇒ 安静丢掉（内容变了）
                m.Read = s.read;
                m.ClaimedAtMs = s.claimedAt > 0 ? s.claimedAt : 0;
            }
        }

        static void Persist(string why)
        {
            if (!StoreOn) return;
            var path = StorePath;
            try
            {
                var dto = new StoreDto();
                var st = new List<MsgStateDto>();
                for (int i = 0; i < _messages.Count; i++)
                {
                    var m = _messages[i];
                    if (!m.Read && m.ClaimedAtMs <= 0) continue;      // 出厂态不占档
                    st.Add(new MsgStateDto { id = m.Id, read = m.Read, claimedAt = m.ClaimedAtMs });
                }
                dto.states = st.ToArray();
                var json = JsonUtility.ToJson(dto);
                var dir = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                // 原子写：先 `.tmp` 再换名（与 `DailyData.Persist` 同一形状 —— 中途崩不毁旧档）。
                var tmp = path + ".tmp";
                System.IO.File.WriteAllText(tmp, json);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                System.IO.File.Move(tmp, path);
                LastStorePath = path;
                Debug.Log("[Inbox] 落盘（" + why + "）→ `" + path + "`：" + Brief());
            }
            catch (Exception e)
            {
                Debug.LogError("[Inbox] 落盘失败：" + e.Message + "（`" + path + "`；" + why + "）"
                             + " ⇒ **这一次改动只在内存里**（红线：不许静默失败）");
            }
        }

        static string Brief()
        {
            int read = 0, claimed = 0;
            for (int i = 0; i < _messages.Count; i++) { if (_messages[i].Read) read++; if (_messages[i].IsClaimed) claimed++; }
            return _messages.Count + " 条 · 已读 " + read + " · 已领 " + claimed;
        }

        // ---- 自检口（⛔ 不在生产路径上）----

        /// <summary>打回出厂态（= 模拟「进程刚起来、盘上什么都没有」）。</summary>
        public static void ReloadForTest()
        {
            for (int i = 0; i < _messages.Count; i++) { _messages[i].Read = false; _messages[i].ClaimedAtMs = 0; }
            _loaded = false;
            EnsureLoaded();
        }

        /// <summary>删档。🔴 **没设 `OverrideDir` 时拒绝执行**（否则删的就是玩家真档 —— 硬挡）。</summary>
        public static void DeleteStoreForTest()
        {
            if (string.IsNullOrEmpty(OverrideDir))
            {
                Debug.LogError("[Inbox] 自检：**拒绝删档** —— 没设 `OverrideDir` 时删的就是玩家的真存档。");
                return;
            }
            try
            {
                var p = StorePath;
                if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
                var t = p + ".tmp";
                if (System.IO.File.Exists(t)) System.IO.File.Delete(t);
            }
            catch (Exception e) { Debug.LogError("[Inbox] 自检删档失败：" + e.Message); }
        }

        // ============================================================ 🆕 2026-10-28（B90 · N3，追加在类尾）
        /// <summary>原版 `BaseInboxEvent.GetMessageContent()` 的产物（原版返回 `LocalizedMessage`：
        /// `code` / `title` / `message` 三格）。
        /// <para>判据 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/BaseInboxEvent.cs` 的
        /// `GetMessageContent()`（真体可读的那一段）+ 两个 lambda：`…b__0` = `m.code == currentCode`（
        /// `LocalizationManager.CurrentLanguageCode`）、`…b__2_1` = `m.code == "en"` ⇒
        /// **先取当前语言那一份，取不到回 `en`**。</para>
        /// <para>🔴 **那条「当前语言 → en」的链在我们这儿没有第二份**（铁律 6）：`TitleTerm`/`BodyTerm` 是
        /// **语言无关的官方词条键**，解析口 `Loc.T` 内部就是「当前语言 → `Loc.Fallback`（= English）」
        /// （`Core/Loc.cs` 的 `Fallback` / `Effective` / `T()` 那三处）⇒ 本方法**只做「词条键 vs 已解析文本」的两级选择**，
        /// ⛔ 别在这里再写一份语言回退（那会变成两处写同一条规则）。
        /// ⚠️ 与原版的**形状差**（如实记）：原版一个消息带**多个 `LocalizedMessage`**（每种语言一条）；
        /// 我们一条消息只存**一个键 + 一份明文兜底**（出厂那两条明文本就是 🔶 我们挑的）。</para>
        public sealed class MessageContent
        {
            /// <summary>`title`（= `GetTitle()` 的口径）。</summary>
            public string Title;
            /// <summary>`message`（= `GetMessage()` 的口径）。</summary>
            public string Body;
        }

        /// <summary>原版 `BaseInboxEvent.GetMessageContent()`。`Msg == null` ⇒ null（⛔ 不编一个空消息出来）。
        /// 两级口径逐字段：有**已解析明文**用明文（= 出厂表那一条 `FillArgs` 之后的结果），否则现解词条键。
        /// 两个字段**各自**走这两级 —— 与原版「一条 LocalizedMessage 同时给 title/message」等价。</summary>
        public static MessageContent GetMessageContent(Msg m)
        {
            if (m == null) return null;
            var c = new MessageContent();
            c.Title = !string.IsNullOrEmpty(m.TitleText) ? m.TitleText
                    : (string.IsNullOrEmpty(m.TitleTerm) ? "" : LiveOpsLocalCatalog.TermText(m.TitleTerm));
            c.Body = !string.IsNullOrEmpty(m.BodyText) ? m.BodyText
                    : (string.IsNullOrEmpty(m.BodyTerm) ? "" : LiveOpsLocalCatalog.TermText(m.BodyTerm));
            return c;
        }

        // ============================================================ 🆕 2026（B94 · Y6，追加在类尾）
        // 🔴 运行时占位符替换 —— 真体推翻旧判据「出厂表里已做完」（见 Title / Body 头注释）。

        /// <summary>原版 `GetTitle` / `GetMessage` 尾部那三跳 `String.Replace`（真体
        /// `out_b94/BaseInboxEvent_object__object_.{GetTitle__1589550,GetMessage__15892b0}.c`）：
        /// **顺序照抄** `[[RewardName]]` → `[[PlayerName]]` → `[[AllianceName]]`。
        /// 各值来源：奖励名 = 第一条奖励的物品名（无奖励 ⇒ 空串，见 <see cref="FirstRewardDisplayName"/>）；
        /// 玩家名 = <see cref="ProfileData.PlayerName"/>（原版 `PlayerDataManager.singletonManager.playerName`）；
        /// 联盟名 = <see cref="SocialData.AllianceName"/>（原版 `AlliancesManager.Instance.CurrentPlayerAllianceName`，
        /// 本地恒空 ⇒ 替换成空串）。
        /// ⚠️ 原版在 `content == null || content.title == null` 时走 NRE；我们**不照抄缺陷**：
        /// 文本为 null ⇒ 返回空串（红线：不许静默炸）。</summary>
        static string FillPlaceholders(Msg m, string text)
        {
            if (text == null) return "";
            string rewardName = FirstRewardDisplayName(m);
            string s = text.Replace("[[RewardName]]", rewardName ?? "");
            s = s.Replace("[[PlayerName]]", ProfileData.PlayerName ?? "");
            s = s.Replace("[[AllianceName]]", SocialData.AllianceName ?? "");
            return s;
        }

        /// <summary>原版 `GetRewards().FirstOrDefault()?.Item.&lt;LocalizedName&gt;` 的替身：取**第一条奖励**的
        /// 显示名；没有奖励 ⇒ 空串（与原版「无奖励 ⇒ 空串」一致，`GetTitle` 那两个方法体里 `newValue` 的兜底支）。
        /// ⚠️ 我们的 `Reward` 只存钱包键 `Art`（图名 / 货币 code），没有 `ObtainableItem.LocalizedName` ⇒
        /// 走 <see cref="RewardDisplayName"/> 那张小表兜底。</summary>
        static string FirstRewardDisplayName(Msg m)
        {
            if (m == null || m.Rewards == null || m.Rewards.Length == 0) return "";
            return RewardDisplayName(m.Rewards[0].Art);
        }

        /// <summary>🔶 自己设计（非原版）：`Reward.Art` → 物品显示名。原版是 `ObtainableItem.LocalizedName`
        /// （一个 `I2.Loc.LocalizedString` 词条），我们本地没有 `ObtainableItem` 这层 SO ⇒ 只能按 art 键查一条小表：
        /// <list type="bullet">
        /// <item>货币 code（`C2`/`C3`/`C5`）⇒ <see cref="CurrencyNames.Of"/>（官方词条）；</item>
        /// <item>战役点（`40K_genearl_icon_Campaign_points[_big]`，⚠️ 原版拼写 `genearl`）⇒ 官方词条
        ///       `Rewards Menu/CampaignPoints`（EN `Campaign Points` / ZH `战役点数`，
        ///       判据 = `Resources/official_loc_13lang.json` 现读）。</item>
        /// </list>
        /// 其它 ⇒ 原样返回 art 键（原版是 LocalizedName；我们没有真名，⛔ 不编空串把占位静默吞掉）。
        /// 缺的只有「其它奖励类型的真名」—— 本地没有那份 SO 名表；调度台 Y6 2026-10-29 定。</summary>
        static string RewardDisplayName(string art)
        {
            if (string.IsNullOrEmpty(art)) return "";
            string cur = CurrencyNames.Of(art);
            if (!string.IsNullOrEmpty(cur)) return cur;
            if (art == "40K_genearl_icon_Campaign_points_big" || art == "40K_genearl_icon_Campaign_points")
                return Loc.T("Rewards Menu/CampaignPoints");
            return art;
        }
    }
}
