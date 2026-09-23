// ForgeTab.cs — 阶段二第 3 层：锻造厂页（`Forge Tab`）
//
// ============================ 出处（唯一正本） ============================
// `资料/阶段二_锻造厂与战役页_原版规格.md` §一（页在窗口里的位置）· §二（层 × 参数表）·
// §三（驱动链）· §五（奖励格）· §六（阵营选择格）。
// **每一个矩形的锚点五元组都是原版 JSON 原文**，由 `工具/menu_rect.py <pid> --cs` 机械吐出来、
// 直接贴进来的（不手抄 —— 手抄锚点五元组出过一次静默的版面 bug）。
//
// 🔴 **矩形别从它自己的 pid 算** —— `Forge Tab` 的父（`Content Area`）在**另一份资产**里，
//    单独喂它自己的 pid 时 `menu_rect.py` 的父链是断的，会把它当成 1920×1080 的根（算出来偏 167px）。
//    **必须从窗口根 `Rewards Base Submenu Variant` 起走链**（本轮就是这么发现的）。
//
// 🔴 **六条纪律**（前四条同 `MissionsTab`）：
//   ① **出厂 `activeSelf=false` 的件不建**：`Xp Points Icon` · `Debug Add points` · `Debug Set Forge` ·
//      格里的 `Collect Highlight Effect`。两个调试件另有硬证据：`ForgeWindowTab.Awake` **无条件**
//      `SetActive(false)`（正本 §三·5，亲读指令流复核过）。
//   ② **被布局组排的子节点，矩形是「布局跑之前的模板位」** ⇒ 用 `UguiLayout` 按原版的 pad/spacing/align 算。
//   ③ **有 `localScale` 的子树，缩放要烘进每个子件的矩形**（`ScaleAbout`），**不能**给父设 `localScale`
//      然后照常摆子件 —— `RewardsWindow.Node` 写的 `localPosition` 是**世界单位**，带缩放的父会把它再乘一次
//      ⇒ 位置飞出去（右柱实测会算到 x≈3773，整根柱子出屏幕）。见 `R()`。
//   ④ 🔴 `Ready for level up` **出厂是激活的**，但原版 `ForgeRewardSelector.Initialize` 第一件事就是
//      `Toggle(false)` ⇒ **我们建完立刻关掉**（漏这步进页第一帧就会看到光效）。
//   ⑤ 🔴 格的**等级圆牌只有两张 sprite**：`Locked/InProgress` → `…_milestone_off`，
//      `ToCollect/Collected` → `…_milestone_on`。**不是三张** ——
//      旧记的「`ProgressRewardLevelObject` 0x28/0x30/0x38 三个 sprite」是**读错了**：
//      0x28 是 `levelButton`（Image 组件本体），0x30/0x38 才是 on/off（正本 §五「三态 sprite 定案」）。
//   ⑥ 🔴 阵营格**不换 sprite**：选中态是「**多显一层** `HighlightBG`」（`ArmyItemContainer.Click` 里
//      `SetActive(highlightState, isOn)`），图标本身**永远用同一张阵营徽记**（正本 §六）。
//      `Forge Army Item Button` 与母版 `Army Item Button` 的**唯一差别**是 `HighlightBG` 的**颜色**：
//      原版橙色 `(1,.631,.2784)` → **锻造页品红 `(1,.2784,.902)`**（同一张 `40K_settings_button_selected`）。
//
// 🔴 **本轮没建的两样**（**出声**，不静默 —— 项目红线）：
//   · **粒子**：`War ParticleSystemUI` / `…Down` / `…Up` 三个宿主 + `Ready for level up` 里那两团。
//     照 0917 普查的判决「**先做静态版 + 空态，粒子后补**」（`资料/普查产出_0917/菜单盘点_汇总.md:55`）。
//   · **`Help Icon` 的 tooltip** —— 原版挂 `EverguildTooltipTrigger`；我们现有的 tooltip 只认关键词/卡面数值，
//     这一处要另做一版「任意文本」，**本轮只画图标**。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>锻造厂页。原版 `ForgeWindowTab : WindowTabBase<MainMenuRewardsWindow>`。</summary>
    public class ForgeTab : WindowTabBase
    {
        public override WindowTabType Type { get { return WindowTabType.Forge; } }

        RewardsWindow _win;
        Transform _root;

        // ---- 出处：正本 §一（机械走链 `工具/menu_rect.py`；父链 窗口根 → Content Area → Tabs → Forge Tab）----
        /// <summary>`Forge Tab`：实测 **x 330.69..1920.00 · y 71.12..1079.82**（1589.31 × 1008.70）。</summary>
        public const float TabL = 330.69f, TabT = 71.12f, TabR = 1920f, TabB = 1079.82f;
        static readonly PxRect TabR_ = new PxRect(TabL, TabT, TabR, TabB);

        /// <summary>渲染队列：**照原版的兄弟序（含子树内的先后）逐层 +1**。
        /// 🔴 **不能把两层放在同一个队列** —— 同队列里 Unity 按「到相机的 3D 距离」排，
        ///    这些层都在 z≈0、只是中心不同 ⇒ **谁盖谁不可控**（2026-09-23 实测两次：
        ///    ① `Background` 的纯黑和 `Background/Warp` 都取一个队列 ⇒ **旋涡整张被黑板盖掉**；
        ///    ② 格里的 `BarEnd`/`LevelBg` 同队列 ⇒ 条形末端标记被圆牌随机盖住）。
        ///    原版的次序是：`Background`(自己的 Image) → 它的子件 `Warp` → `Ready for level up`
        ///    → `Rewards Scroll View` → `Forge Army Selector` → `Background Elements`
        ///    → `Selected Army Info` → `Help Icon`。
        /// ⚠️ 格内还有 5 层（见 `QCellBtn..QCellPoints`）—— 它们夹在 `QTabTrackBase` 与
        ///    `QSelLine` 之间，别把 `QTabTrackBase` 当成「轨道就一层」。</summary>
        public const int QTabBg = 3006, QTabWarp = 3007, QTabReady = 3008, QTabTrackBase = 3009;
        /// <summary>**格内**的各层，次序照原版 `Forge Menu Reward Button` 的子节点顺序：
        /// `Generic UI Button` → `Progress Bars` → `RewardTransform` → `LevelBg` → `Points`。
        /// 🔴 每一层（连同层内的**图标与文字**）都必须**各占一个队列** —— 同队列里
        /// Unity 按到相机的距离排，而这些件都在 z≈0、只差中心 ⇒ 谁盖谁不可控。</summary>
        public const int QCellBtnIcon = 3009, QCellBtnText = 3010,
                           QCellBarsBg = 3011, QCellBarsFill = 3012, QCellBarsEnd = 3013,
                           QCellReward = 3014, QCellLevelBg = 3015, QCellLevelText = 3016,
                           QCellPtsIcon = 3017, QCellPtsText = 3018;
        /// <summary>阵营格内部的几层（次序照原版 `Army Item Button` 的子节点序：
        /// `HighlightBG` → `HighlightBG/Arrow` → `Icon` → `Badge Highlight`）。**同样每层一个队列。**</summary>
        public const int QSelLine = 3019,
                           QArmyHighlight = 3020, QArmyArrow = 3021, QArmyIcon = 3022, QArmyBadge = 3023;
        public const int QTabDecor = 3025, QTabInfo = 3026, QTabHelp = 3027;

        // ============================================================ 页内各件的锚点五元组（照 `--cs` 原文）
        // `Background/Warp`           N(2, .5,.5, .5,.5, .5,.5, 0,0, 767,974)
        static readonly Vector2 WarpPos = Vector2.zero, WarpSz = new Vector2(767f, 974f);
        // `Ready for level up`        N(1, .5,.5, .5,.5, .5,.5, 0,0, 194.422,302.76)
        static readonly Vector2 ReadySz = new Vector2(194.422f, 302.76f);
        // `…/Glow`                    N(2, .5,.5, .5,.5, .5,.5, 0,0, 700,700)   色 **#FF2DDF**
        static readonly Vector2 GlowSz = new Vector2(700f, 700f);
        static readonly Color GlowColor = new Color(1f, 0x2D / 255f, 0xDF / 255f, 1f);

        // `Rewards Scroll View`       N(1, 0,.5, 1,.5, 0,.5, 0.277588,−123.83, −0.555176,761.4)
        static readonly Vector2 TrackA0 = new Vector2(0f, 0.5f), TrackA1 = new Vector2(1f, 0.5f),
                                TrackP = new Vector2(0f, 0.5f),
                                TrackPos = new Vector2(0.277588f, -123.83f), TrackSz = new Vector2(-0.555176f, 761.4f);
        /// <summary>`Rewards Content` 的 `HorizontalLayoutGroup`：**pad left 122 · spacing −130 · align MiddleLeft**
        /// ⇒ 每格宽 **505.9**、相邻两格**重叠 130px**（原版就是让格子叠着排的）。</summary>
        const float TrackPadL = 122f, TrackSpacing = -130f;
        // 格根 `Forge Menu Reward Button`  N(0, 0,1, 0,1, .5,.5, 252.95,−376.553, 505.9,719.463)
        const float CellW = 505.9f, CellH = 719.463f;

        // `Forge Army Selector`       N(1, 0,1, 1,1, .5,.5, 0,−63, −514.955,125.1)
        static readonly Vector2 SelA0 = UguiRect.A01, SelA1 = new Vector2(1f, 1f), SelP = UguiRect.P50c,
                                SelPos = new Vector2(0f, -63f), SelSz = new Vector2(-514.955f, 125.1f);
        // `…/Separator Line`          N(2, 0,.5, 1,.5, .5,.5, 0,−60.258, 193.48,6)
        static readonly Vector2 SepA0 = new Vector2(0f, 0.5f), SepA1 = new Vector2(1f, 0.5f), SepP = UguiRect.P50c,
                                SepPos = new Vector2(0f, -60.258f), SepSz = new Vector2(193.48f, 6f);
        /// <summary>`Army Content` 的 `HorizontalLayoutGroup`：**spacing −14 · align MiddleCenter**（pad bottom 5）。
        /// 条目尺寸取 `Forge Army Item Button` 根的 **136.36 × 121.59**（正本 §六）。</summary>
        const float SelSpacing = -14f, ArmyItemW = 136.36f, ArmyItemH = 121.59f;

        // `Selected Army Info`        N(1, 0,1, 0,1, .5,.5, 599.354,−186, 621.295,122.718)
        static readonly Vector2 InfoA0 = UguiRect.A01, InfoA1 = UguiRect.A01, InfoP = UguiRect.P50c,
                                InfoPos = new Vector2(599.354f, -186f), InfoSz = new Vector2(621.295f, 122.718f);
        // `…/ArmyText`   N(2, 1,1, 1,1, .5,.5, −317.327,−38.6, 320.915,50)   #FCD382 · 41.3（auto 18→41.3）
        static readonly Vector2 AT_A = UguiRect.A11, AT_P = UguiRect.P50c,
                                AT_Pos = new Vector2(-317.327f, -38.6f), AT_Sz = new Vector2(320.915f, 50f);
        // `…/LevelText`  N(2, 1,1, 1,1, .5,.5, −309.087,−82, 331.013,50)     白 · 32.38（auto 18→32.38）
        static readonly Vector2 LT_Pos = new Vector2(-309.087f, -82f), LT_Sz = new Vector2(331.013f, 50f);
        // `…/Army Icon`  N(2, 0,.5, 0,.5, .5,.5, 65,6.1, 125.284,125.28)
        static readonly Vector2 AI_A = new Vector2(0f, 0.5f), AI_Pos = new Vector2(65f, 6.1f),
                                AI_Sz = new Vector2(125.284f, 125.28f);

        // `Help Icon`  N(1, 1,1, 1,1, .5,.5, −208.7,−170.87, 52.1861,52.186)
        static readonly Vector2 HelpA = UguiRect.A11, HelpP = UguiRect.P50c,
                                HelpPos = new Vector2(-208.7f, -170.87f), HelpSz = new Vector2(52.1861f, 52.186f);

        // ---- `Background Elements`：左右两根石柱 + 顶部装饰 ----
        // `Decoration Top`  N(2, 0,1, 0,1, 0,.5, 114,−219, 273,210)
        static readonly Vector2 DecoA0 = UguiRect.A01, DecoA1 = UguiRect.A01, DecoP = new Vector2(0f, 0.5f),
                                DecoPos = new Vector2(114f, -219f), DecoSz = new Vector2(273f, 210f);
        // `Column Left`  N(2, 0,1, 0,1, 0,1, 0,0, 358.626,1396.94)  scl **1.04**
        // `Column Right` N(2, 1,1, 1,1, 0,1, 0,0, 358.625,1396.94)  scl **−1.04**
        static readonly Vector2 ColA0L = UguiRect.A01, ColA1L = UguiRect.A01, ColPL = UguiRect.P01,
                                ColPosL = Vector2.zero, ColSzL = new Vector2(358.626f, 1396.94f);
        static readonly Vector2 ColA0R = UguiRect.A11, ColA1R = UguiRect.A11, ColPR = UguiRect.P01,
                                ColPosR = Vector2.zero, ColSzR = new Vector2(358.625f, 1396.94f);
        /// <summary>石柱整棵子树的 `localScale` 大小（左 **+** / 右 **−**；**大小恒为 1.04**）。</summary>
        const float ColScale = 1.04f;
        // 柱子的子件（**设计空间**。左右两列的锚点不同 ⇒ 各自的五元组都照 dump 原值）
        // `Culumn Top`  N(3, 0,1, 0,1, 0,.5, 0,−223.5, 319,460)   —— 原版拼写就是 Culumn
        static readonly Vector2 CT_A = UguiRect.A01, CT_P = new Vector2(0f, 0.5f),
                                CT_Pos = new Vector2(0f, -223.5f), CT_Sz = new Vector2(319f, 460f);
        // `Culumn Mid`  N(4, 0,0, 0,0, 0,1, 0,0, 206,461)
        static readonly Vector2 CM_A = UguiRect.A00, CM_P = UguiRect.P01, CM_Pos = Vector2.zero,
                                CM_Sz = new Vector2(206f, 461f);
        // `Culumn Down` N(5, 0,0, 0,0, 0,1, 0,0, 213,474)
        static readonly Vector2 CD_A = UguiRect.A00, CD_P = UguiRect.P01, CD_Pos = Vector2.zero,
                                CD_Sz = new Vector2(213f, 474f);
        // `Candle`      N(4, 0,1, 0,1, .5,.5, 142.1,−201.3, 40,59)        scl .962
        // `Light Candle`N(4, 0,1, 0,1, .5,.5, 95.6154,−224.501, 142.584,175.495)  scl .962
        static readonly Vector2 Ca_PosL = new Vector2(142.1f, -201.3f), Ca_Sz = new Vector2(40f, 59f);
        static readonly Vector2 LC_PosL = new Vector2(95.6154f, -224.501f), LC_Sz = new Vector2(142.584f, 175.495f);
        // `Candle (1)`  N(4, 1,1, 1,1, .5,.5, −178.3,−201.3, 40,59)       scl **−0.962**
        // `Light Candle (1)` N(4, 1,1, 1,1, .5,.5, −223.56,−224.51, 142.394,175.492)  scl .962
        static readonly Vector2 Ca_PosR = new Vector2(-178.3f, -201.3f);
        static readonly Vector2 LC_PosR = new Vector2(-223.56f, -224.51f), LC_SzR = new Vector2(142.394f, 175.492f);
        /// <summary>蜡烛/烛光**自己那一层**的 `localScale`（左右都是 +0.962；镜像由外层柱子的负号给）。</summary>
        const float CandleScale = 0.962f;

        // ---- 阵营格（`Forge Army Item Button`，母版 + 一处改色）出处 正本 §六 ----
        /// <summary>`HighlightBG`：`(0.5,0.5)` **136×122**；**选中时才显**；色 **品红 `(1,.2784,.902)`**
        /// （母版 `Army Item Button` 是橙 `(1,.631,.2784)`，**同一张贴图**）。</summary>
        static readonly Vector2 HB_Pos = Vector2.zero, HB_Sz = new Vector2(136f, 122f);
        /// <summary>`HighlightBG/Arrow`：`(0,0)` pos **(68, 7.5)** size **102.38×30.71**。</summary>
        static readonly Vector2 Ar_Pos = new Vector2(68f, 7.5f), Ar_Sz = new Vector2(102.38f, 30.71f);
        /// <summary>`Icon`：**拉伸锚** `(0.0807,0.0822)-(0.9267,0.9260)`，`preserveAspect`。</summary>
        static readonly Vector2 Ic_A0 = new Vector2(0.0807f, 0.0822f), Ic_A1 = new Vector2(0.9267f, 0.9260f);
        /// <summary>`Badge Highlight`：`(0.5,0.5)` pos **(−44.2,−40.5)** **35×35**。</summary>
        static readonly Vector2 Bd_Pos = new Vector2(-44.2f, -40.5f), Bd_Sz = new Vector2(35f, 35f);
        static readonly Color ForgeHighlightColor = new Color(1f, 0.2784f, 0.9020f, 1f);

        public void SetHost(RewardsWindow win, Transform root) { _win = win; _root = root; }

        public override void Setup() { Build(); }

        /// <summary>每次切到本页时调。原版 `ForgeWindowTab.OnOpen` 做的是 `SelectArmy` +
        /// `armySelector.Initialize` —— **它不写任何文本/可见性**；我们等价于「刷一遍数据」。</summary>
        public override void OnOpen() { Refresh(); }

        Transform _armyContent, _trackContent, _readyRoot;
        Label _armyText, _levelText;
        ImageQuad _armyIcon;

        // 现算出来的矩形（`Build` 里填），后面 `Refresh` / 子件摆放都靠它们
        PxRect _tabR, _trackR, _selR, _infoR;

        public void Build()
        {
            var root = _root;
            _tabR = TabR_;
            _trackR = UguiRect.Child(_tabR, TrackA0, TrackA1, TrackP, TrackPos, TrackSz);
            _selR = UguiRect.Child(_tabR, SelA0, SelA1, SelP, SelPos, SelSz);
            _infoR = UguiRect.Child(_tabR, InfoA0, InfoA1, InfoP, InfoPos, InfoSz);

            // ---- ① `Background`：原版 `Image(sprite = 空)` + 色 **#000000** ⇒ 一块**纯黑**满铺（照画，别省）----
            var bg = RewardsWindow.Node(root, "Background", _tabR);
            _win.Rect(bg, null, _tabR, "Black", QTabBg, new Color(0f, 0f, 0f, 1f));
            // `Warp`：旋涡传送门大图（767×974，**与原图同尺寸** ⇒ 不拉伸）。
            // 🔴 **必须比黑板高一个队列**（见 `QTabBg` 上面的说明：同队列会谁盖谁不可控）
            _win.Rect(bg, "40K_ArmyTrack_bg", UguiRect.Child(_tabR, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                      WarpPos, WarpSz), "Warp", QTabWarp);

            // ---- ② `Ready for level up`（可领光效）：**建完立刻关**（纪律④）----
            var readyR = UguiRect.Child(_tabR, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c, Vector2.zero, ReadySz);
            _readyRoot = RewardsWindow.Node(root, "Ready for level up", readyR);
            _win.Rect(_readyRoot, "Glow_UI_W40K",
                      UguiRect.Child(readyR, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c, Vector2.zero, GlowSz),
                      "Glow", QTabReady, GlowColor);
            _readyRoot.gameObject.SetActive(false);

            // ---- ③ `Rewards Scroll View`：奖励轨道 ----
            var track = RewardsWindow.Node(root, "Rewards Scroll View", _trackR);
            var trackVp = RewardsWindow.Node(track, "Viewport", _trackR);
            _trackContent = RewardsWindow.Node(trackVp, "Rewards Content",
                new PxRect(_trackR.x1, _trackR.y1, _trackR.x1 + TrackPadL, _trackR.y2));

            // ---- ④ `Forge Army Selector`：阵营选择条 ----
            var sel = RewardsWindow.Node(root, "Forge Army Selector", _selR);
            _win.Rect(sel, "40k_main_line_purple",
                      UguiRect.Child(_selR, SepA0, SepA1, SepP, SepPos, SepSz), "Separator Line", QSelLine);
            var selVp = RewardsWindow.Node(sel, "Viewport", _selR);
            _armyContent = RewardsWindow.Node(selVp, "Army Content", new PxRect(_selR.x1, _selR.y1, _selR.x1, _selR.y2));

            // ---- ⑤ `Background Elements`：左右石柱 + 顶部装饰（**在轨道之上**，原版兄弟序如此）----
            var decor = RewardsWindow.Node(root, "Background Elements", _tabR);
            _win.Rect(decor, "40k_rewards_forge_decoration_2",
                      UguiRect.Child(_tabR, DecoA0, DecoA1, DecoP, DecoPos, DecoSz), "Decoration Top", QTabDecor);
            BuildColumn(decor, UguiRect.Child(_tabR, ColA0L, ColA1L, ColPL, ColPosL, ColSzL), +1f);
            BuildColumn(decor, UguiRect.Child(_tabR, ColA0R, ColA1R, ColPR, ColPosR, ColSzR), -1f);

            // ---- ⑥ `Selected Army Info`：阵营名 + 等级 + 徽记 ----
            var info = RewardsWindow.Node(root, "Selected Army Info", _infoR);
            _armyText = _win.TextBox(info, UguiRect.Child(_infoR, AT_A, AT_A, AT_P, AT_Pos, AT_Sz), "",
                                     new Color(0xFC / 255f, 0xD3 / 255f, 0x82 / 255f, 1f), "ArmyText", 41.3f, 18f);
            _levelText = _win.TextBox(info, UguiRect.Child(_infoR, AT_A, AT_A, AT_P, LT_Pos, LT_Sz), "",
                                      Color.white, "LevelText", 32.38f, 18f);
            _armyIcon = _win.Rect(info, DeckRuntime.FactionIcon(ForgeData.Selected),
                                  UguiRect.Child(_infoR, AI_A, AI_A, UguiRect.P50c, AI_Pos, AI_Sz),
                                  "Army Icon", QTabInfo, null, true);
            // ⚠️ `Xp Points Icon`（出厂 inactive）**不建** —— 反编译实证全代码无人点亮它（正本 §三·5）。

            // ---- ⑦ `Help Icon` ----
            _win.Rect(root, "40K_generic_bt_info",
                      UguiRect.Child(_tabR, HelpA, HelpA, HelpP, HelpPos, HelpSz), "Help Icon", QTabHelp);

            // ---- ⑧ 两条活数据 ----
            BuildArmyItems();
            Refresh();

            Debug.Log("[Forge] 粒子这一层**本轮没建**（`War ParticleSystemUI` / `…Down` / `…Up` + 可领光效里那两团）"
                      + " —— 照 0917 普查的判决「先做静态版 + 空态，粒子后补」");
            Debug.Log("[Forge] `Help Icon` 原版挂 `EverguildTooltipTrigger`，**本轮只画图标、不带 tooltip**"
                      + "（我们现有的 tooltip 只认关键词/卡面数值，这一处要另做一版「任意文本」）");
        }

        // ============================================================ 石柱

        /// <summary>一根石柱。`s` = ±1（**符号**才是镜像，大小恒为 1.04）。
        /// 🔴 **做法：容器不带 `localScale`，把缩放烘进每个子件的矩形**（`R()`）——
        /// 见文件头纪律③（给父设 scale 会让 `Node` 的世界单位局部坐标被再乘一次，位置飞出去）。</summary>
        void BuildColumn(Transform parent, PxRect colRect, float s)
        {
            bool left = s > 0f;
            float k = left ? ColScale : -ColScale;      // 🔴 `s` 只表**符号**，大小恒为 1.04 —— 别把 ±1 当倍数用
            var col = RewardsWindow.Node(parent, left ? "Column Left" : "Column Right", colRect);

            // `Culumn Top` 的**设计空间**矩形（`Candle`/`Light Candle` 是它的子件，要相对它算）
            var topD = UguiRect.Child(colRect, CT_A, CT_A, CT_P, CT_Pos, CT_Sz);
            var topR = R(topD, colRect.x1, k, ColScale);
            var top = RewardsWindow.Node(col, "Culumn Top", topR);           // 原版拼写就是 Culumn
            _win.Rect(top, "40k_rewards_forge_decoration_Column_top", topR, "img", QTabDecor);
            Candle(top, topD, left, colRect.x1, k);

            var midR = R(UguiRect.Child(colRect, CM_A, CM_A, CM_P, CM_Pos, CM_Sz), colRect.x1, k, ColScale);
            var mid = RewardsWindow.Node(top, "Culumn Mid", midR);
            _win.Rect(mid, "40k_rewards_forge_decoration_Column_mid", midR, "img", QTabDecor);

            var downR = R(UguiRect.Child(colRect, CD_A, CD_A, CD_P, CD_Pos, CD_Sz), colRect.x1, k, ColScale);
            var down = RewardsWindow.Node(mid, "Culumn Down", downR);
            _win.Rect(down, "40k_rewards_forge_decoration_Column_down", downR, "img", QTabDecor);
        }

        /// <summary>把**设计空间**矩形换成最终矩形：绕柱子的**左上角** `(pivotX, TabT)` 按 `(sx, sy)` 缩放。
        /// ⚠️ `menu_rect.py` 只打 `scl=` 标记（**且只打 x**）、**不把 scale 乘进 rect**
        /// ⇒ 五元组算出来的就是设计空间。
        /// 🔴 **两轴必须分开**：原版 `Column Right` 的 `m_LocalScale = (−1.04, **+1.04**, +1.04)`
        ///    —— **只翻 x**（实测读的原始 JSON）。整根柱子若两轴都取负，y 也跟着翻 ⇒ 柱子朝上长、跑到屏幕外。</summary>
        static PxRect R(PxRect r, float pivotX, float sx, float sy) => ScaleXY(r, pivotX, TabT, sx, sy);

        /// <summary>绕 `(px,py)` 按 `(sx,sy)` 缩放；**负号是镜像** ⇒ 结果归一化成 `x1&lt;x2 · y1&lt;y2`。</summary>
        static PxRect ScaleXY(PxRect r, float px, float py, float sx, float sy)
        {
            float ax1 = px + (r.x1 - px) * sx, ax2 = px + (r.x2 - px) * sx;
            float ay1 = py + (r.y1 - py) * sy, ay2 = py + (r.y2 - py) * sy;
            return new PxRect(Mathf.Min(ax1, ax2), Mathf.Min(ay1, ay2), Mathf.Max(ax1, ax2), Mathf.Max(ay1, ay2));
        }

        /// <summary>蜡烛 + 烛光：**各自再绕自己的中心缩 0.9615**（右列那一份的 x 是 **负**的 —— 实测
        /// `Candle (1)` 的 `m_LocalScale = (−0.9615, +0.9615, +0.9615)`），最后跟着柱子缩。
        /// `topD` 是 `Culumn Top` 的**设计空间**矩形（子件的锚点是相对它给的）。</summary>
        void Candle(Transform parent, PxRect topD, bool left, float pivotX, float colSx)
        {
            float cx = left ? CandleScale : -CandleScale;    // 蜡烛自己那一层的 x 缩放（带符号）
            var aP = left ? UguiRect.A01 : UguiRect.A11;

            var c0 = UguiRect.Child(topD, aP, aP, UguiRect.P50c, left ? Ca_PosL : Ca_PosR, Ca_Sz);
            var cr = R(ScaleXY(c0, c0.CX, c0.CY, cx, CandleScale), pivotX, colSx, ColScale);
            _win.Rect(RewardsWindow.Node(parent, left ? "Candle" : "Candle (1)", cr),
                      "40k_rewards_forge_decoration_1_candle", cr, "img", QTabDecor);

            var l0 = UguiRect.Child(topD, aP, aP, UguiRect.P50c, left ? LC_PosL : LC_PosR, left ? LC_Sz : LC_SzR);
            var lr = R(ScaleXY(l0, l0.CX, l0.CY, cx, CandleScale), pivotX, colSx, ColScale);
            _win.Rect(RewardsWindow.Node(parent, left ? "Light Candle" : "Light Candle (1)", lr),
                      "40k_rewards_forge_decoration_1_candle_light", lr, "img", QTabDecor);
        }

        // ============================================================ 阵营选择条

        /// <summary>`Forge Army Selector` 的行。原版的列表来自**服务端 forge 事件里出现过的阵营** —— 我们取不到
        /// ⇒ 用我们自己的 13 个阵营（照用户边界「全解锁」）。**徽记图走 `DeckRuntime.FactionIcon`（全工程唯一一份）。**</summary>
        void BuildArmyItems()
        {
            for (int i = 0; i < ForgeData.Armies.Length; i++)
            {
                string army = ForgeData.Armies[i];
                var r = UguiLayout.HorizontalChild(_selR, ArmyItemW, ArmyItemH, i, 0f, SelSpacing);
                var item = RewardsWindow.Node(_armyContent, "ForgeArmyItem_" + i, r);

                // 选中层的**底**（纪律⑥：选中 = 多显一层，不是换图）。**只有选中的那个建**（照原版 SetActive 语义）
                if (army == ForgeData.Selected)
                {
                    var hb = UguiRect.Child(r, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c, HB_Pos, HB_Sz);
                    var hbGo = RewardsWindow.Node(item, "HighlightBG", hb);
                    _win.Rect(hbGo, "40K_settings_button_selected", hb, "img", QArmyHighlight, ForgeHighlightColor);
                    _win.Rect(hbGo, "40K_ArmyTrack_chosen_faction",
                              UguiRect.Child(hb, UguiRect.A00, UguiRect.A00, UguiRect.P50c, Ar_Pos, Ar_Sz),
                              "Arrow", QArmyArrow);
                }

                // 图标（拉伸锚 + preserveAspect）。**两态都用同一张**（纪律⑥）
                _win.Rect(item, DeckRuntime.FactionIcon(army), UguiRect.Child(r, Ic_A0, Ic_A1, UguiRect.P50c,
                          Vector2.zero, Vector2.zero), "Icon", QArmyIcon, null, true);

                // `Badge Highlight`（出厂 `m_IsActive=1`，但**显隐走 alpha 补间**，同左栏四键那条 —— 我们初值 alpha 0）
                _win.Rect(item, "40K_notification_number",
                          UguiRect.Child(r, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c, Bd_Pos, Bd_Sz),
                          "Badge Highlight", QArmyBadge, new Color(0.7358f, 0.7358f, 0.7358f, 0f));

                AddHit(item, "Hit", r, QArmyBadge, () => SelectArmy(army));
            }
        }

        /// <summary>换阵营。**照原版 `ForgeWindowTab.SelectArmy`**：写 `ArmyText` / `LevelText` / `Army Icon`，
        /// 重建奖励轨道（原版在这里 `rewardSelector.Initialize(fe)`，再由 `OnLevelUpdated` 回调写 `LevelText`
        /// —— 我们直接写，**结果一样**，那个回调只有这一处用途）。</summary>
        public void SelectArmy(string army)
        {
            ForgeData.Select(army);
            // 选中层是**逐格判断**建的（照原版「选中 = 多显一层」）⇒ 换阵营要重建整条
            for (int i = _armyContent.childCount - 1; i >= 0; i--) Object.DestroyImmediate(_armyContent.GetChild(i).gameObject);
            BuildArmyItems();
            Refresh();
        }

        /// <summary>把当前阵营的数据刷到画面上（原版 `Refresh` / `RefreshLevel` / `CreateRewards` 三步）。</summary>
        public void Refresh()
        {
            string a = ForgeData.Selected;
            if (_armyText != null) _armyText.SetText(a);
            if (_levelText != null) _levelText.SetText(ForgeData.LevelText(a));   // 原版 `"{0} {1}/{2}"` 套 "MainMenu/Level"
            if (_armyIcon != null)
            {
                var t = _win.Art(DeckRuntime.FactionIcon(a));
                if (t != null) { _armyIcon.SetTexture(t); _armyIcon.SetAspect((float)t.width / t.height); }
            }
            BuildRewardCells();
            // 纪律④：**可领光效的判据 = `hasToCollectReward`**（原版在 `CreateRewards` 里对每格取或）
            if (_readyRoot != null) _readyRoot.gameObject.SetActive(ForgeData.HasToCollect(a));
        }

        // ============================================================ 奖励轨道（一格 = 一个等级）

        /// <summary>建当前阵营的全部格。**照原版 `ForgeRewardSelector.CreateRewards`**：
        /// 一格一个 `ForgeRewardItemContainer`，父 = `Rewards Content`（那是个 HLG）。
        /// ⚠️ 批处理下没有帧循环 ⇒ 用 `DestroyImmediate`（用 `Destroy` 不会立刻消失、会和新建的叠在一起）。</summary>
        void BuildRewardCells()
        {
            if (_trackContent == null) return;
            for (int i = _trackContent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(_trackContent.GetChild(i).gameObject);
            for (int i = 0; i < ForgeData.MaxLevel; i++) BuildCell(i);
        }

        /// <summary>一格。子件版面出处 正本 §五；锚点照 `menu_rect.py -2025252949017502972 --cs` 原文。</summary>
        void BuildCell(int i)
        {
            var r = UguiLayout.HorizontalChild(_trackR, CellW, CellH, i, TrackPadL, TrackSpacing);
            var cell = RewardsWindow.Node(_trackContent, "ForgeCell_" + i, r);
            string a = ForgeData.Selected;
            int st = ForgeData.StateAt(a, i);
            bool claimed = st == ForgeData.Collected;
            bool open = st != ForgeData.Locked;      // `Locked` 时进度条与点数**整段不显示**（原版 cVar8 分支）

            // ---- `RewardTransform`：奖励物品的**运行时挂点**（出厂空节点，pivot (0.5,**0.7**)）----
            // N(1, .5,.5, .5,.5, .5,.7, 0,207, 297.655,384.66)
            var rw = UguiRect.Child(r, UguiRect.P50c, UguiRect.P50c, new Vector2(0.5f, 0.7f),
                                    new Vector2(0f, 207f), new Vector2(297.655f, 384.66f));
            BuildRewardIcon(RewardsWindow.Node(cell, "RewardTransform", rw), i);

            // ---- `LevelBg`（等级圆牌）+ `LevelLabel` ----
            // N(1, .5,.5, .5,.5, .5,.5, −0.59652,−267.2, 85,88) · `LevelLabel` N(2, 0,0, 1,1, .5,.5, 0,0, −0,0)
            var lv = UguiRect.Child(r, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                    new Vector2(-0.59652f, -267.2f), new Vector2(85f, 88f));
            // 纪律⑤：**只有两张 sprite**
            _win.Rect(cell, (st == ForgeData.ToCollect || claimed) ? "40k_ArmyTrack_milestone_on"
                                                                  : "40k_ArmyTrack_milestone_off",
                      lv, "LevelBg", QCellLevelBg);
            var lvLab = _win.TextBox(cell, lv, (i + 1).ToString(), Color.white, "LevelLabel", 45f, 0f);
            if (lvLab != null) lvLab.SetRenderQueue(QCellLevelText);

            if (open)
            {
                // ---- `Progress Bars`：N(1, .5,.5, .5,.5, .5,.5, −185.672,−267.2, 310,20) ----
                var bar = UguiRect.Child(r, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                         new Vector2(-185.672f, -267.2f), new Vector2(310f, 20f));
                var bars = RewardsWindow.Node(cell, "Progress Bars", bar);
                _win.Rect(bars, "40K_ArmyTrack_bar", bar, "ProgressionBG", QCellBarsBg);
                // `ProgressionBar` N(2, .5,.5, .5,.5, 0,.5, −154.964,0, 309.933,19.598)
                // ⚠️ 原版是**改它的 `sizeDelta.x`** 来表达进度（不是缩放）⇒ 这里直接把矩形右边缘缩到 f01
                float f01 = claimed ? 1f : ForgeData.Bar01(a, i);
                var barFull = UguiRect.Child(bar, UguiRect.P50c, UguiRect.P50c, new Vector2(0f, 0.5f),
                                             new Vector2(-154.964f, 0f), new Vector2(309.933f, 19.598f));
                var fill = new PxRect(barFull.x1, barFull.y1, barFull.x1 + barFull.W * f01, barFull.y2);
                _win.Rect(bars, "40K_ArmyTrack_bar_fill", fill, "ProgressionBar", QCellBarsFill);
                // `BarEnd` N(3, 1,.5, 1,.5, 0.15,.5, 0.25,−0.0003, 45,40) —— 钉在**填充的右端**
                _win.Rect(bars, "40K_ArmyTrack_bar_position",
                          UguiRect.Child(fill, UguiRect.A10, UguiRect.A10, new Vector2(0.15f, 0.5f),
                                         new Vector2(0.25f, -0.0003f), new Vector2(45f, 40f)),
                          "BarEnd", QCellBarsEnd);

                // ---- `Points`：N(1, .5,.5, .5,.5, .5,.5, −196.1,−301.51, 0,40.8397) ----
                // 宽 0 是「`ContentSizeFitter` 还没跑」的模板值；实算 = `Clamp(46.2852+162.54, 0, 214)` = **208.83**，
                // 中心不动 ⇒ 盒子以中心对称、各 104.41 宽（正本 §五「布局组实算 A」）。
                var ptC = UguiRect.Child(r, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                         new Vector2(-196.1f, -301.51f), new Vector2(0f, 40.8397f));
                const float boxW = 208.83f, iconW = 46.2852f, labelW = 162.54f;
                float px1 = ptC.CX - boxW * 0.5f;
                _win.Rect(cell, ForgeData.ForgePointIcon(a),
                          new PxRect(px1, ptC.y1, px1 + iconW, ptC.y2), "Army", QCellPtsIcon, null, true);
                // `PointsLabel` 的文本照原版 `"{currentPoints}/{neededPoints}"`（`__Initialize.c:200-202`）
                var ptsRect = new PxRect(px1 + iconW, ptC.y1, px1 + iconW + labelW, ptC.y2);
                var ptsLab = _win.TextBox(cell, ptsRect, ForgeData.PointsText(a, i), Color.white, "PointsLabel", 33.7f, 18f);
                if (ptsLab != null) { ptsLab.SetRenderQueue(QCellPtsText); MenuDraw.AlignLeft(ptsLab, ptsRect); }
            }

            // ---- `Generic UI Button`（Claim）：**只有 `ToCollect` 时才建**（原版是开关 `claimButton`）----
            if (st == ForgeData.ToCollect)
            {
                // N(1, .5,.5, .5,.5, .5,.5, −1.5e−05,−134.147, 200.762,47.252)
                var btn = UguiRect.Child(r, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                         new Vector2(0f, -134.147f), new Vector2(200.762f, 47.252f));
                _win.Rect(cell, "40K_button", btn, "Generic UI Button", QCellBtnIcon, new Color(1f, 0.363f, 0.919f, 1f));
                // `Button Text` N(2, 0,0, 1,1, .5,.5, 0,0, −26,0) ⇒ 左右各缩 13
                var btLab = _win.TextBox(cell, new PxRect(btn.x1 + 13f, btn.y1, btn.x2 - 13f, btn.y2), "Claim",
                                         Color.white, "Button Text", 36.8f, 12f);
                if (btLab != null) btLab.SetRenderQueue(QCellBtnText);
                AddHit(cell, "ClaimHit", btn, QCellBtnIcon, () => ClaimCell(i));
            }
        }

        /// <summary>格里的奖励图标（原版是 `ItemDrawer.Draw(rewardTransform, …)` 把物品 prefab 实例化进去）。
        /// ⚠️ 原版的奖励物品是**服务端数据** ⇒ 我们用 `ForgeData.RewardAt` 那张自建表（逐条标明「我们挑的」）。</summary>
        void BuildRewardIcon(Transform holder, int i)
        {
            var tex = _win.Art(ForgeData.RewardAt(i).Art);
            if (tex == null) return;                       // 图不在时不画（`Art` 已经会报出来）
            var q = ImageQuad.Create(holder, tex, Vector3.zero, LayoutSpace.Px(150f),
                                     new Vector2(0.5f, 0.5f), "Reward");
            if (q == null) return;
            q.SetAspect((float)tex.width / tex.height);
            q.SetRenderQueue(QCellReward);
        }

        /// <summary>一个透明点击区（整块矩形），挂 `WindowButton`。</summary>
        void AddHit(Transform parent, string name, PxRect r, int q, System.Action onClick)
        {
            var hit = RewardsWindow.New(parent, name);
            var hq = ImageQuad.Create(hit, CardArt.Solid(), RewardsWindow.Local(parent, r.x1, r.y1, r.x2, r.y2),
                                      LayoutSpace.Px(r.H), new Vector2(0.5f, 0.5f), "Hit");
            if (hq != null)
            {
                hq.SetAspect(r.W / Mathf.Max(1e-6f, r.H));
                hq.SetTint(new Color(0f, 0f, 0f, 0f));
                hq.SetRenderQueue(q);
            }
            var wb = hit.gameObject.AddComponent<WindowButton>();
            wb.onClick = onClick;
        }

        void ClaimCell(int i)
        {
            string why;
            if (ForgeData.Claim(ForgeData.Selected, i, out why)) { Refresh(); return; }
            // 红线：**不许静默失败** —— 领不到时说清为什么
            Debug.Log("[Forge] 第 " + (i + 1) + " 格领不了：" + why);
        }

        public string Dump()
        {
            string a = ForgeData.Selected;
            return "Forge：阵营 " + a + " · " + ForgeData.LevelText(a)
                   + " · 经验 " + ForgeData.PointsOf(a)
                   + " · 轨道 " + (_trackContent != null ? _trackContent.childCount : 0) + " 格"
                   + " · 可领光效 " + (_readyRoot != null && _readyRoot.gameObject.activeSelf ? "亮" : "灭");
        }
    }
}
