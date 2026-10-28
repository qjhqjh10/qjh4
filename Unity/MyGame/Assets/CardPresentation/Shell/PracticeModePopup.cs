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
//   · `Character Image`（905×905）—— ✅ **2026-10-22（界面线 M2）已建**。
//     🔴 **原来这一行写的是「原版 `m_Enabled=0` ⇒ 不建」—— 那条是错的**（铁律 5）：`m_Enabled=0` 只是
//     prefab 出厂态，原版**运行时**由 `DeckGeneralInfoDemo.ConfigPanel` 打开并喂 `fullHeroSprite`
//     （= `UI_Deck_Warlord_<督军名>` 整身立绘）。见 `BuildCharacterImage` 的 doc。
//   ✅ **2026-10-03（§三第29条 A14）**：~~`Deck Information cost drawer` + `Deck Information Cost/balance text`
//     —— **不建**（那套 `DeckEnergyCostDrawer` 费用曲线柱还没接）~~ ⇒ **两件都建了**：标题那条按原版
//     `Card / Energy cost`（fs34），曲线 9 行走 **`Core/CostCurveDrawer`**（与 `Deck info Popup` 那扇**共用一份画法**；
//     这一扇的 `localScale` 是 **1.2**，那扇是 1.8）。
//   · `Lore Text` —— ✅ **2026-10-19（界面线 P05）已接上**（原来这一行写「仍不建 … 正文在远端 CCD 的词条表里」，
//     🔴 **已过期**：84 份 `Demo DeckInfo` 的 `loreLocalizationKey` **84/84 命中**当前官方表，
//     en/zh 都非空；映射由 `工具/gen_prebuilt_decks.py` 的 `DEMO_LORE_KEY` 落到 `prebuilt_decks.json`
//     的 `loreKey`（103 副里 **78 副有**；其余 25 副本地确实没有 lore ⇒ 空 + 出声）。
//     原版 `DeckGeneralInfoDemo__ShowDemoDeckInfo.c` 实读：**演示卡组**下 `changePlayerDeck`（= `Generic Simplified UI Button`）
//     被 `SetActive(false)` ⇒ 所以我们选**预组卡组**时同样把 `Change Deck` 关掉、再显示 lore；
//     选「我的卡组」时反过来（原版 `SetContent` 那条路把两颗都打开）—— 见 `PickPrebuilt` / `PickDeck`。
//     ⚠️ 那个框 1280.27,647.41→1770.27,791.41 **套着** `Change Deck`（1398.00,680.75→1652.54,764.08）——
//     原版两件**不是同时可见的**（上面那条显隐判据），⛔ 别再拿「会叠字」当不建的理由。
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
//     ├ `Character Image`（905×905，原版出厂 `m_Enabled=0`、**运行时打开并喂整身立绘** —— ✅ 2026-10-22 已建）
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
        // 🔴 **2026-10-22（界面线 M2）就地订正（铁律 5）**：上一行那句「本窗 3101…3108 / 下一个是 3115」
        //    **已过期** —— `Deck info ▸ Character Image` 要插一档，本窗现在是 **3101…3110**
        //    （新增 `QPrChar`(3107) 立绘档 + `QPrFront`(3108) 本面板图档；`QPrText`/`QPrHit` 各上移两号到 3109/3110）。
        //    逐档理由与「撞了谁」写在 `QPrChar` / `QPrFront` 那两条 doc 上。
        // 🔴 **2026-10-11（A218 顺手收 · A252）可见性收窄**：`QPr` / `QPrText` / `QPrHit` 留 `public`
        //    （自检宿主 `Editor/{MainMenuScene,CollectionScene}.cs` 真按**限定名**引用它们）；
        //    `QPrRow` 与下面那五个 item 档**全工程只有本文件用** —— 2026-10-11 现量
        //    `grep -rn "PracticeModePopup.QPr"` 命中的限定名只有上面三个；`QPrRow` / `QPrItem*` 在别的文件里
        //    只出现在**字符串字面量**（一条断言文案）里，**不是代码引用** ⇒ 收成 `const`（= 不写 `public`）。
        //    ⚠️ 收窄**只针对用不到的**（放宽本身是必要的：本仓 0 个 `.asmdef` ⇒ 跨程序集要用就得 public）。
        public const int QPr = 3100, QPrText = 3109, QPrHit = 3110;
        // ⚠️ 下面这些**只在本文件用**（A252 收窄前的读数 = 外部代码引用 0 处）：格底那一档
        const int QPrRow = 3101;
        /// <summary>🆕 **2026-10-22（界面线 M2）**：`Deck info ▸ Character Image`（督军**整身立绘**，
        /// 原版 prefab 里 `m_Enabled=0`、**运行时由 `DeckGeneralInfoDemo.ConfigPanel` 打开并喂图**）。
        ///
        /// <para>🔴 **为什么它必须独占一档**：原版那颗是 `Deck info` 的**第 2 个子件**（红底之后、
        /// `Deck Name`/`Warlord Name`/`Army Image`/两个抽屉之前）⇒ 它要**盖住**左列与红底、
        /// 又被右面板自己的内容盖住。而我们的队列是**按角色分档**、不是按子树（左列与本面板共用
        /// `QPrRow`/`QPrItem*`）⇒ 只能在这条梯子上给它插一档。**本窗完整梯子**（从下到上）：
        /// `QPr`(3100 压暗/面板底) < `QPrRow`(3101 窗框件/格底) < `QPrItemHL..TxBg`(3102–3106 两族 item 内景)
        /// < **`QPrChar`(3107 立绘)** < `QPrFront`(3108 本面板的图 + 圆钮图标) < `QPrText`(3109) < `QPrHit`(3110)。
        /// ⚠️ **插一档就必然撞到别人的档**（3100…3108 已被本窗与 `LiveOpsEventWindow`(3104–3116) 占满，
        /// `3109` 是 `LiveOpsEventWindow.QDeck1`）—— 这是**已知代价**：`QPrText`/`QPrHit` 各上移两号，
        /// `QPrHit`(3110) 落在 `CampaignRewardWindow` 的档里。那几扇窗与本窗**不同时可见**
        /// （整页 vs 弹窗；本窗开时整页在最底下、被 `QPr` 的压暗层盖住），且**命中区那一档不渲染**。</para>
        /// <para>🧨 改坏法：把这层并回 `QPrRow` ⇒ 立绘会按「到相机的距离」和 `Army Image`/费用曲线柱
        /// 抢位置（2026-10-18 `DeckInfoPopup` 的同族先例：立绘被排到不透明红底**下面**，真机只剩一条）。
        /// 判据 → `Editor/MainMenuScene.cs` 练习窗段「`Character Image` 队列严格夹在红底与本面板内容之间」。</para>
        const int QPrChar = 3107;
        /// <summary>🆕 **2026-10-22（界面线 M2）**：本窗**压在底图之上、文字之下**那一档图 ——
        /// ① `Deck info` 自己那一组（`Army Image` / `Cardback` / `Show Deck Content` 两颗 / `Change Deck` 底 /
        /// 费用曲线 9 行的柱）；② **圆钮上的图标**（`Back Icon` / `Show Icon` / `Show Info Icon`）。
        /// <para>判据一（`Deck info` 那一组）= 原版兄弟序里它们**都排在 `Character Image` 之后** ⇒ 必须压在立绘之上
        /// （实测：63 张 `UI_Deck_Warlord_*` 里有多张在 `Army Image`/费用曲线那一带的 alpha = **255**，
        /// 同档硬碰会真的盖住它们）。</para>
        /// <para>🔴 判据二（图标那一组，**2026-10-22 顺手查出的真缺陷**）：那几颗钮的底图
        /// `UI_Button_Round_background` **中心是不透明纯黑**（直读 PNG：`(0,0,0,255)` 覆盖到 237² 全幅），
        /// 而图标（`40k_UI_bt_back` / `40k_UI_bt_deck`）必须画在它**上面** —— 改前两者**同档（`QPrRow`）**
        /// ⇒ 谁盖谁退化成渲染器排序 ⇒ 真机截图 `32_practice_again.png` 里**返回钮 / Show Deck Content 都是纯黑圆盘、
        /// 看不到绿色图标**（原版参考图里那两颗都是「金属环 + 绿色图标」）。</para>
        /// ⛔ 别把这两族改回 `QPrRow`。</summary>
        const int QPrFront = 3108;
        /// <summary>卡组格**内景**那五层 —— 原版 `Deck Selector Menu Item` 的兄弟序（**从下到上**）：
        /// `Highlight` → `Button border` → `Main image` → `IsPlayerDeck` → `Text background`(其下 `Deck Name`)。
        /// 🔴 **一层一个队列**：同队列「谁盖谁」不可控（`ImageQuad` 全是透明队列、按到相机的 3D 距离排 ——
        /// `CLAUDE.md` §三那两条）。先例 = `SearchingMatchPopup` 头部那条（第一版把 6 层塞进一个 3130，
        /// 结果压暗层把面板整块盖住）。`Main image` 在 `Highlight`/`Button border` **之上**是原版的兄弟序，
        /// ⛔ 别按「谁是底图」的直觉重排（三张图四角都是透明的，透明处露出下面那层，见各 `m_Sprite` 像素实测）。
        /// <para>🆕 **2026-10-10（A178）起这五个号由【两族 item】共用**（第二族 = 阵营格 `Practice Army Select
        /// Button` 的四层 `Highlight`/`Background`/`Icon`/`Has Player Deck`，见下面 `ArmyHl*` 那段）：
        /// 两族的层名同形（`Highlight` → 底 → 主图 → 「是不是我的」那件），而**屏上区域互不相交**
        /// （阵营列 110.15..205.81 × 卡组列 270.08..630.08，见 `PracticeModePopup__ChangeSelectedArmy` 的
        /// 那一族几何）⇒ 同号不会互相盖。
        /// 🔴 **为什么不新开五个号**：`3104–3119` 那一段已被 `LiveOpsEventWindow`(3104–3116) /
        /// `CampaignRewardWindow`(3110–3120) / `CardDetailPopup`(3115–3118) 占满，`3109` 也是
        /// `LiveOpsEventWindow.QDeck1`（现读全工程 `const int Q*`）；而这五个号本来就是「**本窗 item 内景**」
        /// 这一层语义 —— 新开号就得挤进别人的档，那才是真撞档。</para></summary>
        // 🔴 **2026-10-11（A218 顺手收 · A252）**：这五个号**全工程只有本文件用**（外部代码引用 0 处）
        //    ⇒ 收成 `const`（与上面 `QPrRow` 同一条口径）。值一个字没动。
        const int QPrItemHL = 3102, QPrItemBd = 3103, QPrItemArt = 3104, QPrItemSeal = 3105, QPrItemTxBg = 3106;

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
        /// = `floor((372.04 − 4 + 0 + 0.001) ÷ 180)` = **2**（判据 = uGUI `GridLayoutGroup` 里算行/列数那一段）。
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
        /// 判据 = uGUI `LayoutGroup` + `GridLayoutGroup` 那几处。</summary>
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
        /// <summary>🔴 **2026-10-12（A336①）：原版那一颗的 `m_fontSizeBase`**（画布 px，与 `AutoMin/Max` 同量纲）。
        /// 判据 = 扫 `d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/*.json` 的
        /// `m_fontSizeBase`（`工具/menu_dump.py` **不印这一列**，见 V7 §六·1）：全库唯一那颗
        /// `m_text = 'Name of the deck'` 的读数是
        /// **`m_fontSizeBase = 17.950000762939453` · `m_fontSize 29.0` · `auto[8.0~29.0]` · 折行 1**
        /// —— `fs`/`auto` 两列与我们 `ItemNameFontPx`/`ItemNameAutoMin/Max` 逐值相同 ⇒ 同一颗。
        /// ⚠️ 17.95 ≠ 29（标称）也 ≠ 36（TMP 出厂默认）⇒ 原版**显式设过**，必须照抄。</summary>
        public const float ItemNameAutoBase = 17.95f;
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
        /// <summary>阵营格 **82×82**、spacing **(0, 26.38)**、pad **0**、`m_ChildAlignment = 4`
        /// —— 原版 `Filters` 的 `GridLayoutGroup` 字段逐条抄。
        /// 出处 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-3879242981144375492.json`
        /// （全包**只有这一份** `m_CellSize = 82×82`；宿主 GameObject 的名字就叫 `Filters`）。
        /// 🔴 **2026-10-24（`W8` · P1 差异 #1）就地更正**：原来这里是**单列**（`x` 恒定居中、`i` 只喂纵轴）
        /// —— **与原版不符**。原版那一份的 `m_Constraint = Flexible`（0）+ `m_Spacing = (0, 26.38)`，
        /// 在 **177.12** 宽的容器里按 uGUI 现算 = **2 列 × 7 行**（13 格一屏全见、**不滚**）。
        /// 判据 = 本地真 uGUI 源码（`MyGame/Library/PackageCache/com.unity.ugui@…/Runtime/UGUI/UI/Core/
        /// Layout/GridLayoutGroup.cs` 的 `SetCellsAlongAxis`）：
        ///   `cellCountX = Max(1, FloorToInt((width − pad.horizontal + spacing.x + 0.001) / (cellSize.x + spacing.x)))`
        ///   = `Floor((177.12 + 0 + 0.001) / 82)` = **2**；
        ///   `positionX = i % cellCountX` · `positionY = i / cellCountX`（`m_StartAxis = Horizontal`(0)）。
        /// ⚠️ 横轴起点用同一份源码的 `LayoutGroup.GetStartOffset(0, requiredSpace.x)`：
        ///   `requiredSpace.x = 2×82 + 1×0 = 164` ⇒ 余量 `177.12 − 164 = 13.12` ⇒
        ///   `startOffset.x = pad.left + 13.12 × alignX(0.5) = **6.56**` ⇒ 第 0 列左沿 **75.98**。
        /// ⚠️ 纵轴**仍贴顶**（第 0 行上沿 = 视口上沿 149.07）：原版 `Filters` 同 GO 上那颗
        ///   `ContentSizeFitter(m_VerticalFit = MinSize)` 把内容**撑到恰好 732.28**（7×82 + 6×26.38）
        ///   ⇒ 纵轴余量 0。我们这一侧不建 fitter，故直接按「内容高 = 内容矩形高」摆（见 `ArmyGridContentH`）。
        /// ⛔ 别再退回一列：那样内容高 1382.56 > 视口 764.21 ⇒ 只能露 7 格、要滚（P1 §二 实测）。</summary>
        public const float ArmyCell = 82f, ArmyGapY = 26.38f, ArmyGapX = 0f;
        /// <summary>`Filters` 的 **列数** —— 照 uGUI `GridLayoutGroup` 的 `Flexible` 档现算（算式见上一条）。
        /// 今天 = **2**（177.12 宽里放两个 82 的格；间距 x = 0 所以两列紧挨着）。</summary>
        public static int ArmyCols
        {
            get
            {
                float w = ArmVpR - ArmVpL;
                return Mathf.Max(1, Mathf.FloorToInt((w - 0f + ArmyGapX + 0.001f) / (ArmyCell + ArmyGapX)));
            }
        }
        /// <summary>`Filters` 的 `m_ChildAlignment` 序列化值 = **4**（`TextAnchor.MiddleCenter`）。
        /// ⚠️ 对**横轴**它等价于 `1 UpperCenter`（只有 `align % 3` 参与）⇒ **两列 82 宽的格**在 177.12 里
        /// **整组居中**（`startOffset.x = 6.56`，见 `ArmyCell` 那条的 uGUI 实算式）。
        /// 纵轴那半（`align / 3 = 1` ⇒ `GetAlignmentOnAxis(1) = 0.5`）在原版里**吃不到余量**：
        /// `ContentSizeFitter(MinSize)` 把容器压到内容高 732.28，余量 = 0 ⇒ 恒贴顶（第一行 = `Viewport` 上沿）。
        /// 🔴 **2026-10-24（`W8`）就地更正**：本行原来写「13 个阵营的内容高 **1382.56**」—— 那是**单列**的
        /// 高（13 × 108.38）；改 2 列后 = **7 行 = 732.28 ≤ 视口 764.21** ⇒ 一屏全见、不再滚动。</summary>
        public const int ArmyAlign = 4;
        /// <summary>阵营格**第 0 列**的左沿 —— 照 uGUI `LayoutGroup.GetStartOffset(0, requiredSpace.x)` 现算：
        /// `pad.left + alignX × (可用宽 − (需要的宽 + pad.horizontal))`
        /// = `0 + 0.5 × (177.12 − (2×82 + 1×0))` = **6.56** ⇒ 格 = **75.98..157.98**（第 1 列）·
        /// **157.98..239.98**（第 2 列，间距 x = 0 ⇒ 两列紧挨）。
        /// 判据 = uGUI 源码 `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/LayoutGroup.cs`
        /// （`GetStartOffset`）+ `…/GridLayoutGroup.cs` 的 `SetCellsAlongAxis`（`cellCountX` / `actualCellCountX` /
        /// `requiredSpace` 那三句）。
        /// 🔴 **2026-10-24（`W8` · P1 差异 #1）**：原来这里算的是**单列居中**（`(177.12 − 82) × 0.5 = 47.56`
        /// ⇒ 116.98，正好落在原版两列的**缝隙**上）；改成原版那 2 列之后 = **75.98**。
        /// ⛔ 别把 `- ArmyCell` 写回来 —— 那是「一列居中」的算式。</summary>
        public static float ArmyCellL
        {
            get
            {
                float alignX = (ArmyAlign % 3) * 0.5f;       // 照 uGUI `GetAlignmentOnAxis(0)`（`4 % 3 = 1` ⇒ 0.5 = 居中）
                float required = ArmyCols * ArmyCell + (ArmyCols - 1) * ArmyGapX;
                return ArmVpL + ((ArmVpR - ArmVpL) - required) * alignX;
            }
        }

        // ---- 🆕 **2026-10-10（A178）**：阵营格 item prefab = `Practice Army Select Button` 的**四个子件** ----
        // 判据链（可复查）→ `资料/普查产出_1009/查证V1_原版prefab四件.md` §二：
        //   `PracticeModePopup.armySelectionButton`（MB `-5721221448436461764`）→ MB `-6931238765327743977`
        //   （类 `PracticeArmySelectionButton`）→ GO `Practice Army Select Button`（pid `-16359150157888489`）
        //   · RT `8940217906067473431`；现读命令 =
        //   `python 工具/menu_dump.py bundle_menus_assets_all --rt 8940217906067473431 --root-size 82x82 --depth 3 --md`
        //   兄弟序（原版**从下到上**）：`Highlight` → `Background` → `Icon` → `Has Player Deck`。
        // ⚠️ **与 A118 那颗 `Ranked Army Selector Container V2`（168²）不是同一颗** —— 两族层清单**零重叠**。
        /// <summary>`Highlight` 的**格内**矩形 —— **比格大 6.83/边 ⇒ 95.65²、会伸出格框**（原版如此，⛔ 别内缩）。
        /// 出处：`--rt 8940217906067473431` 的 `Highlight` 行 `-6.83,-6.83→88.83,88.83`（`Image` · `Simple`）。</summary>
        public const float ArmyHlX1 = -6.83f, ArmyHlY1 = -6.83f, ArmyHlX2 = 88.83f, ArmyHlY2 = 88.83f;
        /// <summary>`Has Player Deck`（右下那颗火漆印）的**格内**矩形 `54,45→81,102.25`（**下沿探出格 20.25px**）。</summary>
        public const float ArmySealX1 = 54f, ArmySealY1 = 45f, ArmySealX2 = 81f, ArmySealY2 = 102.25f;
        /// <summary>三张图的**落盘名**（`CardArt.MenuUi` 只按这个名字找）。
        /// 🔴 原版 sprite 名 → 落盘名的换算 = **空格换下划线**（导入器 `工具/import_original_art.py` 的
        /// `MENU_IMAGES` 那一批的规矩；`CardArt.MenuUi` **自己不换算**，传原名会静默取不到图 ——
        /// 同族先例见 `Shell/SocialWindow.cs` 里那条「`CardArt.MenuUi` 不做空格→下划线」注）：
        /// `UI_Deck_button_click`（169²，原名同）· `UI_Button_Round_background`（237²，原名同）·
        /// **`Purity Seal_02`（128×256）→ `Purity_Seal_02`**。</summary>
        public const string ArmyHlArt = "UI_Deck_button_click", ArmyBgArt = "UI_Button_Round_background",
                            ArmySealArt = "Purity_Seal_02";

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
        //   · `Practice Mode Menu/Deck Selector/Army Selector/Viewport`                    = **(0,50)**（本文件）
        //   · `Practice Mode Menu/Deck Selector/Deck Buttons/Decks Scroll view/Viewport`   = **(0,23)**（本文件）
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
        //
        // 🔴 **2026-10-13（A435 阶段 2 · 戊 / A754）：裁切状态已经搬到本窗那两颗 `Viewport` 节点上。**
        //   两处视口各自的框 + 软边现在长在 `ViewportClip` 组件里（`BuildArmySelector` / `BuildDeckRows`
        //   里 `AddComponent` 的那一颗，`padding = (0,0,0,0)`）——
        //   与迁移前「逐件把 `view` 当 `clip` 传 + 下面这两个常量当 `clipSoftness`」**逐位同值**
        //   （框 = 节点自己的 rect，本来就是同一个 `PxRect(ArmVp…)` / `PxRect(DecksVp…)`；两个参数逐字相同）。
        //   ⇒ 下面两个常量现在是**回落那一档**的软边（形参非空 = 旧路赢；节点在时以节点的 `softness` 为准）。
        //   先例 / 同一形状 = `Shell/ChatPanel.cs` 的 `ChatTab.VpSoft`（A435 丙）·
        //   `Shell/LiveOpsEventWindow.cs` 的 `VpSoft`（A435 丁 / A745）。⛔ **别把它们删掉**（回落那一档还要）。
        //   ⚠️ 上面那段「**为什么是逐件传而不是设一个 `ClipSoftness`**」是 **A9 尾巴当时的判断**，
        //   迁移后**只覆盖「`clipSoftness` 那一个形参」这一半**（`clip` 那半边已经交给节点）——
        //   按铁律 5 保留原文并就地标注，⛔ 不删。
        //
        // ⚠️ **本窗的窗级三兄弟（`GameWindow.Clip`/`ClipPad`/`ClipSoftness`）一个字都没用**（迁移前也没用 ——
        //   本窗当年走的是「逐件传 `clip`」而不是「设窗级字段」）⇒ 节点就是**唯一**的裁切状态载体，
        //   ⛔ 别照着别的窗的样子往这里写 `_win.Clip = …`（那会让形参盖住节点、**静默**不裁）。
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
        /// <summary>🆕 **2026-10-22（界面线 M2）**：`Deck info ▸ Character Image` 的矩形 —— **905 × 905**，
        /// `586.88,-9.72 → 1491.88,895.28`（原版 prefab 实读：锚 `(0.5,0.5)-(0.5,0.5)` · **pivot `(0.5,0)`** ·
        /// `posDelta (-201,-424)` · `sizeDelta (905,905)` · `Image`/`Simple` · **`m_Enabled = 0`**（出厂关））。
        /// 判据命令 = `python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Practice Mode Menu" --depth 4 --md`
        /// （2026-10-22 现读，`Deck info` 的第 2 个子件那一行）。
        /// <para>🔴 **出厂 `m_Enabled=0` 不是「原版不显示」**：原版运行时
        /// `DeckGeneralInfoDemo.ConfigPanel` 里 `Behaviour.set_enabled(本件, 1)` + `Image.set_sprite(heroSprite)`
        /// （反编译 `DeckGeneralInfoDemo__ConfigPanel.c` 中段：`+0x48` 那颗组件先 `set_enabled(1)` 再 `set_sprite`；
        /// `+0x48` = 序列化字段 `characterImage`，由 `menu_dump` 的「字段: …characterImage…」那一列坐实）。
        /// 喂的图 = `RawCardScript.fullHeroSprite`（玩家卡组那一支）/ `DemoDeckInfoSO.CharacterImage`
        /// （= `PrebuiltDeck.DeckHero.fullHeroSpriteReference`，预组那一支）= 我们工程里的
        /// `CardArt.DeckHero` 那一族 `UI_Deck_Warlord_&lt;督军名&gt;`（2048² 方形整身立绘）。
        /// ⛔ **别用 `CardArt.Portrait`**（卡面插图 671×1024 紧裁）—— 那是「战将立绘残缺」那条用户报过的病。</para></summary>
        public const float ChL = 586.88f, ChT = -9.72f, ChR = 1491.88f, ChB = 895.28f;
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
        /// <summary>🔴 **2026-10-12（A336①）：原版那颗 `Card Name` 的 `m_fontSizeBase`**（画布 px）。
        /// 判据 = 本仓自己的 dump（`m_fontSizeBase` 那一列 `menu_dump.py` 不印，本件另扫的）：
        /// `/Practice Mode Menu/Deck info/Deck List Drawer/Content/Deck Selector Card Info button_ref/Content/Background/Text fill/Card Name`
        /// ⇒ **`fb = 27.690000534057617`** · `fs = 19.85` · `auto[2~38]` · 折行 0
        /// （`fs`/`auto`/`折行` 与我们传的三格逐值相同 ⇒ 就是这一颗）。
        /// ⚠️ 27.69 ≠ 19.85（标称）也 ≠ 36（TMP 出厂默认）⇒ 原版显式设过。</summary>
        public const float CardNameAutoBase = 27.69f;
        public const float DlPadL = 22f, DlPadT = 4f;
        public const float DnL = 1371.64f, DnT = 224.13f, DnR = 1726.64f, DnB = 271.82f;
        /// <summary>⚠️ 原版 `Warlord Name` 的矩形**宽 0**（`ContentSizeFitter` + `RectSizeLimiter` 运行时算）
        /// ✅ **2026-10-28 订正（铁律 5 / 18·0）：原来这里写「🔶 自己设计（非原版）… 原版判据拿不到」【不成立】** —— 判据完全可读（`decomp_full/RectSizeLimiter__SetLayoutHorizontal.c` / `__SetLayoutVertical.c`，首行函数名就是它自己、35 行全文）⇒ 组件本体已逐句建成 `Shell/RectSizeLimiter.cs`，装配口 = `AttachWarlordNameLimiter`；`m_maxSize = (350, 0)` · `m_minSize = (0, 0)`（判据 = `bundle_menus_assets_all` 的 `MonoBehaviour_-6920805963187007684.json` / `MonoBehaviour_-888295288352693547.json`，两份值逐位相同）。**仍然是我们挑的只剩 `WnR` 这个常量右沿**（原版宽 0、由 `ContentSizeFitter(H=PreferredSize)` 撑到文本宽再被 350 夹住；本仓没有 uGUI 布局管线 ⇒ 常量框 + 夹取逼近，Hug 那一半不是逐字复刻）。</summary>
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
        /// <summary>原版 `Practice Mode Menu/Toggle` 的**根矩形**（`EverguildToggle` + `NonDrawingGraphic`）。
        /// ⚠️ 它**只是交互框**，四件可见子件**都探出**这个矩形（见下）—— 旧代码把这四个数当「整框」画底图/文字，
        /// 那是错的（U4-1 已改）。</summary>
        public const float TogL = 813.24f, TogT = 906.26f, TogR = 1106.76f, TogB = 964.10f;
        // ---- `Game mode` 开关的四件子件（🔴 2026-10-17（U4-1）**现读 dump 逐值**，绝对设计 px、y 向上）----
        // 判据命令 = `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Practice Mode Menu" --depth 6 --md`
        //   `Background`            915.50,915.18 → 1004.50,955.18   **89×40**  · sprite `40_main_bt_toggle_off`
        //   `Skirmish toggle Image` 813.24,887.86 →  907.89,982.50   **94.6439²** · `40k_gamemode_icon_skirmish`
        //   `Classic toggle Image` 1012.11,887.86 → 1106.76,982.50   **94.6439²** · `40k_gamemode_icon_classic`
        //   `Label Background`      799.87,853.24 → 1120.13,893.24   **320.267×40** · `40k_main_bt_nametag`（源图 109×41，Simple 拉伸）
        //   `Label`                 854.73,853.24 → 1065.27,893.24   **210.536×40** · fs36 基准36 · Center/Capline · 折行1
        // ⚠️ 命名沿用本文件既有 `L/T/R/B`（`T` = **下沿** y1、`B` = **上沿** y2 —— 与 `Back*` / `Cont*` 那几组同一约定）。
        public const float GmBgL = 915.50f, GmBgT = 915.18f, GmBgR = 1004.50f, GmBgB = 955.18f;
        public const float GmSkL = 813.24f, GmSkT = 887.86f, GmSkR = 907.89f, GmSkB = 982.50f;
        public const float GmClL = 1012.11f, GmClT = 887.86f, GmClR = 1106.76f, GmClB = 982.50f;
        public const float GmPlateL = 799.87f, GmPlateT = 853.24f, GmPlateR = 1120.13f, GmPlateB = 893.24f;
        public const float GmLabelL = 854.73f, GmLabelT = 853.24f, GmLabelR = 1065.27f, GmLabelB = 893.24f;
        public const float ContL = 1384.90f, ContT = 906.26f, ContR = 1837.98f, ContB = 956.10f;
        public const float BtTxL = 1385.08f, BtTxR = 1672.41f, BtTxT = 911.25f, BtTxB = 956.10f;
        public const float CircL = 1678.74f, CircT = 886.18f, CircR = 1768.74f, CircB = 976.18f;

        static readonly Vector4 InfoBorder = new Vector4(42f, 363f, 655f, 81f);
        const float InfoTexW = 1100f, InfoTexH = 701f;
        static readonly Vector4 SelBorder = new Vector4(0f, 0f, 0f, 0f);
        const float SelTexW = 1f, SelTexH = 1f;

        /// <summary>卡列表列数 = `floor((526.10 − 22 + 22) ÷ (231 + 22))` = **2**
        /// （照原版 `GridLayoutGroup`：`constraint=0/Flexible` ⇒ 按宽度算，pad 左 22 右 0）。
        /// <para>🔴 **2026-10-12（A1531⑧）钉一句**：这个数 = uGUI 的 **`cellCountX`**，只决定 `Content`
        /// 的 preferred 宽；原版 **`m_StartAxis = 1(Vertical)`** ⇒ 真决定填充的是**行容量**
        /// （见 <see cref="ListRowCap"/>）。⛔ 别拿它当「每行几格」摆位。</para></summary>
        public static int ListCols
        {
            get
            {
                float usable = (DlContentR - DlContentL) - DlPadL;
                return Mathf.Max(1, Mathf.FloorToInt((usable + DlGapX) / (DlCellW + DlGapX)));
            }
        }

        /// <summary>🆕 **2026-10-12（A1531⑧）**：一列放得下几行 —— 这**才是**本窗卡列表的填充单位。
        /// <para>判据 = 解包实读 `Practice Mode Menu ◂ Deck info ◂ Deck List Drawer ◂ Content` 的
        /// `GridLayoutGroup`（`工具/_probe_deckinfo.py bundle_menus_assets_all "Deck List Drawer"
        /// --pid -3777783439481286852` 亲读）：`m_CellSize 231×27.88` · `m_Spacing (22, 3.6)` ·
        /// `m_Padding = {L22, R0, T4, B0}` · `m_ChildAlignment/m_StartCorner = 0(UpperLeft)` ·
        /// **`m_StartAxis = 1(Vertical)`** · `m_Constraint = 0(Flexible)`。</para>
        /// <para>uGUI（`GridLayoutGroup.cs:265-267` + `:350-351`）：
        /// `cellCountY = floor((高 − padV + spacing.y + 0.001) ÷ (cellH + spacing.y))`
        /// = `floor((475.45 − 4 + 3.6 + 0.001) ÷ 31.48)` = **15**；
        /// **`positionX = i / cellCountY` · `positionY = i % cellCountY`**（先填满一列再换列）。
        /// （`DlContentB − DlContentT` = `793.87 − 318.42` = **475.45**；padV = `DlPadT(4) + 0`。）</para></summary>
        public static int ListRowCap
        {
            get
            {
                return Mathf.Max(1, Mathf.FloorToInt((DlContentB - DlContentT - DlPadT + DlGapY + 0.001f)
                                                     / (DlCellH + DlGapY)));
            }
        }

        /// <summary>空节点（**`RectTransform`**）**＋ 矩形（位置 + 尺寸）**。
        /// 🔴 **2026-10-07（A92）**：原来是裸 `Transform`。
        /// 🔴 **2026-10-11（A332）**：**收口到 `MenuDraw.Node`** —— 位置走 `MenuDraw.Local`、
        /// 尺寸走 `MenuDraw.SetPxSize`，**换算只有那一份**（`CLAUDE.md` §三「两处写同一条规则 =
        /// 迟早不一致」）。此前这 9 个调用点清一色是 `x = New(p, n); x.localPosition = Local3(...)`
        /// —— 只写位置、**不写 `sizeDelta`** ⇒「空节点 + 原版像素矩形」的宽高**验收不了**
        /// （与 A218 修掉的是同一个缺陷）。
        /// ⚠️ **本次收口对位置是零行为变化**：`Local3(basis, …)` 的定义就是
        /// `LayoutSpace.RectCenter(…) − MenuDraw.PosInDesignSpace(basis)`（本文件 `Local3`，`本文件`），
        /// 与 `MenuDraw.Local` **逐字同式**。
        /// ⚠️ **只收 `(parent, name)` 的那个重载已删**（不是漏写）：留着它 = 留一条**不写尺寸**的入口，
        /// 下一个人照它写就又回到旧路。9 个调用点全部改成收 `PxRect` 那一版（逐个核过，见
        /// `资料/普查产出_1011/WB2_A332.md`）。
        ///
        /// 判据与实读见 `MenuDraw.Node` 的注释；本文件这 9 个名字逐个核过，**全是 `RectTransform`**
        /// （`Deck info` · `Background Info` · `General container` · `Army Selector`（原版 `ScrollRect`）·
        /// `Viewport`（原版挂 `RectMask2D`）· `Filters`（原版 `GridLayoutGroup`）· `Decks Scroll view` ·
        /// `Content` —— 实读 `bundle_menus_assets_all` 同名 `GameObject` 的组件类型）。
        /// ⛔ 别写成 `AddComponent&lt;RectTransform&gt;()`。</summary>
        static Transform New(Transform parent, string name, PxRect r)
            => MenuDraw.Node(parent, name, r);

        Label _txtArmy, _txtDeckName, _txtWarlord;
        /// <summary>🆕 **2026-10-19（界面线 P05）**：`Deck info ▸ General container ▸ Lore Text`
        /// （原版 `DeckGeneralInfoDemo.lore`）。内容只来自 `PrebuiltDecks.Deck.LoreText`
        /// （= `DemoDeckInfoSO.loreLocalizationKey` 的官方词条）；「我的卡组」没有 lore ⇒ 空串。</summary>
        Label _txtLore;
        /// <summary>`Change Deck` 那三件（底图 / 字 / 命中区）。原版 `changePlayerDeck` =
        /// `Generic Simplified UI Button` 整颗 —— 演示卡组下 `ShowDemoDeckInfo` 会 `SetActive(false)`，
        /// 「我的卡组」下 `SetContent` 会重新打开 ⇒ 两个入口都要跟着切。</summary>
        Transform _chgBg, _chgText, _chgHit;
        /// <summary>「这副牌本地没有 lore」只出声一次（切来切去不刷屏；但仍要说，⛔ 不静默）。</summary>
        bool _loreWarnedNoData;
        Transform _info, _bgInfo, _general, _deckList;
        ImageQuad _armyIcon, _cardback;
        /// <summary>🆕 **2026-10-22（界面线 M2）**：`Deck info ▸ Character Image`（督军整身立绘，905×905）。
        /// 换卡组时要跟着换图（`RefreshCharacterImage`，判源 = <see cref="CurrentDeck"/>）。</summary>
        ImageQuad _charImg;

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
            // 🆕 **A1205 接上**（`R5` §2·H #46：原版 `Deck Selector ▸ Deck Buttons ▸ Selected Army Title`
            //    = `auto[1~36] · base 23 · 折行 1`）⇒ 折行档 = 1，`wrapPx` = 本格框宽
            //    （原版那颗的宽由布局排出来、prefab 里是模板位 ⇒ **这一格不是原版字段值**，
            //     同 `Shell/SettingsWindow.cs:4717` 那条登记）。
            // 🔴 **A1024③（2026-10-17）换面**：`Practice Mode Menu` 里 **Asar 三颗** =
            //    `Selected Army Title` / `Lore Text` / `Deck Name`（判据 = `menu_dump … "Practice Mode Menu"
            //    --depth 25` 逐颗现读）；其余（`Warlord Name` / `GameModeText` / `Main Search message` /
            //    `Few players online message` / `tooltip` / `Cards in deck` / `Card Cost` / `Button Text` …）**全 Pragati**。
            //    ⚠️ **2026-10-19（界面线 P05）就地订正**：这里原来写「`Lore Text` 本扇**不建** ⇒ 两颗 Asar 有落点」
            //    —— **已过期**：`Lore Text` 已建（见 `BuildGeneralContainer`）且**就是** Asar（`LatinUi`）
            //    ⇒ 本窗三颗 Asar **全部有落点**（`Selected Army Title` / `Lore Text` / `Deck Name`）。
            //    ⚠️ **同名不同字**：`Deck Selector Menu Item ▸ Deck Name`（`本文件`）是 **Pragati**，
            //    只有 `Deck info ▸ Deck Name`（`本文件`）是 Asar —— 逐站判过，⛔ 别按名统一。
            _txtArmy = Txt(root, SelectedArmyName(), ArmyTxL, ArmyTxR, ArmyTxT, ArmyTxB, 36f, Align.Center, "Selected Army Title", QPrText,
                           wrapPx: ArmyTxR - ArmyTxL, autoMinPx: 1f, autoMaxPx: 36f, autoBasePx: 23f,
                           vAlign: Label.VAlign.Midline, face: TmpFont.Face.LatinUi);   // 🆕 N11：原版 `Center/**Midline**`（现读 dump）

            // `Deck info` —— **容器本身没有图**（红底在子件 `Generic Window Red Background Big` 上，见常量段注释）
            // 🔴 **2026-10-11（A332）**：矩形随节点一起写（`InfoL/InfoT/InfoR/InfoB` = 638.38,78.79→1842.38,863.77
            //    ⇒ **1204 × 784.98**）。出处 = `资料/主菜单_原版规格.md` 那条链的实读（`Editor/MainMenuScene.cs`
            //    里那条 `CheckAt(… "Deck info" 容器")` 用的就是这四个数）。
            _info = New(root, "Deck info", new PxRect(InfoL, InfoT, InfoR, InfoB));

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

            // 🆕 **2026-10-22（界面线 M2）**：`Character Image`（督军整身立绘）—— 原版 `Deck info` 的**第 2 个子件**，
            //    排在红底之后、`Background Info` 之前（兄弟序照抄；判据见 `ChL/T/R/B` 的 doc）。
            BuildCharacterImage();

            // `Background Info` —— 原版实读：**【空容器】+ 出厂 act=0**（一条子件都没有）。
            // ⚠️ 它**不是** `Deck Name`/`Lore Text` 那几件的父（正本 §二 A 那么写是错的 —— 第 53 条）。
            //    照原版**留空**：建出来、关着，让别人一眼看得出「这里原版就是空的」。
            //    🔴 **A332**：矩形 1258.08,316.03→1789.08,800.45（**531 × 484.42**）—— 原版这一件实读的四个数。
            _bgInfo = New(_info, "Background Info", new PxRect(1258.08f, 316.03f, 1789.08f, 800.45f));
            _bgInfo.gameObject.SetActive(false);

            // ---- 直挂 `Deck info` 的三件（原版 act=1 ⇒ **本来就该看得见**）----
            BuildDeckInfoHead();

            // ---- `General container`（**出厂可见**那个抽屉）----
            _general = New(_info, "General container", new PxRect(DlL, DlT, DlR, DlB));   // A332：569.79 × 619.53
            BuildGeneralContainer();

            // ---- `Deck List Drawer`（原版 act=1，但出厂被 `SetContent` 关掉；内容见 `BuildCardRows`）----
            BuildCardRows(_info);
            Toggle(true);                       // 出厂 = 总览（`SetContent` 那两句）
        }

        // ============================================================ 🆕 2026-10-22（界面线 M2）：`Character Image`
        /// <summary>`Deck info ▸ Character Image` —— 督军的**整身立绘**（905×905，rect 见 <see cref="ChL"/>）。
        ///
        /// <para>🔴 **原来这里记的是「原版 `m_Enabled=0` ⇒ 不建」（见文件头「没建的」那一行）—— 那条已作废**
        /// （铁律 5）：`m_Enabled=0` 只是 **prefab 出厂态**，原版**运行时**会把它打开并喂图 ——
        /// `DeckGeneralInfoDemo.ConfigPanel` 里对序列化字段 `characterImage` 先 `Behaviour.set_enabled(1)`
        /// 再 `Image.set_sprite(heroSprite)`（`d:/2/tools/decomp_full/DeckGeneralInfoDemo__ConfigPanel.c` 中段）。
        /// 参考图 `C:/Users/qjh36/Desktop/warpforge参考图/练习界面参考图.png` 右侧那块大立绘就是它
        /// （Iconward Malak Vorenth 举旗站姿）。</para>
        ///
        /// <para>⚠️ **图只走 `CardArt.DeckHero`**（`UI_Deck_Warlord_&lt;督军名&gt;`，2048² 方形整身抠图）——
        /// ⛔ 别用 `CardArt.Portrait`（卡面插图 671×1024 紧裁）：那是用户报过的「战将立绘残缺」
        /// （同 `Shell/DeckInfoPopup.cs` 的 `Warlord Image`，2026-10-18 已在那里改对）。</para></summary>
        void BuildCharacterImage()
        {
            var tex = CurrentWarlordArt();
            if (tex == null) return;                 // 已经出声（`CurrentWarlordArt` 里）
            _charImg = ImgTex(_info, tex, "Character Image（督军整身立绘）", ChL, ChT, ChR, ChB,
                              "Character Image", QPrChar, KeepAspectFor(tex));
        }

        /// <summary>`CardArt.DeckHero` 那一族是 **2048² 方形** ⇒ 905² 框里「拉伸」与「等比内接」逐位同值
        /// （= 原版那颗 `Image` 的 `m_PreserveAspect = 0`）；只有「取不到立绘、退回卡面插图（671×1024）」
        /// 那条**我们自己的兜底**按非方形等比内接保形。判据与 `Shell/DeckInfoPopup.cs` 的 `Warlord Image`
        /// **同一式**（那里写的是 `!(Mathf.Abs(w - h) &lt;= 1)`）—— ⛔ 别另立一套。</summary>
        static bool KeepAspectFor(Texture2D t) => t == null || t.height <= 0 || Mathf.Abs(t.width - t.height) > 1;

        /// <summary>当前卡组的督军整身立绘 —— **唯一判源 = <see cref="CurrentDeck"/>**
        /// （预组那一支走 `PrebuiltDecks.ToPlayerDeck`，它的 `WarlordId` 就是 `Deck.heroId`）。
        /// <para>🔴 **2026-10-22（界面线 M3）改走两档可判别的 `CardArt.TryDeckHero`**：
        /// 旧写法拿**纹理名前缀** (`UI_Deck_Warlord_`) 去猜「是不是拿到了方形立绘」—— 那条判据
        /// 在 `DA75` 那种资产名**没有** `_Warlord_` 前缀（`UI_Deck_Master Zacharial`）的档上会误判。
        /// 现在：true ⇒ 方形立绘（905² 槽**铺满**）；false ⇒ 退回卡面插图（**等比内接** + 出声）。</para>
        /// <para>取不到 / 只剩兜底卡面插图时**都出声**（⛔ 不静默）—— `TryDeckHero` 自己那条已经
        /// 说清了「期望哪个资产名」，这里只补「本窗会怎么画」。</para></summary>
        Texture2D CurrentWarlordArt()
        {
            var deck = CurrentDeck;
            string wid = deck != null ? deck.WarlordId : null;
            if (string.IsNullOrEmpty(wid))
            {
                Debug.LogWarning("[Practice] `Character Image` **不画**：当前卡组没有督军"
                                 + (deck != null ? "（「" + deck.Name + "」）" : "（没有卡组）"));
                return null;
            }
            Texture2D tex;
            if (CardArt.TryDeckHero(wid, out tex) && tex != null) return tex;
            // 兜底：`CardArt.DeckHero` 内部退回 `Portrait`（卡面插图）。
            // **出声**（这是我们的兜底，不是原版做法 —— 原版喂的是 `RawCardScript.fullHeroSpriteReference`）。
            tex = CardArt.Portrait(wid);
            if (tex == null)
            {
                Debug.LogWarning("[Practice] `Character Image` **不画**：督军 " + wid + " 的整身立绘与卡面插图都取不到");
                return null;
            }
            Debug.LogWarning("[Practice] `Character Image` 退回**卡面插图**（" + tex.name + "，"
                             + tex.width + "×" + tex.height + "）并按竖构图**等比内接** —— "
                             + "**不是原版做法**（原版喂 `RawHeroSpriteReference` = `UI_Deck_Warlord_*` 2048² 方形）");
            return tex;
        }

        /// <summary>换卡组（`PickDeck` / `PickPrebuilt`）时换图 —— 与 `_txtDeckName` / `_armyIcon` / `_cardback`
        /// 同一批刷新，判源也是同一个 <see cref="CurrentDeck"/>（⛔ 别按 `DeckIndex` 再判一次：预组档它恒为 −1）。
        /// 🧨 改坏法：只在建窗时画一次 ⇒ 换卡组之后立绘停在上一次那副的督军（断言量两次纹理名，会红）。</summary>
        public void RefreshCharacterImage()
        {
            if (_charImg == null)
            {
                // 建窗那一刻当前卡组没有督军（空卡组）⇒ 那一层当时没建。这里补建 —— 否则「先空后选」永远缺这一块。
                if (_info != null && CurrentDeck != null && !string.IsNullOrEmpty(CurrentDeck.WarlordId))
                    BuildCharacterImage();
                return;
            }
            string newWid = CurrentDeck != null ? CurrentDeck.WarlordId : null; if (!string.IsNullOrEmpty(_charImgWarlordId) && _charImgWarlordId != newWid) CardArt.ReleaseFullHeroSprite(_charImgWarlordId); _charImgWarlordId = newWid; var tex = CurrentWarlordArt();   // 账 `战斗逻辑驱动:1280`：原版 `PlayerDeckDemoDeckInfoWrapper__UnloadCharacterImage`（`decomp_full/…:7-11`）是「换图前先放**上一张**」—— 我们只有一条按路径的登记，所以必须**自己记着上一张是谁**（`_charImgWarlordId`）；与本行末尾那句取图合并写 = **零行数变化**
            if (tex == null) { _charImg.gameObject.SetActive(false); return; }
            if (!_charImg.gameObject.activeSelf) _charImg.gameObject.SetActive(true);
            _charImg.SetTexture(tex, true);          // 只换图；比例由下一行**显式**给（keepAspect 不会改 `_aspect`）
            FitQuadToRect(_charImg, tex, ChR - ChL, ChB - ChT);
        }

        /// <summary>把 quad 按贴图比例**内接**进 `w×h` 的框（居中）—— 与 `ImgTex` 里 `keepAspect` 那一支**同一式**：
        /// `WorldH` 取框高、`WorldW = WorldH × 贴图比例`（宽的那一档反过来）。
        /// <para>🔴 **2026-10-22（界面线 M3）**：**方形图（|w−h|≤1）直接铺满**，不走内接 ——
        /// 原版那颗 `Character Image` 是 `m_PreserveAspect = 0`（拉满 905²），而方形图「拉满 ≡ 等比」，
        /// 两种写法同解；写成显式的「铺满」是为了**别让将来某张近似方形（2048×2047 之类）被悄悄缩一圈**。
        /// ⚠️ 只有「退回卡面插图（671×1024）」那条**我们自己的兜底**才等比内接保形。</para>
        /// 换图后**必须**重算：方形立绘 ↔ 竖构图插图互换时，quad 不重算就会被横向拉宽（静默，只是「看着胖」）。</summary>
        static void FitQuadToRect(ImageQuad q, Texture2D tex, float w, float h)
        {
            if (q == null || tex == null || tex.height <= 0) return;
            if (System.Math.Abs(tex.width - tex.height) <= 1)
            {
                // 方形槽**铺满**（= 原版 `m_PreserveAspect = 0`）。
                q.SetWorldHeight(LayoutSpace.Px(h));
                q.SetAspect(w / Mathf.Max(1e-6f, h));
                return;
            }
            float sa = tex.width / (float)tex.height, ra = w / Mathf.Max(1e-6f, h);
            if (sa > ra) h = w / sa; else w = h * sa;
            q.SetWorldHeight(LayoutSpace.Px(h));
            q.SetAspect(w / h);
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
            // 🆕 **A1205 接上**（`R5` §2·H #47：原版 `Deck info ▸ Deck Name` = `auto[1~36] · base 22.68 · 折行 1`）
            _txtDeckName = Txt(_info, d.Name, DnL, DnR, DnT, DnB, 36f, Align.Left, "Deck Name", QPrText,
                               wrapPx: DnR - DnL, autoMinPx: 1f, autoMaxPx: 36f, autoBasePx: 22.68f,
                               face: TmpFont.Face.LatinUi);
            // 「未选战将」在本文件有三处（建窗这一处 + 两个 `SetText` 刷新点）—— **一律**走同一条词条
            // `MenuDeck/Error/NoWarlord`，施工单 §附_Shell #1 判「复用，别新造」。
            // ⚠️ **如实记**：表里中文列是「还没有选战将」—— 与改前写死的「未选战将」**不同字**，这是有意的
            //    （键名/两列都由 `Loc` 定，⛔ 别为「保住旧字」另立一条同义键，铁律 6）。
            // 🆕 **A1205 接上**（`R5` §2·H #48：原版 `Deck info ▸ Warlord Name` = `auto[1~35] · base 24.3 · 折行 1`）
            _txtWarlord = Txt(_info, WarlordNameOf(wl), WnL, WnR, WnT, WnB, 35f,
                              Align.Left, "Warlord Name", QPrText,
                              wrapPx: WnR - WnL, autoMinPx: 1f, autoMaxPx: 35f, autoBasePx: 24.3f,
                              face: TmpFont.Face.LatinCardBody); BuildWarlordHit(wl); AttachWarlordNameLimiter(_txtWarlord);   // 原版第 4 颗钮（账 卡组:134），见方法头；末尾那句 = 原版那颗 `RectSizeLimiter`（账 未分类:509/510）
            _armyIcon = Img(_info, DeckRuntime.FactionIcon(d.Faction), ArmImgL, ArmImgT, ArmImgR, ArmImgB,
                            "Army Image", QPrFront, true);   // 🆕 2026-10-22（M2）：`QPrRow`→`QPrFront`（要压在立绘之上，见 `QPrFront` doc）
        }

        /// <summary>`General container` 里的几件：cardback · `Show Deck Content Button` · `Change Deck`。
        /// **没建的**（出声，见文件头）：`Deck Information cost drawer` + `Deck Information Cost/balance text`。
        /// 🔴 **2026-10-03 就地更正（A20）**：这里原来写着「我们没有『卡组装备了哪张卡背』这份数据 ⇒
        /// 退回 `CardArt.CardBack(阵营)`……**这条是我们挑的，不是复刻**」—— **那句话现在作废**：
        /// `CollectionData.DeckInfo` **有 `CardbackId`**（`Shell/CollectionData` 里 `DeckInfo` 那条，卡组编辑器那页能装备），
        /// 而原版这条链要的就是它（`DeckGeneralInfoDemo.ShowPayerDeckInfo → CardDeck.GetDeckCardback()`）
        /// ⇒ 改成走**判据那一处** `CardArt.DeckCardback(卡背 id, 阵营)`（选了用选的、没选用阵营默认背）。</summary>
        void BuildGeneralContainer()
        {
            var d = CollectionData.DeckAt(DeckIndex);

            // ✅ **2026-10-19（界面线 P05）：`Lore Text` 建出来了** —— 这一段原来写「不画」，
            //   理由（「正文在远端」）**已过期**：84 份 `Demo DeckInfo` 的 `loreLocalizationKey` 84/84
            //   命中当前官方表（en/zh 非空），映射落在 `PrebuiltDecks.Deck.loreKey`
            //   （生成器 `工具/gen_prebuilt_decks.py` 的 `DEMO_LORE_KEY`，判据见它的头注释）。
            //   判据（逐颗现读 `menu_dump … "Practice Mode Menu" --depth 5 --md`）：
            //   `Deck info ▸ General container ▸ Lore Text` = 裸 `TextMeshProUGUI + EverguildTextController`，
            //   出厂 `'Start here! … Orks.'`（**旧占位，别当词条值**）· fs **30** · base **29.5** ·
            //   **auto[1.0~30.0]** · 对齐 `Center/**Middle**` · **折行 = 1** · 色 (1,1,1,1) ·
            //   框 **490×144**（= `LoreL/T/R/B`）· 字体 **Asar**（`A1024③`：本窗三颗 Asar 之一）⇒ `LatinUi`。
            //   ⚠️ 出厂文本没抄（运行时由 `DemoDeckInfoSO.get_Lore` 覆盖）；「我的卡组」没有 lore ⇒ **空串**，
            //   ⛔ 不拿督军效果文字顶（那是明令禁止的口径，见 `资料/任务线/界面线/任务/T05…`）。
            _txtLore = Txt(_general, "", LoreL, LoreR, LoreT, LoreB, 30f, Align.Center, "Lore Text", QPrText,
                           wrapPx: LoreR - LoreL, autoMinPx: 1f, autoMaxPx: 30f, autoBasePx: 29.5f,
                           face: TmpFont.Face.LatinUi);

            // 🆕 2026-10-03（§三第29条 A14）：`Cost/balance text` + `Deck Information cost drawer` **补上了**。
            //   判据：`资料/阶段二_战斗入口_原版规格.md` §二 A 那张表（`python 工具/menu_dump.py … "Practice Mode Menu"`）
            //   · `Cost/balance text` **1276.27,326.95→1540.27,381.27** · fs34 auto[1-34] · 居中 · 原文 `Card / Energy cost`
            //   · `cost drawer` **1329.20,405.18→1487.35,604.24** · **`localScale 1.2`**（`Deck Info Popup` 那份是 1.8）
            //   🔴 **画法不在这儿**：与 `Deck Info Popup` 那扇共用 `Core/CostCurveDrawer`
            //      （CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
            // 🆕 **A1205 接上**（`R5` §2·H #49：原版 `Deck info ▸ General container ▸
            //    Deck Information Cost/balance text` = `auto[1~34] · base 22.3 · 折行 1`）
            // 🔴 **2026-10-19（`A1501`）：文案改走 `Loc.T`** —— 那颗 TMP 的
            //    `mTerm = Demo/CardCostBalance`、`m_text = 'Card / Energy cost'`（本轮现读）。
            Txt(_general, Loc.T("Demo/CardCostBalance"), CostTxtL, CostTxtR, CostTxtT, CostTxtB, 34f, Align.Center,
                "Deck Information Cost/balance text", QPrText,
                wrapPx: CostTxtR - CostTxtL, autoMinPx: 1f, autoMaxPx: 34f, autoBasePx: 22.3f,
                face: TmpFont.Face.LatinCardBody);
            {
                // 🔴 **2026-10-21**：数一遍 + 画一遍收进 `RefreshCostRows()` —— 换卡组（`PickDeck` /
                //   `PickPrebuilt`）也要重画，**画法与判源只此一处**（⛔ 别在换卡组那边再写一份）。
                RefreshCostRows();
            }

            // 卡背是 **两层**（`Cardback` + `Cardback Front`）+ 一条 0.25 s 入场 —— 判据/几何/动画见 `BuildCardbacks` 的方法头
            BuildCardbacks(d);

            // ⚠️ 图标那层用**原版 `Icon` 子件自己的矩形**（1737.68,246.46→1785.07,293.12 = 47.39×46.66），
            //    不是父件的 64.24×63.24（用父的会把图标放大 1.36 倍、而且不居中）
            Img(_general, "UI_Button_Round_background", ShowBtnL, ShowBtnT, ShowBtnR, ShowBtnB, "Show Bg", QPrRow, true);
            // 🔴 图标在钮面之上（同 `Back Icon` 那条：钮底中心不透明纯黑）。
            Img(_general, "40k_UI_bt_deck", 1737.68f, 246.46f, 1785.07f, 293.12f, "Show Icon", QPrFront, true);
            // ✅ **2026-09-24 订正**：原版这颗钮**不是**翻开卡列表抽屉 —— 它是
            //    `DeckGeneralInfoDemo.cardInDeckInfoButton`（pid 由序列化字段解出），
            //    点下去的 `CardInDeckInfoButtonOnClick` 走 `WindowsManager.OpenWindow(new DeckInfoContext(deck, 2, …))`
            //    ⇒ **开的是「卡组详情」那扇窗**（`Deck info Popup`，我们已建）。
            HitOn(_general, _general, "ShowDeckHit", new PxRect(ShowBtnL, ShowBtnT, ShowBtnR, ShowBtnB),
                  () => ShowDeckContent());

            // 🔴 **2026-10-19（界面线 P05）**：`Change Deck` 三件存下来 —— 原版 `DeckGeneralInfoDemo.changePlayerDeck`
            //   （= 这颗 `Generic Simplified UI Button`）在**演示卡组**下被 `ShowDemoDeckInfo` 关掉、
            //   在「我的卡组」下被 `SetContent` 打开（反编译字段 PPtr 现核：`changePlayerDeck` → 本颗）。
            //   ⚠️ 它与 `Lore Text` 的框**互相重叠**（lore 1280.27,647.41→1770.27,791.41 套着本钮
            //   1398.00,680.75→1652.54,764.08）—— 原版靠**两者不同时可见**避开；我们照抄这条显隐。
            var chgBgQ = Img(_general, "UI_Button_Mulligan", ChgL, ChgT, ChgR, ChgB, "Change Deck Bg", QPrFront, true);
            _chgBg = chgBgQ != null ? chgBgQ.transform : null;
            // 🆕 **A1205 接上**（`R5` §2·H #50：原版 `General container ▸ Generic Simplified UI Button ▸
            //    Button Text` = `auto[10~45] · base 12 · **折行 0**`）—— 本方法 8 处里**唯一**折行 0 的一处
            //    ⇒ `wrapPx` 缺省 0（漏斗内部会 `SetWrapping(false)` 把 `SetWrapWidth` 开的 `Normal` 还原）。
            // 🔴 **2026-10-19（`A1501`）：文案改走 `Loc.T`** —— 那颗 `Button Text` 的
            //    `mTerm = MainMenu/RankedWindow/ChangeDeck`、`m_text = 'Change Deck'`（本轮现读）。
            var chgTx = Txt(_general, Loc.T("MainMenu/RankedWindow/ChangeDeck"), ChgL, ChgR, ChgT, ChgB, 45f, Align.Center, "Change Deck Text", QPrText,
                autoMinPx: 10f, autoMaxPx: 45f, autoBasePx: 12f,
                vAlign: Label.VAlign.Capline,   // 🆕 N11：原版 `Button Text` = `Center/**Capline**`（现读 dump）
                // 🆕 **2026-10-17（A1226）框**：原版 `General container ▸ Generic Simplified UI Button ▸ Button Text`
                //   的**有效框 = 236.77×61.68 画布 px**（`AspectRatioFitter` 宽控高；相对钮 254.538×83.321：
                //   左 +8.85 / 上 +10.74）。原来传的是**整颗钮 254.54×83.33** ⇒ 宽多 17.77、高多 21.65。
                //   ⚠️ 与 `DeckInfoPopup.Txt` 的 `boxW/boxH` 同一个口径（父宽×锚跨度＋sd，⛔ 不是裸 `m_SizeDelta`）。
                boxW: 236.77f, boxH: 61.68f, face: TmpFont.Face.LatinCardBody);
            _chgText = chgTx != null ? chgTx.transform : null;
            // ✅ 原版这条就是 `DeckGeneralInfoDemo.ChangePlayerDeckButton`
            //    → `WindowsManager.OpenWindow(new DeckSelectionContext(...))`（`…__ChangePlayerDeckButton.c`）。
            _chgHit = HitOn(_general, _general, "ChangeDeckHit", new PxRect(ChgL, ChgT, ChgR, ChgB), () => OpenDeckSelection());
        }

        /// <summary>🆕 **2026-10-21**：重画 `Deck information cost drawer` 那 **9 行**（费用 0..8）。
        ///
        /// <para>🔴 **数一遍 + 画一遍只此一处**：建窗（`BuildGeneralContainer`）与换卡组（`PickDeck` /
        /// `PickPrebuilt`）都走它。原来那段内联在 `BuildGeneralContainer` 里 ⇒ 换卡组时**没人重算**，
        /// 面板上留着上一副的曲线（预组那一档更是 9 行全 0，真机 2026-10-21 抓到）。</para>
        ///
        /// <para>⚠️ **判源 = <see cref="CurrentDeck"/>**（预组那一支 = `PrebuiltDecks.ToPlayerDeck`，
        /// 其 `CardIds` 就是 `PrebuiltDecks.Deck.cardIds`，**不含督军与防御卡** —— 与
        /// `Shell/DeckInfoPopup.cs` 那条费用曲线同一个口径）。</para>
        ///
        /// <para>⚠️ **重建前必须先撤掉上一次那 9 行**：`CostCurveDrawer.Build` 只建不删
        /// （它的形参 `rowName` 缺省 = 原版节点名 `Deck CostQuanityt Row Drawer`，拼写照抄原版），
        /// 连调两次会长出第二套、叠在一起（静默 —— 数字看着还是对的）。</para></summary>
        void RefreshCostRows()
        {
            if (_general == null) return;
            const string rowName = "Deck CostQuanityt Row Drawer";   // = `CostCurveDrawer.Build` 的缺省 `rowName`（原版节点名）
            for (int i = _general.childCount - 1; i >= 0; i--)
            {
                var ch = _general.GetChild(i);
                if (!ch.name.StartsWith(rowName, System.StringComparison.Ordinal)) continue;
                // ⚠️ 运行期 `DestroySafe` 走的是 `Object.Destroy`（**帧末才真销毁**）⇒ 不先关掉的话，
                //    这一帧里新旧两套 9 行同矩形、同队列会叠着画（z-fighting 闪一下）。先 `SetActive(false)`。
                ch.gameObject.SetActive(false);
                RewardsWindow.DestroySafe(ch.gameObject);
            }
            var counts = CostCurveDrawer.Counts(CurrentDeck, CollectionData.Card);
            // 🆕 2026-10-22（M2）：柱子的队列 `QPrRow`→`QPrFront` —— 费用曲线那 9 行**压在立绘之上**
            //    （实测多张 `UI_Deck_Warlord_*` 在柱区 alpha=255；原版兄弟序里 `General container`
            //     排在 `Character Image` 之后）。文字那一档照旧 `QPrText`（它已高于 `QPrFront`）。
            CostCurveDrawer.Build(_general, CostDrwCX, CostDrwCY, CostDrwScl, counts, QPrFront, QPrText, rowName);
            CostRows.Clear();
            CostRows.AddRange(counts);
            Debug.Log("[Practice] 费用曲线画了 9 行（" + CostCurveDrawer.Dump(counts) + "）");
        }

        /// <summary>🆕 **2026-10-19（界面线 P05）**：原版 `DeckGeneralInfoDemo` 的 `changePlayerDeck` 显隐 ——
        /// **演示（预组）卡组** ⇒ `ShowDemoDeckInfo` 里 `SetActive(false)`；**玩家自己的卡组** ⇒ `SetContent`
        /// 里 `SetActive(true)`（两处都反编译实读）。三件一起切（底图 / 字 / 命中区）——
        /// ⚠️ 命中区不关的话，看不见的钮照样吃点击（静默错）。
        /// <para>为什么要跟 `Lore Text` 一起管：两件的框**重叠**（见上），原版只靠这条显隐避开。</para></summary>
        void ShowChangeDeck(bool show)
        {
            if (_chgBg != null) _chgBg.gameObject.SetActive(show);
            if (_chgText != null) _chgText.gameObject.SetActive(show);
            if (_chgHit != null) _chgHit.gameObject.SetActive(show);
        }

        /// <summary>`Show Deck Content` —— **开「卡组详情」窗**（原版 `DeckInfoContext(deck, 2, …)`）。
        /// 🔴 **2026-10-04（§三第29条 A65①）这一行接上了**：`state` 必须传 **`View`(2)** ——
        ///   判据 = `DeckGeneralInfoDemo__CardInDeckInfoButtonOnClick.c` 的
        ///   `DeckInfoContext___ctor(uVar3, deck, **2**, 0, 0, 0, 0)`（第 3 个实参就是 `context.state`）。
        ///   原来走默认值 `Edit`(0) ⇒ 那一扇会**多显示 `Edit Deck` + 4 颗圆钮**
        ///   （`DeckInfoControls__Initialize` 的显隐表：`Edit Deck` ← `state &lt; 2`、四颗圆钮 ← `state == 0 &amp;&amp; isPlayerDeck`）。
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
            DeckInfoPopup w;
            var pre = PickedPrebuilt;
            if (pre != null)
            {
                // 🔴 **2026-10-21（同一批真机缺陷）**：预组**不在 `DeckLibrary` 里** ⇒ 原来这里传 `DeckIndex`
                //   （预组档 = **−1**）开出来的是一扇**空窗**（没有督军、没有卡表、费用曲线全 0）。
                //   现在把预组那份数据喂进去 —— `OverrideDeck` + `OverrideInfo` 是 `DeckInfoPopup` 给
                //   「外面送进来的卡组」留的**唯一那个口**（`CurDeck` / `CurInfo` 只此一处判源，
                //   见 `Shell/DeckInfoPopup.cs` 那两条 doc）；预组那一支的用法与
                //   `DeckSelectionPopup.UpdateInfo` 逐句相同。
                //   ⚠️ **不**走 `CreateForPicked`：那一支是「在选卡组窗里点行预览」那条，它强制
                //     `SelectButtonProvided = true`（会多出一颗 `Select Deck`）；而原版这颗 `Show Deck Content`
                //     开的是 **`DeckInfoContext(deck, 2(View), selectButton = null, …)`**（判据见文件头）
                //     ⇒ 保持 `Create(...)` 那条老路，**只换数据**。
                //   `IsPlayerDeck = false` = 原版 `InventoryManager.HasItem(context.Deck)` 对预组为假
                //   ⇒ `Practice Deck` / `Share` / `Delete` / `Duplicate` 全按判据藏（预组没有库下标，
                //   那几颗点了也无从下手 —— 原来那颗永远灰着的「练习」钮就是这么来的）。
                w = DeckInfoPopup.Create(Manager, -1, DeckInfoPopup.DeckInfoState.View);
                w.OverrideDeck = PrebuiltDecks.ToPlayerDeck(pre);
                // 🔴 **2026-10-21（审查代理 #10）**：显示信息**复用** `DeckSelectionPopup.InfoOf`（它已经是
                //   「预组 → `DeckInfo`」唯一那一份，选卡组窗的格子就用它）—— 我原来又写了一份
                //   `InfoOfPrebuilt`，两处同一条规则。
                //   ⚠️ 它**不带 `GameMode`**（= 0）：`DeckInfoPopup` 里读 `CurInfo.GameMode` 的只有
                //   `SelectPracticeOpponentDeck` 的 `modeFilter` 那一处，而那颗 `Practice Deck` 钮按
                //   `IsPlayerDeck = false` 已经藏了、走不到 ⇒ 这里是**够用的**（审查代理同结论）。
                w.OverrideInfo = DeckSelectionPopup.InfoOf(pre);
                w.IsPlayerDeck = false;
            }
            else
            {
                w = DeckInfoPopup.Create(Manager, DeckIndex, DeckInfoPopup.DeckInfoState.View);
            }
            Manager.OpenWindow(w);
            LastDeckInfo = w;
            Debug.Log("[Practice] `Show Deck Content` ⇒ 开 `Deck info Popup`"
                      + "（原版 `cardInDeckInfoButton → OpenWindow(new DeckInfoContext(deck, 2, …))`）"
                      + " · state = **View(2)**"
                      + (pre != null ? " · 数据 = **预组「" + pre.DisplayName + "」**（`OverrideDeck` 那一支）" : ""));
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

        /// <summary>当前选中的卡组（默认 = `DeckLibrary` 的当前那套）。
        /// ⚠️ **只在「我的卡组」那一档有意义**：左列现在列的是**练习对**（`PrebuiltDecks.Pairs`），
        /// 选一对 ⇒ `PickPrebuilt` 会把它显式写成 **−1**；只有走 `Change Deck` 选了玩家自己的卡组才会 ≥ 0
        /// （⛔ 别拿它判「当前是哪副」，见 <see cref="CurrentDeck"/>）。</summary>
        public int DeckIndex;

        /// <summary>🆕 **2026-10-22**：左列**当前选中的那一对**（`PrebuiltDecks.Pairs` 的下标；−1 = 没选 / 选的是「我的卡组」）。
        /// 它只回答「**哪一格**亮着」；「当前是哪一副牌」仍然只有一个判源 <see cref="CurrentDeck"/>。</summary>
        public int PairIndex = -1;

        /// <summary>当前选中的那一对（自检用；`PairIndex` 越界 / −1 ⇒ null）。</summary>
        public PrebuiltDecks.Pair PickedPair
        {
            get
            {
                var ps = PrebuiltDecks.Pairs;
                return PairIndex >= 0 && PairIndex < ps.Count ? ps[PairIndex] : null;
            }
        }

        /// <summary>🆕 **2026-10-22**：左列**逻辑上一共几格** = 当前（模式 + 阵营）筛选下能列出来的**对数**。
        /// ⚠️ 与 <see cref="DeckRows"/>`.Count` **不是一回事**：后者只装**视口里那几格**
        /// （`RebuildDeckRows` 里 `MenuDraw.VisibleAbove` 那道粗筛，78 对只建得出 8 格）；
        /// 本属性 = 原版 `Content` 上 `ContentSizeFitter` 撑出来的那批子件数。</summary>
        public int ListedDeckCount { get { return FilteredDeckCount(); } }
        /// <summary>在「预组卡组」那一页选中的那副（空 = 用的是「我的卡组」）。
        /// 🔴 **2026-10-22 就地订正（铁律 5）**：这里原来写「**还不能拿去开战** —— 见 `PrebuiltDecks.WarnNotPlayableYet`」——
        /// **两句都已过期**：那个方法 2026-09-26 就删了（`PrebuiltDecks.cs` 里留着说明），开战通道
        /// `PrebuiltDecks.SetPendingBattleDeck` / `TakePendingBattleDeck` 也已经接上了。
        /// 它现在只回答「哪一副预组」，**「是哪一格」看 <see cref="PairIndex"/>**。</summary>
        public PrebuiltDecks.Deck PickedPrebuilt;
        /// <summary>🔴 **2026-10-21：本面板「当前是哪一副」的【唯一判源】。**
        ///
        /// <para>面板上**凡是问「当前是哪一副」的地方一律走它**（阵营标题 / 费用曲线 / 卡表 /
        /// 阵营列那颗 `Has Player Deck` 火漆印 / `Battle!` 的督军闸 / `Show Deck Content` 开的详情窗）——
        /// ⛔ 别再直接读 <see cref="DeckIndex"/>：**预组不在卡组库里**，预组档 `DeckIndex` 恒为 −1，
        /// 直接读它 = 拿到「一副空卡组」（`CollectionData.Raw(-1)` / `DeckAt(-1)` 都是空的）。</para>
        ///
        /// <para>🔴 **为什么非有这一处不可**（真机缺陷 · 2026-10-21）：`PickPrebuilt` 原先是**只刷一半**的
        /// 增量刷新（卡组名 / 督军 / 阵营图 / 卡背 / lore），而面板另外四处读的是 `DeckIndex`（此时 −1）
        /// ⇒ 选完预组之后：阵营标题回到「（未选阵营）」· 费用曲线 9 行全 0 · 卡表不建 ·
        /// 点 `Battle!` 被「这套卡组还没有督军，开不了局。」挡死。
        /// **两处各判一次「当前是哪副」= 迟早不一致**（`CLAUDE.md` §三）。</para>
        ///
        /// <para>⚠️ 预组这一支走 `PrebuiltDecks.ToPlayerDeck`（每次**新造**一份只读视图，含我们补的防御卡；
        /// 它**不写回**任何数据）—— 面板上用得起；⛔ 别拿它去 `CollectionData` 那套「按库下标」的动作。</para>
        /// <para>⚠️ 它与「本局用这副牌」那条**跨场景通道**（`PrebuiltDecks.PendingSource`）是**两件事**：
        /// 这里是**面板显示态**，那条是**交给战斗场景**的；两者由 `PickPrebuilt` / `PickDeck` / `ResetContent`
        /// **成对**写入与清空（见那三处）。</para></summary>
        public RuleEngine.PlayerDeck CurrentDeck
        {
            get
            {
                if (PickedPrebuilt != null) return PrebuiltDecks.ToPlayerDeck(PickedPrebuilt);
                return CollectionData.Raw(DeckIndex);
            }
        }

        /// <summary>督军 id → 阵营（**与 `CollectionData.DeckAt` 里那条推导同一个**：督军卡身上的 `Faction`）。
        /// 预组没有库下标 ⇒ 只能这样取；⛔ 别在这里另立一套阵营判据。</summary>
        static string FactionOf(string warlordId)
        {
            var hero = string.IsNullOrEmpty(warlordId) ? null : CollectionData.Card(warlordId);
            return hero != null ? hero.Faction : "";
        }

        // ============================================================ 🆕 2026-10-23（W66）：开窗的阵营筛选默认值

        /// <summary>原版 `PracticeModePopup__Open` 末尾那条 `ChangeSelectedArmy(...)` 的**回落阵营**。
        ///
        /// <para>判据（第一权威 = 反编译方法体）`d:/2/tools/decomp_full/PracticeModePopup__Open.c:172-180`：
        /// 活动那副牌为空 ⇒ `:174` **`uVar8 = 10`**；有牌 ⇒ `:178` `uVar8 = 牌.deckHero(+0x40).cardArmy(+0x2c)`
        /// （= **牌里督军卡身上的阵营**，⛔ 不是 `CardDeck.deckArmy(+0x50)` —— `dump.cs` 的 `CardDeck` 字段表里两个都在，
        /// 反编译读的是前者）；随后 `:180` `ChangeSelectedArmy(self, uVar8)`。</para>
        ///
        /// <para>**`10` 是什么档**：`CardArmy.Ultramarines` —— 枚举值是**每档 +10**
        /// （`d:/2/tools/il2cpp_out/dump.cs` 的 `public enum CardArmy`：`Neutral = 0` · **`Ultramarines = 10`** ·
        /// `Goff = 20` · `SaimHann = 30` · `Sautekh = 40` · `BlackLegion = 50` · `Leviathan = 60` ·
        /// `TauEmpire = 70` · `Sororitas = 80` · `Genestealers = 90` · `AstraMilitarum = 100` ·
        /// `DarkAngels = 110` · `EmperorsChildren = 120` · `SpaceWolves = 130`）。
        /// 本工程同一映射的另一处旁证：`BattleLogData.ResolveArmy("Uriel Ventris") == 10`（`Editor/MainMenuScene.cs` 那条断言）。
        /// ⚠️ 阵营格是**按 `Factions()` 的下标**编号的 ⇒ 这一档存**阵营名**，用的时候换下标（见 <see cref="DefaultArmyIndexOf"/>）。
        /// ⛔ 别把 `10` 直接当 `ArmyIndex` —— 那是「第 10 格」（= `DarkAngels`）而不是 Ultramarines。</para></summary>
        public const string DefaultArmyFaction = "Ultramarines";

        /// <summary>`Open()` 那条默认筛选：**你的卡组督军所在阵营在 `Factions()` 里的下标**；
        /// 你的那份阵营查不到（没有卡组 / 没有督军 / 阵营不在那 13 格里）⇒ <see cref="DefaultArmyFaction"/> 那一格。
        /// <para>⚠️ 原版只在一个条件下回落到 `10`：**活动那副牌本身为空**（`Open.c:172-174`）。
        /// 「牌在、督军为空」在原版走的是 `:177` 那句 `goto`（反编译里落到 `LAB_18080e960` 的异常/提前返回，
        /// **不会**调 `ChangeSelectedArmy`）。我们本地**只有一份当前卡组**、没有 liveops 事件那副牌
        /// ⇒ 「没有可用的牌」= 上面那三种情形，一律按**最接近的语义**回落到 `10` 并出声，⛔ 不用本窗的 `-1`
        /// （那一档原版根本没有）。</para></summary>
        int DefaultArmyIndex()
        {
            var facs = CollectionWindow.CardsState.Factions();     // 与 `BuildArmySelector` 里 `_facs` 同一份来源
            var deck = CurrentDeck;
            string fac = FactionOf(deck != null ? deck.WarlordId : null);
            int i = DefaultArmyIndexOf(fac, facs);
            if (i < 0)
                Debug.LogWarning("[Practice] `Factions()` 里连回落阵营 `" + DefaultArmyFaction
                                 + "` 都找不到（卡池数据残缺）⇒ 开窗不筛阵营（如实出声）");
            else if (string.IsNullOrEmpty(fac) || !DeckRules.SameFaction(fac, facs[i]))
                Debug.Log("[Practice] 开窗阵营筛选回落 = `" + facs[i] + "`（原版 `Open.c:174` 的 `10`；"
                          + "当前卡组没有可用阵营：" + (string.IsNullOrEmpty(fac) ? "<无卡组/无督军>" : fac) + "）");
            return i;
        }

        /// <summary>自检口（纯函数，无窗状态）：阵营名 → 阵营格下标。
        /// 命中（按 `DeckRules.SameFaction`，与本窗其它几处同一个相等）⇒ 那个下标；
        /// 未命中 / 空 ⇒ <see cref="DefaultArmyFaction"/> 的下标；连它都不在 ⇒ −1。</summary>
        public static int DefaultArmyIndexOf(string faction, IList<string> facs)
        {
            if (facs == null || facs.Count == 0) return -1;
            if (!string.IsNullOrEmpty(faction))
                for (int i = 0; i < facs.Count; i++)
                    if (DeckRules.SameFaction(faction, facs[i])) return i;
            for (int i = 0; i < facs.Count; i++)
                if (DeckRules.SameFaction(DefaultArmyFaction, facs[i])) return i;
            return -1;
        }

        /// <summary>`Selected Army Title` 现在显示的那句话（自检用）。
        /// ⚠️ 读的是**真 `Label` 上那句**（⛔ 不是重新算一次 `SelectedArmyName()` —— 那会与实现同一个口 = 自证）。</summary>
        public string ArmyTitleText { get { return _txtArmy != null ? _txtArmy.Text : ""; } }
        /// <summary>🆕 2026-10-03：本局**对手**卡组（`Deck info Popup` 的 `Practice Deck` 那条链选出来的那一副）。
        /// `null` = 没指定 ⇒ 照旧自动凑 —— 这正是原版练习窗自己那条 `Battle!` 的行为
        /// （`PracticeModePopup__BattleButtonOnClick.c:225`：`enemyDeck` 传的是 **0 / null**）。</summary>
        public PlayerDeck OpponentDeck;
        /// <summary>🆕 **2026-10-15（A383）**：本局的 **`PlayModes` 号**（原版 `IPlayEvent.EventPlayMode`）。
        ///
        /// <para>练习窗自己那条链 = **`OfflinePractice 6`** —— 判据 = 原版
        /// `PracticeEvent.get_EventPlayMode`（VA **0x1808B66B0**，3 字节常量返回；读数 →
        /// `资料/普查产出_1014/RO_战场与窗口判据三件.md` §二 ①·B·6）。
        /// 🔴 **`Deck info ▸ Practice Deck` 那条链是另一档** —— `OwnDeckTraining 12`
        /// （判据 = `DeckInfoPopup__StartPracticeMatch.c` 里调的 `StartMatch(0xc, …)`，见
        /// `DeckInfoPopup.StartPracticeMatch`）。它借本窗当开战宿主，由
        /// `StartPracticeMatch(…, playMode)` 覆写这一格 ⇒ **别在这里硬判成 6**。</para>
        ///
        /// <para>它跟 <see cref="OpponentDeck"/> 一样要**跨场景**带给对局：放在 `BattleDriver` 的
        /// `SetPendingPlayMode` 静态通道里（时机 = `StartBotBattle`，见那一处）。</para></summary>
        public GameMode PlayMode = GameMode.OfflinePractice;
        /// <summary>当前阵营筛选 = **`Factions()` 的下标**（⛔ 不是 `CardArmy` 的值 —— 那个是 10/20/…/130）。
        /// <para>🆕 **2026-10-23（W66）**：`Open()` 现在按原版写**真值**（你的卡组阵营，或 `Ultramarines`），
        /// 见 <see cref="DefaultArmyIndex"/>。`-1`（= 「不限阵营」）**原版没有这一档** ——
        /// 今天只剩两个来路：① 自检里显式摆位（下面那几段按 79 格的基线跑）；② 数据残缺时的兜底
        /// （`Factions()` 里连 `Ultramarines` 都没有 —— 正常数据不会发生，且会出声）。</para></summary>
        public int ArmyIndex = -1;              // `Factions()` 的下标；-1 = 不限（原版没有这一档，见 doc）
        public readonly List<Transform> DeckRows = new List<Transform>();
        public readonly List<Transform> ArmyCells = new List<Transform>();
        public readonly List<Transform> CardRows = new List<Transform>();
        public MenuScroll DeckScroll, ArmyScroll;
        /// <summary>`Battle!` 真的切了场景没有（自检用 —— 批处理下不切）。</summary>
        public bool StartedBattle { get; private set; }

        public Transform Hit(string name) { return transform.Find(name); }
        public Transform BtHit { get { return transform.Find("BattleHit"); } }
        public Transform BackHit { get { return transform.Find("BackHit"); } }
        // 🔴 **2026-10-17（第四十八会话 · 桶 G3）**：`ToggleHit` 挂在一个叫 `Toggle` 的**中间节点**上
        //   （`:955` `New(root, "Toggle", …)` → `:990` `HitOn(tog, tog, "ToggleHit", …)`）⇒
        //   `transform.Find` **只查直接子件**，写 `"ToggleHit"` 恒 null（自检报「命中区没建出来」）。
        public Transform TogHit { get { return transform.Find("Toggle/ToggleHit"); } }
        public Transform ChgHit { get { return transform.Find("ChangeDeckHit"); } }

        public static PracticeModePopup Create(WindowsManager mgr)
        {
            // 🔴 **2026-10-11（A218）**：窗口根是 `RectTransform` ＋ 写 `sizeDelta`。
            //    判据 = 原版同名 prefab 实读：`Practice Mode Menu` 的 `RectTransform`
            //    `anchor (0,0)-(1,1)` · `sizeDelta (0,0)` · pivot (0.5,0.5) · **绝对矩形 (0,0)-(1920,1080)**
            //    （`bundle_menus_assets_all`，2026-10-11 现读）⇒ 整屏矩形。
            //    ⚠️ 原版 stretch 拿父（Canvas）尺寸；我们用「重合锚点 + 屏尺寸」表达同一个矩形
            //    （锚点不复刻，见 `MenuDraw.SetPxSize`）。
            //    ⚠️ 父链缩放的档：本窗 `extraScaleSmallScreen = 1.07`，但**小屏开关出厂关**且
            //    `WindowsManager.AttachToAnchor` 把窗根写成 `localScale = one` ⇒ 今天父链是**单位缩放**
            //    （开关真开时缩放加在**窗根**上 ⇒ 窗内每件的世界尺寸自动 ×1.07，`sizeDelta` **不该**再折算）。
            //    改坏法：删掉 `SetPxSize` 那句 ⇒ `Editor/MainMenuScene.cs` §A218「练习窗根 = 整屏矩形」红。
            var go = new GameObject("Practice Mode Menu", typeof(RectTransform));
            MenuDraw.SetPxSize(go.transform, LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);
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
            // 🔴 **2026-10-21（审查代理 #3）**：**开窗先清「预组」这一档** —— 原来只重置 `DeckIndex`、
            //   把 `PickedPrebuilt` 留在上一窗口那副上 ⇒ 同一实例再 `Open()`（= `Build()` 重建）会变成
            //   「头显示自建那副（`BuildDeckInfoHead` / `BuildGeneralContainer` 读 `DeckIndex`）、
            //   体显示预组（`CurrentDeck` 优先 `PickedPrebuilt`）」的**混合态**。
            //   清掉之后，下面 `Build()` 期间 `PickedPrebuilt == null` ⇒ `CurrentDeck` ≡ `Raw(DeckIndex)`，
            //   那两处按库下标读的老代码**与唯一判源逐位一致**（所以不必再改它们）。
            //   通道**一并作废**：原版 `PracticeModePopup__Close` 第三句就是 `ResetContent()`
            //   （`decomp_full/PracticeModePopup__Close.c` 三句实读）⇒「关窗/再开 = 选择归零」是原版行为；
            //   留着通道会出现「闸验自建、发牌发预组」（审查代理 #11 那条）。
            PickedPrebuilt = null;
            PairIndex = -1;                  // 🆕 2026-10-22：左列那一格的选中态一并归零（同 `DeckIndex`）
            PrebuiltDecks.ClearPendingBattleDeck();
            DeckIndex = CollectionData.CurrentIndex();
            // 🔴 **2026-10-23（W66）：开窗的阵营筛选 = 原版 `Open` 末尾那条 `ChangeSelectedArmy(...)`。**
            //   判据（`d:/2/tools/decomp_full/PracticeModePopup__Open.c:172-180`，逐句）：
            //     `:172` `CardDeck__op_Inequality(活动那副牌)` ⇒ `:174` **空 ⇒ `uVar8 = 10`**
            //     ⇒ 否则 `:178` `uVar8 = 牌.deckHero(+0x40).cardArmy(+0x2c)` ⇒ `:180` `ChangeSelectedArmy(self, uVar8)`。
            //   `10` = `CardArmy.Ultramarines`（枚举值每档 +10，见 `DefaultArmyFaction` 的 doc）。
            //   改前这里是 `ArmyIndex = -1`（本窗自加的「不限」档）⇒ 开窗左列 **79 格**；
            //   照原版之后 = **该阵营 6 对 + 首格「你的卡组」= 7 格**。
            //   ⚠️ 顺序与原版的差别（如实记）：原版 `ConfigureArmyFilterButtons` **先**建格、`ChangeSelectedArmy`
            //     **后**设筛选并刷高亮；我们在 `Build()` 之前就把值算好 ⇒ 首次绘制直接就是终态
            //     （原版中间那一帧没有高亮，我们连那一帧都没有）。**最终可见状态逐项相同**。
            //   ⛔ 别改回 `-1`：那会让「开窗 = 全部 78 对」既是**与原版不符**、又让下面那批 79 格的数字回魂。
            ArmyIndex = DefaultArmyIndex();
            // 🔴 **2026-10-22（`D1`）就地订正（铁律 5）：`Game mode` 出厂值 = 原版 `Skirmish(13)`。**
            //   判据 = 原版 `PracticeModePopup__ctor.c` 的**首句** `*(undefined4 *)(param_1 + 0x110) = 0xd;`
            //   （`d:/2/tools/decomp_full/PracticeModePopup__.ctor.c`，本批亲读）—— 与 prefab 那颗
            //   `EverguildToggle.m_IsOn = 0`（false ⇒ 13，见 `GameModeValueChange`）自洽。
            //   ⚠️ **原来这里写的是**「初值 = 当前选中卡组自己的模式」，并把它记成一处在 doc 里的
            //   「主动偏离」—— **已按原版改掉**（理由与更正痕迹 → `SelectedGameMode` 的 doc）。
            //   ⚠️ **这条只决定「没有外部指定」时的初值**，两条显式路径**不受影响**：
            //     · `StartPracticeMatch(…)`（`Deck info ▸ Practice Deck`）= 在 `OpenWindow` **之后**写它；
            //     · 玩家点开关 = `SetGameMode`（原版 `GameModeValueChange`）。
            //   必须在 `Build()` **之前**：`BuildGameModeToggle` 要按它画底图、`RebuildDeckRows` 要按它筛。
            SelectedGameMode = GameMode.Skirmish;
            Build();
        }

        /// <summary>🔴 **2026-10-21（审查代理 #11）**：关窗时把**本窗这一档选择**归零（面板 + `PrebuiltDecks` 通道）。
        ///
        /// <para>判据 = 原版 `PracticeModePopup__Close` 的**三句**（`decomp_full/PracticeModePopup__Close.c` 实读）：
        /// `GameWindow.Close()` · `SaveSelectedDeck()` · **`ResetContent()`** —— 第三句就是「选择归零」。
        /// 我们这边与 `SaveSelectedDeck` 对应的那一步 = **选定时就写进 `PrebuiltDecks` 通道**（`PickPrebuilt`），
        /// 所以关窗这里只需补 `ResetContent` 的等价物。</para>
        ///
        /// <para>🔴 **为什么非清不可**：那条通道是**全静态**的，`Shell/LiveOpsEventWindow.cs` 的
        /// `StartBotBattle` 也读它（`:1179` 附近）。关掉练习窗之后若还留着上一次挑的预组，
        /// 下一场（排位 / 遭遇 / 再开练习窗）就会**闸验自建那副、发牌发预组那副** —— 全静默。</para>
        ///
        /// <para>⚠️ **只有一种情况不许清**：`StartedBattle` —— `StartBotBattle` 已经把「本局用哪副」
        /// 定下、正要切场景，而**跨场景那一次读在 `BattleDriver` 里**（`TakePendingBattleDeck()`），
        /// 清掉就等于「这一局打的是别的牌」。
        /// ⚠️ 匹配中（`SearchingMatch`）**不豁免**：关掉本窗 = 那 12 秒的 `Update` 再也不跑
        /// （`TickSearch` 挂在 `Update` 上）⇒ 这一局本来就不会开打，通道留着只会漏到下一场去。
        /// ⚠️ 原版「关窗重开能恢复上次选的牌」是靠 `SaveSelectedDeck` → `PlayerDataManager.SetDemoDeck`
        /// 再接 `LoadSavedDeck`（两个方法体都在本地，见 `decomp_full`）；**我们没做那两条口**
        /// ⇒ 这一档如实退化成「关窗 = 归零」（并且出声，不假装记住了）。
        /// 🔴 **2026-10-23 补一条查证（这条支持「关窗=归零」与原版结果一致）**：全量反编译里
        /// **`PracticeModePopup__LoadSavedDeck` / `__IsSavedDeck` / `__PlayerDeckChanged` 三个方法全是 0 调用点**
        /// （`grep -rl "PracticeModePopup__LoadSavedDeck" d:/2/tools/decomp_full/*.c` 只命中它自己）
        /// ⇒ 这个 build 里**「记住上次选的那副」根本没有读回那一环**（写进去的 `SetDemoDeck` 只喂
        /// `PlayerDataManager+0x318`，而这个窗自己从不读它）⇒ 原版**关窗重开也不会恢复**，
        /// 「关窗 = 归零」不是我们独有的偏离。⛔ 别按旧注释去补一条原版都没有的「记住」。</para></summary>
        public override void Close()
        {
            // 🆕 **2026-10-26（批次 6 · G15）：原版 `Close` 的第二句 `SaveSelectedDeck()`**
            //   （判据 = `cpp2il/.../PracticeModePopup.cs:1096-1101`：`base.Close(); SaveSelectedDeck(); ResetContent();`）。
            //   🔴 **必须排在那段「选择归零」之前**（原版相对次序 = 先存后清；倒过来就永远存到 null）。
            //   ⚠️ 我们的 `base.Close()` 仍在**最后**（窗的可见性收尾要在状态收干净之后）——
            //      相对次序差**不可观测**（`SaveSelectedDeck` 只读 `PickedPrebuilt`，而它下面就被清掉了）。
            SaveSelectedDeck();
            if (!StartedBattle)
            {
                if (PickedPrebuilt != null)
                    Debug.Log("[Practice] 关窗 ⇒ 预组选择归零（「" + PickedPrebuilt.DisplayName + "」，"
                              + PickedPrebuilt.deckId + "）—— 原版 `__Close → SaveSelectedDeck + ResetContent`；"
                              + "⚠️ 我们没做 `SaveSelectedDeck`/`LoadSavedDeck` 那两条口 ⇒ 重开窗不会恢复它");
                PickedPrebuilt = null;
                PairIndex = -1;          // 🆕 2026-10-22：左列那一格的选中态（同 `PickedPrebuilt` 一起作废）
                PrebuiltDecks.ClearPendingBattleDeck();
            }
            if (!string.IsNullOrEmpty(_charImgWarlordId)) { CardArt.ReleaseFullHeroSprite(_charImgWarlordId); _charImgWarlordId = null; } base.Close();   // 账 `战斗逻辑驱动:1280`：原版 `PlayerDeckDemoDeckInfoWrapper__UnloadCharacterImage` 由 `DeckGeneralInfoDemo.OnDisable` 调（`out_cpp2il_dev_ilspy/DeckGeneralInfoDemo.cs:67-74`）= 本窗隐藏/关窗那一刻 ⇒ 放掉本窗那张督军整身立绘；与 `base.Close()` 合并写 = **零行数变化**
        }

        void Build()
        {
            // 🔴 **2026-10-18（A867 · S1）**：**首句**就把本窗名下的旧滚动区撤掉 —— 这一句下面马上会把
            //   `root` 的子件全清掉（下一次 `BuildArmySelector` / `BuildDeckRows` 又各登记一颗**新** `MenuScroll`）。
            //   ⛔ **兜不住**，所以非补不可（`PointerLayer` 的两道自动清理**都判不出这里已死**）：
            //     · `PruneScrolls` 的 `s == null` —— `MenuScroll` 是**普通 C# 类**（`Shell/MenuScroll.cs` 的 `MenuScroll`）
            //       ⇒ 节点被销毁**不会**让它变 null ⇒ **恒假**；
            //     · `s.Owner == null` —— `Owner` 指的是**活下来的窗根**（本方法只删**子件**，`transform` 本身还在，
            //       而且我们关窗是 `SetActive(false)`、**不销毁**）⇒ 也**恒假**（`Shell/PointerLayer.PruneScrolls`）。
            //   ⇒ 少了这一句 = **每重建一次净涨 1~2 条**，而且旧条目**还能被滚轮命中**（`OnChanged` 指向已销毁的节点）。
            //   ✅ 形状照抄 `Shell/InboxWindow.BuildMessageList`（重建前先撤）—— 本方法里那句
            //   `UnregisterOwnedBy` 必须在 `DestroySafe` **之前**（清完子件再撤也行，但要与登记的顺序成对）。
            PointerLayer.UnregisterOwnedBy(gameObject);
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);
            DeckRows.Clear(); ArmyCells.Clear(); CardRows.Clear();
            // 🆕 2026-10-22（M2）：上面那圈把**旧子件**全撤了 —— `Character Image` 那一颗的引用也要一并作废。
            //   ⚠️ 不置空的话，`RefreshCharacterImage` 在**同一帧**里（`StartPracticeMatch` = `OpenWindow`
            //   紧接着 `PickDeck`）会拿到**已排进销毁队列、但还没真销毁**的旧 quad（`== null` 此刻仍为假）。
            _charImg = null;

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
            // 🔴 **2026-10-22（M2）**：图标**必须在钮面之上** —— 钮底图 `UI_Button_Round_background` 中心是
            //    **不透明纯黑**，同档会把它整个盖掉（真机截图里这颗就是纯黑圆盘）。见 `QPrFront` 的 doc。
            var backIc = Img(root, "40k_UI_bt_back", BackIcL, BackIcT, BackIcR, BackIcB, "Back Icon", QPrFront, true);
            // 🆕 **A1205 接上**（`R5` §2·H #51：原版 `Back button ▸ Text` = `auto[1~45] · base 31.9 · 折行 1`）
            // 🔴 **2026-10-19（`A1501`）：文案改走 `Loc.T`** —— 原版那颗 TMP 上挂着一颗 I2 `Localize`，
            //    `mTerm = MainMenu/MainButtons/ButtonLabel/Back`（本轮现读：把 `Practice Mode Menu` 整棵
            //    129 个节点逐颗打印 `mTerm` / `m_text` 得到；该键**表里已有** ⇒ 不再新增）。
            //    原来直接画资产里的 `m_text`（`Back`）⇒ 中文档永远出英文。
            //    ⚠️ 键名⛔ 不许自拟（否则查不到 ⇒ 界面上印键名，见 `Core/Loc.cs` 的 `T` doc）。
            Txt(root, Loc.T("MainMenu/MainButtons/ButtonLabel/Back"), BackTxL, BackTxR, BackT, BackB, 45f, Align.Left, "Back Text", QPrText,
                wrapPx: BackTxR - BackTxL, autoMinPx: 1f, autoMaxPx: 45f, autoBasePx: 31.9f,
                face: TmpFont.Face.LatinCardBody);
            // 🆕 A17：原版 `Practice Mode Menu` 的 `Back button` 是 SpriteSwap（普查 §块 5 第 21 行）
            var backHit = Hit(root, root, "BackHit", new PxRect(BackL, BackT, BackR, BackB), () => Close());
            var backWb = backHit != null ? backHit.GetComponent<WindowButton>() : null;
            if (backWb != null) backWb.Bind(backIc, "40k_UI_bt_back");
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 C1）**：原版 `Practice Mode Menu/Back button` = 空 cue +
            //   **`useDefaultAudioClipIfNull = 0`** ⇒ **静音**（关窗本身那一声由 `GameWindow.Close()` 出）。
            if (backWb != null) backWb.playClickSound = false;

            // 3) 选卡组那一列
            // ⚠️ 原版这一件是 **Sliced**（贴图 439×664 · border (0,325,0,35)）—— 原来按 Simple+keepAspect 拉的
            Nine(root, root, "UI_Deck_Selection_Back", new Vector4(0f, 325f, 0f, 35f), 439f, 664f,
                 DbtnL, DbtnT, DbtnR, DbtnB, QPr, "Deck Buttons");
            // 🆕 **2026-10-06（A94）：选卡组那一列的面板底图吸收点击**。判据 = 原版 prefab
            //   `Practice Mode Menu > Deck Selector > Deck Buttons > Generic Window Red Background Small`
            //   那颗 `Image` 的 **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读）—— 射线打到它自己、
            //   父链上没有点击处理器（关窗那颗 `BackgroundCloseButton` 在压暗层上）⇒ 原版**什么都不做**。
            MenuDraw.Absorb(root, "AbsorbHit", new PxRect(DbtnL, DbtnT, DbtnR, DbtnB), QPr, QPrHit);
            // 🆕 **A1205 接上**（`R5` §2·H #52：原版 `Deck Selector ▸ Deck Buttons ▸ tooltip`
            //    = `auto[1~38] · base 23.2 · 折行 1`）
            // 🔴 **2026-10-19（`A1501`）：文案改走 `Loc.T`** —— `tooltip` 那颗 TMP 的
            //    `mTerm = Demo/DeckSelectionDemo/SelectDeck`、`m_text = 'Select deck to play'`（本轮现读）。
            Txt(root, Loc.T("Demo/DeckSelectionDemo/SelectDeck"), TipL, TipR, TipT, TipB, 38f, Align.Center, "tooltip", QPrText,
                wrapPx: TipR - TipL, autoMinPx: 1f, autoMaxPx: 38f, autoBasePx: 23.2f,
                face: TmpFont.Face.LatinCardBody);
            BuildArmySelector(root);
            BuildDeckRows(root);

            // 4) 右半：卡组信息（`Deck info` 容器 → 红底 + 卡组名/督军名/阵营图 + 两个互斥抽屉）
            //    ⚠️ 红底**不是** `Deck info` 自己那一层画的（见 `BuildDeckInfo` 的注释）
            BuildDeckInfo(root);

            // 5) 底下那一条：`Game mode` 开关 + `Battle!`
            //    🔴 **2026-10-17（U4-1）**：原来这里把 **89×40 的开关底图**按 `keepAspect` 拉进整框
            //    293.51×57.84、文字也画在整框上，**两颗 94.64² 模式图标 + 名牌底一个都没建**、点了只 `NotBuilt`。
            //    现在四件按原版逐值建全并接上 `SetGameMode`（判据 = 本文件常量段 `Gm*` 那一组）。
            BuildGameModeToggle(root);
            // 🆕 **2026-10-26（批次 6 · G15）：原版 `Awake` 的末句 —— 开关的显隐闸**。
            //   判据 = `cpp2il/out_cpp2il_dev_ilspy/PracticeModePopup.cs:95-100`（`Awake` 逐句）：
            //     `gameModeSelector.gameObject.SetActive(LiveOpsManager.GetHandler<GameModes>()
            //        .GetActiveEvent<RankedV3Event>() != null)` —— **有 RankedV3 活动才显示那颗开关**。
            //   🔴 **本地等价源 = `Shell/RankedData.cs` 的那一季**（判据见 <see cref="HasActiveRankedEvent"/>）——
            //     ⛔ 别写成「本地没有 RankedV3 事件 ⇒ 隐藏」：我们有本地排季 + `RankedEventWindowV2` 恒可进
            //     （`Shell/MainMenuRuntime.cs` 的排位模式卡恒在），那是**同一语义的本地来源**。
            //   ⚠️ 隐藏时**开关下那四件一起藏**（它们是 `Toggle` 的子件）—— 原版 `SetActive` 打的就是那整颗
            //     `gameModeSelector`（四件都是它的子件，`Barn`/`Background`/两颗图标/名牌都跟着藏）。
            if (_gmToggle != null) _gmToggle.gameObject.SetActive(HasActiveRankedEvent);
            if (!HasActiveRankedEvent)
                Debug.Log("[Practice] `Game mode` 开关**藏起来**：本地没有在期的 RankedV3 活动"
                        + "（原版 `Awake` 末句的闸 = `GetActiveEvent<RankedV3Event>() != null`）"
                        + " ⇒ 本窗照旧按 `Skirmish(13)` 跑（原版开关藏起来时 `currentPlayMode` 就停在出厂值）");

            Img(root, "40k_bt_underbutton", ContL, ContT, ContR, ContB, "Continue Button", QPrRow, true,
                new Color(0.369f, 0.894f, 0.587f, 1f));
            // 🆕 **A1205 接上**（`R5` §2·H #54：原版 `DeckSelectionContinueButton ▸ Text`
            //    = `auto[18~45] · base 36 · 折行 1`）
            // 🔴 **2026-10-19（`A1501`）：文案改走 `Loc.T`** —— 那颗 TMP 的
            //    `mTerm = Demo/ToBattle`、`m_text = 'Battle!'`（本轮现读：节点 = `DeckSelectionContinueButton ▸ Text`）。
            Txt(root, Loc.T("Demo/ToBattle"), BtTxL, BtTxR, BtTxT, BtTxB, 45f, Align.Right, "Battle Text", QPrText,
                wrapPx: BtTxR - BtTxL, autoMinPx: 18f, autoMaxPx: 45f, autoBasePx: 36f,
                face: TmpFont.Face.LatinCardBody);
            Img(root, "40k_UI_bt_play", CircL, CircT, CircR, CircB, "CircleButton", QPrRow, true);
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 B11）**：原版
            //   `Practice Mode Menu/DeckSelectionContinueButton/Continue Button` → **`MenuStartGame`**
            //   （pid -1545367626748338049，clip `StartMatch`）—— 同 prefab 根 `DeckSelectionContinueButton` 也是它。
            HitOn(root, root, "BattleHit", new PxRect(ContL, ContT, CircR, CircB), () => StartBattle(),
                  QPrHit, null, WindowSounds.StartGameCue);
        }

        // ============================================================ 🆕 2026-10-17（U4-1）：`Game mode` 开关
        // 原版 = `Practice Mode Menu/Toggle`（`EverguildToggle`，`isOn = 0`）+ 四件子件。
        // 行为判据（逐句实读）：
        //   · `PracticeModePopup__GameModeValueChange.c`：`false → *(int*)(self+0x110) = 13 (Skirmish)` ·
        //     `true → 0 (Classic)`，然后 `ResetContent`（销毁旧卡组格、选择归零、按新模式重列）。
        //   · `PracticeModePopup__.ctor.c`：`*(int*)(self+0x110) = 0xd`（**出厂 = Skirmish**）；
        //     prefab 那颗 `Toggle.m_IsOn = 0` 与它自洽。
        //   · prefab MB `-8743438662623342788`：`changeSpriteOnValueChange = 1` · `spriteToChange` = `Background` ·
        //     `colorTintOnValueChange = 0`（**只换图、不换色** —— 别照 `onColor/offColor` 做色偏，那两个值在本件是死值）。
        //   · `PracticeModePopup__SetArmyButtons.c`：卡组列表按 `gameMode` 在**两套槽位**（`+0x18` / `+0x20`）里二选一
        //     ⇒ 我们的等价物 = 按 `PlayerDeck.GameMode` 过滤「我的卡组」列表。

        /// <summary>本窗当前的赛制（原版 `PracticeModePopup.gameMode` @`+0x110`，出厂 `0xd` = **Skirmish(13)**）。
        /// <para>🔴 **2026-10-22（`D1`）就地订正（铁律 5）**：这里原来写着「**一处主动偏离**：原版 ctor 默认
        /// Skirmish(13)，本类 `Open()` 却按**当前选中卡组自己的模式**初始化」——**那处偏离已经删掉**，
        /// `Open()` 现在照原版给 **Skirmish**。
        /// 🆕 **2026-10-22（同一批 · 练习窗数据源对齐）**：这里原来还写「我们这扇窗列的是**玩家自己的卡组**
        /// （原版是预组 `Practice Deck`）」—— **数据源已经换成原版那 78 对 `DemoDeckInfoSO`**（`PrebuiltDecks.Pairs`），
        /// 所以「默认 Skirmish 会列出空列表」那个下游症状**已经消失**（78 对两侧齐全，两种模式都列得出）。
        /// 两个模式的差别只剩「同一对的哪一副」：`Classic → classicDeck(30 张)`、`Skirmish → skirmishDeck(12 张)`
        /// （原版 `DemoDeckInfoSO.GetDeck`）。</para>
        /// <para>开关的出现条件不同：原版 `Awake` 里只有 LiveOps 有对应活动才 `SetActive(true)`，
        /// 我们没有服务器 ⇒ 常显（同 `ChatPanel` 那条老账）。</para>
        /// <para>🔴 **2026-10-23 就地订正（铁律 5）**：这里原来写「`Open()` 之后左列**没有选中格**」——
        /// **已过期**（2026-10-23 之前就过期了：首格「你的卡组」建出来之后，`Open()` 那条路本来就让首格亮着）。
        /// 现在 `Open()` = `DeckIndex = CollectionData.CurrentIndex()` + `PickedPrebuilt = null` +
        /// `PairIndex = -1` + `Build()` ⇒ **默认选中的就是「你的卡组」那一格**（与原版
        /// `SetArmyButtons → DeckSelected(item0)` 的结果逐项一致，见 <see cref="ResetContent"/> 的 doc）。
        /// ⚠️ 与原版仍有一处来源差别：原版那副来自 LiveOps `GameModes.GetActiveEvent(mode)`（
        /// `PracticeModePopup__Open.c` 中段），我们没有服务器 ⇒ 用 `DeckLibrary.Current` 顶上（同一语义的本地来源）。</para></summary>
        public GameMode SelectedGameMode { get; private set; } = GameMode.Skirmish;

        /// <summary>`Background` 那一颗（开关底图，换态时换图用）。</summary>
        ImageQuad _gmKnob;

        /// <summary>把卡组里的 `GameMode` 号归到这颗开关能表达的两档（`0` / `13`）。
        /// 原版 `EverguildToggle` 只有两档（`false→13 / true→0`）⇒ 别的号（例如事件窗建的卡组）原版同样表达不了；
        /// 我们统一按 **Classic** 处理（与 `MenuDraw.GameModeIconFile` 只认 `0/13` 那一条同向，但不复用它的 UI 映射）。</summary>
        public static GameMode ToggleModeOf(int rawGameMode)
            => rawGameMode == (int)GameMode.Skirmish ? GameMode.Skirmish : GameMode.Classic;

        /// <summary>开关底图：`isOn = false → offSprite`、`true → onSprite`（prefab 实读；见上面那一段）。
        /// `isOn` ⟺ `Classic`（`GameModeValueChange` 的 `true → 0`）。</summary>
        static string GameModeKnobArt(GameMode mode)
            => mode == GameMode.Classic ? "40_main_bt_toggle_on" : "40_main_bt_toggle_off";

        void BuildGameModeToggle(Transform root)
        {
            // 原版 `Toggle` 那一层（`EverguildToggle` + `NonDrawingGraphic`）—— 四件子件挂在它下面，
            // 兄弟序照原版（从下到上）：`Background` → `Skirmish toggle Image` → `Classic toggle Image`
            //                      → `Label Background`（其下 `Label`）。
            // 🆕 2026-10-26（批次 6 · G15）：节点存一份 —— `Build()` 末尾那道 `Awake` 闸要用它
            //   （原版 `gameModeSelector.SetActive(有 RankedV3 活动)`，判据见 `Build()` 里那一段）。
            _gmToggle = New(root, "Toggle", new PxRect(TogL, TogT, TogR, TogB));
            var tog = _gmToggle;

            // ① `Background`（89×40）—— **只有它随 `isOn` 换图**（`m_PreserveAspect = 0` ⇒ ⛔ 别 keepAspect，
            //    这张图正好 89×40；旧代码把它按 keepAspect 拉进整框就是这么错的）。
            _gmKnob = Img(tog, GameModeKnobArt(SelectedGameMode), GmBgL, GmBgT, GmBgR, GmBgB,
                          "Background", QPrRow, false);

            // ②③ 两颗 94.6439² 模式图标（源图 256×256 方形；原版 `m_PreserveAspect = 0`，
            //     方形框 + 方形图 ⇒ keepAspect 与拉伸同值，这里显式写 keepAspect 只表达「保持比例」）。
            Img(tog, "40k_gamemode_icon_skirmish", GmSkL, GmSkT, GmSkR, GmSkB,
                "Skirmish toggle Image", QPrRow, true);
            Img(tog, "40k_gamemode_icon_classic", GmClL, GmClT, GmClR, GmClB,
                "Classic toggle Image", QPrRow, true);

            // ④ `Label Background`（`40k_main_bt_nametag`，源图 109×41 **拉成** 320.267×40 ⇒ keepAspect = false）
            //    队列比两颗图标高：原版兄弟序里它在**最后**（后来的兄弟画在上面），而它的上沿（893.24）
            //    压进图标下沿（887.86）5.38px。`QPrItemHL` 那一档与本窗 item 内景**屏上不相交**
            //    （item 在左两列，这一条在底部 799.87..1120.13 × 853.24..893.24）⇒ 复用，
            //    与 A178「两族 item 共号」同一口径（⛔ 别为此新开号挤别人的档）。
            Img(tog, "40k_main_bt_nametag", GmPlateL, GmPlateT, GmPlateR, GmPlateB,
                "Label Background", QPrItemHL, false);

            // ⑤ `Label` —— 🔴 原版节点名就叫 `Label`；我们沿用**既有名 `Toggle Label`**
            //    （`Editor/MainMenuScene.cs` 的文案断言按这个名字取件 ⇒ 改名会红）。
            //    原版字段：`m_fontSize 36 / m_fontSizeBase 36`（**无自适应**）、`Center/Capline`、`折行 = 1`。
            //    文案走 `Loc.T`（`mTerm = Event_Description/Generic/GameMode`，既有那条不动）。
            Txt(tog, Loc.T("Event_Description/Generic/GameMode"), GmLabelL, GmLabelR, GmLabelT, GmLabelB,
                36f, Align.Center, "Toggle Label", QPrText, wrapPx: GmLabelR - GmLabelL,
                vAlign: Label.VAlign.Capline, face: TmpFont.Face.LatinCardBody);   // 🆕 N11：原版 `Toggle ▸ Label` = `Center/**Capline**`（现读 dump）

            // 命中区 = 原版**可射线件的并集**（`Toggle` 根那颗 `NonDrawingGraphic` +
            // 四颗 `m_RaycastTarget = 1` 的 `Image` —— 图标那一份是本轮从 prefab MB 实读的，
            // `m_RaycastTarget = 1`、`m_RaycastPadding = 0`）。四块都指向同一个动作：
            // `PointerLayer` 要「按下与抬起落在同一个 `WindowButton` 上」才派发 ⇒ 重叠处不会双触发。
            // ⚠️ 主命中区沿用既有节点名 `ToggleHit`（`Editor/CollectionScene.cs` 的 `ToggleHit` 按这个名字断类型）。
            HitOn(tog, tog, "ToggleHit", new PxRect(TogL, TogT, TogR, TogB), ToggleGameMode);
            HitOn(tog, tog, "ToggleSkirmishHit", new PxRect(GmSkL, GmSkT, GmSkR, GmSkB), ToggleGameMode);
            HitOn(tog, tog, "ToggleClassicHit", new PxRect(GmClL, GmClT, GmClR, GmClB), ToggleGameMode);
            HitOn(tog, tog, "ToggleLabelHit", new PxRect(GmPlateL, GmPlateT, GmPlateR, GmPlateB), ToggleGameMode);
        }

        /// <summary>点一下开关 = 原版 `Toggle.onValueChanged` → `GameModeValueChange(isOn)`
        /// （`false → Skirmish(13)`、`true → Classic(0)`）。</summary>
        public void ToggleGameMode()
            => SetGameMode(SelectedGameMode == GameMode.Classic ? GameMode.Skirmish : GameMode.Classic);

        /// <summary>切赛制（原版 `PracticeModePopup__GameModeValueChange.c` 的等价物）：
        /// 写 `gameMode` → `ResetContent`（重列该模式的卡组）。</summary>
        public void SetGameMode(GameMode mode)
        {
            mode = ToggleModeOf((int)mode);
            bool changed = mode != SelectedGameMode;
            SelectedGameMode = mode;
            RefreshGameModeVisual();
            ResetContent();
            Debug.Log("[Practice] `Game mode` → " + (mode == GameMode.Classic ? "Classic(0)" : "Skirmish(13)")
                      + (changed ? "" : "（本来就是这一档，仍按原版走一次 `ResetContent`）")
                      + " ⇒ 重列该模式的卡组");
        }

        /// <summary>换开关底图（`spriteToChange = Background`）。图取不到 ⇒ **出声一次**、停在上一态。</summary>
        void RefreshGameModeVisual()
        {
            if (_gmKnob == null) return;
            string art = GameModeKnobArt(SelectedGameMode);
            var tex = CardArt.MenuUi(art);
            if (tex == null)
            {
                Debug.LogWarning("[Practice] `Game mode` 开关底图取不到：" + art
                                 + " ⇒ 底图停在上一态（如实出声，不假装换成功）");
                return;
            }
            // ⚠️ 两个重载语义不同：`SetTexture(t)` 会顺手把 `_aspect` 冲成新贴图自己的比例；
            //    这里两张图都是 89×40（同比例）⇒ 今天同值，但**显式**走 `keepAspect: true`（A292 那条纪律）。
            _gmKnob.SetTexture(tex, true);
        }

        /// <summary>原版 `PracticeModePopup__ResetContent` + `SetArmyButtons` 那两步的等价物：
        /// **选择归零 → 按当前（模式 + 阵营）筛选重列 → 默认选中第 0 格（你的卡组）**。
        ///
        /// <para>🔴 **2026-10-23（W65 · 1c 收尾）：「默认选中 = 你的卡组」接上了。**
        /// 判据 = `d:/2/tools/decomp_full/PracticeModePopup__SetArmyButtons.c:117-136` —— 建完第 0 项
        /// （= 玩家自己的卡组）之后**紧接着**三句：`SetActive(item0.gameObject, 1)` ·
        /// `SetActive(item0+0x38 /* Highlight */, 1)` · **`DeckSelected(item0.popup, item0.deckData, item0)`**
        /// ⇒ 开窗 / 切模式 / 换阵营之后，被选中的那一副**是「你的卡组」，不是演示卡组**。
        /// 只有「你那副的阵营 ≠ 当前阵营筛选」时第 0 项不建（见 <see cref="ShowYourDeckCell"/>），
        /// 此时原版落到 `DeckSelectorMenuItemDemo__SelectItem(list[0])`（`SetArmyButtons.c:184-190` 的
        /// `uVar9 == 0 &amp;&amp; !bVar4` 那一支）⇒ 我们的等价物 = 「该模式 + 该阵营下**第一对**」。</para>
        ///
        /// <para>⚠️ **与原版的差别（如实记）**：原版 `ResetContent` 只清选择（`+0xf8 = 0`、`+0xe8 = 0`）
        /// 并销毁旧格，重列与默认选中都在随后的 `SetArmyButtons` 里；我们把两半都收在本方法
        /// （`PickDeck` / `PickPair` 内部各自会 `RebuildDeckRows`）。</para>
        ///
        /// <para>🔴 **2026-10-23 按原版删掉一条我们自加的行为**：改前这里「保住 `PairIndex`、切模式落回同一对」
        /// —— 原版没有这回事（`ResetContent` 把 `+0xf8` 清空，`SetArmyButtons` 一律重建并选 item0）。
        /// 双卡组那条不变式**仍然成立**，只是入口换成「点那一格」：
        /// `PickPair(pi)` 按**当前** `Game mode` 取该对的另一副 ⇒
        /// `PickedPrebuilt` / `PendingSource` / `CurrentDeck` 永远 = 当前模式那一副。</para></summary>
        void ResetContent()
        {
            if (SelectDefaultDeck()) return;
            ShowNoDeckSelected("`Game mode` = "
                               + (SelectedGameMode == GameMode.Classic ? "Classic(0)" : "Skirmish(13)"));
        }

        /// <summary>原版 `SetArmyButtons` **末尾那半**：格建完之后**默认选中**哪一副。
        /// <para>判据与逐句对照 → <see cref="ResetContent"/> 的 doc。两个入口都走它（⛔ 别各写一份）：
        /// · <see cref="ResetContent"/>（= 原版 `ResetContent + SetArmyButtons`：切模式）；
        /// · <see cref="PickArmy"/>（= 原版 `ChangeSelectedArmy` 末句那句 `SetArmyButtons`：换阵营）。
        /// </para>
        /// <returns>`true` = 选出了某一副（自家的卡组 / 第一对）；`false` = 当前筛选下**一格都没有**。</returns></summary>
        bool SelectDefaultDeck()
        {
            // ① 原版第 0 项（玩家自己的卡组）—— 阵营不匹配时那一格根本不建。
            if (ShowYourDeckCell()) { PickDeck(YourDeckIndex); return true; }
            // ② 原版退到 `SelectItem(list[0])` —— 列表按 `decks` 数组序（= 我们的 `uiOrder`）筛过阵营。
            int first = FirstPairInMode(SelectedGameMode);
            if (first >= 0) { PickPair(first); return true; }
            return false;
        }

        /// <summary>当前（模式 + 阵营）筛选下**一套都没有** ⇒ 面板归零再出声（⛔ 不许静默、也不许留下上一副）。
        /// <para>🔴 **2026-10-21（审查代理 #1）**：一套都不剩 ⇒ 曲线也必须归零（判源 = `CurrentDeck` = null
        /// ⇒ `Counts` 全 0）。原来这一支不重画，面板上会**留着一副已经不存在的牌**的 9 行。</para>
        /// <para>🆕 2026-10-22（M2）：立绘一并撤掉 —— 同一句理由：⛔ 面板上不留上一副牌的东西。</para>
        /// <param name="why">为什么走到这一支（进日志；两个调用方各说各的筛法）。</param></summary>
        void ShowNoDeckSelected(string why)
        {
            PairIndex = -1;
            DeckIndex = -1;
            PickedPrebuilt = null;
            PrebuiltDecks.ClearPendingBattleDeck();
            if (_deckHolder != null) RebuildDeckRows(_deckHolder);
            if (_info != null) BuildCardRows(_info);
            RefreshCostRows();
            RefreshCharacterImage();
            if (_txtDeckName != null) _txtDeckName.SetText("");
            if (_txtWarlord != null) _txtWarlord.SetText("");
            if (_txtArmy != null) _txtArmy.SetText(SelectedArmyName());
            Debug.LogWarning("[Practice] " + why + "：当前筛选下**一套卡组都没有** ⇒ 列表为空（如实出声，不假装选到了）");
        }

        /// <summary>当前（模式 + 阵营）筛选下**第一对**在 `PrebuiltDecks.Pairs` 里的下标；一套都没有 ⇒ −1。</summary>
        int FirstPairInMode(GameMode mode)
        {
            var pairs = PrebuiltDecks.Pairs;
            for (int pi = 0; pi < pairs.Count; pi++)
            {
                var deck = PrebuiltDecks.PairDeck(pairs[pi], mode);
                if (deck == null) continue;
                if (ArmyIndex >= 0 && _facs != null && ArmyIndex < _facs.Count
                    && deck.faction != _facs[ArmyIndex]) continue;
                return pi;
            }
            return -1;
        }

        /// <summary>左栏：13 个阵营格子（**82×82、spacing (0,26.38)、2 列 × 7 行、整组横向居中**）。
        /// 🔴 **2026-10-24（`W8` · P1 差异 #1）**：原版是 **2 列**（`Filters` 的 `GridLayoutGroup`
        /// `m_Constraint = Flexible` 在 177.12 宽里现算 = 2）⇒ **13 格一屏全见、不滚**；
        /// 我们原来是**单列 13 行**（只露约 7 格）。参数与算式 → 常量段 `ArmyCell` / `ArmyCols` / `ArmyCellL`。
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
            var sel = New(root, "Army Selector", new PxRect(ArmL, ArmT, ArmR, ArmB));  // `ScrollRect` 自己那一层
            // ⚠️ A332：矩形 = 69.42,182.18→246.54,880.17（**177.12 × 697.99**）。⛔ 别拿 `Viewport` 那一对
            //    （它上下各探出 33.11 —— 见下一条）。

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
            //      「面板底」这一层就是 `QPr`；⚠️ **2026-10-10（A178）起格子排在 `QPrItemHL`(3102) 以上**
            //      ⇒ 照旧画在格子下面（原来那句写的是 `QPrRow` 3101）。
            Nine(sel, sel, "UI_Background_faction_buttons", new Vector4(2f, 201f, 2f, 202f), 54f, 420f,
                 ArmL, ArmT, ArmR, ArmB, QPr, "Background");
            // 吸收层（公共件建、档由它算 = `qContentMin − 1`）—— 与原版那颗 `m_RaycastTarget = 1` 的
            // `Image` 同一个语义：**这一下被吃掉、什么都不做**。两个档与同窗那次 `ShadeHit` 同源。
            MenuDraw.Absorb(sel, "AbsorbHitArmy", new PxRect(ArmL, ArmT, ArmR, ArmB), QPr, QPrHit);

            // A332：`Viewport` 与 `Filters` **同一对矩形**（69.42,149.07→246.54,913.28 ⇒ **177.12 × 764.21**）
            // —— 它们与 `Army Selector` 那一对**不同**（上下各探出 33.11）⇒ 别互推。
            // ⚠️ **2026-10-13（A435 阶段 2 · 戊 / A754-a）**：这个局部量**已经不是裁切框了** ——
            //   它现在只用来建下面那两个节点 + 喂 `MenuScroll.TopAligned`（滚动区自己的矩形，**另一条路**）。
            //   ⛔ 别再拿它去当 `MenuDraw.Rect/Nine/Hit/Txt` 的 `clip` 形参（那是迁移前的写法，会把节点盖住）。
            var view = new PxRect(ArmVpL, ArmVpT, ArmVpR, ArmVpB);
            var vp = New(sel, "Viewport", view);                  // 原版挂 `RectMask2D` 的那一件
            // 🔴 **2026-10-13（A435 阶段 2 · 戊 / A754-a）**：这颗 `Viewport` 就是**这颗视口的裁切状态载体**
            //   （= 原版 `Practice Mode Menu/Deck Selector/Army Selector/Viewport` 身上那个 `RectMask2D`；
            //    参数判据 = 本文件常量段 `ArmyClipSoft` 那条注释里的逐处实读 `(0,50)`，`m_Padding` 全 0）。
            //   形状照 **`Shell/LiveOpsEventWindow.cs` 的 `Army Selector/Viewport` 那一颗**（A435 丁 / A745 的成品）——
            //   ⚠️ 本节点**本来就存在**（`Filters` 就挂在它下面）⇒ 只 `AddComponent`、**零结构改动**
            //   （同 `AvatarTab` / `TitleTab` / `ChatPanel` 走的迁移表 §二·A 注①【低风险】那条路）。
            //   ✅ 与迁移前「`RebuildArmyCells` 里逐件传 `view` + `ArmyClipSoft`」**逐位同值**：
            //   框 = 本节点自己的 rect（本来就是同一个 `PxRect(ArmVp…)`）、两个参数逐字相同。
            //   ⛔ 别改回去逐件传 `view` —— 那样节点会被形参盖住（`ViewportClip.NodeShadowedByParam`），
            //   框一个像素都不生效、而且不出声（迁移表 §二 通则那一句）。
            var armyVc = vp.gameObject.AddComponent<ViewportClip>();
            armyVc.padding = Vector4.zero;                                    // 原版这一处 `m_Padding` 全 0
            armyVc.softness = new Vector2Int((int)ArmyClipSoft.x, (int)ArmyClipSoft.y);   // (0,50)：只渐变上下
            // 🔴 **2026-10-18（A840 · S1）**：补一句 `CaptureNow()` —— 把**刚写进这个节点的那个矩形**记成基准
            //   （= 框的中心那一帧，口径 → `ViewportClip` 文件头 §①）。⛔ 少了它，这颗视口**逐位回落到旧写法**
            //   （实时反推 + `LiveDerivations`）：本窗每开一次整棵树重建，框与被比矩形就可能**不在同一帧**。
            //   ⚠️ **必须在这里调**（`AddComponent` 之后、`RebuildArmyCells` 之前）：`MenuDraw.ApplyPxRect` 写矩形时
            //   组件还不存在（那一句的穿透写在 `ApplyPxRect` 尾）—— 同 `ViewportClip.Hang` 里那句
            //   `vc.SetBaseRect(r)` 的位置理由（`CardPresentation/Shell/ViewportClip.cs`）。⛔ 别改成 `OnEnable` 里自动抓（文件头 `CardPresentation/Shell/ViewportClip.cs` 明令）。
            armyVc.CaptureNow();

            var filters = New(vp, "Filters", view);               // 原版格容器（`GridLayoutGroup`）

            var facs = CollectionWindow.CardsState.Factions();
            // 内容高照 uGUI `GridLayoutGroup.CalculateLayoutInputVertical`：
            // `pad.vertical + (格高 + spacing.y) × 行数 − spacing.y`（**行数 = ⌈13 / 列数⌉ = 7**）——
            // 原来少减了一个 spacing.y，而且行数按「一格一行」（单列）算。
            ArmyScroll = MenuScroll.TopAligned(view, ArmyGridContentH(facs.Count), 50f);   // 原版 `Practice Mode Menu/…/Army Selector` = 50（D_N29）
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

        /// <summary>阵营列（`Filters`，**2 列网格**）在给定格数下的内容高。
        /// 行数 = `⌈n / ArmyCols⌉` —— 照 uGUI `GridLayoutGroup.SetCellsAlongAxis` 的
        /// `cellCountY = n / cellCountX + (n % cellCountX > 0 ? 1 : 0)`。
        /// 13 格 ⇒ 7 行 ⇒ `(82 + 26.38) × 7 − 26.38` = **732.28**（≤ 视口 764.21 ⇒ **不滚**）。
        /// ⚠️ 原来按「一格一行」算（13 行 = 1382.56）—— 那是单列时代的读数（P1 差异 #1）。</summary>
        public static float ArmyGridContentH(int cells)
        {
            int cols = ArmyCols;
            int rows = cells <= 0 ? 0 : (cells + cols - 1) / cols;
            return GridContentH(rows, ArmyCell, ArmyGapY, 0f, 0f);
        }

        void RebuildArmyCells(Transform holder)
        {
            for (int i = holder.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(holder.GetChild(i).gameObject);
            ArmyCells.Clear();
            var facs = _facs ?? new List<string>();
            // 🔴 **2026-10-13（A435 阶段 2 · 戊 / A754-a）**：这里原来有个
            //   `var view = new PxRect(ArmVpL, ArmVpT, ArmVpR, ArmVpB);`（逐件当 `clip` 传的那一份）——
            //   **已删**：裁切状态现在长在 `BuildArmySelector` 建的那颗 `Army Selector/Viewport` 节点上，
            //   下面的粗筛走 `VisibleAbove`、逐件的 `clip` 全部传 `null` ⇒ `MenuDraw.*` 沿父链解析到它。
            //   ⛔ 别在这里再留一个 `view` 局部量（留着 = 下一个人照它写就又回到「两处状态」那条老路）。
            // 🆕 A178：`Has Player Deck` 的显隐要用**当前选中卡组的阵营**（见下面那段判据）—— 循环外算一次。
            // 🔴 **2026-10-21**：改走唯一判源 `CurrentDeck`（原来按 `DeckIndex` 取 —— 预组档是 −1 ⇒
            //   预组明明有阵营，这一列的火漆印一个都不亮）。owned 档两处逐位相同
            //   （`CollectionData.DeckAt` 里那条 `Faction` 就是督军卡身上的字段，与 `FactionOf` 同一条推导）。
            var curDeck = CurrentDeck;
            string deckFac = curDeck != null ? FactionOf(curDeck.WarlordId) : "";
            // 🔴 格位照 uGUI `GridLayoutGroup.SetCellsAlongAxis`（`Flexible` 档、`m_StartAxis = Horizontal`）：
            //   横 = `Viewport.x1 + startOffset.x(6.56) + 列 × (格宽 + spacing.x)`，列 = `i % ArmyCols`；
            //   纵 = `Viewport.y1 + 行 × (格高 + spacing.y)`，行 = `i / ArmyCols`（**第一行贴视口上沿**）。
            //   🔴 **2026-10-24（`W8` · P1 差异 #1）**：原来是**单列**（`y1 = ArmVpT + i × 108.38`、
            //   `x1` 恒 = `ArmyCellL`）⇒ 13 行、内容高 1382.56、只能露约 7 格还要滚。
            //   参数出处逐条 → 常量段 `ArmyCell` / `ArmyCols` / `ArmyCellL` 那三条的注释（uGUI 实算式）。
            int cols = ArmyCols;
            float x0 = ArmyCellL;
            for (int i = 0; i < facs.Count; i++)
            {
                int col = i % cols, row = i / cols;
                float x1 = x0 + col * (ArmyCell + ArmyGapX), x2 = x1 + ArmyCell;
                float y1 = ArmVpT + row * (ArmyCell + ArmyGapY);
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
                // 🔴 **2026-10-13（A435 阶段 2 · 戊 / A754-a）**：改走 **`VisibleAbove`**（节点态那一版）——
                //   裁切状态已经搬 `Army Selector/Viewport` 那颗 `ViewportClip` 上，下面逐件传的 `clip`
                //   全部换成 `null` ⇒ 沿用裸 `Visible(rr, view)` 的话这里**永远放行**（静默：整格滚出视口照建）。
                //   解析起点 = `holder`（= `Filters`，正挂在那颗 `Viewport` 下 ⇒ 一次父链就命中）。
                if (!MenuDraw.VisibleAbove(holder, rr, null)) continue;   // 整块在视口外 ⇒ 不建（连点击区一起）
                // 🔴 **2026-10-11（A218）**：格子节点也是 `RectTransform` + 写 `sizeDelta`
                //    （尺寸 = 这一格的矩形 `rr`，与位置**同一份换算**）。
                //    判据 = 原版这一格的 item prefab `Practice Army Select Button` 实读 **82×82**
                //    （`menu_dump --rt 8940217906067473431 --root-size 82x82`，注释见上面那段），
                //    与 `ArmyCell` 同值。改坏法：删掉 `SetPxSize` ⇒ `Editor/MainMenuScene.cs` §A218
                //    「`Army_<i>` 的 rect = 格矩形」红（两态：格与 `GetComponent<RectTransform>()` 都在、
                //    只有 `rect` 会变 0 ⇒ 弱断言分不出来）。
                var cell = new GameObject("Army_" + i, typeof(RectTransform));
                cell.transform.SetParent(holder, false);
                MenuDraw.SetPxSize(cell.transform, rr.W, rr.H);
                cell.transform.localPosition = Local3(holder, rr.x1, rr.y1, rr.x2, rr.y2);
                // 🆕 A9 尾巴：这一格吃**软边**（原版 `Army Selector/Viewport` 的 `m_Softness = (0,50)`）——
                //   压在渐隐带里的**第一格**会被按剖面削 alpha（`MenuDraw.ApplySoftEdges` 的几何等效物）。
                //
                // ============================================================ 🆕 **2026-10-10（A178）**
                // **这一格的 item prefab = 原版 `Practice Army Select Button`，四个子件**（兄弟序照原版、
                // **从下到上**：`Highlight` → `Background` → `Icon` → `Has Player Deck`）。
                // 我们原来**只建了 `Icon` 一层**（+ 命中区 `Hit`）⇒ 整件漏建三层。判据链与实读命令
                // → 常量段 `ArmyHlX1…` 那一段（`menu_dump --rt 8940217906067473431 --root-size 82x82`）。
                //
                // 🔴 **四层各一个队列**（`QPrItemHL` → `QPrItemBd` → `QPrItemArt` → `QPrItemSeal`）——
                //    同队列「谁盖谁」不可控（`ImageQuad` 全是透明队列、按到相机的 3D 距离排，`CLAUDE.md` §三）。
                //    这四个号是**本窗 item 内景**那一族、两族 item 共用（理由见 `QPrItem*` 的注释）。
                // ⚠️ 四层都在原版那个 `RectMask2D` 视口里 ⇒ **都吃同一个硬裁 + `ArmyClipSoft` 渐隐带**
                //    （`Highlight` 比格大 6.83/边、第一格那颗的上沿在视口之上 ⇒ 原版就被裁掉一条）。
                //
                // ---- 两个显隐条件（🔴 **原版判据，不是我们挑的**）----
                // 判据 = `d:/2/tools/decomp_full/` 里那两个方法体**逐句读到**（V1 §五·4 记的「赋值点没找到」
                // 是**错的**：它们不在 `PracticeArmySelectionButton` 自己身上，而在 `PracticeModePopup` 上）：
                //   ① `PracticeModePopup__ConfigureArmyFilterButtons.c` 尾段 —— 每颗 `Instantiate` 之后**紧接着**
                //      两句 `GameObject.SetActive`：`btn+0x28`(`highlight`) = **0**、`btn+0x30`
                //      (`highlightPlayerDeckObject`) = **0** ⇒ **出厂两颗都是关的**（prefab 里 `act=1` 是模板态）。
                //   ② `PracticeModePopup__ChangeSelectedArmy.c` 的循环（**唯一会开它们的地方**）：
                //      · `SetActive(btn+0x28, btn.army(`+0x48`) == param_2)`  ⇒ **`Highlight` 亮 ⟺ 这一格就是当前选中的阵营**；
                //      · `deck == null ? SetActive(btn+0x30, 0) : SetActive(btn+0x30, btn.army == deck.army)`
                //        （`deck` = `*(self+0x100)+0x58` 那副；`deck.army` 读的是 `deck+0x40 → +0x2c`）
                //        ⇒ **`Has Player Deck` 亮 ⟺ 有选中卡组、且它的阵营 == 这一格的阵营**（**与「哪一格被选中」无关**）。
                //   ③ 调用时机：`PracticeModePopup__Open.c` 尾部 —— 开窗时 `ChangeSelectedArmy(有卡组 ? 卡组阵营 : 10)`，
                //      所以**开窗后一定恰好有一格亮着**（`ResetContent` 把 `+0xe8` 归 0，再靠这一句补上）。
                // ⚠️ **我们的等价物**：`ArmyIndex`（本窗的那个「当前阵营」；`-1` = 不限）与
                //    `CollectionData.DeckAt(DeckIndex).Faction`。🔴 **2026-10-19（批次 D）就地订正（铁律 5）**：
                //    这里原来写「本窗 `ArmyIndex` 比原版**多一个**『再点一下取消筛选』，本轮不动它」—— **已过期**。
                //    现在 `PickArmy` 按原版 `ChangeSelectedArmy.c:30-33` **同阵营早返回**（再点不改筛选）。
                //    🔴 **2026-10-23（W66）就地订正（铁律 5）**：这一行原来还写「`-1` 只剩 `Open()` 里那句
                //    `ArmyIndex = -1`」—— **已过期**：`Open()` 现在按原版 `Open.c:172-180` 写**真值**
                //    （`ArmyIndex = DefaultArmyIndex()` = 你的卡组阵营 / `Ultramarines`），
                //    `-1` 今天只剩「自检显式摆位」与「`Factions()` 数据残缺」两个来路。
                // 🔴 **2026-10-13（A435 阶段 2 · 戊 / A754-a）**：下面四层 + 命中区的 `clip` 形参
                //    **一律换成 `null`** —— 硬裁那一刀（以及 `ArmyClipSoft (0,50)` 那道渐隐带）现在由
                //    `Army Selector/Viewport` 那颗 `ViewportClip` 节点给（`MenuDraw.*` / `ImgTex` / `HitOn`
                //    内部各自 `Resolve`）。⚠️ `clipSoftness` **照旧传 `ArmyClipSoft`**：它是**回落那一档**
                //    的带宽（形参非空 = 旧路赢；节点在时由**节点自己的 `softness`** 说了算，两者逐字同值）。
                var hlQ = Img(cell.transform, ArmyHlArt,
                              rr.x1 + ArmyHlX1, rr.y1 + ArmyHlY1, rr.x1 + ArmyHlX2, rr.y1 + ArmyHlY2,
                              "Highlight", QPrItemHL, false, null, null, ArmyClipSoft);
                var bgQ = Img(cell.transform, ArmyBgArt, rr.x1, rr.y1, rr.x2, rr.y2,
                              "Background", QPrItemBd, false, null, null, ArmyClipSoft);
                var iconQ = Img(cell.transform, DeckRuntime.FactionIcon(facs[i]), rr.x1, rr.y1, rr.x2, rr.y2,
                                "Icon", QPrItemArt, true, null, null, ArmyClipSoft);
                // ⚠️ `Icon` 那一档的队列 **2026-10-10 由 `QPrRow`(3101) 挪到 `QPrItemArt`(3104)** ——
                //    四层要有严格的内外次序，而它必须夹在 `Background` 与 `Has Player Deck` 之间。
                //    `QPrRow` 是「格底」那一档、比面板底 `QPr`(3100) 只高一号，塞不下四层。
                //    `Editor/MainMenuScene.cs` 那条「`Army Selector/Background` 档 = `QPr`、格子比它高」照旧成立。
                var sealQ = Img(cell.transform, ArmySealArt,
                                rr.x1 + ArmySealX1, rr.y1 + ArmySealY1, rr.x1 + ArmySealX2, rr.y1 + ArmySealY2,
                                "Has Player Deck", QPrItemSeal, false, null, null, ArmyClipSoft);
                if (hlQ != null) hlQ.gameObject.SetActive(i == ArmyIndex);
                // ⚠️ 阵营比较走 `DeckRules.SameFaction`（OrdinalIgnoreCase）—— 与同窗卡组列表那条筛选用的是
                //    **同一种相等**（`RebuildDeckRows` 里那句 `deck.faction != _facs[ArmyIndex]` 是大小写敏感的裸比，
                //    两处今天对同一批数据等价；真要收口是另一件，本轮**不动**、如实登记）。
                if (sealQ != null) sealQ.gameObject.SetActive(deckFac.Length > 0
                                                              && RuleEngine.DeckRules.SameFaction(deckFac, facs[i]));
                int idx = i;
                // 🔴 **2026-10-13（A435 阶段 2 · 戊 / A754-a）**：`clip` 传 `null` —— 命中区要的
                //   【裸框 + pad】也由那颗 `Viewport` 节点给（`HitOn` → `MenuDraw.ClipRectAbove`）。
                //   ⚠️ 软边**故意不传**（`HitOn` 也不收这个形参）：原版 `m_Softness` 只改渲染，射线那一面只看矩形。
                HitOn(cell.transform, cell.transform, "Hit", rr, () => PickArmy(idx), QPrHit, null);
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
            // A332：矩形 = 261.28,262.64→634.88,803.43（**373.60 × 540.79**）。
            var sel = New(root, "Decks Scroll view", new PxRect(DecksL, DecksT, DecksR, DecksB));

            // A332：`Viewport` 与 `Decks Scroll view` **同一对矩形**（原版这一件实读同矩形）。
            // ⚠️ **2026-10-13（A435 阶段 2 · 戊 / A754-b）**：这个局部量**已经不是裁切框了** ——
            //   它现在只用来建下面那个节点 + 喂 `MenuScroll.TopAligned`（滚动区自己的矩形，**另一条路**）。
            //   ⛔ 别再拿它去当 `MenuDraw.*` 的 `clip` 形参（那是迁移前的写法，会把节点盖住）。
            var view = new PxRect(DecksVpL, DecksVpT, DecksVpR, DecksVpB);
            var vp = New(sel, "Viewport", view);
            // 🔴 **2026-10-13（A435 阶段 2 · 戊 / A754-b）**：这颗 `Viewport` 是**本窗第二处视口**的
            //   裁切状态载体（= 原版 `…/Deck Buttons/Decks Scroll view/Viewport` 身上那个 `RectMask2D`；
            //   参数判据 = 本文件常量段 `DeckClipSoft` 那条注释里的逐处实读 `(0,23)`，`m_Padding` 全 0）。
            //   🔴 **与同窗那处阵营视口【不是同一个值】**（(0,50) vs (0,23)）—— 这正是「一扇窗两个视口
            //   各自一份状态」这一迁移要表达的东西（铁律 5·c：逐处实读，⛔ 别互推）。
            //   形状照 `Shell/LiveOpsEventWindow.cs` / `Shell/ChatPanel.cs` 那两颗（A435 丁 / 丙的成品）——
            //   ⚠️ 本节点**本来就存在**（`Content` 就挂在它下面）⇒ 只 `AddComponent`、**零结构改动**。
            //   ✅ 与迁移前「`RebuildDeckRows` 里逐件传 `view` + `DeckClipSoft`」**逐位同值**。
            //   ⛔ 别改回去逐件传 `view` —— 那样节点会被形参盖住（`ViewportClip.NodeShadowedByParam`），
            //   框一个像素都不生效、而且不出声（迁移表 §二 通则那一句）。
            var deckVc = vp.gameObject.AddComponent<ViewportClip>();
            deckVc.padding = Vector4.zero;                                    // 原版这一处 `m_Padding` 全 0
            deckVc.softness = new Vector2Int((int)DeckClipSoft.x, (int)DeckClipSoft.y);   // (0,23)：只渐变上下
            // 🔴 **2026-10-18（A840 · S1）**：补一句 `CaptureNow()`（同本窗上面那处阵营视口，理由逐字相同）——
            //   本窗**两处视口各挂一颗 `ViewportClip`**，而取状态那一路是**沿父链找最近的节点**
            //   （`ViewportClip.Resolve` 第 2 步）⇒ **一处漏补 = 那一处静默回落旧写法**，另一处补了也**管不到它**。
            //   ⛔ 别只补一处。
            deckVc.CaptureNow();

            // A332：`Content` 的矩形 = **372.04 × 0**（`DecksVpT` 上下同值 ⇒ 高 0）。
            //   原版靠 `ContentSizeFitter` 长高，**我们这条不会跟着长**（见 `MenuScroll.TopAligned` 的初值）
            //   ⇒ 这里如实写「出厂高 0」，别拿内容高顶替。
            var content = New(vp, "Content", new PxRect(DecksCL, DecksVpT, DecksCR, DecksVpT));

            // ⚠️ 行数按**真正铺出来的格**算（= 对 + 「你的卡组」那一格）—— 用 `FilteredDeckCount()` 会少算一格
            //    ⇒ 出厂那一次内容高差一行（`RebuildDeckRows` 末尾会重算，所以只是中途一帧的错，但别留着）。
            int n = ListedCellCount;
            int rows = Mathf.Max(1, (n + DeckCols - 1) / DeckCols);
            DeckScroll = MenuScroll.TopAligned(view, GridContentH(rows, DeckCellH, DeckGapY, 0f, 0f), 100f);   // 原版 `Practice Mode Menu/…/Decks Scroll view` = 100（D_N29）
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
            // 🔴 **2026-10-13（A435 阶段 2 · 戊 / A754-b）**：这里原来有个
            //   `var view = new PxRect(DecksVpL, DecksVpT, DecksVpR, DecksVpB);`（逐件当 `clip` 传的那一份）——
            //   **已删**：裁切状态现在长在 `BuildDeckRows` 建的那颗 `Decks Scroll view/Viewport` 节点上，
            //   下面的粗筛走 `VisibleAbove`、逐件的 `clip` 全部传 `null` ⇒ `MenuDraw.*` 沿父链解析到它。
            //   ⛔ 别在这里再留一个 `view` 局部量（见那一处的注释）。
            int slot = 0;                                  // **格号**（筛掉的不占格）—— 与「第几对」`pi` 是两件事
            // 🔴 **2026-10-22（练习窗数据源对齐原版）**：左列 = **原版那 78 对 `DemoDeckInfoSO`**
            //    （`PrebuiltDecks.Pairs`，每个对 = `classicDeck` + `skirmishDeck` 两副），
            //    **不是**玩家自己的卡组（改前那句 `CollectionData.DeckCount()` 那一版已删）。
            //    判据 → `资料/普查产出_1022/D3_DemoDeckInfo成对表.md` §一·4（78 项 = `PracticeModePopup.decks`
            //    数组逐项相等）+ `DemoDeckInfoSO__GetDeck.c`（`0 → classic`、否则 `→ skirmish`）。
            var pairs = PrebuiltDecks.Pairs;

            // ============================================================ 🆕 **2026-10-23（W65 · 1c）：首格「你的卡组」**
            // **原版这一格一直在、我们缺了** —— 台账那句「「你的卡组」首格缺失」指的就是它（2026-10-23 查实）。
            // 判据（第一权威 = 反编译方法体）`d:/2/tools/decomp_full/PracticeModePopup__SetArmyButtons.c`：
            //   ① 清空列表后**先**看 `_data(+0x100).selectedDeck(+0x58)`；非空**且**它的 `army(+0x40).id(+0x2c)`
            //      == 当前阵营 ⇒ 取**第 0 项**、`InitializeWithPracticeDeck(item0, this, _data, 0, lang)`
            //      —— 那是「玩家自己的卡组」那一支（`DeckSelectorMenuItemDemo__InitializeWithPracticeDeck.c`
            //      里 `DAT_1842605c8` 那个类型检查）：名字取**格子自己的词条**（`+0x78`）、图取
            //      `ArmyUtilities.GetArmyIcon(army)`、`IsPlayerDeck` 那层 `SetActive(1)`；随后
            //      `DeckSelected(item0, _data, item0)` **默认选中它**；
            //   ② 演示卡组从**第 1 项起**（`uVar9 = 1` 那一支）—— 所以下面那一列对**整体后移一格**。
            // 词条 = `MenuDeck/Practice/YourDeck` = `Your deck` / **你的卡组**（官方表实读；`MenuDeck/HUD/YourDeck`
            //   是同值的另一条，⛔ 别混）。用户实拍 `C:/Users/qjh36/Desktop/warpforge参考图/练习界面参考图.png`
            //   左列第 1 格就是它（名字「你的卡组」+ `IsPlayerDeck` 那枚印玺）。
            // ⚠️ **`selectedDeck` 的离线等价物**：原版那份来自 `GameModes.GetActiveEvent(mode)` 的当前活动卡组
            //   （`PracticeModePopup__Open.c` 里 `LiveOpsManager.GetHandler` → `GetActiveEvent` → 写 `+0x58` 那段）——
            //   本工程**没有 liveops 事件实例** ⇒ 取**卡组库当前那一套**（`CollectionData.CurrentIndex()`；
            //   选了预组时 `DeckIndex = −1`，仍回落到它）。这是同一语义的本地来源，⛔ 不是另起设计。
            // ⚠️ **模式不参与筛选**：原版取的是「当前模式那个事件登记的那副」，本地没有按模式登记的这层数据
            //   （`CollectionData` 只有一份当前卡组）⇒ 这里**只按阵营筛**。如实记，⛔ 别假装按模式筛过。
            // 🔴 **2026-10-24（`W8` · 台账登记，⛔ 不是待修项）**：**原版在没有活动练习事件时这一格不存在** ——
            //   `PracticeModePopup__Open.c` 里只有 `LiveOpsManager.GetHandler()` → `GameModes.GetActiveEvent(mode)`
            //   **非空**才 `ScriptableObject.CreateInstance<PlayerDeckDemoDeckInfoWrapper>()` 并把该事件的
            //   `IPlayEvent.GetEventDeck()` 写进 `wrapper.cardDeck(+0x58)`；`SetArmyButtons` 里首格那一支整段
            //   被 `if (playerDeckInfoWrapper != null)` 包着，换卡组那条 `PlayerDeckChanged` 也同样被
            //   `if (wrapper != null)` 挡住 ⇒ 那种环境下**只有 6 格预组、连「更换卡组」都换不出这一格**。
            //   我们**永远有**这一格 = **有意的离线等价物**（本地没有 liveops 事件层），⛔ 别据此说「原版恒有」。
            //   判据 → `资料/普查产出_1024d/P1_练习窗原版对照审查.md` §三·2 与 §四 差异 #8。
            // ✅ **2026-10-23（W65 · 1c 收尾）：「默认选中」那半已经接上了** —— 判据与落点见
            //   `ResetContent` / `SelectDefaultDeck`（原版 `SetArmyButtons.c:132-133` 的 `DeckSelected(item0, …)`）。
            //   ⚠️ 改前这里写的是「默认选中仍是该模式第一对（那半还没接）」—— **已过期**。
            // ⛔ 别把这一格塞进 `PrebuiltDecks.Pairs`（那是演示卡组的数据面，一格一对，与玩家卡组无关）。
            if (ShowYourDeckCell())
            {
                int yi = YourDeckIndex;
                var yInfo = CollectionData.DeckAt(yi);
                int yiForHit = yi;                 // 命中回调要**建格那一刻**的下标（别在闭包里读字段）
                var ycell = BuildDeckCell(
                    holder, slot, YourDeckCellName, Loc.T(YourDeckTerm),
                    CardArt.MenuUi(DeckRuntime.FactionIcon(yInfo.Faction)),
                    ItemYourDeckColor,
                    PickedPrebuilt == null && DeckIndex >= 0,
                    true,                       // `isPlayerDeck` ⇒ `Button border` + `IsPlayerDeck` 两件开
                    () => PickDeck(yiForHit),
                    "「你的卡组」那一格的阵营图（判据 = `ArmyUtilities.GetArmyIcon(selectedDeck.army)`"
                    + " —— 与预组那一支**同一个来源**，只是不看 `DemoDeckInfoSO.armyIcon`）");
                slot++;
                if (ycell != null) DeckRows.Add(ycell);
            }

            for (int pi = 0; pi < pairs.Count; pi++)
            {
                var pair = pairs[pi];
                // 当前模式那一副（原版 `GetDeck(gameMode)`）。取不到 ⇒ `PairDeck` 已经 `LogWarning` 出声、这里跳过。
                var deck = PrebuiltDecks.PairDeck(pair, SelectedGameMode);
                if (deck == null) continue;
                // ⚠️ 阵营筛选用**这副牌自己的 `faction`**（产物里那一列 = 原版 `decklists` 的 `faction`；
                //    与 `FactionOf(heroId)` 同源 —— 别在这儿再推一次）。
                if (ArmyIndex >= 0 && _facs != null && ArmyIndex < _facs.Count && deck.faction != _facs[ArmyIndex])
                    continue;
                int pidx = pi;
                var cell = BuildDeckCell(
                    holder, slot, "DeckRow_" + pi, deck.DisplayName,
                    ArmyIconOfPair(pair, deck),
                    pair != null ? pair.armyColorRaw.ToColor() : ItemYourDeckColor,
                    pi == PairIndex,
                    false,                      // 预组格：原版 `Button border` / `IsPlayerDeck` **都关**
                    () => PickPair(pidx),
                    "`Deck Selector Menu Item/Main image` 的阵营图（`DemoDeckInfoSO.armyIcon` = "
                    + (pair != null ? pair.armyIcon : "<无>") + "）");
                slot++;
                if (cell != null) DeckRows.Add(cell.transform);
            }
            if (_txtArmy != null) _txtArmy.SetText(SelectedArmyName());

            // 🔴 **2026-10-07（A77-㉒④）**：**滚动范围跟着这一次重建重算**（原来只在 `BuildDeckRows` 里算一次）。
            //    `MenuScroll` 的可滚极值全部由 `ContentX1/ContentX2` 推（`MaxOffset = max(0, ContentX2 − 视口下沿)`；
            //    ⚠️ **2026-10-09（A269）起那层 `max(0, …)` 长在 `MaxOffset` 自己身上**，见 `Shell/MenuScroll.cs`）——
            //    出厂按**全量**卡组数算过之后就不动了 ⇒ 点阵营格筛掉一批时，内容高**不跟着缩**，
            //    列表只剩一行却还能往下拖出一大段空白（老账，见 §㉒④）。
            //    判据 = 原版 `Content` 上常驻 `ContentSizeFitter(m_VerticalFit = 1 MinSize)` ⇒ 子件增删**每次都重算**；
            //    换阵营那一支（`PracticeModePopup__SetArmyButtons`）甚至是**整批 `Destroy` + 逐格 `Instantiate`**。
            //    ⚠️ 这里直接写 `Offset` 而**不**调 `SetOffset`：后者会回调 `OnChanged`（= **本函数**）⇒ 重建套重建。
            //      夹取算式与 `SetOffset` 同源（`ClampLo/ClampHi` 现在**就**是 `MinOffset/MaxOffset` 本身 —— A269）。
            if (DeckScroll != null)
            {
                int nRows = Mathf.Max(1, (slot + DeckCols - 1) / DeckCols);
                DeckScroll.ContentX1 = DecksVpT;
                DeckScroll.ContentX2 = DecksVpT + GridContentH(nRows, DeckCellH, DeckGapY, 0f, 0f);
                DeckScroll.Offset = Mathf.Clamp(DeckScroll.Offset, DeckScroll.ClampLo, DeckScroll.ClampHi);
            }
        }

        // ============================================================ 🆕 2026-10-23（W65 · 1c）「你的卡组」那一格

        /// <summary>「你的卡组」那格的**节点名**（自检按名字找它 —— ⛔ 别改成 `DeckRow_<数字>`：
        /// 那一族名字是**对下标**，混进来会让「第 N 对在哪一格」全部错位）。</summary>
        public const string YourDeckCellName = "DeckRow_YourDeck";

        /// <summary>「你的卡组」那格的文案词条（官方表实读 `Your deck` / `你的卡组`）。
        /// ⚠️ 同族还有一条 `MenuDeck/HUD/YourDeck`（值相同）—— 本窗用的是 **`Practice`** 这一条
        /// （格子自己的 `+0x78` 词条，判据见 `RebuildDeckRows` 那段注释）。</summary>
        public const string YourDeckTerm = "MenuDeck/Practice/YourDeck";

        /// <summary>「你的卡组」= 卡组库里**当前那一套**的下标；一套都没有 ⇒ −1。
        /// <para>取法：选了预组时 `DeckIndex = −1`（预组不在库里）⇒ 回落到 `CollectionData.CurrentIndex()`。
        /// 判据与「为什么是这一套」见 `RebuildDeckRows` 里那段「离线等价物」注释。</para></summary>
        public int YourDeckIndex
        {
            get
            {
                int i = DeckIndex >= 0 ? DeckIndex : CollectionData.CurrentIndex();
                return (i >= 0 && i < CollectionData.DeckCount()) ? i : -1;
            }
        }

        /// <summary>这一列要不要画「你的卡组」首格 —— 判据 = 原版 `PracticeModePopup__SetArmyButtons.c`：
        /// `selectedDeck != null` **且** 它的阵营 == 当前阵营筛选（`army.id == param_2`）。
        /// ⚠️ `ArmyIndex < 0`（本窗的「不限阵营」；原版没有这一档 —— `Open()` 已不再产出它，
        /// 今天只剩自检显式摆位与数据残缺）⇒ 不筛、一律显示。</summary>
        public bool ShowYourDeckCell()
        {
            int i = YourDeckIndex;
            if (i < 0) return false;
            if (ArmyIndex >= 0 && _facs != null && ArmyIndex < _facs.Count)
            {
                var info = CollectionData.DeckAt(i);
                if (!string.Equals(info.Faction, _facs[ArmyIndex], System.StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }

        /// <summary>画**一格** `Deck Selector Menu Item`（左列那 78 对 + 首格「你的卡组」**共用这一份**）。
        /// <para>两处唯一不同的东西都从形参进来：`displayName` / `mainIcon` / `nameBg` / `cur` /
        /// `isPlayerDeck` / `onClick`
        /// —— 判据 = 原版 `DeckSelectorMenuItemDemo__InitializeWithPracticeDeck.c` 的两支：
        /// **玩家自己的卡组**（名字 = 格子自己的词条、色 = `yourDeckColor`、图 = `GetArmyIcon(army)`、
        /// `Button border` 与 `IsPlayerDeck` **两件开**）
        /// 与**演示卡组**（名字 = `DemoDeckInfoSO.GetName`、色 = `armyColor`、图 = `DemoDeckInfoSO.armyIcon`、
        /// 那两件**都关**）。</para>
        /// <para>🔴 **别把它拆成两份实现**（「两处写同一条规则 = 迟早不一致」：五层的偏移/队列/裁切
        /// 一旦只改一边，首格与其余格就会静默长得不一样）。</para>
        /// <returns>建出来的格节点；`null` = 整格在视口外（判据 = 原版 `RectMask2D` 只裁不判整格 ⇒ 不建）。</returns></summary>
        Transform BuildDeckCell(Transform holder, int slot, string nodeName, string displayName,
                                Texture2D mainIcon, Color nameBg, bool cur, bool isPlayerDeck,
                                System.Action onClick, string iconNote)
        {
            // 🔴 格位照原版 `GridLayoutGroup.SetCellsAlongAxis`：
            //   横 = `Content.x1 + startOffset + 列 × (格宽 + spacing.x)`（`startOffset` = 8.02 ⇒ 第 1 列 270.08）
            //   纵 = `Viewport.y1 + 行 × (格高 + spacing.y)`（**spacing.y 是 −21** ⇒ 行距 159）
            int col = slot % DeckCols, row = slot / DeckCols;
            float x1 = DeckCellX0 + col * (DeckCellW + DeckGapX);
            float y1 = DecksVpT + row * (DeckCellH + DeckGapY);
            var r = new PxRect(x1, y1, x1 + DeckCellW, y1 + DeckCellH);
            var rr = DeckScroll.Shift(r);
            // 🔴 **2026-10-07（A12①）**：同 `RebuildArmyCells` —— 内联的 `Inside(...)`（**完整**在视口里）
            //    收口成唯一那份求交（**与视口相交**）；判据 = 原版 `RectMask2D`（只裁、不判整格在不在）。
            // 🔴 **2026-10-13（A435 阶段 2 · 戊 / A754-b）**：改走 **`VisibleAbove`**（节点态那一版）——
            //    理由与解析起点同 `RebuildArmyCells`（这里 = `holder` = `Content`，正挂在那颗 `Viewport` 下）。
            if (!MenuDraw.VisibleAbove(holder, rr, null)) return null;   // 整格在视口外 ⇒ 不建
            // 🔴 **2026-10-11（A218）**：格节点也是 `RectTransform` + 写 `sizeDelta`（尺寸 = 格矩形 `rr`）。
            //    判据 = 原版这一格的 item prefab `Deck Selector Menu Item` 的 rect（= `DeckCellW × DeckCellH`，
            //    见常量段那条 `GridLayoutGroup` 推导）。改坏法：删掉 `SetPxSize` ⇒ `Editor/ShellScene.cs`
            //    §A218「`DeckRow_<i>` 的 rect = 格矩形」红。
            var cell = new GameObject(nodeName, typeof(RectTransform));
            cell.transform.SetParent(holder, false);
            MenuDraw.SetPxSize(cell.transform, rr.W, rr.H);
            cell.transform.localPosition = Local3(holder, rr.x1, rr.y1, rr.x2, rr.y2);
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
            // 🔴 **2026-10-24（`W8` · P1 差异 #6，记账不删）**：原版 `Deck Selector Menu Item` 的**子件只有五层**
            //    （`Highlight → Button border → Main image → IsPlayerDeck → Text background(+Deck Name)`）
            //    ⇒ 我们这颗 `Row Bg` **是自建件、原版没有**。今天**没有删**，原因如实记：
            //    判据只覆盖「`m_Children` 列表」，而原版 item **根节点自己**带不带一张底图、悬停换图走哪一路
            //    （`Selectable` 的 transition target）**还没查**（P1 §五·4 只核了节点态）——
            //    现在删会把悬停反馈一起删掉（换一处偏离）⇒ 先记账，等那条判据补齐再决定去留。
            // 🆕 A9 尾巴：这一格也吃**软边**（原版 `Decks Scroll view/Viewport` 的 `m_Softness = (0,23)`）——
            //   压在带上/带下的格按剖面削 alpha（`MenuDraw.ApplySoftEdges` 的几何等效物）。
            // 🔴 **2026-10-13（A435 阶段 2 · 戊 / A754-b）**：本格**七处**（下面五层 + 名字 + 命中区）的
            //   `clip` 形参**一律换成 `null`** —— 硬裁那一刀（以及 `DeckClipSoft (0,23)` 那道渐隐带）
            //   现在由 `Decks Scroll view/Viewport` 那颗 `ViewportClip` 节点给（`MenuDraw.*` / `ImgTex`
            //   / `HitOn` 内部各自 `Resolve`）。⚠️ `clipSoftness` **照旧传 `DeckClipSoft`**：它是
            //   **回落那一档**的带宽（形参非空 = 旧路赢；节点在时由**节点自己的 `softness`** 说了算）。
            var rowBg = Img(cell.transform, "UI_Button_Mulligan", rr.x1, rr.y1, rr.x2, rr.y2, "Row Bg", QPrRow,
                            false, null, null, DeckClipSoft);
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
                         "Highlight", QPrItemHL, false, null, null, DeckClipSoft);
            if (hl != null) hl.gameObject.SetActive(cur);
            // ② `Button border`（`UI_Button_Round_background`，原版 `preserveAspect=1` ⇒ `keepAspect`）
            // 🔴 **2026-10-24（`W8` · P1 差异 #4）**：原版 `InitializeWithPracticeDeck` **开场 `set_enabled(false)`**，
            //   **只有「玩家自己的卡组」那一支再 `set_enabled(true)`**（预组支不动它）⇒ 今天**只有首格
            //   「你的卡组」**有这一圈。我们原来 7 格无条件可见（而且它中心不透明纯黑、只靠 `Main image` 盖着）。
            var bd = Img(cell.transform, "UI_Button_Round_background",
                ix + ItemBdL, iy + ItemBdT, ix + ItemBdR, iy + ItemBdB,
                "Button border", QPrItemBd, true, null, null, DeckClipSoft);
            if (bd != null) bd.gameObject.SetActive(isPlayerDeck);
            // ③ `Main image` —— 原版**运行期喂** `ArmyUtilities.GetArmyIcon(army)`
            //    （`DeckSelectorMenuItemDemo__InitializeWithPracticeDeck.c` 的 SO 那一支；
            //     我方卡组那一支在 `InitializeWithPlayerDeck.c` 里也是同一句）。
            //    🆕 **2026-10-22**：练习对那一支改用**产物里的 `armyIcon`**（`DemoDeckInfoSO.armyIcon`，
            //    13 张 `40k_DeckSelection_icon_*`，与 `ArmyUtilities.GetArmyIcon` 逐阵营同一张）。
            //    取不到 ⇒ 回落 `DeckRuntime.FactionIcon(faction)`（同一份映射，判据只那一处）。
            //    🆕 **2026-10-23（W65 · 1c）**：图从形参 `mainIcon` 进来 —— 「你的卡组」那一格走**同一句**
            //    `GetArmyIcon` 的结果（只是不看 `DemoDeckInfoSO.armyIcon`），⛔ 别为它另写一条取图路。
            ImgTex(cell.transform, mainIcon, iconNote,
                   ix, iy, ix + DeckCellW, iy + DeckCellH,
                   "Main image", QPrItemArt, false, null, null, DeckClipSoft);
            // ④ `IsPlayerDeck`（`Purity Seal_02`）—— `highlightIsPlayerDeck`
            // 🔴 **2026-10-24（`W8` · P1 差异 #3）**：原版**只有「玩家自己的卡组」那一支 `SetActive(true)`**，
            //   预组支一律 `SetActive(false)`（`DeckSelectorMenuItemDemo__InitializeWithPracticeDeck.c`；
            //   参考图 `练习界面参考图.png` 里只有第 1 格有火漆印）。我们原来 7 格无条件可见。
            //   现在**照旧建**（自检要能在同一批节点上断两态）、只按 `isPlayerDeck` 开关。
            var seal = Img(cell.transform, "Purity_Seal_02",
                ix + ItemSealL, iy + ItemSealT, ix + ItemSealR, iy + ItemSealB,
                "IsPlayerDeck", QPrItemSeal, false, null, null, DeckClipSoft);
            if (seal != null) seal.gameObject.SetActive(isPlayerDeck);
            // ⑤ `Text background`（`40k_bt_underbutton`）+ 其下的 `Deck Name`。
            //    🔴 **2026-10-22：练习对那一支的色 = `DemoDeckInfoSO.armyColor`**（用 `armyColorRaw` 的
            //       原样 4 个 float —— 原版存的是任意浮点、不是 8bit/255，`#RRGGBB` 只是便利值）。
            //       判据 = `DeckSelectorMenuItemDemo__InitializeWithPracticeDeck.c` 末尾那段
            //       `set_color(this+0x48, demoDeck.armyColor)`（`+0x48` = `Text background` 那颗 Image）；
            //       对照：同一文件里**玩家自己的卡组**那一支（`InitializeWithPlayerDeck.c`）用的是
            //       `yourDeckColor` —— 两支的色**不同源**，⛔ 别拿 `ItemYourDeckColor` 顶上。
            //       🆕 **2026-10-23（W65 · 1c）**：色从形参 `nameBg` 进来 —— 「你的卡组」那一格**正是**
            //       原版那个 `yourDeckColor` 的落点（`ItemYourDeckColor`），⛔ 别再让它只当兜底。
            //       prefab 里 Image 自带的 `m_Color=(0.408,0.811,0.279,1)` 是**模板默认**、会被盖掉。
            Img(cell.transform, "40k_bt_underbutton",
                ix + ItemNameBgL, iy + ItemNameBgT, ix + ItemNameBgR, iy + ItemNameBgB,
                "Text background", QPrItemTxBg, false,
                nameBg, null, DeckClipSoft);
            // 文字与图**同一套**：原版 `RectMask2D` 对 TMP 一视同仁 ⇒ 走 `MenuDraw.ClipText` 同一个剖面
            // 🔴 **㉒③**：名字条 = **172×30.6**（照原版锚点现算）+ **原版 `fs29 / auto[8,29]` / 居中 / 折行 1**
            //    —— 原来是「顶 38px + 172 宽 + fs26 + **没有自适应**（我们挑的）」⇒ 长卡组名画到框外。
            var nm = Txt(cell.transform, displayName,
                         ix + ItemNameL, ix + ItemNameR, iy + ItemNameT, iy + ItemNameB,
                         ItemNameFontPx, Align.Center, "Deck Name", QPrText, null, DeckClipSoft,
                         // 🔴 **2026-10-17（`A1233`）**：原来这里是「先 `Txt`、再自己补
                         //    `nm.SetAutoFitBox(…)`」—— 收口进本方法形参（同一件事只写一处）。
                         //    ⚠️ `wrapPx` **必须显式给**（本件的原版折行 = 1）：不给的话本方法末段
                         //    会走 `if (autoMinPx > 0f && wrapPx <= 0f) lb.SetWrapping(false)` 那一支，
                         //    把折行悄悄关掉（下面那句显式 `SetWrapping(true)` 要留着 —— 理由见它自己的注释）。
                         wrapPx: ItemNameR - ItemNameL,
                         autoMinPx: ItemNameAutoMin, autoMaxPx: ItemNameAutoMax, autoBasePx: ItemNameAutoBase,
                         face: TmpFont.Face.LatinCardBody);
            if (nm != null)
            {
                // 原版 `m_TextWrappingMode = 1 (Normal)`；`SetAutoFitBox` 内部已经开了折行，
                // 这里**显式**再写一次（它现在是 `Label` 上唯一那个显式口，别让以后改公共件时静默回退）。
                nm.SetWrapping(true);
                // 🔴 **必须再推一次版面**：`SetAutoFitBox` 里那次 `RefreshBounds()` 会把 TMP 子节点
                //    **挪一个位**，而 `ClippedTextGuard` 的重裁发生在**挪之前**的 `ForceMeshUpdate` 里
                //    ⇒ 那一刀整体偏「新旧位置之差」（本窗实测约 (12, 5)px，**静默**）。
                //    见 `RelayoutNow` 的注释：它重排一次（顺带在**挪到位之后**重裁）+ 再收一次尺寸。
                RelayoutNow(nm);
            }
            // 🔴 **2026-10-13（A435 阶段 2 · 戊 / A754-b）**：`clip` 传 `null`（同 `RebuildArmyCells`）——
            //   命中区要的【裸框 + pad】由那颗 `Viewport` 节点给（`HitOn` → `MenuDraw.ClipRectAbove`）；
            //   软边**故意不传**（原版 `m_Softness` 只改渲染，射线那一面只看矩形）。
            var rowHit = HitOn(cell.transform, cell.transform, "Hit", rr, onClick, QPrHit, null);
            var rowWb = rowHit != null ? rowHit.GetComponent<WindowButton>() : null;
            if (rowWb != null) rowWb.Bind(rowBg, "UI_Button_Mulligan");
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 C2）**：原版
            //   `Practice Mode Menu/Deck info/Deck List Drawer/Content/Deck Selector Card Info button_ref`
            //   = 空 cue + **`useDefaultAudioClipIfNull = 0`** ⇒ **静音**。
            if (rowWb != null) rowWb.playClickSound = false;
            return cell.transform;
        }

        /// <summary>🆕 **2026-10-07（A77-㉒③/⑤）**：把一次「改完折行模式 / 改完自适应」的**版面真正推下去**。
        /// <para>🔴 **2026-10-08（A214②）就地退化成直调公共件**：实现已收进 `Battle/Label.cs` 的
        /// <see cref="Label.ForceRelayout"/>（A205 那一件把它从本函数原样收上去的）⇒ 这里只留**转调**。</para>
        /// <para>**先核过行为等价**（逐句对照，不是「看着像」）：
        /// · `_tmp != null` 时 —— 旧 = `SetFontSize(_tmp.fontSize)` + `RefreshBounds()`；
        ///   新 = `ForceRelayout()` 的**同样两句**（`Label.cs` 的实现一字不差）⇒ **逐位相同**。
        /// · `_tmp == null`（点阵后端）时 —— 旧 = 第一句在 `SetFontSize` 里**早退**、第二句 `RefreshBounds()`
        ///   在 `Battle/Label.cs` 的 `ForceRelayout` 也是 **`if (_tmp == null) return;`** ⇒ **两句都是空操作**；
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
        //
        // 🔴 **2026-10-13（A435 阶段 2 · 戊 / A754）就地续写（铁律 5，上面那几段一字未删）：**
        //    本窗两处视口之后又把状态**从「逐件传 `view`」搬到了那两颗 `Viewport` 节点上**
        //    （`ViewportClip`）⇒ 上面第 4 行那句「两个格子里的每一颗图/字**自己**把 `view` 当 `clip` 传」
        //    **已过期**：现在两处构建循环逐件传的是**字面 `null`**（`Img`/`ImgTex`/`Txt`/`HitOn`），
        //    粗筛也从裸 `MenuDraw.Visible` 换成了 **`VisibleAbove`**（节点态那一版）。
        //    ⇒ 这一整段讲 `Inside` 的历史**照旧成立**（`Inside` 确实已删、语义确实是「相交就建」），
        //    只是「谁来裁」那一层换了载体 —— ⛔ 别按第 4 行那句话去推「本窗今天还逐件传框」。

        /// <summary>当前（模式 + 阵营）筛选下**画出来的格数** = 练习对的格数 + 「你的卡组」那一格（有才加）。
        /// <para>🔴 **与 <see cref="FilteredDeckCount"/> 分开**：那个只数**对**（78 这个数字到处被引用、口径别动），
        /// 这一个才是**铺格子**用的（滚动内容高 / 行数）。⛔ 别把「你的卡组」那格算进 `FilteredDeckCount`
        /// —— 会把「练习对 = 78」那一族判据一起改掉。</para></summary>
        public int ListedCellCount { get { return FilteredDeckCount() + (ShowYourDeckCell() ? 1 : 0); } }

        int FilteredDeckCount()
        {
            // 🔴 **2026-10-22**：与 `RebuildDeckRows` 同一套筛选（**对** + 当前模式那一副 + 阵营）——
            //    两处必须同源，否则「内容高」按 A 算、列表按 B 建（滚到一半是空白）。
            int n = 0;
            var pairs = PrebuiltDecks.Pairs;
            for (int pi = 0; pi < pairs.Count; pi++)
            {
                var deck = PrebuiltDecks.PairDeck(pairs[pi], SelectedGameMode);
                if (deck == null) continue;
                if (ArmyIndex >= 0 && _facs != null && ArmyIndex < _facs.Count && deck.faction != _facs[ArmyIndex]) continue;
                n++;
            }
            return n;
        }

        /// <summary>`Selected Army Title` 现在该显示的那句话。
        /// 🔴 **2026-10-24（`W8` · P1 差异 #2）就地更正**：原来返回 `_facs[i].ToUpperInvariant()`
        /// ⇒ 真机上印的是 **`GENESTEALERS`** 这种英文大写键。原版 `ChangeSelectedArmy` 走的是
        /// `LocalizationManager.GetTermTranslation(String.Format(<格式串>, army))` —— **本地化词条**
        /// （参考图 `练习界面参考图.png` 显示 **基因窃取者教团**）。
        /// 键 = `Armies/<CardArmy 成员名>`，官方表那 13 条与 `CardArmy` 成员名**逐个同名**
        /// （`Ultramarines…SpaceWolves`）⇒ 直接 `Loc.T(CardText.ArmyTermPrefix + fac)`
        /// （`CardText.ArmyTermPrefix` = `"Armies/"`，同一份前缀常量，⛔ 别在本地再写一遍字面量）。
        /// ⚠️ `Loc.T` 对缺键**返回键名本身**（= 界面会印 `Armies/Goff`）⇒ 这里多一道闸：查不到就回
        /// **裸阵营键**（不印键名、也不 `ToUpperInvariant`）并出声一次；「13 个全部取到真文案」由
        /// `Editor/ShellScene.cs` 的断言兜（缺 0）。</summary>
        string SelectedArmyName()
        {
            if (ArmyIndex >= 0 && _facs != null && ArmyIndex < _facs.Count)
                return ArmyLocalized(_facs[ArmyIndex]);
            // 🔴 **2026-10-21**：阵营一律从 <see cref="CurrentDeck"/> 的督军推 —— 预组档 `DeckIndex` 是 −1，
            //   原来那句 `CollectionData.DeckAt(DeckIndex)` 拿到的是**空 `DeckInfo`** ⇒ 预组明明有阵营，
            //   标题却回到「（未选阵营）」（真机 2026-10-21）。⛔ 别再改回读下标。
            string fac = FactionOf(CurrentDeck != null ? CurrentDeck.WarlordId : null);
            // 兜底那句也走词条（键 `MenuDeck/HUD/NoArmySelected`；
            //   ZH 列与改前写死串**逐字相同** ⇒ 中文档零变化）。⚠️ P3 两个附件源都漏了这条（它会画上屏）。
            return string.IsNullOrEmpty(fac) ? Loc.T("MenuDeck/HUD/NoArmySelected") : ArmyLocalized(fac);
        }

        /// <summary>阵营键（`CardArmy` 成员名）→ 官方中文/本地化阵营名（原版 `Selected Army Title` 那一句）。
        /// 缺键 ⇒ 回**裸键**并出声一次（⛔ 不印 `Armies/xxx` 这种键名 —— 那等于静默失败）。</summary>
        static string ArmyLocalized(string fac)
        {
            if (string.IsNullOrEmpty(fac)) return "";
            string key = CardText.ArmyTermPrefix + fac;
            string v = Loc.T(key);
            if (string.IsNullOrEmpty(v) || v == key)
            {
                if (!_warnedArmyTerm)
                {
                    _warnedArmyTerm = true;
                    Debug.LogWarning("[Practice] 官方表里没有阵营词条 `" + key + "` ⇒ 阵营标题回落裸键「"
                                     + fac + "」（⛔ 不印键名；`Editor/ShellScene.cs` 有一条「13 个阵营全部命中」的断言）");
                }
                return fac;
            }
            return v;
        }
        static bool _warnedArmyTerm;

        /// <summary>督军名的**显示文案** —— 原版两支**不同源**，⛔ 别统一：
        /// · **你的卡组**支 = `deckHero` 的**裸名**（英文；`DeckGeneralInfoDemo.ShowPayerDeckInfo`）；
        /// · **预组（演示卡组）**支 = `RawCardScript.GetLocalizedCardName(hero)` = **本地化卡名**
        ///   （`DeckGeneralInfoDemo.ShowDemoDeckInfo`）。
        /// 🔴 **2026-10-24（`W8` · P1 差异 #5）**：我们原来两支都读 `CardDef.Name`（英文）⇒ 预组那一支错了。
        /// 判据 → `资料/普查产出_1024d/P1_练习窗原版对照审查.md` §二 `Warlord Name` 行：
        /// 参考图显示英文，是因为当时选中的正是**「你的卡组」**那一支 —— 原版那一支恰好用裸名。
        /// 本地化通道 = `CardText.Name(CardDef)`（中文档取 `CardDef.NameZh`；其余语言取官方 `Card_Name/&lt;id&gt;`）。
        /// ⚠️ 兜底键 `MenuDeck/Error/NoWarlord` 是**自造键**（`Resources/official_names.json` 与官方 13 语言表
        /// **0 命中**；P1 §四 差异 #7）—— 官方那族里只有 `MenuDeck/Error/MissingHero`，但它是**整句校验文案**
        /// （「请为您的卡组选择一个战将，将其拖入您的卡组列表。」），不是这一格的占位名 ⇒ **不换**，
        /// 就地标明「自造」，⛔ 不编中文。</summary>
        string WarlordNameOf(RuleEngine.CardDef wl)
        {
            if (wl == null) return Loc.T("MenuDeck/Error/NoWarlord");
            return PickedPrebuilt != null ? CardText.Name(wl) : wl.Name;
        }

        /// <summary>`Warlord Name` 现在显示的那句话（自检用；读的是**真 `Label`**，
        /// ⛔ 不是重新算一次 `WarlordNameOf()` —— 那会与实现同一个口 = 自证）。</summary>
        public string WarlordNameText { get { return _txtWarlord != null ? _txtWarlord.Text : ""; } }

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
                    // 🆕 **2026-10-26（批次 6 · G15）**：原版「换卡组」那条链 = `DeckGeneralInfoDemo.ChangePlayerDeckButton`
                    //   → `DeckSelectionPopup`（回调 `DeckChanged`）→ 本窗的 `PlayerDeckChanged`（判据 → 那个方法的 doc）。
                    //   ⚠️ **预组也用同一条回调**（原版 `DeckChanged(CardDeck)` 不区分预组/自建，它只拿到 `CardDeck`）
                    //   ⇒ 阵营筛选照原版跟到这副牌的阵营（哪怕它是从「预组」页挑的）。
                    if (pick.PrebuiltDeck != null) PlayerDeckChanged(pick.PrebuiltDeck.faction);
                    return;
                }
                int idx = CollectionData.IndexOf(pick.Info.Name);
                if (idx >= 0)
                {
                    PickDeck(idx);
                    // 🆕 2026-10-26（批次 6 · G15）：同上 —— 换了自己的卡组之后阵营筛选跟到新卡组。
                    PlayerDeckChanged(CollectionData.DeckAt(idx).Faction);
                }
            }, (int)SelectedGameMode);   // 🆕 2026-10-17（U4-1）：按**开关当前的赛制**筛「我的卡组」页
            // 🔴 **2026-10-17（U4-1）就地订正（铁律 5）**：这里原来写「原版练习窗里有个 `Game mode` 开关我们**没建**
            //    ⇒ 写死经典」—— **已经建了**（`BuildGameModeToggle`），模式按 `SelectedGameMode` 走。
            //    判据 = `PracticeModePopup__BattleButtonOnClick.c:96-160`（按 `gameMode` 在两套卡组/督军数据里二选一）
            //    + `DeckSelectionPopup.__TryOpen_b__10_0`（候选卡组模式 == `context.GameMode` 那条筛选）。
            //    ⚠️ 预组那一页**照原版不筛**（`DeckSelectionPopup.Shown` 只筛 `OwnDecks`）。
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
            _cardHolder = new GameObject("Deck List Drawer", typeof(RectTransform)).transform;
            _cardHolder.SetParent(parent, false);
            // 🔴 **2026-10-11（A218）**：抽屉节点也是 `RectTransform` + 写 `sizeDelta`
            //    （= `DlL/DlT/DlR/DlB` 那块 **569.79 × 619.53** —— 与 `localPosition` 同一份换算）。
            //    判据 = 原版 `Deck List Drawer`（`DeckEditingWindow`/`DeckGeneralInfoDemo.cardsInDeckPanel`
            //    那颗 `GameObject`）实读是 `RectTransform`（A92 那张类型表）。改坏法：删掉 `SetPxSize` ⇒
            //    `Editor/MainMenuScene.cs` §A218「`Deck List Drawer` 的 rect = 抽屉矩形」红。
            MenuDraw.SetPxSize(_cardHolder, DlR - DlL, DlB - DlT);
            _cardHolder.localPosition = Local3(parent, DlL, DlT, DlR, DlB);
            _deckList = _cardHolder;

            // 抽屉自己那颗钮（原版 `Show Deck General Info button`：`UI_Button_Round_background` + `40k_UI_bt_back`）
            //   ⇒ 原版 `deckGeneralInfoButton → DeckGeneralInfoButtonOnClick`：`General container.SetActive(true)`
            //     + `Deck List Drawer.SetActive(false)`（**切回总览**）。
            Img(_cardHolder, "UI_Button_Round_background", ShowBtnL, ShowBtnT, ShowBtnR, ShowBtnB, "Show Info Bg", QPrRow, true);
            // 🔴 图标在钮面之上（同 `Back Icon` 那条）。
            Img(_cardHolder, "40k_UI_bt_back", 1737.68f, 246.46f, 1785.07f, 293.12f, "Show Info Icon", QPrFront, true);
            HitOn(_cardHolder, _cardHolder, "ShowInfoHit", new PxRect(ShowBtnL, ShowBtnT, ShowBtnR, ShowBtnB),
                  () => Toggle(true));

            // 原版 `Content` 是布局组节点；我们的 `Deck List Drawer` 的 localPosition 已经把它摆到 (1240.38,207.65)，
            // 而 `Content` 的绝对矩形是 1261.27,318.42→1787.37,793.87 ⇒ 子件的坐标**一律用页面绝对 px**，
            // `Local3(_cardHolder, …)` 会自己换算（这是本工程画图小工具的约定）。
            // 🔴 **2026-10-21**：卡表读 <see cref="CurrentDeck"/>（唯一判源）—— 预组档 `DeckIndex` 是 −1，
            //   原来那句 `CollectionData.Raw(DeckIndex)` 返回 null ⇒ 直接 return、**卡表根本不建**
            //   （真机 2026-10-21 的「右侧半空」）。预组那一支的 `CardIds` 就是 `Deck.cardIds`，
            //   防御卡照旧由下面 `deck.DefensiveId` 那一段并进来（与 `DeckInfoPopup` 同一条口径）。
            var deck = CurrentDeck;
            if (deck == null) return;

            // 🔴 **2026-10-12（A1531⑧）这一支照 `DeckListDrawer` 修**（本窗挂的就是那个类；判据 =
            //    `Practice Mode Menu ◂ Deck info ◂ Deck List Drawer` 的组件字段，`工具/_probe_deckinfo.py`
            //    按 GO pid `-3777783439481286852` 亲读：**`showWarlord = 0` · `stackCopies = 0`** ·
            //    `checkOwnership = 0` · `defensiveCardInfoItem = null`）：
            //      · **没有督军行** —— `DeckListDrawer__Initialize.c:77-93` 在 `showWarlord = 0` 时套
            //        `Where(*(int*)(card + 0x94) != 10)`（卡种 ≠ 督军）⇒ 原来 `order.Add(deck.WarlordId)`
            //        那一行是**多的**。
            //      · **`stackCopies = 0` ⇒ 每份一行、不合并**（`Initialize.c:154` 那一支直接
            //        `SpawnCardInfo` 建新行）—— 与卡组编辑窗（`stackCopies = 1`：同名合并成一行 + `xN`）
            //        **相反** ⇒ 这里**不按 id 去重**，`CardIds` 有几条就几行（同名几张就是几行、也没有 `xN`）。
            //      · **排序 = 费用升序 → 本地化卡名**（`DeckListDrawer__Reorder.c:33-40`，`Initialize.c:112`
            //        建完行**无条件**调）—— 与 `Shell/DeckInfoPopup.BuildDeckList` ② **同一把尺**。
            //        ⚠️ 这段 lambda 现在**两处各一份**（本文件 + `DeckInfoPopup`）——
            //        建议下一轮收口成共用件（报告 §第二轮·给主对话），⛔ 别只改一处。
            //      · **防御卡**：原版这里也是**两件事**（同 `DeckInfoPopup.BuildDeckList §⑤`，判据不再抄第二份）：
            //        **(甲) 进列表** = `GetLibraryInFull.c:29-42` 把 `deck.defensiveCard`(`+0x48`)
            //        `Insert(0)` 进卡表（那条 `Where` 只滤督军 ⇒ 它留下）；
            //        **(乙) 钉第 0 格** = `Reorder.c:82-89` 认的是**卡身上的两截判据**
            //        `cardType == 0x14`(**`CardTypeOptions.Tactic`** —— ⚠️ 不是「防御卡」！) **且**
            //        `spellType == 0xd2`(**`SpellType.DefensiveCard = 210`**)；不满足就**不钉**、按费用参与排序。
            //        ⚠️ `defensiveCardInfoItem = null` **不等于「不画」**：`SpawnCardInfo.c:40-51` 判空后
            //        **兜底**用通用 `cardInfoItem`（`param_1 + 0x20`）⇒ 照样 `Instantiate` 建行。
            //        🔴 数据缺口（`spellType` 我们没这一列）与近似写法 → `DeckInfoPopup.IsDefensiveRow` 的 doc。
            var cards = new List<CardDef>();
            if (deck.CardIds != null)
                foreach (var id in deck.CardIds)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    var c = CollectionData.Card(id);
                    if (c != null) cards.Add(c);      // ⛔ 不去重（`stackCopies = 0`）
                }
            // ⑤（甲） **防御卡一定在列表里**（原版 `GetLibraryInFull.c:29-42` 把卡组那个槽的卡 `Insert(0)`）
            //    —— 两截判据**不成立**时它照样在列表里、只是**不被钉**（见下），所以先并进【待排】这一支。
            //    ⚠️ 我们这份数据上「在槽里」蕴涵「是防御卡」⇒ 这一支**不可达**，照原版形状留着（⛔ 别删）。
            var defensive = string.IsNullOrEmpty(deck.DefensiveId) ? null : CollectionData.Card(deck.DefensiveId);
            if (defensive != null && !DeckInfoPopup.IsDefensiveRow(defensive)) cards.Add(defensive);
            cards.Sort((a, b) =>
            {
                int c = a.Cost.CompareTo(b.Cost);
                if (c != 0) return c;
                c = string.CompareOrdinal(CardText.Name(a.Name, a.NameZh),
                                          CardText.Name(b.Name, b.NameZh));
                if (c != 0) return c;
                return string.CompareOrdinal(a.Id, b.Id);
            });
            var order = new List<CardDef>();
            if (defensive != null && DeckInfoPopup.IsDefensiveRow(defensive)) order.Add(defensive);  // (乙) 钉第 0 格
            order.AddRange(cards);

            float oL = DlContentL + DlPadL, oT = DlContentT + DlPadT;
            // 🔴 **逐列填充**（原版 `m_StartAxis = 1(Vertical)`，算式见 `ListRowCap`）——
            //    原来写 `i % cols, i / cols` 是 `Horizontal` 那一档（逐行）。
            int rowCap = ListRowCap;
            for (int i = 0; i < order.Count; i++)
            {
                var card = order[i];
                int c = i / rowCap, rr = i % rowCap;
                float x1 = oL + c * (DlCellW + DlGapX);
                float y1 = oT + rr * (DlCellH + DlGapY);
                var cell = new GameObject("CardRow_" + i, typeof(RectTransform));
                cell.transform.SetParent(_cardHolder, false);
                // 🔴 **2026-10-11（A218）**：卡格也是 `RectTransform` + 写 `sizeDelta`
                //    （= `DlCellW × DlCellH` = **231 × 27.88**，与原版 `UICardInfoItem` 那一格的
                //    `GridLayoutGroup` 格尺寸同源；尺寸与位置同一份换算）。改坏法：删掉 `SetPxSize` ⇒
                //    `Editor/MainMenuScene.cs` §A218「`CardRow_<i>` 的 rect = 卡格格矩形」红。
                MenuDraw.SetPxSize(cell.transform, DlCellW, DlCellH);
                cell.transform.localPosition = Local3(_cardHolder, x1, y1, x1 + DlCellW, y1 + DlCellH);
                var nm = Txt(cell.transform, CardText.Name(card.Name, card.NameZh), x1, x1 + DlCellW, y1, y1 + DlCellH, 20f, Align.Left,
                             "Name", QPrText, null, default(Vector2),
                             // 🔴 **2026-10-17（`A1233`）**：原来这里是「先 `Txt`、再自己补
                             //    `nm.SetAutoFitBox(DlCellW, DlCellH, 2, 38, 27.69)`」—— 收口进本方法形参。
                             //    ⚠️ **故意不传 `wrapPx`**：本件的原版折行 = 0，而本方法在
                             //    `autoMinPx > 0 && wrapPx <= 0` 那一支会调 `SetWrapping(false)` ——
                             //    正是这一站要的那一档（下面那句显式 `SetWrapping(false)` 仍留着，理由见它自己的注释）。
                             autoMinPx: CardNameAutoMin, autoMaxPx: CardNameAutoMax, autoBasePx: CardNameAutoBase,
                             face: TmpFont.Face.LatinCardBody);
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
                //        时 TMP 会**往上长到装不下为止**（`TextMeshPro.cs:2151` 起点 = `Clamp(base,min,max)`（`TextMeshPro` 的自适应那一句）；
                //        天花板 = `fontSizeMax`；「长高/长宽到溢出就回退」那两支在 `TextMeshPro` 里）
                //        —— 这正是原版那一档（它的 `m_fontSize` 也是 38）。
                //      🔴 **没查清 / 仍是偏离的一处（如实标）**：原版这颗 `Card Name` 的**框**是它父件
                //        `Text fill`（锚 `(0.13,0.07)→(0.99,0.93)` · sizeDelta `(−5,0)`）⇒ 高 **23.98**；
                //        而 `Text fill` 宽由 `HorizontalLayoutGroup` 现算、`menu_dump` 打的是 `unk`
                //        （首选宽要 Unity 的字体度量）⇒ **宽度查不到**。我们这边没有 `Text fill` 这一层，
                //        框直接取**整格 231×27.88** ⇒ 收敛出来的字号会比原版**略大**（高多 3.9px）。
                //        ⛔ 这不是「原版就是这样」，是**我们知道的一处差别**，记在这儿等判据齐了再收。
                if (nm != null)
                {
                    // 原版 `Card Name` 是 `m_TextWrappingMode = 0`（**不折行**）—— 必须在 `SetAutoFitBox`
                    // **之后**调（那个函数内部无条件走 `SetWrapWidth` ⇒ 会把模式开成 `Normal`）。
                    nm.SetWrapping(false);
                    // 🔴 **再推一次版面**：`textWrappingMode` 的 setter 只 `SetVerticesDirty()`，批处理里
                    //    **没有帧循环** ⇒ mesh 还停在**折行**那一版（字段说不折行、画面却折着）——
                    //    见 `RelayoutNow` 的注释（顺带让「渲出来多宽」= 不折行时多宽，那条宽度断言才有鉴别力）。
                    RelayoutNow(nm);
                    // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版 `Content/Text fill/Card Name`
                    //   = `Left/**Capline**`（判据 = 上面 :1375-1376 那条亲读的 `menu_dump … "Deck Selector
                    //   Card Info button" --depth 5 --md`：`对齐 Left/Capline`）。
                    //   ⚠️ 排在 `SetAutoFitBox` / `SetWrapping(false)` / `RelayoutNow` **之后**（档位本身幂等，
                    //   但位移要按**收敛后**的字号算 —— 早调会在重排后被 `RefreshBounds` 按新字号重算一次，
                    //   结果一样；放在最后只为**少一次重排**，语义与顺序纪律都不冲突）。
                    MenuDraw.SetVAlign(nm, Label.VAlign.Capline, new PxRect(x1, y1, x1 + DlCellW, y1 + DlCellH));
                }
                CardRows.Add(cell.transform);
            }
        }

        // ============================================================ 交互

        void PickArmy(int i)
        {
            // 🔴 **2026-10-19（批次 D · 红 #4/#5）**：原版 `PracticeModePopup__ChangeSelectedArmy.c:30-33`
            //   **开头第一句**就是 `if (param_2 == *(int *)(param_1 + 0xe8)) return;` ⇒ **同阵营再点 = 直接返回**
            //   （不改筛选、不重建列表）。改前这里是 toggle（`ArmyIndex == i ? -1 : i`）——
            //   那是我们自设计的「再点取消」，原版没有，已删。
            //   ⚠️ 练习窗的阵营格是真 prefab `Practice Army Select Button` 上的 **`EverguildButton`**（**不是 Toggle**）
            //   ⇒ ⛔ 别改用 `WindowButton.allowClickWhenAlreadySelected = false` 来表达这件事：那会把原版
            //   第二次点击**仍会响的默认按钮音**一起吞掉（`m_onClickSound=null` + `useDefaultAudioClipIfNull=1`）。
            if (ArmyIndex == i) return;
            ArmyIndex = i;
            if (DeckScroll != null) DeckScroll.SetOffset(0f);
            // 🔴 **2026-10-23（W65 · 1c 收尾）**：换阵营 = **重列 + 默认选中** —— 原版
            //   `PracticeModePopup__ChangeSelectedArmy.c:61` 的**末句**就是 `SetArmyButtons(param_1, param_2)`
            //   （与切模式那条同一份逻辑：建完第 0 项就 `DeckSelected(item0)`）。
            //   改前这里只 `RebuildDeckRows` ⇒ **面板上会留着刚被筛掉的那一对牌**
            //   （列里已经没有它、`Battle!` 却还拿它开战），原版不会出现这种态。
            //   🧨 改坏法：把这两行换回 `if (_deckHolder != null) RebuildDeckRows(_deckHolder);`
            //   ⇒ 切到别的阵营之后 `PickedPrebuilt` 仍是旧阵营那一副（断言「选中态必须在列表里」红）。
            if (!SelectDefaultDeck())
                ShowNoDeckSelected("阵营筛选：" + (ArmyIndex < 0 ? "不限" : _facs[ArmyIndex]));
            // 🆕 **2026-10-26（批次 6 · G15）：`ChangeSelectedArmy` 的【高亮那一圈】补上。**
            //   原版 `PracticeModePopup.ChangeSelectedArmy`（`cpp2il/out_cpp2il_dev_ilspy/PracticeModePopup.cs:745-853`）
            //   在 `selectedArmy = army` 之后、`SetArmyButtons(army)` 之前还有一整圈：
            //     · `selectedArmyName.text = GetTermTranslation("Armies/{army}")`（我们由 `PickDeck`/`ShowNoDeckSelected` 写 `_txtArmy`）
            //     · **逐格** `SetActive(btn.highlight,  btn.army == army)`
            //     · **逐格** `SetActive(btn.hasPlayerDeck,
            //                  wrapper.cardDeck == null ? false : btn.army == wrapper.cardDeck.deckHero.cardArmy)`
            //   ⇒ 这两颗显隐的**唯一写点**就是那一圈（另一处 = `ConfigureArmyFilterButtons` 的出厂全关，
            //     判据写在 `RebuildArmyCells` 头上那一段）。我们原来只有「建窗 / 滚轮」会重画那一列
            //     ⇒ **点一格之后高亮停在旧格、火漆印不跟卡组**（G7 §⑥·② 第 9 条）。
            //   🔑 现成的补法 = 本文件另一条入口（`StartPracticeMatch`）已经这么做的那一句：
            //     `w.ArmyScroll.OnChanged()` ⇒ 重画阵营列。**不整列重建新的 Scroll**（`OnChanged` 只是回调
            //     `RebuildArmyCells`），所以**滚动位置不动**：`RebuildArmyCells` 用 `ArmyScroll.Shift(r)` 摆格，
            //     `MenuScroll.Offset` 是它自己的字段、重建格不会重置它。
            //   ⚠️ **顺序与原版不同（如实记）**：原版先刷高亮、后 `SetArmyButtons`（重列 + 默认选 item0）；
            //     我们反过来（先 `SelectDefaultDeck` 再重画）—— 因为我们的火漆印读的是 `CurrentDeck`
            //     （唯一判源），而它**在 `SelectDefaultDeck` 里才换**。净可见状态相同（都是终态），
            //     只是中间那一帧没有高亮（原版那一帧也没有高亮，两边都不上屏）。
            if (ArmyScroll != null) ArmyScroll.OnChanged();   // 阵营列重画 ⇒ 高亮落到这一格 + 火漆跟卡组
            Debug.Log("[Practice] 阵营筛选：" + (ArmyIndex < 0 ? "不限" : _facs[ArmyIndex]));
        }

        void PickDeck(int i)
        {
            DeckIndex = i;
            PickedPrebuilt = null;      // 选了「我的卡组」⇒ 预组那次选中作废（两个是互斥的）
            // …左列那一格（练习对）的选中态一起作废。🆕 **2026-10-23（W65 · 1c）就地订正（铁律 5）**：这里原来
            //   写「（「我的卡组」不在那一列里）」—— **已不成立**：原版那一列**首格就是**「你的卡组」
            //   （`PracticeModePopup__SetArmyButtons.c` 的第 0 项），我们本轮也已按原版补上（见 `RebuildDeckRows`）。
            //   ⛔ 别按那句旧话去推「「我的卡组」不在这列 ⇒ 不用重画选中态」：重画由 `RebuildDeckRows` 自己按
            //   `PickedPrebuilt == null && DeckIndex >= 0` 判，两处判据不要各写一份。
            PairIndex = -1;
            PrebuiltDecks.ClearPendingBattleDeck();
            if (_deckHolder != null) RebuildDeckRows(_deckHolder);
            bool wasList = !ShowingGeneralInfo;             // 换卡组时**别把抽屉状态翻掉**
            BuildCardRows(_info);                          // 重建卡列表（卡组换了）
            Toggle(wasList ? false : true);
            // 🔴 **2026-10-21（审查代理 #1）**：换「我的卡组」也要重画费用曲线 —— 同一副面板上
            //   「哪副牌」只有一个判源（`CurrentDeck`），**曲线不能比它慢一拍**。
            //   原来只有建窗那一次算 ⇒ 自建卡组互切之后 9 行仍是上一副的（与本次要修的预组症状同源）。
            RefreshCostRows();
            var info = CollectionData.DeckAt(i);
            if (_txtDeckName != null) _txtDeckName.SetText(info.Name);
            var wl = CollectionData.Warlord(i);
            if (_txtWarlord != null) _txtWarlord.SetText(WarlordNameOf(wl));
            if (_txtArmy != null) _txtArmy.SetText(SelectedArmyName());
            if (_armyIcon != null) _armyIcon.SetTexture(CardArt.MenuUi(DeckRuntime.FactionIcon(info.Faction)));
            SetCardbackTexture(CardArt.DeckCardback(info.CardbackId, info.Faction));   // 两层一起换 + 重播入场（账 卡组:138）
            // 🆕 2026-10-22（M2）：督军整身立绘跟着换（判源 = `CurrentDeck`，与上面那几件同一批）。
            RefreshCharacterImage();
            // 🆕 **2026-10-19（界面线 P05）**：玩家自己的卡组**没有 lore**（原版这份数据挂在
            //   `Demo DeckInfo` 上，只覆盖演示/预组卡组）⇒ 清空；`Change Deck` 照原版 `SetContent` 打开。
            if (_txtLore != null) _txtLore.SetText("");
            ShowChangeDeck(true);
            if (!_loreWarnedNoData)
            {
                _loreWarnedNoData = true;
                Debug.Log("[Practice] `Lore Text` 对「我的卡组」**留空** —— 原版这份数据来自 `DemoDeckInfoSO`，"
                          + "只覆盖演示/预组卡组（我们的自建卡组没有 lore，⛔ 不拿别的文案顶）");
            }
            Debug.Log("[Practice] 选中卡组：「" + info.Name + "」");
        }

        /// <summary>🆕 **2026-10-22**：点左列的一格 = 选中**一对**（原版 `PracticeModePopup.DeckSelected`
        /// → 记下那个 `DemoDeckInfoSO`）。一格带两副 ⇒ 这里按**当前 `Game mode`** 取那一副
        /// （原版 `DemoDeckInfoSO.GetDeck(gameMode)`），并把对下标记进 <see cref="PairIndex"/>。
        ///
        /// 🔴 **双卡组最容易错的一处**：切模式之后必须重取「**同一对的另一副**」——
        /// 所以 `SetGameMode → ResetContent` 会**保住 `PairIndex`** 再走一次本方法（见 `ResetContent`），
        /// 而开战读的 `CurrentDeck` / `PrebuiltDecks.PendingSource` **都由这一次 `PickPrebuilt` 写成同一副**。
        /// 🧨 改坏法：`PickPair` 里写死用 `pair.classic` ⇒ 切到 Skirmish 之后开战仍是 30 张那副（断言红）。
        /// </summary>
        public void PickPair(int i)
        {
            var pairs = PrebuiltDecks.Pairs;
            if (i < 0 || i >= pairs.Count)
            {
                Debug.LogWarning("[Practice] `PickPair(" + i + ")` 越界（共 " + pairs.Count + " 对）—— 忽略");
                return;
            }
            var pair = pairs[i];
            var deck = PrebuiltDecks.PairDeck(pair, SelectedGameMode);
            if (deck == null) return;                 // `PairDeck` 已经出声（查不到那一副）
            // `PairIndex` 必须在 `PickPrebuilt` **里面**写（它会立刻 `RebuildDeckRows` 重画选中态）
            PickPrebuilt(deck, pair.loreKey, i);
        }

        /// <summary>左列那一格的 `Main image` 图（见 `RebuildDeckRows` ③ 的判据注释）。
        /// 取不到配对图的 Sprite ⇒ 回落 `DeckRuntime.FactionIcon(faction)`（同一份映射，判据只那一处）。</summary>
        static Texture2D ArmyIconOfPair(PrebuiltDecks.Pair p, PrebuiltDecks.Deck d)
        {
            if (p != null && !string.IsNullOrEmpty(p.armyIcon))
            {
                var tex = CardArt.MenuUi(p.armyIcon);
                if (tex != null) return tex;
                Debug.LogWarning("[Practice] 练习对「" + p.so + "」的 `armyIcon = " + p.armyIcon
                                 + "` 在本工程 `Resources/Art/ui_*` 里取不到 ⇒ 回落该阵营的徽记");
            }
            return CardArt.MenuUi(DeckRuntime.FactionIcon(d != null ? d.faction : null));
        }

        /// <summary>
        /// 选中一副**预组卡组**（原版这条是「登记」型：`DeckGeneralInfoDemo.DeckChanged` → 记下用哪副牌，不立刻开战）。
        ///
        /// 我们这边多做一件：把它写进 `PrebuiltDecks` 的**「本局用这副牌」**通道 —— 因为
        /// `StartBotBattle` 原先只认 `DeckLibrary.Current`，而**预组不在玩家的卡组库里**。
        /// </summary>
        /// <param name="loreKeyOverride">🆕 **2026-10-22**：练习对那一路传**对上的** `loreLocalizationKey`
        /// （原版一个 SO 一条 lore、两副共用）；传 null = 用这副牌自己的 `Deck.loreKey`
        /// （选卡组窗的「预组」页那条路 —— 它没有「对」这一层）。</param>
        /// <param name="pairIndex">🆕 该副牌属于哪一对（`PrebuiltDecks.Pairs` 下标）；−1 = 不是从左列选的。</param>
        public void PickPrebuilt(PrebuiltDecks.Deck d, string loreKeyOverride = null, int pairIndex = -1)
        {
            if (d == null) return;
            // 🔴 **2026-10-21 就地重写（真机缺陷）**：这一支原来只做**增量刷新**（卡组名 / 督军 / 阵营图 /
            //   卡背 / lore），而面板另外四处读的是**另一份状态** `DeckIndex`（预组档 = −1）⇒ 真机症状：
            //   阵营标题回到「（未选阵营）」· 费用曲线 9 行全 0 · 卡表不建 · 点 `Battle!` 被
            //   「这套卡组还没有督军，开不了局。」挡死。
            //   ⇒ 现在照 `PickDeck` 那条「我的卡组」的路走一次**全量刷新**，两边**同一个判源**
            //   <see cref="CurrentDeck"/>（⛔ 别再回到「四处各读一份状态」那版）。
            //   ⚠️ `PickDeck` 那条路的行为一个字没改（它只多用了同一个判源，owned 档两处逐位相同）。
            DeckIndex = -1;                  // 显式作废旧下标（预组不在 `DeckLibrary` 里；⛔ 别让旧下标留在面板上）
            PairIndex = pairIndex;           // 🆕 左列那一格的选中态（−1 = 不是从左列选的 ⇒ 一格都不亮）
            PickedPrebuilt = d;
            PrebuiltDecks.SetPendingBattleDeck(d);
            // ① 卡组那一列：重列（左列是 78 对 —— 这里靠 `PairIndex` 标出「哪一格的选中态」；
            //    从选卡组窗选进来的预组没有「对」，`PairIndex = −1` ⇒ 一格都不亮）
            if (_deckHolder != null) RebuildDeckRows(_deckHolder);
            // ② 卡表 + 抽屉状态（换卡组时**别把抽屉状态翻掉** —— 同 `PickDeck`）
            bool wasList = !ShowingGeneralInfo;
            BuildCardRows(_info);                          // 卡表 = 预组那一副（走 `CurrentDeck`）
            Toggle(wasList ? false : true);
            // ③ 费用曲线 9 行（走 `CurrentDeck` ⇒ 预组的 `cardIds`）
            RefreshCostRows();
            // ④ 身份那几件
            if (_txtDeckName != null) _txtDeckName.SetText(d.DisplayName);
            if (_txtWarlord != null)
            {
                var wl = CollectionData.Card(d.heroId);
                _txtWarlord.SetText(WarlordNameOf(wl));
            }
            // ⚠️ `RebuildDeckRows` 末尾自己会刷一次 `Selected Army Title`；这里再写一次是为了
            //   `_deckHolder == null`（没建出来）那一档也不留旧字（同 `PickDeck` 的写法）。
            if (_txtArmy != null) _txtArmy.SetText(SelectedArmyName());
            if (_armyIcon != null) _armyIcon.SetTexture(CardArt.MenuUi(d.FactionIcon));
            SetCardbackTexture(d.Cardback);   // 两层一起换 + 重播入场（账 卡组:138）
            // 🆕 2026-10-22（M2）：督军整身立绘跟着换 —— 预组那一副自己的督军（`d.heroId`）。
            //   🧨 改坏法：漏这一句 ⇒ 从左列换一对之后，立绘停在上一次那副的督军（断言量两次纹理名会红）。
            RefreshCharacterImage();
            // 🆕 **2026-10-19（界面线 P05）**：预组 = 原版的「演示卡组」⇒ 两件照 `ShowDemoDeckInfo` 办：
            //   ① `Lore Text` ← `DemoDeckInfoSO.loreLocalizationKey` 的官方词条；
            //   ② `Change Deck`（原版 `changePlayerDeck`）**关掉** —— 演示卡组不能换。
            //   没有 lore 的 25 副（Demo DeckInfo 之后新增）⇒ 空 + 出声，⛔ 不编。
            //   🆕 **2026-10-22**：从左列选的那一路传 `loreKeyOverride` —— 原版**一个 SO 一条 lore、两副共用**，
            //   所以取「对上那条」比取「这副牌自己那条」更贴原版（两者今天同值，但来源只有一处是对的）；
            //   从选卡组窗的「预组」页选的那一路没有「对」，仍读 `Deck.loreKey`。
            if (_txtLore != null)
            {
                string lore;
                if (loreKeyOverride != null)
                {
                    lore = string.IsNullOrEmpty(loreKeyOverride) ? null : Loc.T(loreKeyOverride);
                }
                else lore = d.LoreText;
                _txtLore.SetText(lore ?? "");
                if (lore == null)
                    Debug.LogWarning("[Practice] 预组「" + d.DisplayName + "」(" + d.deckId + ") **本地没有 lore**"
                                     + "（不在 84 份 `Demo DeckInfo` 里）⇒ `Lore Text` 留空，⛔ 不编");
            }
            ShowChangeDeck(false);
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
        /// <param name="playMode">🆕 **2026-10-15（A383）**：这一局的 `PlayModes` 号 —— **由调用方给**，
        /// 因为两个调用方在**原版里是两档**（本窗自己那条 = `OfflinePractice 6`；
        /// `Deck info ▸ Practice Deck` = `OwnDeckTraining 12`，见 <see cref="PlayMode"/>）。
        /// ⛔ **不给默认值**：默认值会让「新写的第三个调用方」静默挑一档 —— 逼它显式声明。</param>
        public static PracticeModePopup StartPracticeMatch(WindowsManager mgr, int ownDeckIndex,
                                                          DeckSelectionPopup.DeckPick opponent,
                                                          GameMode playMode)
        {
            if (mgr == null) { Debug.LogWarning("[Practice] 没有 `WindowsManager` ⇒ 开不了练习赛"); return null; }
            var w = Create(mgr);
            mgr.OpenWindow(w);                       // `Open()` 会把 `DeckIndex` 重置成 `DeckLibrary.Current` ⇒ 下面再对齐
            w.PlayMode = playMode;                   // 🔴 必须在 `StartBattle()` **之前**（它会把这个号发出去）
            // 🆕 2026-10-17（U4-1）：开关先对齐**这一副自己的模式**，再 `PickDeck`（否则列表会按
            //   `DeckLibrary.Current` 的模式筛，选中的那一副可能不在列表里）。
            w.SelectedGameMode = ToggleModeOf(CollectionData.DeckAt(ownDeckIndex).GameMode);
            w.RefreshGameModeVisual();
            // 🆕 **2026-10-23（W66）**：阵营筛选也按**这一副**对齐 —— 与上面那句「模式对齐」同一条理由：
            //   `Open()` 的默认筛选（原版 `Open.c:172-180`）读的是 **`DeckLibrary.Current` 那副**的阵营，
            //   而这条链要播的是 `ownDeckIndex`。两者阵营不同时，`PickDeck` 之后**选中的那一副不在列表里**
            //   （面板 / `Battle!` 拿的还是它）—— 又是那种「列里没有、却拿它开战」的静默不一致。
            //   ⚠️ **原版这条链不经过本窗**：`DeckInfoPopup__StartPracticeMatch.c` 是 `ShowPopUp(等待窗)` +
            //   直接 `StartMatch(...)`（`decomp_full` 实读），**没有**开 `Practice Mode Menu` ——
            //   我们借本窗当开战宿主（见本文件头「练习对手链」），所以这一步是**我们内部的自洽**、
            //   ⛔ 不是「原版也有这一句」。
            w.ArmyIndex = DefaultArmyIndexOf(CollectionData.DeckAt(ownDeckIndex).Faction,
                                             CollectionWindow.CardsState.Factions());
            if (w.ArmyScroll != null) w.ArmyScroll.OnChanged();   // 阵营列重画 ⇒ 高亮落到这一格
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
            // 🔴 **2026-10-21：督军闸读【唯一判源】`CurrentDeck`**（原来读 `CollectionData.DeckAt(DeckIndex)`）——
            //   预组档 `DeckIndex` 是 −1 ⇒ `DeckAt(-1)` 是一份**空 `DeckInfo`**，`WarlordId` 空
            //   ⇒ 面板上明明是一副有督军的预组，却被「这套卡组还没有督军，开不了局。」挡死
            //   （真机 2026-10-21 的第三处症状）。⛔ 别再在这里读下标。
            //   ⚠️ **发牌那一侧不动**：仍然是 `PrebuiltDecks.PendingSource`（跨场景通道，见下面那一段）——
            //      它与 `PickedPrebuilt` 由 `PickPrebuilt` / `PickDeck` / `ResetContent` **成对**写入与清空。
            var deck = CurrentDeck;
            if (deck == null || string.IsNullOrEmpty(deck.WarlordId))
            {
                Debug.LogWarning("[Practice] 这套卡组**没有督军**，开不了局 —— 如实说，不静默。"
                                 + "（现在这副 = " + (deck == null ? "**空**（连卡组都没有）" : "「" + deck.Name + "」") + "）");
                // 弹窗正文走**整句**词条（键 `MenuDeck/Error/CantStartNoWarlord`）。
                // 🔴 **2026-10-18（波 1b · `A1034` 裁定）**：上一轮这里复用了**短键** `MenuDeck/Error/NoWarlord`
                //    （「还没有选战将」）⇒ 正文从「这套卡组还没有选战将，开不了局。」缩成 5 个字 = 玩家可见
                //    内容缩水 ⇒ **按裁定改回整句**。（短键仍留给本文件那 3 处「未选战将」栏位。）
                // 钮文案同 `DeckInfoPopup`：`MainMenu/General/OK`（⚠️ `OK` 不是 `Ok`）。
                if (Manager != null) Manager.ShowPopUp(Loc.T("MenuDeck/Error/CantStartNoWarlord"), Loc.T("MainMenu/General/OK"), null);
                return;
            }
            // 🆕 **2026-10-26（批次 6 · G15）：原版 `BattleButtonOnClick` 里那一句 `SaveSelectedDeck()`**
            //   （判据 = `cpp2il/.../PracticeModePopup.cs:979-1082` 的方法体：它在 `StartMatch(...)` **之前**调
            //   `SaveSelectedDeck()` ⇒ 点 `Battle!` 也会把「上次选的演示卡组」写进存档）。
            //   ⚠️ 放在督军闸**之后**：闸不过就不开局（原版那一刻也走不到 `SaveSelectedDeck` 后面那几跳）。
            SaveSelectedDeck();
            // 🆕 **2026-10-28（盘 571）：原版 `PracticeEvent.StartMatch → StartMatchNoCheck(CardDeck)` 那三句的本地落地**
            //   （`joinState = Joined` + `save.matchStarted(+0x44) = true` + `randomSeed`；判据见
            //   `GameModesHandler.StartPracticeMatchNoCheck` 的方法头）。原版 `StartMatchNoCheck` 在
            //   `MatchMakerManager.StartMatch` 之前跑 ⇒ 与 `SaveSelectedDeck()` 同刻，**放在这里**。
            //   ⚠️ 这一步是 `PracticeEvent.Initialize` 那道闸（「上一场没收尾 ⇒ 判负 + 弹 OngoingMatch」）唯一的输入源。
            new GameModesHandler().StartPracticeMatchNoCheck(
                PrebuiltDecks.PendingSource != null ? PrebuiltDecks.PendingSource.deckId : deck.Name);
            // 🆕 2026-10-17（U4-1）：`Battle!` **按当前开关的赛制取卡组**（原版 `BattleButtonOnClick.c:96-160`
            //   按 `gameMode` 在 `plVar1[3]/[4]` 两套卡组/督军槽位里二选一）。我们每副牌自带 `GameMode`
            //   ⇒ 列表与选卡组窗都已按模式筛，这里只做**一致性出声**：选中的那副若是另一档
            //   （最可能的来源 = 选卡组窗的「预组」页 —— 原版那一页**不按模式筛**），原版会取同一个条目的
            //   **另一套槽位**，我们没有那套数据 ⇒ **如实警告**，⛔ 不许静默换成别的牌。
            {
                var preSel = PrebuiltDecks.PendingSource;
                // 🔴 **2026-10-21**：兜底那一支从 `info.GameMode`（按库下标取的）换成 `deck.GameMode`
                //   —— 同一副牌的两处取法逐位相同（`PlayerDeck.GameMode` 就是 `DeckAt` 那一列），
                //   但预组档只有 `deck` 这条能取到（`DeckIndex` = −1）。
                int selMode = preSel != null ? preSel.gameMode : deck.GameMode;
                if (ToggleModeOf(selMode) != SelectedGameMode)
                    Debug.LogWarning("[Practice] ⚠️ 选中的卡组是 "
                                     + (ToggleModeOf(selMode) == GameMode.Classic ? "Classic(0)" : "Skirmish(13)")
                                     + " 模式，而 `Game mode` 开关在 "
                                     + (SelectedGameMode == GameMode.Classic ? "Classic(0)" : "Skirmish(13)")
                                     + " ⇒ 原版会取该条目的另一套槽位，我们没有那套数据（如实出声，不换牌）");
            }
            // 🆕 2026-09-26（N3）：**联机已连上 ⇒ 走 P2P，不跑那 12 秒 bot 链**
            //    （判据 → `资料/联机P2P_设计与交接.md` §六 N3）。没接管时照旧 —— 单机行为一字不改。
            {
                var pre0 = PrebuiltDecks.PendingSource;
                // 🔴 **2026-10-21**：`pd` / 阵营的兜底支改成 `deck`（= `CurrentDeck`）——
                //   预组档 `CollectionData.Raw(-1)` 是 null，原来那一支会把「一副空牌」交给联机层。
                var pd = pre0 != null ? PrebuiltDecks.ToPlayerDeck(pre0) : deck;
                // 🔴 **2026-10-15（A383）**：过网的模式从写死的 `"Classic"` 换成**本窗真正的模式号**
                //    （`PlayModeNames.Name(PlayMode)` —— 练习窗自己 = `"OfflinePractice"`，
                //    `Deck info ▸ Practice Deck` 那条 = `"OwnDeckTraining"`）。两端各自声明、
                //    主机开局时比对（`NetMatchmaking.HostStartMatch` 那道闸）。
                if (NetMatchmaking.TryStart(pd, PlayModeNames.Name(PlayMode),
                                            pre0 != null ? pre0.faction : FactionOf(deck.WarlordId), out string netWhy))
                {
                    Debug.Log($"[Practice] 这一局走**联机**（本机交了卡组「{pd.Name}」）—— 不跑 12 秒 bot 链");
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
            // 🔴 **2026-10-25（E4 · 联机账 79）：带 `enemyDeck` 的练习赛**不走那 12 秒搜索。
            //   判据 = 原版 `MatchMakerManager.StartMatch(playMode, playerBattleData, playerDeck, **enemyDeck**, …,
            //   waitLoadTime = 1)`（`Everguild/MatchMakerManager.cs:364-414` 逐句）：它**根本不搜索** ——
            //   `MoveToBattleRoom(...)` 之后直接 `StartBattleWithDelay(1f)` ⇒ 约 **1 秒**开打。
            //   而「等不到真人就换 bot」那 12 秒（`GetTimeToWaitForOpponent` = 12）属于 `FindMatch` 那条链。
            //   ⇒ 本窗里 `OpponentDeck != null` 只有一条来路 = `StartPracticeMatch`（`Deck info ▸ Practice Deck`
            //     那条把对手那副牌挑好了，原版就是 `enemyDeck`）⇒ 那一条改成 **1 秒**倒计时（不再是 12）。
            //   ⚠️ 走同一个 `SearchingMatchPopup`（倒计时/取消那套原样复用），只是**秒数**不同 ——
            //     所以画面上仍然是「Searching…」那一小段，与原版那 1 秒的等待窗同形。
            if (OpponentDeck != null)
            {
                Debug.Log("[Practice] 「本局对手」已指定（原版 `StartMatch(…, enemyDeck)`）⇒ **不跑 12 秒搜索**，"
                          + "等 1 秒就开打（原版 `StartBattleWithDelay(waitLoadTime = 1)`）");
                _search.BeginSearch(seconds: 1f);
                return;
            }
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
                // 整句走词条（键 `Settings/Online/MatchCancelled`）。
                NetRuntime.Notice(Loc.T("Settings/Online/MatchCancelled"));
            }
            else
            {
                // 走词条（键 `Settings/Online/MatchCancelFailed`）。
                NetRuntime.Notice(Loc.T("Settings/Online/MatchCancelFailed").Replace("{0}", why));   // **不假装取消成功**
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
                // `BattleDriver.PickSavedDeck` 读的就是 `DeckLibrary.Current`
                // 🔴 **2026-10-13（A600）**：这一步是**全链里最要命的一处** —— 紧接着（非批处理时）
                //   就 `LoadScene` 切走，而 `PickSavedDeck` 是**从磁盘重读**的 ⇒ `Select` 内部那次落盘失败
                //   会让**这一局打的是磁盘上那套旧牌**（原来完全静默）。
                //   ⚠️ 本件只把失败**说出来**；⛔「失败时该不该照样开战 / 该不该弹提示」= 改玩家可见流程，
                //   另一笔账（报告 §五·2），别顺手在这里加 `return`。
                if (!CollectionData.Select(DeckIndex))
                    Debug.LogWarning("[Practice] 选中第 " + DeckIndex + " 套失败（⚠️ 这一局会拿磁盘上那套旧的）："
                                     + CollectionData.LastSelectError);
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
            // 🔴 **2026-10-15（A383）**：**本局真正的模式号放进跨场景通道**（原版
            //   `MatchMakerManager.StartMatch(playMode, …)` 的第 1 个实参就是把模式带进对局的）。
            //   时机 = **开战这一步**（不是「点 Battle! 那一刻」—— 那之后还有 12 秒搜索，
            //   玩家取消再换一扇窗就会串）。读完就清（`BattleDriver.TakePendingPlayMode`）。
            BattleDriver.SetPendingPlayMode(PlayMode);
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
            // 🔴 **2026-10-17（界面线 T16）**：壳侧 `LoadingMenu` 黑场 → 再 `LoadScene`
            //   （原版 `GeneralMenuController.ShowLoading` 那条链；`U3` 把「进战斗是硬切」列为本仓缺口）。
            //   批处理已经在上面 return，**自检行为零变化**。
            ShellRuntime.LoadSceneWithFade(scene);
        }

        void NotBuilt(string what)
        {
            Debug.LogWarning("[Practice] `" + what + "` 还没实现（**出声**，见 `Shell/PracticeModePopup.cs` 文件头「没建的」）");
        }

        // ============================================================ 画图小工具（同 `DeckInfoPopup` 那一套）
        // ⚠️ **坐标一律页面绝对 px；`parent` 与 `basis` 给同一个节点**（第三种错法见 `已知的坑.md`）。

        enum Align { Center, Left, Right }

        /// <summary>🔴 **2026-10-11（A306④）**：`basis` 的**位置**先换算进**设计空间**再减
        /// （`MenuDraw.PosInDesignSpace`）—— 改前写的是 `− basis.position`，**少除了一次 `basis` 上面
        /// 那一级的 `lossyScale`**（与 A294 / A297 修掉的 `MenuDraw.Local` / `MainMenuSubmenuWindow.Local`
        /// 是**同一个病**，本处 = 那份算式的同形副本，同族第三份 `Local3`）。
        /// 小屏缩放开关一开（窗根 ×M），`basis.position` 是**已放大**的世界坐标、
        /// 而 `RectCenter` 给的是**设计坐标** ⇒ 两者不同量纲。
        ///
        /// <para>⚠️ **首参是 `basis`（坐标基准）不是树父 `parent`** ⇒ 这里**只改量纲、不动 `basis` 语义**：
        /// ⛔ 别换成 `MenuDraw.Local(parent, …)` —— 本文件的 `HitOn`（下面那条）**分开收** `parent` 与 `basis`
        /// 两个参数，还有 `HitBasisProbeForTest` 专门造 `basis != parent` 那一态（A229 的守卫用例）
        /// ⇒ 换成单基准会在那一态上变行为。收口成 `MenuDraw.Local(basis, …)` 只在 `basis == parent` 时逐字等价。</para>
        ///
        /// <para>📌 `k == 1`（缩放开关出厂关着）时与改前**逐位相同**。
        /// · `basis == null`：旧写法 NRE，新写法返回零分量（`PosInDesignSpace` 首句）——
        ///   **实读全部调用点都传真 `Transform`**（`probeH` / `cell.transform` / `root` …），不是放宽。</para>
        ///
        /// <para>🔴 **改坏法**：换回裸 `basis.position` ⇒ **今天一条现有断言都不会红**
        /// （`k == 1` 时两式逐位相同 ⇒ 这是**潜伏缺陷**）⇒ 要补的两态断言写在
        /// `资料/普查产出_1011/W4_子3.md` §四，由调度台安排。</para></summary>
        static Vector3 Local3(Transform basis, float x1, float y1, float x2, float y2)
            => LayoutSpace.RectCenter(x1, y1, x2, y2) - MenuDraw.PosInDesignSpace(basis);

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
        /// **只在真正建出 quad 之后**才切软边（`keepAspect` 会把矩形缩过，`vis` 必须是**缩完**那一块）。
        /// 🔴 **2026-10-13（A435 阶段 2 · 戊 / A754）**：两个形参的语义**没变**，但取状态已经走
        /// `ViewportClip.Resolve`（形参非空 = 显式覆盖 > 父链上的视口节点 > 无）—— 判据与本窗两处视口
        /// 迁到节点上的那一段写在函数体里，别在这上面重复第二份。</summary>
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
            // 🔴 **2026-10-09（A233）就地订正（铁律 5）**：这里原来写的是
            //    `if (clip.HasValue && (clipSoftness.x > 0f || clipSoftness.y > 0f))` —— 那道闸把
            //    「渐隐带」和「裁切」当成了同一件事（**有 softness 才裁**），而原版是**先硬裁、再按
            //    `m_Softness` 渐隐**两件事（判据 = uGUI `Culling/Clipping.FindCullAndClipWorldRect`
            //    **四边都求交**；`m_Softness = (0,0)` 只表示**没有渐隐带**，不是「不裁」）。
            //    ⚠️ `clipSoftness = (0,0)` 那一档现在由 `MenuDraw.ApplySoftEdges` 扛：它入口先走
            //    `ClipVisToClip` 硬裁（A225-①），两分量都 0 时只跳过「切开 + 上斜坡」（A277）。
            //
            // 🔴 **2026-10-13（A435 阶段 2 · 戊 / A754）：取裁切状态走【同一份解析】**
            //    （`ViewportClip.Resolve`，与 `MenuDraw.Rect` / `Nine` / `Hit` / `ClipText` 那几路**同一个口**）。
            //    优先级 = **显式形参（非空 ⇒ 旧路赢、连父链都不走）> 父链上最近的 `ViewportClip` > 无**。
            //    🔴 **为什么本函数必须自己补这一句**：它是**本文件唯一一个自己吃 `clip` 的建图口**
            //    （别的都转调 `MenuDraw.*`，那几路内部已经解析过；`Txt` 走的 `MenuDraw.ClipText` 也是）。
            //    ⇒ 不补的话，两处视口迁完之后逐件传的是 `null` ⇒ **`ApplySoftEdges` 一次都不调**
            //    = **既无硬裁、也无渐隐带**（画面上「看着还在」，静默 —— 这正是迁移要消灭的那种病）。
            //    ✅ **旧路逐位不变**：形参非空时 `RenderClip` = `PaddedClip(clip, Vector4.zero)` **首句早退**
            //    ⇒ 与原来那个 `clip.Value` 逐位相同；`Softness` = `clipSoftness` 原样带出。
            //    ✅ **没有节点、也没形参**（本窗别处那些不裁的 `ImgTex` 调用点）⇒ `RenderClip` 是 `null`
            //    ⇒ 那道闸照旧为假、一个字都不动（与迁移前逐位相同）。
            //    ⚠️ 闸上留的仍是**非空判据**（不是「softness 非 0」）：`ApplySoftEdges` 要**非空** `clip`
            //    —— A233 那条「`clipSoftness = 0` 也照样硬裁」是它**内部**的事（入口 `ClipVisToClip` 无条件跑）。
            var _st = ViewportClip.Resolve(parent, clip, clipSoftness, default(Vector4));
            var _clip = _st.RenderClip;          // 渲染那一份（= V − pad），与 `MenuDraw.Rect` 同一份口径
            var _soft = _st.Softness;
            if (_clip.HasValue)
                MenuDraw.ApplySoftEdges(quad, new PxRect(x1, y1, x2, y2), _clip.Value, _soft,
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
            //       （`LayoutSpace.RectCenter(…) − 基准的**设计空间**位置`，逐字相同 ——
            //        🆕 2026-10-11（A306④）起两边都走 `MenuDraw.PosInDesignSpace`）⇒ 收口只在 `basis == parent`
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
        /// 🔴 **必须在最后调**（`SetText` / `SetGlyphHeight` 任何一次重排都会把 mesh 重算回去）⇒ 放在本函数的**末句**。
        /// <para>🆕 **2026-10-19（A1205）：补上 `wrapPx` / `autoMinPx` / `autoMaxPx` / `autoBasePx` 四个形参**
        /// —— 本方法此前**一个 autosize 形参都没有** ⇒ 本文件里**8 处「该接」不是「忘了传」是「没地方传」**
        /// （出处 = `资料/普查产出_第十会话/R5_包装层菜单族.md` §2·H #46–#52、#54）。
        /// 四个新形参**全默认 `0`** ⇒ **既有调用点一字不改、行为逐位不变**
        /// （含 `:1173` 那处把 `null, DeckClipSoft` **按位置**传给 `clip`/`clipSoftness` 的写法 ——
        /// 新形参排在它们**之后**，位置语义不变）。</para>
        /// <para>🔴 **契约取哪一档 —— 照 `A1195`「从原版那颗 TMP 真有的组合倒推」**：本文件这 8 处里
        /// **7 处是 `m_TextWrappingMode = 1`**、**1 处是 `折行 = 0` 却开着自适应**
        /// （`Change Deck Text` = `auto[10~45] · base 12 · 折行 0`）⇒ **必须取
        /// `Shell/SettingsWindow.cs` 的 `Text`（`A1181`）那一档：两个开关拆开**
        /// （`wrapPx` = 折行开关 · `autoMinPx` = 自适应开关）——
        /// `Shell/MenuWindowBase.cs` 的 `Text`（`A1173`）那条闸把两个开关绑在一起、**表达不了那个组合**。
        /// ⛔ 不是「把两个漏斗改成一个样」（`A1195` 禁止的那件事）；契约原文 → `Shell/SettingsWindow.cs` 的方法头。</para>
        /// <para>🔴 **量纲**：四个新形参与本方法的 `fontPx` 同一档（= 原版 TMP 的**设计 px**）；
        /// `SetAutoFitBox` 的后三个实参按它的量纲注**不过** `LayoutSpace.Px()`（同 `MenuWindowBase.Text:337`）。
        /// ⚠️ 本方法**没有** `MenuDraw.TextCore` 那条 `fontPx &gt; autoMinPx` 的闸（走 `Label.SetAutoFitBox`
        /// 的这条路本来就没有）—— 本文件 8 处的 `autoMinPx` 全部 &lt; 各自 `fontPx`，今天加不加闸同效。
        /// ⚠️ **次序**：自适应/折行两步排在 `MenuDraw.ClipText` **之前**（裁切必须是最后一步 —— 见上面那条）。</para></summary>
        /// <param name="vAlign">🆕 **2026-10-17（`N11`）**：原版那一颗 TMP 的**纵向档**
        /// （`m_VerticalAlignment`）。⛔ 缺省（`null` = `Middle`）= **旧行为逐位不变** —— 本文件
        /// 11 个调用点里只有 3 处不是 `Middle`，其余一处都不用动。
        /// <para>逐处判据 = **本轮现读** `python -I 工具/menu_dump.py bundle_menus_assets_all
        /// "Practice Mode Menu" --depth 8 --md`（每个节点的「文字」那一格末尾 `对齐=H/V`）：
        /// · `Deck Selector ▸ Deck Buttons ▸ Selected Army Title` = `Center/**Midline**`
        /// · `Deck info ▸ General container ▸ Generic Simplified UI Button ▸ Button Text`（Change Deck）
        ///   = `Center/**Capline**`
        /// · `Toggle ▸ Label`（Game mode）= `Center/**Capline**`
        /// · 其余（`Deck Name` / `Warlord Name` / `Cost/balance text` / `Back button ▸ Text` /
        ///   `tooltip` / `Continue Button ▸ Text` / `Deck Selector Menu Item ▸ Deck Name`）**全是 `Middle`**。
        /// ⚠️ 卡列表那一颗 `Card Name` 也是 `Left/Capline`，但它要在 `SetAutoFitBox` / `SetWrapping` /
        ///   `RelayoutNow` **之后**才摆（调用方自己的那句 `MenuDraw.SetVAlign`，见 `BuildCardRows`）⇒ ⛔ 别搬进来。</para>
        Label Txt(Transform parent, string text, float x1, float x2, float y1, float y2, float fontPx,
                  Align align, string name, int q,
                  PxRect? clip = null, Vector2 clipSoftness = default(Vector2),
                  float wrapPx = 0f, float autoMinPx = 0f, float autoMaxPx = 0f, float autoBasePx = 0f,
                  Label.VAlign? vAlign = null, float boxW = 0f, float boxH = 0f,
                  TmpFont.Face face = TmpFont.Face.Cjk)
        {
            var lb = Label.Create(parent, text ?? "", Local3(parent, x1, y1, x2, y2), 5, Color.white,
                                  new Vector2(0.5f, 0.5f), name);
            if (lb == null) return null;
            lb.SetRenderQueue(q);
            // 🔴 **2026-10-17（A1024③）**：可选字体面 —— 默认 `Cjk` ⇒ 旧调用点零行为变化。
            //    ⚠️ **次序**：`SetFace` 必须在 `SetGlyphHeight` 之前（字号按 `ActualFace` 的 em 换算）。
            if (face != TmpFont.Face.Cjk) lb.SetFace(face);
            if (fontPx > 0f) lb.SetGlyphHeight(LayoutSpace.Px(fontPx));
            // 🆕 **A1205**：自适应 + 折行两个开关（逐字照 `Shell/SettingsWindow.cs:4741-4748` 那一档的算法，
            //    落在裸 `Label` 上）。🔴 **必须排在下面两句对齐之前** —— `SetWrapWidth` / `SetAutoFitBox`
            //    会改写 `sizeDelta.x`，而对齐读的就是那一份（次序判据 → `Battle/Label.cs` 的 `SetWrappingMode` 头）。
            //    两个开关全缺省 ⇒ `fitW = 0` ⇒ **一个分支都不进 = 旧行为逐位不变**。
            // 🆕 **2026-10-17（A1226 · Change Deck Text 收口）**：`boxW/boxH` = 原版那颗 TMP **自己的有效框**
            //   （画布 px）；缺省 0 ⇒ 退回 `x2−x1` / `y2−y1`（旧行为逐位不变）。判据与 DeckInfoPopup 同形。
            float fitW = wrapPx > 0f ? wrapPx : (autoMinPx > 0f ? (boxW > 0f ? boxW : x2 - x1) : 0f);
            float fitH = boxH > 0f ? boxH : (y2 - y1);
            if (fitW > 0f)
            {
                lb.SetWrapWidth(LayoutSpace.Px(fitW));
                if (autoMinPx > 0f)
                    // 上限兜底 `autoMaxPx <= 0 ⇒ fontPx` 与 `MenuDraw.TextCore` 同义（A333）
                    lb.SetAutoFitBox(LayoutSpace.Px(fitW), LayoutSpace.Px(fitH), autoMinPx,
                                     autoMaxPx > 0f ? autoMaxPx : fontPx, autoBasePx);
            }
            // 🔴 原版 `折行 = 0` 那一档（本文件 8 处里 1 处：`Change Deck Text`）要还原：
            //    上面 `SetWrapWidth` **无条件**把模式开成 `Normal`。也必须在 `ClipText` **之前**（它下面的末句）
            //    —— `SetWrapping` 自己会 `ForceRelayout`。
            if (autoMinPx > 0f && wrapPx <= 0f) lb.SetWrapping(false);
            if (align == Align.Right) lb.AlignRightOn(LayoutSpace.FromPixel(x2, 0f).x);
            else if (align == Align.Left) lb.AlignLeftOn(LayoutSpace.FromPixel(x1, 0f).x);
            // 🆕 **2026-10-17（`N11`）**：纵向档（判据 / 逐处档位 → 上面 `vAlign` 的 `<param>`）。
            //    🔴 **必须排在 `ClipText` 之前** —— `Label.SetVAlign` 末句是 `RefreshBounds()`（一次重排），
            //    重排会把 mesh 重刷一遍；排在裁切之后 = 那一刀白裁（本文件 `ClipText` 是最后一步的那条纪律）。
            //    ⚠️ 缺省 `null` ⇒ **一句都不执行**（旧行为逐位不变）；`Middle` 传进来也跳过（出厂档就是它）。
            if (vAlign.HasValue && vAlign.Value != Label.VAlign.Middle)
                MenuDraw.SetVAlign(lb, vAlign.Value, new PxRect(x1, y1, x2, y2));
            // 🔴 **2026-10-09（A233）就地订正（铁律 5）**：原来这道闸写的是
            //    `clip.HasValue && (clipSoftness.x > 0f || clipSoftness.y > 0f)` ⇒ **`clipSoftness = (0,0)`
            //    时这段字既不硬裁也不建软边**（整段画到视口外）。判据 = 原版 `RectMask2D`：
            //    **先硬裁、再按 `m_Softness` 渐隐**（`Culling/Clipping.FindCullAndClipWorldRect` 四边都求交；softness 只管
            //    渐隐带落在哪）⇒ **文字也必须硬裁**，且与 softness 是不是 0 无关。
            //    修法 = 判据本身：`MenuDraw.ClipText(…, Vector2.zero)` **就是纯硬裁**
            //    （`ClipQuad` 把框外的顶点夹到框沿 + 按同一仿射关系改 uv；`SoftAlpha` 在 soft ≤ 0 时恒 1
            //      ⇒ 一个 alpha 都不削）⇒ 这里把 `clipSoftness` **原样**传下去即可，闸只留 `clip.HasValue`。
            //    ⚠️ 今天两处调用点（`ArmyClipSoft (0,50)` / `DeckClipSoft (0,23)`）都非 0 ⇒ **零行为变化**。
            // 🔴 **2026-10-11（A233 收尾 · 调度台裁定「不开口」）**：**外面那道 `if (clip.HasValue)` 已删**
            //    —— 前置条件归**被调方**：`MenuDraw.ClipText` 自己的首句就是 `if (lb == null || !clip.HasValue) return false;`
            //    （`Shell/MenuDraw.cs` 的 `ClipText`），所以这层闸**是同义反复、「行为上不可观测」**。
            //    ⛔ 不为此给生产类开测试注入口（与 A185 同一条纪律）；本条**行为零变化**，
            //    牙口 = `Editor/ShellScene.cs` 既有的 ⑤·d-2 / ⑤·d-3（那两条直调 `MenuDraw.ClipText` /
            //    `ApplySoftEdges`，钉的是「soft=0 也要硬裁」那一半机制）。
            //    ⚠️ **同文件 `ImgTex` 那道 `if (clip.HasValue)` 不能跟着删** —— `ApplySoftEdges` 要**非空** clip。
            MenuDraw.ClipText(lb, clip, clipSoftness);
            return lb;
        }

        Transform Hit(Transform parent, Transform basis, string name, PxRect r, System.Action onClick, int q = QPrHit)
            => HitOn(parent, basis, name, r, onClick, q);

        /// <summary>🆕 **2026-10-09（A229）自检口**：拿**任意 `parent`/`basis` 组合**真调一次 `HitOn`
        /// —— 自检用它造出 `basis != parent` 那一态（`HitOn` 是私有的实例方法，自检没有别的路能进去）。
        /// ⚠️ 只给自检用：建出来的命中区挂在传进来的 `parent` 下，**调用方自己销毁**。
        /// 空 `onClick` 不会被派发（节点建完就销毁）—— 守卫在那之前就已经跑过了。</summary>
        public Transform HitBasisProbeForTest(Transform parent, Transform basis)
            => HitOn(parent, basis, "HitBasisProbe", new PxRect(0f, 0f, 120f, 30f), () => { });

        /// <summary>🆕 **2026-10-07（A12①）**：多了 `clip` —— **命中区也要截到视口内**。
        /// 判据 = 原版 `RectMask2D` 的**射线那一面**（`IsRaycastLocationValid`：框外的点判不中任何东西；
        /// UGUI 源码 `RectMask2D` 里那一处）—— 与 `MenuDraw.Hit` / `MenuDraw.ClipRect` 同一份判据。
        /// 🔴 **为什么这两个格子必须一起改**：本窗基类 `GameWindow` **没有** `Clip`（那是 `MenuWindowBase` 的），
        ///   ⚠️ **2026-10-11 就地订正（铁律 5）**：这一句**已过期** —— `Clip` / `ClipSoftness` / `ClipPad` 三份状态
        ///   2026-10-07（A78②）起**上移到 `GameWindow`**（`Shell/WindowsManager.cs` 的「裁切状态」那一节：
        ///   「只声明一次，全 `GameWindow` 族都继承得到」），本窗**现在也有**。上面那两句历史叙述**照旧成立**
        ///   （当年收口时确实没有）⇒ 只订正这一句，别按它去推「本窗拿不到 `Clip`」。
        ///   收口前靠 `Inside`（**只建完整在视口里的格**）间接保证「命中区不会伸出视口」；
        ///   改成「相交就建」之后半行是**建出来**的 ⇒ 不截的话**点在视口外那条带子上照样命中**
        ///   （那条带子上还压着别的件：`Army Selector` 上沿 / 两列之间的缝）—— 那是收口顺带引入的偏离。
        /// ⚠️ `clip = null`（本文件其余那些调用点，以及**迁移后的两个格子**）⇒ `ClipRect` 那一支
        /// 取决于**父链上有没有 `ViewportClip`** —— 见下面 🔴 那一段（⛔ 别再按这一句推「`null` = 不裁」）。
        /// ⚠️ 整块在视口外 ⇒ 返回 **null**（节点一起不建）—— 两个调用点都已经判了 null。
        ///
        /// 🔴 **2026-10-13（A435 阶段 2 · 戊 / A754）**：那句求交从**纯矩形**的 `MenuDraw.ClipRect`
        /// 换成了**节点态**的 **`MenuDraw.ClipRectAbove(parent, …)`**。
        /// 🔴 **为什么必须换**：`ClipRect` / `Visible` 是**纯矩形函数**、手上没有 `Transform`
        /// ⇒ 它们**解析不了节点**；两个格子现在逐件传的是 `null` ⇒ 沿用旧口的话 `ClipRect` 第一句就
        /// `outRect = r; return true`（**原样放行**）⇒ 滚出视口一半的那一格，**点在视口外那条带子上照样命中**
        /// （那条带子上还压着别的件）—— 而且是**静默**的（画面对，交互错）。
        /// ✅ **旧路逐位不变**：`clip` 非空时 `Resolve` 第 1 支把形参原样带出
        /// （`RenderClip` = `PaddedClip(clip, zero)` 首句早退）⇒ 与 `ClipRect(r, clip, …)` 逐位相同；
        /// 没有节点也没有形参时两者也逐位相同（`ClipRectAbove` 的头里写着这一条）。
        /// ⚠️ 交出去的仍是**调用方原样那一份** `clip`（⛔ 别把解析后的框传进来 —— 那样会走成「显式覆盖」）。</summary>
        Transform HitOn(Transform parent, Transform basis, string name, PxRect r, System.Action onClick,
                        int q = QPrHit, PxRect? clip = null, string clickCue = null)
        {
            // 🔴 **2026-10-09（A229）**：与同文件 `Nine`（上面那条）**同一条守卫、同一句话**。
            //   ⚠️ **本文件没有带 `basis` 的 `Txt`**（那个 `Txt` 只有 `parent` 一个基准、`Local3(parent, …)`，
            //      结构上不可能不等）—— A229 在本文件的真身就是这两个带 `basis` 的 helper 里的**没守卫的那个**
            //      （`Nine` 早已有；`Hit` 只是转调 `HitOn` ⇒ 守卫只写在 `HitOn` 一处）。
            //   本文件自己的约定（`本文件`）：「**坐标一律页面绝对 px；`parent` 与 `basis` 给同一个节点**
            //   （第三种错法见 `已知的坑.md`）」⇒ 不等就是那个错法。
            //   今天 9 个调用点**全部**传同一个对象（`HitOn(root, root, …)` / `(cell.transform, cell.transform, …)`
            //   / `(_general, _general, …)` / `(_cardHolder, _cardHolder, …)`）⇒ **零行为变化**；⛔ 不许静默。
            //   改坏法：删掉这一句 ⇒ `Editor/CollectionScene.cs` 的「A229 `HitOn` 守卫」那一组**红**。
            if (!ReferenceEquals(basis, parent))
                Debug.LogWarning("[Practice] `HitOn` 的 `basis` 必须等于 `parent`"
                                 + "（节点按 `Local3(basis, …)` 落位、而树父是 `parent`；"
                                 + "两个不同就是两套坐标系、整颗命中区会偏 `parent.position − basis.position`）");
            PxRect hr;
            // 🔴 **2026-10-13（A435 阶段 2 · 戊 / A754）**：走**节点态**那一版（理由见本函数的头）——
            //   解析起点 = `parent`（= 这一格挂在哪：`Filters` / `Content` ⇒ 一次父链就命中那颗 `Viewport`）。
            if (!MenuDraw.ClipRectAbove(parent, r, clip, out hr)) return null;
            // 🔴 **2026-10-07（A92）**：这个 `Hit` 节点也从裸 `Transform` 改成 `RectTransform` ——
            //    它和 `MenuDraw.Hit` / `MenuWindowBase.AddHit` 建的是**同一种东西**（透明 quad + `WindowButton`），
            //    而那两条本次一起改了 ⇒ 同一件东西不许一半一种类型（判据 = 原版 16510/16768 是 `RectTransform`）。
            //    ⚠️ 本节点**要摆位置**（下一行）—— 位置走 `Local3`（世界坐标差），与节点类型无关，不受影响。
            var hit = new GameObject(name, typeof(RectTransform));
            hit.transform.SetParent(parent, false);
            // 🔴 **2026-10-11（A218）**：命中区节点也写 `sizeDelta`（= 命中区矩形 `hr` 那块的大小），
            //    与 `MenuDraw.Hit` / `Node` **同一份换算**（`MenuDraw.SetPxSize`）。
            //    ⚠️ 本节点**要摆位置**（下一行 `Local3`）—— `SetPxSize` 自带「写完不动位置」的守卫，
            //    所以这里先尺寸、后位置，与 `MenuDraw.Hit`（节点恒在父原点）走的是同一个函数。
            //    改坏法：删掉 `SetPxSize` ⇒ `Editor/MainMenuScene.cs` §A218「`HitOn` 的 rect = 命中区矩形」红。
            MenuDraw.SetPxSize(hit.transform, hr.W, hr.H);
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
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 B11）**：逐个 cue（原版 `m_onClickSound`）——
            //   缺省 `null` = 原版默认按钮音（既有调用点逐位不变）。
            wb.clickCue = clickCue;
            return hit.transform;
        }

        // ============================================================ 🆕 2026-10-26（批次 6 · G15）
        //  本窗三条**原版方法**的等价物（判据全部实读 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/PracticeModePopup.cs`，
        //  那是唯一有真方法体的那份）。为什么新成员都追加在类尾：铁律「增行会推下游行号」——
        //  本文件在账里只有**类级**引用（`卡组编辑.tsv` 一族写的是 `Shell/PracticeModePopup.cs`，不带 `:行`）
        //  ⇒ 追加是安全的；仍然照规矩**只往类尾加**，⛔ 别插进已有成员之间。

        /// <summary>原版 `Awake` **末句**的闸（`:95-100`）：
        /// `gameModeSelector.SetActive(LiveOpsManager.GetHandler&lt;GameModes&gt;().GetActiveEvent&lt;RankedV3Event&gt;() != null)`。
        ///
        /// <para>🔴 **本地等价源 = `Shell/RankedData.cs` 的那一季**（不是「本地没有」——那是 G7 特意提醒过的一句）。
        /// 我们的对应物有三件：① `Shell/RankedData.cs`（本地排季：赛季号 / 积分 / 段位，带本地存档）；
        /// ② `Shell/RankedEventWindow.cs`（`RankedEventWindowV2` 的本地实现，**恒可进**）；
        /// ③ `Shell/MainMenuRuntime.cs` 的「排位」模式卡（**恒在**）。⇒ 本地**恒有一档在期的排位季**
        /// ⇒ 开关**常显**，与原版「有 RankedV3 活动 ⇒ 显示」**等价**（F6 / G7 都写过：别按「本地没有」结案）。</para>
        ///
        /// <para>⚠️ **为什么读 `SeasonNumber` 而不是恒 `true`**：`SeasonNumber` 是那份数据层的真实字段
        /// （缺省 1、存档里读回，`RankedData.Load`）—— 万一将来本地排季被关掉/清空（赛季号 ≤ 0），
        /// 这颗开关会自动跟着藏起来，与「没有在期活动」同向。⛔ 别把它写成 `return true`（那就是写死）。
        /// ⚠️ **时间限定**：当前 build 里 `LiveOpsLocalCatalog` 的 13 份本地活动 SO **没有任何 Ranked 类**
        /// （枚举只有 Parent / Scoring / PremiumItemOffer / FastMode），所以**不能**拿它当判据源；
        /// 若将来本地出现 Ranked 类活动实例，应改指它（那才是原版那个 `GetActiveEvent` 的真等价物）。</para></summary>
        public static bool HasActiveRankedEvent { get { return RankedData.SeasonNumber > 0; } }

        /// <summary>原版 `PlayerDeckChanged(CardDeck newDeck)`（`:722-731`）的等价物。
        ///
        /// <para>原版三句（逐句）：`playerDeckInfoWrapper.cardDeck = newDeck;` · `selectedArmy = CardArmy.Neutral;` ·
        /// `ChangeSelectedArmy(newDeck.deckHero.cardArmy);` —— 第二句是**故意的**：`ChangeSelectedArmy` 开头有
        /// `if (army == selectedArmy) return;`（`:750-753`），先把筛选取成 `Neutral` 才保证「哪怕新旧卡组同阵营也走完整一圈」
        /// （重取 label + 重刷高亮 / 火漆 + 末尾 `SetArmyButtons` 重列）。</para>
        ///
        /// <para>⚠️ **我们的「selectedArmy」就是 <see cref="ArmyIndex"/>**（阵营名在 `Factions()` 里的下标），
        /// 没有 `Neutral` 那一档（那是 `CardArmy` 的 0 号；我们这一维是**下标**、原版那一维是**枚举值**）。
        /// ⇒ 等价做法：直接按新卡组的阵营算下标再走一次「重画 + 重列」——
        /// 少掉的只是「同阵营时也要走一圈」这个副作用，而本方法**本来就会被调用**
        /// （调用点 = `OpenDeckSelection` 的选卡组回调，每次玩家点定都调一次），所以那一圈不会漏。</para>
        ///
        /// <para>🔴 **为什么不复用 <see cref="PickArmy"/>**：那一支有「同阵营早返回」的闸（原版
        /// `ChangeSelectedArmy.c:30-33`），而本方法要的正是**绕过**它（原版先用 `Neutral` 顶开的那个闸）。</para>
        ///
        /// <para>⚠️ **一条与原版一致的怪行为（如实记，⛔ 别「修」）**：原版 `SetArmyButtons` 建完列表后
        /// **默认选 item0**（= 你的卡组；阵营不匹配时 = 该阵营第一对）⇒ 玩家若从选卡组窗的「预组」页挑了一副
        /// **不是该阵营第一对**的牌，这一圈会把它换回**第一对**。原版就是这样（`DeckSelected(item0)`），
        /// 我们照做。</para></summary>
        /// <param name="faction">新选中那副牌的阵营名（`CollectionData.DeckAt(i).Faction` / `PrebuiltDecks.Deck.faction`）。
        /// 空 ⇒ 落 `Ultramarines`（同 <see cref="DefaultArmyIndexOf"/> 的回落）。</param>
        public void PlayerDeckChanged(string faction)
        {
            var facs = CollectionWindow.CardsState.Factions();
            int ni = DefaultArmyIndexOf(faction, facs);
            Debug.Log("[Practice] 换了自己的卡组 ⇒ 阵营筛选跟到新卡组（原版 `PlayerDeckChanged`："
                    + "`selectedArmy = Neutral` 再 `ChangeSelectedArmy(新卡组阵营)`）：「"
                    + (string.IsNullOrEmpty(faction) ? "<空>" : faction) + "」→ 第 " + ni + " 格"
                    + (ni == ArmyIndex ? "（与当前筛选同一格，仍按原版走完一圈）" : ""));
            ArmyIndex = ni;
            // 🔴 **2026-10-26（`B8_BR` · 铁律 5 就地订正 —— 本句原来会换掉玩家刚选的那一副，真缺陷）**
            //   原来这里是 `if (!SelectDefaultDeck()) ShowNoDeckSelected(…)`。
            //   判据（🥇 `cpp2il/out_cpp2il_dev_ilspy/PracticeModePopup.cs:854-960` 的 `SetArmyButtons`）：
            //   原版 item0 是用**刚写进 `wrapper.cardDeck(+0x58)` 的那一副**建出来的，随后 `DeckSelected(item0)`
            //   是**重选同一副**，**不是**退回卡组库那套。
            //   ⇒ 改成**只重列、不动选中**（`RebuildDeckRows` 只按当前 `_deckHolder` 重排行）。
            //   ⚠️ 遗留（可选、按铁律 11 记着）：原版这一圈会把 item0 **高亮**，我们今天选预组时一格都不亮。
            if (_deckHolder != null) RebuildDeckRows(_deckHolder);
            // 原版那一圈的两颗显隐（`Highlight` / `Has Player Deck`）都由 `ChangeSelectedArmy` 重刷
            // ⇒ 等价物 = 重画阵营列（与 `PickArmy` 末尾那**同一句、同一顺序**，⛔ 别各写一份）。
            // ⚠️ 顺序与 `PickArmy` 保持一致（先选默认牌、后重画）：火漆印读的是 `CurrentDeck`，
            //    `SelectDefaultDeck` 有可能换成「该阵营第一对」⇒ 重画放在它后面，读到的才是终态。
            if (ArmyScroll != null) ArmyScroll.OnChanged();
        }

        /// <summary>原版 `SaveSelectedDeck()`（`:733-743`）的等价物 —— **写存档里的「上次选的演示卡组」**。
        ///
        /// <para>原版三句：取 `selectedDeck` · 按当前 `currentPlayMode` 二选一（`classicDeck` / `skirmishDeck`）·
        /// **那一档非空才** `PlayerDataManager.SetDemoDeck(prebuiltDeck.deckId)`（= 写 `DemoData.currentDeck`）。</para>
        ///
        /// <para>🔴 **它写进去的那个值在原版这一版【只写不读】** —— 两个读者
        /// `IsSavedDeck`(:645) / `LoadSavedDeck`(:610) 都是**零调用死代码**（cpp2il 只有定义、`decomp_full` 只命中它自己；
        /// 判据链 → F6 第 4 轮报告 §二 与 `账/_parts/B4_F6.tsv` 那一行）。
        /// ⇒ 观感上「关窗重开不会恢复上次选的牌」，但**存档那一格真的会被写**（行为可观察）⇒ 仍要做（铁律 11）。</para>
        ///
        /// <para>⚠️ **玩家自己的卡组那一档在原版【不写】**：那时 `selectedDeck` 是
        /// `PlayerDeckDemoDeckInfoWrapper`（`ScriptableObject.CreateInstance` 出来的，`classicDeck/skirmishDeck`
        /// 两个字段**都是 null**）⇒ `if ((bool)obj)` 为假 ⇒ 跳过。我们照此：`PickedPrebuilt == null` 时**不写**。</para>
        ///
        /// <para>落点：`SaveCenter` 的 <see cref="SaveKeys.DemoData"/>（原版 `PlayerDataKey.DemoData` = 第 15 条，
        /// 本工程此前**空缺** —— `SaveKeys.cs` 那一条的注释写着）。⚠️ **只写 `currentDeck` 一个字段**：
        /// 原版 `DemoData` 另有 8 个字段（`tutorialsCompleted` / `gameAfterTutorialPlayed` / `canEnterDraftMode` …），
        /// 它们在本工程各自由别的模块负责/尚未实现 ⇒ ⛔ 别在这里塞默认值冒充（会覆盖将来那些字段）。</para>
        /// <para>存/取都只是 JSON：`SaveCenter.Set`（批处理/没进 Play 且没设 `OverrideRoot` 时它自己会出声并丢弃写）。</para></summary>
        public void SaveSelectedDeck()
        {
            var pre = PickedPrebuilt;
            if (pre == null)
            {
                Debug.Log("[Practice] `SaveSelectedDeck`：现在选的是**玩家自己的卡组** ⇒ 原版这一档**不写**"
                        + "（`selectedDeck` 是 `PlayerDeckDemoDeckInfoWrapper`，它的 `classicDeck/skirmishDeck` 都是 null）");
                return;
            }
            if (string.IsNullOrEmpty(pre.deckId))
            {
                Debug.LogWarning("[Practice] `SaveSelectedDeck`：预组「" + pre.DisplayName
                               + "」**没有 deckId** ⇒ 不写（⛔ 不编一个 id 塞进存档）");
                return;
            }
            bool ok = DemoDataStore.Mutate(d => d.currentDeck = pre.deckId);   // 唯一写口（⛔ 不再自建 DTO 写同一个 key）
            // 失败原因 = `DemoDataStore.LastError`（`SaveCenter` 自己也会出声一次；⛔ 别在这里再判一遍）
            Debug.Log("[Practice] `SaveSelectedDeck` ⇒ `DemoData.currentDeck = " + pre.deckId + "`"
                    + "（原版 `SetDemoDeck(deckId)`；⚠️ 本 build 那两个读者 `IsSavedDeck`/`LoadSavedDeck` 是死代码"
                    + " ⇒ **只写不读**）" + (ok ? " · 已落盘" : " · **没落盘**（原因见紧邻的那条 `[SaveCenter]` 日志）"));
        }

        // ============================================================ 2026-10-29（B94_B7）：`DemoDataDto` 已删除
        // 为什么删：它和 `Shell/DemoData.cs` 的完整 `DemoData` **写同一个 `SaveKeys.DemoData`**，
        // 而它只有 `currentDeck` 一个字段 ⇒ 谁后写谁就把对方那 9 格**清成默认值**（零报错、没有任何提示）。
        // 现在唯一写口 = `DemoDataStore.Mutate(...)`（见上 `SaveSelectedDeck`），完整 DTO = `Shell/DemoData.cs`。
        // 🔴 2026-10-26（批次 6 · G15）原注释的两个约束**仍然有效**：
        //   · 字段名照原版（`currentDeck`，`cpp2il/DemoData.cs:24`）—— 落盘格式，⛔ 别改名；
        //   · 其余 9 格**不许在这里塞默认值冒充**（会覆盖将来那些字段）⇒ 现在由完整 DTO 统一保管。
        // （本段占位保持行数不变：本文件被账按行号引用，删类同时删行会让下游落点全漂。）

        /// <summary>`Game mode` 开关那一整颗节点（原版 `gameModeSelector`）—— `Build()` 末尾那道
        /// 「有 RankedV3 活动才显示」的闸要用它（判据见 <see cref="HasActiveRankedEvent"/>）。
        /// 🔴 隐藏时**开关四件一起藏**（它们都是这颗的子件，同原版 `SetActive` 打在 `gameModeSelector` 上）。</summary>
        Transform _gmToggle;

        // ============================================================ 🆕 2026-10-27（B9_W8PREB · 账 卡组:134 / 卡组:138）
        //  本窗两条原版行为的等价物。判据全部实读，逐条写在各自的方法头里。
        //  ⚠️ **为什么都追加在类尾**：账里本文件的落点最远到 `:2445`（`ShowInfoHit`），往下加不动它们；
        //     上面那 4 处改动（`:832` / `:883-884` / `:2681` / `:2779`）已逐处保持在**原有行数**上（见各条行尾注释）。

        /// <summary>`Cardback` 的**第二层** —— 原版 `DeckGeneralInfoDemo.cardBacks[1]`，节点名 `Cardback Front`
        /// （它是 `Cardback` 的子件；我们平铺在 `General container` 下，靠队列 + 旋转复现同样的层次，见
        /// <see cref="BuildCardbacks"/>）。</summary>
        ImageQuad _cardbackFront;
        /// <summary>卡背入场补间（原版 `DeckGeneralInfoDemo.cardbacksAnimation` 那条 `OpenCardbacks` clip 的等价物）。</summary>
        DG.Tweening.Tween _cardbacksTween;
        /// <summary>两层卡背**当前**喂的那张图（原版 `ConfigPanel` 里那句「`image.m_Sprite == cosmeticSprite` ⇒ 直接 break」
        /// 的判据就是它：同图再来一次**什么都不做**，换图才 `Rewind + Play`）。</summary>
        Texture2D _cardbackTex;
        /// <summary>`Cardback Front` 建出来时的**静止** `localPosition`（入场动画的终位；起点 = 它 + <see cref="CbFrontSlide"/>）。</summary>
        Vector3 _cardbackFrontRestPos;
        /// <summary>入场动画「内层相对外层」的位移量（原版 clip 的 `anchoredPosition` 关键帧：`(0.3,0.3)` → `(11.2012,4.9016)`），
        /// 已按外层 `1.60°` 旋到**世界方向**（px，y 向上）。换算：`R(1.6°)·(−10.9012,−4.6016) = (−10.7684,−4.9042)`。</summary>
        static readonly Vector2 CbFrontSlide = new Vector2(-10.7684f, -4.9042f);
        /// <summary>`OpenCardbacks` clip 的时长（`AnimationClip_7721407185759151047.m_SampleRate = 60`、末键 `t = 0.25`）。</summary>
        public const float CardbacksAnimDuration = 0.25f;
        /// <summary>`Cardback`（外层）的静止 z 旋转 —— 现读 `RectTransform_-3917913493052829892.m_LocalRotation.z = 0.0139622`
        /// ⇒ `2·asin(0.0139622) = 1.60°`。</summary>
        public const float CbRot = 1.60f;
        /// <summary>`Cardback Front` 的静止 z 旋转（**相对外层**）—— 现读 `RectTransform_8791577597720547132.m_LocalRotation.z = −0.0299279`
        /// ⇒ `2·asin(−0.0299279) = −3.43°`。⇒ 平铺之后它的**世界角** = `CbRot + CbFrontLocalRot = −1.83°`。</summary>
        public const float CbFrontLocalRot = -3.43f;

        /// <summary>`Cardback Front` 的静止矩形（现读：外层矩形 + 内层 `anchoredPosition` 差 `(11.2012, +4.9016§)` 换到左上原点 y 向下）。
        /// ⚠️ 差值由 `RectTransform_8791577597720547132.m_AnchoredPosition` 与 `RectTransform_-3917913493052829892` 相减得出，
        /// 与 `menu_dump.py … "Practice Mode Menu" --depth 8` 那一行（`Cardback Front 1553.6 324.7 1763.1 627.1`）逐值吻合。</summary>
        public const float CbFrontL = 1553.55f, CbFrontT = 324.73f, CbFrontR = 1763.11f, CbFrontB = 627.06f;

        /// <summary>≡ 原版 `DeckGeneralInfoDemo.Awake()` 给 `warlordTextButton` 挂的那一颗（账 `卡组:134`）。
        /// <para>判据（🥇 = `decomp_full/DeckGeneralInfoDemo__Awake.c`：四颗钮里第 4 个 `AddListener` 指向
        /// `WarlordInfoButtonClick`；🥈 = `cpp2il/out_cpp2il_dev_ilspy/DeckGeneralInfoDemo.cs:59-61`：
        /// `Button button = warlordTextButton; button.m_OnClick.AddListener(WarlordInfoButtonClick)`）。</para>
        /// <para>🔴 **可点区域 = 可射线件的并集 + 各自的 `m_RaycastPadding`**（本工程口径）。逐字段现读：
        /// 那颗 `Button` 的 GameObject（`MonoBehaviour_-4747125247262437572.m_GameObject`）上只挂一颗吃射线的
        /// Graphic = `warlordName` 那颗 TMP（`MonoBehaviour_3676532614230396732`：`m_RaycastTarget = 1`、
        /// **`m_RaycastPadding = (0,0,0,0)`**，同时就是 `Button.m_TargetGraphic`）⇒ 并集 = **那颗 TMP 自己的矩形**、
        /// 不外扩。它的 RectTransform（`RectTransform_-2158633528951091396`）= `sizeDelta (0, 34)` · `pivot (0, 0.5)`
        /// · **`ContentSizeFitter` 横向 PreferredSize** ⇒ **宽 = 文字实测宽**（`menu_dump` 现读 `0×34`：未撑开那一帧）。
        /// ⇒ ⛔ **不能拿我们那个固定框 `WnL..WnR`（宽 352.86）当命中区** —— 原版只有文字那么宽，
        /// 多出来的空白在原版点不中（本工程踩过「不重叠但过大」那一类）。这里按 `Label.WorldW` 量出来再夹右沿；
        /// **量不到（`WorldW` 是哨兵/天文数字）就退回固定框并出声一次**，⛔ 不静默变成 0 宽（那会「点不着」）。</para></summary>
        void BuildWarlordHit(RuleEngine.CardDef wl)
        {
            if (_info == null) return;
            float w = WnR - WnL;
            if (_txtWarlord != null)
            {
                float measured = _txtWarlord.WorldW * LayoutSpace.PxPerWorldX;   // 世界单位 → 设计 px
                if (measured >= 8f && measured <= w) w = measured;
                else if (!_warlordHitWidthWarned)
                {
                    _warlordHitWidthWarned = true;
                    Debug.Log("[Practice] 督军名实测宽 " + measured.ToString("R")
                            + "px 不在 [8, " + w + "] ⇒ 命中区退回固定框（原版那颗 TMP 是 `ContentSizeFitter`"
                            + " 贴合文字宽；量不到时宁可宽一点，也比 0 宽「点不着」好）");
                }
            }
            // `pad` 传零 = 原版 `m_RaycastPadding = (0,0,0,0)`（上面那段逐字段现读）⇒ `PaddedRect` 原样返回。
            HitOn(_info, _info, "WarlordNameHit",
                  MenuDraw.PaddedRect(new PxRect(WnL, WnT, WnL + w, WnB), Vector4.zero),
                  () => OpenWarlordDetail());
        }
        bool _warlordHitWidthWarned;

        /// <summary>≡ 原版 `DeckGeneralInfoDemo.WarlordInfoButtonClick()`（账 `卡组:148` 的调用面）：
        /// `CardDisplayWindow.Instance.ShowCard(deckHero, tier: null, showOptions: false, …)`。
        /// <para>判据（🥇 `decomp_full/DeckGeneralInfoDemo__WarlordInfoButtonClick.c`：取 `currentDeck` 的
        /// `classicDeck`/`skirmishDeck`（按 `currentSelectedPlayMode`）→ `PrebuiltDeck.get_DeckHero` →
        /// `CardDisplayWindow.ShowCard(hero, 0,0,0,0,0)`；🥈 `cpp2il…/DeckGeneralInfoDemo.cs:417-427` 同形）。</para>
        /// <para>🔴 **我们的等价物 = 菜单侧那扇卡片详情窗**（`Shell/CardDetailPopup` = 原版菜单版 `CardDisplayWindow`，
        /// 见它自己的文件头）。**判据只此一处**：与 `DeckInfoPopup.OpenWarlordDetail` 同一个写法，⛔ 别再写第二份。</para></summary>
        void OpenWarlordDetail()
        {
            var wl = CollectionData.Warlord(DeckIndex);
            if (wl == null) { Debug.LogWarning("[Practice] 这副没有督军 ⇒ 开不了详情窗"); return; }
            if (Manager == null) { Debug.LogWarning("[Practice] 没有 `WindowsManager` ⇒ 开不了详情窗"); return; }
            var w = CardDetailPopup.Create(Manager);
            Manager.OpenWindow(w);
            w.ShowCard(wl);
            Debug.Log("[Practice] 点督军名 ⇒ 开卡片详情窗（「" + wl.Name + "」；原版 `WarlordInfoButtonClick`）");
        }

        /// <summary>≡ 原版 `DeckGeneralInfoDemo.ConfigPanel(...)` 的**卡背那一段**（账 `卡组:138`）。
        /// <para>判据（🥇 `decomp_full/DeckGeneralInfoDemo__ConfigPanel.c`：
        /// ① 收尾处 `currentCardback = param_7`；`currentCardback` 为空 ⇒ 直接 return；
        /// ② `sprite = currentCardback.CosmeticSprite`；③ `foreach (Image img in cardBacks)`：
        ///   `img.sprite != null && img.sprite == sprite` ⇒ **break**（同图什么都不做）；
        ///   否则 `img.sprite = sprite; img.enabled = true; cardbacksAnimation.Rewind(); cardbacksAnimation.Play();`）。
        /// 🥈 `cpp2il/out_cpp2il_dev_ilspy/DeckGeneralInfoDemo.cs:280-319` 同形。</para>
        /// <para>🔴 **「5 连轮播」是错的 —— 现读是 2 层**（派活包/施工单写「5 连」，**与 prefab 不符**，就地订正）：
        /// `MonoBehaviour_-3841077181908889796.cardBacks` 数组长度 = **2**，两个 PPtr 分别落到
        /// `Cardback`（`RectTransform_-3917913493052829892`）与它的子件 `Cardback Front`（`RectTransform_8791577597720547132`）。
        /// `menu_dump` 那棵树也只有一个 `Cardback` ▸ 一个 `Cardback Front`。</para>
        /// <para>🔴 **那条「轮播」动画 = `OpenCardbacks` clip（0.25 s，`m_WrapMode = 1 = Once`）**，不是循环：
        /// 现读 `AnimationClip_7721407185759151047` —— 两条 `m_RotationCurves` 都从**单位四元数**
        /// （`z = 0`，即 0°）走到各自 prefab 的静止角（外层 `+1.60°`、内层 `−3.43°`）；
        /// `Cardback Front` 的 `m_FloatCurves` 是 `anchoredPosition (0.3,0.3) → (11.2012,4.9016)`；
        /// 外层那两条 `anchoredPosition` 曲线只有一个 `t=0.25` 的键 = **自己不动**（只当常量保持）。
        /// ⇒ 视觉 = 「两张卡背从正着的位置扇开成静态那个倾角，正面那张顺带滑 11px」。
        /// ⚠️ 插值方式：原版是 Unity 的 Hermite 自动切线（两端 `inSlope/outSlope = 0`），我们用 `InOutQuad` 近似
        /// —— 关键帧是原版值，只有**帧间**那一档是退化写法（同 `Shell/BoosterPackAnim.cs` 的口径）。</para>
        /// <para>⚠️ 原版 `cardbacksAnimation` 挂在**外层 `Cardback` 那个 GameObject** 上（`Animation_-3822919705446428868.m_GameObject`），
        /// 而它的曲线同时驱动子件 ⇒ 我们用一条 `DOTween.To` 同时写两层的 `localRotation` 与内层的 `localPosition`。</para></summary>
        void BuildCardbacks(CollectionData.DeckInfo d)
        {
            var tex = CardArt.DeckCardback(d.CardbackId, d.Faction);
            if (tex == null) { Debug.LogWarning("[Practice] 卡背图取不到，两层都没建"); return; }
            // 外层 `Cardback`：矩形/队列/图都与建窗前一致（⚠️ 行数与原来那一条相等，账的落点不漂）
            _cardback = ImgTex(_general, tex, "Cardback(这副牌的卡背)", CbL, CbT, CbR, CbB,
                               "Cardback", QPrFront, true);
            // 内层 `Cardback Front`：原版是外层子件 ⇒ 平铺后世界角 = 外层 + 内层，队列排在外层之上
            _cardbackFront = ImgTex(_general, tex, "Cardback Front(卡背正面)", CbFrontL, CbFrontT, CbFrontR, CbFrontB,
                                    "Cardback Front", QPrText, true);
            _cardbackTex = tex;
            if (_cardbackFront != null) _cardbackFrontRestPos = _cardbackFront.transform.localPosition;
            ApplyCardbacksPose(1f);      // 先落静止态（批处理/编辑器里就是终态）
            _cardbacksTween = null;
            if (Application.isPlaying) PlayCardbacksAnim();   // 原版第一次 `ConfigPanel` 会 Rewind + Play
        }

        /// <summary>换卡背图（两个刷新点共用：`PickDeck` 的玩家卡组 / `PickPrebuilt` 的预组）。原版 `ConfigPanel` 的同图守卫照抄。
        /// ⚠️ 与原来那两句 `_cardback.SetTexture(...)` **行数相等**（账的落点不漂）。</summary>
        void SetCardbackTexture(Texture2D tex)
        {
            if (tex == null) { Debug.LogWarning("[Practice] 卡背图取不到 ⇒ 不换（原版 `currentCardback` 为空时整段 return）"); return; }
            bool same = _cardbackTex == tex;      // 原版：`img.sprite == cosmeticSprite` ⇒ break（连 Rewind 都不做）
            if (_cardback != null) _cardback.SetTexture(tex);
            if (_cardbackFront != null) _cardbackFront.SetTexture(tex);
            _cardbackTex = tex;
            if (!same) PlayCardbacksAnim();
        }

        /// <summary>把两层卡背摆到动画的第 <paramref name="t"/>（0 = 正着、1 = 静止倾角）那一帧。
        /// `t = 1` 与 prefab 的静止态逐值一致（外层 `1.60°` · 内层世界 `−1.83°` · 内层在自己的静止位上）。</summary>
        void ApplyCardbacksPose(float t)
        {
            if (_cardback != null)
                _cardback.transform.localRotation = Quaternion.Euler(0f, 0f, CbRot * t);
            if (_cardbackFront == null) return;
            _cardbackFront.transform.localRotation = Quaternion.Euler(0f, 0f, (CbRot + CbFrontLocalRot) * t);
            float k = 1f - t;      // 起点 = 静止位 + `CbFrontSlide`（原版 clip 的 `(0.3,0.3)` 相对 `(11.2012,4.9016)`）
            _cardbackFront.transform.localPosition = _cardbackFrontRestPos
                + new Vector3(LayoutSpace.Px(CbFrontSlide.x) * k, LayoutSpace.Px(CbFrontSlide.y) * k, 0f);
        }

        /// <summary>从第 0 帧重播卡背入场（原版 `cardbacksAnimation.Rewind(); cardbacksAnimation.Play();`）。
        /// ⛔ 只在真 Play 起补间：批处理没有帧循环 ⇒ 起了会永久停在「正着」那一帧（同 `PlayPackOpenAnim` 那道闸）。</summary>
        void PlayCardbacksAnim()
        {
            ApplyCardbacksPose(1f);
            if (!Application.isPlaying) return;
            if (_cardbacksTween != null) { DG.Tweening.TweenExtensions.Kill(_cardbacksTween); _cardbacksTween = null; }
            float t = 0f;
            ApplyCardbacksPose(0f);
            var tw = DG.Tweening.DOTween.To(() => t, v => { t = v; ApplyCardbacksPose(v); }, 1f, CardbacksAnimDuration);
            DG.Tweening.TweenSettingsExtensions.OnComplete(tw, () => { ApplyCardbacksPose(1f); _cardbacksTween = null; });
            DG.Tweening.TweenSettingsExtensions.OnKill(tw, () => ApplyCardbacksPose(1f));
            // ⚠️ 缓动只能挂在**数值补间自己**身上（Sequence 上挂非 Linear 会整条时间轴再弯一次）。
            _cardbacksTween = CardTween.Use(tw, DG.Tweening.Ease.InOutQuad, this);
        }

        // ============================================================ 账 未分类:509 / 510 · `Warlord Name` 上的 `RectSizeLimiter`
        // 【判据（2026-10-28 现读，两份都读了）】
        //   · prefab：`Warlord Name` 的组件表 = `TextMeshProUGUI, ContentSizeFitter, RectSizeLimiter, Button, EverguildTextController`
        //     （`python 工具/menu_dump.py bundle_menus_assets_all "Practice Mode Menu" --depth 8 --md`，
        //      同一行还印出 `⚙CSF h:PreferredSize ; RSL （0×34 在界内，没改）`）
        //   · 实例值：`m_maxSize = (350, 0)` · `m_minSize = (0, 0)`（两份 MB 逐位相同，见 `WnL..WnB` 的 summary）
        //   · 组件体：`Shell/RectSizeLimiter.cs`（逐句照 `decomp_full` 的两份 `.c`）
        // ⇒ 这一颗的**上限 350 是原版数字**，不再是「我们挑的」。

        /// <summary>原版 `Warlord Name` 那颗 `RectSizeLimiter` 的 `m_maxSize.x`（**350**，判据 = prefab 实例字段，见上）。</summary>
        public const float WarlordNameMaxW = 350f;

        /// <summary>把原版那颗 `RectSizeLimiter` 装到 `Warlord Name` 上，并**立刻跑一次**原版那两个回调。
        /// <para>🔴 为什么要手动跑一次：本仓的 UI **不走 uGUI 布局管线**（每颗 rect 都是显式摆好的），
        /// 所以没有任何 `LayoutRebuilder` 会来调 `SetLayoutHorizontal/Vertical` —— 不跑就等于「装了但从不执行」。
        /// 跑的那一次用的是**原版的入参**（`rectTransform` = 这一格、`maxSize = (350,0)`、`minSize = (0,0)`），
        /// ⛔ 不调全局 `ForceRebuildLayoutImmediate`（那会把整棵树上的其它控件也排一遍，动静过大）。</para>
        /// <para>⚠️ 夹取会真的生效：我们那个常量框宽 = `WnR − WnL` = **352.86 px** &gt; 原版上限 350
        /// ⇒ 夹完这一格是 **350 px**（差 2.86 px）。这正是原版的行为（它的框永远不可能超过 350）。</para></summary>
        static void AttachWarlordNameLimiter(Label lb)
        {
            if (lb == null) { Debug.LogWarning("[Practice] `Warlord Name` 那颗 `Label` 没建出来 ⇒ 原版的 `RectSizeLimiter` 装不上（出声，不静默）"); return; }
            var rt = lb.transform as RectTransform;
            if (rt == null) { Debug.LogWarning("[Practice] `Warlord Name` 底下取不到 `RectTransform` ⇒ `RectSizeLimiter` 装不上（出声）"); return; }
            var lim = rt.gameObject.AddComponent<RectSizeLimiter>();
            lim.rectTransform = rt;                                         // 原版由 prefab 序列化赋值
            lim.maxSize = new Vector2(LayoutSpace.Px(WarlordNameMaxW), 0f);  // 原版 `(350, 0)`，换算进本仓的布局单位
            lim.minSize = Vector2.zero;                                     // 原版 `(0, 0)`
            lim.SetLayoutHorizontal();                                      // 原版这一格由布局泵每趟调；本仓没有泵 ⇒ 显式跑一次
            lim.SetLayoutVertical();
            Debug.Log("[Practice] `Warlord Name` 装上原版 `RectSizeLimiter`（maxSize = (" + WarlordNameMaxW + ", 0) · minSize = (0, 0)）"
                    + " ⇒ 当前宽 " + (rt.rect.width / LayoutSpace.Px(1f)).ToString("F2") + " px（上限 " + WarlordNameMaxW + " px）");
        }

        // ============================================================ 账 `战斗逻辑驱动:1280`（追加在类尾）
        /// <summary>`Character Image` 上**现在挂着的那张整身立绘属于哪位督军**（`null` = 没有 / 走的是卡面插图兜底）。
        /// <para>🔴 为什么非记不可：原版 `PlayerDeckDemoDeckInfoWrapper` 放的是**它自己持有的那个句柄**
        /// （`PlayerDeckDemoDeckInfoWrapper__UnloadCharacterImage.c:7-11` 读 `this+0x58`），所以它天然知道「上一张是谁」；
        /// 我们只有一条**按路径**的登记（`CardArt._cache`）⇒ 换卡组时**只知道新的、不知道旧的**
        /// ⇒ 不记下这一个 id，换图那一刻就放不掉上一张（静默把旧图永久留在缓存里）。</para>
        /// <para>写点只有两处：`RefreshCharacterImage()`（换图时改）与 `Close()`（放完置空）。</para></summary>
        string _charImgWarlordId;
    }
}
