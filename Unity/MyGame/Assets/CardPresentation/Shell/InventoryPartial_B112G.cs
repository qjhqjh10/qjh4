// InventoryPartial_B112G.cs — 执行代理 `B112_G`（2026-11-01 · 第一百一十一会话第 2 波）· 账 `B-84`
//
// ============================ 这一段补的是什么 ============================
//  `B-14`（`B111_S2`）只堵住了**未来**的坏写入：`CardInventory.Add` 现在会先播种
//  （`Inventories.EnsureCardsSeededForCardAdd()`）。但**已经写坏的盘上档不会自愈** ——
//  `InventoryStore.Load` 的 `RestoreAmounts` 会把盘上那个 `1` 原样灌回背包。
//  本段 = 那件事的**一次性迁移**：把盘上「被写成数量 1」的卡条目**按唯一播种口径重算一遍**。
//
// ============================ 判据（⛔ 别另写第二份算法）============================
//  「这张卡应当在背包里有多少张」= **唯一播种口径**：
//      `Inventories.SeedCardsAllUnlocked(pool, c => CardProgress.Owned(c.Id, c.Rarity))`
//      —— 与 `Shell/CollectionWindow.cs:903`（`CardsState` getter）**逐字同一句**。
//  ⇒ 本段**不自己算数量**：先把坏条目**摘掉**，再**原样调一次那个播种方法**，
//     让 `CardProgress.Owned` 在「条目不存在」的状态下给出它本该给的值
//     （= `DeckCap(rarity) + (NeedCopies[MaxLevel] - NeedCopies[1])`，恒 ≥ 10）。
//  ⚠️ 为什么不能「把已有条目补到 n」：播种的 amount 回调是 `CardProgress.Owned`，而它对
//     **已在背包里的**条目直接回条目数量 ⇒ n 就是那个 1，补了等于没补（`判据/B110_V2` 四① 的实证）。
//
// ============================ 三条硬要求（`B-84` 的验收口径）============================
//  ① **幂等**：连跑两遍结果相同（第二遍找不到低于基准量的条目 ⇒ 一个字节都不写）。
//  ② **可回滚**：写盘前先把旧档**复制**一份 `<档>.B112G-bak-<UTC 时间戳>`（同目录，改名即可回滚）。
//     备份失败 ⇒ **整次迁移不动**（⛔ 不回滚就等于不可回滚）。
//  ③ **只动坏的那几条**：`amount >= 基准量` 的条目全部不碰；**一条坏记录都没有时连 `Save` 都不调**
//     （⇒ 正确档**逐字节不变**，这是「防顺手改坏」的关键）。
//  取不到应有数量（id 在卡池里查不到 / 播种后仍低于基准量）⇒ **不动 + `LogError` 出声**（⛔ 不许猜）。
//
// ============================ 🆕 2026-11-01 `B-161`：坏行判据 = 「低于基准量」============================
//  原来两处判据都写成「== 1 / > 1」（`BadCardAmountSentinel`）⇒ **`1 < 数量 < 基准量` 的陈旧值两头都漏**：
//    · 坏行筛选（`kv.Value != BadCardAmountSentinel` ⇒ `continue`）根本**挑不进 `bad`**；
//    · 复核（`now > BadCardAmountSentinel ⇒ repaired`）会把播种出来的 5 当「修好」。
//  5 从哪来：播种那句 `CardProgress.Owned` 对**背包里没有的卡**先查 `_owned` 缓存（`Shell/CardDetailPopup.cs:243-249`）
//  ⇒ `WarpforgeCardLevels.json` 残留的 `owned[5]` 会压过「给足」公式（应为 11），被原样播进背包并**落盘、不出声**。
//  现判据：**期望值 = 基准量**（`ExpectedCardAmount`，见下面那一处注释），`now >= expected` 才算修好；
//  `now < expected` ⇒ 进 `stillBad` + `LogError`，而且**落盘前先把它恢复成迁移前的值**
//  （`InventoryStore.Save` 写的是整份 `SnapshotAmounts()` ⇒ 不还原等于把那个 5 趁别人的 Save 写进盘）。
//
// ============================ 挂在哪儿 / 为什么挂那儿 ============================
//  `InventoryStore.Load` 的 `inv.RestoreAmounts(am);` **之后**（同一物理行追加，既有行号零漂移），
//  且只有 `inv is CardInventory` 才跑（商店 / 异画 / 万能卡那几份背包不受影响）。
//  选这里的理由：它正是「**读盘之后、任何消费者之前**」的唯一那一拍 —— `CardsState` getter
//  （`CollectionWindow.cs:903`）也是「播种 → `InventoryStore.Load` → 返回」，迁移完刚好交出去。
//  ⚠️ 落盘门（`InventoryStore.StoreOn`）在 `Load` 开头已经判过 ⇒ **批处理 / 自检绝不碰玩家真档**。
//
// ============================ 已知未覆盖（如实记）============================
//  · `WarpforgeCardLevels.json` 的 `owned` 数组若也残留 1，播种会**照样**把它当基准量写进背包
//    （`CardProgress.Owned` 对「背包里没有的卡」优先读 `_owned` 缓存）。本迁移只认 `cards.json`
//    —— 那条是另一份档，要动得先有裁定（见 `报告/B112W_B161.md` §边界 · 第 3 条）。
//  · 中央档（`WarpforgeSave/OursInventory.cards.json`）的备份也做了，但**自检里 `SaveCenter.StoreOn`
//    关着** ⇒ 自检只覆盖旧档那一份（生产两遍都走 `SaveCenter.SaveThroughLegacy`，形状相同）。
using System;
using System.Collections.Generic;

