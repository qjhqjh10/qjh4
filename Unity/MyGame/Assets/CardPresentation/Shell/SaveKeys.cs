// SaveKeys.cs — 中央存档的【键表】（完全复刻 · Step 0「立规」）
//
// 判据（⛔ 别自创 key 名）：
//   `d:/2/Warpforge_code/Scripts/Assembly-CSharp/PlayerDataKey.cs:1-55`
//   —— 原版 `public enum PlayerDataKey`；`undefined = 0` 是**哨兵、不是数据 key**，数据 key = 1..51。
//
// 🔴 **数量口径（实测，别照抄文档）**：方案 `资料/完全复刻/判据/存档体系_原版语义与施工方案.md`
//   §四·1 标题写「53 个」、正文列了 **51** 个；F 报告写「53 个（52 有效）」；N6 简报写「52 个」。
//   **脚本逐条数枚举 = 52 个成员 / 51 个数据 key**（`undefined` 是第 52 个）⇒ 本表按 **51** 条建。
//   复现：对 `PlayerDataKey.cs` 跑 `re.findall(r'^\t(\w+) = (\d+)', ...)`，`>=1` 的共 51 条。
//   差异已写进 `资料/完全复刻/报告/N6_存档_第1轮.md` §五。
//
// 这张表干什么：
//   ① `SaveCenter` 只认这里的 key（外加 `_ours_` 前缀）—— 打错一个字母会**当场出声**，
//      不会静默建出一个没人读的文件（G1「没有中央入口」的直接防线）；
//   ② 每个 key 后面写明「**我们对应哪份现有存档**」或「**目前空缺**」——
//      Step 4 逐子系统切换时照这张表找目标，不用再查一遍。
//
// ⚠️ 原版另有 3 个**字符串常量** key（**不在枚举里**，`Everguild/W40k/AzureObjects/PlayFabReadOnlyDataKeys.cs:7,13,15`）
//    —— 单独列在文件末尾，不进 `All`（它们不是 `PlayerDataKey` 的成员，混进来会让对账数对不上）。
//
// ⚠️ 本文件**只有常量与纯函数**：不读写盘、不引用任何现有子系统。

