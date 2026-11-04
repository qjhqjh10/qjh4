// BoosterTables.cs —— **卡包 / 掉落表**的运行时表（B143 · `B142-REWARD-INVENTORY` Step 0/1 的读口）
//
// ============================ 判据（唯一来源，⛔ 别在别处再抄一份）============================
//  · 数据产物 = `Assets/RuleEngine/Resources/booster_tables.json`（人读正本在 `Unity/数据/游戏数据/`，两份逐字节相同）。
//    产物**由生成器产生**：`Unity/工具/gen_booster_tables.py`（`--write` / `--check` / `--dry`）。⛔ 别手改 json。
//  · 原版形状（本轮逐份实读 `d:/2/新解包资源/assets_full`）：
//    - 卡包 = `m_Script.m_PathID -5965435178007098505` ⇒ `Everguild.LiveOps.ShopContainer`；
//      字段 `uniqueId` / `containerType` / `cardArmy` / `dropTableItems[].targetId`（= **5 个卡位**）/
//      `legendaryContainer.targetId` / `containerPreviewImage.m_SubObjectName` / `containerNameKey.mTerm`。
//    - 掉落表 = `m_Script.m_PathID -2313241570638646064` ⇒ `DropTableItem`；
//      字段 `uniqueId` / `cardArmy` / `cardRarity` / `obtainableItems` + `obtainableItemsWeight` /
//      `dropTableItems` + `dropTableItemsWeight`。
//    - 原版真身：`DropTableItem` 是 `PlayerItem : ObtainableItem`（`cpp2il/out_cpp2il_dev_ilspy/DropTableItem.cs`），
//      两侧候选 + 两侧权重；**同一行 SO 里只有一侧非空**，另一侧的权重数组是**上一版遗留**
//      （例 `DT Goff R1`：`obtainableItems` 34 条 / `dropTableItems` 0 条，而 `dropTableItemsWeight` 仍有 34 个）
//      ⇒ 生成器只取**候选非空**那一侧，遗留权重一律丢弃（⛔ 别把它当有效权重）。
//  · 原版「开包 / 开表」在**服务器**（`PlayfabWrapper__UnlockContainer.c:40-42` = 云函数 `0xf1`）⇒
//    我们这一步是**本地等价**：权重与候选照抄 SO，**只有随机数序列是我们定的**（见下 `🔶`）。
//
// ============================ 自己设计（非原版）两处 ============================
//  🔶 自己设计（非原版）：**掷骰的随机数序列**
//     · 为什么必须自己定：原版在服务器上掷（`UnlockContainer` 云函数 0xf1），关服后拿不到它的 RNG；
//       而项目红线要求引擎内只用 `System.Random(seed)`（⛔ 不用 `UnityEngine.Random`）。
//     · 权重 / 候选 / 卡位顺序**全部照抄 SO**，不是我们挑的。
//     · 唯一后果：同一 seed 可复现，但**与线上那一局的序列不同**（本来也无法相同）。
//     · 调度台 2026-11-05 裁定（`B143-R4` 报告）。
//
// ⚠️ 与 `BoosterPackOpenWindow.RollPack` 的关系：那是**改前**的自拟实现（1 张 rare+ + 4 张随机）。
//    Step 3（购买链改「先进背包再开」）会把它换成这里的 `RollPackIds`；**本文件先建、Step 3 再接**。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一条「id + 权重」（原文是两条平行的 List：候选数组 + 权重数组；我们合成一条）。</summary>
    [Serializable]
    public class BoosterEntry
    {
        public string id;
        public int weight;
    }

    /// <summary>一份卡包（原版 `ShopContainer`）。<see cref="slots"/> = **5 个卡位**，每格一条掉落表 id。</summary>
    [Serializable]
    public class BoosterContainerRow
    {
        public string id;
        public int army;
        public int containerType;
        public string nameKey;
        public string previewSprite;
        public string legendary;
        public string[] slots;
        public string src;
    }

    /// <summary>一张掉落表（原版 `DropTableItem`）。<c>cards</c> / <c>tables</c> **互斥**（原版 SO 同一行只填一侧）。</summary>
    [Serializable]
    public class BoosterDropTableRow
    {
        public string id;
        public int army;
        public int rarity;
        public BoosterEntry[] cards;
        public BoosterEntry[] tables;
        public string src;
    }

    [Serializable]
    public class BoosterCounts { public int containers; public int dropTables; }

    /// <summary>`booster_tables.json` 的 DTO。<c>duplicates</c> 是 `string[][]`，`JsonUtility` 不支持 ⇒ 不进 DTO。</summary>
    [Serializable]
    public class BoosterTableDoc
    {
        public int version;
        public string source;
        public string note;
        public BoosterCounts counts;
        public BoosterContainerRow[] containers;
        public BoosterDropTableRow[] dropTables;
    }

    /// <summary>卡包 / 掉落表的**只读运行时表**（懒加载 + 掷骰）。</summary>
    public static class BoosterTables
    {
        /// <summary>`Assets/RuleEngine/Resources/booster_tables.json` 的无扩展名路径。</summary>
        public const string ResourcePath = "booster_tables";

        /// <summary>掉落表嵌套的深度上限（`DT All → DT R1` 是 1 层；留余量，防数据成环）。</summary>
        public const int MaxRollDepth = 8;

        static BoosterTableDoc _doc;
        static Dictionary<string, BoosterContainerRow> _byContainer;
        static Dictionary<string, BoosterDropTableRow> _byDropTable;

        /// <summary>最近一次加载失败的原因（`null` = 成功或还没加载）。</summary>
        public static string LoadError { get; private set; }

        /// <summary>表加载好了吗（`true` 之后 <see cref="TryGetContainer"/> 才有意义）。</summary>
        public static bool Loaded { get { return _doc != null; } }

        /// <summary>卡包份数（未加载 = 0）。</summary>
        public static int ContainerCount { get { return _byContainer != null ? _byContainer.Count : 0; } }

        /// <summary>掉落表份数（未加载 = 0）。</summary>
        public static int DropTableCount { get { return _byDropTable != null ? _byDropTable.Count : 0; } }

        /// <summary>整张卡包表（给「建背包条目」这一层用；未加载会先 `EnsureLoaded`）。
        /// ⚠️ 返回的是**表自己的数组**，⛔ 别改它。</summary>
        public static BoosterContainerRow[] Containers
        {
            get { EnsureLoaded(); return _doc != null ? _doc.containers : null; }
        }

        /// <summary>整张掉落表（同上）。⛔ 别改。</summary>
        public static BoosterDropTableRow[] DropTables
        {
            get { EnsureLoaded(); return _doc != null ? _doc.dropTables : null; }
        }

        /// <summary>加载一次（幂等）。失败**出声**并留 <see cref="LoadError"/>（⛔ 不静默）。</summary>
        public static void EnsureLoaded()
        {
            if (_doc != null || LoadError != null) return;
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                LoadError = "`Resources/" + ResourcePath + ".json` 取不到（`Resources.Load<TextAsset>` 返 null）";
                Debug.LogWarning("[BoosterTables] " + LoadError + " ⇒ 卡包 / 掉落表两档一律当「查不到」");
                return;
            }
            try { BuildFromJson(ta.text); }
            catch (Exception e)
            {
                LoadError = "`" + ResourcePath + ".json` 解析失败：" + e.Message;
                Debug.LogWarning("[BoosterTables] " + LoadError);
            }
        }

        /// <summary>自检 / 探针用：直接从一段 json 文本装表（⛔ 生产侧别调）。</summary>
        public static void ReloadForTest(string jsonText)
        {
            _doc = null; _byContainer = null; _byDropTable = null; LoadError = null;
            if (string.IsNullOrEmpty(jsonText)) { LoadError = "空 json"; return; }
            BuildFromJson(jsonText);
        }

        /// <summary>自检收尾：丢回「未加载」（下一次 `EnsureLoaded` 会重新读 Resources）。</summary>
        public static void ResetForTest()
        {
            _doc = null; _byContainer = null; _byDropTable = null; LoadError = null;
        }

        static void BuildFromJson(string text)
        {
            var doc = JsonUtility.FromJson<BoosterTableDoc>(text);
            if (doc == null)
            {
                LoadError = "`JsonUtility.FromJson` 返 null";
                Debug.LogWarning("[BoosterTables] " + LoadError);
                return;
            }
            var byC = new Dictionary<string, BoosterContainerRow>(StringComparer.Ordinal);
            if (doc.containers != null)
                for (int i = 0; i < doc.containers.Length; i++)
                {
                    var r = doc.containers[i];
                    if (r == null || string.IsNullOrEmpty(r.id)) continue;
                    byC[r.id] = r;
                }
            var byD = new Dictionary<string, BoosterDropTableRow>(StringComparer.Ordinal);
            if (doc.dropTables != null)
                for (int i = 0; i < doc.dropTables.Length; i++)
                {
                    var r = doc.dropTables[i];
                    if (r == null || string.IsNullOrEmpty(r.id)) continue;
                    byD[r.id] = r;
                }
            _byContainer = byC;
            _byDropTable = byD;
            _doc = doc;
        }

        /// <summary>按 `uniqueId` 取一份卡包。</summary>
        public static bool TryGetContainer(string id, out BoosterContainerRow row)
        {
            row = null;
            EnsureLoaded();
            return !string.IsNullOrEmpty(id) && _byContainer != null && _byContainer.TryGetValue(id, out row);
        }

        /// <summary>按 `uniqueId` 取一张掉落表。</summary>
        public static bool TryGetDropTable(string id, out BoosterDropTableRow row)
        {
            row = null;
            EnsureLoaded();
            return !string.IsNullOrEmpty(id) && _byDropTable != null && _byDropTable.TryGetValue(id, out row);
        }

        /// <summary>权重数组之和（<= 0 表示这张表掷不出来）。</summary>
        public static int WeightTotal(BoosterEntry[] list)
        {
            if (list == null) return 0;
            long sum = 0;
            for (int i = 0; i < list.Length; i++) if (list[i] != null && list[i].weight > 0) sum += list[i].weight;
            return sum > int.MaxValue ? int.MaxValue : (int)sum;
        }

        /// <summary>在一张「id + 权重」表上掷一次（`rnd` 由调用方给，保证可复现）。
        /// 权重全 0 / 表为空 ⇒ `null`。</summary>
        public static string PickWeighted(BoosterEntry[] list, System.Random rnd)
        {
            if (list == null || list.Length == 0 || rnd == null) return null;
            int total = WeightTotal(list);
            if (total <= 0) return null;
            int r = rnd.Next(total);                 // [0, total)
            for (int i = 0; i < list.Length; i++)
            {
                var e = list[i];
                if (e == null || e.weight <= 0) continue;
                if (r < e.weight) return e.id;
                r -= e.weight;
            }
            // 理论上到不了（r < total）；出声而不是静默返回第一张
            Debug.LogWarning("[BoosterTables] 加权掷骰越界（total=" + total + "）⇒ 退回最后一格，如实出声");
            for (int i = list.Length - 1; i >= 0; i--) if (list[i] != null && !string.IsNullOrEmpty(list[i].id)) return list[i].id;
            return null;
        }

        /// <summary>展开一张掉落表，返回**一张卡的 id**（原版 `DropTableItem` 的一次展开）。
        /// 子表为空 / 取不到 / 层数超限 ⇒ `null`（调用方出声）。</summary>
        public static string RollDropTable(string dropTableId, System.Random rnd)
        {
            return RollDropTable(dropTableId, rnd, 0);
        }

        static string RollDropTable(string dropTableId, System.Random rnd, int depth)
        {
            if (string.IsNullOrEmpty(dropTableId) || rnd == null) return null;
            if (depth > MaxRollDepth)
            {
                Debug.LogWarning("[BoosterTables] 掉落表嵌套超过 " + MaxRollDepth + " 层（`" + dropTableId + "`）⇒ 停止展开");
                return null;
            }
            BoosterDropTableRow row;
            if (!TryGetDropTable(dropTableId, out row) || row == null)
            {
                Debug.LogWarning("[BoosterTables] 掉落表 `" + dropTableId + "` 不在 `booster_tables.json` 里 ⇒ 这一格掷不出卡");
                return null;
            }
            // 叶子：候选卡
            if (row.cards != null && row.cards.Length > 0) return PickWeighted(row.cards, rnd);
            // 中间层：子表
            if (row.tables != null && row.tables.Length > 0)
            {
                string sub = PickWeighted(row.tables, rnd);
                if (string.IsNullOrEmpty(sub)) return null;
                return RollDropTable(sub, rnd, depth + 1);
            }
            Debug.LogWarning("[BoosterTables] 掉落表 `" + dropTableId + "` 两侧候选都是空的 ⇒ 这一格掷不出卡");
            return null;
        }

        /// <summary>开一包：按 <paramref name="containerId"/> 的 5 个卡位逐格展开，返回卡 id 数组
        /// （顺序 = 卡位顺序 = 原版 `dropTableItems` 的顺序）。
        /// <para>⚠️ **Step 3 之前没有生产调用方**（`BoosterPackOpenWindow` 仍走它自己的 `RollPack`）。</para></summary>
        public static string[] RollPackIds(string containerId, int slotCount, int seed)
        {
            BoosterContainerRow c;
            if (!TryGetContainer(containerId, out c) || c == null || c.slots == null)
            {
                Debug.LogWarning("[BoosterTables] 卡包 `" + containerId + "` 不在表里 ⇒ 开不出一包");
                return new string[0];
            }
            int n = slotCount > 0 ? slotCount : c.slots.Length;
            if (slotCount > 0 && slotCount != c.slots.Length)
                Debug.LogWarning("[BoosterTables] 请求 " + slotCount + " 格，但原版 `" + containerId + "` 是 "
                               + c.slots.Length + " 格（按请求数取，越界格给 null）");
            var rnd = new System.Random(seed);
            var outp = new string[n];
            for (int i = 0; i < n; i++)
                outp[i] = i < c.slots.Length ? RollDropTable(c.slots[i], rnd) : null;
            return outp;
        }
    }
}
