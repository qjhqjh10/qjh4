// B109_E11_Drawer_Assert.cs — 第一百零九会话 · 执行代理 `B109_E11`（抽屉全族 25 档）
//
// ============================ 它盯的是什么 ============================
//  1) **覆盖**：`ItemDrawerConfig` 那 20 个物品类型 × 5 档（主档 + 4 个 override）指向的**每一个原版抽屉类**，
//     都必须在 `DrawerImpls` 里有实现 —— 本会话补上第 25 个（`GenericArmyItemDrawer`）之后应为**零缺口**。
//  2) **走对分支**：`PickDrawer(ItemSpec, DrawerOverride)` 对**每一条记录 × 每一档**都要给出
//     「该档写的类（没写 ⇒ 回落主档）」对应的那个实现 —— 这是「key → 分支」的机检口。
//  3) **版面**：共享抽屉版面尺（底图 sprite 名 / 根与 Content 尺寸 / Image 默认矩形 / Label 行 /
//     Quantity·Name 分栏 / 节点名）逐项对着 **prefab 实读值**核，并且核**三条派生关系**
//     （Label 的 25px padding 与 10px spacing）—— 防「两个常量被抄成同一个数」。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A2：把 `DrawerImpls` 里 `"GenericArmyItemDrawer"` 那条删掉 ⇒ A2/A4 红（覆盖回 24）。
//  · B 段任意一条：把 `ItemTypeSets` 某一格改错（例如 `ForgePoints` 的 Icon 档写成主档）⇒ 那一条红。
//  · C4/C5/C8：把 `ItemDrawerLayout.cs` 里 `DrImgY2` 或 `DrCx2` 改一位 ⇒ 对应条红；
//    把 `DrQtyX2` 改成 `DrNameX1`（去掉 10px spacing）⇒ C8 红。
//  · C9：把节点名常量改成 `"Background2"` ⇒ 红。
//  · D1/D2/D3：把两档合并成一个实现常量 ⇒ 红（这正是施工单「拿主档顶替变体」那个坑）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  与 `B101R8_Assert` 同一宿主即可（`CardPresentation/Editor/ShellScene.cs` 最稳：抽屉全部消费点都在外壳侧；
//  商店那条在 `ShopScene.cs` 也吃得到）。在它那条聚合行末尾**同一物理行**追加：
//      B109_E11_Drawer_Assert.Run(); pass += B109_E11_Drawer_Assert.Pass; fail += B109_E11_Drawer_Assert.Fail;
//
// ============================ 判据 / 报告 ============================
//  · 施工单：`资料/完全复刻/判据/B100_D1_抽屉18档_施工单.md`（§〇 口径 20 类型 / 25 类；表 A、表 B、§三-2）
//  · 版面：`Shell/ItemDrawerLayout.cs` 文件头那 13 份 prefab 逐份 `menu_dump` 实读（B101_E3，2026-10-30）
//  · 本件报告：`资料/完全复刻/报告/B109_E11.md`
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B109_E11`：抽屉全族（25 档）的**覆盖 + 路由 + 版面**机检（纯计算，不建节点）。</summary>
public static class B109_E11_Drawer_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B109E11] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    public static void Run()
    {
        MenuCheck.Section(_sink, "B109_E11 抽屉全族：覆盖 / 路由 / 版面（纯计算，不依赖 Unity 运行时）");
        SectionA_Coverage();
        SectionB_Routing();
        SectionC_Layout();
        SectionD_Discriminators();
        SectionE_AgainstMappingTable();
        Debug.Log(_sink.P + "B109E11 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 覆盖：25 个原版类零缺口
    static void SectionA_Coverage()
    {
        MenuCheck.Section(_sink, "A 覆盖：`ItemTypeSets`（20 类型 × 5 档）里每个原版抽屉类都在 `DrawerImpls`");

        // 前提（硬检查）：表本身得在（20 条 = 映射表 §② 的 20 个 ObtainableItem 类型）
        int rows = ItemDrawer.ItemTypeSets == null ? 0 : ItemDrawer.ItemTypeSets.Length;
        C(rows, 20, "A0 **前提**：`ItemTypeSets` = 20 条（映射表 §② 的 20 个物品类型）");
        if (rows != 20) return;

        // A1：20 主档 + 19 档 override = 39 个 (类型, 档) 格，每一格都必须解析到一个实现
        int cells = 0, missing = 0;
        string firstMissing = null;
        DrawerOverride[] ovs = { DrawerOverride.Default, DrawerOverride.Icon, DrawerOverride.Horizontal, DrawerOverride.Shop, DrawerOverride.OfferPopups };
        for (int i = 0; i < ItemDrawer.ItemTypeSets.Length; i++)
        {
            var e = ItemDrawer.ItemTypeSets[i];
            for (int k = 0; k < ovs.Length; k++)
            {
                string cls = ovs[k] == DrawerOverride.Default ? e.Main : e.Variant(ovs[k]);
                if (cls == null) continue;                     // 这一档没写 ⇒ 回落主档（§三 口径）
                cells++;
                if (ItemDrawer.ImplOfDrawerClass(cls) == null) { missing++; if (firstMissing == null) firstMissing = e.Type + "/" + ovs[k] + " ⇒ " + cls; }
            }
        }
        C(cells, 39, "A1 **前提**：表里写了 39 格（20 主档 + 19 档 override；与映射表 §② 条数一致）");
        C(missing, 0, "A2 这 39 格**全部**有实现（缺一个 ⇒ 这一条红；第一个缺的是 " + (firstMissing ?? "—") + "）");

        // A3：原版抽屉类 —— 现读 `DrawerImpls` **26 条** = 配置表里的 24 个类（本件 A/E 两张表覆盖）
        //     + `DeckAndCardbackDrawer`（施工单 §〇：配置表不引用它，但 `Deck Drawer` 的宿主实例上挂着）
        //     + `CosmeticDrawer`（施工单表 C 的 B4 旁支，只被 `DeckAndCardbackDrawer.cardBackDrawer` 用）
        C(ItemDrawer.DrawerImpls == null ? 0 : ItemDrawer.DrawerImpls.Length, 26,
          "A3 `DrawerImpls` = 26 条 = 配置表 24 类 + `DeckAndCardbackDrawer` + `CosmeticDrawer`（施工单 §〇 / 表 C B4）");

        // A4：本会话补的那一档
        C(ItemDrawer.ImplOfDrawerClass("GenericArmyItemDrawer"), ItemDrawer.DrawerGenericArmy,
          "A4 `GenericArmyItemDrawer` 已登记（表 C2 那一档；删掉那条 ⇒ 回 24 ⇒ 红）");
        Tr(ItemDrawer.ShowInfoOnClickOf(ItemDrawer.DrawerGenericArmy),
          "A5 `GenericArmyItemDrawer` 的 `showItemInfoOnClick` = 1/1（prefab 实读 `MonoBehaviour_1212849660543184768.json`）");
        C(ItemDrawer.ShowInfoOnClickOf(ItemDrawer.DrawerWildcardIcon), false,
          "A6 `WildcardIconDrawer` 是**全票 0**（0:1）⇒ 不可点（混合档/全 0 档的默认值判据）");

        // A7：两个「配置表不引用、但原版有」的类（漏了它们 ⇒ 覆盖数对不上，但 39 格全绿）
        C(ItemDrawer.ImplOfDrawerClass("DeckAndCardbackDrawer"), ItemDrawer.DrawerDeckAndCardback,
          "A7 `DeckAndCardbackDrawer` 已登记（施工单 §六-1：`Deck Drawer` 宿主实例上挂的其实是它）");
        C(ItemDrawer.ImplOfDrawerClass("CosmeticDrawer"), ItemDrawer.DrawerCosmetic,
          "A8 `CosmeticDrawer` 已登记（表 C B4 旁支；`CardbackDrawer` 与它**不是**同一个类）");
    }

    // ============================================================ B · 路由：每档 key 走对分支
    static void SectionB_Routing()
    {
        MenuCheck.Section(_sink, "B 路由：`PickDrawer` 对每条记录 × 每一档都给出该档写的类（没写 ⇒ 回落主档）");

        DrawerOverride[] ovs = { DrawerOverride.Default, DrawerOverride.Icon, DrawerOverride.Horizontal, DrawerOverride.Shop, DrawerOverride.OfferPopups };
        int bad = 0;
        string firstBad = null;
        for (int i = 0; i < ItemDrawer.ItemTypeSets.Length; i++)
        {
            var e = ItemDrawer.ItemTypeSets[i];
            for (int k = 0; k < ovs.Length; k++)
            {
                string cls = ovs[k] == DrawerOverride.Default ? e.Main : (e.Variant(ovs[k]) ?? e.Main);   // 回落主档
                string want = ItemDrawer.ImplOfDrawerClass(cls);
                var spec = new ItemSpec { Id = "assert:" + e.Type, Kind = ItemKind.Generic, Type = e.Type };
                string got = ItemDrawer.PickDrawer(spec, ovs[k]);
                if (got != want) { bad++; if (firstBad == null) firstBad = e.Type + "/" + ovs[k] + " got=" + (got ?? "null") + " want=" + (want ?? "null"); }
            }
        }
        C(bad, 0, "B1 100 票（20 类型 × 5 档）**全对**（第一个错的：" + (firstBad ?? "—") + "）");

        // B2：种类为 None ⇒ 照原版第 ② 步**不画**（`ItemDrawer__Draw.c`：drawer == null ⇒ return null）
        var none = new ItemSpec { Id = null, Kind = ItemKind.None };
        C(ItemDrawer.PickDrawer(none, DrawerOverride.Default), null,
          "B2 `ItemKind.None` ⇒ `PickDrawer` 返回 null（= 原版「不画」，⛔ 不是给个默认抽屉）");

        // B3：类型判据空 ⇒ **兜底**（不是 null，也不是静默画成原版某一档）
        var unknownType = new ItemSpec { Id = "assert:no-type", Kind = ItemKind.Generic, Type = null };
        string fb = ItemDrawer.PickDrawer(unknownType, DrawerOverride.Default);
        Tr(!string.IsNullOrEmpty(fb), "B3 类型判据空 ⇒ 有兜底（实得 " + (fb ?? "null") + "），⛔ 不静默返回空");
    }

    // ============================================================ C · 版面：底图 / 尺寸 / 文本位
    static void SectionC_Layout()
    {
        MenuCheck.Section(_sink, "C 版面：共享抽屉尺（prefab 逐份实读，出处 = `ItemDrawerLayout.cs` 文件头）");

        var m = ItemDrawer.Metrics();

        // 尺寸：根（ARF 0.66 高控宽）与 Icon 变体根
        N(ItemDrawer.DrRootW, 712.8f, 0.01f, "C1 抽屉根宽 = 712.8 设计 px（ARF 0.66 × 1080）");
        N(ItemDrawer.DrRootH, 1080f, 0.01f, "C2 抽屉根高 = 1080");
        N(ItemDrawer.DrIconRoot, 1080f, 0.01f, "C3 Icon 变体根 = 1080²（ARF 1）");

        // Content：根内缩 0.4/边 ⇒ 712×1080
        N(m.ContentX1, 0.4f, 0.01f, "C4a `Content` 左沿 = 0.4");
        N(m.ContentX2, 712.4f, 0.01f, "C4b `Content` 右沿 = 712.4");
        N(m.ContentX2 - m.ContentX1, ItemDrawer.DrRootW - 0.8f, 0.01f,
          "C4c `Content` 宽 = 根宽 − 0.8（两处都从 prefab 来，写错任一个 ⇒ 红）");

        // 主图槽（多数档的默认矩形）
        N(m.ImageY1, 216f, 0.01f, "C5a `Content/Image` 上沿 = 216");
        N(m.ImageY2, 648f, 0.01f, "C5b `Content/Image` 下沿 = 648");

        // Label 行 + 底图名
        N(m.LabelY1, 810f, 0.01f, "C6a `Content/Label` 上沿 = 810");
        N(m.LabelY2, 918f, 0.01f, "C6b `Content/Label` 下沿 = 918（高 108）");
        N(m.LabelX1, 36f, 0.01f, "C6c `Label` 左沿 = 36");
        N(m.LabelX2, 676.8f, 0.01f, "C6d `Label` 右沿 = 676.8");
        C(m.BackgroundSprite, "40k_Rewards_icon_base_card",
          "C6e `Content/Background` 的 sprite 名（落盘名：空格→下划线；⛔ 别写成带空格的原名）");

        // 三条**派生关系**（= HLG 的 padding 25 / spacing 10）：两个常量被抄成同一个数时这里会红
        N(m.QtyX1 - m.LabelX1, 25f, 0.01f, "C7a `Quantity` 左沿 = `Label` 左沿 + HLG padding 25");
        N(m.LabelX2 - m.NameX2, 25f, 0.01f, "C7b `Name` 右沿 = `Label` 右沿 − HLG padding 25");
        N(m.NameX1 - m.QtyX2, 10f, 0.01f, "C7c `Name` 左沿 = `Quantity` 右沿 + HLG spacing 10（改成相等 ⇒ 红）");

        // 节点名（自检按它们取件 ⇒ 改名等于把断言改软）
        C(ItemDrawer.NodeContent, "Content", "C8a 节点名 `Content`");
        C(ItemDrawer.NodeBackground, "Background", "C8b 节点名 `Background`");
        C(ItemDrawer.NodeImage, "Image", "C8c 节点名 `Image`");
        C(ItemDrawer.NodeArmy, "Army", "C8d 节点名 `Army`");
        C(ItemDrawer.NodeName, "Name", "C8e 节点名 `Name`");
        C(ItemDrawer.NodeQuantity, "Quantity", "C8f 节点名 `Quantity`");
        C(ItemDrawer.NodeTrophyFrame, "Frame", "C8g 节点名 `Frame`（`Alliance Badge Drawer` 的档位框）");
        C(ItemDrawer.NodeTrophyIcon, "Trophy Icon", "C8h 节点名 `Trophy Icon`");
        C(ItemDrawer.NodeTitleText, "Title", "C8i 节点名 `Title`（`Title Drawer` 的称号正文）");
        C(ItemDrawer.NodeArmyIconExtra, "Army Icon Extra", "C8j 节点名 `Army Icon Extra`（`additionalArmyIconForEffect`）");
        C(ItemDrawer.NodeCollectedBadge, "Collected Badge", "C8k 节点名 `Collected Badge`（`claimedWarning`）");
    }

    // ============================================================ D · 灭自证
    static void SectionD_Discriminators()
    {
        MenuCheck.Section(_sink, "D 灭自证：变体档 / 稀有度顺序 / 版面常量都不能被合并掉");

        // D1：同一个类型的两档**必须是不同的实现**（否则「拿主档顶替变体」也全绿）
        Tr(ItemDrawer.DrawerForgePoint != ItemDrawer.DrawerForgePointIcon,
          "D1 `ForgePoints` 主档 vs Icon 档是两个实现（`ForgePointDrawer` / `ForgePointIconDrawer`）");
        Tr(ItemDrawer.DrawerWildcard != ItemDrawer.DrawerWildcardIcon,
          "D2 `Wildcard` 主档 vs Icon 档是两个实现");
        Tr(ItemDrawer.DrawerTitle != ItemDrawer.DrawerTitleIcon && ItemDrawer.DrawerTitle != ItemDrawer.DrawerTitleHorizontal
           && ItemDrawer.DrawerTitleIcon != ItemDrawer.DrawerTitleHorizontal,
          "D3 `CosmeticItemTitle` 三档（主 / Icon / Horizontal）互不相同");
        Tr(ItemDrawer.DrawerPremium != ItemDrawer.DrawerPremiumIcon,
          "D4 `PremiumItem` 族两档是两个实现（`PremiumDrawer` / `PremiumIconDrawer`）");

        // D5：稀有度 → 图名（施工单表 A1/A2 的 `cardRarity 1..4` 顺序）
        C(ItemDrawer.WildcardArtName(1), "40k_general_wildcard_common", "D5a `cardRarity 1` ⇒ common");
        C(ItemDrawer.WildcardArtName(2), "40k_general_wildcard_rare", "D5b `cardRarity 2` ⇒ rare");
        C(ItemDrawer.WildcardArtName(3), "40k_general_wildcard_epic", "D5c `cardRarity 3` ⇒ epic");
        C(ItemDrawer.WildcardArtName(4), "40k_general_wildcard_legendary", "D5d `cardRarity 4` ⇒ legendary");
        C(ItemDrawer.WildcardArtName(0), null, "D5e 域外（0）⇒ null（由 `WildcardIconTex` 那支 `FirstOrDefault` 兜底）");

        // D6：`WildcardIconFirstRarity` 只能返回那四条之一或 null（⛔ 不许编一个图名出来）
        string first = ItemDrawer.WildcardIconFirstRarity();
        Tr(first == null || first == "40k_general_wildcard_common" || first == "40k_general_wildcard_rare"
           || first == "40k_general_wildcard_epic" || first == "40k_general_wildcard_legendary",
          "D6 `WildcardIconFirstRarity()` 只返回四条之一或 null（实得 " + (first ?? "null") + "）");

        // D7：`Image` 默认矩形与 `Label` 行**不能是同一个矩形**（常量被合并的典型症状）
        var m = ItemDrawer.Metrics();
        Tr(m.ImageY1 != m.LabelY1 || m.ImageY2 != m.LabelY2,
          "D7 `Image` 默认矩形与 `Label` 行不是同一个矩形（Y " + m.ImageY1 + ".." + m.ImageY2 + " vs " + m.LabelY1 + ".." + m.LabelY2 + "）");
    }

    // ============================================================ E · 与映射表逐格对账（39 格，字面量）
    //
    //  为什么还要这一节：B 段是「`ItemTypeSets` ↔ `PickDrawer`」的一致性，**两边一起错会一起绿**；
    //  这一节把 `资料/普查产出_1004/ItemDrawerConfig_映射表.md` §② 那张表**逐格抄成字面量**（20 主档 + 19 档 override）
    //  ⇒ 谁把某一格改成主档（或改错类名），这里直接红。
    static readonly string[] _expectedCells =
    {
        // 类型 | override | 原版抽屉类（映射表 §② 逐行）
        "Currency|0|CurrencyDrawer",
        "Currency|10|CurrencyDrawer",
        "PlayerAvatar|0|AvatarDrawer",
        "PlayerAvatar|10|AvatarDrawer",
        "PlayerAvatar|20|AvatarDrawer",
        "Everguild.LiveOps.ShopContainer|0|ContainerDrawer",
        "Everguild.LiveOps.ShopContainer|10|ContainerDrawer",
        "RawCardScript|0|CardDrawer",
        "DropTableItem|0|RandomCardDrawer",
        "DropTableItem|10|RandomCardDrawer",
        "CosmeticItemCardback|0|CardbackDrawer",
        "Wildcard|0|WildcardDrawer",
        "Wildcard|10|WildcardIconDrawer",
        "CampaignPoints|0|CampaignPointDrawer",
        "CampaignPoints|10|CampaignPointDrawer",
        "ForgePoints|0|ForgePointDrawer",
        "ForgePoints|10|ForgePointIconDrawer",
        "Everguild.LiveOps.ExpansionPassPoints|0|ExpansionPassPointDrawer",
        "Everguild.LiveOps.ExpansionPassPoints|10|ExpansionPassPointDrawer",
        "PrebuiltDeck|0|DeckDrawer",
        "CosmeticItemTitle|0|TitleDrawer",
        "CosmeticItemTitle|10|TitleIconDrawer",
        "CosmeticItemTitle|15|TitleDrawerHorizontal",
        "CosmeticItemTitle|30|TitleDrawerHorizontal",
        "PremiumItem|0|PremiumDrawer",
        "PremiumItem|10|PremiumDrawer",
        "VIPPremiumItem|0|PremiumDrawer",
        "VIPPremiumItem|10|PremiumIconDrawer",
        "ExpansionPremiumItem|0|ExpansionPassPremiumDrawer",
        "ExpansionPremiumItem|10|ExpansionPassPremiumDrawer",
        "ExpansionPremiumItem|30|ExpansionPassPremiumDrawer",
        "AllianceTrophyData|0|AllianceBadgeDrawer",
        "AllianceTrophyData|10|AllianceBadgeDrawer",
        "CosmeticItemAvatarBorder|0|AvatarBorderDrawer",
        "CosmeticItemAvatarBorder|30|AvatarBorderDrawer",
        "CosmeticItemAvatarBorder|20|AvatarBorderDrawer",
        "XSollaBundleItem|0|XSollaOfferDrawer",
        "AlternateArtCard|0|CardAlternateArtDrawer",
        "GenericArmyItem|0|GenericArmyItemDrawer",
    };

    static void SectionE_AgainstMappingTable()
    {
        MenuCheck.Section(_sink, "E 逐格对账：`ItemTypeSets` × 映射表 §②（39 格字面量，改错一格就红）");

        C(_expectedCells.Length, 39, "E0 **前提**：期望表 = 39 格（20 主档 + 19 档 override，映射表 §①）");

        int bad = 0;
        string firstBad = null;
        for (int i = 0; i < _expectedCells.Length; i++)
        {
            string[] p = _expectedCells[i].Split('|');
            string type = p[0];
            int ov = int.Parse(p[1]);
            string want = p[2];

            ItemDrawer.ItemTypeSet e = default(ItemDrawer.ItemTypeSet);
            bool found = false;
            for (int k = 0; k < ItemDrawer.ItemTypeSets.Length; k++)
                if (ItemDrawer.ItemTypeSets[k].Type == type) { e = ItemDrawer.ItemTypeSets[k]; found = true; break; }
            if (!found) { bad++; if (firstBad == null) firstBad = type + "（表里没有这一条）"; continue; }

            string got = ov == 0 ? e.Main : e.Variant((DrawerOverride)ov);
            if (got != want) { bad++; if (firstBad == null) firstBad = type + "/" + ov + " got=" + (got ?? "null") + " want=" + want; }
        }
        C(bad, 0, "E1 `ItemTypeSets` 与映射表 **39 格全等**（第一个不符的：" + (firstBad ?? "—") + "）");
    }
}