namespace CardPresentation
{
    // ============================================================ ① 只给迁移用的「摘掉一条」
    public partial class Inventory<T> where T : class
    {
        /// <summary>**只给 `B-84` 一次性迁移用**：按 id 摘掉一条（返回是否真的摘掉了）。
        /// <para>为什么不直接用 `ClearForTest()`：那会把**整份背包**清空 —— 迁移只许动那几条坏记录
        /// （「已经正确的条目一个字节都不动」是 `B-84` 的硬要求）。</para>
        /// <para>⚠️ 摘掉之后**没有**对应的「补回」口：补回一律走
        /// <see cref="Inventories.SeedCardsAllUnlocked"/>（= 唯一播种口径），⛔ 别在这里自己算数量。</para></summary>
        internal bool RemoveEntryForMigration(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            ItemInfo<T> info;
            if (!_byId.TryGetValue(id, out info)) return false;
            _byId.Remove(id);
            if (info != null) NotifySubsets(info);   // 通知订阅方（`Cards` 目前没登记子集 ⇒ 空转；留着是为了将来不漏通知）
            return true;
        }
    }

    // ============================================================ ② 一次性迁移
    public static partial class Inventories
    {
        /// <summary>「确定坏」的哨兵值 —— `B-84` 记的就是这个 1（`B-14` 修之前 `CardInventory.Add`
        /// 给新卡建的第一条数量 = 奖励数量 1，之后播种那句 `if (Cards.Get(c) != null) continue;` 永远跳过它）。
        /// <para>为什么「1」一定是坏的：全解锁口径下基准量 = `DeckCap(rarity) + 9`，**恒 ≥ 10**；
        /// 而且本工程**没有任何**扣卡路径（升级只做「够不够」检查、不真扣 —— 见 `CardDetailPopup.DoDowngrade`
        /// 的注释「不消耗任何东西」）⇒ 合法值不可能落到 1。</para>
        /// <para>🆕 2026-11-01 `B-161`：**它现在只是「卡池查不到时」用来判「确定坏」的地板**，不再是修复判据 ——
        /// 修复判据是「**低于基准量**」（见 <see cref="ExpectedCardAmount"/>）。`1 < 数量 < 基准量` 同样是坏的。</para></summary>
        public const int BadCardAmountSentinel = 1;

        /// <summary>迁移备份的后缀：`<档>.B112G-bak-<UTC 时间戳>`（**与档同目录** ⇒ 回滚 = 改名回来）。</summary>
        public const string MigrationBackupSuffix = ".B112G-bak-";

        static bool _cardsAmountMigrationDone;
        static bool _cardsAmountMigrating;

        /// <summary>最近一次迁移的报告（`null` = 本进程还没跑过）。自检与诊断读它。</summary>
        public static string LastCardAmountMigrationReport { get; private set; }

        /// <summary>自检用：把「本进程已经跑过迁移」这一位清掉（配合临时 `OverrideDir` ⇒ 同一个进程里能反复验）。
        /// ⚠️ 只有 `Editor/**` 该调它 —— ⛔ 别在运行时路径里拿它当开关。</summary>
        public static void ResetCardAmountMigrationForTest() { _cardsAmountMigrationDone = false; LastCardAmountMigrationReport = null; }

