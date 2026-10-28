// GameNewsEvent.cs —— **公告/消息类活动的基类**（原版 `GameNewsEvent<TPersistence,TData> : LiveOpsEvent<>`）
//
// ============================ 这是什么、判据在哪 ============================
// 判据（唯一来源）：
//   · `资料/完全复刻/判据/泛型与启动链.md` §四 `GameNewsEvent<TPersistence,`（C 7 条，全 P2）
//   · 账 `资料/完全复刻/账/泛型与启动链_补读.tsv` 同类名下那 7 行
// 原版真身：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/GameNewsEvent.cs`（185 行，**整类有真方法体**）；
//   配套类型同目录 `../BaseMessageData.cs` / `../GameMessageData.cs` / `../GameMessagePersistence.cs` /
//   `../LocalizedMessage.cs`；`Openned` / `Claimed` 的判据 = `decomp_full/Everguild.LiveOps.GameMessagePersistence__get_Openned.c`
//   （**全部内容** `return 0 &lt; *(long long *)(this + 0x38);`）与 `…__get_Claimed.c`（`+0x30`）。
//   字段表 = `dump.cs:128851`（类声明）/`:3476-3490`（三个数据类）。
//
// 🔴 **`IsRead()` 读的是「打开时刻」不是一个 bool**：`GameMessagePersistence.Openned = opennedAt &gt; 0`
//   （判据见上）⇒ 「已读」= **曾经把这条消息打开过**。`Claimed = claimedAt &gt; 0` 是另一格。
//
// ⚠️ 与原版的差异（⛔ 不是「已经一样」）：
//   ① 原版 `SelectEvent()` 的开头几跳是**服务器动作**（`UiBadgeNotificationManager.Refresh&lt;InboxBadge&gt;`
//      + 打云脚本 `CloudHandle.OpenInbox`）。本地没有服务器 ⇒ 保留**判据与顺序**、把云脚本那一跳出声跳过。
//   ② `BaseMessageData.behaviorWhenArmyIsHidden` 是个枚举（`BehaviorWhenArmyIsHidden`），
//      本批 7 条账一行都不读它 ⇒ **没搬**（⛔ 别猜一个枚举值表出来）。
//   ③ `RewardInfo[]` → `CampaignData.RewardSpec[]`（全工程口径，同 `Shell/ScoringEvent.cs`）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    // ============================================================ 一条本地化消息（原版 `LocalizedMessage`）
    /// <summary>原版 `LocalizedMessage`（`dump.cs:3478`）：`code`（语言码）/ `message` / `title`。
    /// <para>它是**数据行**（随消息一起下发），不是本地化表本身 —— 三个字段都是「已经翻好的文本」。</para></summary>
    [Serializable]
    public class LocalizedMessage
    {
        public string code;      // 原版 `[Dropdown("GetLocCodes")]` 选语言码，如 "en" / "zh-CN"
        public string message;
        public string title;

        public LocalizedMessage() { }
        public LocalizedMessage(string code, string message, string title)
        { this.code = code; this.message = message; this.title = title; }
    }

    // ============================================================ 消息数据（原版 `BaseMessageData` / `GameMessageData`）
    /// <summary>原版 `BaseMessageData : LiveOpsEventData`（`dump.cs:3476`，字段照抄；
    /// `behaviorWhenArmyIsHidden` 没搬 —— 见文件头差异 ②）。</summary>
    [Serializable]
    public class BaseMessageData : LiveOpsEventData
    {
        public string minVersion;
        public string maxVersion;
    }

    /// <summary>原版 `GameMessageData : BaseMessageData`（`dump.cs:3477`）：`messages`（`+0x78`）/
    /// `Rewards`（`+0x80`）/ `canClaimReward`（`+0x88`）/ `virtual bool HasRewards`。</summary>
    [Serializable]
    public class GameMessageData : BaseMessageData
    {
        public List<LocalizedMessage> messages = new List<LocalizedMessage>();
        public CampaignData.RewardSpec[] Rewards;
        public bool canClaimReward;

        /// <summary>原版 `virtual bool HasRewards`（`get_HasRewards`，`dump.cs:3477` Slot 11）：
        /// 语义 = 「这条消息带奖励」。⚠️ 那个方法体本批**没读**（`decomp_full` 里没有它的产物）
        /// ⇒ 这里只给**空判**（`Rewards != null &amp;&amp; Rewards.Length &gt; 0`），并在够不着的地方出声。
        /// ⛔ 别拿它当判据去决定「能不能领」（能领的判据是 `canClaimReward` + 存档 `Claimed`）。</summary>
        public virtual bool HasRewards { get { return Rewards != null && Rewards.Length > 0; } }
    }

    /// <summary>原版 `GameMessagePersistence : LiveOpsEventPersistence`（`dump.cs:3479`，字段照抄）：
    /// `claimedAt`（`+0x30`）/ `opennedAt`（`+0x38`），两个派生读口都是「&gt; 0」。</summary>
    [Serializable]
    public class GameMessagePersistence : LiveOpsSave
    {
        public long claimedAt;
        public long opennedAt;

        /// <summary>原版 `bool Openned`：`claimedAt` 之外的另一格 —— `opennedAt &gt; 0`（判据见文件头）。</summary>
        public bool Openned { get { return opennedAt > 0; } }
        /// <summary>原版 `bool Claimed`：`claimedAt &gt; 0`。</summary>
        public bool Claimed { get { return claimedAt > 0; } }
    }

    // ============================================================ 公告活动（原版 `GameNewsEvent<TPersistence,TData>`）
    /// <summary>原版 `GameNewsEvent&lt;TPersistence,TData&gt; : LiveOpsEvent&lt;TPersistence,TData&gt;`
    /// （约束 `TPersistence : GameMessagePersistence` / `TData : BaseMessageData`）。
    /// <para>它是**只读消息**那一族：公告（`AnnouncementEvent`）与收件箱（`BaseInboxEvent`）都从这里派生。</para></summary>
    public abstract class GameNewsEvent<TPersistence, TData> : LiveOpsEvent<TPersistence, TData>
        where TPersistence : GameMessagePersistence
        where TData : BaseMessageData
    {
        // ── 抽象两件（原版 `public abstract string GetTitle()` / `GetMessage()`）────────────────
        /// <summary>原版 `public abstract string GetTitle()`。</summary>
        public abstract string GetTitle();
        /// <summary>原版 `public abstract string GetMessage()`。</summary>
        public abstract string GetMessage();

        /// <summary>原版 `public new BaseMessageData GetBaseData()` —— 类型化读口（`GameNewsEvent.cs:104`）。</summary>
        public new BaseMessageData GetBaseData() { return Data; }

        /// <summary>原版 `public new GameMessagePersistence GetBaseSave()`（`GameNewsEvent.cs:142`）。</summary>
        public new GameMessagePersistence GetBaseSave() { return SaveGame; }

        /// <summary>原版 `public bool IsRead()`（`GameNewsEvent.cs:60`，**方法体可读**）：
        /// `return SaveGame.Openned;`
        /// 🔴 注意它是 **`new`**（不是 override）：`ILiveOps` 上没有 `IsRead`，所以原版用影子方法。
        /// ⚠️ 没有存档时原版 NRE ⇒ 我们出声 + false。</summary>
        public new bool IsRead()
        {
            if (SaveGame == null)
            {
                Debug.LogError("[GameNewsEvent] IsRead：没有存档（原版这一跳 NRE）⇒ false");
                return false;
            }
            return SaveGame.Openned;
        }

        /// <summary>原版 `public long StartDate()`（`GameNewsEvent.cs:77`，**方法体可读**）：
        /// <code>if (Data != null &amp;&amp; Data.dates != null) return Data.dates.startDate;  // 毫秒</code>
        /// ⚠️ 任一为 null 原版 NRE ⇒ 我们出声 + 0。</summary>
        public new long StartDate()
        {
            var d = Data == null ? null : Data.dates;
            if (d == null)
            {
                Debug.LogError("[GameNewsEvent] StartDate：没有 dates（原版这一跳 NRE）⇒ 0");
                return 0L;
            }
            return d.startDate;
        }

        /// <summary>原版 `public override void SelectEvent()`（`GameNewsEvent.cs:9-54`，**控制流可读**）：
        /// <code>
        /// if (SaveGame.Openned) return;                       // ★ 已读 ⇒ 不再开
        /// … TimeHelper.ServerTime;                            //   只为刷新「服务器时间」缓存
        /// UiBadgeNotificationManager.Refresh&lt;InboxBadge&gt;();   //   徽记重算
        /// if (Data is LocalInboxMessageData &amp;&amp; &lt;非空&gt;) return; //   本地消息不走向导（服务端回执才需要）
        /// PlayfabWrapper.GenericCloudScriptHandler(CloudHandle.OpenInbox, new { eventId });
        /// </code>
        /// <para>🔴 **`if (SaveGame.Openned) return;` 是第一句**（已读过就不再开）。
        /// ✅ **2026-10-28（B93_E19）**：基类 `LiveOpsEvent.SelectEvent()` **已补**（`Shell/LiveOpsEvent.cs:1326`，B93_E12）
        /// ⇒ 本方法改成 `override` 并调 `base.SelectEvent()`（原版那一跳的等价物 = `WindowsManager.OpenWindow(MainWindow /*10*/, this)`）。
        /// 🔴 **⛔ 别再改回 `OpenWindow()`**：那一版按**状态**选 label（OnPreview⇒20 / Finished⇒21），而原版 `SelectEvent` **恒用 10**
        /// ⇒ 经 `ILiveOps.SelectEvent` 虚分派时会**绕过**基类那一段（另多一条 `CS0114` 告警）。</para>
        /// <para>⚠️ 徽记刷新（`UiBadgeNotificationManager`）与云脚本两跳都**没有落点**（前者属外壳线、
        /// 后者是服务器）⇒ 出声一次，顺序不改。</para></summary>
        public override void SelectEvent()
        {
            if (SaveGame != null && SaveGame.Openned) return;    // ★ 原版第一句

            Debug.Log("[GameNewsEvent] SelectEvent：'" + GetID() + "' —— 徽记重算（UiBadgeNotificationManager）"
                    + "与云脚本 OpenInbox 两跳都没有落点（前者属外壳线、后者是服务器）");
            base.SelectEvent();                                  // ★ 判据 = 基类那一跳（恒 MainWindow(10)）
        }
    }
}
