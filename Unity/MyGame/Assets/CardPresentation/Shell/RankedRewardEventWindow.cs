// RankedRewardEventWindow.cs — **排位加分活动窗**（原版 `RankedRewardEventWindow : GameWindow`）
//
// ⚠️ **别与 `Shell/RankedEventWindow.cs` 混** —— 那一个对应原版 `RankedEventWindowV2`（排位**活动**窗，
//    左上角左列 + 段位块那一棵），**是另一扇**。本文件对应 `Ranked Boost Reward Event Window`（**17 节点**）。
//
// ============================ 出处（唯一正本）============================
// prefab：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/` 根 GO `Ranked Boost Reward Event Window`
//   · 窗口参数 MB = `MonoBehaviour_3493192490119290576` 实读；
//   · 逐节点几何 = `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Ranked Boost Reward Event Window" --depth 12 --relative --md`
//     ⚠️ **本扇的根是「拉伸」根**（锚 `(0,0)-(1,1)`、`sizeDelta (0,0)`）⇒ `--relative` 与绝对框**同一套数**
//     （根自己就是 `0,0→1920,1080`），两档无需换算。
//   · 子预制体 `armyImagePrefab` → GO `Reward Event Faction Bonus Container`（类 `SimpleArmyImage`，
//     **6 个节点**），逐节点现读（见下面 `CardChild` 那张表）。
// 行为 = 反编译 `d:/2/tools/decomp_full/RankedRewardEventWindow__{Awake,Open,CloseButtonClick,OnDestroy}.c`
//   ＋ `SimpleArmyImage__Initialize.c` ＋ 字段偏移表 `dump.cs`（TypeDefIndex 1599 / 1743）。
//
// ============================ 窗参（MB 逐字段实读）============================
// 🔴 **`type = 0`（Fullscreen）** · `windowsPlacement = 15`(Popup) · `closeOnESC = 1` · `updateNavPanel = 0` ·
// `extraScaleSmallScreen = 1.2`（**不是 1.0** —— 这一扇真的会被放大 1.2，逐扇实读）。
// 根上**只有一颗组件**（`RankedRewardEventWindow`）—— ⛔ 没有 `TransformScalerBySmallScreenUI`
// （「窗口根上带成品的 3 扇」不含它）。
// 字段 → 节点（pid 反查，8/8 逐个对上）：
//   `closeButton → Generic Close Button Orange` · `backgroundCloseButton → Menu Dark Background` ·
//   `titleText → Title` · `descriptionText → Description` · `pointsBonus → Bonus points text` ·
//   `armiesAnchor → Content` · `armyImagePrefab → Reward Event Faction Bonus Container`（子预制体）·
//   `timerDisplay → Timer`（那颗 `TimerDisplay` 组件）。
// ⚠️ `window` 这个中间节点**没有 `GameWindow` 字段指向它** —— 它是树上的容器（`m_Children` 实读）。
//
// ============================ 🔴 原版的显隐模型（`Open()` 逐句读出来的）============================
// `context = Data as RankedScoreBoostEvent`（**为 null 直接抛**）；`data = context +0x18`（`RankedScoreBoostData`）：
//   ① `data.AffectedArmies` 逐个 `Instantiate(armyImagePrefab, armiesAnchor)` → `SimpleArmyImage.Initialize(army)`
//      → 收进 `armyImages`（`OnDestroy` 时逐个销毁）。
//   ② `titleText.SetText(LiveOpsAssetUtility.GetLabel(data, 0))`；**取到的串为空 ⇒ `titleText.enabled = false`**。
//   ③ 同上，`descriptionText` ← `GetLabel(data, 1)`，空 ⇒ `enabled = false`。
//   ④ `pointsBonus.SetText(string.Format(GetLabel(data, 2), context.PointsBonus))`
//      —— 🔴 **是 `string.Format`**：那个词条带 `{0}` 占位（prefab 出厂原文 `'+20 Classic points'` 已经把那一位填好了）。
//   ⑤ `timerDisplay`：**`data.DurationHours == 0` ⇒ 整个 `Timer` 节点 `SetActive(false)`**；否则
//      `TimerDisplay.Initialize(data.EndTime)`。
// `Awake()`：`closeButton.onClick` / `backgroundCloseButton.onClick` 两条都挂 `CloseButtonClick`。
// ⇒ **这份「显隐模型」就是原版自己的「没数据」分支**（②③⑤ 三条都自带兜底）—— 我们照它写，
//    ⛔ 不另编内容（见 `Apply()`）。
//
// ============================ 🔴 我们这一侧的处置 ============================
// `get_context()` 读的是 `GameWindow.Data`；`RankedScoreBoostEvent` 是 **LiveOps 配置** ⇒ 远端 CCD。
// 本地**没有**那份数据、也**没有任何调用方**（全量反编译里只命中它自己那 12 个 `.c`）
// ⇒ 出厂 = **一条数据都没有**那一档：`Title` / `Description` **关着**（原版「串为空 ⇒ `enabled = false`」）、
//    `Bonus points text` **空串**、`Timer` **关着**（`DurationHours == 0`）、`Content` 下 0 张阵营卡。
//    ⚠️ **我们这一侧「关」用 `SetActive(false)`**，原版是 `TMP.enabled = false`（节点还 active）——
//    对一棵**没有子件的叶子文字**来说可观测结果相同（都不画），如实记（本仓没有 `enabled` 那一层）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `RankedRewardEventWindow`（`GameWindow` 子类）—— 排位**加分**活动窗
    /// （标题 / 说明 / 加成横幅 / 一列受影响的阵营卡 / 倒计时）。
    /// <para>🔴 **它不在任何一页的层带里**：自成一档 **3460–3479**（挑战弹窗 `DuelPopupWindow` 3400–3405
    /// 与战斗日志 `BattleLogPopup` 3450–3458 之上、排行榜 `LeaderboardWindow` 3500–3520 之下 —— 层带不许重叠）。</para>
    /// <para>⚠️ ⛔ **别与 `Shell/RankedEventWindow.cs`（= `RankedEventWindowV2`）混**：那是另一扇窗、另一棵树。</para></summary>
    public class RankedRewardEventWindow : GameWindow
    {
        // ============================================================ 队列档（本窗自成一档 · 3460–3479）
        //   逐层顺序 = **原版兄弟序**（uGUI 按兄弟序画 ⇒ 后面的压前面的；判据 = prefab 的 `m_Children`）：
        //   根 = [Menu Dark Background, window]；
        //   window = [Generic Window Red Background Big, Generic Close Button Orange, Title, Description,
        //             Timer, Bonus points, Scroll View]。
        public const int QShade = 3460,        // `Menu Dark Background`
                         QShadeHit = 3461,     // └ 它的命中区（= `backgroundCloseButton`，点窗外关窗）
                         QWinBg = 3462,        // `window/Generic Window Red Background Big`
                         QTitle = 3463,        // `Title`
                         QDesc = 3464,         // `Description`
                         QTimerIcon = 3465,    // `Timer/Icon`
                         QTimerText = 3466,    // `Timer/Timer Text`
                         QBonus = 3467,        // `Bonus points`（红横幅）
                         QBonusText = 3468,    // └ `Bonus points text`
                         QCard = 3469,         // `Content/<一张阵营卡>`
                         QCardBg = 3470,       // └ `Background`
                         QCardIcon = 3471,     // └ `Army Icon`
                         QCardTextBg = 3472,   // └ `TextBackground`
                         QCardName = 3473,     //    └ `Army Text`
                         QCardBadge = 3474,    // └ `Feature Badge`
                         QClose = 3475,        // `Generic Close Button Orange` 的圆底
                         QCloseBg = 3476,      // └ `Background`
                         QCloseIcon = 3477;    // └ `Icon`
        /// <summary>本窗**内容命中区**那一档（压暗层命中区**严格低于**它 —— `MenuDraw.ShadeHit` 现场核）。</summary>
        public const int QHit = 3478;

        // ============================================================ 几何（绝对 = 现读 · 画布 px）
        //   根是拉伸根（`(0,0)-(1,1)`、`sizeDelta (0,0)`）⇒ 整屏。
        static readonly PxRect ShadeR    = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        static readonly PxRect WinR      = new PxRect(395.72f, 188.35f, 1524.28f, 851.65f);      // `window`
        static readonly PxRect WinBgR    = new PxRect(395.70f, 178.35f, 1547.30f, 895.80f);
        static readonly PxRect TitleR    = new PxRect(432.90f, 216.35f, 1487.10f, 268.35f);
        static readonly PxRect DescR     = new PxRect(411.70f, 384.55f, 1508.30f, 436.55f);
        static readonly PxRect TimerR    = new PxRect(806.77f, 772.30f, 1113.23f, 851.65f);
        static readonly PxRect TimerIconR= new PxRect(936.10f, 784.49f, 983.90f, 839.46f);
        /// <summary>⚠️ **原版这一格的宽就是 0.00**（`sizeDelta.x = 0`、锚 `(0,1)`、pivot `(0,1)`、
        /// `ap (177.131, −27.174)`）—— **不是 dump 失真**：它 `m_enableAutoSizing = 0`、
        /// 字号写死 50.31，靠 `ContentSizeFitter` 那一路也没有 ⇒ 原版就是**从一个点往右溢出**。
        /// 我们照建 0 宽节点 + `AlignLeft`（= 文字左沿落在这一格）—— 与 `TrophyInfoPopup` 那条
        /// 「`AlignLeftOn` 是世界 x − 父的世界 x」同源。</summary>
        static readonly PxRect TimerTextR= new PxRect(983.90f, 799.47f, 983.90f, 824.47f);
        static readonly PxRect BonusR    = new PxRect(432.90f, 288.25f, 1487.10f, 366.74f);      // 图与字**同框**
        static readonly PxRect ScrollR   = new PxRect(401.53f, 470.39f, 1518.47f, 763.61f);      // `Scroll View` = `Viewport`
        static readonly PxRect CloseR    = new PxRect(1487.08f, 159.85f, 1561.47f, 235.45f);
        static readonly PxRect CloseBgR  = new PxRect(1495.24f, 167.83f, 1552.10f, 225.96f);

        /// <summary>`Viewport` 那颗 `RectMask2D` 的 **`m_Softness = (55, 0)`**（实读）——
        /// **非零**（左右两条边是软的），`m_Padding = (0,0,0,0)`。⚠️ 同族的 `RewardWindow` 也有软边
        /// （`(200,0)`）⇒ 我们这条滚动区**必须把软边一起喂进 `MenuDraw`**（谁设 `Clip` 谁负责软边）。
        /// 那张阵营卡横向滚，所以软的是**左右**两条边（`Softness.x` 管左右）。
        /// 🆕 **2026-10-13（A435 阶段 2 · 庚 · W-A435庚 / A768③）回原版复核（本次不是照抄上面那段注释）**：
        /// 直接读 prefab 的 MB 本体 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_4030138604520103632.json`
        /// = `m_Padding (0,0,0,0)` · `m_Softness (55,0)` · `m_Enabled 1` ·
        /// `m_Script.m_PathID = 536591447201701790` = `bundle_Waprforge_monoscripts` 的 `UnityEngine.UI.RectMask2D`；
        /// 那颗 MB 的 `m_GameObject`（PathID `-2590556875553874224`）按 `m_Father` 父链上行 =
        /// `Ranked Boost Reward Event Window/window/Scroll View/Viewport`
        /// （`q1_rm2d.txt:209-210` 同一行同值，本次从原始 JSON 独立复核了一遍）。
        /// 🔴 这个数现在**只此一份**：`Build()` 里 `ViewportClip.Hang(...)` 的 `softness` 就是它
        /// （⛔ 别再写一个字面量 `55`）。</summary>
        public static readonly Vector2 Softness = new Vector2(55f, 0f);
        /// <summary>`Content` 的 `HorizontalLayoutGroup`：`m_Padding(0,0,0,0)` · `m_ChildAlignment = 4 (MiddleCenter)` ·
        /// `spacing = 24.489999771118164`（实读，**不是整数**）· `ctrlW 0 / ctrlH 1 / expandW 1 / expandH 1`。</summary>
        public const float CardSpacing = 24.489999771118164f;
        /// <summary>一张阵营卡的宽（`SimpleArmyImage` 根的 `sizeDelta.x = 300`；`ctrlW = 0` ⇒ 宽不由布局写）。</summary>
        public const float CardW = 300f;

        // ============================================================ `SimpleArmyImage`（子预制体）逐节点字段
        //   出处 = `python 工具/menu_dump.py bundle_menus_assets_all "Reward Event Faction Bonus Container" --depth 6 --md`
        //   🔴 **子件全部用「锚点 + pivot + sizeDelta + anchoredPosition」描述**（不是死矩形）：
        //     那一族卡片自己会被用户数据撑成 300×293.22（`ctrlH = 1` + `expandH = 1` ⇒ 高 = 视口高，
        //     ⛔ **不是 prefab 里那个 300**）⇒ 死矩形会错 6.78px 且随视口高度漂。算式见 `CardChild`。
        /// <summary>一张卡里的一个子件：`(anchorMinX, anchorMinY, anchorMaxX, anchorMaxY, pivotX, pivotY,
        /// sizeDeltaX, sizeDeltaY, apX, apY)` —— 全部逐字照 prefab 的 `RectTransform` 实读。</summary>
        public struct CardNode
        {
            public string Name;
            public float AminX, AminY, AmaxX, AmaxY, PivX, PivY, SdX, SdY, ApX, ApY;
            public CardNode(string n, float ax1, float ay1, float ax2, float ay2,
                            float px, float py, float sdx, float sdy, float apx, float apy)
            { Name = n; AminX = ax1; AminY = ay1; AmaxX = ax2; AmaxY = ay2;
              PivX = px; PivY = py; SdX = sdx; SdY = sdy; ApX = apx; ApY = apy; }
        }
        /// <summary>6 个节点（含根）。逐条实读：`Background` 铺满；`Army Icon` 锚 `(0.13,0.13)-(0.87,0.8733)`、
        /// `sizeDelta 0`；`TextBackground` 锚 `(0.5,0)-(0.5,0)`、pivot `(0.5,0)`、`sizeDelta (295.132, 61.188)`；
        /// `Army Text` 是 `TextBackground` 的**子件**（锚中心、`sizeDelta (295.13, 59.594)`、`ap (0, 0.79697)`）；
        /// `Feature Badge` 锚 `(-0.0115,0.38)-(0.62,1.015)`、pivot `(0,1)`、`sizeDelta (0.3,-0.7)`、`ap (-0.3,-0.7)`。</summary>
        public static readonly CardNode[] Cards =
        {
            new CardNode("Reward Event Faction Bonus Container", 0f, 1f, 0f, 1f, 0.5f, 0.5f, 300f, 300f, 444.6f, -150f),
            new CardNode("Background",        0f,     0f,    1f,    1f,    0.5f, 0.5f, 0f,       0f,      0f,       0f),
            new CardNode("Army Icon",         0.13f,  0.13f, 0.87f, 0.8733f, 0.5f, 0.5f, 0f,    0f,      0.0000381f, 0f),
            new CardNode("TextBackground",    0.5f,   0f,    0.5f,  0f,    0.5f, 0f,   295.132f, 61.188f, -0.2422f, 0f),
            new CardNode("Army Text",         0.5f,   0.5f,  0.5f,  0.5f,  0.5f, 0.5f, 295.13f, 59.594f, 0f,       0.79697f),
            new CardNode("Feature Badge",     -0.0115f, 0.38f, 0.62f, 1.015f, 0f, 1f,  0.3f,   -0.7f,   -0.3f,    -0.7f),
        };

        // ============================================================ 图 / 色 / 字号（逐条 MB 实读）
        public const string ArtWinBg = "UI_Deck_Information_Back";       // 1100×701 · border 42,363,655,81 · Sliced
        /// <summary>`Bonus points` 的图 —— 源 = `bundle_atlasindividual_assets_0_mainmenu/Sprite/40k_UI_Banner BW.json`
        /// （图集切片名里是**空格**，落盘时按导入器的 `slug()` 换成 `_`）。
        /// <para>取图口 = <see cref="Tex"/> → `CardArt.MenuUi`（它按 `ui_menu/ → ui_deck/ → ui/` 三目录查）。
        /// 🔴 **2026-10-18（铁律 5 订正）**：原文写着「🔴 **工程里没有** …… 只 staged 在 `Art/原版/0_mainmenu/`、
        /// **没进 `Resources/`** ⇒ 节点照建、**这一格不画** + 出声」—— **这一整段已不成立**：
        /// 导入器 `工具/import_original_art.py` 的 `MENU_IMAGES` 里有
        /// `('40k_UI_Banner BW', 'atlasindividual_assets_0_mainmenu')`（那一行注释就标着「战役奖励窗 `Bonus points` 横幅（A702）」），
        /// `--only-menu` 跑完已落到 `Resources/Art/ui_menu/40k_UI_Banner_BW.png` ⇒ **今天画得出来**，
        /// 走不到「照建不画 + 进 `MissingArt`」那一支。</para></summary>
        public const string ArtBanner = "40k_UI_Banner_BW";
        public const string ArtClock = "WF_icon_clock";                  // 64×64 · Simple · PA
        public const string ArtClose = "UI_Button_Round_background";     // 237² · Simple · PA
        public const string ArtCloseBg = "40k_general_bt_yellow";        // 71² · Simple · PA
        public const string ArtCloseIcon = "40k_general_bt_yellow_close";
        /// <summary>🔴 **2026-10-18（A1053）**：关窗钮那两颗子件（`Background` / `Icon`，同矩形 `CloseBgR`）
        /// **自己的** `m_RaycastPadding`（原版实读 `(-20)⁴`；L,B,R,T · **负 = 外扩**）⇒ 命中区 =
        /// 子件矩形外扩 20 = **96.86 × 98.13**（⛔ 不是根矩形 `CloseR` 的 74.39×75.60）。
        /// 算式只走 `MenuDraw.PaddedRect`；口径 → `普查_全仓命中区与关闭键族.md` §〇-1。</summary>
        static readonly Vector4 ClosePad = new Vector4(-20f, -20f, -20f, -20f);
        /// <summary>一张阵营卡的底 —— 源 = `bundle_boosterpacks_assets_all/Sprite/40K_shop_offer_bg_Sororitas_0.json`
        /// （注意**大写 `K`**，落盘名原样保留）。
        /// <para>取图口 = <see cref="Tex"/>（调用点在本文件 `RebuildCards()` 里那颗 `bgTex`）→ `CardArt.MenuUi`。
        /// 🔴 **2026-10-18（铁律 5 订正）**：原文写着「🔴 **工程里没有** …… 本仓 `Resources/` 下**一张都没有**）
        /// ⇒ 节点照建、不画 + 出声」—— **这一整段已不成立**：导入器 `工具/import_original_art.py` 的
        /// `MENU_IMAGES` 里有 `('40K_shop_offer_bg_Sororitas_0', 'boosterpacks_assets_all')`，
        /// `--only-menu` 跑完已落到 `Resources/Art/ui_menu/40K_shop_offer_bg_Sororitas_0.png`
        /// ⇒ **今天画得出来**（⚠️ 落盘那一刻它还没 `.meta`，编辑器导入一次即生成 —— 那不是「本地没有这张图」）。</para></summary>
        public const string ArtCardBg = "40K_shop_offer_bg_Sororitas_0";
        public const string ArtCardBadge = "UI_Army_Selection_Featured"; // 172×172 · Simple
        public static readonly Color ShadeTint = new Color(0f, 0f, 0f, 0.772549f);        // 实读
        public static readonly Color BonusTint = new Color(0.82075f, 0.16065f, 0.12776f, 1f); // `Bonus points` 实读
        public static readonly Color CardTextBgTint = new Color(0f, 0f, 0f, 0.353f);      // `TextBackground` 实读

        public const float TitleFont = 54.85f, TitleBase = 36f, TitleMin = 18f, TitleMax = 72f;
        public const float DescFont = 54.85f, DescBase = 36f, DescMin = 18f, DescMax = 72f;
        public const float BonusFont = 72f, BonusBase = 36f, BonusMin = 18f, BonusMax = 72f;
        /// <summary>`Timer Text`：`m_fontSize 50.31 · base 50.31`、**没有自适应**（实读）。</summary>
        public const float TimerFont = 50.31f;
        public const float CardNameFont = 38f, CardNameBase = 36f, CardNameMin = 18f, CardNameMax = 38f;

        // ============================================================ 出厂文本（prefab 原文，逐字）
        public const string TxtTitle = "FEATURE FACTIONS";
        public const string TxtDesc = "for every Ranked victory gained with a featured faction.";
        public const string TxtBonus = "+20 Classic points";
        public const string TxtTimer = "23h 34m";
        public const string TxtCardName = "BLACK LEGION";                // `Army Text` 的出厂样例串
        /// <summary>`Timer` 那颗 `TimerDisplay` 的实读参数（`useExtraText = 0` / `useLastMinutesText = 1`）。
        /// ⚠️ `lastMinutesText` 的词条我们读不到（`EverguildLocalizedText` 的参数在远端词条表里）。</summary>
        public const bool TimerUseLastMinutes = true, TimerUseExtra = false;

        // ============================================================ 数据（原版 `RankedScoreBoostEvent` / `…Data`）
        /// <summary>一份加分活动的公开面（原版那两层的字段：`AffectedArmies` / `GetLabel(…,0..2)` /
        /// `PointsBonus` / `DurationHours` / `EndTime`）。**本地一条都没有**（LiveOps 配置在远端 CCD）。
        /// <para>`Label0/1/2` 对应原版 `LiveOpsAssetUtility.GetLabel(data, 0/1/2)` —— 那三个是 I2 词条，
        /// 我们这边收成调用方给的**现成文本**（`null` / 空串 = **原版自己那一支**：关掉那一栏）。</para></summary>
        public struct BoostView
        {
            /// <summary>`RankedScoreBoostData.AffectedArmies`（`CardArmy` 枚举值列表）。</summary>
            public int[] Armies;
            /// <summary>`GetLabel(data, 0)` → `Title`。空 ⇒ 关（原版 `enabled = false`）。</summary>
            public string Label0;
            /// <summary>`GetLabel(data, 1)` → `Description`。空 ⇒ 关。</summary>
            public string Label1;
            /// <summary>`GetLabel(data, 2)` → 给 `string.Format` 用的带 `{0}` 的模板（`Bonus points text`）。</summary>
            public string Label2;
            /// <summary>`context.PointsBonus`（喂进上面那个 `{0}`）。</summary>
            public int PointsBonus;
            /// <summary>`RankedScoreBoostData.DurationHours`：**0 ⇒ 整个 `Timer` 关掉**（原版明写）。</summary>
            public int DurationHours;
            /// <summary>`data.EndTime`（`DateTime` —— 我们收 `Time.realtimeSinceStartup` 那一族的**秒**）。</summary>
            public float EndTime;
            /// <summary>一张卡上的阵营名（原版由 `SimpleArmyImage.Initialize(army)` 灌；
            /// **本地有 13 个阵营名**，调用方可传）。长度不足时按 `TxtCardName` 兜底。</summary>
            public string[] ArmyNames;
        }

        /// <summary>最近一次开出来的那一扇（自检用）。</summary>
        public static RankedRewardEventWindow LastOpened { get; private set; }

        BoostView _view;
        /// <summary>真的喂过数据吗（`false` = 出厂那一档：三栏关着 + `Timer` 关着 + 0 张卡）。</summary>
        public bool HasData { get; private set; }

        Label _title, _desc, _bonus, _timerText;
        Transform _window, _timer, _content;
        readonly System.Collections.Generic.List<Transform> _cards = new System.Collections.Generic.List<Transform>();
        MenuScroll _scroll;
        bool _warnedCardIcon;

        // ============================================================ 开

        /// <summary>建一扇。`view == null` ⇒ **出厂那一档**（见文件头）。</summary>
        public static RankedRewardEventWindow Create(WindowsManager mgr, BoostView? view = null)
        {
            var go = new GameObject("Ranked Boost Reward Event Window");   // 节点名照原版
            var win = go.AddComponent<RankedRewardEventWindow>();
            win.type = WindowType.Fullscreen;             // 🔴 实读 `type = 0`（**Fullscreen**，不是 Popup）
            win.placement = WindowsPlacement.Popup;       // 实读 `windowsPlacement = 15`
            win.closeOnEsc = true;                        // 实读 `closeOnESC = 1`
            win.extraScaleSmallScreen = 1.2f;             // 实读 1.2（**不是 1.0** —— 这一扇真会被放大）
            win.Manager = mgr;
            if (view.HasValue) { win._view = view.Value; win.HasData = true; }
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[RankedBoost] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();
            Debug.Log("[RankedBoost] 开了 `Ranked Boost Reward Event Window`（原版 `RankedRewardEventWindow`）。"
                    + "⚠️ **本地没有任何活动数据**（`RankedScoreBoostEvent` 是 LiveOps 配置、在远端 CCD），"
                    + "全量反编译里也没有第二个引用它的地方 ⇒ **入口由调用方自己定**（⛔ 我们不编一个）。"
                    + "出厂那一档走的**就是原版自己的「没数据」分支**：`Title`/`Description` 关着"
                    + "（原版「取到的串为空 ⇒ `enabled = false`」）、`Bonus points text` 空串、"
                    + "`Timer` 关着（`DurationHours == 0`）、`Content` 下 0 张阵营卡。");
        }

        /// <summary>喂一份数据（**自检用**，也是数据路唯一的入口）。</summary>
        public void SetBoost(BoostView v) { _view = v; HasData = true; Apply(); }

        // ============================================================ 几何助手

        /// <summary>uGUI 的 `RectTransform` 矩形算式（**唯一一份**）。
        /// <para>🔴 **推导（不是抄的，逐位可验）**：`RectTransform` 里三条独立关系——
        ///   ① `size = (anchorMax − anchorMin) × parentSize + sizeDelta`；
        ///   ② 锚点矩形的左下角 = `anchorMin × parentSize`；
        ///   ③ **pivot 点 = 锚点矩形左下角 + 锚点矩形尺寸 × pivot + anchoredPosition**（y 向上）。</para>
        /// <para>**逐位验过三处**（三份 prefab 的原读值）：
        ///   · `Reward Event Faction Bonus Container/TextBackground`（点锚 `(0.5,0)`、pivot `(0.5,0)`、
        ///     `sd (295.132,61.188)`、`ap (−0.2422,0)`）⇒ `2.19,238.81→297.32,300.00` ✓；
        ///   · 同树的 `Army Icon`（拉伸锚、`sd 0`、`ap≈0`）⇒ `39,38→261,261` ✓；
        ///   · 同树的 `Feature Badge`（拉伸锚 `(−0.0115,0.38)-(0.62,1.015)`、pivot `(0,1)`、
        ///     `sd (0.3,−0.7)`、`ap (−0.3,−0.7)`）⇒ `−3.75,−3.80→186.00,186.00` ✓
        ///     （⚠️ **这一格是 ③ 里那个 `锚点矩形尺寸 × pivot` 项的判别式** —— 少了它 y 会差 **0.7px**）。</para>
        /// <para>`out` 参数一律是**画布 px、y 向下**（与 `PxRect` 同系）。`parent` = 父框。</para></summary>
        public static PxRect CardChild(PxRect parent, CardNode n)
        {
            float ax1 = parent.x1 + n.AminX * parent.W;
            float ax2 = parent.x1 + n.AmaxX * parent.W;
            // 锚点矩形左下角（y 向上）换算到「y 向下」：`parent.y2 − y_up`
            float ayLowUp = n.AminY * parent.H;                     // 距父底
            float anchorH = (n.AmaxY - n.AminY) * parent.H;
            float w = (ax2 - ax1) + n.SdX;
            float h = anchorH + n.SdY;
            // pivot 点（y 向上，距父底）
            float pvx = n.AminX * parent.W + (n.AmaxX - n.AminX) * parent.W * n.PivX + n.ApX;
            float pvyUp = ayLowUp + anchorH * n.PivY + n.ApY;
            float pvxAbs = parent.x1 + pvx;
            float pvyDn = parent.y2 - pvyUp;                        // 同一点、y 向下
            // pivot 在 rect 内部的位置（y 向下：pivot.y = 1（顶）⇒ 点落在 rect 顶沿）
            float x1 = pvxAbs - n.PivX * w;
            float y1 = pvyDn - (1f - n.PivY) * h;
            return new PxRect(x1, y1, x1 + w, y1 + h);
        }

        /// <summary>`Content` 的矩形（`HorizontalLayoutGroup` + `ContentSizeFitter(h:PreferredSize)` 跑完那一档）。
        /// 锚 `(0,0)-(1,1)`、pivot `(0.5,0.5)`、`sizeDelta (−1116.95, 0)` ＋ CSF 把宽写成首选宽；
        /// pivot 点 = `视口左 + 视口宽×0.5`（= **960**，与 dump 的 `Content 960.00` 逐位同）
        /// ⇒ 内容**水平居中**在视口里。n = 0 ⇒ 宽 0。高度 = 视口高。</summary>
        public static PxRect ContentRect(int n)
        {
            float w = n <= 0 ? 0f : CardW * n + CardSpacing * (n - 1);
            float cx = (ScrollR.x1 + ScrollR.x2) * 0.5f;
            return new PxRect(cx - w * 0.5f, ScrollR.y1, cx + w * 0.5f, ScrollR.y2);
        }

        /// <summary>第 `i` 张卡的矩形（内容坐标 = 偏移 0 那一档；布局从 `Content` 左沿起排、间距 24.49）。
        /// 卡高 = 视口高（`ctrlH = 1` + `childForceExpandHeight = 1` 且卡上没有 `LayoutElement`
        /// ⇒ 首选 0、flexible 被顶成 1 ⇒ 撑满容器）—— ⛔ **不是 prefab 里那个 300**。</summary>
        public static PxRect CardRect(int i)
        {
            var cr = ContentRect(i + 1);
            float x1 = cr.x1 + i * (CardW + CardSpacing);
            return new PxRect(x1, ScrollR.y1, x1 + CardW, ScrollR.y2);
        }

        // ============================================================ 建

        public void Build()
        {
            var root = transform;
            MenuDraw.ClearChildren(root);
            MissingArt.Clear();
            _cards.Clear();
            _title = _desc = _bonus = _timerText = null;

            // ---- 1) `Menu Dark Background`（**这一扇是 `a = 0.772549`**，同族统一那个值）----
            MenuDraw.Rect(root, CardArt.Solid(), ShadeR, "Menu Dark Background", QShade, ShadeTint);
            MenuDraw.ShadeHit(root, ShadeR, QShade, QHit, () => Close(), "BackgroundHit");

            // ---- 2) `window` + 3) 面板底 ----
            _window = MenuDraw.Node(root, "window", WinR);
            var winBgTex = Tex(ArtWinBg, "窗体底");
            if (winBgTex != null)
                MenuDraw.Nine(_window, winBgTex, WinBgR, new Vector4(42f, 363f, 655f, 81f), 1100f, 701f,
                              QWinBg, null, true, "Generic Window Red Background Big");
            else MenuDraw.Node(_window, "Generic Window Red Background Big", WinBgR);

            // ---- 4) `Generic Close Button Orange`（`Awake()` 里两颗钮都挂 `CloseButtonClick`）----
            // 🔴🔴 **2026-10-18（A1149 第一半）圆底盘【挂点】归真**（本件 = 第九会话 P7）：
            //   改前圆底盘画在一颗**自造子件 `Image`** 上、而 `Generic Close Button Orange` 是颗**裸节点**；
            //   原版那颗 `Image` **就长在根节点自己身上**（无独立子件名）。
            //   判据（逐字段直读）= `python -I d:/tmp/wf_b4probe/pa.py bundle_menus_assets_all
            //   "Ranked Boost Reward Event Window" 8`：根 `window/Generic Close Button Orange` 自己带 `Image`：
            //   `UI_Button_Round_background` · `m_Type=0`(Simple) · **`m_PreserveAspect=1`** ·
            //   `m_PixelsPerUnitMultiplier=1.0` · `m_RaycastTarget=0` · 矩形 **74.39×75.61**；
            //   贴图 `m_Rect` = **237×237 正方** ⇒ 实绘 **74.39×74.39**。
            //   ⇒ 照兄弟窗先例 `Shell/InboxWindow.cs:386-387` / `Shell/ChatPanel.cs:307-308` 的形状
            //   （`Rect` 画在根节点上、图取不到才退回 `Node`）。矩形与 `keepAspect` **改前就是对的**。
            var closeTex = Tex(ArtClose, "关窗钮底");
            var closeBg = closeTex != null
                ? MenuDraw.Rect(_window, closeTex, CloseR, "Generic Close Button Orange", QClose, null, true)
                : null;
            var close = closeBg != null ? closeBg.transform
                                        : MenuDraw.Node(_window, "Generic Close Button Orange", CloseR);
            var cbTex = Tex(ArtCloseBg, "关窗钮内底");
            var closeFaceQ = cbTex != null
                ? MenuDraw.Rect(close, cbTex, CloseBgR, "Background", QCloseBg, null, true) : null;
            var ciTex = Tex(ArtCloseIcon, "关窗钮叉");
            if (ciTex != null) MenuDraw.Rect(close, ciTex, CloseBgR, "Icon", QCloseIcon, null, true);
            // 🆕 **2026-10-18（A1058 · 第六会话批 2）**：换图那一层 = **子件 `Background`**（`closeFaceQ`，
            //   画的是 `ArtCloseBg` = `40k_general_bt_yellow`），⛔ **不是圆底盘 `closeBg`**。
            //   判据（原版亲读）=
            //   `python -I d:/tmp/wf_hit/rcunion.py bundle_menus_assets_all "Ranked Boost Reward Event Window" --depth 8`：
            //   根 `window/Generic Close Button Orange` 那颗 `EverguildButton` 的
            //   **`m_TargetGraphic` = pid-7553376917821432112**；解该 pid ⇒ **所属 GO 名 = `Background`**、
            //   贴图 pid `5693181797853584851` → `40k_general_bt_yellow`。
            // 🆕 **2026-10-18（A1053 · 第六会话批 2）**：**命中区**归真值 —— 原版圆底盘
            //   `m_RaycastTarget = 0`，吃射线的是同矩形两颗子件（56.86×58.13）按 `(-20)⁴` 外扩
            //   ⇒ **96.86 × 98.13**；改前传根矩形 `CloseR`（74.39×75.60）⇒ 每边小 11.2。
            //   判据 = `python -I d:/tmp/wf_hit/rcpad.py bundle_menus_assets_all
            //   "Ranked Boost Reward Event Window" --depth 8 --substr "Generic Close Button"`
            //   （实读 `96.86 x 98.13`）。
            var closeHit = MenuDraw.Hit(close, "Hit", MenuDraw.PaddedRect(CloseBgR, ClosePad),
                                        QHit, () => Close(), closeFaceQ, ArtCloseBg,
                                        "40k_general_bt_yellow_hover", "40k_general_bt_yellow_pressed");
            if (closeHit == null)
                Debug.LogWarning("[RankedBoost] 关窗钮的命中区没建出来 ⇒ **点它关不了窗**（还能点窗外或 ESC）。");

            // ---- 5) `Title` / 6) `Description`（都是 `EverguildTextMeshPro`，hAlign = Center）----
            _title = MenuDraw.TextBox(_window, TitleR, TxtTitle, Color.white, "Title",
                                      TitleFont, TitleMin, QTitle, TitleMax, TitleBase);
            _desc = MenuDraw.TextBox(_window, DescR, TxtDesc, Color.white, "Description",
                                     DescFont, DescMin, QDesc, DescMax, DescBase);

            // ---- 7) `Timer`（`TimerDisplay` + HLG）> `Icon` + `Timer Text` ----
            _timer = MenuDraw.Node(_window, "Timer", TimerR);
            var clockTex = Tex(ArtClock, "`Timer/Icon`");
            if (clockTex != null) MenuDraw.Rect(_timer, clockTex, TimerIconR, "Icon", QTimerIcon, null, true);
            else MenuDraw.Node(_timer, "Icon", TimerIconR);
            // ⚠️ 这一格的**框宽就是 0**（见 `TimerTextR` 那条）⇒ 用 `Text`（不设折行） + `AlignLeft`，
            //    否则 `SetWrapWidth(0)` 会把字压没。
            _timerText = MenuDraw.Text(_timer, TimerTextR, TxtTimer, Color.white, "Timer Text",
                                       TimerFont, QTimerText);
            if (_timerText != null) MenuDraw.AlignLeft(_timerText, TimerTextR);

            // ---- 8) `Bonus points`（红横幅）> `Bonus points text` ----
            var bonus = MenuDraw.Node(_window, "Bonus points", BonusR);
            var bannerTex = Tex(ArtBanner, "`Bonus points` 的红横幅");
            if (bannerTex != null) MenuDraw.Rect(bonus, bannerTex, BonusR, "Image", QBonus, BonusTint);
            _bonus = MenuDraw.TextBox(bonus, BonusR, TxtBonus, Color.white, "Bonus points text",
                                      BonusFont, BonusMin, QBonusText, BonusMax, BonusBase);

            // ---- 9) `Scroll View` → `Viewport` → `Content` ----
            //   ⚠️ `Scroll View` 上那颗 `NonDrawingGraphic`（**不是 `Image`**）与 `Viewport` 上**没有 Image**
            //   （只有 `RectMask2D`）⇒ 这两层原版**什么都不画** ⇒ 节点照建、不画。
            var sv = MenuDraw.Node(_window, "Scroll View", ScrollR);
            // 🔴 **2026-10-13（A435 阶段 2 · 庚 · W-A435庚 / A768③）**：这颗 `Viewport` 从今天起就是**本视口的裁切状态载体**
            //   （= 原版那个 `RectMask2D`）。**参数回原版逐字复核过（⛔ 不是照抄上面的注释）**：
            //    · `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_4030138604520103632.json`
            //      = `m_Padding (0,0,0,0)` · `m_Softness (55,0)` · `m_Enabled 1` ·
            //      `m_Script.m_PathID = 536591447201701790`（= `UnityEngine.UI.RectMask2D`）；
            //      它的 `m_GameObject`（PathID `-2590556875553874224`）按 `m_Father` 父链上行 =
            //      `Ranked Boost Reward Event Window/window/Scroll View/Viewport`（本件现走一遍）。
            //   ⇒ 与迁移前的差别 = 「按视口框裁 + 左右各 55px 的渐隐带」（原版本来就是裁的、本来就有这条软边）。
            //  ⚠️ 软边那个数**只此一处**：`Softness` 既是节点字段的来源、也是……（本窗**没有**逐件传软边的调用点，
            //      那张阵营卡上的层以前一个 `clip` 都不吃）—— 见 `Softness` 那条常量注释。
            var vpVc = ViewportClip.Hang(sv, "Viewport", ScrollR, Vector4.zero,
                                         new Vector2Int((int)Softness.x, (int)Softness.y));   // (55,0)
            var vp = vpVc.transform;
            // ⚠️ **A465（W-A435己）在本文件留下的那段「这处没接、只报不改」的说明已被本件取代**
            //   （铁律 5 就地订正，原文见 git）：调度台按 A754 那条同族先例裁定「**算 A435 站点 ⇒ 要做**」。
            _content = MenuDraw.Node(vp, "Content", ContentRect(0));
            // 横向滚动（原版 `m_Horizontal 1 / m_Vertical 0`）⇒ `LeftAligned`。
            _scroll = MenuScroll.LeftAligned(ScrollR, 0f);
            // 🔴 **A465（W-A435己）那一行，本件补上**：构建循环（`RebuildCards` 里
            //   `if (_scroll != null && !_scroll.Intersects(on)) continue;`）从今天起读**同一颗节点**的状态。
            //   ⚠️ `_content` / `_scroll` / 这颗 `Viewport` **都是每次 `Build()` 新建**的 ⇒ 与节点同频，
            //      赋值写在建它的那一行旁边即可（⛔ 别跨构建复用 `_scroll`——本窗没有那条路）。
            _scroll.ClipNode = vpVc;
            _scroll.Elastic = true;
            _scroll.Inertia = true;
            _scroll.Owner = gameObject;
            _scroll.OnChanged = RebuildCards;
            PointerLayer.RegisterScroll(_scroll);
            // ⚠️ 原版 `m_ScrollSensitivity = 100`（逐处实读）—— 我们走公共件那一份系数
            //    （`MenuScroll.NotchK = 0.4`）⇒ 手感**不是同一个数**，如实记（报告 §九②）。

            Apply();
        }

        /// <summary>把 `_view` 铺上去（= 原版 `Open()` 那五条）。**没数据 ⇒ 走原版自己的兜底分支**。</summary>
        public void Apply()
        {
            // ⚠️ **次序**：先把滚动区的极值按新的卡数改掉，再重建卡（否则 `SetOffset`/夹取用的是旧的极值）。
            if (_scroll != null)
            {
                int n0 = HasData && _view.Armies != null ? _view.Armies.Length : 0;
                var cr0 = ContentRect(n0);
                _scroll.ContentX1 = cr0.x1;
                _scroll.ContentX2 = cr0.x2;
            }
            RebuildCards();

            // ② `titleText`（原版：取到的串为空 ⇒ `enabled = false`）
            string t0 = HasData ? _view.Label0 : null;
            if (_title != null)
            {
                _title.SetText(string.IsNullOrEmpty(t0) ? (HasData ? "" : TxtTitle) : t0);
                _title.gameObject.SetActive(!string.IsNullOrEmpty(t0));
            }
            // ③ `descriptionText`
            string t1 = HasData ? _view.Label1 : null;
            if (_desc != null)
            {
                _desc.SetText(string.IsNullOrEmpty(t1) ? (HasData ? "" : TxtDesc) : t1);
                _desc.gameObject.SetActive(!string.IsNullOrEmpty(t1));
            }
            // ④ `pointsBonus`（`string.Format(词条, PointsBonus)`）
            if (_bonus != null)
            {
                if (HasData)
                    _bonus.SetText(FillBonus(_view.Label2, _view.PointsBonus));
                else
                    _bonus.SetText("");
            }
            // ⑤ `Timer`：`DurationHours == 0` ⇒ **整件关掉**
            if (_timer != null)
            {
                bool on = HasData && _view.DurationHours != 0;
                _timer.gameObject.SetActive(on);
                if (on && _timerText != null) _timerText.SetText(FormatRemaining(_view.EndTime));
                if (!on && HasData)
                    Debug.Log("[RankedBoost] `Timer` 关着 —— 原版那一支：`data.DurationHours == 0` ⇒ "
                            + "`timerDisplay.gameObject.SetActive(false)`。");
            }
        }

        /// <summary>`string.Format(模板, points)` 的等价物（模板里的 `{0}` 换成 `PointsBonus`）。
        /// 模板为空 ⇒ 空串（`string.Format("", x)` 就是 `""`，与 C# 行为逐字一致）。</summary>
        public static string FillBonus(string fmt, int points)
        {
            if (string.IsNullOrEmpty(fmt)) return "";
            return fmt.Replace("{0}", points.ToString());
        }

        /// <summary>剩余时间那串字（原版由 `TimerDisplay.Initialize(endTime)` 填，格式串在 `TimerDisplay` 里）。
        /// ⚠️ **这一段是我们写的**（本仓没有 `TimerDisplay`，也没有它的格式串判据）—— 形状照 prefab 出厂原文
        /// `23h 34m`（`h` + `m`，个位不进位）。如实标在报告 §九。
        /// `useLastMinutesText = 1` 那一支（最后几分钟换一句词条）我们**没有词条表** ⇒ 不实现。</summary>
        public static string FormatRemaining(float endTime)
        {
            float left = Mathf.Max(0f, endTime - Time.realtimeSinceStartup);
            int mins = Mathf.FloorToInt(left / 60f);
            return (mins / 60) + "h " + (mins % 60) + "m";
        }

        /// <summary>重建 `Content` 下那一列阵营卡（滚动偏移变了也重建 —— 同全壳那条规矩）。</summary>
        void RebuildCards()
        {
            if (_content == null) return;
            foreach (var c in _cards) if (c != null) MainMenuSubmenuWindow.DestroySafe(c.gameObject);
            _cards.Clear();
            int n = HasData && _view.Armies != null ? _view.Armies.Length : 0;
            if (n == 0) return;
            var cr = ContentRect(n);
            for (int i = 0; i < n; i++)
            {
                var r = CardRect(i);
                var on = _scroll != null ? _scroll.Shift(r) : r;
                if (_scroll != null && !_scroll.Intersects(on)) continue;   // 整块在视口外 ⇒ 不建（同全壳口径）
                string nm = (_view.ArmyNames != null && i < _view.ArmyNames.Length && !string.IsNullOrEmpty(_view.ArmyNames[i]))
                          ? _view.ArmyNames[i] : TxtCardName;
                var go = BuildCard(_content, on, nm);
                _cards.Add(go);
            }
        }

        /// <summary>一张 `SimpleArmyImage` 卡（6 个节点）。`card` = 卡的框（会被撑成 300×视口高）。
        /// <para>🔴 子件的框全部走 <see cref="CardChild"/>（**不是死矩形**）—— 卡高随视口变，
        /// 死矩形会错（见 `Cards` 那条注释）。</para>
        /// <para>⚠️ `Army Icon`（原版运行期 `ArmyIconsSO.GetArmyIcon` 灌）**只建节点、不画** + 出声。
        /// `Background` 那张底图（<see cref="ArtCardBg"/>）**本仓有**（见那条的 doc）⇒ 正常铺满、不再只建节点；
        /// ⛔ 本文原文写的「与 `Background`（**本仓没有那张图**）**都**只建节点、不画」是**过期的**
        /// （2026-10-18 订正，同 <see cref="ArtCardBg"/> 那条）。</para></summary>
        Transform BuildCard(Transform parent, PxRect card, string armyName)
        {
            var nRoot = Cards[0]; var nBg = Cards[1]; var nIcon = Cards[2];
            var nTb = Cards[3];   var nTx = Cards[4]; var nBadge = Cards[5];
            var go = MenuDraw.Node(parent, nRoot.Name, card);
            var bgR = CardChild(card, nBg);
            var bgTex = Tex(ArtCardBg, "阵营卡的底");
            // ⚠️ 原版那颗 `Image` 是 `m_Type = 0 (Simple)`（**不是九宫**）⇒ 走 `Rect` 拉伸铺满。
            if (bgTex != null) MenuDraw.Rect(go, bgTex, bgR, "Background", QCardBg);
            else MenuDraw.Node(go, "Background", bgR);
            MenuDraw.Node(go, "Army Icon", CardChild(card, nIcon));      // 无图（运行期灌）
            var tbR = CardChild(card, nTb);
            var tb = MenuDraw.Node(go, "TextBackground", tbR);
            // `TextBackground` 那颗 `Image` 原版 **`m_Sprite = <无图>` + `m_Color = (0,0,0,0.353)`**
            // ⇒ 就是一块半透明黑条（我们用纯色 quad 画它，同 `MenuDraw.Rect(root, CardArt.Solid(), …)` 那条口径）。
            MenuDraw.Rect(tb, CardArt.Solid(), tbR, "Image", QCardTextBg, CardTextBgTint);
            var txR = CardChild(tbR, nTx);
            // 🔴 **2026-10-16（A799 · 生产 3/9）：这一格【会新裁】= 目的，⛔ 别「修」。**
            //   父链：`tb` ← `go`（`BuildCard` 的 `Node(parent, …)`）← `_content`（`:402`）← `vp`（`:399`）
            //   ← `vpVc`（`:397` 那颗 `ViewportClip`；`Cards()` 在 `:503` 正是拿 `_content` 当 parent 调的）。
            //   ⇒ 本处 `clip` 恒 `null` ⇒ A781 起由那颗节点接管 ⇒ **压着视口边**的那张卡上「Army Text」
            //   第一次被夹到视口沿（「整张卡在外」那一档早在 `:500` `_scroll.Intersects` 挡掉、连节点不建）。
            //   ✅ 为什么可以：判据 = 原版 `RectMask2D` 对文字与图片一视同仁（同卡那张 `Background` 的 `Rect`
            //   早在裁）⇒ 更贴原版。⚠️ 返回值有守卫（下一句 `if (tx != null) MenuDraw.AlignLeft(...)`）。
            //   判据全文 → `资料/普查产出_1015/R2_A799全量表.md` §一① #3。
            var tx = MenuDraw.TextBox(tb, txR, armyName, Color.white, "Army Text",
                                      CardNameFont, CardNameMin, QCardName, CardNameMax, CardNameBase);
            if (tx != null) MenuDraw.AlignLeft(tx, txR);
            var bdR = CardChild(card, nBadge);
            var badgeTex = Tex(ArtCardBadge, "`Feature Badge`");
            if (badgeTex != null) MenuDraw.Rect(go, badgeTex, bdR, "Feature Badge", QCardBadge);
            else MenuDraw.Node(go, "Feature Badge", bdR);
            // `Army Icon` 的图原版由 `ArmyIconsSO.GetArmyIcon(army)` 在运行期灌（`SimpleArmyImage.Initialize.c`）
            // —— 本地那张表**没有**（`SingletonBehaviour` 那份在远端）⇒ 只建节点、不画。
            // 🔴 **2026-10-10 订正（铁律 5）**：本文原来印的是「`Army Icon` 与 `Background` 的图**都**本仓没有
            //   ⇒ **两张都只建节点、不画**」—— **那句话今天在骗人**：`Background` 那张底图（`ArtCardBg`）
            //   **本仓有**（见 `ArtCardBg` 那条 doc）⇒ 上面 `:565` 那颗 `if (bgTex != null)` **真会把它铺满**，
            //   只有取不到时才退化成空节点。⇒ 出声文案改成**按实际走的那一支**说（见下），⛔ 别再印那句旧话。
            //   ⚠️ 连带：这道 `if` 原来只看 `_warnedCardIcon`、**不看 `bgTex`** ⇒ 它会在「Background 明明画出来了」
            //   的情况下也报「没画」。现在把 `bgTex` 那一半并进文案里，两者一致。
            if (!_warnedCardIcon)
            {
                _warnedCardIcon = true;
                Debug.Log("[RankedBoost] 阵营卡的 `Army Icon`：原版由 "
                        + "`ArmyIconsSO.GetArmyIcon(army)` 运行期灌（本地没有那张表）"
                        + "⇒ **只建节点、不画**。"
                        + (bgTex != null
                            ? " `Background` 那张底图本仓有 ⇒ **已铺满**。"
                            : " ⚠️ 本次 `Background` 底图**取不到**（`ArtCardBg` 没命中）⇒ 它也只建了节点。"));
            }
            return go;
        }

        // ============================================================ 取图（取不到必须出声）
        readonly System.Collections.Generic.List<string> _missArt = new System.Collections.Generic.List<string>();
        /// <summary>本窗**取不到的图**（自检读口）。</summary>
        public System.Collections.Generic.List<string> MissingArt { get { return _missArt; } }

        Texture2D Tex(string art, string what)
        {
            if (art == null) return null;                  // 调用方明说「这一件没有图」（`m_Sprite = <无图>`）
            var t = CardArt.MenuUi(art);
            if (t == null && !_missArt.Contains(art))
            {
                _missArt.Add(art);
                Debug.LogWarning("[RankedBoost] 图取不到：`" + art + "`（" + what + "）⇒ **这一件没画**"
                               + "（`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。"
                               + "导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            }
            return t;
        }

        // ============================================================ 读口（自检专用）
        public Transform WindowNode { get { return _window; } }
        public Transform TimerNode { get { return _timer; } }
        public Transform ContentNode { get { return _content; } }
        public Label TitleLabel { get { return _title; } }
        public Label DescLabel { get { return _desc; } }
        public Label BonusLabel { get { return _bonus; } }
        public Label TimerTextLabel { get { return _timerText; } }
        public MenuScroll Scroll { get { return _scroll; } }
        public System.Collections.Generic.List<Transform> Cards_ { get { return _cards; } }

        /// <summary>自检用：把当前状态摊开。</summary>
        public string DebugDump()
        {
            return "hasData=" + HasData
                 + " cards=" + _cards.Count
                 + " titleActive=" + (_title != null && _title.gameObject.activeSelf)
                 + " descActive=" + (_desc != null && _desc.gameObject.activeSelf)
                 + " timerActive=" + (_timer != null && _timer.gameObject.activeSelf)
                 + " bonus=\"" + (_bonus != null ? _bonus.Text : "-") + "\""
                 + " missingArt=" + _missArt.Count
                 + (MissingArt.Count > 0 ? "（" + string.Join("、", MissingArt.ToArray()) + "）" : "");
        }

        // ============================================================ 对账表（**纯数据 · 不参与渲染**）
        /// <summary>一个节点的**冻结事实**（路径 / 框 / 出厂 `activeSelf`）。
        /// ⚠️ 本扇的根是**拉伸根**（`(0,0)-(1,1)`、`sizeDelta (0,0)`）⇒ **绝对框 == 相对框**，
        /// 所以这一张表直接对着 `python 工具/menu_dump.py bundle_menus_assets_all
        /// "Ranked Boost Reward Event Window" --depth 12 --relative --md` 的现读值抄。
        /// 无 `On = false` 的行 —— 这一扇**出厂 17 个节点全开**（显隐全靠 `Open()` 运行期写）。</summary>
        public struct ReconRow
        {
            public string Path;
            public PxRect R;
            public ReconRow(string p, PxRect r) { Path = p; R = r; }
        }

        /// <summary>逐节点对账表（**17 条 = 原版 17 个节点**）。</summary>
        public static readonly ReconRow[] Recon =
        {
            new ReconRow("Ranked Boost Reward Event Window",  new PxRect(0f,         0f,        1920f,      1080f)),
            new ReconRow("Menu Dark Background",              new PxRect(-1327.30f, -746.18f,  3247.30f,   1826.18f)),
            new ReconRow("window",                            new PxRect(395.72f,   188.35f,   1524.28f,   851.65f)),
            new ReconRow("window/Generic Window Red Background Big",
                                                              new PxRect(395.70f,   178.35f,   1547.30f,   895.80f)),
            new ReconRow("window/Generic Close Button Orange",new PxRect(1487.08f,  159.85f,   1561.47f,   235.45f)),
            new ReconRow("window/Generic Close Button Orange/Background",
                                                              new PxRect(1495.24f,  167.83f,   1552.10f,   225.96f)),
            new ReconRow("window/Generic Close Button Orange/Icon",
                                                              new PxRect(1495.24f,  167.83f,   1552.10f,   225.96f)),
            new ReconRow("window/Title",                      new PxRect(432.90f,   216.35f,   1487.10f,   268.35f)),
            new ReconRow("window/Description",                new PxRect(411.70f,   384.55f,   1508.30f,   436.55f)),
            new ReconRow("window/Timer",                      new PxRect(806.77f,   772.30f,   1113.23f,   851.65f)),
            new ReconRow("window/Timer/Icon",                 new PxRect(936.10f,   784.49f,   983.90f,    839.46f)),
            new ReconRow("window/Timer/Timer Text",           new PxRect(983.90f,   799.47f,   983.90f,    824.47f)),
            new ReconRow("window/Bonus points",               new PxRect(432.90f,   288.25f,   1487.10f,   366.74f)),
            new ReconRow("window/Bonus points/Bonus points text",
                                                              new PxRect(432.90f,   288.25f,   1487.10f,   366.74f)),
            new ReconRow("window/Scroll View",                new PxRect(401.53f,   470.39f,   1518.47f,   763.61f)),
            new ReconRow("window/Scroll View/Viewport",       new PxRect(401.53f,   470.39f,   1518.47f,   763.61f)),
            new ReconRow("window/Scroll View/Viewport/Content",
                                                              new PxRect(960.00f,   470.39f,   960.00f,    763.61f)),
        };
    }
}