        /// <summary>`B-84` 一次性迁移：把盘上「数量 = `BadCardAmountSentinel`」的卡条目按唯一播种口径重算。
        /// <para><paramref name="diskAmounts"/> = **刚从盘上读出来的** `id → 数量`（`InventoryStore.Load` 手里那一份）。
        /// 用它而不是读背包，是为了「盘上是 1」与「内存里是 1」不混为一谈
        /// （`RestoreAmounts` 会跳过背包里没有的 id）。</para>
        /// <returns>`true` = 这一次**真的改了盘**（至少修好一条且落盘成功）；
        /// `false` = 无事可做 / 没修好 / 没写成（原因一律在 `LastCardAmountMigrationReport` 与日志里）。</returns></summary>
        public static bool MigrateBadCardAmounts(IReadOnlyDictionary<string, int> diskAmounts)
        {
            if (_cardsAmountMigrating) return false;                 // 重入保护（播种链路里若有人再摸 Load）
            if (_cardsAmountMigrationDone) return false;             // 进程内只做一次
            if (diskAmounts == null || diskAmounts.Count == 0)
            {
                _cardsAmountMigrationDone = true;
                LastCardAmountMigrationReport = "盘上没有条目 ⇒ 无事可做";
                return false;
            }

            _cardsAmountMigrating = true;
            try
            {
                var bag = Cards;
                if (bag == null)
                {
                    UnityEngine.Debug.LogError("[Inventories] `B-84` 迁移：`Inventories.Cards` 为 null ⇒ 迁移不做（不猜）");
                    return false;                                    // 不置位：下次还有机会
                }
                var pool = RuleEngine.CardDatabase.Load();
                if (pool == null || pool.Count == 0)
                {
                    UnityEngine.Debug.LogError("[Inventories] `B-84` 迁移：卡池为空（`CardDatabase.Load()` 没拿到）"
                                             + " ⇒ **不动**（算不出应有数量，⛔ 不猜）");
                    return false;                                    // 不置位：下次还有机会
                }

                // ① 盘上的 id → 卡定义（播种口径只认卡池；池外的 id 一律**不动 + 出声**）
                var defs = new Dictionary<string, RuleEngine.CardDef>(StringComparer.Ordinal);
                for (int i = 0; i < pool.Count; i++)
                {
                    var c = pool[i];
                    if (c == null || string.IsNullOrEmpty(c.Id)) continue;
                    if (!defs.ContainsKey(c.Id)) defs[c.Id] = c;
                }

                // ② 先把「坏行」找出来 —— 🆕 2026-11-01 `B-161`：判据 = **低于基准量**（原来只挑「== 1」）。
                //    ⚠️ 卡池查不到的 id 算不出基准量 ⇒ 只把「确定坏」的那一档（`<= BadCardAmountSentinel`）报出来，
                //       其余**不动也不报**（⛔ 不猜）。
                var bad = new List<string>();
                var unresolved = new List<string>();
                var expectedById = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var kv in diskAmounts)
                {
                    RuleEngine.CardDef d;
                    if (string.IsNullOrEmpty(kv.Key) || !defs.TryGetValue(kv.Key, out d))
                    {
                        if (kv.Value <= BadCardAmountSentinel) unresolved.Add(kv.Key ?? "<null>");
                        continue;
                    }
                    int want = ExpectedCardAmount(d);
                    expectedById[kv.Key] = want;
                    if (kv.Value < want) bad.Add(kv.Key);          // ★ `1 < 数量 < 基准量` 现在也进 bad
                }

                var sb = new System.Text.StringBuilder();
                sb.Append("盘上 ").Append(diskAmounts.Count).Append(" 条；低于基准量的 ")
                  .Append(bad.Count + unresolved.Count).Append(" 条（可解析 ").Append(bad.Count).Append(" / 卡池查不到 ")
                  .Append(unresolved.Count).Append("）");

                if (unresolved.Count > 0)
                    UnityEngine.Debug.LogError("[Inventories] `B-84` 迁移：有 " + unresolved.Count
                        + " 条「数量 ≤ 1」的 id **在卡池里查不到** ⇒ 不知道它该有多少张 ⇒ 这几条**不动**（⛔ 不猜）："
                        + string.Join(", ", unresolved.ToArray()));

                if (bad.Count == 0)
                {
                    _cardsAmountMigrationDone = true;
                    LastCardAmountMigrationReport = sb.Append("；没有可修的 ⇒ 一个字节都没写").ToString();
                    return false;                                    // ★ 正确档走这里：连 Save 都不调 ⇒ 逐字节不变
                }

                // ③ 行集守恒闸：`InventoryStore.Save` 写回的是 `SnapshotAmounts()` ⇒「盘上有、背包里没有」的 id
                //    会被**静默丢掉**。发现这种行 ⇒ 整次迁移不动 + 出声（宁可这轮不修，也不许毁数据）。
                var missing = new List<string>();
                foreach (var kv in diskAmounts)
                    if (!string.IsNullOrEmpty(kv.Key) && bag.Get(kv.Key) == null) missing.Add(kv.Key);
                if (missing.Count > 0)
                {
                    UnityEngine.Debug.LogError("[Inventories] `B-84` 迁移：盘上有 " + missing.Count
                        + " 条 id 在背包里**不存在**（写回时会被丢掉）⇒ **整次迁移不动**，原档一个字节没改："
                        + string.Join(", ", missing.ToArray()));
                    _cardsAmountMigrationDone = true;
                    LastCardAmountMigrationReport = sb.Append("；行集不守恒 ⇒ 不动").ToString();
                    return false;
                }

                // ④ 备份（备份不成就整次不动 —— 不可回滚的迁移不许做）
                //    ⚠️ **中央档只在它真的会被改时才备份**：`SaveCenter.StoreOn == false`（批处理 / 自检没注入
                //    `SaveCenter.OverrideRoot`）时 `SaveThroughLegacy` **只写旧档**、一个字节都不碰中央档 ——
                //    这时去备份中央档会 **在玩家的真存档目录里写文件**（批处理自检污染真档，红线）。
                string stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", System.Globalization.CultureInfo.InvariantCulture);
                string legacyPath = InventoryStore.StorePath(CardProgress.InventoryName);
                string centralPath = null;
                if (SaveCenter.StoreOn)
                {
                    string centralKey = SaveKeys.OursInventoryPrefix + CardProgress.InventoryName;
                    try { centralPath = SaveCenter.PathOf(centralKey); }
                    catch (Exception e) { UnityEngine.Debug.LogWarning("[Inventories] `B-84` 迁移：算不出中央档路径（" + centralKey + "）：" + e.Message + " ⇒ 只备份旧档"); }
                }

                string legacyBak, centralBak;
                if (!BackupOne(legacyPath, stamp, out legacyBak) || !BackupOne(centralPath, stamp, out centralBak))
                {
                    _cardsAmountMigrationDone = true;
                    LastCardAmountMigrationReport = sb.Append("；**备份失败 ⇒ 整次迁移不动**").ToString();
                    return false;
                }

                // ⑤ 摘掉坏条目 → **原样调唯一播种口径**（⛔ 这里不自己算数量）
                var before = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0; i < bad.Count; i++)
                {
                    var info = bag.Get(bad[i]);
                    before[bad[i]] = info != null ? info.Amount : -1;
                    bag.RemoveEntryForMigration(bad[i]);
                }
                SeedCardsAllUnlocked(pool, c => CardProgress.Owned(c.Id, c.Rarity));   // ★★ 与 `CollectionWindow.CardsState` 逐字同一句

