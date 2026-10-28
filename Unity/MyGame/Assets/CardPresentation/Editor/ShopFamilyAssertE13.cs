// ShopFamilyAssertE13.cs — 商店族（G1 批）14 条的断言（执行代理 B93_E13，2026-10-28）
//
// ============================ 它盯的是什么 ============================
// `判据/B93_Q5_有活入口183.tsv` 里 **批次 = G1** 的 14 条（抽卡箱 `Awake`/开箱 · 页签 `Reset`/`SetEventTab` ·
// 轮播抽屉 `DoReset`/`DrawRewards`/`Initialize` · 战令付费档 2 条 · 战役付费点数 `CollectButtonClicked` ·
// VIP 计时 · 主菜单报价格容器 `OnInitialize` · `FixedDateDailyStoreItem.OnAfterWrite`（D，不动））。
// 逐条的判据、做法、为什么没落地的部分 → `资料/完全复刻/报告/B93_E13.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么用（宿主）============================
// `ShopFamilyAssertE13.Run();`
//   · **不跑 Unity 批处理**、不建持久资产；建的组件与临时结点在收尾 `DestroyImmediate`。
//   · **不需要 `WindowsManager`**（除了那一段 `GachaEventWindow.Build()` —— 它在 try 里，炸了会红、不会带崩整场）。
//   · 建议宿主：**`Editor/ShopScene.cs`**（商店族的主场；VIP / 轮播 / 战令都在这条线上）。
//     ⚠️ 本文件**还没接到宿主里**（本批的文件所有权只到 `Shell/*.cs`）⇒ 由调度台在 `ShopScene.Run` 里加两行：
//       `ShopFamilyAssertE13.Run();`
//       `_sink.Pass += ShopFamilyAssertE13.Pass; _sink.Fail += ShopFamilyAssertE13.Fail;`
//
// ============================ 🔴 灭自证（每条的两态/翻面法都写在文案里）============================
//   · B 组：把「先第二组后第一组」改成「先第一组」⇒ B5 红；把 SpliceWhere 换成两次 Where（不去重）⇒ B6 红；
//           把阈值 4 改成 3 ⇒ B3/B4 翻面；把 `ReleaseAssets` 真的实现出来（不再出声）⇒ B10 红。
//   · C 组：把 `ResetEventTab` 里那句 `_getCardsClick = null` 删掉 ⇒ C4 红（C3 仍绿 ⇒ 两条合起来才分得出真假）。
//   · D 组：把收摊那句 `SetInputEnabled(true)` 删掉 ⇒ D5 红（输入被留在「停用」= 玩家点不动任何东西）。
//   · E 组：把 `CollectButtonClicked` 的 state 0/1 两档删掉、只剩「无动作」⇒ E3/E4 红（E5 仍绿）。
//   · H 组：把 `OnInitialize` 的「无抽屉就 return」那道闸删掉 ⇒ H3 红；把 null 事件那一支改成继续走 ⇒ H4/H5 红。
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 E13：商店族（G1）14 条的断言。计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。
/// <para>⛔ **期望值全是字面量**（4 / "Currency" / 分支号 / true·false），**不从被测实现里读**
/// —— 否则就是「拿我们的实现证明我们的实现」。</para></summary>
public static class ShopFamilyAssertE13
{
    static readonly CheckSink _sink = new CheckSink("[E13] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "E13 商店族（G1）：轮播抽屉 / 页签 / 开箱 / 战令付费 / 战役点数 / VIP / 主菜单报价格");

        SectionA_判据字面量();
        SectionB_轮播抽屉();
        SectionC_页签();
        SectionD_开箱();
        SectionE_战役点数();
        SectionF_战令付费();
        SectionG_VIP倒计时();
        SectionH_主菜单报价格容器();
        SectionI_战役阵营流光参数();

        Debug.Log(_sink.P + "E13 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ A 判据字面量（⛔ 硬检查，先跑）
    static void SectionA_判据字面量()
    {
        MenuCheck.Section(_sink, "A 判据字面量（原版实读值 —— 被改掉就要红）");
        // `CarouselOfferPopupDrawer__.ctor.c` 第一句：`*(int*)(this+0x28) = 4`
        C(OfferContainer.Carousel.ThresholdForEnableAutoScroll, 4,
          "A1 原版 `CarouselOfferPopupDrawer.thresholdForEnableAutoScroll` = **4**（ctor 实读）");
        // 谓词② = `x.Item is Currency`（ILSpy `isinst` 形状；见 `OfferContainer.Carousel` 段头）
        Tr(OfferContainer.Carousel.IsCurrency("Currency"), "A2 谓词②：原版类型名 `Currency` ⇒ 命中（`x.Item is Currency`）");
        Tr(!OfferContainer.Carousel.IsCurrency("RawCardScript"), "A3 谓词②：`RawCardScript` ⇒ 不命中（两态对照）");
        Tr(!OfferContainer.Carousel.IsCurrency(null), "A4 谓词②：没给类型名（null）⇒ 不命中（⛔ 不默认 true）");
    }

    // ------------------------------------------------------------ B 轮播抽屉（`Carousel.State`）
    static void SectionB_轮播抽屉()
    {
        MenuCheck.Section(_sink, "B `CarouselOfferPopupDrawer`：Initialize / DrawRewards（分组次序 + 阈值）/ DoReset");

        var savedFilter = OfferContainer.Carousel.Filter;      // `Filter` 是静态的 ⇒ 跑完必须还原（否则污染别的宿主）
        try
        {
            OfferContainer.Carousel.Filter = null;

            // ---- B1/B2：`Initialize` = `base.Initialize`（DrawRewards）+ `scroll.ManualInit()`
            var st = new OfferContainer.Carousel.State();
            var items = new List<CampaignData.RewardSpec>
            {
                new CampaignData.RewardSpec("C2", 100, 0),      // 货币（谓词②命中）
                new CampaignData.RewardSpec("X1", 1, 0),
                new CampaignData.RewardSpec("X2", 1, 0),
            };
            var drawn = new List<string>();
            st.Initialize(items, null, TypeOfCurrency, (r, q) => { drawn.Add(r.Id); return true; }, 2);
            C(st.Initialized, true, "B1 `Initialize` 第二句 = `scroll.ManualInit()` 跑过了");
            C(st.Visible, 2, "B2 `ManualInit(visible)` 把一屏槽数记下来了（我们这一侧的输入；原版是 UI_InfiniteScroll 量的）");

            // ---- B3：3 条 < 阈值 4 ⇒ `AutoScroll = false`
            C(st.AutoScroll, false, "B3 画出来的**总条数 3 < 阈值 4** ⇒ 原版末尾那句 `scroll.AutoScroll = false`");
            C(st.DrawnCount, 3, "B4 三条全部画出来（谓词① 为 null = 不过滤）");

            // ---- B5：**先第二组（货币）后第一组** —— 原版两个 foreach 的次序
            C(drawn.Count == 3 ? drawn[0] : "<空>", "C2",
              "B5 ★ 次序：**货币那一组先画**（原版 `DrawRewards` 先遍历 `SpliceWhere` 的返回值）");
            // ---- B6：`SpliceWhere` 会把命中的从第一组摘走 ⇒ **两组不重复**
            int c2 = 0;
            for (int i = 0; i < drawn.Count; i++) if (drawn[i] == "C2") c2++;
            C(c2, 1, "B6 ★ `SpliceWhere` 摘走命中项 ⇒ 同一条**不会画两遍**（换成两次 Where 这条就红）");
            C(st.CurrencyGroupCount, 1, "B7 谓词②命中的那一组 = 1 条");

            // ---- B8：4 条 ≥ 阈值 ⇒ `AutoScroll` **不被关**（与 B3 两态对照）
            var st4 = new OfferContainer.Carousel.State();
            var four = new List<CampaignData.RewardSpec>
            {
                new CampaignData.RewardSpec("A", 1, 0), new CampaignData.RewardSpec("B", 1, 0),
                new CampaignData.RewardSpec("C", 1, 0), new CampaignData.RewardSpec("D", 1, 0),
            };
            st4.Initialize(four, null, TypeOfNone, (r, q) => true, 1);
            C(st4.AutoScroll, true, "B8 ★ 四条 = 阈值 ⇒ `AutoScroll` **保持 true**（与 B3 构成两态对照）");
            C(st4.DrawnCount, 4, "B9 四条全画（谓词② 一条都不命中时也是全画，只是都在第一组）");

            // ---- B10：`DoReset` = 逐条 ReleaseAssets（原版 TODO 空实现）+ Destroy，然后清表
            int destroyed = 0;
            st.DoReset(e => destroyed++);
            C(destroyed, 3, "B10 `DoReset` 逐条 `Destroy(gameObject)` ⇒ 3 条都销毁");
            C(st.CurrentItemDrawers.Count, 0, "B11 `DoReset` 末尾清表");
            Tr(st.ReleaseWarnings > 0,
              "B12 ★ 原版 `ReleaseAssets()` 是 **TODO 空实现**（只刷 `[TODO] Implement itemdrawer release`）"
              + " ⇒ 我们必须**照抄：出声、不释放**（把它真实现出来 ⇒ 这条红）");

            // ---- B13：谓词① 可注入（原版那个虚 bool 没解出来）
            OfferContainer.Carousel.Filter = (i, r) => r.Id != "X2";
            var st5 = new OfferContainer.Carousel.State();
            st5.DrawRewards(items, TypeOfNone, (r, q) => true);
            C(st5.DrawnCount, 2, "B13 ★ 谓词①（`Carousel.Filter`）注进去 ⇒ 被过掉的那条不画（`null` = 不过滤，见 B4）");
            OfferContainer.Carousel.Filter = null;   // 🔴 2026-10-28 B93_FX5（`判据/B93_DG3_三宿主红.md` §C1 · 定性 α）：`Carousel.Filter` 是【静态】的 ⇒ B13 注进去的谓词**必须在这里还原**，否则 B14/B15 仍被它滤掉一条 `X2`（B15 期望 3 ⇒ 实得 2）。⛔ 本行占用原空行 = 本文件行数不变（`:206` 等下游落点不位移）；收尾仍有 `finally` 还原 `savedFilter`
            // ---- B14：没给类型名映射 ⇒ 谓词② 判不了 ⇒ 第二组为空（⛔ 不默认 true）
            var st6 = new OfferContainer.Carousel.State();
            st6.DrawRewards(items, null, (r, q) => true);
            C(st6.CurrencyGroupCount, 0, "B14 ★ `typeOf == null` ⇒ 谓词② 一律 false（第二组空），⛔ 不默认 true");
            C(st6.DrawnCount, 3, "B15 …但第一组照画（三条都在）= 「判不了」不是「丢掉」");
        }
        finally { OfferContainer.Carousel.Filter = savedFilter; }
    }

    static string TypeOfCurrency(CampaignData.RewardSpec r) { return r.Id == "C2" ? "Currency" : "RawCardScript"; }
    static string TypeOfNone(CampaignData.RewardSpec r) { return "RawCardScript"; }

    // ------------------------------------------------------------ C 页签（`GachaEventTab.Reset` / `SetEventTab`）
    static void SectionC_页签()
    {
        MenuCheck.Section(_sink, "C `GachaEventTab.SetEventTab`（四句）/ `Reset`（两句）");

        var go = new GameObject("E13_Gacha");
        var win = go.AddComponent<GachaEventWindow>();
        try
        {
            // 前提（硬检查）：裸组件还没建树 ⇒ 监听位是空的
            C(win.GetCardsClickBound, false, "C1 前提：还没 `Build()` ⇒ `getCardsButton` 上**没有监听者**");

            // ---- `SetEventTab` 的 ② `as GachaEvent`：两态
            win.SetEventTab(new GachaEventWindow.EventTabSource
            { LiveOpsId = "ev-gacha-1", IsGachaEvent = false, CurrentOffer = new object() });
            C(win.EventTabLiveOpsId, "ev-gacha-1", "C2 `SetEventTab` 第 ① 句：liveops id 存下来了（+0x58）");
            C(win.EventTabHasGachaEvent, false,
              "C3 ★ `as GachaEvent` 判不过 ⇒ +0x60 **null**、**直接 return**（第 ③ 句）");
            C(win.EventTabScheduledHandle == null, true, "C4 …也不排程（句柄位仍是 null）");

            win.SetEventTab(new GachaEventWindow.EventTabSource
            { LiveOpsId = "ev-gacha-2", IsGachaEvent = true, CurrentOffer = new object() });
            C(win.EventTabHasGachaEvent, true, "C5 ★ 判型过 ⇒ +0x60 非空（与 C3 两态对照）");
            C(win.EventTabScheduledHandle == null, true,
              "C6 第 ④ 句**只做到形状**：本仓没有 `ActionScheduler` ⇒ 句柄位恒 null（账记 B，⛔ 不改成 A）");

            // ---- `Reset` 第 ② 句（`RemoveAllListeners`）—— 前提 = 先有监听者
            bool built = false;
            try { win.Build(); built = true; }
            catch (System.Exception ex)
            {
                Tr(false, "C7 前提：`GachaEventWindow.Build()` 跑得完（炸了：「" + ex.GetType().Name + " " + ex.Message + "」）");
            }
            if (built)
            {
                C(win.GetCardsClickBound, true,
                  "C8 前提：`BuildChest` 那句 `_getCardsClick = OnGetCardsClicked` 生效（= 原版 `OnClick.AddListener`）");
                win.ResetEventTab();
                C(win.GetCardsClickBound, false,
                  "C9 ★ `ResetEventTab()` = 原版 `getCardsButton.OnClick.RemoveAllListeners()` ⇒ 监听者归零");
                win.Build();
                C(win.GetCardsClickBound, true,
                  "C10 ★ 再 `Build()` ⇒ 监听者回来了（C9 的 false 不是「一次性的」—— 两态对照，灭自证）");
            }
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------ D 开箱（`GachaCratePanel.OpenCrateButtonClick` / `CrateAnimationFinished`）
    static void SectionD_开箱()
    {
        MenuCheck.Section(_sink, "D `GachaCratePanel.OpenCrateButtonClick`（六句）/ `CrateAnimationFinished`（三档）");

        var go = new GameObject("E13_Crate");
        var win = go.AddComponent<GachaEventWindow>();
        try
        {
            // 前提（硬检查）：输入层出厂是启用的
            PointerLayer.SetInputEnabled(true);
            C(PointerLayer.InputEnabled, true, "D1 前提：输入层出厂**启用**（原版那台 `EventSystem` 出厂也是启用的）");

            // ---- 第 ⑤ 句必然拿不到回包 ⇒ 收摊、**不启动** ⑥
            bool completed = win.OpenCrateButtonClick();
            C(completed, false, "D2 ★ 第 ⑤ 句（`TryPurchaseOffer`，服务器）本地必失败 ⇒ 不谎报走完");
            C(win.CrateOpened, false, "D3 ★ 原版 `crateOpenned` 只在回包回来才置 true ⇒ 必须仍是 **false**");
            C(win.CrateFlowState, -1, "D4 ⑥（协程）**没启动**（原版会启动并死等；我们收摊，见报告「有意偏离」）");
            C(PointerLayer.InputEnabled, true,
              "D5 ★★ **收摊必须把输入还回去**：第 ④ 句关掉的输入层要恢复 —— 否则玩家点不动任何东西（静默灾难）");
            C(win.VideoBackgroundActive, false,
              "D6 ★ 收摊时 `videoBackground` **关回去**（原版 `RewardWindowClosed` 的形状；⛔ 不留一块黑屏盖着）");

            // ---- 状态机本身：没有回包 ⇒ 停在 state 1（原版就是在这里等）
            win.CrateFlowBegin();
            C(win.CrateFlowState, 0, "D7 `CrateFlowBegin()` ⇒ 从 state 0 起（原版先等 `clip.length × 0.85`）");
            bool fin = win.CrateFlowStep(10f);          // 没有 clip ⇒ 这一档的等待是 0
            C(fin, false, "D8 ★ 没有 `crateOpenned` ⇒ **这一步不往下走**（原版 `if (!crateOpenned) yield return null`）");
            C(win.CrateFlowState, 1, "D9 …停在 state 1（等回包那一档）");

            // ---- `VideoPlayer` 三件：建树之后才有（`BuildChest` 里 `BuildCrateVideo`）
            bool built = false;
            try { win.Build(); built = true; }
            catch (System.Exception ex)
            {
                Tr(false, "D10 前提：`Build()` 跑得完（炸了：「" + ex.GetType().Name + " " + ex.Message + "」）");
            }
            if (built)
            {
                Tr(win.CrateVideoNode != null, "D11 节点 `Video Player` 建出来了（原版 `GachaCratePanel.videoPlayer` 的宿主）");
                Tr(win.CrateVideoPlayer != null, "D12 ★ 上面**真的挂了 `VideoPlayer` 组件**（原来只建空节点）");
                C(win.CrateVideoPlayer != null && win.CrateVideoPlayer.enabled, false,
                  "D13 ★ 原版 `Awake` 第 ① 句 `videoPlayer.enabled = false`（出厂**关**）");
            }
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------ E 战役付费点数（`CollectButtonClicked` 三档）
    static void SectionE_战役点数()
    {
        MenuCheck.Section(_sink, "E `CampaignPremiumPointsPanel.CollectButtonClicked` 三档（0=领 / 1=买 / 其它=无动作）");

        var go = new GameObject("E13_Campaign");
        var tab = go.AddComponent<CampaignTab>();
        try
        {
            // 前提（硬检查）：还没建过 ⇒ `_premState == -1`
            C(tab.PremiumPanelState, -1, "E1 前提：裸组件还没 `BuildPremiumPanel` ⇒ `_premState = -1`");

            tab.ApplyPremiumState(2);
            C(tab.PremiumPanelState, 2, "E2 `ApplyPremiumState(2)` = 本地常态（`AlreadyCollected`）");
            int before = tab.CollectClicks;
            tab.CollectButtonClicked();
            C(tab.CollectClicks, before + 1, "E3 Collect 真的被调到了（不是空按钮）");
            C(tab.LastCollectBranch, -1,
              "E4 ★★ **state 2 ⇒ 什么都不做**（原版 `CampaignPremiumPointsPanel__CollectButtonClicked.c` 第 3 档整段 return）");

            tab.ApplyPremiumState(0);
            tab.CollectButtonClicked();
            C(tab.LastCollectBranch, 0,
              "E5 ★ state 0（`PremiumPurchased`）⇒ 走第 1 档（调订阅方委托；本地没有订阅方 ⇒ 出声）");
            tab.ApplyPremiumState(1);
            tab.CollectButtonClicked();
            C(tab.LastCollectBranch, 1, "E6 ★ state 1（`PremiumNotPurchased`）⇒ 走第 2 档（开付费窗；prefab 引用在远端 ⇒ 出声）");

            // 灭自证：E4 与 E5/E6 必须**分得开** —— 把两档删掉只剩「无动作」⇒ E5/E6 红；只留无动作且 E4 恒 -1 ⇒ 上面已两态
            Tr(tab.LastCollectBranch == 1, "E7 最后一次调用确实是 state 1（E4/E5/E6 三条不是同一个数的三份拷贝）");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------ F 战令付费档（`PurchasePremiumClick` / `Start`）
    static void SectionF_战令付费()
    {
        MenuCheck.Section(_sink, "F `ExpansionPassPurchasePremiumPanel.PurchasePremiumClick` / `Start`");

        var go = new GameObject("E13_Pass");
        var win = go.AddComponent<ExpansionPassEventWindow>();
        try
        {
            C(win.PurchasePremiumClicks, 0, "F1 前提：还没点过");
            bool opened = win.PurchasePremiumClick();
            C(opened, false,
              "F2 ★★ **打不开**：原版第 ③ 句 `GetComponentReference(premiumOfferData, 0xc)` 要远端 premium 报价资产"
              + "（本地没有）⇒ 不许假装打开购买窗");
            C(win.PurchasePremiumClicks, 1,
              "F3 ★ 警示里要带事件 id ⇒ 说明这条链**真的走到了那一步**（不是空函数）；`Start` 那一拍 = "
              + "`BuildNotPremiumBranch` 里 `MenuDraw.Hit` 挂的就是它（无第二处 AddListener）");
            C(win.CurrentEvent == null, true,
              "F4 前提：本地没有事件数据（`CurrentEvent == null`）⇒ 告警里那格是 `<没有事件数据>`，⛔ 不编一个 id");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------ G VIP 倒计时（`VipTimerOnOnRunOutOfTime`）
    static void SectionG_VIP倒计时()
    {
        MenuCheck.Section(_sink, "G `VIPCardShopTab.VipTimerOnOnRunOutOfTime`：数据缺 ⇒ **不假装倒计时**");

        C(ShopData.VipExpiresAt == null, true,
          "G1 ★★ 本机拿不到 VIP 到期时刻（`InventoryManager.GetExpirationDate(Store.RequiredItem)` 的那个 "
          + "`RequiredItem` 在远端 store SO）⇒ 目标时刻必须 **null**。⚠️ 若有人**编一个日期**填进来，这条立刻红");
        C(ShopTabPage.VipExpiryKnown, false, "G2 ★ 同上（同一口径的自检读口）");
        Tr(!string.IsNullOrEmpty(ShopData.VipExpiryUnknownReason),
          "G3 缺数据**有出声理由**（`TimerDisplay` 的 `unknownReason` 非空）—— 红线：不许静默显示 `--`");
        // 等价物本身（`VipTimerOnOnRunOutOfTime` = 一句 `CheckVIPState()`）在 `Shell/ShopTabPage.cs`
        // 的 `OnVipTimerRunOutOfTime` 里，逐字对齐；**它今天永远不会被触发**（没有到期时刻就走不完），
        // ⇒ 这里不建 VIP 页（那要整套货架 + 存档），只钉「数据缺 ⇒ 不假装」这一条。
    }

    // ------------------------------------------------------------ H 主菜单报价格容器（`MainMenuOfferContainer.OnInitialize` 四拍）
    static void SectionH_主菜单报价格容器()
    {
        MenuCheck.Section(_sink, "H `MainMenuOfferContainer.OnInitialize` 四拍（base / as-cast / GetComponent / Initialize）");

        var go = new GameObject("E13_OfferContainer");
        var box = go.AddComponent<MainMenuOfferContainer>();
        try
        {
            // ---- H1：没有抽屉 ⇒ **return**（原版 `if ((bool)component)` 那道闸在最前面）
            box.Drawer = null;
            box.BaseInitializer = null;
            box.OnInitialize(new OfferEventData { Id = "offer-1", Base = new OfferBaseData { Id = "offer-1" } });
            C(box.DrawerInitialized, false,
              "H1 ★ 卡上没有抽屉 ⇒ **什么都不装**（原版 `GetComponent<GeneralOfferPopupDrawerBase>()` 取不到就 return）");

            // ---- H2~H5：把测试抽屉挂上去
            var dGo = new GameObject("TestDrawer");
            dGo.transform.SetParent(go.transform, false);
            var drawer = dGo.AddComponent<TestDrawer>();
            box.Drawer = drawer;

            OfferEventData seeded = null;
            box.BaseInitializer = e => seeded = e;                 // = 原版 `base.OnInitialize(playEvent)`
            var data = new OfferBaseData { Id = "offer-2", Items = new CampaignData.RewardSpec[0] };
            var evt = new OfferEventData { Id = "offer-2", Base = data };
            box.OnInitialize(evt);
            C(seeded == evt, true, "H2 ★ 第 ① 拍 `base.OnInitialize(playEvent)` 接到了（`LiveopMenuContainer` 那一段由宿主注入）");
            C(drawer.InitializeCalls, 1,
              "H3 ★ 第 ④ 拍 `drawer.Initialize(shopOfferEvent.GetBaseData())` 真的调到了");
            C(drawer.LastData == data, true, "H4 ★ 喂进去的是**这一份报价的基数据**（不是 null、也不是别人的）");

            // ---- H5：事件不是报价格事件（`as ShopOfferEvent` 得 null）⇒ 原版**抛 NRE**；我们出声 + 早退
            int beforeCalls = drawer.InitializeCalls;
            box.OnInitialize(null);
            C(drawer.InitializeCalls, beforeCalls,
              "H5 ★★ `playEvent == null`（= 原版 `as ShopOfferEvent` 得 null 那一支）⇒ **不调** `Initialize`"
              + "（原版那里是 `throw NullReferenceException`；我们出声 + 早退 —— 有意偏离，见文件头 ②）");
            C(box.DrawerInitialized, false, "H6 …并且 `DrawerInitialized` 保持 false（两态，C/H3 那一支才是 true）");

            // ---- H7：`DoReset` / `ReleaseAssets` 的形状
            drawer.ResetCalls = 0;
            drawer.DoReset();
            C(drawer.ResetCalls, 1, "H7 `DoReset()` 走得到（原版 `CarouselOfferPopupDrawer.DoReset` 的入口）");
            drawer.ReleaseAssets();
            Tr(drawer.ReleaseCalls > 0,
              "H8 ★ 原版 `ReleaseAssets()` 是 **TODO 空实现**（只出声）⇒ 我们照抄（不是「已经释放好了」）");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------ I 战役阵营图标流光（`CampaignArmyUIDecorator.OnObjectUpdated` 的第 ② 句）
    static void SectionI_战役阵营流光参数()
    {
        MenuCheck.Section(_sink, "I `CampaignArmyUIDecorator.shinyEffect.Play(resetTime: true)` = `WildcardShineLayer.Params.Icon`");

        // 🔴 期望值 = 原版 prefab 字段实读（判据写在 `Shell/CampaignTab.cs` 的 `PlayArmyIconShine` 上）：
        //   `UIEffect`：`m_TransitionRotation 120` · `m_TransitionWidth 0.25` · `m_TransitionSoftness 0.5` ·
        //   `m_TransitionColor (1,1,1,1)` · `m_TransitionColorFilter 6` · `m_TransitionColorGlow 0` ·
        //   `m_TransitionReverse 0`；`UIEffectTweener`：`m_Duration 1.0` · `m_WrapMode 0 (Once)`
        var p = WildcardShineLayer.Params.Icon;
        MenuCheck.Near(_sink, p.rotation, 120f, 0.001f, "I1 原版 `m_TransitionRotation` = 120°");
        MenuCheck.Near(_sink, p.width, 0.25f, 0.001f, "I2 原版 `m_TransitionWidth` = 0.25");
        MenuCheck.Near(_sink, p.softness, 0.5f, 0.001f, "I3 原版 `m_TransitionSoftness` = 0.5");
        MenuCheck.Near(_sink, p.duration, 1f, 0.001f, "I4 ★ 原版 `UIEffectTweener.m_Duration` = **1.0 s**（不是收藏窗那颗 0.75）");
        MenuCheck.Near(_sink, p.colorFilter, 6f, 0.001f, "I5 原版 `m_TransitionColorFilter` = 6（MultiplyAdditive）");
        MenuCheck.Near(_sink, p.colorGlow, 0f, 0.001f, "I6 原版 `m_TransitionColorGlow` = 0");
        C(p.loop, false, "I7 原版 `m_WrapMode` = 0 ⇒ 播一次（⛔ 不是 Loop）");
        C(p.reverse, false, "I8 原版 `m_TransitionReverse` = 0");
        // ---- 灭自证：同族另有一档 **0.75 s**（收藏窗 `Wildcard Display`）⇒ 「Icon 是 1.0」不是「全族恒 1.0」的巧合
        MenuCheck.Near(_sink, WildcardShineLayer.Params.WildcardDisplay.duration, 0.75f, 0.001f,
                       "I9 ★ 两态对照：`WildcardDisplay` 那档是 **0.75 s**（同一份 `Make` 出来的另一个值）"
                       + " ⇒ I4 的 1.0 有区分度");
    }

    /// <summary>H 组用的**测试抽屉**（不画任何东西，只记「有没有被调到」）。
    /// ⚠️ 它只证明「第 ④ 拍被调到、喂的是哪一份数据」；**画得像不像**不在本文件范围
    /// （轮播那一半的可测行为在 B 组，用 `Carousel.State` 直接钉）。</summary>
    public sealed class TestDrawer : MainMenuOfferDrawer
    {
        public int InitializeCalls, ResetCalls, ReleaseCalls;
        public OfferBaseData LastData;
        public override int DrawerCount { get { return 0; } }
        public override void Initialize(OfferBaseData data) { InitializeCalls++; LastData = data; }
        public override void DrawRewards(IEnumerable<CampaignData.RewardSpec> items,
                                         IEnumerable<CampaignData.RewardSpec> v2Extra) { }
        public override void DoReset() { ResetCalls++; }
        public override void ReleaseAssets() { ReleaseCalls++; base.ReleaseAssets(); }
    }
}
