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
// | `Timer` / `Timer Icon` | `'Termina en: 23d 5h'` | `TimerDisplay.Initialize(endTime)` | **整棵不建**（截止时刻本地没有 ⇒ 原版 `UpdateTimer` 那一支是 `return;` 静默 ⇒ ⛔ 不印 `--`、⛔ 不编日期；2026-10-30 `B99_A4`） |
// | `Energy Instructions` | `'… every {0} minutes.'` | `ShowCurrentEnergyRegenText.OnEnable` 读 **LiveOps Config** | **按原版填两个参数**（词条 `Event_Description/ScoringEvent/EnergyRules` + `EnergyConfig` 默认 1 / 300000ms，判据见下） |
// | `Total Victories` | `'751'` | `RewardProgressBarPanel` 按活动数据 | **照 prefab 出厂原文** + 出声 |
// | `Event Image` / `Scoring Icon` / `ArmyItemReference` | **`m_Sprite` = null** | 运行时由字段灌图 | **`Scoring Icon` 画 Skulls 图**（原版 `GetScoringIconFromEvent` 写死 Skulls，映射 `Shell/ScoringIcons.cs`）；其余两颗建节点、**不画**、出声 |
// | `Army Content` 下的阵营 | 出厂只有模板 `ArmyItemReference` | `ShowEventArmiesPanel` 逐阵营 `Instantiate` | **只建模板那一颗**（= 出厂同档） |
// | `Progress Bar` 的 `Fill` | 现读 **0.00 × 5.74**（= 进度 0） | `Slider.normalizedValue`（`InterpolateMilestones`） | 出厂画 0 宽；`SetProgress(v)` 是**两态**那一半（自检用） |
//
// 🔴 `Energy Instructions` 的填充判据（`ShowCurrentEnergyRegenText__OnEnable.c` 逐句 + 2026-10-25 现读补齐）：
//    `ConfigManager.GetConfig<EnergyConfig>()` → `+0x20` = `rechargeAmount` · `+0x18` = `rechargeCooldown`(ms)
//    → `TimeSpan.FromMilliseconds(...)` → `TimeHelper.FormatTimeSpan(ts, showDays:1, hide0Values:1)`
//    → `string.Format(LocalizationManager.GetTranslation(localizationKey), 上面两个)`。
//    · `localizationKey` 现读 = **`Event_Description/ScoringEvent/EnergyRules`**（`assets_full` 的
//      `bundle_menus_assets_all` 里 `localizationKey` 只有两种取值，另一种是 `MenuShop/PremiumCampaign`）；
//      官方 13 语表全有这条，en 原文 = `… You recover <color=#32CD32>{0}</color> Energy every <color=#32CD32>{1}</color>.`
//    · `EnergyConfig` 的**序列化默认**（本 build 出货值；服务端不可达 ⇒ 本地只有这一份）：`rechargeAmount = 1` ·
//      `rechargeCooldown = 300000 ms`（`cpp2il/EnergyConfig.cs:8-9`）⇒ `{1}` = `5m` / `5分钟`
//      （`TimerDisplay.FormatTimeString` = `SupportMethods.GetTimeString` 的逐句移植）。
//    🔴 **2026-10-25 就地订正（铁律 5；旧结论已过期）**：原文写「本地没有那份 Config ⇒ 替换值查不到 ⇒ 原样保留 `{0}`」。
//      **不成立** —— 值的来源有两条本地判据（词条在官方表、两个数在 `EnergyConfig` 的序列化默认里）⇒ 现在**按原版填**。
//      ⛔ 仍然不许自己编一个别的分钟数；要改只能改 `EnergyRechargeAmount / EnergyRechargeCooldownMs` 两个带出处的常量。
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
//     —— 形状照同族范本（`Shell/RankedEventWindow.cs` · `Shell/SkirmishEventWindow.cs`）。
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
            TryOpen(null, PermanentEvent);   // 原版 `TryOpen`：`activationType == Permanent ⇒ timerDisplay.SetActive(false)`（四句见类尾 `TryOpen`）
            if (_warned) return;
            _warned = true;
            Debug.Log("[Energy] 本窗的**活动数据全在远端**（`SinglePlayerScoringEvent` 是 LiveOps 事件）："
                    + "5 档分数 / 箱子开关 / 倒计时截止时刻 / `Total Victories` 都是**运行时灌**的 "
                    + "⇒ 我们照 **prefab 出厂原文**画（`1256` / `751` / 闭箱图），⛔ 不编数字；"
                    + "`Timer` 那一行**整棵不建**（原版 `TimerDisplay.UpdateTimer` 取不到目标时刻是 `return;` 静默）—— 截止时刻本地没有（出声一次）。");
            Debug.Log("[Energy] `Energy Instructions` **已按原版填参**（模板 = 官方词条 "
                    + "`Event_Description/ScoringEvent/EnergyRules`；`{0}` = `EnergyConfig.rechargeAmount` 默认 **1**、"
                    + "`{1}` = `rechargeCooldown` 默认 **300000 ms** 的时长串 ⇒ `5m` / `5分钟`）。");
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
            _levels.Clear(); _scoreLabels.Clear(); _crateHi.Clear(); _chests.Clear();
            _scoreLines.Clear(); _scoreBar = null; _scoreSlider = null;   // 🆕 2026-10-29（B97_MINI2）：组件表随树重建

            _root = transform;
            BuildBackdrop(transform);
            BuildImageMask(transform);
            BuildRewardPanel(transform);
            BuildHeader(transform);
            if (EnergyTimerDeadline.HasValue) BuildTimer(transform); else { _timerText = null; Debug.Log("[Energy] `Timer` **整棵不建** —— 原版 `SinglePlayerOnlyEnergyEventWindow__TryOpen` 只在 `activationType != 0` 时 `timerDisplay.Initialize(事件.GetEndDate())`，而 `TimerDisplay__UpdateTimer.c` 取不到目标时刻那一支是 `return;`（静默、一个字都不印）；截止时刻由服务端 LiveOps 下发、本地没有（判据 → `判据/B99_R3b_施工单_其余按文件.md` §2·4 / §五 #1；先例 = `B99_W2` 的 `SkirmishEventWindow.BuildClassicTimer`）。"); }   // ⛔ 不建 `Timer`/`Timer Icon`、⛔ 一个字都不印（§2·4「留开关，不硬删」：将来接上 liveop 截止时刻只改类尾 `EnergyTimerDeadline`）
            BuildInstructions(transform);
            BuildArmySelector(transform);

            // ---- 两颗命中节点（判据全文 → 文件头那一段，2026-10-13 A795 订正）------------------
            // ① **压暗层那颗（含「点它关窗」）** —— 原版 `__OnEnable.c:13-18` 给
            //    `backgroundCloseButton.onClick` 挂的监听与表头返回钮是**同一个 delegate**
            //    ⇒ 点压暗层 = 点返回钮 = `Close()`。
            //    形状照同族范本 `Shell/RankedEventWindow.cs` / `Shell/SkirmishEventWindow.cs`：
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
            MenuDraw.ShadeHit(transform, RootR, QShade, QHit, () => { if (_shadeClickBound) CloseButtonClick(); }, "BackgroundHit");
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
            // 🔴 **2026-10-19（界面线 P05）**：文案走官方词条 —— 本窗 `Reward Tile` 的 `Localize.mTerm =
            //   Ranked/Skirmish/ProgressionTitle`（UnityPy 现读本 prefab 的 Reward Tile → Localize；
            //   官方值 en `Progression` / zh `进步`；英文档与改前写死的逐字相同）。
            MenuDraw.Text(rp, RTileR, Loc.T("Ranked/Skirmish/ProgressionTitle"), Color.white, "Reward Tile", 45.87f, QText,
                          RTileR.W, 18f, 45.87f, 36f, face: TmpFont.Face.LatinCardBody);
            // `Reward Help`：**出厂 act = F** ⇒ 建出来、**关着**（同族先例：`Banned card in deck`）
            //   🔴 **2026-10-19（界面线 P05）**：文案走官方词条（`Localize.mTerm = Ranked/Skirmish/Progression`；
            //   官方值 en = 改前那句原文 / zh `赢得战斗以在活动中取得进步并解锁奖励。`）。
            var help = MenuDraw.TextBox(rp, RHelpR, Loc.T("Ranked/Skirmish/Progression"),
                                        Color.white, "Reward Help", 38f, 18f, QText, 38f, 36f, face: TmpFont.Face.LatinCardBody);
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
            // 🆕 2026-10-29（`B97_MINI2` · 账 `未分类_其他:430`）：原版 `MilestoneScoreBar.progressBar` 就挂在这颗
            //   `Progress Bar` 上（判据 = `menu_dump.py bundle_menus_assets_all "EnergySinglePlayerOnlyEventWindow"` 现读：
            //   `Scoring Bar Event Score Info` 的组件 = `MilestoneScoreBar`（字段 `progressBar,scoringLines`）·
            //   `Progress Bar` 的组件 = **`Slider`** · 5 行 `Score Bar Line Level k` 的组件 = `GenericScoreBarLine`）。
            //   本工程的渲染线**不画 uGUI 滑块** ⇒ 这颗 `Slider` 只承载 `maxValue / normalizedValue`
            //   （原版 `SetProgressBar` 写的就是这两格），Fill 仍由下面那颗 quad 表示（见 `DrawFill`）。
            var barSlider = pr.gameObject.AddComponent<UnityEngine.UI.Slider>();
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
            // 🆕 **2026-10-17（界面线 `T07` · `A1753` #12）**：原版 `Collect Button ▸ Button Text` 挂
            //   `Pragati-Regular Off White Cold`，`_FaceColor = (0.912, 0.981, 0.961)`、顶点白。
            //   判据 = `资料/普查产出_1017/UI/R_T07_剩余四项.md` §4·2 #12（同族另一站 `SkirmishEventWindow` 由别的写手改）。
            // 🔴 **2026-10-17（第四十八会话 · `N47`）**：这一颗原版 `Button Text` 的 `Localize.mTerm` =
            //   **`MainMenu/General/Collect`**（整包普查 26 颗里 24 颗是这个键、`m_Enabled = 1`）⇒ 接词条。
            //   ✅ **2026-10-19（`W57`）独立复核**：本颗 = `EnergySinglePlayerOnlyEventWindow/Reward Progress Panel/
            //   Scoring Bar Event Score Info/Generic Simplified UI Button/Button Text` ⇒ `mTerm` 同键 · `m_Enabled = 1`
            //   （按 `m_GameObject` 认领，⛔ 不是照 Skirmish 那颗互推）。
            var ct = MenuDraw.Text(_collect, CollectTxR, Loc.T("MainMenu/General/Collect"),
                                   new Color(0.912f, 0.981f, 0.961f, 1f), "Button Text",
                                   55f * SBarFontScale, QText, CollectTxR.W,
                                   10f * SBarFontScale, 55f * SBarFontScale, 12f * SBarFontScale, face: TmpFont.Face.LatinCardBody);
            // 🔴 **2026-10-19（A1190）**：这一颗原来**没传 autosize 实参** ⇒ 固定 55px。
            //   判据 = 逐颗现读原版那一颗（`工具/menu_dump.py bundle_menus_assets_all
            //   "EnergySinglePlayerOnlyEventWindow" --depth 10 --md`）：
            //   `…/Generic Simplified UI Button/Button Text` = `'Collect'` · 字号 **55.0** ·
            //   基准 **12.0** · **`auto[10.0~55.0]`**（`m_enableAutoSizing = 1`）·
            //   对齐 `Center/Capline` · 🔴 **折行 = 0** · 框 **280.88 × 73.17**（= `CollectTxR` 逐位同值）。
            //   ⚠️ **折行 0**：`SetAutoFitBox` 内部那句 `SetWrapWidth` 会**无条件**把模式开成 `Normal`
            //   ⇒ 紧跟着关掉（同族先例 = `Shell/AlliancePanelWindow.cs` 的 `lb.SetWrapping(false)`）。
            //   ⚠️ 关得越早越好（`SetWrapping` 会推版面）—— 本颗后面**没有** `Align*`，所以排在这里就够。
            // 🔴 **2026-10-19（A1197）**：四格字号 **全部 × `SBarFontScale`（1.32）** —— 本颗在
            //   `Scoring Bar Event Score Info`（`m_LocalScale = 1.32`）那棵子树里，而原版 prefab 里的
            //   `m_fontSize/m_fontSizeMin/m_fontSizeMax/m_fontSizeBase`（55/10/55/12）是**未缩放**的值
            //   ⇒ 原版屏上字号 = 55 × 1.32 = **72.6**。我们写的 55 相对框（`CollectTxR`，已是屏值）小 **24.2%**。
            //   判据全文 → 本文件上面那一段「A1197：缩放子树里的**字号**刻度积」。⛔ **框不动**。
            if (ct != null) ct.SetWrapping(false);
            // 原版这颗钮 → `CollectClicked()`：要向 LiveOps 领奖 ⇒ **要服务器**。
            //（本工程边界③：入口照做、点了如实说，⛔ 不静默。）
            MenuDraw.Hit(_collect, "CollectHit", CollectR, QHit, CollectClicked, cb, ArtMulligan,
                         ArtMulligan + "_hover", ArtMulligan + "_Pressed",
                         clickCue: WindowSounds.PositiveCue);

            WireScoreBar(bar, barSlider);
        }

        /// <summary>点第 `level` 档（1..5）的宝箱 —— 原版 lambda = `RewardService.Preview(reward)`
        /// （`GenericScoreBarLine.__c__DisplayClass6_0___Initialize_b__0.c`）。本地没有奖励对象/预览窗 ⇒ 出声。</summary>
        void OnChestClick(int level)
        {
            Debug.Log($"[Energy] `Score Bar Line Level {level}/Chest` 点了 ⇒ **无奖励预览**"
                    + "（原版这一跳 = `RewardService.Preview(该档奖励)`，奖励对象来自远端 LiveOps；"
                    + "本地没有那份数据、也没有「奖励预览」窗 —— 照边界③不静默）。");
        }

        void OnCollectClick()
        {
            Debug.Log("[Energy] `Collect` 点了 ⇒ **如实提示「暂无服务器」**"
                    + "（原版 `SinglePlayerOnlyEnergyEventWindow__CollectClicked.c` 要走 LiveOps 领奖；"
                    + "本地没有活动、也没有服务器 —— 照边界③不静默）。");
            if (WindowsManager.Instance != null)
                // 🔴 **2026-10-17（`A1756`）如实标注**：这一句**是我们自拟的**（官方表里**没有**
                //   `Event rewards are not available offline.` 这条 —— 现读 `官方本地化_en+zh-CN.json` 0 命中；
                //   原版这一路要连服务器，客户端没有「离线」这个分支的文案）⇒ ⛔ 别硬套键、别当成原版文案。
                //   同族同一句还在 `Shell/SkirmishEventWindow.cs`（那一处不在本笔白名单）。
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
                var hiQ = MenuDraw.Rect(row, Art(ArtCrateHi),
                              CenterRect(CrateHiCx, CrateHiCy + dy, CrateHiW, CrateHiH),
                              "Highlight Crate", QArt1, CrateHiTint);
                _crateHi.Add(hiQ);
                // `Chest`（自带 scl **0.8696**）· `Skull`（同）—— 中心随行一起走
                // 🔴 **2026-10-18（音效线转来的界面缺口）：补命中区 + 点击音**（原来**只有视觉**，点了什么都不发生）。
                //   判据（原版 prefab 逐字段现读 + 反编译）：
                //   · 这颗是 `Button`/`EverguildButton`（GO `Chest*`，MB 五件一组：RT/Image/Button/
                //     EverguildButton/EverguildButtonMaterialModifier）；`m_onClickSound` = **空 PPtr** +
                //     `useDefaultAudioClipIfNull = 1` ⇒ 播**默认 `ButtonClick`**（≠ Positive，⛔ 别顺手改）。
                //   · 点击动作 = `GenericScoreBarLine.Initialize` 里给 Chest 挂的 lambda：
                //     `RewardService.Preview(reward)`（`GenericScoreBarLine.__c__DisplayClass6_0___Initialize_b__0.c`）
                //     —— 打开该档奖励的**预览**。
                //   ⚠️ **本地没有“奖励预览”的窗口/数据**（`RewardService` 走远端 LiveOps 奖励对象）⇒
                //     命中区与音照原版接，动作**留空并出声**（同本窗 `Collect` 的口径：边界③不静默）。
                var chestRect = CenterRect(ChestCx, ChestCy + dy, ChestW, ChestH);
                var chestQ = MenuDraw.Rect(row, Art(CrateArt[i]), chestRect, "Chest", QArt);
                _chests.Add(chestQ);
                //   原版这颗是 **`m_Transition = 1 (ColorTint)`**（不是 SpriteSwap）⇒ **不传 `target`**：
                //   `WindowButton.Bind` 在 `art == null` 时会把 `" → "` 记进 `MissingSwapArt`（假缺口），
                //   而这里没有图要换 —— 命中 quad 自己那颗透明件就够（同 `MissionsTab.AddInfoTooltip` 的写法）。
                MenuDraw.Hit(row, "ChestHit", chestRect, QHit, () => OnChestClick(i + 1));
                var skullQ = MenuDraw.Rect(row, Art(ArtSkull), CenterRect(SkullCx, SkullCy + dy, SkullSide, SkullSide),
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
                                       18f * ScoreFontScale, 48f * ScoreFontScale, 36f * ScoreFontScale, face: TmpFont.Face.LatinCardBody);
                // 🔴 **2026-10-19（A1197）**：四格字号 **全部 × `ScoreFontScale`（1.1478264）** ——
                //   这颗的祖先链是 `Scoring Bar Event Score Info`(1.32) ← `Skull`(**0.86956543**) ← `Score`，
                //   而原版 prefab 里的 48/18/48/36 是**未缩放**的值 ⇒ 原版屏上字号 = 48 × 1.1478264 = **55.096**。
                //   我们原来直接传 48 ⇒ 相对框（`ScoreW` = 133.32，**已是屏值**）小 **12.9%**。
                //   判据全文（含「TMP 在本地单位里二分」那两条包源码行号）→ 上面常量声明处那一段。
                //   ⚠️ 那颗 `0.8696` 长在 **`Skull`** 身上、`Score` 是 `Skull` 的**子件**（现读链，⛔ 别记到行节点头上）。
                //   ⛔ 框不动（`sr` = `CenterRect(…, ScoreW, ScoreH)`，本来就是乘过刻度的屏值）。
                MenuDraw.AlignRight(sc, sr); _scoreLabels.Add(sc);
                // 🆕 2026-10-29（`B97_MINI2` · 账 430）：这一行就是原版那颗 `GenericScoreBarLine`
                //   （prefab 现读：`Score Bar Line Level k` 挂它，字段 `score/chestReward/chestOpen/readyToCollect/resourceIcon`
                //    正好指向 `Score` / `Chest` / 出厂开箱图 / `Highlight Crate` / `Skull` 这五件）。
                //   ⇒ 组件挂上去、把**已经建好的这五件**绑给它（几何/颜色/父子链一个像素都没动）。
                var line = row.gameObject.AddComponent<CardPresentation.ScoreBars.GenericScoreBarLine>();
                line.Bind(sc, chestQ, Art(CrateArt[i] + "_open"),
                          hiQ != null ? hiQ.gameObject : null, skullQ);
                _scoreLines.Add(line);
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
            // 🔴 **2026-10-19（界面线 P05）**：文案走官方词条（`Localize.mTerm = Ranked/Victories`；
            //   官方值 en `Victories: `（**带尾空格**，与改前写死的逐字相同）/ zh `胜利：`）。
            var vt = MenuDraw.Text(pv, vtR, Loc.T("Ranked/Victories"), Color.white, "Vicotries title", 53.5f, QText,
                                   vtR.W, 18f, 53.5f, 36f, face: TmpFont.Face.LatinCardBody);
            // 🔴 **A1178**：量 preferred width ⇒ 把框收成它（= 原版 CSF 那一档）。
            //   ⚠️ `SetAutoFitBox` 会**再跑一次自适应**（原版 CSF 撑完框也要重排一次，语义同）；
            //   它的 `SetWrapWidth` 顺手把折行模式开成 `Normal` —— 本颗原版就是 **折行 1** ⇒ 不用还原。
            float vtW = PreferredWidthPx(vt, "Victories title");
            // `Skull Victories` 先按模板位建着，下面量完 `ttW` 再整行重排（HLG 复制）。
            var skullQ = MenuDraw.Rect(pv, Art(ArtSkull), VSkullR, "Skull Victories", QArt);
            var ttR = new PxRect(VTotalX1, VTitleY1, VBgR.x2, VTitleY2);
            // 🔴 **2026-10-19（A1179）**：补 autosize 四格（原来没传 ⇒ 固定 77px）。
            //   判据 = 同一颗现读（同 `Reward Tile` 那条命令）：
            //   `…/PLayer Victories/Total Victories` · `'751'` · 字号 **77.0** · 基准 **36.0** ·
            //   **`m_enableAutoSizing = 1` · `auto[18.0~77.0]`** · 对齐 `Left/Midline` · **折行 = 1** ·
            //   原版框 **宽 0**（+ `ContentSizeFitterMinMax(h:PreferredSize)`，工具标注 `⚙CSF` = 算不出，
            //   运行期框宽 = 文字的 preferred width）。
            //   ⚠️ 原版**折行 = 1** ⇒ 传 `wrapPx` 与它同档，⛔ 不要补 `SetWrapping(false)`。
            var tt = MenuDraw.Text(pv, ttR, "751", Color.white, "Total Victories", 77f, QText,
                                   ttR.W, 18f, 77f, 36f, face: TmpFont.Face.LatinCardBody);
            float ttW = PreferredWidthPx(tt, "Total Victories");

            // 🔴🆕 **2026-10-17（A1245 + A1246）：整行按原版那条 `HorizontalLayoutGroup` 重排。**
            //   `<para>判据（本次现跑两份 prefab 的差别本身就是判据）：
            //   `EnergySinglePlayerOnlyEventWindow`（**没跑过布局**）的模板位 = `Victories title` 1574.60 宽 0 ·
            //   `Skull Victories` 1581.20..1654.59 · `Total Victories` 1661.19 宽 0；
            //   `SkirmishModeEventWindow`（**跑过布局**）的真值 = `…`（那一扇没有 HLG，见报告）。
            //   Energy 这一颗的 HLG 实读：**`spacing = 6.6` · `align = 4 (MiddleCenter)` · pad 0**、
            //   容器 `Player victories` = `[1320.49, 227.39] – [1915.30, 317.83]`（**594.81** 宽）；
            //   `Victories Background` 挂 `LayoutElement.m_IgnoreLayout = 1` ⇒ **不参与**布局。
            //   ⇒ `content = vtW + 6.6 + 73.39 + 6.6 + ttW`、`start = 1320.49 + (594.81 − content) × 0.5`。
            //   模板位正是 `vtW = ttW = 0` 那一档：`1320.49 + (594.81 − 86.59)/2 = 1574.60` ✓（交叉自洽）。</para>
            //   ⚠️ 量不到（`PreferredWidthPx` 返回 0，它自己已经出声）⇒ **整行退回建树时的模板位**，
            //   ⛔ 不拿 0 当宽去排。`vtW` 只量到一半时也不重排（两个 CSF 子件是**同一条 HLG** 里的两块）。
            if (vtW > 0f && ttW > 0f)
            {
                const float RowSpacing = 6.6f;                   // 原版 HLG `m_Spacing`
                float content = vtW + RowSpacing + VSkullR.W + RowSpacing + ttW;
                float x = PVicR.x1 + (PVicR.W - content) * 0.5f; // align 4 = MiddleCenter
                var vtFit = new PxRect(x, VTitleY1, x + vtW, VTitleY2);
                x += vtW + RowSpacing;
                var skFit = new PxRect(x, VTitleY1, x + VSkullR.W, VTitleY2);
                x += VSkullR.W + RowSpacing;
                var ttFit = new PxRect(x, VTitleY1, x + ttW, VTitleY2);
                vt.SetAutoFitBox(LayoutSpace.Px(vtFit.W), LayoutSpace.Px(vtFit.H), 18f, 53.5f, 36f);
                MenuDraw.AlignRight(vt, vtFit);                  // 原版 `Right/Capline`（框 = 字宽 ⇒ 两者同解）
                if (skullQ != null)
                    skullQ.transform.localPosition =
                        MenuDraw.Local(pv, skFit.x1, skFit.y1, skFit.x2, skFit.y2);
                tt.SetAutoFitBox(LayoutSpace.Px(ttFit.W), LayoutSpace.Px(ttFit.H), 18f, 77f, 36f);
                MenuDraw.AlignLeft(tt, ttFit);                   // 原版 `Left/Midline`
            }
            else
            {
                MenuDraw.AlignRight(vt, vtR);
                MenuDraw.AlignLeft(tt, ttR);
            }
            _victoriesTitle = vt != null ? vt.transform : null;
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
            System.Action onBack = CloseButtonClick;   // 原版三处用**同一个 delegate**（表头返回 / 压暗层 / Open）
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
                OnBack = onBack,
            });
            _titleLabel = p.Title;
            _title = p.Title != null ? p.Title.transform : null;
            _back = p.BackNode;
            _backBg = p.BackQuad != null ? p.BackQuad.transform : null;

            WireHeaderComponent(p, onBack);   // 🔴 2026-10-28（B89-E29）接线到原版那颗 `WindowHeaderWithBackButton`（方法在类尾）
        }

        // ------------------------------------------------------------ 倒计时
        void BuildTimer(Transform root)
        {
            var tm = MenuDraw.Node(root, "Timer", TimerR);
            var iconQ = MenuDraw.Rect(tm, Art(ArtClock), TimerIcR, "Timer Icon", QArt);
            // 那一格的现读框**宽 = 0**（`ContentSizeFitterMinMax(h:PreferredSize)` 算不出）。
            // 🔴 **2026-10-10（A1178）这一档【已复刻】**：运行时框宽 = **文字的 preferred width**，
            //   由下面的 `PreferredWidthPx` 在**建树期量一次**写回（⛔ 量我方字体，不是原版那个 248.44）。
            var txR = new PxRect(TimerTxX1, TimerTxY1, TimerR.x2, TimerTxY2);
            // 🔴 **2026-10-18（A1126 · A1 档）**：这一处原来**没传 autosize 实参** ⇒ 固定 38px。
            //   判据 = 逐颗现读原版那一颗（同本文件头那条命令）：`…/Timer/Timer`（TMP） = `'Termina en: 23d 5h'` ·
            //   字号 **38.0** · 基准 **38.0** · **`auto[10.0~38.0]`**（`m_enableAutoSizing = 1`）·
            //   对齐 `Center/Capline` · **折行 = 0**（`m_TextWrappingMode = 0`）· 原版 `m_SizeDelta = (0, 55.905)`。
            //   ⚠️ 原版**折行 = 0** ⇒ 传 `wrapPx` 会让 `SetWrapWidth` 把它**静默开成 `Normal(1)`**
            //   ⇒ 紧跟一句 `SetWrapping(false)` 还原（成对写法同 A404 / A205 / A34-F4 那一族）。
            // 🔴 **2026-10-17（N2）**：描述串 = **官方词条** —— 原版那个键是
            //   `Event_Description/General/EndsInPlusOrangeTime`（`bundle_menus_assets_all/MonoBehaviour/
            //   MonoBehaviour_666837755613493937.json` 实读 `useExtraText = 1` + 该 mTerm）。
            //   🔴 **2026-10-19（界面线 P05 · 铁律 5 就地订正）**：这里原来写「**正文不在**官方本地化快照里
            //   （只有 `EndsIn` / `EndsInPlusTime` / `TimeEndsIn`）⇒ 照 prefab 原文」—— **已过期**：
            //   当前官方表（10,854 条）里 `EndsInPlusOrangeTime` = en `Ends in:  <color=#E98A00FF>{0}</color>` /
            //   zh `结束于：<color=#E98A00FF>{0}</color>` ⇒ 改走 `Loc.T`。
            //   ⚠️ 值带 `{0}` ⇒ 出厂占位用 `TimerDisplay.Compose` 填（⛔ 不能用 `+` 拼，会印出花括号）。
            string TimerPrefix = Loc.T("Event_Description/General/EndsInPlusOrangeTime");
            var tx = MenuDraw.Text(tm, txR, TimerDisplay.Compose(TimerPrefix, "23d 5h"), Color.white, "Timer", 38f, QText,
                                   txR.W, 10f, 38f, 38f, face: TmpFont.Face.LatinCardBody);
            if (tx != null) tx.SetWrapping(false);
            // 🔴 **A1178**：量 preferred width ⇒ 把框收成它（= 原版 CSF 那一档）。
            float txW = PreferredWidthPx(tx, "Timer");

            // 🔴🆕 **2026-10-17（A1246）：按原版那条 `HorizontalLayoutGroup` 重排这一组。**
            //   判据（本次现跑两份 prefab 的差别本身就是判据）：
            //   · `EnergySinglePlayerOnlyEventWindow`（**没跑过布局**）= 模板位：图标 `938.0..982.0`
            //     （43.91 宽）、文字 `982.0` **宽 0**；
            //   · `SkirmishModeEventWindow`（**跑过布局**）= 运行期真值：图标 `813.8..857.7`、文字
            //     `857.7..1106.2`（248.44 宽）—— 正是 `start = 648.33 + (623.34 − 43.91 − 248.44)/2 = 813.83`。
            //   HLG 实读：`spacing 0 · align 4 (MiddleCenter) · pad (0,0,5,0)`、容器 `Timer` = `[648.33,1008.55]–[1271.67,1069.45]`。
            //   ⇒ `start = 648.33 + (623.34 − 43.91 − txW) / 2`、图标紧随、文字紧跟图标（⛔ 不是把它钉在 981.95）。
            //   ⚠️ 量不到（helper 已出声）⇒ 保留建树时的模板位，⛔ 不拿 0 当宽去排。
            if (txW > 0f)
            {
                float content = TimerIcR.W + txW;                 // spacing = 0
                float x1 = TimerR.x1 + (TimerR.W - content) * 0.5f;
                var icFit = new PxRect(x1, TimerIcR.y1, x1 + TimerIcR.W, TimerIcR.y2);
                var txFit = new PxRect(icFit.x2, TimerTxY1, icFit.x2 + txW, TimerTxY2);
                if (iconQ != null)
                    iconQ.transform.localPosition =
                        MenuDraw.Local(tm, icFit.x1, icFit.y1, icFit.x2, icFit.y2);
                // 文字是 `Center/Capline`（框 = 字宽 ⇒ 居中与左对齐同解）⇒ 直接摆到新框的中心。
                if (tx != null)
                    tx.transform.localPosition =
                        MenuDraw.Local(tm, txFit.x1, txFit.y1, txFit.x2, txFit.y2);
                tx.SetAutoFitBox(LayoutSpace.Px(txFit.W), LayoutSpace.Px(txFit.H), 10f, 38f, 38f);
                // ⚠️ 上面那一句内部会调 `SetWrapWidth` ⇒ **无条件把模式开成 `Normal`**，而原版这一颗是
                //   **折行 = 0** ⇒ 紧跟着还原（成对写法同 A404 / A205 / A34-F4 那一族，这里与上面那次同源）。
                tx.SetWrapping(false);
            }
            // 🔴 **2026-10-17（N2）**：接上全工程唯一的倒计时口径 `TimerDisplay`（本文件头 `本文件` 自己写着
            //   原版是 `TimerDisplay.Initialize(endTime)`）。那个 `endTime` = LiveOps 事件的截止时刻，
            //   **由服务端下发、本地没有** ⇒ 🔴 **2026-10-30（`B99_A4`）目标取自类尾 `EnergyTimerDeadline`**（本地恒 `null` ⇒ 这一行**整棵不建**，见 `Build()` 那道闸；⛔ 不印 `--`、⛔ 不编日期）。
            //   ⚠️ **挂的时机必须在 `PreferredWidthPx` 之后** —— 那一格量的是 prefab 出厂串的 preferred 宽
            //   （A1178 已复刻）；先挂会按倒计时串把框重新量窄，`Editor/ShellScene.cs` 那条
            //   「框宽 − 渲出宽 ≥ 1.5px」当场红。
            //   ⚠️ 原版这颗还开了 `useLastMinutesText = 1`（键 `Event_Description/Generic/EndsSoon`）。
            //   🔴 **2026-10-19（界面线 P05 · 铁律 5 就地订正）**：这里原来写「该键正文也**不在**官方快照里
            //   ⇒ 最后 5 分钟档**暂不接**」—— **已过期**：当前官方表里 `EndsSoon` = en `Ends soon` /
            //   zh `即将结束` ⇒ 这一档现已接上（`TimerDisplay.Attach` 的 `lastMinutesPrefix`）。
            if (tx != null)
                TimerDisplay.Attach(tx, () => EnergyTimerDeadline, TimerPrefix, "单机能源活动窗 Timer",
                                    "活动截止时刻由服务端 LiveOps 事件下发，本地没有",
                                    lastMinutesPrefix: Loc.T("Event_Description/Generic/EndsSoon"));
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
                                     41.1f, 18f, QText, 50f, 36f, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(a, InsScoreR);
            // `Energy Instructions`：**按原版 `ShowCurrentEnergyRegenText.OnEnable` 填两个参数**（判据 → 文件头那一段）。
            //   ⛔ 别再把 prefab 出厂那句英文抄成常量 —— 它把「1 Energy」写死、只有一个 `{0}`(minute)，
            //      与官方模板 `… {0} Energy every {1}.` 的参数序**不对应**（照抄会印出 `every 5m minutes`）。
            var b = MenuDraw.TextBox(ins, InsEnergyR, EnergyRulesText(), Color.white, "Energy Instructions",
                                     44.35f, 18f, QText, 72f, 50f, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(b, InsEnergyR);
            // `Scoring Icon`：原版 `m_Sprite` 出厂是 **null**，运行时由 `GetScoringIconFromEvent()` 灌：
            //   `SinglePlayerOnlyEnergyEventWindow.cs:407-455`（现读）—— 事件计分物是 `ScoringPoint` 时
            //   **写死** `ScriptableObjectsCollectionsUtilities.GetScoringIcon(ScoringItemType.Skulls)`。
            //   本窗 = 单机计分（能量）事件 ⇒ 这张就是 **Skulls** 图；映射与判据 → `Shell/ScoringIcons.cs`。
            //   ⚠️ 本地没有事件数据，「计分物是不是 `ScoringPoint`」这一格判不了 ⇒ 按本窗类型**画 Skulls**
            //      （原版这条在本窗类里也没有第二档）。缺图时照旧出声（`Art()` 会记账）。
            var sci = ScoringIcons.Icon(ScoringIcons.ItemType.Skulls);
            if (sci != null) MenuDraw.Rect(ins, sci, InsIconScR, "Scoring Icon", QArt);
            else MenuDraw.Node(ins, "Scoring Icon", InsIconScR);
            // `Energy Icon`：`40k_topmarquee_currency_energy` 90×90 —— 🔴 **本仓 `Resources/` 里没有这张图**
            //   （源在 `bundle_boosterpacks_assets_all/Sprite/`，切好的 PNG 在
            //    `d:/2/Warpforge_tools/data/ui_extract/boosterpacks_assets_all/Sprite/`）
            //   ⇒ 节点照建、这一格**不画**，`Art()` 会记账进 `MissingArt` 并出声（⛔ 不拿别的图顶上）。
            var en = Art(ArtEnergy);
            if (en != null) MenuDraw.Rect(ins, en, InsIconEnR, "Energy Icon", QArt);
            else MenuDraw.Node(ins, "Energy Icon", InsIconEnR);
            if (sci == null)
                Debug.LogWarning("[Energy] `Scoring Icon` **没画** —— `Resources/Art/` 取不到 `40k_battle_Win_Skull`"
                               + "（原版 `GetScoringIcon(ScoringItemType.Skulls)` 的图；映射 → `Shell/ScoringIcons.cs`）");
            else
                Debug.Log("[Energy] `Scoring Icon` = **Skulls 图**（原版 `GetScoringIconFromEvent` 在本窗类里写死 Skulls）。");
        }

        // ------------------------------------------------------------ `Energy Instructions` 的两个参数
        /// <summary>`EnergyConfig.rechargeAmount` 的**本 build 序列化默认**（`cpp2il/EnergyConfig.cs:8`：
        /// `private int rechargeAmount = 1;`）。服务端不可达 ⇒ 这就是能拿到的唯一真值，⛔ 别按手感改。</summary>
        const int EnergyRechargeAmount = 1;

        /// <summary>`EnergyConfig.rechargeCooldown` 的**本 build 序列化默认**（`cpp2il/EnergyConfig.cs:9`：
        /// `private long rechargeCooldown = 300000L;` ⇒ 300 000 ms = 5 分钟）。</summary>
        const long EnergyRechargeCooldownMs = 300000L;

        /// <summary>原版词条键（`bundle_menus_assets_all` 的 `localizationKey` 现读；13 语全有）。</summary>
        const string EnergyRulesKey = "Event_Description/ScoringEvent/EnergyRules";

        /// <summary>`Energy Instructions` 那颗字的正文。
        /// <para>原版 `ShowCurrentEnergyRegenText__OnEnable.c`：`string.Format(GetTranslation(key),
        /// EnergyConfig.rechargeAmount, FormatTimeSpan(FromMilliseconds(rechargeCooldown), showDays:1, hide0Values:1))`。</para>
        /// <para>⚠️ 词条缺键（`Loc.T` 会把键名原样回给我们）⇒ 退回 **prefab 出厂原文**并**出声一次**
        /// （⛔ 不静默印出花括号；那条出厂原文的 `{0}` 是 minute，所以退回时**不格式化**）。</para>
        /// <para>⚠️ **如实标注的残余**：`{1}` 走 `TimerDisplay.FormatTimeString`，而它那三条 `*_Only` 格式串
        /// （`M_Only` 等）是**我们按单位机械拼的**（原版 `M_Only` 的格式串没解出，见 `TimerDisplay.cs` 的注释）
        /// ⇒ **数值与档位对**，单位文案可能有细微差别（`5m` vs 原版某种写法）。</para></summary>
        static string EnergyRulesText()
        {
            const string Fallback = "Energy is consumed when you begin a match with one of the participating "
                                  + "factions. You recover 1 Energy every {0} minutes.";
            string tpl = Loc.T(EnergyRulesKey);
            if (string.IsNullOrEmpty(tpl) || tpl == EnergyRulesKey || !tpl.Contains("{0}"))
            {
                if (!_warnedEnergyRules)
                {
                    _warnedEnergyRules = true;
                    Debug.LogWarning("[Energy] 官方词条 `" + EnergyRulesKey + "` 取不到 ⇒ `Energy Instructions` "
                                   + "退回 prefab 出厂原文（含 `{0}`）");
                }
                return Fallback;
            }
            string every = TimerDisplay.FormatTimeString(
                System.TimeSpan.FromMilliseconds(EnergyRechargeCooldownMs),
                eventTimer: true, clampTo0: false, hide0Values: true);
            return string.Format(tpl, EnergyRechargeAmount, every);
        }
        static bool _warnedEnergyRules;

        // ------------------------------------------------------------ 阵营选择
        void BuildArmySelector(Transform root)
        {
            // 🔴 **2026-10-28（B93-E29）就地收口**：这里原来内联摆了 `Army Selector Panel` / `Factions Title` /
            //   `Army Selector` / `Viewport` / `Army Content` / `ArmyItemReference` / `Faction instructions`
            //   七个节点，与 `Shell/ShowEventArmiesPanel.cs` 的 `Build` 是**两处写同一条规则**（CLAUDE.md §三）
            //   ⇒ 现在只留组件那一份，本窗只接返回值（逐格几何判据 → 那个文件的文件头，全部 prefab 现读）。
            //   ⚠️ 本文件上面那组常量（`AspR` / `FacTitleR` / `ArmySelR` / `ArmyContentR` / `FactionInsR`）
            //   **留着**：它们是 prefab 现读的**取证记录**，⛔ 不要在任何地方照它们再摆第二份节点。
            // 🔴 **队列档必须传本窗的**（`QArt` 3939 / `QArt1` 3940 / `QText` 3941）：组件缺省是 LiveOps 那一档
            //   （3111 / 3112 / 3114），全部**在压暗层 `QShade`（3935）之下** ⇒ 阵营徽记会被压暗层盖住。
            //   透明队列按「到相机的 3D 距离」排、不看 z（CLAUDE.md §三）⇒ 这类错**静默**，所以这里显式给。
            // ⚠️ `Factions Title` 的字体面：D1 那份写 `LatinCardBody`、本窗原来写 `LatinUi`，两版判据冲突，
            //   而两者都没落到「prefab 那一颗 TMP 的 `m_fontAsset` 到底是哪一份」⇒ 取**本窗现状**（`LatinUi`），
            //   ⛔ 不在这一笔里顺手改渲染（那会让 `Editor/ShellScene.cs` 的 A1190 那条 fitCase 换一把尺子）。
            // ✅ `ShowEventArmiesPanel.Build` 里 `Viewport` 走的是 `ViewportClip.Hang`（原版 `RectMask2D`）——
            //   这一档是**本笔补的**（D1 那版用裸 `MenuDraw.Node`，13 个阵营时会溢出视口、不裁）。
            // ✅ 那颗 `Army Selector` 的**横向滚动**也在组件里接好了（`ShowEventArmiesPanel.EnsureScroll`）：
            //   本窗不再自己建 `MenuScroll`，也不改几何（⛔ 别为了「13 颗摆得下」缩格 —— 原版就是滚）。
            //   `MenuScroll` 本来就是**双轴**件（`Vertical` 缺省 false = 横向）⇒ 这一笔只是接线，
            //   ⛔ 没有动 `Shell/MenuScroll.cs`（共用件，多扇窗在用）。
            // ⚠️ 原来那段「`Army Content` 下只有模板那一颗」的出声也一并搬进组件（`Initialize`/`RebuildItems`），
            //   本窗不再重复打一遍（两处都在打 = 日志里看出「两次初始化」的假象）。
            _armiesPanel = ShowEventArmiesPanel.Build(root, QArt, QArt1, QText);
            _armySelector = _armiesPanel != null ? _armiesPanel.Selector : null;
            _armyContent  = _armiesPanel != null ? _armiesPanel.Anchor   : null;
            if (_armiesPanel == null)
                Debug.LogWarning("[Energy] `Army Selector Panel` **没建出来** ⇒ 阵营行与它的滚动都会缺席（出声，⛔ 不静默）");
            else
                Debug.Log("[Energy] `Army Selector Panel` 由 `ShowEventArmiesPanel.Build` 建（组件唯一一份）；出厂仍只有"
                        + "模板 `ArmyItemReference`，真阵营由 `InitializeEventArmies()` 从 `RewardEventHandler` 那条链灌"
                        + "（原版 `SinglePlayerOnlyEnergyEventWindow__Open.c:114`）");
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
        /// ⛔ **不是框摆错了** ⇒ 无需改本文件。
        ///
        /// <para>🔴 **2026-10-21（`A1262` / `T07` 尾巴 · 铁律 5）就地补：上面那个「≤1.47em」只是【上界】，
        /// 原版的真值已经查到了 = **1.293 em** —— 差的那一格在字体资产的 `m_Scale`。**</para>
        /// <list type="bullet">
        /// <item>原版那份 CJK 资产 =
        /// `d:/2/新解包资源/assets_full/bundle_fonts_assets_all/MonoBehaviour/NotoSerifCJK-Regular SDF.json`：
        /// `m_FaceInfo` = `m_PointSize 50` · **`m_Scale 0.9`** · `m_LineHeight 71.85` · `m_AscentLine 57.55` ·
        /// `m_DescentLine −14.3` ⇒ face 行盒 = 71.85 ÷ 50 = **1.437 em**；TMP 再乘 `faceInfo.scale`
        /// （`Library/PackageCache/com.unity.ugui@…/Runtime/TMP/TMP_Text.cs:4188/4190`：
        /// `adjustedScale = fontSize / pointSize × faceInfo.scale × …`）
        /// ⇒ **有效行盒 = 1.437 × 0.9 = 1.2933 em**。</item>
        /// <item>我们那份 = `Resources/Fonts/NotoSerifCJK-Regular SDF.asset` 的 `m_FaceInfo:134` **`m_Scale: 1`**
        /// ⇒ 1.437 em —— `Editor/TmpSetup.cs` 的烘焙参数抄了 pointSize / padding / atlas / renderMode /
        /// populationMode，**独独漏了这一格**（那份注释列的参数表里也没有它）。</item>
        /// <item>原版**运行期**旁证（中文 TMP，`资料/原版参照图/Unity参照管线_0825/data/panel_0914b/`）：
        /// `p3_choose_tree.tsv` 的 `ContinueText` = `rect.height 43.62 ÷ fontSize 33.7` = **1.2944**（作者摆的固定框）；
        /// `p3_mulligan_tree.tsv` 的 `MulliganText/Text` 69.44 ÷ 53.7 = **1.2931**、
        /// `MulliganText/TurnText` 54.17 ÷ 41.8 = **1.2959**（这两颗的高度由父件 79.44 撑出来，
        /// 而父件 79.44 = 53.7 × 1.2933 + 10 ⇒ 同一把行盒）。</item>
        /// </list>
        /// <para>🔴 **2026-10-21（`A1262` 已落地 · 铁律 5 就地订正）—— 上面那句「外推 `1.517 × 0.9 = 1.365` ⇒
        /// 会重新收敛到 38 / 53.5」以及「全部 CJK 站点实渲缩 ≈10%」【都不成立】，错因 = 把 `fontSize` 当成了不变量。**</para>
        /// <list type="bullet">
        /// <item>✅ **已落地的部分**：资产 `Resources/Fonts/NotoSerifCJK-Regular SDF.asset` 的 `m_Scale` 已改 **0.9**，
        /// `Editor/TmpSetup.cs` 建完也补了同一刀（`var fi = fa.faceInfo; fi.scale = 0.9f; fa.faceInfo = fi;`）
        /// ⇒ 这一格与原版一致。断言 → `Editor/ShellScene.cs` 的 `③′`（`A1262` 那组，**读资产字段**）。</item>
        /// <item>❌ **渲染结果逐位不变**：本工程的 px 口径 = **汉字墨高**
        /// （`Label.FontSizeToPx` = `fontSize × TmpFont.WorldGlyphPerFontSize × 108`，后者是**运行时实测**的
        /// `TmpFont.MeasureGlyph('国')`）。`SetAutoFitBox` 里 `fontSizeMax = maxPx ÷ (Wglyph × 108)`，
        /// 而一行字的世界高 = `fontSize × face 行盒 × m_Scale × 0.1` ⇒ `Wglyph` 与行盒**同时**乘 `m_Scale`
        /// ⇒ **乘积与 `m_Scale` 无关**。实测坐实：渲出比 **1.52 = 1.437 ÷ 0.948** —— 分子分母都含 `m_Scale`、约掉了
        /// （这正是那四条读数 1.5170 / 1.5252 / 1.5470 / 1.5252 的来历：它们量的**不是「我们的行盒」**，
        /// 而是**行盒 ÷ 汉字墨高**）。</item>
        /// <item>⇒ **真正的残差是【口径】，不是缺 `m_Scale`**：我们的「px」= 汉字墨高，而原版 `m_fontSize` 对应的
        /// 墨高只有 `0.948 × 0.9 = 0.853 × px` ⇒ 同一句话我们排出来比原版大 **≈17%**。要回到 38 / 53.5 得改**口径**
        /// （`EmPerFontSize(Cjk)` 换成真 em）或**逐站改 `minPx/maxPx` 实参**（全仓 510+ 站、实渲缩 ≈15%），
        /// 属**另一笔**、须单独一批 + 真机并排验收。⛔ 别在本文件里单独挪某一站的字号去「凑回 38」。</item>
        /// </list>
        /// <para>⛔ **本文件一个字都不用改**（框、`PreferredWidthPx`、`A1178` 的两条关系式都与 `m_Scale` 无关）。
        /// 判据全文 → `资料/普查产出_1021/W61b_界面_A1262_mScale.md`。</para></summary>
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

        // ============================================================ 🆕 2026-10-27（B88 · E11）
        //   **第二态：分数 / 里程碑 / 领取**（原版 `RefreshScore` / `CollectClicked` / `TryCollect`）
        //
        // 判据（逐句，本地都可读 —— ⛔ 别去别处找第二份）：
        //  · `decomp_full/SinglePlayerOnlyEnergyEventWindow__RefreshScore.c`（无参那支 = 转调有参那支的入口）
        //    ＋ `SinglePlayerOnlyEnergyEventWindow.__c__DisplayClass13_0___RefreshScore_b__0.c`（状态机）
        //  · `cpp2il/out_cpp2il_dev_ilspy/MilestoneScoreBar.cs`（`SetProgressBar`：`maxValue = 末档` ·
        //    `normalizedValue = InterpolateMilestones(score, [0, 阈值…])`）
        //  · `cpp2il/out_cpp2il_dev_ilspy/RewardProgressBarPanel.cs`（`Initialize`）：
        //    `collectButton.interactable = 任一 state == Unlocked` · `scoreBar.Initialize(...)` ·
        //    `scoreText.text = currentScore` · `scoringImage.sprite/enabled` · `scoringTitleText.text`
        //  · `cpp2il/out_cpp2il_dev_ilspy/GenericScoreBarLine.cs`（`Initialize`）：
        //    分数格 = `milestone.score`；**state 决定开箱图与「可领高亮」**：
        //      `Locked`    ⇒ 闭箱 + 高亮关
        //      `Unlocked`  ⇒ 闭箱 + **高亮开**（`readyToCollect.SetActive(true)`）
        //      `Collected` ⇒ **开箱**（`chestOpen`）+ 高亮关
        //
        // 🔴 状态机（`_.c` 里 `iVar1 = (param_3 < collectedRewards) + 1`）逐字是：
        //      `currentScore < r.scoreToUnlock` ⇒ **Locked(0)**
        //      否则 ⇒ **`i < collectedRewards` ? Collected(2) : Unlocked(1)**
        //    ⚠️ 判「已领」用的是**序号**（`i < collectedRewards`），**不是**逐档布尔表。
        //
        // 🔶 我们自己定的两处（都按铁律 18 标在这里，因为原版那两半不可离线复刻）：
        //  ① **分数/已领数的来源** —— 原版读 `SinglePlayerScoringEvent`（LiveOps 事件实例 + 服务器存档）；
        //     本机 13 份活动 SO **全部过期**、没有在期事件实例 ⇒ `ScoreSource` 默认 **null**，
        //     `RefreshScore()` 走「没有数据源」那支（出声一次，界面保持 prefab 出厂值）。
        //     自检/将来接上本地存档时，给它一个委托即可（判据形状 = `EnergyScoreState`）。
        //  ② **领取事务**（`SinglePlayerScoringEvent.TryColllect`）—— 原版是服务器事务；本地等价口径
        //     （线与 `B9_日常C_施工单.tsv` 第 706/716/717 行已定：服务器事务 → 本地存档 + 出声）：
        //     走 `TryCollect`，没有数据源 ⇒ `onError`；**奖励表本机没有**（`RewardService.Collect` 那一层
        //     未复刻）⇒ 成功路只推进「已领档位」并出声，⛔ 不编奖励、不假装发了东西。
        public struct EnergyScoreState
        {
            /// <summary>当前分（原版 `RefreshScore` 里那句 `currentScore`）。</summary>
            public int CurrentScore;
            /// <summary>5 档解锁分（原版 `ScoringMilestones[i].scoreToUnlock`）。</summary>
            public int[] Thresholds;
            /// <summary>**已领档数**（原版 `collectedRewards`；状态机按「序号 &lt; 它」判已领）。</summary>
            public int CollectedRewards;
        }

        /// <summary>活动分数来源（原版 = `SinglePlayerScoringEvent` 的存档读口）。默认 null = 本机没有。 🔶 自己设计</summary>
        public static System.Func<EnergyScoreState> ScoreSource;

        // 建树时留下的那三组件（`BuildMilestones` 里 Add；`Build()` 开头清空）
        readonly List<Label> _scoreLabels = new List<Label>();
        readonly List<ImageQuad> _crateHi = new List<ImageQuad>();
        readonly List<ImageQuad> _chests = new List<ImageQuad>();
        readonly List<int> _thresholds = new List<int>();
        WindowButton _collectBtn;
        int _currentScore;
        int _collectedRewards;
        bool _warnedNoScoreSource;

        /// <summary>当前分（原版 `RefreshScore` 读到的那个数；出厂 0 = 没喂过）。</summary>
        public int CurrentScore { get { return _currentScore; } }
        /// <summary>已领档数（状态机的第二个输入）。</summary>
        public int CollectedRewards { get { return _collectedRewards; } }
        /// <summary>有没有「已达未领」的档（原版 `RewardProgressBarPanel.HasMilestoneToCollect`）——
        /// 它决定 `Collect` 钮的 `interactable`。</summary>
        public bool HasMilestoneToCollect
        {
            get
            {
                for (int i = 0; i < _thresholds.Count && i < _scoreLabels.Count; i++)
                    if (_currentScore >= _thresholds[i] && i >= _collectedRewards) return true;
                return false;
            }
        }

        // ------------------------------------------------------------ 倒计时的显隐（原版 `TryOpen` 那两行）
        /// <summary>原版 `SinglePlayerOnlyEnergyEventWindow.TryOpen(object, GameWindowOptions)` 的两句：
        /// `timerDisplay.gameObject.SetActive(!(baseData.activationType == Permanent));`
        /// 且**只有非 Permanent** 才 `timerDisplay.Initialize(结束时刻)`。
        /// <para>判据 = `cpp2il/out_cpp2il_dev_ilspy/SinglePlayerOnlyEnergyEventWindow.cs` 的 `TryOpen`。
        /// ⚠️ `activationType` 来自 **LiveOps 事件实例**（服务器数据）；本机 13 份 SO 全过期、没有在期实例
        /// ⇒ 由宿主设 <see cref="PermanentEvent"/>；**默认 false**（= 非 Permanent ⇒ 计时器照显，
        /// 与改前完全一致，⛔ 不改变今天的行为）。</para></summary>
        public bool PermanentEvent;

        /// <summary>把 <see cref="PermanentEvent"/> 落到界面上：Permanent ⇒ `Timer` 整件关掉。
        /// ⛔ 不碰 `Timer` 里的文字（原版也是整件 `SetActive`）。</summary>
        public void ApplyTimerVisibility()
        {
            // `Timer` 是 `BuildTimer` 建在本窗根下的**直接子件**（`MenuDraw.Node(root, "Timer", TimerR)`）。
            var t = transform.Find("Timer");
            if (t == null)
            {
                // 🔴 **2026-10-30（`B99_A4`）**：`Timer` 现在**只在 `EnergyTimerDeadline.HasValue` 时建**（原版 `TryOpen` 也只在
                //   `activationType != 0` 时才 `Initialize`）⇒ 本地无截止时刻 ⇒ 节点**本来就不存在** = 已经「藏」了 ⇒ ⛔ 不报警。
                return;
            }
            t.gameObject.SetActive(!PermanentEvent);
        }

        /// <summary>原版 `private void RefreshScore()`：读事件分数 → 投射成 `GenericScoringMilestone[]` →
        /// `rewardsBarPanel.Initialize(...)`。**没有数据源 ⇒ 出声一次并把界面留在出厂态**（⛔ 不编分数）。</summary>
        public void RefreshScore()
        {
            if (ScoreSource == null)
            {
                if (!_warnedNoScoreSource)
                {
                    _warnedNoScoreSource = true;
                    Debug.Log("[Energy] `RefreshScore()` 没有数据源（原版读 `SinglePlayerScoringEvent` 的服务器存档；"
                            + "本机 13 份活动 SO 全过期、没有在期事件实例）⇒ 界面保持 prefab 出厂值（出声一次）。");
                }
                return;
            }
            var st = ScoreSource();
            SetScore(st.CurrentScore, st.Thresholds, st.CollectedRewards);
        }

        /// <summary>把 `RefreshScore` 那个投射**落地**（= `MilestoneScoreBar.Initialize` ＋ `RewardProgressBarPanel.Initialize`
        /// 的净效果）：分数格换成阈值、箱子闭/开、可领高亮、进度条、`Collect` 钮 `interactable`。
        /// <param name="thresholds">5 档解锁分（原版 `ScoringMilestones[i].scoreToUnlock`）。</param>
        /// <param name="collectedRewards">已领档数（状态机第二个输入）。</param></summary>
        public void SetScore(int currentScore, IList<int> thresholds, int collectedRewards)
        {
            _currentScore = currentScore; ApplyScorePanel(currentScore);
            _collectedRewards = collectedRewards < 0 ? 0 : collectedRewards;
            _thresholds.Clear();
            int n = thresholds == null ? 0 : Mathf.Min(thresholds.Count, _scoreLines.Count);
            for (int i = 0; i < n; i++) _thresholds.Add(thresholds[i]);

            // 🆕 2026-10-29（`B97_MINI2` · 账 `未分类_其他:430`）：**这一跳改走真组件** ——
            //   原版 `SinglePlayerOnlyEnergyEventWindow.RefreshScore` 的产物就是 `GenericScoringMilestone[]`，
            //   交给 `RewardProgressBarPanel.Initialize` → `MilestoneScoreBar.Initialize`（两个类本体已复刻，
            //   判据见 `报告/B94_B17.md`）。原来这里是「手工逐件写」的等价族（分数 / 箱子 / 高亮 / 进度），
            //   现在改成喂组件 —— **几何与外观逐件同前**（`GenericScoreBarLine.Initialize` 写的正是那四件）。
            //   ⚠️ 状态机没搬走：它仍是原版那条（`currentScore < scoreToUnlock ⇒ Locked`，
            //   否则 `i < collectedRewards ? Collected : Unlocked`），实现收在
            //   `Shell/MilestoneLocalSimulation.cs` 的 `FromScore`（⛔ 不在本窗重写第二份）。
            var ms = CardPresentation.ScoreBars.MilestoneLocalSimulation.FromScore(currentScore, _thresholds, _collectedRewards);
            if (_scoreBar != null) _scoreBar.Initialize(currentScore, ms, null);
            else Debug.LogWarning("[Energy] `MilestoneScoreBar` 没建（`_scoreBar == null`）⇒ 5 行里程碑没刷"
                                + "（原版这一跳就在组件上；⛔ 不静默）");
            PinChestAspect();      // 换图会把 `_aspect` 拉成贴图比例 ⇒ 钉回框的比例（见 `PinChestAspect` 的 doc）

            // 原版 `RewardProgressBarPanel.HasMilestoneToCollect` = 「有没有一档 state == Unlocked」。
            bool anyClaimable = RewardProgressBarPanel.HasMilestoneToCollect(ms);

            // 原版 `MilestoneScoreBar.InitializeLines` 同一处只打 `[ERROR] … more scoring lines that thresholds` 然后**停**
            // ⇒ 没有阈值的那几行**不动**（保持出厂）。本窗再出声一条（说明我们这边的行数从哪来）。
            if (n < _scoreLines.Count)
                Debug.LogWarning("[Energy] 里程碑阈值只有 " + n + " 档、记分条有 " + _scoreLines.Count
                        + " 行（原版 `MilestoneScoreBar` 同一处只报错并停止）⇒ 余下几行保持出厂值。");

            // 进度：原版 `slider.maxValue = 末档` · `normalizedValue = InterpolateMilestones(score, [0, 阈值…])`。
            // 🔴 这个数现在**由组件算**（`MilestoneScoreBar.SetProgressBar`）⇒ 直接从 `Slider` 读回来，
            //    ⛔ 不在本窗再算一遍（那正是「两处写同一条规则」）。
            SetProgress(_scoreSlider != null
                        ? _scoreSlider.normalizedValue
                        : AllianceEventScoreInfo.InterpolateMilestones(currentScore, _thresholds.ToArray()));

            if (_collectBtn == null && _collect != null)
                _collectBtn = _collect.GetComponentInChildren<WindowButton>(true);
            if (_collectBtn != null) _collectBtn.Interactable = anyClaimable;
        }

        /// <summary>原版 `private void RefreshScore(RewardInfo[] obj)`：**只是转发**到无参那支
        /// （`SinglePlayerOnlyEnergyEventWindow__RefreshScore.c` = 一句 `RefreshScore()`）——
        /// 它是 `TryColllect` 的成功回调。奖励表在这里**不用**（原版也不用）。</summary>
        public void RefreshScoreCallback(CampaignData.RewardSpec[] rewards) { RefreshScore(); }

        /// <summary>原版 `private void CollectClicked()`：`TryColllect(onComplete: RefreshScore)`。
        /// 失败/没有数据源 ⇒ `onError` ⇒ 沿用本窗原来的「暂无服务器」那条如实提示（`OnCollectClick`）。</summary>
        public void CollectClicked()
        {
            TryCollect(RefreshScoreCallback,
                       why => { Debug.LogWarning("[Energy] `TryCollect` 失败：" + why); OnCollectClick(); });
        }

        /// <summary>= 原版 `SinglePlayerScoringEvent.TryColllect(Action&lt;RewardInfo[]&gt; onComplete, Action&lt;string&gt; onError)`。
        /// <para>原版 = **服务器事务**（`ScoringEvent&lt;…&gt;` 的云脚本，本地无后端）。🔶 本地等价口径（见本段头部）：
        /// 没有数据源 ⇒ `onError`；有数据源 ⇒ 领**第一档已达未领**、把「已领档数」推进 1，再 `onComplete(空表)`。</para>
        /// <para>⚠️ **奖励表本机没有**（`RewardService.Collect` / `ItemDrawer` 那条发放链未复刻）⇒ 成功路给**空表**
        /// 并出声，⛔ 不编奖励条目、⛔ 不假装「已发到背包」。</para></summary>
        public void TryCollect(System.Action<CampaignData.RewardSpec[]> onComplete,
                               System.Action<string> onError)
        {
            if (ScoreSource == null)
            {
                string why = "本机没有活动分数来源（原版 `TryColllect` 走 LiveOps 后端；"
                           + "13 份活动 SO 全过期 ⇒ 没有在期事件实例）";
                if (onError != null) onError(why); else Debug.LogWarning("[Energy] TryCollect 失败：" + why);
                return;
            }
            var st = ScoreSource();
            int idx = st.CollectedRewards;
            if (st.Thresholds == null || idx >= st.Thresholds.Length || st.CurrentScore < st.Thresholds[idx])
            {
                string why = "没有可领的里程碑（当前分 " + st.CurrentScore + "、已领 " + st.CollectedRewards + " 档）";
                if (onError != null) onError(why); else Debug.LogWarning("[Energy] TryCollect 失败：" + why);
                return;
            }
            Debug.Log("[Energy] `TryCollect` 本地等价：领取第 " + (idx + 1) + " 档（分数 " + st.Thresholds[idx]
                    + "）—— 奖励发放链（`RewardService.Collect`）本机没有 ⇒ 只推进已领档位、奖励表给空表。");
            SetScore(st.CurrentScore, st.Thresholds, idx + 1);
            if (onComplete != null) onComplete(new CampaignData.RewardSpec[0]);
        }

        /// <summary>= 原版 `SinglePlayerScoringEvent.PurchaseMilestone(int, Action&lt;RewardInfo[]&gt;, Action&lt;string&gt;)`：
        /// 单档领取（`TryColllect` 是「领下一档」的一次性入口）。本地口径同 `TryCollect`。</summary>
        public void PurchaseMilestone(int milestoneIndex, System.Action<CampaignData.RewardSpec[]> onComplete,
                                      System.Action<string> onError)
        {
            if (ScoreSource == null)
            {
                string why = "本机没有活动分数来源（服务器事务）";
                if (onError != null) onError(why); else Debug.LogWarning("[Energy] PurchaseMilestone 失败：" + why);
                return;
            }
            var st = ScoreSource();
            if (st.Thresholds == null || milestoneIndex < 0 || milestoneIndex >= st.Thresholds.Length)
            {
                string why = "里程碑下标 " + milestoneIndex + " 越界";
                if (onError != null) onError(why); else Debug.LogWarning("[Energy] PurchaseMilestone 失败：" + why);
                return;
            }
            if (st.CurrentScore < st.Thresholds[milestoneIndex] || milestoneIndex < st.CollectedRewards)
            {
                string why = "里程碑 " + milestoneIndex + " 不可领（分 " + st.CurrentScore
                           + " / 阈值 " + st.Thresholds[milestoneIndex] + " / 已领 " + st.CollectedRewards + " 档）";
                if (onError != null) onError(why); else Debug.LogWarning("[Energy] PurchaseMilestone 失败：" + why);
                return;
            }
            SetScore(st.CurrentScore, st.Thresholds, Mathf.Max(st.CollectedRewards, milestoneIndex + 1));
            if (onComplete != null) onComplete(new CampaignData.RewardSpec[0]);
        }

        // ============================================================ 窗头组件接线（B89-E29 · 追加于类尾）
        /// <summary>接线到原版那颗 `WindowHeaderWithBackButton`
        /// （原版 `SinglePlayerOnlyEnergyEventWindow` 持有它并 `Initialize(liveopEvent, onBack)`）。
        /// 返回钮 = `QuadInOwnNode` 那一档 ⇒ `WindowButton` 在 `BackHit` 命中区节点上。
        /// <para>⚠️ 放在类尾是照「新成员追加类尾」的纪律（⛔ 插进已有成员之间会让账的行号整体漂移）。</para></summary>
        void WireHeaderComponent(WindowHeader.Parts p, System.Action onBack)
        {
            ParentEventWindow.EnsureHeaderTitleResolver();
            var backWb = p.BackHit != null ? p.BackHit.GetComponent<WindowButton>() : null;
            WindowHeaderWithBackButton.WireHeader(p.Root, backWb, p.Title, onBack, "Game Mode");
        }

        // ════════════════════════════════════════════════════════════════════════════════════════
        //  B93-5（2026-10-28 · 文件所有者代理 `D2`）
        //  原版 `SinglePlayerOnlyEnergyEventWindow` 的六跳生命周期（`GetScoringIconFromEvent` /
        //  `CloseButtonClick` / `OnEnable` / `OnDisable` / `OnDestroy` / `TryOpen`）
        //  ＋ `ShowEventArmiesPanel`（逐阵营布局）
        //  ＋ `RewardProgressBarPanel` 的 `scoreText` / `scoringImage` / `scoringTitleText` 三格。
        //
        //  【为什么全加在类尾】账按 `文件:行号` 记落点（`:295/:327/:358/:705/:741/:819/:1144/:1148/`
        //    `:1163/:1183/:1234/:1249/:1275/:1307`）⇒ 新成员只能加在**所有老落点之后**；
        //    真要在中间加行，下游那十几条账会一起漂（CLAUDE.md §三「新成员追加到类尾」）。
        //    中间那四处改动都是**同一物理行的就地改写**（`:298` / `:358` / `:705` / `:1185`），行数不变。
        // ════════════════════════════════════════════════════════════════════════════════════════

        // ------------------------------------------------------------ ① `GetScoringIconFromEvent`
        /// <summary>「本窗有没有可查的计分物」——原版读 `LiveOpsUtility.GetLiveOpsData(事件).ScoringItem`。
        /// 🔶 自己设计（非原版）：<「计分物存在」的本地替身> · <为什么必须自定：`ScoringItem` 在远端 LiveOps
        /// 事件数据里，本机 13 份活动 SO 全过期、没有在期实例；而本窗类里 `GetScoringIcon` 的**唯一分支**
        /// 就是「计分物是 `ScoringPoint` ⇒ Skulls」，没有第二档> · <调度台 2026-10-28 裁定（B93-5）>。
        /// <para>默认 `true` ⇒ 与「本窗是单机计分事件」自洽，且与改前的画面逐像素相同；真数据到了改这一格。</para></summary>
        public bool HasScoringItem = true;

        /// <summary>原版 `private bool GetScoringIconFromEvent(out ScoringIconsSO.ScoringIcon scoringIcon)`
        /// （`decomp_full/SinglePlayerOnlyEnergyEventWindow__GetScoringIconFromEvent.c` 全文 6 句，逐句）：
        /// <code>
        /// scoringIcon = null;                                          // ① 先清 out
        /// if (liveOp == null || liveOp.&lt;+0x18&gt; == null) return false;   // ② 事件 / 事件数据为空
        /// var item = eventData.ScoringItem;                             // ③
        /// if (item == null) return false;                               // ④
        /// if (!(item is ScoringPoint)) return false;                    // ⑤ ★ 类型闸（唯一分支）
        /// scoringIcon = GetScoringIcon((ScoringItemType)item[9]);       // ⑥ 本窗类里恒 Skulls
        /// return true;                                                  //   ★ 即便图是 null 也返 true
        /// </code>
        /// <para>⚠️ **类型替换（如实记）**：原版 out 参是 `ScoringIconsSO.ScoringIcon`（SO 内嵌结构，带 sprite+名字），
        /// 我们这一侧那张图走 `Shell/ScoringIcons.cs`（`Texture2D`）⇒ out 参换成 `Texture2D`，语义一格不少。
        /// `ScoringItemType.Skulls` 这一档的映射判据 → `Shell/ScoringIcons.cs` 文件头。</para></summary>
        public bool GetScoringIconFromEvent(out Texture2D scoringIcon)
        {
            scoringIcon = null;                                          // ①
            if (!HasScoringItem) return false;                            // ②③④（本地替身，见上面那格）
            scoringIcon = ScoringIcons.Icon(ScoringIcons.ItemType.Skulls); // ⑤⑥
            return true;                                                  //   原版：图取不到也返 true
        }

        // ------------------------------------------------------------ ② `CloseButtonClick`
        /// <summary>原版 `SinglePlayerOnlyEnergyEventWindow.CloseButtonClick()`：**`Close()`**。
        /// <para>🔴 **判据标注（铁律 5）**：`decomp_full/SinglePlayerOnlyEnergyEventWindow__CloseButtonClick.c`
        /// 的**内容不是它** —— 文件里装的是 `TutorialTipScript__StopTipPermanent`（本仓已知的
        /// 「文件名与内容不符」那一类，⛔ 别拿它当判据）。真身由三处**同一个 delegate** 反证
        /// （`__Open.c` / `__OnEnable.c` / `__OnDisable.c` 里都是 `DAT_18427a338`）：
        /// ① `Open` 把它作 `OnBackButtonPressed` 交给 `WindowHeaderWithBackButton.Initialize`
        /// ② `OnEnable` 把它挂到 `backgroundCloseButton.onClick` ③ `OnDisable` 摘掉
        /// ⇒ 表头返回钮 = 点压暗层 = **同一个处理器**，本类里唯一候选就是它 ⇒ 体 = `Close()`。
        /// （判据全文 → 本文件头 2026-10-13 A795 那段就地订正。）</para></summary>
        public void CloseButtonClick() { Close(); }

        // ------------------------------------------------------------ ③ `OnEnable` / `OnDisable`
        /// <summary>`backgroundCloseButton.onClick` 上那个监听的**0/1 状态**
        /// （= 原版 `AddListener` / `RemoveListener` 之后 `UnityEvent` 里的监听者数）。
        /// <para>默认 `true`：字段初始化器不能引用实例方法，而本窗的等价「监听」在 `Build()` 里就绑好了
        /// ⇒ 出厂即视为在册；`OnDisable` 摘、`OnEnable` 挂回来（原版两跳）。</para></summary>
        bool _shadeClickBound = true;

        /// <summary>监听见没见（自检读口 = 原版 `backgroundCloseButton.onClick` 的监听者数）。</summary>
        public bool ShadeClickBound { get { return _shadeClickBound; } }

        /// <summary>原版 `private void OnEnable()`（`__OnEnable.c` 全文 2 句）：
        /// `if (backgroundCloseButton == null) return; backgroundCloseButton.onClick.AddListener(CloseButtonClick);`
        /// <para>⚠️ 这是 Unity 生命周期口 —— 批处理里 `AddComponent` **不会**触发它
        /// （CLAUDE.md §三那条实测），所以自检要么显式调本方法、要么读 <see cref="ShadeClickBound"/>。</para></summary>
        public void OnEnable() { _shadeClickBound = true; }

        /// <summary>原版 `private void OnDisable()`（`__OnDisable.c` 全文 2 句）：`RemoveListener(CloseButtonClick)`。</summary>
        public void OnDisable() { _shadeClickBound = false; }

        // ------------------------------------------------------------ ④ `OnDestroy`
        bool _liveOpReleased;

        /// <summary>原版 `public void OnDestroy()`（`__OnDestroy.c` 全文 1 句）：
        /// `LiveOpsAssetUtility.ReleaseEventAssets(事件);`。
        /// <para>🔴 **2026-10-29（B95_RD）订正**：原来写「本仓没有等价物 ⇒ 恒空」**已过期** —— `ReleaseEventAssets` 早在
        /// `Shell/LiveOpsLocalCatalog.cs` 实现（账 310 已 A），按离线目录里 `EventComponents` / `EventAssets` 的 GUID 逐条释放；原版**没有** `HasScoringItem` 那道闸（`__OnDestroy.c` 全文 1 句），`LiveOp == null` 时它自己早退。</para>
        /// <para>⚠️ 幂等：Unity 在销毁时调一次，自检也会显式调一次 ⇒ 用 <see cref="_liveOpReleased"/> 去重。</para></summary>
        public void OnDestroy()
        {
            if (_liveOpReleased) return;
            _liveOpReleased = true;
            LiveOpsAssetUtility.ReleaseEventAssets(LiveOp);   // 🔴 2026-10-29（B95_RD）：从「只出声」改成**真释放**（原版全文 1 句）
            // ⛔ 原版**没有** `HasScoringItem` 那道闸（`__OnDestroy.c` 全文就 1 句）；`LiveOp == null` 时
            //    `ReleaseEventAssets` 首句就 `return`（原版同一句）⇒ 不需要额外判空。
        }

        // ------------------------------------------------------------ ⑤ `TryOpen`
        /// <summary>原版 `public override void TryOpen(object data, GameWindowOptions options = null)`
        /// （`decomp_full/SinglePlayerOnlyEnergyEventWindow__TryOpen.c` 全文 3 句，逐句）：
        /// <code>
        /// base.TryOpen(data, options);                                   // ① 开窗本体
        /// int activationType = LiveOpsUtility.&lt;取激活类型&gt;(事件);          // ★ 0 = Permanent
        /// timerDisplay.gameObject.SetActive(activationType != 0);         // ② Permanent ⇒ 藏计时器
        /// if (activationType != 0) timerDisplay.Initialize(事件.GetEndDate());   // ③ 非 Permanent 才灌截止时刻
        /// RefreshScore();                                                // ④ ★ 两支**都会**走到这里
        /// </code>
        /// <para>⚠️ **签名差异（如实记）**：原版第二参 `GameWindowOptions` 在本仓不存在
        /// （`GameWindow.TryOpen` 只有 1 参、且非虚），而原版从事件读到的那一格就是「激活类型」
        /// ⇒ 换成显式 `bool permanent`（= `activationType == 0`，今天由 <see cref="PermanentEvent"/> 提供）。</para></summary>
        public void TryOpen(object data, bool permanent = false)
        {
            PermanentEvent = permanent;      // 原版那个 activationType 的本地替身（默认 false = 非 Permanent）
            Build(); InitializeEventArmies();  // ①（本仓的开窗本体在 Open()，Open 里转调本方法）＋ 原版 `__Open.c:114` 那一跳（灌参战阵营）
            ApplyTimerVisibility();           // ②③（Permanent ⇒ 整件关；节点本来不建时它什么都不做 —— `EnergyTimerDeadline` 为 null ⇒ `Build()` 不建 `Timer`）
            RefreshScore();                   // ④ 原版两支都会走到
        }

        // ------------------------------------------------------------ ⑥ `RewardProgressBarPanel` 三格
        /// <summary>原版 `RewardProgressBarPanel.Initialize(...)` 里那三格（`decomp_full/RewardProgressBarPanel__Initialize.c`）：
        /// <code>
        /// scoreText.text       = score.ToString();                       // +0x30 → 本窗「Total Victories」
        /// scoringImage.sprite  = 传入的 sprite; scoringImage.enabled = (sprite != null);   // +0x38 → 「Skull Victories」
        /// scoringTitleText.text = 传入的 title;                           // +0x40 → 「Victories title」
        /// </code>
        /// <para>节点对位（prefab 现读 `Player victories` 那一行三颗）：
        /// `Total Victories`=`'751'`（一个分数）· `Skull Victories`=骷髅图 · `Victories title`=`'Victories: '`
        /// ⇒ 三个字段与三颗节点**一一对上**（判据：`Score` 那 5 格是 1256、这一颗 751 才是面板的大分数）。</para>
        /// <para>⚠️ **`scoringTitleText`（`param_6`）的取值没闭合**：原版 `RefreshScore` 的**两参重载体没导出**
        /// （`__RefreshScore.c` 只有一参那支在转调它；`SkirmishEventWindow` 那一族调这一格时**传 0**）
        /// ⇒ 我们保持 prefab 出厂词条（`Ranked/Victories` = 「Victories: 」），⛔ 不编一个标题。</para></summary>
        void ApplyScorePanel(int currentScore)
        {
            // `scoreText.text = score.ToString()`（+0x30）
            var sc = _victoriesTotal != null ? _victoriesTotal.GetComponent<Label>() : null;
            if (sc != null) sc.SetText(currentScore.ToString());

            // `scoringImage.sprite/enabled`（+0x38）—— 建树期就按「图取不到 ⇒ 不建 quad」落地，
            //   这里只在**有图而当时没画**时把它补出来（今天恒不触发，出声以便真数据到了能看见）。
            var icon = ScoringIcons.Icon(ScoringIcons.ItemType.Skulls);
            var row = _victoriesTotal != null ? _victoriesTotal.parent : null;
            var skull = row != null ? row.Find("Skull Victories") : null;
            if (icon == null && skull != null && skull.gameObject.activeSelf)
                Debug.LogWarning("[Energy] `scoringImage` 那颗（`Skull Victories`）按原版 `enabled = (sprite != null)` 该关，"
                               + "但节点是开的 —— 判据不一致，请核 `BuildPlayerVictories`。");
            else if (icon == null && skull == null && !_warnedScoreIcon)
            {
                _warnedScoreIcon = true;
                Debug.LogWarning("[Energy] `scoringImage`（`Skull Victories`）：`Resources/Art/` 取不到 Skulls 图 ⇒ "
                               + "原版那一跳 `sprite = null ⇒ enabled = false` ⇒ 这一格**不画**（⛔ 不拿别的图顶上）。");
            }
        }
        static bool _warnedScoreIcon;

        /// <summary>自检读口：`scoreText`（`Total Victories`）现在印的是什么
        /// （= 原版 `RewardProgressBarPanel.Initialize` 第 ① 句写进去的那个 `score.ToString()`）。</summary>
        public string ScorePanelText
        {
            get
            {
                var l = _victoriesTotal != null ? _victoriesTotal.GetComponent<Label>() : null;
                return l != null ? l.Text : null;
            }
        }

        // ------------------------------------------------------------ ⑦ `ShowEventArmiesPanel`（逐阵营布局）
        readonly List<Transform> _armyItems = new List<Transform>();

        /// <summary>本窗建出来的「参战阵营」格数（原版 `ShowEventArmiesPanel.armyItemContainers.Count`，自检读口）。</summary>
        public int EventArmyCount { get { return _armyItems.Count; } }

        /// <summary>原版 `ShowEventArmiesPanel.Initialize(List&lt;int&gt; armies)`
        /// （`decomp_full/ShowEventArmiesPanel__Initialize.c` 全文 7 句，逐句）：
        /// <code>
        /// if (armyItemContainers.Count &gt; 0) Reset();                     // ① 先清旧
        /// template.gameObject.SetActive(true);                            // ② 模板先开（Instantiate 前的原版写法）
        /// foreach (var army in armies) {                                 // ③
        ///     var go = Instantiate(template, template.parent);            // ④ 复制到 `Army Content`
        ///     armyItemContainers.Add(go);                                // ⑤
        ///     go.GetComponent&lt;Image&gt;().sprite = ArmyUtilities.GetArmyIcon(army);   // ⑥
        /// }
        /// template.gameObject.SetActive(false);                           // ⑦ ★ 模板最后收起来
        /// </code>
        /// <para>🔴 **布局判据（prefab 现读，⛔ 不是我们挑的）**：`Army Content` 挂 `HorizontalLayoutGroup`
        /// （`spacing 0` · `align 0`(UpperLeft) · pad 0 · `expandW 1` · `ctrlH 1`）+ `ContentSizeFitter(h:PreferredSize)`；
        /// 每格 **136.501 × 136.501**（= <see cref="ArmyContentR"/> 那一格，`ArmyItemReference` 现读同值）
        /// ⇒ 第 i 格左上角 x = `ArmyContentR.x1 + i × 136.501`。
        /// 我们这套没有 HLG/CSF 运行时 ⇒ 按同一算式手工排（同 `RaidEventWindow.RebuildRewards` 的先例）。</para>
        /// <para>⚠️ **`armies` 从哪来**：原版 = `RewardEventHandler.GetEvents().Where(RewardEvent.IsTargetTo)` 取到
        /// 那个 `RewardEvent` 的阵营表（判据 `SinglePlayerOnlyEnergyEventWindow__Open.c` 中段）。
        /// 本机活动数据 0 份 ⇒ 生产路径今天一次都不会调；本方法只做「给了表就按表排」，
        /// ⛔ 自己不去编一张阵营表（那是编数据）。</para>
        /// <returns>排出来的格数。</returns></summary>
        public int ShowEventArmies(IList<int> armies)
        {
            // 🔴 2026-10-28（B93-E29）：**实现已收口到 `Shell/ShowEventArmiesPanel.cs`**（唯一一份）。
            //   本方法退成「原版那个调用点」本身 —— `SinglePlayerOnlyEnergyEventWindow__Open.c:114` 的宿主侧：
            //   转调组件的 `Initialize`，再把面板里那批节点抄进 `_armyItems`（`EventArmyCount` 的读口）。
            //   ⛔ 别在本窗再写第二份布局算式（改前这里有一整段 `MenuDraw.Rect` 的 HLG 手工推法）。
            //   ✅ 组件那一侧现在还带**横向滚动**与 `ViewportClip`（原版 `RectMask2D`），判据 → 那个文件 §滚动。
            // 📌 收口之后的调用链（自证「新代码真的在跑」）：
            //   `Open()` → `TryOpen()` → `Build()` → `BuildArmySelector()` → `ShowEventArmiesPanel.Build()`
            //   → `InitializeEventArmies()` →（原版那句话）→ 本方法 → 组件 `Initialize` → 组件 `RebuildItems`。
            //   ⇒ 断言只要读组件那边的 `Count` / `ItemSpriteName` / `Scroll`，读到的就是**生产路径**的产物。
            // ⚠️ 顺序照原版：`__Open.c` 里「灌阵营」在 `GetScoringIconFromEvent` **之前**；
            //   我们把它挂在 `Build()` 之后、`RefreshScore()` 之前（`TryOpen` 那一行就地改写，行数不变）。
            if (_armiesPanel == null)
            {
                Debug.LogWarning("[Energy] `ShowEventArmies`：`Army Selector Panel` 没建出来 ⇒ 先 `Build()`（出声，⛔ 不静默）");
                _armyItems.Clear();
                return 0;
            }
            // 原版那句 `First().Data.armyAllowList.ToArray()` 的产物已经由 `InitializeEventArmies` 算好，
            // 这里只做「形状搬运」：`IList<int>` → 组件要的 `int[]`（⛔ 不做语义判断，也不缓存第二份）。
            var arr = armies == null ? new int[0] : new int[armies.Count];
            for (int i = 0; i < arr.Length; i++) arr[i] = armies[i];
            int n = _armiesPanel.Initialize(arr);
            // `EventArmyCount` 的读口副本（原版 `armyItemContainers.Count`）：
            // 组件那一侧才是正本；本窗这一份只给自检读，⛔ 不参与任何行为判断。
            _armyItems.Clear();
            for (int i = 0; i < _armiesPanel.Count; i++)
            {
                var t = _armiesPanel.Item(i);
                if (t != null) _armyItems.Add(t);
            }
            return n;
        }

        /// <summary>原版 `ShowEventArmiesPanel.Reset()`（`decomp_full/ShowEventArmiesPanel__Reset.c` 全文 4 句）：
        /// `foreach (var go in armyItemContainers) Destroy(go.gameObject); armyItemContainers.Clear();`。</summary>
        public void ResetEventArmies()
        {
            // 🔴 2026-10-28（B93-E29）：实现收口到 `ShowEventArmiesPanel.Reset()`（唯一一份）。
            //   原版 `ShowEventArmiesPanel__Reset.c` 四句 = 逐个 `Destroy` + 清表；组件那边逐句照抄过
            //   （含批处理下 `Object.Destroy` 不生效 ⇒ 走 `RewardsWindow.DestroySafe` 那一档的处置）。
            //   ⚠️ `_armyItems` 这一份只是**本窗的读口副本**（`EventArmyCount` 用它），
            //   ⛔ 不是第二份实现 —— 行为一律以组件那一份为准。
            // ✅ 组件 `Reset()` 顺带把滚动偏移归零并把可滚范围收成 0（空表 ⇒ 滚不动），
            //   这是原版那条 `ContentSizeFitter(h:PreferredSize)` 的等价物（内容宽跟着格数走）。
            // ⚠️ 顺序：先清本窗的读口副本、再让组件去销毁真节点 —— 反过来也能跑，但那样
            //   组件 `Reset` 会先 `Destroy` 掉 `_armyItems` 里那几颗（Unity 假 null），
            //   本窗那份副本就会留着一串「非 null 但已死」的引用（自检读到会莫名其妙）。
            _armyItems.Clear();
            if (_armiesPanel != null) _armiesPanel.Reset();
        }

        // ════════════════════════════════════════════════════════════════════════════════════════
        //  B93-E29（2026-10-28 · 执行代理 `E29`）
        //  ① 把 `ShowEventArmiesPanel` **真接线**（原版唯一调用点 = `__Open.c:114`，在本窗 `Open()` 里）
        //  ② 阵营条的**横向滚动**（原版 `Army Selector ▸ ScrollRect`，h=1 v=0）
        //
        //  【为什么全加在类尾】本文件被账按 `文件:行号` 引用（`:295/:298/:327/:358/:705/:741/:819/`
        //   `:1144/:1148/:1163/:1183/:1234/:1249/:1275/:1307/:1349/:1383/:1386/:1396/:1418/:1439/`
        //   `:1464/:1501/:1538`）⇒ 新成员只能加在**所有老落点之后**；中间那三处改动
        //   （`BuildArmySelector` / `ShowEventArmies` / `ResetEventArmies`）都是**等行数就地改写**，
        //   另有一处（`TryOpen` 的 `Build()` 那一行）是同一物理行改写 ⇒ **零行数变化**。
        // ════════════════════════════════════════════════════════════════════════════════════════

        /// <summary>原版那颗 `ShowEventArmiesPanel`（`__Open.c` 的 `this+0xa0`；`Build()` 里建）。</summary>
        ShowEventArmiesPanel _armiesPanel;

        /// <summary>原版 `this`（本窗持有的 `ILiveOps` 事件实例）—— `RewardEvent.IsTargetTo(this)` 的输入。
        /// <para>⚠️ 本地没有在期事件（13 份 SO 全过期）⇒ **默认 null**，`InitializeEventArmies()` 走「出声 + 出厂态」那一支。
        /// 宿主/将来接上事件实例时把它灌进来即可（`LocalLiveOpsEvent` 就是现成的实现）。</para></summary>
        public ILiveOps LiveOp;

        /// <summary>原版 `LiveOpsManager.GetHandler&lt;RewardEventHandler&gt;()` 的本地口。
        /// <para>灌了就用它；没灌则退回 `LiveOpsManager.GetHandler&lt;RewardEventHandler&gt;()`（没注册就是 null）。</para></summary>
        public RewardEventHandler RewardHandler;

        /// <summary>上一次 `InitializeEventArmies()` 命中的奖励事件条数（原版那个 `Where(...).Any()` 的计数；自检读口）。</summary>
        public int LastArmyMatchCount;

        /// <summary>本窗建出来的阵营面板（自检/宿主读口 —— 原版 `__Open.c` 里那个 `this+0xa0`）。</summary>
        public ShowEventArmiesPanel ArmiesPanel { get { return _armiesPanel; } }

        /// <summary>原版 `SinglePlayerOnlyEnergyEventWindow.Open()` 里那一段（`__Open.c:103-118`，逐句）：
        /// <code>
        /// var handler = LiveOpsManager.GetHandler&lt;RewardEventHandler&gt;();
        /// var matches = handler.GetEvents(false).Where(e =&gt; e.IsTargetTo(this));   // ← 谓词 b__11_0
        /// if (matches.Any())                                                        // ★ 没有命中 ⇒ **一次都不调**
        ///     armiesPanel.Initialize(matches.First().Data.armyAllowList.ToArray());
        /// </code>
        /// <para>🔴 **数据源是 `RewardEventData.armyAllowList`（`+0x80`）** —— `.c` 里读的是
        /// `[[First+0x18]+0x80]`（`+0x18` = `LiveOpsEvent.Data`）⇒ 不是「各条活动上某个叫 `Army` 的字段」，
        /// 也**不是把所有命中的 `armyAllowList` 拼起来**（只有 `First()` 那一条）。⛔ 别按二手文档改。</para>
        /// <para>⚠️ **本地口径（如实记）**：本工程的 `RewardEvent` **不派生 `LiveOpsEvent`**
        /// （见 `Shell/RewardEvent.cs` 文件头）⇒ handler 的事件表是 `RewardEventHandler.Events`
        /// （`RegisterEvent` 登记的那一批），不是基类的 `GetEvents()`。判据链仍是一条：同一条 `IsTargetTo`。</para>
        /// <returns>排出来的格数（0 = 没有命中 / 没有数据源 ⇒ 保持出厂态）。</returns></summary>
        public int InitializeEventArmies()
        {
            if (_armiesPanel == null)
            {
                Debug.LogWarning("[Energy] `InitializeEventArmies`：`Army Selector Panel` 没建出来 ⇒ 阵营行缺席（出声）");
                return 0;
            }
            var handler = RewardHandler;
            if (handler == null) handler = LiveOpsManager.GetHandler<RewardEventHandler>();
            if (handler == null)
            {
                if (!_warnedNoRewardHandler)
                {
                    _warnedNoRewardHandler = true;
                    Debug.Log("[Energy] `InitializeEventArmies`：没有 `RewardEventHandler`（原版这里是 "
                            + "`LiveOpsManager.GetHandler<RewardEventHandler>()`；本机没有注册任何 handler）"
                            + " ⇒ 阵营行保持出厂态（只有模板 `ArmyItemReference`），⛔ 不编一张阵营表");
                }
                LastArmyMatchCount = 0;
                return 0;
            }
            if (LiveOp == null)
            {
                if (!_warnedNoLiveOp)
                {
                    _warnedNoLiveOp = true;
                    Debug.Log("[Energy] `InitializeEventArmies`：`LiveOp` 没灌（原版 `this` = 那条 LiveOps 活动实例；"
                            + "本机 13 份活动 SO 全过期、没有在期实例）⇒ 没法跑 `IsTargetTo(this)` ⇒ 阵营行保持出厂态");
                }
                LastArmyMatchCount = 0;
                return 0;
            }

            // `handler.GetEvents(false).Where(e => e.IsTargetTo(this))` —— 只取 **First**（原版 `Any()` 之后 `First()`）
            RewardEvent first = null;
            int matches = 0;
            for (int i = 0; i < handler.Events.Count; i++)
            {
                var e = handler.Events[i];
                if (e == null || !e.IsTargetTo(LiveOp)) continue;
                if (matches == 0) first = e;
                matches++;
            }
            LastArmyMatchCount = matches;
            if (matches == 0)
            {
                // ★ 原版这一档 `Any() == false` ⇒ `Initialize` **整句不执行**（组件保持它当时的样子）。
                //   我们今天每次开窗都会重建树 ⇒ 组件此刻正是出厂态，所以「不调」与「调空表」今天同解；
                //   但⛔ **仍按原版不调**（将来组件被复用时，这一档的行为才会分叉）。
                Debug.Log("[Energy] `InitializeEventArmies`：登记的 " + handler.Events.Count
                        + " 条奖励事件里**没有一条** `IsTargetTo(本事件)` ⇒ 原版这一档不调 `Initialize`，阵营行保持出厂态");
                return 0;
            }

            var allow = first.Data != null ? first.Data.ArmyAllowList : null;
            var arr = new int[allow == null ? 0 : allow.Length];
            for (int i = 0; i < arr.Length; i++) arr[i] = (int)allow[i];
            if (arr.Length == 0)
                Debug.Log("[Energy] `InitializeEventArmies`：命中的奖励事件 `" + first.GetID()
                        + "` 的 `armyAllowList` 是空的 ⇒ 原版这时会调 `Initialize(空表)`（= 清掉旧图标、一颗不建）");
            return ShowEventArmies(arr);
        }

        bool _warnedNoRewardHandler, _warnedNoLiveOp;

        // ================================================================================================
        //  🆕 2026-10-29（`B97_MINI2` · 账 `未分类_其他:430`）—— **`MilestoneScoreBar` 组件接线**。
        //  **追加在类尾**（⛔ 中段加行会让账按 `文件:行号` 的落点整体漂移）。
        //  判据：原版这扇窗的 `Scoring Bar Event Score Info` 挂的就是 `MilestoneScoreBar`
        //  （`progressBar` = `Progress Bar` 上那颗 `Slider`；`scoringLines` = 5 行 `GenericScoreBarLine`）。
        // ================================================================================================

        /// <summary>本窗建出来的 5 行组件（`scoringLines` 那一格）。</summary>
        readonly List<CardPresentation.ScoreBars.GenericScoreBarLine> _scoreLines
            = new List<CardPresentation.ScoreBars.GenericScoreBarLine>();
        CardPresentation.ScoreBars.MilestoneScoreBar _scoreBar;
        UnityEngine.UI.Slider _scoreSlider;

        /// <summary>自检读口：本窗的 `MilestoneScoreBar`（没建 ⇒ null）。</summary>
        public CardPresentation.ScoreBars.MilestoneScoreBar ScoreBarForTest { get { return _scoreBar; } }
        /// <summary>自检读口：`progressBar` 那一格（`Progress Bar` 上的 `Slider`）。</summary>
        public UnityEngine.UI.Slider ScoreBarSliderForTest { get { return _scoreSlider; } }

        /// <summary>把 `MilestoneScoreBar` 挂到已经建好的记分条上，并喂**本地模拟表**（见 `Shell/MilestoneLocalSimulation.cs`）。
        /// <para>🔴 这一步**不建任何新节点** —— 行/箱子/骷髅/高亮/分数全是 `BuildMilestones` 已经按 prefab 逐值建好的那五件；
        /// 这里只把 `<see cref="CardPresentation.ScoreBars.GenericScoreBarLine"/>` 绑上去，再由
        /// `MilestoneScoreBar.Initialize` 按原版写一遍（`score.text` / 箱子图 / 高亮开关 / 进度条两格）。</para>
        /// <para>⚠️ 模拟表的取值口径 = **镜像 prefab 出厂态** ⇒ 这一步**不改变任何可见像素**
        /// （`score` = `1256` 与出厂 TMP 原文同字、`state = Unlocked` 与出厂「高亮亮着」同态、`currentScore = 0` ⇒ 进度 0）。</para></summary>
        void WireScoreBar(Transform bar, UnityEngine.UI.Slider slider)
        {
            _scoreSlider = slider;
            _scoreBar = bar.gameObject.AddComponent<CardPresentation.ScoreBars.MilestoneScoreBar>();
            _scoreBar.Bind(slider, _scoreLines.ToArray());
            CardPresentation.ScoreBars.MilestoneLocalSimulation.LogOnce("Energy");
            _scoreBar.Initialize(CardPresentation.ScoreBars.MilestoneLocalSimulation.CurrentScore,
                                 CardPresentation.ScoreBars.MilestoneLocalSimulation.CreateTable(), null);
            PinChestAspect();
            // 出厂进度 = 0：让 `Slider.normalizedValue` 与画出来的 `Fill` 保持一致（`DrawFill(0)` ⇒ 不建 quad）。
            SetProgress(_scoreSlider.normalizedValue);
        }

        /// <summary>把 5 颗 `Chest` 的 `_aspect` 钉回**各自框**的比例。
        /// <para>🔴 为什么必须有这一步（2026-10-29 `B97_MINI2`）：`GenericScoreBarLine.Initialize` 写箱子走的是
        /// `ImageQuad.SetTexture(tex)` 那个**单参**重载（= 原版 `chestReward.image.sprite = x` 那一跳），
        /// 它会把 `_aspect` 拉成**贴图自己**的比例（`keepAspect:false` 档）；而本窗的箱子框是按 prefab 矩形画的
        /// —— `ChestW 104.49 × ChestH 93.20` ⇒ **1.1211**，贴图 `40k_Crate_Tier*` 是 **512×512** ⇒ 1.0
        /// ⇒ 不钉回来的话箱子会**窄 10.8%**（`WorldW = WorldH × _aspect`，而且**静默**）。</para>
        /// <para>改前那一版 `SetScore` 用的是 `SetTexture(tex, true)`（不动比例）⇒ 这条钉子是「接组件后**视觉等价**」
        /// 的必要件，⛔ 别删（`Editor/B97_MINI2_Assert.cs` 的 C 段钉着它存在）。</para></summary>
        void PinChestAspect()
        {
            const float aspect = ChestW / ChestH;
            for (int i = 0; i < _chests.Count; i++) if (_chests[i] != null) _chests[i].SetAspect(aspect);
        }

        // ============================================================ 类尾追加（B99_A4 2026-10-30）
        /// <summary>`Timer` 那一行的**截止时刻**（= 原版 `SinglePlayerOnlyEnergyEventWindow.TryOpen` 里
        /// `timerDisplay.Initialize(事件.GetEndDate())` 那个实参）。
        /// <para>🔴 **本地恒 `null`（判据）**：截止时刻 = 服务端 LiveOps 的 `SinglePlayerScoringEvent.endDate`
        /// （`decomp_full/SinglePlayerOnlyEnergyEventWindow__TryOpen.c`：`activationType != 0` 才 `Initialize`）
        /// —— 本机 13 份活动 SO 全过期、没有在期事件实例。</para>
        /// <para>🔴 **为什么 `null` ⇒ 整棵不建**：原版 `TimerDisplay__UpdateTimer.c` 第一条
        /// `if (*(char *)(this+0x78) == '\0') return;` = **静默返回**（不写字、不动 GameObject）
        /// ⇒ 建了也只会把 prefab 占位（`Termina en: 23d 5h`）留在屏幕上 = **假倒计时**
        /// ⇒ 按 §18·0「照抄原版」+ §2·4「留开关，不硬删」，这里做成**一个开关**：有值才建那一行。</para>
        /// <para>⚠️ 追加在**类尾**（⛔ 别插中段 —— 会让账按 `文件:行号` 的落点整体漂移，铁律 18·b）。</para></summary>
        public static System.DateTime? EnergyTimerDeadline;
    }
}