                // ⑥ 复核：**相对基准量**才算修好（🆕 `B-161`：原来只要求 `> 1` ⇒ 播种出来的 5 也算「修好」）
                int repaired = 0;
                var stillBad = new List<string>();
                var stillBadIds = new List<string>();
                for (int i = 0; i < bad.Count; i++)
                {
                    var info = bag.Get(bad[i]);
                    int now = info != null ? info.Amount : -1;
                    int expected = expectedById[bad[i]];
                    if (now >= expected) repaired++;
                    else
                    {
                        stillBad.Add(bad[i] + "=" + now + "（期望 " + expected + "，原 " + before[bad[i]] + "）");
                        stillBadIds.Add(bad[i]);
                    }
                }

                if (repaired == 0)
                {
                    RestoreUnfixedAmounts(bag, stillBadIds, before, stillBad);   // 内存也回到迁移前（写盘本来就不做）
                    UnityEngine.Debug.LogError("[Inventories] `B-84` 迁移：**一条都没修好**（播种口径给的值仍低于基准量）"
                        + " ⇒ **不写盘**、原档一个字节没改。明细：" + string.Join(", ", stillBad.ToArray()));
                    _cardsAmountMigrationDone = true;
                    LastCardAmountMigrationReport = sb.Append("；一条都没修好 ⇒ 不动").ToString();
                    return false;
                }

