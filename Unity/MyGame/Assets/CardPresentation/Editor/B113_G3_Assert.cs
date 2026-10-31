// B113_G3_Assert.cs — 查判据代理 `B113_G3`（2026-11-01 · 第一百一十一会话 · 判据缺口组）
//
// ============================ 它盯的是哪两件事 ============================
//  `B-143`：`CardbackTable.DefaultFor(army)` 的 **13 条逐条图名** —— 原来只钉了「13 个都在且互不相同」，
//          本件把**判据值（13 条字面量）**钉死（判据表 → `资料/完全复刻/判据/B113_G3_存档owned与卡背图名.md` §二）。
//          这 13 条来自**原版原始字节**（`ArmyUtilities.defaultCardBacks` → `DefaultCarbackByArmySO` 的 13 条
//          `CosmeticItemCardback` PPtr → `imageReference.m_SubObjectName` 去 `_Main`），
//          ⛔ **不是**从我们自己的 `Cardbacks.json` 反推的（那是自证）。
//  `B-140`：`WarpforgeCardLevels.json` 的 `owned` 会不会是第二条「坏数量 1」的源。
//          查清结论 = **当前代码不可能写出 1**（两个写点都是算出来的、恒 ≥ 10）⇒ 迁移**不需扩**；
//          本件把这三条不变量钉住，并把「外部把 `_owned` 改成 1 时迁移的兜底行为」也钉住（防将来改坏）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · **K 组**：13 条期望值是**字面量**（原版字节读出来的），不是 `CardbackTable` 自己的派生。
//      🧨 把 `Resources/Cardbacks.json` 的任一 `defaults[i].name` 改掉 / 改漏一个阵营 ⇒ 对应那条红。
//      🧨 把两个「空格名」写成下划线（`Cardback_SW_Campaign_Free`）⇒ K-3 红（图也取不到）。
//  · **O 组**：
//      🧨 把 `CardProgress.Owned` 的兜底公式改成 `DeckCap`（漏掉 +9）⇒ O-1 红。
//      🧨 把 `MigrateBadCardAmounts` 的「一条都没修好 ⇒ 不写盘」那道闸去掉 ⇒ O-4c 红（文件被重写）。
//      🧨 把 `stillBad` 判据从 `> BadCardAmountSentinel` 改成「播种过就算修好」⇒ O-4a 红。
//
// ============================ 怎么接（⚠️ 本代理不动断言宿主）============================
//  自包含（不建窗、不依赖 UI）。**建议接在 `Editor/CollectionScene.cs:13250` 那条聚合行的最末尾**
//  （`//` 之前、**同一物理行**）：
//      B113_G3_Assert.Run(); _sink.Pass += B113_G3_Assert.Pass; _sink.Fail += B113_G3_Assert.Fail;
//  ⚠️ 也可接在 `RuleEngineTest.cs:1378`（`B112_T2C_Assert` 那一行）末尾 —— 两条都只读 `Resources`。
//
// ============================ 副作用/恢复（O 组）============================
//  · `CardProgress._owned` / `_loaded` / `_persist`、卡背包全量 —— **反射快照 → 临时摆 → finally 逐项还原**；
//    ⛔ 不调 `CardProgress.ResetForTest()`（那会永久关掉落盘、影响后面的断言）。
//  · 迁移夹具全程 `InventoryStore.OverrideDir` = `%TEMP%/wf_b113g3/…` 一次性目录；`SaveCenter.OverrideRoot` 临时置 null。
//  · **真档三重闸**：`Application.isPlaying` / `InventoryStore.StoreOn` / `SaveCenter.StoreOn` 任一为真 ⇒ **O 组整组不跑**。
//    （Play 模式下 `AddOwned` 会经 `InventoryStore.Save` 写玩家真背包 —— 断言绝不许做这件事。）
//  · K 组只读 `Resources`（`CardArt.Cosmetic`），无副作用。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using CardPresentation;

