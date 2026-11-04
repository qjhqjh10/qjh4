// B143_R4_Assert.cs — 第一百三十会话 · 执行代理 `B143-R4`（奖励背包：卡包 / 掉落表 两层）
//
// ============================ 它盯的是什么 ============================
//  判据 = `资料/完全复刻/判据/B143_奖励背包与待裁四项.md`（`B142-REWARD-INVENTORY` 节）
//        + `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/InventoryManager.cs:1686-1712`（原版背包类型表）
//        + `d:/2/tools/decomp_full/RewardService__CollectSingleReward.c:182`（统一 `HandleOnCollect`）。
//  实现落点：`Shell/BoosterTables.cs`（读表 + 掷骰）· `Shell/BoosterInventory.cs`（两份背包 + 落盘门面）
//           · `Shell/BoosterRewards.cs`（发放口）· `Shell/RewardServicePartial_B142E54.cs:148-149`（分派改真发）。
//  数据产物：`Assets/RuleEngine/Resources/booster_tables.json`（生成器 `Unity/工具/gen_booster_tables.py`）。
//
// ============================ 组 ============================
//  A 表加载：`BoosterTables.Loaded` 真 · 卡包 **11** 份 · 掉落表 **34** 份（原版 `assets_full` 现数）。
//  B 逐层可解析（**卡包 → 卡位 → 掉落表 → 权重**）：
//     `Booster Pack Ultramarines` 恰 **5** 个卡位、第 0 位 = `DT Ultramarines All R2+`；
//     `DT Ultramarines All` 的 4 张子表权重 = **[2600,200,97,4]**（原版 SO 实读）、和 = **2901**；
//     叶子 `DT Ultramarines R1` 有 **38** 张候选卡且每条权重 > 0。
//  C 掷骰：同 seed 逐位相同（可复现）；不同 seed 会出现不同结果；掷出来的 id **一定在**该表声明的候选集合里
//     （⛔ 不会凭空造 id），且**没有空格**（5 格里一个 null 都不许有）。
//  D 真发放：`CollectSingleReward("Booster Pack Ultramarines" ×2)` ⇒ `Inventories.Containers` **+2**；
//     `CollectSingleReward("DT Ultramarines All" ×4)` ⇒ `Inventories.DropTables` **+4**（不是 +1）；
//     两次都**不动卡背包**（两份账互不串）。
//  E 反例：`C2`（货币）/ `UM2`（卡）⇒ 两份新背包**一条都不加**（⛔ 分派不许放宽）；
//     `UM_SK_Starter`（预组）⇒ 分类必须是 `PrebuiltDeck`，且**不落这两份背包**。
//  F 预组卡组（Step 4）：`UM_SK_Starter` 可解析（12 张 · 督军 `UM47` · 模式 13 · 卡背 `Cardback_All_Starter`）·
//     发放后 **卡组库 +1**（原版 `CollectDeckReward` = `InventoryManager.Add<CardDeck>`）且两份新背包不动。
//  G 开已拥有的容器（Step 3 数据层）：发放 1 包 ⇒ 背包 +1 · 开包 ⇒ 扣 1 + **5 格** · 一个都没有时再开 ⇒ false + 出声；
//     掉落表 3 张 ⇒ 展开 1 张 ⇒ 剩 2 张。
//     ⚠️「买包扣钱对不对」**没有判据**（用户边界②不做真实经济，`ShopData.Buy` 刻意不扣钱；原版在服务端扣）。
//  H 商店格 → 容器 `uniqueId` 映射（`🔶 自己设计`，调度台 2026-11-05 裁定走 A）：UM/SW 有值且能在 `booster_tables.json` 里查到；
//     Sautekh/Leviathan/VIP **显式 null + 开包窗出声一次**；反例 = 编一个不存在的 id 必须查不到。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · 把 `RewardServicePartial_B142E54.cs:148/149` 改回 `NoteContainerReward(...)` ⇒ D 组全变 0 ⇒ 红
//    （**改前就是这个状态**：出声、不发）。
//  · 把 `GrantDropTableReward` 的数量写成常量 1 ⇒ D2 红（数量是 4）。   // 🔴 2026-11-05 订正（B143-R4 复核）：原写 `D3`，实际那条是 **D2**（`D3` 是「卡背包条目数不变」）
//  · 把 `RollPackIds` 改成「返回固定 5 张」⇒ **C2d** 红（同 seed 两次相同 **＋** 20 个不同 seed 的**包签名至少 2 种**；
//    固定返回 ⇒ 签名恒 1 种 ⇒ 红）。⚠️ 改前这里写的是「`seen.Count > 1`」——**那条抓不到**（固定 5 张也满足 `5 > 1`），
//    复核指出后已换掉（判据：`C2d` 现在数的是**整包签名**，不是单卡种数）。
//  · 把生成器里 `obtainableItems` / `dropTableItems` 两侧搞混（取到遗留权重那一侧）⇒ B4 红（38 张）。
//
// ============================ ⚠️ 副作用 ============================
//  本件只动**内存**：`BoosterInventoryStore.ResetForTest()` 收尾；`InventoryStore.Save` 在批处理里
//  `StoreOn == false`（`Shell/Inventory.cs:395-399`）⇒ **不碰玩家真存档**。
//  `CollectSingleReward` 会给 `UiBadgeNotificationManager.Refresh(typeof(InventoryBadge))` 一发（原版那条
//  `ShopContainerInventory.OnItemCountModify` 的等价物）—— 只重算表，无落盘。
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B143-R4`：卡包 / 掉落表 两档的**表 → 掷骰 → 落背包**全链。</summary>
public static class B143_R4_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B143R4] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // 原版值（`d:/2/新解包资源/assets_full` 逐份实读，⛔ 别按现表反推自己）
    const int OriginalContainerCount = 11;
    const int OriginalDropTableCount = 34;
    const int OriginalSlots = 5;
    const string OriginalUmSlot0 = "DT Ultramarines All R2+";
    static readonly int[] OriginalUmAllWeights = { 2600, 200, 97, 4 };
    const int OriginalUmR1CardCount = 38;

    public static void Run()
    {
        BoosterInventoryStore.ResetForTest();
        BoosterTables.ResetForTest();
        BoosterTables.EnsureLoaded();

        // ---------------------------------------------------------------- A 表加载
        MenuCheck.Section(_sink, "A 表加载：`booster_tables.json` 已装载，卡包 11 份 · 掉落表 34 份");
        Tr(BoosterTables.Loaded, "A1 `BoosterTables.Loaded` 为真（`Resources/booster_tables.json` 解析成功）"
                              + (BoosterTables.LoadError != null ? "｜失败原因：" + BoosterTables.LoadError : ""));
        C(BoosterTables.ContainerCount, OriginalContainerCount, "A2 卡包份数 = 11（原版 `ShopContainer` SO 数）");
        C(BoosterTables.DropTableCount, OriginalDropTableCount, "A3 掉落表份数 = 34（原版 `DropTableItem` SO 数）");

        // ---------------------------------------------------------------- B 逐层可解析
        MenuCheck.Section(_sink, "B 逐层：卡包 → 卡位 → 掉落表 → 权重表（原版值逐个核）");
        BoosterContainerRow um;
        Tr(BoosterTables.TryGetContainer("Booster Pack Ultramarines", out um) && um != null,
           "B1 `Booster Pack Ultramarines` 取得到");
        if (um == null) { BoosterInventoryStore.ResetForTest(); return; }
        C(um.slots != null ? um.slots.Length : 0, OriginalSlots, "B2 卡位数 = 5（原版 `dropTableItems` 5 条）");
        C(um.slots != null && um.slots.Length > 0 ? um.slots[0] : null, OriginalUmSlot0,
           "B3 第 0 卡位 = `DT Ultramarines All R2+`（= 原版的「至少一张 Rare or better」那一格）");

        BoosterDropTableRow all;
        Tr(BoosterTables.TryGetDropTable("DT Ultramarines All", out all) && all != null,
           "B4a `DT Ultramarines All` 取得到");
        if (all == null) { BoosterInventoryStore.ResetForTest(); return; }
        C(all.tables != null ? all.tables.Length : 0, OriginalUmAllWeights.Length,
           "B4b `DT Ultramarines All` 的子表数 = 4（R1..R4）");
        for (int i = 0; i < OriginalUmAllWeights.Length; i++)
        {
            int w = all.tables != null && i < all.tables.Length ? all.tables[i].weight : -1;
            C(w, OriginalUmAllWeights[i], "B4c 子表权重[" + i + "] = " + OriginalUmAllWeights[i] + "（原版 SO 实读）");
        }
        C(BoosterTables.WeightTotal(all.tables), 2901, "B4d 权重和 = 2600+200+97+4 = 2901");

        BoosterDropTableRow r1;
        Tr(BoosterTables.TryGetDropTable("DT Ultramarines R1", out r1) && r1 != null,
           "B5a 叶子表 `DT Ultramarines R1` 取得到");
        if (r1 != null)
        {
            C(r1.cards != null ? r1.cards.Length : 0, OriginalUmR1CardCount,
               "B5b 叶子候选卡 = 38 张（⚠️ 不是遗留 `dropTableItemsWeight` 那一侧的条数）");
            int bad = 0;
            if (r1.cards != null) for (int i = 0; i < r1.cards.Length; i++) if (r1.cards[i] == null || r1.cards[i].weight <= 0) bad++;
            C(bad, 0, "B5c 叶子每条权重都 > 0（没有 0/负权重把某张卡掷成黑洞）");
        }

        // ---------------------------------------------------------------- C 掷骰
        MenuCheck.Section(_sink, "C 掷骰：同 seed 可复现 · 不同 seed 会变 · 结果一定在候选集合里");
        var a = BoosterTables.RollPackIds("Booster Pack Ultramarines", OriginalSlots, 12345);
        var b = BoosterTables.RollPackIds("Booster Pack Ultramarines", OriginalSlots, 12345);
        C(a.Length, OriginalSlots, "C1a 一包 = 5 格");
        bool same = a.Length == b.Length;
        if (same) for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) same = false;
        Tr(same, "C1b **同 seed 逐位相同**（可复现）");

        var allowed = new HashSet<string>();
        CollectLeafCards("DT Ultramarines All", allowed, 0);
        Tr(allowed.Count > 0, "C2a 候选集合非空（从 `DT Ultramarines All` 递到叶子）");
        int empty = 0, outside = 0;
        var seen = new HashSet<string>();
        var packSigs = new HashSet<string>();     // 🔴 整包签名（20 个不同 seed）—— 这才是「有没有真在摇」的判别式
        for (int k = 0; k < 200; k++)
        {
            var pack = BoosterTables.RollPackIds("Booster Pack Ultramarines", OriginalSlots, 1000 + k);
            if (k < 20) packSigs.Add(string.Join(",", pack));
            for (int i = 0; i < pack.Length; i++)
            {
                if (string.IsNullOrEmpty(pack[i])) { empty++; continue; }
                seen.Add(pack[i]);
                if (!allowed.Contains(pack[i])) outside++;
            }
        }
        C(empty, 0, "C2b 200 包 × 5 格，**一个 null 都没有**（每格都掷得出来）");
        C(outside, 0, "C2c 掷出来的卡 id **一定在** `DT Ultramarines All` 的叶子候选集合里（⛔ 不凭空造 id）");
        // 🔴 2026-11-05 订正（B143-R4 复核）：原来这条是 `seen.Count > 1` —— **判别力为零**（固定返回 5 张也满足 5>1）。
        //    改成数**整包签名**：固定返回 ⇒ 20 包全一样 ⇒ 签名恒 1 种 ⇒ 红；真在摇 ⇒ 几乎必然 ≥ 2 种。
        Tr(packSigs.Count >= 2, "C2d **不同 seed 会摇出不同的包**（20 个不同 seed ⇒ " + packSigs.Count + " 种整包签名；"
                              + "单卡种数 " + seen.Count + "）｜🧨 `RollPackIds` 改成「返回固定 5 张」⇒ 签名恒 1 ⇒ 红");

        // ---------------------------------------------------------------- D 真发放
        MenuCheck.Section(_sink, "D 真发放：卡包 ×2 / 掉落表 ×4 各自进自己的背包，互不串");
        int c0 = Amount(Inventories.Containers, "Booster Pack Ultramarines");
        int d0 = Amount(Inventories.DropTables, "DT Ultramarines All");
        int cards0 = Inventories.Cards.Count;
        RewardService.CollectSingleReward(new RewardInfo("Booster Pack Ultramarines", 2));
        RewardService.CollectSingleReward(new RewardInfo("DT Ultramarines All", 4));
        C(Amount(Inventories.Containers, "Booster Pack Ultramarines") - c0, 2, "D1 卡包背包 +2（数量照奖励条目，不是 +1）");
        C(Amount(Inventories.DropTables, "DT Ultramarines All") - d0, 4, "D2 掉落表背包 +4（同上）");
        C(Inventories.Cards.Count, cards0, "D3 卡背包条目数不变（卡包/掉落表停在背包里，**没被当场打开**）");
        Tr(Amount(Inventories.DropTables, "Booster Pack Ultramarines") == 0,
           "D4 卡包没串到掉落表背包（两份账是分开的）");
        Tr(Amount(Inventories.Containers, "DT Ultramarines All") == 0,
           "D5 掉落表没串到卡包背包");

        // ---------------------------------------------------------------- E 反例
        MenuCheck.Section(_sink, "E 反例：货币 / 卡 / 预组 都不许落这两份背包");
        // ⚠️ 这一组**不真发货币/卡**（那会改内存 Wallet / 卡背包，可能扰动同宿主里别的断言）⇒
        //    改用「分类 + 一次不可解析 id 的空发放」两半。
        C(RewardService.RewardKindOf("C2"), RewardService.RewardKind.Currency,
           "E1 `C2` 的分类是 `Currency`（⛔ 不是容器 / 掉落表）");
        C(RewardService.RewardKindOf("UM2"), RewardService.RewardKind.Unknown,
           "E2 卡 id `UM2` 不在这两档里（卡走 `CollectItemRewardCard` 那一支；`RewardKindOf` 刻意不报 Card）");
        C(RewardService.RewardKindOf("Booster Pack Ultramarines"), RewardService.RewardKind.ShopContainer,
           "E3 `Booster Pack Ultramarines` 分类 = `ShopContainer`（与反例同一条判据的另一侧）");
        C(RewardService.RewardKindOf("DT Ultramarines All"), RewardService.RewardKind.DropTable,
           "E4 `DT Ultramarines All` 分类 = `DropTable`");
        C(RewardService.RewardKindOf("DT Ultramarines R3"), RewardService.RewardKind.DropTable,
           "E5 `DT Ultramarines R3` 也认得出（`ItemDrawer.IdTypes` 实据，非前缀推广）");

        // 预组那一档在 Step 4 之后**会真发**（不是「什么都不发」）⇒ 它的正例/反例都由 F 组单独断，
        // 这里只留上面两条「分类」反例，不在这里发东西。

        // ---------------------------------------------------------------- F 预组卡组（Step 4）
        MenuCheck.Section(_sink, "F 预组卡组：`UM_SK_Starter` 可解析 ⇒ 发放后进卡组库（原版 `CollectDeckReward`）");
        var spec = BoosterPrebuiltDecks.ById("UM_SK_Starter");
        Tr(spec != null, "F1 `BoosterPrebuiltDecks.ById(\"UM_SK_Starter\")` 非空"
                       + (BoosterPrebuiltDecks.LoadError != null ? "｜加载失败：" + BoosterPrebuiltDecks.LoadError : ""));
        if (spec != null)
        {
            C(spec.cardIds != null ? spec.cardIds.Length : 0, 12, "F2 卡表 = 12 张（原版 SO `cardLibraryIds` 实读）");
            C(spec.heroId, "UM47", "F3 督军 = `UM47`（原版 `deckHero.targetId`）");
            C(spec.gameMode, 13, "F4 模式 = 13（`GameMode.Skirmish`；原版 SO `gameMode` 实读）");
            C(spec.cardback, "Cardback_All_Starter", "F5 卡背 = `Cardback_All_Starter`（原版 `cardbackId` 链到名字）");
        }
        C(RewardService.RewardKindOf("UM_SK_Starter"), RewardService.RewardKind.PrebuiltDeck,
           "F6 `UM_SK_Starter` 分类 = `PrebuiltDeck`（`prebuilt_decks.json` 只收练习池 103 副，它在 `prebuilt_deck_lists.json`）"
         + "｜🧨 去掉 `BoosterPrebuiltDecks.ById` 那一跳 ⇒ 落 `Unknown` ⇒ 红");
        C(RewardService.RewardKindOf("UM_SK_Nope"), RewardService.RewardKind.Unknown,
           "F7 不存在的 `UM_SK_Nope` ⇒ `Unknown`（⛔ 按 `UM_SK_` 前缀推广）");

        var lib = DeckRuntime.SharedLibrary;
        Tr(lib != null, "F8 `DeckRuntime.SharedLibrary` 取得到");
        if (lib != null)
        {
            int decks0 = lib.Decks.Count;
            int cont0 = Inventories.Containers.Count, dt0 = Inventories.DropTables.Count;
            RewardService.CollectSingleReward(new RewardInfo("UM_SK_Starter", 1));
            C(lib.Decks.Count, decks0 + 1, "F9 发一副预组 ⇒ 卡组库 **+1**（原版 `InventoryManager.Add<CardDeck>`）"
                                         + "｜🧨 把 `GrantPrebuiltDeckReward` 改回 `NoteContainerReward` ⇒ 这里红");
            C(Inventories.Containers.Count, cont0, "F10 预组没被误塞进卡包背包");
            C(Inventories.DropTables.Count, dt0, "F11 预组没被误塞进掉落表背包");
        }

        // ---------------------------------------------------------------- G 开已拥有的容器（Step 3 数据层）
        MenuCheck.Section(_sink, "G 开已拥有的卡包 / 掉落表：扣 1 → 摇出 5 格 / 1 张（原版 `ShopOfferContainer.OpenContainer`）");
        // ⚠️ 「买包扣钱对不对」这一条**没有判据**：用户 2026-09-17 边界②「不做真实经济」⇒ `ShopData.Buy` 刻意不扣钱
        //    （`Shell/ShopData.cs:465-478` 的 doc 写死）。原版扣钱在 PlayFab 服务端（关服）。这里只断**背包账**。
        BoosterInventoryStore.ResetForTest();                 // G 组要从干净的两份背包起算
        Inventories.Containers.ClearForTest();
        Inventories.DropTables.ClearForTest();
        RewardService.CollectSingleReward(new RewardInfo("Booster Pack Ultramarines", 1));
        C(BoosterOpenOwned.OwnedCount("Booster Pack Ultramarines"), 1, "G1 发放 1 包 ⇒ 卡包背包 **+1**");
        string[] got;
        string why;
        Tr(BoosterOpenOwned.TryOpen("Booster Pack Ultramarines", 777, out got, out why) && got != null,
           "G2 开一个已拥有的包 ⇒ true（失败原因：" + (why ?? "<无>") + "）");
        C(got != null ? got.Length : 0, 5, "G3 开出来 **5 格**（原版 `dropTableItems` 5 个卡位）");
        C(BoosterOpenOwned.OwnedCount("Booster Pack Ultramarines"), 0, "G4 开包后背包 **−1**（1 → 0）"
                                                                   + "｜🧨 不扣减 ⇒ 这里读到 1 ⇒ 红");
        Tr(!BoosterOpenOwned.TryOpen("Booster Pack Ultramarines", 778, out got, out why) && !string.IsNullOrEmpty(why),
           "G5 一个都没有时再开 ⇒ **false + 非空原因**（⛔ 不静默；原版这一档退回购买）");

        RewardService.CollectSingleReward(new RewardInfo("DT Ultramarines All", 3));
        C(BoosterOpenOwned.OwnedDropTableCount("DT Ultramarines All"), 3, "G6 发放 3 张掉落表 ⇒ 掉落表背包 **+3**");
        string card;
        Tr(BoosterOpenOwned.TryOpenDropTable("DT Ultramarines All", new System.Random(9), out card, out why)
           && !string.IsNullOrEmpty(card), "G7 展开一张掉落表 ⇒ 得到一张卡 id（实得 `" + (card ?? "<null>") + "`）");
        C(BoosterOpenOwned.OwnedDropTableCount("DT Ultramarines All"), 2, "G8 展开后掉落表 **−1**（3 → 2）");

        // ---------------------------------------------------------------- H 商店格 → 容器 id 映射（`🔶 自己设计`）
        MenuCheck.Section(_sink, "H 商店格 → 容器 `uniqueId` 映射：UM/SW 有值且能查到；Sautekh/Leviathan/VIP 显式 null + 出声");
        BoosterOpenOwned.ResetUnmappedNotesForTest();
        C(BoosterOpenOwned.ContainerIdOf(0, 0), "Booster Pack Ultramarines",
           "H1 第 0 页第 0 件（`Ultramarines Booster`）⇒ `Booster Pack Ultramarines`");
        C(BoosterOpenOwned.ContainerIdOf(0, 2), "Booster Pack Space Wolves",
           "H2 第 0 页第 2 件（`Space Wolves Booster`）⇒ `Booster Pack Space Wolves`");
        BoosterContainerRow _crow;
        Tr(BoosterTables.TryGetContainer(BoosterOpenOwned.ContainerIdOf(0, 0), out _crow) && _crow != null,
           "H3 映射到的 id **能在 `booster_tables.json` 里查到**（⛔ 不是编的）");
        Tr(BoosterTables.TryGetContainer(BoosterOpenOwned.ContainerIdOf(0, 2), out _crow) && _crow != null,
           "H4 同上（Space Wolves）");
        C(BoosterOpenOwned.ContainerIdOf(0, 1), null, "H5 `Sautekh Booster` ⇒ **显式 null**（本地没有这一族的容器 SO，⛔ 编 id）");
        C(BoosterOpenOwned.ContainerIdOf(0, 3), null, "H6 `Leviathan Booster` ⇒ 显式 null");
        C(BoosterOpenOwned.ContainerIdOf(3, 0), null, "H7 `VIP Booster Cache` ⇒ 显式 null");
        string _r;
        RuleEngine.CardDef[] _defs;
        Tr(!BoosterOpenOwned.TryTakeOwnedCards(null, 1, out _defs, out _r) && BoosterOpenOwned.UnmappedNoteCount >= 1,
           "H8 没映射的那一格走开包窗 ⇒ **返 false + 出声一次**（`UnmappedNoteCount` = "
         + BoosterOpenOwned.UnmappedNoteCount + "，⛔ 不静默）");
        Tr(!BoosterTables.TryGetContainer("Booster Pack NotExist", out _crow),
           "H9 反例：编一个不存在的 id ⇒ **查不到**（挡「按前缀/名字像就算数」）｜🧨 放宽成 `StartsWith(\"Booster Pack\")` ⇒ 红");
        C(BoosterOpenOwned.OwnedCount("Booster Pack NotExist"), 0, "H10 反例：不存在的 id 的拥有数 = 0（不抛）");

        BoosterInventoryStore.ResetForTest();
        BoosterTables.ResetForTest();
        BoosterPrebuiltDecks.ResetForTest();
        DeckRuntime.InvalidateSharedLibrary();   // 丢掉本次自检改过的内存态（盘上没写：批处理 `StoreOn == false`）
    }

    static int Amount<T>(Inventory<T> inv, string id) where T : class
    {
        if (inv == null || string.IsNullOrEmpty(id)) return 0;
        var info = inv.Get(id);
        return info != null ? info.Amount : 0;
    }

    /// <summary>从一张掉落表递归收集**叶子卡 id**（深度上限同生产侧）。</summary>
    static void CollectLeafCards(string dtId, HashSet<string> into, int depth)
    {
        if (depth > BoosterTables.MaxRollDepth || string.IsNullOrEmpty(dtId)) return;
        BoosterDropTableRow row;
        if (!BoosterTables.TryGetDropTable(dtId, out row) || row == null) return;
        if (row.cards != null)
            for (int i = 0; i < row.cards.Length; i++)
                if (row.cards[i] != null && !string.IsNullOrEmpty(row.cards[i].id)) into.Add(row.cards[i].id);
        if (row.tables != null)
            for (int i = 0; i < row.tables.Length; i++)
                if (row.tables[i] != null) CollectLeafCards(row.tables[i].id, into, depth + 1);
    }
}
