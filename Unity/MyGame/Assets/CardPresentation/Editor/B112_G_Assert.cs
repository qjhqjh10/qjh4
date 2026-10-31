// B112_G_Assert.cs — 执行代理 `B112_G`（2026-11-01 · 第一百一十一会话第 2 波）的断言：账 `B-84`
//
// ============================ 它盯的是什么 ============================
//  `B-14` 只堵住**未来**的坏写入（`CardInventory.Add` 先播种）；**已经写坏的盘上档不会自愈** ——
//  `InventoryStore.Load` 的 `RestoreAmounts` 会把 `cards.json` 里那个 `1` 原样灌回背包。
//  本件的被测物 = `Inventories.MigrateBadCardAmounts(...)`（`Shell/InventoryPartial_B112G.cs`），
//  它挂在 `InventoryStore.Load` 的 `inv.RestoreAmounts(am);` **之后**、只对 `CardInventory` 生效。
//
// ============================ 判据（原版侧 / 我们的口径）============================
//  「这张卡应当有多少张」**只有一个口径**：`Inventories.SeedCardsAllUnlocked(pool, c => CardProgress.Owned(c.Id, c.Rarity))`
//  （= `Shell/CollectionWindow.cs:903` 逐字那一句）。全解锁口径下基准量 = `DeckCap(rarity) + 9` ⇒ **恒 ≥ 10**，
//  而且本工程没有任何扣卡路径（升级只做「够不够」检查、不真扣）⇒ **合法值不可能落到 1**。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · **A2/A3（迁移的真正判别力）**：手上先造出一份**确实含 `amount:1` 的盘上档**（A1 先断前提「文件里真的是 1」），
//    再走一次**生产入口** `InventoryStore.Load`。
//      🧨 把 `Inventory.cs` 里 `if (inv is CardInventory) Inventories.MigrateBadCardAmounts(am);` 删掉
//         ⇒ `RestoreAmounts` 把 1 灌回来、没有任何东西把它改掉 ⇒ A2 红（值停在 1）。
//      🧨 把迁移写成「补到 n」（`n = CardProgress.Owned`）= 补了等于没补 ⇒ A2 照样红。
//  · **D0（跑得起来的灭自证）**：**绕过 `Load`、直接 `RestoreAmounts` 一份含 1 的表** ⇒ 条目**仍是 1**
//    ⇒ 证明「修好」不是别的路径顺手做的，就是迁移那一步。
//  · **B5（防「顺手改坏」）**：一份本来就正确的档，`Load` 之后**文件逐字节不变**
//      🧨 改成「每次 Load 都无条件 `Save` 一遍」⇒ B5 红（`JsonUtility` 重排 + 字典序都可能变）。
//  · **B6**：正确档**连备份都不该生成**（没写盘就没有备份）⇒ 🧨 无条件备份 ⇒ B6 红。
//
// ============================ 怎么接（⚠️ 本代理不动断言宿主）============================
//  自包含（不建窗、不依赖 UI）。**建议接在某个宿主聚合行的最末尾**（`//` 之前、同一物理行）：
//      B112_G_Assert.Run(); _sink.Pass += B112_G_Assert.Pass; _sink.Fail += B112_G_Assert.Fail;
//  ⚠️ 本件**不调** `CardProgress.ResetForTest()`（那会永久关掉落盘、影响后面的断言）；
//    改成「反射快照 → 临时摆 → finally 逐项还原」。
//
// ============================ 副作用/恢复 ============================
//  · 全程 `InventoryStore.OverrideDir` = `%TEMP%/wf_b112g/…` 下的一次性目录；`SaveCenter.OverrideRoot` 临时置 null
//    （⇒ 中央档那一份不参与，自检只覆盖旧档 —— 生产两遍走的都是 `SaveCenter.SaveThroughLegacy`，形状相同）。
//  · `CardProgress._owned` / `_loaded` / `_persist`、卡背包全量快照 —— **finally 里逐项还原**。⛔ 不碰玩家真存档。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B112_G`：`B-84` 一次性迁移 —— 盘上「数量 1」的卡条目按唯一播种口径重算（幂等 · 可回滚 · 正确档一字节不动）。</summary>
public static class B112_G_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B112G] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>被测的卡 id（`UM2` = C9 / 收藏线一直在用的那张，`CollectionData.Card` 认得）。</summary>
    const string Id = "UM2";

    static readonly string InvName = CardProgress.InventoryName;

    // `CardProgress` 的三个私有静态位（只读、只在这里摆一下，finally 全部还原）
    static readonly FieldInfo F_Owned   = typeof(CardProgress).GetField("_owned",   BindingFlags.NonPublic | BindingFlags.Static);
    static readonly FieldInfo F_Loaded  = typeof(CardProgress).GetField("_loaded",  BindingFlags.NonPublic | BindingFlags.Static);
    static readonly FieldInfo F_Persist = typeof(CardProgress).GetField("_persist", BindingFlags.NonPublic | BindingFlags.Static);

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B112_G · `B-84` 一次性迁移：盘上「数量 1」的卡条目按**唯一播种口径**重算（幂等 · 可回滚 · 正确档一字节不动）");

        var bag = Inventories.Cards;
        Tr(bag != null, "0 **前提**：`Inventories.Cards` 在（拥有数的正本）");
        if (bag == null) return;

        var def = CollectionData.Card(Id);
        Tr(def != null, "0b **前提**：卡池认得 `" + Id + "`（`CollectionData.Card`）");
        if (def == null) return;

        var pool = RuleEngine.CardDatabase.Load();
        Tr(pool != null && pool.Count > 0, "0c **前提**：卡池非空（`CardDatabase.Load()`，= `CardsState.Pool` 的同一份来源）");
        if (pool == null || pool.Count == 0) return;

        // 尺子：另一张**同稀有度**的卡（播种基准量只按稀有度走 ⇒ 它的量就是被测卡「本该」的量）
        RuleEngine.CardDef ruler = null;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null || string.IsNullOrEmpty(c.Id) || c.Id == Id) continue;
            if (c.Rarity != def.Rarity) continue;
            ruler = c; break;
        }
        Tr(ruler != null, "0d **前提**：卡池里找得到一张与 `" + Id + "` **同稀有度**的对照卡（`" + def.Rarity + "`）");
        if (ruler == null) return;

        // 🔴 **真档保护闸**：下面要把 `SaveCenter.OverrideRoot` 置 null（⇒ 中央档不参与自检）。
        //   若这一进程本来就「在真的一局里」（`isPlaying && !isBatchMode`），`SaveCenter.StoreOn` 会是 true
        //   ⇒ `InventoryStore.Save` 会把中央档写进**玩家的真存档目录**。那种情况下整组不跑（⛔ 不许污染真档）。
        Tr(!SaveCenter.StoreOn, "0e **前提**：`SaveCenter.StoreOn` 关着（批处理 / 编辑器非 Play）—— 否则自检会写进玩家的真中央档");
        if (SaveCenter.StoreOn) return;

        string root = Path.Combine(Path.GetTempPath(), "wf_b112g", DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
        string prevDir = InventoryStore.OverrideDir;
        string prevRoot = SaveCenter.OverrideRoot;
        Dictionary<string, int> keptBag = bag.SnapshotAmounts();
        object ownedObj = F_Owned != null ? F_Owned.GetValue(null) : null;
        Dictionary<string, int> keptOwned = ownedObj as Dictionary<string, int> != null
            ? new Dictionary<string, int>((Dictionary<string, int>)ownedObj) : new Dictionary<string, int>();
        object keptLoaded = F_Loaded != null ? F_Loaded.GetValue(null) : null;
        object keptPersist = F_Persist != null ? F_Persist.GetValue(null) : null;

        try
        {
            Directory.CreateDirectory(root);
            SaveCenter.OverrideRoot = null;                 // 中央档那一份不进自检（只验旧档；生产两遍同形）
            if (F_Owned != null && ownedObj is Dictionary<string, int>) ((Dictionary<string, int>)ownedObj).Clear();   // 清掉旧的 `_owned` 缓存 ⇒ `Owned` 只走「给足」公式
            if (F_Loaded != null) F_Loaded.SetValue(null, true);      // 跳过 `WarpforgeCardLevels.json` 读盘
            if (F_Persist != null) F_Persist.SetValue(null, false);   // `CardProgress.Save()` 变空操作（⛔ 不碰玩家真档）

            // 背包先归到「干净的全解锁态」（没有任何数量 = 1 的条目）
            bag.ClearForTest();
            Inventories.SeedCardsAllUnlocked(pool, c => CardProgress.Owned(c.Id, c.Rarity));

            TrySection("A 坏档被修好", () => SectionA_RepairsBadSave(bag, def, ruler, Path.Combine(root, "a")));
            TrySection("B 正确档逐字节不变", () => SectionB_CorrectSaveUntouched(bag, Pool(), Path.Combine(root, "b")));
            TrySection("C 幂等（连跑两次结果相同）", () => SectionC_Idempotent(bag, def, ruler, Path.Combine(root, "c")));
            TrySection("D 灭自证（`RestoreAmounts` 本身不会修）", () => SectionD_SelfProof(bag, def, ruler, Path.Combine(root, "d")));
        }
        catch (Exception e) { Tr(false, "B112_G 夹具抛异常：" + e.GetType().Name + "：" + e.Message); }
        finally
        {
            try
            {
                if (F_Loaded != null) F_Loaded.SetValue(null, keptLoaded);
                if (F_Persist != null) F_Persist.SetValue(null, keptPersist);
                InventoryStore.OverrideDir = prevDir;
                SaveCenter.OverrideRoot = prevRoot;
                Inventories.ResetCardAmountMigrationForTest();

                // 卡背包还原：`RestoreAmounts` 的契约是「已在的改、不在的跳过」⇒ **先播种、再灌快照**
                bag.ClearForTest();
                Inventories.SeedCardsAllUnlocked(Pool(), c => CardProgress.Owned(c.Id, c.Rarity));
                bag.RestoreAmounts(keptBag);

                // 🔴 `_owned` 必须放在**背包还原之后**再摆回去：上面那次播种会对「不在背包里的卡」写 `_owned` 缓存，
                //    先摆回来的话会被这一步弄脏（影响排在后面的断言）。
                if (F_Owned != null && ownedObj is Dictionary<string, int>)
                {
                    var d = (Dictionary<string, int>)ownedObj;
                    d.Clear();
                    foreach (var kv in keptOwned) d[kv.Key] = kv.Value;
                }
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch (Exception e) { Debug.LogWarning("[B112G] 临时目录没删掉（" + root + "）：" + e.Message); }
                Debug.Log("[B112G] 收尾：卡背包已按快照还原（" + keptBag.Count + " 条）· 迁移位已复位 · 临时目录已清");
            }
            catch (Exception e) { Debug.LogError("[B112G] 收尾失败：" + e); }
        }

        Debug.Log(_sink.P + "B112_G 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    static RuleEngine.CardDef[] Pool() { return RuleEngine.CardDatabase.Load().ToArray(); }

    static void TrySection(string name, Action body)
    {
        try { body(); }
        catch (Exception e) { Tr(false, "「" + name + "」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
    }

    // ============================================================ A · 坏档被修好
    static void SectionA_RepairsBadSave(CardInventory bag, RuleEngine.CardDef def, RuleEngine.CardDef ruler, string dir)
    {
        MenuCheck.Section(_sink, "A · 盘上「数量 = 1」的条目 ⇒ 走生产入口 `InventoryStore.Load` 之后**不再是 1**");

        Tr(NoSentinelInBag(bag), "A0 **前提**：干净态下背包里**没有任何**数量 = 1 的条目（否则 A2「!= 1」会自证）");

        Directory.CreateDirectory(dir);
        InventoryStore.OverrideDir = dir;
        InventoryStore.Delete(InvName);

        // ① 造坏档：把被测卡设成 1，写盘；再**从文件里读回来**确认「盘上真的是 1」（不是只有内存是 1）
        var info = bag.Get(def);
        Tr(info != null, "A1a **前提**：背包里有 `" + Id + "` 的条目（播种给的）");
        if (info == null) return;
        info.SetAmount(Inventories.BadCardAmountSentinel);
        Tr(InventoryStore.Save(InvName, bag), "A1b **前提**：坏档写下去了（`InventoryStore.Save` 回 true）");

        string path = InventoryStore.StorePath(InvName);
        string text0 = File.Exists(path) ? File.ReadAllText(path) : null;
        // ⚠️ 必须连**逗号**一起比：`"amount":1` 是 `"amount":10` 的前缀，单比前缀会在「本来就是 10」时假绿
        Tr(text0 != null && text0.Contains("{\"k\":\"" + Id + "\",\"amount\":1,"),
           "A1 **前提**：盘上那份 `" + InvName + ".json` 里 `" + Id + "` 的 `amount` **确实是 1**（坏档造出来了）");

        // ② 走**生产入口**（不是直接调迁移 —— 那才是玩家真会走的路径）
        Inventories.ResetCardAmountMigrationForTest();
        int rulerBefore = RulerAmount(bag, ruler);
        InventoryStore.Load(InvName, bag);

        var after = bag.Get(def);
        int now = after != null ? after.Amount : -1;
        Tr(now != 1, "A2 ★★ **迁移后不再是 1**（实得 " + now + "）"
                    + "｜🧨 删掉 `Inventory.cs` 里 `if (inv is CardInventory) Inventories.MigrateBadCardAmounts(am);`"
                    + " ⇒ `RestoreAmounts` 把 1 灌回来、没人改它 ⇒ 红");
        C(now, RulerAmount(bag, ruler), "A3 ★★ 修好的值 = **同稀有度对照卡的量**（= 唯一播种口径给的基准量，" + now + " vs " + RulerAmount(bag, ruler) + "）"
            + "｜🧨 改成「补到 n」（`n = Owned` = 那个 1）⇒ 值停在 1 ⇒ 红");

        var baks = Directory.GetFiles(dir, "*" + Inventories.MigrationBackupSuffix + "*");
        Tr(baks.Length >= 1, "A4 **可回滚**：迁移前留下了备份（" + baks.Length + " 份："
            + (baks.Length > 0 ? Path.GetFileName(baks[0]) : "<无>") + "）"
            + "｜🧨 去掉备份那一段 ⇒ 红");
        Tr(!string.IsNullOrEmpty(Inventories.LastCardAmountMigrationReport)
           && Inventories.LastCardAmountMigrationReport.Contains("修好 1 条"),
           "A5 迁移报告写明「修好 1 条」且带备份路径：" + (Inventories.LastCardAmountMigrationReport ?? "<null>"));

        Tr(rulerBefore == RulerAmount(bag, ruler), "A6 对照卡（没被写坏的那条）**一个字节都没动**（" + rulerBefore + " → " + RulerAmount(bag, ruler) + "）"
            + "｜🧨 迁移顺手把「已经正确的条目」也重算/改掉 ⇒ 红（这条只挡得住「改成别的值」，改成同值仍绿 —— 由 B5 兜住）");
    }

    // ============================================================ B · 正确档逐字节不变
    static void SectionB_CorrectSaveUntouched(CardInventory bag, RuleEngine.CardDef[] pool, string dir)
    {
        MenuCheck.Section(_sink, "B · 本来就正确的档 ⇒ `Load` 之后**逐字节相同**（防「顺手改坏」）");

        Tr(NoSentinelInBag(bag), "B0 **前提**：这份档里没有任何数量 = 1 的条目（= 「本来就正确」的定义）");

        Directory.CreateDirectory(dir);
        InventoryStore.OverrideDir = dir;
        InventoryStore.Delete(InvName);

        Tr(InventoryStore.Save(InvName, bag), "B1 **前提**：正确档写下去了");
        string path = InventoryStore.StorePath(InvName);
        byte[] bytes0 = File.ReadAllBytes(path);

        var snap = bag.SnapshotAmounts();
        Inventories.ResetCardAmountMigrationForTest();
        InventoryStore.Load(InvName, bag);

        bool allSame = true;
        foreach (var kv in snap)
        {
            var it = bag.Get(kv.Key);
            if (it == null || it.Amount != kv.Value) { allSame = false; break; }
        }
        Tr(allSame, "B2 读回来之后**每一条数量都和写盘前一模一样**（" + snap.Count + " 条）");

        byte[] bytes1 = File.ReadAllBytes(path);
        Tr(bytes0.Length == bytes1.Length, "B3 文件长度不变（" + bytes0.Length + " → " + bytes1.Length + "）");
        bool identical = bytes0.Length == bytes1.Length;
        if (identical)
            for (int i = 0; i < bytes0.Length; i++) if (bytes0[i] != bytes1[i]) { identical = false; break; }
        Tr(identical, "B4 ★★ **文件逐字节完全相同**（" + bytes0.Length + " 字节）"
                    + "｜🧨 改成「每次 Load 都无条件 `Save`」⇒ 重排/重写 ⇒ 红");

        var baks = Directory.GetFiles(dir, "*" + Inventories.MigrationBackupSuffix + "*");
        C(baks.Length, 0, "B5 ★ 正确档**连备份都没生成**（没写盘 ⇒ 没备份；实得 " + baks.Length + " 份）"
            + "｜🧨 无条件先备份再决定 ⇒ 红");

        // 泛型里那个 `pool` 参数只为「形状对齐」保留（本组不需要它），显式用一次避免「未使用」告警
        Tr(pool != null && pool.Length > 0, "B6 **前提**：卡池非空（" + (pool != null ? pool.Length : 0) + " 张）");
    }

    // ============================================================ C · 幂等
    static void SectionC_Idempotent(CardInventory bag, RuleEngine.CardDef def, RuleEngine.CardDef ruler, string dir)
    {
        MenuCheck.Section(_sink, "C · **幂等**：同一份坏档连跑两次 `Load`，结果相同（第二遍一个字节都不写）");

        Directory.CreateDirectory(dir);
        InventoryStore.OverrideDir = dir;
        InventoryStore.Delete(InvName);

        var info = bag.Get(def);
        Tr(info != null, "C0 **前提**：背包里有 `" + Id + "` 的条目");
        if (info == null) return;
        info.SetAmount(Inventories.BadCardAmountSentinel);
        Tr(InventoryStore.Save(InvName, bag), "C0b **前提**：坏档写下去了");

        string path = InventoryStore.StorePath(InvName);

        Inventories.ResetCardAmountMigrationForTest();
        InventoryStore.Load(InvName, bag);
        int once = bag.Get(def) != null ? bag.Get(def).Amount : -1;
        Tr(once != 1, "C1 第一遍修好了（" + once + "）");
        byte[] bytes1 = File.ReadAllBytes(path);
        int baks1 = Directory.GetFiles(dir, "*" + Inventories.MigrationBackupSuffix + "*").Length;

        Inventories.ResetCardAmountMigrationForTest();       // 清「本进程已跑过」这一位 ⇒ 第二遍会**真的重新判一次**
        InventoryStore.Load(InvName, bag);
        int twice = bag.Get(def) != null ? bag.Get(def).Amount : -1;
        C(twice, once, "C2 ★ 第二遍之后数量**完全相同**（幂等）");
        byte[] bytes2 = File.ReadAllBytes(path);
        bool same = bytes1.Length == bytes2.Length;
        if (same) for (int i = 0; i < bytes1.Length; i++) if (bytes1[i] != bytes2[i]) { same = false; break; }
        Tr(same, "C3 ★ 第二遍**一个字节都没写**（文件逐字节相同，" + bytes1.Length + " 字节）"
               + "｜🧨 把「没有坏条目就不写盘」那道闸去掉 ⇒ 重写一遍 ⇒ 红");
        C(Directory.GetFiles(dir, "*" + Inventories.MigrationBackupSuffix + "*").Length, baks1,
          "C4 第二遍**没有再多出一份备份**（" + baks1 + " → 现在的份数）");

        Tr(bag.Get(ruler) != null, "C5 对照卡仍在（" + (bag.Get(ruler) != null ? bag.Get(ruler).Amount : -1) + "）");
    }

    // ============================================================ D · 灭自证
    static void SectionD_SelfProof(CardInventory bag, RuleEngine.CardDef def, RuleEngine.CardDef ruler, string dir)
    {
        MenuCheck.Section(_sink, "D · 灭自证：**`RestoreAmounts` 自己不会修** —— 修好的一定是迁移那一步");

        Directory.CreateDirectory(dir);
        InventoryStore.OverrideDir = dir;
        InventoryStore.Delete(InvName);

        var info = bag.Get(def);
        Tr(info != null, "D0 **前提**：背包里有 `" + Id + "` 的条目");
        if (info == null) return;

        var disk = new Dictionary<string, int>();
        disk[Id] = Inventories.BadCardAmountSentinel;

        // ① 只走 `RestoreAmounts`（Load 里的另一半）⇒ 1 原样留下
        info.SetAmount(5);
        bag.RestoreAmounts(disk);
        C(bag.Get(def) != null ? bag.Get(def).Amount : -1, 1,
          "D1 ★★ 只调 `RestoreAmounts` ⇒ **仍是 1**（= 「修好」不可能来自这一步）"
          + "｜🧨 若有人把迁移的逻辑挪进 `RestoreAmounts`，这条会变绿并**在别处（快照还原）**炸 —— 提醒作用");

        // ② 再调一次迁移 ⇒ 变成基准量
        Inventories.ResetCardAmountMigrationForTest();
        bool changed = Inventories.MigrateBadCardAmounts(disk);
        Tr(changed, "D2 ★★ `MigrateBadCardAmounts` 真的改了盘（回 true）");
        int fixedNow = bag.Get(def) != null ? bag.Get(def).Amount : -1;
        Tr(fixedNow > 1, "D3 ★★ 调过迁移之后 **> 1**（实得 " + fixedNow + "）");
        C(fixedNow, RulerAmount(bag, ruler), "D4 值 = 同稀有度对照卡的基准量（" + fixedNow + " vs " + RulerAmount(bag, ruler) + "）");

        // ③ 再调一次（幂等位已清，但盘上已没有 1）⇒ 不改、不写
        // ⚠️ 2026-11-01 `B115_D3` 订正：必须**重新取「盘上那份」** —— ② 的迁移里 `InventoryStore.Save`
        //    已把档重写成修好后的值（`Save` 写的就是 `SnapshotAmounts()`，`Inventory.cs:419`）。
        //    ⛔ 不能再传上面那份陈旧的 `disk`（它还写着 1）⇒ 那会让迁移再修一遍并落盘、回 true。
        Inventories.ResetCardAmountMigrationForTest();
        var diskNow = bag.SnapshotAmounts();
        Tr(!Inventories.MigrateBadCardAmounts(diskNow), "D5 再调一次 ⇒ 回 `false`（没坏条目 ⇒ 不动）");
        C(bag.Get(def) != null ? bag.Get(def).Amount : -1, fixedNow, "D6 数量不再变化（幂等）");
    }

    // ============================================================ 小工具
    static int RulerAmount(CardInventory bag, RuleEngine.CardDef ruler)
    {
        var it = bag.Get(ruler);
        return it != null ? it.Amount : -1;
    }

    /// <summary>背包里有没有「数量 = 1」的条目（照 `BadCardAmountSentinel`）。</summary>
    static bool NoSentinelInBag(CardInventory bag)
    {
        foreach (var it in bag.GetItems())
            if (it != null && it.Amount == Inventories.BadCardAmountSentinel) return false;
        return true;
    }
}
