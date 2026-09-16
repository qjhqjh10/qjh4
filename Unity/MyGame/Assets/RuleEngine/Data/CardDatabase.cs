// CardDatabase.cs — 卡表加载
//
// ⚠️ **`Core/` 下的文件一律不碰 UnityEngine**（加 asmdef 隔离时要保证 `Core/` 能单独编过）。
//    `Data/` 下允许碰 —— 本文件和 `Data/DeckStore.cs`（存档要走 persistentDataPath）都用。
//
// 数据不是直接读 `数据/游戏数据/card_stats.json`（978 KB，大半是引擎用不到的路径字符串），
// 而是读 `工具/gen_cards_engine.py` 生成的精简版（232 KB，数值已归一化、没有 null）。
// 改数据 → 重跑那个脚本 → 再跑 `--check` 对账。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuleEngine
{
    public static class CardDatabase
    {
        /// <summary>`Assets/RuleEngine/Resources/cards_engine.json`</summary>
        public const string ResourcePath = "cards_engine";

        [Serializable]
        public class FileDto
        {
            public int version;
            public int count;
            public string source;
            public CardDto[] cards;
        }

        [Serializable]
        public class CardDto
        {
            /// <summary>**稳定 id**（`AM12` 这样的原版 id，或 `AM_Some_Card` 这样的自造 id）。
            /// 见 `gen_cards_engine.py` 里 `IDS_SRC` 那段 —— 原版 id 取不到时自造，两种形态一眼可分。</summary>
            public string id;
            public string name;
            public string type;
            public int cost;
            public int attack;
            public int health;
            public int ranged;
            public string[] keywords;
            public string desc;
            public string faction;
            public string rarity;
            /// <summary>兵种（`Infantry` / `Vehicle` / `Drone` / `Elixir` / `Secret` …）。
            /// 空串 = 原版数据里就没有。见 `CardDef.Subtype` 的说明。</summary>
            public string subtype;
            /// <summary>中文名（可选 —— 没翻译的卡没有这两个字段）。来自 `数据/卡牌翻译/zh_cards.json`</summary>
            public string nameZh;
            public string descZh;
        }

        /// <summary>从 Resources 读卡表。取不到返回空表（不抛异常 —— 让调用方能自己决定怎么报）。</summary>
        public static List<CardDef> Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogError($"[RuleEngine] 找不到卡表 Resources/{ResourcePath}.json —— "
                             + "跑一下：\"D:/2/Warpforge_tools/py312/python.exe\" d:/4/Unity/工具/gen_cards_engine.py");
                return new List<CardDef>();
            }
            return Parse(asset.text);
        }

        public static List<CardDef> Parse(string json)
        {
            var list = new List<CardDef>();
            if (string.IsNullOrEmpty(json)) return list;

            var dto = JsonUtility.FromJson<FileDto>(json);
            if (dto == null || dto.cards == null) return list;

            // ⚠️ 卡表 < v6 就没有 `id` 字段（v6 = 2026-09-13 第三十三轮开始，每张卡带稳定 id）。
            //    读旧表**不报错**，但 `Id` 会退回卡名 —— 那是老毛病，得说出来，别让人以为身份是稳的。
            if (dto.version < 6)
                Debug.LogWarning($"[RuleEngine] 卡表 version={dto.version} < 6 —— 没有稳定 id，"
                               + "`CardDef.Id` 退回**卡名**（同名卡会串）。跑一下 gen_cards_engine.py 重生成。");

            int noId = 0;
            foreach (var c in dto.cards)
            {
                if (c == null || string.IsNullOrEmpty(c.name)) continue;
                string cid = c.id;
                if (string.IsNullOrEmpty(cid)) { cid = c.name; noId++; }
                list.Add(new CardDef(
                    id: cid,                    // 稳定 id（v6 起）；缺了才退回卡名，并在下面报数
                    name: c.name,
                    type: c.type,
                    desc: c.desc,
                    rarity: c.rarity,
                    faction: c.faction,
                    cost: c.cost,
                    attack: c.attack,
                    health: c.health,
                    rangedAttack: c.ranged,
                    keywords: c.keywords,
                    nameZh: c.nameZh,
                    descZh: c.descZh,
                    fromOriginalPool: true,     // 这条路上来的都是原版卡（卡面文字取它自己的效果原文）
                    subtype: c.subtype));
            }
            if (noId > 0)
                Debug.LogWarning($"[RuleEngine] 卡表里有 {noId} 张卡**没有 id**，已退回卡名当身份 —— "
                               + "同名卡（跨阵营那 4 组）会串在一起。重跑 gen_cards_engine.py。");
            // 卡名索引（2026-09-16）：「按卡名指目标」那条路的判据 —— `ParseTarget` 要问
            // 「卡面写的那个名词是不是一张卡的名字」。**只此一处建**，见 `CreatePool.BuildNameIndex`。
            CreatePool.BuildNameIndex(list);
            return list;
        }

        // ---- 常用筛选 ----

        public static List<CardDef> Units(IEnumerable<CardDef> pool)
        {
            var outList = new List<CardDef>();
            foreach (var c in pool) if (c != null && c.Type == "unit") outList.Add(c);
            return outList;
        }

        public static List<CardDef> OfFaction(IEnumerable<CardDef> pool, string faction)
        {
            var outList = new List<CardDef>();
            foreach (var c in pool) if (c != null && c.Faction == faction) outList.Add(c);
            return outList;
        }

        public static CardDef Find(IEnumerable<CardDef> pool, string name)
        {
            foreach (var c in pool) if (c != null && c.Name == name) return c;
            return null;
        }

        /// <summary>
        /// 按卡名在**指定阵营**里找。找不到返回 null。
        ///
        /// 🔴 **为什么需要这个重载**（2026-09-13 第三十二轮）：**原版有跨阵营同名卡**
        /// （`Terminator` / `Terminator Champion` / `Maulerfiend` / `Bladeguard Veteran` …），
        /// 而 <see cref="Find"/> 只按名字匹配、**取池子里第一个** —— 同名卡谁在前谁赢。
        /// 后果是 2026-09-13 那次改名之后暴露出来的：卡组里明明存的是 Ultramarines 那张
        /// `Bladeguard Veteran`，`Find` 却先撞上 DarkAngels 那张同名卡 ⇒
        /// `DeckRules.Validate` 判 `WrongFaction`、**一副合法卡组被打回自动凑**。
        /// （⚠️ 卡池按 `cards_engine.json` 的顺序遍历，所以「谁在前面」是**数据顺序**决定的，
        /// 换个数据源就可能翻车 —— 这正是不能靠巧合的地方。）
        ///
        /// ⚠️ **`faction` 传 null / 空 = 退回旧行为**（只按名字找），所以既有调用点一处不用改。
        /// </summary>
        public static CardDef Find(IEnumerable<CardDef> pool, string name, string faction)
        {
            if (string.IsNullOrEmpty(faction)) return Find(pool, name);
            foreach (var c in pool)
                if (c != null && c.Name == name && DeckRules.SameFaction(c.Faction, faction)) return c;
            // 阵营里没有同名卡 —— 不假装找到（调用方按 null 处理）
            return null;
        }

        /// <summary>按卡名找督军（`type == "hero"`）</summary>
        public static CardDef FindHero(IEnumerable<CardDef> pool, string name)
        {
            foreach (var c in pool) if (c != null && c.Type == "hero" && c.Name == name) return c;
            return null;
        }

        /// <summary>
        /// 按**稳定 id** 找卡（2026-09-13 第三十三轮）。**这是唯一没有歧义的找法** ——
        /// 按卡名找永远要担心跨阵营重名（见 <see cref="Find(IEnumerable{CardDef}, string, string)"/>），
        /// id 不会。**凡是要指「哪一张牌」的地方，都该用这个，不该用卡名。**
        /// 找不到返回 null。
        /// </summary>
        public static CardDef FindById(IEnumerable<CardDef> pool, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var c in pool) if (c != null && c.Id == id) return c;
            return null;
        }

        /// <summary>`id → CardDef`（要反复按 id 找时先建一次，别在循环里 `FindById`）。</summary>
        public static Dictionary<string, CardDef> IdIndex(IEnumerable<CardDef> pool)
        {
            var map = new Dictionary<string, CardDef>(StringComparer.Ordinal);
            foreach (var c in pool)
            {
                if (c == null || string.IsNullOrEmpty(c.Id)) continue;
                map[c.Id] = c;         // id 撞车在生成器那一侧就炸了（见 gen_cards_engine.py），这里不判
            }
            return map;
        }

        /// <summary>
        /// **把卡组里的一条「牌引用」解析成卡** —— 解析卡组引用的**唯一一处**。
        ///
        /// 顺序有意为之（2026-09-13 第三十三轮定，配合稳定 id）：
        ///   ① **按稳定 id 找**（`IdIndex`）—— 新卡组存档写的就是 id，**没有歧义、不需要阵营**；
        ///   ② 找不到再**按卡名找**（给了 `faction` 就只在本阵营里找）——
        ///      这条只为**旧存档**留着：2026-09-13 之前卡组里存的是**卡名**，
        ///      而原版有跨阵营同名卡（`Terminator` / `Bladeguard Veteran` …），
        ///      所以走这条必须带 `faction`，否则会撞上另一个阵营那张。
        ///
        /// 两条都找不到返回 null，**由调用方报错**（不静默）。
        /// </summary>
        public static Func<string, CardDef> DeckLookup(IList<CardDef> pool, string faction = null)
        {
            var byId = IdIndex(pool);
            return key =>
            {
                if (string.IsNullOrEmpty(key)) return null;
                CardDef c;
                if (byId.TryGetValue(key, out c)) return c;   // ① 稳定 id
                return Find(pool, key, faction);              // ② 卡名（旧存档）
            };
        }

        /// <summary>
        /// **id 不变量自检**（给 `RuleEngineTest` 用）：每张卡都有 id、且两两不同。
        /// 返回人类的说明（过了就是空串）—— 不抛异常，让自检去断言。
        /// </summary>
        public static string CheckIds(IEnumerable<CardDef> pool)
        {
            var seen = new Dictionary<string, string>(StringComparer.Ordinal);
            int n = 0;
            foreach (var c in pool)
            {
                if (c == null) continue;
                n++;
                if (string.IsNullOrEmpty(c.Id)) return $"「{c.Name}」没有 id";
                if (seen.TryGetValue(c.Id, out var other))
                    return $"id 撞车：{c.Id} —— 「{other}」与「{c.Name}」共用";
                seen[c.Id] = c.Name;
            }
            return n == 0 ? "卡池是空的" : "";
        }

        /// <summary>数据集里出现过的阵营（按卡数从多到少）</summary>
        public static List<string> Factions(IEnumerable<CardDef> pool)
        {
            var count = new Dictionary<string, int>();
            foreach (var c in pool)
            {
                if (c == null || string.IsNullOrEmpty(c.Faction)) continue;
                count[c.Faction] = (count.TryGetValue(c.Faction, out var n) ? n : 0) + 1;
            }
            var list = new List<string>(count.Keys);
            list.Sort((a, b) => count[b].CompareTo(count[a]));
            return list;
        }
    }
}