                // ⑥·b 🆕 `B-161`：**没修好的那几条必须恢复成迁移前的值**再落盘。
                //    `InventoryStore.Save` 写的是整份 `SnapshotAmounts()`（`Inventory.cs:261-267`）⇒ 不还原的话，
                //    那个「仍不对但 > 1」的值（例：`_owned` 缓存里的 5）会趁**别的卡被修好**的这一次 Save **静默写进盘**。
                //    还原不了（条目在背包里没了 / 迁移前就没有）⇒ **整次不写盘**（宁可这轮不修，也不许毁数据）。
                if (!RestoreUnfixedAmounts(bag, stillBadIds, before, stillBad))
                {
                    _cardsAmountMigrationDone = true;
                    LastCardAmountMigrationReport = sb.Append("；仍不对的那条恢复不了 ⇒ 不动").ToString();
                    return false;
                }

                // ⑦ 落盘（旧档 + 中央档，走既有的 `SaveThroughLegacy`；⛔ 不自己写文件）
                bool saved = InventoryStore.Save(CardProgress.InventoryName, bag);
                if (!saved)
                    UnityEngine.Debug.LogError("[Inventories] `B-84` 迁移：**落盘失败**（" + (InventoryStore.LastStoreError ?? "见上一条日志")
                        + "）⇒ 内存里已修好（本次运行可见），但盘上仍是旧的；备份在："
                        + (legacyBak ?? "<无>") + (centralBak != null ? " / " + centralBak : ""));

                sb.Append("；修好 ").Append(repaired).Append(" 条")
                  .Append(saved ? "，已落盘" : "，**落盘失败**");
                if (stillBad.Count > 0)
                {
                    sb.Append("；").Append(stillBad.Count).Append(" 条仍不对（不动）：")
                      .Append(string.Join(", ", stillBad.ToArray()));
                    UnityEngine.Debug.LogError("[Inventories] `B-84` 迁移：这些 id 播种后**仍低于基准量**（已恢复成迁移前的值，"
                        + "**不动**，⛔ 不猜）：" + string.Join(", ", stillBad.ToArray()));
                }
                sb.Append("；备份 = ").Append(legacyBak ?? "<旧档不存在>");
                if (centralBak != null) sb.Append(" / ").Append(centralBak);

