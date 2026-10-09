// DeckSelectionPopup.cs — 卡组线最后一个弹窗：**`Deck Selection Popup with Tabs`**（原版 `DeckSelectionPopup`）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0923/A1_外壳与弹窗.md` **§3**（逐节点表 + 根脚本字段）。
// 🔴 **2026-09-24 复核过坐标系**：这扇窗的根是**整屏 popup**（父链上没有 `Content Area`）
//    ⇒ A1 §3 那批坐标**是绝对的、可以直接施工**（不像卡背/异画两页被父链平移过；坑见 `资料/已知的坑.md`）。
//    实读命令：`py 工具/menu_rect.py bundle_menus_assets_all -3585457330712889985 --depth 3`。
//
// 窗口参数（A1 §3 表头）：`type=1 Popup` · `windowsPlacement=15 Popup` · `closeOnESC=1` · `autoInitialize=0`。
//
// ============================ 结构（照 A1 §3 抄）============================
//   · `Menu Dark Background` sprite=0 col **(0,0,0,.773)**（rect 比屏幕大，为了盖住任何画幅）→ **点它关窗**
//   · `Alliance Header Buttons` 167.50,35.07→1756.50,107.25 → **`Tab buttons`**（HLG spacing **12.45** align MiddleLeft）
//     → 两个页签 **260×67.6421**：`Generic Tab UI Button`（`40K_tab_button`，**出厂 m_IsOn=1**）
//     与 `Generic Tab UI Button 1`（`40K_tab_button_overwindow`，出厂 0）
//   · `Generic Window Red Background Big` = **`UI_Deck_Information_Back`** Sliced（134.50,82→1839.50,1032）
//   · `Deck Display` 144.50,124.95→1809.50,973.19
//     ├ `Header` 184.50,124.95→1769.50,209.95（`Separator Line` 在 **204.95→214.95**）
//     │  ├ `Instructions`(act=**N**, 618.72→1335.28) · `Instructions 2`(act=**Y**, 1236.12→1770.24)
//     │  ├ `Practice buttons`（HLG spacing 24.5 align MiddleLeft）→ `Generic Simplified UI Button`
//     │  │   （`UI_Button_Mulligan`，作者 **212.24×63.92**）—— 原版是**「随机挑一套」**（术语 `MenuDeck/Button/Random`）
//     │  ├ `Filter Buttons`(act=**N**) · `Collection Menu Input Field`(act=**N**, 727.00,142.91→1227.00,191.98)
//     └ `Collection Display` 144.50,208.63→1809.50,973.19 → **`Deck Scroll View`** 194.50,208.63→1759.50,986.69
//       （**1565 × 778.06**）· `RecyclableScrollRect`：`_cellWidth/_cellHeight` **225/364.5** · `_spacingX` **20**
//       · `_controlSegmentSize=1` ⇒ **列数按宽度算** = `floor(1565 ÷ (225+20))` = **6** · 原型格
//       = **`Collection Deck With Highlight`**（`useSelectedHighlight=1`，作者 250×405）
//   · `Generic Close Button Orange` 1783.11,62.90→1857.49,138.50（圆底 + `40k_general_bt_yellow_close` 56.86×58.13）
//
// ============================ 格子的画法 ============================
// 🔴 **不重写** —— 走 `MenuDraw.DeckCell`（2026-09-24 收口）：原版 `Collection Deck`（收藏窗 Deck 页）
//    与 `Collection Deck With Highlight`（本窗）**是同一份 prefab 几何的两个变体**
//    （逐字段 diff 过，唯一差别是根组件的 `useSelectedHighlight`）。本窗传 `selected:true` 画金框。
//
// ============================ 入口（我们这条链）============================
// 原版有**四个**调用点，**全在别处**（`RankedDeckSelector.OnSelectDeckButtonClick` ·
// `RankedEventWindow.ChangeDeckButtonClick` · `DeckGeneralInfoDemo.ChangePlayerDeckButton` ·
// **`DeckInfoPopup.SelectPracticeOpponentDeck`**），
// 收藏窗那条链**不走它**（收藏窗点格 = 开 `Deck info Popup`，由那里的 `Edit Deck` 进编辑 —— 已核实）。
//   🔴 **2026-10-03 订正**：这里原来写「**三个**调用点」—— 漏了第 4 个 `DeckInfoPopup.SelectPracticeOpponentDeck`
//      （**练习对手**那条链），判据 = `资料/预组卡组_原版规格.md` §五之二 那张 4 行的表。
//      补齐它的意义不只是数数：**那一个入口选的是【对手】卡组**（其余三个都是「换我自己的卡组」）——
//      同一个窗、同一套数据，**语义由调用方定**（`DeckSelectionContext.Callback`）。
// ✅ **我们接的第一条是原版的第三个**：练习窗 → `Show Deck Content`（翻抽屉）→ **`Change Deck`** ⇒ 开本窗。
// ✅ **2026-10-03 接上第二条 = 原版的第四个**：`Deck info Popup` 点 `Practice Deck` ⇒
//    **这一副当中途的「我的卡组」、本窗选出来的那副当【对手】** ⇒ 回调 `DeckInfoPopup.StartPracticeMatch`
//    （判据与订正 → `Shell/DeckInfoPopup.cs` 文件头那段）。本窗**不用改**：它本来就只负责
//    「列出来 → 选一个 → 把选中那个交给 `OnPicked`」。
//   🆕 **2026-10-17（D18）：还差一行「第 5 条入口」—— 记在这里，⛔ 不建那颗钮。**
//     原版 `Deck info Popup` 的 `Deck Options`（`DeckInfoControls`）上有一颗 **`Select Deck`**
//     （序列化字段 `selectButton` = 8 颗钮里的第 2 颗，偏移 `+0x28`）。
//     它**在本 build 里永不出现**：显示规则是 `context.SelectButton != null`，而
//     `DeckInfoContext` 的 **6 个调用点（`DeckCollectionTab` / `DeckDrawer` / `DeckGeneralInfoDemo` /
//     `RankedEventWindow` / `RankedDeckSelector` / `ChatMessageUI`）全传 `null`**，
//     且 `DeckInfoContext.set_SelectButton` **全库无调用者** ⇒ 实拍也没有这颗钮。
//     逐处实读与偏移表 → `Shell/DeckInfoPopup.cs:66-70` 与 `:109`（**判据只此一份，别抄第二份**）。
//     ⚠️ **它到底连不连本窗，本轮没查到** —— 全量反编译里**只有 `SelectPracticeOpponentDeck` 一处**
//     用了 `DeckInfoPopup.deckSelectionPopup`（`param_1[0x1a]`，grep 全 `decomp_full` 只此一命中），
//     而 `Select Deck` 那颗钮的 onClick 落点**没有反编译产物** ⇒ 如实记「**连线未验**」，
//     ⛔ 不因为它「像」就写成本窗的第 5 条入口。
//
// ============================ 🆕 2026-10-17：本窗这一轮的四处改动（D11 / D12 / D16 / D18）============================
//   · **D11 空态**：原版只有 `Deck Scroll View/Empty Collection Warning` **一件**（视口那一格 + 子 TMP `Warning`，
//     fs 36 · 折行 1 · **无 auto**）⇒ 我们那行自己加的 `Scope Note` **删掉**了（见 `Build()` 第 7/7b 步）。
//   · **D12 页签选中态**：原版 `EverguildToggle` **换底图 + 换染色**（两颗共用 `onSprite=40K_tab_button` /
//     `offSprite=40K_tab_button_overwindow`、`onColor=(1,0.6308285,0,1)` / `offColor=(1,0.5442529,0,1)`），
//     **文字两态都是白的** ⇒ 原来那套「用文字色区分」删掉了（判据全文 → `BuildTab` 的注释）。
//   · **D16 格子的 `Ban Icon` / `Create`**：触发条件**查清了、都不建**（全文 → `RebuildCells` 里那段长注释）。
//   · **D18 `Select Deck` 那颗钮**：记一行、**不建**（判据只此一份在 `Shell/DeckInfoPopup.cs:66-70`）。
//
// ============================ 没建的 / 我们挑的（逐条出声）============================
//   · ✅ **2026-09-26：「预组卡组」那一页接上了**（此前恒空）。数据 = `Resources/prebuilt_decks.json`，
//     由 `工具/gen_prebuilt_decks.py` 生成：取 `isPractice==1` 的 **103 副**，卡表与督军按**同一套**
//     t1~t5 匹配规则解成**我们的卡 id**（与 `工具/check_prebuilt_decks.py` 同源，判据只一份）。
//     🔴 **只列经典（30 张）那 29 副**：那一池里 **54 副是遭遇模式（12 张）**，而**我们的对战不支持 12 张那套** ——
//     2026-09-26 用户拍板「**先只列经典（b）**，把补 Skirmish 对战记进计划（a）」。逐条判据
//     → `资料/预组卡组_原版规格.md` §五之四；规格 → `资料/加时与冲突模式_原版规格.md` §二。
//     ⚠️ **难度角标**（原版预组页会画 `easyMark/normalMark/hardMark`）与 **`gameMode` 图标**都**还没画**，
//     原因写在 `PrebuiltDecks.cs` 文件头 —— **是缺口，不是「原版没有」**。
//   · **两个页签的文案是我们定的**：原版两个 `m_text` 都是**空串**、只有 `mTerm`
//     （`MenuDeck/Button/PresetDeck` / `MenuDeck/Button/OwnDeck`），而**本地没有任何术语表/翻译源**
//     （2026-09-24 在 `assets_full` / `extract` / 反编译 / `资料/` 四处搜过，0 命中）⇒ 写 `Prebuilt Decks` / `My Decks`。
//   · `Instructions 2`(act=Y)：**本地拿不到译文**（它只带 `mTerm`，`m_text` 是空串）。
//     ⚠️ **2026-10-17（D6）就地更正**：原来这里接着写「同窗的 `Instructions`(act=N) 有明文
//     **"Select deck"** ⇒ **我们用它那句**填在 `Instructions 2` 的框里」—— **那句现在不成立了**：
//     标题改走**词条**（`MenuDeck/Tip/SelectDeckAgainst`，见 `TitleTerm`），中文 = 实拍「**选择卡组**」。
//     （旁注：本解包里 `GameObject/Instructions.json` 那颗 TMP 的 `m_text` 实际是 `'\n'`；
//      bundle 里 `"m_text": "Select deck"` 的两处是 fs **64** 的另一族节点，**不是同窗这颗**。）
//   · ✅ **搜索框不建**（2026-09-26 用户拍板「**按照原版设计**」）：原版出厂 `act=N`，且**四条证据都指向「没有任何代码打开它」**——
//     ① `DeckSelectionPopup` 的 6 个序列化字段里没有它；② `DeckSelectionTabController` **6 个方法里零 `SetActive`**，
//     而且它拿的是 `EverguildInputField` **组件**引用、**够不到容器 GameObject**；
//     ③ `EverguildInputField` 已反编译的 3 个方法也不动 `SetActive`；④ 预制体 `Collection Menu Input Field` 出厂 `act=N`。
//     ⇒ **原版那 4 个节点（底板 / `Search Text` / 点击区）我们一个都不建**，也不再做「按名字过滤」
//     （原版 `Filter` 那条路我们同样够不到 —— 那个输入框是做了但没启用的件）。
//     ⚠️ 反编译覆盖不全 ⇒ 「原版确实一直不显示」是**推断**、不是铁证；但取舍是用户拍的。
//   · `Filter Buttons`（act=N，HLG + `forceExpand` 会把 60×60 撑成 84.56×80）**不建** —— 照「出厂关的件不建」。
//   · `Instructions`(act=N) 那一格**也不建**。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`DeckSelectionPopup : GameWindow` —— 选一套卡组（原版从战斗入口那几扇窗开）。</summary>
    public class DeckSelectionPopup : GameWindow
    {
        // 层：**高于收藏窗（最高 3043）与 `Deck info Popup`(3120+) · `Import Deck Popup`(3130+) 之下**，
        //    但**必须低于** `PromptPopup`（3140+，提示窗要能压在所有窗之上）。
        // ✅ **2026-10-11（A252）可见性收窄**：这行原是 2026-10-04（A47 接线批）**整行**放宽成 `public` 的；
        //   留 `public` 的两个**各有实测引用**（脚本扫全工程 301 个 `.cs`、剔注释、剔本文件）：
        //   `QDs` 3 处（`Editor/MainMenuScene.cs` 的「弹窗压住整条顶栏」比较 · `CheckAbsorbRule` 的档参）、
        //   `QDsHit` 2 处（`CheckShadeRule` · `CheckAbsorbRule`）；`QDsRow`/`QDsText` **外部引用 = 0** ⇒ 回 `const`。
        //   ⚠️ 另核过：同文件里的另一个类 `DeckPick` 不用它们；`class X : DeckSelectionPopup` 全 0。
        public const int QDs = 3125;        // ✅ 留 `public`：`Editor/MainMenuScene.cs` 引用（3 处）
        const int QDsRow = 3126;            // 3126 卡组行
        const int QDsText = 3127;           // 3127 行上的字
        public const int QDsHit = 3128;     // ✅ 留 `public`：`Editor/MainMenuScene.cs` 引用（2 处）

        // ---- 几何（A1 §3 逐条；2026-09-24 从根节点复核过，见文件头）----
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.773f);
        /// <summary>`Tab buttons` 容器（HLG spacing 12.45 align MiddleLeft）；两个页签各 **260×67.6421**（模板位是重合的）。</summary>
        public const float TabL = 196.30f, TabT = 35.07f, TabW = 260f, TabH = 67.6421f, TabGap = 12.45f;
        /// <summary>红底板 `Generic Window Red Background Big`（`UI_Deck_Information_Back`，border 42/363/655/81）。</summary>
        public const float RedL = 134.50f, RedT = 82f, RedR = 1839.50f, RedB = 1032f;
        /// <summary>`Deck Display`（`Header` + `Collection Display` 的父）。</summary>
        public const float DdL = 144.50f, DdT = 124.95f, DdR = 1809.50f, DdB = 973.19f;
        /// <summary>`Deck Scroll View`（RSR 的视口）**194.50,208.63 → 1759.50,986.69**（1565 × 778.06）。</summary>
        public const float SvL = 194.50f, SvT = 208.63f, SvR = 1759.50f, SvB = 986.69f;
        public static readonly PxRect SvRect = new PxRect(SvL, SvT, SvR, SvB);
        /// <summary>`Header` 那一行（`Separator Line` 在 204.95→214.95）。</summary>
        public const float HdrT = 124.95f, HdrB = 209.95f, SepT = 204.95f, SepB = 214.95f;
        /// <summary>提示语：`Instructions`(act=N) 618.72→1335.28 · `Instructions 2`(act=Y) 1236.12→1770.24。</summary>
        public const float Ins2L = 1236.12f, Ins2R = 1770.24f;
        /// <summary>`Practice buttons`(HLG spacing 24.5 align MiddleLeft) 184.50,117.50→443.61,217.39；
        /// 子件作者 **212.24×63.92**、`childControl=0/forceExpand=0` ⇒ 保持作者尺寸、垂直居中。</summary>
        public const float RndL = 184.50f, RndW = 212.24f, RndH = 63.92f, RndBoxT = 117.50f, RndBoxB = 217.39f;
        /// <summary>搜索框 `Collection Menu Input Field`（`InputFieldBackground` 500×49.078）。</summary>
        public const float InL = 727.00f, InT = 142.91f, InR = 1227.00f, InB = 191.98f;
        /// <summary>关闭圆钮 `Generic Close Button Orange`（74.39×75.60）+ 图标 56.86×58.13。</summary>
        public const float CloseL = 1783.11f, CloseT = 62.90f, CloseR = 1857.49f, CloseB = 138.50f;
        public const float CloseIw = 56.86f, CloseIh = 58.13f;
        /// <summary>🔴 **2026-10-18（A1053）**：关窗钮那两颗子件（黄面 + 叉，同矩形 `CloseIw×CloseIh`）
        /// **自己的** `m_RaycastPadding`（原版实读 `(-20)⁴`；L,B,R,T · **负 = 外扩**）⇒ 命中区 =
        /// 子件矩形外扩 20 = **96.86 × 98.13**（⛔ 不是根矩形 `CloseL..CloseB` 的 74.38×75.60）。
        /// 算式只走 `MenuDraw.PaddedRect`；口径 → `普查_全仓命中区与关闭键族.md` §〇-1。</summary>
        static readonly Vector4 ClosePad = new Vector4(-20f, -20f, -20f, -20f);

        /// <summary>格 **225×364.5**、横向步进 **245**（= 225 + spacing 20）。</summary>
        public const float CellW = 225f, CellH = 364.5f, CellGapX = 20f, CellStep = CellW + CellGapX;
        /// <summary>列数 = `floor(视口宽 ÷ (格宽 + spacingX))`（`_controlSegmentSize=1`）。</summary>
        public static int Cols { get { return Mathf.Max(1, Mathf.FloorToInt((SvR - SvL) / CellStep)); } }
        /// <summary>内容**整体居中**的左边距 = `(视口宽 − (列数×格宽 + (列数−1)×spacing)) / 2`
        /// = (1565 − 1450) / 2 = **57.5**（`CreateCellPool` 的算法；与收藏窗 Cards/Styles 两页同一口径）。</summary>
        public static float PadX
        {
            get { return (SvR - SvL - (Cols * CellW + (Cols - 1) * CellGapX)) * 0.5f; }
        }

        public static PxRect CellRect(int i)
        {
            int r = i / Cols, c = i % Cols;
            float x = SvL + PadX + c * CellStep;
            float y = SvT + r * CellH;
            return new PxRect(x, y, x + CellW, y + CellH);
        }

        public static readonly Vector4 RedBorder = new Vector4(42f, 363f, 655f, 81f);
        const float RedTexW = 1100f, RedTexH = 701f;
        static readonly Vector4 TabBorder = new Vector4(0f, 0f, 0f, 0f);

        /// <summary>🆕 2026-10-17（D6）：标题那一格的**词条键** —— 原版 `Instructions 2` 节点上那颗
        /// `Localize` 的 `mTerm` 原文（不是自拟）。
        /// <para>判据：`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_4152179270747796863.json`
        /// （`mTerm = "MenuDeck/Tip/SelectDeckAgainst"` · `mLocalizeTargetName =
        /// I2.Loc.LocalizeTarget_TextMeshPro_UGUI`），它的 GameObject 就是
        /// `bundle_menus_assets_all/GameObject/Instructions 2.json`（组件表里含这一颗 MB）。</para></summary>
        public const string TitleTerm = "MenuDeck/Tip/SelectDeckAgainst";

        // ---- 状态 ----
        /// <summary>`false` = 预组卡组那一页（**原版出厂 `m_IsOn=1` 的就是它**）；`true` = 我的卡组。</summary>
        public bool OwnDecks;
        /// <summary>选中的「我的卡组」下标（−1 = 没选）。</summary>
        public int DeckIndex = -1;
        /// <summary>选中的预组卡组 `deckId`（空 = 没选）。与 `DeckIndex` **互不干扰**（两页各记各的）。</summary>
        public string PrebuiltId = "";
        // 🔴 **2026-09-26 删掉了搜索**（原来有个 `Search` 字段 + 输入框 + 过滤）。
        //    原因：**原版那个搜索框根本没有任何代码打开它** ——
        //    · `DeckSelectionPopup` 的 6 个序列化字段里没有它；
        //    · `DeckSelectionTabController` 对 `searchBar`(+0x40) 只做两件事（`Awake` 挂回调 · `Filter` 读文本），
        //      它 **6 个方法里一处 `SetActive` 都没有**，而且它拿的是 `EverguildInputField` **组件**引用、**够不到容器 GameObject**；
        //    · `EverguildInputField` 已反编译的 3 个方法也不动 `SetActive`；
        //    · 预制体里 `Collection Menu Input Field` 出厂 `act=N`。
        //    ⇒ 用户 2026-09-26 拍板「**按照原版设计**」⇒ 我们不再建它（原版那 4 个节点：输入框 + `Search Text` + 点击区都不建）。
        //    ⚠️ 反编译覆盖不全 ⇒ 「原版确实一直不显示」是**推断**，不是铁证；但四条证据都指向它。

        /// <summary>
        /// 选中一套之后交给外面的东西。
        /// 🔴 **为什么不是一个 `int`**：原版的回调是 `Action&lt;CardDeck>`，而 `CardDeck` **同时覆盖
        /// 「预组」与「玩家自己的卡组」**（`DeckSelectionContext.cs:12`）；我们原来只回传
        /// `CollectionData.DeckInfo`，外面再拿**名字**回查自己的卡组库 —— 预组不在库里
        /// ⇒ `idx = −1`、**静默什么都不发生**（撞「不许静默失败」）。
        /// 见 `资料/预组卡组_原版规格.md` §五之二 末。
        /// </summary>
        public struct DeckPick
        {
            /// <summary>true = 预组卡组（原版资产）· false = 玩家自己的卡组。</summary>
            public bool Prebuilt;
            /// <summary>显示用的那一份（两种都有；预组那份按 `PrebuiltDecks.Deck` 现造）。</summary>
            public CollectionData.DeckInfo Info;
            /// <summary>`Prebuilt == false` 时有效：在 `CollectionData` 里的下标。</summary>
            public int OwnIndex;
            /// <summary>`Prebuilt == true` 时有效：产物里的那一副。</summary>
            public PrebuiltDecks.Deck PrebuiltDeck;
        }

        /// <summary>点一套之后干什么（**关窗 + 回调** —— 原版 `DeckSelectionPopup.Select` 就是这两步）。</summary>
        public System.Action<DeckPick> OnPicked;

        public MenuScroll Scroll;
        /// <summary>画出来的格（自检用）。</summary>
        public readonly List<Transform> Cells = new List<Transform>();

        /// <summary>最近一次开出来的（自检用）。</summary>
        public static DeckSelectionPopup LastOpened;

        public Transform TabHit(bool own) { return Find(transform, "TabHit_" + (own ? "Own" : "Pre")); }
        public Transform CloseHit { get { return Find(transform, "CloseHit"); } }
        public Transform ShadeHit { get { return Find(transform, "BackgroundHit"); } }
        public Transform RandomHit { get { return Find(transform, "RandomHit"); } }
        /// <summary>搜索点击区 —— **恒为 null**（按原版不建搜索框；自检用它盯这一点）。</summary>
        public Transform SearchHit { get { return Find(transform, "SearchHit"); } }
        /// <summary>空态那行字现在显示什么（自检用）。</summary>
        public string EmptyText { get { return _empty; } }
        string _empty = "";
        /// <summary>空态那一件（原版 `Empty Collection Warning`）的**节点**与它那颗 `Warning` TMP ——
        /// 一件两用：**这一页有没有东西**只靠它的 `activeSelf`（原版也一样）。`Build` 重建时置空。</summary>
        Transform _emptyNode;
        Label _emptyLabel;
        /// <summary>「这一页列了多少、藏了多少」那句话的文本（自检用）。
        /// 🔴 **2026-10-17（D11）：它已经【不再上屏】** —— 原来画在红底板下沿（节点叫 `Scope Note`），
        /// 而原版空态只有 `Empty Collection Warning` 那一件 ⇒ 节点已按原版删掉，这里只**算字符串 + `Debug.Log` 出声**。
        /// ⚠️ **这个属性现在只被两处自检读**（`Editor/MainMenuScene.cs:2651` / `:2670`）—— 它们断的是
        /// **一个不上屏的字符串**了，**必须**在拿到 `MainMenuScene.cs` 时一并改（本批白名单外，已记进报告）。
        /// ⛔ 别把它读成「功能还在」。</summary>
        public string ScopeText { get { return _scope; } }
        string _scope = "";

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        public static DeckSelectionPopup Create(WindowsManager mgr, System.Action<DeckPick> onPicked = null,
                                                int? modeFilter = null)
        {
            var go = new GameObject("Deck Selection Popup with Tabs");
            var win = go.AddComponent<DeckSelectionPopup>();
            win.type = WindowType.Popup;                 // 实证 type=1
            win.placement = WindowsPlacement.Popup;      // 实证 windowsPlacement=15
            win.closeOnEsc = true;                       // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1f;              // 实证 1.0
            win.Manager = mgr;
            win.OnPicked = onPicked;
            win.ModeFilter = modeFilter;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        /// <summary>「我的卡组」那一页按**哪个模式**筛（`null` = 不筛）。
        ///
        /// 🆕 2026-09-26：原版就是筛的 —— `DeckSelectionPopup` 的 `TryOpen` 收一个 `DeckSelectionContext`，
        /// 它的筛选 lambda 是「**候选卡组的模式 == 当前卡组的模式** 或 == `context.GameMode`」
        /// （`DeckSelectionPopup___TryOpen_b__10_0.c:10-17`，出处汇总 → `资料/加时与冲突模式_原版规格.md` §2.7）。
        /// ⚠️ **预组那一页不筛** —— 照原版预组页就是**两种模式混在一页**列出来的（66 副），
        ///    点错模式那一副由 `LiveOpsEventWindow.SelectedDeckFitsMode` 在开战前挡（原版同款校验）。</summary>
        public int? ModeFilter;

        public override void Open()
        {
            // ✅ **2026-09-26 归位**：原版 `DeckSelectionTabController.Start()` 唯一一句就是
            // `ShowPrebuiltDecks(true)`（`DeckSelectionTabController__Start.c:5`）⇒ **起手落在「预组卡组」页**。
            // 我们原来写死 `OwnDecks = true`，那是因为预组那一页当时没数据（文件头那条偏离）—— **现在数据接上了，回退偏离**。
            OwnDecks = false;
            DeckIndex = FirstOwnDeckInMode(CollectionData.CurrentIndex());
            PrebuiltId = "";
            Build();
            LastOpened = this;
        }

        /// <summary>「我的卡组」里**符合本窗模式**的第一套（从 <paramref name="prefer"/> 起绕一圈）。
        /// 一套都没有 ⇒ 保持原样（那一页会显示空态）。</summary>
        public int FirstOwnDeckInMode(int prefer)
        {
            int n = CollectionData.DeckCount();
            if (n <= 0) return 0;
            for (int k = 0; k < n; k++)
            {
                int i = ((prefer < 0 ? 0 : prefer) + k) % n;
                var d = CollectionData.Raw(i);
                if (d != null && (!ModeFilter.HasValue || d.GameMode == ModeFilter.Value)) return i;
            }
            return (prefer >= 0 && prefer < n) ? prefer : 0;
        }

        // ============================================================ 列表数据

        /// <summary>当前页签要列出来的条目。
        /// ⚠️ **没有搜索过滤**了 —— 原版那个搜索框没有任何代码打开它，我们照原版不建（见 `Search` 那段的注释）。</summary>
        public List<DeckPick> Shown()
        {
            var list = new List<DeckPick>();
            if (OwnDecks)
            {
                for (int i = 0; i < CollectionData.DeckCount(); i++)
                {
                    // 🆕 2026-09-26：**按模式筛**（原版 `DeckSelectionPopup.__TryOpen_b__10_0` 那条 lambda）
                    if (ModeFilter.HasValue && CollectionData.DeckAt(i).GameMode != ModeFilter.Value) continue;
                    list.Add(new DeckPick { Prebuilt = false, Info = CollectionData.DeckAt(i), OwnIndex = i, PrebuiltDeck = null });
                }
            }
            else
            {
                foreach (var d in PrebuiltDecks.Tab)
                    list.Add(new DeckPick { Prebuilt = true, Info = InfoOf(d), OwnIndex = -1, PrebuiltDeck = d });
            }
            return list;
        }

        /// <summary>把一副预组卡组包成格子要的 `DeckInfo`（`MenuDraw.DeckCell` 只吃 Name/Faction/CardbackId 三个字段）。</summary>
        public static CollectionData.DeckInfo InfoOf(PrebuiltDecks.Deck d)
        {
            return new CollectionData.DeckInfo
            {
                Name = d.DisplayName,
                WarlordId = d.heroId,
                Faction = d.faction,
                Count = d.cardIds != null ? d.cardIds.Length : 0,
                CardbackId = d.cardback,
            };
        }

        /// <summary>当前列表条数（自检用）。</summary>
        public int ShownCount { get { return Shown().Count; } }

        // ============================================================ 建

        void Build()
        {
            // 🔴 **2026-10-18（A867 · S1）**：**首句**就把本窗名下的旧滚动区撤掉 —— 下一句就把 `root` 的子件
            //   全清掉，而第 6 步会登记一颗**新**的 `MenuScroll`（`Scroll.Owner = gameObject`）。
            //   ⛔ **兜不住**，所以非补不可（`PointerLayer` 的两道自动清理**都判不出这里已死**）：
            //     · `PruneScrolls` 的 `s == null` —— `MenuScroll` 是**普通 C# 类**（`Shell/MenuScroll.cs:79`）
            //       ⇒ 节点被销毁**不会**让它变 null ⇒ **恒假**；
            //     · `s.Owner == null` —— `Owner` 指的是**活下来的窗根**（本方法只删**子件**，`transform` 本身还在，
            //       而且我们关窗是 `SetActive(false)`、**不销毁**）⇒ 也**恒假**（`Shell/PointerLayer.cs:223-231`）。
            //   ⇒ 少了这一句 = **每重建一次净涨 1 条**，而且旧条目**还能被滚轮命中**（`OnChanged` 指向已销毁的节点）。
            //   🔴 **本窗这条路径【不靠重开窗就够得着】**：切页签 = `SwitchTab` → `RebuildAll()` → 本方法
            //   （`Shell/DeckSelectionPopup.cs:536-544` / `:573-579`），而且**同一扇窗里可以来回切**
            //   （`Editor/CollectionScene.cs:2418/:2429/:2432` 就是连切三次的现场）⇒ 这是**单次开窗内**就会涨的漏。
            //   ✅ 形状照抄 `Shell/InboxWindow.cs:405`。
            PointerLayer.UnregisterOwnedBy(gameObject);
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) CollectionWindow.DestroySafe(root.GetChild(i).gameObject);
            _emptyNode = null; _emptyLabel = null;      // 旧的那件随子件一起没了 ⇒ 引用也要清（同 `CardDetailPopup.Build`）

            // 1) 压暗 + 点背景关窗（原版 `backgroundCloseButton`）
            var shade = MenuDraw.Rect(root, CardArt.Solid(),
                                      new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f), "Menu Dark Background",
                                      QDs, ShadeColor);
            if (shade == null)
                MenuDraw.Rect(root, CardArt.Solid(), new PxRect(0f, 0f, 1920f, 1080f), "Menu Dark Background", QDs, ShadeColor);
            // 🔴 **2026-10-04（A47 接线批）订正档号：`QDsHit − 1`(3127) → `QDs`(3125)** ——
            //   规矩 = 「压暗层的命中区落在**压暗层自己那一档**，且严格低于本窗任何内容命中区档」
            //   （判据 → `MenuDraw.ShadeHit` 的注释 · `资料/待办判据_阶段二与联机.md` §A25·补（一））。
            //   ⚠️ 3127 恰好是 `QDsText`（文字那一档）。
            MenuDraw.ShadeHit(root, new PxRect(0f, 0f, 1920f, 1080f), QDs, QDsHit, () => Close(), "BackgroundHit");

            // 2) 两个页签（HLG spacing 12.45 ⇒ 第 2 个从 196.30 + 260 + 12.45 起）
            //    容器高 **68.50**、键高 **67.6421** ⇒ 竖直居中（`align=MiddleLeft`）
            float ty1 = TabT + (68.50f - TabH) * 0.5f, ty2 = ty1 + TabH;
            BuildTab(root, "Prebuilt Decks", TabL, ty1, ty2, false);
            BuildTab(root, "My Decks", TabL + TabW + TabGap, ty1, ty2, true);

            // 3) 红底板
            MenuDraw.Nine(root, CardArt.MenuUi("UI_Deck_Information_Back"), new PxRect(RedL, RedT, RedR, RedB),
                          RedBorder, RedTexW, RedTexH, QDs, null, true);
            // 🆕 **2026-10-06（A94）：红底板吸收点击**。判据 = 原版 prefab
            //   `Deck Selection Popup with Tabs > Generic Window Red Background Big` 那颗 `Image` 的
            //   **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读）—— 射线打到它自己、
            //   父链上没有点击处理器（关窗那颗 `BackgroundCloseButton` 在压暗层上）⇒ 原版**什么都不做**。
            MenuDraw.Absorb(root, "AbsorbHit", new PxRect(RedL, RedT, RedR, RedB), QDs, QDsHit);

            // 4) `Header` 那一行：分隔线 + 提示语 + 「随机挑一套」
            MenuDraw.Rect(root, CardArt.MenuUi("40k_main_line"), new PxRect(DdL, SepT, DdR, SepB), "Separator Line", QDsRow);
            // 🔴 2026-10-17（D6）：这一格的文案**走词条**，键 = 原版 `Instructions 2` 上那颗 `Localize`
            //   （<see cref="TitleTerm"/> = `MenuDeck/Tip/SelectDeckAgainst`）。
            //   ⚠️ 这里原来写「`Instructions 2`(act=Y) 的文案取不到 ⇒ 用同窗 `Instructions` 那句
            //   `Select deck`」—— **「取不到」说的是译文**（那颗 TMP 的 `m_text = ''`），
            //   **词条名一直在**（`.cs` 那句「`.cs` 说文案取不到**不成立**」指的是这件事）。
            //   ⇒ 改成按键取词：中文 = **实拍那四个字「选择卡组」**（`资料/原版参照图/用户实拍_1017/
            //   更换卡组的参考.png` 右上角）、英文 = 我们原来那句 `Select deck`。
            //   🔴 **2026-10-17（F2）就地补记两件（铁律 5 + 11）**：
            //   ① **词条表里原来根本没有这条键**（`Core/Loc.cs` 只有注释提到它）⇒ `Loc.T` 走「没有这个键
            //      ⇒ 返回键名本身 + 出声」⇒ **两语档下界面上真的印着 `MenuDeck/Tip/SelectDeckAgainst`**
            //      （不是只错在断言上）。**已补**（`Core/Loc.cs` 卡组编辑窗那一族，含两列各自的出处）。
            //   ② **英文列的原文实读不到**：本键那颗 TMP（`MonoBehaviour_925819347406796159.json`）
            //      `m_text = ''` ⇒ 「照抄原版 prefab 那颗 TMP」这条路**走不通**；`Select deck` 的旁证 =
            //      同窗兄弟 `Instructions`（键 `MenuDeck/Tip/SelectDeck`）那颗 TMP 的 `m_text = 'Select deck'`
            //      （`工具/menu_dump.py bundle_menus_assets_all "Deck Selection Popup with Tabs" --depth 3` 实读）。
            //      ⛔ 别把这一列写成「原版如此」。
            //   ⚠️ **版面一个字都没动**：rect 与对齐照 prefab（`Instructions 2` 节点中心 1503.18
            //   = 本窗那颗的矩形中心；实拍那版右端贴分隔线 = 与 prefab 有漂移，**不照它**）。
            MenuDraw.Text(root, new PxRect(Ins2L, HdrT, Ins2R, HdrB), Loc.T(TitleTerm), Color.white, "Instructions 2",
                          36f, QDsText);
            {
                float ry1 = RndBoxT + (RndBoxB - RndBoxT - RndH) * 0.5f;
                var rr = new PxRect(RndL, ry1, RndL + RndW, ry1 + RndH);
                // `UI_Button_Mulligan` **没有 border**（实读 `Sprite/UI_Button_Mulligan.json` 的 `m_Border = None`）
                // ⇒ 拉伸。走 `Nine` 会吐「border 比图还大，退回单块」（§三 第 15 条 第 57 行；同 `DeckInfoPopup`）。
                var randomBg = MenuDraw.Rect(root, CardArt.MenuUi("UI_Button_Mulligan"), rr, "Random Bg", QDsRow);
                // 🔴 **2026-10-18（A891 的续）：字走 `Loc.T`** —— 词条键 = 原版那颗 `Localize.mTerm`
                //   的原文 `MenuDeck/Button/Random`（本批按 pid 亲读；父链 = `Deck Selection Popup with Tabs >
                //   Deck Display > Header > Practice buttons > Generic Simplified UI Button > Button Text` ——
                //   就是本行底下那颗 `EverguildButton`）。
                //   ⚠️ **中英两列都是我们拟的**：那颗 TMP（`Button Text_7450249239293310335`）的
                //   `m_text = ''`（本批实读）⇒ **英文原文取不到**（同 `MenuDeck/Tip/SelectDeckAgainst` 那条的先例），
                //   而 `zh_CN.csv` 里也没有 `Random` ⇒ 中文按英文自拟「随机」。⛔ 别写成「照抄原版」。
                //   ⛔ 节点名 `"Random Text"` 与 `"Random Bg"` 不动。
                MenuDraw.Text(root, rr, Loc.T("MenuDeck/Button/Random"), Color.white, "Random Text", 45f, QDsText);
                // A17：原版 `…>Practice buttons>Generic Simplified UI Button` 是 SpriteSwap（普查 §块 3 第 9 行）；
                // 底下那颗 `EverguildButton`(trans=1) 不换图 —— 换图的是 `Button` 那颗
                Hit(root, "RandomHit", rr, PickRandom, QDsHit, randomBg, "UI_Button_Mulligan");
            }

            // 5) ❌ **搜索框不建**（按原版：预制体里 `Collection Menu Input Field` 出厂 `act=N`，
            //    而且**没有任何代码打开它** —— 四条证据见 `Search` 那段注释）。
            //    原版那 4 个节点（输入框底板 / `Search Text` / 点击区）我们**一个都不建**。

            // 6) 卡组列表（RSR 视口 + 6 列格）
            var holder = MenuDraw.Node(root, "Deck Scroll View", SvRect);
            // 🔴 **2026-10-13（A435 阶段 2 · 乙 · A21）**：裁切状态长在这颗视口节点上
            //   （= 原版 `Viewport` 上那个 `RectMask2D`；本窗这两个视口实读 `m_Padding = (0,0,0,0)` ·
            //    `m_Softness = (0,0)` —— 与 `RebuildCells` 里原来那一对 `Clip = SvRect; … Clip = prevClip;` 同值）。
            var vc = ViewportClip.Hang(holder, "Viewport", SvRect, Vector4.zero, Vector2Int.zero);
            var shown = Shown();
            int rows = Mathf.Max(1, Mathf.CeilToInt(shown.Count / (float)Cols));
            Scroll = MenuScroll.TopAligned(SvRect, rows * CellH);
            Scroll.Owner = gameObject;
            Scroll.ClipNode = vc;                       // 🔴 A465（构建循环那一路也要吃这颗节点）
            Scroll.OnChanged = () => RebuildCells(holder);
            PointerLayer.RegisterScroll(Scroll);
            RebuildCells(holder);

            // 7) 空态那一件 —— **原版就有**：`Deck Scroll View/Empty Collection Warning`
            //    （`DeckCollectionDisplay.emptyWarning` 这个序列化字段指向的就是它），出厂 `act = F`。
            //    🔴 **2026-10-17（D11）现读原版 prefab 逐字段核过**（`menu_dump.py bundle_menus_assets_all
            //       "Deck Selection Popup with Tabs" --depth 6`）：
            //      · 节点 = `…/Collection Display(DeckCollectionDisplay)/Deck Scroll View/Empty Collection Warning`
            //        —— 是 `Viewport` 的**兄弟**（挂在 `Deck Scroll View` 下），rect = **194.5,208.6 → 1759.5,986.7**
            //        （= **整个视口那一格**，与本窗 `SvRect` 同值）；**无 Image 组件**（不挡射线）。
            //      · 子件 = 一颗 TMP，名叫 **`Warning`**，文案 `'There are no deck in your collection for the selected filters'`，
            //        **fs 36.0 · base 36.0 · 对齐 Center/Middle · 折行=1 · 色 (1,1,1,1)**，**无 auto**。
            //    ⚠️ **文案仍是我们自己的三条**（如实说明**为什么**空 —— 红线：不许静默失败）；原版那一句是
            //       **写死的英文**（还带着原文的语法错误 "no deck"）。版面照原版，文字照实说 —— 这叫**出声**。
            RefreshEmptyText();
            _emptyNode = MenuDraw.Node(root, "Empty Collection Warning", SvRect);
            _emptyLabel = MenuDraw.Text(_emptyNode, SvRect, _empty, Color.white, "Warning", 36f, QDsText, SvRect.W);
            // 🔴 **必须马上摆一次显隐**：`RebuildCells`（第 6 步）里的那次 `RefreshEmptyNote` 跑在本件**之前**，
            //    当时 `_emptyNode` 还是 null ⇒ 什么都没做。少了这一句，「列表非空」时这一件会**留在开着**的状态
            //    （只是文案是空串、看不见 —— 静默错），自检那条 `activeSelf == false` 会红。
            RefreshEmptyNote();

            // 7b) ❌ **原来这里有一行我们自己加的小字 `Scope Note`**（红底板下沿，`(DdL, SvB+6)-(DdR, RedB-6)`）——
            //     🔴 **2026-10-17（D11）删掉**：原版空态**只有** `Empty Collection Warning` 那**一件**
            //     （判据同上，`DeckCollectionDisplay.emptyWarning`），我们多出来的这一行是**真偏离** ⇒ **节点不建**。
            //     ⚠️ 但 `_scope` 那句话**照算**（+ `Debug.Log` 出声）—— 见 `RefreshScopeNote` 的注释：
            //     `Editor/MainMenuScene.cs:2651/2670` 两条自检还在读 `ScopeText`，**那个文件不在本批白名单**。
            RefreshScopeNote();

            // 8) 关闭圆钮
            // 🔴 **2026-10-03 补一层（A17 顺带查出的真偏离）**：原版 `Generic Close Button Orange` 是
            //    **三层**（圆底 → **黄面 `40k_general_bt_yellow`** → 关闭图标），我们这里**漏了中间那层**
            //    （同族 `DeckInfoPopup` / `PlayerProfileWindow` 都有）。⚠️ **换图的目标是【圆底那一层】**（见下面 `Hit` 那条），
            //    ⚠️ 同时把图标提到更高一档队列 —— 原来底色与图标**同一个 `QDsRow`**（谁盖谁不可控，
            //    见 `资料/已知的坑.md`「同一个渲染队列的两层」）。
            // 🔴🔴 **2026-10-18（A1149 第一半）圆底盘【挂点 + 按钮节点】归真**（本件 = 第九会话 P7）：
            //   改前**原版那颗 `Generic Close Button Orange` 节点我们根本没建** —— 三颗图全是 `root`
            //   的直接子件、圆底盘画在自造子件 `Close Bg` 上。
            //   判据（逐字段直读）= `python -I d:/tmp/wf_b4probe/pa.py bundle_menus_assets_all
            //   "Deck Selection Popup with Tabs" 8`：根 `Generic Close Button Orange`（**74.39×75.60**）
            //   **自己带 `Image`**：`UI_Button_Round_background` · `m_Type=0`(Simple) · **`m_PreserveAspect=1`** ·
            //   `m_PixelsPerUnitMultiplier=1.0` · `m_RaycastTarget=0`；它下面唯一的子件 = `Background`(56.86×58.13)
            //   （= 我们那颗 `Close Face`）。贴图 `m_Rect` = **237×237 正方** ⇒ 实绘 **74.39×74.39**。
            //   ⇒ 照兄弟窗先例 `Shell/InboxWindow.cs:386-387` / `Shell/ChatPanel.cs:307-308` 的形状
            //   （`Rect` 画在根节点上、图取不到才退回 `Node`）。矩形与 `keepAspect` **改前就是对的**。
            var closeBaseQ = MenuDraw.Rect(root, CardArt.MenuUi("UI_Button_Round_background"),
                          new PxRect(CloseL, CloseT, CloseR, CloseB), "Generic Close Button Orange", QDsRow, null, true);
            var closeNode = closeBaseQ != null ? closeBaseQ.transform
                          : MenuDraw.Node(root, "Generic Close Button Orange", new PxRect(CloseL, CloseT, CloseR, CloseB));
            float ix = CloseL + (CloseR - CloseL - CloseIw) * 0.5f;
            float iy = CloseT + (CloseB - CloseT - CloseIh) * 0.5f;
            var closeFace = MenuDraw.Rect(closeNode, CardArt.MenuUi("40k_general_bt_yellow"),
                          new PxRect(ix, iy, ix + CloseIw, iy + CloseIh), "Close Face", QDsRow + 1, null, true);
            MenuDraw.Rect(closeNode, CardArt.MenuUi("40k_general_bt_yellow_close"),
                          new PxRect(ix, iy, ix + CloseIw, iy + CloseIh), "Close Icon", QDsRow + 2, null, true);
            // 🆕 **2026-10-18（A1058 · 第六会话批 2）**：换图那一层 = **黄面 `closeFace`**
            //   （`40k_general_bt_yellow`），⛔ **不是圆底盘 `closeBaseQ`**（它留着**只画**，不做换图目标；
            //   本件归真后它已从自造子件 `Close Bg` 搬到根节点 `Generic Close Button Orange` 自己身上）。
            //   判据（原版 prefab 亲读）=
            //   `python -I d:/tmp/wf_hit/rcunion.py bundle_menus_assets_all "Deck Selection Popup with Tabs" --depth 8`：
            //   根 `Generic Close Button Orange` 那颗 `EverguildButton` 的
            //   **`m_TargetGraphic` = pid4362550681893451135**；解该 pid ⇒ **所属 GO 名 = `Background`**、
            //   贴图 pid `5693181797853584851` → `40k_general_bt_yellow`（`d:/4/_tmp_view/sprite_pids_ALL.json`）。
            //   ⚠️ **2026-10-18 更正（铁律 5）**：原注释写「`trans=2` 换的是它自己的 Image」——
            //   **那句是错的**（错因 = 只读 `m_Transition`、没读 `m_TargetGraphic`）。
            // 🆕 **2026-10-18（A1053 · 第六会话批 2）**：**命中区**归真值 —— 原版根那颗
            //   `UI_Button_Round_background` 带 `m_RaycastTarget = 0`（不吃射线），吃射线的是两颗同矩形子件
            //   （56.86×58.13）按 `(-20)⁴` 外扩 ⇒ **96.86 × 98.13**；改前传根矩形（74.38×75.60）⇒ 每边小 11.2。
            //   判据 = `python -I d:/tmp/wf_hit/rcpad.py bundle_menus_assets_all "Deck Selection Popup with Tabs"
            //   --depth 8 --substr "Generic Close Button"`（实读 `96.86 x 98.13`）。
            Hit(root, "CloseHit",
                MenuDraw.PaddedRect(new PxRect(ix, iy, ix + CloseIw, iy + CloseIh), ClosePad),
                () => Close(), QDsHit, closeFace, "40k_general_bt_yellow", "40k_general_bt_yellow_hover");

            if (MissingArtCount() > 0)
                Debug.LogWarning("[DeckSel] ⚠️ 有 " + MissingArtCount() + " 张图取不到（**这几件没画**）：" + MissingArtList());
        }

        readonly List<string> _missing = new List<string>();
        Texture2D Art(string n)
        {
            var t = CardArt.MenuUi(n);
            if (t == null && !_missing.Contains(n)) _missing.Add(n);
            return t;
        }
        int MissingArtCount() { return _missing.Count; }
        string MissingArtList() { return string.Join("、", _missing.ToArray()); }

        /// <summary>选中态那一对 —— 原版 `EverguildToggle` 的**换底图 + 换染色**（⚠️ **不是**我们原来那套「只改文字色」）。
        ///
        /// <para>判据（第一权威 · 原版 prefab 序列化字段，2026-10-17 现读）= `bundle_menus_assets_all` 的
        /// `Deck Selection Popup with Tabs/Alliance Header Buttons/Tab buttons/{Generic Tab UI Button,Generic Tab UI Button 1}`
        /// 两颗 `EverguildToggle`（MB pid `907016068568626559` = `m_IsOn=1` 那颗 / `-4477857345849663325` = `m_IsOn=0` 那颗，
        /// **七个同名件的字段逐字相同**）：
        /// `changeSpriteOnValueChange = **1**` · `colorTintOnValueChange = **1**`（**两个开关都开着** ⇒ 两条都生效）·
        /// `onSprite = 40K_tab_button` · `offSprite = 40K_tab_button_overwindow` ·
        /// `onColor = (1, **0.6308285**, 0, 1)` · `offColor = (1, **0.5442529**, 0, 1)`（字面量照 JSON 抄，别四舍五入成 0.631/0.544）。</para>
        ///
        /// <para>落地代码 = `decomp_full/EverguildToggle__RefreshVisuals.c`：按**自己那颗 toggle 的 `m_IsOn`** 分别调
        /// `ToggleSprite`（`__ToggleSprite.c`：`isOn` ⇒ `Image.sprite = onSprite`，否则 `offSprite`）与
        /// `ToggleTint`（`__ToggleTint.c`：`isOn` ⇒ `onColor`，否则 `offColor`，走 `CanvasRenderer.SetColor`）。</para>
        ///
        /// <para>🔴 **为什么不能再靠文字色区分**：原版两颗 `Button Text` 的 TMP **都是 `色=(1,1,1,1)`**，
        /// 而且它的组件表是 `TextMeshProUGUI,EverguildTextController,Localize` —— **没有 `EverguildButtonMaterialModifier`**，
        /// 而 `ToggleTint` 只染**带那个组件**的 graphic（`__ToggleTint.c` 遍历的是
        /// `EverguildButtonHelper.GetGraphicsInChildren` 收出来的修饰器那一串）⇒ **染不到字**。
        /// 原来那句「我们用文字色区分（选中亮、未选中灰）」是我们自己发明的，已删（铁律 5 就地订正）。</para></summary>
        /// <summary>原版那两颗 toggle 的 `onColor` / `offColor`（**逐字抄 prefab JSON 的浮点**，⛔ 别四舍五入）。</summary>
        static readonly Color TabOnColor = new Color(1f, 0.6308285f, 0f, 1f);
        static readonly Color TabOffColor = new Color(1f, 0.5442529f, 0f, 1f);
        /// <summary>原版那两颗 toggle 的 `onSprite` / `offSprite`（两颗**共用同一对**）。</summary>
        const string TabOnArt = "40K_tab_button", TabOffArt = "40K_tab_button_overwindow";

        void BuildTab(Transform root, string label, float x1, float y1, float y2, bool own)
        {
            var r = new PxRect(x1, y1, x1 + TabW, y2);
            bool on = OwnDecks == own;                  // = 原版那颗 `EverguildToggle.m_IsOn`
            // 底图与染色**都由 `on` 决定**（⛔ 不是「按是哪一颗页签写死两张图」—— 那会把
            // `My Decks` 选中时的底图钉死在 `…_overwindow` 上，正是 D12 那条偏离）。
            MenuDraw.Rect(root, Art(on ? TabOnArt : TabOffArt), r,
                          own ? "Generic Tab UI Button 1" : "Generic Tab UI Button", QDsRow,
                          on ? TabOnColor : TabOffColor);
            // 文字**两态都是白的**（原版两颗 TMP 都是 `(1,1,1,1)`，且染色够不到它 —— 见上面那段）
            MenuDraw.Text(root, r, label, Color.white, "Tab Text " + (own ? "Own" : "Pre"), 34f, QDsText);
            Hit(root, "TabHit_" + (own ? "Own" : "Pre"), r, () => SwitchTab(own), QDsHit);
        }

        /// <summary>切页签（照原版：重建列表 + `Instructions` 那类提示不变）。</summary>
        public void SwitchTab(bool own)
        {
            if (OwnDecks == own) return;
            OwnDecks = own;
            DeckIndex = -1;
            Debug.Log("[DeckSel] 切到「" + (own ? "我的卡组" : "预组卡组") + "」那一页");
            RebuildAll();
        }

        // ❌ **`BeginTyping` / `RefreshSearchText` 已删**（2026-09-26）—— 搜索框不建了，见 `Search` 那段注释与 `Build()` 第 5 步。
        //    原版的 `Filter`（按卡组名过滤）那条路因此在我们这儿不会触发；**这是照原版的**（原版也够不到那个输入框）。

        /// <summary>抽一套（原版 `RandomizeDeck`：从当前列表随机挑 ⇒ **选中并关窗**）。</summary>
        public void PickRandom()
        {
            var shown = Shown();
            if (shown.Count == 0) { Debug.Log("[DeckSel] 当前列表是空的，没得抽"); return; }
            Pick(shown[Random.Range(0, shown.Count)]);
        }

        /// <summary>选一套：**关窗 + 回调**（= 原版 `DeckSelectionPopup.Select` 的两步）。</summary>
        public void Pick(DeckPick pick)
        {
            if (pick.Prebuilt)
            {
                PrebuiltId = pick.PrebuiltDeck != null ? pick.PrebuiltDeck.deckId : "";
                Debug.Log("[DeckSel] 选中预组：「" + pick.Info.Name + "」(" + PrebuiltId + ") ⇒ 关窗 + 回调");
            }
            else
            {
                DeckIndex = pick.OwnIndex;
                Debug.Log("[DeckSel] 选中：「" + pick.Info.Name + "」⇒ 关窗 + 回调");
            }
            if (OnPicked != null) OnPicked(pick);
            Close();
        }

        /// <summary>重建整个窗（切页签时调）。</summary>
        public void RebuildAll()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) CollectionWindow.DestroySafe(transform.GetChild(i).gameObject);
            _missing.Clear();
            Build();
        }

        /// <summary>只重建列表（搜索时调 —— 别整窗重建，不然键盘焦点会跟着没了）。</summary>
        public void RebuildListNow()
        {
            var h = Find(transform, "Deck Scroll View");
            if (h != null) RebuildCells(h);
            RefreshEmptyNote();
        }

        void RebuildCells(Transform holder)
        {
            var vp = holder.Find("Viewport");
            var parent = vp != null ? vp : holder;
            for (int i = parent.childCount - 1; i >= 0; i--) CollectionWindow.DestroySafe(parent.GetChild(i).gameObject);
            Cells.Clear();

            var shown = Shown();
            if (Scroll == null) return;
            // 🔴 **2026-10-11（A198③）**：本窗此前**从不设 `Clip`**（格子走裸重载、自己把 `SvRect` 当 `clip` 传）。
            //   改走 `MenuDraw.DeckCell(GameWindow, …)` 之后，「裁哪一块」由窗级状态说了算
            //   （= 原版模型：mask 挂在**视口节点**上）。
            //   ⚠️ **零行为变化**：本窗 `ClipPad` 从未被设过（= 0），旧写法那个 `maskPad` 缺省也是 0 ⇒ 四个输入两两相同。
            //   🔴 **2026-10-13（A435 阶段 2 · 乙 · A21）**：原来这里那对「`Clip = SvRect;` → 循环 →
            //   `Clip = prevClip;`」**整对删掉** —— 状态迁到了 `Deck Scroll View/Viewport` 那颗
            //   `ViewportClip` 上（`Build()` 里 `ViewportClip.Hang` 建的）。于是这里那个重载转发的
            //   `Clip` 恒 `null` + `ClipPad` = 0 ⇒ `MenuDraw.DeckCell` 内部沿 `parent` 找到**同一颗节点**。
            //   仍然零可见变化（节点框 = `SvRect` 反推、`pad` 全 0 ⇒ 逐字段同值）；
            //   ⚠️ 顺带把下面 `RefreshEmptyNote` 那句 `MenuDraw.Text` 的语义也摆正了 ——
            //   它本来就该**不被裁**（那句话在 `root` 上、不在视口节点下）⇒ 现在靠父链天然成立。
            //
            // ================= 🔴 2026-10-17（D16）：格子的 `Ban Icon` / `Create` 两态 —— **查清了，都不建** =========
            //   施工单 D16 说「触发条件没查」。**本轮查清了**，判据与两个条件都写在这儿（照做 = 保持不画）：
            //   · 原型格的脚本 = **`CollectionDeck`**（`CollectionItem<CardDeck>`），判据 = `CollectionDeck.cs` 的字段表
            //     与 `decomp_full/CollectionDeck__Config.c`（**唯一一处**动这两个 GameObject 的代码）。
            //     字段↔节点（按声明序 + 用法对出来的，`bannedImage`/`createObject` 逐条有下面的证据）：
            //       `content`=0x68 · `highlight`=0x70 · **`createObject`=0x78** · **`cardObject`=0x80** ·
            //       `gameModeIcon`=0x88 · **`bannedImage`=0x90** · `greyscaleController`=0x98 ·
            //       `easyMark`=0xa0 · `normalMark`=0xa8 · `hardMark`=0xb0
            //       （旁证：`+0x60` 那颗按 `PrebuiltDeck.difficulty` 换 `0xa0/0xa8/0xb0` 三张图 ⇒ 就是 `deckDifficulty`；
            //        `+0x50` 收 `deck.cardback` ⇒ `deckCardback`；`+0x58` 收 `ArmyIconsSO.GetArmyIcon` ⇒ `deckFaction`）。
            //   · **`Create`（`createObject`）**：`Config` 里 `SetActive(0x78, item == null)` ——
            //     **它是「列表里那一格没有卡组」的空位格**（`Config(null)`），文案 `Create\nNew Deck`。
            //     🔴 **本窗永远不会有这种格子**：`DeckSelectionTabController.ShowOwnDecks/ShowPrebuiltDecks`
            //     传进来的都是 `InventoryManager.GetInventory<…>().Where(筛选).ToArray()` / 预组数组，
            //     **没有一处塞 `null`**（`DeckSelectionPopup.Initialize(IEnumerable<CardDeck>)` 的形参也是卡组序列）。
            //     ⇒ 这一态在原版的**这扇窗**里本来就出不来 ⇒ **不建**（同 §八「New/Ban 都不印」）。
            //   · **`Ban Icon`（`bannedImage`）**：`Config` 末尾 `SetActive(0x90, cVar4)`，其中
            //     `cVar4 = Enumerable.Any(deck.GetLibraryInFull(), c => deck.CustomGameModeEvent.IsCardBanned(c))`
            //     （谓词 = `CollectionDeck.__c__DisplayClass16_0.<Config>b__0` → `PlayEventData.IsCardBanned`）
            //     ⇒ 触发条件 = **这副卡组里有一张被「该卡组的自定义活动」禁掉的卡**；
            //     命中时同时 `greyscaleController.ToggleGreyScale(true)`（4 张图转灰）+
            //     `DeckName` 色改成 `(1, 0.3, 0.3, 1)`（常量 `0x1834b2e50…` vs `0x1834b3210…`，读法见
            //     `资料/普查产出_0923/A2_Deck页.md` §2·3「被 Ban」那行）。
            //     🔴 **本 build 判据是空的**：禁卡表来自 LiveOps 活动数据（`PlayEventData` / 远端 CCD），
            //     **本地没有**（用户 2026-09-17 边界「过期的活动不做」）⇒ **永不触发** ⇒ **不建**（⛔ 不编一个禁用卡表）。
            //   · 两态的画法 / 触发条件**全表也已在** `资料/普查产出_0923/A2_Deck页.md` §2·2（逐子件 sprite / 字体 / 出厂 act）
            //     与 §2·3（六种状态 × 多显少显）—— 将来真要建，照那两张表，⛔ 别重新猜。
            //   ⚠️ 顺带记：格子几何**不归本文件**（走 `MenuDraw.DeckCell`，与收藏窗 Deck 页共用一份）⇒
            //     这两态真要补，落点在 `Shell/MenuDraw.cs`（**本批白名单外**）。
            for (int k = 0; k < shown.Count; k++)
            {
                var r = Scroll.Shift(CellRect(k));
                if (!Scroll.Intersects(r)) continue;
                var pick = shown[k];   // ⚠️ 在循环体内声明 ⇒ 闭包每轮各拿一份（别提到循环外）
                bool sel = pick.Prebuilt
                    ? (pick.PrebuiltDeck != null && pick.PrebuiltDeck.deckId == PrebuiltId)
                    : (pick.OwnIndex == DeckIndex);
                string cellName = pick.Prebuilt
                    ? "DeckSel_Pre_" + (pick.PrebuiltDeck != null ? pick.PrebuiltDeck.deckId : "?")
                    : "DeckSel_" + pick.OwnIndex;
                // 🆕 **预组格多两层**（原版 `<Deck>` 下本来就有）：
                //   ① **模式图标** —— 喂 `PrebuiltDeck.gameMode`（`DeckDrawer.Draw` 那条）
                //   ② **难度角标** —— 只在**预组页**显示：原版 `DeckCollectionDisplay.displayDifficultyLabel`
                //      在 `ShowPrebuiltDecks` 里置 1、`ShowOwnDecks` 里置 0（`DeckSelectionTabController__*.c`）
                bool pre = pick.Prebuilt && pick.PrebuiltDeck != null;
                Cells.Add(MenuDraw.DeckCell(this, parent, cellName, r, pick.Info, sel,
                                            QDsRow, QDsText, QDsText + 1, QDsHit,
                                            () => Pick(pick),
                                            pre ? (int?)pick.PrebuiltDeck.gameMode : null,
                                            pre ? (int?)pick.PrebuiltDeck.difficulty : null,
                                            showDifficulty: !OwnDecks));
            }
            RefreshEmptyNote();
        }

        void RefreshEmptyNote()
        {
            RefreshEmptyText();
            // ⚠️ 不按名字去 `Find`（原版那件叫 `Empty Collection Warning`、TMP 子件叫 `Warning`）——
            //    直接用 `Build` 存下来的两份引用（重建后为空则是「本窗还没建」，不是「找不到」）。
            if (_emptyLabel != null) _emptyLabel.SetText(_empty);
            if (_emptyNode != null) _emptyNode.gameObject.SetActive(Shown().Count == 0);
        }

        /// <summary>空态那句话。**如实说明原因**，不编数据（红线：不许静默失败）。</summary>
        void RefreshEmptyText()
        {
            if (Shown().Count > 0) { _empty = ""; return; }
            // 🔴 **2026-10-18（波 1b）**：这两句**整句**走词条 —— `MenuDeck/Error/PrebuiltMissing`
            //   （`MenuDeck/Error/PrebuiltMissing`）与 `MenuDeck/Error/NoUsablePrebuilt` 两条键。
            //   两列 ZH 与改前写死串（含那句拼接）**逐字相同** ⇒ 中文档零变化。
            if (!PrebuiltDecks.Available)
                _empty = Loc.T("MenuDeck/Error/PrebuiltMissing");
            else if (!OwnDecks && PrebuiltDecks.Tab.Count == 0)
                _empty = Loc.T("MenuDeck/Error/NoUsablePrebuilt");
            else
                // 空态那句话走词条（键 `MenuCollection/NoDecksFound`；施工单 §附_Shell #31）。
                // ⚠️ **如实记**：表里中文列是「没有符合当前筛选的卡组」—— 本窗没有筛选器（搜索框已按原版去掉）
                //    ⇒ 中文档下这句比改前那句**多提了「筛选」**，语义略有出入；施工单判「最近邻 ⇒ 复用」，照做。
                _empty = Loc.T("MenuCollection/NoDecksFound");   // ⚠️ 原来这里会说「搜索串：…」—— 搜索框已按原版去掉（见 `Search` 那段注释）
        }

        /// <summary>
        /// 「这一页列了什么、藏了什么」那句话 —— **只算字符串 + `Debug.Log` 出声，不上屏**。
        /// 🔴 **2026-10-17（D11）**：原来它画在红底板下沿（节点 `Scope Note`），但**原版没有这一行**
        /// （空态只有 `Empty Collection Warning` 那一件，判据 → `Build()` 第 7 步那一段）⇒ 节点删掉。
        /// ⚠️ 为什么**字符串还留着**：`Editor/MainMenuScene.cs:2651` / `:2670` 两条自检在读 `ScopeText`
        /// （`ds2.ScopeText.Contains("本页列 " + tab.Count)` / `ds2.ScopeText.Length == 0`），
        /// 而**那个文件不在本批白名单** ⇒ 删属性会让**整个工程编不过**。⇒ 折中：**算**、**出声**、**不画**，
        /// 并在报告里把「那两条断言现在断的是一个不上屏的字符串」记成**欠账**（拿到那个文件就改）。
        /// ⚠️ 为什么信息本身仍要有：我们**只列经典**、而且**拼不齐的整副不显示** —— 两件都会让玩家觉得「牌少了」，
        /// 所以如实出声（见文件头与 `资料/预组卡组_原版规格.md` §五之四）。
        /// </summary>
        void RefreshScopeNote()
        {
            _scope = ScopeNoteText();
            // 不上屏了 ⇒ 至少出声（`Build` 一次一条；`RebuildCells` 那种高频路径不调它）
            if (_scope.Length > 0) Debug.Log("[DeckSel] " + _scope);
        }

        /// <summary>那句话的**算法**（唯此一处；`RefreshScopeNote` 只负责出声）。</summary>
        string ScopeNoteText()
        {
            if (OwnDecks) return "";
            if (!PrebuiltDecks.Available) return "预组数据读不到（重跑 工具/gen_prebuilt_decks.py）";
            int classic, skirm; PrebuiltDecks.CountByMode(out classic, out skirm);
            int dropped = PrebuiltDecks.NotListed;
            var sb = new System.Text.StringBuilder();
            // 🔴 这句话要**如实**：列出来的是两类模式**混在一页**（2026-09-26 起照原版全列），
            //    经典 30 张 / 遭遇 12 张 —— 靠每格右下角的模式图标区分，这里先说清各有多少。
            sb.Append("预组共 ").Append(PrebuiltDecks.All.Count).Append(" 副 · 本页列 ")
              .Append(PrebuiltDecks.Tab.Count).Append(" 副（经典 ").Append(classic)
              .Append(" · 遭遇 ").Append(skirm).Append("，按原版难度序）");
            if (dropped > 0) sb.Append(" · ").Append(dropped).Append(" 副我们卡池拼不齐（不显示）");
            return sb.ToString();
        }

        // ============================================================ 小工具

        Transform Hit(Transform parent, string name, PxRect r, System.Action onClick, int q = QDsHit,
                      ImageQuad target = null, string art = null, string hoverArt = null)
        {
            var hit = MenuDraw.Node(parent, name, r);
            var quad = ImageQuad.Create(hit, CardArt.Solid(), Vector3.zero, LayoutSpace.Px(r.H),
                                        new Vector2(0.5f, 0.5f), "Hit");
            if (quad != null)
            {
                quad.SetAspect(r.W / Mathf.Max(1e-6f, r.H));
                quad.SetTint(new Color(0f, 0f, 0f, 0f));
                quad.SetRenderQueue(q);
            }
            var wb = hit.gameObject.AddComponent<WindowButton>();
            wb.onClick = onClick;
            if (target != null) wb.Bind(target, art, hoverArt);
            return hit;
        }
    }
}
