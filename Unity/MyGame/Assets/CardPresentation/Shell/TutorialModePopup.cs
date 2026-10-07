// TutorialModePopup.cs — 主菜单「TUTORIAL」模式卡点开的那扇窗
//   （原版 prefab **`Tutorial Mode Menu`** · 类 **`TutorialModePopup : GameWindow`**）
//
// ============================ 出处（唯一正本）============================
// ① 原版 prefab：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/` 里那个 `Tutorial Mode Menu`
//    （脚本实例 = `MonoBehaviour/MonoBehaviour_-6844287500407552138.json`）。
//    本文件**每一个矩形/字号/图名**都是 2026-10-17 现读出来的，逐条写在各自那一行；
//    取数命令 = `python 工具/menu_dump.py bundle_menus_assets_all "Tutorial Mode Menu" --depth 4 --md`
//    + 同一条命令换成 `"Tutorial Army Select Button"`（名单那一格的模板）。
// ② 原版方法体：`d:/2/tools/decomp_full/TutorialModePopup__{Open,Close,BattleButtonOnClick,
//    ConfigureArmyFilterButtons,SetArmyButtons,ChangeSelectedArmy}.c` ·
//    `TutorialSpecificInfo__Setup.c` · `TutorialArmySelectionButton__Initialize.c` ·
//    `WindowHeaderWithBackButton__Initialize.c`；签名桩在 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/`。
// ③ 关卡数据：6 个 `Demo DeckInfo {UM,Orks,Sautekh,BlackLegion,Aeldari,Leviathan} Tutorial`
//    （4 个在 `bundle_menus_assets_all/MonoBehaviour/`、2 个在 `bundle_duplicateassetisolationso_assets_all/`）
//    + 12 副 `bundle_prebuiltdecks_assets_all/MonoBehaviour/*_Deck0_Tutorial{1..6}_*.json`。
//
// ============================ 窗口参数（逐字段实读）============================
// `type = 1 (Popup)` · `windowsPlacement = 15 (Popup)` · `closeOnESC = 1` · `extraScaleSmallScreen = 1.0`。
// 判据 = `资料/普查产出_1006/A154_A155_窗口档位与缩放.md:361`（那一行就是本窗：`93 | Tutorial Mode Menu |
// TutorialModePopup | 1 | 15 | 1 | 0 | 1 | 1`）。⚠️ 与练习窗不同 —— **那是 1.07，本窗是 1.0**。
//
// ============================ 链路 ============================
// 原版：主菜单那张模式卡的根上挂 `LiveopMenuContainer` + `EverguildButton` → 按 **liveop 事件数据**
//   里的 `drawData.EventComponents[MainWindow]` 开这扇窗（全游戏**唯一**消费 `TutorialEvent` 的窗就是它）。
//   窗里那颗 `Play` 钮才 `MatchMakerManager.StartMatch(…, PlayModes.Tutorial = 4, …)` 进战斗。
// 我们：`Shell/MainMenuRuntime.cs` 的 `OpenMode("tutorial")` 直接建本窗（那张映射在服务端，本地没有
//   ⇒ 入口是**我们接的**，同 A789 的 `draft` 先例）。
// ⚠️ **教程事件那份数据本地任何形态都没有**（随 PlayFab userInfo 下发）⇒ 原版关服时这张卡根本不存在。
//   受影响的只有两处，各自在下面标了「**查不到**」：① 标题栏的字（`WindowHeaderWithBackButton.Initialize`
//   从事件数据取标签）· ② 各关的副标题/描述（远端词条表）。
//
// ============================================================ 进教程战斗那一条
// ✅ **2026-10-17（B29）起 `Play` 钮真开局** —— 原版 `TutorialModePopup.BattleButtonOnClick` →
//    `MatchMakerManager.StartMatch(…, playMode: 4 /* PlayModes.Tutorial */, playerDeck: 选中那关的预组牌,
//     enemyDeck: null, …)`。我们这条链**跨场景** ⇒ 走两条静态通道（读一次就清）：
//      `BattleDriver.SetPendingTutorialStage(i)` + `SetPendingPlayMode(GameMode.Tutorial)` → `LoadScene`。
//      ⚠️ 本窗口**只负责「第几关」**；关卡数据 / 两副牌 / 执行器全在 `BattleDriver.BeginTutorial` 里现取。
//
// 🔴 **原文记的「三件缺口」现在是这个状态**（留痕，⛔ 别再照旧话去「补」）：
//   ① **教程关卡执行器** —— ✅ **引擎侧已落地**（`RuleEngine/Core/TutorialScript.cs`：单指针状态机 +
//      玩家动作白名单闸门 + `playerAlwaysWins`/`preventPlayerResign`）；数据源是
//      `Resources/tutorial_stages.json`（6 关 / 79 回合 / 462 动作，全量版）。
//      🔴 **2026-10-18 更正（原来写「动作只接了 `DrawCard`（+ `PlayerChoice` 的空转）、`PlayCard`/`Attack`/
//      `ActiveAbility`/`ChangeToX` 四族还没接」—— 那句**已过期**，铁律 5）**：
//      ① 真正的病根不是「接得少」，是 **执行器在真局里一次都没被驱动**（`PlayScriptedTurn` /
//         `UpdateTurn` / `PermitsPlayerAction` 在 `CardPresentation/` 生产代码里**零调用点**、
//         `Ctx.Tutorial` 从没被读过）⇒ 当时实际是 **0 条**在执行，不是「只接了 DrawCard」；
//         **2026-10-18 已接线**（AI 回合脚本接管 · 玩家回合每帧推一步 · 四个 `BeginTurn` 后跟
//         `SyncTutorialTurn` · 新增 `ContinueTutorialScript()` = 原版「玩家做完 ⇒ 指针 +1」那个口）。
//      ② 六档**已实现**：`PlayCard` / `Attack` / `AttackFreeMode` / `ChangeToRanged` / `ChangeToMelee`；
//         **`ActiveAbility` 教程数据 0 条** ⇒ 只留**桩 + 出声**（原版 `ExecuteAction` 里有它，6 关不用）。
//      ③ 认不出的动作**不执行 + 出声**并记进 `TutorialScript.Unhandled`；⚠️ 「**原版本来就没有**的档」
//         （如 `EndTurn` 73 条 —— 原版 `ExecuteAction` 里根本没有分支 = no-op）另记 `StageActions`
//         ⇒ ⛔ **别把这两本账混读成同一件事**（一本是「我们没做」，一本是「原版就没有」）。
//      🔴 **仍没做的**：教程**表现层**（小提示 / 左右箭头 / 高亮 / 指点光标 / 教学标注 / 跳过钮 /
//         聊天三档含 `RadioChat` / 音效 / 督军两拍落场 / 五个 `hide*`）与 **`A939` 胜利脚本 13 条**。
//   ② **教程对局规则** —— ✅ **已进引擎**：不洗牌 / 先手照关卡 / 不起换牌阶段 / 起手卡插牌库顶 /
//      起始单位落场 / 初始法力（6 关全 0）/ `playerAlwaysWins`（6 关全 false）逐条照做；
//      参数收在 `GameplayVariables.Tutorial`（`For(GameMode.Tutorial)` 唯一入口）。
//      ⚠️ **还没接的**：`hideCemetery` / `hideCardsLeftInDeck` / `hideLargeCardDisplay` / `hideChat`
//      四条**只到引擎字段**（`ctx.Tutorial.Stage.*`），BattleHud 那一侧还没读它们。
//   ③ **教程那 12 副牌** —— ✅ **数据早就有了**（`tutorial_decks.json`，B9 那批落地的），
//      `BattleDriver.BeginTutorial` 按 `tutorialIndex` 取「那一关的 player/ai 两副」。
//      ⚠️ 那 12 副里有**已知的洞**（原版 id 表没覆盖到的卡号，本地无 id→名 表可补）⇒ 逐条出声、不补不猜。
//
// ============================ 建窗纪律（同族六份逐句对过）============================
// `Create()` 建 `RectTransform` 根 + 写 `sizeDelta`（A218）· 六条窗参逐条赋 · `Open()` 里 `Build()` 打头
// （`PurchasePremiumWindow` 就是漏了 `Build()` ⇒ 窗参六条全绿、树却是空的）· 压暗层走 `MenuDraw.ShadeHit`
// · 窗底走 `MenuDraw.Absorb` · 命中区档严格高于压暗层档。
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>`TutorialModePopup : GameWindow` —— **`Tutorial Mode Menu`**（教程模式窗）。
    /// 6 关各一行（左列），右列是选中那一关的说明 + 开战钮。表 = `Rows`。</summary>
    public class TutorialModePopup : GameWindow
    {
        // ============================================================ 队列
        // 🔴 **一条梯子，每层 +1**（同队列「谁盖谁」不可控 —— 透明物按到相机的 3D 距离排，见
        //    `CLAUDE.md` §三 那两条）。本段 **3420–3430 全库未占**（2026-10-17 现扫：全工程 `const int Q*`
        //    取值为 3400–3405 = `DuelPopupWindow` · 3450–3458 = `BattleLogPopup` · 3460–3478 =
        //    `RankedRewardEventWindow` · 其他都在 3420 之外）⇒ 本窗独占这一段，不与既有任何一扇撞档。
        public const int QShade = 3420;   // `Menu Dark Background`（视觉压暗层）——**它的命中区也用这一档**
        const int QBg = 3421;             // `Generic Window Red Background Big`（窗底红板）+ 它的吸收层
        const int QDeck = 3422;           // `Warlod Image`（督军立绘）
        const int QDeck2 = 3423;          // `Warlord Darkening`（压立绘那层）
        const int QArt = 3424;            // `Header Background` · 名单格的横幅底 `Background (1)` · 钮底
        const int QArt1 = 3425;           // `Header Background (1)`（往左伸的尖角）· 名单格的圆底 `Background`
        const int QArt2 = 3426;           // `Header Back Button` · 名单格的 `Highlight`
        const int QArt3 = 3427;           // 名单格的 `BackgroundComplete`
        const int QArt4 = 3428;           // 名单格的 `Icon`（最上一件）
        const int QText = 3429;           // 全部文案
        /// <summary>窗内**命中区**那一档 —— 必须**严格高于** `QShade`（判据 → `MenuDraw.ShadeHit`）。</summary>
        public const int QHit = 3430;

        // ============================================================ 几何（全部照 `menu_dump` 实读，绝对画布 px）
        // ① 压暗层：`Menu Dark Background`（原版 4574.60×2572.36 —— 比屏幕大，四周都溢出）
        const float ShadeL = -1327.30f, ShadeT = -746.18f, ShadeR = 3247.30f, ShadeB = 1826.18f;
        /// <summary>压暗的色 —— 原版 `Image.m_Color = (0,0,0,**0.773**)`（同族窗逐值相同）。</summary>
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.773f);

        // ② 窗底红板：`Generic Window Red Background Big`（sprite `UI_Deck_Information_Back` 1100×701 ·
        //    九宫 **(42,363,655,81)** · `Sliced` · `m_PixelsPerUnitMultiplier 0.62`）
        //    实读矩形 -601.28,105.84→2521.28,1028.32（3122.56×922.49）。
        public const float BgL = -601.28f, BgT = 105.84f, BgR = 2521.28f, BgB = 1028.32f;
        public const string ArtPaneBg = "UI_Deck_Information_Back";
        public static readonly Vector4 BgBorder = new Vector4(42f, 363f, 655f, 81f);
        public const float BgTexW = 1100f, BgTexH = 701f;

        // ③ 标题栏：`Header With Back Button`（0,40.87→550,150.42）
        //    🔴 **这是原版那个共用件 `WindowHeaderWithBackButton`**（`TutorialModePopup.header` 字段指的就是它），
        //       同族的 `SkirmishModeEventWindow` / `RankedEventWindowV2` / `Daily Streak Popup` 用的是**同一个 prefab**
        //       ⇒ 三个 plate/返回钮的矩形**逐值相同**。这里**直接复用 `LiveOpsEventWindow` 的公开常量**
        //       （⛔ 不抄第二份数字：CLAUDE.md §三「两处写同一条规则 = 迟早不一致」）。
        //       ⚠️ 唯一的差别 = 本窗的节点叫 `Header With Back Button`，**没有** `Game Mode Icon` 那一件
        //       （实读：它下面只有 `Header Background` / `Header Background (1)` / `Header Back Button`）。
        const float HdrL = LiveOpsEventWindow.HdrL, HdrT = LiveOpsEventWindow.HdrT;
        /// <summary>`Header With Back Button` **自己那一格**（实读 0,40.87→550,150.42 = 550×109.55）。
        /// ⚠️ 它比下面那颗 `Header Background`（到 156.23）**矮** —— 原版就这样（子件可以探出父件的矩形），
        /// ⛔ 别「顺手对齐」成父宽高。</summary>
        const float HdrR = 550f, HdrB = 150.42f;
        const float HdrBgT = LiveOpsEventWindow.HdrBg1T, HdrBgB = LiveOpsEventWindow.HdrBg1B;
        const float HdrBg1L = LiveOpsEventWindow.HdrBg1L, HdrBg1R = LiveOpsEventWindow.HdrBg1R;
        const float HdrBackL = LiveOpsEventWindow.HdrBackL, HdrBackT = LiveOpsEventWindow.HdrBackT;
        const float HdrBackR = LiveOpsEventWindow.HdrBackR, HdrBackB = LiveOpsEventWindow.HdrBackB;
        /// <summary>`Header Background` 的**布局后**宽度 = **550**（原版挂 `ContentSizeFitterMinMax`
        /// `widthMin 550 / widthMax 1250`，内容只有 **491.12** ⇒ **clamp 到下限 550**）。
        /// 判据 = `menu_dump` 那一行 `Header Background 0.00,40.87→550.00,156.23`。
        /// 🔴 **它随标题变长会撑开**（同族遭遇战窗的标题更长 ⇒ 那边是 690.86）——
        /// 我们这行字 = prefab 出厂原文 `Game mode`，与实读那一份同字 ⇒ **550 就是这一态的值**，
        /// ⛔ 别把它当成常量去套别的标题。</summary>
        const float HdrBgR = 550f;
        /// <summary>`Window Title`（实读 155,57.22→430.12,139.87 · 275.12 宽 = `ContentSizeFitter`
        /// 按这行字算出来的）。文案 **`Game mode`** = prefab 出厂 `m_text`
        /// （`MonoBehaviour_-1926919290004399602.json` 那一族同值）。`fontSize 67.55` · `base 36` ·
        /// `auto[18, 67.55]` · `characterSpacing 5` · `hAlign Left`。
        /// 🔴 **原版运行时它会被换掉**：`WindowHeaderWithBackButton.Initialize` 末尾
        /// `titleText.text = LiveOpsAssetUtility.GetLabel(事件数据)` —— **那是教程事件里的标签，
        /// 本地任何形态都没有**（见文件头）⇒ 我们照 prefab 出厂原文印 `Game mode`，并出声一次。</summary>
        const float TitleL = 155f, TitleT = 57.22f, TitleR = 430.12f, TitleB = 139.87f;
        public const string TitleText = "Game mode";

        // ④ `Warlod Image`（410.93,-9.07→1509.07,1089.07，1098.14² · 原版 `sprite = 0` ⇒ 运行时喂）
        //    + 它下面那层 `Warlord Darkening`（489.97,627.87→1430.03,947.80 = 940.05×319.93 ·
        //      `Smooth background square` 32² 九宫 (12,2,12,12) · `Sliced` · `m_Color (0,0,0,0.816)`)
        //    🔴 **两件的矩形与同族遭遇战窗【不同】**（那边是 450.93,-95.07→1549.07,1003.07 /
        //       529.97,418.18→1470.03,975.82）—— 铁律 5·c：一个值 ≠ 全部情况，⛔ 别互推。
        public const float WlL = 410.93f, WlT = -9.07f, WlR = 1509.07f, WlB = 1089.07f;
        public const float WlDarkL = 489.97f, WlDarkT = 627.87f, WlDarkR = 1430.03f, WlDarkB = 947.80f;
        public static readonly Color WlDarkTint = new Color(0f, 0f, 0f, 0.816f);

        // ⑤ `PlayTutorialButton`（1387.83,902.70→1828.17,1023.30 = 440.33×120.60 ·
        //    `UI_Button_Mulligan` 410×124 · **`Simple` + `preserveAspect = 1`**）
        //    + `Button Text`（1403.28,909.70→1812.05,1016.18 · `Play Tutorial` · fs 74.25 · base 12 · auto[10,74.25]）
        public const float PlayL = 1387.83f, PlayT = 902.70f, PlayR = 1828.17f, PlayB = 1023.30f;
        public const float PlayTxL = 1403.28f, PlayTxT = 909.70f, PlayTxR = 1812.05f, PlayTxB = 1016.18f;
        public const string PlayText = "Play Tutorial";

        // ⑥ `TutorialInfo`（1344.25,244.88→1866.17,831.10）+ 里面四行
        //    四条的字号/自适应窗口/对齐**逐条实读**（`Left/Capline` 三条 + `Left/Top` 折行一条）。
        public const float InfoL = 1344.25f, InfoT = 244.88f, InfoR = 1866.17f, InfoB = 831.10f;
        public const float ITiL = 1344.25f, ITiT = 265.04f, ITiR = 1847.20f, ITiB = 370.73f;   // `TutorialTitle`   fs 74.25
        public const float ISuL = 1343.67f, ISuT = 326.52f, ISuR = 1847.77f, ISuB = 457.85f;   // `TutorialSubTitle` fs 54.30
        public const float IWlL = 1344.25f, IWlT = 475.89f, IWlR = 1847.20f, IWlB = 552.94f;   // `TutorialWarlordTitle` fs 54.30
        public const float IDsL = 1344.25f, IDsT = 576.53f, IDsR = 1866.17f, IDsB = 831.10f;   // `TutorialDescription`  fs 40.00

        // ⑦ `Army Selector`（60,244.89→649.68,902.69 = 589.68×657.80 · `ScrollRect` 纵向）
        //      └ `Viewport`（**与父件同矩形** · 挂 `RectMask2D`）
        //          └ `Filters`（`VerticalLayoutGroup` **spacing 7.31** · `align 0`/UpperLeft · pad 全 0 ·
        //                      出厂 **0 高 0 子** —— 6 格全是运行时 `Instantiate` 出来的）
        public const float ArmL = 60f, ArmT = 244.89f, ArmR = 649.68f, ArmB = 902.69f;
        /// <summary>`Filters` 的 `m_Spacing = 7.31`（实读）。**纵向**。</summary>
        public const float ArmGap = 7.31f;
        /// <summary>`Completed Text`（-0.01,129.52→658.33,301.02 · fs 54 · base 12 · auto[10,54] · 居中）。
        /// 原版文案 = `String.Format(词条 "Demo/Tutorial", 已完成数, 总数)`（`TutorialModePopup__Open.c`：
        /// `GetTranslation(…)` → `Concat(完成数, "/", 总关数)` → `Format`）⇒ 词条名在远端，
        /// **我们印的就是那个格式本身**：`Completed: n/6`。</summary>
        public const float CompL = -0.01f, CompT = 129.52f, CompR = 658.33f, CompB = 301.02f;

        // ---- 名单那一格（`Tutorial Army Select Button`，独立 prefab · 根 **589.75×200**）----
        // 几何 = 把实读的**绝对矩形减掉模板根左上角 `(-294.88, 980.00)`**（模板根 pivot(0.5,0.5)、
        // 尺寸 589.75×200，工具把它摆在画布中央 ⇒ 减完就是「相对格左上角」）。
        public const float ItemW = 589.75f, ItemH = 200f;
        /// <summary>格内五层 + 三行字（相对格左上角；括号里是实读的绝对矩形）。
        /// 兄弟序（= 画序，**照原版**）：`Background (1)` → `Background` → `Highlight` →
        /// `BackgroundComplete` → `Icon`。⛔ 别按「谁是底图」的直觉重排。</summary>
        public static readonly PxRect ItemBanner = new PxRect(7.50f, 16.31f, 589.75f, 189.93f);   // `Background (1)`  582.26×173.62
        public static readonly PxRect ItemBack = new PxRect(-5.00f, 5.09f, 191.93f, 202.01f);    // `Background`      196.93×196.92
        public static readonly PxRect ItemHL = new PxRect(-5.00f, 5.09f, 191.93f, 202.01f);      // `Highlight`（与上一件同矩形）
        public static readonly PxRect ItemBar = new PxRect(0.00f, 170.80f, 187.36f, 197.01f);    // `BackgroundComplete` 187.36×26.21
        public static readonly PxRect ItemIcon = new PxRect(-5.00f, 5.09f, 180.41f, 190.51f);    // `Icon`            185.41×185.41
        public static readonly PxRect ItemTi = new PxRect(234.15f, 20.72f, 581.20f, 67.28f);     // `TutorialTitle`   347.05×46.55
        public static readonly PxRect ItemSu = new PxRect(234.16f, 67.28f, 581.20f, 114.91f);    // `TutorialSubTitle` 347.04×47.63
        public static readonly PxRect ItemDone = new PxRect(234.15f, 130.66f, 584.00f, 186.74f); // `TutorialComplete` 349.85×56.08
        /// <summary>格内横幅的色 —— 原版 `Background (1)` 的 `m_Color = (0.423, 0.348, 0.547, 1)`。</summary>
        public static readonly Color ItemBannerTint = new Color(0.423f, 0.348f, 0.547f, 1f);
        /// <summary>进度条的色 —— 原版 `BackgroundComplete` 的 `m_Color = (0, 1, 0.078, 1)`。</summary>
        public static readonly Color ItemBarTint = new Color(0f, 1f, 0.078f, 1f);
        public const string ArtItemBanner = "Tutorial_Background";        // 512×144（下划线：见下方 `Tex` 那一跳的口径）
        public const string ArtItemBack = "UI_Army_Selection_Back";       // 172×172
        public const string ArtItemHL = "UI_Army_Selection_Back_Pressed"; // 172×172（出厂 act **F**，选中才开）
        public const string ArtItemBar = "UI_Army_Selection_Back_Progression"; // 48×18 九宫 (20,0,20,0)
        public static readonly Vector4 ItemBarBorder = new Vector4(20f, 0f, 20f, 0f);
        /// <summary>`TutorialComplete` 的文案 —— 原版 prefab 出厂 `m_text = 'Complete!'`
        /// （`TutorialArmySelectionButton.Initialize` 里那一行是词条，本地没有 ⇒ 照出厂原文印）。</summary>
        public const string ItemDoneText = "Complete!";

        // ============================================================ 6 关的表
        /// <summary>6 关 —— **一行一关**，四列都有出处：
        /// <para>· `So` = 原版 `Demo DeckInfo * Tutorial` 的资产名（`TutorialModePopup.decks[6]` 那 6 个）。</para>
        /// <para>· `Idx` = 该 SO 的 `tutorialIndex` 字段（实读：UM 0 · Orks 1 · Sautekh 2 · BlackLegion 3 ·
        ///   Aeldari 4 · Leviathan 5）。⚠️ 它**不是**数组下标 —— 本表已按 `tutorialIndex` 升序排好。</para>
        /// <para>· `Deck` = 该 SO 的 `classicDeck` 指过去的那副预组牌（原版资产名，
        ///   `bundle_prebuiltdecks_assets_all/MonoBehaviour/<Deck>.json`）。</para>
        /// <para>· `Hero` = **督军卡 id**。推导链 = 原版资产名里那个督军短名 → 我们卡表里同阵营的**唯一**一个
        ///   名字含该短名的 `hero` 卡。🟢 **独立交叉验证**：第 0 关推出来是 `UM3 Uriel Ventris`，
        ///   而 prefab 出厂原文写的就是 `Warlord: &lt;color=orange&gt;Uriel Ventris&lt;/color&gt;` —— **逐字吻合**。
        ///   （原版取名的判据 = `DemoDeckInfoSO__GetWarlordName.c` → `classicDeck.DeckHero.GetLocalizedCardName()`，
        ///    即「督军卡自己的名字」——本表这一列就是照这条链取的。）</para>
        /// <para>· `Fac` = 阵营（喂 `Warlord Image` 的立绘 + 名单格的徽记）；取的是**督军卡自己的阵营**
        ///   （= `DeckRuntime.FactionIcon` 的键）。</para>
        /// ⚠️ **原版还有两列本地取不到**（`loreLocalizationKey`：`Demo/UMTutorial` · `Demo/GoffDeckTutorial` ·
        ///   `Demo/NecronsDeckTutorial` · `Demo/BlackLegionDeckTutorial` · `Demo/AeldariDeckTutorial` ·
        ///   `Demo/LeviathanDeckTutorial`）—— **正文在远端 CCD 的词条表里**（原版客户端连 TextAsset 目录都没有）
        ///   ⇒ 副标题/描述那两行**只有第 0 关有出厂原文**（`Stage1Subtitle`/`Stage1Description`），
        ///   其余 5 关**查不到**，见 `SubOf/DescOf` 那两条。</para></summary>
        public struct StageRow
        {
            public string So, Deck, Hero, Fac;
            public int Idx;
        }

        public static readonly StageRow[] Rows =
        {
            new StageRow { So = "Demo DeckInfo UM Tutorial",          Idx = 0, Deck = "Ultramarines_Deck0_Tutorial1_Uriel",     Hero = "UM3",  Fac = "Ultramarines" },
            new StageRow { So = "Demo DeckInfo Orks Tutorial",        Idx = 1, Deck = "Orks_Deck0_Tutorial2_Ghazghkull",         Hero = "GOF3", Fac = "Goff" },
            new StageRow { So = "Demo DeckInfo Sautekh Tutorial",     Idx = 2, Deck = "Sautekh_Deck0_Tutorial3_Zahndrekh",       Hero = "SAU3", Fac = "Sautekh" },
            new StageRow { So = "Demo DeckInfo BlackLegion Tutorial", Idx = 3, Deck = "BlackLegion_Deck0_Tutorial4_Sylar",       Hero = "BL5",  Fac = "BlackLegion" },
            new StageRow { So = "Demo DeckInfo Aeldari Tutorial",     Idx = 4, Deck = "Aeldari_Deck0_Tutorial5_Ghaelyn",         Hero = "ASH5", Fac = "SaimHann" },
            new StageRow { So = "Demo DeckInfo Leviathan Tutorial",   Idx = 5, Deck = "Leviathan_Deck0_Tutorial6_Tervigon",      Hero = "TL5",  Fac = "Leviathan" },
        };

        /// <summary>第 0 关（Ultramarines）的副标题 —— **prefab 出厂原文**（`TutorialSubTitle.m_text`）。
        /// 🔴 **只这一关有**：其余 5 关的字在远端词条表里（原版 `TutorialSpecificInfo.Setup` 走
        /// `Loc(loreLocalizationKey + 后缀)`），本地**查不到** ⇒ `SubOf()` 会出声并给「查不到」的占位。</summary>
        public const string Stage1Subtitle = "The Basics";
        /// <summary>第 0 关的描述 —— 同上，prefab 出厂原文（`TutorialDescription.m_text`）。</summary>
        public const string Stage1Description =
            "Start here! Spar with your Chapter Master to learn the teachings of the Codex before heading out into battle.";
        /// <summary>本地没有那两行字时印什么。🔴 **不许编文案**（铁律 11 第 ① 种：判据是空的）——
        /// 印一个**明确的**占位，不是空格（空格 = 静默失败）。</summary>
        public const string TextMissing = "—";

        // ============================================================ 状态
        /// <summary>选中的关卡（0..5）。原版 `TutorialModePopup._selectedArmy`（+0xc0）——
        /// `Open()` 里调的是 `ChangeSelectedArmy(10)`，而 `10 = CardArmy.Ultramarines`（同
        /// `PurchasePremiumWindow` 那条「`data` 为空时取 10」）⇒ **出厂选中第 0 关**。
        /// `Close()` 里原版把它归 0（`(param_1 + 0xc0) = 0`）—— 那**不是**「选中第 0 关」，
        /// 是 `CardArmy` 的 0 档（无）；我们这支没有那个语义，只把下标复位到 0。</summary>
        public int StageIndex;

        /// <summary>本窗 6 个格节点（自检用）。</summary>
        public readonly List<Transform> StageCells = new List<Transform>();
        /// <summary>取不到的图（一声不吭地少一层是红线 ⇒ 逐张记账，自检那条「图一张都不能少」会带上它）。</summary>
        public readonly List<string> MissingArt = new List<string>();

        /// <summary>最近一次开出来的那扇（自检用）。</summary>
        public static TutorialModePopup LastOpened;

        // ============================================================ 建
        public static TutorialModePopup Create(WindowsManager mgr)
        {
            // 窗口根 = `RectTransform` + `sizeDelta`（A218）。判据 = 原版 prefab 的根 RT：
            // `anchor (0,0)-(1,1)` · `sizeDelta (0,0)` ⇒ **绝对矩形 (0,0)-(1920,1080)**（整屏）。
            // ⚠️ 原版 stretch 拿父（Canvas）尺寸；我们用「重合锚点 + 屏尺寸」表达同一个矩形（锚点不复刻，
            //    见 `MenuDraw.SetPxSize` 那条）。⚠️ 本窗 `extraScaleSmallScreen = 1.0` 且
            //    `WindowsManager.AttachToAnchor` 把窗根写成 `localScale = one` ⇒ 今天是**单位缩放**。
            var go = new GameObject("Tutorial Mode Menu", typeof(RectTransform));
            MenuDraw.SetPxSize(go.transform, LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);
            var win = go.AddComponent<TutorialModePopup>();
            win.type = WindowType.Popup;                 // 实证 type = 1
            win.placement = WindowsPlacement.Popup;      // 实证 windowsPlacement = 15
            win.closeOnEsc = true;                       // 实证 closeOnESC = 1
            win.extraScaleSmallScreen = 1f;              // 实证 1.0（**别抄练习窗那个 1.07**）
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            StageIndex = 0;                 // 原版 `Open()` 末尾 `ChangeSelectedArmy(10)` = Ultramarines = 第 0 关
            Build();
        }

        /// <summary>原版 `Close()` = `GameWindow.Close()` + 把选中项复位（`TutorialModePopup__Close.c`）。
        /// ⚠️ 原版复位的那个 `0` 是 `CardArmy` 的 0 档、**不是**「第 0 关」；照我们的语义只复位下标。</summary>
        public override void Close()
        {
            StageIndex = 0;
            base.Close();
        }

        // ============================================================ 建整棵树
        void Build()
        {
            var root = transform;
            MissingArt.Clear();
            StageCells.Clear();
            MenuDraw.ClearChildren(root);

            BuildBackdrop(root);
            BuildPane(root);
            BuildHeader(root);
            BuildWarlordImage(root);
            BuildInfo(root);
            BuildArmySelector(root);
            BuildCompleted(root);
            BuildPlayButton(root);

            // 🔴 **标题栏那一行字是事件数据喂的**，本地没有（见文件头）⇒ 出声一次，不静默。
            Debug.Log("[Tutorial] `Window Title` 印的是 **prefab 出厂原文** `" + TitleText + "` —— 原版运行时"
                    + "由 `WindowHeaderWithBackButton.Initialize` 按**教程事件数据**里的标签换掉，"
                    + "而那份数据本地任何形态都没有（随 PlayFab userInfo 下发）⇒ 照出厂值印，不编一个标题。");
        }

        void BuildBackdrop(Transform root)
        {
            // ① 整屏压暗（原版 `Menu Dark Background`，rect 比屏大 + `m_Color (0,0,0,0.773)`）
            MenuDraw.Rect(root, CardArt.Solid(), new PxRect(ShadeL, ShadeT, ShadeR, ShadeB),
                          "Menu Dark Background", QShade, ShadeColor);
            // ② 点压暗层 = 关窗（原版那颗 `BackgroundCloseButton` 的 `onClick`）。
            //    🔴 档 = **压暗层自己那一档**（`QShade`），且严格 < 窗内命中区档 `QHit` ——
            //      同档时 `ImageQuad` 的世界 z 恒 0，点「Play Tutorial」会被判成点背景直接关窗。
            //      判据 → `MenuDraw.ShadeHit` 的注释 · `LiveOpsEventWindow.BuildBackdrop` 那两句。
            MenuDraw.ShadeHit(root, new PxRect(0f, 0f, 1920f, 1080f), QShade, QHit, () => Close(), "BackdropHit");
        }

        void BuildPane(Transform root)
        {
            // `Generic Window Red Background Big` —— 九宫底板（原版 `Sliced`）。
            // ⚠️ `m_PixelsPerUnitMultiplier = 0.62`：本工程的口径（`MenuDraw.Nine` 的 `borderOutPx`）
            //    在**同族同图**的既有调用点上一律**不传**（`Shell/BoosterInfoPopup.cs:316` 与
            //    `Shell/BaseOfferPopup.cs` 都是同一张 `UI_Deck_Information_Back` + 同一个九宫 (42,363,655,81)、
            //    都只传 `border`）⇒ 本处照那一份办，保持全工程同一处理。**不是**「算过 ppuMul」。
            MenuDraw.Nine(root, Tex(ArtPaneBg), new PxRect(BgL, BgT, BgR, BgB),
                          BgBorder, BgTexW, BgTexH, QBg, null, true, "Generic Window Red Background Big");
            // 原版那块 `Image` 的 `m_RaycastTarget = 1`、父链上没有点击处理器
            // ⇒ 射线打到窗底**什么都不做**（同族先例 = `PracticeModePopup.Build` 的 `AbsorbHit`、
            //   `BoosterInfoPopup` 的那一条）。⛔ 不加这一层，点窗底会穿到压暗层去**把窗关掉**。
            MenuDraw.Absorb(root, "AbsorbHit", new PxRect(BgL, BgT, BgR, BgB), QShade, QHit);
        }

        /// <summary>`Header With Back Button`（原版共用件 `WindowHeaderWithBackButton` 的一态）。
        /// 三层，**从下到上照原版的兄弟序**：`Header Background` → `Header Background (1)` → `Header Back Button`。
        /// ⚠️ 本窗**没有** `Game Mode Icon` 那一件（遭遇战/排位窗才有）。
        /// 返回钮的链：原版 `WindowHeaderWithBackButton.BackButtonPressed` → 那个 `UnityEvent` 回调
        /// （本窗注册的是 `TutorialModePopup.Close`）⇒ 我们直接 `Close()`。</summary>
        void BuildHeader(Transform root)
        {
            var hdr = MenuDraw.Node(root, "Header With Back Button", new PxRect(HdrL, HdrT, HdrR, HdrB));

            // `Header Background`：`WF_Campaign_Info_Background` 740×167 · 九宫 (335,0,395,0) · `Sliced`
            // 🔴 **2026-10-17（F3 · D3 红①，实现缺陷）**：原来这里**没给节点起名** ⇒ `MenuDraw.Nine` 落到
            //    缺省名 **`"Nine"`**（`Shell/MenuDraw.cs:1408` 末参 `string name = "Nine"`），而原版那颗
            //    **叫 `Header Background`**（`menu_dump` 实读）⇒ 自检读到「节点不在」。
            //    改法照**同族三份先例**：`Shell/LiveOpsEventWindow.cs:748` · `Shell/DailyStreakPopup.cs:523` ·
            //    `Shell/EnergySinglePlayerOnlyEventWindow.cs:488`（都是「先 `Node(具名)`、再把 `Nine` 挂进它」）。
            // ⚠️ 同时把 `Window Title` 挂进 `bgT` —— 原版它是 `Header Background` 的**子件**（缩进 3 层），
            //    我们原来挂在 `hdr` 底下（兄弟关系）⇒ 一并纠正。
            // ⚠️ 两种挂法**位置逐位相同**：`MenuDraw.Nine` / `MenuDraw.Text` 都走 `Local(parent, 矩形)`
            //    = `矩形中心 − PosInDesignSpace(parent)`，多插一层同矩形的 `Node` 之后
            //    `bgT.position + (中心 − bgT.position)` 仍等于原来的 `中心`。
            var bgT = MenuDraw.Node(hdr, "Header Background", new PxRect(HdrL, HdrBgT, HdrBgR, HdrBgB));
            MenuDraw.Nine(bgT, Tex(LiveOpsEventWindow.ArtHeaderBg), new PxRect(HdrL, HdrBgT, HdrBgR, HdrBgB),
                          LiveOpsEventWindow.HeaderBorder, LiveOpsEventWindow.HeaderTexW,
                          LiveOpsEventWindow.HeaderTexH, QArt);
            // `Window Title` —— 原版 hAlign = **Left**（不是居中）
            var title = MenuDraw.Text(bgT, new PxRect(TitleL, TitleT, TitleR, TitleB), TitleText, Color.white,
                                      "Window Title", 67.55f, QText);
            if (title != null)
            {
                // 🔴 **次序不能反**（工程血泪，同 `LiveOpsEventWindow.BuildHeader`）：改**渲染宽度**的那两句
                //    （字距 / 自适应）必须排在 `AlignLeft` **之前** —— `AlignLeftOn` 是量**当时的** `WorldW`
                //    反推位置 ⇒ 排在它之后改宽，字会整体往左溢出，**且不出声**。
                title.SetCharSpacing(5f);          // 原版 `m_characterSpacing = 5`
                title.SetAutoFitBox(LayoutSpace.Px(TitleR - TitleL), LayoutSpace.Px(TitleB - TitleT),
                                    18f, 67.55f, 36f);   // 原版 auto[18, 67.55] · `m_fontSizeBase 36.0`
                MenuDraw.AlignLeft(title, new PxRect(TitleL, TitleT, TitleR, TitleB));
            }

            // `Header Background (1)`（往左延伸的尖角）与返回钮：**兄弟序在后 ⇒ 队列更高**
            MenuDraw.Nine(hdr, Tex(LiveOpsEventWindow.ArtHeaderBg),
                          new PxRect(HdrBg1L, HdrBgT, HdrBg1R, HdrBgB),
                          LiveOpsEventWindow.HeaderBorder, LiveOpsEventWindow.HeaderTexW,
                          LiveOpsEventWindow.HeaderTexH, QArt1, null, true, "Header Background (1)");
            MenuDraw.Rect(hdr, Tex(LiveOpsEventWindow.ArtHeaderBack),
                          new PxRect(HdrBackL, HdrBackT, HdrBackR, HdrBackB), "Header Back Button", QArt2, null, true);
            MenuDraw.Hit(hdr, "BackHit", new PxRect(HdrBackL, HdrBackT, HdrBackR, HdrBackB), QHit, () => Close());
        }

        /// <summary>`Warlod Image` + `Warlord Darkening`。
        /// 原版那层 `Image` 出厂 **`sprite = 0`**，运行时由 `TutorialModePopup.SetArmyButtons` 喂
        /// `deck.classicDeck.DeckHero.fullHeroSprite`，且 **`Behaviour.set_enabled(图, sprite != null)`**
        /// —— 没有立绘时**整层关掉**（不是留一张空图）。我们照这一条办。</summary>
        void BuildWarlordImage(Transform root)
        {
            var holder = MenuDraw.Node(root, "Warlod Image", new PxRect(WlL, WlT, WlR, WlB));
            var hero = HeroDef(StageIndex);
            var tex = hero != null ? CardArt.Portrait(hero.Id) : null;
            if (tex != null)
            {
                // ⚠️ **原版那件 `m_PreserveAspect = 0`（直接拉满 1098.14²）**，而喂给它的是一张**方形**
                //    立绘；我们手上只有**卡面插图**（竖构图）⇒ 拉满会把人拉变形。
                //    这里 `keepAspect = true`（等比放进框、居中）**是我们挑的**，不是复刻 ——
                //    与同族 `LiveOpsEventWindow.BuildDeckSelection` 那条**逐字同一条处置**。
                MenuDraw.Rect(holder, tex, new PxRect(WlL, WlT, WlR, WlB), "Warlord", QDeck, null, true);
            }
            else
            {
                Debug.Log("[Tutorial] 第 " + (StageIndex + 1) + " 关的督军立绘取不到（"
                        + (hero != null ? hero.Name + " / " + hero.Id : "卡表里没有这张卡")
                        + "）—— `Warlod Image` 那一层**不画**（原版这一档就是 `set_enabled(false)`）");
            }
            MenuDraw.Nine(holder, Tex("Smooth_background_square"),
                          new PxRect(WlDarkL, WlDarkT, WlDarkR, WlDarkB),
                          new Vector4(12f, 2f, 12f, 12f), 32f, 32f,
                          QDeck2, WlDarkTint, true, "Warlord Darkening");
        }

        /// <summary>`TutorialInfo` 那四行。空实现 = 四行字全在。
        /// 判据（谁喂谁）：`TutorialSpecificInfo.Setup(tutorialNumber, deck)`——
        ///   `TutorialTitle` = `ToUpper(词条) + 关号` · `TutorialSubTitle` = `Loc(deck.loreKey + 后缀)` ·
        ///   `TutorialWarlordTitle` = `Format(词条, deck.GetWarlordName())` ·
        ///   `TutorialDescription` = `Loc(deck.loreKey)`（`TutorialSpecificInfo__Setup.c`）。
        /// 🔴 **能算的照原版算式算**（关号那行、督军名那行）· 词条那两行**本地没有** ⇒ 只有第 0 关有
        ///    prefab 出厂原文，其余给占位 + 出声。</summary>
        void BuildInfo(Transform root)
        {
            var info = MenuDraw.Node(root, "TutorialInfo", new PxRect(InfoL, InfoT, InfoR, InfoB));
            // 四条都是 `Left/Capline`（描述那条是 `Left/Top` + 折行）—— 逐条实读
            var t1 = MenuDraw.Text(info, new PxRect(ITiL, ITiT, ITiR, ITiB), StageTitle(StageIndex),
                                   Color.white, "TutorialTitle", 74.25f, QText);
            if (t1 != null) { t1.SetAutoFitBox(LayoutSpace.Px(ITiR - ITiL), LayoutSpace.Px(ITiB - ITiT), 10f, 74.25f, 12f); MenuDraw.AlignLeft(t1, new PxRect(ITiL, ITiT, ITiR, ITiB)); }
            var t2 = MenuDraw.Text(info, new PxRect(ISuL, ISuT, ISuR, ISuB), SubOf(StageIndex),
                                   Color.white, "TutorialSubTitle", 54.30f, QText);
            if (t2 != null) { t2.SetAutoFitBox(LayoutSpace.Px(ISuR - ISuL), LayoutSpace.Px(ISuB - ISuT), 10f, 54.30f, 12f); MenuDraw.AlignLeft(t2, new PxRect(ISuL, ISuT, ISuR, ISuB)); }
            var t3 = MenuDraw.Text(info, new PxRect(IWlL, IWlT, IWlR, IWlB), WarlordLine(StageIndex),
                                   Color.white, "TutorialWarlordTitle", 54.30f, QText);
            if (t3 != null) { t3.SetAutoFitBox(LayoutSpace.Px(IWlR - IWlL), LayoutSpace.Px(IWlB - IWlT), 10f, 54.30f, 12f); MenuDraw.AlignLeft(t3, new PxRect(IWlL, IWlT, IWlR, IWlB)); }
            // 描述那条：原版 `折行=1` + `Left/Top` ⇒ 给折行宽 + 顶对齐（其余三条都是单行 Capline）
            var t4 = MenuDraw.Text(info, new PxRect(IDsL, IDsT, IDsR, IDsB), DescOf(StageIndex),
                                   Color.white, "TutorialDescription", 40f, QText,
                                   LayoutSpace.Px(IDsR - IDsL));
            if (t4 != null)
            {
                t4.SetAutoFitBox(LayoutSpace.Px(IDsR - IDsL), LayoutSpace.Px(IDsB - IDsT), 10f, 40f, 12f);
                MenuDraw.SetVAlign(t4, Label.VAlign.Top, new PxRect(IDsL, IDsT, IDsR, IDsB));   // 原版 `Left/Top`
                MenuDraw.AlignLeft(t4, new PxRect(IDsL, IDsT, IDsR, IDsB));
            }
        }

        // ------------------------------------------------------------ 左列：6 关名单（可纵向滚）
        /// <summary>`Army Selector` → `Viewport`(RectMask2D) → `Filters`(VLG)。
        /// ⚠️ 原版 `Filters` 出厂 **0 高 0 子** —— 6 格全是 `ConfigureArmyFilterButtons` 里
        ///    `Instantiate(armySelectionButton, selectArmyAnchor)` 出来的（`TutorialModePopup__ConfigureArmyFilterButtons.c`）；
        ///    模板 = 独立 prefab `Tutorial Army Select Button`（根 589.75×200）。
        ///    我们照同一结构：容器按「6 格 + 5 个 spacing」长开，格逐个建（数量少，不做池化）。</summary>
        Transform _stageContent;
        MenuScroll _scroll;

        void BuildArmySelector(Transform root)
        {
            var sel = MenuDraw.Node(root, "Army Selector", new PxRect(ArmL, ArmT, ArmR, ArmB));
            // 原版 `Viewport` 与父件 **同矩形**（实读 `anchor (0,0)-(1,1)` + `sizeDelta (0,0)`）
            var vp = MenuDraw.Node(sel, "Viewport", new PxRect(ArmL, ArmT, ArmR, ArmB));
            // 🔴 **裁切状态挂在 `Viewport` 这一颗上**（= 原版那个 `RectMask2D`；形状照 A435 阶段 2 的迁移表：
            //    `ChatPanel.ChatTab/Viewport` / `LiveOpsEventWindow` 的 `Army Selector/Viewport` 同款）。
            //    ⚠️ **本窗的 `m_Padding` / `m_Softness` 没读到**（`menu_dump` 只打出字段名、不打值；
            //       本轮两次尝试（按组件 pid 反查宿主 GO）都没拿到）⇒ 按 **0 / 0（硬边）** 处理，
            //       并在报告里挂着待核。⛔ 别当成「读过、确认是 0」。
            var vc = vp.gameObject.AddComponent<ViewportClip>();
            vc.padding = Vector4.zero;
            vc.softness = Vector2Int.zero;

            float contentH = Rows.Length * ItemH + (Rows.Length - 1) * ArmGap;   // 6×200 + 5×7.31 = 1236.55
            _scroll = MenuScroll.TopAligned(new PxRect(ArmL, ArmT, ArmR, ArmB), contentH);
            _scroll.Owner = gameObject;
            _stageContent = MenuDraw.Node(vp, "Filters", new PxRect(ArmL, ArmT, ArmR, ArmT + contentH));
            _scroll.OnChanged = () => RebuildStageCells(_stageContent);
            PointerLayer.RegisterScroll(_scroll);
            RebuildStageCells(_stageContent);
        }

        void RebuildStageCells(Transform holder)
        {
            StageCells.Clear();
            for (int i = holder.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(holder.GetChild(i).gameObject);
            // VLG `align = 0`(UpperLeft) + pad 全 0 ⇒ 第 i 格顶 = 容器顶 + i×(200 + 7.31)
            for (int i = 0; i < Rows.Length; i++)
            {
                float y1 = ArmT + i * (ItemH + ArmGap);
                var rr = _scroll.Shift(new PxRect(ArmL, y1, ArmL + ItemW, y1 + ItemH));
                if (!MenuDraw.VisibleAbove(holder, rr, null)) continue;   // 整格在视口外 ⇒ 连节点一起不建
                StageCells.Add(BuildStageCell(holder, i, rr));
            }
        }

        /// <summary>一格 = 原版 `Tutorial Army Select Button` 的全树（五层图 + 三行字），
        /// 几何 = 模板的绝对矩形减模板根左上角（见 `Item*` 那组常量）。
        /// 🔴 点它 = 原版 `TutorialArmySelectionButton.OnArmySelected` → `selectionMenu.ChangeSelectedArmy(army)`
        ///    ⇒ 换选中关 + 重建名单 + 换右列那一整块。我们照同一件事：`SelectStage(i)`。</summary>
        Transform BuildStageCell(Transform holder, int i, PxRect cell)
        {
            var row = Rows[i];
            bool sel = (i == StageIndex);
            var node = MenuDraw.Node(holder, "Tutorial Army Select Button " + i, cell);

            // ① `Background (1)` —— 那条带阵营色的横幅（现读：**只有第 0 关有出厂原文的图**，见下）
            //    ✅ **2026-10-18 补图（`A865`）**：sprite `Tutorial Background` 512×144 已进
            //       `Resources/Art/ui_menu/Tutorial_Background.png`（由 `工具/import_original_art.py` 的
            //       `MENU_IMAGES` 导入；md5 与解包源 `Art/原版/0_mainmenu/` 那份逐字节同）。
            //    🔴 **常量必须填【落盘名】**：`CardArt.MenuUi(name)` **不做「空格→下划线」转换**
            //       ⇒ 原来写 `"Tutorial Background"`（带空格）会去找盘上不存在的名字、这一层**永远不画**
            //       （同族先例 = `ShopData` 那批，口径见 `import_original_art.py` 的 A544 注）。
            //       ⇒ 已改成 `"Tutorial_Background"`（**这一行才是真正让图出现的改动**）。
            //    ⚠️ 同族另两条老账（未收）：`BaseOfferPopup` 的 `40k_OfferBadge` ·
            //       `RankedRewardEventWindow` 的 `40k_UI_Banner BW`。
            var banner = Tex(ArtItemBanner);
            if (banner != null)
                MenuDraw.Rect(node, banner, InCell(cell, ItemBanner), "Background (1)", QArt, ItemBannerTint);
            else
                MenuDraw.Node(node, "Background (1)", InCell(cell, ItemBanner));

            // ② `Background` —— 圆形阵营底（选中与否都画，常态图）
            MenuDraw.Rect(node, Tex(ArtItemBack), InCell(cell, ItemBack), "Background", QArt1);
            // ③ `Highlight` —— 选中态那一圈（原版 **出厂 `m_IsActive = false`**，选中才 `SetActive(true)`）
            MenuDraw.Rect(node, Tex(ArtItemHL), InCell(cell, ItemHL), "Highlight", QArt2);
            var hl = node.Find("Highlight");
            if (hl != null) hl.gameObject.SetActive(sel);
            // ④ `BackgroundComplete` —— 完成高亮（原版脚本里那个 `completeHighlight` 字段 = 本件**推导**：
            //    它是这棵树上**唯一一件**「完成才亮」形的节点；⚠️ 字段指向的 pid 没读出来 ⇒ 这一条是**按形状认的**，
            //    与下面 `TutorialComplete` **成对** —— `Initialize` 里是**同一句 `SetActive(uVar3)` 关掉两颗**
            //    （`TutorialArmySelectionButton__Initialize.c`：`0x30`(completeHighlight) 与 `0x48`(complete)）。
            //    我们**没有教程进度存档**（见 `CompletedCount`）⇒ 两件都按「未完成」关着。
            //    ⚠️ `Sliced` 九宫 (20,0,20,0)，`m_Color (0,1,0.078,1)`
            MenuDraw.Nine(node, Tex(ArtItemBar), InCell(cell, ItemBar), ItemBarBorder, 48f, 18f,
                          QArt3, ItemBarTint, true, "BackgroundComplete");
            var bar = node.Find("BackgroundComplete");
            if (bar != null) bar.gameObject.SetActive(false);
            // ⑤ `Icon` —— 阵营徽记（原版 `sprite = 0`，运行时喂 `ArmyUtilities.GetArmyIcon(army)`；
            //    我们喂同一批阵营徽记 —— 与 `LiveOpsEventWindow.BuildArmySelector` 同一条处置）
            MenuDraw.Rect(node, Tex(DeckRuntime.FactionIcon(row.Fac)), InCell(cell, ItemIcon), "Icon", QArt4, null, true);

            // 三行字（fs 36 · auto[18,36] · hAlign Left）—— 文案见表头那两条说明
            var a = MenuDraw.Text(node, InCell(cell, ItemTi), StageTitle(i), Color.white, "TutorialTitle", 36f, QText);
            if (a != null) { a.SetAutoFitBox(LayoutSpace.Px(ItemTi.W), LayoutSpace.Px(ItemTi.H), 18f, 36f, 36f); MenuDraw.AlignLeft(a, InCell(cell, ItemTi)); }
            var b = MenuDraw.Text(node, InCell(cell, ItemSu), SubOf(i), Color.white, "TutorialSubTitle", 36f, QText);
            if (b != null) { b.SetAutoFitBox(LayoutSpace.Px(ItemSu.W), LayoutSpace.Px(ItemSu.H), 18f, 36f, 36f); MenuDraw.AlignLeft(b, InCell(cell, ItemSu)); }
            var c = MenuDraw.Text(node, InCell(cell, ItemDone), ItemDoneText, Color.white, "TutorialComplete", 36f, QText);
            if (c != null)
            {
                c.SetAutoFitBox(LayoutSpace.Px(ItemDone.W), LayoutSpace.Px(ItemDone.H), 18f, 36f, 36f);
                MenuDraw.AlignLeft(c, InCell(cell, ItemDone));
                // 🔴 **`Complete!` 与上面 `BackgroundComplete` 成对**：原版 `Initialize` 里是**同一句
                //    `SetActive(uVar3)`**（`uVar3` = 该关是否已完成）关掉两颗 —— 只关一件就是**偏离**。
                //    我们没有教程进度 ⇒ 恒关（同上）。
                c.gameObject.SetActive(false);
            }

            // 命中区：**不换图**（原版那颗 `EverguildButton` 是 `trans = 1` + `target = 0` ⇒ ColorTint 没有目标，
            // 悬停/按下**不改任何贴图**；选中态是上面那颗 `Highlight` 的 `SetActive`）⇒ `target`/`art` 全不传。
            int idx = i;
            MenuDraw.Hit(node, "Hit", cell, QHit, () => SelectStage(idx));
            return node;
        }

        /// <summary>`Completed Text`：原版 = `Format(词条, 已完成, 总数)`（`TutorialModePopup__Open.c`）。
        /// 🔴 **我们数不出「已完成」** —— 教程进度在原版走 **PlayFab 云脚本**
        /// （`PlayerDataManager.RecordTutorialPassed` / `UploadTutorialV2SaveData`），原版已关服、
        /// 本地也无存档字段 ⇒ 按 **0** 印（新号的真实值），并在文本里保持原版的格式 `Completed: n/6`。
        /// ⛔ **不编一个假进度**。</summary>
        void BuildCompleted(Transform root)
        {
            MenuDraw.Text(root, new PxRect(CompL, CompT, CompR, CompB),
                          "Completed: " + CompletedCount() + "/" + Rows.Length, Color.white,
                          "Completed Text", 54f, QText);
        }

        /// <summary>打过的关数。**恒 0** —— 判据见 `BuildCompleted` 上面那段（进度在服务端，本地无源）。</summary>
        public static int CompletedCount() { return 0; }

        /// <summary>`PlayTutorialButton` + `Button Text`。
        /// 原版链：`TutorialModePopup.BattleButtonOnClick` →
        ///   `MatchMakerManager.StartMatch(…, playMode: 4 /* PlayModes.Tutorial */, PlayerBattleData,
        ///    playerDeck: 选中那关的预组牌, enemyDeck: 0 (null), …, 5, …)`（`(iVar1>>0x1f & 2)+4`，
        ///   实参 `iVar1 ≥ 0` ⇒ **恒为 4**）。
        /// 🔴 **我们打不了**（缺口逐条 → 文件头那一节）⇒ 点了**如实出声**，⛔ 不静默、⛔ 不拿普通局冒充。</summary>
        void BuildPlayButton(Transform root)
        {
            var btn = MenuDraw.Node(root, "PlayTutorialButton", new PxRect(PlayL, PlayT, PlayR, PlayB));
            // `UI_Button_Mulligan` 410×124 —— 原版这一格是 **`Simple` + `preserveAspect = 1`** ⇒ `keepAspect`
            MenuDraw.Rect(btn, Tex("UI_Button_Mulligan"), new PxRect(PlayL, PlayT, PlayR, PlayB),
                          "Bg", QArt, null, true);
            // 原版 hAlign = **Center** ⇒ 不调 `Align*`
            MenuDraw.Text(btn, new PxRect(PlayTxL, PlayTxT, PlayTxR, PlayTxB), PlayText, Color.white,
                          "Button Text", 74.25f, QText);
            MenuDraw.Hit(btn, "PlayHit", new PxRect(PlayL, PlayT, PlayR, PlayB), QHit, () => PlayTutorial());
        }

        // ============================================================ 交互
        /// <summary>换选中关 —— 原版 `ChangeSelectedArmy(army)`：**只做两件事**
        /// （① 逐格 `Highlight.SetActive(是否是这一档)` ② 把右列那一整块重建）⇒ 我们照做。</summary>
        public void SelectStage(int i)
        {
            if (i < 0 || i >= Rows.Length) { Debug.LogWarning("[Tutorial] 关号越界：" + i); return; }
            if (i == StageIndex) return;                 // 原版 `if (param_2 != _selectedArmy)` 那一句
            StageIndex = i;
            if (_stageContent != null) RebuildStageCells(_stageContent);
            // 右列那两块**重建而不是原地改**（同 `LiveOpsEventWindow.RefreshDeckColumn` 的理由：
            // 立绘 / 关号 / 副标题 / 描述四样都可能整块变）
            var wl = transform.Find("Warlod Image");
            if (wl != null) RewardsWindow.DestroySafe(wl.gameObject);
            BuildWarlordImage(transform);
            var info = transform.Find("TutorialInfo");
            if (info != null) RewardsWindow.DestroySafe(info.gameObject);
            BuildInfo(transform);
            Debug.Log("[Tutorial] 选中第 " + (i + 1) + " 关：" + Rows[i].So + " / " + Rows[i].Deck);
        }

        /// <summary>点 `Play Tutorial` —— **原版这一步真开一局**
        /// （`TutorialModePopup.BattleButtonOnClick` → `MatchMakerManager.StartMatch(…, playMode: 4
        ///  /* PlayModes.Tutorial */, playerDeck: **选中那关的预组牌**, enemyDeck: null, …)`，
        ///  那个 `(iVar1 &gt;&gt; 0x1f &amp; 2) + 4` 在 `iVar1 ≥ 0` 时**恒为 4**）。
        ///
        /// ✅ **2026-10-17（B29）起真的开**：两条静态通道（**读一次就清**）+ 切战场场景。
        /// 触发者只传「**第几关**」——关卡数据 / 两副牌 / 执行器全在 `BattleDriver.BeginTutorial` 里现取。
        ///
        /// 🔴 **取不到数据时那条链会停在原地并出声**（`BeginTutorial` 三处 `LogError` + `return`）——
        ///    ⛔ **它不会退化成「一场普通 bot 局」**：那样玩家会拿到一局「没有教程的教程」。
        /// </summary>
        public void PlayTutorial()
        {
            var row = Rows[StageIndex];
            // 🔴 本窗**只负责「第几关」**（`tutorialIndex`，0..5 —— 原版 `DemoDeckInfoSO.TutorialIndex`）。
            //    两条通道都照 `_pendingPlayMode` / `PrebuiltDecks._pending` 的先例：跨场景、读一次就清。
            BattleDriver.SetPendingTutorialStage(StageIndex);
            BattleDriver.SetPendingPlayMode(GameMode.Tutorial);
            // 战场那一份照原版 `GetBattleArena(army)` 查表（§27 起 `BattleSceneNameFor` 恒为 `Battle`，
            // 「哪一场」由运行时按 `ArenaByArmy.SceneFor(faction)` 取 prefab）。
            var hero = HeroDef(StageIndex);
            string faction = hero != null ? hero.Faction : null;
            var scene = ArenaByArmy.BattleSceneNameFor(faction);
            Debug.Log("[Tutorial] 点 `Play Tutorial`（第 " + (StageIndex + 1) + " 关 `" + row.So + "` / `" + row.Deck
                    + "`）⇒ **真开局**：`tutorialIndex = " + StageIndex + "` 与 `GameMode.Tutorial(4)`"
                    + " 已放进静态通道 ⇒ 切 `" + scene + ".unity`"
                    + "（原版 `TutorialModePopup.BattleButtonOnClick` → `MatchMakerManager.StartMatch(…, 4, …)`）");
            if (Application.isBatchMode) { Debug.Log("[Tutorial] （批处理：不切场景，只记账）"); return; }
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
        }

        // ============================================================ 文案
        /// <summary>关号那一行 —— 原版算式 = `ToUpper(词条) + 关号`，出厂原文 `TUTORIAL 01`
        /// （`TutorialSpecificInfo.Setup` 与 `TutorialArmySelectionButton.Initialize` **同一句**；
        /// 两处都拼 `局部词条("Tutorial 0"?) + (index+1)`，prefab 上那三处出厂字 = `TUTORIAL 01` /
        /// `Tutorial 1` / `'TUTORIAL 01'`）。我们按**可见结果**印：`TUTORIAL 0n`（n = 1..6）。</summary>
        public static string StageTitle(int i) { return "TUTORIAL 0" + (i + 1); }

        /// <summary>副标题。原版 = `Loc(deck.loreLocalizationKey + 后缀)` —— **在远端词条表**。
        /// 🔴 **只有第 0 关有 prefab 出厂原文**（`The Basics`）；其余 5 关本地**查不到** ⇒ 给占位 + 出声。</summary>
        public static string SubOf(int i)
        {
            if (i == 0) return Stage1Subtitle;
            MissingTextOnce(i, "副标题");
            return TextMissing;
        }

        /// <summary>描述。原版 = `Loc(deck.loreLocalizationKey)` —— 同上，只有第 0 关有出厂原文。</summary>
        public static string DescOf(int i)
        {
            if (i == 0) return Stage1Description;
            MissingTextOnce(i, "描述");
            return TextMissing;
        }

        /// <summary>`Warlord: &lt;color=orange&gt;{督军}&lt;/color&gt;` —— **格式串就是 prefab 出厂原文**
        /// （`TutorialWarlordTitle.m_text = 'Warlord: &lt;color=orange&gt;Uriel Ventris&lt;/color&gt;'`），
        /// 名字取 `DemoDeckInfoSO.GetWarlordName()` = 督军卡自己的名字（见 `Rows` 那段的推导）。</summary>
        public static string WarlordLine(int i)
        {
            var d = HeroDef(i);
            string nm = d != null ? d.Name : "?";
            return "Warlord: <color=orange>" + nm + "</color>";
        }

        /// <summary>督军卡（`CollectionData.Card` = 引擎卡表）。取不到时出声（红线：不许静默）。</summary>
        public static CardDef HeroDef(int i)
        {
            if (i < 0 || i >= Rows.Length) return null;
            var d = CollectionData.Card(Rows[i].Hero);
            if (d == null && !_missingHero.Contains(Rows[i].Hero))
            {
                _missingHero.Add(Rows[i].Hero);
                Debug.LogWarning("[Tutorial] 卡表里找不到督军 " + Rows[i].Hero + "（第 " + (i + 1) + " 关 "
                               + Rows[i].So + "）—— 立绘与 `Warlord:` 那行会缺");
            }
            return d;
        }
        static readonly List<string> _missingHero = new List<string>();
        static readonly List<string> _missingText = new List<string>();

        static void MissingTextOnce(int i, string what)
        {
            if (_missingText.Contains(i + what)) return;
            _missingText.Add(i + what);
            Debug.Log("[Tutorial] 第 " + (i + 1) + " 关的**" + what + "文案本地查不到** —— 原版走远端词条表"
                    + "（`TutorialSpecificInfo.Setup` / `TutorialArmySelectionButton.Initialize` 里的 `Loc(...)`；"
                    + "原版客户端连 TextAsset 目录都没有）⇒ 印占位 `" + TextMissing + "`。"
                    + " 🔴 **不编文案**（铁律 11 第 ① 种：判据是空的）。");
        }

        // ============================================================ 工具
        /// <summary>取图 + **记账**（`MenuDraw.Rect/Nine` 取不到图就 `return null`、一声不吭 ⇒ 整层静默消失）。
        /// 走 `CardArt.MenuUi`（`Resources/Art/{ui_menu,ui_deck,ui}/`）。</summary>
        Texture2D Tex(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name)) MissingArt.Add(name);
            return t;
        }

        /// <summary>「相对格左上角」的偏移矩形 → 画布坐标。</summary>
        static PxRect InCell(PxRect cell, PxRect off)
        { return new PxRect(cell.x1 + off.x1, cell.y1 + off.y1, cell.x1 + off.x2, cell.y1 + off.y2); }
    }
}
