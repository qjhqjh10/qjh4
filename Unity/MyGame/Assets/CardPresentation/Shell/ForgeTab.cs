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
//      🔴 **2026-09-23 找茬更正**：原来这里写「`Forge Army Item Button` 与母版的**唯一差别**是
//      `HighlightBG` 的颜色」—— **不成立**。逐份读五元组（`menu_rect.py <pid> --cs`）后，
//      **变体在三处都与母版不同**（母版那一份只在母版自己的 prefab 里成立）：
//        · `HighlightBG` 宽 **114.36**（母版 136）· 色 **品红 `(1,.2784,.902)`**（母版橙，同一张贴图）
//        · `Arrow` 锚 `(0.5,0)`（**框底中点**）+ pos `(0, 12.1)`（母版 `(0,0)` + `(68, 7.5)`）
//        · `Badge Highlight` 锚 `(1,1)`（**右上角**）+ pos `(−17.5,−17.5)`（母版：中心 + `(−44.2,−40.5)`）
//      ⇒ **别拿母版那张表当变体的表用** —— 我们原来就是这么错的：高亮框宽了 21.6px、
//        箭头比原版低 4.6px、红点跑到了**左上角**（差 96px，因初值 alpha 0 而看不见）。
//
// 🔴 **2026-10-03（A12-P1）：上轮欠的四样补完**（`项目任务.md` §三 第 29 条 A12 的 P1 块）：
//   · **粒子三宿主**（`War ParticleSystemUI` · `…Down` · `…Up`）+ **两团可领**（两个宿主里的 `Rays → Glow`）
//     —— 见 `BuildParticleHosts` / `BuildReadyBlobs`。**宿主与接线建了、粒子本身没建**（**出声**，
//     判据见 `Build()` 末尾那条 `Debug.Log`：三套 `ParticleSystem` 的材质是**外链**、我们工程里没有它们的导出资产）。
//   · **`Help Icon` 的 tooltip** —— 原版挂 `EverguildTooltipTrigger`（悬停即出、`OnPointerExit` 即收）。
//     我们这一版**接线走全工程的 `Tooltip` 层**（`Core/Tooltip.cs`），正文取值见 `HelpTipBody`。
//   · **两个 `Viewport` 的裁切** —— `_win.Clip` 的**拿捏范围**补齐：本文件原来那份自己的 `AddHit` 副本
//     与 `BuildRewardIcon` 里的裸 `ImageQuad.Create` **都不吃 `Clip`**（2026-10-03 收口，见两处注释）。
//   · 🆕 **2026-10-04：两处 `Viewport` 的软边（`RectMask2D.m_Softness`）也接了** —— 阵营条 **(42,0)**、
//     奖励轨道 **(0,0) 硬边**（逐处实读，见 `SelSoft` / `TrackSoft` 的注释）。原来那条
//     「⚠️ 软边不在这里 —— 那是 `MenuWindowBase` 那条路」**已作废**（`ClipSoftness` 已经传到台面上了）。
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
        /// <summary>阵营条自己的**纯黑底**（原版 `Forge Army Selector` 根上的 `Image`：`sprite=0` +
        /// `col=(0,0,0,1)` ⇒ **不透明纯黑**；正本 §二「两块纯黑，照画」）。
        /// 🔴 2026-09-23 找茬查出**这一块原来整个漏画**（第 15 条第 18 行那一轮）—— 战役页那份我们是画了的，
        /// 只有锻造页漏 ⇒ 在它上面插入一层，后面几层顺移 +1（层序仍严格递增）。</summary>
        public const int QSelBg = 3019;
        /// <summary>阵营格内部的几层（次序照原版 `Army Item Button` 的子节点序：
        /// `HighlightBG` → `HighlightBG/Arrow` → `Icon` → `Badge Highlight`）。**同样每层一个队列。**</summary>
        public const int QSelLine = 3020,
                           QArmyHighlight = 3021, QArmyArrow = 3022, QArmyIcon = 3023, QArmyBadge = 3024;
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
        public const float TrackPadL = 122f, TrackSpacing = -130f;
        // 格根 `Forge Menu Reward Button`  N(0, 0,1, 0,1, .5,.5, 252.95,−376.553, 505.9,719.463)
        public const float CellW = 505.9f, CellH = 719.463f;

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

        // ---- 粒子宿主（正本 §二 :73 / :76 / :78）----------------------------------------
        // 出处 = 解包 JSON 原文（`assets_full/bundle_menus_assets_all/RectTransform/`），
        // 五元组由 `工具/menu_dump.py bundle_menus_assets_all "Rewards Base Submenu Variant" --depth 12` 复核。
        // 🔴 三件宿主的五元组**逐字相同**：`aMin/aMax/pivot = (.5,.5)` · `sizeDelta = 1483.637939453125²`
        //    · `m_LocalScale = (0,0,0)` —— **scale 0 不是「隐藏」**，是 `UIParticle` 插件的做法
        //    （宿主自己不画，粒子由插件画进画布）⇒ **别照抄那个 0**（我们的节点不带渲染，见 `BuildParticleHosts`）。
        // ⚠️ `War Particle System Up` 这个名字**没有 `UI`**（正本 §二 那格写的 `…Down / Up` 是简写）；
        //    逐份读 GameObject 名字：Down 那份叫 `War ParticleSystemUI Down`、Up 那份叫 `War ParticleSystem Up`。
        /// <summary>宿主方框边长（原版 `m_SizeDelta` 原文，三件同值）。</summary>
        const float PsHostSize = 1483.637939453125f;
        /// <summary>宿主里那个 `ParticleSystem` 子节点的方框边长（原版 100×100，三件同值）。</summary>
        const float PsBodySize = 100f;
        /// <summary>`Ready for level up` 底下两个宿主的 `m_AnchoredPosition`（原文）。
        /// 🔴 **两件不对称**（Down `−374` · Up `+384`）—— 照抄，别「顺手改齐」。</summary>
        static readonly Vector2 PsDownPos = new Vector2(0f, -374f), PsUpPos = new Vector2(0f, 384f);
        /// <summary>`Particle System nebula` 的 `m_LocalPosition`（原文 `(0, ~0, **0.5529959**)`）。
        /// 🔴 **它有 z 不是笔误** —— 这一件**只有一个 `Transform`、没有 `RectTransform`**（3D 子件，
        /// 挂在 `Warp Particle System` 下），z 是它到画布平面的距离。我们这套世界空间里
        /// 相机在 z=−20 朝 +z 看 ⇒ 0.5529959 = 比所有 quad 靠后（**照抄**，见 `BuildParticleHosts`）。</summary>
        const float NebulaZ = 0.5529959f;

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

        // ---- 阵营格（`Forge Army Item Button` 变体）出处 = `menu_rect.py -832184363931035735 --cs` 原文 ----
        /// <summary>`HighlightBG`：锚/枢轴 `(0.5,0.5)` pos `(0,0)` · 尺寸 **114.36×122**；**选中时才显**；
        /// 色 **品红 `(1,.2784,.902)`**（母版是橙 `(1,.631,.2784)`，**同一张贴图**）。
        /// 🔴 2026-09-23 找茬更正：**114.36 是 `Forge Army Item Button` 变体自己的值**（母版是 **136**）——
        ///    正本 §六 那张表列的是**母版**，我们原来拿母版的几何套在变体上（高亮框宽了 21.6px）。</summary>
        static readonly Vector2 HB_Pos = Vector2.zero, HB_Sz = new Vector2(114.36f, 122f);
        /// <summary>`HighlightBG/Arrow`：锚 `(0.5,0)`（= **高亮框底边中点**）pos **(0, 12.1)** · 尺寸 **102.38×30.71**。
        /// 🔴 2026-09-23 找茬更正：原来照母版写「锚 `(0,0)` + pos `(68, 7.5)`」—— 那是靠
        ///    「母版框宽 136 的一半恰好是 68」才凑巧居中；变体框宽 114.36 时它就不居中了，而且比原版低 4.6px。</summary>
        static readonly Vector2 Ar_Pos = new Vector2(0f, 12.1f), Ar_Sz = new Vector2(102.38f, 30.71f);
        /// <summary>`Icon`：**拉伸锚** `(0.0807,0.0822)-(0.9267,0.9260)`，`preserveAspect`。
        /// （这一条**母版与变体一致**，两份原文逐字相同。）</summary>
        static readonly Vector2 Ic_A0 = new Vector2(0.0807f, 0.0822f), Ic_A1 = new Vector2(0.9267f, 0.9260f);
        /// <summary>`Badge Highlight`：**锚 `(1,1)`（右上角）** pos **(−17.5,−17.5)** **35×35**。
        /// 🔴 2026-09-23 找茬更正：原来照母版写「中心 + `(−44.2,−40.5)`」⇒ 落在**左上角**（差 96px）。
        ///    因为它的初值 alpha 是 0（原版 `UiBadgeNotification.Hide()` 之后的样子），**画面上看不出来**；
        ///    红点一亮的那些状态就会露。</summary>
        static readonly Vector2 Bd_Pos = new Vector2(-17.5f, -17.5f), Bd_Sz = new Vector2(35f, 35f);
        static readonly Color ForgeHighlightColor = new Color(1f, 0.2784f, 0.9020f, 1f);

        // ---- 🆕 **2026-10-04：两个 `Viewport` 的 `RectMask2D.m_Softness`（原版逐处实读，别互推）** ----
        /// <summary>`Forge Army Selector/Viewport` = **(42,0)** —— **只渐变 x**（左右各 42px 的渐隐带），y 是硬边。
        /// 🔴 判据（`d:/4/_tmp_view/q1_rm2d.txt`）**三条路径、值都是 (42,0)**：
        ///   · `Forge Tab/Forge Army Selector/Viewport`（:9-10）
        ///   · `Rewards Base Submenu Variant/Content Area/Tabs/Forge Tab/Forge Army Selector/Viewport`（:111-112）
        ///   · `Forge Army Selector/Viewport`（:247-248）
        /// ⚠️ 机制与代价 → `MenuDraw.ApplySoftEdges`（按渐隐带内沿切开 + 逐顶点 alpha 斜坡）。
        /// 🔴 **别把 42 推广到同页的奖励轨道** —— 那是 (0,0)，见下一条（铁律 5·c：一个值 ≠ 全部情况）。</summary>
        static readonly Vector2 SelSoft = new Vector2(42f, 0f);
        /// <summary>`Rewards Scroll View/Viewport`（奖励轨道）= **(0,0) = 硬边**。
        /// 🔴 判据（同上文件）：`Forge Tab/Rewards Scroll View/Viewport`（:189-190）与
        /// `Rewards Base Submenu Variant/…/Forge Tab/Rewards Scroll View/Viewport`（:105-106）soft 都是 **(0,0)**
        /// —— 那两条的 **`m_Padding` 才是 (10,0,0,0)**（padding 只改射线那一面，见 `MenuDraw.PaddedHitRect`）。
        /// ⚠️ 显式写出来（而不是靠默认值）是照 `Clip`/`ClipSoftness` 那条纪律：**谁设 `Clip` 谁顺手把它设对**。</summary>
        static readonly Vector2 TrackSoft = Vector2.zero;

        public void SetHost(RewardsWindow win, Transform root) { _win = win; _root = root; }

        public override void Setup() { Build(); }

        /// <summary>每次切到本页时调。原版 `ForgeWindowTab.OnOpen` 做的是 `SelectArmy` +
        /// `armySelector.Initialize` —— **它不写任何文本/可见性**；我们等价于「刷一遍数据」。</summary>
        public override void OnOpen() { Refresh(); FocusSelectedArmy(); FocusClaimable(); }

        Transform _armyContent, _trackContent, _readyRoot;
        /// <summary>两条滚动区（**全壳唯一一份滚动实现** = `MenuScroll`）：奖励轨道 + 阵营条。
        /// 🔴 2026-09-23 之前**一处滚动都没有** ⇒ 第 5 格（level 5）以后全在屏幕外（**领完第 4 格就领不动**）、
        ///    阵营条两端各 2 个够不着。见 `项目任务.md` §三 第 15 条 **第 21 条**。</summary>
        MenuScroll _trackScroll, _armyScroll;
        /// <summary>自检用：批处理里没有滚轮事件 ⇒ 直调 `MenuScroll.Wheel/ScrollBy`（**和真滚同一条**）。</summary>
        public MenuScroll TrackScroll { get { return _trackScroll; } }
        public MenuScroll ArmyScroll { get { return _armyScroll; } }
        Label _armyText, _levelText;
        ImageQuad _armyIcon;

        // 现算出来的矩形（`Build` 里填），后面 `Refresh` / 子件摆放都靠它们
        PxRect _tabR, _trackR, _selR, _infoR;
        /// <summary>`Army Content` 的矩形 = **选择条中心的一个对称展开区**（见 `UguiLayout.HorizontalContentCentered`）。
        /// 条目的矩形由它算出（**不是**从 `_selR` 左边缘起）。</summary>
        PxRect _armyContentR;

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
            // 粒子宿主之一（正本 §二 :73-75）：`War ParticleSystemUI → Warp Particle System → Particle System nebula`
            BuildParticleHosts(bg);

            // ---- ② `Ready for level up`（可领光效）：**建完立刻关**（纪律④）----
            var readyR = UguiRect.Child(_tabR, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c, Vector2.zero, ReadySz);
            _readyRoot = RewardsWindow.Node(root, "Ready for level up", readyR);
            _win.Rect(_readyRoot, "Glow_UI_W40K",
                      UguiRect.Child(readyR, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c, Vector2.zero, GlowSz),
                      "Glow", QTabReady, GlowColor);
            // 两团可领光效的宿主（正本 §二 :76 / :78）：`…Down` 与 `…Up`，各含 `Rays → Glow`
            BuildReadyBlobs(readyR);
            _readyRoot.gameObject.SetActive(false);

            // ---- ③ `Rewards Scroll View`：奖励轨道（**横向可滚** —— 原版这一件是个 `ScrollRect(横, Elastic)`）
            var track = RewardsWindow.Node(root, "Rewards Scroll View", _trackR);
            var trackVp = RewardsWindow.Node(track, "Viewport", _trackR);
            _trackContent = RewardsWindow.Node(trackVp, "Rewards Content",
                new PxRect(_trackR.x1, _trackR.y1, _trackR.x1 + TrackPadL, _trackR.y2));
            // 内容总宽 = `ContentSizeFitter` 跑完的宽（padLeft + 50 格 + 49 个 −130 的间距）
            // 🔴 没有它 ⇒ 第 5 格中心在 2209px（屏幕外）⇒ **领完第 4 格就再也领不动**（第 21 条）
            _trackScroll = MenuScroll.LeftAligned(_trackR,
                UguiLayout.HorizontalContentW(FixedWs(ForgeData.MaxLevel, CellW), TrackPadL, 0f, TrackSpacing));
            _trackScroll.Owner = root.gameObject;
            _trackScroll.OnChanged = BuildRewardCells;
            PointerLayer.RegisterScroll(_trackScroll);

            // ---- ④ `Forge Army Selector`：阵营选择条 ----
            var sel = RewardsWindow.Node(root, "Forge Army Selector", _selR);
            // 🔴 原版这一件的根上挂 `Image(sprite = 0)` + **`col = (0,0,0,1)`** ⇒ **一条不透明纯黑底**
            //    （正本 §二「`Background` / `Forge Army Selector` 的两块纯黑 ⇒ 照画」）。
            //    2026-09-23 找茬查出：原来**只有 `Background` 那块画了、这一块整个漏了**。
            _win.Rect(sel, null, _selR, "Black", QSelBg, new Color(0f, 0f, 0f, 1f));
            _win.Rect(sel, "40k_main_line_purple",
                      UguiRect.Child(_selR, SepA0, SepA1, SepP, SepPos, SepSz), "Separator Line", QSelLine);
            var selVp = RewardsWindow.Node(sel, "Viewport", _selR);
            // 🔴 **`Army Content` = 「选择条正中心的一个零宽点」**（原版五元组 `… 6.1e-05,0, 0,130`）
            //    + `ContentSizeFitter` ⇒ 布局跑完**以中心对称展开** ⇒ 条目**居中**排。
            //    2026-09-23 找茬实测：原来照左边缘排 ⇒ 13 个条目右端到 **2192.85**，
            //    **最后两个阵营（EmperorsChildren / SpaceWolves）出屏、点不到**。
            _armyContentR = UguiLayout.HorizontalContentCentered(_selR, ForgeData.Armies.Length,
                                                                 ArmyItemW, 0f, 0f, SelSpacing);
            _armyContent = RewardsWindow.Node(selVp, "Army Content", _armyContentR);
            // 🔴 **阵营条也能横向滚**（原版 `Forge Army Selector` 就是一个 `ScrollRect(横)` + `RectMask2D`）：
            //    13 个条目 1604.68 宽 > 视口 1074.36 ⇒ 两端各 2 个够不着；**居中内容的范围是【两侧都有】的**
            //    （`MenuScroll` 那两个极值由内容两端算出来）⇒ 往右滚能看被左柱盖住的前两个、往左滚能看后两个。
            _armyScroll = new MenuScroll(_selR, _armyContentR.x1, _armyContentR.x2);
            _armyScroll.Owner = root.gameObject;
            _armyScroll.OnChanged = BuildArmyItems;
            PointerLayer.RegisterScroll(_armyScroll);

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

            // ---- ⑦ `Help Icon`（原版挂 `EverguildTooltipTrigger` —— 悬停出 tooltip）----
            var helpR = UguiRect.Child(_tabR, HelpA, HelpA, HelpP, HelpPos, HelpSz);
            _win.Rect(root, "40K_generic_bt_info", helpR, "Help Icon", QTabHelp);
            BuildHelpTip(root, helpR);

            // ---- ⑧ 两条活数据 ----
            BuildArmyItems();
            Refresh();
            // 开局定位（滚动的初值）：选中的阵营 + 该领的那一格都**对到视口中心**
            FocusSelectedArmy();
            FocusClaimable();

            Debug.Log("[Forge] 粒子：**三个宿主 + 两团已建**（`War ParticleSystemUI` · `War ParticleSystemUI Down` ·"
                      + " `War ParticleSystem Up` 与其 `Rays → Glow`），**粒子本身没建** —— 原版那三套是 `UIParticle`"
                      + "（Canvas 插件）驱动的 `ParticleSystem`：① 两个 `Rays → Glow` 的 `ParticleSystemRenderer`"
                      + " 材质是**外链**（`m_FileID = 14` / `3`）；② 包内那两张材质（`WarpStuff UI` / `Warp Particle For UI`）"
                      + " 挂的 **shader 也是外链**（`m_FileID = 15` / `20`）。⇒ 这三套在本工程里**没有导出资产**"
                      + "（实测：`WarpforgeVFX/Prefabs/` 那 1922 个 prefab 全来自**战斗特效包**，`effect_index.json`"
                      + " 里 `War ParticleSystem` / `nebula` / `Rays` **各 0 命中**）⇒ **不拿「参数是我们挑的」粒子"
                      + "冒充原版**（铁律 3）。空态：宿主的名字/层级/矩形照原文各就各位，不报错、不留残留。");
            Debug.Log("[Forge] `Help Icon` 的 tooltip **已接线**（悬停进 `Tooltip` 层、离开收）——"
                      + "原版正文是 I2 词条 `MainMenu/Forge/Help`（`EverguildTooltipTrigger.text` + `localize=1`），"
                      + "**词条表在远端 CCD、本地一个 value 都没有** ⇒ **正文留空、不自己编一个字**（见 `HelpTipBody`）。");
        }

        // ============================================================ 粒子宿主（正本 §二 :73-78）

        /// <summary>`Background/War ParticleSystemUI` 那一支（原版三级：
        /// `War ParticleSystemUI`(Canvas+`UIParticle`) → `Warp Particle System`(`ParticleSystem`) →
        /// `Particle System nebula`(`ParticleSystem`，**只有 `Transform`、没有 `RectTransform`**)）。
        /// 🔴 **原版这两级的身位**：宿主 `N(2, .5,.5, .5,.5, .5,.5, 0,0, 1483.637939453125²)`；
        ///    子件 `N(3, .5,.5, .5,.5, .5,.5, (−0.0001638,0), 100²)`（**照抄那个 −0.0001638**）；
        ///    孙子件是**3D 子件**：`m_LocalPosition = (0, 1.65e−08, 0.5529959)`。
        /// ⚠️ 原版宿主 `m_LocalScale = (0,0,0)` —— 那是 `UIParticle` 插件把宿主收起来的做法，
        ///    **不是我们要照抄的版面**；我们的节点不带渲染、也不带 Canvas ⇒ 节点就摆在原版那个矩形上。</summary>
        void BuildParticleHosts(Transform bg)
        {
            var hostR = UguiRect.Child(_tabR, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c, Vector2.zero,
                                       new Vector2(PsHostSize, PsHostSize));
            var host = RewardsWindow.Node(bg, "War ParticleSystemUI", hostR);
            var bodyR = UguiRect.Child(hostR, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                       new Vector2(-0.00016377767315134406f, 0f),
                                       new Vector2(PsBodySize, PsBodySize));
            var body = RewardsWindow.Node(host, "Warp Particle System", bodyR);
            // 这一件**没有 RectTransform** ⇒ 不能用 `Node`（那个按像素矩形摆）；照原版给一个纯 `Transform`
            var neb = RewardsWindow.New(body, "Particle System nebula");
            if (neb != null) neb.localPosition = new Vector3(0f, 0f, NebulaZ);
        }

        /// <summary>`Ready for level up` 底下那**两团**（正本 §二 :76 / :78）：两个 `UIParticle` 宿主，
        /// 各带一套 `Rays → Glow`。两件的名字**不一样**（原文：Down 的孙件叫 **`Glow (1)`**、Up 的叫 **`Glow`**）
        /// —— 照抄，别统一。
        /// 🔴 **归属**：它们是 `Ready for level up` 的子件 ⇒ 纪律④那个 `SetActive(false)` / `Refresh()` 里
        ///    `HasToCollect` 的开关**一并管住它们**（可领才亮、领完就收）—— 这就是「两团**可领**」的接线。</summary>
        void BuildReadyBlobs(PxRect readyR)
        {
            var downR = UguiRect.Child(readyR, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                       PsDownPos, new Vector2(PsHostSize, PsHostSize));
            Blob(RewardsWindow.Node(_readyRoot, "War ParticleSystemUI Down", downR), downR, "Glow (1)");
            var upR = UguiRect.Child(readyR, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                     PsUpPos, new Vector2(PsHostSize, PsHostSize));
            Blob(RewardsWindow.Node(_readyRoot, "War Particle System Up", upR), upR, "Glow");
        }

        /// <summary>一团：`Rays`（`ParticleSystem`）+ 它的子件 `Glow`（`ParticleSystem`）。
        /// 两件的五元组都是 `N(.5,.5, .5,.5, .5,.5, ≈0, 100²)`—— 原版 `m_AnchoredPosition` 是 **1e−5 量级**
        /// （实测 x 都是 `−7.165272836573422e−05`），照抄。</summary>
        void Blob(Transform host, PxRect hostR, string glowName)
        {
            var raysR = UguiRect.Child(hostR, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                       new Vector2(-7.165272836573422e-05f, 0f),
                                       new Vector2(PsBodySize, PsBodySize));
            RewardsWindow.Node(RewardsWindow.Node(host, "Rays", raysR), glowName, raysR);
        }

        // ============================================================ `Help Icon` 的 tooltip

        /// <summary>`Help Icon` 的 tooltip 正文。**原版取值的出处**（`EverguildTooltipTrigger` 的序列化字段，
        /// GameObject `Help Icon` pid `−7799203429925155171` / 触发器 pid `−4382542594159047011`）：
        /// `text = "MainMenu/Forge/Help"` · `localize = 1` · `title = ""` · `tooltipAnchor = 25 (右上)` ·
        /// `offset = (0,0,0)` · `registerEvents = 1` · `preventPassingClickEventToParent = 0`。
        /// 🔴 `Show()` 里那句是 `I2_Loc_LocalizationManager.GetTranslation(text)`（反编译
        /// `EverguildTooltipTrigger__Show.c`）—— **词条表在远端 CCD、本地一个 value 都没有**
        /// ⇒ **正文留空**（同 `ShellRuntime` 那条 `Loading text` 的做法：版式照做、文案留空并说一声）。
        /// ⚠️ **一个空格不是文案**：`Tooltip.Show` 的契约是「空串 = 不显示」
        /// （`Core/Tooltip.cs:120` 的 `if (string.IsNullOrEmpty(body)) { Hide(); return; }`）
        /// ⇒ 用空格让**面板照原版的时机弹出来、里面是空的**；
        /// 断言 `ShownBody.Trim() == ""` 照样成立（即「一个字都没编」）。</summary>
        public const string HelpTipBody = " ";

        /// <summary>自检用/出声用：`Help Icon` 被悬停过几次（批处理里没有输入 ⇒ 见 `PointerLayer.HoverAt`）。</summary>
        public static int HelpTipHovers { get; private set; }

        /// <summary>接线：透明命中区（原版这一件**不是按钮**：没有 `Selectable`、`OnPointerClick` 里
        /// `preventPassingClickEventToParent = 0` ⇒ 点它什么都不做）+ 悬停进 / 离开收。
        /// ⚠️ 这一件的 `m_RaycastPadding` 实测是 **(0,0,0,0)** ⇒ **命中区就是图标自己那个 52.19²**
        ///    （商店那扇 `BoosterInfoPopup` 的同名图标是 −15 = 外扩，逐件不同，别互抄）。</summary>
        void BuildHelpTip(Transform root, PxRect r)
        {
            _helpCenterPx = new Vector2(r.CX, r.CY);
            var hit = AddHit(root, "Help Icon Hit", r, QTabHelp, null);
            if (hit == null) return;
            var wb = hit.GetComponent<WindowButton>();
            if (wb == null) return;
            wb.onEnter = ShowHelpTip;
            wb.onExit = Tooltip.Hide;
            HelpTipHit = wb;
        }

        /// <summary>自检用：`Help Icon` 那个命中区（`PointerLayer.HoverAt` 打到它才算接线通）。</summary>
        public static WindowButton HelpTipHit { get; private set; }

        /// <summary>`Help Icon` 的矩形中心（画布像素）—— tooltip 面板要**钉在触发器自己的位置上**
        /// （原版 `EverguildTooltipTrigger.Show` 取的是 `transform.position`，**不跟鼠标**；
        ///  我们这一件的 `pivot = (.5,.5)` ⇒ 那个位置就是矩形中心）。</summary>
        Vector2 _helpCenterPx;

        void ShowHelpTip()
        {
            HelpTipHovers++;
            if (HelpTipHovers == 1)
                Debug.Log("[Forge] `Help Icon` 悬停：原版正文是 I2 词条 `MainMenu/Forge/Help`（本地没有，见 `HelpTipBody`）"
                          + "⇒ **面板照弹、正文是空的**。要真文案只需把 `HelpTipBody` 换成那份词条值 —— 别再编第二份。");
            // 原版 `tooltipAnchor = 25`（右上）· `offset = (0,0,0)` —— 原样传
            Tooltip.Show(HelpTipBody, LayoutSpace.FromPixel(_helpCenterPx.x, _helpCenterPx.y), 25, Vector3.zero);
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
            // 🔴 **幂等（必须先清）**：滚动回调 `OnChanged` 会重入这里 ⇒ 不清就会越建越多
            //    （2026-09-23 自检当场报出「13 个阵营建了 18 条」—— 断言抓 bug 的又一次实例）。
            //    批处理下没有帧循环 ⇒ 用 `DestroyImmediate`（`Destroy` 不会立刻消失、会和新建的叠在一起）。
            for (int i = _armyContent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(_armyContent.GetChild(i).gameObject);
            // 🔴 **偏移 + 裁切**：条目按**内容坐标**摆，再整体 `Shift` 到屏幕；越界部分由 `Clip` 逐 quad 截掉。
            //    为什么不「先裁再摆」：条目里的图标/高亮/箭头锚点都相对**条目矩形** —— 先裁会把它们一起挪走。
            // 🔴 `_selR` 就是原版那个 `Viewport` 的矩形（原版 `Forge Army Selector` 的 ScrollRect 与它的
            //    `Viewport` **同矩形**：588.2,71.6 → 1662.5,196.7；正本 §二 :84）⇒ `_win.Clip` 用等效
            //    `RectMask2D` 的 **渲染那一面 + 射线那一面**（后者要 `AddHit` 也走这条路，见本文件 `AddHit`）。
            // 🆕 2026-10-04：`ClipSoftness` 也成对拿捏（`_selR` = **(42,0)**，见 `SelSoft`）。
            var prevClip = _win.Clip;
            var prevSoft = _win.ClipSoftness;
            _win.Clip = _selR;
            _win.ClipSoftness = SelSoft;
            for (int i = 0; i < ForgeData.Armies.Length; i++)
            {
                string army = ForgeData.Armies[i];
                // 内容坐标：从 `Army Content` 的**居中**矩形起排（它以选择条中心对称展开），不是从 `_selR` 左边缘
                var content = UguiLayout.HorizontalChild(_armyContentR, ArmyItemW, ArmyItemH, i, 0f, SelSpacing);
                var r = _armyScroll != null ? _armyScroll.Shift(content) : content;
                if (_armyScroll != null && !_armyScroll.Intersects(r)) continue;   // 整条在视口外 ⇒ 不建（点击区也没了）
                var item = RewardsWindow.Node(_armyContent, "ForgeArmyItem_" + i, r);

                // 选中层的**底**（纪律⑥：选中 = 多显一层，不是换图）。**只有选中的那个建**（照原版 SetActive 语义）
                if (army == ForgeData.Selected)
                {
                    var hb = UguiRect.Child(r, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c, HB_Pos, HB_Sz);
                    var hbGo = RewardsWindow.Node(item, "HighlightBG", hb);
                    _win.Rect(hbGo, "40K_settings_button_selected", hb, "img", QArmyHighlight, ForgeHighlightColor);
                    _win.Rect(hbGo, "40K_ArmyTrack_chosen_faction",
                              UguiRect.Child(hb, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), UguiRect.P50c,
                                             Ar_Pos, Ar_Sz),
                              "Arrow", QArmyArrow);
                }

                // 图标（拉伸锚 + preserveAspect）。**两态都用同一张**（纪律⑥）
                _win.Rect(item, DeckRuntime.FactionIcon(army), UguiRect.Child(r, Ic_A0, Ic_A1, UguiRect.P50c,
                          Vector2.zero, Vector2.zero), "Icon", QArmyIcon, null, true);

                // `Badge Highlight`（出厂 `m_IsActive=1`，但**显隐走 alpha 补间**，同左栏四键那条 —— 我们初值 alpha 0）
                // 🔴 锚点是 **变体的 `(1,1)`（右上角）**，不是母版的「中心 + 负偏置」（见 `Bd_Pos` 的更正说明）
                _win.Rect(item, "40K_notification_number",
                          UguiRect.Child(r, UguiRect.A11, UguiRect.A11, UguiRect.P50c, Bd_Pos, Bd_Sz),
                          "Badge Highlight", QArmyBadge, new Color(0.7358f, 0.7358f, 0.7358f, 0f));

                AddHit(item, "Hit", r, QArmyBadge, () => SelectArmy(army));
            }
            _win.Clip = prevClip;
            _win.ClipSoftness = prevSoft;
        }

        /// <summary>换阵营。**照原版 `ForgeWindowTab.SelectArmy`**：写 `ArmyText` / `LevelText` / `Army Icon`，
        /// 重建奖励轨道（原版在这里 `rewardSelector.Initialize(fe)`，再由 `OnLevelUpdated` 回调写 `LevelText`
        /// —— 我们直接写，**结果一样**，那个回调只有这一处用途）。</summary>
        public void SelectArmy(string army)
        {
            ForgeData.Select(army);
            // 选中层是**逐格判断**建的（照原版「选中 = 多显一层」）⇒ 换阵营要重建整条
            //（`BuildArmyItems` 自己会先清 —— 别在这里再清一遍、也别指望调用方清）
            BuildArmyItems();
            FocusSelectedArmy();          // 照原版 `ArmySelector.FocusOnArmy`：把选中的那个对到视口中心
            Refresh();
            FocusClaimable();
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
            // 🔴 **整条一起裁**（原版 `Viewport` 的 `RectMask2D`）：视口外整格不建、压在边缘的按 uv 截
            //    `_trackR` 就是那个 `Viewport` 的矩形（原版 ScrollRect 与 Viewport 同矩形：
            //    331.0,318.6 → 1919.7,1080.0；正本 §二 :80）。
            // 🆕 2026-10-04：这一条的软边是 **(0,0) = 硬边**（原版实读，见 `TrackSoft`）——
            //    显式设一遍（不靠「上一个调用点留下的值」），清的时候也一起清。
            var prevClip = _win.Clip;
            var prevSoft = _win.ClipSoftness;
            _win.Clip = _trackR;
            _win.ClipSoftness = TrackSoft;
            for (int i = 0; i < ForgeData.MaxLevel; i++) BuildCell(i);
            _win.Clip = prevClip;
            _win.ClipSoftness = prevSoft;
        }

        /// <summary>把**该领的那一格**对到视口中心（照原版 `ForgeRewardSelector` 的吸附语义 ——
        /// 它有一路 `FinishedSnapping` 回调，说明原版确实会「吸到某一格」上）。
        /// ⚠️ **只在开页 / 换阵营时调**，别放进 `Refresh`：那样用户滚到别处、一领奖就会被拽回来。</summary>
        public void FocusClaimable()
        {
            if (_trackScroll == null) return;
            int lv = Mathf.Clamp(ForgeData.LevelOf(ForgeData.Selected), 0, ForgeData.MaxLevel - 1);
            _trackScroll.FocusOn(_trackR.x1 + TrackPadL + lv * (CellW + TrackSpacing) + CellW * 0.5f);
        }

        /// <summary>把**选中的阵营**对到视口中心（照原版 `ArmySelector.FocusOnArmy` →
        /// `ScrollViewFocusFunctions.FocusOnItem`：语义就是「item 中心 = 视口中心」）。
        /// ⚠️ 原版那个协程**全代码零调用点**（只能由 prefab 侧 UnityEvent 触发）⇒ 我们用不开页时它会飘到哪去。</summary>
        public void FocusSelectedArmy()
        {
            if (_armyScroll == null) return;
            int i = System.Array.IndexOf(ForgeData.Armies, ForgeData.Selected);
            if (i < 0) return;
            _armyScroll.FocusOn(_armyContentR.x1 + i * (ArmyItemW + SelSpacing) + ArmyItemW * 0.5f);
        }

        /// <summary>`HorizontalLayoutGroup` 的 `ContentSizeFitter` 要的「子件宽表」—— 全同宽时用这个。</summary>
        static float[] FixedWs(int n, float w)
        {
            var a = new float[n];
            for (int i = 0; i < n; i++) a[i] = w;
            return a;
        }

        /// <summary>一格。子件版面出处 正本 §五；锚点照 `menu_rect.py -2025252949017502972 --cs` 原文。</summary>
        void BuildCell(int i)
        {
            var content = UguiLayout.HorizontalChild(_trackR, CellW, CellH, i, TrackPadL, TrackSpacing);
            var r = _trackScroll != null ? _trackScroll.Shift(content) : content;
            if (_trackScroll != null && !_trackScroll.Intersects(r)) return;      // 整格在视口外 ⇒ 不建
            var cell = RewardsWindow.Node(_trackContent, "ForgeCell_" + i, r);
            string a = ForgeData.Selected;
            int st = ForgeData.StateAt(a, i);
            bool claimed = st == ForgeData.Collected;
            bool open = st != ForgeData.Locked;      // `Locked` 时进度条与点数**整段不显示**（原版 cVar8 分支）

            // ---- `RewardTransform`：奖励物品的**运行时挂点**（出厂空节点，pivot (0.5,**0.7**)）----
            // N(1, .5,.5, .5,.5, .5,.7, 0,207, 297.655,384.66)
            var rw = UguiRect.Child(r, UguiRect.P50c, UguiRect.P50c, new Vector2(0.5f, 0.7f),
                                    new Vector2(0f, 207f), new Vector2(297.655f, 384.66f));
            BuildRewardIcon(RewardsWindow.Node(cell, "RewardTransform", rw), rw, i);

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
                var btnQ = _win.Rect(cell, "40K_button", btn, "Generic UI Button", QCellBtnIcon, new Color(1f, 0.363f, 0.919f, 1f));
                // `Button Text` N(2, 0,0, 1,1, .5,.5, 0,0, −26,0) ⇒ 左右各缩 13
                var btLab = _win.TextBox(cell, new PxRect(btn.x1 + 13f, btn.y1, btn.x2 - 13f, btn.y2), "Claim",
                                         Color.white, "Button Text", 36.8f, 12f);
                if (btLab != null) btLab.SetRenderQueue(QCellBtnText);
                // A17：原版 `Forge Menu Reward Button` 的 `Generic UI Button` 是 SpriteSwap（普查 §块 2 第 6 行）
                AddHit(cell, "ClaimHit", btn, QCellBtnIcon, () => ClaimCell(i), btnQ, "40K_button");
            }
        }

        /// <summary>格里的奖励图标（原版是 `ItemDrawer.Draw(rewardTransform, …)` 把物品 prefab 实例化进去）。
        /// ⚠️ 原版的奖励物品是**服务端数据** ⇒ 我们用 `ForgeData.RewardAt` 那张自建表（逐条标明「我们挑的」）。
        /// ⚠️ 高度 **150px 是我们挑的**（原版那件是抽屉 prefab，本地没有 —— 见 `项目任务.md` §三 第 29 条 A12）。
        /// 🔴 **2026-10-03（A12-P1）：改走 `_win.Rect`**（= `MenuDraw.Rect` + 本窗的 `Clip`）。
        ///    原来那一版用的是**裸 `ImageQuad.Create`** ⇒ **不吃裁切**：压在视口边上的格子里这张图
        ///    会整张画到视口外（原版 `RectMask2D` 下面它是被切掉的）。`keepAspect` 那条路会把图
        ///    按自身宽高比放进框 —— 我们给的框与图同比 ⇒ 与原来的「150px 高 + 等比」**逐像素一致**。</summary>
        void BuildRewardIcon(Transform holder, PxRect holderR, int i)
        {
            var tex = _win.Art(ForgeData.RewardAt(i).Art);
            if (tex == null) return;                       // 图不在时不画（`Art` 已经会报出来）
            const float IconH = 150f;
            float w = IconH * ((float)tex.width / Mathf.Max(1f, tex.height));
            var box = new PxRect(holderR.CX - w * 0.5f, holderR.CY - IconH * 0.5f,
                                 holderR.CX + w * 0.5f, holderR.CY + IconH * 0.5f);
            _win.Rect(holder, ForgeData.RewardAt(i).Art, box, "Reward", QCellReward, null, true);
        }

        /// <summary>一个透明点击区（整块矩形），挂 `WindowButton`。🆕 A17：可传「常态图 → 高亮图」。
        /// 🔴 **2026-10-03（A12-P1）：转调 `_win.AddHit`** —— 本文件原来自己那份副本**没吃 `Clip`**，
        ///    于是两个 Viewport 里的点击区会越出视口（压在石柱/页边上的那一块照样吃点击，
        ///    而原版 `RectMask2D` 同时是**射线过滤器**：框外的点判不中）。
        /// 判据与收口说明见 `MenuWindowBase.AddHit` 的注释（UGUI 源码出处写在 `MenuDraw.ClipRect` 里）。</summary>
        Transform AddHit(Transform parent, string name, PxRect r, int q, System.Action onClick,
                         ImageQuad target = null, string art = null, string hoverArt = null, string pressedArt = null)
            => _win.AddHit(parent, name, r, q, onClick, target, art, hoverArt, pressedArt);

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
