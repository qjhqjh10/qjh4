// B109_E23_DailyClick_Assert.cs — 第一百零九会话 · AUD05 §J6（每日奖励「Locked 态抽屉点击」）执行代理 `B109_E23` 的断言
//   施工单 = `资料/完全复刻/判据/B109_R13_AUD05剩余.md` §4（J6：原版 prefab 定位 / `OnPointerClick` 逐句 / 改法 / 断言要点）。
//   报告   = `资料/完全复刻/报告/B109_E23.md`。
//
// ============================ 它盯的是什么 ============================
//  原版 `ItemDrawer<T>.OnPointerClick`（`decomp_generic_trial/out/ItemDrawer_object___OnPointerClick.c`）三跳：
//    ① `if (showItemInfoOnClick(+0x44) == 0) return;`                 —— 闸关 ⇒ 连音都没有
//    ② `if (onClick(+0x50) == null) HandlePointerClick(); else onClick.Invoke();`
//       `HandlePointerClick()` = `WindowsManager.OpenWindow(new ItemDisplayWindowContext(item, quantity))`
//       （`ItemDrawer_object___HandlePointerClick.c:15-25`）—— **`Count <= 1` 时的常态就是这个**；
//       `onClick` 只由 `DailyRewardDrawerController.Initialize` 的 `if (1 < rewards.Count)` 挂
//    ③ `if (inspectSound(+0x48) != null) inspectSound.Play2D();`     —— **两条分支都放**
//  我们原来（`B106_W51`）只做了 ② 的前半（预览）+ 闸关那一档的静音 ⇒ `else` 支**既不开窗也不出声**（两处偏离）。
//  本件盯的就是「`else` 支 = 开物品展示窗 + 那一声音」。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B109_E23_DailyClick_Assert.Run -logFile "d:/4/_tmp_view/b109e23.log"
//   🔴 **接线由调度台做**（9 个断言宿主不在本代理白名单）。推荐宿主 = `Editor/RewardsScene.cs`（每日奖励窗就在那儿），
//      在它那条聚合行末尾**同一物理行**追加（行数零变化）：
//          B109_E23_DailyClick_Assert.Run(); CheckSink.Pass += B109_E23_DailyClick_Assert.Pass; ...
//      ⚠️ 具体累加写法照该宿主现有的 `_sink.Pass += …` 那一行抄（各宿主计数器是**逐宿主**的）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A4：把 `hit.playClickSound` 写回 `false` ⇒ 红（原版两条分支都放 `inspectSound`）。
//  · A5：删掉 `hit.clickCue = WindowSounds.ButtonClick1Cue` ⇒ 走默认按钮音（`DefaultButtonCue`）⇒ 红。
//  · A6 / B1：把 `else` 支改回「`onClick` 留空」⇒ 红（`Count == 1` 时 `onClick` 必须挂，这是本轮新增的那一半）。
//  · B3：把 `Title` 换成别的串（或空）⇒ 红。
//  · B6：点一下顺手发了奖（接错 `CollectReward`）⇒ 红。
//  · C3/C4：让新分支抢走旧的（`Count > 1` 也开物品窗）⇒ 红。
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B109_E23`：每日奖励抽屉点一下 —— Locked 态开物品展示窗 + 放 `inspectSound`（原版 `ItemDrawer.OnPointerClick`）。</summary>
public static class B109_E23_DailyClick_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B109E23] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B109E23 每日奖励抽屉点击（AUD05 §J6）：`Count <= 1` 那一支 ⇒ 开物品展示窗 + 放 inspectSound");

        var wm = WindowsManager.Get();
        Tr(wm != null, "A0 **前提**：拿得到 `WindowsManager`（拿不到 ⇒ 下面全部不可信，⛔ 不是绿）");
        if (wm == null) { Debug.Log(_sink.P + "B109E23 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败"); return; }

        // 三个 Test 口先摆回出厂态（别的谱系可能留着），收尾再原样还回去。
        var saveCount = DailyData.RewardCountOverrideForTest;
        var saveSpecs = DailyData.RewardSpecsOverrideForTest;
        var saveClick = DailyData.RewardClickableOverrideForTest;
        DailyData.RewardCountOverrideForTest = -1;
        DailyData.RewardSpecsOverrideForTest = null;
        DailyData.RewardClickableOverrideForTest = null;

        bool saveSfx = WarpforgeVFX.WFSoundPlayer.Enabled;
        WarpforgeVFX.WFSoundPlayer.Enabled = false;   // 批处理不真发声（同 `Editor/DeckScene.cs:10286` 的先例）；`WindowSounds.LastCue` 在拨给播放器**之前**就记下了
        try
        {
            SectionA_Config(wm);
            SectionB_ClickOpensItemWindow(wm);
            SectionC_MultiRewardStillPreviews(wm);
        }
        catch (System.Exception ex)
        {
            Tr(false, "（不静默）夹具抛了异常：" + ex.GetType().Name + "：" + ex.Message);
        }
        finally
        {
            DailyData.RewardCountOverrideForTest = saveCount;
            DailyData.RewardSpecsOverrideForTest = saveSpecs;
            DailyData.RewardClickableOverrideForTest = saveClick;
            WarpforgeVFX.WFSoundPlayer.Enabled = saveSfx;
            WindowSounds.ResetCounters();
            RewardWindowFixture.DismissRewardWindows();
        }

        Debug.Log(_sink.P + "B109E23 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具
    /// <summary>今天 `Locked` 的那一天普通轨（`RewardStateOf` 现算；出厂 = 第 3 天，但不写死）。</summary>
    static int LockedDay()
    {
        for (int d = 0; d < DailyData.RewardDays; d++)
            if (DailyData.RewardStateOf(d, false) == RewardState.Locked) return d;
        return -1;
    }

    /// <summary>建一扇每日奖励窗（生产入口 `Create` + `OpenWindow`，⛔ 不复制建法）。</summary>
    static DailyRewardPopup BuildPopup(WindowsManager wm)
    {
        var dr = DailyRewardPopup.Create(wm);
        if (dr == null) return null;
        wm.OpenWindow(dr);
        return dr;
    }

    static void KillPopup(DailyRewardPopup dr)
    {
        if (dr == null) return;
        try { dr.Close(); } catch { }
        MainMenuSubmenuWindow.DestroySafe(dr.gameObject);
    }

    // ============================================================ A · 命中件的静态配置（不点）
    static void SectionA_Config(WindowsManager wm)
    {
        MenuCheck.Section(_sink, "A 命中件配置：可点 / 按钮音没被关 / cue = 原版 `inspectSound` / `Count==1` 也挂回调");

        var dr = BuildPopup(wm);
        Tr(dr != null && dr.CurrentState == WindowState.Open, "A1 **前提**：每日奖励窗开出来了");
        if (dr == null) return;

        int day = LockedDay();
        Tr(day >= 0, "A2 **前提**：找到了一个 `Locked` 态的日子（本轮要验的就是这一档）");
        if (day < 0) { KillPopup(dr); return; }

        C(DailyData.RewardCountOf(day, false), 1,
          "A3 **前提**：出厂这一天普通轨的奖励列表 = **1 条**（⇒ 原版走 `HandlePointerClick()` 那一支，⛔ 不是预览）");

        var hit = dr.PreviewRewardsHitForTest(day, false);
        Tr(hit != null, "A4 **前提**：那一天的抽屉命中件建出来了");
        if (hit == null) { KillPopup(dr); return; }

        Tr(hit.Interactable, "A5 `Locked` 态命中件**可点**（`Drawer.SetState` 只关 `Unlocked`/`Collected` 两档）"
                           + "｜🧨 把 `SetState` 那句 `Interactable` 改成恒 false ⇒ 红");

        Tr(hit.playClickSound, "A6 ★ **按钮音没被关**（原版 `inspectSound.Play2D()` 写在闸后面、**两条分支都放**）"
                             + "｜🧨 把 `else` 支写回 `hit.playClickSound = false` ⇒ 红");

        C(hit.clickCue, WindowSounds.ButtonClick1Cue,
          "A7 ★ 点击音 cue = 原版 `inspectSound` = `" + WindowSounds.ButtonClick1Cue + "`（clip `UI Click 1`；"
        + "判据 `ItemDrawer_object___OnPointerClick.c:38-41` + 374 份 MB 的 PPtr 同值）"
        + "｜🧨 删掉 `hit.clickCue = …` 那行 ⇒ 走默认按钮音 ⇒ 红");

        Tr(hit.onClick != null,
          "A8 ★ **`Count == 1` 时 `onClick` 也挂上了**（原版没有 `OverrideOnClickAction` 时走 `HandlePointerClick()`；"
        + "我们这一侧两颗口合成一颗命中件）｜🧨 把 `else` 支改回「留空」⇒ 本条与 B 段一起红");

        Tr(WarpforgeVFX.WFSoundBank.HasCue(WindowSounds.ButtonClick1Cue),
          "A9 **前提**：cue `" + WindowSounds.ButtonClick1Cue + "` 在音效表里（不在 ⇒ 下面那一声音验不到，⛔ 不是绿）");

        KillPopup(dr);
    }

    // ============================================================ B · 真点一下 ⇒ 开物品展示窗 + 那一声
    static void SectionB_ClickOpensItemWindow(WindowsManager wm)
    {
        MenuCheck.Section(_sink, "B Locked 态点一下：开 `ItemDisplayWindow`（物品 = 那天的图 + 名）＋ `WindowSounds.LastCue` = inspectSound");

        var dr = BuildPopup(wm);
        if (dr == null) { Tr(false, "B0 **前提不成立**：每日奖励窗没建出来"); return; }

        int day = LockedDay();
        if (day < 0) { Tr(false, "B0 **前提不成立**：没有 `Locked` 态的日子"); KillPopup(dr); return; }

        var hit = dr.PreviewRewardsHitForTest(day, false);
        if (hit == null) { Tr(false, "B0 **前提不成立**：命中件没建出来"); KillPopup(dr); return; }

        string art = DailyData.RewardIconOf(day, false);
        int goldBefore = Wallet.Of(art);
        var idwBefore = ItemDisplayWindow.LastOpened;
        // 🔴 **复用那一支会让「开出来的是一扇新的」判据假红**：`OpenByRef` 命中缓存 + `TryOpen` 在 `Open` 态早退
        //    ⇒ `LastOpened` 会停在同一具上。先关掉场上还开着的那扇（`Close()` 对已关的窗是首句早退，安全）。
        if (idwBefore != null && idwBefore.CurrentState != WindowState.Closed) idwBefore.Close();

        WindowSounds.ResetCounters();
        hit.ClickForTest();                                  // = 真点（`WindowButton.Click()` 那条生产路）
        string cueAfter = WindowSounds.LastCue;              // 🔴 快照：关窗音会把 `LastCue` 覆盖
        var idw = ItemDisplayWindow.LastOpened;

        Tr(idw != null && !ReferenceEquals(idw, idwBefore),
          "B1 ★ 点一下 ⇒ **新开出一扇 `Item Display Window`**（原版 `HandlePointerClick()` → "
        + "`WindowsManager.OpenWindow(new ItemDisplayWindowContext(item, quantity))`）"
        + "｜🧨 `else` 支不挂 `onClick` ⇒ 红");

        if (idw != null)
        {
            C(idw.Ctx.Title, DailyData.RewardName(day, false),
              "B2 ★ 窗里标题 = 那一格的奖励名（= 抽屉上印的那一条；原版 `item.LocalizedName` 那一格）"
            + "｜🧨 传空串 / 传别的天数 ⇒ 红");
            C(idw.Ctx.ArtName, art,
              "B3 ★ 窗里的图 = 抽屉图标那张（原版 `ItemDrawer.Draw` 把 `image.sprite` 换成 `item.CosmeticSprite`）");
            Tr(idw.Ctx.Art != null, "B4 ★ …那张图真取得到（`ItemDrawer.ArtTexture(\"" + art + "\")` 非空）"
                                  + "｜🧨 传 null ⇒ 图那一格不画 ⇒ 红");
            Tr(idw.CurrentState != WindowState.Closed, "B5 窗是**开着**的（⛔ 不是建出来就关）");
        }

        C(cueAfter, WindowSounds.ButtonClick1Cue,
          "B6 ★ 点这一下 ⇒ 壳侧播放口收到的 cue = `" + WindowSounds.ButtonClick1Cue + "`（原版 `inspectSound.Play2D()`）"
        + "｜🧨 只开窗不放音 ⇒ 红。实测 = " + (cueAfter ?? "<null>"));

        C(Wallet.Of(art), goldBefore, "B7 点开物品展示窗**不发奖**（`Wallet` 那一格不变）｜🧨 误接 `CollectReward` ⇒ 红");

        if (idw != null) { idw.Close(); MainMenuSubmenuWindow.DestroySafe(idw.gameObject); }
        KillPopup(dr);
    }

    // ============================================================ C · `Count > 1` 仍走预览（别让新分支抢走旧的）
    static void SectionC_MultiRewardStillPreviews(WindowsManager wm)
    {
        MenuCheck.Section(_sink, "C 回归：`Count > 1` + 可点 ⇒ 仍是「预览整串奖励」（原版 `OverrideOnClickAction` 那一支）");

        DailyData.RewardCountOverrideForTest = 2;
        DailyData.RewardSpecsOverrideForTest = new[]
        {
            new CampaignData.RewardSpec("40k_topmarquee_currency_gold", 150, CampaignData.TierBasic),
            new CampaignData.RewardSpec("40K_missions_icon_Daily_skulls", 20, CampaignData.TierBasic),
        };
        DailyData.RewardClickableOverrideForTest = true;

        var dr = BuildPopup(wm);
        if (dr == null) { Tr(false, "C0 **前提不成立**：每日奖励窗没建出来"); return; }

        int day = LockedDay();
        var hit = day >= 0 ? dr.PreviewRewardsHitForTest(day, false) : null;
        if (hit == null) { Tr(false, "C0 **前提不成立**：命中件 / `Locked` 日没拿到"); KillPopup(dr); return; }

        C(DailyData.RewardCountOf(day, false), 2, "C1 **前提**：这一格现在摆成 2 条奖励");

        var idwBefore = ItemDisplayWindow.LastOpened;
        int openBefore = RewardWindowFixture.OpenRewardWindowCount();

        hit.ClickForTest();
        var rw = RewardWindowFixture.FindOpenRewardWindow();

        Tr(rw != null && RewardWindowFixture.OpenRewardWindowCount() > openBefore,
          "C2 ★ `Count > 1` 点一下 ⇒ 开的是**领奖窗**（预览态），⛔ 不是物品展示窗"
        + "｜🧨 把新分支写成「无条件开物品窗」⇒ 红");
        Tr(rw != null && rw.IsPreview, "C3 ★ …而且是 **Preview 态**（原版 `RewardService.Preview`，`isPreview = 1`）");
        C(rw != null && rw.Context != null ? rw.Context.Rewards.Length : -1, 2,
          "C4 ★ …预览窗里是**两条**奖励（= 抽屉的 `rewards.Count`，⛔ 不是一条）");
        Tr(ReferenceEquals(ItemDisplayWindow.LastOpened, idwBefore),
          "C5 灭自证：这一下**没有**开物品展示窗（旧的预览分支没被新分支顶掉）｜🧨 两个分支接反 ⇒ 红");

        if (rw != null) rw.Close();                 // 关窗会走 `RefreshOpenDailyWindows` ⇒ 弹窗自己重建
        RewardWindowFixture.DismissRewardWindows();
        KillPopup(dr);
    }
}
