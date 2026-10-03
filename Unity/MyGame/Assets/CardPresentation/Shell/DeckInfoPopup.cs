// DeckInfoPopup.cs — 卡组线 `Deck info Popup`（原版 **`DeckInfoPopup`**）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0923/A1_外壳与弹窗.md` **§2**（逐节点表：路径 / sprite / rect / 锚点五元组 / 字号 / act / 组件）。
// 窗口参数（同表表头）：`type=1 Popup` · `windowsPlacement=15` · `closeOnESC=1`。
//
// 🔴 **它接上了一个原本用「再点一下」顶着的缺口**：收藏窗 Deck 页里，点一格卡组 = 选中，
//    原版是「选中 ⇒ 开这扇窗 ⇒ 窗里点 `Edit Deck` 才进编辑」。我们上一轮没建这扇窗，
//    在 `CollectionWindow.SelectDeck` 里**用「再点一下同一格 = 进编辑」顶着**（当时就出声了）。
//    ⇒ 现在这扇窗建起来了；那条顶替路径**保留**（自检还断它），但玩家有了原版的那条路。
//
// ============================ 结构（照 A1 §2 抄，逐条有出处）============================
//   · `Menu Dark Background` 色 **(0,0,0,.773)**（原版 rect 比屏幕大，是为了盖住任何画幅）
//   · `Generic Window Red Background Big` = **`UI_Deck_Information_Back`**（Sliced，border 42/363/655/81）
//   · `Warlord Image`（**原版 sprite=0、运行时喂**）⇒ 我们用督军立绘
//   · `Deck Details` → `Game Mode Separator`（`40k_Generic Smooth line`，色 (.42,.157,.137,1)）
//     + `Army Icon` + `Deck Name`（fs44.5 居中）+ `Warlord Name`（fs40 居中）
//   · `Buttons`（HLG spacing **36** align **MiddleRight**）→ 三个 `UI_Button_Mulligan` **324.5×80.1**
//   · `Deck Options`（HLG spacing **−50** align MiddleRight **reverse**）→ 五个圆钮 **74.386×75.605**
//   · `Info Panel` = `UI_Deck_Information_submenu_Back`（Sliced，border 18）→ `Deck List`
//     （GridLayoutGroup **cell 360×58 · spacing 11/4.5 · pad 15/0/15/0**）
//   · `Generic Close Button Orange` = 圆钮 + `40k_general_bt_yellow_close`
//
// ---- 没建的（出声，不静默）----
//   ✅ **2026-10-03（§三第29条 A10）四件都补上了** —— 原来这四条里**两条的记录本身就是过期的**：
//   · `Game Mode Icon` —— ✅ **画上了**（两张图 `40k_gamemode_icon_{classic,skirmish}` **本来就在工程里**，
//     原来记的「本地没有那两张图」是**过期**的；按卡组的 `gameMode`（0 经典 / 13 遭遇）选图）
//   · `Deck Info` 抽屉（费用曲线 / 卡背）—— ✅ **建了**（`Switch Deck Info` 真的切两个抽屉；
//     逐值 → 本文件 `BuildDeckInfoDrawer` 的注释）。⚠️ `Lore Text` **仍不建**：原版出厂 `act=N` +
//     我们引擎**没有 lore 字段**（同 `CardDetailPopup` 那条老账）
//   · `Share` / `Share On Chat` —— ✅ **接了**：原版走平台/服务端，我们**给卡组串**（`DeckLibrary.ExportString`）
//     + 如实说明聊天那条发不出去（`ChatPanel` 自己就写着「没有服务器」）
//   · `Practice Deck` —— ✅ **接了**（选中/开练习窗）。🔴 **但有一层没复刻**：原版是
//     `SelectPracticeOpponentDeck` ⇒ 这一副当**【对手】**卡组（`enemyDeck = 刚选中的那副`，判据 →
//     `资料/预组卡组_原版规格.md` §五之二 第 4 行）；我们的练习窗只认**玩家自己**的卡组
//     ⇒ **已记进待办**（`项目任务.md` §三第29条 A10 的尾巴），别当已复刻。
//
// 🔴 **2026-10-03 就地订正（铁律 5）—— 上面那一整条「这一副是【对手】卡组」是【读反了】**：
//   · **原文**：「`SelectPracticeOpponentDeck` ⇒ 这一副当【对手】卡组（`enemyDeck = 刚选中的那副`）」——
//     这句话**自己跟自己矛盾**：若「刚选中的那副」是 `enemyDeck`，那这一副就只能是 `playerDeck`。
//   · **实况**：`MatchMakerManager.StartMatch(PlayModes, PlayerBattleData, CardDeck playerDeck, CardDeck enemyDeck, …)`
//     （形参名 = 签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/Everguild/MatchMakerManager.cs:136`）。
//     `DeckInfoPopup__StartPracticeMatch.c`：第 4 个实参 = **本窗 `context.Deck`**（= 这一副）⇒ **`playerDeck`**；
//     第 5 个实参 = **选卡组窗回调回来的那一副**（`param_2`）⇒ **`enemyDeck`**。
//     三条旁证同向：`CreatePlayerBattleData(这一副)` · `CheckHiddenCardsInDeck(这一副)` ·
//     模式号 `0xc = PlayModes.OwnDeckTraining`「**用自己**的卡组训练」。
//   · **正确语义**：点 `Practice Deck` ⇒ **这一副是【我】的卡组** ⇒ 开选卡组窗挑**对手**那一副
//     （所以那扇窗默认落在「预组卡组」页 —— 对手多半是预组）⇒ 选定即开打。
//   · **错因**：把方法名 `SelectPracticeOpponentDeck`（= 「挑**练习对手**的卡组」）读成了「把这一副当对手」，
//     而没去核 `StartMatch` 的形参名。**本轮已按正确语义实现**（见 `SelectPracticeOpponentDeck` / `StartPracticeMatch`）。
//   · 让上一版以为「做不到」的那层理由（「我们的练习窗只认玩家自己的卡组，没有『指定对手卡组』这个入口」）
//     **仍然成立**，只是**方向相反**：缺的入口是「指定**对手**」，补在 `PracticeModePopup.OpponentDeck` +
//     「本局对手」通道上；✅ **2026-10-04 收口**：**通道的读点已经写上了** —— 在 `Battle/BattleDriver.cs` 的
//     `BeginFromDeckLibrary()`（`foeDeck: PracticeModePopup.TakePendingOpponentDeck()`）。
//     ⚠️ **原来这句写的是「读点还没写 ⇒ 那条缺口照旧开着」—— 已过期**（审查 2026-10-04 抓到的），留着更正痕。
//
// ---- 🆕 2026-10-03 顺手查出、2026-10-04（A31）**已实现**：这扇窗是「按 state 显示不同按钮」的 ----
//   ✅ **判据表已落成代码**：`DeckInfoPopup.State`（`DeckInfoState{Edit0,Import1,View2,Practice3}`）+
//     `SelectButtonProvided` + `IsPlayerDeck`，逐颗显隐在 `ApplyStateVisibility()`（**只有这一份实现**）。
//     逐颗规则与出处 → 下面那张表（`DeckInfoControls__Initialize.c` 的 SetActive :60-112）。
//   🔴 **2026-10-04 补查实的两条（表里原来没有）**：
//     · **`isPlayerDeck` = `InventoryManager.HasItem(context.Deck)`**（「这副是我的库存里那副」）——
//       判据原文 `DeckInfoPopup__Open.c:43-48`（`HasItem(Instance, *(context+0x10)=Deck, …)` 之后原样传进
//       `DeckInfoControls__Initialize(…, uVar4, 0)`）。**我们恒 true**（三处调用点拿的都是玩家自己的卡组，
//       我们没有库存/拥有度系统）—— 这条**从「查不到」变成「查到了」**，记一笔。
//     · **`Select Deck` 在原版 6 个调用点【全都传 null】⇒ 本 build 里这颗钮从不出现** ——
//       逐处实读 `DeckInfoContext__ctor(this, deck, state, selectButton, checkOwnership, editButtonAction)`
//       的第 3 个实参（`DeckCollectionTab` / `DeckDrawer` / `DeckGeneralInfoDemo` / `RankedEventWindow` /
//       `RankedDeckSelector` / `ChatMessageUI` 六处**全是 0**），且 `DeckInfoContext__set_SelectButton`
//       **全库无调用者**。⇒ 我们原来**恒画**它 = 真偏离，现已按判据默认关（`SelectButtonProvided = false`）。
//   ⚠️ **仍欠一件（出声，别当已复刻）**：
//     **`SoftDisable(!CanImportDeck(popup, context.Deck))` 的「变灰但点得动」观感没做** ——
//        判据我们算得出（`CanImportDeck` 只认 `CardDeck.CustomGameModeEvent`，我们**没有任何**那种卡组
//        ⇒ 恒 false，见 `DeckInfoPopup__CanImportDeck.c:33-35`），但 `WindowButton` 没有 disabled 态
//        ⇒ 如实记着（`NoteUnimplementedLayers()` 每次显隐都会出声）。**这一件归 §三第29条 A65②。**
//     ✅ **2026-10-04 就地订正**：这里原来还写着「⚠️ 仍欠两件 …① `PracticeModePopup.ShowDeckContent()`
//        还没把它那一态（View/2）传进来」—— **那一行已经接上了**（**A65①**：
//        `Shell/PracticeModePopup.cs` 的 `ShowDeckContent()` 现在传 `DeckInfoState.View`）。
//        错因：那一轮那是**白名单外**的文件，只记不写；本批拿到那个文件就顺手接完了。**留着更正痕。**
//   · 入口对照（原版 6 个 `DeckInfoContext` 调用点、我们 3 个）：`DeckCollectionTab`→0 ·
//      `DeckDrawer`→2 · `DeckGeneralInfoDemo.CardInDeckInfoButtonOnClick`→2 ·
//      `RankedEventWindow.ViewDeckButtonClick`→0 · `RankedDeckSelector.OnViewDeckButtonClick`→0 ·
//      `ChatMessageUI.OnMessageClicked`→1；**没有一处用 `Practice = 3`**（和 `PlayerItem.IsHidden()` 一样，
//      这个 build 里是死档）。**我们的**：`CollectionWindow.OpenDeckInfo`→0 · `LiveOpsEventWindow.OpenDeckInfo`→0 ·
//      `PracticeModePopup.ShowDeckContent`→**2**（✅ **2026-10-04 A65① 已接** —— 不再是「该给」）。
//   · 🆕 同批**顺手实读到**的三条（原版 `Initialize` 里接线那一段，都不属于「显隐」）——
//     ✅ **2026-10-04（A65④）三条都接上了**，实现只有一处：`ApplyControlStates()`。
//     ① `Practice Deck` 的 `interactable = DeckUtility.ValidateDeck(context.Deck)`（`:201-207`）；
//     ② `Switch Deck Info` 建好即 `Toggle.SetIsOnWithoutNotify(true)`（`:210-211`）；
//     ③ `deleteButton.interactable = (卡组数 > 1)` · `duplicateButton.interactable = (卡组数 < 上限)`
//        （`:229-243`，两处 `Selectable.set_interactable`），而 `selectButton` 的**文案**是从
//        `context.SelectButton.Text` 那颗钮上**抄过来的**（`:213-224`：`*(context+0x20)` 的 `+0x10` → TMP 文本）。
//        🔴 **「上限」= `GameStaticData.totalCustomDecks` = 114**（查法见 `MaxCustomDecks` 那段注释）。
//   —— 下面这段是**怎么把偏移对上节点名的**（A31 的判据来源，留着省得再查一遍）——
//   ✅ **2026-10-04 调度台把「哪个 offset 是哪颗钮」查清了**：
//      宿主节点 = **`Deck Options`**（挂 `DeckInfoControls`）。
//      字段序 = 签名桩 `DeckInfoControls.cs` 的 8 个 `[SerializeField]` **按声明序** ⇒ 偏移 `+0x20…+0x58`，
//      紧跟其后 `context`=`+0x60`、`popup`=`+0x68` —— 与反编译里那两句赋值（`param_1+0x60 = param_3` /
//      `+0x68 = param_2`）**逐一对上**；`DeckInfoContext` 那边同理（`Deck`=`+0x10` · **`State`=`+0x18`** ·
//      **`SelectButton`=`+0x20`** · `CheckOwnership`=`+0x28` · `EditButtonAction`=`+0x30`，出处 `DeckInfoContext.cs`）。
//      节点名 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-2318415566341371480.json` 的 8 个 PPtr
//      **按 pid 反查 GameObject**（不是猜的）：
//        | 偏移 | 字段 | 原版节点名 | 显示规则（`Initialize(popup, context, isPlayerDeck)`） |
//        |---|---|---|---|
//        | +0x20 | `editButton`         | **`Edit Deck`**               | `state < 2`；`SoftDisable(!CanImportDeck(popup, context.Deck))`；文本 `state==1` 换一条本地化键 |
//        | +0x28 | `selectButton`       | **`Select Deck`**             | `context.SelectButton != null` |
//        | +0x30 | `shareButton`        | **`Share Button`**            | `state == 0 && isPlayerDeck` |
//        | +0x38 | `shareOnChatButton`  | **`Share On Chat`**           | 同上 |
//        | +0x40 | `deleteButton`       | **`Delete Button`**           | 同上 |
//        | +0x48 | `duplicateButton`    | **`Duplicate Button`**        | 同上 |
//        | +0x50 | `practiceButton`     | **`Practice Deck`**           | `isPlayerDeck && (state == 0 ‖ state == 2)`（`state==3` 时监听目标换成自己） |
//        | +0x58 | `toggleDrawerButton` | **`Switch Deck Info Button`** | `EverguildToggle`（`onValueChanged` 接监听；**没看到 SetActive ⇒ 常显**） |
//      出处：`DeckInfoControls__Initialize.c`（SetActive :60-112 · 事件接线 :120-200）。
//      ⚠️ 表里那个 `isPlayerDeck`（签名桩 `DeckInfoControls.cs` 的 `Initialize(DeckInfoPopup, DeckInfoContext, bool)`
//      第 3 个形参）== 上面那条 `InventoryManager.HasItem(context.Deck)` 的返回值 —— **同一个布尔**，别当成两个条件。
//   · 🆕 同批补的：`Warlord Image` 那层的 `EverguildButton` 点击（原版开卡详情窗）—— 详情窗 09-24 就建好了，
//     原来那句「那扇窗还没建」也是过期的。
//   · 🆕 同批**就地订正两处真缺陷**（fresh dump 抓出来的，见常量那段）：`Deck Details` 整块原来用的是
//     **布局组跑之前的模板位**（`Army Icon` 画到容器外）· `Deck Options` 五颗的**左右顺序反了**。
//
// ---- 🔴 一处**原版数据本身就重叠**（照画，但出声）----
//   `Deck Details` 那两个文本的 rect **伸进 `Info Panel` 里**：
//   `Deck Name` y 170.05→224.55、`Warlord Name` y 222.30→272.30（x 468.10→955.10），
//   而 `Info Panel` 是 **(659,218.10)→(1799,868.10)**，`Deck List` 的第 1 行就从面板顶边起排。
//   ⇒ 重叠区 x∈[659,955]、y∈[218,272]，**两段字与第一行卡名压在一起**（截图 `05_收藏_DeckInfo弹窗.png`）。
//   · 我们**照 rect 画**（没挪），但把面板画在文本**下面**（原版兄弟序是 `Deck Details` 在前、
//     `Info Panel` 在后 ⇒ 面板会**盖掉**督军名右半）—— 盖掉更难解释，所以反过来。
//   ⚠️ **没跑实况核过**（原版这时长什么样，要么联网跑客户端、要么等 P2P 那条线，见 `项目任务.md` §三 第 15 条）。
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>`DeckInfoPopup : GameWindow` —— 收藏窗 Deck 页点一格卡组开的那扇窗。</summary>
    public class DeckInfoPopup : GameWindow
    {
        // 层：**必须高于收藏窗那一整片**（它最高到 `CollectionWindow.QFltHit = 3043`）。
        // ⚠️ 但**低于** `PromptPopup`（3140+）—— 提示窗要能压在这扇窗之上。
        public const int QDI = 3120, QDIRow = 3121, QDIText = 3122, QDIHit = 3123;

        // ---- 几何（A1 §2 逐条）----
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.773f);
        public const float RedL = 134.50f, RedT = 82f, RedR = 1839.50f, RedB = 1032f;
        public const float WarlordL = -108.98f, WarlordT = -33.99f, WarlordR = 999.02f, WarlordB = 1074f;
        public const float SepL = 759.0f, SepT = 106.7f, SepR = 767.9f, SepB = 216.7f;
        public const float DdIconL = 767.9f, DdIconT = 106.7f, DdIconR = 867.9f, DdIconB = 216.7f;
        public const float DdNameL = 872.9f, DdNameT = 114.4f, DdNameR = 1358.5f, DdNameB = 168.9f;
        public const float DdWlL = 872.9f, DdWlT = 166.7f, DdWlR = 1359.9f, DdWlB = 216.7f;
        // 🔴 **2026-10-03 就地订正（A10）**：上面四个常量原来抄的是 `A1 §2` 那张表的值
        //   （`SepL=659 / DdIconL=363.10 / DdNameL=468.10 / DdWlL=468.10`）—— **那是布局组跑【之前】的模板位**。
        //   实据：`python 工具/menu_dump.py bundle_menus_assets_all "Deck info Popup" --depth 4` 末尾那行
        //   「⚠️ 布局组（子节点位置**由布局算**，上面已是**布局跑之后**的值）：Deck Details → HorizontalLayoutGroup [ok]」，
        //   跑后 `Game Mode Icon` **659.0,106.7→759.0,216.7** · `Separator` **759.0..767.9** ·
        //   `Army Icon` **767.9..867.9** · `Deck Name` **872.9,114.4→1358.5,168.9** · `Warlord Name` **872.9,166.7→1359.9,216.7**
        //   （三者首尾相接 = 布局真的跑过）。**旧值把整块画到了容器左边之外**（`Army Icon` 363.10 落在 `Deck Details` 659..1329.8 之外）。
        //   ⚠️ 同批订正的还有两处**对齐**：`Deck Name` 是 **Left/Bottom**、`Warlord Name` 是 **Left/Middle**（原来都按 Center 画）。
        /// <summary>`Buttons` 行：三个 **324.5×80.1**、spacing **36**、右对齐到 **1770.70**、y **885.81**。</summary>
        public const float BtnL = 725.20f, BtnT = 885.81f, BtnW = 324.5f, BtnH = 80.1f, BtnGap = 36f;
        /// <summary>`Deck Options` 行：五个圆钮、spacing **−50**、右对齐到 **1783.62**、y **130.20**。</summary>
        public const float OptR = 1783.62f, OptT = 130.20f, OptW = 74.386f, OptH = 75.605f, OptStep = OptW - 50f;
        /// <summary>圆钮里 `Background`/`Icon` 那两层的上下边（A1 §2：1790.96,71.18→1847.82,129.30）。</summary>
        public const float OptIconT = 71.18f, OptIconH = 58.12f, OptIconW = 56.86f;
        public const float PanelL = 659f, PanelT = 218.10f, PanelR = 1799f, PanelB = 868.10f;
        public const float RowL = PanelL + 15f, RowT = PanelT, RowW = 360f, RowH = 58f, RowGapX = 11f, RowGapY = 4.5f;
        public const float CloseL = 1782.81f, CloseT = 63.20f, CloseR = 1857.19f, CloseB = 138.80f;

        static readonly Vector4 RedBorder = new Vector4(42f, 363f, 655f, 81f);
        const float RedTexW = 1100f, RedTexH = 701f;
        static readonly Vector4 PanelBorder = new Vector4(18f, 18f, 18f, 18f);
        const float PanelTexW = 69f, PanelTexH = 63f;

        // ============================================================ 🆕 2026-10-03（§三第29条 A10）
        // 补齐的四件（**逐值来自 2026-10-03 的 fresh dump**）：
        //   `python 工具/menu_dump.py bundle_menus_assets_all "Deck info Popup" --depth 8`
        // 🔴 **A1 §2 那张表里 `Game Mode Icon` 等几个给的是【布局组跑之前的模板位】**（y 差 55.6px）
        //    —— 那正是铁律 5·c 说的「一个值 ≠ 全部情况」；下面这几个一律按 **fresh dump 的跑后值**。

        /// <summary>`Deck Details/Game Mode Icon`：跑后 **659.0,106.7 → 759.0,216.7**（100×110 · `preserveAspect`）。
        /// 原版 `sprite = 0`，运行期按 `gameModeDeckIcon` 喂 —— 图在工程里（`40k_gamemode_icon_{classic,skirmish}`）。</summary>
        public const float DdGmL = 659.0f, DdGmT = 106.7f, DdGmR = 759.0f, DdGmB = 216.7f;
        /// <summary>`Deck Info/Deck Information Cost/balance text`：440 宽那条标题（原版是**葡语占位** `Cartas / Coste`）。</summary>
        public const float DiHeadL = 740.0f, DiHeadT = 305.6f, DiHeadR = 1213.8f, DiHeadB = 365.6f;
        /// <summary>`Deck Information cost drawer`：rect **160×200** 但 **`localScale = 1.8`** ⇒ 真画出来 **288×360**，
        /// 中心 **(969.40, 558.80)**。里面 `Content` 是 VLG（spacing 3.43）、**9 行**（费用 0..8）。</summary>
        public const float DiDrawerCX = 969.40f, DiDrawerCY = 558.80f, DiDrawerScl = 1.8f;
        public const float DiRowW = 223.59f, DiRowH = 18.91f, DiRowStep = 22.295f;
        public const int DiRowCount = 9;
        /// <summary>`Cardback`：rect **210×305** 但 **`localScale = 1.85`** ⇒ 真画 **388.5 × 564.25**，中心 **(1466, 552.6)**。
        /// ⚠️ 原版是**两层**（`Cardback` + 子 `Cardback Front` 209.56×302.33，子件中心比父**偏 (+11.2, −4.9)**、
        /// 再乘 1.85）—— 我们只画父那一块（同一张图，画两层只是重一遍）。</summary>
        public const float DiCbCX = 1466f, DiCbCY = 552.6f, DiCbScl = 1.85f;
        public const float DiCbW = 210f, DiCbH = 305f;

        /// <summary>现在展示的是哪个抽屉：`false` = `Deck List`（出厂）· `true` = `Deck Info`。</summary>
        public bool InfoDrawerShown { get; private set; }
        /// <summary>`Deck Info` 抽屉那 9 行的读数（自检用）。</summary>
        public readonly List<int> CostRowCounts = new List<int>();

        /// <summary>卡组列表列数 = `floor((1140 − 15 − 15 + 11) ÷ (360 + 11))` = **3**（照 GridLayoutGroup 那套算）。</summary>
        public static int ListCols
        {
            get { return Mathf.Max(1, Mathf.FloorToInt((PanelR - PanelL - 30f + RowGapX) / (RowW + RowGapX))); }
        }

        // ============================================================ 🆕 2026-10-04（§三第29条 A31）
        // **按 state 显隐**（原文/判据 → 文件头那张表；这里只落实现）。
        //
        // 逐颗（判据 = `DeckInfoControls__Initialize.c` 的 SetActive :60-112）：
        //   `Edit Deck`            ← `state < 2`
        //   `Select Deck`          ← `context.SelectButton != null`
        //   `Share`/`Share On Chat`/`Delete`/`Duplicate` ← `state == 0 && isPlayerDeck`（**四颗同一条**）
        //   `Practice Deck`        ← `isPlayerDeck && (state == 0 ‖ state == 2)`
        //   `Switch Deck Info`     ← 常显（原版那件是 `EverguildToggle`，没看到 SetActive）
        //
        // 🔴 **`isPlayerDeck` 到底是什么**（2026-10-04 查实 —— 文件头那张表原来只写了「第 4 个实参」）：
        //   原版 = **`InventoryManager.HasItem(context.Deck)`**，判据原文 `DeckInfoPopup__Open.c:43-48`：
        //     `lVar6 = SingletonBehaviour<InventoryManager>.get_Instance();`
        //     `uVar4 = InventoryManager__HasItem(lVar6, *(context + 0x10) /* = context.Deck */, …);`
        //     `DeckInfoControls__Initialize(popup[0x19], popup, context, uVar4, 0);`
        //   ⇒ 语义 = 「**这副卡组在我的库存里**」（自己的 vs 别人的 —— 聊天里别人分享的那副就不是）。
        //   **我们恒真**：三处调用点拿的都是 `CollectionData` 里**玩家自己**那副（我们没有库存/拥有度系统）。
        //   ⚠️ 别把它和 `state == 0` 混成一件事 —— 原版那两个是**并列**的与条件（表里写过一次，这里再钉一遍）。

        /// <summary>原版 `DeckInfoContext.DeckInfoStates`（签名桩 `DeckInfoContext.cs:5`）—— 这扇窗的四态。</summary>
        public enum DeckInfoState { Edit = 0, Import = 1, View = 2, Practice = 3 }

        /// <summary>这一扇是哪一态（原版 `context.state`）—— **决定 8 颗钮的显隐**（见上面那张表）。
        /// 我们的调用点对照：`CollectionWindow.OpenDeckInfo` → `Edit`（原版 `DeckCollectionTab.OnItemSelected`，
        /// state 0）· `LiveOpsEventWindow.OpenDeckInfo` → `Edit`（原版 `RankedEventWindow.ViewDeckButtonClick`，
        /// state 0）· `PracticeModePopup.ShowDeckContent` → **该给 `View`**（原版
        /// `DeckGeneralInfoDemo.CardInDeckInfoButtonOnClick` = `DeckInfoContext(deck, 2, …)`）。
        /// ⛔ **原版一处也没用 `Practice = 3`**（和 `PlayerItem.IsHidden()` 一样是死档）。</summary>
        public DeckInfoState State = DeckInfoState.Edit;

        /// <summary>原版 `context.SelectButton != null`：**给了才显示 `Select Deck`**。
        /// 🔴 **2026-10-04 查实：原版 6 个调用点【全都传 null】** —— 逐处实读
        /// `DeckInfoContext__ctor(this, deck, state, selectButton, checkOwnership, editButtonAction)` 的第 3 个实参：
        /// `DeckCollectionTab`(0) · `DeckDrawer`(0) · `DeckGeneralInfoDemo`(0) · `RankedEventWindow`(0) ·
        /// `RankedDeckSelector`(0) · `ChatMessageUI`(0)；而且 `DeckInfoContext__set_SelectButton` **全库无调用者**
        /// ⇒ **本 build 里这颗钮从不出现**。我们原来恒画它 = **真偏离**（已按判据改：默认关）。</summary>
        public bool SelectButtonProvided;

        /// <summary>原版 `Initialize(…, bool isPlayerDeck)` 的第 4 个实参（语义见上面那段）。
        /// 我们三处调用点都是「玩家自己的卡组」⇒ 恒 true（**如实标：我们没有库存系统**）。</summary>
        public bool IsPlayerDeck = true;

        // ============================================================ 🆕 2026-10-04（§三第29条 A65④）
        // **原版 `DeckInfoControls.Initialize` 末尾那三条「接线层」**（都不属于显隐）——
        // 逐条判据、偏移怎么对上节点名的，全在文件头那张表与这段注释里；实现只有 `ApplyControlStates()`。
        //
        // 🔴 **先说清一件事：这一段在本 build 里是【活的】，不是死档。**
        //    反编译里这三条长在 `if (*(longlong *)(param_1 + 0x28) != 0)` 这条守卫里
        //    （`DeckInfoControls__Initialize.c:190`，`:212` 又写了一遍）——`param_1 + 0x28` = **`selectButton`
        //    那个 `[SerializeField]` 组件引用**（字段序见文件头那张偏移表），它**挂在 prefab 上** ⇒ **恒非空**
        //    （`SetActive(false)` 不会把引用变 null）。别把这个守卫读成 `context.SelectButton != null`
        //    （那个布尔只决定这颗钮**显不显示**，见 `ApplyStateVisibility`）—— 两者同名不同物，
        //    这正是 `资料/全量反编译复核_靠推断的清单.md` 里那类「看着像死档」的坑。

        /// <summary>`Delete Button` 的 `interactable`（原版 `:229-232`：`1 < 卡组数`）。
        /// ⇒ 只剩一套时不给你删（`CollectionData.DeleteDeck` 本来也拒，这条把它摆到按钮这一层）。</summary>
        public bool DeleteInteractable { get; private set; }
        /// <summary>`Duplicate Button` 的 `interactable`（原版 `:239-243`：`卡组数 < 上限`）。</summary>
        public bool DuplicateInteractable { get; private set; }
        /// <summary>`Practice Deck` 的 `interactable`（原版 `:201-207`：`DeckUtility.ValidateDeck(context.Deck)` ⇒
        /// 我们这一侧的同一份判据 = `RuleEngine.DeckRules.Validate`，与遭遇窗那条链**共用一处实现**）。</summary>
        public bool PracticeDeckInteractable { get; private set; }
        /// <summary>`Switch Deck Info` 那颗 **toggle 的 `isOn`**（原版 `:210-211` 的
        /// `Toggle.SetIsOnWithoutNotify(true)` ⇒ 出厂 **ON**，而且**不发通知**⇒ 不切抽屉）。
        /// 🔴 **我们只忠实记录这个状态本身**：原版出厂就是 `isOn = true`，而 `SetContent` 摆的是
        /// **`Deck List`** 那一面（`generalInfoContainer.SetActive(true)` + `cardsInDeckPanel.SetActive(false)`）
        /// ⇒ **`isOn` 与「哪一面在前」的对应关系，本地判据不足**（`ToggleDrawers(bool)` 只看得出来
        /// 「回调参数为真时 b0 亮」，而 `b0`/`a8` 与两个抽屉的对应没查）⇒ ⛔ **别编一个映射**，
        /// 断言只钉「出厂 ON」这一条原版实事。</summary>
        public bool DrawerToggleIsOn { get; private set; }
        /// <summary>卡组数上限 —— 原版那一条读的是 **`GameStaticData.totalCustomDecks`**：
        /// 反编译 `DeckInfoControls__Initialize.c:240-243` = `iVar1 < *(int *)(GameStaticData_StaticFields + 0x250)`；
        /// `dump.cs:119558` 的字段偏移正是 **`0x250`**（`public static int totalCustomDecks; // 0x250`）；
        /// 值 = **114** —— `GameStaticData__.cctor.c:308` 的 `*(undefined4 *)(… + 0x250) = 0x72;`。
        /// 邻居逐条对得上（不是读串位）：`+0x248` = `maxItemsToShowInChat` = `0x96` = 150 ·
        /// `+0x254` = `maxTranslateTaps` = `5` · `+0x25c` = 0x63 = 99。
        /// ⚠️ 我们**没有**服务端覆盖它那条路 ⇒ 恒 114；`GameStaticData` 里**没有**别的调用点写过它。</summary>
        public const int MaxCustomDecks = 114;
        /// <summary>`Select Deck` 那颗钮的**文案**（原版 `:216-224`：从 `context.SelectButton.Text` 抄过来；
        /// `context+0x20` 为 0 时抄的是空串）。⚠️ 原版 6 个调用点全传 null ⇒ 这条链在本 build 里从不触发，
        /// 我们把它照原样摆着（给 `SelectButtonProvided = true` 那一档用）。</summary>
        public string SelectButtonText;

        /// <summary>一颗钮「要一起显隐的那几个节点」。🔴 **底 / 字 / 点击区是【兄弟】、不是一个组的子件**
        /// （它们各自按绝对 px 摆，见 `Hit` 那段注释：父是 `Buttons`/`Deck Options` 容器）
        /// ⇒ 显隐只能逐颗点名，不能 `SetActive` 一个父节点。</summary>
        readonly Dictionary<string, List<GameObject>> _parts = new Dictionary<string, List<GameObject>>();

        /// <summary>登记一颗钮的组成节点（`Build()` 里建的时候调）。</summary>
        void Track(string key, params Component[] parts)
        {
            var list = new List<GameObject>();
            for (int i = 0; i < parts.Length; i++) if (parts[i] != null) list.Add(parts[i].gameObject);
            _parts[key] = list;
        }

        /// <summary>显隐一颗钮（**自检按名字查得到它开没开** —— `Transform.Find` 找得到关着的节点）。
        /// ⚠️ 档位名拼错 = **静默少一颗** ⇒ 没登记过的名字在这里出声。</summary>
        public void ShowItem(string key, bool on)
        {
            List<GameObject> l;
            if (!_parts.TryGetValue(key, out l))
            { Debug.LogWarning("[DeckInfo] 没有登记过这颗钮：「" + key + "」（不许静默）"); return; }
            for (int i = 0; i < l.Count; i++) if (l[i] != null) l[i].SetActive(on);
        }

        /// <summary>这颗钮现在露着没有（自检用；读**第一个**组成节点的 `activeSelf`）。</summary>
        public bool IsItemShown(string key)
        {
            List<GameObject> l;
            return _parts.TryGetValue(key, out l) && l.Count > 0 && l[0] != null && l[0].activeSelf;
        }

        /// <summary>按 `State` / `SelectButtonProvided` / `IsPlayerDeck` 摆 8 颗钮的显隐。
        /// **只有这一份实现** —— `Build()` 末尾调一次；自检改完 `State` 再调它一次就能复验。</summary>
        public void ApplyStateVisibility()
        {
            bool mine = IsPlayerDeck;
            int st = (int)State;
            ShowItem("Btn:Edit Deck", st < 2);                                    // 原版 :76
            ShowItem("Btn:Select Deck", SelectButtonProvided);                    // 原版 :~100（`context.SelectButton != null`）
            ShowItem("Opt:Share", st == 0 && mine);                               // 原版 :60-72（四颗同一条）
            ShowItem("Opt:Share On Chat", st == 0 && mine);
            ShowItem("Opt:Delete", st == 0 && mine);
            ShowItem("Opt:Duplicate", st == 0 && mine);
            ShowItem("Btn:Practice Deck", mine && (st == 0 || st == 2));          // 原版 :105-112
            ShowItem("Opt:Switch Deck Info", true);                               // 原版没看到 SetActive ⇒ 常显

            // 出声（别静默）：这一窗现在是哪一态、藏了哪几颗、以及判据里**我们做不到**的那两处
            var hidden = new List<string>();
            string[] all = { "Btn:Edit Deck", "Btn:Select Deck", "Opt:Share", "Opt:Share On Chat",
                             "Opt:Delete", "Opt:Duplicate", "Btn:Practice Deck", "Opt:Switch Deck Info" };
            foreach (var k in all) if (!IsItemShown(k)) hidden.Add(k.Substring(k.IndexOf(':') + 1));
            Debug.Log("[DeckInfo] state = " + State + "（" + st + "）· 藏起来的有："
                      + (hidden.Count == 0 ? "（一颗都不藏）" : string.Join(" / ", hidden.ToArray()))
                      + "；判据 = `DeckInfoControls__Initialize.c` 的 SetActive（表 → 本文件头）");
            NoteUnimplementedLayers();
        }

        /// <summary>判据里有、我们**做不到 / 判不出**的两处 —— 每次显隐都出声（铁律 11：先记录，别静默）。</summary>
        void NoteUnimplementedLayers()
        {
            // ① `SoftDisable(!CanImportDeck(popup, context.Deck))`（原版 :~78-80）
            //    我们**算得出**这个布尔（原版 `CanImportDeck` 只认 `CardDeck.CustomGameModeEvent`：
            //    `DeckInfoPopup__CanImportDeck.c:33-35` —— `get_CustomGameModeEvent(deck) == null ⇒ return 0`；
            //    我们**没有任何自定义游戏模式事件的卡组** ⇒ **恒 `CanImportDeck == false` ⇒ 原版会 `SoftDisable(true)`**）。
            //    ⚠️ **但「变灰但还点得动」这套观感我们没做**（`WindowButton` 只有 hover/press 换图，没有 disabled 态）
            //    ⇒ **如实记着，不拿一个假灰顶替**（真的把 `Edit Deck` 置灰会挡住编辑卡组这条主路）。
            //    📌 这一层要补：得先给 `WindowButton` 加 disabled 观感，再照 `EverguildButton.SoftDisable`
            //    （`EverguildButton.cs:55` 的 `softDisabled` 字段 + `SetToStateActiveOrDisabled`）接。
            if (State == DeckInfoState.Edit || State == DeckInfoState.Import)
                Debug.Log("[DeckInfo] ⚠️ 未复刻的一层：原版对这颗 `Edit Deck` 还会做 "
                          + "`SoftDisable(!CanImportDeck(popup, context.Deck))`；"
                          + "我们判得出这个布尔（`CanImportDeck` 只认 `CustomGameModeEvent`，我们没有那种卡组 ⇒ 恒 false）"
                          + "，但 `WindowButton` **没有 disabled 观感** ⇒ 没做，出声（别当已复刻）");

            // ② `state == 1` 时 `Edit Deck` 的文案换成**另一条本地化键**（原版 :84-95，两条键
            //    `DAT_1842d05e0` / `DAT_1842d04e0`）。词条表在**远端 CCD**（本地无 I2 表）⇒ **文案读不到**；
            //    而且我们**没有任何 state==1 的调用点**（原版那条是 `ChatMessageUI.OnMessageClicked`）⇒ 保持英文标签。
            if (State == DeckInfoState.Import)
                Debug.LogWarning("[DeckInfo] state==1（Import）时原版把 `Edit Deck` 的**文案**换成另一条本地化键 —— "
                                 + "本地无 I2 词条表（在远端 CCD）⇒ **文案取不到**，这扇窗仍显示我们那句英文（不许静默）");
        }

        /// <summary>这扇窗看的是哪一副（`CollectionData` 的下标）。</summary>
        public int DeckIndex;

        /// <summary>🆕 2026-10-04（§三第29条 A65④）：**原版 `Initialize` 末尾那三条「接线层」** —— 唯一实现。
        /// 逐条判据与偏移怎么对上节点名的，见文件头那张表 + 上面那四个字段的注释。**出厂在建窗末尾调一次。**
        ///
        /// 🔴 **它是「接线」不是「显隐」**：显隐（哪一颗露不露）在 `ApplyStateVisibility()` 那一份里；这里只管
        /// 「点了该不该生效」与两个**状态位**。三者互不覆盖 ⇒ ⛔ 别把其中一条挪进另一份实现里。
        ///
        /// 🔴 **`interactable = false` 我们怎么落地**：原版那颗钮是 UGUI `Selectable`，
        /// 置假之后 `OnPointerClick` **头一句就返回**（`Selectable.OnPointerClick`：
        /// `if (!IsActive() || !IsInteractable()) return;`）⇒ 语义 = **点了什么都不发生**（而且是灰的）。
        /// 我们的 `WindowButton` 只有 `onClick` 一个口 ⇒ 等价物 = **在动作那一层挡掉 + 出声**
        /// （见 `Blocked`）。⚠️ **灰的那一半归 A65②**（`WindowButton` 的 disabled 观感），**如实标、别假装已有**。</summary>
        public void ApplyControlStates()
        {
            var deck = CollectionData.Raw(DeckIndex);

            // ① `Practice Deck`：原版 `:201-207`
            //    `uVar4 = DeckUtility__ValidateDeck(context.Deck, local_res10 /*out err*/, **1** /*validateOwnership*/, 0);`
            //    `set_interactable(practiceButton, uVar4);`
            // 🔴 判据**只此一份**：`RuleEngine.DeckRules.Validate`（遭遇窗那条链也走它 —— 两处写同一条规则 = 迟早不一致）。
            //    ⚠️ 两处如实差别：原版那次还查 `ValidateDeckOwnership`（「这些卡我有没有」）——
            //    **我们没有拥有度系统**（同 `IsPlayerDeck` 那条老账）⇒ 这一半做不了，出声。
            int mode = deck != null ? deck.GameMode : 0;
            bool skirmish = mode == (int)GameMode.Skirmish;
            PracticeDeckInteractable = deck != null
                && DeckRules.Validate(deck, CollectionData.Card, skirmish) == DeckError.None;
            if (!PracticeDeckInteractable)
                Debug.Log("[DeckInfo] `Practice Deck` 置成 **不可点**（原版 `interactable = DeckUtility.ValidateDeck(deck, …)`）："
                          + (deck == null ? "这一格没有卡组"
                                          : DeckRules.Describe(DeckRules.Validate(deck, CollectionData.Card, skirmish)))
                          + " ⇒ 点了不生效（**不许静默**；⚠️ 原版同时会把那颗钮**变灰**，"
                          + "那半归 §三第29条 A65②，我们还没有 disabled 观感）");

            // ② `Switch Deck Info`：原版 `:210-211` —— `Toggle.SetIsOnWithoutNotify(true)`。
            //    **不带通知** ⇒ 不切抽屉（`SetContent` 那两句摆的才是出厂那两个抽屉的状态，见本文件 `Build`）。
            //    ⚠️ 那条回调（`onValueChanged` → `DeckInfoPopup.ToggleDrawers(bool)`）走的是 `SwitchDrawer()`；
            //    `isOn` 与「哪一面在前」的对应关系本地判据不足 ⇒ 我们只记录这个状态位（见 `DrawerToggleIsOn`）。
            DrawerToggleIsOn = true;

            // ③ 两颗圆钮：原版 `:229-243` —— `AssetLocator.GetAssets<CardDeck>(...)` 的 **count** 是判据
            //    （我们这一侧的同一件事 = `CollectionData.DeckCount()`）
            int n = CollectionData.DeckCount();
            DeleteInteractable = n > 1;                     // 原版 `set_interactable(deleteButton,    **1 < iVar1**)`
            DuplicateInteractable = n < MaxCustomDecks;     // 原版 `set_interactable(duplicateButton, **iVar1 < 上限**)`
            Debug.Log("[DeckInfo] 接线层（原版 `DeckInfoControls.Initialize` 末尾那三条）："
                      + "`Practice Deck` 可点 = " + PracticeDeckInteractable
                      + " · `Delete` 可点 = " + DeleteInteractable + "（卡组数 " + n + " > 1）"
                      + " · `Duplicate` 可点 = " + DuplicateInteractable + "（卡组数 " + n + " < 上限 " + MaxCustomDecks + "）"
                      + " · `Switch Deck Info` 的 `isOn` = " + DrawerToggleIsOn + "（`SetIsOnWithoutNotify(true)`）");
        }

        /// <summary>`Selectable.interactable == false` 的等价物：**动作照进来，但立刻返回**。
        /// 返回 true = 这一下**不该生效**（调用方 `return`）。
        /// 🔴 **出声**（红线：不许静默失败）—— 原版玩家看得见「钮是灰的」，什么都不做**是合理的**；
        /// 我们还没有那层观感，若连日志都不打，玩家只会以为 UI 坏了。</summary>
        bool Blocked(string key, bool interactable)
        {
            if (interactable) return false;
            Debug.LogWarning("[DeckInfo] `" + key + "` **点了不生效** —— 原版这颗钮是 `interactable = false`"
                             + "（判据 → `ApplyControlStates` 的注释）：那颗钮既不吃点击、又是灰的。"
                             + "我们这一半做了（挡动作 + 出声）；**变灰那一半归 §三第29条 A65②**"
                             + "（`WindowButton` 的 disabled 观感）");
            return true;
        }

        /// <summary>画出来的卡行数（自检用）。</summary>
        public readonly List<Transform> Rows = new List<Transform>();
        /// <summary>三个大钮的点击区（自检按名字找）。</summary>
        public Transform Btn(string n) { return Find(transform, "Buttons/Btn_" + n); }
        /// <summary>五个圆钮的点击区。</summary>
        public Transform Opt(string n) { return Find(transform, "Deck Options/Opt_" + n); }

        static Transform Find(Transform root, string path)
        {
            if (root == null) return null;
            var parts = path.Split('/');
            var t = root;
            foreach (var p in parts)
            {
                t = t.Find(p);
                if (t == null) return null;
            }
            return t;
        }

        /// <summary>开一扇。`state` = 原版 `context.state`（决定 8 颗钮的显隐，见 `DeckInfoState`）；
        /// `selectButtonProvided` = 原版 `context.SelectButton != null`（**原版 6 个调用点全传 null**）；
        /// 🆕 `selectButtonText` = 原版 `context.SelectButton.Text`（`Select Deck` 那颗钮的文案**从它抄过来**，
        /// `:216-224`；给了 `selectButtonProvided = false` 时它没有意义）。</summary>
        public static DeckInfoPopup Create(WindowsManager mgr, int deckIndex,
                                           DeckInfoState state = DeckInfoState.Edit,
                                           bool selectButtonProvided = false,
                                           string selectButtonText = null)
        {
            var go = new GameObject("Deck info Popup");
            var win = go.AddComponent<DeckInfoPopup>();
            win.type = WindowType.Popup;                 // 实证 type=1
            win.placement = WindowsPlacement.Popup;      // 实证 windowsPlacement=15
            win.closeOnEsc = true;                       // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1f;
            win.DeckIndex = deckIndex;
            win.State = state;
            win.SelectButtonProvided = selectButtonProvided;
            win.SelectButtonText = selectButtonText;
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open() { Build(); }

        void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);
            Rows.Clear();
            _parts.Clear();          // 🆕 A31：重建 ⇒ 旧的「哪几颗钮」记录一并作废（别留着指已销毁的节点）

            var info = CollectionData.DeckAt(DeckIndex);

            // 1) 压暗整屏（原版 rect 比屏幕大 ⇒ 我们直接铺满可见区；断言量的是「铺满」）
            Solid(root, root, 960f, 540f, 1920f, 1080f, ShadeColor, QDI, "Menu Dark Background");

            // 2) 红底（Sliced）
            Nine(root, root, "UI_Deck_Information_Back", RedBorder, RedTexW, RedTexH,
                 RedL, RedT, RedR, RedB, QDI, "Generic Window Red Background Big");

            // 3) 督军立绘（原版 sprite=0 运行时喂）
            var wl = CollectionData.Warlord(DeckIndex);
            var wlTex = wl != null ? CardArt.PortraitByName(wl.Name) : null;
            if (wlTex != null)
                Img(root, root, wlTex, WarlordL, WarlordT, WarlordR, WarlordB, "Warlord Image", QDI, true);
            else
                Debug.Log("[DeckInfo] 督军立绘取不到（" + (wl != null ? wl.Name : "没有督军") + "）—— 那一层不画，出声");
            // 🆕 A10：`Warlord Image` 那一层**本来就是 `EverguildButton`**（原版点了开卡详情窗）——
            //    原来记的「那扇窗还没建」是**过期**的（`CardDetailPopup` 2026-09-24 就建好了）⇒ 接上。
            Hit(root, root, "WarlordHit", new PxRect(WarlordL, WarlordT, WarlordR, WarlordB), OpenWarlordDetail);

            // 4) `Deck Details`（**跑后真值**，见常量那段的订正）
            //    HLG{align=MiddleLeft} ⇒ 视觉顺序 = 树序：`Game Mode Icon` → `Separator` → `Deck Details`(内层)
            // 🆕 A10：`Game Mode Icon` 补上了（原版 `sprite=0`、运行期按 `gameModeDeckIcon` 喂；
            //    两张图 `40k_gamemode_icon_{classic,skirmish}` **本来就在工程里** —— 原来记的「本地没图」是**过期**的）
            {
                string gmArt = info.GameMode == 13 ? "40k_gamemode_icon_skirmish" : "40k_gamemode_icon_classic";
                Img(root, root, CardArt.MenuUi(gmArt), DdGmL, DdGmT, DdGmR, DdGmB, "Game Mode Icon", QDIRow, true);
            }
            Nine(root, root, "40k_Generic_Smooth_line", new Vector4(55f, 15f, 55f, 15f), 113f, 32f,
                 SepL, SepT, SepR, SepB, QDIRow, "Game Mode Separator", new Color(0.42f, 0.157f, 0.137f, 1f));
            var facTex = string.IsNullOrEmpty(info.Faction) ? null : CardArt.MenuUi(DeckRuntime.FactionIcon(info.Faction));
            if (facTex != null)
                Img(root, root, facTex, DdIconL, DdIconT, DdIconR, DdIconB, "Army Icon", QDIRow, true);
            Txt(root, root, info.Name, DdNameL, DdNameT, DdNameR, DdNameB, 44.5f, Align.Left, "Deck Name", QDIText);
            Txt(root, root, wl != null ? wl.Name : "未选督军", DdWlL, DdWlT, DdWlR, DdWlB, 40f, Align.Left,
                "Warlord Name", QDIText);

            // 5) `Info Panel` + 两个抽屉（`Deck List` 出厂在前、`Deck Info` 出厂 **INACT** ⇒ 建了关着）
            Nine(root, root, "UI_Deck_Information_submenu_Back", PanelBorder, PanelTexW, PanelTexH,
                 PanelL, PanelT, PanelR, PanelB, QDI, "Info Panel");
            BuildDeckList(root);
            BuildDeckInfoDrawer(root);

            // 6) `Buttons`：右对齐到 1770.70、spacing 36 ⇒ 最左一格 = 1770.70 − (3×324.5 + 2×36)
            {
                var holder = new GameObject("Buttons");
                holder.transform.SetParent(root, false);
                holder.transform.localPosition = Local3(root, BtnL, BtnT, BtnL + 3f * BtnW + 2f * BtnGap, BtnT + BtnH);
                string[] btns = { "Practice Deck", "Edit Deck", "Select Deck" };
                for (int i = 0; i < btns.Length; i++)
                {
                    // ⚠️ **坐标一律是「页面绝对 px」、basis 给【实际父节点】** ——
                    //    `Local3(basis, …)` 算的是 `RectCenter(绝对px) − basis.position`，
                    //    所以它必须与 parent 一致，否则整块会**再叠一层父节点的偏移**。
                    //    🔴 2026-09-23 两种错法都踩过：① 「容器内坐标 + basis」→ 飞走；
                    //      ② 「绝对坐标 + basis=root 而 parent 是容器」→ 叠一层容器偏移
                    //      （按钮量出来 1175.40，期望 887.45，差的就是容器中心 287.95）。
                    float x1 = BtnL + i * (BtnW + BtnGap);
                    var r = new PxRect(x1, BtnT, x1 + BtnW, BtnT + BtnH);
                    // ⚠️ `UI_Button_Mulligan` **不是九宫格图** —— 实读 `Sprite/UI_Button_Mulligan.json`：
                    //    **`m_Border = None`**，图 410×124。原版标 `type=Sliced` 但**没有 border 就等于拉伸**
                    //    ⇒ 正确做法是走普通 `Img`（拉伸）。原来走 `Nine` + `border=(333,96,333,96)`：
                    //    **左+右 = 666 > 图宽 410** ⇒ 每建一次吐一条「Nine: border 比图还大，退回单块」，
                    //    画面一样但日志被刷脏（§三 第 15 条 **第 57 行**）。
                    //    卡片详情窗 2026-09-24 已经这么改过（`CardDetailPopup.cs` 的 `Craft Bg` 那条）。
                    var bgq = Img(holder.transform, holder.transform, CardArt.MenuUi("UI_Button_Mulligan"),
                        r.x1, r.y1, r.x2, r.y2, "Bg " + btns[i], QDIRow, false);
                    // 🆕 A65④：`Select Deck` 那颗的**文案是从 `context.SelectButton.Text` 抄过来的**
                    //   （原版 `DeckInfoControls__Initialize.c:213-224`：`uVar11 = *(context + 0x20 + 0x10)`
                    //    ⇒ 抄进 `selectButton` 的 TMP；`context+0x20 == 0` 时抄的是空串）。
                    //   ⚠️ 原版 6 个调用点全传 null ⇒ 这条在本 build 里从不触发；我们照原样摆着。
                    string labelText = (btns[i] == "Select Deck" && SelectButtonProvided)
                                       ? (SelectButtonText ?? "") : btns[i];
                    var lab = Txt(holder.transform, holder.transform, labelText, r.x1, r.y1, r.x2, r.y2, 34f, Align.Center,
                        "Text " + btns[i], QDIText);
                    string key = btns[i];
                    // A17：原版 `Buttons>{Practice,Edit,Select} Deck` 是 SpriteSwap（普查 §块 3 第 6 行）
                    var hitq = Hit(holder.transform, holder.transform, "Btn_" + key, r, () => OnButton(key),
                        bgq, "UI_Button_Mulligan");
                    // 🆕 A31：这三颗要**按 state 一起显隐**（底/字/点击区是兄弟 ⇒ 逐颗点名）
                    Track("Btn:" + key, bgq, lab, hitq);
                }
            }

            // 7) `Deck Options`：spacing **−50** · MiddleRight ⇒ 相邻两颗叠 50px
            //    🔴 **2026-10-03 就地订正（A10）**：顺序原来**反了**。原版左→右 = **树序**：
            //      `Switch Deck Info`(1611.7) → `Duplicate`(1636.1) → `Share`(1660.5) → `Share On Chat`(1684.8) → `Delete`(1709.2)
            //      （实据 = fresh dump 的跑后 x；步进 24.386 = 74.386 − 50）。
            //      我们原来是「`Delete` 最左」—— 那条注释猜的是「`reverse=1` ⇒ 与树序相反」，**实测不成立**。
            {
                var holder = new GameObject("Deck Options");
                holder.transform.SetParent(root, false);
                float total = OptW + (5 - 1) * OptStep;
                holder.transform.localPosition = Local3(root, OptR - total, OptT, OptR, OptT + OptH);
                string[][] opts =
                {
                    new[] { "Switch Deck Info", "40k_general_bt_yellow_seedeck" },
                    new[] { "Duplicate",        "40k_general_bt_yellow_duplicate" },
                    new[] { "Share",            "40k_general_bt_yellow_share" },
                    new[] { "Share On Chat",    "40k_general_bt_yellow_share in chat" },
                    new[] { "Delete",           "40k_general_bt_yellow_delete" },
                };
                float optX0 = OptR - total;
                for (int i = 0; i < opts.Length; i++)
                {
                    float x1 = optX0 + i * OptStep;
                    var r = new PxRect(x1, OptT, x1 + OptW, OptT + OptH);
                    var obg = Img(holder.transform, holder.transform, CardArt.MenuUi("UI_Button_Round_background"),
                        r.x1, r.y1, r.x2, r.y2, "Bg " + opts[i][0], QDIRow, true);
                    float fx1 = r.x1 + (OptW - OptIconW) * 0.5f;
                    float fy1 = OptT + (OptH - OptIconH) * 0.5f;
                    var face = Img(holder.transform, holder.transform, CardArt.MenuUi("40k_general_bt_yellow"),
                        fx1, fy1, fx1 + OptIconW, fy1 + OptIconH, "Face " + opts[i][0], QDIRow, true);
                    var oicon = Img(holder.transform, holder.transform, CardArt.MenuUi(opts[i][1]),
                        fx1, fy1, fx1 + OptIconW, fy1 + OptIconH, "Icon " + opts[i][0], QDIRow, true);
                    string key = opts[i][0];
                    // A17：`Deck Options>…` 五颗都是 SpriteSwap（普查 §块 3 第 7 行）
                    var ohit = Hit(holder.transform, holder.transform, "Opt_" + key, r, () => OnOption(key),
                        face, "40k_general_bt_yellow");
                    // 🆕 A31：五颗里除 `Switch Deck Info`（常显）外都按 state 显隐
                    Track("Opt:" + key, obg, face, oicon, ohit);
                }
            }

            // 8) 关闭圆钮
            {
                var r = new PxRect(CloseL, CloseT, CloseR, CloseB);
                Img(root, root, CardArt.MenuUi("UI_Button_Round_background"), r.x1, r.y1, r.x2, r.y2,
                    "Close Bg", QDIRow, true);
                float fx1 = r.x1 + (r.W - OptIconW) * 0.5f, fy1 = OptIconT;
                var closeFace = Img(root, root, CardArt.MenuUi("40k_general_bt_yellow"),
                    fx1, fy1, fx1 + OptIconW, fy1 + OptIconH, "Close Face", QDIRow, true);
                Img(root, root, CardArt.MenuUi("40k_general_bt_yellow_close"),
                    fx1, fy1, fx1 + OptIconW, fy1 + OptIconH, "Close Icon", QDIRow, true);
                // A17：`Generic Close Button Orange` 是 SpriteSwap（普查 §块 3 第 8 行）
                Hit(root, root, "CloseHit", r, () => Close(), closeFace, "40k_general_bt_yellow");
            }

            // 9) 🆕 A31：按 state 摆 8 颗钮的显隐（判据 → 本文件头那张表 / `DeckInfoControls__Initialize.c`）
            ApplyStateVisibility();
            // 10) 🆕 A65④：原版 `Initialize` 末尾那三条「接线层」（同一个方法里、**在显隐之后** ——
            //     原版那一段也在 SetActive 那一片之后，见 `DeckInfoControls__Initialize.c:190-243`）
            ApplyControlStates();
        }

        /// <summary>`Deck List`：卡组里的每一张（**同名合并成 `xN`**）摆成 3 列 × 360×58 的格。</summary>
        void BuildDeckList(Transform root)
        {            var deck = CollectionData.Raw(DeckIndex);
            if (deck == null) return;

            var order = new List<string>();
            var count = new Dictionary<string, int>();
            if (deck.CardIds != null)
                foreach (var id in deck.CardIds)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    if (!count.ContainsKey(id)) { count[id] = 0; order.Add(id); }
                    count[id]++;
                }
            if (!string.IsNullOrEmpty(deck.WarlordId)) { count[deck.WarlordId] = 1; order.Insert(0, deck.WarlordId); }

            var holder = new GameObject("Deck List");
            holder.transform.SetParent(root, false);
            _listDrawer = holder.transform;
            int cols = ListCols;
            for (int i = 0; i < order.Count; i++)
            {
                var card = CollectionData.Card(order[i]);
                if (card == null) continue;
                int c = i % cols, rr = i / cols;
                float x1 = RowL + c * (RowW + RowGapX);
                float y1 = RowT + rr * (RowH + RowGapY);
                var node = new GameObject("Row_" + i);
                node.transform.SetParent(holder.transform, false);
                node.transform.localPosition = Local3(root, x1, y1, x1 + RowW, y1 + RowH);

                Solid(node.transform, node.transform, x1 + RowW * 0.5f, y1 + RowH * 0.5f, RowW, RowH,
                      new Color(1f, 1f, 1f, 0.08f), QDIRow, "Row Bg");
                Img(node.transform, node.transform, CardArt.MenuUi("Card_Frame_Cost_Icon"),
                    x1 + 6f, y1 + 9f, x1 + 46f, y1 + 49f, "Cost", QDIRow, true);
                Txt(node.transform, node.transform, card.Cost.ToString(), x1 + 6f, y1 + 9f, x1 + 46f, y1 + 49f, 26f, Align.Center,
                    "Cost Text", QDIText);
                Txt(node.transform, node.transform, card.Name, x1 + 52f, y1, x1 + RowW - 52f, y1 + RowH, 26f, Align.Left,
                    "Name", QDIText);
                Txt(node.transform, node.transform, "x" + count[order[i]], x1 + RowW - 52f, y1, x1 + RowW - 8f, y1 + RowH, 26f,
                    Align.Right, "Count", QDIText);
                Rows.Add(node.transform);
            }
        }

        // ============================================================ 🆕 2026-10-03（A10）：`Deck Info` 抽屉

        Transform _listDrawer, _infoDrawer;

        /// <summary>`Deck Info`（原版 `DeckInfoDrawer`，与 `Deck List` **同矩形**、出厂 `INACT`）。
        /// 逐值 = `python 工具/menu_dump.py bundle_menus_assets_all "Deck info Popup" --depth 8`（2026-10-03）。
        /// 🔴 **两处 `localScale`**：`Deck Information cost drawer` **1.8**、`Cardback` **1.85**
        /// —— 原版是把小图放大画的，所以真画出来的尺寸 = 序列化 rect × 那个倍数（**照乘**）。</summary>
        void BuildDeckInfoDrawer(Transform root)
        {
            var go = new GameObject("Deck Info");
            go.transform.SetParent(root, false);
            var b = go.transform;
            CostRowCounts.Clear();

            // ① 标题那条（原版是**葡语占位** `Cartas / Coste` · fs44 auto[10,44] · hAlign=Center）
            //    ⇒ 我们写英文 `Cards / Cost`（**这一处文案是我们挑的**，原版那份是占位串）
            Txt(b, b, "Cards / Cost", DiHeadL, DiHeadT, DiHeadR, DiHeadB, 44f, Align.Center,
                "Deck Information Cost/balance text", QDIText);

            // ② 费用曲线（9 行 = 费用 0..8）
            BuildCostDrawer(b);

            // ③ 卡背（`Cardback` 210×305 @ 中心 (1466,552.6) · **scl 1.85**）
            {
                var info = CollectionData.DeckAt(DeckIndex);
                var tex = CardArt.DeckCardback(info.CardbackId, info.Faction);
                float w = DiCbW * DiCbScl, h = DiCbH * DiCbScl;
                if (tex != null)
                    Img(b, b, tex, DiCbCX - w * 0.5f, DiCbCY - h * 0.5f, DiCbCX + w * 0.5f, DiCbCY + h * 0.5f,
                        "Cardback", QDIRow, false);
                else
                    Debug.Log("[DeckInfo] 卡背取不到（`" + (info.CardbackId ?? "") + "` / 阵营 "
                              + (info.Faction ?? "") + "）⇒ `Cardback` 那一层不画，出声");
            }

            // ④ `Lore Text`（出厂 **INACT**）⇒ 照纪律**不建**；而且我们**没有 lore 字段**（老账）
            Debug.Log("[DeckInfo] `Deck Info/Lore Text` **不建**：原版出厂 `act=N`，而且 lore 文本在服务端 —— "
                      + "我们引擎里**没有 lore 字段**（同 `CardDetailPopup` 那条老账）⇒ 如实说明，不编文案");

            go.SetActive(false);           // 出厂 INACT（只有 `Deck List` 那一面开着）
            _infoDrawer = b;
        }

        /// <summary>`Deck Energy Cost Drawer` 那 9 行。**画法只此一份** ⇒ 转调 `Core/CostCurveDrawer`
        /// （练习窗那扇走同一个函数，只是 `localScale` 是 1.2）。几何与判据全在那个文件里。</summary>
        void BuildCostDrawer(Transform b)
        {
            var counts = CostCurveDrawer.Counts(CollectionData.Raw(DeckIndex), CollectionData.Card);
            CostCurveDrawer.Build(b, DiDrawerCX, DiDrawerCY, DiDrawerScl, counts, QDIRow, QDIText);
            CostRowCounts.AddRange(counts);
        }

        // ============================================================ 交互

        void OnButton(string key)
        {
            if (key == "Edit Deck")
            {
                // 「进编辑」**只有一份实现**（`CollectionWindow.GoEdit`）—— 收藏窗那条路也走它
                CollectionWindow.GoEdit(DeckIndex);
                Close();
                return;
            }
            if (key == "Select Deck")
            {
                CollectionData.Select(DeckIndex);
                Debug.Log("[DeckInfo] 已选中「" + CollectionData.DeckAt(DeckIndex).Name + "」");
                Close();
                return;
            }
            if (key == "Practice Deck")
            {
                // 🆕 A65④①：原版这颗钮的 `interactable = DeckUtility.ValidateDeck(deck, …)` ⇒ 判假时**点了不生效**
                if (Blocked("Practice Deck", PracticeDeckInteractable)) return;
                // 🆕 2026-10-03：**照原版那条链**（这一副 = 我的；再挑对手那一副）——
                //   原来这里只开练习窗、并把缺口写进日志，现在接上（判据见 `SelectPracticeOpponentDeck`）。
                SelectPracticeOpponentDeck();
                return;
            }
            NotBuilt("`" + key + "` —— 见 `Shell/DeckInfoPopup.cs` 文件头「没建的」");
        }

        // ============================================================ 🆕 2026-10-03：`Practice Deck` 那条链
        //   （原版 `DeckInfoPopup.SelectPracticeOpponentDeck` → `StartPracticeMatch`；判据 → 文件头那段订正）

        /// <summary>最近一次由 `Practice Deck` 开出来的选卡组窗（自检用）。</summary>
        public static DeckSelectionPopup LastOpponentSelection;

        /// <summary>
        /// 原版 `DeckInfoPopup.SelectPracticeOpponentDeck`：`WindowsManager.OpenWindow(deckSelectionPopup,
        /// context{ Deck = **这一副**, Callback = StartPracticeMatch, Filter = null, GameMode = Classic })`，
        /// **开完就把自己关掉**（`DeckInfoPopup__SelectPracticeOpponentDeck.c:41-45`；虚表 `+0x1b8` = `Close`）。
        ///
        /// 🔴 **开的那扇窗选的是【对手】卡组** —— 所以原版默认就落在「预组卡组」页（对手多半是预组）。
        /// 我们**直接用已有那扇** `DeckSelectionPopup`（不另建），并照原版把「**这一副**」当「当前卡组」传进去 ——
        /// 它唯一的用处是**模式筛选**（`DeckSelectionPopup.__TryOpen_b__10_0`：候选 `gameMode` 必须等于当前卡组的）
        /// ⇒ 对应我们的 `ModeFilter = 这一副的 gameMode`。
        /// </summary>
        public void SelectPracticeOpponentDeck()
        {
            LastOpponentSelection = null;
            if (Manager == null) { Debug.LogWarning("[DeckInfo] 没有 `WindowsManager` ⇒ 开不了「挑对手卡组」那扇窗"); return; }
            var info = CollectionData.DeckAt(DeckIndex);
            int mine = DeckIndex;                       // 闭包别抓 `DeckIndex`（关窗后这个对象还可能被复用）
            var w = DeckSelectionPopup.Create(Manager, pick => StartPracticeMatch(mine, pick),
                                              modeFilter: info.GameMode);
            Manager.OpenWindow(w);
            LastOpponentSelection = w;
            Debug.Log("[DeckInfo] `Practice Deck` ⇒ 开选卡组窗挑**对手**（原版 `SelectPracticeOpponentDeck`："
                      + "context 里那副「" + info.Name + "」= **我**的卡组 = `playerDeck`，回调回来那副才是 `enemyDeck`）");
            Close();                                    // 原版开完那扇窗就关自己
        }

        /// <summary>
        /// 原版 `DeckInfoPopup.StartPracticeMatch(对手那副)`：`ShowPopUp(等待窗)` →
        /// `CreatePlayerBattleData(**自己**那副)` → `GameStaticData.CheckHiddenCardsInDeck(**自己**那副)` —
        /// **有隐藏卡 ⇒ 弹提示 + 不开打**；否则 `MatchMakerManager.StartMatch(OwnDeckTraining(0xc),
        /// …, playerDeck: 自己那副, enemyDeck: 对手那副)`。
        ///
        /// ⚠️ **两处如实差别**：① 原版 `StartMatch` 是**联机**匹配（匹配不到才落 bot），我们是**打 bot**；
        /// ② 原版那扇等待窗走 `ShowPopUp`，我们的等待窗 = 练习窗里那扇 12 秒 `Searching Oponent Popup`
        /// （我们这条开战链长在 `PracticeModePopup` 上，故借它当宿主）。
        /// </summary>
        public void StartPracticeMatch(int ownDeckIndex, DeckSelectionPopup.DeckPick opponent)
        {
            var mine = CollectionData.Raw(ownDeckIndex);
            // ① 原版那句 `GameStaticData.CheckHiddenCardsInDeck(context.Deck)` —— 查的是**自己**那副
            if (PracticeModePopup.HasHiddenCards(mine, out string hiddenWhy))
            {
                Debug.LogWarning("[DeckInfo] 我这副「" + (mine != null ? mine.Name : "?") + "」里有**隐藏卡** ⇒ "
                                 + "**不开打**（原版 `CheckHiddenCardsInDeck` 那一支）" + hiddenWhy);
                if (Manager != null)
                    Manager.ShowPopUp("这套卡组里有隐藏卡，开不了练习赛。" + hiddenWhy, "知道了", null);
                return;
            }
            Debug.Log("[DeckInfo] 隐藏卡检查过了 ⇒ 开打（原版 `CheckHiddenCardsInDeck` 返回假那一支）"
                      + (string.IsNullOrEmpty(hiddenWhy) ? "（⚠️ 见 `PracticeModePopup.HasHiddenCards`："
                        + "这个检查在我们的数据上恒为假，已出声）" : hiddenWhy));
            // ② 开练习窗 + 立即开打（原版 `ShowPopUp(等待窗)` → `StartMatch`）
            PracticeModePopup.StartPracticeMatch(Manager, ownDeckIndex, opponent);
        }

        void OnOption(string key)
        {
            if (key == "Delete")
            {
                // 🆕 A65④③：原版 `deleteButton.interactable = (卡组数 > 1)` ⇒ 只剩一套时点了不生效
                if (Blocked("Delete", DeleteInteractable)) return;
                if (CollectionData.DeleteDeck(DeckIndex)) { Debug.Log("[DeckInfo] 已删除该卡组"); Close(); }
                else Debug.Log("[DeckInfo] 删不了（`DeckLibrary.Delete` 的规矩：只剩一套时不许删 / 下标越界）");
                return;
            }
            if (key == "Duplicate")
            {
                // 🆕 A65④③：原版 `duplicateButton.interactable = (卡组数 < GameStaticData.totalCustomDecks)`
                if (Blocked("Duplicate", DuplicateInteractable)) return;
                string nm = CollectionData.DuplicateDeck(DeckIndex);
                if (!string.IsNullOrEmpty(nm)) { Debug.Log("[DeckInfo] 已复制成「" + nm + "」"); Close(); }
                else Debug.Log("[DeckInfo] 复制失败（`DeckLibrary.Duplicate` 返回空）");
                return;
            }
            // 🆕 A10：`Switch Deck Info` —— 原版是 `DeckInfoControls.toggleDrawerButton`，
            // 在 `Deck List` 与 `Deck Info` 两个抽屉之间切（两个是互斥的，同矩形叠着）
            if (key == "Switch Deck Info") { SwitchDrawer(); return; }
            if (key == "Share" || key == "Share On Chat") { ShareDeck(key); return; }
            NotBuilt("`" + key + "` —— 见 `Shell/DeckInfoPopup.cs` 文件头「没建的」");
        }

        /// <summary>🆕 A10：点督军立绘 ⇒ 开**卡片详情窗**（原版 `Warlord Image` 上那个 `EverguildButton`）。</summary>
        void OpenWarlordDetail()
        {
            var wl = CollectionData.Warlord(DeckIndex);
            if (wl == null) { Debug.LogWarning("[DeckInfo] 这副没有督军 ⇒ 开不了详情窗"); return; }
            if (Manager == null) { Debug.LogWarning("[DeckInfo] 没有 `WindowsManager` ⇒ 开不了详情窗"); return; }
            var w = CardDetailPopup.Create(Manager);
            Manager.OpenWindow(w);
            w.ShowCard(wl);
            LastWarlordDetail = w;
            Debug.Log("[DeckInfo] 点督军立绘 ⇒ 开卡片详情窗（「" + wl.Name + "」）");
        }

        /// <summary>🆕 最近一次由「点督军立绘」开出来的卡片详情窗（自检用）。
        /// ⚠️ **不能借 `CardDetailPopup.LastOpened`** —— 那个字段只在 `RebuildKeepingState()` 里赋值，
        /// `Create()`/`Open()` 都不设它（2026-10-03 自检在这里 NRE 过，已记）。</summary>
        public static CardDetailPopup LastWarlordDetail;

        /// <summary>`Switch Deck Info`：两个抽屉互斥切换（原版两个抽屉**同矩形叠着**、出厂只有 `Deck List` 开）。
        /// 🆕 A65④②：这条点击就是原版 `toggleDrawerButton.onValueChanged → DeckInfoPopup.ToggleDrawers(bool)`
        /// 那一条（`DeckInfoControls__Initialize.c:178-189` 把回调挂在 toggle 的 `+0x118` 上，
        /// 目标是 `DeckInfoPopup.ToggleDrawers`）—— uGUI 的点击 = 先翻 `isOn`、再拿新值发通知
        /// ⇒ 这里把 `DrawerToggleIsOn` 一并翻过来（**出厂是 `SetIsOnWithoutNotify(true)`**，见 `ApplyControlStates`）。</summary>
        public void SwitchDrawer()
        {
            InfoDrawerShown = !InfoDrawerShown;
            DrawerToggleIsOn = !DrawerToggleIsOn;
            if (_listDrawer != null) _listDrawer.gameObject.SetActive(!InfoDrawerShown);
            if (_infoDrawer != null) _infoDrawer.gameObject.SetActive(InfoDrawerShown);
            Debug.Log("[DeckInfo] 切到 " + (InfoDrawerShown ? "`Deck Info`（费用曲线 / 卡背）" : "`Deck List`")
                      + " 那一面（原版 `Switch Deck Info Button` 的语义）；toggle `isOn` → " + DrawerToggleIsOn);
        }

        /// <summary>`Share` / `Share On Chat`：**原版走服务端 / 平台分享**，我们没有那两条。
        /// 能拿出来的、真正可分享的东西 = **卡组串**（`DeckLibrary.ExportString`，与卡组编辑那颗 `Share` 同一份）
        /// ⇒ 弹出来给用户看/抄（批处理与桌面都没法替用户按剪贴板），并**如实说明**聊天那条发不出去。</summary>
        void ShareDeck(string key)
        {
            var deck = CollectionData.Raw(DeckIndex);
            string s = deck != null ? RuleEngine.DeckLibrary.ExportString(deck) : "";
            if (string.IsNullOrEmpty(s))
            {
                Debug.LogWarning("[DeckInfo] `" + key + "`：卡组串导不出来（`ExportString` 返回空）⇒ 没东西可分享");
                return;
            }
            Debug.Log("[DeckInfo] `" + key + "` ⇒ 卡组串（" + s.Length + " 字符）：" + s);
            if (Manager != null)
                Manager.ShowPopUp(key == "Share On Chat"
                    ? "聊天窗**发不出消息**（原版走服务端，我们这条线没有网络）。\n\n这是这一副的卡组串，可以自己复制：\n" + s
                    : "原版是**平台分享**。\n\n这是这一副的卡组串，可以自己复制：\n" + s,
                    "知道了", null);
        }

        /// <summary>红线：**不许静默失败** —— 没做的必须说出来。</summary>
        void NotBuilt(string what)
        {
            Debug.LogWarning("[DeckInfo] `" + what + "` 还没实现（**出声**，见 `Shell/DeckInfoPopup.cs` 文件头「没建的」）");
        }

        // ============================================================ 画图小工具（照 PromptPopup 那套）
        // ⚠️ 每个都**同时收「画在谁下面」和「坐标以谁为基准」两个参数** ——
        // 两个写成一个（用 parent 当基准却画在别处）会让子件**整块偏走**，而矩形断言量不到。

        enum Align { Center, Left, Right }

        static Vector3 Local3(Transform basis, float x1, float y1, float x2, float y2)
        {
            return LayoutSpace.RectCenter(x1, y1, x2, y2) - basis.position;
        }

        void Solid(Transform parent, Transform basis, float cx, float cy, float w, float h, Color color, int q, string name)
        {
            var quad = ImageQuad.Create(parent, CardArt.Solid(),
                                        Local3(basis, cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f),
                                        LayoutSpace.Px(h), new Vector2(0.5f, 0.5f), name);
            if (quad == null) return;
            quad.SetAspect(w / h);
            quad.SetTint(color);
            quad.SetRenderQueue(q);
        }

        ImageQuad Img(Transform parent, Transform basis, Texture2D tex, float x1, float y1, float x2, float y2,
                      string name, int q, bool keepAspect)
        {
            if (tex == null) { Debug.LogWarning("[DeckInfo] 图取不到，这一层不画：" + name); return null; }
            float w = x2 - x1, h = y2 - y1;
            if (keepAspect && tex.height > 0)
            {
                float sa = (float)tex.width / tex.height, ra = w / Mathf.Max(1e-6f, h);
                if (sa > ra) { float nh = w / sa, d = (h - nh) * 0.5f; y1 += d; y2 -= d; h = nh; }
                else { float nw = h * sa, d = (w - nw) * 0.5f; x1 += d; x2 -= d; w = nw; }
            }
            var quad = ImageQuad.Create(parent, tex, Local3(basis, x1, y1, x2, y2),
                                        LayoutSpace.Px(h), new Vector2(0.5f, 0.5f), name);
            if (quad == null) return null;
            quad.SetAspect(w / Mathf.Max(1e-6f, h));
            quad.SetRenderQueue(q);
            return quad;
        }

        GameObject Nine(Transform parent, Transform basis, string art, Vector4 border, float texW, float texH,
                        float x1, float y1, float x2, float y2, int q, string name, Color? tint = null)
        {
            var tex = CardArt.MenuUi(art);
            if (tex == null) { Debug.LogWarning("[DeckInfo] 九宫格图取不到：" + art); return null; }
            var g = ImageQuad.CreateNineSlice(parent, tex, border, texW, texH, Local3(basis, x1, y1, x2, y2),
                                              LayoutSpace.Px(x2 - x1), LayoutSpace.Px(y2 - y1), name);
            if (g != null)
                foreach (var c in g.GetComponentsInChildren<ImageQuad>())
                {
                    c.SetRenderQueue(q);
                    if (tint.HasValue) c.SetTint(tint.Value);
                }
            return g;
        }

        Label Txt(Transform parent, Transform basis, string text, float x1, float y1, float x2, float y2,
                  float fontPx, Align align, string name, int q)
        {
            var lb = Label.Create(parent, text ?? "", Local3(basis, x1, y1, x2, y2), 5, Color.white,
                                  new Vector2(0.5f, 0.5f), name);
            if (lb == null) return null;
            lb.SetRenderQueue(q);
            if (fontPx > 0f) lb.SetGlyphHeight(LayoutSpace.Px(fontPx));
            // 原版这几条 TMP 的对齐：`Deck Name`/`Warlord Name` 居中、行里的卡名左、数量右
            if (align == Align.Right) lb.AlignRightOn(LayoutSpace.FromPixel(x2, 0f).x);
            else if (align == Align.Left) lb.AlignLeftOn(LayoutSpace.FromPixel(x1, 0f).x);
            return lb;
        }

        Transform Hit(Transform parent, Transform basis, string name, PxRect r, System.Action onClick,
                      ImageQuad target = null, string art = null, string hoverArt = null)
        {
            var hit = new GameObject(name);
            hit.transform.SetParent(parent, false);
            // 🔴 **节点本身也要摆**（2026-09-23 踩）：原来只摆了里面那个 quad ⇒ **节点全停在容器的 (0,0)**
            //    （= 容器中心）。自检按名字取节点量位置时，三个按钮**量出来是同一个 x**
            //    （1247.95 = 整行的中心），而 quad 画得是对的 ⇒ **量节点与量渲染是两回事**。
            //    凡「按名字取节点」的地方（自检 / 命中的 `GetComponentInChildren`）都要求节点摆对。
            hit.transform.localPosition = Local3(basis, r.x1, r.y1, r.x2, r.y2);
            var q = ImageQuad.Create(hit.transform, CardArt.Solid(), Vector3.zero,
                                     LayoutSpace.Px(r.H), new Vector2(0.5f, 0.5f), "Hit");
            if (q != null)
            {
                q.SetAspect(r.W / Mathf.Max(1e-6f, r.H));
                q.SetTint(new Color(0f, 0f, 0f, 0f));
                q.SetRenderQueue(QDIHit);
            }
            var wb = hit.AddComponent<WindowButton>();
            wb.onClick = onClick;
            if (target != null) wb.Bind(target, art, hoverArt);
            return hit.transform;
        }
    }
}
