// DeckRuntime.cs — 卡组编辑界面的**运行时控制器**（建界面 + 听鼠标/键盘 + 重画）
//
// 为什么需要它：`DeckEditorState`（纯 C#）早就写好、自检也覆盖了行为，
// 但**运行时交互层从来没建过** —— `DeckEditor.unity` 里原来只有 `ImageQuad`/`Label` 画的
// 一堆静态方块，点/拖一律不响应。这个类补上「谁听鼠标 + 谁重画」这一层。
//
// ============================ 版面权威（唯一出处） ============================
// `D:/2/Warpforge_tools/data/ui_layout/_deck_editing_godot_rects.txt`
//   —— `chain_rect.py` 算的 Godot 绝对坐标（1920×1080，y 向下），每行带 `RectTransform_<id>.json` 可复查。
// 节点树同一棵：`资料/说明书/04_界面UI/菜单全树.md` 的 `Deck Editing Menu` 段（:9249-9496），
//   带每个元素的 sprite id；id → 名字是拿 `ui_extract/**/Sprite/*.json` 的 `pathid` 反查的。
// 三路查证的正本：`资料/卡组编辑界面_查证_0920.md`（**动手前先读它**）。
//
// 下面所有 `const float` 都是**从那张表逐行抄下来的**（写的是 x,y,w,h，和表里同序），不是估的。
// **改版面先改表、再改这里。**
//
// ============================ 交互判据（逐条照原版） ============================
//   · 卡池卡：**左键 = 弹放大窗** · **右键 = 加进卡组**（`DeckEditingWindow__OnCardClick.c:16,42`）
//   · 卡组条目：**左键 = 弹放大窗**（**不是删除**！`DeckEditingWindow__Start.c:109` → `OpenCardInformation`）
//   · **删除只有「拖出」一条路**（`DeckEditingPanel__CheckCardSlotDrag.c:59,66`）
//   · **ESC = 保存**（`DeckEditingWindow__ESCPressed.c:5`，**不是关闭**）· Done = 保存（`TrySaveDeck.c`）
//   · **翻页不存在** —— 卡池是 `RecyclableScrollRect` 无限滚动（`CollectionDisplay.cs:11`）；
//     我们 2026-09-20 之前那版**自加的 prev/next 分页已删**（`DeckEditorState` 里那套 Page/PageCount 也删了）
//   · 筛选栏**在左**、与侧栏同一块 rect（由 Header 的 `Filters` 键开合）—— 权威坐标 R:174-176
//
// ============================ 仍然是我们挑的（原版查不到，如实标） ============================
//   ① **卡池一屏 4 列 × 2 行** —— 原版是 `RecyclableScrollRect` 运行时算的，节点树给不出（`查证:166`）
//   ② 🔴 **2026-10-04（A41 ①）订正 —— 这一条已经不成立了，页签图标【照原版整格画】**：
//      原来写的是「页签图标显示尺寸 100×100 —— 节点树里那三个 `Icon` 的 rect 是 **0×0**（VLG 撑的）」，
//      **那个 0×0 是「VLG 布局跑之前的模板位」**（铁律 5·c：只量了一种情况）。布局跑完之后：
//      `Icon` 的 rect = **整格 108.96×150** + `m_PreserveAspect = 1`，源图 126×126
//      ⇒ 原版实绘 **108.96²**（居中）。判据 = `python 工具/menu_dump.py bundle_menus_assets_all
//      "Deck Editing Menu" --depth 7`：`Cards/Icon [0.3,156]–[109.2,306] 108.96×150 …… preserveAspect`。
//      我们原来画 100×100 ⇒ **小 8.97px（≈9%）**，现在按整格 + `keepAspect` 画。
//   ③ **行距 56**（行高 55.7 是权威，垂直排布的 spacing 查不到）· **卡池行距 528**（卡高 512 + 16 缝）
//   ④ 侧栏三个钮（新建/复制/删除）**已按原版删掉** —— 原版那套动作在 `DeckInfoPopup` 的 5 圆钮里
//   ⑤ 稀有度色条的颜色值、筛选栏里各行的高度与字号（原版是 VLG 流式，绝对坐标只是模板位）
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>卡组编辑界面的运行时控制器。挂在 `DeckEditor.unity` 的根对象上。</summary>
    public class DeckRuntime : MonoBehaviour
    {
        public static DeckRuntime Instance { get; private set; }

        // ============================================================ 比例尺
        // 原版界面按 1920×1080 设计；这套布局可见高 10 世界单位 → 108 px/单位
        public const float PxPerUnit = 1080f / LayoutSpace.DesignHeight;   // = 108
        public const float ScreenW = 1920f, ScreenH = 1080f;

        // ============================================================ 版面常量（px，y 向下）
        // ---- Content Area / Header（附表 R:2-31）----
        const float HdrBackX = 192.2f, HdrBackY = 83.5f, HdrBackW = 150f, HdrBackH = 60f;
        const float HdrFltBtnX = 367.2f, HdrFltBtnY = 88.5f, HdrFltBtnS = 50f;
        const float HdrFltIconX = 377.2f, HdrFltIconY = 98.5f, HdrFltIconS = 30f;
        const float HdrFltLblX = 437.2f, HdrFltLblY = 88.5f, HdrFltLblW = 150f, HdrFltLblH = 50f;
        // ⚠️ `Clear filters` 的 x **不能照抄 dump 的 1488.6** —— 它落在父容器 `Filters [592,71 876×85]`
        //    的**外面**（容器右边界 1468.6），那是 **VLG 布局跑之前的模板位**（同 `查证:103` 那条）。
        //    容器右边界 1468.6 与 `WIldcard Counter` 的左边界 1470 **正好相接** ⇒ 头部是一行：
        //    [Filters 圆钮][容器：Clear filters **右对齐**][Wildcard Counter][Army Icon]。
        //    ⇒ 取 `1468.6 − 250 = 1218.6`（右对齐）。照抄 1488.6 会**压在四个稀有度计数上**（自检抓过）。
        const float HdrClearX = 1218.6f, HdrClearY = 83.5f, HdrClearW = 250f, HdrClearH = 60f;
        const float HdrSepY = 151f, HdrSepH = 10f;
        const float WcBgX = 1550f, WcBgY = 91.5f, WcBgW = 320f, WcBgH = 44f;
        // `WIldcard Counter`（注意原版拼写就是 `WIldcard`）：`Counters` 是个 HLG（pad L/R 10 · spacing 5 · UpperLeft）
        // ⇒ 4 槽宽 70、槽 x = 1565+75i、**行带 y = [91.5,135.5]**（高 44）。
        // 每条 2 个子件：**`Icon`(30 宽) 在左 + `Counter`(41 宽) 紧随其后、同一水平带**（正本 `卡组编辑界面_查证_0920.md` §③）。
        // 🔴 2026-09-23 修：原来图标 y 抄了**外层容器顶边 71**（高 20.5）、数字被摆成**图标正下方**且字号只有 1/3。
        const float WcIconY = 91.5f, WcIconW = 30f, WcIconH = 44f;
        const float WcCntW = 41f, WcCntFontPx = 32.6f;      // 字号 = 原版 TMP 的 `m_fontSize`（autosize 10~38）
        static readonly float[] WcIconX = { 1565f, 1640f, 1715f, 1790f };
        const float ArmyIconX = 1470f, ArmyIconY = 71f, ArmyIconW = 80f, ArmyIconH = 85f;

        // ---- Sidebar（R:33-56）----
        const float SideBgX = -203f, SideBgY = 156f, SideBgW = 538.5f, SideBgH = 924.1f;
        const float TabsX = 0.3f, TabsY = 156f, TabsW = 326.9f, TabsH = 150f;
        /// <summary>一个页签格 = `TabsW / 3` = **108.9667**（原版三个节点的实测 rect：
        /// `Cards [0.3,109.2]` · `Info [109.2,218.2]` · `Cosmetics [218.2,327.1]` —— **各自整格**）。
        /// 三个子件（`Highlight` / `Icon` / `Label` 的父级几何）都按这一格算：
        ///   · `Highlight` = **整格**（图 `40k_main_bt_selected BW`，`Sliced` + `ppuMul 0.92` ⇒ 九宫格）；
        ///   · `Icon` = **整格** + `m_PreserveAspect = 1`，源图 126×126 ⇒ **实绘 108.9667²、居中**
        ///     （🔴 A41 ①：我们原来画 **100×100**，小 8.97px ≈ 9% —— 那条理由「rect 是 0×0」
        ///      量的是**布局跑之前的模板位**，见文件头 ② 那条订正）；
        ///   · `Label`（名牌）= 见下面那组 `TabName*`。</summary>
        static readonly float TabCellW = TabsW / 3f;                       // 108.96667
        /// <summary>页签名牌（原版 `Cards/Label`，Image = `40k_main_bt_nametag` 109×41 Simple、
        /// **无 `preserveAspect`** ⇒ **拉伸**进这个矩形）。
        /// 判据 = dump 的 `Label [5.3,261]–[104.2,301]` = **98.96×40**（三个页签各 +108.97 一格：
        /// `114.2` / `223.2` 逐条对上）⇒ 相对格的左缘 = `5.3 − 0.3` = **5.0**。</summary>
        const float TabNameDx = 5f, TabNameY = 261f, TabNameW = 98.96f, TabNameH = 40f;
        /// <summary>名牌上那行字的字号 = 原版 `Text` 的 **`m_fontSizeMax`**（三页签都是 **34**，
        /// `m_fontSizeMin = 10`，`m_enableAutoSizing = 1`）。
        /// ⚠️ dump 印的 `字号=34 / 31.5 / 28.15` 是**自适应的结果** —— 三颗的 `m_fontSizeBase` **各不同**
        /// （**Cards 26 · Deck info 24 · Cosmetics 24**，逐颗实读 prefab；原来这里写成「只有 26」，
        /// 2026-10-04 R-W4 订正）——
        /// TMP 会往框塞得下的最大号涨（`TextMeshPro.cs:4139` 那段「increase font size to fill text container」），
        /// 所以三个页签的**输入**其实是同一对 `[10,34]`、差别是**文案长度**算出来的（铁律 5·c）。
        /// ⇒ 我们照**机制**做：`SetGlyphHeight(34px)` + `SetAutoFitBox(…, 10, 34)`。
        /// 🔴 **2026-10-05 就地订正**：这里原来写「（`SetAutoFitBox` 取 max 就是从它来）」—— **那是旧语义**。
        ///    现在 `SetAutoFitBox` 内部自己把原版的 `m_fontSizeMax` 折成 TMP 的 `fontSize` 单位：
        ///    `fontSizeMax = cur × maxPx / NominalPx()`（`Battle/Label.cs:372`）⇒ **上限 ≠ 传进去的字号**，
        ///    两者只在 `maxPx == 调用方那个 px` 时相等（本件正是这一档）。
        /// ⚠️ **换字体会改结果**：我们全工程用的是 `Fonts/NotoSerifCJK-Regular SDF`（`Core/TmpFont.cs:26`），
        ///    它的拉丁字母比原版那套窄体宽 ⇒ 同一句 `Deck info` / `Cosmetics` 自适应出来会**比原版那几个
        ///    冻结值（31.5 / 28.15）小**。这是**字体替换**的后果，不是版式错 —— 真要逐像素对上得连字体一起换
        ///    （全工程的事，不在本轮）。断言盯的是**机制**（要么停在 `34`、要么被压到框的边界上 ——
        ///    F3 换掉了原来那条同义反复的区间断言），**不钉**自适应出来的那个数。</summary>
        const float TabNameMaxPx = 34f, TabNameMinPx = 10f;
        /// <summary>页签高亮**画出来的角块** = 原版 `m_Border 30` ÷ `m_PixelsPerUnitMultiplier 0.92`
        /// = **32.6087px**（判据/同一条结论 → `Shell/MenuWindowBase.cs:430`，那边同一张图同一个键；
        /// 2026-10-04 订正：原来引的 `:414-418` 是 `BuildTabButton` 的签名/开头，不是这条结论）。
        /// ⚠️ **本仓口径**：`borderOutPx = border ÷ (spritePPU/refPPU × ppuMul)`，本工程的 `Sprite` 都是
        /// 100/100 ⇒ **直接 `÷ ppuMul`**（同 `Shell/DuelPopupWindow.cs:127-129`、`MenuWindowBase.cs:430`）。</summary>
        const float TabHiCorner = 30f / 0.92f;
        const float NameX = 9.5f, NameY = 311f, NameW = 307.7f, NameH = 50f;
        const float NameTxX = 19.5f, NameTxY = 318f, NameTxW = 287.7f, NameTxH = 37f;
        const float NameClrX = 277.2f, NameClrY = 316f, NameClrW = 35f, NameClrH = 40f;

        // ---- Deck Details：卡组列表（R:57-96）----
        const float ListX = 0.4f, ListY = 366f, ListW = 325f, ListH = 644.1f;
        const float RowW = 325f, RowH = 55.7f;
        const float RowPitch = 56f;                       // ⚠️ 我们挑的（见文件头 ③）
        const float RowCostX = 17f, RowCostS = 38f;
        const int RowVisible = 11;                        // 644.1 / 55.7 = 11.56 ⇒ 11 行整
        /// <summary>稀有度色条**只占行右侧那一段**（原版锚 `0.606 → 1.0`），不是整行。
        /// 🔴 2026-09-23（第 12 条 第 5 项 ③）：原来铺满整行 325。**两处位置（建的时候 + `MoveRow` 滚动时）
        /// 必须都用这一对常量** —— 一处写成 `ListX + RowW/2` 的话，滚动一下色条就跳到整行中心（自检抓过）。</summary>
        const float GradFrac = 0.606f;
        static readonly float GradX = ListX + RowW * GradFrac, GradW = RowW * (1f - GradFrac);

        // ---- 费用曲线（R:97-153；原版在 Deck info 页签下）----
        const float CurveX = 56.1f, CurveY = 411f, CurveRowH = 18.9f, CurveStep = 22.3f;
        const float CurveBarX = 92.2f, CurveBarW = 151.3f;

        // ---- Footer（R:159-166）----
        const float DoneX = 13f, DoneY = 1020.5f, DoneW = 188.5f, DoneH = 50.2f;
        const float DoneHlX = 67.2f, DoneHlY = 933.5f, DoneHlW = 81.4f, DoneHlH = 223.2f;
        const float CntX = 251.6f, CntY = 1020f, CntW = 75f, CntH = 50f;
        const float FootIcX = 201.6f, FootIcY = 1025f, FootIcW = 50f, FootIcH = 40f;

        // ---- Card Display 卡池（R:167-170）----
        // 🔴 **2026-09-23 按定案改**（`项目任务.md` §三 第 12 条 **第 6 项**）：
        //    **原版默认那一套** = 视口 **1589.8×924.1** · **6 列** · 格 **262.5×384** · **间距 0** · **贴左起排**
        //    ⇒ 一屏 **6 × 2.4**（两行满 + 第三行露头）。出处（真货）：
        //    `decomp_full/PolyAndCode.UI.VerticalRecyclingSystem__CreateCellPool.c` L127-167 ——
        //    `_coloums = floor(viewport.rect.width ÷ (_cellWidth + _spacingX))`，**用 `_cellWidth` 原值、不乘任何系数**。
        // ⚠️ **原版还有另一套**（`GameStaticData.smallScreenUI = true` 时格尺寸 ×`_mobileSizeScale`，
        //    本界面是 1.5 ⇒ **393.75×576 · 4 列**）—— **我们没实现**：单机没有「小屏 UI」这个开关。
        //    硬证据（私有 `Initialize()` 的指令流，RVA `0x89F460`）与两套的对照表都在
        //    `项目任务.md` §三 第 12 条 第 6 项 ⇒ **将来加那个设置时，两套都要摆出来（状态 → 参数）**。
        const float PoolX = 330.2f, PoolY = 156f, PoolW = 1589.8f, PoolH = 924.1f;
        const float CellW = 262.5f, CellH = 384f;          // 原版**卡位**（不是 `Collection Card` 那张图的 350×512）
        const int PoolCols = 6, PoolRows = 3;              // 一屏 6×2.4 ⇒ 摆 3 行（第 3 行只露头）
        const float PoolSpacing = 0f;                      // 原版 `_spacingX/_spacingY` 都是 0

        // ---- 🆕 2026-09-28：卡位里**底下那条「张数」**（原版 `Collection Card/Content/Counter`）----
        // 出处：`bundle_menus_assets_all/RectTransform/RectTransform_9015809549177451864.json`
        //   `m_AnchorMin = (0.2857143, 0.002)` / `m_AnchorMax = (0.7142857, 0.09966714)` ·
        //   `sizeDelta = (0,0)` · `pivot = (0.5, 0)` ⇒ 相对 262.5×384 的卡位：
        //   **x 75.0…187.5（宽 112.5）· 距格底 0.768…38.272（高 37.504）**，换成左上原点 = y 345.728…383.232。
        // 🔴 **卡池里那张卡的高度 = CellH − 这条 = 345.728，不是 384。**
        //   实拍印证（桌面《卡组编辑界面参考.png》竖剖量亮带）：卡框 **331.5** ≈ 0.9777（= 卡框 814.25 / 卡体 832.825）
        //   × 345.73；而且**卡框下沿在张数条之上**（原来我们按 384 画 ⇒ 卡顶满格子、那一整条会把卡底压住）。
        const float PoolCounterH = 38.272f;
        const float PoolCardH = CellH - PoolCounterH;      // 345.728
        const float PoolBarX1 = 75f, PoolBarX2 = 187.5f;   // 底图框（宽 112.5，格内水平居中）
        const float PoolCntY1 = 358.29f, PoolCntY2 = 380.98f;   // 字框（高 22.69，居中于底图框）
        /// <summary>那条底图 = `40K_main_deck_card counter`（116×36 · **无九宫格** · 原版 `m_PreserveAspect = 1`
        /// ⇒ 内接进 112.5×37.5 后**实绘 112.5×34.91**）。
        /// 🔴 **不是** `40k_CardAmount_bar_bg` / `_fill` —— 那两张是 **Slider 型**（费用曲线那一批），
        ///   遍历 `Collection Card` 全子树证实卡位上没有它们（判据文件原来指错了图，2026-09-28 已订正）。</summary>
        const string PoolCounterSprite = "40K_main_deck_card_counter";
        /// <summary>那行字的字号 —— 原版 `m_fontSize = 31.9` · **autosize 7…32** · `Center/Midline` · 白。
        /// ⚠️ 字框只有 22.69 高 ⇒ 原版运行时会被 autosize 压小（我们走 `SetAutoFitBox`，同一条路）。</summary>
        const float PoolCounterPx = 31.9f;

        /// <summary>**贴左但整体居中**：内容宽 = 6×262.5 = 1575 < 视口 1589.8 ⇒ 两侧各留 7.4。
        /// 出处（2026-09-23 子代理复核）：`RecyclableScrollRect` 的居中量常量 `0x1834b2bb4 = 0.5f`
        /// ⇒ 首格左边缘 = 330.2 + 7.4 = **337.6**、整个内容右边缘 1912.6 ✓。
        /// ⚠️ 别写成「贴左起排」（=330.2）—— 那差 7.4px，而且**看着像对的**。</summary>
        static readonly float PoolPadX = (PoolW - PoolCols * CellW) * 0.5f;
        const float PoolPadY = 0f;
        const float PoolRowPitch = CellH + PoolSpacing;    // 行距 = 格高
        /// <summary>卡池里那张卡缩到多大 —— 按**卡位高减去底下张数条**（`PoolCardH` = 345.728）反解
        /// （我们的卡 `CardView.Height 3.3313 × 108 = 359.7 px` 是**卡图**，不是卡位）。
        /// ⚠️ 原版卡位 262.5×384 的宽高比与我们的卡不是同一个 ⇒ 取「**高度对齐**」：
        /// 缩到 345.728 后宽度约 **217.2** &lt; 262.5 ✓ 放得下。
        /// 🔴 原来按 **384** 反解（卡顶满格子）—— 2026-09-28 订正，理由见 `PoolCounterH` 上面那段。</summary>
        const float PoolCardScale = PoolCardH / (CardView.Height * PxPerUnit);

        // ---- Card Filters 筛选栏（**在左**，R:174-217）----
        const float FltX = 2.2f, FltY = 156f, FltW = 331.7f, FltH = 924.1f;
        /// <summary>🆕 2026-10-04（§三第29条 **A67**）：抽屉收起时**左移多少 px**（= 原版那段位移的**行程**）。
        /// 🔴 **不是 −550**：原版 `CollectionFilterController<T>`（本窗那份就挂在 `Card Filters` /
        ///   `Cosmetic FIlter` 上）收起的 x = `hiddenPosition.x` = **−550**、展开 x = `originalAnchorPosition.x`
        ///   = **−165** —— 两个都是**父系里的 `anchoredPosition`**（绝对锚点值）⇒ **行程 = 两者之差 = −385**。
        ///   我们的面板按**屏幕绝对 px** 摆（原位 2.2..333.9，原版那棵展开位 2.18..333.90 —— 差 1.9px）
        ///   ⇒ 同一段行程可以直接搬（同 `Shell/CollectionWindow.FltHiddenDx`，判据也同一份）。
        /// ⚠️ **残余 ±2px（如实记）**：那两个端点来自**另一个抽屉实例**（宽 331.72 vs 我们 335.50/331.7）；
        ///   「整栏滑出屏幕」这个语义与这 2px 无关。
        /// 📌 **逐实例核过**（2026-10-04）：`bundle_menus_assets_all/MonoBehaviour/` 里 6 个带
        ///   `hiddenPosition` 的实例**全是 (−550, 0) / `animationTime` 0.3**，其中**就有本窗这棵**
        ///   （`Deck Editing Menu` 全树里 `Card Filters [2,156 332x924]` 与 `Cosmetic FIlter (inactive)`，
        ///   见 `资料/说明书/04_界面UI/菜单全树.md` 的 `Deck Editing Menu` 段）。
        ///   判据全文 → `资料/待办判据_卡面卡池与双语.md` §四那条操作链 + `资料/卡组编辑界面_查证_0920.md:433-434`。</summary>
        const float FltHiddenDx = -385f;
        /// <summary>原版 `animationTime` = **0.3 秒**（逐实例实读，同上）—— 位移与时长都对得上原版。</summary>
        const float FltAnimTime = 0.3f;
        // 🔴 **七行内部不再由我们挑参数**（原来那两个 `FltPad = 10` / `FltRowH = 42` 是「我们挑的」）——
        //    行顶 / 格尺寸 / 选项表全在 `Core/FilterPanelModel.cs`（与收藏窗**共用一份**，
        //    出处 = `资料/普查产出_0923/A3_Cards页.md` §5·1 实读）。2026-09-28 抽走。
        //    内容高 989.02 > 可见高 924.1 ⇒ **可滚 64.92**（原版是 Scroll View + VerticalLayoutGroup）。

        // ---- 分层：**用渲染队列，不用 z** ----
        // 🔴 踩过的坑（2026-09-20）：透明队列里 Unity 是按**到相机的 3D 距离**排序的，
        //    而我们的图铺满整屏 ⇒ **屏幕中间那张（离相机近）会盖住屏幕边缘那张**，哪怕它的 z 更远。
        //    实测：侧栏底板（中心 x≈-5.3）盖住了整个卡组列表（x≈-7.4）—— 卡组行一个都看不见。
        //    ⇒ 改用 `SetRenderQueue`（每个 quad 一份独立材质，改队列是安全的）。
        //    ⚠️ 同一条队列里仍然只放**互不重叠**的东西（自检有一条「同一层不许压住」的断言）。
        // 🔴 **2026-10-04（A41 ③）加这一档**：页头那条**分隔线**单开一层、放在**最下面**。
        //    它跟**侧栏底板**、跟**页签高亮**在 y 156..161 那 5px 上是**真重叠**的
        //    （原版 rect 也重叠：`Separator Line [167.2,151]–[1920,161]` × `Cosmetics [218.2,156]–[327.1,306]`，
        //      逐对算：与 `Info [109.2,218.2]` 重叠 **51×5px**、与 `Cosmetics [218.2,327.1]` 重叠 **108.9×5px**。
        //      🔴 **2026-10-04 订正（R-W4 F7a）**：这里原来写「与 `Cosmetics` 重叠 160×5px」——
        //      160 是**整块 `Buttons`**（`327.1 − 167.2`）与分隔线的重叠，不是 `Cosmetics` 那一格）。
        //    原版靠**兄弟序**分先后 —— `Content Area` 的孩子是 `Background → Header → Sidebar → …`
        //    ⇒ **侧栏及其页签压在分隔线之上**。我们原来两者同在 `QPanel`（谁盖谁不定；而且
        //    `QPanel > QSide` ⇒ 分隔线**压在侧栏底板上**，与原版正好相反）。
        //    ⇒ 照原版次序把分隔线放到底下。⚠️ 这正是「同一层不许压住」那条判据（A41 ③ 修好单位后）
        //      抓出来的**真重叠**，不是误报。
        const int QSep = 2999;       // 页头分隔线（**最低层**：原版它在 Header 里，被 Sidebar 压住）
        const int QSide = 3000;      // 侧栏底板
        const int QDoneHl = 3001;    // Done 的外发光（原版它纵跨到卡组列表区，单独一层）
        // 🔴 **2026-09-23 重排**（第 12 条 第 5 项）：原版卡组行的兄弟序是
        //    **`Background` → `Rarity Gradient` → `Border`**（行底最下）；我们原来是
        //    `QGrad(色条) < QPanel(行底) < QBorder` ⇒ **行底盖住色条**，与原来反了。
        //    ⇒ 给行底单开一层（`QRowBg`），其余各层顺移 +1（相对次序一律不变）。
        const int QRowBg = 3002;     // 卡组行的**行底**（原版 `40k_deck_cardlist_bg`，九宫格）
        const int QGrad = 3003;      // 行的稀有度色条 / 空卡组提示（**压在行底之上**）
        const int QPanel = 3004;     // 输入框底 / 曲线槽 / 计数器底（⚠️ 分隔线 2026-10-04 起搬去 `QSep`，不再用这一档）
        const int QBorder = 3005;    // 行描边 / 小图标
        const int QPoolInfo = 3006;  // 卡池读数（⚠️ 我们自己加的，原版没有）
        const int QRow = 3007;       // 按钮底 / 费用圆
        // 🔴 **2026-10-04（A41 ②）加这一档**：页签那个名牌底板（原版 `Cards/Label`，98.96×40 落在
        //    y 261..301）与**图标**（整格 108.97² 居中 ⇒ 底边 285.5）**在纵向上真重叠 24.5px**，
        //    而原版兄弟序是 `Highlight → Icon → Label`（名牌**在后 = 压在图标上**）。
        //    ⇒ 两者**必须分属两层**（同层会撞上「同一层不许压住」那条断言、且谁盖谁不定）。
        //    这里夹在 `QRow`（图标）与 `QText`（名牌上那行字）之间。
        const int QTabName = 3008;   // 页签名牌底板
        const int QText = 3009;      // 文字（⚠️ 从 3008 顺移一档 —— 本文件里 **没有任何 `ImageQuad` 用旧值**，
                                     //    只有 TMP 用它，见 `Img(...)` 的调用点；上面那条断言才不受影响）
        // 🆕 2026-09-28：卡池每格底下那条「张数」（原版 `Collection Card/Content/Counter`）——
        // **必须压在卡之上**：卡池里卡与卡不重叠（所以卡自己仍用 `CardView` 默认的 3000），
        // 但这一条落在**格子的下沿、会压到卡底下那一段**，队列比 3000 大才画得出来。
        const int QPoolBar = 3010, QPoolBarText = 3011;
        const int QFlt = 3020;       // 筛选栏（盖住侧栏 ⇒ 队列更大 = 更后画）
        const int QFltRow = 3021, QFltText = 3023;
        // 🔴 **2026-09-28 加这一档**：搜索框的**尾图标**与输入框底图**故意重叠**（原版就是这样），
        //    同一队列里只能靠距离排 ⇒ 谁盖谁不定（实测：图标被底图盖住，截图才看出来）。单独给一档。
        const int QFltIconTop = 3022;
        // 导入弹窗是**模态**，压在一切之上（`CardDisplayWindow` 那套也在 3000 段，所以留足余量）
        const int QModal = 3100, QModalRow = 3101, QModalText = 3102;

        // ============================================================ 状态
        public DeckEditorState State { get; private set; }
        public DeckLibrary Library { get; private set; }
        public Transform Root { get; private set; }

        Camera _cam;
        int _tab;                       // 0 = Cards · 1 = Deck info · 2 = Cosmetics
        bool _filtersOpen;
        float _poolScroll, _deckScroll, _fltScroll;
        /// <summary>正在编辑的文本缓冲（`null` = 没在编辑）。<see cref="_editKind"/> 说明它在改什么。</summary>
        string _nameEdit;
        /// <summary>0 = 没在编辑 · 1 = 改**卡组名**（原版 `DeckEditingPanel.ChangeName`）· 2 = 改**卡名筛选**（原版 `CardNameFilter`）。</summary>
        int _editKind;

        // 导入卡组弹窗（原版 `ImportDeckPopup`，坐标 = `资料/说明书/04_界面UI/卡组界面说明书.md` §五）
        bool _importOpen;
        string _importText = "";
        string _importError = "";
        readonly HashSet<string> _missingArt = new HashSet<string>();

        readonly List<CardView> _poolViews = new List<CardView>();
        readonly List<int> _poolIndex = new List<int>();

        // 行底 / 行描边是**九宫格**（`ImageQuad.CreateNineSlice` 建出来的是**一棵小树**）⇒ 存**根节点**
        readonly List<GameObject> _deckRowBg = new List<GameObject>();
        readonly List<ImageQuad> _deckRowGrad = new List<ImageQuad>();
        readonly List<GameObject> _deckRowBorder = new List<GameObject>();
        readonly List<ImageQuad> _deckRowCntIc = new List<ImageQuad>();
        readonly List<Label> _deckRowName = new List<Label>();
        readonly List<Label> _deckRowCount = new List<Label>();
        readonly List<ImageQuad> _deckRowCost = new List<ImageQuad>();

        readonly List<Label> _tabLabel = new List<Label>();
        /// <summary>页签高亮那一层 —— 🔴 **2026-10-04（A41 ⑥）起它是【九宫格】**（原版
        /// `Image.Type = Sliced` + `m_Border (30,30,30,30)` + `m_PixelsPerUnitMultiplier = 0.92`），
        /// 一棵树里 **9 块** ⇒ 开关/上色一律走 `_tabHiRoot[i]` 那棵**根**，`_tabHi[i]` 只是
        /// **树里第一块**（自检读数用；只给一块上色会**留下另外 8 块**，静默）。</summary>
        readonly List<GameObject> _tabHiRoot = new List<GameObject>();
        readonly List<ImageQuad> _tabHi = new List<ImageQuad>();
        readonly List<ImageQuad> _tabIcon = new List<ImageQuad>();

        readonly List<ImageQuad> _curveFill = new List<ImageQuad>();
        readonly List<Label> _curveLabel = new List<Label>();
        readonly List<ImageQuad> _curveBar = new List<ImageQuad>();
        readonly List<GameObject> _infoOnly = new List<GameObject>();   // 只在 Deck info 页签显示的东西

        /// <summary>可点的矩形（px 坐标）。命中判定一律走它 —— 比拿 quad 反推尺寸稳。</summary>
        struct Btn { public string Key; public float X, Y, W, H; }
        readonly List<Btn> _btns = new List<Btn>();

        Label _title, _notice, _storeErr, _deckNameText, _deckNameHint, _counterTxt, _verdict, _poolInfo;
        readonly Label[] _wcTxt = new Label[4];
        ImageQuad _emptyWarn, _doneHl;
        /// <summary>页头那颗 `Filters` 圆钮 —— 它要**按状态换图**（面板开=`40k_menu_bt_pressed`、关=`40k_menu_bt`），
        /// 见 `RefreshHeader` 里那段判据（原版 `EverguildToggle.changeSpriteOnValueChange=1`）。</summary>
        ImageQuad _hdrFltBtn;
        GameObject _emptyWarnGo;
        string _noticeText = "";
        float _noticeUntil;

        // 拖拽状态（把卡组条目拖出侧栏 = 删除，照原版）
        GameObject _dragQuad;      // 行底现在是**九宫格根**（整棵树一起跟着鼠标走）
        Label _dragText;
        int _dragRow = -1;
        bool _dragging, _draggingMoved;
        Vector3 _dragOrigin;
        /// <summary>指针现在压在卡组列表的第几行（`-1` = 没有）。🆕 A24：行悬停色靠它 —— 见 `ApplyRowHover`。</summary>
        int _hoverRow = -1;

        // ---- 🆕 2026-10-04（A24）悬停派发的状态 ----
        /// <summary>`_btns` 的 key → 那一颗接了**悬停**的按钮（`trans=2` 换图那 5 颗 + `trans=1` 变色那两颗
        /// `hdr_filters` / `name_box`）。抽屉里那几格**不在这份表里** —— 见下面两份。</summary>
        readonly Dictionary<string, WindowButton> _hoverBtns = new Dictionary<string, WindowButton>();
        /// <summary>🆕 2026-10-05（A32②）**卡牌筛选栏**里接了悬停变色的那几格（key = `_fltHit` 的 key：
        /// `$name` 搜索框 · `$owned` / `$upgradable` 两个开关）。⚠️ 与 `_cosmoHoverBtns` **必须分开**：
        /// 两栏可以同时存在（`_filtersOpen` × `_tab == 2`），而两边都有 `$owned` ⇒ 合成一份会互相盖掉。</summary>
        readonly Dictionary<string, WindowButton> _fltHoverBtns = new Dictionary<string, WindowButton>();
        /// <summary>🆕 同上 —— **卡背抽屉**那两行（只有 `$owned` 一颗接了，原版 `Card Filters` 那套选项
        /// 卡背页没有）。</summary>
        readonly Dictionary<string, WindowButton> _cosmoHoverBtns = new Dictionary<string, WindowButton>();
        /// <summary>指针此刻悬停的那一颗（没有 = null）。</summary>
        WindowButton _hoverBtn;
        /// <summary>左键正压着的那一颗（抬起时要还原；没有 = null）。</summary>
        WindowButton _pressedBtn;

        // ============================================================ 生命周期

        void Awake() { Instance = this; }

        void Start()
        {
            // 按 Play 的入口 —— 和自检**必须是同一条路**（`CardPresentation` 的规矩：
            // 开局写在 `Start()` 里 = 批处理下永远验不到，所以抽成公开的 `Build` 让两边都调它）
            if (State == null) Build(DeckLibrary.Load());
        }

        void Update()
        {
            // 🆕 A67：先推抽屉动画（摆位置 / 开关命中）**再**处理指针 ——
            //   同一帧里点下去吃到的是**这一帧**的命中口径（顺序反了会慢一帧）。
            TickDrawers(Time.deltaTime);
            HandlePointer();
            HandleScroll();
            HandleTyping();
            TickTooltip();
            if (_notice != null && _noticeText.Length > 0 && Time.unscaledTime > _noticeUntil)
            {
                _noticeText = "";
                _notice.SetText("");
            }
        }

        /// <summary>悬停信息层。原版那套触发器挂在**卡面数值容器**上（`Health/Range Attack/Melee Attack/Cost Container`），
        /// 卡池里的卡也是同一批 prefab ⇒ 卡组编辑这边一样有。
        /// ⚠️ **护甲那个容器原版就没有 tooltip**（`子代理读报_2dcard_0827.md:95`）⇒ 这里也不给。</summary>
        void TickTooltip()
        {
            var mouse = Mouse.current;
            if (mouse == null) { Tooltip.Hide(); return; }
            if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame) { Tooltip.Hide(); return; }
            TickTooltipAt(LayoutSpace.ScreenToWorld(mouse.position.ReadValue(), _cam));
        }

        /// <summary>自检入口：批处理没有鼠标 ⇒ 直接喂世界坐标（**和鼠标那条路同一个函数**）。</summary>
        public bool TickTooltipAt(Vector3 wp)
        {
            for (int i = 0; i < _poolViews.Count; i++)
            {
                var v = _poolViews[i];
                if (v == null || !v.gameObject.activeSelf) continue;
                int s = v.StatAt(wp);
                if (s == CardView.StatNone || s == CardView.StatArmour) continue;
                Vector3 at = v.StatWorld(s);
                switch (s)
                {
                    case CardView.StatMelee:  Tooltip.Show(TipText.Melee,  at, 15, new Vector3(-49.33f / 108f, 0f, 0f)); return true;
                    case CardView.StatRanged: Tooltip.Show(TipText.Ranged, at, 15, new Vector3(-53.54f / 108f, 0f, 0f)); return true;
                    case CardView.StatHealth: Tooltip.Show(TipText.Health, at, 10, new Vector3( 73.05f / 108f, 0f, 0f)); return true;
                    case CardView.StatCost:   Tooltip.Show(TipText.Cost,   at, 10, new Vector3( 52.60f / 108f, 0f, 0f)); return true;
                }
            }
            Tooltip.Hide();
            return false;
        }

        // ============================================================ 建

        /// <summary>按像素坐标建整个界面。**自检与运行时调同一个**（见 <see cref="Start"/>）。</summary>
        public void Build(DeckLibrary lib)
        {
            Library = lib ?? DeckLibrary.Load();
            _cam = Camera.main;
            if (_cam != null) LayoutSpace.Apply(_cam);

            Root = transform;
            foreach (Transform c in Root) DestroySafe(c.gameObject);
            _btns.Clear(); _missingArt.Clear();
            // 🆕 A24：界面上一次建的 `WindowButton` 跟着子树一起销毁了 ⇒ 这两份登记也要清
            //（不清的话 `UiHoverAt` 会去 `Enter/Exit` 一颗已经销毁的组件 —— 静默无效）
            // 🆕 2026-10-05（A32②）：抽屉那两份登记同理（`_fltHoverBtns` / `_cosmoHoverBtns`）
            _hoverBtns.Clear(); _hoverBtn = null; _pressedBtn = null; _hoverRow = -1;
            _fltHoverBtns.Clear(); _cosmoHoverBtns.Clear();

            // 空库时先替玩家建一套（演示卡组），这样界面一打开就有内容
            if (Library.Count == 0)
            {
                var fresh = NewState();
                Library.Create("我的卡组");
                Library.CommitCurrent(PlayerDeckForDemo(fresh));
            }
            // 🔴 **「从收藏进编辑」的交接**（2026-09-23）：收藏窗点「编辑」时把下标写进
            //    `CollectionData.PendingEditDeck`，这里**开局就读掉并清掉** ⇒ 编辑器直接打开那一套。
            //    （原版在同一扇窗里换页、不需要交接；我们是两个场景 —— 见 `资料/阶段二_卡组线_原版规格.md` §七。）
            if (CollectionData.PendingEditDeck >= 0)
            {
                int want = CollectionData.PendingEditDeck;
                CollectionData.PendingEditDeck = -1;
                if (want < Library.Count)
                {
                    Library.Select(want);
                    Debug.Log("[Deck] 从收藏进来 ⇒ 直接打开第 " + (want + 1) + " 套「" + Library.Current.Name + "」");
                }
                else Debug.Log("[Deck] 交接的下标 " + want + " 越界（共 " + Library.Count + " 套）—— 退回当前那套");
            }
            State = NewState();
            State.LoadDeck(Library.Current);

            BuildHeader();
            BuildSidebar();
            BuildDeckList();
            BuildCurve();
            BuildInfoActions();
            BuildFooter();
            BuildFilters();
            BuildPool();
            BuildCosmeticsPage();
            BuildImportPopup();
            BuildNotice();

            if (_missingArt.Count > 0)
                Debug.LogWarning("[DeckRuntime] 缺 " + _missingArt.Count + " 张原版 UI 图（会画成纯白占位，"
                                 + "**不是静默**）：" + string.Join("、", _missingArt));

            RefreshAll();

            // 🔴 2026-10-04（A24）：**这里【故意】不建 `PointerLayer`** —— 悬停的派发由本类自己的
            //   `HandlePointer`（下面 `UpdateButtonHover`）做，理由有三条，写在这里免得下一个人「顺手补上」：
            //   ① 本窗的指针语义是**有模态状态的**（`_importOpen` 模态 / `_filtersOpen` 左抽屉 / `_dragging`），
            //      而 `PointerLayer` 无状态：导入弹窗开着时它照样会让**弹窗背后**的按钮亮起来
            //      （我们没建原版那块全屏暗底，没有东西挡它）；
            //   ② `PointerLayer` 自己的「点击 / 滚轮 / 拖拽」会和本类那条**并行跑起来** ——
            //      点一下 `ClickLog` 会多写一块（`Shell/PointerLayer.cs:279`，正文还会说
            //      「这个命中区没有绑动作」，而那颗按钮的动作其实走 `_btns` 那条路）⇒ 给真机诊断添噪声；
            //   ③ `Shell/PointerLayer.cs:89-92` 早就写明「**收口不到卡组编辑那一套**，两者语义相同、
            //      **各写一份（明账）**」（同「滚动也是两份」）⇒ 悬停这一份跟着这条既有口径走。
            //   代价（如实记）：本窗**没有** `PointerLayer` 那套「按下越过 10px 判成拖拽」的语义 ——
            //   但本类的拖拽是**行拖出删除**，判据本来就在 `EndDrag` 里，与按钮无关。
        }

        DeckEditorState NewState()
        {
            var s = new DeckEditorState(CardDatabase.Load());
            // 🆕 2026-09-27：**只有卡组编辑这条路**开「按督军分流卡池」
            //   （没督军 ⇒ 只列各阵营的督军；定了 ⇒ 只列该阵营的卡）。规格与理由见 `WarlordGatedPool`。
            s.WarlordGatedPool = true;
            return s;
        }

        // ------------------------------------------------------------ Header

        void BuildHeader()
        {
            // ⚠️ 队列 = `QSep`（**最低层**）—— 它跟侧栏/页签在 y 156..161 上真重叠，
            //    原版靠兄弟序把侧栏盖在它上面（见 `QSep` 那段注释）。
            // 🔴 **2026-10-04（A51 F10）：这张图原版也是【九宫格】，我们原来是单块拉伸。**
            //   判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 7`：
            //   `Separator Line [167.2,151]–[1920,161] 1752.83×10 | 40k_main_line 171×6 九宫80,0,80,0 | Sliced ppuMul=0.75`
            //   （`Sprite/40k_main_line.json`：`m_Border=(80,0,80,0)`、`m_Rect=171×6`；我们的
            //    `Resources/Art/ui_deck/40k_main_line.png` 实测也是 **171×6** ⇒ UV 直接按图宽切就对）。
            //   画出来的端帽 = `80 ÷ 0.75` = **106.6667px**（口径同 `TabHiCorner` 那条：
            //   本工程 sprite 都是 100/100 ⇒ `border ÷ (spritePPU/refPPU × ppuMul)` 就是 `÷ ppuMul`）。
            //   ⚠️ 拉伸的代价：1752.8 ÷ 171 = **10.25 倍**横向拉伸 ⇒ 两端那 ~80px 的端帽被拉成 ~146px。
            //   上下 `m_Border` 都是 0 ⇒ 只有中间那一行 ⇒ **共 3 块**（不是 9 块）。
            NineSlice("hdr_sep", Ui("40k_main_line"), new Vector4(80f, 0f, 80f, 0f),
                      167.2f, HdrSepY, 1752.8f, HdrSepH, QSep,
                      new Vector4(80f / 0.75f, 0f, 80f / 0.75f, 0f));
            // 🆕 2026-10-04（A24）：原版 `Content Area/Header/Close`（文本 'Back'）是 **`SpriteSwap`**
            //   （`trans=2` · `m_TargetGraphic` = **它自己那层 Image** · HL=`UI_Button_Mulligan_hover`
            //    · P=`UI_Button_Mulligan_Pressed`）—— 两张图都在 `Resources/Art/ui_menu/`，`WindowButton`
            //   的两张表**推得出**这两张，不必显式传。逐颗实读见 `资料/普查产出_1003/卡组编辑器_按钮悬停图_普查.md` 块 A。
            Hover("hdr_back",
                  Img("hdr_back", "UI_Button_Mulligan", HdrBackX, HdrBackY, HdrBackW, HdrBackH, QRow),
                  "UI_Button_Mulligan");
            Txt("hdr_back_t", "返回", HdrBackX, HdrBackY, HdrBackW, HdrBackH, 2, Ink, QText);
            Btn_("hdr_back", HdrBackX, HdrBackY, HdrBackW, HdrBackH);

            // Filters 圆钮 + 图标 + 文字（原版这三块是分开的三条 rect）
            _hdrFltBtn = Img("hdr_fltbtn", "40k_menu_bt", HdrFltBtnX, HdrFltBtnY, HdrFltBtnS, HdrFltBtnS, QRow);
            // 🆕 2026-10-05（A32②）：这一颗**两种行为都有**，各接各的（不冲突）：
            //   ① **状态 → 换图**（`changeSpriteOnValueChange`，`RefreshHeader` 里那句 `SetSprite`）；
            //   ② **悬停 → 变色**（`m_Transition = 1(ColorTint)`，目标件 = 它自己那层图，`m_Color=(1,1,1,1)`
            //      ⇒ 悬停**看得见**）。⚠️ `RefreshHeader` 换的是**纹理**、`WindowButton` 变的是**顶点色**
            //      ⇒ 两条路互不覆盖（这正是原版那颗 prefab 的实际行为）。
            HoverTint("hdr_filters", _hdrFltBtn != null ? _hdrFltBtn.gameObject : null);
            Img("hdr_flticon", "40k_bt_icon_search", HdrFltIconX, HdrFltIconY, HdrFltIconS, HdrFltIconS, QBorder);
            Txt("hdr_fltlbl", "Filters", HdrFltLblX, HdrFltLblY, HdrFltLblW, HdrFltLblH, 2, Ink, QText);
            Btn_("hdr_filters", HdrFltBtnX, HdrFltBtnY, HdrFltLblX + HdrFltLblW - HdrFltBtnX, HdrFltLblH);

            // 🆕 A24：原版 `Header/Filters/Generic Simplified UI Button_updated`（文本 'Clear filters'）
            //   也是 `SpriteSwap`（`m_TargetGraphic` = 自己那层 Image，两张高亮图同上）
            Hover("hdr_clear",
                  Img("hdr_clear", "UI_Button_Mulligan", HdrClearX, HdrClearY, HdrClearW, HdrClearH, QRow),
                  "UI_Button_Mulligan");
            Txt("hdr_clear_t", "Clear filters", HdrClearX, HdrClearY, HdrClearW, HdrClearH, 2, Ink, QText);
            Btn_("hdr_clear", HdrClearX, HdrClearY, HdrClearW, HdrClearH);

            // Wildcard Counter：底板 + 四个稀有度图标（30×44）+ 各自的数量
            Img("hdr_wcbg", "40k_topmarquee_currency_display_BW", WcBgX, WcBgY, WcBgW, WcBgH, QPanel);
            string[] wcIc = { "40k_general_wildcard_common_small", "40k_general_wildcard_rare_small",
                              "40k_general_wildcard_epic_small", "40k_general_wildcard_legendary_small" };
            for (int i = 0; i < 4; i++)
            {
                // 🔴 2026-09-27（PA 普查）：原版这 4 个 `.../WIldcard Counter/Counters/*/Icon` 是 PA=1 + Simple，
                //   贴图 42×51 / 41×51 塞进 30×44 ⇒ 原版实绘 **30×36.4（37.3）**，我们原来拉伸成 30×44（高 ×1.18~1.21）。
                //   ⚠️ 同一件在收藏窗（`CollectionWindow.cs:318`）与卡片详情窗（`CardDetailPopup.cs:602`）也是同一错，三处一起修。
                Img("hdr_wc" + i, wcIc[i], WcIconX[i], WcIconY, WcIconW, WcIconH, QRow, true);
                // `Counter` 在图标**右侧同一水平带**（41×44 · 字号 32.6 · 白 · 居中 · NoWrap）
                _wcTxt[i] = TxtPx("hdr_wct" + i, "0", WcIconX[i] + WcIconW, WcIconY, WcCntW, WcIconH,
                                  WcCntFontPx, Ink, QText);
            }
            Img("hdr_army", FactionIcon(null), ArmyIconX, ArmyIconY, ArmyIconW, ArmyIconH, QRow);
        }

        // ------------------------------------------------------------ Sidebar

        void BuildSidebar()
        {
            Img("side_bg", "40k_main_tab_background", SideBgX, SideBgY, SideBgW, SideBgH, QSide);

            // Window Options：三个互斥页签（Cards / Deck info / Cosmetics）
            string[] tabIc = { "40k_collection_bt_cards", "40k_collection_bt_decks", "40k_collection_bt_cosmetics" };
            string[] tabTx = { "Cards", "Deck info", "Cosmetics" };
            for (int i = 0; i < 3; i++)
            {
                float cellX = TabsX + TabCellW * i;                  // 这一格（页签）的左缘
                // 🔴 **2026-10-04（A37 ①）改：`Highlight` 的 rect = 【整格 108.96×150】**，我们原来画 **100×100**
                //   （只有图标那一方块那么大）。判据（自己重跑，不是转抄）：
                //   `python 工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 6` ⇒
                //   `Cards`（`[0.3,156]–[109.2,306]`）的子件 `Highlight` 的绝对矩形 = **`[0.3,156]–[109.2,306]`**
                //   （**与父节点同矩形 = 整格**；`Info` `109.2→218.2`、`Cosmetics` `218.2→327.1` 同理）
                //   · 图 = `40k_main_bt_selected BW 71×71 九宫30,30,30,30` · `Sliced (1,0,0,1)` · `ppuMul=0.92`。
                //   ⚠️ 那条 `Sliced (1,0,0,1)` 印的是**这颗 Image 的 `m_Color` = (r,g,b,a) = 红** ——
                //     `RefreshHeader` 那一段按它给 tint（见那里的注释）。
                //   🔴 **2026-10-04（A41 ⑥）把那半 `Sliced` 也照做了** —— 原来我们**单块拉伸**
                //     （71² 的图拉到 108.97×150 ⇒ 那条 ~30px 的软边被拉成 ~46px）。
                //     画出来的角块 = `30 ÷ 0.92` = **32.6087px**（传 `borderOutPx`），同 `Shell/MenuWindowBase`
                //     那颗同名键早就是九宫格了。`m_Border` 读的是 (30,30,30,30)（四边同值）。
                var hi = NineSlice("tab_hi" + i, Ui("40k_main_bt_selected_BW"), new Vector4(30f, 30f, 30f, 30f),
                                   cellX, TabsY, TabCellW, TabsH, QPanel,
                                   new Vector4(TabHiCorner, TabHiCorner, TabHiCorner, TabHiCorner));
                _tabHiRoot.Add(hi);
                _tabHi.Add(hi != null ? hi.GetComponentInChildren<ImageQuad>(true) : null);
                // 图标（A41 ①）：原版 `Icon` 的 rect = **整格 108.96×150** + `preserveAspect`（源图 126²）
                //   ⇒ 实绘 **108.9667²**、在格子里居中。我们原来是 100×100（小 8.97px），见文件头 ② 那条订正。
                _tabIcon.Add(Img("tab_ic" + i, tabIc[i], cellX, TabsY, TabCellW, TabsH, QRow, true));
                // 名牌底板（A41 ②）：原版 `Label`，98.96×40 @ 格内左边 5.0 / 屏上 y 261。
                Img("tab_nm" + i, "40k_main_bt_nametag",
                    cellX + TabNameDx, TabNameY, TabNameW, TabNameH, QTabName);
                // 名牌上那行字（A41 ②）：原版子件 `Text` —— **与底板同一矩形**、居中、auto 10~34。
                //   ⚠️ 顺序仍然不能反，但**理由变了** —— 🔴 **2026-10-05 就地订正**：原来写
                //     「`SetAutoFitBox` 的 `max` 取的是**那一刻 `SetGlyphHeight` 设的字号**（`Label.cs:315-319`）」
                //     —— **行号与新语义都不成立**。现在是：`SetAutoFitBox` 拿 `cur`（= `SetGlyphHeight`
                //     那一刻定下的字号，`Battle/Label.cs:355`）当**换算基准** `NominalPx()`，
                //     `fontSizeMax = cur × maxPx / NominalPx()`（`Battle/Label.cs:372`）⇒
                //     **没有先 `SetGlyphHeight` 就没有基准**（`cur <= 0f` 会直接早退）⇒ 仍要先 `TxtPx`（内含它）。
                var tx = TxtPx("tab_tx" + i, tabTx[i], cellX + TabNameDx, TabNameY, TabNameW, TabNameH,
                               TabNameMaxPx, Ink, QText);
                if (tx != null) tx.SetAutoFitBox(U(TabNameW), U(TabNameH), TabNameMinPx, TabNameMaxPx);
                _tabLabel.Add(tx);
                // 点击区 = **整格**。判据（2026-10-04 R-W4 订正：只写「Toggle 挂在 `Cards` 节点上」**不够**）：
                //   · `Cards` 节点**自己那颗 `Image`**：`m_RaycastTarget=1` 但 **`m_Enabled=0`**
                //     ⇒ uGUI 只在组件启用时才参与射线 ⇒ **它不射线**（若只有它，整格就点不到）；
                //   · 真正的射线源 = **子件 `Highlight`**：`m_Enabled=1` · **`m_RaycastTarget=1`**、
                //     它的 RectTransform = **整格 108.96×150**（`Graphic.Raycast` 按 RectTransform 矩形测）
                //     ⇒ 事件上冒给父节点的 `EverguildToggle` = **整格吃射线**；
                //   · `Icon` 是 `m_RaycastTarget=0`（不射线）；`Label` 是 1，但只有 98.96×40（格内一小条）。
                //   ⇒ 面积与我们的 `Btn_`（`cellX, TabsY, TabCellW, TabsH`）**逐值等同**；
                //     原来我们给的是「图标框 + 底下 20px」⇒ 名牌那一横条点不到。
                Btn_("tab_" + i, cellX, TabsY, TabCellW, TabsH);
            }

            // Deck Name（原版是 `EverguildInputField : TMP_InputField`，占位字 'Tap to edit deck name'）
            // 🔴 **2026-10-04（A24）改图**：这颗的底图**画错了**。实读原版
            //   `Deck Editing Menu > … > Sidebar > Window Options > Deck Name` 的 Image：
            //   **`InputFieldBackground`**（Unity 内置 32×32 · **九宫格 (10,10,10,10)** · ppu=200）·
            //   `m_Color = (0.0627,0,0,1)`；同窗 `Card Filters/…/Name FIlter/Input Field` 也是这一张。
            //   我们原来用的是 **`40K_dropdown_bg`** —— 那张是 **`Import Deck Popup` 的输入框**用的
            //   （两处不是同一张图；`40K_dropdown_bg` 仍归导入弹窗，见 `BuildImportPopup`）。
            //   画法与搜索框**同一条**（`BuildFilterFixedParts` 那 6 行）：九宫格 + 同一个 tint
            //   （`FilterPanelModel.InputSprite/InputBorder/InputTint` —— 那一份是共用的唯一出处）。
            var nameTex = Ui(FilterPanelModel.InputSprite);
            if (nameTex != null)
            {
                // 🔴 **2026-10-06（A50③）：这一处收口到公共件 `MenuDraw.Nine`** —— 原来直调
                //   `ImageQuad.CreateNineSlice`（= 绕开公共件的那条路，**拿不到 `clip` / `clipSoftness`**）。
                //   与旧代码**逐项等价**（三样都别改）：
                //    ① **矩形** = `(NameX, NameY) → (NameX+NameW, NameY+NameH)`（旧代码那两个实参就是它的
                //       中心与宽高：`Pos(x+w/2, y+h/2)` / `U(w)` / `U(h)`）；
                //    ② **落位** = `MenuDraw.Local(Root, 矩形)` 与旧代码的 `Pos(px, py)` 给的是**同一个世界点**：
                //       `Root` = 场景根 `DeckEditor`（**出厂在原点、无父**）⇒ `Local` 里那句
                //       `− parent.position` 减的就是零（判据 → `Editor/DeckScene.cs:748-749` 那条不变式）；
                //    ③ **队列 = `QPanel`** · **tint = `FilterPanelModel.InputTint`**（旧代码建完逐块设的就是这两样，
                //       `MenuDraw.Nine` 会替我们设）；`SetTint` 仍**先于**下面的 `HoverTint` 跑完 ——
                //       `Collect()` 抓基准色的时机见下条注释，**这个顺序别动**。
                var nb = MenuDraw.Nine(Root, nameTex,
                    new PxRect(NameX, NameY, NameX + NameW, NameY + NameH),
                    new Vector4(FilterPanelModel.InputBorder, FilterPanelModel.InputBorder,
                                FilterPanelModel.InputBorder, FilterPanelModel.InputBorder), 32f, 32f,
                    QPanel, FilterPanelModel.InputTint, true, "name_bg");
                if (nb != null)
                {
                    foreach (var q in nb.GetComponentsInChildren<ImageQuad>(true))
                    { q.SetTint(FilterPanelModel.InputTint); q.SetRenderQueue(QPanel); }
                    // 🆕 2026-10-05（A32②）：原版这颗 `Deck Name` 是 `EverguildInputField`，
                    //   `m_Transition = 1(ColorTint)` · `m_TargetGraphic` = **它自己那张底图**
                    //   （`InputFieldBackground` · `m_Color = (0.0627,0,0,1)` **不透明**）· `m_Colors` 是 UGUI 默认那组
                    //   ⇒ 悬停把底图乘 **0.9607843**（按下 **0.7843137**），**看得见**。
                    //   ⚠️ 挂在**九宫格的根**上：`WindowButton.Collect()` 收的是**自己子树里全部** `ImageQuad`
                    //     （9 块）⇒ 整颗一起变（只给中心那块上色 = 边框不跟着变）。
                    //   ⚠️ `Collect()` 在**第一次悬停时**才抓基准色 ⇒ 上面那圈 `SetTint(InputTint)` 必须**先**跑完
                    //     （顺序反了会把「基准色」抓成白，于是第一次悬停反而**变亮**）。
                    HoverTint("name_box", nb);
                }
            }
            _deckNameText = Txt("name_t", "", NameTxX, NameTxY, NameTxW, NameTxH, 2, Ink, QText);
            _deckNameHint = Txt("name_h", "Tap to edit deck name", NameTxX, NameTxY, NameTxW, NameTxH, 2,
                                new Color(1f, 1f, 1f, 0.42f), QText);
            Img("name_clear", "40k_icon_search", NameClrX, NameClrY, NameClrW, NameClrH, QBorder);
            Btn_("name_box", NameX, NameY, NameW, NameH);
            Btn_("name_clear", NameClrX, NameClrY, NameClrW, NameClrH);
        }

        // ------------------------------------------------------------ 卡组列表

        void BuildDeckList()
        {
            // 🔴 **2026-09-23（第 12 条 第 5 项）**：行底原来用的是 `UI_Card_name_background_normal_BW` ——
            //    那张是 **462×62、border 全 0**，原版里**只出现在两处、都是 `alpha=0` 的按钮根图**
            //    （`deck_editing_raw.txt:62,219`）⇒ **它本来是全透明的**，我们却把它当成了可见行底。
            //    原版行底 = **`40k_deck_cardlist_bg`**（**九宫格** `m_Border=(150,0,150,0)`）。
            var rowTex = Ui("40k_deck_cardlist_bg");
            var gradTex = Ui("40k_deck_cardlist_bg_rarityColorGradient");
            var borderTex = Ui("40k_deck_cardlist_border");   // 11×11，原版**九宫格** 5/5/5/5（我们原来拉满）
            // ⚠️ `Card Frame Cost Icon` 的**文件名是下划线版**（`sync_battle_ui_art.py` 把空格换成 `_`）
            var costTex = Ui("Card_Frame_Cost_Icon");
            var cntTex = Ui("40K_main_deck_card_counter");
            // 稀有度色条**只占右侧那一段**（原版锚 0.606→1.0）—— 位置常量在类级（`GradX/GradW`），
            //   **`MoveRow` 也用同一对**（别在任一处写 `ListX + RowW/2`）

            for (int i = 0; i < RowVisible; i++)
            {
                float y = ListY + i * RowPitch;
                // 层序照原版兄弟序：**行底(最小) → 色条 → 描边**（每个九宫格自己那 9 块同队列、互不重叠）
                _deckRowBg.Add(NineSlice("row_" + i, rowTex, new Vector4(150f, 0f, 150f, 0f),
                                         ListX, y, RowW, RowH, QRowBg));
                _deckRowGrad.Add(Img("row_g" + i, gradTex, GradX, y, GradW, RowH, QGrad));
                _deckRowBorder.Add(NineSlice("row_b" + i, borderTex, new Vector4(5f, 5f, 5f, 5f),
                                             ListX, y, RowW, RowH, QBorder));
                _deckRowCost.Add(Img("row_c" + i, costTex, RowCostX, y + (RowH - RowCostS) * 0.5f, RowCostS, RowCostS, QRow));
                _deckRowCntIc.Add(Img("row_k" + i, cntTex, 258f, y + 8f, 40f, RowH - 16f, QRow));
                _deckRowName.Add(Txt("row_n" + i, "", 62f, y, 190f, RowH, 1, Ink, QText));
                _deckRowCount.Add(Txt("row_cnt" + i, "", RowCostX, y + (RowH - RowCostS) * 0.5f, RowCostS, RowCostS, 1, Ink, QText));
            }

            // Empty Warning（原版 inactive，空卡组时才显示）
            // ⚠️ **虚线底 `40k_deck_cardlist_doted_bg` 是【空槽行】用的**（原版 67×54 灰 0.44 + `-- Warlord --`）
            //    —— 我们暂时仍拿它当「空卡组提示」的底（**这一处与原版不同**，已记在正本「查不到的」里）。
            _emptyWarn = Img("empty_warn", "40k_deck_cardlist_doted_bg", 20.3f, 445f, 295.3f, 186f, QGrad);
            var warnLabel = Txt("empty_warn_l", "把卡拖到这里", 20.3f, 500f, 295.3f, 60f, 2,
                                new Color(1f, 1f, 1f, 0.65f), QText);
            if (warnLabel != null) _emptyWarnGo = warnLabel.gameObject;
        }

        /// <summary>建一个**九宫格**并把 9 块都推到同一个渲染队列。
        /// ⚠️ `ImageQuad.CreateNineSlice` 自己**不设队列**（2026-10-04 订正：原来引的 `:195` 是本批加行之后
        /// 漂掉的旧号；⚠️ **2026-10-06 再订正：那个「订正过」的 `:304` 也漂了、现在指的是 `SetUvRect`**
        /// —— ⛔ **一律引符号名，别引行号**）——
        /// 不设的话那 9 块落在默认队列，与别的层「谁盖谁」不可控（同 2026-09-23 那条层序坑）。
        /// 🔴 **2026-10-06（A50③）起本函数改调公共件 `MenuDraw.Nine`**，那 9 块的队列由它设（仍是本函数的 `q`）。
        /// 返回**根节点**（整层一起移动/开关就动它）。
        /// <param name="borderOut">🆕 2026-10-04（A41 ⑥）：**画出来的角块长**（px），不传 = 与 `border` 相同。
        /// 单独有这个参数是因为原版 `Image` 的 `m_PixelsPerUnitMultiplier` 会**缩放画出来的角块**
        /// （页签高亮那颗：`border 30` + `ppuMul 0.92` ⇒ 画出来 **32.61**）。只给一个量的话
        /// 「UV 怎么切」与「角块多大」必有一个是错的（同 `MenuDraw.Nine` 那条注释）。</param></summary>
        GameObject NineSlice(string key, Texture2D tex, Vector4 border, float x, float y, float w, float h, int q,
                             Vector4? borderOut = null)
        {
            if (tex == null) return null;      // 缺图由 `Ui()` 记账
            // 🔴 **2026-10-06（A50③）：改走公共件 `MenuDraw.Nine`**（旧写法直调 `ImageQuad.CreateNineSlice`
            //   ⇒ 绕开公共件、**拿不到 `clip` / `clipSoftness`**）。与旧代码**逐项等价**：
            //    ① **矩形** = 左上角 `(x, y)` + 宽高 `(w, h)`（旧代码那三个实参就是它的中心与宽高）；
            //    ② **落位** = `MenuDraw.Local(Root, …)` 与 `Pos(x+w/2, y+h/2)` 是**同一个世界点** ——
            //       `Root` 出厂在原点（判据 → `Editor/DeckScene.cs:748-749`），且 `U(px)` 与
            //       `LayoutSpace.Px(px)` **同为 `px/108`**（`PxPerUnit` = `1080/DesignHeight`）；
            //    ③ **队列 = `q`**（旧代码建完逐块设的就是它）· **tint 不传**（旧代码也没传）·
            //       `borderOut` 与公共件的 `borderOutPx` **同一条退化**（`?? border`）。
            return MenuDraw.Nine(Root, tex, new PxRect(x, y, x + w, y + h), border, tex.width, tex.height,
                                 q, null, true, key, borderOut);
        }

        // ------------------------------------------------------------ 费用曲线（Deck info 页签）

        void BuildCurve()
        {
            var barBg = Ui("40k_CardAmount_bar_bg");
            var barFill = Ui("40k_CardAmount_bar_fill");
            for (int c = 0; c <= 8; c++)
            {
                float y = CurveY + c * CurveStep;
                var lb = Txt("curve_l" + c, c.ToString(), CurveX - 14f, y, 26f, CurveRowH, 1, Ink, QText);
                var bar = Img("curve_b" + c, barBg, CurveBarX, y + 1.6f, CurveBarW, 16.6f, QPanel);
                var fill = Img("curve_f" + c, barFill, CurveBarX, y + 2f, 1f, 15.3f, QRow);
                var num = Txt("curve_n" + c, "0", CurveBarX + CurveBarW + 6f, y, 34f, CurveRowH, 1, Ink, QText);
                _curveBar.Add(bar); _curveFill.Add(fill); _curveLabel.Add(lb); _curveLabel.Add(num);
                _infoOnly.Add(bar != null ? bar.gameObject : null);
                _infoOnly.Add(fill != null ? fill.gameObject : null);
                _infoOnly.Add(lb != null ? lb.gameObject : null);
                _infoOnly.Add(num != null ? num.gameObject : null);
            }
        }

        // ------------------------------------------------------------ Footer

        void BuildFooter()
        {
            _doneHl = Img("foot_hl", "FX_Square_UI_SDF", DoneHlX, DoneHlY, DoneHlW, DoneHlH, QDoneHl);
            // 🆕 A24：原版 `Sidebar/Footer/Done` 也是 `SpriteSwap`（同上，三颗同图）
            Hover("foot_done",
                  Img("foot_done", "UI_Button_Mulligan", DoneX, DoneY, DoneW, DoneH, QRow),
                  "UI_Button_Mulligan");
            Txt("foot_done_t", "Done", DoneX, DoneY, DoneW, DoneH, 2, Ink, QText);
            // 🔴 2026-09-27（PA 普查）：原版 `Content Area/Sidebar/Footer/Image` 是 PA=1，贴图
            //   `40k_general_icon_card_amount` **64×64** 塞进 50×40 ⇒ 原版实绘 **40×40**（居中），我们 50 宽（**1.25×**）。
            Img("foot_ic", "40k_general_icon_card_amount", FootIcX, FootIcY, FootIcW, FootIcH, QBorder, true);
            _counterTxt = Txt("foot_cnt", "0/30", CntX, CntY, CntW, CntH, 2, Ink, QText);
            Btn_("foot_done", DoneX, DoneY, DoneW, DoneH);
            _verdict = Txt("foot_verdict", "", CntX - 70f, CntY - 42f, 180f, 34f, 1, Ink, QText);
        }

        // ------------------------------------------------------------ 筛选栏（在左）

        void BuildFilters()
        {
            // 🆕 2026-10-04（A67）：两栏**各给一个容器** —— 「整栏滑出去」得有个对象可挪
            //   （原版 `DOAnchorPosX(rect, …)` 动的就是那一棵的 `RectTransform`）。
            //   原来每一件都平铺挂在 `Root` 下、各自按绝对 px 摆位（**没有整栏节点**）⇒ 想整栏挪都没有可挪的东西。
            //   ⚠️ 容器只是**分组**：子件仍旧按 `FltAbs()` / `Pos()` 那套**屏幕绝对 px** 摆
            //     （容器在原点、无缩放 ⇒ `Pos()` 给的 Root 相对坐标原样可用）。
            //   ⚠️ **显隐/位移的唯一出处是 `ApplyDrawerSlide`** —— 所以下面不再有
            //     `SetOn(_fltPanelBg, _filtersOpen)` 那种逐层硬切（两处写同一件事 = 动画途中会被另一方按逻辑态切回去）。
            _fltSlide = NewDrawer("flt_drawer");
            _cosmoFltSlide = NewDrawer("cosmoflt_drawer");

            // ⚠️ 这两张是**面板本体**，开/关由 `ApplyDrawerSlide` 控制 ——
            //    忘了关的话它（队列 3020，比侧栏大）会**一直盖住整个侧栏**
            //    （2026-09-20 实测：卡组行/页签/Done 全被它盖住，画面上只剩一块底板色）。
            _fltPanelShadow = Img("flt_shadow", "40k_main_tab_shadow", FltX, FltY, FltW, FltH, QFlt - 1,
                                  false, FltParent);
            _fltPanelBg = Img("flt_bg", "40k_main_tab_background", FltX, FltY, FltW, FltH, QFlt, false, FltParent);
            BuildFilterFixedParts();

            // 🆕 2026-10-01：**卡背页那个抽屉**（原版 `Cosmetic FIlter`，**另一棵 prefab**，出厂 INACT）。
            //    它与卡牌筛选栏**同一块 rect**（2.18,155.97 → 333.90,1080.03，见 `FilterPanelModel` 那段注释）
            //    ⇒ 两套**不会同时开**（`RefreshCosmoFilters` 与 `RefreshFilters` 按 `_tab` 各管各的）。
            //    `Shadow` 在卡背这棵里是**同父矩形**（不是卡牌那棵的 152.8 宽），照实读。
            _cosmoFltShadow = Img("cosmoflt_shadow", "40k_main_tab_shadow", FltX, FltY, FltW, FltH, QFlt - 1,
                                  false, CosmoFltParent);
            if (_cosmoFltShadow != null) _cosmoFltShadow.SetTint(new Color(0f, 0f, 0f, 0.314f));
            _cosmoFltBg = Img("cosmoflt_bg", "40k_main_tab_background", FltX, FltY, FltW, FltH, QFlt,
                              false, CosmoFltParent);

            // 出厂两栏都收着 ⇒ 按进度 0 摆一次（位置 / 显隐 / 命中）。
            // 🔴 **收尾这一次必须 `force: true`**：新建的件「默认就是活的」，而这次是**关** ——
            //   不强制走一遍，「建完那一刻命中区该是关的」就不成立（收藏窗那边踩过同一个洞，已进坑表）。
            ApplyDrawerSlide(_fltSlide, 0f, true);
            ApplyDrawerSlide(_cosmoFltSlide, 0f, true);
        }
        ImageQuad _fltPanelBg, _fltPanelShadow;
        // ---- 卡背页那套（与卡牌那套**完全分开**）----
        bool _cosmoFltOpen;
        ImageQuad _cosmoFltShadow, _cosmoFltBg;
        readonly List<GameObject> _cosmoFltObjs = new List<GameObject>();
        readonly List<FilterPanelModel.Cell> _cosmoFltCells = new List<FilterPanelModel.Cell>();
        /// <summary>卡背抽屉里每一格的图示 quad（`Key` → quad）—— 格子**没进 `_named` 登记表**（见下面自检读数那一段），
        /// 所以「状态换图」那条断言得靠这一份读（`UiCosmoFilterCellTex`）。</summary>
        readonly Dictionary<string, ImageQuad> _cosmoFltQuads = new Dictionary<string, ImageQuad>();
        readonly List<Btn> _cosmoFltHit = new List<Btn>();
        /// <summary>卡背页**自己**的筛选条件 —— 与卡池那套分开（原版是两棵 prefab、两个 `ownedToggle`；
        /// 混用一份的话，在卡背页选个阵营会**把卡池也筛掉**）。出厂 = `DeckFilter.None`（= 原版出厂态）。</summary>
        DeckFilter _cosmoFilter = DeckFilter.None;

        // ============================================================ 🆕 2026-10-04（§三第29条 A67）
        // **两个左抽屉的滑入/滑出**（原来是**整块硬切**：`SetOn(..., _filtersOpen)` —— 真偏离，这一节就是补它）。
        //
        // 判据（**与收藏窗同源**，原版两处都是 `CollectionFilterController<T>`）：
        //   `Filter Toggle` → `CollectionDisplay.OnEnable → ToggleFilters(bool)`
        //   → **`CollectionFilterController.Toggle(bool, bool)`** → `DOTween.Kill` +
        //     **`DOAnchorPosX(rect, x, 0.3)`** + `SetActive`：收起 x = `hiddenPosition.x` = −550、
        //     展开 x = `originalAnchorPosition.x` = −165 ⇒ **行程 −385px**（见 `FltHiddenDx` 那段）。
        //   ⚠️ **只动 x**（`anchoredPosition = (hiddenPosition.x, originalAnchorPosition.y)`）—— y 保留。
        //
        // 🔴 **批处理没有帧循环**（CLAUDE.md §二）：`Update` 一次都不跑 ⇒
        //   · 真跑（Play）走 `Update → TickDrawers(Time.deltaTime)`，0.3 秒滑完；
        //   · `-executeMethod` 自检里 `UiToggleFilters()` **直接到位**（`Application.isPlaying == false`），
        //     动画本身由**确定性口** `SetDrawerProgressForTest(...)` / `TickDrawers(dt)` 复验
        //     —— 两条路都走同一个 `ApplyDrawerSlide`（**不是两份实现**）。
        //
        // 🔴 **位移量必须过 `LayoutSpace.Px()`**：`FltHiddenDx` 是**原版 px**，而 `localPosition` 是**世界单位**
        //   （1 单位 = 108px）。粗加（`lp.x += FltHiddenDx * …`）⇒ 位移放大 108 倍 = **−59,400px**：
        //   整栏在 0.3 秒的**前 0.6%** 就飞出屏幕（动画实际看不见 = 等价原来的硬切），
        //   而命中/滚轮那 0.3 秒**照样全失效**。⚠️ 这个坑 `Shell/CollectionWindow.cs` 真的踩过（X3 审查 R1）。
        //
        // ⚠️ **「位移期间命中失效」这件事在本窗的做法与收藏窗不同**（**别照抄那边**）：
        //   收藏窗每一格是**真 `WindowButton`** ⇒ 关 `enabled` 就完事（`PointerLayer.CollectHits` 只挑它）。
        //   **本窗没有 `PointerLayer`**（理由写在 `Build()` 末尾那三条），左抽屉的点击是**区域判断**
        //   （`HandlePointer` 里 `px.x < FltX + FltW` 那两处 + `HandleFilterClick` / `HandleCosmoFltClick`
        //   按 `_fltHit` / `_cosmoFltHit` 那两张**px 矩形表**判）⇒ 失效的落点是**那三处区域判断**
        //   （`PointerLayer` 那两条规则在这里都不适用：本窗**没有指针层**，抽屉里的点击是区域判断。）
        //   🔴 **2026-10-05（A32②）订正一句**：括号里原来写的是「**抽屉里一件 `WindowButton` 都没有**」——
        //     现在**有 3 件**了（搜索框 + 三个开关的悬停色偏，`HoverTint` 挂的）。但它仍然**不吃
        //     `PointerLayer` 那两条规则**：那 3 颗的 `WindowButton` **不由 `PointerLayer` 派发**
        //     （本窗没有指针层），而是走 `HoverTargetUnder → DrawerHoverUnder`，且那里**先判
        //     `FltStripOn` / `CosmoFltStripOn`**（= 完全到位才认）⇒ 「位移那 0.3 秒里不亮」自动成立。
        //     ⛔ 别因为它们现在在树上就以为 `SetDrawerInteractive` 关 `enabled` 那条路也管它们（那条路本窗没有）。
        //   判据仍是「**位移期间点不到**」：`Interactive` 只在**完全到位**时为真。

        /// <summary>一个左抽屉的滑动状态（卡牌那栏一份、卡背那栏一份）。
        /// 字段语义与 `Shell/CollectionWindow.FilterPanel` 的那半份**逐条对齐**（同一套判据）。</summary>
        class FilterDrawer
        {
            /// <summary>整栏的容器（`Root` 的子物体，出厂在原点）—— 位移就是动它的 `localPosition.x`。</summary>
            public Transform Node;
            /// <summary>**逻辑态**（UI/自检读的都是它；动画期间与 `Slide` 故意不同）。</summary>
            public bool Open;
            /// <summary>滑动进度：**0 = 已滑出（`hiddenPosition` 那一头）· 1 = 停在原位**。</summary>
            public float Slide;
            /// <summary>滑动目标（0 或 1）—— `Open` 一变就设它。</summary>
            public float SlideTarget;
            /// <summary>到位时的 `localPosition`（第一次用到时抓一次；位移是「相对它」加的）。</summary>
            public Vector3 BasePos;
            /// <summary>`BasePos` 抓过没有（别拿 `BasePos == zero` 判 —— 那个位置**就是** 0）。</summary>
            public bool HasBasePos;
            /// <summary>现在参不参与命中/滚轮（**只有完全到位才 true**）。见 `SetDrawerInteractive`。</summary>
            public bool Interactive;
            /// <summary>它所属的**页**现在在不在画面上（卡背那棵挂在 `Cosmetic Display` 底下 ⇒ 随 `_tab`）。
            /// 这一半是**瞬时**的（照原版「整页 `SetActive`」），不参与滑动。</summary>
            public bool PageOn = true;
        }

        FilterDrawer _fltSlide;             // 卡牌那栏（原版 `Card Filters`）
        FilterDrawer _cosmoFltSlide;        // 卡背那栏（原版 `Cosmetic FIlter`，另一棵 prefab）

        /// <summary>建一个抽屉容器：`Root` 的子物体、原点、无缩放（只做分组 + 整栏位移）。</summary>
        FilterDrawer NewDrawer(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return new FilterDrawer { Node = go.transform };
        }

        /// <summary>建抽屉里的件挂到哪个父级（还没建容器时退回 `Root` —— 只可能出现在建的过程中）。</summary>
        Transform FltParent { get { return _fltSlide != null && _fltSlide.Node != null ? _fltSlide.Node : Root; } }
        Transform CosmoFltParent { get { return _cosmoFltSlide != null && _cosmoFltSlide.Node != null ? _cosmoFltSlide.Node : Root; } }

        // ---- 滑动引擎（帧路与自检口**共用** `ApplyDrawerSlide` 这一份实现）----

        /// <summary>每帧推一次抽屉动画（原版是 DOTween 的 0.3 秒）。
        /// ⚠️ 批处理下这个方法**不会**被调用（没有帧循环）—— 那是预期的，见上面那段。</summary>
        void TickDrawers(float dt)
        {
            if (dt <= 0f) return;                    // 照 UGUI 那条 `deltaTime > 0` 守卫
            StepDrawer(_fltSlide, dt);
            StepDrawer(_cosmoFltSlide, dt);
        }

        void StepDrawer(FilterDrawer d, float dt)
        {
            if (d == null || d.Node == null) return;
            if (d.Slide == d.SlideTarget) return;                                  // 已到位 ⇒ 一根手指都不动
            float step = dt / Mathf.Max(0.0001f, FltAnimTime);                     // 原版 `animationTime` = 0.3
            ApplyDrawerSlide(d, Mathf.MoveTowards(d.Slide, d.SlideTarget, step));
        }

        /// <summary>`Open` 一变就调它：设目标 + （Play 里）起动画 / （批处理里）直接到位。</summary>
        void StartDrawerSlide(FilterDrawer d)
        {
            if (d == null || d.Node == null) return;
            d.SlideTarget = d.Open ? 1f : 0f;
            if (Application.isPlaying)
                // 起手先按**当前**进度摆一次：关着的那一栏原来停在原位、只是被藏了，
                // 现在要真的**从滑出去那一头滑回来**（起点 = 整栏在屏外，行程 −385px）。
                ApplyDrawerSlide(d, d.Slide);
            else
                ApplyDrawerSlide(d, d.SlideTarget);                                // 没有帧循环 ⇒ 一步到位
        }

        /// <summary>**动画的全部效果都在这一个函数里**（位置 / 显隐 / 命中）——
        /// 帧路与自检口都调它，所以「自检绿的」与「跑起来的样子」是同一份实现。
        /// <paramref name="forceInteractive"/>：见 <see cref="SetDrawerInteractive"/>（建完 / 重建后用）。</summary>
        void ApplyDrawerSlide(FilterDrawer d, float t, bool forceInteractive = false)
        {
            if (d == null || d.Node == null) return;
            if (!d.HasBasePos) { d.BasePos = d.Node.localPosition; d.HasBasePos = true; }
            d.Slide = Mathf.Clamp01(t);

            // ① 显隐：**滑出去了才关**、**在滑的途中要活着**（要不那 0.3 秒什么都看不见）。
            //    另外「它所属的那一页不在」也直接关（瞬时，不参与滑动）。
            bool live = (d.Slide > 0f || d.SlideTarget > 0f) && d.PageOn;
            if (d.Node.gameObject.activeSelf != live) d.Node.gameObject.SetActive(live);

            // ② 位移：**只改 x**（判据见上）；y/z 保留 —— 原版那句 `(hiddenPosition.x, originalAnchorPosition.y)`
            // 🔴 `FltHiddenDx` **必须过 `LayoutSpace.Px()`**（px → 世界单位；见本节开头那条注释）。
            var lp = d.BasePos;
            lp.x += LayoutSpace.Px(FltHiddenDx) * (1f - d.Slide);
            d.Node.localPosition = lp;

            // ③ 命中：**只有完全到位才生效**（位移期间点不到、滚轮也不吃）
            SetDrawerInteractive(d, d.Slide >= 1f && d.SlideTarget >= 1f && d.PageOn, forceInteractive);
        }

        /// <summary>命中区开/关。`force = true` 时忽略「没变就不动」那条短路（建完之后要重按一次）。
        ///
        /// ① 本窗的机制是**区域判断**（见本节开头那段：抽屉里没有 `WindowButton`、也没有 `PointerLayer`）
        ///    ⇒ 这一个布尔就是 `HandlePointer` / `UiClickPx` / `HandleScroll` 那几处区域的开关。
        /// ② ⚠️ **如实标注（我们自己的口径，别当成原版行为）**：**原版在那 0.3 秒里到底屏不屏蔽点击，
        ///    我们没核过** —— `CollectionFilterController<T>.Toggle` 的方法体在泛型里、`decomp_full` 无产物
        ///    （见 `资料/卡组编辑界面_查证_0920.md`）⇒「滑出去了就点不到才对」是**我们挑的**口径
        ///    （同 `Shell/CollectionWindow.SetDrawerInteractive` 的 R10/R11 那两条如实标注）。
        ///    ⛔ 不是实读出来的原版行为。</summary>
        void SetDrawerInteractive(FilterDrawer d, bool on, bool force)
        {
            if (d == null || d.Node == null) return;
            if (!force && d.Interactive == on) return;      // 每帧都调 ⇒ 没变就别白扫一遍
            d.Interactive = on;
        }

        /// <summary>这一栏现在「还在不在画面上」（= 正在滑 或 停在原位那一头）—— 与 `Open` **不同**：
        /// 收起时 `Open` 立刻翻假，但整栏还要滑出去 0.3 秒（那 0.3 秒里格子要留着一起滑走）。</summary>
        static bool DrawerLive(FilterDrawer d) { return d != null && (d.Slide > 0f || d.SlideTarget > 0f); }

        // 🔴 **左抽屉那一条竖带吃不吃指针 —— 唯一出处**（`HoverTargetUnder` / `HandlePointer` /
        //   `UiClickPx` / `HandleScroll` 四处都读这两个属性，别各写一遍 `_filtersOpen`）。
        //   判据 = 逻辑态开着 **且** 已经完全到位（`Interactive`）—— 位移期间不吃（A67）。
        //   批处理下面板总是「一步到位」⇒ 这两个属性与 `_filtersOpen` / `_cosmoFltOpen` **同值**，
        //   既有断言（点得到 / 点不到）一条都不变。
        bool FltStripOn { get { return _filtersOpen && _fltSlide != null && _fltSlide.Interactive; } }
        bool CosmoFltStripOn { get { return _cosmoFltOpen && _cosmoFltSlide != null && _cosmoFltSlide.Interactive; } }

        /// <summary>把容器摆回**原位**（位移 0）—— **建格子之前**调一次。
        /// 🔴 为什么必须：格子里的小字走 `Label.AlignLeftOn/AlignRightOn`，那两个口减的是
        ///   **父级当前的世界 x**（`Battle/Label.cs:447,456`）⇒ 容器偏着建，字会被摆到「原位」的世界坐标上、
        ///   与容器差出整段行程（**该偏 385px**）。建完再由 `ApplyDrawerSlide` 按进度整体挪回去。
        ///   ⚠️ 两个口**只减直接父级**，再往上一层（`Root`）的位移它们看不见 —— 所以容器这一层必须自己归零。</summary>
        void DrawerHome(FilterDrawer d)
        {
            if (d == null || d.Node == null) return;
            if (!d.HasBasePos) { d.BasePos = d.Node.localPosition; d.HasBasePos = true; }
            d.Node.localPosition = d.BasePos;
        }

        /// <summary>建内容之前把容器弄成「**量得出 TMP**」的样子：激活。
        /// （老账：在不可见的父级上量 TMP / `AlignRightOn` 会摆错 —— 见 `RefreshFilterCells` 那条注释。）
        /// 建完 `ApplyDrawerSlide` 会按进度重摆一次，所以中途多激活一下没有副作用。</summary>
        static void PrepareDrawerForBuild(FilterDrawer d)
        {
            if (d == null || d.Node == null) return;
            if (!d.Node.gameObject.activeSelf) d.Node.gameObject.SetActive(true);
        }

        // ---- 抽屉里**位置固定**的三件（搜索框底/字/尾图标）+ 四个小标题 ----
        //      它们只在「面板内坐标 + 滚动量」上移动 ⇒ **建一次、之后只摆位**（格子才是每次重建的）。
        GameObject _fltInputRoot; Label _fltInputText; ImageQuad _fltInputIcon;
        readonly List<Label> _fltTitles = new List<Label>();

        void BuildFilterFixedParts()
        {
            // 搜索框底 = **九宫格**（原版 `InputFieldBackground` 是 Unity 内置图 32×32、`m_Border=(10,10,10,10)`）
            PxRect inR, taR, icR;
            FilterPanelModel.NameRowRects(FltW, out inR, out taR, out icR);
            var tex = Ui(FilterPanelModel.InputSprite);
            if (tex != null)
            {
                var r = new PxRect(FltX + inR.x1, FltY + inR.y1, FltX + inR.x2, FltY + inR.y2);
                // 🔴 **2026-10-06（A50③）：这一处收口到公共件 `MenuDraw.Nine`** —— 原来直调
                //   `ImageQuad.CreateNineSlice`（= 绕开公共件的那条路，**拿不到 `clip` / `clipSoftness`**）。
                //   与旧代码**逐项等价**（三样都别改）：
                //    ① **矩形** = `r`（旧代码那两个实参就是它的 `CX/CY` 与 `W/H`）；
                //    ② **落位** = 旧代码给的是 `Pos(r.CX, r.CY)`、父级却是 `FltParent`（抽屉容器）——
                //       容器由 `NewDrawer` 建、**出厂 `localPosition` 就是零**，且本行跑在
                //       `ApplyDrawerSlide` **之前**（`Build()` 里 `BuildFilterFixedParts()` 那次调用先于
                //       收尾的两次 `ApplyDrawerSlide(…, 0f, true)`）
                //       ⇒ 此刻 `FltParent.position` = `Root.position` = 原点，`MenuDraw.Local(FltParent, r)`
                //       里那个 `− parent.position` 减的就是零（判据 → `Editor/DeckScene.cs:748-753`：
                //       `Root` 出厂在原点 + 抽屉容器同样在原点；同一条口语见 `Img` 的 `parent` 参数注释）；
                //    ③ **队列 = `QFltRow`** · **tint = `FilterPanelModel.InputTint`**（旧代码建完逐块设的就是这两样，
                //       `MenuDraw.Nine` 会替我们设）；`SetTint` 仍**先于**下面的 `HoverTint` 跑完 —— 顺序别动。
                _fltInputRoot = MenuDraw.Nine(FltParent, tex, r,
                    new Vector4(FilterPanelModel.InputBorder, FilterPanelModel.InputBorder,
                                FilterPanelModel.InputBorder, FilterPanelModel.InputBorder), 32f, 32f,
                    QFltRow, FilterPanelModel.InputTint, true, "flt_input");
                if (_fltInputRoot != null)
                {
                    foreach (var q in _fltInputRoot.GetComponentsInChildren<ImageQuad>())
                    { q.SetTint(FilterPanelModel.InputTint); q.SetRenderQueue(QFltRow); }
                    // 🆕 2026-10-05（A32②）：这个搜索框（原版 `… > Name FIlter > Input Field`，`EverguildInputField`）
                    //   也是 `m_Transition = 1(ColorTint)`、目标 = **自己那张底图**、`m_Colors` = UGUI 默认那组
                    //   ⇒ 悬停乘 0.9607843（按下 0.7843137）。**判据与 `Deck Name` 那颗同一组**（同一族 prefab）。
                    //   ⚠️ 登记进**抽屉那份表**（`_fltHoverBtns`）—— 它的矩形不在 `_btns` 里，
                    //     悬停派发走 `DrawerHoverUnder`（与 `_fltHit` 同一张矩形表 = 与点击同源）。
                    //   ⚠️ 同样：`SetTint` 那圈必须**先**跑完再挂（`Collect()` 抓基准色的时机，见 `Deck Name` 那条）。
                    HoverTint("$name", _fltInputRoot, _fltHoverBtns);
                }
            }

            var tr = new PxRect(FltX + taR.x1, FltY + taR.y1, FltX + taR.x2, FltY + taR.y2);
            _fltInputText = Txt("flt_input_t", "", tr.x1, tr.y1, tr.W, tr.H, 1, Ink, QFltText, FltParent);
            if (_fltInputText != null) _fltInputText.SetGlyphHeight(LayoutSpace.Px(FilterPanelModel.InputFontPx));

            var ir = new PxRect(FltX + icR.x1, FltY + icR.y1, FltX + icR.x2, FltY + icR.y2);
            _fltInputIcon = Img("flt_input_i", FilterPanelModel.SearchIconSprite, ir.x1, ir.y1, ir.W, ir.H,
                                QFltIconTop, true, FltParent);

            var titles = new List<FilterPanelModel.Title>();
            FilterPanelModel.BuildTitles(State, FltW, titles);
            foreach (var tl in titles)
            {
                var r = new PxRect(FltX + tl.R.x1, FltY + tl.R.y1, FltX + tl.R.x2, FltY + tl.R.y2);
                var lb = Txt("flt_title_" + tl.Text.Replace(" ", "_"), tl.Text, r.x1, r.y1, r.W, r.H, 1, Ink,
                             QFltText, FltParent);
                if (lb != null)
                {
                    lb.SetGlyphHeight(LayoutSpace.Px(tl.Px));
                    _fltTitles.Add(lb);
                }
            }
        }

        // ------------------------------------------------------------ 卡池

        void BuildPool()
        {
            // 🔴 卡池的视图**不能拿 `default(CardData)` 建** —— 空数据会走 `CardView.Build` 的
            //    「空卡位」分支（没有卡框/立绘那几层），而 `SetData` **只更新已存在的层**、
            //    不会把缺的层补出来 ⇒ 整个卡池的画面上是**一排空卡位**（2026-09-20 截图看出来的）。
            //    ⇒ 改成「**卡变了就重建视图**」：一屏只有 8 张，重建的代价可以接受，正确性优先。
            _poolViews.Clear();
            _poolViewIds.Clear();
            Img("pool_info_bg", "40k_topmarquee_currency_display_BW", PoolX + 8f, PoolY - 8f, 220f, 40f, QPoolInfo);
            _poolInfo = Txt("pool_info", "", PoolX + 8f, PoolY - 8f, 220f, 40f, 1, Ink, QText);
        }

        /// <summary>每个格子里现在建的是哪张卡（`null` = 空位）。和 <see cref="_poolViews"/> 一一对应。</summary>
        readonly List<string> _poolViewIds = new List<string>();

        void RefreshPool()
        {
            var all = State.VisibleCards();

            float pitch = PoolRowPitch;
            int firstRow = Mathf.FloorToInt(_poolScroll / pitch);
            float off = _poolScroll - firstRow * pitch;
            float top = PoolY + PoolPadY;

            _poolIndex.Clear();
            for (int vi = 0; vi < PoolCols * PoolRows; vi++)
            {
                int r = vi / PoolCols, c = vi % PoolCols;
                int idx = (firstRow + r) * PoolCols + c;
                // 🔴 **卡池只在 Cards 页签出现**（原版 `DeckEditingPanel.ToggleCards/ToggleCosmetics` 会
                //    关掉 `Card Display`）—— 2026-09-24 加：原来切到 Deck info / Cosmetics 页时
                //    **卡池还画在那儿**（只是被那一页自己的底板盖住了，靠叠层遮丑，不是真关）。
                bool on = _tab == 0 && idx < all.Count;
                _poolIndex.Add(on ? idx : -1);
                while (_poolViews.Count <= vi) { _poolViews.Add(null); _poolViewIds.Add(null); }

                if (!on)
                {
                    if (_poolViews[vi] != null) _poolViews[vi].gameObject.SetActive(false);
                    ShowPoolCounter(vi, false);
                    continue;
                }

                var def = all[idx];
                // 贴左起排：格中心 = 视口左边 + 格宽/2 + 列·（格宽 + 间距）
                float cx = PoolX + PoolPadX + CellW * 0.5f + c * (CellW + PoolSpacing);
                float cellTop = top + r * pitch - off;                    // 这一格的**上沿**
                // 🔴 卡**按「张数条上方那一段」居中**（不是整格）—— 底下 38.272 留给张数条，见 `PoolCounterH`
                float cy = cellTop + PoolCardH * 0.5f;

                if (_poolViews[vi] == null || _poolViewIds[vi] != def.Id)
                {
                    if (_poolViews[vi] != null)
                    {
                        // ⚠️ Play 模式下 `Destroy` 要等帧末 —— 先关掉，否则那一帧新旧两张会叠在一起
                        _poolViews[vi].gameObject.SetActive(false);
                        DestroySafe(_poolViews[vi].gameObject);
                    }
                    _poolViews[vi] = CardView.Create(Root, BattleDriver.ToCardData(def, def.Faction), "pool_" + vi);
                    _poolViewIds[vi] = def.Id;
                }
                var v = _poolViews[vi];
                v.gameObject.SetActive(true);
                v.SetPose(Pos(cx, cy), 0f, PoolCardScale);
                v.SetData(BattleDriver.ToCardData(def, def.Faction));
                v.SetFace(CardFace.Full);
                v.SetHighlight(State.CanAdd(def) == DeckError.None
                               ? CardHighlightState.Playable : CardHighlightState.Normal);
                // 底下那条「张数」（原版 `Collection Card/Content/Counter` + `Text (TMP)`）
                ShowPoolCounter(vi, true);
                SetPoolCounter(vi, def, cx - CellW * 0.5f, cellTop);
            }

            int rows = Mathf.Max(1, Mathf.CeilToInt(all.Count / (float)PoolCols));
            // 内容总高 = 行数 × 行距（间距 0）；可滚的 = 超出视口的那些
            float maxScroll = Mathf.Max(0f, rows * pitch - PoolH);
            _poolScroll = Mathf.Clamp(_poolScroll, 0f, maxScroll);
        }

        // ============================================================ 卡池每格底下那条「张数」

        readonly List<ImageQuad> _poolBars = new List<ImageQuad>();
        readonly List<Label> _poolBarTexts = new List<Label>();

        /// <summary>那条「张数」的**文本**。两个格式来自**两处不同的代码**（别当成一条规则）：
        ///   · **卡组里还没有督军** ⇒ `x{…}` —— 原版 `CardCollectionDisplay__SetCell.c:72` 的格式串实测是
        ///     **`x{0}`，喂的是【拥有数】**（同一处还有 `ToggleGreyScale(拥有数 < 1)` ⇒ 没拥有的那张置灰）。
        ///     ⚠️ **我们这版不照喂那个数**：本作资源固定 9999、`CardProgress.Owned` 给足
        ///     （= 卡组上限 + 升满所需）⇒ 照原式会显示 `x11` 那种**没有意义**的数。
        ///     按**用户 2026-09-28 的口径**显示 `min(拥有, 卡组上限)` = 「**能放进卡组的张数**」。
        ///   · **已经有督军** ⇒ `{已在卡组中}/{min(拥有, 卡组上限)}` —— 原版
        ///     `DeckEditorCollectionDisplay__DrawCell.c` 的格式串**实测就是 `"{0}/{1}"`**
        ///     （`stringliteral.json@0x426DE28`，两个数：一个「组里已有几张」、一个「最多能放几张」）。
        /// 分母两处相同；⚠️ **详情弹窗**那条 `x{a}/ {b}`（可放入张数 / 多余副本数）是**第三个**格式，别混。</summary>
        string PoolCounterText(CardDef def)
        {
            int cap = DeckRules.CopyLimit(def.Rarity);
            int den = Mathf.Min(CardProgress.Owned(def.Id, def.Rarity), cap);
            if (string.IsNullOrEmpty(State.Deck.WarlordId)) return "x" + den;   // 还没督军
            return State.Deck.CountOf(def.Id) + "/" + den;                       // 已有督军
        }

        /// <summary>摆一格底下那条「张数」（底图 + 那行字）。**每次刷新都要重摆** ——
        /// 滚动时 `cellTop` 一直在变，而 `Img`/`Txt` 只在建的时候摆一次。</summary>
        void SetPoolCounter(int vi, CardDef def, float cellLeft, float cellTop)
        {
            while (_poolBars.Count <= vi) { _poolBars.Add(null); _poolBarTexts.Add(null); }
            float bw = PoolBarX2 - PoolBarX1, tw = PoolBarX2 - PoolBarX1;
            float tx = cellLeft + (PoolBarX1 + PoolBarX2) * 0.5f;
            if (_poolBars[vi] == null)
            {
                _poolBars[vi] = Img("poolbar_" + vi, PoolCounterSprite,
                                    cellLeft + PoolBarX1, cellTop + PoolCardH, bw, PoolCounterH,
                                    QPoolBar, true);
                var lb = TxtPx("poolcnt_" + vi, "", cellLeft + PoolBarX1, cellTop + PoolCntY1,
                               tw, PoolCntY2 - PoolCntY1, PoolCounterPx, Color.white, QPoolBarText);
                // 原版那行字**开了 autosize（7…32）**，而字框只有 22.69 高 ⇒ 运行时会被压小；
                // 我们走同一条路（`SetAutoFitBox`）：限宽/限高 = 字框、下限 7px。
                if (lb != null) lb.SetAutoFitBox(U(tw), U(PoolCntY2 - PoolCntY1), 7f, PoolCounterPx);
                _poolBarTexts[vi] = lb;
            }
            var bar = _poolBars[vi];
            if (bar != null)
                bar.transform.localPosition = Pos(tx, cellTop + PoolCardH + PoolCounterH * 0.5f);
            var t = _poolBarTexts[vi];
            if (t != null)
            {
                t.transform.localPosition = Pos(tx, cellTop + (PoolCntY1 + PoolCntY2) * 0.5f);
                t.SetText(PoolCounterText(def));
            }
        }

        void ShowPoolCounter(int vi, bool on)
        {
            if (vi < _poolBars.Count && _poolBars[vi] != null) _poolBars[vi].gameObject.SetActive(on);
            if (vi < _poolBarTexts.Count && _poolBarTexts[vi] != null) _poolBarTexts[vi].gameObject.SetActive(on);
        }

        // ------------------------------------------------------------ Deck info 的动作钮 + Cosmetics 空态

        /// <summary>分享 / 导入两个钮。
        /// 🔴 **入口位置是我们放的** —— 原版这两个动作**不在编辑器窗口里**：
        /// 分享 = `DeckInfoPopup.ShareDeck()`（卡组选择界面的 Info 弹窗）、
        /// 导入 = `SelectDecksTab.ImportDeck()` → `ImportDeckPopup`。
        /// 那两个界面我们还没有（属第 16 行阶段二），所以先摆在 **Deck info 页签**下面，
        /// 免得玩家找不到入口。**别当原版抄。**</summary>
        void BuildInfoActions()
        {
            // ⚠️ 这两张是 **71×71 的圆钮图标**（`40k_general_bt_yellow_share` / `_edit`，
            //    和 `DeckInfoPopup` 那 5 个圆钮同一族）—— 按**原尺寸**摆，别拉成横条
            //    （拉过一次：变成两条斜飘带，截图看出来的）。文字放钮下面。
            const float bs = 71f, by = 636f, lh = 26f;
            float bx1 = 60f, bx2 = 200f;
            // ⚠️ 这两张**只是图标**（四周透明）—— 原版那套圆钮是**两层**：
            //    底 `UI_Button_Round_background` + 图标。只画图标的话屏幕上什么都没有。
            Img("info_share_bg", "UI_Button_Round_background", bx1, by, bs, bs, QRow);
            Img("info_share", "40k_general_bt_yellow_share", bx1, by, bs, bs, QBorder);
            var shareTx = Txt("info_share_t", "分享", bx1, by + bs, bs, lh, 1, Ink, QText);
            Img("info_import_bg", "UI_Button_Round_background", bx2, by, bs, bs, QRow);
            Img("info_import", "40k_general_bt_yellow_edit", bx2, by, bs, bs, QBorder);
            var importTx = Txt("info_import_t", "导入", bx2, by + bs, bs, lh, 1, Ink, QText);
            Btn_("info_share", bx1, by, bs, bs);
            Btn_("info_import", bx2, by, bs, bs);
            foreach (var k in new[] { "info_share_bg", "info_share", "info_import_bg", "info_import" })
            { var q = Lookup(k); _infoOnly.Add(q != null ? q.gameObject : null); }
            // ⚠️ **标签要显式收进来** —— `Lookup()` 只认 `ImageQuad`（`_named` 是 `Img()` 填的），
            //    `Label` 不在里面 ⇒ 拿 `Lookup("info_share_t")` 永远拿到 null、字就**不跟着页签隐藏**
            //    （2026-09-20 截图抓到：Cards 页签下还飘着「分享 / 导入」两个字）。
            foreach (var l in new[] { shareTx, importTx }) if (l != null) _infoOnly.Add(l.gameObject);
        }

        // ------------------------------------------------------------ Cosmetics 页（换卡背）2026-09-24
        //
        // 原版 = `Deck Editing Menu > Content Area > Cosmetic Display`（`CardbackCollectionDisplay`）。
        // 逐条实读（普查报告，`menu_rect.py bundle_menus_assets_all "Deck Editing Menu" --depth 8`）：
        //   · `Cosmetic Display` 167.18,70.97 → 1920.00,1080.03（**出厂 act=F**，切到本页才开）
        //   · `Scroll View`  330.23,155.97 → 1920.00,1080.03（**1589.78 × 924.06**）
        //   · 网格：`_cellWidth=250` `_cellHeight=405` **spacing 0**、
        //     🔴 **列数是【按宽度算】的 = `floor(1589.78 ÷ 250)` = 6**，**不是字段里的 `_segments=4`**
        //     （`_controlSegmentSize=1` ⇒ `ConfigureColumnNumber` 每帧按宽度覆盖它；与收藏窗那条同一机制）
        //   · 一格 = 原型 `CollectionCosmetic` 250×405，**只有一层图**（卡背本身），没有卡名/数值
        //   · `Cosmetic FIlter`（左抽屉）2.18,155.97 → 333.90,1080.03，**出厂 act=F** ⇒ **先不建**（见文件尾「没建」）
        //   · `Cosmetic Drag Controller`（拖拽预览，100×100 + `scl 0.6` 的 250×405 预览）⇒ **先不建**
        //   · `Empty Collection Warning` 135.23,70.97 → 1970.00,1080.03（act=F）
        //   · 🔴 **点格子怎么装备**（`DeckEditingWindow__OnCosmeticClick.c:26-44`）：
        //     **只有右键（`button==1`）才装备** —— `editingDeck.cardbackId = item.GetID()`；
        //     **左键什么都不做**（对照 `__OnCardClick.c` 左键是开卡牌详情，而饰品没有详情窗）。
        //     我们照做（与「卡池左键放大 / 右键加牌」同一条手感）。
        //   · 装备结果**原版不在格子上标**（原型 `useSelectedHighlight=0`），而是显示在侧栏
        //     `Sidebar/Deck Details/Cosmetic Drawer` = **0.25,485.50 → 335.56,885.50**（335.31×400）；
        //     切到本页时 `DeckEditingPanel.ToggleCosmetics` **关掉卡组列表/计数/费用**三个抽屉、只开它。
        const float CosmoX = 330.23f, CosmoY = 155.97f, CosmoW = 1589.78f, CosmoH = 924.06f;
        const float CosmoCellW = 250f, CosmoCellH = 405f;
        /// <summary>列数 = `floor(视口宽 ÷ 格宽)`（**算出来的，不是 `_segments`**）。</summary>
        static readonly int CosmoCols = Mathf.Max(1, Mathf.FloorToInt(CosmoW / CosmoCellW));
        /// <summary>内容**整体居中**的左边距（= (视口宽 − 列数×格宽) ÷ 2 = 44.89）。</summary>
        static readonly float CosmoPadX = (CosmoW - CosmoCols * CosmoCellW) * 0.5f;
        /// <summary>一屏盖几行（多算一行，滚动时正好接上）。</summary>
        static readonly int CosmoRows = Mathf.CeilToInt(CosmoH / CosmoCellH) + 1;
        /// <summary>`Sidebar/Deck Details/Cosmetic Drawer`（原版 rect，切到本页才显示）</summary>
        const float DrawerX = 0.25f, DrawerY = 485.5f, DrawerW = 335.31f, DrawerH = 400f;

        float _cosmScroll;
        readonly List<string> _cosmNames = new List<string>();      // 当前铺的卡背名（与 _cosmCells 一一对应）
        readonly List<ImageQuad> _cosmCells = new List<ImageQuad>();
        readonly List<bool> _cosmCellOn = new List<bool>();         // 这一格有数据吗（显隐见 ApplyCosmCellVisibility）
        readonly List<GameObject> _cosmOnly = new List<GameObject>();
        ImageQuad _cosmDrawerBack;                                  // 侧栏「已装备」那张
        Label _cosmDrawerName;
        GameObject _cosmEmptyWarn;

        /// <summary>
        /// Cosmetics 页签 —— 原来是**一句空态**（`BuildCosmeticsEmpty`：只写「饰品系统未实现」）。
        /// 🔴 **2026-09-24 改成真页面**：233 张卡背铺格 + **右键装备**。
        /// 数据落在 `PlayerDeck.CardbackId`（原版同名同义），判据与出处见那个字段。
        /// </summary>
        void BuildCosmeticsPage()
        {
            // ---- 卡背网格（**懒得建中间节点**：直接挂在 `Root` 下、按 px 摆，与 `Img` 同一条路）----
            //      格子是**按需建 quad**的（`ImageQuad.Create` 不收 null 贴图 ⇒ 第一帧才有图）
            for (int i = 0; i < CosmoRows * CosmoCols; i++) _cosmCells.Add(null);

            // ---- 侧栏「已装备的卡背」（原版 `Cosmetic Drawer`）----
            // ⚠️ `ImageQuad.Create` **不收 null 贴图**（返回 null）⇒ 起手没有默认卡背时先不建，
            //    等 `RefreshCosmeticDrawer` 拿到图再补建（装备之后一定会有图）。
            _cosmDrawerName = Txt("cosm_drawer_name", "", DrawerX, DrawerY + DrawerH - 44f, DrawerW, 44f, 2, Ink, QText);
            _cosmOnly.Add(_cosmDrawerName != null ? _cosmDrawerName.gameObject : null);
            // 侧栏那行说明（**我们挑的**：原版这一页没有这行字，它靠抽屉里的图说话；
            //   我们加它是为了让「右键装备」这条**只写在原版代码里的**操作在界面上说得出口）
            var hint = Txt("cosm_hint", "右键卡背 = 装备到当前卡组（左键不做任何事，与原版一致）",
                           DrawerX, DrawerY + DrawerH + 6f, DrawerW, 40f, 1, new Color(1f, 1f, 1f, 0.75f), QText);
            _cosmOnly.Add(hint != null ? hint.gameObject : null);

            // ---- `Empty Collection Warning`（原版 act=F；判据 = 过滤后为空）----
            // 出厂 `act=F`、**运行期才按数据开** ⇒ 建出来先关着。判据写在 `RefreshTabVisibility` 一处
            //（本页还没有筛选抽屉 ⇒ 233 张永远非空 ⇒ 实际上永远不显示，与原版「没筛就不空」一致）。
            _cosmEmptyWarn = NewGo("cosm_empty");
            var emptyTx = Txt("cosm_empty_t", "There are no cards in your collection for the selected filters",
                              135.23f, 70.97f, 1970.00f, 1080.03f, 5, Ink, QText);
            if (_cosmEmptyWarn != null)
            {
                if (emptyTx != null) emptyTx.transform.SetParent(_cosmEmptyWarn.transform, true);
                _cosmOnly.Add(_cosmEmptyWarn);
            }

            var names = CardArt.CosmeticNames();
            Debug.Log($"[Deck] Cosmetics 页：卡背 {names.Length} 张 · 视口 {CosmoW}×{CosmoH} · "
                      + $"列数 {CosmoCols}（= floor({CosmoW} ÷ {CosmoCellW})，**不是** _segments=4）· "
                      + $"一屏铺 {CosmoRows} 行 × 下滚");
            RefreshCosmetics();
        }

        /// <summary>建一个空节点（只为分组/显隐；不挂任何图）。</summary>
        GameObject NewGo(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root, false);
            return go;
        }

        void RefreshCosmetics()
        {
            // 🆕 2026-10-01：卡背页那个 `Army Filter`（原版 `Cosmetic FIlter` 里的一行）——
            //   筛的是**卡背归属的阵营**；判据 = 原版 SO 的 `cardArmy`（表 → `Core/CardbackTable.cs`，
            //   生成 → `工具/gen_cardbacks.py`；对账实测 243 个 SO → 233 个图名、**0 缺口**）。
            //   ⚠️ 只决定**这一页铺哪几张**，不碰卡池（与卡牌那套筛选条件分开）。
            var names = CardbackTable.NamesFor(_cosmoFilter.Faction, CardArt.CosmeticNames());
            int rows = Mathf.Max(1, Mathf.CeilToInt(names.Length / (float)CosmoCols));
            float maxScroll = Mathf.Max(0f, rows * CosmoCellH - CosmoH);
            _cosmScroll = Mathf.Clamp(_cosmScroll, 0f, maxScroll);
            int firstRow = Mathf.FloorToInt(_cosmScroll / CosmoCellH);
            float off = _cosmScroll - firstRow * CosmoCellH;

            while (_cosmNames.Count < CosmoRows * CosmoCols) _cosmNames.Add(null);
            while (_cosmCells.Count < CosmoRows * CosmoCols) _cosmCells.Add(null);
            while (_cosmCellOn.Count < CosmoRows * CosmoCols) _cosmCellOn.Add(false);

            for (int vi = 0; vi < CosmoRows * CosmoCols; vi++)
            {
                int r = vi / CosmoCols, c = vi % CosmoCols;
                int idx = (firstRow + r) * CosmoCols + c;
                bool on = idx < names.Length;
                _cosmCellOn[vi] = on;
                var q = _cosmCells[vi];
                if (!on) { _cosmNames[vi] = null; continue; }

                float cx = CosmoX + CosmoPadX + c * CosmoCellW + CosmoCellW * 0.5f;
                float cy = CosmoY + r * CosmoCellH - off + CosmoCellH * 0.5f;
                var tex = CardArt.Cosmetic(names[idx]);
                if (q == null && tex != null)
                {
                    q = ImageQuad.Create(Root, tex, Pos(cx, cy), U(CosmoCellH), new Vector2(0.5f, 0.5f), "cosm_cell" + vi);
                    if (q != null) q.SetRenderQueue(QPanel);
                    _cosmCells[vi] = q;
                }
                if (q == null) continue;
                q.transform.localPosition = Pos(cx, cy);
                q.SetTexture(tex);
                q.SetAspect(CosmoCellW / CosmoCellH);       // 与收藏窗那一页**同一条**（那页也这么压）
                _cosmNames[vi] = names[idx];
            }
            ApplyCosmCellVisibility();
            RefreshCosmeticDrawer();
        }

        /// <summary>卡背格的显隐 **只在这一个地方判**（`_cosmCellOn` = 这一格有数据 · `_tab == 2` = 这一页开着）。
        /// ⚠️ 别在 `RefreshCosmetics` 里直接 `SetActive(true)` —— 那样在 Cards 页签上重建一次
        ///    就会把卡背**盖到卡池上**（同一族坑：`_cosmOnly` 那批也靠 `RefreshTabVisibility` 一处判）。</summary>
        void ApplyCosmCellVisibility()
        {
            bool cosm = _tab == 2;
            for (int vi = 0; vi < _cosmCells.Count; vi++)
            {
                var q = _cosmCells[vi];
                if (q == null) continue;
                q.gameObject.SetActive(cosm && vi < _cosmCellOn.Count && _cosmCellOn[vi]);
            }
        }

        /// <summary>侧栏「已装备」那张：显示**当前卡组实际用的卡背**（选了显示选的、没选显示阵营默认）。</summary>
        void RefreshCosmeticDrawer()
        {
            var back = CardArt.DeckCardback(State.Deck.CardbackId, FactionOf(State.Deck.WarlordId));
            if (_cosmDrawerBack == null && back != null)
            {
                _cosmDrawerBack = ImageQuad.Create(Root, back, Pos(DrawerX + DrawerW * 0.5f, DrawerY + DrawerH * 0.5f),
                                                   U(DrawerH), new Vector2(0.5f, 0.5f), "cosm_drawer");
                if (_cosmDrawerBack != null)
                {
                    _cosmDrawerBack.SetAspect(DrawerW / DrawerH);
                    _cosmDrawerBack.SetRenderQueue(QSide + 1);
                    _cosmOnly.Add(_cosmDrawerBack.gameObject);
                    RefreshTabVisibility();          // 刚建的这件要立刻按页签定显隐
                }
            }
            if (_cosmDrawerBack != null && back != null)
            {
                _cosmDrawerBack.SetTexture(back);
                _cosmDrawerBack.SetAspect(DrawerW / DrawerH);
            }
            if (_cosmDrawerName != null)
                _cosmDrawerName.SetText(string.IsNullOrEmpty(State.Deck.CardbackId)
                                        ? "默认卡背（没选过）" : State.Deck.CardbackId);
            // ⚠️ `Empty Collection Warning` 的显隐**不在这里** —— 它是 `_cosmOnly` 的一员，
            //    由 `RefreshTabVisibility` 一处判（两处各写一次迟早不一致）。
        }

        /// <summary>督军 id → 阵营（取「默认卡背」要它）。查不到返回空串。</summary>
        public string FactionOf(string warlordId)
        {
            var d = State.Find(warlordId);
            return d != null ? d.Faction : "";
        }

        /// <summary>装备一张卡背（**等价原版右键那一下**：`editingDeck.cardbackId = item.GetID()`）。
        /// 返回是否真的变了。</summary>
        public bool EquipCardback(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (CardArt.Cosmetic(name) == null) return false;       // 不认识的卡背名：不写进存档（不静默失败）
            if (State.Deck.CardbackId == name) return false;
            State.Deck.CardbackId = name;
            CommitDeck();
            RefreshCosmetics();
            RefreshDeckList();
            Say("卡背已换成 " + name);
            return true;
        }

        /// <summary>点卡背格：**照原版只有右键装备**（`DeckEditingWindow__OnCosmeticClick.c`）。
        /// 左键什么都不做 —— 这是原版行为，不是漏了。
        /// 命中用**与摆位同一条式子**反算行列（`ToPx` 是 `Pos` 的严格逆函数），不另存一份矩形。</summary>
        bool HandleCosmeticClick(Vector2 px, bool right)
        {
            if (_tab != 2) return false;
            if (px.x < CosmoX || px.x > CosmoX + CosmoW) return false;
            if (px.y < CosmoY || px.y > CosmoY + CosmoH) return false;
            var names = CardArt.CosmeticNames();
            if (names.Length == 0) return false;
            int c = Mathf.FloorToInt((px.x - (CosmoX + CosmoPadX)) / CosmoCellW);
            if (c < 0 || c >= CosmoCols) return false;          // 落在两侧留白里
            int r = Mathf.FloorToInt((px.y - CosmoY + _cosmScroll) / CosmoCellH);
            int idx = r * CosmoCols + c;
            if (idx < 0 || idx >= names.Length) return false;    // 最后一行之后的空白
            if (right) EquipCardback(names[idx]);
            return true;                                        // 左键也吃掉（原版就是什么都不做）
        }

        // ------------------------------------------------------------ 导入卡组弹窗（原版 `ImportDeckPopup`）

        /// <summary>原版 `ImportDeckPopup` 的 rect（`资料/说明书/04_界面UI/卡组界面说明书.md` §五）。
        /// ⚠️ **Confirm 那两个数是改过的**，理由写在下面。</summary>
        void BuildImportPopup()
        {
            const float wx = 560f, wy = 234f, ww = 800f, wh = 452f;
            Img("imp_bg", "40k_popup", wx, wy, ww, wh, QModal);
            _impTitle = Txt("imp_title", "Paste your deck", 610f, 280f, 700f, 60f, 2, Ink, QModalText);
            // 输入框：dump 给的是 [610,370] 700×141
            _impInputBg = Img("imp_input_bg", "40K_dropdown_bg", 610f, 370f, 700f, 141f, QModalRow);
            _impInputTx = Txt("imp_input", "Enter text...", 630f, 370f, 660f, 141f, 2,
                              new Color(1f, 1f, 1f, 0.5f), QModalText);
            _impErr = Txt("imp_err", "", 593f, 528f, 734f, 35f, 1, new Color(0.95f, 0.45f, 0.4f), QModalText);
            // ⚠️ Confirm：dump 说 `[354,615] 478×75` —— **x 落在窗口（560..1360）外面**，
            //    又是「VLG 布局前的模板位」（同 `Clear filters` 那条）⇒ 取**窗口内水平居中** = 721；
            //    y 取 615 会让按钮**冒出窗口下沿 4px**，改成**贴窗口底** = 686−75 = 611。
            // 🆕 A24：原版 `Import Deck Popup/Window/Buttons/Generic UI Button` 也是 `SpriteSwap`
            //   （`m_TargetGraphic` = 自己那层 Image · HL=`40K_button_hover` · P=`40K_button_pressed`，
            //    `WindowButton` 的两张表推得出）
            Hover("imp_ok", Img("imp_ok", "40K_button", 721f, 611f, 478f, 75f, QModalRow), "40K_button");
            _impOkTx = Txt("imp_ok_t", "Confirm", 721f, 611f, 478f, 75f, 2, Ink, QModalText);
            Img("imp_close", "UI_Button_Round_background", 1317f, 202f, 75f, 75f, QModalRow);
            // 🆕 A24：这颗的 `m_TargetGraphic` 实测 = **子件 `Icon`**（就是这张 `40k_bt_close`），
            //   **不是**按钮自己那个圆底 —— 普查 §二 块 B 写的「常态图 = `UI_Button_Round_background`」
            //   是**按钮节点**的 Image；本轮逐字段复读 prefab：
            //   `targetGo = Icon` · `sprite = 40k_bt_close` · HL=`40k_bt_close_hover` · P=`40k_bt_close_pressed`
            //   （照铁律 5 就地订正；两张高亮图 `WindowButton` 的表也推得出）。
            // 🔴 **2026-10-04（A37 ⑤）改尺寸**：那颗 `Icon` 原来我们画 **75×75**（跟着圆底走），
            //   原版实读（`menu_dump.py bundle_menus_assets_all "Import Deck Popup" --depth 8`）：
            //   `Generic Close Button Green` = `[1317.3,202.1]–[1392.3,277.1]`（**75×75 圆底，我们那层是对的**）·
            //   子件 `Icon` = **`[1326.6,212.4]–[1383.0,266.8]` = 56.37×54.50**，图 `40k_bt_close` 175×174 ·
            //   `Simple (1,1,1,1)` **无 preserveAspect** ⇒ 原版是**拉**进这个矩形的（我们照拉伸画）。
            Hover("imp_close",
                  Img("imp_close_x", "40k_bt_close", 1326.6f, 212.4f, 56.37f, 54.5f, QModalText - 1), "40k_bt_close");
            Btn_("imp_ok", 721f, 611f, 478f, 75f);
            Btn_("imp_close", 1317f, 202f, 75f, 75f);
            Btn_("imp_input", 610f, 370f, 700f, 141f);
            _modalOnly.Add(Lookup("imp_bg") != null ? Lookup("imp_bg").gameObject : null);
            foreach (var k in new[] { "imp_input_bg", "imp_ok", "imp_close", "imp_close_x" })
            { var q = Lookup(k); _modalOnly.Add(q != null ? q.gameObject : null); }
            // ⚠️ 标签同样要显式收（`Lookup()` 不认 Label —— 同 `info_*` 那条）
            foreach (var l in new[] { _impTitle, _impInputTx, _impErr, _impOkTx })
                if (l != null) _modalOnly.Add(l.gameObject);
            foreach (var go in _modalOnly) if (go != null) go.SetActive(false);
        }
        ImageQuad _impInputBg;
        Label _impInputTx, _impTitle, _impErr, _impOkTx;
        readonly List<GameObject> _modalOnly = new List<GameObject>();

        void BuildNotice()
        {
            _notice = Txt("notice", "", 640f, 1004f, 640f, 44f, 2, new Color(0.95f, 0.85f, 0.5f), QText);
            _storeErr = Txt("store_err", "", 640f, 966f, 640f, 34f, 1, new Color(0.95f, 0.5f, 0.4f), QText);
            _title = Txt("side_title", "卡组编辑", 0.3f, 160f, 326.9f, 0f, 2, Ink, QText);
        }

        // ============================================================ 刷新

        public void RefreshAll()
        {
            RefreshPool();
            RefreshDeckList();
            RefreshHeader();
            RefreshFilters();
        }

        /// <summary>卡组里那些条目（督军 / 防御卡 / 普通卡，顺序照原版 `DeckEditingPanel` 的三个槽）。</summary>
        public List<CardDef> DeckEntries()
        {
            var shown = new List<CardDef>();
            if (State.Deck.WarlordId != null) shown.Add(State.Find(State.Deck.WarlordId));
            if (State.Deck.DefensiveId != null) shown.Add(State.Find(State.Deck.DefensiveId));
            foreach (var id in State.Deck.CardIds) shown.Add(State.Find(id));
            shown.RemoveAll(x => x == null);
            return shown;
        }

        void RefreshDeckList()
        {
            bool cards = _tab == 0, info = _tab == 1;
            var shown = DeckEntries();

            float maxScroll = Mathf.Max(0f, shown.Count * RowPitch - ListH);
            _deckScroll = Mathf.Clamp(_deckScroll, 0f, maxScroll);
            int firstRow = Mathf.FloorToInt(_deckScroll / RowPitch);
            float off = _deckScroll - firstRow * RowPitch;

            for (int i = 0; i < RowVisible; i++)
            {
                bool on = cards && (firstRow + i) < shown.Count;
                SetOn(_deckRowBg[i], on); SetOn(_deckRowGrad[i], on);
                SetOn(_deckRowBorder[i], on); SetOn(_deckRowCntIc[i], on);
                SetOn(_deckRowCost[i], on);
                SetOn(_deckRowName[i], on); SetOn(_deckRowCount[i], on);
                if (!on) continue;

                var def = shown[firstRow + i];
                float y = ListY + i * RowPitch - off;
                MoveRow(i, y);
                _deckRowName[i].SetText(CardText.Name(def.Name, def.NameZh));
                int n = (def.Type == "hero" || def.Type == "defence") ? 1 : State.Deck.CountOf(def.Id);
                _deckRowCount[i].SetText(n.ToString());
                _deckRowGrad[i].SetTint(RarityTint(def.Rarity));
            }

            SetOn(_emptyWarn, cards && shown.Count == 0);
            if (_emptyWarnGo != null) _emptyWarnGo.SetActive(cards && shown.Count == 0);
            RefreshTabVisibility();

            var curve = State.CostCurve();
            for (int c = 0; c < _curveFill.Count; c++)
            {
                int n = c < curve.Length ? curve[c] : 0;
                SetQuadWidth(_curveFill[c], Mathf.Clamp01(n / 10f) * CurveBarW);   // 原版按 10 张归一化
                _curveLabel[c * 2 + 1].SetText(n.ToString());
            }
        }

        List<ImageQuad> AllRowParts(int i) { return null; }     // 已废弃（改用显式列表，见上）
        readonly Dictionary<string, ImageQuad> _named = new Dictionary<string, ImageQuad>();
        ImageQuad Lookup(string key) { ImageQuad q; return _named.TryGetValue(key, out q) ? q : null; }

        void MoveRow(int i, float y)
        {
            float cy = y + RowH * 0.5f;
            Move(_deckRowBg[i], ListX + RowW * 0.5f, cy, QRowBg);
            Move(_deckRowGrad[i], GradX + GradW * 0.5f, cy, QGrad);   // 右对齐那一段（别写 ListX + RowW/2）
            Move(_deckRowBorder[i], ListX + RowW * 0.5f, cy, QBorder);
            Move(_deckRowCost[i], RowCostX + RowCostS * 0.5f, cy, QRow);
            Move(_deckRowCntIc[i], 258f + 20f, y + 8f + (RowH - 16f) * 0.5f, QRow);
            MoveLabel(_deckRowName[i], 62f + 95f, cy, QText);
            MoveLabel(_deckRowCount[i], RowCostX + RowCostS * 0.5f, cy, QText);
        }

        void Move(ImageQuad q, float cx, float cy, int queue)
        {
            if (q == null) return;
            q.transform.localPosition = Pos(cx, cy);
            q.SetRenderQueue(queue);
        }

        /// <summary>整棵子树一起搬（九宫格那种：动根、并把 9 块的队列一起设）。</summary>
        void Move(GameObject go, float cx, float cy, int queue)
        {
            if (go == null) return;
            go.transform.localPosition = Pos(cx, cy);
            foreach (var q in go.GetComponentsInChildren<ImageQuad>(true)) q.SetRenderQueue(queue);
        }

        /// <summary>卡组行的**行矩形**（画布 px）—— 命中判定用它，**别拿九宫格里某一块 quad 的尺寸**。</summary>
        bool RowRectAt(int i, out float x, out float y)
        {
            x = ListX; y = ListY + i * RowPitch - _deckScroll;
            return y + RowH > ListY && y < ListY + ListH;
        }

        /// <summary>一个世界点是否落在第 `i` 行里（**按行矩形**判，与画法无关）。</summary>
        bool HitRow(int i, Vector3 wp)
        {
            float x, y;
            if (!RowRectAt(i, out x, out y)) return false;
            var p = ToPx(wp);
            return p.x >= x && p.x <= x + RowW && p.y >= y && p.y <= y + RowH;
        }

        // ---- 🆕 2026-10-04（A24）卡组行的悬停变暗（判据见 `RowHoverK`）----

        /// <summary>指针底下那一行（`-1` = 没有）。只在 **Cards 页 + 没在拖 + 导入弹窗没开** 时成立
        /// （其余情况下那几行要么不显示、要么被模态挡着）。</summary>
        int RowUnder(Vector2 px)
        {
            if (_tab != 0 || _dragging || _importOpen) return -1;
            if (px.x < ListX || px.x > ListX + RowW) return -1;
            for (int i = 0; i < _deckRowBg.Count; i++)
            {
                if (_deckRowBg[i] == null || !_deckRowBg[i].activeSelf) continue;
                float x, y;
                if (!RowRectAt(i, out x, out y)) continue;
                if (px.y >= y && px.y <= y + RowH) return i;
            }
            return -1;
        }

        /// <summary>鼠标那条路每帧调它（自检走 `UiRowHoverAt` 同一个函数）。变了才动材质。</summary>
        void UpdateRowHover(Vector2 px)
        {
            int hr = RowUnder(px);
            if (hr == _hoverRow) return;
            _hoverRow = hr;
            ApplyRowHover();
        }

        /// <summary>按 `_hoverRow` 给**行底**上/下悬停色。三条口径（出处 = 那段 `RowHoverK` 的注释）：
        /// ① 只有**非督军**那几行会变（督军行原版目标图是全透明的）；
        /// ② 只变**行底**那一件（UGUI `ColorTint` 只作用在 `m_TargetGraphic` 上）；
        /// ③ 常态 = 白（原版那张图 `m_Color = (1,1,1,1)`）。</summary>
        void ApplyRowHover()
        {
            var shown = DeckEntries();
            int firstRow = Mathf.FloorToInt(_deckScroll / RowPitch);
            for (int i = 0; i < _deckRowBg.Count; i++)
            {
                var go = _deckRowBg[i];
                if (go == null) continue;
                float k = 1f;
                if (i == _hoverRow)
                {
                    int idx = firstRow + i;
                    var def = (idx >= 0 && idx < shown.Count) ? shown[idx] : null;
                    if (def != null && def.Type != "hero") k = RowHoverK;
                }
                var c = new Color(k, k, k, 1f);
                foreach (var q in go.GetComponentsInChildren<ImageQuad>(true)) q.SetTint(c);
            }
        }

        // ---- 🆕 2026-10-04（A24）按钮的悬停 / 按下态派发（只借 `WindowButton` 那四个口）----

        /// <summary>指针底下那一颗**接了悬停换图**的按钮（没有 = null）。
        /// 🔴 **命中口径 = 和点击同一条**：都走 `TopKeyAt` → `ClickOrder`（那张表就是点击那条链的顺序）。
        ///   ⚠️ **2026-10-04（A37 ④）改**：这句声明原来**是错的** —— 那时点击走 `HandleButtons` 的 if 链、
        ///   悬停却走 `_btns` 的**登记顺序**（两套顺序；本窗矩形今天互不重叠所以看不出分叉，但那正是
        ///   「两处写同一条规则 = 迟早不一致」）。现在两处**共用 `ClickOrder` 一份**，声明与实现一致。
        /// 遮挡口径也照点击那条路（`HandlePointer` 里那三条早退）：导入弹窗是模态 ⇒ **只认弹窗自己那几颗**
        ///   （原版靠弹窗的全屏暗底吃射线，我们没建那块暗底 ⇒ 在这里显式挡，否则弹窗背后的钮会亮起来）；
        ///   左抽屉开着时它盖住的那条竖带不认；行拖拽中不认。</summary>
        WindowButton HoverTargetUnder(Vector2 px)
        {
            if (_dragging) return null;
            if ((FltStripOn || CosmoFltStripOn) && px.x < FltX + FltW) return DrawerHoverUnder(px);
            foreach (var k in ClickOrder)
            {
                WindowButton wb;
                if (!_hoverBtns.TryGetValue(k, out wb)) continue;   // 只认接了悬停换图的那几颗
                // 🔴 **模态件与普通件互斥**（导入弹窗开着时 `_modalOnly` 那一批才显示）：
                //   弹窗开着 ⇒ 只认 `imp_*`（原版靠弹窗的全屏暗底吃射线，我们没建那块暗底，在这里挡）；
                //   弹窗关着 ⇒ 反过来不认 `imp_*`（否则会在**看不见**的输入框/按钮上亮起来 —— 静默的那种错）。
                // 🔴 **2026-10-04（A41 ④）：这段判断已经收进 `HitBtn` → `KeyLive` 了**（唯一出处）——
                //   悬停与点击都走它，这里不用再判一次（两处写同一条规则 = 迟早不一致）。
                if (HitBtn(k, px)) return wb;
            }
            return null;
        }

        /// <summary>🆕 2026-10-05（A32②）抽屉里那几颗 `trans=1(ColorTint)` 件的悬停派发
        /// （搜索框 + 三个开关；判据见 `HoverTint` 上那两段）。
        /// 🔴 **命中口径 = 点击那张表**（`_fltHit` / `_cosmoFltHit`，与 `HandleFilterClick` / `HandleCosmoFltClick`
        ///   逐字同源）—— **不新开第二份矩形表**（两处写同一条规则 = 迟早不一致）。
        /// 抽屉只在**完全到位**时才认（`FltStripOn` / `CosmoFltStripOn`，与点击同一条判据）⇒
        ///   位移那 0.3 秒里鼠标划过不会点亮任何东西。</summary>
        WindowButton DrawerHoverUnder(Vector2 px)
        {
            Dictionary<string, WindowButton> reg;
            List<Btn> hits;
            if (FltStripOn) { reg = _fltHoverBtns; hits = _fltHit; }
            else if (CosmoFltStripOn) { reg = _cosmoHoverBtns; hits = _cosmoFltHit; }
            else return null;
            foreach (var b in hits)
            {
                WindowButton wb;
                if (!reg.TryGetValue(b.Key, out wb)) continue;      // 只认接了悬停的那几格
                if (px.x >= b.X && px.x <= b.X + b.W && px.y >= b.Y && px.y <= b.Y + b.H) return wb;
            }
            return null;
        }

        /// <summary>鼠标那条路每帧调它（自检走 `UiHoverAt` 同一个函数）：进 / 出悬停态。
        /// ⚠️ **不是** `PointerLayer.HoverAt` —— 为什么不给这扇窗建指针层，见 `Build()` 末尾那三条理由。</summary>
        void UpdateButtonHover(Vector2 px)
        {
            var h = HoverTargetUnder(px);
            if (h == _hoverBtn) return;
            if (_hoverBtn != null) _hoverBtn.Exit();
            _hoverBtn = h;
            if (_hoverBtn != null) _hoverBtn.Enter();
        }

        /// <summary>左键按下 / 抬起时给那一颗进 / 出按下态（原版 `m_SpriteState.m_PressedSprite`）。
        /// ⚠️ 本窗的**动作**是在**按下**那一刻做的（`HandleButtons`），与 UGUI「按抬同处才算点」不同 ——
        ///    那是本类既有的口径（见 `HandlePointer`）；按下态只是让**画面**跟一下。</summary>
        void UpdateButtonPress(bool down, bool released, Vector2 px)
        {
            if (_pressedBtn != null && (released || !down)) { _pressedBtn.Release(); _pressedBtn = null; }
            if (down && _pressedBtn == null)
            {
                var h = HoverTargetUnder(px);
                if (h != null) { _pressedBtn = h; h.Press(); }
            }
        }
        void MoveLabel(Label l, float cx, float cy, int queue)
        {
            if (l == null) return;
            l.transform.localPosition = Pos(cx, cy);
            l.SetRenderQueue(queue);
        }
        static void SetOn(Component c, bool on) { if (c != null) c.gameObject.SetActive(on); }
        static void SetOn(GameObject go, bool on) { if (go != null) go.SetActive(on); }

        void RefreshHeader()
        {
            _deckNameText.SetText(State.Deck.Name);
            _deckNameHint.gameObject.SetActive(string.IsNullOrEmpty(State.Deck.Name));
            _title.gameObject.SetActive(!_filtersOpen);

            // 🆕 2026-10-04（A24）：`Header/Filters` 是 `EverguildToggle`（**不是**按钮悬停那一套）——
            //   `changeSpriteOnValueChange = 1` · `m_IsOn = 0` · `offSprite = 40k_menu_bt` ·
            //   `onSprite = 40k_menu_bt_pressed` · `spriteToChange` = 自己那层 Image
            //   ⇒ **筛选面板开着时换成 `40k_menu_bt_pressed`**（原来恒画 `40k_menu_bt`：开着也不换图）。
            //   ⚠️ 它 `colorTintOnValueChange = 0` ⇒ 原版**不给它打 on/off 色偏**，状态只体现在图上。
            //   判据（逐字段实读 prefab）：`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-3758886955019145436.json`
            //   + 反编译 `EverguildToggle__RefreshVisuals` / `__ToggleSprite`。
            //   🔴 **「按下」= 当前这一页的抽屉开着** —— 原版 `Card Display`（`DeckEditorCollectionDisplay`）与
            //     `Cosmetic Display`（`CardbackCollectionDisplay`）的 `filterToggle` 字段**指的是同一颗**
            //     （两者都 = pid `-3758886955019145436`，就是这颗 `Header/Filters`）⇒ 卡背页开抽屉时
            //     页头这颗**也**该是按下的（铁律 5·c：一个值 ≠ 全部情况）。
            //
            // 🔴 **2026-10-05（A76②）把「逻辑态 vs 动画态」这条核实了 —— 结论：按【逻辑态】瞬时翻，我们已对。**
            //   原来这条挂着一句「按下态按逻辑态翻（0.3 秒错位）」，现在给出**判据链**（全在反编译里，逐条可复跑）：
            //   ① `EverguildToggle__OnPointerClick.c:19` **只调基类** `UnityEngine_UI_Toggle__OnPointerClick`
            //      —— UGUI 那颗的原话是 `Set(!m_IsOn, true)` ⇒ **`m_IsOn` 在「点下去」这一帧就翻了**，
            //      并**同步**广播 `onValueChanged`。
            //   ② `EverguildToggle__Awake.c` 尾段：把换图那个处理器 `AddListener` 到 `onValueChanged`
            //      （`+0x118` 那根 `UnityEvent<bool>`）⇒ **精灵图跟着 `m_IsOn` 立刻换**。
            //   ③ `EverguildToggle__ToggleSprite.c`：`spriteToChange.sprite = isOn ? onSprite : offSprite`
            //      —— **只读 `m_IsOn`，没有任何插值/计时**。
            //   ④ 那 **0.3 秒属于抽屉**、不属于按钮：`CardCollectionFilterController.animationTime = 0.3`
            //      + `hiddenPosition` → `CollectionFilterController.Toggle(bool,bool)` 里的 `DOAnchorPosX`。
            //   ⇒ 「点下去按钮立刻变按下图、面板还差 0.3 秒才滑到位」**就是原版的行为**，
            //     我们这句 `(_tab == 2 ? _cosmoFltOpen : _filtersOpen)` 与它**逐字同语义**（`_xxxOpen` 是逻辑态）
            //     ⇒ ⛔ **不要改成跟 `Slide`/`SlideTarget` 走**（那才会与原版不符）。
            //   ⚠️ 另一半（**悬停变色**）原来确实没接 —— 2026-10-05（A32②）补上了，见 `BuildHeader` 里那句 `HoverTint`。
            SetSprite(_hdrFltBtn, (_tab == 2 ? _cosmoFltOpen : _filtersOpen)
                                  ? "40k_menu_bt_pressed" : "40k_menu_bt");

            // 🔴 2026-10-04（A24）**侧栏三页签的选中态**：原版走 UGUI `Toggle.PlayEffect` ——
            //   `graphic.CrossFadeAlpha(m_IsOn ? 1 : 0, 0.1s)`，而这三颗的 `graphic` 实测 = **子件 `Highlight`**
            //   （`MonoBehaviour_-8573138721491521756.json` 的 `graphic.m_PathID` → 那个 `Image`，
            //     它的 GameObject 名就叫 `Highlight`，图 = `40k_main_bt_selected BW`）。
            //   ⇒ **未选中的高亮 alpha = 0**（原来我们打的是 **0.15** —— 屏幕上有一层 15% 的幽灵高亮
            //     压在两个未选中的页签上，`UiTabHighlightAlpha` 那条断言盯的就是这个 0）。
            //   ⚠️ **更正一句旧记录**：`资料/普查产出_1003/卡组编辑器_按钮悬停图_普查.md` §二 块 C 写的是
            //     「原版是切子件 `Highlight` 的 **`enabled`**」—— **不是 enabled**：那张 Image 出厂
            //     `m_Enabled = 1`（三颗页签都一样），UGUI 那条路是**画布 alpha**。
            //   ⚠️ 原版还有个 0.1s 的淡入淡出（`CrossFadeAlpha`），我们这里是**瞬时**（同 `WindowButton` 的现状）。
            // 🔴 **2026-10-04（A37 ①）改：那条高亮的颜色 —— 原版是【红】，我们打的是【白】。**
            //   判据（自己重跑，两处独立、互不依赖）：
            //   ① prefab 实读：子件 `Highlight` 的 Image `m_Color = (1, 0, 0, 1)` —— 就是
            //      `menu_dump.py … "Deck Editing Menu" --depth 6` 那行印的 `Sliced (1,0,0,1)`
            //      （同一行左边写着 `40k_main_bt_selected BW`：**这张图是灰度的，红全来自 tint**）。
            //   ② 通道语义：UGUI `Toggle.PlayEffect` → `graphic.CrossFadeAlpha(m_IsOn?1:0)` →
            //      `CrossFadeColor(…, useAlpha:true, useRGB:**false**)`
            //      （`PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Graphic.cs:1045-1048,1009`）
            //      ⇒ **只动 alpha，RGB 两个态都保持红**（`Toggle.cs:297-308`）。
            //   同工程早就做对过一次：`Shell/MenuWindowBase.cs:420-424` 用同一张图 + `(1,0,0,1)`。
            //   ⚠️ 我们原来打 `Color.white` ⇒ 屏幕上是一块**浅灰**（红线：与原版不符 ⇒ 完全复刻）。
            //   ⚠️ 未选中的那颗**也要带红**（RGB 恒为红、只有 alpha 在 0/1 之间）—— 见上面 ②。
            for (int i = 0; i < 3; i++)
            {
                bool cur = i == _tab;
                // ⚠️ **2026-10-04（A41 ⑥）：这一层现在是九宫格（9 块）** ⇒ 上色必须**整棵树一起**
                //   （只给 `_tabHi[i]` 那一块上色会留下另外 8 块保持上一态 —— 静默、只在画面上现形）。
                SetTabHighlightTint(i, new Color(1f, 0f, 0f, cur ? 1f : 0f));
                // 🔴 **2026-10-04（A46）：字色【照原版改成恒白】。**
                //   原版这三颗 `Text` 的 `m_fontColor` **恒为 `(1,1,1,1)`**（三个页签一样，实读 prefab：
                //   Cards `MonoBehaviour_-8616675548124614876.json` · Deck info `…1651772317734268708.json`
                //   · Cosmetics `…-6724143614169387228.json`，也可以看 `menu_dump.py … "Deck Editing Menu"`
                //   那三行的 `色=(1,1,1,1)`）⇒ **选中与未选中的字是同一个颜色**，区分**只靠身后那块红高亮**。
                //   那颗 `EverguildButtonMaterialModifier` 是遮罩/灰度用的 `GetModifiedMaterial`，**不改字色**。
                //   ⚠️ 我们原来打的是 `Gold : Ink` —— 那两个都是**我们自己挑的**，与原版不符。
                _tabLabel[i].SetColor(Color.white);
            }

            // 🔴 **2026-09-28 修：只数那 30 张卡，别把督军/防御卡加进去。**
            //   原版实拍（`卡组编辑界面参考.png`）右下角印的是 **`30/30`**，而那一屏的卡表里
            //   **只有卡组的 30 张**（督军在标题行、防御卡在它自己的格子里，都不在这张表里）。
            //   我们原来算的是 `卡数 + 督军(1) + 防御卡(1)` / `MaxDeckCount + 2` ⇒ 满编时印 **32/32**，**多 2**。
            //   判据出处：`资料/历史/五张参考图_逐件核对_0922.md` 第 10 条（那条挂着「要核」，2026-09-28 核完并改）。
            _counterTxt.SetText(State.DeckCount + "/" + State.MaxDeckCount);
            var err = State.Validate();
            _verdict.SetText(err == DeckError.None ? "合法" : DeckRules.Describe(err));
            _verdict.SetColor(err == DeckError.None ? new Color(0.5f, 0.9f, 0.5f) : new Color(0.95f, 0.6f, 0.4f));
            SetOn(_doneHl, err == DeckError.None);          // 原版 `Done Highlight` 就是这个开关
            _storeErr.SetText(Library.LastError ?? "");

            // 🔴 **2026-09-28 用户拍板：万能卡数字一律恒定 `99`**。
            // 原来这里写的是「卡池里四个稀有度各有几张」—— **语义是错的**：原版这 4 个数字是
            // `WildcardDisplay.Initialize(card.army)` 的**库存直出**（跟着**指针悬停那张卡**的阵营走）。
            // 单机没有发放源、外壳也没有 hover（`PointerLayer` 无悬停能力）⇒ **不自己算**，统一写 99。
            // 判据 → `项目任务.md` §三 第 15 条 第 29 项。
            // ⚠️ **2026-10-04 更正（铁律 5）**：括号里那句「`PointerLayer` 无悬停能力」**已经过期** ——
            //   它 2026-10-03（A17）起就有 `HoverAt`（悬停换图），本轮 A24 又给这扇窗接上了悬停换图
            //   （**没建指针层**，由本类 `HandlePointer` 派发 —— 理由见 `Build()` 末尾那段）。**但结论不变**：
            //   那 4 个数字仍然写 99，因为缺的是**发放源**（`WildcardDisplay` 的库存），不是悬停事件。
            for (int i = 0; i < 4; i++) _wcTxt[i].SetText("99");

            _poolInfo.SetText("卡池 " + State.VisibleCards().Count + " / " + State.PoolCount + " 张");
        }

        void Say(string s)
        {
            _noticeText = s ?? "";
            if (_notice != null) _notice.SetText(_noticeText);
            _noticeUntil = Time.unscaledTime + 2.5f;
            // 🆕 2026-09-24：**同时打一条日志** —— 这行字以前只画在屏幕上，日志里看不见，
            //    于是「点了一下、屏幕上闪了一句」在 `ClickLog` 的「实际触发了什么」那一栏里是**空的**。
            Debug.Log("[Deck] " + _noticeText);
        }

        // ============================================================ 交互：鼠标

        void HandlePointer()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 sp = mouse.position.ReadValue();
            Vector3 wp = LayoutSpace.ScreenToWorld(sp, _cam);
            Vector2 px = ToPx(wp);

            bool downL = mouse.leftButton.wasPressedThisFrame;
            bool downR = mouse.rightButton.wasPressedThisFrame;

            // 🆕 2026-10-04（A24）：卡组行的**悬停变暗**（原版 `Deck Selector *` 是 `trans=1(ColorTint)`，
            //   `m_HighlightedColor = 0.6887`）。每帧重算一次（≤11 行的矩形比较，代价可忽略），
            //   变了才动材质 —— 见 `ApplyRowHover` 里的判据。
            UpdateRowHover(px);
            // 🆕 同上：那 5 颗 `SpriteSwap` 按钮的悬停 / 按下态（判据见 `Build()` 末尾那段与 `HoverTargetUnder`）
            UpdateButtonHover(px);
            UpdateButtonPress(mouse.leftButton.wasPressedThisFrame, mouse.leftButton.wasReleasedThisFrame, px);

            // 🆕 **真实点击记录**（用户 2026-09-24；与 `PointerLayer` 那条同源）。
            //    这里只报「按**点击优先级**（`ClickOrder`），这一点上第一个吃到的是谁」（`TopKeyAt` 就是取第一个）
            //    —— **实际干了什么**由 `ClickLog` 同帧捕获的日志说话（这个界面的动作都留了日志）。
            if (ClickLog.Enabled && (downL || downR) && !_dragging)
            {
                ClickLog.Begin(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                               downR ? "DeckRuntime(右键)" : "DeckRuntime(左键)", px);
                var keys = ButtonKeysAt(px);
                ClickLog.Hit(keys.Count > 0
                             ? ("盖住这一点的候选（**按点击优先级 `ClickOrder`，第一个是赢家**）：" + string.Join("、", keys))
                             : "`_btns` 里没有一件盖住这一点（空白处）—— 除非拖拽/卡片视图自己处理，否则这一下不会有反应");
            }

            // ---- 拖拽中：让那一行跟着鼠标，松开时判定 ----
            if (_dragging && _dragQuad != null)
            {
                _dragQuad.transform.position = new Vector3(wp.x, wp.y, _dragQuad.transform.position.z);
                if (_dragText != null) _dragText.transform.position = new Vector3(wp.x, wp.y, _dragText.transform.position.z);
                if (!_draggingMoved && Mathf.Abs(wp.x - _dragOrigin.x) > 0.15f) _draggingMoved = true;
                if (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed) { EndDrag(px.x); return; }
                return;
            }

            if (!downL && !downR) return;

            // 导入弹窗是**模态** —— 开着的时候只认它（点别处不穿透）
            if (_importOpen) { if (downL) HandleButtons(px); return; }

            // 筛选栏**盖住侧栏**（两者是同一块 rect）⇒ 它开着的时候先问它、并且不许穿透。
            // 🆕 A67：判据走 `FltStripOn`（= 开着**且**已到位）—— **位移那 0.3 秒里不吃**，
            //   点击照常往下走（与收藏窗「位移期间这一栏的按钮全失效」同一个语义）。
            if (FltStripOn && px.x < FltX + FltW)
            {
                if (downL) { HandleFilterClick(px); return; }
                return;
            }
            // 🆕 2026-10-01：卡背页那套抽屉**也是同一块 rect**（两套不会同时开）⇒ 同样在这里拦
            if (CosmoFltStripOn && px.x < FltX + FltW)
            {
                if (downL) { HandleCosmoFltClick(px); return; }
                return;
            }

            if (HandlePoolClick(wp, px, downR)) return;
            if (HandleDeckRowClick(wp, px, downL)) return;
            // Cosmetics 页的卡背格：**右键装备 / 左键不做事**（原版 `OnCosmeticClick`，见那个方法）
            if (HandleCosmeticClick(px, downR)) return;
            if (downL && HandleButtons(px)) return;
        }

        /// <summary>卡池：**左键放大 / 右键加牌**（照原版 `DeckEditingWindow__OnCardClick.c:16,42`）。</summary>
        bool HandlePoolClick(Vector3 wp, Vector2 px, bool right)
        {
            if (px.x < PoolX || px.x > PoolX + PoolW) return false;
            for (int i = 0; i < _poolViews.Count; i++)
            {
                var v = _poolViews[i];
                if (!v.gameObject.activeSelf || i >= _poolIndex.Count || _poolIndex[i] < 0) continue;
                if (!v.Contains(wp)) continue;
                var def = State.VisibleAt(_poolIndex[i]);
                if (def == null) return true;
                if (right) TryAddCard(def);
                else OpenCardWindow(def);
                return true;
            }
            return false;
        }

        /// <summary>卡组条目：**左键放大**（原版**不是删除**）；按住拖出侧栏 = 删除。</summary>
        bool HandleDeckRowClick(Vector3 wp, Vector2 px, bool left)
        {
            if (_tab != 0) return false;
            if (px.x > ListX + RowW || px.y < ListY || px.y > ListY + ListH) return false;
            for (int i = 0; i < _deckRowBg.Count; i++)
            {
                var rowGo = _deckRowBg[i];
                if (rowGo == null || !rowGo.activeSelf || !HitRow(i, wp)) continue;
                if (!left) return true;
                var shown = DeckEntries();
                int idx = Mathf.FloorToInt(_deckScroll / RowPitch) + i;
                if (idx >= shown.Count) return true;
                StartRowDrag(idx, i, wp);
                return true;
            }
            return false;
        }

        /// <summary>记下拖拽起点。**鼠标与自检走同一个函数**（删牌那条路只有「拖出」一条，必须验到）。</summary>
        /// <summary>「返回」= **存盘之后离场**，回主菜单场景（原版是回收藏页、还在同一扇窗里 ——
        /// 我们是两个场景 ⇒ 见 `资料/阶段二_卡组线_原版规格.md` §七 那处偏离）。
        /// ⚠️ **批处理下不切场景**（自检要靠同一个进程跑完；切了会把后面的断言全带走）。
        /// ⚠️ `MainMenu` 必须在 Build Settings 里（`MainMenuScene.BuildAndSaveScene` 会加）。</summary>
        public void BackToMenu()
        {
            if (Application.isBatchMode) { Debug.Log("[Deck] （批处理：不切场景）返回 = 主菜单场景 `MainMenu`"); return; }
            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            else
                Debug.LogWarning("[Deck] 返回失败：`MainMenu` 不在 Build Settings 里（跑一次 `MainMenuScene.BuildAndSaveScene`）");
        }

        void StartRowDrag(int deckIndex, int viewRow, Vector3 fromWorld)
        {
            _dragRow = deckIndex;
            _dragQuad = _deckRowBg[viewRow];
            _dragText = _deckRowName[viewRow];
            _dragOrigin = fromWorld;
            _dragging = true;
            _draggingMoved = false;
        }

        void EndDrag(float screenX)
        {
            _dragging = false;
            int row = _dragRow;
            _dragQuad = null; _dragText = null; _dragRow = -1;

            // 没怎么动 = 点击 ⇒ 弹放大窗（照原版）
            if (!_draggingMoved)
            {
                var shown0 = DeckEntries();
                if (row >= 0 && row < shown0.Count) OpenCardWindow(shown0[row]);
                RefreshDeckList();
                return;
            }
            // 拖出侧栏 = 删除（照原版 `DeckEditingPanel__CheckCardSlotDrag.c:59,66`）
            // 🔴 `screenX` 是**设计坐标（1920 宽）**，不是屏幕像素 —— 传进来的是 `ToPx()` 的结果。
            //    原来这里又乘了一次 `Screen.width / ScreenW`（那是给真实像素用的）⇒
            //    批处理下 `Screen.width` 不是 1920，阈值被缩小，**拖到侧栏里面也会删**
            //    （自检抓到：往 x=150 拖，卡组照样少一张）。
            if (screenX > ListX + RowW)
            {
                var shown = DeckEntries();
                if (row >= 0 && row < shown.Count)
                {
                    var def = shown[row];
                    if (State.TryRemove(def)) { CommitDeck(); Say("已移出卡组：" + CardText.Name(def.Name, def.NameZh)); }
                }
            }
            RefreshAll();
        }

        /// <summary>🆕 2026-10-04（A37 ④）：**点击与悬停共用同一条命中顺序**（唯一出处）。
        /// 为什么要有它：原来**点击**走下面 `HandleButtons` 的 if 链、**悬停**走 `_btns` 的**登记顺序**，
        ///   而 `HoverTargetUnder` 的注释却写着「命中口径 = 和点击同一条」—— **声明与实现不符**。
        ///   本窗矩形今天互不重叠 ⇒ 两套顺序行为一致（看不出分叉），但照 `CLAUDE.md` 那条
        ///   「两处写同一条规则 = 迟早不一致」，不能留两份。
        /// 顺序 = 原 if 链**逐字照搬**（页签排最后：`tab_*` 的矩形最大，压到别人这件事只有它在最后才算对）。
        /// 🔴 **新接一颗悬停 / 点击按钮，必须把 key 写进这张表** —— `Editor/DeckScene.cs` 有一条断言盯着
        ///   「接了悬停换图的 key ⊆ 这张表」，漏写就红。</summary>
        static readonly string[] ClickOrder = {
            "hdr_filters", "hdr_clear", "hdr_back", "foot_done",
            "name_box", "name_clear", "info_share", "info_import",
            "imp_input", "imp_ok", "imp_close",
            "tab_0", "tab_1", "tab_2",
        };

        /// <summary>这一点上**按 `ClickOrder` 第一个吃到它的 key**（没有 = `null`）—— 点击与悬停都走它。</summary>
        string TopKeyAt(Vector2 px)
        {
            foreach (var k in ClickOrder) if (HitBtn(k, px)) return k;
            return null;
        }

        bool HandleButtons(Vector2 px)
        {
            switch (TopKeyAt(px))
            {
                case "hdr_filters": ToggleFilters(); return true;
                case "hdr_clear": ClearFilters(); return true;
                case "hdr_back": SaveAndSay(); BackToMenu(); return true;
                case "foot_done": SaveAndSay(); return true;
                case "name_box": BeginNameEdit(); return true;
                case "name_clear": State.SetDeckName("新卡组"); CommitDeck(); RefreshHeader(); return true;
                case "info_share": ShareDeckString(); return true;
                case "info_import": OpenImport(); return true;
                case "imp_input": _nameEdit = _importText ?? ""; _editKind = 3; RefreshImportText(); return true;
                case "imp_ok": TryImport(); return true;
                case "imp_close": CloseImport(); return true;
                case "tab_0": SetTab(0); return true;
                case "tab_1": SetTab(1); return true;
                case "tab_2": SetTab(2); return true;
                default: return false;      // 没命中（`TopKeyAt` 返回 null）
            }
        }

        void SaveAndSay() { CommitDeck(); Say("已保存"); }

        // ============================================================ 分享 / 导入

        /// <summary>分享 = **把卡组串写进系统剪贴板**（原版 `DeckInfoPopup__ShareDeck.c:16`
        /// 就一句 `GUIUtility.systemCopyBuffer = MakeDeckString()`）。
        /// ⚠️ 原版**只有写、没有读**（`get_systemCopyBuffer` 全库 0 命中）⇒ 我们也不做「一键从剪贴板导入」。</summary>
        void ShareDeckString()
        {
            var s = DeckLibrary.ExportString(State.Deck);
            GUIUtility.systemCopyBuffer = s;
            Say("卡组串已复制到剪贴板（" + s.Length + " 字符）");
        }

        void OpenImport()
        {
            _importOpen = true; _importText = ""; _importError = "";
            RefreshImportText();
            foreach (var go in _modalOnly) if (go != null) go.SetActive(true);
            Say("把卡组串粘进输入框（Ctrl+V），再点 Confirm");
        }

        void CloseImport()
        {
            _importOpen = false;
            if (_editKind == 3) { _nameEdit = null; _editKind = 0; _typed.Clear(); }
            foreach (var go in _modalOnly) if (go != null) go.SetActive(false);
        }

        /// <summary>走**原版那条链**：`Split(';').Last()` → `DeserializeDeckString`
        /// （我们这边是 `DeckLibrary.ImportString`，格式逐字对齐）。失败要给**人话**。</summary>
        public bool TryImport()
        {
            var deck = DeckLibrary.ImportString(_importText, State.Find);
            if (deck == null)
            {
                _importError = string.IsNullOrWhiteSpace(_importText) ? "先粘贴卡组串" : "这不是一条合法的卡组串";
                RefreshImportText();
                return false;
            }
            CloseImport();
            Library.Add(deck);
            State.LoadDeck(Library.Current);
            RefreshAll();
            Say("已导入「" + deck.Name + "」" +
                (DeckLibrary.LastDroppedIds.Count > 0
                 ? "（有 " + DeckLibrary.LastDroppedIds.Count + " 张卡在我们卡池里没有，已按原版丢掉）" : ""));
            return true;
        }

        void RefreshImportText()
        {
            if (_impInputTx == null) return;
            bool empty = _importText.Length == 0;
            _impInputTx.SetText(empty ? "Enter text..." : _importText);
            _impInputTx.SetColor(empty ? new Color(1f, 1f, 1f, 0.5f) : Ink);
            if (_impErr != null) _impErr.SetText(_importError ?? "");
        }

        /// <summary>Deck info 页签开着吗 —— **`_infoOnly` 那批件的显隐判据（唯一出处）**。
        /// `RefreshTabVisibility` 与 `KeyLive` 共用它；不许各写一遍 `_tab == 1`
        /// —— 那正是 A41 ④「看不见却能点」的来源（两处写同一条规则 = 迟早不一致）。</summary>
        bool InfoTab { get { return _tab == 1; } }

        /// <summary>页签决定「侧栏那三组东西谁显示」：卡组行(Cards) / 费用曲线+动作钮(Deck info) / 饰品页(Cosmetics)。</summary>
        void RefreshTabVisibility()
        {
            bool info = InfoTab, cosm = _tab == 2;
            foreach (var go in _infoOnly) if (go != null) go.SetActive(info);
            foreach (var go in _cosmOnly) if (go != null) go.SetActive(cosm);
            ApplyCosmCellVisibility();          // 卡背格（同一个判据的第二个消费者，见那个方法）
            RefreshCosmoFilters();              // 🆕 2026-10-01：卡背那个抽屉（同样的判据：只在 Cosmetics 页露）
            // `Empty Collection Warning`（原版判据：**过滤后为空**）——
            // ⚠️ 2026-10-01 更新：这一页**已经有筛选抽屉了**（`Cosmetic FIlter`），但 13 个阵营每个都有卡背
            //    （生成脚本实测分布 **9~20 张**）⇒ 仍然**永远非空** ⇒ 实际上不显示（与原版「没筛就不空」一致）。
            if (_cosmEmptyWarn != null) _cosmEmptyWarn.SetActive(cosm && CardArt.CosmeticNames().Length <= 0);
        }

        // ============================================================ 自检入口
        // 🔴 批处理里**没有真鼠标** ⇒ 自检从这几个口子直接驱动状态。
        //    鼠标回调调的是**同一个函数**（`HandleButtons`/`HandleScroll`/`HandleFilterRow` 都转发到这里）
        //    —— 「两处写同一条规则 = 迟早不一致」。
        public bool FiltersOpen { get { return _filtersOpen; } }
        public int ActiveTab { get { return _tab; } }
        public float PoolScrollPx { get { return _poolScroll; } }
        public float DeckScrollPx { get { return _deckScroll; } }
        public float MaxPoolScrollPx
        {
            get
            {
                int rows = Mathf.Max(1, Mathf.CeilToInt(State.VisibleCards().Count / (float)PoolCols));
                // 与 `RefreshPool` 里那条**同一条式子**（原来两处各写一遍、还差一个 −16 的拍脑袋项）
                return Mathf.Max(0f, rows * PoolRowPitch - PoolH);
            }
        }
        public float MaxDeckScrollPx { get { return Mathf.Max(0f, DeckEntries().Count * RowPitch - ListH); } }

        public void UiToggleFilters() { ToggleFilters(); }

        // ---- 🆕 2026-10-01：卡背页那个抽屉（`Cosmetic FIlter`）的自检读数 ----
        /// <summary>卡背抽屉开着没有。</summary>
        public bool CosmoFiltersOpen { get { return _cosmoFltOpen; } }
        /// <summary>抽屉里现在几格（关着 / 不在 Cosmetics 页 = 0）。开着 = **13 个阵营 + 1 个 Owned = 14**。</summary>
        public int UiCosmoFilterCellCount { get { return (_cosmoFltOpen && _tab == 2) ? _cosmoFltCells.Count : 0; } }
        /// <summary>某个 key 的格子（屏幕绝对 px + 选中态）。key = `$fac:<阵营名>` / `$owned`。</summary>
        public bool UiCosmoFilterCell(string key, out float x, out float y, out float w, out float h, out bool on)
        {
            foreach (var c in _cosmoFltCells)
                if (c.Key == key)
                {
                    var r = FltAbs(c.R.x1, c.R.y1, c.R.x2, c.R.y2);
                    x = r.CX; y = r.CY; w = r.W; h = r.H; on = c.On;
                    return true;
                }
            x = y = w = h = 0f; on = false;
            return false;
        }
        /// <summary>卡背页当前的阵营筛选（空 = 不限）。</summary>
        public string CosmoFilterArmy { get { return _cosmoFilter.Faction; } }
        /// <summary>卡背页当前的 `Owned only` 开关（原版出厂 = 开，所以这里出厂也是 true）。</summary>
        public bool CosmoFilterOwned { get { return _cosmoFilter.Owned; } }
        /// <summary>按当前筛选**会铺出来几张卡背**（判据走 `CardbackTable` 那一份，别在自检里另算）。</summary>
        public int UiCosmoShownCount
        { get { return CardbackTable.NamesFor(_cosmoFilter.Faction, CardArt.CosmeticNames()).Length; } }
        /// <summary>自检用：走一遍抽屉里的点击（`$fac:X` / `$owned`）。</summary>
        public void UiCosmoFilterRow(string key) { ApplyCosmoFilter(key); }
        public void UiSetTab(int t) { SetTab(t); }
        public void UiScrollPool(float dy) { _poolScroll = Mathf.Max(0f, _poolScroll + dy); RefreshPool(); RefreshHeader(); }
        public void UiScrollDeck(float dy) { _deckScroll = Mathf.Max(0f, _deckScroll + dy); RefreshDeckList(); }
        public void UiClearFilters() { ClearFilters(); }
        public void UiFilterRow(string key) { HandleFilterRow(key); }
        public void UiAddCard(CardDef d) { TryAddCard(d); }
        public void UiBeginNameEdit() { BeginNameEdit(); }
        /// <summary>改名：批处理下没有键盘 ⇒ 直接把缓冲设进去再走「提交」那条路（和回车是同一个函数）。</summary>
        public void UiCommitName(string name)
        {
            _nameEdit = name ?? ""; _editKind = 1;
            EndTextEdit(true);
        }

        /// <summary>通用「输入完回车」：改名(1) / 卡名筛选(2) / 导入框(3) 都走它。</summary>
        public void UiCommitEdit(string text)
        {
            if (_editKind == 0) return;
            _nameEdit = text ?? "";
            EndTextEdit(true);
        }

        /// <summary>通用「按 ESC 取消」。</summary>
        public void UiCancelEdit() { if (_editKind != 0) EndTextEdit(false); }

        /// <summary>卡池第 i 个格子现在显示的是哪张卡（没显示给 null）。</summary>
        public CardDef UiPoolCardAt(int view)
        {
            if (view < 0 || view >= _poolIndex.Count || _poolIndex[view] < 0) return null;
            return State.VisibleAt(_poolIndex[view]);
        }

        /// <summary>卡组列表第 i 行现在**显示**的是哪张卡（那一行不可见就给 null —— 页签不对也算不可见）。</summary>
        public CardDef UiDeckRowAt(int i)
        {
            if (_tab != 0) return null;
            var shown = DeckEntries();
            int idx = Mathf.FloorToInt(_deckScroll / RowPitch) + i;
            return (i >= 0 && i < RowVisible && idx >= 0 && idx < shown.Count) ? shown[idx] : null;
        }

        /// <summary>某个具名图在不在（自检用来点验「Header/页签/Footer 这些件真的建了」）。
        /// ⚠️ 也认**场景里同名的一棵子树** —— 行底/行描边现在是**九宫格**（`CreateNineSlice` 建的是根 + 9 块，
        /// 根不进 quad 登记表），所以不能只查 `Lookup(key)`（2026-09-23 踩）。</summary>
        public bool UiHasQuad(string key)
        {
            return Lookup(key) != null || (Root != null && Root.Find(key) != null);
        }

        /// <summary>世界坐标 → 画布 px（**静态**版：自检算版面用；`ToPx` 那份是实例版、走同一条式子）。
        /// 🔴 两处必须同一条式子 —— 2026-09-23 之前自检另抄了一份 `x*108+960`，已收口到 `LayoutSpace`。</summary>
        public static Vector2 PxOfWorld(Vector3 world)
        {
            return new Vector2(world.x * PxPerUnit + ScreenW * 0.5f, ScreenH * 0.5f - world.y * PxPerUnit);
        }

        /// <summary>卡池第 `i` 格的**卡位**矩形（自检比版面用）。
        /// ⚠️ 给的是**卡位**（262.5×384，原版值），**不是卡本身** —— 卡缩到 `PoolCardScale`，
        /// 那个由 `CardView` 的 `localScale` 管（自检另有 `CardViewScaleOf` 量它）。</summary>
        public bool UiPoolCellRect(int i, out float cx, out float cy, out float w, out float h)
        {
            cx = cy = w = h = 0f;
            var go = Root != null ? Root.Find("pool_" + i) : null;
            if (go == null || !go.gameObject.activeSelf) return false;
            var p = PxOfWorld(go.localPosition);
            cx = p.x; cy = p.y; w = CellW; h = CellH;
            return true;
        }



        /// <summary>某个具名图**显示出来了没有**（`UiHasQuad` 只问建没建）。</summary>
        /// <summary>🆕 2026-10-04（A67）：**`activeInHierarchy`，不是 `activeSelf`**。
        /// 左抽屉的两栏现在各挂在一个容器下（滑动的对象就是那个容器）⇒ 「露着没有」必须连父链一起看
        /// —— 只看 `activeSelf` 的话，整栏收起来时底板的 `activeSelf` **仍是真**（关的是容器）。
        /// ⚠️ 这是**更严**的判据，不是放松：原来那三处（`flt_bg` ×2 / `side_bg`）在旧结构下两者等价。
        /// （同族老账：`Shell/DeckInfoPopup.IsItemShown` 只看 `activeSelf` 那条 = X3 审查的 R13。）</summary>
        /// <summary>🔴 **2026-10-05（A57 ①）：加 `Root.Find` 兜底** —— 同族其它读数（`UiHasQuad` /
        /// `UiQuadRect` / `UiQuadCount` / `UiQueueOf` / `UiTextureName`）**都有**，只有它以前**只走 `Lookup`**
        /// （`_named` 只装 `Img()` 建的单块）⇒ 对**九宫格 / 子树**件（`hdr_sep` · `tab_hi*` · `name_bg` ·
        /// `row_*` · 抽屉里的 `flt_input`）**静默返回 `false` = 谎报「没显示」**（`UiHasQuad` 问「建没建」、
        /// 本条问「露没露」，两条不能一个真一个假）。
        /// ⚠️ 找法照 `UiNodeRect`（A67 那条）：**先找直接子物体、再往深处找** —— 抽屉那几件挂在容器
        /// （`flt_drawer` / `cosmoflt_drawer`）底下，只认 `Root.Find` 的话它们**照样**答 false。
        /// ⚠️ `_named` 命中时行为**一个字节不变**（`Lookup` 优先）⇒ 既有三处调用不受影响。</summary>
        public bool UiQuadActive(string key)
        {
            var q = Lookup(key);
            if (q == null && Root != null && !string.IsNullOrEmpty(key))
            {
                Transform go = Root.Find(key);
                if (go == null)
                {
                    var deep = FindDeep(Root, key);
                    if (deep != null) go = deep.transform;
                }
                if (go != null) q = go.GetComponentInChildren<ImageQuad>(true);
            }
            return q != null && q.gameObject.activeInHierarchy;
        }

        /// <summary>某个具名 `Label` 现在写的字（自检读它 —— `_named` 只登记 `ImageQuad`，文字得按名字找）。</summary>
        public string UiLabelText(string key)
        {
            if (Root == null || string.IsNullOrEmpty(key)) return null;
            var t = Root.Find(key);
            var lb = t != null ? t.GetComponent<Label>() : null;
            return lb != null ? lb.Text : null;
        }

        /// <summary>卡池第 `i` 格上那张卡的卡表项（自检算「张数条该写什么」用）。</summary>
        public CardDef UiPoolCellDef(int i)
        {
            if (i < 0 || i >= _poolIndex.Count) return null;
            int idx = _poolIndex[i];
            var all = State != null ? State.VisibleCards() : null;
            return (all != null && idx >= 0 && idx < all.Count) ? all[idx] : null;
        }

        /// <summary>某个可点矩形的 px 位置（自检比版面用）。</summary>
        public bool UiBtnRect(string key, out float x, out float y, out float w, out float h)
        {
            foreach (var b in _btns)
                if (b.Key == key) { x = b.X; y = b.Y; w = b.W; h = b.H; return true; }
            x = y = w = h = 0f; return false;
        }

        /// <summary>具名图的 px 中心与尺寸（自检比版面用）。
        /// 🆕 2026-10-04（A24）：`_named` 里没有的**九宫格/平铺那种「一棵小树」**改量它的**包围盒**
        /// （`name_bg` 从单块换成九宫格之后就要这条路；单块与九宫格的矩形语义在自检里是同一条）。</summary>
        public bool UiQuadRect(string key, out float cx, out float cy, out float w, out float h)
        {
            var q = Lookup(key);
            if (q == null) return UiNodeRect(key, out cx, out cy, out w, out h);
            // 🔴 **量世界坐标**（`transform.position`），不是 `localPosition`（2026-10-04，A67）——
            //    `_named` 里那几件现在有挂在容器下的（左抽屉两栏），`localPosition` 是**容器内**的
            //    ⇒ 整栏滑动时它会**纹丝不动**（= 读数说谎）。`Root` 在原点 ⇒ 没容器时两者本来就同值，
            //    所以这条改动对既有断言（`flt_bg` 那些）**一个数都不变**。
            var p = ToPx(q.transform.position);
            cx = p.x; cy = p.y; w = q.WorldW * PxPerUnit; h = q.WorldH * PxPerUnit;
            return true;
        }

        /// <summary>按名字**往深处**找一个节点（`Transform.Find` 只认直接子物体）。</summary>
        static GameObject FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (c.name == name) return c.gameObject;
                var r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }

        /// <summary>具名节点（九宫格 / 平铺那种**子树**）的 px 中心与尺寸 = 子树里所有 quad 的**包围盒**。
        /// 判据 = 世界坐标（九宫格那 9 块是**根的子物体**，它们的 `localPosition` 都在根附近 —— 同
        /// 「同一层不许压住」那条注释踩过的坑）。</summary>
        public bool UiNodeRect(string key, out float cx, out float cy, out float w, out float h)
        {
            cx = cy = w = h = 0f;
            if (Root == null || string.IsNullOrEmpty(key)) return false;
            // 🆕 2026-10-04（A67）：**先找直接子物体、再往深处找** —— 左抽屉那几件现在活在容器底下
            //   （`Root.Find` 只认直接子物体）；直接子物体优先 ⇒ 既有那些 key 找到的还是同一个。
            Transform go = Root.Find(key);
            if (go == null)
            {
                var deep = FindDeep(Root, key);
                if (deep != null) go = deep.transform;
            }
            if (go == null) return false;
            var qs = go.GetComponentsInChildren<ImageQuad>(true);
            if (qs == null || qs.Length == 0) return false;
            float x1 = float.MaxValue, x2 = float.MinValue, y1 = float.MaxValue, y2 = float.MinValue;
            foreach (var q in qs)
            {
                var p = PxOfWorld(q.transform.position);
                float hw = q.WorldW * PxPerUnit * 0.5f, hh = q.WorldH * PxPerUnit * 0.5f;
                x1 = Mathf.Min(x1, p.x - hw); x2 = Mathf.Max(x2, p.x + hw);
                y1 = Mathf.Min(y1, p.y - hh); y2 = Mathf.Max(y2, p.y + hh);
            }
            cx = (x1 + x2) * 0.5f; cy = (y1 + y2) * 0.5f; w = x2 - x1; h = y2 - y1;
            return true;
        }

        /// <summary>费用曲线那 9 根柱子现在可不可见（Deck info 页签才显示）。</summary>
        public bool UiCurveVisible { get { return _curveFill.Count > 0 && _curveFill[0] != null && _curveFill[0].gameObject.activeSelf; } }

        /// <summary>通配符计数条第 `i` 个**数字**的渲染矩形（自检用）。
        /// 判据 = `Label.WorldW/WorldH`（TMP **真测量**，不是我们传进去的框）。
        /// ⚠️ 原版那条 TMP 是 `VerticalAlignment = Capline` ⇒ 数字相对几何中心**略偏上**，
        /// 所以自检要按**渲染矩形**比、别按几何中心比（正本 §③ 末条）。</summary>
        public bool UiWcCounterRect(int i, out float cx, out float cy, out float w, out float h)
        {
            cx = cy = w = h = 0f;
            if (i < 0 || i >= 4 || _wcTxt[i] == null) return false;
            var p = ToPx(_wcTxt[i].transform.localPosition);
            cx = p.x; cy = p.y; w = _wcTxt[i].WorldW * PxPerUnit; h = _wcTxt[i].WorldH * PxPerUnit;
            return true;
        }

        /// <summary>通配符计数条第 `i` 个数字现在写的是什么（自检用）。</summary>
        public string UiWcText(int i) { return (i >= 0 && i < 4 && _wcTxt[i] != null) ? _wcTxt[i].Text : null; }

        /// <summary>一个具名节点（**含九宫格那种子树根**）里第一块 quad 的**贴图名** —— 自检查「用对了图没有」。</summary>
        public string UiTextureName(string key)
        {
            var q = Lookup(key);
            if (q == null && Root != null)
            {
                var go = Root.Find(key);
                if (go != null) q = go.GetComponentInChildren<ImageQuad>(true);
            }
            return q != null && q.Texture != null ? q.Texture.name : null;
        }

        /// <summary>一个具名节点里 quad 的**块数**（九宫格 = 9 ⇒ 用它判「是九宫格还是拉满」）。</summary>
        public int UiQuadCount(string key)
        {
            if (Root != null)
            {
                var go = Root.Find(key);
                if (go != null) return go.GetComponentsInChildren<ImageQuad>(true).Length;
            }
            return Lookup(key) != null ? 1 : 0;
        }

        /// <summary>一个具名节点里第一块 quad 的**渲染队列**（自检比层序用）。</summary>
        public int UiQueueOf(string key)
        {
            var q = Lookup(key);
            if (q == null && Root != null)
            {
                var go = Root.Find(key);
                if (go != null) q = go.GetComponentInChildren<ImageQuad>(true);
            }
            return q != null ? q.RenderQueue : -1;
        }

        /// <summary>🔴 用**合成坐标**走一遍鼠标那条路（批处理没有真鼠标）。
        /// `Ui*()` 那组只驱动状态、**验不到命中矩形**；这条专门验「点在哪儿、命中谁」。</summary>
        public bool UiClickPx(float x, float y)
        {
            var px = new Vector2(x, y);
            if (_importOpen) return HandleButtons(px);
            if (FltStripOn && px.x < FltX + FltW) return HandleFilterClick(px);
            if (CosmoFltStripOn && px.x < FltX + FltW) return HandleCosmoFltClick(px);
            return HandleButtons(px);
        }

        /// <summary>🆕 A41 ④：这一点上「按 `ClickOrder` 第一个吃到的 key」，`null` = 没有一件命中。
        /// 与点击/悬停**同一条**（都过 `HitBtn` → `KeyLive`：看不见的钮不算）—— 自检直接问它，
        /// 免得靠副作用反推「到底谁被点到了」。</summary>
        public string UiTopKeyAt(float x, float y) { return TopKeyAt(new Vector2(x, y)); }

        public bool ImportOpen { get { return _importOpen; } }
        public string ImportText { get { return _importText; } }
        public string ImportError { get { return _importError; } }
        public void UiOpenImport() { OpenImport(); }
        public void UiSetImportText(string s) { _importText = s ?? ""; RefreshImportText(); }
        public bool UiTryImport() { return TryImport(); }
        public void UiCloseImport() { CloseImport(); }
        public string UiShareString() { return DeckLibrary.ExportString(State.Deck); }
        /// <summary>Cosmetics 页那一组（卡背格 + 侧栏「已装备」+ 提示）现在显示着没有。
        /// ⚠️ 2026-09-24 改口径：原来是「那块**空态**显示出来了没有」——这一页现在是**真页面**了，
        ///    判据跟着变成「这一组里有没有东西在显示」（`UiCosmOnlyActive > 0`）。</summary>
        public bool UiCosmeticsVisible { get { return UiCosmOnlyActive > 0; } }

        // ---- 自检入口：Cosmetics 页（换卡背）----
        /// <summary>列数（= `floor(视口宽 ÷ 格宽)`，原版**不是** `_segments=4`）</summary>
        public int CosmoColsPx { get { return CosmoCols; } }
        /// <summary>视口矩形（原版 `Cosmetic Display/Scroll View`）</summary>
        public Vector4 CosmoView { get { return new Vector4(CosmoX, CosmoY, CosmoW, CosmoH); } }
        public float CosmoCellWpx { get { return CosmoCellW; } }
        public float CosmoCellHpx { get { return CosmoCellH; } }
        public float CosmoScrollPx { get { return _cosmScroll; } }
        public float MaxCosmoScrollPx
        {
            get
            {
                int rows = Mathf.Max(1, Mathf.CeilToInt(CardArt.CosmeticNames().Length / (float)CosmoCols));
                // 与 `RefreshCosmetics` 里那条**同一条式子**
                return Mathf.Max(0f, rows * CosmoCellH - CosmoH);
            }
        }
        /// <summary>当前卡组**装备的**卡背（空 = 没选过，用阵营默认）。原版 `CardDeck.cardbackId`。</summary>
        public string EquippedCardback { get { return State.Deck.CardbackId; } }
        /// <summary>侧栏抽屉里画的那张图的名字（自检拿它比对「装备后真的换了」）</summary>
        public string CosmeticDrawerTex
        {
            get { return (_cosmDrawerBack != null && _cosmDrawerBack.Texture != null) ? _cosmDrawerBack.Texture.name : "<无>"; }
        }
        /// <summary>一格卡背现在用的是哪张图（`vi` = 窗口内第几格）。取不到返回 "&lt;无&gt;"。</summary>
        public string CosmoCellTex(int vi)
        {
            return (vi >= 0 && vi < _cosmCells.Count && _cosmCells[vi] != null && _cosmCells[vi].Texture != null)
                ? _cosmCells[vi].Texture.name : "<无>";
        }
        public int CosmoCellShown { get { int n = 0; foreach (var q in _cosmCells) if (q != null && q.gameObject.activeSelf) n++; return n; } }
        public void UiScrollCosmetics(float dy) { _cosmScroll = Mathf.Max(0f, _cosmScroll + dy); RefreshCosmetics(); }
        /// <summary>走**鼠标那条路**点一下卡背格（`right` = 右键）。自检用它验命中矩形，
        /// 而不是直接调 `EquipCardback`（那验不到「点在哪儿」）。</summary>
        public bool UiClickCosmetic(float px, float py, bool right) { return HandleCosmeticClick(new Vector2(px, py), right); }
        /// <summary>Deck info 页签那两颗动作钮显示出来了没有。</summary>
        public bool UiInfoActionsVisible { get { var q = Lookup("info_import"); return q != null && q.gameObject.activeSelf; } }
        public bool UiModalVisible { get { var q = Lookup("imp_bg"); return q != null && q.gameObject.activeSelf; } }

        /// <summary>🔴 **「该藏的东西藏住了吗」的通用守卫**（图 + 文字都算）。
        /// 为什么单开一条：`Lookup()` 只认 `ImageQuad` ⇒ 只验图的话**文字会漏**，
        /// 而漏了文字的表现是「别的页签上飘着一行字」—— 2026-09-20 就是这么漏的。</summary>
        public int UiInfoOnlyActive { get { int n = 0; foreach (var g in _infoOnly) if (g != null && g.activeSelf) n++; return n; } }
        public int UiCosmOnlyActive
        {
            get
            {
                int n = 0;
                foreach (var g in _cosmOnly) if (g != null && g.activeSelf) n++;
                // ⚠️ **卡背格不在 `_cosmOnly` 里**（它的显隐由 `ApplyCosmCellVisibility` 一处判）
                //    ⇒ 这里要把它一起数上，否则「Cards 页签上不许露 Cosmetics 的东西」会漏掉最显眼的那 24 格。
                foreach (var q in _cosmCells) if (q != null && q.gameObject.activeSelf) n++;
                return n;
            }
        }
        public int UiModalActive { get { int n = 0; foreach (var g in _modalOnly) if (g != null && g.activeSelf) n++; return n; } }
        /// <summary>正在编辑文本（改名 / 卡名筛选 / 导入框）。</summary>
        public int UiEditKind { get { return _editKind; } }

        /// <summary>🔴 模拟「按住卡组第 <paramref name="viewRow"/> 行 → 拖到屏幕 x = <paramref name="toScreenX"/> → 松开」。
        /// **走的是鼠标那条路**（`StartRowDrag` → `EndDrag`），批处理没有真鼠标、这是唯一能验到
        /// 「拖出侧栏 = 删除」的办法（删牌只有这一条路，原版 `DeckEditingPanel__CheckCardSlotDrag.c:59,66`）。</summary>
        public bool UiDragDeckRow(int viewRow, float toScreenX)
        {
            var shown = DeckEntries();
            int idx = Mathf.FloorToInt(_deckScroll / RowPitch) + viewRow;
            if (viewRow < 0 || viewRow >= RowVisible || idx >= shown.Count) return false;
            var rowGo = _deckRowBg[viewRow];
            if (rowGo == null || !rowGo.activeSelf) return false;
            StartRowDrag(idx, viewRow, rowGo.transform.position);
            _draggingMoved = true;                 // 模拟「拖动过阈值」⇒ 松开时按拖出处理
            EndDrag(toScreenX);
            return true;
        }

        /// <summary>模拟「点一下卡组第 viewRow 行」（不动 ⇒ 松开时弹放大窗，照原版）。</summary>
        public bool UiClickDeckRow(int viewRow)
        {
            var shown = DeckEntries();
            int idx = Mathf.FloorToInt(_deckScroll / RowPitch) + viewRow;
            if (viewRow < 0 || viewRow >= RowVisible || idx >= shown.Count) return false;
            var rowGo = _deckRowBg[viewRow];
            if (rowGo == null || !rowGo.activeSelf) return false;
            StartRowDrag(idx, viewRow, rowGo.transform.position);
            EndDrag(rowGo.transform.position.x);       // 没动过 ⇒ 走「点击」那一支
            return true;
        }
        /// <summary>放大窗现在开着没有（`UiClickDeckRow` / 池卡左键都会开它）。</summary>
        public bool UiCardWindowVisible { get { return _cardWindow != null && _cardWindow.Visible; } }
        public string UiCardWindowTitle { get { return _cardWindow != null ? _cardWindow.ShownTitle : null; } }
        public void UiCloseCardWindow() { if (_cardWindow != null) _cardWindow.Hide(); }

        void ToggleFilters()
        {
            // 🆕 2026-10-01：**卡背页有自己的左抽屉**（原版 `Cosmetic FIlter` → `Army Filter` + `Owned Toggle`）。
            //    它和卡牌筛选栏**不是一套**（行都不一样：卡背这套只有 Army + Owned，而且行序相反）
            //    ⇒ 按 `_tab` 分派，别拿卡牌那七行去筛卡背（原版就是这么分的两棵 prefab）。
            // 🆕 2026-10-04（A67）：两条路都改成「**逻辑态先翻 → 重建内容 → 起滑动**」
            //    （顺序照 `Shell/CollectionWindow.ToggleFiltersNow`）—— 内容要在**容器还在原位**时建好，
            //    再由 `ApplyDrawerSlide` 按进度整栏挪走。
            if (_tab == 2)
            {
                _cosmoFltOpen = !_cosmoFltOpen;
                // 🆕 A24：页头那颗 `Filters` 的按下态**跟着当前页的抽屉**走（两页的 `filterToggle`
                //   在 prefab 里就是同一颗 —— 见 `RefreshHeader` 里那段判据）⇒ 这里也要刷一次头。
                RefreshHeader();
                RefreshCosmoFilters();
                StartDrawerSlide(_cosmoFltSlide);
                Debug.Log("[Deck] 卡背抽屉 " + (_cosmoFltOpen ? "打开" : "收起")
                          + "（原版 `DOAnchorPosX(rect, x, 0.3)`：收起 `hiddenPosition.x = -550` / "
                          + "展开 `originalAnchorPosition.x = -165` ⇒ **行程 -385px**、`animationTime` 0.3；"
                          + "进度 = " + _drawerSlideDesc(_cosmoFltSlide) + "）");
                return;
            }
            _filtersOpen = !_filtersOpen;
            RefreshHeader(); RefreshFilters();
            StartDrawerSlide(_fltSlide);
            Debug.Log("[Deck] 筛选栏 " + (_filtersOpen ? "打开" : "收起")
                      + "（同原版：**位移 -385px + 0.3 秒**，不是硬切；进度 = "
                      + _drawerSlideDesc(_fltSlide) + "）");
        }

        /// <summary>日志里那句进度的人话（`Open` 与 `Slide` 在动画期间会不同 —— 说清楚）。</summary>
        string _drawerSlideDesc(FilterDrawer d)
        {
            if (d == null) return "（没有这一份）";
            return d.Slide.ToString("F2") + " → " + d.SlideTarget.ToString("F0")
                   + (d.Slide == d.SlideTarget ? "（已到位）" : "（滑动中）");
        }

        void SetTab(int t)
        {
            _tab = Mathf.Clamp(t, 0, 2);
            _deckScroll = 0f;
            // ⚠️ 顺序：先 `RefreshDeckList`（它会 `RefreshTabVisibility`），再 `RefreshPool`
            //    —— 卡池的显隐判据里有 `_tab`（见 `RefreshPool`），切回 Cards 页要让它重开。
            RefreshDeckList(); RefreshPool(); RefreshHeader();
        }

        // ============================================================ 交互：滚轮

        void HandleScroll()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            float dy = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(dy) < 1f) return;
            Vector2 px = ToPx(LayoutSpace.ScreenToWorld(mouse.position.ReadValue(), _cam));
            float step = dy * 0.4f;

            if (FltStripOn && px.x < FltX + FltW)
            {
                // 可滚范围 = 内容高 − 可见高（内容高**随阵营数变**：Army 行高 = 它自己的内容高）
                float max = Mathf.Max(0f, FilterPanelModel.ContentHFor(State) - FltH);
                _fltScroll = Mathf.Clamp(_fltScroll - step, 0f, max);
                RefreshFilters();
            }
            else if (CosmoFltStripOn && px.x < FltX + FltW)
            {
                // 🆕 2026-10-01：卡背抽屉**不滚**（内容 628 < 抽屉 924，原版那棵树里也没有 Scroll View）
                // —— 但滚轮要**吃掉**，别穿透到后面的卡背网格上。
            }
            else if (_tab == 2) { _cosmScroll = Mathf.Max(0f, _cosmScroll - step); RefreshCosmetics(); }
            else if (px.x < SideBgX + SideBgW) { _deckScroll = Mathf.Max(0f, _deckScroll - step); RefreshDeckList(); }
            else { _poolScroll = Mathf.Max(0f, _poolScroll - step); RefreshPool(); RefreshHeader(); }
        }

        // ============================================================ 交互：键盘（改名）

        public bool NameEditing { get { return _nameEdit != null; } }
        public string NameBuffer { get { return _nameEdit; } }

        void BeginNameEdit() { _nameEdit = State.Deck.Name ?? ""; _editKind = 1; Say("改名中：输入后回车确认，ESC 取消"); }
        /// <summary>卡名筛选的输入框（原版 `CardNameFilter` 用 `TMP_InputField`）。</summary>
        void BeginFilterNameEdit() { _nameEdit = State.Filter.Name ?? ""; _editKind = 2; RefreshFilters(); Say("筛选卡名：输入后回车确认，ESC 取消"); }

        void HandleTyping()
        {
            var kb = Keyboard.current;
            if (kb == null || _nameEdit == null) return;

            if (kb.escapeKey.wasPressedThisFrame) { EndTextEdit(false); return; }
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) { EndTextEdit(true); return; }
            if (kb.backspaceKey.wasPressedThisFrame && _nameEdit.Length > 0)
                _nameEdit = _nameEdit.Substring(0, _nameEdit.Length - 1);

            // `onTextInput` 是 InputSystem 的回调，拿得到已按本地化处理的字符（中文输入法也能进来）
            for (int i = 0; i < _typed.Count; i++) if (_nameEdit.Length < 24) _nameEdit += _typed[i];
            _typed.Clear();

            if (_editKind == 1)
            {
                _deckNameHint.gameObject.SetActive(false);
                _deckNameText.SetText(_nameEdit + "_");
            }
            else if (_editKind == 3) { _importText = _nameEdit; RefreshImportText(); }
            else RefreshFilters();
        }
        readonly List<char> _typed = new List<char>();

        /// <summary>结束文本编辑。**提交与取消走同一个函数** —— 两条路各写一份迟早不一致。</summary>
        void EndTextEdit(bool commit)
        {
            int kind = _editKind;
            string buf = _nameEdit ?? "";
            _nameEdit = null; _editKind = 0;
            _typed.Clear();

            if (kind == 1)
            {
                if (commit)
                {
                    if (State.SetDeckName(buf)) { CommitDeck(); Say("卡组名已改"); }
                    else Say("名字不能是空的");
                }
                else Say("已取消改名");
                RefreshHeader();
            }
            else if (kind == 2)
            {
                var f = State.Filter;
                f.Name = commit ? buf.Trim() : f.Name;
                if (commit) ApplyFilter(f); else RefreshFilters();
                if (commit) Say(string.IsNullOrEmpty(f.Name) ? "已清空卡名筛选" : "卡名筛选：" + f.Name);
            }
            else if (kind == 3)
            {
                if (commit) _importText = buf.Trim();
                RefreshImportText();
            }
        }

        void OnEnable() { if (Keyboard.current != null) Keyboard.current.onTextInput += OnText; }
        void OnDisable() { if (Keyboard.current != null) Keyboard.current.onTextInput -= OnText; }
        void OnText(char c) { if (_nameEdit != null && !char.IsControl(c)) _typed.Add(c); }

        // ============================================================ 筛选栏

        void ClearFilters()
        {
            // 清空 = 回到「按卡组自动筛选」那套（`CardCollectionFilterController__SetFiltersToDeck.c:29`）
            // 🔴 **两个开关不动**（2026-09-28 审核抓出来的口径冲突）：原版 `showOnlyOwnedCards` 全仓
            //    只有 `ToggleShowOwnedCards` 一个写点，`Clear filters` 清的是 `filters[]` 那几件
            //    ⇒ 玩家关掉 Owned 再点清空，它不该自己跳回开。
            var f = DeckFilter.None;
            f.Owned = State.Filter.Owned;
            f.Upgradable = State.Filter.Upgradable;
            State.SetFilter(f);
            _fltScroll = 0f;
            RefreshPool(); RefreshFilters(); RefreshHeader();
            Say("已清空筛选");
        }

        void ApplyFilter(DeckFilter f)
        {
            State.SetFilter(f);
            _poolScroll = 0f;
            RefreshPool(); RefreshFilters(); RefreshHeader();
        }

        // ============================================================ 筛选抽屉（照原版七行）
        //
        // 🔴 **行的几何 / 文案 / 选项表只有一份**：`Core/FilterPanelModel.cs`（与收藏窗共用，
        //    出处 = `资料/普查产出_0923/A3_Cards页.md` §5·1 实读）。这里只做三件事：
        //    ① 面板内坐标 → 屏幕绝对坐标（加 `FltX/FltY`、减滚动量，**只此一处**）；
        //    ② 建/摆对象；③ 登记点击区。
        // ⚠️ 原版这七行在一个 `Scroll View` 里（内容 989.02 > 可见 924.1 ⇒ 可滚 64.92）；
        //    它那个遮罩（`UIMask`）我们**没建** —— 内容只溢出屏幕底 65px，看不见也不影响点击，如实记。
        // ⚠️ 行**只在抽屉开着时建**（关着的时候建 = 在不可见的父级上量 TMP，`AlignRightOn` 会摆错）。

        readonly List<FilterPanelModel.Cell> _fltCells = new List<FilterPanelModel.Cell>();
        readonly List<GameObject> _fltCellObjs = new List<GameObject>();
        readonly List<Btn> _fltHit = new List<Btn>();     // 抽屉里的点击区（**绝对 px，已减滚动量**）
        /// <summary>卡牌筛选抽屉里每一格的图示 quad（`Key` → quad）—— 同 `_cosmoFltQuads` 的道理。</summary>
        readonly Dictionary<string, ImageQuad> _fltCellQuads = new Dictionary<string, ImageQuad>();
        /// <summary>🆕 2026-10-05（A32④）：每一格的**标签**（`Key` → `Label`）—— 自检要量「左对齐后文字左缘在
        /// 原版那个 x 上」（两侧都在同一个 `flt_lab` 名字下 ⇒ 按名字找不唯一）。
        /// ⚠️ Army 行**没有**标签（模型里 `Label = null`）⇒ 那些 key 不在表里。</summary>
        readonly Dictionary<string, Label> _fltCellLabels = new Dictionary<string, Label>();

        /// <summary>面板内坐标 → 屏幕绝对坐标。**只此一处** ——
        /// 🔴 2026-09-23 收藏窗踩过：模型里一半加了面板原点一半没加 ⇒ **整排偏上 155.9px**。</summary>
        PxRect FltAbs(float x1, float y1, float x2, float y2)
        {
            return new PxRect(FltX + x1, FltY + y1 - _fltScroll, FltX + x2, FltY + y2 - _fltScroll);
        }

        // ⚠️ 这里原来还有一个 `FilterTint(bool on) => FilterPanelModel.ToggleTint(on)` ——
        //   **2026-10-05（A32③）删掉**：`ToggleTint` 现在**必须带那一行自己的 off 色**
        //   （`(bool on, Color off)`，逐行不同，见 `FilterPanelModel` 那三个常量）⇒ 单参那版
        //   一留就会有人拿它当「通用值」，正是铁律 5·c 要防的那种静默错。
        //   唯一调用方 `CellTint` 已改成带 `c.OffTint` 的写法。

        void RefreshFilters()
        {
            // 容器是 `BuildFilters()` 建的；万一将来有人调换了 `Build()` 里的顺序，**出声**而不是空引用。
            if (_fltSlide == null || _fltSlide.Node == null)
            {
                Debug.LogWarning("[Deck] `RefreshFilters` 在 `BuildFilters` 之前被调了 —— 这一次忽略"
                                 + "（抽屉容器还没建，`Build()` 里的顺序别动）");
                return;
            }
            // 🆕 2026-10-04（A67）：**显隐不再由这里逐层 `SetOn`** —— 唯一出处是 `ApplyDrawerSlide`
            //   （跟着滑动走：滑出去了才关、在滑的途中要活着）。这里只负责重建内容 + 登记命中区。
            _fltSlide.Open = _filtersOpen;         // 逻辑态（滑动目标由 `StartDrawerSlide` 设）
            DrawerHome(_fltSlide);                 // 建之前把容器摆回原位（`Label.AlignLeftOn` 减的是父级世界 x）
            PrepareDrawerForBuild(_fltSlide);      // 建之前容器必须是活的（TMP 在非激活对象上量不出尺寸）
            _fltHit.Clear();
            RefreshFilterInput();
            RefreshFilterCells();
            RefreshFilterTitles();
            ApplyDrawerSlide(_fltSlide, _fltSlide.Slide);   // 建完按进度摆：位置 / 显隐 / 命中
        }

        // ---- ① 搜索框那一行（原版 `CardNameFilter` → `Input Field` 281.28×40）----
        void RefreshFilterInput()
        {
            // 🔴 **显隐用 `live` 不用 `_filtersOpen`**（A67）：收起时 `_filtersOpen` 立刻翻假，
            //   但整栏还要滑出去 0.3 秒 —— 那 0.3 秒里搜索框得**还在**（要画），只是不再吃点击/滚轮。
            bool live = _filtersOpen || DrawerLive(_fltSlide);
            if (_fltInputRoot != null) _fltInputRoot.SetActive(live);
            SetOn(_fltInputText, live);
            SetOn(_fltInputIcon, live);
            // ⚠️ **命中区只在逻辑态开着时登记**（与原来一致）：收起途中不重登 ——
            //   反正 `ApplyDrawerSlide` 已经把重定位/命中开关这一步接管了。
            if (!_filtersOpen) return;

            PxRect inR, taR, icR;
            FilterPanelModel.NameRowRects(FltW, out inR, out taR, out icR);
            var r = FltAbs(inR.x1, inR.y1, inR.x2, inR.y2);
            if (_fltInputRoot != null) _fltInputRoot.transform.localPosition = Pos(r.CX, r.CY);

            // 空的时候画的是**占位符**（原版 `Placeholder` TMP 原文 `Search`），在输入态时显示缓冲 + 光标
            string cur = State.Filter.Name;
            string txt = _editKind == 2 ? (_nameEdit ?? "") + "_"
                                       : (string.IsNullOrEmpty(cur) ? FilterPanelModel.InputPlaceholder : cur);
            var tr = FltAbs(taR.x1, taR.y1, taR.x2, taR.y2);
            if (_fltInputText != null)
            {
                _fltInputText.SetText(txt);
                // 原版 `Placeholder/Text` 是 `auto(18–30)` ⇒ 长卡名在 231.28 宽的框里要缩，不许溢出到面板外
                _fltInputText.SetAutoFitBox(LayoutSpace.Px(tr.W), LayoutSpace.Px(tr.H),
                                            FilterPanelModel.InputFontAutoMin, FilterPanelModel.InputFontPx);
                _fltInputText.transform.localPosition = Pos(tr.CX, tr.CY);
                // 🔴 **必须左对齐**（原版 `Placeholder`/`Text` 在 `Text Area` 里是左对齐；
                //    收藏窗那份也是这么画的）—— 不摆的话 `Txt` 是**居中**，字会飘到框中间（2026-09-28 截图看出来的）
                _fltInputText.AlignLeftOn(LayoutSpace.FromPixel(tr.x1, 0f).x);
            }

            var ir = FltAbs(icR.x1, icR.y1, icR.x2, icR.y2);
            if (_fltInputIcon != null)
            {
                _fltInputIcon.transform.localPosition = Pos(ir.CX, ir.CY);
                _fltInputIcon.gameObject.SetActive(true);
            }
            _fltHit.Add(new Btn { Key = "$name", X = r.x1, Y = r.y1, W = r.W, H = r.H });
        }

        // ---- ②…⑦ 31 格（数量随卡池阵营数变 ⇒ 每次刷新重建）----
        void RefreshFilterCells()
        {
            // 🔴 **拆格的判据不是 `_filtersOpen`**（A67）：收起时 `_filtersOpen` 立刻翻假，而整栏还要滑出去
            //   0.3 秒 —— 那时把格拆掉 = 画面上只剩一块**空底板**在滑（原版是连格子一起滑走）。
            //   ⇒ 只有「**这一栏整个不在画面上了**」（既没收着也不算在滑）才拆。
            //   ⚠️ 所以收起之后 `_fltCells` 里可能还留着上一次建的那批 —— **读它之前先确认 `FiltersOpen`**
            //     （`UiFilterCellCount` 就是这么做的）。
            if (!_filtersOpen && !DrawerLive(_fltSlide)) { ClearFilterCells(); return; }
            if (!_filtersOpen) return;            // 收起途中：留着现成那批一起滑走，不重建
            ClearFilterCells();

            FilterPanelModel.Build(State, FltW, _fltCells);
            foreach (var c in _fltCells)
            {
                var b = FltAbs(c.Bg.x1, c.Bg.y1, c.Bg.x2, c.Bg.y2);
                if (b.y2 < FltY || b.y1 > FltY + FltH) continue;      // 滚出面板的不建
                // 🔴 2026-10-04（A24）：**开关那一类按状态换图** —— 原版那三颗是 `EverguildToggle`
                //   （`changeSpriteOnValueChange=1` · `onSprite=40_main_bt_toggle_on` · `offSprite=40_main_bt_toggle_off`
                //    · `spriteToChange` = 自己那个 `Image`）⇒ 关掉时**换成 off 那张**。
                //   原来恒画 `40_main_bt_toggle_on` 那张、只靠色偏 ⇒ **关掉的开关和打开的长得一模一样，只暗一点**。
                var tex = Ui(c.IconOff != null && !c.On ? c.IconOff : c.Icon);
                // 🔴 **图取不到就不登记点击区** —— 否则会出现「看不见却点得动」的空格
                //    （缺图由 `Ui()` 记账，最终由 `DeckScene` 的「一张不缺」断言兜住）
                if (tex == null) continue;
                var r = FltAbs(c.R.x1, c.R.y1, c.R.x2, c.R.y2);
                _fltHit.Add(new Btn { Key = c.Key, X = r.x1, Y = r.y1, W = r.W, H = r.H });   // 点击区 = 格
                float w = b.W, h = Mathf.Max(1f, b.H);
                // 原版 `m_PreserveAspect`：按图自身宽高比**内接**进框、中心不动（与 `Img(keepAspect)` 同一条）
                if (tex.height > 0 && w > 0f) { float sa = (float)tex.width / tex.height, ra = w / h; if (sa > ra) h = w / sa; else w = h * sa; }
                var q = ImageQuad.Create(FltParent, tex, Pos(b.CX, b.CY), U(h), new Vector2(0.5f, 0.5f), "flt_cell");
                if (q != null)
                {
                    q.SetAspect(w / h);
                    q.SetRenderQueue(QFltRow);
                    q.SetTint(CellTint(c));
                    // 🆕 2026-10-05（A32②）：**开关那一类**（`IconOff != null`）原版还有悬停变暗 ——
                    //   三颗 `EverguildToggle` 都是 `m_Transition = 1(ColorTint)`、`m_Colors` = UGUI 默认那组、
                    //   目标件 = 子件 `Image`（`m_Color=(1,1,1,1)` · `m_Enabled=1`）⇒ 悬停乘 0.9607843。
                    //   ⚠️ 这四个**选项行**的格子（Army/Rarity/Cost/Type）**不接** —— 它们 `m_Transition = 0(None)`，
                    //     原版悬停什么都不变（见 `HoverTint` 那段「不接的」）。
                    //   ⚠️ `SetTint` 必须在**第一次悬停之前**跑完（`Collect()` 第一次 `ApplyTint` 时抓基准色）
                    //     ⇒ 这里 `SetTint` 在前、`HoverTint` 在后，**别把两行调过来**（同 `Deck Name` 那条）。
                    if (c.IconOff != null) HoverTint(c.Key, q.gameObject, _fltHoverBtns);
                    _fltCellObjs.Add(q.gameObject);
                    if (!string.IsNullOrEmpty(c.Key)) _fltCellQuads[c.Key] = q;   // 自检读它（`UiFilterCellTex`）
                }

                if (string.IsNullOrEmpty(c.Label)) continue;
                var lr = FltAbs(c.Lab.x1, c.Lab.y1, c.Lab.x2, c.Lab.y2);
                var lb = Label.Create(FltParent, c.Label, Pos(lr.CX, lr.CY), 1, CellTint(c),
                                      new Vector2(0.5f, 0.5f), "flt_lab");
                if (lb == null) continue;
                if (!string.IsNullOrEmpty(c.Key)) _fltCellLabels[c.Key] = lb;   // 自检读它（`UiFilterCellLabelLeft`）
                lb.SetRenderQueue(QFltText);
                lb.SetGlyphHeight(LayoutSpace.Px(c.LabelPx));
                // 原版那几行是 `auto(min-max)`：**不开自适应的话 `Legendary` 在 100px 格里冲出去**
                if (c.LabelAutoMin > 0f) lb.SetAutoFitBox(LayoutSpace.Px(lr.W), LayoutSpace.Px(lr.H), c.LabelAutoMin, c.LabelPx);
                // 🔴 **2026-10-05（A32④）订正**：原来这句写「两个开关行的标签原版是 **hAlign=Center**（A3 §5·1）
                //   ⇒ 居中时不要再摆对齐」—— **`Center` 那个读数是错的**：全包 8 个 `Owned only`/`Upgradable only`
                //   的 `m_HorizontalAlignment` 实测都是 `1`(Left)（两扇窗都读过）
                //   ⇒ 模型里 `LabelCenter` 已改成 `false`，于是这三行走 `AlignLeftOn(lr.x1)`。
                if (!c.LabelCenter)
                {
                    float wx = c.LabelRight ? LayoutSpace.FromPixel(lr.x2, 0f).x : LayoutSpace.FromPixel(lr.x1, 0f).x;
                    if (c.LabelRight) lb.AlignRightOn(wx); else lb.AlignLeftOn(wx);
                }
                _fltCellObjs.Add(lb.gameObject);
            }
        }

        /// <summary>把抽屉里那 31 格的图与字全拆掉（**只在「这一栏整个不在了」时调**）。</summary>
        void ClearFilterCells()
        {
            foreach (var go in _fltCellObjs) DestroySafe(go);
            _fltCellObjs.Clear();
            _fltCells.Clear();
            _fltCellQuads.Clear();
            _fltCellLabels.Clear();
            // 🆕 2026-10-05（A32②）：格子上那几颗 `WindowButton` 跟着 quad 一起销毁了 ⇒
            //   **登记也要撤**（不撤的话 `DrawerHoverUnder` 会 `Enter()` 一颗已销毁的组件 = 报异常）。
            //   ⚠️ **只撤格子那两个 key** —— `$name`（搜索框）是**常驻件**（`BuildFilterFixedParts` 建一次），
            //     它不在 `_fltCellObjs` 里、也不该被这次清理带走（一起 `Clear()` 会让搜索框从此不再亮）。
            _fltHoverBtns.Remove("$owned");
            _fltHoverBtns.Remove("$upgradable");
        }

        // ---- 四行小标题（`Title` TMP · fs32 · **hAlign=Left/Middle**）----
        void RefreshFilterTitles()
        {
            // 🔴 同 `RefreshFilterInput`：显隐用 `live`（A67 —— 收起的那 0.3 秒里四个小标题也得在）；
            //   而**摆位**仍只在逻辑态开着时做（位置本来就固定，收起途中不必重算）。
            bool live = _filtersOpen || DrawerLive(_fltSlide);
            foreach (var lb in _fltTitles) SetOn(lb, live);
            if (!_filtersOpen) return;
            var titles = new List<FilterPanelModel.Title>();
            FilterPanelModel.BuildTitles(State, FltW, titles);
            for (int i = 0; i < _fltTitles.Count && i < titles.Count; i++)
            {
                var r = FltAbs(titles[i].R.x1, titles[i].R.y1, titles[i].R.x2, titles[i].R.y2);
                _fltTitles[i].transform.localPosition = Pos(r.CX, r.CY);
                // 🔴 **2026-10-05：左对齐和摆位【一起】做** —— 原版这四行 `Title` 是 `Left/Middle`
                //   （判据 = `FilterPanelModel.TitleFontPx` 那段，两扇窗逐行实读）。
                //   ⚠️ 为什么不能只在**建的时候**对齐一次：`_fltTitles` 里的 `Label` 是**复用的**，
                //   而这个方法在**每次滚动**时都要重新摆一遍（`localPosition = Pos(...)` 会把 x 也写回中心）
                //   ⇒ 「建时对齐」滚一下就散。所以对齐必须挂在**同一处**、写在摆位后面。
                if (titles[i].Left) MenuDraw.AlignLeft(_fltTitles[i], r);
            }
        }

        bool HandleFilterClick(Vector2 px)
        {
            foreach (var b in _fltHit)
                if (px.x >= b.X && px.x <= b.X + b.W && px.y >= b.Y && px.y <= b.Y + b.H)
                { HandleFilterRow(b.Key); return true; }
            return false;
        }

        // ============================================================ 卡背页那套抽屉（`Cosmetic FIlter`）

        /// <summary>画卡背页那个抽屉。内容只有两行（Army 13 格 + Owned 开关），
        /// 全高 `15 + 550 + 12.81 + 50 ≈ 628` < 抽屉 924.06 ⇒ **不用滚动**（原版那棵树里也没有 Scroll View）。
        /// 几何全在 `FilterPanelModel`（与卡牌那套同一份尺子）。</summary>
        void RefreshCosmoFilters()
        {
            // ⚠️ **显隐只在这一处判**（`_cosmoFltOpen` × `_tab == 2`）—— 切页签时也要跟着收
            //    （原版那棵 `Cosmetic FIlter` 挂在 `Cosmetic Display` 底下，那一页不开它就不在画面上）。
            //    🆕 A67 把这一条拆成两半：
            //      · **页在不在** = `PageOn`（**瞬时**，照原版「整页 `SetActive`」的语义）；
            //      · **抽屉开没开** = `Open` / `Slide`（**跨页签保留**，开合走那 0.3 秒滑动）。
            //    ⚠️ 如实标注：「切页签时不放动画」是**我们挑的** —— 原版那条 `OnEnable → ToggleFilters(bool)`
            //       在页签回来时会不会重跑一次 tween，判不出来（泛型方法体缺失，见 §A67 开头那段）。
            // ⚠️ 容器是 `BuildFilters()` 建的（`Build()` 里它排在 `BuildCosmeticsPage` 之前）；
            //    万一将来有人调换了顺序，这里**出声**而不是空引用（不许静默失败）。
            if (_cosmoFltSlide == null || _cosmoFltSlide.Node == null)
            {
                Debug.LogWarning("[Deck] `RefreshCosmoFilters` 在 `BuildFilters` 之前被调了 —— 这一次忽略"
                                 + "（抽屉容器还没建，`Build()` 里的顺序别动）");
                return;
            }
            _cosmoFltSlide.PageOn = (_tab == 2);
            _cosmoFltSlide.Open = _cosmoFltOpen;
            DrawerHome(_cosmoFltSlide);
            PrepareDrawerForBuild(_cosmoFltSlide);

            if (_tab == 2 && _cosmoFltOpen)
            {
                ClearCosmoFlt();                       // 内容随筛选/刷新重建（与卡牌那栏同一口径）
                FilterPanelModel.BuildCosmetics(State.Factions(), _cosmoFilter, FltW, _cosmoFltCells);
                foreach (var c in _cosmoFltCells)
                {
                    var b = FltAbs(c.Bg.x1, c.Bg.y1, c.Bg.x2, c.Bg.y2);
                    // 🔴 2026-10-04（A24）：卡背页那颗 `Owned Toggle` 同样**按状态换图**（判据同卡牌那一套：
                    //   原版 `… > Cosmetic FIlter > Filters > Owned Toggle` 的 `offSprite = 40_main_bt_toggle_off`）。
                    var tex = Ui(c.IconOff != null && !c.On ? c.IconOff : c.Icon);
                    // 同卡牌那套：**图取不到就不登记点击区**（不做「看不见却点得动」的空格）
                    if (tex == null) continue;
                    var r = FltAbs(c.R.x1, c.R.y1, c.R.x2, c.R.y2);
                    _cosmoFltHit.Add(new Btn { Key = c.Key, X = r.x1, Y = r.y1, W = r.W, H = r.H });
                    float w = b.W, h = Mathf.Max(1f, b.H);
                    if (tex.height > 0 && w > 0f) { float sa = (float)tex.width / tex.height, ra = w / h; if (sa > ra) h = w / sa; else w = h * sa; }
                    var q = ImageQuad.Create(CosmoFltParent, tex, Pos(b.CX, b.CY), U(h), new Vector2(0.5f, 0.5f), "cosmoflt_cell");
                    if (q != null)
                    {
                        q.SetAspect(w / h);
                        q.SetRenderQueue(QFltRow);
                        q.SetTint(CellTint(c));
                        // 🆕 2026-10-05（A32②）：卡背页那颗 `Owned Toggle` 同样**有悬停变暗**
                        //   （`m_Transition = 1` · `m_Colors` 默认 · 目标件 = 子件 `Image` 白且启用）。
                        //   ⚠️ 登记进**卡背那份表**（`_cosmoHoverBtns`）—— 两栏可以同时存在，key 又都是 `$owned`。
                        if (c.IconOff != null) HoverTint(c.Key, q.gameObject, _cosmoHoverBtns);
                        _cosmoFltObjs.Add(q.gameObject);
                        if (!string.IsNullOrEmpty(c.Key)) _cosmoFltQuads[c.Key] = q;
                    }
                    if (string.IsNullOrEmpty(c.Label)) continue;
                    var lr = FltAbs(c.Lab.x1, c.Lab.y1, c.Lab.x2, c.Lab.y2);
                    var lb = Label.Create(CosmoFltParent, c.Label, Pos(lr.CX, lr.CY), 1, CellTint(c),
                                          new Vector2(0.5f, 0.5f), "cosmoflt_lab");
                    if (lb == null) continue;
                    lb.SetRenderQueue(QFltText);
                    lb.SetGlyphHeight(LayoutSpace.Px(c.LabelPx));
                    if (c.LabelAutoMin > 0f) lb.SetAutoFitBox(LayoutSpace.Px(lr.W), LayoutSpace.Px(lr.H), c.LabelAutoMin, c.LabelPx);
                    if (!c.LabelCenter)
                    {
                        float wx = c.LabelRight ? LayoutSpace.FromPixel(lr.x2, 0f).x : LayoutSpace.FromPixel(lr.x1, 0f).x;
                        if (c.LabelRight) lb.AlignRightOn(wx); else lb.AlignLeftOn(wx);
                    }
                    _cosmoFltObjs.Add(lb.gameObject);
                }
            }
            else if (!(_tab == 2 && DrawerLive(_cosmoFltSlide)))
            {
                ClearCosmoFlt();                       // 整栏整个不在了 ⇒ 拆
            }
            // else：收起途中（`_tab == 2` + 还在滑）⇒ **留着现成那批**一起滑走（同卡牌那栏）

            ApplyDrawerSlide(_cosmoFltSlide, _cosmoFltSlide.Slide);   // 摆位 / 显隐 / 命中（唯一出处）
        }

        /// <summary>把卡背抽屉那几个格拆掉（只在「这一栏整个不在了」时调）。</summary>
        void ClearCosmoFlt()
        {
            foreach (var go in _cosmoFltObjs) DestroySafe(go);
            _cosmoFltObjs.Clear();
            _cosmoFltCells.Clear();
            _cosmoFltQuads.Clear();
            _cosmoFltHit.Clear();
            // 🆕 2026-10-05（A32②）：`$owned` 那颗悬停件跟着 quad 一起销毁了 ⇒ 登记也要撤
            //   （理由同 `ClearFilterCells`；卡背这一栏里**没有常驻件**，清空是安全的）。
            _cosmoHoverBtns.Clear();
        }

        bool HandleCosmoFltClick(Vector2 px)
        {
            foreach (var b in _cosmoFltHit)
                if (px.x >= b.X && px.x <= b.X + b.W && px.y >= b.Y && px.y <= b.Y + b.H)
                { ApplyCosmoFilter(b.Key); return true; }
            return false;
        }

        /// <summary>卡背抽屉里的点击：`$fac:X` = 换阵营（**再点一次 = 取消**）· `$owned` = 开关。
        /// 🔴 `Owned` 在我们这儿**不改变结果**（单机全解锁，原版出厂它就是开的）—— 不静默，如实标在注释与自检里。
        /// 只刷**卡背格**（`RefreshCosmetics`），**不碰卡池**（两套筛选条件分开，见 `_cosmoFilter` 的注释）。</summary>
        void ApplyCosmoFilter(string key)
        {
            var f = _cosmoFilter;
            if (key == "$owned") f.Owned = !f.Owned;
            else if (key != null && key.StartsWith("$fac:")) { var v = key.Substring(5); f.Faction = f.Faction == v ? "" : v; }
            else return;
            _cosmoFilter = f;
            _cosmScroll = 0f;                 // 筛选一变就回到顶部（否则可能停在一片空白上）
            RefreshCosmoFilters();
            RefreshCosmetics();
        }

        // ---- 自检用的读数（`DeckScene` 拿它盯原版参数；格子没进 `_named`，所以单开这一组）----

        // ============================================================ 🆕 2026-10-04（A67）滑动那半份的自检口
        /// <summary>卡牌那栏的滑动进度（`0` = 已滑出 · `1` = 原位；还没建给 `−1`）。</summary>
        public float FilterDrawerSlide { get { return _fltSlide != null ? _fltSlide.Slide : -1f; } }
        /// <summary>卡背那栏的滑动进度（同上）。</summary>
        public float CosmoFilterDrawerSlide { get { return _cosmoFltSlide != null ? _cosmoFltSlide.Slide : -1f; } }
        /// <summary>这一栏**现在参不参与命中/滚轮**（**只有完全到位才真** —— 位移期间是假）。</summary>
        public bool FilterDrawerInteractive { get { return _fltSlide != null && _fltSlide.Interactive; } }
        public bool CosmoFilterDrawerInteractive { get { return _cosmoFltSlide != null && _cosmoFltSlide.Interactive; } }
        /// <summary>这一栏里**现在还画着几个格子**（收起的那 0.3 秒里要 &gt; 0 —— 原版是连格子一起滑走）。</summary>
        public int UiFilterCellObjects { get { return _fltCellObjs.Count; } }
        /// <summary>自检口：把某一栏**钉在某个进度**上（真的摆节点 + 开关命中）。
        /// ⚠️ **不动 `SlideTarget`**（那是逻辑目标）—— 所以「钉完还能被 `TickDrawers` 接着推」。</summary>
        public void SetDrawerProgressForTest(bool cosmo, float t)
        {
            ApplyDrawerSlide(cosmo ? _cosmoFltSlide : _fltSlide, t);
        }
        /// <summary>自检口：推一帧（`dt` 秒）—— 与 Play 那条 `Update → TickDrawers(Time.deltaTime)`
        /// 走**同一个** `StepDrawer`（不是两份实现）。</summary>
        public void TickDrawersForTest(float dt) { TickDrawers(dt); }
        /// <summary>抽屉里现在有几个格子（**关着时是 0**）。开着一共 **31** 个：
        /// 2（Owned/Upgradable）+ Army 13 + Rarity 5 + Cost 8 + Type 3。</summary>
        public int UiFilterCellCount { get { return _filtersOpen ? _fltCells.Count : 0; } }

        /// <summary>搜索框里**现在显示的那行字**（没输入卡名时 = 占位符 `Search`）。自检盯画面用。</summary>
        public string UiFilterInputText { get { return _fltInputText != null ? _fltInputText.Text : null; } }

        /// <summary>某个 key 的格子：**屏幕绝对 px**（**中心 x/y + 宽高**，与 `UiQuadRect` 同口径）+ 选中态。
        /// key 形如 `$owned` / `$upgradable` / `$rar:legendary` / `$cost:8` / `$fac:Ultramarines` / `$type:unit`。
        /// ⚠️ **只在 `FiltersOpen` 时读它**：A67 起「收起的那 0.3 秒」格子是**留着**的（要连格子一起滑走）
        ///   ⇒ 抽屉关着时 `_fltCells` 里可能还压着上一次建的那批（`On` 是**旧值**）。
        ///   `UiFilterCellCount` 就是这么做的（关着恒报 0）。</summary>
        public bool UiFilterCell(string key, out float x, out float y, out float w, out float h, out bool on)
        {
            foreach (var c in _fltCells)
                if (c.Key == key)
                {
                    var r = FltAbs(c.R.x1, c.R.y1, c.R.x2, c.R.y2);
                    x = r.CX; y = r.CY; w = r.W; h = r.H; on = c.On;
                    return true;
                }
            x = y = w = h = 0f; on = false;
            return false;
        }

        /// <summary>🆕 A24：某个筛选格**现在贴的是哪张图**（自检盯「状态→换图」；格子没进 `_named`，读这一份）。
        /// 只认卡牌那套抽屉；卡背抽屉那颗 `$owned` 见 <see cref="UiCosmoFilterCellTex"/>。</summary>
        public string UiFilterCellTex(string key)
        {
            ImageQuad q;
            if (key != null && _fltCellQuads.TryGetValue(key, out q) && q != null && q.Texture != null)
                return q.Texture.name;
            return null;
        }

        /// <summary>🆕 2026-10-05（A32②）自检用：抽屉**命中表**里某个 key 的矩形（绝对 px，已减滚动量）。
        /// 卡牌栏与卡背栏两张表都查。`false` = 现在没有这一格（抽屉没开 / 正在收 / 那一栏没建）。
        /// ⚠️ 搜索框 `$name` **不在 `_fltCells` 里**（它是常驻件）⇒ `UiFilterCell` 查不到它，用这一个。
        /// ⚠️ 同 `UiFilterCell`：**只在抽屉开着时读**（收起那 0.3 秒留着旧值）。</summary>
        public bool UiDrawerHitRect(string key, out float x, out float y, out float w, out float h)
        {
            return HitRectIn(_fltHit, key, out x, out y, out w, out h)
                || HitRectIn(_cosmoFltHit, key, out x, out y, out w, out h);
        }
        static bool HitRectIn(List<Btn> list, string key, out float x, out float y, out float w, out float h)
        {
            foreach (var b in list)
                if (b.Key == key) { x = b.X; y = b.Y; w = b.W; h = b.H; return true; }
            x = y = w = h = 0f;
            return false;
        }

        /// <summary>🆕 2026-10-05（A32④）自检用：某个筛选格**标签的文字左缘**（屏幕绝对 px）。
        /// 判据 = 原版那颗 `Label` 的 **`m_HorizontalAlignment = 1`(Left)** + 它自己的 rect 左缘
        /// （`$owned` 那两颗 = 面板内 x **25** ⇒ 绝对 **27.18**）。
        /// ⚠️ 量的是 `center − WorldW/2`（**文字左缘**，不是节点位置）—— 同 `CollectionScene` 那条
        /// 「量渲染真值」的做法；`false` = 这一格没有标签（Army 行）或量不出宽度（TMP 在非激活对象上会量成 0）。</summary>
        public bool UiFilterCellLabelLeft(string key, out float px)
        {
            px = 0f;
            Label lb;
            if (key == null || !_fltCellLabels.TryGetValue(key, out lb) || lb == null) return false;
            float w = lb.WorldW * PxPerUnit;
            if (w <= 1f) return false;                        // 量到 0 ⇒ 别报一个假坐标（那也是「TMP 没量到」的症状）
            px = PxOfWorld(lb.transform.position).x - w * 0.5f;
            return true;
        }

        /// <summary>🆕 2026-10-05（A32③）自检用：某个筛选格**图示现在什么 tint**（`(0,0,0,0)` = 没有这一格）。
        /// 自检拿它比对**原版 `offColor` 的字面量**（逐行不同，见 `FilterPanelModel.OffTint*`）。</summary>
        public Color UiFilterCellTint(string key)
        {
            ImageQuad q;
            if (key != null && _fltCellQuads.TryGetValue(key, out q) && q != null) return q.Tint;
            return new Color(0f, 0f, 0f, 0f);
        }

        /// <summary>🆕 A24：卡背抽屉里某一格现在贴的是哪张图（卡背那套）。</summary>
        public string UiCosmoFilterCellTex(string key)
        {
            ImageQuad q;
            if (key != null && _cosmoFltQuads.TryGetValue(key, out q) && q != null && q.Texture != null)
                return q.Texture.name;
            return null;
        }

        /// <summary>🆕 A24：侧栏第 `i` 个页签的**选中高亮**现在什么状态（alpha）—— 原版 `Toggle.graphic`
        /// （= 子件 `Highlight`）的 `PlayEffect` 把未选中的压到 **0**。取不到返回 `-1`。
        /// ⚠️ **只看得见 alpha ⇒ 对颜色是瞎的**（A37 ① 那笔账）：要连 RGB 一起验，用 `UiTabHighlightTint`。</summary>
        public float UiTabHighlightAlpha(int i)
        {
            return (i >= 0 && i < _tabHi.Count && _tabHi[i] != null) ? _tabHi[i].Tint.a : -1f;
        }

        /// <summary>🆕 A37 ①：侧栏第 `i` 个页签高亮的**整条 tint（RGB + alpha）**。取不到 = `(0,0,0,-1)`。
        /// 为什么要有这一条：原版那层是**灰度图 + 红色 tint**，而 `UiTabHighlightAlpha` 只比 alpha
        /// ⇒ **把 tint 改成白色也照样全绿**（这正是 A37 ① 之前那一版断言的问题）。
        /// 原版 prefab 实读：三颗 `Highlight` 的 Image **`m_Color` 都是 `(1,0,0,1)`**（Cards / Info / Cosmetics
        /// **同值**），出厂差别只在 Toggle 的 `m_IsOn`（1/0/0）。
        /// 🔴 **2026-10-04 订正（R-W4 F5）**：原来这里写「`(1,1,1,1)`（选中）/ `(1,1,1,1)`（未选中）」
        ///   —— **`(1,1,1,1)` 不是 prefab 实读**，那是我照「未选中就把它藏起来」的推理写的。
        ///   真值如上：**三颗同色，两个态在 prefab 里没有差别**。
        /// ✅ **「未选中的怎么藏起来」已经解出**（顺着 A24 那条 UGUI 路补完，出处逐条可查）：
        ///   · 那颗 `EverguildToggle` 的 `colorTintOnValueChange = 0` / `changeSpriteOnValueChange = 0`
        ///     ⇒ **不走** `EverguildToggle.ToggleTint`（`CanvasRenderer.SetColor(onColor/offColor)`）那条路
        ///     （该分支的条件正是 `colorTintOnValueChange`，`EverguildToggle__RefreshVisuals.c:7-11`）；
        ///   · 剩下的就是 UGUI 标准的 `Toggle.PlayEffect`：`graphic.CrossFadeAlpha(m_IsOn ? 1 : 0, 0.1s,
        ///     只动 alpha)`，而那颗 `graphic` 的 PathID `-8989903718472880348` **就是 `Highlight` 子件的
        ///     Image**（按 `m_Component` 反查 owner GO = `Highlight`）⇒ **未选中 = 画布 alpha 0**。
        ///   ⇒ 我们实现的就是这条路（下面是读回我们自己的值，不是原版值）。
        /// ⚠️ **2026-10-04（A41 ⑥）：这一层是九宫格（9 块）** ⇒ 这里读的是**树里第一块**；
        ///   上色一律走 `SetTabHighlightTint`（整棵树），别用 `_tabHi[i].SetTint` 只改一块。</summary>
        public Color UiTabHighlightTint(int i)
        {
            return (i >= 0 && i < _tabHi.Count && _tabHi[i] != null)
                 ? _tabHi[i].Tint : new Color(0f, 0f, 0f, -1f);
        }

        /// <summary>🆕 A41 ⑥：给第 `i` 个页签的高亮**整棵九宫格树**上色（9 块一起）。
        /// 单独开一个写口的原因：`_tabHi[i]` 只是「树里第一块」，
        /// 直接对它 `SetTint` 会**留下另外 8 块**保持上一态（静默 —— 只在画面上看得见）。</summary>
        void SetTabHighlightTint(int i, Color c)
        {
            if (i < 0 || i >= _tabHiRoot.Count || _tabHiRoot[i] == null) return;
            foreach (var q in _tabHiRoot[i].GetComponentsInChildren<ImageQuad>(true)) q.SetTint(c);
        }

        /// <summary>🆕 A41 ⑥：第 `i` 个页签高亮那棵树里**几块**（原版 `Sliced` ⇒ 9；拉满的单块 = 1）。
        /// 判据用它盯「九宫格没被退回单块拉伸」。</summary>
        public int UiTabHighlightBlocks(int i)
        {
            return (i >= 0 && i < _tabHiRoot.Count && _tabHiRoot[i] != null)
                 ? _tabHiRoot[i].GetComponentsInChildren<ImageQuad>(true).Length : 0;
        }

        /// <summary>🆕 A41 ②：第 `i` 个页签名牌上那行字**现在实际生效的字号**（画布 px 口径）。
        /// 原版那三颗是 `m_enableAutoSizing=1` + `m_fontSizeMin/Max = 10/34` ⇒ 结果必然落在 `[10,34]` 内，
        /// 但**「落在区间内」抓不到任何错**（区间就是它自己的定义域）⇒ 自检那边断的是
        /// 「**要么停在 34、要么被压到框的边界上**」（A51 F3 换过的判据，见 `DeckScene`）。取不到返回 `-1`。</summary>
        public float UiTabLabelFontPx(int i)
        {
            return (i >= 0 && i < _tabLabel.Count && _tabLabel[i] != null) ? _tabLabel[i].FontPxNow : -1f;
        }

        /// <summary>🔴 **2026-10-04（A46）**：第 `i` 个页签名牌上那行字**现在的字色**（取不到 = `(0,0,0,-1)`）。
        /// 判据 = 原版那三颗 `Text` 的 `m_fontColor` **恒为 `(1,1,1,1)`**：
        /// 三个页签**同色**、**选中与未选中也同色**（区分只靠身后那块红高亮，见 `RefreshHeader`）——
        /// 所以断言钉的是「**两态都是纯白**」，而不是「选中金、未选中灰」那种我们自编的配色。</summary>
        public Color UiTabLabelColor(int i)
        {
            return (i >= 0 && i < _tabLabel.Count && _tabLabel[i] != null)
                 ? _tabLabel[i].color : new Color(0f, 0f, 0f, -1f);
        }

        /// <summary>🆕 A41 ②：第 `i` 个页签名牌上那行字的**渲染矩形**（`Label.WorldW/WorldH` = TMP 真测量）。
        /// 断言拿它验「字真的落在名牌那个框里」（宽 ≤ 98.96、高 ≤ 40）。</summary>
        public bool UiTabLabelRect(int i, out float cx, out float cy, out float w, out float h)
        {
            cx = cy = w = h = 0f;
            if (i < 0 || i >= _tabLabel.Count || _tabLabel[i] == null) return false;
            var p = ToPx(_tabLabel[i].transform.localPosition);
            cx = p.x; cy = p.y;
            w = _tabLabel[i].WorldW * PxPerUnit; h = _tabLabel[i].WorldH * PxPerUnit;
            return true;
        }

        /// <summary>🆕 A24：第 `i` 行的**行矩形**（屏幕 px：左 / 上 / 宽 / 高）—— 自检算「指针该落哪」用。
        /// 与命中判定**同一条** `RowRectAt`（别在自检里再抄一遍行距/行高）。</summary>
        public bool UiRowRect(int i, out float x, out float y, out float w, out float h)
        {
            x = y = w = h = 0f;
            float rx, ry;
            if (!RowRectAt(i, out rx, out ry)) return false;
            x = rx; y = ry; w = RowW; h = RowH;
            return true;
        }

        /// <summary>🆕 A24：自检入口 —— 把指针挪到画布 px `(px,py)` 上，**走鼠标那条路的同一条函数**
        /// （`UpdateButtonHover` → `HoverTargetUnder`）。返回悬停到的那一颗（没有 = `null`）。</summary>
        public WindowButton UiHoverAt(float px, float py)
        {
            UpdateButtonHover(new Vector2(px, py));
            return _hoverBtn;
        }

        /// <summary>🆕 A37 ④：自检用 —— ① 现在**接了悬停换图**的 key（`_hoverBtns` 的键）；
        /// ② 点击与悬停**共用的那张命中顺序表**（`ClickOrder`）。
        /// 断言 = **前者必须是后者的子集** —— 漏写进 `ClickOrder` 的那颗会变成
        /// 「亮得起来但点不到 / 点得到但亮不起来」两套口径（静默分叉，`UiHoverAt` 那条断言也看不见）。</summary>
        public List<string> UiHoverKeys() { return new List<string>(_hoverBtns.Keys); }
        public static string[] UiClickOrder() { return (string[])ClickOrder.Clone(); }

        /// <summary>🆕 A24：自检入口 —— 左键按下 / 抬起（走 `UpdateButtonPress`）。返回此刻按着的那一颗。</summary>
        public WindowButton UiPressAt(float px, float py, bool down, bool released)
        {
            UpdateButtonPress(down, released, new Vector2(px, py));
            return _pressedBtn;
        }

        /// <summary>🆕 A24：自检入口 —— 把「指针底下那一行」设到画布 px `(px,py)` 上，**走鼠标那条路的同一条函数**
        /// （`UpdateRowHover` → `RowUnder`）。返回现在压着第几行（`-1` = 没有）。</summary>
        public int UiRowHoverAt(float px, float py) { UpdateRowHover(new Vector2(px, py)); return _hoverRow; }

        /// <summary>🆕 A24：第 `i` 行**行底**现在的色偏系数（`1` = 常态 · `0.6887` = 原版悬停 · `-1` = 取不到）。</summary>
        public float UiRowBgTint(int i)
        {
            if (i < 0 || i >= _deckRowBg.Count || _deckRowBg[i] == null) return -1f;
            var q = _deckRowBg[i].GetComponentInChildren<ImageQuad>(true);
            return q != null ? q.Tint.r : -1f;
        }

        /// <summary>搜索框那一行的三个矩形（**屏幕绝对 px，左/上/宽/高**）。</summary>
        public void UiFilterNameRects(out float ix, out float iy, out float iw, out float ih,
                                      out float tx, out float ty, out float tw, out float th,
                                      out float cx, out float cy, out float cw, out float chh)
        {
            PxRect i, t, c;
            FilterPanelModel.NameRowRects(FltW, out i, out t, out c);
            ix = FltX + i.x1; iy = FltY + i.y1; iw = i.W; ih = i.H;
            tx = FltX + t.x1; ty = FltY + t.y1; tw = t.W; th = t.H;
            cx = FltX + c.x1; cy = FltY + c.y1; cw = c.W; chh = c.H;
        }

        void HandleFilterRow(string key)
        {
            if (key == "$name") { BeginFilterNameEdit(); return; }
            // 🔴 **「点一格改哪个条件」只有一份实现**（`FilterPanelModel.Click`，收藏窗走同一个函数）。
            //    这里只负责「出声」与重画。
            if (!FilterPanelModel.Click(State, key)) return;
            if (key == "$owned")
                Say(State.Filter.Owned ? "Owned only：单机全解锁 ⇒ 结果不变（不筛也在池里）" : "Owned only：已关");
            if (key == "$upgradable")
                Say(State.Filter.Upgradable ? "Upgradable only：单机版没有升级系统 ⇒ 必然筛成空（预期）" : "Upgradable only：已关");
            ApplyFilter(State.Filter);
        }

        // ============================================================ 卡组 / 卡

        void TryAddCard(CardDef def)
        {
            string why;
            if (State.TryAdd(def, out why)) { CommitDeck(); RefreshAll(); Say("已加入：" + CardText.Name(def.Name, def.NameZh)); }
            else Say(why);
        }

        /// <summary>写回卡组库（原版是 `syncedToServer=false` 标脏 + Done 时才上传；我们单机直接落盘）。</summary>
        public void CommitDeck()
        {
            Library.CommitCurrent(State.Deck);
            RefreshHeader();
        }

        CardDisplayWindow _cardWindow;

        void OpenCardWindow(CardDef def)
        {
            if (def == null) return;
            // 复用对战的放大窗（查证：原版放大窗与战斗里那个是同一套 `CardDisplayWindow`）
            if (_cardWindow == null) _cardWindow = CardDisplayWindow.Create(Root);
            // 🆕 2026-09-28：**把卡表项一起传进去** —— 相关卡那一叠要按它算（判据 → `RelatedCards`）
            _cardWindow.Show(BattleDriver.ToCardData(def, def.Faction), def);
        }

        // ============================================================ 命中判定

        /// <summary>世界坐标 → 原版像素坐标（1920×1080，y 向下）。</summary>
        public Vector2 ToPx(Vector3 world)
        {
            return new Vector2(world.x * PxPerUnit + ScreenW * 0.5f, ScreenH * 0.5f - world.y * PxPerUnit);
        }

        bool Hit(ImageQuad q, Vector3 wp)
        {
            if (q == null || !q.gameObject.activeSelf) return false;
            var p = q.transform.position;
            return Mathf.Abs(wp.x - p.x) <= q.WorldW * 0.5f && Mathf.Abs(wp.y - p.y) <= q.WorldH * 0.5f;
        }

        void Btn_(string key, float x, float y, float w, float h)
        {
            _btns.Add(new Btn { Key = key, X = x, Y = y, W = w, H = h });
        }

        bool HitBtn(string key, Vector2 px)
        {
            if (!KeyLive(key)) return false;       // 🔴 A41 ④：看不见的钮**不许命中**（见 `KeyLive`）
            foreach (var b in _btns)
                if (b.Key == key && px.x >= b.X && px.x <= b.X + b.W && px.y >= b.Y && px.y <= b.Y + b.H)
                    return true;
            return false;
        }

        /// <summary>🔴 **2026-10-04（A41 ④）：这颗按钮「现在真的在」吗** —— 不在 ⇒ 矩形**不参与命中**。
        ///
        /// 为什么必须有它：本窗有两批按钮是**成组显隐**的（`RefreshTabVisibility` 关掉 `_infoOnly`、
        /// `CloseImport` 关掉 `_modalOnly`），而 `_btns` 里的矩形**从不跟着开关走** ⇒ 那些**看不见的钮
        /// 照旧吃点击**（真缺陷，不是疑点）：
        ///   · （60..131, 636..707）与（200..271, 636..707）这两片空白（`info_share` / `info_import`）
        ///     ⇒ 一次点击就**静默分享卡组串到剪贴板** / **打开导入弹窗**；
        ///   · 导入弹窗**关着**时，（610..1310, 370..511）落在**卡池**里 —— 点在没卡的空白上
        ///     ⇒ `imp_input` 命中 ⇒ **进入一个看不见的文本编辑态**（`_editKind = 3`，与静默失败同类）。
        /// 🔴 **2026-10-04 订正（R-W4 F1）：「必现页」是 `Cosmetics`，不是 `Cards`。**
        ///   真实指针链是 `HandlePoolClick → HandleDeckRowClick → HandleCosmeticClick → HandleButtons`
        ///   （`:1606-1610`，`HandleButtons` 是**最后一站**），而那两片矩形（x 60..271, y 636..707）
        ///   **整片落在卡组列表里**（列表 = x 0.4..325.4 · y 366..1010.1）⇒
        ///     · **`Cards` 页**（`_tab==0`）：只要那一格真有行，`HandleDeckRowClick`（`:1634` 前提正好成立）
        ///       就把它吃掉（开始行拖拽）⇒ **点不到** `info_*`；只有**那一格是空槽**时才漏得过去
        ///       （`RefreshDeckList` 的 `on = cards && (firstRow + i) < shown.Count`，`:1263`）；
        ///     · **`Cosmetics` 页（必现）**：`_tab==2` ⇒ `HandleDeckRowClick` 在 `:1634` 早退、
        ///       `HandleCosmeticClick`（`:1159` 要求 `_tab==2`）又只管 x ≥ `CosmoX` 那一片
        ///       （两片空白在 x 60..271）⇒ **谁也拦不住**，直接落到 `HandleButtons`。
        ///   ⇒ 「静默分享 / 静默开弹窗」这一条在 **Cosmetics 页必现**、Cards 页**空槽时可现**。
        /// 判据 = **与显隐同一个谓词**（不许两份）：
        ///   `info_*` ↔ `InfoTab`（`RefreshTabVisibility` 用的就是它）· `imp_*` ↔ `_importOpen`（`OpenImport/CloseImport`）。
        /// 表里没有的 key = **恒在**（`hdr_*` / `foot_done` / `name_*` / `tab_*` 都是常显件）。
        /// ⚠️ 加一颗**成组显隐**的按钮，必须把它的名字写进这里 —— 否则又会出现「看不见却能点」。</summary>
        bool KeyLive(string key)
        {
            // 🔴 **模态优先（2026-10-04 首跑红了，就地补回）**：导入弹窗开着 ⇒ **只认 `imp_*`**
            //   （原版靠弹窗那块全屏暗底吃射线；我们没建那块暗底，所以在这里挡）。
            //   ⚠️ A41 ④ 把这套口径收进 `KeyLive` 时**漏了这一条** —— 它只按「各自的显示条件」判，
            //   于是弹窗背后那颗 `Back` 又会亮（自检当场报出来：`UiHoverAt(267.2,113.5) != null`）。
            //   现在两者都在这里：**模态**是先后关系（弹窗开着时背后一律不算），**显示条件**是各自那一支。
            if (_importOpen)
            {
                switch (key)
                {
                    case "imp_input":
                    case "imp_ok":
                    case "imp_close":
                        return true;
                    default:
                        return false;      // 弹窗开着 ⇒ 背后的一切**不接受命中/悬停**
                }
            }
            switch (key)
            {
                case "info_share":      // 只在 Deck info 页签显示（`_infoOnly`）
                case "info_import":
                    return InfoTab;
                case "imp_input":       // 只在导入弹窗开着时存在（`_modalOnly`）
                case "imp_ok":
                case "imp_close":
                    return false;       // 弹窗关着 ⇒ 这三颗**一律不算**（否则会在看不见的输入框上亮起来）
                default:
                    return true;
            }
        }

        /// <summary>**盖住这一点**的所有 key —— **按 `ClickOrder`（= 真正谁先吃到）**，第一个就是赢家
        /// （`TopKeyAt` 也是从那张表上取第一个）。点击记录（`ClickLog`）用它报「我点了什么」。
        /// ⚠️ **2026-10-04（A37 ④）改**：原来按 `_btns` 的**登记顺序**报，而点击实际走 `ClickOrder`
        ///   ⇒ 两套顺序不一致时报出来的「第一个」就是错的（本窗矩形今天互不重叠，所以没露过）。</summary>
        List<string> ButtonKeysAt(Vector2 px)
        {
            var list = new List<string>();
            foreach (var k in ClickOrder) if (HitBtn(k, px)) list.Add("`" + k + "`");
            return list;
        }

        // ============================================================ 建图工具

        public Vector3 Pos(float px, float py)
        {
            return new Vector3((px - ScreenW * 0.5f) / PxPerUnit, (ScreenH * 0.5f - py) / PxPerUnit, 0f);
        }

        public float U(float px) { return px / PxPerUnit; }

        /// <summary>原版 UI 图。名字 = sprite 名（空格换下划线），见 `工具/sync_battle_ui_art.py`。
        /// ⚠️ **两个目录都要找**：卡组编辑这批在 `Art/ui_deck/`，但有几张（`40K_button` /
        /// `UI_Button_Round_background` / `40k_bt_close`）是**战斗那批**、在 `Art/ui/` 里 ——
        /// 只查 ui_deck 会误报「缺图」（自检抓到过）。
        /// **取不到时记进 `_missingArt`，建完之后统一报警**（不许静默变空白）。</summary>
        Texture2D Ui(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.DeckUi(name);
            if (t == null) t = CardArt.Ui(name);
            // 🔴 **2026-09-28 补第三批**：菜单那批（`ui_menu/`）原来**没查** —— 筛选栏的搜索框底图
            //    `InputFieldBackground` 就在那儿（收藏窗走 `MenuUi`，所以那边一直没露）。
            //    保持「先 ui_deck → 再 ui → 最后 ui_menu」的次序，免得同名图被前一批遮掉。
            if (t == null) t = CardArt.MenuUi(name);
            if (t == null) _missingArt.Add(name);
            return t;
        }

        /// <summary>按**左上角 + 宽高**摆一张图（和权威坐标表同序，抄表不会抄错）。
        /// ⚠️ 原版 rect 的比例和源图常不一样（例：行底源图 462×62、显示 325×55.7）⇒ 必须强制宽高比。
        /// 🆕 A67：`parent` 用来把左抽屉那几件挂进它们各自的容器（不传 = `Root`，与原来一致）——
        ///   坐标仍是**屏幕绝对 px**（容器在原点、无缩放 ⇒ 两种父级下同一个数）。</summary>
        ImageQuad Img(string key, string sprite, float x, float y, float w, float h, int q,
                      bool keepAspect = false, Transform parent = null)
        {
            return Img(key, Ui(sprite), x, y, w, h, q, keepAspect, parent);
        }

        ImageQuad Img(string key, Texture2D tex, float x, float y, float w, float h, int queue,
                      bool keepAspect = false, Transform parent = null)
        {
            if (tex == null) return null;      // 缺图由 `Ui()` 记账（别在这里按 key 再记一次）
            // `keepAspect` = 原版 `Image.m_PreserveAspect`：**按图自身宽高比放进框、居中**（不拉伸）。
            // 🔴 **2026-09-27 加（PA 普查）**：本文件原来**恒 `SetAspect(w/h)`（拉伸）**（全文件无一处等比）
            //   ⇒ 原版 PA=1 的件被我们画成拉伸。算法与 `MenuDraw.Rect:68-73` **同一条**
            //   （内接：图比框宽就压高，否则压宽；**中心不动**）。
            float cx = x + w * 0.5f, cy = y + h * 0.5f;
            if (keepAspect && tex.height > 0 && w > 0f && h > 0f)
            {
                float sprAspect = (float)tex.width / tex.height, rectAspect = w / h;
                if (sprAspect > rectAspect) h = w / sprAspect; else w = h * sprAspect;
            }
            var q = ImageQuad.Create(parent != null ? parent : Root, tex, Pos(cx, cy),
                                     h > 0f ? U(h) : 0.01f, new Vector2(0.5f, 0.5f), key);
            if (q != null && h > 0f) q.SetAspect(w / h);
            if (q != null)
            {
                q.SetRenderQueue(queue);
                if (!string.IsNullOrEmpty(key)) _named[key] = q;
            }
            return q;
        }

        Label Txt(string key, string s, float x, float y, float w, float h, int scale, Color c, int queue,
                  Transform parent = null)
        {
            float cy = h > 0f ? y + h * 0.5f : y;
            var l = Label.Create(parent != null ? parent : Root, s, Pos(x + w * 0.5f, cy), scale, c,
                                 new Vector2(0.5f, 0.5f), key);
            if (l != null) l.SetRenderQueue(queue);
            return l;
        }

        /// <summary>按**原版字号（画布 px）**摆一段文字 —— `Txt` 那个 `scale` 是**档位**、给不出精确字号。
        /// 用法先例：`MenuWindowBase.Text(..., fontPx)`（同一条换算 `LayoutSpace.Px`）。
        /// ⚠️ 验收这类数字**量渲染图**，别按 `textBounds`/几何中心 —— 原版 `VerticalAlignment = Capline`
        /// 会让数字相对几何中心略偏上（正本 `卡组编辑界面_查证_0920.md` §③ 末条）。</summary>
        Label TxtPx(string key, string s, float x, float y, float w, float h, float fontPx, Color c, int queue)
        {
            var l = Txt(key, s, x, y, w, h, 1, c, queue);
            if (l != null && fontPx > 0f) l.SetGlyphHeight(LayoutSpace.Px(fontPx));
            return l;
        }

        static void SetQuadWidth(ImageQuad q, float wPx)
        {
            if (q == null) return;
            float h = q.WorldH;
            if (h > 1e-5f) q.SetAspect(Mathf.Max(1f, wPx) / PxPerUnit / h);
        }

        // ============================================================ 🆕 2026-10-04（A24）悬停 / 状态 换图
        //
        // 判据 = **原 prefab 的组件字段**（不是截图、不是我们的常量）：
        //   卡组编辑窗 `Deck Editing Menu`（`bundle_menus_assets_all`）全树 29 个 `m_Transition`：
        //   `trans=2(SpriteSwap)` **3** 颗 + 自带导入弹窗 2 颗（本文件要接的那 5 颗）·
        //   `trans=1(ColorTint)` 12 颗（走 `m_Colors`，其中三颗页签另有 UGUI `Toggle.graphic` 那条路）·
        //   `trans=0` 14 颗（原版悬停什么都不变 ⇒ **我们也不接**）。
        //   逐颗表与出处 → `资料/普查产出_1003/卡组编辑器_按钮悬停图_普查.md`（块 A/B/C/D）。

        /// <summary>把一颗**已经画好的**按钮接上原版的「悬停 / 按下换图」（`Selectable.m_Transition = 2(SpriteSwap)`）。
        /// 两张高亮图默认由 `WindowButton` 的名字表推（`&lt;常态图&gt;_hover` / `_pressed` + 4+6 条特例），
        /// 逐颗不同的（例：常态图相同的两颗配不同高亮图）用参数显式覆盖。
        /// ⚠️ UGUI 一颗 `Selectable` 只有**一种** transition ⇒ `Bind` 会把色偏兜底关掉（原版 SpriteSwap 那档不叠色偏）。
        /// ⚠️ 只接 `trans=2` 的 —— `trans=1` 的原版**只变色**（那批还没接，见 `RowHoverK` 那段与报告）。
        /// <param name="key">它在 `_btns` 里的那个 key —— 悬停派发要用它把「命中」和「点击」对成同一条口径。</param>
        WindowButton Hover(string key, ImageQuad q, string art, string hoverArt = null, string pressedArt = null)
        {
            if (q == null) return null;      // 图没取到（`Ui()` 已记账）⇒ 不挂一颗点不出反应的假按钮
            var wb = q.gameObject.AddComponent<WindowButton>();
            wb.BindSelf(art, hoverArt, pressedArt);
            if (!string.IsNullOrEmpty(key)) _hoverBtns[key] = wb;
            return wb;
        }

        // ============================================================ 🆕 2026-10-05（A32②）`trans=1(ColorTint)` 那批
        //
        // 🔴 **本窗原来一颗 `trans=1` 都没接** —— A24 只接了 5 颗 `SpriteSwap`（换图），而这批原版的
        //   悬停表现是**变色**（UGUI `Selectable.DoStateTransition(ColorTint)` →
        //   `targetGraphic.CrossFadeColor(m_Colors.m_HighlightedColor / m_PressedColor, m_FadeDuration)`）。
        //   `WindowButton` 的色偏那一半（`tintOnHover` 默认 **true**、`Enter/Exit/Press/Release` +
        //   `HighlightK = 0.9607843` / `PressedK = 0.7843137` / `FadeSeconds = 0.1`）就是为它准备的 ⇒ **不调
        //   `Bind`**（`Bind` 会把 `tintOnHover` 关掉，那是给换图那一档的）。
        //
        // 判据 = **逐颗读 prefab 的 `m_Transition` / `m_Colors` / `m_TargetGraphic`（再读目标件自己的
        //   `m_Color.a` 与 `m_Enabled` —— 「看不看得见」的唯一判据是**目标件**，不是有没有 `m_Transition`）**：
        //   本窗**接**（原版看得见，`m_Colors` 全是 UGUI 默认那组 ⇒ 0.9608 / 0.7843）：
        //     · `Header/Filters`（`EverguildToggle`，MB `-3758886955019145436`）`trans=1` ·
        //       `m_TargetGraphic` = **它自己那层 Image**（`40k_menu_bt` · `m_Color=(1,1,1,1)` · `m_Enabled=1`）
        //       ⇒ 悬停变暗**看得见**（这一颗同时还有 `changeSpriteOnValueChange` 那半份，见 `RefreshHeader`）
        //     · `Sidebar/Window Options/Deck Name`（`EverguildInputField`）`trans=1` · 目标 = **自己那张底图**
        //       （`InputFieldBackground` · `m_Color=(0.0627,0,0,1)` **不透明**）⇒ 看得见
        //     · `Card Filters/…/Owned Toggle`（MB `-9051368228953265348`）·
        //       `…/Upgradable Toggle`（`-7134010496431493340`）· `Cosmetic FIlter/…/Owned Toggle`（`-3725464403648385244`）
        //       —— 三颗都 `trans=1` · `m_Colors` 默认 · 目标 = **子件 `Image`**（`40_main_bt_toggle_on/off` ·
        //       `m_Color=(1,1,1,1)` · `m_Enabled=1`）⇒ 看得见
        //     · `Card Filters/…/Name FIlter/Input Field`（搜索框）`trans=1` · 目标 = 自己那张底图 ⇒ 看得见
        //   ⛔ **本窗【不接】的（原版本身就没有，逐颗实读 —— 这不是「暂缓」）**：
        //     · 卡池**四个选项行**的格子（`Army/Rarity/Cost/Type Filter/Content/Toggle`）`m_Transition = 0(None)`
        //       ⇒ 悬停什么都不变（判据 = 逐颗读 `m_Transition`，四个模板各一颗，两扇窗都读过）
        //     · **侧栏三页签**：`trans=1` 但目标件是**子件 `Highlight`**，而那颗 `m_Color.a = 0`、
        //       且节点自己的 `Image` `m_Enabled = 0` ⇒ 原版悬停**看不见**（`RefreshHeader` 那段有长注释）
        //     · `Deck Selector *` 行按钮：`trans=1` 但 `m_TargetGraphic` = 子件 `Background`，
        //       色值是**逐颗非默认**的 `0.6886792` ⇒ 走 `ApplyRowHover` 那一份（**不是**这里）
        //
        // ⚠️ 命中口径与点击**共用**：`ClickOrder` 那 14 个 key 走 `HitBtn`；抽屉里那几格
        //   （搜索框 + 三个开关）走 `_fltHit` / `_cosmoFltHit`（= `HandleFilterClick` 用的同一张矩形表），
        //   见 `DrawerHoverUnder`。空白的 `_hit` 命中区（`MenuDraw.Hit` 建的那种透明 quad）**不在这条路上**
        //   —— 那种子树的 `Collect()` 只收得到一个**全透明**的 quad ⇒ 变色看不见（= 与原版 `trans=0` 等效）。

        /// <summary>把一颗**已经画好的**件接上原版的「悬停 / 按下**变色**」（`m_Transition = 1(ColorTint)`）。
        /// ⛔ **不要**再调 `Bind`（那是换图那一档；UGUI 一颗 `Selectable` 只有一种 transition）。
        /// <param name="key">见 <see cref="Hover"/>；抽屉里那几格用它们的**点击 key**（`$name` / `$owned` / `$upgradable`）。
        /// <param name="drawerReg">抽屉那两栏的登记表（`_fltHoverBtns` / `_cosmoHoverBtns`）—— 传 `null` = 登记进
        /// `_hoverBtns`（`ClickOrder` 那一档）。⚠️ **两份表必须分开**：卡牌栏与卡背栏可以同时存在，
        /// 而两边都有 `$owned` 这个 key ⇒ 合成一份会**互相盖掉**（后登记的赢，另一颗永不亮）。</param></summary>
        WindowButton HoverTint(string key, GameObject go, Dictionary<string, WindowButton> drawerReg = null)
        {
            if (go == null) return null;
            var wb = go.AddComponent<WindowButton>();     // `tintOnHover` 默认 true ⇒ 只要不 `Bind` 就是变色那一档
            if (!string.IsNullOrEmpty(key)) (drawerReg ?? _hoverBtns)[key] = wb;
            return wb;
        }

        /// <summary>**状态**换图（不是悬停）：把一张画好的 quad 换成另一张，**并把宽高比拉回来**。
        /// 🔴 `ImageQuad.SetTexture` 会把 `_aspect` 冲成**贴图自己的**比值 —— 我们这套矩形是按原版矩形定的，
        /// 不拉回来那颗件的**逻辑宽度**就被改掉了（A17 在设置窗上实测过：300px 的钮变成 329，肉眼看不出来）。
        /// 同一张图就别再设（`RefreshHeader` 会被滚轮/每帧调到）。</summary>
        void SetSprite(ImageQuad q, string art)
        {
            if (q == null) return;
            var t = Ui(art);
            if (t == null || q.Texture == t) return;
            float a = q.WorldH > 0f ? q.WorldW / q.WorldH : 0f;
            q.SetTexture(t);
            if (a > 0f) q.SetAspect(a);
        }

        /// <summary>筛选格里那件图示 / 文字的着色。只有**开关那一类**（`Cell.IconOff != null`）例外：
        /// 原版那三颗 `EverguildToggle` 的 `colorTintOnValueChange = 0` ⇒ 值一变**不改色**、只看换图，
        /// 图示的常态色 = `m_Colors.m_NormalColor = (1,1,1,1)` ⇒ 我们给**白**，
        /// 让开/关**全靠 `40_main_bt_toggle_on/off` 两张图**区分（原来那套「关了就乘 0.349」的颜色不再需要）。</summary>
        static Color CellTint(FilterPanelModel.Cell c)
        {
            return c.IconOff != null ? Color.white : FilterPanelModel.ToggleTint(c.On, c.OffTint);
        }

        /// <summary>原版 `Deck Selector {Card Info button, Defensive Card Slot}` 的
        /// `m_Colors.m_HighlightedColor` —— 卡组行的悬停**只把行底变暗到 0.6887**（不是全库默认的 0.9608）。
        /// 逐条判据（2026-10-04 直接读 prefab，不是转抄普查）：
        ///   · 那两颗 `m_Transition = 1(ColorTint)`、`m_TargetGraphic` = **子件 `Background`**
        ///     （sprite = `40k_deck_cardlist_bg`，正是我们画的行底）、那张图 `m_Color = (1,1,1,1)`
        ///     ⇒ 悬停 = **白 × 0.6886792**。
        ///   · 同一行的 `Deck Selector Hero Card Info button` 也是 `trans=1`，但目标图是它自己那张
        ///     `UI_Card_name_background_normal BW`，而那张图 `m_Color = (0.886,0.388,0.388, **alpha 0**)`
        ///     —— **全透明** ⇒ **督军行悬停看不到任何变化**（原版如此）⇒ 我们不给它上色。
        ///   · UGUI 的 `ColorTint` 只作用在 `targetGraphic` 那一件 ⇒ 色条/描边/费用/文字**都不跟着变**。
        /// ⚠️ 这条**没有**走 `WindowButton`：它的 `HighlightK` 是全库默认的 0.9608 常量、逐颗覆盖要改
        ///   `Shell/PromptPopup.cs`（本轮白名单只允许改那里的一句注释）⇒ 行悬停这一份自己实现，
        ///   只在 `ApplyRowHover` 一处（**别在别处再写第二份**）。
        /// ⚠️ **自检不许读这个常量**（A37 ③ 那笔账：`Editor/DeckScene.cs` 原来拿它当期望值 = 自证）
        ///   —— 那边现在写死同一个数的**字面量**。本常量留着只给 `ApplyRowHover` 用。</summary>
        public const float RowHoverK = 0.6886792f;

        static void DestroySafe(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        /// <summary>自检用：建完之后还缺哪些图（空 = 素材齐）。</summary>
        public List<string> MissingArt { get { return new List<string>(_missingArt); } }

        // ============================================================ 配色 / 阵营

        static readonly Color Ink = new Color(0.93f, 0.93f, 0.95f, 1f);
        // ⚠️ 这里原来还有一个 `Gold = (0.96, 0.83, 0.45)` —— **2026-10-04（A46）随页签字色一起删掉**：
        //   原版那三颗 `Text` 的 `m_fontColor` 恒为白（判据见 `RefreshHeader` 里 `SetColor(Color.white)`
        //   那一段），而它只被页签那一处用过 ⇒ 留着就是一段「与原版不符的死代码」。

        /// <summary>稀有色条的颜色 —— ⚠️ **我们挑的**：原版是 `40k_deck_cardlist_bg_rarityColorGradient`
        /// 这张图 + 运行时染色，**染色值没查到**（节点树里 `m_Color` 是白的）。</summary>
        public static Color RarityTint(string rarity)
        {
            switch (rarity)
            {
                case "rare": return new Color(0.35f, 0.55f, 0.95f, 0.9f);
                case "epic": return new Color(0.75f, 0.85f, 0.95f, 0.9f);
                case "legendary": return new Color(0.95f, 0.78f, 0.35f, 0.9f);
                case "special": return new Color(0.85f, 0.45f, 0.85f, 0.9f);
                default: return new Color(0.65f, 0.68f, 0.72f, 0.9f);
            }
        }

        /// <summary>当前卡组的阵营（督军决定）。</summary>
        public string ArmyFaction()
        {
            if (State == null) return null;
            var w = State.Deck.WarlordId != null ? State.Find(State.Deck.WarlordId) : null;
            return w != null ? w.Faction : null;
        }

        /// <summary>阵营 → 原版徽记图（13 张 `40k_DeckSelection_icon_*`）。</summary>
        public static string FactionIcon(string faction)
        {
            switch (faction)
            {
                case "Ultramarines": return "40k_DeckSelection_icon_FactionUM";
                case "Goff": return "40k_DeckSelection_icon_FactionOrks";
                case "SaimHann": return "40k_DeckSelection_icon_FactionSaimHann";
                case "Sautekh": return "40k_DeckSelection_icon_FactionSautekh";
                case "BlackLegion": return "40k_DeckSelection_icon_FactionBlackLegion";
                case "Leviathan": return "40k_DeckSelection_icon_FactionLeviathan";
                case "TauEmpire": return "40k_DeckSelection_icon_FactionTauEmpire";
                case "Sororitas": return "40k_DeckSelection_icon_FactionSororitas";
                case "Genestealers": return "40k_DeckSelection_icon_Genestealers";
                case "AstraMilitarum": return "40k_DeckSelection_icon_FactionAstraMilitarum";
                case "DarkAngels": return "40k_DeckSelection_icon_FactionDarkAngels";
                case "EmperorsChildren": return "40k_DeckSelection_icon_FactionEmperorsChildren";
                case "SpaceWolves": return "40k_DeckSelection_icon_SpaceWolves";
                default: return "40k_collection_bt_decks";
            }
        }

        /// <summary>自检/演示用的一副卡组：能凑合法就凑合法，凑不出就有什么用什么。</summary>
        public static PlayerDeck PlayerDeckForDemo(DeckEditorState state)
        {
            var deck = new PlayerDeck { Name = "复仇者之刃" };
            CardDef warlord = null;
            foreach (var c in state.Pool) if (c.Type == "hero") { warlord = c; break; }
            if (warlord == null) return deck;
            deck.WarlordId = warlord.Id;
            foreach (var c in state.Pool)
                if (c.Type == "defence" && DeckRules.SameFaction(c.Faction, warlord.Faction)) { deck.DefensiveId = c.Id; break; }
            foreach (var c in state.Pool)
            {
                if (deck.CardIds.Count >= DeckRules.ClassicCards) break;
                if (c.Type != "unit" || !DeckRules.SameFaction(c.Faction, warlord.Faction)) continue;
                for (int i = 0; i < DeckRules.CopyLimit(c.Rarity) && deck.CardIds.Count < DeckRules.ClassicCards; i++)
                    deck.CardIds.Add(c.Id);
            }
            return deck;
        }
    }
}
