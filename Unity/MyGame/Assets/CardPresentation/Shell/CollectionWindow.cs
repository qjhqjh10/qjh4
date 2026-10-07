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
//      · 覆盖 **2 种风格**：`AA_HB`(6 张) + `v2`(1 张)；其余风格在远端 CCD 的 `alternateartstyles` 包。
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

        /// <summary>左栏四个键。文案是**卡面文案**（`Cosmetics`/`Styles`），不是页节点名。出处 A1 表 `:17-22`。</summary>
        public static readonly TabBtnSpec[] Buttons =
        {
            new TabBtnSpec("40k_collection_bt_decks",     "Decks",     36f, 5f, 36f, "DecksTabButton",     47.9f),
            new TabBtnSpec("40k_collection_bt_cards",     "Cards",     36f, 5f, 36f, "CardsTabButton",     47.9f),
            new TabBtnSpec("40k_collection_bt_cosmetics", "Cosmetics", 36f, 5f, 36f, "CardBacksTabButton", 47.9f),
            new TabBtnSpec("40k_collection_bt_style",     "Styles",    36f, 5f, 36f, "AltArtTabButton",    47.9f),
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
        /// （**在 `Shell/CardDetailPopup.cs:74` / `:79`**，⛔ 不是本文件）；⛔ **别退回不带卡型那一档**。</para></summary>
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
            var lb = Text(q.transform, CardsCounterText(def), tr.x1, tr.x2, tr.y1, tr.y2, 5, PageInk,
                          "Text (TMP)", CardsCounterFontPx);
            if (lb == null) return;
            lb.SetRenderQueue(QPageText);
            // 原版这颗开了 autosize、`m_fontSizeBase = 32`（框只有 22.69 高 ⇒ 运行时会被压小）。
            // ⚠️ 传的 4 个数是**原版 prefab 的字段值**（见上面那段现读）；`DeckRuntime` 那处传的上限是
            //    31.9（= `m_fontSize`，原版上限其实是 32），那条挂在 A333 上 ⇒ **别照它抄**。
            lb.SetAutoFitBox(LayoutSpace.Px(tr.W), LayoutSpace.Px(tr.H),
                             CardsCounterFontMin, CardsCounterFontMax, CardsCounterFontMax);
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
        /// ⛔ **别用 `SetWrapping(false)` 顶替** —— 那是 `0`(`NoWrap`)，会静默降级掉「空白保留」那一半。</summary>
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
            /// （`FltHiddenDx`）⇒ 这个标志今天防的是**可见错位**，不再是「整列空白」。
            /// 更彻底那条（让 `MenuDraw.Local` 的帧也取那份记录矩形）= **A834**，另开一趟；
            /// 判据 → `资料/普查产出_1016/W11_A811根治.md` §③·C。⛔ **别因为「不再空列」就把 ④ 删掉**。</para>
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
            //    🔴 **今天真正欠的那点差 = 落点**：`MenuDraw.Local(parent, r)` 算的是
            //      `RectCenter(r) − PosInDesignSpace(parent)`（父件**当下**位置）⇒ 面板停在收起位时建的
            //      那一版会**跟着面板一起滑回来**，到位那一刻偏 **+385px**（`FltHiddenDx`）⇒ ④ 把它
            //      **重排到位**。（0.3 秒滑入期间那批件偏 0→+385px = **A834** 记的那处过渡表现；
            //      更彻底的修法要让 `MenuDraw.Local` 的帧也取那份记录矩形 ⇒ **另开一趟**，同上那份报告 §④·3。）
            //    ⚠️ 「闸本身」不是缺陷、⛔ 别去动它（A798 已裁定「只裁不建在画面上等价」）——
            //      这里修的**只是重建时机**（按今天的口径，准确说是**重建的落点**）。
            //
            //    ✅ 三个限定都必要，⛔ 一个都别省：
            //      · `prevSlide < 1f` —— 只在**跨过 1** 那一拍触发；`StepDrawer` 每帧都调本函数，
            //        少了它就成了「每帧重建一次」；
            //      · `RowsBuiltOffBase` —— 只在这**真的欠着一版**时才建：无条件重建会把别人手里的
            //        节点引用打散（`CollectionScene` 那条「同一格现在又点得到了」正拿着一颗格的引用
            //        **跨过这一拍**）；它同时保证**起手建的那一版**（还没挪过杆子 ⇒ `HasBasePos == false`）
            //        不会被误判；
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
        /// <summary>卡组格 **225 × 364.5**（prefab 根 250×405 的 **0.9 倍**）· 横向 spacing **20** · pad **L10** ⇒ **6 列**。</summary>
        public const float DeckCellW = 225f, DeckCellH = 364.5f, DeckSpacingX = 20f, DeckPadL = 10f;
        public const float DeckCellScale = 0.9f;
        /// <summary>列数 = `floor(1589.1 ÷ (225+20))` = **6**（原版是运行时算的，A2 §四·1 说算法读不到）。</summary>
        public const int DeckCols = 6;

        public static PxRect DeckCellRect(int i)
        {
            int r = i / DeckCols, c = i % DeckCols;
            float x = DeckViewport.x1 + DeckPadL + c * (DeckCellW + DeckSpacingX);
            float y = DeckViewport.y1 + r * (DeckCellH + DeckSpacingX);
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
                var t = Text(page, "99", x + 30f, x + 71f, 91.5f, 135.5f, 5, PageInk,
                             "Wildcard Count " + i, 32.6f);
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
            CardsScroll = MenuScroll.TopAligned(CardsViewport, rows * CardsCellH);
            CardsScroll.Owner = gameObject;
            // 🔴 **A465**：构建循环那一路「整块在视口外就不建」也要吃这颗节点
            //   （⛔ 别改成让 `Intersects` 沿 `Owner` 找 —— `Owner` 是窗根、节点在它下面，找不到）。
            CardsScroll.ClipNode = cardsVp;
            CardsScroll.OnChanged = () => RebuildCardsCells(holder);
            PointerLayer.RegisterScroll(CardsScroll);
            RebuildCardsCells(holder);

            // 左侧筛选栏（**在卡池之后建** ⇒ 兄弟序在原版里也是它靠后 = 画在卡池之上）
            // 起手收起（原版靠 `hiddenPosition` 滑出去 —— 🆕 A11 起我们也真的滑，见 `ApplyDrawerSlide`）
            _fltCards = BuildFilterPanel(page, CardsState, RefreshCardsAfterFilter, false);

            // `Empty Collection Warning`：**原版三页都有**，出厂 `act=F`，
            // 运行期条件 = **过滤后为空**（`CollectionDisplay.RefreshCollection`：`filteredCollection.Count <= 0` ⇒ `SetActive(true)`）。
            // 坐标 = **135.22,70.94 → 1970.01,1080**（**比父还宽、左右都溢出** —— 原版就这样，别"修正"）。
            // ✅ 2026-09-24 补建（Styles 页同日已建过同一条；这是 §三 第 15 条 第 50 行的下半场）。
            {
                var ew = Node(page, "Empty Collection Warning", new PxRect(135.22f, 70.94f, 1970.01f, 1080f));
                var wt = Text(ew, "There are no cards in your collection for the selected filters",
                              135.22f, 1970.01f, 70.94f, 1080f, 5, PageInk, "Warning", 36f);
                if (wt != null) wt.SetRenderQueue(QPageText);
                _cardsEmpty = ew;
            }
            RefreshCardsEmpty();
        }

        Transform _deckEmpty;      // Deck 页的「一套卡组都没有」
        Transform _cardsEmpty;     // Cards 页
        Transform _cosmoEmpty;     // Cosmetics 页（卡背）
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
            for (int i = page.childCount - 1; i >= 0; i--) DestroySafe(page.GetChild(i).gameObject);
            BuildCardsPage(page);
        }

        void RebuildCardsCells(Transform holder)
        {
            var vp = holder.Find("Viewport");
            var parent = vp != null ? vp : holder;
            for (int i = parent.childCount - 1; i >= 0; i--) DestroySafe(parent.GetChild(i).gameObject);
            CardsCells.Clear();

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
                var v = CardView.Create(parent, BattleDriver.ToCardData(list[i], list[i].Faction), "CollectionCard_" + i);
                if (v == null) continue;
                v.gameObject.SetActive(true);
                // 🔴 **2026-10-08（A181）：卡自己也要吃这道裁切** —— 窗级的裁切只到
                //   `MenuWindowBase` 那几个绘图助手（`Rect`/`Text`/`Nine`/`AddHit`），而这张卡是
                //   `CardView` 的**自建网格**，看不见它 ⇒ 压在视口边上的卡**整张画出去**
                //   （实测滚 192px：第一排卡的上半截画到视口上沿 155.9 以上，压在页头那条空带上）。
                //   原版这一页的 `Viewport` 挂着 `RectMask2D`（`m_Softness=(0,0)`·`m_Padding=(0,0,0,0)`）⇒ 会裁。
                // 📌 走 `SetPose` 定完位姿、**再**让卡去解析裁切边界 —— 顺序不能反：
                //   位姿是「画布矩形 → 卡的局部系」那个换算的输入，先裁再摆 = 按旧位姿裁的一刀。
                //   （这两句 = **一次调用**的等价物：`CardView` 自己就在 `SetPose` 之后立刻重裁。）
                v.SetPose(Local(parent, r.x1, r.y1, r.x2, r.y2), 0f, scale);
                // 🔴 **2026-10-13（A435 辛 · A774）**：裁切边界**沿父链解析**（就是上面那颗
                //   `holder/Viewport` 上的 `ViewportClip`）—— 原来是把 `CardsViewport` 这个**常量矩形**
                //   当 `SetPose` 的第 4 个实参传进去 ⇒ **第二份状态源**（节点上那份框/`padding` 改了，卡不跟）。
                //   ⛔ **别改成传 `null`**：`SetPose(…, null)` 的语义是「**不改**当前状态」，而这张卡出生时
                //   `_clip` 本来就是 `null` ⇒ 那样**整卡一张都不裁**（静默，正是 A181 当初那个缺陷）。
                //   ⚠️ 代价：`SetData` 会**再重裁一遍**（见 `CardView.ApplyClip` 的注释）—— 正确性优先。
                v.SetClipFromTree();
                v.SetData(BattleDriver.ToCardData(list[i], list[i].Faction));
                v.SetFace(CardFace.Full);
                v.SetHighlight(CardHighlightState.Normal);
                CardsCells.Add(v.transform);
                // 🆕 2026-10-17（B10 · 账上「卡重复与升级 · 三件 UI ①」）：格子**底下那条「张数」**
                //    （原版 `Collection Card/Content/Counter`）—— 判据 / 文案 / 几何全在 `BuildCardsCounter` 那一段。
                BuildCardsCounter(parent, i, r, list[i]);
                // ⚠️ 闭包**别捕循环变量 `i`** —— 点击发生在重建之后，那时 `i` 已经是 `list.Count`（越界）
                var def = list[i];
                AddHit(parent, "CardHit_" + i, r, QPageRow, () => OpenCardDetail(def));
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
        //   · 一格 = `Collection Cosmetic` 250×405，**只有两层图**：`Cardback`(铺满) + `Cardback Shadow SDF`
        //     （后者 `sprite=0` 运行时喂）—— **没有卡名/费用/文字**
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
            var title = Text(page, "Your cosmetics collection", 1200f, 1821.01f, 80.94f, 140.94f, 5, PageInk,
                             "Header Label", 38f);
            if (title != null) { title.SetRenderQueue(QPageText); title.AlignRightOn(LayoutSpace.FromPixel(1821.01f, 0f).x); }

            // ---- 卡背网格 ----
            var holder = Node(page, "Scroll View", CosmoView);
            // 🔴 **2026-10-13（A435 阶段 2 · 乙 · A16）**：裁切状态长在这颗视口节点上
            //   （原版这一页的 `Viewport` 挂着 `RectMask2D`：`m_Softness=(0,0)`·`m_Padding=(0,0,0,0)`，
            //    实读 → `资料/普查产出_1008/波C2_A181_A212收藏窗_A214一.md`）。
            var cosmoVp = ViewportClip.Hang(holder, "Viewport", CosmoView, Vector4.zero, Vector2Int.zero);
            int n = CosmoTotal;
            int rows = Mathf.Max(1, Mathf.CeilToInt(n / (float)CosmoCols));
            CosmoScroll = MenuScroll.TopAligned(CosmoView, rows * CosmoCellH);
            CosmoScroll.Owner = gameObject;
            CosmoScroll.ClipNode = cosmoVp;             // 🔴 A465（同卡池页那一条）
            CosmoScroll.OnChanged = () => RebuildCosmoCells(holder);
            PointerLayer.RegisterScroll(CosmoScroll);
            RebuildCosmoCells(holder);

            // `Empty Collection Warning`：**本页那一份的矩形与别页不同** —— 直读 = **170.44,70.94 → 1970.00,1080**
            // （1799.56×1009.06；出处同 Deck 页那条注释）。判据与别页同一条：**过滤后为空**。
            // 🆕 2026-10-03（A11）：抽屉那两行建好了 ⇒ 这条判据现在**真的能触发**（13 阵营各 9~20 张 ⇒ 正常筛不空）。
            {
                var ew = Node(page, "Empty Collection Warning", new PxRect(170.44f, 70.94f, 1970.00f, 1080f));
                var wt = Text(ew, "There are no cardbacks in your collection for the selected filters",
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
                        //    （没被裁到时两者同值 ⇒ 行为逐字不变）。
                        //    🔴 **2026-10-13（A435 阶段 2 · 乙 · A16）**：`clip` 形参从 `CosmoView` 改成 **`null`** ——
                        //    `CosmoView` 现在长在上面那颗 `ViewportClip` 节点上，显式传它 = 形参永远非空 ⇒
                        //    `Resolve` 第 1 支（节点**被形参盖住**，且 `NodeShadowedByParam` 会被这个站点**永久污染**）。
                        //    节点框（`ClipPx`）就是 `CosmoView` 反推回来的（差 ~1e-4px）、`padding`/`softness`
                        //    逐字相同 ⇒ **像素级不变**。`clipSoftness` 一并归零（节点态下由节点那份说了算）。
                        var qs = MenuDraw.Rect(cell, sdfTex, sr, "Cardback Shadow SDF", QPageSdf,
                                               null, false, null, default(Vector2));
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
                    var q = MenuDraw.Rect(cell, tex, r, "Cardback", QPageRow, null, false, null, default(Vector2));
                    if (q != null) q.SetRenderQueue(QPageRow);
                }
                else
                {
                    Debug.LogWarning("[Collection] 卡背取不到：" + names[i]);
                }
                AddHit(cell, "Hit", r, QPageRow, () => NotifyNotBuilt("卡背详情/装备（原版点它开哪个窗，普查标了「不确定」）"));
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
            RefreshFilterToggles();                           // 🆕 2026-10-05：页头那颗圆钮按**逻辑态**换图
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
        //   · **风格清单**：本机只覆盖 `AA_HB`(6 张) + `v2`(1 张)，其余风格在远端 CCD 的 `alternateartstyles` 包。
        //     `v2` 的**显示名查不到** ⇒ 直接印 token。
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

        /// <summary>本机能拿到的督军异画（**7 张 · 2 种风格**）。表与出处 = `工具/import_original_art.py` 的 `ALT_ART`。</summary>
        public struct AltArtCard
        {
            public readonly string CardId, Style;
            public AltArtCard(string id, string style) { CardId = id; Style = style; }
        }

        public static readonly AltArtCard[] AltArtCards =
        {
            new AltArtCard("AM5",                 "AA_HB"),
            new AltArtCard("BL1",                 "AA_HB"),
            new AltArtCard("SAU1",                "AA_HB"),
            new AltArtCard("GOF3",                "AA_HB"),
            new AltArtCard("SW1",                 "AA_HB"),
            new AltArtCard("UM_Lieutenant_Titus", "AA_HB"),
            new AltArtCard("DA3",                 "v2"),
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

        /// <summary>某个风格的**显示名**。`AA_HB` = **Hammer and Bolter**（`AlternateArtStyleIconsSO/HammerAndBolter`，
        /// 出处 `资料/索引与盘点/解包资源使用地图.md:1174`）；其余**查不到**（风格 SO 在服务端）⇒ 原样印 token。</summary>
        public static string StyleLabel(string style)
        {
            if (style == "AA_HB") return "Hammer and Bolter";
            return style;
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
            // `Art Style Logo`：原版运行时喂图（**风格图标 SO 本地没有**）⇒ 我们画风格名文字（**我们挑的**）
            // 🔴 **必须限宽自适应** —— 那一格是 **512×128**，`Hammer and Bolter` 按 56px 画出来宽 ≈1270px，
            //    **直接压到右箭钮上**（第一版实拍一眼可见）。`SetAutoFitBox` 按框宽缩到放得下为止。
            //    ⚠️ 判据要量**渲染宽度**，不是比字号 —— 见 `CLAUDE.md` §二 的 `AutoFitBox` 教训。
            //    🔴 **2026-10-16（A830）**：那个「渲染宽度」现在由**自检自己量**
            //       （TMP 自己渲出来那块 `textBounds`，`Editor/CollectionScene.cs` 的 `LabelRenderedPx`）——
            //       ⛔ 本窗那个 `StyleLogoWidthPx` 属性（读 `Label.WorldW` = **字段缓存** `_tmpW/_tmpH`，
            //       只有 `RefreshBounds()` 写 = **被测实现自己**）**已删**，别再把它加回来当尺子。
            //       （判据 → `资料/普查产出_1014/RO_缓存口径与输入三件.md` §一·3；同族先例 = W4/W5/W6 三个宿主换口。）
            _styleLogo = Text(page, StyleLabel(CurrentStyleName()), StyleLogoL, StyleLogoR, StyleBarT, StyleBarB,
                              6, PageInk, "Art Style Logo", 56f);
            if (_styleLogo != null)
            {
                _styleLogo.SetRenderQueue(QPageText);
                _styleLogo.SetAutoFitBox(LayoutSpace.Px(StyleLogoR - StyleLogoL),
                                         LayoutSpace.Px(StyleBarB - StyleBarT), 18f, 56f);
            }
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
            StyleScroll = MenuScroll.TopAligned(StyleView, rows * StyleCellH);
            StyleScroll.Owner = gameObject;
            StyleScroll.ClipNode = styleVp;             // 🔴 A465（同卡池页那一条）
            StyleScroll.OnChanged = () => RebuildStyleCells(holder);
            PointerLayer.RegisterScroll(StyleScroll);
            RebuildStyleCells(holder);

            // `Empty Collection Warning`：原版 `act=F`，运行期条件 = **过滤后为空**
            // （`CollectionDisplay.RefreshCollection`：`filteredCollection.Count <= 0 ⇒ SetActive`）
            {
                var ew = Node(page, "Empty Collection Warning", StyleView);
                var wt = Text(ew, "There are no cards in your collection for the selected filters",
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
            var node = Node(page, "Select Art Button " + side, r);
            Rect(node, "UI_Button_Round_background", r, "Background Round", QPagePanel, null, true);
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
            AddHit(node, "ArrowHit " + side, r, QPageRow, onClick, arrowBg, "40k_general_bt_yellow");
        }

        /// <summary>切风格（`ChangeStyleButton(bool isLeft)`：**isLeft=true = 上一个(−1)**，越界回绕）。</summary>
        public void ChangeStyle(int dir)
        {
            var s = AltStyles;
            if (s.Length == 0) return;
            StyleIndex = ((StyleIndex + dir) % s.Length + s.Length) % s.Length;
            Debug.Log("[Collection] 切风格 → 「" + StyleLabel(s[StyleIndex]) + "」（第 " + (StyleIndex + 1) + "/" + s.Length + " 种）");
            RebuildStylesPage();
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

                var d = BattleDriver.ToCardData(card, card.Faction);
                // 🔴 **立绘换成异画**（`CardData.artOverride`）—— 卡框/数值/名字照旧用原卡
                d.artOverride = CardArt.AltArt(a.CardId);
                if (d.artOverride == null)
                    Debug.LogWarning("[Collection] 异画立绘取不到（卡面会退回普通立绘）：" + a.CardId);

                var v = CardView.Create(parent, d, "CollectionAltArt_" + a.CardId);
                if (v == null) continue;
                v.gameObject.SetActive(true);
                // 🔴 **2026-10-08（A181）**：异画页同样要给卡带上裁切 —— 判据与卡池页那条逐字相同
                //   （原版 `Alternate Art Tab/Collection Display/Scroll View/Viewport` 的 `RectMask2D`
                //   `m_Softness=(0,0)`·`m_Padding=(0,0,0,0)`；视口 = 330.22,287.67 → 1920.01,1080）。
                //   🔴 **2026-10-13（A435 辛 · A774）**：边界改成**沿父链解析**（= 上面那颗
                //   `holder/Viewport` 上的 `ViewportClip`），原来传的是 `StyleView` 那个常量矩形
                //   ⇒ 第二份状态源。顺序与原由同卡池页那一处（`SetPose` 定完位姿再解析）。
                v.SetPose(Local(parent, r.x1, r.y1, r.x2, r.y2), 0f, scale);
                v.SetClipFromTree();
                v.SetData(d);
                v.SetFace(CardFace.Full);
                v.SetHighlight(CardHighlightState.Normal);
                StyleCells.Add(v.transform);
                AddHit(parent, "AltArtHit_" + a.CardId, r, QPageRow, () => OpenCardDetail(card));
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
                _fltScroll = MenuScroll.TopAligned(FltView, FilterPanelModel.ContentHFor(_flt != null ? _flt.State : null));
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
            RefreshFilterToggles();                    // 🆕 2026-10-05：页头那颗圆钮按**逻辑态**换图
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
                    AddHit(cell, "Hit", r, QFltHit, () => Scope(owner, () => ApplyFilter(key)));
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
            var lb = Text(parent, text, r.x1, r.x2, r.y1, r.y2, 5, col, name, fontPx);
            if (lb == null) return;
            lb.SetRenderQueue(QFltText);
            // 🔴 A333：上限取**原版 `m_fontSizeMax`**（`autoMaxPx <= 0` 才退回 `fontPx` = 旧行为）；
            //    A336④：base 取**原版 `m_fontSizeBase`**（`basePx <= 0` 才退回调用方那一档）。
            if (autoMinPx > 0f) lb.SetAutoFitBox(LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), autoMinPx,
                                                 autoMaxPx > 0f ? autoMaxPx : fontPx, basePx);
            // 🔴 **2026-10-08（A212）**：折行按**原版逐处实读的那一档**显式设 ——
            //    与 `Deck/DeckRuntime.cs:2982` / `:3110` 那两行**同一条判据**（那扇窗已经收口过），
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

            // 字：`Text Area` [37.4,182.4,231.3,27]（绝对）→ 面板内 (37.15, 26.5)~(268.45, 53.5)；空时是占位符 "Search"
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
            AddHit(cell, "Hit", r, QFltHit, () => Scope(owner, () => ApplyFilter("$name")));
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
            var lb = Text(parent, text, r.x1, r.x2, r.y1, r.y2, 5, PageInk, "Title " + text, fontPx);
            if (lb == null) return;
            lb.SetRenderQueue(QFltText);
            if (!left) return;
            MenuDraw.AlignLeft(lb, r);
            // 🔴 **2026-10-13（A435 阶段 2 · 乙 · B2）**：这一行的守卫原来是 `if (Clip.HasValue)`
            //   （吃**本窗**那个字段）—— 迁移后本窗 `Clip` **恒为 `null`** ⇒ 那个守卫会让这一句
            //   **永远不跑**（静默：压在视口边上的标题行会停在 `AlignLeft` 之前那一刀的位置上）。
            //   ⇒ 改成先走共用解析（第二参数仍传本窗 `Clip` = 旧路那一份，非空时照旧赢）。
            //   ⛔ **`ClipText` 的 `clip` 形参必须传 `null`**（别把解析结果 `st.RenderClip` 喂进去）：
            //   那会让 `MenuDraw` 里那个 `ClippedTextGuard` 存一份**解析后的快照**、并在每次重裁时
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
            Build();
            if (tabButtons != null) tabButtons.Click(0);       // 默认落在第一页（`GetStartingTab` 的回落）
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
        //    ⚠️ **字号逐页不同**（原版每页是自己的实例值）：Deck 42/33 · Cards 42/42 · Cosmetics 35/33 · Styles 42/42
        //       ⇒ **做成参数**，不许当成全局常量（`阶段二_卡组线_原版规格.md` §三 那句「别拿一个数当全部情况」）。
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

        /// <summary>建一条**共用页头**：`Filters` 圆钮 + `icon detail` + `label` + 分隔线 + `Clear filters`。
        /// <paramref name="filterPx"/> / <paramref name="clearPx"/> = **本页自己的**字号（原版逐页不同）。
        /// <paramref name="pageNo"/> = **窗口页号**（0=Deck · 1=Cards · 2=Cosmetics · 3=Styles）——
        /// 存下那颗圆钮的 quad，好让开合时按状态换图（见 <see cref="_hdrFltBtn"/>）。
        /// 🔴 **Deck 页也走这一条**（2026-10-05 订正）：原版 `Select Deck Tab` 底下**同样有**
        /// `Header/Filter Toggle`（`menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 12`
        /// 印出来的 `4 Header 167.2,70.9→1920,155.9` → `5 Filter Toggle 367.2,88.4 50×50 40k_menu_bt`
        /// + `label 'Filters' fs42` + `icon detail 40k_bt_icon_search 30×30`，**与另外三页同值**）——
        /// 我们原来给 Deck 页单独写的 `BuildDeckHeader` **连那颗 `40k_menu_bt` 都没建**（只铺了 50×50 的图标）。
        /// 四页的差别只有：① 字号（Deck 42/33）② Deck 多一条 `Control Buttons`（见 `BuildDeckHeader`）。</summary>
        public void BuildFilterHeader(Transform page, int pageNo, float filterPx, float clearPx, System.Action onClear,
                                      System.Action onToggle)
        {
            var btnQ = Rect(page, "40k_menu_bt", new PxRect(FltBtnX, FltBtnY, FltBtnX + FltBtnS, FltBtnY + FltBtnS),
                 "Filters Button", QPagePanel);
            if (pageNo >= 0 && pageNo < _hdrFltBtn.Length) _hdrFltBtn[pageNo] = btnQ;
            Rect(page, "40k_bt_icon_search", new PxRect(FltBtnX + 10f, FltBtnY + 10f, FltBtnX + 40f, FltBtnY + 40f),
                 "Filters Icon", QPageRow, null, true);
            var fltLab = Text(page, "Filters", 437.2f, 587.2f, FltBtnY, FltBtnY + FltBtnS, 5, PageInk,
                              "Filters Label", filterPx);
            if (fltLab != null) fltLab.SetRenderQueue(QPageText);
            if (onToggle != null)
                AddHit(page, "FiltersHit", new PxRect(FltBtnX, FltBtnY, FltBtnX + FltBtnS + 220f, FltBtnY + FltBtnS),
                       QPageRow, onToggle);

            Rect(page, "40k_main_line", new PxRect(167.17f, HdrSepT, 1920.01f, HdrSepB), "Separator Line", QPageRow);

            var clrQ2 = Rect(page, "UI_Button_Mulligan", new PxRect(ClearFltX, ClearFltY, ClearFltX + ClearFltW, ClearFltY + ClearFltH),
                 "Clear filters", QPageRow);
            var clrLab = Text(page, "Clear filters", ClearFltX, ClearFltX + ClearFltW, ClearFltY, ClearFltY + ClearFltH,
                              5, PageInk, "Clear filters Text", clearPx);
            if (clrLab != null) clrLab.SetRenderQueue(QPageText);
            if (onClear != null)
                // A17：原版 `…>Clear Filter Button` 是 SpriteSwap（普查 §块 3 第 4 行；我们 1 颗盖原版 3 颗）
                AddHit(page, "ClearFiltersHit",
                       new PxRect(ClearFltX, ClearFltY, ClearFltX + ClearFltW, ClearFltY + ClearFltH), QPageRow, onClear,
                       clrQ2, "UI_Button_Mulligan");

            // 建的时候抽屉大多还没建（`_fltCards` 在 `BuildCardsPage` 更靠后那几行才赋值）
            // ⇒ 这一下只把「出厂态」摆正；真正的兜底在 `Build()` 尾巴那次 `RefreshFilterToggles()`。
            RefreshFilterToggle(pageNo);
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
        public Transform PageRoot(int p) { return (p >= 0 && p < _pages.Length) ? _pages[p] : null; }

        /// <summary>四页页头那颗 `Filter Toggle` 的图（下标 = **窗口页号**：0=Deck · 1=Cards · 2=Cosmetics · 3=Styles）。
        /// 原版四页**各有一颗**（`Collection Menu Variant` 逐页实读，见 `BuildFilterHeader` 那段），
        /// 且都是 `EverguildToggle`：**`changeSpriteOnValueChange = 1`** · `m_IsOn = 0` ·
        /// `offSprite = 40k_menu_bt`（4472012397149938974）· `onSprite = 40k_menu_bt_pressed`（4570862220269996290）
        /// ⇒ **抽屉开着时换按下图**。
        /// 🔴 2026-10-05 之前：这颗 quad 的返回值**被丢弃**（没有引用）、三条开合路径一处都不碰它
        /// ⇒ 「开着也不换图」（与 `DeckRuntime` 那颗 `hdr_fltbtn` 当年同一个毛病，那边 A76② 已修）。</summary>
        readonly ImageQuad[] _hdrFltBtn = new ImageQuad[4];

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
            var res = BuildShell(transform, Buttons, "CollectionTabButton_", "Tabs");
            tabButtons = res.buttons;
            _btnHighlight = res.highlight;
            _btnRes = res;

            // 🔴 `visualTypes` 在基类里带一个**奖励窗的默认表** ⇒ 本窗必须**整表替换**
            if (visualTypes != null)
            {
                visualTypes.Clear();
                visualTypes.Add(WindowTabType.CollectionDecks);
                visualTypes.Add(WindowTabType.CollectionCards);
                visualTypes.Add(WindowTabType.CollectionCosmetics);
                visualTypes.Add(WindowTabType.CollectionStyles);
            }

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
                var cl = Text(cq != null ? cq.transform : shared, "Back", bt.x1, bt.x2, bt.y1, bt.y2,
                              5, Color.white, "Button Text", 40f);
                if (cl != null)
                {
                    cl.SetRenderQueue(QText);
                    // 🔴 **2026-10-11（A305①）**：第 5 个实参 = 原版 `Button Text` 的 `m_fontSizeBase` **原文**。
                    //    判据（原版实读）：`/Collection Menu Variant/…/Shared/Close Button/Button Text`
                    //    = `m_fontSize 40` · `auto[10~40]` · **`base 12.0`**（逐站表 §二·3 #22）
                    //    —— 同族（按钮文案那一族）`Back` / `WebShop Button` / `Continue` 也都是 **12**。
                    cl.SetAutoFitBox(LayoutSpace.Px(bt.W), LayoutSpace.Px(bt.H), 10f, 40f, 12f);
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
                AddHit(transform, "CloseHit", cr, QPanel, () => Close(), cq, "UI_Button_Mulligan");
            }

            // 🔴 **本窗的底图必须压到所有页内容之下**（2026-09-23 实拍抓的）：
            //    `CardView`（卡池用的那个外来组件）**所有层都落在默认队列 3000**
            //    （全工程只给 SDF 那层显式设过 3000，其余靠材质默认）—— 而基类给 `Background` 的是
            //    `QPanel = 3005` ⇒ **整片卡池被窗口底图盖住**：截图里一片空，而**断言全绿**
            //    （断言量的是节点位置与尺寸，量不到「谁盖谁」）。⇒ 把这张底图压到 2000：
            //    页内容（3000+）与左栏（3005）都在它上面；它下面没有别的东西（它本来就是最底层）。
            // ⚠️ 底图在 `Content Area/Background`（不是窗口根的直接子节点 —— 正本 §二）
            var bgNode = transform.Find("Content Area/Background");
            if (bgNode != null)
                foreach (var q in bgNode.GetComponentsInChildren<ImageQuad>(true)) q.SetRenderQueue(2000);

            foreach (var t in tabs) t.Setup();

            // 🔴 **四页的 `Filter Toggle` 收尾刷一次**（2026-10-05）：四份抽屉是在上面那句 `Setup()` 里
            //   才陆续建起来的（页头比抽屉先建）⇒ `BuildFilterHeader` 末尾那次 `RefreshFilterToggle(pageNo)`
            //   读到的 `PageDrawer(page)` 要么是 **null**（首建）、要么是**上一轮那一份**（重建 ——
            //   `_fltCards`/`_fltStyles`/… 还指着旧对象）⇒ 少了这一下，**重建**时页头那颗钮会停在
            //   上一轮那张图上（与抽屉此刻的状态不符）。⛔ 它是**兜底**，别删。
            // ⚠️ **2026-10-05 更正（铁律 5）**：原文写「异画页那份**出厂就是展开的** ⇒ 少了这一下，
            //   它起手会画成常态图」—— **那个前提是错的**（四页起手一律收起，见 `BuildStylesPage` 那段更正）。
            //   ⛔ 但这一行照旧要留：它的作用在**重建**那条路上，不在首建。
            RefreshFilterToggles();
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
            // 本页字号 = **42 / 33**（原版逐页不同）
            BuildFilterHeader(page, 0, 42f, 33f, ClearDeckFilters, ToggleDeckFilters);

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
            var ctrl = Node(page, "Control Buttons", new PxRect(CtrlGrpL, CtrlGrpT, CtrlGrpR, CtrlGrpB));

            // 🔴 **2026-10-03 就地更正（A22①）**：常态图原来画的是 `40K_button` —— **原版是 `UI_Button_Mulligan`**
            //（判据 = `资料/普查产出_1003/按钮悬停图_普查.md` §块 3 第 1、2 行：
            //  `Select Deck Tab>Header>Control Buttons>{Create,Import}`，且常态图与高亮图逐颗列了）
            var newQ = Rect(ctrl, "UI_Button_Mulligan", new PxRect(CreateX, HdrBtnY, CreateX + HdrBtnW, HdrBtnY + HdrBtnH), "Create", QPageRow);
            var impQ = Rect(ctrl, "UI_Button_Mulligan", new PxRect(ImportX, HdrBtnY, ImportX + HdrBtnW, HdrBtnY + HdrBtnH), "Import", QPageRow);
            // ⚠️ 文案与字号照 A2 §三·4：`Import` 那条的字是 **"Import Deck"**、`Create` 是 **"Create Deck"**，都是 **fs42**(auto 10-42)
            // 🆕 A98：字挂在**按钮自己**底下（原版 = `Create>Button Text` / `Import>Button Text`）。
            //    ⚠️ **名字不照原版那个 `Button Text`**：两颗同名 ⇒ `FindChild`（按名字取**第一个**）会取错那一颗，
            //       故沿用我们自己的「`Create Text` / `Import Text`」。⚠️ 图缺了就把字退回挂在组节点上（不许静默丢字）。
            var newLab = Text(newQ != null ? newQ.transform : ctrl, "Create Deck", CreateX, CreateX + HdrBtnW,
                              HdrBtnY, HdrBtnY + HdrBtnH, 5, PageInk, "Create Text", 42f);
            if (newLab != null) newLab.SetRenderQueue(QPageText);
            var impLab = Text(impQ != null ? impQ.transform : ctrl, "Import Deck", ImportX, ImportX + HdrBtnW,
                              HdrBtnY, HdrBtnY + HdrBtnH, 5, PageInk, "Import Text", 42f);
            if (impLab != null) impLab.SetRenderQueue(QPageText);
            BuildUnlockPlaceholder(ctrl);           // 组内**最左**那一格（原版画不出东西，只建结构）

            // 🆕 A17：这三颗原版都是 SpriteSwap，高亮图都 = `<常态图>_hover`（普查 §块 3 第 1～3 行）
            AddHit(ctrl, "ImportHit", new PxRect(ImportX, HdrBtnY, ImportX + HdrBtnW, HdrBtnY + HdrBtnH), QPageRow,
                   () => OpenImportPopup(), impQ, "UI_Button_Mulligan");
            AddHit(ctrl, "CreateHit", new PxRect(CreateX, HdrBtnY, CreateX + HdrBtnW, HdrBtnY + HdrBtnH), QPageRow,
                   () => CreateDeck(), newQ, "UI_Button_Mulligan");
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
            var holder = Node(page, "Deck Scroll View", DeckViewport);
            // 🔴 **2026-10-13（A435 阶段 2 · 乙 · A19）**：裁切状态长在这颗视口节点上
            //   （原版这一页的 `Viewport` 挂着 `RectMask2D`：`m_Softness=(0,0)`·`m_Padding=(0,0,0,0)`
            //    —— 与 `RebuildDeckCells` 里原来那一对 `Clip = DeckViewport; … Clip = prevClip;` 逐字同值）。
            var deckVp = ViewportClip.Hang(holder, "Viewport", DeckViewport, Vector4.zero, Vector2Int.zero);
            int n = CollectionData.DeckCount();
            int rows = Mathf.Max(1, Mathf.CeilToInt(n / (float)DeckCols));
            float contentH = rows * (DeckCellH + DeckSpacingX) - DeckSpacingX;
            DeckScroll = MenuScroll.TopAligned(DeckViewport, contentH);
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
            {
                var ew = Node(page, "Empty Collection Warning", new PxRect(165.88f, 70.94f, 1970.01f, 1080f));
                var wt = Text(ew, "There are no decks in your collection", 165.88f, 1970.01f, 70.94f, 1080f, 5, PageInk,
                              "Warning", 36f);
                if (wt != null) wt.SetRenderQueue(QPageText);
                _deckEmpty = ew;
            }
            RefreshDeckEmpty();
            BuildDeckFilterDrawer(page);
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
                                  DInputT + 6.5f, DInputT + DInputH - 6.5f, 5, PageInk, "Input Text", 30f);
            if (_deckFltNameTx != null) _deckFltNameTx.SetRenderQueue(QFltText);
            Rect(nameRow, "40k_icon_search", new PxRect(268.45f, 180.94f, 303.45f, 210.94f), "Search Icon",
                 QFltRow, null, true);
            AddHit(nameRow, "Hit", ir, QFltHit, BeginDeckNameFilter);

            // ---- ② `Army Filter`：Title + 13 格（3 格一行）----
            var army = Node(filters, "Army Filter", new PxRect(DfltL, DArmyT, DfltR, DArmyT + DArmyH));
            var ttl = Text(army, "Army", DTitleL, DTitleL + DTitleW, DTitleT, DTitleT + DTitleH, 5, PageInk,
                           "Title", 32f);
            if (ttl != null) ttl.SetRenderQueue(QFltText);
            var armies = CampaignData.Armies;
            for (int i = 0; i < armies.Length; i++)
            {
                string fac = armies[i];
                float x = DCellPad + (i % 3) * (DCellS + DCellGap);
                float y = DCellT + (i / 3) * DCellS;
                var cr = new PxRect(x, y, x + DCellS, y + DCellS);
                var cell = Node(army, "Cell_fac_" + fac, cr);
                Rect(cell, DeckRuntime.FactionIcon(fac), cr, "Icon", QFltRow,
                     ToggleTint(fac == _deckFacFilter), true);
                AddHit(cell, "Hit", cr, QFltHit, () => ToggleDeckFacFilter(fac));
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
            return string.IsNullOrEmpty(_deckNameFilter) ? "Search" : _deckNameFilter;
        }

        /// <summary>`Filters` 圆钮 / 自检：开合 Deck 页的左抽屉（**原版那颗钮开的就是它**）。</summary>
        public void ToggleDeckFilters()
        {
            if (_deckFltSlide == null || _deckFltPanel == null) return;
            bool on = !_deckFltSlide.Open;                 // 🆕 A11：与另外三页同一套（逻辑态 + 滑动）
            _deckFltSlide.Open = on;
            StartDrawerSlide(_deckFltSlide);
            if (on && _deckFltNameTx != null) _deckFltNameTx.SetText(DeckNameFilterText());
            RefreshFilterToggles();                        // 🆕 2026-10-05：页头那颗圆钮按**逻辑态**换图
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
            var armies = CampaignData.Armies;
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
            float contentH = rows * (DeckCellH + DeckSpacingX) - DeckSpacingX;
            if (DeckScroll != null)
            {
                // ⚠️ `MenuScroll` 没有「改内容尺寸」的 API ⇒ 直接写它那两个**公开字段**
                //    （纵向时 `ContentX1/X2` 装的是上下两端，见那个字段的注释），再夹一次偏移
                DeckScroll.ContentX2 = DeckScroll.Viewport.y1 + contentH;
                DeckScroll.SetOffset(DeckScroll.Offset);
            }
            var page = PageRoot(0);
            var holder = page != null ? page.Find("Deck Scroll View") : null;
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
                if (ch.name == "Viewport") continue;             // 视口节点留着
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
            return MenuDraw.DeckCell(this, parent, "CollectionDeck_" + i, r, CollectionData.DeckAt(i),
                                     i == CollectionData.CurrentIndex(),
                                     QPageRow, QPageText, QPageOverlay, QPageRow,
                                     () => SelectDeck(idx));
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
                var holder = pr != null ? pr.Find("Deck Scroll View") : null;
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
            var holder = pr != null ? pr.Find("Deck Scroll View") : null;
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

        /// <summary>进编辑。⚠️ **这是相对原版的一处偏离**：原版在同一扇窗里换页；我们的编辑器早已建成**独立场景**
        ///    （200 条自检 + 自带的坐标层），嵌进窗里要重写它的 `Pos/ToPx` ⇒ 先用「切场景」把闭环打通。
        ///    ⚠️ **批处理下不切场景**（自检要靠同一个进程跑完）⇒ 自检验的就是「交接下标对不对」。</summary>
        public void EditDeck(int i) { GoEdit(i); }

        /// <summary>「进编辑」的**唯一实现** —— 收藏窗那条路与 `Deck info Popup` 的 `Edit Deck` 钮**都走它**
        /// （CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。</summary>
        public static void GoEdit(int i)
        {
            CollectionData.PendingEditDeck = i;
            // 🆕 2026-10-18（A855）：**这条来路的回程意图** —— 「按来路回」（用户 2026-10-18 拍板）：
            //   全仓仅有两处 `LoadScene("DeckEditor")`，这是**第一处** ⇒ 来源写在这里、
            //   ⛔ **不由离场方式决定**（`DeckRuntime.BackToMenu` 那三处调用点都只是「关闭这条路上的编辑器」）。
            //   ⚠️ 写在批处理闸**之前**：批处理不切场景，自检只能靠这个意图观测（同上面那个下标）。
            CollectionData.SetReturnIntent(DeckExitSource.Collection, WindowTabType.CollectionDecks);
            Debug.Log("[Collection] 进编辑：「" + CollectionData.DeckAt(i).Name + "」"
                      + "（把下标 " + i + " 交给 `DeckRuntime`；返回时回主菜单场景 → 在那里重开**收藏窗的卡组页**）");
            if (Application.isBatchMode) { Debug.Log("[Collection] （批处理：不切场景，只交接）"); return; }
            UnityEngine.SceneManagement.SceneManager.LoadScene("DeckEditor");
        }

        /// <summary>点 `New Deck` 那颗钮：建一套空卡组 + 重建卡组列表。
        /// 🔴 **2026-10-14（A549①）**：`CollectionData.CreateDeck` 自 A503 起**失败时返回空串**
        /// ⇒ 原来那句日志会打成「新建卡组**「」**」—— 既认不出是谁、也看不出「没写进存档」。
        /// ⚠️ **功能本来就没错**（那一套确实在内存里、列表也确实该重建 ⇒ 上面那次重建**照旧无条件跑**）
        /// ⇒ 本件**只把那一支的措辞改对**：⛔ 不改流程、不加阻拦（同 A609 的裁定）。</summary>
        public void CreateDeck()
        {
            string name = CollectionData.CreateDeck();
            var pr = PageRoot(0);
            var holder = pr != null ? pr.Find("Deck Scroll View") : null;
            if (holder != null) RebuildDeckCells(holder);
            if (string.IsNullOrEmpty(name))
                Debug.LogWarning("[Collection] 新建卡组**没写进存档**（内存里那套还在、列表已重建；重启就没了"
                                 + " —— 原因见上一条 `[CollectionData]` 警告，⛔ 别在这里另写一套原因）");
            else
                Debug.Log("[Collection] 新建卡组「" + name + "」（原版走 `Deck Editing Menu`，本轮只建卡组）");
        }

        public string Dump()
        {
            return "Collection：页 " + CurrentTab + " · 卡组 " + CollectionData.DeckCount() + " 套"
                   + " · 当前第 " + (CollectionData.CurrentIndex() + 1) + " 套"
                   + " · 列表滚动 " + (DeckScroll != null ? DeckScroll.Offset.ToString("F0") : "-") + "px"
                   + " · 画出的格 " + DeckCells.Count;
        }
    }

    /// <summary>收藏窗的一页（四页共用这一个组件，按 `_page` 分）。</summary>
    public class CollectionTabPage : WindowTabBase
    {
        CollectionWindow _win;
        Transform _root;
        int _page;

        public override WindowTabType Type
        {
            get
            {
                switch (_page)
                {
                    case 0: return WindowTabType.CollectionDecks;
                    case 1: return WindowTabType.CollectionCards;
                    case 2: return WindowTabType.CollectionCosmetics;
                    default: return WindowTabType.CollectionStyles;
                }
            }
        }

        public void SetHost(CollectionWindow win, Transform root, int page)
        {
            _win = win; _root = root; _page = page;
        }

        public override void Setup()
        {
            if (_win == null || _root == null) return;
            switch (_page)
            {
                case 0:
                    _win.BuildDeckHeader(_root);
                    _win.BuildDeckList(_root);
                    break;
                case 1:
                    _win.BuildCardsPage(_root);
                    break;
                case 2:
                    _win.BuildCosmeticsPage(_root);        // 233 张卡背 2026-09-23 已导 ⇒ 可建了
                    break;
                default:
                    _win.BuildStylesPage(_root);           // 异画页：本地 7 张督军异画（2 种风格）
                    break;
            }
        }
    }
}
