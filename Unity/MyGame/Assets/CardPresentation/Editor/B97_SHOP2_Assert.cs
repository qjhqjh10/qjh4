// B97_SHOP2_Assert.cs —— 执行代理 `B97_SHOP2`（第九十七会话 · 商店 REMOTE 三条 + 限购售罄闸）
//
// ============================ 它盯的是什么 ============================
// 逐条结论 → `资料/完全复刻/报告/B97_SHOP2.md`；账 → `账/_parts/B97_SHOP2.tsv`。
// 口径 = CLAUDE.md §18·0：**只有和服务器有关的才允许自己设计**（服务器已关服、判据在远端）
//   ⇒ `商店:102` / `108` / `112` 三条 = 客户端 UI 照原版 + **数据本地模拟 + 出声**，本地那一格打 `🔶 自己设计（非原版）`。
//
//   · **A**（账 `商店:297`）**限购售罄闸** —— 原版 `CatalogItemContainer__Refresh.c:45-51`（`max < 1 ⇒ 可买`，
//        否则 `已购数 < max` 才可买）/ `:65-77`（`Selectable.set_interactable`）：售罄时**按钮不可点**。
//        我们这一侧 = `ShopData.SoldOut` + `ShopData.Buy` 拒绝 + `ShopTabPage.Buy` 第一句拒绝（⛔ 不静默、⛔ 不弹错误窗）。
//   · **G**（账 `商店:102`）🔶 本地开箱奖励表 + 默认 `CratePurchaseHook`：让「第 ⑤ 句等服务器回包」这一跳在本地能走完。
//   · **D**（账 `商店:108`）🔶 本地 `CardsSpecialConfig` 表：原版第 ⑥ 句那个 bool 由它算（替掉写死的 `true`）。
//   · **E**（账 `商店:112`）🔶 本地 `ActionScheduler` 替身：服务端时钟 → 本地时钟、云脚本 → **只出声**。
//
// ============================ 怎么用（宿主）============================
// `B97SHOP2Assert.Run();`
//   + 宿主里两行（同族先例 `B97REMOTE1Assert` / `B94B5_Assert`）：
//     `_sink.Pass += B97SHOP2Assert.Pass; _sink.Fail += B97SHOP2Assert.Fail;`
//   · **不跑 Unity 批处理**、不建持久资产；临时件在 `finally` 里 `DestroyImmediate` + 复位。
//   · **不依赖 `WindowsManager`**（`RefuseSoldOut` 一档在 `ShopData` 层就返回，⛔ 不进 `DoBuy`、不建树）；
//     G 组那扇窗用裸 `AddComponent`（`Build()` 会装默认 hook，但**不**需要 manager）。
//   · A 组会**写盘**（`ShopData.Buy` 的 `PersistShop`）⇒ 走 `OverrideDir` 临时目录，收尾 `DeleteStoreForTest()` + `ResetForTest()`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A6：把 `SoldOut` 的 `<= 0` 改成 `< 0`（或干脆 `return false`）⇒ A6/A7 红。
//   · A7：把 `Buy` 里那道闸删掉 ⇒ 售罄那件照样记账 ⇒ A7 回一句「名字 ×N」⇒ 红（A8 也会红）。
//   · A3 ★★：把 `SoldOut` 写成 `Available <= 0`（漏掉 `AvailableMax > 0`）⇒ 第 4 件（0/0 = **无限制**）被误判售罄 ⇒ A3 红。
//   · A13：把 `ShopTabPage.Buy` 第一句那道闸挪到 `DoBuy` 之后 ⇒ 这里的 `pg` 没有 `_win` ⇒ 走不到 `Setup()` 就炸/不回哨兵 ⇒ A13 红。
//   · A15b：把售罄那支塞进 `OnPurchaseFailed`（弹 `InternetUnreachable`）⇒ `PurchaseFailedCalls` 变 1 ⇒ A15b 红
//     （原版售罄**没有**错误弹窗 —— 那是服务端回包失败那一支）。
//   · G8：把默认 hook 改成「只出声、不调 `onComplete`」⇒ `crateOpened` 不变真 ⇒ G8 红。
//   · D3：把 `ResolveWhereToGetCardsVisibleFromLocalConfig` 里的 `n != 0` 写成常量 `true` ⇒ D2 红。
//   · E6：把 `SetEventTab` 第 ④ 句退回「句柄恒 null」⇒ E6 红；把 `ResetEventTab` 的 Release 删掉 ⇒ E7 红（句柄还在）。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B97_SHOP2`：商店「限购售罄」闸 + 三条 REMOTE 的本地模拟（102 / 108 / 112）。
/// <para>⛔ **期望值全是字面量**（`1` / `5` / `0` / `"售罄"` / `false` / `2`），不从被测实现里读回来。</para></summary>
public static class B97SHOP2Assert
{
    static readonly CheckSink _sink = new CheckSink("[SHOP2] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    /// <summary>收一段日志（`needle` 为 null ⇒ 不过滤）。<paramref name="warningsOnly"/> = true 时只数 `LogType.Warning`。</summary>
    sealed class WarnTap : System.IDisposable
    {
        readonly string _needle; readonly bool _warnOnly; int _n;
        readonly Application.LogCallback _h;
        public WarnTap(string needle, bool warningsOnly = true)
        {
            _needle = needle; _warnOnly = warningsOnly;
            _h = (cond, stack, type) =>
            {
                if (cond == null) return;
                if (_warnOnly && type != LogType.Warning) return;
                if (_needle == null || cond.Contains(_needle)) _n++;
            };
            Application.logMessageReceived += _h;
        }
        public int Count { get { return _n; } }
        public void Dispose() { Application.logMessageReceived -= _h; }
    }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B97_SHOP2：限购售罄闸（商店:297）+ 开箱本地回包（102）/ CardsSpecialConfig（108）/ ActionScheduler（112）");
        SectionA_限购售罄闸();
        SectionG_开箱本地回包();
        SectionD_CardsSpecialConfig本地表();
        SectionE_本地ActionScheduler();
    }

