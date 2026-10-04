// PracticeModePopup.cs — 第 3 层第 6 件「战斗入口」：**`Practice Mode Menu`**（原版 **`PracticeModePopup : GameWindow`**）
//
// ============================ 出处（唯一正本） ============================
// `资料/阶段二_战斗入口_原版规格.md` **§一（窗口参数）+ §二 A（逐节点表）**。
// 窗口参数（实测）：`type=1 Popup` · `windowsPlacement=15 Popup` · `closeOnESC=1` · **`extraScaleSmallScreen=1.07`**。
//
// 🔴 **它是「点模式卡 → 进对应界面」那条路的第一个落地窗**（用户 2026-09-24 拍板：
//    「是直接点击这些卡片，然后就进去这些对应模式的界面的」）。
//    ⚠️ 原版这张「哪个模式 → 哪张卡 → 哪扇窗」的映射在 **liveop 服务端**（本地查不到、也不许自己编）——
//    本地入口是**我们定的**，逐条记在 `资料/阶段二_战斗入口_原版规格.md` §〇/§五。
//
// ============================ 开战链（§三）============================
// 原版：`Battle!` → `MatchMakerManager.StartMatch` →（无人应答）→ `StartBotBattle`
//       → `SearchOpponentManager.StartBattle` → `GetBattleArena(army)` → `LoadScene(场景名)`
//       （`SearchOpponentManager__StartBattle.c:57/61`，全二进制**唯一**的 `LoadScene` 点）。
// 我们：**练习模式本来就有 AI**（用户 2026-09-22 的裁决）⇒ `Battle!` = **选定卡组 + 切 `Battle.unity`**，
//       战斗场景自己会 `DeckLibrary.Load().Current`（`BattleDriver.PickSavedDeck`）。
//       ✅ **2026-09-25 更正**：arena 那张表**在本地、已实读**（`ArenaByArmy` 运行时表；
//       判据 → `资料/普查产出_0920/场景光照与后处理_原版规格.md` §六）。
//       ⚠️ **但还没接上** —— `Battle.unity` 建场时就把战场几何烘死了，换场要先定架构，见 `ArenaByArmy` 头注释。
//
// ============================ 🔴 练习对手链（2026-10-03 · A10 的尾巴）============================
// 原版 `Deck info Popup` 点 `Practice Deck` 走的是**另一条**链（**不是**开这扇窗）：
//   `DeckInfoPopup.SelectPracticeOpponentDeck` → 开 `DeckSelectionPopup`（挑**对手**卡组）→
//   回调 `DeckInfoPopup.StartPracticeMatch(对手那副)` → `ShowPopUp(等待窗)` →
//   `PlayerDataManager.CreatePlayerBattleData(自己那副)` → `GameStaticData.CheckHiddenCardsInDeck(自己那副)` →
//   `MatchMakerManager.StartMatch(OwnDeckTraining(0xc = 12), …, playerDeck: 自己那副, enemyDeck: 对手那副, …)`。
//   🔴 **语义判据**（唯一一处 = `MatchMakerManager.StartMatch(PlayModes, PlayerBattleData, CardDeck playerDeck,
//      CardDeck enemyDeck, …)` 的**形参名**，签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/Everguild/MatchMakerManager.cs:136`）：
//      **`Deck info Popup` 里那一副 = `playerDeck`（自己的）** ·
//      **选卡组窗回调回来的那一副 = `enemyDeck`（对手）**。
//      反证三条同时成立：`CreatePlayerBattleData(自己那副)` 用的是同一副 · `CheckHiddenCardsInDeck(自己那副)`
//      检查的是「**我能不能用这副牌打**」· 模式号 `12 = PlayModes.OwnDeckTraining`「用自己的卡组训练」。
//      ⚠️ 本工程 2026-10-03 之前**两处文档把它写反了**（`Shell/DeckInfoPopup.cs` 文件头 + 项目任务 §三第29条 A10
//      的提要，写的是「这一副是【对手】卡组」）—— 已在 `DeckInfoPopup.cs` 就地订正（铁律 5）。
//   ⚠️ **原版那一步还做两件我们没有的事**（如实标，不是「挑着不做」）：
//      ① 先取 LiveOps 的 `PracticeEvent` 并 `GameModes.CurrentPlayingEvent = 它` —— **事件在服务端**，
//         我们**没有服务器**（同 `ChatPanel` 那条老账）⇒ 不取；练习赛在我们这儿是纯本地一条链。
//      ② `PlayerDataManager.CreatePlayerBattleData(自己那副)` 造的是**对局名片**（玩家名 / 头像 / 头衔 /
//         卡背 / 站前喊话…，`DeckAndWarlordData` 那一堆字段）。我们只把**卡组**交给 `BattleDriver.Begin(myDeck:)`，
//         没有那份名片（我们的头像/头衔线本来就没接进对局）。⇒ 别以为 `CreatePlayerBattleData` 已经复刻。
// 原版那扇窗自己的 `Battle!` 是**另一条**：`PracticeModePopup.BattleButtonOnClick` →
//   `StartMatch(OfflinePractice(6), …, playerDeck: 选中的那副, **enemyDeck: 0 (null)**)`（`:225`）
//   ⇒ **练习窗那条链的对手是自动凑的**，这就是本文件 `OpponentDeck == null` 那一档。
//
// ✅ **2026-10-04 收口了**（原来这里记的是「缺口、读它的那一行还没写」）：原版的 `enemyDeck` 由
//    `MatchMakerManager.StartMatch` 直接传进对局；我们的开战链是「壳里切 `Battle.unity` →
//    `BattleDriver.Start → BeginFromDeckLibrary`」，中间**跨场景** ⇒ 对手卡组走一条**静态待读通道**
//    （同 `PrebuiltDecks` 那条先例）。通道 = `SetPendingOpponentDeck` / `TakePendingOpponentDeck`（见下），
//    **读它的那一行已由调度台补上** —— `Battle/BattleDriver.cs` 的 `BeginFromDeckLibrary()` 里
//    `Begin(seed: …, myDeck: saved, **foeDeck: PracticeModePopup.TakePendingOpponentDeck()**, deckNote: …, vars: …)`。
//    （自检里能验到的：通道里放的是「选中的那副」、且不是「我自己那副」、读一次就清 —— 见 `CollectionScene`。）
// 🔴 **仍与原版不同的两处（如实标，别再当"缺口"重复报）**：① 原版那一步还取 LiveOps 的 `PracticeEvent` 并写
//    `GameModes.CurrentPlayingEvent`（**事件在服务端，我们没有服务器**）；② 原版还调
//    `PlayerDataManager.CreatePlayerBattleData` 造**对局名片**（玩家名/头像/头衔/卡背/喊话）—— 我们只把**卡组**交给 `Begin`。

// ---- 没建的（出声，不静默）----
//   · `GameModeText`（"Game mode: Multiplayer"）—— 原版**出厂 act=0** ⇒ 照纪律不建
//   · `Character Image`（905×905）—— 原版 **`m_Enabled=0`** ⇒ 不建
//   ✅ **2026-10-03（§三第29条 A14）**：~~`Deck Information cost drawer` + `Deck Information Cost/balance text`
//     —— **不建**（那套 `DeckEnergyCostDrawer` 费用曲线柱还没接）~~ ⇒ **两件都建了**：标题那条按原版
//     `Card / Energy cost`（fs34），曲线 9 行走 **`Core/CostCurveDrawer`**（与 `Deck info Popup` 那扇**共用一份画法**；
//     这一扇的 `localScale` 是 **1.2**，那扇是 1.8）。
//   · `Lore Text` —— **仍不建**（原版喂的是 `DemoDeckInfoSO.Lore` = **卡组**简介）。🔴 **2026-10-03 查清了卡在哪**：
//     `DemoDeckInfoSO`（`bundle_menus_assets_all/MonoBehaviour/Demo DeckInfo *.json`，**82 份**）里存的是
//     **`loreLocalizationKey`**（例 `Demo/AeldariDeck1`）—— **正文在远端 CCD 的词条表里**（原版客户端根本没有那份表）
//     ⇒ **判据（文本）本地就是空的**，按铁律 11 的第 ① 种挂着，**不编文案**。
//     上一版拿督军的效果文字顶上去 ⇒ 内容不对，而且它的框套着 `Change Deck`、实拍里两行字叠在一起。
//     见 `BuildGeneralContainer` 里那段注释）
//   · `Searching Oponent Popup` —— ✅ **2026-09-24 建了**（`Shell/SearchingMatchPopup.cs`，四窗共用）。
//     🔴 原来这行写「原版出厂 act=0 ⇒ 不建（由开战流程运行时打开；我们点 `Battle!` 直接切场景）」——
//        **下半句是错的**：原版点 `Battle!` 也要先过 `MatchMakerManager.StartMatch`，这扇窗就是那 12 秒里显示的。
//   · 🔴 **2026-10-05 就地更正（铁律 5）**：这里原来写「卡组行的**行高**原版没给 ⇒ 我们自己挑的（38）」——
//     **那句是错的，原版给了**：`Decks Scroll view/Viewport/Content` 自己就是一个 `GridLayoutGroup`
//     （`m_CellSize = 180×180` · `m_Spacing = (0,−21)` · `m_Padding = (4,0,0,0)` · `m_ChildAlignment = 1` ·
//     `m_Constraint = 0 Flexible`；判据文件 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_5600458694838669116.json`，
//     宿主 GO = `Content`，pid **1570281722326503228** = 原版 `PracticeModePopup.deckButtonContainer` 那一件）。
//     **错因**：当初只查到 `ScrollRect` 那一层（§二 A 那张表只到视口），**没往下多读一层 `Content`**
//     ⇒ 把「我们挑的 38px 单列」记成了「原版没给」。⇒ 两处都已照原版改（见常量段 `DeckCellW/DeckGapY/…`）。
//     ⚠️ **2026-10-07（A77-㉒②③）就地更正（铁律 5）**：这一行原来写
//     「现在**唯一还属于「我们挑的」**的只剩「**格子里那几件怎么摆**」（名字条的位置/字号/命中区）……
//      **已查实、本轮没做** ⇒ 记成待办」——
//     **那一件本轮做完了**：`Deck Selector Menu Item` 的五层内景（`Highlight` / `Button border` /
//     `Main image` / `IsPlayerDeck` / `Text background` + 其下 `Deck Name`）全部照原版锚点五元组建出，
//     各层的格内矩形、sprite、贴图模式、字号/自适应窗口/折行的出处逐条写在常量段 `Item*` 那一组上
//     （判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Deck Selector Menu Item" --depth 6 --md`
//      + `MonoBehaviour_914357594831109544.json` 的 `yourDeckColor`）。名字条**照原版摆格底**，
//     我们原来那条「格的顶 38px」已删（`DeckNameH` 不存在了）。
//     ⚠️ 仍然属于「我们挑的」的只剩两样：① `Row Bg` 那层 `UI_Button_Mulligan` + 黄染（见
//     `RebuildDeckRows` 里那段）；② 命中区（原版吃射线的是那颗 `Image`+`EverguildButton`，我们是一块透明 `Hit`）。
//
// ============================ 🔴 2026-09-24 结构订正（第 53 条）============================
// **原来那份挂错了父**：把 `Deck Name`/`Warlord Name`/`Lore Text`/`Change Deck` 塞进
// `Deck info/Background Info`（那一层实读是**【空容器】+ act=0**），于是它们要么不显示、
// 要么和卡列表挤在同一块矩形上（实拍：字叠成一团）。**从根节点实读**（`工具/menu_rect.py` +
// `工具/menu_dump.py` + `工具/_probe_deckinfo.py` 三条互证）后的真结构：
//
//   `Deck info`（**自己没图**）
//     ├ `Generic Window Red Background Big`  761.63,187.00→1831.93,869.00  `UI_Deck_Information_Back`
//     ├ `Character Image`（m_Enabled=0，不建）
//     ├ `Background Info`（**【空容器】act=0**，照原版留空）
//     ├ `Deck Name`（act=1）· `Warlord Name`（act=1）· `Army Image`（act=1）  ← 直挂，本来就该看得见
//     ├ `General container`（act=1）→ lore · cardback · `Show Deck Content Button` · `Change Deck`
//     └ `Deck List Drawer`（act=1）→ `Content`（起 y **318.42**）· `Show Deck General Info button`
//
// **两个抽屉互斥**（`DeckGeneralInfoDemo.Toggle(bool)`），**出厂 = 总览**（`SetContent` 末尾
// `generalInfoContainer.SetActive(true)` + `cardsInDeckPanel.SetActive(false)`）。
// 🔴 卡列表那个抽屉在**本地这份包里打不开**：`Toggle(false)` 在全量反编译里**找不到调用者**
//    （`grep -l DeckGeneralInfoDemo__Toggle *.c` 只命中它自己）⇒ **如实记着，别自己给它编一个入口**。
// `Show Deck Content Button` 原版**不是**抽屉开关 —— 它走
//    `CardInDeckInfoButtonOnClick → WindowsManager.OpenWindow(new DeckInfoContext(deck, 2, …))`，
//    **开的是「卡组详情」窗**（= 我们已建的 `DeckInfoPopup`）。
// ⚠️ 正本 `阶段二_战斗入口_原版规格.md` §二 A 那张表的缩进与「红底写在 `Deck info` 那行」两处**与实读不符**，
//    已就地更正（`项目任务.md` §三 第 53 条）。
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;
using CardPresentation.Net;      // 🆕 联机（N3：`Battle!` 走 P2P 还是走 12 秒 bot 链）

namespace CardPresentation
{
    /// <summary>`PracticeModePopup : GameWindow` —— 练习模式的模式窗。</summary>
    public class PracticeModePopup : GameWindow
    {
        // 层：高于所有「页」（最高 `CampaignTab` 3064）与卡组线那几个弹窗（3120…），**低于** `PromptPopup`（3140）
        // 🔴 **2026-10-07（A77-㉒）**：`QPrText` / `QPrHit` 原来是 **3102 / 3103**，现在挪到 **3107 / 3108** ——
        //    卡组格（原版 `Deck Selector Menu Item`）的内景要**五层各一个队列**，插在 `QPrRow` 与文字之间。
        //    ⚠️ 那三个号（3102…3106）**全工程没被别处占**（现读：`QPr` 3100 / 本窗 3101…3108 / 下一个是 3115
        //    `QArmyFeat`、3120 战役页、3130 `SearchingMatchPopup`）⇒ 挪完不与任何东西撞档。
        //    ⚠️ 外部只按**常量名**引用这两个号（`Editor/MainMenuScene.cs` / `Editor/CollectionScene.cs`
        //    的命中区助手）⇒ 值变了它们跟着变，不用改调用点（只有注释里的字面量要跟着订正）。
        public const int QPr = 3100, QPrRow = 3101, QPrText = 3107, QPrHit = 3108;
        /// <summary>卡组格**内景**那五层 —— 原版 `Deck Selector Menu Item` 的兄弟序（**从下到上**）：
        /// `Highlight` → `Button border` → `Main image` → `IsPlayerDeck` → `Text background`(其下 `Deck Name`)。
        /// 🔴 **一层一个队列**：同队列「谁盖谁」不可控（`ImageQuad` 全是透明队列、按到相机的 3D 距离排 ——
        /// `CLAUDE.md` §三那两条）。先例 = `SearchingMatchPopup` 头部那条（第一版把 6 层塞进一个 3130，
        /// 结果压暗层把面板整块盖住）。`Main image` 在 `Highlight`/`Button border` **之上**是原版的兄弟序，
        /// ⛔ 别按「谁是底图」的直觉重排（三张图四角都是透明的，透明处露出下面那层，见各 `m_Sprite` 像素实测）。</summary>
        public const int QPrItemHL = 3102, QPrItemBd = 3103, QPrItemArt = 3104, QPrItemSeal = 3105, QPrItemTxBg = 3106;

