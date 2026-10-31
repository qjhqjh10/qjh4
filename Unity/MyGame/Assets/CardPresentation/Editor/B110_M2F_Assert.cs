// B110_M2F_Assert.cs — 第一百一十会话 · 执行代理 `B110_M2F`（2026-10-31）
//
// ============================ 它盯的是什么 ============================
//  `ItemDisplayWindow.Context` 的**另外两个生产者**（`B110_F2` 只修了抽屉点击那一路）：
//  每日奖励弹窗 `DailyRewardPopup.OpenItemInfo` 与 连登弹窗 `DailyStreakPopup.OpenItemInfo`
//  —— 两处原来**两格都没传**（`Amount` 落 struct 的 0、`ShowAmount` 落 false）。
//
//  生产路与这里的读口是**同一个方法**（`OpenItemInfo` 只转调 `ItemInfoContext`；⛔ 别在 `OpenItemInfo` 里再拼一份）
//  ⇒ 本件读到的那份 `Context` 就是真开进物品窗的那一份。
//
//  判据（三条，逐条对上）：
//   ① 第 2 个实参 = **数量**：`decomp_generic_trial/out/ItemDrawer_object___HandlePointerClick.c:15-25`
//      （`+0x38 = item` · `+0x40 = quantity`）⇒ 每日/连登两处原版也是同一段字节
//      （`out_il2csharp/Assembly-CSharp/DailyStreakItemContainer.cs:105-127`）。
//      数量来源 = `DailyData.RewardAmount` / `DailyData.StreakRewardSpecsOf(i)[0].Quantity`（= 发奖用的那一份）。
//   ② `ShowAmount` = **物品窗里现挑的那具抽屉**画不画数量行 —— `ItemDisplayWindow__Open.c:35-46`
//      → `decomp_full/ItemDrawer__Draw.c:22-46` → `ItemDrawerConfig.ItemDrawerReference__GetDrawer.c:19-33`
//      （`override == 0` ⇒ **取主档**）→ `ItemDrawer.HasQuantitySlot(主档那一族)`。
//      🔴 **不是**「奖励列表里那颗抽屉的族」：列表摆的是 `Icon Currency Drawer Variant`（`quantity` 全 null、不画），
//      而物品窗另挑主档 `Currency Drawer`（有数量槽、画）—— 两者答案不同。
//   ③ 原版 ctor 默认实参是 **1**，我们的 `Context` 是 struct ⇒ 不写就是 **0**，`BuildAmount()` 会原样印 `"0"`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A4/A5：`ShowAmountForReward` 恒 false ⇒ 红；写成「一律 true」⇒ A6（卡背那一族）红。
//  · B2/B3：`Amount` 不写 ⇒ 落 0 ⇒ 红（`!= 0` 那一半直接钉死「忘了给」）。
//  · B3 ★：换一天（150 → 20）⇒ 挡「恒写某个固定值」。
//  · D4：端到端那一扇窗里必须真印出 `"150"` —— 窗侧 `BuildAmount` 断了 / `ShowAmount` 没传进去 ⇒ 红。
//  · E：连登那边同样的形状（1 / 20），与每日奖励互为独立证据。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `Editor/<宿主>.cs` 那条聚合行**同一物理行、第一个 `//` 之前**追加（行数零变化）：
//      B110_M2F_Assert.Run(); pass += B110_M2F_Assert.Pass; fail += B110_M2F_Assert.Fail;
//  ⚠️ 本件**不需要**已建好的窗口/场景（自己 `ItemDisplayWindow.Create(null, ctx)` 建、跑完拆对象）；
//     `DailyRewardPopup` / `DailyStreakPopup` 只用它们的 `public static ItemInfoContext`（不实例化窗口）。
using System;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B110_M2F`：每日奖励 / 连登两处生产者的 `Context.Amount` / `ShowAmount`。</summary>
public static class B110_M2F_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B110M2F] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    const string ArtGold = "40k_topmarquee_currency_gold";
    const string ArtSkulls = "40K_missions_icon_Daily_skulls";
    const string ArtBooster = "40k_main_bt_rewards";

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B110_M2F：每日奖励 / 连登两处 `ItemDisplayWindow.Context`（原版 `(item, quantity)`）");

        SectionA_ShowAmountRule();
        SectionB_DailyProducer();
        SectionC_StreakProducer();
        SectionD_WindowRendersAmount();

        Debug.Log(_sink.P + "B110M2F 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · `ShowAmount` 那条判据
    static void SectionA_ShowAmountRule()
    {
        MenuCheck.Section(_sink, "A 「图标 → 原版类型 → 主档抽屉 → 画不画数量」= 原版 `ItemDisplayWindow__Open` → `ItemDrawer.Draw(…, override 0)`");

        C(DailyRewardPopup.RewardItemTypeOf(ArtGold), "Currency",
          "A1 金币图标 ⇒ 原版类型 `Currency`（= `ItemTypeSets` 的键；与 `DailyData.ItemTypeNameOfIcon` 同一张映射）");
        C(DailyRewardPopup.RewardItemTypeOf(ArtSkulls), "Currency",
          "A2 骷髅（每日任务点）图标 ⇒ `Currency`");
        C(DailyRewardPopup.RewardItemTypeOf(ArtBooster), "Everguild.LiveOps.ShopContainer",
          "A3 卡包图标 ⇒ `Everguild.LiveOps.ShopContainer`");

        string nullType = DailyRewardPopup.RewardItemTypeOf("B110M2F_NO_SUCH_ART");
        Tr(nullType == null, "A4 未知图标 ⇒ **null**（判据空；铁律 2：⛔ 不猜一个类型出来）");
        Tr(!DailyRewardPopup.ShowAmountForReward("B110M2F_NO_SUCH_ART"),
           "A5 未知图标 ⇒ `ShowAmount = false`（判据空就不画）");

        Tr(DailyRewardPopup.ShowAmountForReward(ArtGold),
           "A6 ★★ 金币 ⇒ `ShowAmount = true`（窗里现挑的是**主档** `Currency Drawer`：quantity 非 null、`Content/Label` active）"
         + "｜🧨 恒 false ⇒ 红（那会**比原版少画**一个数字）");
        Tr(DailyRewardPopup.ShowAmountForReward(ArtBooster),
           "A7 ★ 卡包 ⇒ `ShowAmount = true`（主档 `Container Drawer` 两道闸都过）");
        Tr(DailyRewardPopup.ShowAmountForReward(ArtSkulls), "A8 ★ 骷髅 ⇒ `ShowAmount = true`（同 `Currency` 那一族）");

        // 反例：判据不是「一律 true」
        Tr(!ItemDrawer.HasQuantitySlot(ItemDrawer.DrawerCardback),
           "A9 ★（反例）`CardbackDrawer` ⇒ **false**（quantity 非 null，但 `Content/Label` `m_IsActive = 0`）"
         + "｜🧨 把 `ShowAmountForReward` 做成「一律 true」⇒ 那族会多画一个数字");
        Tr(!ItemDrawer.HasQuantitySlot(ItemDrawer.DrawerAvatar),
           "A10（反例）`AvatarDrawer` ⇒ false（quantity 槽 `{fileID:0}`）");
    }

    // ============================================================ B · 每日奖励两处生产者
    static void SectionB_DailyProducer()
    {
        MenuCheck.Section(_sink, "B 每日奖励：`DailyRewardPopup.ItemInfoContext(day, premium)`（= `OpenItemInfo` 交给物品窗的那一份）");

        var d0 = DailyRewardPopup.ItemInfoContext(0, false);
        C(d0.Amount, DailyData.RewardAmount(0, false),
          "B1 ★★ 第 1 天（普通）`Amount` = `DailyData.RewardAmount(0,false)`（= 抽屉自己的 `+0x40`；同一份数量也是 `CollectReward` 发的）");
        Tr(d0.Amount != 0,
           "B2 ★★ `Amount != 0`（struct 默认值就是 0 ⇒ 不写这两处就印 `\"0\"`）｜🧨 两格都不传 ⇒ 红。实测 = " + d0.Amount);
        Tr(d0.ShowAmount,
           "B3 ★★ 第 1 天 `ShowAmount = true`（金币 ⇒ 主档 `Currency Drawer` 画数量行）｜🧨 漏传 ⇒ 红");
        C(d0.Title, DailyData.RewardName(0, false), "B4 标题仍是那一格的奖励名（⛔ 别为了塞数量改标题）");
        C(d0.ArtName, DailyData.RewardIconOf(0, false), "B5 图仍是抽屉图标那张");

        var d1 = DailyRewardPopup.ItemInfoContext(1, false);
        C(d1.Amount, DailyData.RewardAmount(1, false),
          "B6 ★（灭自证）第 2 天（卡包）`Amount` = 那一天自己的数量 ⇒ 挡「恒写某个固定值」");
        Tr(d1.Amount != d0.Amount,
          "B7 ★（灭自证）两天的数量**不同**（" + d0.Amount + " vs " + d1.Amount + "）⇒ 值真的来自那一天");
        Tr(d1.ShowAmount, "B8 第 2 天（卡包 ⇒ 主档 `Container Drawer`）`ShowAmount` 也是 true");

        var d0p = DailyRewardPopup.ItemInfoContext(0, true);
        C(d0p.Amount, DailyData.RewardAmount(0, true),
          "B9 ★ Premium 轨同一格 `Amount` = Premium 那一份数量（⛔ 别把普通轨的数带过去）");
        Tr(d0p.Amount != d0.Amount, "B10 ★（灭自证）普通轨与 Premium 轨的数量不同 ⇒ 轨道参数真的被用上了");
    }

    // ============================================================ C · 连登两处生产者
    static void SectionC_StreakProducer()
    {
        MenuCheck.Section(_sink, "C 连登：`DailyStreakPopup.ItemInfoContext(day)`（原版与每日奖励是同一段字节）");

        var specs0 = DailyData.StreakRewardSpecsOf(0);
        Tr(specs0 != null && specs0.Length > 0, "C0 **前提**：连登第 1 格有奖励条目（空表 ⇒ 下面三条无从验）");
        if (specs0 == null || specs0.Length == 0) return;

        var s0 = DailyStreakPopup.ItemInfoContext(0);
        C(s0.Amount, specs0[0].Quantity,
          "C1 ★★ 第 1 格 `Amount` = `StreakRewardSpecsOf(0)[0].Quantity`（= 原版抽屉 `+0x40`；同一份也是 `CollectStreak` 发的）");
        Tr(s0.ShowAmount, "C2 ★★ 第 1 格 `ShowAmount = true`（卡包 ⇒ 主档 `Container Drawer`）｜🧨 漏传 ⇒ 红");

        var s2 = DailyStreakPopup.ItemInfoContext(2);
        C(s2.Amount, DailyData.StreakRewardSpecsOf(2)[0].Quantity,
          "C3 ★（灭自证）第 3 格 `Amount` = 那一格自己的数量");
        Tr(s2.Amount != s0.Amount, "C4 ★（灭自证）两格数量不同（" + s0.Amount + " vs " + s2.Amount + "）⇒ 值真的来自那一格");
        C(s2.ArtName, DailyData.StreakRewardIcon(2), "C5 图仍是奖格图标那张");
    }

    // ============================================================ D · 端到端：窗里真印出那个数字
    static void SectionD_WindowRendersAmount()
    {
        MenuCheck.Section(_sink, "D 端到端：`ItemDisplayWindow.Create(null, ctx)` ⇒ 数量那一格真的印出来");

        ItemDisplayWindow w = null;
        try
        {
            var ctx = DailyRewardPopup.ItemInfoContext(0, false);
            w = ItemDisplayWindow.Create(null, ctx);
            Tr(w != null, "D0 **前提**：物品窗建得出来（`ItemDisplayWindow.Create`）");
            if (w == null) return;

            C(w.Ctx.Amount, ctx.Amount, "D1 ★ 窗里拿到的 `Ctx.Amount` = 注入的那一份（没在中途被换掉）");
            Tr(w.Ctx.ShowAmount, "D2 ★ 窗里 `Ctx.ShowAmount = true`（生产路那一份真的走到了窗）");
            Tr(w.AmountLabel != null,
               "D3 ★ 前提：`ShowAmount = true` ⇒ 数量那一格建出来了（`AmountLabel != null`；建不出来 `BuildAmount` 会出声）");
            if (w.AmountLabel != null)
                C(w.AmountLabel.Text, ctx.Amount.ToString(),
                  "D4 ★★ 那一格印 **\"" + ctx.Amount + "\"**（原版 `quantity.setText(amount.ToString())`，裸数字无 `x` 前缀）"
                + "｜🧨 `Amount` 没传进 `Context` ⇒ 印 `\"0\"` ⇒ 红");
        }
        catch (Exception e) { Tr(false, "D 段抛异常：" + e.GetType().Name + "：" + e.Message); }
        finally { Kill(ref w); }
    }

    // ============================================================ 辅助
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
}