    // ------------------------------------------------------------ A 账 商店:297
    static void SectionA_限购售罄闸()
    {
        MenuCheck.Section(_sink, "A 账 商店:297 —— 原版「限购售罄 ⇒ 按钮不可点」的本地闸（`CatalogItemContainer__Refresh.c:45-77`）");

        string prevDir = ShopData.OverrideDir;
        ShopData.OverrideDir = "d:/4/_tmp_view/shop_selftest_soldout";
        ShopTabPage.ResetSoldOutForTest();
        ShopTabPage.ResetPurchaseFailForTest();
        ShopData.ResetForTest();
        try
        {
            // 前提（硬检查）：出厂表 —— 第 1 件 1/5（有限购）、第 4 件 0/0（**无**限购）
            C(ShopData.AvailableOf(0, 0), 1, "A1 前提：第 1 件出厂限购剩 **1**（本地商品表 `_cards[0]`，值是我们挑的）");
            C(ShopData.Offers(0)[0].AvailableMax, 5, "A1b 前提：第 1 件限购上限 **5**");
            C(ShopData.Offers(0)[3].AvailableMax, 0, "A1c 前提：第 4 件 `AvailableMax = 0`（**没有**限购信息）");

            // ① 判据本身
            C(ShopData.SoldOut(0, 0), false, "A2 ★ 1/5 ⇒ **没售罄**（还能买）");
            C(ShopData.SoldOut(0, 3), false,
              "A3 ★★ **0/0 ≠ 售罄**（`AvailableMax = 0` 是「这一件没有限购信息」，原版 `max < 1 ⇒ bVar12 = true`）"
              + "｜🧨 把 `SoldOut` 写成 `Available <= 0`（漏掉 `AvailableMax > 0`）⇒ 本条红");
            C(ShopData.SoldOut(0, 99), false, "A4 ★ 越界下标 ⇒ **false**（那是 `BuyNotFound` 那一档，⛔ 别和售罄混成一句）");

            // ② 买到最后一支 ⇒ 售罄
            int owned0 = ShopData.OwnedOf(0, 0);
            string got1 = ShopData.Buy(0, 0);
            Tr(ShopData.PurchaseSucceeded(got1), "A5 前提：还有 1 支 ⇒ `ShopData.Buy` 成功（S1 那句「名字 ×N」）");
            C(ShopData.AvailableOf(0, 0), 0, "A5b 前提：限购剩 0");
            C(ShopData.SoldOut(0, 0), true, "A6 ★★ 剩 0/5 ⇒ **售罄**（原版 `已购数 >= max`）");

            int owned1 = ShopData.OwnedOf(0, 0);
            string got2 = ShopData.Buy(0, 0);
            C(got2, ShopData.BuySoldOut,
              "A7 ★★ 售罄后再买 ⇒ `ShopData.Buy` 回 **`BuySoldOut`**（⛔ 不许再回「名字 ×N」谎报买到了）");
            C(ShopData.OwnedOf(0, 0), owned1, "A8 ★ …拥有数**不变**（`OwnedOf` 仍 = " + owned1 + "，改前会 +1）");
            C(owned1, owned0 + 1, "A8b 前提：之前那次真买成功了一次（" + owned0 + " → " + owned1 + "）");

            // ③ 两个哨兵的分工（⛔ 别混成一句）
            C(ShopData.BuySoldOut, "售罄", "A9 哨兵字面量 = `\"售罄\"`（与 `BuyNotFound = \"越界\"` 分开 —— 两者原版行为不同）");
            C(ShopData.PurchaseSucceeded(ShopData.BuySoldOut), false, "A10 ★ 售罄 ⇒ 判**失败**");
            C(ShopData.PurchaseSucceeded(ShopData.BuyNotFound), false, "A10b 两态：越界也判失败（两个哨兵都不算成功）");
            C(ShopData.PurchaseSucceeded("Ultramarines Booster ×2"), true, "A10c 两态：正常那句仍判成功（⛔ 别一刀切成失败）");

            // ④ 页面入口（第一句就挡；`_win == null` 也安全 ⇒ 它根本进不到 `DoBuy`）
            var go = new GameObject("B97SHOP2_SoldOut");
            var pg = go.AddComponent<ShopTabPage>();
            try
            {
                pg.SetHost(null, null, 0);
                ShopTabPage.ResetSoldOutForTest();
                ShopTabPage.ResetPurchaseFailForTest();
                int owned2 = ShopData.OwnedOf(0, 0);
                string got3;
                using (var tap = new WarnTap("已售罄"))
                {
                    got3 = pg.Buy(0);
                    C(tap.Count, 1, "A14 ★ 售罄被点 ⇒ **出声 1 次**（红线：不许静默失败）");
                }
                C(got3, ShopData.BuySoldOut, "A13 ★★ `ShopTabPage.Buy` 的**第一句**就挡（原版那道闸在按钮 `interactable` 上，早于传奇确认/等待框）");
                C(ShopTabPage.SoldOutRefusals, 1, "A13b ★ 读口计数 = 1（自检读口没接错）");
                C(ShopTabPage.LastSoldOutPage, 0, "A13c 读口：页号 = 0");
                C(ShopTabPage.LastSoldOutIndex, 0, "A13d 读口：件号 = 0");
                C(ShopData.OwnedOf(0, 0), owned2, "A15 ★ …拥有数**仍不变**");
                C(ShopTabPage.PurchaseFailedCalls, 0,
                  "A15b ★★ 售罄**不走** `OnPurchaseFailed`（原版这一档没有任何弹窗；错误窗只属于服务端回包失败）"
                  + "｜🧨 把售罄塞进 `OnPurchaseFailed` ⇒ 本条红");
            }
            finally { Object.DestroyImmediate(go); }

            // ⑤ 复位 ⇒ 又买得动（两态对照：⛔ 别把闸做成「一次性」）
            ShopData.ResetForTest();
            ShopTabPage.ResetSoldOutForTest();
            C(ShopData.SoldOut(0, 0), false, "A16 ★★ 两态对照：`ResetForTest()` 之后第 1 件又回到 1/5 ⇒ **不再售罄**");
            C(ShopTabPage.SoldOutRefusals, 0, "A16b 读口也清零（`ResetSoldOutForTest`）");
        }
        finally
        {
            // ⚠️ 顺序要紧：`ResetForTest()` 自己会 `DeleteStoreForTest()` ⇒ **必须仍在临时目录里**才调它，
            //    否则会把玩家的真存档删掉（`Shell/ShopData.cs:487` 的 `ResetForTest` → `DeleteStoreForTest`）。
            ShopData.DeleteStoreForTest();
            ShopData.ResetForTest();          // 仍在 `OverrideDir` 的临时目录里
            ShopData.OverrideDir = prevDir;   // 关掉门（后面的节不再碰盘）
            ShopTabPage.ResetSoldOutForTest();
        }
    }