        // ---- 几何（§二 A 逐条）----
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.773f);
        public const float BackL = 215.76f, BackT = 892.86f, BackR = 280f, BackB = 956.10f;
        public const float BackIcL = 224.19f, BackIcT = 901.15f, BackIcR = 271.58f, BackIcB = 947.81f;
        public const float BackTxL = 289.07f, BackTxR = 594.40f;
        public const float DsL = 77.10f, DsT = 165.33f, DsR = 610.90f, DsB = 858.19f;
        public const float DbtnL = 242.94f, DbtnT = 208.83f, DbtnR = 657.98f, DbtnB = 861.25f;
        public const float TipL = 283.76f, TipT = 170.83f, TipR = 617.16f, TipB = 208.83f;
        public const float ArmyTxL = 279f, ArmyTxT = 223.01f, ArmyTxR = 621.92f, ArmyTxB = 272.01f;
        public const float DecksL = 261.28f, DecksT = 262.64f, DecksR = 634.88f, DecksB = 803.43f;
        /// <summary>`Decks Scroll view/Viewport`（挂 `RectMask2D` 的那一件）—— 原版**与父件 `Decks Scroll view` 同矩形**
        /// （261.28,262.64→634.88,803.43；锚 `(0,0)→(1,1)` · `posDelta (0,~0)` · `sizeDelta (0,~0)`）。
        /// 🔴 **这一处与同窗的 `Army Selector` 正好相反**（那里的 `Viewport` 比父件高 66.22px）——
        /// 两个 `ScrollRect` 逐处实读，⛔ **别互推**（铁律 5·c）。
        /// ⇒ 软边切线的位置**没变**（上沿 262.64 + 23 = **285.64**），本轮只是把这个事实写清楚。</summary>
        public const float DecksVpL = 261.28f, DecksVpT = 262.64f, DecksVpR = 634.88f, DecksVpB = 803.43f;
        /// <summary>`Decks Scroll view/Viewport/Content` 的矩形 = **262.06..634.10**（宽 **372.04**）。
        /// 实读：锚 `(0,1)→(1,1)` · `pivot (0.5,1)` · `sizeDelta.x = −1.56`（= 373.60 − 1.56）· 高 0（出厂没有子件）。
        /// ⚠️ 这是**格容器自己的框**（`GridLayoutGroup` 的可用宽就是它）—— 不是视口宽。</summary>
        public const float DecksCL = 262.06f, DecksCR = 634.10f;
        /// <summary>卡组格：**180×180**（原版 `m_CellSize`）。
        /// 🔴 **2026-10-05 就地更正（铁律 5）**：这一对原来不存在 —— 代码里写的是
        /// `DeckRowH = 38 / DeckRowGap = 6`，注释还标着「**行高是我们挑的** —— 原版没给格尺寸」。
        /// **那句是错的**：判据（`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_5600458694838669116.json`，
        /// 宿主 = `Content`，pid 1570281722326503228）一直就在。**错因** = 当初只查到 `ScrollRect` 那一层。
        /// ⇒ `DeckRowH/DeckRowGap` 已删。</summary>
        public const float DeckCellW = 180f, DeckCellH = 180f;
        /// <summary>格间 `m_Spacing` = **(0, −21)** —— **纵向是负的**（相邻两行**重叠 21px**）⇒ 行距 = 180 − 21 = **159**。</summary>
        public const float DeckGapX = 0f, DeckGapY = -21f;
        /// <summary>`m_Padding` = **(4,0,0,0)**（左 4、右 0、上 0、下 0 —— 原版序列化字段逐条抄）。</summary>
        public const float DeckPadL = 4f, DeckPadR = 0f;
        /// <summary>`m_ChildAlignment` = **1**（`TextAnchor.UpperCenter`）⇒ 横向**居中**、纵向贴顶。
        /// ⚠️ 原版 `m_ConstraintCount` 序列化是 **2**，但 `m_Constraint = 0 (Flexible)` ⇒ **那个数不参与计算**，
        /// 列数按宽度现算（见 `DeckCols`）—— 别拿 `ConstraintCount` 当判据。</summary>
        public const int DeckAlign = 1;
        /// <summary>列数 —— 照 uGUI `GridLayoutGroup.SetCellsAlongAxis(1)` 的 `Flexible` 那一支现算：
        /// `max(1, floor((可用宽 − pad.horizontal + spacing.x + 0.001) ÷ (格宽 + spacing.x)))`
        /// = `floor((372.04 − 4 + 0 + 0.001) ÷ 180)` = **2**（判据 = `GridLayoutGroup.cs:261-263`）。
        /// ⚠️ 用 `DeckCellW` 等常量算**不是**「从被测实现读期望值」—— 它们全是**原版序列化字段**的抄录，
        /// 出处逐条写在上面；自检里另有一条把**列数**按原版字面量（372.04 / 4 / 180）钉死。</summary>
        public static int DeckCols
        {
            get
            {
                float avail = DecksCR - DecksCL;
                return Mathf.Max(1, Mathf.FloorToInt((avail - (DeckPadL + DeckPadR) + DeckGapX + 0.001f)
                                                     / (DeckCellW + DeckGapX)));
            }
        }
        /// <summary>第一列的**左沿** —— 照 uGUI `LayoutGroup.GetStartOffset(0, 需要的宽)`：
        /// `pad.left + alignX × (可用宽 − (需要的宽 + pad.horizontal))`
        /// = `4 + 0.5 × (372.04 − (360 + 4))` = `4 + 4.02` = **8.02**（相对 `Content` 左沿 262.06）
        /// ⇒ 第 1 列 = **270.08..450.08**、第 2 列 = **450.08..630.08**。
        /// 判据 = `LayoutGroup.cs:193` + `GridLayoutGroup.cs:284/326`。</summary>
        public static float DeckCellX0 { get { return DecksCL + DeckPadL + DeckStartSurplus; } }
        static float DeckStartSurplus
        {
            get
            {
                float avail = DecksCR - DecksCL;
                float required = DeckCols * DeckCellW + (DeckCols - 1) * DeckGapX;
                float alignX = (DeckAlign % 3) * 0.5f;
                return (avail - (required + DeckPadL + DeckPadR)) * alignX;
            }
        }
        /// <summary>🔴 **2026-10-07（A77-㉒②③）**：原来这里是 `public const float DeckNameH = 38f;`
        /// —— 注释自己写着「**这一条是我们挑的**（原版名字在**格的底部**）」。
        /// **现在照原版把它摆回格底**（判据见下面 `Item*` 那一组常量），`DeckNameH` 已删。
        /// ⚠️ **副作用（如实登记）**：摆格底之后，名字条**不再压在视口上渐隐带里** ⇒ 原来那条
        ///    「第 1 格的卡组名也吃软边」的断言在本夹具下**没有鉴别力**了（详见 `MainMenuScene` 那一节
        ///    的订正：改成**换一份能滚动的夹具**再验）。
        /// ⛔ 别再写回「我们挑的 38px 顶条」。</summary>
        // ============================================================ 原版 `Deck Selector Menu Item` 内景（㉒②）
        //
        // 判据出处（2026-10-07 现读，两条同一次命令）：
        //   `python 工具/menu_dump.py bundle_menus_assets_all "Deck Selector Menu Item" --depth 6 --md`
        //   —— 那是**独立 prefab**（`PracticeModePopup.deckSelectorMenuItem` 字段指的就是它），
        //      模板根 = **296×296** · 子件 `Highlight`/`Button border`/`Main image`/`IsPlayerDeck`/`Text background`
        //      （+ `Text background` 下的 `Deck Name`）。
        // 🔴 **换算不是「整体乘 180/296」**：原版那 296 的模板被 `GridLayoutGroup`（`m_CellSize 180×180`）
        //    **拉成 180×180** ⇒ 「锚点撑满」的层按比例缩、「固定 `sizeDelta`」的层**不缩**
        //    （实测：`Highlight` 293.29 → **178.46**，而 `IsPlayerDeck` 85.78 → **52.93**、`Text background`
        //    高 50.32 → **30.60** —— 三条各是各的比例）。所以下面每个数都是**照那一层的锚点五元组现算**的，
        //    推导写在各自那一行。算式 = uGUI 那三句（`LayoutGroup` 家族通用）：
        //      锚点两端 = `根左上 + anchorMin × (宽,高)` / `根左上 + anchorMax × (宽,高)`（y 从**下**边量）
        //      尺寸 = (锚点两端之差) + `sizeDelta` · 中心 = 两端中点 + `anchoredPosition`（y 向上为正）
        //    ⛔ 别把 296 那一列数字直接抄进来（会缩错）。
        //    ✅ **自证过一遍**：把 296/296 代进同一算式，5 层逐层复现 `menu_dump` 打出来的绝对矩形
        //      （`Highlight` 3.26,17.84→296.54,311.14 · `Button border` 29.24,45.45→270.55,286.78 ·
        //       `Main image` 0,0→296,296 · `IsPlayerDeck` 226.21,157.92→311.99,329.91 ·
        //       `Text background` 0,227.92→296,278.24 —— 与实读**逐值吻合**）。
        /// <summary>`Highlight`（sprite **`UI_Deck_button_click`** 169×169 · `Simple` · **无** `preserveAspect`）——
        /// 锚 `(0.0101351,0)→(1,0.939243)` · pos `(0.399994,−7.49998)` · size `(0.288399,15.2883)` ⇒ 格内
        /// **2.08,10.79→180.54,195.14**（178.46×184.35，右下都探出格 —— 原版就这样，`Content` 上**没有**遮罩，
        /// 只有 `Viewport` 那层 `RectMask2D`）。
        /// 🔴 **它就是「当前这套」的唯一指示**（`DeckSelectorMenuItemDemo.SelectItem()` → `highlightObject.SetActive(true)`；
        /// 而每个格子 `Initialize(..., isSelected)` 里 `+0x38` 传的是 `false`）⇒ 原来那条「黄色 = 当前套」的
        /// 自建染色**依据不足**（原注释写「原版怎么标当前套本地判据不足」，**那条已被判据推翻**）。</summary>
        public const float ItemHLL = 2.08f, ItemHLT = 10.79f, ItemHLR = 180.54f, ItemHLB = 195.14f;
        /// <summary>`Button border`（sprite **`UI_Button_Round_background`** 237×237 · `Simple` · **`preserveAspect=1`**）
        /// —— 锚 `(0.098,0.0303784)→(0.915487,0.847892)` · pos `(−0.100006,−0.100006)` · size `(−0.661804,−0.661804)`
        /// ⇒ 格内 **17.87,27.81→164.36,174.30**（146.49×146.49，正方形 ⇒ PA 不缩）。
        /// 它是 `buttonBorderForPlayerDeck`（`DeckSelectorMenuItemDemo` 的 `+0x60`，**`set_enabled`**）——
        /// 原版只对**玩家自己的卡组**开（演示卡组那一支 `set_enabled(0)`）；本窗只列玩家自己的卡组 ⇒ **恒开**。</summary>
        public const float ItemBdL = 17.87f, ItemBdT = 27.81f, ItemBdR = 164.36f, ItemBdB = 174.30f;
        /// <summary>`IsPlayerDeck`（sprite **`Purity Seal_02`** 128×256 · `Simple` · 无 PA）——
        /// 锚 `(0.766865,−0.115)→(1.05,0.466243)` · pos `(0.205421,0.100006)` · size `(1.9695,−0.0610962)`
        /// ⇒ 格内 **137.26,96.01→190.19,200.57**（52.93×104.56，右/下都探出格）。
        /// 它是 `highlightIsPlayerDeck`（`+0x40`，`SetActive`）—— 同 `Button border`，只对玩家卡组开 ⇒ **恒开**。
        /// ⚠️ 原版 sibling **排在 `Main image` 之后** ⇒ 画在它上面（⛔ 别按「印章该在最上面」重排）。</summary>
        public const float ItemSealL = 137.26f, ItemSealT = 96.01f, ItemSealR = 190.19f, ItemSealB = 200.57f;
        /// <summary>`Text background`（sprite **`40k_bt_underbutton`** 485×83 · `Simple` · `m_Color=(0.408,0.811,0.279,1)`）
        /// —— 锚 `(0,0.06)→(1,0.23)` · pos `(0,0)` · size `(0,0)` ⇒ 格内 **x 0→180 · y 138.60→169.20**（180×30.60）。
        /// ⚠️ 高 **30.60** 是「锚点跨 0.17 × 180」算出来的，**不是** 50.32×180/296=30.6 —— 数值巧合，别看错来源。</summary>
        public const float ItemNameBgL = 0f, ItemNameBgT = 138.60f, ItemNameBgR = 180f, ItemNameBgB = 169.20f;
        /// <summary>`Deck Name`（`Text background` 的子件）—— 锚 `(0,0)→(1,1)` · pos `(−2,0)` · size `(−8,0)`
        /// ⇒ 格内 **x 2→174（宽 172）· y 138.60→169.20**。
        /// 字号 **29** · 自适应 **`[8,29]`**（`m_fontSizeMin/Max`）· 对齐 **Center/Middle** · **折行 = 1**。
        /// 🔴 **㉒③ 的那条**：原来名字条是 **172px 宽 + fs26 + 没有自适应**（我们挑的）⇒ 长卡组名画到框外；
        /// 现在照原版把 `SetAutoFitBox(172, 30.6, 8, 29)` 接上。</summary>
        public const float ItemNameL = 2f, ItemNameT = 138.60f, ItemNameR = 174f, ItemNameB = 169.20f;
        public const float ItemNameFontPx = 29f, ItemNameAutoMin = 8f, ItemNameAutoMax = 29f;
        /// <summary>`textBackground` 那一层实际用的色 = **`DeckSelectorMenuItemDemo.yourDeckColor`**
        /// （`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_914357594831109544.json` 实读：
        /// `{r:1.0, g:0.7951523065567017, b:0.0, a:1.0}`）。
        /// 🔴 它与 prefab 里那颗 `Image` **自带**的 `m_Color = (0.408,0.811,0.279,1)` **不是一回事** ——
        /// 后者是模板默认，`InitializeWithPlayerDeck`（我方卡组那一支）运行期把 `yourDeckColor` 盖上去
        /// （`+0x50/+0x58` 那对折成 `Color` 传给 `+0x48` 的 setter）。本窗只列玩家自己的卡组 ⇒ 取这个。
        /// ⛔ 别退回 `m_Color`（那是「演示卡组」那一支的底色，本窗走不到）。</summary>
        public static readonly Color ItemYourDeckColor = new Color(1f, 0.7951523f, 0f, 1f);
        public const float ArmL = 69.42f, ArmT = 182.18f, ArmR = 246.54f, ArmB = 880.17f;
        /// <summary>🔴 **2026-10-05 本轮补**：`Army Selector/Viewport`（挂 `RectMask2D` 的那一件）的矩形 =
        /// **69.42,149.07→246.54,913.28**（177.12 × **764.21**）—— **比父件 `Army Selector` 高 66.22px、上下各探出 33.11**。
        /// 出处 = `python 工具/menu_dump.py bundle_menus_assets_all "Practice Mode Menu" --depth 6 --md`
        /// 的 `Army Selector/Viewport` 行（锚 `(0,0)→(1,1)` · `pivot (0,1)` · `posDelta (0, 33.1124)` · `sizeDelta (0, 66.2249)`）。
        /// 🔴 **裁切与滚动范围一律用它** —— 原来我们用的是父件那个矩形（182.18→880.17）⇒ 上沿少 33.08、下沿少 33.11，
        /// **一共少 66.2px**（第一格跟着下移 33.08）。⚠️ 父件 `Army Selector` 自己的矩形**没写错**
        /// （原版就是 69.42,182.18→246.54,880.17）—— 错的是「拿父件当视口」。
        /// （铁律 5·c：同一个 `ScrollRect` 底下，`Viewport` 与它**自己**不是同一个矩形 —— 这一处差 66px，
        ///  而同窗的 `Decks Scroll view` 那一处**两者恰好同矩形**；**逐处实读，别互推**。）</summary>
        public const float ArmVpL = 69.42f, ArmVpT = 149.07f, ArmVpR = 246.54f, ArmVpB = 913.28f;
        /// <summary>阵营格 **82×82**、**纵向** spacing **26.38**、pad **0**、`m_ChildAlignment = 4`
        /// —— 原版 `Filters` 的 `GridLayoutGroup` 字段逐条抄。
        /// 出处 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-3879242981144375492.json`
        /// （全包**只有这一份** `m_CellSize = 82×82`；宿主 GameObject 的名字就叫 `Filters`）。</summary>
        public const float ArmyCell = 82f, ArmyGapY = 26.38f;
        /// <summary>`Filters` 的 `m_ChildAlignment` 序列化值 = **4**（`TextAnchor.MiddleCenter`）。
        /// ⚠️ 对**横轴**它等价于 `1 UpperCenter`（只有 `align % 3` 参与）⇒ 一列 82 宽的格在 177.12 里**居中**；
        /// 纵轴那半（`align / 3 = 1`）只在内容**比容器矮**时才生效 —— 13 个阵营的内容高 1382.56，
        /// 被 `ContentSizeFitter`（`m_VerticalFit = 1 MinSize`，与本 GridLayoutGroup 同 GO）撑到比视口高
        /// ⇒ 纵轴余量为 0、恒贴顶（第一格 = `Viewport` 上沿）。</summary>
        public const int ArmyAlign = 4;
        /// <summary>阵营格的**左沿** —— 照 uGUI 现算（`GetStartOffset(0, 需要的宽)`）：
        /// `pad.left + alignX × (可用宽 − (需要的宽 + pad.horizontal))` = `0 + 0.5 × (177.12 − 82)` = **47.56**
        /// ⇒ 格 = **116.98..198.98**（横向居中）。
        /// 判据 = uGUI 源码 `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/LayoutGroup.cs:193`
        /// （`GetStartOffset`）+ `…/GridLayoutGroup.cs:326`（`startOffset` 那两句 / `:284` `requiredSpace`）。
        /// 🔴 原来我们**靠左**（69.42..151.42）—— 与居中差 **47.56px**。</summary>
        public static float ArmyCellL
        {
            get
            {
                float alignX = (ArmyAlign % 3) * 0.5f;       // 照 uGUI `GetAlignmentOnAxis(0)`（`4 % 3 = 1` ⇒ 0.5 = 居中）
                return ArmVpL + ((ArmVpR - ArmVpL) - ArmyCell) * alignX;
            }
        }

