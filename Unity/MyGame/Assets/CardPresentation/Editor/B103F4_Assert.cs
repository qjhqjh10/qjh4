// B103F4_Assert.cs — 执行代理 `B103_F4`（2026-10-30 · 第一百零三会话 · 波 1）
//
// ============================ 这件活是什么 ============================
// `资料/完全复刻/判据/AUD06_链路验收_第2轮.md` §六·6.1 / §6.2 的两条「**断言通过、玩家那条路可以整个坏掉**」：
//
// ① `FP-FORGE-02`（锻造领奖）：玩家那颗钮走 `Shell/ForgeTab.cs:1241 ClaimCell → ForgeData.ClaimAll`
//    （原版「点任意格 = 一次全领」），而 `Editor/RewardsScene.cs:3643` 驱动的是**单格** `ForgeData.Claim`。
//    🔴 **2026-10-30（本代理现读）就地校正 AUD06 的一句话**：AUD06 说「玩家真实走的那条路零断言」——
//    **不准确**：`Editor/RewardsScene.cs:4592` 的 A441 那一击**已经**是「点 `ClaimHit` 节点」的真路径了。
//    真正缺的是**语义**：那一击的夹具里**只有一格可领**（`ForgeData.Init` 每个阵营的 `points` 恰好等于本格门槛）
//    ⇒ 「一次全领」这条口径**从来没有被压到过**（只领一格也照样全绿）。
//    ⇒ 本文件补的正是那一格：**三格同时可领、点一下、断三格一起变已领**。
//
// ② `FP-FORGE-05`（卡包详情窗）：自检只直调 `ShopTabPage.OpenBoosterInfo(0)`（= 公开口），
//    玩家那颗（`Shell/ShopTabPage.cs:562` 的 `InfoHit`）与**原版那条链**（`TryOpenContainerArmy`）都零断言。
//
// ============================ 判据（⛔ 不是我挑的）============================
// ① `ForgeRewardItemContainer__ClickClaim.c` → `Everguild.LiveOps.Forge.CollectRewards`：
//    回包 `RewardService.Collect(rewards, showAnimation: 1, …)`（全文与出处见 `Shell/ForgeTab.cs` 的 `ClaimCell` 方法头）；
//    `Everguild/LiveOps/Forge.cs:54-61` 的 `GetRewardsToCollect()` = `rewards.Skip(rewardsCollected).Where(r => currentPoints >= r.Points)`
//    ⇒ **经验够几档就一次领几档**（本仓等价物 = `Shell/ForgeData.cs:262 ClaimAll`）。
// ② 原版 `RankedDeckSelector.OnGoToStoreButtonClick`（`cpp2il/out_cpp2il_dev_ilspy/RankedDeckSelector.cs:527-545`）：
//    `var openData = new ShopWindow.OpenData { army = currentArmy };` ＋ `startTab = BoosterShopTab` ⇒ 开商店**直奔卡包页**；
//    `BoosterShopTab.OnOpen`（`out_cpp2il_dev_ilspy/BoosterShopTab.cs:76`）⇒ `TryOpenContainerArmy(openData)`；
//    方法体（第一权威 `decomp_full/BoosterShopTab__TryOpenContainerArmy.c` + ILSpy `:89-140`）逐句 =
//      `army == Neutral ⇒ return` → 遍历报价、`cardArmy` 相符的那件 `GetComponent<ShopOfferContainer>()?.OpenContainer()`
//      → **`context.army = CardArmy.Neutral`**（消费掉这次提示，⇒ 再切回本页不会重复开）。
//    🔴 **本代理的结论：原版这条链【是通的】，不是「判据为空」**（判 D 的三个前提一个都不成立）：
//      · 有调用点：`BoosterShopTab.OnOpen`（`out_cpp2il_dev_ilspy/BoosterShopTab.cs:76`，`decomp_full/BoosterShopTab__OnOpen.c` 同）；
//      · 唯一生产者 `RankedDeckSelector.OnGoToStoreButtonClick` 存在且赋 `army = currentArmy`（非 Neutral）；
//      · 不是内联假象：`.c` 与 ILSpy 都给出了**完整可读**的方法体（见上）。
//    ⇒ 所以**不**按铁律 11 例外① 判「判据为空」，本文件按**真链路**补断言。
//    ⚠️ 同时如实记一条**与原版的差异**（不改代码、只记账）：原版那颗「开详情窗」的入口是
//    `ShopOfferContainer.OnClick()`（`decomp_full/ShopOfferContainer__OnClick.c` = 无条件 `OpenContainer()`，
//    `OpenContainer` 取 offer 的 `Popup` 引用、**取不到才**回落 `base.OnClick()` 的购买路）。
//    我们那批格是 `CatalogItemContainer` 那族（只画抽屉、没有 popup 引用）⇒ **点主图开窗这一颗是我们补的**。
//    它**不是** `TryOpenContainerArmy` 的等价物（那条链由 `Setup()` 自动触发、只在带阵营提示时走）——
//    两者是**两条不同的玩家入口**，本文件两条各断一次。
//
// ============================ 怎么用（宿主接线归调度台）============================
//   `Editor/RewardsScene.cs`（锻造页在那里）：
//       B103F4_Assert.RunForge(win);
//       pass += B103F4_Assert.Pass; fail += B103F4_Assert.Fail;
//   `Editor/ShopScene.cs`（商店）：
//       B103F4_Assert.RunShop(win.Manager);   // 它自己建/拆自己的商店窗
//       pass += B103F4_Assert.Pass; fail += B103F4_Assert.Fail;
//
//   ⚠️ **两个入口各自有 sink**（`Pass` / `Fail` 是两者之和）—— 失败不会记进别人账上。
//   ⚠️ `RunForge(null)` 会自己建一扇 `RewardsWindow`；`RunShop(null)` 会用 `WindowsManager.EnsureHost()`。
//      两者**都会开全屏窗**（`OpenWindow` 的 Fullscreen 支会 `HideAllWindows`）⇒ 接线时**放在该宿主那一段的末尾**，
//      跑完它自己 `Close()`（`Close` → `NotifyClosed` → `ShowPreviousWindow`，把宿主的窗带回来）+ `DestroyImmediate`。
//   ⚠️ `RunForge` 会改 `ForgeData`（**全局静态**）⇒ 它在 `finally` 里把**已领格数 / 经验 / 选中阵营**逐项还原并 `Refresh()`。
//
// ============================ 🔴 灭自证（🧨 改坏法写在各条文案里）============================
//   · F6：把 `Shell/ForgeTab.cs` 的 `ClaimCell` 里 `ForgeData.ClaimAll(a, out why)` 改回**只领一格**
//         （`ForgeData.Claim(a, i, out why); n = 1;`）⇒ F6 红（实得 lv0+1）+ F7 红（窗里只有 1 条奖励）。
//   · F8：反过来「不看经验、把整条轨道全领掉」⇒ F8 红（第 lv0+K 格也会变 Collected）。
//       ⇒ F6 与 F8 **结构上不可能同时绿**：一个只领一格、一个全领，两边的读数互相排斥。
//   · S9：删掉 `Shell/ShopTabPage.cs` 的 `TryOpenContainerArmy` 末尾那句 `ctx.army = ArmyIcons.Neutral;`
//         ⇒ 开完商店之后 `OpenData.army` 仍是那个阵营 ⇒ S9 红。
//         （⚠️ 「切走再切回不重开」那条**不能**当它的证据 —— 本仓 `ShopWindow.Open()` 会把 `_openData` 清空，
//           第二次 `Setup` 在 `ctx == null` 就返回了，见那一节的注释。）
//   · S12：把 `InfoHit` 的 `() => OpenBoosterInfo(idx)` 写成固定下标 ⇒ S12 红（弹窗 `Index` 对不上）。
using System;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B103_F4`：锻造领奖的「一次全领」+ 卡包详情窗的两条玩家入口（AUD06 §6.1 / §6.2）。</summary>
public static class B103F4_Assert
{
    static readonly CheckSink _forge = new CheckSink("[B103F4·Forge] ") { Near = MenuNearStyle.Compact2 };
    static readonly CheckSink _shop = new CheckSink("[B103F4·Shop] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>两段合计（宿主按下面两行取值接线即可）。</summary>
    public static int Pass { get { return _forge.Pass + _shop.Pass; } }
    public static int Fail { get { return _forge.Fail + _shop.Fail; } }

    static void FC<T>(T got, T want, string msg) { MenuCheck.Check(_forge, got, want, msg); }
    static void FT(bool c, string msg) { MenuCheck.True(_forge, c, msg); }
    static void SC<T>(T got, T want, string msg) { MenuCheck.Check(_shop, got, want, msg); }
    static void ST(bool c, string msg) { MenuCheck.True(_shop, c, msg); }

    // ============================================================ 真的点一下（命中走 PointerLayer）

    /// <summary>把一颗按钮**按它渲出来那一块的中心**点一下（走 `PointerLayer` 真路径）。
    /// <para>与 `Editor/RewardsScene.cs:566 ClickButtonByQuad` **同一套三步**（渲出来那块的中心 → 设计帧换算 →
    /// `PointerLayer.ButtonAt/ClickAt`），但那份是**那个宿主的私有件**（拿它的 `Check` 去断会把失败记进别人账）
    /// ⇒ 这里按本文件的 sink 重写一份。**判据（中心怎么量）只此一份**：`MenuDraw.QuadRectPx` +
    /// `MenuCheck.PointerFramePxFromDesign`，⛔ 别在这里再手搓一遍乘 108。</para></summary>
    static bool ClickByQuad(CheckSink s, Transform node, WindowButton wb, string what)
    {
        var q = node != null ? node.GetComponentInChildren<ImageQuad>() : null;
        float x1, y1, x2, y2;
        if (!MenuDraw.QuadRectPx(q, out x1, out y1, out x2, out y2))
        {
            MenuCheck.True(s, false, what + "：量不到它渲出来的矩形（没有 `ImageQuad` ⇒ 真鼠标点不到）");
            return false;
        }
        var c = MenuCheck.PointerFramePxFromDesign(new Vector2((x1 + x2) * 0.5f, (y1 + y2) * 0.5f), q.transform);
        var pl = PointerLayer.Instance;
        var got = pl != null ? pl.ButtonAt(c.x, c.y) : null;
        MenuCheck.True(s, pl != null && wb != null && got == wb,
            what + "：（前提）它**渲出来那一块的中心**命中的就是这一颗（引用相等 ⇒ 真鼠标点得到）"
            + (pl == null ? " —— ⚠️ 这一刻 `PointerLayer` 根本取不到（真鼠标这条路没得走）"
                          : " —— 中心 (" + c.x.ToString("F1") + ", " + c.y.ToString("F1") + ")"));
        return pl != null && pl.ClickAt(c.x, c.y);
    }

    // ============================================================ ① FP-FORGE-02

    /// <summary>`FP-FORGE-02`：**打玩家入口**（那颗 `ClaimHit`）⇒ 断「**一次全领**」。
    /// <paramref name="win"/> 传宿主已经开着的那扇奖励窗；传 `null` 就自己建一扇（跑完拆掉）。</summary>
    public static void RunForge(RewardsWindow win = null)
    {
        _forge.Pass = 0; _forge.Fail = 0;
        MenuCheck.Section(_forge, "B103_F4 ① FP-FORGE-02：点 `ClaimHit` ⇒ **一次全领**（原版 `ClickClaim` → `Forge.CollectRewards`）");

        RewardsWindow own = null;
        int prevVisual = -1;
        string origArmy = ForgeData.Selected;
        string army = origArmy;
        int lv0 = 0, p0 = 0;
        bool restoreNeeded = false;
        try
        {
            if (win == null)
            {
                var mgr = WindowsManager.EnsureHost();
                own = RewardsWindow.Create(mgr);
                mgr.OpenWindow(own);
                win = own;
            }
            FT(win != null, "（前提）拿到了一扇 `RewardsWindow`（锻造页在里面）");
            if (win == null) return;

            var tabNode = MenuCheck.FindChild(win.transform, "Forge Tab");
            var ft = tabNode != null ? tabNode.GetComponent<ForgeTab>() : null;
            FT(ft != null, "（前提）`Forge Tab` 节点上挂着 `ForgeTab`");
            if (ft == null) return;

            FT(win.tabButtons != null, "（前提）奖励窗的左栏键建出来了");
            if (win.tabButtons != null)
            {
                prevVisual = win.tabButtons.CurrentVisualIndex;
                win.tabButtons.Click(2);                       // 视觉第 3 键 = Forge（`RewardsWindow.Buttons` 第 3 项）
            }
            // 前提：锻造成了**当前页**（`PointerLayer` 的命中表只收激活链上的 quad ⇒ 页不亮就点不到）
            FT(win.CurrentTab == WindowTabType.Forge, "（前提）锻造页现在是当前页（`CurrentTab == Forge`）");

            var track = MenuCheck.FindChild(ft.transform, "Rewards Scroll View");
            var content = track != null ? MenuCheck.FindChild(track, "Rewards Content") : null;
            FT(content != null, "（前提）奖励轨道的内容节点 `Rewards Content` 找得到");
            if (content == null) return;

            // ---- 挑一个等级不贴顶的阵营（本段要连领 3 格）----
            lv0 = ForgeData.LevelOf(army); p0 = ForgeData.PointsOf(army);
            if (lv0 > ForgeData.MaxLevel - 3)
            {
                foreach (var a in ForgeData.Armies)
                    if (ForgeData.LevelOf(a) <= ForgeData.MaxLevel - 3) { army = a; break; }
                if (army != origArmy) ft.SelectArmy(army);     // = 生产入口（换阵营）
                lv0 = ForgeData.LevelOf(army); p0 = ForgeData.PointsOf(army);
            }
            FT(lv0 <= ForgeData.MaxLevel - 3,
                "（前提）`" + army + "` 的已领格数 = " + lv0 + "（≤ " + (ForgeData.MaxLevel - 3) + "，够连领 3 格）");

            const int K = 3;
            int need = ForgeData.NeededAt(lv0 + K - 1);        // 三格全部达标所需的**累计**经验
            if (ForgeData.PointsOf(army) < need) ForgeData.AddPoints(army, need - ForgeData.PointsOf(army));
            restoreNeeded = true;                              // 从这里起 `ForgeData` 已被改过
            FT(ForgeData.PointsOf(army) >= need,
                "（前提）经验推到够连领 " + K + " 格（= `NeededAt(" + (lv0 + K - 1) + ")` = " + need + "）");
            FT(ForgeData.LevelOf(army) == lv0, "（前提）此刻已领格数仍是 " + lv0 + "（一格都还没领）");

            ft.Refresh(); ft.FocusClaimable();                  // 生产刷新 + 把该领那格对到视口中心

            FC(ForgeData.StateAt(army, lv0), ForgeData.ToCollect,
                "（前提）第 " + (lv0 + 1) + " 格是 `ToCollect`（0 基 " + lv0 + "）");
            for (int k = 1; k < K; k++)
                FC(ForgeData.StateAt(army, lv0 + k), ForgeData.ToCollectButFirstRedeemThePrevious,
                    "（前提）第 " + (lv0 + k + 1) + " 格 = `ToCollectButFirstRedeemThePrevious`"
                    + "（经验够、前面还没领 —— 原版这一档**不开 Claim 钮**，所以整条轨道上只有第 " + (lv0 + 1) + " 格有钮）");

            var cell = MenuCheck.FindChild(content, "ForgeCell_" + lv0);
            FT(cell != null, "（前提）第 " + (lv0 + 1) + " 格的节点 `ForgeCell_" + lv0 + "` 建出来了（在视口里）");
            var hit = cell != null ? cell.Find("ClaimHit") : null;
            var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
            FT(wb != null, "（前提）这一格有 `ClaimHit`（玩家真点得到的那颗命中区）");
            if (wb == null) return;

            FC(RewardWindowFixture.OpenRewardWindowCount(), 0, "（前提）点之前场上没有遗留的领奖窗");
            bool clicked = ClickByQuad(_forge, hit, wb, "锻造轨道第 " + (lv0 + 1) + " 格的 `ClaimHit`");
            FT(clicked, "…这一下点在它**渲出来那块的中心**上（走 `PointerLayer` 真命中，⛔ 不是直调 `ClaimCell`）");

            // ---- 结果：**一次全领** ----
            FC(ForgeData.LevelOf(army), lv0 + K,
                "★★ 点**一格** ⇒ 已领格数一次跳到 **" + (lv0 + K) + "**（= 原版 `Forge.CollectRewards` 的"
                + " `Skip(rewardsCollected).Where(points >= r.Points)`：经验够几档就一次领几档）"
                + "｜🧨 `ClaimCell` 改回「只领那一格」⇒ 这里是 " + (lv0 + 1) + " ⇒ 本条红");

            var rw = RewardWindowFixture.FindOpenRewardWindow();
            FT(rw != null, "★ …而且弹了领奖窗（原版 `RewardService.Collect(rewards, showAnimation: 1, …)`）");
            if (rw != null)
            {
                var rc = rw.Context;
                FC(rc != null && rc.Rewards != null ? rc.Rewards.Length : -1, K,
                    "★★ 窗里铺的是**本批全部 " + K + " 条**奖励（= 回包里那个**数组**，⛔ 不是单条）"
                    + "｜🧨 只领一格 ⇒ 这里是 1 ⇒ 红");
            }
            RewardWindowFixture.DismissRewardWindows();          // 弹窗会把底窗压到 Background ⇒ 先关掉再读页面

            // ---- 结果：三格全 Collected、第 4 格**没被多领** ----
            for (int k = 0; k < K; k++)
                FC(ForgeData.StateAt(army, lv0 + k), ForgeData.Collected,
                    "★ 第 " + (lv0 + k + 1) + " 格被一起领掉（`Collected`）");
            FT(ForgeData.StateAt(army, lv0 + K) != ForgeData.Collected,
                "★★ 第 " + (lv0 + K + 1) + " 格**没有**被多领（经验还不够它 —— 一次全领 ≠ 全轨清空）"
                + "｜🧨 让 `ClaimAll` 不看经验把整条轨道全领 ⇒ 本条红（与上一条 `LevelOf` 互相钉死）");
            FT(!ForgeData.HasToCollect(army), "★ 领完 ⇒ `hasToCollectReward` 变假（可领光效灭）");

            int hits = 0;
            foreach (var t in content.GetComponentsInChildren<Transform>(true))
                if (t.name == "ClaimHit") hits++;
            FC(hits, 0, "★ 重建之后整条轨道上**一个 Claim 钮都不剩**（重建走的是 `Refresh()`，不是我们手摆的）");
        }
        finally
        {
            // ---- 还原（`ForgeData` 是全局静态；后面几张截图还要拍「可领态」）----
            if (restoreNeeded)
            {
                ForgeData.SetLevelForTest(army, lv0);
                int cur = ForgeData.PointsOf(army);
                if (cur != p0) ForgeData.AddPoints(army, p0 - cur);
            }
            if (win != null && win.tabButtons != null)
            {
                var tabNode2 = MenuCheck.FindChild(win.transform, "Forge Tab");
                var ft2 = tabNode2 != null ? tabNode2.GetComponent<ForgeTab>() : null;
                if (ft2 != null)
                {
                    if (army != origArmy) ft2.SelectArmy(origArmy);
                    else { ft2.Refresh(); ft2.FocusClaimable(); }
                }
                if (prevVisual >= 0 && prevVisual != win.tabButtons.CurrentVisualIndex)
                    win.tabButtons.Click(prevVisual);
            }
            if (own != null)
            {
                RewardWindowFixture.DismissRewardWindows();
                own.Close();
                UnityEngine.Object.DestroyImmediate(own.gameObject);
            }
        }
    }

    // ============================================================ ② FP-FORGE-05

    /// <summary>`FP-FORGE-05`：卡包详情窗的**两条玩家入口**各打一次 ——
    /// ① 原版链 `BoosterShopTab.TryOpenContainerArmy`（带阵营提示开商店 ⇒ 自动开对应那件 + 消费掉提示）；
    /// ② 我们补的那颗 `InfoHit`（点卡包格的主图 ⇒ 开详情窗）。
    /// <para>⚠️ 它自己建/拆商店窗（`WindowsManager.OpenWindow` 的 Fullscreen 支会 `HideAllWindows`）⇒
    /// 宿主接线时放在那一段**末尾**。跑完逐扇 `Close()`（`NotifyClosed` → `ShowPreviousWindow` 把底窗带回来）。</para></summary>
    public static void RunShop(WindowsManager mgr = null)
    {
        _shop.Pass = 0; _shop.Fail = 0;
        MenuCheck.Section(_shop, "B103_F4 ② FP-FORGE-05：卡包详情窗的两条玩家入口（原版 `TryOpenContainerArmy` / 我们的 `InfoHit`）");

        if (mgr == null) mgr = WindowsManager.EnsureHost();
        ST(mgr != null, "（前提）有 `WindowsManager`（两条链都要靠它开窗）");
        if (mgr == null) return;

        int armyUM = ArmyIcons.Id("Ultramarines");
        int armyDA = ArmyIcons.Id("DarkAngels");
        ST(armyUM > 0 && armyDA > 0, "（前提）阵营表查得到 Ultramarines / DarkAngels（实测 " + armyUM + " / " + armyDA + "）");

        // ---- A：原版链 —— 带阵营提示开商店 ⇒ 落卡包页 + 自动开对应那件的详情窗 + 消费掉提示 ----
        ShopWindow w = null;
        try
        {
            var ctx = new ShopWindow.OpenData(armyUM);
            w = ShopWindow.Create(mgr, ctx);
            ST(w != null, "（前提）商店窗建得出来（`ShopWindow.Create(mgr, OpenData)`）");
            if (w == null) return;
            mgr.OpenWindow(w);                                    // = 原版 `OpenWindow(shopWindow, openData, …)` 那一跳

            SC(w.CurrentTab, WindowTabType.ShopCrates,
                "★ 带阵营提示开商店 ⇒ 落在 **Boosters/Crates 页**（原版 `GameWindowWithTabsOptions.startTab = BoosterShopTab`）"
                + "｜🧨 `StartVisualIndex` 里那条 `army != Neutral` 分支删掉 ⇒ 本条红（会落第 1 页 Cards）");

            var page = w.PageOf(ShopTabPage.CratesPage);
            ST(page != null, "（前提）第 5 页（Crates/Boosters）的 `ShopTabPage` 拿得到");
            if (page != null)
            {
                SC(page.LastAutoOpenedOffer, 0,
                    "★★ 原版 `TryOpenContainerArmy` 真的跑了：按 `OpenData.army` 命中**第 1 件**（`Ultramarines Booster`，`_cards[0]`）"
                    + "｜🧨 把 `Setup()` 末尾那句 `TryOpenContainerArmy()` 删掉 ⇒ 这里是 −1 ⇒ 红");

                var pop = page.LastBoosterInfo;
                ST(pop != null, "★★ …并且**真的开了那件的详情窗**（原版 `ShopOfferContainer.OpenContainer()` 那一跳）");
                if (pop != null)
                {
                    SC(pop.Page, ShopTabPage.CratesPage, "★ …弹窗的页 = Crates 页（4）");
                    SC(pop.Index, 0, "★ …弹窗的件号 = 0（= 命中的那件；⛔ 不是「随便开第 0 件」—— 页与件号一起断）");
                    pop.Close();                                  // 弹窗会把商店窗压到 Background ⇒ 关掉再往下
                }
                SC(ctx.army, ArmyIcons.Neutral,
                    "★★ 那次提示被**消费掉**了（原版 `context.army = CardArmy.Neutral`，`.c` 末句）"
                    + "｜🧨 删掉 `Shell/ShopTabPage.cs` 的 `TryOpenContainerArmy` 末尾那句 ⇒ 本条红");

                // 提示只消费一次：**切走再切回**不该再自动开一次。
                // 🔴 **2026-10-30（本代理查实，如实标）**：这一条今天**测得比原版粗** —— 本仓
                //   `Shell/ShopWindow.cs:167` 的 `Open()` 末尾会 `_openData = null`（`B103_R1` 2026-10-30 加的），
                //   而 `Context = _openData ?? Data as OpenData`、`OpenWindow` 又把 `Data` 清成 null
                //   ⇒ 第二次 `Setup` 在 `TryOpenContainerArmy` 的第一句 `if (ctx == null) return;` 就走了，
                //   **比原版早一句**（原版 `data` 一直在，只是 `army` 被清成 Neutral）。
                //   ⇒ 「消费」这件事真正钉住的是上面那条 `ctx.army == ArmyIcons.Neutral`；
                //     本条只钉**玩家可见结果**（切回来不会再弹一扇），不假装能区分这两条路。
                int popupsNow = mgr.GetOpenWindow<BoosterInfoPopup>() != null ? 1 : 0;
                SC(popupsNow, 0, "（编排）关掉之后场上没有开着的卡包详情窗");
                w.tabButtons.Click(0);                            // 切到 Cards 页
                w.tabButtons.Click(4);                            // 再切回 Crates 页（= 重新跑一次 `Setup → TryOpenContainerArmy`）
                SC(page.LastAutoOpenedOffer, -1,
                    "★★ 切走再切回 ⇒ **不再自动开**（玩家的那个提示只消费一次）"
                    + "｜⚠️ 今天它在 `ctx == null` 那一句就返回（见本段注释），⛔ 别把这条当「消费」的证据");
                ST(mgr.GetOpenWindow<BoosterInfoPopup>() == null,
                    "★ …而且场上**没有多出一扇**卡包详情窗");
            }
        }
        finally
        {
            if (w != null) { BoosterInfoPopup p; if ((p = mgr.GetOpenWindow<BoosterInfoPopup>()) != null) p.Close(); w.Close(); UnityEngine.Object.DestroyImmediate(w.gameObject); }
        }

        // ---- B：我们那颗 `InfoHit` —— 点卡包格的主图 ⇒ 开详情窗（⛔ 不是直调 `OpenBoosterInfo`）----
        ShopWindow w2 = null;
        try
        {
            w2 = ShopWindow.Create(mgr);                          // 不带提示 ⇒ 落第 1 页（Cards）
            ST(w2 != null, "（前提）第二扇商店窗建得出来");
            if (w2 == null) return;
            mgr.OpenWindow(w2);
            SC(w2.CurrentTab, WindowTabType.ShopCards, "（前提）不带阵营提示 ⇒ 落在第 1 页 Cards");

            var page0 = w2.PageOf(0);
            ST(page0 != null, "（前提）第 1 页的 `ShopTabPage` 拿得到");
            if (page0 != null)
            {
                var cell0 = MenuCheck.FindChild(page0.transform, "CatalogItemShopContainer_0");
                ST(cell0 != null, "（前提）第 1 页第 1 件的格子 `CatalogItemShopContainer_0` 在");
                var infoHit = cell0 != null ? cell0.Find("InfoHit") : null;
                var infoWb = infoHit != null ? infoHit.GetComponent<WindowButton>() : null;
                ST(infoWb != null, "（前提）那颗格子上有 `InfoHit`（玩家点主图的那颗命中区）");
                if (infoWb != null)
                {
                    bool ok = ClickByQuad(_shop, infoHit, infoWb, "第 1 页第 1 件的主图 `InfoHit`");
                    ST(ok, "…这一下点在它**渲出来那块的中心**上（真鼠标点得到）");
                    var pop = page0.LastBoosterInfo;
                    ST(pop != null, "★★ 点**主图** ⇒ 开出卡包详情窗（这条链此前只有「直调 `OpenBoosterInfo(0)`」的断言）"
                        + "｜🧨 `MenuDraw.Hit(cell, \"InfoHit\", …)` 那一句删掉 ⇒ 前面「有 `InfoHit`」先红、这里也红");
                    if (pop != null)
                    {
                        SC(pop.Page, 0, "★ …弹窗的页 = 1（0 基 0）");
                        SC(pop.Index, 0,
                            "★★ …件号 = **被点的那一件**（0）｜🧨 把 `() => OpenBoosterInfo(idx)` 写成固定下标 ⇒ 本条红");
                        pop.Close();
                    }
                }
            }
        }
        finally
        {
            if (w2 != null) { BoosterInfoPopup p; if ((p = mgr.GetOpenWindow<BoosterInfoPopup>()) != null) p.Close(); w2.Close(); UnityEngine.Object.DestroyImmediate(w2.gameObject); }
        }

        // ---- C：负例 —— 阵营在卡包页上**没有对应那件** ⇒ 不弹窗、也**不消费**提示 ----
        ShopWindow w3 = null;
        try
        {
            var ctxDA = new ShopWindow.OpenData(armyDA);          // DarkAngels：`_cards` 那四件里没有它
            w3 = ShopWindow.Create(mgr, ctxDA);
            ST(w3 != null, "（前提）第三扇商店窗建得出来（带一个**本页没有对应卡包**的阵营）");
            if (w3 == null) return;
            mgr.OpenWindow(w3);

            var page = w3.PageOf(ShopTabPage.CratesPage);
            ST(page != null, "（前提）Crates 页在");
            if (page != null)
            {
                SC(page.LastAutoOpenedOffer, -1,
                    "★ 本页没有 DarkAngels 的卡包 ⇒ **不自动开**（原版这一支不出声、也不开窗）");
                ST(mgr.GetOpenWindow<BoosterInfoPopup>() == null, "★ …场上没有卡包详情窗");
                SC(ctxDA.army, armyDA,
                    "★★ …而且那个提示**原样留着**（原版只在**命中**那一支写 `= 0`，⛔ 别「顺手清掉」）"
                    + "｜🧨 把 `ctx.army = Neutral` 挪到 for 循环之前 ⇒ 本条红");
            }
        }
        finally
        {
            if (w3 != null) { BoosterInfoPopup p; if ((p = mgr.GetOpenWindow<BoosterInfoPopup>()) != null) p.Close(); w3.Close(); UnityEngine.Object.DestroyImmediate(w3.gameObject); }
        }
    }
}
