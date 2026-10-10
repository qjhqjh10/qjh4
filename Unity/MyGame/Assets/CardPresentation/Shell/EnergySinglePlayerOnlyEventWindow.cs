// EnergySinglePlayerOnlyEventWindow.cs — A103 第 8 扇：**`EnergySinglePlayerOnlyEventWindow`（单机能源活动窗）**
//   （原版 `SinglePlayerOnlyEnergyEventWindow : LiveOpsEventWindow<SinglePlayerScoringEvent>`）
//
// ============================ 出处（判据一律现读）============================
// ① 节点树 / 矩形 / 字号 / 对齐 / 颜色 / 九宫 / 出厂显隐（**70 个节点**）：
//      python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "EnergySinglePlayerOnlyEventWindow" \
//             --depth 14 --relative --md
//    下面每一个数都能在那条命令的输出里逐字找到。
// ② 窗口参数（`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_2964990757648218801.json`，逐字段实读）：
//      `type = 0`(Fullscreen) · `windowsPlacement = 15`(Popup) · `closeOnESC = 0` ·
//      `updateNavPanel = 0` · `extraScaleSmallScreen = 1.0`
//    🔴 **逐窗实读、⛔ 别互推**：同族的 `SkirmishModeEventWindow` / `RankedEventWindowV2` 是
//      `type = 1 Popup` + `placement = 5 Canvas` + `closeOnESC = 1` —— **三格全不一样**。
// ③ 类接口与分支（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/SinglePlayerOnlyEnergyEventWindow.cs`
//    字段表 + `d:/2/tools/decomp_full/SinglePlayerOnlyEnergyEventWindow__*.c` + `ShowCurrentEnergyRegenText__OnEnable.c`）。
// ④ 同族形状照 `Shell/LiveOpsEventWindow.cs`（表头 / 记分条 / 按钮那一套 idiom）。
//
// ============================ 这一扇为什么**不继承** `LiveOpsEventWindow` ============================
// 原版它确实是 `LiveOpsEventWindow<T>` 的子类，但**我们那个基类不是原版基类的等价物** ——
// `Shell/LiveOpsEventWindow.cs` 是**遭遇战/排位那一族**的基类，`Open()` 里带三件本窗没有的东西：
//   ① `DeckIndex = FirstDeckInMode(...)`（选卡组）② `SearchingMatchPopup.Attach(...)`（12 秒匹配链）
//   ③ `To Battle Button` 那一整棵开战树。
// 本窗的树里**一件都没有**（没有 `To Battle Button` / `Ranked Deck Selection` / `TrophyIcon` /
// `ButtonIcons`）⇒ 硬继承会把匹配弹窗与开战链**静默**拖进来（本工程红线）。
// 原版那个 `LiveOpsEventWindow<T>` 本体在反编译里只有 4 个成员（`LiveOp` 属性 / `OnRefresh` /
// `Close` / `SetupData`），本窗一件都用不上 ⇒ **直接挂 `GameWindow`**；等价关系如实记在这里（⛔ 不假装）。
//
// ============================ 状态 → 参数（哪些格受运行时影响）============================
// | 件 | prefab 出厂（现读） | 运行期由谁改 | 我们的处置 |
// |---|---|---|---|
// | `Reward Help` | **act = F** | 无人（出厂就关） | 建出来、**关着** |
// | 5 行 `Score` | `'1256'` | `RefreshScore()` 按活动分数 | **照 prefab 出厂原文** + 出声 |
// | 5 颗 `Chest` / `Highlight Crate` | 闭箱图 / 不透明金高亮（都开着） | 同上 | **照 prefab 出厂原文** + 出声 |
// | `Timer` / `Timer Icon` | `'Termina en: 23d 5h'` | `TimerDisplay.Initialize(endTime)` | **照 prefab 出厂原文** + 出声 |
// | `Energy Instructions` | `'… every {0} minutes.'` | `ShowCurrentEnergyRegenText.OnEnable` 读 **LiveOps Config** | **照 prefab 出厂原文（含 `{0}`）** + 出声（判据见下） |
// | `Total Victories` | `'751'` | `RewardProgressBarPanel` 按活动数据 | **照 prefab 出厂原文** + 出声 |
// | `Event Image` / `Scoring Icon` / `ArmyItemReference` | **`m_Sprite` = null** | 运行时由字段灌图 | 建节点、**不画**、出声 |
// | `Army Content` 下的阵营 | 出厂只有模板 `ArmyItemReference` | `ShowEventArmiesPanel` 逐阵营 `Instantiate` | **只建模板那一颗**（= 出厂同档） |
// | `Progress Bar` 的 `Fill` | 现读 **0.00 × 5.74**（= 进度 0） | `Slider.normalizedValue`（`InterpolateMilestones`） | 出厂画 0 宽；`SetProgress(v)` 是**两态**那一半（自检用） |
//
// 🔴 `{0}` 的判据（`ShowCurrentEnergyRegenText__OnEnable.c` 逐句）：
//    `LiveOps ConfigManager.GetConfig(...)`（**远端配置**）+0x20 = 那个整数，`String.Format(译文, 它, 时长)`。
//    本地没有那份 Config ⇒ **替换值查不到**。按「照字段抄 + 不编数字」⇒ **原样保留**（含 `{0}`）并出声。
//    ⛔ 别自己填一个「几分钟」（那就是编数字；本工程红线）。`--active-only` 那一档下这一行也照旧。
//
// ============================ 🔴🔴 2026-10-13（A795）就地订正 —— 下面这一段原来是【错的】 ============================
// 原文（已删，更正痕迹照铁律 5 保留）：「`BackgroundCloseButton.window` 在 prefab 里是 `null`、
//   `onClick.m_PersistentCalls.m_Calls = []` ⇒ **原版这扇窗点压暗层什么都不发生** ⇒ 出口只有返回钮」。
// 🔴 **它推错了一步，结论也反了** —— 错因 = **只读序列化字段就把行为推死**
//    （CLAUDE.md 铁律 5·c「一个值 ≠ 全部情况」）。三步实据（W-A788 2026-10-13 现读，本节逐条复核过）：
//   ① `BackgroundCloseButton.OnPointerClick` 里那两个 `if` 是**并列**的
//      （`BackgroundCloseButton__…OnPointerClick.c`）：`window` 为空**只**少一次 `window.Close()`，
//      下面那句 `onClick.Invoke()` **照常跑** ⇒「`window` 是空 ⇒ 点了什么都不发生」**推不出来**。
//   ② 原版**在运行时**给它挂了监听：`SinglePlayerOnlyEnergyEventWindow__OnEnable.c:13-18` ——
//      字段 `+0x80`(`backgroundCloseButton`) → `+0x28`(`onClick`) → `UnityEvent.AddListener`。
//   ③ 挂的**正是表头返回钮那一个处理函数**：`__Open.c` 把**同一个 delegate token**（`DAT_18427a338`，
//      与 ② 同一个）交给 `WindowHeaderWithBackButton.Initialize(header, data, <它>, 0)`
//      （第三参 = `Action OnBackButtonPressed`）⇒ **点压暗层 = 点返回钮 = `Close()`**，**出口有两条**。
// 🔴 细节上的三条配套（都实读过，别再把它们当成两条路不一致的证据）：
//   · 原版 `window` 字段在 prefab 里确实是 `{0,0}`、`onClick` 的 `m_Calls` 确实是空 ——
//     **这两条都属实**，它们只是**推不出**「点了不关」（接线在运行时、不在序列化里）。
//   · 我们的处置 = `Build()` 末尾那句 `MenuDraw.ShadeHit(transform, RootR, QShade, QHit, () => Close(), "BackgroundHit")`
//     —— 形状照同族范本（`Shell/RankedEventWindow.cs:64` · `Shell/SkirmishEventWindow.cs:74`）。
//   · 面板那一块（`Reward Background Get Reward` / `Noise`）另建一颗 `MenuDraw.Absorb` 吸收层
//     （`rayscan` 实读：两颗 `ray=1 en=1`，矩形 0,112.3 → 1920,1003.2 = 本窗 `GenRedR`）
//     ⇒ 原版点面板**什么都不发生**，只有面板**之外**那两条窄边才是「点外面关窗」。
using System.Collections.Generic;
using TMPro;                       // A1178：量 `GetPreferredValues()`（复刻原版 CSF）要 TMP 的类型
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`EnergySinglePlayerOnlyEventWindow` —— 单机能源限时活动窗（原版 `SinglePlayerOnlyEnergyEventWindow`）。</summary>
    public class EnergySinglePlayerOnlyEventWindow : GameWindow
    {
        // ============================================================ 分层（渲染队列，⛔ 不是 z）
        // 🔴 透明物体按「到相机的 3D 距离」排 ⇒ 同队列时谁盖谁不可控（CLAUDE.md §三）。
        // 本窗占 **3935–3945**（现读全工程 `3[0-9]{3}` 扫出来的一段空档；⛔ 别挪进别人的段）。
        /// <summary>`Menu Dark Background`（整屏压暗）—— 也是「吸收层」比对的基准档。</summary>
        public const int QShade = 3935;
        const int QBg1 = 3936;   // `Reward Background Get Reward`（红底）
        const int QBg2 = 3937;   // `Noise`
        const int QBg3 = 3938;   // `Menu Vignette`
        const int QArt = 3939;   // 面板底 / 图标 / 箱子那一层
        const int QArt1 = 3940;  // 压在 `QArt` 上的那一层（返回钮 / 高亮框 / 白底按钮 / 进度填充）
        public const int QText = 3941;
        /// <summary>本窗**内容命中区的最低档**（`Absorb` 取 `QHit − 1` ⇒ 3944，严格高于 `QShade`）。</summary>
        public const int QHit = 3945;

        // ============================================================ 几何（= 现读的 `--relative` 值）
        // ⚠️ 读数口径：`menu_dump` 给的「绝对矩形」**已经乘过父链的 `m_LocalScale`**（A60⑤⑨ 起补的）
        //    ⇒ 这些 px 可以直接当画布坐标用。同族 `Shell/SkirmishEventWindow.cs` 当年还要自己写
        //    `ScoreRect`/`ProgressRect` 补两层换算，是因为那会儿用的是 `menu_rect.py`（**不套 scale**）；
        //    本窗**不需要**那些助手。
        // ⚠️ **自带 `m_LocalScale` 的节点**：表里「绝对矩形」= 布局框、「视觉框」= 布局框 × 自己的 scale。
        //    下面凡带 `Vis*` 的常量取的是**视觉框**（画出来的大小），中心与布局框相同。
        static readonly PxRect RootR      = new PxRect(0f, 0f, 1920f, 1080f);

        // `General Red Background` 那一族
        static readonly PxRect GenRedR    = new PxRect(0f, 112.30f, 1920f, 1003.15f);
        static readonly PxRect ShadeR     = new PxRect(-1327.30f, -755.91f, 3247.30f, 1816.45f);
        const float ShadeA = 0.773f;                    // 实读 `m_Color = (0,0,0,0.773)`

        // `Image Mask` / `Event Image`
        static readonly PxRect MaskR      = new PxRect(425.71f, 116.92f, 1494.29f, 997.67f);
        static readonly PxRect EventImgR  = new PxRect(425.71f, 23.01f, 1494.29f, 1091.58f);

        // `Reward Progress Panel` 与记分条
        static readonly PxRect RPPR       = new PxRect(1315.79f, 133.93f, 1920f, 947.29f);
        const float SBarVisW = 378.29f, SBarVisH = 494.25f;   // `Scoring Bar Event Score Info` × scl 1.32
        const float SBarCx = 1617.90f, SBarCy = 604.285f;
        const float ProgVisW = 87.23f, ProgVisH = 475.84f;    // `Progress Bar` × scl 0.8696
        const float ProgCx = 1617.90f, ProgCy = 604.54f;

        // ============================================================ 🔴 A1197：缩放子树里的**字号**刻度积
        // **上面那些 `Vis*` 常量烘的是【框】；这一组烘的是【字号】—— 同一件事的两半，⛔ 别只做一半。**
        // 判据（为什么字号也要乘）—— 原版 TMP 的自适应**在【本地单位】里二分**，不是屏上：
        //   · `Unity 包源码 com.unity.ugui@27635d171b1a/Runtime/TMP/TextMeshProUGUI.cs:2333-2345`
        //     `ComputeMarginSize()` 里 `Rect rect = m_rectTransform.rect;` —— `RectTransform.rect` 是**局部**尺寸，
        //     不含任何祖先缩放；`m_marginWidth = rect.width - m_margin.x - m_margin.z`。
        //   · 二分两端都拿 `m_fontSize`（本地字号）与那个**局部** `marginWidth` 比：
        //     缩 = `:3424-3428` · 放 = `:4488-4502`。
        //   ⇒ 祖先缩放在「局部 → 屏」那一步把 rect 与字形**一起**乘，**改不了 fit 的结果**；
        //     而 prefab 里序列化的是**未缩放的** `m_fontSize` ⇒ 原版**屏上**字号 = 本地字号 × 祖先刻度积。
        //   ⚠️ 这份反编译 `d:/2/tools/decomp_full/` 里**没有 TMP**（只覆盖 `Assembly-CSharp`；`grep -rl
        //     "enableAutoSizing"` 零命中）⇒ 判据取的是**工程自带的包源码**（上面那条路径）。
        // 我们为什么不跟着祖先走：我们的 `Label` **不在**那条缩放子树里（父件只有窗根，刻度 1.0），
        //   而传进去的 `fontPx` 就是**画布 px**（`MenuDraw.TextCore` → `Label.SetGlyphHeight(LayoutSpace.Px(fontPx))`，
        //   `SetAutoFitBox` 的 `minPx/maxPx` 同为画布 px）⇒ **原版屏上的那个字号，只能自己乘出来。**
        //   ⇒ 框不动（已经是屏值）、四格字号（标称 + 自适应上/下限 + 基准）**一起乘**（乘的量纲对得上：
        //     `fontSizeMax = cur × (maxPx / nomPx)` 在 `nomPx` 也乘了 s 之后正好等比放大 s 倍）。
        // 先例（同一条规矩、照它的形状抄）：`Shell/MissionsTab.cs` 的 `FS()` +
        //   `Shell/DailyStreakPopup.cs` 的 `BuildEntry`（「整组的 scl 已经烘进矩形里，但 TMP 的 `m_fontSize`
        //   是**未缩放**的值 ⇒ 字号要自己乘」）。
        // ⚠️ 同一份子 prefab 在**别的窗**里刻度不同（`SkirmishModeEventWindow` / `Two Sides Event Window`
        //   的 `Scoring Bar Event Score Info` 是 **1.2563**，不是 1.32）⇒ 🔴 **逐窗取值、⛔ 别一刀切**。
        // 出处（现读）：`python -I d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all
        //   "EnergySinglePlayerOnlyEventWindow" --depth 14 --relative --no-sprite` 的行末 `⇲ls=`，
        //   并用 `d:/4/_tmp_view/r4scratch/chain2.py` 逐层读 `m_LocalScale`。链子逐颗（父 ← 子）：
        //   `Scoring Bar Event Score Info`(**1.3200000524520874**) ← `Score Levels`(1.0)
        //       ← `Score Bar Line Level k`(1.0) ← `Skull`(**0.8695654273033142**) ← `Score`(1.0)  ⇒ 积 1.1478264
        //   `Scoring Bar Event Score Info`(1.32) ← `Generic Simplified UI Button`(1.0) ← `Button Text`(1.0) ⇒ 积 1.32
        //   ⚠️ 那个 0.8696 长在 **`Skull`** 上，而 `Score` 是 `Skull` 的**子件** ——
        //     ⛔ 不是长在 `Score Bar Line Level k` 上（`资料/普查产出_第十会话/P8_A1190.md` §6·2 记错了方向）。
        /// <summary>`Scoring Bar Event Score Info` 自己那一颗的刻度（= 它**所有**后代的公共因子）。</summary>
        const float SBarFontScale = 1.32f;
        /// <summary>`Skull` 自己那一颗的刻度（`Score` 那 5 颗比别的后代**多**乘这一层）。</summary>
        const float SkullFontScale = 0.8695654273033142f;
        /// <summary>`Score` 那 5 颗的祖先刻度积 = `1.1478264…`（`SBarFontScale × SkullFontScale`）。</summary>
        const float ScoreFontScale = SBarFontScale * SkullFontScale;
        // `Progress Bar` 的两个子件（现读框**已经含过**那两层 scale）
        const float ProgBgCx = 1617.90f, ProgBgCy = 604.54f, ProgBgW = 50.16f, ProgBgH = 547.21f;
        const float FillX1 = 1599.06f, FillX2 = 1635.99f;     // `Fill Area` 的横向范围
        const float FillCy = 603.18f, FillAreaH = 494.26f;    // `Fill Area` 中心 y / 高
        // `Fill` 现读 **0.00 × 5.74**、框 1599.06,844.57→1599.06,850.31（画布 y 向下 ⇒ **844.57 是上沿、
        // 850.31 是下沿**）。🔴 塌的是**宽**（原版 `Slider` 按 `anchorMax.x = fillAmount` 缩 ⇒ 它那一档
        // `m_Direction` 是横向那一族）⇒ 填充**向右长**。（2026-10-13 第一版把 850.31 当成了上沿、
        // 整条低了 5.74px —— 自查抓的。）
        const float FillBarH = 5.74f, FillTopY = 844.57f;
        // `Score Levels`（VLG **reverse=1** · spacing 0 · pad 0 · align MiddleCenter · expandH=1）
        static readonly PxRect LevelsR    = new PxRect(1551.90f, 400.15f, 1683.90f, 707.74f);
        const float LvlX1 = 1422.79f, LvlX2 = 1813.00f;       // 每行那一档的框（**比容器宽** —— 原版就这么溢）
        const float LvlTop0 = 719.49f, LvlRowH = 79.84f;      // `Level 1` 的顶 = 719.49；往上每档 −79.836
        /// 步进 = 行高 60.482 × 这一族的 scl 1.32 = **79.836**（逐位可验：719.49 − 4×79.836 = 400.146
        /// ≈ 现读 `Level 5` 顶 **400.15**）。⚠️ 取 79.83 时第 5 档会差 **0.025px** —— 对账脚本抓出来的。
        const float LvlStep = 79.836f;
        const float CrateHiCx = 1708.32f, CrateHiCy = 757.695f, CrateHiW = 158.51f, CrateHiH = 141.24f;
        const float ChestCx   = 1710.56f, ChestCy   = 759.41f,  ChestW   = 104.49f, ChestH   = 93.20f;
        const float SkullCx   = 1556.645f, SkullCy  = 759.41f,  SkullSide = 67.78f;
        const float ScoreCx   = 1449.13f, ScoreCy   = 759.41f,  ScoreW   = 133.32f, ScoreH   = 57.39f;
        static readonly string[] CrateArt =
        { "40k_Crate_Tier1_Iron", "40k_Crate_Tier2_Copper", "40k_Crate_Tier3_Silver",
          "40k_Crate_Tier4_Gold", "40k_Crate_Tier5_Warp" };
        // `Collect` 钮
        static readonly PxRect CollectR   = new PxRect(1467.00f, 864.55f, 1768.79f, 955.44f);
        static readonly PxRect CollectTxR = new PxRect(1477.47f, 873.40f, 1758.35f, 946.57f);

        // 右列其余几栏
        static readonly PxRect RTileR     = new PxRect(1320.49f, 151.25f, 1915.30f, 205.94f);
        static readonly PxRect RHelpR     = new PxRect(1320.49f, 202.93f, 1915.30f, 303.92f);
        static readonly PxRect PVicR      = new PxRect(1320.49f, 227.39f, 1915.30f, 317.83f);
        static readonly PxRect VBgR       = new PxRect(1320.49f, 227.39f, 1875.18f, 317.83f);
        const float VTitleX2 = 1574.60f, VTitleY1 = 235.92f, VTitleY2 = 309.30f;   // 现读框**宽 0**
        static readonly PxRect VSkullR    = new PxRect(1581.20f, 235.92f, 1654.59f, 309.30f);
        const float VTotalX1 = 1661.19f;                                            // 现读框**宽 0**
        static readonly PxRect PLayerVicR = new PxRect(1567.90f, 490.61f, 1667.90f, 590.61f);

        // 表头
        static readonly PxRect HdrR       = new PxRect(0f, 41.02f, 550.00f, 150.57f);
        static readonly PxRect HdrBgR     = new PxRect(0f, 41.02f, 626.16f, 156.38f);
        static readonly PxRect TitleR     = new PxRect(155.00f, 57.37f, 565.16f, 140.03f);
        static readonly PxRect HdrBg2R    = new PxRect(-462.10f, 41.02f, 87.90f, 156.38f);
        static readonly PxRect BackR      = new PxRect(-24.40f, 43.04f, 143.48f, 154.36f);

        // 倒计时
        static readonly PxRect TimerR     = new PxRect(648.33f, 1008.55f, 1271.67f, 1069.45f);
        static readonly PxRect TimerIcR   = new PxRect(938.05f, 1019.55f, 981.95f, 1063.45f);
        const float TimerTxX1 = 981.95f, TimerTxY1 = 1013.55f, TimerTxY2 = 1069.45f;   // 现读框**宽 0**

        // 左列说明
        static readonly PxRect InsR       = new PxRect(24.15f, 208.65f, 680.75f, 645.23f);
        static readonly PxRect InsScoreR  = new PxRect(133.49f, 239.99f, 680.75f, 395.81f);
        static readonly PxRect InsEnergyR = new PxRect(133.49f, 422.19f, 680.75f, 590.32f);
        static readonly PxRect InsIconScR = new PxRect(24.15f, 239.98f, 118.64f, 344.50f);
        static readonly PxRect InsIconEnR = new PxRect(24.15f, 422.19f, 115.74f, 513.78f);

        // 阵营选择
        static readonly PxRect AspR       = new PxRect(0f, 606.69f, 680.75f, 903.67f);
        static readonly PxRect FacTitleR  = new PxRect(37.77f, 606.69f, 601.38f, 661.38f);
        static readonly PxRect ArmySelR   = new PxRect(37.77f, 666.69f, 642.99f, 803.20f);
        // 🔴 `Army Content`（与它的模板子件 `ArmyItemReference`）**不是视口那么宽** ——
        //    现读是 **136.50 × 136.50**（`ContentSizeFitter(h:PreferredSize)` 撑出来的内容宽）。
        //    ⛔ 第一次抄成了视口框（642.99 宽），是**对账脚本抓出来的**。
        static readonly PxRect ArmyContentR = new PxRect(37.77f, 666.69f, 174.27f, 803.20f);
        static readonly PxRect FactionInsR= new PxRect(37.77f, 813.68f, 642.99f, 895.16f);

        // ============================================================ 素材名（九宫 / 原尺寸照实读）
        const string ArtRedBg    = "40k_general_popup_simple_red";    // 21×81 · 九宫 (0,26,0,26) · Sliced
        const string ArtNoise    = "UI_Dirt_And_Noise_skratches";     // 1024² · Tiled · ppuMul 1.77
        const string ArtMulligan = "UI_Button_Mulligan";              // 410×124 · 九宫 (333,96,333,96)
        const string ArtClock    = "WF_icon_clock";                   // 64×64 · Simple
        const string ArtHdrBg    = "WF_Campaign_Info_Background";     // 740×167 · 九宫 (335,0,395,0) · Sliced
        const string ArtBack     = "UI_Button_Menu_Back";             // 187×124 · Simple
        const string ArtMiniBar  = "MiniBar_01";                      // 59×648 · Simple
        const string ArtCrateHi  = "Crate_Border_Highlight";          // 128² · Simple fillCenter=0
        const string ArtSkull    = "Rank_Skull";                      // 128² · Simple
        const string ArtNametag  = "40k_main_bt_nametag";             // 109×41 · Sliced
        const string ArtEnergy   = "40k_topmarquee_currency_energy";  // 90×90 · Simple（⚠️ 本仓 Resources 里**没有**）
        static readonly Vector4 RedBorder  = new Vector4(0f, 26f, 0f, 26f);
        // ⚠️ `HdrBorder`（= 顶栏那张 `WF_Campaign_Info_Background` 的九宫）2026-10-17（A866）已随窗头收口
        //    一并收进 `WindowHeader.PlateBorder`，本窗不再留第二份。
        static readonly Vector4 MullBorder = new Vector4(333f, 96f, 333f, 96f);
        static readonly Color NoiseTint   = new Color(0.311f, 0.127f, 0f, 0.718f);   // 实读
        static readonly Color VigTint     = new Color(0f, 0f, 0f, 0.58f);            // 实读
        static readonly Color FillTint    = new Color(1f, 0.228f, 0.0142f, 1f);      // 实读
        static readonly Color CrateHiTint = new Color(0.98f, 0.801f, 0.0588f, 1f);   // 实读

        // ============================================================ 建出来的东西（自检用）
        /// <summary>最后开的那一扇（同族 `LiveOpsEventWindow.LastOpened` 的形状）。</summary>
        public static EnergySinglePlayerOnlyEventWindow LastOpened;

        readonly List<Transform> _levels = new List<Transform>();
        /// <summary>本窗**取不到的图**（自检读口 —— 非空 = 有件根本没画）。
        /// ⚠️ `MainMenuSubmenuWindow` 那个同名成员**不在我们这条继承链上**（`GameWindow` 没有它）
        /// ⇒ 同族三扇（`GenericOptionsPanel` / `PurchasePremiumWindow` / `RankedRewardEventWindow`）各自
        /// 声明一份，本件照办（**口径同源**：`Art()` 记账 / `Build()` 开头清空）。</summary>
        readonly List<string> _missArt = new List<string>();
        public List<string> MissingArt { get { return _missArt; } }
        Transform _root, _mask, _armySelector, _armyContent, _fillArea, _fill;
        Transform _collect, _collectBg, _back, _backBg, _title, _victoriesTitle, _victoriesTotal, _timerText;
        Label _titleLabel;
        bool _warned;

        public List<Transform> LevelRows { get { return _levels; } }
        public Transform Root { get { return _root; } }
        public Transform ImageMask { get { return _mask; } }
        public Transform ArmySelector { get { return _armySelector; } }
        public Transform ArmyContent { get { return _armyContent; } }
        public Transform FillArea { get { return _fillArea; } }
        public Transform Fill { get { return _fill; } }
        public Transform CollectButton { get { return _collect; } }
        public Transform CollectBg { get { return _collectBg; } }
        public Transform BackButton { get { return _back; } }
        public Transform BackBg { get { return _backBg; } }
        public Transform WindowTitle { get { return _title; } }
        public Transform VictoriesTitle { get { return _victoriesTitle; } }
        public Transform TotalVictories { get { return _victoriesTotal; } }
        public Transform TimerText { get { return _timerText; } }

        // ============================================================ 开
        public static EnergySinglePlayerOnlyEventWindow Create(WindowsManager mgr)
        {
            var go = new GameObject("EnergySinglePlayerOnlyEventWindow");   // 节点名 = prefab 根名
            var win = go.AddComponent<EnergySinglePlayerOnlyEventWindow>();
            win.type = WindowType.Fullscreen;              // 🔴 MB 实读 `type = 0`（**Fullscreen**，不是 Popup）
            win.placement = WindowsPlacement.Popup;        // MB 实读 `windowsPlacement = 15`
            win.closeOnEsc = false;                        // 🔴 MB 实读 `closeOnESC = **0**`（同族两扇是 1）
            win.extraScaleSmallScreen = 1f;                // MB 实读 1.0（= 不覆盖）
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[Energy] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();
            if (_warned) return;
            _warned = true;
            Debug.Log("[Energy] 本窗的**活动数据全在远端**（`SinglePlayerScoringEvent` 是 LiveOps 事件）："
                    + "5 档分数 / 箱子开关 / 倒计时截止时刻 / `Total Victories` 都是**运行时灌**的 "
                    + "⇒ 我们照 **prefab 出厂原文**画（`1256` / `751` / `Termina en: 23d 5h` / 闭箱图），⛔ 不编数字。");
            Debug.Log("[Energy] `Energy Instructions` 里那个 **`{0}` 原样保留** —— 替换值来自 **LiveOps Config**"
                    + "（远端）：`ShowCurrentEnergyRegenText__OnEnable.c` 逐句可读（`ConfigManager.GetConfig(...)` "
                    + "+0x20 那个整数 + `String.Format(译文, 它, 时长)`）。本地没有那份 Config ⇒ 照字段抄。");
            Debug.Log("[Energy] 关窗有**两条**路（照原版，2026-10-13 A795 订正）："
                    + "① 表头那颗返回钮 ② **点压暗层** —— 原版 `SinglePlayerOnlyEnergyEventWindow__OnEnable.c:13-18` "
                    + "给 `backgroundCloseButton.onClick` 挂了监听，挂的与表头那颗是**同一个 delegate** "
                    + "⇒ 两条路通向同一个 `Close()`。`closeOnESC = 0` ⇒ ESC 不关。"
                    + "⚠️ 但**面板那一块**（`Reward Background Get Reward` / `Noise`，`rayscan` 实读 `ray=1`）"
                    + "被吸收层吃掉、**什么都不做** ⇒ 在那儿点不出关窗（只有面板之外的窄边才关）。");
            Debug.Log("[Energy] 🔴 A1178：`Victories title` / `Timer` 两颗的**框宽**已按原版的"
                    + " `ContentSizeFitterMinMax(h: PreferredSize)` 复刻 —— 建树期用 `Label` 里那颗 TMP 的"
                    + " `GetPreferredValues()` 量出**我方字体**的 preferred width 再写回框"
                    + "（⛔ 不是照抄原版 `Pragati-Regular SDF` 算出的 176.98 / 248.44）。"
                    + "⚠️ **已知差异（如实出声）**：原版那个 fitter 是**运行期活的** —— 换语言 / 倒计时文案一变，"
                    + "框就跟着变；我们的框是**建树期量一次**写死的 ⇒ 文案在运行期变长时**原版撑框、我们缩字**"
                    + "（本窗的文案目前全走 prefab 出厂原文、没有换语言的路，所以今天这条够不到）。"
                    + "⚠️ 另注：**换字体资产会跟着变**（量就发生在建树那一刻、用的就是当时那个字体资产）；"
                    + "「不会自动变」的只有**运行期换语言**这一条。判据 → "
                    + "`资料/普查产出_第十会话/R4_布局刻度族查实.md` §1·1/§1·3/§1·4。");
        }

        // ============================================================ 建
        void Build()
        {
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            _levels.Clear();

            _root = transform;
            BuildBackdrop(transform);
            BuildImageMask(transform);
            BuildRewardPanel(transform);
            BuildHeader(transform);
            BuildTimer(transform);
            BuildInstructions(transform);
            BuildArmySelector(transform);

            // ---- 两颗命中节点（判据全文 → 文件头那一段，2026-10-13 A795 订正）------------------
            // ① **压暗层那颗（含「点它关窗」）** —— 原版 `__OnEnable.c:13-18` 给
            //    `backgroundCloseButton.onClick` 挂的监听与表头返回钮是**同一个 delegate**
            //    ⇒ 点压暗层 = 点返回钮 = `Close()`。
            //    形状照同族范本 `Shell/RankedEventWindow.cs:64` / `Shell/SkirmishEventWindow.cs:74`：
            //      `ShadeHit(dark, rect, qShade, qContentMin, onClick, name)`
            //    ⚠️ `name` 在**最后一个**位置 —— 与 `Absorb(parent, name, rect, …)` 的**参数顺序不一样**，
            //       别照 `Absorb` 那一行抄（照抄会把 `name` 当 rect 传、编不过）。
            //    ⚠️ `qShade` 传的是**压暗层自己那一档**（`QShade` = 3935，与本窗视觉压暗层同档）：命中区
            //       自己带 `ImageQuad`，与那块**非命中**的视觉 quad 同档无害；关键是它**严格低于**内容命中区档。
            // ② **面板底图的吸收层** —— 原版 `Reward Background Get Reward` 与 `Noise`
            //    （`rayscan` 实读两颗都 `ray=1 en=1`，矩形 0,112.3 → 1920,1003.2 = 本窗 `GenRedR`）
            //    把面板那一整块**吃掉、什么都不做**（父链上没有点击处理器）
            //    ⇒ 只有面板**之外**的上下两条窄边才落得到 ① 上。
            //    🔴 `Absorb` 的档由它自己算 = `QHit − 1`(3944) ⇒ 严格夹在 `QShade`(3935) 与 `QHit`(3945) 之间
            //      （同档/越档的症状是「点窗内空白处**有时**会关窗」，且**静默**）。
            MenuDraw.ShadeHit(transform, RootR, QShade, QHit, () => Close(), "BackgroundHit");
            MenuDraw.Absorb(transform, "AbsorbHit", GenRedR, QShade, QHit);
        }

        // ------------------------------------------------------------ 背景那一族
        void BuildBackdrop(Transform root)
        {
            var gen = MenuDraw.Node(root, "General Red Background", GenRedR);
            // ① 整屏压暗（rect 比屏幕大）；原版这颗同时挂 `BackgroundOverDrawController`
            MenuDraw.Rect(gen, CardArt.Solid(), ShadeR, "Menu Dark Background", QShade,
                          new Color(0f, 0f, 0f, ShadeA));
            // ② 红底（`40k_general_popup_simple red` 21×81 · 九宫 0,26,0,26 · Sliced）
            MenuDraw.Nine(gen, Art(ArtRedBg), GenRedR, RedBorder, 21f, 81f, QBg1, null, true,
                          "Reward Background Get Reward");
            // ③ `Noise`（`UI Dirt And Noise skratches` 1024² · **Tiled** · `m_PixelsPerUnitMultiplier = 1.77`）
            //    格边像素 = 贴图 1024 ÷ ppuMul 1.77 = **578.53**（我们的 `Tiled` 只有一个格边形参，
            //    原版是「按 ppuMul 反算 UV 密度」⇒ 等价换算，出处 = 那一格实读的 `ppuMul=1.77`）。
            MenuDraw.Tiled(gen, Art(ArtNoise), GenRedR, 1024f / 1.77f, QBg2, "Noise", NoiseTint);
            // ④ `Menu Vignette`（`m_Sprite = 0` + `type = Sliced` ⇒ 纯色块）
            MenuDraw.Rect(gen, CardArt.Solid(), GenRedR, "Menu Vignette", QBg3, VigTint);
        }

        // ------------------------------------------------------------ `Image Mask`（原版 `RectMask2D`）
        void BuildImageMask(Transform root)
        {
            // 原版这一颗是**真掩码**（`m_Padding = 0` · `m_Softness = 0`）⇒ 走 `ViewportClip` 节点
            // （本壳取裁切状态**唯一入口**，A198②；软边 (0,0) = 原版这一颗没有软边）。
            var vc = ViewportClip.Hang(root, "Image Mask", MaskR, Vector4.zero, Vector2Int.zero);
            _mask = vc != null ? vc.transform : null;
            // `Event Image`：原版 `m_Sprite` 出厂就是 **null**（运行时由 `eventImage` 字段灌）
            var host = _mask != null ? _mask : root;
            var n = MenuDraw.Node(host, "Event Image", EventImgR);
            Debug.Log("[Energy] `Event Image` **没画** —— 原版这颗 `Image.m_Sprite` 出厂就是 null"
                    + "（prefab 现读；由 `eventImage` 字段运行时灌）。节点照建、这一格空着。");
            if (n == null) { /* 只为对齐同族写法 */ }
        }

        // ------------------------------------------------------------ 右列 `Reward Progress Panel`
        void BuildRewardPanel(Transform root)
        {
            var rp = MenuDraw.Node(root, "Reward Progress Panel", RPPR);
            BuildScoreBar(rp);
            // `Reward Tile`：原版 hAlign = **Center**（现读 `对齐=Center/Midline`）⇒ 不调 `Align*`
            // 🔴 **2026-10-19（A1179）**：补 autosize 四格（原来没传 ⇒ 固定 45.87px）。
            //   判据 = 逐颗现读原版那一颗（`python -I d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all
            //   "EnergySinglePlayerOnlyEventWindow" --depth 8 --relative --no-sprite --no-layout`）：
            //   `Reward Tile` · `'Progression'` · 字号 **45.87** · 基准 **36.0** ·
            //   **`m_enableAutoSizing = 1` · `auto[18.0~45.869999…]`** · **折行 = 1** ·
            //   框 **594.81 × 54.68**（与本文件 `RTileR` 逐位同值）。
            //   ⚠️ 原版**折行 = 1** ⇒ 传 `wrapPx` 与它同档，⛔ 不要补 `SetWrapping(false)`。
            MenuDraw.Text(rp, RTileR, "Progression", Color.white, "Reward Tile", 45.87f, QText,
                          RTileR.W, 18f, 45.87f, 36f);
            // `Reward Help`：**出厂 act = F** ⇒ 建出来、**关着**（同族先例：`Banned card in deck`）
            var help = MenuDraw.TextBox(rp, RHelpR, "Win battles to progress in the event and unlock rewards.",
                                        Color.white, "Reward Help", 38f, 18f, QText, 38f, 36f);
            if (help != null) help.transform.gameObject.SetActive(false);
            BuildPlayerVictories(rp);
            // `PLayer Victories`（原版这个名字里 `P` 是大写 —— 照抄，⛔ 别顺手改成 `Player`）
            MenuDraw.Node(rp, "PLayer Victories", PLayerVicR);
        }

        /// <summary>`Scoring Bar Event Score Info`（`MilestoneScoreBar`）：竖条 + 5 档里程碑。
        /// 🔴 这一族整棵被 **1.32** 的 `localScale` 包着（`Scoring Bar Event Score Info` 自己那一颗）——
        ///    子件框在现读表里**已经乘过**它（行末 `⇲ls=1.32`）⇒ 子件照抄、**容器取视觉框**。
        ///    里面那层 `Progress Bar` 自己还有 **0.8696** ⇒ 它的容器也取视觉框，再往里两层现读值已含。</summary>
        void BuildScoreBar(Transform rp)
        {
            var bar = MenuDraw.Node(rp, "Scoring Bar Event Score Info",
                                    CenterRect(SBarCx, SBarCy, SBarVisW, SBarVisH));
            var pr = MenuDraw.Node(bar, "Progress Bar", CenterRect(ProgCx, ProgCy, ProgVisW, ProgVisH));
            //   `Background`（`MiniBar_01` 59×648 · Simple）
            MenuDraw.Rect(pr, Art(ArtMiniBar), CenterRect(ProgBgCx, ProgBgCy, ProgBgW, ProgBgH),
                          "Background", QArt);
            //   `Fill Area`（原版这一颗**只有 `RectTransform`、没有组件**）+ 它的孩子 `Fill`
            _fillArea = MenuDraw.Node(pr, "Fill Area", CenterRect((FillX1 + FillX2) * 0.5f, FillCy,
                                                                  FillX2 - FillX1, FillAreaH));
            //   `Fill`：原版现读 **0.00 × 5.74** —— `Slider` 的填充在 `normalizedValue = 0` 时归零
            //   ⇒ **出厂那一档 = 一条 0 宽的填充条**（= 看不见，但节点在）。
            //   `SetProgress(v)` 是它的**第二态**（自检用；原版对应 `Slider.normalizedValue`）。
            _fill = MenuDraw.Node(_fillArea, "Fill", new PxRect(FillX1, FillTopY, FillX1, FillTopY + FillBarH));
            DrawFill(0f);

            BuildMilestones(bar);
            // `Generic Simplified UI Button`（`Collect` · `UI_Button_Mulligan` 九宫 + `preserveAspect`）
            _collect = MenuDraw.Node(bar, "Generic Simplified UI Button", CollectR);
            var cb = MenuDraw.Rect(_collect, Art(ArtMulligan), CollectR, "Bg", QArt1, null, true);
            _collectBg = cb != null ? cb.transform : null;
            var ct = MenuDraw.Text(_collect, CollectTxR, "Collect", Color.white, "Button Text",
                                   55f * SBarFontScale, QText, CollectTxR.W,
                                   10f * SBarFontScale, 55f * SBarFontScale, 12f * SBarFontScale);
            // 🔴 **2026-10-19（A1190）**：这一颗原来**没传 autosize 实参** ⇒ 固定 55px。
            //   判据 = 逐颗现读原版那一颗（`工具/menu_dump.py bundle_menus_assets_all
            //   "EnergySinglePlayerOnlyEventWindow" --depth 10 --md`）：
            //   `…/Generic Simplified UI Button/Button Text` = `'Collect'` · 字号 **55.0** ·
            //   基准 **12.0** · **`auto[10.0~55.0]`**（`m_enableAutoSizing = 1`）·
            //   对齐 `Center/Capline` · 🔴 **折行 = 0** · 框 **280.88 × 73.17**（= `CollectTxR` 逐位同值）。
            //   ⚠️ **折行 0**：`SetAutoFitBox` 内部那句 `SetWrapWidth` 会**无条件**把模式开成 `Normal`
            //   ⇒ 紧跟着关掉（同族先例 = `Shell/AlliancePanelWindow.cs:373` 的 `lb.SetWrapping(false)`）。
            //   ⚠️ 关得越早越好（`SetWrapping` 会推版面）—— 本颗后面**没有** `Align*`，所以排在这里就够。
            // 🔴 **2026-10-19（A1197）**：四格字号 **全部 × `SBarFontScale`（1.32）** —— 本颗在
            //   `Scoring Bar Event Score Info`（`m_LocalScale = 1.32`）那棵子树里，而原版 prefab 里的
            //   `m_fontSize/m_fontSizeMin/m_fontSizeMax/m_fontSizeBase`（55/10/55/12）是**未缩放**的值
            //   ⇒ 原版屏上字号 = 55 × 1.32 = **72.6**。我们写的 55 相对框（`CollectTxR`，已是屏值）小 **24.2%**。
            //   判据全文 → 本文件上面那一段「A1197：缩放子树里的**字号**刻度积」。⛔ **框不动**。
            if (ct != null) ct.SetWrapping(false);
            // 原版这颗钮 → `CollectClicked()`：要向 LiveOps 领奖 ⇒ **要服务器**。
            //（本工程边界③：入口照做、点了如实说，⛔ 不静默。）
            MenuDraw.Hit(_collect, "CollectHit", CollectR, QHit, OnCollectClick, cb, ArtMulligan,
                         ArtMulligan + "_hover", ArtMulligan + "_Pressed");
        }

        void OnCollectClick()
        {
            Debug.Log("[Energy] `Collect` 点了 ⇒ **如实提示「暂无服务器」**"
                    + "（原版 `SinglePlayerOnlyEnergyEventWindow__CollectClicked.c` 要走 LiveOps 领奖；"
                    + "本地没有活动、也没有服务器 —— 照边界③不静默）。");
            if (WindowsManager.Instance != null)
                WindowsManager.Instance.ShowPopUp("Event rewards are not available offline.", "OK", null);
        }

        /// <summary>进度条那一格（`Fill`）的**两态**：`v = 0`（出厂）与 `v > 0`。
        /// 原版是 `Slider.normalizedValue` + `Image.type = Sliced` 把填充条拉长/压短；
        /// 我们这套没有 uGUI 的 `Slider` ⇒ 直接按 `v` 重画那一颗 quad（⛔ 不改别的节点）。</summary>
        public void SetProgress(float v)
        {
            v = Mathf.Clamp01(v);
            DrawFill(v);
        }

        /// <summary>画（或重画）`Fill`：高度恒 `FillBarH`、**从左沿向右长**。
        /// 判据：原版 `Fill` 现读 **0.00 宽 × 5.74 高** ⇒ 塌的是**宽**，而 uGUI 的 `Slider` 是拿
        /// `anchorMax.x = fillAmount` 缩那一格的（⇒ 它那一档的 `m_Direction` 是横向那一族；
        /// ⚠️ **枚举值没有逐字段读**，见报告 §九·8）。`v = 0` ⇒ 宽 0（**不建 quad**）。</summary>
        void DrawFill(float v)
        {
            if (_fill == null) return;
            MenuDraw.ClearChildren(_fill);
            _fillProgress = v;
            float w = (FillX2 - FillX1) * v;
            if (w <= 0.01f)
            {
                // 出厂态：原版就是 0 宽 ⇒ **不建 quad**（不是「建了看不见」）；节点本身留着
                return;
            }
            MenuDraw.Rect(_fill, CardArt.Solid(),
                          new PxRect(FillX1, FillTopY, FillX1 + w, FillTopY + FillBarH), "FillBar", QArt1,
                          FillTint);
        }
        float _fillProgress;
        /// <summary>当前进度（自检用；0 = 出厂那一档）。</summary>
        public float FillProgress { get { return _fillProgress; } }

        /// <summary>5 档里程碑（`Score Bar Line Level 1..5`）。
        /// 🔴 **`Score Levels` 的 VLG 是 `reverse=1`** ⇒ 树序**最后一个**子件排在**最上**：
        ///    现读实测 `Level 5` 顶 **400.15** / `Level 1` 顶 **719.49**（画布 y 向下）。
        ///    ⛔ **别按「树序第一个 = 最上」摆** —— 那会把整条记分条上下镜像（而且断言只断「有几行」时**看不出来**）。
        ///    判据 = uGUI `HorizontalOrVerticalLayoutGroup.cs:152-155`（`startIndex = reverse ? Count−1 : 0` ·
        ///    `increment = reverse ? −1 : 1`）。</summary>
        void BuildMilestones(Transform bar)
        {
            var levels = MenuDraw.Node(bar, "Score Levels", LevelsR);
            for (int i = 0; i < 5; i++)
            {
                // i = 树序下标（0 起 = `Level 1`）⇒ 它排在**最下**那一档；往上走一步就 −LvlStep
                float top = LvlTop0 - i * LvlStep;
                var row = MenuDraw.Node(levels, "Score Bar Line Level " + (i + 1),
                                        new PxRect(LvlX1, top, LvlX2, top + LvlRowH));
                _levels.Add(row);
                float dy = -i * LvlStep;      // `Level 1` 那一档 = 基准，往上每档减 LvlStep
                // `Highlight Crate`（自带 scl **0.796** ⇒ 框取**视觉尺寸**）
                //   原版 tint = **不透明金** `(0.98,0.801,0.0588,1)`；运行时只在「这一档可领」时点亮。
                MenuDraw.Rect(row, Art(ArtCrateHi),
                              CenterRect(CrateHiCx, CrateHiCy + dy, CrateHiW, CrateHiH),
                              "Highlight Crate", QArt1, CrateHiTint);
                // `Chest`（自带 scl **0.8696**）· `Skull`（同）—— 中心随行一起走
                MenuDraw.Rect(row, Art(CrateArt[i]), CenterRect(ChestCx, ChestCy + dy, ChestW, ChestH),
                              "Chest", QArt);
                MenuDraw.Rect(row, Art(ArtSkull), CenterRect(SkullCx, SkullCy + dy, SkullSide, SkullSide),
                              "Skull", QArt);
                // `Score`：原版 hAlign = **Right**（现读 `对齐=Right/Midline · 折行=1`）⇒ 右对齐到那一格
                var sr = CenterRect(ScoreCx, ScoreCy + dy, ScoreW, ScoreH);
                // 🔴 **2026-10-19（A1190）**：这一处原来**没传 autosize 实参** ⇒ 固定 48px。
                //   判据 = 逐颗现读原版那 5 颗（`工具/menu_dump.py bundle_menus_assets_all
                //   "EnergySinglePlayerOnlyEventWindow" --depth 10 --md` ⇒
                //   `…/Score Bar Line Level 1..5/Score`）：**5 颗逐值完全一致** ——
                //   字号 **48.0** · 基准 **36.0** · **`auto[18.0~48.0]`**（`m_enableAutoSizing = 1`）·
                //   对齐 `Right/Midline` · **折行 = 1** · 框 **133.32 × 57.39**（= `ScoreW`/`ScoreH` 逐位同值）
                //   ⇒ 一行建 5 颗、**5 颗同值**，一套实参就够（⛔ 不是「原版 5 颗不同值」那一族）。
                //   ⚠️ 折行 1 ⇒ ⛔ 不补 `SetWrapping(false)`。
                var sc = MenuDraw.Text(row, sr, "1256", Color.white, "Score",
                                       48f * ScoreFontScale, QText, sr.W,
                                       18f * ScoreFontScale, 48f * ScoreFontScale, 36f * ScoreFontScale);
                // 🔴 **2026-10-19（A1197）**：四格字号 **全部 × `ScoreFontScale`（1.1478264）** ——
                //   这颗的祖先链是 `Scoring Bar Event Score Info`(1.32) ← `Skull`(**0.86956543**) ← `Score`，
                //   而原版 prefab 里的 48/18/48/36 是**未缩放**的值 ⇒ 原版屏上字号 = 48 × 1.1478264 = **55.096**。
                //   我们原来直接传 48 ⇒ 相对框（`ScoreW` = 133.32，**已是屏值**）小 **12.9%**。
                //   判据全文（含「TMP 在本地单位里二分」那两条包源码行号）→ 上面常量声明处那一段。
                //   ⚠️ 那颗 `0.8696` 长在 **`Skull`** 身上、`Score` 是 `Skull` 的**子件**（现读链，⛔ 别记到行节点头上）。
                //   ⛔ 框不动（`sr` = `CenterRect(…, ScoreW, ScoreH)`，本来就是乘过刻度的屏值）。
                MenuDraw.AlignRight(sc, sr);
            }
            Debug.Log("[Energy] 5 行 `Score` 照的是 **prefab 出厂原文 `1256`**、`Highlight Crate` 照出厂"
                    + "（原版是**不透明金**）—— 原版 `MilestoneScoreBar.Initialize(currentScore, milestones)` "
                    + "运行时按活动分数改这两件，本地没有活动数据（⛔ 不编数字）。");
        }

        // ------------------------------------------------------------ 右列 `Player victories` 那三行
        void BuildPlayerVictories(Transform rp)
        {
            var pv = MenuDraw.Node(rp, "Player victories", PVicR);
            MenuDraw.Nine(pv, Art(ArtNametag), VBgR, Vector4.zero, 109f, 41f, QArt, null, true,
                          "Victories Background");
            // 🔴 这两格的现读框**宽 = 0**：原版挂 `ContentSizeFitterMinMax(h:PreferredSize)`（工具标注
            //   `⚙CSF h:PreferredSize？` = **算不出**，要 Unity 的字体度量）。
            //   🔴 **2026-10-10（A1178）这一档【已复刻】**：运行时框宽 = **文字的 preferred width**，
            //   由下面的 `PreferredWidthPx` 在**建树期量一次**写回（见那个助手的判据与出处）；
            //   ⛔ 量的是**我方字体**的宽度，**不是**原版用 `Pragati-Regular SDF` 算出来的那个数。
            //   ⚠️ 仍然**是我们摆的**只剩「**框的右沿**」这一个自由度：`VTitleX2` = 现读 **1574.60**
            //   （原版那一颗的 rect 宽 0 ⇒ 那个落点本身是 HLG 跑完的结果，见报告 §7 顺手发现①）。
            var vtR = new PxRect(VBgR.x1, VTitleY1, VTitleX2, VTitleY2);
            // 🔴 **2026-10-18（A1126 · A1 档）**：这一处原来**没传 autosize 实参** ⇒ 固定 53.5px。
            //   判据 = 逐颗现读原版那一颗（`工具/menu_dump.py bundle_menus_assets_all
            //   "EnergySinglePlayerOnlyEventWindow" --depth 6 --md`）：
            //   `…/PLayer Victories/Victories title` = `'Victories: '` · 字号 **53.5** · 基准 **36.0** ·
            //   **`auto[18.0~53.5]`**（`m_enableAutoSizing = 1`）· 对齐 `Right/Capline` · **折行 = 1**
            //   ⇒ 传 `wrapPx` 与原版同档（⛔ 不补 `SetWrapping(false)`）。
            //   ⚠️ 原版那颗**宽 = 0** + `ContentSizeFitterMinMax(h:PreferredSize)`（工具标注 `⚙CSF`）
            //   ⇒ 运行时框宽 = **文字的 preferred width**（A1178 已复刻，见上）。
            //   🔴 **2026-10-10 就地订正（A1178 · 铁律 5）**：本段原来写「我们这 254.11 若比原版 preferred 窄，
            //   开自适应会让字**比原版小一点**」——**方向反了**。现算（`R4_布局刻度族查实.md` §1·3）：
            //   `254.112 vs 176.98` = 我们**宽 44%** ⇒ 开自适应**不会缩字号**；两边的残差只在
            //   「运行期文案变长」那一档（原版**撑框**、我们**缩字**），见 `Open()` 里那句出声。
            var vt = MenuDraw.Text(pv, vtR, "Victories: ", Color.white, "Victories title", 53.5f, QText,
                                   vtR.W, 18f, 53.5f, 36f);
            // 🔴 **A1178**：量 preferred width ⇒ 把框收成它（= 原版 CSF 那一档）。
            //   生长方向 = **右沿不动**（`VTitleX2`）：原版这一颗 `H=Right`、我们下面也是 `AlignRight`
            //   ⇒ 框往左收，右对齐那一刀的落点不变（画面上零位移；变的是「框」这个结构本身）。
            //   ⚠️ `SetAutoFitBox` 会**再跑一次自适应**（原版 CSF 撑完框也要重排一次，语义同）；
            //   它的 `SetWrapWidth` 顺手把折行模式开成 `Normal` —— 本颗原版就是 **折行 1** ⇒ 不用还原。
            float vtW = PreferredWidthPx(vt, "Victories title");
            var vtFit = vtW > 0f ? new PxRect(VTitleX2 - vtW, VTitleY1, VTitleX2, VTitleY2) : vtR;
            if (vtW > 0f)
                vt.SetAutoFitBox(LayoutSpace.Px(vtFit.W), LayoutSpace.Px(vtFit.H), 18f, 53.5f, 36f);
            MenuDraw.AlignRight(vt, vtFit);
            _victoriesTitle = vt != null ? vt.transform : null;
            MenuDraw.Rect(pv, Art(ArtSkull), VSkullR, "Skull Victories", QArt);
            var ttR = new PxRect(VTotalX1, VTitleY1, VBgR.x2, VTitleY2);
            // 🔴 **2026-10-19（A1179）**：补 autosize 四格（原来没传 ⇒ 固定 77px）。
            //   判据 = 同一颗现读（同 `Reward Tile` 那条命令）：
            //   `…/PLayer Victories/Total Victories` · `'751'` · 字号 **77.0** · 基准 **36.0** ·
            //   **`m_enableAutoSizing = 1` · `auto[18.0~77.0]`** · 对齐 `Left/Midline` · **折行 = 1** ·
            //   原版框 **宽 0**（+ `ContentSizeFitterMinMax(h:PreferredSize)`，工具标注 `⚙CSF` = 算不出，
            //   运行期框宽 = 文字的 preferred width）⇒ 下面传的 `ttR.W` = **213.99 是我们摆的**
            //   （同上面 `Victories title` 那一段的处置与残差说明）。
            //   ⚠️ 原版**折行 = 1** ⇒ 传 `wrapPx` 与它同档，⛔ 不要补 `SetWrapping(false)`。
            var tt = MenuDraw.Text(pv, ttR, "751", Color.white, "Total Victories", 77f, QText,
                                   ttR.W, 18f, 77f, 36f);
            MenuDraw.AlignLeft(tt, ttR);
            _victoriesTotal = tt != null ? tt.transform : null;
        }

        // ------------------------------------------------------------ 表头
        /// <remarks>🔴 **2026-10-17（A866）：建法已收口到 `WindowHeader.WithBackButton`**
        /// （全工程 4 扇窗各抄一遍 ⇒ 收成一份）。本窗这一档的**差异**（逐格对照 →
        /// `资料/普查产出_1018/S5_A866窗头收口.md` §2）：
        /// · 标题走 **`FitBeforeSpacing`**（先自适应、后字距 —— 与 `TutorialModePopup` / `LiveOpsEventWindow`
        ///   那两扇的 `FitAfterSpacing` **相反**；两档静态收敛，照旧保留，⛔ 不替它选边）；
        /// · 🔴 **2026-10-18（三扇标题垂直档漏网）**：标题**多一档 `TitleVAlign = Capline`**（原文这里没列这一条 ⇒
        ///   A866 时本窗走的是 `Label` 出厂的 `Middle`，是**与原版不符**的，见 `BuildHeader` 里那段判据）；
        /// · 返回钮 = `QuadInOwnNode`（多一层具名节点 `Header Back Button`，图是它的子件 `Bg`）+ **有换图**；
        /// · 本窗要把建出来的四件存进字段（`_titleLabel` / `_title` / `_back` / `_backBg`）⇒ **接返回值**。</remarks>
        void BuildHeader(Transform root)
        {
            var p = WindowHeader.WithBackButton(root, new WindowHeader.Spec
            {
                RootRect = HdrR,
                PlateRect = HdrBgR,
                TitleRect = TitleR,
                // ⚠️ 本窗的字面量是 `Game Mode`（大写 M）—— 与另三扇的 `Game mode` **不同**，照抄原值。
                TitleText = "Game Mode",
                // 🔴 **顺序不能反**（同族 A475 的教训，判据已搬进 `WindowHeader.TitleFit`）：
                //   `SetAutoFitBox` / `SetCharSpacing` **都会改渲染宽度**，而 `AlignLeftOn` 是
                //   「量当时的 `WorldW` 再反推整块位置」。⚠️ 本窗是**先自适应、后字距** —— 照旧。
                TitleMode = WindowHeader.TitleFit.FitBeforeSpacing,
                // 照本窗原来的写法（`TitleR.W` / `TitleR.H`），⛔ 别改成 `TitleR.x2 - TitleR.x1`（可能差 1 ulp）。
                TitleFitW = TitleR.W, TitleFitH = TitleR.H,
                // 🔴 **2026-10-18（三扇标题垂直档漏网 · 铁律 11）**：补这一档 —— 原版 `Window Title` 那颗 TMP 是
                //   **`m_VerticalAlignment = 8192`（`Capline`）**（判据：亲读 `bundle_menus_assets_all` 里
                //   **8 颗 `Window Title` 逐颗现读、逐值相同**）；本窗此前没传 ⇒ 落到 `Label` 出厂的
                //   `Middle`，是**真差异**。写法照 `DailyStreakPopup` 那一处。
                //   ⚠️ 共件里 `SetVAlign` **排在 `AlignLeft` 之后**（纵向/横向互不干涉），照旧。
                TitleVAlign = Label.VAlign.Capline,
                WingRect = HdrBg2R,
                BackRect = BackR,
                BackButtonStyle = WindowHeader.BackStyle.QuadInOwnNode,
                BackKeepAspect = true,
                // 原版 `Header Back Button` → `CloseButtonClick()` → `Close()`
                BackSwapArt = ArtBack,
                PlateTex = Art(ArtHdrBg),
                BackTex = Art(ArtBack),
                QPlate = QArt, QWing = QArt1, QTitle = QText, QBack = QArt1, QHit = QHit,
                OnBack = () => Close(),
            });
            _titleLabel = p.Title;
            _title = p.Title != null ? p.Title.transform : null;
            _back = p.BackNode;
            _backBg = p.BackQuad != null ? p.BackQuad.transform : null;
        }

        // ------------------------------------------------------------ 倒计时
        void BuildTimer(Transform root)
        {
            var tm = MenuDraw.Node(root, "Timer", TimerR);
            MenuDraw.Rect(tm, Art(ArtClock), TimerIcR, "Timer Icon", QArt);
            // 那一格的现读框**宽 = 0**（`ContentSizeFitterMinMax(h:PreferredSize)` 算不出）。
            // 🔴 **2026-10-10（A1178）这一档【已复刻】**：运行时框宽 = **文字的 preferred width**，
            //   由下面的 `PreferredWidthPx` 在**建树期量一次**写回（⛔ 量我方字体，不是原版那个 248.44）。
            //   ⚠️ 仍然**是我们摆的**只剩两颗自由度：① 框的**左沿**（`TimerTxX1` = 图标右沿 981.95 ——
            //   原版那一颗由 HLG 算位置，见报告 §7 顺手发现②）；② 量之前的那个初值框
            //   （从图标右沿 981.95 撑到容器右沿 1271.67）。**文字照 prefab 出厂原文**。
            var txR = new PxRect(TimerTxX1, TimerTxY1, TimerR.x2, TimerTxY2);
            // 🔴 **2026-10-18（A1126 · A1 档）**：这一处原来**没传 autosize 实参** ⇒ 固定 38px。
            //   判据 = 逐颗现读原版那一颗（同本文件头那条命令）：`…/Timer/Timer`（TMP） = `'Termina en: 23d 5h'` ·
            //   字号 **38.0** · 基准 **38.0** · **`auto[10.0~38.0]`**（`m_enableAutoSizing = 1`）·
            //   对齐 `Center/Capline` · **折行 = 0**（`m_TextWrappingMode = 0`）· 原版 `m_SizeDelta = (0, 55.905)`。
            //   ⚠️ 原版**折行 = 0** ⇒ 传 `wrapPx` 会让 `SetWrapWidth` 把它**静默开成 `Normal(1)`**
            //   ⇒ 紧跟一句 `SetWrapping(false)` 还原（成对写法同 A404 / A205 / A34-F4 那一族）。
            //   ⚠️ 原版那颗**宽 = 0** + `ContentSizeFitterMinMax(h:PreferredSize)`（工具标注 `⚙CSF` = 算不出）
            //   ⇒ 运行时框宽 = **文字的 preferred width**（A1178 已复刻，见上面那一段）。
            //   🔴 **2026-10-10 就地订正（A1178 · 铁律 5）**：本段原来写「`txR` 的 289.72 是我们摆的 ⇒
            //   「原版框放不下才缩」这条边界我们复刻不了，残差如实记在 `W2_A1126A1.md` §五」——
            //   **框那一半已复刻**（不再是「复刻不了」）；而且现算（`R4_布局刻度族查实.md` §1·3）
            //   `289.72 vs 248.44` = 我们**宽 17%** ⇒ 用出厂文案时自适应**不会缩字**，
            //   残差只剩「运行期文案变长」那一档（原版**撑框**、我们**缩字**，见 `Open()` 里那句出声）。
            var tx = MenuDraw.Text(tm, txR, "Termina en: 23d 5h", Color.white, "Timer", 38f, QText,
                                   txR.W, 10f, 38f, 38f);
            if (tx != null) tx.SetWrapping(false);
            // 🔴 **A1178**：量 preferred width ⇒ 把框收成它（= 原版 CSF 那一档）。
            //   生长方向 = **左沿不动**（`TimerTxX1` = 图标右沿 981.95）：原版这一颗在
            //   `HorizontalLayoutGroup` 里排在**图标之后**、框自然往右长（组再整体居中）⇒
            //   我们这一档对应的就是「框的左沿 = 图标右沿、右沿跟着文字走」。
            //   ⚠️ 两条**我们摆的**自由度如实记：① 框的左沿（原版由 HLG 算，见报告 §7 顺手发现②）；
            //   ② 量之前那个初值框（`txR` 的右沿 = 容器右沿 1271.67）。⚠️ 这条**不是**「零位移」——
            //   框一收/一放，TMP 的自适应就会按新框重排（原本被压小的字会回到 38px），
            //   而文字的定位口径是「框内居中」⇒ 字的中心会跟着框心走。判据/残差 → 报告 §4/§5。
            float txW = PreferredWidthPx(tx, "Timer");
            var txFit = txW > 0f ? new PxRect(TimerTxX1, TimerTxY1, TimerTxX1 + txW, TimerTxY2) : txR;
            if (txW > 0f)
            {
                tx.SetAutoFitBox(LayoutSpace.Px(txFit.W), LayoutSpace.Px(txFit.H), 10f, 38f, 38f);
                // ⚠️ 上面那一句内部会调 `SetWrapWidth` ⇒ **无条件把模式开成 `Normal`**，而原版这一颗是
                //   **折行 = 0** ⇒ 紧跟着还原（成对写法同 A404 / A205 / A34-F4 那一族，这里与上面那次同源）。
                tx.SetWrapping(false);
            }
            _timerText = tx != null ? tx.transform : null;
        }

        // ------------------------------------------------------------ 左列说明
        void BuildInstructions(Transform root)
        {
            var ins = MenuDraw.Node(root, "Event Instructions", InsR);
            // 原版 hAlign = **Left / Top**（现读 `对齐=Left/Top · 折行=1`）
            const string TxtScore = "Earn Skulls by playing <color=#E27E1B>Classic</color>, "
                                  + "<color=#E27E1B>Skirmish</color> or <color=#E27E1B>Draft</color> "
                                  + "matches as one of the participating factions to advance the reward track.";
            var a = MenuDraw.TextBox(ins, InsScoreR, TxtScore, Color.white, "Scoring Instructions",
                                     41.1f, 18f, QText, 50f, 36f);
            MenuDraw.AlignLeft(a, InsScoreR);
            // `Energy Instructions`：`{0}` **照 prefab 原文**（判据见文件头那一段）
            const string TxtEnergy = "Energy is consumed when you begin a match with one of the participating "
                                   + "factions. You recover 1 Energy every {0} minutes.";
            var b = MenuDraw.TextBox(ins, InsEnergyR, TxtEnergy, Color.white, "Energy Instructions",
                                     44.35f, 18f, QText, 72f, 50f);
            MenuDraw.AlignLeft(b, InsEnergyR);
            // `Scoring Icon`：原版 `m_Sprite` 出厂就是 **null**（运行时由 `scoringIcon` 字段灌）
            MenuDraw.Node(ins, "Scoring Icon", InsIconScR);
            // `Energy Icon`：`40k_topmarquee_currency_energy` 90×90 —— 🔴 **本仓 `Resources/` 里没有这张图**
            //   （源在 `bundle_boosterpacks_assets_all/Sprite/`，切好的 PNG 在
            //    `d:/2/Warpforge_tools/data/ui_extract/boosterpacks_assets_all/Sprite/`）
            //   ⇒ 节点照建、这一格**不画**，`Art()` 会记账进 `MissingArt` 并出声（⛔ 不拿别的图顶上）。
            var en = Art(ArtEnergy);
            if (en != null) MenuDraw.Rect(ins, en, InsIconEnR, "Energy Icon", QArt);
            else MenuDraw.Node(ins, "Energy Icon", InsIconEnR);
            Debug.Log("[Energy] `Scoring Icon` **没画** —— 原版这颗 `m_Sprite` 出厂就是 null（同上）。");
        }

        // ------------------------------------------------------------ 阵营选择
        void BuildArmySelector(Transform root)
        {
            var asp = MenuDraw.Node(root, "Army Selector Panel", AspR);
            // 🔴 **2026-10-19（A1190）**：这一颗原来**没传 autosize 实参** ⇒ 固定 44.65px。
            //   判据 = 逐颗现读原版那一颗（同一颗的现读命令见 `Reward Tile` 那一段）：
            //   `…/Army Selector Panel/Factions Title` = `'Factions'` · 字号 **44.65** ·
            //   基准 **36.0** · **`auto[18.0~45.869998…]`**（`m_enableAutoSizing = 1`）·
            //   对齐 `Left/Midline` · **折行 = 1** · 框 **563.61 × 54.68**（= `FacTitleR` 逐位同值）。
            //   ⚠️ 🔴 **`m_fontSizeMax`(45.87) ≠ `m_fontSize`(44.65)** —— 别拿字号顶上限（那是 A333 那一条），
            //   上限照实传 **45.87**（同 `label.SetAutoFitBox` 那条「上限不能取 `_tmp.fontSize`」的血证）。
            //   ⚠️ 折行 1 ⇒ ⛔ 不补 `SetWrapping(false)`。
            var ft = MenuDraw.Text(asp, FacTitleR, "Factions", Color.white, "Factions Title", 44.65f, QText,
                                   FacTitleR.W, 18f, 45.87f, 36f);
            MenuDraw.AlignLeft(ft, FacTitleR);
            // `Army Selector`（原版 `ScrollRect` + `CustomRaycaster` + `ShowEventArmiesPanel`）
            var sel = MenuDraw.Node(asp, "Army Selector", ArmySelR);
            _armySelector = sel;
            //   `Viewport`（原版 `RectMask2D`）⇒ 视口节点（软边 (0,0) = 原版这一颗没有软边）
            var vp = ViewportClip.Hang(sel, "Viewport", ArmySelR, Vector4.zero, Vector2Int.zero);
            var vpT = vp != null ? vp.transform : sel;
            //   `Army Content`（原版 `ContentSizeFitter(h:PreferredSize)` + `HorizontalLayoutGroup`）
            _armyContent = MenuDraw.Node(vpT, "Army Content", ArmyContentR);
            //     `ArmyItemReference`：原版的**模板件**（`m_Sprite` 出厂 null；`ShowEventArmiesPanel`
            //     运行时按活动阵营表 `Instantiate` 它）。本地没有那份表 ⇒ **只建模板那一颗**（= 出厂同档）。
            MenuDraw.Node(_armyContent, "ArmyItemReference", ArmyContentR);
            Debug.Log("[Energy] `Army Content` 下**只有模板那一颗 `ArmyItemReference`**（= 原版出厂态）——"
                    + " 原版 `ShowEventArmiesPanel` 按**活动阵营表**逐阵营 `Instantiate`，本地没有那份表。");
            var fi = MenuDraw.TextBox(asp, FactionInsR,
                                      "Play with any of these factions to take part in the event.",
                                      Color.white, "Faction instructions", 42.95f, 18f, QText, 72f, 36f);
            MenuDraw.AlignLeft(fi, FactionInsR);
        }

        // ============================================================ 几何 / 素材助手
        /// <summary>「中心 + 宽高」→ 画布矩形（y 向下）。</summary>
        static PxRect CenterRect(float cx, float cy, float w, float h)
            => new PxRect(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);

        /// <summary>🔴 **2026-10-10（A1178）**：量出这颗文字的 **preferred width**（单位 = **画布 px**）——
        /// 复刻原版那两颗 `ContentSizeFitterMinMax(h: PreferredSize)` 的**量尺**（框宽 = 这个数）。
        ///
        /// <para>**原版那一档是什么**（现读）：`Victories title` 与 `Timer` 的 `m_SizeDelta.x = 0` +
        /// `ContentSizeFitter(m_HorizontalFit = PreferredSize)` ⇒ **运行时框宽 = 文字的 preferred width**。
        /// 出处 = `资料/普查产出_第十会话/R4_布局刻度族查实.md` §1·1（工具那一列标 `⚙CSF` = 算不出）。
        /// `R4` §1·4 的裁定 = **要复刻这一档**（铁律 11）。</para>
        ///
        /// <para>🔴 **为什么量的是【我方】字体**：本仓给 TMP 赋字体的**只有一处** ——
        /// `Core/TmpFont.cs` 的 `ResourcePath = "Fonts/NotoSerifCJK-Regular SDF"`（全仓只有一个字体资产，
        /// 判据见那个文件头部「这不是我们换了字体，是全仓只有一个字体」那一段）；
        /// 而原版那两颗用的是 **`Pragati-Regular SDF`**（`pointSize 95`，无 kerning）
        /// ⇒ **两个字体量出来的宽本来就不该相等**
        /// （原版算得 `'Victories: '`@53.5 = **176.98** · `'Termina en: 23d 5h'`@38 = **248.44**，
        /// 公式 = `Σ advance × (fontSize/pointSize)`，出处 `TMP_Text.cs:3684-3717`）。
        /// ⇒ ⛔ **不许照抄那两个数**，必须量我们**真的会画出来**的那个宽度。</para>
        ///
        /// <para>**量的是 TMP 自己的 `GetPreferredValues()`**（= `TMP_Text.GetPreferredWidth()` 那条路）——
        /// 它与 UGUI 的 `ContentSizeFitter` **走同一个函数**：
        /// `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TMP_Text.cs:3684-3699` ——
        /// 字号取 `m_enableAutoSizing ? m_fontSizeMax : m_fontSize`、margin 取 **∞**（= 不折行）。
        /// 这与 `SetAutoFitBox(…, maxPx: 标称)` 配起来正好是**同一个不动点**：框 = 「上限字号排一行」的宽、
        /// 自适应二分于是收敛在**上限**上 ⇒ **字不会被缩**（原版语义逐条相同）。
        /// ⚠️ 反编译那份（`d:/2/tools/decomp_full/`）**只覆盖 `Assembly-CSharp`、没有 TMP**
        /// （`grep -rl "enableAutoSizing"` 零命中）⇒ 判据取**工程自带的包源码**（同 `A1197` 那一段的口径）。</para>
        ///
        /// <para>⚠️ **量不到就返回 0**（`lb == null` / 点阵后端 / 取不到 TMP）⇒ 调用方**保留建树时那个框**
        /// 并出声，⛔ 不静默（红线：不许静默失败）。（点阵后端没有「preferred width」这回事 ——
        /// 那后端连折行都没有。）</para>
        ///
        /// <para>🔴 **落地之后【动了自检基线】**（下一会话必读）：`Editor/ShellScene.cs` 的 `fitCase` 那两条
        /// （`"…的 Timer"` / `"…的 Victories title"`，A1126 那一节）把**旧框宽** `289.72` / `254.112`
        /// 当字面量传进去断「渲出来 ≤ 框」⇒ 本件改完**那两条要重基线**。✅ **2026-10-10 已重基线**：
        /// 那两条的**框那两格改成【故意给整屏】**（真框是运行时量出来的 ⇒ 拿它当字面量 = **同义反复**）。
        ///
        /// 🔴 **2026-10-10（`F2` · 铁律 5）就地订正下面这段「建议形态」—— 它的后半句是错的**：
        /// 本处原来建议「断 `FontPxNow` **没被缩**」，而**实测那是 (α) 断言错**：`A1178` 撑开的
        /// **只有【宽】**，**真正紧的是【高度】**（`Timer` 框高 `55.905` · `Victories title` `73.385`，
        /// 且**框高就是原版的** —— `menu_dump` 实读 `m_SizeDelta = (0, 55.905)` / `(0, 73.385)`、
        /// 锚 `(0,1)→(0,1)` 不拉伸 ⇒ `rect.height` 就是它）⇒ 同一把框高**原版装得下**（行盒 `≤ 1.47em`）、
        /// **我们装不下**（行盒 `≈ 1.52em`，`38 × 1.52 = 57.8 > 55.905`）⇒ 收敛 `36.851` / `48.111`
        /// → **「≈上限」必红**。⇒ **落地形态改成两条关系式**（见 `Editor/ShellScene.cs` 那一段）：
        /// ①「渲出高 ≈ **原版那一颗的框高**」②「**框宽 − 渲出宽 ≥ 1.5px**」（宽度那一侧不是约束）。
        /// ⚠️ 这是**字体度量的差**（行盒 `1.52em` vs 原版 `≤1.47em`，已立 **`A1262`**），
        /// ⛔ **不是框摆错了** ⇒ 无需改本文件。</para></summary>
        static float PreferredWidthPx(Label lb, string who)
        {
            if (lb == null) return 0f;
            // `Label` 没有公开 TMP 句柄（`_tmp` 私有；共用件 `Battle/Label.cs` **不在本件白名单**，
            // 不动它）⇒ 从子件取：TMP 是 `TmpFont.NewText` 建在 `Label` 底下的**子件**
            // （`Battle/Label.cs` 的 `BuildTmp`：`_tmp = TmpFont.NewText(transform, "text", …)`）。
            var t = lb.GetComponentInChildren<TextMeshPro>(true);
            if (t == null)
            {
                Debug.LogWarning("[Energy] `" + who + "` 量不出 preferred width（这颗没有 TMP —— 字体资产缺失时"
                        + " `Label` 退回点阵后端）⇒ 框宽停在建树时那一档，**不是**原版 CSF 的行为。");
                return 0f;
            }
            // ⚠️ 用**无参**那个重载（不是 `GetPreferredValues(text, w, h)`）—— 它不重解析 `m_text`，
            //   且内部就是 `GetPreferredWidth()`：margin = ∞、字号取 `fontSizeMax`（判据见方法头）。
            float wWorld = t.GetPreferredValues().x;
            if (float.IsNaN(wWorld) || float.IsInfinity(wWorld) || wWorld <= 1e-6f)
            {
                Debug.LogWarning("[Energy] `" + who + "` 的 preferred width 量出 " + wWorld
                        + "（NaN/Inf/非正）⇒ 框宽停在建树时那一档，**不是**原版 CSF 的行为。");
                return 0f;
            }
            // 世界单位 → 画布 px：**`LayoutSpace.Px` 的逆**（全壳唯一那条换算：108 px = 1 世界单位）。
            // ⛔ 别在这里手写 `× 108` —— `MenuDraw.TextCore` 收 `wrapPx` 走的就是 `LayoutSpace.Px`，
            //    两边必须是同一条（否则「框 = 量出来的宽」差一点点就够触发 TMP 的缩小）。
            return wWorld * (LayoutSpace.DesignPxH / LayoutSpace.DesignHeight);
        }

        /// <summary>取图 + **记账**（同族 `LiveOpsEventWindow.Tex`）。
        /// 🔴 为什么要它：`MenuDraw.Rect/Nine/Tiled` **取不到图就 `return null`、一声不吭** ⇒ 整层静默消失。
        /// 本窗所有取图都走这里；自检那条「图一张都不能少」会读 <see cref="MissingArt"/>。</summary>
        public Texture2D Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name)) MissingArt.Add(name);
            return t;
        }

        /// <summary>`Window Title` 的 `Label`（自检量字距 / 自适应用）。</summary>
        public Label WindowTitleLabel { get { return _titleLabel; } }
    }
}