        // ---- 🆕 2026-10-04（§三第29条 A9 尾巴）：本窗**两处** `RectMask2D.m_Softness` ----
        // 判据 = **逐处实读** `_tmp_view/q1_rm2d.txt`。
        //   ⚠️ **2026-10-05 更正（铁律 5，标签错、值没错）**：原来称它「156 个 `RectMask2D` 的全量 dump」
        //   是错的 —— 那张表**只扫了 3 个菜单族包**（表头 `150 + 1 + 5 = 156`）；**全库真值 = 222**
        //   （菜单族 156 + `mainmenualwaysloaded` 1 + 通用弹窗 5 + `scenes_mainmenuwarpforge` 1
        //   + 13 个 arena 各 5 = 65）。🔑 两条复现判据（会再犯）：① 认的是 `m_Script` 的 PathID
        //   **`536591447201701790`**（`m_FileID = 1` → `bundle_Waprforge_monoscripts`）—— 拿工程本地
        //   `com.unity.ugui` 的 guid 去 grep 解包目录**命中 0**；② **必须限定 `MonoBehaviour/`**
        //   （整包 grep 会逐包多算 1）。逐包数字只留一处 → `MenuWindowBase.ClipSoftness` 的注释。
        //   下面那两条值取自本窗 prefab，**不受这次标签订正影响**：
        //   · `Practice Mode Menu/Deck Selector/Army Selector/Viewport`                    = **(0,50)**（:136-137）
        //   · `Practice Mode Menu/Deck Selector/Deck Buttons/Decks Scroll view/Viewport`   = **(0,23)**（:208-209）
        // 🔴 **同一扇窗里两个视口就是两个值** —— ⛔ 别互推、别拿一个去顶另一个（铁律 5·c）。
        // ⚠️ **渐隐带按【我们这一格实际裁到的那条边】摆**（带宽 = 原版那个分量）——
        //    ✅ **2026-10-05 起两处的裁切矩形都＝原版 `Viewport` 的矩形**了：
        //       `Army Selector/Viewport` = 69.42,149.07→246.54,913.28（见 `ArmVp*`）·
        //       `Decks Scroll view/Viewport` = 261.28,262.64→634.88,803.43（**与父件同矩形**，见 `Decks*`）。
        //    ⇒ 软边切线的位置可以直接拿原版那个矩形现算，**不再需要折中**。
        //    🔴 **原来这里记的是**「本窗的滚动视口是我们自己的矩形（`Army Selector` 182.18→880.17…），
        //       与原版 `Viewport` **不一致**，那是另一条账」—— 那条账**本轮已结**（就是上面那两条常量改的）。
        //    按 `Clip` 配 `ClipSoftness` = 那几处已有接线的写法
        //    （`ForgeTab` / `AvatarTab` / `CampaignTab` / 商店：**谁设 `Clip` 谁顺手把软边设对**）。
        /// <summary>`Army Selector` 那个 `RectMask2D` 的原版 `m_Softness`（画布像素：x 管左右、y 管上下）。</summary>
        public static readonly Vector2 ArmyClipSoft = new Vector2(0f, 50f);
        /// <summary>`Decks Scroll view` 那个 `RectMask2D` 的原版 `m_Softness`。</summary>
        public static readonly Vector2 DeckClipSoft = new Vector2(0f, 23f);
        public const float InfoL = 638.38f, InfoT = 78.79f, InfoR = 1842.38f, InfoB = 863.77f;
        /// <summary>🔴 **2026-09-24 实读订正**：`Deck info` **自己身上没有图** —— 红底在它的子件
        /// `Generic Window Red Background Big` 上，矩形是 **761.63,187.00→1831.93,869.00**
        /// （不是 `Deck info` 那整块 1204×784.98）。两处出处互证：
        /// `menu_dump.py … 7119707400320339772`（`Deck info  MB[DeckGeneralInfoDemo]` 无 Img、子件带 `Img[UI_Deck_Information_Back] type=Sliced`）
        /// 与 `_probe_deckinfo.py`（`Deck info` 的组件只有 RectTransform + DeckGeneralInfoDemo）。
        /// ⚠️ 正本 `阶段二_战斗入口_原版规格.md` §二 A 把这张图写在 `Deck info` 那一行 —— 与实读不符。</summary>
        public const float BgBigL = 761.63f, BgBigT = 187.00f, BgBigR = 1831.93f, BgBigB = 869.00f;
        public const float DlL = 1240.38f, DlT = 207.65f, DlR = 1810.17f, DlB = 827.18f;
        /// <summary>`Deck List Drawer/Content` 的矩形（原版 LayoutGroup 节点本身）。
        /// 🔴 我们原来把卡列表起在 `Deck List Drawer` 自己的左上角（1240.38, 207.65）——
        /// 正好压在 `Deck Name`(224.13→271.82) / `Warlord Name`(269.62→303.62) 上（第 53 条那条「字叠成一团」）。</summary>
        public const float DlContentL = 1261.27f, DlContentT = 318.42f, DlContentR = 1787.37f, DlContentB = 793.87f;
        /// <summary>格：**231×27.88**、spacing **(22, 3.6)**、pad **(22, 0, 4, 0)**（原版 `GridLayoutGroup` 字段，逐条抄）。</summary>
        public const float DlCellW = 231f, DlCellH = 27.88f, DlGapX = 22f, DlGapY = 3.6f;
        /// <summary>卡列表格子里那颗卡名的自适应窗口 —— **原版 `m_fontSizeMin/Max` 的原文**。
        /// 出处 = `python 工具/menu_dump.py bundle_menus_assets_all "Deck Selector Card Info button" --depth 5 --md`
        /// （2026-10-07 现读）：`Content/Text fill/Card Name` = `字号 38 · auto[2.0~38.0] · 折行=0 · Left/Capline`。
        /// 🔴 **㉒⑤ 的真偏离就是这一对**：原来传的是 `10 / 20`（我们挑的），原版是 `2 / 38`。</summary>
        public const float CardNameAutoMin = 2f, CardNameAutoMax = 38f;
        public const float DlPadL = 22f, DlPadT = 4f;
        public const float DnL = 1371.64f, DnT = 224.13f, DnR = 1726.64f, DnB = 271.82f;
        /// <summary>⚠️ 原版 `Warlord Name` 的矩形**宽 0**（`ContentSizeFitter` + `RectSizeLimiter` 运行时算）
        /// ⇒ **右沿是我们挑的**（取 `Deck Name` 的 `DnR`）。**不是复刻**。</summary>
        public const float WnL = 1373.78f, WnT = 269.62f, WnR = 1726.64f, WnB = 303.62f;
        /// <summary>`Army Image` **105.26×104**（原版 sprite = 该阵营的 `40k_DeckSelection_icon_Faction*`）。</summary>
        public const float ArmImgL = 1258.85f, ArmImgT = 210.08f, ArmImgR = 1364.11f, ArmImgB = 314.08f;
        /// <summary>`Cardback`（原版 `sprite=0`、运行时喂 `CardDeck.GetDeckCardback`）。</summary>
        public const float CbL = 1542.35f, CbT = 329.63f, CbR = 1751.91f, CbB = 631.96f;
        public const float LoreL = 1280.27f, LoreT = 647.41f, LoreR = 1770.27f, LoreB = 791.41f;

        // 🆕 2026-10-03（A14）：`General container` 里补的两件（判据 → `阶段二_战斗入口_原版规格.md` §二 A）
        /// <summary>`Deck Information Cost/balance text`：**1276.27,326.95→1540.27,381.27** · fs34 auto[1-34] · 居中。</summary>
        public const float CostTxtL = 1276.27f, CostTxtT = 326.95f, CostTxtR = 1540.27f, CostTxtB = 381.27f;
        /// <summary>`Deck Information cost drawer`：rect **158.15×199.06**、**`localScale = 1.2`** ⇒ 真画 189.78×238.87。
        /// 中心 **(1408.275, 504.71)**。里面 9 行的画法与 `Deck Info Popup` 那扇**共用** `Core/CostCurveDrawer`。</summary>
        public const float CostDrwCX = 1408.275f, CostDrwCY = 504.71f, CostDrwScl = 1.2f;
        /// <summary>费用曲线 9 行的读数（自检用）。</summary>
        public readonly System.Collections.Generic.List<int> CostRows =
            new System.Collections.Generic.List<int>();
        public const float ShowBtnL = 1729.25f, ShowBtnT = 238.17f, ShowBtnR = 1793.49f, ShowBtnB = 301.41f;
        public const float ChgL = 1398f, ChgT = 680.75f, ChgR = 1652.54f, ChgB = 764.08f;
        public const float TogL = 813.24f, TogT = 906.26f, TogR = 1106.76f, TogB = 964.10f;
        public const float ContL = 1384.90f, ContT = 906.26f, ContR = 1837.98f, ContB = 956.10f;
        public const float BtTxL = 1385.08f, BtTxR = 1672.41f, BtTxT = 911.25f, BtTxB = 956.10f;
        public const float CircL = 1678.74f, CircT = 886.18f, CircR = 1768.74f, CircB = 976.18f;

        static readonly Vector4 InfoBorder = new Vector4(42f, 363f, 655f, 81f);
        const float InfoTexW = 1100f, InfoTexH = 701f;
        static readonly Vector4 SelBorder = new Vector4(0f, 0f, 0f, 0f);
        const float SelTexW = 1f, SelTexH = 1f;

        /// <summary>卡列表列数 = `floor((526.10 − 22 + 22) ÷ (231 + 22))` = **2**
        /// （照原版 `GridLayoutGroup`：`constraint=0/Flexible` ⇒ 按宽度算，pad 左 22 右 0）。</summary>
        public static int ListCols
        {
            get
            {
                float usable = (DlContentR - DlContentL) - DlPadL;
                return Mathf.Max(1, Mathf.FloorToInt((usable + DlGapX) / (DlCellW + DlGapX)));
            }
        }