                LastCardAmountMigrationReport = sb.ToString();
                UnityEngine.Debug.Log("[Inventories] `B-84` 迁移完成：" + LastCardAmountMigrationReport);
                _cardsAmountMigrationDone = true;
                return saved;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[Inventories] `B-84` 迁移抛异常（**整次放弃**；备份若已生成见上一条日志）：" + e);
                LastCardAmountMigrationReport = "抛异常：" + e.GetType().Name + "：" + e.Message;
                _cardsAmountMigrationDone = true;                    // 抛过一次就别在同一进程里反复试
                return false;
            }
            finally { _cardsAmountMigrating = false; }
        }

        /// <summary>🆕 2026-11-01 `B-161`：把「复核后仍不对」的那几条**恢复成迁移前的值**（内存与「不写盘」口径一致）。
        /// <para>为什么两处都要调：① 有别的卡被修好要落盘时 —— `InventoryStore.Save` 写的是整份
        /// `SnapshotAmounts()`（`Inventory.cs:261-267`），不还原就等于把这个仍不对的值（例：缓存里的 5）**跟着写进盘**；
        /// ② 「一条都没修好 ⇒ 不写盘」那条分支 —— 内存若留在播种值上，**下一次任何 Save**（`AddOwned` 等）照样会把它带走。</para>
        /// <returns>`true` = 全都还原好了；`false` = 有条目在背包里没了 / 迁移前就没有值（⇒ 调用方**整次不写盘**）。</returns></summary>
        static bool RestoreUnfixedAmounts(Inventory<RuleEngine.CardDef> bag, List<string> ids,
                                          Dictionary<string, int> before, List<string> details)
        {
            bool ok = true;
            for (int i = 0; i < ids.Count; i++)
            {
                var entry = bag.Get(ids[i]);
                int prev;
                if (entry == null || !before.TryGetValue(ids[i], out prev) || prev < 0)
                {
                    UnityEngine.Debug.LogError("[Inventories] `B-84` 迁移：仍不对的 `" + ids[i]
                        + "` 在背包里**恢复不了**（条目=" + (entry == null ? "<无>" : "有") + "）⇒ **整次迁移不写盘**。明细："
                        + string.Join(", ", details.ToArray()));
                    ok = false;
                    continue;
                }
                entry.SetAmount(prev);
            }
            return ok;
        }

        /// <summary>🆕 2026-11-01 `B-161`：这张卡**应当**有多少张（= 播种那句会给出的基准量）。
        /// <para>式子与 `CardProgress.Owned` 的兜底分支（`Shell/CardDetailPopup.cs:247`）**逐字同一句**：
        /// <c>DeckCap(rarity) + (NeedCopies[MaxLevel] - NeedCopies[1])</c>（全解锁「给足」口径，恒 ≥ 10）。</para>
        /// <para>🔴 **为什么不直接调 `CardProgress.Owned(c.Id, c.Rarity)` 求期望值**（那样更省事）：
        /// 它对「背包里没有的卡」**先查 `_owned` 缓存**（`CardDetailPopup.cs:243-249`）；而迁移第 ⑤ 步正是
        /// 「先摘掉条目、再播种」⇒ 那一刻 `Owned` 回的就是缓存里那个**陈旧 5** ⇒ `现在(5) >= 期望(5)` 恒真、
        /// 判据重新变成**恒绿**（= 本就要堵的那个洞）。只有这一式子绕得开缓存。</para>
        /// <para>⚠️ 这是**同一式子的第二处副本**（不是第二套算法）：唯一权威仍是 `CardProgress.Owned` 的兜底分支，
        /// 那边改口径这里必须跟着改。要彻底消掉这份副本，得给 `CardProgress` 开一个「不含 `_owned` 缓存的基准量」
        /// 公开口 —— `_owned` 是 `private static`（`CardDetailPopup.cs:133`），本次白名单不含那个文件 ⇒ 没开（见报告）。</para>
        /// <para>⚠️ 刻意走**不带卡型**的 `DeckCap(rarity)`：与播种那句 `Owned(c.Id, c.Rarity)` 同档
        /// （那一条也吃不到 `CardDef.Type`）—— 否则督军那张「期望 10 / 播种实际给 11」差 1，会误判成没修好。</para></summary>
        static int ExpectedCardAmount(RuleEngine.CardDef c)
        {
            return CardProgress.DeckCap(c.Rarity)
                 + (CardProgress.NeedCopies[CardProgress.MaxLevel] - CardProgress.NeedCopies[1]);
        }

        /// <summary>把一个档复制成 `<档>.B112G-bak-<stamp>`（**名字撞了就在末尾加 `-2` / `-3` …**，见下）。
        /// <returns>`true` = 备份好了**或者本来就没有那个文件**（没有可回滚的旧档 ⇒ 不需要备份）；
        /// `false` = 文件在、但复制失败。</returns></summary>
        static bool BackupOne(string path, string stamp, out string backupPath)
        {
            backupPath = null;
            if (string.IsNullOrEmpty(path)) return true;
            try
            {
                if (!System.IO.File.Exists(path)) return true;
                // 🔴 2026-11-01（`B115_D3` 诊断）：名字必须**唯一** —— `stamp` 只到**秒**，同一秒里第二次迁移
                //    （自检 O-4 → O-5 就是同一目录、同一秒连跑两次）会撞上**上一份备份** ⇒ `File.Copy(overwrite:false)`
                //    抛 ⇒ 走「备份失败 ⇒ 整次迁移不动」⇒ **该修的那一次静默不修**（`B113_G3` 的 O-5 / O-5b 两条红）。
                //    ⛔ 仍然**不覆盖**旧备份（覆盖 = 悄悄毁掉上一份回滚点）：撞名就加序号。
                string root = path + MigrationBackupSuffix + stamp;
                backupPath = root;
                for (int i = 2; i <= 100 && System.IO.File.Exists(backupPath); i++)
                    backupPath = root + "-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (System.IO.File.Exists(backupPath))
                {
                    UnityEngine.Debug.LogError("[Inventories] `B-84` 迁移：备份名被占满（" + root + "-2…-100 都存在）⇒ 备份失败");
                    backupPath = null;
                    return false;
                }
                System.IO.File.Copy(path, backupPath, false);        // 不覆盖（上面已保证名字空着；并发撞名仍会抛 ⇒ 出声）
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[Inventories] `B-84` 迁移：备份**失败**（" + path + " → " + backupPath + "）：" + e.Message);
                backupPath = null;
                return false;
            }
        }
    }
}
