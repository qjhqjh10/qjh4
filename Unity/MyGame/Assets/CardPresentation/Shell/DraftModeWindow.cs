// DraftModeWindow.cs — 轮抽（Draft）**免费/常规**那一扇窗：原版 `Draft Mode Menu Demo`
//   （`DraftModeMenuDemo : GameWindowWithTabs`；**不是**限时版 `Draft Mode Timed Mode Window`）
//
// ==================================================================
//  出处（唯一判据）
// ------------------------------------------------------------------
// · **原版规格正本** = `资料/普查产出_1024b/S3_轮抽窗原版规格.md`（94 KB，逐节点：路径 / 矩形 / anchor / pivot /
//   字号 / 图片 PathID / 初始 active / 显隐判据；§三 交互链 17 步、§四 空态、§五 缺口、§七 查不到）。
//   本文件每一段几何的注释都写「照 S3 §x.y」或「我们挑的」。
// · 复核用的现读（本轮亲自跑过，数值直接抄进常量）：
//   `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "<节点名>" --depth N --relative`
//   —— `Draft Mode Menu Demo` / `Draft Mode Pay State` / `Draft Mode Select Packs State` /
//   `Draft Mode Select Warlord State` / `Draft Mode Ongoing State` / `Draft Mode Deck Info Panel` /
//   `Prebuilt Pack UI` / `Generic Select Card Container` **八棵树逐棵现读**。
// · 窗参（根 MB `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-7910505541983962427.json` 逐字段实读）：
//   `type = 1`(Popup) · `windowsPlacement = 15`(Popup) · `closeOnESC = 1` · `updateNavPanel = 0` ·
//   `extraScaleSmallScreen = 1.0` · `useDefaultCloseSoundIfNull = 1`.
// · 状态机：`decomp_full/DraftModeMenuDemo__GetTabFromState.c` / `__GetStartingTab.c` / `__GoToCurrentState.c`；
//   枚举值 `dump.cs:126782`（`DraftModeState`：Intro0 / PayScreen2 / SelectWarlord5 / SelectPack10 /
//   OngoingRun15 / Finished20 / Collected22）。
// · 抽包 / 组卡：**只走** `Shell/DraftPacks.cs`（`RollRound` / `BuildClosedDeck`）—— ⛔ 本文件不另写一套抽样。
// · 进对局：`BattleDriver.SetPendingPlayMode(GameMode.ClosedDeck)` + `PrebuiltDecks.SetPendingBattleDeck`
//   （= 既有的「本局用这副牌」通道，判据 → `PrebuiltDecks.SetPendingBattleDeck` 那段注释）。
//
// ==================================================================
//  🔴 本批的边界（用户口径 / 派单）
// ------------------------------------------------------------------
// · **只做免费档**：`Premium Button` / `Premium Ten Button` **不建**（原版 `premium10Price < 0` 时整节点
//   `SetActive(false)` ⇒ 序列化上就允许「没有这两档」；我们把它当成「本地档位永远不存在」）。
//   ⚠️ 价格 `premiumPrice` / `premium10Price` **是服务端值、本地 0 命中**（S3 §五）⇒ 不编价格。
// · **重掷先不做**：`GetReRollCosts` 无真值（S3 §五 最后一行）。原版 `Reroll/Price Display Button`
//   那一族**整支不建**，并在 `Open()` 里**出声一次**（⛔ 不静默）。
// · **不做限时版**：`Draft Mode Timed Mode Window` 是另一份 prefab（Pay/Warlord 两页换 Timed 子类 + 多
//   阵营进度条 / 联盟积分，S3 §2.10）⇒ 本窗一个节点都不带它。
// · **Intro 页不建节点**：原版 `Draft Mode Intro State` **不在 `tabs` 里**、全反编译找不到它的激活者
//   （S3 §七·1「查不到」）；而 `GetTabFromState(0)` 落到 **Pay 页**（else 分支）⇒ 我们的
//   `DraftModeState.Intro` **路由到 Pay 页**，⛔ 不凭空造一个第 5 页。
// · **付费活动倒计时 / 计时器不建**：`Timer` 读的是活动结束时刻（服务端下发的 liveop 数据，本地 0 命中）
//   ⇒ 建一个假倒计时 = 骗玩家。`Open()` 出声说明。
//
// ==================================================================
//  ⚠️ 缺口表（S3 §五）—— 哪些数是原版、哪些是我们挑的
// ------------------------------------------------------------------
//  | 参数 | 谁给的 | 我们的取法 |
//  |---|---|---|
//  | `CardsPerPack = 3` | **原版真值**（882/882 包实测） | `DraftPacks.CardsPerPack` |
//  | `draftRounds = 4` | 服务端（`DraftEventData+0x98`），本地 0 命中 | `DraftPacks.RoundsPerDraft`（**我们挑的**） |
//  | `packsPerDraft = 3` | 服务端（`+0x9C`） | `DraftPacks.PacksPerRoundOffered`（**我们挑的**） |
//  | `warlordsPerDraft` | 服务端（`+0xA0`） | `WarlordsPerDraft = 5`（**我们挑的**；S3 §五 也建议 5） |
//  | `seed` | **服务端事件实例字段**，本地不存在 | 本窗自己抽（`BeginRun` 收一个 seed；**打进日志**，可复现） |
//  | `Rewards` / `reRollCosts` / `premiumPrice` | 服务端 | 本轮不做（见上「本批的边界」） |
//
//  🔴 **`warlordsPerDraft` 取 5 的副作用**：5 张选将卡 × 视觉宽 503.97 + 间距 80 = **2475 px**，
//     比 1920 的屏幕宽 ⇒ 原版 prefab 的 `Generic Card Selector Menu` 是 `ScrollRect(h=1,v=0)`，
//     但它的 **`ScrollRect` 组件序列化值是 `m_Enabled = 0`**（现读，见 `menu_dump` 的
//     `⛔MULTI-COMP(2组件·1禁用)`）⇒ **原版那一刻拖不动**。我们**加了横向滚动**（`MenuScroll`，
//     120 px/格）—— 🔴 **这一条是我们挑的**，理由 = 否则后 2 张战将永远点不到（那不是「复刻」，是「不能用」）。
//
// ==================================================================
//  🔴 建窗纪律（本工程踩过的，逐条照办）
// ------------------------------------------------------------------
// · `Open()` 第一句就 `PointerLayer.UnregisterOwnedBy(gameObject)` —— 本方法会清子件并重建 `MenuScroll`，
//   旧滚动条目不清会**每重建一次净涨 1 条**（判据 → `Shell/LiveOpsEventWindow.cs` 的 `Build()` 首段）。
// · 压暗层点击 = **关窗**（`MenuDraw.ShadeHit`，档落在压暗层自己那一档、严格低于本窗内容命中档）。
// · 红底那一块**吸收**点击（`MenuDraw.Absorb`）：原版点红底什么都不做，只有压暗层外沿是「点外面关窗」。
// · 文字/按钮都要**挂在原版那个父节点底下**（`FindChild(钮,"Button Text")` 靠它）；节点名逐字照原版。
using System;
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>原版 `DraftModeState`（`dump.cs:126782`）。**值照原版**，别改。</summary>
    public enum DraftModeState
    {
        Intro = 0,
        PayScreen = 2,
        SelectWarlord = 5,
        SelectPack = 10,
        OngoingRun = 15,
        Finished = 20,
        Collected = 22,
    }

    /// <summary>轮抽的**免费/常规**窗（原版 `Draft Mode Menu Demo`）。
    ///
    /// <para>页面切换**按状态类型**（原版 `GameWindowWithTabs.GetTab&lt;T&gt;()` + `ChangeTabCO`），
    /// ⛔ 不是页签按钮 —— 原版 prefab 里没有任何页签节点（S3 §〇·3）。我们不挂 `MonoBehaviour` 页类
    /// （嵌套类建不出脚本资产，判据 → `CLAUDE.md` §二 的 `!u!115` 那一段），用
    /// <see cref="PageFor"/> 那一张 **state → 页节点** 的表代替 `GetTab&lt;T&gt;()`。</para>
    ///
    /// <para>⚠️ **本类不碰战斗**：`To Battle` 只做「组牌 + 把牌交给既有开战通道 + 切场景」，
    /// 战斗内一行不动。</para></summary>
    public class DraftModeWindow : GameWindow
    {
        public const string RootName = "Draft Mode Menu Demo";

        // ============================================================ 渲染队列（自成一档 4130–4199）
        // 相邻带（现读全仓队列常量，最高一条 = ToastNotifications 4072–4076 · `QGlow` 4110 / 4112–4116）
        // ⇒ 本窗从 **4130** 起，留 13 档空隙。
        public const int QShade = 4130;   // 压暗整屏（`Menu Dark Background` 的 quad + 它的命中区）
        public const int QBg1 = 4131;     // 红底（`Reward Background Get Reward`）
        public const int QBg2 = 4132;     // `Noise`
        public const int QBg3 = 4133;     // `Menu Vignette`
        public const int QPanel = 4135;   // 右栏面板 / 奖励面板的底 / 右栏那三颗 toggle
        /// <summary>**卡面【后面】**那一层（督军立绘 · `Packs Mask` 的底板 · 单包卡的底与描边）。
        /// 🔴 它必须**严格低于** `QCardBase` —— 否则卡面会被自己的底板盖死（本工程的老坑：
        /// 同档/高挡的透明层谁盖谁不可控，判据 → `Shell/LiveOpsEventWindow.cs` 的队列梯子注释）。</summary>
        public const int QPanelBack = 4136;   // 底板（`Packs Mask` 的底板 / 督军立绘）
        public const int QPanelBack2 = 4137;  // 压在底板上的那一层（**单包卡的底** —— 它压在 Mask 底板上）
        public const int QPanelArt = 4138;    // 再上一层（包卡的橙色描边 / 分隔线）
        /// <summary>卡面那一段的**基号**：**一张卡占 1 档**（`CardView.SetQueue` 把整张卡盖成同一号；
        /// 卡内层序由 `CardView` 自己按层名定）。同一屏最多 5 张选将卡 / 9 张包内小卡 ⇒ 4140…4148。</summary>
        public const int QCardBase = 4140;
        public const int QArt = 4160;     // 按钮底（压在卡面上）
        public const int QArt1 = 4161;    // 压在 `QArt` 上的那一层（图标 / 金框）
        public const int QText = 4165;
        public const int QHit = 4170;     // 内容命中区（**必须 > 卡面**，否则卡盖住按钮点不到）
        /// <summary>`MenuDraw.ShadeHit` 要求压暗档 **严格低于**内容命中档 —— 本窗 = `QShade` &lt; `QHit`。</summary>
        public const int QHitContentMin = QHit;

        // ============================================================ 规模（见文件头「缺口表」）
        /// <summary>🔴 **我们挑的，不是原版真值**（`warlordsPerDraft` 在服务端 `DraftEventData+0xA0`；
        /// 本地 0 命中）。S3 §五 也建议 5。改这一个数会同时改选将页的卡片数（其余地方全跟着它）。</summary>
        public const int WarlordsPerDraft = 5;

        // ============================================================ 背景（S3 §2.1 现读）
        static readonly PxRect ShadeR = new PxRect(-1327.30f, -773.63f, 3247.30f, 1798.73f);
        static readonly PxRect RedR = new PxRect(0f, 94.60f, 1920f, 985.40f);
        static readonly PxRect VigR = new PxRect(0f, -27.20f, 1920f, 1052.30f);
        static readonly Vector4 RedBorder = new Vector4(0f, 26f, 0f, 26f);   // 21×81 · border(0,26,0,26)
        const float RedTexW = 21f, RedTexH = 81f;
        /// <summary>`Noise` 的 Tiled 格 = 贴图 1024 ÷ `m_PixelsPerUnitMultiplier 1.77`。</summary>
        const float NoiseTilePx = 1024f / 1.77f;

        // ============================================================ 顶部 title 栏（S3 §2.1）
        // 原版 `Game Mode Header With Back Button` = 550×109.55 @ (0,526) · anchor (0,0.5) · pivot (0,1)
        // ⇒ 设计中（左上原点）= y 14 … 123.55。下面几格照兄弟窗（`LiveOpsEventWindow.BuildHeader`）
        // 的同一套结构、按本窗那 26.86 的位移整体上移（-26.86 = 40.86 − 14）。
        const float HdrL = 0f, HdrT = 14f, HdrR = 550f, HdrB = 123.55f;
        const float PlateR = 690.86f;                     // 底板被 `ContentSizeFitter` 撑开到 690.86
        const float WingT = 14.01f, WingB = 129.37f;      // `Header Background (1)`
        const float WingL = -462.10f, WingR = 87.90f;
        const float BackL = -24.40f, BackT = 16.02f, BackR = 143.48f, BackB = 127.35f;
        static readonly string[] ToggleArtOnOff = { "40K_settings_button_hover", "40K_settings_button" };

        // ============================================================ Pay 页（S3 §2.3 · Demo 版）
        // 换算：`Scale menu` = 1920×1082 @ (272,33)（anchor/pivot 都是 (0.5,0.5)）
        // ⇒ 页内 center = (1232, 507)；下面每格 = center + (anchoredPosition.x, −anchoredPosition.y)。
        static readonly PxRect PayTitleR = new PxRect(957.36f, 275.80f, 1524.64f, 321.48f);
        static readonly PxRect PayHelpR = new PxRect(966f, 380.10f, 1516f, 510.50f);
        static readonly PxRect PayDescR = new PxRect(966f, 526.73f, 1516f, 661.88f);
        static readonly PxRect PayBarR = new PxRect(957.36f, 653.77f, 1524.64f, 822.24f);
        static readonly PxRect FreeBtnR = new PxRect(966.91f, 707.25f, 1136.91f, 768.77f);
        static readonly PxRect FreeTxR = new PxRect(978.5f, 714.5f, 1125.3f, 762.5f);

        // ============================================================ SelectWarlord 页（S3 §2.4 现读）
        static readonly PxRect WlTitleR = new PxRect(53.60f, 148.60f, 1866.40f, 225.40f);
        static readonly PxRect WlViewR = new PxRect(0f, 101.90f, 1920f, 1006.90f);
        static readonly PxRect WlContBtnR = new PxRect(1526.50f, 932.70f, 1866.40f, 1026.50f);
        static readonly PxRect WlContTxR = new PxRect(1539.7f, 938.9f, 1853.2f, 1020.3f);
        /// <summary>选将格：布局框 412.75×617.69 @ `localScale 1.221` ⇒ 视觉 503.97×754.20（S3 §2.4 / 现读）。</summary>
        const float WlSlotW = 412.75f, WlSlotH = 617.69f, WlSlotScale = 1.221f, WlSlotGap = 80f;
        const float WlSlotVisW = WlSlotW * WlSlotScale;      // 503.97
        const float WlSlotVisH = WlSlotH * WlSlotScale;      // 754.20
        /// <summary>选将格在视口里的中心 y：`Content` 顶部 101.9 + 锚点偏移 452.52（S3 §2.4 现读）。</summary>
        const float WlSlotCy = 554.42f;
        /// <summary>`Select Button` 在选将格里的**视觉**矩形（现读 `Generic Select Card Container`：
        /// `101.4,585.5 → 311.4,667.1`；已含 1.221 的换算）—— 相对格子视觉框左上角。</summary>
        static readonly PxRect WlSelBtnOff = new PxRect(101.40f, 585.50f, 311.40f, 667.10f);
        static readonly PxRect WlSelTxOff = new PxRect(112.50f, 601.90f, 299.60f, 650.70f);
        /// <summary>卡本体的视觉高（= `CardView.Height × 108 × 比例`）：原版 `2DCard` 386.6×615.42
        /// × 1.221 = **472.0×751.4**（S3 §2.4 现读）⇒ 我们按高铺满（宽跟着卡的比例走）。</summary>
        const float WlCardBodyH = 751.40f;
        /// <summary>滚动一格多少画布像素。🔴 原版那台 `ScrollRect` 的 `m_ScrollSensitivity` 查不到
        /// （组件被禁，现读只有 `h=1/v=0`），**120 是我们挑的**（与主菜单模式卡同一条口径）。</summary>
        const float WlScrollNotch = 120f;

        // ============================================================ SelectPack 页（S3 §2.5 现读）
        static readonly PxRect WarlordImgR = new PxRect(-258.20f, -17.70f, 814.20f, 1054.80f);
        static readonly PxRect SubTitleR = new PxRect(498.10f, 263.20f, 1472.70f, 326.60f);
        static readonly PxRect PackLabelR = new PxRect(698.60f, 317.60f, 916.50f, 365.50f);
        static readonly PxRect StageR = new PxRect(927.60f, 322.60f, 1145.50f, 360.50f);
        static readonly PxRect PacksMaskR = new PxRect(395f, 178.30f, 1472.70f, 837.10f);
        static readonly PxRect PacksContR = new PxRect(510.50f, 272.60f, 1456.20f, 938.20f);
        static readonly Vector4 MaskBorder = new Vector4(42f, 363f, 655f, 81f);   // 1100×701 · ppuMult 2
        const float MaskTexW = 1100f, MaskTexH = 701f;
        const float PacksContSpacing = 13f;

        // ---- 单包卡（`Prebuilt Pack UI`，独立根 prefab；S3 §2.6）----
        const float PackW = 305.89f, PackH = 390.94f;
        static readonly PxRect PackTitleR = new PxRect(4.40f, 25.50f, 301.50f, 58.70f);
        static readonly PxRect PackSepR = new PxRect(0f, 70.40f, 305.89f, 73.30f);
        /// <summary>包内 3 张缩略卡的区域（🔴 **我们挑的布局**：原版按 `pack.GetCardLibrary()` 逐张实例化
        /// `cardInfoItem` = `UICardInfoItem` 信息行，那颗模板 prefab 的尺寸本地解不出
        /// （`cardInfoItem` 的 PathID `-5880222813801091413` 在 `GameObject/` 里 0 命中）⇒
        /// 我们改成 **3 张缩略卡面 + 卡名**（复用 `CardView`，判据 → 派单「卡区按 3 张实例化（复用现有卡面渲染件）」）。</summary>
        static readonly PxRect PackCardsArea = new PxRect(4.95f, 84f, 300.94f, 240f);
        static readonly PxRect PackNameArea = new PxRect(4.95f, 242f, 300.94f, 262f);
        static readonly PxRect PackSelR = new PxRect(41.90f, 300f, 264.00f, 349.49f);   // 222.10×49.49
        static readonly PxRect PackSelTxR = new PxRect(52.80f, 301.5f, 253.10f, 347.99f);

        // ============================================================ Ongoing 页（S3 §2.7 现读 · 已含 1.06）
        static readonly PxRect MainContR = new PxRect(118.70f, 156.60f, 1505.90f, 880.50f);
        static readonly PxRect OnWlImgR = new PxRect(-290.40f, -49.90f, 846.40f, 1087.00f);
        static readonly PxRect OnTitleR = new PxRect(673.80f, 191.50f, 1495.60f, 255.10f);
        static readonly PxRect OnVictR = new PxRect(909.20f, 327.80f, 1141.00f, 373.80f);
        static readonly PxRect OnVictNumR = new PxRect(1148.40f, 303.40f, 1297.60f, 398.10f);
        static readonly PxRect OnStagesR = new PxRect(746.60f, 408.80f, 1425.10f, 683.50f);
        static readonly PxRect OnDefeatR = new PxRect(697.90f, 742.50f, 825.40f, 798.00f);
        static readonly PxRect OnLossesR = new PxRect(825.40f, 728.10f, 1072.50f, 812.40f);
        static readonly PxRect OnResetR = new PxRect(1230.70f, 748.10f, 1417.30f, 804.20f);
        static readonly PxRect OnResetTxR = new PxRect(1240.5f, 754.5f, 1406.9f, 797.8f);
        static readonly PxRect OnBattleR = new PxRect(1455.70f, 927.70f, 1896.10f, 1048.30f);
        static readonly PxRect OnBattleTxR = new PxRect(1471.20f, 934.70f, 1880.00f, 1041.20f);
        /// <summary>`Stages Container` 的 GridLayoutGroup：cell 80×80 · spacing (23.7, 30.04) · align MiddleCenter · 2 列。</summary>
        const float StageCell = 80f, StageGapX = 23.70f, StageGapY = 30.04f;
        /// <summary>`Losses container` 的 HLG：3 格 · spacing 9.86 · 格子 67.74²（S3 §2.13 `Losses Info UI_ref`）。</summary>
        const float LossCell = 67.74f, LossGap = 9.86f;
        const int LossSlots = 3;      // 「失败 3 场结束」（S3 §2.7：Defeat Marks 模板恒 3 格）

        // ---- 奖励面板（`DraftRewardPanel`，只有 Finished 显示；S3 §2.8 现读）----
        static readonly PxRect RewardPanelR = new PxRect(1505.90f, 168.30f, 1896.10f, 880.50f);
        static readonly PxRect CrateGlowR = new PxRect(1444.50f, 245.90f, 1957.50f, 702.90f);
        static readonly PxRect CrateR = new PxRect(1505.90f, 311.90f, 1896.10f, 636.90f);
        static readonly PxRect CollectR = new PxRect(1537.60f, 627.70f, 1864.40f, 706.10f);
        static readonly PxRect CollectTxR = new PxRect(1552.2f, 628.3f, 1848.8f, 705.5f);

        // ============================================================ 右栏 `Draft Mode Deck Info Panel`（S3 §2.9 现读）
        static readonly PxRect DeckPanelR = new PxRect(1504.94f, 204.41f, 1895.07f, 916.55f);
        static readonly PxRect DeckTabsBarR = new PxRect(1542.94f, 147.71f, 1857.04f, 210.86f);
        static readonly PxRect[] DeckToggleR =
        {
            new PxRect(1542.94f, 140.01f, 1625.03f, 218.46f),
            new PxRect(1638.04f, 140.01f, 1720.13f, 218.46f),
            new PxRect(1733.14f, 140.01f, 1815.23f, 218.46f),
        };
        /// <summary>三颗 toggle 的图标（原版 `Icon` 64×64，左/上各内缩 9.1 / 7.3；第三颗 `localScale 1.1`）。
        /// 🔴 **前两张图本地没有**（`40K_Info_button_draft menu` / `40K_Cards_button_draft menu` 在
        /// `Resources/Art/ui*/` 0 命中）⇒ 取不到就**不画那一格图标并出声**，⛔ 不拿别的图顶。</summary>
        static readonly string[] DeckToggleIconArt =
        {
            "40K_Info_button_draft menu", "40K_Cards_button_draft menu", "40K_Chat_icon_Alliance v2",
        };
        static readonly PxRect DeckTitleR = new PxRect(1542.40f, 256.17f, 1857.60f, 306.17f);
        static readonly PxRect DeckCounterR = new PxRect(1565.40f, 821.09f, 1834.60f, 871.09f);
        static readonly PxRect DeckEnergyR = new PxRect(1537.95f, 540.42f, 1862.05f, 590.42f);
        /// <summary>`Card List` 页的行区（🔴 **我们挑的**：原版 `DrafModeDeckInfoCardListPanel` 是
        /// `DeckListDrawer` + `Scroll View`，格子模板在另一份 prefab 里 ⇒ 我们按行排）.</summary>
        static readonly PxRect DeckListR = new PxRect(1542f, 330f, 1858f, 800f);
        const float DeckRowH = 38f;

        // ============================================================ 状态
        /// <summary>当前页（原版 `DraftModeState`）。</summary>
        public DraftModeState State { get; private set; }
        /// <summary>本窗最近打开的那一扇（自检用，照 `LiveOpsEventWindow.LastOpened` 的形状）。</summary>
        public static DraftModeWindow LastOpened { get; private set; }

        int _seed;
        /// <summary>本局种子（**日志里会打**，照它重开就是同一串包）。</summary>
        public int Seed { get { return _seed; } }
        /// <summary>自检/入口想钉种子时用；`0` = 每局现抽。</summary>
        public int SeedOverride;

        readonly List<string> _pickedKeys = new List<string>();
        readonly List<string> _selectedCards = new List<string>();
        List<DraftPacks.PackDto> _offered = new List<DraftPacks.PackDto>();
        List<CardDef> _warlords = new List<CardDef>();
        string _warlordId;
        DraftPacks.ClosedDeck _deck;
        /// <summary>右栏三颗 toggle 的选中项：0 = Deck Info · 1 = Card List · 2 = Alliance Info（互斥）。
        /// 🔴 **开窗默认 0（Deck Info）** —— ⚠️ 原版 prefab 序列化的是 `Deck Info isOn=0 / Card List isOn=1 /
        /// Alliance isOn=0`，而同族的 `Initialize` 又把三个内容页全 `SetActive(false)`、**只留 Energy 页**（S3 §2.9）
        /// ⇒ 原版那一刻「亮的 toggle」与「显示的内容页」**不一致**（序列化残留）。
        /// 我们取「亮的那个 = 显示的那个」（0），**这一条是我们挑的**（照原版那对矛盾没有意义）。</summary>
        int _deckTab;

        /// <summary>已经选过的包（`key`，按选包顺序 ⇒ 顺序就是轮次）。</summary>
        public IList<string> PickedPackKeys { get { return _pickedKeys; } }
        /// <summary>已经拿到手的卡（每选 1 包 +3；原版 `DraftEvent.selectedCards`）。</summary>
        public IList<string> SelectedCards { get { return _selectedCards; } }
        /// <summary>`CurrentDraftRound` = 已选包数（**0 起**，原版同义：`selectedPacks.Count`）。</summary>
        public int CurrentDraftRound { get { return _pickedKeys.Count; } }
        /// <summary>这一轮「一屏给出的包」—— **就是 `DraftPacks.RollRound` 的返回**（⛔ 不是写死 3）。</summary>
        public IList<DraftPacks.PackDto> OfferedPacks { get { return _offered; } }
        /// <summary>当前选中的战将（卡 id；空 = 还没选）。</summary>
        public string WarlordId { get { return _warlordId; } }
        /// <summary>这一轮可选战将（`WarlordsPerDraft` 张，按 seed 确定性抽）。</summary>
        public IList<CardDef> Warlords { get { return _warlords; } }
        /// <summary>`To Battle` 真的把这一局交出去了没有（批处理下不切场景，只记账）。</summary>
        public bool StartedBattle { get; private set; }
        /// <summary>组出来的 ClosedDeck（`LaunchBattle` 那一刻才有）。</summary>
        public DraftPacks.ClosedDeck ClosedDeckBuilt { get { return _deck; } }

        /// <summary>取不到的图（自检读口 —— 非空就是「有件根本没画」；照 `LiveOpsEventWindow.MissingArt`）。</summary>
        readonly List<string> _missingArt = new List<string>();
        public List<string> MissingArt { get { return _missingArt; } }

        // 页面与常驻件
        Transform _pagePay, _pageWarlord, _pagePacks, _pageOngoing;
        Transform _packsContainer, _warlordViewport, _warlordContent, _deckTabHolder;
        Transform _deckPanelRoot, _rewardPanelRoot, _packHint, _warlordHint;
        Label _stageCounter, _cardsCounter, _victoriesNum, _deckTitle;
        MenuScroll _warlordScroll;
        readonly List<Transform> _warlordNodes = new List<Transform>();
        readonly List<CardView> _packCardViews = new List<CardView>();

        // ============================================================ 取图（取不到**必须出声**）
        Texture2D Tex(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !_missingArt.Contains(name)) _missingArt.Add(name);
            return t;
        }

        // ============================================================ 建窗
        /// <param name="seed">本局种子。`0` = 每局现抽（生产路径）；自检传定值以便复现（同 `SeedOverride`）。</param>
        public static DraftModeWindow Create(WindowsManager mgr, int seed = 0)
        {
            var go = new GameObject(RootName);
            var win = go.AddComponent<DraftModeWindow>();
            win.SeedOverride = seed;
            // ---- 窗参（逐字段实读根 MB `MonoBehaviour_-7910505541983962427.json`）----
            win.type = WindowType.Popup;                       // `type = 1`
            win.placement = WindowsPlacement.Popup;            // `windowsPlacement = 15`
            win.closeOnEsc = true;                             // `closeOnESC = 1`
            win.updateNavPanel = false;                        // `updateNavPanel = 0`
            win.extraScaleSmallScreen = 1f;                    // `extraScaleSmallScreen = 1.0`（= 不覆盖）
            // `openSound` / `closeSound` 实读都是空 PPtr、`useDefaultCloseSoundIfNull = 1`
            // ⇒ 一个 clip 都不赋（关窗音走基类默认 `ButtonClick Back`）。
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[Draft] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            MainMenuRuntime.SetNavPanelVisible(false, true);   // 整屏红底窗 ⇒ 收起左竖导航（同 `RankedEventWindow.Open`）
            // 🔴 每关一次再开 ⇒ **重开一轮**（`BeginRun`）。原版的进度在服务端（`DraftEvent.selectedPacks/…`），
            //    本地没有任何落盘通道 ⇒ 如实重置，⛔ 不假装「接着上次」。
            //    ⚠️ 窗**还开着**时再点模式卡 ⇒ `TryOpen` 走 `Background`/`Open` 支、**不调本方法**
            //    （`GameWindow.TryOpen` 的三档）⇒ 那种情况下进度照旧保留。
            int seed = SeedOverride != 0 ? SeedOverride
                                         : unchecked((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
            BeginRun(seed);
        }

        public override void Close()
        {
            MainMenuRuntime.SetNavPanelVisible(true, true);
            base.Close();
        }

        /// <summary>开一局新的轮抽（**重置全部状态**并落到 Pay 页）。`Open()` 与「放弃本轮」都走它。</summary>
        public void BeginRun(int seed)
        {
            _seed = seed;
            _pickedKeys.Clear();
            _selectedCards.Clear();
            _offered = new List<DraftPacks.PackDto>();
            _warlordId = null;
            _deck = null;
            _deckTab = 0;
            StartedBattle = false;
            _warlords = RollWarlords(_seed, WarlordsPerDraft);
            Build();
            GoToState(DraftModeState.PayScreen);
            Debug.Log("[Draft] 开一局（种子 " + _seed + "，战将候选 " + _warlords.Count + " 张；"
                    + "`draftRounds` = " + DraftPacks.RoundsPerDraft + " 轮 × `packsPerDraft` = "
                    + DraftPacks.PacksPerRoundOffered + " 包 —— 🔴 这三个数是**我们挑的**，原版真值在服务端（S3 §五））");
            Debug.Log("[Draft] 本轮**不建**这两件（原版有、但判据缺失）："
                    + "① `Premium` / `10x Premium` 两档（价格 `premiumPrice`/`premium10Price` 在服务端 ⇒ 不编价格）；"
                    + "② `Reroll` 那一支（`GetReRollCosts` 无真值 ⇒ 不编价格）。"
                    + "还有 `Timer`（活动结束时刻在服务端 ⇒ 不建假倒计时）。");
        }

        void Build()
        {
            // 🔴 首句撤旧滚动区（本方法会清子件，旧条目不清 = 每重建一次净涨 1 条；
            //    判据 → `Shell/LiveOpsEventWindow.Build()` 首段那三条理由）
            PointerLayer.UnregisterOwnedBy(gameObject);
            _missingArt.Clear();
            _warlordNodes.Clear();
            _packCardViews.Clear();
            _warlordScroll = null;
            MenuDraw.ClearChildren(transform);

            var root = transform;
            BuildBackdrop(root);
            BuildHeader(root);
            // `Select Pack Anim Anchor (It must be out of Select Pack State)`：原版 sz=100×100 ·
            // anchor/pivot (0.5,0.5) · anchoredPosition (0,0) ⇒ **屏幕正中**（离屏？不 —— 名字里的
            // "out of Select Pack State" 说的是「它是状态页的**兄弟**、不在页里」，不是「在屏外」）。
            // ⚠️ 原版的选包飞出动画落点；我们**不做飞行动画**（时长常量只在 `DAT_*` 里，S3 §五·3）
            // ⇒ 节点照建（结构与断言认得到），位置照原版。
            MenuDraw.Node(root, "Select Pack Anim Anchor (It must be out of Select Pack State)",
                          new PxRect(910f, 490f, 1010f, 590f));

            _pagePay = BuildPayPage(root);
            _pageWarlord = BuildWarlordPage(root);
            _pagePacks = BuildPacksPage(root);
            _pageOngoing = BuildOngoingPage(root);
            _deckPanelRoot = BuildDeckInfoPanel(root);
            // `Generic Multi Card Display`（预览弹窗）本轮不做：它是 `ScrollRect + UIMultiCardDisplay`
            // 的**独立弹窗**（S3 §2.5「预览」），需要自己的一扇窗（`PackPreview` → `WindowsManager.OpenWindow`）
            // —— 本批范围只到「点包 = 选包」，预览另开一笔（⛔ 不伪造一个内嵌面板充数）。
            Debug.Log("[Draft] `Generic Multi Card Display`（点包卡空白处的 3 卡预览弹窗）**本轮不做** —— "
                    + "原版那是独立一扇弹窗（S3 §2.5），不在本批范围。");
        }

        // ------------------------------------------------------------ 背景（S3 §2.1 现读的四层）
        void BuildBackdrop(Transform root)
        {
            var gen = MenuDraw.Node(root, "General Red Background", new PxRect(0f, 0f, 1920f, 1080f));
            // ① `Menu Dark Background`（纯黑 0.773）+ 命中区（= 原版 `BackgroundCloseButton`，点它关窗）
            MenuDraw.Rect(gen, CardArt.Solid(), ShadeR, "Menu Dark Background", QShade, new Color(0f, 0f, 0f, 0.773f));
            MenuDraw.ShadeHit(gen, new PxRect(0f, 0f, 1920f, 1080f), QShade, QHitContentMin,
                              () => Close(), "BackdropHit");
            // ② `Reward Background Get Reward`（红底九宫）+ 吸收层（原版点红底什么都不做）
            MenuDraw.Nine(gen, Tex("40k_general_popup_simple red"), RedR, RedBorder, RedTexW, RedTexH,
                          QBg1, null, true, "Reward Background Get Reward");
            MenuDraw.Absorb(gen, "AbsorbHit", RedR, QShade, QHitContentMin);
            // ③ `Noise`（Tiled · 原版 tint (0.311,0.127,0,0.718)）
            MenuDraw.Tiled(gen, Tex("UI Dirt And Noise skratches"), RedR, NoiseTilePx, QBg2, "Noise",
                           new Color(0.311f, 0.127f, 0f, 0.718f));
            // ④ `Menu Vignette`（纯黑 0.58）
            MenuDraw.Rect(gen, CardArt.Solid(), VigR, "Menu Vignette", QBg3, new Color(0f, 0f, 0f, 0.58f));
        }

        void BuildHeader(Transform root)
        {
            float titleL = HdrL + 155f;                      // 原版 HLG padLeft 155
            WindowHeader.WithBackButton(root, new WindowHeader.Spec
            {
                RootName = WindowHeader.RootNameGameMode,
                RootRect = new PxRect(HdrL, HdrT, HdrR, HdrB),
                PlateRect = new PxRect(HdrL, WingT, PlateR, WingB),
                TitleRect = new PxRect(titleL, HdrT + 16.36f, titleL + 369.36f, HdrT + 99.01f),
                // 标题文案 = 原版 `WindowHeaderWithBackButton.Initialize` 换上的本模式名
                // （`Demo/DraftMode/Title` = `Draft Mode` / `选秀模式`；官方本地化现读）
                TitleText = Loc.T("Demo/DraftMode/Title"),
                TitleMode = WindowHeader.TitleFit.FitAfterSpacing,
                TitleFitW = 369.36f, TitleFitH = 82.65f,
                TitleVAlign = Label.VAlign.Capline,          // 本 prefab 那颗 `Window Title` = 8192 Capline
                WingRect = new PxRect(WingL, WingT, WingR, WingB),
                BackRect = new PxRect(BackL, BackT, BackR, BackB),
                BackButtonStyle = WindowHeader.BackStyle.QuadOnHeader,
                BackKeepAspect = true,
                BackSwapArt = "UI_Button_Menu_Back",
                PlateTex = Tex("WF_Campaign_Info_Background"),
                BackTex = Tex("UI_Button_Menu_Back"),
                QPlate = QArt, QWing = QArt1, QTitle = QText, QBack = QArt1, QHit = QHit,
                OnBack = () => Close(),
            });
        }

        // ------------------------------------------------------------ Pay 页（只留免费档）
        Transform BuildPayPage(Transform root)
        {
            var page = MenuDraw.Node(root, "Draft Mode Pay State", new PxRect(0f, 0f, 1920f, 1080f));
            // 原版树：`Pay State > Scale menu > {Title, Game mode instructions, Description 1, ButtonsLayout}`
            //    （`Scale menu` 自己挂 `TransformScalerBySmallScreenUI`，序列化 1920×1082 @ (272,33)）
            //    ⇒ 三颗文本必须挂在它底下（节点名逐字照原版；`MenuDraw` 用绝对坐标，多插这一层位置不变）。
            var bar = MenuDraw.Node(page, "Scale menu", new PxRect(272f, -34f, 2192f, 1048f));
            // 标题 / 说明 / 描述（`Localize` 三颗，官方词条现读）
            MenuDraw.Text(bar, PayTitleR, Loc.T("Demo/DraftMode/TitleIntroScreen"), Color.white,
                          "Game Mode Title", 37.30f, QText, PayTitleR.W, 25f, 72f, 47.73f,
                          face: TmpFont.Face.LatinCardBody);
            MenuDraw.Text(bar, PayHelpR, Loc.T("Demo/DraftMode/IntroHelp1"), Color.white,
                          "Game mode instructions", 28.55f, QText, PayHelpR.W, 18f, 32f, 22.50f,
                          face: TmpFont.Face.LatinCardBody);
            MenuDraw.Text(bar, PayDescR, Loc.T("Demo/DraftMode/IntroHelp2"), Color.white,
                          "Description 1", 30f, QText, PayDescR.W, 18f, 32f, 22.50f,
                          face: TmpFont.Face.LatinCardBody);
            // `ButtonsLayout`（原版 HLG：spacing 0 · align MiddleCenter）—— 三颗钮里我们只建 Free
            var btns = MenuDraw.Node(bar, "ButtonsLayout", PayBarR);
            var freeNode = MenuDraw.Node(btns, "Free Button", FreeBtnR);
            var freeBg = MenuDraw.Rect(freeNode, Tex("UI_Button_Mulligan"), FreeBtnR, "Bg", QArt, null, true);
            var freeTx = MenuDraw.Text(freeNode, FreeTxR, Loc.T("Demo/DraftMode/FreeButton"), Color.white,
                                       "Button Text", 40f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(freeTx, Label.VAlign.Capline, FreeTxR);        // 原版 align 2/8192
            MenuDraw.Hit(freeNode, "Hit", FreeBtnR, QHit, ClickFree, freeBg, "UI_Button_Mulligan",
                         "UI_Button_Mulligan_hover", "UI_Button_Mulligan_Pressed",
                         clickCue: WindowSounds.PositiveCue);
            // ⚠️ 原版 `FreeTimeText`（170×70 @ (0,-75)，「Available in …」）是**免费时段未开放**那一支
            // （`GetFreeButtonInteractable` + `Update()` 刷倒计时）。我们没有「免费时段」这条服务端配置
            // ⇒ 不建（建了就得编一个倒计时）。原版那时它 `SetActive(false)` 之外的路径也读不到。
            return page;
        }

        // ------------------------------------------------------------ SelectWarlord 页
        Transform BuildWarlordPage(Transform root)
        {
            var page = MenuDraw.Node(root, "Draft Mode Select Warlord State", new PxRect(0f, 0f, 1920f, 1080f));
            var title = MenuDraw.Text(page, WlTitleR, Loc.T("Demo/DraftMode/SelectWarlord"), Color.white,
                                      "Title", 42f, QText, WlTitleR.W, 10f, 42f, 32.80f,
                                      face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(title, Label.VAlign.Midline, WlTitleR);        // 原版 4096 = Midline

            // `Generic Card Selector Menu`(ScrollRect h=1 v=0) > `Viewport`(Mask) > `Content`(HLG)
            var menu = MenuDraw.Node(page, "Generic Card Selector Menu", WlViewR);
            var view = MenuDraw.Node(menu, "Viewport", WlViewR);
            // 视口裁切 = 原版这颗 `Viewport` 上的 `Mask`（`RectMask2D` 的等价物）；padding / softness 全 0
            // （原版那一颗现读没有软边；硬边 ⇒ `(0,0)`）。
            var vc = view.gameObject.AddComponent<ViewportClip>();
            vc.padding = Vector4.zero;
            vc.softness = Vector2Int.zero;
            vc.CaptureNow();
            _warlordViewport = view;

            float total = WarlordsPerDraft * WlSlotW + (WarlordsPerDraft - 1) * WlSlotGap;
            _warlordScroll = new MenuScroll(WlViewR, WlViewR.x1, WlViewR.x1 + Mathf.Max(total, WlViewR.W), WlScrollNotch);
            _warlordScroll.Owner = gameObject;
            _warlordScroll.ClipNode = vc;
            _warlordScroll.OnChanged = RebuildWarlordCards;
            PointerLayer.RegisterScroll(_warlordScroll);
            _warlordContent = MenuDraw.Node(view, "Content", new PxRect(WlViewR.x1, WlViewR.y1,
                                                                      WlViewR.x1 + Mathf.Max(total, WlViewR.W), WlViewR.y2));

            // 空态提示（🔴 我们自拟 —— 原版这一页**没有空态节点**，S3 §四「没有可选战将」那一行：
            // 「原版没有空态文案（该页无 Empty/Warning 节点）⇒ 判定：查不到原版空态，需自建」）
            _warlordHint = MenuDraw.Node(page, "NoWarlordHint", new PxRect(360f, 470f, 1560f, 540f));
            MenuDraw.Text(_warlordHint, new PxRect(360f, 470f, 1560f, 540f),
                          OurText("本机卡池里没有可选战将 —— 轮抽开不了（这一句是我们自拟的：原版这一页没有空态节点）",
                                  "No warlord is available in this build - the draft cannot start."),
                          new Color(1f, 0.85f, 0.4f, 1f), "Text", 30f, QText, 1200f, 18f, 30f, 24f,
                          face: TmpFont.Face.LatinCardBody);
            _warlordHint.gameObject.SetActive(false);

            var cont = MenuDraw.Node(page, "Generic Simplified UI Button_updated", WlContBtnR);
            var contBg = MenuDraw.Rect(cont, Tex("UI_Button_Mulligan"), WlContBtnR, "Bg", QArt, null, true);
            var contTx = MenuDraw.Text(cont, WlContTxR, Loc.T("Battle/Tips/Continue"), Color.white,
                                       "Button Text", 65f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(contTx, Label.VAlign.Capline, WlContTxR);     // 原版 align 2/8192
            MenuDraw.Hit(cont, "Hit", WlContBtnR, QHit, () => ContinueAfterWarlord(), contBg, "UI_Button_Mulligan",
                         "UI_Button_Mulligan_hover", "UI_Button_Mulligan_Pressed",
                         clickCue: WindowSounds.PositiveCue);
            // ⚠️ 原版 `DEBUG_REROLL_BUTTON`（212.93×54.49，粉色）在**正式包预制体里也是 ON**，
            //     S3 §2.4 明写「施工时删掉或置灰」⇒ 本窗**不建**（不给玩家一颗没实现的调试钮）。
            return page;
        }

        void RebuildWarlordCards()
        {
            if (_warlordContent == null) return;
            for (int i = _warlordContent.childCount - 1; i >= 0; i--)
                RewardsWindow.DestroySafe(_warlordContent.GetChild(i).gameObject);
            _warlordNodes.Clear();
            bool empty = _warlords == null || _warlords.Count == 0;
            if (_warlordHint != null) _warlordHint.gameObject.SetActive(empty);
            if (empty)
            {
                Debug.LogWarning("[Draft] 可选战将 = 0 ⇒ 选将页**一张卡都不实例化**、`Continue` 保持灰"
                               + "（原版同解：`UICardSelector.Initialize(空列表)`；提示句是我们自拟的）。");
                return;
            }
            float y1 = WlSlotCy - WlSlotVisH * 0.5f;
            float y2 = WlSlotCy + WlSlotVisH * 0.5f;
            for (int i = 0; i < _warlords.Count; i++)
            {
                var def = _warlords[i];
                if (def == null) continue;
                float cx = i * (WlSlotW + WlSlotGap) + WlSlotW * 0.5f;
                var slotR = _warlordScroll.Shift(new PxRect(cx - WlSlotVisW * 0.5f, y1,
                                                            cx + WlSlotVisW * 0.5f, y2));
                var node = MenuDraw.Node(_warlordContent, "Generic Select Card Container", slotR);
                var content = MenuDraw.Node(node, "Content", slotR);

                // 卡面（原版 `BasicCardUI` 的 `2DCard`，视觉 472×751.4 —— 见 `WlCardBodyH`）
                float cardScale = WlCardBodyH / (CardView.Height * 108f);
                var cv = CardView.Create(content, BattleDriver.ToCardData(def, def.Faction), "CardUI");
                if (cv != null)
                {
                    cv.SetPose(MenuDraw.Local(content, slotR.x1, slotR.y1, slotR.x2, slotR.y2), 0f, cardScale);
                    cv.SetQueue(QCardBase + i);
                }

                // `Select Button`（原版 210.02×81.66 视觉，压在卡体下沿之外）
                var selR = new PxRect(slotR.x1 + WlSelBtnOff.x1, slotR.y1 + WlSelBtnOff.y1,
                                      slotR.x1 + WlSelBtnOff.x2, slotR.y1 + WlSelBtnOff.y2);
                var txR = new PxRect(slotR.x1 + WlSelTxOff.x1, slotR.y1 + WlSelTxOff.y1,
                                     slotR.x1 + WlSelTxOff.x2, slotR.y1 + WlSelTxOff.y2);
                var selNode = MenuDraw.Node(content, "Select Button", selR);
                var selBg = MenuDraw.Rect(selNode, Tex("UI_Button_Mulligan"), selR, "Bg", QArt, null, true);
                var selTx = MenuDraw.Text(selNode, txR, Loc.T("Battle/Prebattle/SelectButton"), Color.white,
                                          "Button Text", 40f, QText, face: TmpFont.Face.LatinCardBody);
                MenuDraw.SetVAlign(selTx, Label.VAlign.Capline, txR);        // 原版 align 2/8192
                string id = def.Id;
                MenuDraw.Hit(selNode, "Hit", selR, QHit, () => ChooseWarlord(id), selBg, "UI_Button_Mulligan",
                             "UI_Button_Mulligan_hover", "UI_Button_Mulligan_Pressed",
                             clip: WlViewR, clickCue: WindowSounds.SelectAvatarCue);
                _warlordNodes.Add(node);
            }
        }

        // ------------------------------------------------------------ SelectPack 页
        Transform BuildPacksPage(Transform root)
        {
            var page = MenuDraw.Node(root, "Draft Mode Select Packs State", new PxRect(0f, 0f, 1920f, 1080f));

            // `Warlord Image`（1072.5²，`sprite=0` ⇒ 运行时喂督军立绘；**sprite 为空 ⇒ 整节点关**，S3 §2.5）
            var wlNode = MenuDraw.Node(page, "Warlord Image", WarlordImgR);

            var content = MenuDraw.Node(page, "Content", new PxRect(118.70f, 156.60f, 1500.30f, 880.50f));
            MenuDraw.Text(content, SubTitleR, Loc.T("MenuDeck/HUD/ChooseCards"), Color.white,
                          "SubTitle", 42f, QText, SubTitleR.W, 10f, 42f, 35.16f,
                          face: TmpFont.Face.LatinCardBody);

            // `Packs`（HLG align MiddleCenter · spacing 11.1）：`Pack` 标签 + `Stage Counter`
            var packsRow = MenuDraw.Node(content, "Packs", new PxRect(698.60f, 317.60f, 1145.50f, 365.50f));
            var packLb = MenuDraw.Text(packsRow, PackLabelR, Loc.T("MainMenu/DraftPack"), Color.white,
                                       "Pack", 35f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignRight(packLb, PackLabelR);                  // 原版 hAlign = Right
            // 🔴 `Stage Counter` 的**文本**在 `RefreshPacksPage()` 里写（`{round+1}/{draftRounds}`）；
            //    初值写一次只是为了让批处理里「节点 + 字」都在。
            _stageCounter = MenuDraw.Text(packsRow, StageR, "1/" + DraftPacks.RoundsPerDraft, Color.white,
                                          "Stage Counter", 40f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(_stageCounter, StageR);                // 原版 hAlign = Left

            // `Packs Mask`（`Image(ty=1) + Mask`）+ 里面那层 `Packs Container`（HLG spacing 13）
            var mask = MenuDraw.Node(content, "Packs Mask", PacksMaskR);
            MenuDraw.Nine(mask, Tex("UI_Deck_Information_Back"), PacksMaskR, MaskBorder, MaskTexW, MaskTexH,
                          QPanelBack, new Color(0.802f, 0.802f, 0.802f, 1f), true, "Image",
                          new Vector4(MaskBorder.x * 0.5f, MaskBorder.y * 0.5f,
                                      MaskBorder.z * 0.5f, MaskBorder.w * 0.5f));
            var mvc = mask.gameObject.AddComponent<ViewportClip>();
            mvc.padding = Vector4.zero;
            mvc.softness = Vector2Int.zero;
            mvc.CaptureNow();
            _packsContainer = MenuDraw.Node(mask, "Packs Container", PacksContR);

            // 空态提示（🔴 我们自拟：原版包不够时**不报错、不占位**（S3 §四），我们照它「有几张画几张」，
            // 只在**一张都没有**时补一句人话 + 出声）
            _packHint = MenuDraw.Node(page, "NoPackHint", new PxRect(560f, 560f, 1360f, 630f));
            MenuDraw.Text(_packHint, new PxRect(560f, 560f, 1360f, 630f),
                          OurText("这个阵营没有可用的卡包了 —— 轮抽开不下去（这一句是我们自拟的）",
                                  "No card pack is available for this army - the draft cannot continue."),
                          new Color(1f, 0.85f, 0.4f, 1f), "Text", 30f, QText, 800f, 18f, 30f, 24f,
                          face: TmpFont.Face.LatinCardBody);
            _packHint.gameObject.SetActive(false);
            _warlordImgNode = wlNode;
            return page;
        }

        Transform _warlordImgNode;

        // ------------------------------------------------------------ Ongoing 页（含 Finished）
        Transform BuildOngoingPage(Transform root)
        {
            var page = MenuDraw.Node(root, "Draft Mode Ongoing State", new PxRect(0f, 0f, 1920f, 1080f));
            // `MainContent`（`DraftRunDisplay`，`localScale = 1.06` —— 下面的坐标**已含**那次缩放，现读值）
            var main = MenuDraw.Node(page, "MainContent", MainContR);
            _ongoingWarlordNode = MenuDraw.Node(main, "Warlord Image", OnWlImgR);
            var scaled = MenuDraw.Node(main, "Scaled Content", new PxRect(1030.70f, 498.40f, 1136.70f, 604.40f));
            var onTitle = MenuDraw.Text(scaled, OnTitleR, Loc.T("Demo/DraftMode/Title"), Color.white,
                                        "Game Mode Title", 45f, QText, OnTitleR.W, 10f, 45f, 47.73f,
                                        face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(onTitle, Label.VAlign.Midline, OnTitleR);     // 原版 align 2/4096
            // `Victories:` + 数字
            var vict = MenuDraw.Text(scaled, OnVictR, Loc.T("Demo/DraftMode/Victories"), Color.white,
                                     "Victories text", 45.80f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignRight(vict, OnVictR);
            _victoriesNum = MenuDraw.Text(scaled, OnVictNumR, "0", Color.white, "Victories text Number",
                                          89.23f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(_victoriesNum, OnVictNumR);
            MenuDraw.SetVAlign(_victoriesNum, Label.VAlign.Midline, OnVictNumR);   // 原版 align 1/4096

            // 胜场格（`Stages Container` GridLayoutGroup：cell 80² · spacing (23.7,30.04) · 2 列 · MiddleCenter）
            var winMarks = MenuDraw.Node(scaled, "Win Marks", new PxRect(697.90f, 373.80f, 1471.60f, 708.80f));
            // `Background Back`：原版那颗 `Image` **组件级禁用**（`m_Enabled = 0`）⇒ 原版不画 ⇒ 不建
            RebuildStageMarks(winMarks);

            // 败场（`Defeats:` + 3 格：原版 `Losses container` HLG spacing 9.86 · 格子 67.74²）
            var defeat = MenuDraw.Text(scaled, OnDefeatR, Loc.T("Demo/DraftMode/Losses"), Color.white,
                                       "Defeat Marks", 42.85f, QText, OnDefeatR.W, 18f, 50f, 36f,
                                       face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(defeat, OnDefeatR);
            var losses = MenuDraw.Node(scaled, "Losses container", OnLossesR);
            float lx = OnLossesR.x1;
            for (int i = 0; i < LossSlots; i++)
            {
                var cell = new PxRect(lx, OnLossesR.CY - LossCell * 0.5f, lx + LossCell, OnLossesR.CY + LossCell * 0.5f);
                MenuDraw.Node(losses, "Losses Info UI_ref", cell);
                // 空槽 = 一颗半透明黑块（原版 `Losses Info UI_ref` 的图 `ext10:-5957999898718987630` 在
                // 外部依赖里、本地没有 ⇒ 画占位并出声一次，⛔ 不拿别的骷髅图顶）
                MenuDraw.Rect(losses, CardArt.Solid(), cell, "Slot", QArt, new Color(0f, 0f, 0f, 0.45f));
                lx += LossCell + LossGap;
            }
            if (CardArt.MenuUi("40k_Defeat_Mark") == null)
                Debug.Log("[Draft] 败场格的原版图（`Losses Info UI_ref` 的 sprite）在外部依赖里、**本地没有**"
                        + " ⇒ 现在画的是半透明黑块占位（⛔ 没拿别的图顶）。");

            // `Quote End`（只有 Finished 时显示；按胜场档取一句，S3 §2.7）
            _quoteEnd = MenuDraw.Text(main, new PxRect(489.30f, 245f, 1501.60f, 305.50f), "", Color.white,
                                      "Quote End", 32f, QText, 1012.3f, 10f, 32f, 26f,
                                      face: TmpFont.Face.LatinCardBody);
            if (_quoteEnd != null) _quoteEnd.gameObject.SetActive(false);

            // `Reset Event Button`（Ongoing 显示 · Finished 隐藏；文案 = `Demo/DraftMode/ResignDraftButton`）
            _resetNode = MenuDraw.Node(scaled, "Reset Event Button", OnResetR);
            var resetBg = MenuDraw.Rect(_resetNode, Tex("UI_Button_Mulligan"), OnResetR, "Bg", QArt, null, true);
            var resetTx = MenuDraw.Text(_resetNode, OnResetTxR, Loc.T("Demo/DraftMode/ResignDraftButton"),
                                        Color.white, "Button Text", 40f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(resetTx, Label.VAlign.Capline, OnResetTxR);   // 原版 align 2/8192
            MenuDraw.Hit(_resetNode, "Hit", OnResetR, QHit, Resign, resetBg, "UI_Button_Mulligan",
                         "UI_Button_Mulligan_hover", "UI_Button_Mulligan_Pressed");

            // `To Battle Button`（Ongoing 显示 · Finished 隐藏）
            _toBattleNode = MenuDraw.Node(page, "To Battle Button", OnBattleR);
            var battleBg = MenuDraw.Rect(_toBattleNode, Tex("UI_Button_Mulligan"), OnBattleR, "Bg", QArt, null, true);
            var battleTx = MenuDraw.Text(_toBattleNode, OnBattleTxR, Loc.T("Demo/ToBattle"), Color.white,
                                         "Button Text", 74.25f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(battleTx, Label.VAlign.Capline, OnBattleTxR);  // 原版 align 2/8192
            MenuDraw.Hit(_toBattleNode, "Hit", OnBattleR, QHit, () => LaunchBattle(), battleBg, "UI_Button_Mulligan",
                         "UI_Button_Mulligan_hover", "UI_Button_Mulligan_Pressed",
                         clickCue: WindowSounds.PositiveCue);

            // `Debug Win` / `Debug Battle Button Button`：原版 `OnEnable` **强制 SetActive(false)**（正式包也不显示）
            // ⇒ ⛔ 不建。

            _rewardPanelRoot = BuildRewardPanel(page);
            return page;
        }

        Transform _ongoingWarlordNode, _resetNode, _toBattleNode;
        Label _quoteEnd;

        void RebuildStageMarks(Transform holder)
        {
            for (int i = holder.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(holder.GetChild(i).gameObject);
            // ⚠️ 本批**没有战斗结果回灌**（战斗内不动）⇒ 胜场恒 0：4 个空槽。
            int n = DraftPacks.RoundsPerDraft;
            int cols = 2;
            float gridW = cols * StageCell + (cols - 1) * StageGapX;
            float x0 = OnStagesR.CX - gridW * 0.5f;
            int rows = Mathf.CeilToInt(n / (float)cols);
            float gridH = rows * StageCell + (rows - 1) * StageGapY;
            float y0 = OnStagesR.CY - gridH * 0.5f;
            for (int i = 0; i < n; i++)
            {
                int c = i % cols, r = i / cols;
                float x = x0 + c * (StageCell + StageGapX), y = y0 + r * (StageCell + StageGapY);
                var cell = new PxRect(x, y, x + StageCell, y + StageCell);
                MenuDraw.Node(holder, "Stage Info UI_ref", cell);
                MenuDraw.Rect(holder, CardArt.Solid(), cell, "Slot", QArt, new Color(0f, 0f, 0f, 0.45f));
            }
            Debug.Log("[Draft] 胜场格画了 " + n + " 个空槽（= `draftRounds`；原版 `SetupStageMarks` 逐格实例化）。"
                    + "⚠️ 本批**没有战斗结果回灌**（战斗内不动）⇒ 胜场恒 0 —— 不是缺陷，是范围。");
        }

        // ------------------------------------------------------------ 奖励面板（Finished）
        Transform BuildRewardPanel(Transform page)
        {
            var panel = MenuDraw.Node(page, "Reward Info Panel", RewardPanelR);
            var info = MenuDraw.Node(panel, "Reward Info", RewardPanelR);
            // `Highlight Crate`（`Crate Border Highlight` · 金色 0.427 · **只画边框** fillCenter=0）
            MenuDraw.Nine(info, Tex("Crate Border Highlight"), CrateGlowR, new Vector4(0f, 0f, 0f, 0f), 128f, 128f,
                          QArt1, new Color(0.98f, 0.801f, 0.0588f, 0.427f), false, "Highlight Crate",
                          null, null, default(Vector2));
            // `Crate`（199) 宝箱图按 `CurrentStage` 五档取（阈值 3/5/7/9/13 —— S3 §2.8）；
            //    本批**没有 stage**（胜场不回灌）⇒ 取第 0 档（= `stage < 3` 那支）
            MenuDraw.Rect(info, Tex("40k_Crate_Tier1_Iron"), CrateR, "Crate", QArt, null, true);
            var btn = MenuDraw.Node(info, "Collect Button", CollectR);
            var bg = MenuDraw.Rect(btn, Tex("UI_Button_Mulligan"), CollectR, "Bg", QArt, null, true);
            var collectTx = MenuDraw.Text(btn, CollectTxR, Loc.T("MenuShop/CollectButton"), Color.white,
                                          "Button Text", 40f, QText, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(collectTx, Label.VAlign.Capline, CollectTxR);  // 原版 align 2/8192
            MenuDraw.Hit(btn, "Hit", CollectR, QHit, CollectReward, bg, "UI_Button_Mulligan",
                         "UI_Button_Mulligan_hover", "UI_Button_Mulligan_Pressed",
                         clickCue: WindowSounds.PositiveCue);
            panel.gameObject.SetActive(false);
            return panel;
        }

        // ------------------------------------------------------------ 右栏（Deck Info Panel）
        Transform BuildDeckInfoPanel(Transform root)
        {
            var panel = MenuDraw.Node(root, "Draft Mode Deck Info Panel", DeckPanelR);
            // `Buttons`（HLG + ToggleGroup；三颗互斥）—— 原版那颗 bar 宽 314.05（现读），
            // 三颗 82.09×78.45 · spacing 12.98 · align MiddleLeft ⇒ 从 bar 左沿起排。
            MenuDraw.Node(panel, "Buttons", DeckTabsBarR);
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var r = DeckToggleR[i];
                var tog = MenuDraw.Node(panel, DeckToggleNames[i], r);
                var bgNode = MenuDraw.Node(tog, "button_bg", r);
                var bgQ = MenuDraw.Rect(bgNode, Tex(i == _deckTab ? ToggleArtOnOff[0] : ToggleArtOnOff[1]), r,
                                        "Image", QArt, new Color(0.868f, 0.0264f, 0f, 1f));
                // `Icon` 64×64（左/上内缩 9.1 / 7.3；第三颗 ×1.1）
                bool big = i == 2;
                float side = big ? 70.40f : 64f;
                float icx = r.x1 + 9.1f + 32f, icy = r.y1 + 7.3f + 32f;
                var icR = new PxRect(icx - side * 0.5f, icy - side * 0.5f, icx + side * 0.5f, icy + side * 0.5f);
                MenuDraw.Node(tog, "Icon", icR);
                var icon = Tex(DeckToggleIconArt[i]);
                if (icon != null) MenuDraw.Rect(tog, icon, icR, "Image", QArt1, null, true);
                else Debug.LogWarning("[Draft] 右栏 toggle 图标「" + DeckToggleIconArt[i] + "」**本地没有**"
                                    + "（`Resources/Art/ui*/` 0 命中）⇒ 这一格只有底、没有图标（⛔ 不拿别的图顶）");
                MenuDraw.Hit(tog, "Hit", r, QHit, () => SetDeckTab(idx), bgQ, ToggleArtOnOff[1],
                             ToggleArtOnOff[0], ToggleArtOnOff[0], clickCue: WindowSounds.SelectToggleCue);
            }
            // `Generic Window Red Background Small`：原版**初始 act=off**（`UIEffectTweener` 只在
            // 「选包成功」那一下 Play 闪一次，S3 §2.9 末行）⇒ 节点照建、**默认关着**；
            // 本轮不做那一下闪光（动画时长常量只在 `DAT_*` 里）—— 见 `PickPack` 那条出声。
            var flash = MenuDraw.Nine(panel, Tex("UI_Deck_Selection_Back"),
                                      new PxRect(1500f, 215.30f, 1900f, 911.76f),
                                      new Vector4(0f, 325f, 0f, 35f), 439f, 664f, QPanel, null, true,
                                      "Generic Window Red Background Small");
            if (flash != null) flash.SetActive(false);
            // `content` 里只有三页；本批建两页（Deck Info / Card List），第三页（Alliance）**留空 + 出声**
            _deckTitle = MenuDraw.Text(panel, DeckTitleR, Loc.T("MenuDeck/HUD/DeckDescription/DeckInfo"), Color.white,
                                       "Title", 36f, QText, DeckTitleR.W, 18f, 36f, 36f,
                                       face: TmpFont.Face.LatinCardBody);
            _cardsCounter = MenuDraw.Text(panel, DeckCounterR, "", Color.white, "CardCounter", 36f, QText,
                                          face: TmpFont.Face.LatinCardBody);
            MenuDraw.Text(panel, DeckEnergyR, Loc.T("Demo/EnergyBalance/EnergyBalance"), Color.white,
                          "Energy balance", 36f, QText, DeckEnergyR.W, 18f, 36f, 36f,
                          face: TmpFont.Face.LatinCardBody);
            _deckTabHolder = MenuDraw.Node(panel, "Cards in deck panel", DeckListR);
            return panel;
        }
        static readonly string[] DeckToggleNames = { "Deck Info Toggle", "Card List Toggle", "Alliance Info Toggle" };

        // ============================================================ 换页（= 原版 `GetTab<T>()`）
        /// <summary>state → 页节点（原版 `GetTabFromState` 那张表）。
        /// ⚠️ `Intro` / `PayScreen` **都落 Pay 页**（`GetTabFromState(0)` 的 else 分支 —— S3 §〇·3 / §七·1）；
        /// `OngoingRun` / `Finished` 同落 Ongoing 页（只是显隐不同）。</summary>
        Transform PageFor(DraftModeState s)
        {
            switch (s)
            {
                case DraftModeState.SelectWarlord: return _pageWarlord;
                case DraftModeState.SelectPack: return _pagePacks;
                case DraftModeState.OngoingRun:
                case DraftModeState.Finished: return _pageOngoing;
                default: return _pagePay;      // Intro / PayScreen
            }
        }

        public void GoToState(DraftModeState s)
        {
            // `Collected`（22）：原版「窗口自行关闭/刷新」（S3 §一 的表）⇒ 关窗。
            if (s == DraftModeState.Collected)
            {
                Debug.Log("[Draft] 状态 `Collected(22)` ⇒ 关窗（原版那一档没有独立页）。");
                Close();
                return;
            }
            var page = PageFor(s);
            State = s;
            SetOn(_pagePay, page == _pagePay);
            SetOn(_pageWarlord, page == _pageWarlord);
            SetOn(_pagePacks, page == _pagePacks);
            SetOn(_pageOngoing, page == _pageOngoing);

            bool showDeckPanel = (s == DraftModeState.SelectPack || s == DraftModeState.OngoingRun);
            SetOn(_deckPanelRoot, showDeckPanel);
            SetOn(_rewardPanelRoot, s == DraftModeState.Finished);
            // Finished：隐藏 `To Battle` / `Reset Event Button` / `Timer`，显示 `Quote End`（S3 §2.7 判据）
            bool finished = (s == DraftModeState.Finished);
            SetOn(_toBattleNode, !finished);
            SetOn(_resetNode, !finished);
            if (_quoteEnd != null) _quoteEnd.gameObject.SetActive(finished);
            if (finished && _quoteEnd != null)
                _quoteEnd.SetText(Loc.T("Demo/DraftMode/Quote12Victories"));   // 现读只有 5 档文案，本批 win 恒 0 ⇒ 第 12 档兜底

            if (page == _pageWarlord) RebuildWarlordCards();
            if (page == _pagePacks) RefreshPacksPage();
            if (page == _pageOngoing) RefreshOngoingPage();
            if (showDeckPanel) RefreshDeckPanel();
            Debug.Log("[Draft] 换页 ⇒ " + s + "（页 = " + (page != null ? page.name : "<none>") + "）");
        }

        static void SetOn(Transform t, bool on) { if (t != null) t.gameObject.SetActive(on); }

        // ------------------------------------------------------------ 各页刷新
        void RefreshPacksPage()
        {
            if (_packsContainer == null) return;
            // `Warlord Image`：按选中战将的**方形立绘**喂（原版运行时喂 `warlord.fullHeroSprite`；
            // sprite 为空 ⇒ 整节点关）。我们走 `CardArt.TryDeckHero`（2048² 方形立绘，与排位窗同一条口径）。
            if (_warlordImgNode != null)
            {
                // 先清旧 quad（本页每次重刷都会重建这一层；不清就会一次叠一层）
                for (int i = _warlordImgNode.childCount - 1; i >= 0; i--)
                    RewardsWindow.DestroySafe(_warlordImgNode.GetChild(i).gameObject);
                Texture2D hero = null;
                bool ok = !string.IsNullOrEmpty(_warlordId) && CardArt.TryDeckHero(_warlordId, out hero) && hero != null;
                if (ok) MenuDraw.Rect(_warlordImgNode, hero, WarlordImgR, "Image", QPanelBack, null, true);
                _warlordImgNode.gameObject.SetActive(ok);      // 取不到 ⇒ 整节点关 + 出声
                if (!ok)
                    Debug.LogWarning("[Draft] `Warlord Image` 取不到方形立绘（" + (_warlordId ?? "<未选战将>")
                                   + "）⇒ **整节点关**（原版同解：`sprite == null ⇒ SetActive(false)`）。");
            }

            _offered = RollOffers();
            for (int i = _packsContainer.childCount - 1; i >= 0; i--)
                RewardsWindow.DestroySafe(_packsContainer.GetChild(i).gameObject);
            _packCardViews.Clear();

            _stageCounter.SetText((CurrentDraftRound + 1) + "/" + DraftPacks.RoundsPerDraft);
            MenuDraw.AlignLeft(_stageCounter, StageR);     // 换字之后要重新左对齐（原版 hAlign = Left）
            bool empty = _offered.Count == 0;
            if (_packHint != null) _packHint.gameObject.SetActive(empty);
            if (empty)
                Debug.LogWarning("[Draft] 第 " + (CurrentDraftRound + 1) + " 轮的包 = **0 个**（`RollRound` 返回空）"
                               + " ⇒ 这一页不实例化包卡（原版同解：有几张画几张、不报错不占位；提示句是我们自拟的）。");

            float cy = PacksContR.CY;
            for (int i = 0; i < _offered.Count; i++)
            {
                float x1 = PacksContR.x1 + i * (PackW + PacksContSpacing);
                var r = new PxRect(x1, cy - PackH * 0.5f, x1 + PackW, cy + PackH * 0.5f);
                BuildPackCard(_packsContainer, _offered[i], i, r);
            }
            Debug.Log("[Draft] 第 " + (CurrentDraftRound + 1) + " 轮给出 **" + _offered.Count + "** 个包"
                    + "（= `DraftPacks.RollRound(seed=" + _seed + ", round=" + CurrentDraftRound
                    + ", army=" + WarlordArmyId() + ", count=" + DraftPacks.PacksPerRoundOffered + ")` 的返回条数）");
        }

        void BuildPackCard(Transform parent, DraftPacks.PackDto pack, int index, PxRect r)
        {
            var node = MenuDraw.Node(parent, "Prebuilt Pack UI", r);
            var content = MenuDraw.Node(node, "Content", r);
            // 包底 + 橙色描边（原版 `BackgroundImage` = `UI_Deck_Information_submenu_Back` 九宫 18,18,18,18
            // · `Highlight` = `OctagonUI Border SDF` 九宫 52² 橙色 (1,0.558,0.078)）
            MenuDraw.Nine(content, Tex("UI_Deck_Information_submenu_Back"),
                          new PxRect(r.x1, r.y1, r.x2, r.y2), new Vector4(18f, 18f, 18f, 18f), 69f, 63f,
                          QPanelBack2, null, true, "BackgroundImage", new Vector4(9f, 9f, 9f, 9f));
            MenuDraw.Nine(content, Tex("OctagonUI Border SDF"),
                          new PxRect(r.x1 + 2f, r.y1 + 2f, r.x2 - 2f, r.y2 - 2f),
                          new Vector4(52f, 52f, 52f, 52f), 128f, 128f, QPanelArt,
                          new Color(1f, 0.558f, 0.078f, 1f), false, "Highlight", null, null, default(Vector2));

            MenuDraw.Text(content, Abs(r, PackTitleR), pack.packName ?? pack.packId, Color.white,
                          "Pack title", 35f, QText, PackTitleR.W, 18f, 35f, 36f,
                          face: TmpFont.Face.LatinCardBody);
            MenuDraw.Nine(content, Tex("40k_Separator Fade Sides Horizontal"), Abs(r, PackSepR),
                          new Vector4(63f, 0f, 63f, 0f), 128f, 4f, QPanelArt,
                          new Color(0.757f, 0.502f, 0.122f, 1f), true, "Separator",
                          new Vector4(63f / 2.89f, 0f, 63f / 2.89f, 0f));

            // 3 张缩略卡（每包恒 3 张 —— `DraftPacks.CardsPerPack`；🔴 布局是我们挑的，见 `PackCardsArea`）
            string[] ids = pack.cardIds ?? new string[0];
            float cw = PackCardsArea.W / 3f - 10f;                 // 3 列，列间 15
            float ch = PackCardsArea.H;
            for (int k = 0; k < ids.Length && k < 3; k++)
            {
                var def = CardDatabase.FindById(CardPool(), ids[k]);
                if (def == null)
                {
                    Debug.LogWarning("[Draft] 包 `" + pack.packId + "` 第 " + (k + 1) + " 张卡 id `" + ids[k]
                                   + "` 在卡池里查不到 ⇒ 那一格**没画**（⛔ 不假装画了）。");
                    continue;
                }
                float cx1 = r.x1 + PackCardsArea.x1 + k * (cw + 15f);
                var cellR = new PxRect(cx1, r.y1 + PackCardsArea.y1, cx1 + cw, r.y1 + PackCardsArea.y2);
                var cv = CardView.Create(content, BattleDriver.ToCardData(def, def.Faction), "CardUI");
                if (cv != null)
                {
                    float sc = ch / (CardView.Height * 108f);
                    cv.SetPose(MenuDraw.Local(content, cellR.x1, cellR.y1, cellR.x2, cellR.y2), 0f, sc);
                    cv.SetQueue(QCardBase + index * 3 + k);
                    _packCardViews.Add(cv);
                }
                var nameR = new PxRect(cellR.x1, r.y1 + PackNameArea.y1, cellR.x2, r.y1 + PackNameArea.y2);
                MenuDraw.Text(content, nameR, CardText.Name(def), Color.white, "Card Name", 18f, QText,
                              nameR.W, 12f, 18f, 18f, face: TmpFont.Face.LatinCardBody);
            }

            // `Select Button`（222.10×49.49 · 文案 `Battle/Prebattle/SelectButton`）
            var selR = Abs(r, PackSelR);
            var selNode = MenuDraw.Node(content, "Select Button", selR);
            var bg = MenuDraw.Rect(selNode, Tex("UI_Button_Mulligan"), selR, "Bg", QArt, null, true);
            var packSelTx = MenuDraw.Text(selNode, Abs(r, PackSelTxR), Loc.T("Battle/Prebattle/SelectButton"),
                                          Color.white, "Button Text", 36f, QText,
                                          face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(packSelTx, Label.VAlign.Capline, Abs(r, PackSelTxR));   // 原版 align 2/8192
            string key = pack.key;
            MenuDraw.Hit(selNode, "Hit", selR, QHit, () => PickPack(key), bg, "UI_Button_Mulligan",
                         "UI_Button_Mulligan_hover", "UI_Button_Mulligan_Pressed",
                         clickCue: WindowSounds.PositiveCue);
            // `BackgroundClick`（点包卡空白处 ⇒ 预览弹窗）：本批不做预览（见 `Build()` 末尾那条出声），
            // 但**照原版吸收**这一块的点击（原版那颗 `Image(a=0) + UIGenericEventCatcher`）
            MenuDraw.Absorb(content, "BackgroundClick", new PxRect(r.x1, r.y1, r.x2, r.y2), QShade, QHitContentMin);
        }

        /// <summary>把「相对包卡左上角」的矩形搬成绝对坐标。</summary>
        static PxRect Abs(PxRect outer, PxRect off)
        {
            return new PxRect(outer.x1 + off.x1, outer.y1 + off.y1, outer.x1 + off.x2, outer.y1 + off.y2);
        }

        void RefreshOngoingPage()
        {
            // 胜场恒 0（本批没有战斗结果回灌 —— 见 `RebuildStageMarks` 的出声）
            if (_victoriesNum != null) _victoriesNum.SetText("0");
            if (_ongoingWarlordNode != null)
            {
                for (int i = _ongoingWarlordNode.childCount - 1; i >= 0; i--)
                    RewardsWindow.DestroySafe(_ongoingWarlordNode.GetChild(i).gameObject);
                Texture2D hero = null;
                bool ok = !string.IsNullOrEmpty(_warlordId) && CardArt.TryDeckHero(_warlordId, out hero) && hero != null;
                if (ok) MenuDraw.Rect(_ongoingWarlordNode, hero, OnWlImgR, "Image", QPanelBack, null, true);
                _ongoingWarlordNode.gameObject.SetActive(ok);     // 原版 §2.13 那份 `Warlord Image` 初始就是 off
            }
        }

        void SetDeckTab(int tab)
        {
            _deckTab = Mathf.Clamp(tab, 0, 2);
            RefreshDeckPanel();
        }

        void RefreshDeckPanel()
        {
            if (_deckPanelRoot == null) return;
            // 三颗互斥（原版 `ToggleGroup`）：后两页的 `SetActive` 由这里统一给
            for (int i = 0; i < 3; i++)
            {
                var tog = _deckPanelRoot.Find(DeckToggleNames[i]);
                if (tog == null) continue;
                var bg = tog.Find("button_bg");
                var q = bg != null ? bg.Find("Image") : null;
                var iq = q != null ? q.GetComponent<ImageQuad>() : null;
                var tex = Tex(i == _deckTab ? ToggleArtOnOff[0] : ToggleArtOnOff[1]);
                if (iq != null && tex != null) iq.SetTexture(tex, true);
            }
            int n = _selectedCards.Count;
            if (_cardsCounter != null)
                _cardsCounter.SetText(Loc.T("Demo/EnergyBalance/Cards") + " " + n + "/"
                                    + (DraftPacks.RoundsPerDraft * DraftPacks.CardsPerPack));
            // 页内容：0 = Deck Info（标题 + 计数 + 能量平衡已经在 panel 上，恒显示）
            //         1 = Card List（列出已拿到的卡名）
            //         2 = Alliance Info（原版要联盟数据/排行榜，本地 0）⇒ 自拟一句提示 + 出声
            if (_deckTabHolder == null) return;
            for (int i = _deckTabHolder.childCount - 1; i >= 0; i--)
                RewardsWindow.DestroySafe(_deckTabHolder.GetChild(i).gameObject);
            if (_deckTab == 1)
            {
                int rows = Mathf.Min(n, Mathf.FloorToInt(DeckListR.H / DeckRowH));
                for (int i = 0; i < rows; i++)
                {
                    var def = CardDatabase.FindById(CardPool(), _selectedCards[i]);
                    var rr = new PxRect(DeckListR.x1, DeckListR.y1 + i * DeckRowH,
                                        DeckListR.x2, DeckListR.y1 + (i + 1) * DeckRowH);
                    MenuDraw.Text(_deckTabHolder, rr, (i + 1) + ". " + (def != null ? CardText.Name(def) : _selectedCards[i]),
                                  Color.white, "Card Row", 26f, QText, rr.W, 16f, 26f, 24f,
                                  face: TmpFont.Face.LatinCardBody);
                }
            }
            else if (_deckTab == 2)
            {
                MenuDraw.Text(_deckTabHolder, new PxRect(DeckListR.x1, DeckListR.y1, DeckListR.x2, DeckListR.y1 + 120f),
                              OurText("联盟信息要服务端的联盟/排行榜数据，本机没有（这一句是我们自拟的）",
                                      "Alliance info needs server-side alliance and ranking data."),
                              new Color(1f, 0.85f, 0.4f, 1f), "Alliance Hint", 26f, QText, DeckListR.W,
                              16f, 26f, 24f, face: TmpFont.Face.LatinCardBody);
                Debug.Log("[Draft] `Alliance Info` 页要 `LeaderboardManager.GetCachedLeaderboard(...)` 的数据"
                        + "（原版那段调用见 S3 §2.9）—— 本机没有服务器 ⇒ **页留空 + 一句自拟提示**（⛔ 不编联盟数据）。");
            }
        }

        // ============================================================ 动作（按钮回调 = 自检直调，同一份实现）
        /// <summary>Pay 页 `Free Button` ⇒ 选将页（原版 `FreeButtonOnClick` → `FreeEventEntrance` 成功后
        /// `GoToState(SelectWarlord)`；我们本地没有服务端 ⇒ 直接走成功那一支，并**出声说明**）。</summary>
        public void ClickFree()
        {
            if (State != DraftModeState.PayScreen && State != DraftModeState.Intro)
            { Debug.LogWarning("[Draft] `Free` 只在 Pay 页有效（当前 " + State + "）"); return; }
            Debug.Log("[Draft] `Free Button` ⇒ 进选将页（原版 `Draft.FreeEventEntrance` 是服务端调用、"
                    + "本地没有服务器 ⇒ **本地直接走成功那一支**，这一条是我们挑的）。");
            GoToState(DraftModeState.SelectWarlord);
        }

        /// <summary>选一位战将（本地暂存，原版 `SelectWarlord(card)`；此时 `Continue` 才变可点）。</summary>
        public bool ChooseWarlord(string cardId)
        {
            if (State != DraftModeState.SelectWarlord)
            { Debug.LogWarning("[Draft] 选将只在 SelectWarlord 页有效（当前 " + State + "）"); return false; }
            bool found = false;
            for (int i = 0; i < _warlords.Count; i++)
                if (_warlords[i] != null && _warlords[i].Id == cardId) { found = true; break; }
            if (!found)
            {
                Debug.LogWarning("[Draft] 选的战将 `" + cardId + "` **不在本局候选里** ⇒ 忽略（原版 `UICardSelector` 只给候选卡）。");
                return false;
            }
            _warlordId = cardId;
            Debug.Log("[Draft] 选中战将 `" + cardId + "`（阵营 " + WarlordFaction + " / army " + WarlordArmyId() + "）");
            return true;
        }

        /// <summary>`Continue` ⇒ 选包页（原版 `ContinueButtonOnClick` → `PickWarlord` 成功后
        /// `CheckWarlordSelectionFinished` → Packs 页）。没选战将时按钮**本来就是灰的**（原版
        /// `continueButton.interactable = false`，S3 §2.4）⇒ 这里如实拒绝并出声。</summary>
        public bool ContinueAfterWarlord()
        {
            if (State != DraftModeState.SelectWarlord) return false;
            if (string.IsNullOrEmpty(_warlordId))
            {
                Debug.LogWarning("[Draft] `Continue` 点不动：**还没选战将**（原版 `Continue` 出厂就是灰的，"
                               + "点了等于没点 —— 这一下不出弹窗，与 `interactable = false` 同观感）。");
                return false;
            }
            // 战将定下来之后，候选包池就定了（按**阵营**，不是按战将 —— `DraftPacks.RollRound` 的粒度）
            GoToState(DraftModeState.SelectPack);
            return true;
        }

        /// <summary>选一个包：+3 张、轮次 +1；没到末轮 ⇒ 直接给下一轮的包；到末轮 ⇒ 进 Ongoing 页。
        /// （原版 `PackSelected` → `PickPack` → `UpdateDeckInfoPanelAnimSync`，S3 §2.5 那两行。）</summary>
        public bool PickPack(string packKey)
        {
            if (State != DraftModeState.SelectPack)
            { Debug.LogWarning("[Draft] 选包只在 SelectPack 页有效（当前 " + State + "）"); return false; }
            DraftPacks.PackDto pack = null;
            for (int i = 0; i < _offered.Count; i++)
                if (_offered[i] != null && _offered[i].key == packKey) { pack = _offered[i]; break; }
            if (pack == null)
            {
                Debug.LogWarning("[Draft] `" + packKey + "` **不在这一轮给出的包里** ⇒ 忽略（原版只认 `packsUIContainer` 里那几张）。");
                return false;
            }
            _pickedKeys.Add(pack.key);
            if (pack.cardIds != null) _selectedCards.AddRange(pack.cardIds);
            _deck = null;                     // 牌变了 ⇒ 之前组的那副作废
            // 原版这一刻：右栏 `DOPunchScale` + `UIEffectTweener.Play`（红底闪一下，S3 §2.5「进下一轮」那一行）。
            // 本轮不做动画（时长常量只在 `DAT_*` 里）—— 出声一次，⛔ 不假装播了。
            Debug.Log("[Draft] 选包成功：原版这里右栏会 `DOPunchScale` + 闪一下红底（`UIEffectTweener`）—— "
                    + "**本轮不做动画**（S3 §五·3：时长/缓动只有 `DAT_*` 符号，查不到真值）。");
            Debug.Log("[Draft] 选包 `" + pack.packId + "`（" + pack.packName + "）⇒ 手里 "
                    + _selectedCards.Count + " 张 / 第 " + CurrentDraftRound + " 轮完成");
            // 原版：`CurrentDraftRound < draftRounds` ⇒ `ShowPacks()` 再来一轮；否则切 Ongoing
            if (CurrentDraftRound < DraftPacks.RoundsPerDraft)
            {
                RefreshPacksPage();
                RefreshDeckPanel();
            }
            else
            {
                GoToState(DraftModeState.OngoingRun);
            }
            return true;
        }

        /// <summary>`To Battle`：组 ClosedDeck ⇒ 交给既有开战通道 ⇒ 切战场（批处理只记账）。
        /// ⚠️ **战斗内一行不动**。</summary>
        public bool LaunchBattle()
        {
            if (State != DraftModeState.OngoingRun)
            { Debug.LogWarning("[Draft] `To Battle` 只在 OngoingRun 页有效（当前 " + State + "）"); return false; }
            if (string.IsNullOrEmpty(_warlordId))
            {
                Debug.LogWarning("[Draft] 没有战将 ⇒ 开不了局（原版 `GetClosedDeckByPackIds(warlordId, …)` 需要它）。");
                if (Manager != null) Manager.ShowPopUp(Loc.T("MenuDeck/Error/CantStartNoWarlord"),
                                                       Loc.T("MainMenu/General/OK"), null);
                return false;
            }
            if (_pickedKeys.Count == 0)
            {
                Debug.LogWarning("[Draft] 一个包都没选 ⇒ 开不了局。");
                if (Manager != null) Manager.ShowPopUp(Loc.T("Battle/Tips/NoCards"), Loc.T("MainMenu/General/OK"), null);
                return false;
            }
            // `ClosedDeck.warlordId` 是 **int**（原版 `GetClosedDeckByPackIds` 的标记位、不参与选卡 —— D1 已核）；
            // 我们的战将 id 是字符串（`AM3`）⇒ 这里放**阵营 id** 当标记。🔴 这一条是我们挑的。
            var deck = DraftPacks.BuildClosedDeck(WarlordArmyId(), _pickedKeys);
            if (deck == null) return false;
            _deck = deck;

            // 交给既有通道：`PrebuiltDecks.PendingSource` + `BattleDriver._pendingPlayMode`
            var pre = new PrebuiltDecks.Deck
            {
                deckId = "draft_" + _seed,
                name = "Draft " + _seed,
                nameZh = "选秀 " + _seed,
                faction = WarlordFaction,
                armyOrder = WarlordArmyId(),
                // 🔴 **我们挑的**：`GameplayVariables.For` 里**没有 ClosedDeck 那一档**
                // （`RuleEngine/Core/GameplayVariables.cs:444-449` ⇒ 落到 Classic **30 张**），
                // 而轮抽牌恒 12 张（4 包 × 3）⇒ 这副牌按 **Skirmish（12 张 / 4 传说）** 那套参数打；
                // 真实模式号仍走 `SetPendingPlayMode(GameMode.ClosedDeck)`（日志/记录/骷髅口径用那个）。
                gameMode = (int)GameMode.Skirmish,
                difficulty = 0,
                heroId = _warlordId,
                cardback = null,
                cardbackFrom = "none",
                cardIds = deck.cardIds,
                complete = true,
                missing = new string[0],
                problems = new string[0],
                overLegendary = new string[0],
            };
            PrebuiltDecks.SetPendingBattleDeck(pre);
            BattleDriver.SetPendingPlayMode(GameMode.ClosedDeck);
            StartedBattle = true;

            string scene = ArenaByArmy.BattleSceneNameFor(WarlordFaction);
            Debug.Log("[Draft] 开战：`ClosedDeck` · 战将 `" + _warlordId + "` · " + deck.cardIds.Length
                    + " 张（" + deck.packIds.Length + " 包）· 战场 `" + scene + "`"
                    + "（本局模式号 = `ClosedDeck(7)`；⚠️ 参数表按 `Skirmish` 那套 12 张 —— 见 `LaunchBattle` 注释）");
            if (Application.isBatchMode)
            {
                Debug.Log("[Draft] （批处理：不切场景，只记账 —— `StartedBattle = true`、"
                        + "`PendingPlayMode = " + BattleDriver.PendingPlayMode + "`）");
                return true;
            }
            ShellRuntime.LoadSceneWithFade(scene);
            return true;
        }

        /// <summary>`Reset Event Button`（原版文案 = `Demo/DraftMode/ResignDraftButton`「放弃」）：
        /// 原版 `ResignButtonClick` → `ShowPopUp(ExitDraftQuestion)` 二选一 → 确认后 `RefundRun`（S3 §三·16）。
        /// 我们：弹同一句官方词条，**OK = 重开一轮**（退款/服务端那半边本地没有 ⇒ 不假装退了什么）。</summary>
        public void Resign()
        {
            Debug.Log("[Draft] `Resign`（原版 `Draft.RefundRun` 是服务端调用 —— 本地没有服务器 ⇒ "
                    + "**只重开一轮、不假装退款**）。");
            if (Manager != null)
                Manager.ShowPopUp(Loc.T("Demo/DraftMode/ExitDraftQuestion"), Loc.T("MainMenu/General/OK"),
                                  () => BeginRun(unchecked((int)(DateTime.Now.Ticks & 0x7FFFFFFF))));
            else
                BeginRun(unchecked((int)(DateTime.Now.Ticks & 0x7FFFFFFF)));
        }

        /// <summary>`Collect Button`（只有 Finished 有）：原版 `Draft.CollectRewards` 是服务端调用，
        /// 发什么奖在 `DraftEventData.Rewards`（本地 0 命中）⇒ 我们**如实说「奖励表在服务端」**，
        /// 然后走 `Collected`（关窗）。⛔ 不发假奖。</summary>
        public void CollectReward()
        {
            Debug.LogWarning("[Draft] `Collect reward`：**奖励表（`DraftEventData.Rewards`）在服务端、本地 0 命中**"
                           + " ⇒ 本窗**不发任何奖励**，只把状态推到 `Collected`（关窗）。"
                           + "⛔ 不编奖励（铁律：查不到就说查不到）。");
            GoToState(DraftModeState.Collected);
        }

        // ============================================================ 抽将 / 抽包 / 查表
        static List<CardDef> _cardPool;
        static List<CardDef> CardPool()
        {
            if (_cardPool == null) _cardPool = CardDatabase.Load();
            return _cardPool;
        }

        /// <summary>本局的候选择将 —— **按 seed 确定性抽**（原版 `GetRandomWarlords()` 的**方法体缺失**
        /// （泛型基类里，S3 §七·2）⇒ 形状我们照 `RollRound` 那一条：逐位一份 `System.Random`）。
        /// 🔴 候选池 = 本机卡池里**所有** `type == "hero"` 的卡（原版由 `specificArmies/specificWarlords/
        /// guaranteedArmies/blacklistedWarlords` 过滤，**那四个都在服务端**、我们取空 ⇒ 全池）。</summary>
        public static List<CardDef> RollWarlords(int seed, int count)
        {
            var pool = new List<CardDef>();
            var all = CardPool();
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].Type == "hero") pool.Add(all[i]);
            var outp = new List<CardDef>(count);
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int slotSeed = unchecked((seed * 83492791) ^ (i * 19349663) ^ 0x5F3A1);
                int idx = new System.Random(slotSeed).Next(pool.Count);
                outp.Add(pool[idx]);
                pool.RemoveAt(idx);        // 同一局不重复
            }
            return outp;
        }

        /// <summary>这一轮给出的包 —— **就是 `DraftPacks.RollRound` 的返回**（⛔ 本类不另写抽样；
        /// `count` 也直接取 `DraftPacks.PacksPerRoundOffered`，不写死 3）。</summary>
        List<DraftPacks.PackDto> RollOffers()
        {
            var excl = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _pickedKeys.Count; i++)
            {
                excl.Add(_pickedKeys[i]);
                var p = DraftPacks.Resolve(_pickedKeys[i]);
                if (p != null) excl.Add(p.packId);
            }
            return DraftPacks.RollRound(_seed, CurrentDraftRound, WarlordArmyId(),
                                        DraftPacks.PacksPerRoundOffered, excl);
        }

        /// <summary>选中战将的阵营名（空 = 还没选 ⇒ 回落到 `Ultramarines`，只为让 UI 有个确定的池子；
        /// 调用点都会先拦住「没选战将」那一档）。</summary>
        public string WarlordFaction
        {
            get
            {
                if (string.IsNullOrEmpty(_warlordId)) return DeckRuntime.NoArmyFaction;
                for (int i = 0; i < _warlords.Count; i++)
                    if (_warlords[i] != null && _warlords[i].Id == _warlordId) return _warlords[i].Faction;
                var def = CardDatabase.FindById(CardPool(), _warlordId);
                return def != null ? def.Faction : DeckRuntime.NoArmyFaction;
            }
        }

        /// <summary>选中战将的 `CardArmy` 值（`DraftPacks.RollRound` 的过滤键）。</summary>
        public int WarlordArmyId() { return ArmyIcons.Id(WarlordFaction); }

        /// <summary>`Stage Counter` 现在印的字（自检读口 —— 「文本 = {轮次+1}/{轮数}」那条断言读它）。</summary>
        public string StageCounterText { get { return _stageCounter != null ? _stageCounter.Text : null; } }
        /// <summary>`Packs Container` 底下**真的建出来**几个包卡（自检读口 —— 断「= `RollRound` 的返回条数」）。</summary>
        public int VisiblePackCount { get { return _packsContainer != null ? _packsContainer.childCount : 0; } }
        /// <summary>选将页真的建出来几张卡（自检读口）。</summary>
        public int VisibleWarlordCount { get { return _warlordNodes.Count; } }
        /// <summary>右栏 `Card Counter` 现在印的字。</summary>
        public string DeckCounterText { get { return _cardsCounter != null ? _cardsCounter.Text : null; } }
        /// <summary>页节点（自检读口，按 state 取）。</summary>
        public Transform PageNode(DraftModeState s) { return PageFor(s); }
        /// <summary>右栏面板节点（自检读口 —— 显隐按页变，见 `GoToState`）。</summary>
        public Transform DeckInfoPanelNode { get { return _deckPanelRoot; } }
        /// <summary>奖励面板节点（只有 `Finished` 显示）。</summary>
        public Transform RewardPanelNode { get { return _rewardPanelRoot; } }

        /// <summary>我们自拟的文案（**只在原版确实没有节点/词条时用**）—— 中英按当前语档挑，
        /// 每一条的调用点都写明「这一句是我们自拟的」。形状照 `Shell/CampaignTab.cs:1314` 那一条。</summary>
        static string OurText(string zh, string en)
        {
            return Loc.Current == AvailableLanguages.Chinese ? zh : en;
        }
    }
}