        /// <summary>空节点（**`RectTransform`**）。🔴 **2026-10-07（A92）**：原来是裸 `Transform`。
        /// 判据与实读见 `MenuDraw.Node` 的注释；本文件这 9 个名字逐个核过，**全是 `RectTransform`**
        /// （`Deck info` · `Background Info` · `General container` · `Army Selector`（原版 `ScrollRect`）·
        /// `Viewport`（原版挂 `RectMask2D`）· `Filters`（原版 `GridLayoutGroup`）· `Decks Scroll view` ·
        /// `Content` —— 实读 `bundle_menus_assets_all` 同名 `GameObject` 的组件类型）。
        /// ⛔ 别写成 `AddComponent&lt;RectTransform&gt;()`。</summary>
        static Transform New(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        Label _txtArmy, _txtDeckName, _txtWarlord;
        Transform _info, _bgInfo, _general, _deckList;
        ImageQuad _armyIcon, _cardback;

        /// <summary>🔴 **两个抽屉是互斥的**（原版 `DeckGeneralInfoDemo.Toggle(bool)`：
        /// `generalInfoContainer.SetActive(opt)` + `cardsInDeckPanel.gameObject.SetActive(!opt)` —— 反编译
        /// `DeckGeneralInfoDemo__Toggle.c`，两个字段的 pid 由 `工具/_probe_deckinfo.py` 从序列化数据解出：
        /// `generalInfoContainer` = GO `General container` · `cardsInDeckPanel` = GO `Deck List Drawer`）。
        ///
        /// **出厂态 = 总览（`Toggle(true)`）**：原版 `SetContent` 末尾就是
        /// `generalInfoContainer.SetActive(true)` + `cardsInDeckPanel.SetActive(false)`，而
        /// `PracticeModePopup.DeckSelected → DeckGeneralInfoDemo.SetContent` 是开窗/换卡组的必经之路
        /// （`PracticeModePopup__DeckSelected.c`）。
        ///
        /// 挂父也一并订正（正本 §二 A 那张表的缩进是错的）：
        /// `Deck Name` / `Warlord Name` / `Army Image` / `Background Info` / `General container` / `Deck List Drawer`
        /// **全是 `Deck info` 的直接子件**；`Background Info` 实读是**空容器**。</summary>
        void BuildDeckInfo(Transform root)
        {
            _txtArmy = Txt(root, SelectedArmyName(), ArmyTxL, ArmyTxR, ArmyTxT, ArmyTxB, 36f, Align.Center, "Selected Army Title", QPrText);

            // `Deck info` —— **容器本身没有图**（红底在子件 `Generic Window Red Background Big` 上，见常量段注释）
            _info = New(root, "Deck info");
            _info.localPosition = Local3(root, InfoL, InfoT, InfoR, InfoB);

            Nine(_info, _info, "UI_Deck_Information_Back", InfoBorder, InfoTexW, InfoTexH,
                 BgBigL, BgBigT, BgBigR, BgBigB, QPr, "Generic Window Red Background Big");
            // 🆕 **2026-10-06（A94）：右半那块红底也吸收点击**（本窗有**两块**互不相连的面板底图 ⇒
            //   两处各一行 —— 裁定那句「一行一处」说的是每扇窗都要接，不是「一扇窗只许接一处」）。
            //   判据 = 原版 prefab `Practice Mode Menu > Deck info > Generic Window Red Background Big`
            //   那颗 `Image` 的 **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读），rect = 761.63,187→1831.93,869。
            //   ⚠️ **同一扇窗的第三块面板底**：原版 `Deck Selector > Army Selector > Background`
            //   （`UI_Background faction buttons`，69.42,182.18→246.54,880.17）**也是 ray=1、也吸收**。
            //   🔴 **2026-10-06（A132）就地订正**：本行原来写的是「**我们那一列根本没画那颗底图** ⇒ 这一块
            //   **不接**，等底图补上再接」。那句「**没画**」是**对的**（底图确实一直缺），但**本轮**把
            //   底图与它的吸收层**一起补上了** ⇒ 那一块**现在是接的**（见 `BuildArmySelector` 里那两行）。
            //   ⛔ 别再照着旧注释把那一行删掉。
            MenuDraw.Absorb(root, "AbsorbHitDeckInfo", new PxRect(BgBigL, BgBigT, BgBigR, BgBigB), QPr, QPrHit);

            // `Background Info` —— 原版实读：**【空容器】+ 出厂 act=0**（一条子件都没有）。
            // ⚠️ 它**不是** `Deck Name`/`Lore Text` 那几件的父（正本 §二 A 那么写是错的 —— 第 53 条）。
            //    照原版**留空**：建出来、关着，让别人一眼看得出「这里原版就是空的」。
            _bgInfo = New(_info, "Background Info");
            _bgInfo.localPosition = Local3(_info, 1258.08f, 316.03f, 1789.08f, 800.45f);
            _bgInfo.gameObject.SetActive(false);

            // ---- 直挂 `Deck info` 的三件（原版 act=1 ⇒ **本来就该看得见**）----
            BuildDeckInfoHead();

            // ---- `General container`（**出厂可见**那个抽屉）----
            _general = New(_info, "General container");
            _general.localPosition = Local3(_info, DlL, DlT, DlR, DlB);
            BuildGeneralContainer();

            // ---- `Deck List Drawer`（原版 act=1，但出厂被 `SetContent` 关掉；内容见 `BuildCardRows`）----
            BuildCardRows(_info);
            Toggle(true);                       // 出厂 = 总览（`SetContent` 那两句）
        }

        /// <summary>`Deck Name`(fs36 居中) / `Warlord Name`(fs35 **左对齐**) / `Army Image`(105.26×104)。
        /// ⚠️ `Warlord Name` 原版矩形**宽 0** 且 **`pivot=(0, 0.5)`**（实测）⇒ `ContentSizeFitter`
        /// 从 **x=1373.78 向右撑开**；TMP 那边虽然写 `hAlign=Center`，但框是贴合文本的 ⇒ 视觉上就是**左起**。
        /// 我们给它一个固定框（右沿取 `Deck Name` 的 `DnR`，**这一条是我们挑的**）并按**左对齐**画，
        /// 起点与原版一致（用 Center 会右移约 176px）。</summary>
        void BuildDeckInfoHead()
        {
            var d = CollectionData.DeckAt(DeckIndex);
            var wl = CollectionData.Warlord(DeckIndex);
            _txtDeckName = Txt(_info, d.Name, DnL, DnR, DnT, DnB, 36f, Align.Left, "Deck Name", QPrText);
            _txtWarlord = Txt(_info, wl != null ? wl.Name : "未选督军", WnL, WnR, WnT, WnB, 35f,
                              Align.Left, "Warlord Name", QPrText);
            _armyIcon = Img(_info, DeckRuntime.FactionIcon(d.Faction), ArmImgL, ArmImgT, ArmImgR, ArmImgB,
                            "Army Image", QPrRow, true);
        }

        /// <summary>`General container` 里的几件：cardback · `Show Deck Content Button` · `Change Deck`。
        /// **没建的**（出声，见文件头）：`Deck Information cost drawer` + `Deck Information Cost/balance text`。
        /// 🔴 **2026-10-03 就地更正（A20）**：这里原来写着「我们没有『卡组装备了哪张卡背』这份数据 ⇒
        /// 退回 `CardArt.CardBack(阵营)`……**这条是我们挑的，不是复刻**」—— **那句话现在作废**：
        /// `CollectionData.DeckInfo` **有 `CardbackId`**（`Shell/CollectionData.cs:30`，卡组编辑器那页能装备），
        /// 而原版这条链要的就是它（`DeckGeneralInfoDemo.ShowPayerDeckInfo → CardDeck.GetDeckCardback()`）
        /// ⇒ 改成走**判据那一处** `CardArt.DeckCardback(卡背 id, 阵营)`（选了用选的、没选用阵营默认背）。</summary>
        void BuildGeneralContainer()
        {
            var d = CollectionData.DeckAt(DeckIndex);

            // 🔴 **`Lore Text` 不画**（2026-09-24）：原版它喂的是 `DemoDeckInfoSO.Lore`（**卡组**的简介，
            //   `ShowPayerDeckInfo` 里那句 `DemoDeckInfoSO__get_Lore` + `set_text`）。
            //   我们的卡组**没有这个字段**，上一版拿**督军的效果文字**顶上去 ⇒ 两宗错：
            //   ① 内容不对（效果文字 ≠ 卡组简介）· ② 它的框 1280.27,647.41→1770.27,791.41
            //      **套着** `Change Deck`（1398.00,680.75→1652.54,764.08）⇒ 实拍里两行字叠在一起。
            //   按「宁可没有，不可错着显示」**不画**，并把这条记在这里与文件头。
            Debug.Log("[Practice] `Lore Text` **没画** —— 原版喂的是 `DemoDeckInfoSO.Lore`（卡组简介），"
                      + "我们的卡组没有这个字段（拿督军的效果文字顶上去会和 `Change Deck` 叠）。**出声，不静默**");

            // 🆕 2026-10-03（§三第29条 A14）：`Cost/balance text` + `Deck Information cost drawer` **补上了**。
            //   判据：`资料/阶段二_战斗入口_原版规格.md` §二 A 那张表（`python 工具/menu_dump.py … "Practice Mode Menu"`）
            //   · `Cost/balance text` **1276.27,326.95→1540.27,381.27** · fs34 auto[1-34] · 居中 · 原文 `Card / Energy cost`
            //   · `cost drawer` **1329.20,405.18→1487.35,604.24** · **`localScale 1.2`**（`Deck Info Popup` 那份是 1.8）
            //   🔴 **画法不在这儿**：与 `Deck Info Popup` 那扇共用 `Core/CostCurveDrawer`
            //      （CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
            Txt(_general, "Card / Energy cost", CostTxtL, CostTxtR, CostTxtT, CostTxtB, 34f, Align.Center,
                "Deck Information Cost/balance text", QPrText);
            {
                var counts = CostCurveDrawer.Counts(CollectionData.Raw(DeckIndex), CollectionData.Card);
                CostCurveDrawer.Build(_general, CostDrwCX, CostDrwCY, CostDrwScl, counts, QPrRow, QPrText);
                CostRows.Clear();
                CostRows.AddRange(counts);
                Debug.Log("[Practice] 费用曲线画了 9 行（" + CostCurveDrawer.Dump(counts) + "）");
            }

            _cardback = ImgTex(_general, CardArt.DeckCardback(d.CardbackId, d.Faction), "Cardback(这副牌的卡背)", CbL, CbT, CbR, CbB,
                               "Cardback", QPrRow, true);

            // ⚠️ 图标那层用**原版 `Icon` 子件自己的矩形**（1737.68,246.46→1785.07,293.12 = 47.39×46.66），
            //    不是父件的 64.24×63.24（用父的会把图标放大 1.36 倍、而且不居中）
            Img(_general, "UI_Button_Round_background", ShowBtnL, ShowBtnT, ShowBtnR, ShowBtnB, "Show Bg", QPrRow, true);
            Img(_general, "40k_UI_bt_deck", 1737.68f, 246.46f, 1785.07f, 293.12f, "Show Icon", QPrRow, true);
            // ✅ **2026-09-24 订正**：原版这颗钮**不是**翻开卡列表抽屉 —— 它是
            //    `DeckGeneralInfoDemo.cardInDeckInfoButton`（pid 由序列化字段解出），
            //    点下去的 `CardInDeckInfoButtonOnClick` 走 `WindowsManager.OpenWindow(new DeckInfoContext(deck, 2, …))`
            //    ⇒ **开的是「卡组详情」那扇窗**（`Deck info Popup`，我们已建）。
            HitOn(_general, _general, "ShowDeckHit", new PxRect(ShowBtnL, ShowBtnT, ShowBtnR, ShowBtnB),
                  () => ShowDeckContent());

            Img(_general, "UI_Button_Mulligan", ChgL, ChgT, ChgR, ChgB, "Change Deck Bg", QPrRow, true);
            Txt(_general, "Change Deck", ChgL, ChgR, ChgT, ChgB, 45f, Align.Center, "Change Deck Text", QPrText);
            // ✅ 原版这条就是 `DeckGeneralInfoDemo.ChangePlayerDeckButton`
            //    → `WindowsManager.OpenWindow(new DeckSelectionContext(...))`（`…__ChangePlayerDeckButton.c`）。
            HitOn(_general, _general, "ChangeDeckHit", new PxRect(ChgL, ChgT, ChgR, ChgB), () => OpenDeckSelection());
        }

        /// <summary>`Show Deck Content` —— **开「卡组详情」窗**（原版 `DeckInfoContext(deck, 2, …)`）。
        /// 🔴 **2026-10-04（§三第29条 A65①）这一行接上了**：`state` 必须传 **`View`(2)** ——
        ///   判据 = `DeckGeneralInfoDemo__CardInDeckInfoButtonOnClick.c` 的
        ///   `DeckInfoContext___ctor(uVar3, deck, **2**, 0, 0, 0, 0)`（第 3 个实参就是 `context.state`）。
        ///   原来走默认值 `Edit`(0) ⇒ 那一扇会**多显示 `Edit Deck` + 4 颗圆钮**
        ///   （`DeckInfoControls__Initialize` 的显隐表：`Edit Deck` ← `state < 2`、四颗圆钮 ← `state == 0 && isPlayerDeck`）。
        /// 🔴 2026-09-24 实读订正：这一件**不是**抽屉开关。两个抽屉的开关只有 `Toggle(bool)` 一个入口，
        /// 而它在本地的全量反编译里**找不到任何调用者**（`grep -l DeckGeneralInfoDemo__Toggle *.c` 只命中它自己）——
        /// 也就是说卡列表那个抽屉在本地这份包里**打不开**。**如实记着**，别自己给它编一个入口。</summary>
        public DeckInfoPopup ShowDeckContent()
        {
            if (Manager == null)
            {
                Debug.LogWarning("[Practice] 没有 `WindowsManager`，开不了 `Deck info Popup`");
                return null;
            }
            var w = DeckInfoPopup.Create(Manager, DeckIndex, DeckInfoPopup.DeckInfoState.View);
            Manager.OpenWindow(w);
            LastDeckInfo = w;
            Debug.Log("[Practice] `Show Deck Content` ⇒ 开 `Deck info Popup`"
                      + "（原版 `cardInDeckInfoButton → OpenWindow(new DeckInfoContext(deck, 2, …))`）"
                      + " · state = **View(2)**");
            return w;
        }

        /// <summary>最近一次开出来的卡组详情窗（自检用）。</summary>
        public static DeckInfoPopup LastDeckInfo;

        /// <summary>`Toggle(bool)` —— 原版 `DeckGeneralInfoDemo.Toggle`：**总览 / 卡列表两个抽屉互斥**。
        /// `generalInfo == true` ⇒ 显示 `General container`、关掉 `Deck List Drawer`（反之亦然）。</summary>
        public void Toggle(bool generalInfo)
        {
            if (_general != null) _general.gameObject.SetActive(generalInfo);
            if (_deckList != null) _deckList.gameObject.SetActive(!generalInfo);
        }

        /// <summary>切到「另一个抽屉」（自检/将来接入口用）。返回切换后**是不是总览**。</summary>
        public bool ToggleDeckInfo()
        {
            bool general = _general == null || !_general.gameObject.activeSelf;
            Toggle(general);
            return general;
        }

        /// <summary>现在显示的是不是总览（= `General container` 开着）。</summary>
        public bool ShowingGeneralInfo { get { return _general != null && _general.gameObject.activeSelf; } }

        /// <summary>当前选中的卡组（默认 = `DeckLibrary` 的当前那套）。</summary>
        public int DeckIndex;
        /// <summary>在「预组卡组」那一页选中的那副（空 = 用的是「我的卡组」）。
        /// ⚠️ **还不能拿去开战** —— 原因与出处见 `PrebuiltDecks.WarnNotPlayableYet`。</summary>
        public PrebuiltDecks.Deck PickedPrebuilt;
        /// <summary>🆕 2026-10-03：本局**对手**卡组（`Deck info Popup` 的 `Practice Deck` 那条链选出来的那一副）。
        /// `null` = 没指定 ⇒ 照旧自动凑 —— 这正是原版练习窗自己那条 `Battle!` 的行为
        /// （`PracticeModePopup__BattleButtonOnClick.c:225`：`enemyDeck` 传的是 **0 / null**）。</summary>
        public PlayerDeck OpponentDeck;
        public int ArmyIndex = -1;              // -1 = 不限阵营
        public readonly List<Transform> DeckRows = new List<Transform>();
        public readonly List<Transform> ArmyCells = new List<Transform>();
        public readonly List<Transform> CardRows = new List<Transform>();
        public MenuScroll DeckScroll, ArmyScroll;
        /// <summary>`Battle!` 真的切了场景没有（自检用 —— 批处理下不切）。</summary>
        public bool StartedBattle { get; private set; }

        public Transform Hit(string name) { return transform.Find(name); }
        public Transform BtHit { get { return transform.Find("BattleHit"); } }
        public Transform BackHit { get { return transform.Find("BackHit"); } }
        public Transform TogHit { get { return transform.Find("ToggleHit"); } }
        public Transform ChgHit { get { return transform.Find("ChangeDeckHit"); } }

        public static PracticeModePopup Create(WindowsManager mgr)
        {
            var go = new GameObject("Practice Mode Menu");
            var win = go.AddComponent<PracticeModePopup>();
            win.type = WindowType.Popup;                 // 实证 type=1
            win.placement = WindowsPlacement.Popup;      // 实证 windowsPlacement=15
            win.closeOnEsc = true;                       // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1.07f;           // 实证 1.07（**别抄 1.0**）
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        /// <summary>最近一次开出来的那扇（自检用）。</summary>
        public static PracticeModePopup LastOpened;

        public override void Open()
        {
            LastOpened = this;
            DeckIndex = CollectionData.CurrentIndex();
            ArmyIndex = -1;
            Build();
        }

        void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);
            DeckRows.Clear(); ArmyCells.Clear(); CardRows.Clear();

            // 1) 压暗整屏 + 点背景关（原版 `BackgroundCloseButton`）
            // 🔴 **2026-09-24 修**：原来传的是 `960,540,1920,1080` —— 那是**右下四分之一**的角色！
            //    `Solid(parent,x1,y1,x2,y2,…)` 是**矩形语义**（原版 = -1327.30,-746.18→3247.30,1826.18）。
            //    （`DeckInfoPopup` 那个 `Solid` 是另一种签名 `(cx,cy,w,h)`，两处的调用串了。）
            Solid(root, -1327.30f, -746.18f, 3247.30f, 1826.18f, ShadeColor, QPr, "Menu Dark Background");
            // 🔴 **2026-10-04（A47 接线批）订正档号：`QPrHit − 1`(3102) → `QPr`(3100)** ——
            //   规矩 = 「压暗层的命中区落在**压暗层自己那一档**，且严格低于本窗任何内容命中区档」
            //   （判据 → `MenuDraw.ShadeHit` 的注释 · `资料/待办判据_阶段二与联机.md` §A25·补（一））。
            //   ⚠️ 3102 恰好是 `QPrText`（**文字那一档**）——「内容档 − 1」看着像派生，其实落在别的层上。
            //   ⚠️ 摆法随之改变（**`MenuDraw.Hit` 的既有摆法**）：节点在父原点、quad 在矩形中心。
            MenuDraw.ShadeHit(root, new PxRect(0f, 0f, 1920f, 1080f), QPr, QPrHit, () => Close(), "BackdropHit");

            // 2) `Back`（圆钮 + 箭头 + 文案）
            Img(root, "UI_Button_Round_background", BackL, BackT, BackR, BackB, "Back Bg", QPrRow, true);
            var backIc = Img(root, "40k_UI_bt_back", BackIcL, BackIcT, BackIcR, BackIcB, "Back Icon", QPrRow, true);
            Txt(root, "Back", BackTxL, BackTxR, BackT, BackB, 45f, Align.Left, "Back Text", QPrText);
            // 🆕 A17：原版 `Practice Mode Menu` 的 `Back button` 是 SpriteSwap（普查 §块 5 第 21 行）
            var backHit = Hit(root, root, "BackHit", new PxRect(BackL, BackT, BackR, BackB), () => Close());
            var backWb = backHit != null ? backHit.GetComponent<WindowButton>() : null;
            if (backWb != null) backWb.Bind(backIc, "40k_UI_bt_back");

            // 3) 选卡组那一列
            // ⚠️ 原版这一件是 **Sliced**（贴图 439×664 · border (0,325,0,35)）—— 原来按 Simple+keepAspect 拉的
            Nine(root, root, "UI_Deck_Selection_Back", new Vector4(0f, 325f, 0f, 35f), 439f, 664f,
                 DbtnL, DbtnT, DbtnR, DbtnB, QPr, "Deck Buttons");
            // 🆕 **2026-10-06（A94）：选卡组那一列的面板底图吸收点击**。判据 = 原版 prefab
            //   `Practice Mode Menu > Deck Selector > Deck Buttons > Generic Window Red Background Small`
            //   那颗 `Image` 的 **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读）—— 射线打到它自己、
            //   父链上没有点击处理器（关窗那颗 `BackgroundCloseButton` 在压暗层上）⇒ 原版**什么都不做**。
            MenuDraw.Absorb(root, "AbsorbHit", new PxRect(DbtnL, DbtnT, DbtnR, DbtnB), QPr, QPrHit);
            Txt(root, "Select deck to play", TipL, TipR, TipT, TipB, 38f, Align.Center, "tooltip", QPrText);
            BuildArmySelector(root);
            BuildDeckRows(root);

            // 4) 右半：卡组信息（`Deck info` 容器 → 红底 + 卡组名/督军名/阵营图 + 两个互斥抽屉）
            //    ⚠️ 红底**不是** `Deck info` 自己那一层画的（见 `BuildDeckInfo` 的注释）
            BuildDeckInfo(root);

            // 5) 底下那一条：`Game mode` 开关 + `Battle!`
            Img(root, "40_main_bt_toggle_off", TogL, TogT, TogR, TogB, "Toggle Bg", QPrRow, true);
            Txt(root, "Game mode", TogL, TogR, TogT, TogB, 36f, Align.Center, "Toggle Label", QPrText);
            HitOn(root, root, "ToggleHit", new PxRect(TogL, TogT, TogR, TogB), () =>
                NotBuilt("`Game mode` 开关（原版切 Classic/Skirmish 两种赛制；本地只有一个卡池口径）"));

            Img(root, "40k_bt_underbutton", ContL, ContT, ContR, ContB, "Continue Button", QPrRow, true,
                new Color(0.369f, 0.894f, 0.587f, 1f));
            Txt(root, "Battle!", BtTxL, BtTxR, BtTxT, BtTxB, 45f, Align.Right, "Battle Text", QPrText);
            Img(root, "40k_UI_bt_play", CircL, CircT, CircR, CircB, "CircleButton", QPrRow, true);
            HitOn(root, root, "BattleHit", new PxRect(ContL, ContT, CircR, CircB), () => StartBattle(),
                  QPrHit);
        }

