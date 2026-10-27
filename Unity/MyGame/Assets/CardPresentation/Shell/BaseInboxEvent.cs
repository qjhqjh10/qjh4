// BaseInboxEvent.cs —— **收件箱消息基类**（原版 `BaseInboxEvent<TSave,TData> : GameNewsEvent<TSave,TData>`）
//
// ============================ 这是什么、判据在哪 ============================
// 判据（唯一来源）：
//   · `资料/完全复刻/判据/泛型与启动链.md` §四 `BaseInboxEvent<TSave,`（C 9 条，全 P2）
//   · 账 `资料/完全复刻/账/泛型与启动链_补读.tsv` 同类名下那 9 行
// 原版真身：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/BaseInboxEvent.cs`（665 行，**整类有真方法体**）
//   配套：`GameNewsEvent.cs`（父类）/ `GameMessageData.cs` / `GameMessagePersistence.cs` / `LocalizedMessage.cs`
//   谓词实体：`decomp_full/Everguild.LiveOps.BaseInboxEvent.__c__DisplayClass2_0_object__object____GetMessageContent_b__0.c`
//   （**全部内容** `return m.code == currentCode;`）与 `…__c_object__object____GetMessageContent_b__2_1`（回退 `"en"` 那一支）。
//   字段表 = `dump.cs:129192`（类声明）/`:3477-3490`。
//
// 🔴 **正文/标题里那三个占位符是字符串替换，不是格式化**（判据：`GetTitle` / `GetMessage` 两个方法体里
//   各三次 `String.Replace`）：`[[RewardName]]` → 第一条奖励的物品名；`[[PlayerName]]` → 玩家名；
//   `[[AllianceName]]` → 联盟名。**顺序也是照抄**（RewardName → PlayerName → AllianceName）。
//
// ⚠️ 与原版的差异（⛔ 不是「已经一样」）：
//   ① 原版 5 个具体子类（`InboxMessage` / `DLCMessage` / `TwitchDropsMessage` / `XSollaMessage` /
//      `LocalInboxMessage`）= 5 种消息来源；离线只留 `LocalInboxMessage` 这一路（判据 §四 那句）。
//      ⇒ 本文件给 `LocalInboxMessageData` 一个最小实现，其余 4 路**不建**（原版语义就是「等远端推」）。
//   ② `RewardService.Collect(...)` / `GameAnalytics.RecordTransaction(...)` / `PlayerDataManager.playerName` /
//      `AlliancesManager.CurrentPlayerAllianceName` 四样我们都没有等价物 ⇒ 各走一个可注入的口（见字段注释），
//      默认**出声一次**（⛔ 不静默：领奖那一步没落点必须让调用方看得见）。
//   ③ `RewardInfo[]` → `CampaignData.RewardSpec[]`（全工程口径）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `LocalInboxMessageData : GameMessageData`（`dump.cs:1521`）的**最小实现** ——
    /// 离线只保留这一路消息来源（判据 §四「离线只留 LocalInboxMessage 一路即可」）。
    /// <para>⚠️ 它自己那几个字段（`dump.cs` 里在 `+0x90` 之后）本批 9 条账一行都不读 ⇒ 没搬。</para></summary>
    [Serializable]
    public class LocalInboxMessageData : GameMessageData { }

    /// <summary>原版 `BaseInboxEvent&lt;TSave,TData&gt; : GameNewsEvent&lt;TSave,TData&gt;`
    /// （约束 `TSave : GameMessagePersistence` / `TData : GameMessageData`）。
    /// <para>它把「公告」那一族补成「**可领奖的消息**」：五个读口（标题/正文/富内容/可领时刻/奖励）
    /// + 已读/开始时刻（影子方法）+ 领取。</para></summary>
    public abstract class BaseInboxEvent<TSave, TData> : GameNewsEvent<TSave, TData>
        where TSave : GameMessagePersistence
        where TData : GameMessageData
    {
        // ============================================================ 可注入的四样（差异 ②）
        /// <summary>`() =&gt; 玩家名`（原版 `PlayerDataManager.singletonManager.playerName`）。
        /// null ⇒ 占位符替换成空串 + 出声一次。</summary>
        public static Func<string> PlayerNameProvider;
        /// <summary>`() =&gt; 联盟名`（原版 `AlliancesManager.Instance.CurrentPlayerAllianceName`）。
        /// null ⇒ 同上。</summary>
        public static Func<string> AllianceNameProvider;
        /// <summary>`(奖励表, 有没有物品名) =&gt; 物品的显示名`（原版 `ObtainableItem.&lt;+0x1A8&gt;`）。
        /// null ⇒ `[[RewardName]]` 替换成空串。</summary>
        public static Func<CampaignData.RewardSpec, string> RewardNameProvider;
        /// <summary>`(奖励, 是否播动画) =&gt; 真的发奖`（原版 `RewardService.Collect(rewards, showAnimation: true, …)`）。
        /// null ⇒ 出声一次（⛔ 不假装发了）。</summary>
        public static Action<CampaignData.RewardSpec[], bool> RewardSink;

        static readonly HashSet<string> warnedOnce = new HashSet<string>();
        static void WarnOnce(string key, string msg)
        {
            if (warnedOnce.Add(key)) Debug.LogWarning(msg);
        }

        /// <summary>自检用：清空注入 + 告警去重表。</summary>
        public static void ResetInjectionForTest()
        {
            PlayerNameProvider = null; AllianceNameProvider = null;
            RewardNameProvider = null; RewardSink = null;
            warnedOnce.Clear();
        }

        // ============================================================ 五个读口
        /// <summary>原版 `public virtual LocalizedMessage GetMessageContent()`（`BaseInboxEvent.cs:286`，**控制流可读**）：
        /// <code>
        /// string code = LocalizationManager.CurrentLanguageCode;
        /// var msgs = Data.messages;                       // (+0x78)
        /// var m = msgs.Find(x =&gt; x.code == code);         // b__0（**精确相等**）
        /// if (m == null) m = msgs.Find(x =&gt; x.code == "en");   // b__2_1（回退英文）
        /// return m;
        /// </code>
        /// 🔴 **`code` 用的是「当前语言码」而不是语言枚举**（`LocalizedMessage.code` 是 `"en"` / `"zh-CN"` 这种串）。
        /// 我们的 `Loc.Current` 是枚举 ⇒ 用 <see cref="CodeFor"/> 换成短码（`en/es/zh/…`），**仍然精确相等**。
        /// ⚠️ 如果数据里的 code 是 `"zh-CN"` 而我们的短码是 `"zh"`，这条路会落到英文回退 —— 那时**出声**
        /// 打出数据里到底有哪些 code（⛔ 别偷偷改成 StartsWith，那会造出原版没有的匹配规则）。</summary>
        public virtual LocalizedMessage GetMessageContent()
        {
            var msgs = Data == null ? null : Data.messages;
            if (msgs == null || msgs.Count == 0)
            {
                Debug.LogError("[Inbox] GetMessageContent：没有 messages（原版这一跳会找到 null 并继续 NRE）⇒ null");
                return null;
            }
            string code = CodeFor(Loc.Current);
            LocalizedMessage m = FindByCode(msgs, code);
            if (m != null) return m;
            m = FindByCode(msgs, "en");
            if (m == null)
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < msgs.Count; i++) { if (i > 0) sb.Append(", "); sb.Append(msgs[i] != null ? msgs[i].code : "<null>"); }
                Debug.LogWarning("[Inbox] GetMessageContent：'" + (GetID() ?? "<null>") + "' 既没有 `" + code
                               + "` 也没有 `en`（数据里现有的 code = [" + sb + "]）⇒ 标题/正文会是空");
            }
            return m;
        }

        static LocalizedMessage FindByCode(List<LocalizedMessage> msgs, string code)
        {
            for (int i = 0; i < msgs.Count; i++)
                if (msgs[i] != null && msgs[i].code == code) return msgs[i];
            return null;
        }

        /// <summary>`Loc.Current`（12 档枚举）→ `LocalizedMessage.code` 用的短码。
        /// 码表来源 = `Core/Loc.cs:2971` 的 `Lang13Codes`（`en/es/ru/fr/de/pt/it/ko/zh/ja/pl/cs/tr`）。
        /// <para>🔶 自己设计（非原版）：原版读的是 I2 的 `LocalizationManager.CurrentLanguageCode`（一个现成的串），
        /// 我们没有那个类、只有枚举 ⇒ 需要这一层映射。映射**只做名字翻译**，不改任何比较规则。
        /// 调度台 C2 2026-10-27 定。</para></summary>
        public static string CodeFor(AvailableLanguages lang)
        {
            switch (lang)
            {
                case AvailableLanguages.English: return "en";
                case AvailableLanguages.Spanish: return "es";
                case AvailableLanguages.French: return "fr";
                case AvailableLanguages.German: return "de";
                case AvailableLanguages.Portuguese: return "pt";
                case AvailableLanguages.Russian: return "ru";
                case AvailableLanguages.Italian: return "it";
                case AvailableLanguages.Korean: return "ko";
                case AvailableLanguages.Japanese: return "ja";
                case AvailableLanguages.Czech: return "cs";
                case AvailableLanguages.Polish: return "pl";
                case AvailableLanguages.Chinese: return "zh";
                default: return "en";
            }
        }

        /// <summary>第一条奖励的物品名（`rewards.FirstOrDefault()?.Item` → 物品名），替换 `[[RewardName]]` 用。</summary>
        string FirstRewardName()
        {
            var rs = GetRewards();
            if (rs == null || rs.Length == 0) return string.Empty;
            if (RewardNameProvider == null)
            {
                WarnOnce("rewardname", "[Inbox] `RewardNameProvider` 没接 ⇒ `[[RewardName]]` 会被替换成空串"
                        + "（原版这一跳是 `ObtainableItem` 的显示名）");
                return string.Empty;
            }
            return RewardNameProvider(rs[0]) ?? string.Empty;
        }

        /// <summary>三个占位符的替换（**顺序照抄**：RewardName → PlayerName → AllianceName）。
        /// 判据：`GetTitle` / `GetMessage` 两个方法体里那三跳 `String.Replace`。</summary>
        string Fill(string text, string rewardName)
        {
            if (text == null) return null;
            text = text.Replace("[[RewardName]]", rewardName ?? string.Empty);

            string pn = null;
            if (PlayerNameProvider != null) pn = PlayerNameProvider();
            else WarnOnce("playername", "[Inbox] `PlayerNameProvider` 没接 ⇒ `[[PlayerName]]` 会被替换成空串"
                                       + "（原版读 `PlayerDataManager.playerName`）");
            text = text.Replace("[[PlayerName]]", pn ?? string.Empty);

            string an = null;
            if (AllianceNameProvider != null) an = AllianceNameProvider();
            else WarnOnce("alliancename", "[Inbox] `AllianceNameProvider` 没接 ⇒ `[[AllianceName]]` 会被替换成空串"
                                        + "（原版读 `AlliancesManager.CurrentPlayerAllianceName`）");
            text = text.Replace("[[AllianceName]]", an ?? string.Empty);
            return text;
        }

        /// <summary>原版 `public override string GetTitle()`（`BaseInboxEvent.cs:152`，**控制流可读**）：
        /// <code>
        /// string rewardName = &lt;第一条奖励的物品名，没有就空&gt;;
        /// var content = GetMessageContent();
        /// return Fill(content.title, rewardName);
        /// </code></summary>
        public override string GetTitle()
        {
            string rewardName = FirstRewardName();
            var content = GetMessageContent();
            if (content == null || content.title == null) return null;   // 原版这一支 NRE ⇒ 我们返回 null
            return Fill(content.title, rewardName);
        }

        /// <summary>原版 `public override string GetMessage()`（`BaseInboxEvent.cs:218`）：同 <see cref="GetTitle"/>
        /// 但取 `content.message`。</summary>
        public override string GetMessage()
        {
            var content = GetMessageContent();
            if (content == null || content.message == null) return null;
            return Fill(content.message, FirstRewardName());
        }

        /// <summary>原版 `public virtual DateTime RedeemableAt()`（`BaseInboxEvent.cs:474`）：
        /// **全部内容** `return DateTime.MinValue;`（基类就是「随时可领」）。</summary>
        public virtual DateTime RedeemableAt() { return DateTime.MinValue; }

        /// <summary>原版 `public virtual RewardInfo[] GetRewards()`（`BaseInboxEvent.cs:479`，**控制流可读**）：
        /// `return Data.Rewards(+0x80) ?? Array.Empty&lt;RewardInfo&gt;();`
        /// 🔴 **null 给空表，不给 null**（`System.EmptyArray&lt;RewardInfo&gt;.Value` 那两跳）。
        /// ⚠️ `Data` 为 null 时原版 NRE ⇒ 我们出声 + 空表。</summary>
        public virtual CampaignData.RewardSpec[] GetRewards()
        {
            if (Data == null)
            {
                Debug.LogError("[Inbox] GetRewards：没有 data（原版这一跳 NRE）⇒ 空表");
                return Array.Empty<CampaignData.RewardSpec>();
            }
            return Data.Rewards ?? Array.Empty<CampaignData.RewardSpec>();
        }

        /// <summary>原版 `public new bool IsRead()`（`BaseInboxEvent.cs:533`）：`return SaveGame.Openned;`
        /// （与父类同一句，原版用 `new` 影子它）。</summary>
        public new bool IsRead()
        {
            if (SaveGame == null)
            {
                Debug.LogError("[Inbox] IsRead：没有存档（原版这一跳 NRE）⇒ false");
                return false;
            }
            return SaveGame.Openned;
        }

        /// <summary>原版 `public new long StartDate()`（`BaseInboxEvent.cs:550`）：
        /// `Data.dates.startDate`（毫秒），同父类那一跳。</summary>
        public new long StartDate()
        {
            var d = Data == null ? null : Data.dates;
            if (d == null)
            {
                Debug.LogError("[Inbox] StartDate：没有 dates（原版这一跳 NRE）⇒ 0");
                return 0L;
            }
            return d.startDate;
        }

        // ============================================================ 领取
        /// <summary>原版 `public virtual void ClaimRewards()`（`BaseInboxEvent.cs:577`，**控制流可读**）：
        /// <code>
        /// payload = new { eventId = GetID() };
        /// PlayfabWrapper.GenericCloudScriptHandler(&lt;领奖 handle&gt;, payload,
        ///         onComplete: 7_0,          // ← 见 <see cref="OnRewardsCollected"/>
        ///         onError: CustomDebug.LogError);
        /// </code>
        /// ⚠️ 那个 handle 常量（`CloudHandle.*`）本批**没解出名字**（`.c` 里是元数据指针，不是数值字面量）
        /// ⇒ 本跳**不打云脚本**、直接走本地落地并出声（本地本来就没有服务器）。
        /// <para>本地落地 = <see cref="OnRewardsCollected"/>（原版 7_0）。⚠️ 原版的「领没领」记在
        /// 存档 `claimedAt` 上，而那是**服务器回包**写的 ⇒ 我们的本地等价是
        /// <see cref="ClaimedSink"/>（默认不写，出声）。</para></summary>
        public virtual void ClaimRewards()
        {
            Debug.Log("[Inbox] ClaimRewards：'" + (GetID() ?? "<null>") + "' —— 原版这一跳打云脚本（handle 名本批未解出）"
                    + "，本地没有服务器 ⇒ 直接走本地落地（`OnRewardsCollected`）");
            OnRewardsCollected(GetRewards());
        }

        /// <summary>`(奖励, 领取后要不要把存档的 claimedAt 写上) =&gt; …`。
        /// 原版这一格由**服务器回包**写（`GameMessagePersistence.claimedAt`）⇒ 本地默认不写并出声。</summary>
        public static Action<CampaignData.RewardSpec[]> ClaimedSink;

        /// <summary>原版私有回调 `7_0(CollectedRewardsResult rewards)`（`BaseInboxEvent.cs:598-664`，**控制流可读**）：
        /// <code>
        /// TimeHelper.ServerTime;
        /// RewardService.Collect(rewards.Rewards, showAnimation: true, null, null, &lt;flag&gt;, &lt;title&gt;, &lt;showXpToast&gt;);
        /// bool twitch = Data is TwitchDropsMessageData;              // ← 只影响遥测的 location 档（71 / 72）
        /// GameAnalytics.RecordTransaction(71 + (twitch ? 1 : 0), TransactionType.TRADE, rewards.Rewards, null, &lt;flag&gt;);
        /// </code>
        /// <para>⚠️ 两处没有落点：`RewardService.Collect`（走 <see cref="RewardSink"/>）、
        /// `GameAnalytics.RecordTransaction`（遥测；本地无后端 ⇒ **不记**，但**出声**说明按哪个档记）。
        /// `TwitchDropsMessageData` 那一路我们不建（差异 ①）⇒ 遥测档恒 71。</para></summary>
        protected virtual void OnRewardsCollected(CampaignData.RewardSpec[] rewards)
        {
            var rs = rewards ?? Array.Empty<CampaignData.RewardSpec>();
            if (RewardSink != null) RewardSink(rs, true);
            else
            {
                WarnOnce("rewardsink", "[Inbox] `RewardSink` 没接 ⇒ 这 " + rs.Length + " 件奖励**没有发出去**"
                                     + "（原版这一跳是 `RewardService.Collect(rewards, showAnimation: true, …)`）");
            }

            Debug.Log("[Inbox] 遥测：GameAnalytics.RecordTransaction(location = 71, TransactionType.TRADE, " + rs.Length
                    + " 件) —— 本地无后端，未上报（原版 location 71 / TwitchDrops 那条 72）");

            if (ClaimedSink != null) ClaimedSink(rs);
            else
                WarnOnce("claimedsink", "[Inbox] `ClaimedSink` 没接 ⇒ 存档的 `claimedAt` **没有写**"
                                      + "（原版这一格由服务器回包写）；`Claimed` 会一直是 false");
        }
    }
}
