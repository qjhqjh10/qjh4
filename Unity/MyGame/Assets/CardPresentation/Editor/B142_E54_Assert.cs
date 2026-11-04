// B142_E54_Assert.cs — 第一百二十九会话 · 执行代理 `B142-E54`（战役奖励的**非卡**领取链：货币 / 万能卡 / 装饰）
//
// ============================ 它盯的是什么 ============================
//  判据 = `资料/完全复刻/判据/B109_R22_卡奖励收藏链.md` §1 J5（原版 `CollectSingleReward` 的类型分派）
//        + `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/RewardService.cs:641-700`（逐支现读）
//        + `d:/2/tools/decomp_full/RewardService__CollectSingleReward.c`（机器码复核）。
//  实现落点：`Shell/RewardServicePartial_B142E54.cs`（分派 + 三种落地）· `Shell/CardProgressPartial_B142E54.cs`
//            （万能卡发放口）+ `Shell/Inventory.cs` 的 `CollectItemRewardById` **同一物理行**追加（零行数变化）。
//
// ============================ 组 ============================
//  A 货币：`CollectSingleReward(C2 × 500)` ⇒ `Wallet[水晶那一格]` **+500**（不是 +1）；卡背包零变化。
//  B 万能卡：`CollectSingleReward(WildcardUltramarines2 × 3)` ⇒ `CardProgress` 的 `Ultramarines|rare` 桶 **+3**，
//           **别的桶一点不动**（灭自证：加成不分阵营 / 不分档 ⇒ 红）。
//  C 装饰：战役奖励里那条 avatar 类 ⇒ **认得出**（`RewardKindOf == Cosmetic`）且三份库存**零变化**（净零，不是「发出去了」）。
//  D 端到端 + 重复领取：从根一路领到一个**基础档含货币**的节点 ⇒ 水晶 +该条数量；
//            **再领一次同一节点 ⇒ 返回 false 且水晶一分不动**（`Claimable` 闸，重复领取不重复写）。
//  E 反例：`UM_SK_Starter`（预组，`UM` 前缀诱饵）· `Booster Pack Ultramarines`（容器）· `DT Ultramarines R3`（本地没 SO）
//           ⇒ **都不许**被当成货币/万能卡/装饰；`R3` 必须落 `Unknown`（⛔ 不按前缀推广）。
//  F 回归：C9 那条卡支仍然 +1（我这一行追加没把它挤掉）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · 摘掉 `CollectItemRewardById` 里那句 `CollectItemRewardNonCard(id, quantity)` ⇒ A/B/C/D 全变 0 ⇒ 红
//    （**今天改前就是这个状态**：`Note("rs-item:unresolved")` 之后 return）。
//  · 把 `CollectItemRewardNonCard` 改成「任何 id 都 return true」/ 把 `RewardKindOf` 放宽成按前缀 ⇒ E 组红。
//  · 把货币那一支写成「+1」而不是 +quantity ⇒ A1 红（数量 500）。
//
// ============================ ⚠️ 顺序与副作用（接宿主时必读）============================
//  本件开头就调 `CardProgress.ResetForTest()`（**清内存态 + 永久关掉落盘**）—— 理由：D 组沿路领到的战役奖励里
//  有万能卡，真落盘会写开发机的玩家档。⇒ **本件必须排在宿主的【最后】**（与 `B111_S2_Assert` 同一条纪律）。
//  Wallet / CardProgress / CampaignData 的改动：`CampaignData` 在 finally 里 `ResetForTest` + `ResetPointsForTest`；
//  Wallet 只是内存账（`Wallet.Gold` 字典），不落盘、无需还原；卡背包 / 万能卡在自检进程里本来就是夹具态。
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B142-E54`：战役奖励 → `CollectSingleReward` → **货币 / 万能卡 / 装饰** 的落地（卡那一支见 `C9_Assert`）。</summary>
public static class B142_E54_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B142E54] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>水晶那一格的 `Wallet` 记账键（= `MainMenuRuntime.CounterSpecs` 里 `CurCrystals` 那条的 `WalletKey`；
    /// 断言里**独立取一次**，不走被测的 `CurrencyCounterSpec`）。</summary>
    static string CrystalWalletKey()
    {
        var all = MainMenuRuntime.CounterSpecs;
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].Currency == MainMenuRuntime.CurCrystals) return all[i].WalletKey;
        return null;
    }

    /// <summary>四档的顺序（**与 `CardDetailPopup.WildcardRarities` 同序**：原版 `cardRarity` 1..4 ⇒
    /// `common/rare/epic/legendary`）。⛔ 这里是**测试夹具的一份抄本**，因为那一份是 `internal`（Editor 程序集看不到）——
    /// 生产侧仍然只有一张表，两边不一致时 B 组会红（不会静默）。</summary>
    static readonly string[] Rarities = { "common", "rare", "epic", "legendary" };

    /// <summary>自包含：只要一个空场景就能跑（但会关掉落盘，见文件头「顺序与副作用」）。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "B142-E54 · 非卡奖励领取链：`CollectSingleReward → 货币 / 万能卡 / 装饰`（判据 `判据/B109_R22_卡奖励收藏链.md` §1 J5）");

        CardProgress.ResetForTest();     // ⚠️ 关掉落盘（D 组会发万能卡）—— 见文件头
        // 预热：第一次发卡会把卡背包**懒加载**（实测：不预热时「发之前」读到 0、「发之后」读到「存档值 + 1」⇒ 假红）。
        // 与 `C9_Assert` 的 A 组同一条规避。
        RewardService.CollectItemReward(new RewardInfo("UM2", 1));

        try { SectionA_Currency(); }
        catch (System.Exception e) { Tr(false, "「A 货币」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionB_Wildcard(); }
        catch (System.Exception e) { Tr(false, "「B 万能卡」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionC_Cosmetic(); }
        catch (System.Exception e) { Tr(false, "「C 装饰」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionE_Negative(); }
        catch (System.Exception e) { Tr(false, "「E 反例」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionF_CardRegression(); }
        catch (System.Exception e) { Tr(false, "「F 卡支回归」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionD_EndToEnd(); }
        catch (System.Exception e) { Tr(false, "「D 端到端」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B142-E54 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    /// <summary>`ItemDrawer.IdTypes`（原版 SO 的 `m_ClassName` 实读表）里这个 id 的类型；没有条目 ⇒ null。
    /// **独立取数**（不走被测的 `RewardKindOf`），用来给 E 组的期望值当判据。</summary>
    static string TypeInIdTypes(string id)
    {
        var all = ItemDrawer.IdTypes;
        for (int i = 0; i < all.Length; i++)
        {
            int bar = all[i].IndexOf('|');
            if (bar > 0 && all[i].Substring(0, bar) == id) return all[i].Substring(bar + 1);
        }
        return null;
    }

    static void Grant(string id, int qty)
    {
        RewardService.CollectSingleReward(RewardInfo.FromSpec(new CampaignData.RewardSpec
        { Id = id, Quantity = qty, Tier = CampaignData.TierBasic }));
    }

    // ============================================================ A · 货币（原版 `Currency` 那一支）
    static void SectionA_Currency()
    {
        MenuCheck.Section(_sink, "A 货币：`CollectSingleReward(C2 × 500)` ⇒ 水晶那一格 **+500**（原版 `InventoryManager.Add<Currency>` 的本地落点 = `Wallet`）");

        string key = CrystalWalletKey();
        Tr(!string.IsNullOrEmpty(key), "A0 **前提**：`MainMenuRuntime.CounterSpecs` 给 `CurCrystals` 配了 `WalletKey`（现读 = `" + key + "`）");
        if (string.IsNullOrEmpty(key)) return;

        C(RewardService.RewardKindOf("C2"), RewardService.RewardKind.Currency,
           "A0b ★ `RewardKindOf(\"C2\") == Currency`（判据 = 三份货币 SO 的 `code` 字段实读）｜🧨 认不出货币 ⇒ 本条红");

        int before = Wallet.Of(key);
        int bagBefore = Inventories.Cards.Count;
        Grant("C2", 500);
        C(Wallet.Of(key), before + 500,
           "A1 ★ 领 `C2 × 500` ⇒ `Wallet[" + key + "]` **+500**（" + before + " → " + Wallet.Of(key) + "）"
         + "｜🧨 写成「+1」/ 不接货币那一支 ⇒ 红");
        C(Inventories.Cards.Count, bagBefore,
           "A2 ★ 货币奖励**不进卡背包**（条目数 " + bagBefore + " → " + Inventories.Cards.Count + "）｜🧨 把货币 id 当卡发 ⇒ 红");

        // A3 灭自证：卡 id 不许动钱包（货币与卡是两支互斥的落点）
        int w0 = Wallet.Of(key);
        RewardService.CollectItemReward(new RewardInfo("UM2", 1));
        C(Wallet.Of(key), w0,
           "A3 ★ 发一张卡 `UM2` 之后**钱包一分不动**（" + w0 + " → " + Wallet.Of(key) + "）"
         + "｜🧨 `RewardKindOf` 把卡认成货币 ⇒ 红");
    }

    // ============================================================ B · 万能卡（原版 `Wildcard` 那一支）
    static void SectionB_Wildcard()
    {
        MenuCheck.Section(_sink, "B 万能卡：`CollectSingleReward(WildcardUltramarines2 × 3)` ⇒ `Ultramarines|rare` 桶 **+3**，别的桶不动");

        const string Id = "WildcardUltramarines2";
        var spec = ItemDrawer.Spec(Id, null, null);
        C(spec.Kind, ItemKind.Wildcard,
           "B0 **前提**：`ItemDrawer.Spec(\"" + Id + "\").Kind == Wildcard`（原版 SO 的 `m_ClassName` 实读）");
        if (spec.Kind != ItemKind.Wildcard) return;

        C(RewardService.RewardKindOf(Id), RewardService.RewardKind.Wildcard,
           "B0b ★ `RewardKindOf(\"" + Id + "\") == Wildcard`");

        int idx = spec.Rarity - 1;
        if (idx < 0 || idx >= Rarities.Length) { Tr(false, "B0c **前提**：档位 " + spec.Rarity + " 在四档里"); return; }
        string rarity = Rarities[idx];              // 档位 2 ⇒ `rare`
        string other = Rarities[(idx + 1) % Rarities.Length];

        int before = CardProgress.Wildcards("Ultramarines", rarity);
        int otherBefore = CardProgress.Wildcards("Ultramarines", other);
        int armyBefore = CardProgress.Wildcards("Aeldari", rarity);        // 跨阵营不串桶的基线（⛔ 别拿它跟它自己比）
        Grant(Id, 3);
        C(CardProgress.Wildcards("Ultramarines", rarity), before + 3,
           "B1 ★ 领 `" + Id + " × 3` ⇒ `CardProgress[Ultramarines/" + rarity + "]` **+3**（" + before + " → "
         + CardProgress.Wildcards("Ultramarines", rarity) + "）｜🧨 不接万能卡那一支 ⇒ 红");
        C(CardProgress.Wildcards("Ultramarines", other), otherBefore,
           "B2 ★ **别的档**（`" + other + "`）一点不动（" + otherBefore + " → " + CardProgress.Wildcards("Ultramarines", other) + "）"
         + "｜🧨 档位解析写错（全加一桶）⇒ 红");
        C(CardProgress.Wildcards("Aeldari", rarity), armyBefore,
           "B2b ★ **别的阵营**（`Aeldari`，同一档）一点不动（" + armyBefore + " → " + CardProgress.Wildcards("Aeldari", rarity) + "）"
         + "｜🧨 `army` 解析成空串 / 固定值 ⇒ 红");
    }

    // ============================================================ C · 装饰（原版 `CosmeticItem` 一族）
    static void SectionC_Cosmetic()
    {
        MenuCheck.Section(_sink, "C 装饰：战役奖励里的 avatar/title ⇒ **认得出是装饰**、三份库存**零变化**（本工程全解锁 ⇒ 净零）");

        // 从真数据里取（⛔ 不写死 hex —— 数据变了这条会自己跟着走）
        string cosmeticId = null;
        for (int i = 0; i < CampaignData.NodeCount && cosmeticId == null; i++)
        {
            var all = CampaignData.RewardsOf(i);
            for (int k = 0; k < all.Length; k++)
                if (RewardService.RewardKindOf(all[k].Id) == RewardService.RewardKind.Cosmetic) { cosmeticId = all[k].Id; break; }
        }
        Tr(cosmeticId != null,
           "C0 **前提**：战役奖励表里找得到一条装饰类奖励（avatar / title / border；现读 5 条）"
         + "｜⚠️ 一条都找不到 = 数据变了（`CampaignData.Rewards`），回来重核，⛔ 别删这条");
        if (cosmeticId == null) return;

        C(RewardService.RewardKindOf(cosmeticId), RewardService.RewardKind.Cosmetic,
           "C1 ★ `RewardKindOf(\"" + cosmeticId + "\") == Cosmetic`（认得出，⛔ 不落回 `rs-item:unresolved`）"
         + "｜🧨 不接装饰那一支 ⇒ 红（它会落 `Unknown`）");

        string key = CrystalWalletKey();
        int wallet0 = string.IsNullOrEmpty(key) ? 0 : Wallet.Of(key);
        int bag0 = Inventories.Cards.Count;
        int wild0 = CardProgress.Wildcards("Ultramarines", "common");

        bool threw = false;
        try { Grant(cosmeticId, 1); }
        catch (System.Exception e) { threw = true; Tr(false, "C2 收到异常：" + e.GetType().Name + "：" + e.Message); }

        Tr(!threw, "C2 ★ 领一条装饰奖励**不抛**");
        if (!string.IsNullOrEmpty(key)) C(Wallet.Of(key), wallet0, "C3 ★ 装饰奖励**不动钱包**（" + wallet0 + " → " + Wallet.Of(key) + "）");
        C(Inventories.Cards.Count, bag0, "C4 ★ 装饰奖励**不动卡背包**（" + bag0 + " → " + Inventories.Cards.Count + "）");
        C(CardProgress.Wildcards("Ultramarines", "common"), wild0, "C5 ★ 装饰奖励**不动万能卡**（" + wild0 + " → " + CardProgress.Wildcards("Ultramarines", "common") + "）");
    }

    // ============================================================ E · 反例：认不出的 / 别的类型都不许混进这三支
    static void SectionE_Negative()
    {
        MenuCheck.Section(_sink, "E 反例：预组诱饵 · 容器 · 本地没 SO 的掉落表 ⇒ 都不进货币/万能卡/装饰；`DT … R3` 必须落 `Unknown`");

        C(RewardService.RewardKindOf(""), RewardService.RewardKind.Unknown, "E1 空 id ⇒ `Unknown`");
        C(RewardService.RewardKindOf(null), RewardService.RewardKind.Unknown, "E2 null ⇒ `Unknown`");

        var starterKind = RewardService.RewardKindOf("UM_SK_Starter");
        Tr(starterKind == RewardService.RewardKind.Unknown || starterKind == RewardService.RewardKind.PrebuiltDeck,
           "E3 ★ `UM_SK_Starter`（预组卡组，`UM` 前缀诱饵）现读 = `" + starterKind + "` —— 只许是 `Unknown` / `PrebuiltDeck`，"
         + "**⛔ 绝不当货币 / 万能卡 / 装饰**｜🧨 `RewardKindOf` 放宽成按前缀 ⇒ 红");

        C(RewardService.RewardKindOf("Booster Pack Ultramarines"), RewardService.RewardKind.ShopContainer,
           "E4 ★ `Booster Pack Ultramarines` ⇒ `ShopContainer`（判据 = 原版 SO 的 `m_ClassName` 实读）");
        C(RewardService.RewardKindOf("DT Ultramarines All"), RewardService.RewardKind.DropTable,
           "E5 ★ `DT Ultramarines All` ⇒ `DropTable`");

        // 🔴 上面的分类**不能自证**：期望值从 `ItemDrawer.IdTypes`（原版 SO 实读的那张表）**独立取一次**
        C(TypeInIdTypes("Booster Pack Ultramarines"), "Everguild.LiveOps.ShopContainer",
           "E4b **前提**：`ItemDrawer.IdTypes` 里 `Booster Pack Ultramarines` 的类型实读仍是 `ShopContainer`");
        C(TypeInIdTypes("DT Ultramarines All"), "DropTableItem",
           "E5b **前提**：`ItemDrawer.IdTypes` 里 `DT Ultramarines All` 的类型实读仍是 `DropTableItem`");
        Tr(TypeInIdTypes("DT Ultramarines R3") == null,
           "E6b **前提**：`DT Ultramarines R3` 在 `ItemDrawer.IdTypes` 里**没有条目**（本地没有它的 SO）"
         + "｜🧨 有人往那张表里按前缀补了 R3/R4 ⇒ 这条红（回头核 SO 在不在）");

        C(RewardService.RewardKindOf("DT Ultramarines R3"), RewardService.RewardKind.Unknown,
           "E6 ★ `DT Ultramarines R3` ⇒ **`Unknown`**（本地没有它的 SO；⛔ 别因为「长得像 `DT Ultramarines All`」就推广）"
         + "｜🧨 改成 `StartsWith(\"DT \")` ⇒ 红");

        // 这三个类型本工程没有背包 ⇒ 领了也不许动这三份库存
        string key = CrystalWalletKey();
        int wallet0 = string.IsNullOrEmpty(key) ? 0 : Wallet.Of(key);
        int bag0 = Inventories.Cards.Count;
        int wild0 = CardProgress.Wildcards("Ultramarines", "common");
        Grant("Booster Pack Ultramarines", 1);
        Grant("DT Ultramarines All", 1);
        Grant("UM_SK_Starter", 1);
        if (!string.IsNullOrEmpty(key)) C(Wallet.Of(key), wallet0, "E7 容器/掉落表/预组 之后钱包不动（" + wallet0 + " → " + Wallet.Of(key) + "）");
        C(Inventories.Cards.Count, bag0, "E8 容器/掉落表/预组 之后卡背包不动（" + bag0 + " → " + Inventories.Cards.Count + "）");
        C(CardProgress.Wildcards("Ultramarines", "common"), wild0, "E9 容器/掉落表/预组 之后万能卡不动（" + wild0 + " → " + CardProgress.Wildcards("Ultramarines", "common") + "）");
    }

    // ============================================================ F · 回归：卡那一支（C9）没被这一行挤掉
    static void SectionF_CardRegression()
    {
        MenuCheck.Section(_sink, "F 回归：卡支（C9）仍 +1 —— `CollectItemRewardById` 那一句同物理行追加没把它挤掉");

        if (CollectionData.Card("UM2") == null) { Tr(false, "F0 **前提**：卡池认得 `UM2`"); return; }
        int before = Inventories.Cards.Get("UM2") == null ? 0 : Inventories.Cards.Get("UM2").Amount;
        RewardService.CollectItemReward(new RewardInfo("UM2", 1));
        int after = Inventories.Cards.Get("UM2") == null ? 0 : Inventories.Cards.Get("UM2").Amount;
        C(after, before + 1, "F1 ★ 发一张 `UM2` ⇒ 卡背包 +1（" + before + " → " + after + "）｜🧨 追加时把 C9 那一句挤掉 ⇒ 红");
    }

    // ============================================================ D · 端到端 + 重复领取
    static void SectionD_EndToEnd()
    {
        MenuCheck.Section(_sink, "D 端到端：从根领到一个**基础档含货币**的节点 ⇒ 水晶 +该条数量；**再领一次 ⇒ 返回 false 且一分不动**");

        string key = CrystalWalletKey();
        if (string.IsNullOrEmpty(key)) { Tr(false, "D0 **前提**：找得到水晶的 `WalletKey`"); return; }

        // 1) 先找「基础档含货币」的第一个节点（⛔ 不写死节点号）
        int target = -1, targetQty = 0;
        for (int i = 0; i < CampaignData.NodeCount && target < 0; i++)
        {
            var rs = CampaignData.RewardsOf(i, CampaignData.TierBasic);
            for (int k = 0; k < rs.Length; k++)
                if (RewardService.RewardKindOf(rs[k].Id) == RewardService.RewardKind.Currency) { target = i; targetQty = rs[k].Quantity; break; }
        }
        Tr(target >= 0, "D0b **前提**：战役奖励里找得到「基础档含货币」的节点（现读 8 条 `C2`）"
                      + "｜⚠️ 找不到 = 数据变了，回来重核，⛔ 别删这条");
        if (target < 0) return;

        CampaignData.ResetForTest();
        CampaignData.SetPointsForTest(1000000);          // 点数闸别挡路（`Claim` 不扣点）
        var go = new GameObject("B142E54_Assert_CampaignTab");
        CampaignTab tab = null;
        try
        {
            tab = go.AddComponent<CampaignTab>();
            Tr(tab != null, "D0c **前提**：`CampaignTab` 挂得上去");
            if (tab == null) return;

            bool hit = false;
            for (int i = 0; i <= target && !hit; i++)
            {
                if (!CampaignData.Claimable(i)) continue;      // 还没解锁（领过的成功者会解锁后继）
                if (i == target)
                {
                    int before = Wallet.Of(key);
                    bool ok = tab.ClaimForTest(i, CampaignData.TierBasic);
                    RewardWindowFixture.DismissRewardWindows();   // 🔴 不关窗 ⇒ 节点不翻（`ClaimForTest` 的翻节点在关窗回调里）
                    int after = Wallet.Of(key);
                    Tr(ok, "D1 **前提**：`ClaimForTest(" + i + ", Basic)` 受理了（节点 " + CampaignData.NodeName(i) + "）");
                    C(after, before + targetQty,
                       "D2 ★ 端到端：领了节点 `" + CampaignData.NodeName(i) + "`（" + i + "）之后水晶 **+" + targetQty
                     + "**（" + before + " → " + after + "）"
                     + "｜🧨 删掉 `CollectItemRewardById` 里那句 `CollectItemRewardNonCard` ⇒ 本条红");

                    // 3) 重复领取：同一节点再领一次 —— 必须被 `Claimable` 挡下，且一分不动
                    //    ⚠️ 可重复节点（`State == Repeatable(40)`）**原版本来就允许再领** ⇒ 那一档跳过这两条，如实记
                    if (CampaignData.StateOf(i) == CampaignData.Repeatable)
                        Tr(true, "D3（跳过）节点 " + i + " 是**可重复**节点（`State == Repeatable`）—— 原版允许再领，"
                               + "「重复领取不重复写」这两条不适用于它（如实标，不假装验过）");
                    else
                    {
                        int again0 = Wallet.Of(key);
                        bool again = tab.ClaimForTest(i, CampaignData.TierBasic);
                        RewardWindowFixture.DismissRewardWindows();
                        Tr(!again, "D3 ★ 同一节点**再领一次被拒**（`ClaimForTest` 返回 false —— 状态已翻成 `BaseCollected`，`Claimable` 为假）");
                        C(Wallet.Of(key), again0,
                           "D4 ★ **重复领取不重复写**：再领一次之后水晶一分不动（" + again0 + " → " + Wallet.Of(key) + "）"
                         + "｜🧨 把发奖挪到 `Claim` 成功之前又去掉 `Claimable` 闸 ⇒ 红");
                    }
                    hit = true;
                }
                else
                {
                    tab.ClaimForTest(i, CampaignData.TierBasic);     // 只为解锁后继
                    RewardWindowFixture.DismissRewardWindows();
                }
            }

            Tr(hit, "D5 ★ 从根一路领到了目标节点（" + target + "）"
                  + "｜⚠️ 没到 = 战役拓扑变了（后继下标比前驱小那类），回来重核这条断言，⛔ 别删它");
        }
        finally
        {
            if (go != null) Object.DestroyImmediate(go);
            RewardWindowFixture.DismissRewardWindows();   // 兜底：中途抛异常也别把领奖窗留给后面几段
            CampaignData.ResetPointsForTest();
            CampaignData.ResetForTest();
        }
    }
}