        /// <summary>左栏：13 个阵营格子（**82×82、纵向 spacing 26.38、横向居中**，可纵向滚）。
        /// 点一个 ⇒ 只显示该阵营的卡组。
        /// 🔴 **2026-10-05 照原版补了两层节点**（原来只有 `Army Selector` 一层、格直接挂在它下面）：
        ///   `Army Selector`（`ScrollRect`，69.42,182.18→246.54,880.17）
        ///     ├ `Background`（**2026-10-06 A132 补**，见下）
        ///     └ `Viewport`（`RectMask2D`，**69.42,149.07→246.54,913.28** —— 比父件高 66.22）
        ///         └ `Filters`（`GridLayoutGroup`，与 `Viewport` 同矩形）
        ///            └ 13 个格
        /// 出处 = 同一次 `menu_dump.py` 的树（名字逐字照抄）。</summary>
        void BuildArmySelector(Transform root)
        {
            var sel = New(root, "Army Selector");                 // `ScrollRect` 自己那一层
            sel.localPosition = Local3(root, ArmL, ArmT, ArmR, ArmB);

            // ============================================================ `Background`（A132）
            // 🔴 **2026-10-06（A132）**：这颗底图**一直缺**（A94 相 1 §四·1 记的「我们那一列根本没画底图」
            //   是**真话**），但当时由此推出的「所以不接吸收层」只对了一半 —— **资源其实一直都在**：
            //     `Resources/Art/ui_menu/UI_Background_faction_buttons.png`（`CardArt.MenuUi` 取得到），
            //     导入名单 `工具/import_original_art.py` 的 `MENU_IMAGES` 里那条注释就写着「练习窗阵营纵列底」。
            //   ⇒ **补底图 + 同时接吸收层**（两件一起做）：只补图不接吸收，我们这边就会多出一块
            //     「看着是块底、点了却没反应」的**死区**（图不吃射线 ⇒ 这一点穿到压暗层上把窗关掉）。
            //
            //   判据 = 2026-10-06 `python 工具/menu_dump.py bundle_menus_assets_all "Practice Mode Menu"`
            //   现读（与 A94 相 1 §2·2 的 `rayscan` 实读逐值吻合）：
            //     `Deck Selector`(77.10,165.33→610.90,858.19)
            //       ├ `Deck Buttons` …                    ← 我们已建（选卡组那一列）
            //       └ `Army Selector`(69.42,182.18→246.54,880.17 · `ScrollRect`+`GraphicRaycaster`)
            //          ├ **`Background`** ← **第一颗子件（排在 `Viewport` 之前 ⇒ 画在格子下面）**
            //          │    `Image` · sprite **`UI_Background faction buttons`**（**54×420**）
            //          │    **`m_Type = Sliced`** · `m_Border = (2,201,2,202)`（**左,下,右,上** = x,y,z,w）
            //          │    `m_PixelsPerUnitMultiplier = 1`（未给） ⇒ 角块 = `m_Border` 原值
            //          │    **`m_RaycastTarget = 1`** ⇒ 射线停在它自己身上，而「点它关窗」那颗
            //          │        `BackgroundCloseButton` 在压暗层上 ⇒ 原版点它**什么都不发生**
            //          └ `Viewport`(69.42,149.07→246.54,913.28) → `Filters` → 13 格
            //   ⚠️ **矩形 = `Army Selector` 自己那一格**（不是 `Viewport` 那一格 —— 那一个上下各探出 33.11）
            //      ⇒ 底图**不被视口裁**（兄弟件，同原版）。
            //   ⚠️ **档 `QPr`(3100)**：与原窗那块「选卡组」面板底（`Deck Buttons`）同档 —— 本窗的
            //      「面板底」这一层就是 `QPr`；格子（`QPrRow` 3101）比它高 ⇒ 照旧画在格子下面。
            Nine(sel, sel, "UI_Background_faction_buttons", new Vector4(2f, 201f, 2f, 202f), 54f, 420f,
                 ArmL, ArmT, ArmR, ArmB, QPr, "Background");
            // 吸收层（公共件建、档由它算 = `qContentMin − 1`）—— 与原版那颗 `m_RaycastTarget = 1` 的
            // `Image` 同一个语义：**这一下被吃掉、什么都不做**。两个档与同窗那次 `ShadeHit` 同源。
            MenuDraw.Absorb(sel, "AbsorbHitArmy", new PxRect(ArmL, ArmT, ArmR, ArmB), QPr, QPrHit);

            var vp = New(sel, "Viewport");                        // 原版挂 `RectMask2D` 的那一件
            vp.localPosition = Local3(sel, ArmVpL, ArmVpT, ArmVpR, ArmVpB);
            var view = new PxRect(ArmVpL, ArmVpT, ArmVpR, ArmVpB);

            var filters = New(vp, "Filters");                     // 原版格容器（`GridLayoutGroup`）
            filters.localPosition = Local3(vp, ArmVpL, ArmVpT, ArmVpR, ArmVpB);

            var facs = CollectionWindow.CardsState.Factions();
            // 内容高照 uGUI `GridLayoutGroup.CalculateLayoutInputVertical`：
            // `pad.vertical + (格高 + spacing.y) × 行数 − spacing.y`（行数 = 13）—— 原来少减了一个 spacing.y。
            ArmyScroll = MenuScroll.TopAligned(view, GridContentH(facs.Count, ArmyCell, ArmyGapY, 0f, 0f));
            ArmyScroll.Owner = gameObject;
            ArmyScroll.OnChanged = () => RebuildArmyCells(filters);
            PointerLayer.RegisterScroll(ArmyScroll);
            _facs = facs;
            RebuildArmyCells(filters);
        }
        List<string> _facs;

        /// <summary>一行网格内容的总高（照 uGUI `GridLayoutGroup`：`padTop + padBottom + (格高 + spacing.y) × 行数 − spacing.y`）。
        /// 0 行给 0。原版这一件是 `ContentSizeFitter`（`m_VerticalFit = 1 MinSize`）跑的，同一个算式。</summary>
        static float GridContentH(int rows, float cellH, float gapY, float padT, float padB)
        {
            if (rows <= 0) return 0f;
            return padT + padB + (cellH + gapY) * rows - gapY;
        }