    // ------------------------------------------------------------ G 账 商店:102
    static void SectionG_开箱本地回包()
    {
        MenuCheck.Section(_sink, "G 账 商店:102 —— 🔶 本地开箱奖励表 + 默认 `CratePurchaseHook`（让第 ⑤ 句在本地能走完）");

        GachaEventWindow.LocalCrateRewards.Clear();
        var go = new GameObject("B97SHOP2_Gacha");
        var win = go.AddComponent<GachaEventWindow>();
        try
        {
            C(GachaEventWindow.LocalCrateRewards.Count, 0, "G1 前提：本地奖励表出厂是空的");

            // ① 空表 ⇒ 空表 + 出声（⛔ 不静默）
            CampaignData.RewardSpec[] empty;
            using (var tap = new WarnTap("本地开箱奖励表里没有这一箱"))
            {
                empty = GachaEventWindow.LocalCrateRewardFor(null);
                C(tap.Count, 1, "G2 ★ 空表（连通配档都没有）⇒ **出声 1 次**（⛔ 不静默发空表）");
            }
            C(empty != null ? empty.Length : -1, 0, "G2b ★ 空表 ⇒ 返回**空数组**（⛔ 不返回 null）");

            // ② 只填通配档 ⇒ 任何 id 都落到它
            GachaEventWindow.LocalCrateRewards[GachaEventWindow.AnyCrateId] = new[]
            {
                new CampaignData.RewardSpec { Id = "gold", Quantity = 1 },
                new CampaignData.RewardSpec { Id = "card", Quantity = 2 },
            };
            C(GachaEventWindow.LocalCrateRewardFor(null).Length, 2, "G3 ★ 通配档 2 条 ⇒ `LocalCrateRewardFor(null)` 取到 2 条");
            C(GachaEventWindow.LocalCrateRewardFor("crate-nobody-knows").Length, 2,
              "G3b ★ 未知 id ⇒ **回退通配档**（2 条）");

            // ③ 精确 id 优先于通配
            GachaEventWindow.LocalCrateRewards["crateA"] = new[]
            { new CampaignData.RewardSpec { Id = "special", Quantity = 1 } };
            C(GachaEventWindow.LocalCrateRewardFor("crateA").Length, 1,
              "G4 ★ 专用 id 优先于通配档（精确档 1 条，不是通配的 2 条）");

            // ④ 装默认 hook（两态：替换 / 不替换）
            Tr(win.InstallLocalCratePurchaseHook(true), "G5 ★★ `InstallLocalCratePurchaseHook(replace: true)` 装上（返回 true）");
            Tr(win.CratePurchaseHook != null, "G5b ★ 装完之后注入位非 null（= 原版 `CurrentOffer` 那一跳有了本地替身）");
            C(win.InstallLocalCratePurchaseHook(false), false,
              "G5c ★ 已有 hook 且 `replace = false` ⇒ **不覆盖**（自检注入的优先级更高）");

            // ⑤ 整条链真的走完（清空奖励表 ⇒ state2 走「空表不弹领奖窗」那一支，不碰 `WindowsManager`）
            GachaEventWindow.LocalCrateRewards.Clear();
            win.CratePurchaseHook = null;
            Tr(win.InstallLocalCratePurchaseHook(true), "G6 前提：重新装回默认 hook");
            bool started;
            using (var tap = new WarnTap("本地模拟", warningsOnly: false))
            {
                started = win.OpenCrateButtonClick();
                C(tap.Count, 1, "G7 ★ …并且**出声一次**说清「奖励来自本地表、不是服务器回包」（红线：不许静默失败）");
            }
            Tr(started, "G8 ★★ 第 ⑤ 句拿到「本地回包」⇒ `OpenCrateButtonClick()` 返回 **true**（改前恒 false）");
            C(win.CrateOpened, true,
              "G8b ★★ …而且默认 hook **同步**调了 `onComplete` ⇒ `crateOpened = true`（原版第 2 句；state 1 等的就是它）");
            Tr(win.CrateFlowStep(0f), "G9 ★ 状态机推到终点（state 1 等的 `_crateOpened` 已被本地回包置真）");
            C(win.CrateFlowState, -1, "G9b ★ 终点：状态机复位（原版 state 2 收尾）");
            C(win.VideoBackgroundActive, false, "G10 收尾第 3 句：`videoBackground` 关回去（⛔ 不留黑屏盖着）");
        }
        finally
        {
            try { PointerLayer.SetInputEnabled(true); } catch { }     // 第 ④ 句关过输入 ⇒ 无论成败都还回去
            GachaEventWindow.LocalCrateRewards.Clear();
            Object.DestroyImmediate(go);
        }
    }

