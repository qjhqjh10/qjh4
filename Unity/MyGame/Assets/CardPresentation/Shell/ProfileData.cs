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
            public string _note;
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

        static List<Item> _avatars, _titles;
        static bool _loaded, _failed;

        /// <summary>469 张可选头像（已按 阵营 → 名字 排好序）。</summary>
        public static List<Item> Avatars { get { Ensure(); return _avatars; } }
        /// <summary>462 个称号（同上排序）。</summary>
        public static List<Item> Titles { get { Ensure(); return _titles; } }

        /// <summary>给自检用的：清单是不是真的读进来了（没读进来时会**出声一次**，不静默）。</summary>
        public static bool Loaded { get { Ensure(); return _loaded; } }

        static void Ensure()
        {
            if (_loaded || _failed) return;
            _avatars = new List<Item>();
            _titles = new List<Item>();
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                _failed = true;
                // 🔴 **不许静默失败**：缺了它，Title/Avatar 两页会变成空列表，而空列表**看着像「没数据」**
                Debug.LogError("[Profile] 读不到 `Resources/" + ResourcePath + ".json` ⇒ "
                             + "称号/头像两页会是**空列表**。生成器：`工具/gen_profile_cosmetics.py`");
                return;
            }
            FileDto d = null;
            try { d = JsonUtility.FromJson<FileDto>(ta.text); }
            catch (Exception e) { Debug.LogError("[Profile] `" + ResourcePath + "` 解析失败：" + e.Message); }
            if (d == null) { _failed = true; return; }
            Fill(d.avatars, _avatars);
            Fill(d.titles, _titles);
            _loaded = true;
        }

        static void Fill(CosmeticDto[] src, List<Item> dst)
        {
            if (src == null) return;
            for (int i = 0; i < src.Length; i++)
                dst.Add(new Item { Id = src[i].id, Name = src[i].n, Army = src[i].a, Art = src[i].art });
        }

        /// <summary>按阵营筛（`0` = 全阵营通用，**任何阵营都算它**；`army <= 0` = 不筛）。</summary>
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
