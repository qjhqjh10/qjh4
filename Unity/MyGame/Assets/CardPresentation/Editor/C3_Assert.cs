// C3_Assert.cs — 第一百零九会话（C 组）· 执行代理 `C3` 的断言
//   施工单 = `资料/完全复刻/报告/B109_E23.md` §五·③ / §五·④ · `判据/B109_R13_AUD05剩余.md` §J6 · `判据/B109_D1_八条红.md` §5/§6。
//   报告   = `资料/完全复刻/报告/C3.md`。
//
// ============================ 它盯的三件 ============================
//  ① **`DailyStreakPopup.BuildPreviewHit` 的 `else` 支**（`Shell/DailyStreakPopup.cs`）：
//     原版 `ItemDrawer<T>.OnPointerClick`（`decomp_generic_trial/out/ItemDrawer_object___OnPointerClick.c`）三跳：
//       ① `if (showItemInfoOnClick(+0x44) == 0) return;`
//       ② `if (onClick(+0x50) == null) HandlePointerClick(); else onClick.Invoke();`
//          `HandlePointerClick()` = `WindowsManager.OpenWindow(new ItemDisplayWindowContext(item, quantity))`
//          —— **`Count <= 1` 时的常态就是它**；`onClick` 只由 `if (1 < rewards.Count)` 挂
//       ③ `if (inspectSound(+0x48) != null) inspectSound.Play2D();`  —— **两条分支都放**
//     我们原来（`B106_W51`）的 `else` 支是 `hit.playClickSound = false;` ⇒ **既不开窗又静音**（两处偏离）。
//  ② **`DailyData.RewardClickable` 对「每日/连登」这一格是误判**：
//     R13 已把这一格用的 prefab 钉死（`ItemDrawerConfig` 的 `Currency ▸ Key:10` = `Icon Currency Drawer Variant`，
//     `showItemInfoOnClick = 1`）⇒ 一刀切 `false` 与原版不符；`C3` 改成按图标映射（🔶 自设计，见 `ItemTypeNameOfIcon`）。
//  ③ 🆕 **2026-10-31（`B110_W1`）：上面 ① 之前还有一道闸** —— `showItemInfoOnClick == 0` ⇒ **整条 return**
//     （`ItemDrawer_object___OnPointerClick.c:14`），`inspectSound.Play2D()` 在闸内 ⇒ **连音都没有**。
//     两处 `BuildPreviewHit`（`DailyRewardPopup` / `DailyStreakPopup`）的 `else` 支原来**无条件**开窗 + 无条件播音
//     ⇒ 点卡包那几格会开一扇原版不会开的窗、还响一声。D 段盯它（闸关 ⇒ 三样都没有 / 灭自证：闸开 ⇒ 三样都回来）。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod C3_Assert.Run -logFile "d:/4/_tmp_view/c3.log"
//   🔴 **接线由调度台做**（9 个断言宿主不在本代理白名单）。推荐宿主 = `Editor/RewardsScene.cs`（连登窗就在那儿），
//      在它那条聚合行末尾**同一物理行**追加（行数零变化）：
//          C3_Assert.Run(); CheckSink.Pass += C3_Assert.Pass; ...
//      ⚠️ 具体累加写法照该宿主现有的 `_sink.Pass += …` 那一行抄（各宿主计数器是**逐宿主**的）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A2：把 `ItemTypeNameOfIcon` 的 gold 那一档删掉 ⇒ 落「查不到 ⇒ false」⇒ 红。
//  · A3：把映射写成「一刀切 true」⇒ 卡包那两条红（`40k_main_bt_rewards` 原版 = `ShopContainer` ⇒ 0）。
//  · A6：`RewardClickableOverrideForTest` 不再优先 ⇒ 红。
//  · B4：把 `else` 支写回 `hit.playClickSound = false;` ⇒ B4/B6 一起红。
//  · B5：删掉 `hit.clickCue = …` ⇒ 走默认按钮音（`DefaultButtonCue`）⇒ 红。
//  · B7/B8：把 `else` 支改回「`onClick` 留空」⇒ 红。
//  · B8/B9：传空串 / 传别的天数 / 传 null 图 ⇒ 红。
//  · B11：点一下顺手发了奖（接错 `CollectStreak`）⇒ 红。
//  · C3/C5：让新分支抢走旧的（`Count > 1` 也开物品窗）⇒ 红。
//  · D4-D8：把 `else` 支改回「无条件挂回调 + 无条件播音」（= `B110_W1` 之前那样）⇒ 红 —— 真点卡包那一格会开窗 + 响一声。
//  · D9-D11：闸不读（恒 true / 恒 false）⇒ 卡包那一格与金币那一格读数会分不开 ⇒ 红。
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `C3`：连登奖格的「未领态点一下 ⇒ 开物品展示窗 + 放 `inspectSound`」＋ 可点判据按图标映射。</summary>
public static class C3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[C3] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "C3 连登奖格点击（同族同病 B109_E23 §五·④）＋ `RewardClickable` 按图标映射（§五·③）");

        var wm = WindowsManager.Get();
        Tr(wm != null, "A0 **前提**：拿得到 `WindowsManager`（拿不到 ⇒ 下面全部不可信，⛔ 不是绿）");
        if (wm == null) { Debug.Log(_sink.P + "C3 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败"); return; }

        // 三个 Test 口先摆回出厂态（别的谱系可能留着），收尾再原样还回去。
        var saveCount = DailyData.StreakRewardCountOverrideForTest;
        var saveSpecs = DailyData.StreakRewardSpecsOverrideForTest;
        var saveClick = DailyData.RewardClickableOverrideForTest;
        DailyData.StreakRewardCountOverrideForTest = -1;
        DailyData.StreakRewardSpecsOverrideForTest = null;
        DailyData.RewardClickableOverrideForTest = null;

        bool saveSfx = WarpforgeVFX.WFSoundPlayer.Enabled;
        WarpforgeVFX.WFSoundPlayer.Enabled = false;   // 批处理不真发声（同 `Editor/DeckScene.cs:10286` 的先例）；`WindowSounds.LastCue` 在拨给播放器**之前**就记下了
        try
        {
            SectionA_ClickableMapping();
            SectionB_LockedClickOpensItemWindow(wm);
            SectionC_MultiRewardStillPreviews(wm);
            SectionD_ShowItemInfoGateOff(wm);
        }
        catch (System.Exception ex)
        {
            Tr(false, "（不静默）夹具抛了异常：" + ex.GetType().Name + "：" + ex.Message);
        }
        finally
        {
            DailyData.StreakRewardCountOverrideForTest = saveCount;
            DailyData.StreakRewardSpecsOverrideForTest = saveSpecs;
            DailyData.RewardClickableOverrideForTest = saveClick;
            WarpforgeVFX.WFSoundPlayer.Enabled = saveSfx;
            WindowSounds.ResetCounters();
            RewardWindowFixture.DismissRewardWindows();
        }

        Debug.Log(_sink.P + "C3 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 可点判据按图标映射
    static void SectionA_ClickableMapping()
    {
        MenuCheck.Section(_sink, "A `RewardClickable` / `StreakRewardClickable`：图标 → 原版 item 类型 → `showItemInfoOnClick`");

        Tr(DailyData.RewardDays >= 4, "A1 **前提**：每日奖励至少 4 天（本映射要读到第 4 天那颗骷髅图标）");
        if (DailyData.RewardDays < 4) return;

        // 出厂表（`_rewardFreeIcon` / `_rewardPremIcon` / `_streakIcon`）——⛔ 不写死「第几天」，用图标名反查下标。
        int goldDay = IconDay("40k_topmarquee_currency_gold");
        int packDay = IconDay("40k_main_bt_rewards");
        int skullDay = IconDay("40K_missions_icon_Daily_skulls");
        Tr(goldDay >= 0, "A2 **前提**：每日表里找得到 gold 那一天（`40k_topmarquee_currency_gold`）");
        Tr(packDay >= 0, "A3 **前提**：每日表里找得到卡包那一天（`40k_main_bt_rewards`）");
        Tr(skullDay >= 0, "A4 **前提**：每日表里找得到骷髅那一天（`40K_missions_icon_Daily_skulls`）");

        if (goldDay >= 0)
            Tr(DailyData.RewardClickable(goldDay, false),
               "A5 ★ `gold` 那一天 ⇒ **可点**（原版 = `Currency` 抽屉 ⇒ `showItemInfoOnClick = 1`，R13 §4·1）"
             + "｜🧨 把映射里 gold 那一档删掉 ⇒ 落「查不到 ⇒ false」⇒ 红");

        if (packDay >= 0)
            Tr(!DailyData.RewardClickable(packDay, false),
               "A6 ★ `40k_main_bt_rewards`（卡包）那一天 ⇒ **不可点**（原版 = `Everguild.LiveOps.ShopContainer` ⇒ `0`，"
             + "`Icon Container Drawer Variant`；§1.5）"
             + "｜🧨 把映射写成「一刀切 true」⇒ 本条红（这条同时证明 A5 不是恒真）");

        if (skullDay >= 0)
            Tr(DailyData.RewardClickable(skullDay, false),
               "A7 ★ `skulls` 那一天 ⇒ **可点**（🟡 映射按 E23 §五·③ 的裁定：`Daily_skulls` 按货币计 ⇒ `Currency`；"
             + "⛔ 本地没有它的原版 item SO，这是近似、不是原版判据 —— 见 `C3.md`「没查清」）");

        // 连登轨用同一个映射（原版两窗是同一段字节）
        int sGold = -1, sPack = -1;
        for (int i = 0; i < DailyData.StreakDays; i++)
        {
            if (sGold < 0 && DailyData.StreakRewardIcon(i) == "40k_topmarquee_currency_gold") sGold = i;
            if (sPack < 0 && DailyData.StreakRewardIcon(i) == "40k_main_bt_rewards") sPack = i;
        }
        Tr(sGold >= 0 && sPack >= 0, "A8 **前提**：连登表里同时有 gold 与卡包两格");
        if (sGold >= 0) Tr(DailyData.StreakRewardClickable(sGold), "A9 ★ 连登 `gold` 那一格 ⇒ **可点**（同一映射）");
        if (sPack >= 0) Tr(!DailyData.StreakRewardClickable(sPack), "A10 ★ 连登 `40k_main_bt_rewards` 那一格 ⇒ **不可点**");

        // 自检口仍然最优先（`RewardsScene` / `B109_E23` 那一族都靠它摆状态）
        if (goldDay >= 0 && packDay >= 0)
        {
            DailyData.RewardClickableOverrideForTest = false;
            Tr(!DailyData.RewardClickable(goldDay, false), "A11 自检口 `RewardClickableOverrideForTest = false` **压过**真实映射"
                                                       + "｜🧨 改成先查映射 ⇒ 红");
            DailyData.RewardClickableOverrideForTest = true;
            Tr(DailyData.RewardClickable(packDay, false), "A12 自检口 `= true` **压过**真实映射（卡包也能被摆成可点）");
            DailyData.RewardClickableOverrideForTest = null;
            Tr(!DailyData.RewardClickable(packDay, false), "A13 （灭自证）复位 null 之后回到真实判据（卡包重新不可点）");
        }
    }

    /// <summary>在每日奖励的出厂表里按图标名找一天（找不到 ⇒ −1）。⛔ 不写死下标。</summary>
    static int IconDay(string art)
    {
        for (int d = 0; d < DailyData.RewardDays; d++)
            if (DailyData.RewardIconOf(d, false) == art) return d;
        return -1;
    }

    // ============================================================ B · 未领那一格真点一下 ⇒ 开物品展示窗 + 那一声
    static void SectionB_LockedClickOpensItemWindow(WindowsManager wm)
    {
        MenuCheck.Section(_sink, "B 连登窗未领那一格点一下：开 `ItemDisplayWindow`（物品 = 那格的图 + 名）＋ `WindowSounds.LastCue` = inspectSound");

        var ds = BuildStreak(wm);
        if (ds == null) { Tr(false, "B0 **前提不成立**：连登窗没建出来"); return; }

        // 挑一格：**未领**（`claimWins = false` ⇒ 命中件可点）且命中件建出来了（视口外那几格不建）。
        int day = FindLockedClickableDay(ds);
        Tr(day >= 0, "B1 **前提**：找到一格「未领 + 命中件建出来 + 可点」的奖格（本轮要验的就是这一档）");
        if (day < 0) { KillStreak(ds); return; }

        C(DailyData.StreakRewardCountOf(day), 1,
          "B2 **前提**：这一格出厂奖励列表 = **1 条**（⇒ 原版走 `HandlePointerClick()` 那一支，⛔ 不是预览）");

        var hit = ds.PreviewRewardsHitForTest(day);
        Tr(hit != null, "B3 **前提**：那一格的命中件拿得到");
        if (hit == null) { KillStreak(ds); return; }

        Tr(hit.playClickSound, "B4 ★ **按钮音没被关**（原版 `inspectSound.Play2D()` 写在闸后面、**两条分支都放**）"
                             + "｜🧨 把 `else` 支写回 `hit.playClickSound = false` ⇒ 红");

        C(hit.clickCue, WindowSounds.ButtonClick1Cue,
          "B5 ★ 点击音 cue = 原版 `inspectSound` = `" + WindowSounds.ButtonClick1Cue + "`（clip `UI Click 1`；"
        + "判据 `ItemDrawer_object___OnPointerClick.c:38-41`）"
        + "｜🧨 删掉 `hit.clickCue = …` 那行 ⇒ 走默认按钮音 ⇒ 红");

        Tr(hit.onClick != null,
          "B6 ★ **`Count == 1` 时 `onClick` 也挂上了**（原版没有 `OverrideOnClickAction` 时走 `HandlePointerClick()`）"
        + "｜🧨 把 `else` 支改回「`onClick` 留空」⇒ 本条与 B 段下面一起红");

        Tr(WarpforgeVFX.WFSoundBank.HasCue(WindowSounds.ButtonClick1Cue),
          "B7 **前提**：cue `" + WindowSounds.ButtonClick1Cue + "` 在音效表里（不在 ⇒ 下面那一声音验不到，⛔ 不是绿）");

        string art = DailyData.StreakRewardIcon(day);
        int goldBefore = Wallet.Of(art);
        var idwBefore = ItemDisplayWindow.LastOpened;
        // 复用那一支会让「开出来的是一扇新的」判据假红（同 `B109_E23` B 段的坑）：先关掉场上还开着的那扇。
        if (idwBefore != null && idwBefore.CurrentState != WindowState.Closed) idwBefore.Close();

        WindowSounds.ResetCounters();
        hit.ClickForTest();                                  // = 真点（`WindowButton.Click()` 那条生产路）
        string cueAfter = WindowSounds.LastCue;              // 🔴 快照：关窗音会把 `LastCue` 覆盖
        var idw = ItemDisplayWindow.LastOpened;

        Tr(idw != null && !ReferenceEquals(idw, idwBefore),
          "B8 ★ 点一下 ⇒ **新开出一扇 `Item Display Window`**（原版 `HandlePointerClick()` → "
        + "`WindowsManager.OpenWindow(new ItemDisplayWindowContext(item, quantity))`）"
        + "｜🧨 `else` 支不挂 `onClick` ⇒ 红");

        if (idw != null)
        {
            C(idw.Ctx.Title, DailyData.StreakRewardName(day),
              "B9 ★ 窗里标题 = 那一格的奖励名（原版 `item.LocalizedName` 那一格）"
            + "｜🧨 传空串 / 传别的天数 ⇒ 红");
            C(idw.Ctx.ArtName, art,
              "B10 ★ 窗里的图 = 奖格图标那张（原版 `ItemDrawer.Draw` 把 `image.sprite` 换成 `item.CosmeticSprite`）");
            Tr(idw.Ctx.Art != null, "B11 ★ …那张图真取得到（`ItemDrawer.ArtTexture(\"" + art + "\")` 非空）"
                                  + "｜🧨 传 null ⇒ 图那一格不画 ⇒ 红");
            Tr(idw.CurrentState != WindowState.Closed, "B12 窗是**开着**的（⛔ 不是建出来就关）");
        }

        C(cueAfter, WindowSounds.ButtonClick1Cue,
          "B13 ★ 点这一下 ⇒ 壳侧播放口收到的 cue = `" + WindowSounds.ButtonClick1Cue + "`（原版 `inspectSound.Play2D()`）"
        + "｜🧨 只开窗不放音 ⇒ 红。实测 = " + (cueAfter ?? "<null>"));

        C(Wallet.Of(art), goldBefore, "B14 点开物品展示窗**不发奖**（`Wallet` 那一格不变）｜🧨 误接 `CollectStreak` ⇒ 红");

        if (idw != null) { idw.Close(); MainMenuSubmenuWindow.DestroySafe(idw.gameObject); }
        KillStreak(ds);
    }

    // ============================================================ C · `Count > 1` 仍走预览（别让新分支抢走旧的）
    static void SectionC_MultiRewardStillPreviews(WindowsManager wm)
    {
        MenuCheck.Section(_sink, "C 回归：`Count > 1` + 可点 ⇒ 仍是「预览整串奖励」（原版 `OverrideOnClickAction` 那一支）");

        DailyData.StreakRewardCountOverrideForTest = 2;
        DailyData.StreakRewardSpecsOverrideForTest = new[]
        {
            new CampaignData.RewardSpec("40k_topmarquee_currency_gold", 150, CampaignData.TierBasic),
            new CampaignData.RewardSpec("40K_missions_icon_Daily_skulls", 20, CampaignData.TierBasic),
        };
        DailyData.RewardClickableOverrideForTest = true;

        var ds = BuildStreak(wm);
        if (ds == null) { Tr(false, "C0 **前提不成立**：连登窗没建出来"); return; }

        int day = FindLockedClickableDay(ds);
        var hit = day >= 0 ? ds.PreviewRewardsHitForTest(day) : null;
        if (hit == null) { Tr(false, "C0 **前提不成立**：命中件 / 可点那一格没拿到"); KillStreak(ds); return; }

        C(DailyData.StreakRewardCountOf(day), 2, "C1 **前提**：这一格现在摆成 2 条奖励");

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
        KillStreak(ds);
    }

    // ============================================================ 夹具
    /// <summary>建一扇连登窗（生产入口 `Create` + `OpenWindow`，⛔ 不复制建法）。</summary>
    static DailyStreakPopup BuildStreak(WindowsManager wm)
    {
        var ds = DailyStreakPopup.Create(wm);
        if (ds == null) return null;
        wm.OpenWindow(ds);
        return ds;
    }

    static void KillStreak(DailyStreakPopup ds)
    {
        if (ds == null) return;
        try { ds.Close(); } catch { }
        MainMenuSubmenuWindow.DestroySafe(ds.gameObject);
    }

    /// <summary>挑一格「未领（⇒ 命中件没被 `claimWins` 按灰）+ 命中件建出来了 + **闸开着**」的奖格；没有 ⇒ −1。
    /// <para>🔴 **2026-10-31（`B110_W1`）补第三条判据（`DailyData.StreakRewardClickable`）**：本轮把原版第一道闸
    /// （`showItemInfoOnClick == 0` ⇒ 整条 return）补进 `BuildPreviewHit` 之后，**闸关那一格点一下什么都不做**
    /// ⇒ B 段（「点一下要开窗 + 出声」）的夹具**必须先挑闸开着的那一格**。改前这里不挑，
    /// 而连登第 0 格正是卡包（闸关）⇒ 夹具会挑到一格「本来就不该有反应」的，B4-B13 全红
    /// （**那不是回归，是夹具前提不成立**）。闸关那一档由 D 段单独验。</para></summary>
    static int FindLockedClickableDay(DailyStreakPopup ds)
    {
        if (ds == null || ds.entries == null) return -1;
        for (int d = 0; d < ds.entries.Count; d++)
        {
            if (DailyData.StreakRewardUnlocked(d)) continue;          // 可领那一格：命中件被按灰（`Interactable = false`）
            if (!DailyData.StreakRewardClickable(d)) continue;        // 🆕 B110_W1：闸关那一格原版整条 return（无窗、无声）
            var h = ds.PreviewRewardsHitForTest(d);
            if (h != null && h.Interactable) return d;
        }
        return -1;
    }

    // ============================================================ D · 第一道闸（`showItemInfoOnClick == 0` ⇒ 整条 return）
    // 🆕 2026-10-31（`B110_W1`）：原版 `ItemDrawer<object>.OnPointerClick` **第一句** =
    //   `if (*(char*)(this + 0x44) == 0) return;`（`decomp_generic_trial/out/ItemDrawer_object___OnPointerClick.c:14`）
    //   ⇒ 闸关时**不开窗、不预览、连 `inspectSound.Play2D()` 都不放**（那一句写在闸内，同文件 `:31-34`）。
    // 我们这一侧的闸 = `DailyData.RewardClickable` / `StreakRewardClickable`（= 抽屉 prefab 的 `showItemInfoOnClick`）。
    static void SectionD_ShowItemInfoGateOff(WindowsManager wm)
    {
        MenuCheck.Section(_sink, "D 第一道闸（`showItemInfoOnClick == 0` ⇒ 整条 return）：不开窗、不出声、不挂回调");

        // 本段只用出厂态（C 段会把 Test 口摆成 2 条奖励 + 可点）
        DailyData.StreakRewardCountOverrideForTest = -1;
        DailyData.StreakRewardSpecsOverrideForTest = null;
        DailyData.RewardCountOverrideForTest = -1;
        DailyData.RewardSpecsOverrideForTest = null;
        DailyData.RewardClickableOverrideForTest = null;

        // ---------------------------------------------------------- D1-D8：连登窗「卡包那一格」（真实映射 ⇒ 闸关）
        var ds = BuildStreak(wm);
        if (ds == null) { Tr(false, "D0 **前提不成立**：连登窗没建出来"); return; }

        int pack = -1;
        for (int i = 0; i < DailyData.StreakDays; i++)
            if (DailyData.StreakRewardIcon(i) == "40k_main_bt_rewards" && !DailyData.StreakRewardUnlocked(i)) { pack = i; break; }
        Tr(pack >= 0, "D1 **前提**：连登轨里找得到一格「卡包（闸关）+ 未领」");
        var hitPack = pack >= 0 ? ds.PreviewRewardsHitForTest(pack) : null;
        Tr(hitPack != null, "D2 **前提**：那一格的命中件建出来了（视口内）");
        Tr(hitPack != null && hitPack.Interactable,
          "D3 **前提**：命中件**可点**（⛔ 不是被 `Interactable = false` 挡住的 —— 那样 D4-D8 就成了空转绿）");
        Tr(hitPack != null && hitPack.onClick == null,
          "D4 ★ 闸关 ⇒ **不挂回调**（原版 `ItemDrawer_object___OnPointerClick.c:14` 第一句就 return）"
        + "｜🧨 把 `else` 支改回无条件 `hit.onClick = …` ⇒ 红");
        Tr(hitPack != null && !hitPack.playClickSound,
          "D5 ★ 闸关 ⇒ **按钮音关着**（`inspectSound.Play2D()` 写在闸内，同文件 `:31-34`）"
        + "｜🧨 删掉 `else hit.playClickSound = false` ⇒ 走默认按钮音 ⇒ 红");
        C(hitPack != null ? hitPack.clickCue : "<没有命中件>", null,
          "D6 ★ 闸关 ⇒ **没有 cue**（原版整条 return，`inspectSound` 那一声根本没进）");

        var idwBefore = ItemDisplayWindow.LastOpened;
        int openBefore = RewardWindowFixture.OpenRewardWindowCount();
        WindowSounds.ResetCounters();
        if (hitPack != null) hitPack.ClickForTest();      // = 真点（`WindowButton.Click()` 那条生产路）
        Tr(hitPack != null                                   // ⛔ 前提写进结论里：`hitPack == null` 时这两条不许空转变绿
           && ReferenceEquals(ItemDisplayWindow.LastOpened, idwBefore)
           && RewardWindowFixture.OpenRewardWindowCount() == openBefore,
          "D7 ★ 点一下 ⇒ **一扇窗都没开**（物品展示窗与预览领奖窗都不该有）"
        + "｜🧨 闸没补 ⇒ 卡包那一下会开物品展示窗（B110_M2F 顺手发现 #1）");
        C(hitPack != null ? WindowSounds.LastCue : "<没有命中件>", null,
          "D8 ★ 点一下 ⇒ **一声都没响**（`WindowSounds.LastCue` 仍为 null）");

        KillStreak(ds);

        // ---------------------------------------------------------- D9-D11：灭自证（闸摆成「开」⇒ 三样都回来）
        DailyData.RewardClickableOverrideForTest = true;   // 连登窗读同一个口（`StreakRewardClickable` 共享它）
        var ds2 = BuildStreak(wm);
        if (ds2 == null) { Tr(false, "D9 **前提不成立**：灭自证那一份连登窗没建出来"); return; }
        var hitOpen = pack >= 0 ? ds2.PreviewRewardsHitForTest(pack) : null;
        Tr(hitOpen != null && hitOpen.onClick != null,
          "D9 ★（灭自证）**同一格**把闸摆成开 ⇒ `onClick` **挂上了**（D4 读的确实是闸，不是「这一格恒 null」）");
        Tr(hitOpen != null && hitOpen.playClickSound,
          "D10 ★（灭自证）…按钮音**回来了**（D5 读的确实是闸）");
        C(hitOpen != null ? hitOpen.clickCue : "<没有命中件>", WindowSounds.ButtonClick1Cue,
          "D11 ★（灭自证）…cue 也回来了 = `" + WindowSounds.ButtonClick1Cue + "`（D6 读的确实是闸）");
        DailyData.RewardClickableOverrideForTest = null;
        KillStreak(ds2);

        // ---------------------------------------------------------- D12-D15：每日奖励窗「卡包那一天」（同一道闸的第二处落点）
        var dr = BuildPopup(wm);
        if (dr == null) { Tr(false, "D12 **前提不成立**：每日奖励窗没建出来"); return; }

        int packDay = -1;
        for (int d = 0; d < DailyData.RewardDays; d++)
            if (DailyData.RewardIconOf(d, false) == "40k_main_bt_rewards") { packDay = d; break; }
        Tr(packDay >= 0, "D12 **前提**：每日奖励表里找得到卡包那一天");
        var hitDay = packDay >= 0 ? dr.PreviewRewardsHitForTest(packDay, false) : null;
        Tr(hitDay != null, "D13 **前提**：那一天的命中件建出来了");
        Tr(hitDay != null && hitDay.onClick == null && !hitDay.playClickSound && hitDay.clickCue == null,
          "D14 ★ 每日奖励窗同一处：闸关 ⇒ **不挂回调 + 关按钮音 + 没有 cue**（`DailyRewardPopup.BuildPreviewHit`）"
        + "｜🧨 只补了连登窗那一处、漏了每日奖励窗 ⇒ 红");
        DailyData.RewardClickableOverrideForTest = true;
        dr.ResetRewards(); dr.Build();
        var hitDayOpen = dr.PreviewRewardsHitForTest(packDay, false);
        Tr(hitDayOpen != null && hitDayOpen.onClick != null && hitDayOpen.playClickSound,
          "D15 ★（灭自证）每日奖励窗同一格把闸摆成开 ⇒ 回调与按钮音**都回来**"
        + "｜🧨 闸写成「按图标名硬判」而不是读 `RewardClickable` ⇒ 红");
        DailyData.RewardClickableOverrideForTest = null;

        KillPopup(dr);
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
}