    // ------------------------------------------------------------ D 账 商店:108
    static void SectionD_CardsSpecialConfig本地表()
    {
        MenuCheck.Section(_sink, "D 账 商店:108 —— 🔶 本地 `CardsSpecialConfig.whereToGetCardsData.expansionData.Length`（替掉写死的 `true`）");

        int prev = GachaEventWindow.LocalWhereToGetCardsExpansionCount;
        var go = new GameObject("B97SHOP2_GachaCfg");
        var win = go.AddComponent<GachaEventWindow>();
        try
        {
            C(GachaEventWindow.LocalWhereToGetCardsExpansionCount, 2, "D1 出厂本地表 = **2**（>0 ⇒ 显示；这个值是我们挑的）");

            // ① 两态：0 ⇒ 隐藏；2 ⇒ 显示（⛔ 别写死）
            GachaEventWindow.LocalWhereToGetCardsExpansionCount = 0;
            win.ResolveWhereToGetCardsVisibleFromLocalConfig();
            C(win.WhereToGetCardsVisible, false,
              "D2 ★★ 本地表 = 0 ⇒ `WhereToGetCardsVisible = false`（= 原版 `expansionData.Length == 0` 那一档）"
              + "｜🧨 把取值的 `n != 0` 写成常量 ⇒ 本条红");
            GachaEventWindow.LocalWhereToGetCardsExpansionCount = 2;
            win.ResolveWhereToGetCardsVisibleFromLocalConfig();
            C(win.WhereToGetCardsVisible, true, "D3 ★★ 两态对照：本地表 = 2 ⇒ 又变 true（⛔ 别一刀切成常量）");

            // ② `Build()` 走的是同一个口（先取值、再落地）
            GachaEventWindow.LocalWhereToGetCardsExpansionCount = 0;
            bool built = false;
            try { win.Build(); built = true; }
            catch (System.Exception e) { Tr(false, "D4 前提：`Build()` 不能抛（实际抛了 " + e.GetType().Name + "：" + e.Message + "）"); }
            if (built)
            {
                C(win.WhereToGetCardsButtonActive, (bool?)false,
                  "D4 ★★ 本地表 = 0 ⇒ `Build()` 之后那颗钮**是关的**（`BuildPage` 先 `Resolve…` 再 `Apply…` 那条链真的接上了）");
            }

            // ③ 复位
            GachaEventWindow.LocalWhereToGetCardsExpansionCount = 2;
            win.ResolveWhereToGetCardsVisibleFromLocalConfig();
            if (built) win.ApplyWhereToGetCardsVisible();
            if (built)
                C(win.WhereToGetCardsButtonActive, (bool?)true, "D5 两态对照：本地表改回 2 ⇒ 钮又开");
        }
        finally
        {
            GachaEventWindow.LocalWhereToGetCardsExpansionCount = prev;
            Object.DestroyImmediate(go);
        }
    }

