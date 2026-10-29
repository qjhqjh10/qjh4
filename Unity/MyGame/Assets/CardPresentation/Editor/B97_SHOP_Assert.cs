// B97_SHOP_Assert.cs —— 执行代理 `B97_SHOP`（第九十七会话）的断言
//
// ============================ 它盯的是什么（账 `商店.tsv` 的 16 条 B）============================
// 分诊 → `资料/完全复刻/判据/B96_商店轮抽个人32_分诊.md` §一；逐条结论 → `资料/完全复刻/报告/B97_SHOP.md`。
// 本文件只断言**本代理这一批真能动的那几处**（其余 12 条是「结构差 / REMOTE / 等 Unity 导入」，
// 没有可在批处理里断言的运行时面 —— ⛔ 不为过闸造空断言）：
//   · **A**（账 `商店:95`）开箱视频 clip 的**落地名与真身**：`gacha_crate_opening.mp4`，
//     `ftyp` / `mp42` / 5,141,522 B，且 **⛔ 没有** 同名 `.webm`。
//     判据 → `判据/B96_商店95_开箱视频.md` §一/§二（源 = `bundle_menus_assets_all/VideoClip/Gacha Crate Opening.mp4`）。
//   · **B**（账 `商店:101`）`GachaCratePanel.<Awake>b__9_0(VideoPlayer)`：`started` 一响就 `Play2D(cue)`，
//     **原版不判 clip 有没有** ⇒ 我们⛔不 gate。判据 = `GachaCratePanel___Awake_b__9_0.c:1-14`。
//   · **C**（账 `商店:38`）`CampaignPointDrawer.DrawForArmy(army)`：主图 = **阵营徽记**（替换，不是叠两层），
//     `army == 0` ⇒ 整格不画。判据 = `CampaignPointDrawer__DrawForArmy.c:22-27`。
//   · **D**（账 `商店:162`）`MainMenuOfferContainer.OnInitialize` 的**调用方**：
//     模式卡拿到 liveop 事件之后调一次；非报价格卡 / 手挑卡（`owner == null`）一律不接。
//     判据 = `MainMenuOfferContainer__OnInitialize.c` 的入参语义 + `MainMenuWindow__InstantiateLiveOp.c` 的调用链。
//
// ============================ 怎么用（宿主）============================
// `B97SHOPAssert.Run();`
//   + 宿主里两行（同族先例 `B94B5_Assert` / `ShopFamilyAssertE13`）：
//     `_sink.Pass += B97SHOPAssert.Pass; _sink.Fail += B97SHOPAssert.Fail;`
//   · 不跑 Unity 批处理、不建持久资产；临时件全在 `finally` 里 `DestroyImmediate`。
//   · D 组只造「卡节点 + 活动数据」，**不建整棵主菜单**（整页属 `Editor/MainMenuScene.cs` 的地盘）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A3：把 mp4 改名成 `.webm`（「照 intro/defeat/draw/victory 那四支的命名」）⇒ A2 红 + A3 红。
//   · A5：换一支真 WebM 进来 ⇒ `mp42` 那一格红。
//   · B5：给 `OnCrateVideoStarted` 加一道「只播一次」的闸（`if (_crateCueSaid) return;` 提到 `PlayFx`
//         **之前**）⇒ 第二次不再调 `PlayFx` ⇒ `Throttled` 停在 0 ⇒ **B5 红**（B4 仍是 1，单看 B4 看不出来）。
//   · C2：把 `CampaignPoint` 的主图改回 `CampaignPoints.GetIcon`（出厂那张
//         `40K_genearl_icon_Campaign_points_big`）⇒ C2 红。
//   · C3：`army == 0` 时也拿一张默认图顶 ⇒ C3 红（原版是把 `Image` 关掉）。
//   · D6：把 `BuildModeCardForCard` 里那一跳删掉 ⇒ `InitializeCalls` 停在 0 ⇒ D6 红。
//   · D3/D4：把闸写成「卡在 `_modeContent` 下就接」⇒ D3/D4 红（两张反例卡都会被挂上）。
using System.Reflection;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B97_SHOP`：商店族 16 条 B 里**能动手的那几处**的断言。
/// <para>⛔ **期望值全是字面量**（`"mp42"` / `5141522L` / `"40k_DeckSelection_icon_FactionUM"` /
/// `"Gacha vault open"` / `1`），不从被测实现里读回来 —— 否则就是「拿我们的实现证明我们的实现」。</para></summary>
public static class B97SHOPAssert
{
    static readonly CheckSink _sink = new CheckSink("[SHOP] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B97_SHOP 商店族：开箱 clip 真身 / `started` 音效 / 战役点抽屉 / 报价格容器调用方");
        SectionA_开箱视频资源();
        SectionB_开箱音效();
        SectionC_战役点抽屉();
        SectionD_报价格容器调用方();
    }

    // ------------------------------------------------------------ A 账 `商店:95`
    static void SectionA_开箱视频资源()
    {
        MenuCheck.Section(_sink, "A 账 商店:95 —— `GachaCratePanel.Awake` 的 clip：落地名 `.mp4`、内容 = 真 MP4");

        C(GachaEventWindow.CrateClipResource, "Art/videos/gacha_crate_opening",
          "A1 `Resources.Load` 的资源名（不带扩展名）—— 原版那一格 = prefab 的 `videoPlayer.clip`");

        // 真身：`Assets/CardPresentation/Resources/Art/videos/gacha_crate_opening.mp4`
        string dir = Application.dataPath + "/CardPresentation/Resources/Art/videos";
        string mp4 = dir + "/gacha_crate_opening.mp4";
        string webm = dir + "/gacha_crate_opening.webm";

        Tr(System.IO.File.Exists(mp4),
          "A2 ★ 落盘件 `gacha_crate_opening.mp4` 在 `Resources/Art/videos/` 里"
          + "（源 = `bundle_menus_assets_all/VideoClip/Gacha Crate Opening.mp4`，5,141,522 B）");
        Tr(!System.IO.File.Exists(webm),
          "A3 ★★ 灭自证：**没有**同名 `.webm` —— 那支内容是真 MP4（`ftyp`/`mp42`/H.264），"
          + "改名成 `.webm` 会让 Unity 用 VP8 解码器去解（那四支 `intro/defeat/draw/victory` 才是真 WebM）");

        // ⚠️ 三条**硬检查**（⛔ 不写成 `if (File.Exists(...)) { … }` —— 那是「前提不成立就静默跳过」）
        var head = new byte[12];
        int got = -1;
        long len = -1;
        try
        {
            using (var fs = new System.IO.FileInfo(mp4).OpenRead())
            {
                got = fs.Read(head, 0, 12);
                len = fs.Length;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[SHOP] 读 `" + mp4 + "` 炸了：" + ex.GetType().Name + " " + ex.Message);
            got = -1;
        }
        C(got, 12, "A4 前提：文件读得开、够 12 字节（头 = `00 00 00 18 66 74 79 70 6D 70 34 32`）");
        C(got == 12 ? System.Text.Encoding.ASCII.GetString(head, 4, 4) : "(读不到)", "ftyp",
          "A5 偏移 4..8 = `ftyp`（ISO-BMFF/MP4 容器）");
        C(got == 12 ? System.Text.Encoding.ASCII.GetString(head, 8, 4) : "(读不到)", "mp42",
          "A6 ★ 主 brand = `mp42`（= `ftypmp42`；判据 → `判据/B96_商店95_开箱视频.md` §一）");
        C(len, 5141522L, "A7 字节数 = 5,141,522（原版 `VideoClip/Gacha Crate Opening.mp4` 现读）");
    }

    // ------------------------------------------------------------ B 账 `商店:101`
    static void SectionB_开箱音效()
    {
        MenuCheck.Section(_sink, "B 账 商店:101 —— `GachaCratePanel.<Awake>b__9_0(VideoPlayer)`：`started` 一响就播");

        var mi = typeof(GachaEventWindow).GetMethod("OnCrateVideoStarted",
                                                    BindingFlags.Instance | BindingFlags.NonPublic);
        Tr(mi != null, "B1 前提：`OnCrateVideoStarted` 找得到（= `videoPlayer.started` 那个回调的落点，原版 `__Awake_b__9_0`）");
        if (mi == null) return;      // ⚠️ 这里 return 是安全的：B1 已经**红**了，不会静默

        var go = new GameObject("B97SHOP_Gacha");
        var win = go.AddComponent<GachaEventWindow>();
        try
        {
            WindowSounds.ResetCounters();

            mi.Invoke(win, new object[] { null });
            C(WindowSounds.Played, 1, "B2 ★ 第一次 `started` ⇒ 真的播了一声（`WindowSounds.Played` = 1）");
            C(WindowSounds.LastCue, WindowSounds.GachaVaultOpenCue,
              "B3 ★ cue 名 = `Gacha vault open`（AssetRipper `Assets/Gacha vault open.asset`，clip = `Vault Crate Open Full`）");

            mi.Invoke(win, new object[] { null });
            C(WindowSounds.Played, 1,
              "B4 第二次 `started` 后 `Played` 仍是 1 —— cue 的 `timeToPlayAgain = 0.1` 把它挡在**音效表**那一层");
            C(WindowSounds.Throttled, 1,
              "B5 ★★ 灭自证：第二次**确实调到了** `PlayFx`（节流计数 +1）—— 原版每次 `started` 都播、⛔ 不 gate；"
              + "若把 `_crateCueSaid` 那道早退提到 `PlayFx` **之前**，这里会是 0 ⇒ 本条红（单看 B4 看不出来）");
        }
        finally
        {
            WindowSounds.ResetCounters();
            Object.DestroyImmediate(go);
        }
    }

    // ------------------------------------------------------------ C 账 `商店:38`
    static void SectionC_战役点抽屉()
    {
        MenuCheck.Section(_sink, "C 账 商店:38 —— `CampaignPointDrawer.DrawForArmy(army)`：主图 = 阵营徽记（替换，不是叠两层）");

        var go = new GameObject("B97SHOP_CampaignPoint", typeof(RectTransform));
        try
        {
            var box = new PxRect(0f, 0f, 60f, 70f);
            var st = ItemDrawerStyle.Default(3000, 3001, 3002);

            var r = ItemDrawer.DrawCampaignPointsReward(go.transform, box, 200, 10 /* Ultramarines */, st);
            C(r.Drawer, ItemDrawer.DrawerCampaignPoint,
              "C1 走的是 `CampaignPointDrawer` 那档（`Type = \"CampaignPoints\"` ⇒ 真表那一跳，不是兜底）");
            C(r.Art, "40k_DeckSelection_icon_FactionUM",
              "C2 ★★ 主图 = **Ultramarines(10) 的阵营徽记**（原版 `image.sprite = ArmyUtilities.GetArmyIcon(army)`；"
              + "⛔ 不是 prefab 出厂那张 `40K_genearl_icon_Campaign_points_big` —— 运行时会被换掉）");

            var r0 = ItemDrawer.DrawCampaignPointsReward(go.transform, box, 200, 0 /* Neutral */, st);
            C(r0.Art, (string)null,
              "C3 ★ 两态对照：`army = 0 (Neutral)` ⇒ `GetArmyIcon` 落空 ⇒ 原版把 `Image` **关掉**、整格不画"
              + "（`Art` 保持 null；⛔ 不拿默认图顶）");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------ D 账 `商店:162`
    static void SectionD_报价格容器调用方()
    {
        MenuCheck.Section(_sink, "D 账 商店:162 —— 模式卡拿到 liveop 事件 ⇒ 调一次 `MainMenuOfferContainer.OnInitialize`");

        var host = new GameObject("B97SHOP_OfferHost", typeof(RectTransform));
        var card = new GameObject("Main Menu Offer Container Carousel 1x1", typeof(RectTransform));
        var other = new GameObject("Base Game Mode Container 1x2 - Skirmish", typeof(RectTransform));
        try
        {
            card.transform.SetParent(host.transform, false);
            other.transform.SetParent(host.transform, false);

            var evt = new LiveOpsLocalEvent { EventId = "b97shop-offer" };

            // 前提（硬检查）：族判定
            C(MainMenuRuntime.IsOfferContainerPrefab("Main Menu Offer Container Static Image 1x2"), true,
              "D1 前提：报价格容器族判定 —— 原版 5 份 prefab 的根名（`… 1x1` / `… Carousel 1x1` / `… Static Image 1x1` / `… Static Image 1x2` / `… 1x1 Premium_…`）都以此开头");
            C(MainMenuRuntime.IsOfferContainerPrefab("Base Game Mode Container 1x2 - Skirmish"), false,
              "D2 前提：普通模式卡**不**落这一族");

            // 反例 1：非报价格卡 + 有活动 ⇒ 不接
            MainMenuRuntime.BindMainMenuOfferContainer(other.transform, evt, 535f, 848.8f);
            C(other.GetComponent<MainMenuOfferContainer>(), null,
              "D3 ★ 非报价格卡 ⇒ **不挂**容器组件（原版只有报价格容器 prefab 上才有这颗 `m_Script`）");

            // 反例 2：报价格卡 + 没有活动归属（手挑卡）⇒ 不接
            MainMenuRuntime.BindMainMenuOfferContainer(card.transform, null, 535f, 414.4f);
            C(card.GetComponent<MainMenuOfferContainer>(), null,
              "D4 ★ 手挑卡（`owner == null`）⇒ **不挂**（原版容器全部由 liveop 事件实例化，没有静态卡）");

            // 正例：报价格卡 + 有活动 ⇒ 挂上并喂一次 OnInitialize
            MainMenuRuntime.BindMainMenuOfferContainer(card.transform, evt, 535f, 414.4f);
            var box = card.GetComponent<MainMenuOfferContainer>();
            Tr(box != null, "D5 ★★ 报价格卡（根名属该族 + 有在期活动）⇒ 挂上 `MainMenuOfferContainer`");
            if (box != null)
            {
                C(box.InitializeCalls, 1,
                  "D6 ★★ `OnInitialize(data.Event)` **被调了一次**（原版 = `ContainerBuilder<T>.MakeContainer` 之后宿主那一跳；"
                  + "此前全仓唯一调用方是自检）");
                Tr(box.LastEvent != null && box.LastEvent.Id == "b97shop-offer",
                  "D7 ★ 喂进去的就是**这一份**活动（不是 null、也不是别人的）");
                Tr(box.Drawer != null,
                  "D8 ★ 第 ③ 拍 `GetComponent<GeneralOfferPopupDrawerBase>()` 有对位：卡下真建了抽屉");
                C(box.DrawerInitialized, true,
                  "D9 ★ 第 ④ 拍 `drawer.Initialize(GetBaseData())` 跑到了（`Base` 传 null ⇒ 由抽屉那条**既有**告警出声，"
                  + "⛔ 不编假报价 —— 报价 item 在远端）");
                Tr(MainMenuRuntime.LastOfferContainerForTest == box,
                  "D10 读口 `LastOfferContainerForTest` 指向它（宿主/后续自检用）");
            }
        }
        finally { Object.DestroyImmediate(host); }
    }
}
