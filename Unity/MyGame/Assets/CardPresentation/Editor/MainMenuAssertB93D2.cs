// MainMenuAssertB93D2.cs — 执行代理 `B93_D2`（2026-10-28）的断言宿主：
//   **日常/奖励线第 5 批（活动/通行证窗口）** 那 33 条里「代码侧能断」的全部。
//
// 本文件**只放断言**（⛔ 不改产品行为、不建场景）。
// 挂法（本文件已在 `Editor/MainMenuScene.cs` 的 `RunCore` 收尾处接好三行）：
//     MainMenuAssertB93D2.Run();
//     _sink.Pass += MainMenuAssertB93D2.Pass;
//     _sink.Fail += MainMenuAssertB93D2.Fail;
//
// ============================ 期望值从哪来（⛔ 防自证）============================
//  · 所有「几档 / 第几句 / 偏移量」的期望值都来自 `d:/2/tools/decomp_full/` 与 prefab 序列化字段，
//    **不读被测类自己的常量**：偏移 `-74.34 / -72.31` 是从 `RaidProgressBarScaleAndPositionController`
//    的 prefab MB 实读值，直接写死在这里（⛔ 不写 `RaidBarPlacementLogic.PositionOffsetCurrent`）；
//    每格宽度 `136.501` 是 `EnergySinglePlayerOnlyEventWindow` prefab 的 `ArmyItemReference` 现读值。
//  · 每族至少一条**灭自证**（把实现改回旧写法就红），在注释里用 🧨 标出。
// ==================================================================
using System.Collections;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

public static class MainMenuAssertB93D2
{
    const string P = "[B93D2] ";

    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    static readonly CheckSink _sink = new CheckSink(P);
    static void Section(string t) => MenuCheck.Section(_sink, t);
    static void Check<T>(T got, T want, string msg) => MenuCheck.Check(_sink, got, want, msg);
    static void CheckTrue(bool c, string msg) => MenuCheck.True(_sink, c, msg);

    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0;

        RaidHeaderAndTab();
        RaidProgressBar();
        RaidMenuContainer();
        ExpansionPassTabs();
        EventImageWiring();
        EnergyWindow();