    // ------------------------------------------------------------ E 账 商店:112
    static void SectionE_本地ActionScheduler()
    {
        MenuCheck.Section(_sink, "E 账 商店:112 —— 🔶 `ActionScheduler` 本地替身（服务端时钟 → 本地时钟；云脚本 → 只出声）");

        GachaEventWindow.LocalActionScheduler.ResetForTest();
        var go = new GameObject("B97SHOP2_GachaSched");
        var win = go.AddComponent<GachaEventWindow>();
        try
        {
            C(GachaEventWindow.LocalActionScheduler.PendingCount, 0, "E1 前提：本地排程表是空的（`ResetForTest`）");

            // ① 原版那条「时刻已过 ⇒ 不排程」（`ActionScheduler__Schedule.c:26-30`）
            C(GachaEventWindow.LocalActionScheduler.Schedule(System.DateTime.UtcNow.AddMinutes(-1), "过去的时刻"), 0,
              "E2 ★★ 时刻已过 ⇒ 返回 **0**（原版 `if (param_2 < get_ServerTime()) return 0;`）");
            C(GachaEventWindow.LocalActionScheduler.PendingCount, 0, "E2b 同上一档：表里仍是 0 条");

            // ② 将来 ⇒ 排上
            int h = GachaEventWindow.LocalActionScheduler.Schedule(System.DateTime.UtcNow.AddHours(1), "测试排程");
            Tr(h != 0, "E3 ★ 将来时刻 ⇒ 排上了（句柄非 0）");
            C(GachaEventWindow.LocalActionScheduler.PendingCount, 1, "E3b ★ 本地排程表 1 条");

            // ③ `SetEventTab` 第 ④ 句：时刻拿不到 ⇒ 不排程 + 出声（两态之一）
            using (var tap = new WarnTap("排程时刻拿不到"))
            {
                win.SetEventTab(new GachaEventWindow.EventTabSource
                {
                    LiveOpsId = "gacha-test", IsGachaEvent = true,
                    CurrentOffer = new object(),                // 非 null ⇒ 过第 ④ 句那道闸
                    ScheduledServerTime = null,                 // 但时刻拿不到
                });
                C(tap.Count, 1, "E4 ★★ 时刻拿不到 ⇒ **出声 1 次**（⛔ 不假装排过任务）");
            }
            C(win.EventTabScheduledHandle, (object)null, "E4b ★ …而且句柄位仍是 null");

            // ④ 时刻拿得到 ⇒ 真排（两态之二）
            GachaEventWindow.LocalActionScheduler.ResetForTest();
            win.SetEventTab(new GachaEventWindow.EventTabSource
            {
                LiveOpsId = "gacha-test", IsGachaEvent = true,
                CurrentOffer = new object(),
                ScheduledServerTime = System.DateTime.UtcNow.AddHours(1),
            });
            Tr(win.EventTabScheduledHandle != null,
              "E6 ★★ 时刻拿得到 ⇒ `SetEventTab` 第 ④ 句**真排了**（句柄位非 null —— 改前恒 null）");
            C(GachaEventWindow.LocalActionScheduler.PendingCount, 1, "E6b ★ 本地排程表 1 条（替身真的被调到了）");

            // ⑤ `Reset()` ⇒ 放掉（原版 `ActionScheduler.Release(+0x68)`）
            win.ResetEventTab();
            C(win.EventTabScheduledHandle, (object)null, "E7 ★★ `ResetEventTab()` ⇒ 句柄位清掉（= 原版 `Release`）");
            Tr(!win.GetCardsClickBound, "E7b 同一条的另一句：`getCardsButton` 上没有监听者了（`RemoveAllListeners`）");

            // ⑥ 非 Gacha 事件 ⇒ 第 ③ 句早退（不排程）
            win.SetEventTab(new GachaEventWindow.EventTabSource
            {
                LiveOpsId = "not-gacha", IsGachaEvent = false, CurrentOffer = new object(),
                ScheduledServerTime = System.DateTime.UtcNow.AddHours(1),
            });
            C(win.EventTabScheduledHandle, (object)null, "E8 ★ 两态对照：`IsGachaEvent = false` ⇒ 第 ③ 句早退，不排程");
            C(GachaEventWindow.LocalActionScheduler.PendingCount, 1, "E8b …排程表没有多出一条（仍是 1）");
        }
        finally
        {
            GachaEventWindow.LocalActionScheduler.ResetForTest();
            Object.DestroyImmediate(go);
        }
    }
}
