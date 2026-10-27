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
            /// <summary>🆕 **2026-10-25（E4 · 账 330②）**：好友的玩家 id —— `ChatPlayerOptionsPanel.ShowOptionsFor`
            /// 里 `AddButton.interactable = 好友表里查不到该 playerId` 的**必要输入**（原版 `FriendInfo.playFabId`）。
            /// ⚠️ 本地没有写入方（好友表恒空）⇒ 填上真数据之前这条判据恒为「查不到 ⇒ 可点」。</summary>
            public string Id;
            /// <summary>在线 ⇒ 建 `Connected Image`（原版 act F，运行时才开）；离线 ⇒ `Disconnected`。
            /// 🆕 **2026-10-27（C5 · 账 369）**：四档另有 <see cref="Status"/>（原版 `ChatPlayerStatus`：0 Default /
            /// 1 Disconnected / 2 Away / 3 Connected）。**`Status &gt; 0` 时以它为准**，否则回落到 `Online`。</summary>
            public bool Online; public int Status;
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
            /// <summary>🆕 2026-10-28（B88-B7 · 账 14①）：原版 `Group.badge`（`GroupBadge` → `featuredTrophy.reward`）拆成的两格 ——
            /// `BadgeId` = `AllianceBadge.uniqueId`（映射见 `AllianceBadgeArt`）· `BadgeTier` = `GroupBadge.tierLevel`
            /// （画档位框 `Max(0,tier-1)`）。默认 null/0 ⇒ 默认徽章 + TIER1（= 原版 `DefaultTrophy` 回落支）。⚠️ 本地无写入方。</summary>
            public string BadgeId; public int BadgeTier;
        }

        static readonly List<Invitation> _invitations = new List<Invitation>();
        public static List<Invitation> Invitations { get { return _invitations; } }

        // ============================================================ 公开联盟（`AllianceListEntry`）

        /// <summary>公开列表里的一行（原版 `AllianceListEntry`；尾部只有一个 `Join`）。</summary>
        public class AllianceListing
        {
            public string Name, Region, Rating;
            public int Members, MemberMax;
            /// <summary>🆕 **2026-10-27（E9 · 账 21）**：原版 `Group.privacy`（`GroupPrivacy`：**0 = Open / 1 = Private**，
            /// 判据 = `out_cpp2il_dev_ilspy/GroupPrivacy.cs` 的枚举序）。两个消费者都在
            /// `AllianceListEntry.Initialize`（`GroupPrivacy.cs` 同批实读）：
            /// ① `joinButton.interactable = privacy == Open &amp;&amp; MemberCount &lt; maxMemberCount`；
            /// ② `privacy == Private` ⇒ `joinButton.text` 换成词条 `SocialMenu/Alliances/Privacy/Private`。
            /// 默认 0（Open）= 「公开」是这一列的本义，也保住既有夹具（不写这个字段）的可见态不变。</summary>
            public int Privacy;
            /// <summary>🆕 2026-10-28（B88-B7 · 账 21①）：同 `Invitation.BadgeId / BadgeTier` —— 原版
            /// `AllianceListEntry.Initialize` 里 `badgeDrawer.Draw(Group__get_Badge(group))`（`AllianceListEntry__Initialize.c:80-81`）
            /// 要的那两格。默认 null/0 ⇒ 默认徽章 + TIER1（原版 `DefaultTrophy` 回落支）。⚠️ 本地无写入方。</summary>
            public string BadgeId; public int BadgeTier;
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
            /// <summary>🆕 **2026-10-25（N9 · 账 31）**：数值评分（原版 `GroupMember.Rating`，**int**）。
            /// 原版 `AllianceMemberList.Initialize` 用 `OrderByDescending(m =&gt; m.Rating)` 排序、
            /// 行号取**排序后序号**（判据 → `AllianceMemberList.cs` 的两个排序 lambda `b__6_0` / `b__6_1`）。
            /// ⚠️ 与 `DraftRating` / `RankedRating`（**显示串**）不是一回事：那两个只是行里印出来的字。</summary>
            public int Rating;
            /// <summary>🆕 **2026-10-27（C5 · 账 373④ / 123）**：成员的原版 id（`GroupMember.id`）。
            /// 两个消费者：`SetCurrentGroupCached` 的**按 id 去重**，以及 `AllianceMemberOptionsPopup` 的
            /// `IsFriend`（那一半归账 30）。⚠️ 本地没有写入方（联盟成员表恒空）。</summary>
            public string Id;
            /// <summary>🆕 **2026-10-27（E9 · 账 30①）**：成员角色的**数值**（原版 `GroupMember.role`，
            /// `GroupRole`：`0 Member / 1 Moderator / 2 Admin / 3 Leader`，判据 = `out_cpp2il_dev_ilspy/GroupRole.cs`
            /// ＋ `AllianceMemberEntry.Initialize` 把 `((Enum)role).ToString()` 拼成词条键 `SocialMenu/Alliances/Role/{名}`）。
            /// <para>🔴 **与 <see cref="Role"/> 不是一回事**：那个是**显示串**（原版样例 `Alliance Master`），
            /// 只用于行里的字；这一格才是 `AllianceMemberOptionsPopup.MemberView.Role` 要的 **int**
            /// （`outrank = SameGroup &amp;&amp; Role &lt; MyRole` ⇒ Promote/Demote/Kick 三颗的显隐判据）。</para>
            /// <para>⚠️ 本地没有写入方（联盟成员表恒空）⇒ 默认 0；填真数据时**两格要一起填**
            /// （`RoleText(RoleId)` 能直接算出 <see cref="Role"/>，见 `SocialData.RoleText`）。</para></summary>
            public int RoleId;
        }

        static readonly List<Member> _members = new List<Member>();
        public static List<Member> Members { get { return _members; } }

        // ============================================================ 聊天（`ChatMessageUI` / `ChatTab`）

        /// <summary>一条聊天消息（原版 `ChatEntryView` / `ChatMessageUI` 的那几个字段）。
        /// ⚠️ 频道只有两个值：`Global` / `Alliance`（`ChatRoom` 枚举）—— **字符串就是它俩**。</summary>
        public class ChatMessage
        {
            // 原版 `ChatMessageType` 的 17 个值（判据 = `out_cpp2il_dev_ilspy/ChatMessageType.cs`，逐个照抄）。
            // ⚠️ 只有下面标了「有模板」的 6 个会在 `Text()` 里出正文；其余原版一律 `return ""`。
            public const int TypePractice = 0, TypeDeletePractice = 10, TypeJoin = 20, TypeLeave = 30,
                             TypeAskLeadership = 40, TypeMessage = 50, TypeKickPlayer = 60, TypeInvitePlayer = 70,
                             TypePromoted = 80, TypeDemoted = 90, TypeAnnouncement = 100,
                             TypePracticeMatchStarted = 110, TypePracticeMatchEnd = 120, TypeRally = 130,
                             TypeReplayShare = 140, TypeDeckShare = 150, TypeRewardShare = 160;
            /// <summary>原版 `ChatMessageType` 的数值（判据 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ChatMessageType.cs`，
            /// 17 个值逐个照抄）。🆕 **2026-10-27（C5 · 账 291/302）**：`GetMessage()` 就是按它分支的。
            /// <para>🔴 **默认 = `Message(50)`（字段初始化器）**：本仓既有的聊天夹具（`Editor/MainMenuScene.cs`
            /// 那一条 `ChatMessages.Add(new …{ Channel, Sender, Time, Text … })`）不写 `Type`
            /// ⇒ 若默认是 0（`Practice`）那一档 `GetMessage()` 返回空串，**既有断言会一夜变红**。
            /// 原版没有「缺省类型」这一说（服务端每条都带类型）⇒ 这个缺省是**我们的**、只服务本地夹具。</para>
            public int Type = 50;        // ChatMessageType.Message
            /// <summary>🔴 **2026-10-27 恢复（调度台）**：C5 重写本类时把这格删了，而 `ChatTab.cs:156/176`
            /// 与 `MainMenuRuntime.cs:3700` 都在读它 ⇒ 全仓 CS1061。原语义不变：频道名（<c>"Global"</c> / <c>"Alliance"</c>）。</summary>
            public string Channel;
            public string Sender;
            public string Time;          // 原版样例 `1d 6h`（相对时间，服务器给；**没有 TimestampMs 时用它**）
            /// <summary>🆕 **2026-10-27（C5 · 账 305）**：原版 `ChatMessageData.timestamp`（Unix 毫秒）。
            /// 原版 `ChatMessageUI.UpdateTimer` = `FromUnixTimeMilliseconds(timestamp)` 与 `UtcNow` 求差
            /// ⇒ `GetTimeStringWithSingleDigit(ts, eventTimer:true)`。**&gt;0 才现算**，否则回落到 <see cref="Time"/>。</summary>
            public long TimestampMs;
            /// <summary>原版 `ChatMessageData.data`：Join/Leave/KickPlayer 时是**词条键**；
            /// Promoted/Demoted 时是**角色**（int 或 `a/b/Role` 这种串）；DeckShare 时是**多行文本，末行才是卡组串**。</summary>
            public string LocData;
            /// <summary>原版 `ChatMessageData.targetPlayerName`（`{targetPlayer}` 那个占位符）。</summary>
            public string TargetPlayer;
            /// <summary>原版 `ChatMessageTranslation`：0 Normal / 1 Translating / 2 Translated。
            /// 本地**没有翻译服务**（`TranslationService` 走远端）⇒ 恒 0（见 `ChatMessageRow` 正文那一段）。</summary>
            public int TranslationStatus;
            public string Text;
            /// <summary>自己发的 ⇒ 走 `Player Header`（头像靠右）；别人的 ⇒ `Friend Header`（头像靠左）。</summary>
            public bool Mine;
            /// <summary>🆕 **2026-10-25（E4 · 账 303/314/330）**：发信人的玩家 id ——
            /// 选项面板那两件（`Add as a friend.interactable` = 好友表里查不到该 id · `Block`/`Report`/`Profile`
            /// 的目标）都要它（原版 `ChatMessageData.playerPlayfabId`）。
            /// ⚠️ 本地**没有写入方**（聊天表由自检填）⇒ 默认 null；判据链已通，真数据到了自己生效。</summary>
            public string PlayerId;
            public string AvatarArt;
            /// <summary>行高。&lt;= 0 就用原版 `_DefaultItemSize` = 60。</summary>
            public float Height;
        }

        static readonly List<ChatMessage> _chat = new List<ChatMessage>();
        public static List<ChatMessage> ChatMessages { get { return _chat; } }

        // ============================================================ 自检用

        /// <summary>清空全部表（与 `ForgeData.ResetForTest` / `BattleLogData.ResetForTest` 同族）。
        /// 🆕 **2026-10-04（A55④）**：两个**静态标量**（`AllianceName` / `AllianceTrophies`）也一起清 ——
        /// 它们原来**不在**清理范围内：自检喂完数据忘了手动清，值就会**漏到后面的断言里**
        /// （上一批那一段正是手动清的 —— `Editor/MainMenuScene.cs` 那两句「⚠️ 手动清（`ResetForTest`
        /// 不管这个字段）」，本批一并订正）。
        /// 判据 = 本文件表头那条语义「**默认全空**」：`AllianceName` 空 = 没有盟（原版
        /// `AlliancesManager.CurrentGroupCached` 恒空 ⇒ 恒走未入盟支）；`AllianceTrophies` 0 = 一个没达成。</summary>
        public static void ResetForTest()
        {
            _friends.Clear(); _invitations.Clear(); _openAlliances.Clear(); _members.Clear(); _chat.Clear();
            AllianceName = null;
            AllianceTrophies = 0;
            MyRoleId = 0;                 // 🆕 2026-10-27（E9 · 账 30）：静态标量，同上面两颗一起清
        }

        /// <summary>联盟人数上限（原版 `AlliancesConfig.maxMemberCount`，`Members: {n}/{cap}` 的那个 `cap`）。
        /// 🔴 **2026-10-25（N9 · 账 31）**：原版读的是 `ConfigManager.GetConfig&lt;AlliancesConfig&gt;()`，
        /// **本仓没有那份配置资产** ⇒ 取 **20**：它就是 prefab 出厂样例串 `Members: --/20`
        /// （`MonoBehaviour_5317387791711264931.json` / `…_6451041557333008547.json` 的 `m_text` 实读）
        /// 里印的那个数。⚠️ **这是「prefab 样例值」，不是从配置读到的真值**（本地无该配置）。</summary>
        public static int MemberCap = 20;

        // ============================================================ 🆕 2026-10-27（C5 批）
        // 🔴 纪律：这一整段**追加在类尾**（本文件被账按行号引用 —— 行号只在上面的 `Member.Id`
        //    那一处 +1，其余改动全是「末尾追加」）。

        /// <summary>本盟缓存的写入时刻（原版 `AlliancesManager.currentGroupCachedAt`；原版存的是
        /// **服务器时间** `ServerDateTime`，本地没有服务器时钟 ⇒ 用 `DateTime.UtcNow`）。
        /// 判据 = `AlliancesController__SetCurrentGroupCached.c` 与 `…__InvalidateLocalGroup.c`。</summary>
        public static System.DateTime CachedAt;

        /// <summary>= 原版 `AlliancesController.InvalidateLocalGroup()`（**逐句**，判据
        /// `d:/2/tools/decomp_full/AlliancesController__InvalidateLocalGroup.c`）：
        /// ① `currentGroupCached = null` ⇒ 本仓 = <see cref="AllianceName"/> 置空
        /// ② 成员表清空 ③ `currentGroupCachedAt = default` ⇒ <see cref="CachedAt"/> 归零。
        /// <para>⚠️ **调用点**：原版谁调它本代理**没查到**（`grep InvalidateLocalGroup` 在 ILSpy 全树只有
        /// 定义本身 —— 与「ILSpy 会丢异步状态机体 / 会内联」同族，**没查到 ≠ 不存在**）⇒ 本仓**不接线**，
        /// 只提供 API 并出声（⛔ 不自己发明触发源）。</para></summary>
        public static void InvalidateLocalGroup()
        {
            AllianceName = null;
            _members.Clear();
            CachedAt = default(System.DateTime);
            UnityEngine.Debug.Log("[SocialData] `InvalidateLocalGroup()` ⇒ 盟名清空 / 成员表清空 / 缓存时刻归零"
                                + "（原版 `AlliancesController.InvalidateLocalGroup`；本地没有触发源，只留 API）");
        }

        /// <summary>= 原版 `AlliancesController.SetCurrentGroupCached(Group g)`（**逐句**，判据
        /// `AlliancesController__SetCurrentGroupCached.c`）：写盟名 + 把 `g.members` 并入成员表 + 记缓存时刻。
        /// <para>① **按 id 去重替换**（原版是整表替换语义：先从 `members` 里删掉同 id 的旧条目、再追加）；
        /// ② **同名不同 id 都保留**（判据只看 id）；③ `CachedAt = DateTime.UtcNow`（原版是 ServerDateTime）。</para></summary>
        public static void SetCurrentGroupCached(string name, IList<Member> members)
        {
            AllianceName = name;
            if (members != null)
            {
                for (int i = 0; i < members.Count; i++)
                {
                    var m = members[i];
                    if (m == null) continue;
                    int at = -1;
                    if (!string.IsNullOrEmpty(m.Id))
                        for (int k = 0; k < _members.Count; k++)
                            if (_members[k] != null && _members[k].Id == m.Id) { at = k; break; }
                    if (at >= 0) _members[at] = m; else _members.Add(m);
                }
            }
            CachedAt = System.DateTime.UtcNow;
        }

        /// <summary>= 原版 `FriendsData.IsFriend(string playerId)`（判据 `FriendsData.cs:40`）：
        /// 好友表里有没有这个 id。**✅ 全仓唯一一份** —— `ChatPanel.IsFriend` 转调这里
        /// （两处写同一规则 = 迟早不一致）。空 id 一律 false（原版没有 id 的 `Friend` 也匹配不上）。</summary>
        public static bool IsFriend(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return false;
            for (int i = 0; i < _friends.Count; i++)
                if (_friends[i] != null && _friends[i].Id == playerId) return true;
            return false;
        }

        // ============================================================ 聊天气泡正文（账 291 / 302）
        /// <summary>= 原版 `ChatMessageData.GetMessage()`（**逐句**，判据 `decomp_full/ChatMessageData__GetMessage.c`
        /// ＋ `out_cpp2il_dev_ilspy/ChatMessageData.cs:138-266`）。
        /// <para>**原版只有 6 条模板 + 2 条直通**（′15 种′ 那个旧口径是错的，见 `B9_社交C_派活包.md` §0·2③）：</para>
        /// <list type="bullet">
        /// <item>`Message(50)` / `Announcement(100)` ⇒ `translatedMessage ?? msg`（本地无翻译服务 ⇒ 恒 `msg`）。</item>
        /// <item>`Join(20)` / `Leave(30)` / `KickPlayer(60)` ⇒ 键 `SocialMenu/Alliances/Messages/{类型名}`，
        ///       替换 `{playerName}` `{targetPlayer}`，再替换 `{locData}` = `GetTranslation(data)`。</item>
        /// <item>`Promoted(80)` / `Demoted(90)` ⇒ 同上，但 `{locData}` = **角色词条**
        ///       （`data` 先 TryParse 成 int、否则按 `'/'` 切末段 Enum.TryParse；解析失败 ⇒ 空串）。</item>
        /// <item>`DeckShare(150)` ⇒ 键 `…/Message/DeckShare`，`string.Format(键, playerName, deck.deckName)`；
        ///       卡组串 = `data.Split('\n')` 的**最后一行**（原版走 `CardDeck.DeserializeDeckString`）。</item>
        /// <item>其余（0/10/40/70/110/120/130/140/160）⇒ **空串**（原版就是 `return ""`）。</item>
        /// </list>
        /// <para>⚠️ **两处与原版的差异（如实记）**：① 原版 `data == null` / 卡组串解析不出时**抛 NRE**
        /// （`LAB_18065509f` 是 noreturn；服务端不会发坏数据）⇒ 我们回空串 + 出声一次，**不复刻崩溃**；
        /// ② 原版 `GetTranslation` 拿不到键时回空、我们走 `Loc.T` —— 这 6 条键**官方表全有**（已逐条现读）。</para></summary>
        public static string Text(ChatMessage m)
        {
            if (m == null) return "";
            string sender = m.Sender ?? "";
            switch (m.Type)
            {
                case ChatMessage.TypeMessage:
                case ChatMessage.TypeAnnouncement:
                    return m.Text ?? "";
                case ChatMessage.TypeJoin:
                case ChatMessage.TypeLeave:
                case ChatMessage.TypeKickPlayer:
                {
                    string t = Loc.T("SocialMenu/Alliances/Messages/" + TypeName(m.Type));
                    t = t.Replace("{playerName}", sender).Replace("{targetPlayer}", m.TargetPlayer ?? "");
                    return t.Replace("{locData}", string.IsNullOrEmpty(m.LocData) ? "" : Loc.T(m.LocData));
                }
                case ChatMessage.TypePromoted:
                case ChatMessage.TypeDemoted:
                {
                    string t = Loc.T("SocialMenu/Alliances/Messages/" + TypeName(m.Type));
                    t = t.Replace("{playerName}", sender).Replace("{targetPlayer}", m.TargetPlayer ?? "");
                    return t.Replace("{locData}", RoleText(ParseRole(m.LocData)));
                }
                case ChatMessage.TypeDeckShare:
                {
                    string data = m.LocData;
                    if (!string.IsNullOrEmpty(data))
                    {
                        int cut = data.LastIndexOf('\n');
                        string deckString = cut >= 0 ? data.Substring(cut + 1) : data;
                        var deck = RuleEngine.DeckLibrary.ImportString(deckString, CollectionData.Card);
                        if (deck != null)
                            return string.Format(Loc.T("SocialMenu/Alliances/Messages/DeckShare"), sender, deck.Name ?? "");
                    }
                    WarnOnce("deckshare", "[SocialData] `DeckShare` 消息的卡组串**解析不出来**"
                                       + "（原版这里会因空引用崩；我们回空串、不崩）");
                    return "";
                }
                default:
                    return "";       // 原版：其余类型一律 `return ""`
            }
        }

        /// <summary>= 原版 `ChatMessageUI.UpdateTimer()` 那一跳的**文本**：`DateTimeOffset.FromUnixTimeMilliseconds(timestamp)`
        /// 与 `UtcNow` 求差 ⇒ `SupportMethods.GetTimeStringWithSingleDigit(ts, eventTimer: true)`。
        /// <para>🆕 **2026-10-27（C5 · 账 305 / 298）**：`TimestampMs &gt; 0` 才现算；否则回落 <see cref="ChatMessage.Time"/>
        /// （预置串 —— 既有的夹具不写 timestamp，删了它会把自检打光）。</para>
        /// <para>判据（逐句） = `decomp_full/SupportMethods__GetTimeStringWithSingleDigit.c` +
        /// `out_cpp2il_dev_ilspy/SupportMethods.cs:1897-1945`：<b>Days &gt; 0 且 eventTimer</b> ⇒
        /// `MainMenu/Time/D`；否则 Hours ≥ 1 ⇒ `MainMenu/Time/H`；否则 Minutes &lt; 1 ⇒
        /// `MainMenu/Time/FewSeconds`（**无参**）；否则 `MainMenu/Time/M`。
        /// ⚠️ 用的是 `TimeSpan.Days/Hours/Minutes`（**分量**、不是 `TotalX`）—— 与 `TimerDisplay.FormatTimeString`
        /// 那条「严格大于 1 天」的口径**不是同一个函数**，⛔ 别互相顶替。</para></summary>
        public static string TimeText(ChatMessage m)
        {
            if (m == null) return "";
            if (m.TimestampMs <= 0) return m.Time ?? "";
            var then = System.DateTimeOffset.FromUnixTimeMilliseconds(m.TimestampMs).UtcDateTime;
            var ts = System.DateTime.UtcNow - then;
            if (ts < System.TimeSpan.Zero) ts = System.TimeSpan.Zero;      // 原版没有 clamp；本地时钟偏差保底
            if (ts.Days > 0) return string.Format(Loc.T("MainMenu/Time/D"), ts.Days);
            if (ts.Hours >= 1) return string.Format(Loc.T("MainMenu/Time/H"), ts.Hours);
            if (ts.Minutes < 1) return Loc.T("MainMenu/Time/FewSeconds");
            return string.Format(Loc.T("MainMenu/Time/M"), ts.Minutes);
        }

        /// <summary>原版 `ChatMessageType` 的枚举名（拼模板键用；只有 5 个值会用到）。</summary>
        static string TypeName(int t)
        {
            switch (t)
            {
                case ChatMessage.TypeJoin: return "Join";
                case ChatMessage.TypeLeave: return "Leave";
                case ChatMessage.TypeKickPlayer: return "KickPlayer";
                case ChatMessage.TypePromoted: return "Promoted";
                case ChatMessage.TypeDemoted: return "Demoted";
                default: return "";
            }
        }

        /// <summary>原版 `GroupRole`（0..3 = Member/Moderator/Admin/Leader）→ 词条
        /// `SocialMenu/Alliances/Role/{枚举名}`（官方表 4 条全有）。-1 = 解析失败 ⇒ 空串。</summary>
        public static string RoleText(int role)
        {
            if (role < 0 || role > 3) return "";
            return Loc.T("SocialMenu/Alliances/Role/" + RoleName(role));
        }

        static string RoleName(int role)
        {
            switch (role)
            {
                case 0: return "Member";
                case 1: return "Moderator";
                case 2: return "Admin";
                case 3: return "Leader";
                default: return "Member";
            }
        }

        /// <summary>原版 `GetMessage` 里 Promoted/Demoted 的 `data` → `GroupRole`：先 `int.TryParse`，
        /// 否则按 `'/'` 切**末段** `Enum.TryParse`。两者都失败 ⇒ -1（原版那一步会落进
        /// `GetTranslation("")`，等价于「角色一格是空」）。</summary>
        static int ParseRole(string data)
        {
            if (string.IsNullOrEmpty(data)) return -1;
            int v;
            if (int.TryParse(data, out v)) return v;
            int cut = data.LastIndexOf('/');
            string tail = cut >= 0 ? data.Substring(cut + 1) : data;
            for (int i = 0; i < 4; i++) if (RoleName(i) == tail) return i;
            return -1;
        }

        static readonly HashSet<string> _warned = new HashSet<string>();
        /// <summary>出声一次（按 key 去重；红线 = 不许静默失败、也不许每行刷屏）。</summary>
        static void WarnOnce(string key, string msg)
        {
            if (_warned.Add(key)) UnityEngine.Debug.LogWarning(msg);
        }

        // ============================================================ 🆕 2026-10-27（E9 批）
        // 🔴 纪律：这一整段**追加在类尾**（本文件被账按行号引用）。

        /// <summary>= 原版 `GameStaticData.GroupLanguages`（**两个下拉的选项表之一**，账 101）。
        /// <para>判据（**实读，不是我们编的**）= `out_cpp2il_dev_ilspy/GameStaticData.cs:2476-2507` 的静态构造：
        /// `GroupLanguages = new string[14] { "Global", "English", "Chinese", "Czech", "French", "German",
        /// "Italian", "Japanese", "Korean", "Polish", "Portuguese", "Russian", "Spanish", "Turkish" }`
        /// （原版那一段是逐槽逐字面量赋值，槽序即此序）。</para>
        /// <para>消费者 = 原版 `AllianceView.OnToggleEditClicked`：
        /// `languageDropdown.value = Array.IndexOf(GroupLanguages, alliance.region)` ——
        /// 那一跳是 `AllianceInvitationEntry` / `AllianceListEntry` 里
        /// `regionText = GetTranslation("SocialMenu/Alliances/Language/" + region)` 的**同一份词根**。</para>
        /// <para>⚠️ **我们建不出来的是「展开后的那一面」**：原版 `TMP_Dropdown` 的 `Template` 是
        /// **Unity 内置模板**（`Item Label = 'Option A'`，普查 §B·13 口径「不建」）⇒ 本表目前只用两处：
        /// ① 算「选中第几项」② 行里 `region` 的**词条键**（`Language/{region}`）。⛔ 别为了「看着能用」自造模板。</para></summary>
        public static readonly string[] GroupLanguages =
        {
            "Global", "English", "Chinese", "Czech", "French", "German", "Italian",
            "Japanese", "Korean", "Polish", "Portuguese", "Russian", "Spanish", "Turkish",
        };

        /// <summary>= 原版 `GroupPrivacy` 枚举的**选项表**（第二个下拉，账 101）：
        /// 判据 = `out_cpp2il_dev_ilspy/GroupPrivacy.cs`（`Open = 0 / Private = 1`，只有两个值）。
        /// <para>消费者 = 原版 `AllianceView.OnToggleEditClicked` 的 `privacyDropdown.value = (int)alliance.privacy`。
        /// ⚠️ 与 <see cref="GroupLanguages"/> 同一条边界：展开模板是 Unity 内置 ⇒ 不建。</para></summary>
        public static readonly string[] GroupPrivacyNames = { "Open", "Private" };

        /// <summary>本机玩家自己在当前盟里的 `GroupRole` 数值（原版
        /// `AlliancesManager.controller.CurrentPlayerCache.role`，`AllianceView.OnToggleEditClicked`
        /// 与 `AllianceMemberOptionsPopup.MemberView.MyRole` 都用它算 `outrank`）。
        /// <para>⚠️ 本地没有写入方（联盟成员表恒空）⇒ 默认 0；那一格决定
        /// `outrank = SameGroup &amp;&amp; Role &lt; MyRole` ⇒ 默认态下 Promote/Demote/Kick 恒不出（如实，不编）。</para></summary>
        public static int MyRoleId;

        /// <summary>`outrank` 要的「我的角色」：先取 <see cref="MyRoleId"/>（真数据来了就生效），
        /// 没设过就退到**成员表里标了 `IsPlayer` 那一行**的 <see cref="Member.RoleId"/>
        /// （原版这两个是同一个量：`CurrentPlayerCache.role` 就是「我」在 `g.members` 里的那一项的 role）。
        /// 两处都没有 ⇒ 0（= 最低档，与原版「无数据」等价）。</summary>
        public static int SelfRoleId()
        {
            if (MyRoleId > 0) return MyRoleId;
            for (int i = 0; i < _members.Count; i++)
                if (_members[i] != null && _members[i].IsPlayer) return _members[i].RoleId;
            return 0;
        }
    }
}