        Pass = _sink.Pass; Fail = _sink.Fail;
    }

    // ============================================================ ① Raid 页头 / 页签（原版 RaidHeaderController / RaidEventTab）
    static void RaidHeaderAndTab()
    {
        Section("★ B93-5 ① Raid 页头 / 页签的生命周期（判据 decomp_full/RaidHeaderController__*.c · RaidEventTab__*.c）");

        // `Awake`：原版 `eventHelpButton == null ⇒ 那一跳不执行`；本 prefab 那颗是空 PPtr ⇒ 恒 false。
        Check(RaidHeaderControllerLogic.SubscribeHelpButton(false), false,
              "`RaidHeaderController.Awake`：`eventHelpButton` 为空 ⇒ 不挂监听（本 prefab 就是这一档，⛔ 不伪造钮）");
        Check(RaidHeaderControllerLogic.SubscribeHelpButton(true), true,
              "· 有钮 ⇒ 要挂（两态分得开的另一半）");

        // `ClickEventHelpButton`：三道闸（HasComponent → 组件自述 → WindowsManager），逐档必须分得开。
        // 🧨 灭自证：把第 2/3 道闸的顺序对调 ⇒ (true,false,false) 与 (true,true,false) 的值会互换 ⇒ 红。
        var h1 = RaidHeaderControllerLogic.ClickEventHelpButton(false, false, false);
        var h2 = RaidHeaderControllerLogic.ClickEventHelpButton(true, false, false);
        var h3 = RaidHeaderControllerLogic.ClickEventHelpButton(true, true, false);
        var h4 = RaidHeaderControllerLogic.ClickEventHelpButton(true, true, true);
        Check(h1.StoppedAt, 1, "`ClickEventHelpButton`：没有帮助组件 ⇒ 停在第 1 道闸");
        Check(h2.StoppedAt, 2, "· 组件自述不可开 ⇒ 停在第 2 道闸");
        Check(h3.StoppedAt, 3, "· 没有 `WindowsManager` ⇒ 停在第 3 道闸");
        Check(h4.Opens, true, "· 三道闸全过 ⇒ 才开帮助窗");
        CheckTrue(h1.StoppedAt != h2.StoppedAt && h2.StoppedAt != h3.StoppedAt && h3.StoppedAt != h4.StoppedAt,
                  "· 四档 `StoppedAt` **两两不等**（🧨 闸序对调 ⇒ 塌成两个值 ⇒ 红）");

        // `Initialize`：整段被「事件不为空」包着 ⇒ 没有事件时一个字段都不写。
        var noEv = RaidHeaderControllerLogic.Initialize(false);
        CheckTrue(!noEv.WritesText && !noEv.StartsTimer && !noEv.TitleFromRemoteLabel,
                  "`RaidHeaderController.Initialize`：事件为空 ⇒ `UpdateText` / 协程 / `GetLabel` 全不跑");
        var hasEv = RaidHeaderControllerLogic.Initialize(true);
        CheckTrue(hasEv.WritesText && hasEv.StartsTimer && hasEv.ArmyIconFromRemoteAsset,
                  "· 有事件 ⇒ 四跳都要跑（远端两格如实标为「取不到」）");

        // `RaidEventTab.Reset`：原版两句 `Unload(事件,1)` + （有 headerController 时）`Unload(事件,5)`。
        var r1 = RaidEventTabLogic.Reset(true);
        var r0 = RaidEventTabLogic.Reset(false);
        Check(r1.AssetTypes.Length, 2, "`RaidEventTab.Reset`：有 headerController ⇒ 卸两张图（type 1 + 5）");
        Check(r0.AssetTypes.Length, 1, "· 没有 headerController ⇒ 只卸一张（type 1）");
        // 🧨 灭自证：把第三句那道 `!= null` 闸去掉 ⇒ 两态都变 2 ⇒ 红。
        CheckTrue(r1.AssetTypes.Length != r0.AssetTypes.Length, "· 两态长度**不等**（🧨 去掉 headerController 那道闸 ⇒ 都是 2 ⇒ 红）");
        Check(r1.AssetTypes[0], 1, "· 第一张是背景图（type 1）");
        Check(r1.AssetTypes[1], 5, "· 第二张是阵营图（type 5）");

        // `RaidEventTab.Initialize`：没有 rewardSelector ⇒ 原版 NRE 那道闸 ⇒ 什么都不做。
        var ti = RaidEventTabLogic.Initialize(false, true, false);
        CheckTrue(!ti.RebuildRewards && !ti.HeaderInitialized, "`RaidEventTab.Initialize`：没有 rewardSelector ⇒ 整段不跑");
        var ti2 = RaidEventTabLogic.Initialize(true, false, false);
        CheckTrue(ti2.RebuildRewards && !ti2.HeaderInitialized, "· 有 selector 无事件 ⇒ 奖励格照建（0 件）、页头不初始化");

        // `IEnumerator.Reset()` 是编译器给协程状态机生成的：`state = -1; current = null`。
        int st = 7; object cur = "x";
        RaidHeaderControllerLogic.ResetEnumeratorState(ref st, ref cur);
        Check(st, -1, "`_d__12.Reset`：`state = -1`");
        CheckTrue(cur == null, "· `current = null`");

        // `UpdateTimer()` 协程：`while (谓词) { yield return WaitForSeconds(1); UpdateText(); }`
        // 手驱枚举器（批处理没有帧循环）—— 谓词真 2 次 ⇒ 恰好 2 拍、且**每拍先 yield 再 tick**。
        // 🧨 灭自证：把 `yield` 挪到 `tick` 之后 / 或写成 `do..while` ⇒ 计数或先后次序立刻变。
        int ticks = 0, cond = 0;
        var e = RaidHeaderControllerLogic.UpdateTimer(
                    () => { cond++; return cond <= 2; },
                    () => ticks++).GetEnumerator();
        int moves = 0;
        while (e.MoveNext() && moves < 10) { moves++; CheckTrue(ticks == moves - 1, "`UpdateTimer`：第 " + moves + " 拍先把时间让出去、再 `UpdateText()`"); }
        Check(ticks, 2, "`UpdateTimer`：谓词真 2 次 ⇒ `UpdateText` 恰好 2 次");
    }

    // ============================================================ ② 个人进度条的按档位移
    static void RaidProgressBar()
    {
        Section("★ B93-5 ② Raid 个人进度条按档位移（判据 prefab MB 偏移 -74.34 / -72.31）");

        var r = new PxRect(0f, 0f, 100f, 20f);
        var pos = new Vector2(-74.34f, 0f);
        var size = new Vector2(-72.31f, 0f);

        // 闸：`stateToApply < 2 ⇒ return`（Locked / PlayerUnlocked 一个字段都不动）。
        var locked = RaidProgressBarScaleAndPositionControllerLogic.SetPositionAndScale(r, RaidRewardState.Locked, pos, size);
        Check(locked.x1, r.x1, "`Locked` ⇒ 左沿不动（闸：`state < 2 ⇒ return`）");
        Check(locked.x2, r.x2, "· 右沿不动");
        Check(locked.W, r.W, "· 宽不动");
        var pl = RaidProgressBarScaleAndPositionControllerLogic.SetPositionAndScale(r, RaidRewardState.PlayerUnlocked, pos, size);
        Check(pl.W, r.W, "`PlayerUnlocked(1)` 也不动（闸是 `< 2`，不是 `< 3`）");

        var com = RaidProgressBarScaleAndPositionControllerLogic.SetPositionAndScale(r, RaidRewardState.CommunityUnlocked, pos, size);
        Check(com.W, r.W - 72.31f, "`CommunityUnlocked` ⇒ 宽 = 原宽 − 72.31（sizeDelta + offset）");
        Check(com.x2, r.x2 - 74.34f, "· 右沿 = 原右沿 + (−74.34)（pivot.x = 1 ⇒ 挪的是右缘）");
        // 🧨 灭自证 + 🔴 **2026-10-29（B93_FX3）就地改断言**（原来断「左沿**向右**收」—— 与上面两条**算术互斥**、恒假）：
        //   右缘左移 74.34、宽只缩 72.31 ⇒ 左沿必然**左移 2.03**。期望值 = 两个 prefab 真值之差，⛔ 不读被测常量
        //   （出处 `资料/完全复刻/报告/E6_N10五条+未分类_第2轮.md:56` 的 `positionOffsetCurrent(-74.34,0)` / `scaleOffsetCurrent(-72.31,0)`）。
        MenuCheck.Near(_sink, com.x1, r.x1 - (74.34f - 72.31f), 0.01f,
                       "· 左沿 = 原左沿 − (74.34 − 72.31) = **−2.03**（右缘锚定 + 只改宽 ⇒ 左沿跟着往左 2.03）"
                     + "｜🧨 纯平移（宽不变）⇒ 得 −74.34 ⇒ 红；根本没缩放 ⇒ 得 0 ⇒ 红");

        // `TotallyUnlocked` / `Collected` 也要动（同一道闸）。
        var tot = RaidProgressBarScaleAndPositionControllerLogic.SetPositionAndScale(r, RaidRewardState.TotallyUnlocked, pos, size);
        Check(tot.W, r.W - 72.31f, "`TotallyUnlocked(3)` ⇒ 也按偏移改宽");
        var col = RaidProgressBarScaleAndPositionControllerLogic.SetPositionAndScale(r, RaidRewardState.Collected, pos, size);
        Check(col.W, r.W - 72.31f, "`Collected(4)` ⇒ 也按偏移改宽");
    }

    // ============================================================ ③ 主菜单活动入口格上的小进度条
    static void RaidMenuContainer()
    {
        Section("★ B93-5 ③ `RaidOrArmyReleaseMenuContainer`（判据 decomp_full/…__OnInitialize/__RefreshProgressBar.c）");

        var b0 = RaidOrArmyReleaseMenuContainerLogic.OnInitialize(false, 0);
        Check(b0.Branch, 0, "不是 `ParentEvent` ⇒ 整段不跑（Branch 0）");
        var b1 = RaidOrArmyReleaseMenuContainerLogic.OnInitialize(true, 1);
        Check(b1.Branch, 1, "· 找到 ExpansionPass 计分事件 ⇒ Branch 1");
        var b2 = RaidOrArmyReleaseMenuContainerLogic.OnInitialize(true, 2);
        Check(b2.Branch, 2, "· 找到 Raid 计分事件 ⇒ Branch 2");
        var b3 = RaidOrArmyReleaseMenuContainerLogic.OnInitialize(true, 3);
        Check(b3.Branch, 3, "· 两种都不是 ⇒ Branch 3（且收起进度容器）");
        // 🧨 灭自证：四支必须两两不等 + 只有 1/2 两支才会去 SetAndRefresh。
        CheckTrue(b0.Branch != b1.Branch && b1.Branch != b2.Branch && b2.Branch != b3.Branch,
                  "· 四支**两两不等**（🧨 把两支合成一支 ⇒ 红）");
        CheckTrue(!b0.CallsSetAndRefresh && b1.CallsSetAndRefresh && b2.CallsSetAndRefresh && !b3.CallsSetAndRefresh,
                  "· 只有 Branch 1/2 才接着 `SetAndRefreshProgressBar`");
        CheckTrue(b0.HideProgressContainer && b3.HideProgressContainer, "· Branch 0/3 要把进度容器收起来");

        // `RefreshProgressBar`：🔴 两个事件都非空 ⇒ **早退**（原版就是这样，⛔ 别「顺手修正」）。
        var both = RaidOrArmyReleaseMenuContainerLogic.RefreshProgressBar(true, true, 3, 7, null, null);
        Check(both.Writes, false, "两个计分事件都非空 ⇒ 原版**早退**、一个字段都不写（反直觉但属实）");
        var none = RaidOrArmyReleaseMenuContainerLogic.RefreshProgressBar(false, false, 0, 0, null, null);
        Check(none.Writes, false, "· 两个都没有 ⇒ 也早退");
        var onlyRaid = RaidOrArmyReleaseMenuContainerLogic.RefreshProgressBar(false, true, 0, 7, null, null);
        Check(onlyRaid.LevelText, "8", "· 只有 Raid ⇒ 档位文字 = 下标 **+1**（7 ⇒ 8）");
        var onlyPass = RaidOrArmyReleaseMenuContainerLogic.RefreshProgressBar(true, false, 3, 0, null, null);
        Check(onlyPass.LevelText, "4", "· 只有 ExpansionPass ⇒ 档位文字 = 下标 **+1**（3 ⇒ 4）");
        // 🧨 灭自证：两边用**不同**下标 ⇒ 若把 `+1` 写死成某个具体数，两条不可能同时对。
        CheckTrue(onlyRaid.LevelText != onlyPass.LevelText, "· 两边的档位文字不同（🧨 写死 +1 的某一侧 ⇒ 红）");

        // 进度 = current / next（`ProgressBar.SetProgress(ScoringDataInfo)` 的 `+0x10 / +0x18`）。
        var info = new ScoringDataInfo(150, 100, 300);
        Check(RaidOrArmyReleaseMenuContainerLogic.ProgressOf(info), 0.5f, "`ProgressOf` = current/next = 150/300");
        Check(RaidOrArmyReleaseMenuContainerLogic.ProgressOf(new ScoringDataInfo(150, 0, 0)), 0f,
              "· next = 0 ⇒ 0（⛔ 不除零）");
    }

    // ============================================================ ④ ExpansionPass 两页签
    static void ExpansionPassTabs()
    {
        Section("★ B93-5 ④ ExpansionPass 两页签的生命周期（判据 decomp_full/ExpansionPass*Tab__*.c）");

        var none = ExpansionPassEventTabLogic.Initialize(false, false, false);
        CheckTrue(!none.RebuildRewards && !none.ApplyPremiumPanel && !none.HeaderInitialized,
                  "`ExpansionPassEventTab.Initialize`：事件为空 ⇒ 五跳一件都不做");
        var full = ExpansionPassEventTabLogic.Initialize(true, false, true);
        CheckTrue(full.RebuildRewards && full.ApplyPremiumPanel && full.HeaderInitialized,
                  "· 有事件 ⇒ ①③④ 三跳都跑");
        Check(full.BackgroundSpriteAvailable, false, "· ② 背景图本地取不到 ⇒ `Image.enabled = false`（原版缺图同一支）");
        Check(full.GetCardsButtonVisible, true, "· ⑤ `Where get cards` 的显隐跟着远端 flag");
        var off = ExpansionPassEventTabLogic.Initialize(true, false, false);
        Check(off.GetCardsButtonVisible, false, "· 远端 flag = 0 ⇒ 那颗钮要藏（两态分得开）");

        var rs = ExpansionPassEventTabLogic.Reset(true, true);
        Check(rs.AssetTypes.Length, 1, "`ExpansionPassEventTab.Reset`：只卸一张（原版 `Unload(事件,1)`）");
        Check(rs.RemovesGetCardsListener, true, "· 有那颗钮 ⇒ 摘监听");
        var rs0 = ExpansionPassEventTabLogic.Reset(false, false);
        Check(rs0.AssetTypes.Length, 0, "· 没事件 ⇒ 不卸（⛔ 原版那一跳会 NRE）");
        Check(rs0.RemovesGetCardsListener, false, "· 没那颗钮 ⇒ 不摘");

        var mi = ExpansionPassMissionsTabLogic.Initialize(true, true, false);
        CheckTrue(mi.HeaderInitialized, "`ExpansionPassMissionsTab.Initialize`：`headerController.Initialize(事件)` 要跑");
        Check(mi.WarlordSpriteAvailable, false, "· `LoadAsset(事件,2)` 的督军立绘本地取不到");
        var mi0 = ExpansionPassMissionsTabLogic.Initialize(false, true, false);
        Check(mi0.HeaderInitialized, false, "· 没有 headerController ⇒ 不初始化（原版 `+0x30` 那道闸）");
        Check(ExpansionPassMissionsTabLogic.ReleasesEventAssetsOnClose(true), true, "`OnClose`：有事件 ⇒ 释放事件资产");
        Check(ExpansionPassMissionsTabLogic.ReleasesEventAssetsOnReset(false), false, "`Reset`：没事件 ⇒ 无事可做");
    }

    // ============================================================ ⑤ 活动资产 → Image 两条接线
    static void EventImageWiring()
    {
        Section("★ B93-5 ⑤ `EventImageSelectorController` / `LiveopUIDrawer_EventAssetSprites`（判据 decomp_full 两份 .c）");

        // 取图口本身（真实美术，⛔ 不拿被测表当期望值）。
        CheckTrue(EventArt.LoadByName(null) == null, "`EventArt.LoadByName(null)` ⇒ null（⛔ 不兜底别的图）");
        var real = EventArt.LoadByName("40K_shop_bt_boosters");   // 真实存在于 Resources/Art/ui_menu/
        CheckTrue(real != null, "· 真实美术名能取到图（证明解析口通了：`40K_shop_bt_boosters`）");
        Check(EventArt.HasAsset(null, (int)LiveOpsAssetRef.BackgroundImage), false, "`HasAsset(null, …)` ⇒ false");

        // `LiveopUIDrawer_EventAssetSprites.EventAssetImages.Initialize`：没有事件 ⇒ `enabled = false`。
        var t1 = new FakeImage();
        var cell = new LiveopUIDrawer_EventAssetSprites.EventAssetImages
        { EventAssetType = (int)LiveOpsAssetRef.BackgroundImage, Image = t1 };
        Check(cell.Initialize(null), false, "`EventAssetImages.Initialize(null)` ⇒ 不画");
        Check(t1.Enabled, false, "· 且把 `enabled` 关掉（原版第 ② 句）");

        // `EventImageDrawData.TryDraw`：按 type 命中 + 按**资产名**取图。
        var t2 = new FakeImage();
        var t3 = new FakeImage();
        var d2 = new EventImageSelectorController.ImageEventDrawData { EventAssetType = (int)LiveOpsAssetRef.BackgroundImage, Image = t2 };
        var d3 = new EventImageSelectorController.ImageEventDrawData { EventAssetType = (int)LiveOpsAssetRef.ArmyImage, Image = t3 };
        var assets = new List<EventImageSelectorController.EventAsset>
        {
            new EventImageSelectorController.EventAsset { Type = (int)LiveOpsAssetRef.BackgroundImage, AssetName = "40K_shop_bt_boosters" },
        };
        int n = EventImageSelectorController.Initialize(new List<EventImageSelectorController.ImageEventDrawData> { d2, d3 }, assets);
        Check(n, 1, "两格只有一格命中 type ⇒ 只画 1 格");
        CheckTrue(t2.Enabled && t2.Sprite != null, "· 命中的那一格：`sprite != null` 且 `enabled = true`");
        // 🧨 灭自证：若把 `if (a.Type != EventAssetType) continue;` 删掉 ⇒ 第二格也会被灌图 ⇒ 下面这条红。
        Check(t3.Enabled, false, "· **没命中 type** 的那一格必须关着（🧨 去掉 type 那道闸 ⇒ 两格都开 ⇒ 红）");
        Check(d2.LastAssetName, "40K_shop_bt_boosters", "· 命中时把资产名缓存进 `+0x20`（原版第 ④ 句）");

        // 名字取不到 ⇒ 关掉（原版 `sprite = null ⇒ enabled = false`）。
        var t4 = new FakeImage();
        var d4 = new EventImageSelectorController.ImageEventDrawData { EventAssetType = 1, Image = t4 };
        EventImageSelectorController.Initialize(new List<EventImageSelectorController.ImageEventDrawData> { d4 },
            new List<EventImageSelectorController.EventAsset>
            { new EventImageSelectorController.EventAsset { Type = 1, AssetName = "不存在的图名_zzz" } });
        Check(t4.Enabled, false, "· 命中 type 但图取不到 ⇒ 仍然关掉（⛔ 不假装画上了）");
    }

    sealed class FakeImage : IEventImageTarget
    {
        public Texture2D Sprite { get; set; }
        public bool Enabled { get; set; }
    }

    // ============================================================ ⑥ 单机能量活动窗
    static void EnergyWindow()
    {
        Section("★ B93-5 ⑥ 单机能量活动窗的四跳（判据 decomp_full/SinglePlayerOnlyEnergyEventWindow__*.c）");

        EnergySinglePlayerOnlyEventWindow win = null;
        try
        {
            win = EnergySinglePlayerOnlyEventWindow.Create(WindowsManager.Instance);
        }
        catch (System.Exception e)
        {
            CheckTrue(false, "建 `EnergySinglePlayerOnlyEventWindow` 抛异常：" + e.Message);
            return;
        }
        if (win == null) { CheckTrue(false, "建 `EnergySinglePlayerOnlyEventWindow` 返回 null（`Create` 里没建出来）"); return; }

        try
        {
            // ① `GetScoringIconFromEvent`：本窗类只有「是 ScoringPoint ⇒ Skulls」这一档。
            Texture2D icon;
            bool got = win.GetScoringIconFromEvent(out icon);
            Check(got, true, "`GetScoringIconFromEvent` ⇒ true（本窗类恒 ScoringPoint）");
            // ② `OnEnable` / `OnDisable` = `backgroundCloseButton.onClick` 的 Add/RemoveListener 两态。
            Check(win.ShadeClickBound, true, "`OnEnable` 之后压暗层那颗监听在册");
            win.OnDisable();
            Check(win.ShadeClickBound, false, "· `OnDisable` ⇒ 摘掉（🧨 只写一半 ⇒ 红）");
            win.OnEnable();
            Check(win.ShadeClickBound, true, "· 再 `OnEnable` ⇒ 挂回来");
            // ③ `ShowEventArmies`：按 prefab 的 HLG 算式（每格 136.501，从 `ArmyContent` 左沿起）。
            int n = win.ShowEventArmies(new List<int> { 10, 20, 30 });
            Check(n, 3, "`ShowEventArmies`：3 个阵营 ⇒ 3 格（原版 `ShowEventArmiesPanel.Initialize` 逐件 Instantiate）");
            Check(win.EventArmyCount, 3, "· `EventArmyCount` 读口一致");
            Check(win.ShowEventArmies(new List<int> { 10 }), 1, "· 再来一次只给 1 个 ⇒ 先 Reset 再建 1 格（原版第 ① 句）");
            Check(win.EventArmyCount, 1, "· 旧格被清掉（不是累加）");
            // ④ `RefreshScore` → `RewardProgressBarPanel` 的 `scoreText`（`Total Victories`）。
            win.SetScore(4321, new int[] { 100, 200, 300, 400, 500 }, 1);
            Check(win.ScorePanelText, "4321", "`scoreText`（`Total Victories`）= `score.ToString()`（原版第 ① 句）");
            CheckTrue(win.FillProgress > 0f, "· 进度条跟着分数走（不是停在出厂 0）");
        }
        finally
        {
            if (win != null && win.gameObject != null)
            {
                var go = win.gameObject;
#if UNITY_EDITOR
                if (!Application.isPlaying) { UnityEngine.Object.DestroyImmediate(go); }
                else
#endif
                UnityEngine.Object.Destroy(go);
            }
        }
    }
}
