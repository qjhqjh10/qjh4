// CardbackTable.cs — 原版 cosmetic ScriptableObject 里**卡背那一批**的运行期替身。
//
// 为什么需要它：卡背页那个筛选抽屉（原版 `Cosmetic FIlter`）里的 **`Army Filter` 要按阵营筛卡背**，
// 而「哪张卡背属于哪个阵营」在原版是**跑在 SO 上的**（`CosmeticItemCardback.cardArmy`）。
// 我们的 `Assets/` 里没有那份 SO（只在 `d:/2` 的导出里）⇒ 压成 `Resources/Cardbacks.json` 按图名查。
// **同一模式**：`UnitTweens.json` / `OffensiveCards.json` / `EnvironmentConditions.json` / `VfxMap`。
//
// 数据：`工具/gen_cardbacks.py` 从 `bundle_cosmeticsso_assets_all/MonoBehaviour/*.json`（1261 份）生成；
//       对账实测（同一次生成）：**243 个卡背 SO → 233 个图名，0 个对不上、0 个本地图缺表项、
//       0 个阵营查不出**（10 个 SO 与别的共用同一张图，逐对核过**阵营一致**，无信息损失）。
//
// ==================================================================
//  判据（不猜）
// ==================================================================
//  · `cardArmy` 是 **`CardArmy` 枚举**（`d:/2/tools/il2cpp_out/dump.cs:45637-45650`）：
//    `0 Neutral · 10 Ultramarines · 20 Goff · 30 SaimHann · 40 Sautekh · 50 BlackLegion ·
//     60 Leviathan · 70 TauEmpire · 80 Sororitas · 90 Genestealers · 100 AstraMilitarum ·
//     110 DarkAngels · 120 EmperorsChildren · 130 SpaceWolves`
//    —— 与我们的 `CardDef.Faction` **逐字同名**（`cards_engine.json` 里就是这 13 个加 `Neutral` 之外的 13 个），
//    所以表里直接存**枚举名**，这一层不用再转。
//  · 🔴 **对账键不是 SO 名**：SO 名与图名**有 23 个对不上**（`Cardback_AM_Forge` ↔
//    `Cardback_AM_Forge_Grit and Determination` 那种）。真正对得上的是
//    **`imageReference.m_SubObjectName` 去掉结尾 `_Main`**（= 图集里那张 sprite 的名字，
//    而我们的 PNG 就是按 sprite 名导的）—— 实测 243/243 全中。生成脚本里写着这一段。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>卡背 → 阵营那张表。**只读**，懒加载 `Resources/Cardbacks.json`。</summary>
    public static class CardbackTable
    {
        public const string ResourcePath = "Cardbacks";

        [Serializable]
        public class Item
        {
            public string name;      // **我们的图名**（= `CardArt.CosmeticNames()` 里那一串）
            public string so;        // 原版 SO 名（可复查的坐标）
            public int armyId;       // `CardArmy` 枚举值
            public string army;      // 枚举名（= `CardDef.Faction` 那套；`Neutral` = 不属于任何阵营）
            public int rarity;
            public string uniqueId;
        }

        [Serializable]
        public class DefaultItem
        {
            public string army;      // 阵营（= `CardDef.Faction`）
            public string name;      // **我们的图名**（`CardArt.Cosmetic(name)` 直接取得到）
            public string cardback;  // 原版 SO 名（可复查的坐标）
            public string uniqueId;
        }

        [Serializable]
        class File
        {
            public string note;
            public Item[] items;
            /// <summary>🆕 2026-10-03（A20）：**每阵营的默认卡背** —— 原版 `DefaultCarbackByArmySO` 那张表。</summary>
            public DefaultItem[] defaults;
        }

        static Item[] _all;
        static Dictionary<string, Item> _byName;

        public static int Count { get { EnsureLoaded(); return _all != null ? _all.Length : 0; } }

        static void EnsureLoaded()
        {
            if (_byName != null) return;
            _byName = new Dictionary<string, Item>(StringComparer.Ordinal);
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                // **不静默**：表不在就当「一个都查不到」，调用方按「查不到」处理（并在自检里红）
                Debug.LogWarning("[CardbackTable] 取不到 Resources/" + ResourcePath + ".json —— "
                               + "跑 `python d:/4/Unity/工具/gen_cardbacks.py` 生成");
                _all = new Item[0];
                return;
            }
            File f = null;
            try { f = JsonUtility.FromJson<File>(ta.text); }
            catch (Exception e) { Debug.LogError("[CardbackTable] 解析失败：" + e.Message); }
            _all = (f != null && f.items != null) ? f.items : new Item[0];
            foreach (var it in _all)
                if (it != null && !string.IsNullOrEmpty(it.name)) _byName[it.name] = it;
            _defaultByArmy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (f != null && f.defaults != null)
                foreach (var d in f.defaults)
                    if (d != null && !string.IsNullOrEmpty(d.army) && !string.IsNullOrEmpty(d.name))
                        _defaultByArmy[d.army] = d.name;
        }

        static Dictionary<string, string> _defaultByArmy;

        /// <summary>🆕 2026-10-03（A20）：**该阵营的默认卡背**（图名）—— 原版 `ArmyUtilities.GetDefaultCardback`。
        /// 查不到（阵营名不对 / 表没生成）返回 **null**（调用方自己决定退路，别拿空串冒充）。
        /// 判据只有这一处：`Resources/Cardbacks.json` 的 `defaults`，由 `工具/gen_cardbacks.py`
        /// 调 `工具/read_default_cardbacks.py` 从**原版原始字节**读出来（那份 SO 不在任何 bundle/导出里）。</summary>
        public static string DefaultFor(string army)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(army) || _defaultByArmy == null) return null;
            string v;
            return _defaultByArmy.TryGetValue(army, out v) ? v : null;
        }

        /// <summary>表里登记了几个阵营的默认卡背（自检用：应等于 13）。</summary>
        public static int DefaultCount { get { EnsureLoaded(); return _defaultByArmy != null ? _defaultByArmy.Count : 0; } }

        /// <summary>图名 → 阵营（`CardArmy` 枚举名；`Neutral` = 不属于任何阵营）。
        /// 查不到返回 **null**（别拿空串冒充 —— 调用方要能区分「查不到」与「Neutral」）。</summary>
        public static string ArmyOf(string cardbackName)
        {
            EnsureLoaded();
            Item it;
            return (cardbackName != null && _byName.TryGetValue(cardbackName, out it)) ? it.army : null;
        }

        /// <summary>图名 → 整条（要稀有度/uniqueId 时用）。查不到返回 null。</summary>
        public static Item Find(string cardbackName)
        {
            EnsureLoaded();
            Item it;
            return (cardbackName != null && _byName.TryGetValue(cardbackName, out it)) ? it : null;
        }

        /// <summary>按阵营筛图名（`army` 传 null/空 = 不筛）。判据只有这一处 ——
        /// 卡背页那个 `Army Filter` 与自检都走它。**顺序照 `allNames`**（铺格的次序不能变）。</summary>
        public static string[] NamesFor(string army, string[] allNames)
        {
            if (allNames == null) return new string[0];
            if (string.IsNullOrEmpty(army)) return allNames;
            var res = new List<string>();
            for (int i = 0; i < allNames.Length; i++)
            {
                var a = ArmyOf(allNames[i]);
                if (a != null && string.Equals(a, army, StringComparison.OrdinalIgnoreCase)) res.Add(allNames[i]);
            }
            return res.ToArray();
        }
    }
}
