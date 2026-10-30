// B104_P3_Assert.cs — 第一百零四会话 · 执行代理 `B104_P3`（2026-10-31 · 「写盘返回值被丢弃 / 失败仍报成功」这一族）
//
// ============================ 它盯的是什么 ============================
// 七处「调了会返回 bool 的落盘口、但**返回值被丢掉**」的调用点，加上一处同族清理：
//   P3-1 `Shell/ExpansionPassEvent.cs`：`Score()` / `PurchaseMilestone()` / `PurchaseLevelUp()` 三条（原来 `Save.Save()` 的 bool 丢）
//   P3-2 `Shell/CampaignData.cs`      ：`SaveToSave()`（`SaveCenter.Set` 的 bool 丢）→ `Claim()` / `Select()`
//   P3-3 `Shell/CardDetailPopup.cs`   ：`CardProgress.Save()`（`void`！）/ `AddOwned()` / `TrySpendWildcard()`
//   P3-4 `Shell/ShopData.cs`          ：`PersistShop()`（`InventoryStore.Save` 的 bool 丢）→ `Buy()`
//   P3-5 `Shell/GameModesHandler.cs`  ：`SavePracticeMatchStarted()` 的三处调用点
//   P3-6 `Shell/LocalPlayerData.cs`   ：`SavePlayerData()` 的两处调用点
//   P3-7 `Shell/WindowsManager.cs`    ：`win.TryOpen(data)` 的 bool 丢、窗仍被记成「已打开」
//
// 判据 / 背景 → `资料/完全复刻/报告/B104_P3.md`。
//
// ============================ 失败是怎么**造出来**的 ============================
// 全部靠**同一招**：把某个落盘口指到一个**存在但其实是文件**的路径（`blocker_is_a_file`）——
//   `Directory.CreateDirectory(<那是个文件>)` / `File.WriteAllText(<那是个目录>)` 都会抛 ⇒ 落盘口按它自己的
//   try/catch 回 `false`，而**落盘门（`StoreOn` / `PersistOn`）仍然是「开着」** ⇒ 正好是「真失败」那一档。
// ⛔ 绝不能拿「落盘门关着」当失败（那是批处理 / 自检的设计性空操作）—— 那两档本件**分开**断（P3-1e / P3-3d / P3-5c）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   `B104_P3_Assert.CheckB104P3();`
//   · **不需要**开窗、不需要开局；只吃静态件 + `new GameObject()`（P3-7 那一件）。
//   · 临时目录 = `%TEMP%/wf_b104p3_<guid>`，收尾整个删掉；所有 `Override*` 静态口都**原样还回去**。
//   · ⚠️ **环境前提**：本件要在**批处理**里跑（P3-1e / P3-5c 断的是「没在真跑 ⇒ 落盘门关着」）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   · 把任意一处「判返回值」改回「丢掉」⇒ 对应的 `★` 条红（失败支会去走成功支 / 状态不回滚）。
//   · 把「失败 ⇒ 撤销内存改动」删掉 ⇒ 对应的 `'` 条红（`TotalScore` / `BaseClaimed` / `Wildcards` / `OwnedOf` 停在改后的值）。
//   · 把「落盘门关着也算成功」写成「关着也算失败」⇒ **P3-1e / P3-3d / P3-5c** 红。
//   · 把 `Buy` 的失败哨兵从 `PurchaseSucceeded` 里漏掉 ⇒ **P3-4a'** 红。
//   · 把 `GameWindow.OpenByState` 的 `this == null` 短路删掉 ⇒ **P3-7b** 红（那条短路正是 `OpenWindow` 撤销登记的依据）。
using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B104_P3`：「写盘返回值被丢弃」这一族的**结构断言**。
/// <para>⛔ 期望值全是**字面量**（0 / 1 / true / false），不从被测实现里反算。</para></summary>
public static class B104_P3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[P3] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    /// <summary>本件唯一入口（宿主 `Run()` 里调一次；⛔ 不用 `Test` 前缀，同 `B97W3_Assert` 的理由）。</summary>
    public static void CheckB104P3()
    {
        MenuCheck.Section(_sink, "P3：写盘返回值不再被丢弃（真失败 ⇒ 不报成功；落盘门关着 ⇒ 不算失败）");
        string root = NewTempDir();
        try
        {
            CheckExpansionPass(root);
            CheckCampaign(root);
            CheckCardProgress(root);
            CheckShop(root);
            CheckGameModes(root);
            CheckLocalPlayerData(root);
            CheckWindowsManager();
        }
        finally { TryDeleteDir(root); }
        Debug.Log(_sink.P + "P3 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具
    static string NewTempDir()
    {
        string d = Path.Combine(Path.GetTempPath(), "wf_b104p3_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }

    /// <summary>造一个「存在、但其实是**文件**」的路径。任何把它当目录 `CreateDirectory` 的落盘口都会抛 ⇒ 稳定注入失败。</summary>
    static string BlockerFile(string root)
    {
        string f = Path.Combine(root, "blocker_is_a_file");
        File.WriteAllText(f, "x");
        return f;
    }

    static void TryDeleteDir(string d)
    {
        try { if (Directory.Exists(d)) Directory.Delete(d, true); }
        catch (Exception e) { Debug.LogWarning("[P3] 临时目录没删掉（" + d + "）：" + e.Message); }
    }

    // ============================================================ P3-1 ExpansionPassEvent
    static void CheckExpansionPass(string root)
    {
        MenuCheck.Section(_sink, "P3-1 ExpansionPassEvent：Score / PurchaseMilestone / PurchaseLevelUp 的落盘失败");
        string prev = ExpansionPassSave.OverrideDir;
        try
        {
            string blocked = BlockerFile(root);
            ExpansionPassSave.OverrideDir = blocked;
            Tr(ExpansionPassSave.PersistOn, "P3-1 前提：落盘门**开着**（`OverrideDir` 非空）⇒ 下面那次 false 是**真失败**");

            var save = new ExpansionPassSave();
            var ev = new ExpansionPassEvent(save, new ExpansionPassData { XpPerGold = 10 });
            ev.Milestones.Add(new ExpansionMilestone(100, 1, false));

            // ---- a) Score
            Tr(!ev.Score(50), "P3-1a ★ `Score()` 在写盘失败时回 **false**（原来是 `void` + `Save.Save()` 的 bool 丢掉）");
            C(save.TotalScore, 0, "P3-1a' 那一拍**已撤销**：`TotalScore` 仍是 0（原来会停在 50 = 内存与盘不一致）");

            // ---- b) PurchaseMilestone（失败）
            save.TotalScore = 100;
            bool completed = false, errored = false; string why = null;
            ev.PurchaseMilestone(0, _ => completed = true, e => { errored = true; why = e; });
            Tr(!completed, "P3-1b ★★ 写盘失败 ⇒ **不回调 `onComplete`**（原来照样回 ⇒ 玩家看到「已领取」、重启却没有）");
            Tr(errored, "P3-1b' 走 `onError`（失败必须说出来）");
            Tr(why != null && why.Contains("落盘"), "P3-1b'' `onError` 的文案点名「落盘」（实际：" + (why ?? "<null>") + "）");
            C(save.RewardClaimCount(0), 0, "P3-1b''' 领取计数**已撤销**（回 0）");

            // ---- c) PurchaseMilestone（成功支）
            ExpansionPassSave.OverrideDir = root;
            completed = false; errored = false;
            ev.PurchaseMilestone(0, _ => completed = true, _ => errored = true);
            Tr(completed, "P3-1c 正常目录 ⇒ `onComplete` 被调（成功支没被误杀）");
            Tr(!errored, "P3-1c' 正常目录 ⇒ `onError` **不**被调");
            C(save.RewardClaimCount(0), 1, "P3-1c'' 领取计数真的落到 1");

            // ---- d) PurchaseLevelUp
            ExpansionPassSave.OverrideDir = blocked;
            var s2 = new ExpansionPassSave { TotalScore = 0 };
            var ev2 = new ExpansionPassEvent(s2, new ExpansionPassData { XpPerGold = 10 });
            ev2.Milestones.Add(new ExpansionMilestone(100, 1, false));
            completed = false; errored = false;
            ev2.PurchaseLevelUp(0, () => completed = true, _ => errored = true);
            Tr(!completed && errored, "P3-1d ★ 升级写盘失败 ⇒ `onComplete` **不调**、`onError` 调");
            C(s2.TotalScore, 0, "P3-1d' 补分**已撤销**（`TotalScore` 仍 0）");

            ExpansionPassSave.OverrideDir = root;
            completed = false; errored = false;
            ev2.PurchaseLevelUp(0, () => completed = true, _ => errored = true);
            Tr(completed && !errored, "P3-1d'' 正常目录 ⇒ 升级成功回调");
            C(s2.TotalScore, 100, "P3-1d''' 补分到阈值 100");

            // ---- e) 落盘门关着 = 设计上的空操作，⛔ 不是失败
            ExpansionPassSave.OverrideDir = null;
            C(ExpansionPassSave.PersistOn, false, "P3-1e 前提（批处理）：没注入 `OverrideDir` ⇒ 落盘门**关着**");
            Tr(ev2.Score(5), "P3-1e' ★ 门关着 ⇒ `Score()` 仍算成功（⛔ 把「没落盘」当失败会让每条批处理自检都红）");
        }
        finally { ExpansionPassSave.OverrideDir = prev; }
    }

    // ============================================================ P3-2 CampaignData
    static void CheckCampaign(string root)
    {
        MenuCheck.Section(_sink, "P3-2 CampaignData：Claim / Select 的落盘失败");
        string prevRoot = SaveCenter.OverrideRoot;
        string before = CampaignData.Selected;
        CampaignData.ResetForTest();
        try
        {
            SaveCenter.OverrideRoot = BlockerFile(root);
            Tr(SaveCenter.StoreOn, "P3-2 前提：`SaveCenter.StoreOn` 为真（`OverrideRoot` 指到阻断路径）⇒ 下面那次 false 是**真失败**");

            string why = null;
            bool ok = CampaignData.Claim(0, CampaignData.TierBasic, out why);
            Tr(!ok, "P3-2a ★ 进度写不进去 ⇒ `Claim` 回 **false**（原来 `SaveToSave()` 的 bool 被丢掉、照样回 true）");
            Tr(why != null && why.Contains("落盘"), "P3-2a' `why` 点名「落盘」（实际：" + (why ?? "<null>") + "）");
            Tr(!CampaignData.BaseClaimed(0), "P3-2a'' 标记载体**已撤销**（`BaseClaimed(0)` 仍 false ⇒ 窗上不会显示「已领取」）");

            // ---- 成功支
            SaveCenter.OverrideRoot = root;
            why = null;
            ok = CampaignData.Claim(0, CampaignData.TierBasic, out why);
            Tr(ok, "P3-2b 正常目录 ⇒ `Claim` 回 true（成功支没被误杀）");
            Tr(CampaignData.BaseClaimed(0), "P3-2b' 标记真的落了（`BaseClaimed(0)` true）");

            // ---- Select
            string other = null;
            for (int i = 0; i < CampaignData.Armies.Length; i++)
                if (CampaignData.Armies[i] != before) { other = CampaignData.Armies[i]; break; }
            if (other != null)
            {
                SaveCenter.OverrideRoot = BlockerFile(root);
                CampaignData.Select(other);
                C(CampaignData.Selected, before, "P3-2c ★ 选中落盘失败 ⇒ **退回原选中**（原来窗上换了、重启又回旧的）");
                SaveCenter.OverrideRoot = root;
                CampaignData.Select(other);
                C(CampaignData.Selected, other, "P3-2c' 正常目录 ⇒ 选中真的变了");
            }
            else Debug.Log("[P3] `CampaignData.Armies` 只有一个阵营 ⇒ P3-2c 跳过（不静默：这一行就是原因）");
        }
        finally
        {
            // 把 `Selected` 还回去（它没有公开 setter ⇒ 走 `Select`，指到可写目录才真的换得动）
            SaveCenter.OverrideRoot = root;
            CampaignData.Select(before);
            SaveCenter.OverrideRoot = prevRoot;
            CampaignData.ResetForTest();
        }
    }

    // ============================================================ P3-3 CardProgress
    static void CheckCardProgress(string root)
    {
        MenuCheck.Section(_sink, "P3-3 CardProgress：通配符扣减 / 拥有数的落盘失败");
        string prevPath = CardProgress.OverridePath;
        bool prevPersist = CardProgress.PersistOn;
        string prevInvDir = InventoryStore.OverrideDir;   // `AddOwned` 会顺手写背包那一口 ⇒ 别让别处留下的「阻断目录」把它拖红
        try
        {
            InventoryStore.OverrideDir = root;           // 背包那口干到临时目录（⛔ 绝不落到玩家的真存档）
            CardProgress.ResetForTest();                 // 内存清零 + 默认「不落盘」
            CardProgress.OverridePath = root;            // 一个**目录** ⇒ `File.WriteAllText` 抛 ⇒ `Save()` 回 false
            CardProgress.SetPersistForTest(true);        // 拨回「落盘」（`ResetForTest` 把它拨成 false 且没有恢复口）
            Tr(CardProgress.PersistOn, "P3-3 前提：本进程落盘档**开着**（否则 `Save()` 是空操作、测不到失败支）");

            string army = "Ultramarines", rarity = "epic";
            int before = CardProgress.Wildcards(army, rarity);
            Tr(!CardProgress.TrySpendWildcard(army, rarity), "P3-3a ★ 扣减写不进盘 ⇒ `TrySpendWildcard` 回 **false**（原来 `Save()` 是 void、恒 true）");
            C(CardProgress.Wildcards(army, rarity), before, "P3-3a' 那一张**已退回**（⛔ 不白扣玩家的卡）");

            CardProgress.OverridePath = Path.Combine(root, "cardlevels_ok.json");
            Tr(CardProgress.TrySpendWildcard(army, rarity), "P3-3b 正常路径 ⇒ 扣减成功（成功支没被误杀）");
            C(CardProgress.Wildcards(army, rarity), before - 1, "P3-3b' 少了一张");

            CardProgress.OverridePath = root;
            Tr(!CardProgress.AddOwned("UM82", "epic", 1), "P3-3c ★ 拥有数写不进盘 ⇒ `AddOwned` 回 **false**（原来两个落盘口都不判）");
            CardProgress.OverridePath = Path.Combine(root, "cardlevels_ok.json");
            Tr(CardProgress.AddOwned("UM82", "epic", 1), "P3-3c' 正常路径 ⇒ `AddOwned` 回 true");

            CardProgress.SetPersistForTest(false);       // 自检档 = 空操作
            Tr(CardProgress.TrySpendWildcard(army, rarity), "P3-3d ★ 自检档（`PersistOn == false`）⇒ 扣减仍算成功（空操作，⛔ 不是失败）");
        }
        finally
        {
            CardProgress.ResetForTest();
            CardProgress.SetPersistForTest(prevPersist);
            CardProgress.OverridePath = prevPath;
            InventoryStore.OverrideDir = prevInvDir;
        }
    }

    // ============================================================ P3-4 ShopData
    static void CheckShop(string root)
    {
        MenuCheck.Section(_sink, "P3-4 ShopData：购买落盘失败 ⇒ 不谎报「买到了」");
        string prevDir = ShopData.OverrideDir;
        try
        {
            ShopData.OverrideDir = root;
            ShopData.ResetForTest();
            ShopData.OverrideDir = BlockerFile(root);
            Tr(InventoryStore.StoreOn, "P3-4 前提：背包落盘门**开着** ⇒ 下面那次 false 是**真失败**");

            int o0 = ShopData.OwnedOf(0, 0), a0 = ShopData.AvailableOf(0, 0);
            string got = ShopData.Buy(0, 0);
            C(got, ShopData.BuyPersistFailed, "P3-4a ★★ 写盘失败 ⇒ `Buy` 回 **`BuyPersistFailed`**（原来回「名字 ×N」= 谎报买到）");
            Tr(!ShopData.PurchaseSucceeded(got), "P3-4a' 那个哨兵算**失败**（调用方走原版 `OnPurchasedFailed`）");
            C(ShopData.OwnedOf(0, 0), o0, "P3-4a'' 拥有数**已撤回**（盘上那份没变，内存也回原样）");
            C(ShopData.AvailableOf(0, 0), a0, "P3-4a''' 限购剩余**已撤回**");

            ShopData.OverrideDir = root;
            got = ShopData.Buy(0, 0);
            Tr(ShopData.PurchaseSucceeded(got), "P3-4b 正常目录 ⇒ 购买成功（成功支没被误杀）");
            C(ShopData.OwnedOf(0, 0), o0 + 1, "P3-4b' 拥有数 +1");
        }
        finally
        {
            ShopData.OverrideDir = prevDir;
            ShopData.ResetForTest();
        }
    }

    // ============================================================ P3-5 GameModesHandler
    static void CheckGameModes(string root)
    {
        MenuCheck.Section(_sink, "P3-5 GameModesHandler：练习局 `matchStarted` 的落盘失败");
        string prevRoot = SaveCenter.OverrideRoot;
        try
        {
            GameModesHandler.ResetPracticeForTest();
            SaveCenter.OverrideRoot = BlockerFile(root);
            Tr(SaveCenter.StoreOn, "P3-5 前提：`SaveCenter` 落盘门**开着**");
            Tr(!GameModesHandler.NotePracticePersist(false, "自检"), "P3-5a ★ 门开着 + 写失败 ⇒ `NotePracticePersist` 回 **false**（原来三处调用点把返回值直接丢掉）");
            Tr(GameModesHandler.NotePracticePersist(true, "自检"), "P3-5a' 写成功 ⇒ true");

            // `StartPracticeMatchNoCheck` 是**实例**方法（`GameModesHandler : LiveOpsHandler`，后者是普通类不是组件）
            var gm = new GameModesHandler();
            Tr(!gm.StartPracticeMatchNoCheck("P3_SELFTEST"), "P3-5b ★ `StartPracticeMatchNoCheck` 写盘失败 ⇒ 回 **false**（原签名 `void`、失败也当开局记好了）");

            SaveCenter.OverrideRoot = root;
            Tr(gm.StartPracticeMatchNoCheck("P3_SELFTEST"), "P3-5b' 正常目录 ⇒ 回 true（成功支没被误杀）");

            SaveCenter.OverrideRoot = null;
            C(SaveCenter.StoreOn, false, "P3-5c 前提（批处理）：没注入 `OverrideRoot` ⇒ 落盘门**关着**");
            Tr(GameModesHandler.NotePracticePersist(false, "自检"), "P3-5c' ★ 门关着 ⇒ 那一次 false **不算失败**（回 true；⛔ 否则每条批处理自检都会红）");
        }
        finally
        {
            SaveCenter.OverrideRoot = prevRoot;
            GameModesHandler.ResetPracticeForTest();
        }
    }

    // ============================================================ P3-6 LocalPlayerData
    static void CheckLocalPlayerData(string root)
    {
        MenuCheck.Section(_sink, "P3-6 LocalPlayerData：存总档 `SavePlayerData` 的返回值");
        string prevRoot = SaveCenter.OverrideRoot;
        bool prevOverride = LocalPlayerDataStore.PersistOverride;
        try
        {
            LocalPlayerDataStore.PersistOverride = true;      // ⛔ 自检不动 mixer / PlayerPrefs
            LocalPlayerDataStore.ResetForTest();

            SaveCenter.OverrideRoot = BlockerFile(root);
            Tr(!LocalPlayerDataStore.SavePlayerData(), "P3-6a ★ 写盘失败 ⇒ `SavePlayerData()` 回 **false**（原来两处调用点都把返回值丢掉）");
            Tr(!string.IsNullOrEmpty(LocalPlayerDataStore.LastError), "P3-6a' `LastError` 有原因（调用方据此出声）：" + LocalPlayerDataStore.LastError);

            SaveCenter.OverrideRoot = root;
            LocalPlayerDataStore.ResetForTest();
            Tr(LocalPlayerDataStore.SavePlayerData(), "P3-6b 正常目录 ⇒ 回 true（成功支没被误杀）");
        }
        finally
        {
            SaveCenter.OverrideRoot = prevRoot;
            LocalPlayerDataStore.PersistOverride = prevOverride;
            LocalPlayerDataStore.ResetForTest();
        }
    }

    // ============================================================ P3-7 WindowsManager
    static void CheckWindowsManager()
    {
        MenuCheck.Section(_sink, "P3-7 WindowsManager：开窗失败不记成「已打开」");
        // 🔴 先说清**可测边界**：`OpenWindow` 入口那句 `if (win == null)` 用的是 Unity 的 `==`（**已销毁也判真**），
        //   所以在生产路径上走不到 `TryOpen == false`。本段改为直接断**那条短路本身**（`OpenWindow` 撤销登记的依据）：
        //   活着的窗回 true、销毁之后回 false。⛔ 别再为此去 `AddComponent<WindowsManager>()`（批处理下那套手抄有硬伤，见 `RewardsScene`）。
        GameObject go = null;
        GameWindow w = null;
        try
        {
            go = new GameObject("B104_P3_Win");
            w = go.AddComponent<GameWindow>();
            Tr(w.TryOpen(null), "P3-7a 活着的窗 ⇒ `TryOpen` 回 true（正常路径）");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[P3] P3-7 建窗失败（" + e.Message + "）⇒ 这一段只跑得成「已销毁」那一半之外的部分（不静默：这一行就是原因）");
        }
        finally
        {
            if (!ReferenceEquals(go, null)) UnityEngine.Object.DestroyImmediate(go);
        }
        if (!ReferenceEquals(w, null))
            Tr(!w.TryOpen(null), "P3-7b ★ 已销毁的窗 ⇒ `TryOpen` 回 **false**（`OpenWindow` 据此撤销 `openWindows`/`currentWindow`，⛔ 不把窗记成「已打开」）");
    }
}