        void RebuildArmyCells(Transform holder)
        {
            for (int i = holder.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(holder.GetChild(i).gameObject);
            ArmyCells.Clear();
            var facs = _facs ?? new List<string>();
            var view = new PxRect(ArmVpL, ArmVpT, ArmVpR, ArmVpB);
            // 🔴 格位：横轴**居中**（`m_ChildAlignment = 4` ⇒ `align % 3 = 1`），纵轴第一格贴 `Viewport` 上沿
            float x1 = ArmyCellL, x2 = ArmyCellL + ArmyCell;
            for (int i = 0; i < facs.Count; i++)
            {
                float y1 = ArmVpT + i * (ArmyCell + ArmyGapY);
                var r = new PxRect(x1, y1, x2, y1 + ArmyCell);
                var rr = ArmyScroll.Shift(r);
                // 🔴 **2026-10-07（A12①）求交收口**：这里原来是内联的 `Inside(view, rr)`
                //    （= 「**完整**落在视口里才建」）—— 它既不是 `MenuScroll.Intersects`（只判滚动轴）
                //    也不是 `MenuDraw.Visible`（两轴求交），是**第三套**判据 ⇒ 收进唯一那一份。
                //    🔴 **语义变化（主动改的，逐处登记）**：判据从「**完整**在视口里」→「**与视口相交**」。
                //      判据 = 原版 `RectMask2D` —— 它只**裁**，不判「整格在不在」：压在视口边上的行
                //      原版**是画出来（被裁掉一半）的**，不是整行消失 ⇒ 旧的「整格进出」是偏离原版的那一档。
                //      ⚠️ 代价/收益：半行现在会露一半（`Img` 吃着 `view` + `ArmyClipSoft (0,50)` 那道渐隐带
                //      就是为这一档准备的）；命中区同批跟着截到视口内（见 `HitOn` 的 `clip`）。
                if (!MenuDraw.Visible(rr, view)) continue;   // 整块在视口外 ⇒ 不建（连点击区一起）
                var cell = new GameObject("Army_" + i);
                cell.transform.SetParent(holder, false);
                cell.transform.localPosition = Local3(holder, rr.x1, rr.y1, rr.x2, rr.y2);
                // 🆕 A9 尾巴：这一格吃**软边**（原版 `Army Selector/Viewport` 的 `m_Softness = (0,50)`）——
                //   压在渐隐带里的**第一格**会被按剖面削 alpha（`MenuDraw.ApplySoftEdges` 的几何等效物）。
                Img(cell.transform, DeckRuntime.FactionIcon(facs[i]), rr.x1, rr.y1, rr.x2, rr.y2,
                    "Icon", QPrRow, true, null, view, ArmyClipSoft);
                int idx = i;
                HitOn(cell.transform, cell.transform, "Hit", rr, () => PickArmy(idx), QPrHit, view);
                ArmyCells.Add(cell.transform);
            }
        }

        /// <summary>卡组列表（纵向滚）。🔴 **2026-10-05 起照原版 = 两列 `180×180` 的网格**
        /// （原来是我们挑的「单列 38px 行」—— 判据一直都有，见 `DeckCellW` 那段更正痕）。
        /// 节点也照原版补齐：`Decks Scroll view`（`ScrollRect`）→ `Viewport`（`RectMask2D`，**同矩形**）
        /// → `Content`（`GridLayoutGroup`，262.06..634.10）。
        /// ⚠️ 这里那两行「内容高」只是**给 `MenuScroll` 一个初值**（构造函数要）—— 真正的口径在
        ///    `RebuildDeckRows` 末尾（**每次重建都重算**，㉒④；换阵营筛掉一批时要跟着缩）。</summary>
        void BuildDeckRows(Transform root)
        {
            var sel = New(root, "Decks Scroll view");
            sel.localPosition = Local3(root, DecksL, DecksT, DecksR, DecksB);

            var vp = New(sel, "Viewport");
            vp.localPosition = Local3(sel, DecksVpL, DecksVpT, DecksVpR, DecksVpB);
            var view = new PxRect(DecksVpL, DecksVpT, DecksVpR, DecksVpB);

            var content = New(vp, "Content");                     // 格容器（原版 `GridLayoutGroup`；出厂高 0）
            content.localPosition = Local3(vp, DecksCL, DecksVpT, DecksCR, DecksVpT);

            int n = FilteredDeckCount();
            int rows = Mathf.Max(1, (n + DeckCols - 1) / DeckCols);
            DeckScroll = MenuScroll.TopAligned(view, GridContentH(rows, DeckCellH, DeckGapY, 0f, 0f));
            DeckScroll.Owner = gameObject;
            DeckScroll.OnChanged = () => RebuildDeckRows(content);
            PointerLayer.RegisterScroll(DeckScroll);
            RebuildDeckRows(content);
        }

        Transform _deckHolder;
        void RebuildDeckRows(Transform holder)
        {
            _deckHolder = holder;
            for (int i = holder.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(holder.GetChild(i).gameObject);
            DeckRows.Clear();
            var view = new PxRect(DecksVpL, DecksVpT, DecksVpR, DecksVpB);
            int slot = 0;                                  // **格号**（筛掉的不占格）—— 与卡组下标 `i` 是两件事
            int total = CollectionData.DeckCount();
            for (int i = 0; i < total; i++)
            {
                var info = CollectionData.DeckAt(i);
                if (ArmyIndex >= 0 && _facs != null && ArmyIndex < _facs.Count && info.Faction != _facs[ArmyIndex])
                    continue;
                // 🔴 格位照原版 `GridLayoutGroup.SetCellsAlongAxis`：
                //   横 = `Content.x1 + startOffset + 列 × (格宽 + spacing.x)`（`startOffset` = 8.02 ⇒ 第 1 列 270.08）
                //   纵 = `Viewport.y1 + 行 × (格高 + spacing.y)`（**spacing.y 是 −21** ⇒ 行距 159）
                int col = slot % DeckCols, row = slot / DeckCols;
                float x1 = DeckCellX0 + col * (DeckCellW + DeckGapX);
                float y1 = DecksVpT + row * (DeckCellH + DeckGapY);
                var r = new PxRect(x1, y1, x1 + DeckCellW, y1 + DeckCellH);
                var rr = DeckScroll.Shift(r);
                slot++;
                // 🔴 **2026-10-07（A12①）**：同 `RebuildArmyCells` —— 内联的 `Inside(...)`（**完整**在视口里）
                //    收口成唯一那份求交（**与视口相交**）；判据 = 原版 `RectMask2D`（只裁、不判整格在不在）。
                if (!MenuDraw.Visible(rr, view)) continue;   // 整格在视口外 ⇒ 不建
                var cell = new GameObject("DeckRow_" + i);
                cell.transform.SetParent(holder, false);
                cell.transform.localPosition = Local3(holder, rr.x1, rr.y1, rr.x2, rr.y2);
                bool cur = i == DeckIndex;
                // 🔴 **2026-10-03 就地更正（A17 顺带查出的偏离）**：原来这里画的是**一块纯色**（我们自建），
                // 而**原版 `Practice Deck` 的根就是一张 `UI_Button_Mulligan`**（实测：324.5×80.1 Simple、
                // `trans=2` → HL `UI_Button_Mulligan_hover`；见普查 §块 5 第 22 行）。
                // ⚠️ **2026-10-07 更正（A77-㉒②，铁律 5）**：下面那行原注释写着「**选择态仍用我们原来那层色**
                //    （黄色 = 当前这套；**原版怎么标当前套本地判据不足**）」—— **后半句已被判据推翻**：
                //    原版 `Deck Selector Menu Item` 的当前套标记 = 它自己那颗 **`Highlight`** 子件
                //    （`DeckSelectorMenuItemDemo.SelectItem()` / `ToggleHighlight(bool)` → `highlightObject.SetActive`；
                //    每个格子 `Initialize(…, isSelected)` 里传的正是 false）。`Highlight` 那一层**本轮已建**（见下）。
                //    ⚠️ 这层黄**留着**：它上面还压着 `rowWb.Bind(rowBg, "UI_Button_Mulligan")` 的悬停换图，
                //    整层删掉是另一个决定 ⇒ **如实标：这一层色是我们挑的**（原版给的是 `Highlight` 那层，
                //    且「染色 = 你的卡组」那个语义原版用的是 `yourDeckColor`、落在 `Text background` 上，见下面 ⑤）。
                // 🆕 A9 尾巴：这一格也吃**软边**（原版 `Decks Scroll view/Viewport` 的 `m_Softness = (0,23)`）——
                //   压在带上/带下的格按剖面削 alpha（`MenuDraw.ApplySoftEdges` 的几何等效物）。
                var rowBg = Img(cell.transform, "UI_Button_Mulligan", rr.x1, rr.y1, rr.x2, rr.y2, "Row Bg", QPrRow,
                                false, null, view, DeckClipSoft);
                if (rowBg != null)
                    rowBg.SetTint(cur ? new Color(1f, 0.773f, 0f, 0.55f) : new Color(1f, 1f, 1f, 0.10f));

                // ============================================================ 🆕 2026-10-07（A77-㉒②）
                // **`Deck Selector Menu Item` 的五层内景** —— 兄弟序照原版（**从下到上**）：
                //   `Highlight` → `Button border` → `Main image` → `IsPlayerDeck` → `Text background`(+`Deck Name`)
                // 逐层的锚点五元组 / sprite / 贴图模式出处 → 常量段 `Item*` 那一大段注释。
                // ⚠️ 下面所有坐标 = **格左上角 + 该层的格内偏移**（`Item*` 常量），⛔ 别整体按 180/296 缩。
                float ix = rr.x1, iy = rr.y1;
                // ① `Highlight`（`UI_Deck_button_click`）—— **只有当前这套**在（原版那句 `SetActive(1/0)`）。
                //    ⚠️ 建出来再 `SetActive`（不是「选了才建」）—— 自检要能在**同一批节点**上断两态。
                var hl = Img(cell.transform, "UI_Deck_button_click",
                             ix + ItemHLL, iy + ItemHLT, ix + ItemHLR, iy + ItemHLB,
                             "Highlight", QPrItemHL, false, null, view, DeckClipSoft);
                if (hl != null) hl.gameObject.SetActive(cur);
                // ② `Button border`（`UI_Button_Round_background`，原版 `preserveAspect=1` ⇒ `keepAspect`）
                Img(cell.transform, "UI_Button_Round_background",
                    ix + ItemBdL, iy + ItemBdT, ix + ItemBdR, iy + ItemBdB,
                    "Button border", QPrItemBd, true, null, view, DeckClipSoft);
                // ③ `Main image` —— 原版**运行期喂** `ArmyUtilities.GetArmyIcon(army)`（我方卡组那一支）。
                //    ⚠️ 本窗只列玩家自己的卡组 ⇒ 走那一支；没有督军（阵营空串）时 `DeckRuntime.FactionIcon`
                //       落到它那个默认值（与 `Deck info/Army Image` 同一份映射，判据只那一处）。
                ImgTex(cell.transform, CardArt.MenuUi(DeckRuntime.FactionIcon(info.Faction)),
                       "`Deck Selector Menu Item/Main image` 的阵营图（" + DeckRuntime.FactionIcon(info.Faction) + "）",
                       ix, iy, ix + DeckCellW, iy + DeckCellH,
                       "Main image", QPrItemArt, false, null, view, DeckClipSoft);
                // ④ `IsPlayerDeck`（`Purity Seal_02`）—— `highlightIsPlayerDeck`，同样只对玩家卡组开 ⇒ 恒开
                Img(cell.transform, "Purity_Seal_02",
                    ix + ItemSealL, iy + ItemSealT, ix + ItemSealR, iy + ItemSealB,
                    "IsPlayerDeck", QPrItemSeal, false, null, view, DeckClipSoft);
                // ⑤ `Text background`（`40k_bt_underbutton`）+ 其下的 `Deck Name`。
                //    🔴 **色取 `yourDeckColor`**（`DeckSelectorMenuItemDemo.yourDeckColor` 序列化值 = `(1, 0.7951523, 0, 1)`）
                //       —— 我方卡组那一支运行期把这个色盖到 `textBackground` 上（`InitializeWithPlayerDeck`
                //       把 `+0x50/+0x58` 那对折成 `Color` 传给 `+0x48` 的 setter）；
                //       prefab 里 Image 自带的 `m_Color=(0.408,0.811,0.279,1)` 是**模板默认**、会被盖掉。
                Img(cell.transform, "40k_bt_underbutton",
                    ix + ItemNameBgL, iy + ItemNameBgT, ix + ItemNameBgR, iy + ItemNameBgB,
                    "Text background", QPrItemTxBg, false, ItemYourDeckColor, view, DeckClipSoft);
                // 文字与图**同一套**：原版 `RectMask2D` 对 TMP 一视同仁 ⇒ 走 `MenuDraw.ClipText` 同一个剖面
                // 🔴 **㉒③**：名字条 = **172×30.6**（照原版锚点现算）+ **原版 `fs29 / auto[8,29]` / 居中 / 折行 1**
                //    —— 原来是「顶 38px + 172 宽 + fs26 + **没有自适应**（我们挑的）」⇒ 长卡组名画到框外。
                var nm = Txt(cell.transform, info.Name,
                             ix + ItemNameL, ix + ItemNameR, iy + ItemNameT, iy + ItemNameB,
                             ItemNameFontPx, Align.Center, "Deck Name", QPrText, view, DeckClipSoft);
                if (nm != null)
                {
                    nm.SetAutoFitBox(LayoutSpace.Px(ItemNameR - ItemNameL),
                                     LayoutSpace.Px(ItemNameB - ItemNameT),
                                     ItemNameAutoMin, ItemNameAutoMax);
                    // 原版 `m_TextWrappingMode = 1 (Normal)`；`SetAutoFitBox` 内部已经开了折行，
                    // 这里**显式**再写一次（它现在是 `Label` 上唯一那个显式口，别让以后改公共件时静默回退）。
                    nm.SetWrapping(true);
                    // 🔴 **必须再推一次版面**：`SetAutoFitBox` 里那次 `RefreshBounds()` 会把 TMP 子节点
                    //    **挪一个位**，而 `ClippedTextGuard` 的重裁发生在**挪之前**的 `ForceMeshUpdate` 里
                    //    ⇒ 那一刀整体偏「新旧位置之差」（本窗实测约 (12, 5)px，**静默**）。
                    //    见 `RelayoutNow` 的注释：它重排一次（顺带在**挪到位之后**重裁）+ 再收一次尺寸。
                    RelayoutNow(nm);
                }
                int idx = i;
                var rowHit = HitOn(cell.transform, cell.transform, "Hit", rr, () => PickDeck(idx), QPrHit, view);
                var rowWb = rowHit != null ? rowHit.GetComponent<WindowButton>() : null;
                if (rowWb != null) rowWb.Bind(rowBg, "UI_Button_Mulligan");
                DeckRows.Add(cell.transform);
            }
            if (_txtArmy != null) _txtArmy.SetText(SelectedArmyName());

            // 🔴 **2026-10-07（A77-㉒④）**：**滚动范围跟着这一次重建重算**（原来只在 `BuildDeckRows` 里算一次）。
            //    `MenuScroll` 的可滚极值全部由 `ContentX1/ContentX2` 推（`MaxOffset = ContentX2 − 视口下沿`）——
            //    出厂按**全量**卡组数算过之后就不动了 ⇒ 点阵营格筛掉一批时，内容高**不跟着缩**，
            //    列表只剩一行却还能往下拖出一大段空白（老账，见 §㉒④）。
            //    判据 = 原版 `Content` 上常驻 `ContentSizeFitter(m_VerticalFit = 1 MinSize)` ⇒ 子件增删**每次都重算**；
            //    换阵营那一支（`PracticeModePopup__SetArmyButtons`）甚至是**整批 `Destroy` + 逐格 `Instantiate`**。
            //    ⚠️ 这里直接写 `Offset` 而**不**调 `SetOffset`：后者会回调 `OnChanged`（= **本函数**）⇒ 重建套重建。
            //      夹取算式与 `SetOffset` 逐字相同（`ClampLo/ClampHi` 就是那两个 `Mathf.Min/Max`）。
            if (DeckScroll != null)
            {
                int nRows = Mathf.Max(1, (slot + DeckCols - 1) / DeckCols);
                DeckScroll.ContentX1 = DecksVpT;
                DeckScroll.ContentX2 = DecksVpT + GridContentH(nRows, DeckCellH, DeckGapY, 0f, 0f);
                DeckScroll.Offset = Mathf.Clamp(DeckScroll.Offset, DeckScroll.ClampLo, DeckScroll.ClampHi);
            }
        }

        /// <summary>🆕 **2026-10-07（A77-㉒③/⑤）**：把一次「改完折行模式 / 改完自适应」的**版面真正推下去**。
        /// <para>🔴 **2026-10-08（A214②）就地退化成直调公共件**：实现已收进 `Battle/Label.cs` 的
        /// <see cref="Label.ForceRelayout"/>（A205 那一件把它从本函数原样收上去的）⇒ 这里只留**转调**。</para>
        /// <para>**先核过行为等价**（逐句对照，不是「看着像」）：
        /// · `_tmp != null` 时 —— 旧 = `SetFontSize(_tmp.fontSize)` + `RefreshBounds()`；
        ///   新 = `ForceRelayout()` 的**同样两句**（`Label.cs` 的实现一字不差）⇒ **逐位相同**。
        /// · `_tmp == null`（点阵后端）时 —— 旧 = 第一句在 `SetFontSize` 里**早退**、第二句 `RefreshBounds()`
        ///   在 `Label.cs:569` 也是 **`if (_tmp == null) return;`** ⇒ **两句都是空操作**；
        ///   新 = `ForceRelayout()` 第一句就 `return` ⇒ 同样是空操作。**两边都什么都不做。**</para>
        /// <para>⛔ **两份实现会分叉**（`CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」）：本函数原来是
        /// A205 收口后**残留的第二份**，删掉它才是收口完成。</para>
        /// ⚠️ 它顺带触发 `ClippedTextGuard` 的**重裁**（`RefreshBounds` 末句 `guard.Reclip()`）
        /// —— 这正是要它的另一半理由：重裁必须在 **`RefreshBounds()` 把 TMP 挪到位之后**才作数，
        /// 否则那一刀整体偏「新旧位置之差」（本窗实测约 **(12, 5)px**，而且**静默**）。
        /// <para>顺序固定：`SetAutoFitBox` → `SetWrapping` → **本函数**。⛔ 别调两次以上。</para></summary>
        static void RelayoutNow(Label lb)
        {
            if (lb == null) return;
            lb.ForceRelayout();              // 公共件里唯一那一份（值同名 ⇒ 幂等，见它的头）
        }

        /// <summary>🆕 **2026-10-07（A77-㉒④ 的自检口）**：按**当前**的卡组库/阵营筛选**重建卡组列表**。
        /// 🔴 为什么要有它：列表长在窗里，**换了数据不重建就还是旧的那一批**（`OnChanged` 只在滚动时响）——
        ///    而自检要用另一份临时存档（9 套 / 指定阵营）把「内容高跟着筛选重算」和「格底名字条压进渐隐带」
        ///    这两条逼出来。⛔ 运行期别调它（换数据一律走 `CollectionData` 那几条正规通道 + `OnChanged`）。</summary>
        public void RebuildDeckListForTest()
        {
            if (_deckHolder != null) RebuildDeckRows(_deckHolder);
        }

        /// <summary>自检用：卡组列表**内容高**（`MenuScroll` 的 `ContentX2 − ContentX1`，画布 px）。
        /// 判据 = 原版 `Content` 上那个 `ContentSizeFitter(m_VerticalFit = MinSize)` 撑出来的高。</summary>
        public float DeckScrollContentH
        {
            get { return DeckScroll != null ? DeckScroll.ContentX2 - DeckScroll.ContentX1 : 0f; }
        }

        // 🔴 **2026-10-07（A12①）这里原来有个 `static bool Inside(PxRect view, PxRect r)`**
        //    （「矩形**完整**落在视口里没有」）—— 本窗两处构建循环都在用它，而它与
        //    `MenuScroll.Intersects`（只判滚动轴）、`MenuDraw.Visible`（两轴求交）**并列成了第三套判据**。
        //    已删：两处改调 `MenuDraw.Visible`（唯一那一份求交），语义从「**完整**在视口里」变成
        //    「**与视口相交**」（判据 = 原版 `RectMask2D` 只裁不判整格）；命中区同批截到视口内
        //    （`HitOn` 的 `clip`）—— 逐处登记在该两处调用点的注释里。
        //    ⚠️ 当时写 `Inside` 的理由是「`GameWindow`（本窗基类）**没有 `Clip`**」—— 那条**已过期**：
        //    这两个格子里的每一颗图/字**自己**就把 `view` 当 `clip` 传（`Img(…, view, …)` /
        //    `Txt(…, view, …)`，2026-10-04 的 A9 尾巴那批加的）⇒ 部分可见的格本来就画得出、也裁得对。
        //    ⚠️ 旧实现自带 **0.5px 余量**（`±0.5f`），新判据没有 —— 差在「离视口 0.5px 以内」那一档，
        //    那一档本来就在框外看不见，别当回归。
        //    🔴 **`view` 的口径没变**（2026-10-05 起）：一律是**原版 `Viewport` 的矩形**（`ArmView*` / `DecksVp*`），
        //    不是 `Army Selector` / `Decks Scroll view` 自己的矩形 —— 见那两组常量的注释。

        int FilteredDeckCount()
        {
            if (ArmyIndex < 0 || _facs == null || ArmyIndex >= _facs.Count) return CollectionData.DeckCount();
            int n = 0;
            for (int i = 0; i < CollectionData.DeckCount(); i++)
                if (CollectionData.DeckAt(i).Faction == _facs[ArmyIndex]) n++;
            return n;
        }

        string SelectedArmyName()
        {
            if (ArmyIndex >= 0 && _facs != null && ArmyIndex < _facs.Count)
            {
                var c = CollectionWindow.CardsState;
                return _facs[ArmyIndex].ToUpperInvariant();
            }
            var d = CollectionData.DeckAt(DeckIndex);
            return string.IsNullOrEmpty(d.Faction) ? "（未选阵营）" : d.Faction.ToUpperInvariant();
        }

        /// <summary>开 `Deck Selection Popup with Tabs`（原版 `DeckGeneralInfoDemo.ChangePlayerDeckButton` 那条）。</summary>
        public DeckSelectionPopup OpenDeckSelection()
        {
            LastDeckSelection = null;
            if (Manager == null)
            {
                Debug.LogWarning("[Practice] 没有 `WindowsManager`，开不了 `Deck Selection Popup`");
                return null;
            }
            var w = DeckSelectionPopup.Create(Manager, pick =>
            {
                // 回调 = 选中那一套（原版 `DeckSelectionPopup.Select` 的两步：关窗 + 回调）。
                // 🔴 原版的回调是 `Action<CardDeck>`，**预组与自己的卡组走同一条**；我们这边必须分开 ——
                //    预组**不在** `DeckLibrary` 里，拿名字回查 `CollectionData.IndexOf` 必然给 −1，
                //    那就是**静默无事发生**（撞红线）。判据 → `资料/预组卡组_原版规格.md` §五之二 末。
                if (pick.Prebuilt)
                {
                    PickPrebuilt(pick.PrebuiltDeck);
                    return;
                }
                int idx = CollectionData.IndexOf(pick.Info.Name);
                if (idx >= 0) PickDeck(idx);
            }, (int)GameMode.Classic);   // 🆕 2026-09-26：练习 = 经典 ⇒「我的卡组」页只列经典那批
            // ⚠️ 原版练习窗里有个 `Game mode` 开关（经典/冲突）我们**没建**（见本文件头那条 `NotBuilt`），
            //    所以这里**写死经典**；真要支持「练习里也能打遭遇」得先把那个开关做出来。
            Manager.OpenWindow(w);
            LastDeckSelection = w;
            Debug.Log("[Practice] 开 `Deck Selection Popup with Tabs`");
            return w;
        }

        /// <summary>最近一次开出来的选卡组窗（自检用）。</summary>
        public static DeckSelectionPopup LastDeckSelection;

        Transform _cardHolder;

        /// <summary>建（或**重建**）卡列表 —— 挂在 `Deck info/Deck List Drawer` 下（原版那一件的真父是 `Deck info`）。
        /// 🔴 重建前**先把旧的那个销毁** —— 否则每换一次卡组就多留一棵孤儿树。
        ///
        /// **格原点** = `Content` 左上角 + `GridLayoutGroup` 的 pad **(22, 4)**：
        /// 第一格左上 = **(1261.27 + 22, 318.42 + 4) = (1283.27, 322.42)**。
        /// 我们原来起在 `Deck List Drawer` 自己的左上角 (1240.38, 207.65) —— 正好压在
        /// `Deck Name`(224.13→271.82) 与 `Warlord Name`(269.62→303.62) 上（第 53 条那条「字叠成一团」）。</summary>
        void BuildCardRows(Transform parent)
        {
            var old = parent.Find("Deck List Drawer");
            if (old != null) RewardsWindow.DestroySafe(old.gameObject);
            CardRows.Clear();
            _cardHolder = new GameObject("Deck List Drawer").transform;
            _cardHolder.SetParent(parent, false);
            _cardHolder.localPosition = Local3(parent, DlL, DlT, DlR, DlB);
            _deckList = _cardHolder;

            // 抽屉自己那颗钮（原版 `Show Deck General Info button`：`UI_Button_Round_background` + `40k_UI_bt_back`）
            //   ⇒ 原版 `deckGeneralInfoButton → DeckGeneralInfoButtonOnClick`：`General container.SetActive(true)`
            //     + `Deck List Drawer.SetActive(false)`（**切回总览**）。
            Img(_cardHolder, "UI_Button_Round_background", ShowBtnL, ShowBtnT, ShowBtnR, ShowBtnB, "Show Info Bg", QPrRow, true);
            Img(_cardHolder, "40k_UI_bt_back", 1737.68f, 246.46f, 1785.07f, 293.12f, "Show Info Icon", QPrRow, true);
            HitOn(_cardHolder, _cardHolder, "ShowInfoHit", new PxRect(ShowBtnL, ShowBtnT, ShowBtnR, ShowBtnB),
                  () => Toggle(true));

            // 原版 `Content` 是布局组节点；我们的 `Deck List Drawer` 的 localPosition 已经把它摆到 (1240.38,207.65)，
            // 而 `Content` 的绝对矩形是 1261.27,318.42→1787.37,793.87 ⇒ 子件的坐标**一律用页面绝对 px**，
            // `Local3(_cardHolder, …)` 会自己换算（这是本工程画图小工具的约定）。
            var deck = CollectionData.Raw(DeckIndex);
            if (deck == null) return;
            var order = new List<string>();
            if (!string.IsNullOrEmpty(deck.WarlordId)) order.Add(deck.WarlordId);
            if (deck.CardIds != null)
                foreach (var id in deck.CardIds)
                    if (!string.IsNullOrEmpty(id) && !order.Contains(id)) order.Add(id);

            float oL = DlContentL + DlPadL, oT = DlContentT + DlPadT;
            int cols = ListCols;
            for (int i = 0; i < order.Count; i++)
            {
                var card = CollectionData.Card(order[i]);
                if (card == null) continue;
                int c = i % cols, rr = i / cols;
                float x1 = oL + c * (DlCellW + DlGapX);
                float y1 = oT + rr * (DlCellH + DlGapY);
                var cell = new GameObject("CardRow_" + i);
                cell.transform.SetParent(_cardHolder, false);
                cell.transform.localPosition = Local3(_cardHolder, x1, y1, x1 + DlCellW, y1 + DlCellH);
                var nm = Txt(cell.transform, card.Name, x1, x1 + DlCellW, y1, y1 + DlCellH, 20f, Align.Left,
                             "Name", QPrText);
                // ⚠️ 卡名会超过 **231px** 的格宽（实测 "Death Spinner Warp Spider" 会撞进右边那一列）
                //    ⇒ 照原版那套开**自适应字号**把它缩进格子里。
                // 🔴 **2026-10-07（A77-⑤）就地更正（铁律 5）**：这行上面的原注释写着
                //    「原版 `UICardInfoItem` 内部怎么排**没查**」—— **现在查了**，判据如下：
                //      出处 = `python 工具/menu_dump.py bundle_menus_assets_all "Deck Selector Card Info button"
                //             --depth 5 --md`（2026-10-07 现读；那一件就是卡列表格子的原版 prefab，
                //             挂 `UICardInfoItem`，`Deck List Drawer/Content` 里逐格实例化的就是它）
                //      真值 = `Content/Text fill/Card Name`：`m_fontSize = **38**` ·
                //             `m_fontSizeMin/Max = **2 / 38**`（`auto[2.0~38.0]`）· 对齐 `Left/Capline` ·
                //             **`m_TextWrappingMode = 0`（不折行）** · 色 `(0.934,0.934,0.934,1)`。
                //      ⇒ 我们原来传的 **`min 10 / max 20`** 是**真偏离**（上下界都不是原版那两个数）；
                //        折行那半也**没显式关**（`SetAutoFitBox` 内部会无条件开折行，见 `Label.SetWrapping` 的头）。
                //      ⚠️ 两个口径都写清：`minPx/maxPx` 传的是**原版那两个字段的原文**（2 / 38），
                //        而**设计字号仍是 `Txt` 那个 20**（框 = 我们的格 231×27.88）—— `maxPx(38) > 调用方 20`
                //        时 TMP 会**往上长到装不下为止**（`TextMeshPro.cs:2151` 起点 = `Clamp(base,min,max)`；
                //        天花板 = `fontSizeMax`；「长高/长宽到溢出就回退」那两支在 `TextMeshPro.cs:3386/3575`）
                //        —— 这正是原版那一档（它的 `m_fontSize` 也是 38）。
                //      🔴 **没查清 / 仍是偏离的一处（如实标）**：原版这颗 `Card Name` 的**框**是它父件
                //        `Text fill`（锚 `(0.13,0.07)→(0.99,0.93)` · sizeDelta `(−5,0)`）⇒ 高 **23.98**；
                //        而 `Text fill` 宽由 `HorizontalLayoutGroup` 现算、`menu_dump` 打的是 `unk`
                //        （首选宽要 Unity 的字体度量）⇒ **宽度查不到**。我们这边没有 `Text fill` 这一层，
                //        框直接取**整格 231×27.88** ⇒ 收敛出来的字号会比原版**略大**（高多 3.9px）。
                //        ⛔ 这不是「原版就是这样」，是**我们知道的一处差别**，记在这儿等判据齐了再收。
                if (nm != null)
                {
                    nm.SetAutoFitBox(LayoutSpace.Px(DlCellW), LayoutSpace.Px(DlCellH),
                                     CardNameAutoMin, CardNameAutoMax);
                    // 原版 `Card Name` 是 `m_TextWrappingMode = 0`（**不折行**）—— 必须在 `SetAutoFitBox`
                    // **之后**调（那个函数内部无条件走 `SetWrapWidth` ⇒ 会把模式开成 `Normal`）。
                    nm.SetWrapping(false);
                    // 🔴 **再推一次版面**：`textWrappingMode` 的 setter 只 `SetVerticesDirty()`，批处理里
                    //    **没有帧循环** ⇒ mesh 还停在**折行**那一版（字段说不折行、画面却折着）——
                    //    见 `RelayoutNow` 的注释（顺带让「渲出来多宽」= 不折行时多宽，那条宽度断言才有鉴别力）。
                    RelayoutNow(nm);
                }
                CardRows.Add(cell.transform);
            }
        }

        // ============================================================ 交互

        void PickArmy(int i)
        {
            ArmyIndex = (ArmyIndex == i) ? -1 : i;
            if (DeckScroll != null) DeckScroll.SetOffset(0f);
            if (_deckHolder != null) RebuildDeckRows(_deckHolder);
            Debug.Log("[Practice] 阵营筛选：" + (ArmyIndex < 0 ? "不限" : _facs[ArmyIndex]));
        }

        void PickDeck(int i)
        {
            DeckIndex = i;
            PickedPrebuilt = null;      // 选了「我的卡组」⇒ 预组那次选中作废（两个是互斥的）
            PrebuiltDecks.ClearPendingBattleDeck();
            if (_deckHolder != null) RebuildDeckRows(_deckHolder);
            bool wasList = !ShowingGeneralInfo;             // 换卡组时**别把抽屉状态翻掉**
            BuildCardRows(_info);                          // 重建卡列表（卡组换了）
            Toggle(wasList ? false : true);
            var info = CollectionData.DeckAt(i);
            if (_txtDeckName != null) _txtDeckName.SetText(info.Name);
            var wl = CollectionData.Warlord(i);
            if (_txtWarlord != null) _txtWarlord.SetText(wl != null ? wl.Name : "未选督军");
            if (_txtArmy != null) _txtArmy.SetText(SelectedArmyName());
            if (_armyIcon != null) _armyIcon.SetTexture(CardArt.MenuUi(DeckRuntime.FactionIcon(info.Faction)));
            if (_cardback != null) _cardback.SetTexture(CardArt.DeckCardback(info.CardbackId, info.Faction));
            Debug.Log("[Practice] 选中卡组：「" + info.Name + "」");
        }

        /// <summary>
        /// 选中一副**预组卡组**（原版这条是「登记」型：`DeckGeneralInfoDemo.DeckChanged` → 记下用哪副牌，不立刻开战）。
        ///
        /// 我们这边多做一件：把它写进 `PrebuiltDecks` 的**「本局用这副牌」**通道 —— 因为
        /// `StartBotBattle` 原先只认 `DeckLibrary.Current`，而**预组不在玩家的卡组库里**。
        /// </summary>
        public void PickPrebuilt(PrebuiltDecks.Deck d)
        {
            if (d == null) return;
            PickedPrebuilt = d;
            PrebuiltDecks.SetPendingBattleDeck(d);
            // 面板上要显示「选的是哪副」—— 卡列表那是「我的卡组」的，这里只把身份显示对
            if (_txtDeckName != null) _txtDeckName.SetText(d.DisplayName);
            if (_txtWarlord != null)
            {
                var wl = CollectionData.Card(d.heroId);
                _txtWarlord.SetText(wl != null ? wl.Name : "未选督军");
            }
            if (_armyIcon != null) _armyIcon.SetTexture(CardArt.MenuUi(d.FactionIcon));
            if (_cardback != null) _cardback.SetTexture(d.Cardback);
            Debug.Log("[Practice] 选中**预组卡组**「" + d.DisplayName + "」(" + d.deckId + ") ⇒ 本局就用它"
                      + "（防御卡 = 我们补的那张「" + d.defensiveNameZh + "」" + d.defensiveId + "）");
        }

        // ============================================================ 🆕 2026-10-03：`Deck info Popup` 那条「练习对手」链

        /// <summary>原版 `DeckInfoPopup.StartPracticeMatch(自己的卡组, 对手卡组)` 的落地：
        /// **开练习窗（= 我们这条开战链的宿主）⇒ 面板对齐到自己那副 ⇒ 记下对手那副 ⇒ 立刻开打**。
        ///
        /// ⚠️ **与原版的两处差别，如实标**：
        ///   ① 原版**不开这扇窗** —— 它 `ShowPopUp(等待窗)` 之后就 `StartMatch`。我们的 12 秒搜索 + 打 bot
        ///      这条链**长在这扇窗上**（`StartBattle` / `SearchingMatchPopup`），所以拿它当宿主；
        ///      玩家看到的差别 = 背后多一扇练习窗（等待窗照旧在最上面）。
        ///   ② 原版是**联机匹配**（`MatchMakerManager`），我们是**打 bot** —— 见文件头。
        /// </summary>
        /// <param name="ownDeckIndex">**自己**那副 = `Deck info Popup` 里那一副（原版 `context.Deck` → `playerDeck`）。</param>
        /// <param name="opponent">**对手**那副 = 选卡组窗回调回来的（原版回调形参 → `enemyDeck`）。</param>
        public static PracticeModePopup StartPracticeMatch(WindowsManager mgr, int ownDeckIndex,
                                                          DeckSelectionPopup.DeckPick opponent)
        {
            if (mgr == null) { Debug.LogWarning("[Practice] 没有 `WindowsManager` ⇒ 开不了练习赛"); return null; }
            var w = Create(mgr);
            mgr.OpenWindow(w);                       // `Open()` 会把 `DeckIndex` 重置成 `DeckLibrary.Current` ⇒ 下面再对齐
            w.PickDeck(ownDeckIndex);                // 面板/卡列表跟着走（原版开这扇窗时也是拿这副铺的）
            w.OpponentDeck = PlayerDeckOf(opponent);
            Debug.Log("[Practice] 练习赛：**我** = 「" + CollectionData.DeckAt(ownDeckIndex).Name + "」（原版 `playerDeck`）"
                      + " · **对手** = 「" + (w.OpponentDeck != null ? w.OpponentDeck.Name : "（没有）") + "」（原版 `enemyDeck`）");
            if (w.OpponentDeck == null)
                Debug.LogWarning("[Practice] ⚠️ 选中的那副**搓不出 `PlayerDeck`**（预组数据读不到？）⇒ 对手退回自动凑，出声");
            w.StartBattle();                         // 原版 `StartPracticeMatch` 也是选完就直接开打
            return w;
        }

        /// <summary>把选卡组窗交出来的那一副搓成 <see cref="PlayerDeck"/>（原版这一步是 `CardDeck`，不分预组/自建）。
        /// 🔴 **判据只此一份**：预组走 `PrebuiltDecks.ToPlayerDeck`（含我们补的防御卡），自建走卡组库那一份。</summary>
        public static PlayerDeck PlayerDeckOf(DeckSelectionPopup.DeckPick pick)
        {
            if (pick.Prebuilt) return PrebuiltDecks.ToPlayerDeck(pick.PrebuiltDeck);
            return CollectionData.Raw(pick.OwnIndex);
        }

        // ------------------------------------------------------------ 「本局对手」通道（跨场景，读一次就清）
        //
        // 🔴 **为什么要有它**：我们的开战是「壳里 `LoadScene("Battle")` → `BattleDriver.Start()` →
        //    `BeginFromDeckLibrary()`」，两段之间只有**静态字段**过得去（同 `PrebuiltDecks._pending`）。
        //    原版没有这条：`MatchMakerManager.StartMatch` 的 `enemyDeck` 是形参，直接带到对局里。
        // ⚠️ **读点还没接**（在 `Battle/BattleDriver.cs`，不在本轮白名单）—— 见文件头那条缺口。

        static PlayerDeck _pendingOpponent;

        /// <summary>待读的「本局对手」（自检用；`null` = 没指定）。</summary>
        public static PlayerDeck PendingOpponent { get { return _pendingOpponent; } }

        /// <summary>开战前把对手那副放这儿。</summary>
        public static void SetPendingOpponentDeck(PlayerDeck d) { _pendingOpponent = d; }

        /// <summary>开局读一次（**读完就清** —— 下一局不该还带着它）。没有给 null。</summary>
        public static PlayerDeck TakePendingOpponentDeck()
        {
            var d = _pendingOpponent;
            _pendingOpponent = null;
            return d;
        }

        /// <summary>作废（联机局 / 没指定对手时）。</summary>
        public static void ClearPendingOpponentDeck() { _pendingOpponent = null; }

        // ------------------------------------------------------------ 开打前的「隐藏卡」前置检查

        /// <summary>自检用：把这**同一个对象**当成「带隐藏卡的卡组」（null = 不干预）。
        /// ⚠️ 为什么要这个口子：我们的卡数据里**没有** `IsHidden` 这个字段（见下），
        ///    不注入的话那条分支**永远走不到**（= 断言等于没查）。</summary>
        public static PlayerDeck ForceHiddenCardsDeck;

        static bool _hiddenWarned;

        /// <summary>
        /// 原版 `GameStaticData.CheckHiddenCardsInDeck(playerDeck)`（判据 = `d:/2/tools/decomp_full/GameStaticData__CheckHiddenCardsInDeck.c`）：
        /// **督军是隐藏卡，或 `cardLibrary` 里有任何一张是隐藏卡** ⇒ 返回真 ⇒ 弹提示、**不开打**。
        /// 「隐藏」的判据 = `PlayerItem.IsHidden()`（`RawCardScript : PlayerItem`，签名桩 `PlayerItem.cs:41`）。
        ///
        /// 🔴 **我们这边判据是空的**（铁律 11 的第 ① 种，不是「挑着不做」）：
        ///   · `d:/2/tools/decomp_full/` 里**没有 `RawCardScript__IsHidden.c`**，ILSpy 签名桩里
        ///     `RawCardScript` 也**没有** `IsHidden` 覆写 ⇒ 这个 build 里恒走 `PlayerItem.IsHidden()` 的基实现；
        ///   · 我们的卡数据（`cards_engine.json` / `CardDef`）里也**没有**任何「隐藏」字段 ——
        ///     实测 `数据/游戏数据/card_stats.json` 里 "Hidden" 只有 **5 处、全是卡名 `Hidden Hunters`**。
        ///   ⇒ 今天这条检查**不会触发**。**不假装它能触发**：第一次调用时用 `LogWarning` 把这件事说清楚，
        ///     并把**分支**留着（判据一旦有了，只改这一个函数）。
        /// </summary>
        public static bool HasHiddenCards(PlayerDeck deck, out string why)
        {
            why = "";
            if (deck == null) return false;
            if (ForceHiddenCardsDeck != null && ReferenceEquals(deck, ForceHiddenCardsDeck))
            {
                why = "（**自检注入**：这一副被 `ForceHiddenCardsDeck` 指定成「带隐藏卡」）";
                return true;
            }
            if (!_hiddenWarned)
            {
                _hiddenWarned = true;
                Debug.LogWarning("[Practice] ⚠️ 开打前的「隐藏卡」检查（原版 `GameStaticData.CheckHiddenCardsInDeck`）"
                                 + "**在我们的数据上恒为假**：原版的判据是 `PlayerItem.IsHidden()`，"
                                 + "而这个 build 的反编译里 `RawCardScript` 没有覆写它（`PlayerItem` 基实现恒 false），"
                                 + "我们的 `CardDef` 里也没有任何「隐藏」字段 ⇒ **这条检查今天不会触发**。"
                                 + "**如实说明，不是静默**；判据一旦有了，改 `PracticeModePopup.HasHiddenCards` 这一处。");
            }
            return false;
        }

        /// <summary>点 `Battle!` —— 照原版 `PracticeModePopup__BattleButtonOnClick → MatchMakerManager.StartMatch`：
        /// **先开 `Searching Oponent Popup` 等 12 秒**（离线时「不能匹配真人」那一支的常量，见 `SearchingMatchPopup`），
        /// 等不到真人再打 bot。真正的开战在 `StartBotBattle`。</summary>
        public void StartBattle()
        {
            var info = CollectionData.DeckAt(DeckIndex);
            if (string.IsNullOrEmpty(info.WarlordId))
            {
                Debug.LogWarning("[Practice] 这套卡组**没有督军**，开不了局 —— 如实说，不静默。");
                if (Manager != null) Manager.ShowPopUp("这套卡组还没有选督军，开不了局。", "知道了", null);
                return;
            }
            // 🆕 2026-09-26（N3）：**联机已连上 ⇒ 走 P2P，不跑那 12 秒 bot 链**
            //    （判据 → `资料/联机P2P_设计与交接.md` §六 N3）。没接管时照旧 —— 单机行为一字不改。
            {
                var pre0 = PrebuiltDecks.PendingSource;
                var pd = pre0 != null ? PrebuiltDecks.ToPlayerDeck(pre0) : CollectionData.Raw(DeckIndex);
                if (NetMatchmaking.TryStart(pd, "Classic",
                                            pre0 != null ? pre0.faction : info.Faction, out string netWhy))
                {
                    Debug.Log($"[Practice] 这一局走**联机**（本机交了卡组「{info.Name}」）—— 不跑 12 秒 bot 链");
                    // 🆕 2026-10-03：**对手由对面决定** ⇒ `Practice Deck` 那条链指的那一副作废
                    //    （原版那一支也是另一条路：`MatchMakerManager` 匹配到真人就轮不到 bot 的 `enemyDeck`）。
                    if (PendingOpponent != null)
                        Debug.Log("[Practice] 联机局：**对手卡组由对面决定** ⇒ 清掉「本局对手」通道里那一副"
                                  + "（原版匹配到真人时 `MatchMakerManager` 也走不到 bot 那条 `enemyDeck`）");
                    ClearPendingOpponentDeck();
                    // 🆕 2026-10-03（A2）：**把「在等对面」显示出来** —— 原来是屏幕上什么都没有。
                    //    练习窗是 `currentWindow` ⇒ 用**窗内**那扇 `Searching Oponent Popup`（不开全屏那扇）。
                    NetTookOver = true;
                    if (_search == null) AttachSearch();
                    _search.OnCancel = CancelSearch;
                    _search.BeginNetWait();
                    return;
                }
                Debug.Log($"[Practice] 联机没接管（{netWhy}）⇒ 照旧走「等 12 秒再打 bot」那条链");
                // 🔴 **红线**（`项目任务.md` §三 第 14 条 表 第 5 条）：配了联机却没连上要说一声。
                //    ⚠️ 判定「该不该说」在那一处（单机玩家不打扰）—— 别在这儿再写一遍。
                NetMatchmaking.ExplainNotTakingOver(netWhy);
            }
            if (_search == null) AttachSearch();
            _search.OnSearchDone = StartBotBattle;
            _search.BeginSearch();
        }

        /// <summary>建窗内那扇 `Searching Oponent Popup`（出厂关着）并把取消接到 `CancelSearch`。</summary>
        void AttachSearch()
        {
            _search = SearchingMatchPopup.Attach(transform, "Searching Oponent Popup");
            _search.OnCancel = CancelSearch;
        }

        /// <summary>🆕 2026-10-03（A1/A2）：这一局**交给联机了**（`StartBattle` 里 `TryStart` 返回真）。</summary>
        public bool NetTookOver { get; private set; }

        /// <summary>取消匹配（窗内那扇 `Cancel` / 点背板 / ESC 都走它）。
        /// 🆕 2026-10-03：**联机那一支要真拆局**（原来只关窗 ⇒ 对面照样把你拉进战场）；
        /// ⚠️ **这条链是我们设计的、不是复刻** —— 判据 → `NetMatchmaking.Cancel`。</summary>
        public void CancelSearch()
        {
            Debug.Log("[Practice] 取消匹配（原版 `MatchMakerManager.CancelSearch`）");
            if (!NetTookOver) return;
            if (NetMatchmaking.Cancel("对局发起方点了取消", out string why))
            {
                NetTookOver = false;
                NetRuntime.Notice("已经取消这一局的联机匹配 —— 对面会收到通知，**双方都没有开局**。\n"
                                + "想再打一次：两边各自重新点一次 `Battle!`。");
            }
            else
            {
                NetRuntime.Notice("取消不了这一局：" + why);   // **不假装取消成功**
            }
        }

        /// <summary>推进匹配（`Update` 与自检都走它 —— 批处理没有帧循环）。</summary>
        public void TickSearch(float dt) { if (_search != null) _search.Tick(dt); }

        /// <summary>现在是不是正在「等对手」那 12 秒里（自检用；**不是** `StartedBattle`）。</summary>
        public bool SearchingMatch { get { return _search != null && _search.Searching; } }

        void Update() { TickSearch(Time.deltaTime); }

        SearchingMatchPopup _search;

        /// <summary>等满 12 秒 ⇒ 选定卡组 + 切该阵营那份对战场景（= 原版 `StartBattle → LoadScene` 的等价物）。</summary>
        public void StartBotBattle()
        {
            var info = CollectionData.DeckAt(DeckIndex);
            // 🔴 **本局用哪副牌**：在选卡组窗里挑过**预组**就走预组那条（`PrebuiltDecks` 那条通道），
            //    否则照旧用「我的卡组」（`DeckLibrary.Current`）。两条路都经过 `BattleDriver.Begin(myDeck:)`。
            var pre = PrebuiltDecks.PendingSource;
            string faction = pre != null ? pre.faction : info.Faction;
            string deckName = pre != null ? pre.DisplayName : info.Name;
            if (pre != null)
            {
                Debug.Log("[Practice] 本局用**预组卡组**「" + deckName + "」(" + pre.deckId + ")"
                          + "（防御卡用我们补的「" + pre.defensiveNameZh + "」" + pre.defensiveId
                          + "；原版预组那份是 null，见 `资料/预组卡组_原版规格.md` §五之七）");
            }
            else
            {
                CollectionData.Select(DeckIndex);  // `BattleDriver.PickSavedDeck` 读的就是 `DeckLibrary.Current`
            }
            // 🆕 2026-10-03：**本局对手**（`Deck info Popup` 的 `Practice Deck` 那条链指定过才有）。
            //   原版它是 `MatchMakerManager.StartMatch(…, enemyDeck)` 的**形参**，直接带进对局；
            //   我们这条链跨场景 ⇒ 只能走静态通道（同 `PrebuiltDecks` 那条），**开局读一次就清**。
            //   ✅ **2026-10-04 收口**：读点**已经写上**了 —— `Battle/BattleDriver.cs` 的 `BeginFromDeckLibrary()` 里
            //      `Begin(…, foeDeck: PracticeModePopup.TakePendingOpponentDeck(), …)`。这一段**现在真的接进对局了**。
            //   ⚠️ **原来的注释在这里写「🔴 读点还没写／别当已经接进对局了」—— 已过期**（代码先落地、注释后没跟上，
            //      审查 2026-10-04 抓到的）。**留着更正痕**：下一个会话别再照着旧话去"补"一行已经存在的代码。
            //   没有指定对手 ⇒ **清掉**（上一条链留下的不该跟到这一局）。
            if (OpponentDeck != null)
            {
                SetPendingOpponentDeck(OpponentDeck);
                // 🆕 2026-10-04：**读点已经接上了**（`BattleDriver.BeginFromDeckLibrary` 里那一句 `foeDeck: …TakePendingOpponentDeck()`）
                //     ⇒ 这里**不再报警告**（原来那条写「读它的那一行还没写 ⇒ 这一局实际打的是自动凑的对手」——
                //     **是假话、而且每次开练习赛都会往日志里打一遍**，审查 2026-10-04 抓到）。
                //     改成一个**普通的说明日志**，把「这一局对手是谁」如实打出来，方便对着日志核。
                Debug.Log("[Practice] 本局**对手** = 「" + OpponentDeck.Name + "」（原版 `StartMatch(…, enemyDeck)` 那一副）"
                          + " ⇒ 已放进「本局对手」通道，由 `BattleDriver.BeginFromDeckLibrary` 开局时读一次（读完就清）");
            }
            else ClearPendingOpponentDeck();
            StartedBattle = true;
            // 🔴 2026-09-30（§27）：`BattleSceneNameFor` 现在恒为 `Battle`；
            //    「哪一场」由运行时按 `SceneFor(faction)` 取 prefab（见 `ArenaRuntimeLoader`）。
            var scene = ArenaByArmy.BattleSceneNameFor(faction);
            Debug.Log("[Practice] 开战：「" + deckName + "」→ 切 `" + scene + ".unity`"
                      + "（原版走 `StartMatch → StartBotBattle → StartBattle → LoadScene`，唯一 LoadScene 点；"
                      + " 照原版查表，督军阵营「" + faction + "」该去 `" + ArenaByArmy.OriginalNameFor(faction)
                      + "`（我们的键 `" + ArenaByArmy.SceneFor(faction) + "`）"
                      + (scene == "Battle"
                         ? " —— ⚠️ **该场那份场景还没建，这一局用的是兜底 `Battle`（战场 = " + ArenaByArmy.DefaultScene + "）**"
                         : "）"));
            if (Application.isBatchMode) { Debug.Log("[Practice] （批处理：不切场景，只记账）"); return; }
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
        }

        void NotBuilt(string what)
        {
            Debug.LogWarning("[Practice] `" + what + "` 还没实现（**出声**，见 `Shell/PracticeModePopup.cs` 文件头「没建的」）");
        }

        // ============================================================ 画图小工具（同 `DeckInfoPopup` 那一套）
        // ⚠️ **坐标一律页面绝对 px；`parent` 与 `basis` 给同一个节点**（第三种错法见 `已知的坑.md`）。

        enum Align { Center, Left, Right }

        static Vector3 Local3(Transform basis, float x1, float y1, float x2, float y2)
            => LayoutSpace.RectCenter(x1, y1, x2, y2) - basis.position;

        void Solid(Transform parent, float x1, float y1, float x2, float y2, Color color, int q, string name)
        {
            var quad = ImageQuad.Create(parent, CardArt.Solid(), Local3(parent, x1, y1, x2, y2),
                                        LayoutSpace.Px(y2 - y1), new Vector2(0.5f, 0.5f), name);
            if (quad == null) return;
            quad.SetAspect((x2 - x1) / Mathf.Max(1e-6f, y2 - y1));
            quad.SetTint(color); quad.SetRenderQueue(q);
        }

        ImageQuad Img(Transform parent, string art, float x1, float y1, float x2, float y2,
                      string name, int q, bool keepAspect, Color? tint = null,
                      PxRect? clip = null, Vector2 clipSoftness = default(Vector2))
            => ImgTex(parent, CardArt.MenuUi(art), art, x1, y1, x2, y2, name, q, keepAspect, tint, clip, clipSoftness);

        /// <summary>同 `Img`，但**直接给图**（原版不少件的图是运行时喂的，比如 `Army Image` / `Cardback`）。
        /// 🆕 A9 尾巴：多了 `clip` / `clipSoftness`（原版 `RectMask2D` 的硬裁 + `m_Softness`）——
        /// **只在真正建出 quad 之后**才切软边（`keepAspect` 会把矩形缩过，`vis` 必须是**缩完**那一块）。</summary>
        ImageQuad ImgTex(Transform parent, Texture2D tex, string what, float x1, float y1, float x2, float y2,
                         string name, int q, bool keepAspect, Color? tint = null,
                         PxRect? clip = null, Vector2 clipSoftness = default(Vector2))
        {
            if (tex == null) { Debug.LogWarning("[Practice] 图取不到，这一层不画：" + what); return null; }
            float w = x2 - x1, h = y2 - y1;
            if (keepAspect && tex.height > 0)
            {
                float sa = (float)tex.width / tex.height, ra = w / Mathf.Max(1e-6f, h);
                if (sa > ra) { float nh = w / sa, d = (h - nh) * 0.5f; y1 += d; y2 -= d; h = nh; }
                else { float nw = h * sa, d = (w - nw) * 0.5f; x1 += d; x2 -= d; w = nw; }
            }
            var quad = ImageQuad.Create(parent, tex, Local3(parent, x1, y1, x2, y2),
                                        LayoutSpace.Px(h), new Vector2(0.5f, 0.5f), name);
            if (quad == null) return null;
            quad.SetAspect(w / Mathf.Max(1e-6f, h));
            quad.SetRenderQueue(q);
            if (tint.HasValue) quad.SetTint(tint.Value);
            // 🔴 软边**在 tint 之后**（切出来的子块要抄这份 tint）；`uv0` = **整张图的 uv**（这一层没截过 uv ⇒ (0,0,1,1)）
            if (clip.HasValue && (clipSoftness.x > 0f || clipSoftness.y > 0f))
                MenuDraw.ApplySoftEdges(quad, new PxRect(x1, y1, x2, y2), clip.Value, clipSoftness,
                                        new Rect(0f, 0f, 1f, 1f));
            return quad;
        }

        GameObject Nine(Transform parent, Transform basis, string art, Vector4 border, float texW, float texH,
                        float x1, float y1, float x2, float y2, int q, string name, Color? tint = null)
        {
            var tex = CardArt.MenuUi(art);
            if (tex == null) { Debug.LogWarning("[Practice] 九宫格图取不到：" + art); return null; }
            // 🔴 **2026-10-06（A94）：改走公共件 `MenuDraw.Nine`**（旧写法直调 `ImageQuad.CreateNineSlice`
            //   ⇒ 绕开公共件、**拿不到 `clip` / `clipSoftness`**）。与旧代码**逐项等价**：
            //    ① **矩形** = `(x1,y1)-(x2,y2)`（旧代码喂的 `LayoutSpace.Px(x2-x1)` / `Px(y2-y1)`
            //       就是公共件内部的 `LayoutSpace.Px(r.W)` / `Px(r.H)`）；
            //    ② **落位** = `Local3(basis, …)` 与 `MenuDraw.Local(parent, …)` **是同一份算式**
            //       （`LayoutSpace.RectCenter(…) − 基准.position`，逐字相同）⇒ 收口只在 `basis == parent`
            //       时才等价 —— 公共件**只认 `parent` 一个基准**（树父与坐标基准是同一个参数）。
            //       本文件两个调用点**都传同一个对象**，而这不等于「以后也一定」
            //       ⇒ 不等就**是位置画错**，⛔ 不许静默（下面出声）。
            //    ③ **队列 = `q`** + **tint 传 `tint`**（旧代码建完逐块设的就是这两样；两者写的是
            //       `sharedMaterial` 的**不同字段**（`renderQueue` / `color`）⇒ **先后无影响**）；
            //       `tint` 没传时两边**都不设**，同一条退化。块数 / uv 切分 / 每块 `SetAspect`
            //       两条路都出自同一个 `CreateNineSlice`（⛔ 别再给子块套整个面板的比例）。
            if (!ReferenceEquals(basis, parent))
                Debug.LogWarning("[Practice] 九宫格 `Nine` 的 `basis` 必须等于 `parent`"
                                 + "（`MenuDraw.Nine` 只按 `parent` 定位，两个不同就会摆错位置）");
            return MenuDraw.Nine(parent, tex, new PxRect(x1, y1, x2, y2), border, texW, texH, q, tint, true, name);
        }

        /// <summary>🆕 A9 尾巴：多了 `clip` / `clipSoftness` —— 原版 `RectMask2D` 对**文字**与图一视同仁
        /// （掩码在 shader 里按像素裁），我们这边走 `MenuDraw.ClipText`（TMP 逐字夹顶点 + 改 uv + 按剖面削 alpha）。
        /// 🔴 **必须在最后调**（`SetText` / `SetGlyphHeight` 任何一次重排都会把 mesh 重算回去）⇒ 放在本函数的**末句**。</summary>
        Label Txt(Transform parent, string text, float x1, float x2, float y1, float y2, float fontPx,
                  Align align, string name, int q,
                  PxRect? clip = null, Vector2 clipSoftness = default(Vector2))
        {
            var lb = Label.Create(parent, text ?? "", Local3(parent, x1, y1, x2, y2), 5, Color.white,
                                  new Vector2(0.5f, 0.5f), name);
            if (lb == null) return null;
            lb.SetRenderQueue(q);
            if (fontPx > 0f) lb.SetGlyphHeight(LayoutSpace.Px(fontPx));
            if (align == Align.Right) lb.AlignRightOn(LayoutSpace.FromPixel(x2, 0f).x);
            else if (align == Align.Left) lb.AlignLeftOn(LayoutSpace.FromPixel(x1, 0f).x);
            if (clip.HasValue && (clipSoftness.x > 0f || clipSoftness.y > 0f))
                MenuDraw.ClipText(lb, clip, clipSoftness);
            return lb;
        }

        Transform Hit(Transform parent, Transform basis, string name, PxRect r, System.Action onClick, int q = QPrHit)
            => HitOn(parent, basis, name, r, onClick, q);

        /// <summary>🆕 **2026-10-07（A12①）**：多了 `clip` —— **命中区也要截到视口内**。
        /// 判据 = 原版 `RectMask2D` 的**射线那一面**（`IsRaycastLocationValid`：框外的点判不中任何东西；
        /// UGUI 源码 `RectMask2D.cs:178-185`）—— 与 `MenuDraw.Hit` / `MenuDraw.ClipRect` 同一份判据。
        /// 🔴 **为什么这两个格子必须一起改**：本窗基类 `GameWindow` **没有** `Clip`（那是 `MenuWindowBase` 的），
        ///   收口前靠 `Inside`（**只建完整在视口里的格**）间接保证「命中区不会伸出视口」；
        ///   改成「相交就建」之后半行是**建出来**的 ⇒ 不截的话**点在视口外那条带子上照样命中**
        ///   （那条带子上还压着别的件：`Army Selector` 上沿 / 两列之间的缝）—— 那是收口顺带引入的偏离。
        /// ⚠️ `clip = null`（本文件其余那些调用点）⇒ `ClipRect` 第一句就 `return true` ⇒ 位置/尺寸逐字同旧。
        /// ⚠️ 整块在视口外 ⇒ 返回 **null**（节点一起不建）—— 两个调用点都已经判了 null。</summary>
        Transform HitOn(Transform parent, Transform basis, string name, PxRect r, System.Action onClick,
                        int q = QPrHit, PxRect? clip = null)
        {
            PxRect hr;
            if (!MenuDraw.ClipRect(r, clip, out hr)) return null;
            // 🔴 **2026-10-07（A92）**：这个 `Hit` 节点也从裸 `Transform` 改成 `RectTransform` ——
            //    它和 `MenuDraw.Hit` / `MenuWindowBase.AddHit` 建的是**同一种东西**（透明 quad + `WindowButton`），
            //    而那两条本次一起改了 ⇒ 同一件东西不许一半一种类型（判据 = 原版 16510/16768 是 `RectTransform`）。
            //    ⚠️ 本节点**要摆位置**（下一行）—— 位置走 `Local3`（世界坐标差），与节点类型无关，不受影响。
            var hit = new GameObject(name, typeof(RectTransform));
            hit.transform.SetParent(parent, false);
            hit.transform.localPosition = Local3(basis, hr.x1, hr.y1, hr.x2, hr.y2);   // **节点本身也要摆**
            var quad = ImageQuad.Create(hit.transform, CardArt.Solid(), Vector3.zero,
                                        LayoutSpace.Px(hr.H), new Vector2(0.5f, 0.5f), "Hit");
            if (quad != null)
            {
                quad.SetAspect(hr.W / Mathf.Max(1e-6f, hr.H));
                quad.SetTint(new Color(0f, 0f, 0f, 0f));
                quad.SetRenderQueue(q);
            }
            var wb = hit.AddComponent<WindowButton>();
            wb.onClick = onClick;
            return hit.transform;
        }
    }
}
