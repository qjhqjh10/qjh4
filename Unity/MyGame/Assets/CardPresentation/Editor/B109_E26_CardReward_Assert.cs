// B109_E26_CardReward_Assert.cs — 第一百零九会话 · 执行代理 `B109_E26`（战役奖励里的真卡 id 走 `CardDrawer`）
//
// ============================ 它盯的是什么 ============================
//  原版按**运行时类型**选抽屉：`ItemDrawer.get_DrawerConfig` → `ItemDrawerConfig.GetReference(item.GetType(), ov)`
//  （两轮谓词逐条现读：`decomp_full/ItemDrawerConfig.__c__DisplayClass1_0___GetReference_b__0.c` = 精确判等 ·
//   `…_b__1.c` = `ReflectionHelper.Is`）⇒ `RawCardScript` 那一条的主档抽屉 = `CardDrawer`。
//  我们的替身 = `ItemDrawer.Spec(id, …)` 把「卡池认得出这个 id」翻成 `ItemSpec.Type = "RawCardScript"`
//  （实现 = `Shell/ItemDrawerPartial_B109E26.cs`，`Spec` 末尾同一物理行调它）。
//
//    A ★ 前提：真表那一跳的两端（`RawCardScript → CardDrawer`、`CardDrawer → DrawerCard`）都在
//    B ★ 主项：`CampaignData` 的**真奖励数据**里扫出来的每一个卡 id ⇒ `Spec().Type = RawCardScript`
//        且 `PickDrawer` = `CardDrawer`（⛔ 不是占位板）
//    C ★ 反向：不是卡的 id **不许**被认成卡（`C2` / `UM_SK_Starter` / 卡包 / 掉落表 / 野牌）
//    D ★ 端到端：`ItemDrawer.Draw(Spec("UM2"), …)` 真的建出卡面（`CardView`），且 `Placeholder = false`
//    E ★ 灭自证：卡池里没有的 id ⇒ 仍是兜底；手搓 spec（绕过 `Spec`）⇒ 仍走兜底（补位只发生在 `Spec` 一处）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · B 组：把 `FillCardType` 清空 / 让 `Spec` 不再调它 ⇒ **15 条全红**（今天改前就是这个状态）。
//  · B 组：把 `ItemTypeSets` 里 `RawCardScript` 那行的 `Main` 改掉 ⇒ B 组全红 + A0 红。
//  · C 组：去掉 `FillCardType` 的 `!string.IsNullOrEmpty(s.Type)` 守卫（改成无条件覆盖）⇒
//    `DT Ultramarines All` / `Booster Pack Ultramarines` 两条红。
//  · C 组：把判据从「卡池查得到」放宽成「`UM` 开头」⇒ `UM_SK_Starter` 那条红。
//  · D 组：让 `PickDrawer` 对所有 id 都返回 `CardDrawer` ⇒ D3（对照件 `C2`）+ C 组一起红。
//  · E 组：把补位挪进 `PickDrawer`（绕过 `Spec`）⇒ E2 红（那是**另一处**写同一条规则）。
//
// ============================ 怎么接（⚠️ 本代理不动断言宿主）============================
//  自包含（只要一个空场景）：任一宿主的聚合行、`//` **之前**同一物理行追加：
//      B109_E26_CardReward_Assert.Run(); pass += B109_E26_CardReward_Assert.Pass; fail += B109_E26_CardReward_Assert.Fail;
//  建议落点：`Editor/RewardsScene.cs`（战役奖励窗）或 `Editor/ShellScene.cs`（抽屉全部消费点在外壳侧）。
//
// ============================ 判据 / 报告 ============================
//  · 报告：`资料/完全复刻/报告/B109_E26.md`；上游：`报告/B109_E20.md` §4·1、`判据/B109_R13_AUD05剩余.md` §J1
//  · 映射表：`资料/普查产出_1004/ItemDrawerConfig_映射表.md` §②
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B109_E26`：战役奖励里那 15 个**真卡 id** 必须走 `CardDrawer`（不再是占位板）。</summary>
public static class B109_E26_CardReward_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B109E26] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>今日实读的卡奖励 id（= `CampaignData.Rewards` 32 个唯一 `targetId` 里能对上 `cards_engine.json` 的那 15 个）。
    /// ⚠️ 它**不是判据**，是「A1 那条数据面断言」的期望值：数据一变（多/少一个卡奖励）A1 就红，提醒回来重数。</summary>
    static readonly string[] _campaignCardIds =
    {
        "UM2", "UM3", "UM8", "UM13", "UM17", "UM18", "UM22", "UM24",
        "UM27", "UM28", "UM34", "UM37", "UM39", "UM44", "UM55",
    };

    /// <summary>自包含：只要一个空场景就能跑。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "B109_E26 · 战役奖励里的真卡 id ⇒ `RawCardScript` ⇒ `CardDrawer`（判据 = `ItemDrawerConfig` 映射表 §②）");
        try { SectionA_Prereq(); }
        catch (System.Exception e) { Tr(false, "「A 前提」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionB_RealRewards(); }
        catch (System.Exception e) { Tr(false, "「B 真奖励」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionC_Negative(); }
        catch (System.Exception e) { Tr(false, "「C 反向」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionD_EndToEnd(); }
        catch (System.Exception e) { Tr(false, "「D 端到端」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionE_Discriminators(); }
        catch (System.Exception e) { Tr(false, "「E 灭自证」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B109E26 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 前提：真表那一跳的两端
    static void SectionA_Prereq()
    {
        MenuCheck.Section(_sink, "A 前提：`ItemTypeSets` 里 `RawCardScript → CardDrawer`，且 `CardDrawer` 有实现");

        string main = null;
        for (int i = 0; i < ItemDrawer.ItemTypeSets.Length; i++)
            if (ItemDrawer.ItemTypeSets[i].Type == ItemDrawer.TypeRawCardScript) { main = ItemDrawer.ItemTypeSets[i].Main; break; }
        C(main, "CardDrawer", "A0 **前提**：`ItemTypeSets` 里 `RawCardScript` 的主档 = `CardDrawer`（映射表 §② 那一行）");
        C(ItemDrawer.ImplOfDrawerClass("CardDrawer"), ItemDrawer.DrawerCard,
          "A1 **前提**：`CardDrawer` 这个原版抽屉类有我们的实现（`DrawerImpls` 那条）");
        Tr(CollectionData.Card("UM2") != null,
          "A2 **前提**：卡池认得 `UM2`（`CollectionData.Card` —— 与 `CardDrawerItem` 取卡**同一个口**）");

        // A3：真数据面 —— 扫 `CampaignData` 的 89 条奖励，把「卡池认得出」的那些 id 收出来，与期望表逐条对
        int nodes = CampaignData.NodeCount;
        Tr(nodes > 0, "A3 **前提**：`CampaignData.NodeCount = " + nodes + "`（战役奖励表装着）");
        if (nodes <= 0) return;

        var found = new System.Collections.Generic.List<string>();
        for (int i = 0; i < nodes; i++)
        {
            var rs = CampaignData.RewardsOf(i);
            if (rs == null) continue;
            for (int k = 0; k < rs.Length; k++)
            {
                string id = rs[k].Id;
                if (string.IsNullOrEmpty(id)) continue;
                if (CollectionData.Card(id) == null) continue;          // 不是卡
                if (!found.Contains(id)) found.Add(id);
            }
        }
        int missing = 0, extra = 0;
        string firstDiff = null;
        for (int i = 0; i < _campaignCardIds.Length; i++)
            if (!found.Contains(_campaignCardIds[i])) { missing++; if (firstDiff == null) firstDiff = "缺 " + _campaignCardIds[i]; }
        for (int i = 0; i < found.Count; i++)
            if (System.Array.IndexOf(_campaignCardIds, found[i]) < 0) { extra++; if (firstDiff == null) firstDiff = "多 " + found[i]; }

        C(found.Count, _campaignCardIds.Length,
          "A4 ★ 战役奖励里「卡池认得出」的 id = 期望那 15 个（现扫 " + found.Count + " 个；不一致时请回来重数期望表，"
        + "第一个差异：" + (firstDiff ?? "—") + "）");
        C(missing + extra, 0,
          "A5 ★ 期望表与真数据**逐条对得上**（缺 " + missing + " · 多 " + extra + "）");
    }

    // ============================================================ B · 主项：15 个真卡 id
    static void SectionB_RealRewards()
    {
        MenuCheck.Section(_sink, "B 主项：15 个真卡奖励 id ⇒ `Spec().Type = RawCardScript` 且 `PickDrawer` = `CardDrawer`");

        int badType = 0, badDrawer = 0, badCardId = 0, badStack = 0;
        string firstBad = null;
        for (int i = 0; i < _campaignCardIds.Length; i++)
        {
            string id = _campaignCardIds[i];
            var spec = ItemDrawer.Spec(id, null, null);
            string drawer = ItemDrawer.PickDrawer(spec, DrawerOverride.Default);

            if (spec.Type != ItemDrawer.TypeRawCardScript)
            { badType++; if (firstBad == null) firstBad = id + " Type=" + (spec.Type ?? "null"); }
            if (drawer != ItemDrawer.DrawerCard)
            { badDrawer++; if (firstBad == null) firstBad = id + " drawer=" + (drawer ?? "null"); }
            if (spec.CardId != id) badCardId++;
            if (ItemDrawer.IsStackable(spec, DrawerOverride.Default)) badStack++;   // 原版 `RawCardScript.options.stackable = 0`
        }

        C(badType, 0, "B1 ★ 15 个卡 id 的 `Spec().Type` **全是** `RawCardScript`（第一个错的：" + (firstBad ?? "—") + "）"
                    + "｜🧨 清空 `FillCardType` ⇒ 本行红（15/15 全错）");
        C(badDrawer, 0, "B2 ★ 15 个卡 id 的 `PickDrawer` **全是** `CardDrawer`（⛔ 不再是 `PlaceholderDrawer`）"
                     + "｜🧨 把 `ItemTypeSets` 里 `RawCardScript` 那行改掉 ⇒ 红");
        C(badCardId, 0, "B3 15 个卡 id 的 `ItemSpec.CardId` 都填上了（`CardDrawer` 取卡用它）");
        C(badStack, 0, "B4 15 个卡 id 都**不可堆叠**（原版 `RawCardScript.options.stackable = 0`，"
                     + "判据 = `ItemDrawerOptions` 那张 20 行表；这批 quantity 全是 1 ⇒ 可见效果 0）");

        // B5：逐条列名字（15 条，一条一格 —— 哪一格坏了直接看得见是哪个 id）
        for (int i = 0; i < _campaignCardIds.Length; i++)
        {
            string id = _campaignCardIds[i];
            string drawer = ItemDrawer.PickDrawer(ItemDrawer.Spec(id, null, null), DrawerOverride.Default);
            C(drawer, ItemDrawer.DrawerCard, "B5-" + id + " ★ 战役奖励 `" + id + "` ⇒ `CardDrawer`（改前是 `PlaceholderDrawer`）");
        }
    }

    // ============================================================ C · 反向：不许把非卡认成卡
    static void SectionC_Negative()
    {
        MenuCheck.Section(_sink, "C 反向：非卡 id 不许被认成卡（货币 / 卡包 / 掉落表 / 野牌 / `UM` 前缀诱饵）");

        // C1 货币 code（`Spec` 靠 `CurrencyNames` 认出图，类型仍不是 RawCardScript）
        Tr(ItemDrawer.Spec("C2", null, null).Type != ItemDrawer.TypeRawCardScript,
          "C1 `C2`（货币）**不是** `RawCardScript`（实得 " + (ItemDrawer.Spec("C2", null, null).Type ?? "null") + "）");

        // C2 ★ 前缀诱饵：`UM_SK_Starter` 也是 `UM` 开头，但 `cards_engine.json` 里没有这条 id
        Tr(ItemDrawer.Spec("UM_SK_Starter", null, null).Type != ItemDrawer.TypeRawCardScript,
          "C2 ★ `UM_SK_Starter`（战役真数据里的开局卡组 id，`UM` 前缀诱饵）**不是**卡"
        + "｜🧨 把判据放宽成「`UM` 开头」⇒ 本行红");
        Tr(ItemDrawer.PickDrawer(ItemDrawer.Spec("UM_SK_Starter", null, null), DrawerOverride.Default) != ItemDrawer.DrawerCard,
          "C3 ★ `UM_SK_Starter` 的抽屉**不是** `CardDrawer`");

        // C4/C5/C6：`IdTypes` 那 4 条实据**不被覆盖**（先填先赢）
        C(ItemDrawer.Spec("DT Ultramarines All", null, null).Type, "DropTableItem",
          "C4 `DT Ultramarines All` 仍是 `DropTableItem`（`IdTypes` 实据先填，`FillCardType` 不许覆盖）"
        + "｜🧨 去掉 `!string.IsNullOrEmpty(s.Type)` 守卫 ⇒ 红");
        C(ItemDrawer.Spec("Booster Pack Ultramarines", null, null).Type, "Everguild.LiveOps.ShopContainer",
          "C5 `Booster Pack Ultramarines` 仍是 `Everguild.LiveOps.ShopContainer`（同上）");
        C(ItemDrawer.Spec("WildcardUltramarines2", null, null).Type, ItemDrawer.TypeWildcard,
          "C6 野牌 `WildcardUltramarines2` 仍是 `Wildcard`（野牌那条模式先填，`FillCardType` 不许覆盖）");

        // C7：卡包/掉落表这两档的抽屉也不是卡抽屉
        Tr(ItemDrawer.PickDrawer(ItemDrawer.Spec("DT Ultramarines All", null, null), DrawerOverride.Default) != ItemDrawer.DrawerCard,
          "C7 `DT Ultramarines All` 的抽屉**不是** `CardDrawer`（`RandomCardDrawer`）");
    }

    // ============================================================ D · 端到端：真建出卡面
    static void SectionD_EndToEnd()
    {
        MenuCheck.Section(_sink, "D 端到端：`ItemDrawer.Draw(Spec(\"UM2\"), …)` 建出真卡面（不是占位板）");

        var def = CollectionData.Card("UM2");
        Tr(def != null, "D0 **前提**：卡池认得 `UM2`");
        if (def == null) return;

        var root = new GameObject("B109E26_ROOT");
        ItemDrawResult card = default(ItemDrawResult), ctrl = default(ItemDrawResult);
        try
        {
            var st = ItemDrawerStyle.Default(10, 11, 12);
            st.NamePx = 0f; st.QuantityPx = 0f;              // 文字层不参与本项（也免了 TMP 依赖）

            card = ItemDrawer.Draw(root.transform, new PxRect(0f, 0f, 200f, 300f),
                                   ItemDrawer.Spec("UM2", null, null), 1, DrawerOverride.Default, st);
            C(card.Drawer, ItemDrawer.DrawerCard, "D1 ★ 卡奖励 `UM2` 的抽屉 = `CardDrawer`（改前 = `PlaceholderDrawer`）");
            Tr(card.Node != null, "D2 **前提**：抽屉根节点建出来了");
            Tr(!card.Placeholder, "D3 ★ **没有**走占位板（`Placeholder = false`）—— 这是玩家可见的那一处变化");
            if (card.Node != null)
            {
                var cv = card.Node.GetComponentInChildren<CardView>(true);
                Tr(cv != null, "D4 ★ 卡面真建出来了（`GetComponentInChildren<CardView>(true)` 取得到 —— "
                             + "卡体挂在 `Content/CardUI` 这个**孙节点**上，`Find` 找直接子件是找不到的）");
                Tr(card.Node.Find(ItemDrawer.NodeCardUI) == null,
                  "D5 反例：`res.Node.Find(\"CardUI\")` 是 null（同一件事的另一面，钉住「取件必须 InChildren」）");
            }

            // D6：对照件 —— 非卡 id 走 `Draw` **不**落到 CardDrawer（防「对所有 id 都返回卡抽屉」）
            ctrl = ItemDrawer.Draw(root.transform, new PxRect(0f, 0f, 200f, 300f),
                                   ItemDrawer.Spec("C2", null, null), 1, DrawerOverride.Default, st);
            Tr(ctrl.Drawer != null && ctrl.Drawer != ItemDrawer.DrawerCard,
              "D6 ★ 对照：货币 `C2` 的抽屉**不是** `CardDrawer`（实得 " + (ctrl.Drawer ?? "null") + "）"
            + "｜🧨 让 `PickDrawer` 对所有 id 都返回 `CardDrawer` ⇒ 本行红");
        }
        finally
        {
            if (card.View != null) card.View.Release();
            if (ctrl.View != null) ctrl.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ E · 灭自证
    static void SectionE_Discriminators()
    {
        MenuCheck.Section(_sink, "E 灭自证：卡池没有的 id 仍兜底 · 手搓 spec 不会被补位");

        // E1：不存在的 id ⇒ 判据空，仍走兜底（⛔ 不是「猜一个卡」）
        const string ghost = "B109E26-卡池里没有的 id";
        C(ItemDrawer.Spec(ghost, null, null).Type, null,
          "E1 卡池里没有的 id ⇒ `Spec().Type` 仍是 null（判据空，⛔ 不猜）");
        C(ItemDrawer.PickDrawer(ItemDrawer.Spec(ghost, null, null), DrawerOverride.Default), ItemDrawer.DrawerPlaceholder,
          "E2 卡池里没有的 id ⇒ 仍走 `PlaceholderDrawer`（兜底那条路没被这次改动关掉）");

        // E3：手搓的 `ItemSpec`（**没走 `Spec`**）不会被补位 —— 补位只发生在 `Spec` 那一处
        //     （`CLAUDE.md` §三：两处写同一条规则 = 迟早不一致）
        var hand = new ItemSpec { Id = "UM2", CardId = null, Kind = ItemKind.Unknown, Type = null };
        Tr(ItemDrawer.PickDrawer(hand, DrawerOverride.Default) != ItemDrawer.DrawerCard,
          "E3 ★ 手搓的 `ItemSpec`（`Type = null`，绕过 `Spec`）**不会**被认成卡抽屉"
        + "｜🧨 把补位挪进 `PickDrawer`（= 第二处写同一条规则）⇒ 本行红");
    }
}
