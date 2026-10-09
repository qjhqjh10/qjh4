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
//   · `Share` / `Share On Chat` —— ✅ **接了**（`A1040` 2026-10-08 收口）。🔴 **这是两件不同的事**：
//     · `Share` = **写系统剪贴板 + 弹一条提示**（原版 `DeckInfoPopup__ShareDeck.c` 三句：造串 →
//       `UnityEngine.GUIUtility.set_systemCopyBuffer(串)` → `UIMessageController.ShowMessage`）；
//       🔴 **2026-10-09（`A1050`）：第 ③ 句现在与原版【同形】** —— 走 `Shell/MessageToast`（宿主 = 原版
//       `UIMessageController`）。**改前**它是 `Manager.ShowPopUp(…)` = **要玩家点 OK 的模态弹窗**（那句
//       「我们是模态弹窗」的标注就是它），**已按铁律 5 就地改掉**（改前/改后与全部判据 → `ShareDeck` 的 doc）；
//       写剪贴板那份实现**全库只有一处** = `Deck/DeckRuntime.CopyDeckToClipboard`（卡组编辑那颗 `Share` 也走它）
//       ⚠️ **卡组编辑那一侧仍没接上**（`DeckRuntime.ShareDeckString()` 走 `Say()`，屏幕上什么都不画）—— 如实标注，见 `ShareDeck` 的 doc。
//     · `Share On Chat` = 原版**开一扇两键面板（群组 / 全局）→ 发一条聊天消息**、**不写剪贴板**；
//       那扇面板**还没建**（账 `A1045`）⇒ 本路**如实出声**（`NotBuilt`）+ 把卡组串印出来给玩家自己抄
//     🔴 **订正（铁律 5）**：本条原文写「原版走平台/服务端，我们**给卡组串**」—— **两半都不对**：
//       原版既不上服务端、也没做平台分享（就是本地 `GUIUtility.systemCopyBuffer`，`DeckInfoPopup__ShareDeck.c:16`），
//       而「我们给卡组串」现在只剩 `Share On Chat` 那一路（`Share` 已真写剪贴板）。
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
//   ✅ **2026-10-05（A65②）做完了**：`SoftDisable(!CanImportDeck(popup, context.Deck))` 的
//      「**变灰但点得动**」观感接上了 —— 唯一实现在 `ApplyControlStates()` 的第 ④ 条，判据与核验见
//      那里 + `CanImportDeck` 的注释。⚠️ **这条观感是真·照抄，但后果要说清**：
//      原版那句 `SoftDisable` **没有 state 守卫**（`DeckInfoControls__Initialize.c:79-83` 在 `SetActive` 之后
//      无条件调），而我们 `CanImportDeck` **恒 false** ⇒ **state 0/1 下这颗钮一直是灰的**
//      （原版在「该 deck 的 game mode 没有正在跑的活动」时**也是**这样；我们根本没有活动系统）。
//      ⛔ 别为了「让钮亮着」把 `CanImportDeck` 改成 `true` —— 那是编一个原版没有的判据（铁律 3）。
//     🔴 **原来这里是那句「仍欠一件…`WindowButton` 没有 disabled 态 ⇒ 如实记着」** —— 留着更正痕（铁律 5）。
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
        /// <summary>`Warlord Image` 那颗 `Image` 的 **`m_RaycastPadding`**（UGUI 序 **L,B,R,T**）。
        /// 出处 = 解包 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-7131536541767857752.json`
        /// （`m_GameObject` 指回 `GameObject/Warlord Image_-6735770576364533336.json` —— 就是本窗子节点
        /// `Deck info Popup > Warlord Image`，`m_RaycastTarget = 1`）。
        /// 🔴 **它是全 `bundle_menus_assets_all` 里唯一一条「四个分量全正」的 `m_RaycastPadding`**
        /// （非零 208 条里 207 条是负的）⇒ 当年被记成「反例、符号存疑」。
        /// ✅ **2026-10-06（FIX-1）符号已坐实**（判据见 `WarlordHit` 那条注释），**正 = 把命中区往里缩**。
        /// ⚠️ **立绘本身照旧画满 1108²** —— padding **只改命中区**，不改渲染。</summary>
        public static readonly Vector4 WarlordPad = new Vector4(246.8f, 84.44f, 338.6f, 132.38f);
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
        /// <summary>`Deck Options` 行：五个圆钮、HLG spacing **−50** · align **MiddleRight** · **`reverse=1`**、
        /// 右对齐到 **1783.62**、y **130.20**。
        /// 🔴 **2026-10-05 更正**：`OptStep` 原来写 `OptW − 50f`（= **24.386**）—— 那是「总 flexible = 0 ⇒
        /// 整排按 `align` 平移」的**旧模型**；真值 **93.976** —— 原版 `m_ChildForceExpandWidth = 1`
        /// ⇒ 每格被撑到 `childSize 143.976`，步进 = `143.976 + spacing(−50)`。算式 → 本文件第 7) 节注释；
        /// 判据 = uGUI `HorizontalOrVerticalLayoutGroup.cs:186-216`。</summary>
        public const float OptR = 1783.62f, OptT = 130.20f, OptW = 74.386f, OptH = 75.605f, OptStep = 93.976f;
        /// <summary>圆钮里 `Background`/`Icon` 那两层的上下边（A1 §2：1790.96,71.18→1847.82,129.30）。</summary>
        public const float OptIconT = 71.18f, OptIconH = 58.12f, OptIconW = 56.86f;

        /// <summary>🆕 **2026-10-11（A308）**：圆钮里那两层的**四个内缩** —— 原版的子件矩形**不居中于按钮矩形**。
        ///
        /// <para>判据 = **逐字段实读**（`d:/2/新解包资源/assets_full/bundle_menus_assets_all`，
        /// 2026-10-11 沿 `m_Children` 走完整棵 `Deck Options`）：五颗按钮的 `Background` / `Icon`
        /// **十个节点逐值相同**（**铁律 5·c 逐颗核过，不是取一个代表**）——
        /// `m_SizeDelta = 0.3774` · `m_AnchorMin = (0.114, 0.12429)` ·
        /// `m_AnchorMax = (0.8733663, 0.88814)` · `m_AnchoredPosition = (−0.14, 0.29)` · `m_Pivot = (0.5, 0.5)`
        /// （按钮自身 `74.386 × 75.605`）。</para>
        ///
        /// <para>算式（uGUI：`子件边 = 父边 × 锚点 + anchoredPosition`，`sizeDelta` 加在锚点跨度上）：
        /// `w = 0.3774 + 0.7593663 × 74.386 = 56.8636` · `h = 0.3774 + 0.7638499 × 75.605 = 58.1283`；
        /// 左 = `74.386 × 0.4936832 − 0.14 − w/2 = 8.1513` ⇒ 右 = `74.386 − 8.1513 − 56.8636 = 9.3711`；
        /// 上（`T` 那一侧）= `75.605 − (75.605 × 0.5062151 + 0.29 + h/2) = 7.9785` ⇒ 下 = `9.4983`。
        /// ✅ **两条独立复核**：① 原版 dump 的子件绝对矩形 `138.2 → 196.3`（按钮 `130.20 → 205.805`）
        /// ⇒ 两侧内缩 `7.98 / 9.51`，与算式同量级；② **关窗钮那颗同模型**的子件 T 边 = `71.18`
        /// = `CloseT 63.20 + 7.98` —— 与本表 `OptIconT` 常量逐位自洽。</para>
        ///
        /// <para>🔴 **原来按「居中」画**（`(OptW − OptIconW)/2 = 8.763` · `(OptH − OptIconH)/2 = 8.7425`）
        /// ⇒ `Face` / `Icon` / 命中区**整块**偏 **+0.612(x) / +0.764(y)**（左侧与上侧各少一截、
        /// 右侧与下侧各多一截）。⚠️ 「取中」的代价就在这里：它**看着有依据**（居中是直觉默认），
        /// 而原版是**锚点算出来的**，本来就不居中（铁律 10 第 2 条：判据是**表**、不是截图）。
        /// ✅ **2026-10-12（A329）更正**：原文写「**关窗钮（第 8) 节）不在本件范围**：它的 y 早就是实读绝对值
        /// （`fy1 = OptIconT`，与 `OptInT` 自洽），但 x 仍是居中的 `+8.763`（差 0.612px…）—— ⛔ 本件没动它」。
        /// 那是 2026-10-11（A308）**当时**的状态；现在第 8) 节那颗的 `fx1` **也走 `OptInL`** 了
        /// （原版两侧子件矩形**逐值相同**，pid → `资料/普查产出_1011/W7_子5.md` §五·1）⇒
        /// **同一个文件里只剩这一套口诀**（`y` 侧那颗一开头就是实读绝对值，本来就没这毛病）。</para>
        ///
        /// <para>⛔ **别写回 `(OptW − OptIconW) * 0.5f`** —— 那就是 A308 本身
        /// （`Editor/CollectionScene.cs` 里「子件矩形中心 vs `Bg` 中心」那两条会红）。</para></summary>
        const float OptInL = 8.151f, OptInR = 9.371f, OptInT = 7.978f, OptInB = 9.498f;
        public const float PanelL = 659f, PanelT = 218.10f, PanelR = 1799f, PanelB = 868.10f;
        public const float RowL = PanelL + 15f, RowT = PanelT, RowW = 360f, RowH = 58f, RowGapX = 11f, RowGapY = 4.5f;
        public const float CloseL = 1782.81f, CloseT = 63.20f, CloseR = 1857.19f, CloseB = 138.80f;

        /// <summary>🆕 **2026-10-11（A180）**：关窗钮的 **`Graphic.m_RaycastPadding`** —— 原版那颗按钮
        /// **自己的 `Image` 不吃射线**（`m_RaycastTarget = 0`），吃射线的是**两个子件**
        /// `Background` / `Icon`（`m_RaycastTarget = 1`），而这两颗都带
        /// **`m_RaycastPadding = (−20, −20, −20, −20)`**（**负 = 外扩**，符号口径见 `MenuDraw.PaddedHitRect`）。
        ///
        /// <para>判据 = 解包实读（本包 `bundle_menus_assets_all`）：
        /// `Generic Close Button Orange_4868612507233777240` 那一棵 —— 关窗钮**自己的** `Image`
        /// `MonoBehaviour_3635446896823339432.json`（`m_RaycastTarget = 0` · pad 全 0）·
        /// 子件 `Background` `MonoBehaviour_-583606675015890520.json` 与 `Icon` `MonoBehaviour_7496897533368830376.json`
        /// （**两颗都 `m_RaycastTarget = 1` · `m_RaycastPadding = (−20,−20,−20,−20)`**）。
        /// 子件 rect（`menu_dump … "Deck info Popup"`）= **`1791.0, 71.2 → 1847.8, 129.3`**
        /// ⇒ 外扩 20 之后 = **`1771.0, 51.2 → 1867.8, 149.3`（96.8 × 98.1）**。</para>
        ///
        /// <para>🔴 **加在【子件】那一份 rect 上、不是加在按钮自己的 `CloseL/T/R/B` 上** ——
        /// 原版带 pad 的就是子件（`1791.0,71.2`）那一颗；加到按钮矩形上会算出 114.386×115.605，
        /// 比原版**每边多 ≈9px**（= `(74.386 − 56.86)/2` 那两个内缩）。
        /// ✅ **2026-10-12（A329）**：我们那两个子件层（`Close Face` / `Close Icon`）现在画在
        /// `CloseL + OptInL = 1790.961` ⇒ 复算出来 **`1770.961, 51.18 → 1867.821, 149.30`**，
        /// 与原版（`1771.0 / 51.2 / 1867.8 / 149.3`）**四边都差 ≤0.039px**（左 0.039 / 上 0.020 / 右 0.021 / 下 0.000）。
        /// 🔴 原文写的「画在 `CloseL + (CloseW − OptIconW)/2 = 1791.573` ⇒ 差 **0.57px**（那条亚像素差早已记账）」
        /// 是 **A329 之前**的状态；2026-10-12（A329）已把那一行改成 `OptInL`（详见第 8) 节那段与
        /// `OptInL` 的注释）。⚠️ 现在**四边只剩 ≤0.039px 的取整残差**（`CloseL` 取整与 `OptIconW/H`
        /// 少 0.0036/0.0082 那点 —— 见 `OptIconW` 那条「差 0.0036/0.0082」的记账），**不再是 0.57px 的亚像素差**。
        /// 🔴 **2026-10-11（A308）补一条指针**：这一颗的 y 侧**没有**那个毛病（`fy1 = OptIconT` 是**实读绝对值**
        /// `71.18`，与 A308 解出的「上内缩 7.978」自洽），**x 的 0.61px 已于 2026-10-12（A329）收掉** ——
        /// 根因与 A308 同一处（原版子件矩形不居中、左内缩 **8.151** 而非 `(W − w)/2 = 8.763`）。</para>
        ///
        /// <para>⚠️ 邻居 `Switch Deck Info Button` **实测同一套模型**（自己的 `Image` `m_RaycastTarget = 0`、
        /// 两个子件 `= 1` 且 pad 同值）⇒ 那条窄带里**两颗都吃得到**，谁赢看**深度**：解包实读窗根
        /// `Deck info Popup` 的 `m_Children` 顺序 = `… Deck Options(5) → Info Panel(6) → Generic Close Button Orange(7)`
        /// ⇒ **关窗钮是最后一个子件（最深）**，原版在那条窄带里点下去**应当关窗**。
        /// ✅ **2026-10-11（A299）**：那 5 颗 `Deck Options` 的同类外扩**已经做完**（见下面 `OptPad`）——
        /// 那条窄带因此从「我们这侧 12×19」回到**原版的 ≈24×30**（= 两侧子件都外扩）。</para>
        ///
        /// <para>⛔ **别写 `Vector4.zero`**（那就退回 74.386×75.605 = 比原版每边小 ≈20px，就是 A180 本身）。</para></summary>
        public static readonly Vector4 ClosePad = new Vector4(-20f, -20f, -20f, -20f);

        /// <summary>🆕 **2026-10-11（A299）**：`Deck Options` **五颗圆钮**的 `Graphic.m_RaycastPadding`
        /// —— 与上面关窗钮**完全同源**（同一套 prefab 模型、同一个值、同一处踩过的坑）。
        ///
        /// <para>判据 = 解包实读（本包 `bundle_menus_assets_all`，2026-10-11 逐节点走 `m_Children` 核过一整棵）：
        /// `Deck info Popup > Deck Options` 的五个子件 —— `Switch Deck Info Button` · `Duplicate Button` ·
        /// `Share Button` · `Share On Chat` · `Delete Button` —— **每一颗自己的 `Image` 都是
        /// `m_RaycastTarget = 0` · pad 全 0**；吃射线的是它的**两个子件** `Background` / `Icon`
        /// （**两颗都 `m_RaycastTarget = 1` · `m_RaycastPadding = (−20,−20,−20,−20)`**）。
        /// 子件矩形（`python 工具/menu_dump.py bundle_menus_assets_all "Deck Options" --depth 3`）
        /// = **`56.86 × 58.13`**（按钮自身 `74.386 × 75.605`）⇒ **外扩 20 后 = 96.86 × 98.13**。</para>
        ///
        /// <para>🔴 **底取【子件】那一份矩形** —— 实现里就是 `face`/`oicon` 那两个 `Img`
        /// 用的 `fx1, fy1, fx1+OptIconW, fy1+OptIconH`（第 7) 节），⛔ **不是**按钮矩形 `r`：
        /// 加到 `r` 上会算出 `114.386 × 115.605`，比原版**每边多 ≈8.76px**
        /// （= `(74.386 − 56.86)/2` 那道内缩，**与 A180 关窗钮是同一个坑**）。
        /// 🔴 **2026-10-11（A308）**：那两颗子件的 `fx1/fy1` **不是**「按钮矩形里居中」——
        /// 逐字段实读的四个内缩见 `OptInL/R/T/B` 段（左 8.151 / 上 7.978，都不等于居中的 8.763 / 8.7425）。</para>
        ///
        /// <para>⚠️ **外扩后相邻两颗的命中区重叠**（**原版如此**：`96.865 − 93.976` ≈ **2.888px**；
        /// 我们这侧 2.884）⇒ 那条窄带里谁赢 **只能靠兄弟序**（我们这侧靠 z，见第 7) 节那两句）。
        /// ⚠️ **2026-10-11（A308）就地订正**：这句原来把 2.884 归给「子件矩形那 0.6px 的亚像素差」——
        /// 那个**位置**差 A308 已经收掉了（`fx1` 现在与原版同在 `+8.151`）；剩下这 0.004px 是
        /// **`OptIconW` 的四舍五入**（原版解出 `56.8636`，我们常量 `56.86`；
        /// `96.86 − 93.976 = 2.884` vs `96.8636 − 93.976 = 2.8876`）。</para>
        ///
        /// <para>⛔ **别写 `Vector4.zero`**：命中区会退回按钮矩形 `74.386×75.605`（比原版左小 11.8px /
        /// 右小 10.6px / 上下小 ≈10.6px —— 「小 ≈20/边」是**相对子件矩形**说的），正是 A299 本身。</para></summary>
        public static readonly Vector4 OptPad = new Vector4(-20f, -20f, -20f, -20f);

        /// <summary>🆕 **2026-10-11（A180）**：关窗钮命中区**往前挪的 z**（相对量，见 `Build()` 第 8) 节那两句）。
        /// <para>它**不是渲染分层**（那张 quad 全透明、本来就在本窗最上面）—— 它是**命中优先级**的那把尺子：
        /// `Shell/PointerLayer.cs` 的 `HitButton` = **队列大的先**，**同队列再比 z、越小越靠前**。
        /// 本窗 9 颗命中区全在 `QDIHit`(3123)，**必须**有一个 z 才能分出「原版里谁是最深那个子件」。</para>
        /// ⚠️ 只在**同窗同队列**内比较 ⇒ 任何值都行，取一个远小于相邻层的量（不碰渲染、不碰别的窗）。
        /// <para>🆕 **2026-10-11（A299）**：那 **5 颗 `Deck Options`** 用的是**同一把尺子**、但**往后**挪
        /// （`+ HitZFront × (n−1−i)`，i = 原版树序下标）—— 因为原版里关窗钮是窗根**最后一个**子件、
        /// 比**整组** `Deck Options` 都深 ⇒ 这五颗**一律不许往前挪**（往前就会盖过关窗钮、把 A180 那条弄红）。
        /// 语义（原版靠兄弟序）与算式 → `Build()` 第 7) 节那两句。</para></summary>
        const float HitZFront = 0.01f;

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

        /// <summary>🆕 **2026-10-05（A81）**：压暗层的**命中区**节点（「点窗外关窗」）—— 自检用
        /// （`MenuDraw.ShadeRuleOk(darkHit, darkVisual, qContentMin, out why)` 的 `darkHit`；断言入口 =
        /// `MenuDraw.CheckShadeRule`。⚠️ **2026-10-08（A221③）签名订正**：原写第二参 `qShade`（调用方传的常量），
        /// A77⑬③ 起已换成**视觉压暗层** `darkVisual` —— 量的是那颗 quad 的 `RenderQueue`，不是传进来的常量）。
        /// ⚠️ 它是 `BackgroundHit`、**不是** `CloseHit` —— 后者是 **`:847`** 那颗带按钮脸的关窗钮。
        /// 🔴 **2026-10-07（波 4 件① A126）行号订正**：这里原来引的是**两轮之前的旧行号**
        /// （旧号 `701` → `780` → 现 **`:847`**）；那颗节点被同批别的改动推下去过两次，注释没跟着走。
        /// 订正当天实读确认过（`grep -n '"CloseHit"' Shell/DeckInfoPopup.cs`）⇒ ⛔ 别再照抄旧号。</summary>
        public Transform ShadeHit { get { return transform.Find("BackgroundHit"); } }

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

        /// <summary>`Delete Button` 的 `interactable`（原版 `:229-232`：`1 &lt; 卡组数`）。
        /// ⇒ 只剩一套时不给你删。
        ///
        /// <para>🔴 **2026-10-13（A548）就地订正（铁律 5）**：本行后半句原来还写着
        /// 「（**`CollectionData.DeleteDeck` 本来也拒**，这条把它摆到按钮这一层）」—— **那句是错的**：
        /// `CollectionData.DeleteDeck` **从来没有**拒过「只剩一套」（它转调的 `DeckLibrary.Delete` 只判
        /// **下标越界**，删到 0 套也照删）⇒ 「只剩一套不许删」这条规矩**只在这一层**（按钮的
        /// `interactable`），⛔ **别当成数据层也拦着**（数据层现在报的两种失败是「没删（越界）」与
        /// 「删了但没写进存档」，见 `CollectionData.LastDeleteError`）。</para></summary>
        public bool DeleteInteractable { get; private set; }
        /// <summary>`Duplicate Button` 的 `interactable`（原版 `:239-243`：`卡组数 &lt; 上限`）。</summary>
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
        /// 反编译 `DeckInfoControls__Initialize.c:240-243` = `iVar1 &lt; *(int *)(GameStaticData_StaticFields + 0x250)`；
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

        /// <summary>🆕 **A65②**：建窗时按同一个档位名登记的 `WindowButton` —— 灰 / 可点这两个状态位要落到它身上
        /// （`ApplyControlStates` 用）。🔴 与 `_parts` **同生共死**：`Build()` 重建时一起 `Clear`（旧的会随子树被销毁，
        /// 留着就是一堆 fake-null）。</summary>
        readonly Dictionary<string, WindowButton> _wbs = new Dictionary<string, WindowButton>();

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

        /// <summary>判据里有、我们**做不到 / 判不出**的那一处 —— 每次显隐都出声（铁律 11：先记录，别静默）。
        /// ⚠️ 原来这里有**两条**，第 ① 条（`SoftDisable` 的变灰观感）**2026-10-05（A65②）已做完**
        /// （见 `ApplyControlStates` 第 ④ 条 + `CanImportDeck`）⇒ 只剩这一条。</summary>
        void NoteUnimplementedLayers()
        {
            // `state == 1` 时 `Edit Deck` 的文案换成**另一条本地化键**（原版 :84-95，两条键
            //    `DAT_1842d05e0` / `DAT_1842d04e0`）。词条表在**远端 CCD**（本地无 I2 表）⇒ **文案读不到**；
            //    而且我们**没有任何 state==1 的调用点**（原版那条是 `ChatMessageUI.OnMessageClicked`）⇒ 保持英文标签。
            if (State == DeckInfoState.Import)
                Debug.LogWarning("[DeckInfo] state==1（Import）时原版把 `Edit Deck` 的**文案**换成另一条本地化键 —— "
                                 + "本地无 I2 词条表（在远端 CCD）⇒ **文案取不到**，这扇窗仍显示我们那句英文（不许静默）");
        }

        /// <summary>🆕 2026-10-05（§三第29条 A65②）——
        /// **`DeckInfoPopup.CanImportDeck(CardDeck)`** —— 本窗唯一的一份判据，两个消费者：
        /// ① `Edit Deck` 那颗的 `SoftDisable`（`ApplyControlStates` 第 ④ 条）· ② state 1 点它时的错误提示。
        ///
        /// 🔴 **判据原文（逐句亲读 `DF:DeckInfoPopup__CanImportDeck.c`）**：
        /// <code>
        /// if (deck.CustomGameModeEvent == null) return false;                      // :33-35  ← 头一句就返回
        /// // 然后：拿 InventoryManager 的拥有度 → 该 event 的要求件数 + 1 &lt;= 我有的件数（:52）
        /// //       再对 event 的每一个奖励/要求做 All(...)（:48-72）⇒ 全过才 true
        /// </code>
        /// 🔴 **`CardDeck.CustomGameModeEvent` 是什么**（`DF:CardDeck__get_CustomGameModeEvent.c` 亲读）：
        /// `LiveOpsManager.GetHandler&lt;GameModes&gt;().GetActiveEvent(deck.gameMode)` ——
        /// `deck.gameMode` 是 `Nullable&lt;PlayModes&gt;` @0x70（value @0x74，`dump.cs` 字段序坐实），
        /// 而 `GameModes.GetActiveEvent` 是「在配置表里筛出**该模式当下正在跑的活动**，`FirstOrDefault`」
        /// ⇒ **没有正在跑的活动时它是 null**。
        /// ⇒ **我们恒 `false`**：本工程**没有 LiveOps / 活动系统**（配置表在远端 CCD，且我们不做真实经济）
        /// ⇒ 第一条就返回 false。**这不是猜的**，是判据的第一句照抄。
        /// ⛔ **不许**改成 `true` / 或者「返回 DeckRules.Validate」之类 —— 那是编一个原版没有的判据（铁律 3）；
        /// `ValidateDeck` 是 `Practice Deck` 那颗的判据（`ApplyControlStates` 第 ① 条），**不是这条**。
        ///
        /// 🔴 **连带后果（照实说）**：`SoftDisable(!CanImportDeck(...))` **没有 state 守卫**
        /// （`DeckInfoControls__Initialize.c:76-83`：`SetActive(state&lt;2)` 之后**无条件**调），
        /// 而 `editButton` 的 `colorTintGreyOnDisable`（`EverguildButton` 的 `0x17A`）= **1**
        /// （真包实读 MB `-8697463422759302744`，真包 GO `Edit Deck`，父链 `Edit Deck/Buttons/Deck info Popup`）
        /// ⇒ **原版在 state 0/1 下确实会把它画成灰的**（灰＝子树图形件的材质换成 `Everguild/UI/Greyscale`，
        /// 见 `Shell/PromptPopup.cs` 的 `WindowButton` 头部）。我们照抄 ⇒ **这颗钮一直是灰的、但点得动**
        /// （`SoftDisable` **不**改 `interactable`，`DF:EverguildButton__SoftDisable.c` 亲读）。</summary>
        public static bool CanImportDeck(RuleEngine.PlayerDeck deck)
        {
            // 判据第一句：`CustomGameModeEvent == null ⇒ false`；我们这边**没有任何**带活动模式的卡组。
            if (deck == null) return false;
            Debug.Log("[DeckInfo] `CanImportDeck` = **false**（判据 = 原版那一句 "
                      + "`deck.CustomGameModeEvent == null ⇒ return 0`；我们的等价物 = 「没有任何活动模式的卡组」"
                      + "，因为我们**没有 LiveOps 活动系统** —— 见本方法的注释）");
            return false;
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
        /// ✅ **2026-10-05（A65②）两条都落地了**：**变灰**走 `WindowButton.Interactable` / `SetSoftDisabled`
        /// （原版同一个机制：`EverguildButton.DoStateTransition(Disabled)` → 材质换 `Everguild/UI/Greyscale`，
        /// 判据见 `Shell/PromptPopup.cs` 的 `WindowButton` 头部）；**挡派发**由 `WindowButton.Click()` 做
        /// （仍然出声）。`Blocked()` 那一层**保留**（同一份布尔、同一处算出来的 —— 不是第二份判据）。</summary>
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
                          // 🔴 **2026-10-18（A985①）**：`DeckRules.Describe` 只出**词条键** ⇒ 这里必须过
                          //   `Loc.T` 才印得出人话（先例 = `DeckRuntime.DeckErrorText`）。⛔ 别学它印键名。
                          + (deck == null ? "这一格没有卡组"
                                          : Loc.T(DeckRules.Describe(DeckRules.Validate(deck, CollectionData.Card, skirmish))))
                          + " ⇒ 点了不生效 + **变灰**（两半都接了：`WindowButton.Interactable`，见下面第 ④ 段的说明）");

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

            // ④ 🆕 **A65②：`Edit Deck` 那颗的 `SoftDisable`** —— 原版 `:79-83`（**无 state 守卫**）：
            //    `cVar3 = CanImportDeck(popup, context.Deck);  EverguildButton__SoftDisable(editButton, cVar3 == '\0');`
            //    🔴 语义 = **只变灰、不改 `interactable`** ⇒ **灰着也照样点得动**（判据见 `CanImportDeck` 的注释）。
            //    ⚠️ 前提：那颗钮的 `colorTintGreyOnDisable`（`EverguildButton` 的 `0x17A`）= **1**（真包实读）
            //       —— 不为真时原版这一段**什么都不做**，那就成了「照抄一个不生效的调用」，所以这一条必须核。
            bool canImport = CanImportDeck(deck);
            SetSoftDisabled("Btn:Edit Deck", !canImport);

            // ⑤ 🆕 **A65② 顺手补齐**：①③ 那三颗的 `interactable` **落到按钮自己身上**（原来只在 `Blocked` 里挡动作）。
            //    判据：原版这三颗的 `colorTintGreyOnDisable` **也全是 1**（真包实读 MB：
            //    `practiceButton 6400377646024133032` / `deleteButton -4569730492560078424` /
            //    `duplicateButton 3454378354448042408`）⇒ 原版在它们不可交互时**同样会画成灰的**。
            SetInteractable("Btn:Practice Deck", PracticeDeckInteractable);
            SetInteractable("Opt:Delete", DeleteInteractable);
            SetInteractable("Opt:Duplicate", DuplicateInteractable);

            // ⑥ 🆕 **2026-10-05（A85）：变灰是「换材质」⇒ 换完把这 8 颗钮的显式队列补回去。**
            //    判据链（三段都可查）：
            //      ① `WindowButton.Interactable` 的 setter 与 `SetSoftDisabled` **都走 `RefreshGray()`**
            //         （`Shell/PromptPopup.cs`），而它换材质用的是 `ImageQuad.SetMaterial`；
            //      ② 它建的是 `new Material(sh)` ⇒ 队列退回 **shader 自带的那一个**，而
            //         `Everguild/UI/Greyscale` 的 SubShader 标签是 `QUEUE: Transparent` = **3000**
            //         （`工具/dump_shader.py "Everguild/UI/Greyscale"` 实读，2026-10-05）；
            //      ③ 本窗红底 `UI_Deck_Information_Back` 在 **`QDI = 3120`**、这几颗钮在 `QDIRow = 3121`
            //         ⇒ 不补这一句，变灰那几颗会**掉到红底之下** = 画面上「按钮没了」，
            //         而 `WindowButton.AuditGrayLook` 只核 **shader 名** ⇒ **照样全绿**（弱断言分不出两种状态）。
            //    ⚠️ **与 `Shell/MissionsTab.cs:850` 那 6 颗是同一做法**，但那一处的注释写着「通用修法 = 让
            //       `SetMaterial` 保留 `renderQueue`」—— A85 **已经落到通用那一层了**；这一句留作**第二道**：
            //       通用修法保的是**相对值**（调用前那一份），这一句钉的是**绝对值**（这些钮本来就该在的档）。
            //    🔴 **必须在状态翻转【之后】调** —— `RefreshGray` 是同步换材质的，写在建窗那一段等于写进旧材质
            //       （正是 `MissionsTab` 那处能生效、而「建完再统一扫一遍」不能生效的原因）。
            ReassertButtonQueues();

            Debug.Log("[DeckInfo] 接线层（原版 `DeckInfoControls.Initialize` 末尾那三条 + A65② 的 `SoftDisable`）："
                      + "`Practice Deck` 可点 = " + PracticeDeckInteractable
                      + " · `Delete` 可点 = " + DeleteInteractable + "（卡组数 " + n + " > 1）"
                      + " · `Duplicate` 可点 = " + DuplicateInteractable + "（卡组数 " + n + " < 上限 " + MaxCustomDecks + "）"
                      + " · `Switch Deck Info` 的 `isOn` = " + DrawerToggleIsOn + "（`SetIsOnWithoutNotify(true)`）"
                      + " · `Edit Deck` 的 `softDisabled` = " + (!canImport)
                      + "（`!CanImportDeck(deck)`，判据见 `CanImportDeck` —— **这颗灰着也点得动**）");
        }

        /// <summary>按建窗时登记的名字取那颗 `WindowButton`（🔴 没建出来/名字拼错 ⇒ **出声**，别静默）。</summary>
        WindowButton WbOf(string key)
        {
            WindowButton wb;
            if (_wbs.TryGetValue(key, out wb) && wb != null) return wb;
            Debug.LogWarning("[DeckInfo] 没登记过这颗钮：「" + key + "」⇒ 它的灰/可点状态**没落地**（不许静默）");
            return null;
        }

        void SetSoftDisabled(string key, bool on)
        { var wb = WbOf(key); if (wb != null) wb.SetSoftDisabled(on); }

        void SetInteractable(string key, bool on)
        { var wb = WbOf(key); if (wb != null) wb.Interactable = on; }

        /// <summary>🆕 **2026-10-05（A85）**：把登记过的 8 颗钮的**显式渲染队列**重新钉一遍 ——
        /// 变灰（`RefreshGray`）是**换材质**，换完新材质只剩 shader 自带的 `Transparent(3000)`
        /// ⇒ 本来在 `QDIRow`/`QDIHit` 的那几张会掉到红底（`QDI`）之下。调用点与判据链 → `ApplyControlStates` 第 ⑥ 段。
        /// <para>两层件各按它建窗时的档：**可见那一张**（`WindowButton.target` = `Bg …` / `Face …`）在 `QDIRow`、
        /// **命中区那一张**（按钮自己的子 quad `Hit`）在 `QDIHit` —— 与 `Build()` 里建它们时给的是**同一对常量**。
        /// ⚠️ 这个「`target` + 子树」的分组**与 `PromptPopup.GrayTargets()` 是同一套**（变灰会同时换这两张）
        /// ⇒ 两张都得钉，否则「可见的补回来了、命中区还掉着」（命中区全透明，肉眼与截图都看不出来）。</para></summary>
        void ReassertButtonQueues()
        {
            foreach (var kv in _wbs)
            {
                var wb = kv.Value;
                if (wb == null) continue;
                if (wb.target != null) wb.target.SetRenderQueue(QDIRow);
                foreach (var q in wb.GetComponentsInChildren<ImageQuad>(true))
                    if (q != null && q != wb.target) q.SetRenderQueue(QDIHit);
            }
        }

        /// <summary>`Selectable.interactable == false` 的等价物（**第二道**）：**动作照进来，但立刻返回**。
        /// 返回 true = 这一下**不该生效**（调用方 `return`）。
        /// 🔴 **2026-10-05（A65②）起，第一道闸在 `WindowButton.Click()`**（`Interactable == false` 时
        /// 连派发都没有，原版 `Selectable.OnPointerClick` 头一句就是那个语义）⇒ 这一段**正常不再被走到**；
        /// **留着**是因为它花的是**同一份布尔**（`PracticeDeckInteractable` 等，`ApplyControlStates` 里算一次），
        /// 不是第二份判据 —— 而 `WindowButton` 那一层万一被别处改成可交互，这里仍然挡得住 + 出声。</summary>
        bool Blocked(string key, bool interactable)
        {
            if (interactable) return false;
            Debug.LogWarning("[DeckInfo] `" + key + "` **点了不生效** —— 原版这颗钮是 `interactable = false`"
                             + "（判据 → `ApplyControlStates` 的注释）：那颗钮既不吃点击、又是灰的。"
                             + "两半都接了（`WindowButton.Interactable` 变灰 + 挡派发）；"
                             + "走到这一行说明第一道闸没拦住，**出声**（不许静默）。");
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
            _wbs.Clear();            // 🆕 A65②：同上（这两个表必须同生共死，见 `_wbs` 的注释）

            var info = CollectionData.DeckAt(DeckIndex);

            // 1) 压暗整屏（原版 rect 比屏幕大 ⇒ 我们直接铺满可见区；断言量的是「铺满」）
            Solid(root, root, 960f, 540f, 1920f, 1080f, ShadeColor, QDI, "Menu Dark Background");
            // 🔴 **2026-10-05（A81）**：压暗层的**点击区**（「点窗外关窗」）—— **原来漏了**（本窗只建了压暗层）。
            //   判据：原版这一层不是独立节点，而是压在 `Menu Dark Background` **自身节点**上的
            //   `BackgroundCloseButton`；挂/摘在窗口类自己身上（`DeckInfoPopup__Open.c:135-146` 挂 ·
            //   `OnDisable.c:32-42` 摘 —— 预制体里 `window` 的 pid 恒 0、`onClick` 持久调用表全空）。
            //   档 = **压暗层自己那一档 `QDI`(3120)**，**严格低于**本窗内容命中区档 `QDIHit`(3123)
            //   （同档时谁吃到命中退化成枚举顺序 ⇒ 症状是「点不动的钮看着像正常工作」）。
            //   ⚠️ 它与 **`:847`** 那颗 `CloseHit` **不是一件事**：那是**带按钮脸的关窗钮**，两颗都要有。
            //      （`:847` 是 **2026-10-07（波 4 件① A126）**实读订正的行号 —— 这里与上面 `ShadeHit`
            //       那处原来都引旧号 `701`，那颗节点已被推下去过两次。）
            //   出处 → `资料/待办判据_阶段二与联机.md` §A81 · 公共件规矩 → `MenuDraw.ShadeHit` 的注释。
            MenuDraw.ShadeHit(root, new PxRect(0f, 0f, 1920f, 1080f), QDI, QDIHit, () => Close(), "BackgroundHit");

            // 2) 红底（Sliced）
            Nine(root, root, "UI_Deck_Information_Back", RedBorder, RedTexW, RedTexH,
                 RedL, RedT, RedR, RedB, QDI, "Generic Window Red Background Big");
            // 🆕 **2026-10-06（A94）：红底那块面板吸收点击**。判据 = 原版 prefab
            //   `Deck info Popup > Generic Window Red Background Big` 那颗 `Image` 的
            //   **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读）—— 射线打到它自己、
            //   父链上没有点击处理器（关窗那颗 `BackgroundCloseButton` 挂在压暗层上）⇒ 原版**什么都不做**。
            //   ⚠️ 本窗是调度台点名的那一例：**压暗底 / 红底 / `Info Panel` 全在 `QDI`(3120)**，
            //   而「点窗外关窗」也传 `QDI` ⇒ 压低档时赢家退化成枚举顺序；吸收层因此**不能**沿用
            //   面板那一档，必须由公共件算（`qContentMin − 1`）。红底盖住了 `Info Panel` ⇒ 一块就够。
            MenuDraw.Absorb(root, "AbsorbHit", new PxRect(RedL, RedT, RedR, RedB), QDI, QDIHit);

            // 3) 督军立绘（原版 sprite=0 运行时喂）
            var wl = CollectionData.Warlord(DeckIndex);
            var wlTex = wl != null ? CardArt.PortraitByName(wl.Name) : null;
            if (wlTex != null)
            {
                // 🔴 **2026-10-07（波 4 件① A142①）：`keepAspect` 改回原版语义 = 【拉伸】。**
                //   判据 = 原版 prefab 那颗 `Image` 的 **`m_PreserveAspect = 0`**（+ `m_Type = 0` Simple ·
                //   rect 1108²）—— 出处 `bundle_menus_assets_all/MonoBehaviour/`
                //   `MonoBehaviour_-7131536541767857752.json`（`m_GameObject` 指回
                //   `GameObject/Warlord Image_-6735770576364533336.json`）。
                //   原来是 `true` ⇒ `Img` 会**内接留边**（按贴图自己的宽高比缩），只在图是方的时等价。
                //   ✅ **督军的图不是方的（2026-10-07 实测）**：`Assets/CardPresentation/Resources/Art/cards/` 下
                //      全库 **1153** 张 `art_*.png` 逐张读 PNG 头量过 = **668 张 671×1024** ·
                //      **483 张 1024×1024** · 1 张 670×1024 · 1 张 512×512；
                //      而**督军那一族落在 671×1024 那一档（非方）** ——
                //      57 张 `subtype == "Warlord"` 的卡里 **56 张的 `art_<id>.png` 就是 671×1024**（逐张读 PNG 头量过），
                //      唯一的例外 `GOF_Da_Bigger_Dey_Iz_Mozrog_s_Talent` 是 1024²（**那张是天赋卡、不是督军**，
                //      方形 ⇒ 两种取值本来就等价）；103 副预组里 **96 副有 `heroId`，96/96 全是 671×1024**
                //      （另 7 副 `heroId` 为空 ⇒ 不画这一层）。
                //      ⇒ `true` 时贴进 1108² 会左右各留 ≈ (1108 − 1108×671/1024)/2 ≈ **191px** 空白，
                //      原版是**拉满 1108²**。⚠️ 观感差归 `资料/真Play待验清单.md`（批处理只能量几何、看不了像不像）。
                //   🔴 **这一行只改【渲染】**（`Img` 的 `keepAspect`）—— **命中区与它无关**（由下一段的
                //      `WarlordPad` 决定，⛔ 别把两者写进同一条断言；自检那边是两条分开的）。
                Img(root, root, wlTex, WarlordL, WarlordT, WarlordR, WarlordB, "Warlord Image", QDI, false);
            }
            else
                Debug.Log("[DeckInfo] 督军立绘取不到（" + (wl != null ? wl.Name : "没有督军") + "）—— 那一层不画，出声");
            // 🆕 A10：`Warlord Image` 那一层**本来就是 `EverguildButton`**（原版点了开卡详情窗）——
            //    原来记的「那扇窗还没建」是**过期**的（`CardDetailPopup` 2026-09-24 就建好了）⇒ 接上。
            //
            // 🔴 **2026-10-06（FIX-1）命中区要过原版的 `m_RaycastPadding`**（`WarlordPad`）——
            //    **矩形本身没写错**（原版那颗 `Image` 的 rect 逐字就是 `WarlordL/T/R/B`，1108²），
            //    错的是**我们把它整个当成了命中区**：
            //      · 原版这颗 `m_RaycastTarget = 1`、rect 盖住 **x∈[−108.98, 999.02] × y∈[−33.99, 1074]**
            //        —— **比屏还大**（含屏幕左上角、含大半块暗底），而它是窗根的第 3 个子件
            //        ⇒ 在 UGUI 里**排在暗底之后**（= 盖在暗底上）。
            //      · 不缩的后果**不是观感**：点「窗外的暗底」会被它吃掉 ⇒ 那个点**关不掉窗**，
            //        改成开督军的卡片详情窗（本工程 2026-10-06 的自检就是这么红的：
            //        `CollectionScene.Run` 在 (5,5) 拿到 `WarlordHit`、窗不关，后面三条被那扇
            //        一直开着的详情窗顶掉）。
            //      · 缩完 **(137.82, 98.39) → (660.42, 989.56)**（522.6 × 891.18）—— 四条边正好收在窗口里：
            //        左 137.82 > 红底左 134.50 · 右 660.42 ≈ `Info Panel` 左 659.0 · 上 98.39 > 红底上 82
            //        · 下 989.56 < 红底下 1032 ⇒ 语义 = 「**窗口内那块立绘**才可点」。
            //    ✅ **符号判据（本机能读到，不是推断）**：UGUI 里 `Graphic.m_RaycastPadding` 与
            //       `RectMask2D.m_Padding` **喂的是同一个 `offset` 形参**
            //       （`RectTransformUtility.RectangleContainsScreenPoint`），而唯一能读到符号的实现是
            //       `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Culling/Clipping.cs`
            //       的 `FindCullAndClipWorldRect`：`xMin = current.xMin + offset.x` · `xMax = current.xMax − offset.z`
            //       · `yMin + offset.y` · `yMax − offset.w` ⇒ **正分量把四边往里推**；紧接着那句
            //       `validRect = xMax > xMin && yMax > yMin`（正 padding 能把遮罩算成空）只有「缩」才可能触发。
            //    ⇒ 走公共件 `MenuDraw.PaddedHitRect`（**同一个符号约定，别在这里再抄一遍算式**），
            //      全 0 时它原样返回 ⇒ 对没 padding 的节点零影响。
            Hit(root, root, "WarlordHit",
                MenuDraw.PaddedHitRect(new PxRect(WarlordL, WarlordT, WarlordR, WarlordB), WarlordPad),
                OpenWarlordDetail);

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
            // 空战将位那行字走词条（键 `MenuDeck/Error/NoWarlord`）。
            // ⚠️ 中文列是「还没有选战将」—— 与改前写死的「未选战将」**不同字**，这是**有意的**：
            //    施工单 §附_Shell #1 判「语义一致 ⇒ 复用，别新造」（铁律 6：同义两键迟早不一致）。
            Txt(root, root, wl != null ? wl.Name : Loc.T("MenuDeck/Error/NoWarlord"), DdWlL, DdWlT, DdWlR, DdWlB, 40f, Align.Left,
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
                    //    `Local3(basis, …)` 算的是 `RectCenter(绝对px) − basis.position`
                    //    （🆕 **2026-10-11（A306②）**：`basis.position` 那一项现在走
                    //     `MenuDraw.PosInDesignSpace(basis)` —— **与 `MenuDraw.Local` 逐字同源**；
                    //     `k == 1` 时与旧写法逐位相同，别据此以为两者还分家），
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
                    // 🆕 A65②：登记这颗的 `WindowButton`（`Edit Deck` 的 `SoftDisabled` / `Practice Deck` 的
                    //   `interactable` 都要落到它身上）
                    _wbs["Btn:" + key] = hitq != null ? hitq.GetComponent<WindowButton>() : null;
                }
            }

            // 7) `Deck Options`：五个圆钮 · HLG spacing **−50** · align **MiddleRight(5)** · 🔴 **`m_ReverseArrangement = 1`**
            //    ⇒ **视觉左→右 = 树序【倒排】**（判据 = uGUI 源码逐行读过：
            //      `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/HorizontalOrVerticalLayoutGroup.cs`
            //      `:152-155` `startIndex = reverse ? Count−1 : 0` / `endIndex = reverse ? 0 : Count` / `increment = reverse ? −1 : 1`
            //      `:207` 起循环体里 `pos` 从**主轴起点**递增、`SetChildAlongAxis(child[startIndex], pos)`
            //      ⇒ 树序**最后一个**子件落在**最左**）：
            //      `Delete` → `Share On Chat` → `Share` → `Duplicate` → `Switch Deck Info`
            //    ⚠️ **2026-10-05 就地订正（铁律 5）**：**原文**（2026-10-03「A10」）写的是
            //      「原版左→右 = **树序**（`Switch Deck Info` → … → `Delete`）……那条『`reverse=1` ⇒ 与树序相反』
            //       **实测不成立**」—— **那次订正本身是错的**。**错因**：当时用的读数出自**还不建模
            //      `m_ReverseArrangement` 的 `工具/menu_dump.py`**（输出的是「正序 + 模板位」那一套 = **镜像读数**）
            //      ⇒ A10 把一处**本来正确**的实现（`Delete` 最左）改成了错的，并把这句话抄进了
            //      `Editor/CollectionScene.cs` 的两条 `CheckNear`（**同批已订正**）。
            //      **实况**：`menu_dump.py` 2026-10-05 起已建模该字段（`--verify-layout` 带倒排回归用例），
            //      输出与上面那份 uGUI 源码逐位一致 —— **别再按「树序 = 视觉序」改回去**。
            //    🔴 **2026-10-05 重取（A88）**：那排**绝对 x 也随之作废**（旧值 `1611.7 / 1636.1 / 1660.5 / 1684.8 / 1709.2`
            //      是「总 flexible = 0」那套旧模型的输出）。跑后真值（左→右）：
            //      `Delete` **1333.33** · `Share On Chat` **1427.31** · `Share` **1521.28** · `Duplicate` **1615.26**
            //      · `Switch Deck Info` **1709.23**；**步进 93.976**（= `childSize 143.976 + spacing(−50)`），**不是** 24.386。
            //      算式（判据 = 上面那份 uGUI 源码 `:186-216`）：
            //        组宽 519.88（组矩形 **1263.74,93.00 → 1783.62,243.00**）· 5 颗各 `sizeDelta.x = 74.386`
            //        · `总首选 = 5×74.386 + 5×(−50) − (−50) = 171.93`
            //        ⇒ `surplus = 347.95`、`总 flexible = 5` ⇒ `fmul = 69.59` ⇒ 每格 `childSize = 143.976`、
            //        `offsetInCell = (143.976 − 74.386) × 1 = 69.59`（`align=5` ⇒ `alignmentOnAxis = 1`）
            //        ⇒ 最左那颗左缘 = 组左沿 1263.74 + 69.59 = **1333.33**；最右那颗右缘 = **1783.62**（**正好贴组右沿**）。
            //    ⇒ 实现：`opts[]` **保持原版树序**（= 原版 `DeckInfoControls` 的名字顺序），**按下标倒排**摆位
            //      （`x1 = optX0 + (n−1−i)·OptStep`）—— 这样「倒排」这层语义在代码里看得见，不是靠把数组抄反。
            //      重取命令：`python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Deck info Popup" --depth 12 --md`
            {
                var holder = new GameObject("Deck Options");
                holder.transform.SetParent(root, false);
                float total = OptW + (5 - 1) * OptStep;                  // 450.29（= 最左左缘 → 最右右缘）
                holder.transform.localPosition = Local3(root, OptR - total, OptT, OptR, OptT + OptH);
                string[][] opts =
                {
                    new[] { "Switch Deck Info", "40k_general_bt_yellow_seedeck" },
                    new[] { "Duplicate",        "40k_general_bt_yellow_duplicate" },
                    new[] { "Share",            "40k_general_bt_yellow_share" },
                    new[] { "Share On Chat",    "40k_general_bt_yellow_share in chat" },
                    new[] { "Delete",           "40k_general_bt_yellow_delete" },
                };
                float optX0 = OptR - total;                              // 1333.33
                for (int i = 0; i < opts.Length; i++)
                {
                    // 🔴 倒排：树序第 `i` 个落在**第 (n−1−i) 格**（原版 `m_ReverseArrangement = 1`）。
                    float x1 = optX0 + (opts.Length - 1 - i) * OptStep;
                    var r = new PxRect(x1, OptT, x1 + OptW, OptT + OptH);
                    var obg = Img(holder.transform, holder.transform, CardArt.MenuUi("UI_Button_Round_background"),
                        r.x1, r.y1, r.x2, r.y2, "Bg " + opts[i][0], QDIRow, true);
                    // 🔴 **2026-10-11（A308）**：子件矩形的**四个内缩逐字段实读**、**不是**「居中」——
                    //    原来写的是 `(OptW − OptIconW)/2 = 8.763` / `(OptH − OptIconH)/2 = 8.7425`，
                    //    而原版解出来是 **左 8.151 / 右 9.371 / 上 7.978 / 下 9.498**（不居中！）
                    //    ⇒ `Face`/`Icon`/命中区整块偏 +0.612(x) / +0.764(y)。判据与算式 → `OptInL` 那段。
                    //    改坏法：把这两句退回 `(OptW − OptIconW) * 0.5f` / `(OptH − OptIconH) * 0.5f`
                    //      ⇒ `Editor/CollectionScene.cs` 的「子件矩形中心 − `Bg` 中心 = (−0.610, −0.760)」
                    //      两条（相对量、容差 0.3）与命中区四边那几条（容差 0.3）一起红。
                    float fx1 = r.x1 + OptInL;
                    float fy1 = OptT + OptInT;
                    var face = Img(holder.transform, holder.transform, CardArt.MenuUi("40k_general_bt_yellow"),
                        fx1, fy1, fx1 + OptIconW, fy1 + OptIconH, "Face " + opts[i][0], QDIRow, true);
                    var oicon = Img(holder.transform, holder.transform, CardArt.MenuUi(opts[i][1]),
                        fx1, fy1, fx1 + OptIconW, fy1 + OptIconH, "Icon " + opts[i][0], QDIRow, true);
                    string key = opts[i][0];
                    // A17：`Deck Options>…` 五颗都是 SpriteSwap（普查 §块 3 第 7 行）
                    // 🔴 **2026-10-11（A299）**：命中区按原版**外扩** —— 带 `m_RaycastPadding = (−20,…)` 的是
                    //    **两个子件**（`Background`/`Icon`），原版每颗按钮**自己的 `Image` 都不吃射线**
                    //    （`m_RaycastTarget = 0`）⇒ 射线那一面 = **子件矩形**外扩 20（判据与复算 → `OptPad` 那段）。
                    //    外扩后用 `Face`/`Icon` 那一份矩形（`fx1,fy1,fx1+OptIconW,fy1+OptIconH`）做底 ——
                    //    **不是** `r`（加到按钮矩形上会比原版**每边多 ≈8.76px**；去掉外扩则每边小 10.6~11.8px）。
                    var ohit = Hit(holder.transform, holder.transform, "Opt_" + key,
                        MenuDraw.PaddedHitRect(
                            new PxRect(fx1, fy1, fx1 + OptIconW, fy1 + OptIconH), OptPad),
                        () => OnOption(key), face, "40k_general_bt_yellow");
                    // 🔴 **2026-10-11（A299）**：外扩之后**相邻两颗的命中区重叠 ≈2.888px**（原版如此）——
                    //    谁赢**只能靠兄弟序**。原版判据（解包实读 + uGUI 源码）：
                    //      · `Deck Options` 的子件树序 = `Switch Deck Info Button → Duplicate Button →
                    //        Share Button → Share On Chat → Delete Button`（本文件 `opts[]` 就是照它抄的）；
                    //      · uGUI 挑赢家 = **深度大者先**（`EventSystem.cs:239-240`
                    //        `return rhs.depth.CompareTo(lhs.depth)`；`Graphic.depth` = `canvasRenderer.absoluteDepth`
                    //        = 层级遍历序）⇒ **树序靠后的那颗赢** = **视觉上靠左的那一颗**。
                    //    我们这侧「谁压谁」= **队列 → z**（`Shell/PointerLayer.cs` 的 `HitButton`：
                    //    同队列再比 z、**越小越靠前**），而五颗**全在 `QDIHit`、z 又全是 0** ⇒ 打平后落到
                    //    `FindObjectsByType` 的返回序上（**不保证**）⇒ 把「树序靠后 = 更靠前」显式写出来。
                    //    ⚠️ **一律往后挪（正 z）**：关窗钮那颗已经被 A180 前移到 `−HitZFront`
                    //      （原版它是窗根**最后一个**子件 ⇒ 比**整组** `Deck Options` 都深），
                    //      这五颗往前挪就会盖过关窗钮、把 A180 那条「关窗钮更深」弄红。
                    //    ⚠️ 相对量、**不改渲染**（这张 quad 全透明，`MakeHitQuad` 给的就是 `(0,0,0,0)`）。
                    //    改坏法：删掉下面这两句 ⇒ 那 2.888px 里谁赢变成**看运气**
                    //      （`Editor/CollectionScene.cs` 的「重叠带里靠左那颗赢」与「深度序 = 树序倒排」会红）。
                    if (ohit != null)
                        ohit.localPosition += new Vector3(0f, 0f, HitZFront * (opts.Length - 1 - i));
                    // 🆕 A31：五颗里除 `Switch Deck Info`（常显）外都按 state 显隐
                    Track("Opt:" + key, obg, face, oicon, ohit);
                    // 🆕 A65②：`Delete` / `Duplicate` 的 `interactable` 要落到按钮身上（见 `ApplyControlStates` 第 ⑤ 段）
                    _wbs["Opt:" + key] = ohit != null ? ohit.GetComponent<WindowButton>() : null;
                }
            }

            // 8) 关闭圆钮
            {
                var r = new PxRect(CloseL, CloseT, CloseR, CloseB);
                Img(root, root, CardArt.MenuUi("UI_Button_Round_background"), r.x1, r.y1, r.x2, r.y2,
                    "Close Bg", QDIRow, true);
                float fx1 = r.x1 + OptInL, fy1 = OptIconT;
                // ✅ **2026-10-12（A329）就地改掉**：`fx1` 原来写的是**居中**（`(r.W − OptIconW) * 0.5f` = `+8.763`），
                //    而原版关窗钮的子件矩形**不居中** —— 左内缩 **8.151**、右 **9.371**（与那五颗 `Deck Options`
                //    **同一套 prefab 模型、逐值相同**，判据与算式 → `OptInL` 那段；pid 见
                //    `资料/普查产出_1011/W7_子5.md` §五·1：`Background -1132451123836099456` /
                //    `Icon -4258212455239654272`）。
                //    ⚠️ 改之前差 **0.612px**（我们 `CloseL + 8.763 = 1791.573` vs 原版 `1791.00`）——
                //    同时 `ClosePad` 是加在**这份子件矩形**上的 ⇒ 命中区四条边也一起偏 0.612。
                //    ⇒ 现在 = `1770.961 / 51.18 / 1867.821 / 149.30`，与原版（`1771.0 / 51.2 / 1867.8 / 149.3`）
                //    差 **≤0.039px**（左 0.039 / 上 0.020 / 右 0.021 / 下 0.000；改前 0.573/0.633）⇒ `Editor/CollectionScene.cs` 那**四条边**的容差
                //    同批从 **1.5 收到 0.3**（0.02 的残差 vs 0.61 的缺陷，0.3 正夹在中间）。
                //    ⚠️ **y 侧没有这个毛病**（`fy1 = OptIconT = 71.18` 是**实读绝对值** = `CloseT 63.20 + 7.978`
                //    = A308 解出的上内缩，两边自洽）⇒ ⛔ 别顺手「一起改成居中/一起改 `OptInT`」。
                //    **改坏法**：把这一句退回 `r.x1 + (r.W - OptIconW) * 0.5f` ⇒
                //      `Editor/CollectionScene.cs` 关窗钮命中区那四条边（容差 0.3）**四条一起红**。
                var closeFace = Img(root, root, CardArt.MenuUi("40k_general_bt_yellow"),
                    fx1, fy1, fx1 + OptIconW, fy1 + OptIconH, "Close Face", QDIRow, true);
                Img(root, root, CardArt.MenuUi("40k_general_bt_yellow_close"),
                    fx1, fy1, fx1 + OptIconW, fy1 + OptIconH, "Close Icon", QDIRow, true);
                // A17：`Generic Close Button Orange` 是 SpriteSwap（普查 §块 3 第 8 行）
                // 🔴 **2026-10-11（A180）**：命中区按原版**外扩** —— 带 `m_RaycastPadding = (−20,…)` 的是
                //    **两个子件**（`Background`/`Icon`），原版那颗按钮**自己的 `Image` 不吃射线**
                //    （`m_RaycastTarget = 0`）⇒ 射线那一面 = **子件矩形**外扩 20（判据与复算 → `ClosePad` 那段）。
                //    外扩后用 `Close Face`/`Close Icon` 那一份矩形（`fx1,fy1,fx1+OptIconW,fy1+OptIconH`）做底 ——
                //    **不是** `r`（加到按钮矩形上会比原版每边多 ≈9px）。
                var closeHitNode = Hit(root, root, "CloseHit",
                    MenuDraw.PaddedHitRect(new PxRect(fx1, fy1, fx1 + OptIconW, fy1 + OptIconH), ClosePad),
                    () => Close(), closeFace, "40k_general_bt_yellow");
                // 🔴 **2026-10-11（A180）**：外扩之后这条窄带**与邻居重叠**（与 `Opt_Switch Deck Info`）。
                //    ⚠️ **2026-10-11（A299）就地订正**：这段原来写「我们这侧 ≈ 12×19 px、原版 ≈ 24×30 px」——
                //    那是**当时**读数（邻居那 5 颗还没做外扩）；A299 把那 5 颗也外扩之后，我们这侧
                //    与**原版同尺** —— 那条带 = `x ∈ [1771.57, 1794.86] × y ∈ [118.94, 149.30]` ≈ **23.3×30.4 px**
                //    （两侧的子件矩形都外扩 20，原版 ≈ 24×30）。
                //    ✅ **2026-10-12（A329）就地订正【x 那半】**：上面那个 x 区间是 **A308 之前**的两个端点 ——
                //    A308 把**邻居**那颗的内缩从「居中 8.763」改成 **8.151** ⇒ 邻居的右沿 `1794.86 → 1794.25`；
                //    A329 把**本颗**同样改了 ⇒ 本颗的左沿 `1771.57 → 1770.96`
                //    ⇒ 现在 = **`x ∈ [1770.96, 1794.25]`（宽 23.28 ≈ 同一条 23.3px 的带）**。
                //    ⚠️ **y 那半本件没复核**（照原文留着）—— 本件按 `OptIconT/OptIconH` 复算的两颗 padded
                //    矩形的 y 交叠是 `[51.18, 149.30]`（98.12px），与原文那个 `[118.94, 149.30]`（30.4px）
                //    **对不上** ⇒ 已记进 `H4_每日重置与小件.md` 的「没查清」，⛔ 别照抄也别顺手改。
                //    原版靠**深度**定胜负：
                //    解包实读窗根的 `m_Children` 顺序 —— 关窗钮是**最后一个**子件（`Deck Options` 之后）
                //    ⇒ 那条带里点下去**关窗**。我们这一侧的「谁压谁」是**队列 → z**（`PointerLayer.HitButton`：
                //    同队列比 z、**越小越靠前**），而本窗 9 颗命中区**全在 `QDIHit`、z 又全是 0** ⇒ 打平，
                //    胜负落在 `FindObjectsByType` 的返回序上（**不可靠**）。⇒ 把「最深」这件事显式写出来：
                //    本节点 z 往前挪一点点（相对量，不改渲染 —— 这张 quad 全透明、本就在本窗最上面）。
                //    🔴 **A299 起这条前移还担着第二件事**：那 5 颗 `Deck Options` 现在**一律往后**挪
                //    （`+HitZFront×(n−1−i)`，第 7) 节）⇒ 它们的 z ∈ [0, +0.04]，**都排在这颗（−0.01）之后**，
                //    与「关窗钮是窗根最后一个子件」逐位自洽（⛔ 别把任何一颗往后挪改成往前挪）。
                //    ⚠️ 改坏法：删掉这两句 ⇒ 那条带里谁赢变成**看运气**
                //      （`Editor/CollectionScene.cs` 的「关窗钮比邻居更深」那条会红）。
                if (closeHitNode != null)
                    closeHitNode.localPosition -= new Vector3(0f, 0f, HitZFront);
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
                // 🆕 **A65②**：原版 `DeckInfoPopup.EditDeck` 的**头一道闸**（`DF:DeckInfoPopup__EditDeck.c:21-45`）：
                //   `if (context.State == 1 /*Import*/ && !CanImportDeck(popup, context.Deck)) { 弹错误提示; return; }`
                //   ⇒ **state 1 这一态下点它，原版是「弹提示、不进编辑器」**。
                //   ⚠️ 我们**没有 state 1 的调用点**（原版那条是 `ChatMessageUI.OnMessageClicked`）⇒ 这一段在本 build
                //      里是**死档**，但字段是 public（自检造得出来）⇒ 照判据摆着，**不许静默**。
                //   ⚠️ 原版那句提示的文案是一条 **I2 词条键**（`DAT_1842cebf0`，格式化进阵营名），
                //      本地没有语言表 ⇒ **读不到** ⇒ 下面这句英文是**我们挑的**（铁律 3，别当原版文案）。
                if (State == DeckInfoState.Import && !CanImportDeck(CollectionData.Raw(DeckIndex)))
                {
                    var wl = CollectionData.Warlord(DeckIndex);
                    // 🔴 **2026-10-18（波 1b）**：原来这里是**写死的英文**（中文档下也印英文）⇒ 改走词条
                    //   （键 `MenuDeck/CantImportDeck`；EN 列与改前写死串**逐字相同**）。
                    //   ⚠️ 这条**键名是原版的**（`stringliteral.json` RVA `0x42CEBF0`）—— 正是上面注释里那个
                    //   `DAT_1842cebf0`（VA − ImageBase `0x180000000`）⇒ 查到就用它，不再自拟。
                    string msg = Loc.T("MenuDeck/CantImportDeck") + (wl != null ? "（" + wl.Name + "）" : "");
                    Debug.LogWarning("[DeckInfo] state = Import 且 `CanImportDeck == false` ⇒ **不进编辑器**"
                                     + "（原版 `DeckInfoPopup.EditDeck` 那条闸）；⚠️ 提示文案**是我们挑的** —— "
                                     + "原版那一条是 I2 词条键（`DAT_1842cebf0`），本地无语言表");
                    // 弹窗唯一那颗钮走词条（键 `MainMenu/General/OK`，出处 = 原版同 GO TMP `m_text` = `OK`；
                    //   证据与来历写在 `Core/Loc.cs` 里 `MainMenu/General/OK` 那一条的留痕块）。⚠️ 是 `OK` **不是** `Ok`（施工单 §④ 那条口径）。
                    if (Manager != null) Manager.ShowPopUp(msg, Loc.T("MainMenu/General/OK"), null);
                    return;
                }
                // 「进编辑」**只有一份实现**（`CollectionWindow.GoEdit`）—— 收藏窗那条路也走它
                CollectionWindow.GoEdit(DeckIndex);
                Close();
                return;
            }
            if (key == "Select Deck")
            {
                // 🔴 **2026-10-13（A600）**：`CollectionData.Select` 现在**有出口**了（返回 `bool` + `LastSelectError`）——
                //   原来这里紧接着就**无条件**打「已选中」，而它内部那次落盘失败会被吞掉
                //   （`BattleDriver.PickSavedDeck` 是**从磁盘重读**的 ⇒ 切场景后这一下选择**静默失效**、
                //   战斗拿的是磁盘上那套旧的）。
                //   ⚠️ **关窗那一下照旧无条件**（`Close()` 不动位置）—— 本件只把失败**说出来**，
                //   ⛔ 不动玩家可见流程（「失败时该不该留窗 / 弹提示」= 另一笔账，报告 §五·2）。
                if (CollectionData.Select(DeckIndex))
                    Debug.Log("[DeckInfo] 已选中「" + CollectionData.DeckAt(DeckIndex).Name + "」");
                // 🔴 **2026-10-13（A646）**：这一句从 `Debug.Log` 改成 **`Debug.LogWarning`**（⛔ 正文一字不动，只换通道）。
                //   当初用**普通级**的理由是「与 A548 那两句（`DeleteDeck` / `DuplicateDeck` 的失败支）**同通道**」，
                //   而 **A610（2026-10-13）已把那两句统一成 `LogWarning`**（见下面 `Delete Deck` / `Duplicate Deck`
                //   那两行各自的 A610 注释）⇒ **那条理由已经失效**，这一句就此成了本窗**唯一**「失败走普通级」的地方
                //   （口径不齐本身就会误导下一个会话 —— 本文件里 `Blocked()` / `没有登记过这颗钮` / 各种取不到图
                //   一律 `LogWarning`，「失败 = 警告级」是 A610/A611 之后本工程的统一口径）。
                else Debug.LogWarning("[DeckInfo] 选中卡组失败：" + CollectionData.LastSelectError);
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
                // 正文走词条（键 `MenuDeck/Error/HiddenCards`；ZH 列与改前逐字相同）。
                // ⚠️ 拼在后面的 `hiddenWhy` 是**自检注入向的诊断串**（P3 §③·D 判 ②、未进表）⇒ 英文档下这条会是
                //    「英文正文 + 中文诊断」的混合体。如实记着，**不是静默**。
                if (Manager != null)
                    Manager.ShowPopUp(Loc.T("MenuDeck/Error/HiddenCards") + hiddenWhy, Loc.T("MainMenu/General/OK"), null);
                return;
            }
            Debug.Log("[DeckInfo] 隐藏卡检查过了 ⇒ 开打（原版 `CheckHiddenCardsInDeck` 返回假那一支）"
                      + (string.IsNullOrEmpty(hiddenWhy) ? "（⚠️ 见 `PracticeModePopup.HasHiddenCards`："
                        + "这个检查在我们的数据上恒为假，已出声）" : hiddenWhy));
            // ② 开练习窗 + 立即开打（原版 `ShowPopUp(等待窗)` → `StartMatch`）
            // 🔴 **2026-10-15（A383）**：**本局的 `PlayModes` = `OwnDeckTraining 12`** —— 判据 = 原版这一支
            //   调的就是 `MatchMakerManager.StartMatch(OwnDeckTraining(0xc), …, playerDeck: 自己那副,
            //   enemyDeck: 对手那副)`（`DeckInfoPopup__StartPracticeMatch.c`，见本方法头注）。
            //   🔴 **它不是练习窗那一条** `OfflinePractice 6`（`PracticeEvent.get_EventPlayMode`，VA 0x1808B66B0）
            //   —— 两条链共用 `PracticeModePopup` 当开战宿主，**模式号必须由调用方声明**
            //   （`PracticeModePopup.StartPracticeMatch(…, playMode)` 没有默认值，就是为了逼这一句显式）。
            PracticeModePopup.StartPracticeMatch(Manager, ownDeckIndex, opponent, GameMode.OwnDeckTraining);
        }

        void OnOption(string key)
        {
            if (key == "Delete")
            {
                // 🆕 A65④③：原版 `deleteButton.interactable = (卡组数 > 1)` ⇒ 只剩一套时点了不生效
                if (Blocked("Delete", DeleteInteractable)) return;
                if (CollectionData.DeleteDeck(DeckIndex)) { Debug.Log("[DeckInfo] 已删除该卡组"); Close(); }
                // 🔴 **2026-10-13（A548）**：原来是「删不了（`DeckLibrary.Delete` 的规矩：只剩一套时不许删 / 下标越界）」——
                //   ① 那句里的「只剩一套」规矩**根本不在数据层**（拦它的是上面那颗钮的 `interactable`，见
                //      `DeleteInteractable` 的注释），② 而且它把**两种**失败混成同一句话。
                //   ⇒ 直接打 `CollectionData` 那条**唯一出口**：它自己会分开说「**没删**（下标越界）」与
                //   「**删了，但没写进存档**」，⛔ 别在这里再判一遍（两处写同一条规则 = 迟早不一致）。
                //   🆕 **2026-10-13（A610）**：这一句原来是 `Debug.Log` —— 失败**本来就是警告级**
                //   （`CollectionData` 自己的失败一律 `LogWarning`、`Blocked()` 也是）⇒ 统一成 `LogWarning`。
                else Debug.LogWarning("[DeckInfo] 删卡组失败：" + CollectionData.LastDeleteError);
                return;
            }
            if (key == "Duplicate")
            {
                // 🆕 A65④③：原版 `duplicateButton.interactable = (卡组数 < GameStaticData.totalCustomDecks)`
                if (Blocked("Duplicate", DuplicateInteractable)) return;
                string nm = CollectionData.DuplicateDeck(DeckIndex);
                if (!string.IsNullOrEmpty(nm)) { Debug.Log("[DeckInfo] 已复制成「" + nm + "」"); Close(); }
                // 🔴 **2026-10-13（A548）**：原来是「复制失败（`DeckLibrary.Duplicate` 返回空）」——
                //   空串有**两种**成因（下标越界 / 写盘失败），那一句把原因**一律归给「返回空」**。
                //   A548 当时只能拿「调用前后 `DeckCount()` 变没变」那个**间接判据**分两半
                //   （⛔ 拿不到 `LastError`：`CollectionData.Lib` 是私有的）。
                // 🔴 **2026-10-13（A611）**：`CollectionData` 现在**有出口了**（`LastDuplicateError`，
                //   与 `LastDeleteError` 逐条同形）⇒ **直接打它的原话**；上面那个 `DeckCount()` 间接判据
                //   **已删**（同一条规则留两处 = 迟早不一致，CLAUDE.md §三 —— 而且它现在也判不出更多东西）。
                // 🔴 **2026-10-13（A610）**：通道一并统一成 `Debug.LogWarning`（失败本来就是警告级）。
                else Debug.LogWarning("[DeckInfo] 复制失败：" + CollectionData.LastDuplicateError);
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

        /// <summary>`Share` / `Share On Chat` —— 🔴 **两件不同的事，⛔ 别混成一个动作**（实证见下）。
        /// <para>· `Share` = **写系统剪贴板 + 弹一条提示**。原版逐句（`decomp_full/DeckInfoPopup__ShareDeck.c`）：
        /// ① `MakeDeckString()` 造串 ② `UnityEngine.GUIUtility.set_systemCopyBuffer(串)` **写系统剪贴板**
        /// ③ `UIMessageController.ShowMessage(…)` **弹一条消息** —— 没有确认弹窗、没有平台分享、没有「先问再写」。
        /// 写剪贴板那份实现**全库只有一处** = `DeckRuntime.CopyDeckToClipboard`（判据 `CLAUDE.md` §三
        /// 「两处写同一条规则 = 迟早不一致」）⇒ ⛔ 别在这里再抄一份 `ExportString` + 赋值。</para>
        /// <para>· `Share On Chat` = **另一件事**：原版开一扇 `GenericOptionsPanel`（群组 / 全局 两键，可用性由
        /// `AlliancesManager.IsInGroup` / `PlayerDataManager.IsBannedFromChat` 决定）→ 选中后发一条聊天消息
        /// （`ChatGlobalManager.ShareDeck`）⇒ **不写剪贴板**。见 <see cref="ShareOnChat"/>。</para>
        /// <para>🔴 **2026-10-08（`A1040`）就地订正（铁律 5）**：本处原文写「**原版走服务端 / 平台分享**」
        /// 与「（**批处理与桌面都没法替用户按剪贴板**）」—— **两句都不成立**：原版既不走上服务端、也没做
        /// 平台分享（`DeckInfoPopup__ShareDeck.c:16` 调的就是本地 API `GUIUtility.systemCopyBuffer`）；而
        /// 「没法按剪贴板」被**同一个动作本来的那份实现**直接反证（`Deck/DeckRuntime.cs` 的 `ShareDeckString()`，
        /// 它早就写着 `GUIUtility.systemCopyBuffer = s`）⇒ 那句理由**本身就是错的**，已删。</para>
        /// <para>🆕 **2026-10-09（`A1050`）：第 ③ 句现在是【同一形】了 —— 改前不是。**
        /// 改前挂的是 `Manager.ShowPopUp(…)` = **要玩家点 `确定` 的模态弹窗**（旧注释里那句
        /// 「我们是模态弹窗」说的就是它）；现走 <see cref="MessageToast.Show"/>（= 原版
        /// `UIMessageController` 那条瞬时提示，**自己会消失、不用点**）。
        /// 判据 = `decomp_full/DeckInfoPopup__ShareDeck.c:21-23` 的 `UIMessageController.ShowMessage`
        /// （公开重载 `ShowMessage(string, bool)`，`localize = 1`；尾参那个 `0` 是 IL2CPP 的 `MethodInfo*`，
        /// **不是第 4 个实参** —— 逐条实证见 `Shell/MessageToast.cs` 文件头 ①）。
        /// ⚠️ **另一处宿主（卡组编辑那颗 `Share`）仍是旧形**：`Deck/DeckRuntime.cs` 的 `ShareDeckString()`
        /// 走的是 `Say()`（D35 删件之后它**只写日志、屏幕上什么都不画**）⇒ 那一边**还没**接上这条通道
        /// （`Deck/` 不在本批白名单，已如实上报，归调度台）。</para></summary>
        void ShareDeck(string key)
        {
            if (key == "Share On Chat") { ShareOnChat(); return; }   // ⛔ 那条**不写剪贴板**（原版也不写）
            var deck = CollectionData.Raw(DeckIndex);
            string s = DeckRuntime.CopyDeckToClipboard(deck);        // 全库唯一一份写剪贴板的实现
            if (string.IsNullOrEmpty(s))
            {
                Debug.LogWarning("[DeckInfo] `Share`：卡组串导不出来（`ExportString` 返回空）⇒ 剪贴板**没写**、"
                                 + "什么都没发生（出声，不静默）");
                return;
            }
            Debug.Log("[DeckInfo] `Share` ⇒ 卡组串（" + s.Length + " 字符）**已写进系统剪贴板**：" + s);
            // 🆕 **2026-10-09（`A1050`）**：原版第 ③ 句是 **toast（瞬时提示）**，⛔ **不是模态弹窗** ——
            //   换走 `MessageToast`（宿主 = 原版 `UIError Message Controller`；判据、复用的理由与一处
            //   已知偏离 → `Shell/MessageToast.cs` 文件头）。
            //   🔴 **改前 → 改后**：`Manager.ShowPopUp(ShareCopiedText(len) + "\n\n" + 串, 确定, null)`
            //     （**要玩家点 OK**） →  `MessageToast.Show(键, localize:true, 兜底句)`（自己会消失）。
            //   🔴 **文案照原版的形状**：键 = **`MenuDeck/Share/ExportSuccesful`** —— 出处是原版自己的
            //     **字符串常量表**（`d:/2/tools/il2cpp_out/stringliteral.json:98759`，
            //     同族另三条 `MenuDeck/Share/{Title,ShareOnAlliance,ShareOnGlobal}` 紧邻），
            //     `localize = true`（= 原版 `ShowMessage(键, 1)`）。⚠️ 原版把 `Successful` 拼成了
            //     **`Succesful`（一个 s）** —— **一个字都别改**，改了就等于换了一条键。
            //   ⚠️ 那条键**本地词条表里没有 value**（`Core/Loc.cs` 那 6 条 `MenuDeck/Share/*` 全无 value，
            //     与其余 I2 值一样在远端）⇒ 今天恒走**兜底句** `ShareCopiedText`（两个宿主共用的那一份文案）。
            //     ⛔ 别把键名印到屏幕上（`ShowMessage` 的两态行为见 `ErrorMessageBanner.ShowMessage`）。
            //   ⚠️ 与旧弹窗的**一处有意差别**：原版那条提示**只有一句话、不含卡组串**（串已经写进系统
            //     剪贴板了）⇒ 这里也**不再把串印出来**（原来印串是「让玩家自己抄」的权宜做法）。
            MessageToast.Show(ShareSuccessTerm, true, DeckRuntime.ShareCopiedText(s.Length));
        }

        /// <summary>分享成功那条 toast 的**词条键** —— **原版字符串常量表实读**（不是自拟）：
        /// `d:/2/tools/il2cpp_out/stringliteral.json:98759` 的 `"MenuDeck/Share/ExportSuccesful"`
        /// （拼写照原版，**少一个 s**）。
        /// <para>⚠️ 它的 **value 在远端 I2 语言表**里，本地**没有** ⇒ 调用点必须给兜底句
        /// （`DeckRuntime.ShareCopiedText`）—— 见 <see cref="ShareDeck"/> 里那一串注释。
        /// ⛔ 别因为「看着像拼错」就改成 `Successful`：那样键就变了、永远查不到。</para></summary>
        public const string ShareSuccessTerm = "MenuDeck/Share/ExportSuccesful";

        /// <summary>`Share On Chat`（原版 `DeckInfoPopup__ShareDeckOnChat.c`）—— ⛔ **不写剪贴板**（原版不写）。
        /// <para>原版 = 开一扇 `GenericOptionsPanel`（两键：发给群组 / 发给全局）→ 选中后
        /// `CardDeck.Serialize()` → `ChatGlobalManager.ShareDeck(串, 目标)`（= `CreateChatEvent(0x96)` +
        /// `SendChatMessage`）。**那扇面板我们还没建**（账 `A1045`）⇒ 本路**如实出声**（`NotBuilt` 的
        /// `LogWarning` + 一句人话弹窗），并把卡组串印出来给玩家自己选中复制。</para>
        /// <para>🔴 **2026-10-08（`A1040`）就地订正（铁律 5）**：本处原来的正文写「**原版是平台分享**」
        /// —— **与事实不符**（原版与平台分享无关，见上）；「发不出去」的理由原来写「原版走服务端，
        /// 我们这条线没有网络」⇒ 现在写**准确的两条**：① 面板没建（`A1045`）② 我们**没有**
        /// `ChatGlobalManager` 的对等发送口。</para></summary>
        void ShareOnChat()
        {
            // ⛔ 别让它「什么都不发生」：`NotBuilt` = `Debug.LogWarning`（本文件现成那条出声通道）。
            NotBuilt("Share On Chat 的【目标选择面板】（原版 DeckInfoPopup__ShareDeckOnChat.c → "
                     + "GenericOptionsPanel 两键：群组 / 全局）—— 账 A1045，等它把面板建出来");
            var deck = CollectionData.Raw(DeckIndex);
            string s = deck != null ? RuleEngine.DeckLibrary.ExportString(deck) : "";
            // ⛔ 这里【故意】调 `ExportString` 而不调 `DeckRuntime.CopyDeckToClipboard`：本条**不能**写剪贴板。
            if (Manager != null)
                Manager.ShowPopUp(
                    "`Share On Chat`：原版是**另一件事** —— 开一扇两键面板（发给群组 / 发给全局）→ 发一条聊天消息；"
                    + "那扇面板我们**还没建**（A1045），聊天线也没有跟原版 `ChatGlobalManager` 对等的发送口 "
                    + "⇒ 这条今天**发不出去**。（这条**不会**写剪贴板 —— 原版也不写。）\n\n"
                    + (string.IsNullOrEmpty(s) ? "（这一副的卡组串导不出来）"
                                              : "这是这一副的卡组串，可以自己选中复制：\n" + s),
                    Loc.T("MainMenu/General/OK"), null);
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

        /// <summary>🔴 **2026-10-11（A306②）**：`basis` 的**位置**先换算进**设计空间**再减
        /// （`MenuDraw.PosInDesignSpace`）—— 改前写的是 `− basis.position`，**少除了一次 `basis` 上面
        /// 那一级的 `lossyScale`**（与 A294 / A297 修掉的 `MenuDraw.Local` / `MainMenuSubmenuWindow.Local`
        /// 是**同一个病**，本处 = 那份算式的同形副本）。小屏缩放开关一开（窗根 ×M），
        /// `basis.position` 是**已放大**的世界坐标，而 `RectCenter` 给的是**设计坐标** ⇒ 两者不同量纲。
        ///
        /// <para>⚠️ **首参是 `basis`（坐标基准）不是树父 `parent`** ⇒ 这里**只改量纲、不动 `basis` 语义**：
        /// ⛔ 别换成 `MenuDraw.Local(parent, …)`（那只认一个基准，`basis != parent` 的那些点行为会变）；
        /// 收口成 `MenuDraw.Local(basis, …)` 只在「`basis == parent`」时**逐字等价**，而那正是本文件
        /// `Nine`（上面那条）与 `Txt`（下面那条）两处守卫在管的事 —— **两个不同就出声**。</para>
        ///
        /// <para>📌 `k == 1`（缩放开关出厂关着）时与改前**逐位相同**（`DivByScale(v, 1f)` 就是 `v`）。
        /// · `basis == null`：旧写法 NRE，新写法返回零分量（`PosInDesignSpace` 首句）——
        ///   **实读本文件没有这种调用点**（全是 `Transform` 实参），这一条只是行为边界、不是放宽。</para>
        ///
        /// <para>🔴 **改坏法**：换回裸 `basis.position` ⇒ **今天一条现有断言都不会红**
        /// （`k == 1` 时两式逐位相同 ⇒ 这是**潜伏缺陷**，不是「有断言挡着」）⇒ 要补的两态断言
        /// （宿主 + 断什么）写在 `资料/普查产出_1011/W4_子3.md` §四，由调度台安排。</para></summary>
        static Vector3 Local3(Transform basis, float x1, float y1, float x2, float y2)
        {
            return LayoutSpace.RectCenter(x1, y1, x2, y2) - MenuDraw.PosInDesignSpace(basis);
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
            // 🔴 **2026-10-06（A50③）：改走公共件 `MenuDraw.Nine`**（旧写法直调 `ImageQuad.CreateNineSlice`
            //   ⇒ 绕开公共件、**拿不到 `clip` / `clipSoftness`**）。与旧代码**逐项等价**：
            //    ① **矩形** = `(x1,y1)-(x2,y2)`（旧代码喂的两个宽高 `LayoutSpace.Px(x2-x1)` / `(y2-y1)`
            //       就是公共件内部的 `LayoutSpace.Px(r.W)` / `Px(r.H)`）；
            //    ② **落位** = `Local3(basis, …)` 与 `MenuDraw.Local(parent, …)` **是同一份算式**
            //       （`LayoutSpace.RectCenter(…) − 基准的**设计空间**位置`，逐字相同 ——
            //        🆕 2026-10-11（A306②）起两边都走 `MenuDraw.PosInDesignSpace`，
            //        旧注释里那句「`− 基准.position`」是**改前**的写法）⇒ 收口只在 `basis == parent`
            //       时才等价 —— 公共件**只认 `parent` 一个基准**（树父与坐标基准是同一个参数）。
            //       本文件 3 个调用点**全都传同一个对象**（`:622` / `:644` / `:654` 的 `Nine(root, root, …)`），
            //       而这不等于「以后也一定」⇒ 不等就**是位置画错**，⛔ 不许静默（下面出声）。
            //    ③ **队列 = `q`** + **tint 传 `tint`**（旧代码建完逐块设的就是这两样，公共件会替我们设；
            //       `tint` 没传时两边**都不设**，同一条退化）。九宫格切边不用我们管：块数与每块的
            //       `SetAspect` 都由 `CreateNineSlice` 按真九宫格算好（⛔ 别再给子块套整个面板的比例）。
            if (!ReferenceEquals(basis, parent))
                Debug.LogWarning("[DeckInfo] 九宫格 `Nine` 的 `basis` 必须等于 `parent`"
                                 + "（`MenuDraw.Nine` 只按 `parent` 定位，两个不同就会摆错位置）");
            return MenuDraw.Nine(parent, tex, new PxRect(x1, y1, x2, y2), border, texW, texH, q, tint, true, name);
        }

        Label Txt(Transform parent, Transform basis, string text, float x1, float y1, float x2, float y2,
                  float fontPx, Align align, string name, int q)
        {
            // 🔴 **2026-10-09（A229）**：与同文件 `Nine`（上面那条）**同一条守卫、同一句话** ——
            //   本类「**位置按 `basis` 算、树父给 `parent`**」这一套里，`basis != parent` 就是**两套坐标系**
            //   （`Label.Create` 先 `SetParent(parent)` 再把 `localPosition` 设成 `Local3(basis, …)`
            //    ⇒ 世界位置 = `RectCenter + (parent.position − basis.position)` ≠ 目标矩形）。
            //   今天所有调用点都传同一个对象 ⇒ **零行为变化**；⛔ 不许静默（不同就出声）。
            //   改坏法：删掉这一句 ⇒ `Editor/CollectionScene.cs` 的「A229 `Txt` 守卫」那一组**红**。
            if (!ReferenceEquals(basis, parent))
                Debug.LogWarning("[DeckInfo] 文字 `Txt` 的 `basis` 必须等于 `parent`"
                                 + "（`Label` 按 `Local3(basis, …)` 落位，而树父是 `parent`；"
                                 + "两个不同就是两套坐标系、整段字会偏 `parent.position − basis.position`）");
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

        /// <summary>🆕 **2026-10-09（A229）自检口**：拿**任意 `parent`/`basis` 组合**真调一次上面的 `Txt`
        /// —— 自检用它造出 `basis != parent` 那一态（`Txt` 是私有的实例方法，自检没有别的路能进去）。
        /// ⚠️ 只给自检用：建出来的标签挂在传进来的 `parent` 下，调用方自己销毁。
        /// 参数里那个矩形/字号/对齐**不是判据**，只是让这次调用能走完（守卫在那之前就已经跑过了）。</summary>
        public Label TxtBasisProbeForTest(Transform parent, Transform basis)
            => Txt(parent, basis, "A229", 0f, 0f, 120f, 30f, 30f, Align.Left, "TxtBasisProbe", QDIText);

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
