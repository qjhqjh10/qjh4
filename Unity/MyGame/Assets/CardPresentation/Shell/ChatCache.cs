// ChatCache.cs — `ChatGlobalManager` 那 6 个**纯本地**方法的等价物（黑名单 + per-channel 消息缓存 + 登录时刻）
//
// ============================ 判据（第一权威 = 反编译方法体） ============================
//   · `d:/2/tools/decomp_full/ChatGlobalManager__BlockPlayer.c:27-77`
//   · `d:/2/tools/decomp_full/ChatGlobalManager__CheckBlockedList.c:9-13`
//   · `d:/2/tools/decomp_full/ChatGlobalManager__CheckIfOlderLogin.c:31-59`
//   · `d:/2/tools/decomp_full/ChatGlobalManager__IsMessageRepeated.c:15-44`
//   · `d:/2/tools/decomp_full/ChatGlobalManager__TryCacheMessage.c:19-63`
//   · `d:/2/tools/decomp_full/ChatGlobalManager__TryCacheMessages.c:25-52`
//   · `d:/2/tools/decomp_full/ChatGlobalManager__GetCachedChatMessages.c`（per-channel List：**容量 50** = 初始 `new List<>(50)`）
//   交叉核对（字段名/字段归属）：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ChatGlobalManager.cs:1603-1690`（三个布尔/写入方法）
//   · `/2800-2880`（三个缓存方法）· `ChatMessageData.cs:55-73`（字段顺序：type/playerId/playerName/data/msg）
//
// ⚠️ **`.c` 是权威，ILSpy 那三处不可直接用**：
//   · `IsMessageRepeated`：ILSpy 把「与缓存末条的同名字段比」读成 `if (data == (string)0)`（**第二个实参整个丢了**）——
//     `.c:23-38` 才是 `op_Inequality(message.X, last.X)` 五个字段逐个比、**全相等才 `return 1`**。
//   · `CheckIfOlderLogin` / `CheckIfNewerLogin`：ILSpy 只剩 `obj = -num; …return flag3 != flag2;` 一堆残渣。
//     `.c:33-58` 是 `long.TryParse(parameter1)` 与 `playerData.serverTimeOnLogin` 直接比大小（`<` / `>`）。
//   · `TryCacheMessages`：`.c:49` 那条 `NetworkingPeer__OnMessage` 是**枚举器 `Dispose()`**（ICF 折叠改了名，不是网络调用）。
//
// ============================ 为什么单开一个文件 ============================
// `ChatGlobalManager` 是 **Photon Chat 的宿主 MonoBehaviour**（连接 / 订阅 / 收发全在网络层，那一族判 D）；
// 但这 6 个方法体内**一处网络调用都没有**（Q4 §四·二 现核）⇒ 本地可做、也**应当**做。
// ⛔ 别塞进表现层：`Shell/ChatTab.cs` 只负责「按频道把行画出来」，缓存/黑名单是数据侧的事
//   （与 `DailyData` / `BattleLogData` 那条分界同形）。
//
// ============================ 存储口径（⛔ 全工程只有一份聊天表） ============================
// 原版：
//   · `Dictionary<string, List<ChatMessageData>> cachedMessages` —— **per-channel List**（初始容量 50，超 100 条裁一条）
//   · `List<string> blockedPlayers` —— 存 `PlayerDataKey.BlockedPlayers`（第 29 号 key，公共 user data，JSON 数组）
// 我们：
//   · **单表** `SocialData.ChatMessages`（`ChatTab.Rebuild` 按 `m.Channel` 过滤 = per-channel 视图）。
//     ⇒ 本文件对「某频道的缓存」= 对那张表**按 `Channel` 过滤**，⛔ **不另建第二个缓存**
//     （另建 = 两份数据源：缓存进去了而 UI 永远看不到）。这正是 `GetCachedChatMessages` 那条账的 A 口径。
//   · 黑名单存 **`SaveCenter`** 的 `SaveKeys.BlockedPlayers`（= 原版那个 29 号 key 的**本地**落点；
//     原版走 `PlayerDataManager.UpdateOnlineUserData` = 云端 user data，我们无云端 ⇒ 走中央存档）。
//     形状差异：原版 payload 是**裸 JSON 数组**，`SaveCenter` 的既有约定要求 payload 是 **JSON 对象**
//     ⇒ 外面裹一层 `{"playerIds":[...]}`（读回来语义一致；`SaveCenter` 的约定见其文件头）。
//
// ============================ 字段映射（原版 `ChatMessageData` → 我们的 `SocialData.ChatMessage`） ============================
//   原版 `.c` 偏移   原版字段        我们字段      出处
//   `+0x18` (int)    `type`          `Type`        `ChatMessageData.cs:55`
//   `+0x20`          `playerId`      `PlayerId`    `:57`（`TryCacheMessage.c:21` 就是拿它查黑名单）
//   `+0x28`          `playerName`    `Sender`      `:59`
//   `+0x50`          `data`          `LocData`     `:69`
//   `+0x58`          `msg`           `Text`        `:71`
//   （`timestamp` 我们另有 `TimestampMs`，见 `SocialData.ChatMessage`；原版这 6 个方法都不看它）

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`ChatGlobalManager` 的本地等价物（原版那 6 个「方法体内 0 处网络调用」的方法）。
    /// <para>⛔ 只放纯逻辑：黑名单 / 频道缓存 / 登录时刻比较。UI 在 `Shell/ChatTab.cs`（画）、
    /// 发送在 `Shell/ChatPanel.cs`（那一族要 Photon，判 D）。</para></summary>
    public static class ChatCache
    {
        // ════════════════════════════════════════════════════════════════════════════════════
        //  一、屏蔽名单（原版 `ChatGlobalManager.CheckBlockedList` / `BlockPlayer`）
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>`SaveKeys.BlockedPlayers` 的 payload 形状（原版是**裸数组**；见文件头「存储口径」那一段）。
        /// ⚠️ `SaveCenter.Set{T}` 要求 `T : class` 且 payload 必须是 JSON **对象** ⇒ 必须有个容器类。</summary>
        [Serializable]
        public class BlockedDto
        {
            /// <summary>被屏蔽的玩家 id（原版 `List<string> blockedPlayers`）。</summary>
            public List<string> playerIds = new List<string>();
        }

        static readonly List<string> _blocked = new List<string>();
        static bool _blockedLoaded;

        /// <summary>屏蔽名单本体（**只读用**）。第一次摸到时候从 <see cref="SaveCenter"/> 读一次。
        /// <para>原版 `ChatGlobalManager.Initialize`（`ChatGlobalManager.cs:1033-1034`）：
        /// `playerData.GetOnlineData&lt;List&lt;string&gt;&gt;(PlayerDataKey.BlockedPlayers, checkPublicData: true)`。</para></summary>
        public static List<string> BlockedPlayers { get { EnsureBlockedLoaded(); return _blocked; } }

        /// <summary>玩家是否在屏蔽名单里 —— 原版 `ChatGlobalManager.CheckBlockedList(string playerId)`。
        /// <para>判据 = `decomp_full/ChatGlobalManager__CheckBlockedList.c:9-13`：**整个方法体就是
        /// `blockedPlayers.Contains(playerId)`**（列表为 null 时原版抛 NRE，我们列表恒非 null）。</para></summary>
        public static bool CheckBlockedList(string playerId)
        {
            EnsureBlockedLoaded();
            return _blocked.Contains(playerId);
        }

        /// <summary>把 playerId 加进屏蔽名单并持久化 —— 原版 `ChatGlobalManager.BlockPlayer(string playerId)`。
        /// <para>判据 = `decomp_full/ChatGlobalManager__BlockPlayer.c:27-77`，逐句：</para>
        /// <list type="number">
        /// <item>`.c:28-33` 已在名单里 ⇒ **直接 return**（不重复加、也不播报）。</item>
        /// <item>`.c:34-54` 追加一条（`List.Add`）。</item>
        /// <item>`.c:55-67` 组一个 `{PlayerDataKey.BlockedPlayers → JSON(整个名单)}` 的字典，写进玩家数据。</item>
        /// <item>`.c:69` `PlayerDataManager.UpdateOnlineUserData(...)` —— **云端副本**。我们无云端 ⇒
        ///      换成本地中央存档（<see cref="SaveCenter"/>）的同一个 key；写失败**出声**（`SaveCenter` 自己会喊）。</item>
        /// <item>`.c:72` `GameAnalytics.SendBlockUser(playerId)` —— **P3 遥测**（`GameAnalytics` 走后端）⇒ 不复刻，
        ///       如实记在这里（账 `B90-N5` 报告 §待裁）。</item>
        /// </list>
        /// <para>🔶 自己设计（非原版）：**空 / null playerId 的守卫** —— 原版直接 `List.Contains(null)` / `Add(null)`
        /// 也会照做（名单里从此多一个 null 条目）。我们选择「出声 + 不加」，理由是本仓的 `PlayerId` 由夹具/数据填，
        /// 空值只会是缺数据（⛔ 不能让一个空 id 把所有人的消息都「屏蔽」掉）。
        /// 裁定：调度台 2026-10-28（`B90-N5` 简报授权「必要时自行决策」）。</para></summary>
        public static void BlockPlayer(string playerId)
        {
            EnsureBlockedLoaded();
            if (string.IsNullOrEmpty(playerId))
            {
                Debug.LogWarning("[ChatCache] BlockPlayer 收到空 playerId ⇒ 不加进屏蔽名单（原版会加一个 null 条目；"
                               + "见本方法注释的 🔶 守卫那条）。");
                return;
            }
            if (_blocked.Contains(playerId)) return;      // 原版 .c:28-33
            _blocked.Add(playerId);                       // 原版 .c:34-54
            SaveBlocked();                                // 原版 .c:55-69（云端 → 本地存档）
        }

        /// <summary>第一次用到时把名单读进来（原版在 `Initialize` 里读，我们懒加载 —— 本仓没有「聊天初始化」那一刻）。
        /// <para>⚠️ 缺 key 是**正常**的（第一次跑）：`SaveCenter.Get` 会 `SpeakOnce` 说一次，⛔ 别当成错误。</para></summary>
        static void EnsureBlockedLoaded()
        {
            if (_blockedLoaded) return;
            _blockedLoaded = true;
            var dto = SaveCenter.Get<BlockedDto>(SaveKeys.BlockedPlayers);
            if (dto == null || dto.playerIds == null) return;
            _blocked.Clear();
            _blocked.AddRange(dto.playerIds);
        }

        /// <summary>把整份名单写回存档（原版写的是**整个 List**，不是增量 —— `.c:57-61` `SerializeObject(blockedPlayers)`）。</summary>
        static bool SaveBlocked()
        {
            var dto = new BlockedDto();
            dto.playerIds.AddRange(_blocked);
            bool ok = SaveCenter.Set(SaveKeys.BlockedPlayers, dto);
            if (!ok)
            {
                Debug.LogWarning("[ChatCache] 屏蔽名单**没能落盘**（`SaveCenter.Set` 返回 false，见它自己的那条日志）"
                               + "⇒ 本次屏蔽只在内存里有效。");
            }
            return ok;
        }

        // ════════════════════════════════════════════════════════════════════════════════════
        //  二、per-channel 消息缓存（原版 `GetCachedChatMessages` / `TryCacheMessage` / `TryCacheMessages` / `IsMessageRepeated`）
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>原版 `TryCacheMessage` 的裁剪阈值：**加完一条后若该频道 > 100 条 ⇒ 丢掉最旧一条**。
        /// 判据 = `decomp_full/ChatGlobalManager__TryCacheMessage.c:53-57`（`.c:53` 与 `0x65` = **101** 比 ⇒ 上限 100）。
        /// <para>⚠️ 旧账（`B89-E25b` 的判据要点）把它写成「裁剪到 **50**」—— **50 是初始容量**
        /// （`GetCachedChatMessages.c` 的 `new List&lt;ChatMessageData&gt;(50)`），**不是**裁剪阈值。本轮就地订正。</para></summary>
        public const int MaxCachedPerChannel = 100;

        /// <summary>聊天表本体（**全工程唯一一份**，见文件头）。</summary>
        static List<SocialData.ChatMessage> Store { get { return SocialData.ChatMessages; } }

        /// <summary>某频道缓存里现在有几条（`IsMessageRepeated` / 自检用）。</summary>
        public static int CachedCount(string channelId)
        {
            int n = 0;
            var all = Store;
            for (int i = 0; i < all.Count; i++) if (SameChannel(all[i], channelId)) n++;
            return n;
        }

        /// <summary>按 channelId 取**该频道的缓存切片**。
        /// <para>原版 `GetCachedChatMessages(channelId)`（`ChatGlobalManager.cs:2800-2818`）= 从
        /// `cachedMessages` 字典里取那条 List（缺频道就建一条**容量 50** 的空 List 并登记）。</para>
        /// <para>⚠️ **我们返回的是切片快照**（单表模型下没有「那条 List 本体」可言）⇒ 只读用，
        /// ⛔ 别拿它回写（回写走 <see cref="TryCacheMessage"/> / <see cref="TryCacheMessages"/>）。
        /// 账里 `GetCachedChatMessages` 判 A 的等价物 = `SocialData.ChatMessages` + `ChatTab` 按 `Channel` 过滤，
        /// 本条是那同一个口径的**取数口**。</para></summary>
        public static List<SocialData.ChatMessage> GetCachedChatMessages(string channelId)
        {
            var src = Store;
            var dst = new List<SocialData.ChatMessage>(Mathf.Min(MaxCachedPerChannel, Mathf.Max(0, src.Count)));
            for (int i = 0; i < src.Count; i++) if (SameChannel(src[i], channelId)) dst.Add(src[i]);
            return dst;
        }

        /// <summary>与缓存里**最后一条**比：五个字段全同 ⇒ `true`（挡重复发送）。
        /// <para>判据 = `decomp_full/ChatGlobalManager__IsMessageRepeated.c:15-44`（`ChatGlobalManager.cs:1603`）：</para>
        /// <list type="number">
        /// <item>`.c:15-17` `last = GetCachedChatMessages(channelId).LastOrDefault()`；`last == null` ⇒ **false**。</item>
        /// <item>`.c:23-31` 逐字段 `op_Inequality`：`data(+0x50)` → `msg(+0x58)` → `playerId(+0x20)` → `playerName(+0x28)`；
        ///       任何一个不等 ⇒ 立刻 false。</item>
        /// <item>`.c:33-38` `type(+0x18)` 相等 ⇒ **true**。</item>
        /// </list>
        /// <para>⚠️ 原版**没有**「消息表里已有该条」那种全表查找（那是表现侧 `ChatContentView.CheckForMessage`）；
        /// 这里只比**末条**，别顺手改成全表扫描。</para></summary>
        public static bool IsMessageRepeated(string channelId, SocialData.ChatMessage message)
        {
            var slice = GetCachedChatMessages(channelId);
            if (slice.Count == 0) return false;
            var last = slice[slice.Count - 1];
            if (last == null) return false;
            if (message == null) return false;                      // 原版 .c:19-22 是抛 NRE；我们给 false（同 `ChatContentView` 那族的守卫口径）
            if (!string.Equals(message.LocData, last.LocData)) return false;    // 原版 `data`（+0x50）
            if (!string.Equals(message.Text, last.Text)) return false;          // 原版 `msg`（+0x58）
            if (!string.Equals(message.PlayerId, last.PlayerId)) return false;  // 原版 `playerId`（+0x20）
            if (!string.Equals(message.Sender, last.Sender)) return false;      // 原版 `playerName`（+0x28）
            return message.Type == last.Type;                                   // 原版 `type`（+0x18）
        }

        /// <summary>把一条消息放进该频道的缓存（**黑名单过滤 + 超 100 裁旧**）。
        /// <para>判据 = `decomp_full/ChatGlobalManager__TryCacheMessage.c:19-63`（`ChatGlobalManager.cs:2820`）：</para>
        /// <list type="number">
        /// <item>`.c:19` 取该频道缓存。</item>
        /// <item>`.c:20-32` `message == null` ⇒ 抛；**发送者在黑名单里（`CheckBlockedList(message.playerId)`，
        ///       `.c:27-28` 读的是 `+0x20`）⇒ 原样返回、一条都不加**。</item>
        /// <item>`.c:33-52` 追加该条。</item>
        /// <item>`.c:53-57` 该频道条数 &gt; 100 ⇒ `RemoveAt(0)`（丢最旧一条）。</item>
        /// </list>
        /// <para>⚠️ 我们这边多一步：把 `message.Channel` 置成 `channelId` —— 原版的频道是字典的 key、
        /// 消息本身不带频道；我们单表模型靠这个字段做切片（见文件头「存储口径」）。</para>
        /// <returns>该频道的缓存切片（原版返回的就是那条 List 本体；我们返回快照，理由同 `GetCachedChatMessages`）。</returns></summary>
        public static List<SocialData.ChatMessage> TryCacheMessage(string channelId, SocialData.ChatMessage message)
        {
            if (message == null) return GetCachedChatMessages(channelId);
            if (CheckBlockedList(message.PlayerId)) return GetCachedChatMessages(channelId);   // 原版 .c:27-32
            message.Channel = channelId;
            Store.Add(message);                                                               // 原版 .c:33-52
            if (CachedCount(channelId) > MaxCachedPerChannel) DropOldestOf(channelId);         // 原版 .c:53-57
            return GetCachedChatMessages(channelId);
        }

        /// <summary>**整批替换**该频道的缓存（先清空、再逐条走 <see cref="TryCacheMessage"/>）。
        /// <para>判据 = `decomp_full/ChatGlobalManager__TryCacheMessages.c:25-52`（`ChatGlobalManager.cs:2856`）：</para>
        /// <list type="number">
        /// <item>`.c:25-32` `list.Clear()`（`_version++` / `_size = 0` / `Array.Clear`）。</item>
        /// <item>`.c:33-48` `messages == null` ⇒ 抛；否则枚举逐条 `TryCacheMessage(channelId, m)`
        ///       （**再走一遍黑名单过滤与裁剪** —— 所以黑名单里的人即使在这批里也进不去）。</item>
        /// <item>`.c:49-51` `Dispose()` 枚举器 + 返回 `GetCachedChatMessages(channelId)`。</item>
        /// </list>
        /// <returns>替换后该频道的缓存切片（快照）。</returns></summary>
        public static List<SocialData.ChatMessage> TryCacheMessages(string channelId, List<SocialData.ChatMessage> messages)
        {
            ClearChannel(channelId);                                                          // 原版 .c:25-32
            if (messages != null)
            {
                for (int i = 0; i < messages.Count; i++) TryCacheMessage(channelId, messages[i]);   // 原版 .c:43-48
            }
            return GetCachedChatMessages(channelId);                                          // 原版 .c:51
        }

        /// <summary>从单表里摘掉某频道的**全部**条目（保持其余频道的相对次序）。</summary>
        static void ClearChannel(string channelId)
        {
            var all = Store;
            for (int i = all.Count - 1; i >= 0; i--) if (SameChannel(all[i], channelId)) all.RemoveAt(i);
        }

        /// <summary>摘掉该频道**最旧**的一条（= 原版那条 List 的 `RemoveAt(0)`）。</summary>
        static void DropOldestOf(string channelId)
        {
            var all = Store;
            for (int i = 0; i < all.Count; i++)
            {
                if (SameChannel(all[i], channelId)) { all.RemoveAt(i); return; }
            }
        }

        static bool SameChannel(SocialData.ChatMessage m, string channelId)
        {
            return m != null && string.Equals(m.Channel, channelId);
        }

        // ════════════════════════════════════════════════════════════════════════════════════
        //  三、登录时刻（原版 `ChatGlobalManager.CheckIfOlderLogin`）
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>「我们这次登录的服务器时刻」（Unix 毫秒）。
        /// <para>原版 = `PlayerDataManager.serverTimeOnLogin`（服务器在登录结果里给的：
        /// `PlayerDataManager.cs:11753-11759` `serverTimeOnLogin__BackingField = result.ServerTime`），
        /// `CheckIfOlderLogin` 直接拿它比大小。</para>
        /// <para>🔶 自己设计（非原版）：**取值来源**。我们没有服务器、也没有登录握手 ⇒ 取**本进程第一次碰到本类时**的
        /// 本机 UTC 毫秒（懒初始化一次）。为什么必须自己定：判据（服务器时间）在远端、本机拿不到；
        /// 语义仍是「本次会话的开始时刻」，只是时钟从服务器换成本机。
        /// 裁定：调度台 2026-10-28（`B90-N5` 简报授权「必要时自行决策」）。</para></summary>
        public static long LoginTimeMs
        {
            get
            {
                if (!_loginMsSet) { _loginMs = NowMs(); _loginMsSet = true; }
                return _loginMs;
            }
        }
        static long _loginMs;
        static bool _loginMsSet;

        static long NowMs() { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }

        /// <summary>自检用：把「登录时刻」钉死（不然断言会随真实时钟飘）。
        /// <para>⚠️ 与 `SaveCenter.SetCurrentVersionForTest` 同一族 —— **正式代码别调**。</para></summary>
        public static void SetLoginTimeForTest(long ms) { _loginMs = ms; _loginMsSet = true; }

        /// <summary>「这条消息比我这次登录还早」⇒ `true`（旧登录 / 陈旧消息）。
        /// <para>判据 = `decomp_full/ChatGlobalManager__CheckIfOlderLogin.c:31-59`：
        /// `.c:33` `long.TryParse(messageData.parameter1, out v)`（解析失败时 `v` 保持 **0**）；
        /// `.c:58` `return v &lt; 服务器登录时刻;`。⚠️ **形参收窄**：原版只用到 `messageData.parameter1`
        /// 这一个字段 ⇒ 我们直接收「服务器时刻毫秒」（本仓没有 `PrivateMessageData` 这个类型，
        /// 它的其余字段属于私信链，判 D）。</para>
        /// <para>⚠️ `TryParse` 失败 ⇒ `v = 0` ⇒ 结果 **true**（0 比任何真实时刻都小）。**这是原版行为，照抄**
        /// （⛔ 别「修」成 false —— 铁律：原版自己也有 bug 时照抄，见 CLAUDE.md 那条 `BroadcastCardDrawn` 先例）。</para></summary>
        public static bool CheckIfOlderLogin(long messageServerTimeMs)
        {
            return messageServerTimeMs < LoginTimeMs;
        }

        /// <summary>原版那一跳的**逐字等价**：先 `long.TryParse`、再比。
        /// <para>判据同 <see cref="CheckIfOlderLogin(long)"/>（`.c:33` 的 `TryParse` 是**方法体里的第一句**，
        /// 不把它单独开一个口就没法「逐条对过」）。解析失败 ⇒ `v = 0` ⇒ **true**（原版如此）。</para></summary>
        public static bool CheckIfOlderLoginParsed(string parameter1)
        {
            long v;
            if (!long.TryParse(parameter1, out v)) v = 0;     // 原版：out 变量初值 0，TryParse 失败不改它
            return v < LoginTimeMs;
        }

        // ════════════════════════════════════════════════════════════════════════════════════
        //  四、自检
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>清空本类自己的两态（黑名单内存表 + 登录时刻），供自检每条用例前调。
        /// <para>⚠️ **不删存档**（要删走 `SaveCenter.Delete`）；只把「已加载」的闩复位，
        /// 免得上一条用例喂的黑名单漏到后面（同 `SocialData.ResetForTest` / `ForgeData.ResetForTest` 的口径）。</para></summary>
        public static void ResetForTest()
        {
            _blocked.Clear();
            _blockedLoaded = false;
            _loginMs = 0;
            _loginMsSet = false;
        }
    }
}
