// B110_F2_Assert.cs — 第一百一十会话 · 执行代理 `B110_F2`（2026-10-31）
//
// ============================ 它盯的是什么 ============================
//  「原版 `quantity` 传进物品窗」这**最后一跳**：`ItemDrawerView.HandlePointerClick()` 里那个
//  `ItemDisplayWindow.Context` 初始化器**补了 `Amount` / `ShowAmount` 两格**。
//
//  判据（三条，逐条对上）：
//   ① 原版实参 = **`(item, quantity)`**：`decomp_full/ItemDrawer_object___HandlePointerClick.c` 里
//      `uVar1 = *(undefined4*)(param_1+0x40)`（抽屉实例的 `+0x40` = `amount`）才是第 2 个实参；
//      **末尾那个字面 `0` 不是实参**（它是 ICF 折叠后 ctor 的第 4 个寄存器位）。同一 ctor 的签名桩也写着
//      `ItemDisplayWindowContext(ObtainableItem item, int amount = 1)`。
//   ② 原版 ctor 的默认实参是 **1**，而我们的 `Context` 是 **`struct`**（表达不了默认实参）⇒ 不写就是 **0**。
//      `BuildAmount()` 会把 0 原样印成 `"0"` ⇒ 「忘了给」与「真的 0」在画面上分不开。
//   ③ 「画不画那一行」= 该族抽屉 prefab 的 `ItemDrawerComponents.quantity != null`
//      **且** `Content/Label` 在激活链上（`ItemDrawer.HasQuantitySlot` 那张表；逐份 prefab 现读）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · B4：把 `ItemDrawer.cs` 那行 `Amount = Quantity` 删掉 ⇒ `Context.Amount` 落到 **0** ⇒ B4 红（B5/B6 一起红）。
//  · B7：把数量从 3 换成 7 再走一遍 ⇒ `Amount` / 文本必须跟着变成 7（挡「恒写某个固定值」）。
//  · B9：`ShowAmount` 恒写 `false` ⇒ B9 红；恒写 `true` ⇒ C3 红（卡背那一族原版**不画**）。
//  · C 段：`ForCardback` / `CardbackDrawer` 两路 —— `Amount` 照样传（4 / 1）、但 `ShowAmount` 必须是 **false**，
//    且物品窗里**没有**那颗数量文本（`AmountLabel == null`）。把 `HasQuantitySlot` 写成「一律 true」⇒ 红。
//  · A 段那张表是 `HasQuantitySlot` 的**镜像断言**（值本身来自 prefab 现读，见 `ItemDrawer.HasQuantitySlot`
//    的 doc）：改表不改判据 / 改判据不改表 ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `Editor/<宿主>.cs` 那条聚合行**同一物理行、第一个 `//` 之前**追加（行数零变化）：
//      B110_F2_Assert.Run(); pass += B110_F2_Assert.Pass; fail += B110_F2_Assert.Fail;
//  ⚠️ 本件**不需要**任何已建好的窗口/场景（自己 `WindowsManager.Get()` 建壳、跑完关窗拆对象），
//     所以接哪个宿主都行；推荐 `Editor/RewardsScene.cs`（那一族本来就会遍历奖励抽屉）。
//  ⚠️ 壳侧窗口管理器会被**建出来**（`WindowsManager.Get()` 幂等），与 `B109_E23_DailyClick_Assert` 同一套做法。
using System;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B110_F2`：抽屉点击 ⇒ 物品窗的 `Amount` / `ShowAmount`（原版 `ItemDisplayWindowContext(item, quantity)`）。</summary>
public static class B110_F2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B110F2] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>抽屉根的设计尺寸（同 `C4_Assert.BoxW/H`：`ItemDrawer.DrRootW/H`）。</summary>
    const float BoxW = 712.8f, BoxH = 1080f;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B110_F2：抽屉点击 ⇒ 物品窗 `Amount` / `ShowAmount`（原版 `ItemDisplayWindowContext(item, quantity)`）");

        SectionA_Table();
        SectionB_ProducerEndToEnd();
        SectionC_CounterExamples();

        Debug.Log(_sink.P + "B110F2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 「抽屉族 → 有没有数量槽」那张表
    static void SectionA_Table()
    {
        MenuCheck.Section(_sink, "A 「抽屉族 → 有没有数量槽」= prefab 现读（`ItemDrawerComponents.quantity` + `Content/Label` 的 `m_IsActive`）");

        // 两道闸都过（数量槽非 null 且 Content/Label active）
        Tr(ItemDrawer.HasQuantitySlot(ItemDrawer.DrawerContainer), "A1 `ContainerDrawer` ⇒ **有**数量槽（`Container Drawer`：quantity 非 null · Label active）");
        Tr(ItemDrawer.HasQuantitySlot(ItemDrawer.DrawerWildcard),  "A2 `WildcardDrawer` ⇒ **有**（`Wildcard Drawer`）");
        Tr(ItemDrawer.HasQuantitySlot(ItemDrawer.DrawerCurrency),  "A3 `CurrencyDrawer` ⇒ **有**（`Currency Drawer`：`label` 槽 null 但 `Content/Label` 节点 active —— 两件事）");

        // 闸①不过：数量槽 null
        Tr(!ItemDrawer.HasQuantitySlot(ItemDrawer.DrawerAvatar),        "A4 `AvatarDrawer` ⇒ **没有**（`Avatar Drawer`：quantity = {fileID:0}）");
        Tr(!ItemDrawer.HasQuantitySlot(ItemDrawer.DrawerCard),          "A5 `CardDrawer` ⇒ **没有**（`Card Drawer`：`background/image/label/quantity` 全 null）");
        Tr(!ItemDrawer.HasQuantitySlot(ItemDrawer.DrawerPremium),       "A6 `PremiumDrawer` ⇒ **没有**（quantity null）");

        // 闸②不过：数量槽非 null，但 `Content/Label` 的 `m_IsActive = 0` —— **关键反例**
        Tr(!ItemDrawer.HasQuantitySlot(ItemDrawer.DrawerCardback),
           "A7 ★ `CardbackDrawer` ⇒ **没有**（quantity **非 null**、TMP 出厂文本 `'2'`，但 `Content/Label` `m_IsActive = 0` ⇒ 原版写进去也看不见）");
        Tr(!ItemDrawer.HasQuantitySlot(ItemDrawer.DrawerTitleHorizontal),
           "A8 ★ `TitleDrawerHorizontal` ⇒ **没有**（quantity 非 null、Label 关）");

        // 判据空 / 我们自己那一档
        Tr(!ItemDrawer.HasQuantitySlot("B110F2_NO_SUCH_DRAWER"),
           "A9 未知族 ⇒ **false**（铁律 2：判据没读到就按不画办 + 出声，⛔ 不猜）");
    }

    // ============================================================ B · 端到端：抽屉点击 ⇒ 物品窗
    static void SectionB_ProducerEndToEnd()
    {
        MenuCheck.Section(_sink, "B 端到端：`ItemDrawer.Draw(…, 数量 3)` → `view.HandlePointerClick()` → `ItemDisplayWindow.Context`");

        var root = new GameObject("B110F2_DRAWER_ROOT");
        ItemDisplayWindow w3 = null, w7 = null;
        try
        {
            // ===== 数量 3 =====
            var view = DrawContainer(root.transform, 3, out string drawer);
            Tr(view != null, "B0 **前提**：`ContainerDrawer` 那一格建出来了（`ItemDrawer.Draw` 的 `View != null`）");
            if (view == null) return;
            C(drawer, ItemDrawer.DrawerContainer, "B0b **前提**：路由到 `ContainerDrawer`（`Everguild.LiveOps.ShopContainer` 那一档）");
            C(view.QuantityForTest, 3, "B0c **前提**：句柄上留着的数量 = 3（原版实例字段 `+0x40`）");

            // `ContainerDrawer` 是混合档（`ShowInfoOnClickOf` 给 0）⇒ 先照调用方的做法把闸打开（原版逐 prefab 给 1）
            view.SetShowInfoOnClick(true);
            Tr(view.ShowInfoOnClick, "B0d **前提**：点击闸已打开（否则 `HandlePointerClick` 走不到）");

            var before = ItemDisplayWindow.LastOpened;
            if (before != null && before.CurrentState != WindowState.Closed) before.Close();

            view.HandlePointerClick();
            w3 = ItemDisplayWindow.LastOpened;
            Tr(w3 != null && !ReferenceEquals(w3, before),
               "B1 ★ 点一下 ⇒ **新开出一扇** `Item Display Window`（原版 `HandlePointerClick()` → `ItemDisplayWindowContext(item, quantity)`）");
            if (w3 == null) return;

            C(w3.Ctx.Amount, 3,
              "B4 ★★ `Context.Amount` = 抽屉自己的数量 **3**（原版第 2 个实参 = `*(undefined4*)(param_1+0x40)`）"
            + "｜🧨 删掉 `ItemDrawer.cs` 那行 `Amount = Quantity` ⇒ 落到 struct 的 **0** ⇒ 红");

            Tr(w3.Ctx.ShowAmount,
               "B5 ★★ `Context.ShowAmount = true`（`ContainerDrawer` 那一族的抽屉**会**画数量行）"
            + "｜🧨 `ShowAmount` 恒 false ⇒ 红");

            Tr(w3.AmountLabel != null, "B6 **前提**：`ShowAmount = true` ⇒ 数量那一格建出来了（`AmountLabel != null`）");
            if (w3.AmountLabel != null)
                C(w3.AmountLabel.Text, "3",
                  "B7 ★ 那一格写 **`3`**（原版 `quantity.setText(amount.ToString())`，裸数字、无 `x` 前缀）");

            // ===== 灭自证：换一组相反的值（3 → 7）=====
            w3.Close(); MainMenuSubmenuWindow.DestroySafe(w3.gameObject); w3 = null;

            var view7 = DrawContainer(root.transform, 7, out drawer);
            Tr(view7 != null, "B8 **前提**：第二格（数量 7）建出来了");
            if (view7 == null) return;
            view7.SetShowInfoOnClick(true);
            view7.HandlePointerClick();
            w7 = ItemDisplayWindow.LastOpened;
            Tr(w7 != null, "B8b **前提**：第二格点出了物品窗");
            if (w7 == null) return;

            C(w7.Ctx.Amount, 7, "B9 ★（灭自证）数量 7 ⇒ `Context.Amount == 7`（挡「恒写某个固定值」）");
            Tr(w7.Ctx.ShowAmount, "B10（灭自证）同一族 ⇒ `ShowAmount` 仍是 true");
            if (w7.AmountLabel != null) C(w7.AmountLabel.Text, "7", "B11 ★（灭自证）那一格写 `7`");
            else Tr(false, "B11 **前提不成立**：数量 7 那一格的文本没建出来（`AmountLabel == null`）");
        }
        catch (Exception e) { Tr(false, "B 段抛异常：" + e.GetType().Name + "：" + e.Message); }
        finally
        {
            Kill(ref w3); Kill(ref w7);
            DestroySafe(root);
        }
    }

    // ============================================================ C · 反例：原版**不画**数量的两族
    static void SectionC_CounterExamples()
    {
        MenuCheck.Section(_sink, "C 反例：卡背一族 —— `Amount` 照传、但 `ShowAmount` 必须 false（原版那两族的数量行不画）");

        // ---- C-1 生产路：`CardbackDrawer`（quantity 非 null，但 `Content/Label` 关）
        var root = new GameObject("B110F2_CARDBACK_ROOT");
        ItemDisplayWindow w = null;
        try
        {
            var st = ItemDrawerStyle.Default(10, 11, 12);
            var spec = new ItemSpec
            {
                Id = "B110F2_CardbackSpec",
                Type = "CosmeticItemCardback",      // ⇒ `PickDrawer` 落到 `CardbackDrawer`
                Kind = ItemKind.Generic,
                Label = "B110F2",
            };
            var res = ItemDrawer.Draw(root.transform, new PxRect(0f, 0f, BoxW, BoxH), spec, 4, DrawerOverride.Default, st);
            C(res.Drawer, ItemDrawer.DrawerCardback, "C0 **前提**：路由到 `CardbackDrawer`");
            Tr(res.View != null, "C0b **前提**：卡背那一格建出来了");
            if (res.View == null) return;

            res.View.SetShowInfoOnClick(true);
            var before = ItemDisplayWindow.LastOpened;
            if (before != null && before.CurrentState != WindowState.Closed) before.Close();

            res.View.HandlePointerClick();
            w = ItemDisplayWindow.LastOpened;
            Tr(w != null && !ReferenceEquals(w, before), "C1 **前提**：卡背那一格也点得出物品窗");
            if (w == null) return;

            C(w.Ctx.Amount, 4,
              "C2 `Context.Amount` **照样** = 4（原版把 quantity 交给窗，画不画是抽屉 prefab 的事 —— 两件事别混）");
            Tr(!w.Ctx.ShowAmount,
               "C3 ★★ `CardbackDrawer` ⇒ `ShowAmount = false`（`Content/Label` `m_IsActive = 0`）"
            + "｜🧨 `HasQuantitySlot` 写成「一律 true」⇒ 红（那会**比原版多画一个数字**）");
            Tr(w.AmountLabel == null, "C4 ★ 物品窗里**没有**那颗数量文本（`AmountLabel == null`）");
        }
        catch (Exception e) { Tr(false, "C 段抛异常：" + e.GetType().Name + "：" + e.Message); }
        finally { Kill(ref w); DestroySafe(root); }

        // ---- C-2 `ForCardback` 那一跳（原版 `ItemDisplayWindowContext(item, 1)`）
        string cb = FirstCardbackName();
        Tr(cb != null, "C5 **前提**：`Resources/Cardbacks.json` 里至少有一条卡背（否则下面三条无从验）");
        if (cb != null)
        {
            var ctx = ItemDisplayWindow.ForCardback(cb);
            C(ctx.Amount, 1, "C6 `ForCardback` 的 `Amount = 1`（= 原版 `CardbackCollectionTab__OnItemSelected` 的实参）");
            Tr(!ctx.ShowAmount, "C7 ★ `ForCardback` 的 `ShowAmount = false`（卡背抽屉那一行整组不激活）");

            ItemDisplayWindow w2 = null;
            try
            {
                w2 = ItemDisplayWindow.Create(null, ctx);
                Tr(w2 != null, "C8 **前提**：`ItemDisplayWindow.Create(null, ctx)` 建得出来");
                if (w2 != null) Tr(w2.AmountLabel == null, "C9 ★ `ForCardback` 那一扇里**没有**数量文本");
            }
            finally { Kill(ref w2); }
        }
    }

    // ============================================================ 辅助

    /// <summary>画一格 `ContainerDrawer`（数量由调用方给），返回它的句柄。</summary>
    static ItemDrawerView DrawContainer(Transform parent, int quantity, out string drawer)
    {
        var st = ItemDrawerStyle.Default(10, 11, 12);
        var spec = new ItemSpec
        {
            Id = "B110F2_ContainerSpec",
            Type = "Everguild.LiveOps.ShopContainer",   // ⇒ `PickDrawer` 落到 `ContainerDrawer`
            Kind = ItemKind.Generic,
            Label = "B110F2",
        };
        var res = ItemDrawer.Draw(parent, new PxRect(0f, 0f, BoxW, BoxH), spec, quantity, DrawerOverride.Default, st);
        drawer = res.Drawer;
        return res.View;
    }

    /// <summary>从 `Resources/Cardbacks.json` 取第一条卡背的图名（**独立读数据**，不走被测的取数口）。</summary>
    static string FirstCardbackName()
    {
        var ta = Resources.Load<TextAsset>(CardbackTable.ResourcePath);
        if (ta == null) return null;
        var f = JsonUtility.FromJson<CbFile>(ta.text);
        return f != null && f.items != null && f.items.Length > 0 ? f.items[0].name : null;
    }

    [Serializable]
    class CbFile { public CardbackTable.Item[] items; }

    static void Kill(ref ItemDisplayWindow w)
    {
        if (w == null) return;
        try
        {
            w.Close();
            MainMenuSubmenuWindow.DestroySafe(w.gameObject);
        }
        catch { /* 已关掉/已销毁：批处理下不算缺陷 */ }
        w = null;
    }

    static void DestroySafe(UnityEngine.Object o)
    {
        if (o == null) return;
        try
        {
            if (o is Component) UnityEngine.Object.DestroyImmediate(((Component)o).gameObject);
            else UnityEngine.Object.DestroyImmediate(o);
        }
        catch { /* 批处理下已销毁的对象再 DestroyImmediate 会抛 —— 不算缺陷 */ }
    }
}
