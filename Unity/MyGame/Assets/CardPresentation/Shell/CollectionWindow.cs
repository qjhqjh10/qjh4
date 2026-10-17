// CollectionWindow.cs — 阶段二第 3 层第 5 件「卡组线」：**收藏窗**（`Collection Menu Variant`）
//
// ============================ 出处（唯一正本） ============================
// `资料/阶段二_卡组线_原版规格.md`（本文件的常量都从它来；细节表在 `资料/普查产出_0923/`
// 的 A1_外壳与弹窗 / A2_Deck页 / A3_Cards页 / A4_装饰页与驱动链 四份）。
//
// 🔴 **这条线是「进卡组编辑的唯一门」** —— 在此之前 `DeckEditor.unity` 能编辑、但**玩家没有路径进去**
//    （`资料/普查产出_0917/菜单盘点_汇总.md` §二·5 原话）。
//
// ============================ 原版实测（照抄，别自己编） ============================
//   · 类名 **`CollectionScreen : GameWindowWithTabs`**；窗口参数 `type=0 Fullscreen` ·
//     `windowsPlacement=5 Canvas` · `closeOnESC=1` · `updateNavPanel=1` · `extraScaleSmallScreen=1.0`
//     （与**奖励窗**只差 `closeOnESC`：它是 1、奖励窗是 0）
//   · 入口 = 主菜单左竖导航的 **COLLECTION 钮**（`OpenWindowButton` GUID `6b5ba7fb…`，`closeOtherMenus=1`）
//   · 外壳与奖励窗/商店**同一个壳**（`Content Area` 167.17,70.94→1920.01,1080 · 左栏 165 宽 ·
//     同一批图 `40k_main_tab_background`/`_shadow`/`40k_main_bt_selected BW`/`40k_main_bt_nametag`）
//     ⇒ 直接继承 `MainMenuSubmenuWindow`。**唯一差别**：左栏第一个键顶边 **188.64**
//     （条本身从 158.64 起 + VLG `padTop=30`）⇒ 覆写 `BarPadTop = 188.64 − 70.94 = **117.7**`。
//   · 四个页签的**卡面文案 = Decks / Cards / Cosmetics / Styles**（不是页节点名），
//     图标 `40k_collection_bt_{decks,cards,cosmetics,style}`，标签 fs **36**（auto 5–36）。
//
// ============================ 建了什么 / 没建什么（出声） ============================
//   ✅ 外壳 + 四页签 · **Deck 页**（页头 + 卡组列表，6 列 × 225×364.5，可纵向滚）
//   ✅ **Cards 页**：页头 + 万能卡计数条 + 卡池网格（6 列 × 262.5×384）+ **左侧完整筛选栏**
//      （7 行：搜索框 / Owned / Upgradable / Army 13 档 / Rarity 5 档 / Cost 8 档 / Type 3 档）
//   ✅ **Cosmetics 页**（卡背）：页头 + **233 张卡背网格**（列数**按宽度算** = **6 列** · 250×405）+ 左抽屉（起手收起）
//      🔴 **2026-09-24 坐标整套订正**：原来那一套（`168.27,85→1920,1080`）是**在 `Content Area` 的局部系里**的
//      ⇒ 整页偏 (167.17, 70.94)、**列数因此多算成 7 列**。真值 = `Scroll View` **335.44,155.94 → 1920.01,1080**
//      （1584.56 × 924.06）⇒ `floor(1584.56 ÷ 250)` = **6 列**。根因是 `menu_rect.py` 的父链口径，坑见 `资料/已知的坑.md`。
//   ✅ **Styles 页**（异画）**2026-09-24 建完**：换风格条（左右两颗圆钮 + 风格名）+ **6 列 × 262.5×384 网格** + 左抽屉（起手收起）
//      ⚠️ **2026-10-05 更正（铁律 5）**：原文写「左抽屉（**出厂展开**）」—— 字段读数（`act=T`）对、**推论错**：
//      `act=T` 只等于「节点启用」，推不出「抽屉停在哪一头」。判据与更正的完整链见 `:1150` 那段。
//      · 数据 = **本地 7 张督军异画**（`assets_full/bundle_<阵营>cardassets_assets_all/Texture2D/`，
//        文件名是 **`AA_HB_…` / `…_AA_HB.png`**，另有 `DarkAngels_AA_warlord_Azrael_v2`）
//      · 🔴 **2026-10-14 就地订正（铁律 5 · 账 `A1808`/`A1830`）**：本条原来写「覆盖 **2 种风格**：
//        `AA_HB`(6 张) + `v2`(1 张)；其余风格在远端 CCD 的 `alternateartstyles` 包」—— **两句都不成立**：
//        ① **只有 1 档**（那「2 档」是我们按图片文件名拆的，原版 `AlternateArtSyle` 只有
//        `Default`/`HammerAndBolter`，且轮播走 `FindAll(style != 0)`）② `alternateartstyles` 包**已经在手**
//        （旧 profile 的 Unity 缓存里，69,579 B、旧新两版 catalog 内容哈希相同）——
//        它那两张风格 logo **已导进工程**（`Resources/Art/ui_menu/`，导入器 `工具/import_altstyle_logos.py`）。
//        映射与判据全文 → 下面 `StyleLogoName` 的头注。
//        ⚠️ 上一版这里写「远程包、本机零副本」—— **错因是普查搜的词**（`alternate`/`variant`/`skin`）
//        **一个都不命中 `AA_HB` 这种命名**，是用户拿文件名来问才发现的（`阶段二_卡组线_原版规格.md` §七 ③b）。
//   ✅ **`Deck info Popup`**（2026-09-23）：点一格卡组 ⇒ 选中 + 开它；窗里 `Edit Deck` 才进编辑
//      ⇒ **`SelectDeck` 里那条「再点一下 = 进编辑」的顶替路已撤**（原版那条路有了）
//   ✅ **`Import Deck Popup`**（2026-09-23）：接上 Deck 页那个 `Import` 钮（此前点了只报「没实现」）
//   ⏭ **`Deck Selection Popup with Tabs`**（三个弹窗里最后一个）
//   ⏭ 卡片详情窗（参数已于 2026-09-23 普查完，够建 80%；见 `项目任务.md` §三 第 15 条）
//
// ---- Cosmetics 页**没做**的（逐条出声）----
//   · ~~`Cardback Shadow SDF`~~ ✅ **2026-09-26 建完 + 已加 6 条断言**（不再是缺口；原来那段「仍然没画」已删）。
//     现在的样子：卡背格 = **两层**，底 = SDF（比卡背大一圈）、面 = 卡背，**各自一个渲染队列**（`QPageSdf` < `QPageRow`）。
//     逐值与出处（**唯一出处**）→ `项目任务.md` §三 第 8b 条 —— 那里写了这个 rect 是多少、格式是 `x,y,w,h`、
//     以及为什么不能拿战斗牌堆那处的倍数来套（**两处倍数不同**：这里 337.5/250，那里 2.9212/2.1739）。
//   · ~~抽屉里的 `Army Filter`：A4 只给了容器 rect 与「→ Title + Content(HLG) → Toggle×N」，**没给格子尺寸** ⇒ 没建~~
//     ✅ **2026-10-03 建完（§三 第 29 条 A11）** —— 格子尺寸 2026-09-28 已在**卡组编辑那扇窗**上实测补全
//     （13 格 100×100 · pad L14 · 列距 7 · 3 格/行，见 `Core/FilterPanelModel.BuildCosmetics` 那段），
//     本页从此**走同一份模型**：抽屉两行 = `Army`(在前) + `Owned`(在后)，**阵营真的筛卡背**
//     （判据 `CardbackTable.NamesFor`，与卡组编辑同一个函数）。
//     ⚠️ **仍欠**：`Owned only` 在单机下**不改变结果**（全解锁，如实标）。
//     ✅ **2026-10-04 更正（A11）**：原来这行还写着「抽屉的**滑入动画**没做（整块显隐）」—— **已经做了**（见下）。
//   · ✅ `Empty Collection Warning`（`act=F`）**建了**（原来这行写「照纪律不建」是**过期的** —— 2026-09-24
//     就建了，只是恒关）。判据 = **筛完为空**；2026-10-03 起抽屉两行齐了 ⇒ 这条判据真能触发。
//
// ---- 筛选栏**没做**的（逐条出声）----
//   · ~~**滑动动画**：原版 `hiddenPosition=(-550,0)` + `animationTime=0.3`；我们是整块显隐~~
//     ✅ **2026-10-04（§三第29条 A11）做了** —— 见 `ApplyDrawerSlide` 那一节（位置 / 0.3 秒 /
//     位移期间命中区与滚轮失效）。⚠️ **`Deck/DeckRuntime.cs` 那一份抽屉还是硬切**（那棵树不在本轮白名单
//     —— 判据同源 `CollectionFilterController.Toggle`，记在 `项目任务.md` 待办里）。
//   · **`Owned only` 恒真**（单机全解锁）· **`Upgradable only` 没有升级系统** —— 与卡组编辑同一口径
//   · **`Type` 只有 3 档**（原版 `CardTypeOptions` 就是这样）⇒ **我们的防御卡筛不到**，照原版不加第 4 档
//   · 原版的 `Viewport` 比屏幕长（1150.94 > 1080）—— 我们按屏幕可见的 924.1 做视口，多出来的靠滚动
//
// ---- 🔴 我们挑的（原版取不到，逐条出声）----
//   · **卡组格里的图**：原版是**玩家选的卡背**（`CollectionManager` 的 cosmetic）。
//     ✅ 233 张卡背已导进工程（`Resources/Art/cardbacks/`），**卡背 id 在 `CollectionData.DeckInfo.CardbackId` 上**（卡组编辑器那页能装备）
//     ⇒ 走**判据那一处** `CardArt.DeckCardback(id, 阵营)`（`MenuDraw.DeckCell` 就是这么取的：选了用选的、没选用该阵营默认背）。
//     ⚠️ **2026-10-03 就地更正（A20）**：这一段原来写「**玩家选哪张这件事我们还没有数据源/入口** ⇒ 仍用 `CardArt.CardBack(阵营)` 顶着」
//     —— **两句都不成立了**（数据源与入口都在，取法也早就是 `DeckCardback`）。
//   · **卡组的稳定标识**用 `Name`（`PlayerDeck` 没有 id 字段）—— 重命名会让选中态丢，如实记。
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;      // `CardDatabase` / `CardDef`（`DeckDef` 那套在 RuleEngine 里）

namespace CardPresentation
{
    /// <summary>收藏窗。原版 `CollectionScreen : GameWindowWithTabs`。</summary>
    public class CollectionWindow : MainMenuSubmenuWindow
    {
        // ============================================================ 页签

        /// <summary>左栏四个键。文案是**卡面文案**（`Cosmetics`/`Styles`），不是页节点名。出处 A1 表 `:17-22`。
        /// <para>🔴 **2026-10-19（`A1498`）三处就地订正（铁律 5）**：
        /// <list type="number">
        /// <item><b>第 1 键的 `Label` 由 `Decks` 改成 <c>Deck</c>（单数）</b> —— 判据 = 原版那颗 TMP
        /// （`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_6937732152595899093.json`）的
        /// **`m_text = 'Deck'`**、`m_fontStyle = 16`(UpperCase)、`m_fontSize 36`、`m_TextWrappingMode 0`
        /// ⇒ 屏上是 **`DECK`**。**错因**：`Decks` 是照页节点名（`DeckCollectionTab` 那一族的复数叫法）写的，
        /// 没回 prefab 读那串 `m_text`。⚠️ **2026-10-17 现核**：`Editor/CollectionScene.cs` 的 `wantLabels[0]`
        /// 现在**不是**写死的 `"DECKS"` —— 它走 `Loc.T(term).ToUpperInvariant()`（`:985`）⇒ **本条不用再跟改**
        /// （旧注释说「那一条要跟着改」已过期，就地订正）。</item>
        /// <item><b>四颗都补上 <c>term</c></b>（= 原版 `TabButtonLabel ▸ Localize.mTerm` 原文，本轮现读
        /// `…/Tab Buttons/{Deck,Cards,CardBacks,Alternate Art}/Label/TabButtonLabel`）——
        /// 屏上的字从此走 `Loc.T`（`Shell/MainMenuSubmenuWindow.cs` 的 `BuildTabButton`），中文档下是
        /// **卡组 / 张牌 / 美容品 / 风格**（出处 → `Core/Loc.cs` 的 **F1** 一节，含那两条**与卡组编辑窗共用**的键）。
        /// **错因**：原来直接画资产里的英文 ⇒ 中文档永远出英文（与 `A1493②` 左竖导航、`A1501` 练习窗同一根因）。</item>
        /// <item>⚠️ <b>本表【四项全是真页签、没有母版】</b> —— 见 `Build()` 里 `BuildShell(..., lastIsMaster: false)`
        /// 与 `Shell/MainMenuSubmenuWindow.cs` 的 `BuildBar` doc（判据 = 原版本窗 `TabButtons.tabButtonPrefab` 是 `null`）。
        /// **本项与 `Label`/`term` 无关，但同一笔账**：这三条是同一个会话里一起查出来的。</item>
        /// </list></para>
        /// <para>⚠️ `Label` 从此只是「原版 TMP 的英文原文」那份记录（`Term` 非空 ⇒ 屏上不画它）；
        /// ⛔ 别把 `Decks` 改回去 —— 那会与 prefab 对不上（自检/对账读的就是这一格）。</para></summary>
        public static readonly TabBtnSpec[] Buttons =
        {
            new TabBtnSpec("40k_collection_bt_decks",     "Deck",      36f, 5f, 36f, "DecksTabButton",     47.9f,
                           term: "MainMenu/MainButtons/ButtonLabel/Deck"),
            new TabBtnSpec("40k_collection_bt_cards",     "Cards",     36f, 5f, 36f, "CardsTabButton",     47.9f,
                           term: "MenuShop/ShopItemType/Cards"),
            new TabBtnSpec("40k_collection_bt_cosmetics", "Cosmetics", 36f, 5f, 36f, "CardBacksTabButton", 47.9f,
                           term: "MenuShop/ShopItemType/Cosmetics"),
            new TabBtnSpec("40k_collection_bt_style",     "Styles",    36f, 5f, 36f, "AltArtTabButton",    47.9f,
                           term: "MenuShop/AlternateArtStyle"),
        };

        /// <summary>左栏第一个键的顶边（原版 188.64）− `Content Area` 顶边（70.94）= **117.7**。
        /// ⚠️ 别照抄「padTop = 30」—— 那是**条内**布局组的 padTop，条本身还从 158.64 起（比 `Content Area` 低 87.7）。</summary>
        protected override float BarPadTop { get { return 117.7f; } }

        // ============================================================ 页内自己的层（照兄弟序逐层 +1）
        /// <summary>页内的渲染队列**梯子**。
        /// 🔴 **2026-09-26 插了一层**：卡背格多了一个 **`Cardback Shadow SDF`**（原版那层距离场，
        /// 比卡背本体大一圈、**画在卡背底下**）⇒ 给它单开 `QPageSdf`，其余整体 +1。
        /// ⚠️ **别把 SDF 和卡背放同一个队列** —— 透明物体按「到相机的 3D 距离」排序，
        /// 同一队列里谁盖谁不可控（这工程踩过三次，见 `资料/已知的坑.md`）。</summary>
        public const int QPagePanel = 3030, QPageSdf = 3031, QPageRow = 3032,
                         QPageText = 3033, QPageOverlay = 3034;
        /// <summary>筛选栏的层**在整页之上**（原版兄弟序里 `Card Filters` 排在 `Collection Display` **之后** ⇒ 压在卡池上）。
        /// ⚠️ 队列要**高过卡池那一整片**（`CardView` 的层走材质默认 3000、页底板 3030）—— 用 3040 段。</summary>
        public const int QFlt = 3040, QFltRow = 3041, QFltIconTop = 3042, QFltText = 3043, QFltHit = 3044;

        /// <summary>页面上文字的主色。原版这几页的 TMP 都是白（`DeckRuntime.Ink` 是**卡组编辑那一边**的
        /// 常量，跨窗别用 —— 那是两个窗各自的调色板）。</summary>
        static readonly Color PageInk = Color.white;

        public static CollectionWindow Create(WindowsManager mgr)
        {
            var go = new GameObject("Collection Menu Variant");
            var win = go.AddComponent<CollectionWindow>();
            win.type = WindowType.Fullscreen;                 // 实证 type=0
            win.placement = WindowsPlacement.Canvas;          // 实证 windowsPlacement=5（**商店是 10**）
            win.closeOnEsc = true;                            // 实证 closeOnESC=1（**奖励窗是 0**）
            win.extraScaleSmallScreen = 1f;                   // 实证 1.0
            // 🔴 **2026-10-17（音效线 `W_G`）**：原版 prefab `useDefaultCloseSoundIfNull = 0`（实读）
            //   ⇒ 关窗**不出声**。`GameWindow` 的出厂值是 `true`（原版 ctor 默认 1）⇒ 这一扇必须显式关。
            win.useDefaultCloseSoundIfNull = false;
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        /// <summary>卡组列表的滚动区（**纵向**）—— 自检用（批处理里没有滚轮，直调 `Wheel/ScrollBy`）。</summary>
        public MenuScroll DeckScroll;
        /// <summary>卡池（Cards 页）的滚动区 —— 同上。</summary>
        public MenuScroll CardsScroll;
        /// <summary>卡池当前画出来的格（自检比版面用）。</summary>
        public readonly List<Transform> CardsCells = new List<Transform>();

        // ============================================================ Cards 页的几何（A3 实测）
        /// <summary>卡池视口 **330.2, 155.9 → 1919.9, 1079.9**（1589.7 × 924.06）——
        /// ⚠️ 与 Deck 页的视口（330.9,155.9→1920,1080）**差 0.7/0.1**，别互相抄（两份实测如此）。</summary>
        public static readonly PxRect CardsViewport = new PxRect(330.2f, 155.9f, 1919.9f, 1079.9f);
        /// <summary>卡位 **262.5 × 384** · spacing **0** · 行距 **384** · **贴左但整体居中**（A3）。</summary>
        public const float CardsCellW = 262.5f, CardsCellH = 384f;
        /// <summary>列数 = `floor(1589.7 ÷ (262.5 + 0))` = **6**（原版运行时算的，A3 §四·1 说算法读不到）。</summary>
        public const int CardsCols = 6;
        /// <summary>贴左但整体居中：内容宽 6×262.5 = 1575 &lt; 视口 1589.7 ⇒ 两侧各留 **7.35**。</summary>
        public static float CardsPadX { get { return (CardsViewport.W - CardsCols * CardsCellW) * 0.5f; } }

        public static PxRect CardsCellRect(int i)
        {
            int r = i / CardsCols, c = i % CardsCols;
            float x = CardsViewport.x1 + CardsPadX + c * CardsCellW;
            float y = CardsViewport.y1 + r * CardsCellH;
            return new PxRect(x, y, x + CardsCellW, y + CardsCellH);
        }

        // ------------------------------------------------------------ 🆕 2026-10-17（B10）
        // 卡位**底下那条「张数」** —— 原版 `Collection Card/Content/Counter`（`bundle_menus_assets_all` 逐件现读）：
        //   · `/Counter`（RT pid `-5006247910548507304`，GO `m_IsActive = True`）
        //     `m_AnchorMin = (0.2857142984867096, 0.0020000000949949026)` ·
        //     `m_AnchorMax = (0.7142857313156128, 0.09966713935136795)` · `sizeDelta = (0,0)` · `pivot = (0.5, 0)`
        //     ⇒ **全是相对量**（预置根 350×512 上等价 100,460.97→250,511.01）。运行时格子改成 262.5×384 ⇒
        //        **x 75…187.5 · 距格底 0.768…38.272**（下面 `CardsCounterRect` 按锚点现算，⛔ 别写死那一组数）。
        //   · `/Counter/Text (TMP)`（RT pid `-4591212569639258792`）
        //     `m_AnchorMin = (0, 0.06)` · `m_AnchorMax = (1, 0.665)` · `sizeDelta = (0,0)`
        //     ⇒ 相对 `/Counter` 的框：**宽满 112.5 · 距底 2.25…24.94（高 22.69）**。
        //   底图 = `40K_main_deck_card counter`（`m_Type = 0 (Simple)` · **`m_PreserveAspect = 1`** · 白 · 无九宫格）；
        //   组件 = `Image` + `CollectionCardCounter`（字段 `counter`(TMP) + `greyscale`，
        //   `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CollectionCardCounter.cs`；`greyscale` 长在 `/Counter` 上
        //   ⇒ 置灰时**整条**（底图 + 字）一起灰）。
        //   那颗 TMP 的逐字段原版值（MB pid `435197177245070680`，本笔实读）：
        //   `m_fontSize 31.9` · `m_fontSizeBase 32.0` · `auto[7 … 32]` · `m_HorizontalAlignment = 2 (Center)`
        //   · `m_VerticalAlignment = 4096 (Midline)` · `m_TextWrappingMode = 1 (Normal)` · 白
        //   ⚠️ **`CollectionScene` 那本 A3 普查表把这一颗的对齐记成了 `Right`** —— **现读是 `Center(2)`**
        //     （同表里 `卡组编辑界面_查证_0920.md` 那份没记这一格）⇒ **以字段为准**，本处**不调** `Align*`。
        public const float CardsCntMinX = 0.2857143f, CardsCntMaxX = 0.7142857f;
        public const float CardsCntMinY = 0.002f, CardsCntMaxY = 0.09966714f;
        public const float CardsCntTxtMinY = 0.06f, CardsCntTxtMaxY = 0.665f;
        /// <summary>那条底图（**无九宫格** ⇒ 走 `Rect` + `keepAspect`）。</summary>
        public const string CardsCounterSprite = "40K_main_deck_card_counter";
        /// <summary>那行字的标称字号（原版 `m_fontSize = 31.9`）· auto 上下限 **7 / 32** · base **32**。</summary>
        public const float CardsCounterFontPx = 31.9f, CardsCounterFontMin = 7f, CardsCounterFontMax = 32f;

        /// <summary>`/Counter` 在**格内**的矩形（相对格左上角；输入 = 这一格**滚动之后**的画布矩形）。
        /// ⚠️ 锚点全是相对量 ⇒ 小屏那一档（若将来这页也分档）跟着格子一起走，本函数不用改。</summary>
        public static PxRect CardsCounterRect(PxRect cell)
        {
            return new PxRect(cell.x1 + CardsCntMinX * CardsCellW, cell.y2 - CardsCntMaxY * CardsCellH,
                              cell.x1 + CardsCntMaxX * CardsCellW, cell.y2 - CardsCntMinY * CardsCellH);
        }

        /// <summary>`/Counter/Text (TMP)` 的矩形（相对**画布**；输入 = 上面那条的矩形）。</summary>
        public static PxRect CardsCounterTextRect(PxRect bar)
        {
            return new PxRect(bar.x1, bar.y2 - CardsCntTxtMaxY * bar.H,
                              bar.x2, bar.y2 - CardsCntTxtMinY * bar.H);
        }

        /// <summary>这一格底下那条「张数」印什么。
        /// <para>🔴 **原版 = `"x{0}"` 填【原始拥有数】** —— 判据 `decomp_full/CardCollectionDisplay__SetCell.c`
        /// （`InventoryManager.GetOwnedCount(card)` → `String_Format(DAT_18425ce10, …)`，同一段在
        /// `CardCollectionDisplay__UpdateCardVisuals.c:45-47` 逐字重复）；并把 `拥有数 &lt; 1` 喂给
        /// `CollectionCardCounter.Set(…, setGreyscale)` ⇒ **没拥有的那张置灰**。</para>
        /// <para>⚠️ **我们这一格印 `min(拥有, 卡组上限)`，这是我们挑的偏离（不是原版的做法）**：
        /// 本作资源固定 9999、`CardProgress.Owned` 是**给足**口径（= 卡组上限 + 升满所需 ⇒ 每张 10~11）
        /// ⇒ 照原式会把**每一格**印成 `x11` 那种没有意义的数。这一档**沿用同一个节点**在
        /// `DeckRuntime.PoolCounterText`（「还没有督军」那一支）里**用户 2026-09-28 已经拍过板**的写法
        /// ⇒ 同一棵树、两处印同一个数，不再各定一套口径。</para>
        /// <para>⚠️ **置灰那一支本版【不可达】⇒ 没画**（如实出声）：`owned &lt; 1` 在我们这边恒假
        /// （`CardProgress.Owned ≥ 卡组上限 + 9 ≥ 10`），而且原版那颗是
        /// `UIImageGreyscaleController`（`Image` + 一份灰材质对），我们的 `ImageQuad` 没有对应的判据灰值
        /// ⇒ **只记不画**（⛔ 别自己编一个灰度）。</para>
        /// <para>🔴 **2026-10-17（A934）：上限那一档必须带【卡型】** —— 原来只传稀有度，而原版
        /// `GetMaxCopiesInDeck` 那句 `if (cardType == 10) return 1;` **排在稀有度判断之前** ⇒ 我们池子里
        /// **28 位非传说督军**（epic 15 / rare 13）被印成了 `x2`。判据 / 出处 = `CardProgress.DeckCap` 的 doc
        /// （**在 `Shell/CardDetailPopup.cs` 的 `CardProgress.DeckCap` 那两个重载上** —— `DeckCap(string rarity)` / `DeckCap(string rarity, string type)`，⛔ 不是本文件）；⛔ **别退回不带卡型那一档**。</para></summary>
        public static string CardsCounterText(CardDef def)
        {
            int cap = CardProgress.DeckCap(def.Rarity, def.Type);
            return "x" + Mathf.Min(CardProgress.Owned(def.Id, def.Rarity), cap);
        }

        /// <summary>一格底下那条「张数」（底图 + 那行字）。**每次重建都重建** —— 滚动时格的矩形一直在变，
        /// 而「重建整页格子」本来就是这个函数的语义（`RebuildCardsCells` 先把 `Viewport` 的子件全销毁）。
        /// ⚠️ 挂的是 `parent`（= `holder/Viewport`，那颗 `ViewportClip` 的载体）⇒ `Rect` / `Text` 两个助手
        /// 会沿父链解析到裁切状态（`MenuWindowBase.Text` 里 `ViewportClip.Resolve(parent, …)`），
        /// 压在视口边上的那半条**会被切掉**（与卡同一套）。
        /// ⛔ **本函数不留引用字段**：父件每次被销毁重建，留着的 `Label`/`ImageQuad` 一律变成 Unity 假 null
        /// （`!= null` 恒假）—— 那正是「读起来有值、其实没有」的那类静默失败。自检按**名字**找
        /// （`FindChild(root, "Counter " + i)`），和 `CardHit_*` 同一套。</summary>
        void BuildCardsCounter(Transform parent, int i, PxRect cell, CardDef def)
        {
            // 🔴 **两种「没建出来」要分开**（别拿正常的那个刷警告）：
            //   ① **图不在工程里** = 真缺口 ⇒ 出声（下面这一句）；
            //   ② **整条落在视口外** ⇒ `MenuDraw.Rect` 里那句 `if (!ClipRect(…)) return null;` 会连节点一起不建
            //      —— 那是**正常路径**（每次滚动都会有半行格子被裁掉），返回 null 直接走人、**不报**。
            if (Art(CardsCounterSprite) == null)
            {
                Debug.LogWarning("[Collection] `" + CardsCounterSprite + "` 不在 `Resources/Art/` ⇒ "
                                 + "卡位底下那条「张数」**整条画不出来**（这是缺口，不是「被视口裁掉了」）");
                return;
            }
            var bar = CardsCounterRect(cell);
            // 渲染队列：**必须压在卡之上**（原版兄弟序里 `Counter` 排在 `CardUI` 之后 ⇒ 画在卡上面）。
            // 卡走 `CardView` 的默认 3000 ⇒ 这条用页内的 `QPageRow/QPageText`（3032/3033，> 3000；
            // 又低于筛选栏那一段 3040+，所以抽屉打开时照样压得住它）。
            var q = Rect(parent, CardsCounterSprite, bar, "Counter " + i, QPageRow, null, true);
            if (q == null) return;                  // = 整条在视口外（正常，见上 ②）
            var tr = CardsCounterTextRect(bar);
            // 🔴 **2026-10-09（A1173）**：原版这颗开了 autosize、`m_fontSizeBase = 32`（框只有 22.69 高 ⇒
            //   运行时会被压小）。**这四个数现在直接喂 `Text` 的形参**（口径 = 原版 prefab 的字段值）；
            //   ⛔ 收口前那套「先 `Text`、再手工 `SetAutoFitBox`」已删（两份写法并存 = 迟早不一致），
            //   而且那一套还**把 `Text` 里那道 `ClipText` 裁在了重排之前**（被 `SetAutoFitBox` 抹掉）。
            //   ⚠️ 那三档 autosize 是**原版 prefab 的字段值**（见上面那段现读）；`DeckRuntime` 那处传的上限是
            //    31.9（= `m_fontSize`，原版上限其实是 32），那条挂在 A333 上 ⇒ **别照它抄**。
            var lb = Text(q.transform, CardsCounterText(def), tr.x1, tr.x2, tr.y1, tr.y2, 5, PageInk,
                          "Text (TMP)", CardsCounterFontPx,
                          wrapPx: tr.W, autoMinPx: CardsCounterFontMin, autoMaxPx: CardsCounterFontMax,
                          autoBasePx: CardsCounterFontMax);
            if (lb == null) return;
            lb.SetRenderQueue(QPageText);
            // 纵向档 = 原版 `m_VerticalAlignment = 4096 (Midline)`；
            // 水平档 = `2 (Center)` = `TmpFont.NewText` 的出厂档 ⇒ **不调** `Align*`（调了反而偏）。
            MenuDraw.SetVAlign(lb, Label.VAlign.Midline, tr);
        }

        // ============================================================ Cards 页的筛选栏（A3 §五·1 + 本工程实读）
        //
        // 原版 = `CardsTab` 里的 `Card Filters` 面板（组件 `CollectionFilterController<RawCardScript>`）。
        // 逐条实读（本机 `工具/menu_dump.py` / `menu_rect.py bundle_menus_assets_all -8460121208602172715`）：
        //   · 面板 **0.25,155.9 → 335.56,1080**，图 `40k_main_tab_background`（**Simple**，图本身只有 185×4 ⇒ 拉伸）
        //   · 面板自己的 `Shadow`（`40k_main_tab_shadow`，**Simple**，col α **0.314**）宽 **152.82**，兄弟序**在面板之前**
        //   · 面板里 = `Scroll View`(sens **50**) → `Viewport`(`UIMask` + Mask showGraphic=0)
        //     → `Filters`(**VerticalLayoutGroup** sp0 pad0) → **7 行**
        //   · `filters[6]` 是后 6 行；第 1 行是搜索框（`CardNameFilter` + `EverguildInputField`）
        //   · 收起时整栏滑到 `hiddenPosition = (-550, 0)`（**父系里的绝对锚点值**；
        //     我们的**行程** = `hiddenPosition.x − originalAnchorPosition.x` = −385px），`animationTime = 0.3`
        //
        // 🔴 **选项表是从 MB 实读的，别按枚举直觉编**（`FilterOptions` 列表就在各 `Card*Filter` 的
        //    `options` 字段里 —— 见文件头出处）：
        //      Army **13** 档（alt 空 ⇒ 走 Localize，我们直接印阵营名）
        //      Rarity **5** 档（alt = `Common/Rare/Epic/Legendary/Special`）
        //      Cost  **8** 档（alt = `1-` / `2`…`7` / `8+` —— **是区间，不是「每个费用一格」**）
        //      Type  **3** 档（`Warlord` / `Troops` / `Stratagem`；
        //             `CardTypeOptions { Minion=0, Hero=10, Tactic=20, Whispers=40 }`，`ShowOnlyHero` 比的就是 `== 10`）
        //   ⇒ 30 个选项的 `background` 图**全是运行时赋的**（预设里 `sprite=0`），`checkMark` **30/30 全是 null**
        //   ⇒ **选中态没有对勾图，靠 `EverguildToggle` 的 tint** —— 我们用
        //      `on = 白(1,1,1,1)` / `off = 灰(0.349,0.341,0.341,1)`。
        //      ⚠️ **这两个颜色是「同 bundle 里那对重复出现的 toggle 预制值」，没证明就是采集筛选那一支**
        //         （`togglePrefab` 的 pid 在本 bundle 里找不到根 GO，见文件头）。**如实标成我们的取法。**
        //
        // 🔴 **两处「一个值 ≠ 全部情况」**（铁律 5·c）：
        //   ① 行高/标题位是**布局组跑之后**的值（`Filters` 高 **989.02** = 7 行之和），不是模板位；
        //   ② 面板**比屏幕长**：预设里视口一直画到 y=1150.94，而 `Content Area` 只到 1080
        //      ⇒ 我们按**屏幕可见的 924.1** 做视口，于是内容 989.02 **可滚 64.92**（Type 行靠滚动够得着）。
        //      ⚠️ **这条是我们的判读，不是实读** —— 原版运行时到底是「滚」还是「就那么画到屏幕外」，
        //         **还没跑实况核过**（记在 `项目任务.md` §三 第 15 条）。
        public const float FltL = 0.25f, FltT = 155.9f, FltW = 335.31f;
        public const float FltShadowW = 152.82f;
        /// <summary>🆕 **2026-10-11（A249）**：搜索框那段字的折行档 = **原版 `m_TextWrappingMode` 的 `3`**
        /// （`PreserveWhitespaceNoWrap`，TMP 给**单行输入框**的那一档 —— 见 `Battle/Label.cs` 的 `WrappingMode` 头）。
        /// <para>判据 = 原版 `Name FIlter/…/Text Area/Text` 实测 `折行=3`（**卡牌页与异画页都是 3**）；
        /// 同一棵树里的 `Placeholder` 是 `0`。**我们这一颗 `Label` 兼作两者**（没输入时显示占位符、输入时显示文本）
        /// ⇒ 取 `Text` 那一档 —— **与 `FilterPanelModel.DeckEditInputWrap = 3` 是同一条判据、同一句写法**
        /// （那个常量早就定了，只是收藏窗这一处没跟上，一直落在 `SetAutoFitBox` 的 `Normal`(=1) 上）。</para>
        /// ⛔ **别用 `SetWrapping(false)` 顶替** —— 那是 `0`(`NoWrap`)，会静默降级掉「空白保留」那一半。
        /// 🔴 **2026-10-10（A1212 · 块 3 #12）：本窗的第二个消费者** = Deck 页那颗卡组名筛选框
        /// （`… ▸ Deck Filters ▸ Filters ▸ Deck Name Filter ▸ Input Field ▸ Text Area ▸ Text`，
        /// `R5_包装层菜单族.md` §2 #12 实测**折行也是 `3`**）—— 同一个形状（单行输入框）⇒ **共用这条常量**，
        /// ⛔ 别在那边再写一个 `3`（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。</summary>
        public const int SearchBoxWrap = 3;
        /// <summary>面板**可见**高（屏幕底裁掉）—— 与卡组编辑那条筛选栏同一个数（`DeckRuntime.FltH = 924.1`）。</summary>
        public const float FltViewH = 924.1f;
        /// <summary>`Filters`（VLG）内容高 —— **不再是一个常数**：Army 那一行的高度 = 它自己的内容高
        /// （13 格 3 列 = 5 行）⇒ 它下面三行跟着往下挪。见 `FilterPanelModel.ArmyRowH` 那段说明。
        /// 取值走 `FilterPanelModel.ContentHFor(state)`（**格子数与行高同源**）。</summary>
        /// <summary>收起时**左移多少 px**（= 原版那段位移的**行程**）。
        /// 🔴 **不是 −550**：原版 `hiddenPosition.x = −550` 与 `originalAnchorPosition.x = −165`
        /// **都是父系里的 `anchoredPosition`（绝对锚点值）**，位移量 = 两者之差 ⇒ **行程 = −385 px**。
        /// 🆕 2026-10-04 订正（X3 审查的 **R5**）：原来这里把 −550 当位移用 —— 照搬会把行程放大 43%，
        /// 而我们的展开位（0.25..335.56）与原版展开位（2.18..333.90）几乎重合 ⇒ 行程可以直接搬。
        /// 出处：`资料/待办判据_卡面卡池与双语.md` 那条操作链（两个端点）
        /// + `资料/卡组编辑界面_查证_0920.md:433-434`（`SetupFilters` 的 VA 反汇编）。</summary>
        public const float FltHiddenDx = -385f;
        public const float FltAnimTime = 0.3f;    // 原版 `animationTime`（🆕 A11 起**真的用它**当滑动时长，见 `StepDrawer`）
        public static readonly PxRect FltView = new PxRect(FltL, FltT, FltL + FltW, FltT + FltViewH);

        // 🔴 **七行的行顶 / 内件 rect / 格尺寸 / 选项表：全在 `Core/FilterPanelModel.cs`（只此一份）** ——
        //    卡组编辑的同一个抽屉复用它。原来这里抄了一整套 `FR_*` / `Flt*` 常量，2026-09-28 抽走。

        // 🔴 **2026-10-16（`项目任务.md` §三 第 2 条）：本窗那一份 `struct FltCell` 已删。**
        //    它原来是 `Core/FilterPanelModel.Cell` 的**17 字段镜像**，而两处建模型的地方
        //    （`BuildFilterRowModel` / `BuildCosmoRowModel`）各做一次**逐字段对拷** ——
        //    模型加一格字段就要改三处，且两扇窗（本窗 vs 卡组编辑）迟早不一致。
        //    现在**直接用共用模型那一份**（与 `Deck/DeckRuntime.cs` 同一条路）⇒ 对拷点 = 0。
        //    ⚠️ 模型给的坐标是**面板内**（未减滚动量、未加面板原点）⇒ 画的时候一律过一遍本类的
        //    `Abs(...)`（`R` / `Bg` / `Lab` 三处 —— ⛔ 一个都别漏，见 `RebuildFilterRowsNow`）。
        //    ⛔ **别再建第二份镜像结构**：模型加一格字段，这一边不该再有任何一处要跟着改。

        /// <summary>一页的「左侧筛选栏 + 它的状态」。**Cards 页与 Styles 页各一份、实现只有一份**
        /// （铁律「两处写同一条规则 = 迟早不一致」）—— 下面那些 `_fltXxx` 不是字段，是**转发到当前这一份**。
        /// 每份自己带 <see cref="State"/>（筛的是哪一批卡）与 <see cref="OnChanged"/>（筛完该重画什么）。
        /// 🔴 **实测两页的抽屉矩形完全相同**：`Card Filters` 与 `Card Filters`（异画页）都是
        /// **0.25, 155.94 → 335.56, 1080**（335.31 × 924.06）⇒ 几何不必参数化。
        /// ⚠️ **2026-10-05 更正（铁律 5）**：原文写「出厂态**两页不同**：Cards 页默认收起、
        /// **Styles 页 act=T（展开）**」—— **字段读数对、推论错**：`act=T` 只等于「节点启用」，
        /// 推不出「抽屉展开」。**原版四页起手一律收起**；判据 = `CollectionDisplay&lt;T>.Initialize
        /// → filters.SetupFilters()`（VA 反汇编 `0x1815EC278` 调用点 · 本体 `0x1815F0740` 收尾把
        /// `anchoredPosition` 摆到 `hiddenPosition.x`）+ 四颗 `Filter Toggle` 的 `EverguildToggle.m_IsOn` 全是 0
        /// （`资料/普查产出_1005/块8_卡组窗断言与异画页查证.md` 件 B）。</summary>
        class FilterPanel
        {
            public Transform Node;                       // `Card Filters` 容器（卡背页是 `Cosmetic FIlter`）
            public MenuScroll Scroll;
            /// <summary>这一页的格子表 —— **就是共用模型那一份**（<see cref="FilterPanelModel.Cell"/>，
            /// 卡组编辑那扇窗用同一个类型）。🔴 **2026-10-16（§三第2条）起不再是本窗的镜像结构**；
            /// ⚠️ 坐标是**面板内**（未减滚动量、未加面板原点）⇒ 画的时候过 `Abs(...)`。</summary>
            public readonly List<FilterPanelModel.Cell> Cells = new List<FilterPanelModel.Cell>();
            public bool Open;
            public DeckEditorState State;                // 这一页筛的是哪一批卡
            public System.Action OnChanged;              // 筛选变了之后重画什么（各页自己给）
            /// <summary>🆕 2026-10-03：**卡背页那一份**（原版 `Cosmetic FIlter`）—— 只有 **两行**
            /// （`Army` 13 格 + `Owned`），**没有**搜索框 / 稀有度 / 费用 / 类型，也没有小标题。
            /// 模型走 `FilterPanelModel.BuildCosmetics`（与卡组编辑那扇窗**同一份**）。</summary>
            public bool Cosmo;
            /// <summary>🆕 **2026-10-11（A248）**：**异画页那一份**（原版 `Alternate Art Tab/…/Card Filters`）——
            /// 几何与卡牌页**逐条相同**，只有**字号那一档**不同（原版 `35/auto[10~35]` · `36/auto[10~36]` · 四行小标题 `36`
            /// ⇒ <see cref="FilterPanelModel.InputFontPxStyles"/> 那一族）。
            /// <para>⛔ **不是「异画页要不要筛」**（那由 `State` 决定），只用来选**字号那一套**；
            /// 卡牌页（`_fltCards`）与卡背页（`_fltCosmo`）都留 `false`（= 共用常量那一套）。</para>
            /// <para>判据（现读命令与逐行原值）→ `Core/FilterPanelModel.cs` 的「异画页那一套字号」那一段。</para></summary>
            public bool Styles;

            // ==================================================== 🆕 2026-10-04（§三第29条 A11）：滑入/滑出
            // 判据（`待办判据_卡面卡池与双语.md` §四那条 + `卡组编辑界面_查证_0920.md:434`）：
            //   `Filter Toggle` → `CollectionDisplay.OnEnable → ToggleFilters(bool)`
            //   → **`CollectionFilterController.Toggle(bool, bool)`** → `DOTween.Kill` +
            //     **`DOAnchorPosX(rect, x, 0.3)`** + `SetActive`：收起 x = `hiddenPosition.x` = **−550**、
            //     展开 x = `originalAnchorPosition.x`。
            //   ⚠️ 那只动画 **x**（`anchoredPosition = (hiddenPosition.x, originalAnchorPosition.y)`）——
            //     y 保留当前值 ⇒ 我们**只改 localPosition.x**，y/z 原样。
            /// <summary>滑动进度：**0 = 已滑出到 `hiddenPosition`（整栏在屏幕左外）· 1 = 停在原位**。
            /// 与 <see cref="Open"/>（**逻辑态**，UI/自检读的都是它）在动画期间**故意不同**。</summary>
            public float Slide = 1f;
            /// <summary>滑动目标（0 或 1）—— `Open` 一变就设它。</summary>
            public float SlideTarget = 1f;
            /// <summary>到位时的 `localPosition`（第一次用到时抓一次；位移是「相对它」加的）。</summary>
            public Vector3 BasePos;
            /// <summary>`BasePos` 抓过没有（别用 `BasePos == zero` 判 —— 那个位置**可能就是 0**）。</summary>
            public bool HasBasePos;
            /// <summary>现在参不参与命中/滚轮（**只有完全展开才 true**）。
            /// 见 `SetDrawerInteractive`：位移期间要失效（`CollectionWindow.cs` 那条老注释点的就是这个语义）。</summary>
            public bool Interactive;

            /// <summary>🆕 **2026-10-16（A811）**：**最近那一版是在抽屉没停在展开位时建的** ——
            /// 写在 `RebuildFilterRowsNow` 头、读在 `ApplyDrawerSlide` ④（**到位那一拍**补一次重建）。
            ///
            /// <para>🔴 **2026-10-16 订正（A835 · 铁律 5）：这一格的后果变了 —— 原文那套「病根」从
            /// A811 根治当天起就不成立。** 原文（留痕）：**带闸的件**（三件 `Label` / `Cell_*` /
            /// `Name Filter` / 底图…）**全被判成「框外」**、只剩**不带闸的裸 `Node`**（面板自己）；
            /// 「病根」= **看框那两条路读的都是节点当下的位置** —— 文字 = `ViewportClip.ClipPx`
            /// （`PosInDesignSpace(transform)`）、容器 = `MenuScroll.ClipNode` → `RenderClip`；
            /// 而被比较的矩形是**基准位的绝对设计矩形**（`Abs(...)` / `Scroll.Shift(...)`）
            /// ⇒ 面板滑到屏左外那一刻（收起 = 左移 **385px**，比面板宽 **335.31** 还大）两者**恒不相交**。
            /// ✅ **现在**：框的中心改取**宿主写进这个节点的设计矩形**（`ViewportClip.BaseRect`，
            /// 由 `Hang` / `MenuDraw.ApplyPxRect` 写入）、**不再跟实时 `localPosition` 走** ⇒ 框与被比的
            /// 矩形**同一帧** ⇒ **收起期间那次重建不再被整批判掉**（那一版现在**建得出来**；
            /// ⚠️ 这一句是**静态推读、未跑** —— `资料/普查产出_1016/W11_A811根治.md` §③·C / §⑤）。</para>
            /// <para>⚠️ **那这一版今天还差什么（= ④ 真正在补的东西）**：**落点**。
            /// `MenuDraw.Local(parent, r)` 算的是 `RectCenter(r) − PosInDesignSpace(parent)`，用的是父件
            /// **当下**位置 ⇒ 面板停在收起位时建的那一版会**跟着面板一起滑回来**，到位那一刻偏 **+385px**
            /// （`FltHiddenDx`）⇒ 这个标志今天防的是**可见错位**，不再是「整列空白」。</para>
            /// <para>🔴 **2026-10-18（A834 · 铁律 5 订正）**：原文（留痕）写的是「更彻底那条（让 `MenuDraw.Local`
            /// 的帧也取那份记录矩形）= **A834**，另开一趟；判据 → `资料/普查产出_1016/W11_A811根治.md` §③·C。
            /// ⛔ **别因为「不再空列」就把 ④ 删掉**」。
            /// ✅ **A834 那天修的是另一条路**（调度台裁定 = **甲**）：`RebuildFilterRowsNow` **建之前**先调
            /// `DrawerHome(_flt)` 把面板摆回基准位 ⇒ 每一版**本来就是基准帧的坐标**。
            /// ⇒ 这一格**从这个标志的名字读已经读不出「错」了**：它现在只表示「④ 会在到位那一拍做一次
            /// **结果相同**的重建」（冗余，不是缺陷）。⛔ **④ 仍然留着**（它是 3 条既有断言的「改坏法」锚点，
            /// 删它要先复跑 `CollectionScene.Run` 确认那 16 条全绿 —— 见 `ApplyDrawerSlide` ④ 那段）。
            /// 判据与三个候选的取证 → `资料/普查产出_1018/Q-A834_裁定取证.md` §5–§7。</para>
            /// <para>⚠️ **只有 Cards / Styles 两页走得到 ④** —— 只有它们在**面板内部**挂了
            /// `ViewportClip`（`BuildFilterPanel` 里 `ViewportClip.Hang(panel.Find("Scroll View"), "Viewport", …)`
            /// 那一句）；页面级那几颗（`holder/Viewport`）不跟着面板动，卡背页那一份没有视口节点、
            /// Deck 页那一列走的是 `RebuildDeckFilterCells`。</para></summary>
            public bool RowsBuiltOffBase;
        }

        /// <summary>当前动作作用在哪一份筛选栏上（建 / 刷 / 点 / 开合都走它）。</summary>
        FilterPanel _flt;
        FilterPanel _fltCards;      // Cards 页那份
        FilterPanel _fltStyles;     // Styles 页那份（2026-09-24 加）
        FilterPanel _fltCosmo;      // Cosmetics 页那一份（🆕 2026-10-03，A11）

        // ⚠️ 这三个**是属性不是字段** —— 原来它们是字段、只服务 Cards 页一份。
        //    改成转发之后，下面所有筛选栏方法**一个字都不用改**就同时服务两页。
        List<FilterPanelModel.Cell> _fltCells { get { return _flt.Cells; } }
        Transform _fltPanel { get { return _flt != null ? _flt.Node : null; } set { if (_flt != null) _flt.Node = value; } }
        MenuScroll _fltScroll { get { return _flt != null ? _flt.Scroll : null; } set { if (_flt != null) _flt.Scroll = value; } }
        bool _fltOpen { get { return _flt != null && _flt.Open; } set { if (_flt != null) _flt.Open = value; } }
        /// <summary>筛选栏代码里**唯一**该用的筛选状态（别在那些方法里直接写 `CardsState` ——
        /// 异画页筛的是异画那批卡，不是卡池）。</summary>
        DeckEditorState FltState { get { return _flt.State; } }

        /// <summary>把 `_flt` 指向某一份，跑完还原（同步调用，不跨帧）。
        /// 🔴 **必须还原** —— 否则「Cards 页建完把指针留在自己身上」，异画页那次就作用错对象了。</summary>
        void Scope(FilterPanel p, System.Action body)
        {
            // ⚠️ 面板还没建就点了（例：页还没切过去）—— **出声**，别静默吞掉
            if (p == null) { Debug.LogWarning("[Collection] 这一页的筛选栏还没建，这次动作忽略"); return; }
            var prev = _flt;
            _flt = p;
            try { body(); } finally { _flt = prev; }
        }

        // ============================================================ 🆕 2026-10-04（§三第29条 A11）
        // **抽屉的滑入/滑出**（原来是整块 `SetActive` 硬切 —— 真偏离，这一节就是补它）。
        //
        // 判据（原文 → `待办判据_卡面卡池与双语.md` 那条操作链 + `卡组编辑界面_查证_0920.md:434`）：
        //   `Filter Toggle` → `CollectionDisplay.OnEnable → ToggleFilters(bool)`
        //   → **`CollectionFilterController.Toggle(bool, bool)`** → `DOTween.Kill` +
        //     **`DOAnchorPosX(rect, x, 0.3)`** + `SetActive`：
        //       收起 x = `hiddenPosition.x` = **−550**、展开 x = `originalAnchorPosition.x`（原位）
        //   ⚠️ **只动 x**（`anchoredPosition = (hiddenPosition.x, originalAnchorPosition.y)`）。
        //
        // 🔴 **`FltHiddenDx` 记的是「行程」，不是原版那个绝对锚点值**（2026-10-04 订正，X3 审查的 R5）：
        //   原版 `hiddenPosition.x = −550` / `originalAnchorPosition.x = −165` 都是 `anchoredPosition`
        //   （**父系里的绝对锚点值**）⇒ 位移量 = 两者之差 = **−385 px**。原来这里写「−550 当位移用」
        //   ⇒ 行程放大 **43%**（165px 在 0.3 秒里肉眼看得出来）。
        //   我们的面板按**屏幕绝对 px** 摆（原位 0.25..335.56），而原版展开位是 2.18..333.90（差 1.9px）
        //   ⇒ 同一段行程可以直接搬：收在 **−384.75**（右缘 **−49.44 < 0** ⇒ **整栏仍在屏外**）。
        //   ⚠️ **残余不确定（如实记）**：`originalAnchorPosition` 是**运行期逐实例**抓的
        //     （签名桩里它非序列化），上述两个端点来自**另一个抽屉实例**（宽 331.72，我们是 335.50）
        //     ⇒ 行程可能有 ±2px 出入；但「整栏滑出屏幕」这个语义与这 2px 无关。
        //     （`hiddenPosition` / `animationTime` 逐实例核过 **6/6 相同** —— 四个抽屉共用同一组值。）
        //
        //   ⚠️ **换算**：`FltHiddenDx` 是**原版 px**，`localPosition` 是**世界单位**（1 单位 = 108 px）
        //     ⇒ 必须过 `LayoutSpace.Px()`（见 `ApplyDrawerSlide` ② 那条注释）。
        //
        // 🔴 **批处理没有帧循环**（CLAUDE.md §二）：`Update` 一次都不跑 ⇒
        //   · 真正跑起来（Play）走 `Update → TickDrawers(Time.deltaTime)`，0.3 秒滑完；
        //   · `-executeMethod` 自检里 `Toggle*` **直接到位**（`Application.isPlaying == false`），
        //     动画本身由**确定性口** `SetDrawerProgressForTest(page, t)` / `TickDrawers(dt)` 复验
        //     （两条路都走同一个 `ApplyDrawerSlide` —— 不是两份实现）。

        /// <summary>每帧推一次抽屉动画（原版是 DOTween 的 0.3 秒）。
        /// ⚠️ 批处理下这个方法**不会**被调用（没有帧循环）—— 那是预期的，见上面那段。</summary>
        void Update() { TickDrawers(Time.deltaTime); }

        /// <summary>推一帧：每一份还在动的抽屉各走 `dt / FltAnimTime` 的进度。
        /// 自检直调它做确定性推进（`TickDrawers(0.15f)` ⇒ 进度 +0.5）。</summary>
        public void TickDrawers(float dt)
        {
            if (dt <= 0f) return;                 // 照 UGUI 那条 `deltaTime > 0` 守卫（见 `PointerLayer.TickAt`）
            StepDrawer(_fltCards, dt);
            StepDrawer(_fltStyles, dt);
            StepDrawer(_fltCosmo, dt);
            StepDrawer(_deckFltSlide, dt);        // Deck 页那一列（A11：它只借 `FilterPanel` 的滑动那半份）
        }

        void StepDrawer(FilterPanel p, float dt)
        {
            if (p == null || p.Node == null) return;
            if (p.Slide == p.SlideTarget) return;                       // 已到位 ⇒ 一根手指都不动
            float step = dt / Mathf.Max(0.0001f, FltAnimTime);          // 原版 `animationTime` = 0.3
            ApplyDrawerSlide(p, Mathf.MoveTowards(p.Slide, p.SlideTarget, step));
        }

        /// <summary>`Open` 一变就调它：设目标 + （Play 里）起动画 / （批处理里）直接到位。</summary>
        void StartDrawerSlide(FilterPanel p) { StartDrawerSlide(p, Application.isPlaying); }

        /// <summary>上面那条的真身。`playLike` 只是**把分支选择权交出来**（自检在批处理里也要能走
        /// 「Play 那一支」—— 见 <see cref="StartDrawerSlideForTest"/>），两支各自的语义一个字没改。</summary>
        void StartDrawerSlide(FilterPanel p, bool playLike)
        {
            if (p == null || p.Node == null) return;
            p.SlideTarget = p.Open ? 1f : 0f;
            if (playLike)
            {
                // 起手先把节点按**当前**进度摆一次 —— 关着的那一块原来停在原位、只是被 `SetActive` 藏了，
                // 现在要真的**从滑出去那一头滑回来**（起点就是整栏在屏外那端，行程 −385px）。
                //
                // 🔴 **`force: true` 不能省**（2026-10-05 修，§三第29条 **A76①** · 判据 →
                //   `资料/已知的坑.md` 那条「同一个洞会在下一个新入口重开」+ `待办判据_阶段二与联机.md` §A25⑥）：
                //   起滑这一刻 `Slide` 还停在**旧**值上 ⇒ `SetDrawerInteractive` 算出来的 `on` 与
                //   `p.Interactive` 常常**相等**（收起态起滑：`on=false` == `Interactive=false`）⇒
                //   撞上「没变就不动」那条短路，**这一整栏的 `WindowButton` 一下都不按**。
                //   碰上「某个新入口建完却漏了收尾 `force: true`」的抽屉（新按钮出厂 `enabled=true`），
                //   症状就是**那 0.3 秒里真鼠标点得到**（画面在滑、命中区却是活的）——
                //   X3 审查的 R4 就是这么在 Deck 页漏出来的，当时只补了**建的那一处**；
                //   这条**起滑口**是「下一个新入口」的兜底：⛔ 不是重复，是**唯一覆盖所有入口**的那一处。
                //   ⚠️ 它的代价（2026-10-04 那条「改了就没有断言能覆盖建完那一刻」的顾虑）已经还掉：
                //   批处理里 `Toggle*` 走的是**另一支**（`playLike=false`）⇒ 建完那一刻的状态**照样**由
                //   「收起态命中区全 `enabled=false`」那几条断言盯着；Play 这一支另配了一条
                //   **故障注入**断言（`CollectionScene` 里先 `MakeDrawerHitsStaleForTest` 再起滑）。
                ApplyDrawerSlide(p, p.Slide, true);
            }
            else
            {
                // 没有帧循环 ⇒ 一步到位（与老行为同结果）。**这一支故意不 `force`**：
                // `Toggle*` 一到位 `on` 就变了、本来就会真按一遍；而「建完那一刻命不命中」这条不变量，
                // 正需要**一支不替它兜底**的路径才验得出来（上面那条）。
                ApplyDrawerSlide(p, p.SlideTarget);
            }
        }

        /// <summary>**动画的全部效果都在这一个函数里**（位置 / 显隐 / 命中 / 滚轮）——
        /// 帧路与自检口都调它，所以「自检绿的」与「跑起来的样子」是同一份实现。
        /// <paramref name="forceInteractive"/>：见 <see cref="SetDrawerInteractive"/>（重建后用）。</summary>
        void ApplyDrawerSlide(FilterPanel p, float t, bool forceInteractive = false)
        {
            if (p == null || p.Node == null) return;
            if (!p.HasBasePos) { p.BasePos = p.Node.localPosition; p.HasBasePos = true; }
            float prevSlide = p.Slide;      // 🆕 A811：**进来时**那一格进度（用来认「刚跨过 1」那一拍）
            p.Slide = Mathf.Clamp01(t);

            // ① 显隐：**滑出去了才关**、**在滑的途中要活着**（要不什么都看不见）
            bool live = p.Slide > 0f || p.SlideTarget > 0f;
            if (p.Node.gameObject.activeSelf != live) p.Node.gameObject.SetActive(live);

            // ② 位移：**只改 x**（判据见上）；y/z 保留 —— 原版那句 `(hiddenPosition.x, originalAnchorPosition.y)`
            // 🔴 **`FltHiddenDx` 必须过 `LayoutSpace.Px()`**（2026-10-04 修，X3 审查的 **R1**）：
            //   那个常量是**原版 px**，而 `localPosition` 是**世界单位** —— 这棵树里 1 世界单位 = 108 px
            //   （`MenuDraw.Local()` 减的是世界坐标，`Core/LayoutSpace.cs` 那条「可见高 10 单位 = 1080px」）。
            //   粗加（`lp.x += FltHiddenDx * …`）⇒ 位移放大 108 倍 = **−59,400 px**：抽屉在 0.3 秒的
            //   **前 0.6%** 就飞出屏幕（动画实际看不见 = 等价原来的硬切），而命中/滚轮那 0.3 秒**照样全失效**。
            //   对照组 = 同工程另一处面板滑动 `Battle/BattleLogPanel.cs` 里那次显式换算（`LayoutSpace.ToWorld`）（它显式换算过）。
            var lp = p.BasePos;
            lp.x += LayoutSpace.Px(FltHiddenDx) * (1f - p.Slide);
            p.Node.localPosition = lp;

            // ③ 命中区 / 滚轮：**只有完全展开才生效**（位移期间两者都失效）
            SetDrawerInteractive(p, p.Slide >= 1f && p.SlideTarget >= 1f, forceInteractive);

            // ④ 🆕 **2026-10-16（A811 的最小那条修法）**：**刚滑到展开位**这一拍，把「收起时建的那一版」
            //    **重排到位** —— 不补的话，打开抽屉那一刻那一列是**偏的（+385px）**（可见后果）。
            //
            //    🔴 **为什么会欠那一版（⚠️ 2026-10-16 订正 · A835 · 铁律 5）**：
            //      原文（留痕）写的是：「收起 = 面板整块左移 **385px**（`FltHiddenDx`），而本页那整块面板
            //      只有 **335.31px** 宽（`FltL`/`FltW`）⇒ 面板＋视口整条滑到屏左外；那一刻「看框」的两条路
            //      读的都是**节点当下位置**（文字 `ViewportClip.ClipPx`、容器 `_fltScroll.Intersects`
            //      → `MenuScroll.ClipNode`），被比的却是**基准位的绝对设计矩形** ⇒ 恒不相交 ⇒ 收起态下
            //      发生的任何一次重建（`ClearFiltersNow` / `RefreshCardsAfterFilter` / 滚轮 `OnChanged` …）
            //      都把**所有带闸的件**判成框外（只剩不带闸的裸 `Node`）⇒ 展开这一下又不重建 ⇒
            //      **打开抽屉那一刻整列是空的**」。
            //      ✅ **这一条从 A811 根治当天起不成立**：框的中心现在取的是**宿主写进该节点的设计矩形**
            //      （`ViewportClip.BaseRect`，由 `Hang` / `MenuDraw.ApplyPxRect` 写入），**不再跟实时
            //      `localPosition` 走** ⇒ 框与被比的矩形**同一帧** ⇒ 收起期间那次重建**照样建得出来**
            //      （静态推读、**未跑** → `资料/普查产出_1016/W11_A811根治.md` §③·C / §⑤）。
            //    🔴 **④ 今天（2026-10-18 起）已经是【冗余】的一拍 —— A834 落地了（铁律 5 留痕）**：
            //      原文（留痕）写的是「**今天真正欠的那点差 = 落点**：`MenuDraw.Local(parent, r)` 算的是
            //      `RectCenter(r) − PosInDesignSpace(parent)`（父件**当下**位置）⇒ 面板停在收起位时建的
            //      那一版会**跟着面板一起滑回来**，到位那一刻偏 **+385px**（`FltHiddenDx`）⇒ ④ 把它
            //      **重排到位**。（0.3 秒滑入期间那批件偏 0→+385px = **A834** 记的那处过渡表现；
            //      更彻底的修法要让 `MenuDraw.Local` 的帧也取那份记录矩形 ⇒ **另开一趟**）」。
            //      ✅ **现在**：那处「过渡偏位」已由 `RebuildFilterRowsNow` 里那句 `DrawerHome(_flt)`
            //      （建之前把面板摆回基准位）**从根上治掉** ⇒ 建出来的每一版**本来就是基准帧的坐标**、
            //      整列跟着面板**刚性**平移 ⇒ 到位那一拍**不需要**任何重排，④ 这一下算出来与不算是**同一个结果**。
            //
            //      🔴 **那 ④ 为什么还留着**：它是 3 条既有断言的「改坏法」锚点（见下面 ✅ 那段），
            //      而删它要同步改那几条的文案 + **复跑 `CollectionScene.Run` 才能确认那 16 条全绿**
            //      （`资料/普查产出_1018/Q-A834_裁定取证.md` §8·3 把这一格明确记成「没核」）——
            //      本批（A834）跑不了 Unity ⇒ **不夹带**。留着的代价只是「展开那一拍多重建一次」
            //      （与 A834 之前逐字相同的开销，不是新增）。⛔ 谁要删它，**必须先把那 16 条复跑绿**。
            //    ⚠️ 「闸本身」不是缺陷、⛔ 别去动它（A798 已裁定「只裁不建在画面上等价」）——
            //      这里修的**只是重建时机**（按今天的口径，准确说是**重建的落点**）。
            //
            //    ✅ 三个限定都必要，⛔ 一个都别省：
            //      · `prevSlide < 1f` —— 只在**跨过 1** 那一拍触发；`StepDrawer` 每帧都调本函数，
            //        少了它就成了「每帧重建一次」；
            //      · `RowsBuiltOffBase` —— ⚠️ **A834 之后它不再表示「那一版是错的」**（建的时候已经摆回
            //        基准位了），只表示「这一版是在面板没停在展开位时建的」⇒ ④ 会多做一次**结果相同**的重建。
            //        它原来那条理由仍然成立：无条件重建会把别人手里的节点引用打散（`CollectionScene` 那条
            //        「同一格现在又点得到了」正拿着一颗格的引用**跨过这一拍**），而且它保证**起手建的那一版**
            //        （还没挪过杆子 ⇒ `HasBasePos == false`）不会被误判；
            //      · `Scroll != null` —— **只有 Cards / Styles 两页**在面板里挂了 `ViewportClip`
            //        （`BuildFilterPanel` 的 `Scroll View/Viewport`）⇒ 只有它们吃这道闸。
            //        ⛔ **千万别把 `Scroll == null` 的那两份喂进 `RebuildFilterRows`**：
            //        `RebuildFilterRowsNow` 一进来就**先把子件全销毁**再重建 —— Deck 页那棵树
            //        （`Deck Filters/{Shadow,Panel,Filters}`）会被**连根拆掉**（卡背页是靠 `Cosmo`
            //        那个分支才活下来的，Deck 页没有那一支）。
            //
            //    ⚠️ **不会递归**：`RebuildFilterRowsNow` 的收尾也调本函数，但传的是 `p.Slide` 本身
            //      ⇒ 那一次 `prevSlide == p.Slide` ⇒ 条件当场不成立。
            //    ✅ **配套的验收断言**（宿主 `Editor/CollectionScene.cs` 的 #57–#59 段）：
            //      把 `win.ClearCardFilters()` 那一步换成「只点一下展开」之后，三件字
            //      （`Input Text` / `Cell_owned/Label` / `Title Army`）**应当在**。
            //      🔴 **2026-10-16 订正（A835 · 铁律 5）：那条断言的「电」变了。** 原文（留痕）：
            //      「…**应当在**（**改前会红**）」—— 那个「改前」指的是 **A811 根治之前**（那时收起期间
            //      那次重建**真的**把这三件判成框外 ⇒ 删掉 ④ 就红）。✅ 根治之后**它们照样建得出来**
            //      （见上面那段订正）⇒ 那三条只查「在不在」的断言**删掉 ④ 也不会红**。
            //      ⇒ ✅ **2026-10-16 已收口（W24 · A843）**：补了 **16 条量落点的断言**，落在 `Editor/CollectionScene.cs`
            //      的「**A843 · 落点**」段（紧接 A248 对照组之后）—— **删掉 ④ 会红 3 条**（帧路落点实得 `552.905` ·
            //      `Toggle*` 入口落点 `552.905` · `Title Army` 渲染左沿 `385.25`），**灭自证**那条钉的是**帧路**
            //      （把「点开再重排一次」或补偿塞进 `ToggleFiltersNow` 都碰不到它）⇒ **修法必须落在本函数**。
            //      ⛔ 不再需要主对话裁；判据 → `资料/普查产出_1016/W24_落点断言与量法收口.md`。
            //
            //    🆕 **2026-10-18（A994⑥ · 调度台裁定：④【不删】，只补这一段注释）** —— 三件如实记账：
            //      ① **它今天为什么是冗余的**：`A834` 走的是**候选甲** —— `RebuildFilterRowsNow` 开建之前先调
            //         `DrawerHome(_flt)` 把面板摆回基准位 ⇒ 建出来的**每一版本来就是基准帧的坐标**、
            //         整列跟着面板刚性平移 ⇒ 到位这一拍**不需要**任何重排：④ 算与不算**结果逐位相同**
            //         （即上面那句「多重建一次」）。它从「修法」退化成**空转的那一拍**。
            //         ⛔ 这不是「影响小所以留着」—— 裁定的是**风险 > 收益**（见 ②③），去留仍开着。
            //      ② **真要删它，必须同时动 3 条断言的【改坏法】文案**（都在 `Editor/CollectionScene.cs` 的
            //         「**A843 · 落点**」段；**按文案指认、⛔ 不写行号** —— 行号每次瘦身都会变）：
            //         · `★★ A843 · 落点：展开到位后 Cell_owned 必须重排回 167.905`（**帧路**那一条）
            //         · `★★ 同一拍、另一条建法的件也一样：小标题 Army 的渲染左沿回到 0.25`（`TitleLeftPx` 那一条）
            //         · `★ A843 · 落点（Toggle* 那条入口）：同上，必须回到 167.905`（`Toggle*` 入口那一条）
            //         —— 这三句今天仍把「**删掉 ④ ⇒ 实得 552.905 / 385.25 ⇒ 红**」当改坏法，
            //         而那句话**从 A834 落地当天起已不成立**（删 ④ 它们照样绿，见上面那段订正）。
            //         ⇒ 留 ④ 不删 = 那三句文案一个字都不用动；真要删 ④ = 必须同时把那三句改成
            //         「删 `DrawerHome(_flt)` ⇒ 实得 …」（`DeleteDrawerHome` 那一路才是今天真会红的改坏法）。
            //      ③ **判据 = 那 16 条（「A843 · 落点」整段）在【删掉 ④ 之后】仍全绿** ——
            //         🔴 **这一格今天仍是「未核」**：① 只是**静态推读**，**从没真删过、也没跑过**
            //         （`资料/普查产出_1018/Q-A834_裁定取证.md` §8·3 把这一格明确记成「没核」）。
            //         ⛔ 谁要删 ④：**先跑一次 `CollectionScene.Run`** 把那段跑绿，再照 ② 改那三句文案 ——
            //         顺序反了就是「改了文案却没人验过」（本项目最贵的一类返工）。
            if (p.Slide >= 1f && p.SlideTarget >= 1f && prevSlide < 1f
                && p.RowsBuiltOffBase && p.Scroll != null)
                RebuildFilterRows(p);
        }

        /// <summary>命中区与滚轮的开/关。`force = true` 时忽略「没变就不动」那条短路
        /// （重建完一行新按钮默认就是 enabled 的，而抽屉可能正收着 ⇒ 必须重按一次）。
        ///
        /// ① **命中区**：`PointerLayer.CollectHits` 只挑 `WindowButton.isActiveAndEnabled`
        ///    （`Shell/PointerLayer.cs` 的 `CollectHits`）⇒ 把这一栏底下的按钮 `enabled = false` 就够了；
        ///    而且它们的 quad 是**跟着面板一起挪**的（位置不用管）。
        /// ② **滚轮**：`PointerLayer.HitScroll` 判的是 `MenuScroll.Owner.activeInHierarchy`
        ///    （`:514`）—— 滑动中节点**是活着的**（要画），那条判据不够用
        ///    ⇒ 滑动期间**把登记撤掉**、到位再登记回来
        ///    （`RegisterScroll` 自带去重、`UnregisterOwnedBy` 按宿主撤，两下配对不会漏也不会重复）。
        ///    这正是 `BuildFilterPanel` 里那条老注释说的语义：「面板收起时整块 SetActive(false)，
        ///    滚轮就不该再被这一列吃掉」。
        ///
        /// ⚠️ **两条如实标注（都是我们自己的口径，别当成原版行为 —— X3 审查的 R10 / R11）**：
        ///   ① **原版在那 0.3 秒里到底屏不屏蔽点击，我们没核过**。`CollectionFilterController&lt;T>.Toggle`
        ///      的方法体在泛型里、`decomp_full` 无产物（见 `资料/卡组编辑界面_查证_0920.md`）
        ///      ⇒ 「滑出去了就点不到才对」是**我们挑的口径**（`项目任务.md` §三 A11 行也这么记着），
        ///      ⛔ 不是实读出来的原版行为。
        ///   ② 上面 ② 那半份**只有 Cards / Styles 两页有对象可撤** —— 只有它们建了 `_fltScroll`
        ///      （`BuildFilterPanel`）；**Cosmo 页那份本来就没有滚动区**、**Deck 页那份也没有**
        ///      ⇒ 对那两页「撤登记」是**空转**，不是「四页对称地各撤了一次」。</summary>
        void SetDrawerInteractive(FilterPanel p, bool on, bool force)
        {
            if (p == null || p.Node == null) return;
            if (!force && p.Interactive == on) return;      // 每帧都调 ⇒ 没变就别白扫一遍组件
            p.Interactive = on;

            var wbs = p.Node.GetComponentsInChildren<WindowButton>(true);
            for (int i = 0; i < wbs.Length; i++) if (wbs[i] != null) wbs[i].enabled = on;

            if (p.Scroll != null)
            {
                if (on) PointerLayer.RegisterScroll(p.Scroll);
                else PointerLayer.UnregisterOwnedBy(p.Node.gameObject);
            }
        }

        /// <summary>抽屉页号 → 那一份（自检口与内部共用；**只此一处**映射）。
        /// `0` = Cards（`Card Filters`）· `1` = Styles · `2` = Cosmetics（`Cosmetic FIlter`）· `3` = Deck（`Deck Filters`）。</summary>
        FilterPanel DrawerByPage(int page)
        {
            if (page == 0) return _fltCards;
            if (page == 1) return _fltStyles;
            if (page == 2) return _fltCosmo;
            if (page == 3) return _deckFltSlide;
            Debug.LogWarning("[Collection] 抽屉页号只有 0(Cards)/1(Styles)/2(Cosmetics)/3(Deck)，给的是 " + page);
            return null;
        }

        /// <summary>这一页抽屉的滑动进度（`0` = 已滑出 · `1` = 原位；没有那一页时给 `−1`）。自检用。</summary>
        public float DrawerSlide(int page) { var p = DrawerByPage(page); return p != null ? p.Slide : -1f; }

        /// <summary>这一页抽屉「到位没」= **命中区/滚轮生效中**（自检据它断「位移期间失效」）。</summary>
        public bool DrawerSettled(int page) { var p = DrawerByPage(page); return p != null && p.Interactive; }

        /// <summary>自检口：把某一页的抽屉**钉在某个进度**上（真的摆节点 / 开关命中与滚轮）。
        /// ⚠️ **不动 `SlideTarget`**（那是逻辑目标）—— 所以「钉完还能被 `TickDrawers` 接着推」。</summary>
        public void SetDrawerProgressForTest(int page, float t)
        {
            var p = DrawerByPage(page);
            if (p == null) return;
            ApplyDrawerSlide(p, t);
        }

        /// <summary>🆕 **2026-10-05（A76①）自检口**：走**与 `ToggleFiltersNow` 同一条**起滑逻辑
        /// （`StartDrawerSlide`），但允许在批处理里选**走 Play 那一支**（`Toggle*` 在 `-executeMethod`
        /// 下永远走的是「直接到位」那支）—— 那支里的 `force: true` 否则**一处断言都覆盖不到**。
        /// ⛔ 这里**不是**第二份实现：设 `Open` + 调 `StartDrawerSlide`，与生产路径逐字同源。</summary>
        public void StartDrawerSlideForTest(int page, bool open, bool playLike)
        {
            var p = DrawerByPage(page);
            if (p == null) return;
            p.Open = open;
            StartDrawerSlide(p, playLike);
        }

        /// <summary>🆕 **2026-10-05（A76①）自检口（故障注入）**：把某一页抽屉底下的 `WindowButton`
        /// **全按回 `enabled = true`**，重现「一个新入口建完、收尾却忘了 `force: true`」那个**脏状态**
        /// （新按钮的出厂默认值就是 `enabled = true` —— 见 `资料/已知的坑.md` 里 X3 的 R4）。
        /// 返回按到几个按钮（`-1` = 这一页没有抽屉）。**只有自检调它** —— 生产路径一处都不调。</summary>
        public int MakeDrawerHitsStaleForTest(int page)
        {
            var p = DrawerByPage(page);
            if (p == null || p.Node == null) return -1;
            var wbs = p.Node.GetComponentsInChildren<WindowButton>(true);
            for (int i = 0; i < wbs.Length; i++) if (wbs[i] != null) wbs[i].enabled = true;
            return wbs.Length;
        }

        /// <summary>万能卡计数条那 4 个数字（筛选变了要重算）。⚠️ 语义**与原生不同** —— 见 `项目任务.md` §三 第 15 条 第 29 项。</summary>
        readonly Label[] _wcCount = new Label[4];

        /// <summary>Cards 页的筛选状态（**复用卡组编辑那套** `DeckEditorState.Filter` —— 别写第二套）。</summary>
        static DeckEditorState _cardsState;
        public static DeckEditorState CardsState
        {
            get
            {
                if (_cardsState == null) _cardsState = new DeckEditorState(CardDatabase.Load());
                return _cardsState;
            }
        }

        /// <summary>🆕 2026-10-03（A11）：**卡背页自己的筛选状态** —— 与 Cards 页**分开**一份。
        /// 判据：原版那两棵树是**两棵 prefab**（`Card Filters` vs `Cosmetic FIlter`），筛选条件互不影响
        /// （卡组编辑那扇窗也是这么分的：`_cosmoFilter` 独立于卡牌那份）。</summary>
        static DeckEditorState _cosmoState;
        public static DeckEditorState CosmoState
        {
            get
            {
                if (_cosmoState == null) _cosmoState = new DeckEditorState(CardDatabase.Load());
                return _cosmoState;
            }
        }
        /// <summary>左栏四个键的选中底图（`BuildShell` 给的**数组**；选中态由 `RefreshHighlights` 刷）。</summary>
        ImageQuad[] _btnHighlight;
        /// <summary>🆕 2026-10-03：高亮层要**整棵**开关（九宫格）⇒ 留一份 `BarResult`。</summary>
        MainMenuSubmenuWindow.BarResult _btnRes;

        /// <summary>切页后刷左栏选中态（基类 `ChangeTab` 会调 —— 2026-09-23 加的那条钩子）。
        /// 🔴 起因：收藏窗第一版漏了这一步，**截图里高亮停在第 2 键上**（DECKS 页却亮着 CARDS）。</summary>
        public override void RefreshHighlights()
        {
            int sel = tabButtons != null ? tabButtons.CurrentVisualIndex : -1;
            if (_btnHighlight == null) return;
            // 🆕 2026-10-03：**整棵九宫格一起开关**（`Highlight` 现在是 `Sliced`，一棵树 9 个 quad）
            MainMenuSubmenuWindow.SetHighlight(_btnRes, sel);
        }
        /// <summary>卡组格的节点（自检比版面用；顺序 = 卡组顺序）。</summary>
        public readonly List<Transform> DeckCells = new List<Transform>();

        // ============================================================ 页的几何（A2 实测）

        /// <summary>`Deck Scroll View/Viewport`：**330.9, 155.9 → 1920, 1080**（1589.1 × 924.1）。</summary>
        public static readonly PxRect DeckViewport = new PxRect(330.9f, 155.9f, 1920f, 1080f);
        /// <summary>卡组格 **225 × 364.5**（prefab 根 250×405 的 **0.9 倍**）· 横向 spacing **20** ⇒ **6 列**。
        /// 🔴 **2026-10-12（本会话 · 现读 prefab + 反编译 + 用户实拍三方对账）就地订正（铁律 5）**：
        /// 原文接着写的是「· pad **L10**」—— **那个 `padL=10` 【不参与落位】**，别拿它当左边距。
        /// 判据三条（都现读，不是推的）：
        /// ① `Content` 上那颗 `GridLayoutGroup`（`m_Enabled = 0`）里的 `m_Padding = (10,0,0,0)` 是**死值**
        ///    —— 组件被禁 ⇒ uGUI 一个字节都不写（`menu_dump.py … "Deck Scroll View" --depth 3` 现读，
        ///    同一行自带 `**m_Enabled=0**`）；
        /// ② 真正落位的是 **`RecyclableScrollRect`**：`PolyAndCode.UI.VerticalRecyclingSystem__CreateCellPool.c`
        ///    里那两行是
        ///    `segments = floor(viewportRect.width / (cellWidth + spacingX))` ·
        ///    `offset = ((viewportRect.width − (spacingX + cellWidth) * segments) + spacingX) * 0.5`
        ///    （`* 0.5` 那个常量 = `DAT_1834b2bb4`，与 Cards 页那条「居中量常量」同一个）
        ///    ⇒ **列方向的原点 = 视口左沿 + 整体居中偏移**，与 `DeckGridOffsetX` 逐位同式；
        /// ③ **用户实拍**（`C:/Users/qjh36/Desktop/warpforge参考图/收藏—卡组的参考图.png`）量出来的第 1 格
        ///    左沿 = **400.4 ± 3**（把实拍按卡框宽 221.4 归一后换算），而「居中」那一式给的是 **400.45**
        ///    （原文那套 `padL 10` 给 340.9 —— 差 **59.6px**，实拍上肉眼可见，见报告里的并排图）。</summary>
        public const float DeckCellW = 225f, DeckCellH = 364.5f, DeckSpacingX = 20f;
        /// <summary>⚠️ **2026-10-12**：`DeckPadL = 10` 那个常量**已删**（它的语义是错的 —— 见 `DeckCellW` 的
        /// 订正）。原版 prefab 里那个 `m_Padding.left = 10` 仍在，但**它是死值**（组件 `m_Enabled=0`）。
        /// ⛔ 别再按「视口左沿 + 10」算首格（那是**错的**，实拍会差 59.6px）。</summary>
        /// <summary>🔴 **2026-10-11（A1503 ⑥）：行距的【纵向】间距 —— 原版是 `0`，我们原来【错写成横向那个 20】。**
        /// 判据（只读代理 2026-10-11 现读）：`Content` 上那颗 `GridLayoutGroup` 的 `m_Enabled = 0`
        /// ⇒ 真正说话的是 `RSR` 的 `_spacingX/**20**` / `_spacingY/**0**` / `_cellHeight 364.5`。
        /// ⇒ **行距 = `DeckCellH + DeckSpacingY` = 364.5**（我们原来拿横向那个 20 当行距 ⇒ **每行多 20px**）。
        /// ⚠️ **三处一起改**（格子落位 / 两处内容高），改一处等于没改。</summary>
        public const float DeckSpacingY = 0f;
        public const float DeckCellScale = 0.9f;
        /// <summary>列数 = `floor(1589.1 ÷ (225+20))` = **6**
        /// （原版是 `VerticalRecyclingSystem.ConfigureColumnNumber` 运行时按视口宽算的，
        /// `_segments` 那个序列化值（=5）被 `_controlSegmentSize=1` 覆盖 ⇒ **别拿 `_segments` 当列数**）。</summary>
        public const int DeckCols = 6;

        /// <summary>格子在**列方向**上的原点偏移 = `RecyclableScrollRect` 的**整体居中**量。
        /// <para>判据（反编译逐句，`d:/2/tools/decomp_full/PolyAndCode.UI.VerticalRecyclingSystem__CreateCellPool.c`）：
        /// `offset = ((viewportRect.width − (spacingX + cellWidth) * segments) + spacingX) * 0.5`
        /// ⇒ 本式 = `(W − 6×225 − 5×20) ÷ 2` = `(1589.1 − 1450) ÷ 2` = **69.55**
        /// ⇒ 第 1 格左沿 = `330.9 + 69.55` = **400.45**。</para>
        /// <para>⚠️ 它与 **Cards 页 / 卡背页**那份「整体居中」是**同一条规则**（那两页 `spacing = 0`，
        /// 所以看起来像 `(W − cols×cellW)/2`）——⛔ 别当成两种算法。
        /// 🔴 与 `Content` 那颗 `GridLayoutGroup` 的 `m_Padding.left = 10` **无关**（组件 `m_Enabled=0` = 死值）。</para></summary>
        public static float DeckGridOffsetX
        {
            get { return ((DeckViewport.W - (DeckSpacingX + DeckCellW) * DeckCols) + DeckSpacingX) * 0.5f; }
        }

        public static PxRect DeckCellRect(int i)
        {
            int r = i / DeckCols, c = i % DeckCols;
            float x = DeckViewport.x1 + DeckGridOffsetX + c * (DeckCellW + DeckSpacingX);
            float y = DeckViewport.y1 + r * (DeckCellH + DeckSpacingY);
            return new PxRect(x, y, x + DeckCellW, y + DeckCellH);
        }

        // ------------------------------------------------------------ Cards 页（卡池网格）

        /// <summary>Cards 页：**卡池网格**（6 列 × 262.5×384 · 贴左但整体居中 · 纵向滚）+ 页头 + 万能卡计数。
        /// ⚠️ **筛选面板（13 阵营 3 列 / 稀有度 / 费用那几组格）本轮只做「清空筛选」与计数条**，
        ///    完整筛选格是下一切片（规格在 `资料/普查产出_0923/A3_Cards页.md` §「筛选栏」）。**出声**。</summary>
        public void BuildCardsPage(Transform page)
        {
            // 页头：Filters 圆钮 + 文案 + Clear filters（**四页共用的那一行** —— 见 `BuildFilterHeader`）
            // ⚠️ 2026-09-23 顺手修两处（A3 §5·2 原文）：原版这套按钮是
            //    **[圆钮 50×50]** → 里面 `icon detail` 只有 **30×30**（`sd=(-20,-20)` = 四边各内缩 10、preserveAspect）
            //    · `label` 在 **[437.2, y, 150, 50]**；我们原来是「图拉满 50×50」+「label 从 427.2 起」。
            // 本页字号 = **42 / 42**（原版逐页不同，见 `BuildFilterHeader` 的注释）
            BuildFilterHeader(page, 1, 42f, 42f, ClearCardFilters, ToggleFilters);

            // 万能卡计数：4 个稀有度图标 + 各自的数字（**复用卡组编辑那条几何**，别抄第二份）
            Rect(page, "40k_topmarquee_currency_display_BW",
                 new PxRect(1550f, 91.5f, 1870f, 135.5f), "Wildcard Bg", QPagePanel);
            string[] wcIc = { "40k_general_wildcard_common_small", "40k_general_wildcard_rare_small",
                              "40k_general_wildcard_epic_small", "40k_general_wildcard_legendary_small" };
            // 🔴 **2026-09-28 用户拍板：万能卡数字一律恒定 `99`**（原版这 4 个数是 `WildcardDisplay`
            //    的库存直出、跟着**指针悬停那张卡**的阵营走；我们既没有 hover 也没有发放源 ⇒ 不再自己算）。
            //    判据 → `项目任务.md` §三 第 15 条 第 29 项。
            for (int i = 0; i < 4; i++)
            {
                float x = 1565f + 75f * i;
                // 🔴 **2026-09-27 补 `keepAspect`（PA 普查抓的）**：原版这 4 个图标 `m_PreserveAspect = 1`、
                //   `m_Type=0`（Simple），贴图 42×51 塞进 30×44 的框 ⇒ 原版只画 **30×36.4**（按框居中）。
                //   我们原来拉伸 ⇒ 画满 30×**44**，**高 ×1.21**。同为 PA=1 的另三张（41×51）同理。
                Rect(page, wcIc[i], new PxRect(x, 91.5f, x + 30f, 135.5f), "Wildcard Icon " + i, QPageRow,
                     null, true);
                var t = Text(page, DeckRuntime.WcCountText, x + 30f, x + 30f + DeckRuntime.WcCountWrapPx,
                             91.5f, 91.5f + DeckRuntime.WcCountBoxH, 5, PageInk,
                             "Wildcard Count " + i, DeckRuntime.WcCountFontPx,
                             // 🔴 **2026-10-10（A1212 · 块 3 #1）**：接上原版那颗的 `m_enableAutoSizing`
                             //   （四格 = **10 / 38 / 45 / 1**）。判据 = `R5_包装层菜单族.md` §2 #1
                             //   （`Collection Menu Variant ▸ … ▸ WIldcard Display ▸ Counters ▸ {…} ▸ Counter`，
                             //   四颗逐值相同）。`wrapPx` = 框宽 **41**（= `x+71 − (x+30)`，与卡组编辑窗那颗
                             //   `WcCntW = 41f` 同值）。折行 = `1` ⇒ 与漏斗里那一档同档，不必还原。
                             //   ⚠️ 闸：`wrapPx 41 > 0 ∧ autoMinPx 10 > 0 ∧ fontPx 32.6 > 10` 三条全真 ⇒ 生效。
                             //   🔴 **2026-10-17（`A1229`）**：字号 / 框 / 四格 **全部收口到
                             //   `DeckRuntime.WcCount*` + `FitWildcardCount`** —— 收口前这一组在三个宿主各写一份。
                             wrapPx: DeckRuntime.WcCountWrapPx, autoMinPx: DeckRuntime.WcCountAutoMinPx,
                             autoMaxPx: DeckRuntime.WcCountAutoMaxPx, autoBasePx: DeckRuntime.WcCountAutoBasePx);
                if (t != null) { t.SetRenderQueue(QPageText); _wcCount[i] = t; }
            }
            // 🆕 2026-10-17（B10 · 账上「三件 UI ③」的**「按阵营」那一半**）：这一条计数条**缺的是 `Army Icon`** ——
            //   原版 `WIldcard Display` 三个孩子 = `Background` + `Counters` + **`../Army Icon`**
            //   （`资料/普查产出_0923/A3_Cards页.md` §5·3：**1470,70.94 → 1550,155.94**（80×85）·
            //    sprite = `40k_DeckSelection_icon_FactionBlackLegion` · **preserveAspect**）。
            //   运行期它由 `WildcardDisplay.Initialize(card.army)` 换图 —— 触发者是
            //   `CardCollectionDisplay__CheckFocusedArmy.c`（拿 `Reference Card Pointer` 的 rect 去
            //   `OverlapsAny` 卡位，命中那张卡的 `cardArmy`）。
            //   ⚠️ **我们这一页没有悬停**（`PointerLayer` 只注册了滚动，没有指针层）⇒ 保持**预置出厂那一张**
            //      = `BlackLegion`（判据 = 预置里的 `m_Sprite`，**不是**我们挑的）。**如实出声**：
            //      「4 个数 + 这张徽记跟着悬停那张卡的阵营换」这件事，本作**没有实现**（用户 2026-09-28
            //      已就那 4 个数拍过板：恒定 `99`；徽记同一条口径 ⇒ 恒定出厂那张）。
            //   ⚠️ 图走 `DeckRuntime.FactionIcon`（**全工程唯一一份**阵营名→徽记映射），⛔ 别在这儿写第二份。
            Rect(page, DeckRuntime.FactionIcon("BlackLegion"), new PxRect(1470f, 70.94f, 1550f, 155.94f),
                 "Army Icon", QPageRow, null, true);

            var holder = Node(page, "Scroll View", CardsViewport);
            // 🔴 **2026-10-13（A435 阶段 2 · 乙 · A15）**：裁切状态**长在视口节点上**（= 原版 `Viewport` 上
            //   那个 `RectMask2D`；本页参数实读 = `m_Softness (0,0)` · `m_Padding (0,0,0,0)`，
            //   出处 `资料/普查产出_1008/波C2_A181_A212收藏窗_A214一.md`）。
            //   ⚠️ 这里原来是裸 `Node(holder,"Viewport",…)` + 循环外那一对 `Clip = CardsViewport; … Clip = prevClip;`
            //   —— **那三行已整对删掉**（留着 = 形参永远非空 ⇒ `Resolve` 第 1 支 ⇒ 节点一个像素都不生效，静默）。
            var cardsVp = ViewportClip.Hang(holder, "Viewport", CardsViewport, Vector4.zero, Vector2Int.zero);
            int n = CardsState.VisibleCards().Count;
            int rows = Mathf.Max(1, Mathf.CeilToInt(n / (float)CardsCols));
            CardsScroll = MenuScroll.TopAligned(CardsViewport, rows * CardsCellH, 100f);   // 原版 `m_ScrollSensitivity = 100`（D_N29）
            CardsScroll.Owner = gameObject;
            // 🔴 **A465**：构建循环那一路「整块在视口外就不建」也要吃这颗节点
            //   （⛔ 别改成让 `Intersects` 沿 `Owner` 找 —— `Owner` 是窗根、节点在它下面，找不到）。
            CardsScroll.ClipNode = cardsVp;
            CardsScroll.OnChanged = () => RebuildCardsCells(holder);
            PointerLayer.RegisterScroll(CardsScroll);
            // 🆕 2026-10-12（`A1628` 第二轮）：这一页的**逐格相位**（只在真 Play 出声；批处理零影响）。
            // ⚠️ 只包**建页这一次**调用 —— 滚动时 `OnChanged` 也会调 `RebuildCardsCells`，
            //   那条路逐格重建同样贵，但不该把日志刷爆（要看就把这三行搬到滚动那条路自己量）。
            BuildWatch.CellsBegin();
            RebuildCardsCells(holder);
            BuildWatch.CellsReport("CardsTab 卡池（只建视口内的格，自检实测 18）");
            // 🆕 `A1628` 第二轮：再量**一次文字重排**的绝对耗时（本账最要紧的一个数；理由与
            // 「量完把裁切补回去」那条不变量都在 `BuildWatch.TimeOneTextRelayout` 的 doc 里）。
            if (CardsCells.Count > 0) BuildWatch.TimeOneTextRelayout(CardsCells[0]);

            // 左侧筛选栏（**在卡池之后建** ⇒ 兄弟序在原版里也是它靠后 = 画在卡池之上）
            // 起手收起（原版靠 `hiddenPosition` 滑出去 —— 🆕 A11 起我们也真的滑，见 `ApplyDrawerSlide`）
            _fltCards = BuildFilterPanel(page, CardsState, RefreshCardsAfterFilter, false);

            // `Empty Collection Warning`：**原版三页都有**，出厂 `act=F`，
            // 运行期条件 = **过滤后为空**（`CollectionDisplay.RefreshCollection`：`filteredCollection.Count <= 0` ⇒ `SetActive(true)`）。
            // 坐标 = **135.22,70.94 → 1970.01,1080**（**比父还宽、左右都溢出** —— 原版就这样，别"修正"）。
            // ✅ 2026-09-24 补建（Styles 页同日已建过同一条；这是 §三 第 15 条 第 50 行的下半场）。
            {
                var ew = Node(page, "Empty Collection Warning", new PxRect(135.22f, 70.94f, 1970.01f, 1080f));
                // 🔴 **2026-10-18（A891 的续 · 第三轮整改 · 审查 P4）字走 `Loc.T`** —— 键 = 原版那颗
                //   `Warning` 的 `Localize.mTerm` 原文 `MenuCollection/NoCardsFound`
                //   （4 颗同键；本窗这一颗 = `… > CardsTab > Collection Display > Scroll View >
                //   Empty Collection Warning/Warning`）。英文列 = 那颗 TMP 的 `m_text` 原文逐字符；
                //   中文列 = 「在您选择的过滤器中，您的收藏里没有任何张牌」（2026-10-14 官方 zh-CN 表；原「没有符合当前筛选的卡牌」）。⛔ 节点名 `"Warning"` 不动。
                var wt = Text(ew, Loc.T("MenuCollection/NoCardsFound"),
                              135.22f, 1970.01f, 70.94f, 1080f, 5, PageInk, "Warning", 36f);
                if (wt != null) wt.SetRenderQueue(QPageText);
                _cardsEmpty = ew;
            }
            RefreshCardsEmpty();
        }

        Transform _deckEmpty;      // Deck 页的「一套卡组都没有」
        Transform _cardsEmpty;     // Cards 页
        Transform _cosmoEmpty;     // Cosmetics 页（卡背）
        // 🔴 **2026-10-17（T09 第 1 步）：卡池视图池** —— 滚动不再整屏重建。
        //   键 = `CardDef.Id + 语档`（语档变了卡面文字要重排 ⇒ 不跨语档复用）；上限 ≈2.5 屏。
        //   判据与实测数字 → `资料/普查产出_1017/UI/PERF_T09_收藏窗性能诊断.md`
        readonly Dictionary<string, CardView> _cellPool = new Dictionary<string, CardView>();
        readonly List<string> _cellKeyOrder = new List<string>();
        readonly List<CardView> _cellUsed = new List<CardView>();
        const int CellPoolCap = 48;
        /// <summary>Cards 页的「过滤后为空」提示（判据与 Styles 页**同一条**：`VisibleCards().Count &lt;= 0`）。</summary>
        void RefreshCardsEmpty()
        {
            if (_cardsEmpty != null) _cardsEmpty.gameObject.SetActive(CardsState.VisibleCards().Count <= 0);
        }

        /// <summary>重建整个 Cards 页（筛选变了调它；自检也用）。</summary>
        public void RebuildCardsPage()
        {
            var page = PageRoot(1);
            if (page == null) return;
            // 🔴 **2026-10-17（T09 第 1 步）**：池里的视图挂在**将要被销毁的页树**下 ⇒ 先清池，别留悬空引用。
            _cellPool.Clear(); _cellKeyOrder.Clear(); _cellUsed.Clear();
            for (int i = page.childCount - 1; i >= 0; i--) DestroySafe(page.GetChild(i).gameObject);
            BuildCardsPage(page);
        }

        void RebuildCardsCells(Transform holder)
        {
            var vp = holder.Find("Viewport");
            var parent = vp != null ? vp : holder;
            // 🔴 **2026-10-17（T09 第 1 步）：池化** —— 只清「每趟重建」的两类（张数条 + 命中区），
            //    `CardView` 留在池里复用。原来这里把 `parent` 下**所有孩子**（含 18 张卡）销毁再重建，
            //    而滚动回调的阈值只有 **0.01 px**（`MenuScroll`）⇒ 拖一下 = 18 × 2.6 s ≈ **49 秒**。
            //    实测与修法 → `资料/普查产出_1017/UI/PERF_T09_收藏窗性能诊断.md`
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var ch = parent.GetChild(i);
                if (ch.name.StartsWith("Counter ") || ch.name.StartsWith("CardHit_"))
                    DestroySafe(ch.gameObject);
            }
            CardsCells.Clear();
            _cellUsed.Clear();

            // 🔴 **2026-10-13（A435 阶段 2 · 乙 · A15）**：这里原来是一对「`Clip = CardsViewport;` →
            //   循环 → `Clip = prevClip;`」—— 迁移后**整对删掉**：裁切状态长在 `holder/Viewport` 那颗
            //   `ViewportClip` 上（`BuildCardsPage` 里 `ViewportClip.Hang` 建的），本循环里所有件
            //   （`CardView` 的 `SetPose` 实参、`AddHit` 转发的裸 `Clip`）都挂在它下面 ⇒ 沿父链解析得到同一份。
            var list = CardsState.VisibleCards();
            float scale = CardsCellH / (CardView.Height * 108f);      // 按**卡位高 384** 反解（同卡组编辑）
            for (int i = 0; i < list.Count; i++)
            {
                var content = CardsCellRect(i);
                var r = CardsScroll.Shift(content);
                if (!CardsScroll.Intersects(r)) continue;
                BuildWatch.CellBegin();                      // 🆕 A1628 第二轮：逐格相位（只在真 Play 出声）
                // 🔴 **`ToCardData` 只算一次** —— 原来这里算一次、下面 `SetData` 又算一次，
                //   两个实参逐字相同（`list[i]` / `list[i].Faction`）⇒ 结果逐字段相同。
                //   它是拼卡面文案那条路（`FaceTextFull`：中/英二选一 + `CardIcons.Rewrite` + 关键词段），
                //   算两遍纯属浪费。⛔ 别把它挪回 `CardView.Create` 的实参里（那就又变两份）。
                var data = BattleDriver.ToCardData(list[i], list[i].Faction);
                // 🔴 **2026-10-17（T09 第 1 步）**：**按 `CardDef.Id + 语档` 复用**。
                //   键带语档的理由：语档变了卡面文字要重排，旧视图不能拿（同 `CardView.Create` 的语义）。
                //   ⛔ 复用时**不要** `SetData` —— 同一个 id + 同一份语档 ⇒ `ToCardData` 的产物逐字段相同，
                //   只需要重做「位姿 / 裁切 / 高亮 / 激活」（见下）。
                string key = list[i].Id + "|" + (int)Loc.Current;
                CardView v;
                bool reused = _cellPool.TryGetValue(key, out v) && v != null;
                if (reused)
                {
                    v.gameObject.name = "CollectionCard_" + i;   // 按当前槽号重命名（自检按名字找）
                    v.gameObject.SetActive(true);                // ⚠️ 先激活：TMP 在未激活对象上量不出尺寸
                }
                else
                {
                    v = CardView.Create(parent, data, "CollectionCard_" + i);
                    if (v == null) continue;
                    v.gameObject.SetActive(true);
                    _cellPool[key] = v;
                    _cellKeyOrder.Add(key);
                }
                BuildWatch.CellPhase(reused
                    ? "① `CardView` **复用**（池命中，无新建）"
                    : "① `CardView.Create`（建整张卡：各层 + TMP + `FitToBox` 二分 + `FitToWidth`）");
                // 🔴 **2026-10-08（A181）：卡自己也要吃这道裁切** —— 窗级的裁切只到
                //   `MenuWindowBase` 那几个绘图助手（`Rect`/`Text`/`Nine`/`AddHit`），而这张卡是
                //   `CardView` 的**自建网格**，看不见它 ⇒ 压在视口边上的卡**整张画出去**
                //   （实测滚 192px：第一排卡的上半截画到视口上沿 155.9 以上，压在页头那条空带上）。
                //   原版这一页的 `Viewport` 挂着 `RectMask2D`（`m_Softness=(0,0)`·`m_Padding=(0,0,0,0)`）⇒ 会裁。
                // 📌 走 `SetPose` 定完位姿、**再**让卡去解析裁切边界 —— 顺序不能反：
                //   位姿是「画布矩形 → 卡的局部系」那个换算的输入，先裁再摆 = 按旧位姿裁的一刀。
                //   （这两句 = **一次调用**的等价物：`CardView` 自己就在 `SetPose` 之后立刻重裁。）
                v.SetPose(Local(parent, r.x1, r.y1, r.x2, r.y2), 0f, scale);
                BuildWatch.CellPhase("② `SetPose`（摆位姿 —— 首次无裁切 ⇒ 这一句里的重裁是空转）");
                // 🔴 **2026-10-13（A435 辛 · A774）**：裁切边界**沿父链解析**（就是上面那颗
                //   `holder/Viewport` 上的 `ViewportClip`）—— 原来是把 `CardsViewport` 这个**常量矩形**
                //   当 `SetPose` 的第 4 个实参传进去 ⇒ **第二份状态源**（节点上那份框/`padding` 改了，卡不跟）。
                //   ⛔ **别改成传 `null`**：`SetPose(…, null)` 的语义是「**不改**当前状态」，而这张卡出生时
                //   `_clip` 本来就是 `null` ⇒ 那样**整卡一张都不裁**（静默，正是 A181 当初那个缺陷）。
                //   ⚠️ 代价：`SetData` 会**再重裁一遍**（见 `CardView.ApplyClip` 的注释）—— 正确性优先。
                //   🔴 **2026-10-12（`A1628` 第二轮）就地订正**：下一行那句 `v.SetData(data)` **已删**
                //      ⇒ 本页每格真裁**从两次变一次**（原来 = 本句 + `SetData` 尾巴那次；
                //      `SetPose` 尾巴那次本来就是空转 —— 它那道闸是 `if (Clipping)`，
                //      而这一步时卡还没开跟随、`_clip` 也是 null，出处 `Core/CardView.cs:3820`）。
                //      上面那句「代价」写的是删之前的情形（留着，它是本句**顺序**的理由）。
                v.SetClipFromTree();
                BuildWatch.CellPhase("③ `SetClipFromTree`（解析裁切 + 第一刀真裁：逐层切网格 + 逐字重排）");
                // 🔴 **2026-10-12（`A1628` 第二轮）就把这一句 `v.SetData(data)` 删掉了** —— 它与上面
                //   那句 `Create(parent, data, …)` **做的是同一件事**，逐条核过（`Core/CardView.cs`）：
                //    · `Create` = `AddComponent` + `Build(d)`；`Build` 头一句就是 `Data = d`（`:1584`），
                //      尾三句是 `BuildTextLayers(d)`（`:1791`）· `SetBadges(d.badges)`（`:1794`）·
                //      `BuildStatLayers(); SyncStatLayers();`（`:1801-1802`）。
                //    · `SetData(d)` 里那几段：`CaptureStatChanges` 非 `Board` 直接 `return`（`:1195`）·
                //      `_face != null` 那支只在**没有卡框**的兜底路上才非空（真卡走 `_frame`）·
                //      `_info` / `_art` 重新赋的是**同一个缓存键算出来的同一张贴图**·
                //      `SetCardImageTo3DBase()` 在 `_body3D == null` 时 `return`（`:2781`）·
                //      末尾 `if (Clipping) ApplyClip()` 与上面那句 `SetClipFromTree` 的产物同值 ⇒ **幂等**。
                //      ⇒ 净效果只剩一件：**把 5 段 TMP 整个重排第二遍**（`BuildTextLayers`，
                //      其中效果文字要走 `FitToBox` 的 8 步二分 ⇒ 每段最多 9 次 `ForceMeshUpdate`）。
                //    ⛔ **不是「少建东西」**：卡面每一个件都还在，只是不再建第二遍（铁律：不许静默失败）。
                //    ⚠️ **要退回**：把 `v.SetData(data);` 加回本行即可（`data` 就是上面那一份）。
                v.SetFace(CardFace.Full);      // ⚠️ 空操作：`Create` 已经按 `CardFace.Full` 建（`SetFace` 首句早退）
                v.SetHighlight(CardHighlightState.Normal);
                BuildWatch.CellPhase("④ `SetFace` + `SetHighlight`（`SetFace` 是空操作，见本行注释）");
                CardsCells.Add(v.transform);
                _cellUsed.Add(v);
                // 🆕 2026-10-17（B10 · 账上「卡重复与升级 · 三件 UI ①」）：格子**底下那条「张数」**
                //    （原版 `Collection Card/Content/Counter`）—— 判据 / 文案 / 几何全在 `BuildCardsCounter` 那一段。
                BuildCardsCounter(parent, i, r, list[i]);
                // ⚠️ 闭包**别捕循环变量 `i`** —— 点击发生在重建之后，那时 `i` 已经是 `list.Count`（越界）
                var def = list[i];
                AddHit(parent, "CardHit_" + i, r, QPageRow, () => OpenCardDetail(def));
                BuildWatch.CellPhase("⑤ 张数条（`Rect`+`Text`）+ 命中区（`AddHit`）");
                BuildWatch.CellEnd();
            }

            // 🔴 **2026-10-17（T09 第 1 步）**：本趟没露头的视图 —— **只关不删**（滚回来零成本），
            //   并改名成 `CollectionCard_parked`：⛔ 别留着旧的 `CollectionCard_<i>`，
            //   自检按名字 `Find` 会先撞上屏外那颗（假绿/假红）。
            foreach (var kv in _cellPool)
            {
                var v = kv.Value;
                if (v == null || _cellUsed.Contains(v)) continue;
                if (v.gameObject.activeSelf) v.gameObject.SetActive(false);
                v.gameObject.name = "CollectionCard_parked";
            }
            // 上限回收：**最老且本趟没露头**的那个直接销毁（长会话滚到底不会把整本卡池都建出来）。
            for (int k = 0; k < _cellKeyOrder.Count && _cellPool.Count > CellPoolCap; )
            {
                string key = _cellKeyOrder[k];
                CardView v;
                if (!_cellPool.TryGetValue(key, out v)) { _cellKeyOrder.RemoveAt(k); continue; }
                if (v != null && _cellUsed.Contains(v)) { k++; continue; }
                _cellPool.Remove(key);
                _cellKeyOrder.RemoveAt(k);
                if (v != null) DestroySafe(v.gameObject);
            }
        }

        /// <summary>卡池当前可见卡数（自检用；筛选之后会变）。</summary>
        public int CardsVisibleCount { get { return CardsState.VisibleCards().Count; } }

        /// <summary>自检用：卡池里第 `i` 张（下标 = **筛选之后的可见列表**，与 `CollectionCard_i` / `Counter i`
        /// 那两个节点名**同号** —— `RebuildCardsCells` 的循环变量就是它）。越界 ⇒ `null`。</summary>
        public CardDef CardsCellDef(int i)
        {
            var list = CardsState.VisibleCards();
            return (i >= 0 && i < list.Count) ? list[i] : null;
        }

        // ============================================================ Cosmetics 页（卡背；A4 §二）
        //
        // 原版 = `Cardback Tab`（脚本 `CardbackCollectionTab`）→ `Cardback Display`（`CardbackCollectionDisplay`）。
        // 逐条实读（A4 §二 + §2·1）：
        //   · 页头 `Header` 0,0→1920,85：`Filter Toggle`(200,17.5,50²) `40k_menu_bt` ·
        //     `label`("Filters") 270,17.5→420,67.5 **fs35**(auto 10-35) · `icon detail` 210,27.5,30² ·
        //     `Separator Line` 0,80→1920,90 `40k_main_line` ·
        //     `label`("Your cosmetics collection") 右缘 1821、y 10..70 **fs38** ·
        //     `Clear Filter Button` 1405,12.5→1655,72.5 `UI_Button_Mulligan` + 字 "Clear filters" **fs33**(auto 10-33) hRight
        //     ⚠️ **字号是「每页自己的实例」值**：本页 Filters=**35** / Clear=**33**，异画页同名两处是 **42** —— 不许拿一个数当全局
        //   · `Scroll View` 168.27,85→1920,1080（**1751.73 × 995**）+ `Viewport`(`UIMask`+`Mask showGraphic=0`)
        //   · 网格：`_cellWidth=250` `_cellHeight=405` spacing 0 ⇒
        //     🔴 **列数是【按宽度算】的 = `floor(1751.73 ÷ 250)` = 7**，不是 `_segments=5`
        //     （`_controlSegmentSize=1` ⇒ `ConfigureColumnNumber` 每帧按宽度覆盖 `_segments`；
        //      A4 §2·1 原来写「5 列是定值」**已就地更正** —— 证据同 A3 §3·5）
        //   · 一格 = `Collection Cosmetic` 250×405，**只有两层图**：`Cardback` + `Cardback Shadow SDF`
        //     （后者 `sprite=0` 运行时喂）—— **没有卡名/费用/文字**
        //     🔴 **2026-10-18（A994③）就地订正**：这里原来写 `Cardback`「**铺满**」—— **错了**：
        //       两颗 `Image` 都带 **`m_PreserveAspect = 1`**（A4 §2·1「Simple preserveAspect」）
        //       ⇒ 各自**按自己的贴图比例内接**进自己的框，**都不拉满**（见 `RebuildCosmoCells` 那两处逐条证据）。
        //   · 左抽屉 `Cosmetic FIlter`（0.05,85→335.55,1080）**出厂 act=F**
        //
        // ---- 没建的（出声）----
        //   · `Cardback Shadow SDF`：**图本地有、喂法也查清了**（见文件头），**但没画** —— 这是缺口，别写成「本地没有」
        //   · 抽屉里的 `Army Filter`：A4 只给了容器 rect（335.5×345）与「→ Title + Content(HLG) → Toggle×N」，
        //     **没给格子的尺寸** ⇒ **没建**（要建得先补一次普查）
        //   · `Empty Collection Warning`（`act=F`）⇒ 照纪律不建
        // ---- 🔴 2026-09-24 坐标整套订正（原来那一套在 `Content Area` 的局部系里）----
        // 原来写的是 `CosmoL/CosmoT = 168.27 / 85`。那是把 **`Content Area`(167.17,70.94) 当成原点** 的值
        // ⇒ 整页（网格 + 抽屉 + 页头）**统一偏了 (167.17, 70.94)**，而且**列数因此多算了一列**。
        // 真值（这次是**从 `Collection Menu Variant` 根节点一路走下来**读的，口径见
        // `资料/已知的坑.md` 那条 `menu_rect.py` 的坑）：
        //   · `Cardback Display/Scroll View` = **335.44, 155.94 → 1920.01, 1080**（1584.56 × 924.06）
        //   · `Cardback Display/Cosmetic FIlter` = **0.06, 155.94 → 335.56, 1080**
        //   · `Header/Filter Toggle` = **367.17, 88.44**（与 Deck/Cards/Styles 三页**同值** ⇒ 已收口到 `BuildFilterHeader`）
        // ⚠️ 上一版据 A4 写成 `168.27,85 → 1920,1080`（1751.73 宽）⇒ 列数算出 **7**；
        //    真视口 1584.56 宽 ⇒ **floor(1584.56 ÷ 250) = 6 列**。两个数只差 0.87px 时看不出对错，这次差一列。
        public const float CosmoL = 335.44f, CosmoT = 155.94f, CosmoR = 1920.01f, CosmoB = 1080f;
        public const float CosmoCellW = 250f, CosmoCellH = 405f;
        /// <summary>列数 = `floor(视口宽 ÷ 格宽)`（**算出来的，不是 `_segments`**）。</summary>
        public static int CosmoCols { get { return Mathf.Max(1, Mathf.FloorToInt((CosmoR - CosmoL) / CosmoCellW)); } }
        /// <summary>内容**整体居中**的左边距（`CreateCellPool` 的 `((W − cols·cellW) + cellW)·0.5` 那条）
        /// = (1584.56 − 6×250) × 0.5 = **42.28**。</summary>
        public static float CosmoPadX { get { return (CosmoR - CosmoL - CosmoCols * CosmoCellW) * 0.5f; } }
        public static readonly PxRect CosmoView = new PxRect(CosmoL, CosmoT, CosmoR, CosmoB);

        public static PxRect CosmoCellRect(int i)
        {
            int r = i / CosmoCols, c = i % CosmoCols;
            float x = CosmoView.x1 + CosmoPadX + c * CosmoCellW;
            float y = CosmoView.y1 + r * CosmoCellH;
            return new PxRect(x, y, x + CosmoCellW, y + CosmoCellH);
        }

        /// <summary>卡背那一页的滚动区 / 画出来的格（自检用）。</summary>
        public MenuScroll CosmoScroll;
        public readonly List<Transform> CosmoCells = new List<Transform>();
        /// <summary>卡背总张数（自检用；应是 **233**）。</summary>
        public static int CosmoTotal { get { return CardArt.CosmeticNames().Length; } }

        public void BuildCosmeticsPage(Transform page)
        {
            // ---- 页头 = **四页共用的那一行**（2026-09-24 收口；本页字号 35 / 33，逐页不同）----
            BuildFilterHeader(page, 2, 35f, 33f, ClearCosmoFilters, ToggleCosmoFilters);

            // 标题那条 rect **宽是 0**（实测 `sd=(0,60)`、`m_HorizontalAlignment=4` = **Right**）
            // ⇒ **右对齐到 1821.01** 才是它的真值（A4 表里写「hFlush」**是错的**，已就地更正）。
            // y 真值 = **80.94 .. 140.94**（2026-09-24 订正；原来写的 10..70 是 `Content Area` 局部值）。
            // 🔴 **2026-10-18（第三轮整改 · 审查 P4）字走 `Loc.T`** —— 键 = 原版那颗 `label` 的
            //   `Localize.mTerm` 原文 `MenuCollection/Label/Cosmetics`（全库只 1 颗，就是本页这一点：
            //   `… > Cardback Tab > Header > label`）。英文列 = TMP 原文 `Your cosmetics collection`；
            //   中文列 = 「你的化妆品收藏」（2026-10-14 官方 zh-CN 表；原「你的装饰收藏」= `zh_CN.csv:25` 精确命中）。⛔ 节点名 `"Header Label"` 不动（自检按名找）。
            var title = Text(page, Loc.T("MenuCollection/Label/Cosmetics"), 1200f, 1821.01f, 80.94f, 140.94f, 5, PageInk,
                             "Header Label", 38f);
            if (title != null) { title.SetRenderQueue(QPageText); title.AlignRightOn(LayoutSpace.FromPixel(1821.01f, 0f).x); }

            // ---- 卡背网格 ----
            var holder = Node(page, "Scroll View", CosmoView);
            // 🔴 **2026-10-13（A435 阶段 2 · 乙 · A16）**：裁切状态长在这颗视口节点上
            //   （原版这一页的 `Viewport` 挂着 `RectMask2D`：`m_Softness=(0,0)`·`m_Padding=(0,0,0,0)`，
            //    实读 → `资料/普查产出_1008/波C2_A181_A212收藏窗_A214一.md`）。
            var cosmoVp = ViewportClip.Hang(holder, "Viewport", CosmoView, Vector4.zero, Vector2Int.zero);
            // 🔴 **A1628 探针**：这一句是**整窗里唯一一处「一次把一堆图全拉起来」**——
            //    `CardArt.CosmeticNames()` 走 `Resources.LoadAll<Texture2D>("Art/cardbacks")`。
            //    打进包里的真值（本机 UnityPy 读 `player/WarpforgePlayer_Data/resources.assets`）：
            //    **466 张 Texture2D · `m_CompleteImageSize` 合计 610.1 MB · `m_TextureFormat = 4`(RGBA32 未压缩)**
            //    ⇒ 它同时是「50 秒」与「工作集 +0.9 GB」两条现象的头号嫌疑 ⇒ **必须先量它**。
            int n = CosmoTotal;
            BuildWatch.Mark("①b2a `CosmoTotal` = `CardArt.CosmeticNames()`（`Resources.LoadAll` 466 张 / 610 MB）");
            int rows = Mathf.Max(1, Mathf.CeilToInt(n / (float)CosmoCols));
            CosmoScroll = MenuScroll.TopAligned(CosmoView, rows * CosmoCellH, 100f);   // 原版 `Cardback Display/Scroll View` = 100（D_N29）
            CosmoScroll.Owner = gameObject;
            CosmoScroll.ClipNode = cosmoVp;             // 🔴 A465（同卡池页那一条）
            CosmoScroll.OnChanged = () => RebuildCosmoCells(holder);
            PointerLayer.RegisterScroll(CosmoScroll);
            RebuildCosmoCells(holder);
            BuildWatch.Mark("①b2b `RebuildCosmoCells`（只建**视口内**的格；自检实测 12 格）");

            // `Empty Collection Warning`：**本页那一份的矩形与别页不同** —— 直读 = **170.44,70.94 → 1970.00,1080**
            // （1799.56×1009.06；出处同 Deck 页那条注释）。判据与别页同一条：**过滤后为空**。
            // 🆕 2026-10-03（A11）：抽屉那两行建好了 ⇒ 这条判据现在**真的能触发**（13 阵营各 9~20 张 ⇒ 正常筛不空）。
            {
                var ew = Node(page, "Empty Collection Warning", new PxRect(170.44f, 70.94f, 1970.00f, 1080f));
                // 🔴 **2026-10-18（第三轮整改 · 审查 P4）字走 `Loc.T`** —— 键 = 原版那颗 `Warning` 的
                //   `Localize.mTerm` 原文 `MenuCollection/NoCardbackFound`（全库只 1 颗）
                //   —— 本窗这一颗就是它（`… > Cardback Tab > Cardback Display > Scroll View > Empty Collection Warning/Warning`）。
                //   英文列 = TMP 原文逐字符；中文列**是我们自拟的**（`zh_CN.csv` 里没有这条，见 `Core/Loc.cs`）。
                var wt = Text(ew, Loc.T("MenuCollection/NoCardbackFound"),
                              170.44f, 1970.00f, 70.94f, 1080f, 5, PageInk, "Warning", 36f);
                if (wt != null) wt.SetRenderQueue(QPageText);
                _cosmoEmpty = ew;
                if (_cosmoEmpty != null) _cosmoEmpty.gameObject.SetActive(FilteredCosmoNames().Length <= 0);
            }

            BuildCosmoDrawer(page);
        }

        void RebuildCosmoCells(Transform holder)
        {
            var vp = holder.Find("Viewport");
            var parent = vp != null ? vp : holder;
            for (int i = parent.childCount - 1; i >= 0; i--) DestroySafe(parent.GetChild(i).gameObject);
            CosmoCells.Clear();

            var names = FilteredCosmoNames();
            for (int i = 0; i < names.Length; i++)
            {
                var r = CosmoScroll.Shift(CosmoCellRect(i));
                if (!CosmoScroll.Intersects(r)) continue;
                var cell = Node(parent, "CollectionCosmetic_" + i, r);
                // 🔴 **2026-10-17（界面线 T25）**：名字先落成局部量 —— 下面那颗命中区的闭包要它。
                //    `for (int i …)` 的 `i` **整轮共用一个变量**（点的时候循环早跑完了），
                //    直接把 `names[i]` 写进 lambda = **永远点到最后一张**（静默错一张）。
                var cbName = names[i];
                // 一格两层（**照原版兄弟序**）：底 = `Cardback Shadow SDF`（距离场，比卡背本体大一圈）、
                // 面 = 卡背本体。两层**各自一个渲染队列**（`QPageSdf` < `QPageRow`）—— 见梯子那段的注释。
                var tex = CardArt.Cosmetic(names[i]);
                if (tex != null)
                {
                    // 先画 SDF（它必须**在卡背底下**）。逐值出处：`资料/普查产出_0923/A4_装饰页与驱动链.md:91`
                    //   `/…/Cardback Shadow SDF` = **-42.5,-70.87,337.5,550.8**（相对卡背格 250×405）、
                    //   锚点 (-0.17,-0.185)-(1.18,1.175)（**拉伸**）、pivot (.5,.5)、`Simple + preserveAspect`、`act=T`。
                    //   ⇒ 在格子坐标里就是「左 −42.5、下 −70.87、右 337.5、上 550.8」。
                    var sdfTex = CardArt.CosmeticSdf(names[i]);
                    var sdfBase = CardView.CardbackSdfMaterialBase();
                    if (sdfTex != null && sdfBase != null)
                    {
                        // ⚠️ 表里那串是 **`x, y, w, h`**（不是 x1,y1,x2,y2 —— 同一张表第一行写 `0,0,250,405`
                        //    而那正是格子的尺寸）。y 是**向下**、相对**格左上**。
                        //    两条独立路径核过同一个矩形：① `rect = -42.5,-70.87,337.5,550.8`；
                        //    ② 锚点 (-0.17,-0.185)-(1.18,1.175) + sizeDelta (0,0)（拉伸）⇒
                        //       x: −0.17×250 = **−42.5** ✓ · y: −(1.175−1)×405 = **−70.875** ✓。
                        var sr = new PxRect(r.x1 - 42.5f, r.y1 - 70.875f, r.x1 + 295f, r.y1 + 479.925f);
                        // 🔴 **2026-10-08（A181）走公共件 `MenuDraw.Rect`**（全工程唯一那份「与视口求交 + uv 跟着截」）——
                        //    原来直调 `ImageQuad.Create` ⇒ 这一层**比格大一圈**（左 −42.5 / 上 −70.875），
                        //    压在视口边上那几格会**整块画到视口外**（原版这一页的 `Viewport` 挂着
                        //    `RectMask2D`（`m_Softness=(0,0)`·`m_Padding=(0,0,0,0)`，实读见
                        //    `资料/普查产出_1008/波C2_A181_A212收藏窗_A214一.md`），会裁）。
                        //    ⚠️ `MenuDraw.Rect` 把宽高比设成**裁剩那块**的比 ⇒ 下面**不再自己 `SetAspect`**
                        //    （没被裁到时两者同值 ⇒ 行为逐字不变；🔴 A994③ 起 `keepAspect` 变 `true`，
                        //     它设的是「**按贴图比例内接**之后再求交」那块 —— 依旧由它一次算对，
                        //     ⛔ 别在外面补一句 `SetAspect`：矩形与 uv 是一对被拆开就拉伸的量）。
                        //    🔴 **2026-10-13（A435 阶段 2 · 乙 · A16）**：`clip` 形参从 `CosmoView` 改成 **`null`** ——
                        //    `CosmoView` 现在长在上面那颗 `ViewportClip` 节点上，显式传它 = 形参永远非空 ⇒
                        //    `Resolve` 第 1 支（节点**被形参盖住**，且 `NodeShadowedByParam` 会被这个站点**永久污染**）。
                        //    节点框（`ClipPx`）就是 `CosmoView` 反推回来的（差 ~1e-4px）、`padding`/`softness`
                        //    逐字相同 ⇒ **像素级不变**。`clipSoftness` 一并归零（节点态下由节点那份说了算）。
                        // 🔴 **2026-10-18（A994③）**：`keepAspect` 从 `false` 改成 **`true`** —— 原版那颗
                        //    `Cardback Shadow SDF` 的 `Image` 带 **`m_PreserveAspect = 1`**（A4 §2·1 那行
                        //    「Simple **preserveAspect**」）⇒ 要**按贴图自己的比例内接**进 `sr`
                        //    （337.5×550.8，比例 0.61275），⛔ 不是拉满。
                        //    实测 `_SDF` 全部 **100×130**（比例 **0.76923**）> 0.61275 ⇒ **宽定**：
                        //    实绘 **337.5×438.75**（高缩 112.05、上下各内缩 56.025）。
                        //    ⚠️ **反证**：拉满时 x 缩放 = 337.5/100 = **3.375**，而 y 缩放 = 550.8/130 = **4.237**
                        //    —— **非等比**，距离场会被竖向拉长；内接之后两轴都是 **3.375**（= 等比）。
                        //    🆕 **2026-10-19（B2）**：`_SDF` 那批**故意不进** `Core/CardbackFace.cs` 那张表
                        //    ⇒ 本层走的仍是「按贴图自身比例内接」（`Fit` 的兜底）—— 理由：它的 `padding`
                        //    恒 0（只差图集取整 0.5px），两段式与本式差 <0.4%。全文 → 那个文件的文件头。
                        //    `keepAspect` 在**求交之前**作用（与 uGUI 的顺序一致：先按 pivot 内缩矩形、
                        //    再由 `RectMask2D` 裁）⇒ uv 仍跟着裁剩那块走，不引入拉伸。
                        var qs = MenuDraw.Rect(cell, sdfTex, sr, "Cardback Shadow SDF", QPageSdf,
                                               null, true, null, default(Vector2));
                        if (qs != null)
                        {
                            var mat = new Material(sdfBase);       // ⚠️ 每格一份：共享会让所有格共用最后一张掩码
                            mat.mainTexture = sdfTex;
                            qs.SetMaterial(mat);
                            // ⚠️ 队列必须在 `SetMaterial` **之后**再设一次：`SetMaterial` 会把**旧材质那份队列**
                            //    （`Sprites/Default` = 3000）搬过来 ⇒ 先设的 `QPageSdf` 会被它盖掉（静默降档）
                            qs.SetRenderQueue(QPageSdf);
                        }
                    }
                    else if (sdfTex == null)
                        Debug.LogWarning("[Collection] 卡背 SDF 取不到：" + names[i] + "_sdf"
                                       + "（跑 `工具/import_original_art.py --only-cardback-sdf` 补）");

                    // 🔴 同上：卡背本体也走公共件（**整块在视口外 ⇒ 连节点都不建**，同原版 `RectMask2D` 的命中语义）
                    //    🔴 **A16（2026-10-13）**：`CosmoView` → `null`（理由逐条同上面那一处）。
                    //    🔴 **2026-10-18（A994③）**：`keepAspect` 从 `false` 改成 **`true`** —— 原版那颗
                    //    `Cardback` 的 `Image` 也带 **`m_PreserveAspect = 1`**（A4 §2·1），
                    //    ⛔ 不是拉满 250×405（我们原来就是这么画的 = 拉伸）。
                    //    🔴 **2026-10-19（B2）就地订正（铁律 5）**：上面那句原来接着写「按**贴图自己的比例**
                    //    内接」「一律宽定：宽仍 250，高缩」——**只是半套**。uGUI 是**两段**
                    //    （定框比 sprite 的 `m_Rect` = 707×1020；贴图再按 `padding` 内缩并挪位），
                    //    算式/判据全文 → `Core/CardbackFace.cs`（本仓唯一一份，`MenuDraw.Rect` 转调它）。
                    //    对本仓那批**已裁成 `textureRect`** 的 PNG：**比例不变**，但**宽也缩**（不是「宽仍 250」）
                    //    —— 框仍是 `250 × 250·1020/707 = 250×360.68`，画心 = 框 × (`texRect`/`m_Rect`)。
                    //    例 `Cardback_AM_Shield of Humanity`（`texRect` **707**×981）⇒ **250×346.9**（宽没缩）；
                    //    **偏得最远的**是 `Cardback_All_Early Backer`（`texRect` **608.85**×926.9）⇒
                    //    **215.3×327.8**（宽少 **34.7**px、高少 **52.8**px，且按 `padding` 上下各挪）。
                    //    ⛔ 别拿某一张的值当全部（铁律 5·c）—— 那张表的 `rectW/rectH/padX` 六列逐张不同。
                    var q = MenuDraw.Rect(cell, tex, r, "Cardback", QPageRow, null, true, null, default(Vector2));
                    if (q != null) q.SetRenderQueue(QPageRow);
                }
                else
                {
                    Debug.LogWarning("[Collection] 卡背取不到：" + names[i]);
                }
                // 🔴 **2026-10-17（界面线 T25）**：这里原来 `NotifyNotBuilt(...)`（「原版点它开哪个窗，普查标了「不确定」」）
                //    —— **判据当天已定**：原版 `CardbackCollectionTab__OnItemSelected.c` = 构造
                //    `ItemDisplayWindowContext(item, 1)` → `WindowsManager.OpenWindow<ItemDisplayWindow>`
                //    （`d:/2/tools/decomp_full/CardbackCollectionTab__OnItemSelected.c:17-24`；
                //     ctor 的符号错配成 `UpgradeCardSignal___ctor`，字段 `+0x10 = item` / `+0x18 = amount`）。
                //    ⇒ 点一张卡背开 **`Item Display Window`**（`Shell/ItemDisplayWindow.cs`，T18 建的）。
                AddHit(cell, "Hit", r, QPageRow,
                       () => WindowsManager.OpenItemDisplay(ItemDisplayWindow.ForCardback(cbName)));
                CosmoCells.Add(cell);
            }
        }

        // ---- 左抽屉 `Cosmetic FIlter`（**出厂关**）----
        Transform _cosmoDrawer;
        /// <summary>抽屉开着没有。🆕 A11：读的是**逻辑态** `Open`（原来读 `activeSelf` ——
        /// 加了滑动之后，滑动途中那个节点是**活着**的，读 `activeSelf` 会把「正在合上」当成「开着」）。</summary>
        public bool CosmoFiltersOpen { get { return _fltCosmo != null && _fltCosmo.Open; } }

        void BuildCosmoDrawer(Transform page)
        {
            // 抽屉真值（2026-09-24 从根走下来读的；原来那套是 `Content Area` 局部值、y 少 70.94）：
            //   `Cosmetic FIlter` **0.06, 155.94 → 335.56, 1080** · `Shadow` 0.06→153.07
            //   → `Filters`(HLG)：`Spacing`(×15) → **`Army Filter`**（`Title` 50 + `Content` 100 起，
            //      13 格 100×100 · pad L14 · 列距 7 · 3 格/行）→ `Spacing (1)`(×12.81)
            //     → **`Owned Toggle`**(335.5×50 · `Image` 239.91→310.56 · `Label` 25.06→234.91)
            //   🔴 **行序与卡牌那套相反**（这里 **Army 在前**）—— 几何全在 `FilterPanelModel.BuildCosmetics`
            //      （与卡组编辑那扇窗**同一份尺子**），本文件不抄第二份。
            //
            // 🆕 **2026-10-03（§三 第 29 条 A11）：两行都建了** —— 原来只建了 `Owned only` 那一行，
            //    `Army Filter` 因为「A4 没给格子尺寸」一直空着（`ClearCosmoFilters` 那时只会出声）。
            //    格尺寸 2026-09-28 已在卡组编辑那扇窗上实测补全（模型里那段注释有全部出处）。
            //    ⚠️ 抽屉矩形 (0.06,155.94) 与 `Abs()` 用的 (FltL=0.25,FltT=155.9) 差 **0.19px/0.04px**
            //       —— 沿用 `Abs` 那一套（< 0.2px，不为此另开一套换算）。
            const float Dl = 0.06f, Dt = 155.94f, Dr = 335.56f;
            var d = Node(page, "Cosmetic FIlter", new PxRect(Dl, Dt, Dr, 1080f));
            _cosmoDrawer = d;
            Rect(d, "40k_main_tab_shadow", new PxRect(Dl, Dt, 153.07f, 1080f), "Shadow", QFlt,
                 new Color(0f, 0f, 0f, 0.314f));
            Rect(d, "40k_main_tab_background", new PxRect(Dl, Dt, Dr, 1080f), "Panel", QFlt);

            // 内容高 = `CosmoContentH(13)` ≈ 627.8 **< 抽屉 924.06** ⇒ **不用滚动**（原版那棵树里也没有
            // `Scroll View`）⇒ 这一份 `FilterPanel.Scroll` **故意留 null**（`RebuildFilterRowsNow` 认这个分支）。
            var p = new FilterPanel
            {
                State = CosmoState, Cosmo = true, Open = false,
                Slide = 0f, SlideTarget = 0f,                  // 🆕 A11：出厂关 ⇒ 起手就在「滑出去」那一头
                OnChanged = RefreshCosmoAfterFilter,
            };
            _fltCosmo = p;
            Scope(p, () => { p.Node = d; RebuildFilterRows(p); });

            // 实证 act=F（A11：整栏停在 `hiddenPosition`）。
            // ⚠️ **这一下是冗余的**（2026-10-05 逐处核过一遍收口面）：上一行的 `RebuildFilterRows(p)`
            //    已经流到 `RebuildFilterRowsNow` 的收尾、在那里 `force: true` 按过一次（见那一行）——
            //    本行算出来的 `on` 与它**逐字相同**，所以是空转（A71 也这么记着）。
            //    **留着，但别把它当成「这里也兜了底」**：命中区那条不变量归 `RebuildFilterRowsNow` 的收尾；
            //    起滑那一头归 `StartDrawerSlide` 的 Play 支（A76①）——⛔ 别删那两处、把账记到这一行上。
            ApplyDrawerSlide(p, 0f);
        }

        public void ToggleCosmoFilters()
        {
            if (_cosmoDrawer == null || _fltCosmo == null) return;
            _fltCosmo.Open = !_fltCosmo.Open;                 // 🆕 A11：与 Cards/Styles 同一套（逻辑态 + 滑动）
            StartDrawerSlide(_fltCosmo);
            RefreshFilterToggles();
            RefreshClearFilters();                           // 🆕 2026-10-05：页头那颗圆钮按**逻辑态**换图
            Debug.Log("[Collection] 卡背页的筛选抽屉 " + (CosmoFiltersOpen ? "打开" : "收起")
                      + "（进度 = " + _fltSlideDesc(_fltCosmo) + "）");
        }

        /// <summary>原版 `Clear filters` —— 清的是**筛选条件**；`Owned only` 那个开关**不动**
        /// （与 Cards/Styles 两页同一条口径，见 `ClearFiltersNow`）。
        /// ⚠️ 原来这一处**只会出声**（「抽屉里只建了 `Owned only`、`Army Filter` 缺格子尺寸」）——
        /// 🆕 2026-10-03（A11）那两行都建了，这里改回真清空。</summary>
        public void ClearCosmoFilters()
        {
            Scope(_fltCosmo, () =>
            {
                var f = DeckFilter.None;
                f.Owned = CosmoState.Filter.Owned;      // 开关不动（同 Cards/Styles）
                CosmoState.SetFilter(f);
                _fltCosmo.OnChanged();
            });
            Debug.Log("[Collection] 卡背页：已清空筛选");
        }

        /// <summary>卡背页当前该铺哪几张 —— **判据只有一份**：`CardbackTable.NamesFor`
        /// （与卡组编辑那扇窗走同一个函数；阵营来自 SO 的 `cardArmy`）。</summary>
        public static string[] FilteredCosmoNames()
        {
            return CardbackTable.NamesFor(CosmoState.Filter.Faction, CardArt.CosmeticNames());
        }

        /// <summary>🆕 A11：卡背页改完筛选 ⇒ 回到顶部 + 重设滚动区高 + 重画卡背格 + 重算空态。
        /// （筛完卡背少了 ⇒ 内容也短了 —— 不重设就会留一段空白滚得到。）</summary>
        void RefreshCosmoAfterFilter()
        {
            var page = PageRoot(2);
            if (page == null) return;
            if (CosmoScroll != null) CosmoScroll.SetOffset(0f);
            var holder = page.Find("Scroll View");
            if (holder != null)
            {
                var names = FilteredCosmoNames();
                int rows = Mathf.Max(1, Mathf.CeilToInt(names.Length / (float)CosmoCols));
                if (CosmoScroll != null)
                {
                    CosmoScroll.ContentX1 = CosmoView.y1;
                    CosmoScroll.ContentX2 = CosmoView.y1 + rows * CosmoCellH;
                }
                RebuildCosmoCells(holder);
                if (_cosmoEmpty != null) _cosmoEmpty.gameObject.SetActive(names.Length <= 0);
            }
            Debug.Log("[Collection] 卡背页筛选：阵营 = "
                      + (string.IsNullOrEmpty(CosmoState.Filter.Faction) ? "全部" : CosmoState.Filter.Faction)
                      + " ⇒ 铺出 " + FilteredCosmoNames().Length + " / " + CardArt.CosmeticNames().Length + " 张卡背");
        }

        // ============================================================ Styles 页（异画；A4 §三）
        //
        // 原版 = `Alternate Art Tab`（脚本 `AlternateArtCardCollectionTab`）→ `Collection Display`（`CardCollectionDisplay`）。
        // 🔴 **本节的坐标是 2026-09-24 从 `Collection Menu Variant` 根节点一路走下来实读的**。
        //    上一轮那批（A4）是**被 `menu_rect.py` 的父链 bug 平移过**的（整页偏 (167.17, 70.94)），
        //    坑与判据见 `资料/已知的坑.md` 那条「`menu_rect.py` 会把 `Content Area` 下的节点整套平移」。
        //
        //   · `Collection Display/Scroll View` **330.22, 287.67 → 1920.01, 1080**（1589.78 × 792.33）
        //   · 格 = `Collection Card`（作者 350×512）→ 槽 **262.5 × 384** · 行距 384
        //     · 列数 = `floor(1589.78 ÷ 262.5)` = **6** · 内容**整体居中**：pad = **7.39**
        //       ⇒ 首格 **337.61, 287.67 → 600.11, 671.67**（中心 **468.86, 479.67**）
        //   · `Header`（**换风格条**，不是标题栏）332.35, 155.94 → 1920.00, 283.94（1587.66×128）
        //     ├ `Select Art Button Right` **1320.40, 190.67 → 1394.78, 266.27**（74.39×75.61）
        //     │   ⚠️ 它的**脚本字段名是 `leftStyleButton`**（挂在 `Select Art Button Right` 上）——
        //     │      **接线按位置、不按字段名**（A4 §五·4；2026-09-24 复核确认）
        //     ├ `Select Art Button Left`  **683.40, 190.67 → 757.78, 266.27**
        //     │   两钮的子件一样：`Background` = `40k_general_bt_yellow` · `Icon` = `40k_general_bt_arrow`；
        //     │   **左钮的 Icon 带 `UIFlippable(m_Horizontal=1)`** ⇒ 同一张图**镜像**，别去找左箭头
        //     ├ `Art Style Logo` **787.59, 155.94 → 1299.59, 283.94**（512×128）
        //     └ `Separator Line (1)` **339.44, 281.86 → 1920.01, 290.44**
        //   · 左抽屉 `Card Filters` **0.25, 155.94 → 335.56, 1080**（与 Cards 页**同矩形**、同 7 行）
        //
        // ---- 我们挑的 / 没建的（逐条出声）----
        //   · **风格图 SO 本地没有**：`Art Style Logo` 原版 `sprite=0`，运行时由 `styleImage` 载
        //     `alternateArtStyles[i]` 的 Addressable，而 `AlternateArtStyleIconsSO` 全库**只有类名没有资产**
        //     ⇒ 我们在那一格里**画风格名文字**（**我们挑的做法，不是原版的**）。
        //   · **风格清单**：🔴 **2026-10-14 就地订正（铁律 5 · `A1808`/`A1830`）** —— 原来写「本机只覆盖
        //     `AA_HB`(6 张) + `v2`(1 张)，其余风格在远端 CCD 的 `alternateartstyles` 包。`v2` 的**显示名查不到**
        //     ⇒ 直接印 token」：**①②③ 全不成立** —— ①「2 档」是我们按图片文件名拆的，**原版只有 1 档**；
        //     ② 那个远端包**已经在手**、两张风格 logo **已导进工程**；③ `v2` 不是一档、也就无所谓「显示名」。
        //     判据 → 上面 `StyleLogoName` 的头注。
        //   · 点一张卡：原版开 `CardDisplayWindow`（卡片详情窗）—— **那扇窗还没建** ⇒ `NotifyNotBuilt`。
        //   · `Card Filters` 的 `Army Filter` 一格仍然没建（A4 没给格子尺寸）。

        /// <summary>网格视口 **330.22, 287.67 → 1920.01, 1080**（1589.78 × 792.33）。</summary>
        public static readonly PxRect StyleView = new PxRect(330.22f, 287.67f, 1920.01f, 1080f);
        /// <summary>格 **262.5 × 384**（作者 350×512，同 Cards 页）。</summary>
        public const float StyleCellW = 262.5f, StyleCellH = 384f;

        /// <summary>列数 = `floor(1589.78 ÷ 262.5)` = **6**。</summary>
        public static int StyleCols { get { return Mathf.Max(1, Mathf.FloorToInt(StyleView.W / StyleCellW)); } }
        /// <summary>内容**整体居中**的左边距 = (1589.78 − 6×262.5) × 0.5 = **7.39**。</summary>
        public static float StylePadX { get { return (StyleView.W - StyleCols * StyleCellW) * 0.5f; } }

        public static PxRect StyleCellRect(int i)
        {
            int r = i / StyleCols, c = i % StyleCols;
            float x = StyleView.x1 + StylePadX + c * StyleCellW;
            float y = StyleView.y1 + r * StyleCellH;
            return new PxRect(x, y, x + StyleCellW, y + StyleCellH);
        }

        // ---- 换风格条（A4 §三 `Header`）----
        public const float StyleArrowW = 74.39f, StyleArrowH = 75.61f;
        public const float StyleArrowY1 = 190.67f, StyleArrowY2 = 266.27f;
        public const float StyleArrowLx = 683.40f, StyleArrowRx = 1320.40f;
        public const float StyleLogoL = 787.59f, StyleLogoR = 1299.59f;
        public const float StyleBarT = 155.94f, StyleBarB = 283.94f;
        public const float StyleSep1L = 339.44f, StyleSep1T = 281.86f, StyleSep1B = 290.44f;
        /// <summary>圆钮里 `Background`/`Icon` 那两层（原版 `sd=(0.3774,0.3774)` 等比）56.86×58.13，居中。</summary>
        public const float StyleArrowIconW = 56.86f, StyleArrowIconH = 58.13f;

        /// <summary>本机能拿到的督军异画（**7 张 · 1 种风格**）。表与出处 = `工具/import_original_art.py` 的 `ALT_ART`。
        /// 🔴 **2026-10-14 就地订正（铁律 5 · 账 `A1830`）**：原来这里写「7 张 · **2 种风格**」，并把第 7 张标成 `"v2"` ——
        /// **那 2 档是【我们自己按图片文件名拆的】，原版只有 1 档**。判据（三路互证）：
        /// ① `AlternateArtSyle` **只有两个成员**：`Default = 0` / `HammerAndBolter = 5`
        ///    （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AlternateArtSyle.cs`）；
        /// ② 原版 `AlternateArtCardCollectionTab.Setup` 调 `AlternateArtStylesUtilities.Instance.styleIcons
        ///    .GetAlternateArtStyleOnly()`，而那个方法的**方法体**是 `FindAll(style != 0)`
        ///    （`d:/2/tools/decomp_full/AlternateArtStyleIconsSO__GetAlternateArtStyleOnly.c` + 它的 lambda）
        ///    ⇒ **轮播里只列非 `Default` 的档**；
        /// ③ 本地 7 张卡的 `AlternateArtCard.style` 全是 **5**：导出件 `cards_1195_2022时代.json` 里
        ///    `AA_HB_DarkAngels_war_Azrael` 的 `style = 5`，而它 `cardImage` 的 AssetGUID `50de37c7…`
        ///    **现解就是** `DarkAngels_AA_warlord_Azrael_v2`（= 我们原来标 `v2` 的那张图）
        ///    ⇒ **`v2` 与 `AA_HB` 是同一张卡的两种文件名写法，不是两种风格**。
        /// ⇒ token 现在直接用**原版枚举名**，7 张全 = `HammerAndBolter`。
        /// ⚠️ **没查清**：`.png` 文件名为什么两套写法（`AA_HB_…` 前缀 vs `…_AA_…_vN` 后缀 + 6 张 `Avatar/Cardback` 共用 `_AA_HB` 后缀）——
        /// 那是**原版美术自己的命名**，我们只是读它。</summary>
        public struct AltArtCard
        {
            public readonly string CardId, Style;
            public AltArtCard(string id, string style) { CardId = id; Style = style; }
        }

        /// <summary>原版风格枚举名的常量（`AlternateArtSyle.HammerAndBolter = 5` 的**名字**；见上条注释的判据）。</summary>
        public const string StyleHammerAndBolter = "HammerAndBolter";
        /// <summary>原版风格枚举名的常量（`AlternateArtSyle.Default = 0`）—— **轮播里不出现**，但
        /// `AlternateArtPanel.SetStyle(style, image)` 取图时它是一档（见 `StyleLogoName`）。</summary>
        public const string StyleDefault = "Default";

        public static readonly AltArtCard[] AltArtCards =
        {
            new AltArtCard("AM2",                 StyleHammerAndBolter),
            new AltArtCard("BL34",                 StyleHammerAndBolter),
            new AltArtCard("SAU35",                StyleHammerAndBolter),
            new AltArtCard("GOF28",                StyleHammerAndBolter),
            new AltArtCard("SW1",                 StyleHammerAndBolter),
            new AltArtCard("UM76", StyleHammerAndBolter),
            new AltArtCard("DA1",                 StyleHammerAndBolter),
        };

        /// <summary>风格顺序（**表里第一次出现的次序**，稳定）。</summary>
        public static string[] AltStyles
        {
            get
            {
                var list = new List<string>();
                foreach (var a in AltArtCards) if (!list.Contains(a.Style)) list.Add(a.Style);
                return list.ToArray();
            }
        }

        /// <summary>某个风格的**显示名**（⚠️ **只在 logo 图载不到时当兜底文字用** —— 原版那一格是**图**，没有字）。
        /// `HammerAndBolter` = **Hammer and Bolter**（出处 `资料/索引与盘点/解包资源使用地图.md:1174` 记的
        /// `AlternateArtStyleIconsSO/HammerAndBolter`）。
        /// 🔴 **2026-10-14 就地订正（铁律 5 · `A1808`）**：原来这里写「其余**查不到**（风格 SO 在服务端）⇒ 原样印 token」——
        /// **不成立**：`AlternateArtStyleIconsSO` 的本体就在**本地**（内置文件 `Warpforge_Data/sharedassets0.assets`，
        /// 无 type tree ⇒ 按原始字节解），它那两条 `styleIcons` 的两张图**现在都在工程里**（见 `StyleLogoName`）。</summary>
        public static string StyleLabel(string style)
        {
            if (style == StyleHammerAndBolter) return "Hammer and Bolter";
            return style;
        }

        /// <summary>某个风格的 **logo 图名**（`Resources/Art/ui_menu/` 下、**原版资产名、空格换下划线** ⇒ 走 `CardArt.MenuUi`）。
        /// 🔴 **映射是原版自己的字节读出来的**（2026-10-14 · `A1808`，判据三路互证）：
        /// ① 内置文件 `Warpforge_Data/sharedassets0.assets` 里 `AlternateArtStyleIconsSO` 的序列化体
        ///    （`m_Name`(24) → `count=2` → 每条 `[style:int32][AssetGUID]`）读出
        ///    **`style 0 (Default)` → `0195addac64424715b03a2310dfbbfe9`** ·
        ///    **`style 5 (HammerAndBolter)` → `cd516d1bf3fd249f3ad4776db89e70f7`**；
        /// ② 那两个 GUID 拿**远端 catalog** 解到同一个束 `alternateartstyles_assets_all_9db3b696…bundle`，
        ///    束内 `m_Container` 键 `'0'` → `WarpForgeLogo Style`、`'c'` → `Cell Shaded Alternative Art Style`（各 512×128）；
        /// ③ 两张图的**内容**也自洽：`Cell Shaded…` 印的是 `CEL SHADED`，`WarpForgeLogo Style` 印的是
        ///    `WARPFORGE THE CARD GAME` 徽标。
        /// ⇒ **`Default` = Warpforge 徽标 · `HammerAndBolter` = CEL SHADED**。
        /// ⚠️ 本机唯一一份来源 = `d:/2/远端缓存_旧profile_1013/…/9db3b696…/__data`（69,579 B；旧/新两版 catalog 里
        /// **内容哈希相同** ⇒ 不因「2022 快照」作废）；导入器 = `工具/import_altstyle_logos.py --install`。</summary>
        public static string StyleLogoName(string style)
        {
            if (style == StyleHammerAndBolter) return "Cell_Shaded_Alternative_Art_Style";
            if (style == StyleDefault) return "WarpForgeLogo_Style";
            return null;                     // 不认识的档 ⇒ 调用方出声 + 退回文字（⛔ 不静默）
        }

        /// <summary>某个风格的 logo 贴图（载不到返回 `null`，由调用方出声 —— 红线：不许静默失败）。</summary>
        public static Texture2D StyleLogo(string style)
        {
            string n = StyleLogoName(style);
            return string.IsNullOrEmpty(n) ? null : CardArt.MenuUi(n);
        }

        /// <summary>当前风格下标（0 起）。**自检也读它**。</summary>
        public int StyleIndex;
        /// <summary>**画面上真会出现的异画张数** = 「属于当前风格」∩「过了筛选」—— 自检用。
        /// ⚠️ 别拿 `StylesState.VisibleCards().Count` 充数：那是**两种风格合起来**的池子
        /// （第一版就这么断的，量出 7 而画面只有 6）。</summary>
        public int StyleVisibleCount { get { return ShownAltArts().Count; } }
        /// <summary>异画页的滚动区（自检用；7 张 = 2 行 ⇒ 本页**滚不动**，如实记）。</summary>
        public MenuScroll StyleScroll;
        public readonly List<Transform> StyleCells = new List<Transform>();

        static DeckEditorState _stylesState;
        /// <summary>异画那一批卡的筛选状态 —— **另一份 `DeckEditorState`**（筛的是异画这批，不是卡池）。
        /// 复用同一套 `DeckFilter` / `VisibleCards()` ⇒ 筛选栏那 7 行**一行代码都不用重写**。</summary>
        public static DeckEditorState StylesState
        {
            get
            {
                if (_stylesState == null)
                {
                    var pool = new List<CardDef>();
                    foreach (var a in AltArtCards)
                    {
                        var c = CollectionData.Card(a.CardId);
                        if (c != null) pool.Add(c);
                        else Debug.LogWarning("[Collection] 异画表里的卡 id 在卡池里查不到：" + a.CardId);
                    }
                    _stylesState = new DeckEditorState(pool);
                }
                return _stylesState;
            }
        }
        /// <summary>自检用：丢掉异画那两份缓存。</summary>
        public static void ResetStylesForTest() { _stylesState = null; }

        /// <summary>当前风格下的异画（按 `AltArtCards` 的次序，**不受筛选影响**）。</summary>
        public List<AltArtCard> CurrentStyleAltArts()
        {
            var list = new List<AltArtCard>();
            string st = AltStyles[Mathf.Clamp(StyleIndex, 0, AltStyles.Length - 1)];
            foreach (var a in AltArtCards) if (a.Style == st) list.Add(a);
            return list;
        }

        /// <summary>**画面上真会出现的那几张** = 「属于当前风格」∩「过了筛选」（原版也是这两个条件叠加）。
        /// `RebuildStyleCells` 与 `StyleVisibleCount` **共用这一份**（两处各写一遍迟早不一致）。</summary>
        public List<AltArtCard> ShownAltArts()
        {
            var visible = StylesState.VisibleCards();
            var show = new List<AltArtCard>();
            foreach (var a in CurrentStyleAltArts())
                foreach (var c in visible)
                    if (c.Id == a.CardId) { show.Add(a); break; }
            return show;
        }

        public void BuildStylesPage(Transform page)
        {
            // ---- 页头 = **共用那一行**（本页字号 42 / 42，逐页不同）----
            BuildFilterHeader(page, 3, 42f, 42f, ClearStyleFilters, ToggleStyleFilters);

            // ---- 换风格条 `Header` ----
            // 🔴 **按位置接线**：左边那颗 = 上一个、右边那颗 = 下一个
            //    （脚本字段名反着：`leftStyleButton` 挂的是**右边**那颗）
            BuildStyleArrow(page, StyleArrowLx, true, () => ChangeStyle(-1));
            BuildStyleArrow(page, StyleArrowRx, false, () => ChangeStyle(+1));
            // `Art Style Logo` = **原版那一格是 `Image`**：`AlternateArtCardCollectionTab.styleImage`
            //   ← `AlternateArtStylesUtilities.GetStyleIcon(style)`（判据 → `StyleLogoName` 的头注）。
            //   🔴 **2026-10-14 就地订正（铁律 5 · 账 `A1808`）**：原来这里画的是**风格名文字**，注释写着
            //   「原版运行时喂图（**风格图标 SO 本地没有**）⇒ 我们画风格名文字（**我们挑的**）」——
            //   **两句都不成立**：SO 的本体就在**本地内置文件**里，而它那两张图（各 512×128）**现在都在工程里**
            //   （`Resources/Art/ui_menu/Cell_Shaded_Alternative_Art_Style.png` / `WarpForgeLogo_Style.png`）。
            //   ⇒ 现在**照原版画图**；载不到才退回画名字（并出声 —— 红线：不许静默失败）。
            //   ⚠️ 图与框同比例（都 512×128 = 4.0）⇒ `keepAspect` 传哪一档画出来都一样；
            //      原版那颗 `Image.m_PreserveAspect` 的值**本地没有判据**，如实记在这里，别当原版值。
            BuildStyleLogo(page);
            Rect(page, "40k_main_line", new PxRect(StyleSep1L, StyleSep1T, 1920.01f, StyleSep1B),
                 "Separator Line (1)", QPageRow);

            // ---- 网格 ----
            var holder = Node(page, "Scroll View", StyleView);
            // 🔴 **2026-10-13（A435 阶段 2 · 乙 · A17）**：裁切状态长在这颗视口节点上
            //   （原版 `Alternate Art Tab/Collection Display/Scroll View/Viewport` 的 `RectMask2D`
            //    `m_Softness=(0,0)`·`m_Padding=(0,0,0,0)`，视口 = 330.22,287.67 → 1920.01,1080）。
            var styleVp = ViewportClip.Hang(holder, "Viewport", StyleView, Vector4.zero, Vector2Int.zero);
            int n = StylesState.VisibleCards().Count;
            int rows = Mathf.Max(1, Mathf.CeilToInt(n / (float)StyleCols));
            StyleScroll = MenuScroll.TopAligned(StyleView, rows * StyleCellH, 100f);   // 原版 `Alternate Art Tab/…/Scroll View` = 100（D_N29）
            StyleScroll.Owner = gameObject;
            StyleScroll.ClipNode = styleVp;             // 🔴 A465（同卡池页那一条）
            StyleScroll.OnChanged = () => RebuildStyleCells(holder);
            PointerLayer.RegisterScroll(StyleScroll);
            // 🆕 2026-10-12（`A1628` 第二轮）：异画页的**逐格相位**（只在真 Play 出声；理由同卡池页那三行）
            BuildWatch.CellsBegin();
            RebuildStyleCells(holder);
            BuildWatch.CellsReport("Alternate Art Tab 异画（只建视口内的格，自检实测 6）");

            // `Empty Collection Warning`：原版 `act=F`，运行期条件 = **过滤后为空**
            // （`CollectionDisplay.RefreshCollection`：`filteredCollection.Count <= 0 ⇒ SetActive`）
            {
                var ew = Node(page, "Empty Collection Warning", StyleView);
                // 🔴 **2026-10-18（第三轮整改 · 审查 P4）字走 `Loc.T`** —— 异画页这一份与 Cards 页**同一条键**
                //   （`MenuCollection/NoCardsFound`，原版 4 颗里有它）。英文列 = TMP 原文；中文列 = `zh_CN.csv:177`。
                var wt = Text(ew, Loc.T("MenuCollection/NoCardsFound"),
                              StyleView.x1, StyleView.x2, StyleView.y1, StyleView.y2, 5, PageInk,
                              "Warning", 36f);
                if (wt != null) wt.SetRenderQueue(QPageText);
                _styleEmpty = ew;
            }

            // 左抽屉（**起手收起**）—— 与 Cards 页**同一份实现**，只是状态与刷新对象不同。
            // ⚠️ **2026-10-05 更正（铁律 5）**：原文写「**出厂 act=T = 展开**」—— 字段读数对、**推论错**：
            //    `act=T` 只等于「节点启用」，**推不出**抽屉停在哪一头。起手态的唯一判据是
            //    `CollectionDisplay<T>.Initialize → filters.SetupFilters()`（VA 反汇编：调用点 `0x1815EC278`，
            //    本体 `0x1815F0740` 收尾 = 记下 `originalAnchorPosition` 后立刻
            //    `anchoredPosition = (hiddenPosition.x, originalAnchorPosition.y)`）⇒ **停在「收起」那一头**；
            //    之后**没有任何一处**在起手把它打开（唯一的开启者 = 页头那颗 toggle 的 `onValueChanged`）。
            //    旁证：四颗 `Filter Toggle` 的 `EverguildToggle.m_IsOn` 全是 0、出厂画的是 `offSprite`；
            //    同 prefab 的**卡组编辑窗**那份 `Card Filters` 也是 `act=T` 而它**已独立证实起手收起**。
            //    逐字段实读见 `资料/普查产出_1005/块8_卡组窗断言与异画页查证.md` 件 B。
            _fltStyles = BuildFilterPanel(page, StylesState, RefreshStylesAfterFilter, false, styles: true);
            RefreshStyleEmpty();
        }

        Transform _styleEmpty;
        /// <summary>`Art Style Logo` 那一格里的字（原版是图；**待替换成风格图标 SO**，本地没有）。
        /// 🔴 **2026-10-16（A830）**：`public float StyleLogoWidthPx`（读 `Label.WorldW`）**已删** ——
        /// 那是一条**缓存口径**（`_tmpW/_tmpH`），而它当时是 `Editor/CollectionScene.cs` 那条
        /// 「渲染宽度 ≤ 512」断言的**唯一尺子** ⇒ 尺子长在被测实现身上。现在自检自己量
        /// TMP 的 `textBounds`（同宿主 `LabelRenderedPx`）；本字段只剩「建的时候判空 + 收尾置 null」两个用处。</summary>
        Label _styleLogo;

        string CurrentStyleName()
        {
            var s = AltStyles;
            return s.Length == 0 ? "" : s[Mathf.Clamp(StyleIndex, 0, s.Length - 1)];
        }

        /// <summary>一个换风格圆钮：`UI_Button_Round_background`（圆底）+ `40k_general_bt_yellow` + `40k_general_bt_arrow`。
        /// 左钮**镜像**（原版 `UIFlippable(m_Horizontal=1)`）—— 我们翻转 uv 的 x。</summary>
        void BuildStyleArrow(Transform page, float x1, bool mirror, System.Action onClick)
        {
            string side = mirror ? "Left" : "Right";
            var r = new PxRect(x1, StyleArrowY1, x1 + StyleArrowW, StyleArrowY2);
            // 🔴🔴 **2026-10-18（A1149 第一半）圆底盘【挂点】归真**（本件 = 第九会话 P6）。
            //   ⚠️ **它不是关窗钮** —— 是「换风格」圆钮；`A1149` 那一行把它记成
            //   `Shell/CollectionWindow.cs:1524`（挂在子件 `Background Round`），**行号是旧的、类别也标错了**：
            //   本文件里唯一「圆底盘画在子件上」的就是**这一处**（`:1534`，现读）。
            //   **改前**：圆底盘画在一颗**自造子件 `Background Round`** 上、节点 `Select Art Button {L,R}` 是裸节点。
            //   **原版实读**（`资料/普查产出_0923/A4_装饰页与驱动链.md:129`，逐字段）：
            //   `/Header/Select Art Button Right` 自己带 `Image[UI_Button_Round_background]` ·
            //   **Simple · `preserveAspect`** · `sizeDelta = 74.386×75.605` · `EverguildButton`；
            //   两个子件 = `Background`(`40k_general_bt_yellow`) 与 `Icon`(`40k_general_bt_arrow`)
            //   ⇒ 圆底盘**长在根节点自己身上**（无独立子件名）。
            //   贴图 `UI_Button_Round_background` 的 sprite `m_Rect` = **237×237 正方**、`m_Border`/`m_Offset` 全 0
            //   ⇒ `PA=1` 等比内接 = **实绘 74.386×74.386**。
            //   ⇒ 照兄弟窗先例 `Shell/BaseOfferPopup.cs` / `Shell/ReferralPopupWindow.cs` 的形状
            //   （`closeQ != null ? closeQ.transform : MenuDraw.Node(…)` —— `Rect` 画在根节点上、图取不到才退回 `Node`）。位置/子件序**逐位不变**（同一份 `Local(parent, …)`）。
            var baseQ = Rect(page, "UI_Button_Round_background", r, "Select Art Button " + side, QPagePanel, null, true);
            var node = baseQ != null ? baseQ.transform : Node(page, "Select Art Button " + side, r);
            float ix = x1 + (StyleArrowW - StyleArrowIconW) * 0.5f;
            float iy = StyleArrowY1 + (StyleArrowH - StyleArrowIconH) * 0.5f;
            var ir = new PxRect(ix, iy, ix + StyleArrowIconW, iy + StyleArrowIconH);
            // A17：原版 `Select Art Button {Left,Right}` 是 SpriteSwap，且 `m_TargetGraphic` 是**子件 `Background`**
            //（普查 §块 3 第 5 行）⇒ 换图落在这张上（我们这颗的常态图 = `40k_general_bt_yellow`）
            var arrowBg = Rect(node, "40k_general_bt_yellow", ir, "Background", QPageRow, null, true);
            // 🔴 **Icon 必须比 Background 高一层队列** —— 两层摆在同一个矩形上，同队列时
            //    「谁盖谁不可控」会把箭头盖掉（2026-09-24 实拍：两颗钮里只看得见黄底、箭头没出现，
            //    而**矩形断言全绿**）。同族坑见 `资料/已知的坑.md`「同一个渲染队列的两层」。
            var q = Rect(node, "40k_general_bt_arrow", ir, "Icon", QPageOverlay, null, true);
            // 左钮**镜像**（原版 `UIFlippable(m_Horizontal=1)`）—— uv 的 u 从 1 到 0
            if (q != null && mirror) q.SetUvRect(new Rect(1f, 0f, -1f, 1f));
            // ⚠️ 名字要**带左右**：两颗钮各有一个 `ArrowHit`，同名的话自检按名字找只会拿到第一颗
            //    （`FindChild` 是按名字找的，传路径进去恒 null —— `资料/已知的坑.md`）
            AddHit(node, "ArrowHit " + side, r, QPageRow, onClick, arrowBg, "40k_general_bt_yellow",
                   clickCue: WindowSounds.DefaultCloseCue);
        }

        /// <summary>切风格（`ChangeStyleButton(bool isLeft)`：**isLeft=true = 上一个(−1)**，越界回绕）。</summary>
        public void ChangeStyle(int dir)
        {
            var s = AltStyles;
            if (s.Length == 0) return;
            int next = ((StyleIndex + dir) % s.Length + s.Length) % s.Length;
            // 🔴 **2026-10-17（桶 C）**：只有 1 档时 `next == StyleIndex` —— 原版那是**死键**
            //   （`ConfigureStyle` 根本不会被调用）。这里必须早退：否则会白白重建一次页面，
            //   把正在点击的箭钮一起销毁（自检第二下点击即 NRE）。
            if (next == StyleIndex) return;
            StyleIndex = next;
            Debug.Log("[Collection] 切风格 → 「" + StyleLabel(s[StyleIndex]) + "」（第 " + (StyleIndex + 1) + "/" + s.Length + " 种）");
            RefreshStyleView();
        }

        /// <summary>重建整个 Styles 页（切风格 / 筛选变了调它）。</summary>
        public void RebuildStylesPage()
        {
            var page = PageRoot(3);
            if (page == null) return;
            for (int i = page.childCount - 1; i >= 0; i--) DestroySafe(page.GetChild(i).gameObject);
            _styleEmpty = null;
            _styleLogo = null;
            BuildStylesPage(page);
        }

        /// <summary>异画页的刷新回调（筛选栏变了调它）—— 与 Cards 页那份是**同一个函数指针位置**，只是重画的东西不同。</summary>
        void RefreshStylesAfterFilter()
        {
            var page = PageRoot(3);
            if (page == null) return;
            var holder = page.Find("Scroll View");
            if (holder != null)
            {
                if (StyleScroll != null) StyleScroll.SetOffset(0f);
                RebuildStyleCells(holder);
            }
            if (_fltStyles != null) RebuildFilterRows(_fltStyles);
            RefreshStyleEmpty();
        }

        /// <summary>画/重画「风格 logo」那一格（`BuildStylesPage` 与切风格共用一份，⛔ 别写两遍）。</summary>
        void BuildStyleLogo(Transform page)
        {
            // `Art Style Logo` = **原版那一格是 `Image`**：`AlternateArtCardCollectionTab.styleImage`
            //   ← `AlternateArtStylesUtilities.GetStyleIcon(style)`（判据 → `StyleLogoName` 的头注）。
            //   🔴 **2026-10-14 就地订正（铁律 5 · 账 `A1808`）**：原来这里画的是**风格名文字**，注释写着
            //   「原版运行时喂图（**风格图标 SO 本地没有**）⇒ 我们画风格名文字（**我们挑的**）」——
            //   **两句都不成立**：SO 的本体就在**本地内置文件**里，而它那两张图（各 512×128）**现在都在工程里**
            //   （`Resources/Art/ui_menu/Cell_Shaded_Alternative_Art_Style.png` / `WarpForgeLogo_Style.png`）。
            //   ⇒ 现在**照原版画图**；载不到才退回画名字（并出声 —— 红线：不许静默失败）。
            //   ⚠️ 图与框同比例（都 512×128 = 4.0）⇒ `keepAspect` 传哪一档画出来都一样；
            //      原版那颗 `Image.m_PreserveAspect` 的值**本地没有判据**，如实记在这里，别当原版值。
            var _styleTex = StyleLogo(CurrentStyleName());
            if (_styleTex != null)
            {
                Rect(page, StyleLogoName(CurrentStyleName()),
                     new PxRect(StyleLogoL, StyleBarT, StyleLogoR, StyleBarB),
                     "Art Style Logo", QPageText, null, keepAspect: true);
            }
            else
            {
                Debug.LogWarning("[Collection] 风格 logo 图载不到（style=" + CurrentStyleName()
                                 + "）⇒ 退回画风格名 —— 查 `Resources/Art/ui_menu/` 与 `CardArt.MenuUi`");
                // 兜底 = 画风格名。**必须限宽自适应** —— 那一格是 **512×128**，`Hammer and Bolter` 按 56px
                //   画出来宽 ≈1270px，**直接压到右箭钮上**（第一版实拍一眼可见）。`SetAutoFitBox` 按框宽缩到放得下为止。
                //   ⚠️ 判据要量**渲染宽度**，不是比字号 —— 见 `CLAUDE.md` §二 的 `AutoFitBox` 教训。
                //   🔴 **2026-10-16（A830）**：那个「渲染宽度」现在由**自检自己量**
                //      （TMP 自己渲出来那块 `textBounds`，`Editor/CollectionScene.cs` 的 `LabelRenderedPx`）——
                //      ⛔ 本窗那个 `StyleLogoWidthPx` 属性（读 `Label.WorldW` = **字段缓存** `_tmpW/_tmpH`，
                //      只有 `RefreshBounds()` 写 = **被测实现自己**）**已删**，别再把它加回来当尺子。
                //      （判据 → `资料/普查产出_1014/RO_缓存口径与输入三件.md` §一·3；同族先例 = W4/W5/W6 三个宿主换口。）
                //   🔴 **2026-10-09（A1173）**：那对 `(18f, 56f)` 现在直接喂 `Text` 的形参（原来是「先 `Text`、
                //      再手工 `SetAutoFitBox`」两份写法；且 `Text` 里那道裁切还被那次重排抹掉）。
                _styleLogo = Text(page, StyleLabel(CurrentStyleName()), StyleLogoL, StyleLogoR, StyleBarT, StyleBarB,
                                  6, PageInk, "Art Style Logo", 56f,
                                  // 框 = 那一格自己（`StyleLogoR−L` × `StyleBarB−T`），与收口前手传的两维逐值相同
                                  wrapPx: StyleLogoR - StyleLogoL, autoMinPx: 18f, autoMaxPx: 56f);
                if (_styleLogo != null)
                    _styleLogo.SetRenderQueue(QPageText);
            }
        }

        /// <summary>🔴 **2026-10-17（第四十八会话 · 桶 C）：切风格只刷新，不整页重建。**
        /// <para>原版 `ChangeStyleButton` → `ConfigureStyle`（decomp `:45/:50`）只换 logo 与卡格；
        /// 我们原来调 `RebuildStylesPage()` **把整页（含两颗箭钮）`DestroyImmediate`** ——
        /// 于是「点右箭、再点左箭」第二下点在已销毁的对象上（`WindowButton.PlayClickSound`
        /// 的 `get_transform` NRE，`CollectionScene` 整条崩）。</para>
        /// <para>现在只重画 logo + 卡格 + 空态；页头/箭钮/分隔线/滚动区**原地不动**。</para></summary>
        void RefreshStyleView()
        {
            var page = PageRoot(3);
            if (page == null) return;

            var oldLogo = page.Find("Art Style Logo");
            if (oldLogo != null) DestroySafe(oldLogo.gameObject);
            _styleLogo = null;
            BuildStyleLogo(page);

            var holder = page.Find("Scroll View");
            if (holder != null)
            {
                if (StyleScroll != null) StyleScroll.SetOffset(0f);
                RebuildStyleCells(holder);
            }
            RefreshStyleEmpty();
        }

        void RefreshStyleEmpty()
        {
            if (_styleEmpty != null) _styleEmpty.gameObject.SetActive(StylesState.VisibleCards().Count <= 0);
        }

        void RebuildStyleCells(Transform holder)
        {
            var vp = holder.Find("Viewport");
            var parent = vp != null ? vp : holder;
            for (int i = parent.childCount - 1; i >= 0; i--) DestroySafe(parent.GetChild(i).gameObject);
            StyleCells.Clear();

            // 🔴 **2026-10-13（A435 阶段 2 · 乙 · A17）**：原来这里是一对「`Clip = StyleView;` → 循环 →
            //   `Clip = prevClip;`」—— **整对删掉**（裁切状态长在 `holder/Viewport` 那颗 `ViewportClip` 上，
            //   本循环所有件都挂在它下面 ⇒ 沿父链解析得到同一份；留着旧写法 = 节点被形参盖住，静默）。
            var show = ShownAltArts();

            float scale = StyleCellH / (CardView.Height * 108f);      // 按**卡位高 384** 反解（同 Cards 页）
            for (int i = 0; i < show.Count; i++)
            {
                var a = show[i];
                var card = CollectionData.Card(a.CardId);
                if (card == null) continue;
                var content = StyleCellRect(i);
                var r = StyleScroll.Shift(content);
                if (!StyleScroll.Intersects(r)) continue;
                BuildWatch.CellBegin();                      // 🆕 A1628 第二轮：逐格相位（只在真 Play 出声）

                var d = BattleDriver.ToCardData(card, card.Faction);
                // 🔴 **立绘换成异画**（`CardData.artOverride`）—— 卡框/数值/名字照旧用原卡
                d.artOverride = CardArt.AltArt(a.CardId);
                if (d.artOverride == null)
                    Debug.LogWarning("[Collection] 异画立绘取不到（卡面会退回普通立绘）：" + a.CardId);

                var v = CardView.Create(parent, d, "CollectionAltArt_" + a.CardId);
                if (v == null) continue;
                v.gameObject.SetActive(true);
                BuildWatch.CellPhase("① `CardView.Create`（建整张卡：各层 + TMP + `FitToBox` 二分 + `FitToWidth`）");
                // 🔴 **2026-10-08（A181）**：异画页同样要给卡带上裁切 —— 判据与卡池页那条逐字相同
                //   （原版 `Alternate Art Tab/Collection Display/Scroll View/Viewport` 的 `RectMask2D`
                //   `m_Softness=(0,0)`·`m_Padding=(0,0,0,0)`；视口 = 330.22,287.67 → 1920.01,1080）。
                //   🔴 **2026-10-13（A435 辛 · A774）**：边界改成**沿父链解析**（= 上面那颗
                //   `holder/Viewport` 上的 `ViewportClip`），原来传的是 `StyleView` 那个常量矩形
                //   ⇒ 第二份状态源。顺序与原由同卡池页那一处（`SetPose` 定完位姿再解析）。
                v.SetPose(Local(parent, r.x1, r.y1, r.x2, r.y2), 0f, scale);
                BuildWatch.CellPhase("② `SetPose`（摆位姿 —— 首次无裁切 ⇒ 这一句里的重裁是空转）");
                v.SetClipFromTree();
                BuildWatch.CellPhase("③ `SetClipFromTree`（解析裁切 + 第一刀真裁：逐层切网格 + 逐字重排）");
                // 🔴 **2026-10-12（`A1628` 第二轮）删掉了这里的 `v.SetData(d)`** —— 与卡池页那一处
                //   **逐条同由**（`Create` → `Build(d)` 已经把 `Data` / 文字 / 徽标 / 数值层全部做完，
                //   而 `SetData(d)` 只有「把 5 段 TMP 再排一遍」是有实质的）⇒ 完整论证与退回办法
                //   写在 `RebuildCardsCells` 那一行上（⛔ 别在这里再抄一份理由）。
                //   📌 这一页**多一条**：`d.artOverride` 是**在 `Create` 之前**设好的（上面那两行）
                //   ⇒ `Build` 那次就已经按异画建过立绘，`SetData` 那次并没有额外换图。
                v.SetFace(CardFace.Full);      // ⚠️ 空操作：`Create` 已经按 `CardFace.Full` 建（`SetFace` 首句早退）
                v.SetHighlight(CardHighlightState.Normal);
                BuildWatch.CellPhase("④ `SetFace` + `SetHighlight`（`SetFace` 是空操作）");
                StyleCells.Add(v.transform);
                AddHit(parent, "AltArtHit_" + a.CardId, r, QPageRow, () => OpenCardDetail(card));
                BuildWatch.CellPhase("⑤ 命中区（`AddHit`；本页**没有**张数条）");
                BuildWatch.CellEnd();
            }
        }

        // ============================================================ 筛选栏：建 / 刷 / 点
        //
        // 🔴 **2026-09-24 收口**：这一整套（建 / 开合 / 清空 / 点格 / 重画）**只有一份实现**，
        //    靠模块级的 `_flt` 指针决定作用在 **Cards 页**还是 **Styles 页**那份上。
        //    起因：异画页的左抽屉在原版里是**同一个矩形、同一套 7 行**（实测都是
        //    `Card Filters` **0.25,155.94 → 335.56,1080**）⇒ 再抄一份就是铁律说的「两处写同一条规则」。

        /// <summary>**Cards 页**的筛选栏开着没有（`Filters` 圆钮开合）。原版默认**收起**。</summary>
        public bool FiltersOpen { get { return _fltCards != null && _fltCards.Open; } }
        /// <summary>**Cards 页**筛选栏那一列的滚动量（自检用）。</summary>
        public MenuScroll FilterScroll { get { return _fltCards != null ? _fltCards.Scroll : null; } }
        /// <summary>**Cards 页**画出来的筛选格数（自检用）。</summary>
        public int FilterCellCount { get { return _fltCards != null ? _fltCards.Cells.Count : 0; } }
        /// <summary>🆕 **卡背页**（`Cosmetic FIlter`）的筛选格数（自检用）—— 一共 **14** = Army 13 + Owned 1。</summary>
        public int CosmoFilterCellCount { get { return _fltCosmo != null ? _fltCosmo.Cells.Count : 0; } }
        /// <summary>**Styles 页**的抽屉开着没有（自检用）。**起手收起**。
        /// ⚠️ **2026-10-05 更正（铁律 5）**：原文写「出厂就展开（实证 act=T）」—— 读数对、推论错
        /// （`act=T` ≠ 抽屉展开）⇒ 改成与其余三页一致；判据链见 `:1150` 那段。</summary>
        public bool StyleFiltersOpen { get { return _fltStyles != null && _fltStyles.Open; } }

        /// <summary>建一页的左侧筛选栏。**起手收起**（原版靠 `hiddenPosition` 滑出去；
        /// 🆕 A11 起我们也是「滑出去」—— 收起时整栏真的滑到屏幕左外（行程 = 原版
        /// `hiddenPosition.x − originalAnchorPosition.x` = **−385 px**），只是**已滑完**所以不画）。
        /// <paramref name="openAtStart"/> = 建出来就让抽屉停在**展开**那一头（`Slide = 1`）。
        /// ⚠️ **2026-10-05 更正（铁律 5）**：原文写「= 出厂就展开（**异画页是 act=T**）」—— 半句误读、
        /// 半句不成立：`act=T` 推不出展开，且**原版四页起手一律收起**（判据见 `:1150`）。
        /// 本文件现在**两个调用点都传 `false`**（`:620` Cards · `:1151` Styles；Cosmo/Deck 各建各的）。
        /// 形参留着 = 「将来真有哪一页要起手展开」时的**唯一**下手处 —— ⛔ 不是给异画页的。
        /// 返回这一页那份 <see cref="FilterPanel"/>。</summary>
        FilterPanel BuildFilterPanel(Transform page, DeckEditorState state, System.Action onChanged,
                                            bool openAtStart, bool styles = false)
        {
            var p = new FilterPanel { State = state, OnChanged = onChanged, Open = openAtStart, Styles = styles };
            p.Slide = p.SlideTarget = openAtStart ? 1f : 0f;      // 🆕 A11：起手就在该在的那一头
            Scope(p, () =>
            {
                var panel = Node(page, "Card Filters", FltView);
                _fltPanel = panel;

                // 兄弟序照原版：`Shadow` **先**、面板本体**后** ⇒ 面板压在影子上
                Rect(panel, "40k_main_tab_shadow", new PxRect(FltL, FltT, FltL + FltShadowW, FltT + FltViewH),
                     "Shadow", QFlt, new Color(0f, 0f, 0f, 0.314f));
                Rect(panel, "40k_main_tab_background", FltView, "Panel", QFlt);
                Node(panel, "Scroll View", FltView);
                // 🔴 **2026-10-13（A435 阶段 2 · 乙 · A18）**：裁切状态长在这颗视口节点上
                //   （原版 `Card Filters/Scroll View/Viewport` 的 `RectMask2D`：本壳这四页的左栏同族，
                //    `m_Softness=(0,0)`·`m_Padding=(0,0,0,0)` —— 与 `RebuildFilterRowsNow` 里原来那一对
                //    `Clip = FltView; … Clip = prevClip;` 逐字同值）。
                //   ⚠️ **卡背页那一份不走这里**（它没有 `Scroll View/Viewport` 子树、`FilterPanel.Node` 是
                //    `Cosmetic FIlter`，不在本节点的父链上）⇒ 挂在这里**够不着它**，原版那一份也确实不裁
                //    （内容高 ≈627.8 < 抽屉 924.06）。**这不是「顺手生效」**，是两棵树。
                var fltVp = ViewportClip.Hang(panel.Find("Scroll View"), "Viewport", FltView,
                                              Vector4.zero, Vector2Int.zero);

                // ⚠️ `Owner` 指向**面板**（不是窗口）—— `PointerLayer.HitScroll` 靠它判「这一区还开着没」
                //    （面板收起时整块 SetActive(false)，滚轮就不该再被这一列吃掉）
                //    🆕 A11：滑动期间节点是**活着的**（要画）⇒ 那条判据不够用，改由
                //    `SetDrawerInteractive` 在滑动期间**撤登记**、完全展开再登记回来（见那个函数）。
                _fltScroll = MenuScroll.TopAligned(FltView, FilterPanelModel.ContentHFor(_flt != null ? _flt.State : null), 50f);   // 原版 `Card Filters/Scroll View` = 50（D_N29）
                _fltScroll.Owner = panel.gameObject;
                _fltScroll.ClipNode = fltVp;                 // 🔴 A465（同上；⚠️ `Owner` 是**面板**、节点在它下面 ⇒ 只能显式给）
                _fltScroll.OnChanged = () => RebuildFilterRows(p);
                PointerLayer.RegisterScroll(_fltScroll);

                RebuildFilterRows(p);                        // **先建**（内部会临时激活 —— TMP 量不到非激活对象）
                // 再按进度摆：位置 / 显隐 / 命中 / 滚轮（A11）。
                // ⚠️ **这一下同样是冗余的**（2026-10-05 与 `BuildCosmoDrawer` 那处一起核的）：
                //    上一行的收尾已经在 `RebuildFilterRowsNow` 里 `force: true` 按过一次；这里 `Slide`
                //    与它同值 ⇒ 空转。⛔ 别把命中区那条不变量的账记到这一行（同 `BuildCosmoDrawer` 那条注释）。
                ApplyDrawerSlide(p, p.Slide);
            });
            return p;
        }

        /// <summary>`Filters` 圆钮 / 自检：开合筛选栏。</summary>
        public void ToggleFilters() { Scope(_fltCards, ToggleFiltersNow); }
        /// <summary>异画页的左抽屉开合（`Filter Toggle`）。</summary>
        public void ToggleStyleFilters() { Scope(_fltStyles, ToggleFiltersNow); }

        void ToggleFiltersNow()
        {
            _fltOpen = !_fltOpen;                      // 逻辑态**立即**翻转（UI/自检读的都是它）
            StartDrawerSlide(_flt);                    // 🆕 A11：视觉走滑动（Play）/ 直接到位（批处理）
            RefreshFilterToggles();
            RefreshClearFilters();                    // 🆕 2026-10-05：页头那颗圆钮按**逻辑态**换图
            Debug.Log("[Collection] 筛选栏 " + (_fltOpen ? "打开" : "收起")
                      + "（原版**滑进滑出**：`hiddenPosition.x = -550` · `originalAnchorPosition.x = -165`"
                      + " ⇒ **行程 -385px**；`animationTime = 0.3` ⇒ 我们也是位移 + 0.3 秒，进度 = "
                      + _fltSlideDesc(_flt) + "）");
        }

        /// <summary>日志里那句进度的人话（`Open` 与 `Slide` 在动画期间会不同 —— 说清楚）。</summary>
        string _fltSlideDesc(FilterPanel p)
        {
            if (p == null) return "（没有这一份）";
            return p.Slide.ToString("F2") + " → " + p.SlideTarget.ToString("F0")
                   + (p.Slide == p.SlideTarget ? "（已到位）" : "（滑动中）");
        }

        /// <summary>清空筛选（`Clear filters` 钮）。原版回到「不限」那一套。</summary>
        public void ClearCardFilters() { Scope(_fltCards, ClearFiltersNow); }
        /// <summary>异画页的 `Clear filters`。</summary>
        public void ClearStyleFilters() { Scope(_fltStyles, ClearFiltersNow); }

        void ClearFiltersNow()
        {
            if (PointerLayer.Instance != null && PointerLayer.Instance.TextEditing) PointerLayer.Instance.EndText(false);
            // 🔴 **两个开关不动**（与卡组编辑同一口径，2026-09-28）：原版 `showOnlyOwnedCards` 只有
            //    `ToggleShowOwnedCards` 一个写点，`Clear filters` 清的是 `filters[]` 那几件。
            var f = DeckFilter.None;
            f.Owned = FltState.Filter.Owned;
            f.Upgradable = FltState.Filter.Upgradable;
            FltState.SetFilter(f);
            _flt.OnChanged();                       // 各页自己决定重画什么（Cards：卡池 + 计数条；Styles：异画格）
            RefreshClearFilters();   // 🆕 A1496：`Clear filters` 的显隐跟着筛选走（四页一起刷，幂等）
            Debug.Log("[Collection] 已清空筛选");
        }

        /// <summary>点了一格筛选（`$name` / `$fac:x` / `$rar:x` / `$cost:i` / `$type:x`）—— **Cards 页用**。</summary>
        public void ApplyCardFilter(string key) { Scope(_fltCards, () => ApplyFilter(key)); }
        /// <summary>异画页点了一格筛选。</summary>
        public void ApplyStyleFilter(string key) { Scope(_fltStyles, () => ApplyFilter(key)); }

        /// <summary>自检入口：**直接给 Cards 页设一个筛选**（走的是**同一条**刷新路：`SetFilter` + `OnChanged`）。
        /// 为什么需要：批处理里**敲不出键盘**，而「筛到空结果 ⇒ 出 `Empty Collection Warning`」这条
        /// 只能用空结果去触发（拿 `$name` 那条路要 `PointerLayer` 的文本输入，批处理下没有）。</summary>
        public void UiSetCardFilter(DeckFilter f)
        {
            Scope(_fltCards, () => { FltState.SetFilter(f); _flt.OnChanged(); });
            RefreshClearFilters();   // 🆕 A1496：`Clear filters` 的显隐跟着筛选走（四页一起刷，幂等）
        }

        /// <summary>点了一格筛选（`$name` / `$fac:x` / `$rar:x` / `$cost:i` / `$type:x`）。</summary>
        void ApplyFilter(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (key == "$name")
            {
                var pl = PointerLayer.Instance;
                if (pl == null) return;
                // 🔴 **键盘那三个回调是「后来」才跑的**（每次按键 / 回车 / ESC）—— 那时 `_flt` 早就还原了
                //    ⇒ **必须先把这一份面板捕获下来**，回调里一律走 `owner`，不许再碰 `_flt`。
                var owner = _flt;
                pl.BeginText(FltState.Filter.Name ?? "", 24,
                             s => Scope(owner, () => { var f0 = owner.State.Filter; f0.Name = (s ?? "").Trim(); owner.State.SetFilter(f0); owner.OnChanged(); Debug.Log("[Collection] 卡名筛选：" + (string.IsNullOrEmpty(f0.Name) ? "（清空）" : f0.Name)); }),
                             () => RebuildFilterRows(owner),
                             s => { var t = owner.Node != null ? owner.Node.Find("Scroll View/Viewport/Name Filter/Input Text") : null; var lb = t != null ? t.GetComponent<Label>() : null; if (lb != null) lb.SetText(s + "_"); });
                Debug.Log("[Collection] 卡名筛选：输入后回车确认，ESC 取消");
                return;
            }
            // 🔴 **「点一格改哪个条件」只有一份实现**（`FilterPanelModel.Click`，卡组编辑走同一个函数）——
            //    这里只负责「出声」与重画。
            if (!FilterPanelModel.Click(FltState, key)) return;
            if (key == "$owned")
                Debug.Log("[Collection] Owned only：" + (FltState.Filter.Owned
                    ? "只列已拥有 —— **单机全解锁 ⇒ 结果不变**" : "已关"));
            if (key == "$upgradable")
                Debug.Log("[Collection] Upgradable only：" + (FltState.Filter.Upgradable
                    ? "只列可升级 —— **单机版没有升级系统 ⇒ 必然筛成空**（预期行为）" : "已关"));
            _flt.OnChanged();
            RefreshClearFilters();   // 🆕 A1496：`Clear filters` 的显隐跟着筛选走（四页一起刷，幂等）
        }

        /// <summary>**Cards 页**改完筛选：重画卡池（回到顶部）+ 重刷筛选栏选中态 + 重算计数条。</summary>
        void RefreshCardsAfterFilter()
        {
            var page = PageRoot(1);
            if (page == null) return;
            var holder = page.Find("Scroll View");
            if (holder != null)
            {
                if (CardsScroll != null) CardsScroll.SetOffset(0f);
                RebuildCardsCells(holder);
            }
            if (_fltCards != null) RebuildFilterRows(_fltCards);
            RefreshCardsEmpty();       // 「过滤后为空」那条提示跟着筛选走（与 Styles 页同一条判据）
            // 🔴 万能卡计数条：**2026-09-28 起恒定 `99`**（原版跟着「指针悬停的那张卡」走
            //    —— `CardCollectionDisplay.CheckFocusedArmy`，而我们没有 hover）⇒ 建页时已写死，这里不再重算。
            //    判据 → `项目任务.md` §三 第 15 条 第 29 项。
        }

        /// <summary>费用那一档的**上界**（原版 8 档：`1-` / 2…7 / `8+`）。</summary>

        /// <summary>🔴 **2026-10-18（A834 · 候选甲）**：把抽屉摆回**基准位**（位移 0）—— **建格子之前**调一次。
        ///
        /// <para>**它治什么**：「世界 → 局部落位」这条链上**两个读口**读的都是**父件【当下】的位置** ——
        /// `MenuDraw.Local`（=`RectCenter(r) − PosInDesignSpace(parent)`）与
        /// `Label.AlignLeftOn/AlignRightOn` → `Label.ParentXInDesignSpace()`（`Battle/Label.cs`）——
        /// 而筛选栏这一批件**两者都用到**（格子的 `Node`/`Rect` 走前者、格子里的字走后者）。
        /// 面板停在**收起位**（左移 385px）时建的那一版，局部坐标里**多烘了整段行程**：
        /// 出生那一刻看着是对的（跟着面板一起偏），**面板滑回原位之后整列偏 +385px**，
        /// 而 0.3 秒的滑入过程中它**一直**与别的件不在同一帧上（= A834 记的「过渡偏位」）。</para>
        ///
        /// <para>**为什么这样修而不是「按节点记设计矩形」**：本工程对「父件被挪 + 子件按绝对矩形落位」
        /// 这一类**已有两种在用的解法**，本条用的是**最省的那种**（同类现成写法 =
        /// `Deck/DeckRuntime.DrawerHome`（`:1602` 起）与它旁边那句 `PrepareDrawerForBuild`，调用点 `:4993` / `:5280`；
        /// 另两处「先摆父件、再建子件」= `Shell/FriendsTab.cs` / `Shell/AllianceMemberTab.cs`）。
        /// 记矩形那条（要动 `Shell/MenuDraw.cs` **与** `Battle/Label.cs`）改动面跨到战斗侧共用件，
        /// 见 `资料/普查产出_1018/Q-A834_裁定取证.md` §6/§7（本条的取证与三个候选都在那儿）。</para>
        ///
        /// <para>⚠️ **它只在建树期间生效、不外泄**：`RebuildFilterRowsNow` 收尾那次
        /// `ApplyDrawerSlide(_flt, _flt.Slide, true)`（同一帧内、同步）按进度把面板摆回去
        /// ⇒ Play 下没有任何一帧渲染到「临时在基准位」的样子。</para>
        ///
        /// <para>⚠️ **`HasBasePos == false` 那一档不是多余**：起手那次建（`BuildFilterPanel` / `BuildCosmoDrawer`）
        /// 发生在 `ApplyDrawerSlide(p, 0f)` **之前** —— 那时节点**本来就在基准位**，
        /// 而 `BasePos` 还没被谁抓过 ⇒ 这里顺手抓一次（值与 `ApplyDrawerSlide` 抓的是同一个，
        /// 判据同 `RebuildFilterRowsNow` 里 `RowsBuiltOffBase` 那条注释）。</para></summary>
        void DrawerHome(FilterPanel p)
        {
            if (p == null || p.Node == null) return;
            if (!p.HasBasePos) { p.BasePos = p.Node.localPosition; p.HasBasePos = true; }
            p.Node.localPosition = p.BasePos;
        }

        /// <summary>按当前筛选条件 + 滚动量，把 7 行摆出来。**每次刷新都重建**（29 格 + 1 个搜索框，量小，省一套脏标记）。
        ///
        /// 🔴 **2026-09-23 修**：进来先把面板**临时激活**，建完再还原。
        ///    起因（实拍 + 断言双查）：本方法是「先 `SetActive(false)` 再建」的，而 **TMP 在非激活对象上量不出尺寸**
        ///    —— `Label.RefreshBounds` 读到的 `textBounds.size.x` 是 0（`WorldW` 返回下限 `1e-4`），
        ///    于是 `AlignRightOn/AlignLeftOn` 里 `worldRight - WorldW*0.5` 退化成「**把锚点放在右边界**」，
        ///    整段字右移半个字宽（`common`/`legendary` 那几个标签就是这么跑到面板边上被切掉的）。
        ///    ⚠️ 这正是 `CLAUDE.md` §三 记的那条：「TMP 在对象没激活时量不出尺寸，`ForceMeshUpdate()` 要在
        ///    `SetActive(true)` **之后**调」—— 那次是 tooltip，这次是筛选栏。**同类坑在同一工程里第三次出现。**
        ///    断言的空转也要记一笔：我第一条「标签不越出左边界」的断言**恒真**（左边缘 = 右边界 − 0/2），
        ///    是「量出来 0.0px」把它暴露出来的 —— 断言光「过」不够，还得**看一眼量出来的数**。</summary>
        void RebuildFilterRows(FilterPanel p)
        {
            if (p == null) return;
            var prev = _flt;
            _flt = p;
            try
            {
                RebuildFilterRowsNow();
            }
            finally { _flt = prev; }
        }

        void RebuildFilterRowsNow()
        {
            var panel = _flt.Node;
            if (panel == null) return;
            // 🆕 **2026-10-16（A811）**：记下这一版是**按哪一帧的框**建的（判据写在该字段的头里）。
            //   · `HasBasePos == false` ⇒ 这根杆子**一次都没被 `ApplyDrawerSlide` 挪过**（节点就在原位）
            //     ⇒ 这一版是**好的**；
            //   · 挪过之后，**只有 `Slide >= 1`（停在原位）**建出来的才是好的。
            //   🔴 ⛔ 别把 `HasBasePos` 那一项当成多余：起手那次建（`BuildFilterPanel` / `BuildCosmoDrawer`）
            //     发生在 `ApplyDrawerSlide(p, 0f)` **之前** —— 那时 `Slide` 已经是 0、而节点**还在原位**
            //     ⇒ 少了这一项，那一次会被误判成「按错框建的」，于是**每次展开都白重建一遍**
            //     （不是错，但会把别人手里的节点引用打散 —— 见 `ApplyDrawerSlide` ④ 的第二条限定）。
            _flt.RowsBuiltOffBase = _flt.HasBasePos && _flt.Slide < 1f;
            // 🔴 **2026-10-18（A834 · 甲 · 铁律 5 订正）**：**建之前先把抽屉摆回基准位** ——
            //   这一句是从 `Deck/DeckRuntime.DrawerHome` 抄来的同类解法，它**同时**治好两个读口
            //   （`MenuDraw.Local` 与 `Label.ParentXInDesignSpace`，见 `DrawerHome` 的 doc），
            //   于是**上面那句标志**（`RowsBuiltOffBase`）**不再表示「这一版是错的」**：
            //     · 改前：建的时候面板停在哪，落点就按哪一帧算 ⇒ 收起态那一版**局部坐标多烘 385px**
            //       ⇒ 0.3 秒滑入期间整列偏 0→+385px，只有 ④ 在到位那一拍把它重排回来；
            //     · 改后：**每一版都按基准帧建** ⇒ 整列跟着面板**刚性**平移、途中每一帧都对，
            //       `RowsBuiltOffBase` 于是只等于「④ 会在到位那一拍把**同一件事再做一遍**」（冗余，不是错）。
            //   🔴 **⛔ 别以为它多余**：`HasBasePos == false` 那一档（起手建）它顺手抓一次 `BasePos`，
            //     与 `ApplyDrawerSlide` 抓的是同一个值；而**去掉这一句**，`RowsBuiltOffBase` 就成了**谎话**
            //     （标志说「按错帧建的」，实际却建在基准帧上）。
            DrawerHome(_flt);
            bool wasActive = panel.gameObject.activeSelf;
            if (!wasActive) panel.gameObject.SetActive(true);     // ⇒ 建的时候必须活着（见上）

            var vp = panel.Find("Scroll View/Viewport");
            var parent = vp != null ? vp : panel;
            for (int i = parent.childCount - 1; i >= 0; i--) DestroySafe(parent.GetChild(i).gameObject);
            _fltCells.Clear();

            if (_fltScroll != null || _flt.Cosmo)
            {
                // 卡背页那一份**没有滚动区**（内容 `CosmoContentH(13)` ≈ 627.8 < 抽屉 924.06）⇒ 不裁剪
                // 🔴 **2026-10-13（A435 阶段 2 · 乙 · A18）**：原来是「`var prevClip = Clip;` →
                //   `if (!cosmo) Clip = FltView;` → 建 → `Clip = prevClip;`」那三行 —— **全部删掉**。
                //   裁切状态迁到了 `BuildFilterPanel` 里那颗 `…/Scroll View/Viewport` 的 `ViewportClip` 上。
                //   ⚠️ **条件那一档（`!cosmo`）不是靠「挂节点时也加条件」保住的**：卡背页那一份的树里
                //   根本没有 `Scroll View/Viewport`（`FilterPanel.Node` = `Cosmetic FIlter`，不在那颗节点的
                //   父链上）⇒ 它**天然吃不到**节点态，与迁移前「故意不裁」逐字同效。
                bool cosmo = _flt.Cosmo;
                bool styles = _flt.Styles;              // 🆕 A248：异画页那一档字号（`cosmo` 那一份不用它）

                if (cosmo)
                {
                    BuildCosmoRowModel();
                }
                else
                {
                    // 🔴 **2026-10-11（A248）**：`styles` 一路传下去 —— 异画页的**字号那一档**与卡牌页不同
                    //   （模型侧三个函数各有一个**可选**形参，缺省 = 卡牌页 = 共用常量）。⛔ 只有这一个来源，
                    //   别在三个函数里各判一次 `_flt`（两处写同一条规则 = 迟早不一致）。
                    BuildFilterRowModel(styles);
                    BuildNameRow(parent, styles);
                    BuildFilterTitles(parent, styles);
                }

                foreach (var c in _fltCells)
                {
                    // 🔴 **2026-10-16（§三第2条）**：`_fltCells` 现在直接装**模型**那一份
                    //   （`FilterPanelModel.Cell`，坐标 = **面板内**）⇒ 这一行三处都要过 `Abs(...)`
                    //   （模型里那三个 rect 是面板内坐标；`Abs` = 加 `FltL/FltT` 换成页面绝对坐标）。
                    //   ⛔ 漏一个就是「整排偏上/偏左 155.9px」那一族静默错位（见 `Abs` 的注释）。
                    var r = cosmo ? Abs(c.R) : _fltScroll.Shift(Abs(c.R));
                    if (!cosmo && !_fltScroll.Intersects(r)) continue;
                    var cell = Node(parent, "Cell_" + KeyToName(c.Key), r);
                    var b = cosmo ? Abs(c.Bg) : _fltScroll.Shift(Abs(c.Bg));
                    // 🔴 2026-10-05（A32①）：**关着时换成 off 那张**（原版 `EverguildToggle.onSprite/offSprite`
                    //   —— 那三颗 `changeSpriteOnValueChange = 1`）· 着色走 `CellTint`（开关那一类给白，
                    //   因为原版那三颗 `colorTintOnValueChange = 0` ⇒ 状态**只体现在图上**、不按值改色）。
                    //   ⚠️ 原来这一行恒传 `c.Icon` ⇒ 这扇窗的开关**开/关长得一模一样、只差一点色偏**
                    //   （卡组编辑那扇 2026-10-04 A24 就修了，收藏窗这一份一直开着 —— 就是本条要收的账）。
                    //   判据与实现与 `Deck/DeckRuntime.RefreshFilterCells` 逐条同源。
                    Rect(cell, c.IconOff != null && !c.On ? c.IconOff : c.Icon, b, "Background", QFltRow, CellTint(c), true);
                    if (!string.IsNullOrEmpty(c.Label))
                    {
                        var lr = cosmo ? Abs(c.Lab) : _fltScroll.Shift(Abs(c.Lab));
                        // 🔴 **2026-10-08（A212）**：第 10 个实参 = 这一族原版的 `m_TextWrappingMode`
                        //   （开关/稀有度/类型三族 = 0、费用桶那一族 = 1）—— 见 `TextAligned` 的注释。
                        // 🔴 **2026-10-11（A258）**：直接传 `c.LabelWrap`（原版档位原文），**不再折成 bool**
                        //   —— 布尔表达不了原版第三档 `3`，而这一族将来可能真出现 `3`（同族搜索框就是 3）。
                        TextAligned(cell, c.Label, lr, CellTint(c), "Label", c.LabelPx, c.LabelRight,
                                    c.LabelAutoMin, c.LabelCenter, c.LabelWrap, c.LabelAutoMax, c.LabelBase);
                    }
                    var key = c.Key;
                    // 🔴 **点击回调必须带上"这是哪一份面板"** —— `ApplyFilter` 读的是模块级的 `_flt`，
                    //    而点击发生在**建完之后**（那时 `_flt` 已经还原了）。第一版漏了这一步，
                    //    点一格筛选就 `NullReferenceException`（自检当场抓到）。
                    var owner = _flt;
                    AddHit(cell, "Hit", r, QFltHit, () => Scope(owner, () => ApplyFilter(key)),
                           clickCue: WindowSounds.SelectToggleCue);
                }
            }

            if (!wasActive) panel.gameObject.SetActive(false);

            // 🆕 A11：收尾按**进度**再摆一次 —— ① 显隐（滑出去的整栏要继续关着）
            //   ② **刚建出来的那些 `WindowButton` 默认是 `enabled` 的**，而抽屉可能正收着
            //   ⇒ 命中区必须**强制**重按一次（`force: true`；不加这一下，重建过的列会出现
            //     「画面上滑出去了、真鼠标还点得到」——正是 A9/A26 那类量不到的静默 bug）
            ApplyDrawerSlide(_flt, _flt.Slide, true);
        }

        /// <summary>筛选格的**稳定名字**（`$rar:legendary` → `rar_legendary`）—— 自检按它找格，
        /// **别按序号找**（视口外的格不建 ⇒ 序号会错位）。</summary>
        static string KeyToName(string key)
        {
            return (key ?? "").Replace("$", "").Replace(":", "_");
        }

        /// <summary>格子的着色 = **原版那套 tint**（`checkMark` 30/30 全是 null ⇒ 选中态没有对勾图）。
        /// 🔴 **2026-10-05（A32③）改口径**：关着的色偏**逐行不同**（`FilterPanelModel.OffTint*`），
        /// 开关那一类（`IconOff != null`）**根本不吃 tint**（原版那三颗 `colorTintOnValueChange = 0`）
        /// ⇒ 用 `CellTint(c)`，**别再拿一个共用值去乘**。
        /// 与 `Deck/DeckRuntime.CellTint` **同一条判据**（两扇窗同一个 `Cell` 模型，别各写一套）。
        /// 🔴 **2026-10-16（§三第2条）**：形参随 `FltCell` 一起去掉，现在是**模型自己的**
        /// `FilterPanelModel.Cell` —— 与 `Deck/DeckRuntime.cs` 那一份**逐字同签名**。</summary>
        static Color CellTint(FilterPanelModel.Cell c)
        {
            return c.IconOff != null ? Color.white : FilterPanelModel.ToggleTint(c.On, c.OffTint);
        }

        /// <summary>非筛选的那些 toggle（卡组页那一列阵营格等）—— 它们全是 **Army 族**
        /// （`Deck Filters/Army Filter` 与 `Cosmetic FIlter/Army Filter` 的模板 `offColor` 实测都是
        /// `(0.5,0.5,0.5,1)`）⇒ 用 `OffTintFaction`。⚠️ 这是**逐处实读**的结果，不是「默认值」——
        /// 换个族就要换常量（铁律 5·c）。</summary>
        static Color ToggleTint(bool on)
        {
            // **色表只有一份**（`FilterPanelModel`）—— 本类里非筛选的那些 toggle 也用它，口径一致
            return FilterPanelModel.ToggleTint(on, FilterPanelModel.OffTintFaction);
        }

        /// <summary>摆一段**左/右对齐**的字（`Label.Align*On` 吃世界坐标）。
        /// 🔴 **必须在建好之后再对齐** —— TMP 在空串/未激活时量出的是垃圾边界（`项目任务.md` §三 第 15 条 第 8 项）。
        /// ⚠️ `autoMinPx &gt; 0` 时开**自适应字号**（原版那几处 `auto(10-27)` / `auto(25-45)`）——
        ///    不开的话 `Legendary` 在 100px 的格宽里会**冲出去**（实测 105.9px &gt; 100）。
        /// <para>🔴 **2026-10-08（A212）新增折行档**：`SetAutoFitBox` 内部会 `SetWrapWidth`，而那个
        /// **无条件把 `m_TextWrappingMode` 设成 `Normal`(=1)** ⇒ 「要自适应、但原版**不折行**」的件会被悄悄打开折行。
        /// 判据 = 原版各节点自己的 `m_TextWrappingMode`（逐族实读；**同一个形参不能给所有行一刀切**：
        /// 费用桶那一族是 `1`、开关/稀有度/类型三族是 `0`）⇒ 由调用方把 `Cell.LabelWrap` 传进来。</para>
        /// <para>🔴 **2026-10-11（A258）**：这个形参**没有缺省值了**（原来是 `bool wrap = true`）——
        /// 缺省值不是原版概念（V4a 亲跑 `menu_dump` 量过：原版三族本来就是**混的**），
        /// 「统一缺省」是伪问题 ⇒ **倒逼逐处现读**。今天两个调用点各自的真值都写在调用处。
        /// ⚠️ 档位是**原版 `m_TextWrappingMode` 的原文**（`0`/`1`/`2`/`3`，见 `Label.SetWrappingMode`），
        /// **不再是布尔** —— 原版第三档 `3`（`PreserveWhitespaceNoWrap`，单行输入框那一档）布尔表达不了，
        /// 而搜索框正好是 `3`（A249）。`1` = 什么都不做（= `SetAutoFitBox` 原来那一档）。</para></summary>
        /// <para>🔴 **2026-10-12（A333）新增 `autoMaxPx`**：**原版那一颗的 `m_fontSizeMax`**（画布 px、
        /// 与 `autoMinPx` 同量纲）。**`&lt;= 0` ⇒ 旧行为**（上限 = `fontPx`）。原版的 `m_fontSizeMax`
        /// **不一定等于** `m_fontSize`：本窗实测错处 = 稀有度 / 类型两族（原版 `auto[10~27]`、标称 23.2
        /// —— 旧写法天花板矮 **3.8px**）。逐族实读 → `FilterPanelModel` 那三对常量。</para>
        /// <para>🔴 **2026-10-12（A336④）新增 `basePx`**：**原版那一颗的 `m_fontSizeBase`**（画布 px）。
        /// **`&lt;= 0` ⇒ 旧行为**（base = 调用方那一档）。只影响自适应**二分起点**，渲染差 ≤ 0.05 fontSize 单位。</para>
        void TextAligned(Transform parent, string text, PxRect r, Color col, string name, float fontPx, bool right,
                         float autoMinPx, bool center, int wrapMode, float autoMaxPx = 0f, float basePx = 0f)
        {
            // 🔴 **2026-10-09（A1173）**：autosize 那一段收进 `Text` 的形参（原来是「先 `Text`、再手工
            //   `SetAutoFitBox`」——`autoMinPx > 0` 那道闸现在由**漏斗**判，本处只要把 `wrapPx` 一并带上
            //   （⚠️ **只传 `autoMinPx` 不传 `wrapPx` = 死实参**，见 `MenuWindowBase.Text` 的注释）。
            //   `autoMinPx == 0`（= 这一族不开自适应）时 `wrapPx` 传 0 ⇒ **连 `SetWrapWidth` 都不调**，
            //   与收口前逐位相同（那时整段都被 `if (autoMinPx > 0f)` 挡着）。
            //   ⚠️ 上限 `autoMaxPx <= 0 ⇒ fontPx`（A333）、base `basePx` 都由漏斗按同一口径兜底。
            var lb = Text(parent, text, r.x1, r.x2, r.y1, r.y2, 5, col, name, fontPx,
                          wrapPx: autoMinPx > 0f ? r.W : 0f,
                          autoMinPx: autoMinPx, autoMaxPx: autoMaxPx, autoBasePx: basePx);
            if (lb == null) return;
            lb.SetRenderQueue(QFltText);
            // 🔴 **2026-10-08（A212）**：折行按**原版逐处实读的那一档**显式设 ——
            //    与 `Deck/DeckRuntime.cs` 的 `RefreshFilterCells` / `RefreshCosmoFilters` 里那两行**同一条判据**（那扇窗已经收口过），
            //    漏了这一步就是「碰巧对/碰巧错」（`SetAutoFitBox` 刚无条件开过折行）。
            //    ⚠️ `!= 1` 而不是 `== 0`：`1`(= `Normal`) 正是 `SetAutoFitBox` 刚设好的那一档 ⇒ 不必白重排一次。
            if (wrapMode != 1) lb.SetWrappingMode(wrapMode);
            // 🔴 **2026-10-05（A32④）订正**：这里原来写「两个开关行的标签原版是 hAlign=**Center**（A3 §5·1）
            //    ⇒ `center=true` 时不摆对齐」—— **那半个读数是错的**：全包 8 个 `Owned only`/`Upgradable only`
            //    的 `m_HorizontalAlignment` 实测都是 `1`(Left)，**收藏窗这 5 个也在内**
            //    ⇒ 模型里 `LabelCenter` 已按实读改成 `false`，于是走到下面那句 `AlignLeftOn(r.x1)`。
            //    ⚠️ `center` 这个形参留着（将来若真有一行是居中，改模型那一格即可）。
            if (center) return;
            float x = right ? r.x2 : r.x1;
            float wx = LayoutSpace.FromPixel(x, 0f).x;
            if (right) lb.AlignRightOn(wx); else lb.AlignLeftOn(wx);
        }

        /// <summary>第 ① 行：搜索框（原版 `CardNameFilter` → `Input Field` 281.28×40 @面板内 (27.01,19.51)）。
        /// ⚠️ 坐标同样要**加 `FltL/FltT` 换成页面绝对**（见 `BuildFilterRowModel` 那条踩坑）。
        /// 🔴 **2026-10-11（A248/A249）**：`styles` 选字号那一档（异画页 `35/auto[10~35]`；卡牌页 `30/auto[18~30]`）。</summary>
        void BuildNameRow(Transform parent, bool styles)
        {
            // 三个矩形的位置**来自共用模型**（`Input Field` 281.28×40 居中、顶内缩 19.51）
            PxRect inR, taR, icR;
            FilterPanelModel.NameRowRects(FltW, out inR, out taR, out icR);
            var r = _fltScroll.Shift(Abs(inR));
            if (!_fltScroll.Intersects(r)) return;
            var cell = Node(parent, "Name Filter", r);

            // 底：**九宫格**（原版 `InputFieldBackground` 是 **Unity 内置图** 32×32、`m_Border=(10,10,10,10)`，
            //     藏在 `bundle_Warpforge_unitybuiltinassets` ⇒ 得单独导，见 `工具/import_original_art.py`）
            //     —— 图名/边宽/着色/占位符/字号**一律读共用模型**（别再手写第二份）
            var tex = Art(FilterPanelModel.InputSprite);
            if (tex != null)
            {
                float bd = FilterPanelModel.InputBorder;
                // 🔴 **2026-10-06（A50③）：这一处收口到公共件 `MenuDraw.Nine`** —— 原来直调
                //   `ImageQuad.CreateNineSlice`（= 绕开公共件的那条路，**拿不到 `clip` / `clipSoftness`**）。
                //   与旧代码**逐项等价**（三样都别改）：
                //    ① **矩形 / 落位** = `r`：旧代码那两个实参就是公共件内部要算的**同一句**
                //       `Local(cell, r.x1, r.y1, r.x2, r.y2)`（本类的 `Local` 与 `MenuDraw.Local`
                //       **逐字同源** —— 都是 `LayoutSpace.RectCenter(…) − parent.position`），
                //       尺寸也一字不差（`LayoutSpace.Px(r.W)` / `LayoutSpace.Px(r.H)`）；
                //    ② **队列 = `QFltRow`** · **tint = `FilterPanelModel.InputTint`**（旧代码建完逐块设的就是这两样，
                //       公共件会替我们设；`GetComponentsInChildren<ImageQuad>()` 两边都不含未激活件 ⇒ 同一批子块、同一先后）；
                //    ③ `borderOutPx` 不传 ⇒ 与旧代码同一条 `?? border` 退化 · `fillCenter` 同为默认 `true` ·
                //       **`clip` 一律 `null`**（本批只保证等价，⛔ 不顺手改观感）。
                //   ⇒ 下面那圈 `foreach` **删掉了**：它设的两个值与公共件内部设的**同值**，
                //     留着就是「同一条规则写两处」（将来改一处、另一处静默不动）。
                MenuDraw.Nine(cell, tex, r, new Vector4(bd, bd, bd, bd), 32f, 32f,
                              QFltRow, FilterPanelModel.InputTint, true, "Input BG");
            }

            // 字：`Text Area` [37.4,182.4,231.3,27]（绝对）→ 面板内 (37.15, 26.5)~(268.45, 53.5)；
            //   空时那句是**占位符**：🔴 **2026-10-18 起走词条**（`Core/FilterPanelModel.InputPlaceholder` →
            //   `Loc.T("MenuDeck/HUD/SearchFilter")`，中文档印「搜索」）—— 本行原写「占位符 "Search"」，
            //   那是**词条化之前**的字面量（第三轮整改 · 审查 P8 就地更新）。
            //     🔴 **左对齐 + `auto(18–30)`**（原版 `Placeholder/Text` 的两个属性；收藏窗这条一直是对的）
            string cur = FltState.Filter.Name;
            bool editing = PointerLayer.Instance != null && PointerLayer.Instance.TextEditing;
            string txt = editing ? (PointerLayer.Instance.TextBuffer + "_")
                                 : (string.IsNullOrEmpty(cur) ? FilterPanelModel.InputPlaceholder : cur);
            var tr = _fltScroll.Shift(Abs(taR));
            // 🔴 **2026-10-11（A248）**：字号那一档**按页取**（异画页 `35 · auto[10~35]`，卡牌页 `30 · auto[18~30]`
            //   —— 两页**矩形逐条相同、只有字号不同**）。⛔ 别改共用的那两对常量。
            // 🔴 **2026-10-11（A249）**：**折行那一档 = 原版 `3`**（`PreserveWhitespaceNoWrap`）。
            //   判据 = 原版 `Name FIlter/…/Text` 的 `m_TextWrappingMode`：**两页都是 3**
            //   （`Placeholder` 那半是 0；我们这一颗 `Label` **兼作**两者 ⇒ 取 `Text` 那一档 3 ——
            //    同 `FilterPanelModel.DeckEditInputWrap` 那条先例：`3` 多保住的「空白保留」正是**输入文本**要的那一半）。
            //   ⚠️ 我们原来是 1：`TextAligned` 走 `SetAutoFitBox`，而它**无条件**把模式设成 `Normal`(=1)
            //   ⇒ 不过这一句就永远是「碰巧对/碰巧错」。
            TextAligned(cell, txt, tr, PageInk, "Input Text",
                        styles ? FilterPanelModel.InputFontPxStyles : FilterPanelModel.InputFontPx,
                        false,
                        styles ? FilterPanelModel.InputFontAutoMinStyles : FilterPanelModel.InputFontAutoMin,
                        false, SearchBoxWrap,
                        // 🆕 2026-10-12（A333 + A336④）：上限 = 原版 `m_fontSizeMax`（两页都**恰好等于标称**：
                        //   卡牌页 `30` / 异画页 `35`）；base = 原版 `m_fontSizeBase` = **26**（两页同值）。
                        //   ⛔ 别把 base 删掉 —— 26 ≠ 30/35、也 ≠ TMP 出厂默认 36，是原版显式设过的值。
                        styles ? FilterPanelModel.InputFontAutoMaxStyles : FilterPanelModel.InputFontAutoMax,
                        styles ? FilterPanelModel.InputFontBaseStyles : FilterPanelModel.InputFontBase);

            // 尾图标 `40k_icon_search` 35×30（面板内 268.35,24.5 → 303.35,54.5）
            //     🔴 **单独一档**：它与输入框底图**故意重叠**，同队列时谁盖谁不定（2026-09-28 在卡组编辑那扇实测到）
            var ir = _fltScroll.Shift(Abs(icR));
            Rect(cell, FilterPanelModel.SearchIconSprite, ir, "Search Icon", QFltIconTop, null, true);

            // ⚠️ 同 `RebuildFilterRowsNow` 那一条：点击回调要**带上这一份面板**，别用 `ApplyCardFilter`
            //    （那个写死了作用在 Cards 页那份上）
            var owner = _flt;
            AddHit(cell, "Hit", r, QFltHit, () => Scope(owner, () => ApplyFilter("$name")),
                   clickCue: WindowSounds.SelectToggleCue);
        }

        /// <summary>后 6 行的格子表 —— **模型在 `Core/FilterPanelModel.cs`（与卡组编辑共用一份）**，
        /// 本函数只负责**把模型填进 `_fltCells`**（坐标 = 面板内，**未**加面板原点）。
        /// 🔴 2026-09-23 踩过：最初模型里一半加了 `FltT` 一半没加，而 `RebuildFilterRows` 是**按绝对坐标摆**的
        /// ⇒ **整排偏上 155.9px**，搜索框干脆落到视口外**根本没建**（8 条断言把它抓出来）。
        /// 🔴 **2026-10-16（§三第2条）**：那一跳现在**只发生在画的时候**（`RebuildFilterRowsNow` 里对
        /// `c.R` / `c.Bg` / `c.Lab` 各过一次 `Abs`）—— ⛔ 换了地方不等于可以漏，见 `Abs` 的注释。</summary>
        void BuildFilterRowModel(bool styles)
        {
            // 🔴 **模型只有一份** —— 七行的行顶 / 格尺寸 / 选项表全在 `Core/FilterPanelModel.cs`
            //    （卡组编辑那扇窗走的是同一个函数）。本函数**不再做坐标换算**（那条已挪到画的那一处）。
            // 🔴 2026-09-23 踩过：最初模型里一半加了 `FltT` 一半没加 ⇒ **整排偏上 155.9px**，
            //    搜索框干脆落到视口外**根本没建**（8 条断言把它抓出来）。
            // 🔴 **2026-10-11（A248）**：`styles` 只选**两个开关标签的字号那一档**（异画页 36/auto[10~36]，
            //   卡牌页 32/auto[18~32]）；稀有度 / 费用 / 类型那三族**两页相同** ⇒ 走模型的共用常量。
            // 🔴 **2026-10-16（§三第2条）**：模型**直接写进 `_fltCells`**（`FilterPanel.Cells`）——
            //    原来这里是「先收进一个临时 `src`、再逐字段对拷成 `FltCell`」那一段，**已删**
            //    （`FltCell` 整个结构都没了，见文件上方那条）。⇒ 格子那三个 rect 的换算只剩
            //    **画的时候那三处** `Abs(...)`（`BuildNameRow` 那三处本来就在画的时候）。
            //    ⚠️ 调用方负责先 `_fltCells.Clear()`（`RebuildFilterRowsNow` 开头那一步），
            //    模型这两个函数只 `Add`、**不自己清**（与 `DeckRuntime` 那条路一致）。
            FilterPanelModel.Build(FltState, FltW, _fltCells,
                styles ? FilterPanelModel.ToggleFontPxStyles : FilterPanelModel.ToggleFontPx,
                styles ? FilterPanelModel.ToggleFontAutoMinStyles : FilterPanelModel.ToggleFontAutoMin,
                // 🆕 2026-10-12（A333 + A336④）：上限那一档**按页分**（异画页原版 `auto[10~36]`、
                //   卡牌页 `auto[18~32]`）—— 两页的上限都**恰好等于各自的标称**；
                //   base 那一档**三页同值 32**（原版显式设过）⇒ 走缺省即对，这里不另传。
                styles ? FilterPanelModel.ToggleFontAutoMaxStyles : FilterPanelModel.ToggleFontAutoMax);
        }

        /// <summary>🆕 2026-10-03（A11）**卡背页那两行**的格子表 —— 模型在 `FilterPanelModel.BuildCosmetics`
        /// （与卡组编辑那扇窗**同一份**：13 个阵营格 + 1 个 `Owned` 开关，**行序 Army 在前**）。
        /// 同 `BuildFilterRowModel`：本函数只把模型填进 `_fltCells`（坐标 = 面板内，
        /// 「面板内 → 页面绝对」那一跳在画的时候由 `Abs` 兑现）。</summary>
        void BuildCosmoRowModel()
        {
            // 🔴 **2026-10-16（§三第2条）**：同 `BuildFilterRowModel` —— 模型直接写进 `_fltCells`
            //    （调用方已 `Clear`），那份 `src` + 逐字段对拷已删。
            FilterPanelModel.BuildCosmetics(CosmoState.Factions(), CosmoState.Filter, FltW, _fltCells);
        }

        /// <summary>面板内坐标 → 页面绝对坐标（**换算函数只此一处**；调用点 = `RebuildFilterRowsNow`
        /// 里对 `c.R` / `c.Bg` / `c.Lab` 那三处 `<c>Abs(...)</c>` + `BuildNameRow` 那三处，见上面那段踩坑）。</summary>
        PxRect Abs(PxRect r) { return new PxRect(FltL + r.x1, FltT + r.y1, FltL + r.x2, FltT + r.y2); }

        /// <summary>四行的小标题（原版 `Title` TMP，**fs32 · hAlign=Left/Middle**）。
        /// ⚠️ 2026-09-23 实拍补的：第一版**漏了这四个**（只建了格子），断言一条都没报 —— 因为它们不是「位置不对」
        /// 而是**根本不在**，而当时没有「标题在不在」的断言（已补在 `CollectionScene`）。
        /// rect 出处：`menu_rect.py … -5393211807834578219` 等（Rarity/Cost/Type 的 Title 从 x=25 起，Army 从 0 起）。
        /// 🔴 **2026-10-05 改对齐**：原来那句写的是 `hAlign=Center` —— **读错了**，原版是 `Left/Middle`
        /// （判据 = `FilterPanelModel.TitleFontPx` 那段，两扇窗逐行实读）⇒ 现在按 `Title.Left` 走。</summary>
        void BuildFilterTitles(Transform parent, bool styles)
        {
            // 四行小标题的位置**也只有一份**（`FilterPanelModel.BuildTitles`，与卡组编辑共用）
            // 🔴 **2026-10-11（A248）**：`styles` 只选**字号那一档**（异画页 36，卡牌页 32）；
            //   四行的 rect **两页逐条相同**（见 `FilterPanelModel` 那段现读）。⛔ 别改共用常量。
            var titles = new List<FilterPanelModel.Title>();
            FilterPanelModel.BuildTitles(FltState, FltW, titles,
                styles ? FilterPanelModel.TitleFontPxStyles : FilterPanelModel.TitleFontPx);
            // 🔴 字号**从这里传下去**（`tl.Px`）—— 此前那一处写死 `32f`，模型里的 `Px` **一个读者都没有**
            //   ⇒ 异画页那 36 传了也不会生效（「看着有依据」的那一类）。
            foreach (var tl in titles) TitleRow(parent, tl.Text, tl.R.x1, tl.R.y1, tl.R.H, tl.Left, tl.Px);
        }

        void TitleRow(Transform parent, string text, float x, float y, float h, bool left, float fontPx)
        {
            var r = _fltScroll.Shift(new PxRect(FltL + x, FltT + y, FltL + FltW, FltT + y + h));
            if (!_fltScroll.Intersects(r)) return;
            // 🔴 **2026-10-05：原版这四行是 `Left/Middle`，不是 Center**（判据见 `FilterPanelModel.TitleFontPx`）
            //   ⇒ 先照旧用 `Text()`（它摆的是矩形中心），**再**把它挪到矩形左沿。顺序不能反：
            //   `Text()` 里的 `MenuDraw.ClipText` 是「逐字夹顶点」的，而 `AlignLeftOn` → `RefreshBounds`
            //   → `ForceMeshUpdate` 会**重排 mesh**（`MenuDraw` 头部那条纪律：「**先建 → 再 Align* → 最后 ClipText**」）
            //   ⇒ 挪完必须**再裁一次**，否则这一行若正压在视口边上，裁的那一刀停在旧位置上。
            // 🔴 **2026-10-18（A891 的续 · 续做 A）：只换【显示文案】那一个实参** ——
            //   `FilterPanelModel.TitleText(text)`（= `Loc.T(键)`，键见那边的 `TitleTerm`）。
            //   ⛔ **节点名那半句 `"Title " + text` 一个字没动**：`Editor/CollectionScene.cs` 的 `TitleLeftPx`
            //   （`:392`）与两条 `FindChild(fltPanel, "Title " + ttl)` 都**按名找**（名里还是英文原名）。
            //   ⚠️ `Energy Cost` 那一行**原版没有 `Localize`** ⇒ `TitleText` 原样返回英文。
            var lb = Text(parent, FilterPanelModel.TitleText(text), r.x1, r.x2, r.y1, r.y2, 5, PageInk, "Title " + text, fontPx);
            if (lb == null) return;
            lb.SetRenderQueue(QFltText);
            if (!left) return;
            MenuDraw.AlignLeft(lb, r);
            // 🔴 **2026-10-13（A435 阶段 2 · 乙 · B2）**：这一行的守卫原来是 `if (Clip.HasValue)`
            //   （吃**本窗**那个字段）—— 迁移后本窗 `Clip` **恒为 `null`** ⇒ 那个守卫会让这一句
            //   **永远不跑**（静默：压在视口边上的标题行会停在 `AlignLeft` 之前那一刀的位置上）。
            //   ⇒ 改成先走共用解析（第二参数仍传本窗 `Clip` = 旧路那一份，非空时照旧赢）。
            //   ⛔ **`ClipText` 的 `clip` 形参必须传 `null`**（别把解析结果 `st.RenderClip` 喂进去）：
            //   那会让 `ClippedTextGuard` 存一份**解析后的快照**、并在每次重裁时
            //   给 `NodeShadowedByParam` +1（A484 已经把 `ClipText` 自己那一处修好了 ——
            //   传 `null` 让它**每次重裁都重新解析**，节点挪了跟得上、也不污染那个探测器）。
            var st = ViewportClip.Resolve(parent, Clip, ClipSoftness, default(Vector4));
            if (st.RenderClip.HasValue) MenuDraw.ClipText(lb, null, st.Softness);
        }

        // ⚠️ 原来这里抄了一整套选项表（Rarity 5 / Cost 8 / Type 3）——
        //    2026-09-28 搬进 `Core/FilterPanelModel.cs`（卡组编辑的同一个抽屉复用同一份）。

        // ============================================================ 页头（A2 §三·4）
        public const float CreateX = 1661f, ImportX = 1391f, HdrBtnY = 80.9f, HdrBtnW = 245f, HdrBtnH = 60f;

        // ---- 🆕 A98（2026-10-06）：`Control Buttons` 组节点 + `Unlock` 占位 ----
        /// <summary>`Control Buttons` 组节点的矩形（原版 **1180.00,80.94 → 1920.01,140.94** = 740.01×60）。
        /// 判据 = 直读 `bundle_menus_assets_all/RectTransform/RectTransform_-1134804646651895083.json`
        /// （`m_AnchorMin/Max` = (1,0)/(1,1) · `m_Pivot` = (1,.5) · `m_AnchoredPosition` = (0,2.5) ·
        /// `m_SizeDelta` = (740.01,−25)）。⚠️ 工具（`menu_dump.py`，建模 CSF 写回）读出来是 **1179.99→1920.01**，
        /// 那 0.01 是浮点尾差 —— 手算 = **1180.00→1920.01**，取手算。
        /// ⚠️ 组矩形的 y 用原版的 **80.94**，而两颗钮沿用的 `HdrBtnY = 80.9` 是既有常量（差 0.04px、不可见）
        /// —— A98 **不动**既有常量。</summary>
        public const float CtrlGrpL = 1180f, CtrlGrpT = 80.94f, CtrlGrpR = 1920.01f, CtrlGrpB = 140.94f;

        /// <summary>`Unlock` 的宽（原版 **186.01**，高 60 与另两颗同；组内**最左**那一格）。
        /// 🔴 它虽然画不出东西，**但占着布局位** —— `Import` 落在 1391.01 的原因就是它
        /// （1180.00 + 186.01 + spacing 25 = 1391.01）。</summary>
        public const float UnlockW = 186.01f;

        /// <summary>`Shared/Close Button`（文字是 **"Back"**）—— **2026-10-03 A22② 补**。
        /// 原版实测 `192.2,83.4 → 342.2,143.4`（150×60），**在 `Tab Buttons`（y 158.6 起）之上**、不压左栏。</summary>
        public const float CloseBtnL = 192.2f, CloseBtnT = 83.4f, CloseBtnR = 342.2f, CloseBtnB = 143.4f;

        /// <summary>🆕 **2026-10-09（A265）**：`Shared/Close Button/**Button Text**` 自己的矩形
        /// —— **不是**整颗钮那个 `CloseBtn*`（两颗差 0.25px 的中心，字看不出来；**差的是框宽**）。
        /// <para>原版实测（`python 工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 12`
        /// 第 255 行）：`Button Text  200.5, 89.3 → 333.4, 137.5` = **132.86×48.24**
        /// （同一行还写着 `'Back' 字号=40.0 auto[10.0~40.0] 对齐=Center/Capline 折行=0`）。</para>
        /// <para>🔴 **「132.86×48.24 是算出来的还是序列化的」已查实 = 序列化的**（这是 A265 立项时
        /// 「未查清的一半」）：那颗节点上**确实挂着** `AspectRatioFitter`（`m_AspectMode=1`
        /// 宽控高 · `m_AspectRatio=3.8386404514312744`），但它的 **`m_Enabled=0`**
        /// ⇒ `AspectRatioFitter.UpdateRect` 头一句 `if (!IsActive()) return;` 直接返回、**一个字段都不写**
        /// （判据 → `工具/menu_dump.py` 的 `_ar_note`，它也是因此**没在那一行印 `⚙ARF` 标记**）。
        /// 真值完全由 RectTransform 自己的字段解释：`m_AnchorMin=(0.035511188, 0.099)` ·
        /// `m_AnchorMax=(0.961261868, 0.903)` · `m_SizeDelta=(-6.000002, 0)` · `m_AnchoredPosition=(0,0)` ·
        /// `m_Pivot=(0.5,0.5)`，父 = 150×60 ⇒ 宽 `0.92575068×150 − 6 = **132.86**`、
        /// 高 `0.804×60 + 0 = **48.24**`（两个轴都逐位对上 dump 的 132.86/48.24）。
        /// 复现命令：`python "C:/Users/qjh36/AppData/Local/Temp/wf_wb_probe_close.py"`
        /// （probe 用 `工具/menu_rect.py` 的 `Bundle` 直读 `RectTransform/*.json` 与 `MonoBehaviour/*.json`）。</para>
        /// <para>⚠️ ARF 若那天被打开，给出的高会是 `132.86 ÷ 3.83864 = 34.61` —— **不是 48.24**
        /// ⇒ 这也是「它没在跑」的旁证。**照原版字面量建**（A265 说的最保守那条）。</para></summary>
        public const float BackTxtL = 200.5f, BackTxtT = 89.3f, BackTxtR = 333.4f, BackTxtB = 137.5f;

        public const float FltBtnX = 367.2f, FltBtnY = 88.5f, FltBtnS = 50f;
        /// <summary>🔴 **2026-09-23 更正**：原来这里写 **1218.6**（注释说是「按容器内右对齐实算」）—— **那是错的**。
        /// 真值 = **612.2**（`资料/普查产出_0923/A2_Deck页.md:161` **实测落点**：=「Filters」文字条右缘 **587.2 + 25**）。
        /// **错因**：`Header/Filters` 是个 `GridLayoutGroup`（cell 85×70 / count 1 / MiddleLeft）+ `CSF(H=Preferred)`，
        /// 而唯一子件 `Clear Filter Button` 带 **`LayoutElement.m_IgnoreLayout = 1`** ⇒ **布局根本不排它**、
        /// 容器**宽度算出来是 0** ⇒ 按钮按自己的锚点 `(1,.5) pos(20,0)` 落在**容器右缘 592.2 + 20 = 612.2**。
        /// **代价**：照 1218.6 摆 ⇒ 它和右边的 `Import`（1391..1636）**压掉 77.6px**，实拍一眼可见
        /// （`_tmp_view/collection/06_收藏_ImportDeck弹窗.png`）。</summary>
        public const float ClearFltX = 612.2f, ClearFltY = 83.5f, ClearFltW = 250f, ClearFltH = 60f;

        public override void Open()
        {
            // 🆕 **2026-10-12（`A1628`）计时探针** —— 真机实测「点收藏 ⇒ 整个 player 假死约 50 秒」
            //   （点完 `Get-Process` 读到 `Responding: False`、工作集 1.8 GB，约 50 s 后回 `True`(0.9 GB)；
            //    期间三张截图逐像素一致、**一条日志都没有**）。判据与已排除项 → `BuildWatch` 的注释。
            //   ⚠️ **它在真 Play 里才出声**（`Application.isPlaying`）⇒ 批处理自检的日志一字不变。
            BuildWatch.Begin();
            Build();
            BuildWatch.Mark("① `Build()`（建壳 + 四页 + 收尾刷）");
            if (tabButtons != null) tabButtons.Click(0);       // 默认落在第一页（`GetStartingTab` 的回落）
            BuildWatch.Mark("② `tabButtons.Click(0)`（落第一页）");
            if (Application.isPlaying) StartCoroutine(BuildWatch.ReportAtFrameEnd("开收藏窗"));
        }

        // ============================================================ 四页共用的那一条页头
        //
        // 🔴 **2026-09-24 抽出来（铁律「两处写同一条规则 = 迟早不一致」）**：Cards / Cosmetics / Styles
        //    三页的页头在**原版里是同一行** —— 实读（从 `Collection Menu Variant` 根一路走下来）：
        //    🔴 **2026-10-05 订正（A93①）：`Deck` 页也是这一行**（原来写「三页」，那是我漏看 ——
        //      `Select Deck Tab>Header` 底下**同样有** `Filter Toggle 367.2,88.4 50×50 40k_menu_bt`
        //      + `label 'Filters' fs42` + `icon detail` + `Separator Line` + `Clear Filter Button`，
        //      只是它多一条 `Control Buttons`）⇒ 现在是**四页共用**、`BuildDeckHeader` 也走它。
        //      · `Filter Toggle`  `40k_menu_bt`          **367.17, 88.44 → 417.17, 138.44**（50×50）
        //      · ├ `icon detail` `40k_bt_icon_search`    377.17, 98.44 → 407.17, 128.44（SD −20 ⇒ 30×30）
        //      · ├ `label`       "Filters"              437.17, 88.44 → 587.17, 138.44
        //      · `Separator Line` `40k_main_line`        167.17,150.94 → 1920.01,160.94
        //      · `Filters`（GridLayoutGroup）→ `Clear Filter Button` `UI_Button_Mulligan` **250×60**
        //        ⇒ 真值 **612.17, 83.44 → 862.17, 143.44**（**四页都是这个 x**，理由见下）
        //    ⚠️ **字号逐页不同**（原版每页是自己的实例值）：**Deck 42/42 · Cards 42/42 · Cosmetics 35/33 · Styles 42/42**
        //       ⇒ **做成参数**，不许当成全局常量（`阶段二_卡组线_原版规格.md` §三 那句「别拿一个数当全部情况」）。
        //       🔴 **2026-10-12 就地订正（铁律 5）**：原来这一行写的是 **「Deck 42/33」** —— **错**（串了页）。
        //      判据 = 四页逐颗现读 `… ▸ {Header,Header Filters} ▸ {Filter Toggle/label, Clear Filter Button/Button Text}`
        //      的 `m_fontSize`：Deck `42 / 42` · Cards `42 / 42` · Cosmetics `35 / 33` · Styles `42 / 42`
        //      （那一行 `35/33` 是**卡背页**的，被误抄到本页头上）。两处消费点（`filterPx` / `clearPx`）都已订正。
        //
        // 🔴 `Clear filters` 的 x 为什么是 612.17 而不是 Cardback 页序列化里的 **1488.59**：
        //    `Header/Filters` 是 `GridLayoutGroup`(cell 85×70 · FixedRowCount 1 · MiddleLeft) + `CSF(H=Preferred)`，
        //    而唯一子件 `Clear Filter Button` 带 **`LayoutElement.m_IgnoreLayout = 1`** ⇒ **布局根本不排它**，
        //    容器宽度**算出来是 0** ⇒ 按钮按自己的锚 `(1,.5) pos=(20,0)` 落回**容器左缘 592.17 + 20**。
        //    Deck 页的容器宽实测 **−0.00**、Styles 页 **−0.17**、Cards 页同族 —— 都是 0；
        //    Cardback 页序列化里那个 **876.42** 是**布局跑之前的模板值**（同 2026-09-23 第 40 条那次错法）。
        //    判据：照 1488.59 摆，它会和**右对齐到 1821 的标题**压在一起 —— 原版不会这样。

        /// <summary>页头那条分隔线（`Header/Separator Line`，四页同值）。</summary>
        public const float HdrSepT = 150.94f, HdrSepB = 160.94f;

        /// <summary>🆕 **2026-10-17（`A1642`）**：页头**容器** `Header` 的原版矩形 =
        /// **167.2, 70.9 → 1920.0, 155.9**（1752.83 × 85.00）。
        /// <para>判据（`python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Select Deck Tab" --depth 4` **现读**）：
        /// `Select Deck Tab` 的三个孩子依序 = `Decks Tab` · `Select Game Mode Deck Tab`（本窗不建）· **`Header`**，
        /// 而 `Header` 自己的矩形就是这四个数；它与父件**同左右沿**、只占顶部 85px。</para>
        /// <para>⚠️ 它的 `Separator Line`（150.94→160.94）**跨出了容器下沿 155.9** —— 原版如此
        /// （dump 里那颗也是 150.9→160.9）⇒ 别把它当作「容器裁切」的证据去改。</para>
        /// <para>🔴 **2026-10-17（第四十八会话 · `N52②`）就地订正（铁律 5）**：原文写「另外三页的页头节点名
        /// 不叫 `Header`，判据未逐页现读」—— **判据其实早已在 `R5 §2 #7`**，四页逐页是：
        /// `Select Deck Tab ▸ Header` · `CardsTab ▸ Header Filters` · `Cardback Tab ▸ Header` ·
        /// `Alternate Art Tab ▸ Header Filters`（rect 四页同值）⇒ **本函数按页名建容器**。</para></summary>
        public const float HdrL = 167.2f, HdrT = 70.9f, HdrR = 1920.0f, HdrB = 155.9f;

        /// <summary>建一条**共用页头**：`Filters` 圆钮 + `icon detail` + `label` + 分隔线 + `Clear filters`。
        /// <paramref name="filterPx"/> / <paramref name="clearPx"/> = **本页自己的**字号（原版逐页不同）。
        /// <paramref name="pageNo"/> = **窗口页号**（0=Deck · 1=Cards · 2=Cosmetics · 3=Styles）——
        /// 存下那颗圆钮的 quad，好让开合时按状态换图（见 <see cref="_hdrFltBtn"/>）。
        /// 🔴 **Deck 页也走这一条**（2026-10-05 订正）：原版 `Select Deck Tab` 底下**同样有**
        /// `Header/Filter Toggle`（`menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 12`
        /// 印出来的 `4 Header 167.2,70.9→1920,155.9` → `5 Filter Toggle 367.2,88.4 50×50 40k_menu_bt`
        /// + `label 'Filters' fs42` + `icon detail 40k_bt_icon_search 30×30`，**与另外三页同值**）——
        /// 我们原来给 Deck 页单独写的 `BuildDeckHeader` **连那颗 `40k_menu_bt` 都没建**（只铺了 50×50 的图标）。
        /// 四页的差别只有：① 字号（Deck/Cards/Styles 各 **42/42**、卡背页 **35/33**）② Deck 多一条 `Control Buttons`（见 `BuildDeckHeader`）。</summary>
        public void BuildFilterHeader(Transform page, int pageNo, float filterPx, float clearPx, System.Action onClear,
                                      System.Action onToggle, Transform headerOverride = null)
        {
            // 🔴 **2026-10-17（`N52②`）**：按原版建**页头容器**（逐页名不同；rect 四页同值 167.2,70.9→1920,155.9）
            //   + `Filter Toggle` 容器 + `Filters`（GridLayoutGroup 容器）。
            //   孩子仍用**绝对设计 px** 落位（`Local(parent, …)` 会按父链换算）⇒ 视觉不变。
            //   ⚠️ 页内会多出一颗叫 `Filters` 的容器（与 Deck 页的 `Deck Filters ▸ Filters` 同名）——
            //   现读 `Editor/CollectionScene.cs` 里按 `"Filters"` 取的那一处只在 `PracticeModePopup` 子树
            //   （不是收藏窗）⇒ **不歧义**；以后要取就用**路径取法**（`FindChild(header, "Filters")`）。
            string headerName = (pageNo == 1 || pageNo == 3) ? "Header Filters" : "Header";
            // Deck 页的 `Header` 由 `BuildDeckHeader` 先建（它还要往里放 `Control Buttons`）⇒ 复用，⛔ 别再嵌一层。
            var header = headerOverride != null ? headerOverride : Node(page, headerName, new PxRect(HdrL, HdrT, HdrR, HdrB));
            var filterToggle = Node(header, "Filter Toggle",
                                    new PxRect(FltBtnX, FltBtnY, FltBtnX + FltBtnS, FltBtnY + FltBtnS));

            var btnQ = Rect(filterToggle, "40k_menu_bt", new PxRect(FltBtnX, FltBtnY, FltBtnX + FltBtnS, FltBtnY + FltBtnS),
                 "Filters Button", QPagePanel);
            if (pageNo >= 0 && pageNo < _hdrFltBtn.Length) _hdrFltBtn[pageNo] = btnQ;
            Rect(filterToggle, "40k_bt_icon_search", new PxRect(FltBtnX + 10f, FltBtnY + 10f, FltBtnX + 40f, FltBtnY + 40f),
                 "Filters Icon", QPageRow, null, true);
            // 🔴 **2026-10-18（A891 的续 · 续做 B）：字走 `Loc.T`** —— 键 = 原版那颗 `Label` 的
            //   `Localize.mTerm` 原文 **`MenuDeck/Filters/Filters`**（与卡组编辑窗页头那颗**同一条键**，
            //   表里早就有；`Deck/DeckRuntime.cs` 的 `hdr_fltlbl` 一直在用）。
            //   🔴 本批之前这里是**写死 `"Filters"`** ⇒ 中文档下卡组编辑窗印「过滤器」、**收藏窗四页页头却印英文**
            //   （同一条键两种表现）。英文列 = 那颗 TMP 的 `m_text` 原文 `Filters`；中文列 = 「过滤器」
            //   （⚠️ 那一列是**原版实拍**读到的，见 `Core/Loc.cs` 里 `MenuDeck/Filters/Filters` 那条的注释）。
            //   ⛔ 节点名 `"Filters Label"`（`Editor/CollectionScene.cs` 按名找）与上面那颗底图/图标的名字都不动。
            // `label` 自己的矩形（= 原版 `Filter Toggle/label` 那一格）；`AlignLeft` 要用它。
            var fltLabR = new PxRect(437.2f, FltBtnY, 587.2f, FltBtnY + FltBtnS);
            var fltLab = Text(filterToggle, Loc.T("MenuDeck/Filters/Filters"), fltLabR.x1, fltLabR.x2, fltLabR.y1, fltLabR.y2,
                              5, PageInk,
                              "Filters Label", filterPx,
                              // 🔴 **2026-10-10（A1212 · 块 3 #7）**：接上 `m_enableAutoSizing`（四格 = **10 / 42 / 32 / 0**）。
                              //   判据 = `R5` §2 #7（三页三颗同名：`CardsTab ▸ Header Filters` / `Cardback Tab ▸ Header` /
                              //   `Alternate Art Tab ▸ Header Filters`，`Filter Toggle ▸ label`）—— **本处一个调用点
                              //   撑三页**，而 `filterPx` 逐页不同（42/35/42），⚠️ 闸要求 `fontPx > autoMinPx 10`：
                              //   最小的那一页是 **35**，四页全过。
                              //   ⚠️ 折行 = `0` ⇒ 漏斗内部把模式开成 `Normal(1)` 之后要**显式还原**（见下面那句）。
                              wrapPx: 150f, autoMinPx: 10f, autoMaxPx: 42f, autoBasePx: 32f);
            if (fltLab != null)
            {
                fltLab.SetRenderQueue(QPageText);
                fltLab.SetWrappingMode(0);      // 原版 `m_TextWrappingMode = 0`
                // 🔴 **2026-10-12（本会话 · 现读 prefab + 用户实拍）新增：`label` 是【左对齐】，我们原来居中。**
                //   判据①（原版 prefab，四页逐颗现读；`menu_dump.py bundle_menus_assets_all "<页节点>" --depth 4`）：
                //     `Select Deck Tab ▸ Header ▸ Filter Toggle ▸ label` = `'Filters' 字号=42.0 基准=32.0
                //      auto[10.0~42.0] **对齐=Left/Middle** 折行=0 色=(1,1,1,1)`；`CardsTab ▸ Header Filters` ·
                //     `Cardback Tab ▸ Header` · `Alternate Art Tab ▸ Header Filters` 三处**同值**（**4/4 全是 Left**）
                //     ⇒ 四页共用本函数，一处改到位。
                //   判据②（用户实拍 `C:/Users/qjh36/Desktop/warpforge参考图/收藏—卡组的参考图.png`）：
                //     实拍里「过滤器」三个字的**左缘 = 443.5**（把实拍按卡框宽 221.4 归一后换算成画布 px），
                //     而框左沿 = 437.2 ⇒ 那是**左对齐**（CJK 在 fs42 下左留白 ~6px）；
                //     我们居中那一版左缘落在 **462.3**（= 框心 512.2 − 文字宽一半）—— 差 **19px**，实拍上一眼可见。
                //   ⚠️ 顺序与 `TitleRow` 那条**同一条纪律**：`Text()` → `SetRenderQueue` → `AlignLeft`
                //     （`AlignLeftOn` → `RefreshBounds` → `ForceMeshUpdate` 会重排 mesh ⇒ 必须排在 `Text()` 之后；
                //      本处没有 `ClipText`，左栏不在滚动视口里）。⛔ 别挪到 `Text()` 之前（那样会被重排冲掉）。
                MenuDraw.AlignLeft(fltLab, fltLabR);
            }
            if (onToggle != null)
                AddHit(filterToggle, "FiltersHit", new PxRect(FltBtnX, FltBtnY, FltBtnX + FltBtnS + 220f, FltBtnY + FltBtnS),
                       QPageRow, onToggle);

            Rect(header, "40k_main_line", new PxRect(167.17f, HdrSepT, 1920.01f, HdrSepB), "Separator Line", QPageRow);

            // `Header ▸ Filters`（原版 `GridLayoutGroup`，cell 85×70 · count 1 · MiddleLeft · CSF(H=Preferred)；
            // 唯一子件 `Clear Filter Button` 带 `LayoutElement.m_IgnoreLayout = 1` ⇒ 容器宽算出来 **0**、
            // 钮按自己的锚落在「容器右缘 592.2 + 20 = 612.2」）。⇒ 容器按**退化矩形**建，纯结构。
            var filtersGrp = Node(header, "Filters",
                                  new PxRect(ClearFltX - 20f, ClearFltY, ClearFltX - 20f, ClearFltY + ClearFltH));
            var clrQ2 = Rect(filtersGrp, "UI_Button_Mulligan", new PxRect(ClearFltX, ClearFltY, ClearFltX + ClearFltW, ClearFltY + ClearFltH),
                 "Clear filters", QPageRow);
            // 🔴 **2026-10-18（A891 的续）：字走 `Loc.T`** —— 词条键 = 原版 prefab 上那颗
            //   `Localize.mTerm` 的原文 `MenuDeck/Filters/ClearFilters`（5 颗同键，节点名一律 `Button Text`；
            //   本窗这一棵 = `Collection Menu Variant > … > {CardsTab 的 Header Filters / Cardback Tab 的
            //   Header / Alternate Art Tab 的 Header Filters / Select Deck Tab 的 Header}/Clear Filter Button/Button Text`）。
            //   英文列 = 那颗 TMP 的 `m_text` 原文 `Clear filters`；中文列 = 「清除过滤器」（2026-10-14 官方 zh-CN 表；原「清除筛选」= `zh_CN.csv:9`）。
            //   ⛔ **上一个实参（节点名 `"Clear filters"`）与 `"Clear filters Text"` 都不动**
            //      （`Editor/CollectionScene` 里按名找它的那两处 —— 底图 `FindChild(…, "Clear filters")`、字 `…"Clear filters Text"`）。
            var clrLab = Text(filtersGrp, Loc.T("MenuDeck/Filters/ClearFilters"),
                              ClearFltX, ClearFltX + ClearFltW, ClearFltY, ClearFltY + ClearFltH,
                              5, PageInk, "Clear filters Text", clearPx,
                              // 🔴 **2026-10-10（A1212 · 块 3 #8）**：接上 `m_enableAutoSizing`（四格 = **10 / 42 / 12 / 0**）。
                              //   判据 = `R5` §2 #8（三页三颗同名：`… ▸ Clear Filter Button ▸ Button Text`，
                              //   原版 CardsTab 那颗 fs42）。`clearPx` 逐页 = **42/42/33/42**（Deck/Cards/Cosmetics/Styles；
                              //   🔴 2026-10-12 订正：原来写 42/33/42/33 —— 那两组数串了页，见 `BuildDeckHeader` 那条）
                              //   ⇒ 最小 33 > 10，闸全过。
                              //   ⚠️ 折行 = `0` ⇒ 显式还原（下面那句）。
                              // 🔴 **2026-10-17（`A1226` 表 B #8）：框 = 原版有效框 221.44（原来整颗钮 250）。**
                              //   判据 = 原版 `…/Clear Filter Button/Button Text` 的 `RectTransform`
                              //   （伸锚跨度 0.92575 · `m_SizeDelta.x = −10`）⇒ 250 × 0.92575 − 10 = **221.44**，
                              //   高 57.69（宽 +28.56 / 高 +2.31 = `RM_原版TMP框尺寸.md` 表 B 那一行）。
                              //   ⚠️ **本处只改宽**：`MenuWindowBase.Text` 的框高来自传进去的矩形（60），
                              //   高的那 2.31 未跟 —— 同一族理由见 `DeckRuntime.hdr_back_t` 那条（CJK 行盒 1.516em）。
                              wrapPx: 221.44f, autoMinPx: 10f, autoMaxPx: 42f, autoBasePx: 12f);
            if (clrLab != null)
            {
                clrLab.SetRenderQueue(QPageText);
                clrLab.SetWrappingMode(0);      // 原版 `m_TextWrappingMode = 0`
            }
            Transform clrHit = null;
            if (onClear != null)
                // A17：原版 `…>Clear Filter Button` 是 SpriteSwap（普查 §块 3 第 4 行；我们 1 颗盖原版 3 颗）
                clrHit = AddHit(filtersGrp, "ClearFiltersHit",
                       new PxRect(ClearFltX, ClearFltY, ClearFltX + ClearFltW, ClearFltY + ClearFltH), QPageRow, onClear,
                       clrQ2, "UI_Button_Mulligan", clickCue: WindowSounds.PositiveCue);

            // 🆕 **2026-10-11（A1496）**：三件存下来，交给 `RefreshClearFilter` 按「这一页的筛选空不空」开关
            //   （判据 / 为什么走 `SetActive` 而不是原版的 −550 平移 / 命中闸为什么必须同时翻 → 那三个数组上面那段）。
            if (pageNo >= 0 && pageNo < 4)
            {
                _hdrClearQ[pageNo] = clrQ2;
                _hdrClearTx[pageNo] = clrLab;
                _hdrClearHit[pageNo] = clrHit;
            }

            // 建的时候抽屉大多还没建（`_fltCards` 在 `BuildCardsPage` 更靠后那几行才赋值）
            // ⇒ 这一下只把「出厂态」摆正；真正的兜底在 `Build()` 尾巴那次 `RefreshFilterToggles()`。
            RefreshFilterToggle(pageNo);
            RefreshClearFilter(pageNo);      // 🆕 2026-10-11（A1496）：同上，出厂态先摆正一次
        }

        /// <summary>点了还没做的件 —— **出声**（红线：不许静默失败）。</summary>
        public override void NotifyNotBuilt(string what)
        {
            Debug.Log("[Collection] `" + what + "` 还没实现（本轮建到「外壳 + 四页签 + **Deck 页** + **Cards 页（含筛选栏）**」，"
                      + "见 `Shell/CollectionWindow.cs` 文件头与 `资料/阶段二_卡组线_原版规格.md` §七）");
        }

        static string PageNode(int p)
        {
            switch (p)
            {
                case 0: return "Select Deck Tab";          // ⚠️ 页节点名不是 "Deck Tab"
                // 🔴 **2026-10-12（A288）订正**：原来写的是 `"Card Collection Tab"` —— **错**。
                //   原版的**节点名**是 **`CardsTab`**（`CardCollectionTab` 是**脚本类名**，不是节点名）：
                //   亲读 `d:/2/新解包资源/assets_full/bundle_menus_assets_all/GameObject/CardsTab.json`
                //   的 `m_Name = "CardsTab"`（`m_IsActive = false`）；同目录另有
                //   `Select Deck Tab.json` / `Cardback Tab.json` / `Alternate Art Tab.json` —— 那三个
                //   节点名 = 文件名，**只有这一个例外**（所以当初按前三个的形状推错了）。
                //   ⛔ 别改回 `"Card Collection Tab"`：`Editor/CollectionScene.cs` 那条断言按这个名字取页节点，
                //   名字一错**取不到就红**（那条正是用来钉住这个名字的）。
                case 1: return "CardsTab";
                case 2: return "Cardback Tab";
                default: return "Alternate Art Tab";
            }
        }

        // 🔴 **2026-09-23 修一个静默 bug**：原来四处刷新都写 `transform.Find("Tabs/" + 页名)`，
        //    而 **`Tabs` 是 `Content Area` 的子节点**（`MenuWindowBase.BuildShell`：`res.tabs = Node(area, "Tabs", …)`
        //    —— 那句就在 `Node(root, "Content Area", …)` 下面几行）⇒ **那条路径恒为 null**，
        //    于是「重建卡池 / 重建卡组列表」全在**空转**：**状态对、画面不刷新**，
        //    而断言量的都是状态（`DeckCount()` / `CardsVisibleCount`）⇒ **一条都没报**。
        //    实拍才露出来（清空筛选后搜索框还停在 `impe_`）。
        //    ⇒ 不再拼路径，**直接存页节点引用**。
        readonly Transform[] _pages = new Transform[4];
        /// <summary>第 <paramref name="p"/> 页的节点（未建时给 null）。</summary>
        public Transform PageRoot(int p)
        {
            if (p < 0 || p >= _pages.Length) return null;
            // 🔴 **2026-10-17（`T09` 性能第 1 步的配套）**：页是**懒建**的 —— 任何一处（含自检）
            //   拿到 `PageRoot(p)` 就意味着「要用这一页」⇒ 在这里补一次 `SetupOnce()`。
            //   `SetupOnce` 先置 `_built = true` 再建 ⇒ 建的过程中再调 `PageRoot(同一页)` 不会递归。
            //   ⚠️ 生产路径正常是「切页 → OnOpen → SetupOnce」，这里只是**兜底**（不会被多建）。
            if (p < tabs.Count && tabs[p] is CollectionTabPage ct) ct.SetupOnce();
            return _pages[p];
        }

        /// <summary>四页页头那颗 `Filter Toggle` 的图（下标 = **窗口页号**：0=Deck · 1=Cards · 2=Cosmetics · 3=Styles）。
        /// 原版四页**各有一颗**（`Collection Menu Variant` 逐页实读，见 `BuildFilterHeader` 那段），
        /// 且都是 `EverguildToggle`：**`changeSpriteOnValueChange = 1`** · `m_IsOn = 0` ·
        /// `offSprite = 40k_menu_bt`（4472012397149938974）· `onSprite = 40k_menu_bt_pressed`（4570862220269996290）
        /// ⇒ **抽屉开着时换按下图**。
        /// 🔴 2026-10-05 之前：这颗 quad 的返回值**被丢弃**（没有引用）、三条开合路径一处都不碰它
        /// ⇒ 「开着也不换图」（与 `DeckRuntime` 那颗 `hdr_fltbtn` 当年同一个毛病，那边 A76② 已修）。</summary>
        readonly ImageQuad[] _hdrFltBtn = new ImageQuad[4];

        // ============================================================ 🆕 **2026-10-11（A1496）：`Clear filters` 的显隐**
        // 存下页头那三件（底图 / 字 / 命中区），好按「这一页的筛选是不是空的」整组开关。
        // 🔴 **判据（原版）**：`CollectionFilterController` 带 `clearFiltersButton` / `hiddenPosition` /
        //   `animationTime` 三个字段，**隐藏方式是【平移】到 `hiddenPosition = (−550, 0)`**（不是 `SetActive`），
        //   卡组编辑窗那一份实例逐字段实读得到这组值。
        // ⚠️ **触发条件仍是【推断】**（= 一个筛选都没设时藏）：真正干这件事的方法体**本地缺失**
        //   （`decomp_full` 里 `CollectionFilterController` 只导出 `ctor` + 两个 lambda，都不碰这颗钮）；
        //   旁证 = 用户实拍参考图 `收藏—张组的参考图.png` / `收藏—张牌的界面参考图（打开过滤器按钮后）.png`
        //   那一帧上**这颗钮根本不在**（空筛选）。
        // 🔴 **我们走 `SetActive` 而不是原版那条平移**（与卡组编辑窗 `DeckRuntime` 同一口径、同一句如实标注）：
        //   理由是**命中区**——我们的命中是**静态 `PxRect`**、不跟着节点走，平移了但没关闸就是
        //   「藏起来却还点得到」= 静默失败（本仓红线）。⇒ **显隐与命中闸必须同一条判据一起翻**。
        readonly ImageQuad[] _hdrClearQ = new ImageQuad[4];
        readonly Label[] _hdrClearTx = new Label[4];
        readonly Transform[] _hdrClearHit = new Transform[4];

        /// <summary>这一页的 `Clear filters` 该不该在（= **这一页的筛选不是空的**）。
        /// ⚠️ 口径与卡组编辑窗**逐字相同**：只读 <c>State.Filter.IsEmpty</c>（五个筛选字段），
        /// **不含** `Owned` / `Upgradable` 那两个开关 —— 因为 `Clear filters` 清的正是那五件
        /// （`ClearFiltersNow` 里显式把 `Owned`/`Upgradable` 抄回去，见那里的注释）。</summary>
        public void RefreshClearFilter(int page)
        {
            if (page < 0 || page >= 4) return;
            var d = PageDrawer(page);
            bool on = d != null && d.State != null && !d.State.Filter.IsEmpty;
            var q = _hdrClearQ[page]; var tx = _hdrClearTx[page]; var hit = _hdrClearHit[page];
            if (q != null && q.gameObject.activeSelf != on) q.gameObject.SetActive(on);
            if (tx != null && tx.gameObject.activeSelf != on) tx.gameObject.SetActive(on);
            if (hit != null && hit.gameObject.activeSelf != on) hit.gameObject.SetActive(on);
        }

        /// <summary>四页一起刷（便宜且幂等 —— 同 <see cref="RefreshFilterToggles"/> 那条口径）。</summary>
        void RefreshClearFilters() { for (int i = 0; i < 4; i++) RefreshClearFilter(i); }

        /// <summary>**窗口页号** → 那一页的抽屉（0=Deck · 1=Cards · 2=Cosmetics · 3=Styles）。
        /// ⚠️ 与 <see cref="DrawerByPage"/> 的**抽屉页号**（0=Cards · 1=Styles · 2=Cosmetics · 3=Deck）
        /// 是**两套编号**，别混 —— 前者跟 `PageNode`/`PageRoot` 走，后者是本文件内部的自检口。</summary>
        FilterPanel PageDrawer(int page)
        {
            switch (page)
            {
                case 0: return _deckFltSlide;
                case 1: return _fltCards;
                case 2: return _fltCosmo;
                case 3: return _fltStyles;
                default: return null;
            }
        }

        /// <summary>页头那颗 `Filter Toggle` 按**该页抽屉的逻辑态**换图。
        /// 🔴 跟的是**逻辑态**（`FilterPanel.Open`），**不是**滑动进度 —— 判据链同
        /// `Deck/DeckRuntime.cs` 的 `RefreshHeader`（UGUI `Toggle.Set(!m_IsOn, true)` 点下去就翻、
        /// `onValueChanged` **同步**换图；那 0.3 秒属于**抽屉**的 `DOAnchorPosX`）。
        /// 幂等：值没变时 `q.Texture == t` 直接返回，可以放心到处调。</summary>
        public void RefreshFilterToggle(int page)
        {
            if (page < 0 || page >= _hdrFltBtn.Length) return;
            var q = _hdrFltBtn[page];
            if (q == null) return;
            var p = PageDrawer(page);
            var t = Art(p != null && p.Open ? "40k_menu_bt_pressed" : "40k_menu_bt");
            if (t == null || q.Texture == t) return;
            float a = q.WorldH > 0f ? q.WorldW / q.WorldH : 0f;
            q.SetTexture(t);
            if (a > 0f) q.SetAspect(a);
        }

        /// <summary>四页一起刷（便宜且幂等 —— 见 <see cref="RefreshFilterToggle"/>）。</summary>
        void RefreshFilterToggles() { for (int i = 0; i < _hdrFltBtn.Length; i++) RefreshFilterToggle(i); }

        public void Build()
        {
            // 🔴 **2026-10-17（`A1536`）：首句就把本窗名下的旧滚动区撤掉 —— `Build()` 必须可重入。**
            //   下面 `BuildShell` 第一句是 `DestroyChildren(root)`（把 `transform` 的子件全清掉重建），
            //   而四个页各自会 **重新 `RegisterScroll` 一颗新的 `MenuScroll`**（`DeckScroll` / `CardsScroll` /
            //   `CosmoScroll` / `StyleScroll`，`Owner` 全 = `gameObject`）——「清空再建」不会碰登记表。
            //   ⛔ **兜不住，所以非补不可**（`PointerLayer` 的两道自动清理**都判不出这里已死**）：
            //     · `PruneScrolls` 的 `s == null` —— `MenuScroll` 是**普通 C# 类** ⇒ 节点被销毁不会让它变 null；
            //     · `s.Owner == null` —— `Owner` 指的是**活下来的窗根**（本方法只删子件）⇒ 也恒假。
            //   ⇒ 少了这一句 = **每重建一次净涨 4 条**，旧条目还能被滚轮命中（`OnChanged` 指向已销毁的节点）。
            //   ✅ 形状照抄 `Shell/PracticeModePopup.Build()`（A867 · S1）与 `Shell/InboxWindow.BuildMessageList`：
            //      `UnregisterOwnedBy` **必须在清子件之前**（与登记的顺序成对）。
            //   📌 为什么现在补：`Editor/CollectionScene.cs` 的换语档真鉴别式（`A1517`，按符号搜 `win.Build()`）
            //      会**原地重建三次**，那是本函数的第二次以后调用 —— 见 `资料/普查产出_第十八会话/W5_断言宿主.md`「风险」。
            PointerLayer.UnregisterOwnedBy(gameObject);
            // 🔴 **2026-10-19（`A1498`）：`lastIsMaster: false`** —— 本窗左栏**四项全是真页签**。
            //   判据（原版 prefab 本轮现读，逐级解父链）：`Collection Menu Variant` 根 →
            //   `TabbedWindowComponents`（MB `2277236495604703957`）→ `tabButtons` = MB `1756822263936377557`
            //   → 它的 **`tabButtonPrefab` = `m_PathID 0`（null）**；而 `Content Area/Tab Buttons` 的四个孩子
            //   `Deck` / `Cards` / `CardBacks` / `Alternate Art` 全是 `EverguildToggle`（**没有** `Menu Navigation Panel Button`
            //   那种母版）。⚠️ `Shadow` 是第 5 个孩子，但它不是键（我们走 `BarShadowW` 单独建）。
            //   改法 = `Shell/MainMenuSubmenuWindow.cs` 的 `BuildBar` 新增的 `lastIsMaster` 形参（判据 / 四窗逐条实读 → 它的 doc）。
            //   ⚠️ 缺省 `true` 那一支**逐位等于旧行为** ⇒ 奖励窗/商店一个字没变。
            //   **改坏法**：删掉这个实参 ⇒ 第 4 颗（`Styles`）被当母版关掉 ⇒ 实拍又是「`COSMETICS` 下面整片空」。
            // 🔴 `visualTypes` 在基类里带一个**奖励窗的默认表** ⇒ 本窗必须**整表替换**。
            // 🔴 **2026-10-17（`A1511(a)`）就地改序（铁律 5）**：这两句原来排在 `BuildShell` **之后** ——
            //   而 `BuildBar` 在建每一颗键时就读 `visualTypes[i]` 写进 `TabButtons.options[i].type`
            //   （`Shell/MainMenuSubmenuWindow.cs:465`）⇒ **建的时候拿到的还是基类那张奖励窗默认表**，
            //   事后再替换**已经不生效**（`options` 全仓 0 个读取点 ⇒ 今天只是「数据错了但没人用」，
            //   仍按原版口径修）。⇒ **必须排在 `BuildShell` 之前**。
            if (visualTypes != null)
            {
                visualTypes.Clear();
                visualTypes.Add(WindowTabType.CollectionDecks);
                visualTypes.Add(WindowTabType.CollectionCards);
                visualTypes.Add(WindowTabType.CollectionCosmetics);
                visualTypes.Add(WindowTabType.CollectionStyles);
            }

            var res = BuildShell(transform, Buttons, "CollectionTabButton_", "Tabs", lastIsMaster: false);
            tabButtons = res.buttons;
            _btnHighlight = res.highlight;
            _btnRes = res;

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Collection] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));

            tabs.Clear();
            for (int p = 0; p < 4; p++)
            {
                var pg = Node(tabHolder, PageNode(p), TabsRect);
                _pages[p] = pg;
                var t = pg.gameObject.AddComponent<CollectionTabPage>();
                t.SetHost(this, pg, p);
                tabs.Add(t);
            }
            BuildWatch.Mark("①a `BuildShell` + 四页节点");

            // 🆕 **2026-10-03 A22②：整窗原来没有关闭钮** —— 原版有 `Shared>Close Button`
            //（原版实测：`Collection Menu Variant` 的 `Shared/Close Button` = **192.17,83.44 → 342.17,143.44**（150×60）·
            //  `UI_Button_Mulligan` **Simple**（拉伸）· `trans=2` → HL `UI_Button_Mulligan_hover` ·
            //  它的文字子节点 `Button Text` = **"Back"**（`200.5,89.3→333.4,137.5`，fs40 auto[10~40] 居中）。
            //  复现命令：`python 工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 12 | grep -i close`）
            // 🔴 **2026-10-09（A264）就地订正（铁律 5）**：这里原来写「⚠️ **不在 `Content Area` 里**，是 `Content Area`
            //   的**兄弟**（`Shared`）⇒ 挂窗口根」—— **父链读错了**（错因：照 `menu_dump` 的**缩进**推层级，
            //   没有逐级读 `m_Father`；与 2026-09-14 那条「只比字段值、没解父链」是同一个错法）。**逐级读 `m_Father` 的实读**：
            //     `Collection Menu Variant` → `Content Area` → **`Tabs`** → `Shared` → `Close Button` → `Button Text`
            //   ⇒ `Shared` 是 **`Tabs` 的【直接】子件**（所以本块挂 `tabHolder` 下），`Button Text` 系在 `Close Button` 底下。
            //   判据（**原始 MB 字段现读**，⛔ 不是 dump 缩进）：
            //     · `bundle_menus_assets_all/GameObject/Shared.json` 的 `m_Component` **只有 1 项**（= 那颗 RT `5226810256338313941`）
            //       ⇒ `Shared` 是**纯容器**（既没有 `Image`、也没有 `RectMask2D` ⇒ 我们不建任何图，只有一颗 RectTransform 节点）
            //     · 那颗 RT：`m_AnchorMin=(0,0)` · `m_AnchorMax=(1,1)` · `m_SizeDelta=(0,0)` · `m_AnchoredPosition=(0,0)`
            //       ⇒ **拉伸锚 ⇒ 矩形 = 父件（`Tabs`）的矩形** = **167.17,70.94 → 1920.01,1080（1752.83 × 1009.06）**
            //       ⇒ 所以下面用 `TabsRect`（与 `Tabs` 同一条表达式，⛔ 不是「另抄一份数」）
            //     · 它的 `m_Father` = `Tabs`（RT `7557460012314976981` · GO `Tabs_-3296323895330151723.json`）；
            //       `m_Children` **只有 1 项** = `Close Button`（RT `−6464277725189250347`）
            //     · `Close Button` 那颗 RT = `a=(0,1)-(0,1)` · `pivot=(0,.5)` · `sd=(150,60)` · `ap=(25,−42.5)`
            //       ⇒ 世界矩形 **192.17,83.44 → 342.17,143.44**（`CloseBtnL/T/R/B` 那几个常量与 `menu_dump` 印的都是
            //       1 位小数版 192.2/83.4/342.2/143.4，差 ≤0.04px —— **本件不动那几个常量**）
            //     · 🔴 **只有收藏窗有这一层**：`bundle_menus_assets_all` 的 **616 个 prefab 根**逐棵走树，
            //       命中 `Shared` 的**只有** `Collection Menu Variant`（`Rewards Base Submenu Variant` /
            //       `Shop Menu Variant` / `Social Submenu Variant` / `Deck Editing Menu` 逐根复查**全 ❌**）
            //       ⇒ ⛔ **别的窗别照抄这一层**（全库普查 → `资料/普查产出_1009/查证V1_原版prefab四件.md` §四）
            //     · ⚠️ **本块摆在四页之后是刻意的**：原版 `Tabs` 的子件序 = `Select Deck Tab` / `CardsTab` /
            //       `Cardback Tab` / `Alternate Art Tab` / **`Shared`（最后一个）**（那颗 `Tabs` RT 的 `m_Children`
            //       逐项现读）⇒ ⛔ 别为了「少动几行」把它挪回 `BuildShell` 后面（那样 `Shared` 会变成第一个子件）。
            {
                var shared = Node(tabHolder, "Shared", TabsRect);      // `tabHolder` = `Content Area/Tabs`
                var cr = new PxRect(CloseBtnL, CloseBtnT, CloseBtnR, CloseBtnB);
                var cq = Rect(shared, "UI_Button_Mulligan", cr, "Close Button", QPanel);
                // 🔴 **2026-10-08 就地订正（铁律 5）：`Button Text` 要挂在 `Close Button` **底下**（原来是兄弟）。**
                //   原版层级（`python 工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 12`
                //   实读，**缩进就是层级**）：
                //     3  Shared            167.2, 70.9 → 1920.0, 1080.0
                //     4    Close Button    192.2, 83.4 →  342.2,  143.4（150×60）
                //     5      Button Text   200.5, 89.3 →  333.4,  137.5 · 'Back' 字号=40 auto[10~40] Center/Capline 折行=0
                //   ⇒ `Button Text` 是 `Close Button` 的**子节点**，不是它的兄弟。
                //   我们原来把两者都挂在窗口根上（`Text(transform, …)`）⇒ 断言按原版路径取
                //   `FindChild(closeBtn, "Button Text")` 时**取不到**（那两条因此红，见
                //   `Editor/CollectionScene.cs` 的「A22② 那颗 Back 钮」一组）。
                //   ⚠️ **画面不动**：`MenuWindowBase.Text` 用的 `Local(parent, …)` 是「**世界 − 父的世界位置**」
                //   ⇒ 换父**世界矩形逐位不变**（同一个坑 `Shell/TrophyInfoPopup.cs` 文件头记过一次）。
                // 🔴 **2026-10-09（A265）**：文字用自己的矩形 `BackTxt*`（**132.86×48.24**），
                //   **不是整颗钮的 `cr`（150×60）**。中心只差 0.25px（看不出来），但
                //   `SetAutoFitBox` 的**框宽也跟着错** ⇒ 字号自适应那一档的上限框比原版宽 17.14px。
                //   判据 / 「是序列化不是 ARF」的查证过程 → `BackTxtL` 的注释。
                //   单击命中区**照旧用整颗钮的 `cr`**（原版 `AddHit` 那颗是 150×60，别跟着改）。
                var bt = new PxRect(BackTxtL, BackTxtT, BackTxtR, BackTxtB);
                // 🔴 **2026-10-18（第三轮整改 · 审查 P4）字走 `Loc.T`** —— 键 = 原版那颗 `Button Text` 的
                //   `Localize.mTerm` 原文 `MainMenu/MainButtons/ButtonLabel/Back`（同键 8 颗；本窗这一颗 =
                //   `… > Collection Menu Variant > Content Area > Tabs > Shared > Close Button > Button Text`
                //   —— 正是本行底下那颗）。英文列 = TMP 原文 `Back`；中文列 = 「返回」(`zh_CN.csv:5`)。
                //   ⛔ 节点名 `"Button Text"` 不动（`Editor/CollectionScene.cs` 的 A212 那一节按名找它读折行档）。
                // 🔴 **2026-10-09（A1173）**：`SetAutoFitBox` 那 5 个数收进 `Text` 的形参（原来是先 `Text`、
                //   再手工补一次 —— 两份写法并存 = 迟早不一致）。下面那段 A305① 的判据原样留着。
                //   ⚠️ 框用**文字自己的** `bt`（`bt.W × bt.H`），⛔ 不是整颗钮的 `cr`（见 A265 那条注释）。
                var cl = Text(cq != null ? cq.transform : shared, Loc.T("MainMenu/MainButtons/ButtonLabel/Back"),
                              bt.x1, bt.x2, bt.y1, bt.y2,
                              5, Color.white, "Button Text", 40f,
                              // 🔴 **2026-10-11（A305①）**：`autoBasePx` = 原版 `Button Text` 的 `m_fontSizeBase` **原文**。
                              //    判据（原版实读）：`/Collection Menu Variant/…/Shared/Close Button/Button Text`
                              //    = `m_fontSize 40` · `auto[10~40]` · **`base 12.0`**（逐站表 §二·3 #22）
                              //    —— 同族（按钮文案那一族）`Back` / `WebShop Button` / `Continue` 也都是 **12**。
                              wrapPx: bt.W, autoMinPx: 10f, autoMaxPx: 40f, autoBasePx: 12f);
                if (cl != null)
                {
                    cl.SetRenderQueue(QText);
                    // 🔴 **2026-10-08（A212）**：原版这颗 `Button Text` 是 **`折行=0`**（实读：
                    //   `Button Text … 'Back' 字号=40.0 auto[10.0~40.0] 对齐=Center/Capline 折行=0`
                    //   —— 命令 `python 工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 20`），
                    //   而 `SetAutoFitBox` 上面刚**无条件**把折行打开了 ⇒ 显式还原成原版那一档
                    //   （同 `Shell/PromptPopup.cs` 里那颗钮的折行修法（A77⑩ · 子表 E4）注 那颗钮的修法 = A62 的 E4）。
                    cl.SetWrapping(false);
                    // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版这颗 `Button Text` 是
                    //   `Center/**Capline**`（判据 = 上面 `:2234` 与 `:2258` 两次实读的原版 dump 原文：
                    //   `Button Text … 'Back' 字号=40.0 auto[10.0~40.0] 对齐=Center/Capline 折行=0`）。
                    //   框用**文字自己的** `bt`（132.86×48.24，`Capline` 不吃框高 ⇒ 传它只为口径统一）。
                    MenuDraw.SetVAlign(cl, Label.VAlign.Capline, bt);
                }
                // ⚠️ **命中区照旧挂在窗口根上、本件不动它**：`CloseHit` 是**我们自己加的节点**（原版那颗钮
                //   自己就是射线靶子，没有对应的原版节点/路径）⇒ 挪它不属于 A264 的范围；而且命中矩形的世界
                //   坐标与父链无关（`MenuDraw.Hit` 内部走 `Local(parent, …)`）⇒ 换不换挂点在画面与手感上都一样。
                AddHit(transform, "CloseHit", cr, QPanel, () => Close(), cq, "UI_Button_Mulligan",
                       clickCue: WindowSounds.PositiveCue);
            }

            // 🔴 **本窗的底图必须压到所有页内容之下**（2026-09-23 实拍抓的）：
            //    `CardView`（卡池用的那个外来组件）**所有层都落在默认队列 3000**
            //    （全工程只给 SDF 那层显式设过 3000，其余靠材质默认）—— 而基类给 `Background` 的是
            //    `QPanel = 3005` ⇒ **整片卡池被窗口底图盖住**：截图里一片空，而**断言全绿**
            //    （断言量的是节点位置与尺寸，量不到「谁盖谁」）。
            // 🔴 **2026-10-17（第四十八会话 · `A1644`）：2000 → 2940。**
            //    原来压到 2000 只考虑了「页内容 3000+ / 左栏 3005」，**漏了外壳自己的 2910 那一档** ——
            //    `Navigation Panel/Panel Shadow`（`Shell/MainMenuRuntime.cs:863`，队列 **2910**、α 0.4667）
            //    是外壳左竖导航的投影；原版兄弟序里 `Navigation Panel` 在窗口 Holder **之前**⇒ 窗画在它上面。
            //    我们把底图压到 2000 ⇒ 这条影子反过来盖在窗底图上，在收藏窗左沿露出一条暗渐变。
            //    **取值区间 = (2910, 3000)**：> 外壳 2910 梯子、< `CardView` 的 3000。取 2940。
            // ⚠️ 底图在 `Content Area/Background`（不是窗口根的直接子节点 —— 正本 §二）
            var bgNode = transform.Find("Content Area/Background");
            if (bgNode != null)
                foreach (var q in bgNode.GetComponentsInChildren<ImageQuad>(true)) q.SetRenderQueue(2940);

            // 🔴 **2026-10-17（第四十八会话 · `T09` 性能第 1 步）：只建落地页（第 0 页），其余页懒建。**
            //   实测旧行为「四页一次性全建」首开 ≈75.5 s（Cards 18 格 51.4 s · 异画 6 格 17.3 s ·
            //   卡背 3.4 s · 卡组 2.75 s），而玩家落地的是第 0 页 ⇒ 另外三页的 71 s 白花。
            //   `CollectionTabPage.OnOpen()` 会在**每次切页**时调 `SetupOnce()`（首建 + 之后空转）。
            //   ⚠️ 下面 `RefreshFilterToggles` / `RefreshClearFilters` 对未建页的 null 已有守卫。
            var page0 = tabs.Count > 0 ? tabs[0] as CollectionTabPage : null;
            if (page0 != null) page0.SetupOnce();
            else if (tabs.Count > 0) tabs[0].Setup();       // 兜底：类型对不上时仍建第 0 页（⛔ 不静默空着）
            BuildWatch.Mark("①b 页 0（`" + PageNode(0) + "`）· `Setup()`（其余三页懒建）");

            // 🔴 **四页的 `Filter Toggle` 收尾刷一次**（2026-10-05）：四份抽屉是在上面那句 `Setup()` 里
            //   才陆续建起来的（页头比抽屉先建）⇒ `BuildFilterHeader` 末尾那次 `RefreshFilterToggle(pageNo)`
            //   读到的 `PageDrawer(page)` 要么是 **null**（首建）、要么是**上一轮那一份**（重建 ——
            //   `_fltCards`/`_fltStyles`/… 还指着旧对象）⇒ 少了这一下，**重建**时页头那颗钮会停在
            //   上一轮那张图上（与抽屉此刻的状态不符）。⛔ 它是**兜底**，别删。
            // ⚠️ **2026-10-05 更正（铁律 5）**：原文写「异画页那份**出厂就是展开的** ⇒ 少了这一下，
            //   它起手会画成常态图」—— **那个前提是错的**（四页起手一律收起，见 `BuildStylesPage` 那段更正）。
            //   ⛔ 但这一行照旧要留：它的作用在**重建**那条路上，不在首建。
            RefreshFilterToggles();
            RefreshClearFilters();
            BuildWatch.Mark("①c 收尾刷（`RefreshFilterToggles` + `RefreshClearFilters`）");
        }

        // ============================================================ Deck 页的内容（由 `CollectionTabPage` 调）

        /// <summary>页头：**与另外三页同一条 `Header`**（2026-10-05 收口，见 `BuildFilterHeader`）
        /// + 本页多出来的 `Control Buttons`（`Import` / `Create`）。
        /// 原版 `Select Deck Tab>Header` 的**树序** = `Filter Toggle` → `Control Buttons` → `Separator Line`
        /// → `Filters`(→`Clear Filter Button`)。
        /// 🔴 `Control Buttons` 的 HLG：spacing **25** · `pad.right` **14** · align **MiddleRight(5)** ·
        /// **`m_ReverseArrangement = 1`** · `expandW=1` `ctrlW=0` · 外加 `ContentSizeFitter`
        /// （`m_HorizontalFit = 1` = **MinSize**）。
        /// ⇒ **视觉左→右 = 树序 `[Create, Import, Unlock]` 的【倒排】**：
        /// `Unlock` **1180.00** → `Import` **1391.00** → `Create` **1661.01**（组矩形 **1179.99,80.94 → 1920.01,140.94**）。
        /// ⚠️ **2026-10-05 就地订正（铁律 5）**：**原文**写「（spacing 25 · **padTop 14** · 右对齐）⇒
        /// `Create` **1180,80.9→1425,140.9**、`Import` **1450→1695**、`Unlock` 1720→1906」——
        /// **那三个 x 是【镜像读数】**（`Create`/`Import`/`Unlock` 按**树序**从左往右摆 = 把倒排画反了），
        /// 且 **padding 读错了轴**（是 `pad.right = 14` 不是 `padTop`）。
        /// **错因**：结论抄自还不建模 `m_ReverseArrangement` 的 `menu_dump.py` 输出。
        /// ⚠️ **实现本身一直是对的**（`CreateX = 1661` / `ImportX = 1391` 与真值逐位吻合）—— 错的是这段注释；
        /// 订正后**别按这段旧注释把两颗钮镜像过去**。
        /// 🔴 `Unlock`（组内最左、**1180.00,80.94→1366.00,140.94**，宽 186.01）：**照原版建了**（🆕 A98 补，
        /// 见 `BuildUnlockPlaceholder`）—— 它 `GameObject.m_IsActive = 1`，但**三个可见组件全是 `m_Enabled = 0`**
        /// （自己的 `Image` · `Button Outline` 的 `Image` · `Text` 的 TMP ⇒ 连 `Debug Unlock` 那行字也不画）
        /// ⇒ **原版也画不出东西**，所以我们只建**结构**、不画像素。
        /// ⚠️ 这是**组件级**禁用、不是 GO 级 —— 与「顺序错」是**两条账**，别混。
        /// 判据：`python 工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 20 --md`。</summary>
        public void BuildDeckHeader(Transform page)
        {
            // 🔴 **2026-10-12（本会话 · 逐颗现读 prefab）就地订正（铁律 5）：`Clear filters` 的字号 = 42，不是 33。**
            //   原文写「本页字号 = **42 / 33**」，而 `33` 是**「Cosmetics 页那一颗」的值**（串了页）。
            //   判据（`menu_dump.py bundle_menus_assets_all "<页节点>" --depth 4`，四页逐颗现读
            //   `… ▸ Clear Filter Button ▸ Button Text` 的 `m_fontSize`）：
            //     · `Select Deck Tab`（**本页**）= **42.0**（`'Clear filters' 字号=42.0 基准=12.0 auto[10.0~42.0]`）
            //     · `CardsTab` = 42.0 · `Alternate Art Tab` = 42.0 · `Cardback Tab` = **33.0**
            //   ⇒ 四页里只有卡背页是 33（它连 `Filters` 那颗也是 35，见 `BuildFilterHeader` 的 `filterPx`）。
            //   ⚠️ 这一颗**只在「这一页有筛选」时才显示**（`RefreshClearFilter`），所以默认态截图上看不出来 ——
            //     但它是**实打实的偏离**，照原版改。
            // ============================================================ 🆕 **2026-10-17（`A1642`）**：页头容器 **`Header`**
            // 🔴 **原版这一层我们原来根本没有** —— 页头那几件（`Filters Button` / `Filters Label` / `Separator Line` /
            //   `Control Buttons` / `Clear filters`）是**直接建在 `page`（`Select Deck Tab`）上**的。
            //   判据（`python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Select Deck Tab" --depth 4` **现读**）：
            //     `Select Deck Tab` 的直接子件 = **`Decks Tab`** → `Select Game Mode Deck Tab`（本窗不建）
            //     → **`Header` 167.2,70.9 → 1920.0,155.9（1752.83 × 85.00）**，`Header` 是**最后一个孩子**；
            //   `Header` 自己的孩子（原版序）= `Filter Toggle` → `Control Buttons` → `Separator Line` → `Filters`
            //   （`Filters` 里才是 `Clear Filter Button`）。
            // ⚠️ **换父不动一个像素**：`MenuDraw.Node/Rect/Text` 全走 `Local(parent, 世界矩形)`（= 世界 − 父的世界位置）
            //   ⇒ 只要父节点的**世界位置**对，子件的世界矩形逐位不变（先例：`Shared ▸ Close Button ▸ Button Text`）。
            // ⚠️ **2026-10-17（`N52②`）就地订正（铁律 5）**：原文写「`Filters` 那层本批没建（同名会取错）」——
            //   **已建**（`BuildFilterHeader` 里按退化矩形建，纯结构）；现读 `FindChild(…,"Filters")`
            //   在收藏窗外**没有**第二处消费者 ⇒ 取法用**路径**即可。
            var header = Node(page, "Header", new PxRect(HdrL, HdrT, HdrR, HdrB));
            BuildFilterHeader(header, 0, 42f, 42f, ClearDeckFilters, ToggleDeckFilters, header);

            // 🆕 A98（2026-10-06）：**`Control Buttons` 组节点**（原来两颗钮**直接挂在页节点上**、这一层没建）。
            //    原版参数（我逐字段直读 prefab，不是转抄）：
            //      · 组 rect **1180.00,80.94 → 1920.01,140.94**（740.01×60；见 `CtrlGrpL/CtrlGrpT/CtrlGrpR/CtrlGrpB`）
            //      · 组上挂 `HorizontalLayoutGroup`（MB `-8678298005025004843`，`m_Enabled = 1`）：
            //        **`m_ReverseArrangement = 1`** · `m_Spacing = 25` · `m_ChildAlignment = 5`(MiddleRight) ·
            //        `m_Padding` = (left 0, **right 14**, top 0, bottom 0) ·
            //        `m_ChildForceExpandWidth = 1` · `m_ChildControlWidth = 0`
            //      · 组上还挂 `ContentSizeFitter`（MB `8352583001903784661`，`m_Enabled = 1`）：
            //        **`m_HorizontalFit = 1`(MinSize)** · `m_VerticalFit = 0`
            //    ⇒ 组宽 = 186.01 + 245 + 245 + 25 + 25 + **14**(pad.right) = **740.01**（= 实读值）；
            //      子件落点 = `Unlock` 1180.00 → `Import` **1391.01** → `Create` **1661.01**。
            //    🔴 **「等价 HLG/CSF」= 上面这套数学，不是真挂两个组件**：本工程全线是「正交相机 + 世界空间
            //      mesh(`ImageQuad`) + TMP 世界空间文字」、**整个 Shell 里没有 Canvas**（见 `Shell/ShellRuntime.cs` 文件头那条「原版是 UGUI Canvas、我们全线没有 Canvas」注）
            //      ⇒ uGUI 的布局组件**永远不会被驱动**（`CanvasUpdateRegistry.PerformUpdate` 只由
            //      `Canvas.willRenderCanvases` 推）—— 挂上去是死数据，而且将来一旦有 Canvas，
            //      `CSF(H=MinSize)` 会按「无 rect 子件 ⇒ 首选宽 0」把这个节点的 `sizeDelta.x` **悄悄改成 0**。
            //      ⇒ 组节点按**原版 rect** 建、三颗子件按**上面那套算式**摆（与另外 40+ 扇窗同一条路，
            //        布局数学只此一份：`Core/UguiRect.cs`）。
            //    ⚠️ 树序照原版 = **`[Create, Import, Unlock]`** —— 它与**视觉序相反**（`reverse=1`：
            //      视觉左→右 = `Unlock`(1180) → `Import`(1391) → `Create`(1661)）。⛔ 别按「树序 = 视觉序」摆。
            var ctrl = Node(header, "Control Buttons", new PxRect(CtrlGrpL, CtrlGrpT, CtrlGrpR, CtrlGrpB));
            // 🔴 **原版兄弟序** = `Filter Toggle` → `Control Buttons` → `Separator Line` → `Filters`；
            //   `BuildFilterHeader` 先把后三件建好了 ⇒ 把本组挪到第 1 位（视觉零影响，纯结构复刻）。
            ctrl.SetSiblingIndex(1);

            // 🔴 **2026-10-03 就地更正（A22①）**：常态图原来画的是 `40K_button` —— **原版是 `UI_Button_Mulligan`**
            //（判据 = `资料/普查产出_1003/按钮悬停图_普查.md` §块 3 第 1、2 行：
            //  `Select Deck Tab>Header>Control Buttons>{Create,Import}`，且常态图与高亮图逐颗列了）
            var newQ = Rect(ctrl, "UI_Button_Mulligan", new PxRect(CreateX, HdrBtnY, CreateX + HdrBtnW, HdrBtnY + HdrBtnH), "Create", QPageRow);
            var impQ = Rect(ctrl, "UI_Button_Mulligan", new PxRect(ImportX, HdrBtnY, ImportX + HdrBtnW, HdrBtnY + HdrBtnH), "Import", QPageRow);
            // ⚠️ 文案与字号照 A2 §三·4：`Import` 那条的字是 **"Import Deck"**、`Create` 是 **"Create Deck"**，都是 **fs42**(auto 10-42)
            // 🆕 A98：字挂在**按钮自己**底下（原版 = `Create>Button Text` / `Import>Button Text`）。
            //    ⚠️ **名字不照原版那个 `Button Text`**：两颗同名 ⇒ `FindChild`（按名字取**第一个**）会取错那一颗，
            //       故沿用我们自己的「`Create Text` / `Import Text`」。⚠️ 图缺了就把字退回挂在组节点上（不许静默丢字）。
            // 🔴 **2026-10-18（A891 的续）：两颗钮的字都走 `Loc.T`** —— 词条键 = 原版 prefab 上那两颗
            //   `Localize.mTerm` 的**原文**（本批按 pid 亲读；父链 = `Collection Menu Variant > Content Area >
            //   Tabs > Select Deck Tab > Header > Control Buttons > {Create,Import}/Button Text`）：
            //     · `Create` 那颗 = **`MenuDeck/MenuButtons/CreateDeck`**，TMP 原文 `Create Deck`（大写 D）；
            //     · `Import` 那颗 = **`MenuDeck/MenuButtons/ImportDeck`**，TMP 原文 `Import Deck`。
            //   中文列：`Create Deck` = 「创建卡组」(`zh_CN.csv:7`)；⚠️ `Import Deck` 那份 CSV 里**没有这个键**
            //   ⇒ 那一条的中文**是我们自拟的**（见 `Core/Loc.cs` 那一条自己的注释）。
            //   ⛔ 节点名 `"Create Text"` / `"Import Text"` 不动（`Editor/CollectionScene` 里按名找它们的那两处）。
            var newLab = Text(newQ != null ? newQ.transform : ctrl, Loc.T("MenuDeck/MenuButtons/CreateDeck"),
                              CreateX, CreateX + HdrBtnW,
                              HdrBtnY, HdrBtnY + HdrBtnH, 5, PageInk, "Create Text", 42f,
                              // 🔴 **2026-10-10（A1212 · 块 3 #9）**：接上 `m_enableAutoSizing`（四格 = **10 / 42 / 12 / 0**）。
                              //   判据 = `R5` §2 #9（`… ▸ Select Deck Tab ▸ Header ▸ Control Buttons ▸ Create ▸ Button Text`）。
                              //   闸：`42 > 0 ∧ 10 > 0 ∧ fontPx 42 > 10` 全真。折行 = `0` ⇒ 还原（下面那句）。
                              // 🔴 **2026-10-17（`A1226` 表 B #9）：框 = 原版有效框 220.81（原来整颗钮 245）。**
                              //   判据 = 原版 `…/Control Buttons ▸ Create ▸ Button Text`（伸锚 · `m_SizeDelta.x = −6`）
                              //   ⇒ 220.81 × 57.52（宽 +24.19 / 高 +2.48）。
                              wrapPx: 220.81f, autoMinPx: 10f, autoMaxPx: 42f, autoBasePx: 12f);
            if (newLab != null)
            {
                newLab.SetRenderQueue(QPageText);
                newLab.SetWrappingMode(0);      // 原版 `m_TextWrappingMode = 0`
            }
            var impLab = Text(impQ != null ? impQ.transform : ctrl, Loc.T("MenuDeck/MenuButtons/ImportDeck"),
                              ImportX, ImportX + HdrBtnW,
                              HdrBtnY, HdrBtnY + HdrBtnH, 5, PageInk, "Import Text", 42f,
                              // 🔴 **2026-10-10（A1212 · 块 3 #10）**：接上 `m_enableAutoSizing`（四格 = **10 / 42 / 12 / 0**）。
                              //   判据 = `R5` §2 #10（同上 `… ▸ Control Buttons ▸ Import ▸ Button Text`）。
                              //   折行 = `0` ⇒ 还原（下面那句）。
                              // 🔴 **2026-10-17（`A1226` 表 B #10）：框 = 原版有效框 220.81（原来整颗钮 245）。**
                              //   判据 = 原版 `…/Control Buttons ▸ Import ▸ Button Text`（同族伸锚）⇒ 220.81 × 57.52。
                              wrapPx: 220.81f, autoMinPx: 10f, autoMaxPx: 42f, autoBasePx: 12f);
            if (impLab != null)
            {
                impLab.SetRenderQueue(QPageText);
                impLab.SetWrappingMode(0);      // 原版 `m_TextWrappingMode = 0`
            }
            BuildUnlockPlaceholder(ctrl);           // 组内**最左**那一格（原版画不出东西，只建结构）

            // 🆕 A17：这三颗原版都是 SpriteSwap，高亮图都 = `<常态图>_hover`（普查 §块 3 第 1～3 行）
            AddHit(ctrl, "ImportHit", new PxRect(ImportX, HdrBtnY, ImportX + HdrBtnW, HdrBtnY + HdrBtnH), QPageRow,
                   () => OpenImportPopup(), impQ, "UI_Button_Mulligan", clickCue: WindowSounds.PositiveCue);
            AddHit(ctrl, "CreateHit", new PxRect(CreateX, HdrBtnY, CreateX + HdrBtnW, HdrBtnY + HdrBtnH), QPageRow,
                   () => CreateDeck(), newQ, "UI_Button_Mulligan", clickCue: WindowSounds.PositiveCue);
            // ⚠️ `Filters` / `Clear filters` 两处命中区（原来在本方法里各建过一颗）现在随 `BuildFilterHeader`
            //    一起在场 —— **别在这里再建第二颗**：同名节点会长出两个，`FindChild` 只拿得到先建的那颗。
        }

        /// <summary>🆕 A98：`Control Buttons` 里那颗 **`Unlock`** —— **原版自己也画不出东西**。
        /// 照原版建**结构**、**一个像素都不画**：下表那三个可见组件在原版里**全是 `m_Enabled = 0`**
        /// （组件级禁用；三个 GO 自己的 `m_IsActive` 都是 1 —— 别记成「GO 级禁用」）。
        ///
        /// <para>判据（逐字段直读 `d:/2/新解包资源/assets_full/bundle_menus_assets_all`，不是转抄）：
        /// ① `Unlock`（GO pid `7676841223838687957`）`m_IsActive = 1`；rect 组内 (0,0)→(186.01,60)
        ///    ⇒ 绝对 **1180.00,80.94 → 1366.01,140.94**；它自己的 `Image`（MB pid `-7966590713807052075`，
        ///    `m_Type = 1` Sliced · sprite `2549195772099431333` · 色 `(0.212,0.094,0.098,1)`）**`m_Enabled = 0`**；
        /// ② 子 `Button Outline`（GO `-8849062637360586027`）的 `Image`（MB pid `5063668835359646421`，
        ///    Sliced · `m_FillCenter = 0` · sprite `-2491358013934088604`）**`m_Enabled = 0`**，
        ///    rect 相对 `Unlock` (0.50,0)→(185.51,60)；
        /// ③ 子 `Text`（GO `-220370555535629611`）的 `TextMeshProUGUI`（MB pid `6174172274644869845`，
        ///    文案「**Debug Unlock**」· fs 35.75 auto[18,45]）**`m_Enabled = 0`**，
        ///    rect 相对 `Unlock` (8.36,8.59)→(177.65,51.41)。</para>
        ///
        /// <para>🔴 **那为什么还要建**：① 原版树里就有这三个节点（铁律 11：全量复刻，判据齐就得建）；
        /// ② **它占的那 186.01px 是 `Import` 落在 1391.01 的原因**
        /// （1180.00 + 186.01 + spacing 25 = 1391.01）—— 少了它，组节点的几何就对不上。
        /// ⛔ **别顺手给它加图 / 加字**：原版一个像素都没有（`Editor/CollectionScene.cs` 有一条断言钉着
        /// 「`Unlock` 底下没有任何 `ImageQuad` / `Label`」）。</para></summary>
        void BuildUnlockPlaceholder(Transform ctrl)
        {
            var un = Node(ctrl, "Unlock", new PxRect(CtrlGrpL, CtrlGrpT, CtrlGrpL + UnlockW, CtrlGrpB));
            // 两个子件的 rect 在原版是**相对 `Unlock` 左上角**的（见上面 ②③）⇒ 这里加上 `Unlock` 的左上角
            Node(un, "Button Outline", new PxRect(CtrlGrpL + 0.50f, CtrlGrpT,
                                                  CtrlGrpL + 185.51f, CtrlGrpB));
            Node(un, "Text", new PxRect(CtrlGrpL + 8.36f, CtrlGrpT + 8.59f,
                                        CtrlGrpL + 177.65f, CtrlGrpT + 51.41f));
            // ⚠️ 子件名**照原版**（`Button Outline` / `Text`）。注意 `Text` 是个**通用名**：
            //    本窗此前没有别的节点叫它，但**将来谁要按 `FindChild(随便一个宽范围的根, "Text")` 找字**，
            //    会遇到这一颗（它**没有 `Label`**）⇒ 找字请**从具体的父节点往下**找（本仓库既有写法就是那样）。
        }

        /// <summary>卡组列表（**纵向滚**）：6 列 × 225×364.5、spacing (20,0)、pad L10。</summary>
        public void BuildDeckList(Transform page)
        {
            // ============================================================ 🆕 **2026-10-17（`A1642`）**：容器层级
            // 🔴 **原版 `Select Deck Tab` 底下是三层容器**（`menu_dump.py … "Select Deck Tab" --depth 4` **现读**）：
            //     `Select Deck Tab`(167.2,70.9→1920,1080)
            //       └ `Decks Tab`(同矩形)                       ← 挂 `DeckCollectionTab`，字段 `display`
            //          └ `Collection Display`(同矩形)          ← 挂 `DeckCollectionDisplay` + `EventRelay`
            //             ├ `Deck Scroll View`(330.9,155.9→1920,1080)  ← 里面才有 `Empty Collection Warning`
            //             └ `Deck Filters`    (0.1,155.9→335.6,1080)
            // 我们原来**这两层都没建**（`Deck Scroll View` / `Deck Filters` 直接挂 `page`）。
            // ⚠️ 两个容器**与页节点同矩形**（原版它们是**拉伸锚 + `sizeDelta (0,0)`**）⇒ 建出来**像素零变化**，
            //   只是把树形补齐；子件的世界矩形也不动（`MenuDraw.Local` = 世界 − 父的世界位置）。
            //   这里直接复用 `TabsRect`（**与页节点 `Node(tabHolder, PageNode(p), TabsRect)` 同一条表达式**）——
            //   ⛔ 别再抄一份 `167.2/70.9/1920/1080` 的数（铁律：同一条规则只留一处）。
            var decksTab = Node(page, "Decks Tab", TabsRect);
            var disp = Node(decksTab, "Collection Display", TabsRect);
            var holder = Node(disp, "Deck Scroll View", DeckViewport);
            // 🔴 **2026-10-13（A435 阶段 2 · 乙 · A19）**：裁切状态长在这颗视口节点上
            //   （原版这一页的 `Viewport` 挂着 `RectMask2D`：`m_Softness=(0,0)`·`m_Padding=(0,0,0,0)`
            //    —— 与 `RebuildDeckCells` 里原来那一对 `Clip = DeckViewport; … Clip = prevClip;` 逐字同值）。
            var deckVp = ViewportClip.Hang(holder, "Viewport", DeckViewport, Vector4.zero, Vector2Int.zero);
            int n = CollectionData.DeckCount();
            int rows = Mathf.Max(1, Mathf.CeilToInt(n / (float)DeckCols));
            // ⚠️ **尾巴那一项也是纵向的**（原来写的是横向的 `- DeckSpacingX`）—— 2026-10-11 改的时候漏了它，
            //   内容高少 20 ⇒ 「滚到底 ⇒ 最后一格完整落进视口」那条断言当场红（实测 max=149.4 而不是 169.4）。
            float contentH = rows * (DeckCellH + DeckSpacingY) - DeckSpacingY;
            DeckScroll = MenuScroll.TopAligned(DeckViewport, contentH, 100f);   // 原版 `Deck Scroll View` = 100（D_N29）
            DeckScroll.Owner = gameObject;
            DeckScroll.ClipNode = deckVp;               // 🔴 A465（同上）
            DeckScroll.OnChanged = () => RebuildDeckCells(holder);
            PointerLayer.RegisterScroll(DeckScroll);
            RebuildDeckCells(holder);

            // `Empty Collection Warning`：**原版四个页各有一份、矩形各不相同**（2026-09-24 直读
            // `menu_rect.py bundle_menus_assets_all "Collection Menu Variant" --depth 8`）：
            //   Deck `165.88,70.94 → 1970.01,1080`（1804.12×1009.06）· Cards `135.22,…`（1834.78）
            //   · Cosmetics `170.44,…`（1799.56）· Styles `330.22,287.67 → 1920,1080`（= 它自己的视口矩形）。
            // ⚠️ 原来那条「Deck 页没有这一件」是**没查到**，不是不存在。
            // 🆕 **2026-10-17（`A1642`）**：父件改成 **`Deck Scroll View`**（原版父链逐级现读 =
            //   `Select Deck Tab ▸ Decks Tab ▸ Collection Display ▸ Deck Scroll View ▸ Empty Collection Warning`）
            //   —— 我们原来挂在 `page` 上。
            //   ⚠️ **它现在是 `Deck Scroll View` 的兄弟之外的唯一"常驻"孩子** ⇒ `RebuildDeckCells` 那个
            //   「留下 `Viewport`、其余全删」的循环**必须也放过它**（否则一滚动/一次筛选就把空态提示删掉，
            //   而 `_deckEmpty` 变成已销毁对象的引用 ⇒ `RefreshDeckEmpty` 静默失效）—— 那里已同步。
            {
                var ew = Node(holder, "Empty Collection Warning", new PxRect(165.88f, 70.94f, 1970.01f, 1080f));
                // 🔴 **2026-10-18（第三轮整改 · 审查 P4）字走 `Loc.T`** —— 键 = 原版那颗 `Warning` 的
                //   `Localize.mTerm` 原文 `MenuCollection/NoDecksFound`（全库只 1 颗，就是本窗这一颗：
                //   `… > Decks Tab > Select Deck Tab > Collection Display > Deck Scroll View >
                //   Empty Collection Warning/Warning`）。
                //   🔴 **我们原来那句英文与原文【不同】**（我们写 `There are no decks in your collection`，
                //   原文是 `There are no deck …` 单数 + `…for the selected filters`）—— 按本表英文列的口径
                //   照抄原文（含原版自己的语病），⛔ 不再用我们那句（`Shell/ShopWindow.cs` 的 `TxtEmpty`
                //   用的一直是原文那一串，现在两处同一串了）。
                var wt = Text(ew, Loc.T("MenuCollection/NoDecksFound"), 165.88f, 1970.01f, 70.94f, 1080f, 5, PageInk,
                              "Warning", 36f);
                if (wt != null) wt.SetRenderQueue(QPageText);
                _deckEmpty = ew;
            }
            RefreshDeckEmpty();
            // 🆕 2026-10-17（`A1642`）：`Deck Filters` 挂在 **`Collection Display`** 之下（原版父链逐级现读）
            BuildDeckFilterDrawer(disp);

            // 🔴 **`Header` 必须是 `Select Deck Tab` 的【最后一个孩子】**（原版 `m_Children` 序 =
            //   `Decks Tab` → `Select Game Mode Deck Tab` → **`Header`**；本窗不建中间那一项）。
            //   ⚠️ **不能靠「调整 `CollectionTabPage.Setup()` 里的调用次序」** —— `BuildDeckHeader` 必须排在
            //   `BuildDeckList` **之前**（页头要先把 `_hdrFltBtn[0]` 等引用建出来，`RefreshFilterToggle`
            //   那条兜底才读得到；见 `CollectionWindow.Build()` 里「①c 收尾刷」那段注释），
            //   而且那个文件**不在本批白名单**里 ⇒ 在这里显式挪序。
            var hdr = page.Find("Header");
            if (hdr != null) hdr.SetAsLastSibling();
        }

        // ============================================================ Deck 页的左抽屉 `Deck Filters`（2026-09-24）
        //
        // 出处（**直读**，不是推的）：正本 §三 + `menu_rect.py bundle_menus_assets_all "Deck Filters" --depth 6`
        //   · `Deck Filters` **0.06,155.94 → 335.56,1080**（335.50 × 924.06）
        //   · 兄弟 `Shadow` 宽 **153.01**
        //   · 里面 `Filters`(LayoutGroup) = `Deck Name Filter`(335.50×80) + `Army Filter`(335.50×345)
        //     — `Deck Name Filter/Input Field` **281.28×40 @(27.17,175.94)**，尾部一个 35×30 的 `Image`
        //       （Cards 页同族那份是 `40k_icon_search` 放大镜）
        //     — `Army Filter` = `Title`(310.50×50) + `Content`(GridLayoutGroup)，格模板 **100×100**
        // ⚠️ **两个 LayoutGroup 的 spacing/pad 没读**（`Filters` 与 `Content` 都是布局组，
        //    `menu_rect` 给的是**布局跑之前的模板位**）⇒ 这里照 **Cards 页 Army 那一行**（同族、已实读）：
        //    cell 100×100 · spacing 7/0 · pad 14（**3 格一行**）。
        // ✅ 起手**收起** = **照原版**（不是「我们挑的」）—— 🆕 2026-10-05（A101）升级，判据两条：
        //    ① 原版这一页的 `Deck Filters` 的 `m_IsActive` **读到了**（= **`T`**；实读表见
        //       `资料/普查产出_1005/块8_卡组窗断言与异画页查证.md` 的 Deck 那一行）；
        //    ② 起手态与 Cards 页**同一套** —— `CollectionTab<object>$$Setup → CollectionDisplay<object>$$Initialize
        //       → filters.SetupFilters()`（VA 反汇编：调用点 `0x1815EC278` · 本体 `0x1815F0740` 收尾
        //       `anchoredPosition = (hiddenPosition.x, originalAnchorPosition.y)`）⇒ 停在**收起**那一头；
        //       之后开合**唯一**跟页头那颗 `Filter Toggle` 的 bool（入口就是它）。
        //    ⚠️ **2026-10-05 更正（铁律 5）**：原文写「原版这一页的 `Deck Filters` **出厂 `act=?` 没读到** …
        //       **这是我们挑的**」—— 现在有判据了 ⇒ 升级为「照原版」。🔴 措辞别写成「出厂展开」：
        //       **`act = T` 只等于「节点启用」**，推不出抽屉停在哪一头（判据 → `资料/已知的坑.md` 2026-10-05 那节）。
        // ⚠️ `Deck Name Filter` 那条输入：批处理里没有键盘，走 `PointerLayer.BeginText`（Play 里能敲）。
        const float DfltL = 0.06f, DfltT = 155.94f, DfltR = 335.56f, DfltB = 1080f, DfltShadowW = 153.01f;
        const float DNameT = 155.94f, DNameH = 80f;
        const float DInputL = 27.17f, DInputT = 175.94f, DInputW = 281.28f, DInputH = 40f;
        const float DArmyT = 235.94f, DArmyH = 345f;
        const float DTitleL = 25.06f, DTitleT = 240.94f, DTitleW = 310.50f, DTitleH = 50f;
        const float DCellT = 300.94f, DCellS = 100f, DCellGap = 7f, DCellPad = 14f;

        public static readonly PxRect DeckFltView = new PxRect(DfltL, DfltT, DfltR, DfltB);

        Transform _deckFltPanel;
        Label _deckFltNameTx;
        /// <summary>🆕 A11：Deck 页那一列也走**同一套滑动**（`ApplyDrawerSlide`）。它原来不是 `FilterPanel`
        /// （那份是 Cards/Styles/Cosmo 三页的模型，带 `State`/`Cells`），所以这里只给它一份**只管滑动**的壳。
        /// 判据同源：原版那几页的左栏都是 `CollectionFilterController&lt;T>`（`DeckCollectionFilterController`
        /// 也是它的子类）⇒ 同一个 `hiddenPosition = (-550,0)` + `animationTime = 0.3`。</summary>
        FilterPanel _deckFltSlide;
        /// <summary>Deck 页的筛选（**只管卡组列表**，与 Cards 页那套卡牌筛选是两回事）：空串 = 不限。</summary>
        string _deckFacFilter = "", _deckNameFilter = "";

        /// <summary>🆕 A11：读**逻辑态**（原来读 `activeSelf` —— 滑动途中节点是活着的，那个读法会把「正在合上」当「开着」）。</summary>
        public bool DeckFiltersOpen { get { return _deckFltSlide != null && _deckFltSlide.Open; } }
        public string DeckFacFilter { get { return _deckFacFilter; } }
        public string DeckNameFilter { get { return _deckNameFilter; } }
        /// <summary>自检用：画出来的卡组格数（筛选后）</summary>
        public int DeckCellCount { get { return DeckCells.Count; } }

        void BuildDeckFilterDrawer(Transform page)
        {
            var panel = Node(page, "Deck Filters", DeckFltView);
            // 兄弟序照原版：`Shadow` **先**、面板本体**后** ⇒ 面板压在影子上
            Rect(panel, "40k_main_tab_shadow", new PxRect(DfltL, DfltT, DfltL + DfltShadowW, DfltB), "Shadow", QFlt,
                 new Color(0f, 0f, 0f, 0.314f));
            Rect(panel, "40k_main_tab_background", DeckFltView, "Panel", QFlt);
            var filters = Node(panel, "Filters", DeckFltView);

            // ---- ① `Deck Name Filter` ----
            var nameRow = Node(filters, "Deck Name Filter", new PxRect(DfltL, DNameT, DfltR, DNameT + DNameH));
            var ir = new PxRect(DInputL, DInputT, DInputL + DInputW, DInputT + DInputH);
            var itex = Art("InputFieldBackground");
            if (itex != null)
            {
                // 🔴 **2026-10-06（A50③）：收口到公共件 `MenuDraw.Nine`**（等价性论证与 `BuildNameRow`
                //   那一处**同一套**，逐项同值）：矩形 `ir` / 落位 `Local(nameRow, …)`（同名同源的 `Local`）/
                //   尺寸 `LayoutSpace.Px(ir.W/H)` / 队列 `QFltRow` / tint `(0.0627, 0, 0, 1)`
                //   （与 `FilterPanelModel.InputTint` 同一个值，这里照旧写就地量）。切边 `(10,10,10,10)` · `32f,32f`
                //   原样传；公共件内部直接转调 `ImageQuad.CreateNineSlice` ⇒ 块数 / uv 切分 / 每块 `SetAspect` /
                //   `borderOutPx ?? border` 的退化**全同一条**。`clip` 一律 `null`（⛔ 不顺手改观感）。
                //   ⚠️ `ir` **别动** —— 下面 `AddHit` 还要用它；公共件只按值读 `PxRect`，不改调用方那个变量。
                MenuDraw.Nine(nameRow, itex, ir, new Vector4(10f, 10f, 10f, 10f), 32f, 32f,
                              QFltRow, new Color(0.0627f, 0f, 0f, 1f), true, "Input BG");
            }
            _deckFltNameTx = Text(nameRow, DeckNameFilterText(), DInputL + 10f, DInputL + DInputW - 45f,
                                  DInputT + 6.5f, DInputT + DInputH - 6.5f, 5, PageInk, "Input Text", 30f,
                                  // 🔴 **2026-10-10（A1212 · 块 3 #12）**：接上 `m_enableAutoSizing`（四格 = **10 / 35 / 26 / 3**）。
                                  //   判据 = `R5` §2 #12（`… ▸ Select Deck Tab ▸ Deck Filters ▸ Filters ▸
                                  //   Deck Name Filter ▸ Input Field ▸ Text Area ▸ Text`）。
                                  //   `wrapPx` = 本条自己的框宽 = `DInputW − 55`（左右各让 10 / 45）。
                                  //   ⚠️ **折行 = `3`**（`PreserveWhitespaceNoWrap`，TMP 给单行输入框的那一档：
                                  //   `TMP_InputField.SetTextComponentWrapMode()` = `multiLine ? Normal : PreserveWhitespaceNoWrap`）
                                  //   ⇒ 走本文件已有的那条常量 `SearchBoxWrap`（**同一条判据的第二处消费者**，
                                  //   另一处 = `CardNameFilter` 那颗，A249），⛔ **别用 `SetWrapping(false)` 顶替**（那是 `0`）。
                                  //   ⚠️ 闸：`wrapPx 226.28 > 0 ∧ 10 > 0 ∧ fontPx 30 > 10` 全真。
                                  wrapPx: DInputW - 55f, autoMinPx: 10f, autoMaxPx: 35f, autoBasePx: 26f);
            if (_deckFltNameTx != null)
            {
                _deckFltNameTx.SetRenderQueue(QFltText);
                _deckFltNameTx.SetWrappingMode(SearchBoxWrap);   // 原版 `m_TextWrappingMode = 3`
            }
            Rect(nameRow, "40k_icon_search", new PxRect(268.45f, 180.94f, 303.45f, 210.94f), "Search Icon",
                 QFltRow, null, true);
            AddHit(nameRow, "Hit", ir, QFltHit, BeginDeckNameFilter);

            // ---- ② `Army Filter`：Title + 13 格（3 格一行）----
            var army = Node(filters, "Army Filter", new PxRect(DfltL, DArmyT, DfltR, DArmyT + DArmyH));
            // 🔴 **2026-10-18（A891 的续）：`Army` 小标题走 `Loc.T`** —— 词条键 = 原版 prefab 上那颗
            //   `Localize.mTerm` 的原文 `MenuDeck/Filters/Army`（6 颗同键，节点名一律 `Title`；
            //   本窗这一棵 = `Collection Menu Variant > … > Decks Tab > Select Deck Tab > Deck Filters >
            //   Army Filter/Title`，**与卡牌页那一棵（`Collection Display > Card Filters > Army Filter/Title`）
            //   是两个实例、同一条 mTerm**）。英文列 = 那颗 TMP 的 `m_text` 原文 `Army`；
            //   中文列 = 「军队」(`zh_CN.csv:11`)。
            //   ⛔ 节点名 `"Title"` 不动。
            var ttl = Text(army, Loc.T("MenuDeck/Filters/Army"), DTitleL, DTitleL + DTitleW, DTitleT, DTitleT + DTitleH, 5, PageInk,
                           "Title", 36f,
                           // 🔴 **2026-10-10（A1212 · 块 3 #13）**：接上 `m_enableAutoSizing`（四格 = **10 / 36 / 32 / 1**）。
                           //   判据 = `R5` §2 #13（`… ▸ Select Deck Tab ▸ Deck Filters ▸ Filters ▸ Army Filter ▸ Title`）。
                           //   🔴 **2026-10-17（`A1207`）就地核到原版 MB**：同一份
                           //   `MonoBehaviour_4475443253947130581.json`（=`Collection Menu Variant ▸ … ▸
                           //   Select Deck Tab ▸ Decks Tab ▸ Collection Display ▸ Deck Filters ▸ Filters ▸
                           //   Army Filter ▸ Title`）里 **`m_fontSize = 36`**（`min 10 / max 36 / base 32`）
                           //   ⇒ 标称那一格原来写 32 是**抄错**（`R5` §5·2 登记的那条偏离坐实）。
                           //   折行 = `1` ⇒ 与漏斗那一档同档，不必还原。
                           // 🔴 **2026-10-17（`A1226` 表 B #13）：框 = 原版有效框 335.31 × 50（原来 310.50，窄 −24.81）。**
                           //   判据 = 原版 `…/Army Filter ▸ Title` 的 `RectTransform`（伸锚 · `m_SizeDelta.x = −25`）。
                           wrapPx: 335.31f, autoMinPx: 10f, autoMaxPx: 36f, autoBasePx: 32f);
            if (ttl != null)
            {
                ttl.SetRenderQueue(QFltText);
                // 🔴 **2026-10-17（`A1207` 同批现核·横向那一格）**：原版这颗 `Title` 是
                //   **`m_HorizontalAlignment = 1 (Left)`**（`menu_dump … "Collection Menu Variant" --rt …`
                //   现读：`Army` · 25.1,240.9→335.6,290.9 (=310.5×50，与我们 `DTitle*` 逐位相同) ·
                //   **Left/Middle** · `fs 36 / base 32 / auto[10~36] / 折行 1`）。
                //   而本窗的 `Text` 包装层默认 **Center**（`TmpFont.NewText` 写死 `TextAlignmentOptions.Center`）
                //   ⇒ 不补这一句，这段字会比原版**右移「剩余宽度的一半」**。
                //   同族先例 = 本文件 `TextAligned(…, center: false)` 那一支（`A32④` 的实读口径）。
                //   ⚠️ 纵向 `Middle` 与原版一致（`TextAlignmentOptions.Center` = MiddleCenter）⇒ 不用 `SetVAlign`。
                ttl.AlignLeftOn(LayoutSpace.FromPixel(DTitleL, 0f).x);
            }
            // 🔴 **2026-10-11（A1503 ⑤）**：改用**原版 option 序号**那一份（`DeckEditorState.ArmyOrder`）——
            //   原来这里取 `CampaignData.Armies`（= 锻造厂/战役页用的**我们自定序**）⇒ 与参考图逐格对不上。
            //   两处（建格 / 刷选中色）**必须同一份**，否则格子与名字错位。
            var armies = DeckEditorState.ArmyOrder;
            for (int i = 0; i < armies.Length; i++)
            {
                string fac = armies[i];
                float x = DCellPad + (i % 3) * (DCellS + DCellGap);
                float y = DCellT + (i / 3) * DCellS;
                var cr = new PxRect(x, y, x + DCellS, y + DCellS);
                var cell = Node(army, "Cell_fac_" + fac, cr);
                Rect(cell, DeckRuntime.FactionIcon(fac), cr, "Icon", QFltRow,
                     ToggleTint(fac == _deckFacFilter), true);
                AddHit(cell, "Hit", cr, QFltHit, () => ToggleDeckFacFilter(fac),
                       clickCue: WindowSounds.SelectToggleCue);
            }

            panel.gameObject.SetActive(false);       // 起手收起（**照原版** —— `SetupFilters` 把抽屉摆到 `hiddenPosition` 那一头，见上面那条注释）
            _deckFltPanel = panel;
            // 🆕 A11：起手收起 = 整栏停在 `hiddenPosition` 那一头（不是「只在原位隐身」）
            _deckFltSlide = new FilterPanel { Node = panel, Open = false, Slide = 0f, SlideTarget = 0f };
            // 🔴 **`force: true` 不能省**（2026-10-04 修，X3 审查的 **R4**）：这一份**不走** `RebuildFilterRows`
            //   —— 底下的格子是这里自己 `AddHit` 建的 ⇒ 少了这一下，那些 `WindowButton` 会停在
            //   **建出来的默认 `enabled=true`**，而 `Interactive` 是 false。于是 Play 里**第一次**从收起态
            //   滑出来时，`SetDrawerInteractive` 撞上「没变就不动」那条短路（`Slide` 还停在 0 ⇒
            //   `on=false` == `Interactive=false`），整个 0.3 秒里那一列的命中区**真鼠标点得到**
            //   （原来被 R1 那个 108 倍位移掩盖着 —— 抽屉飞到 −59,400px，物理上点不到；R1 一修它就现形）。
            //   另外三页由 `RebuildFilterRowsNow` 收尾那一下 `force: true` 覆盖 ⇒ 这里补上才四页齐平。
            ApplyDrawerSlide(_deckFltSlide, 0f, true);
        }

        string DeckNameFilterText()
        {
            var pl = PointerLayer.Instance;
            if (pl != null && pl.TextEditing) return pl.TextBuffer + "_";
            // 🔴 **2026-10-18（A891 的续）：占位符走 `Loc.T`** —— 词条键 = 原版那颗 `Placeholder` 的
            //   `Localize.mTerm` 原文 `MenuDeck/HUD/SearchFilter`（11 颗同键 = `Placeholder`×6 + `Button Text`×5；
            //   本窗这一棵 = `… > Decks Tab > Deck Filters > Deck Name Filter/Input Field/Text Area/Placeholder`）。
            //   英文列 = 那颗 TMP 的 `m_text` 原文 `Search`；中文列 = 「搜索」(`zh_CN.csv:10`)。
            //   ⚠️ 这与 `Core/FilterPanelModel.InputPlaceholder`（另外三页那一份）**同一条 mTerm**
            //   —— 但**不是同一个常量**：本函数是 Deck 页那一份自己的源头（CLAUDE.md §三「两处写同一条规则」
            //   那条讲的就是这一对；两处都走 `Loc.T(同一个键)` ⇒ 文案不会再分叉）。
            return string.IsNullOrEmpty(_deckNameFilter) ? Loc.T("MenuDeck/HUD/SearchFilter") : _deckNameFilter;
        }

        /// <summary>`Filters` 圆钮 / 自检：开合 Deck 页的左抽屉（**原版那颗钮开的就是它**）。</summary>
        public void ToggleDeckFilters()
        {
            if (_deckFltSlide == null || _deckFltPanel == null) return;
            bool on = !_deckFltSlide.Open;                 // 🆕 A11：与另外三页同一套（逻辑态 + 滑动）
            _deckFltSlide.Open = on;
            StartDrawerSlide(_deckFltSlide);
            if (on && _deckFltNameTx != null) _deckFltNameTx.SetText(DeckNameFilterText());
            RefreshFilterToggles();
            RefreshClearFilters();                        // 🆕 2026-10-05：页头那颗圆钮按**逻辑态**换图
            Debug.Log("[Collection] Deck 页筛选栏 " + (on ? "打开" : "收起") + "（原版 `Deck Filters`）"
                      + " · 进度 = " + _fltSlideDesc(_deckFltSlide));
        }

        /// <summary>点一格阵营格：**再点一次取消**（与 Cards 页 `$fac:` 同一条手感）。</summary>
        public void ToggleDeckFacFilter(string fac)
        {
            _deckFacFilter = _deckFacFilter == fac ? "" : (fac ?? "");
            RebuildDeckFilterCells();
            ApplyDeckFilterChanged();
        }

        /// <summary>Deck 页的 `Clear filters`（原版那颗钮：清掉本页的名字 + 阵营筛选）。</summary>
        public void ClearDeckFilters()
        {
            _deckFacFilter = ""; _deckNameFilter = "";
            RebuildDeckFilterCells();
            ApplyDeckFilterChanged();
            if (_deckFltNameTx != null) _deckFltNameTx.SetText(DeckNameFilterText());
        }

        void BeginDeckNameFilter()
        {
            var pl = PointerLayer.Instance;
            if (pl == null) { Debug.Log("[Collection] 批处理里没有 PointerLayer ⇒ 卡组名筛选敲不了字（Play 里可以）"); return; }
            string started = _deckNameFilter ?? "";
            pl.BeginText(started, 24,
                         s => { _deckNameFilter = (s ?? "").Trim(); RebuildDeckFilterCells();
                                if (_deckFltNameTx != null) _deckFltNameTx.SetText(DeckNameFilterText());
                                ApplyDeckFilterChanged(); },
                         () => { if (_deckFltNameTx != null) _deckFltNameTx.SetText(DeckNameFilterText()); },
                         s => { if (_deckFltNameTx != null) _deckFltNameTx.SetText(s + "_"); });
            Debug.Log("[Collection] 卡组名筛选：输入后回车确认，ESC 取消");
        }

        /// <summary>按当前筛选重刷阵营格的选中色（**只改颜色，不重建节点** —— 格子是固定的 13 个）。</summary>
        void RebuildDeckFilterCells()
        {
            if (_deckFltPanel == null) return;
            // 🔴 **2026-10-11（A1503 ⑤）**：改用**原版 option 序号**那一份（`DeckEditorState.ArmyOrder`）——
            //   原来这里取 `CampaignData.Armies`（= 锻造厂/战役页用的**我们自定序**）⇒ 与参考图逐格对不上。
            //   两处（建格 / 刷选中色）**必须同一份**，否则格子与名字错位。
            var armies = DeckEditorState.ArmyOrder;
            for (int i = 0; i < armies.Length; i++)
            {
                var cell = FindDeep(_deckFltPanel, "Cell_fac_" + armies[i]);
                var q = cell != null ? cell.GetComponentInChildren<ImageQuad>() : null;
                if (q != null) q.SetTint(ToggleTint(armies[i] == _deckFacFilter));
            }
        }

        /// <summary>Deck 页的卡组在当前筛选下要显示哪些（**原始下标**）。</summary>
        List<int> FilteredDeckIndices()
        {
            var list = new List<int>();
            int n = CollectionData.DeckCount();
            for (int i = 0; i < n; i++)
            {
                var d = CollectionData.DeckAt(i);
                if (!string.IsNullOrEmpty(_deckFacFilter)
                    && !string.Equals(d.Faction, _deckFacFilter, System.StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrEmpty(_deckNameFilter)
                    && (d.Name ?? "").IndexOf(_deckNameFilter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                list.Add(i);
            }
            return list;
        }

        /// <summary>改完筛选：重算滚动区 + 重画格子 + 重判空态（**只此一处**，三样一起动）。</summary>
        void ApplyDeckFilterChanged()
        {
            int cnt = FilteredDeckIndices().Count;
            int rows = Mathf.Max(1, Mathf.CeilToInt(cnt / (float)DeckCols));
            // ⚠️ **尾巴那一项也是纵向的**（原来写的是横向的 `- DeckSpacingX`）—— 2026-10-11 改的时候漏了它，
            //   内容高少 20 ⇒ 「滚到底 ⇒ 最后一格完整落进视口」那条断言当场红（实测 max=149.4 而不是 169.4）。
            float contentH = rows * (DeckCellH + DeckSpacingY) - DeckSpacingY;
            if (DeckScroll != null)
            {
                // ⚠️ `MenuScroll` 没有「改内容尺寸」的 API ⇒ 直接写它那两个**公开字段**
                //    （纵向时 `ContentX1/X2` 装的是上下两端，见那个字段的注释），再夹一次偏移
                DeckScroll.ContentX2 = DeckScroll.Viewport.y1 + contentH;
                DeckScroll.SetOffset(DeckScroll.Offset);
            }
            var page = PageRoot(0);
            // 🆕 2026-10-17（`A1642`）：`Deck Scroll View` 现在在 **两层容器之下**
            //   （`Decks Tab ▸ Collection Display ▸ Deck Scroll View`）⇒ `Transform.Find`（只找直接子件）取不到了，
            //   改走本文件已有的 `FindDeep`（递归按名字找）。
            var holder = page != null ? FindDeep(page, "Deck Scroll View") : null;
            if (holder != null) RebuildDeckCells(holder);
            RefreshDeckEmpty();
        }

        /// <summary>按名字**深度**找（`Transform.Find` 不递归；本页这几处要找的节点在两层以内）。</summary>
        static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        /// <summary>Deck 页的「一套卡组都没有」提示。🔴 判据 = **当前筛选下一套都不剩**
        /// （原版 `CollectionDisplay.RefreshCollection`：`filteredCollection.Count &lt;= 0`）。</summary>
        void RefreshDeckEmpty()
        {
            if (_deckEmpty != null) _deckEmpty.gameObject.SetActive(FilteredDeckIndices().Count <= 0);
        }

        public void RebuildDeckCells(Transform holder)
        {
            for (int i = holder.childCount - 1; i >= 0; i--)
            {
                var ch = holder.GetChild(i);
                // 🔴 **2026-10-17（`A1642`）**：`Empty Collection Warning` 现在也是 `Deck Scroll View` 的孩子
                //   （原版父链要求）⇒ **必须一起留下**。少了这一句，任何一次滚动 / 筛选 / 重画都会把
                //   空态提示连根删掉，而 `_deckEmpty` 只是**已销毁对象**的引用 ⇒ `RefreshDeckEmpty` 静默失效
                //   （画面：筛到空结果时什么都不显示；断言：`DeckEmptyShown` 取不到节点 ⇒ 那条才红）。
                if (ch.name == "Viewport" || ch.name == "Empty Collection Warning") continue;   // 视口 / 空态提示留着
                Object.DestroyImmediate(ch.gameObject);
            }
            var vp = holder.Find("Viewport");
            var parent = vp != null ? vp : holder;
            for (int i = parent.childCount - 1; i >= 0; i--) Object.DestroyImmediate(parent.GetChild(i).gameObject);
            DeckCells.Clear();

            // 🔴 **2026-10-13（A435 阶段 2 · 乙 · A19）**：原来这里是一对「`Clip = DeckViewport;` → 循环 →
            //   `Clip = prevClip;`」—— **整对删掉**（状态长在 `holder/Viewport` 那颗 `ViewportClip` 上）。
            //   下面 `MenuDraw.DeckCell(this, …)` 那个重载转发的是本窗的 `Clip`（现在恒 `null`）+
            //   `ClipPad`（本窗从未设过 = 0）⇒ `Resolve` 沿 `parent` 找到同一颗节点，**逐字段同值**
            //   （节点框 = `DeckViewport` 反推、`pad` 全 0；A16 那条 ~1e-4px 浮点残差在这里同样无可见差异）。
            // 🔴 **摆位按「筛选后的序号」、身份用「原始下标」** —— 两件事分开（2026-09-24 建抽屉时改的）。
            //    筛选为空时这里自然一个都不建，`_deckEmpty` 那条提示由 `RefreshDeckEmpty` 打开。
            var idxs = FilteredDeckIndices();
            for (int p = 0; p < idxs.Count; p++)
            {
                var content = DeckCellRect(p);
                var r = DeckScroll != null ? DeckScroll.Shift(content) : content;
                if (DeckScroll != null && !DeckScroll.Intersects(r)) continue;
                DeckCells.Add(BuildDeckCell(parent, idxs[p], r));
            }
        }

        /// <summary>一格卡组。版式照 A2：根 250×405、显示 **0.9 倍**。
        /// 🔴 **2026-09-24 收口到 `MenuDraw.DeckCell`** —— 原版 `Collection Deck` 与
        /// `Deck Selection Popup` 的 `Collection Deck With Highlight` 是**同一份 prefab 几何的两个变体**
        /// （逐字段 diff 过，只差根组件的 `useSelectedHighlight`）⇒ 画法**只能有一份**。
        /// 🔴 **2026-10-11（A198③）**：改走**带 `GameWindow` 的那个重载** —— 裁切与内缩都取**本窗**的
        /// `Clip` / `ClipPad`（原版这类 mask 挂在**视口节点**上，`m_Padding` 与它成对）。
        /// ⚠️ **2026-10-13（A435 阶段 2 · 乙 · A19）就地订正**：这一句原来接着写「**零行为变化**：调用点外面
        /// `RebuildDeckCells` 已经 `Clip = DeckViewport`」—— **那一对已经从 `RebuildDeckCells` 删掉了**
        /// （裁切状态迁到 `Deck Scroll View/Viewport` 那颗 `ViewportClip` 上）。现在这个重载转发的是
        /// **`Clip == null` + `ClipPad == 0`** ⇒ `MenuDraw.DeckCell` 内部 `Resolve` 沿 `parent` 找到**那颗节点**。
        /// 仍然**零可见变化**（节点框 = `DeckViewport` 反推、`pad` 全 0 ⇒ 与旧路逐字段同值），
        /// 但理由换了：**不是「本窗设着」，而是「节点说了算」**。
        /// ⚠️ 走这一个重载的**语义**是「用本窗的那份状态（旧路）」，要「由父链节点说了算」得改走裸重载
        /// 并显式写 `clip: null`（`MenuDraw.DeckCell` 那两处注释里有这条订正）。</summary>
        Transform BuildDeckCell(Transform parent, int i, PxRect r)
        {
            int idx = i;                                   // ⚠️ 闭包别捕 `i`（循环变量）
            var info = CollectionData.DeckAt(i);
            // 🆕 **2026-10-11（A1503 ①）：把 `gameMode` 传下去 —— 右下那颗【绿色双箭头】原来整层没建。**
            //   判据（只读代理 2026-10-11 现读 `bundle_menus_assets_all` 的卡组格 prefab）：
            //     `Game Mode Icon` **84.5×85.75 @作者系 (170.5, 273.7)**（= 同一份几何里 `Faction Icon`
            //     是左下那个红圆、这一颗是右下），`enabled = (icon != null)`；
            //     遭遇 = `40k_gamemode_icon_skirmish`（**青绿双箭头圆徽**）· 经典 = `40k_gamemode_icon_classic`（红枪）。
            //   🔴 **为什么现在才接**：`MenuDraw.DeckCell` 那一支**早就写好了**（`if (gameMode.HasValue) Rect(… "Game Mode Icon")`），
            //     只是**本调用点从来没传**（形参默认 `null` ⇒ 那一层静默不画）。**字段也早就有了**
            //     （`Shell/CollectionData.cs:53` 的 `DeckInfo.GameMode` ← `:113` 抄自 `PlayerDeck.GameMode`）。
            //   ⚠️ `0` 是**有意义的值**（`GameMode.Classic`），不是「没设」—— 映射表在
            //     `MenuDraw.GameModeIconFile`（0→classic · 13→skirmish · 其余→null ⇒ 整层不建，与原版同）。
            // 🔴🔴 **2026-10-11（第十七会话 · `A1525⑥`）：`selected` 恒传 `false` —— 收藏窗的卡组格原版【不画】金高亮。**
            //   判据（机器码级两关）：① 唯一的调用点是 `CollectionItem<T>.Config` 里那句
            //     **`if (useSelectedHighlight) SelectedHighlight(isSelected)`**（`script.json` 的
            //     `CollectionItem<object>$$Config` @ RVA `0x15F1B10`：`cmp byte ptr [rbx+0x30],0` → `call [rax+0x228]`，
            //     `+0x30` = 基类 `protected bool useSelectedHighlight`）；
            //   ② **本窗用的那一份 prefab 变体这个字段 = 0** —— `bundle_menus_assets_all` 里 `Collection Deck`
            //     （`m_GameObject` = `-2024334647938937812`，本页 `Deck Scroll View/Viewport/Content` 那一支用的就是它）
            //     的 `useSelectedHighlight` = **0**，而弹窗那一份 `Collection Deck With Highlight` = **1**
            //     ⇒ **本窗永远不会调 `SelectedHighlight`** ⇒ 那一格**本来就没有金框**。
            //   旁证：两张用户实拍（`收藏—卡组的参考图.png` 4 格 · `更换卡组的参考.png` 12 格）**没有一格是金的**。
            //   🔴 原来这里传的是 `i == CollectionData.CurrentIndex()`（**自加的**：原版 `SelectedHighlight` 只在
            //     `useSelectedHighlight=1` 的变体上才有效）⇒ 实拍里当前那一套被一整片金环糊住（`A1525⑥` 的现象）。
            //   ⚠️ 「当前是哪一套」这件事在**本页没有别处用到**（只有这里喂过 `selected`；`CollectionData.Select`
            //     那条链照旧）；高亮那一支**照旧留给弹窗**（`Shell/DeckSelectionPopup.cs:731` 传的才是真值）。
            return MenuDraw.DeckCell(this, parent, "CollectionDeck_" + i, r, info,
                                     false,
                                     QPageRow,
                                     // 🆕 **2026-10-17（A1529①）**：`qBelow` = 选中金环那一档（原版 `Highlight`
                                     //   是 `Deck` 的 child[0] ⇒ 必须在 `q` **之下**）。本页 `selected` 恒 `false`
                                     //   （`useSelectedHighlight = 0`，见上）⇒ 这一档今天**不画**；取 `QPageSdf`
                                     //   （= 3031，梯子上 `QPageRow` 的下一格）只为**把形参显式表态**（⛔ 不留缺省值：
                                     //   缺省 = 下一个调用点静默把金环画到最顶，A1529 就是这么来的）。
                                     QPageSdf,
                                     QPageText, QPageOverlay, QPageRow,
                                     () => SelectDeck(idx),
                                     gameMode: info.GameMode);   // `DeckInfo` 是 **struct** ⇒ 没有 null 那一档
        }

        /// <summary>点一格卡组：**选中 + 开 `Deck info Popup`**（2026-09-23 起 —— 那扇窗建好了）。
        /// 原版就是这条：点格 ⇒ 选中，由窗里的 `Edit Deck` 才进编辑。
        /// 🔴 **2026-10-13（A600）**：`CollectionData.Select` 的返回值现在**要读** —— 它内部那次落盘失败会让
        /// 「你选的那一套」**切场景后失效**（`BattleDriver.PickSavedDeck` 从磁盘重读 ⇒ 战斗拿旧的那套）。
        /// ⚠️ 下面那趟重建**照旧无条件跑**：内存里的选中态未必没变（落盘失败那一支**是**变了的），
        /// 而且格子高亮读的就是 `CurrentIndex()`（见 `BuildDeckCell` 的 `i == CollectionData.CurrentIndex()`）。</summary>
        public void SelectDeck(int i)
        {
            if (i != CollectionData.CurrentIndex())
            {
                if (!CollectionData.Select(i))
                    Debug.LogWarning("[Collection] 选中第 " + i + " 套失败：" + CollectionData.LastSelectError);
                var pr = PageRoot(0);
                // 🆕 2026-10-17（`A1642`）：两层容器之下 ⇒ 改递归找（同 `ApplyDeckFilterChanged` 那条注释）
                var holder = pr != null ? FindDeep(pr, "Deck Scroll View") : null;
                if (holder != null) RebuildDeckCells(holder);
            }
            OpenDeckInfo(i);
        }

        /// <summary>开 `CardDetailPopup`（= 原版菜单版 `CardDisplayWindow`）。
        /// **两处入口共用这一份**（CLAUDE.md §三）：Cards 页点一张卡（`CardCollectionTab.OnItemSelected`
        /// → `CardDisplayWindow.Instance.ShowCard(card,…)`）与 Styles 页点一张异画（`AlternateArtCardCollectionTab` 同一条）。</summary>
        public CardDetailPopup OpenCardDetail(CardDef card)
        {
            if (card == null) { Debug.LogWarning("[Collection] 没有卡可展示"); return null; }
            if (Manager == null)
            {
                Debug.LogWarning("[Collection] 没有 `WindowsManager`，开不了卡片详情窗");
                return null;
            }
            var w = CardDetailPopup.Create(Manager);
            Manager.OpenWindow(w);
            w.ShowCard(card);                 // 原版 `ShowCard` 复用同一个窗（不新建）
            LastCardDetail = w;
            Debug.Log("[Collection] 开卡片详情窗：「" + card.Name + "」");
            return w;
        }

        /// <summary>最近一次开出来的卡片详情窗（自检用）。</summary>
        public static CardDetailPopup LastCardDetail;

        /// <summary>最近一次开出来的 `Deck info Popup`（自检用）。</summary>
        public static DeckInfoPopup LastOpened;

        /// <summary>最近一次开出来的 `Import Deck Popup`（自检用）。</summary>
        public static ImportDeckPopup LastImport;

        /// <summary>开 `Import Deck Popup`（Deck 页那个 `Import` 钮走它；自检也直调）。</summary>
        public ImportDeckPopup OpenImportPopup()
        {
            LastImport = null;
            if (Manager == null) { Debug.Log("[Collection] 没有 `WindowsManager`，开不了 `Import Deck Popup`"); return null; }
            var w = ImportDeckPopup.Create(Manager, RebuildDeckListNow);
            Manager.OpenWindow(w);
            LastImport = w;
            Debug.Log("[Collection] 开 `Import Deck Popup`");
            return w;
        }

        /// <summary>重画 Deck 页的卡组列表（导入成功后调）。</summary>
        public void RebuildDeckListNow()
        {
            var pr = PageRoot(0);
            // 🆕 2026-10-17（`A1642`）：同上 —— `Deck Scroll View` 在两层容器之下，递归找
            var holder = pr != null ? FindDeep(pr, "Deck Scroll View") : null;
            if (holder != null) RebuildDeckCells(holder);
        }

        /// <summary>开 `Deck info Popup`（自检也直调它）。</summary>
        public DeckInfoPopup OpenDeckInfo(int i)
        {
            LastOpened = null;
            if (Manager == null) { Debug.Log("[Collection] 没有 `WindowsManager`，开不了 `Deck info Popup`"); return null; }
            var w = DeckInfoPopup.Create(Manager, i);
            Manager.OpenWindow(w);
            LastOpened = w;
            Debug.Log("[Collection] 开 `Deck info Popup`：「" + CollectionData.DeckAt(i).Name + "」");
            return w;
        }

        /// <summary>进编辑。⚠️ **这是相对原版的一处偏离**：原版是**在收藏窗【之上】再开一扇窗**、离场 = **关窗**
        ///    （入口 = `d:/2/tools/decomp_full/SelectDecksTab__CreateDeck.c:28` 的 `WindowsManager__OpenWindow&lt;…&gt;`；
        ///     离场 = `DeckEditingWindow__Close.c:19-20` 的 `NavigationPanelController__Toggle` + `GameWindow__Close`
        ///     ⇒ **页签从未被切过**，关窗露出的就是下面那扇一直开着的 `CollectionScreen`），
        ///    而我们的编辑器早已建成**独立场景**（200 条自检 + 自带的坐标层），嵌进窗里要重写它的 `Pos/ToPx`
        ///    ⇒ 先用「切场景」把闭环打通。
        ///    ⚠️ **批处理下不切场景**（自检要靠同一个进程跑完）⇒ 自检验的就是「交接下标对不对」。
        ///    🔴 **2026-10-18（A855）就地订正（铁律 5）**：上面那句原来写「原版**在同一扇窗里换页**」——
        ///      **不准确**，原版是「**叠窗 + 关窗**」（判据 = 上面那两处 `decomp_full` 出处）。方向不变
        ///      （我们仍是独立场景、仍要切场景），改的只是**对原版形状的描述**。
        ///    🔴 **2026-10-18（A855）补记：闭环已经合上了** —— 回程 = `DeckRuntime.BackToMenu` **只切场景**，
        ///      由到达端 `Shell/MainMenuRuntime.cs` 的 `Build` 按 `CollectionData.PendingReturn` **重开本窗的卡组页**，
        ///      并在开窗**之前**作废 `CollectionData` 的库缓存（否则列表画的还是编辑前那一份；
        ///      判据与真根因 → `CollectionData.InvalidateLibrary`）。
        ///      ⚠️ **还开着的那一半 = 载入模式**（要**做**，⛔ 不是「影响小、不做」那一类）：把它做成
        ///      **additive**（编辑器叠在窗上、主菜单相机让位）—— 判据已经齐了（「叠窗 + 关窗」，见上面两处出处），
        ///      做法 = `LoadScene(…, LoadSceneMode.Additive)` + `SetActiveScene` + 显式关掉一台 `MainCamera`
        ///      （现成写法 → `Shell/ShellRuntime.cs` 切场景前那句 `Camera.main.enabled = false`）。
        ///      ⚠️ **本波范围外**（简报明写「本波只做『作废缓存』那一环」）⇒ 账记在这里，别当成已经做完。</summary>
        public void EditDeck(int i) { GoEdit(i); }

        /// <summary>「进编辑」的**落点**（三处入口共用一张表 —— CLAUDE.md §三「两处写同一条规则 = 迟早不一致」）。</summary>
        public enum DeckEntryRoute
        {
            /// <summary>`Application.isBatchMode` ⇒ **既不切场景、也不开窗**，只把交接值留在静态口上
            /// （自检靠它观测）。⚠️ **这是刻意保留的兜底，⛔ 不是「还没做」** ——
            /// 见 `RouteDeckEntry` 的 doc：改它会把自检既有观测点一起动掉。</summary>
            BatchSkip,
            /// <summary>有壳 ⇒ **开卡组编辑窗**（第 3 步起的生产落点 = 原版 `SelectDecksTab__CreateDeck.c:28`
            /// 的 `WindowsManager__OpenWindow&lt;DeckEditingWindow&gt;` 那一跳）。</summary>
            OpenWindow,
            /// <summary>没壳（直起 `-wfscene DeckEditor`）⇒ 老路：切 `DeckEditor` 场景
            /// （`Shell/PlayerBoot.LoadSceneMatching` 按名字找它）。</summary>
            LoadScene,
        }

        /// <summary>🆕 **2026-10-13（`A1132` 第 3 步）**：**唯一的落点判定** —— 三处入口
        /// （`GoEdit` / `GoNewDeck` / `Shell/LiveOpsEventWindow.CreateDeckInMode`）的 `switch` 本体都只有一处。
        ///
        /// <para>两个入参就是生产现场那两个真值：`batch` = `Application.isBatchMode`、
        /// `hasShell` = `WindowsManager.Instance != null`（**读它们的那两行各只有一处**，见三个入口的单参重载）。</para>
        ///
        /// <para>🔴 **为什么抽成一个可传参的纯函数**：批处理下那道 `Application.isBatchMode` 闸会让三个入口
        /// **永远**停在 `BatchSkip`（= 与改前逐位一致、**有意保留**；直起 `-wfscene DeckEditor` 也走它）
        /// ⇒「入口 → **开窗那一跳**」在自检里**永远观测不到**。抽成纯函数之后，
        /// 「真值 → 落点」那张表可以直接断，而 **生产路径读的仍是现场那两个值**
        /// （⛔ 没有任何缝能让它绕过 —— 见各入口单参重载那一行）。</para></summary>
        public static DeckEntryRoute RouteDeckEntry(bool batch, bool hasShell)
        {
            if (batch) return DeckEntryRoute.BatchSkip;
            return hasShell ? DeckEntryRoute.OpenWindow : DeckEntryRoute.LoadScene;
        }

        /// <summary>「进编辑」的**唯一实现** —— 收藏窗那条路与 `Deck info Popup` 的 `Edit Deck` 钮**都走它**
        /// （CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
        ///
        /// <para>🆕 **2026-10-13（`A1132` 第 3 步）**：落点从「一律切场景」改成**按现场判定**
        /// （原版就**是**开窗：`SelectDecksTab__CreateDeck.c:28`）。</para></summary>
        public static void GoEdit(int i)
            => GoEdit(i, WindowsManager.Instance != null, Application.isBatchMode);   // ⛔ 生产只看这一行：读现场那两个真值

        /// <summary>🆕 **2026-10-13（`A1132` 第 3 步）**：两个入参 = 上面那一行从现场读到的两个真值。
        ///
        /// <para>🔴 **它是【自检缝】、⛔ 不是第二条生产路**：**生产路径**上被调用的只有单参那个重载
        /// （`GoEdit(int)` / `GoNewDeck()` / `LiveOpsEventWindow.CreateDeckInMode()`）——
        /// 全仓调用点 = `EditDeck` · `DeckInfoPopup` 的 `Edit Deck` · `CollectionWindow.CreateDeck` 三处，
        /// **都走单参那一版**（可 `grep "GoEdit("` / `"GoNewDeck()"` / `"CreateDeckInMode()"` 复核）。
        /// ⚠️ 这条缝**只喂两个判定值**，唯一用途 = 让自检能走到「批处理那道闸挡住的那一跳」。</para>
        ///
        /// <para>⚠️ **缝能伪造的只有「判定输进来的两个值」，⛔ 伪造不出一个壳** ——
        /// 开窗那一跳拿的是**现场的** `WindowsManager.Instance`（见下面 `OpenWindow` 支）：
        /// 传 `hasShell: true` 而现场真没有壳时，`DeckEditingWindow.OpenFrom(null)` 会**出声**并返回 null
        /// （⛔ 不静默开出一扇空气窗、也不偷偷 `EnsureHost()`）。</para></summary>
        public static void GoEdit(int i, bool hasShell, bool batch)
        {
            CollectionData.PendingEditDeck = i;
            // 🆕 2026-10-18（A855）：**这条来路的回程意图** —— 「按来路回」（用户 2026-10-18 拍板）：
            //   🔴 **2026-10-12（`A1697`）就地订正（铁律 5）**：本行原写「全仓仅有两处，这是**第一处**」
            //     —— **现在是三处**（第三处 = 同文件下面的 `GoNewDeck`）。本处仍是最先执行的那一处 ⇒ 来源写在这里、
            //   ⛔ **不由离场方式决定**（`DeckRuntime.BackToMenu` 那三处调用点都只是「关闭这条路上的编辑器」）。
            //   ⚠️ 写在批处理闸**之前**：批处理不切场景，自检只能靠这个意图观测（同上面那个下标）。
            CollectionData.SetReturnIntent(DeckExitSource.Collection, WindowTabType.CollectionDecks);
            Debug.Log("[Collection] 进编辑：「" + CollectionData.DeckAt(i).Name + "」"
                      + "（把下标 " + i + " 交给 `DeckRuntime`）"
                      // 🔴 **2026-10-14（`A1132` 第 4 步）就地订正（铁律 5）**：本句原文写
                      //   「返回时回主菜单场景 → 在那里重开**收藏窗的卡组页**」—— 那是**第 4 步之前**
                      //   唯一的回程（切场景 + 到达端重开窗）。现在回程有**两条腿**，落点按 `HostWindow` 分：
                      //     · 有壳（本口今天这条路）⇒ `DeckRuntime.LeaveByHostWindow`：**关本窗**、
                      //       底下这扇收藏窗自己回来（页签**从未被切过**）；
                      //     · 没壳（直起 `-wfscene DeckEditor`）⇒ 仍然是老的「切 `MainMenu` +
                      //       `MainMenuRuntime.Build` 重开收藏窗的卡组页」。
                      + "；返回时**有壳则关本窗回到这扇收藏窗** / 无壳则回主菜单场景再重开卡组页");
            // 🔴 **落点 = 上面那张表**（`RouteDeckEntry`）。三支各自的判据：
            //   · `BatchSkip` —— **必须排在最前**：批处理里自检点完这一下立刻要读 `PendingEditDeck`，
            //     而 `DeckRuntime.Build` 开局就把它**读掉并清掉** ⇒ 闸若挪到开窗之后，
            //     `Editor/CollectionScene.cs` 那条「点 `Edit Deck` ⇒ 交接的下标」当场观测不到（会红）。
            //   · `OpenWindow` —— 第 3 步的新落点（有壳）。原版就**是**开窗：`SelectDecksTab__CreateDeck.c:28`
            //     的 `WindowsManager__OpenWindow<DeckEditingWindow>(wm, deck, closeAll:false)`。
            //   · `LoadScene` —— 没壳（直起 `-wfscene DeckEditor`）那条兜底：原样保留。
            //     `Shell/PlayerBoot.LoadSceneMatching` 按名字找到 `DeckEditor` 场景才走得通。
            switch (RouteDeckEntry(batch, hasShell))
            {
                case DeckEntryRoute.BatchSkip:
                    Debug.Log("[Collection] （批处理：不切场景，只交接）");
                    return;

                case DeckEntryRoute.OpenWindow:
                    {
                        var deWin = DeckEditingWindow.OpenFrom(WindowsManager.Instance);
                        Debug.Log("[Collection] 进编辑：**开出卡组编辑窗**（" + (deWin != null ? deWin.name : "<没开出来>")
                                  + "）—— 照原版 `SelectDecksTab.CreateDeck`：**叠一扇窗**，⛔ 不切场景");
                        return;
                    }

                default:
                    Debug.Log("[Collection] 没有壳（`WindowsManager.Instance == null`）⇒ 退回老路：切 `DeckEditor` 场景");
                    UnityEngine.SceneManagement.SceneManager.LoadScene("DeckEditor");
                    return;
            }
        }

        /// <summary>「新建卡组 → 进编辑」的**唯一实现**（收藏窗那颗 `Create` 走它）——
        /// 与 `GoEdit` 是**两条路**：那条交接的是**库里某一套的下标**，这条交接的是
        /// **一份还没入库的新卡组**（`CollectionData.PendingNewDeck`，由 `CollectionData.CreateDeck` 写好）。
        ///
        /// <para>🔴 **2026-10-12（`A1697`）新开**，修的是诊断 §六 的第二处偏离：我们那颗 `Create`
        /// **建完不进编辑器**（只重建列表），而原版 `SelectDecksTab__CreateDeck.c:28` 是**直接开窗**
        /// （`WindowsManager__OpenWindow&lt;DeckEditingWindow&gt;(…)`）⇒ 本口把那一步补回来。</para>
        ///
        /// <para>⚠️ **本口不写 `PendingEditDeck`** —— 新卡组在库里没有下标；而 `CollectionData.CreateDeck`
        /// 那一拍**已经把它清成 `-1`**（两个交接口互斥，判据 → 那个字段的 doc）。</para>
        ///
        /// <para>⚠️ **批处理下不切场景**（自检要靠同一个进程跑完）⇒ 自检验的就是「交接那份卡组在不在」；
        /// 同 `GoEdit`：回程意图写在 `isBatchMode` 那道闸**之前**。</para></summary>
        public static void GoNewDeck()
            => GoNewDeck(WindowsManager.Instance != null, Application.isBatchMode);   // ⛔ 生产只看这一行

        /// <summary>🆕 **2026-10-13（`A1132` 第 3 步）**：与 `GoEdit(int, bool, bool)` **同一个形状、同一张表**
        /// （`RouteDeckEntry`）—— 判据、「生产只走单参那一版」、以及「缝伪造不出一个壳」
        /// → `GoEdit` 那一段，⛔ 别在这儿抄第二份。
        /// ⚠️ 本口交接的是 `PendingNewDeck`（**未入库**那一份），同样会被窗里 `Build` 开局读掉。</summary>
        public static void GoNewDeck(bool hasShell, bool batch)
        {
            // 🆕 2026-10-18（A855）：这条来路的回程意图 —— 与 `GoEdit` 同一口径（「按来路回」）。
            //   ⚠️ 全仓 `LoadScene("DeckEditor")` 原为**两处**，本口是**第三处**（同在收藏窗那一条来路里）。
            CollectionData.SetReturnIntent(DeckExitSource.Collection, WindowTabType.CollectionDecks);
            switch (RouteDeckEntry(batch, hasShell))
            {
                case DeckEntryRoute.BatchSkip:
                    Debug.Log("[Collection] （批处理：不切场景，只交接那份**未入库**的新卡组）");
                    return;

                case DeckEntryRoute.OpenWindow:
                    {
                        var deWin = DeckEditingWindow.OpenFrom(WindowsManager.Instance);
                        Debug.Log("[Collection] 新建卡组 ⇒ **开出卡组编辑窗**（" + (deWin != null ? deWin.name : "<没开出来>")
                                  + "）—— 照原版 `SelectDecksTab.CreateDeck`：**叠一扇窗**，⛔ 不切场景");
                        return;
                    }

                default:
                    Debug.Log("[Collection] 没有壳（`WindowsManager.Instance == null`）⇒ 退回老路：切 `DeckEditor` 场景");
                    UnityEngine.SceneManagement.SceneManager.LoadScene("DeckEditor");
                    return;
            }
        }

        /// <summary>点 `Create` 那颗钮：**造一副不入库的空卡组 → 直接进卡组编辑**（照原版
        /// `SelectDecksTab__CreateDeck`，判据逐行 → `CollectionData.CreateDeck` 的注释）。
        ///
        /// <para>🔴 **2026-10-12（`A1697`）改向（铁律 5：原文留痕）**：本方法原来做的是
        /// 「建一套空卡组（**当场入库并落盘**）+ 重建卡组列表」，还带一条 A549① 加的空串警告分支。
        /// 现在照原版改成**两件事**：① `CollectionData.CreateDeck()` **只造不入库**（库里此刻**一套都没多**）；
        /// ② **紧接着进编辑器**（原版 `SelectDecksTab__CreateDeck.c:28` 那一句开窗）。
        /// ⇒ 那句空串警告的**成因已经换了**：不再是「没写进存档」（本口一个字节都不写盘），
        /// 而是「压根没造出来」（今天不可达 —— 判据 → `CollectionData.CreateDeck` 那条订正）。
        /// ⛔ 那条分支**留着**（红线「不许静默失败」：万一将来重名/词条那一跳出问题，控制台要看得见）。</para>
        ///
        /// <para>⚠️ **`RebuildDeckCells` 这一跳没有了**：原来重建是因为「库里刚多了一套」；
        /// 现在库里没变（Done 那一刻才入库）⇒ 重建是空做。回程时那一跳由 `MainMenuRuntime.Build`
        /// 作废库缓存后重开本窗来做（A855 那条链）。</para></summary>
        public void CreateDeck()
        {
            string name = CollectionData.CreateDeck();
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("[Collection] 新建卡组**没造出来**（⛔ 库里一套都没多、也没进编辑器）"
                                 + " —— 原因见上一条 `[CollectionData]` 警告，⛔ 别在这里另写一套原因");
                return;
            }
            Debug.Log("[Collection] 新建卡组「" + name + "」—— 照原版 `SelectDecksTab__CreateDeck.c:28`："
                      + "**不入库**、直接开卡组编辑（`Done` 过了校验才入库）");
            GoNewDeck();
        }

        public string Dump()
        {
            return "Collection：页 " + CurrentTab + " · " + CollectionData.DeckCount() + " 套"
                   + " · 当前第 " + (CollectionData.CurrentIndex() + 1) + " 套"
                   + " · 列表滚动 " + (DeckScroll != null ? DeckScroll.Offset.ToString("F0") : "-") + "px"
                   + " · 画出的格 " + DeckCells.Count;
        }

        // ============================================================ 🆕 2026-10-12（`A1628`）开窗分段计时

        /// <summary>**开收藏窗的分段计时探针**（临时诊断件 —— 查到那一跳之后可以把 `Mark`/`Begin` 调用点删掉，
        /// 但 ⛔ **删之前先把结论写进账**）。
        ///
        /// <para>🔴 **它要回答的问题**（用户 2026-10-12 真机实测）：**点「收藏」⇒ 整个 player 假死约 50 秒**。
        /// 实测读数：点完 `Get-Process WarpforgePlayer` 读到 **`Responding: False`**、工作集顶到 **1.8 GB**；
        /// **约 50 秒后**才回 `True`（0.9 GB）；期间**三张截图逐像素一致**（画面完全不动）；
        /// **不是崩溃**（进程活着）。实拍 → `_tmp_view/s21_04_collection.png` / `s21_01_collection.png`。</para>
        ///
        /// <para>🔴 **已经排除的（别重复查）**：`ClickLog` 是**同帧**捕获（`Begin` → 帧末 `Driver.LateUpdate`
        /// → `End`，`Core/ClickLog.cs`）⇒ 那一次点击记下来的 7 行日志**就是这一帧的全部日志**，
        /// 而 `_tmp_view/click_log.txt` 里**这一条的时间戳 = `12:12:36.653` = 帧末** ——
        /// 与「点击发生在 ~12:11:46、50 秒后画面才动」逐位吻合（截图 mtime `s21_01`~`s21_03` 都停在同一帧）
        /// ⇒ **那 50 秒花在【点击那一帧里】，不是后续几帧**。
        /// 最后一行为 `[CardView] 卡背 SDF：…`，它全仓只有**两个**调用点：`Battle/BattleDriver.cs:13948`（本局没打到）
        /// 与 `Shell/CollectionWindow.cs` 的 `RebuildCosmoCells`（**卡背页第 1 格**）⇒ **那一行之后直到帧尾
        /// 一条日志都没有**，所以嫌疑只在「卡背页循环剩下的部分 → 异画页 → 落页 → **本帧的渲染**」这段里。</para>
        ///
        /// <para>🔴 **两条头号嫌疑（本探针就是为分开它们而加）**：
        /// ① `Resources.LoadAll&lt;Texture2D&gt;("Art/cardbacks")`（在 `CosmoTotal` 里，`CardArt.CosmeticNames`）——
        ///    打进包里的真值（本机 UnityPy 现读 `player/WarpforgePlayer_Data/resources.assets`）：
        ///    **466 张 · `m_CompleteImageSize` 合计 610.1 MB · 格式全是 `4`(RGBA32 未压缩)**；
        /// ② **本帧第一次渲染**新建的那批材质/贴图 —— 其中 `Everguild/FX/Card Highlight And Shadow`
        ///    是**从原版 bundle 运行时加载**的 shader（本仓已知：bundle 里的 shader 落不了工程资产、
        ///    只能运行时加载），它在本进程里**到这一帧才第一次被画**。`WaitForEndOfFrame` 那一格量的就是它。</para>
        ///
        /// <para>⚠️ **只在真 Play 里出声**（`Application.isPlaying`）⇒ 批处理自检的日志一字不变
        /// （那几条数日志条数的断言因此不受影响）。</para></summary>
        internal static class BuildWatch
        {
            static readonly List<string> _marks = new List<string>();
            static float _t0;
            static string _title;

            /// <summary>记一格：`what` 后面跟「从上一格到这一格」的毫秒数。</summary>
            public static void Mark(string what)
            {
                if (!Application.isPlaying) return;
                float now = Time.realtimeSinceStartup;
                _marks.Add($"    {what}：**{(now - _t0) * 1000f:F1} ms**");
                _t0 = now;
            }
            public static void Begin()
            {
                if (!Application.isPlaying) return;
                _marks.Clear();
                _t0 = Time.realtimeSinceStartup;
            }

            /// <summary>把「从点击到**本帧渲染完**」也记成一格 —— 这一格含 render，是 ② 号嫌疑唯一的量法。</summary>
            public static System.Collections.IEnumerator ReportAtFrameEnd(string title)
            {
                yield return new WaitForEndOfFrame();
                _title = title;
                Mark("③ 帧尾（**含本帧渲染** —— 新建材质/贴图的首次 GPU 上传 + shader 首次编译都在这儿）");
                Dump();
            }

            // ============================================================ 🆕 2026-10-12（`A1628` 第二轮）
            // **逐格相位累计器** —— 上面那 8 个格子只分开了「**哪一页**」，于是第二轮实测把根因从
            // 「整池 1126 张」改成了「**每建一张卡 ≈ 2.2 秒**」（页 1 = 18 格 41531.6ms · 页 3 = 6 格 13011.7ms；
            // 两页**都已经**只建视口内的格，自检 `Editor/CollectionScene.cs:3883` / `:6870` 逐条断着 18 / 6）。
            // 本器接着往下切一层：把**一格**拆成 5 段累计（18 格 × 5 段 ⇒ 压成 5 行），
            // 下一趟真机就能指名「那 2.2 秒在 `CardView.Create` / 两次重裁 / 宿主那两件」里的哪一段。
            // ⚠️ **与 `Mark` 共用 `Application.isPlaying` 那道闸** ⇒ 批处理自检的日志一字不变。
            static readonly List<string> _phName = new List<string>();
            static readonly List<double> _phMs = new List<double>();
            static int _phCells;
            static float _phT;

            /// <summary>开始量一页的格子（清账）。</summary>
            public static void CellsBegin()
            {
                if (!Application.isPlaying) return;
                _phName.Clear(); _phMs.Clear(); _phCells = 0;
            }
            /// <summary>一格开量。</summary>
            public static void CellBegin() { if (!Application.isPlaying) return; _phT = Time.realtimeSinceStartup; }
            /// <summary>记一段：`what` 后面累加「上一段到这一段」的毫秒。</summary>
            public static void CellPhase(string what)
            {
                if (!Application.isPlaying) return;
                float now = Time.realtimeSinceStartup;
                int i = _phName.IndexOf(what);
                if (i < 0) { _phName.Add(what); _phMs.Add(0.0); i = _phName.Count - 1; }
                _phMs[i] += (now - _phT) * 1000.0;
                _phT = now;
            }
            public static void CellEnd() { if (!Application.isPlaying) return; _phCells++; }

            /// <summary>把这一页的逐格相位打出来。**一格都没建就不出声**（页被整块裁掉 / 还没进这一页）。</summary>
            public static void CellsReport(string title)
            {
                if (!Application.isPlaying) return;
                if (_phCells <= 0) return;
                var sb = new System.Text.StringBuilder();
                sb.Append("[Collection·计时] `").Append(title).Append("` **逐格相位**（").Append(_phCells)
                  .Append(" 格 · 逐段累加；单位 ms）：\n");
                for (int i = 0; i < _phName.Count; i++)
                    sb.Append("    ").Append(_phName[i]).Append("：**").Append(_phMs[i].ToString("F1"))
                      .Append(" ms**（均 ").Append((_phMs[i] / _phCells).ToString("F1")).Append(" / 格）\n");
                sb.Append(FontReadout());
                Debug.Log(sb.ToString());
            }

            /// <summary>中文字体资产那一格读数 —— 用来判「每段 TMP 重排都在现生成字形」这条**假设**
            /// （本仓唯一字体 `NotoSerifCJK-Regular SDF`：`m_AtlasPopulationMode = 1`(Dynamic)、
            /// **图集是 1×1 占位** ⇒ 每个新字形都得从 TTF 现烘。出处 = `Core/TmpFont.cs:27-28`）。
            /// 🔴 **它只是读数，不是结论** —— 读出 `1×1` **不证明**它就是那 2.2 秒（那要先量单次
            /// `ForceMeshUpdate` 的耗时）；读不到就如实写「读不到」，⛔ 不猜。</summary>
            /// <summary>量**一次 `ForceMeshUpdate` 有多贵** —— 本账最要紧的那个数，因为整张卡的
            /// 2.2 秒如果按「每段文字重排 × 次数」摊开，只有这一个数能把它对上或推翻。
            ///
            /// <para>**为什么怀疑它**：本仓**只有一个字体**（`NotoSerifCJK-Regular SDF`），而它
            /// `m_AtlasPopulationMode = 1`(Dynamic)、**图集是 1×1 占位**（出处 `Core/TmpFont.cs:27-28`）
            /// ⇒ 每个字形都得从 TTF 现烘。建一张卡要走的文字重排是**几十次**
            /// （`FitToBox` 的 8 步二分每步一次 · `FitToWidth` 两次 · `SetClipFromTree` 那次裁切逐字一次），
            /// 若单次是 10ms 量级，2.2 秒就对上了；若单次是 0.2ms，那这条假设**当场被推翻**
            /// （⛔ 那时别再往字体上查，改去看贴图/材质的首次加载）。</summary>
            /// <para>⚠️ **量完必须把裁切重新贴回去**：`ForceMeshUpdate` 会把我们写进顶点的那一刀
            /// **冲掉**（这正是 `CardView.RestoreClip` 靠它还原原件的那条性质）⇒ 不补一句
            /// `SetClipFromTree()` 的话，被量的那张卡**文字会露出视口外**（真缺陷）。
            /// 补那一句之后画面与「不量这一下」逐位相同。</para>
            /// <para>⚠️ 与其余探针同闸：**只在真 Play 出声**。</para>
            public static void TimeOneTextRelayout(Transform cardRoot)
            {
                if (!Application.isPlaying || cardRoot == null) return;
                try
                {
                    var tmps = cardRoot.GetComponentsInChildren<TMPro.TextMeshPro>(true);
                    if (tmps == null || tmps.Length == 0)
                    {
                        Debug.Log("[Collection·计时] 单次文字重排：**量不到**（这张卡下没有激活的 `TextMeshPro`）");
                        return;
                    }
                    var t = tmps[0];
                    float a = Time.realtimeSinceStartup;
                    t.ForceMeshUpdate();
                    float b = Time.realtimeSinceStartup;
                    t.ForceMeshUpdate();
                    float c = Time.realtimeSinceStartup;
                    Debug.Log("[Collection·计时] 单次文字重排（`" + t.name + "`，第 1 张卡）：第 1 次 **"
                              + ((b - a) * 1000f).ToString("F1") + " ms** · 第 2 次 **"
                              + ((c - b) * 1000f).ToString("F1") + " ms**"
                              + "（⚠️ 两次差得大 ⇒ 第 1 次含**字形现生成**；两次都小 ⇒ 「字体现烘」那条假设**不成立**，"
                              + "别往字体上查）。⚠️ 本行之后已把那张卡的裁切补回（见本方法的 doc）。");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[Collection·计时] 单次文字重排量不到：" + e.GetType().Name + "：" + e.Message);
                }
                var cv = cardRoot.GetComponent<CardView>();
                if (cv != null) cv.SetClipFromTree();          // 把上面那一下冲掉的裁切补回来
            }

            static string FontReadout()
            {
                try
                {
                    var f = TmpFont.Font;
                    if (f == null) return "    · 字体资产读数：**取不到**（`TmpFont.Font == null`）\n";
                    var at = f.atlasTextures;
                    string atlas = (at == null || at.Length == 0 || at[0] == null)
                                 ? "**没有图集**"
                                 : (at[0].width + "×" + at[0].height + "（共 " + at.Length + " 页）");
                    return "    · 字体资产读数：`" + f.name + "` · 图集 **" + atlas + "** · 模式 "
                         + f.atlasPopulationMode
                         + " · 已烘字形 " + (f.glyphTable != null ? f.glyphTable.Count : -1)
                         + " / 字符 " + (f.characterTable != null ? f.characterTable.Count : -1)
                         + "（名义 `m_AtlasWidth/Height` = " + f.atlasWidth + "×" + f.atlasHeight + "）\n";
                }
                catch (System.Exception e)
                {
                    return "    · 字体资产读数：**读不到**（" + e.GetType().Name + "）\n";
                }
            }

            static void Dump()
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("[Collection·计时] `").Append(_title).Append("` 分段（真机 `A1628` 探针；单位 ms）：\n")
                  .Append(string.Join("\n", _marks.ToArray()))
                  .Append("\n    ⚠️ **哪一格上千毫秒，50 秒就在那一格里**；" +
                          "若①b2a（`Resources.LoadAll` 466 张卡背）或③（帧尾/渲染）最大，" +
                          "对应的修法分别是「卡背页别在开窗时全量 `LoadAll`」/「SDF 那族 shader 与贴图别在这一帧才第一次用」。");
                Debug.Log(sb.ToString());
            }
        }
    }

}
