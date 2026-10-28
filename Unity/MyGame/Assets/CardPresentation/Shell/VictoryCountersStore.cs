// VictoryCountersStore.cs — `PlayerDataKey.VictoryCounters`（第 44 项）的 DTO + 本地写入口
//
// ==================================================================================================
//  原版出处（判据，第一权威 = `d:/2/tools/`）
// ==================================================================================================
//  · `PlayerDataKey.cs:47` 的 `VictoryCounters` = 枚举第 **44** 项（0 基：`none=0` 哨兵之后数）；
//    我们 `CardPresentation/Shell/SaveKeys.cs:193` 的 `VictoryCounters = "VictoryCounters"`（`// 44`，
//    与 `:289` 的枚举位同号同名）—— 两侧**同名同号**，已经就位（本文件不改条数，只用它当 key）。
//  · 值形状 = `Dictionary<string, VictoryCounter>`：
//      - `PlayerDataManager.victoryCounters`（字段 `+0x2c8`，`get/set` 只是 load/store，
//        `PlayerDataManager__get/set_victoryCounters.c`）；
//      - `VictoryCounter`（`dump.cs:83398` / `Warpforge_code/.../VictoryCounter.cs`）：
//          `public int totalVictories;`（`+0x10`）
//          `public Dictionary<string,int> victoriesById;`（`+0x18`）
//          `public string HighestCardId { get; }`（**只读属性，不是字段** —— 见下面 getter 判据）
//  · 写入源 = 对局结束：`PlayerDataManager.ProcessMatchResult`（`cpp2il PlayerDataManager.cs:9480`）
//    把 `matchResult.VictoryCounters` 直接赋给玩家的 `victoryCounters`（`+0x2c8`）。
//    ⚠️ ILSpy 把 `OnlineMatchResult.VictoryCounters` 声明成 `Dictionary<string,int>`，但 `:9480` 的
//       那次赋值目标是 `Dictionary<string,VictoryCounter>`（`+0x2c8`）⇒ 真身是
//       **`Dictionary<string, VictoryCounter>`**，ILSpy 那行泛型参数是反编译器误判（作废声明，按 `+0x2c8` 的字段类型为准）。
//
//  **`HighestCardId` 的判据**：`VictoryCounter__get_HighestCardId.c` = `victoriesById.OrderByDescending(kv => kv.Value)`
//    然后取第一项的 **Key**（lambda `b__3_0` 返回 `*(kv+8)` = value；`FirstOrDefault` 取最高者）
//    ⇒ = 「`victoriesById` 里计数最大的那个 id」，空表 ⇒ `null`。
//
//  **key 是什么（「卡 id / uniqueId / 名字」这条，实读判清，不是猜）**：
//    `WarlordMasteryContainer.OnInitialize`（`cpp2il/out_cpp2il_dev_ilspy/WarlordMasteryContainer.cs:120-230`）
//    把 `highestGroup.Value.HighestCardId` 与战斗日志里那条 hero 串**直接喂**
//    `AssetLocator.GetAsset<RawCardScript>(id)`，兜底还写成字面量 `"UM47"` ⇒
//    **外层 key 与 `victoriesById` 的 key 都是「`AssetLocator` / `RawCardScript` 能认的卡 id 字符串」**
//    （形如 `UM47` 的 asset 名），**⛔ 不是数字 uniqueId、也不是本地化卡名**。
//    ⚠️ **仍不可判定的一格**：外层 key（这张 `VictoryCounter` 属于哪个督军）与 `victoriesById`（按什么卡分桶）
//    的**语义分工**（自己督军 vs 击败的对手督军）在本地反编译里**读不出来** —— 那份字典由**服务端**填，
//    客户端只读。本文件按「外层 key = 玩家督军卡 id；`victoriesById` = 同一 id 空间的卡 id」落地，
//    ⛔ **不写死语义**（字段名照原版，语义注释如实标「未判定」）。
//
// ==================================================================================================
//  🔴 为什么本文件要绕开 `SaveCenter.Set<Dictionary<…>>`
// ==================================================================================================
//  我们的存档走 `SaveCenter.Get<T>/Set<T>`（`SaveCenter.cs:156/:263`），内部是 **`JsonUtility`**
//  （原版是 Newtonsoft）—— `JsonUtility` **不能序列化 `Dictionary<,>`**（会静默写成 `{ }`）。
//  ⇒ 本文件用一个 `[Serializable]` 的 **展开壳**（`Blob/Group/Pair`，列表形态）在 `SaveCenter` 上落盘，
//    对外仍是 `Dictionary<string, VictoryCounter>`。`VictoryCounter` 本身保持**原版字段名/序**
//    （`totalVictories` / `victoriesById`），⛔ 没有加原版没有的字段。
//
//  ⚠️ **旧档缺失**：key 44 在我们这边**一直是空的**（`SaveKeys` 那只注写着「【我们】空缺」）⇒ 读不到时
//    **出声**（`LogWarning`）并按**空表**返回；`HasData()` 供需要区分「没有这份档」与「有档但 0 胜」的调用方用
//    （⛔ 别把「没档」静默当成「0 胜」）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 原版 `VictoryCounter`（`dump.cs:83398`）。**字段名/序逐字照原版**：
    /// `totalVictories`（`+0x10`）· `victoriesById`（`+0x18`）。
    /// `HighestCardId` 是**只读计算属性**（原版也是 `{ get; }`，不是序列化字段）。
    /// </summary>
    [Serializable]
    public class VictoryCounter
    {
        public int totalVictories;
        public Dictionary<string, int> victoriesById = new Dictionary<string, int>();

        /// <summary>原版 `VictoryCounter.get_HighestCardId`：`victoriesById` 里计数最大的那个 **key**
        /// （判据 = `VictoryCounter__get_HighestCardId.c`）；空表 ⇒ `null`。
        /// ⚠️ 平手取**枚举顺序第一个**（原版 `OrderByDescending(...).FirstOrDefault()`；字典本身无序，
        /// 这与原版同形，不要额外排序）。</summary>
        public string HighestCardId
        {
            get
            {
                if (victoriesById == null || victoriesById.Count == 0) return null;
                string best = null;
                int bestN = int.MinValue;
                foreach (var kv in victoriesById)
                {
                    if (kv.Value > bestN) { bestN = kv.Value; best = kv.Key; }
                }
                return best;
            }
        }
    }

    /// <summary>
    /// `PlayerDataKey.VictoryCounters`（第 44 项）的**本地读写口**。
    /// ⚠️ 原版对局结束是「用**服务端下发的整份** `matchResult.VictoryCounters` **替换**本地那份」
    /// （`PlayerDataManager.ProcessMatchResult`，`cpp2il:9480`）；我们没有服务端 ⇒ 本地只能**累加**。
    /// 🔶 自己设计（非原版）：`RecordVictory` 走**累加**（原版是「服务端整份替换」） · 为什么必须自己定：
    /// 本地无 PlayFab/CloudScript 下发（P3，用户 2026-10-26 授权调度台判 D），拿不到权威整份 ⇒ 只能本地累计 ·
    /// 调度台 2026-10-27 裁定。
    /// </summary>
    public static class VictoryCountersStore
    {
        /// <summary>`SaveKeys.VictoryCounters`（`// 44`，与原版 `PlayerDataKey` 第 44 项同名同号）。</summary>
        public const string Key = SaveKeys.VictoryCounters;

        // ---- `JsonUtility` 友好的展开壳（`Dictionary` 不能直存，见文件头） ----
        [Serializable] class Pair { public string id; public int count; }
        [Serializable] class Group
        {
            public string id;
            public int totalVictories;
            public List<Pair> victoriesById = new List<Pair>();
        }
        [Serializable] class Blob { public List<Group> entries = new List<Group>(); }

        /// <summary>key 44 在盘上**有没有**（用来区分「没有这份档」与「有档但 0 胜」；⛔ 不静默当 0）。</summary>
        public static bool HasData()
        {
            return SaveCenter.GetRaw(Key) != null;
        }

        /// <summary>
        /// 读出整张表。**缺 key / 读不出 ⇒ 空表 + 出声**（`LogWarning`）—— ⛔ 不静默当「0 胜」；
        /// 要区分就配 <see cref="HasData"/>。
        /// </summary>
        public static Dictionary<string, VictoryCounter> Load()
        {
            return LoadCore(warnIfMissing: true);
        }

        /// <summary>整表落盘（成功 = 真写进盘）。空表也照写（明确表达「有档、0 条」）。</summary>
        public static bool Save(Dictionary<string, VictoryCounter> map)
        {
            var blob = new Blob();
            if (map != null)
            {
                foreach (var kv in map)
                {
                    if (kv.Key == null || kv.Value == null) continue;
                    var g = new Group { id = kv.Key, totalVictories = kv.Value.totalVictories };
                    var vid = kv.Value.victoriesById;
                    if (vid != null)
                        foreach (var p in vid)
                            if (p.Key != null) g.victoriesById.Add(new Pair { id = p.Key, count = p.Value });
                    blob.entries.Add(g);
                }
            }
            return SaveCenter.Set(Key, blob);
        }

        /// <summary>
        /// **本地记一场胜利**：`warlordCardId` 那张督军的 `totalVictories += wins`；再对
        /// <paramref name="defeatedCardIds"/> 里每个 id 在 `victoriesById` 上 `+= wins`。
        /// 返回是否**真落盘**。
        /// <para>⚠️ 见类头：原版这里是「服务端整份替换」，本地只能累加（🔶 已标）。
        /// ⚠️ `warlordCardId` 空 ⇒ 拒写 + 出声（`totalVictories` 挂在某个督军 id 下，没 id 无处可挂）。</para>
        /// </summary>
        public static bool RecordVictory(string warlordCardId, IEnumerable<string> defeatedCardIds, int wins = 1)
        {
            if (string.IsNullOrEmpty(warlordCardId))
            {
                Debug.LogError("[VictoryCountersStore] RecordVictory 的 `warlordCardId` 为空 ⇒ 拒写"
                             + "（原版这张表以督军卡 id 为外层 key，没 id 无处挂）。");
                return false;
            }
            if (wins <= 0) return true;      // 0 胜 = 无事可做（不写盘、不报错）

            var map = LoadCore(warnIfMissing: false);
            VictoryCounter vc;
            if (!map.TryGetValue(warlordCardId, out vc))
            {
                vc = new VictoryCounter();
                map[warlordCardId] = vc;
            }
            vc.totalVictories += wins;
            if (defeatedCardIds != null)
            {
                foreach (string id in defeatedCardIds)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    int prev;
                    vc.victoriesById.TryGetValue(id, out prev);
                    vc.victoriesById[id] = prev + wins;
                }
            }
            return Save(map);
        }

        static Dictionary<string, VictoryCounter> LoadCore(bool warnIfMissing)
        {
            var blob = SaveCenter.Get<Blob>(Key);
            if (blob == null)
            {
                if (warnIfMissing)
                    Debug.LogWarning("[VictoryCountersStore] key '" + Key
                                   + "' 不存在 / 读不出（旧档或还没写过）⇒ 按**空表**返回（不是「0 胜」；"
                                   + "要区分请用 HasData()）。");
                return new Dictionary<string, VictoryCounter>();
            }
            var map = new Dictionary<string, VictoryCounter>();
            if (blob.entries != null)
            {
                foreach (var g in blob.entries)
                {
                    if (g == null || g.id == null) continue;
                    var vc = new VictoryCounter { totalVictories = g.totalVictories };
                    if (g.victoriesById != null)
                        foreach (var p in g.victoriesById)
                            if (p != null && p.id != null) vc.victoriesById[p.id] = p.count;
                    map[g.id] = vc;
                }
            }
            return map;
        }

        /// <summary>原版 `PlayerInfo.GetVictoryCounters()`（账 478）：把「**原始卡 id → 场次**」表
        /// 按**原卡 id** 归并成 `Dictionary&lt;string, VictoryCounter&gt;`。
        /// <para>逐句判据 = `decomp_full/PlayerInfo__GetVictoryCounters.c`：
        /// 遍历 `this.__field_VictoryCounters`（`Dictionary&lt;string,int&gt;`：rawId→count）→
        /// `AssetLocator.GetAsset&lt;RawCardScript&gt;(rawId).GetOriginalId()` 取原卡 id →
        /// 结果表里没有该原卡 id 就 `new VictoryCounter()` → `totalVictories += count` → `victoriesById[rawId] = count`。</para>
        /// <para>「原卡 id」的判据 = 原版 `RawCardScript.GetOriginalId()`（异画 → 原卡 id）⇒ 我们的等价 =
        /// `RuleEngine.PlayerDeck.SameCardKey`（异画条目剥成原卡 id、其余原样）。
        /// ⚠️ 差异（同族）：原版 `AssetLocator` 拿不到卡时 NRE；我们出声并**跳过**该条（坏数据不该让游戏崩）。</para></summary>
        public static Dictionary<string, VictoryCounter> GetVictoryCounters(
            IEnumerable<KeyValuePair<string, int>> rawVictories)
        {
            var result = new Dictionary<string, VictoryCounter>();
            if (rawVictories == null) return result;
            foreach (var kv in rawVictories)
            {
                if (string.IsNullOrEmpty(kv.Key))
                {
                    Debug.LogError("[VictoryCountersStore] GetVictoryCounters：原始卡 id 为空 ⇒ 跳过该条"
                                 + "（原版 `AssetLocator.GetAsset` 拿不到卡会 NRE）");
                    continue;
                }
                string originalId = RuleEngine.PlayerDeck.SameCardKey(kv.Key);   // = 原版 GetOriginalId()
                VictoryCounter vc;
                if (!result.TryGetValue(originalId, out vc))
                {
                    vc = new VictoryCounter();
                    result[originalId] = vc;
                }
                vc.totalVictories += kv.Value;                 // 原版：totalVictories += count
                vc.victoriesById[kv.Key] = kv.Value;           // 原版：victoriesById[rawId] = count
            }
            return result;
        }
    }
}
