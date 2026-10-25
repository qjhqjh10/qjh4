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
//     ⇒ 直接继承 `MainMenuSubmenuWindow`。**唯一差别**：左栏**条自己**从 **158.64** 起（奖励窗/商店是 70.94），
//     条内 VLG `padTop=30` ⇒ 第一个键顶边 **188.64**。
//     🔴 **2026-10-23（W62）就地订正（铁律 5）**：原文写「覆写 `BarPadTop = 188.64 − 70.94 = 117.7`」——
//       那是把「条低 87.7」**折进 padTop** 的旧写法 ⇒ 条本身仍从 70.94 起、**底板比原版高 88 px**。
//       现在拆成两个口：`BarTop = 158.64`（条顶边）+ `BarPadTop = 30`（原版 VLG padTop）。
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
using DG.Tweening;     // 🆕 2026-10-23（`C3`）：卡格选中 punch 用到 `DOPunchScale` / `OnComplete` / `OnKill`
                       //   —— 这几个都是 `DG.Tweening` 里的**扩展方法**，不引这个 using 就编不过（CS1061）。
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
        /// 现在**不是**写死的 `"DECKS"` —— 它走 `Loc.T(term).ToUpperInvariant()`（`CardPresentation/Editor/CollectionScene.cs`）⇒ **本条不用再跟改**
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

        /// <summary>🆕 **2026-10-23（W62 · 界面线）**：本窗左栏 `Tab Buttons` **自己的顶边 = 158.64**
        /// （原版 `Collection Menu Variant` 与奖励窗/商店不同：那两扇的 `Tab Buttons` 铺满 `Content Area`
        /// （从 `ContentT`=70.94 起），本窗的条本身就从 **158.64** 起）。
        /// <para>判据（本轮现读）：`工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 3`
        /// ⇒ `Tab Buttons | 167.2 158.6 332.2 1080.0 | 165.00 921.36 | … pad=0,0,30,0`。
        /// 与奖励窗/商店逐窗对比（同一条命令，三扇跨窗）→ `资料/普查产出_1022/W62_换牌压暗与收藏白条修复.md` §②。</para>
        /// <para>🔴 **原来只有 `Shadow` 的顶边对了、底板没对** ⇒ 本窗左栏底板（`Tab Buttons/Background` 的
        /// `Gradient2` 三条横带）比原版**高 88 px**（1009.06 − 921.36 = 87.7）。</para></summary>
        protected override float BarTop { get { return TabBarTop; } }

        /// <summary>左栏第一个键距**条顶边**（`BarTop` = 158.64）= 原版 VLG 的 **`padTop = 30`** ⇒ 键顶边 188.64。
        /// <para>🔴 **2026-10-23（W62）就地订正（铁律 5）**：这里原来是 **117.7** —— 那是「条从 70.94 起」时代的
        /// 折算值（188.64 − 70.94）。`BarTop` 把条顶边单独表达之后必须回到原版的 **30**，否则键会掉到 276.34。</para></summary>
        protected override float BarPadTop { get { return 30f; } }

        // 🔴 **2026-10-23（W62 · 界面线）**：`BarShadowTop` / `BarShadowTint` 两个覆写**已删** ——
        //   基类现在是「`BarShadowTop` = `BarTop`（= 本窗 158.64）· `BarShadowTint` = 原版 `(0,0,0,0.4745098)`」，
        //   与本窗原先那两个转发**逐值等价** ⇒ 判据只留基类一处（⛔ 别再在这里抄第二份）。
        //   判据/来历 → `MainMenuSubmenuWindow.BarShadowTop` / `BarShadowTint` 的 doc。

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
            win.ApplyVisualTypes();                           // 🔴 2026-10-19（N117）：见 ApplyVisualTypes
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
        /// <summary>卡位 **262.5 × 384** · spacing **0** · 行距 **384** · **贴左但整体居中**（A3）。
        ///
        /// <para>🔴 **2026-10-24（`E6`）判据补全 + 就地订正（铁律 5）** —— 这一组值的出处是
        /// **原版 prefab 的 RSR（`PolyAndCode.UI.RecyclableScrollRect`）序列化字段**，逐字段实读
        /// `d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/`：
        /// **`PrototypeCell` 指向 `Collection Card` 的那 3 份**（`MonoBehaviour_345129740864909092` ·
        /// `_7174171443954770645` · `_-1327870198181403947`）**逐字相同**：
        /// `_useFixedCellSize = 1` · `_cellWidth = **262.5**` · `_cellHeight = **384**` ·
        /// `_spacingX = _spacingY = **0**` · `_mobileSizeScale = 1.5`（桌面档不乘，见下）。
        /// ⇒ **262.5×384 / spacing 0 是原版值，本处无需改**。</para>
        ///
        /// <para>⛔ **别信「原版格 = 225×364.5、间距 20」那条**（`资料/普查产出_1024c/A2_引擎战斗判据总表.md` §Q6）——
        /// 它引的 `MonoBehaviour_4496232554149549872` / `_8316412282010198741` 的 `PrototypeCell`
        /// 其实是 **`Collection Deck With Highlight` / `Collection Deck`**（= **卡组列表**那一格，见本文件 `DeckCellW/H`），
        /// 父节点名 `Deck Scroll View` ⇒ **那是卡组编辑/收藏卡组页的格，不是卡池小卡**。
        /// 卡池那三份的 `PrototypeCell` 是 `Collection Card`（`GameObject/Collection Card.json`）。
        /// 🔑 判据不看「谁引了哪份 MB」，看 **`PrototypeCell` 指哪个 prefab**。</para></summary>
        public const float CardsCellW = 262.5f, CardsCellH = 384f;
        /// <summary>列数 = `floor(1589.7 ÷ (262.5 + 0))` = **6**。
        ///
        /// <para>🔴 **2026-10-24（`E6`）：算法**读得到**了**（A3 当年记「运行时算的、算法读不到」）——
        /// `d:/2/tools/decomp_full/PolyAndCode.UI.VerticalRecyclingSystem__ConfigureColumnNumber.c`：
        /// `_coloumns = floor(Content.rect.width / (_cellWidth + _spacingX))`（**只在该 RSR
        /// `_useFixedCellSize != 0` 时现算**；卡池那三份正好是 1 ⇒ 序列化的 `_segments`
        /// （跨这几页分别是 4 / 5 / 6）**不参与**，真正说话的是这一句）。
        /// ⚠️ 分母是 **`_cellWidth + _spacingX`**（不是 `+spacingY`），Content 宽 = 视口宽。</para></summary>
        public const int CardsCols = 6;
        /// <summary>贴左但整体居中：内容宽 6×262.5 = 1575 &lt; 视口 1589.7 ⇒ 两侧各留 **7.35**。
        ///
        /// <para>🔴 **2026-10-24（`E6`）出处**：`PolyAndCode.UI.VerticalRecyclingSystem__CreateCellPool.c`
        /// （反汇编 `0x1808A4xxx` 段）—— 网格定位式
        /// `offset = ((Content.rect.width − (spacingX + cellWidth) × coloumns) + spacingX) × **0.5**`、
        /// `x = (spacingX + cellWidth) × col + offset`、`y −= (spacingY + cellHeight)`（每行一次）。
        /// 本处 `spacingX = 0` ⇒ `((1589.7 − 262.5×6) + 0) × 0.5 = **7.35**`（与本式逐位相同）。</para></summary>
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

        /// <summary>🆕 **2026-10-18（`W11`）**：那条张数在**格内**占掉的高度 = `CardsCntMaxY × CardsCellH`
        /// = **38.272**（原版 `/Counter` 的上锚点 ⇒ ⛔ 不是我们另挑的数）。
        /// 卡只许占**它上面那一段**：`CardsCellH − 这个` = **345.728**。</summary>
        public static float CardsCounterH { get { return CardsCntMaxY * CardsCellH; } }

        /// <summary>卡体在格里的位置框 = **整格**（卡体按原版公式缩放后**在整格里居中**）。
        ///
        /// <para>🔴 **2026-10-25（`C` 域 · 调度台裁定）改正：原来这里返回「格高 − 张数条」。**
        /// 判据（prefab 现读，`menu_dump.py bundle_menus_assets_all "Collection Card" --depth 4`）：
        /// 卡体 `CardUI/2DCard` 在 **350×512** 原型里是 **y 6.2 → 505.8**（上下各 6.2 边距 ⇒ **居中**）
        /// ⇒ 折成 384 格 = 上下各 **4.65**、高 **374.8** ⇒ **它压住张数条（从 461/512 = 345.7/384 起）约 33.6px**，
        /// 而**那是原版长相**（张数条画在卡下缘之上）。</para>
        ///
        /// <para>⚠️ `W11`（用户 2026-10-18 报「张数与卡牌重叠」）当时量到的重叠，**根因是缩放口径**
        /// （按整格反解、没有公式给的居中），**不是「卡不该压条」**。真正修法见
        /// `RebuildCardsCells` 里 `scale` 那一段（改成原版公式），⛔ 不是把卡缩到条上面。</para>
        /// <para>判据全文 → `资料/普查产出_1018/W11_卡组编辑计数重叠.md`（旧结论）+ 本段（2026-10-25 裁定后的口径）。</para></summary>
        public static PxRect CardsCardRect(PxRect cell)
        {
            return cell;
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
        /// 本作资源固定 2000（用户 2026-10-24 从 9999 改）、`CardProgress.Owned` 是**给足**口径（= 卡组上限 + 升满所需 ⇒ 每张 10~11）
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
            if (def == null) return "x0";
            int cap = CardProgress.DeckCap(def.Rarity, def.Type);
            // 🔴 **2026-10-25（`C` 域 · 背包框架层接通）**：**拥有数的正本 = `Inventories.Cards`**
            //   （原版这一格读 `InventoryManager.GetOwnedCount(card)` —— 判据
            //   `CardCollectionDisplay__SetCell.c`；背包层见 `Shell/Inventory.cs`）。
            //   ⚠️ 条目还没有时（极少数：卡不在 `CardsState.Pool` 里）回退到旧的 `CardProgress.Owned`，
            //      并**出声** —— ⛔ 别静默给 0（那会让某一格凭空显示 `x0`）。
            var info = Inventories.Cards.Get(def);
            int owned;
            if (info != null) owned = info.Amount;
            else
            {
                owned = CardProgress.Owned(def.Id, def.Rarity);
                if (_counterNoInvWarned.Add(def.Id))
                    Debug.LogWarning("[Collection] 卡 `" + def.Id + "` 不在背包 `Inventories.Cards` 里 ⇒ 拥有数回退到 "
                                   + "`CardProgress.Owned`（" + owned + "）。卡池播种见 `CardsState` 的 getter。");
            }
            return "x" + Mathf.Min(owned, cap);
        }

        /// <summary>「这一张卡不在背包里」的出声去重表（每次会话每张卡只喊一次）。</summary>
        static readonly HashSet<string> _counterNoInvWarned = new HashSet<string>();

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
        ///    （`CardPresentation/Shell/MenuScroll.cs`）—— 滑动中节点**是活着的**（要画），那条判据不够用
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

        /// <summary>Cards 页的筛选状态（**复用卡组编辑那套** `DeckEditorState.Filter` —— 别写第二套）。
        /// 🔴 **2026-10-18（卡池排序与显隐）**：这一份要两个开关，**卡组编辑那条路不跟着变**（它自带同一条显示闸）：
        /// · <see cref="DeckEditorState.ShowInCollectionOnly"/> = `true` ⇒ 套原版
        ///   `CardCollectionTab__c__GetCollection_b__8_0.c:19-30` 的 `showInCollectionOptions != Hide(0)`
        ///   —— 剔掉 **122 张 `show=0`**（92 天赋 + 30 `Z-NC`）；
        /// · <see cref="DeckEditorState.SortByInventoryOptions"/> = `true` ⇒ 排序多夹一层 `inventoryOptions`
        ///   （`CardCollectionTab__GetCollection.c:99`；卡组编辑卡池没有这一层）。
        /// ⛔ 别把这两条写进 `DeckEditorState` 的默认值 —— 纯逻辑用例要「不设筛选命中全部」。</summary>
        static DeckEditorState _cardsState;
        public static DeckEditorState CardsState
        {
            get
            {
                if (_cardsState == null)
                {
                    _cardsState = new DeckEditorState(CardDatabase.Load());
                    _cardsState.ShowInCollectionOnly = true;
                    _cardsState.SortByInventoryOptions = true;
                    // 🆕 **2026-10-25（`C` 域）**：把卡按「全解锁」口径**播种**进背包框架层
                    //   （每张卡数量 = 能带几张 = `DeckRules.CopyLimit`）—— 卡池一建就播，之后
                    //   `CardsCounterText` 的「已拥有」读数就走 `Inventories.Cards`（幂等，重复调无害）。
                    // 🔴 **2026-10-25（迁移第一步）**：播种口径 = **`CardProgress.Owned`**（「给足」口径），
                    //   不是默认的 `CopyLimit` —— 为的是让背包里的拥有数与旧的 `CardProgress` **逐值相等**，
                    //   先把「两套并存」的读口对齐（断言在 `Editor/CollectionScene.cs`），下一步再删旧写口。
                    // 🔴 **顺序不能反**：**先读盘**（盘上有值就以它为准），**再播种**（只补还没有条目的卡）。
                    //   反过来的话，播种会把盘上的值盖成出厂值（静默丢数据）。
                    InventoryStore.Load(CardProgress.InventoryName, Inventories.Cards);
                    Inventories.SeedCardsAllUnlocked(_cardsState.Pool,
                        c => CardProgress.Owned(c.Id, c.Rarity));
                }
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
            // 🆕 2026-10-19：上一趟建的 `Army Icon` 已随页树销毁 ⇒ 清掉悬空引用（状态 `_wcArmy` 留着，见下）。
            _wcArmyQuad = null;
            // 页头：Filters 圆钮 + 文案 + Clear filters（**四页共用的那一行** —— 见 `BuildFilterHeader`）
            // ⚠️ 2026-09-23 顺手修两处（A3 §5·2 原文）：原版这套按钮是
            //    **[圆钮 50×50]** → 里面 `icon detail` 只有 **30×30**（`sd=(-20,-20)` = 四边各内缩 10、preserveAspect）
            //    · `label` 在 **[437.2, y, 150, 50]**；我们原来是「图拉满 50×50」+「label 从 427.2 起」。
            // 本页字号 = **42 / 42**（原版逐页不同，见 `BuildFilterHeader` 的注释）
            BuildFilterHeader(page, 1, 42f, 42f, ClearCardFilters, ToggleFilters);

            // 🔴 **2026-10-19（第五十七会话 · 树形尾巴）**：补 `Header Filters ▸ WIldcard Display` / `▸ Counters`
            //   **两层容器**，把下面那四件（底图 / 4 图标 / 4 数字 / 阵营徽记）按原版父链重挂进去。
            //   判据 = **原版 prefab 现读**（`python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "CardsTab" --depth 7`，
            //   2026-10-19）：
            //     · `CardsTab ▸ Header Filters ▸ WIldcard Display` = **1470.0,70.9 → 1870.0,155.9**（400×85，
            //       组件 `WildcardDisplay`，字段 `armyIcon/commonCount/epicCount/legendaryCount/rareCount/shinyEffect`）；
            //       孩子序 = **`Background` → `Counters` → `Army Icon`**；
            //     · `WIldcard Display ▸ Counters` = **1555.0,91.4 → 1870.0,135.4**（315×44，`HorizontalLayoutGroup`
            //       spacing=5 align=0 pad=10,10,0,0）。
            //   🔴 **容器是纯结构节点**（`Node` = 只有 `RectTransform`、没有 quad）⇒ **不改渲染队列、不改命中**：
            //     孩子仍按**绝对设计 px** 落位（`MenuDraw.Local(parent, …)` 会按父链换算）⇒ 视觉逐位不变。
            //   ⚠️ 原版 `Counters` 底下还有四组 `Common/Rare/Epic/Legendary`（每组 `Icon` + `Counter`）——
            //     🆕 **2026-10-19（续）：四组已按原版建出**（`Counters ▸ {组} ▸ Icon/Counter`，见下面那个循环）。
            var wcHeader = page.Find("Header Filters");
            if (wcHeader == null)
                Debug.LogWarning("[Collection] `CardsTab ▸ Header Filters` 没找到 ⇒ `WIldcard Display` 退回建在页节点上（树形不对）");
            var wcDisplay = Node(wcHeader != null ? wcHeader : page, "WIldcard Display",
                                 new PxRect(1470f, 70.9f, 1870f, 155.9f));

            // 万能卡计数：4 个稀有度图标 + 各自的数字（**复用卡组编辑那条几何**，别抄第二份）
            // 🔴 **2026-10-18（N57）**：原版 `WIldcard Display/Background` 是 **Sliced 九宫 15**、
            //   `m_Color = (0.462,0.462,0.462,1)`（判据 = `Collection Menu Variant` dump；`Deck Editing Menu`
            //   的同名件逐值相同）。我们原来走 `Rect`（Simple 拉伸）+ **无 tint（白）** ⇒ 画成一条亮白边。
            // 🔴 **2026-10-19（续 · 原版现读）**：`Background` 的 y 由 **91.5→135.5** 改成 **91.4→135.4**
            //   （判据 = `menu_dump.py bundle_menus_assets_all "CardsTab" --depth 7 --no-layout` 现读：
            //    `Background` 1550.0,**91.4**→1870.0,**135.4**，高 44；同一批 `Counters`/四组/`Icon`/`Counter` 全是这两个 y）。
            //   节点名也按原版：`Wildcard Bg` → **`Background`**（全仓只有本行建它，⛔ 不留两套名字）。
            Nine(wcDisplay, "40k_topmarquee_currency_display_BW",
                 new PxRect(1550f, 91.4f, 1870f, 135.4f), new Vector4(15f, 15f, 15f, 15f), QPagePanel,
                 new Color(0.462f, 0.462f, 0.462f, 1f), true, "Background", null, 44f, 39f);
            // `WIldcard Display ▸ Counters`（原版 1555,91.4→1870,135.4 · 315×44）—— 纯容器，孩子仍是绝对设计 px。
            var wcCounters = Node(wcDisplay, "Counters", new PxRect(1555f, 91.4f, 1870f, 135.4f));
            string[] wcIc = { "40k_general_wildcard_common_small", "40k_general_wildcard_rare_small",
                              "40k_general_wildcard_epic_small", "40k_general_wildcard_legendary_small" };
            // 🆕 **2026-10-19（续 · 原版现读）**：`Counters` 底下是**四组** `Common/Rare/Epic/Legendary`，
            //   每组 = `HorizontalLayoutGroup`（65×44；孩子 `Icon` 30×44 + `Counter` 41×44）。
            //   判据 = `menu_dump.py … "CardsTab" --depth 7 --no-layout` 现读（2026-10-19）：
            //     Common 1565→1630 · Rare 1640→1705 · Epic 1715→1780 · Legendary 1790→1855（y 全 91.4→135.4）。
            //   ⚠️ 组宽 **65**、而 `Counter` 右沿到 `x+71` —— 原版孩子**溢出组框 6px**（dump 实读），照抄。
            string[] wcGroup = { "Common", "Rare", "Epic", "Legendary" };
            // 🔴 **2026-09-28 用户拍板：万能卡数字一律恒定 `99`**（原版这 4 个数是 `WildcardDisplay`
            //    的库存直出；我们缺的是**发放源** ⇒ 数字不自己算）。
            //    🆕 **2026-10-19（界面线尾巴 · 铁律 5）**：原文那句「我们既没有 hover 也没有发放源」里
            //    **hover 那一半已过期** —— 同一颗 `WildcardDisplay` 的**阵营徽记**已接悬停/详情换图
            //    （`SetWildcardArmy`）；**只有数字**仍恒定（发放源没有）。
            //    判据 → `项目任务.md` §三 第 15 条 第 29 项。
            for (int i = 0; i < 4; i++)
            {
                float x = 1565f + 75f * i;
                // 四组容器（原版名字逐字：`Common/Rare/Epic/Legendary`；纯结构节点、无 quad）。
                var wcGrp = Node(wcCounters, wcGroup[i], new PxRect(x, 91.4f, x + 65f, 135.4f));
                // 🔴 **2026-09-27 补 `keepAspect`（PA 普查抓的）**：原版这 4 个图标 `m_PreserveAspect = 1`、
                //   `m_Type=0`（Simple），贴图 42×51 塞进 30×44 的框 ⇒ 原版只画 **30×36.4**（按框居中）。
                //   我们原来拉伸 ⇒ 画满 30×**44**，**高 ×1.21**。同为 PA=1 的另三张（41×51）同理。
                //   节点名按原版 = **`Icon`**（原来是 `Wildcard Icon i`；全仓只有本处建它）。
                Rect(wcGrp, wcIc[i], new PxRect(x, 91.4f, x + 30f, 135.4f), "Icon", QPageRow,
                     null, true);
                var t = Text(wcGrp, DeckRuntime.WcCountText, x + 30f, x + 30f + DeckRuntime.WcCountWrapPx,
                             91.4f, 91.4f + DeckRuntime.WcCountBoxH, 5, PageInk,
                             // 节点名按原版 = **`Counter`**（原来是 `Wildcard Count i`）。
                             "Counter", DeckRuntime.WcCountFontPx,
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
            // 🆕 2026-10-17（B10）：`WIldcard Display` 三个孩子里那颗 `Army Icon`（1470,70.94 → 1550,155.94 · 80×85 ·
            //   prefab `m_Sprite` = `40k_DeckSelection_icon_FactionBlackLegion` · **preserveAspect**）。
            //   🔴 **2026-10-25（K3）就地订正两处（铁律 5）**：
            //   ① **起点不是 prefab 那张黑军团** —— `CardCollectionTab__Setup.c:44-48` 在 `Setup()` 里就
            //      `WildcardDisplay.Initialize(wildcards, 10)` ⇒ 一开页 sprite 已换成 **Ultramarines**（10）。
            //      见 `WcArmyDefault` 那条 doc（它现在也是 10）。
            //   ② **原版的触发不是「指针悬停」** —— `CardCollectionDisplay__OnListDrag.c` 是
            //      `OnListDrag(Vector2 listPos)`（**列表被拖动/滚动**那一拍）⇒ `CheckFocusedArmy()`；后者拿
            //      **一个固定矩形** `referenceCardPoint` 去
            //      `RectExtensions.OverlapsAny(那个矩形, 当前格们, out 命中格)`，命中就
            //      `WildcardDisplay.Initialize(那张卡的 army)`。**指针 hover 不是它的输入**。
            //   ✅ **2026-10-25（`C` 收藏域）已按原版改写** —— K3 当时留的两个「判据未定」现在都查清了：
            //      ① **`Reference Card Pointer` 的矩形**：`工具/menu_dump.py bundle_menus_assets_all "CardsTab"
            //         --depth 8` 现读 = **402.3,236.4 → 596.1,281.4**（193.84×45.09，父件 = `Collection Display`
            //         的直接孩子、与 `Scroll View` 同级；`m_IsActive = 0` 只是不画、矩形照用）。
            //         ⚠️ K3 记的 `417.69,260.95 → 611.53,306.05` **不是本窗这一颗** —— 那是
            //         **卡组编辑窗**（`Deck Editing Menu ▸ Card Display ▸ Scroll View`）里那颗同名节点的矩形
            //         （`Deck/DeckRuntime.PoolRefL/T/R/B`），两个 prefab 各有一颗、**不是同一颗的两种 dump 口径**。
            //         本行这组与 `CardsViewport` 同一帧（`Scroll View` 330.2,155.9 与我们的视口逐位相同）⇒ **以本组为准**。
            //      ② **`OverlapsAny` 命中多格时返回哪一格**：`RectExtensions.cs:69-244`（🥇 `out_cpp2il_dev_ilspy`）
            //         ＋ `decomp_full/RectExtensions__OverlapsAny.c` 两份逐句一致 = **按传入 `rects` 的枚举顺序，
            //         返回【第一个】相交的**（`if (bVar1) { *param_3 = uVar7; … return 1; }`）——
            //         没有「离中心最近」、没有排序。而传入的顺序 = `Enumerable.Select(cells, …)` 的
            //         `RecyclableScrollRect.Cells` 池序。
            //         📌 **而且这里实际上不会有多格歧义**：参考矩形 193.84 宽 < 一格 262.5、45.09 高 < 一格 384
            //         ⇒ **同列只可能压到 1 格**；横向 x 402.3…596.1 只落在第 0 列（第 1 列从 600.05 起）⇒
            //         **任何滚动位置上最多命中 1 格**（正好压在行边界时才是 2 格，那时按可见序取上面那格）。
            //   ⇒ 实现 = `CheckFocusedArmy()`（本文件），输入是**滚动位置变化**（`CardsScroll.OnChanged`），
            //      不是悬停。⚠️ 原版**没有「移开就还原」**：未命中直接 return ⇒ 保留最后那张。
            //   ⚠️ 图走 `Core/ArmyIcons.cs`（13 阵营，唯一一份；`IconsByArmySO` 逐字节解出）—— ⛔ 别在这写第二份映射。
            var wcArmyR = new PxRect(1470f, 70.94f, 1550f, 155.94f);
            _wcArmyArt = ArmyIcons.IconName(_wcArmy) ?? WcArmyDefaultArt;
            _wcArmyQuad = Rect(wcDisplay, _wcArmyArt, wcArmyR, "Army Icon", QPageRow, null, true);

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
            // 🔴 **2026-10-23（`C3`）**：原版这台 `ScrollRect` 是 **`m_MovementType = 1 (Elastic)`**
            //   （`Collection Menu Variant ▸ CardsTab ▸ Collection Display ▸ Scroll View`；
            //   五处主滚动区同值，判据见 `MenuScroll.Elastic` 的注释与核查报告 §2·15）⇒ 拖过头要有橡皮筋回弹。
            CardsScroll.Elastic = true;
            CardsScroll.Owner = gameObject;
            // 🔴 **A465**：构建循环那一路「整块在视口外就不建」也要吃这颗节点
            //   （⛔ 别改成让 `Intersects` 沿 `Owner` 找 —— `Owner` 是窗根、节点在它下面，找不到）。
            CardsScroll.ClipNode = cardsVp;
            // 🔴 **2026-10-25（`C` 收藏域 · `K3` P2-⑦ 的口径已查清）**：原版这一拍的入口是
            //   `CollectionDisplay.Initialize` 里 `listView.onValueChanged.AddListener(OnListDrag)`
            //   （`CollectionDisplay.cs:512-523`）⇒ **列表一滚动**就 `OnListDrag` → `CheckFocusedArmy()`。
            //   我们的 `OnChanged` 与它逐字同义（滚动位置变了）；**`CheckFocusedArmy` 挂在
            //   `RebuildCardsCells` 的尾巴**（那里同时也接住了原版 `RefreshCollection` 的延迟那一拍）
            //   ⇒ 这里**不用**再调一次（调了就是每滚一格算两遍）。
            CardsScroll.OnChanged = () => RebuildCardsCells(holder);
            PointerLayer.RegisterScroll(CardsScroll);
            // 🆕 2026-10-12（`A1628` 第二轮）：这一页的**逐格相位**（只在真 Play 出声；批处理零影响）。
            // ⚠️ 只包**建页这一次**调用 —— 滚动时 `OnChanged` 也会调 `RebuildCardsCells`，
            //   那条路逐格重建同样贵，但不该把日志刷爆（要看就把这三行搬到滚动那条路自己量）。
            BuildWatch.CellsBegin();
            // 🔴 **`A1834`（2026-10-20）**：这张**before 快照必须排在 `RebuildCardsCells` 之前** ——
            //   它就是「变化之前」那一半（字形/字符计数 + 时刻）。原账的病灶就是探针只排在**变化之后**
            //   ⇒ 建卡早把字形烘完、文本也没再变 ⇒ 那里量到的 `0.0 ms` 是**必然结果**
            //   （判据 → `资料/普查产出_第二十八会话/A1814_A1813_诊断.md` §A1813 ③）。
            //   ⚠️ 位置不变量由 `Editor/CollectionScene.cs` 断着（`TextRelayoutProbe.CardsAtBegin == 0`
            //      ＋ 源码次序）⇒ 把它挪到下面两句之后，`CollectionScene.Run` 当场红。
            BuildWatch.TextProbeBegin(CardsCells.Count);
            RebuildCardsCells(holder);
            BuildWatch.TextProbeEnd(CardsCells.Count);     // ← 「变化之后」那一半：Δ字形 / Δ字符 / 整段 ms
            BuildWatch.CellsReport("CardsTab 卡池（只建视口内的格，自检实测 18）");
            // 🆕 `A1628` 第二轮：再量**一次文字重排**的绝对耗时。
            // ⚠️ **`A1834` 订正**：这一次是**warm 重排**（卡已建好、字形已烘、文本未变）—— 它只回答
            //   「**没有新字形**的一次 `ForceMeshUpdate` 多贵」，**不能**用来证实或推翻「字体现烘」那条假设；
            //   那条假设的判据是上面那句 `TextProbeBegin/End` 的 **Δ字形**（方法 doc 里写清了）。
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
            // 🆕 **2026-10-25（接线 · G 的 `WildcardTutorial`）**：卡池建完就起一次万能牌教程
            //   （原版 `CardCollectionTab.OnOpen`：`FeatureConfig.Check("WildcardTutorial")` + 有万能牌 + 没看过 ⇒
            //    `StartWildcardTutorial(ownedToggle, …)`）。**已做过 ⇒ `TryStart` 自己返回 null**（= 原版 `state == End`）。
            //   ⚠️ 我们**没有** `FeatureConfig`（服务端开关）⇒ 这一层恒真、不带闸；`Inventory<Wildcard>` 那一层我们也还没有
            //   ⇒ 「有没有万能牌」这一条按「教程没做过就起」处理（如实记，见 `资料/完全复刻/报告/C_W11缩放与轮播_第1轮.md`）。
            //   🔴 **只在真跑里起**（`Application.isPlaying`）：教程会建一个指针节点 + 写 `PlayerPrefs`，
            //      而 11 条自检**全在 `-batchmode`**（`isPlaying == false`）⇒ 不加这道闸的话，
            //      自检里会凭空多一个节点、并把「已做过」写进注册表（污染下一次真跑）。**如实标：原版没有这道闸**
            //      （它永远在真跑里）—— 这道闸只为「批处理不许碰玩家状态」，与 `DailyData.StoreOn` 同一条纪律。
            if (Application.isPlaying)
            {
                try
                {
                    WildcardTutorial.TryStart(transform, new WildcardTutorialBindings(this));
                }
                catch (System.Exception e)
                {
                    // 红线：不许静默失败 —— 教程起不来要让日志说得出来（但它**不能**影响卡池）
                    Debug.LogWarning("[Collection] 万能牌教程没起来（不影响卡池）：" + e.Message);
                }
            }
        }

        /// <summary>把本窗的三个节点 + 两个开关状态交给 `WildcardTutorial`。
        /// <para>⚠️ **节点名的如实差异**：原版 `.ctor` 的字典名是 `Filter Toggle` / `Owned Toggle` / `CardsTab`
        /// （`WildcardTutorial.NodeFilterToggle/…`）；我们的**筛选开关**恰好同名，但 **`Owned` 那一格**
        /// 我们叫 **`Cell_owned`**（`FilterPanelModel` 那一族的节点命名）⇒ 这里转一次，⛔ 不为它改节点名
        /// （改了就撞上 Alloy 自检里按名找的那一族）。</para></summary>
        sealed class WildcardTutorialBindings : IWildcardTutorialBindings
        {
            readonly CollectionWindow _w;
            public WildcardTutorialBindings(CollectionWindow w) { _w = w; }

            Transform Page { get { return _w.PageRoot(1); } }   // 1 = Cards 页

            public Transform FilterToggleNode { get { return FindDeep(Page, "Filter Toggle"); } }
            public Transform OwnedToggleNode { get { return FindDeep(Page, "Cell_owned"); } }
            public Transform CardsTabNode { get { return Page; } }
            public bool FilterOn { get { return _w.FiltersOpen; } }
            public bool OwnedOn { get { return CollectionWindow.CardsState.Filter.Owned; } }   // ⚠️ `CardsState` 是 **static**
            /// <summary>= 原版 `filterToggle.isOn = false`（第三步之前把筛选关掉）。我们这颗是自绘的
            /// 「开/关抽屉」⇒ 关了就走 `ToggleFilters()`（幂等：已经关着就什么都不做）。</summary>
            public void SetFilterOn(bool on)
            {
                if (!on && _w.FiltersOpen) _w.ToggleFilters();
            }
        }

        Transform _deckEmpty;      // Deck 页的「一套卡组都没有」
        Transform _cardsEmpty;     // Cards 页
        Transform _cosmoEmpty;     // Cosmetics 页（卡背）
        // ============================================================ 🆕 WildcardDisplay 徽记（Cards 页）
        /// <summary>`WIldcard Display ▸ Army Icon` 那一颗 quad（原版 `WildcardDisplay` 的 `armyIcon`）。
        /// 判据 = 原版 `CardCollectionDisplay.CheckFocusedArmy`：指针压在卡池某格上 ⇒
        /// `WildcardDisplay.Initialize(card.army)` ⇒ 徽记换成**那张卡的阵营**（`Core/ArmyIcons.cs` = 唯一映射）。
        /// 本窗由 `SetWildcardArmy` 换图（`ImageQuad.SetTexture`、PA=1 —— prefab 实读 `m_PreserveAspect = 1`）。</summary>
        ImageQuad _wcArmyQuad;
        /// <summary>当前徽记的阵营（`CardArmy` 数值）。
        /// <para>🔴 **2026-10-25（K3）就地订正（铁律 5）**：原来这里写「出厂 = prefab 那张 **BlackLegion**（`50`）…
        /// ⇒ 我们这一格也照出厂图起步（⛔ 别改成 Ultramarines —— 那是**卡组编辑窗** `FilterToDeck` 的默认档）」
        /// —— **后半句错了**：`CardCollectionTab__Setup.c:44-48` 在 `Setup()` 里就调
        /// **`WildcardDisplay.Initialize(wildcards, 10)`**（`0x40` = `CardCollectionTab.wildcards`，
        /// 见 `dump.cs:70349-70359`），而 `WildcardDisplay__Initialize.c:9-10` 只有一句早退
        /// `if (param_2 == currentArmy) return;` —— 非序列化字段 `currentArmy` 出厂 0，`10 != 0`
        /// ⇒ **它一定跑下去**、把 sprite 换成 `ArmyUtilities.GetArmyIcon(10)`。
        /// `10` = `CardArmy.Ultramarines`（`DeckArmyFilter.options` 13 档的第一档）⇒
        /// **收藏窗 Cards 页一开，徽记就是 Ultramarines**，不是 prefab 那个 BlackLegion 死值。
        /// （「卡组编辑窗的默认档」这句也不成立 —— 那条是 `DeckEditingWindow.FilterToDeck`，
        ///  与本页无关；它同族但**各自初始化**。）</para></summary>
        static readonly int WcArmyDefault = ArmyIcons.Id("Ultramarines");
        int _wcArmy = WcArmyDefault;
        /// <summary>出厂那张图 = `ArmyIcons` 表里 Ultramarines 那一面旗（判据见上一条：`Initialize(10)`）。</summary>
        const string WcArmyDefaultArt = "40k_DeckSelection_icon_FactionUM";
        /// <summary>`_wcArmyQuad` 现在贴的是哪一张（自检/去重用）。</summary>
        string _wcArmyArt = WcArmyDefaultArt;
        static readonly HashSet<string> _wcArmyWarned = new HashSet<string>();
        // 🔴 **2026-10-17（T09 第 1 步）：卡池视图池** —— 滚动不再整屏重建。
        //   键 = `CardDef.Id + 语档`（语档变了卡面文字要重排 ⇒ 不跨语档复用）；上限 ≈2.5 屏。
        //   判据与实测数字 → `资料/普查产出_1017/UI/PERF_T09_收藏窗性能诊断.md`
        readonly Dictionary<string, CardView> _cellPool = new Dictionary<string, CardView>();
        readonly List<string> _cellKeyOrder = new List<string>();
        readonly List<CardView> _cellUsed = new List<CardView>();
        const int CellPoolCap = 48;
        // 🔴 **2026-10-18（本批 · T09 / `A1813`）：异画页（Alternate Art Tab）视图池** ——
        //   与上面那颗**同一套做法**（按「这张卡」复用、屏外 park、上限 48），但**单独一份池**：
        //   同一张卡在异画页带 `CardData.artOverride`（异画立绘）、在卡池页没有 ⇒ view 不能跨页共用。
        //   键 = `AltArtCard.CardId + 语档`（异画立绘由 `CardArt.AltArt(CardId)` 定 ⇒ 同 id 必然同图；
        //   语档变了卡面文字要重排 ⇒ 不跨语档复用，理由同卡池页那一颗）。
        //   为什么要有它：这一页原来 `RebuildStyleCells` 首句把 `Viewport` 下**全部孩子销毁**、
        //   逐格重新 `CardView.Create`（≈2.2 s/格），而 `StyleScroll.OnChanged` 的阈值只有 0.01 px
        //   ⇒ 拖一下 = 整屏重建（账 `A1813` 的「Alternate Art Tab Setup 17.3 s」就是这条路）。
        //   判据/实测 → `资料/普查产出_1017/UI/PERF_T09_收藏窗性能诊断.md`
        readonly Dictionary<string, CardView> _stylePool = new Dictionary<string, CardView>();
        readonly List<string> _styleKeyOrder = new List<string>();
        readonly List<CardView> _styleUsed = new List<CardView>();
        const int StylePoolCap = 48;
        /// <summary>Cards 页上一趟 `RebuildCardsCells` **真正 `Create`** 了几张视图（自检读数：
        /// 滚动一格应当为 **0** —— 池命中 = 搬实例，不新建）。</summary>
        public int CardsCellCreatedLastRefresh { get; private set; }
        /// <summary>异画页上一趟 `RebuildStyleCells` **真正 `Create`** 了几张视图（自检读数：滚动一格 = 0）。</summary>
        public int StyleCellCreatedLastRefresh { get; private set; }
        /// <summary>Cards 页的「过滤后为空」提示（判据与 Styles 页**同一条**：`VisibleCards().Count &lt;= 0`）。</summary>
        void RefreshCardsEmpty()
        {
            if (_cardsEmpty != null) _cardsEmpty.gameObject.SetActive(CardsState.VisibleCards().Count <= 0);
        }

        // ============================================================ 入场动画（原版 `CollectionCard.AnimateIn`）
        //
        // 判据（2026-10-24 · A18 **逐参数现读**，⛔ 不是挑的手感）：
        //   · 方法体 `d:/2/tools/decomp_full/CollectionCard__AnimateIn.c:11-43`；参数从反汇编现读
        //     （`工具/rva_of_method.py "CollectionCard$$AnimateIn"` → VA `0x1806D9540`，
        //      `工具/disasm_va.py` + `工具/read_literal.py`）：
        //       ① **`canvas.alpha = 0`** —— `0x1806D959C` `xorps xmm1,xmm1` → `CanvasGroup.set_alpha`
        //          （`CanvasGroup.set_alpha` 实参走 xmm1，见 `0x18339A3F0` 的 `movaps xmm6,xmm1`）
        //       ② `Sequence.AppendInterval(GetSiblingIndex() * 0.1)` —— 常量 `0x1834B2DC4` 实读 **0.1**
        //       ③ `Append(canvas.DOFade(1, 0.7))` —— `0x1834B2BB8` 实读 **1**、`0x1834B3188` 实读 **0.7**
        //       ④ `Join(canvas.transform.DOLocalMoveY(y, 0.7).From(y + 200))` —— `0x1834B3258` 实读 **200**；
        //          `y` = 建补间之前那次 `get_localPosition` 的 `.y`（`0x1806D95FA` `movss xmm6,[rax+4]`），
        //          `From` 传的是 `y + 200`、`setImmediately = true`（`r8b = 1`）/ `isRelative = false`（`r9 = 0`）
        //          ⇒ **从上方 200px 落到最终位**。
        //   · **缓动**：两条嵌套补间在原版都**没有 `SetEase`** ⇒ 吃 DOTween 的 `defaultEaseType`
        //     （= `Ease.OutQuad`，工程 `Assets/Resources/DOTweenSettings.asset` 实读 `defaultEaseType: 6`；
        //      原版自己那份 DOTweenSettings **本地查不到**，按 DOTween 默认那一档 —— 如实标注）。
        //     ⚠️ **Sequence 上那句 `SetEase` 是「整条时间轴」的缓动**（`DOTween.XML` 原文
        //     "If applied to Sequences eases the whole sequence animation"）⇒ 这里只能传 `Linear`
        //     （= 时间轴不弯曲，两条嵌套补间保留各自的 OutQuad）；⛔ 传 `OutQuad` 会把它们**再弯一次**。
        //   · **触发时机** = 原版 `CollectionDisplay.firstShow`（**只有第一屏播一次**）：
        //       `CollectionDisplay<object>..ctor` 置 `[this+0x60] = 1`（VA `0x1815EF1ED`）
        //       → `Initialize` 尾部调 `listView.ReloadData()`（VA `0x1815EC37B`）建首屏格子时
        //         `SetCell` 把 `[this+0x60]`（= `firstShow`）当 `animate` 传给
        //         `CollectionItem.Config(item, display, animate, isSelected)`（VA `0x1815EE8B3` 读、`0x1815EE925` 分支）
        //       → `Config` 里 `if (animate) AnimateIn()`（VA `0x1815F1B71` → 虚表 `[rax+0x1e8]` = Slot 11）
        //       → 之后 `RefreshCollection` 把它清 0（VA `0x1815ECC77`）
        //       ⇒ 筛选 / 滚动 / 换风格**都不再播**（它们走 `RefreshCollection` / 重建）。
        //   · ⚠️ `initialCardScale`(150) / `initialHeight`(512) **不在 `AnimateIn` 这条路上** ——
        //     它们在 `OnRectTransformDimensionsChange` 里算
        //     `cardUI.localScale = sizeDelta.y × 150 / 512`
        //     （`CollectionCard__OnRectTransformDimensionsChange.c:17`）。
        //     🔴 **A14 报告 §五·1 说它们用在 `AnimateIn` —— 就地更正：不是**（判据 = 该方法体全文
        //     只碰 `+0x60` 与 `transform`，一次都没读 `+0x70` / `+0x74`）。
        //
        // 🔴 **本工程的两个偏离（如实记）**：
        //   ① 原版一个 `CanvasGroup` 就能淡整格；我们这棵树里**卡面/张数条底图都是世界空间 `MeshRenderer`**
        //      （`CardView` 自建网格 · `ImageQuad`）⇒ `CanvasGroup.alpha` **传不下去**（同族的坑：
        //      `Shell/DeckInfoPopup.cs:2081`）。所以淡入要同时驱动卡 / 底图 / 文字三套后端 —— 见 `CellFade`。
        //   ② `.From(y + 200, setImmediately: true)` 在这里写成「先把 `localPosition.y` 摆到 `y + 200`，
        //      再 `DOLocalMoveY(y, 0.7)`」—— **语义逐条等价**（`setImmediately: true` 做的就是这件事），
        //      `isRelative: false` ⇒ 绝对值而不是相对位移。
        //   ③ **批处理（编辑器、非 Play）不播**：没有帧循环，DOTween 不会推进 ⇒ 播了会把整页卡停在
        //      `alpha = 0` / `y + 200` 上（后面所有版面断言与截图全崩）。生产路径用 `Application.isPlaying`
        //      把这一条关掉；动画本身由自检用 `CardTween.Mode = Manual` + `CardTween.Advance` 单独验。

        /// <summary>`AppendInterval` 的步长 —— 原版 `.rdata` `0x1834B2DC4` 实读 **0.1** 秒 / 每格
        /// （下标 = 格子在**本趟可见序列**里的序号；原版取的是 `GetSiblingIndex()`）。</summary>
        public const float CardAnimateDelayStep = 0.1f;
        /// <summary>`DOFade` / `DOLocalMoveY` 的时长 —— `0x1834B3188` 实读 **0.7** 秒。</summary>
        public const float CardAnimateDuration = 0.7f;
        /// <summary>`DOFade` 的两端 —— 终值 `0x1834B2BB8` 实读 **1**；起始 = `CanvasGroup.set_alpha(0)`。</summary>
        public const float CardAnimateAlphaTo = 1f, CardAnimateAlphaFrom = 0f;
        /// <summary>`DOLocalMoveY(…).From(y + 200)` 的 **200** —— `0x1834B3258` 实读。
        /// ⚠️ **不折算**：原版运行时把格子从 350×512 改成 262.5×384 只改 `sizeDelta`，
        /// `localPosition` 的量纲仍是画布 px（= 我们的设计 px）⇒ 这里就是 **200 / 200**。</summary>
        public const float CardAnimateRise = 200f;

        /// <summary>一格的「整格不透明度」把手 —— 原版 = `Collection Card/Content` 上那颗 `CanvasGroup`
        /// （`CollectionCard.canvas`，字段 `+0x60`；`AnimateIn` 对它调 `CanvasGroup.set_alpha` / `DOFade`）。
        ///
        /// <para>🔴 **为什么这里不止一个把手**：`CanvasGroup.alpha` 只乘得到 uGUI 的 `CanvasRenderer`，
        /// 而本工程的卡面（`CardView` 自建网格）与张数条底图（`ImageQuad`）都是**世界空间 `MeshRenderer`**
        /// ⇒ 必须逐个后端写（同族的坑 → `Shell/DeckInfoPopup.cs:2081` 那段注释）。四路一起动：
        /// ① `CardView.SetAlpha`（整卡，含它自己那几段 TMP 的顶点色）
        /// ② `ImageQuad.SetTint`（张数条底图）
        /// ③ `Label.SetColor`（张数条那行字）
        /// ④ 容器上那颗 `CanvasGroup`（原版结构；今天没有 uGUI 子件，留着给将来）。</para></summary>
        public sealed class CellFade
        {
            internal CanvasGroup cg;
            internal CardView card;
            internal ImageQuad[] bars;
            internal Color[] barBase;
            internal Label[] labels;
            internal Color[] labelBase;
            /// <summary>当前不透明度（自检读它 —— 批处理里补间要手动推）。
            /// ⚠️ 出厂 = **1**（= 真实现场：刚建出来的格子是实心的；`AnimateIn` 会先把它们写成 0）。</summary>
            public float Alpha { get; private set; } = 1f;

            public void SetAlpha(float a)
            {
                Alpha = Mathf.Clamp01(a);
                if (cg != null) cg.alpha = Alpha;                   // ④ 原版那颗 CanvasGroup
                if (card != null) card.SetAlpha(Alpha);             // ① 卡面（自建 MeshRenderer）
                if (bars != null)                                   // ② 张数条底图
                {
                    for (int i = 0; i < bars.Length; i++)
                    {
                        if (bars[i] == null) continue;
                        var c = barBase[i]; c.a *= Alpha;
                        bars[i].SetTint(c);
                    }
                }
                if (labels != null)                                 // ③ 张数条文字
                {
                    for (int i = 0; i < labels.Length; i++)
                    {
                        if (labels[i] == null) continue;
                        var c = labelBase[i]; c.a *= Alpha;
                        labels[i].SetColor(c);
                    }
                }
            }
        }

        /// <summary>把一格挂上 `CellFade`（建完卡与张数条**之后**调 —— 它按当时的颜色记基线）。
        /// ⚠️ 卡体内的 `ImageQuad`（战斗态那两颗描边光）**要排掉**：它们已经吃 `CardView.SetAlpha`，
        /// 再乘一次就变成 α² </summary>
        static CellFade MakeCellFade(Transform cellT, CardView card)
        {
            if (cellT == null) return null;
            var f = new CellFade { card = card, cg = cellT.GetComponent<CanvasGroup>() };
            var barList = new List<ImageQuad>();
            var quads = cellT.GetComponentsInChildren<ImageQuad>(true);
            for (int i = 0; i < quads.Length; i++)
            {
                var q = quads[i];
                if (q == null) continue;
                if (card != null && q.transform.IsChildOf(card.transform)) continue;   // 见上：别乘两次
                barList.Add(q);
            }
            f.bars = barList.ToArray();
            f.barBase = new Color[f.bars.Length];
            for (int i = 0; i < f.bars.Length; i++) f.barBase[i] = f.bars[i].Tint;
            f.labels = cellT.GetComponentsInChildren<Label>(true);
            f.labelBase = new Color[f.labels.Length];
            for (int i = 0; i < f.labels.Length; i++)
                f.labelBase[i] = f.labels[i] != null ? f.labels[i].color : Color.white;
            return f;
        }

        /// <summary>自检口：拿一格的 `CellFade`（**与生产路径同一个方法**）。</summary>
        public static CellFade MakeCellFadeForTest(Transform cellT, CardView card) { return MakeCellFade(cellT, card); }

        /// <summary>建一格的入场补间（原版 `CollectionCard.AnimateIn`；四个参数逐条现读，见本节头部）。
        /// <para>顺序 = 原版：`alpha = 0` → 摆到 `y + 200` → `Sequence{ AppendInterval(order × 0.1) ·
        /// Append(fade 0→1 / 0.7) · Join(move 回落 / 0.7) }`。</para>
        /// <para>⚠️ `order` 传的是**本趟可见序列里的序号**（0、1、2…）—— 原版用 `GetSiblingIndex()`，
        /// 而我们的 `Viewport` 底下还留着池化的 `CardView`（同父级兄弟）⇒ 真取兄弟序会跳号。</para>
        /// <para>自检用法：`CardTween.Mode = Manual` → 本方法 → `CardTween.Advance(dt)`（批处理无帧循环）。
        /// 返回 `Tween`（`CardTween.Use` 的签名；自检读 `Duration()` 验「序号 → 延迟」）。</para></summary>
        public static DG.Tweening.Tween BuildCardAnimateInTween(Transform cellT, CellFade fade, int order)
        {
            if (cellT == null) return null;
            // 原版读的是 `canvas.transform.localPosition.y`（补间**开始前**那一次）⇒ 终值
            float targetY = cellT.localPosition.y;
            if (fade != null) fade.SetAlpha(CardAnimateAlphaFrom);        // ① `CanvasGroup.set_alpha(0)`
            var lp = cellT.localPosition;                                 // ④ `.From(y + 200, setImmediately: true)`
            lp.y = targetY + CardAnimateRise;
            cellT.localPosition = lp;

            var seq = DG.Tweening.DOTween.Sequence();
            // ② 原版 `AppendInterval(GetSiblingIndex() * 0.1)`
            DG.Tweening.TweenSettingsExtensions.AppendInterval(seq, order * CardAnimateDelayStep);
            // ③ 原版 `Append(canvas.DOFade(1, 0.7))` —— 换成一条同参数（0→1 / 0.7s）的数值补间，
            //    因为 `CanvasGroup` 传不到我们那两套世界空间渲染后端（见 `CellFade` 的 ①）。
            DG.Tweening.TweenSettingsExtensions.Append(seq, DG.Tweening.DOTween.To(
                () => fade != null ? fade.Alpha : CardAnimateAlphaTo,
                v => { if (fade != null) fade.SetAlpha(v); },
                CardAnimateAlphaTo, CardAnimateDuration));
            // ④ 原版 `Join(canvas.transform.DOLocalMoveY(y, 0.7).From(y + 200))`
            DG.Tweening.TweenSettingsExtensions.Join(seq,
                DG.Tweening.ShortcutExtensions.DOLocalMoveY(cellT, targetY, CardAnimateDuration));
            // ⚠️ Sequence 上只能挂 `Linear`（整条时间轴的缓动；见本节头部）—— 两条嵌套补间各自吃
            //    DOTween 默认 `OutQuad`，与原版「没写 `SetEase`」一致。
            return CardTween.Use(seq, DG.Tweening.Ease.Linear, cellT);
        }

        /// <summary>Cards 页的 `firstShow`（原版 `CollectionDisplay.firstShow`）：**第一屏播一次**入场动画。</summary>
        bool _cardsFirstShow = true;
        /// <summary>Styles 页的 `firstShow`（原版异画页是**另一个** `CollectionDisplay` 实例 ⇒ 自己一份）。</summary>
        bool _stylesFirstShow = true;
        /// <summary>自检读数：本进程真正**起过**几次入场动画（批处理 = 0，真 Play = 第一屏那一批）。
        /// 用途 = 钉住「批处理不播」那道闸（⛔ 播了整页卡会停在 `alpha = 0`）。</summary>
        public static int CardAnimateInCountForTest { get; private set; }

        /// <summary>给一格起入场动画（生产路径）。⚠️ 只在真 Play 下起 —— 批处理没有帧循环，
        /// DOTween 不推进，播了就是「整页卡停在 alpha 0 / y+200」（见本节头部偏离③）。
        /// <para>🔴 **`liveClip`：格子升起时会跨过视口上沿 ⇒ 必须逐帧重裁。**
        /// 原版 `Viewport` 挂的是 `RectMask2D`（真 mask，会随动的子件一起裁）；我们这条裁切是
        /// **把裁切框烘进卡的网格**（`CardView.ApplyClip`，局部系里的几何切刀）⇒ 卡一挪，刀口就**跟着卡走**
        /// ⇒ 不重裁的话第一排卡会整张画到页头空带上（原版被 mask 挡住了）。
        /// 只给「**终位离视口上沿不足 `CardAnimateRise`**」的那些格子挂（见两个调用点的判据）——
        /// 其余格子整张都在视口内，`CropLayer` 的「整块在视口内 ⇒ 一个字节都不动」那条快路径本来就没刀口。
        /// ⚠️ 代价：这一档是**逐帧** 解析裁切 + 切网格 + 被切的 TMP 要 `ForceMeshUpdate`
        /// （`CardView.ClipTextMesh`）——只发生在开场那 0.7 s、且只有跨上沿的那一排格子。
        /// ⚠️ **自检覆盖不到**（批处理不播）⇒ 只能真 Play 看。</para></summary>
        static void PlayCardAnimateIn(Transform cellT, CellFade fade, int order, CardView liveClip)
        {
            if (!Application.isPlaying) return;      // ⛔ 别删：批处理/编辑器下没有帧循环
            var seq = BuildCardAnimateInTween(cellT, fade, order);
            if (seq == null) return;
            if (liveClip != null)
            {
                // 逐帧重裁：刀口跟着卡走的话，第一排卡会画到视口外（见上面那段）。
                DG.Tweening.TweenSettingsExtensions.OnUpdate(seq,
                    () => { if (liveClip != null) liveClip.SetClipFromTree(); });
                // 收尾再裁一次（最后一帧的 `OnUpdate` 与终点可能差一帧）
                DG.Tweening.TweenSettingsExtensions.OnComplete(seq,
                    () => { if (liveClip != null) liveClip.SetClipFromTree(); });
            }
            CardAnimateInCountForTest++;
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
            // 🔴 **2026-10-24（A14 · 界面线第五批）**：每格多了一层容器 `Content <i>`（= 原版
            //    `Collection Card/Content`，判据见 `PunchCardCell` 那段）。**清理顺序不能反**：
            //    ① 先把池里的 `CardView` 从上一趟的容器里**摘出来挂回 `parent`**（它们要活到下一趟复用），
            //    ② 再销毁容器（连里面的张数条一起走）。
            //    ⛔ 反过来 = 把池里的卡连同容器一起销毁 ⇒ `_cellPool` 里全是 Unity 假 null，
            //       池化静默退化成「每趟全部重建」（自检那条「滚 100px 新建 0 张」会红）。
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var ch = parent.GetChild(i);
                if (ch.name.StartsWith("Content "))
                {
                    for (int j = ch.childCount - 1; j >= 0; j--)
                    {
                        var cc = ch.GetChild(j);
                        if (cc.GetComponent<CardView>() != null) cc.SetParent(parent, false);
                    }
                    DestroySafe(ch.gameObject);
                }
                else if (ch.name.StartsWith("Counter ") || ch.name.StartsWith("CardHit_"))
                    DestroySafe(ch.gameObject);
            }
            CardsCells.Clear();
            _cellUsed.Clear();
            CardsCellCreatedLastRefresh = 0;      // 自检读数：本趟真正建了几张（池命中不计）
            // 🆕 **2026-10-24（A18）**：原版 `CollectionDisplay.firstShow` ⇒ **只有第一屏播入场动画**
            //   （本趟读完就清；判据见上面「入场动画」那一节）。
            bool animateIn = _cardsFirstShow;
            int builtNo = 0;                      // 本趟**可见序列**里的序号（= 原版 `GetSiblingIndex()` 那一格）

            // 🔴 **2026-10-13（A435 阶段 2 · 乙 · A15）**：这里原来是一对「`Clip = CardsViewport;` →
            //   循环 → `Clip = prevClip;`」—— 迁移后**整对删掉**：裁切状态长在 `holder/Viewport` 那颗
            //   `ViewportClip` 上（`BuildCardsPage` 里 `ViewportClip.Hang` 建的），本循环里所有件
            //   （`CardView` 的 `SetPose` 实参、`AddHit` 转发的裸 `Clip`）都挂在它下面 ⇒ 沿父链解析得到同一份。
            var list = CardsState.VisibleCards();
            // 🔴 **2026-10-18（`W11`）**：卡的缩放按「**格高 − 张数条**」反解（原来是整格 384 ⇒ 卡底压住张数条，
            //   见 `CardsCardRect` 的 doc）。式子与卡组编辑窗 `DeckRuntime.Pool.CardH` **同一条**：
            //   345.728 ÷ (`CardView.Height` × 108) = **0.9609**。
            //
            // ⚠️ **2026-10-24（`E6`）补记：原版 prefab 那条式子与本式【不是同一条】—— 如实记在这里**。
            //   原版（反汇编逐条，`CollectionCard__OnRectTransformDimensionsChange` @ VA `0x1806D9980`）：
            //       `cardUI.localScale = sizeDelta.**y** × initialCardScale / initialHeight`
            //   其中 `initialCardScale = 150`、`initialHeight = 512`（`MonoBehaviour_40971030049256792.json`），
            //   作用对象是 `Content/CardUI`（其子 `2DCard` = 2.0927 × 3.3313 卡单位；出厂 `CardUI.scale = 150`
            //   对应出厂格高 512 ⇒ 自洽）。**读的是 `sizeDelta.y`（高度）不是 x** ——
            //   指令流是 `mov [rsp+0x70], rax`（Vector2 8 字节：x 在 +0x70 / y 在 +0x74）后
            //   `movss xmm6,[rsp+0x74]`，即取 y。据此刻度 = 384 × 150/512 = **112.5 px/卡单位**
            //   ⇒ 折成我们的世界缩放 = 112.5 ÷ 108 = **1.0417**（比本式大 8.4%）。
            //   ✅ **2026-10-25（`C` 域 · 调度台裁定）：改成按原版公式算，判据升级成 prefab 实测。**
            //   `menu_dump.py bundle_menus_assets_all "Collection Card" --depth 4` 现读（2026-10-25）：
            //     · 原型 `Collection Card` = **350×512**；
            //     · 卡体 `CardUI/2DCard` = **18, 6.2 → 332, 505.8**（313.91 × **499.70**）⇒ 高 **499.70/512 = 97.6%**、
            //       宽 **313.91/350 = 89.7%** —— 与公式推出的 **1.0417** 逐位吻合 ⇒ 公式就是原版卡体的真实大小；
            //     · 张数条 `Content/Counter` = **100, 461 → 250, 511**（150 × 50.01，`Image` `Simple (1,1,1,1)`
            //       + `preserveAspect`）⇒ 与我们的 `CardsCounterRect`（锚 `0.2857..0.7143 / 0.002..0.09967`）**逐位一致**；
            //     · ⇒ **卡体（到 505.8）与张数条（从 461 起）在 512 帧里本来就重叠 44.8px**（折成 384 格 = **33.6px**）
            //       —— **这是原版长相，不是缺陷**。张数条是**画在卡下缘之上**的一条。
            //   ⚠️ **实拍不能当尺子**（调度台 2026-10-25 明确）：用户参考图量的 81.7% / 82.8% 量的是**立绘/边框内沿**、
            //     不是 `CardUI` 外框；2DCard 外框是 **89.7% × 97.6%**。
            //   ⚠️ **`W11` 的真凶**是「按整格反解 + 没有公式给的居中口径」，不是「卡不该压条」；
            //     所以修法是**改缩放**，⛔ 不是把卡缩到条上面。全过程 → `资料/普查产出_1018/W11_卡组编辑计数重叠.md`。
            // 🔴 **原版 `CollectionCard.OnRectTransformDimensionsChange` 的直译**：
            //    `cardUI.localScale = sizeDelta.y * initialCardScale / initialHeight`
            //    = `384 × 150 / 512 = 112.5` px/卡单位 ⇒ 世界缩放 `112.5 / 108 = 1.0417`。
            //    ⛔ **不要再退回 `(格高 − 张数条高) / …` 那一版**（那是 W11 的过度修正，判据见上面那一段）。
            float scale = CardsCellH * 150f / 512f / 108f;
            for (int i = 0; i < list.Count; i++)
            {
                var content = CardsCellRect(i);
                var r = CardsScroll.Shift(content);
                if (!CardsScroll.Intersects(r)) continue;
                // 🔴 **2026-10-24（A14）**：每格一层容器 = 原版 `Collection Card/Content`
                //    （卡体 `CardUI` 与张数条 `Counter` 都挂在它下面；选中 punch 打的就是这一级 ——
                //     判据见 `PunchCardCell` 那段）。命中区 `CardHit_*` **不**放进来（原版的射线件在
                //     `Content` 的**上一层** `Collection Card` 上 ⇒ punch 时命中区不跟着动）。
                var cellT = Node(parent, "Content " + i, r);
                // 🆕 **2026-10-24（A18）**：原版 `Content` 上挂着 `CanvasGroup`（`CollectionCard.canvas`，`+0x60`
                //   ⇒ `AnimateIn` 的 `set_alpha` / `DOFade` 打的就是它）⇒ 我们也补一颗（出厂 alpha = 1，
                //   不播动画时零影响；**不吃射线** —— 原版那颗 `NonDrawingGraphic` 才参与 raycast，本工程没有 uGUI）。
                cellT.gameObject.AddComponent<CanvasGroup>();
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
                    v.transform.SetParent(cellT, false);         // 🆕 A14：从上一趟的容器搬进这一趟的（下一句 `SetPose` 会重写局部位姿）
                }
                else
                {
                    v = CardView.Create(cellT, data, "CollectionCard_" + i);
                    if (v == null) continue;
                    v.gameObject.SetActive(true);
                    _cellPool[key] = v;
                    _cellKeyOrder.Add(key);
                    CardsCellCreatedLastRefresh++;
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
                // 🔴 **2026-10-18（`W11`）**：位姿按**卡那块矩形**（= 格上沿到张数条上沿），⛔ 不是整格 ——
                //   整格居中 + 整格高反解 = 卡底压在张数条下面（见 `CardsCardRect`）。
                var cardR = CardsCardRect(r);
                v.SetPose(Local(cellT, cardR.x1, cardR.y1, cardR.x2, cardR.y2), 0f, scale);
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
                //   那句 `Create(cellT, data, …)` **做的是同一件事**，逐条核过（`Core/CardView.cs`）：
                //    · `Create` = `AddComponent` + `Build(d)`；`Build` 头一句就是 `Data = d`（`CardPresentation/Core/CardView.cs`），
                //      尾三句是 `BuildTextLayers(d)`（`CardPresentation/Core/CardView.cs`）· `SetBadges(d.badges)`（`CardPresentation/Core/CardView.cs`）·
                //      `BuildStatLayers(); SyncStatLayers();`（`CardPresentation/Core/CardView.cs`）。
                //    · `SetData(d)` 里那几段：`CaptureStatChanges` 非 `Board` 直接 `return`（`CardPresentation/Core/CardView.cs`）·
                //      `_face != null` 那支只在**没有卡框**的兜底路上才非空（真卡走 `_frame`）·
                //      `_info` / `_art` 重新赋的是**同一个缓存键算出来的同一张贴图**·
                //      `SetCardImageTo3DBase()` 在 `_body3D == null` 时 `return`（`CardPresentation/Core/CardView.cs`）·
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
                BuildCardsCounter(cellT, i, r, list[i]);
                // ⚠️ 闭包**别捕循环变量 `i`** —— 点击发生在重建之后，那时 `i` 已经是 `list.Count`（越界）
                var def = list[i];
                // 🆕 2026-10-23（`C3`）：点之前先来一下选中 punch（原版 `CollectionCard.SelectedHighlight(true)`）——
                //   幅度/时长/vibrato/elasticity 全在原版 `.rdata`（见 `PunchCardCell` 那段判据）。
                var cHit = AddHit(parent, "CardHit_" + i, r, QPageRow,
                                  // 🆕 2026-10-25（R13）：原版 `CollectionDisplay<T>.Select` 的第一道闸 ——
                                  // **列表正在被拖滚时点格子什么都不做**（判据 → `ListBusy` 的 doc）。
                                  () => { if (ListBusy(CardsScroll)) return; PunchCardCell(cellT); OpenCardDetail(def); }, clickCue: CardClickCue);
                AttachCardHover(cHit, v);                     // 悬停描边（原版 `CollectionCard.HoverHighlight`；徽记**不**走这条，见 `CheckFocusedArmy`）
                BuildWatch.CellPhase("⑤ 张数条（`Rect`+`Text`）+ 命中区（`AddHit`）");
                // 🆕 **2026-10-24（A18）**：入场动画（原版 `CollectionCard.AnimateIn`，四个参数见本节头部）。
                //   位置必须在**卡 + 张数条都建完之后**：`MakeCellFade` 按当时的颜色记基线。
                //   只在 `firstShow` 那一趟 + 真 Play 下起（批处理没有帧循环 ⇒ 见本节头部偏离③）。
                if (animateIn)
                {
                    var fade = MakeCellFade(cellT, v);
                    // 终位离视口上沿不足 200px ⇒ 升起时会跨出视口 ⇒ 要逐帧重裁（理由见 `PlayCardAnimateIn` 的 doc）
                    PlayCardAnimateIn(cellT, fade, builtNo,
                                      r.y1 < CardsViewport.y1 + CardAnimateRise ? v : null);
                }
                builtNo++;                       // 序号只数**本趟真建出来的格**（同原版「可见序列」的兄弟序）
                BuildWatch.CellEnd();
            }
            _cardsFirstShow = false;             // 原版 `CollectionDisplay.RefreshCollection` 的 `firstShow = false`

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
            // 🔴 **2026-10-25（`C` 收藏域）**：原版 `CollectionDisplay.RefreshCollection` 的尾巴是
            //   **延迟 0.1s 再调一次 `CheckFocusedArmy`**（`CardCollectionDisplay.RefreshCollection`，
            //   `DOVirtual.DelayedCall(0.1f, callback)`）——那 0.1s 是等布局稳下来，批处理里没有帧循环。
            //   我们每次重建（= 滚动 / 筛选 / 换语档 / 首次建页）末尾直接算一次，**语义等价**；
            //   `CardsScroll.OnChanged` 那条（= 原版 `OnListDrag`）另有一处，两处都指向同一个纯函数。
            CheckFocusedArmy();
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

            // 🔴 **2026-10-19（第五十七会话 · `N52③` 的同族尾巴）**：原版这颗标题挂在 **`Header` 之下** ——
            //   `Cardback Tab ▸ Header` 的孩子序 = `Filter Toggle` → `Separator Line` → **`label`** → `Filters`
            //   （判据 = `python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Cardback Tab" --depth 3` 现读）。
            //   我们原来把它建在 `page` 上（树形差一层）⇒ 本批换成 `Header` 的孩子、并把兄弟序挪到第 2 位。
            //   ⚠️ **换父不动一个像素**：`Text/Node/Rect` 走的是 `Local(parent, 世界矩形)`（同 `BuildDeckHeader` 那段说明）。
            var cosmoHdr = page.Find("Header");        // `BuildFilterHeader` 刚建出来的**直接孩子**
            if (cosmoHdr == null)
                Debug.LogWarning("[Collection] 卡背页 `Header` 容器取不到 ⇒ 标题 `Header Label` 退回挂页节点（如实出声）");

            // 标题那条 rect **宽是 0**（实测 `sd=(0,60)`、`m_HorizontalAlignment=4` = **Right**）
            // ⇒ **右对齐到 1821.01** 才是它的真值（A4 表里写「hFlush」**是错的**，已就地更正）。
            // y 真值 = **80.94 .. 140.94**（2026-09-24 订正；原来写的 10..70 是 `Content Area` 局部值）。
            // 🔴 **2026-10-18（第三轮整改 · 审查 P4）字走 `Loc.T`** —— 键 = 原版那颗 `label` 的
            //   `Localize.mTerm` 原文 `MenuCollection/Label/Cosmetics`（全库只 1 颗，就是本页这一点：
            //   `… > Cardback Tab > Header > label`）。英文列 = TMP 原文 `Your cosmetics collection`；
            //   中文列 = 「你的化妆品收藏」（2026-10-14 官方 zh-CN 表；原「你的装饰收藏」= `zh_CN.csv:25` 精确命中）。⛔ 节点名 `"Header Label"` 不动（自检按名找）。
            var title = Text(cosmoHdr != null ? cosmoHdr : page, Loc.T("MenuCollection/Label/Cosmetics"),
                             1200f, 1821.01f, 80.94f, 140.94f, 5, PageInk,
                             "Header Label", 38f);
            if (title != null)
            {
                title.SetRenderQueue(QPageText);
                title.AlignRightOn(LayoutSpace.FromPixel(1821.01f, 0f).x);
                // 原版序 = `Filter Toggle`(0) → `Separator Line`(1) → **`label`**(2) → `Filters`(3)；
                //   `BuildFilterHeader` 先把后三件建好了 ⇒ 新加的孩子停在末尾（index 3）⇒ 挪到 `Filters` 之前。
                if (cosmoHdr != null) title.transform.SetSiblingIndex(2);
            }

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
            CosmoScroll.Elastic = true;                 // 原版 `m_MovementType = 1`（同上一条，同窗五处同值）
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
                Material hoverMat = null;      // 🆕 N70 P0-5：卡背悬停要拨的那颗 SDF 材质（`_Outline.a`）
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
                            hoverMat = mat;
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
                var cbHit = AddHit(cell, "Hit", r, QPageRow,
                       // 🆕 2026-10-25（R13）：同一条「列表忙就不选」（原版那道闸在 `CollectionDisplay<T>.Select`，
                       // 四个页签共用 ⇒ 卡背页也吃它）。判据 → `ListBusy`。
                       () => { if (ListBusy(CosmoScroll)) return;
                               WindowsManager.OpenItemDisplay(ItemDisplayWindow.ForCardback(cbName)); });
                // 🆕 N70 P0-5：原版 `CollectionCosmetic.HoverHighlight` = 材质颜色 alpha 0.2s 补到 1/0。
                AttachOutlineHover(cbHit, hoverMat); DisableClickSound(cbHit);   // 原版 `Collection Cosmetic.onClickSound = null` ⇒ **不响**
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
            var f = CosmoState.Filter;
            // 🔴 **2026-10-25（多选筛选）**：阵营**集合非空 ⇒ 并集**（原来只读单值 `Faction`，
            //   多选接上之后单值字段是空的 ⇒ 这里不跟着改就会「点了筛不动」）。
            //   顺序仍按 `CardArt.CosmeticNames()` 的原始序（与 `NamesFor` 一致）。
            if (f.Factions != null && f.Factions.Count > 0)
            {
                var all = CardArt.CosmeticNames();
                var keep = new HashSet<string>();
                foreach (var fac in f.Factions)
                    foreach (var n in CardbackTable.NamesFor(fac, all)) keep.Add(n);
                var outp = new List<string>();
                foreach (var n in all) if (keep.Contains(n)) outp.Add(n);
                return outp.ToArray();
            }
            return CardbackTable.NamesFor(f.Faction, CardArt.CosmeticNames());
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
            StyleScroll.Elastic = true;                 // 原版 `m_MovementType = 1`（同上一条）
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
            //   本文件里唯一「圆底盘画在子件上」的就是**这一处**（`本文件`，现读）。
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
            var hit = AddHit(node, "ArrowHit " + side, r, QPageRow, onClick, arrowBg, "40k_general_bt_yellow",
                   clickCue: WindowSounds.DefaultCloseCue); if (AltStyles.Length == 1) { var wb = hit != null ? hit.GetComponent<WindowButton>() : null; if (wb != null) wb.Interactable = false; }   // 原版 `AlternateArtCardCollectionTab.Setup` 第⑤步：`alternateArtStyles.Count == 1 ⇒ 两颗钮 interactable = false`（本机 7 张异画全同一风格 ⇒ 这是常态，不是边角）
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
            // 🔴 **2026-10-18（T09 / `A1813`）**：池里的视图挂在**将要被销毁的页树**下 ⇒ 先清池，别留悬空引用
            //   （与 `RebuildCardsPage` 那一句同由）。
            _stylePool.Clear(); _styleKeyOrder.Clear(); _styleUsed.Clear();
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
            // 🔴 **2026-10-18（本批 · T09 / `A1813`）：池化** —— 只清「每趟重建」的命中区（`AltArtHit_*`）；
            //    `CardView` 留在 `_stylePool` 里复用。原来这里把 `parent` 下**所有孩子**（含一屏异画卡）
            //   `DestroySafe` 再逐格 `CardView.Create`，而 `StyleScroll.OnChanged` 的阈值只有 **0.01 px**
            //   ⇒ 拖一下 = 整屏重建（≈2.2 s/格）。修法与卡池页（`RebuildCardsCells`）同一套；
            //   判据与实测 → `资料/普查产出_1017/UI/PERF_T09_收藏窗性能诊断.md`
            // 🔴 **2026-10-24（A14）**：与卡池页同一套 —— 每格一层 `Content <i>` 容器（= 原版
            //    `Collection Card/Content`，异画页用的是同一个 cell prefab）。清理顺序同样**先摘卡、再销毁容器**
            //    （池化的 `CardView` 不能跟着容器一起走，理由见 `RebuildCardsCells` 那一行）。
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var ch = parent.GetChild(i);
                if (ch.name.StartsWith("Content "))
                {
                    for (int j = ch.childCount - 1; j >= 0; j--)
                    {
                        var cc = ch.GetChild(j);
                        if (cc.GetComponent<CardView>() != null) cc.SetParent(parent, false);
                    }
                    DestroySafe(ch.gameObject);
                }
                else if (ch.name.StartsWith("AltArtHit_")) DestroySafe(ch.gameObject);
            }
            StyleCells.Clear();
            _styleUsed.Clear();
            StyleCellCreatedLastRefresh = 0;      // 自检读数：本趟真正建了几张（池命中不计）
            // 🆕 **2026-10-24（A18）**：异画页自己一份 `firstShow`（原版异画页是**另一个** `CollectionDisplay`
            //   实例 ⇒ `firstShow` 各自一份；同卡池页，只有第一屏播）。判据见「入场动画」那一节。
            bool animateIn = _stylesFirstShow;
            int builtNo = 0;

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
                // 🔴 **2026-10-24（A14）**：格容器 = 原版 `Collection Card/Content`（punch 打这一级；
                //    命中区 `AltArtHit_*` 留在 `Content` 外面 —— 原版的射线件在上一层，punch 时不跟着动）。
                var cellT = Node(parent, "Content " + i, r);
                // 🆕 **2026-10-24（A18）**：同卡池页 —— 原版 `Content` 上那颗 `CanvasGroup`（入场动画的目标）。
                cellT.gameObject.AddComponent<CanvasGroup>();
                BuildWatch.CellBegin();                      // 🆕 A1628 第二轮：逐格相位（只在真 Play 出声）

                // 🔴 **2026-10-18（T09 / `A1813`）**：**按 `CardId + 语档` 复用**。
                //   同一个 id + 同一份语档 ⇒ `ToCardData(card, card.Faction)` 与
                //   `CardArt.AltArt(CardId)` 的产物逐字段相同 ⇒ 复用不必重算、不必 `SetData`
                //   （理由同 `RebuildCardsCells` 那一句，⛔ 别在这里再抄一份）。
                string key = a.CardId + "|" + (int)Loc.Current;
                CardView v;
                bool reused = _stylePool.TryGetValue(key, out v) && v != null;
                if (reused)
                {
                    v.gameObject.name = "CollectionAltArt_" + a.CardId;   // 停过的视图改回原名（自检按名找）
                    v.gameObject.SetActive(true);                          // ⚠️ 先激活：TMP 在未激活对象上量不出尺寸
                    v.transform.SetParent(cellT, false);                   // 🆕 A14：搬进这一趟的容器（`SetPose` 会重写位姿）
                }
                else
                {
                    var d = BattleDriver.ToCardData(card, card.Faction);
                    // 🔴 **立绘换成异画**（`CardData.artOverride`）—— 卡框/数值/名字照旧用原卡
                    d.artOverride = CardArt.AltArt(a.CardId);
                    if (d.artOverride == null)
                        Debug.LogWarning("[Collection] 异画立绘取不到（卡面会退回普通立绘）：" + a.CardId);

                    v = CardView.Create(cellT, d, "CollectionAltArt_" + a.CardId);
                    if (v == null) continue;
                    v.gameObject.SetActive(true);
                    _stylePool[key] = v;
                    _styleKeyOrder.Add(key);
                    StyleCellCreatedLastRefresh++;
                }
                BuildWatch.CellPhase(reused
                    ? "① `CardView` **复用**（池命中，无新建）"
                    : "① `CardView.Create`（建整张卡：各层 + TMP + `FitToBox` 二分 + `FitToWidth`）");
                // 🔴 **2026-10-08（A181）**：异画页同样要给卡带上裁切 —— 判据与卡池页那条逐字相同
                //   （原版 `Alternate Art Tab/Collection Display/Scroll View/Viewport` 的 `RectMask2D`
                //   `m_Softness=(0,0)`·`m_Padding=(0,0,0,0)`；视口 = 330.22,287.67 → 1920.01,1080）。
                //   🔴 **2026-10-13（A435 辛 · A774）**：边界改成**沿父链解析**（= 上面那颗
                //   `holder/Viewport` 上的 `ViewportClip`），原来传的是 `StyleView` 那个常量矩形
                //   ⇒ 第二份状态源。顺序与原由同卡池页那一处（`SetPose` 定完位姿再解析）。
                v.SetPose(Local(cellT, r.x1, r.y1, r.x2, r.y2), 0f, scale);
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
                _styleUsed.Add(v);
                // 🆕 2026-10-23（`C3`）：异画格与卡池格同一颗 punch（原版同一个 `CollectionCard.SelectedHighlight`）。
                var aHit = AddHit(parent, "AltArtHit_" + a.CardId, r, QPageRow,
                                  // 🆕 2026-10-25（R13）：同一条「列表忙就不选」（判据 → `ListBusy`）。
                                  () => { if (ListBusy(StyleScroll)) return; PunchCardCell(cellT); OpenCardDetail(card); }, clickCue: CardClickCue);
                AttachCardHover(aHit, v);      // 🆕 N70 P0-5：悬停描边 0.2s（同卡牌格）
                BuildWatch.CellPhase("⑤ 命中区（`AddHit`；本页**没有**张数条）");
                // 🆕 **2026-10-24（A18）**：入场动画 —— 与卡池页**同一条实现**（原版两页的格子都是
                //   `CollectionCard` ⇒ `AnimateIn` 也是同一个方法；只有 `firstShow` 各自一份）。
                if (animateIn)
                {
                    var fade = MakeCellFade(cellT, v);
                    // 同卡池页：终位离视口上沿不足 200px ⇒ 升起时会跨出视口 ⇒ 逐帧重裁
                    PlayCardAnimateIn(cellT, fade, builtNo,
                                      r.y1 < StyleView.y1 + CardAnimateRise ? v : null);
                }
                builtNo++;
                BuildWatch.CellEnd();
            }
            _stylesFirstShow = false;            // 同 `CollectionDisplay.RefreshCollection` 的 `firstShow = false`

            // 🔴 **2026-10-18（T09 / `A1813`）**：本趟没露头的视图 —— **只关不删**（滚回来零成本），
            //   并改名成 `CollectionAltArt_parked`：⛔ 别留着原来的 `CollectionAltArt_<卡 id>`，
            //   否则自检按 `Find` 找「这一格」时会先撞上屏外那颗（假绿/假红）。与卡池页那条同由。
            foreach (var kv in _stylePool)
            {
                var v = kv.Value;
                if (v == null || _styleUsed.Contains(v)) continue;
                if (v.gameObject.activeSelf) v.gameObject.SetActive(false);
                v.gameObject.name = "CollectionAltArt_parked";
            }
            // 上限回收：**最老且本趟没露头**的那个直接销毁（切风格/筛选来回切不会把整张异画表都建出来）。
            for (int k = 0; k < _styleKeyOrder.Count && _stylePool.Count > StylePoolCap; )
            {
                string key = _styleKeyOrder[k];
                CardView v;
                if (!_stylePool.TryGetValue(key, out v)) { _styleKeyOrder.RemoveAt(k); continue; }
                if (v != null && _styleUsed.Contains(v)) { k++; continue; }
                _stylePool.Remove(key);
                _styleKeyOrder.RemoveAt(k);
                if (v != null) DestroySafe(v.gameObject);
            }
        }

        /// <summary>自检用：重画异画页卡格一趟（**与 `StyleScroll.OnChanged` 调的是同一个函数**）。
        /// 用途 = 验「池化之后重画一趟**不新建任何 `CardView`**」（账 `A1813`）。
        /// ⚠️ 本页只有 7 张异画、2 行总高 768 &lt; 视口 792 ⇒ **滚不动**，所以自检不能靠滚动来触发。</summary>
        public void RebuildStyleCellsForTest()
        {
            var page = PageRoot(3);
            var holder = page != null ? page.Find("Scroll View") : null;
            if (holder != null) RebuildStyleCells(holder);
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
        /// （`act=T` ≠ 抽屉展开）⇒ 改成与其余三页一致；判据链见 `本文件` 那段。</summary>
        public bool StyleFiltersOpen { get { return _fltStyles != null && _fltStyles.Open; } }

        /// <summary>建一页的左侧筛选栏。**起手收起**（原版靠 `hiddenPosition` 滑出去；
        /// 🆕 A11 起我们也是「滑出去」—— 收起时整栏真的滑到屏幕左外（行程 = 原版
        /// `hiddenPosition.x − originalAnchorPosition.x` = **−385 px**），只是**已滑完**所以不画）。
        /// <paramref name="openAtStart"/> = 建出来就让抽屉停在**展开**那一头（`Slide = 1`）。
        /// ⚠️ **2026-10-05 更正（铁律 5）**：原文写「= 出厂就展开（**异画页是 act=T**）」—— 半句误读、
        /// 半句不成立：`act=T` 推不出展开，且**原版四页起手一律收起**（判据见 `:1150`）。
        /// 本文件现在**两个调用点都传 `false`**（`本文件` Cards · `本文件` Styles；Cosmo/Deck 各建各的）。
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
                _fltScroll.Elastic = true;                   // 原版 `m_MovementType = 1`（同上一条）
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
            // 🔴 万能卡计数条：**4 个数字 2026-09-28 起恒定 `99`**（用户拍板：缺 `WildcardDisplay` 的发放源）
            //    ⇒ 建页时已写死，这里不再重算。
            //    🆕 **2026-10-19（界面线尾巴 · 铁律 5 就地订正）**：原文写「而我们没有 hover」—— **已过期**：
            //    那颗**阵营徽记**（`Army Icon`）自 2026-10-19 起跟着「指针压在卡池格 / 开卡片详情」换图
            //    （`SetWildcardArmy`，判据 = `CardCollectionDisplay.CheckFocusedArmy`）；**只有那 4 个数字**
            //    仍恒定（发放源没有）。判据 → `项目任务.md` §三 第 15 条 第 29 项。
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
        /// `Deck/DeckRuntime.DrawerHome`（`CardPresentation/Deck/DeckRuntime.cs` 起）与它旁边那句 `PrepareDrawerForBuild`，调用点 `CardPresentation/Deck/DeckRuntime.cs` / `CardPresentation/Deck/DeckRuntime.cs`；
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
                    var cellBg = Rect(cell, c.IconOff != null && !c.On ? c.IconOff : c.Icon, b, "Background", QFltRow, CellTint(c), true);
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
                    var cellHit = AddHit(cell, "Hit", r, QFltHit, () => Scope(owner, () => ApplyFilter(key)),
                           clickCue: WindowSounds.SelectToggleCue);
                    // 🆕 **2026-10-23（`C3`）ColorTint 悬停/按下 —— 只给【开关那两行】**（Owned only / Upgradable only）。
                    //   判据：原版那两颗 `EverguildToggle` 是 `m_Transition = 1`、`HL = 0.9607843`、`fade = 0.1`、
                    //   `m_TargetGraphic` = 子件 `Image`；而 **Army / Rarity / Cost / Type 四族是 `m_Transition = 0`**
                    //   （`CollectionFilterToggle` 类里根本没有 pointer 处理）⇒ ⛔ 别把那四族也带上。
                    //   本处唯一的「开关族」判据 = `Cell.IconOff != null`（模型里只有 Owned/Upgradable 给了它，
                    //   与 `CellTint` 用的是同一个口 ⇒ 两处不会分叉）。
                    if (c.IconOff != null && cellBg != null && cellHit != null)
                    {
                        var cellWb = cellHit.GetComponent<WindowButton>();
                        if (cellWb != null) cellWb.TintOn(cellBg);
                    }
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
            GameObject inputBg = null;      // 🆕 2026-10-23（`C3`）：ColorTint 的目标图形（原版 `m_TargetGraphic`）
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
                inputBg = MenuDraw.Nine(cell, tex, r, new Vector4(bd, bd, bd, bd), 32f, 32f,
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
            var nameHit = AddHit(cell, "Hit", r, QFltHit, () => Scope(owner, () => ApplyFilter("$name")),
                   clickCue: WindowSounds.SelectToggleCue);
            // 🆕 **2026-10-23（`C3`）ColorTint 悬停/按下** —— 同 Deck 页那条（原版 `EverguildInputField`
            //   `m_Transition = 1`、`HL 0.9607843` / `PR 0.7843137` / `fade 0.1`，打在输入框底图上）。
            var nameWb = nameHit != null ? nameHit.GetComponent<WindowButton>() : null;
            if (nameWb != null && inputBg != null)
                nameWb.TintOn(inputBg.GetComponentsInChildren<ImageQuad>(true));
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
            //   （`CardPresentation/Editor/CollectionScene.cs`）与两条 `FindChild(fltPanel, "Title " + ttl)` 都**按名找**（名里还是英文原名）。
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
            // 🆕 2026-10-25：页对象已建出来 ⇒ 原版 lazy `SetupTabs` 那一跳（只跑一次）
            EnsureTabsSetup();
            // 🔴 **2026-10-19（`N117` · 用户拍板「重开收藏窗保留当前页」）：原来这里恒 `Click(0)`
            //   ⇒ **每次 `Open()` 都回第 0 页**（`Hide` 之后被 `ShowPreviousWindow` 带回来也一样）。
            //   判据（原版 `GameWindowWithTabs__Open.c:9-21`）：**当前页签**（字段 **0x12**）非空就只重开那一页；
            //   只有它是**空**（= 新建实例）才走 `GetStartingTab()`（虚表 `0x218`，回落 = `tabs` 的
            //   `FirstOrDefault` = 第一页）。而 `WindowTabBase__CloseTab.c` 只 `SetActive(false)`、
            //   `GameWindowWithTabs__Close` 也不写 0x12 ⇒ **隐藏 → 带回来这一路原版保留当前页**。
            //   ⇒ 我们照它：`CurrentTab != None` 就点回**同一个页签**；新实例仍是第 0 页（不造第二个回落值）。
            //   ⚠️ 为什么必须是 `Click(visualIndex)` 而不是 `ChangeTab(CurrentTab)`：左栏高亮读的是
            //   `TabButtons.CurrentVisualIndex`（`RefreshHighlights`），只调 `ChangeTab` 会让高亮停在第 0 键上。
            int startVisual = StartVisualIndex();
            if (tabButtons != null) tabButtons.Click(startVisual);   // 原版 `GetStartingTab` 的回落 = 第一页（新实例才走）
            BuildWatch.Mark("② `tabButtons.Click(" + startVisual + ")`（保留当前页 / 新实例落第一页）");
            if (Application.isPlaying) StartCoroutine(BuildWatch.ReportAtFrameEnd("开收藏窗"));
        }

        /// <summary>🔴 **2026-10-19（`N117`）**：`Open()` 该落在左栏第几颗 —— **唯一判据**。
        /// 原版 `GameWindowWithTabs.Open`（`decomp_full/GameWindowWithTabs__Open.c:9-21`）：
        /// 当前页签（字段 `0x12`）**非空 ⇒ 重开它**；**空 ⇒ `GetStartingTab()`**（虚表 `0x218`，
        /// 回落 = `tabs` 的 `FirstOrDefault` = 第一页）。`Hide()` / `WindowTabBase.CloseTab()` 都**不清** `0x12`
        /// ⇒ 「藏起来 → 被 `ShowPreviousWindow` 带回来」这一路**保留当前页**。
        /// ⚠️ 例外面（如实记）：若 `CurrentTab` 不在本窗的 `visualTypes` 里（绝不该发生），回落第 0 页。</summary>
        int StartVisualIndex()
        {
            if (CurrentTab == WindowTabType.None) return 0;
            int keep = visualTypes != null ? visualTypes.IndexOf(CurrentTab) : -1;
            return keep >= 0 ? keep : 0;
        }

        /// <summary>自检读口（静态断言用：⛔ 不为此真建一遍整窗）—— 见 <see cref="StartVisualIndex"/>。</summary>
        public int StartVisualIndexForTest { get { return StartVisualIndex(); } }

        /// <summary>本窗左栏四颗的**唯一出处**（= `visualTypes` 的内容）。`Build()` 与 `Create()` 都调它。</summary>
        void ApplyVisualTypes()
        {
            if (visualTypes == null) return;
            visualTypes.Clear();
            visualTypes.Add(WindowTabType.CollectionDecks);
            visualTypes.Add(WindowTabType.CollectionCards);
            visualTypes.Add(WindowTabType.CollectionCosmetics);
            visualTypes.Add(WindowTabType.CollectionStyles);
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
            {
                // 🔴 **2026-10-23（`C3`）命中区照原版**：可点 = 本颗 `Image`（`40k_menu_bt`，`m_RaycastTarget = 1`，
                //   **`m_RaycastPadding = (-10,-10,-15,-10)`**）∪ `label`（TMP，`m_RaycastTarget = 1`、pad 0）
                //   ⇒ x **357.2…587.2** · y **78.4…148.4**（核查报告 §2·3）。原来那块是 367.2…637.2 × 50：
                //   左边/上下各少 10，右边多出 50（**原版点不到**的那一块）。
                //   ⚠️ 走 `MenuDraw.PaddedRect` + `Union`（符号约定「正 = 缩、负 = 扩」；⛔ 别手写第二份 pad 算式）。
                //   先各自 pad 再取并集（顺序见 `Union` 的注释）。
                var fltHitR = MenuDraw.Union(
                    MenuDraw.PaddedRect(new PxRect(FltBtnX, FltBtnY, FltBtnX + FltBtnS, FltBtnY + FltBtnS),
                                        new Vector4(-10f, -10f, -15f, -10f)),
                    fltLabR);
                var fltHit = AddHit(filterToggle, "FiltersHit", fltHitR, QPageRow, onToggle, clickCue: pageNo == 0 ? WindowSounds.SelectToggleCue : null);   // 判据：原版四页 `Filter Toggle` 的 `m_onClickSound` 逐颗实读 —— Deck 页那颗（`Select Deck Tab ▸ Header ▸ Filter Toggle`，MB 根 `709345757288850133`）= `Select Toggle`（pid 8881786964274599484）；Cards / Cardback / Alternate Art 三页 = `{0,0}` 且 `useDefaultAudioClipIfNull=1` ⇒ 默认 `ButtonClick`（= 我们 `clickCue:null` 那一支）。
                // 🆕 **ColorTint 悬停/按下** —— 原版 `EverguildToggle.m_Transition = 1`、
                //   `HL = 0.9607843` · `PR = 0.7843137` · `fade = 0.1`、**`m_TargetGraphic` = 本颗 `Image` 自己**。
                //   本仓默认值恰好就是这三个（`WindowButton.HighlightKey/PressedKey/FadeSeconds`）⇒ 只补 `target`。
                //   ⚠️ 用 `TintOn` 而不是 `Bind`：原版 `m_SpriteState` 的 hover/pressed 两格**空着**
                //   （只有 `on/offSprite` 那对按值换图，见 `RefreshFilterToggle`），`Bind` 会把它当换图档、关掉色偏。
                var fltWb = fltHit != null ? fltHit.GetComponent<WindowButton>() : null;
                if (fltWb != null && btnQ != null) fltWb.TintOn(btnQ);
            }

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
                              //   🔴 **2026-10-21（`A1262`）**：这个 1.516em 是**渲出比 = face 行盒 ÷ 汉字墨高**
                              //   （1.437 ÷ 0.948）；**原版那份 CJK 资产带 `m_FaceInfo.m_Scale = 0.9` ⇒ 有效行盒 1.293em**
                              //   （同一份资产我们原来写的是 `m_Scale: 1`，**2026-10-21 起已对齐成 0.9**）。
                              //   ⚠️ 但 **1.516 这个比值不变**（分子分母同乘 `m_Scale`）⇒ 本处那个「高 +2.31 未跟」
                              //   的结论**照旧成立**，别因为对齐了字段就把 1.516 改成 1.293。
                              //   判据/修法 → `资料/普查产出_1021/W61b_界面_A1262_mScale.md`。
                              wrapPx: 221.44f, autoMinPx: 10f, autoMaxPx: 42f, autoBasePx: 12f);
            if (clrLab != null)
            {
                clrLab.SetRenderQueue(QPageText);
                clrLab.SetWrappingMode(0);      // 原版 `m_TextWrappingMode = 0`
            }
            Transform clrHit = null;
            if (onClear != null)
                // A17：原版 `…>Clear Filter Button` 是 SpriteSwap（普查 §块 3 第 4 行；我们 1 颗盖原版 3 颗）
                // 🔴 **2026-10-23（`C3`）**：命中区按原版 `Image.m_RaycastPadding = (-10,-10,-10,-10)` **四边各外扩 10**
                //   （原来 = 精确矩形 250×60）。仍走 `MenuDraw.PaddedRect`（负值 = 扩），⛔ 别手写第二份 pad 算式。
                clrHit = AddHit(filtersGrp, "ClearFiltersHit",
                       MenuDraw.PaddedRect(new PxRect(ClearFltX, ClearFltY, ClearFltX + ClearFltW, ClearFltY + ClearFltH),
                                           new Vector4(-10f, -10f, -10f, -10f)), QPageRow, onClear,
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

        /// <summary>本窗 `Content Area/Tab Buttons` 的顶边（**原版 158.64**）—— 左栏**底板**（`Tab Buttons/Background`
        /// 的 `Gradient2`）与 `Shadow` 都从这一行起。
        /// <para>判据 = `工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 5`：
        /// `Tab Buttons` = **167.2,158.6 → 332.2,1080**（165×921.36）· `Tab Buttons/Shadow` = **167.2,158.6 → 214.8,1080**。
        /// ⚠️ **本窗与奖励窗/商店不同**（那两扇的 `Tab Buttons` 铺满 `Content Area`，从 `ContentT`=70.94 起）
        /// —— 铁律 5·c 那一类，⛔ 别把这个数搬去基类（基类默认值仍是 `ContentT`）。</para>
        /// <para>🔴 **2026-10-23（W62 · 底板）**：现在服务 <see cref="BarTop"/>（条顶边）这一个覆写 ——
        /// `BarShadowTop` 已在基类改成跟 `BarTop` 走（本窗不再单独覆写）。
        /// 原来那份「在子类里删掉重建 `Shadow`」的临时补丁已删（正解 = 基类那几个判据）。</para></summary>
        const float TabBarTop = 158.64f;

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
            // 🔴 **2026-10-19（`N117`）**：这段抽成 `ApplyVisualTypes()`，并在 `Create()` 也调一次 ——
            //   因为「重开保留哪一页」的判据（`StartVisualIndex`）要在**没 Build 过**的实例上也能判
            //   （自检的静态断言就靠它，⛔ 别为此真建一遍整窗）。
            ApplyVisualTypes();

            var res = BuildShell(transform, Buttons, "CollectionTabButton_", "Tabs", lastIsMaster: false);
            // ══════════════════════════════════════════════════════════════════════════════════
            //  🔴 **2026-10-23（W62 · 界面线）：本窗左栏 `Shadow` 的「重建补丁」已删** —— 正解在基类。
            //  原来的病灶（真机截图 `_tmp_view/shots/2026-10-22_第六十二会话/02_collection.png` +
            //    裁图 `45_subnav_crop.png`）：主左栏与子导航列之间有一条**竖向浅白带**
            //    （设计 px x≈167..232 · y≈368..1079）—— `40k_main_tab_shadow` 本体是**纯白 + alpha 渐变**
            //    （51×4、左 255 → 右 3），原版靠那颗 `Image.m_Color = (0,0,0,0.475)` 才变成黑阴影，
            //    而基类建它时**没给 tint** ⇒ 屏上是一条白带。
            //  **现在**：判据收进 `MainMenuSubmenuWindow` 的可覆写口 —— `BarTop`（条顶边 158.64）·
            //    `BarShadowTop`（基类已改成跟 `BarTop` 走）· `BarShadowTint`（基类默认 = 原版 `(0,0,0,0.4745098)`），
            //    三个都在**上面那几个覆写/基类**里给值，`Shadow` 与底板由基类**各只画一次**。
            //    ⛔ 别在本窗再删掉重建一份（会变成两处生产者）。
            //  🔴 **2026-10-23（W62 · 底板）**：本轮再补一块 —— `Tab Buttons` **自己的 rect** 原来从 `ContentT`(70.94)
            //    起，原版是 **158.64..1080** ⇒ 底板比原版**高 88 px**。改法 = `BarTop` 覆写（`TabBarTop`）。
            //  判据全文 → `资料/普查产出_1022/W62_换牌压暗与收藏白条修复.md` §②。
            // ══════════════════════════════════════════════════════════════════════════════════
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
                // 🔴 **2026-10-18（N58）**：原版这颗 `Close Button` 画在左栏底图（`Tab Buttons`，QPanel=3005）**之上**
                //   —— 判据 = 原版父子序 `Tabs/Shared/Close Button` 在 `Tab Buttons` 之后、且它自己的矩形
                //   `192.2..342.2` 与左栏 `167.2..332.2` 交叠 140px，原版实拍里金框完整可见。
                //   我们原来与左栏同队列（QPanel）⇒ **右侧约 10px 被左栏盖住**（截图 `03_collection_cards.png`）。
                //   抬到 `QContent`（= `Tabs` 页内容那一档，3010）：左栏 3005 < 3010。
                if (cq != null) cq.SetRenderQueue(QContent);
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
                // 🆕 **2026-10-22（W64 返回链 · D2）**：原来这里写死 `() => Close()` ——
                //   原版这颗 Back 钮**不是无条件关窗**：`CollectionScreen.BackButtonClick` 先按「当前页签对象」
                //   分派（Deck 页签 ⇒ 交给 `SelectDecksTab.BackButtonClicked`，其余三页走通用返回 = 关窗/上一层）。
                //   ⇒ 改走 `OnBackClicked()`（分派逻辑与判据只在那一处）。
                AddHit(transform, "CloseHit", cr, QPanel, OnBackClicked, cq, "UI_Button_Mulligan",
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
            _deckCreateBtn = newQ != null ? newQ.transform : null;
            _deckImportBtn = impQ != null ? impQ.transform : null;
            _deckUnlock = BuildUnlockPlaceholder(ctrl);   // 组内**最左**那一格（原版画不出东西，只建结构）
            // 🆕 **2026-10-22（W64 · 卡组双模式）**：模式页的显隐矩阵里有 `Filter Toggle` 与 `Filters` 两颗
            //   （§5·3：模式页把它们 `SetActive(0)`，列表页 `SetActive(1)`；`Header` / `Control Buttons` /
            //   `Separator Line` **不动**）⇒ 存引用。两颗都是 `header` 的**直接孩子**（`BuildFilterHeader` 建）。
            _deckFilterToggle = header.Find("Filter Toggle");
            _deckFiltersGrp = header.Find("Filters");

            // 🆕 A17：这三颗原版都是 SpriteSwap，高亮图都 = `<常态图>_hover`（普查 §块 3 第 1～3 行）
            //   🆕 2026-10-22（W64）：**返回值存下来** —— 模式页要按原版 `SetActive(0)` 掉 `Create`
            //   （`Import` 那一颗**保持显示**，§5·3）。
            _deckImportHit = AddHit(ctrl, "ImportHit", new PxRect(ImportX, HdrBtnY, ImportX + HdrBtnW, HdrBtnY + HdrBtnH), QPageRow,
                   () => OpenImportPopup(), impQ, "UI_Button_Mulligan", clickCue: WindowSounds.PositiveCue);
            _deckCreateHit = AddHit(ctrl, "CreateHit", new PxRect(CreateX, HdrBtnY, CreateX + HdrBtnW, HdrBtnY + HdrBtnH), QPageRow,
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
        Transform BuildUnlockPlaceholder(Transform ctrl)
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
            return un;    // 🆕 2026-10-22（W64）：模式页要按原版把它 `SetActive(0)`（§5·3）
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
            _decksTabNode = decksTab;      // 🆕 2026-10-22（W64）：模式页显示时要把它整颗关掉（§5·3 第一行）
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
            DeckScroll.Elastic = true;                  // 原版 `m_MovementType = 1`（同上一条）
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

            // 🆕 **2026-10-22（W64 · 卡组双模式）**：第二子页 = 「选择游戏模式」页。
            //   原版 `m_Children` 序 = `Decks Tab` → **`Select Game Mode Deck Tab`** → `Header`
            //   （W64_原版规格 §1·2 / §1·5 逐级现读）⇒ 先建，排位在下面 `Header` 挪到最后之后再做。
            BuildDeckModePage(page);

            // 🔴 **`Header` 必须是 `Select Deck Tab` 的【最后一个孩子】**（原版 `m_Children` 序 =
            //   `Decks Tab` → `Select Game Mode Deck Tab` → **`Header`**）。
            //   ⚠️ **不能靠「调整 `CollectionTabPage.Setup()` 里的调用次序」** —— `BuildDeckHeader` 必须排在
            //   `BuildDeckList` **之前**（页头要先把 `_hdrFltBtn[0]` 等引用建出来，`RefreshFilterToggle`
            //   那条兜底才读得到；见 `CollectionWindow.Build()` 里「①c 收尾刷」那段注释），
            //   而且那个文件**不在本批白名单**里 ⇒ 在这里显式挪序。
            var hdr = page.Find("Header");
            if (hdr != null) hdr.SetAsLastSibling();
            // 🔴 **2026-10-22（W64 修）：模式页紧跟 `Decks Tab`** —— 序是「`Header` 先建（`BuildDeckHeader`）、
            //   `Decks Tab` 后建」，所以**不能写死 index 1**（那时 `Header` 还占着 0 ⇒ 挪完再 `SetAsLastSibling`
            //   会把模式页挤到 index **0**，实测自检期望 [1] 实得 [0]）。按 `Decks Tab` 的实际下标 +1 取。
            if (_deckModePage != null && _decksTabNode != null)
                _deckModePage.SetSiblingIndex(_decksTabNode.GetSiblingIndex() + 1);

            // 🆕 2026-10-22（W64）：**进 Deck 页先出模式页**（原版 `ChooseTabToOpen` 在
            //   `IOwnDeckPlayEvent` 数 ≥2 时走 `ToggleSelectGameModeDeck`，§5·1）。
            //   离线等价：本地零 liveops 事件 ⇒ 固定两张卡（见 `DeckModeCards`），**这一步就是那个等价物**。
            //   ⚠️ 重建（切语档 → `Build()`）时按 `_deckModeFilter` 还原：选过模式就停在列表，
            //     没选过才重新停在模式页（原版靠 `SelectDecksTab.currentTab` 记住，§5·1 首段）。
            SetDeckModeChrome(_deckModeFilter < 0);
        }

        // ============================================================ 🆕 2026-10-22（W64）：「选择游戏模式」子页
        //
        // 出处（唯一判据）：`资料/普查产出_1022/W64_卡组双模式_原版规格.md`（下称 §x —— 逐条带 bundle 字段 / PID）
        //   ＋ `资料/普查产出_1022/W64_卡组双模式_资产落点.md`（下称「落点 §x」）。
        //   原版节点 = `bundle_menus_assets_all ▸ Collection Menu Variant ▸ Content Area ▸ Tabs ▸
        //   Select Deck Tab ▸ Select Game Mode Deck Tab`（组件 `SelectGameModeDeckTab`）。
        //
        // 🔴 **这一页在原版是「事件清单渲染器」**：出厂 `Content` 0 子，卡由
        //   `SelectGameModeDeckTab.Initialize()` 按 liveops 事件逐颗 `Instantiate` 出来（§3·1），
        //   张数 = 事件数。本地**零事件实例**（§5·4）⇒ 卡的内容是**离线等价物**，逐条：
        //   · **两张 / 左右顺序（左小冲突 · 右经典）= 用户参考图**
        //     `C:/Users/qjh36/Desktop/warpforge参考图/卡组-选择游戏模式的参考图.png`
        //     （落点 §1·3 明写：顺序在原版由服务端事件列表决定，这里按参考图取）；
        //   · **卡名** = 官方词条 `MainMenu/GameMode/{Skirmish,Classic}` = 小冲突 / 经典（落点 §4·2）；
        //   · **卡图** = `40k_main_GameMode_Skirmish` / `Container_Image_Ranked`（落点 §1·1：与参考图逐像素一致）。
        //   ⛔ 其余**一律照预制体**：几何 / 字号 / 颜色 / 九宫 / 滚动 / 显隐（铁律 3：截图只用来验收）。

        /// <summary>这一页的根（原版 `Select Game Mode Deck Tab`）——
        /// **330.79,155.94 → 1920.01,1080**（§1·3 `menu_rect.py` 现读，1589.21 × 924.06）。</summary>
        public static readonly PxRect DeckModePageRect = new PxRect(330.79f, 155.94f, 1920.01f, 1080f);

        /// <summary>标题 `Title Header`：**426.07,155.94 → 1824.73,243.50**（1398.6526 × 87.5574，§1·3 / §2·2）。
        /// ⚠️ 那个 1398.65 **不是**按锚点算出来的（父件宽 1752.84 的 79.8%）—— 原版就是手填的数（§八·12）。
        /// 锚点 = 顶部居中 · pivot (0.5,1) ⇒ 与父件同顶。</summary>
        public static readonly PxRect DeckModeTitleRect = new PxRect(426.07f, 155.94f, 1824.73f, 243.50f);

        /// <summary>`Scroll View`：**330.79,260.90 → 1920.01,1063.20**（1589.21 × 802.30，§1·3 / §4）。
        /// ⚠️ 纵向锚点不是 0/1 —— 原版 `aMin.y = 0.0181810` / `aMax.y = 0.8864095`（§1·4）⇒ **按解算后的矩形**建。</summary>
        public static readonly PxRect DeckModeScrollRect = new PxRect(330.79f, 260.90f, 1920.01f, 1063.20f);

        /// <summary>`Content` 解算矩形（§1·3）：**y 261.05 → 1063.05（高 802.00）**；
        /// 宽 = `ContentSizeFitter(Horizontal=PreferredSize)` 跑完的 **1061.86**
        /// （= 2 × 505.9268 + 50，§3·4）⇒ 中心 anchor ⇒ 两张卡整体居中在视口里。</summary>
        const float DeckModeContentT = 261.05f, DeckModeContentB = 1063.05f;

        /// <summary>模式卡 `Game mode deck selector`：**505.9268 × 802.6741**（§3·2 prefab 原值）；
        /// `HorizontalLayoutGroup.m_Spacing = 50` · `MiddleLeft` · padding 0 · `childControl W/H = 0`
        /// ⇒ 子件用它自己的 `sizeDelta`（§3·4）。</summary>
        public const float DeckModeCardW = 505.9268f, DeckModeCardH = 802.6741f, DeckModeCardGap = 50f;

        /// <summary>`Background/Foreground Image` 的 `sizeDelta.y` —— `AspectRatioFitter(HeightControlsWidth, 1)`
        /// 把它撑成 **卡高 + 0.41 = 803.08** 的方图（§3·2 解析值；落点 §1·2 几何自洽那一段）。</summary>
        const float DeckModeArtSidePad = 0.40997f;

        /// <summary>卡片根上的**层队列**（原版兄弟序 `Background → Foreground → TextDarkening → Border`，§3·2）。
        /// ⚠️ 分层只能靠**渲染队列**（`ImageQuad` 全透明队列、同队列 + 同 z 谁盖谁不可控 —— CLAUDE.md §三）；
        /// 四层各占一档 ⇒ 从 `QPageRow` 起 3032..3035（仍在 `QFlt`(3040) 之下、`QPageText`(3033) 那一带）。</summary>
        const int QModeArt = QPageRow, QModeBand = QPageRow + 1, QModeTitle = QPageRow + 2,
                  QModeBorder = QPageRow + 3, QModeHit = QPageRow + 4;

        /// <summary>标题字色 **rgb(254,153,0) = #FE9900**（§2·2：真值来自 `fontPreset = Pragati Regular Orange`
        /// 的 `fontColor.rgba = 4278229502`；⚠️ TMP 自己的 `m_fontColor` 是**白色**，那是出厂默认、被 preset 覆盖）。</summary>
        public static readonly Color DeckModeTitleInk = new Color(254f / 255f, 153f / 255f, 0f, 1f);

        /// <summary>一张模式卡 = 一个模式（`RuleEngine.GameMode` 的数值：经典 0 / 小冲突 13）。
        /// 🔴 **顺序 = 用户参考图**（左小冲突 / 右经典；原版顺序由服务端事件列表决定，落点 §1·3）。
        /// `Term` = 官方词条（落点 §4·2「文本来源 = 事件 label」的离线等价物）。</summary>
        public struct DeckModeCard
        {
            public int Mode;
            public string Art, Term;
        }
        public static readonly DeckModeCard[] DeckModeCards =
        {
            new DeckModeCard { Mode = (int)GameMode.Skirmish, Art = "40k_main_GameMode_Skirmish",
                               Term = "MainMenu/GameMode/Skirmish" },
            new DeckModeCard { Mode = (int)GameMode.Classic,  Art = "Container_Image_Ranked",
                               Term = "MainMenu/GameMode/Classic"  },
        };

        // ---- 运行时状态 ----
        /// <summary>模式页那一棵（`Select Game Mode Deck Tab`）。null = 这一页还没建（懒建）。</summary>
        Transform _deckModePage;
        /// <summary>`Decks Tab`（列表那一棵）—— 模式页显示时整颗 `SetActive(0)`（§5·3）。</summary>
        Transform _decksTabNode;
        /// <summary>页头里按 §5·3 要跟着模式页开合的几颗。</summary>
        Transform _deckCreateBtn, _deckCreateHit, _deckImportBtn, _deckImportHit,
                  _deckUnlock, _deckFilterToggle, _deckFiltersGrp;
        /// <summary>模式页那个**横向**滚动区（§4：H1/V0 · Elastic · inertia · deceleration 0.135 · sensitivity 1.0 · 无滚动条）。
        /// 今天两张卡宽 1061.86 &lt; 视口 1589.21 ⇒ `AdjustBounds` 语义下**滚不动**（与原版一致）。</summary>
        public MenuScroll DeckModeScroll;

        /// <summary>当前筛的卡组模式（`-1` = 不筛 —— 原版 `DeckCollectionTab.currentEvent == null` 那一档，§5·5·4）。</summary>
        int _deckModeFilter = -1;

        /// <summary>模式页现在显示着吗（自检/诊断口）。</summary>
        public bool DeckModePageShown { get { return _deckModePage != null && _deckModePage.gameObject.activeSelf; } }
        /// <summary>当前筛的模式（`-1` = 不筛）。</summary>
        public int DeckModeFilter { get { return _deckModeFilter; } }
        /// <summary>模式页那棵树（自检按路径取节点用；未建 ⇒ null）。</summary>
        public Transform DeckModePageRoot { get { return _deckModePage; } }

        /// <summary>建「选择游戏模式」子页（节点树 = §1·3 那四层 + 两张卡）。
        /// <para>树（逐层照原版）：`Select Game Mode Deck Tab` ▸ `Title Header` ·
        /// `Scroll View` ▸ `Viewport`(RectMask2D) ▸ `Content`(HLG 50) ▸ `Game mode deck selector` ×2；
        /// 每张卡四层 `Background Image → Foreground Image → TextDarkening(▸ Event Title) → Border`（§3·2）。</para>
        /// <para>⚠️ 本工程**没有 uGUI**（没有 Canvas / LayoutGroup，见 `BuildDeckHeader` 那段等价性论证）
        /// ⇒ HLG / CSF / AspectRatioFitter 一律**按解算后的结果摆**，⛔ 不挂组件（挂了永远不跑）。</para></summary>
        void BuildDeckModePage(Transform page)
        {
            var root = Node(page, "Select Game Mode Deck Tab", DeckModePageRect);
            _deckModePage = root;

            // ---- ① `Title Header`：fs 72（auto 18–72）· Center/Capline · #FE9900 · raycast=0（§2·2）----
            //   本地化键 `MenuDeck/SelectDeckGameMode`（官方表 = `SELECT GAME MODE` / 「选择游戏模式」）；
            //   ⚠️ prefab 的 `m_text` 是 `SELECT GAME MODE DECK`（占位串）—— ⛔ 别把它写进代码（§2·2 末段）。
            //   ⚠️ 字体：原版 `Pragati-Regular SDF` + 一层**全局 CJK 回退**（那份回退表本地没查到，落点 §4·3）
            //      ⇒ 我们传拉丁面，`Label.ActualFace` 在文本含汉字时自动回 CJK（它自己的口径，见 `Core/TmpFont`）。
            var title = Text(root, Loc.T("MenuDeck/SelectDeckGameMode"),
                             DeckModeTitleRect.x1, DeckModeTitleRect.x2, DeckModeTitleRect.y1, DeckModeTitleRect.y2,
                             5, DeckModeTitleInk, "Title Header", 72f,
                             // 四格 = 原版 `m_enableAutoSizing 1` / `min 18` / `max 72` / `base 36`（§4·1）
                             wrapPx: DeckModeTitleRect.W, autoMinPx: 18f, autoMaxPx: 72f, autoBasePx: 36f,
                             face: TmpFont.Face.LatinCardBody);
            if (title != null)
            {
                title.SetRenderQueue(QPageText);
                // 原版 `m_VerticalAlignment = 8192`（Capline）· `m_HorizontalAlignment = 2`（Center，本类默认就是居中）
                title.SetVAlign(Label.VAlign.Capline, LayoutSpace.Px(DeckModeTitleRect.H));
                // 原版 `m_RaycastTarget = 0` —— 我们没有「TMP 吃射线」这回事（`Label` 不是 `Graphic`）⇒ 天然等价。
            }

            // ---- ② `Scroll View ▸ Viewport ▸ Content`（§1·3 / §4）----
            var scrollNode = Node(root, "Scroll View", DeckModeScrollRect);
            // Viewport 上原版挂 **`RectMask2D`**（padding 0 / softness 0），**没有 Image**（§1·5 / §4）
            // ⇒ 用本仓的等价物 `ViewportClip.Hang`（= `RectMask2D` 的渲染 + 射线两面的实现）。
            var vp = ViewportClip.Hang(scrollNode, "Viewport", DeckModeScrollRect, Vector4.zero, Vector2Int.zero);

            float contentW = DeckModeCards.Length * DeckModeCardW + (DeckModeCards.Length - 1) * DeckModeCardGap;
            var contentRect = new PxRect(DeckModeScrollRect.CX - contentW * 0.5f, DeckModeContentT,
                                         DeckModeScrollRect.CX + contentW * 0.5f, DeckModeContentB);
            var content = Node(vp.transform, "Content", contentRect);
            // 横向滚动（原版 H1/V0 · Elastic · elasticity 0.1 · inertia 1 · deceleration 0.135 ·
            //   scrollSensitivity **1.0** · **无滚动条**，§4）。`notchPx` 就是 `m_ScrollSensitivity`。
            DeckModeScroll = new MenuScroll(DeckModeScrollRect, contentRect.x1, contentRect.x2, 1f);
            DeckModeScroll.Elastic = true;                 // `m_MovementType = 1`
            DeckModeScroll.Inertia = true;                 // `m_Inertia = 1`
            DeckModeScroll.ClipNode = vp;
            // 🔴 `Owner` = **模式页那一颗**（不是窗口根）：`PointerLayer` 跳过 `!Owner.activeInHierarchy`
            //   的滚动区 ⇒ 模式页藏起来时这个区自动失效，不会去抢卡组列表的滚轮。
            DeckModeScroll.Owner = root.gameObject;
            PointerLayer.RegisterScroll(DeckModeScroll);

            // ---- ③ 两张模式卡 ----
            float cy = (DeckModeContentT + DeckModeContentB) * 0.5f;    // 662.05
            for (int i = 0; i < DeckModeCards.Length; i++)
            {
                var spec = DeckModeCards[i];
                float x1 = contentRect.x1 + i * (DeckModeCardW + DeckModeCardGap);
                var cr = new PxRect(x1, cy - DeckModeCardH * 0.5f, x1 + DeckModeCardW, cy + DeckModeCardH * 0.5f);
                BuildDeckModeCard(content, spec, cr);
            }
        }

        /// <summary>一张模式卡（原版 prefab **`Game mode deck selector`**，§3·2 的完整节点树）。
        /// <para>四层逐层：`Background Image`（卡图 · 方图裁中竖条）· `Foreground Image`（原版 `m_Sprite = null`
        /// ⇒ **空节点**，落点 §五 末行）· `TextDarkening`（纯黑 a=0.6117647，▸ `Event Title`）· `Border`
        /// （`40k_square_border` Sliced · fillCenter 0 · ppuMul 5 · 灰 0.33962）。</para>
        /// <para>命中区 = **整张卡 505.93 × 802.67**（§3·5：卡内三颗 Image 的 `m_RaycastTarget` 全 1、
        /// padding 全 0、且都被根上的 `RectMask2D` 裁在卡内 ⇒ 并集 = 整卡）。**没有选中态**（`m_SpriteState` 四张全 null），
        /// 只有 ColorTint 0.9608/0.7843 + FadeDuration 0.1 ⇒ 走 `WindowButton` 的色偏（`TintOn` 指到卡图那颗）。</para></summary>
        void BuildDeckModeCard(Transform content, DeckModeCard spec, PxRect cr)
        {
            // ⚠️ 节点名**照原版**（两颗同名 —— 原版 `Instantiate` 出来的两个实例也叫这个名字）。
            //    自检按 `Content` 的孩子序取（`Transform.Find` 认不出重名）。
            var card = Node(content, "Game mode deck selector", cr);

            // ---- ① `Background Image` ----
            //   原版：`m_Sprite` = **null**（运行时由 liveop 数据灌，落点 §1·2）· `AspectRatioFitter(2, 1)` ·
            //   anchor (−0.294,0)-(1.294,1) ⇒ 解算 **803.08² 方图**，再被卡根上的 `RectMask2D` 裁回卡面。
            //   我们**没有 RectMask2D 的像素级掩码**（`ViewportClip` 是矩形裁切，裁不出「方图取中间竖条」）
            //   ⇒ 用 **UV 裁中间竖条**等价复现（同 `MainMenuRuntime.BuildModeCard` 的做法，落点 §六 指定）。
            var artTex = Art(spec.Art);
            ImageQuad artQ = null;
            if (artTex != null)
            {
                artQ = Rect(card, spec.Art, cr, "Background Image", QModeArt);
                if (artQ != null)
                {
                    float side = DeckModeCardH + DeckModeArtSidePad;        // 803.08
                    float u0 = (side - DeckModeCardW) * 0.5f / side;
                    float v0 = (side - DeckModeCardH) * 0.5f / side;
                    artQ.SetUvRect(new Rect(u0, v0, 1f - 2f * u0, 1f - 2f * v0));
                }
            }

            // ---- ② `Foreground Image`：原版 `m_Sprite = null` + `m_Material = null` ⇒ 参考图上是空的
            //   （落点 §五「可建空节点或直接不建」）⇒ **建节点、不画**（树形照原版，纯结构）。
            float sideF = DeckModeCardH + DeckModeArtSidePad;
            Node(card, "Foreground Image", new PxRect(cr.CX - sideF * 0.5f, cr.CY - sideF * 0.5f,
                                                      cr.CX + sideF * 0.5f, cr.CY + sideF * 0.5f));

            // ---- ③ `TextDarkening`：贴底的纯黑带（§3·2 / §3·3）----
            //   `Image(no sprite)` · 色 **(0,0,0, 0.6117647)** · `sizeDelta (−1.18030, 105.8657)` ·
            //   anchor **(0.0036542062,0)-(0.9963457584,0)** · `anchoredPosition (0.03729248, 55.462372)` ·
            //   pivot 居中 ⇒ 解算 **501.05 × 105.87**，带心距卡底 55.46（Unity y 向上）。
            float bandW = (0.9963457584f - 0.0036542062f) * DeckModeCardW - 1.18030f;
            float bandH = 105.8657f;
            float bandCx = cr.CX + 0.03729248f;
            float bandCy = cr.y2 - 55.462372f;                          // 屏幕 y 向下 ⇒ 从卡底往上减
            var bandRect = new PxRect(bandCx - bandW * 0.5f, bandCy - bandH * 0.5f,
                                      bandCx + bandW * 0.5f, bandCy + bandH * 0.5f);
            var band = Rect(card, null, bandRect, "TextDarkening", QModeBand, new Color(0f, 0f, 0f, 0.6117647f));

            // ---- ③′ `Event Title`（在 `TextDarkening` 之下，§3·3）----
            //   fs **58.8**（auto 18–72 · base 36）· Center/Geometry · sizeDelta 493.05 × 55.708 ·
            //   preset **`Pragati Thick Outline White`** ⇒ 白字 + 黑粗描边（`_OutlineWidth .097` /
            //   `_FaceDilate .105` / `_OutlineColor` 黑 a=.788，落点 §4·2）。
            //   ⚠️ 父级 `VerticalLayoutGroup`：padLeft **4** · MiddleLeft · spacing −4.2 · forceExpand 1/1 ·
            //   controlW/H 0（§3·3）⇒ `forceExpandWidth` 把它**撑到暗带整宽 − 4**（落点 §4·2 末段），
            //   高度仍是自己的 55.708、在带内**垂直居中**。
            float etW = bandW - 4f;
            var etRect = new PxRect(bandRect.x1 + 4f, bandCy - 55.708f * 0.5f,
                                    bandRect.x1 + 4f + etW, bandCy + 55.708f * 0.5f);
            var et = Text(band != null ? band.transform : card, Loc.T(spec.Term),
                          etRect.x1, etRect.x2, etRect.y1, etRect.y2, 5, Color.white, "Event Title", 58.8f,
                          // 四格 = 原版 `m_enableAutoSizing 1` / `min 18` / `max 72` / `base 36`
                          wrapPx: etW, autoMinPx: 18f, autoMaxPx: 72f, autoBasePx: 36f,
                          face: TmpFont.Face.LatinCardBody);
            if (et != null)
            {
                ApplyThickOutline(et, QModeTitle);                     // 白字 + 黑描边（材质四项照抄）
                // 原版 `m_VerticalAlignment = 4096`（TMP 的 `Geometry` = 本仓 `Label.VAlign.Midline`，同值 0x1000）
                et.SetVAlign(Label.VAlign.Midline, LayoutSpace.Px(55.708f));
            }

            // ---- ④ `Border`：`40k_square_border` · Sliced · **fillCenter 0** · **ppuMul 5** · 灰 0.33962262 · maskable 0（§3·3）----
            //   `m_Border = (13,13,13,13)`、图 64²、`m_PixelsToUnits 100`
            //   ⇒ 角块 = `13 ÷ ppuMul 5 = 2.6 px`（`m_PixelsPerUnitMultiplier` 是**除** —— 与主菜单那颗同一条口径，
            //     先例见 `MainMenuRuntime.BuildModeCard` ④ 那段订正）。
            Nine(card, "40k_square_border", cr, new Vector4(13f, 13f, 13f, 13f), QModeBorder,
                 new Color(0.33962262f, 0.33962262f, 0.33962262f, 1f), false, "Border",
                 new Vector4(13f / 5f, 13f / 5f, 13f / 5f, 13f / 5f), 64f, 64f);

            // ---- ⑤ 命中区 = 整张卡（§3·5）· 点 = 直接跳走（没有选中态）----
            var hit = AddHit(card, "Hit", cr, QModeHit, () => ChooseDeckMode(spec.Mode));
            var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
            // 原版 `m_TargetGraphic` = **`Background Image` 那颗** ⇒ 色偏打在卡图上（另建的透明命中区自己看不见）。
            // ⚠️ 用 `TintOn` 而**不是** `Bind(...)`：本卡**没有任何 hover 专用 sprite**
            //   （`m_SpriteState` 四张全 null，落点 §五）⇒ `Bind` 会把它按「换图那一档」处理并关掉色偏。
            if (wb != null && artQ != null) wb.TintOn(artQ);
        }

        /// <summary>白字 + 黑粗描边（原版材质 `Pragati-Regular Atlas Material Thick Outline`，
        /// 落点 §4·2 的四个字段原文：`_OutlineWidth .097` · `_FaceDilate .105` ·
        /// `_OutlineColor` = 黑 **a=.7882** · 无 underlay）。
        /// <para>⚠️ 走 **`fontMaterial`（实例材质）** —— 改共享材质会让全工程所有标签一起变
        /// （同 `CardView.ApplyBadgeCounterStyle` 那条口径）。</para>
        /// <para>⚠️ 次序：换/改材质之后**必须重放渲染队列** —— `Label` 的队列写在材质上，
        /// 材质一换就静默回 3000（`Label.ApplyQueue` 的注释记着这条坑）。</para></summary>
        static void ApplyThickOutline(Label lb, int q)
        {
            if (lb == null) return;
            var tmp = lb.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (tmp == null) { Debug.LogWarning("[Collection] 模式卡 `Event Title` 没有 TMP 后端 ⇒ 描边没上"); return; }
            var mat = tmp.fontMaterial;              // 实例材质（`ApplyQueue` 已经实例过一份，不会碰工程资产）
            if (mat == null) return;
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetColor(TMPro.ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.788f));
            mat.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, 0.097f);
            mat.SetFloat(TMPro.ShaderUtilities.ID_FaceDilate, 0.105f);
            tmp.UpdateMeshPadding();                 // 描边会画到原 quad 之外 ⇒ 重算留白（同 CardView 那一句）
            lb.SetRenderQueue(q);                    // 材质换过 ⇒ 队列重放
        }

        /// <summary>模式页 / 卡组列表的**显隐矩阵**（逐行照 §5·3 的 `ToggleSelect*` 反编译表）：
        /// <list type="bullet">
        /// <item>模式页：**关** `Decks Tab` · `Unlock` · `Create` · `Filter Toggle` · `Filters`（含 `Clear filters`）；
        /// **开** `Import`；`Header` / `Control Buttons` / `Separator Line` **不动**。</item>
        /// <item>列表页：逐颗反过来（`Unlock` 打开 —— 原版 `Setup()` 先无条件关它、`ToggleSelectDeck` 再打开）。</item>
        /// </list>
        /// ⚠️ `Import` 在**两态都显示**（§5·3；参考图右上角「导入卡组」就是它）—— 这是 §七 里被订正过的那一格。</summary>
        void SetDeckModeChrome(bool modePage)
        {
            if (_decksTabNode != null) _decksTabNode.gameObject.SetActive(!modePage);
            if (_deckModePage != null) _deckModePage.gameObject.SetActive(modePage);
            SetActiveIf(_deckCreateBtn, !modePage);
            SetActiveIf(_deckCreateHit, !modePage);
            SetActiveIf(_deckUnlock, !modePage);
            SetActiveIf(_deckFilterToggle, !modePage);
            SetActiveIf(_deckFiltersGrp, !modePage);
            SetActiveIf(_deckImportBtn, true);
            SetActiveIf(_deckImportHit, true);
        }
        static void SetActiveIf(Transform t, bool on)
        {
            if (t != null && t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
        }

        /// <summary>点一张模式卡（原版 `SelectGameModeDeckTab.DeckContainerClicked` → `ToggleSelectDeck`，§5·5）：
        /// 记下这一张的模式 → 关模式页 / 开列表 → 按模式重筛列表。
        /// <para>原版第 2 步写的是 `deckCollectionTab.currentEvent = 事件`，筛法是
        /// `deck.CustomGameModeEvent == currentEvent`，语义等价于 **`deck.GameMode == 事件的 EventPlayMode`**
        /// （§5·5·4 + §5·4 第 1 行）⇒ 我们直接存 `EventPlayMode`（= `RuleEngine.GameMode` 的数值）。</para>
        /// <para>⚠️ **次序**：先开列表（`SetDeckModeChrome(false)`）再 `ApplyDeckFilterChanged()` ——
        /// 重建卡组格要在**激活**的树上做（TMP 在未激活对象里量不出尺寸，见 CLAUDE.md §三「面板画出来了字不在」）。</para></summary>
        public void ChooseDeckMode(int mode)
        {
            _deckModeFilter = mode;
            SetDeckModeChrome(false);
            ApplyDeckFilterChanged();
            Debug.Log("[Collection] 选中模式 " + ModeName(mode) + "（`GameMode=" + mode + "`）"
                      + " ⇒ 卡组列表只列这一模式（原版 `DeckCollectionTab.GetCollection` 的谓词）");
        }

        /// <summary>回到「还没选模式」那一档（`_deckModeFilter = -1` ⇒ 列表不按模式筛）。
        /// <para>这一档在原版 = `DeckCollectionTab.currentEvent == null`（§5·5·4 的第一个分支）；
        /// 原版 UI **不清 `currentEvent`**（它一旦设上就留着 —— `BackButtonClicked` 清的是**页签** `+0x60`，
        /// 不是这个事件）⇒ **本口给自检用**，⛔ 别在正常流程里随手调。</para>
        /// <para>🆕 **2026-10-22（W64 返回链 · D2）**：生产上的「返回模式页」走的是
        /// <see cref="ShowDeckModePage"/>（= 清模式筛 **+ 把模式页显出来**），⛔ 不是本口
        /// （本口只清筛、显隐留给 `SetDeckModeChrome` 的调用者）。</para></summary>
        public void ClearDeckModeFilter()
        {
            _deckModeFilter = -1;
            ApplyDeckFilterChanged();
        }

        /// <summary>重新显示模式页（原版 `SelectDecksTab.BackButtonClicked` → 清 `currentTab` →
        /// `ChooseTabToOpen` → 事件 ≥2 ⇒ 又回模式页，§5·1 末段）。
        /// ⚠️ 只切显隐、**不重建列表**（列表这会儿是藏着的 —— 在未激活树上重建量不出 TMP 尺寸）。
        /// <para>🆕 **2026-10-22（W64 返回链 · D2）：生产调用点已接上** —— 收藏窗那颗 `Back` 钮在
        /// 「Deck 页签 + 已选模式」时走这里（判据 = <see cref="ShouldReturnToDeckModePage"/>）。
        /// 改前它是**零生产调用点**（只有自检调）⇒ 选过一次模式后该窗实例永远只筛那个模式、回不到模式页。</para></summary>
        public void ShowDeckModePage()
        {
            _deckModeFilter = -1;
            SetDeckModeChrome(true);
        }

        // ============================================================ 🆕 2026-10-22（W64 返回链 · D2）
        // 「收藏 → Deck 页 → 选择游戏模式页」的**返回**（原版两级分派，逐句读反编译定的）：
        //   ① `CollectionScreen__BackButtonClick.c`：`param_1[0x12]`（= **窗口级** `currentTab`）非空 ⇒ 取它的
        //      `GetType()`；类型 == `SelectDecksTab` ⇒ 交给 `SelectDecksTab.BackButtonClicked`；
        //      **否则**落 vtable `+0x1e8`（= 关窗/上一层）。
        //      ⇒ 我们这一级的等价物 = `CurrentTab == WindowTabType.CollectionDecks`（Deck 页签就是 `SelectDecksTab`）。
        //   ② `SelectDecksTab__BackButtonClicked.c`：
        //      `if (this[+0x60] != this[+0x58]) { this[+0x60] = null; ChooseTabToOpen(0); return; }`
        //      然后才落 vtable `+0x1e8`。
        //      · `+0x58` = **模式页那组件** —— `ToggleSelectGameModeDeck` 里
        //        `SelectGameModeDeckTab__Initialize(*(this + 0x58), …)` + `SetActive(*(this + 0x58))` 实读。
        //        ⇒ 我们的 `_deckModePage`。
        //      · `+0x60` = **`SelectDecksTab` 自己的 `currentTab`**：`ToggleSelectGameModeDeck` 写
        //        `this[+0x60] = this[+0x58]`（= 模式页），`ToggleSelectDeck` 写 `this[+0x60] = this[+0x50]`
        //        （`+0x50` = 列表那组件 `DeckCollectionTab`）⇒ ⚠️ **我们【没有】与它同名的单一字段**。
        //        可观测等价物 = 「当前开的是哪一半」：模式页开着 ⇔ `currentTab == 模式页`；
        //        列表开着 ⇔ `currentTab == 列表`，而列表态在本工程里**恒等于 `_deckModeFilter >= 0`**
        //        （`ChooseDeckMode` 是唯一进列表态的生产入口，`ShowDeckModePage`/`BuildDeckList` 收尾是唯一出口）。
        //      · 那句 `this[+0x60] = null`（「忘掉已选页签」）**没有独立字段可清** —— 它在**这一支**里的可观测
        //        效果 = 紧接着的 `ChooseTabToOpen(0)` 重新挑一次 ⇒ 又落回模式页；而模式页那一支不看
        //        `DeckCollectionTab.currentEvent`（`ChooseTabToOpen` 的分支只数 liveops 事件个数）⇒
        //        我们的等价组合 = **`_deckModeFilter = -1` + `SetDeckModeChrome(true)`** = `ShowDeckModePage()`。
        //   ③ ⚠️ **对不上的那一处（如实记，⛔ 不编一个状态出来）**：原版这两级之间还有「`currentTab` 为 **null**」
        //      这一档（`ChooseTabToOpen` 的 op_Implicit 分支会走「重新挑页」）。我们的 `_deckModeFilter`
        //      只有 `-1`/`>=0` 两档，**表达不了「null 且模式页关着」** —— 该档在原版 UI 上要靠
        //      `currentTab` 被清而窗口仍开着才会出现，本地没有可复现的入口（`ToggleSelect*` 两个写点都会立刻
        //      设成非 null）⇒ 我们按「`_deckModeFilter < 0` 等同于模式页那一档」处理。未查清部分：该 null 档
        //      在联网 liveops 场景下是否可达（本地零事件实例，§5·4）。

        /// <summary>Back 钮**现在该回模式页，还是照旧关窗** —— **唯一判据**（Back 钮与自检都调它）。
        /// <para>判据 = 原版两级分派（全文见上面那段注释）：① 窗口页签是 Deck（`SelectDecksTab`）；
        /// ② 该页签的子页签**不是**模式页（⇒ 我们这边 = 已选模式、列表开着）。</para>
        /// <para>⚠️ ① 的等价性：原版比的是**当前页签对象的类型**；我们只有 `CurrentTab` 这个枚举，
        /// Deck 页签 ⇔ `CollectionDecks`。其余三页今天没有子页 ⇒ 谓词为假 ⇒ 关窗（与原版一致）。</para></summary>
        public bool ShouldReturnToDeckModePage()
        {
            if (CurrentTab != WindowTabType.CollectionDecks) return false;   // 原版 ①：页签不是 `SelectDecksTab` ⇒ 走通用返回
            // 原版 ②：`this[+0x60] != this[+0x58]` —— 等价于「**当前开着的是列表那一半**」。
            // `_deckModeFilter >= 0` 是列表态的定义；`!DeckModePageShown` 再钉一次两半互斥
            // （两者本该互补，这里要求**两条同时成立**才回模式页 ⇒ 万一将来哪个入口把状态写岔，
            //  Back 退回落「关窗」那一支，⛔ 不会在模式页上再「回」一次模式页）。
            return _deckModeFilter >= 0 && !DeckModePageShown;
        }

        /// <summary>收藏窗那颗 `Shared/Close Button`（文字是 **"Back"**）被点 —— 原版
        /// `CollectionScreen.BackButtonClick` 的落点，本窗**唯一**的返回钮。
        /// <para>🆕 **2026-10-22（W64 返回链 · D2）**：改前它写死 `Close()` ⇒ 选过一次模式后
        /// **该窗实例永远只筛那个模式**、回不到模式页（窗被 `Hide` 后复用同一实例，`_deckModeFilter` 留着）。
        /// 现在按 <see cref="ShouldReturnToDeckModePage"/> 分派；⛔ 别把判据抄成第二份。</para></summary>
        void OnBackClicked()
        {
            if (ShouldReturnToDeckModePage())
            {
                ShowDeckModePage();
                Debug.Log("[Collection] Back（Deck 页签 + 已选模式）⇒ **回「选择游戏模式」页**、模式筛清空"
                          + "（原版 `SelectDecksTab.BackButtonClicked` 第一支：`currentTab` ≠ 模式页 ⇒ "
                          + "`currentTab = null` + `ChooseTabToOpen(0)`）");
                return;
            }
            Close();
        }

        /// <summary>模式号 → 屏上那个词（自检/日志用；走官方词条，随语档变）。</summary>
        public static string ModeName(int mode)
        {
            for (int i = 0; i < DeckModeCards.Length; i++)
                if (DeckModeCards[i].Mode == mode) return Loc.T(DeckModeCards[i].Term);
            return mode.ToString();
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
            GameObject inputBg = null;      // 🆕 2026-10-23（`C3`）：ColorTint 的目标图形（原版 `m_TargetGraphic`）
            if (itex != null)
            {
                // 🔴 **2026-10-06（A50③）：收口到公共件 `MenuDraw.Nine`**（等价性论证与 `BuildNameRow`
                //   那一处**同一套**，逐项同值）：矩形 `ir` / 落位 `Local(nameRow, …)`（同名同源的 `Local`）/
                //   尺寸 `LayoutSpace.Px(ir.W/H)` / 队列 `QFltRow` / tint `(0.0627, 0, 0, 1)`
                //   （与 `FilterPanelModel.InputTint` 同一个值，这里照旧写就地量）。切边 `(10,10,10,10)` · `32f,32f`
                //   原样传；公共件内部直接转调 `ImageQuad.CreateNineSlice` ⇒ 块数 / uv 切分 / 每块 `SetAspect` /
                //   `borderOutPx ?? border` 的退化**全同一条**。`clip` 一律 `null`（⛔ 不顺手改观感）。
                //   ⚠️ `ir` **别动** —— 下面 `AddHit` 还要用它；公共件只按值读 `PxRect`，不改调用方那个变量。
                inputBg = MenuDraw.Nine(nameRow, itex, ir, new Vector4(10f, 10f, 10f, 10f), 32f, 32f,
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
            var deckNameHit = AddHit(nameRow, "Hit", ir, QFltHit, BeginDeckNameFilter); DisableClickSound(deckNameHit);   // 判据：原版这颗是 `EverguildInputField`（`TMP_InputField` 子类；`Deck Name Filter ▸ Input Field` 的 MB `3479015056449266389` **没有** `m_onClickSound`/`useDefaultAudioClipIfNull` 字段，UnityEvent 全 0，`EverguildInputField.cs` 也无音频引用）⇒ 点击**无声**，而我们缺省会播 `ButtonClick`。
            // 🆕 **2026-10-23（`C3`）ColorTint 悬停/按下** —— 原版 `EverguildInputField.m_Transition = 1`、
            //   `HL = 0.9607843` · `PR = 0.7843137` · `fade = 0.1`、`m_TargetGraphic` = 输入框底图
            //   （`InputFieldBackground` 那一块）。缺省 key 就是这三个值 ⇒ 只补 `target`。
            //   ⚠️ `TintOn` 而不是 `Bind`：原版这颗的 `m_SpriteState` 四格全空（换了图就没图可换）。
            var deckNameWb = deckNameHit != null ? deckNameHit.GetComponent<WindowButton>() : null;
            if (deckNameWb != null && inputBg != null)
                deckNameWb.TintOn(inputBg.GetComponentsInChildren<ImageQuad>(true));

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

        /// <summary>Deck 页的卡组在当前筛选下要显示哪些（**原始下标**）。
        /// <para>🆕 **2026-10-22（W64）**：多一道**模式**筛 —— 原版 `DeckCollectionTab.GetCollection`
        /// （§5·5·4）在 `currentEvent != null` 时只留 `deck.CustomGameModeEvent == currentEvent` 的卡组，
        /// 语义等价于 **`deck.GameMode == 当前事件的 EventPlayMode`**（§5·5·4 + §5·4 第 1 行）。
        /// `_deckModeFilter < 0` = `currentEvent == null` 那一支（不筛）。</para></summary>
        List<int> FilteredDeckIndices()
        {
            var list = new List<int>();
            int n = CollectionData.DeckCount();
            for (int i = 0; i < n; i++)
            {
                var d = CollectionData.DeckAt(i);
                if (_deckModeFilter >= 0 && d.GameMode != _deckModeFilter) continue;   // 🆕 W64
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
            // ============================================================ 🆕 2026-10-25（R13 · 卡组拖拽重排）
            // 重建 = 把旧格（连命中区）整个销毁 ⇒ **它们的拖拽登记必须先摘掉**。
            // 原版对位：`CollectionItem<T>.OnDisable()` 把两个回调从**静态** `DragAndDropItem.OnStartDragEvent /
            //   OnEndDragEvent` 上摘下来（`CollectionItem.cs:193`），`CollectionDisplay<T>.OnDisable()` 同理
            //   （`CollectionDisplay.cs:582`）—— 原版是「关页才摘」，我们是「重建就摘」，因为**我们这一层
            //   没有 cell 复用**（每趟重建都换一批 GameObject）。
            // ⚠️ `PointerLayer.PruneDrags` 也会清「按钮已销毁」的死条目，但那是**惰性**的（下一次 `RegisterDrag`
            //   才跑）⇒ 拖到一半被重建（滚动 / 筛选）时会留一个半死的 `DragSource`。这里显式摘一次。
            PointerLayer.UnregisterDragsOwnedBy(holder != null ? holder.gameObject : null);
            _deckDragLive = false; _deckDragCell = null; _deckDragFromPos = -1;

            // 原版 `CollectionDisplay<T>.SetCell` 的闸（RVA 0x1815EEB64 实读，见 `DeckDragAllowed`）：
            // `allowItemDrag && collection.Count == filteredCollection.Count` ⇒ 每格给不给拖。
            bool dragOk = DeckDragAllowed();

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
            _deckCellPos.Clear();      // 🆕 R13：与 `DeckCells` 同一趟清（见那个字段的 doc）

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
                DeckCells.Add(BuildDeckCell(parent, idxs[p], r, p, dragOk));
            }
        }

        // ============================================================ 🆕 **2026-10-19（`N70` P0-5）**
        // 收藏窗三类格子的**悬停反馈**（判据全部来自反编译，⛔ 不按截图猜）：
        //   · 卡牌格 / 异画格 = `Card2DController.ToggleHighlight(bool)` —— 那颗图形材质某个颜色的
        //     **alpha** 补到 1/0，时长 **0.2 s**（`DAT_1834b2bb0`）。我们的等价物 = `CardView.SetHoverHighlight`
        //     （状态环那一层写的就是 `_Outline` 的 alpha，时长同 0.2）。
        //   · 卡组格 = `CollectionDeck.HoverHighlight` —— **整格 `localScale` 1.0 ↔ 1.05**（瞬切、无补间），
        //     落在 `MenuDraw.DeckCell`（一族共用一份，⛔ 别在这里再写一遍）。
        //   · 卡背 / 装饰格 = `CollectionCosmetic.HoverHighlight` —— 同一个 `UIImageMaterialColorChanger`
        //     把某个材质属性的 **alpha** 补到 1/0、0.2 s。那颗 SDF 材质上**只有一个颜色属性** `_Outline`
        //     （`Core/CardView.CardbackSdfMaterialBase` 逐项实读）⇒ 用它的 alpha。
        //     ⚠️ **判据半条（如实标）**：原版那个属性 id 是 `CollectionCosmetic` ctor 里
        //     `Shader.PropertyToID(<字面量>)` 算出来的（偏移 `+0x70`），字面量在 metadata 里、静态读不到
        //     ⇒ 「就是 `_Outline`」是**按材质属性集合推的**，不是读出来的字段（与 `Card2DController` 那颗同族）。
        /// <summary>把卡格的悬停接到 `CardView` 的描边层上（判据 → 本段注释）。
        /// 🔴 **2026-10-25（`C` 收藏域 · 铁律 5 就地订正）**：`wildcardArmy` 那个形参**已删** ——
        /// 原来传 `true` 时把「指针压在这张卡上」当徽记输入，**那不是原版的输入**（原版 = 列表滚动，
        /// 见 `CheckFocusedArmy`）。今天徽记**只**由 `CheckFocusedArmy`（滚动/刷新）与
        /// `OpenCardDetail`（开卡片详情）两处驱动，与 `CardCollectionDisplay` / `CardCollectionTab` 逐条对齐。
        /// ⚠️ 悬停描边本身**保留** —— 它是原版 `CollectionCard.HoverHighlight`（`ToggleHighlight`，0.2s）。</summary>
        /// <summary>🆕 **2026-10-25（代理 G · 补 `CollectionItem.OnPointerEnter` 那道闸）**：
        /// 原版**拖拽中不悬停** —— `CollectionItem.OnPointerEnter` 的第一句是
        /// `if (eventData.pointerDrag != null) return;`（有拖拽正在发生就不亮悬停）。
        /// <para>我们的等价物：**鼠标主键按住 = 可能正在拖**（任何拖拽期间必然按住；点击当帧也会命中，
        /// 代价 = 按下瞬间移入卡格不亮描边，松手后下一次 enter 恢复）。</para>
        /// <para>⚠️ 为什么不用「真拖拽标志」：本工程拖拽状态在 `Core/DraggableController` 的**实例**上
        /// （`Dragging`），没有全局口；而收藏卡格本身不参与拖拽 ⇒ 用按键按下做等价闸，并在自检里可强开。</para></summary>
        static bool _hoverSuppressedForTest;
        public static bool HoverSuppressed
        {
            get { return _hoverSuppressedForTest || Input.GetMouseButton(0); }
        }
        public static void SetHoverSuppressedForTest(bool on) { _hoverSuppressedForTest = on; }

        void AttachCardHover(Transform hit, CardView v)
        {
            if (hit == null || v == null) return;
            var wb = hit.GetComponent<WindowButton>();
            if (wb == null) return;
            wb.onEnter = () => { if (v != null && !HoverSuppressed) v.SetHoverHighlight(true); };
            wb.onExit = () => { if (v != null) v.SetHoverHighlight(false); };
        }

        /// <summary>🆕 **2026-10-19（界面线尾巴 · `WildcardDisplay` 徽记）**：把 `WIldcard Display ▸ Army Icon`
        /// 换成 <paramref name="army"/> 那一面旗。**两口共用**（原版同一句 `WildcardDisplay.Initialize(army)`）：
        /// ① `CheckFocusedArmy` = **卡池列表滚动 / 刷新之后**（固定参考矩形命中哪一格 ⇒ 那一格的阵营）；
        /// ② `OpenCardInformation` = 开卡片详情（本文件 `OpenCardDetail`，原版 `CardCollectionTab.OnItemSelected`）。
        /// <para>认不出（`ArmyIcons.Texture` 返回 null —— `0`/`-1`/表外值）⇒ **保留当前图 + 出声一次**，
        /// ⛔ 不拿别的阵营顶（那会把「不知道」画成一张具体的旗）。</para>
        /// <para>⚠️ 原版 `WildcardDisplay.Initialize` 还会刷那 4 个库存数字；用户 2026-09-28 已拍板
        /// **数字恒定 `99`**（缺发放源），⇒ 本函数**只换旗、不碰数字**。</para></summary>
        void SetWildcardArmy(int army, string why)
        {
            string art = ArmyIcons.IconName(army);
            if (string.IsNullOrEmpty(art))        // `0` / -1 / 表外 ⇒ 认不出
            {
                if (_wcArmyWarned.Add(army + "|" + why))
                    Debug.LogWarning("[Collection] `WIldcard Display ▸ Army Icon` 换旗失败：阵营认不出（army="
                                   + army + " · " + why + "）⇒ **保留当前那张**（⛔ 不猜、不拿别的阵营顶）。"
                                   + "映射表 = `Core/ArmyIcons.cs`（13 条，原版 `IconsByArmySO` 逐字节解出）。");
                return;
            }
            _wcArmy = army;
            var tex = ArmyIcons.Texture(army);
            if (tex == null)
            {
                if (_wcArmyWarned.Add(art + "|tex"))
                    Debug.LogWarning("[Collection] `WIldcard Display ▸ Army Icon` 的图 `" + art + "` **取不到**"
                                   + "（`Resources/Art/ui_deck/` 下缺这张）⇒ **保留当前那张**（⛔ 不拿别的图顶）。");
                return;
            }
            bool changed = _wcArmyArt != art;
            _wcArmyArt = art;
            // 还没建那棵树（或页已重建）⇒ 只记状态，建页时 `BuildCardsPage` 会按 `_wcArmy` 取图。
            // 同名图**不再重设**（悬停每帧级触发；`SetTexture` 会重建网格）。
            if (_wcArmyQuad != null && changed) _wcArmyQuad.SetTexture(tex, true);   // PA=1（原版 `m_PreserveAspect = 1`）
        }

        // ============================================================ 🆕 **2026-10-25（`C` 收藏域 · K3 P2-⑦ 落地）**
        /// <summary>原版 `CardCollectionDisplay.CheckFocusedArmy`（`CardCollectionDisplay.cs:385-461`）的等价物：
        /// 拿**一个固定矩形** `Reference Card Pointer` 去和**当前在屏上的卡格**求交，**第一个**相交的那一格的
        /// 阵营交给 `WildcardDisplay.Initialize`。
        ///
        /// <para>判据（三条，全部一手）：
        /// <list type="number">
        /// <item>**矩形**：`工具/menu_dump.py bundle_menus_assets_all "CardsTab" --depth 8` 现读（2026-10-25）——
        ///   节点 `Reference Card Pointer` = **402.3,236.4 → 596.1,281.4**（193.84×45.09），父件 = `Collection Display`
        ///   的直接孩子、与 `Scroll View` **同级**；`m_IsActive = 0` ⇒ **不画、但矩形照用**。
        ///   本组数与 `CardsViewport`（330.2,155.9→1919.9,1079.9）**同一帧**（同一份 dump 里 `Scroll View`
        ///   就是 330.2,155.9）⇒ 与 `CardsCellRect` 可直接比，⛔ 不要再加父链偏移。</item>
        /// <item>**命中序**：`RectExtensions.OverlapsAny`（🥇 `out_cpp2il_dev_ilspy/RectExtensions.cs:69-244`
        ///   ＋ `decomp_full/RectExtensions__OverlapsAny.c`，两份一致）= **按传入序返回第一个相交的**
        ///   （`if (bVar1) { *param_3 = uVar7; return 1; }`）—— 不相交就继续，**没有排序 / 没有取最近**。
        ///   原版传入序 = `Enumerable.Select(Cells, c => c.transform as RectTransform)` 的
        ///   `RecyclableScrollRect.Cells` **池序**；我们按**可见卡序**（= 屏上从上到下、从左到右）枚举，
        ///   理由：参考矩形 193.84×45.09 **比一格（262.5×384）小**、横向只压第 0 列
        ///   （`402.3…596.1` vs 第 1 列起点 `600.05`）⇒ **最多命中一格**，两种枚举序的**结果恒相同**
        ///   （只有正好压在行边界时才是 2 格，那时两种序都取**上面那格**）。</item>
        /// <item>**不相交时什么都不做**（原版 `if (!referenceCardPoint) return;` ＋ 循环走完直接返回）
        ///   ⇒ **没有「移开就还原」**，保留最后那张。我们照此。</item>
        /// </list></para>
        ///
        /// <para>⚠️ 与 `OverlapsMany` 的取舍：原版 `OverlapsAny` 的相交判据是**严格不等**的 AABB
        ///   （`a.xMin < b.xMax && …`，四条全真；贴边不算相交）—— 本方法同式。</para>
        public const float WcRefL = 402.3f, WcRefT = 236.4f, WcRefR = 596.1f, WcRefB = 281.4f;

        /// <summary>卡池当前**屏上**的格序 → 第一个与参考矩形相交的那格的阵营。**没有命中就不动**。</summary>
        void CheckFocusedArmy()
        {
            if (CardsScroll == null) return;            // 页还没建（或已销毁）⇒ 原版那颗 `referenceCardPoint` 也为空
            var list = CardsState.VisibleCards();
            for (int i = 0; i < list.Count; i++)
            {
                var r = CardsScroll.Shift(CardsCellRect(i));
                if (!(r.x1 < WcRefR) || !(WcRefL < r.x2)) continue;     // 严格不等（同原版 AABB）
                if (!(r.y1 < WcRefB) || !(WcRefT < r.y2)) continue;
                SetWildcardArmy(ArmyIcons.Id(list[i].Faction), "列表滚动后参考矩形命中第 " + i + " 格");
                return;                                                  // ★ 第一个命中的
            }
        }

        // ============================================================ 🆕 **2026-10-23（`C3`）**
        // 卡格**选中 punch**（原版 `CollectionCard.SelectedHighlight(true)`）。
        //
        // 判据（`d:/2/tools/decomp_full/CollectionCard__SelectedHighlight.c` 逐句 + `.rdata` 实读）：
        //   `DG_Tweening_ShortcutExtensions__DOPunchScale(<content>.transform, punch, **0.1**, **10**, elasticity, 0)`
        //   · **punch 向量** = 静态表里某个 `Vector3` 的**三个分量各翻符号位**（`^ 0x80000000`，`DAT_1834b2c80`
        //     实读 = −0.0）再 × **0.05**（`0x1834b2f94` 实读 = 0.05）。
        //     那个向量在 `*(longlong*)(DAT_1842da2a8 + 0xb8) + 0xc`：`DAT_1842da2a8` = **`UnityEngine.Vector3`
        //     的类**、`+0xb8` = 它的**静态字段块**，块内 `+0x00 / +0x0c / +0x18 / +0x3c / +0x48` 依次是
        //     `zero / one / up / right / forward` —— 旁证三条：`PhotonTransformViewPositionControl..ctor` 拿 `+0x00`
        //     当零向量初始化字段；`LookAtConstrainWIP__Update` 把 `+0x18` 当 `AngleAxis` 的**轴**（= `up`）；
        //     `MoveByKeys__FixedUpdate` 在 `+0x3c` / `+0x48` 上做水平 / 前向位移（= `right` / `forward`）。
        //     ⇒ **`+0xc` = `Vector3.one`** ⇒ punch = **(−0.05, −0.05, −0.05)**（每轴 ±0.05）。
        //   · 时长 **0.1**（`0x1834b2dc4`）· **vibrato 10**（调用里的立即数）。
        //   · elasticity = **`0x1834b2bb8` = 1.0**（`工具/read_literal.py` 实读）。
        //     🔴 **核查报告 §四 #3 写的「elasticity 0.3」是错的** —— 0.3 是 `0x1834b2dc8`（`CardScript.DoPushBack`
        //     那一处），本条调用传的是 `0x1834b2bb8`。本行按实读的 **1.0**（判据 > 转述）。
        //   · 作用对象 = `param_1 + 0x60` 的 transform —— 🔴 **2026-10-24（A14 · 界面线第五批）现核：
        //     那一格是 `CollectionCard.canvas`（`CanvasGroup`），它的 GameObject 名字就叫 **`Content`**，
        //     ⛔ **不是**「卡体那一层」**（旧说法把 `Content` 当成卡体了）：
        //       · 字段序（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CollectionCard.cs` 声明序）：
        //         `cardUI(0x48)` → `highlightColor(0x50，Color 16B)` → **`canvas(0x60)`** → `counter(0x68)`。
        //         旁证 = `CollectionCard__Start.c` 拿 `+0x48` 调 `BasicCardUI` / 从 `+0x50` 读 Color；
        //         `CollectionCard__AnimateIn.c` 对 `+0x60` 调 **`CanvasGroup.set_alpha` / `DOFade`** ⇒ 就是 CanvasGroup。
        //       · 该 PPtr 在 prefab 里的落点（`bundle_menus_assets_all`）：
        //         `MonoBehaviour_40971030049256792.json` 的 `canvas` = `CanvasGroup 533470822043284824`
        //         → GO `Content`（`GameObject/Content_7243044493727364440.json`；RT `-6374656361261697704`：
        //         `m_AnchorMin (0,0)` · `m_AnchorMax (1,1)` · `m_SizeDelta (0,0)` · `m_Pivot (.5,.5)` ·
        //         `m_LocalScale (1,1,1)` ⇒ 与 cell 根同矩形、恒等缩放）。
        //       · 它的父 = cell 根 `Collection Card`（350×512，`m_Children` **只有 `Content` 一个**），
        //         孩子 = **`Counter`（张数条）+ `CardUI`（卡体）** ⇒ **punch = 整格一起弹**（卡 + 张数条）。
        //   ⇒ 我们这棵树里对应的一级 = **每格的 `Content <i>` 容器**（`RebuildCardsCells` / `RebuildStyleCells`
        //     每趟建一个）：卡体 `CollectionCard_*` 与张数条 `Counter *` 都挂在它下面；命中区 `CardHit_*`
        //     与它**同级**（原版射线件在 `Content` 的上一层 `Collection Card` 上 ⇒ punch 时命中区不跟着动）。
        //     ⛔ **别再打 `CardView` 自己的根**（A14 之前的写法）：那样张数条不弹、缩放中心也偏到卡心上，与原版差一格。
        /// <summary>选中 punch 的三个参数（原版 `.rdata` 直读；见上面那段判据）。</summary>
        public const float CardPunchSize = 0.05f, CardPunchTime = 0.1f, CardPunchElasticity = 1f;
        /// <summary>`DOPunchScale` 的 vibrato —— 原版调用里的立即数 **10**。</summary>
        public const int CardPunchVibrato = 10;

        // ⚠️ 这几份是 **static**：punch 是「同一时刻只有被点的那一格在弹」的瞬时反馈，
        //   而本窗全局只有一扇（同族先例：`LastCardDetail` 也是静态）⇒ 不需要按实例分家。
        //   （`_cardPunchTarget` 只是给自检读的「打在哪一级」，不参与补间本身。）
        static DG.Tweening.Tween _cardPunch;
        static Vector3 _cardPunchBase;
        static int _cardPunchCount;
        static Transform _cardPunchTarget;
        /// <summary>自检用：本进程一共发起过几次卡格 punch（批处理里补间不自己跑 ⇒ 连同「收尾复位」一起验）。</summary>
        public static int CardPunchCountForTest { get { return _cardPunchCount; } }
        /// <summary>自检用：**上一次 punch 打在哪一级** —— 生产路径传的是每格的 `Content &lt;i&gt;` 容器
        /// （原版 `CollectionCard.SelectedHighlight` 打 `<c>content</c>` 那一级，判据见上面那段）。
        /// ⛔ 别让它返回 `CardView` 的根 —— 那是 A14 之前打岔的那一级（张数条不跟着弹）。
        /// 判据 / 用法 → `Editor/CollectionScene.cs` 的「punch 作用节点」那三条断言。</summary>
        public static Transform LastPunchTargetForTest { get { return _cardPunchTarget; } }
        /// <summary>自检用入口：对任意 transform 起一次 punch（与真实点击**同一个方法** —— ⛔ 别另写一份）。
        /// 用法见 `Editor/CollectionScene.cs` 的「选中 punch」那一段（自带 `CardTween.Mode = Manual` + `Advance`）。</summary>
        public static void PunchCardForTest(Transform t) { PunchCardCell(t); }

        /// <summary>卡格被点时的选中反馈（原版 `CollectionCard.SelectedHighlight(true)`，见上面那段判据）。</summary>
        static void PunchCardCell(Transform t)
        {
            if (t == null) return;
            _cardPunchCount++;
            _cardPunchTarget = t;          // 自检读数：打的是哪一级（应为每格 `Content <i>`，⛔ 不是 `CardView` 根）
            // 连点同一格：先把上一条杀掉 —— `OnKill` 会**先把缩放还原**，所以下面取到的 `base` 已经是干净的。
            if (_cardPunch != null && _cardPunch.IsActive()) _cardPunch.Kill();
            _cardPunchBase = t.localScale;
            var tw = DG.Tweening.ShortcutExtensions.DOPunchScale(
                         t, Vector3.one * -CardPunchSize, CardPunchTime, CardPunchVibrato, CardPunchElasticity);
            // ⚠️ punch 走完 / 被打断**都必须显式复位**：目标现在是每格的 `Content <i>` 容器（恒等缩放 1，
            //   没有任何别的代码会去写它）⇒ 不复位就会**永久**留一个非 1 的缩放；而批处理下没有帧循环，
            //   `OnComplete` 不一定跑得到 ⇒ 两处都挂（`OnKill` 那一条正是「连点同一格」时的收尾）。
            tw.OnComplete(() => { if (t != null) t.localScale = _cardPunchBase; });
            tw.OnKill(() => { if (t != null) t.localScale = _cardPunchBase; });
            _cardPunch = CardTween.Use(tw, DG.Tweening.Ease.Linear, t);
        }

        /// <summary>把装饰格（卡背）的悬停接到它那颗 SDF 材质的 `_Outline` alpha 上（0.2 s 补间）。</summary>
        void AttachOutlineHover(Transform hit, Material sdf)
        {
            if (hit == null || sdf == null || !sdf.HasProperty("_Outline")) return;
            var wb = hit.GetComponent<WindowButton>();
            if (wb == null) return;
            var baseC = sdf.GetColor("_Outline");
            wb.onEnter = () => TweenOutline(sdf, baseC, 1f);
            wb.onExit  = () => TweenOutline(sdf, baseC, 0f);
        }

        /// <summary>`_Outline` 的 alpha 补到 <paramref name="a"/>（原版 `DoColorChange(..., 0.2f)`）。
        /// ⚠️ 用 `SetTarget(sdf)` + `Kill(sdf)` 保证**同一颗材质只有一条补间**在跑（悬停/离开快速交替时不会打架）。</summary>
        static void TweenOutline(Material sdf, Color baseC, float a)
        {
            if (sdf == null) return;
            DG.Tweening.DOTween.Kill(sdf);
            float from = sdf.GetColor("_Outline").a;
            var tw = DG.Tweening.DOTween.To(() => from, v =>
            {
                if (sdf == null) return;
                var c = baseC; c.a = v;
                sdf.SetColor("_Outline", c);
            }, a, CardHighlight.OutlineAnimTime);
            // ⚠️ 扩展方法在本文件没 `using DG.Tweening;` ⇒ 走**静态调用**（`SetTarget` 是泛型扩展）。
            DG.Tweening.TweenSettingsExtensions.SetTarget(tw, sdf);
            DG.Tweening.TweenSettingsExtensions.SetEase(tw, DG.Tweening.Ease.Linear);
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
        /// <param name="displayPos">这一格在**本次显示序列**（= `FilteredDeckIndices()` 的下标）里的序号；
        /// `-1` = 没有序号（不该发生，防御用）。它**总是**被记进 <see cref="_deckCellPos"/>，
        /// 因为「拖到哪一格上」要按它算。</param>
        /// <param name="dragOk">这一格**给不给拖**（原版 `SetCell` 那道闸，见 <see cref="DeckDragAllowed"/>）。</param>
        Transform BuildDeckCell(Transform parent, int i, PxRect r, int displayPos = -1, bool dragOk = false)
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
            //     （`Shell/CollectionData.cs:53` 的 `DeckInfo.GameMode` ← `CardPresentation/Shell/CollectionData.cs` 抄自 `PlayerDeck.GameMode`）。
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
            var built = MenuDraw.DeckCell(this, parent, "CollectionDeck_" + i, r, info,
                                     false,
                                     QPageRow,
                                     // 🆕 **2026-10-17（A1529①）**：`qBelow` = 选中金环那一档（原版 `Highlight`
                                     //   是 `Deck` 的 child[0] ⇒ 必须在 `q` **之下**）。本页 `selected` 恒 `false`
                                     //   （`useSelectedHighlight = 0`，见上）⇒ 这一档今天**不画**；取 `QPageSdf`
                                     //   （= 3031，梯子上 `QPageRow` 的下一格）只为**把形参显式表态**（⛔ 不留缺省值：
                                     //   缺省 = 下一个调用点静默把金环画到最顶，A1529 就是这么来的）。
                                     QPageSdf,
                                     QPageText, QPageOverlay, QPageRow,
                                     // 🆕 2026-10-25（R13）：同一条「列表忙就不选」（判据 → `ListBusy`）——
                                     // 原版这一句在**基类** `CollectionDisplay<T>.Select` 里，四个页签共用。
                                     () => { if (ListBusy(DeckScroll)) return; SelectDeck(idx); },
                                     gameMode: info.GameMode);   // `DeckInfo` 是 **struct** ⇒ 没有 null 那一档

            // ============================================================ 🆕 2026-10-25（R13 · 拖拽重排）
            // 🔴 **只有 Deck 页能拖** —— 原版 `Collection Display` 三份 prefab 里**只有这一份**
            // `allowItemDrag = 1`（`bundle_menus_assets_all` 的 8 份 MB 实读：`MonoBehaviour_3428528622247862997`
            // = 1，其余 7 份 = 0；本页那一份的 `m_GameObject` = 4855765916858770133）。
            // 闸在**调用点**（`dragOk` 由 `RebuildDeckCells` 算好传进来），不在本函数里再判一次。
            if (displayPos >= 0)
            {
                _deckCellPos[built] = displayPos;      // 「拖到哪一格上」按它算（⛔ 不是 `DeckCells` 下标）
                if (dragOk) AttachDeckDrag(built, displayPos);
            }
            return built;
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
            // 🆕 **2026-10-25（接线）**：原版 `CardDisplayOpened` —— 只**收指针**、不改状态机
            //   （判据 `WildcardTutorialManager` 那一支；我们的等价口在 `WildcardTutorial.NotifyCardDisplayOpened`）。
            WildcardTutorial.NotifyCardDisplayOpened();
            if (card == null) { Debug.LogWarning("[Collection] 没有卡可展示"); return null; }
            // 🆕 **2026-10-19（界面线尾巴 · 铁律 11）**：原版「开卡片详情 ⇒ 徽记换成**那张卡**的阵营」
            //   （`DeckEditingWindow__OpenCardInformation.c:35`：`WildcardDisplay.Initialize(window.wildcardDisplay,
            //   card.army, 0)` 就在 `CardDisplayWindow.ShowCard` 之前一句）⇒ 本窗同一条语义接在**同一个口**
            //   （`SetWildcardArmy`；判据与 `CheckFocusedArmy` 同源，见 `BuildCardsPage` 那一段）。
            //   ⚠️ 卡池页与异画页共用本函数 —— 两页的 `WIldcard Display` 是**同一颗**，行为一致；
            //   认不出阵营（如异画那张的 `faction` 为空）⇒ `SetWildcardArmy` 自己出声并保留当前图。
            SetWildcardArmy(ArmyIcons.Id(card.Faction), "开卡片详情：" + card.Name);
            if (Manager == null)
            {
                Debug.LogWarning("[Collection] 没有 `WindowsManager`，开不了卡片详情窗");
                return null;
            }
            // 🔴 **2026-10-18（`N116`）：这里每开一次都新建一扇，而 `Close()` 只做「置灰 + 摘表」**
            //   —— 旧的那扇**从不销毁** ⇒ 连续开关 N 次 = 场上 N 具残骸（原版 `CloseWindowCO` 的收尾是
            //   **Hide → `List.Remove` → `Destroy` + 释放**，判据 → `d:/2/tools/decomp_full/WindowsManager__CloseWindowCO.c`，
            //   另见 `Shell/GameWindow.cs` 的 `Close()` 里 A1709 那段逐句注释；我们这条链的「Destroy」没人做）。
            //   ⇒ 在**新建之前**把上一扇收掉：先 `Close()`（走记账：摘表 + `ShowPreviousWindow`；
            //   已经 `Closed` 的会按原版 `GameWindow.Close` 首句早退、**不再重复记账**），再 `DestroySafe`
            //   （批处理下没有帧循环 ⇒ 必须 `DestroyImmediate`，`MainMenuSubmenuWindow.DestroySafe` 就是那口）。
            //   ⇒ 任意次开关之后，场上最多剩 **1 扇**（最后那扇、按原版保留「关掉后只是不显示」那一态）。
            //   ⛔ 别改成「复用单实例」：`CardDetailPopup` 还有别的宿主（`DeckInfoPopup.OpenWarlordDetail` /
            //   `BoosterPackOpenWindow` / 战斗侧），它们的生命周期不归本文件管 —— 这里只收拾**本文件开的**那扇。
            if (LastCardDetail != null)
            {
                if (LastCardDetail.CurrentState != WindowState.Closed) LastCardDetail.Close();
                DestroySafe(LastCardDetail.gameObject);
                LastCardDetail = null;
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

        // ============================================================ 🆕 2026-10-19 · `WildcardDisplay` 徽记（自检读口）
        /// <summary>`WIldcard Display ▸ Army Icon` 当前阵营（`CardArmy` 值；自检用）。</summary>
        public int WildcardArmyForTest { get { return _wcArmy; } }
        /// <summary>那一格当前贴的 sprite 名（自检用；建页前 = 出厂 BlackLegion）。</summary>
        public string WildcardArmyArtForTest { get { return _wcArmyArt; } }
        /// <summary>换旗入口（自检直调 —— 生产路径只有两处：卡格悬停 / `OpenCardDetail`）。
        /// 行为与生产完全同一口（`SetWildcardArmy`，含「认不出保留原图 + 出声」）。</summary>
        public void SetWildcardArmyForTest(int army, string why) { SetWildcardArmy(army, why); }

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
            // 🆕 **2026-10-22（W64）**：**新建的卡组继承当前模式** —— 原版 `SelectDecksTab.CreateDeck`（§5·5·6）：
            //   `deck = CardDeck.GetEmptyDeck(); deck.gameMode = (PlayModes)currentEvent; …`
            //   ⇒ 「新建的卡组一定落在当前模式上」。我们原来恒传 0（= 经典）⇒ 永远建经典（W63 §二 记的那条偏离）。
            //   `_deckModeFilter < 0`（还没选模式 —— 正常流程走不到：`Create` 在模式页是藏着的）⇒ 退回经典 0。
            int mode = _deckModeFilter >= 0 ? _deckModeFilter : (int)GameMode.Classic;
            string name = CollectionData.CreateDeck(mode);
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
            // 两页**都已经**只建视口内的格，自检 `Editor/CollectionScene.cs:3883` / `CardPresentation/Editor/CollectionScene.cs` 逐条断着 18 / 6）。
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
            // ============================================================ 🆕 2026-10-20（`A1834`）
            // **before → after 快照：建这一页卡，字形表到底长没长？**
            //
            // 原账的病灶（判据 → `资料/普查产出_第二十八会话/A1814_A1813_诊断.md` §A1813 ③）：
            //   `TimeOneTextRelayout` 只排在 `RebuildCardsCells` **之后** —— 卡在建的时候就 `ForceMeshUpdate`
            //   过（`Core/CardView.cs:2476` 等）、文本之后再没变 ⇒ 那一次量到的 `0.0 ms` 是**必然结果**，
            //   它**结构上**回答不了「每个新字形是不是要现烘」这个问题，日志却拿它当「假设被推翻」的证据。
            // ⇒ 把**「before 快照」挪到变化之前**（`RebuildCardsCells` 的上一句）：
            //   `TextProbeBegin` 记 **字形/字符计数 + 时刻**，`TextProbeEnd` 在建完之后算 Δ：
            //     · **Δ字形 = 0** ⇒ 这一页一张新字形都没烘 ⇒「字体现烘」**当场排除**（与耗时大小无关）；
            //     · **Δ字形 > 0** ⇒ 上界 = 整段 ms ÷ Δ字形（拿这个数去对「单卡 ≈2.2 s」才成立）。
            // ⚠️ **位置不变量**（批处理也记，供 `Editor/CollectionScene.cs` 断言）：
            //   `TextRelayoutProbe.CardsAtBegin` **必须是 0** —— 快照那一刻一张卡格都还没建。
            //   把 `TextProbeBegin` 挪回 `RebuildCardsCells` 之后 ⇒ 变成 18 ⇒ `CollectionScene.Run` 红。
            // ⚠️ **字形计数只在真 Play 读**：`TmpFont.Font` 是惰性加载（`Core/TmpFont.cs:108` 第一取就
            //   `Resources.Load`）—— 批处理里提前读它会把「字体资产首次加载」从第一张卡的
            //   `CardView.Create` 挪到这句，**扰动 `A1813` 的每格计时**。⇒ 批处理只记账位置（`CardsCells.Count`，
            //   零开销），字形的 before/after 留给真 Play；读不到时如实写「取不到」，⛔ 不写成「Δ = 0」。
            static bool _tpOpen;
            static float _tpT0;
            static int _tpCards0 = -1, _tpGlyphs0 = -1, _tpChars0 = -1;

            /// <summary>**变化之前**的快照（`A1834`）。调用点必须排在 `RebuildCardsCells` <b>之前</b>。
            /// <paramref name="cardsAlreadyBuilt"/> = 那一刻 `CardsCells.Count`（正确值 = **0**）。</summary>
            public static void TextProbeBegin(int cardsAlreadyBuilt)
            {
                if (_tpOpen) TextRelayoutProbe.UnclosedBegins++;
                TextRelayoutProbe.Begins++;
                TextRelayoutProbe.CardsAtBegin = cardsAlreadyBuilt;
                if (cardsAlreadyBuilt > 0) TextRelayoutProbe.BeginsWithCards++;
                _tpOpen = true;
                _tpCards0 = cardsAlreadyBuilt;
                _tpT0 = Time.realtimeSinceStartup;
                _tpGlyphs0 = _tpChars0 = -1;
                if (!Application.isPlaying) return;
                var f = TmpFont.Font;
                if (f == null) return;
                _tpGlyphs0 = f.glyphTable != null ? f.glyphTable.Count : -1;
                _tpChars0 = f.characterTable != null ? f.characterTable.Count : -1;
            }

            /// <summary>**变化之后**那一半（`A1834`）：算 Δ 并出声（只在真 Play 出声）。</summary>
            public static void TextProbeEnd(int cardsAfter)
            {
                TextRelayoutProbe.Ends++;
                if (!_tpOpen) return;
                _tpOpen = false;
                double ms = (Time.realtimeSinceStartup - _tpT0) * 1000.0;
                TextRelayoutProbe.BuildMs = ms;
                int g1 = -1, c1 = -1;
                if (Application.isPlaying)
                {
                    var f = TmpFont.Font;
                    if (f != null)
                    {
                        g1 = f.glyphTable != null ? f.glyphTable.Count : -1;
                        c1 = f.characterTable != null ? f.characterTable.Count : -1;
                    }
                }
                TextRelayoutProbe.GlyphsAtBegin = _tpGlyphs0; TextRelayoutProbe.GlyphsAtEnd = g1;
                TextRelayoutProbe.CharsAtBegin = _tpChars0; TextRelayoutProbe.CharsAtEnd = c1;
                if (!Application.isPlaying) return;
                var sb = new System.Text.StringBuilder();
                sb.Append("[Collection·计时] `CardsTab` 建卡 **before → after**（`A1834`：快照在 `RebuildCardsCells` 之前）：\n");
                sb.Append("    · 卡格 ").Append(_tpCards0).Append(" → ").Append(cardsAfter)
                  .Append(" · 这一段 **").Append(ms.ToString("F1")).Append(" ms**\n");
                if (_tpGlyphs0 >= 0 && g1 >= 0)
                {
                    int dg = g1 - _tpGlyphs0, dc = c1 - _tpChars0;
                    sb.Append("    · 字形 ").Append(_tpGlyphs0).Append(" → ").Append(g1)
                      .Append("（**Δ ").Append(dg).Append("**）· 字符 ")
                      .Append(_tpChars0).Append(" → ").Append(c1)
                      .Append("（**Δ ").Append(dc).Append("**）\n");
                    if (dg <= 0)
                        sb.Append("    ⇒ **Δ字形 = 0 ⇒ 这一页一张新字形都没烘** ⇒ 「字体现烘」这条假设**不能**解释这段耗时（当场排除，⛔ 与耗时大小无关）。\n");
                    else
                        sb.Append("    ⇒ 每新字形上界 **").Append((ms / dg).ToString("F2"))
                          .Append(" ms**（整段 ÷ Δ字形）—— 拿它去对「单卡 ≈2.2 s」：对得上假设才成立，对不上就推翻。\n");
                }
                else
                    sb.Append("    · 字形读数：**取不到**（`TmpFont.Font == null`）⇒ 本行只报耗时；⛔ 别把「取不到」读成「Δ = 0」。\n");
                Debug.Log(sb.ToString());
            }

            /// <summary>量**一次 `ForceMeshUpdate` 有多贵**。
            ///
            /// <para>**为什么怀疑它**：本仓**只有一个字体**（`NotoSerifCJK-Regular SDF`），而它
            /// `m_AtlasPopulationMode = 1`(Dynamic)、**图集是 1×1 占位**（出处 `Core/TmpFont.cs:27-28`）
            /// ⇒ 每个字形都得从 TTF 现烘。建一张卡要走的文字重排是**几十次**
            /// （`FitToBox` 的 8 步二分每步一次 · `FitToWidth` 两次 · `SetClipFromTree` 那次裁切逐字一次），
            /// 若单次是 10ms 量级，2.2 秒就对上了；若单次是 0.2ms，那这条假设**当场被推翻**
            /// （⛔ 那时别再往字体上查，改去看贴图/材质的首次加载）。</para>
            ///
            /// <para>🔴 **`A1834`（2026-10-20）订正 —— 本条量的是【warm 重排】，不是冷启动**：
            /// 调用点排在 `RebuildCardsCells` **之后**，那时这张卡的字形在建卡时就烘完了、文本也没再变
            /// ⇒ 两次 `ForceMeshUpdate` 都**不可能**含「字形现生成」。原口径「两次差得大 ⇒ 第 1 次含
            /// 字形现生成」在这条位置上是**恒假的推断**（量到 0.0 ms 是**必然结果**，判据 →
            /// `资料/普查产出_第二十八会话/A1814_A1813_诊断.md` §A1813 ③）。
            /// ⇒ 现在 ① 它只回答「**没有新字形**的一次重排多贵」（这个数仍然有用：× 每卡重排次数 = 上界）；
            /// ② 它**自己在前后各读一次字形表**，`Δ字形 &gt; 0` 才敢说「第 1 次含现生成」，
            /// `Δ字形 = 0` 就**明说这是 warm 读数、不能当证据**；
            /// ③ 「字体现烘」那条假设的判据**不在这里**，在 `TextProbeBegin/End` 的建卡 before → after 快照。</para>
            ///
            /// <para>⚠️ **量完必须把裁切重新贴回去**：`ForceMeshUpdate` 会把我们写进顶点的那一刀
            /// **冲掉**（这正是 `CardView.RestoreClip` 靠它还原原件的那条性质）⇒ 不补一句
            /// `SetClipFromTree()` 的话，被量的那张卡**文字会露出视口外**（真缺陷）。
            /// 补那一句之后画面与「不量这一下」逐位相同。</para>
            /// <para>⚠️ 与其余探针同闸：**只在真 Play 出声**。</para>
            /// </summary>
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
                    // 🔴 `A1834`：前后各读一次字形表 —— 这一对读数才决定「第 1 次是不是冷启动」，
                    //   而不是拿「两次耗时差得大」当推断（那是反过来的因果）。
                    var f = TmpFont.Font;
                    int g0 = (f != null && f.glyphTable != null) ? f.glyphTable.Count : -1;
                    float a = Time.realtimeSinceStartup;
                    t.ForceMeshUpdate();
                    float b = Time.realtimeSinceStartup;
                    t.ForceMeshUpdate();
                    float c = Time.realtimeSinceStartup;
                    int g1 = (f != null && f.glyphTable != null) ? f.glyphTable.Count : -1;
                    string cold;
                    if (g0 >= 0 && g1 >= 0)
                    {
                        int dg = g1 - g0;
                        cold = dg > 0
                             ? "（⚠️ 这两次共烘了 **" + dg + "** 个新字形 ⇒ **第 1 次确实含字形现生成**，"
                               + "拿「第 1 次 − 第 2 次」≈ 每个新字形的成本）"
                             : "（⚠️ **Δ字形 = 0 ⇒ 两次都是 warm 重排** —— 这两个数**不能**用来证实或推翻"
                               + "「字体现烘」假设；那个判据在上面的建卡 before → after 快照里，`A1834`）";
                    }
                    else
                        cold = "（⚠️ 字形读数**取不到**（`TmpFont.Font == null`）⇒ 判不了这一下是 warm 还是冷启动，"
                             + "⛔ 别把「判不了」写成「warm」。）";
                    Debug.Log("[Collection·计时] 单次文字重排（`" + t.name + "`，第 1 张卡，**warm** 基线）：第 1 次 **"
                              + ((b - a) * 1000f).ToString("F1") + " ms** · 第 2 次 **"
                              + ((c - b) * 1000f).ToString("F1") + " ms**" + cold
                              + "。⚠️ 本行之后已把那张卡的裁切补回（见本方法的 doc）。");
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

        // ============================================================ 🆕 2026-10-20（`A1834`）
        /// <summary>**`A1834` 的自检读口** —— 「文字重排探针到底在什么时候跑的」。
        /// <para>为什么要有它：原账那条探针排在 `RebuildCardsCells` **之后**（字形已烘、文本未变）
        /// ⇒ 量到的 `0.0 ms` 是**必然结果**，却被当成「字体现烘那条假设不成立」的证据
        /// （判据 → `资料/普查产出_第二十八会话/A1814_A1813_诊断.md` §A1813 ③）。</para>
        /// <para>读数与修法：① `CardsAtBegin` / `BeginsWithCards` 必须是 **0** —— 快照那一刻一张卡格
        /// 都还没建；把它挪回建卡之后 ⇒ 18 ⇒ `Editor/CollectionScene.cs` 那条断言**当场红**（灭自证）。
        /// ② `Begins == Ends`（before/after 成对）。③ `GlyphsAtBegin/End` 只在真 Play 有值（批处理 = −1，
        /// 理由见 `BuildWatch.TextProbeBegin` 的 doc：别让快照提前触发字体惰性加载、扰动 `A1813` 计时）。</para>
        /// <para>⚠️ **批处理与真 Play 都记**（只读 `CardsCells.Count`，零开销、零副作用）。</para></summary>
        public static class TextRelayoutProbe
        {
            public static int Begins;
            public static int Ends;
            /// <summary>最近一次 begin 那一刻已建好的卡格数。**正确值 = 0**（变化之前）。</summary>
            public static int CardsAtBegin = -1;
            /// <summary>begin 时已经有卡格的次数 —— **必须为 0**（否则探针又排到变化之后了）。</summary>
            public static int BeginsWithCards;
            /// <summary>上一个 begin 还没 end 就又 begin 的次数（before/after 没配对）。</summary>
            public static int UnclosedBegins;
            public static int GlyphsAtBegin = -1, GlyphsAtEnd = -1;
            public static int CharsAtBegin = -1, CharsAtEnd = -1;
            public static double BuildMs = -1.0;

            /// <summary>自检开场清账（`Editor/CollectionScene.cs` 在 `CollectionWindow.Create` 之前调）。</summary>
            public static void Reset()
            {
                Begins = Ends = 0;
                CardsAtBegin = -1;
                BeginsWithCards = 0;
                UnclosedBegins = 0;
                GlyphsAtBegin = GlyphsAtEnd = CharsAtBegin = CharsAtEnd = -1;
                BuildMs = -1.0;
            }
        }

        // ============================================================ 🆕 2026-10-25（R13 · 完全复刻批次 2）
        //   收藏「卡组页」的 **列表拖拽重排** —— 原版 12 个方法的落地。
        //   判据（唯二）→ `资料/完全复刻/判据/收藏_未读132条_第2轮.md` §二·1（`CollectionItem<T>` 8 条）/
        //   §二·2（`CollectionDisplay<T>` 4 条）/ §三（施工序）；反汇编实读 → 报告 `R8_收藏未读_第2轮.md` §五。
        //
        //   ## 原版三件（缺一不可）
        //     ① **输入** `DragAndDropItem`：按下记 parent + 把 ghost 摆到原 siblingIndex、本体 `SetParent(root)` 跟指针
        //        （`DragAndDropItem__OnBeginDrag.c` / `__OnDrag.c`）；松手找**最近的兄弟**再
        //        `SetSiblingIndex(nearest + 1)`，然后调**静态** `OnEndDragEvent(transform)`（`__OnEndDrag.c` 尾段，RVA 级实读）。
        //     ② **算法** `CollectionDisplay<T>.ReorderCollectionByHierarchy`：`siblingIndex = target.GetSiblingIndex()` →
        //        取 `siblingIndex-1` 那个兄弟（不是 CollectionItem 就再退一格、offset=1）→
        //        `newIndex = offset + filteredCollection.IndexOf(prev.Item)` → `Remove` → `Insert` →
        //        存 `normalizedPosition` → `ReloadData()` → 还回去。
        //        （RVA `0x15ED470` 逐条实读；`decomp_full` 里这一族**没有产物**，cpp2il 版本把 List 调用全丢了。）
        //     ③ **落盘** `DeckCollectionDisplay.SaveDeckOrder`：`collection` 与 `filteredCollection` **逐格比引用**，
        //        只要有一格不同 ⇒ `DeckInventory.UpdateOrder(filteredCollection)`；**两张表长度不等就直接 return**
        //        （RVA `0x1806E0000` 实读：`cmp [rcx+0x18],[rax+0x18]` 不等就跳到收尾 `ret`）。
        //        调用点 = `DeckCollectionDisplay.OnDisable`（RVA `0x1806DFFB0` 末句 `jmp 0x1806E0000` = **尾调**）。
        //
        //   ## 我们这三件的对位（①②③ 的**前置件今天都已就绪**，本段只做接线 + 数据算法）
        //     ① `Shell/PointerLayer.cs:313 RegisterDrag(btn, onDrag, onDrop)`（2026-10-25 落地，
        //        「可拖件优先于滚动区」+ 越过 `DragThreshold`=10 才算拖、拖过就**不派发 onClick**）。
        //     ② `DeckDropTargetPos`（最近的另一格）+ `MoveDeckInOrder`（Remove/Insert 到库序里）。
        //        滚动位置那一半**天然满足**：我们的 `RebuildDeckCells` 不碰 `MenuScroll.Offset`（只按新序重摆），
        //        与原版「存 `normalizedPosition` → ReloadData → 还回去」等价。
        //     ③ `RuleEngine/Data/DeckLibrary.cs:297 UpdateOrder(IList<string>)`（2026-10-25 落地：按名重排 +
        //        `_current` 跟人走 + 尾句 `return Save();`）。
        //
        //   ## 🔴 两处**如实偏离**（架构不同，不是漏做；R13 报告里有逐条交代）
        //     ⓐ **落盘时机**：原版把重排先只落在视图上、**等离页（`OnDisable`）才写盘**。我们**松手即写**。
        //        原因：`DeckLibrary` **没有**「只改内存不落盘」的口（`UpdateOrder` 尾句就是 `return Save();`），
        //        而它在 `Assets/RuleEngine/**`（本批白名单外，`E1`/`R11` 正在那边写）。
        //        可观测差别只有「写文件的时刻」；玩家看到的顺序一致 ⇒ 改动请求见报告 §六。
        //     ⓑ **拖起来那一格**：原版 `SetParent(root)` 跳出裁切/层级 + 原位留 ghost。我们是**绝对摆位网格**
        //        （没有 LayoutGroup ⇒ 原位不会被挤掉，不需要 ghost）⇒ **不 reparent**，只把它抬到
        //        `QPageRow + DeckDragQueueBoost`（高过本页页头与筛选栏 3044、仍低于弹窗那一段 3100+），
        //        免得同一队列下被邻格盖住（本工程透明件的层**靠 `SetRenderQueue`、不靠 z**）。
        //        ⚠️ 因此**拖到视口外不会被裁**（原版会）—— 如实记，没有装作复刻了。

        /// <summary>拖起来那一格抬多少队列。值 = **3044（`QFltHit`，本页最高的筛选栏）之上、3100（弹窗那一段）之下**
        /// —— ⛔ 别抬到 3100+：那会盖到随后弹出来的 `Deck info Popup` 上。</summary>
        public const int DeckDragQueueBoost = 20;

        /// <summary>正在被拖的那一格（`null` = 没在拖）。重建时会被清空（见 `RebuildDeckCells` 头部）。</summary>
        Transform _deckDragCell;
        /// <summary>起拖前那一格的 `localPosition`（位移的**基准**；松手后一律重建 ⇒ 不需要单独还原）。</summary>
        Vector3 _deckDragHomeLocal;
        /// <summary>起拖那一帧的指针（`PointerLayer` 给的**设计帧 px**，见 `RegisterDrag` 的 doc）。</summary>
        Vector2 _deckDragPressPx;
        /// <summary>起拖时它在**显示序列**里的序号（= `FilteredDeckIndices()` 的下标）。</summary>
        int _deckDragFromPos = -1;
        /// <summary>🔴 **格子 → 显示序号**。⛔ 别拿 `DeckCells.IndexOf` 当序号 —— `RebuildDeckCells` 会
        /// **跳过视口外的格子**（`!DeckScroll.Intersects(r) ⇒ continue`）⇒ 滚到第二屏时
        /// `DeckCells[0]` 是**第 12 个**显示序号，两者只在「一屏装得下」时相等。
        /// 每趟重建清空重填（与 `DeckCells` 同一趟、同一批格子）。</summary>
        readonly Dictionary<Transform, int> _deckCellPos = new Dictionary<Transform, int>();
        /// <summary>`CollectionItem&lt;T&gt;.listenToPointerEvents` 的等价物 —— 原版 `OnStartDragEvent` 置 false、
        /// `OnEndDragEvent` 置 true（RVA `0x54B310` = `c6 41 40 00` / `0x15F2950` = `c6 41 40 01`）。
        /// <para>⚠️ 我们**不是在格子上再挂一层闸**：本工程没有 uGUI 事件，点击/悬停都走 `PointerLayer`，
        /// 而它「拖起来过 ⇒ 抬起不派发 `onClick`」已经把这一条吃掉了；悬停那一半另有 `HoverSuppressed`
        /// （鼠标主键按住 = 可能有拖拽）。本字段只作**状态读口**给自检与诊断用。</para></summary>
        bool _deckDragLive;
        static bool _deckDragNoPointerWarned;

        /// <summary>自检读口：正在拖吗（= 原版 `CollectionItem.listenToPointerEvents == false` 的等价态）。</summary>
        public bool DeckDragActive { get { return _deckDragLive; } }
        /// <summary>自检读口：这一趟从哪个显示序号起拖（`-1` = 没在拖）。</summary>
        public int DeckDragFromPos { get { return _deckDragFromPos; } }
        /// <summary>自检读口：一格在**当前显示序列**里的序号（不在这一趟里 ⇒ −1）。</summary>
        public int DeckCellDisplayPos(Transform cell)
        {
            int p;
            return cell != null && _deckCellPos.TryGetValue(cell, out p) ? p : -1;
        }

        /// <summary>原版 `CollectionDisplay&lt;T&gt;.Select(collectable, button)` 的**第一道闸**：
        /// **列表正在被拖动时，点格子什么都不做**。
        /// <para>判据（RVA `0x1815ED9C0` 反汇编前 8 条，实读）：
        /// `mov rax,[this+0x20]`（= `listView`）→ `cmp byte [rax+0x18a], 0` → `jne 收尾`。
        /// `+0x18A` = `RecyclableScrollRect.Dragging`（`d:/2/tools/il2cpp_out/dump.cs:120852`），
        /// 它的**全部**写入点只有两处：`OnBeginDrag` 置 1 / `OnEndDrag` 置 0
        /// （`decomp_full/PolyAndCode.UI.RecyclableScrollRect__OnBeginDrag.c`、`__OnEndDrag.c` 各一行）
        /// ⇒ 语义 = **「按着列表在拖」这一段**，⛔ **不含惯性滑行**（松手那一刻它已经是 0）。</para>
        /// <para>我们的等价物 = `MenuScroll.Dragging`（`m_Dragging`，同一条 Begin/End 语义）。</para>
        /// <para>⚠️ 真指针那条路上这条已经被 `PointerLayer` 吃掉了（拖起滚动区 ⇒ 抬起**不派发** `onClick`）；
        /// 这里补的是**同一句原版行为**：程序化 `Click()` / 自检直调在拖动途中也照样被挡住
        /// （⛔ 别因为「真输入碰不到」就删 —— 原版这一句在基类，四个页签共用）。</para></summary>
        static bool ListBusy(MenuScroll s) { return s != null && s.Dragging; }

        /// <summary>原版 `DeckCollectionTab.GetCollection()` 的等价物：**只过「模式」那一道筛**的那一份（库序；
        /// `_deckModeFilter &lt; 0` ⇒ 全部）。
        /// <para>🔴 为什么必须单列一份：原版那张**权威表** `collection`(`+0x50`) 收的就是 `GetCollection()` 的结果 ——
        /// **已经按当前 liveops 事件（= 我们的 `_deckModeFilter`）筛过**；名字 / 阵营那两道才是
        /// `filteredCollection`(`+0x58`) 做的。`SetCell` 的闸比的是**这两张表**的长度
        /// ⇒ **模式筛不算「有筛选」**（选了模式照样能拖）。</para>
        /// <para>⚠️ **本轮自查抓到的**：第一版直接拿 `CollectionData.DeckCount()`（= 全库）去比，
        /// 而生产流程「进 Deck 页 ⇒ 先出模式页 ⇒ 点一张模式卡」**必然**设上 `_deckModeFilter`
        /// （`ChooseDeckMode:4557`）⇒ 那样写等于**整个功能在默认流程里永远关着**。</para></summary>
        List<int> BaseDeckIndices()
        {
            var list = new List<int>();
            int n = CollectionData.DeckCount();
            for (int i = 0; i < n; i++)
            {
                if (_deckModeFilter >= 0 && CollectionData.DeckAt(i).GameMode != _deckModeFilter) continue;
                list.Add(i);
            }
            return list;
        }

        /// <summary>原版 `CollectionDisplay&lt;T&gt;.SetCell` 里那道闸 —— **能不能拖，只此一处判**。
        /// <para>判据（RVA `0x1815EEB64`，反汇编逐条）：`cmp byte [this+0x49], 0`（= `allowItemDrag`）
        /// ⇒ 非 0 才比 `collection.Count`(`+0x50`) 与 `filteredCollection.Count`(`+0x58`)，
        /// **相等**时才 `GetComponent&lt;DragAndDropItem&gt;(cell).Initialize(父 ScrollRect)` + `Toggle(true)`；
        /// 否则 `Toggle(false)`。</para>
        /// <para>⇒ **原版：有「名字 / 阵营」筛选时格子根本不可拖**（两张表长度不等）。
        /// 我们照抄这一条，比较基准是 <see cref="BaseDeckIndices"/>（= 模式筛之后，**不含**名字/阵营筛）。</para>
        /// <para>`allowItemDrag` 那一半我们在**调用点**表达（只有 Deck 页调 `AttachDeckDrag`；
        /// 原版三份 prefab 里也只有 Deck 那一份是 1，见 `BuildDeckCell` 尾部那段出处）。⚠️ **这里「能拖」是原版行为，不是我们的加法** —— prefab 里 `DragAndDropItem.autoInitialize=0`+`scrollRect=null` 是故意的（`SetCell` 运行时 `Initialize`+`Toggle(true)` 才打开），**勘误详见本文件末尾那段 `🔴🔴 勘误`**。</para></summary>
        bool DeckDragAllowed()
        {
            var baseIdx = BaseDeckIndices();
            if (baseIdx.Count < 2) return false;               // 一格没有「前后」可言
            return FilteredDeckIndices().Count == baseIdx.Count;
        }

        /// <summary>把一格挂成可拖件（原版 `CollectionItem&lt;T&gt;.OnEnable` 里往**静态**
        /// `DragAndDropItem.OnStartDragEvent / OnEndDragEvent` 上挂的两个回调 —— 我们按格登记，
        /// 两个回调在同一处给齐，等价）。
        /// <para>⚠️ `PointerLayer` 不在时**只出声一次**（每格一条会把日志刷满，而那是「环境没建」不是「这一格有错」）。</para></summary>
        void AttachDeckDrag(Transform cell, int displayPos)
        {
            if (cell == null || displayPos < 0) return;
            if (PointerLayer.Instance == null)
            {
                if (!_deckDragNoPointerWarned)
                {
                    _deckDragNoPointerWarned = true;
                    Debug.LogWarning("[Collection] `PointerLayer` 不在 ⇒ Deck 页的**卡组拖拽重排不会发生**"
                                     + "（先 `PointerLayer.Ensure()`；出声一次，不静默）");
                }
                return;
            }
            var wb = cell.GetComponentInChildren<WindowButton>(true);
            if (wb == null) return;                             // 整格被视口裁没了（`MenuDraw.DeckCell` 不建 `Hit`）
            PointerLayer.RegisterDrag(wb,
                pos => DeckDragMove(cell, displayPos, pos),
                pos => DeckDragDrop(cell, displayPos, pos));
        }

        /// <summary>原版 `DragAndDropItem.OnDrag`（跟指针）+ `CollectionItem` 的 `OnBeginDrag/OnDrag` 转发。
        /// <para>`pos` = `PointerLayer` 给的**设计帧 px**（父链缩放还留着，见 `RegisterDrag` 的 doc）；
        /// 我们只取**相对起拖点的位移**，再换回设计单位（`Δdesign = Δpointer / k`，按画布中心为不动点 ——
        /// 判据 = `MenuDraw.PointerFramePxFromDesign` 那条恒等式 `pointer = C + (design − C)·k`）。</para></summary>
        void DeckDragMove(Transform cell, int displayPos, Vector2 pos)
        {
            if (cell == null) { _deckDragLive = false; return; }
            // ⚠️ `_deckDragCell != cell` 也当成「新的一趟」：万一上一趟没收尾（重建 / 断线），
            //    不能让这一趟去拖上一趟那一格的位移。
            if (!_deckDragLive || _deckDragCell != cell)
            {
                _deckDragLive = true;
                _deckDragCell = cell;
                _deckDragHomeLocal = cell.localPosition;
                _deckDragPressPx = pos;
                _deckDragFromPos = displayPos;
                // 原版在这一刻 `SetParent(root)`（⇒ 画在最上）；我们不 reparent ⇒ 用队列做等价（见本节头注释 ⓑ）。
                var quads = cell.GetComponentsInChildren<ImageQuad>(true);
                for (int i = 0; i < quads.Length; i++)
                    if (quads[i] != null) quads[i].SetRenderQueue(quads[i].RenderQueue + DeckDragQueueBoost);
            }
            cell.localPosition = _deckDragHomeLocal + DeckDragDesignDelta(pos - _deckDragPressPx, cell.parent);
        }

        /// <summary>指针位移（**指针帧 px**）→ 本格的 `localPosition` 位移（**设计单位**）。
        /// <para>两跳：① 除回父级 `lossyScale`（`pointer = C + (design − C)·k` 的逆，逐分量）；
        /// ② 设计 px → 设计世界（`LayoutSpace.FromPixel` 的**差**，⛔ 不用绝对值 —— 那个映射的零点不在原点，
        /// 差才是位移）。</para>
        /// <para>⚠️ 退化：`k` 非有限 / ≈0 ⇒ 该分量按 1（同 `MenuDraw.ScaleAboutPx` 的守卫）。</para></summary>
        static Vector3 DeckDragDesignDelta(Vector2 pointerDelta, Transform parent)
        {
            var k = parent != null ? parent.lossyScale : Vector3.one;
            float dx = (float.IsNaN(k.x) || float.IsInfinity(k.x) || Mathf.Abs(k.x) < 1e-6f)
                       ? pointerDelta.x : pointerDelta.x / k.x;
            float dy = (float.IsNaN(k.y) || float.IsInfinity(k.y) || Mathf.Abs(k.y) < 1e-6f)
                       ? pointerDelta.y : pointerDelta.y / k.y;
            return LayoutSpace.FromPixel(dx, dy) - LayoutSpace.FromPixel(0f, 0f);
        }

        /// <summary>原版 `DragAndDropItem.OnEndDrag` 的**落点算法**：在（没被拖走的）兄弟里找**离指针最近**的那一格。
        /// <para>判据（`DragAndDropItem__OnEndDrag.c` 实读）：`minDist = float.MaxValue`，
        /// 逐个 `content.GetChild(i)` 算 `Vector3.Distance(child.position, this.transform.position)`，
        /// 取最近者 `best`，然后 `SetSiblingIndex(best + 1)`；随后 `ReorderCollectionByHierarchy` 用
        /// `siblingIndex − 1`（= `best`）那格的 `Item` 反查下标 ⇒ **等价于「插到最近那一格的位置上」**。</para>
        /// <para>⚠️ 原版那一刻「本体」已经 reparent 到 root、不在 `content` 的兄弟里；我们**不 reparent**，
        /// 所以这里必须**显式跳过自己**（`c == dragged`）—— 否则最近的一定是它自己（距离 0）。</para>
        /// <para>🔴 遍历的是 <see cref="_deckCellPos"/>（格子 → **显示序号**），⛔ 不是 `DeckCells` 的下标：
        /// 视口外的格子**根本没建**（`RebuildDeckCells` 会 `continue`）⇒ 两个下标只在「一屏装得下」时相等。</para></summary>
        int DeckDropTargetPos(Transform dragged)
        {
            int best = -1;
            float bestSq = float.MaxValue;
            Vector3 p = dragged != null ? dragged.position : Vector3.zero;
            foreach (var kv in _deckCellPos)
            {
                var c = kv.Key;
                if (c == null || c == dragged) continue;
                float d = (c.position - p).sqrMagnitude;
                if (d < bestSq) { bestSq = d; best = kv.Value; }
            }
            return best;
        }

        /// <summary>原版 `CollectionDisplay&lt;T&gt;.OnEndDragEvent` + `ReorderCollectionByHierarchy` 的落地。
        /// <para>原版首句是 `if (gameObject.activeInHierarchy) ReorderCollectionByHierarchy(target)` ——
        /// 我们这里 `cell == null`（重建过 / 已销毁）就是「不在场」那一档。</para></summary>
        void DeckDragDrop(Transform cell, int displayPos, Vector2 pos)
        {
            var dragged = _deckDragCell != null ? _deckDragCell : cell;
            int from = _deckDragLive ? _deckDragFromPos : displayPos;
            bool wasLive = _deckDragLive;
            _deckDragLive = false;
            _deckDragCell = null;
            _deckDragFromPos = -1;
            if (dragged == null) return;

            int to = DeckDropTargetPos(dragged);
            // 位置没变（拖了一点点又放回原处）⇒ **不动数据**，只把这一格挪回原位、队列靠重建还原。
            if (wasLive && to >= 0 && to != from) MoveDeckInOrder(from, to);
            // ⚠️ **无论变没变都重建一趟**：这一趟把「拖起来的 `localPosition`」「抬高的渲染队列」一起还原
            //   （原版靠 `SetParent(content)` + `SetSiblingIndex` + `ReloadData` 收这个尾）。
            RebuildDeckListNow();
        }

        /// <summary>把显示序第 <paramref name="fromPos"/> 格挪到第 <paramref name="toPos"/> 格的位置上，
        /// 并**落盘**（原版 `DeckInventory.UpdateOrder`）。
        /// <para>算法照 `ReorderCollectionByHierarchy`（RVA `0x15ED470` 实读）：新下标取自**目标格在移动前的**下标
        /// ⇒ `Remove(from)` 之后直接 `Insert(to)`（移动前 `to` 那格的槽位，移除后正好前移一格给它）。</para>
        /// <para>⚠️ **按名重排**（`DeckLibrary.UpdateOrder` 的口）：我们没有 `deckId`，`PlayerDeck` 只有 `Name`
        /// （老账见 `Shell/CollectionData.cs` 文件头）⇒ 同名卡组有歧义，`UpdateOrder` 自己会出声。</para></summary>
        /// <returns>真的写进库并落盘了吗。</returns>
        bool MoveDeckInOrder(int fromPos, int toPos)
        {
            var baseIdx = BaseDeckIndices();
            // 拖拽只可能在「显示序 == 模式筛之后的全序」时发生（见 `DeckDragAllowed`）；
            // 不满足 ⇒ 位置语义对不上库序，**不做**（出声，不静默改错顺序）。
            if (FilteredDeckIndices().Count != baseIdx.Count)
            {
                Debug.LogWarning("[Collection] 卡组拖拽重排被跳过：当前有名字/阵营筛选 ⇒ 显示序与库序对不上"
                                 + "（原版 `SetCell` 在两张表长度不等时根本不给拖）");
                return false;
            }
            if (fromPos < 0 || fromPos >= baseIdx.Count || toPos < 0 || toPos >= baseIdx.Count) return false;
            if (fromPos == toPos) return false;

            int n = CollectionData.DeckCount();
            var names = new List<string>(n);
            for (int i = 0; i < n; i++)
            {
                var d = CollectionData.Raw(i);
                names.Add(d != null ? (d.Name ?? "") : "");
            }
            // 🔴 **只重排模式筛那一段**（`baseIdx` 那些槽位）；别的模式的卡组**留在原位**。
            //   原版是 `UpdateOrder(filteredCollection)`（只把这一组交给背包）—— 我们这份 `UpdateOrder`
            //   按名字摆整张表，所以必须**自己把非本组的那几格钉回原位**，否则它们会被挤走
            //   （那会是一次「拖一格动全库」的静默破坏）。⚠️ 这是对齐原版**结果**的补丁，不是原版写法。
            var baseNames = new List<string>(baseIdx.Count);
            for (int k = 0; k < baseIdx.Count; k++) baseNames.Add(names[baseIdx[k]]);
            string moved = baseNames[fromPos];
            baseNames.RemoveAt(fromPos);
            baseNames.Insert(toPos, moved);          // 新下标 = 目标格**移动前**的序号（同原版 Remove/Insert）
            for (int k = 0; k < baseIdx.Count; k++) names[baseIdx[k]] = baseNames[k];

            // 🔴 **落盘走 `CollectionData.ReorderDecks`（= 原版 `DeckInventory.UpdateOrder` 的薄口）**。
            //   2026-10-25（`R17`）收掉了 `R13` 那条绕路（`DeckLibrary.Load()` → `UpdateOrder` → `InvalidateLibrary`）：
            //   绕路会**重读一次盘**、丢掉 `CollectionData` 的进程级缓存 ⇒ 将来有人在内存里改了卡组**没落盘**时它会丢。
            //   薄口直接改缓存那一份（`DeckLibrary.UpdateOrder` 原地改 `_decks` 后 `return Save()`）⇒ 顺序当场就是新的，
            //   `RebuildDeckCells` 不必再作废缓存就画得对；失败原因由 `ReorderDecks` 写进 `LastReorderError`（⛔ 别在这里再拼一遍）。
            if (!CollectionData.ReorderDecks(names))
            {
                Debug.LogWarning("[Collection] 卡组拖拽重排失败（是哪一种看 `LastReorderError`，⛔ 别在这里再判一遍）："
                               + CollectionData.LastReorderError);
                return false;
            }
            // ⚠️ 成功那一支的日志在下面（那两行没动；失败那一支的人话由 `ReorderDecks` 负责）。
            Debug.Log("[Collection] 卡组拖拽重排：「" + moved + "」→ 第 " + (toPos + 1) + " 格"
                      + "（本组 " + baseIdx.Count + " 套，" + fromPos + " → " + toPos + "）");
            return true;
        }

        /// <summary>自检口：直接走一次「把显示序第 `fromPos` 格挪到 `toPos`」（不经过指针层）。
        /// ⚠️ 仅供 `Editor/CollectionScene.cs` 断言用；生产路径一律走 `AttachDeckDrag` 那条真指针链。</summary>
        public bool DeckMoveForTest(int fromPos, int toPos) { return MoveDeckInOrder(fromPos, toPos); }

        // ============================================================ 🆕 **2026-10-25（执行代理 `R17`）**：收藏格点击音 + 拖拽自检口
        //
        // 判据（`R13` §七·1 / §七·3 已查好，本轮只**落地**、不重查；cue→clip 那一跳本轮亲核）：
        //   · `Collection Card`（GO `-3932997974561655464`）`onClickSound` = pid `604076303329428464`
        //     ⇒ **`CardClick 1`**（`clipList` 唯一一条 = `UI Select Toggle` · pitch 0.8–1.1 · vol 1–1 · ttp 0.1）。
        //     ⚠️ 它与 `ButtonClick` **共用同一个 clip**，差别只在 pitch ⇒ ⛔ 别把两条 cue 合并（那会把随机 pitch 弄丢）。
        //   · `Collection Cosmetic`（GO `-586895015824482954`）= **null** ⇒ 卡背格**不响**（`playClickSound = false`）。
        //   · `Collection Deck` 那一条**不在这里**：卡组格只有一个画法（`MenuDraw.DeckCell`），
        //     cue 常量落在 `MenuDraw.DeckCellClickCue`（⛔ 别在这儿再抄一遍 —— 两处写同一条规则 = 迟早不一致）。

        /// <summary>卡池格 / 异画格的点击音（原版 `CollectionItem.onClickSound` 的 `Collection Card` 那一档）。
        /// 🔴 **为什么这个常量在本文件、而不在 `Shell/WindowSounds.cs`**：本轮 `R17` 的白名单**不含**
        /// `WindowSounds.cs` ⇒ 先落在**唯一使用点**（`RebuildCardsCells` / `RebuildStyleCells` 两处 `AddHit`）。
        /// 将来要把壳侧 cue 收口到那张「唯一一张表」，**只需搬这一行**（值不许改）。</summary>
        public const string CardClickCue = "CardClick 1";

        /// <summary>把一颗命中区的按钮音**关掉**（原版 `Collection Cosmetic.onClickSound = null` ⇒ 卡背格不响）。
        /// <para>⚠️ 走「建完再改」而不是给 `MenuDraw.Hit` 加形参：`Hit` 是**共用件**、本轮不扩它的签名
        /// （`DeckCell` 那两个尾参是另一回事 —— 它自己建 `WindowButton`）。`null` / 视口外
        /// （`Hit` 整块在视口外时**连节点都不建**、返回 null）一律安全早退。</para></summary>
        static void DisableClickSound(Transform hit)
        {
            if (hit == null) return;
            var wb = hit.GetComponent<WindowButton>();
            if (wb != null) wb.playClickSound = false;
        }

        /// <summary>按**显示序号**取这一格。判据 = <see cref="_deckCellPos"/>（⛔ 不是 `DeckCells` 的下标：
        /// 视口外的格子根本没建 ⇒ 两个下标只在「一屏装得下」时相等，见那个字段的 doc）。</summary>
        Transform DeckCellAtDisplayPos(int displayPos)
        {
            foreach (var kv in _deckCellPos)
                if (kv.Value == displayPos && kv.Key != null) return kv.Key;
            return null;
        }

        /// <summary>自检口：走**真的落点算法 + 真的落盘 + 真的重建**那一段（`DeckDragDrop`），
        /// 只省掉 `PointerLayer` 的 press/move/release 派发（那一层要真鼠标 + 帧循环，批处理里都没有）。
        /// <para>做法（= 玩家那一趟的可观测等价）：起拖（`DeckDragMove` 记 home / 抬渲染队列）→
        /// 把这一格摆到**目标格的中心**（「指针压在目标格上」那一瞬）→ `DeckDragDrop`
        /// （`DeckDropTargetPos` 找最近 ⇒ `MoveDeckInOrder` ⇒ `RebuildDeckListNow`）。</para>
        /// <para>⚠️ **如实记**：**不覆盖** `PointerLayer.Update` 那两条（10px 起拖阈值 · 拖过就不派发 `onClick`）
        /// —— 它们的判据在 `Shell/PointerLayer.cs`（本轮不在白名单）⇒ 真机验法见 `R17` 报告 §四。</para></summary>
        /// <returns>两格都建着（在视口内）并跑完那一趟 ⇒ true。</returns>
        public bool DeckDragDropForTest(int fromPos, int toPos)
        {
            var from = DeckCellAtDisplayPos(fromPos);
            var to = DeckCellAtDisplayPos(toPos);
            if (from == null || to == null) return false;
            DeckDragMove(from, fromPos, Vector2.zero);      // 起拖：记 `_deckDragHomeLocal` / `_deckDragPressPx`
            from.position = to.position;                    // = 指针落在目标格中心（落点算法取最近格）
            DeckDragDrop(from, fromPos, Vector2.zero);      // 真落点 + 真落盘 + 重建
            return true;
        }

        /// <summary>关窗时把本窗名下的**可拖件登记**撤掉 —— 原版 `DeckCollectionDisplay.OnDisable()`
        /// （`CollectionDisplay.cs:46`：`base.OnDisable()` 退订 + `SaveDeckOrder()`）里**退订那一跳**的等价物。
        /// <para>⚠️ 两处如实差异：① 原版摘的是 `DragAndDropItem` 的**静态事件回调**（那是它的拖拽层）；
        /// 我们摘的是 `PointerLayer.UnregisterDragsOwnedBy(gameObject)`（我们的拖拽层）。② 原版**同一处**还会
        /// `SaveDeckOrder()`（逐格比 Item、顺序变了才落盘）；我们是**松手即落盘**（`MoveDeckInOrder` → `CollectionData.ReorderDecks`），
        /// 所以这里**不重复写盘** —— 可观测结果相同（顺序已落），时机不同（已明账）。</para>
        /// <para>⚠️ **不加也不会出错**：`PointerLayer.HitButton` 只认激活件 ⇒ 关掉的窗不会误命中；
        /// 只是登记表要等到下一次 `RebuildDeckCells` 才被清。补这一条是为了**与原版同一时机**，不是修 bug。</para></summary>
        public override void Close()
        {
            PointerLayer.UnregisterDragsOwnedBy(gameObject);
            base.Close();
        }
    }

}

// ============================================================================================
// 🔴🔴 勘误：**原版收藏窗的卡组格【是可以拖拽重排的】** —— 2026-10-26（用户亲自把关后重查）
//
// 这一条踩过一次，代价是「差点把一个原版真功能当成我们的多余发明删掉」。写完以后**别再重犯**。
//
// 【错在哪】
//   只看 prefab 的序列化字段，看到 `DragAndDropItem` 的 `autoInitialize = 0` 且 `scrollRect = null`，
//   就判定「组件出厂是关的 ⇒ 原版拖不动」。**那个读法漏了运行时的第二条开启路。**
//
// 【原版真机制（判据：`CollectionDisplay<T>.SetCell`，RVA `0x1815EEB64` 反汇编逐条）】
//   `cmp byte [this+0x49], 0`                     // `allowItemDrag`
//   ⇒ 非 0 才比 `collection.Count(+0x50)` 与 `filteredCollection.Count(+0x58)`
//   ⇒ **相等**时：`GetComponent<DragAndDropItem>(cell).Initialize(父 ScrollRect)` + `Toggle(true)`
//   ⇒ 否则：`Toggle(false)`
//   而 `DragAndDropItem.Initialize(ScrollRect)` 的体内第一句就是 **`enabled = true;`**
//   （随后存 `scrollRect`、没有占位件就 `CreateEmptyObject()`）——`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/DragAndDropItem.cs:79-104`
//   ⇒ **prefab 里 `autoInitialize = 0` / `scrollRect = null` 是【故意的】**：那两条是给
//     `Awake()` 那条「自动初始化」路用的；Deck 页走的是 `SetCell` **运行时注入**这条路。
//
// 【两条**不构成反证**的东西，别再拿它们说「原版拖不动」】
//   ① `DragAndDropItem.Awake()` 末尾 `if (!scrollRect) enabled = false;` —— 那是在 `SetCell` 之前跑的，
//      之后 `Initialize` 会把它打开。只看 `Awake` 会得出相反的结论。
//   ② `CollectionDeck.ToggleReorder(bool)` 全量 C# **0 调用者**、`assets_full` 字符串也 **0 命中** ——
//      它**根本不是**这套机制（我们一度把它当成唯一开关）。真正的开关是 `CollectionDisplay.SetCell`。
//
// 【原版「重新排序」落盘那一跳】
//   `DeckCollectionDisplay.SaveDeckOrder()`（比较显示序与旧序 → 变了才 `DeckInventory.UpdateOrder(list)`
//   → `PlayerDataManager.singletonManager.OrderDecks(playMode, 成功回调, 失败回调)`；
//   成功打 `"Success ordering decks"`、失败打 `"[ERROR] Can't reorder decks"`）⇒ 顺序是**存服务器**的。
//   我们对位 = `MoveDeckInOrder` → `CollectionData.ReorderDecks`（见 `DeckDragDrop`）。
//
// 【我们现在的状态 = 忠实复刻，不是超集】
//   `DeckDragAllowed()` 照抄了原版那道闸（`allowItemDrag` 那一半在调用点表达：只有 Deck 页挂拖拽；
//   另一半 = `FilteredDeckIndices().Count == BaseDeckIndices().Count`，即**有名字/阵营筛选时不给拖**）。
//   ⚠️ 所以账里凡是写「我们用超集 / 拖拽恒开」的措辞**都过期**，正解是「与原版一致」。
//
// 【用户口径】2026-10-26：「**卡组应该可以调整位置，否则不合理。**」—— 保留可拖，⛔ 不要关。
// ============================================================================================
