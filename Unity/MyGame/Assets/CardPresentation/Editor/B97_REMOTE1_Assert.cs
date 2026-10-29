// B97_REMOTE1_Assert.cs —— 执行代理 `B97_REMOTE1`（第九十七会话 · 商店 7 条 REMOTE）
//
// ============================ 它盯的是什么 ============================
// 逐条结论 → `资料/完全复刻/报告/B97_REMOTE1.md`；账 → `账/_parts/B97_REMOTE1.tsv`。
// 口径 = CLAUDE.md §18·0：**只有和服务器有关的才允许自己设计**（服务器已关服、判据在远端）
//   ⇒ 这 7 条一律「客户端 UI 照原版 + 数据本地模拟 + 出声」，本地那一格打 `🔶 自己设计（非原版）`。
// 本文件只断**本批真落地的四条**（89 / 91 / 92 / 203）：
//   · **R**（账 `商店:89`/`91`/`92`）`ExpansionPassPremiumOffers` 的本地报价表 + 两条关窗回调：
//       ① 空表 ⇒ `PurchasePremiumClick()` 返回 false + 出声（原版第 ⑤ 句那条 `LogWarning` 的形状）
//       ② 有表 ⇒ 真把 `PurchasePremiumWindow` 开出来 + 两条回调挂到 `GameWindow.OnCloseWindow`
//       ③ 关窗 ⇒ `b__12_0`（重跑 `ApplyPremiumPanel`）**先**跑、`b__12_1`（转出 `onClosePurchasePopup`）**后**跑
//   · **S**（账 `商店:203`）本地购买失败支：`ShopData.BuyNotFound` 哨兵 = 原版 `onFailPurchase` 的**唯一**本地成因；
//       `ErrorPopupWindowController` 里 `10` 必须是 `InternetUnreachable`（原版 `.c` 里写死 `ShowErrorMessage(10)`）。
//   ⛔ **`商店:102`/`108`/`112` 没有断言**：那三处的落点文件 `Shell/GachaEventWindow.cs` 本批在别人名下（`B97_W2`），
//      代码没落地 ⇒ 断言写在这里就是「测不存在的实现」。补丁与断言片段都在报告 §四，由调度台转派后一起接。
//
// ============================ 怎么用（宿主）============================
// `B97REMOTE1Assert.Run();`
//   + 宿主里两行（同族先例 `B97SHOPAssert` / `ShopFamilyAssertE13`）：
//     `_sink.Pass += B97REMOTE1Assert.Pass; _sink.Fail += B97REMOTE1Assert.Fail;`
//   · 不跑 Unity 批处理、不建持久资产；临时件全在 `finally` 里 `DestroyImmediate` + `ResetForTest()`。
//   · ⚠️ **R6 起会真的开一扇 `PurchasePremiumWindow`**（走生产那条 `WindowsManager.OpenPurchasePremiumWindow`）
//     ⇒ 这个断言组要在**有 `WindowsManager` 的宿主**里跑（`ShopScene` / `ShellScene` 都行）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · R3：把 `SayCannotOpen` 那一支改成 `return true`（「空表也当成功」）⇒ R3 红。
//   · R6：把 `WireClose` 里两条 `+=` 删掉 ⇒ R7/R8 的计数停在 0 ⇒ 红。
//   · R8/R9：把 `NotifyParent` 改成「null 也照样调」⇒ `notified.Count` 会是 2 而
//     `NotifyParentNullCalls` 不是 1 ⇒ R9 红。
//   · R12 ★★ 灭自证：把 `WireClose` 里两条 `+=` 的**先后对调** ⇒ 次序串变 `"NR"` ⇒ R12 红
//     （R7 / R8 两个计数都还是 1，单看它们看不出来）。
//   · S2：把 `CustomError.InternetUnreachable` 的枚举值从 10 改成别的（原版 `.c` 写死 10）⇒ S4 红。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B97_REMOTE1`：商店 4 条 REMOTE 的断言（89 / 91 / 92 / 203）。
/// <para>⛔ **期望值全是字面量**（`10` / `false` / `1` / `"越界"` / `"RN"`），不从被测实现里读回来。</para></summary>
public static class B97REMOTE1Assert
{
    static readonly CheckSink _sink = new CheckSink("[REMOTE1] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B97_REMOTE1 商店 REMOTE 四条：付费档报价表 / 两条关窗回调 / 购买失败支");
        SectionR_付费档();
        SectionS_购买失败支();
    }

    // ------------------------------------------------------------ R 账 商店:89 / 91 / 92
    static void SectionR_付费档()
    {
        MenuCheck.Section(_sink, "R 账 商店:89/91/92 —— `ExpansionPassPremiumOffers` + `GameWindow.OnCloseWindow` 上那两条");

        ExpansionPassPremiumOffers.ResetForTest();
        ExpansionPassPremiumOffers.ResetCountersForTest();

        // ① 出厂表 = 一条通配档，聚焦阵营 = 原版 `TryOpen` 的缺省 `Ultramarines = 10`
        var any = ExpansionPassPremiumOffers.ForEvent(null);
        Tr(any.HasValue, "R1 前提：出厂表里有**通配档**（本地常态 `_event == null` 也要能打开付费窗 —— 否则这条 REMOTE 一直挂着）");
        if (!any.HasValue) return;                     // ⚠️ 安全：R1 已经红了，不会静默
        C(any.Value.FocusArmy, 10, "R2 通配档的聚焦阵营 = 10（`Ultramarines`，原版 `PurchasePremiumWindow__TryOpen.c` 的 `?? Ultramarines(10)`）");

        var go = new GameObject("B97REMOTE1_ExpansionPass");
        var panel = go.AddComponent<ExpansionPassEventWindow>();
        try
        {
            // ② 空表 ⇒ 打不开 + 出声（原版第 ⑤ 句那条 `LogWarning` 带事件 id 的形状）
            ExpansionPassPremiumOffers.ClearForTest();
            C(ExpansionPassPremiumOffers.Count, 0, "R3 前提：`ClearForTest()` 之后表是空的");
            int clicks0 = panel.PurchasePremiumClicks;
            bool opened0 = panel.PurchasePremiumClick();
            C(opened0, false, "R3b ★ 表空 ⇒ `PurchasePremiumClick()` 返回 **false**（⛔ 不许「空表也当成功」）");
            C(panel.PurchasePremiumClicks - clicks0, 1, "R4 前提：点了一次（读口 `PurchasePremiumClicks` 也真的 +1 —— 原版第一句就是那个计数）");
            C(ExpansionPassPremiumOffers.CannotOpenCalls, 1,
              "R5 ★ 空表那一支走了 `SayCannotOpen`（= 原版第 ⑤ 句 `LogWarning(… + premiumOfferData.GetID())`；本工程红线：不许静默失败）");

            // ③ 出厂表恢复 ⇒ 真开窗 + 挂两条回调
            ExpansionPassPremiumOffers.ResetForTest();
            bool opened1 = panel.PurchasePremiumClick();
            Tr(opened1, "R6 ★★ 表里有一档 ⇒ 付费窗**真开出来了**（原版 ③④ 那句 `GetComponentReference(…, 0xc)` + `OpenWindow`）");
            var win = PurchasePremiumWindow.LastOpened;
            Tr(win != null, "R6b ★ 开出来的是 `PurchasePremiumWindow`（本仓对位 = 原版那扇远端 prefab）");
            if (!opened1 || win == null) return;
            Tr(win.gameObject.activeSelf, "R6c 前提：窗是活的（`GameWindow.Hide()` 首句 `if (!gameObject.activeSelf) return;`）");

            // 关窗 ⇒ 两条回调都要跑（`GameWindow.Hide()` 末句 Invoke `OnCloseWindow`）
            int notified = 0;
            panel.ApplyPremiumPanel(() => { notified++; });
            ExpansionPassPremiumOffers.ResetCountersForTest();
            win.Hide();

            C(ExpansionPassPremiumOffers.ReinitializeCalls, 1,
              "R7 ★★ 关窗 ⇒ `b__12_0` 跑了 1 次（原版全文一句：`Initialize(this.data, this.onClose)` = 重跑面板）");
            C(notified, 1,
              "R8 ★★ 关窗 ⇒ `b__12_1` 真的把 `onClosePurchasePopup`（原版 `+0x60`）转出去 1 次");
            C(ExpansionPassPremiumOffers.NotifyParentNullCalls, 0,
              "R9 ★ 两态对照：这一次 `onClosePurchasePopup` **非 null** ⇒ 不该落「什么都不做」那一支");
            C(ExpansionPassPremiumOffers.Order, "RN",
              "R12 ★★ 灭自证：两条回调的**实际次序** = 先 `b__12_0`（R）后 `b__12_1`（N）—— 原版两次 `Delegate.Combine` 的顺序；"
              + "把两条 `+=` 对调这一条就变 `\"NR\"`（R7/R8 两个计数仍是 1，单看它们看不出来）");

            // ④ `onClose` 置 null ⇒ `b__12_1` 落「什么都不做」那一支（原版 `if (lVar1 != 0)` 的反面）
            panel.ApplyPremiumPanel(null);
            ExpansionPassPremiumOffers.ResetCountersForTest();
            win.UnHide();                    // `Hide()` 之后物体不活 ⇒ 先回前台，`Close()` 才走生产那条路
            win.Close();                     // 生产关窗链：`Close → manager.CloseWindow → … → Hide()`
            C(ExpansionPassPremiumOffers.ReinitializeCalls, 1, "R10 顺序①：`Close()` 之后 `b__12_0` 又跑了 1 次");
            C(ExpansionPassPremiumOffers.NotifyParentNullCalls, 1,
              "R11 ★ `onClose` 为 null ⇒ `b__12_1` 落「什么都不做」那一支（原版 `if (lVar1 != 0)` 的反面），但**回调本身仍然被调到了**");
            C(ExpansionPassPremiumOffers.Order, "RN", "R12b `onClose` 为 null 这一档的次序同样是 `\"RN\"`（次序与回调内容无关）");
        }
        finally
        {
            ExpansionPassPremiumOffers.ResetForTest();
            ExpansionPassPremiumOffers.ResetCountersForTest();
            Object.DestroyImmediate(go);
        }
    }

    // ------------------------------------------------------------ S 账 商店:203
    static void SectionS_购买失败支()
    {
        MenuCheck.Section(_sink, "S 账 商店:203 —— 本地购买失败支（原版 `OnPurchasedFailed`）");

        // ① 哨兵契约：越界 ⇒ 失败；正常那一句 ⇒ 成功
        C(ShopData.BuyNotFound, "越界",
          "S1 哨兵 = `\"越界\"`（`ShopData.Buy` 在「这一件本地不存在」时返回的那一句 —— 这条字面量改了口径就得改账）");
        C(ShopData.PurchaseSucceeded(ShopData.BuyNotFound), false, "S2 ★ 哨兵 ⇒ 判**失败**（原版 `onFailPurchase` 那一支走不走就看它）");
        C(ShopData.PurchaseSucceeded("Ultramarines Booster ×2"), true, "S3 ★ 正常那一句 ⇒ 判成功（⛔ 别把 `null`/空串当失败，那是另一回事）");

        // ② 原版那格常量：`ShowErrorMessage(10)` 的 10 = `CustomError.InternetUnreachable`
        C((int)CustomError.InternetUnreachable, 10,
          "S4 ★ 原版 `PurchasePremiumWindow__OnPurchasedFailed.c` 头一句写死 `ShowErrorMessage(10)` ⇒ 我们枚举里 10 必须是 `InternetUnreachable`");
        Tr(ErrorPopupWindowController.HasTerm(CustomError.InternetUnreachable),
          "S5 词条 `CustomErrors/InternetUnreachable` 在本地化表里（否则 `ShowErrorMessage` 会回落 + 出声）");

        // ③ 失败支的读口
        ShopTabPage.ResetPurchaseFailForTest();
        C(ShopTabPage.PurchaseFailedCalls, 0, "S6 前提：失败支计数清零（`ResetPurchaseFailForTest`）");
        C(ShopTabPage.LastPurchaseFailResult, (string)null, "S7 前提：失败支的「最近一次返回值」也清掉了（否则会跨段污染）");
    }
}
