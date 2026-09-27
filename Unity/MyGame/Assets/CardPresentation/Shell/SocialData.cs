// SocialData.cs — 社交那一批（联盟 / 成员 / 邀请 / 好友）的**本地数据源**
//
// ============================ 这是什么、不是什么 ============================
// 🔴 **原版这些全在服务器**（`AlliancesManager` / `FriendsManager` / `ChatGlobalManager`，
//    已关服）⇒ 本地**一条都取不到**（判据 → `资料/普查产出_0927/多人界面_入口与调用.md` §③
//    与 `社交_联盟与好友页.md` §C·4）。
// ⇒ 照「日常 / 锻造厂 / 战役 / 对局历史」那几层的先例：**表由我们自建**，默认**恒空**。
//    用户 2026-09-26 定的口径：「有什么复刻什么，**具体的数据和排名这些可以空着**」
//    —— 所以**别为了「好看」塞假数据**；页面留白就是原版没有数据时的样子（原版也没有 `Empty*` 节点）。
//
// ⚠️ **表是可写的**（不是只读的空表）：四款行模板（`AllianceInvitationEntry` /
//    `AllianceListEntry` / `AllianceMemberEntry` / `FriendInfoElement`）都照原版建好了，
//    自检要喂样本、或将来真有源，往这里 `Add` 就行 —— 同 `Shell/BattleLogData.cs` 那条先例。
// ⚠️ **字段名照原版**（`AllianceInvitationEntry` 的 `nameText/regionText/memberCountText/ratingText` 那些），
//    这样行 builder 与反编译能逐字段对上。
using System.Collections.Generic;

namespace CardPresentation
{
    /// <summary>社交那一批的本地数据（**默认全空**）。</summary>
    public static class SocialData
    {
        // ============================================================ 我所在的联盟（`AllianceMemberTab` / `GeneralDetails`）

        /// <summary>本盟的名字（`GeneralDetails>Alliance name text`）。**空 = 没有盟** ——
        /// 原版 `AlliancesManager.CurrentGroupCached` 就是这个语义（空 ⇒ 走「未入盟」那一支）。
        /// 🔴 **本地恒空** ⇒ `AlliancesTab` 永远停在未入盟支（如实记着，不是漏做）。</summary>
        public static string AllianceName;
        /// <summary>`TrophiesWindow` 那行「已达成 N 个」的 N（原版样例 `45 Trophies Achieved!`）。</summary>
        public static int AllianceTrophies;

        // ============================================================ 好友（`FriendUIElementWarpforge`）

        /// <summary>一个好友。原版 `FriendInfo`：名字 + 在不在线（决定两个状态点哪个亮）。</summary>
        public class Friend
        {
            public string Name;
            /// <summary>在线 ⇒ 建 `Connected Image`（原版 act F，运行时才开）；离线 ⇒ `Disconnected`。</summary>
            public bool Online;
        }

        static readonly List<Friend> _friends = new List<Friend>();
        public static List<Friend> Friends { get { return _friends; } }

        // ============================================================ 联盟邀请（`AllianceInvitationEntry`）

        /// <summary>一条入盟邀请（原版 `AllianceInvitationEntry` 的字段）。
        /// ⚠️ 原版行里那两个钮的文案是 **`Join` + `Dismiss`**（节点名却叫 `Reject`）。</summary>
        public class Invitation
        {
            public string Name;          // `nameText`
            public string Region;        // `regionText`（原版静态样例是 `Global`）
            public int Members, MemberMax;   // `memberCountText`（显示成 `17/20`）
            public string Rating;        // `ratingText`（段位图标旁的数字，本地没有段位系统 ⇒ 空串）
        }

        static readonly List<Invitation> _invitations = new List<Invitation>();
        public static List<Invitation> Invitations { get { return _invitations; } }

        // ============================================================ 公开联盟（`AllianceListEntry`）

        /// <summary>公开列表里的一行（原版 `AllianceListEntry`；尾部只有一个 `Join`）。</summary>
        public class AllianceListing
        {
            public string Name, Region, Rating;
            public int Members, MemberMax;
        }

        static readonly List<AllianceListing> _openAlliances = new List<AllianceListing>();
        public static List<AllianceListing> OpenAlliances { get { return _openAlliances; } }

        // ============================================================ 联盟成员（`AllianceMemberEntry`）

        /// <summary>一个成员（原版 `AllianceMemberEntry`）。⚠️ 行里两处评分圆：
        /// `Draft Rating` 的图标是**骷髅**（`40k_battle_Win Skull`）、`Ranked Rating` 的是**段位图标**
        /// （`40k_UI_icon_ranked_Skirmish`）—— 两处**不是同一张图**，别弄混（§A·2·3 第 474/478 行）。</summary>
        public class Member
        {
            public int Index;            // `indexText`（行首那个名次数字）
            public string Name;          // `playerName`
            public string Role;          // `playerRole`（原版样例 `Alliance Master`）
            public bool Online;          // `status`（在线点）
            public string AvatarArt;     // `playerAvatar` 的立绘（`Resources/Art/avatars/` 那批的名字）
            public string DraftRating, RankedRating;
            /// <summary>是不是「我」自己 —— 原版 `isPlayerEntryColor` 会给这一行的几件**换色**
            /// （`graphicsToChangeColorIfIsPlayer`）。我们照建这个字段，颜色值**本地读不到**（§C）⇒ 不换色、如实标着。</summary>
            public bool IsPlayer;
        }

        static readonly List<Member> _members = new List<Member>();
        public static List<Member> Members { get { return _members; } }

        // ============================================================ 聊天（`ChatMessageUI` / `ChatTab`）

        /// <summary>一条聊天消息（原版 `ChatEntryView` / `ChatMessageUI` 的那几个字段）。
        /// ⚠️ 频道只有两个值：`Global` / `Alliance`（`ChatRoom` 枚举）—— **字符串就是它俩**。</summary>
        public class ChatMessage
        {
            public string Channel;       // "Global" / "Alliance"
            public string Sender;
            public string Time;          // 原版样例 `1d 6h`（相对时间，服务器给）
            public string Text;
            /// <summary>自己发的 ⇒ 走 `Player Header`（头像靠右）；别人的 ⇒ `Friend Header`（头像靠左）。</summary>
            public bool Mine;
            public string AvatarArt;
            /// <summary>行高。<= 0 就用原版 `_DefaultItemSize` = 60。</summary>
            public float Height;
        }

        static readonly List<ChatMessage> _chat = new List<ChatMessage>();
        public static List<ChatMessage> ChatMessages { get { return _chat; } }

        // ============================================================ 自检用

        /// <summary>清空全部表（与 `ForgeData.ResetForTest` / `BattleLogData.ResetForTest` 同族）。</summary>
        public static void ResetForTest()
        {
            _friends.Clear(); _invitations.Clear(); _openAlliances.Clear(); _members.Clear(); _chat.Clear();
        }
    }
}
