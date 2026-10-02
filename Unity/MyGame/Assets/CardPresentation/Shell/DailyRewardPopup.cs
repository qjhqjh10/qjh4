// DailyRewardPopup.cs — 「日常」第 2 层：每日奖励窗（原版 `Daily Reward Popup`）
//
// ============================ 出处（唯一正本） ============================
// `资料/日常_原版规格.md` §四（层 × 参数表）· `资料/日常_调用链_DailyRewardPopup.md`（调用链 + 三态）。
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
// （`DF:DailyRewardDrawerController__SetState.c:8-28`），它按这张表开关五个件（出处：`日常_调用链_DailyRewardPopup.md` §D）：
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
            MenuDraw.Text(e, dt, "Day " + (day + 1), Color.white, "Day Title", 48f * 0.8f, QText);

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
            var nm = MenuDraw.Text(node, R(D_Name), DailyData.RewardName(day, prem), Color.white,
                                   "Name", 45f * 0.8f, QText);
            if (nm != null) nm.AlignLeftOn(LayoutSpace.FromPixel(R(D_Name).x1, 0f).x);

            // `Premium Indicator`（角旗 + 锁）—— **只有 PremiumLocked 开**
            var pi = MenuDraw.Node(node, "Premium Indicator", R(D_PremBanner));
            MenuDraw.Rect(pi, Art(ArtPremBanner), R(D_PremBanner), "Banner", QContent);
            // PA=1（`WF Lock Icon Simple` 42×63 塞 33.52×46.15 ⇒ 原版 30.77×46.15，我们拉伸 33.5 宽，差 ~9%）
            MenuDraw.Rect(pi, Art(ArtLock), R(D_PremLock), "Lock", QOverlay, null, true);
            pi.gameObject.SetActive(st == RewardState.PremiumLocked);

            // `Gacha Reward Claimed`（`WF_Special offer_Value` + 'Claimed'）—— **只有 Collected 开**
            var gc = MenuDraw.Node(node, "Gacha Reward Claimed", R(D_Claimed));
            MenuDraw.Rect(gc, Art(ArtClaimed), R(D_Claimed), "Claimed", QContent, ClaimedTint);
            MenuDraw.Text(gc, R(D_ClaimedTex), DailyData.RewardClaimedText(), Color.white, "Claimed Tex", 41.95f * 0.8f, QText);
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
            MenuDraw.Text(price, PremPrice, DailyData.PremiumTrackPrice(), Color.white, "Price", 30f, QText);
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
                hit.onClick = () => Close();
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
            var t = MenuDraw.Text(h, HeaderTitle, DailyData.HeaderArmyName(), Color.white, "Title", 40f, QText);
            if (t != null) t.AlignLeftOn(LayoutSpace.FromPixel(HeaderTitle.x1, 0f).x);
            var s = MenuDraw.Text(h, HeaderSub, DailyData.HeaderArmySubTitle(), Color.white, "Sub-Title", 39.7f, QText);
            if (s != null) s.AlignLeftOn(LayoutSpace.FromPixel(HeaderSub.x1, 0f).x);
        }

        void BuildTimer(Transform root)
        {
            var t = MenuDraw.Node(root, "Timer", TimerRect);
            MenuDraw.Rect(t, Art(ArtClock), TimerClock, "Image", QContent);
            MenuDraw.Text(t, TimerText, DailyData.RewardTimerText(), Color.white, "EverguildTextMeshPro (1)", 50f, QText);
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
