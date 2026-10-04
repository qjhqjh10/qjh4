// DailyStreakPopup.cs — 「日常」第 2 层：每日连登窗（原版 `Daily Streak Popup`）
//
// ============================ 出处（唯一正本） ============================
// `资料/日常_原版规格.md` §五（层 × 参数表）· `资料/日常_调用链_DailyStreak.md`（调用链 + 两态）。
// **类名照原版**：`DailyStreakWindow : LiveOpsEventWindow<MainMenuMission>`（MB `8654310213240890027`）。
// prefab 根 pid `2821652005965210283`。
//
// **窗口参数（实证）**：`type=0(Fullscreen)` · `windowsPlacement=15(Popup)` · `closeOnESC=0` ·
// `extraScaleSmallScreen=1.0` · **`scaleMultiplierFirstElement = 1.2`**。
//
// **矩形出处**：`工具/menu_rect.py bundle_menus_assets_all "Daily Streak Popup" --depth 5 --relative`
// 与 `… "Daily Streak Reward Popup Entry" --depth 3 --relative --root-size 379.816x516.301` **机械走链**算出。
//
// ---- 🔴 两态（**靠面板互斥，不是两套资产**）----
// 驱动字段是 **任务对象的** `MainMenuMission.HasFailed`@0x30 / `FailedValue`@0x34（不是窗口自己的），
// 唯一写入点 = `Open()`（四步顺序与出厂的 T/F 都对上）。**出厂亮着的是「断了」那一态**
// （`Streak Failed` active=true · `Streak Successful` active=false）—— 别照着建还以为默认是连胜。
// ⚠️ **`Timer` 在 `Streak Successful` 面板里** ⇒ **断签态下倒计时看不见**（原版实况）。
//
// ---- 🔴 关窗与返回 ----
// 返回钮与背景遮罩**走同一个 `CloseButtonClicked` → `Close()`（vtable Slot 8）**，
// 而 `Close()` = `LiveOp.TryCollect(() => base.Close())` ⇒ **关窗会先自动收取**（原版的「收完再关」）。
//
// ---- 🔴 奖格轨的视口 / 裁切 / 滚动（**2026-10-08 · A182 建的**）----
// 原版 `Streak Successful/Rewards Scroll View` 底下是三层：`ScrollRect`(**m_Enabled=0 · h=0 v=0**)
// → **`Viewport`(RectMask2D, `m_Softness=(89,0)` `m_Padding=(0,0,0,0)`) → `Rewards Content`(HLG)**，
// 三层**同矩形** 0,159.33 → 1920,964.94（`menu_dump.py … "Daily Streak Popup" --depth 5` 实读；
// 软边/pad 逐处表 = `d:/4/_tmp_view/q1_rm2d.txt:87-88`）。**本件之前这三层一层都没建**
// （连 `Clip` 都一处没有 ⇒ 第 6、7 格照画到屏幕外）。现在按上面那几个字段建全，见 `BuildTrack`。
//
// ---- ⚠️ 我们挑的 / 没做的 ----
//   · **数据全是我们自建**（`DailyData`）—— 原版走 PlayFab 云脚本 #907（`CloudHandle.UpdateMissionPoints`）；
//   · **`bg` 用 `UIGradient` 画**：实测那对颜色是 **#390503 → #0C0004 · angle 82**，与奖励窗 `Content Area` 的
//     完全一致 ⇒ 复用 `RewardsWindow.GradC1/GradC2`（不是猜的，是读出来的）；
//   · **`Streak Failed` 面板**：出厂就亮，但我们单机口径下默认 `HasFailed=false`（见 `DailyData`）⇒
//     默认看到的是 `Streak Successful`。面板本身照建，驱动是数据。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`Daily Streak Popup` —— 连登窗。单机口径：主菜单进场时按「本地有没有可领」决定弹不弹。</summary>
    public class DailyStreakPopup : GameWindow
    {
        // ---------------- 窗口根（正本 §五 表 #1~#4）
        public static readonly PxRect Shade = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        public static readonly PxRect Bg = new PxRect(0f, 152.84f, 1920f, 964.94f);
        public static readonly PxRect SepTop = new PxRect(-5.03f, 152.84f, 1925.03f, 167.74f);
        public static readonly PxRect SepBottom = new PxRect(-5.03f, 950.04f, 1925.03f, 964.94f);

        // ---------------- 断签态（`Streak Failed`，出厂 active=true）
        public static readonly PxRect FailedAnchor = new PxRect(910f, 490f, 1010f, 590f);
        public static readonly PxRect F_Broken = new PxRect(47.43f, 338.55f, 1872.57f, 475.45f);
        public static readonly PxRect F_Lost = new PxRect(47.43f, 465.35f, 1872.57f, 552.45f);
        public static readonly PxRect F_Info = new PxRect(47.43f, 583f, 1872.57f, 633f);
        public static readonly PxRect F_ResetBtn = new PxRect(727.72f, 656.5f, 1192.28f, 759.5f);
        public static readonly PxRect F_ResetText = new PxRect(744.02f, 666.55f, 1175.23f, 749.36f);

        // ---------------- 连胜态（`Streak Successful`，出厂 active=false）
        public static readonly PxRect SuccPanel = new PxRect(0f, 152.84f, 1920f, 964.94f);
        public static readonly PxRect S_FillLine = new PxRect(134.43f, 525.70f, 1622.40f, 592.07f);
        public static readonly PxRect S_CurLabel = new PxRect(43f, 212.42f, 533.91f, 295.07f);
        public static readonly PxRect S_CurValue = new PxRect(545.91f, 212.42f, 583.76f, 295.07f);
        public static readonly PxRect S_Scroll = new PxRect(0f, 159.33f, 1920f, 964.94f);
        public static readonly PxRect S_Info = new PxRect(47.47f, 830f, 1872.53f, 880f);
        public static readonly PxRect S_Timer = new PxRect(598.31f, 964.94f, 1321.69f, 1080f);
        public static readonly PxRect S_TimerNext = new PxRect(573.31f, 997.47f, 935f, 1047.47f);
        public static readonly PxRect S_TimerClock = new PxRect(940f, 1002.47f, 980f, 1042.47f);
        public static readonly PxRect S_TimerText = new PxRect(985f, 997.47f, 1346.69f, 1047.47f);

        // ---------------- 顶栏（`Header With Back Button`）
        public static readonly PxRect HeaderRoot = new PxRect(0f, 21.65f, 550f, 131.20f);
        public static readonly PxRect H_Title = new PxRect(155f, 38f, 534.30f, 120.66f);
        public static readonly PxRect H_Bg = new PxRect(-462.10f, 21.65f, 87.90f, 137.01f);
        public static readonly PxRect H_Back = new PxRect(-24.40f, 23.67f, 143.48f, 134.99f);

        // ---------------- 奖格（Entry 局部坐标，见 `menu_rect.py … "Daily Streak Reward Popup Entry"`）
        public const float EntryW = 379.816f, EntryH = 516.301f;
        /// <summary>`Rewards Content` 的 HLG：**spacing = −64**（相邻两格**故意重叠 64**）、align=3(MiddleLeft)。</summary>
        public const float EntrySpacing = -64f;
        public static float EntryPitch { get { return EntryW + EntrySpacing; } }

        /// <summary>`Rewards Content` 那个 `HorizontalLayoutGroup` 的 **pad.L = 2**（原文
        /// `pad=2,0,58,0`）⇒ **第一格从内容左沿 +2 起排**（内容 = 左对齐容器，它的左沿 = 视口左沿）。
        /// ⚠️ 另外两格 `pad`（T 58 / B 0）是 **y 向**的，属 `Rewards Content` 的**纵向对齐**，
        /// 与本题（视口裁切/滚动）无关 ⇒ 本件**不动纵向摆位**（见报告「顺手发现的」）。</summary>
        public const float ContentPadL = 2f;
        /// <summary>`Rewards Content` 跑完 `ContentSizeFitter` 之后的宽 —— 原版 = pad.L 2 + 7 格 ×379.816
        /// + 6 个 spacing(−64) = **2276.71**（`menu_dump` 那一行给的是**出厂 0 子件**时的 `MinSize=2`）。
        /// **视口只有 1920** ⇒ 可滚范围 = 2276.71 − 1920 = **356.71px**。</summary>
        public static float TrackContentW { get { return ContentPadL + EntryPitch * (DailyData.StreakDays - 1) + EntryW; } }

        // ---------------- 🔴 奖格轨的视口（`Streak Successful/Rewards Scroll View/Viewport`）
        // 原版那一层是 **`RectMask2D`**（组件不在 `MonoBehaviour/` 里按名字查得到 —— 它是 Unity 内置件，
        // 判据 = 解包 `RectMask2D` 实例的 `m_Softness` / `m_Padding`；逐处表 → `d:/4/_tmp_view/q1_rm2d.txt`）：
        //   · `soft=(89,0) pad=(0.0,0.0,0.0,0.0) en=1`（`q1_rm2d.txt:87-88`，
        //     路径 `Daily Streak Popup/Streak Successful/Rewards Scroll View/Viewport`）
        //   · 结构 `Rewards Scroll View`(ScrollRect → `Viewport`(RectMask2D) → `Rewards Content`(HLG)
        //     —— `menu_dump.py bundle_menus_assets_all "Daily Streak Popup" --depth 5` 实读。
        /// <summary>原版 `RectMask2D.m_Softness`（画布像素 · x 管左右 · y 管上下）= **(89,0)**。
        /// ⚠️ **不是 `(0,89)`** —— 这条轨是**横向**滚的，渐隐带在**左右**两条边上（写反了会横切）。</summary>
        public static readonly Vector2 TrackSoft = new Vector2(89f, 0f);
        /// <summary>原版 `RectMask2D.m_Padding` = **(0,0,0,0)**（形状 = UGUI 的 `(L,B,R,T)`；正 = 缩小）。</summary>
        public static readonly Vector4 TrackPad = Vector4.zero;
        public static readonly PxRect E_Bg = new PxRect(14.56f, 26.95f, 365.25f, 480.15f);
        public static readonly PxRect E_Highlight = new PxRect(-69.29f, -59.49f, 449.10f, 566.59f);
        public static readonly PxRect E_Holder = new PxRect(20.21f, 52.72f, 359.79f, 400.58f);
        public static readonly PxRect E_Name = new PxRect(20.82f, 416.66f, 358.99f, 448.96f);
        public static readonly PxRect E_Extra = new PxRect(242.41f, 59.85f, 342.41f, 159.85f);
        public static readonly PxRect E_Collect = new PxRect(9.89f, 63.09f, 283.53f, 145.81f);
        public static readonly PxRect E_CollectText = new PxRect(17.02f, 79.79f, 266.76f, 129.86f);

        // ---------------- 图
        public const string ArtSepLine = "40k_main_line";
        public const string ArtBackOpaque = "UI_Deck_Information_submenu_Back_opaque";
        /// <summary>`Highlight` 的图 —— 与每日奖励窗同一个（**按 pid 反查**：`m_Sprite.m_PathID 1969816644568467221`）。</summary>
        public const string ArtHighlight = "OctagonUI_Border_SDF_2";
        public const string ArtExtra = "40k_main_bt_rewards";
        public const string ArtClaim = "WF_Special_offer_Value";
        public const string ArtFillBar = "40k_generial_bar_fill";
        public const string ArtClock = "WF_icon_clock";
        public const string ArtHeaderBg = "WF_Campaign_Info_Background";
        public const string ArtBackBtn = "UI_Button_Menu_Back";
        public const string ArtMulligan = "UI_Button_Mulligan";

        public static readonly Color HighlightTint = new Color(1f, 1f, 1f, 0.624f);
        public static readonly Color ClaimTint = new Color(0.06f, 0.57f, 0.13f, 1f);
        public static readonly Color FillLineTint = new Color(0.43f, 0f, 0.06f, 0.62f);

        public const int QShade = 3002, QPanel = 3006, QContent = 3010, QText = 3011, QOverlay = 3014;

        public readonly System.Collections.Generic.List<string> MissingArt =
            new System.Collections.Generic.List<string>();
        public readonly System.Collections.Generic.List<Transform> entries = new System.Collections.Generic.List<Transform>();

        /// <summary>奖格轨的滚动区（**全壳唯一一份滚动实现** = `MenuScroll`）。
        /// 自检直调 `ScrollBy`/`SetOffset`（批处理里没有滚轮事件 ⇒ 和真滚同一条路）。
        /// 🔴 **本窗【不】登记给 `PointerLayer`** —— 判据 = 原版那颗 `ScrollRect` 的实读字段：
        /// `h=0 v=0 mode=1 inertia=1 …` **且 `m_Enabled = 0`**（`menu_dump.py … "Daily Streak Popup"`
        /// 的 `Rewards Scroll View` 那一行）⇒ **玩家滚不动它**；位移只有一条路
        /// = `DailyRewardSelector.Initialize/AdjustView` 末尾的 `FocusOnItem(scrollRect, items[day−3])`
        /// （`d:/2/tools/decomp_full/DailyRewardSelector__Initialize.c` / `__AdjustView.c`，两处同源）。
        /// ⚠️ 那条 `FocusOnItem` 只在 **`5 < 当前天`** 时走（同上两文件里 `if (5 &lt; …)`）⇒ 我们这台
        /// `DailyData.StreakCollected` 是常量 **5** ⇒ **当前数据下恒不移动**（照原版如实做，不额外发明）。</summary>
        MenuScroll _trackScroll;
        /// <summary>自检用（量可滚范围 / 直调 `ScrollBy`）。</summary>
        public MenuScroll TrackScroll { get { return _trackScroll; } }
        /// <summary>奖格轨的内容容器（`Rewards Content`）—— 滚动偏移一变就重建它。</summary>
        Transform _trackContent;

        public bool HasFailed { get; private set; }
        public float PanelH { get { return EntryH; } }

        /// <summary>🆕 **2026-10-05（A81）**：压暗层的**命中区**节点（「点窗外关窗」）—— 自检用
        /// （`MenuDraw.ShadeRuleOk(darkHit, qShade, qContentMin, out why)` 的 `darkHit`）。</summary>
        public Transform ShadeHit { get { return transform.Find("BackgroundHit"); } }

        public static DailyStreakPopup Create(WindowsManager mgr)
        {
            var go = new GameObject("Daily Streak Popup");
            var win = go.AddComponent<DailyStreakPopup>();
            win.type = WindowType.Fullscreen;             // 实证 type=0
            win.placement = WindowsPlacement.Popup;       // 实证 windowsPlacement=15
            win.closeOnEsc = false;                       // 实证 closeOnESC=0
            win.extraScaleSmallScreen = 1f;               // 实证 1.0
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open() { Build(); }

        public void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);
            MissingArt.Clear(); entries.Clear();
            HasFailed = DailyData.StreakFailed();

            // 1) 压暗整屏
            MenuDraw.Rect(root, CardArt.Solid(), Shade, "Menu Dark Background", QShade,
                          new Color(0f, 0f, 0f, 0.77f));
            // 🔴 **2026-10-05（A81）**：压暗层的**点击区**（「点窗外关窗」）—— **原来零 `ShadeHit(`**。
            //   判据：原版是压在 `Menu Dark Background` **自身节点**上的 `BackgroundCloseButton`；
            //   本窗的接线字段叫 **`backgroundButton`**（`DailyStreakWindow__Start.c`，偏移 `0x98`）——
            //   ⚠️ 名字各窗不同（全工程 35 处），**别按名字 grep 一次就下结论**。
            //   🔴 **它挂的处理函数与顶栏「返回」钮是【同一个】** —— 实据：
            //     `DailyStreakWindow__Start.c` 里 `backgroundButton.onClick` 用的是 **`DAT_1842b3520`**，
            //     而 `DailyStreakWindow__Open.c` 里 `header.Initialize(…, delegate(DAT_1842b3520))` 用的是
            //     **同一个方法常量**（`resetStreakButton` 那颗用的是 `DAT_1842b3820` = 另一个方法，别混）。
            //     ⇒ 这里的动作**照抄 `BuildHeader` 那颗返回钮**（`StreakAutoCollect` + `Close`，
            //     原版 `Close()` 覆写里就是 `TryCollect(() => base.Close())`），⛔ 不是裸 `Close()`。
            //   档 = **压暗层自己那一档 `QShade`(3002)**，**严格低于**本窗内容命中区档 `QContent`(3010)
            //   （内容命中区 = `Header Back Button` / `Reset Streak` / 奖格的 `Collect`，三颗都在 `QContent`）。
            //   出处 → `资料/待办判据_阶段二与联机.md` §A81 · 公共件规矩 → `MenuDraw.ShadeHit` 的注释。
            MenuDraw.ShadeHit(root, Shade, QShade, QContent,
                              () => { DailyData.StreakAutoCollect(); Close(); }, "BackgroundHit");
            // 2) `bg`：**无 sprite + `UIGradient`** —— 实测那对颜色与奖励窗 `Content Area` **完全一致**
            //    （`m_color1 #390503 / m_color2 #0C0004 / m_angle 82`）⇒ 直接复用那两个常量。
            MenuDraw.Rect(root, CardArt.Gradient(RewardsWindow.GradC1, RewardsWindow.GradC2, 82f), Bg, "bg", QPanel);
            // 🆕 **2026-10-06（A94）：`bg` 那块整幅面板底图吸收点击**。判据 = 原版 prefab
            //   `Daily Streak Popup > bg` 那颗 `Image` 的 **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读）
            //   —— 射线打到它自己、父链上没有点击处理器（关窗那颗 `BackgroundCloseButton` 在压暗层上）
            //   ⇒ 原版点这里**什么都不做**。
            //   ⚠️ 两个面板（`Streak Successful` / `Streak Failed`）**没有自己的底图**（它们只是容器）
            //   ⇒ 吸收层就这一块（盖得住两边）。
            MenuDraw.Absorb(root, "AbsorbHit", Bg, QShade, QContent);
            // 3) 两条分隔线
            MenuDraw.Rect(root, Art(ArtSepLine), SepTop, "Separator Line Top", QPanel);
            MenuDraw.Rect(root, Art(ArtSepLine), SepBottom, "Separator Line Bottom", QPanel);

            // 4) 两个面板 —— **互斥**（`HasFailed` 决定谁开）
            BuildSuccessful(root);
            BuildFailed(root);

            // 5) 顶栏
            BuildHeader(root);

            if (MissingArt.Count > 0)
                Debug.LogWarning("[DailyStreak] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        /// <summary>连胜面板：`Fill Line` + `Current Streak` + 奖格轨 + `Info` + `Timer`。</summary>
        void BuildSuccessful(Transform root)
        {
            var p = MenuDraw.Node(root, "Streak Successful", SuccPanel);
            MenuDraw.Rect(p, Art(ArtFillBar), S_FillLine, "Fill Line", QPanel, FillLineTint);
            MenuDraw.Text(p, S_CurLabel, DailyData.StreakCurrentLabel(), Color.white, "Current streak:", 70f, QText);
            MenuDraw.Text(p, S_CurValue, DailyData.StreakCurrentValue(), Color.white, "Current Streak Value", 80f, QText);

            var view = MenuDraw.Node(p, "Rewards Scroll View", S_Scroll);
            // 🔴 **2026-10-08（A182）：`Viewport` 这一层原来是缺的** —— 原版结构是
            //   `Rewards Scroll View`(ScrollRect) → **`Viewport`(RectMask2D)** → `Rewards Content`(HLG)
            //   （`menu_dump.py bundle_menus_assets_all "Daily Streak Popup" --depth 5` 实读；
            //    三层 **同矩形** 0,159.33 → 1920,964.94）。带掩码的那一层（也是 `Clip` 的落点）就是它。
            var vp = MenuDraw.Node(view, "Viewport", S_Scroll);
            _trackContent = MenuDraw.Node(vp, "Rewards Content",
                                          new PxRect(S_Scroll.x1, 132.09f, S_Scroll.x1, 944.19f));
            // 滚动区：**左对齐内容**（原版 `Rewards Content` 贴视口左边 + `ContentSizeFitter`）
            // ⇒ 范围 `[0, 内容右端 − 视口右端]` = `[0, 356.71]`（由 `MenuScroll` 自己算）。
            // 🔴 `OnChanged` 指向**幂等**的重建（先清后建）—— 正是 `资料/阶段二_滚动与指针_原版规格.md`
            //    §四 第 1、2 条那两个坑（越建越多 / 相对位移翻倍）的规矩。
            _trackScroll = MenuScroll.LeftAligned(S_Scroll, TrackContentW);
            _trackScroll.Owner = gameObject;      // ⚠️ C# 的对象初始化器**只能跟在 `new` 后面** —— 别写在方法调用后面
            _trackScroll.OnChanged = BuildTrack;
            BuildTrack();
            FocusCurrentDay();          // 原版 `DailyRewardSelector` 那一段（当前数据下恒不移动，见字段注释）
            MenuDraw.Text(p, S_Info, DailyData.StreakInfoText(), Color.white, "Info", 36f, QText);
            // ⚠️ `Timer` **在本面板里** ⇒ 断签态下看不到倒计时（原版实况）
            var t = MenuDraw.Node(p, "Timer", S_Timer);
            MenuDraw.Text(t, S_TimerNext, DailyData.StreakNextRewardsText(), Color.white, "Next Rewards text", 36f, QText);
            MenuDraw.Rect(t, Art(ArtClock), S_TimerClock, "Image", QContent);
            MenuDraw.Text(t, S_TimerText, DailyData.StreakTimerText(), Color.white, "Timer Text", 36f, QText);
            p.gameObject.SetActive(!HasFailed);
        }

        /// <summary>建奖格轨的 7 格（**幂等：先清后建** —— 滚动偏移一变 `OnChanged` 就会重入这里）。
        /// 🔴 三件事全在这一段里成对拿捏（原版都长在 `Viewport` 那颗 `RectMask2D` 上）：
        ///   · **裁切** `Clip` = 视口矩形（`S_Scroll`，与 `Viewport` 同矩形）；
        ///   · **软边** `ClipSoftness` = **(89,0)**；**padding** `ClipPad` = **(0,0,0,0)**。
        /// ⚠️ **格节点照样建满 7 个**（原版 `DailyRewardSelector.CreateRewards` 也是实例化全部 7 格、
        ///   被掩码裁掉的只是**像素**）⇒ `entries.Count` 恒 = `DailyData.StreakDays`；
        ///   整块落在视口外的那些**子件**（图标/角标…）由 `ClipRect` 各自「不建」。</summary>
        void BuildTrack()
        {
            if (_trackContent == null) return;
            for (int i = _trackContent.childCount - 1; i >= 0; i--)
                RewardsWindow.DestroySafe(_trackContent.GetChild(i).gameObject);
            entries.Clear();

            var prevClip = Clip;
            var prevSoft = ClipSoftness;
            var prevPad = ClipPad;
            Clip = S_Scroll;                 // = 原版 `Viewport` 的矩形
            ClipSoftness = TrackSoft;
            ClipPad = TrackPad;
            int n = DailyData.StreakDays;
            int first = DailyData.StreakCollected;          // `scaleMultiplierFirstElement` 只作用在**这一格**
            for (int i = 0; i < n; i++)
            {
                // **内容坐标**：从 `Rewards Content` 左沿 + HLG 的 `pad.L`(2) 起排，再整体 `Shift` 到屏幕。
                float x1 = ContentPadL + EntryPitch * i;
                var r = new PxRect(x1, 132.09f, x1 + EntryW, 132.09f + EntryH);
                // 原版 `DailyStreakWindow.scaleMultiplierFirstElement = 1.2` —— 唯一读取点 = `RefreshRewards` 的
                // 第一次循环（`i == challenge.collectedRewards`）⇒ **本次第一个「还没领」的奖格**放大 1.2。
                // 实测该 prefab 根的 `m_Pivot = (.5,.5)` ⇒ 绕**中心**放大。
                if (i == first) r = ScaleAbout(r, 1.2f);
                if (_trackScroll != null) r = _trackScroll.Shift(r);
                entries.Add(BuildEntry(_trackContent, r, i));
            }
            Clip = prevClip;
            ClipSoftness = prevSoft;
            ClipPad = prevPad;
        }

        /// <summary>原版 `DailyRewardSelector.Initialize` / `AdjustView` 的**收尾那一段**（两处逐行同源）：
        /// <code>
        /// if (5 &lt; 当前天) FocusOnItem(scrollRect, items[当前天 − 3]);   // 格中心 = 视口中心
        /// </code>
        /// 判据 → `d:/2/tools/decomp_full/DailyRewardSelector__Initialize.c` 与 `__AdjustView.c`；
        /// `FocusOnItem` 的语义 → `ScrollViewFocusFunctions__FocusOnItem.c`（`normalizedPosition`）
        /// + `ScrollViewFocusFunctions__CalculateFocusedScrollPosition.c`（取 item 矩形中心）。
        /// ⚠️ **本机数据走不到**：`DailyData.StreakCollected` 是常量 **5** ⇒ `5 &lt; 5` 为假 ⇒ 恒不移动
        /// （= 原版开机那一刻的样子；照做、不额外发明）。⚠️ 也**不**依赖原版那条 `if` 之外的东西：
        /// 原版 `ScrollRect` 是 `m_Enabled = 0`（见 `TrackScroll` 的注释）⇒ 位移只有这一条路。</summary>
        public void FocusCurrentDay()
        {
            if (_trackScroll == null) return;
            int day = DailyData.StreakCollected;
            if (day <= 5) return;                                    // 照原版那条 `5 < index`
            int focus = Mathf.Clamp(day - 3, 0, DailyData.StreakDays - 1);
            float cx = ContentPadL + EntryPitch * focus + EntryW * 0.5f;
            _trackScroll.FocusOn(cx);        // 绝对设值（`SetOffset`）—— ⛔ 别改成相对加（规格 §四 第 2 条）
        }

        /// <summary>断签面板：`STREAK BROKEN` + 掉的层数 + 说明 + `Reset Streak`。</summary>
        void BuildFailed(Transform root)
        {
            var anchor = MenuDraw.Node(root, "Streak Failed", FailedAnchor);
            MenuDraw.Text(anchor, F_Broken, DailyData.StreakBrokenText(), Color.white, "Daily Streak Broken", 128.1f, QText);
            MenuDraw.Text(anchor, F_Lost, DailyData.StreakLostText(), Color.white, "Current Streak Lost count", 66.9f, QText);
            MenuDraw.Text(anchor, F_Info, DailyData.StreakInfoText(), Color.white, "Info", 36f, QText);
            var btn = MenuDraw.Rect(anchor, Art(ArtMulligan), F_ResetBtn, "Generic Simplified UI Button", QContent);
            MenuDraw.Text(anchor, F_ResetText, DailyData.ResetStreakText(), Color.white, "Button Text", 55f, QText);
            if (btn != null)
            {
                var hit = btn.gameObject.AddComponent<WindowButton>();
                // 原版 `ResetStreakAfterFail` **不发 PlayFab**（prefab 的 `m_OnClick` 持久调用表是空的，
                // 纯代码挂）；而且它**只是换画面** —— 不写 `HasFailed`、也不减 `currentValue`。
                hit.onClick = () => { DailyData.ResetStreak(); Close(); };
                // 🆕 A17：原版 `Streak Failed>Generic Simplified UI Button` 是 SpriteSwap（普查 §块 4 第 9 行）
                hit.BindSelf(ArtMulligan);
            }
            anchor.gameObject.SetActive(HasFailed);
        }

        void BuildHeader(Transform root)
        {
            var h = MenuDraw.Node(root, "Header With Back Button", HeaderRoot);
            MenuDraw.Rect(h, Art(ArtHeaderBg), H_Bg, "Header Background (1)", QPanel);
            MenuDraw.Text(h, H_Title, DailyData.StreakWindowTitle(), Color.white, "Window Title", 67.55f, QText);
            var back = MenuDraw.Rect(h, Art(ArtBackBtn), H_Back, "Header Back Button", QContent);
            if (back != null)
            {
                var hit = back.gameObject.AddComponent<WindowButton>();
                // 原版：返回钮与背景遮罩**走同一个 `CloseButtonClicked` → `Close()`**，
                // 而 `Close()` = `LiveOp.TryCollect(() => base.Close())` ⇒ **关窗会先自动收取**。
                hit.onClick = () => { DailyData.StreakAutoCollect(); Close(); };
                // 🆕 A17：原版 `Header With Back Button>Header Back Button` 是 SpriteSwap（普查 §块 4 第 10 行）
                // —— ⚠️ 这张的高亮图是 `UI_Button_Menu_Back_Hover`（**大写 H**，走 `WindowButton` 里那张表）
                hit.BindSelf(ArtBackBtn);
            }
        }

        /// <summary>一个奖格：`NormalReward` 那一组（`BG` / `Highlight` / 图标 / 名字 / 角标） + `Collect`。</summary>
        Transform BuildEntry(Transform parent, PxRect entry, int day)
        {
            var e = MenuDraw.Node(parent, "Daily Streak Reward Popup Entry (" + day + ")", entry);
            bool unlocked = DailyData.StreakRewardUnlocked(day);
            bool claimed = DailyData.StreakRewardClaimed(day);

            // 🔴 上面那些 `E_*` 是**格内局部坐标**（原点 = 奖格左上角）——
            //    本窗的奖格**会被 `scaleMultiplierFirstElement` 放大 1.2**，所以必须按
            //    「最终左上角 + 局部坐标 × k」映射（k = 最终宽 ÷ 设计宽），**不能当画布坐标直接用**
            //    （2026-09-23 踩到：所有奖格画到同一处，而断言的宽比对用的是「同一张 BG」⇒ 只报了一条不疼不痒的错）。
            float k = entry.W / EntryW;
            PxRect O(PxRect r) { return new PxRect(entry.x1 + r.x1 * k, entry.y1 + r.y1 * k,
                                                   entry.x1 + r.x2 * k, entry.y1 + r.y2 * k); }

            // 🔴 **2026-10-08（A182）：这一格里的每一件都改走带裁切的那条路**（`DrawRect` / 本文件的 `Text`）
            //    —— 它们都长在 `Viewport` 的 `RectMask2D` 之下，压在视口边上的那几格必须被真裁掉
            //    （原来走裸 `MenuDraw.Rect/Text`：`Rewards Scroll View` 之外的部分**照画出去**，而所有断言全绿）。
            DrawRect(e, Art(ArtBackOpaque), O(E_Bg), "BG", QPanel);
            var hl = DrawRect(e, Art(ArtHighlight), O(E_Highlight), "Highlight", QPanel, HighlightTint);
            if (hl != null) hl.gameObject.SetActive(unlocked && !claimed);
            DrawRect(e, Art(DailyData.StreakRewardIcon(day)), O(E_Holder), "Reward Holder", QContent, null, true);
            Text(e, O(E_Name), DailyData.StreakRewardName(day), Color.white, "Reward Name", 34.05f * k, QText);
            // `Extra Reward Indicator`：**本窗出厂是 true**（每日奖励窗那份是 false）⇒ 照画
            DrawRect(e, Art(ArtExtra), O(E_Extra), "Extra Reward Indicator", QContent);

            // `Collect`（色 (0.06,0.57,0.13,1) + 'Claim'；整组的 `scl=0.7` 已经烘进上面那些矩形里，
            // 但 TMP 的 `m_fontSize` 是**未缩放**的值 ⇒ 字号要自己乘 0.7）
            if (unlocked && !claimed)
            {
                var c = DrawRect(e, Art(ArtClaim), O(E_Collect), "Collect", QContent, ClaimTint);
                Text(e, O(E_CollectText), DailyData.StreakClaimText(), Color.white, "Collect Text",
                     52.85f * 0.7f * k, QText);
                if (c != null)
                {
                    var hit = c.gameObject.AddComponent<WindowButton>();
                    int d = day;
                    hit.onClick = () => DailyData.CollectStreak(d);
                }
            }
            return e;
        }

        /// <summary>绕矩形**中心**缩放（原版 `localScale` 的几何效果；该 prefab 根的 pivot 实测 (.5,.5)）。</summary>
        static PxRect ScaleAbout(PxRect r, float s)
        {
            float cx = (r.x1 + r.x2) * 0.5f, cy = (r.y1 + r.y2) * 0.5f;
            return new PxRect(cx + (r.x1 - cx) * s, cy + (r.y1 - cy) * s,
                              cx + (r.x2 - cx) * s, cy + (r.y2 - cy) * s);
        }

        /// <summary>摆一段字，**吃本窗的裁切**（`Viewport` 的 `RectMask2D` 对文字一视同仁）：
        /// ① 整块在视口外 ⇒ **不建**（`MenuDraw.Visible`）；② 压在视口边上 ⇒ **裁**（`MenuDraw.ClipText`
        /// 逐字夹顶点 + 按同一仿射改 uv）。参数表 = `MenuDraw.Text` + 本窗那一套裁切。
        /// 🔴 **这一段是本文件里的一份副本** —— 同样的三步在 `MainMenuSubmenuWindow.Text` 里也有一份，
        /// 但那一个是 `MenuWindowBase` 家族的实例方法、本窗（`GameWindow` 直系）够不着；
        /// 而**共同基类 `GameWindow` 在 `Shell/WindowsManager.cs`、不在本件白名单**
        /// ⇒ 按「谁的本事谁负责」就地实现，并在报告里记为「该上移到 `GameWindow` 的第二个候选」
        /// （第一个 = A78② 已经上移过去的 `DrawRect` / `DrawNine` / `AddHit` 那三样）。
        /// ⚠️ 两个分支都**先问 `RenderClip`**（含 `ClipPad`；没设裁切时它 = null ⇒ 行为与裸 `MenuDraw.Text` 一致）。</summary>
        Label Text(Transform parent, PxRect r, string s, Color color, string name, float fontPx, int q,
                   float wrapPx = 0f, float autoMinPx = 0f)
        {
            if (!MenuDraw.Visible(r, RenderClip)) return null;
            var lb = MenuDraw.Text(parent, r, s, color, name, fontPx, q, wrapPx, autoMinPx);
            if (lb != null && RenderClip.HasValue) MenuDraw.ClipText(lb, RenderClip, ClipSoftness);
            return lb;
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
            return $"DailyStreak：{(HasFailed ? "断签态" : "连胜态")} · {entries.Count} 个奖格 · 取不到的图 {MissingArt.Count} 张";
        }
    }
}
