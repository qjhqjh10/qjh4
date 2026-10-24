// ProfileData.cs — 玩家档案窗要用的两张清单（**称号**与**头像**）
//
// ============================ 这是什么、不是什么 ============================
// 🔴 **这是【原版资产的清单】，不是【玩家存档】**。
//    · 原版这两页的列表来自**服务器**（`PlayerDataManager.fullCosmeticCollection.OfType<CosmeticItemTitle>()` /
//      `PlayerAvatarDataManager.GetAllItems<T>()`），本地**没有存档**；
//    · 但**定义数据（ScriptableObject）在本地是全的** —— 470 个头像 SO + 462 个称号 SO
//      （`素材/Warpforge原版/装饰品/定义数据/MonoBehaviour/`，那是游戏自己的资产，不是存档）。
//    · ⇒ 由 `工具/gen_profile_cosmetics.py` 抽成 `Resources/profile_cosmetics.json` 给运行时读。
//    用户 2026-09-27 拍板：**列表照填**（配合他定的「资源 2000（用户 2026-10-24 从 9999 改） / 卡池与头像全解锁」那条边界）。
//
// 🔴 **显示名是我们拼的**（原版真名在远端 I2 语言表，本地只有 key）：
//    · 头像：`nameTextReference`（形如 `Avatar_UM_Attack Bike`）去掉 `Avatar_<阵营>_` 前缀 ⇒ `Attack Bike`；
//      变体头像的 `nameTextReference` 是**底图名**（原版就这么设计的，用于去重显示）。
//    · 称号：462 条的 `nameTextReference` **全是空串** ⇒ 只能拿 `m_Name`（`Title_UM_Premium_1`）
//      去掉 `Title_`、下划线换空格 ⇒ `UM Premium 1`。
//    ⚠️ **所以界面上那些名字不是原版文案**，是从资源名反推的 —— 这一点在代码注释与 `_note` 字段里都写着。
//
// 🔴 **2026-10-14（第二十八会话 · `A1803`）就地更正上面那句「不是原版文案」**：
//    官方 zh-CN 词条表 2026-10-14 已解开 ⇒ **上面那个「资源名反推」现在只是【兜底】** ——
//    取值改成先走 `Loc.BulkOr("Titles/" + <32hex id>, 反推名)` / `Loc.BulkOr("Avatars/" + <反推名>, 反推名)`
//    （键的拼法 = 原版 `GetTranslation("…")` 的拼法，见 `工具/_loc_official/gen_official_names.py` 文件头）。
//    现读命中：**Titles 459/462 · Avatars 468/469 · 成就名 102/102 · 成就描述 102/102**
//    （2026-10-22 P1 现读复核：成就那两列从 95 涨到 102 —— 上一批把官方表接全了；
//     Titles 459 是官方表本身的条数，余额 3 条**官方没有**。⚠️ 引用这些数一律现读，别抄。）
//    🔴 **2026-10-18（W18）就地订正**：上面那个 **Avatars 234/469** 是「**只拿反推显示名当键**」时的数 ——
//       原版的键其实是 `Avatars/<nameTextReference 原文>`：老一批 SO 的原文**不带前缀**
//       （`Aggressor Sergeant`）、新一批**带**（`Avatar_UM_Attack Bike`）⇒ 拿原文当键才两批都命中。
//       `gen_profile_cosmetics.py` 已把原文存成 `r` 字段，本文件按 `r → n → art` 的顺序取键
//       ⇒ **现读 Avatars 468/469**（只剩 `Terminator Sergeant`——官方表里确实没有，**保持英文原文**）。
//       ⚠️ 这个 234 不是「官方表缺 235 条」，是**键拼错**；这行旧读数留着只当更正痕迹。
//    ⚠️ **反推名仍是兜底**，别整段删掉（剩下那些要靠它）。
//    （为什么走 `Loc.BulkOr` 而不是手写进 `Loc.cs` 那张表：键是 368 个 GUID，见 `Loc.BulkOr` 的 doc。）
//
// ⚠️ **`Avatar_WF_*` 两张是占位图不是可选头像**（`Warpforge Empty` / `Warpforge Splash`）⇒ 生成器已排除。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>称号 / 头像的清单（从 `Resources/profile_cosmetics.json` 读，只读一次）。</summary>
    public static class ProfileData
    {
        /// <summary>`Assets/CardPresentation/Resources/profile_cosmetics.json`</summary>
        public const string ResourcePath = "profile_cosmetics";

        [Serializable]
        public class FileDto
        {
            public CosmeticDto[] avatars;
            public CosmeticDto[] titles;
            public AchievementDto[] achievements;
            public string _note;
        }

        /// <summary>一条成就。字段名 = 生成器 `工具/gen_profile_cosmetics.py` 的输出（脚本里逐条标了出处）。</summary>
        [Serializable]
        public class AchievementDto
        {
            public string id;   // `eventId`（形如 `Ach_1`）
            public string n;    // 显示名 —— **资产里的真字符串**（`ACH1 Slay the Warlord` → `Slay the Warlord`）
            public string c;    // `challenge` 的 `id`（如 `Damage To Warlord`）—— **也是资产里的真字符串**
            public int t;       // `achievementType`（位标志：1 Battle / 2 Collection / 4 Victories / 8 Account）
            public int[] v;     // 各档阈值（`rewards[].targetValue`）
            public int[] q;     // 各档奖励数量（`rewards[].rewards[0].quantity` ⇒ `Achievements/Points` 那个数）
        }

        [Serializable]
        public class CosmeticDto
        {
            public string id;      // `uniqueId`（32 位 hex）
            public string n;       // 显示名（**我们拼的**，见文件头）
            public int a;          // `cardArmy`（0 = 全阵营通用；其余是阵营 id，与 `FactionId` 同一套）
            public string art;     // 头像图名 = SO 的 `m_Name`，原样交给 `CardArt.Cosmetics`（**含空格**）
            /// <summary>🆕 **2026-10-18（W18）**：头像 SO 的 `nameTextReference` **原文**
            /// —— 原版的本地化键就是 `Avatars/&lt;它>`（`GetTranslation("Avatars/" + nameTextReference)`）。
            /// 老一批 SO 的这个值**不带** `Avatar_&lt;阵营>_` 前缀（`Aggressor Sergeant`），
            /// 新一批**带**（`Avatar_UM_Attack Bike`）⇒ 拿它当键才两批都命中。
            /// 现读：**469/469 条都有值**、命中官方表 **468/469**（只剩 `Terminator Sergeant`）。</summary>
            public string r;
        }

        /// <summary>一条装饰品。`Art` 只有头像有（称号的 `imageReference` 是空的）。</summary>
        public class Item
        {
            public string Id;
            public string Name;
            public int Army;
            public string Art;
            /// <summary>头像的 `nameTextReference` 原文（= 本地化键 `Avatars/&lt;NameRef>`）。
            /// 称号这条恒空（它们的键是 32 位 GUID，走 `Id`）。</summary>
            public string NameRef;
            public override string ToString() { return Name; }
        }

        /// <summary>一条成就（`Trophies` 页用）。**字段全是原版资产里真有的** —— 判据 → `资料/普查产出_0927/档案窗_Trophies页.md` §C3。</summary>
        public class Achievement
        {
            /// <summary>`eventId`（形如 `Ach_1`）。</summary>
            public string Id;
            /// <summary>显示名 —— **资产里的真字符串**（`ACH1 Slay the Warlord` 的 `m_Name` 去掉 `ACH&lt;n> ` 前缀）。</summary>
            public string Name;
            /// <summary>`challenge` 的 `id` 的**显示文本**（如 `Damage To Warlord`）。
            /// ⚠️ 原版那一格装的是 `LocalizedText`（I2 词条），**译文本地查不到** ⇒ 我们拿它顶，
            /// 并在界面上如实标着（`Shell/AchievementsMenu.cs` 的文件头）。
            /// 🔴 **这是给人看的**（可能被官方词条替换成中文）⇒ ⛔ 别拿它当匹配键。</summary>
            public string Challenge;
            /// <summary>🆕 **2026-10-24（E13）**：`challenge` 的 `id` **原文**（资产里的真字符串，如
            /// `Damage To Warlord`）—— 它**不过本地化**，是全表稳定的匹配键
            /// （判据 = `Resources/profile_cosmetics.json` 的 `achievements[].c`，由 `工具/gen_profile_cosmetics.py` 抽出）。
            /// <para>用途 = 成就进度写点按原版 challenge 名找成就（<see cref="FindIdByChallenge"/>）——
            /// 拿 `Id`（`Ach_N`）硬编码也能跑，但那样「这个 id 是哪条挑战」只能靠注释对，改不了资产。</para>
            /// <para>⚠️ **challenge id 不是唯一的**：`Upgrade Sautekh Cards T2` 在 Ach_27 / Ach_29 各有一条
            /// （2026-10-24 现读）。⇒ 只有「确认唯一」的那几条才走 <see cref="FindIdByChallenge"/></para></summary>
            public string ChallengeId;
            /// <summary>位标志：见 `TypeBattle` 那几个常量。</summary>
            public int Type;
            /// <summary>各档阈值（`rewards[].targetValue`）。</summary>
            public int[] Thresholds;
            /// <summary>各档奖励数量（就是 `Achievements/Points` 那个数）。</summary>
            public int[] Quantities;
            public int TierCount { get { return Thresholds != null ? Thresholds.Length : 0; } }
            public override string ToString() { return Name; }
        }

        // ---- 分类（位标志）**值照原版** `Achievement.AchievementType`（`dump.cs:124729`-`124736`）----
        public const int TypeBattle = 1, TypeCollection = 2, TypeVictories = 4, TypeAccount = 8;
        /// <summary>四个分类的**运行期顺序** —— 原版 `AchievementsMenu.Awake` 走 `Enum.GetValues`（**按值升序**）⇒ Battle→Collection→Victories→Account。</summary>
        public static readonly int[] TypeOrder = { TypeBattle, TypeCollection, TypeVictories, TypeAccount };
        /// <summary>分类名。🔴 **2026-10-14（`A1803`）改成先取官方词条**
        /// （`Achievements/Types/{枚举名}`，官方有 `Battle`/`Collection`/`Victories`/`Account` 四条 + `Secret`），
        /// 取不到才回枚举名本身（改前的行为）。
        /// <para>⚠️ 改前的注写「原版真文案是 I2 词条 `Achievements/Types/{枚举名}`，译文在远端查不到」——
        /// 那个「查不到」**作废**（官方 zh-CN 表已到手），今天是**真取得到**。⛔ 预制体里那 4 个 `'Secret'` 仍别用（占位）。</para></summary>
        public static string TypeName(int t)
        {
            string name;
            switch (t)
            {
                case TypeBattle: name = "Battle"; break;
                case TypeCollection: name = "Collection"; break;
                case TypeVictories: name = "Victories"; break;
                case TypeAccount: name = "Account"; break;
                default: return "?";
            }
            return Loc.BulkOr("Achievements/Types/" + name, name);
        }
        /// <summary>按分类筛（`0` = 不筛）。</summary>
        public static List<Achievement> OfType(int type)
        {
            var all = Achievements;
            if (type == 0) return all;
            var r = new List<Achievement>();
            for (int i = 0; i < all.Count; i++) if (all[i].Type == type) r.Add(all[i]);
            return r;
        }

        static List<Item> _avatars, _titles;
        static List<Achievement> _ach;        static bool _loaded, _failed;
        static string _nameOverride;
        /// <summary>🆕 2026-10-18（W18）：官方表里**没有中文名**的头像条数 / 前 10 个例子
        /// （`Fill` 里记账，`EnsureLoad` 末尾**汇总出声一次** —— 逐条 warning 会刷 235 行）。</summary>
        static int _nameFallback;
        static readonly List<string> _nameFallbackSample = new List<string>();

        /// <summary>🔴 **玩家显示名 —— 全工程唯一一份**（档案窗的 `Player Name` 与联机层握手报的名字读的都是它）。
        /// · **原版这个名字来自服务器**（`PlayerInfo` 的玩家名），本地没有 ⇒ **默认取机器名**，
        ///   这是**我们的选择**（2026-09-26 联机那次定的口径：「我们没有玩家名那套数据源，用机器名当显示名，
        ///   并在界面/日志里说明」）；
        /// · 档案窗的 `ChooseNameWindow` 改名写的就是这里 —— **只在本次会话里有效**（我们**没有存档**，
        ///   原版那一步会上传服务器并扣改名费）；
        /// · **别再在别处写第二份**（`NetSession.PlayerName()` 原来自己取机器名，2026-09-27 已收口到这里）。</summary>
        public static string PlayerName
        {
            get { EnsureSave(); return string.IsNullOrEmpty(_nameOverride) ? DefaultPlayerName : _nameOverride; }
            set { _nameOverride = value; SaveIfAuto(); }
        }

        /// <summary>🔴 **默认显示名 —— 用户 2026-09-28 拍板：「暂时显示『玩家123』之类的」**。
        /// 原来这里是**机器名**（2026-09-26 联机那次定的，理由是「我们没有玩家名那套数据源」）——
        /// 现在改成这个固定占位名（界面上一眼看得出是占位，不会把 `DESKTOP-XXXX` 当成昵称）。
        /// ⚠️ **联机时两边若都没改过名就会同名** —— 玩家档案窗的改名窗可以改（`ProfileData.PlayerName` setter）。
        /// ⚠️ 原版这个名字来自服务器，本地没有 ⇒ **这是我们的选择**。
        /// <para>🆕 **2026-10-18（波 1b · `A1047` 裁定「翻」· 表 B2）**：字面量改走语言表
        /// （键 `MainMenu/Profile/DefaultPlayerName` —— ZH 逐字 = 原来那句 `玩家123`、EN `Player123`）。
        /// 🔴 **`const` 必须改成【属性】**：`Loc.T` **不是编译期常量**（`const` 编不过）；
        /// ⚠️ **也不能用 `static readonly`** —— 那在**类型初始化那一刻**就定死（换语言后**新弹的窗会停旧值**）；
        /// 属性每次都现取 ⇒ 跟着 `Loc.Current` 走。全仓消费点只有 `PlayerName` 那个 getter 一处。</para></summary>
        public static string DefaultPlayerName
        {
            get { return Loc.T("MainMenu/Profile/DefaultPlayerName"); }
        }

        /// <summary>469 张可选头像（已按 阵营 → 名字 排好序）。</summary>
        public static List<Item> Avatars { get { EnsureLoad(); return _avatars; } }
        /// <summary>462 个称号（同上排序）。</summary>
        public static List<Item> Titles { get { EnsureLoad(); return _titles; } }

        // ---- 🔴 **当前选中的头像 —— 全工程唯一一份**（2026-09-27 收口）----
        //
        // 【为什么收到这儿】原来这个选择只存在 `AvatarTab.Selected`（**那个页签实例上的字段**）——
        //   ⇒ 关掉档案窗就没了，而**主菜单顶栏那块头像根本读不到它**（顶栏比档案窗先建）。
        //   与 `PlayerName` 同一个路子：**两处各存一份 = 迟早不一致**。
        //
        // ⚠️ **原版这个选择来自服务器**（`PlayerAvatarDataManager` 的存档），我们**没有存档**
        //   ⇒ 只在本次会话里有效（重开游戏回默认）。这一点与 `PlayerName` 完全同性质。
        //
        // ⚠️ **默认「第 0 张」是我们挑的**：原版没选过时回落到
        //   `PlayerAvatarDataManager.get_DefaultAvatarItem()`（一个**真·默认头像**，靠某字段等于一个常量来挑），
        //   而那个常量字符串在 `.rdata` 里**没解出来** ⇒ 本地复刻不了那一条。
        //   列表是按「阵营 → 名字」排好序的 ⇒ 第 0 张 = 第一个阵营的第一张。**别当成原版行为。**

        /// <summary>当前选中头像的**下标**（档案窗 `Avatar` 页写、顶栏/`Profile` 页/`Ranked` 页读）。
        ///
        /// <para>🔴 **2026-10-19（`A1519`）就地硬化**：这里原来是一个 **`public static` 可变字段**
        /// `public static int AvatarIndex = 0;` —— 任何自检都能改它，而改了不还原就会**漏到后面每一节**
        /// （本类是全工程唯一一份选中态：`TopBar` / `BattleDriver` / `DeckRuntime` 全读它）。
        /// 现在收成**属性 + 私有后备字段**：写口只剩这一个，将来要加审计 / 夹取只改这一处。</para>
        ///
        /// <para>🔴 **「谁改谁还原」—— 已知写点逐个列在这里**（新加写点请补进来，铁律 6：清单只留一处）：
        /// · **生产**：`Shell/AvatarTab.cs` 的 `Selected` setter（玩家在档案窗真选头像）——
        ///   这是**正常状态变更**，⛔ 别把它当成「漏状态」去治。
        /// · **自检**：`Editor/MainMenuScene.cs`（顶栏立绘跟着换那一格）、
        ///   `Editor/BattleScene.cs`（`A1502②` 立绘跟随 + 同 seed 引擎输出）。
        ///   ⇒ 自检一律用 <see cref="SetAvatarIndexForTest"/>(返回原值) + <see cref="RestoreAvatarIndexForTest"/>，
        ///   ⛔ 别直接 `AvatarIndex = x`（那样「改完漏没漏」只能靠人记，正是 `A1519` 的病灶）。</para>
        ///
        /// <para>⚠️ 为什么 setter 还留 **public**（⛔ 别改成 `private` / `internal`）：生产侧 `Shell/AvatarTab.cs`
        /// 与 `Editor/*` 的自检分属两个程序集，而它们**都要写**；C# 做不到「这个 setter 只给自检可见」。
        /// ⇒ 硬化落在「**唯一写口 + 成对的测试 API + 写点清单**」这三件上。</para>
        /// <para>⚠️ setter **不夹取**（与原来的字段逐位同语义）：越界值由下面两个读口各自 `Mathf.Clamp`。</para></summary>
        public static int AvatarIndex
        {
            get { EnsureSave(); return _avatarIndex; }
            set { _avatarIndex = value; SaveIfAuto(); }
        }
        static int _avatarIndex = 0;

        /// <summary>🆕 **2026-10-19（`A1519`）自检专用** —— 改头像下标并**返回原值**。
        /// 收尾必须把返回值交给 <see cref="RestoreAvatarIndexForTest"/>。
        /// <para>为什么要有这个口：`AvatarIndex` 是全工程唯一一份选中态，自检改了不还原会**跨节漏状态**
        /// （`A1519`）。返回值把「原值是什么」固定成**一次数据传递** ⇒ 调用方不用自己再存一份
        /// （省掉「存了但存错了时机」那一类坑）。</para></summary>
        public static int SetAvatarIndexForTest(int index)
        {
            EnsureSave();                 // 先读盘，别让「加载」在测试写完之后把值盖掉
            int prev = _avatarIndex;
            _avatarIndex = index;
            return prev;
        }

        /// <summary>🆕 **2026-10-19（`A1519`）自检专用** —— 把 <see cref="SetAvatarIndexForTest"/> 交回的原值放回去。
        /// ⚠️ 只做赋值、**不夹取**（原值本来就来自这个字段）。</summary>
        public static void RestoreAvatarIndexForTest(int saved) { EnsureSave(); _avatarIndex = saved; }

        /// <summary>当前选中头像的**图名**（喂给 `CardArt.Cosmetics`）。清单没读进来时返回 null。</summary>
        public static string AvatarArt
        {
            get
            {
                var list = Avatars;
                if (list == null || list.Count == 0) return null;
                return list[Mathf.Clamp(AvatarIndex, 0, list.Count - 1)].Art;
            }
        }

        /// <summary>当前选中头像的**显示名**。</summary>
        public static string AvatarName
        {
            get
            {
                var list = Avatars;
                if (list == null || list.Count == 0) return "";
                return list[Mathf.Clamp(AvatarIndex, 0, list.Count - 1)].Name;
            }
        }

        // ---- 🔴 **当前选中的称号 —— 全工程唯一一份**（2026-10-17 · `T20` / `A2118`）----
        //
        // 【为什么收到这儿】原来这个选择只存在 `TitleTab.Selected`（**那个页签实例上的字段**）——
        //   ⇒ 关掉档案窗就没了，而**战斗 HUD 名牌更读不到它**（`BattleDriver.BuildHudExtras` 当年
        //   只能恒 `SetTitle(null, null)`，称号那一块永远不显示）。与 `AvatarIndex` 完全同性质：
        //   **两处各存一份 = 迟早不一致**（同 `CLAUDE.md` §三 那条）。
        //
        // ⚠️ **原版这个选择来自服务器存档**（`PlayerDataManager.set_PlayerTitle` + `UploadOnlineDemoData`，
        //   见 `Shell/TitleTab.OnSelectClicked` 原来那条日志），我们**没有存档**
        //   ⇒ 只在本次会话里有效（重开游戏回到「没选」）。与 `AvatarIndex` / `PlayerName` 同一条边界。
        //
        // ⚠️ **默认 −1 = 没选**（**不是 0**）：原版没选过时 `PlayerDataManager.get_PlayerTitle` 是空的
        //   ⇒ 名牌上**整块不显示**（判据 = `PlayerProfileUIController__SetProfileTitle.c` 的
        //   `!IsNullOrEmpty`）。⛔ 别学头像那样给个默认下标 —— 那等于凭空给玩家一个他从没选过的称号。

        /// <summary>当前选中称号的**下标**（**−1 = 没选**）。档案窗 `Title` 页写；战斗 HUD 名牌读。
        /// <para>🆕 **2026-10-22（P1 · 账户界面批次）**：从裸字段改成**属性** —— 写口加一层 `SaveIfAuto()`
        /// （落盘层见本文件末尾「本地存档」那一节）。读口 `EnsureSave()` 保证先读盘再返回；
        /// ⛔ 语义不变（setter 仍**不夹取**，越界由读口 `TitleName` 挡）。</para></summary>
        public static int TitleIndex
        {
            get { EnsureSave(); return _titleIndex; }
            set { _titleIndex = value; SaveIfAuto(); }
        }
        static int _titleIndex = -1;

        /// <summary>当前选中称号的**显示名**。没选（`TitleIndex &lt; 0`）/ 越界 / 清单为空 ⇒ **`""`**
        /// （空串 = 没有称号 ⇒ HUD 那一块整块关，判据见 `BattleDriver.SetTitle`）。</summary>
        public static string TitleName
        {
            get
            {
                var list = Titles;
                if (list == null || list.Count == 0) return "";
                if (TitleIndex < 0 || TitleIndex >= list.Count) return "";
                return list[TitleIndex].Name;
            }
        }
        /// <summary>102 条成就（`Trophies` 页）。</summary>
        public static List<Achievement> Achievements { get { EnsureLoad(); return _ach; } }

        // ============================================================ 🆕 2026-10-24（E13）：成就进度写点要用的键
        //
        // 🔴 **下面是【资产真数】里的 challenge 原文**（`Resources/profile_cosmetics.json` 的
        //    `achievements[].c`，⛔ 别改这个文件）—— 每个常量后面注着落到哪一条成就、阈值是多少。
        // ⚠️ 用 `FindIdByChallenge` 而不是把 `Ach_N` 硬编码进调用点：id 是生成器抽出来的，
        //    challenge 名是**卡面/资产里的原话** ⇒ 后者可读、也能由本表的常量逐条对上。
        // ⛔ 别加「看着像」的键：challege 名重复的（`Upgrade Sautekh Cards T2` = Ach_27 / Ach_29）
        //    一律不进这张表（`FindIdByChallenge` 会返回第一个并出声）。

        /// <summary>`Damage To Warlord` ⇒ `Ach_1`（阈值 1000/10000/50000/250000/999999）——
        /// 数据源 = `Ctx.DamageToEnemyWarlord[seat]`（`RuleCore.ApplyDamage` 督军那一格）。</summary>
        public const string ChallengeDamageToWarlord = "Damage To Warlord";
        /// <summary>`Collect Skulls` ⇒ `Ach_19`（阈值 50/500/2000/10000/50000）——
        /// 数据源 = 结算那一刻的骷髅数（`BattleDriver._foeSkullCount`，与 HUD 那个 `x N` 同源）。</summary>
        public const string ChallengeCollectSkulls = "Collect Skulls";
        /// <summary>`Gain Faith` ⇒ `Ach_63`（阈值 10/100/400/1000/2000）——
        /// 数据源 = `PlayerState.Faith`（引擎里**只增不减**的信仰，见那个字段的注释）。</summary>
        public const string ChallengeGainFaith = "Gain Faith";
        /// <summary>`Play Ranked Multiplayer Matches` ⇒ `Ach_37`（阈值 25/100/1000/5000/10000）。</summary>
        public const string ChallengeRankedPlayed = "Play Ranked Multiplayer Matches";
        /// <summary>`Win Ranked Multiplayer Matches` ⇒ `Ach_38`（阈值 10/50/500/5000/10000）。</summary>
        public const string ChallengeRankedWon = "Win Ranked Multiplayer Matches";

        /// <summary>🆕 **2026-10-24（E13）**：按**原版 challenge 原文**找成就 id（= 写点用的键）。
        /// <para>返回 `null` = 资产里没有这一条（**调用方必须如实出声**，⛔ 别静默当 0 写）。</para>
        /// <para>⚠️ challenge 原文**不保证唯一**（`Upgrade Sautekh Cards T2` 两条）⇒ 命中多条时返回第一条
        /// 并 `LogWarning`。要用它，先确认那条 challenge 在 102 条里唯一（本类上面那几个常量都是核过的）。</para></summary>
        public static string FindIdByChallenge(string challengeId)
        {
            if (string.IsNullOrEmpty(challengeId)) return null;
            var all = Achievements;
            string hit = null; int n = 0;
            for (int i = 0; i < all.Count; i++)
                if (string.Equals(all[i].ChallengeId, challengeId, StringComparison.Ordinal))
                { if (hit == null) hit = all[i].Id; n++; }
            if (n > 1)
                Debug.LogWarning("[Profile] challenge「" + challengeId + "」在成就表里出现了 " + n
                               + " 次（第一条 = " + hit + "）⇒ 按第一条写；要用这条请先确认它唯一");
            return hit;
        }

        /// <summary>给自检用的：清单是不是真的读进来了（没读进来时会**出声一次**，不静默）。</summary>
        public static bool Loaded { get { EnsureLoad(); return _loaded; } }

        static void EnsureLoad()
        {
            if (_loaded || _failed) return;
            _avatars = new List<Item>();
            _titles = new List<Item>();
            _ach = new List<Achievement>();
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                _failed = true;
                // 🔴 **不许静默失败**：缺了它，Title/Avatar/Trophies 三页会变成空列表，而空列表**看着像「没数据」**
                Debug.LogError("[Profile] 读不到 `Resources/" + ResourcePath + ".json` ⇒ "
                             + "称号/头像/成就三页会是**空列表**。生成器：`工具/gen_profile_cosmetics.py`");
                return;
            }
            FileDto d = null;
            try { d = JsonUtility.FromJson<FileDto>(ta.text); }
            catch (Exception e) { Debug.LogError("[Profile] `" + ResourcePath + "` 解析失败：" + e.Message); }
            if (d == null) { _failed = true; return; }
            Fill(d.avatars, _avatars, "Avatars/", true);
            Fill(d.titles, _titles, "Titles/", false);
            // 🆕 2026-10-18（W18）：**汇总出声一次**（不逐条刷屏）。⛔ 不静默 ——
            //    玩家看到的英文条目必须有出处可查（是「官方表没有」而不是「我们没接」）。
            if (_nameFallback > 0)
                Debug.LogWarning("[Profile] 头像名：**官方中文名缺 " + _nameFallback + " 条**（共 "
                               + _avatars.Count + " 条）⇒ 这些**保留英文原文**（⛔ 我们不自译）。前 "
                               + _nameFallbackSample.Count + " 条："
                               + string.Join(" / ", _nameFallbackSample.ToArray())
                               + "。搜过：`Avatars/<nameTextReference>` → `Avatars/<显示名>` → `Avatars/<图名>`"
                               + "（表 = `Resources/official_names.json`，由生成器 `工具/_loc_official/gen_official_names.py` 产出）");
            if (d.achievements != null)
                for (int i = 0; i < d.achievements.Length; i++)
                {
                    var a = d.achievements[i];
                    // 🔴 2026-10-14（`A1803`）：名与描述都先取官方词条（键拼法见文件头），取不到回资产里的真字符串。
                    //   官方 **名** = `Achievements/Title/<Ach_N>` · **描述/挑战** = `Achievements/<Ach_N>`（现读各命中 95/102）。
                    //   ⚠️ 描述那列里有几条**带 `{0}`**（如 `Log in {0} days`）—— 我们的进度恒 0、没有那个数值可填
                    //      ⇒ 带占位符的**退回资产里的 challenge id**（⛔ 不许把 `{0}` 原样印上屏）。
                    string desc = Loc.BulkOr("Achievements/" + a.id, a.c);
                    if (!string.IsNullOrEmpty(desc) && desc.IndexOf("{0}", System.StringComparison.Ordinal) >= 0)
                        desc = a.c;
                    _ach.Add(new Achievement { Id = a.id,
                                               Name = Loc.BulkOr("Achievements/Title/" + a.id, a.n),
                                               Challenge = desc, ChallengeId = a.c, Type = a.t,
                                               Thresholds = a.v, Quantities = a.q });
                }
            _loaded = true;
        }

        /// <summary>把 DTO 灌成清单。`prefix` = 官方词条键的前缀；`keyFromName` = true 时是**头像那一族**
        /// （键 = `nameTextReference` 原文，见下面 🆕 那段），false 时是**称号那一族**（键 = 32 位 GUID，与 `id` 同源）。
        ///
        /// <para>🆕 **2026-10-18（W18）头像的键改按【原文】取**：原版是
        /// `GetTranslation("Avatars/" + nameTextReference)`；老一批 SO 的原文不带 `Avatar_<阵营>_`
        /// （`Aggressor Sergeant`）、新一批**带**（`Avatar_UM_Attack Bike`）。
        /// 以前我们拿「剥掉前缀的显示名」当键 ⇒ 新一批 **235 条全部落空**（界面上就是中英混排）。
        /// 现在三级兜底：**`r`（原文）→ `n`（显示名）→ `art`（图名）**，每级都先 `Loc.HasBulk` 探一下
        /// （⛔ 不用「先 BulkOr 再看返回值」—— 那会给每条缺键都打一条 warning，235 条刷屏）。
        /// 三级都没有 ⇒ **保留英文原文**（⛔ 不自己译），并在 `EnsureLoad` 末尾**汇总出声一次**。</para></summary>
        static void Fill(CosmeticDto[] src, List<Item> dst, string prefix, bool keyFromName)
        {
            if (src == null) return;
            for (int i = 0; i < src.Length; i++)
            {
                string key, name;
                if (keyFromName)
                {
                    key = prefix + (string.IsNullOrEmpty(src[i].r) ? src[i].n : src[i].r);
                    if (!Loc.HasBulk(key))
                    {
                        string k2 = prefix + src[i].n;
                        string k3 = string.IsNullOrEmpty(src[i].art) ? null : prefix + src[i].art;
                        if (Loc.HasBulk(k2)) key = k2;
                        else if (k3 != null && Loc.HasBulk(k3)) key = k3;
                    }
                    bool hasZh = Loc.HasBulk(key);
                    if (!hasZh)
                    {
                        // 官方表三级都没命中 ⇒ 保留英文原文（⛔ 不自己译）；这里只记账，汇总在 `EnsureLoad` 末尾出声一次
                        _nameFallback++;
                        if (_nameFallbackSample.Count < 10) _nameFallbackSample.Add(src[i].n);
                    }
                    name = hasZh ? Loc.BulkOr(key, src[i].n) : src[i].n;
                }
                else
                {
                    key = prefix + src[i].id;
                    name = Loc.BulkOr(key, src[i].n);
                }
                dst.Add(new Item { Id = src[i].id, Name = name, Army = src[i].a,
                                   Art = src[i].art, NameRef = src[i].r });
            }
        }

        /// <summary>按阵营筛（`0` = 全阵营通用，**任何阵营都算它**；`army &lt;= 0` = 不筛）。</summary>
        public static List<Item> OfArmy(List<Item> src, int army)
        {
            if (army <= 0) return src;
            var r = new List<Item>();
            for (int i = 0; i < src.Count; i++)
                if (src[i].Army == army || src[i].Army == 0) r.Add(src[i]);
            return r;
        }

        // ============================================================ 本地存档（唯一一份）
        //
        // 🔴 **这一层是【我们自建】的等价物，不是原版行为**。原版这五样全在服务器（PlayFab）：
        //    玩家名 · 已装备头像 · 已装备头衔 · 头像边框 · 成就进度/积分
        //    （`PlayerDataManager.set_PlayerTitle` · `PlayerAvatarDataManager` 存档 ·
        //     `AchievementsManager.achievementsSave` + `UploadPooledProgress`）。
        //    关服 ⇒ 本地没有来源 ⇒ 这里只做到「**关掉游戏再打开还在**」（跨设备同步做不到）。
        //
        // · 路径照既有先例（`Battle/ReplayStore` = `persistentDataPath/WarpforgeReplays/` ·
        //   `Shell/DailyData` = `persistentDataPath/WarpforgeDaily/daily.json`）：
        //   **`persistentDataPath/WarpforgeProfile/profile.json`**。
        // · 形状 = 一个 JSON（`JsonUtility`），字段见 <see cref="SaveDto"/>。
        // · 🔴 **批处理（`-batchmode`）默认不读也不写真存档**：自检会把 `TitleIndex` 写成 7 / 9999、
        //   把 `AvatarIndex` 拨来拨去（`SetAvatarIndexForTest` 那一族）—— 若照写，测试状态会污染玩家存档、
        //   下次跑又读到脏值（自检不幂等）。自检验落盘走 `SaveNowForTest()` / `ReloadSaveForTest()`。
        // · ⚠️ **迁移**：`ver` 只是记号；读到不认识的版本 ⇒ 缺的字段保持默认值（`JsonUtility` 的语义），
        //   ⛔ 不做破坏性迁移（这份文件里没有不可重建的数据）。

        /// <summary>存档文件内容（`JsonUtility` 只认 public 字段）。</summary>
        [Serializable]
        public class SaveDto
        {
            /// <summary>格式版本（`1` = 2026-10-22 首版）。</summary>
            public int ver = 1;
            /// <summary>`PlayerName` 的**覆盖值**（空串 = 用默认占位名）。</summary>
            public string playerName = "";
            public int avatarIndex = 0;
            /// <summary>−1 = 没选（与 `TitleIndex` 同义）。</summary>
            public int titleIndex = -1;
            public bool avatarBorderOn = true;
            /// <summary>成就进度：与 <see cref="achValues"/> 同下标配对（`Ach_N`）。</summary>
            public string[] achIds = new string[0];
            public int[] achValues = new int[0];
        }

        public const string SaveDirName = "WarpforgeProfile";
        public const string SaveFileName = "profile.json";

        /// <summary>🆕 **自检专用**：把存档路径改到临时文件（`null` = 正常路径）。
        /// 🔴 为什么要有它：自检会改 `AvatarIndex`/`TitleIndex` 再验「落盘后读回还是那一个」——
        /// 若直接写 `persistentDataPath`，**测试状态会污染玩家真存档**，下次启动就带着脏值。
        /// 用完必须置回 `null` 并删掉临时文件（自检那边负责）。</summary>
        public static string SavePathOverrideForTest;

        /// <summary>存档完整路径（`persistentDataPath/WarpforgeProfile/profile.json`；
        /// 自检可用 <see cref="SavePathOverrideForTest"/> 改道）。</summary>
        public static string SavePath
        {
            get
            {
                if (!string.IsNullOrEmpty(SavePathOverrideForTest)) return SavePathOverrideForTest;
                return System.IO.Path.Combine(System.IO.Path.Combine(Application.persistentDataPath, SaveDirName), SaveFileName);
            }
        }

        static SaveDto _save;
        static Dictionary<string, int> _achProgress;
        static bool _saveTried;

        /// <summary>头像边框显隐（`Avatar Tab/Toggle borde`）。**读写都落盘**（原版在 `PlayerAvatarDataManager`）。</summary>
        public static bool AvatarBorderOn
        {
            get { EnsureSave(); return _save.avatarBorderOn; }
            set { EnsureSave(); _save.avatarBorderOn = value; SaveIfAuto(); }
        }

        /// <summary>第一次用存档时读盘（批处理下跳过 —— 见本节头）。</summary>
        static void EnsureSave()
        {
            if (_saveTried) return;
            _saveTried = true;
            _save = new SaveDto();
            _achProgress = new Dictionary<string, int>();
            if (Application.isBatchMode) return;      // 自检不碰真存档
            LoadFromDisk();
        }

        static void LoadFromDisk()
        {
            try
            {
                if (!System.IO.File.Exists(SavePath)) return;
                var d = JsonUtility.FromJson<SaveDto>(System.IO.File.ReadAllText(SavePath));
                if (d == null) return;
                _save = d;
                _nameOverride = string.IsNullOrEmpty(d.playerName) ? null : d.playerName;
                _avatarIndex = d.avatarIndex;
                _titleIndex = d.titleIndex;
                if (d.achIds != null && d.achValues != null)
                    for (int i = 0; i < d.achIds.Length && i < d.achValues.Length; i++)
                        if (!string.IsNullOrEmpty(d.achIds[i])) _achProgress[d.achIds[i]] = d.achValues[i];
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Profile] 读存档失败（" + SavePath + "）：" + e.Message + " ⇒ 用默认值");
            }
        }

        /// <summary>写盘（正式入口；内部会先做「越界值不落盘」的净化）。批处理下由 <see cref="SaveIfAuto"/> 挡掉。</summary>
        public static void SaveNow()
        {
            EnsureSave();
            // 净化：自检会把 TitleIndex 写成 9999 之类 —— 那种值**不写进玩家存档**
            if (_titles != null && _titles.Count > 0)
            {
                if (_titleIndex < -1 || _titleIndex >= _titles.Count) _save.titleIndex = -1;
                else _save.titleIndex = _titleIndex;
                if (_avatarIndex < 0 || _avatarIndex >= _avatars.Count) _save.avatarIndex = 0;
                else _save.avatarIndex = _avatarIndex;
            }
            _save.playerName = _nameOverride ?? "";
            var ids = new List<string>(); var vals = new List<int>();
            foreach (var kv in _achProgress) { ids.Add(kv.Key); vals.Add(kv.Value); }
            _save.achIds = ids.ToArray(); _save.achValues = vals.ToArray();
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SavePath));
                System.IO.File.WriteAllText(SavePath, JsonUtility.ToJson(_save, true));
            }
            catch (Exception e)
            {
                // 不许静默：写不进去要说出来（原版这一步是上传服务器）
                Debug.LogWarning("[Profile] 存档写盘失败（" + SavePath + "）：" + e.Message);
            }
        }

        static void SaveIfAuto() { if (!Application.isBatchMode) SaveNow(); }

        /// <summary>🆕 自检专用：强制写盘（批处理下也能验落盘）。</summary>
        public static void SaveNowForTest() { EnsureSave(); SaveNow(); }

        /// <summary>🆕 自检专用：强制从盘上重读（批处理下也能验「落盘后读回还是那一个」）。
        /// ⚠️ 只改「已装备」三样与成就进度；**不**重读清单（清单是只读资产）。</summary>
        public static void ReloadSaveForTest()
        {
            EnsureSave();
            _achProgress = new Dictionary<string, int>();
            LoadFromDisk();
        }

        // ---- 成就进度（原版 `Achievement.CurrentValue` / `CurrentPoints`，服务器；这里 = 本机存档）----

        /// <summary>一条成就的当前进度值（没记录 ⇒ 0）。</summary>
        public static int AchievementValue(string id)
        {
            EnsureSave();
            int v;
            return !string.IsNullOrEmpty(id) && _achProgress.TryGetValue(id, out v) ? v : 0;
        }

        /// <summary>写一条成就的进度值（负数按 0 收）。原版这条走 `AchievementsManager` + 批量上传。</summary>
        public static void SetAchievementValue(string id, int value)
        {
            if (string.IsNullOrEmpty(id)) return;
            EnsureSave();
            _achProgress[id] = Mathf.Max(0, value);
            SaveIfAuto();
        }

        /// <summary>🆕 **2026-10-24（E13）**：**累加**一条成就的进度（战果写点用）。
        /// <para>🆕 为什么要它：原版成就是**跨对局累计**的（`Achievement.CurrentValue` 由 challenge 一路加上去，
        /// 判据 = `Achievement__OnChallengeUpdate` + `AchievementsManager__UpdateSave`；
        /// 阈值表见资产真数）⇒ 每打完一局应当 `+= 本局战果`，⛔ 不是每局 `= 本局战果`（那会**把历史清掉**）。</para>
        /// <para>`delta &lt;= 0` ⇒ 不写（也不落盘）——「这一局这项是 0」不该产生一次写盘。
        /// ⚠️ 这里**不做封顶**（原版也不封）：超过最高阈值之后只剩 `TargetTier == 总档数` 这一档，
        /// 界面按「已达档数 + 1、封顶总档数」展示（<see cref="AchievementTierOf"/>）。</para></summary>
        public static void AddAchievementValue(string id, int delta)
        {
            if (string.IsNullOrEmpty(id) || delta <= 0) return;
            EnsureSave();
            int cur;
            if (!_achProgress.TryGetValue(id, out cur)) cur = 0;
            _achProgress[id] = Mathf.Max(0, cur) + delta;
            SaveIfAuto();
        }

        /// <summary>一条成就**当前在第几档**（1 起；已达满档则 = 总档数）。照原版 `Achievement.TargetTier`：
        /// 档 n 的判定是「进度 ≥ 第 n 档阈值」，展示用 `{已达档数 + 1}`，封顶总档数。</summary>
        public static int AchievementTierOf(Achievement a)
        {
            int total = a != null && a.Thresholds != null ? a.Thresholds.Length : 0;
            if (total <= 0) return 1;
            int v = AchievementValue(a.Id), reached = 0;
            for (int i = 0; i < total; i++) if (v >= a.Thresholds[i]) reached++;
            return Mathf.Clamp(reached + 1, 1, total);
        }

        /// <summary>那一档的目标阈值（`0` = 没有数据）。</summary>
        public static int AchievementTargetOf(Achievement a, int tier)
        {
            if (a == null || a.Thresholds == null || a.Thresholds.Length == 0) return 0;
            return a.Thresholds[Mathf.Clamp(tier - 1, 0, a.Thresholds.Length - 1)];
        }

        /// <summary>那一档的积分奖励（`Achievements/Points` 的那个数；`0` = 没有数据）。</summary>
        public static int AchievementRewardOf(Achievement a, int tier)
        {
            if (a == null || a.Quantities == null || a.Quantities.Length == 0) return 0;
            return a.Quantities[Mathf.Clamp(tier - 1, 0, a.Quantities.Length - 1)];
        }

        /// <summary>一条成就**已拿到的积分** = 所有「已达档」的 `quantity` 之和
        /// （原版 `Achievement.CurrentPoints`；本地按存档推）。</summary>
        public static int AchievementPointsOf(Achievement a)
        {
            if (a == null || a.Thresholds == null || a.Quantities == null) return 0;
            int v = AchievementValue(a.Id), sum = 0;
            for (int i = 0; i < a.Thresholds.Length && i < a.Quantities.Length; i++)
                if (v >= a.Thresholds[i]) sum += a.Quantities[i];
            return sum;
        }

        /// <summary>全量成就积分之和（`Trophies` 页右上那个计数器；不受分类筛选影响）。</summary>
        public static int AchievementPointsTotal()
        {
            var all = Achievements;
            int sum = 0;
            for (int i = 0; i < all.Count; i++) sum += AchievementPointsOf(all[i]);
            return sum;
        }

        /// <summary>当前这一档的进度比例（0..1）。上一档阈值 = 起点，本档阈值 = 终点。</summary>
        public static float AchievementProgress01(Achievement a, int tier)
        {
            if (a == null || a.Thresholds == null || a.Thresholds.Length == 0) return 0f;
            int idx = Mathf.Clamp(tier - 1, 0, a.Thresholds.Length - 1);
            int target = a.Thresholds[idx];
            int from = idx > 0 ? a.Thresholds[idx - 1] : 0;
            if (target <= from) return 1f;
            int v = AchievementValue(a.Id);
            return Mathf.Clamp01((float)(v - from) / (target - from));
        }
    }
}
