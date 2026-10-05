// ProfileData.cs — 玩家档案窗要用的两张清单（**称号**与**头像**）
//
// ============================ 这是什么、不是什么 ============================
// 🔴 **这是【原版资产的清单】，不是【玩家存档】**。
//    · 原版这两页的列表来自**服务器**（`PlayerDataManager.fullCosmeticCollection.OfType<CosmeticItemTitle>()` /
//      `PlayerAvatarDataManager.GetAllItems<T>()`），本地**没有存档**；
//    · 但**定义数据（ScriptableObject）在本地是全的** —— 470 个头像 SO + 462 个称号 SO
//      （`素材/Warpforge原版/装饰品/定义数据/MonoBehaviour/`，那是游戏自己的资产，不是存档）。
//    · ⇒ 由 `工具/gen_profile_cosmetics.py` 抽成 `Resources/profile_cosmetics.json` 给运行时读。
//    用户 2026-09-27 拍板：**列表照填**（配合他定的「资源 9999 / 卡池与头像全解锁」那条边界）。
//
// 🔴 **显示名是我们拼的**（原版真名在远端 I2 语言表，本地只有 key）：
//    · 头像：`nameTextReference`（形如 `Avatar_UM_Attack Bike`）去掉 `Avatar_<阵营>_` 前缀 ⇒ `Attack Bike`；
//      变体头像的 `nameTextReference` 是**底图名**（原版就这么设计的，用于去重显示）。
//    · 称号：462 条的 `nameTextReference` **全是空串** ⇒ 只能拿 `m_Name`（`Title_UM_Premium_1`）
//      去掉 `Title_`、下划线换空格 ⇒ `UM Premium 1`。
//    ⚠️ **所以界面上那些名字不是原版文案**，是从资源名反推的 —— 这一点在代码注释与 `_note` 字段里都写着。
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
        }

        /// <summary>一条装饰品。`Art` 只有头像有（称号的 `imageReference` 是空的）。</summary>
        public class Item
        {
            public string Id;
            public string Name;
            public int Army;
            public string Art;
            public override string ToString() { return Name; }
        }

        /// <summary>一条成就（`Trophies` 页用）。**字段全是原版资产里真有的** —— 判据 → `资料/普查产出_0927/档案窗_Trophies页.md` §C3。</summary>
        public class Achievement
        {
            /// <summary>`eventId`（形如 `Ach_1`）。</summary>
            public string Id;
            /// <summary>显示名 —— **资产里的真字符串**（`ACH1 Slay the Warlord` 的 `m_Name` 去掉 `ACH&lt;n> ` 前缀）。</summary>
            public string Name;
            /// <summary>`challenge` 的 `id`（如 `Damage To Warlord`）—— 也是资产里的真字符串。
            /// ⚠️ 原版那一格装的是 `LocalizedText`（I2 词条），**译文本地查不到** ⇒ 我们拿它顶，
            /// 并在界面上如实标着（`Shell/AchievementsMenu.cs` 的文件头）。</summary>
            public string Challenge;
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
        /// <summary>分类名。⚠️ **原版真文案是 I2 词条 `Achievements/Types/{枚举名}`，译文在远端查不到**（普查 §C1）
        /// ⇒ 我们用**枚举名本身**（`%s` 就是它）—— 这是**我们的选择**，预制体里那 4 个 `'Secret'` 是占位，别用。</summary>
        public static string TypeName(int t)
        {
            switch (t)
            {
                case TypeBattle: return "Battle";
                case TypeCollection: return "Collection";
                case TypeVictories: return "Victories";
                case TypeAccount: return "Account";
                default: return "?";
            }
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

        /// <summary>🔴 **玩家显示名 —— 全工程唯一一份**（档案窗的 `Player Name` 与联机层握手报的名字读的都是它）。
        /// · **原版这个名字来自服务器**（`PlayerInfo` 的玩家名），本地没有 ⇒ **默认取机器名**，
        ///   这是**我们的选择**（2026-09-26 联机那次定的口径：「我们没有玩家名那套数据源，用机器名当显示名，
        ///   并在界面/日志里说明」）；
        /// · 档案窗的 `ChooseNameWindow` 改名写的就是这里 —— **只在本次会话里有效**（我们**没有存档**，
        ///   原版那一步会上传服务器并扣改名费）；
        /// · **别再在别处写第二份**（`NetSession.PlayerName()` 原来自己取机器名，2026-09-27 已收口到这里）。</summary>
        public static string PlayerName
        {
            get { return string.IsNullOrEmpty(_nameOverride) ? DefaultPlayerName : _nameOverride; }
            set { _nameOverride = value; }
        }

        /// <summary>🔴 **默认显示名 —— 用户 2026-09-28 拍板：「暂时显示『玩家123』之类的」**。
        /// 原来这里是**机器名**（2026-09-26 联机那次定的，理由是「我们没有玩家名那套数据源」）——
        /// 现在改成这个固定占位名（界面上一眼看得出是占位，不会把 `DESKTOP-XXXX` 当成昵称）。
        /// ⚠️ **联机时两边若都没改过名就会同名** —— 玩家档案窗的改名窗可以改（`ProfileData.PlayerName` setter）。
        /// ⚠️ 原版这个名字来自服务器，本地没有 ⇒ **这是我们的选择**。</summary>
        public const string DefaultPlayerName = "玩家123";

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

        /// <summary>当前选中头像的**下标**（档案窗 `Avatar` 页写、顶栏/`Profile` 页/`Ranked` 页读）。</summary>
        public static int AvatarIndex = 0;

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
        /// <summary>102 条成就（`Trophies` 页）。</summary>
        public static List<Achievement> Achievements { get { EnsureLoad(); return _ach; } }

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
            Fill(d.avatars, _avatars);
            Fill(d.titles, _titles);
            if (d.achievements != null)
                for (int i = 0; i < d.achievements.Length; i++)
                {
                    var a = d.achievements[i];
                    _ach.Add(new Achievement { Id = a.id, Name = a.n, Challenge = a.c, Type = a.t,
                                               Thresholds = a.v, Quantities = a.q });
                }
            _loaded = true;
        }

        static void Fill(CosmeticDto[] src, List<Item> dst)
        {
            if (src == null) return;
            for (int i = 0; i < src.Length; i++)
                dst.Add(new Item { Id = src[i].id, Name = src[i].n, Army = src[i].a, Art = src[i].art });
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
    }
}