using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>中央存档的键表（原版 `PlayerDataKey` 逐条照抄 + 「我们对哪份旧档」的映射）。
    /// <para>🔴 数据 key 总数 = <see cref="Count"/> = 51（`undefined = 0` 不算，见文件头）。</para></summary>
    public static class SaveKeys
    {
        // ════════════════════════════════════════════════════════════════════════════════════
        //  原版 `PlayerDataKey` 逐条照抄：顺序与值都照原版；`PlayerDataKey.cs:<行号>` 是出处。
        //  注释格式 = 值 · 出处 · 原版语义（只在能指到证据时写）· 【我们】对应哪份旧档 / 空缺
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>1 · `PlayerDataKey.cs:4` —— 原版存**游戏版本字符串**（`OnlineData.cs:70 GetPlayerGameVersion`）。
        /// 【我们】**空缺**（没有这条链）。</summary>
        public const string ProfileVersion = "ProfileVersion";

        /// <summary>2 · `PlayerDataKey.cs:5` —— 原版调试开关。【我们】**空缺**。</summary>
        public const string ActivateDevTools = "ActivateDevTools";

        /// <summary>3 · `PlayerDataKey.cs:6` —— 原版聊天封禁。【我们】**空缺**（无服务端审核）。</summary>
        public const string ChatBanned = "ChatBanned";

        /// <summary>4 · `PlayerDataKey.cs:7` —— 原版全局聊天封禁。【我们】**空缺**。</summary>
        public const string GlobalChatBanned = "GlobalChatBanned";

        /// <summary>5 · `PlayerDataKey.cs:8` —— 原版 `Decks`（与 `PlayerDecks` 并存，语义未证）。【我们】**空缺**
        /// （我们的卡组整份在 `PlayerDecks`，见下一条）。</summary>
        public const string Decks = "Decks";

        /// <summary>6 · `PlayerDataKey.cs:9` —— 原版玩家卡组（读：`DeckInventory.cs:209` 按方案 §四·2 引）。
        /// 【我们】`persistentDataPath/WarpforgeDecks.json`（`RuleEngine/Data/DeckStore.cs:22`）——**整份**导入。</summary>
        public const string PlayerDecks = "PlayerDecks";

        /// <summary>7 · `PlayerDataKey.cs:10` —— 原版「当前选中第几套」。【我们】同一份 `WarpforgeDecks.json` 的 `current`
        /// 字段（`DeckStore.cs` 的 `Dto.current`）—— 本轮**未拆**（整份在 `PlayerDecks`，见报告 §五）。</summary>
        public const string CurrentDeck = "CurrentDeck";

        /// <summary>8 · `PlayerDataKey.cs:11` —— 原版「当前活动」。【我们】`WarpforgeDraft/run.json`
        /// （`Shell/DraftModeWindow.cs:727` 的 `WarpforgeDraft` 目录 + `:729` 的 `run.json`）——整份导入。</summary>
        public const string ActiveEvent = "ActiveEvent";

        /// <summary>9 · `PlayerDataKey.cs:12` —— 原版战役进度。【我们】**空缺**（战役进度目前不落盘）。</summary>
        public const string ActiveCampaign = "ActiveCampaign";

        /// <summary>10 · `PlayerDataKey.cs:13` —— 原版成就（读名字符串实证：`PlayFabReadOnlyDataKeys.cs:5`）。
        /// 【我们】**本轮空缺**：成就存在 `WarpforgeProfile/profile.json` 的 `achIds/achValues`
        /// （`Shell/ProfileData.cs` 的 `SaveDto`），**按简报只迁 `avatarIndex/titleIndex`** ⇒ 成就这半没导（见报告 §五，建议下一轮补）。</summary>
        public const string AchievementsData = "AchievementsData";

        /// <summary>11 · `PlayerDataKey.cs:14` —— 原版成就（新版）。【我们】**空缺**。</summary>
        public const string AchievementsNewData = "AchievementsNewData";

        /// <summary>12 · `PlayerDataKey.cs:15` —— 原版成就（本期）。【我们】**空缺**。</summary>
        public const string AchievementsCurrentData = "AchievementsCurrentData";

        /// <summary>13 · `PlayerDataKey.cs:16` —— 原版外观选择（头像/头衔一族）。
        /// 【我们】`WarpforgeProfile/profile.json` 的 **`avatarIndex` + `titleIndex` 两个字段**（只迁这两个）。</summary>
        public const string CosmeticData = "CosmeticData";

        /// <summary>14 · `PlayerDataKey.cs:17` —— 原版提示条。【我们】**空缺**。</summary>
        public const string TipsData = "TipsData";

        /// <summary>15 · `PlayerDataKey.cs:18` —— 原版演示数据。【我们】**空缺**。</summary>
        public const string DemoData = "DemoData";

        /// <summary>16 · `PlayerDataKey.cs:19` —— 原版好友。【我们】**空缺**（`Shell/SocialData.cs` 不落盘）。</summary>
        public const string FriendsData = "FriendsData";

        /// <summary>17 · `PlayerDataKey.cs:20` —— 原版特惠。【我们】**空缺**。</summary>
        public const string SpecialOffers = "SpecialOffers";

        /// <summary>18 · `PlayerDataKey.cs:21` —— 原版上一个对手。【我们】**空缺**。</summary>
        public const string LastOpponentInfo = "LastOpponentInfo";

        /// <summary>19 · `PlayerDataKey.cs:22` —— 原版每日**限额**（刷新时间那一类）。
        /// 【我们】**空缺**：`WarpforgeDaily/daily.json` 的 `nextResetTicks/nextSet` 目前**整份**归在 `DailyCounters`
        /// （本轮不拆 —— 原版这两个 key 的 schema 没解出来，硬拆 = 自创口径；见报告 §五）。</summary>
        public const string DailyLimits = "DailyLimits";

        /// <summary>20 · `PlayerDataKey.cs:23` —— 原版每日**计数**（读名字符串实证：`PlayFabReadOnlyDataKeys.cs:9`）。
        /// 【我们】`WarpforgeDaily/daily.json`（`Shell/DailyData.cs:1697` 目录 + `:1699` 文件名）——**整份**导入。</summary>
        public const string DailyCounters = "DailyCounters";

        /// <summary>21 · `PlayerDataKey.cs:24` —— 原版开箱计数。【我们】**空缺**。</summary>
        public const string CrateCounters = "CrateCounters";

        /// <summary>22 · `PlayerDataKey.cs:25` —— 原版赛季（原版有 `PlayerSeason` 类：`PlayerDataManager.cs:5204`）。
        /// 【我们】`WarpforgeRanked/ranked.json`（`Shell/RankedData.cs:154,193`）—— 语义对应、**payload 是我们自己的形状**。</summary>
        public const string PlayerSeason = "PlayerSeason";

        /// <summary>23 · `PlayerDataKey.cs:26` —— 原版公会。【我们】**空缺**（公会窗读的是本地静态数据）。</summary>
        public const string GuildData = "GuildData";

        /// <summary>24 · `PlayerDataKey.cs:27` —— 原版等级数据（语义未证）。【我们】**空缺**；
        /// 我们的**卡牌等级/拥有数**另立 `_ours_CardProgress`（原版 `LevelData` 到底指什么没查到，⛔ 不硬套）。</summary>
        public const string LevelData = "LevelData";

        /// <summary>25 · `PlayerDataKey.cs:28` —— 原版公会邀请。【我们】**空缺**。</summary>
        public const string GuildInvites = "GuildInvites";

        /// <summary>26 · `PlayerDataKey.cs:29` —— 原版对局历史（实证：`PlayerDataManager.cs:11548`
        /// `GetPlayerOnlineData<object>(..., PlayerDataKey.BattleLogData)`）。
        /// 【我们】`WarpforgeBattleLog/battlelog.json`（`Shell/BattleLogData.cs:182,184`）——整份导入。</summary>
        public const string BattleLogData = "BattleLogData";

        /// <summary>27 · `PlayerDataKey.cs:30` —— 原版最后已读消息时间。【我们】**空缺**。</summary>
        public const string LastMessageReadTime = "LastMessageReadTime";

        /// <summary>28 · `PlayerDataKey.cs:31` —— 原版上赛季名次。【我们】**空缺**（`ranked.json` 里的
        /// `lastPlayedSeason/savedTier` 整份在 `PlayerSeason`，本轮未拆）。</summary>
        public const string LastSeasonPosition = "LastSeasonPosition";

        /// <summary>29 · `PlayerDataKey.cs:32` —— 原版屏蔽名单。【我们】**空缺**（屏蔽不落盘）。</summary>
        public const string BlockedPlayers = "BlockedPlayers";

        /// <summary>30 · `PlayerDataKey.cs:33` —— 原版举报名单。【我们】**空缺**。</summary>
        public const string ReportsPlayers = "ReportsPlayers";

        /// <summary>31 · `PlayerDataKey.cs:34` —— 原版促销。【我们】**空缺**。</summary>
        public const string UserPromotions = "UserPromotions";

        /// <summary>32 · `PlayerDataKey.cs:35` —— 原版挑战计数。【我们】**空缺**。</summary>
        public const string ChallengeCounters = "ChallengeCounters";

        /// <summary>33 · `PlayerDataKey.cs:36` —— 原版后台工具。【我们】**空缺**。</summary>
        public const string ModTools = "ModTools";

        /// <summary>34 · `PlayerDataKey.cs:37` —— 原版新卡标记（原版有 `NewCards` 类：`PlayerDataManager.cs:5190`）。
        /// 【我们】**空缺**。</summary>
        public const string NewCards = "NewCards";

        /// <summary>35 · `PlayerDataKey.cs:38` —— 原版推荐。【我们】**空缺**。</summary>
        public const string ReferralData = "ReferralData";

        /// <summary>36 · `PlayerDataKey.cs:39` —— 原版战将数据。【我们】**空缺**（我们战将状态是静态定义）。</summary>
        public const string WarlordData = "WarlordData";

        /// <summary>37 · `PlayerDataKey.cs:40` —— 原版可疑行为。【我们】**空缺**。</summary>
        public const string SuspiciousData = "SuspiciousData";

        /// <summary>38 · `PlayerDataKey.cs:41` —— 原版特惠数据。【我们】**空缺**。</summary>
        public const string SpecialOffersData = "SpecialOffersData";

        /// <summary>39 · `PlayerDataKey.cs:42` —— 原版等级新闻。【我们】**空缺**。</summary>
        public const string LevelNewsData = "LevelNewsData";

        /// <summary>40 · `PlayerDataKey.cs:43` —— 原版技能。【我们】**空缺**。</summary>
        public const string UserSkills = "UserSkills";

        /// <summary>41 · `PlayerDataKey.cs:44` —— 原版 LiveOps。【我们】**空缺**。</summary>
        public const string LiveOps = "LiveOps";

        /// <summary>42 · `PlayerDataKey.cs:45` —— 原版已兑换。【我们】**空缺**。</summary>
        public const string RedeemedData = "RedeemedData";

        /// <summary>43 · `PlayerDataKey.cs:46` —— 原版收藏等级。【我们】部分等价物是 `_ours_CardProgress`
        /// （等级/拥有数/通配符），但**不是同一个东西** ⇒ 仍记为空缺。</summary>
        public const string CollectorLevel = "CollectorLevel";

        /// <summary>44 · `PlayerDataKey.cs:47` —— 原版胜利计数。【我们】**空缺**。</summary>
        public const string VictoryCounters = "VictoryCounters";

        /// <summary>45 · `PlayerDataKey.cs:48` —— 原版分群。【我们】**空缺**。</summary>
        public const string Segmentation = "Segmentation";

        /// <summary>46 · `PlayerDataKey.cs:49` —— 原版成就（第三种写法）。【我们】**空缺**（同 `AchievementsData`）。</summary>
        public const string Achievements = "Achievements";

        /// <summary>47 · `PlayerDataKey.cs:50` —— 原版改名次数。【我们】**空缺**
        /// （玩家名在 `profile.json` 的 `playerName`，但**次数**没落盘）。</summary>
        public const string NameChanges = "NameChanges";

        /// <summary>48 · `PlayerDataKey.cs:51` —— 原版掉落表计数。【我们】**空缺**。</summary>
        public const string DropTableCounters = "DropTableCounters";

        /// <summary>49 · `PlayerDataKey.cs:52` —— 原版教程（v2）。【我们】教程进度在 `PlayerPrefs`
        /// （`Shell/MenuTutorialFlow.PrefDonePrefix`，见 `Shell/MenuTutorialManager.cs:329`）—— 属于**现有读写路径**，
        /// 本轮不迁（简报只点名「语言/音量」不迁；教程这条已写进报告 §五，Step 4 再定）。</summary>
        public const string TutorialV2 = "TutorialV2";

        /// <summary>50 · `PlayerDataKey.cs:53` —— 原版**奖励库存**（实证：`PlayerDataManager.cs:11656`
        /// `GetOnlineData<RewardInfo[]>(PlayerDataKey.EphemeralInventory)`）。
        /// 【我们】**空缺** —— ⚠️ 它**不是**背包/商城拥有数（那两样在 `WarpforgeInventory/*.json`，原版在活动的 `TSave` 里）。</summary>
        public const string EphemeralInventory = "EphemeralInventory";

        /// <summary>51 · `PlayerDataKey.cs:54` —— 原版卡组快表。【我们】**空缺**。</summary>
        public const string PlayerDecksFast = "PlayerDecksFast";

        // ════════════════════════════════════════════════════════════════════════════════════
        //  表本体 + 自检
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>静态构造 = **第一次碰到键表就自检一次**（坏了当场吼，不等到读档才发现）。
        /// ⚠️ 只在**第一次**进这个类时跑；`ValidateTable()` 可以随时再调（自检宿主里调它）。</summary>
        static SaveKeys()
        {
            string bad = ValidateTable();
            if (!string.IsNullOrEmpty(bad))
                Debug.LogError("[SaveKeys] 键表自检**失败**：" + bad + " ⇒ `SaveCenter` 会拒掉不认识的 key，"
                             + "所有落盘调用都会出声失败。先修 `Shell/SaveKeys.cs`。");
        }

        /// <summary>有效数据 key 总数（`undefined = 0` 不算）。⚠️ 改常量必须同步改这里 + <see cref="All"/>。</summary>
        public const int Count = 51;

        /// <summary>全部数据 key，**顺序 = 原版枚举值 1..51**（对着 `PlayerDataKey.cs` 逐行核）。</summary>
        public static readonly string[] All =
        {
            ProfileVersion,            //  1
            ActivateDevTools,          //  2
            ChatBanned,                //  3
            GlobalChatBanned,          //  4
            Decks,                     //  5
            PlayerDecks,               //  6
            CurrentDeck,               //  7
            ActiveEvent,               //  8
            ActiveCampaign,            //  9
            AchievementsData,          // 10
            AchievementsNewData,       // 11
            AchievementsCurrentData,   // 12
            CosmeticData,              // 13
            TipsData,                  // 14
            DemoData,                  // 15
            FriendsData,               // 16
            SpecialOffers,             // 17
            LastOpponentInfo,          // 18
            DailyLimits,               // 19
            DailyCounters,             // 20
            CrateCounters,             // 21
            PlayerSeason,              // 22
            GuildData,                 // 23
            LevelData,                 // 24
            GuildInvites,              // 25
            BattleLogData,             // 26
            LastMessageReadTime,       // 27
            LastSeasonPosition,        // 28
            BlockedPlayers,            // 29
            ReportsPlayers,            // 30
            UserPromotions,            // 31
            ChallengeCounters,         // 32
            ModTools,                  // 33
            NewCards,                  // 34
            ReferralData,              // 35
            WarlordData,               // 36
            SuspiciousData,            // 37
            SpecialOffersData,         // 38
            LevelNewsData,             // 39
            UserSkills,                // 40
            LiveOps,                   // 41
            RedeemedData,              // 42
            CollectorLevel,            // 43
            VictoryCounters,           // 44
            Segmentation,              // 45
            Achievements,              // 46
            NameChanges,               // 47
            DropTableCounters,         // 48
            TutorialV2,                // 49
            EphemeralInventory,        // 50
            PlayerDecksFast,           // 51
        };

        // ════════════════════════════════════════════════════════════════════════════════════
        //  我们自定的 key —— **必须** `_ours_` 前缀（方案 §九·1：原版没有的要显式标出来）
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>我们自定 key 的前缀（`SaveCenter` 认它 ⇒ 加新 key 不用改键表，但要带这个前缀）。</summary>
        public const string OursPrefix = "_ours_";

        /// <summary>`_ours_` · 卡牌等级 / 拥有数 / 通配符（`WarpforgeCardLevels.json`，`Shell/CardDetailPopup.cs`
        /// 的 `CardProgress.SaveFileName`）。**原版没有对应 key**：原版这三样由服务端 inventory 管；
        /// `EphemeralInventory` 是**奖励库存**（`PlayerDataManager.cs:11656` 读成 `RewardInfo[]`），⛔ 不是这个。</summary>
        public const string OursCardProgress = OursPrefix + "CardProgress";

        /// <summary>`_ours_` · 背包/商城拥有数的**前缀**（真实 key = 前缀 + 背包名，例：`_ours_Inventory.Shop`）。
        /// 来源 = `WarpforgeInventory/<name>.json`（`Shell/Inventory.cs:376` 的 `InventoryStore.StoreDirName`）。
        /// **原版没有对应 key**：原版拥有数在**每个活动的 `TSave`** 里（`Shell/ShopData.cs:281-290` 已如实标注）。</summary>
        public const string OursInventoryPrefix = OursPrefix + "Inventory.";

        // ════════════════════════════════════════════════════════════════════════════════════
        //  原版存在、但**不在** `PlayerDataKey` 枚举里的 key（照抄原名字符串）
        //  ⚠️ 不进 `All`（否则「51 条」对账会错）；Step 4 用到时再说。
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>原版字符串 key（`PlayFabReadOnlyDataKeys.cs:7`）—— 当前选中的对象。【我们】**空缺**。</summary>
        public const string CurrentSelectedObjects = "CurrentSelectedObjects";

        /// <summary>原版字符串 key（`PlayFabReadOnlyDataKeys.cs:13`）—— 是否发过起始卡组。【我们】**空缺**。</summary>
        public const string StarterDeckGranted = "StarterDeckGranted";

        /// <summary>原版字符串 key（`PlayFabReadOnlyDataKeys.cs:15`）—— 每活动存档（`EventSaveGames`）。【我们】**空缺**。</summary>
        public const string EventSaveGames = "EventSaveGames";

        // ════════════════════════════════════════════════════════════════════════════════════
        //  纯函数（不碰盘）
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>这个 key 允不允许落盘：键表里的 51 个，或 `_ours_` 前缀的自定 key。
        /// <para>⛔ 别为了省事放行任意字符串 —— 那等于把「键表」这层防线拆掉（拼错 = 静默孤儿文件）。</para></summary>
        public static bool IsKnown(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (key.StartsWith(OursPrefix, StringComparison.Ordinal)) return true;
            for (int i = 0; i < All.Length; i++)
                if (All[i] == key) return true;
            return false;
        }

        /// <summary>键表的自检：返回空串 = 通过；非空 = 出错原因（`SaveCenter` / 自检直接打出来）。</summary>
        public static string ValidateTable()
        {
            if (All == null) return "`All` 是 null";
            if (All.Length != Count) return "`All.Length` = " + All.Length + " 与 `Count` = " + Count + " 不一致";
            for (int i = 0; i < All.Length; i++)
            {
                string k = All[i];
                if (string.IsNullOrEmpty(k)) return "第 " + (i + 1) + " 条是空的";
                if (!IsAsciiKey(k)) return "第 " + (i + 1) + " 条 `" + k + "` 含非法字符（只允许字母/数字/下划线/点）";
                for (int j = i + 1; j < All.Length; j++)
                    if (All[j] == k) return "第 " + (i + 1) + " 与第 " + (j + 1) + " 条重复：`" + k + "`";
            }
            if (IsKnown("*")) return "`IsKnown(\"*\")` 居然是 true —— 键表防线没生效";
            return "";
        }

        /// <summary>key 能不能安全地当文件名（只允许 ASCII 字母 / 数字 / `_` / `.`；⛔ 不许路径分隔符）。</summary>
        public static bool IsAsciiKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '.';
                if (!ok) return false;
            }
            return true;
        }
    }
}