/// <summary>`B-143`（13 条默认卡背图名 · 字面量）+ `B-140`（`WarpforgeCardLevels.json.owned` 不可能被写成 1 · 以及毒化后的兜底行为）。</summary>
public static class B113_G3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B113G3] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ════════════════════════════════════════════════════════════════════════════════════════
    //  ① 编排
    // ════════════════════════════════════════════════════════════════════════════════════════
    public static void Run()
    {
        MenuCheck.Section(_sink, "B113_G3 · `B-143` 13 条默认卡背图名（原版字节 · 字面量）+ `B-140` `owned` 不可能被写成 1 / 毒化后的兜底");

        TrySection("K 默认卡背 13 条图名", SectionK_DefaultCardbacks);
        TrySection("O `owned` 的三个写点与迁移兜底", SectionO_OwnedInvariants);

        Debug.Log(_sink.P + "B113_G3 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    static void TrySection(string name, Action body)
    {
        try { body(); }
        catch (Exception e) { Tr(false, "「" + name + "」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    //  ② K · `B-143`：13 条默认卡背图名（**字面量 = 原版字节读出来的判据值**）
    //     判据链（可复查）：`level0` pid 391（ArmyUtilities，m_Script → globalgamemanagers 3281）
    //       `+0x20` = PPtr{2, 420} → `sharedassets0.assets` pid 420（DefaultCarbackByArmySO，m_Script → 625）
    //       → 13 条 `{army, PPtr{0, p}}` → p 的 m_Name（m_Script → 3315 = CosmeticItemCardback）
    //       → cosmeticsso bundle 里同名 SO 的 `imageReference.m_SubObjectName` 去掉 `_Main`。
    //     ⛔ 上面那 13 条**不是**从 `Cardbacks.json` 抄来的；它来自原版字节（`工具/read_default_cardbacks.py` 同一套切法）。
    // ════════════════════════════════════════════════════════════════════════════════════════
    static readonly string[] KArmies =
    {
        "Ultramarines", "Goff", "SaimHann", "Sautekh", "BlackLegion", "Leviathan", "TauEmpire",
        "Sororitas", "Genestealers", "AstraMilitarum", "DarkAngels", "EmperorsChildren", "SpaceWolves",
    };
    /// <summary>判据值（原版 `DefaultCarbackByArmySO` 13 条；下标与 <see cref="KArmies"/> 一一对应）。
    /// ⚠️ 最后两条**图名里是空格**（SO 名里是下划线/空格混用）—— 别「统一成下划线」。</summary>
    static readonly string[] KNames =
    {
        "Cardback_UM_Campaign_Free",
        "Cardback_GOF_Campaign_Free",
        "Cardback_ASH_Campaign_Free",
        "Cardback_SAU_Campaign_Free",
        "Cardback_BL_Campaign_Free",
        "Cardback_TL_Campaign_Free",
        "Cardback_TAU_Campaign_Free",
        "Cardback_SOR_Campaign_Free",
        "Cardback_GSC_Dawn of Uprising",
        "Cardback_AM_Shield of Humanity",
        "Cardback_DA_The First Legion",
        "Cardback_EC_Emperors Children",
        "Cardback_SW_Campaign Free",
    };

    static void SectionK_DefaultCardbacks()
    {
        MenuCheck.Section(_sink, "K · 默认卡背 13 条：`CardbackTable.DefaultFor(阵营)` == 原版字节读出的**字面量**"
                              + "（原版 `DefaultCarbackByArmySO`，13 条、没有 `Neutral`）");

        C(CardbackTable.DefaultCount, 13, "K-1 表里 13 个阵营（多一个 / 少一个都红）");

        // K-2：逐条字面量
        for (int i = 0; i < KArmies.Length; i++)
        {
            string got = CardbackTable.DefaultFor(KArmies[i]);
            C(got, KNames[i], "K-2." + (i + 1).ToString("00") + " `" + KArmies[i] + "` → **" + KNames[i] + "**"
                            + "（原版 `DefaultCarbackByArmySO` army=" + (10 * (i + 1)) + " 那一格；实得 `" + (got ?? "null") + "`）");
        }

        // K-3：图真的取得到（`CardArt.Cosmetic` = `Resources/Art/cardbacks/<图名>.png`；两个带空格的名字最容易拼错）
        int artMiss = 0;
        for (int i = 0; i < KNames.Length; i++)
        {
            if (CardArt.Cosmetic(KNames[i]) == null)
            {
                artMiss++;
                Tr(false, "K-3 **图取不到**：`Art/cardbacks/" + KNames[i] + "`（跑 `python 工具/import_original_art.py` 补图）");
            }
        }
        C(artMiss, 0, "K-3 灭自证：13 条图**全部**取得到（缺一张就红；含 `Cardback_EC_Emperors Children` / `Cardback_SW_Campaign Free` 这两个带空格的）");

        // K-4：表内自洽（图名 → 阵营 必须回同一个阵营；不然 Army Filter 会把卡背分错阵营）
        int armyMismatch = 0;
        for (int i = 0; i < KNames.Length; i++)
        {
            if (!string.Equals(CardbackTable.ArmyOf(KNames[i]), KArmies[i], StringComparison.OrdinalIgnoreCase))
            {
                armyMismatch++;
                Tr(false, "K-4 `" + KNames[i] + "` 的 `ArmyOf` = `" + (CardbackTable.ArmyOf(KNames[i]) ?? "null")
                        + "`，应为 `" + KArmies[i] + "`");
            }
        }
        C(armyMismatch, 0, "K-4 表内自洽：13 个默认卡背的 `ArmyOf` **全部**等于它那一格阵营");

        // K-5：两两不同 + Neutral 没有默认背
        var set = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < KNames.Length; i++) { string v = CardbackTable.DefaultFor(KArmies[i]); if (v != null) set.Add(v); }
        C(set.Count, 13, "K-5 灭自证：13 个阵营的默认卡背**两两不同**（全返回同一张 ⇒ 这条红）");
        Tr(CardbackTable.DefaultFor("Neutral") == null,
           "K-6 `Neutral` **没有**默认卡背（原版那表就是 13 条、没有 Neutral）｜🧨 表里给 Neutral 也塞一张 ⇒ 红");
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    //  ③ O · `B-140`：`_owned` 的三个写点 + 毒化后的迁移兜底
    //     判据（`Shell/CardDetailPopup.cs`）：
    //       W1 `:189` 从盘读（值 = 上一版 `Save()` 写的）
    //       W2 `:247-248` `Owned` 兜底 = `DeckCap(rarity) + (NeedCopies[4]-NeedCopies[1])` = **11 / 10**（传说）
    //       W3 `:266` `AddOwned` 兜底 = `Owned + n`；生产唯一调用 = `DoCraft` 的 `+1` ⇒ ≥ 11
    //     ⇒ 三个写点**都写不出 1** ⇒ `B-84` 的迁移不需要为这份 json 扩。
    //     另钉一条「兜底行为」：外部把 `_owned` 改成 1 时，迁移必须**识别成没修好 + 不写盘**（`LogError` + 报告）。
    // ════════════════════════════════════════════════════════════════════════════════════════
    const string OId = "UM2";                       // 与 `B112_G_Assert` 同一张（C9 / 收藏线一直在用）
    const string OSynthetic = "__B113G3_NOT_A_CARD__";   // 只用来打 `_owned` 的兜底分支（**不进背包、不落盘**）

    static readonly FieldInfo F_Owned   = typeof(CardProgress).GetField("_owned",   BindingFlags.NonPublic | BindingFlags.Static);
    static readonly FieldInfo F_Loaded  = typeof(CardProgress).GetField("_loaded",  BindingFlags.NonPublic | BindingFlags.Static);
    static readonly FieldInfo F_Persist = typeof(CardProgress).GetField("_persist", BindingFlags.NonPublic | BindingFlags.Static);

    static void SectionO_OwnedInvariants()
    {
        MenuCheck.Section(_sink, "O · `WarpforgeCardLevels.json.owned` 的三个写点都**写不出 1**；外部毒化成 1 时迁移**识别成没修好 + 不写盘**");

        var bag = Inventories.Cards;
        Tr(bag != null, "O-0 **前提**：`Inventories.Cards` 在");
        if (bag == null) return;
        var def = CollectionData.Card(OId);
        Tr(def != null, "O-0b **前提**：卡池认得 `" + OId + "`（`CollectionData.Card`）");
        if (def == null) return;
        var pool = RuleEngine.CardDatabase.Load();
        Tr(pool != null && pool.Count > 0, "O-0c **前提**：卡池非空（`CardDatabase.Load()`）");
        if (pool == null || pool.Count == 0) return;

        // 🔴 真档两道闸：任一为真 ⇒ 整组不跑（下面要种整份背包 + 让迁移写盘）
        Tr(!Application.isPlaying, "O-0d **前提**：不在真 Play 里（Play 下 `AddOwned` 会写玩家真背包 ⇒ 断言不许跑）");
        Tr(!SaveCenter.StoreOn, "O-0e **前提**：`SaveCenter.StoreOn` 关着（批处理 / 编辑器非 Play）—— 否则迁移会写进玩家的真中央档");
        if (Application.isPlaying || SaveCenter.StoreOn) return;

        string root = Path.Combine(Path.GetTempPath(), "wf_b113g3", DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));
        string prevDir = InventoryStore.OverrideDir;
        string prevRoot = SaveCenter.OverrideRoot;
        Dictionary<string, int> keptBag = bag.SnapshotAmounts();
        object ownedObj = F_Owned != null ? F_Owned.GetValue(null) : null;
        var dOwned = ownedObj as Dictionary<string, int>;
        var keptOwned = dOwned != null ? new Dictionary<string, int>(dOwned) : new Dictionary<string, int>();
        object keptLoaded = F_Loaded != null ? F_Loaded.GetValue(null) : null;
        object keptPersist = F_Persist != null ? F_Persist.GetValue(null) : null;

        try
        {
            Directory.CreateDirectory(root);
            SaveCenter.OverrideRoot = null;                 // 中央档那一份不进自检（只验旧档；生产两遍同形）
            if (dOwned != null) dOwned.Clear();
            if (F_Loaded != null) F_Loaded.SetValue(null, true);    // 跳过 `WarpforgeCardLevels.json` 读盘
            if (F_Persist != null) F_Persist.SetValue(null, false); // `CardProgress.Save()` 变空操作（⛔ 不碰玩家真档）

            bag.ClearForTest();
            Inventories.SeedCardsAllUnlocked(pool, c => CardProgress.Owned(c.Id, c.Rarity));

            // ---------------- O-1 / O-2 / O-3：三个写点的值域 ----------------
            // O-1（W2）：卡不在背包 + 缓存里没有 ⇒ 给足公式。基准量 = DeckCap + 9（传说 10 / 其余 11）。
            int gotEpic = CardProgress.Owned(OSynthetic, "epic");
            C(gotEpic, 11, "O-1 W2（`Owned` 兜底）对 epic ⇒ **11**（`DeckCap(epic)=2` + `9`；实得 " + gotEpic + "）"
                         + "｜🧨 把公式改成只留 `DeckCap` ⇒ 2 ⇒ 红");
            int gotLeg = CardProgress.Owned(OSynthetic + "_L", "legendary");
            C(gotLeg, 10, "O-1b W2 对 legendary ⇒ **10**（`DeckCap(legendary)=1` + `9`；实得 " + gotLeg + "）");
            Tr(gotEpic >= 10 && gotLeg >= 10,
               "O-1c **不变量**：W2 的返回值**恒 ≥ 10**（最小卡组上限 1 + `NeedCopies[4]-NeedCopies[1] = 9` ⇒ 永远写不出 1）");

            // O-2（W3）：生产唯一调用 = `DoCraft` 的 +1 ⇒ ≥ 11
            Tr(CardProgress.AddOwned(OSynthetic, "epic", 1), "O-2 **前提**：`AddOwned(+1)` 在兜底分支上回 true（`_persist=false` ⇒ 落盘算成功）");
            int gotEpic2 = CardProgress.Owned(OSynthetic, "epic");
            C(gotEpic2, 12, "O-2b W3（`AddOwned` 兜底）= `Owned + n` ⇒ 12（实得 " + gotEpic2 + "）"
                          + "｜🧨 改成「写死 n」⇒ 1 ⇒ 红");
            Tr(gotEpic2 >= 11, "O-2c **不变量**：生产口径（+1）的 W3 返回值**恒 ≥ 11**");

            // O-3：缓存**参与**唯一播种口径（这是 `B-140` 的真实形状：不是坏源，而是「缓存会压过公式」）
            if (dOwned != null) dOwned[OSynthetic] = Inventories.BadCardAmountSentinel;
            C(CardProgress.Owned(OSynthetic, "epic"), Inventories.BadCardAmountSentinel,
              "O-3 **形状**：`_owned` 里有值就**优先于**公式（毒化成 1 ⇒ `Owned` 回 1）—— 所以迁移才会把它当播种量；"
              + "⚠️ 如果哪天 `Owned` 加了「≤1 就忽略」的净洗，把这条改成断「回 11」并同步判据文件 §1·5");

            // ---------------- O-4：毒化 `_owned` ⇒ 迁移识别成「没修好」+ 不写盘 ----------------
            string dir = Path.Combine(root, "o4");
            Directory.CreateDirectory(dir);
            InventoryStore.OverrideDir = dir;
            InventoryStore.Delete(CardProgress.InventoryName);

            var info = bag.Get(def);
            Tr(info != null, "O-4a **前提**：背包里有 `" + OId + "` 的条目（播种给的）");
            if (info == null) return;
            info.SetAmount(Inventories.BadCardAmountSentinel);
            Tr(InventoryStore.Save(CardProgress.InventoryName, bag), "O-4b **前提**：坏档写下去了");
            string path = InventoryStore.StorePath(CardProgress.InventoryName);
            Tr(File.Exists(path) && File.ReadAllText(path).Contains("{\"k\":\"" + OId + "\",\"amount\":1,"),
               "O-4c **前提**：盘上 `" + OId + "` 的 `amount` **确实是 1**（连逗号一起比，防 `1` 是 `10` 的前缀）");
            byte[] bytes0 = File.ReadAllBytes(path);

            if (dOwned != null) dOwned[OId] = Inventories.BadCardAmountSentinel;   // ★ 毒化：让它播种出来还是 1
            Inventories.ResetCardAmountMigrationForTest();
            InventoryStore.Load(CardProgress.InventoryName, bag);                   // 生产入口

            var afterBad = bag.Get(def);
            C(afterBad != null ? afterBad.Amount : -1, Inventories.BadCardAmountSentinel,
              "O-4d 毒化后播种 = 1 ⇒ 迁移**没修好**这一条（实得 " + (afterBad != null ? afterBad.Amount : -1) + "；"
              + "这正是 `B-140` 记的那条「识别成没修好」）");
            Tr(!string.IsNullOrEmpty(Inventories.LastCardAmountMigrationReport)
               && Inventories.LastCardAmountMigrationReport.Contains("一条都没修好"),
               "O-4e 迁移报告写明 **「一条都没修好」** ⇒ 它**知道**自己没修好（不是静默放过）："
               + (Inventories.LastCardAmountMigrationReport ?? "<null>"));
            byte[] bytes1 = File.ReadAllBytes(path);
            bool same = bytes0.Length == bytes1.Length;
            if (same) for (int i = 0; i < bytes0.Length; i++) if (bytes0[i] != bytes1[i]) { same = false; break; }
            Tr(same, "O-4f **不写盘**：一条都没修好 ⇒ 原档**逐字节不变**（" + bytes0.Length + " → " + bytes1.Length + "）"
                   + "｜🧨 去掉 `repaired == 0 ⇒ 不写` 那道闸 ⇒ 红");

            // ---------------- O-5：灭自证 —— 同夹具、只把毒化去掉 ⇒ 迁移真的会修 ----------------
            if (dOwned != null) dOwned.Remove(OId);
            Inventories.ResetCardAmountMigrationForTest();
            InventoryStore.Load(CardProgress.InventoryName, bag);

            var afterOk = bag.Get(def);
            int want = CardProgress.DeckCap(def.Rarity)
                     + (CardProgress.NeedCopies[CardProgress.MaxLevel] - CardProgress.NeedCopies[1]);
            C(afterOk != null ? afterOk.Amount : -1, want,
              "O-5 灭自证：**只把 `_owned` 的毒去掉**、其余（盘上坏档 / 生产入口 / 用例）完全相同 ⇒ 迁移修成基准量 "
              + want + "（实得 " + (afterOk != null ? afterOk.Amount : -1) + "）⇒ O-4 的红只可能来自「毒」");
            Tr(!string.IsNullOrEmpty(Inventories.LastCardAmountMigrationReport)
               && Inventories.LastCardAmountMigrationReport.Contains("修好 1 条"),
               "O-5b 同一份迁移报告此时写 **「修好 1 条」**：" + (Inventories.LastCardAmountMigrationReport ?? "<null>"));
        }
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
                Inventories.SeedCardsAllUnlocked(RuleEngine.CardDatabase.Load(), c => CardProgress.Owned(c.Id, c.Rarity));
                bag.RestoreAmounts(keptBag);

                // 🔴 `_owned` 必须放在**背包还原之后**再摆回去：上面那次播种会写缓存，先摆会被弄脏
                if (dOwned != null)
                {
                    dOwned.Clear();
                    foreach (var kv in keptOwned) dOwned[kv.Key] = kv.Value;
                }
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch (Exception e) { Debug.LogWarning("[B113G3] 临时目录没删掉（" + root + "）：" + e.Message); }
                Debug.Log("[B113G3] 收尾：卡背包已按快照还原（" + keptBag.Count + " 条）· `_owned` 已还原（" + keptOwned.Count + " 条）· 迁移位已复位");
            }
            catch (Exception e) { Debug.LogError("[B113G3] 收尾失败：" + e); }
        }
    }
}
