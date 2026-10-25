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
using RuleEngine;              // 🆕 2026-10-25（`X2`）：`AchievementTally` / `GameMode` / `TargetCriteriaEval`
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
            /// <summary>🆕 **2026-10-25（`X2`）**：`challenge` 的 **`type.class`** ——
            /// **原版触发源的类名**（`DamageDeltToEnemyUnit` / `GamesPlayed` / `UpgradeCard` / `TriggerAbility` …）。
            /// 判据 = 成就定义 SO（`bundle_staticgeneralassets_assets_all/MonoBehaviour/ACH<n> <名>.json`
            /// 的 `challenge` 是 `SerializeReference`，`references.RefIds[].type.class` 就是它）。
            /// 🔴 有了它，27 条升级 / 30 条对局 / 17 条触发才**分得开**（它们的 challenge 原文有重名，见 §F5）。</summary>
            public string k;
            /// <summary>🆕 2026-10-25（`X2`）：过滤位 —— `data` **去掉 `id` / `rewards`** 之后的**原样 JSON 字符串**
            /// （`JsonUtility` 吃不了 `Dictionary`，所以原样存、消费侧按 <see cref="Filters"/> 这个固定 DTO 再解一次）。</summary>
            public string f;
            /// <summary>🆕 2026-10-25（`X2`）：`allowOnTutorials`（0/1）。现读：**42 条 1 / 60 条 0**。</summary>
            public int tut;
            /// <summary>🆕 2026-10-25（`X2`）：`allowOnDuels`（0/1）。现读：**102 条全 0**（决斗一条都不计）。</summary>
            public int duel;
        }

        /// <summary>🆕 **2026-10-25（`X2`）**：成就 `challenge.data` 的**过滤位**（只列**消费端真的会读**的那些格；
        /// 其余格子 `JsonUtility` 直接忽略）。字段名 = 原版 JSON 的键名（⛔ 别改成驼峰/中文）。
        /// <para>判据 = 成就定义 SO 逐条现读；每一项在消费点（<see cref="ApplyAchievementTally"/>）都注了出处。</para>
        /// <para>⚠️ **0 与 1 是两个意思**（`anyArmy=1` = 不过滤阵营、`anyArmy=0` = 按 `army` 筛），
        /// 所以生成器把 0 也一起写出去了 —— ⛔ 别把 0 当「字段没写」。</para></summary>
        [Serializable]
        public class Filters
        {
            public int army;                  // 阵营 id（`CardArmy`：10 UM / 20 Goff / 40 Sautekh …）
            public int anyArmy;               // 1 = 不过滤阵营
            public int tier;                  // `UpgradeCard`：目标档（**0 基**：1 = Tier2 · 2 = Tier3）
            public int anyTier;               // 1 = 不过滤档（`Ach_23` 的「任意档」）
            public int onlyRanked;            // `GamesPlayed`：1 = 排位
            public int onlyAccountWins;       // 1 = 只算胜场
            public int anyGameMode;           // 1 = 不过滤模式（现读 0 条是 1）
            public GameModeRef[] gameModes;   // 运行期类型（`IRankedEvent` / `IDraftEvent`）—— **原版判的就是它**
            public int attackType;            // `DamageDeltToEnemyUnit`：`AttackTypes`（1 近战 / 2 远程 / 3 计谋）
            public int anyAttackType;
            public int damageType;            // `DamageType`（11 = `Stomp`）
            public int anyDamageType;
            public int onlyDamageByShuriken;      // `Ach_7`
            public int onlyDamageByMarkerlight;   // `Ach_53`
            public int triggerAbility;        // `TriggerAbility`：`AbilityTrigger` 数值（145 Codex / 300 Mob …）
            public int anyTrait;
            public int[] withTrait;           // `DefinedTrait` 数值表（5 = ephemeral · 610 = pack）
            public int anyKeyword;
            public int keyword;               // `Keyword` 枚举（35 = Beast）
            public int anySpellType;
            public int spellType;             // 260 = `CombatElixir`
            public int anyCardType;           // 0 = 要看卡型（`Ach_89`）
            public int onlyCountCompanions;   // `Ach_54`
            public int[] playModeBlocklist, playModeAllowlist;   // 102 条**全空** ⇒ 全模式放行
        }

        /// <summary>🆕 2026-10-25（`X2`）：`gameModes[]` 里那一格 —— 原版 `GamesPlayed` 只读
        /// `TypeReference` 的**运行时类型名**（`GamesPlayed__OnTrigger.c` 那段 `Type.IsAssignableFrom`）。</summary>
        [Serializable]
        public class GameModeRef { public string _typeNameAndAssembly; }


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
            /// <summary>🆕 **2026-10-25（`X2`）**：原版触发源的**类名**（`DamageDeltToEnemyUnit` /
            /// `GamesPlayed` / `UpgradeCard` / `TriggerAbility` / `ConstantChallenge` …）。
            /// 结算时按它分流（判据 = 成就定义 SO 的 `challenge.type.class`）。</summary>
            public string Class;
            /// <summary>🆕 2026-10-25（`X2`）：过滤位的**原样 JSON**（解析结果缓存见 <see cref="FiltersOf"/>）。</summary>
            public string FiltersJson;
            /// <summary>🆕 2026-10-25（`X2`）：`allowOnTutorials` —— **教程局这一条计不计**
            /// （判据 = `MissionChallenge__ValidateMatchConditions.c`：`IsTutorialMatch` ⇒ 直接回它）。</summary>
            public bool TutorialOk;
            /// <summary>🆕 2026-10-25（`X2`）：`allowOnDuels` —— **决斗局这一条计不计**（现读 102 条全 false）。</summary>
            public bool DuelOk;
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

        // ============================================================================
        //  🆕 2026-10-25（执行代理 `X2`）：成就的「原版触发源」那一半
        //  —— 过滤位 / 教程·决斗·回放闸 / 按 `Ach_N` 写入 / 结算那一句
        //
        //  判据与施工单 = `资料/待办判据_成就计数器_1025b.md`（§B-0 数据层 · §B-1 存哪 ·
        //  §B-4 结算统一成一处 · §二① 教程闸）。
        //  🔴 **为什么要有这一节**：97 条成就的触发源在引擎里**没有计数器**（近战/远程伤害拆分、
        //     关键词触发次数、升级卡、对局胜负…），E13 只接了 5 条。用户 2026-10-25 拍板**要补**。
        //  🔴 **两条硬口径**：
        //     ① **一律按 `Ach_N` 写**（⛔ 不再走 `FindIdByChallenge`）—— challenge 原文**有重名**
        //        （`Upgrade Sautekh Cards T2` = `Ach_27`/`Ach_29` 等三组，判据 §F5）⇒ 按原文找会串台；
        //     ② **闸在写之前**（回放一律不计 / 教程按 `TutorialOk` / 决斗按 `DuelOk`）——
        //        原来完全没有这道闸（判据 §F1：`Ach_63` 在教程局会**误涨**）。
        // ============================================================================

        /// <summary>`Ach_17`（`ConstantChallenge`：累计登录天数）—— 由 `DailyData.RecordLogin` 写。</summary>
        public const string IdAchLoginTotalDays = "Ach_17";
        /// <summary>`Ach_18`（`ConstantChallenge`：连续登录天数）—— 同上。</summary>
        public const string IdAchLoginStreakDays = "Ach_18";
        /// <summary>`Ach_15`（`ContainersOpen`：开卡包）—— 由 `Shell/BoosterPackOpenWindow.Open` 写。</summary>
        public const string IdAchContainersOpen = "Ach_15";
        /// <summary>`Ach_20`（`DraftWins`：一次轮抽 run 内的胜场）—— 由 `Shell/DraftModeWindow` 收尾那一拍写。</summary>
        public const string IdAchDraftWins = "Ach_20";
        /// <summary>`Ach_24`（`RewardCollected`：收集不同的牌）—— 由 <see cref="RecordCardPoolCollectedOnce"/> 写。</summary>
        public const string IdAchCardPool = "Ach_24";
        /// <summary>`Ach_16`（`CompleteCampaign`：完成战役）—— 由
        /// `Shell/CampaignData.NotifyCampaignCompletionIfAllClaimed` 写（唯一写点，`checkAlreadyCompleted=1` 语义）。</summary>
        public const string IdAchCompleteCampaign = "Ach_16";

        static Dictionary<string, Filters> _filters;

        /// <summary>一条成就的**过滤位**（`f` 那一格解出来的固定形状）。解析一次后缓存。
        /// <para>返回 `null` = 这一份 json **没有那三格**（旧生成的 `profile_cosmetics.json`）
        /// 或解析失败 —— 调用方必须**当作「判不了」放行还是拦下要自己说清**（本类一律**不写**，
        /// 因为写错进度比不写更糟；`EnsureLoad` 会汇总出声一次）。</para></summary>
        public static Filters FiltersOf(Achievement a)
        {
            if (a == null || string.IsNullOrEmpty(a.Id)) return null;
            EnsureLoad();
            if (_filters == null) _filters = new Dictionary<string, Filters>();
            Filters f;
            if (_filters.TryGetValue(a.Id, out f)) return f;
            if (string.IsNullOrEmpty(a.FiltersJson)) { _filters[a.Id] = null; return null; }
            try { f = JsonUtility.FromJson<Filters>(a.FiltersJson); }
            catch (Exception e)
            {
                Debug.LogWarning("[Profile] 成就「" + a.Id + "」的过滤位解析失败：" + e.Message
                               + " ⇒ 这一条按「判不了」处理（不写进度）");
                f = null;
            }
            _filters[a.Id] = f;
            return f;
        }

        /// <summary>按 `Ach_N` 找那一条成就（102 条线性扫，够快）。找不到 ⇒ `null`。</summary>
        public static Achievement AchievementById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var all = Achievements;
            for (int i = 0; i < all.Count; i++)
                if (string.Equals(all[i].Id, id, StringComparison.Ordinal)) return all[i];
            return null;
        }

        /// <summary>
        /// **这一条成就在这个模式下计不计** —— 逐句照原版
        /// `MissionChallenge__ValidateMatchConditions.c`：
        /// <list type="number">
        ///   <item>`IsReplayMatch` ⇒ **false**（**回放一律不计**）；</item>
        ///   <item>matchType `== 1`（**决斗**）⇒ 返回 `allowOnDuels`（现读 102 条**全 false**）；</item>
        ///   <item>`IsTutorialMatch` ⇒ 返回 `allowOnTutorials`（现读 **42 条 true / 60 条 false**）；</item>
        ///   <item>否则走 `playModeAllowlist` / `playModeBlocklist`（102 条**两格全空** ⇒ 全放行）。</item>
        /// </list>
        /// <para>⚠️ 我们的 `GameMode.Replay` / `TutorialReplay` 也算「回放」（原版是 `IsReplayMatch` 一个布尔，
        /// 本地那两种局都不该涨成就 —— 与「回放不记账」同一条口径）。</para>
        /// </summary>
        public static bool AchievementAllowed(Achievement a, GameMode mode, bool replay)
        {
            if (a == null) return false;
            if (replay || mode == GameMode.Replay || mode == GameMode.TutorialReplay) return false;
            if (mode == GameMode.Duel) return a.DuelOk;
            if (mode == GameMode.Tutorial) return a.TutorialOk;
            var f = FiltersOf(a);
            if (f != null)
            {
                if (ContainsMode(f.playModeBlocklist, mode)) return false;
                if (f.playModeAllowlist != null && f.playModeAllowlist.Length > 0
                    && !ContainsMode(f.playModeAllowlist, mode)) return false;
            }
            return true;
        }

        static bool ContainsMode(int[] list, GameMode mode)
        {
            if (list == null) return false;
            for (int i = 0; i < list.Length; i++) if (list[i] == (int)mode) return true;
            return false;
        }

        /// <summary>**带闸**地按 `Ach_N` 累加一条成就。返回 `true` = 真写了。
        /// <para>闸不过 / delta ≤ 0 / 找不到那条 id ⇒ 不写（找不到那条会**出声**，⛔ 不静默）。
        /// 纪律同 <see cref="AddAchievementValue"/>（累加、不封顶）。</para></summary>
        public static bool GainAchievement(string id, int delta, GameMode mode, bool replay)
        {
            if (delta <= 0) return false;
            var a = AchievementById(id);
            if (a == null)
            {
                Debug.LogWarning("[Profile] 成就进度写点：成就表里**没有** id「" + id + "」⇒ 这一项没写"
                               + "（⛔ 不编 id / 不拿别的数顶上）");
                return false;
            }
            if (!AchievementAllowed(a, mode, replay)) return false;
            AddAchievementValue(id, delta);
            return true;
        }

        /// <summary>**不带模式闸**地按 `Ach_N` 累加（**对局之外**的动作专用：升级卡 / 登录）。
        /// <para>为什么不带闸：教程 / 决斗 / 回放闸是**对局条件**（原版 `ValidateMatchConditions`），
        /// 升级卡、登录这些动作根本不在一局里 ⇒ 没有模式可判。102 条的 `playModeAllow/Blocklist`
        /// 又全空 ⇒ 「对局外」这一档在原版也没有额外限制。</para></summary>
        public static bool GainAchievementOutsideMatch(string id, int delta)
        {
            if (delta <= 0) return false;
            if (AchievementById(id) == null)
            {
                Debug.LogWarning("[Profile] 成就进度写点（对局外）：成就表里**没有** id「" + id + "」⇒ 没写");
                return false;
            }
            AddAchievementValue(id, delta);
            return true;
        }

        /// <summary>🆕 **2026-10-25（`X2`）**：把进度**抬到至少** `value`（原版 `AchievementPersistence.highestValue`
        /// 的语义 —— **进度只涨不落**）。
        /// <para>用途 = `ConstantChallenge` 那两条（`Ach_17` 累计登录 / `Ach_18` 连登）：它们记的是
        /// **当前状态**（连登断了会归 1），而原版存档带 `highestValue` ⇒ **连登断签不该让成就进度倒退**。</para></summary>
        public static void SetAchievementValueAtLeast(string id, int value)
        {
            if (string.IsNullOrEmpty(id)) return;
            EnsureSave();
            int cur;
            if (!_achProgress.TryGetValue(id, out cur)) cur = 0;
            if (value <= cur) return;
            _achProgress[id] = value;
            SaveIfAuto();
        }

        /// <summary>🆕 **2026-10-25（执行代理 `X18`）**：`Ach_24`（`RewardCollected`：**收集不同的牌**）
        /// —— 把进度**抬到至少「引擎卡池张数」**，只写一次（幂等）。
        ///
        /// <para>🔴 **口径 = 我们挑的「全解锁」，不是原版口径**（判据 §五最后一段）：
        /// 原版这条在**领卡牌奖励**时按卡去重累加（`RewardCollected__OnTrigger.c` 的
        /// `ignoreDuplicates=1` + `checkAlreadyCollected=1`）；本项目「卡池全解锁」⇒ **没有领取动作**
        /// ⇒ 本地零信号。用户 2026-09-17 定的边界②（卡池全解锁）与 2026-10-25 的「补计数器」一起
        /// 逼出这个折中：**首次加载档案时把卡池张数记一次**。</para>
        /// <para>🔴 **取哪个数**：「**引擎卡池**」= `CardDatabase.Load()` 的张数（2026-10-25 现读 1127）
        /// —— 与**收藏窗能看到的张数同一口径**；⛔ **不写死 1127**（卡池会变，写死 = 悄悄错），
        /// ⛔ 也不取 `cards_truth.json` 的 1351（含 token / 未实装，收藏窗看不到）。
        /// 原版五档阈值 60·120·180·270·348 ⇒ 这个数一开局就把 5 档全打满（如实标）。</para>
        /// <para>**为什么用 `SetAchievementValueAtLeast` 而不是累加**：这条的语义是「**一共收集了多少种**」，
        /// 是个**状态量**（原版档案的 `highestValue`）⇒ 同一个玩家每次启动不能越加越多。</para>
        /// <returns>这次改变的差值（0 = 已经记过 / 卡池读不到）。</returns>
        public static int RecordCardPoolCollectedOnce()
        {
            EnsureSave();
            int n;
            try { n = RuleEngine.CardDatabase.Load().Count; }
            catch (Exception e)
            {
                Debug.LogWarning("[Profile] `Ach_24` 取卡池张数失败：" + e.Message + " ⇒ 这一项没写");
                return 0;
            }
            if (n <= 0)
            {
                Debug.LogWarning("[Profile] `Ach_24` 取到的卡池张数是 0（`cards_engine` 没读进来？）⇒ 这一项没写");
                return 0;
            }
            int cur;
            if (!_achProgress.TryGetValue(IdAchCardPool, out cur)) cur = 0;
            if (cur >= n) return 0;
            // 走 `SetAchievementValueAtLeast` 的同一条语义（只涨不落）—— ⛔ 别在这里再写一份「取 max」。
            SetAchievementValueAtLeast(IdAchCardPool, n);
            return n - cur;
        }

        /// <summary>
        /// **结算那一句**（唯一调用点 = `BattleDriver.AwardAchievements`）：把本局的
        /// <see cref="AchievementTally"/> + 结算那一刻的几个量，按 102 条各自的 `Class` / 过滤位
        /// 算出每条的增量，**带闸**写下去。
        ///
        /// <para>🔴 **口径三条**（判据 §二 / §B-4）：</para>
        /// <list type="number">
        ///   <item>**按 `Ach_N` 写**（⛔ 不走 challenge 原文，原文有重名）；</item>
        ///   <item>**模式口径按原版**（`GamesPlayed__OnTrigger.c`：看的是**模式**，**不是「对手是不是真人」**）
        ///         —— 2026-10-25 主对话裁断（判据 §F2）：`Classic` = 排位、`ClosedDeck` = 轮抽，
        ///         E13 当初加的 `_net != null` 条件**已去掉**（单机排位 / 单机轮抽照样计）；</item>
        ///   <item>**阵营**按我方督军的 `CardDef.Faction` 比 `Filters.army`（映射唯一一处 =
        ///         `TargetCriteriaEval.FactionOfArmy`）。</item>
        /// </list>
        /// </summary>
        /// <param name="tally">本局计数（`Ctx.Tally`；自检可以自己 new 一个喂数）。</param>
        /// <param name="me">我方座位（0/1）—— tally 里按它取自己那一份。</param>
        /// <param name="damageToEnemyWarlord">`Ach_1` 用：打敌方**督军**的实际伤害。</param>
        /// <param name="skullsCollected">`Ach_19`。</param>
        /// <param name="faithGained">`Ach_63`。</param>
        /// <param name="faction">我方督军的阵营名（`CardDef.Faction`；不知道就传 `null` ⇒ 带阵营过滤的那几条**不写**）。</param>
        /// <param name="won">我方赢（平局/输 = false）。</param>
        /// <param name="replay">这一局是**回放**（一律不计）。</param>
        public static void ApplyAchievementTally(AchievementTally tally, int me,
                                                 int damageToEnemyWarlord, int skullsCollected,
                                                 int faithGained, GameMode mode, string faction,
                                                 bool won, bool replay)
        {
            if (tally == null) return;
            var all = Achievements;
            for (int i = 0; i < all.Count; i++)
            {
                var a = all[i];
                int delta = TallyDeltaOf(a, tally, me, damageToEnemyWarlord, skullsCollected,
                                         faithGained, mode, faction, won);
                if (delta > 0) GainAchievement(a.Id, delta, mode, replay);
            }
        }

        /// <summary>一条成就在这一局的增量（纯函数 —— 自检可以直接喂它，不必真打一局）。
        /// 认不出 / 数不到 ⇒ **0**（⛔ 不猜）。每个 `case` 都注了判据出处。</summary>
        public static int TallyDeltaOf(Achievement a, AchievementTally tally, int me,
                                       int damageToEnemyWarlord, int skullsCollected,
                                       int faithGained, GameMode mode, string faction, bool won)
        {
            if (a == null || tally == null || (me != 0 && me != 1)) return 0;
            var f = FiltersOf(a);
            switch (a.Class)
            {
                // ---- 伤害（`OnDamageDelt`）：按类别取本局实际伤害 ----
                case "DamageDeltToEnemyUnit":
                    // `Ach_1` = `enemyType=10`（**督军**）+ 不过滤类/型 ⇒ 用结算那一刻的督军伤害。
                    if (string.Equals(a.Id, "Ach_1", StringComparison.Ordinal)) return damageToEnemyWarlord;
                    if (f == null) return 0;
                    return tally.DamageOf(me, DamageKindOf(f));

                // ---- 能力触发（`TriggerAbility`）：按 `triggerAbility` 数值取次数 ----
                case "TriggerAbility":
                    if (f == null || f.triggerAbility <= 0) return 0;
                    return tally.TriggerOf(me, f.triggerAbility);

                // ---- 出牌（`CardPlayedCount`）：按过滤位挑一个标签 ----
                case "CardPlayedCount":
                    {
                        int tag = CardPlayedTagOf(f);
                        return tag < 0 ? 0 : tally.CardsPlayedOf(me, tag);
                    }

                // ---- 骷髅 / 信仰（E13 已有）----
                case "SkullsCount":   return skullsCollected;
                case "CollectFaith":  return faithGained;

                // ---- 隐秘 / 破坏（判据 §B-3 #14）----
                // 写点 = `EffectResolver` 里**三处造隐秘**（`DoCreate` 手牌支 · `DA44` 的 `todeck` 支 ·
                // 任务点满阈值入牌库）与**一处造破坏**（`DoCreate` 的 `enemyhand` 支）——
                // 都是原版 `BroadcastSecretCreated` / `BroadcastSabotageCreated` 的同一批落点。
                // 归属 = 造牌的施放者（`Tally` 里就按那个座位记）。
                case "CreateSecret":    return tally.SecretsOf(me);      // `Ach_78` `Create Secrets`
                case "AddSabotageCard": return tally.SabotageOf(me);     // `Ach_65` `Add Sabotage cards to the enemy hand`

                // ---- 授予 / 再造 / 暴群（判据 §B-3 #12 / #13 / #15）----
                // 四条都按**施放者座位**记（`Tally` 里就是那个座位），读我方那一份。
                // · `GiveHuntsMark`   = `ApplyOneGain` 的 huntmark 授予点（原版 `GiveHuntsMark__OnAddCardEffect.c`：每次 +1）
                // · `GiveMarkOfChaos` = `GrantDarkPactCore` 的 `AddKeyword(DarkPact,1)`（原版 `GiveMarkOfChaos__OnAddCardEffect.c`）
                // · `UnitReanimated`  = `DoReanimate` 的两条支（原版那格 `unit+0x40` 过滤语义没解出来 ⇒ **近似**）
                // · `UnitSwarmCount`  = `RuleCore.SwarmMergeNow`（原版方法体撞名丢失 ⇒ 取「真合并了才算」，**近似**）
                case "GiveHuntsMark":   return tally.HuntMarksOf(me);     // `Ach_96` `Give Hunt Marks`
                case "GiveMarkOfChaos": return tally.MarksOfChaosOf(me);  // `Ach_6`  `Give a Dark Pact`
                case "UnitReanimated":  return tally.ReanimatedOf(me);    // `Ach_14` `Reanimate Remnant`
                case "UnitSwarmCount":  return tally.SwarmMergesOf(me);   // `Ach_10` `Trigger Swarm`

                // ---- 对局（`GamesPlayed`，30 条）----
                case "GamesPlayed":   return GamesPlayedDelta(f, mode, faction, won);

                // ---- 战役全通（`CompleteCampaign`，`Ach_16`）**不在本方法里算** ----
                // 它是**对局外**动作：唯一写点 = `Shell/CampaignData.Claim` →
                // `NotifyCampaignCompletionIfAllClaimed`（`checkAlreadyCompleted=1` ⇒ `SetAtLeast(…,1)`）。
                // 这里显式回 0（⛔ 别让它落进 `default` 里、看起来像「没接」）。
                case "CompleteCampaign": return 0;

                // 其余 12 类（升级卡 / 登录 / 开包 / 战役 / 轮抽战绩 / 再造 / 暴群 / 授予…）
                // **不在本方法里算** —— 它们各有自己的写点（见 `ApplyAchievementUpgrade` /
                // `DailyData.RecordLogin` / 判据 §B-3 #7~#15）。
                default: return 0;
            }
        }

        /// <summary>伤害类的过滤位 → <see cref="DamageKind"/>（判据 = 成就定义 SO 的 `data`，逐条现读）。
        /// 认不出 ⇒ `Unknown`（= 不计）。</summary>
        static DamageKind DamageKindOf(Filters f)
        {
            if (f.onlyDamageByShuriken == 1) return DamageKind.Shuriken;         // `Ach_7`
            if (f.onlyDamageByMarkerlight == 1) return DamageKind.Markerlight;   // `Ach_53`
            if (f.anyDamageType == 0 && f.damageType == 11) return DamageKind.Stomp;   // `Ach_86`（`DamageType.Stomp`）
            if (f.anyAttackType == 0)
            {
                // `AttackTypes`：1 Melee / 2 Ranged / 3 Spell（`Ach_2/3/4`）
                if (f.attackType == 1) return DamageKind.Melee;
                if (f.attackType == 2) return DamageKind.Ranged;
                if (f.attackType == 3) return DamageKind.Spell;
            }
            return DamageKind.Unknown;
        }

        /// <summary>出牌类的过滤位 → <see cref="CardPlayedTag"/> 槽位。认不出 ⇒ `-1`。
        /// （五条各一格闸，判据见 `AchievementTally.CardPlayedTag` 每个常量的 doc。）</summary>
        static int CardPlayedTagOf(Filters f)
        {
            if (f == null) return -1;
            if (f.onlyCountCompanions == 1) return CardPlayedTag.Companion;      // `Ach_54`
            if (f.anyTrait == 0 && TraitListed(f, 5)) return CardPlayedTag.Ephemeral;   // `Ach_12`（`DefinedTrait.ephemeral`）
            if (f.anyKeyword == 0 && f.keyword == 35) return CardPlayedTag.Beast;       // `Ach_87`（`Keyword.Beast`）
            if (f.anySpellType == 0 && f.spellType == 260)
                return f.anyCardType == 0 ? CardPlayedTag.ElixirTactic : CardPlayedTag.Elixir;   // `Ach_89`
            if (TraitListed(f, 610)) return CardPlayedTag.Pack;                  // `Ach_97`（`DefinedTrait.pack`）
            return -1;
        }

        static bool TraitListed(Filters f, int trait)
        {
            if (f.withTrait == null) return false;
            for (int i = 0; i < f.withTrait.Length; i++) if (f.withTrait[i] == trait) return true;
            return false;
        }

        /// <summary>我方模式 → 原版那两种**事件类型**（`GamesPlayed` 只认它）。
        /// <para>🔴 **`Classic` = 排位（`IRankedEvent`）· `ClosedDeck` = 轮抽（`IDraftEvent`）** ——
        /// 主对话 2026-10-25 裁断（判据 §F2），⛔ 别再夹「对手是不是真人」那条。
        /// 其余模式（`OfflinePractice` / `Skirmish` / `Campaign`…）**两种都不算**。</para></summary>
        static string ModeClassOf(GameMode mode)
        {
            if (mode == GameMode.Classic) return "ranked";
            if (mode == GameMode.ClosedDeck) return "draft";
            return null;
        }

        /// <summary>那一条 `GamesPlayed` 要的是哪一种模式（判据 = `gameModes[0]._typeNameAndAssembly`，
        /// 原版 `GamesPlayed__OnTrigger.c` 拿它做 `Type.IsAssignableFrom`）。认不出 ⇒ `null`。</summary>
        static string ModeClassWanted(Filters f)
        {
            if (f == null) return null;
            if (f.gameModes != null && f.gameModes.Length > 0)
            {
                string n = f.gameModes[0] != null ? (f.gameModes[0]._typeNameAndAssembly ?? "") : "";
                if (n.IndexOf("IRankedEvent", StringComparison.Ordinal) >= 0) return "ranked";
                if (n.IndexOf("IDraftEvent", StringComparison.Ordinal) >= 0) return "draft";
            }
            if (f.anyGameMode == 1) return null;           // 不筛模式（现读 0 条）
            if (f.onlyRanked == 1) return "ranked";        // 退路：`onlyRanked`
            return null;
        }

        /// <summary>`GamesPlayed` 那一条在这一局的增量（0 或 1）。</summary>
        static int GamesPlayedDelta(Filters f, GameMode mode, string faction, bool won)
        {
            string want = ModeClassWanted(f);
            if (want == null || want != ModeClassOf(mode)) return 0;
            if (f.onlyAccountWins == 1 && !won) return 0;
            if (f.anyArmy == 0 && !ArmyIcons.IsNeutral(f.army))
            {
                string wantFaction = TargetCriteriaEval.FactionOfArmy(f.army);
                if (wantFaction == null) return 0;                       // 认不出阵营 id ⇒ 不写（⛔ 不猜）
                if (faction == null) return 0;                           // 不知道我方阵营 ⇒ 不写
                if (!string.Equals(wantFaction, faction, StringComparison.OrdinalIgnoreCase)) return 0;
            }
            return 1;
        }

        /// <summary>
        /// **升级卡**（`Ach_23` / `25-36` / `55,56,59,60,68,69,74,75,82,83,92,93,99,100` —— 27 条）。
        /// 唯一调用点 = `Shell/CardDetailPopup.DoUpgrade` 的**成功点**（`CardProgress.SetLevel` 之后）。
        /// <para>过滤位：`anyTier == 1` ⇒ 任意档（`Ach_23`）；否则原版 `tier` 是 **0 基**
        /// （`CardTier.cs`：Tier1=0 … Tier4=3；`cards_engine` 的 `army` 表同一个 id 域）⇒
        /// **`tier + 1 == levelAfter`**（我们 `CardProgress.Level` 是 **1 基**）。
        /// 阵营同理比 `Filters.army`（映射唯一一处 = `TargetCriteriaEval.FactionOfArmy`）。</para>
        /// <para>⚠️ **已知偏离（判据 §F7）**：原版 `checkAlreadyUpgraded=1` 的口径是「**同一张卡只算第一次**」，
        /// 我们的 `Level` **单调** ⇒ 每次成功 +1（降级再升会重复）。如实记着，未修。</para>
        /// </summary>
        /// <param name="faction">升级的那张卡的 `CardDef.Faction`（空/null ⇒ 只记「不过滤阵营」的那几条）。</param>
        /// <param name="levelAfter">升级之后的等级（`CardProgress.Level`，**1 基**）。</param>
        /// <returns>这一跳写了几条成就（给自检用）。</returns>
        public static int ApplyAchievementUpgrade(string faction, int levelAfter)
        {
            int wrote = 0;
            var all = Achievements;
            for (int i = 0; i < all.Count; i++)
            {
                var a = all[i];
                if (!string.Equals(a.Class, "UpgradeCard", StringComparison.Ordinal)) continue;
                var f = FiltersOf(a);
                if (f == null) continue;
                if (f.anyTier != 1 && f.tier + 1 != levelAfter) continue;
                if (f.anyArmy == 1 || ArmyIcons.IsNeutral(f.army))
                {
                    if (GainAchievementOutsideMatch(a.Id, 1)) wrote++;
                    continue;
                }
                string want = TargetCriteriaEval.FactionOfArmy(f.army);
                if (want == null || faction == null) continue;
                if (!string.Equals(want, faction, StringComparison.OrdinalIgnoreCase)) continue;
                if (GainAchievementOutsideMatch(a.Id, 1)) wrote++;
            }
            return wrote;
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
                    // 🆕 2026-10-25（`X2`）：`Class` / `FiltersJson` / `TutorialOk` / `DuelOk` 是这一批新加的
                    //   四格（判据 §B-0）—— 没有它们，27 条升级 / 30 条对局 / 17 条触发在运行时**分不开**。
                    _ach.Add(new Achievement { Id = a.id,
                                               Name = Loc.BulkOr("Achievements/Title/" + a.id, a.n),
                                               Challenge = desc, ChallengeId = a.c, Type = a.t,
                                               Thresholds = a.v, Quantities = a.q,
                                               Class = a.k, FiltersJson = a.f,
                                               TutorialOk = a.tut != 0, DuelOk = a.duel != 0 });
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
                if (src[i].Army == army || ArmyIcons.IsNeutral(src[i].Army)) r.Add(src[i]);
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
        // · 🆕 **2026-10-25（执行代理 `R10` · 切换单 C8）：这份档已经进中央存档的双写期**。
        //   中央 key = `SaveKeys.CosmeticData`（**整份** payload，7 个字段一个不少），旧档 = 本文件那份
        //   `profile.json`（**同一份 payload、不带 `_v` 信封**，旧客户端照旧读得懂）。
        //   写：`SaveCenter.SaveThroughLegacy` · 读：`SaveCenter.ReadThroughLegacy`（先中央后旧档）。
        //   🔴 **6 个字段各归谁**（原版出处 → 我们的落点）：
        //     ┌ `playerName`      → 原版在 **PlayFab 玩家档案**（`PlayerDataManager.SetPlayfabDisplayName`
        //     │                     → `UpdateUserTitleDisplayName`），**不是** `PlayerDataKey` 里的任何一个
        //     │                     ⇒ 我们放 `CosmeticData`（本地没有「玩家档案」这一层）。
        //     │                     ⚠️ **不是 `NameChanges`** —— 那个 key 装的是改名历史
        //     │                     （`NameChangeRecord { originalName, changes[] }`）。
        //     ├ `avatarIndex` / `titleIndex` / `avatarBorderOn`
        //     │                   → 原版 `CosmeticData`（key 13）✅ 同一个 key。
        //     └ `achIds` / `achValues`
        //                         → 原版 `AchievementsData`（key 10），形状 = `Dictionary<string, AchievementPersistence>`
        //                           ⇒ `LegacySaveReader.BuildAchievementsPayload` 从同一份 `_achProgress` 投影。
        //                           ⚠️ **读口仍走 `CosmeticData`/`profile.json`**（见 `SaveNow` 那段注释：
        //                           旧客户端回滚时不至于读到过期的投影）。
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
            // 🆕 **2026-10-25（执行代理 `X18`）**：`Ach_24` 的**唯一写点** —— 真实档案加载完之后
            //   把「卡池张数」记一次（幂等，见 `RecordCardPoolCollectedOnce`）。
            //   ⚠️ **只在这一支（非批处理）调**：自检不碰真存档、也不该被这个「全解锁口径」污染
            //   （要自检它请显式调 `RecordCardPoolCollectedOnce()`，见 `RuleEngineTest_EngineX18`）。
            RecordCardPoolCollectedOnce();
        }

        static void LoadFromDisk()
        {
            try
            {
                // 🆕 2026-10-25（执行代理 `R10` · 切换单 C8）：**先中央档、再旧档**（同 `DailyData` 那一族的双写期读法）。
                //   · 落盘开着（真 Play）⇒ `CosmeticData` 在就用它，不在/读不出来才退回 `profile.json`；
                //   · 落盘关着（批处理 / 自检）⇒ `ReadThroughLegacy` **根本不看中央档**，只读 `SavePath`
                //     —— 与切换前逐字节同（自检注入的是 `SavePathOverrideForTest`，它算「旧档路径」这一侧）。
                // ⚠️ 为什么不直接调 `ReadThroughLegacy`：两边都没有时它会走 `SafeRead` 打一条「读不到」的
                //    warning，而「第一次跑、什么都还没有」是**正常状态** ⇒ 这里先探一下存在性，别刷假警告。
                bool hasLegacy = System.IO.File.Exists(SavePath);
                bool hasCentral = SaveCenter.StoreOn && SaveCenter.Exists(SaveKeys.CosmeticData);
                if (!hasLegacy && !hasCentral) return;                 // 第一次跑

                string text = SaveCenter.ReadThroughLegacy(SaveKeys.CosmeticData, SavePath);
                if (text == null)
                {
                    // 两边都在、但都读不出来（坏档 / 未来版本）—— `SaveCenter` 已经逐条出过声了，这里再点一次名
                    Debug.LogWarning("[Profile] 存档读不出来（中央档 `" + SaveKeys.CosmeticData + "` 与旧档 `"
                                   + SavePath + "` 都不行）⇒ 用默认值。⛔ 别当成「没存过」：查上面 `[SaveCenter]` 的日志");
                    return;
                }
                var d = JsonUtility.FromJson<SaveDto>(text);
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

        /// <summary>写盘（正式入口；内部会先做「越界值不落盘」的净化）。批处理下由 <see cref="SaveIfAuto"/> 挡掉。
        /// <para>🆕 **2026-10-25（执行代理 `R10` · 切换单 C8）**：改走 `SaveCenter.SaveThroughLegacy` ——
        /// 真 Play 里「中央档 `CosmeticData` + 旧档 `profile.json`」各写一份（两边是**同一份 payload**），
        /// 批处理 / 自检（`SaveCenter.StoreOn == false`）只写旧档、**一个字节都不碰玩家中央档**。
        /// 🔴 **payload 必须是 `SaveDto` 的整份**（7 个字段）：双写是「1 个 key ↔ 1 份旧档」，
        /// 少写一个字段 = 用少字段覆盖多字段 = 抹掉玩家名/成就（E3/R4 点名的地雷）。</para></summary>
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

            string json;
            try { json = JsonUtility.ToJson(_save, true); }
            catch (Exception e)
            {
                Debug.LogError("[Profile] 存档序列化失败：" + e.Message + " ⇒ **没写盘**（红线：不许静默失败）");
                return;
            }

            // 🔴 **写盘前的最后一道闸**（2026-10-25 · `R10`）：payload 必须含 `profile.json` 该有的**每一个**顶层键。
            //   为什么要它：双写是「同一份 payload 写两份」，**少一个字段 = 用少字段覆盖多字段** ——
            //   就是 E3/R4 点名的那颗地雷（当年中央档只有 2 个字段、旧档有 6 个）。有了这道闸，
            //   以后谁改 `SaveDto`（改名 / 删字段）会**当场拒绝写盘并出声**，而不是静默抹掉玩家数据。
            string missing = LegacySaveReader.MissingProfileFields(json);
            if (missing != null)
            {
                Debug.LogError("[Profile] 存档 payload **字段不全**（缺 " + missing + "）⇒ **不写盘**。"
                             + "`Shell/ProfileData.cs` 的 `SaveDto` 与 `LegacySaveReader.ProfileFields` 必须一一对应；"
                             + "这份档（含 `profile.json`）原地不动，玩家数据没被动过（红线：不许静默失败）");
                return;
            }

            string werr;
            if (!SaveCenter.SaveThroughLegacy(SaveKeys.CosmeticData, SavePath, json, out werr))
            {
                // 不许静默：写不进去要说出来（原版这一步是上传服务器）
                Debug.LogError("[Profile] 存档写盘失败（" + SavePath + "）：" + werr
                             + " ⇒ 这一份状态**只在内存里**，重启会丢（红线：不许静默失败）");
                return;
            }

            // ---- 成就进度的中央投影（原版 `AchievementsData`，key 10）------------------------------
            // 🔴 **只在落盘开着时写**：关着时 `SaveThroughLegacy` 那支只写旧档（自检常态），
            //    再调 `SetRaw` 只会换来一条「落盘关着」的噪声（而且自检本来就不该有中央档）。
            // ⚠️ 这份是**投影**（只写、暂时没人读）：读口仍然走 `CosmeticData`/`profile.json` 那份
            //    —— 那样「用户回滚旧客户端玩过一轮」时读到的还是最新的（旧档里有 achIds/achValues）。
            if (SaveCenter.StoreOn)
            {
                string ap = LegacySaveReader.BuildAchievementsPayload(ids.ToArray(), vals.ToArray());
                if (!SaveCenter.SetRaw(SaveKeys.AchievementsData, ap))
                    Debug.LogError("[Profile] 成就投影写盘失败（`" + SaveKeys.AchievementsData + "`）："
                                 + SaveCenter.LastWriteError + " ⇒ 中央档里成就进度是旧的"
                                 + "（`profile.json` 那一份仍是新的，玩家进度没丢）");
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
