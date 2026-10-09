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
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.77f);

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

        public static readonly Color ClaimedTint = new Color(0f, 0.90f, 0.29f, 0.95f);
        public static readonly Color HighlightTint = new Color(1f, 1f, 1f, 0.62f);

        // 渲染队列：奖励窗最高一层 3014，弹窗从 3018 起（`PromptPopup` 的定义）—— 本窗在两者之间。
        public const int QShade = 3002, QPanel = 3006, QContent = 3010, QText = 3011, QOverlay = 3014;

        public readonly System.Collections.Generic.List<string> MissingArt =
            new System.Collections.Generic.List<string>();

        /// <summary>四个 Entry 的根（自检用）。</summary>
        public readonly System.Collections.Generic.List<Transform> entries = new System.Collections.Generic.List<Transform>();

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
        //    （同族先例 = `Shell/DailyStreakPopup.cs`：返回钮 `:536` 与背景遮罩 `:270-271` 各自
        //      `StreakAutoCollect(); Close();`，`Close()` 一个字没覆写）。
        //    ⇒ **可观测语义与裁定一致**：ESC / 返回钮 / 点窗外都「先收再关」；`CloseAllWindows()` **不收**。
        //    📌 若日后要把裁定落成字面形状，改动面 = `Shell/WindowsManager.cs` 的
        //    `CloseAllWindows()` 一圈（`as DailyRewardPopup` 走只关不收那条）—— 那一条**不在本件**。
        //
        // ⚠️ **点窗外**：本窗**今天没有那颗命中区**（`Build()` 只画了压暗层、没有 `MenuDraw.ShadeHit`；
        //    连登窗有，见 `Shell/DailyStreakPopup.cs` 里那颗 `ShadeHit`）⇒ 那一格**无站立点**（本条只保证：将来接上时
        //    走 `CloseCollecting()`）。这条缺口另记进报告，⛔ 本件不顺手加（不是 A495 的账）。

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

            // 2) `Tracks`：轨底 + 两条分隔线 + 滚动区
            var tracks = MenuDraw.Node(root, "Tracks", Tracks);
            // ⚠️ `bg` 是「无 sprite + `UIGradient`」——**渐变参数没读到**（正本没记），
            //    所以这里**不猜**：只画 `40k_main_line` 那两条分隔线，`bg` 留空并在自检里报出来。
            MenuDraw.Rect(tracks, Art(ArtSepLine), SepTop, "Separator Line Top", QPanel);
            MenuDraw.Rect(tracks, Art(ArtSepLine), SepBottom, "Separator Line Bottom", QPanel);
            var view = MenuDraw.Node(tracks, "Rewards Scroll View", ScrollView);
            var content = MenuDraw.Node(view, "Rewards Content", new PxRect(ScrollView.x1, 132.09f, ScrollView.x1, 944.19f));

            // 3) 四个 Entry（`Rewards Content` 的 HLG：spacing **−64**、align=3 MiddleLeft ⇒ 相邻格**故意重叠 64**）
            for (int i = 0; i < DailyData.RewardDays; i++)
            {
                float x1 = ScrollView.x1 + EntryPitch * i;
                var r = new PxRect(x1, 132.09f, x1 + EntryW, 132.09f + EntryH);
                entries.Add(BuildEntry(content, r, i));
            }

            // 4) 侧栏（双轨 + 关闭圆钮）
            BuildSideBar(root);

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
            var lbDayTitle = MenuDraw.Text(e, dt, "Day " + (day + 1), Color.white, "Day Title", 48f * 0.8f, QText);
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
            MenuDraw.Text(pg, Offset(D_ProgCount, entry), DailyData.RewardDayCounter(day), Color.white,
                          "Counter", 30f, QContent);
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
            //     `Editor/…` 那条注释在 `:345` / `:380` / `:1302`）。⇒ 18/45/36 → **14.4 / 36 / 28.8**。
            var nm = MenuDraw.Text(node, R(D_Name), DailyData.RewardName(day, prem), Color.white,
                                   "Name", 45f * 0.8f, QText, R(D_Name).W, 18f * 0.8f, 45f * 0.8f, 36f * 0.8f);
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
            //   本文件已有同款先例（`:252` 的 `Name` 也是这个口径）。
            // 🔴 **2026-10-09（A1126 · A2 档）**：这一处原来**没传 autosize 实参** ⇒ 固定 33.56px。
            //   判据 = 原版 `…/NormalReward/Gacha Reward Claimed/Claimed Tex`：**`m_enableAutoSizing = 1`** ·
            //   `m_fontSizeMin = **15**` · `m_fontSizeMax = **200**` · `m_fontSizeBase = **36**` · 折行 = 1。
            //   🔴 同上一颗：这一组带 `localScale = 0.8` ⇒ 四格一起 × 0.8 → **12 / 160 / 28.8**
            //   （判据 = `Shell/MissionsTab.cs` 的 `FS()`：「自适应上下限也要一起乘」）。
            var claimedTx = MenuDraw.Text(gc, R(D_ClaimedTex), DailyData.RewardClaimedText(), Color.white, "Claimed Tex", 41.95f * 0.8f, QText,
                                          R(D_ClaimedTex).W, 15f * 0.8f, 200f * 0.8f, 36f * 0.8f);
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
            MenuDraw.Text(f, FreeTitle, DailyData.FreeTrackTitle(), Color.white, "Title", 36f, QText);
            // Premium Track
            var p = MenuDraw.Node(bar, "Premium Track", PremTrack);
            MenuDraw.Rect(p, Art(ArtPremIcon), PremIcon, "Icon", QContent, null, true);
            MenuDraw.Text(p, PremTitle, DailyData.PremiumTrackTitle(), Color.white, "Title", 36f, QText);
            var price = MenuDraw.Node(p, "Price Display Button 2 Variant", PremPrice);
            var pb = MenuDraw.Rect(price, Art("UI_Button_Mulligan"), PremPrice, "Generic UI Button", QContent);
            // 🔴 **2026-10-09（A1126 · A2 档）**：这一处原来**没传 autosize 实参** ⇒ 固定 30px。
            //   判据 = 原版 `menus ▸ …/Premium Track/Price Display Button 2 Variant/Generic UI Button/Price Display/text`
            //   （出厂文本 `'300,00'`）：**`m_enableAutoSizing = 1`** · `m_fontSizeMin = **13.46**` ·
            //   `m_fontSizeMax = **40**` · `m_fontSizeBase = **39**` · **折行 = 0**。
            //   ⚠️ **这一格不在 `localScale = 0.8` 的那棵子树里**（`PremPrice` 没走 `R()`，与 Header 同刻度）
            //   ⇒ 四格**照原值抄、不乘 0.8**（判据 = 同族的 `Shell/PurchasePremiumWindow.cs` 四格常量就是原值）。
            //   ⚠️ 折行 0 ⇒ `SetAutoFitBox` 内部 `SetWrapWidth` 会**无条件**开成 `Normal` ⇒ 紧跟一句关掉。
            var lbPrice = MenuDraw.Text(price, PremPrice, DailyData.PremiumTrackPrice(), Color.white, "Price", 30f, QText,
                                        PremPrice.W, 13.46f, 40f, 39f);
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
                //   `Close()` 保持纯关 —— `Shell/DailyStreakPopup.cs` 里那句 `StreakAutoCollect(); Close();` / `:270-271` 就是这个形状）。
                //   ⛔ **仍然不许把那两句搬进 `Close()` 覆写**：原版那边程序性关窗走的是 `Hide()`
                //   （`Close → manager.CloseWindow → CloseWindowCO` 先调 Slot 9，见 `Shell/WindowsManager.cs` 的
                //   `Hide()` 注释），而**我们的 `CloseAllWindows()` 是逐扇调虚方法 `Close()`** ⇒ 覆写它会把
                //   程序性收口也算进「关窗自动收」：既不是原版，又会当场打破既有夹具
                //   （`Editor/RewardsScene.cs` §四 末尾那句 `wm2.CloseAllWindows()` 在 §九 **之前** ⇒
                //    §九「（前提）找得到一个可领的抽屉」那条当场红 —— 2026-10-13 落 A495 时逐步核过这条链）。
                //   ✅ **今天三条生产路径的状态**：返回钮 = 本行（调 `CloseCollecting()`）；
                //   ESC = 本类的 `ESCPressed()` 覆写（同一份收口，已接）；
                //   **点窗外 = 本窗还没有那颗命中区**（`Build()` 只画压暗层，连登窗才有 `ShadeHit`）⇒ 无站立点，
                //   缺口已记进本件报告（⛔ 不在 A495 的账里顺手加）。
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
                                  HeaderTitle.W, 18f, 40f, 36f);
            if (t != null) t.AlignLeftOn(LayoutSpace.FromPixel(HeaderTitle.x1, 0f).x);
            // 🆕 **2026-10-16（A712 阶段 2）**：原版顶栏 `Title`（fs40）= `Left/**Midline**`（同一份 dump）。
            MenuDraw.SetVAlign(t, Label.VAlign.Midline, HeaderTitle);
            // 同 `Header Header/Title` 那一族的刻度（不带 `localScale`）：原版 `…/Header Header/Sub-Title` =
            // `auto[18~40] · base 36 · 折行 1`（四格照抄，⛔ 不乘 0.8）。
            var s = MenuDraw.Text(h, HeaderSub, DailyData.HeaderArmySubTitle(), Color.white, "Sub-Title", 39.7f, QText,
                                  HeaderSub.W, 18f, 40f, 36f);
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
                                     "EverguildTextMeshPro", 50f, QText);
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
            var tm = MenuDraw.Text(t, TimerText, DailyData.RewardTimerText(), Color.white, "EverguildTextMeshPro (1)", 50f, QText);
            MenuDraw.AlignLeft(tm, TimerText);
            MenuDraw.SetVAlign(tm, Label.VAlign.Capline, TimerText);   // A712 阶段 2：原版 `Left/Capline` 的纵向那一半
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
