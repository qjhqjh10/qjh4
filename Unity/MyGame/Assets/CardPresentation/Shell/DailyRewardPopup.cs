// DailyRewardPopup.cs — 「日常」第 2 层：每日奖励窗（原版 `Daily Reward Popup`）
//
// ============================ 出处（唯一正本） ============================
// `资料/日常_原版规格.md` §四（层 × 参数表）· `资料/日常_调用链_三窗.md` §一（调用链 + 三态；⚠️ 更正：原来指 `资料/日常_调用链_DailyRewardPopup.md`，2026-10-10 已并入）。
// **类名照原版**：`DailyRewardPopup : LiveOpsEventWindow<MainMenuMission>`（MB `-1697206695437574193`）。
// prefab 根 pid `-5601131756878274609`，`a=(0,0)-(1,1) sz=(0,0)`。
//
// **窗口参数（实证）**：`type=0(Fullscreen)` · `windowsPlacement=15(Popup)` · `closeOnESC=1` ·
// `updateNavPanel=0` · `extraScaleSmallScreen=1.0` · `useDefaultCloseSoundIfNull=1`。
//
// **矩形出处**：全部由 `工具/menu_rect.py bundle_menus_assets_all "Daily Reward Popup" --depth 4 --relative`
// 与 `… "Daily Reward Popup Entry" --depth 3 --relative --root-size 315.028x812.1` **机械走链**算出，
// 不手抄（本工程手推锚点连错过两次，见 `资料/日常_原版规格.md` §一 的更正痕迹）。
//
// ---- 🔴 三态（本窗「一个值 ≠ 全部情况」）----
// 每个抽屉**各自**持有一个状态：`Locked=0 · Unlocked=1 · Collected=2 · PremiumLocked=3`
// （`DailyRewardDrawerController.State`）。切换的**唯一方法**是 `SetState(state)`
// （`DF:DailyRewardDrawerController__SetState.c:8-28`），它按这张表开关五个件（出处：`资料/日常_调用链_三窗.md` §一·D；⚠️ 更正：原来指 `日常_调用链_DailyRewardPopup.md`，2026-10-10 已并入）：
//   `Premium Indicator`   只有 PremiumLocked 开
//   `Gacha Reward Claimed` 只有 Collected 开 · `Shadow` 只有 Collected 开
//   `Highlight`           只有 Unlocked 开
//   `colider`（真点击区）  只有 Unlocked 开且可点
// 状态怎么算：`normal = index < collectedRewards ? Collected : (target <= cur ? Unlocked : Locked)`；
// `premium = !IsPremium ? PremiumLocked : (normal == Collected && premiumCollected <= index ? Unlocked : normal)`。
//
// ---- 🔴 两处只在 `Initialize` 里设、`Refresh` 不管的（最容易复刻错的地方）----
//   ① `progressStatusImage` → `Personal Progression/Image` 是 **换图不是显隐**
//      （`normal == Locked ? 40k_missions_milestone_off : 40k_missions_milestone_on`）；
//   ② `progressSlider` → **`milestone.Index != 0` 才开**（第 0 天没有进度条）。
//
// ---- ⚠️ 我们挑的（原版查不到 / 单机没有的）----
//   · **数据全是我们自建**（`DailyData`，逐条标明）—— 原版走 PlayFab 云脚本 #910；
//   · **`Shadow` 不建**：它出厂 `INACT`，而且原版靠 `SetState` 在 Collected 时开 —— 我们没有那张影子图
//     （`日常_原版规格.md` §九 的导入清单里没有它）⇒ **如实不画**，不拿别的图顶替；
//   · **`Army Selector`（阵营选择条）不建**：它是 `ScrollRect` + 运行时按 `FeatureConfig.GetValidArmies()` 填的
//     一排阵营图标，单机没有那套资源清单 ⇒ 本层先跳过（Header 的阵营名/图标照画）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>抽屉状态。**值照原版** `DailyRewardDrawerController.State`。</summary>
    public enum RewardState { Locked = 0, Unlocked = 1, Collected = 2, PremiumLocked = 3 }

    /// <summary>`Daily Reward Popup` —— `MainMenuMission` 的 `Popup(12)` 槽指过来的那个窗。
    /// ⚠️ 单机口径：原版由 LiveOps 事件流程自弹（`MainMenuMission.TrySchedulePopup` → `MenuActions` 排队 →
    /// 只在当前窗口**恰好是 `MainMenuWindow`** 时才开火）—— 我们**由主菜单进场时按「本地有没有可领」决定**。</summary>
    public class DailyRewardPopup : GameWindow
    {
        // ---------------- 窗口根（正本 §四 表 #1~#3）
        public const float ShadeW = 4574.6f, ShadeH = 2572.36f;
        /// <summary>🔴 **2026-10-18（T29 · D6）**：alpha 原来是 `0.77f`，原版 `Menu Dark Background` 的 `m_Color` 是
        /// **(0,0,0,0.773)**（判据 = `R_T29_全界面底纹背景对账.md` §13 那张表；同族 `SettingsWindow.ShadeColor` 也是这一档）。</summary>
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.773f);

        public static readonly PxRect Tracks = new PxRect(231.00f, 152.84f, 1947.55f, 964.94f);
        public static readonly PxRect TrackBg = new PxRect(242.37f, 159.34f, 1918.78f, 558.89f);
        public static readonly PxRect SepTop = new PxRect(245.69f, 153.54f, 1918.78f, 168.44f);
        public static readonly PxRect SepBottom = new PxRect(245.69f, 949.34f, 1918.78f, 964.24f);
        public static readonly PxRect ScrollView = new PxRect(242.37f, 159.33f, 1947.55f, 964.94f);

        /// <summary>`Daily Reward Popup Entry` 的作者尺寸（正本 §四）。</summary>
        public const float EntryW = 315.028f, EntryH = 812.1f;
        /// <summary>`Rewards Content` 的 HLG：**spacing = −64**（相邻两格**故意重叠 64**）、align=3(MiddleLeft)。</summary>
        public const float EntrySpacing = -64f;
        public static float EntryPitch { get { return EntryW + EntrySpacing; } }

        // ---------------- 侧栏（正本 §四 「`Tracks Side Bar`」）
        public static readonly PxRect SideBar = new PxRect(-10f, 152.84f, 250.47f, 964.94f);
        public static readonly PxRect SideBarBg = new PxRect(-17.63f, 142.98f, 273.04f, 967.71f);
        public static readonly PxRect FreeTrack = new PxRect(0f, 226.65f, 236.23f, 479.05f);
        public static readonly PxRect FreeIcon = new PxRect(-6.89f, 254.23f, 243.11f, 371.47f);
        public static readonly PxRect FreeTitle = new PxRect(18.11f, 356.17f, 218.11f, 406.17f);
        public static readonly PxRect PremTrack = new PxRect(0f, 631.64f, 236.23f, 884.05f);
        public static readonly PxRect PremIcon = new PxRect(-6.89f, 659.22f, 243.11f, 776.46f);
        public static readonly PxRect PremTitle = new PxRect(18.11f, 761.16f, 218.11f, 811.16f);
        public static readonly PxRect PremPrice = new PxRect(31.09f, 811.16f, 205.14f, 859.22f);
        public static readonly PxRect CloseBtn = new PxRect(58.23f, 902.93f, 182.24f, 1026.95f);

        // ---------------- 顶栏 / 底栏
        public static readonly PxRect Header = new PxRect(0f, 0f, 604.82f, 152.84f);
        public static readonly PxRect ArmyIcon = new PxRect(0f, 0f, 132.45f, 152.84f);
        public static readonly PxRect HeaderTitle = new PxRect(132.45f, 21.58f, 367.25f, 74.15f);
        public static readonly PxRect HeaderSub = new PxRect(132.45f, 77.01f, 543.13f, 116.06f);
        public static readonly PxRect TimerRect = new PxRect(598.31f, 964.94f, 1321.69f, 1080f);
        public static readonly PxRect TimerClock = new PxRect(938.55f, 995.82f, 991.85f, 1049.12f);
        /// <summary>🆕 **2026-10-13（A516）**：`Timer` 底下的**第一件**（原版 `EverguildTextMeshPro`，
        /// 'Más Recompensas En' = 'More Rewards In'）—— 本件之前我们**只建了后两件**。
        /// 判据 = `工具/menu_rect.py bundle_menus_assets_all "Daily Reward Popup" --depth 4 --relative` 实读
        /// `245.50 990.79 → 935.00 1054.15`（**上下沿与 `TimerText` 逐值相同**，只有左右不同）。</summary>
        public static readonly PxRect TimerMore = new PxRect(245.50f, 990.79f, 935.00f, 1054.15f);
        public static readonly PxRect TimerText = new PxRect(990.20f, 990.79f, 1453.98f, 1054.15f);

        // ---------------- 🆕 **2026-10-26（B9 · P9 #91/#92）**：`Army Selector`（阵营选择条）
        //
        // 🔴 **判据（全部现读，⛔ 不是照截图量的）**：
        //   · 行/视口/内容三层 = `python 工具/menu_dump.py bundle_menus_assets_all "Daily Reward Popup" --depth 3 --relative`：
        //       `Army Selector` 541.2,0 → 1920,152.85（1378.82×152.85 · `ScrollRect`+`ArmySelector`+`CustomRaycaster`）
        //       └ `Viewport`（`RectMask2D`）541.2,0 → 1920,**187.85**（比父件高 34.99 —— 原版就是这样）
        //         └ `Army Content`（HLG+ToggleGroup+CSF）**0 宽 × 130 高** · `spacing = −14` · `align = 4(MiddleCenter)`
        //           · `pad = (0,0,0,5)`（形状 L,R,T,B ⇒ 左 0 右 0 上 0 **下 5**）
        //   · 每格模板 = **`Army Item Button`**（`menu_dump … "Army Item Button" --depth 4 --relative`）：
        //       根 **136.36 × 121.59**（`EverguildToggle` + `ArmyItemContainer` · `onColor (1,1,1,1)` · `offColor (0.75,0.75,0.75,1)`）
        //       ├ `HighlightBG` 0.2,−0.2 → 136.2,121.8（图 pid `6405302285851217683` = `40K_settings_button_selected` ·
        //       │   色 **(1, 0.631, 0.278, 1)** · `Simple` · `ppuMul 0.92`）
        //       ├ `Icon`       11,9 → 126.4,111.6（115.36 × 102.59 · **`preserveAspect`** · 图 = 该阵营徽记）
        //       └ `Arrow`      17,98.9 → 119.4,129.7（原版切片名 `40K_ArmyTrack_chosen faction` —— **选中那一格**底下那个小箭头；⚠️ 落盘/取图用下划线版 `ArtArmyArrow`）
        //   · 谁开它 / 谁写值 = `DailyRewardPopup__Open.c`（`armySelector.Initialize(FeatureConfig.GetValidArmies(),
        //       handler.SelectedArmy, false, false)` + `RefreshOnArmyChange(army)`）· `__SetArmy.c`（`CampaignHandler.SetSelectedArmy`）。
        // ⚠️ **候选列表是我们挑的**：原版按远端 `FeatureConfig` 筛，本地没有那份配置 ⇒ 见 `DailyData.ValidArmies()` 那条 🔶。
        // ⚠️ `Toggle` 的「一组只能选一个」我们用一个普通命中区表达（点中 ⇒ 换阵营 + 重建）——
        //    渲染上等价（`HighlightBG`/`Arrow` 只画在选中那一格），没有引入第二套选中状态。
        public static readonly PxRect ArmySelectorRect = new PxRect(541.2f, 0f, 1920f, 152.85f);
        public static readonly PxRect ArmyViewportRect = new PxRect(541.2f, 0f, 1920f, 187.85f);
        public const float ArmyItemW = 136.36f, ArmyItemH = 121.59f;
        /// <summary>HLG 的 `m_Spacing` = **−14**（相邻两格**故意重叠 14** —— 与奖格轨那条 −64 同族）。</summary>
        public const float ArmyItemGap = -14f;
        /// <summary>`Army Content` 的高（HLG 交叉轴用它 + `align = 4` 居中）。</summary>
        public const float ArmyContentH = 130f;
        /// <summary>HLG 的 `m_Padding.bottom` = **5**（形状 `(L,R,T,B)` ⇒ 只有下边有）。</summary>
        public const float ArmyPadB = 5f;
        /// <summary>格子内三件的**局部**矩形（相对 `Army Item Button` 左上角；判据见上面那段）。</summary>
        public static readonly PxRect AI_Highlight = new PxRect(0.2f, -0.2f, 136.2f, 121.8f);
        /// <summary>见 <see cref="AI_Highlight"/>。</summary>
        public static readonly PxRect AI_Icon = new PxRect(11f, 9f, 126.4f, 111.6f);
        /// <summary>见 <see cref="AI_Highlight"/>。</summary>
        public static readonly PxRect AI_Arrow = new PxRect(17f, 98.9f, 119.4f, 129.7f);
        public const string ArtArmySelected = "40K_settings_button_selected";
        public const string ArtArmyArrow = "40K_ArmyTrack_chosen_faction";   // 落盘名把切片名里的空格换成下划线（导入器 name.replace(' ','_')）；带空格取不到图
        /// <summary>`EverguildToggle.onColor`（选中 ⇒ 徽记满亮）。</summary>
        public static readonly Color ArmyOnTint = new Color(1f, 1f, 1f, 1f);
        /// <summary>`EverguildToggle.offColor`（未选中 ⇒ 徽记压到 0.75 灰）。</summary>
        public static readonly Color ArmyOffTint = new Color(0.75f, 0.75f, 0.75f, 1f);
        /// <summary>`HighlightBG` 的 `Image.m_Color`（**橙** (1, 0.631, 0.278, 1)）。</summary>
        public static readonly Color ArmyHighlightTint = new Color(1f, 0.631f, 0.278f, 1f);

        // ---------------- 抽屉内（Entry 局部坐标，见 `menu_rect.py … "Daily Reward Popup Entry"`）
        // ⚠️ 这些是**相对 Entry 左上角**的绝对矩形；用的时候加到 entry 的 x1/y1 上（`EntryRect`）。
        public static readonly PxRect DayTitle = new PxRect(-4.94f, 0f, 319.97f, 53.49f);
        public static readonly PxRect D_Bg = new PxRect(10.17f, 15.64f, 304.85f, 396.46f);
        public static readonly PxRect D_Highlight = new PxRect(-70.56f, -66.78f, 385.59f, 478.88f);
        public static readonly PxRect D_Holder = new PxRect(14.22f, 36.96f, 300.81f, 330.22f);
        public static readonly PxRect D_Name = new PxRect(14.83f, 330.22f, 300.20f, 382.86f);
        public static readonly PxRect D_PremBanner = new PxRect(3.98f, 12.35f, 130.54f, 137.95f);
        public static readonly PxRect D_PremLock = new PxRect(25.40f, 29.00f, 58.92f, 75.15f);
        public static readonly PxRect D_Claimed = new PxRect(8.22f, 39.25f, 248.50f, 105.88f);
        public static readonly PxRect D_ClaimedTex = new PxRect(14.95f, 52.39f, 231.16f, 92.15f);
        public static readonly PxRect D_Progression = new PxRect(0f, 356.05f, 315.03f, 456.05f);
        public static readonly PxRect D_ProgMark = new PxRect(123.51f, 358.76f, 191.51f, 446.76f);
        public static readonly PxRect D_ProgCount = new PxRect(115.01f, 358.76f, 200.01f, 446.76f);
        /// <summary>🆕 **2026-10-26（B9 · P9 #86）**：`Personal Progression ▸ Progress Bar`（`Slider` +
        /// `MissionProgressBarDisplay`）—— **相对 Entry 左上角**的矩形。
        /// <para>判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Daily Reward Popup Entry"
        /// --depth 3 --relative` 实读：`Progress Bar −64.4 396.7 → 157.5 407.2`（**221.93 × 10.55**）；
        /// 子件 `Background` 与它同框、`Fill` 在作者态是 **0 宽**（value = 0）。
        /// ⚠️ 左沿在 Entry 之外（−64.4）—— 那是原版值，别「修正」回 0。</para></summary>
        public static readonly PxRect D_ProgBar = new PxRect(-64.4f, 396.7f, 157.5f, 407.2f);
        /// <summary>`NormalReward` 那一组的原点（`scl=0.8`，相对 Entry 是 (−6.20,−5.51)）。`Premium Reward` 再 +394.49。</summary>
        public const float DrawerOx = -6.20f, DrawerOy = -5.51f, DrawerPitch = 394.49f;

        // ---------------- 图（正本 §四 / §九）
        public const string ArtBackOpaque = "UI_Deck_Information_submenu_Back_opaque";
        public const string ArtSepLine = "40k_main_line";
        /// <summary>`Highlight` 的图（**按 pid 反查出来的**：`m_Sprite.m_PathID 1969816644568467221`）。</summary>
        public const string ArtHighlight = "OctagonUI_Border_SDF_2";
        public const string ArtTracker = "UI_Login_Tracker";
        public const string ArtFreeIcon = "40K_Profile_icon_title";
        public const string ArtPremIcon = "40k_icon_DailyReward_Premium";
        public const string ArtPremBanner = "WF_Login_CornerBanner";
        public const string ArtLock = "WF_Lock_Icon_Simple";   // ⚠️ `CardArt.MenuUi` **不做空格→下划线转换**，必须传下划线版
        public const string ArtClaimed = "WF_Special_offer_Value";
        public const string ArtBackBtn = "40k_UI_bt_back";
        public const string ArtClock = "WF_icon_clock";
        public const string ArtHeaderBg = "WF_Campaign_Info_Background";
        public const string ArtMilestoneOn = "40k_missions_milestone_on";
        public const string ArtMilestoneOff = "40k_missions_milestone_off";
        /// <summary>🆕 **2026-10-26（B9 · P9 #86）**：`Progress Bar` 的两张图（12×12 · 九宫 4,4,4,4 · `ppuMul 2`）。
        /// 判据 = 原版那一格的 `Image.m_Sprite.m_PathID` 按 pid 反查
        /// （`-4625651602416804825` → `40k_generial_bar_empty` · `-54430120436810690` → `40k_generial_bar_fill`；
        /// 反查口 `grep -rl &lt;pid&gt; d:/2/Warpforge_tools/data/ui_extract/*/Sprite/`）。</summary>
        public const string ArtBarEmpty = "40k_generial_bar_empty";
        /// <summary>见 <see cref="ArtBarEmpty"/>。</summary>
        public const string ArtBarFill = "40k_generial_bar_fill";

        public static readonly Color ClaimedTint = new Color(0f, 0.90f, 0.29f, 0.95f);
        public static readonly Color HighlightTint = new Color(1f, 1f, 1f, 0.62f);

        // 渲染队列：奖励窗最高一层 3014，弹窗从 3018 起（`PromptPopup` 的定义）—— 本窗在两者之间。
        public const int QShade = 3002, QPanel = 3006, QContent = 3010, QText = 3011, QOverlay = 3014;

        public readonly System.Collections.Generic.List<string> MissingArt =
            new System.Collections.Generic.List<string>();

        /// <summary>四个 Entry 的根（自检用）。</summary>
        public readonly System.Collections.Generic.List<Transform> entries = new System.Collections.Generic.List<Transform>();

        /// <summary>压暗层那颗**吸收**命中区（`MenuDraw.Absorb` 建的；自检拿 `MenuDraw.WasAbsorb` 核它）。
        /// ⚠️ 它是**吸收**层、不是「点背景关窗」的 `ShadeHit` —— 判据见 `Build()` 第 1 步那段注释。</summary>
        public Transform ShadeHitNode { get { return transform.Find("AbsorbHit"); } }

        /// <summary>🆕 **2026-10-17（`N46②`）**：`Tracks ▸ Rewards Scroll View` 的滚动区。
        /// 原版那台是 `ScrollRect`（`m_ScrollSensitivity = **1.0**` · `m_MovementType = 1 Elastic` ·
        /// `m_Inertia = 1`）—— 我们原来**根本没建滚动区**（四格按绝对 x 摆死）。
        /// ⚠️ 当前 4 天的内容宽 ≈1068 &lt; 视口 1705 ⇒ 归零后**看不出差别**；但小屏/多天时它是**原版行为**。
        /// 判据 → `资料/普查产出_1017/UI/D_N29_滚动灵敏度逐视图.md` 与 `R_界面线剩余条目现核.md` §`N46②`。</summary>
        MenuScroll _scroll;
        /// <summary>🆕 **2026-10-26（B9 · P9 #91/#92）**：`Army Selector` 那条**横向**滚动区
        /// （原版那台 `ScrollRect`：`h=1 v=0 mode=1 inertia=1`）。⛔ 本窗**只有 `MenuScroll` 一份滚动实现**。
        /// ⚠️ 原版**没登记给指针层的只有连登窗那条**（那条 `m_Enabled = 0`）；这一条 `h=1` ⇒ 玩家**滚得动**，
        /// 所以要 `SocialPage.RegisterScroll`（同下面奖格轨那条）。</summary>
        MenuScroll _armyScroll;

        public static DailyRewardPopup Create(WindowsManager mgr)
        {
            var go = new GameObject("Daily Reward Popup");
            var win = go.AddComponent<DailyRewardPopup>();
            win.type = WindowType.Fullscreen;              // 实证 type=0
            win.placement = WindowsPlacement.Popup;        // 实证 windowsPlacement=15（**0 配 15 是原版的组合**）
            win.closeOnEsc = true;                         // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1f;                // 实证 1.0
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open() { Build(); }

        // ============================================================ 🆕 **2026-10-13（A495）**
        // **「关窗那一拍先收一遍」的三条生产路径**（ESC / 返回钮 / 点窗外）—— 裁定原文与落地形状见下。
        //
        // 🔴 **原版那一下**：`DailyRewardPopup.Close()` = `LiveOp.TryCollect(() => base.Close())`
        //    （判据链逐环实读 → `Shell/DailyData.cs` 的 `DailyRewardAutoCollect` doc：关窗与点奖励
        //    **同一个 `TryCollect`**，续作一个是 `base.Close()`、一个是 `null`）。
        //
        // ⚖️ **调度台裁定（2026-10-13，A495）**：**生产路径（ESC / 返回钮 / 点窗外）都要「收」；
        //    程序性关窗（`WindowsManager.CloseAllWindows()` 那一圈）不收**。⛔ 不许让「我们的断言」
        //    为原版语义让路（铁律 11）—— 所以先把原版那一跳补齐到全部生产路径，再谈夹具。
        //
        // 🔴 **为什么没有照裁定那句「覆写 `Close()` + 另给一个『只关不收』的口」的字面形状落地**（如实标，铁律 3）：
        //    我们的 `Close()` 同时是**生产关窗**与**程序性收口**的唯一出口
        //    （`WindowsManager.CloseAllWindows()` 逐扇调虚方法 `Close()`；
        //      而原版那边程序性关窗走的是 `Hide()`：`Close → manager.CloseWindow → CloseWindowCO` 先调 Slot 9），
        //    要让 `CloseAllWindows()` 改走「只关不收」的那个口 ⇒ **必须改 `Shell/WindowsManager.cs`**
        //    —— **它不在本件白名单**（跨文件撞车面）⇒ 本件改用**同族惯例**：
        //    **每条生产路径自己调 `CloseCollecting()`，`Close()` 保持纯关**
        //    （同族先例 = `Shell/DailyStreakPopup.cs`：返回钮 `CardPresentation/Shell/DailyStreakPopup.cs` 与背景遮罩 `CardPresentation/Shell/DailyStreakPopup.cs` 各自
        //      `StreakAutoCollect(); Close();`，`Close()` 一个字没覆写）。
        //    ⇒ **可观测语义与裁定一致**：ESC / 返回钮 / 点窗外都「先收再关」；`CloseAllWindows()` **不收**。
        //    📌 若日后要把裁定落成字面形状，改动面 = `Shell/WindowsManager.cs` 的
        //    `CloseAllWindows()` 一圈（`as DailyRewardPopup` 走只关不收那条）—— 那一条**不在本件**。
        //
        // ⚠️ **点窗外**：🔴 **2026-10-19（`N117` 顺手）就地订正** —— 本行原来写「本窗**今天没有那颗命中区**…
        //    ⇒ 那一格**无站立点**（将来接上时走 `CloseCollecting()`）」：**前半句已过期**（`Build()` 现在建了
        //    压暗层命中区 `AbsorbHit`），但**后半句的前提也被证伪了** —— 原版 `Daily Reward Popup` 的压暗层
        //    **没有 `BackgroundCloseButton`**、类里也没有接线字段/方法（判据见 `Build()` 第 1 步那段）
        //    ⇒ **原版点这一下什么都不发生**，所以那一颗是**吸收层**（`MenuDraw.Absorb`）、**不关窗、也不收**。
        //    连登窗（`DailyStreakPopup`）的压暗层**有**该组件 ⇒ 它是 `ShadeHit` + `StreakAutoCollect()`，两扇不同，⛔ 别抄成一样。

        /// <summary>**生产路径的关窗** = 原版 `Close()` 里那一跳 `LiveOp.TryCollect(() => base.Close())`
        /// —— **先收再关**。返回钮调它；ESC / 点窗外若将来各接一条，也调它（⛔ 别各写一份
        /// `DailyRewardAutoCollect(); Close();`：两处写同一条规则 = 迟早不一致）。</summary>
        public void CloseCollecting()
        {
            // ⛔ 别在这里另写一份发奖（同一条路 = 同一份守卫 / 记账 / 开窗 —— 见 `CollectReward`）
            DailyData.DailyRewardAutoCollect();
            Close();
        }

        /// <summary>ESC 打在本窗上 = 原版 `GameWindow.ESCPressed()` 两道门槛过 ⇒ 虚表 `Close()`
        /// （= **带 `TryCollect` 的那一份**，与返回钮**同一跳**）⇒ 关这一下也要收。
        /// <para>🔴 **两道门槛照旧由基类判**（`PointerLayer.InputEnabled` / `closeOnEsc`）——
        /// ⛔ 这里**不另抄一份门槛**（「两处写同一条规则 = 迟早不一致」）：先把基类那一跳走完，
        /// 它**真把窗关掉了**才补上那一下收（门槛不过时基类自己出声 + 回 `false` ⇒ 那时**不许收**）。
        /// ⚠️ `CurrentState == Closed` 那道复核是**必须的**：`Close()` 头一句是
        /// 「物体不活 ⇒ 直接 return」（A217①），而 `ESCPressed()` 照旧回 `true`
        /// ⇒ 少了它，一扇**已经灰掉**的窗按 ESC 会**白发一份奖**。</para>
        /// <para>🔴 **如实标注一处次序差（铁律 3）**：原版次序是「**收完再关**」
        /// （`TryCollect(action)` 的续作才是 `base.Close()`），我们这一跳是**关完再收** ——
        /// 因为两条生产路径共用 `Close()` 这一个**程序性口**，把「收」挂进它就会连
        /// `CloseAllWindows()` 一起收（见上面那段）。**可观测差异**只在「那扇领奖窗在底窗回位之后才弹」
        /// 这一拍，终局（窗关掉 + 收进 `Wallet` + 领奖窗在最上面）逐条相同。</para>
        /// <para>**改坏法**：① 删掉本覆写 ⇒ ESC 关窗**不再收**（`Editor/RewardsScene.cs` 的
        /// A495「ESC ⇒ 真收到了」那几条红）；② 把那道 `CurrentState == Closed` 复核删掉 ⇒
        /// 「一扇已经灰掉的窗按 ESC 不许发奖」那条红。</para></summary>
        public override bool ESCPressed()
        {
            bool closed = base.ESCPressed();                 // 门槛 + 关窗都在基类（唯一一份门槛）
            if (closed && CurrentState == WindowState.Closed) DailyData.DailyRewardAutoCollect();
            return closed;
        }

        // ============================================================ 建

        public void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);
            MissingArt.Clear(); entries.Clear();

            // 1) 压暗整屏（无 sprite 的纯色矩形）
            var shadeGo = MenuDraw.Node(root, "Menu Dark Background",
                                        new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f));
            MenuDraw.Rect(shadeGo, CardArt.Solid(), new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f),
                          "Shade", QShade, ShadeColor);
            // 🔴 **2026-10-19（`N117` 顺手）**：压暗层的**吃射线命中区** —— 原来**零命中区**
            //   ⇒ 指针穿过压暗层落到**下面那扇窗**的钮上（本工程的命中候选只收 `WindowButton`，
            //   见 `MenuDraw.Absorb` 的注释），而原版那块 `Image` 是 `m_RaycastTarget = 1` ⇒ 这一下被吃掉。
            //   🔴 **判据（原版 prefab 逐字段实读，2026-10-19）**：
            //     · `Daily Reward Popup/Menu Dark Background` 的组件只有 **`RectTransform` + `CanvasRenderer`
            //       + `Image`**（`MonoBehaviour_6390879965494403023` = `Image`）—— **没有 `BackgroundCloseButton`**
            //       （同族对照：`Daily Streak Popup` 那块压暗层上**有**，所以连登窗「点暗处关窗」是对的）；
            //     · `DailyRewardPopup` 类里也没有 `backgroundCloseButton` 字段（`dump.cs:68853-68868`），
            //       它的 `Start` 只给 `closeButton`（偏移 `0x90`，那颗圆形 X 钮）挂监听。
            //   ⇒ **原版点这一下什么都不发生**（**不关窗**）⇒ 我们按「吃射线」接（`MenuDraw.Absorb`），
            //     ⛔ **不是** `ShadeHit`（那是「点背景关窗」那一族的工具，同族先例 = `Shell/PopUpGameWindow.cs` 的 `ShadeHit`）。
            //   ⚠️ 与 A495 那条裁定的关系：裁定里的「点窗外 ⇒ 要收」以「原版有这条生产路径」为前提；
            //     本窗原版**没有**这一条 ⇒ 这里**不收**（`CloseCollecting()` 只服务 ESC 与返回钮）。
            //   ⚠️ 档由公共件算（`qContentMin − 1` = 3009，严格夹在压暗档 3002 与内容命中区 3010 之间）。
            MenuDraw.Absorb(root, "AbsorbHit", new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f),
                            QShade, QContent);

            // 2) `Tracks`：轨底 + 两条分隔线 + 滚动区
            var tracks = MenuDraw.Node(root, "Tracks", Tracks);
            // 🆕 **2026-10-18（T29 · D4）**：`Tracks/bg` —— 判据本轮已读到（之前那条「渐变参数没读到 ⇒ 不猜」的注释作废）：
            //    原版 `Tracks/bg` = `Image`(**sprite=null** · Simple · `m_Color` 白) + `UIGradient`
            //    （MB `5183763081869680591` 的组件实读：c1 **(0.22352943, 0.01176471, 0.01960784)** ·
            //     c2 **(0.04705883, 0, 0.01568628)** · angle **82** · ignoreRatio 1 · rect → `TrackBg` 242.37,159.34→1918.78,558.89）。
            //    🔴 这组数字与 `Content Area/Background` **逐位相同** ⇒ 直接复用 `MainMenuSubmenuWindow.GradC1/GradC2`；
            //       角度照 §0·2 的换算传 **188**（⛔ 不是 82）。
            //       🔴 **2026-10-25 就地订正（铁律 5 · 代理 D 现核）**：原来这里写「原版方向是 (sin,cos)、我们助手是 (cos,sin) ⇒ 差 90°」——**「差 90°」这个说法不准确**。实际：① `RotationDir(θ)` 返回的是 **`(cosθ, sinθ)`**（`UIGradientUtils__RotationDir.c:9-11`）；② **有效渐变轴**才是 **`(sinθ, cosθ)`**（`UIGradient__ModifyMesh.c:148-149` + `LocalPositionMatrix.c:20-27` 用它做投影）；③ 我们 `CardArt.Gradient` 的最大变暗方向是 `(cos φ, sin φ)` ⇒ 要还原同一张图，换算是 **φ = 90° − θ + 180°（镜像 + 反色序；180° 来自 `c1` 所在端与原版亮端相反）**。⚠️ **数值 188 是对的**（90 − 82 + 180 = 188）—— 这次**只订正理由、不动数值**。
            //    队列 `QPanel − 1`：原版 `Tracks` 子件序 = `bg` → 两条分隔线 → `Rewards Scroll View`
            //    ⇒ bg 必须在分隔线(QPanel)/页内容(QContent)之下，又高于压暗层(QShade)。
            MenuDraw.Rect(tracks, CardArt.Gradient(MainMenuSubmenuWindow.GradC1, MainMenuSubmenuWindow.GradC2, 188f,
                                                  TrackBg.H / TrackBg.W),   // 原版 CompensateAspectRatio 的 h/w
                          TrackBg, "bg", QPanel - 1);
            // 🔴 **2026-10-19（第五十七会话 · `N81`）**：给 `Tracks/bg` 补**吸收层**
            //   （原版那颗 `Image` 是射线靶、父链上没有点击处理器 ⇒ **点它什么都不发生**：不关窗、不播音、不改状态）。
            //   判据 = **本轮亲读 prefab 原件**（`bundle_menus_assets_all`；对象路径 `Daily Reward Popup/Tracks/bg`）：
            //     · `GameObject/bg_5183763081869680591.json`：`m_Name="bg"` · `m_IsActive=1`；
            //       组件 = `RectTransform` + `CanvasRenderer`（`m_CullTransparentMesh=1`）+ **`Image`** + **`UIGradient`**；
            //     · `RectTransform_3779684725085265871.json`：`m_AnchorMin = m_AnchorMax = (0.5,0.5)` ·
            //       `m_Pivot=(0.5,0.5)` · `m_AnchoredPosition=(-8.7007, +199.77)` · `m_SizeDelta=(1676.4014, 399.55)`
            //       ⇒ 绝对矩形 **242.37,159.34 → 1918.78,558.89**（= 现成常量 `TrackBg`）。
            //     · `MonoBehaviour_4004800252594839503.json`（= `Image`）：`m_Sprite = null` · `m_Type=0`(Simple) ·
            //       `m_Color=(1,1,1,1)` · **`m_RaycastTarget = 1`** · `m_RaycastPadding=(0,0,0,0)` · `m_Maskable=1` ·
            //       `m_PreserveAspect=0` · `m_PixelsPerUnitMultiplier=1`。
            //     · `MonoBehaviour_8521710320608567247.json`（= `UIGradient`）：`m_color1=(0.22352943,0.01176471,0.01960784)` ·
            //       `m_color2=(0.04705883,0,0.01568628)` · `m_angle=82` · `m_ignoreRatio=1`（→ 视觉件传 188，换算见上一条）。
            //   🔴 **renderQueue**：原版**没有显式值**（UGUI 由 Canvas 排序）—— 我们的判据是**兄弟序**：
            //     `Tracks.m_Children` = `bg` → `Separator Line Top` → `Separator Line Bottom` → `Rewards Scroll View`
            //     ⇒ 视觉件 `bg` 用 `QPanel − 1`(3005)（上一条）；**吸收件用公共件算出来的档**（`Absorb` 一律取
            //       `qContentMin − 1` = **3009**，严格低于内容命中区 3010、高于压暗层 3002）。
            //   ⚠️ **与 `N117`（本文件 `Build()` 第 1 步那颗整屏 `AbsorbHit`）的关系**：那颗已经把整屏（含这条轨道底）
            //     吃掉了 —— 今天**可观测行为已经正确**；本条补的是**节点级 1:1**（原版这颗 `bg` 自己就是射线靶）。
            //     两者**同族、同档 3009**，点哪一颗都是 `absorbOnly`（四个事件入口 + `Click` 全直接返回 ⇒
            //     零悬停色偏、零按下告警、无按钮音）。⛔ 没有另开第三套吸收机制 —— 走的就是 `MenuDraw.Absorb`。
            //   ⚠️ 兄弟序会多出这一层（原版 4 个孩子 / 我们 5 个）—— 那是「命中层必须是独立 quad」这套实现的代价，
            //     与全工程 30+ 扇窗同族（`ItemDisplayWindow` 三颗 · `PracticeModePopup` 三颗）。
            MenuDraw.Absorb(tracks, "TrackBgAbsorbHit", TrackBg, QShade, QContent);
            MenuDraw.Rect(tracks, Art(ArtSepLine), SepTop, "Separator Line Top", QPanel);
            MenuDraw.Rect(tracks, Art(ArtSepLine), SepBottom, "Separator Line Bottom", QPanel);
            var view = MenuDraw.Node(tracks, "Rewards Scroll View", ScrollView);
            var content = MenuDraw.Node(view, "Rewards Content", new PxRect(ScrollView.x1, 132.09f, ScrollView.x1, 944.19f));
            // 🆕 **2026-10-17（`N46②`）**：接上原版那台滚动区（灵敏度 **1.0** px/格 · Elastic · Inertia）。
            float scrollContentW = EntryPitch * (DailyData.RewardDays - 1) + EntryW;
            var vc = view.gameObject.AddComponent<ViewportClip>();
            vc.padding = Vector4.zero;
            vc.softness = Vector2Int.zero;
            vc.CaptureNow();                    // 同 `AvatarTab`：必须在写矩形之后、建子件之前
            _scroll = MenuScroll.LeftAligned(ScrollView, scrollContentW, 120f);   // 滚轮 120px/格（2026-10-18 主对话定；原版 `Daily Reward Popup > Tracks > Rewards Scroll View` 的 `m_ScrollSensitivity = 1.0`（D_N29）实测一格 1px=纹丝不动；同 `MainMenuRuntime._modeScroll` 的 N66 口径 —— ⛔ 不是原版值，是我们挑的）
            _scroll.Owner = gameObject;
            _scroll.Elastic = true;             // 原版 `m_MovementType = 1`
            _scroll.ClipNode = vc;              // ⚠️ `MenuScroll.ClipNode` 只能宿主显式赋值（`Owner` 反查永远 null）
            var contentBase = content.localPosition;
            _scroll.OnChanged = () =>
                content.localPosition = contentBase + new Vector3(LayoutSpace.Px(-_scroll.Offset), 0f, 0f);
            SocialPage.RegisterScroll(_scroll); // 指针层要认识它，滚轮才落得到这一格上

            // 3) 四个 Entry（`Rewards Content` 的 HLG：spacing **−64**、align=3 MiddleLeft ⇒ 相邻格**故意重叠 64**）
            for (int i = 0; i < DailyData.RewardDays; i++)
            {
                float x1 = ScrollView.x1 + EntryPitch * i;
                // 🔴 四格一律过 `Shift`（与 `CampaignTab` 同一条口径）；`Offset` 归零时逐位不变。
                var r = _scroll != null
                      ? _scroll.Shift(new PxRect(x1, 132.09f, x1 + EntryW, 132.09f + EntryH))
                      : new PxRect(x1, 132.09f, x1 + EntryW, 132.09f + EntryH);
                entries.Add(BuildEntry(content, r, i));
            }

            // 4) 侧栏（双轨 + 关闭圆钮）
            BuildSideBar(root);

            // 4b) 🆕 **2026-10-26（B9 · P9 #91/#92）** 阵营选择条 —— 兄弟序照原版
            //    （`Tracks` → `Tracks Side Bar` → **`Army Selector`** → `Header Header` → `Timer`）。
            BuildArmySelector(root);

            // 5) 顶栏（阵营）与底栏（倒计时）
            BuildHeader(root);
            BuildTimer(root);

            if (MissingArt.Count > 0)
                Debug.LogWarning("[DailyReward] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py`");
        }

        /// <summary>一个 `Daily Reward Popup Entry` = 一天：`Day Title` + 两个抽屉 + `Personal Progression`。</summary>
        Transform BuildEntry(Transform parent, PxRect entry, int day)
        {
            var e = MenuDraw.Node(parent, "Daily Reward Popup Entry (" + day + ")", entry);
            // `Day Title`（`scl=0.8` ⇒ 尺寸与位置都乘 0.8，绕 Entry 左上角）
            var dt = ScaleAbout(DayTitle, 0f, 0f, 0.8f, entry);
            // ⚠️ 实测这条 TMP 是 **`m_HorizontalAlignment = 2 (Center)`**（`Day Title_8118259498043006927` 的 MB）
            // ⇒ **居中**，别左对齐
            var lbDayTitle = MenuDraw.Text(e, dt, "Day " + (day + 1), Color.white, "Day Title", 48f * 0.8f, QText, face: TmpFont.Face.LatinCardBody);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版 `Day Title` = `对齐=Center/**Midline**`（fs48）
            //  （判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Daily Reward Popup" --depth 10 --md`）。
            MenuDraw.SetVAlign(lbDayTitle, Label.VAlign.Midline, dt);

            BuildDrawer(e, entry, day, false);   // `NormalReward`
            BuildDrawer(e, entry, day, true);    // `Premium Reward`

            // `Personal Progression`（进度条那一块）
            var prog = Offset(D_Progression, entry);
            var pg = MenuDraw.Node(e, "Personal Progression", prog);
            // ① `progressStatusImage` = **换图不是显隐**：`normal == Locked ? milestone_off : milestone_on`
            var st = DailyData.RewardStateOf(day, false);
            // 🔴 **2026-09-27 补 `keepAspect`（PA 普查抓的）**：原版 `progressStatusImage` 是 PA=1 + Simple，
            //   贴图 `40k_missions_milestone_off/on` **67×67** 塞进 68×88 的框 ⇒ 原版画 **68×68**，我们拉伸成 68×**88**（高 ×1.29）。
            //   ⚠️ **我们内部本来就不一致**：Missions 页同一件（`MissionsTab.BuildMilestone`）传的是 `true`。
            MenuDraw.Rect(pg, Art(st == RewardState.Locked ? ArtMilestoneOff : ArtMilestoneOn),
                          Offset(D_ProgMark, entry), "Image", QContent, null, true);
            // ② `progressSlider` **只在 `milestone.Index != 0` 时才开** ⇒ 第 0 天没有进度条
            // 🆕 **2026-10-26（B9 · P9 #86）**：这一件**原来根本没建**（注释一直写着「只在 …… 才开」，
            //   而那一件压根不存在 ⇒ 屏上缺每日奖励窗里最容易看见的那条进度条）。
            //   判据（逐条，`DailyRewardItemContainer__Initialize.c:55-78` + prefab 实读）：
            //     · `progressSlider.value = 状态 != Locked ? 1 : 0`（四态里的 Locked 才 0）；
            //     · `wholeNumbers = 1` · `minValue = 0` · `maxValue = 1` · `direction = LeftToRight`；
            //     · 只在 `milestone.Index != 0` 时 `SetActive(true)` ⇒ **第 0 天整件不建**；
            //     · 子件序 = `Progress Bar` → `Image` → `Counter` ⇒ 进度条画在里程碑图**之下**。
            //   ⇒ 队列 `QContent − 2`（底）/ `− 1`（填充）：都低于内容档 3010，又高于 `Personal Progression`
            //     自己那一层（它只是个容器、不画东西）。
            if (day != 0)
            {
                var barBg = Offset(D_ProgBar, entry);
                var barGo = MenuDraw.Node(pg, "Progress Bar", barBg);
                MenuDraw.Nine(barGo, Art(ArtBarEmpty), barBg, new Vector4(4f, 4f, 4f, 4f), 12f, 12f,
                              QContent - 2, new Color(1f, 0.593f, 0f, 1f), true, "Background");
                if (st != RewardState.Locked)
                    MenuDraw.Nine(barGo, Art(ArtBarFill), barBg, new Vector4(4f, 4f, 4f, 4f), 12f, 12f,
                                  QContent - 1, new Color(1f, 0.769f, 0.333f, 1f), true, "Fill");
            }
            MenuDraw.Text(pg, Offset(D_ProgCount, entry), DailyData.RewardDayCounter(day), Color.white,
                          "Counter", 30f, QContent, face: TmpFont.Face.LatinUi);
            return e;
        }

        /// <summary>一个抽屉（`NormalReward` / `Premium Reward`）。`scl=0.8` ⇒ 整套按 0.8 缩。</summary>
        void BuildDrawer(Transform parent, PxRect entry, int day, bool premium)
        {
            bool prem = premium;
            RewardState st = DailyData.RewardStateOf(day, prem);
            float dy = prem ? DrawerPitch : 0f;
            // 抽屉原点：`pos=(−6.20,−5.51)`（相对 Entry）—— 下面每个件都相对**抽屉**算
            var node = MenuDraw.Node(parent, prem ? "Premium Reward" : "NormalReward",
                                     Offset(new PxRect(DrawerOx, DrawerOy + dy, D_Bg.x2, D_Bg.y2 + dy), entry));

            PxRect R(PxRect r) { return ScaleAbout(new PxRect(r.x1, r.y1 + dy, r.x2, r.y2 + dy), 0f, 0f, 0.8f, entry); }

            // `BG`（`UI_Deck_Information_submenu_Back_opaque`）
            MenuDraw.Rect(node, Art(ArtBackOpaque), R(D_Bg), "BG", QPanel);
            // `Highlight`：比格子大一圈的**八边形描边**（`OctagonUI Border SDF 2`，128²、`type=Sliced`）、
            // α=**0.6235** —— 只有 `Unlocked` 才开。
            // 🔴 **2026-09-23 更正**：正本 §四 原来写「无 sprite，α=0.62」—— **有 sprite**。
            //    实据：`Highlight` 的 `Image.m_Sprite.m_PathID = 1969816644568467221`，
            //    按 pid 反查切片名 ⇒ `OctagonUI Border SDF 2`（128×128）。按 pid 反查的办法：
            //    `grep -rl <pid> d:/2/Warpforge_tools/data/ui_extract/*/Sprite/`。
            var hl = MenuDraw.Node(node, "Highlight", R(D_Highlight));
            MenuDraw.Nine(hl, Art(ArtHighlight), R(D_Highlight), new Vector4(0f, 0f, 0f, 0f), 128f, 128f,
                          QPanel, HighlightTint);
            hl.gameObject.SetActive(st == RewardState.Unlocked);
            // `Reward Holder` → 图标
            var holder = MenuDraw.Node(node, "Reward Holder", R(D_Holder));
            var icon = Art(DailyData.RewardIconOf(day, prem));
            var iconQ = MenuDraw.Rect(holder, icon, R(D_Holder), "Reward Holder", QContent, null, true);
            if (iconQ == null && icon == null) MissingArt.Add(DailyData.RewardIconOf(day, prem));
            // 奖励名（`EverguildTextMeshPro` fs=45 × 0.8）
            // 🔴 **2026-10-09（A1126 · A2 档）**：这一处原来**没传 autosize 实参** ⇒ 固定 36px。
            //   判据 = 原版 `menus ▸ Daily Reward Popup/Daily Reward Popup Entry/NormalReward/EverguildTextMeshPro`
            //   （`Premium Reward` 那颗逐值相同）：**`m_enableAutoSizing = 1`** · `m_fontSizeMin = **18**` ·
            //   `m_fontSizeMax = **45**` · `m_fontSizeBase = **36**` · 折行 = 1。
            //   🔴 **四格要按本窗刻度换算**：这一组的节点带 `localScale = 0.8`（见上面 `R()` / `45f * 0.8f`
            //   那两处），而 `m_fontSizeMin/Max/Base` 是**未缩放的设计空间原值** ⇒ **三者一样乘 0.8**
            //   （判据 = 本仓既有先例 `Shell/MissionsTab.cs` 的 `FS()`：**「自适应上下限也要一起乘」**，
            //     `Editor/…` 那条注释在 `CardPresentation/Editor/RewardsScene.cs` / `CardPresentation/Editor/RewardsScene.cs` / `CardPresentation/Editor/RewardsScene.cs`）。⇒ 18/45/36 → **14.4 / 36 / 28.8**。
            var nm = MenuDraw.Text(node, R(D_Name), DailyData.RewardName(day, prem), Color.white,
                                   "Name", 45f * 0.8f, QText, R(D_Name).W, 18f * 0.8f, 45f * 0.8f, 36f * 0.8f, face: TmpFont.Face.LatinCardBody);
            if (nm != null) nm.AlignLeftOn(LayoutSpace.FromPixel(R(D_Name).x1, 0f).x);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版 `EverguildTextMeshPro`（奖励名，fs45）
            //   = `对齐=Center/**Midline**`（同一份 dump，8 份实例逐值相同）。
            MenuDraw.SetVAlign(nm, Label.VAlign.Midline, R(D_Name));

            // `Premium Indicator`（角旗 + 锁）—— **只有 PremiumLocked 开**
            var pi = MenuDraw.Node(node, "Premium Indicator", R(D_PremBanner));
            MenuDraw.Rect(pi, Art(ArtPremBanner), R(D_PremBanner), "Banner", QContent);
            // PA=1（`WF Lock Icon Simple` 42×63 塞 33.52×46.15 ⇒ 原版 30.77×46.15，我们拉伸 33.5 宽，差 ~9%）
            MenuDraw.Rect(pi, Art(ArtLock), R(D_PremLock), "Lock", QOverlay, null, true);
            pi.gameObject.SetActive(st == RewardState.PremiumLocked);

            // `Gacha Reward Claimed`（`WF_Special offer_Value` + 'Claimed'）—— **只有 Collected 开**
            var gc = MenuDraw.Node(node, "Gacha Reward Claimed", R(D_Claimed));
            MenuDraw.Rect(gc, Art(ArtClaimed), R(D_Claimed), "Claimed", QContent, ClaimedTint);
            // 🔴 **2026-10-13（A493 #7）**：`Claimed Tex` 补**显式左对齐**。
            //   判据 = 原版 `Daily Reward Popup/…/Daily Reward Popup Entry/NormalReward/Gacha Reward Claimed/Claimed Tex`：
            //   **`对齐=Left/Midline`** —— `python 工具/menu_dump.py bundle_menus_assets_all "Daily Reward Popup"
            //   --depth 12 --no-sprite` 实读，**8 份实例逐值相同**（`255.1 244.2 428.1 276.0` / `255.1 638.7 428.1 670.5` …）。
            //   ⚠️ 不显式对齐 ⇒ `Label` 默认把文字块**居中**摆在框心（原版贴左）。
            //   本文件已有同款先例（`本文件` 的 `Name` 也是这个口径）。
            // 🔴 **2026-10-09（A1126 · A2 档）**：这一处原来**没传 autosize 实参** ⇒ 固定 33.56px。
            //   判据 = 原版 `…/NormalReward/Gacha Reward Claimed/Claimed Tex`：**`m_enableAutoSizing = 1`** ·
            //   `m_fontSizeMin = **15**` · `m_fontSizeMax = **200**` · `m_fontSizeBase = **36**` · 折行 = 1。
            //   🔴 同上一颗：这一组带 `localScale = 0.8` ⇒ 四格一起 × 0.8 → **12 / 160 / 28.8**
            //   （判据 = `Shell/MissionsTab.cs` 的 `FS()`：「自适应上下限也要一起乘」）。
            var claimedTx = MenuDraw.Text(gc, R(D_ClaimedTex), DailyData.RewardClaimedText(), Color.white, "Claimed Tex", 41.95f * 0.8f, QText,
                                          R(D_ClaimedTex).W, 15f * 0.8f, 200f * 0.8f, 36f * 0.8f, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(claimedTx, R(D_ClaimedTex));
            MenuDraw.SetVAlign(claimedTx, Label.VAlign.Midline, R(D_ClaimedTex));   // A712 阶段 2：原版 `Left/Midline` 的纵向那一半
            gc.gameObject.SetActive(st == RewardState.Collected);

            // `colider`（**真的点击区**：同 BG 的图、α=0.00 + 按钮）—— **只有 Unlocked 开且可点**
            var col = MenuDraw.Node(node, "colider", R(D_Bg));
            var cq = MenuDraw.Rect(col, Art(ArtBackOpaque), R(D_Bg), "Hit", QOverlay,
                                   new Color(1f, 1f, 1f, 0f));
            col.gameObject.SetActive(st == RewardState.Unlocked);
            if (cq != null)
            {
                var hit = cq.gameObject.AddComponent<WindowButton>();
                int d = day; bool pm = prem;
                // 🆕 **2026-10-11（批次1 · W1 · A309）**：这一下现在**会弹原版那扇 `Reward Window`** ——
                //   开窗在 `DailyData.CollectReward` 里面（= 我们唯一的发奖口那一处，见那边的「领奖窗」一段）：
                //   `RewardService.Collect` 在原版就是「发完奖 → 开一扇 Collect 态的全屏领奖窗」。
                // 🔴 **2026-10-12（A479/A480）如实标注（铁律 3/5·c）**：原版**这一下也走 `TryCollect`**
                //   —— `DailyRewardSelector__CollectRewardClicked.c` 调的是选择器 `+0x58` 那个
                //   `Action<ChallengeMilestone> OnCollect`（`dump.cs` 的 `DailyRewardSelector` 字段表），
                //   而 `DailyRewardPopup.Start()` 把它接到 `<Start>b__6_0`，后者末句 =
                //   `LiveOp.TryCollect(null)`（**续作是 null**、而且**根本没用那个 `milestone` 形参**）。
                //   ⇒ 原版点哪一格都一样：收的是 `CurrentChallenges` 里**第一条 `canCollect`**
                //   （**不止本窗那一格**；列表序本地查不到 ⇒ 我们按**点的那一格**收 = 我们的落地，
                //   与关窗那一拍（`DailyData.DailyRewardAutoCollect`）同口径、同一份守卫）。
                hit.onClick = () => DailyData.CollectReward(d, pm);
            }
        }

        void BuildSideBar(Transform root)
        {
            var bar = MenuDraw.Node(root, "Tracks Side Bar", SideBar);
            // 🔴 **2026-09-27 补 `keepAspect`（PA 普查抓的）** —— 左轨那三件原版全是 **PA=1 + Simple**：
            //   · `BG`（`UI_Login_Tracker` 264×828 塞 290.67×824.73）⇒ 原版 **262.9×824.73**（左右各留 13.9），我们 290.67 宽（差 10.6%）
            //   · `Free Track/Icon`（`40K_Profile_icon_title` **134×88**）⇒ 原版 **178.52×117.24**，我们 250 宽（**1.40×**）
            //   · `Premium Track/Icon`（`40k_icon_DailyReward_Premium` **102×102**）⇒ 原版 **117.24×117.24**，我们 250 宽（**2.13×**）
            //   ⇒ 与「顶栏那面盾」是**同一类错**（把 PA 当拉伸画）。
            MenuDraw.Rect(bar, Art(ArtTracker), SideBarBg, "BG", QPanel, null, true);
            // Free Track
            var f = MenuDraw.Node(bar, "Free Track", FreeTrack);
            MenuDraw.Rect(f, Art(ArtFreeIcon), FreeIcon, "Icon", QContent, null, true);
            MenuDraw.Text(f, FreeTitle, DailyData.FreeTrackTitle(), Color.white, "Title", 36f, QText, face: TmpFont.Face.LatinCardBody);
            // Premium Track
            var p = MenuDraw.Node(bar, "Premium Track", PremTrack);
            MenuDraw.Rect(p, Art(ArtPremIcon), PremIcon, "Icon", QContent, null, true);
            MenuDraw.Text(p, PremTitle, DailyData.PremiumTrackTitle(), Color.white, "Title", 36f, QText, face: TmpFont.Face.LatinCardBody);
            var price = MenuDraw.Node(p, "Price Display Button 2 Variant", PremPrice);
            var pb = MenuDraw.Rect(price, Art("UI_Button_Mulligan"), PremPrice, "Generic UI Button", QContent);
            // 🔴 **2026-10-09（A1126 · A2 档）**：这一处原来**没传 autosize 实参** ⇒ 固定 30px。
            //   判据 = 原版 `menus ▸ …/Premium Track/Price Display Button 2 Variant/Generic UI Button/Price Display/text`
            //   （出厂文本 `'300,00'`）：**`m_enableAutoSizing = 1`** · `m_fontSizeMin = **13.46**` ·
            //   `m_fontSizeMax = **40**` · `m_fontSizeBase = **39**` · **折行 = 0**。
            //   ⚠️ **这一格不在 `localScale = 0.8` 的那棵子树里**（`PremPrice` 没走 `R()`，与 Header 同刻度）
            //   ⇒ 四格**照原值抄、不乘 0.8**（判据 = 同族的 `Shell/PurchasePremiumWindow.cs` 四格常量就是原值）。
            //   ⚠️ 折行 0 ⇒ `SetAutoFitBox` 内部 `SetWrapWidth` 会**无条件**开成 `Normal` ⇒ 紧跟一句关掉。
            // 🆕 **2026-10-17（界面线 `T07` · `A1753` #10）**：屏上颜色 = 原版材质 `Pragati-Regular Light Cream`
            //   的 `_FaceColor` **(0.992, 0.868, 0.733)**（顶点白）。判据 = `R_T07_剩余四项.md` §4·2 #10。
            var lbPrice = MenuDraw.Text(price, PremPrice, DailyData.PremiumTrackPrice(), new Color(0.992f, 0.868f, 0.733f, 1f), "Price", 30f, QText,
                                        PremPrice.W, 13.46f, 40f, 39f, face: TmpFont.Face.LatinCardBody);
            if (lbPrice != null) lbPrice.SetWrapping(false);        // 原版折行=0（A205：关这一下顺带推版面）
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版那一格 `…/Price Display Button 2 Variant/
            //   Generic UI Button/…/text`（出厂 `'300,00'`）= `对齐=Center/**Capline**`（同一份 dump）。
            //   ⚠️ 上述这条只判**纵向档**；⚠️ 顺带读到：原版那一颗 `字号=33.5`、我们传 `30f` —— 字号那一笔
            //   不属本账（未改），如实登记在报告里。
            MenuDraw.SetVAlign(lbPrice, Label.VAlign.Capline, PremPrice);
            if (pb != null)
            {
                var hit = pb.gameObject.AddComponent<WindowButton>();
                // ⚠️ 单机口径（边界②）：**不卖 Premium**，点了如实说明（红线：不许静默失败）
                hit.onClick = () => Debug.Log("[DailyReward] 「买 Premium 轨」单机版**没有实现** —— " +
                                              "原版走 `PremiumOffer.TryPurchaseOffer`（真商店），" +
                                              "本项目的边界②是「不做真实经济」⇒ 点了如实提示，不假装成功");
                // 🆕 A17：原版 `Tracks Side Bar>BG>Premium Track>Price Display Button 2 Variant>Generic UI Button`
                // 是 SpriteSwap（普查 §块 4 第 7 行）
                hit.BindSelf("UI_Button_Mulligan");
            }
            // 左下关闭圆钮
            var close = MenuDraw.Node(bar, "Generic Round Button Variant", CloseBtn);
            var cq = MenuDraw.Rect(close, Art(ArtBackBtn), CloseBtn, "Image", QOverlay);
            if (cq != null)
            {
                var hit = cq.gameObject.AddComponent<WindowButton>();
                // 🆕 **2026-10-12（A479/A480）**：关窗**先收一遍再关** —— 原版
                // `DailyRewardPopup.Close()` = `LiveOp.TryCollect(() => base.Close())`
                // （判据链逐环 → `Shell/DailyData.cs` 的 `DailyRewardAutoCollect` 注释）。
                // 🔴 **两处如实标注（铁律 3/5·c）**：
                //   ① 原版那一下收的是 `CurrentChallenges` 里的**第一条 `canCollect`**（**不止本窗那一格**），
                //      而那份**列表序本地查不到** ⇒ 我们按**本窗那一格**收（= 我们的落地，调度台已裁、维持）；
                //   ② 原版这扇窗**没有**「开窗自动收」—— `<Start>b__6_0` 那个 `TryCollect(null)` 是
                //      **选择器上那颗奖励的点按处理器**（`DailyRewardSelector.OnCollect`），不是开窗那一拍
                //      （H29 §七·1 记的「`<Start>` 一处」是它的**形状**、不是时机；本件就地核过）。
                hit.onClick = () => CloseCollecting();
                // 🔴 **2026-10-13（A495）：这一段原来写「留给调度台裁」，裁定已下 —— 就地写回（铁律 5）。**
                //   **裁定**：生产路径（ESC / 返回钮 / 点窗外）**都要「收」**；**程序性关窗（`CloseAllWindows()`）不收**
                //   （铁律 11：⛔ 不让「我们的断言」为原版语义让路 ⇒ 先把原版那一跳补齐到全部生产路径，再谈夹具）。
                //   **落地形状**（为什么不照裁定那句「覆写 `Close()` + 另给一个只关不收的口」的字面形状）→
                //   见本文件 `CloseCollecting()` 上面那一整段：那个形状**必须改 `Shell/WindowsManager.cs`**，
                //   而它不在本件白名单 ⇒ 改走**同族惯例**（每条生产路径自己调 `CloseCollecting()`，
                //   `Close()` 保持纯关 —— `Shell/DailyStreakPopup.cs` 里那句 `StreakAutoCollect(); Close();` / `CardPresentation/Shell/DailyStreakPopup.cs` 就是这个形状）。
                //   ⛔ **仍然不许把那两句搬进 `Close()` 覆写**：原版那边程序性关窗走的是 `Hide()`
                //   （`Close → manager.CloseWindow → CloseWindowCO` 先调 Slot 9，见 `Shell/WindowsManager.cs` 的
                //   `Hide()` 注释），而**我们的 `CloseAllWindows()` 是逐扇调虚方法 `Close()`** ⇒ 覆写它会把
                //   程序性收口也算进「关窗自动收」：既不是原版，又会当场打破既有夹具
                //   （`Editor/RewardsScene.cs` §四 末尾那句 `wm2.CloseAllWindows()` 在 §九 **之前** ⇒
                //    §九「（前提）找得到一个可领的抽屉」那条当场红 —— 2026-10-13 落 A495 时逐步核过这条链）。
                //   ✅ **今天三条生产路径的状态**：返回钮 = 本行（调 `CloseCollecting()`）；
                //   ESC = 本类的 `ESCPressed()` 覆写（同一份收口，已接）；
                //   **点窗外 = 🔴 2026-10-19（`N117` 顺手）就地订正**：原来写「本窗还没有那颗命中区 ⇒ 无站立点」——
                //   现在有了，但它是**吸收层**（原版那块压暗层没有 `BackgroundCloseButton`、类里也不接线
                //   ⇒ 点了**什么都不发生**，判据见 `Build()` 第 1 步）⇒ **`CloseCollecting()` 只服务 ESC 与返回钮**。
                // 🆕 A17：这一颗的高亮图**不是** `<常态图>_hover` —— 原版实测是
                // `40k_UI_bt_back_hover_back`（普查 §块 4 第 8 行）⇒ 逐颗显式覆盖。
                hit.BindSelf(ArtBackBtn, "40k_UI_bt_back_hover_back");
            }
        }

        void BuildHeader(Transform root)
        {
            var h = MenuDraw.Node(root, "Header Header", Header);
            MenuDraw.Rect(h, Art(ArtHeaderBg), Header, "Background", QPanel);
            MenuDraw.Rect(h, Art(DailyData.HeaderArmyIcon()), ArmyIcon, "Army Icon", QContent, null, true);
            // 🔴 **2026-10-09（A1126 · A2 档）**：这一处原来**没传 autosize 实参** ⇒ 固定 40px。
            //   判据 = 原版 `menus ▸ Daily Reward Popup/…/Header Header/Title`：**`m_enableAutoSizing = 1`** ·
            //   `m_fontSizeMin = **18**` · `m_fontSizeMax = **40**` · `m_fontSizeBase = **36**` · 折行 = 1。
            //   ⚠️ Header 这一族**不带** `localScale`（我们传的 `40f` 就是原版 `m_fontSize` 原值）
            //   ⇒ 四格照抄、**不乘 0.8**（⛔ 与上面 `Name` / `Claimed Tex` 那两颗不是一套刻度）。
            var t = MenuDraw.Text(h, HeaderTitle, DailyData.HeaderArmyName(), Color.white, "Title", 40f, QText,
                                  HeaderTitle.W, 18f, 40f, 36f, face: TmpFont.Face.LatinCardBody);
            if (t != null) t.AlignLeftOn(LayoutSpace.FromPixel(HeaderTitle.x1, 0f).x);
            // 🆕 **2026-10-16（A712 阶段 2）**：原版顶栏 `Title`（fs40）= `Left/**Midline**`（同一份 dump）。
            MenuDraw.SetVAlign(t, Label.VAlign.Midline, HeaderTitle);
            // 同 `Header Header/Title` 那一族的刻度（不带 `localScale`）：原版 `…/Header Header/Sub-Title` =
            // `auto[18~40] · base 36 · 折行 1`（四格照抄，⛔ 不乘 0.8）。
            var s = MenuDraw.Text(h, HeaderSub, DailyData.HeaderArmySubTitle(), Color.white, "Sub-Title", 39.7f, QText,
                                  HeaderSub.W, 18f, 40f, 36f, face: TmpFont.Face.LatinCardBody);
            if (s != null) s.AlignLeftOn(LayoutSpace.FromPixel(HeaderSub.x1, 0f).x);
            // 🆕 **2026-10-16（A712 阶段 2）**：原版 `Sub-Title`（fs39.7）= `Left/**Capline**`
            //   —— ⚠️ **与上一行的 `Title`（`Midline`）同框不同档**，⛔ 别一刀切。
            MenuDraw.SetVAlign(s, Label.VAlign.Capline, HeaderSub);
        }

        void BuildTimer(Transform root)
        {
            var t = MenuDraw.Node(root, "Timer", TimerRect);
            // 🔴 **2026-10-13（A516）**：原版 `Timer` 底下是**三件**，我们**只建了两件** ⇒ 补上缺的第一件
            //   `EverguildTextMeshPro`（**节点名照原版**，同一个 `Timer` 底下还有一颗 `(1)`）。
            //   判据（三方，逐条对得上）：
            //     ① 本件亲跑 `python 工具/menu_dump.py bundle_menus_assets_all "Daily Reward Popup" --depth 12`
            //        实读 `245.5 990.8 → 935.0 1054.1 689.50×63.36 · 'Más Recompensas En' · 字号=50.0
            //        · 基准=50.0 · 对齐=Right/Capline · 折行=1 · 色=(1,1,1,1)`；
            //     ② `资料/说明书/04_界面UI/菜单全树.md:1092`
            //        （`EverguildTextMeshPro [245,991 690x63] text:'Más Recompensas En',script script`）；
            //     ③ 正本 `资料/日常_原版规格.md:456`：「`Timer` `pos=(0,−482.47) sz=(723.387,115.06)`
            //        → TMP **fs=50** + **`WF_icon_clock`**(53.292²) + 时间 **fs=50**」= **三件**。
            //   ⚠️ **别一刀切**：这一颗是 **`Right/Capline`**，而下面那颗 `(1)`（A493 #8 改的）是
            //      **`Left/Capline`** —— 同一个 `Timer` 底下**方向相反**（与连登窗 `Timer` 底下那对同形）。
            //   ⚠️ 文案走 `DailyData`（原版这两窗是**同一条本地化词条**；奖励窗 prefab 那份落到了 es 语系
            //      `'Más Recompensas En'`）。⛔ 别在这里再写一份字面量（两处写同一条规则 = 迟早不一致）。
            //   🔴 **2026-10-13（A643）**：口名从 `StreakNextRewardsText()`（名字带 `Streak`）换成**中性名**
            //      `MoreRewardsInText()` —— 原来只有连登窗那条口，本窗只能复用一个「看着像连登窗专用」的名字
            //      （那个会误导下一个会话去找另一份）。⛔ 新调用点一律用中性那个。
            var more = MenuDraw.Text(t, TimerMore, DailyData.MoreRewardsInText(), Color.white,
                                     "EverguildTextMeshPro", 50f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignRight(more, TimerMore);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版 `Right/**Capline**`（上面 ① 那条 dump 逐字写着）。
            MenuDraw.SetVAlign(more, Label.VAlign.Capline, TimerMore);
            MenuDraw.Rect(t, Art(ArtClock), TimerClock, "Image", QContent);
            // 🔴 **2026-10-13（A493 #8）**：倒计时那颗补**显式左对齐**。
            //   判据 = 原版 `Daily Reward Popup/Timer/EverguildTextMeshPro (1)`（节点名照原版）：
            //   **`对齐=Left/Capline`**、矩形 `990.2 990.8 1454.0 1054.1`、`字号=50`
            //   （`menu_dump.py … "Daily Reward Popup" --depth 12 --no-sprite` 实读，与我们的 `TimerText` 逐值相同）
            //   ⇒ 只差对齐这一笔。
            //   ⚠️ **别一刀切**：同一个 `Timer` 底下**另一颗** `EverguildTextMeshPro`（'More Rewards In'）原版是
            //   **`Right/Capline`**（`245.5 990.8 935.0 1054.1`）—— 🔴 **那一颗 2026-10-13（A516）已经补上了**
            //   （就在本方法**上方**，画在时钟图之前 = 原版的兄弟序），本条注释原来写「我们根本没建、
            //   不在本账里顺手补」**已过期**（铁律 5 就地订正）。
            var tm = MenuDraw.Text(t, TimerText, DailyData.RewardTimerText(), Color.white, "EverguildTextMeshPro (1)", 50f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(tm, TimerText);
            MenuDraw.SetVAlign(tm, Label.VAlign.Capline, TimerText);   // A712 阶段 2：原版 `Left/Capline` 的纵向那一半
            // 🔴 **2026-10-17（N3）**：这一颗原来只有**建窗那一刻**的活值（`DailyData.RewardTimerText()` 一次性
            //   算好的字符串），**没有每秒 tick**（判据/缺口见 `资料/普查产出_1017/UI/W_A_TimerDisplay.md` §4·5）。
            //   这里挂上全工程唯一的倒计时口径 `TimerDisplay` ⇒ 每秒重算一次。
            //   目标 = 我们自建的**日界** `DailyData.NextResetAt`（本机时间；原版这颗由 `TimerDisplay` 运行时填，
            //   我们没有服务器 —— 见 `DailyData.ResetIn` 那条注释）。前缀与 `RewardTimerText()` 同一格（`Resets in `）。
            TimerDisplay.Attach(tm, () => DailyData.NextResetAt, Loc.T("Missions/CollectCounter"), "每日奖励窗 Timer", null);
        }

        // ============================================================ 🆕 2026-10-26（B9 · P9 #91/#92）
        // `Army Selector`（阵营选择条）：一排 13 个阵营徽记，**点一格 = 换阵营** ⇒ 顶栏阵营名/图标改写
        // + 整扇窗重建（= 原版 `SetArmy` → `CampaignHandler.SetSelectedArmy` → `RefreshOnArmyChange`
        // → `rewardsTrack.RefreshData(mission, refreshAll:true)`）。几何/图/色全部现读，判据见上面常量块。
        void BuildArmySelector(Transform root)
        {
            var sel = MenuDraw.Node(root, "Army Selector", ArmySelectorRect);
            // `Viewport` = 原版挂 `RectMask2D` 的那一层（比父件高 34.99 —— 原版值，⛔ 别「修正」）
            var vpNode = MenuDraw.Node(sel, "Viewport", ArmyViewportRect);
            var vc = vpNode.gameObject.AddComponent<ViewportClip>();
            vc.padding = Vector4.zero;
            vc.softness = Vector2Int.zero;
            vc.CaptureNow();                       // 同奖格轨：必须在写矩形之后、建子件之前
            var content = MenuDraw.Node(vpNode, "Army Content",
                                        new PxRect(ArmyViewportRect.x1, 0f, ArmyViewportRect.x1, ArmyContentH));
            var ids = DailyData.ValidArmies();
            float contentW = ids.Length * ArmyItemW + (ids.Length - 1) * ArmyItemGap;
            _armyScroll = MenuScroll.LeftAligned(ArmyViewportRect, contentW, 120f);
            _armyScroll.Owner = gameObject;
            _armyScroll.ClipNode = vc;             // ⚠️ `ClipNode` 只能宿主显式赋值（`Owner` 反查永远 null）
            var contentBase = content.localPosition;
            _armyScroll.OnChanged = () =>
                content.localPosition = contentBase + new Vector3(LayoutSpace.Px(-_armyScroll.Offset), 0f, 0f);
            SocialPage.RegisterScroll(_armyScroll);

            // 交叉轴（uGUI `HorizontalLayoutGroup` + `align = 4 MiddleCenter`）：
            //   可用高 = 内容高 − pad.bottom(5) = 125；格高 121.59 ⇒ 上下各留 (125 − 121.59)/2 = 1.705
            float innerH = ArmyContentH - ArmyPadB;
            float itemTop = (innerH - ArmyItemH) * 0.5f;
            float x = ArmyViewportRect.x1;          // pad.L = 0（HLG 的 `m_Padding` 左/右都是 0）
            for (int i = 0; i < ids.Length; i++)
            {
                var r = new PxRect(x, itemTop, x + ArmyItemW, itemTop + ArmyItemH);
                if (_armyScroll != null) r = _armyScroll.Shift(r);
                BuildArmyCell(content, r, ids[i]);
                x += ArmyItemW + ArmyItemGap;
            }
        }

        /// <summary>一格阵营徽记（模板 = `Army Item Button`）：选中 ⇒ 橙底 + 小箭头 + 徽记满亮；
        /// 未选中 ⇒ 只有压到 0.75 灰的徽记。点它 = `DailyData.SetHeaderArmy`（真换了才重建）。</summary>
        void BuildArmyCell(Transform parent, PxRect r, int army)
        {
            bool on = DailyData.HeaderArmyValue == army;
            var cell = MenuDraw.Node(parent, "Army Item Button (" + army + ")", r);
            // 🔴 **队列必须低于顶栏底板**：原版 `Daily Reward Popup.m_Children` 的兄弟序是
            //   `Tracks` → `Tracks Side Bar` → **`Army Selector`** → **`Header Header`** → `Timer`
            //   ⇒ **顶栏画在阵营条之上**（两件在 x 541.2…604.82 那一段故意重叠 63.6px）。
            //   UGUI 的「后面的兄弟盖前面的」在我们这边只能靠渲染队列表达 ⇒ 阵营条三件取 `QPanel − 1`(3005)，
            //   顶栏底板是 `QPanel`(3006) ⇒ 那 63.6px 由顶栏盖住（= 原版的观感）。⛔ 别取 `QContent`。
            //   ⚠️ 命中区仍留在 `QOverlay`(3014)：本窗整屏那颗 `AbsorbHit` 在 3009，命中区必须**高于**它。
            if (on)
                MenuDraw.Rect(cell, Art(ArtArmySelected), Offset(AI_Highlight, r), "HighlightBG", QPanel - 1,
                              ArmyHighlightTint);
            MenuDraw.Rect(cell, Art(ArmyIcons.IconName(army)), Offset(AI_Icon, r), "Icon", QPanel - 1,
                          on ? ArmyOnTint : ArmyOffTint, true);
            if (on)
                MenuDraw.Rect(cell, Art(ArtArmyArrow), Offset(AI_Arrow, r), "Arrow", QPanel - 1,
                              ArmyOnTint, true);
            // 命中区：同 `colider` 那一族（α=0 的图 + `WindowButton`），整格可点。
            var cq = MenuDraw.Rect(cell, Art(ArtBackOpaque), r, "Hit", QOverlay, new Color(1f, 1f, 1f, 0f));
            if (cq == null) return;
            var hit = cq.gameObject.AddComponent<WindowButton>();
            int a = army;
            // 「真换了才重建」—— 原版 `ChangeSelectedArmy` 对同阵营是**早返回**（`SetHeaderArmy` 里那道）。
            hit.onClick = () => { if (DailyData.SetHeaderArmy(a)) Build(); };
        }

        // ============================================================ 工具

        static PxRect Offset(PxRect r, PxRect by) { return new PxRect(by.x1 + r.x1, by.y1 + r.y1, by.x1 + r.x2, by.y1 + r.y2); }

        /// <summary>原版 `scl=0.8` 的几何效果：绕 (ox,oy) 缩放（`ox/oy` 取 Entry 的左上角，即父的 pivot）。</summary>
        static PxRect ScaleAbout(PxRect r, float ox, float oy, float s, PxRect parent)
        {
            float px = parent.x1 + ox, py = parent.y1 + oy;
            return new PxRect(px + (parent.x1 + r.x1 - px) * s, py + (parent.y1 + r.y1 - py) * s,
                              px + (parent.x1 + r.x2 - px) * s, py + (parent.y1 + r.y2 - py) * s);
        }

        Texture2D Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name)) MissingArt.Add(name);
            return t;
        }

        public string Dump()
        {
            return $"DailyReward：{entries.Count} 个 Entry · 取不到的图 {MissingArt.Count} 张";
        }
    }
}
