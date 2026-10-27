// DeckRuntime.cs — 卡组编辑界面的**运行时控制器**（建界面 + 听鼠标/键盘 + 重画）
//
// 为什么需要它：`DeckEditorState`（纯 C#）早就写好、自检也覆盖了行为，
// 但**运行时交互层从来没建过** —— `DeckEditor.unity` 里原来只有 `ImageQuad`/`Label` 画的
// 一堆静态方块，点/拖一律不响应。这个类补上「谁听鼠标 + 谁重画」这一层。
//
// ============================ 版面权威（唯一出处） ============================
// `D:/2/Warpforge_tools/data/ui_layout/_deck_editing_godot_rects.txt`
//   —— 绝对坐标表（1920×1080，y 向下），每行带 `RectTransform_<id>.json` 可复查。
//   🔴 **2026-10-15 就地订正两处（铁律 5）**：
//   ① **生成器写错了** —— 原文写「`chain_rect.py` 算的」，实测真生成器是
//      **`unity_rect_to_godot.py`**（`:110` 打 `名 :: pos(…) size(…)`，与本表逐字相符；
//      而 `chain_rect.py` 打的是 `父链: … / Godot: x[…]`，**两种格式不同**）。
//   ② **它不是「Godot 时代的账」** —— 那个脚本读的是**原版 Unity 解包场景 JSON**
//      （`GameObject/` `RectTransform/` `Transform/` `MonoBehaviour/`），**「Godot」只是坐标约定名**
//      ⇒ **值可以照用**（⚠️ 唯一真坑见 `:488`：该表**逐级不乘节点 scale**）。
//      用户 2026-10-15 口径：「我们直接使用解包资料，不要被 Godot 时代的账这些干扰」。
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
//   ① 🔴 **2026-10-22（`D1`）就地订正（铁律 5）：原来这里写「卡池一屏 4 列 × 2 行 —— 原版是
//      `RecyclableScrollRect` 运行时算的，节点树给不出 ⇒【我们挑的】」—— 这一条【已过期】。**
//      原版那两条式子本地都读得到，`A870`（2026-10-18）已经照它现算了：
//        · **列数** = `floor(视口宽 ÷ (_cellWidth + _spacingX))`
//          （`PolyAndCode.UI.VerticalRecyclingSystem__CreateCellPool.c:127-167`，用 `_cellWidth` **原值**）
//          ⇒ 桌面 **6 列**（1589.8 ÷ 262.5）、小屏 **4 列**（1589.8 ÷ 393.75）；⛔ 不是写死的 4/6。
//        · **行数** = 覆盖 **1.5 个视口高** 与「池格下限 20」两条阈值合成
//          （同文件 `:186-193` / `:256-271`）⇒ 桌面 4 / 小屏 5。
//      实现 = 本文件 `Pool` 属性 / `PoolRowsFor`（搜这两个名字；两档由同一条式子算，不是 if-else）。
//      ⛔ 别再写回「4 列 × 2 行」——那是我们旧的猜测值，**不是**原版参数。
//   ② 🔴 **2026-10-04（A41 ①）订正 —— 这一条已经不成立了，页签图标【照原版整格画】**：
//      原来写的是「页签图标显示尺寸 100×100 —— 节点树里那三个 `Icon` 的 rect 是 **0×0**（VLG 撑的）」，
//      **那个 0×0 是「VLG 布局跑之前的模板位」**（铁律 5·c：只量了一种情况）。布局跑完之后：
//      `Icon` 的 rect = **整格 108.96×150** + `m_PreserveAspect = 1`，源图 126×126
//      ⇒ 原版实绘 **108.96²**（居中）。判据 = `python 工具/menu_dump.py bundle_menus_assets_all
//      "Deck Editing Menu" --depth 7`：`Cards/Icon [0.3,156]–[109.2,306] 108.96×150 …… preserveAspect`。
//      我们原来画 100×100 ⇒ **小 8.97px（≈9%）**，现在按整格 + `keepAspect` 画。
//   ③ 🔴 **2026-10-21（`A1721`）订正：行距不是「我们挑的」，原版给得出来 = 57.7**。
//      原来这里写「**行距 56**（行高 55.7 是权威，垂直排布的 spacing 查不到）」—— **后半句是错的**：
//      `Content` 的 `VerticalLayoutGroup.m_Spacing = 2.0`（`工具/menu_dump.py "Deck Editing Menu" --depth 12` 现读），
//      三行打在 y 366 / 423.7 / 481.4 ⇒ 步长 **57.7 = 55.7 + 2.0**。见 `RowSpacing` / `RowPitch`。
//      （**卡池行距 528**（卡高 512 + 16 缝）那半句不受影响。）
//   ④ 侧栏三个钮（新建/复制/删除）**已按原版删掉** —— 原版那套动作在 `DeckInfoPopup` 的 5 圆钮里
//   ⑤ 稀有度色条的颜色值、筛选栏里各行的高度与字号（原版是 VLG 流式，绝对坐标只是模板位）
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;      // 🆕 2026-10-17：拖拽那一族（`PointerEventData` 要合成给控制器）
using UnityEngine.InputSystem;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>卡组编辑界面的运行时控制器。挂在 `DeckEditor.unity` 的根对象上。
    /// <para>🆕 **2026-10-17：本类同时是原版那个 `DeckEditingPanel`** —— 卡组编辑窗里
    /// 「松手投递」的落点（`Core/DraggableController.cs` 的 `IDropHandler&lt;T&gt;` 那两个）。
    /// 判据（现读）：`dump.cs:72263` 写着 `public class DeckEditingPanel : MonoBehaviour,
    /// IDropHandler&lt;RawCardScript&gt;, IDropHandler&lt;CosmeticItem&gt;`；它挂在 `Sidebar/Deck Details`
    /// 那一件上（MB −2895006486255833308 · GO −1322417011089150172），我们这扇窗里对应的活全在本类
    /// ⇒ **两个接口由本类实现**（等价物，不是同名类）。</para></summary>
    public class DeckRuntime : MonoBehaviour, IDropHandler<string>, IDropHandler<CardDef>
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
        // 🔴 **2026-10-22（`D1`）就地订正（铁律 5）：`Clear filters` = 原版布局组跑完的
        //   `85×70 @ (592.2, 78.5)`；原来写的是 `250×60 @ 1218.6`。**
        //   错因 = 拿 dump 的**模板位** `[1489,83] 250×60` 当尺寸，又拿容器右边界 1468.6 去反推 x
        //   （「容器内右对齐」）。**两半都不是原版的布局结果** —— 模板位量的是「布局跑之前」那一格
        //   （CLAUDE.md §三 末条那条：0×0 / 落在父容器外的 rect 是模板位），而右对齐是我们自己挑的排法。
        //   判据（逐条可复查；`python 工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu"
        //   --depth 8 --no-sprite` 现读）：
        //     · 父容器 `Header/Filters` = **592.2,71.0 → 1468.6,156.0（876.41×85）**
        //       · `GridLayoutGroup`：cellSize **85×70** · spacing (5,0) · pad (0,0,0,0) ·
        //         **`align=3` = MiddleLeft** · **`constraint=2` = FixedRowCount**、count = 1
        //         ⇒ **1 行 × N 列**（⚠️ 本文件旧注写「固定 1 列」是**读错了枚举**：Unity 里
        //         `FixedColumnCount = 1` / `FixedRowCount = 2`）
        //       · 唯一子件 = `Generic Simplified UI Button_updated`（dump 那行仍印 1488.6 / 250×60 ——
        //         **Grid 系布局 `menu_dump` 不套算**（`grid?` 那一档，见该工具 `apply_layout_to_children`），
        //         所以必须按 uGUI 算法自己算）
        //     · uGUI 结果：1 行、横向左对齐 ⇒ `x = 592.2 + pad0 = 592.2`；
        //       纵向 MiddleLeft ⇒ `y = 71 + (85 − 70) / 2 = 78.5` ⇒ **592.2,78.5 → 677.2,148.5**。
        //   ⚠️ 与左边 `Filters` 文字（437.2..587.2）**横隔 5px**、与页头分隔线（y 151）**竖隔 2.5px** —— 不压。
        //   ⛔ 别照抄 1488.6（模板位，落在容器外、压在四个稀有度计数上 —— 自检抓过）。
        const float HdrClearX = 592.2f, HdrClearY = 78.5f, HdrClearW = 85f, HdrClearH = 70f;
        // 🔴🔴 **2026-10-20 —— `Clear filters` 的显隐：原来那段 `2026-10-17（D22）`「查不到、本轮【不改】」
        //   已经按事实就地换掉（铁律 5，不是另起一份）。**
        //   ⚠️ **同一个 prefab 字段还管着另一件**：本文件那个**左抽屉**的两棵**也**带
        //   `hiddenPosition = (−550, 0)` / `animationTime = 0.3`（本窗两棵树都在 menus 包那 6 个实例里，
        //   判据 → `FltHiddenDx` 那一段）—— 那两棵走的是**原版那条「平移」**，本颗没走（理由见下面）。
        //   **现在的判据（原版有这颗钮，但默认被【平移到画面外】）**：
        //     `CollectionFilterController` 带三个相关字段 —— **`clearFiltersButton`**（桩 :14）·
        //     **`hiddenPosition`**（桩 :17）· **`animationTime`**（桩 :20）
        //     （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CollectionFilterController.cs`，本批亲读）；
        //     **卡组编辑窗那一份实读**（`bundle_menus_assets_all` 的 MB，逐字段读）：
        //     **`hiddenPosition = (-550, 0)` · `animationTime = 0.3`**
        //     ⇒ **隐藏方式是「平移」、不是 `SetActive`**（同族先例 = 本文件左抽屉那条 −385px 的行程）。
        //   ⚠️ **触发条件仍是【推断】，不是实读**：真正干这件事的方法体**本地缺失**
        //     （`decomp_full` 里 `CollectionFilterController` 只导出了 `ctor` + `NotifyFilterChange`
        //      的两个 lambda，那两个 lambda 只做 `CollectionFilterToggle.ForceOff`、**没碰这颗钮**）。
        //     旁证 = **实拍那一帧那一块是空的**（`原版参照图/用户实拍_1017/卡组编辑界面参考.png`
        //     设计坐标 `(555..765, 60..170)` 逐个采样：只有渐变底 + 底部那条分隔线，一个像素的钮都没有），
        //     而**那一帧没有任何筛选高亮** ⇒ **推断 = 「一个筛选都没设时隐藏」**。
        //     ⛔ **这是推断**（要坐实还得进原版 dump 三种状态下的 `anchoredPosition`，见下）。
        //   🔴 **我们改成 `SetActive`、没照 −550 平移**（**如实标注，这是我们的偏离**）：
        //     `hiddenPosition` 是原版 prefab 里**相对它自己父容器**的 `anchoredPosition` 口径，
        //     而我们的坐标一律是**绝对屏幕 px**、也没有那颗 85 宽的容器节点。
        //     我们现在的 x（`HdrClearX = 592.2`）**就是原版布局后的绝对位置** ⇒ 照搬 −550 会落到 **42.2**：
        //     仍在屏内、压在 `Back` 圆钮那一带（**不重现「挪出画面」**）⇒ 平移量在我们这套坐标里不复现「藏起来」。
        //   🔴 **显隐与「点得到」同一条判据**（`RefreshHeader` 里那两句 + `KeyLive` 里那一支）——
        //     藏起来却还能点到 = 静默失败（本仓红线）。
        //   ✅ **仍然成立的两条旧事实**（留着，别丢）：
        //     ① **prefab 里它是 active 的**：`Header/Filters/Generic Simplified UI Button_updated`
        //        的 `m_IsActive = True`（全包 11 个同名件逐个读过），图 `UI_Button_Mulligan`、`interactable=1`；
        //     ② 它的**父容器 `Header/Filters`**（`GridLayoutGroup` cellSize **85×70** · spacing (5,0) ·
        //        pad 0 · `constraint=2`(**FixedRowCount**) count 1 ⇒ **1 行** · `align=3`(MiddleLeft) ·
        //        `ContentSizeFitter h:MinSize`）**跑完布局之后**那颗钮 = **85×70 @ (592.2, 78.5)**
        //        —— 🔴 **2026-10-22（`D1`）起我们已经照这个值画**（原来画 250×60 @1218.6，
        //        那一笔账**已收**；旧注那句「固定 1 列」也已按枚举订正，见 `HdrClearX` 那段）。
        //   📌 要收剩下的「触发条件」那一半，需要的是【实况】：进原版卡组编辑窗，看那颗钮的
        //     `anchoredPosition` 在「没筛 / 筛了 / 清空之后」三种状态下各是什么（SceneJumpShot 能 dump）。
        const float HdrSepY = 151f, HdrSepH = 10f;
        const float WcBgX = 1550f, WcBgY = 91.5f, WcBgW = 320f, WcBgH = 44f;
        // `WIldcard Counter`（注意原版拼写就是 `WIldcard`）：`Counters` 是个 HLG（pad L/R 10 · spacing 5 · UpperLeft）
        // ⇒ 4 槽宽 70、槽 x = 1565+75i、**行带 y = [91.5,135.5]**（高 44）。
        // 每条 2 个子件：**`Icon`(30 宽) 在左 + `Counter`(41 宽) 紧随其后、同一水平带**（正本 `卡组编辑界面_查证_0920.md` §③）。
        // 🔴 2026-09-23 修：原来图标 y 抄了**外层容器顶边 71**（高 20.5）、数字被摆成**图标正下方**且字号只有 1/3。
        const float WcIconY = 91.5f, WcIconW = 30f, WcIconH = 44f;
        // 🔴 **2026-10-17（账 `A1229`）：万能卡计数 —— 三个宿主共用这一份，⛔ 别各写一份。**
        //   判据 = 原版 `WIldcard Counter/Counters/{Common,Rare,Epic,Legendary}/Counter` **四颗逐值相同**：
        //   字号 **32.6** · 自适应 **10 / 38** · base **45** · 折行 **1** · 框 **41×44**
        //   （出处 → `资料/普查产出_第十会话/R5_包装层菜单族.md` §2 #1 · `R6` §2·F #38）。
        //   宿主 = 本窗（`DeckEditingWindow`）· `Shell/CollectionWindow.cs`（收藏窗）·
        //          `Shell/CardDetailPopup.cs`（卡片详情窗）—— 三处都引用下面这几个常量。
        //   ⚠️ 数字本身恒 `99`：**用户 2026-09-28 拍板**（原版是 `WildcardDisplay` 的库存直出，
        //      单机没有发放源）→ `项目任务.md` §三 第 15 条 第 29 项。
        public const float WcCountFontPx = 32.6f;      // = 原版 `m_fontSize`
        public const float WcCountWrapPx = 41f;        // 框宽（数字格）
        public const float WcCountBoxH = 44f;          // 框高（行带 91.5 → 135.5）
        public const float WcCountAutoMinPx = 10f, WcCountAutoMaxPx = 38f, WcCountAutoBasePx = 45f;
        public const string WcCountText = "99";
        /// <summary>把「万能卡计数」那四格的字号自适应接上（框固定 `WcCountWrapPx × WcCountBoxH`）。
        /// 三个宿主都走这一句 —— 收口前是「同一规则写三遍」（`A1229`）。</summary>
        public static void FitWildcardCount(Label lb)
        {
            if (lb == null) return;
            lb.SetAutoFitBox(LayoutSpace.Px(WcCountWrapPx), LayoutSpace.Px(WcCountBoxH),
                             WcCountAutoMinPx, WcCountAutoMaxPx, WcCountAutoBasePx);
        }
        const float WcCntW = WcCountWrapPx, WcCntFontPx = WcCountFontPx;   // 本文件内的旧名字（同一份值）
        static readonly float[] WcIconX = { 1565f, 1640f, 1715f, 1790f };
        /// <summary>页头 `WIldcard Counter/Army Icon` 的框。
        /// 🔴 **2026-10-23（界面线 W65 尾账 · 新账-57）**：`Y` 原来是 **71**（四舍五入过的），
        /// 原版 prefab 真值是 **70.97 → 155.97（80×85）** ⇒ 改成 70.97（差 0.03px，纯数值对齐）。
        /// 判据 = `python -I -X utf8 工具/menu_rect.py bundle_menus_assets_all "Deck Editing Menu" --depth 7`
        /// 的 `Army Icon 1470.00 70.97 1550.00 155.97 80.00 85.00` 那一行。
        /// ⚠️ **别照收藏窗那套抄**：收藏窗（`Collection Menu Variant`）里同位置的原版值是
        /// **70.94 / 155.94**（两棵 prefab 差 0.03px）—— 两份各自对，别互相"对齐"。</summary>
        const float ArmyIconX = 1470f, ArmyIconY = 70.97f, ArmyIconW = 80f, ArmyIconH = 85f;

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
        /// TMP 会往框塞得下的最大号涨（`TextMeshPro` 里那段「increase font size to fill text container」），
        /// 所以三个页签的**输入**其实是同一对 `[10,34]`、差别是**文案长度**算出来的（铁律 5·c）。
        /// ⇒ 我们照**机制**做：`SetGlyphHeight(34px)` + `SetAutoFitBox(…, 10, 34)`。
        /// 🔴 **2026-10-05 就地订正**：这里原来写「（`SetAutoFitBox` 取 max 就是从它来）」—— **那是旧语义**。
        ///    现在 `SetAutoFitBox` 内部自己把原版的 `m_fontSizeMax` 折成 TMP 的 `fontSize` 单位：
        ///    `fontSizeMax = cur × maxPx / NominalPx()`（`Battle/Label.cs` 的 `SetAutoFitBox`）⇒ **上限 ≠ 传进去的字号**，
        ///    两者只在 `maxPx == 调用方那个 px` 时相等（本件正是这一档）。
        /// ⚠️ **换字体会改结果**：我们全工程用的是 `Fonts/NotoSerifCJK-Regular SDF`（`TmpFont.cs` 文件头），
        ///    它的拉丁字母比原版那套窄体宽 ⇒ 同一句 `Deck info` / `Cosmetics` 自适应出来会**比原版那几个
        ///    冻结值（31.5 / 28.15）小**。这是**字体替换**的后果，不是版式错 —— 真要逐像素对上得连字体一起换
        ///    （全工程的事，不在本轮）。断言盯的是**机制**（要么停在 `34`、要么被压到框的边界上 ——
        ///    F3 换掉了原来那条同义反复的区间断言），**不钉**自适应出来的那个数。</summary>
        const float TabNameMaxPx = 34f, TabNameMinPx = 10f;
        /// <summary>页签高亮**画出来的角块** = 原版 `m_Border 30` ÷ `m_PixelsPerUnitMultiplier 0.92`
        /// = **32.6087px**（判据/同一条结论 → `Shell/MenuWindowBase.cs` 里 `30 ÷ 0.92 = 32.61px` 那条结论，那边同一张图同一个键；
        /// 2026-10-04 订正：原来引的 `:414-418` 是 `BuildTabButton` 的签名/开头，不是这条结论）。
        /// ⚠️ **本仓口径**：`borderOutPx = border ÷ (spritePPU/refPPU × ppuMul)`，本工程的 `Sprite` 都是
        /// 100/100 ⇒ **直接 `÷ ppuMul`**（同 `Shell/DuelPopupWindow.cs` 的 `Build` 里那条 `ppuMul` 注、`Shell/MenuWindowBase.cs` 里 `30 ÷ 0.92 = 32.61px` 那条结论）。</summary>
        const float TabHiCorner = 30f / 0.92f;
        const float NameX = 9.5f, NameY = 311f, NameW = 307.7f, NameH = 50f;
        const float NameTxX = 19.5f, NameTxY = 318f, NameTxW = 287.7f, NameTxH = 37f;
        const float NameClrX = 277.2f, NameClrY = 316f, NameClrW = 35f, NameClrH = 40f;

        // ---- Deck Details：卡组列表（R:57-96）----
        // 🔴 **2026-10-17（`A1736`）**：`ListH` 由 **644.1 → 644.06**（原版 `Viewport` 高实测 644.06；
        //   原来那个 644.1 是四舍五入 ⇒ 视口底 1010.1 vs 1010.06、残行 9.40 vs 9.36，**差 0.04px**）。
        //   细小但属「逐值对齐」那一族（铁律 11），改完 `ListY + ListH = 1010.06`。
        const float ListX = 0.4f, ListY = 366f, ListW = 325f, ListH = 644.06f;
        const float RowW = 325f, RowH = 55.7f;
        /// <summary>行与行之间的**间距**（画布 px）= 原版 `Content` 那颗 `VerticalLayoutGroup` 的
        /// **`m_Spacing`**。
        /// <para>🔴 **2026-10-13（`A1721`）新增**：原来是 `const float RowPitch = 56f;`、自陈
        /// **「⚠️ 我们挑的」**（文件头 ③）—— **那是错的**，原版给得出来。判据（现读原版 prefab
        /// `bundle_menus_assets_all` 的 `Deck Editing Menu > Content Area > Sidebar > Deck Details >
        /// Deck List drawer/…/Content`，`工具/menu_dump.py "Deck Editing Menu" --depth 12`）：
        /// · `VerticalLayoutGroup.m_Spacing = **2.0**` · `m_Padding = (8,8,0,0)` · CSF `v:MinSize`；
        /// · 三行打在 **y 366 / 423.7 / 481.4** ⇒ 步长 **57.7 = 55.7 + 2.0**；
        /// · `Content` 高 **171.1 = 3×55.7 + 2×2.0** —— **逐位自洽**（不是拿一个数去凑）。</para></summary>
        const float RowSpacing = 2f;
        /// <summary>行距（= <see cref="RowH"/> + <see cref="RowSpacing"/>）= **57.7**。
        /// ⛔ 别把它写成字面量 —— 两个分量各有出处，拆开才查得动（`A1721` 之前那个 56 就是这么错的）。
        /// ⚠️ **改它要连 `maxScroll` 一起改**：原版滚动上界 = `Content 高 − 视口高`
        /// = `n × 57.7 − 2.0 − 644.1` ⇒ 下面 `RefreshDeckList` / `MaxDeckScrollPx` 那两处都写了 `− RowSpacing`。
        /// 只改 `RowPitch` 不改上界 ⇒ **多给 2px**（少给 ≈ `1.7×n`）的滚动量。</summary>
        const float RowPitch = RowH + RowSpacing;         // = 57.7（`A1721` 之前是 56，自陈「我们挑的」）
        /// <summary>卡组行里那颗**费用圆**（原版 `…/Deck Selector Card Info button/Background/Cost Image`）
        /// 的**框**（左上角 x + 边长）。
        /// <para>🔴 **2026-10-21（`A1722`）订正三版**：
        /// **① `17 / 38`（最老）** —— 按 dump 的 `Cost Image` 节点量出来的，而那颗在原版 prefab 里是
        ///   **0×0 的模板位**（布局跑之前的位置）⇒ 拿它量不出真值（⛔ 模板位不当权威）。
        /// **② `12.1 / 46.2`（`A1700`，本轮之前）** —— 拿**实拍**当尺子
        ///   （`资料/原版参照图/用户实拍_1017/卡组编辑界面参考.png` 的**蓝体墨迹** x `[12.1, 58.3]`）；
        /// **③ 本版 `3.377 / 61.7`** —— ②量的是【**墨迹**】，而原版给的是【**框**】。</para>
        /// <para>**判据（原版 prefab 现读 + 布局式子，逐条可复跑）**：卡行 `Cost Image` RT
        /// `1553532234772573988` 的 `m_AnchorMin/Max = (0.03,0)/(0.03,1)` ·
        /// `m_SizeDelta = (0, 6)` · `m_AnchoredPosition = (−0.1800079, 0)` · `m_Pivot = (0.5,0.5)`；
        /// 父 = 卡行 `Background` RT `−2197820584482115804`（**25.655 .. 317.400**，宽 291.745）；
        /// 同 GO 上 `AspectRatioFitter` `AspectMode = 2`（**高控宽**）· `m_AspectRatio = 1.0` ·
        /// `Image.m_Type = 0(Simple)` · **`m_PreserveAspect = 0`**（⇒ 图被**拉伸**进框）。
        /// ⇒ 中心 x = `25.655 + 0.03×291.745 − 0.1800079` = **34.227** ·
        /// 高 = `RowH 55.7 + 6` = **61.7** · 宽 = `61.7 × 1.0` = **61.7**
        /// ⇒ **绝对 3.377 .. 65.077**（61.7 见方）。</para>
        /// <para>**旁证（我们自己的 `Resources/Art/ui_deck/Card_Frame_Cost_Icon.png`，248×244）**：
        /// 拉伸进 61.7 方框后，它自身 `alpha≥128` 的**重墨迹**算出来 = **6.36 .. 60.60**，
        /// 而参考图实测（k = 1.21442，两种掩膜互为佐证）= **8.50 .. 60.72**
        /// ⇒ **右缘只差 0.12px**（左缘差 2.1px = 非整数降采样 1581/1920 的 ±1.2px 带内）
        /// ⇒ **是「框 vs 墨迹」，不是真偏离**（`A1528` 同族）。</para>
        /// ⚠️ 这一对**喂以下几处**：圆 `row_c*` 的**建**（`BuildDeckList`）与**搬**（`MoveRow`）、
        ///   以及圆里那个数 `row_cnt*` 的**搬**（`MoveRow` 的 `MoveLabel` —— 它只取**中心**）
        ///   —— ⛔ 别只改一处。
        ///   🔴 **2026-10-21（`A1735`）订正**：`row_cnt*` 的**建**从本批起**不再**吃这对（改吃
        ///   <see cref="RowCostTxW"/>/<see cref="RowCostTxH"/>，判据见那一段）——
        ///   原来那句「同时喂四处」已过期。
        /// ⚠️ **九宫格搬得动、改不了尺寸**那条限制**不管这一件** —— 费用图是普通 sprite（`m_Type=0`）、
        ///   不是九宫格。
        /// <para>🔴 **2026-10-21（`A1735`）就地订正（铁律 5）**：这里原来写「框变宽 ⇒ `SetAutoFitBox` 的框
        /// 跟着变（那一句读的就是 `RowCostS`）」—— **已过期**：那一句现在读的是
        /// <see cref="RowCostTxW"/>/<see cref="RowCostTxH"/>（**原版 TMP 自己的框 48.82 × 47.88**，
        /// 与这颗**费用图的框**是两件东西）。`RowCostS` 今天**只喂费用图**（建 + 搬）。</para></summary>
        const float RowCostX = 3.377f, RowCostS = 61.7f;
        /// <summary>🔴 **2026-10-21（`A1735`）：费用圆里那颗【数字】的 TMP 文本框**（px）——
        /// ⛔ **与上面那个 `RowCostS`（费用【图】的框）是两件东西**，别混。
        /// <para>判据 = 原版 prefab 现读（`工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 14`，
        /// 本笔 2026-10-21 复跑过）：
        /// · `…/Deck Selector Card Info button/Background/Cost Image/Cost` → **48.82 × 47.88**
        ///   （`TextMeshProUGUI` · `'5'` · `字号=50.0` · `基准=32.0` · `auto[18.0~50.0]` · `Center/Capline` · 白）；
        /// · 它的父 `Cost Image` = **61.70 × 61.70**（= 我们的 `RowCostS`）⇒ 四边**等距内缩**
        ///   （宽 `(61.7−48.82)/2 = 6.44` · 高 `(61.7−47.88)/2 = 6.91`）⇒ **框心 = 图心**；
        /// · 🔴 **2026-10-14（第二十九会话）就地订正（铁律 5）**：本行原来写「督军行那一族
        ///   （`Deck Selector Hero Card Info button/…/Cost`）**逐位同值**」—— **那是错的**：
        ///   该 prefab 里**督军行子树根本没有 `Cost Image` 也没有 `Cost`**（`Content` 下只有
        ///   `Background` / `Rarity Gradient` / `Background Border` / `Card Name` / `Count` /
        ///   `Ban Icon` / `Background Slot Free` / `Slot Free Text`）⇒ 「督军行同值」这句话
        ///   **没有出处**（当年大概是照「同族应该一样」推的）。
        ///   现读（2026-10-14 自己跑 `工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 14`
        ///   逐行核过）：**全树只有 3 个 `Cost Image`** ——
        ///   ① `Deck Selector Defensive Card Slot` 那一颗 **61.70 × 61.70** + `AspectRatioFitter 高控宽(1)`；
        ///   ② `Deck Selector Card Info button` 那一颗 **61.70 × 61.70** + 同款 ARF（**与①逐位同值**）；
        ///   ③ 卡槽**预览**那一颗 **62.00 × 62.00** · **没有 ARF** · 只有 `Image`（**另一族**，
        ///     它的 `Cost` 是 `m_SizeDelta = (−1.5, 0)` · 字号 32）。
        ///   ⇒ **本段那对 `48.82 / 47.88` 的旁证只有①②**（③不是同一件东西）。</para>
        /// <para>🔴 **改前我们把【图框】61.7 直接当字框用** —— 今天两边都停在自适应上限 `50`
        /// （`row_cnt` 是 1~2 位数字，61.7 与 48.82 都塞得下）⇒ **画面同级、一直没露**；
        /// 但**要缩字时我们会比原版【晚缩】**（原版的框窄 12.88px）—— 那才是不符原版的那一半。
        /// ⇒ 本节把**文本框**换成原版那个；⛔ **费用图那个 61.7 一个字都不动**。</para>
        /// <para>⚠️ 框**变小**而字没变 ⇒ 自适应重算（`SetAutoFitBox` 吃的是这一对）——
        /// 本批如实报告实绘字号（见施工报告）；两条断言（`DeckScene` 的 `A1735` 那一节）
        /// 一条钉这个框、一条钉「字仍停在上限」。</para></summary>
        const float RowCostTxW = 48.82f, RowCostTxH = 47.88f;
        /// <summary>行右缘那颗**份数**（原版 TMP 名 `Count`）的**框宽**（px）—— 见下面 `RowCountX`
        /// 那一段（**它只喂 `SetAutoFitBox` 的框与自动缩字**；**真正钉住画面的是【框的右沿】**）。
        /// 文本 = **`"x" + n`**（`UICardInfoItem__SetCount.c` 里那个 `String.Format` 的字面量
        /// = **`x{0}`**，小写 x、**无空格**；同函数 `gameObject.SetActive(1 &lt; count)`
        /// ⇒ **份数 ≤ 1（督军行 / 防御卡行）不显示**）。
        /// <para>🔴 **2026-10-11（`A1528`）：字号 / 字色 / 自适应三项就地订正（铁律 5）。**
        /// 这一段原来写「`anchor (1,0)-(1,1)` + `offsetMin.x = −50` + `pivot (1, 0.5)` ·
        /// 字号 32 · **字色 `(1,1,1,1)`**」——**那三条全是【另一族】读数**
        /// （`Deck Selector Hero Card Info button` = 督军行那一颗，原版**永远看不到**：
        /// `UICardInfoItem__SetCount.c:41` 的 `SetActive(1 &lt; count)`，督军恒 1 张）。
        /// **本窗左栏这一份属 `Deck Selector Card Info button` 族**（`Count` RT
        /// **`-2738939269725491420`**，父 `Text fill`）⇒ 实读：
        /// **`m_fontColor = (0.9333333969116211, 0.9333333969116211, 0.9333333969116211, 1.0)`** ·
        /// `m_fontSize = 32.0` · **`m_enableAutoSizing = 1`** · `m_fontSizeMin = 2.0` ·
        /// `m_fontSizeMax = 32.0` · `m_fontSizeBase = 36.0` · `H=4(Right)` / `V=4096(Midline)`。
        /// 裁定（按 RT pid 认人，15 个同名 TMP 分 3 族，铁律 4 按用途取 ⛔ 不投票）→
        /// `资料/普查产出_第十七会话/Z_两处悬案.md` §A ·
        /// 留痕 → `资料/历史/A表已收口_第十七会话.md` 的 `A1528` 那一行。</para>
        /// <para>⚠️ **这个 50 是【框宽】，不是几何真值**：Card Info 族那颗 `Count` 的框是
        /// **内容自适应**（`LayoutElement.m_MinWidth = **22.87**` · `m_PreferredWidth = 0` ·
        /// `m_FlexibleWidth = **−1**`），没有固定宽 —— `50` 的出处是 **Hero 族**。
        /// 本工程留 50 只为了给 `SetAutoFitBox` 一个**够宽的框**（框太窄 ⇒ 自适应会把字缩下去）；
        /// 画面由 **`RowCountX` / `RowCountRight`（右沿）** 决定，见那两段。</para></summary>
        const float RowCountW = 50f;
        /// <summary>🔴 **2026-10-11（`A1528` 第二轮）：`Count` 框的【右沿】相对**行节点右缘**的内缩**（画布 px）。
        /// <para>**判据（原版 prefab 字段 + 布局式子，逐条可复跑）**：
        /// · 行节点宽 = **309**（`Deck List drawer` 325 − 抽屉 `Content` 的 `VerticalLayoutGroup`
        ///   `m_Padding (8,8,0,0)` · `m_ChildControlWidth = 1` ⇒ 子件宽 = 325−16 = 309）；
        /// · `Background` RT `-2197820584482115804`（`Text fill` 的 `m_Father`，现读）：
        ///   `m_AnchorMin (0.055841296911239624, 0)` · `m_AnchorMax (1,1)` ⇒ 行内 **17.255..309.00**；
        /// · `Text fill` RT `-9081848734182936796`：`m_AnchorMin (0.13, 0.07)` · `m_AnchorMax (0.99, 0.93)` ·
        ///   `m_Pivot (0.5,0.5)` · `m_AnchoredPosition (-2.5, 0)` · `m_SizeDelta (-5, 0)`
        ///   ⇒ 行内 **55.182..301.083**（= `工具/menu_rect.py` 的 `rect_of` 锚点式子，与
        ///   `menu_dump.py "Deck List drawer" --depth 6` 印的 `63.6..309.5`（绝对，行在 8.4）**逐位吻合**）；
        /// ⇒ **`Text fill` 右沿 = 行右缘 − 7.917**。
        /// · **`Count` 的框右沿 = `Text fill` 右沿**：`Text fill` 的 `m_Children` 序 = `[Card Name, Count]`
        ///   （**`Count` 是最后一个**）；`Count` 的 `LayoutElement` 是 `m_FlexibleWidth = −1`，
        ///   而 `Text fill` 的 `HorizontalLayoutGroup` 是 **`m_ChildForceExpandWidth = 1`** ——
        ///   uGUI 的 `GetChildSizes` 是 `if (childForceExpand) flexible = Mathf.Max(flexible, 1)`
        ///   ⇒ **两个子件都拿 flexible ≥ 1** ⇒ 最后一个子件的**框右沿 = 容器右沿**。
        ///   ⛔ 别用「`Count` 的框宽」推位置 —— 它是内容自适应的（见 `RowCountW` 那段）。</para>
        /// <para>⚠️ **换算到我们自己的行**：我们的 `row_*` = `ListX..ListX+RowW` = **0.4..325.4**；
        /// 原版的 `Background` 是 **25.65..317.4**（291.75 宽）—— **那一层差异不属本条**（我们的行底板
        /// 左边多 25.25、右边多 8 ⇒ 宽 33.25，见下面的「新账」）。把原版那条**行相对**规则套上
        /// ⇒ 右沿 = `325.4 − 7.917` = **317.48**。
        /// 🔴 **2026-10-21（`A1700`）就地订正（铁律 5）**：上面这一整句的**行右缘取错了** ——
        /// `ListX+RowW` = **325.4** 是**抽屉**的右缘，行节点右缘是 **317.4**（抽屉各缩 8）。
        /// ⇒ 那条「行相对」规则算出来是 `317.4 − 7.917` = **309.48**（= 本文件 `RowCountRight` 今天给的值，
        /// 与参考图 `x2` 墨迹右缘 ref 255 ⇒ 画布 **309.7** 逐位吻合）。判据全文 → `RowNodeW` 那一段。</para>
        /// <para>🔴 **参考图（`用户实拍_1017/卡组编辑界面参考.png`，1581×887）本代理 2026-10-11 逐像素重量过**
        /// （`k = 1920/1581 = 1.21442`）：
        /// · `x2` 的**墨迹右缘** ≈ **ref 255 px** ⇒ 画布 **309.7**；
        /// · **行底板右缘**（那颗边缘高光） = **ref 261 px** ⇒ 画布 **317.0**。
        /// ⇒ 与 prefab **逐位吻合**（墨迹 309.7 vs **309.48**；底板右缘 317.0 vs **317.4**）。
        /// 🔴 **订正一条账文（铁律 5）**：原来这里（以及 `RowNameX` 那段）写的是
        /// 「`x2` 的**墨迹右缘** 实测 参考图 **261** px ⇒ 画布 **317.0**」—— **261 那一颗量的不是墨迹，
        /// 是【行底板右缘】**（它在图上是一条贯穿整行的边缘高光，比两个字的墨迹**更显眼**，所以被挑中了）。
        /// 墨迹在它**左边 6 ref px**（≈ 7.3 画布 px）—— 与 prefab 的 **7.917** 同量级 ⇒ **规则本身是对的**，
        /// 错的是**当时把哪个像素当成了墨迹**。</para>
        /// <para>✅ **那条「新账」2026-10-21（`A1700`）已收口**（原来写：我们的 `row_*` 底板比原版 `Background`
        /// **宽 33.25px**（左 −25.25 / 右 +8），根因 = `ListX/RowW` 取的是**抽屉**的 0.4..325.4）——
        /// 现在 `row_*` / `row_g*` / `row_b*` 三件**逐件**按原版两份行预制体的锚点算（判据全文与参考图对账
        /// → `RowBgHeroX` / `RowBgCardX` / `RowGradHeroX` / `RowGradCardX` 那四个常量上面的整段注释），
        /// 并且 `RowCountRight` 也跟着改成了「行节点右缘 − 7.92」= **309.48**。
        /// ⚠️ **今后再改行宽 / 行节点位置，这三个数（底板 / 色条 / 份数右沿）必须一起动**。</para></summary>
        const float RowCountRightInset = 7.92f;
        /// <summary>那颗 `Count` 的**框右沿**（画布 px）= **行节点右缘 − <see cref="RowCountRightInset"/> = 317.4 − 7.92 = 309.48**。
        /// 这是**唯一钉住画面**的数（`RefreshDeckList` 里那句 `AlignRightOn` 就用它）。
        /// <para>🔴 **2026-10-21（`A1700`）就地订正（铁律 5）**：这条式子原来写的是
        /// `ListX + RowW − 7.92` = **317.48** —— **用错了「行右缘」**：`ListX+RowW` 是**抽屉**的右缘（325.4），
        /// 而行节点的右缘是 **317.4**（抽屉再各缩 8，判据见 `RowNodeW` 那一段）⇒ 原来那颗字**顶到行外 7.92px**。
        /// 两条独立读数都指向 **309.48**：① 原版卡行 `Text fill` 的绝对右沿 = **309.4826**（raw RT 现算）；
        /// ② 参考图 `x2` 的墨迹右缘 = **ref 255** ⇒ 画布 **309.7**。
        /// （⚠️ 同段旧注释里「行右缘 325.4」的那些读数**都要按 317.4 重读** —— 凡 `− 325.4` 得来的数一律改看本条。）</para></summary>
        static readonly float RowCountRight = RowNodeX + RowNodeW - RowCountRightInset;
        /// <summary>那颗 `Count` 的**框左沿** = 右沿 − 框宽 = **259.48**（`275.4` 那个旧值是错的；
        /// `267.48` 是 A1700 之前那版 —— 同 `RowCountRight` 的理由）。</summary>
        static readonly float RowCountX = RowCountRight - RowCountW;
        /// <summary>行右缘那颗**份数**的字号（原版 `m_fontSize = 32`）—— 画在屏幕绝对 px 上，直接给。
        /// 🔴 **2026-10-11（`A1528`）**：同一个 `Count` TMP 的自适应四格 = **2 / 32 / 36 / 1**
        /// （`min 2` · `max 32` · `base 36` · 自适应开）⇒ 建的时候要跟一句
        /// `SetAutoFitBox`（落点见 `BuildDeckList`）。</summary>
        const float RowCountFontPx = 32f;
        /// <summary>行右缘那颗**份数**的字色 —— 原版 `m_fontColor = **(0.9333333969116211, 0.9333333969116211,
        /// 0.9333333969116211, 1.0)**`（RT `-2738939269725491420` 实读；见 `RowCountW` 那段判据）。
        /// ⚠️ **它不是 `Ink`**：`Ink` 的第三通道是 **0.95**（偏蓝），这一颗三通道**等值** 0.9333。</summary>
        static readonly Color RowCountTint = new Color(0.93333f, 0.93333f, 0.93333f, 1f);
        /// <summary>行名（原版 `…/Content/Text fill/Card Name`）的**框左沿 / 框宽**（画布 px）。
        /// <para>🔴 **2026-10-20 新增**：「行名原版左对齐、我们居中」那一条要用的两个数。</para>
        /// <para>判据一（**对齐档**，原版 prefab 实读）：`python 工具/menu_dump.py bundle_menus_assets_all
        /// "Deck Selector Card Info button"` → `Card Name …… 对齐=**Left/Capline**
        /// 色=(0.934,0.934,0.934,1)`（同一行的 `Count …… 对齐=Right/Midline`）。
        /// ⚠️ 我们原来是**居中**（`TmpFont.NewText` 把每颗 TMP 统一建成 `Center`）。</para>
        /// <para>判据二（**左沿**，**用户参考图**）：`资料/原版参照图/用户实拍_1017/卡组编辑界面参考.png`
        /// （1581×887 ⇒ 画布 px = 参考图 px × **1920/1581 = 1.21442**）。
        /// 🔴 **2026-10-11（`A1528` 第二轮）就地订正（铁律 5）**：这个系数原来是拿
        /// 「行右缘那颗 `x2` 的**墨迹右缘**实测 参考图 **261** px ⇒ 画布 317.0」校的 ——
        /// **261 那一颗不是墨迹、是【行底板右缘】**（本代理在图上逐像素重量过：墨迹右缘在
        /// **ref ≈255**，而 261 是那条贯穿整行的边缘高光）。**巧合的是这个系数仍然对**
        /// —— `261 × 1.21442 = 317.0` 正好等于原版 prefab 的行底右缘 **317.4**（差 0.4px），
        /// 而 `255 × 1.21442 = 309.7` 又正好等于 prefab 的 `Count` 墨迹右缘 **309.48**。
        /// ⇒ **两条独立读数互相印证，系数 1.21442 保留**（⛔ 别因为它「校错了对象」就丢掉它）。
        /// 10 行卡名的**墨迹左缘**实测 = 参考图 px **56…61** ⇒ **画布 68.0…74.1**
        /// （阈值 150/180/210 三档都落在这个区间）⇒ **取左界 68**。</para>
        /// <para>🔴 **2026-10-11（`A1528` 第二轮）再订正一句（铁律 5）**：下面原来写
        /// 「**原版 prefab 给不出这个 x** …… 横向全是模板位」——**那句话不成立**：
        /// 那一族 dump 出来 `0.00` 宽，是因为**没给行节点宽度**（模板位，铁律 3 的那个坑）。
        /// **一旦把行节点的真宽 309 喂进去**（`menu_dump.py … "Deck List drawer" --depth 6`
        /// 或 `--root-size 309x55.70`），prefab **给得出来**：
        /// `Text fill` = 行内 **55.182..301.083** ⇒ 卡名框左沿 = 行左 + 55.182
        /// ⇒ **绝对 63.58**（行节点在 8.4）。拿参考图核：墨迹左缘 68.0 − CJK 左留白（≈1~2px）
        /// = 框左 ≈ **66** ⇒ 与 63.58 同量级（差 ~2.4px，在参考图那条 6px 的读数带里）。
        /// ⚠️ **但本轮【没动】`RowNameX = 68`** —— 它属 `A1525③` 那条账，而且它踩的是
        /// **同一类错**（把**墨迹**左缘当成了**框**左沿，没减留白）⇒ **单开一条账报给调度台**，
        /// ⛔ 别在 A1528 里顺手改（`Editor/DeckScene.cs` 有一条断言正钉着 68）。
        /// （⚠️ 历史读数：`RowNameX` 更早那个 `62f` 是**没有出处的** —— 全库 grep 零命中。）</para>
        /// <para>⚠️ 框宽 190 保持不动：它只影响 `SetAutoFitBox` 的缩放上下限与换行宽，
        /// 而原版 `Card Name` 的宽度**同样是模板位**（那个 `HorizontalLayoutGroup` 工具量不出来）。</para></summary>
        const float RowNameX = 68f, RowNameW = 190f;
        /// <summary>一屏铺几行 —— 建行槽用的常量。
        /// <para>🔴 **2026-10-21（`A1727`）：`11 → 12`** —— **11 是一个用户可见的真缺陷**（最后一行永远看不到）：
        /// · **槽位永远填不满视口**：`644.1 / 57.7 = 11.16` ⇒ 原版**卷到 0 时会露出第 12 行的 9.4px 残行**
        ///   （参考图实测：蓝六边形带最后一颗在 ref y 827、**高只 6px** ⇒ 画布 `1004.3..1010.4`；
        ///   而行 11 顶 = `366 + 11×57.7` = **1000.7**、视口底 = `366 + 644.1` = **1010.1**
        ///   ⇒ 残行高 **9.4** —— **逐位对上**）。11 槽**根本不画那一行**；
        /// · **更严重**：**`max scroll` 时最后一行永远看不到** —— `firstRow_max = Count − 12`，
        ///   槽 `0..10` 只覆盖 `Count−12 .. Count−2` ⇒ **`Count−1` 那颗一个槽都没有**
        ///   （它算出来的屏上 y **落在视口内**，只是**没槽**）。
        /// ⚠️ **光改这个常量不够** —— 12 槽 = `11×57.7 + 55.7` = **690.4** > 视口 644.1
        /// ⇒ 顶/底那一行一定会**越出视口**，必须**同时**给它加视口裁切
        /// （`DeckListBand` + `ClipRowToBand`，判据见那两处）。</summary>
        const int RowVisible = 12;
        /// <summary>卡组列表的**视口矩形**（画布 px · 左上原点）= 原版
        /// `Deck Details > Deck List drawer/Scroll View/Viewport` 上那颗 `RectMask2D` 的等效物。
        /// <para>判据（两条互证）：① 原版那台滚动区的内容高 `= n×57.7 − 2.0`、视口高 **644.06**
        /// （`Content` 三行实测 171.1 ⇒ 单行 55.7、间距 2.0；🔴 2026-10-17 `A1736` 订正：原写 644.1 是四舍五入）；
        /// ② 参考图那条残行：行 11 顶 **1000.7** + 残行高 **9.36** = **1010.06** = `ListY + ListH` ⇒ 视口底就是它。</para>
        /// <para>⚠️ **它不随滚动变**：滚动只改行的绝对 y（`ListY + i×RowPitch − off`），框恒 = 本矩形。
        /// ⛔ 别写成两份（`RefreshDeckList` 的裁切与命中那半边要读同一份 —— 两处写同一条规则迟早不一致）。</para></summary>
        static readonly PxRect DeckListBand = new PxRect(ListX, ListY, ListX + ListW, ListY + ListH);

        // ============================================================ 🔴 2026-10-21（A1700）
        // **行底板那三件（`row_*` / `row_g*` / `row_b*`）的矩形 —— 按原版 prefab 逐件实读、分两档。**
        //
        // 判据（唯一权威 = 原版预制体 JSON，`d:/2/新解包资源/assets_full/bundle_menus_assets_all/`；
        //   `RectTransform/*.json` 的 `m_AnchorMin/m_AnchorMax/m_SizeDelta` 逐颗现读，再用 uGUI 那条
        //   「anchor×父宽 + pivot + anchoredPosition」式子算成绝对画布 px）：
        //   · 行节点（三份预制体**都一样**）= 抽屉 `Content` 的 `VerticalLayoutGroup m_Padding (8,8,0,0)`
        //     的子件 ⇒ 宽 = 325 − 16 = **309**、绝对 **8.4 … 317.4**
        //     （`python 工具/menu_dump.py bundle_menus_assets_all "Deck List drawer" --depth 4`
        //      印的那一行就是 `8.4 366.0 317.4`）。
        //   · **① 行 0 = `Deck Selector Hero Card Info button`**（RT `3862100441888845604`）：
        //       `Background` 锚 `(0,0)-(1,1)` ⇒ **8.400 .. 317.400**（宽 309）
        //       `Background Border` 同 ⇒ **8.400 .. 317.400**
        //       `Rarity Gradient` 锚 `0.611473560` + `sd.x −0.020004` ⇒ **197.364 .. 317.399**（宽 120.035）
        //     ⚠️ **督军行【没有** `Cost Image`**】**（原版 Hero 族那一棵里根本没有这个节点）⇒ 那一行的
        //       底板左缘**露在画面上**，参考图量的就是它（见下）。
        //   · **② 行 1+ = `Deck Selector Card Info button`**（RT `-5688290593639960796`）：
        //       `Background` 锚 `0.055841297`（×309 = 行内 17.255）⇒ **25.655 .. 317.400**（宽 291.745）
        //       `Background Border` 同 ⇒ **25.602 .. 317.400**
        //       `Rarity Gradient` 锚 `0.605750263` + `sd.x 0.4528`（**父 = `Background`**，291.745 宽）
        //       ⇒ **201.927 .. 317.400**（宽 115.473）
        //     ⚠️ `Deck Selector Defensive Card Slot`（原版**行 1**，RT `-6738926706166468828`）的
        //       `Background` / `Background Border` 与这一份**逐位相同**（同一个 `0.055841297`）⇒ **归同一档**；
        //       它的 `Rarity Gradient` 另带 `sd.x −6.372`（⇒ 187.18..302.63）—— 本窗**行 1 不是防御卡位**
        //       （我们这一行是**一个模板复用**，原版是三套预制体），梯度按 ② 走，**如实标注**。
        //
        // 🔴 **参考图对账（`C:/Users/qjh36/Desktop/warpforge参考图/卡组编辑界面参考.png`，1581×887 ⇒
        //   `k = 1920/1581 = 1.21442`，本件 2026-10-21 逐像素重量过）**：
        //   · **行 0（督军行，无费用圆 ⇒ 底板左缘不被挡）**：`x` 方向背景色 `(27,4,7)` 到 **ref 6.5** 为止，
        //     紧接一个**暗边**（ref 7 = `(13,1,1)`）＋**亮边**（ref 8-9 = `(71,58,54)`/`(138,129,122)`），
        //     再往右就是底板内部的 `(80,74,64)` ⇒ 左缘 ≈ **ref 6.5-7** ⇒ 画布 **7.9-8.5** ⇒ **与 8.4 吻合**
        //     （右缘：那条贯穿整行的亮线到 **ref 261** ⇒ 画布 **317.0** ⇒ 与 317.4 吻合）。
        //   · **行 1+（卡行）**：左缘**量不到** —— 费用六边形（原版 `Cost Image` 行内 `−5 .. 56.7`）
        //     正好压在底板的左端上。逐行量 `x = 2..14` 的均值色：**恒等于窗口底色 `(29,4,7)`**
        //     ⇒ **那一带没有底板**，底板左缘必 ≥ 画布 17 ⇒ **与 25.655 相容**（六边形在行顶那一刀
        //     只占 ref 15.5..40.9 ⇒ 量到「第一个亮点在 ref 19」是**六边形**、不是底板）。
        //   ⇒ **prefab 与参考图【一致】**（左缘 8.4 只在行 0 可量、量到了；卡行被挡、只能给「≥17」的区间）。
        //
        // 🔴 **本件否掉原来的两个数（铁律 5 就地订正）**：
        //   · 我们原来 `row_*` / `row_b*` = `ListX..ListX+RowW` = **0.4 .. 325.4**（宽 325）——
        //     那是**抽屉**的矩形，不是行节点的（行节点要再各缩 8）⇒ **左右各宽 8px**（= 本条账名）。
        //   · 我们原来 `row_g*` = `ListX + RowW*0.606 .. ListX + RowW` = **197.35 .. 325.4**（宽 128.05）——
        //     右缘也是抽屉右缘（多 8），而且 `0.606` 是把卡行那个**相对 `Background` 的锚**（0.605750263）
        //     套到了**抽屉宽**上（两次错互相抵消了一部分）。真值见上面那两档。
        /// <summary>行节点（原版三份行预制体的根）在抽屉里的矩形：`Content` 的 `VLG pad (8,8)` ⇒ 宽 309。
        /// 命中判定 / `UiRowRect` 用它（**行节点**才是那件 `EverguildButton` 的矩形）。</summary>
        const float RowNodeX = ListX + 8f, RowNodeW = RowW - 16f;      // 8.4 / 309
        /// <summary>行 0（督军行）的底板 / 描边矩形 —— 原版 `Deck Selector Hero Card Info button`
        /// 的 `Background` / `Background Border`（锚 `(0,0)-(1,1)`）。</summary>
        const float RowBgHeroX = 8.400f, RowBgHeroW = 309.000f;
        /// <summary>行 1+（卡行 / 防御卡行）的底板 / 描边矩形 —— 原版 `Deck Selector Card Info button`
        /// 的 `Background`（锚 `0.055841297`）⇒ 左缘比行节点再内缩 17.255。</summary>
        const float RowBgCardX = 25.655f, RowBgCardW = 291.745f;
        /// <summary>行 0 的稀有度色条（原版 `Rarity Gradient` 锚 `0.611473560`）。</summary>
        const float RowGradHeroX = 197.364f, RowGradHeroW = 120.035f;
        /// <summary>行 1+ 的稀有度色条（原版锚 `0.605750263`、父 = `Background`）。</summary>
        const float RowGradCardX = 201.927f, RowGradCardW = 115.473f;
        /// <summary>按**这一件装的是不是督军**取底板 / 描边的矩形（**建**与**搬**必须走同一份 —— 见 `BuildDeckList` 那段）。
        /// <para>🔴 **2026-10-21（`A1720`）参数从「槽号」改成「这一项是不是督军」** —— 上面那条残余已收口：
        /// 原版那三份行预制体是**按卡种选**的（`DeckListDrawer__SpawnCardInfo.c:29/40/51`），
        /// 而我们这里是**槽位复用**（`RefreshDeckList` 的 `firstRow = floor(_deckScroll / RowPitch)`，
        /// 槽 `i` 装的是 `shown[firstRow+i]`）⇒ 几何必须跟**装进去的那一项**走。
        /// **老写法（按槽号）只在一种状态下落空**：`firstRow ≥ 1`（往下滚过一行）时槽 0 装的是卡行、
        /// 却画了督军行的底板（左缘多 `25.655 − 8.400 = 17.255px`）。
        /// **唯一能装督军行的槽就是 0**（督军恒 `shown[0]`）⇒ 现在**槽 0 建两套件**：
        /// 督军档（`row_0` / `row_g0` / `row_b0`，本函数传 `true`）
        /// ＋ 卡行档（`row_0c` / `row_g0c` / `row_b0c`，本函数传 `false`），由 `RefreshDeckList` 二选一开。</para>
        /// <para>➕ **顺带订正一条（`A1700` 的注释）**：那里说「防御行 `sd.x −6.372` 是**第三档**」——
        /// 那是**别的窗**的实例（`bundle_menus_assets_all` 里带 `Rarity Gradient` + `sd.x −6.372`
        /// 的只有 2 份，父是 `8128755293864849188` / `4979183924807574862`，**不是 `Deck Editing Menu`
        /// 那两行**）；`Deck Editing Menu` 自己的防御行 dump = `202.0..317.4 宽 115.45` = **与卡行同档**
        /// ⇒ **两档模型不用加第三档**（⛔ 别加）。</para></summary>
        static void RowBgRect(bool hero, out float x, out float w)
        {
            if (hero) { x = RowBgHeroX; w = RowBgHeroW; } else { x = RowBgCardX; w = RowBgCardW; }
        }
        /// <summary>按**这一件装的是不是督军**取稀有度色条的矩形（同 `RowBgRect`）。</summary>
        static void RowGradRect(bool hero, out float x, out float w)
        {
            if (hero) { x = RowGradHeroX; w = RowGradHeroW; } else { x = RowGradCardX; w = RowGradCardW; }
        }

        // ---- 费用曲线（原版 `Sidebar/Deck Details/Deck Information cost drawer`；只在 Deck info 页签下）----
        // 🔴 **2026-10-17（施工单 D30–D32）**：整段原来按 **1.0** 摆 —— **漏了抽屉自己的 `m_LocalScale`**，
        //    于是 9 行挤在 178px 里、三列整体内缩（本轮之前那版就是这么错的）。
        // 判据（唯一权威 = 原版预制体 JSON，`d:/2/新解包资源/assets_full/bundle_menus_assets_all/`）：
        //   · 抽屉 RT **`7431712229497630500`**：`m_LocalScale = **1.48**` · `m_Pivot = (0.5, **1**)` ·
        //     `m_AnchorMin/Max = (0.5,1)` · `m_AnchoredPosition = (0,-50)` · `m_SizeDelta = (158.15, 199.06)`。
        //   · `Content`（`-4122267309978095836`）挂 **`VerticalLayoutGroup`**：`m_Spacing = **3.43**` ·
        //     padding 全 0 · `m_ChildAlignment = 0`(UpperLeft) · 两个 `m_ChildControl*` 都是 0；
        //     行 `Deck CostQuanityt Row Drawer`（`-3064245899190735068` 等 9 颗）`m_SizeDelta = (223.59, **18.91**)`
        //     ⇒ **行距 = 18.91 + 3.43 = 22.34**（未缩放。VLG 从顶往下排 ⇒ 首行贴 `Content` 顶）。
        //   · 三列 `Card Cost` / `Slider` / `Cards in deck` 是行下的**序列化**子件（anchors 全 (0.5,0.5)、
        //     `m_AnchoredPosition.x` 分别 −95.64 / ≈0 / +95.23）⇒ 位置固定、没有 LayoutGroup 参与。
        // ⇒ 下面**每个数 = 「原版未缩放字段 × 1.48」**（缩放是节点级的，只放大比例、不改比例关系）。
        // ⚠️ **坑（本件就是被它掩盖的）**：`d:/2/Warpforge_tools/data/ui_layout/_deck_editing_godot_rects.txt`
        //    那张 chain_rect 表**逐级都不乘节点 scale** —— 照抄它整段会小 **1.48** 倍。
        // ⚠️ 同一颗抽屉在**别的窗里是别档**：Deck info 弹窗 **1.8**、战斗入口那两扇 **1.2**
        //    （`普查产出_0923/A1_外壳与弹窗.md:79` · `阶段二_战斗入口_原版规格.md:87`）—— ⛔ 别把别处的系数搬过来。
        const float CurveScale = 1.48f;
        /// <summary>行 0（费用 0）的**顶** —— 实读 = **410.970016**，与抽屉 `Background` 的顶**同一个数**
        /// （VLG 首行贴顶）。</summary>
        const float CurveY = 410.97f;
        const float CurveRowH = 18.91f * CurveScale;      // 27.9868
        const float CurveStep = 22.34f * CurveScale;      // 33.0632
        /// <summary>左列 `Card Cost`（原版 `m_SizeDelta.x = 25.56`、缩放后 x **7.4447..45.2735**、中心 **26.359**）。
        /// ⛔ 别再写成「行左 − 14」—— 那是 `Content` 那棵的原点，中心会**偏右 28.8px**。</summary>
        const float CurveLbX = 7.4447f, CurveLbW = 25.56f * CurveScale;
        /// <summary>右列 `Cards in deck`（原版 `m_AnchoredPosition.x = +95.23` ⇒ 缩放后 x **289.9323..327.7611**、
        /// 中心 **308.847**）。⛔ 别再写成「槽右 + 6」—— 中心会**偏左 42.4px**。</summary>
        const float CurveNumX = 289.9323f, CurveNumW = 25.56f * CurveScale;
        /// <summary>行里那根槽 —— 画出来的其实是 `Slider/Background` 那颗 `Image`
        /// （`m_SizeDelta = (151.34, **16.57**)` · 无九宫格），**不是** `Slider` 自身的 151.34×20。
        /// 缩放后 x **55.9147..279.8979**（宽 223.9832）· 高 24.5236 · **顶距行顶 2.1349**。
        /// ⚠️ `CurveBarW` 同时喂 `RefreshDeckList` 里的归一化宽度（按 10 张）—— 两处必须同一个数。</summary>
        const float CurveBarX = 55.9147f, CurveBarW = 151.34f * CurveScale;
        const float CurveBarDy = 2.1349f, CurveBarH = 16.57f * CurveScale;
        /// <summary>槽里的填充（原版 `Slider/Fill`：锚 0..1 + `sd.y = −4.74` ⇒ 高 15.26、竖直居中于 `Slider`）
        /// ⇒ 缩放后高 22.5848 · **顶距行顶 2.7010**；**宽由张数给**（`SetQuadWidth`，按 10 张归一化）。</summary>
        const float CurveFillDy = 2.7010f, CurveFillH = 15.26f * CurveScale;
        /// <summary>两列数字的字号：原版 `Card Cost` / `Cards in deck` 的 TMP **都是 `m_fontSize = 25`**
        /// （两处逐个实读 · autosize 关 · Center/Middle）⇒ 缩放后 **37**。
        /// ⚠️ 我们画在**屏幕绝对 px** 上、**不是真挂在那颗 1.48 的节点下** ⇒ 字号这一档得**自己乘**
        ///   （原来给的是 `Txt(..., scale: 1)` = 大写高 7px，比原版**小 3.8 倍**）。</summary>
        const float CurveFontPx = 25f * CurveScale;
        /// <summary>🆕 **2026-10-17（D48）**：两列数字的**字色** —— 原版**不是同一个颜色**（我们原来两处都传 `Ink`）。
        /// 判据 = 那两颗 TMP 自己的 `m_fontColor`（`bundle_menus_assets_all/MonoBehaviour/` 逐份实读）：
        ///   · `Card Cost`（左列）= `MB **7891025530621652772**` ⇒ **绿 `(0.29557, 0.77358, 0.49591)`**
        ///   · `Cards in deck`（右列）= `MB **871731656867376932**` ⇒ **白 `(1, 1, 1)`**
        /// ⚠️ 与 `_verdict`（页脚那句合法判词，**我们自加的**）的绿**不是同一个数** —— 别互相顶替。</summary>
        static readonly Color CurveLbTint = new Color(0.29557f, 0.77358f, 0.49591f, 1f);
        static readonly Color CurveNumTint = new Color(1f, 1f, 1f, 1f);
        /// <summary>🆕 **2026-10-17（D33）**：**最后一格（费用 8）是「8 及以上」** —— 两件事一起：
        ///   ① 那一格的计数 = 费用 **≥ 8** 的张数；
        ///   ② 那一格的字带后缀 **`"+"`**。
        ///
        /// <para>🔴 **判据（反编译，逐句）**：
        /// · `DeckCostQuantityRowDrawer.cs` 的字段 `/ Tooltip`：`maxCostToDraw` ——
        ///   **`Will add a + on the text where the cost is this value`**；**9 颗 row 的序列化值全是 8**
        ///   （全包枚举 `MonoBehaviour/*.json` 的 `maxCostToDraw` = 8，逐颗吻合）。
        /// · `DeckCostQuantityRowDrawer__Initialize.c:99-109`（逐句）：
        ///   `+0x28`（= **`cardsInDeck`** 那颗 TMP）吃 `ToString(cardsInDeck)`；
        ///   `+0x30`（= **`cardCost`** 那颗 TMP）吃 `ToString(cardCost)`，**且**
        ///   `if (cardCost == *(int*)(this + 0x50 /* maxCostToDraw */)) uVar3 = Concat(uVar3, DAT_…275430)`
        ///   ⇒ **后缀加在 `Card Cost` 那一列（左列）**。
        /// · 字段→偏移的对应**不是猜的**：拿 prefab 里那颗 MB（`MB -5134945137831081560`）的
        ///   `cardsInDeck` / `cardCost` 两个引用解出来 —— 分别指向 `GameObject('Cards in deck')` 与
        ///   `GameObject('Card Cost')`（`m_text` 都是 `"0"`），与 `DeckCostQuantityRowDrawer.cs` 的
        ///   声明序（`slider` / `cardsInDeck` / `cardCost` / `fillImage` / …）逐项吻合。
        /// · **后缀串本身** = `DAT_184275430` ⇒ 按 `RVA = 0x184275430 − ImageBase(0x180000000)` = `0x4275430`
        ///   查 `d:/2/tools/il2cpp_out/stringliteral.json` = **`"+"`**（单字加号）。
        /// · 「8 及以上」这半边：`DeckEnergyCostDrawer__Initialize.c:150-158` —— 费用超过行数的那些
        ///   **被夹进最后一格**（`piVar1 = counts + (cost−1)*4; *piVar1 += 1`）。
        ///   ⚠️ **我们原来既没夹、也没印 `+`**：`CostCurve()` 给的是 0..20 共 21 桶，而这里只读前 9 个
        ///     ⇒ 费用 9..20 的卡**一张都没算进曲线**（静默漏计）。两处一起修。</para>
        ///
        /// <para>🔴 **订正施工单一处**：`对账_卡组编辑部分_差异与待办.md` D33 那一行把后缀记在
        ///   **`Cards in deck`** 上 —— **那是错的**，判据（上面 `:99-109` 那段 + 两个字段解引用）
        ///   指向 **`Card Cost`**。⛔ 别照着施工单改到右列去。</para></summary>
        const int CurveLastBucket = 8;
        const string CurvePlusSuffix = "+";
        static string CurveCostText(int c) { return c == CurveLastBucket ? c + CurvePlusSuffix : c.ToString(); }
        /// <summary>抽屉**底板** = 原版 `Deck Information cost drawer/Background`（兄弟序在 `Content` **之前**）：
        /// `m_Sprite = null`（**纯色**，`m_Type = 0` Simple）· `m_Color = **(0.12264151, 0.12264151, 0.12264151, 1)**`。
        /// 缩放后 = 抽屉那颗的矩形 **x 50.8753..284.9373 × y 410.9700..705.5788**（234.062 × 294.609）。</summary>
        const float CurveBgX = 50.8753f, CurveBgY = 410.97f;
        const float CurveBgW = 158.15f * CurveScale, CurveBgH = 199.06f * CurveScale;
        static readonly Color CurveBgTint = new Color(0.12264151f, 0.12264151f, 0.12264151f, 1f);

        // ---- 🆕 卡组类型计数抽屉（原版 `Sidebar/Deck Details/Deck Counter Drawer`）------------
        // 🔴 **`A1514`（2026-10-11）：原版**有**、我们**没建**（缺件，铁律 11 —— ⛔ 不是「原版没有」）。
        // 判据（逐条带出处，`bundle_menus_assets_all` 实读）：
        //   · 节点 = `Deck Counter Drawer`，类 **`DeckTypeCounterDrawer`**，序列化字段
        //     **`troopCount` @ 0x20** · **`stratagemsCount` @ 0x28**（`d:/2/tools/il2cpp_out/dump.cs`
        //     的 `DeckTypeCounterDrawer` 那段；字段名↔偏移现核）。
        //   · RT **`-4772579933788246236`**（GO 文件 `GameObject/Deck Counter Drawer.json` 的第一颗组件）：
        //     `m_AnchorMin (0,1)` · `m_AnchorMax (1,1)` · `m_Pivot (0.5,1)` · `m_AnchoredPosition (0,−400)` ·
        //     `m_SizeDelta (0,100)` · **`m_LocalScale = 1`**（⚠️ 与费用曲线那颗 `1.48` **不同档** ——
        //     铁律 5·c：同一族的件在不同窗里是不同档，别把曲线那个系数搬过来）。
        //     父 = `Deck Details`（绝对 `0.25,360.97 → 335.56,1010.03`）⇒ 这颗绝对
        //     **x 0.25..335.56 · y 760.97..860.97**（`menu_dump.py … "Deck Counter Drawer"` 印的是
        //     四舍五入后的 `(0.3,761)-(335.6,861) = 335.31×100`）。
        //   · 两个子件（各自 RT 的 anchors/`sizeDelta` 决定它的半格）：
        //     `Text (TMP) (1)`（**`stratagemsCount`**，RT `2367829158540668708`，anchors (0,0.5)-(1,1)
        //       ⇒ **上半格** y 760.97..810.97）· `Text (TMP)`（**`troopCount`**，RT `-3202665307648495836`，
        //       anchors (0,0)-(1,0.5) ⇒ **下半格** y 810.97..860.97）；
        //     两颗都是 `m_AnchoredPosition.x = 12.5` + `m_SizeDelta.x = −25` ⇒
        //     **x 25.25..335.56（宽 310.31）· 高 50**（dump 相对根印 `25.0..335.3`）。
        //   · 两颗 TMP 的样式（逐份实读）：`m_fontSize = 36` · `m_fontSizeBase = 36` ·
        //     **`m_enableAutoSizing = 0`** · `m_fontColor = (1,1,1,1)` ·
        //     `m_HorizontalAlignment = 1(Left)` / `m_VerticalAlignment = 512(Middle)` ·
        //     `m_TextWrappingMode = 1(Normal)`。字体资产挂 `Localize`：
        //     `troopCount` → **`MenuDeck/HUD/DeckDescription/Minions`** ·
        //     `stratagemsCount` → **`MenuDeck/HUD/DeckDescription/Spells`**
        //     （`GO 2822477237084221220` / `7686141386449022756` 那两颗 `Localize.mTerm`）。
        //   · **写什么字**（第一权威 = 反编译方法体，`DeckTypeCounterDrawer__Initialize.c` 逐句）：
        //     `String.Format("{0}: {1}", GetTermTranslation(键), Count(deck.cardLibrary, 谓词))` ——
        //     格式串 = `DAT_18426e800` ⇒ 查 `d:/2/tools/il2cpp_out/stringliteral.json` = **`"{0}: {1}"`**；
        //     词条键 = `DAT_1842cfdf0` / `DAT_1842cfef0` ⇒ 同上现查 =
        //     **`MenuDeck/HUD/DeckDescription/Minions`** / **`…/Spells`**。
        //     ⛔ **不能按 TMP 的 `m_text`（`Troops` / `Stratagems`）印** —— 那是 prefab 的占位原文，
        //     运行期被 `Initialize` 覆写成「词条: 数字」（与 `Empty Warning` 那次同一族）。
        //   · **数什么**（同文件的谓词，`DeckTypeCounterDrawer.<>c.<Initialize>b__2_0/1.c` 逐句）：
        //     `*(int*)(card+0x94) == 0` ⇒ **`CardTypeOptions.Minion = 0`**；
        //     `== 0x14` ⇒ **`CardTypeOptions.Tactic = 20`**（枚举出处
        //     `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardTypeOptions.cs`：
        //     `Minion 0 / Hero 10 / Tactic 20 / Whispers 40`）⇒ 我们的 `CardDef.Type`
        //     **`"unit"` / `"tactic"`** 两档（督军 hero 与防御卡 defence **都不计**）。
        //     ⚠️ 数的是 **`deck.cardLibrary`（一份拷贝一条）**，不是「有几个不同的 id」——
        //     `DeckListDrawer` 靠 `stackCopies=1` 把同名牌折成一行正说明那里有重复项 ⇒
        //     我们按 **`State.Deck.CardIds`（含重复）** 数。
        //   · **什么时候露**（`DeckEditingPanel__Toggle{DeckList,DeckInfo,Cosmetics}.c` 逐句，
        //     字段↔偏移按 `dump.cs` 的 `DeckEditingPanel` 段现核）：
        //     `ToggleDeckInfo` → `deckCounterDrawer(0x38) → 1`（同一次调用里 `deckList(0x28) → 0` ·
        //     `deckCostDrawer(0x40) → 1` · `deckCosmeticDrawer(0x30) → 0`）；
        //     `ToggleDeckList` → **`deckCounterDrawer → 0`**（Cards 页**不显示**）；
        //     `ToggleCosmetics` → 也是 0。⇒ **只在 Deck info 页签显示** ⇒ 与费用曲线一样进 `_infoOnly`。
        //     ⚠️ **订正一处账文**（铁律 5）：`A1514` 的正文把两颗写反了
        //     （「`deckCounterDrawer(0x40)` / `deckCostDrawer(0x38)`」）—— 上面是按 `dump.cs` 的字段偏移
        //     现核的**正确对应**（`deckCounterDrawer` @ **0x38** = `DeckTypeCounterDrawer`，
        //     `deckCostDrawer` @ **0x40** = `DeckEnergyCostDrawer`）。结论（两颗都 → 1）不受影响。
        // 🔴 **一次刷新两个数**（`DeckEditingPanel__UpdateDrawers.c` 逐句）：它**无条件**依次调
        //   `DeckTypeCounterDrawer.Initialize(editingDeck)` → `DeckEnergyCostDrawer.Initialize(…)` →
        //   `DeckCosmeticDrawer.Initialize(…)` ⇒ 卡组一变（加/删卡）两个抽屉**同时重算** ——
        //   我们的落点 = `RefreshHeader`（`MarkDeckDirty` 的出口、与费用曲线同一个刷新一拍）。
        const float TcX = 25.25f, TcW = 310.31f, TcH = 50f;
        /// <summary>上半格 —— 原版 `Text (TMP) (1)` = `stratagemsCount`（策略数）。</summary>
        const float TcStratY = 760.97f;
        /// <summary>下半格 —— 原版 `Text (TMP)` = `troopCount`（部队数）。</summary>
        const float TcTroopY = 810.97f;
        /// <summary>两颗 TMP 的 `m_fontSize`（原版 36；自适应**关**）。</summary>
        const float TcFontPx = 36f;
        /// <summary>词条键（原版 prefab 上那两颗 `Localize.mTerm` 的原文）。</summary>
        const string TcTermTroops = "MenuDeck/HUD/DeckDescription/Minions";
        const string TcTermStratagems = "MenuDeck/HUD/DeckDescription/Spells";
        /// <summary>原版 `DeckTypeCounterDrawer.Initialize` 里那个 `String.Format` 的**格式串**
        /// （`DAT_18426e800` ⇒ `stringliteral.json` = `"{0}: {1}"`）—— 词条译名 + 空格冒号 + 数字。</summary>
        const string TcFormat = "{0}: {1}";

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
        //    本界面是 1.5 ⇒ **393.75×576 · 4 列**）。
        //    🔴 **2026-10-17（D24）：这一套【已实现】** —— 下面 `Pool` 那个属性按开关现算（状态 → 参数）。
        //    硬证据（私有 `Initialize()` 的指令流，RVA `0x89F460`）与两套的对照表都在
        //    `项目任务.md` §三 第 12 条 第 6 项；开关本体 = `Shell/TransformScalerBySmallScreenUI.cs`
        //    的静态类 `SmallScreenUI`（= 原版 `GameStaticData.smallScreenUI`，⛔ 别在别处再存一份）。
        const float PoolX = 330.2f, PoolY = 156f, PoolW = 1589.8f, PoolH = 924.1f;
        // ---- 以下全是**桌面那一档**的基准值（= 乘数 1）；小屏那一档**逐项 ×1.5**（见 `Pool`）----
        const float PoolCellW0 = 262.5f, PoolCellH0 = 384f;   // 原版**卡位**（不是 `Collection Card` 那张图的 350×512）
        const float PoolMobileSizeScale = 1.5f;               // 原版本界面 `_mobileSizeScale` 的值（判据同上）
        /// <summary>🆕 **2026-10-13（A1702 ③）**：卡池那一颗 `RectMask2D` 的**等效物** —— 视口矩形
        /// （画布 px · 左上原点）。判据 = 原版 `Content Area/Card Display/Scroll View/**Viewport**` 上挂的
        /// `RectMask2D`（`m_Softness=(0,0)` · `m_Padding=(0,0,0,0)` ⇒ **硬边、不内缩**，
        /// 与收藏窗那四处**逐字段相同**），视口尺寸 1589.8×924.1 的出处见上面 `PoolX/…` 那一段。
        /// <para>两处共用**同一个矩形**：① 渲染那一刀（<see cref="RefreshPool"/> 把它喂给
        /// `CardView.SetPose(…, clip:)`）② 命中那一刀（<see cref="HandlePoolClick"/> 拿它挡越界的点）。
        /// ⛔ 别写成两份 —— 两处写同一条规则 = 迟早不一致（本项目反复踩过）。</para>
        /// <para>⚠️ 两档（桌面 / 小屏）**同一份**：原版那颗 `RectMask2D` 的框**不随**
        /// `_mobileSizeScale` 变（变的是**格**尺寸与列数，那是 `Pool` 属性管的那一半）。</para></summary>
        static readonly PxRect PoolViewClip = new PxRect(PoolX, PoolY, PoolX + PoolW, PoolY + PoolH);
        // 🔴 **2026-10-18（A870）：一屏铺几行【不是常量】。** 原来是 `const int PoolRows = 3`（**我们挑的**），
        //    现在与列数那半（`Pool.Cols`）**同一种写法**：`Pool.Rows` 按**视口高**现算。
        //    判据 = 原版 `PolyAndCode.UI.VerticalRecyclingSystem`（逐字读，两处）：
        //    ① **覆盖倍数 `MinPoolCoverage = 1.5f`** —— `PolyAndCode.UI.VerticalRecyclingSystem__.ctor.c`：
        //       `*(undefined4 *)(param_1 + 0x34) = 0x3fc00000;`（= **1.5f**）；紧邻的 `+0x38 = 0x14`（= **20** 格）。
        //       ⚠️ 偏移↔字段名 = 桩 `PolyAndCode/UI/RecyclingSystem.cs` 的字段序（`DataSource/Viewport/Content/
        //       PrototypeCell/IsGrid/MinPoolCoverage/MinPoolSize/…` ⇒ `0x10/0x18/0x20/0x28/0x30/0x34/0x38`），
        //       **而且语义自洽**：`+0x34` 全方法只在一处当「视口高的倍数」用、`+0x38` 只在一处与 `GetItemCount()` 取 min。
        //    ② **它怎么用** —— `..._CreateCellPool.c:175,178-179,193`：
        //       `fVar1 = MinPoolCoverage(0x34); fVar20 = Viewport(0x18).rect.height(rect+0xc);
        //        while (iVar17 < iVar18 || (fVar22 < fVar20 * fVar1)) { …建一格… }`
        //       而总高 `fVar22` 的累加那句在 `:256-258`，**网格档（`IsGrid ≠ 0`）只有 `:271` 的 `goto` 到得了它**
        //       ——那就是「列计数 `0xac` 回绕」那一支 ⇒ **网格档里每换一行才加一次格高**
        //       ⇒ 判据落成 **行数 = ceil(视口高 × 1.5 ÷ 格高)**（⛔ 不是「每格加一次」，那样会算出 1/列数 那么小）。
        //    ③ **池格数下限那一条也照搬**：`iVar18 = max(_poolSize(0x88), min(MinPoolSize = 20, GetItemCount()))`
        //       （同文件 `:186-192`，当**格下标**阈值用）⇒ 行数还要 ≥ `ceil(20 ÷ 列数)`。
        //       ⚠️ 本界面 `_poolSize = 0`：`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-1327870198181403947.json` 逐字段实读。
        //    ⇒ **两档的值**（格高/列数见上面两条）：桌面 `max(ceil(20÷6)=4, ceil(924.06×1.5÷384)=4)` = **4 行 / 24 格**；
        //       小屏 `max(ceil(20÷4)=5, ceil(924.06×1.5÷576)=3)` = **5 行 / 20 格**。
        //       ⚠️ **小屏的行数比桌面【多】**（格数下限恒 20、列数变少 ⇒ 行变多）—— 这正是铁律 5·c 要的「状态 → 参数」，
        //       ⛔ 别按直觉以为「小屏格子大 ⇒ 行数少」。
        //    ④ **超过卡数的格不用另外截**：原版那条 `:215 if (GetItemCount() <= i) break;` 在我们这儿等价于
        //       `RefreshPool` 里本来就有的 `idx < all.Count`（不建 = 不画）。

        /// <summary>原版 `PolyAndCode.UI.RecyclingSystem.MinPoolCoverage` —— 池子至少要盖住 **1.5 个视口高**。
        /// 硬证据 = `PolyAndCode.UI.VerticalRecyclingSystem__.ctor.c`（`*(param_1 + 0x34) = 0x3fc00000`）。</summary>
        const float PoolMinCoverage = 1.5f;
        /// <summary>原版 `PolyAndCode.UI.RecyclingSystem.MinPoolSize` —— 池子至少 **20 格**
        /// （`__.ctor.c`：`*(param_1 + 0x38) = 0x14`；用在 `..._CreateCellPool.c:186-192`）。</summary>
        const int PoolMinSize = 20;
        const float PoolSpacing = 0f;                         // 原版 `_spacingX/_spacingY` 都是 0

        // ---- 🆕 2026-09-28：卡位里**底下那条「张数」**（原版 `Collection Card/Content/Counter`）----
        // 出处：`bundle_menus_assets_all/RectTransform/RectTransform_9015809549177451864.json`
        //   `m_AnchorMin = (0.2857143, 0.002)` / `m_AnchorMax = (0.7142857, 0.09966714)` ·
        //   `sizeDelta = (0,0)` · `pivot = (0.5, 0)` ⇒ 相对 262.5×384 的卡位：
        //   **x 75.0…187.5（宽 112.5）· 距格底 0.768…38.272（高 37.504）**，换成左上原点 = y 345.728…383.232。
        // 🔴 **卡池里那张卡的高度 = CellH − 这条 = 345.728，不是 384。**
        //   实拍印证（桌面《卡组编辑界面参考.png》竖剖量亮带）：卡框 **331.5** ≈ 0.9777（= 卡框 814.25 / 卡体 832.825）
        //   × 345.73；而且**卡框下沿在张数条之上**（原来我们按 384 画 ⇒ 卡顶满格子、那一整条会把卡底压住）。
        // ⚠️ 这一条的**锚点全是相对量**（0.2857… / 0.002… / 0.0997…）⇒ 小屏那一档它**跟着格子一起 ×1.5**
        //   （`38.272 × 1.5 = 57.408`，与 `0.0997 × 576` 逐位吻合）。
        const float PoolCounterH0 = 38.272f;
        const float PoolBarX10 = 75f, PoolBarX20 = 187.5f;     // 底图框（宽 112.5，格内水平居中）
        const float PoolCntY10 = 358.29f, PoolCntY20 = 380.98f; // 字框（高 22.69，居中于底图框）
        /// <summary>那条底图 = `40K_main_deck_card counter`（116×36 · **无九宫格** · 原版 `m_PreserveAspect = 1`
        /// ⇒ 内接进 112.5×37.5 后**实绘 112.5×34.91**）。
        /// 🔴 **不是** `40k_CardAmount_bar_bg` / `_fill` —— 那两张是 **Slider 型**（费用曲线那一批），
        ///   遍历 `Collection Card` 全子树证实卡位上没有它们（判据文件原来指错了图，2026-09-28 已订正）。</summary>
        const string PoolCounterSprite = "40K_main_deck_card_counter";
        /// <summary>那行字的字号 —— 原版 `m_fontSize = 31.9` · **autosize 7…32** · `Center/Midline` · 白。
        /// ⚠️ 字框只有 22.69 高 ⇒ 原版运行时会被 autosize 压小（我们走 `SetAutoFitBox`，同一条路）。
        /// ⚠️ **小屏那一档【不乘 1.5】** —— 原版 `_mobileSizeScale` 缩放的是**格尺寸**
        ///   （`_cellWidth/_cellHeight`），字号是 TMP 自己的 autosize 按框算出来的（我们走 `SetAutoFitBox`，
        ///   框大了它自己会往上顶）⇒ 传进去的四个数**两档同值**。</summary>
        const float PoolCounterPx = 31.9f;

        /// <summary>🆕 **2026-10-17（D24）**：**现在生效的那一套卡池版面参数** —— 按「小屏 UI」开关**现算**
        /// （⛔ 不许再写成编译期常量：那张「状态 → 参数」表是铁律 5·c 要求的那种形状）。
        ///
        /// <para>两套（判据 → `项目任务.md` §三 第 12 条 第 6 项；硬证据 = 原版私有 `Initialize()`
        /// 的指令流 RVA `0x89F460`）：</para>
        /// <list type="bullet">
        /// <item>**桌面**（`smallScreenUI = false`）：格 **262.5×384** · **6 列** · 视口 1589.8×924.1</item>
        /// <item>**小屏**（`= true`）：格 **393.75×576**（= 桌面 × `_mobileSizeScale` **1.5**）· **4 列**</item>
        /// </list>
        ///
        /// <para>🔴 **列数【不是】写死的 4/6** —— 原版是
        /// `_coloums = floor(viewport.rect.width ÷ (_cellWidth + _spacingX))`
        /// （`PolyAndCode.UI.VerticalRecyclingSystem__CreateCellPool.c` L127-167，**用 `_cellWidth` 原值、
        /// 不乘任何系数**）⇒ 两档都由同一条式子算出来：`floor(1589.8 ÷ 262.5) = 6` ·
        /// `floor(1589.8 ÷ 393.75) = 4`。这里照那条式子写，而不是「if 小屏 then 4」。</para>
        ///
        /// <para>🔴 **居中的量（`PadX`）两档恰好都是 7.4** —— 桌面 6×262.5 = 1575、小屏 4×393.75 = 1575，
        /// 同一个数（`RecyclableScrollRect` 的居中量常量 `0x1834b2bb4 = 0.5f`）。⛔ 别据此把 `PadX` 写死成 7.4：
        /// 它仍是 `(视口宽 − 列数×格宽) ÷ 2`，两档相等是**算出来的巧合**。</para>
        ///
        /// <para>⚠️ **每次访问都现算**（开关是静态、运行期可翻；这几条算式是纯算术，没有分配）。
        /// 换档之后要 `RefreshPool()`（调用点：`UiSetSmallScreenUIForTest` / 设置窗那颗开关之后的刷新）。</para></summary>
        struct PoolMetrics
        {
            public float CellW, CellH;                 // 卡位
            public int Cols;                           // = floor(视口宽 ÷ 格宽)
            public int Rows;                           // 🆕 A870：= max(ceil(池格下限 ÷ 列数), ceil(视口高 × 1.5 ÷ 格高))
            public float PadX;                         // 内容整体居中的左边距
            public float CounterH, CardH, CardScale;   // 底下那条 / 卡本身 / 卡的缩放
            public float BarX1, BarX2, CntY1, CntY2;   // 张数条在格内（相对格左上角）的四个数
        }

        static PoolMetrics Pool
        {
            get
            {
                float k = SmallScreenUI.Enabled ? PoolMobileSizeScale : 1f;
                var m = new PoolMetrics();
                m.CellW = PoolCellW0 * k;
                m.CellH = PoolCellH0 * k;
                // 🔴 列数走**原版那条式子**（见上面 doc），⛔ 不是「小屏 ⇒ 4」。
                m.Cols = Mathf.Max(1, Mathf.FloorToInt(PoolW / (m.CellW + PoolSpacing)));
                // 🔴 **2026-10-18（A870）**：行数也走**原版那条式子**（同上 doc ①②③ —— 覆盖 1.5 个视口高、
                //    ⛔ 不是「ceil(视口高 ÷ 格高)」，那会少一行、滚到半行时池底露出空白），并受池格下限约束。
                m.Rows = PoolRowsFor(PoolH, m.CellH, m.Cols);
                m.PadX = (PoolW - m.Cols * m.CellW) * 0.5f;
                m.CounterH = PoolCounterH0 * k;
                m.CardH = m.CellH - m.CounterH;
                // 🔴 **2026-10-18（`W11`）就地复核（铁律 5，无需改动）**：卡心 `cellTop + CardH/2`、缩放
                //   `CardH ÷ (CardView.Height × 108)` ⇒ **卡底正好落在张数条上沿**（桌面 501.73 = 条顶 501.73，
                //   条的可见顶 503.0、卡框可见底 494.75）—— 与原版实拍**同一条间距**（参考图：卡框底 510.1 /
                //   条顶 511.3 ⇒ 1.2px；我们 1.3px）⇒ **本窗卡池没有「张数与卡重叠」**。
                //   同族的**收藏窗 Cards 页**当时漏了这条（按整格 384 反解）⇒ 已在
                //   `Shell/CollectionWindow.CardsCardRect` 收口成同一条式子；⛔ 别只改一处。
                // 卡缩到多大 —— 按**卡位高减去底下张数条**反解（我们的卡 `CardView.Height × 108 = 359.7px`
                //   是**卡图**不是卡位）。⚠️ 原版卡位的宽高比与我们的卡不是同一个 ⇒ 取「**高度对齐**」。
                //   🔴 原来按 **384** 反解（卡顶满格子）—— 2026-09-28 订正，理由见 `PoolCounterH0` 那段。
                m.CardScale = m.CardH / (CardView.Height * PxPerUnit);
                m.BarX1 = PoolBarX10 * k; m.BarX2 = PoolBarX20 * k;
                m.CntY1 = PoolCntY10 * k; m.CntY2 = PoolCntY20 * k;
                return m;
            }
        }

        /// <summary>🆕 **2026-10-18（A870）**：一屏铺几行 —— 原版 `VerticalRecyclingSystem.CreateCellPool`
        /// 那**两条阈值**合成的（⛔ 不是常量，见 `PoolMinCoverage` 那段 doc 的逐字出处）。
        /// <list type="bullet">
        /// <item>**覆盖**：`ceil(viewportH × MinPoolCoverage ÷ cellH)` —— 原版 `CreateCellPool.c:193` 的循环条件
        ///   （`fVar22 &lt; fVar20 * fVar1`），网格档里 `fVar22` **每换一行加一次格高**（`:256-258` + `:271` 的 `goto`）。</item>
        /// <item>**下限**：`ceil(MinPoolSize ÷ cols)` —— 同文件 `:186-192` 那个**格下标**阈值换成行。</item>
        /// </list>
        /// <para>⚠️ `cellH` 传的是**格高**：原版累加的是那一格自己的 `rect.height`（`:258`，格被
        /// `set_sizeDelta(cell, _cellWidth, _cellHeight)` 定成 `_cellHeight`）—— **不含 `_spacingY`**。
        /// 本界面 `_spacingY = 0` ⇒ 格高与行距同值；`_spacingY` 一旦不是 0，仍要照原版用**格高**。</para></summary>
        static int PoolRowsFor(float viewportH, float cellH, int cols)
        {
            float byCoverage = viewportH * PoolMinCoverage / Mathf.Max(1e-4f, cellH);
            float byMinCells = PoolMinSize / (float)Mathf.Max(1, cols);
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(byCoverage, byMinCells)));
        }

        /// <summary>🆕 **2026-10-18（A870）自检口**：**给定视口高**算出来的行数 —— 与 `Pool.Rows` **同一条式子**
        /// （`PoolRowsFor` 就是它本体，这里只是把视口高/格高/列数当参数传进来）。
        /// ⛔ 期望值**不许**从这里读回去，判据是原版那两条阈值（`MinPoolCoverage 1.5` · `MinPoolSize 20`）。</summary>
        public static int UiPoolRowsForViewport(float viewportH, float cellH, int cols)
        {
            return PoolRowsFor(viewportH, cellH, cols);
        }

        /// <summary>自检口：**现在这一档**的卡池列数 / 行数 / 卡位尺寸（自检断「两档各是各的」用）。
        /// ⛔ 期望值不许从这里读回来 —— 判据是原版那四个字面量（6/262.5 · 4/393.75）
        /// 与那两条阈值（`1.5` 倍视口高 · 下限 20 格）。</summary>
        public int UiPoolColsNow { get { return Pool.Cols; } }
        /// <summary>🆕 A870：现在这一档的**行数**（桌面 4 / 小屏 5 —— 见 `PoolMinCoverage` 那段 doc）。
        /// ⛔ 别再写成 3。</summary>
        public int UiPoolRowsNow { get { return Pool.Rows; } }
        public float UiPoolCellWNow { get { return Pool.CellW; } }
        public float UiPoolCellHNow { get { return Pool.CellH; } }
        /// <summary>自检口：把「小屏 UI」开关拨一下并立刻重排卡池（**不落盘**，见 `SmallScreenUI.PersistOverride`）。
        /// ⛔ 生产路径不碰它。</summary>
        public void UiSetSmallScreenUIForTest(bool on)
        {
            bool keep = SmallScreenUI.PersistOverride;
            SmallScreenUI.PersistOverride = true;
            SmallScreenUI.Set(on);
            SmallScreenUI.PersistOverride = keep;
            RefreshPool();
        }

        // ---- Card Filters 筛选栏（**在左**，R:174-217）----
        const float FltX = 2.2f, FltY = 156f, FltW = 331.7f, FltH = 924.1f;
        /// <summary>🆕 2026-10-04（§三第29条 **A67**）：抽屉收起时**左移多少 px**（= 原版那段位移的**行程**）。
        /// 🔴 **不是 −550**：原版 `CollectionFilterController&lt;T>`（本窗那份就挂在 `Card Filters` /
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
        /// <summary>本窗**最低**的一档（= `QSep`）—— 🆕 2026-10-17 公开给自检：
        /// 顶栏那一条带子（`MainMenuRuntime.QBar*`，**`2986–2998`**）必须**严格低于**它
        /// （⚠️ **2026-10-18 订正**：这里原写 `2994–2998` —— 2026-10-17 F5 把下沿从 `2994` 放宽到 `2986`，
        /// **唯一出处 = `Shell/TopBar.cs` 那张表**；方向没变），
        /// 否则「窗口内容盖住顶栏」这条原版口径就反了（判据 → `MainMenuRuntime` 那一段长注释）。</summary>
        public const int QLowest = QSep;
        /// <summary>🆕 **2026-10-17（D15）**：`Content Area/Background` 那层**双色渐变底**。
        /// 它是全窗**最底下**的一层（比分隔线还低、比侧栏还低）⇒ 单开一个号，别蹭 `QSep`。
        /// ⚠️ 它 1752.83×1009.06、与侧栏**真重叠**（侧栏 x ≤ 335.56、它从 167.18 起）——
        /// 原版靠兄弟序（`Background` 是 `Content Area` 的第一个孩子）⇒ 侧栏压在它上面。</summary>
        const int QAreaBg = 2980;
        const int QSide = 3000;      // 侧栏底板
        const int QDoneHl = 3001;    // Done 的外发光（原版它纵跨到卡组列表区，单独一层）
        /// <summary>🆕 **2026-10-17（D31）**：费用曲线抽屉的**底板**（原版 `Deck Information cost drawer/Background`）。
        /// 它只该压在**侧栏底板**（`QSide`）之上、又必须在**槽**（`QPanel`）**之下** ——
        /// 它与那 9 根槽 / 那两列字**真重叠 234×294.6 px** ⇒ ⛔ 不能与 `QPanel` 同层
        /// （同层既会撞上「同一层不许压住」那条断言、谁盖谁也不定）。
        /// 取 `QSide + 1`（= **3001**，与 `_cosmDrawerBack` 那条**同一个写法**）：本件 y 411..705.6、
        /// `QDoneHl` 那颗 `foot_hl` y 933..1157 ⇒ **纵向不重叠**；与美容品那一页签也互斥 ⇒ 安全。</summary>
        const int QCurveBg = QSide + 1;   // 3001
        // 🔴 **2026-09-23 重排**（第 12 条 第 5 项）：原版卡组行的兄弟序是
        //    **`Background` → `Rarity Gradient` → `Border`**（行底最下）；我们原来是
        //    `QGrad(色条) < QPanel(行底) < QBorder` ⇒ **行底盖住色条**，与原来反了。
        //    ⇒ 给行底单开一层（`QRowBg`），其余各层顺移 +1（相对次序一律不变）。
        const int QRowBg = 3002;     // 卡组行的**行底**（原版 `40k_deck_cardlist_bg`，九宫格）
        const int QGrad = 3003;      // 行的稀有度色条 / 空卡组提示（**压在行底之上**）
        const int QPanel = 3004;     // 输入框底 / 曲线槽 / 计数器底（⚠️ 分隔线 2026-10-04 起搬去 `QSep`，不再用这一档）
        const int QBorder = 3005;    // 行描边 / 小图标
        // 🔴 **2026-10-17（D9 删件）**：`QPoolInfo = 3006`（卡池读数）那一档**已删** —— 用它的那两件
        //   （卡池左上那块黄色水印）**原版没有**。⛔ **别回收这个号给别的件用**：队列号在本文件是「谁压谁」
        //   的合同（同一层不许压住），空号留着比复用好（复用会让「位置无关的两件」突然同层）。

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
        // 🔴 **2026-10-17（T23 删件）**：这里原来还有 `QModal / QModalRow / QModalText`（3100..3102）——
        //   那是**导入弹窗那一棵树**的队列档，随那棵树一起删了（为什么删见 `BuildImportPopup` 那一节）。

        // ============================================================ 状态
        public DeckEditorState State { get; private set; }
        public DeckLibrary Library { get; private set; }
        public Transform Root { get; private set; }

        /// <summary>🆕 **2026-10-12（A363）**：编辑器**改脏了没有** —— 原版 `CardDeck.syncedToServer`
        /// （字节 `+0x60`）那一格在编辑窗里的对应物。
        ///
        /// <para>原版那条链（判据逐条在 `d:/2/tools/decomp_full/`）：
        /// · **每个突变只标脏**：`DeckEditingPanel__RemoveCard.c:13`（`deck[+0x60] = 0` 写在最前面）、
        ///   `DeckEditingPanel__ChangeName.c:9`（写完名字就标脏）、`DeckEditingPanel__Drop.c:26`（换卡背）。
        /// · **只有 `Done` 才上传**：`DeckEditingWindow__TrySaveDeck.c:80` 校验 → 合法才
        ///   `:100 DeckEditingWindow__UploadDeck`；而 `__UploadDeck.c` 里才是「把编辑中的副本
        ///   `CardDeck__CopyDeck` 进库里那份 → 标脏 → `PlayerDataManager__UploadUnsyncedDeck`」。
        ///   ⛔ 突变那几处**一处都没有**碰过库里的那份。</para>
        ///
        /// <para>🔴 **改向记录**：我们原来 `CommitDeck()` 挂在**全部 5 个突变点**上 ⇒ **改一下就落盘**
        /// （原版是「标脏 + Done 才上传」）。现在这 5 处收成 <see cref="MarkDeckDirty"/>，
        /// **真落盘只剩 `SaveAndSay()`（= `Done` 那一拍；`ESC` 走的是同一个函数）一处**
        /// （🔴 **2026-10-12（A399）订正**：这里原来写「`hdr_back` 与 ESC 走的是同一个函数」——
        /// **关闭钮已经改走 `TryClose()` 了**，那条链**从不保存**，见 <see cref="TryClose"/>）。</para>
        /// </summary>
        public bool DeckDirty { get; private set; }

        Camera _cam;
        int _tab;                       // 0 = Cards · 1 = Deck info · 2 = Cosmetics
        bool _filtersOpen;
        float _poolScroll, _deckScroll, _fltScroll;
        /// <summary>🆕 **2026-10-18（A889）**：**卡背抽屉自己**那段滚动量 —— 与 <see cref="_fltScroll"/>
        /// （**卡牌**抽屉的）**是两个量**，⛔ 别再让卡背那一族去借 `_fltScroll`（那是别人的滚动位）。
        /// <para>**今天恒 0**：抽屉内容 **707.8** &lt; 可见 **924.1**，而且**原版那棵树里也没有 Scroll View**
        /// （`HandleScroll` 的卡背那一支只把滚轮吃掉）—— 所以**没有写点**（下面那个 `= 0f` 初值就是它唯一被
        /// 赋值的地方；读点只有 `FltAbsCosmo` 一处）。
        /// 留着它是**形状**：将来原版/我们真要给卡背抽屉加滚动时，只要在这一处加写点（照 `UiScrollFilters`
        /// 那一支的夹法），三处摆位**一个字都不用改**。理由与对照见 `FltAbsCosmo` 那段。</para></summary>
        float _cosmoFltScroll = 0f;
        /// <summary>正在编辑的文本缓冲（`null` = 没在编辑）。<see cref="_editKind"/> 说明它在改什么。</summary>
        string _nameEdit;
        /// <summary>0 = 没在编辑 · 1 = 改**卡组名**（原版 `DeckEditingPanel.ChangeName`）· 2 = 改**卡名筛选**（原版 `CardNameFilter`）。</summary>
        int _editKind;

        // 🔴 **2026-10-17（T23 删件）：导入卡组弹窗那一棵树 + `_importOpen/_importText/_importError` 已删。**
        //   为什么删：它**没有生产入口**（侧栏那两颗 `info_*` 圆钮 2026-10-17 已按 D35 删掉，本窗这一份
        //   只剩自检口 `UiOpenImport()`），而**真正在用的那份**是外壳的 `Shell/ImportDeckPopup.cs`
        //   （`CollectionWindow.OpenImportPopup()` 在调，`CollectionScene` / `ShellScene` 都有断言）。
        //   ⇒ 留着 = 同一扇窗两套实现（`CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」）。
        //   ⛔ **别再加回来**：原版这个动作的家在**选卡组页**（`SelectDecksTab.ImportDeck` → `ImportDeckPopup`）。
        readonly HashSet<string> _missingArt = new HashSet<string>();

        readonly List<CardView> _poolViews = new List<CardView>();
        readonly List<int> _poolIndex = new List<int>();
        /// <summary>🆕 **2026-10-17（D24）**：上一趟用的**卡位宽**（判「这两档之间换过没有」——
        /// 换了就要把「张数条」那批拆掉重建，见 `RefreshPool` 里那一段）。
        /// 出厂 **0** ⇒ 第一趟必走一次（此时列表本来就是空的，无副作用）。</summary>
        float _poolCellWUsed;
        /// <summary>🆕 **2026-10-17（D13/D14）**：整条 shell 顶栏那几件。`null` = 还没建
        /// （⚠️ `TopBar` 取不到头像图时 `TopAvatar` 可以是 `null`，那**不是**「没建」）。</summary>
        TopBar.Parts _topBar;

        // 行底 / 行描边是**九宫格**（`ImageQuad.CreateNineSlice` 建出来的是**一棵小树**）⇒ 存**根节点**
        readonly List<GameObject> _deckRowBg = new List<GameObject>();
        readonly List<ImageQuad> _deckRowGrad = new List<ImageQuad>();
        readonly List<GameObject> _deckRowBorder = new List<GameObject>();
        /// <summary>行右缘那颗**份数**（原版 TMP 名 `Count`）—— 🔴 **2026-10-20 新加**：
        /// 原版行里**只有** `Background / Rarity Gradient / Background Border / Cost Image /
        /// banned Icon / Text fill(Card Name, Count)` 这几件，**没有**我们原来那颗
        /// `row_k*`（拿 `40K_main_deck_card_counter` 当底的那块纯色方块 —— 原版行里根本没这个节点，
        /// 实拍上它是块**空方块**）。⇒ 删 `row_k*`、换成这颗 TMP（节点名 `row_x{i}`）。
        /// <para>🔴 **2026-10-17（`A2007` · 交接_第三十一会话 §六·四·一 `N2`）纯改名**：原名
        /// `_deckRowCountX`（后缀 `X` = 那个 `x{n}` 文本），**码本身没错、只是名字读不出语义** ⇒
        /// 改成 `_deckRowCountText`（= **份数** `Count` 那颗 TMP）。同一批：`_deckRowCount`
        /// → <see cref="_deckRowCostText"/>（= **费用** `Cost` 那颗）。⛔ 别按旧名找。</para></summary>
        readonly List<Label> _deckRowCountText = new List<Label>();
        readonly List<Label> _deckRowName = new List<Label>();
        /// <summary>圆里那个**费用**（原版 `…/Cost Image/Cost`）。🔴 **2026-10-20 之前喂的是「份数」**
        /// —— 份数搬去了 `_deckRowCountText`，这一颗改成 `CardDef.Cost`（见 `RefreshDeckList`）。
        /// <para>🔴 **2026-10-17（`A2007`）纯改名**：原名 `_deckRowCount`（名字像「份数」、实际盛**费用**）
        /// ⇒ 改成 `_deckRowCostText`。**代码一行都没动，只改名** —— 判据见上面那条。</para></summary>
        readonly List<Label> _deckRowCostText = new List<Label>();
        readonly List<ImageQuad> _deckRowCost = new List<ImageQuad>();

        // ============================================================ 🔴 2026-10-21（`A1727` + `A1720`）
        //
        // `A1727`：行要**裁到视口内**（`DeckListBand`）——12 槽 = 690.4 > 视口 644.1，顶/底那一行必越界。
        // `A1720`：**槽 0 建两套件**（督军档 / 卡行档，见 `RowBgRect` 那段），二选一开。

        /// <summary>一行里的一件**可裁的 quad** + 它**未裁切时的基准**（画布 px 矩形 + 整张 uv）。
        /// <para>⚠️ **基准必须在这一件【还没被裁过】的时候抓**（= `BuildDeckList` 里刚建完那一刻）；
        /// 抓晚了就是「上一刀裁剩的那块」，于是每刷新一次再裁一刀 —— **越裁越小、静默**
        /// （同 `MenuDraw.ApplySoftEdges` 的 `uv0` 那条坑）。</para></summary>
        sealed class RowQuad
        {
            public ImageQuad Q;
            /// <summary>建的时候那一份**绝对**矩形（画布 px）；每次刷新按行位移 `dy` 平移后使用。</summary>
            public PxRect R0;
            /// <summary>建的时候那一份**整张 uv**（`ImageQuad.UvRect`）。</summary>
            public Rect Uv0;
            /// <summary>它是哪一套 —— <see cref="PieceHero"/> / <see cref="PieceCard"/> / <see cref="PieceShared"/>。</summary>
            public int Kind;
        }
        /// <summary>槽 0 的**督军档**那一套（`row_0` / `row_g0` / `row_b0`）。</summary>
        const int PieceHero = 0;
        /// <summary>**卡行档**那一套（槽 0 = `row_0c` / `row_g0c` / `row_b0c`；槽 1+ = `row_i` / `row_gi` / `row_bi`）。</summary>
        const int PieceCard = 1;
        /// <summary>与档无关的那几件（费用圆 `row_c*`）。
        /// 🔴 **2026-10-14（红 6）：注册进 `_rowPieces` 的件，可见性就【归 `ClipRowToBand` 管】**
        /// —— 它的 ① 会 `SetActive(true)`（那是给「**上一轮被裁出去**」的件复位用的）。
        /// ⇒ 凡是「**状态**要关掉它」的件（今天只有费用圆：督军行不显示），调用点必须把
        /// 「这一行这一颗该不该显示」当 `ClipRowToBand` 的 `shared` 实参传进去，
        /// ⛔ 否则状态关掉的那一颗会被**静默重新点亮**（本档 2026-10-14 踩过：督军行的圆）。</summary>
        const int PieceShared = 2;

        /// <summary>按槽号登记的「这一槽里所有要裁的 quad」（含槽 0 那**两套**）。判据见 `RowQuad` / `ClipRowToBand`。</summary>
        readonly List<RowQuad>[] _rowPieces = new List<RowQuad>[RowVisible];
        /// <summary>🔴 **2026-10-21（`A1720`）：槽 0 的【卡行档】那三件**（原版 `Deck Selector Card Info button`
        /// 那一族的几何）—— 槽 0 装的是卡行时用它（`firstRow ≥ 1`），装督军时用 `_deckRowBg[0]` 那一套。
        /// ⚠️ 为什么是**另建一套**而不是原地改尺寸：九宫格**搬得动、改不了尺寸**
        /// （`MenuDraw.Nine` / `ImageQuad.CreateNineSlice` 没有 resize 口，`Move()` 只写 `localPosition`）。</summary>
        GameObject _row0cBg, _row0cBorder;
        ImageQuad _row0cGrad;

        readonly List<Label> _tabLabel = new List<Label>();
        /// <summary>页签高亮那一层 —— 🔴 **2026-10-04（A41 ⑥）起它是【九宫格】**（原版
        /// `Image.Type = Sliced` + `m_Border (30,30,30,30)` + `m_PixelsPerUnitMultiplier = 0.92`），
        /// 一棵树里 **9 块** ⇒ 开关/上色一律走 `_tabHiRoot[i]` 那棵**根**，`_tabHi[i]` 只是
        /// **树里第一块**（自检读数用；只给一块上色会**留下另外 8 块**，静默）。</summary>
        readonly List<GameObject> _tabHiRoot = new List<GameObject>();
        readonly List<ImageQuad> _tabHi = new List<ImageQuad>();
        readonly List<ImageQuad> _tabIcon = new List<ImageQuad>();

        readonly List<ImageQuad> _curveFill = new List<ImageQuad>();
        /// <summary>🆕 **2026-10-11（A1497 ③）**：每根填充柱**建的时候**的 `localPosition.x` ——
        /// 它是「宽度还是 1px 时」的中心（= 槽左 + 0.5）。`SetQuadWidth` 只改宽度、**围绕锚点(0.5)往两边长**
        /// ⇒ 每帧要按它把中心摆回「槽左 + 宽/2」，否则柱子会**向左跑出槽外**（实拍里白药丸从 x≈0 起）。</summary>
        readonly List<float> _curveFillX0 = new List<float>();
        readonly List<Label> _curveLabel = new List<Label>();
        readonly List<ImageQuad> _curveBar = new List<ImageQuad>();
        readonly List<GameObject> _infoOnly = new List<GameObject>();   // 只在 Deck info 页签显示的东西

        /// <summary>🆕 **`A1514`**：卡组类型计数抽屉那两颗 TMP（原版 `DeckTypeCounterDrawer` 的
        /// `troopCount` / `stratagemsCount`）。判据与几何 → 类级 `Tc*` 常量那一段；两件都进 `_infoOnly`。</summary>
        Label _tcTroops, _tcStratagems;

        /// <summary>🔴 **2026-10-11（`A1497④`）**：**Cards 页 + Deck info 页才显示**的那批件
        /// （= 原版 `DeckEditingWindow.wildcards` 那一整条 `WIldcard Counter`：底板 + 4 图标 + 4 数字 + 阵营徽记）。
        /// 判据/为什么 → `CardDisplayPage` 那个属性。显隐落在 `RefreshTabVisibility`（与 `_infoOnly` 同处）。
        /// ⚠️ **卡池不在这张表里** —— 它是**按格**建的，显隐在 `RefreshPool` 里按**同一个** `CardDisplayPage` 判。</summary>
        readonly List<GameObject> _cardsInfoOnly = new List<GameObject>();

        /// <summary>可点的矩形（px 坐标）。命中判定一律走它 —— 比拿 quad 反推尺寸稳。</summary>
        struct Btn { public string Key; public float X, Y, W, H; }
        readonly List<Btn> _btns = new List<Btn>();

        // 🔴 **2026-10-17（D7/D9/D10/D35 删件）**：原来这一行上还挂着 4 个**原版没有的**件
        //   —— `_title`（左栏顶部标题「卡组编辑」）· `_poolInfo`（卡池左上黄色水印）·
        //     `_storeErr`（底部报错横幅）· `_verdict`（页脚「合法/不合法」判词）。
        //   四条各自的判据 → `资料/普查产出_1017/对账_卡组编辑部分_差异与待办.md` D7/D9/D10/D35。
        //   ⛔ 别「顺手加回来」：它们是自检脚手架长进产品里的那一类。
        Label _deckNameText, _deckNameHint, _counterTxt;
        readonly Label[] _wcTxt = new Label[4];
        ImageQuad _emptyWarn, _doneHl;
        /// <summary>页头那颗 `Filters` 圆钮 —— 它要**按状态换图**（面板开=`40k_menu_bt_pressed`、关=`40k_menu_bt`），
        /// 见 `RefreshHeader` 里那段判据（原版 `EverguildToggle.changeSpriteOnValueChange=1`）。</summary>
        ImageQuad _hdrFltBtn;
        /// <summary>页头那颗 `Clear filters` 的**图**与**字**（两颗一起显隐 —— 判据与「为什么走 `SetActive`
        /// 而不是原版的 −550 平移」写在 `HdrClearX` 上面那一大段注释里）。</summary>
        ImageQuad _hdrClear;
        Label _hdrClearTx;
        GameObject _emptyWarnGo;
        /// <summary>🆕 **2026-10-17（D35 删件）**：最近一次 <see cref="Say"/> 吐出来的那句话。
        /// 🔴 **原来它还印在屏幕底部（`notice` 那行字）—— 原版侧栏没有那一行**（D35）⇒ 那行**已删**，
        /// 但**这条消息通道留着**（`Say` 仍然 `Debug.Log`，这份 state 供自检/日志读）：
        /// 「不许静默失败」要求的是**说得出话**，不是「多画一行字」。
        /// ⚠️ `_noticeUntil` 那套计时也一起删了（它只服务于那行字的自动消失）。</summary>
        string _noticeText = "";

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
        /// <summary>🆕 **2026-10-17（`A1738` 收尾）**：卸载时把静态口清掉。
        /// <para>原来全类**没有 `OnDestroy`** ⇒ 卸载后 `Instance` 指向一个已销毁对象 =
        /// **Unity 的「假 null」**（`Instance == null` 会判真，但字段本身不是 null；谁拿它当「有没有实例」用就会踩）。
        /// 今天 `grep DeckRuntime.Instance` = 0 读者 ⇒ 无害，但这是个**静默陷阱**，顺手堵掉。
        /// 判据 = 第二十四会话 `D9` 顺手发现（`待办判据_第二十五会话.md` §`A1738`）。</para></summary>
        void OnDestroy() { UnhookWildcardUpdate(); if (Instance == this) Instance = null; }   // B9_W63WC：`Inventories.Wildcards` 是**静态**的 ⇒ 不退订就被按着（同 `CollectionWindow.Close`）

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
            HandleEscape();          // 🆕 2026-10-11（A223）：ESC = 保存（原版 `DeckEditingWindow__ESCPressed`）
            TickTooltip();
            // 🆕 **2026-10-17（D13/D14）**：顶栏那块立绘跟着 `ProfileData.AvatarIndex` 走
            //   （原版走 `PlayerAvatarDataManager.OnAvatarChanged`；我们**每帧比一个 int** 就够 —— 同 `MainMenuRuntime`）。
            //   ⚠️ 批处理下 `Update` 不跑 ⇒ 自检直接调 `UiRefreshTopAvatar()`。
            // 🆕 **2026-10-17（B25①）**：记下「这一帧结束时别处有没有窗开着」—— 给下一帧的 ESC 闸用
            //   （`PointerLayer` 的 `Update` 排在本类**之前**，按 ESC 那一帧它已经把窗关掉了；
            //    判据与推导 → `OtherWindowUp` 上面那一段）。
            _winShieldPrev = OtherWindowUp() != null;

            TopBar.RefreshTopAvatarIfChanged(_topBar, NoteMissingArt);
            // 🔴 **2026-10-17（D35 删件）**：这里原来有一段「`notice` 那行字 2.5 秒后自动消失」
            //   —— 那行字**原版没有**、已删（判据 → D35）⇒ 计时/清空/`Update` 窗口一并删掉。
            //   ⛔ 别把 `Say` 也一起删了：它是本窗「不许静默失败」的出口（仍然 `Debug.Log`）。
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

        /// <summary>自检入口：批处理没有鼠标 ⇒ 直接喂世界坐标（**和鼠标那条路同一个函数**）。
        /// <para>🔴 **2026-10-21（`A1740`）**：原来这里是 `for (i &lt; _poolViews.Count)` 逐个问
        /// —— 而本函数**每帧都跑**（`Update` → `TickTooltip` → 这里，`Update` 里**没有**任何闸）
        /// ⇒ 每帧 24 次判定。现在先 `ToPx` 反解「指针在第几格」，**只问那一格**
        /// （等价性三条证明 → <see cref="PoolCellAt"/> 的 doc；`ToPx` 是 `Pos` 的严格逆，
        /// 与 `HandlePoolClick` 走的是同一个设计 px 帧）。</para></summary>
        public bool TickTooltipAt(Vector3 wp)
        {
            int vi;
            if (PoolCellAt(ToPx(wp), out vi) && vi >= 0 && vi < _poolViews.Count)
            {
                var v = _poolViews[vi];
                if (v != null && v.gameObject.activeSelf)
                {
                    int s = v.StatAt(wp);
                    // ⚠️ 护甲那颗原版就没有 tooltip（见上面那段）⇒ 与 `StatNone` 同一条支
                    if (s != CardView.StatNone && s != CardView.StatArmour)
                    {
                        Vector3 at = v.StatWorld(s);
                        switch (s)
                        {
                            case CardView.StatMelee:  Tooltip.Show(TipText.Melee,  at, 15, new Vector3(-49.33f / 108f, 0f, 0f)); return true;
                            case CardView.StatRanged: Tooltip.Show(TipText.Ranged, at, 15, new Vector3(-53.54f / 108f, 0f, 0f)); return true;
                            case CardView.StatHealth: Tooltip.Show(TipText.Health, at, 10, new Vector3( 73.05f / 108f, 0f, 0f)); return true;
                            case CardView.StatCost:   Tooltip.Show(TipText.Cost,   at, 10, new Vector3( 52.60f / 108f, 0f, 0f)); return true;
                        }
                    }
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
            // 🆕 2026-10-17：拖拽那两份「已经按下了」的登记也要清（同上一行那两条的理由：
            //    不清的话重建之后会带着上一趟的起点去命中一张**已经销毁的**内容）
            _cosmArmName = null; _cardArmDef = null;

            // 🔴 **2026-10-22（`W62`）删掉「空库时替玩家建一套并当场落盘」** —— 原来这里是：
            //   `Library.Create(Loc.T("MenuDeck/DefaultDeckName"))` + `Library.CommitCurrent(PlayerDeckForDemo(NewState()))`
            //   ⇒ **一打开卡组编辑器就把一副演示卡组（「复仇者之刃」）静默写进玩家存档**、
            //   连默认名一起覆盖（`DeckLibrary.CommitCurrent` 会 `dst.Name = deck.Name`）。
            //   **原版没有这个副作用**：`SelectDecksTab__CreateDeck.c:17,26,28` = `CardDeck__GetEmptyDeck()`
            //   → 打上模式 → **直接 `WindowsManager.OpenWindow<DeckEditingWindow>(…)`**，全链没有一个入库调用；
            //   入库的唯一路径 = `DeckEditingWindow__TrySaveDeck.c:100 UploadDeck`（先 `:80 ValidateDeck`，
            //   不合法就弹错误窗 `return`，**绝不走到 `:100`**；`:74 UploadUnsyncedDeck` 才真写）。
            //   ⇒ 现在改走同一条「**只造、不入库、不落盘**」的路（见下面 `_newDeck` 那一段）。

            // 🔴 **「从收藏进编辑」的交接**（2026-09-23）：收藏窗点「编辑」时把下标写进
            //    `CollectionData.PendingEditDeck`，这里**开局就读掉并清掉** ⇒ 编辑器直接打开那一套。
            //    （原版在同一扇窗里换页、不需要交接；我们是两个场景 —— 见 `资料/阶段二_卡组线_原版规格.md` §七。）
            //
            // 🔵 **2026-10-12（`A1697`）多了一条交接口（`PendingNewDeck`）**：**新建卡组不进库**
            //    （照原版 `SelectDecksTab__CreateDeck.c:17,26,28` ⇒ 库里**没有下标**）⇒ 那条路交接的是
            //    **一份还没入库的卡组对象**。两条口**互斥**（写新那条的一拍会把旧下标清成 −1）。
            //    ⚠️ **先读新的那条**：`_newDeck` 本身就是「这一扇窗编辑的是一份**尚未入库**的东西」这一状态
            //    —— 下面 `CommitDeck()` 靠它决定走「**入库**」还是「写回当前选中那一套」。
            var freshDeck = CollectionData.TakePendingNewDeck();
            _newDeck = freshDeck;
            if (freshDeck != null)
            {
                Debug.Log("[Deck] 新建卡组进来 ⇒ 打开**尚未入库**的那一副「" + freshDeck.Name + "」"
                          + "（模式 " + freshDeck.GameMode + "）—— 照原版：`Done` 过了校验才入库");
                CollectionData.PendingEditDeck = -1;   // ⛔ 防呆：两个交接口不许同时生效
            }
            else if (CollectionData.PendingEditDeck >= 0)
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

            // 🔴 **2026-10-22（`W62`）空库 = 照原版「新建卡组」那条路开窗**（⛔ **不写玩家存档**）。
            // 判据（逐行读过，见 `Shell/CollectionData.CreateDeck` 那一段的同一组出处）：
            //   `SelectDecksTab__CreateDeck.c:17` `CardDeck__GetEmptyDeck()`（= 一副**不在库里**的空卡组；
            //   `CardDeck__GetEmptyDeck.c:25-27` 把名称/战将/防御都置空、`:28-31` 清 `cardLibraryIds`）
            //   → `:26` 打模式 → `:28` `WindowsManager__OpenWindow<DeckEditingWindow>(…)` —— **全链没有一个入库调用**；
            //   原版入库的唯一路径 = `DeckEditingWindow__TrySaveDeck.c:100 UploadDeck`（先 `:80 ValidateDeck`，
            //   不合法就 `:81-98` 弹错误窗 `return`，**绝不走到 `:100`**）→ `DeckEditingWindow__UploadDeck.c:63`
            //   `CardDeck.CopyDeck` + `:74 PlayerDataManager.UploadUnsyncedDeck`。
            //
            // ⚠️ **演示卡组只当内存里的临时载体**（改动前它是**当场落盘**的 —— 那是本次修掉的真缺陷）：
            //   编辑器**总得有一副东西可编辑**，而库里一套都没有时，我们拿 `PlayerDeckForDemo` 填一副
            //   **尚未入库**的 `_newDeck`（与上面 `PendingNewDeck` 那一路**同一个状态**）
            //   ⇒ 只有玩家按 `Done` 且过了校验，它才 `Library.Insert`（= 原版 `UploadUnsyncedDeck`）落盘。
            //
            // ⚠️ **如实标注（偏离）**：原版 `GetEmptyDeck` 是一副**空卡组**（没有督军、没有牌、名字为空），
            //   我们这里给的是一副**合法满编的演示牌**（名字「复仇者之刃」）。
            // 🔶 自己设计（非原版）：库空时用演示牌填一副未入库的 `_newDeck` · 原版 `GetEmptyDeck` 是空卡组；我们这么做只为编辑器一打开不是空白（原版判据拿不到） · TODO(谁定的)｜原注：这是**我们多做的**、不是原版行为 —— 保留它只为「编辑器一打开不是一片空白」。
            //   ⛔ 别因此把「演示牌」写进库或存档（那正是本次删掉的那段）。
            if (_newDeck == null && Library.Count == 0)
            {
                _newDeck = PlayerDeckForDemo(NewState());
                CollectionData.PendingEditDeck = -1;   // ⛔ 防呆：两个交接口不许同时生效
                Debug.Log("[Deck] 库里一套卡组都没有 ⇒ 照原版 `SelectDecksTab.CreateDeck` 开一副**尚未入库**的卡组"
                          + "（演示牌只当内存载体：**不写存档**，`Done` 过了校验才 `Insert` 落盘）"
                          + " —— 原版 `GetEmptyDeck` 给的是空卡组，我们这里给的是演示满编（明账偏离）");
            }

            State = NewState();
            // 🔴 **A397（2026-10-13）**：`LoadDeck` 装的是**副本**（原版 `DeckEditingWindow__TryOpen.c:41-45`
            //    的 `new CardDeck` + 拷贝构造）⇒ 从这里往下，编辑器改的**都不是** `Library.Current` 那个对象；
            //    库里那份只在 `CommitDeck()`（= `Done`/`ESC`）那一拍被整份拷回去。
            // 🔴 **A1697**：新建那一路装的是 `PendingNewDeck`（还没入库的那一副）—— 同一个「装副本」语义。
            // 🔴 **2026-10-22（`W62`）**：空库那一路也走 `_newDeck`（`GetEmptyDeck` 那个位置）——
            //    ⛔ 不要再退回 `Library.Current`（空库时它是 null）。
            State.LoadDeck(_newDeck != null ? _newDeck : Library.Current);

            BuildAreaBackground();
            BuildTopBar();
            BuildHeader();
            BuildSidebar();
            BuildDeckList();
            BuildCurve();
            BuildTypeCounter();      // 🆕 A1514：原版 `Deck Counter Drawer`（同为 Deck info 页签那一批）
            // 🔴 **2026-10-17（D35）**：原来这里还有一句 `BuildInfoActions();` —— **已删**
            //   （分享 / 导入那两颗圆钮是「我们多画的」，见那一节的注释与 D35）。
            BuildFooter();
            BuildFilters();
            BuildPool();
            BuildCosmeticsPage();
            // 🆕 2026-10-17：**两条拖拽链**（原版两个 `DraggableController<T>` 实例）——
            //   卡背那一支挂在 `Cosmetic Display` 那一页上、卡牌那一支跟 `Card Display` 同级
            // 🔴 **2026-10-11 就地订正（铁律 5 · 代理 `E` 现读）**：原来这里写「原版 `Card Drag Controller` 是 `Card Display` 的**兄弟**」—— **错，它是【孩子】**。
            //   判据：`bundle_menus_assets_all/GameObject/Card Drag Controller.json` 的 RT `-3300583987145543900` 的 `m_Father` = `5543959332` → GO `Card Display`；`menu_dump` 缩进也是 `2 Card Display` → `3 Card Drag Controller`（本包同名只有 1 个实例）。⚠️ 行为等价（别的页起不了拖），但注释要按事实写。
            BuildCosmeticDrag();
            BuildCardDrag();
            // 🔴 **2026-10-17（T23 删件）**：原来这里还有一句 `BuildImportPopup();` —— **已删**
            //   （那棵树没有生产入口、只是外壳 `Shell/ImportDeckPopup.cs` 的重复实现，见那一节的说明）。
            // 🔴 **2026-10-17（D7/D10/D35）**：原来这里还有一句 `BuildNotice();` —— **已删**
            //   （它建的三件原版都没有，见下面那段注释）。

            if (_missingArt.Count > 0)
                Debug.LogWarning("[DeckRuntime] 缺 " + _missingArt.Count + " 张原版 UI 图（会画成纯白占位，"
                                 + "**不是静默**）：" + string.Join("、", _missingArt));

            RefreshAll();

            // 🔴 2026-10-04（A24）：**这里【故意】不建 `PointerLayer`** —— 悬停的派发由本类自己的
            //   `HandlePointer`（下面 `UpdateButtonHover`）做，理由有三条，写在这里免得下一个人「顺手补上」：
            //   ① 本窗的指针语义是**有模态状态的**（`ModalPopupOpen` 模态 / `_filtersOpen` 左抽屉 / `_dragging`），
            //      而 `PointerLayer` 无状态：模态窗开着时它照样会让**窗背后**的按钮亮起来
            //      （我们没建原版那块全屏暗底，没有东西挡它）；
            //   ② `PointerLayer` 自己的「点击 / 滚轮 / 拖拽」会和本类那条**并行跑起来** ——
            //      点一下 `ClickLog` 会多写一块（`Shell/PointerLayer.cs` 里那句 `ClickLog.Begin`，正文还会说
            //      「这个命中区没有绑动作」，而那颗按钮的动作其实走 `_btns` 那条路）⇒ 给真机诊断添噪声；
            //   ③ `Shell/PointerLayer.cs` 里那条「收口不到卡组编辑那一套」 早就写明「**收口不到卡组编辑那一套**，两者语义相同、
            //      **各写一份（明账）**」（同「滚动也是两份」）⇒ 悬停这一份跟着这条既有口径走。
            //   代价（如实记）：本窗**没有** `PointerLayer` 那套「按下越过 10px 判成拖拽」的语义 ——
            //   但本类的拖拽是**行拖出删除**，判据本来就在 `EndDrag` 里，与按钮无关。
            //
            // 🔴 **2026-10-12（A364）就地订正（铁律 5）**：上面这段说的是「**本类自己不去建**指针层」，
            //   那个决定**没变**；但「**本窗没有 `PointerLayer`**」这个**事实**从 A364 起**不再成立**——
            //   模态消息窗那条链走 `WindowsManager.EnsureHost()`（原版那扇窗就是 `WindowsManager` 的窗），
            //   而 `EnsureHost` 会**连指针层一起建**（`Shell/WindowsManager.cs` 的 `EnsureHost` 第一句）⇒
            //   **那扇窗第一次弹出来之后，本场景里就有一台 `PointerLayer` 了，而且它不会消失**。
            //   · **点击不会重复**：本类从没给这些 `WindowButton` 设过 `onClick`（全文件 0 处）
            //     ⇒ `PointerLayer` 打到它们身上时 `Click()` 的 `onClick` 是空、**空转**；
            //     而弹窗开着时它按**渲染队列**取最高那件（`AllButtons` + 队列降序）⇒ 只会命中弹窗自己
            //     （`QShadeHit` 3561 / 按钮 3564，压暗层那颗 `absorbOnly` 还会早退）⇒ **模态那半边成立**。
            //   · **悬停可能被派发两次**：那几颗挂了 `WindowButton` 的件（换图 5 颗 + 色偏 3 颗）会同时收
            //     本类 `UpdateButtonHover` 与 `PointerLayer` 的进入/离开。`Enter()` 有 `Hovered` 守卫
            //     ⇒ 同向幂等；但两者的**命中判据不同**（本类是 `_btns` 的区域表、它是 quad 矩形）
            //     ⇒ 边界上**理论上可能来回抖**（视觉，不涉数据）。→ 已作为**顺手发现**记进报告，归调度台排。
            //   ⛔ 别据此「顺手补上」本类自己的指针层（上面三条理由仍然成立）。
        }

        DeckEditorState NewState()
        {
            var s = new DeckEditorState(CardDatabase.Load());
            // 🆕 2026-09-27：**只有卡组编辑这条路**开「按督军分流卡池」
            //   （没督军 ⇒ 只列各阵营的督军；定了 ⇒ 只列该阵营的卡）。规格与理由见 `WarlordGatedPool`。
            s.WarlordGatedPool = true;
            return s;
        }

        // ------------------------------------------------------------ Content Area 的底板（D15）

        /// <summary>🆕 **2026-10-17（D15）**：`Content Area/Background` —— 卡池那块**双色渐变底**。
        /// 我们原来是**纯黑**（背后什么都没有）；实拍里那一整片是**暗酒红渐变**。
        ///
        /// <para>🔴 **判据（逐字段实读 `bundle_menus_assets_all`，⛔ 不是「自己挑一张图」）**：
        /// · 节点 = `Deck Editing Menu/Content Area/**Background**`（`Content Area` 的**第一个孩子**，
        ///   **出厂 active**）· 矩形 = **167.18,70.97 → 1920.00,1080.03**（1752.83×1009.06）。
        /// · `Image`：**`m_Sprite = null`（没有图！）** · `m_Type = 1`(Sliced) · `m_Color = (1,1,1,1)`。
        ///   ⇒ 它就是一块**纯色板**，颜色全来自同节点上那颗 `UIGradient`。
        /// · `UIGradient`（`MB -3308698737451929820` 等，**本包 8 颗全是同一对色同一个角**）：
        ///   `m_color1 = (0.2235294, 0.0117647, 0.0196078, 1)` ·
        ///   `m_color2 = (0.0470588, 0, 0.0156863, 1)` · `m_angle = **82**`。
        ///   🔴 **这一对色与主菜单整屏底图那对【逐位相同】**（`MainMenuRuntime.BuildBackground` 用的就是
        ///   同一对 + 同一个角）—— 原来那句「原版用哪张底图没查」的答案就是：**没有底图，是渐变**。
        /// · **角度换算**（🔴 **2026-10-25 就地订正（铁律 5 · 判据来自代理 D 的现核）：理由原来写错了，数值 188 不变。**）
        ///   **错因**：原文写「原版方向是 `(sin,cos)`、我们助手是 `(cos,sin)` ⇒ 差 90°」——
        ///   那是把**两个不同的东西**当成同一个量比：
        ///     · `UIGradientUtils.RotationDir` 返回的**就是 `(cosθ, sinθ)`**
        ///       （`decomp_full/UIGradientUtils__RotationDir.c:9-11`）⇒ 与我们的 `(cosθ, sinθ)` **同形**；
        ///     · 真正决定屏幕上的渐变方向的，是 **`ModifyMesh` 里那条【有效轴】= `(sinθ, cosθ)`**
        ///       （`UIGradient__ModifyMesh.c:148-149` 取 `RotationDir` 的两个分量**交叉**写进局部位置矩阵；
        ///        `UIGradientUtils__LocalPositionMatrix.c:20-27`）—— 它才是「镜像」的来源。
        ///   ⇒ 换算 = **`φ = 90° − θ + 180°`**（**镜像（交换轴）+ 反色序**），
        ///     代 `θ = 82` 得 `φ = 188` ⇒ **数值 188 是对的**（这一处只改理由，⛔ 没动数值）。
        ///   ⚠️ 那 180° 那一半的理由不变：我们 `c1` 在 `t=0` 端、而原版亮色在**右上**。
        ///   （同 `MainMenuRuntime.BuildBackground` 那条已订正的结论，⛔ 别把它记成「原版写的就是 188」。）
        ///   实拍复核（`原版参照图/用户实拍_1017/卡组编辑界面参考.png` 按 1920 折算后采样）：
        ///   `(960,90) = (33,1,4)` 偏亮 · `(540,125) = (24,1,6)` 更暗 ⇒ **右上亮、左下暗**，与 188° 自洽。</para>
        ///
        /// <para>⚠️ **它盖不到的那两块是【原版如此】，不是我们漏了**（实拍同位置也是黑的）：
        /// 左带 `x 0..167.18 × y 71..156`（实拍 `(160,120) = (0,0,0)`）、以及顶栏那一横条
        /// （原版由主菜单的顶栏图盖住 —— 我们这边是 D13/D14 那件）。⛔ 别「顺手」把它铺成全屏。</para></summary>
        void BuildAreaBackground()
        {
            var tex = CardArt.Gradient(new Color(0.2235294f, 0.0117647f, 0.0196078f, 1f),
                                       new Color(0.0470588f, 0f, 0.0156863f, 1f), 188f,
                                       (1009.06f - 70.97f) / (1752.83f - 167.18f));   // 下面 Img 那个矩形的高/宽（原版 CompensateAspectRatio）
            Img("area_bg", tex, 167.18f, 70.97f, 1752.83f, 1009.06f, QAreaBg);
        }

        // ------------------------------------------------------------ 外壳顶栏（D13/D14）

        /// <summary>🆕 **2026-10-17（D13/D14）**：**整条 shell 顶栏** —— 左上（头像 + 玩家名 + 信封 + 人形图标）
        /// + 右上（三项资源 + 齿轮）。
        ///
        /// <para>🔴 **为什么这扇窗里要有它**：原版卡组编辑是**盖在主菜单之上的一扇窗**（`DeckEditingWindow`），
        /// 顶栏属于**下面那一层**，所以屏幕上一直看得见（实拍 → `原版参照图/用户实拍_1017/卡组编辑界面参考.png`）。
        /// **我们是独立场景 `DeckEditor.unity`**（`Shell/CollectionWindow.GoEdit` → `LoadScene("DeckEditor")`）
        /// ⇒ 背后没有主菜单。**实拍那一帧整条顶栏都在**，我们原来**一件都没有**。</para>
        ///
        /// <para>🔴 **判据 / 参数**：**全部**在共用件 <see cref="TopBar"/> 里 —— 它现在是顶栏的**唯一实现**
        /// （2026-10-17 B25 收口：`Shell/MainMenuRuntime.cs` 原来那份**已删**，主菜单也改调本件）。
        /// 队列**复用** `MainMenuRuntime.QBar*`（`public const`，**`2986–2998`** —— ⚠️ **2026-10-18 订正**：
        /// 这里原写 `2994–2998`，2026-10-17 F5 把下沿放宽到 `2986`，**唯一出处 = `Shell/TopBar.cs` 那张表**）
        /// —— 那一条带子的判据是
        /// 「在**所有窗之下**」，本窗底下的件从 `QSep 2999` 起 ⇒ **自动成立**（顶栏在最底、被本窗内容压住）。</para>
        ///
        /// <para>🔴 **2026-10-17（B25）：那 4 颗钮【已接点击】**（原来这里如实标着「只画、点了没反应」= 静默失败）——
        /// 接线在 `TopBar.Build` 里（**与主菜单同一份**）：设置 → `Main Menu Settings Window` ·
        /// 信箱 → `Inbox Menu` · 头像 → `Player Profile Window`；「挑战」那颗原版**不开窗**
        /// （= `ChallengeButton`：按挑战数显隐 + 弹「收挑战」确认框），本地没有挑战源 ⇒ 那一声是 `Debug.Log`（**出声**）。
        /// ⚠️ **派发靠 `PointerLayer`**（全壳唯一那条「真鼠标 → 界面」的路）：主菜单场景由壳建
        /// （`ShellRuntime` → `WindowsManager.EnsureHost`，**壳是 `DontDestroyOnLoad` ⇒ 进本场景时它还在**）；
        /// `PointerLayer.Instance` 自己也会惰性现建一台。⛔ **本类仍然不建指针层**（理由见 `Build()` 末尾那三条）。</para></summary>
        void BuildTopBar()
        {
            // 🔴 **2026-10-19（第五十六会话 · `T23`）：窗化档（`HostWindow != null`）不再建第二条顶栏。**
            //   判据（四条，全部现读）：
            //   ① 原版 `Deck Editing Menu` prefab 根**只有一个孩子 `Content Area`**，树里**没有顶栏**
            //      （`资料/普查产出_第二十四会话/D10_prefab树.md` §结论 1，逐节点实读）；
            //   ② 本窗 `placement = World(10)` = 原版 `1 - Below Upper Bar Holder`
            //      （`资料/任务线/界面线/任务/T01_卡组编辑窗.md` §2·1）⇒ 原版的顶栏是**外壳那一条**，
            //      窗本来就摆在它下面；
            //   ③ 外壳顶栏建在 `Safe area Only Horizontal` 下（`Shell/MainMenuRuntime.cs:496`
            //      `TopBar.Build(safeH, …)`），它是 `MainMenuWindow` 的**兄弟**、不随 `HideAllWindows`
            //      被藏（`Shell/WindowsManager.cs:356` 只藏 `GameWindow`）⇒ 开本窗时它**仍在屏上**；
            //   ④ 本方法建的也是 `TopBar.Build`（同一个 `New()` ⇒ localPosition 恒 0、同尺寸），
            //      队列同为 2986–2997（`Shell/TopBar.cs:102-110`）⇒ 两条**同位置同队列** = 双画
            //      （透明叠加 ⇒ 字/图被画两遍）。
            //   ⇒ **窗化档早退**；`HostWindow` 由 `Shell/DeckEditingWindow.Open()` 在 `Build()` **之前**赋值。
            //   ⚠️ **独立场景档**（`-wfscene DeckEditor` / `Editor/DeckScene.cs` 裸建 `DeckRuntime`）
            //      `HostWindow == null` ⇒ **照旧建**（那条路没有壳、没有别的顶栏）⇒ 行为不变。
            //   ⛔ 不要改成「藏外壳顶栏」：原版窗就在顶栏**下面**（placement=World），藏了就与原版相反。
            if (HostWindow != null) return;
            _topBar = TopBar.Build(Root, NoteMissingArt);
            // 🔴 **2026-10-17（B25①）出声**：顶栏那 4 颗钮的**派发靠 `PointerLayer`**（全壳唯一那条
            //   「真鼠标 → 界面」的路）。**正常路径上它一定在** —— 玩家是从收藏窗进来的
            //   （`Shell/CollectionWindow.GoEdit` → `LoadScene("DeckEditor")`），而**壳是 `DontDestroyOnLoad`**、
            //   `ShellRuntime.Build()` 里那句 `WindowsManager.EnsureHost(transform)` 把指针层建在**壳下面**
            //   ⇒ **跨场景活着**。⇒ 这里**只出声、不建**：单独 Play `DeckEditor.unity`（没有壳）时那 4 颗点不动，
            //   而那**不是静默** —— 下面这一句就是那一声（没有壳时打一次，⛔ 不是每帧）：
            if (WindowsManager.Instance == null)
                Debug.Log("[Deck] ⚠️ 顶栏那 4 颗钮（设置 / 信箱 / 头像 / 挑战）的**派发靠 `PointerLayer`**，"
                          + "而本场景此刻**没有壳**（`WindowsManager` 还没建）⇒ 那 4 颗**点不动**。"
                          + "正常路径不会走到这里（从收藏窗进来时壳是 `DontDestroyOnLoad`、指针层跟着它活着）；"
                          + "在编辑器里直接 Play `DeckEditor.unity`（或批处理自检）才会看到这一行。");
        }

        /// <summary>缺图登记（`TopBar` 那条路的回调 —— 与 `Ui()` 用**同一张表**，不另开第二本账）。</summary>
        void NoteMissingArt(string n) { if (!string.IsNullOrEmpty(n)) _missingArt.Add(n); }

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
            // 🔴 **2026-10-18（双语线 · 波 1 · P1）**：「返回」那颗钮走**原版现成的键**
            //   `MainMenu/MainButtons/ButtonLabel/Back`（表里本来就有：`Loc.cs` 的 `(返回, Back)`）。
            //   判据 = 原版 prefab 节点 `Deck Editing Menu / Content Area / Header / Close / Button Text`
            //   —— 本批按 pid 亲读 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_6720903872745959204.json`：
            //   `Localize.mTerm = "MainMenu/MainButtons/ButtonLabel/Back"` · 同 GO 的 TMP `m_text = "Back"`
            //   （父链沿 `RectTransform.m_Father` 逐级解到根，节点名与 `HdrBackX/Y/W/H` 那组常数同源）。
            //   ⛔ 附件里那条「自拟 `MenuDeck/Button/Back`」**不要** —— **原版有键就必须用原版的键**（施工单 §④·1）。
            var hdrBackTx = Txt("hdr_back_t", Loc.T("MainMenu/MainButtons/ButtonLabel/Back"), HdrBackX, HdrBackY, HdrBackW, HdrBackH, 2, Ink, QText);
            // 🔴 **2026-10-10（A1212 · 块 1 #35）**：接上原版那颗的 `m_enableAutoSizing`（四格 = **10 / 40 / 12 / 0**）。
            //   判据 = `R6_包装层战斗与其余.md` §2·F #35（`bundle_menus_assets_all ▸ Deck Editing Menu/Content Area/Header/Close/Button Text`）。
            //   ⚠️ **折行 = `0`** ⇒ `SetAutoFitBox` 内部那句 `SetWrapWidth` 会**无条件**把模式开成 `Normal(1)`
            //   ⇒ 紧跟一句 `SetWrappingMode(0)` 还原（现成先例 = `Shell/CollectionWindow.cs` 那颗 `Back` 的写法，
            //   以及 `Battle/Label.cs` 的 `SetWrapping` doc 里那句「调用方在 `SetAutoFitBox` 之后还原自己那一档」）。
            if (hdrBackTx != null)
            {
                // 🔴 **2026-10-17（`A1226` 表 B #35）：框宽 = 原版有效框 133.22。**
                //    判据 = 原版 `…/Header/Close/Button Text` 的 `RectTransform`（伸锚跨度 0.92575 ·
                //    `m_SizeDelta.x = −5.64`）⇒ 父宽 150 × 0.92575 − 5.64 = **133.22**（原来整颗钮 150，宽 +16.78）。
                // ⚠️ **框高这一格【故意不跟】**：原版有效框高 48.24（按 Pragati 行盒 0.9474em 算，
                //    40px 的字占 37.9 < 48.24 ⇒ 原版停在 40）。而**我们全仓只有 CJK 那一份字体**
                //    （行盒 **1.516em**，见 `Core/TmpFont.cs` 的 `A1024③`）⇒ 喂 48.24 会把字压到
                //    48.24/1.516 = **31.8px**（原版渲 40）—— 那是**看得见的变小**，不是更接近原版。
                //    ⇒ 框高暂留整颗钮高 `HdrBackH`，等 `A1024③` 换面之后一起改（已在 T07 报告登记）。
                hdrBackTx.SetAutoFitBox(U(133.22f), U(HdrBackH), 10f, 40f, 12f);
                hdrBackTx.SetWrappingMode(0);
            }
            Btn_("hdr_back", HdrBackX, HdrBackY, HdrBackW, HdrBackH);

            // Filters 圆钮 + 图标 + 文字（原版这三块是分开的三条 rect）
            _hdrFltBtn = Img("hdr_fltbtn", "40k_menu_bt", HdrFltBtnX, HdrFltBtnY, HdrFltBtnS, HdrFltBtnS, QRow);
            // 🆕 2026-10-05（A32②）：这一颗**两种行为都有**，各接各的（不冲突）：
            //   ① **状态 → 换图**（`changeSpriteOnValueChange`，`RefreshHeader` 里那句 `SetSprite`）；
            //   ② **悬停 → 变色**（`m_Transition = 1(ColorTint)`，目标件 = 它自己那层图，`m_Color=(1,1,1,1)`
            //      ⇒ 悬停**看得见**）。⚠️ `RefreshHeader` 换的是**纹理**、`WindowButton` 变的是**顶点色**
            //      ⇒ 两条路互不覆盖（这正是原版那颗 prefab 的实际行为）。
            HoverTint("hdr_filters", _hdrFltBtn != null ? _hdrFltBtn.gameObject : null);
            // 🔴 **2026-10-20 修：这一颗的队列** —— 原来用 `QBorder`（= 3005），而**按钮底**
            //   `hdr_fltbtn` 用的是 `QRow`（= 3007）⇒ **按钮底压在图标上面**。按钮底那张
            //   `40k_menu_bt` 的中心是**不透明的** ⇒ 图标被**整块盖死**（屏幕上那颗金框看着是空的）。
            //   ⚠️ 图本身没错、也不用 tint：`Resources/Art/ui_deck/40k_bt_icon_search.png` 与解包侧
            //   六个副本 **md5 全同**（`ef79fb28…`、27×27、非透明 355 px）。
            //   改成 **`QText - 1`**（= 3008，与 `QRow + 1` 是同一个数）：**必须高过按钮底 `QRow`、
            //   又留在 `过滤器` 那行字（`QText`）下面**。⚠️ 3008 这个号同时也是 `QTabName`，
            //   但页签名牌在侧栏（x 5.3..104.2 · y 261..301）、本图标在 x 377.2..407.2 · y 98.5..128.5
            //   ⇒ **两处不重叠**（同队列的「不许互相压住」那条断言不碰它）。
            //   🔴 **本仓血规：分层用渲染队列、⛔ 不能拿 z 排**（透明队列按到相机的 3D 距离排序）。
            Img("hdr_flticon", "40k_bt_icon_search", HdrFltIconX, HdrFltIconY, HdrFltIconS, HdrFltIconS, QText - 1);
            // 🔴 **2026-10-17（D20）—— 这颗图标查清了：原版【就是这张图】，保持现状。**
            //   施工单 D20 存疑两点：① 名字是 `search` 不是 `filter`；② 实拍那一帧那颗金框**看着是空白的**。
            //   逐字段实读（`python 工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 4`）：
            //   `Header/Filters/**Icon**` = `Image,EverguildButtonMaterialModifier` ·
            //   **`40k_bt_icon_search 27×27`** · `Simple (1,1,1,1)` · `preserveAspect` ⇒
            //   **原版这个位置挂的就是 `40k_bt_icon_search`**（那颗 `Image` 也是启用着的，行上没有任何
            //   `INACT` / `被禁` 标记）。⇒ 结论 = 「**原版就是这张图**」，我们的图名**本来就对**。
            //   🔴 **2026-10-20 就地订正（铁律 5）**：这一行原来写「⚠️ 实拍里显得空 = 那张图**本身很细**
            //      （27×27、笔画细）在缩略图上看不清，**不是没画**」—— **那个解释是错的**（那是我当时的猜测）。
            //      真因 = **画序反了**：图标 `QBorder(3005)` < 按钮底 `QRow(3007)` ⇒ **按钮底把图标整块盖住**
            //      （按钮底那张 `40k_menu_bt` 的中心不透明）—— 已修，见上面那段。⛔ 别把那条旧解释再写回来。
            //   ⛔ 别因为名字里是 `search` 就换成别的「漏斗」图 —— 那是拿印象推翻判据。
            // 🔴 **2026-10-17（D2）：文案走 `Loc.T`（中文档 = 原版实拍的中文「过滤器」）。**
            //   词条键 = **原版 prefab 上那颗 `Localize` 的 `mTerm`**，⛔ 不是自拟的：
            //   `Deck Editing Menu/Content Area/Header/Filters/Label`（那颗 TMP 的 `m_text = "Filters"`·
            //   fs40）挂的 `Localize.mTerm = **MenuDeck/Filters/Filters**`（实读 `bundle_menus_assets_all`）。
            //   中文那一列 = **原版实拍**那三个字（`资料/原版参照图/用户实拍_1017/卡组编辑界面参考.png`
            //   页头那颗金框钮右边）。
            //   ⛔ 别在代码里再写一份中文常量表 —— 词条只此一份（`Core/Loc.cs`）。
            //   断言 → `Editor/DeckScene.cs` 的 G1 节 ⑭。
            var hdrFltLblTx = Txt("hdr_fltlbl", Loc.T("MenuDeck/Filters/Filters"),
                HdrFltLblX, HdrFltLblY, HdrFltLblW, HdrFltLblH, 2, Ink, QText);
            // 🔴 **2026-10-10（A1212 · 块 1 #36）**：接上 `m_enableAutoSizing`（四格 = **10 / 40 / 32 / 0**）。
            //   判据 = `R6` §2·F #36（`…/Content Area/Header/Filters/Label`）。
            //   ⚠️ 折行 = `0` ⇒ 接完显式还原（同 `hdr_back_t` 那一处，⛔ 别省这一句）。
            if (hdrFltLblTx != null)
            {
                hdrFltLblTx.SetAutoFitBox(U(HdrFltLblW), U(HdrFltLblH), 10f, 40f, 32f);
                hdrFltLblTx.SetWrappingMode(0);
            }
            Btn_("hdr_filters", HdrFltBtnX, HdrFltBtnY, HdrFltLblX + HdrFltLblW - HdrFltBtnX, HdrFltLblH);

            // 🆕 A24：原版 `Header/Filters/Generic Simplified UI Button_updated`（文本 'Clear filters'）
            //   也是 `SpriteSwap`（`m_TargetGraphic` = 自己那层 Image，两张高亮图同上）
            _hdrClear = Img("hdr_clear", "UI_Button_Mulligan", HdrClearX, HdrClearY, HdrClearW, HdrClearH, QRow);
            Hover("hdr_clear", _hdrClear, "UI_Button_Mulligan");
            // 🔴 **2026-10-18（A891 的续）：文案走 `Loc.T`** —— 词条键 = 原版 prefab 上那颗 `Localize.mTerm`
            //   的原文 `MenuDeck/Filters/ClearFilters`（5 颗同键，节点名一律 `Button Text`；本批按 pid 亲读）。
            //   🔴 **本窗这颗的父链**（2026-10-18 第三轮整改 · 审查 P3 就地订正，铁律 5）：
            //     `Deck Editing Menu > Content Area > Header > Filters > **Generic Simplified UI Button_updated** > Button Text`
            //   —— 本行原来写的是 `… > Header/Filters/**Clear Filter Button**/Button Text`：**中间那个节点名写错了**。
            //   `Clear Filter Button` 是**收藏窗那 4 颗**的父名（`… > Header Filters/Header > Clear Filter Button >
            //   Button Text`）—— 两棵树的中间节点名**不同**，当时混成一个了。
            //   英文那一列 = 那颗 TMP 的 `m_text` 原文 `Clear filters`；中文列 = 「清除过滤器」（2026-10-14 官方 zh-CN 表；原「清除筛选」）
            //   （源 `数据/本地化/i18n/zh_CN.csv:9` —— 该中文列 2026-10-14 已被**官方 zh-CN 表**顶掉，不再是「我们译的」）。
            //   ⛔ 节点名 `hdr_clear_t` 不动（`Editor/DeckScene.cs` 按名找）。
            var hdrClearTx = Txt("hdr_clear_t", Loc.T("MenuDeck/Filters/ClearFilters"),
                HdrClearX, HdrClearY, HdrClearW, HdrClearH, 2, Ink, QText);
            // 🔴 **2026-10-20**：这一颗与上面那颗图**一起显隐**（判据 → `HdrClearX` 上面那一大段；
            //   落点在 `RefreshHeader`，命中闸在 `KeyLive`）。
            _hdrClearTx = hdrClearTx;
            // 🔴 **2026-10-10（A1212 · 块 1 #37）**：接上 `m_enableAutoSizing`（四格 = **10 / 42 / 12 / 0**）。
            //   判据 = `R6` §2·F #37（`…/Header/Filters/Generic Simplified UI Button_updated/Button Text`）。
            //   ⚠️ 折行 = `0` ⇒ 显式还原（同上）。
            if (hdrClearTx != null)
            {
                // 🔴 **2026-10-17（`A1226` 表 B #37）：框宽改成【原版有效框】 = 父宽 × 锚跨度 − `m_SizeDelta.x`。**
                //    判据 = 原版那颗 TMP 的 `RectTransform`（`m_AnchorMin.x→m_AnchorMax.x` 跨度 0.92575 ·
                //    `m_SizeDelta.x = −10`）。
                //    🔴 **2026-10-22（`D1`）就地订正（铁律 5）**：上一版拿**模板位**的父宽 250 算出
                //    `250 × 0.92575 − 10 = 221.44` —— 可那颗钮布局完之后父宽是 **85**（见 `HdrClearX` 那段的
                //    Grid 现算）⇒ 真值 = `85 × 0.92575 − 10 = ` **68.69**。221.44 会让字框宽出钮 2.6 倍。
                // ⚠️ **框高【故意不跟】**（原版有效框高 57.69）：CJK 行盒 1.516em ⇒ 喂 57.69 会把 42px 压到
                //    38.05px（原版渲 42）⇒ 暂留 `HdrClearH`（= 钮高 70），与 `A1024③` 一起改（同 `hdr_back_t` 那条）。
                hdrClearTx.SetAutoFitBox(U(68.69f), U(HdrClearH), 10f, 42f, 12f);
                hdrClearTx.SetWrappingMode(0);
            }
            Btn_("hdr_clear", HdrClearX, HdrClearY, HdrClearW, HdrClearH);

            // Wildcard Counter：底板 + 四个稀有度图标（30×44）+ 各自的数量
            // 🔴 **2026-10-11（`A1497④`）：整条通配符计数条吃【同一个页判据】**（`CardDisplayPage`）。
            //   判据 = `DeckEditingWindow__ToggleDisplay.c` 里 `wildcards(0xd0).SetActive(param)` 与
            //   `cardDisplay(0x78).SetActive(param)` 是**同一次调用里的同相两件**（`param = 0` 只在
            //   `DeckEditingStateController__ToggleDeckCosmetic.c` 那一支）⇒ **Cosmetics 页上这一整条
            //   （底板 + 4 图标 + 4 数字 + 阵营徽记）都要关掉**，Cards / Deck info 两页都显示。
            //   ⚠️ 原版这四个 key 的父链都在 **同一个 GO** 底下（`DeckEditingWindow.wildcards` 指向它）：
            //   `Content Area/Header/WIldcard Counter/{Background, Counters/*, Army Icon}`
            //   ⇒ **一件都不能漏**（漏了就是「一条规则两种结果」）。
            //   我们的外层没有那格容器，只能逐个登记进 `_cardsInfoOnly`（显隐在 `RefreshTabVisibility`）。
            // 🔴 **2026-10-18（N57）**：原版 `WIldcard Counter/Background` = `40k_topmarquee_currency_display BW`
            //   **Sliced 九宫 15** · `m_Color (0.462,0.462,0.462,1)`（判据 = `Deck Editing Menu` dump）。
            //   原来走 `Img`（Simple 拉伸 + 无 tint = 白）⇒ 画成一条亮白边；改九宫 + 原版灰。
            var wcBg = MenuDraw.Nine(Root, Ui("40k_topmarquee_currency_display_BW"),
                                     new PxRect(WcBgX, WcBgY, WcBgX + WcBgW, WcBgY + WcBgH),
                                     new Vector4(15f, 15f, 15f, 15f), 44f, 39f, QPanel,
                                     new Color(0.462f, 0.462f, 0.462f, 1f), true, "hdr_wcbg");
            if (wcBg != null) _cardsInfoOnly.Add(wcBg);
            string[] wcIc = { "40k_general_wildcard_common_small", "40k_general_wildcard_rare_small",
                              "40k_general_wildcard_epic_small", "40k_general_wildcard_legendary_small" };
            for (int i = 0; i < 4; i++)
            {
                // 🔴 2026-09-27（PA 普查）：原版这 4 个 `.../WIldcard Counter/Counters/*/Icon` 是 PA=1 + Simple，
                //   贴图 42×51 / 41×51 塞进 30×44 ⇒ 原版实绘 **30×36.4（37.3）**，我们原来拉伸成 30×44（高 ×1.18~1.21）。
                //   ⚠️ 同一件在收藏窗（`CollectionWindow.cs` 里那处 `keepAspect` 补）与卡片详情窗（`CardDetailPopup.cs` 里那处 `keepAspect` 补）也是同一错，三处一起修。
                var wcIco = Img("hdr_wc" + i, wcIc[i], WcIconX[i], WcIconY, WcIconW, WcIconH, QRow, true);
                if (wcIco != null) _cardsInfoOnly.Add(wcIco.gameObject);   // 同上：整条一起显隐
                // `Counter` 在图标**右侧同一水平带**（41×44 · 字号 32.6 · 白 · 居中 · NoWrap）
                // 🔴 **2026-10-17（B10）**：出厂字面量原来写的是 `"0"`，靠 `RefreshHeader()` 里那句
                //   改成 `99`（用户 2026-09-28 拍板：恒定 `99`）—— 中间有一个瞬间它**真的是 `0`**
                //   （谁在 `RefreshHeader` 之前读一次/渲一帧都会看到 0）。这里直接建成 `99`，
                //   与 `RefreshHeader` 那句**同一个值**，⛔ 别再把两边写成两个数。
                _wcTxt[i] = TxtPx("hdr_wct" + i, WcCountText, WcIconX[i] + WcIconW, WcIconY, WcCntW, WcIconH,
                                  WcCntFontPx, Ink, QText);
                // 🔴 **2026-10-10（A1212 · 块 1 #38）**：接上 `m_enableAutoSizing`（四格 = **10 / 38 / 45 / 1**）。
                //   判据 = `R6` §2·F #38（`…/Header/WIldcard Counter/Counters/{Common,Rare,Epic,Legendary}/Counter`
                //   —— 原版这**四颗逐值相同**）。标称字号已经由 `TxtPx(..., WcCntFontPx)` 给定（= 原版 `m_fontSize` 32.6，
                //   见那条常数的注释），本句只补「框 + 自适应区间 + base」。
                //   ⚠️ 折行 = `1`（= `Normal`）⇒ 与 `SetAutoFitBox` 内部设定的那一档**同档**，不必还原。
                //   🔴 **2026-10-17（`A1229`）**：四格/字号/框收口到 `FitWildcardCount`（三个宿主共用那一份）。
                if (_wcTxt[i] != null)
                {
                    FitWildcardCount(_wcTxt[i]);
                    _cardsInfoOnly.Add(_wcTxt[i].gameObject);              // 同上：整条一起显隐
                }
                // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版这 4 颗
                //   `…/WIldcard Counter/Counters/{Common,Rare,Epic,Legendary}/Counter`
                //   = `m_VerticalAlignment = **Capline**`（判据 = `WA712_垂直对齐普查.md` §2·5 的归属表
                //   那一行：「卡片详情窗·通配符计数 ×4（Capline）」＋ 本文件 `:2882` / `:4388` 两处
                //   `TxtPx` 的 doc 自己写着的「原版 `VerticalAlignment = Capline` ⇒ 数字相对几何中心**略偏上**，
                //   所以自检要按**渲染矩形**比、别按几何中心比」）。
                //   🔴 **2026-10-16 就地订正（铁律 5）**：那两句 doc 原来写的是「**验收这类数字量渲染图**」
                //   —— 那是 `SetVAlign` 还不存在时的**绕法**；现在档位能真落了，落完之后**几何中心就是原版那一格**，
                //   自检要**量渲染图**这条纪律对**别的**件仍然成立，但不再是本件的唯一出路。
                MenuDraw.SetVAlign(_wcTxt[i], Label.VAlign.Capline,
                                   new PxRect(WcIconX[i] + WcIconW, WcIconY, WcIconX[i] + WcIconW + WcCntW, WcIconY + WcIconH));
            }
            // 🔴 **2026-10-17（A893）**：这一格喂的是【**阵营徽记**】。
            //   改之前喂的是 `FactionIcon(null)` ⇒ 拿到的是**卡组图标** `40k_collection_bt_decks`，
            //   而**收藏窗同一位置**那颗（同属 `Wildcard Display` 第三个孩子 `Army Icon`）是**阵营徽记**
            //   ⇒ 同一位置两个图标不一致（A893 的开单理由）。
            //
            //   判据（三层，逐条可复跑）：
            //   ① **原版 prefab 出厂值** = `40k_DeckSelection_icon_FactionBlackLegion`
            //      —— 节点 `/Deck Editing Menu/Content Area/Header/WIldcard Counter/Army Icon`，
            //      `m_Sprite` pid `5519774263930623761`（实读
            //      `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-4938031877995384135.json`；
            //      该 pid 在整份切图缓存 5557 个 `Sprite/*.json` 里**只对应这一个名字**）
      //        · `m_Type = 0 (Simple)` · **`m_PreserveAspect = 1`** · 框 80×85（**`1470,70.97 → 1550,155.97`**，
      //          `menu_rect.py bundle_menus_assets_all "Deck Editing Menu"` 现读；原来这里写的 `71/156` 是四舍五入值）。
      //   ② 🔴 **出厂那张只是出厂值 —— 运行期会被换成【这副卡组所属阵营】的徽记**：
      //      `DeckEditingWindow__FilterToDeck.c:24-28`：`uVar3 = 10`（= `CardArmy.Ultramarines`），
      //      `if (EditingDeck.deckArmy != 0) uVar3 = deckArmy;` →
      //      `WildcardDisplay__Initialize(wildcards, uVar3)` → `ArmyUtilities.GetArmyIcon(army)`
      //      → `Image.set_sprite`。（`EditingDeck` = `DeckEditingWindow` 字段 `+0x118`，
      //      `deckArmy` = `CardDeck` 字段 `+0x50` = `dump.cs:20478`；`CardArmy.Ultramarines = 10`
      //      = `dump.cs:45640`。）调用点 = `__TryOpen.c:134`（**开窗**）与 `__RefreshCards.c:7`。
      //      ⇒ **照做 = 按这副卡组的阵营换**；**没有督军（`deckArmy == 0`）⇒ Ultramarines**
      //        （原版那个字面量 `10`），⛔ **不是**保留出厂的 BlackLegion —— 出厂值在运行期**永远走不到**
      //        （`WildcardDisplay.currentArmy` 是**非序列化**私有字段、出厂恒 0，见 `dump.cs:75792`）。
      //   ③ ⚠️ **本笔没做的两条运行期换图路径**（判据在、属别的件，已写进报告）：
      //      `DeckEditorCollectionDisplay__CheckFocusedArmy.c`（指针压到卡池某格 ⇒ 换**那张卡**的徽记）
      //      与 `DeckEditingWindow__OpenCardInformation.c:35`（开卡片详情 ⇒ 换**那张卡**的徽记）。
      //
      //   ⚠️ 顺带把 `keepAspect` 打开：原版这颗 `m_PreserveAspect = 1`（上面 ① 实读），
      //   源图 256×256 正方 ⇒ 原版实绘 **80×80**（框 80×85 里居中），我们原来拉伸成 80×85。
      //   同一位置那颗**收藏窗**的 `Army Icon` 走的是 `MenuDraw.Rect(…, keepAspect: true)`
      //   （`Shell/CollectionWindow` 里那处）⇒ 顺手把这两处也拉齐（其余 PA=1 的件见 `资料/普查产出_0927/PA普查_五块汇总.md`）。
            // 同上：这颗是 `WIldcard Counter` 的第三个孩子 `Army Icon` —— 与那三个 key 同一个 `SetActive(param)`。
            var wcArmy = Img("hdr_army", DeckArmyIcon(State), ArmyIconX, ArmyIconY, ArmyIconW, ArmyIconH, QRow, true);
            if (wcArmy != null) _cardsInfoOnly.Add(wcArmy.gameObject);
        }

        // ------------------------------------------------------------ Sidebar

        void BuildSidebar()
        {
            // ---- 侧栏底板：`40k_main_tab_background` + 原版那颗 **`Gradient2`**（横向 · **Multiply** · 2 色标）----
            // 判据 = `Deck Editing Menu > Content Area > Sidebar > Background`（RT `-8357444074261844188`）的
            //        `Gradient2` 组件 MB `8417643871566819108` 实读（`bundle_menus_assets_all`）：
            //          `_gradientType = 0`（**横**）· `_blendMode = 2`（**Multiply**）· `_modifyVertices = 1`
            //          key0 = (0.10377359, 0.00440548, 0.00440548) @ `ctime0 = 4241 / 65535` = **0.06471**
            //          key1 = **白** @ `ctime1 = 65535`
            //          alpha 键 `atime 0 / 65535`、值 **1 / 1** ⇒ **alpha 恒 1**
            //        ⇒ **左沿 6.47% 是深红、往右线性回到原亮度**（参考图取不到干净采样，本件只有预制体判据）。
            // 🔴 **2026-10-18（T29 · D5）**：原来这里只有 `Img("side_bg", …)` 一张底图 ⇒ 整块同亮度、缺这条渐变。
            // ⚠️ **Multiply 语义**（⛔ 别用「半透明黑压一层」冒充 —— 那是 alpha 混合、不是乘法）：
            //    反编译 `UnityEngine.UI.Extensions.Gradient2__ModifyMesh.c:303-314`：`_blendMode==1` 加法、
            //    `==2` **乘法**（顶点色 = 原顶点色 × 求值色）、其余**替换**。原版那颗 `Image.m_Color` 是**白**的
            //    ⇒ 原顶点色 (1,1,1,1) × 求值色 = **求值色本身** ⇒ 乘法与替换**同结果**。
            //    我们 `ImageQuad` 的 shader 本来就是 `tex × 顶点色`（同 `MenuDraw.GradientV` 的做法）
            //    ⇒ 只要把**顶点色**设成求值色就**逐像素等价**，不需要新的混合原语。
            // ⚠️ 结构 = 照 `MenuDraw.GradientV` 的同一形状就地写**横版**两条带（那个助手是竖版，本批不许改 `MenuDraw.cs`）：
            //      `[0, t0]` 恒 key0 · `[t0, 1]` 从 key0 线性到白（Unity `Gradient.Evaluate` 区间外夹到首尾键）。
            //    ⚠️ `side_bg` 因此从「单块 quad」变成「一棵子树」——`Editor/DeckScene.cs` 那三条读数都认子树：
            //      `UiQuadRect`→`UiNodeRect` 取**包围盒**（= 整条侧栏 -203,156→335.5,1080.1）、
            //      `UiQueueOf`/`UiQuadActive` 取**第一块**（队列 QSide / active）⇒ 既有断言一个数都不变。
            //      ⛔ 别为了「省事」改回单块：那样 0..6.47% 那一段会被画成全程线性（左沿偏亮）。
            var sideBgR = new PxRect(SideBgX, SideBgY, SideBgX + SideBgW, SideBgY + SideBgH);
            var sideBg = MenuDraw.Node(Root, "side_bg", sideBgR);
            {
                const float stop = 4241f / 65535f;                       // 0.0647
                var cLeft = new Color(0.10377359f, 0.00440548f, 0.00440548f, 1f);
                var tex = Ui("40k_main_tab_background");
                var b0 = MenuDraw.Rect(sideBg, tex,
                                       new PxRect(SideBgX, SideBgY, SideBgX + SideBgW * stop, SideBgY + SideBgH),
                                       "Band 0", QSide);
                if (b0 != null)
                {
                    b0.SetUvRect(new Rect(0f, 0f, stop, 1f));            // uv 只取左起那一小段
                    b0.SetCornerColors(cLeft, cLeft, cLeft, cLeft);      // 恒定 key0
                }
                var b1 = MenuDraw.Rect(sideBg, tex,
                                       new PxRect(SideBgX + SideBgW * stop, SideBgY, SideBgX + SideBgW, SideBgY + SideBgH),
                                       "Band 1", QSide);
                if (b1 != null)
                {
                    b1.SetUvRect(new Rect(stop, 0f, 1f - stop, 1f));
                    b1.SetCornerColors(cLeft, Color.white, Color.white, cLeft);   // 左 key0 → 右 key1
                }
            }

            // Window Options：三个互斥页签（Cards / Deck info / Cosmetics）
            string[] tabIc = { "40k_collection_bt_cards", "40k_collection_bt_decks", "40k_collection_bt_cosmetics" };
            // 🔴 **2026-10-17（D3）：三颗页签的文案走 `Loc.T`**（中文档 = 原版实拍的 **张牌 / 卡组信息 / 美容品**）。
            //   词条键 = **原版 prefab 上那三颗 `Localize` 的 `mTerm`**（逐颗实读 `bundle_menus_assets_all`，
            //   节点 = `/Deck Editing Menu/…/Window Options/Buttons/{Cards,Info,Cosmetics}/Label/Text`）：
            //     · `Cards`     → **`MenuShop/ShopItemType/Cards`**（TMP `m_text = "Cards"`）
            //     · `Deck info` → **`MenuDeck/HUD/DeckDescription/DeckInfo`**（`m_text = "Deck info"`）
            //     · `Cosmetics` → **`MenuShop/ShopItemType/Cosmetics`**（`m_text = "Cosmetics"`）
            //   ⚠️ 第 1、3 条页签的键在 **`MenuShop/ShopItemType/*`**（商城那一族的「物品种类」词条）、
            //     第 2 条在 **`MenuDeck/HUD/…`** —— **原版就是这么分的**，
            //     ⛔ 别「看着不齐」把它们改成同一个前缀（键名改了 = 词条查不到 = 界面上印键名）。
            //   中文那一列 = **原版实拍**（`资料/原版参照图/用户实拍_1017/卡组编辑界面参考.png` 名牌上那三行）。
            //   ⚠️ 换成中文之后**字号由自适应重算**（原版中文客户端也是这么自适应那颗 `m_enableAutoSizing`）
            //     —— 版面参数（框 98.96×40 · min/max 10/34）一个字都没动，见下面那两句。
            //   断言 → `Editor/DeckScene.cs` 的 G1 节 ⑭。
            string[] tabTx = { Loc.T("MenuShop/ShopItemType/Cards"),
                               Loc.T("MenuDeck/HUD/DeckDescription/DeckInfo"),
                               Loc.T("MenuShop/ShopItemType/Cosmetics") };
            // 🔴 **2026-10-11（A305①）**：三颗页签**原版 base 各不同** —— 这是「一句站三颗、三颗各不同」
            // 那一类里最典型的一处（铁律 5·c）。判据（原版实读，
            // `/Deck Editing Menu/…/Buttons/{Cards,Info,Cosmetics}/Label/Text`）：
            //   `Cards` **`m_fontSizeBase 26.0`**（`m_fontSize 34`）·
            //   `Deck info` **24.0**（`m_fontSize 31.5`，折行=1）·
            //   `Cosmetics` **24.0**（`m_fontSize 28.15`，折行=0 —— 已单独还原，见下面那句）。
            //   逐站表 → `资料/普查产出_1011/V7_A305_A304_普查.md` §二·3 #13/#14/#15。
            float[] tabBasePx = { 26f, 24f, 24f };
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
                //     「`SetAutoFitBox` 的 `max` 取的是**那一刻 `SetGlyphHeight` 设的字号**（`Label.cs` 里设 `_pendWrapW` 那一段）」
                //     —— **行号与新语义都不成立**。现在是：`SetAutoFitBox` 拿 `cur`（= `SetGlyphHeight`
                //     那一刻定下的字号，`Battle/Label.cs` 的 `SetGlyphHeight`）当**换算基准** `NominalPx()`，
                //     `fontSizeMax = cur × maxPx / NominalPx()`（`Battle/Label.cs` 的 `SetAutoFitBox`）⇒
                //     **没有先 `SetGlyphHeight` 就没有基准**（`cur <= 0f` 会直接早退）⇒ 仍要先 `TxtPx`（内含它）。
                var tx = TxtPx("tab_tx" + i, tabTx[i], cellX + TabNameDx, TabNameY, TabNameW, TabNameH,
                               TabNameMaxPx, Ink, QText);
                // 🔴 **2026-10-07（A62 主表 #2）**：三颗页签共用这一句 `SetAutoFitBox`，而它们原版的折行**不是同一档** ——
                //   dump（`python 工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 14 --md`）逐行：
                //   `'Cards' 折行=1` · `'Deck info' 折行=1` · **`'Cosmetics' 折行=0`**。
                //   `SetAutoFitBox` 内部会**无条件开折行** ⇒ 前两颗「碰巧对」、第三颗是**真偏离** ⇒ 只对 `i == 2` 关掉。
                //   （真正该做的是给三颗各带一个模式参数；今天先按这一条 dump 落地，别拿 `LabelCenter` 之类去反推。）
                // 🔴 **2026-10-11（A305①）**：第 5 个实参 = **逐颗**的原版 `m_fontSizeBase`（见 `tabBasePx` 的注释）
                //   —— 三颗**不是同一个值**（26 / 24 / 24），所以这里必须按 `i` 取，⛔ 别写死一个常数。
                if (tx != null) tx.SetAutoFitBox(U(TabNameW), U(TabNameH), TabNameMinPx, TabNameMaxPx, tabBasePx[i]);
                if (tx != null && i == 2) tx.SetWrapping(false);
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
            //   （两处不是同一张图；`40K_dropdown_bg` 归外壳那扇导入弹窗 —— 本窗那份 2026-10-17 已按 T23 删件删掉）。
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
                //       `− parent.position` 减的就是零（判据 → `Editor/DeckScene.cs` 的 `TestLayout` 里那条「`Root` 在原点」不变式）；
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
            // 🔴 **2026-10-18（A891 的续 · 第三轮整改 · 审查 P4）：占位符走 `Loc.T`** —— 键 = 原版那颗
            //   `Placeholder` 的 `Localize.mTerm` 原文 `MenuDeck/HUD/EditDeckName`（全库只 1 颗，就是这一颗：
            //   `Deck Editing Menu > Content Area > Sidebar > Window Options > Deck Name > Text Area > Placeholder`）。
            //   英文列 = TMP 原文 `Tap to edit deck name`；中文列 = 「点击以编辑卡组名称」(`zh_CN.csv:172`；2026-10-14 换官方 zh-CN 表，原「点击编辑卡组名」)。
            //   ⛔ 节点名 `name_h` 不动（`Editor/DeckScene.cs` 的 D34 那一节按名找它量档位/左沿）；
            //   ⚠️ 它左对齐 ⇒ 文案变宽窄**不动左沿**，D34 那条 19.5 不受影响。
            _deckNameHint = Txt("name_h", Loc.T("MenuDeck/HUD/EditDeckName"), NameTxX, NameTxY, NameTxW, NameTxH, 2,
                                new Color(1f, 1f, 1f, 0.42f), QText);
            // 🔴 **2026-10-17（D34）**：这两个（原版 `EverguildInputField` 的 `Text` 与 `Placeholder`）
            //   都是 **`m_HorizontalAlignment = 1`(Left) / `m_VerticalAlignment = **8192**`** ——
            //   我们原来是**居中**（`TmpFont.NewText` 把每颗 TMP 统一建成 `Center`，见 `Label.SetAlignLeft` 的 doc）。
            //   判据（逐份实读 `bundle_menus_assets_all/MonoBehaviour/`，**自己按 `m_text` 认出来那两颗**）：
            //     · `Placeholder`（`'Tap to edit deck name'` · GO `Placeholder_-4266719589130047708`）
            //       = `MB **-4659515947641016540**`：`HA 1` · **`VA 8192`** · `fs 28` · `base 24`
            //     · 兄弟 `Text`（`'​'` · GO `Text_6772987722004426532`）
            //       = `MB **-9162762440952910044**`：`HA 1` · **`VA 8192`** · `fs 28` · `base 28` · `auto[10~28]`
            //   🔴 **`8192` 就是 TMP 的 `Capline`（`0x2000`），不是 `Baseline`** —— 施工单 D34 那行的
            //     括号里写的是「Left/Baseline」，**那个标签是错的**（`Baseline` 是 `0x800`，本工程
            //     `Label.VAlign` 里也没有这一档；`Capline` 有，见 `Battle/Label.cs` 那个 enum）。
            //     ⇒ 两个半边**都落得下来**：`SetAlignLeft()`（= HA 1）+ `SetVAlign(Capline, 框高)`。
            //   ⚠️ `SetAlignLeft` 必须调在**任何「定版面」之前**（这里刚 `Txt` 完）；`SetVAlign` 排在它
            //     **之后**（它只换算垂直档位、不动 `alignment`，顺序反了会被 `SetAlignLeft` 那条
            //     `TextAlignmentOptions.Left` 里的 `V=Middle` 覆盖掉）。
            var nameBox = new PxRect(NameTxX, NameTxY, NameTxX + NameTxW, NameTxY + NameTxH);
            if (_deckNameText != null)
            {
                _deckNameText.SetAlignLeft();
                // 🔴 **2026-10-10（A1212 · 块 1 #39）**：接上 `m_enableAutoSizing`（四格 = **10 / 28 / 28 / 3**）。
                //   判据 = `R6` §2·F #39（`…/Sidebar/Window Options/Deck Name/Text Area/Text`），
                //   折行档那一格 = 原版 `m_TextWrappingMode = **3**`（`PreserveWhitespaceNoWrap` —— TMP 给
                //   **单行输入框**的那一档，见 `Battle/Label.cs` 的 `WrappingMode` doc）。
                //   ⚠️ **⛔ 别拿 `SetWrapping(false)`（= `0`）顶替** —— 那是**静默降档**
                //   （`Label.SetWrappingMode` 只收 `0/1/2/3` 的原文；同判据 = `FilterPanelModel.DeckEditInputWrap`）。
                //   ⚠️ **位置**：这一段排在 `Txt(...)` 之后、`SetAlignLeft()` **之后** ——
                //   `SetAlignLeft` 的 doc 写着「要在『定版面』之前调（`SetAutoFitBox` / `SetWrapWidth` /
                //   `ForceRelayout` 那三条）」，反过来会让画面停在旧对齐上、并给 `AlignAfterLayoutCount` 多记一笔
                //   （A546② 那条断言点名列过「会计 3 笔」）。`SetVAlign` 照旧排在最后。
                _deckNameText.SetAutoFitBox(U(NameTxW), U(NameTxH), 10f, 28f, 28f);
                _deckNameText.SetWrappingMode(3);
                MenuDraw.SetVAlign(_deckNameText, Label.VAlign.Capline, nameBox);
            }
            if (_deckNameHint != null)
            {
                _deckNameHint.SetAlignLeft();
                // 🔴 **2026-10-10（A1212 · 块 1 #40）**：接上 `m_enableAutoSizing`（四格 = **10 / 28 / 24 / 0**）。
                //   判据 = `R6` §2·F #40（同上 `…/Text Area/Placeholder`）。折行 = `0` ⇒ 还原（同 `hdr_back_t`）。
                //   位置理由同上面那颗。
                _deckNameHint.SetAutoFitBox(U(NameTxW), U(NameTxH), 10f, 28f, 24f);
                _deckNameHint.SetWrappingMode(0);
                MenuDraw.SetVAlign(_deckNameHint, Label.VAlign.Capline, nameBox);
            }
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
            // 🔴 **2026-10-20 删掉一行**：这里原来还有 `var cntTex = Ui("40K_main_deck_card_counter");`
            //   —— 它只喂过行里那颗 `row_k*`（**原版行里没有那个节点**，已删）。
            //   ⚠️ 这张图**没有因此变成孤儿**：卡池那条「张数」底图还在用它
            //   （`PoolCounterSprite` = 同一个名字，`RefreshPool` 里 `poolbar_*` 那一支）。
            // 稀有度色条**只占右侧那一段**（原版 `Rarity Gradient` 的锚）—— 矩形按行号取，
            //   走类级的 `RowGradRect`（判据全文 → `RowBgHeroX` 那一段注释）。
            //   ⛔ 别在任一处写 `ListX + RowW/2`：那是**抽屉**中心，行底/描边会被摆到行外（左右各 8px）。
            // 🔴 **2026-10-21（`A1700`）**：本行原来写的是「位置常量在类级（`GradX/GradW`）」——
            //   那一对**已按原版 prefab 逐件订正并拆成两档**（督军行 / 卡行），旧的单一 `GradFrac 0.606`
            //   已删除（见类级那段「A1700」注释里的订正记录）。

            for (int i = 0; i < RowVisible; i++)
            {
                float y = ListY + i * RowPitch;
                // 层序照原版兄弟序：**行底(最小) → 色条 → 描边**（每个九宫格自己那 9 块同队列、互不重叠）
                // 🔴 **2026-10-21（`A1700`）**：三件的矩形**分两档**取（`RowBgRect` / `RowGradRect`，
                //   判据 → 那四个常量上面的整段注释）。⚠️ **`MoveRow` 读的是同一对函数**，别在任一处写死。
                // 🔴 **2026-10-21（`A1720`）：档位从「槽号」改成「这一项是不是督军」** ——
                //   槽 `i` 装的是 `shown[firstRow+i]`，**只有槽 0 在 `firstRow == 0` 时**才装督军
                //   ⇒ 槽 0 **建两套件**：督军档 `row_0/row_g0/row_b0` ＋ 卡行档 `row_0c/row_g0c/row_b0c`，
                //   由 `RefreshDeckList` 按「这一行装的是不是督军」二选一开（判据 → `RowBgRect` 那段）。
                bool hero = (i == 0);
                float bgx, bgw, gx, gw;
                RowBgRect(hero, out bgx, out bgw);
                RowGradRect(hero, out gx, out gw);
                _deckRowBg.Add(NineSlice("row_" + i, rowTex, new Vector4(150f, 0f, 150f, 0f),
                                         bgx, y, bgw, RowH, QRowBg));
                _deckRowGrad.Add(Img("row_g" + i, gradTex, gx, y, gw, RowH, QGrad));
                _deckRowBorder.Add(NineSlice("row_b" + i, borderTex, new Vector4(5f, 5f, 5f, 5f),
                                             bgx, y, bgw, RowH, QBorder));
                // 🔴 **`A1727`：把这三件的每一块 quad 登记进「要裁」那张表** —— ⚠️ **必须在它们
                //   还被摆在本行的基准位、且【一次都没裁过】的时候登记**（`R0` / `Uv0` 就是那一刻的快照，
                //   理由 → `RowQuad` 那段；抓晚了 = 拿「上一刀裁剩的」当基准 ⇒ 每刷新一次再裁一刀）。
                int pieceKind = hero ? PieceHero : PieceCard;
                RegisterRowPiece(i, pieceKind, _deckRowBg[i]);
                RegisterRowPiece(i, pieceKind, _deckRowBorder[i]);
                RegisterRowQuad(i, pieceKind, _deckRowGrad[i]);
                if (hero)
                {
                    // 槽 0 的**卡行档**那一套（原版 `Deck Selector Card Info button` 的几何）。
                    // ⚠️ 它**不参与 `MoveRow` 的搬动**：几何由 `ClipRowToBand` 按当前 y 现算
                    //   （`MenuDraw.Local(parent, …)` 会减掉父件自己的位置 ⇒ 根停在哪都不影响落点）。
                    float cbgx, cbgw, cgx, cgw;
                    RowBgRect(false, out cbgx, out cbgw);
                    RowGradRect(false, out cgx, out cgw);
                    _row0cBg = NineSlice("row_0c", rowTex, new Vector4(150f, 0f, 150f, 0f),
                                         cbgx, y, cbgw, RowH, QRowBg);
                    _row0cGrad = Img("row_g0c", gradTex, cgx, y, cgw, RowH, QGrad);
                    _row0cBorder = NineSlice("row_b0c", borderTex, new Vector4(5f, 5f, 5f, 5f),
                                             cbgx, y, cbgw, RowH, QBorder);
                    RegisterRowPiece(0, PieceCard, _row0cBg);
                    RegisterRowPiece(0, PieceCard, _row0cBorder);
                    RegisterRowQuad(0, PieceCard, _row0cGrad);
                }
                _deckRowCost.Add(Img("row_c" + i, costTex, RowCostX, y + (RowH - RowCostS) * 0.5f, RowCostS, RowCostS, QRow));
                RegisterRowQuad(i, PieceShared, _deckRowCost[i]);
                // 🔴 **2026-10-20 删件**：这里原来还有一颗
                //   `_deckRowCntIc.Add(Img("row_k" + i, cntTex, 258f, y + 8f, 40f, RowH - 16f, QRow));`
                //   —— 拿 `40K_main_deck_card_counter` 当底、画在行右侧那块方框。
                //   **原版卡组行里根本没有这个节点**（原版行的子件只有 `Background` / `Rarity Gradient` /
                //   `Background Border` / `Cost Image` / `banned Icon` / `Text fill`(Card Name, Count)），
                //   实拍上它就是**一块纯色空方块** ⇒ 删掉，份数改由下面那颗 `row_x*` 印。
                var rowNmLb = Txt("row_n" + i, "", RowNameX, y, RowNameW, RowH, 1, Ink, QText);
                // 🔴 **2026-10-10（A1212 · 块 1 #41）**：接上 `m_enableAutoSizing`（四格 = **2 / 38 / 27.69 / 0**）。
                //   判据 = `R6` §2·F #41（`…/Sidebar/Deck Details/Deck List drawer/…/Background/Text fill/Card Name`）。
                //   ⚠️ `base = 27.69` 是原版原文（**不是笔误**，原版那两颗 `Card Name` / `cardsin deck` 都是它）。
                //   ⚠️ 折行 = `0` ⇒ 还原（同 `hdr_back_t`）。
                if (rowNmLb != null)
                {
                    // 🔴 **2026-10-20（行名左对齐）**：`SetAlignLeft()` 必须排在
                    //   **`SetAutoFitBox` 之前** —— 它自己的 doc 写着「要在『定版面』那三条之前调」，
                    //   排在之后只会给 `AlignAfterLayoutCount` 多记一笔（`A546②` 会红）。
                    //   判据（对齐档）= 原版 prefab `Card Name …… 对齐=Left/Capline`
                    //   （出处同 `RowNameX` 那段 doc）。
                    //   ⚠️ **光调它还不够 —— 它不挪位置**：`Label.RefreshBounds` 恒把文字块
                    //   **居中**摆在锚点上（与 `alignment` 无关），真正把左沿挪到位的**另有一口**
                    //   = `AlignLeftOn`，落点在 `RefreshDeckList`（`SetText` 之后）。
                    //   两条是**一对**，⛔ 别只做一条（只调对齐 = 不动；只挪位置 = 多行时每行仍居中）。
                    rowNmLb.SetAlignLeft();
                    rowNmLb.SetAutoFitBox(U(RowNameW), U(RowH), 2f, 38f, 27.69f);
                    rowNmLb.SetWrappingMode(0);
                }
                _deckRowName.Add(rowNmLb);
                // 🔴 **2026-10-21（`A1735`）**：字框 = 原版那颗 TMP 的 **48.82 × 47.88**
                //   （⛔ **不是**费用图的 `RowCostS 61.7`）—— 判据 / 为什么改 → `RowCostTxW` 那段 doc。
                //   位置仍**对齐费用图的中心**（原版四边等距内缩 ⇒ 框心 = 图心；`MoveRow` 那句
                //   `MoveLabel` 每刷新也把框心搬回图心，「两个口同一条」）。
                float costCx = RowCostX + RowCostS * 0.5f, costCy = y + RowH * 0.5f;
                var rowCntLb = Txt("row_cnt" + i, "",
                                   costCx - RowCostTxW * 0.5f, costCy - RowCostTxH * 0.5f,
                                   RowCostTxW, RowCostTxH, 1, Ink, QText);
                // 🔴 **2026-10-10（A1212 · 块 1 #42）**：接上 `m_enableAutoSizing`（四格 = **18 / 50 / 32 / 1**）。
                //   判据 = `R6` §2·F #42（同上 `…/Background/Cost Image/Cost`）。
                //   🆕 **2026-10-21（`A1735`）逐字复读那颗 TMP**：`字号=50.0` · `基准=32.0` · `auto[18.0~50.0]`
                //   ⇒ 下面那三个数（18 / 50 / 32）与实读**逐位吻合**，本批没动它们。
                //   ⚠️ 折行 = `1` ⇒ 与 `SetAutoFitBox` 内部那一档同档，不必还原。
                //   ⚠️ 变的只是**框**（61.7 → 原版 48.82 × 47.88），不是字号的上下限。
                if (rowCntLb != null)
                {
                    // 🔴 **2026-10-14（第二十九会话 · 账 `A1927` + `A1024` ③）—— 这一颗换成【原版那份字体】。**
                    //   判据：原版这颗 `Cost` 的 `m_fontAsset` = **`Pragati-Regular SDF`**
                    //   （只读代理扫真包 `fonts_assets_all.bundle` 反查 `pid 3485036404935369831` 得到）；
                    //   而它停在 `m_fontSize = 50 = m_fontSizeMax` 的**原因就是这个字体**：
                    //   `Pragati` 行盒 **0.9474 em** ⇒ `50 × 0.9474 = 47.37 ≤ 框 47.88` ⇒ **装得下、不缩**；
                    //   我们过去用 `NotoSerifCJK`（行盒 **1.437 em**）⇒ `50 × 1.437 = 71.85 > 47.88` ⇒
                    //   **必然被自适应压下去**（实测只到 **3.05**，而断言要它停在上限）。
                    //   ⚠️ 这一颗是**纯数字**（0–9）⇒ **不会触发 `ActualFace` 的汉字回退**，换面是安全的。
                    //   ⚠️ ⛔ **只换面、不动下面那三个数**（18 / 50 / 32 是与原版实读逐位吻合的）。
                    rowCntLb.SetFace(TmpFont.Face.LatinCardBody);
                    rowCntLb.SetAutoFitBox(U(RowCostTxW), U(RowCostTxH), 18f, 50f, 32f);
                }
                _deckRowCostText.Add(rowCntLb);
                // 🆕 **2026-10-20**：行右缘那颗**份数**（原版 TMP 名 `Count`）——
                //   判据（三条，逐条）：① 文本 = `"x" + n`（原版 `UICardInfoItem__SetCount.c` 的
                //   `String.Format(字面量, n)`，字面量 = **`x{0}`**，小写 x **无空格**）；
                //   ② **字号 32 · 字色 `(0.9333,0.9333,0.9333,1)` · 自适应【开】`min 2 / max 32 / base 36`**；
                //   ③ 框 = **50 宽、整行高**，**右沿 = `RowCountRight` = 317.48**（= 行右缘 325.4 − 7.92；
                //      判据 → `RowCountRightInset` 那段）⇒ 框 x `267.48..317.48`。
                //   ⚠️ **`n ≤ 1` 时整颗隐藏**（原版同函数那句 `gameObject.SetActive(1 < count)`）
                //      —— 落点在 `RefreshDeckList`（**先激活再 `SetText`**，理由写在那里）。
                //   🆕 **2026-10-11（`A1528` 第二轮）：位置那一半也收口了。**
                //      框的右沿由 `RowCountRight` 定，**墨迹右缘**再由 `RefreshDeckList` 里那句
                //      `AlignRightOn(RowCountRight)` 钉死（原版 `Count` 的 `m_HorizontalAlignment = 4(Right)`）。
                //      🔴 **「对齐档」与「挪位置」在此只能做后半条**：`Battle/Label.cs` **只有
                //      `SetAlignLeft()`、没有 `SetAlignRight()`**（那两条 `SetAlign*` 只管「真折行时每行
                //      在块内怎么排」，而本颗**单行不折行** ⇒ 那一档在画面上是空转）⇒ 真正挪位置的是
                //      `AlignRightOn`。⚠️ **这一处与本仓其它「两个口一对」的件不同，如实标注**；
                //      ⚠️ 也别为了对称去动 `Battle/Label.cs`（共用件，不在本活范围内）。
                //   🔴 **2026-10-11（`A1528`，铁律 5 订正）**：上面③原来抄的是 `(1,1,1,1)` + 字色白，
                //      **那是【另一族】的数**。裁定人 = 代理 `Z`（按 RT pid 认人，15 个同名 TMP 分 3 族）：
                //      · **本窗左栏这一份 = `Deck Selector Card Info button` 族**（`Count` RT
                //        **`-2738939269725491420`**，父 `Text fill`）⇒ `m_fontColor = (0.9333333969116211,…)`
                //        · `m_enableAutoSizing = 1` · `m_fontSizeMin = 2.0` · `m_fontSizeMax = 32.0` ·
                //        `m_fontSizeBase = 36.0` · `m_fontSize = 32.0` · `H=4(Right)` / `V=4096(Midline)`。
                //      · 甲那份 `(1,1,1,1)` + `anchor(1,0)-(1,1)` + `sizeDelta.x=50` **整条来自
                //        `Deck Selector Hero Card Info button`（督军行）** —— 那一颗原版**永远看不到**
                //        （`UICardInfoItem__SetCount.c:41` 的 `SetActive(1 < count)`，督军恒 1 张）。
                //      ⇒ 取 Card Info 那套对**全部可见情形**成立（铁律 4：按用途取，⛔ 不投票）。
                //      出处 → `资料/普查产出_第十七会话/Z_两处悬案.md` §A；
                //      裁定留痕 → `资料/历史/A表已收口_第十七会话.md` 的 `A1528` 那一行。
                //   🆕 **2026-10-11（`A1528` 第二轮）：原来那两条「没动的」都收了**（第二轮量完判据后一起改）：
                //      · **框宽 `50` 的出处确实是 Hero 族** —— Card Info 族那颗是**内容自适应**
                //        （`LayoutElement.m_MinWidth = **22.87**` · `m_PreferredWidth = 0` ·
                //        `m_FlexibleWidth = **−1**`）⇒ 本工程留 `50` 只为给 `SetAutoFitBox` 一个够宽的框，
                //        **它不再是几何真值**（真值是右沿，见 `RowCountRightInset`）；
                //      · **位置/对齐**：框右沿改 `RowCountRight`（**317.48** = 我们行右缘 325.4 − 7.917）
                //        ＋`RefreshDeckList` 里 `AlignRightOn` 钉墨迹右缘。
                //        ⚠️ 参考图核过的是**行相对**那一半：图上墨迹右缘离**行底板右缘**约
                //        **7.3 画布 px**（原版规则是 7.917）⇒ 一致；⚠️ **绝对值不一样**
                //        （prefab + 参考图都指向 **309.48/309.7**，我们这里是 317.48）——
                //        差的 8px **全部来自「我们的行底板比原版宽 8px」那条另账**（见 `RowCountRightInset`）。
                //        ⚠️ 旧写法（50 宽贴行右缘 + 框内居中）对**两字**文本**恰好**落在 ≈317.9
                //        ⇒ 本轮这一改**在今天的卡池上只挪 0.4px**（如实登记）；它的价值在**结构**：
                //        右沿**不再随文字宽度漂**（旧的居中写法遇到更长的串会往右跑出框）。
                var rowXLb = TxtPx("row_x" + i, "", RowCountX, y, RowCountW, RowH,
                                   RowCountFontPx, RowCountTint, QText);
                // 🔴 **2026-10-11（`A1528`）**：接上 `m_enableAutoSizing`（四格 = **2 / 32 / 36 / 1**）。
                //   判据 = 上面那段（`Deck Selector Card Info button` 族的 `Count` 实读）。
                //   ⚠️ 常见情况（`"x" + n`，`n ≤ 3`）渲染下来仍是 **32px** —— 只有框真挤了才缩。
                //   ⚠️ 折行那一档（`m_TextWrappingMode`）**只覆盖 Card Info 这一族**的读数、本轮没单独设：
                //     `SetAutoFitBox` 内部会把折行置成 `Normal`（`Battle/Label.cs` 的 `SetWrapWidth`）。
                if (rowXLb != null) rowXLb.SetAutoFitBox(U(RowCountW), U(RowH), 2f, 32f, 36f);
                _deckRowCountText.Add(rowXLb);
            }

            // Empty Warning（原版 inactive，空卡组时才显示）
            // ⚠️ **虚线底 `40k_deck_cardlist_doted_bg` 是【空槽行】用的**（原版 67×54 灰 0.44 + `-- Warlord --`）
            //    —— 我们暂时仍拿它当「空卡组提示」的底（**这一处与原版不同**，已记在正本「查不到的」里）。
            _emptyWarn = Img("empty_warn", "40k_deck_cardlist_doted_bg", 20.3f, 445f, 295.3f, 186f, QGrad);
            // 🔴 **2026-10-18（双语线 · 波 1 · P1）**：空卡组那句提示走**原版 prefab 的键**
            //   `MenuDeck/HUD/DragCardsTip` —— 判据 = 节点 `Deck Editing Menu / Content Area / Sidebar /
            //   Deck Details / Deck List drawer / Empty Warning`（本批按 pid 亲读
            //   `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-1205960227040661724.json`：
            //   `mTerm = "MenuDeck/HUD/DragCardsTip"`；**同 GO 的 TMP `m_text` 是葡语占位串**
            //   `Arraste as cartas aqui para criar seu deck` ⇒ 原版英文在远端 I2 表、本地拿不到 ⇒ EN 得自拟）。
            //   ✅ **2026-10-18 收口（铁律 5）**：这一行原来写「⚠️ 该键**波 0 未进表** ⇒ 走 `TermOr` 闸门
            //      （今天仍印中文那串，补键后自动生效）」—— **已过期**：键 `MenuDeck/HUD/DragCardsTip`
            //      **已在表**（`Core/Loc.cs` 实读：`("将牌拖到这里以创建您的卡组", "Drag cards here")` —— 该中文列 2026-10-14 已换成官方 zh-CN 表的值），闸门**已删除**
            //      （静默兜底，与「不许静默失败」冲突）⇒ 现在是**裸 `Loc.T`**。
            var warnLabel = Txt("empty_warn_l", Loc.T("MenuDeck/HUD/DragCardsTip"),
                                20.3f, 500f, 295.3f, 60f, 2,
                                new Color(1f, 1f, 1f, 0.65f), QText);
            if (warnLabel != null) _emptyWarnGo = warnLabel.gameObject;
        }

        // ============================================================ 🔴 2026-10-21（`A1727`）
        // **卡组行的视口裁切** —— 等效原版 `Deck Details > Deck List drawer/Scroll View/Viewport`
        // 上那颗 `RectMask2D`（框 = `DeckListBand`，判据在那条常量上）。
        //
        // **为什么非要有它**：`RowVisible = 12` ⇒ 12 槽 = `11×57.7 + 55.7` = **690.4** > 视口 **644.1**
        //   ⇒ 顶/底那一行**一定会越出视口**（原版靠掩码把它切掉 —— 卷到 0 时第 12 行只露 **9.4px**）。
        //
        // **做法**（与 `RefreshFilters` 那条已跑通的路**同一套机制**）：<see cref="ClipQuadToBand"/>
        //   （截矩形 + 截 uv，整块在带外只 `SetActive(false)`、**不删节点**）
        //   ＋ `MenuDraw.ClipText`（截文字网格，并给那颗标签挂上「重排后自动重裁」的守卫）。
        //   ⚠️ `ClipQuadToBand` 与那一族的 `ClipCellToBand` **逐行同源、只有读口那一半不同**
        //   （本族建/搬走**设计帧**）—— 理由写在它的 doc 里，⛔ 别图省事改成转调。
        //
        // ⚠️ **与抽屉那条路有一处必须做对的不同**：抽屉那些格子**每刷新一次就重建**，所以
        //   「拿当时的矩形当基准」天然安全；行这一族是**建一次 + 搬**（12 槽 × 8 件，
        //   重建太贵）⇒ 基准必须**在还没被裁过的时候**抓下来（`RowQuad.R0/Uv0`），每次刷新
        //   **先摆回基准、再裁一刀**。少了「摆回基准」这一步 = 每刷新一次就在上一刀上再裁一刀
        //   （**越裁越小、静默**，与 `MenuDraw.ApplySoftEdges` 的 `uv0` 那条坑同族）。

        /// <summary>把一棵子树里所有 quad 登记进「要裁」那张表（基准 = **这一刻**的矩形与 uv）。</summary>
        void RegisterRowPiece(int slot, int kind, GameObject root)
        {
            if (root == null) return;
            foreach (var q in root.GetComponentsInChildren<ImageQuad>(true)) RegisterRowQuad(slot, kind, q);
        }

        /// <summary>登记一颗要裁的 quad。⛔ **只能在它还没被裁过、且摆在本行基准位的时候调**（见上面那段）。</summary>
        void RegisterRowQuad(int slot, int kind, ImageQuad q)
        {
            if (q == null || slot < 0 || slot >= RowVisible) return;
            if (_rowPieces[slot] == null) _rowPieces[slot] = new List<RowQuad>();
            _rowPieces[slot].Add(new RowQuad { Q = q, R0 = MenuDraw.QuadRectPx(q), Uv0 = q.UvRect, Kind = kind });
        }

        /// <summary>把第 `slot` 行裁到视口内。
        /// <para>`hero` = 这一行装的是**督军**（决定开哪一套、裁哪一套）；
        /// `shared` = **与档无关的那几件（`PieceShared` = 费用圆）这一行该不该显示**
        /// （= `RefreshDeckList` 里那句 `on && !heroRow`，**与它 `SetOn(_deckRowCost[i], …)` 同一个值**）。</para>
        /// <para>三档各管自己那一套：`PieceHero` 只在 `hero` 时裁、`PieceCard` 只在 `!hero` 时裁、
        /// `PieceShared` 只在 `shared` 时裁。**没被选中的那一套一个字都不动** —— 它已经被
        /// `RefreshDeckList` 里那几句 `SetOn` 关掉了（去动它 = 把隐藏的那一套点亮）。</para>
        /// <para>🔴 **2026-10-14（`DIAG_deck七条红.md` ⑥ · 真缺陷）**：原来 `PieceShared` **恒被处理**
        /// （`Kind` 只挡了 `PieceHero` / `PieceCard` 两条）⇒ 状态刚被 `SetOn(_deckRowCost[i], false)`
        /// 关掉的费用圆，在同一趟循环末被 ① 那句 `SetActive(true)` **又点亮了**（静默：
        /// 只有「督军行该没有圆」这一处现形，而 `row_cnt*` 是 `Label`、不在本表 ⇒ 没被点亮）。
        /// ⇒ 补 `shared` 这一档，让三档**同一条口径**。</para></summary>
        void ClipRowToBand(int slot, float y, bool hero, bool shared)
        {
            var list = _rowPieces[slot];
            if (list == null) return;
            float dy = y - (ListY + slot * RowPitch);        // 与建的时候那一位之差
            for (int k = 0; k < list.Count; k++)
            {
                var it = list[k];
                var q = it.Q;
                if (q == null) continue;
                if ((it.Kind == PieceHero && !hero) || (it.Kind == PieceCard && hero)) continue;
                // 🔴 **2026-10-14（红 6）**：`PieceShared`（费用圆）也走**同一条口径** ——
                //   「这一行这一颗**该不该显示**」由调用点传进来（`shared`），**⛔ 不是**「看到
                //   `!activeSelf` 就点亮」（那是把「**状态关掉**」误当成「**上一轮被裁出去**」）。
                //   ⚠️ **这条约定对将来加进 `_rowPieces` 的每一颗都成立**：注册进来 = 这一颗的
                //     可见性归本函数管 ⇒ 调用点必须把它那一行的「该不该显示」一并传进来。
                if (it.Kind == PieceShared && !shared) continue;
                var r = new PxRect(it.R0.x1, it.R0.y1 + dy, it.R0.x2, it.R0.y2 + dy);
                // ① 先摆回【未裁切】的基准 —— **位置 / 高 / 宽高比 / uv 四件一起**。
                //    ⚠️ 只摆一半（比如只摆回 uv）会让几何停在上一刀那一块上 ⇒ 越裁越小、且自检不响。
                //    位置走 `Local(parent, …)`（会减掉父件自己的设计位置）⇒ 九宫格根被 `MoveRow` 搬到哪都不影响落点。
                //    ⚠️ `activeSelf == false` 也要重摆：它可能是**上一轮被裁出去的**（整块在带外），
                //       这一轮又回来了 —— 不重摆就会一直不画（静默）。
                if (!q.gameObject.activeSelf || !NearRect(MenuDraw.QuadRectPx(q), r)
                    || !SameUv(q.UvRect, it.Uv0))
                {
                    var lp = MenuDraw.Local(q.transform.parent, r.x1, r.y1, r.x2, r.y2);
                    lp.z = q.transform.localPosition.z;      // z 不动（同队列里还靠它排序）
                    q.transform.localPosition = lp;
                    q.SetWorldHeight(LayoutSpace.Px(r.H));
                    q.SetAspect(r.W / Mathf.Max(1e-6f, r.H));
                    q.SetUvRect(it.Uv0);
                    q.gameObject.SetActive(true);
                }
                // ② 再截到带口内（整块在带外 ⇒ 它自己 `SetActive(false)`，**不删节点**：自检按名字照样找得到）
                ClipQuadToBand(q, DeckListBand);
            }
        }

        /// <summary>把一颗 quad 截到带口内（`A1727`）。判据/算式与 <see cref="ClipCellToBand"/> **逐行同源**，
        /// **只有「读回来」那一半不同**：这里读 <see cref="MenuDraw.QuadRectPx"/>（**设计帧** —— 与写进去的
        /// `MenuDraw.Local` 是**互逆**的一对），⛔ 不是 `PxOfWorld`（写死 **108 px/世界单位**、
        /// **只在 16:9 与设计帧重合** —— `LayoutSpace.ToPixel` 那条 doc 记着这件事）。
        /// <para>为什么不能直接转调 `ClipCellToBand`：那一份的读口是 108 帧，而本行这一族**建与搬**
        /// 全走设计帧（`MenuDraw.Nine` → `Local`）⇒ 非 16:9 下「读回来的框」与「传进去的框」
        /// **不是同一个东西**，裁出来的边界会整体偏一个 `VisibleWidth / 1920` 倍（静默、只在别的宽高比下现形）。</para>
        /// <para>整块在带外 ⇒ **只 `SetActive(false)`、不删节点**（同 `MenuDraw.ClipNineChildren` 的口径：
        /// 树形/块数不随滚动变，自检按名字照样找得到）。</para></summary>
        static void ClipQuadToBand(ImageQuad q, PxRect band)
        {
            if (q == null) return;
            var qr = MenuDraw.QuadRectPx(q);
            PxRect cr;
            if (!MenuDraw.ClipRect(qr, band, out cr)) { q.gameObject.SetActive(false); return; }
            if (MenuDraw.SameRect(cr, qr)) return;                       // 整块在带内 ⇒ 一个字节都不动
            var uv = q.UvRect;
            float w = Mathf.Max(1e-6f, qr.W), h = Mathf.Max(1e-6f, qr.H);
            var lp = MenuDraw.Local(q.transform.parent, cr.x1, cr.y1, cr.x2, cr.y2);
            lp.z = q.transform.localPosition.z;                          // z 不动（同队列里还靠它排序）
            q.transform.localPosition = lp;
            q.SetWorldHeight(LayoutSpace.Px(cr.H));
            q.SetAspect(cr.W / Mathf.Max(1e-6f, cr.H));
            // ⚠️ **截 uv 是必须的**（只截几何不截 uv 会把图**压扁** —— 同 `ImageQuad.SetUvRect` 那句）；
            //    uv 的 y **自下而上**、`PxRect` 自上而下 ⇒ 纵向翻一次（与 `ClipCellToBand` 同一条规矩）。
            q.SetUvRect(new Rect(uv.x + uv.width * (cr.x1 - qr.x1) / w,
                                 uv.y + uv.height * (qr.y2 - cr.y2) / h,
                                 uv.width * cr.W / w,
                                 uv.height * cr.H / h));
        }

        /// <summary>整张 uv 比一下（`Rect` 自带的那个 `==` 是逐字段的近似比，够用）。</summary>
        static bool SameUv(Rect a, Rect b) { return a == b; }

        /// <summary>两个矩形「在同一处」吗（容差 0.05px —— `QuadRectPx` 是「世界 → px」反推，
        /// 与写进去的那组数隔着一次浮点往返，逐字段 `==` 会**假阴**；同 `MenuDraw.SameRectNear` 那条）。</summary>
        static bool NearRect(PxRect a, PxRect b)
        {
            return Mathf.Abs(a.x1 - b.x1) < 0.05f && Mathf.Abs(a.y1 - b.y1) < 0.05f
                && Mathf.Abs(a.x2 - b.x2) < 0.05f && Mathf.Abs(a.y2 - b.y2) < 0.05f;
        }

        /// <summary>把第 `slot` 行那三颗字裁到视口内（`A1727`）。**逐颗判 `activeInHierarchy`** ——
        /// 未激活的 TMP **不能裁**：`MenuDraw.ClipTmpMesh` 拿不到渲染网格时会记
        /// `TextClipUploadSkipped` 并**出声**，那条信号是留给「本该在渲染、却没有网格」那一档的，
        /// 行里这些「按状态关掉的」标签不是那一档（同 `CardView.ClipTextMesh` 那道闸的理由）。</summary>
        void ClipRowLabels(int slot)
        {
            ClipOneLabel(_deckRowName.Count > slot ? _deckRowName[slot] : null);
            ClipOneLabel(_deckRowCostText.Count > slot ? _deckRowCostText[slot] : null);
            ClipOneLabel(_deckRowCountText.Count > slot ? _deckRowCountText[slot] : null);
        }

        /// <summary>裁一颗标签（并挂上守卫 —— 之后每次 `SetText` / `Align*On` 都会按同一个框重裁）。
        /// `MenuDraw.ClipText` 那一刀**幂等**（硬边：顶点夹进框、uv 按同一仿射走；已经在框内的四个角
        /// 一个都不动 ⇒ 重复调是空转），所以每刷新调一次是安全的。</summary>
        void ClipOneLabel(Label lb)
        {
            if (lb == null || !lb.gameObject.activeInHierarchy) return;
            MenuDraw.ClipText(lb, DeckListBand, Vector2.zero);
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
            //       `Root` 出厂在原点（判据 → `Editor/DeckScene.cs` 的 `TestLayout` 里那条「`Root` 在原点」不变式），且 `U(px)` 与
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
            // 🆕 2026-10-17（D31）：**抽屉底板** —— 原版**有**、我们原来**没画**。
            //   判据：`Background` 的 `Image.m_Sprite = null`（纯色）+ `m_Color = (0.1226, 0.1226, 0.1226, 1)`。
            //   ⇒ 等价物 = 公共的 **1×1 白图 + tint**（同 `Battle/BattleLogPanel.cs` 那颗 `LogBG` 的写法；
            //     `CardArt.Solid()` 是那份共用件，别再自己 new 一张）。
            // ⚠️ 它**必须进 `_infoOnly`** —— 不然切到 Cards / Cosmetics 页签时这块 234×294.6 的板子会
            //   **一直挂着**（同族坑：筛选栏底板那次「建起来了却没跟着开关隐藏」）。
            var bg = Img("curve_bg", CardArt.Solid(), CurveBgX, CurveBgY, CurveBgW, CurveBgH, QCurveBg);
            if (bg != null) bg.SetTint(CurveBgTint);
            _infoOnly.Add(bg != null ? bg.gameObject : null);
            for (int c = 0; c <= 8; c++)
            {
                float y = CurveY + c * CurveStep;
                // ⚠️ 两列数字一律走 `TxtPx`（**带原版字号**）—— `Txt(..., scale: 1)` 那一档是 7px 大写高，
                //    在 1.48 后的行里会小一大截（原来就是这么小的，见 `CurveFontPx` 那条注释）。
                var lb = TxtPx("curve_l" + c, CurveCostText(c), CurveLbX, y, CurveLbW, CurveRowH, CurveFontPx, CurveLbTint, QText);
                var bar = Img("curve_b" + c, barBg, CurveBarX, y + CurveBarDy, CurveBarW, CurveBarH, QPanel);
                var fill = Img("curve_f" + c, barFill, CurveBarX, y + CurveFillDy, 1f, CurveFillH, QRow);
                // 🔴 **2026-10-11（A1497 ②）**：填充图 **`40k_CardAmount_bar_fill` 是【白色蒙版】**
                //   （实拍两张图量的：bg 是深灰 (40,40,40)、fill 是白 (220,220,220)）
                //   ⇒ **必须靠运行时 tint 才是金色**。我们原来**没上 tint** ⇒ 屏上是一排白药丸。
                //   金色值 = `Core/CostCurveDrawer.cs` 的 `FillTint`（原版 `Image.m_Color` 实读）。
                if (fill != null) fill.SetTint(CostCurveDrawer.FillTint);
                _curveFillX0.Add(fill != null ? fill.transform.localPosition.x : 0f);
                var num = TxtPx("curve_n" + c, "0", CurveNumX, y, CurveNumW, CurveRowH, CurveFontPx, CurveNumTint, QText);
                _curveBar.Add(bar); _curveFill.Add(fill); _curveLabel.Add(lb); _curveLabel.Add(num);
                _infoOnly.Add(bar != null ? bar.gameObject : null);
                _infoOnly.Add(fill != null ? fill.gameObject : null);
                _infoOnly.Add(lb != null ? lb.gameObject : null);
                _infoOnly.Add(num != null ? num.gameObject : null);
            }
        }

        // ------------------------------------------------------------ 卡组类型计数（Deck info 页签）

        /// <summary>🆕 **`A1514`（2026-10-11）**：建原版那颗 **`Deck Counter Drawer`**
        /// （= `DeckTypeCounterDrawer`：两行「词条: 数字」，策略在上、部队在下）。
        /// 几何 / 文案 / 数什么 / 什么时候露 —— **全在类级 `Tc*` 常量那一段**（逐条带出处）。
        /// <para>⚠️ 两件都进 **`_infoOnly`** —— 原版 `DeckEditingPanel__ToggleDeckList.c` 明写
        /// Card 页把 `deckCounterDrawer(0x38)` 设 **0**（⛔ 不是 `_cardsInfoOnly` 那张两页表）。</para>
        /// <para>⚠️ **「左对齐」是两个口一对**（`row_n*` 那一段同款）：`SetAlignLeft()` 只管
        /// 「真折行时每行在块内怎么排」（在**这里**调，排在「定版面」那三条之前），
        /// 把**整块**挪到原版左沿的是 `AlignLeftOn`（在 `RefreshTypeCounter` 里、排在 `SetText` **之后**）。</para></summary>
        void BuildTypeCounter()
        {
            _tcStratagems = TxtPx("typecnt_strat", "", TcX, TcStratY, TcW, TcH, TcFontPx, Color.white, QText);
            _tcTroops = TxtPx("typecnt_troops", "", TcX, TcTroopY, TcW, TcH, TcFontPx, Color.white, QText);
            // 原版两颗都是 `H=1(Left)` / `V=512(Middle)`；`Label` 出厂是 **Center/Middle**
            // ⇒ 左对齐得自己设（只管「真折行时每行怎么排」，移块那半在 `AlignLeftOn`）。
            if (_tcStratagems != null) _tcStratagems.SetAlignLeft();
            if (_tcTroops != null) _tcTroops.SetAlignLeft();
            // 原版 `m_TextWrappingMode = 1(Normal)`；`Label.BuildTmp` 出厂是 **NoWrap** ⇒ 还原成原版那一档。
            if (_tcStratagems != null) _tcStratagems.SetWrappingMode(1);
            if (_tcTroops != null) _tcTroops.SetWrappingMode(1);
            // ⚠️ 原版 **`m_enableAutoSizing = 0`** ⇒ **不调** `SetAutoFitBox`（那会把折行也一起改）。
            _infoOnly.Add(_tcStratagems != null ? _tcStratagems.gameObject : null);
            _infoOnly.Add(_tcTroops != null ? _tcTroops.gameObject : null);
        }

        /// <summary>🆕 **`A1514`**：把两个数写进那两颗 TMP（原版 `DeckTypeCounterDrawer.Initialize`）。
        /// 文案 = **`string.Format("{0}: {1}", 词条译名, 计数)`**（格式串与词条键都是反编译里
        /// 现查出来的字面量，见 `TcFormat` / `TcTerm*` 那几行）。
        /// <para>⚠️ **只在它真露着的时候写**：未激活的 TMP 上量不出尺寸（`Battle/Label.cs` 那条已知坑，
        /// 同 `RefreshDeckList` 对 `row_n*` 的 `if (!on) continue;`）。三个调用点（`RefreshHeader`）
        /// 都排在 `RefreshDeckList` → `RefreshTabVisibility` **之后** ⇒ 这里读到的就是当前页的状态。</para></summary>
        void RefreshTypeCounter()
        {
            if (_tcTroops == null || !_tcTroops.gameObject.activeInHierarchy) return;   // Cosmetics 页：不写（见 doc）
            int troops = 0, stratagems = 0;
            // ⚠️ 数的是 **`CardIds` 的每一条**（含重复 = 一份拷贝算一张）—— 原版数的是
            //   `deck.cardLibrary`，那里面一份拷贝一条（`DeckListDrawer` 靠 `stackCopies` 折行正说明如此）。
            foreach (var id in State.Deck.CardIds)
            {
                var d = State.Find(id);
                if (d == null) continue;
                if (d.Type == "unit") troops++;          // 原版 `cardType == 0`（`CardTypeOptions.Minion`）。⚠️ 2026-10-27（批次 9 · `E7ENG` · 普查 `判据/cardType判据点普查.md` §3·5）：**两格就够，⛔ 别补 `defence`/`enchantment` 格子** —— 真值里那两种 `type` 也是 `cardType 20`（按原版口径本应计入 stratagems），但它们**结构上进不了 `CardIds`**：`defence` 走 `PlayerDeck.DefensiveId` 独立字段（`DeckRules.cs:299` 也拦住了），`enchantment` 是 `inventoryOptions = 10 NotInInventory`；`DeckInfoPopup.cs:1885` 也是同一句「防御卡不在 `CardIds` 里」⇒ 补那两个格子 = 永远走不到的死分支。
                else if (d.Type == "tactic") stratagems++;   // 原版 `cardType == 0x14`（`Tactic`）—— `defence`/`enchantment` 为什么不算见上一行（同一理由：进不了 `CardIds`，补格子 = 死代码）
            }
            SetTypeCounter(_tcTroops, TcTermTroops, troops);
            SetTypeCounter(_tcStratagems, TcTermStratagems, stratagems);
        }

        void SetTypeCounter(Label lb, string term, int n)
        {
            if (lb == null) return;
            lb.SetText(string.Format(TcFormat, Loc.T(term), n));
            // ⚠️ 左沿的落点**必须排在 `SetText` 之后**（它要现量 `WorldW`），且**每次刷新都要调**
            //   —— `SetText` 会重排、把 x 写回块心（同 `RefreshHeader` 里 `_deckNameText` 那一句）。
            lb.AlignLeftOn(LayoutSpace.FromPixel(TcX, 0f).x);
        }

        // ------------------------------------------------------------ Footer

        void BuildFooter()
        {
            _doneHl = Img("foot_hl", "FX_Square_UI_SDF", DoneHlX, DoneHlY, DoneHlW, DoneHlH, QDoneHl);
            // 🆕 A24：原版 `Sidebar/Footer/Done` 也是 `SpriteSwap`（同上，三颗同图）
            Hover("foot_done",
                  Img("foot_done", "UI_Button_Mulligan", DoneX, DoneY, DoneW, DoneH, QRow),
                  "UI_Button_Mulligan");
            // 🔴 **2026-10-17（D4）：文案走 `Loc.T`**（中文档 = 原版实拍的 **「完成」**）。
            //   词条键 = **原版 prefab 上那颗 `Localize` 的 `mTerm`**：
            //   `Deck Editing Menu/…/Sidebar/Footer/Done/Button Text` 上挂**两颗** `Localize`，
            //   第一颗的 `mTerm = **MenuDeck/MenuButtons/Done**`（第二颗是 `MenuLogin/Login/DoneButton`
            //   —— 登录页那颗共用的，⛔ 不是本窗这一颗）；那颗 TMP 的 `m_text = "Done"`·fs40。
            //   中文那一列 = **原版实拍**（`用户实拍_1017/卡组编辑界面参考.png` 左栏底部那颗金框钮）。
            //   断言 → `Editor/DeckScene.cs` 的 G1 节 ⑭。
            var footDoneTx = Txt("foot_done_t", Loc.T("MenuDeck/MenuButtons/Done"),
                DoneX, DoneY, DoneW, DoneH, 2, Ink, QText);
            // 🔴 **2026-10-10（A1212 · 块 1 #46）**：接上 `m_enableAutoSizing`（四格 = **10 / 40 / 12 / 0**）。
            //   判据 = `R6` §2·F #46（`…/Sidebar/Footer/Done/Button Text`）。
            //   ⚠️ 折行 = `0` ⇒ 还原（同 `hdr_back_t`）。
            if (footDoneTx != null)
            {
                // 🔴 **2026-10-17（`A1226` 表 B #46）：框宽 = 原版有效框 168.54（原来 188.5，宽 +19.96）。**
                //    判据 = 原版 `…/Footer/Done/Button Text`（伸锚跨度 0.92575 · `m_SizeDelta.x = −6`）
                //    ⇒ 父宽 188.5 × 0.92575 − 6 = **168.54**。
                // ⚠️ **框高【故意不跟】**（原版有效框高 43.91）：CJK 行盒 1.516em ⇒ 43.91 会把 40px 压到 28.96px
                //    （原版因 Pragati 行盒 37.9 < 43.91 停在 40）⇒ 暂留 `DoneH`，与 `A1024③` 一起改。
                footDoneTx.SetAutoFitBox(U(168.54f), U(DoneH), 10f, 40f, 12f);
                footDoneTx.SetWrappingMode(0);
            }
            // 🔴 2026-09-27（PA 普查）：原版 `Content Area/Sidebar/Footer/Image` 是 PA=1，贴图
            //   `40k_general_icon_card_amount` **64×64** 塞进 50×40 ⇒ 原版实绘 **40×40**（居中），我们 50 宽（**1.25×**）。
            Img("foot_ic", "40k_general_icon_card_amount", FootIcX, FootIcY, FootIcW, FootIcH, QBorder, true);
            _counterTxt = Txt("foot_cnt", "0/30", CntX, CntY, CntW, CntH, 2, Ink, QText);
            Btn_("foot_done", DoneX, DoneY, DoneW, DoneH);
            // 🔴 **2026-10-17（D35 删件）**：这里原来还有 `_verdict = Txt("foot_verdict", …)` ——
            //   页脚那行「合法 / <不合法原因>」判词**原版没有**（判据 → D35）。
            //   ⚠️ 「合法不合法」这件事**没有失去出口**：① `Done Highlight`（那颗灯，判据与保存闸同一个
            //     `Validate()`）；② 不合法时 `SaveAndSay()` 会弹原版的模态窗并把原因念出来（A364）。
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

            // 🔴 **2026-10-17（D43）：卡背抽屉那一行 `Army` 小标题** —— 原版**有**、我们原来**没画**
            //   （`RefreshCosmoFilters` 那条链只调了 `BuildCosmetics`、**没调 `BuildTitles`**）。
            //   判据（逐份实读 `bundle_menus_assets_all/MonoBehaviour/`，按 `m_text == "Army"` 找出来那一颗）：
            //     · `…/Cosmetic Display > Cosmetic FIlter > Filters > Army Filter/Title`
            //       = `HA **1**`(Left) · `VA **512**`(Middle) · `m_fontSize **32**` · **`m_fontSizeBase 32`**
            //       · 🔴 **`m_enableAutoSizing = 0`** —— 施工单写的「auto[18..72]」是**那两个残留字段**
            //         （`m_fontSizeMin 18` / `m_fontSizeMax 72` 在自适应关着时是**死值**，同
            //          `FilterPanelModel` 里那条「`min18/max72` 是不生效的残留值」）⇒ **不开自适应**。
            //     · 矩形（`menu_rect.py … "Cosmetic FIlter" --depth 5` 那棵树 + 三个同名 `Army` 标题的
            //       兄弟序）：`2.18,170.97 → 332.34,220.97` ⇒ 面板内 `x 0..331.7` · `y 15..65`
            //       = `(CosmoSpacing1, CosmoSpacing1 + TitleH)`，与 `Army Filter` 那 15px 间距**同一个数**。
            //   ⚠️ 挂 `CosmoFltParent`（抽屉容器）—— 整栏滑出去/切页签的显隐**由 `ApplyDrawerSlide` 一处管**
            //     （⛔ 别再自己 `SetActive`，那正是「建起来了却没跟着开关隐藏」那一族的坑）。
            //   ⚠️ 它**不进 `_cosmoFltObjs`** —— 那份是「每次刷新重建」的格子，这个是常驻件
            //     （`ClearCosmoFlt()` 会 Destroy 那一批；标题跟着走就会在每次筛选后被删掉）。
            var cosmoArmyR = new PxRect(FltX, FltY + FilterPanelModel.CosmoSpacing1,
                                        FltX + FltW, FltY + FilterPanelModel.CosmoSpacing1 + FilterPanelModel.TitleH);
            // 🔴 **2026-10-18（A891 的续）：那一行字走 `Loc.T`** —— 词条键 = 原版 prefab 上那颗
            //   `Localize.mTerm` 的**原文** `MenuDeck/Filters/Army`（6 颗同键，节点名一律 `Title`；
            //   本窗这一棵 = `Deck Editing Menu > Content Area > Cosmetic Display > Cosmetic FIlter >
            //   Filters > Army Filter/Title`）。英文列 = 那颗 TMP 的 `m_text` 原文 `Army`；
            //   中文列 = 「军队」（源 `数据/本地化/i18n/zh_CN.csv:11`，⚠️ 是我们译的）。
            //   ⛔ 节点名 `cosmoflt_title` 不动（`Editor/DeckScene` 里按名找 `cosmoflt_title` 那一处）。
            _cosmoArmyTitle = Txt("cosmoflt_title", Loc.T("MenuDeck/Filters/Army"), cosmoArmyR.x1, cosmoArmyR.y1, cosmoArmyR.W, cosmoArmyR.H,
                                  1, Ink, QFltText, CosmoFltParent);
            if (_cosmoArmyTitle != null)
            {
                _cosmoArmyTitle.SetGlyphHeight(LayoutSpace.Px(FilterPanelModel.TitleFontPx));
                MenuDraw.AlignLeft(_cosmoArmyTitle, cosmoArmyR);   // HA=1（判据见上）
            }

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
        /// <summary>🆕 **2026-10-17（D43）**：卡背抽屉那一行 `Army` 小标题（原版 `Army Filter/Title`）。
        /// 常驻件（**不在 `_cosmoFltObjs` 里**）—— 显隐交给 `ApplyDrawerSlide`，见 `BuildFilters` 里那一段。</summary>
        Label _cosmoArmyTitle;
        readonly List<GameObject> _cosmoFltObjs = new List<GameObject>();
        readonly List<FilterPanelModel.Cell> _cosmoFltCells = new List<FilterPanelModel.Cell>();
        /// <summary>卡背抽屉里每一格的图示 quad（`Key` → quad）—— 格子**没进 `_named` 登记表**（见下面自检读数那一段），
        /// 所以「状态换图」那条断言得靠这一份读（`UiCosmoFilterCellTex`）。</summary>
        readonly Dictionary<string, ImageQuad> _cosmoFltQuads = new Dictionary<string, ImageQuad>();
        /// <summary>🆕 **2026-10-07（A62 #6）**：卡背抽屉每一格的**标签 `Label`**（`Key` → label，自检读换行模式用）。
        /// ⚠️ **与卡牌那栏的 `_fltCellLabels` 分开存**（两栏可同时存在、key 又都是 `$owned`，同 `_fltHoverBtns` 那条注释）。</summary>
        readonly Dictionary<string, Label> _cosmoFltLabels = new Dictionary<string, Label>();
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
        //   **本窗自己的派发不走 `PointerLayer`**（理由写在 `Build()` 末尾那三条），左抽屉的点击是**区域判断**
        //   （`HandlePointer` 里 `px.x < FltX + FltW` 那两处 + `HandleFilterClick` / `HandleCosmoFltClick`
        //   按 `_fltHit` / `_cosmoFltHit` 那两张**px 矩形表**判）⇒ 失效的落点是**那三处区域判断**
        //   （`PointerLayer` 那两条规则在这里都不适用：本窗**自己的派发**不走指针层，抽屉里的点击是区域判断。）
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
            if (!d.HasBasePos)
            {
                d.BasePos = d.Node.localPosition; d.HasBasePos = true;
                // 🔴 **2026-10-21（`A1177`）出声**：`BasePos` 必须在**容器还在原地**时抓
                //   （`NewDrawer` 建出来就是 `Vector3.zero`，第一次调本函数就在 `BuildFilters` 收尾）。
                //   抓到别处 ⇒ 抽屉里那一批「相对原地原点」的子件（`DrawerLocal`）与 `FltAbs` 那一族
                //   会**整段偏**，而且是静默的 ⇒ 这里说一句。
                if (d.BasePos != Vector3.zero)
                    Debug.LogWarning("[Deck] 抽屉 `" + d.Node.name + "` 的 `BasePos` 不是在零位抓到的（= "
                                     + d.BasePos + "）—— 抽屉族按 `DrawerLocal` 摆的子件会整段偏（A1177）");
            }
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
        /// ① 本窗的机制是**区域判断**（见本节开头那段：抽屉里的点击**不由 `PointerLayer` 派发**）
        ///    ⇒ 这一个布尔就是 `HandlePointer` / `UiClickPx` / `HandleScroll` 那几处区域的开关。
        /// ② ⚠️ **如实标注（我们自己的口径，别当成原版行为）**：**原版在那 0.3 秒里到底屏不屏蔽点击，
        ///    我们没核过** —— `CollectionFilterController&lt;T>.Toggle` 的方法体在泛型里、`decomp_full` 无产物
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
        //   判据 = **它所属的那一页在**（`PageOn`）× **逻辑态开着** × **已经完全到位**（`Interactive`）
        //   —— 位移期间不吃（A67）。
        //   批处理下面板总是「一步到位」⇒ 这两个属性与 `_filtersOpen` / `_cosmoFltOpen` **同值**，
        //   既有断言（点得到 / 点不到）一条都不变。
        //   🔴 **2026-10-11（`A1515`）：补上页判据**（原来只有「开着 × 到位」两档）。
        //   判据 = 原版把那棵抽屉**挂在 `Card Display` 底下**（`menu_dump.py … "Deck Editing Menu"`
        //   的缩进 `2 Card Display` → `3 Card Filters`）⇒ `deckDisplay.SetActive(param)` 一动，
        //   **整棵 `Card Filters` 跟着不画**（`DeckEditingWindow__ToggleDisplay.c` 那个入参；
        //   见 `CardDisplayPage` 那段）。⚠️ 我们把它建在 `Root` 下（没有那格父容器）⇒
        //   页判据只能**自己补**在 `PageOn` 上（同一件事的两个落点：`ApplyDrawerSlide` 的显隐 + 这里）。
        //   ⚠️ `PageOn` 由 `SyncFilterDrawerPage()` **一处**写（⛔ 别在别处再赋一次）。
        //   ⚠️ `_fltSlide.PageOn` 这一项**看着冗余**（`Interactive` 里已经 `&& d.PageOn`）——
        //     **故意留着**：`Interactive` 只在 `ApplyDrawerSlide` 跑过之后才反映 `PageOn`，
        //     而这条带是**四个派发口**的共用判据 ⇒ 让它**自包含**、不依赖「谁先跑了一次同步」。
        bool FltStripOn { get { return _filtersOpen && _fltSlide != null && _fltSlide.PageOn && _fltSlide.Interactive; } }
        bool CosmoFltStripOn { get { return _cosmoFltOpen && _cosmoFltSlide != null && _cosmoFltSlide.PageOn && _cosmoFltSlide.Interactive; } }

        /// <summary>把容器摆回**原位**（位移 0）—— **建格子之前**调一次。
        /// 🔴 为什么必须：格子里的小字走 `Label.AlignLeftOn/AlignRightOn`，那两个口减的是
        ///   **父级当前的世界 x**（`Battle/Label.cs` 的 `AlignLeftOn`/`AlignRightOn`）⇒ 容器偏着建，字会被摆到「原位」的世界坐标上、
        ///   与容器差出整段行程（**该偏 385px**）。建完再由 `ApplyDrawerSlide` 按进度整体挪回去。
        ///   ⚠️ 两个口**只减直接父级**，再往上一层（`Root`）的位移它们看不见 —— 所以容器这一层必须自己归零。</summary>
        void DrawerHome(FilterDrawer d)
        {
            if (d == null || d.Node == null) return;
            if (!d.HasBasePos) { d.BasePos = d.Node.localPosition; d.HasBasePos = true; }
            d.Node.localPosition = d.BasePos;
        }

        // ============================================================ 🔴 2026-10-21（A1177）
        // **抽屉里子件的位置，唯一口径 = 「相对容器【原地】原点」，⛔ 不是「相对容器现在在哪儿」。**
        //
        // 为什么单开这一条：本文件 `Img` / `Txt` / `ImageQuad.Create` / `Label.Create` 拿到的
        //   `x/y`（或 `Pos(cx,cy)` 的结果）**只有当「父件的局部原点 = `Root` 的原点」时**才等于
        //   「父件局部坐标」。抽屉容器是 `NewDrawer` 建的、出厂 `localPosition = Vector3.zero`
        //   （= `Root` 原点）⇒ 今天这条等价**恰好成立**，于是「在滑动的途中重建内容」不会露馅。
        //   一旦哪天容器的原地不在零位（`BasePos ≠ 0`），同一批 `Pos()` 就会**整段偏** —— 而且是静默的。
        //
        // ⇒ 抽屉里**自己算位置**的那几处（搜索框底/字、两栏的格子与标签）一律走本函数：
        //   `Pos(画布 px) − 容器原地`。它在 `BasePos == 0` 时**与改动前逐位相同**，
        //   在 `BasePos ≠ 0` 时**仍然对**（这就是 A1151 的 `Vector3.zero` 与 A1170 的
        //   `relativeToParent` 那两套形状之外、本族该用的第三条：**子件多于一个 ⇒ 每个子件各自
        //   给「相对原地原点」的局部坐标**，而不是把偏置摊在子件上或整个塞进根）。
        //
        // ⚠️ `AlignLeftOn` / `AlignRightOn` **不属于这一族** —— 那两个口自己减了父级当前的世界 x
        //   （`Battle/Label.cs` 的 `ParentXInDesignSpace()`），本来就是「跟着容器走」的；
        //   但正因如此，**建之前必须把容器摆回原地**（`DrawerHome`，见它那段）。
        /// <summary>抽屉里子件的 **parent-local** 位置（设计 px → 世界单位）。
        /// ⛔ 别在这里读容器的**当前** `localPosition`（那样重建一次就与容器差出整段行程）。</summary>
        Vector3 DrawerLocal(FilterDrawer d, float cx, float cy)
        {
            var v = Pos(cx, cy);
            if (d != null && d.HasBasePos) { v.x -= d.BasePos.x; v.y -= d.BasePos.y; }
            return v;
        }
        /// <summary>本窗两栏的 `DrawerLocal` 便捷口（与 `FltAbs` 那一族配对用）。</summary>
        Vector3 FltLocal(float cx, float cy) { return DrawerLocal(_fltSlide, cx, cy); }
        Vector3 CosmoFltLocal(float cx, float cy) { return DrawerLocal(_cosmoFltSlide, cx, cy); }

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
                //       里那个 `− parent.position` 减的就是零（判据 → `Editor/DeckScene.cs` 的 `TestLayout` 里那条「`Root` 在原点」不变式：
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
            // 🔴 **2026-10-07（A77⑩「按窗分参数」）**：**卡组编辑窗**的字号 = 原版 **26**（`Placeholder`/`Text` 都是 26），
            //    ⛔ 不是收藏窗那对共用值（30/auto18）—— 判据与出处 → `FilterPanelModel.InputFontPxDeckEdit`。
            if (_fltInputText != null) _fltInputText.SetGlyphHeight(LayoutSpace.Px(FilterPanelModel.InputFontPxDeckEdit));

            var ir = new PxRect(FltX + icR.x1, FltY + icR.y1, FltX + icR.x2, FltY + icR.y2);
            _fltInputIcon = Img("flt_input_i", FilterPanelModel.SearchIconSprite, ir.x1, ir.y1, ir.W, ir.H,
                                QFltIconTop, true, FltParent);

            var titles = new List<FilterPanelModel.Title>();
            // 🔴 **2026-10-14（账 `N14`）**：这里传 `false` = **四行小标题全建**（含 `Energy Cost`）。
            //   费用那一行的显隐**不在建的时候定** —— 它是**复用件**、节点名 `flt_title_Energy_Cost`
            //   还有自检按名找（`DeckScene.FilterTitleLabel` / `TitleLeftPx`）⇒ 建的时候必须四条都在，
            //   显隐留给 `RefreshFilterTitles` 那一拍（那一拍会随卡组状态重跑）。⛔ 别在这里按状态少建一条。
            FilterPanelModel.BuildTitles(State, FltW, titles, costRowNeedsWarlord: false);
            foreach (var tl in titles)
            {
                var r = new PxRect(FltX + tl.R.x1, FltY + tl.R.y1, FltX + tl.R.x2, FltY + tl.R.y2);
                // 🔴 **2026-10-18（A891 的续 · 续做 A）：显示文案走词条，节点名【不变】** ——
                //   `tl.Text` 是**英文原名**（`Army`/`Rarity`/`Energy Cost`/`Type`），它**一行两用**：
                //   节点名 `"flt_title_" + tl.Text.Replace(" ","_")` 与显示文案。本批**只换显示那一个实参**
                //   （`FilterPanelModel.TitleText(tl.Text)`，内部 = `Loc.T(键)`），
                //   ⛔ **节点名那半句一个字没动** —— `Editor/DeckScene.cs` 的 `TitleLeftPx` / `FilterTitleLabel`
                //   按名找（`flt_title_Army` / `flt_title_Energy_Cost` …），名字一换整族断言红。
                //   ⚠️ `Energy Cost` **原版没有 `Localize`** ⇒ `TitleText` 原样返回英文（见 `TitleTerm` 的 doc）。
                var lb = Txt("flt_title_" + tl.Text.Replace(" ", "_"), FilterPanelModel.TitleText(tl.Text), r.x1, r.y1, r.W, r.H, 1, Ink,
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
            _poolViewIdx.Clear();     // 🆕 2026-10-13（A1702②）：「这一格绑的是哪张卡」也要跟着清（三张表一一对应）
            // 🔴 **2026-10-17（D9 删件）**：这里原来还建了卡池左上那块**黄色水印**两件
            //   （`pool_info_bg` 底图 + `pool_info` 字，`"卡池 N / M 张"`）—— **原版没有**、已删
            //   （判据 → D9；那块地方原版第一件东西就是卡池本身）。
        }

        /// <summary>每个格子里现在建的是哪张卡（`null` = 空位）。和 <see cref="_poolViews"/> 一一对应。</summary>
        readonly List<string> _poolViewIds = new List<string>();

        /// <summary>每个格子里那颗视图**绑的是哪张卡**（= `State.VisibleCards()` 的下标；空位 / 没绑 = `−1`）。
        /// 与 <see cref="_poolViews"/> / <see cref="_poolViewIds"/> 一一对应。
        ///
        /// <para>🔴 **2026-10-13（A1702②）它解决什么**：卡池是**按格复用**的（槽 `vi` 装卡表里的
        /// `(firstRow + r) × cols + c`），而**往下滚一格整屏的下标就全变** ⇒ 老那条复用判据
        /// （`_poolViewIds[vi] != def.Id`）**必落重建支**：一格滚轮 = **24 次 `Destroy` + 24 次 `CardView.Create`**
        /// （本仓实测每建一张约 2.2 秒的口径见账 `A1628`）。⇒ 判据改成**按【这张卡】找视图**：
        /// 某张卡**还在这一屏里**，就把它原来那颗 `CardView` **实例搬过来**（只换槽号、名字跟着槽号走）；
        /// **滚出这一屏的**才销毁。</para>
        ///
        /// <para>⛔ **这不是「无脑复用 `CardView`」** —— 那种做法（不销毁、只 `SetData`）会把卡池渲成
        /// **一排空卡位**：`CardView.SetData` **只更新已存在的层、不补缺层**（本仓 2026-09-20 踩过，
        /// 见 <see cref="BuildPool"/> 的头注释）。**旧那条「卡变了就重建视图」的语义原样保留**：
        /// 同一个下标上换成了**另一张卡**（`Id` 不同）照旧 `Destroy` + `Create`。</para>
        ///
        /// <para>⚠️ 空位那几格（`!on`：Cosmetics 页 / 卡表已到头）**保留**视图（只关掉）——
        /// 切回来 / 筛回来时直接复用，不必再花那 2.2 秒。</para></summary>
        readonly List<int> _poolViewIdx = new List<int>();

        /// <summary>`RefreshPool` 装配那一趟的临时表：新一屏的 槽 → 视图 / 卡 `Id` / 绑的下标。</summary>
        readonly List<CardView> _poolNextViews = new List<CardView>();
        readonly List<string> _poolNextIds = new List<string>();
        readonly List<int> _poolNextIdx = new List<int>();
        /// <summary>临时表：卡表下标 → 现在装着它的槽号（只登记**还没被搬走**的那些）。</summary>
        readonly Dictionary<int, int> _poolSlotOfIdx = new Dictionary<int, int>();

        /// <summary>🔴 **2026-10-21（`A1739`）：换档多出来的那几格视图（`cells` 变小 ⇒ 排不进新一屏）。
        /// <para>**为什么留着**：「滚出这一屏 ⇒ 销毁」那条管的是**滚动 / 换卡**（卡表可以有几百张，
        /// 不销毁就没有上界）。**换档不是滚出视口，是屏幕档位变了** —— 多出来的格数有上界
        /// （`max(cells) − cells` ≤ 4），拨回来时它们**正好又是那一屏要的那几张卡**。
        /// ⛔ 改前它们是「不在 `_poolNextViews` 里 ⇒ 销毁」⇒ 24→20→24 往返要重建那 4 张
        /// （本仓每建一张 ≈ 2.2 秒的口径，见 `_poolViewIdx` 的 doc）。
        /// ⇒ 现在**只关不销毁**，并且**追加在 `_poolViews` 的尾巴上**（下标 ≥ `cells`，第二趟与
        /// 收尾那段正是按这个位置认它们）—— 下一趟装配时它们照样进 `_poolSlotOfIdx`，
        /// 于是「拨回来」是**搬**、不是重建（鉴别断言 → `DeckScene` 的 `A1739` 那一节）。</para>
        /// <para>⚠️ 名字让开成 `pool_parked_N`：`UiPoolCellRect` / `PoolCellGo` / `PoolCellsShown`
        /// 都按 `pool_` + 纯数字找格 —— 不换名的话「多出来的那一格」会被读成**还在**。
        /// ⚠️ 数量**不会涨**：每趟能 park 的只剩「旧表里下标 ≥ `cells` 的那些」，而它们正是上一趟 park 来的。</para></summary>
        readonly List<CardView> _poolParkViews = new List<CardView>();
        readonly List<string> _poolParkIds = new List<string>();
        readonly List<int> _poolParkIdx = new List<int>();

        /// <summary>自检读数（<see cref="UiPoolCreatedLastRefresh"/>）：`RefreshPool` 上一趟建了几张卡视图。</summary>
        int _poolCreatedLast;

        void RefreshPool()
        {
            var all = State.VisibleCards();
            var P = Pool;                      // 🆕 D24：**这一趟**的版面参数（两档由开关决定，见 `Pool` 的 doc）
            int cols = P.Cols;
            int cells = cols * P.Rows;         // 这一屏一共几个格（桌面 6×4 = 24 / 小屏 4×5 = 20，行数见 `Pool`）

            float pitch = P.CellH;
            // 🔴 **2026-10-21（`A1740`）就地订正：夹滚动量这一步【挪到排版之前】。**
            //   原来它在收尾（那里原来有一句 `_poolScroll = Mathf.Clamp(…)`，本批删掉）。
            //   为什么非挪不可：`PoolCellAt`（卡池命中的 O(1) 定位，见它那段 doc）是
            //   **按 `_poolScroll` 反解「指针在第几格」**的 ⇒ 前提是「**排出来的格子位置 =
            //   按现读的 `_poolScroll` 算出来的位置**」。放在收尾，一次性滚过头
            //   （`UiScrollPool` 只夹下界）时这一趟用的是**旧值**、而字段已经是夹过的值
            //   ⇒ 那一帧反解到**相邻一格**（滚一下才自愈）。挪上来两个口径当场同一份，
            //   顺带把那帧「滚过头」的画面也修了（内容高 < 视口时也照样夹）。
            int rows = Mathf.Max(1, Mathf.CeilToInt(all.Count / (float)cols));
            // 内容总高 = 行数 × 行距（间距 0）；可滚的 = 超出视口的那些（与 `MaxPoolScrollPx` 同一条式子）
            float maxScroll = Mathf.Max(0f, rows * pitch - PoolH);
            _poolScroll = Mathf.Clamp(_poolScroll, 0f, maxScroll);
            int firstRow = Mathf.FloorToInt(_poolScroll / pitch);
            float off = _poolScroll - firstRow * pitch;
            float top = PoolY;

            _poolCreatedLast = 0;              // 自检读数（`UiPoolCreatedLastRefresh`）
            _poolIndex.Clear();
            // 🔴 **2026-10-13（A1702②）第一步：把现有视图按【它绑的那张卡】登记一遍**
            //   （`卡表下标 → 槽号`）。下一步逐格装配时，「这一格要的那张卡」就能直接找回来 ——
            //   找得到就**把那个 `CardView` 实例搬过来**，找不到才建。见 `_poolViewIdx` 的 doc。
            //   ⚠️ 一个下标只登记**一颗**（正常不会撞：槽与下标一直是双射）。
            _poolSlotOfIdx.Clear();
            for (int vi = 0; vi < _poolViews.Count; vi++)
            {
                if (_poolViews[vi] == null) continue;
                int b = vi < _poolViewIdx.Count ? _poolViewIdx[vi] : -1;
                if (b >= 0 && !_poolSlotOfIdx.ContainsKey(b)) _poolSlotOfIdx[b] = vi;
            }
            // 🆕 **2026-10-17（D24）**：**换档**（桌面 262.5 ⇄ 小屏 393.75）时把「张数条」那批**拆掉重建** ——
            //   它们的**尺寸**（底图框 + 字框）是**建的时候**烘进去的（`Img`/`TxtPx` 只摆一次），
            //   只挪位置不够（会画成上一档的大小）。卡本身不用拆：下面每次刷新都 `SetPose(…, P.CardScale)`。
            //   ⚠️ 判据用**卡位宽**（两档唯一且单调的标识），不比 `Enabled` —— 那个开关在自检里会被临时拨。
            if (!Mathf.Approximately(_poolCellWUsed, P.CellW))
            {
                for (int i = 0; i < _poolBars.Count; i++)
                    if (_poolBars[i] != null) { _poolBars[i].gameObject.SetActive(false); DestroySafe(_poolBars[i].gameObject); }
                for (int i = 0; i < _poolBarTexts.Count; i++)
                    if (_poolBarTexts[i] != null) { _poolBarTexts[i].gameObject.SetActive(false); DestroySafe(_poolBarTexts[i].gameObject); }
                _poolBars.Clear(); _poolBarTexts.Clear();
                _poolCellWUsed = P.CellW;
            }
            _poolNextViews.Clear(); _poolNextIds.Clear(); _poolNextIdx.Clear();
            for (int vi = 0; vi < cells; vi++)
            {
                int r = vi / cols, c = vi % cols;
                int idx = (firstRow + r) * cols + c;    // 这一格现在该装卡表里的第几张
                // 🔴 **卡池在 Cards 页与 Deck info 页【都出现】，只有 Cosmetics 页才关掉** ——
                //    判据 = `CardDisplayPage`（**唯一出处**；原版 `DeckEditingWindow__ToggleDisplay`
                //    的**一个入参**同时管 `cardDisplay` / `cosmeticDisplay` / `wildcards`，见那个属性的 doc）。
                //    🔴 **2026-10-11（`A1497④`）改**：这一格原来是 `_tab == 0` ⇒ **Deck info 页右侧整块是空的**；
                //    而原版这一页**照样是卡池**（`Content Area/Card Display` 1752.83×1009.06、出厂 active，
                //    `Card Display > Scroll View` 视口 (330.2,156)-(1920,1080)，与 Cards 页**同一棵**）。
                //    ⚠️ 上面那条 2026-09-24 的旧记录（「切到 Info / Cosmetics 时卡池还画在那儿、靠叠层遮丑」）
                //    **已作废**：按这条判据，Info 页本来就该画，真关的只有 Cosmetics 页。
                bool on = CardDisplayPage && idx < all.Count;
                _poolIndex.Add(on ? idx : -1);

                // 贴左起排：格中心 = 视口左边 + 整体居中的边距 + 格宽/2 + 列·（格宽 + 间距）
                float cx = PoolX + P.PadX + P.CellW * 0.5f + c * (P.CellW + PoolSpacing);
                float cellTop = top + r * pitch - off;                    // 这一格的**上沿**
                // 🔴 卡**按「张数条上方那一段」居中**（不是整格）—— 底下那一条留给张数条，见 `PoolCounterH0`
                float cy = cellTop + P.CardH * 0.5f;

                // ★ **复用判据（A1702② 改的就是这一处）**：按**这张卡**找视图 ——
                //   卡表下标 `idx` 上原来挂着一颗、而且它那张卡的 `Id` 就是现在要的 ⇒ **搬过来**。
                //   ⛔ 别退回「按槽号比」（`_poolViewIds[vi] != def.Id`）：那就是本条账 ——
                //      `firstRow` 一进 1、24 格的 `idx` 全变 ⇒ 24 张全重建。
                //   ⚠️ 空位那几格（`idx >= all.Count`，卡表已到头）**没有**「要哪张卡」可比，
                //      下标还对得上就把视图留着（关掉、下次滚回来直接用）。
                CardView v = null; string id = null; int bound = -1;
                int src;
                if (_poolSlotOfIdx.TryGetValue(idx, out src)
                    && (idx >= all.Count || _poolViewIds[src] == all[idx].Id))
                {
                    v = _poolViews[src]; id = _poolViewIds[src]; bound = idx;
                    _poolSlotOfIdx.Remove(idx);      // ⚠️ 一颗视图只能被一格搬走，搬走就从表里划掉
                }
                else if (on)
                {
                    var nd = all[idx];
                    v = CardView.Create(Root, BattleDriver.ToCardData(nd, nd.Faction), "pool_" + vi);
                    id = nd.Id; bound = idx;
                    _poolCreatedLast++;
                }
                _poolNextViews.Add(v); _poolNextIds.Add(id); _poolNextIdx.Add(bound);

                if (v == null || !on)
                {
                    // 空位：视图**留着**（只关掉 —— 切页/筛选切回来时直接复用，不必再花那 ≈2.2 秒）
                    if (v != null) v.gameObject.SetActive(false);
                    ShowPoolCounter(vi, false);
                    continue;
                }

                var def = all[idx];
                // ⚠️ 搬过来的那颗名字还带着**旧槽号** ⇒ 每次装配都按现在的槽号写一遍
                //   （自检按 `pool_N` 找格：`UiPoolCellRect` / `PoolCellGo` / `PoolCellsShown` 都吃这个名字）。
                v.gameObject.name = "pool_" + vi;
                v.gameObject.SetActive(true);
                // 🔴 **2026-10-13（A1702 ③）**：第 4 个实参 = 那颗 `RectMask2D` 的等效物（`PoolViewClip`）。
                //   原来**不传**（= 这张卡不裁）⇒ 部分滚出视口的卡**整张画出去**（压在页头空带上）。
                //   ⚠️ 每次刷新都要传：`CardView.SetPose(…, null)` 的语义是「**不改**当前状态」，
                //     ⛔ 不是「跟视口走」—— 这里传的是**同一个矩形**，逐帧重复喂是幂等的。
                //   ⚠️ 顺序照收藏窗那处（`Shell/CollectionWindow.cs`）：**先 `SetPose` 定位姿、
                //     再由它自己重裁**，`SetData` / `SetFace` 的尾巴会跟着重裁（同一份裁切）。
                v.SetPose(Pos(cx, cy), 0f, P.CardScale, PoolViewClip);
                v.SetData(BattleDriver.ToCardData(def, def.Faction));
                v.SetFace(CardFace.Full);
                // 🔴 **2026-10-18（卡池排序与显隐）：加不进的卡 = 【显示 + 变灰】，⛔ 不是不显示。**
                //   判据 = 原版 `DeckEditorCollectionDisplay__DrawCell.c:65,78` 把 `CanAddCard` 的结果当
                //   `canAdd` 传给 `CollectionCard__UpdateCardVisuals`，后者 `:19-20` 是
                //   `UIImageGreyscaleController__ToggleGreyScale(controller, param_2 ^ 1, …)`
                //   （`param_2` = canAdd ⇒ **加不进 = 灰**）。
                //   ✅ **2026-10-19（`A2139`）**：灰的方式改成**真去饱和**了 ——
                //   `CardView.SetGrey` 写原版那个材质属性 `_GreyScale`（0 = 原色 / 1 = 全灰；
                //   判据 = 原版材质 `Sprite Greyscale` 的 `_GreyScale = 1.0`）。
                //
                // 🔴🔴 **2026-10-24（`W6` · `G1`）就地订正（铁律 5）—— 灰错层了：`canAdd` 只该灰【张数条】。**
                //   原版 `CollectionCard` 上有**两个独立**的 `UIImageGreyscaleController`，读的不是同一层：
                //     · `counter.greyscale`（挂在 GO `Counter`，只 pair 那一张药丸底图）
                //       ← `CollectionCard__UpdateCardVisuals.c:19-20` `ToggleGreyScale(counter, canAdd ^ 1, 0.25f)`
                //       ⇒ **「加不进」只灰张数条**；
                //     · `cardUI.greyscaleController`（挂在 GO `2DCard`，pair `CardImage`/`CardFrame`/`Cost Background`）
                //       ← 同文件 `:44-47` `cVar3 = isOwned ? isBanned : 1` ⇒ **卡面只由「未拥有 / 被禁」驱动**。
                //   ⇒ **满编 30/30 时原版卡面一张都不灰、只有张数条全灰**；我们原来把 `canAdd` 接到**卡面**
                //    （`SetHighlight(Unplayable)` + `SetGrey(1)`）⇒ 满编时整池卡面全灰、张数条一直彩着
                //    —— 正好对调（用户 2026-10-24 真机报「怎么所有的卡都是灰色的」）。
                //   判据全文 + prefab 实证 → `资料/普查产出_1024d/G1_卡组编辑变灰判据.md` §1/§2/§4。

                // ① **卡面**：只由 `!isOwned || isBanned` 驱动（原版 `UpdateCardVisuals.c:44-47`）。
                //    ⚠️ **判据在、数据源缺 ⇒ 这一档今天不可达**（如实标注，⛔ 别当成已做）：
                //      · `isOwned` —— 本工程「全卡已解锁」，`CardProgress.Owned` 恒 > 0（它按「给足」口径回
                //        `DeckCap + 升满所需`，见 `Shell/CardDetailPopup.cs:211-227`）⇒ 永远为真；
                //      · `isBanned` —— 原版走 `PlayEventData.IsCardBanned(currentEventData, card)`，只在绑了
                //        活动/自定义模式时非 null；**我们还没有禁卡表**（`DeckError.InvalidDeckBannedCards`
                //        一直是「永远不回」的那两个之一，见本文件 `:5773`）⇒ 恒 false。
                //    代码路径照留（判定式与原版逐字同义）：一旦有了数据源，把 `banned` 换成真读即可。
                bool owned = CardProgress.Owned(def.Id, def.Rarity) > 0;
                bool banned = false;
                // ⚠️ `Unplayable` **会自己 `SetGrey(1)`**（`Core/CardView.cs:6916-6928`）—— 这一档正是原版
                //    「未拥有 / 被禁」要的卡面灰，所以**不额外调 `SetGrey`**（省得两处各写一遍灰度）。
                //    ✅ 这也正是简报里那条「先例」的反面用法：`BattleDriver.cs:23763` 是「`SetHighlight` 之后再
                //       `SetGrey(0)`」把它抵消掉 —— 我们这里**要的**就是它。
                v.SetHighlight(owned && !banned ? CardHighlightState.Playable : CardHighlightState.Unplayable);

                // ② **张数条**：`!canAdd` 才是灰它的那个条件（原版 `:19-20`）。
                //    ⚠️ 灰的是**底板那一张图**，旁边的数字（`poolcnt_*`，`Label`）**不灰** ——
                //    原版那个数字是 GO `Text (TMP)`、**不在** `counter.greyscale` 的 `imageMaterialPairs` 里
                //    （`m_fontColor` 恒 `(1,1,1,1)`）。
                bool canAdd = State.CanAdd(def) == DeckError.None;
                // 底下那条「张数」（原版 `Collection Card/Content/Counter` + `Text (TMP)`）
                ShowPoolCounter(vi, true);
                SetPoolCounter(vi, def, cx - P.CellW * 0.5f, cellTop, !canAdd);
            }

            // 🔴 **2026-10-13（A1702②）第二步：旧视图里【没被搬走】的那些 ⇒ 处置。**
            //   两类来源，**处置不同**（🔴 2026-10-21 · `A1739` 拆开）：
            //     · 下标 **< `cells`**：「**滚出这一屏**的」（滚动 / 筛选 / 同一格上换了卡）⇒ **真销毁**
            //       —— 卡表可以有几百张，这就是本条账那条语义的用处（不设上界就没有上界）。
            //     · 下标 **≥ `cells`**：「**换档**多出来的槽」（24 ⇄ 20）⇒ **只关不销毁**（park）
            //       —— 那不是「滚出视口」，是**屏幕档位变了**，格数有上界，拨回来正好又要它。
            //       进 `_poolParkViews`，装配完追加在 `_poolViews` 尾巴上（见那份 doc）。
            //   ⚠️ 判据 = **引用相等**（在不在 `_poolNextViews` 里），⛔ **不按槽号比** ——
            //      搬的时候槽号是串的（`pool_6` 那颗现在挂在第 0 格）。
            _poolParkViews.Clear(); _poolParkIds.Clear(); _poolParkIdx.Clear();
            for (int vi = 0; vi < _poolViews.Count; vi++)
            {
                var old = _poolViews[vi];
                if (old == null || PoolKept(old)) continue;
                if (vi >= cells)
                {
                    // 🔴 `A1739`：换档多出来的槽 —— 关掉、让开名字，但**实例留着**（下一趟按【它绑的卡】复用）
                    old.gameObject.SetActive(false);
                    old.gameObject.name = "pool_parked_" + vi;
                    _poolParkViews.Add(old);
                    _poolParkIds.Add(_poolViewIds[vi]);
                    _poolParkIdx.Add(_poolViewIdx[vi]);
                    continue;
                }
                // ⚠️ Play 模式下 `Destroy` 要等帧末 —— 先关掉，否则那一帧新旧两张会叠在一起
                old.gameObject.SetActive(false);
                // ⚠️ 名字也要先让开：Play 模式下这颗要**帧末**才真没，同一帧里会有两颗都叫 `pool_N`
                //   —— 而 `UiPoolCellRect` / `PoolCellGo` 走的是 `Root.Find("pool_N")`，会取到**死的那颗**。
                //   （`PoolCellsShown` 数的是 `pool_` 前缀 **且活着** 的，不受影响。）
                old.gameObject.name = "pool_gone";
                DestroySafe(old.gameObject);
            }
            _poolViews.Clear();    _poolViews.AddRange(_poolNextViews);
            _poolViewIds.Clear();  _poolViewIds.AddRange(_poolNextIds);
            _poolViewIdx.Clear();  _poolViewIdx.AddRange(_poolNextIdx);
            // 🔴 **`A1739`**：park 的那几格**追加在尾巴**上（⛔ 位置有意义：必须排在 `cells` 之后 ——
            //   上面那一趟与下面那一段收尾都按「下标 ≥ `cells`」认它们；而 `_poolSlotOfIdx` 那一趟
            //   扫描扫的是**整张表** ⇒ 它们下一趟照样参与「按【这张卡】找视图」的复用）。
            _poolViews.AddRange(_poolParkViews);
            _poolViewIds.AddRange(_poolParkIds);
            _poolViewIdx.AddRange(_poolParkIdx);

            // 🆕 **2026-10-17（D24）**：换档会**换列数**（6 ⇄ 4）—— 多出来的那些卡视图**离开了循环范围**，
            //   不关的话它们会**留在画面上**（静默、只在换档时现形）。
            // 🔴 **2026-10-18（A870）**：换档现在也会**换行数**（桌面 4 行 24 格 ⇄ 小屏 5 行 20 格，判据见 `PoolMinCoverage`）
            //   —— 所以这条收尾**必须**按现算的 `P.Rows` 走，⛔ 不能沿用一个常量（沿用了就会在换档后留下 4 格幽灵）。
            // 🔴 **2026-10-21（`A1739`）就地订正（铁律 5）**：这里原来写「`A1702②` 起这一段**恒为空转**」
            //   —— **本批起它又活了**：`_poolViews` 尾巴上现在挂着 park 那几格，这一段正是把它们
            //   （连同任何漏网的）**再关一次**的那道保险。⚠️ 它是**保险**、不是主口径（主口径在上面那一趟）。
            for (int vi = cells; vi < _poolViews.Count; vi++)
                if (_poolViews[vi] != null) _poolViews[vi].gameObject.SetActive(false);
        }

        /// <summary>`RefreshPool` 用：这个旧实例**被搬进新一屏了**没有（判据 = 在不在 `_poolNextViews` 里）。
        /// <para>⚠️ 这里必须比**引用**（`ReferenceEquals`）—— 搬的时候视图会**换槽号**，按槽号比会全比错；
        /// 而两边都是我们自己刚传过来的同一个引用，不存在 Unity 那种「同一原件两个托管壳」的问题。</para>
        /// <para>没被搬走的 = 滚出这一屏的 / 同一格上卡换了的 / 换档多出来的槽 ⇒ 由调用点处置
        /// （🔴 **2026-10-21 · `A1739`**：**「换档多出来的槽」只关不销毁**，见 `_poolParkViews` 的 doc）。</para></summary>
        bool PoolKept(CardView v)
        {
            for (int i = 0; i < _poolNextViews.Count; i++)
                if (ReferenceEquals(_poolNextViews[i], v)) return true;
            return false;
        }

        /// <summary>🆕 **2026-10-21（`A1740`）：指针落在卡池的**第几格**（设计 px · 左上原点）。
        /// 返回 `false` = 不在任何一格上（含格与格之间的空处、池外）。
        /// <para>**为什么需要它**：`HandlePoolClick` / `TickTooltipAt` 原来都是**24 槽全遍历**
        /// （后者**每帧都跑** —— `Update → TickTooltip → TickTooltipAt`，调用链见那两个方法的 doc）。
        /// 卡池是一张**规则网格**（`RefreshPool` 里那条正式子：格心 x = `PoolX + PadX + CellW/2 + c*(CellW+Spacing)`、
        /// 格顶 y = `PoolY + r*CellH − off`）⇒ 命中就是那条式子**反解**，⛔ 不必遍历。</para>
        /// <para>🔴 **等价性**（为什么换掉遍历**不改行为**，逐条）：① 卡**比格小**
        /// （桌面卡宽 = `P.CardScale × CardView.Width × 108` ≈ **217.2** &lt; 格宽 262.5，小屏同比例
        /// ⇒ 卡**整个落在自己那一格**里，格与卡一一对应）；② 五个数值的**命中盘**也都在本格内
        /// （最靠外的护甲盘越出卡框 ≈ 7.5px，仍远小于那 ≈ 45px 的横向余量）⇒ `StatAt` 那半不会跨格；
        /// ③ 格与格**不重叠**（`PoolSpacing = 0` ⇒ 正好相邻）⇒ 任一点最多属于一格。
        /// ⇒ 「先算格、再问这一格」与「逐格问一遍」结果**逐位相同**，只是 24 次变 1 次。</para>
        /// <para>⚠️ **前提：格子的位置必须只由 `_poolScroll` 决定** —— 这正是本批把「夹滚动量」
        /// 挪到 `RefreshPool` **排版之前**的原因（见那一段）。</para>
        /// <para>⚠️ **判别式**（`Editor/DeckScene.cs` 的 `A1740` 那一节）：拿 `UiPoolCellRect(i)` 量出来的
        /// **每一格真实的框心**喂回来，必须得到 **`i` 自己**（两档都跑）—— 正反两条式子一旦漂开，那条必红。</para></summary>
        bool PoolCellAt(Vector2 px, out int vi)
        {
            vi = -1;
            var P = Pool;
            if (P.CellW <= 0f || P.CellH <= 0f || P.Cols <= 0 || P.Rows <= 0) return false;
            float firstRow = Mathf.Floor(_poolScroll / P.CellH);
            float off = _poolScroll - firstRow * P.CellH;
            int c = Mathf.FloorToInt((px.x - PoolX - P.PadX) / (P.CellW + PoolSpacing));
            int r = Mathf.FloorToInt((px.y - PoolY + off) / P.CellH);
            if (c < 0 || c >= P.Cols || r < 0 || r >= P.Rows) return false;
            vi = r * P.Cols + c;
            return true;
        }

        /// <summary>🆕 **2026-10-21（`A1740`）自检口**：画布 px `(x,y)` 反解出来的**卡池格号**
        /// （`−1` = 不在任何一格上）。与命中那条路**同一个函数**（<see cref="PoolCellAt"/>）。</summary>
        public int UiPoolCellAt(float x, float y)
        {
            int vi;
            return PoolCellAt(new Vector2(x, y), out vi) ? vi : -1;
        }

        // ============================================================ 卡池每格底下那条「张数」

        readonly List<ImageQuad> _poolBars = new List<ImageQuad>();
        readonly List<Label> _poolBarTexts = new List<Label>();

        /// <summary>那条「张数」的**文本**。两个格式来自**两处不同的代码**（别当成一条规则）：
        ///   · **卡组里还没有督军** ⇒ `x{…}` —— 原版 `CardCollectionDisplay__SetCell.c:72` 的格式串实测是
        ///     **`x{0}`，喂的是【拥有数】**（同一处还有 `ToggleGreyScale(拥有数 &lt; 1)` ⇒ 没拥有的那张置灰）。
        ///     ⚠️ **我们这版不照喂那个数**：本作资源固定 2000（用户 2026-10-24 从 9999 改）、`CardProgress.Owned` 给足
        ///     （= 卡组上限 + 升满所需）⇒ 照原式会显示 `x11` 那种**没有意义**的数。
        ///     按**用户 2026-09-28 的口径**显示 `min(拥有, 卡组上限)` = 「**能放进卡组的张数**」。
        ///   · **已经有督军** ⇒ `{已在卡组中}/{min(拥有, 卡组上限)}` —— 原版
        ///     `DeckEditorCollectionDisplay__DrawCell.c` 的格式串**实测就是 `"{0}/{1}"`**
        ///     （`stringliteral.json@0x426DE28`，两个数：一个「组里已有几张」、一个「最多能放几张」）。
        /// 分母两处相同；⚠️ **详情弹窗**那条 `x{a}/ {b}`（可放入张数 / 多余副本数）是**第三个**格式，别混。
        ///
        /// <para>🔴 **2026-10-17（D21）—— 分母到底是不是「上限」：判据坐实了，我们的实现【已经对】。**
        /// 施工单 D21 写「原版印 `1/2`、我们印 `1/1` ⇒ 看着是**上限**」（置信「中 · 未坐实」）。
        /// **逐句读原版那一段**（`d:/2/tools/decomp_full/DeckEditorCollectionDisplay__DrawCell.c`）：
        /// ```
        /// :42  iVar6 = InventoryManager.GetOwnedCount(card)                  // 拥有数
        /// :51  iVar7 = GameplayVariablesData.GetMaxCopiesInDeck(…, card 的两格)   // 卡组上限
        /// :54  if (iVar6 &lt; iVar7) iVar7 = iVar6;      // 🔴 **就是 min(拥有, 上限)**
        /// :72  {0} = Enumerable.Count(当前编辑中卡组里这张卡有几张)
        /// :76  String.Format("{0}/{1}", …)            // 格式串 = DAT_18426de28（= `{0}/{1}`）
        /// ```
        /// ⇒ **原版的分母是 `min(拥有, 上限)`，不是裸上限** —— 我们这一行**逐字同义**，
        /// 所以 D21 **不改**（⛔ 别「照那条看着像的实拍」去把 `Mathf.Min` 删掉：删了就真偏了）。
        /// 实拍也自洽：那一屏的卡全都有 1~2 张（`0/2` `0/1`），`min` 夹出来正好等于上限。</para>
        ///
        /// <para>⚠️ **唯一一处**与原版不同、且是**用户拍过板**的：**没督军**那一支原版印 `x{拥有数}`
        /// （`CardCollectionDisplay__SetCell.c:72`），我们印 `x{min(拥有, 上限)}` —— 理由见上（资源给足，
        /// 照原式会显示出 `x11` 那种没意义的数）。**如实标注，不改。**</para></summary>
        string PoolCounterText(CardDef def)
        {
            // 🔴 **2026-10-17（A894）**：喂**带卡型那一档** —— 原版 `GetMaxCopiesInDeck` 那句
            //   `if (cardType == 10) return 1;`（= 督军）**排在稀有度判断之前** ⇒ **非传说督军也是 1**。
            //   本处是**屏幕上看得见的那一处**：卡池格底下那条分母（`{已在卡组}/{能放的张数}`）——
            //   没选督军时卡池**只列督军**（`WarlordGatedPool`），那些格印的就是督军的上限。
            int cap = DeckRules.CopyLimit(def.Rarity, def.Type);
            int den = Mathf.Min(CardProgress.Owned(def.Id, def.Rarity), cap);
            if (string.IsNullOrEmpty(State.Deck.WarlordId)) return "x" + den;   // 还没督军
            return State.Deck.CountOf(def.Id) + "/" + den;                       // 已有督军
        }

        /// <summary>摆一格底下那条「张数」（底图 + 那行字）。**每次刷新都要重摆** ——
        /// 滚动时 `cellTop` 一直在变，而 `Img`/`Txt` 只在建的时候摆一次。
        /// <para>🆕 **2026-10-24（`W6` · `G1`）**：<paramref name="greyed"/> = 原版那条
        /// `counter.greyscale` 的状态（`canAdd ^ 1`）—— **只灰底板这一张图，数字不灰**
        /// （判据 → `资料/普查产出_1024d/G1_卡组编辑变灰判据.md` §2·B）。</para></summary>
        void SetPoolCounter(int vi, CardDef def, float cellLeft, float cellTop, bool greyed)
        {
            while (_poolBars.Count <= vi) { _poolBars.Add(null); _poolBarTexts.Add(null); }
            var P = Pool;                       // 🆕 D24：与 `RefreshPool` 同一档（每次现取 ⇒ 两处不会错开）
            float bw = P.BarX2 - P.BarX1, tw = P.BarX2 - P.BarX1;
            float tx = cellLeft + (P.BarX1 + P.BarX2) * 0.5f;
            if (_poolBars[vi] == null)
            {
                _poolBars[vi] = Img("poolbar_" + vi, PoolCounterSprite,
                                    cellLeft + P.BarX1, cellTop + P.CardH, bw, P.CounterH,
                                    QPoolBar, true);
                var lb = TxtPx("poolcnt_" + vi, "", cellLeft + P.BarX1, cellTop + P.CntY1,
                               tw, P.CntY2 - P.CntY1, PoolCounterPx, Color.white, QPoolBarText);
                // 原版那行字**开了 autosize（7…32）**，而字框只有 22.69 高 ⇒ 运行时会被压小；
                // 我们走同一条路（`SetAutoFitBox`）：限宽/限高 = 字框、下限 7px。
                // 🔴 **2026-10-11（A305①）**：第 5 个实参 = 原版 `m_fontSizeBase` **原文**。
                //    判据（原版实读）：`/Deck Editing Menu/…/Deck Selector Hero Card Info button/Content/**Card Name**`
                //    同族那颗（`x14`）= `m_fontSize 31.9` · `auto[7~32]` · **`base 32.0`**（逐站表 §二·3 #16）
                //    —— ⚠️ 我们传的上限 `PoolCounterPx 31.9` 是 `m_fontSize`、原版上限是 **32**：那是 A333，本轮不动。
                if (lb != null) lb.SetAutoFitBox(U(tw), U(P.CntY2 - P.CntY1), 7f, PoolCounterPx, 32f);
                // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版那颗（`Card Name` 同族、`x14` 那一格）
                //   = `Center/**Midline**`（判据 = 本文件 `:177` 那条 summary：「原版 `m_fontSize = 31.9` ·
                //   **autosize 7…32** · `Center/Midline` · 白」）。
                //   ⚠️ 框高传**原版那个字框**（`PoolCntY1..PoolCntY2` = 高 22.69）—— `Midline` 用不到它，
                //   但传对才是同一条口径（真要改成 `Top`/`Bottom` 时不用回头改这里）。
                if (lb != null)
                    MenuDraw.SetVAlign(lb, Label.VAlign.Midline,
                                       new PxRect(cellLeft + P.BarX1, cellTop + P.CntY1,
                                                  cellLeft + P.BarX1 + tw, cellTop + P.CntY2));
                _poolBarTexts[vi] = lb;
            }
            var bar = _poolBars[vi];
            if (bar != null)
            {
                bar.transform.localPosition = Pos(tx, cellTop + P.CardH + P.CounterH * 0.5f);
                // 🔴 **2026-10-24（`W6` · `G1`）**：这里就是「加不进 ⇒ 张数条变灰」的落点。
                //   走 `_GreyScale`（真去饱和），⛔ **不是 `SetTint`** —— 材质色是乘法，那是「变暗」不是「去色」。
                //   ⚠️ 补间：原版那一句传 `0.25f`，但那个常量**没有读二进制核实**（`G1` §6.3）
                //   ⇒ 这里瞬切，不猜数（`ImageQuad.SetGrey` 的 doc 也写了这一条）。
                bar.SetGrey(greyed ? 1f : 0f);
            }
            var t = _poolBarTexts[vi];
            if (t != null)
            {
                t.transform.localPosition = Pos(tx, cellTop + (P.CntY1 + P.CntY2) * 0.5f);
                t.SetText(PoolCounterText(def));
            }
        }

        void ShowPoolCounter(int vi, bool on)
        {
            if (vi < _poolBars.Count && _poolBars[vi] != null) _poolBars[vi].gameObject.SetActive(on);
            if (vi < _poolBarTexts.Count && _poolBarTexts[vi] != null) _poolBarTexts[vi].gameObject.SetActive(on);
        }

        // ------------------------------------------------------------ Deck info 的动作钮（🔴 已删，见下）

        // 🔴 **2026-10-17（D35 删件）：`BuildInfoActions()` 整个删掉了** —— 分享 / 导入那两颗圆钮
        //   （`info_share*` / `info_import*`，`60..131` 与 `200..271` × `636..707`）**原版这扇窗里没有**。
        //   判据（原版侧栏树逐件核过 + 施工单 D35「我们多画 4 件」）→
        //   `资料/普查产出_1017/对账_卡组编辑部分_差异与待办.md` D35。
        //   原版这两个动作**各自的正当入口**（我们当时是「先摆这儿免得找不到」）：
        //     · 分享 = `DeckInfoPopup.ShareDeck()`（`decomp_full/DeckInfoPopup__ShareDeck.c`，卡组线那扇 Info 弹窗）；
        //     · 导入 = `SelectDecksTab.ImportDeck()` → `ImportDeckPopup`（外壳那扇，`Shell/ImportDeckPopup.cs` 已建）。
        //   ⇒ 🔴 **2026-10-23（W65 · 1c）**：本窗 `ShareDeckString()` **已整段删掉**（全仓 0 调用点，留痕在它原位置）
        //     （它原来只挂在这颗钮上）；`ImportDeckPopup` 那条链还有外壳那一份（`CollectionWindow.OpenImportPopup`），
        //     本窗那份（`UiOpenImport()` 供着的）已按 **T23** 整棵删掉。⛔ **别把这两颗钮加回来** —— 那是「我们多画的」，
        //     正确做法是把它们**接在原版的家里**（那两处都在别的文件，不在本批白名单）。
        //   ⚠️ 一并删掉的登记点：`ClickOrder` 那两项 · `HandleButtons` 那两个 `case` · `KeyLive` 那两个分支 ·
        //     自检读数 `UiInfoActionsVisible`（它读的就是 `Lookup("info_import")`）。
        //   ⚠️ `_infoOnly` 这张表**没删**：费用曲线那 9 行 × 4 件仍然只在 Deck info 页签显示。
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
        //   · `Cosmetic FIlter`（左抽屉）2.18,155.97 → 333.90,1080.03，**出厂 act=F**
        //     ✅ **2026-10-09 就地订正（铁律 5）**：这里原来写「⇒ **先不建**」—— **早就建了**：
        //     `:1381` 的 `_cosmoFltSlide = NewDrawer("cosmoflt_drawer")` ＋ `:1391` 那段注释（「🆕 2026-10-01：**卡背页那个抽屉**（原版 `Cosmetic FIlter`…）」），`:4178`/`:5492` 还有它的自检读数段；
        //     `项目任务.md` §三那条也写着「两扇筛选抽屉都建完了」⇒ ⛔ 别再把后半句读成「没建」。
        //   · `Cosmetic Drag Controller`（拖拽预览，100×100 + `scl 0.6` 的 250×405 预览）
        //     🔴 **2026-10-17 更正（铁律 5）：这里原来写「⇒ 先不建」—— 那是我们自己写的判断、
        //     不是用户拍板**（出处 `资料/普查产出_1016/可玩性_卡组编辑.md:49`）⇒ 按铁律 11「要么原版没有、
        //     要么用户明说不做，否则**要做**」，**现在建了**：见下面「拖拽」那一大段（`BuildCosmeticDrag`）。
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
        // 🔴 **2026-10-18（`A1032` 删件）**：这里原来还有一个 `Label _cosmDrawerName;`
        //   （印卡背 id / 「默认卡背（没选过）」）—— **原版这一格没有字**，已删。判据三条：
        //   ① `menu_dump.py bundle_menus_assets_all --rt -8862950109591769308`：`Cosmetic Drawer`
        //      子树只有 **1 个 `Image`**（子件 `Cosmetic`，无图 / preserveAspect），**没有任何 TMP**；
        //   ② 全窗 `Deck Editing Menu` 的 TMP 清单里没有落在 `Cosmetic Drawer` 下的行；
        //   ③ 反编译 `DeckCosmeticDrawer__Initialize.c` 只对 `+0x20`（`cosmeticImage`）调
        //      `Image.set_sprite`，**一个字都不写**。
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
            // 🔴 **2026-10-18（`A1032` 删件）**：这里原来还建了一颗字
            //   `Txt("cosm_drawer_name", "", DrawerX, DrawerY + DrawerH - 44f, DrawerW, 44f, …)`
            //   —— **原版这棵子树里一个 TMP 都没有**、已删（判据见字段声明处那三条）。
            //   ⚠️ 删的**只是这行字**：抽屉本体（`_cosmDrawerBack` / `FitDrawerBack`）、
            //      网格、右键装备、以及这颗字**所在的那一格**都没动。
            //   ⚠️ 原来它还往 `_cosmOnly` 塞过一项（`_cosmDrawerName != null ? ….gameObject : null`，
            //      可能是 null）—— 一并去掉；`_cosmOnly` 本来的成员（卡背图 / 空态告警）不变。
            //   ⚠️ 顺带作废：双语线施工单 `…_附_BattleDeck.md` **#43** 那一项
            //      （`"默认卡背（没选过）"` → `MenuDeck/HUD/DefaultCardback`）—— 键随字一起没了
            //      （该键**只在那一处用过**，未进任何词条表 ⇒ 无需清理，见删件报告）。
            // 🔴 **2026-10-17（D44 删件）**：这里原来还有一行 `cosm_hint`
            //   （「右键卡背 = 装备到当前卡组（左键不做任何事，与原版一致）」）—— **原版这一页没有这行字**、
            //   已删（判据 → D44；它当时是作者自己加出来「把只写在原版代码里的操作说出口」的）。
            //   ⚠️ 右键装备这条交互**没变**（`HandleCosmeticClick` / `EquipCardback`），少的只是那行说明。
            //   ⚠️ 顺带说清「为什么原版不需要它」：原版靠抽屉里**已装备那张图**说话（`Cosmetic Drawer`），
            //     换成功了图就变 —— 我们那条链一样在（`RefreshCosmeticDrawer`）。

            // ---- `Empty Collection Warning`（原版 act=F；判据 = 过滤后为空）----
            // 出厂 `act=F`、**运行期才按数据开** ⇒ 建出来先关着。判据写在 `RefreshTabVisibility` 一处
            //（本页还没有筛选抽屉 ⇒ 233 张永远非空 ⇒ 实际上永远不显示，与原版「没筛就不空」一致）。
            _cosmEmptyWarn = NewGo("cosm_empty");
            // 🔴 **2026-10-18（第三轮整改 · 审查 P4）：字走 `Loc.T`** —— 卡背抽屉这一份与收藏窗 Cards 页
            //   **同一条键**（`MenuCollection/NoCardsFound`，原版 4 颗里的 `Deck Editing Menu` 那一颗：
            //   `… > Content Area > Cosmetic Display > Scroll View > Empty Collection Warning/Warning`）。
            //   英文列 = TMP 原文逐字符；中文列 = 「在您选择的过滤器中，您的收藏里没有任何张牌」（2026-10-14 官方 zh-CN 表；原「没有符合当前筛选的卡牌」）。
            //   ⛔ 节点名 `cosm_empty_t` 不动。
            var emptyTx = Txt("cosm_empty_t", Loc.T("MenuCollection/NoCardsFound"),
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

        /// <summary>已经为它报过「卡背缺图」的名字 —— **一个名字只出声一次**
        /// （逐次刷屏会把别的告警淹掉，同 `Label.NoteDotBackendLacks` 那条口径）。</summary>
        readonly HashSet<string> _cardbackArtWarned = new HashSet<string>();

        /// <summary>🆕 **2026-10-17（D23）**：卡背取图 —— **缺图必须出声**。
        /// <para>🔴 **为什么要单开这一个口**：卡池那一路缺图有 `Ui()` 记账 + `Build()` 末尾那条汇总告警；
        /// 而**卡背格这一路原来什么都没有** —— `CardArt.Cosmetic(name)` 返回 null 时
        /// `RefreshCosmetics` 只是 `continue`（那一格**空着**），屏幕上看得见、日志里一个字都没有
        /// （2026-10-17 那张截图里就有 2 格空框）。这就是「静默失败」（CLAUDE.md §三 红线）。</para>
        /// <para>⚠️ **不并进 `_missingArt`**：那一份是「建界面那一趟」的账（`Build()` 里清空 + 末尾一次性报），
        /// 而卡背格是**每次刷新都可能重建**的 ⇒ 并进去只会漏报（刷新发生在 `Build()` 之后）。
        /// 本口自己去重、自己出声。</para></summary>
        Texture2D CosmeticTexOrWarn(string name)
        {
            _cosmeticTexCalls++;
            var t = CardArt.Cosmetic(name);
            if (t == null && !string.IsNullOrEmpty(name) && _cardbackArtWarned.Add(name))
                Debug.LogWarning("[Deck] 卡背缺图：`" + name + "` 取不到 ⇒ 那一格**空着**"
                                 + "（**不是静默** —— 名字来自 `CardbackTable` / `CardArt.CosmeticNames()`；"
                                 + "导入器：`工具/` 那条卡背链，见 `Core/CardbackTable.cs`）");
            return t;
        }

        /// <summary>本类**一共走过几次**卡背取图（自检用它钉「卡背格走的就是这一条路」——
        /// 只数「报过警」的话，一个「另写一条取值路、把告警挂在没人调的岔路上」的实现照样绿）。</summary>
        int _cosmeticTexCalls;
        /// <summary>自检口：卡背取图被调过几次（见 `_cosmeticTexCalls`）。</summary>
        public int UiCosmeticTexCalls { get { return _cosmeticTexCalls; } }
        /// <summary>自检口：**报过缺图的名字有几个**（一个名字只出声一次）。</summary>
        public int UiCardbackArtWarnCount { get { return _cardbackArtWarned.Count; } }
        /// <summary>自检口：走**与卡背格同一条**取图路（缺图会出声 —— ⛔ 别在自检里另写一条取值路）。</summary>
        public Texture2D UiCosmeticTex(string name) { return CosmeticTexOrWarn(name); }

        void RefreshCosmetics()
        {
            // 🆕 2026-10-01：卡背页那个 `Army Filter`（原版 `Cosmetic FIlter` 里的一行）——
            //   筛的是**卡背归属的阵营**；判据 = 原版 SO 的 `cardArmy`（表 → `Core/CardbackTable.cs`，
            //   生成 → `工具/gen_cardbacks.py`；对账实测 243 个 SO → 233 个图名、**0 缺口**）。
            //   ⚠️ 只决定**这一页铺哪几张**，不碰卡池（与卡牌那套筛选条件分开）。
            var names = CosmeticList();
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
                var tex = CosmeticTexOrWarn(names[idx]);
                if (q == null && tex != null)
                {
                    q = ImageQuad.Create(Root, tex, Pos(cx, cy), U(CosmoCellH), new Vector2(0.5f, 0.5f), "cosm_cell" + vi);
                    if (q != null) q.SetRenderQueue(QPanel);
                    _cosmCells[vi] = q;
                }
                if (q == null) continue;
                q.transform.localPosition = Pos(cx, cy);
                q.SetTexture(tex);                          // ⚠️ 单参重载：这一句**会把 `_aspect` 冲成贴图自己的比例**
                // 🔴 **2026-10-18（`A994③`）· 就地订正（铁律 5）**：这里原来写死
                //   `SetAspect(CosmoCellW / CosmoCellH)`（= 250/405 = **0.61728**）—— 那是**拉伸**。
                //   原版那一格 `Cardback Container > Cardback` 的 `Image` 带 **`m_PreserveAspect = 1`**
                //   ⇒ 要**按贴图自己的比例内接**进 250×405 的框（口径/判据全文 → `CosmeticPreview.PreserveAspectSize`，
                //   本仓**唯一**一份；⛔ 别在这里另写一条算式）。
                //   ⚠️ 实测 `Resources/Art/cardbacks/` 那 **233** 张：宽高比区间 **0.6188~0.7652**、
                //   **233 张全部 > 0.61728** ⇒ **一律宽定**（宽仍是 250、高缩成 `250 ÷ 比例`）。
                //   例：`Cardback_AM_Shield of Humanity`（707×981，比例 0.72069）⇒ 高 **346.9**（原来画 405，
                //   竖向多 58.1px = 拉了 17%）；偏离最大的 `Cardback_All_Premium4`（707×924）⇒ 高 **326.73**。
                //   ⚠️ **顺序不能反**：`SetTexture`（会冲掉比例）→ 内接算 `(cw, ch)` → `SetWorldHeight` → `SetAspect`
                //   （`WorldW = WorldH × _aspect` ⇒ 两句都要在内接算完之后）。
                float ccw, cch, cdox, cdoy;
                if (CosmeticPreview.PreserveAspectSize(tex, CosmoCellW, CosmoCellH, out ccw, out cch, out cdox, out cdoy))
                {
                    q.SetWorldHeight(U(cch));
                    q.SetAspect(ccw / cch);
                    // 🆕 **2026-10-19（B2）**：画心**不居中** —— 原版按 sprite 的 `padding` 偏
                    //   （算式/判据 → `Core/CardbackFace.cs`）。上面那句 `localPosition = Pos(cx, cy)`
                    //   是**基准**、这里是「基准 + 偏移」；`Pos` 的 y 向上 ⇒ `cdoy`（向上为正）要**减**。
                    //   ⚠️ 反算命中的 `CosmeticNameAt` 走的是**格矩形**、与这层无关 ⇒ 不影响点击（先例同 `Hit`）。
                    q.transform.localPosition = Pos(cx + cdox, cy - cdoy);
                }
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
            // 🆕 **2026-10-17（D23）**：侧栏那张「已装备」同样**缺图要出声**（同 `CosmeticTexOrWarn`
            //   那条口径；它走的是另一条链 `DeckCardback`（选了/没选各有兜底）⇒ 单独记一个 key）。
            if (back == null && _cardbackArtWarned.Add("cosm_drawer:" + (State.Deck.CardbackId ?? "")))
                Debug.LogWarning("[Deck] Cosmetic Drawer 取不到卡背图（`cardbackId = "
                                 + (State.Deck.CardbackId ?? "<没选>") + "` · 阵营默认也取不到）"
                                 + " ⇒ 侧栏那张**空着**（**不是静默**）");
            if (_cosmDrawerBack == null && back != null)
            {
                _cosmDrawerBack = ImageQuad.Create(Root, back, Pos(DrawerX + DrawerW * 0.5f, DrawerY + DrawerH * 0.5f),
                                                   U(DrawerH), new Vector2(0.5f, 0.5f), "cosm_drawer");
                if (_cosmDrawerBack != null)
                {
                    FitDrawerBack(back);                 // 🔴 `m_PreserveAspect = 1` ⇒ 内接，⛔ 不是拉伸
                    _cosmDrawerBack.SetRenderQueue(QSide + 1);
                    _cosmOnly.Add(_cosmDrawerBack.gameObject);
                    RefreshTabVisibility();          // 刚建的这件要立刻按页签定显隐
                }
            }
            if (_cosmDrawerBack != null && back != null)
            {
                _cosmDrawerBack.SetTexture(back);        // ⚠️ 单参重载：会先把 `_aspect` 冲成贴图比例
                FitDrawerBack(back);                     // 🔴 换图之后**必须再内接一次**（这一跳不能省）
            }
            // 🔴 **2026-10-18（`A1032` 删件）**：这里原来按「选过 → 印卡背 id / 没选 → 印
            //   `MenuDeck/HUD/DefaultCardback` 那条词条（当时经一道 `Loc.HasEntry` 闸门、键不在表就退回
            //   字面量 `默认卡背（没选过）`）」刷新那颗字 ——
            //   **那颗字本身是我们自己加的、原版没有**（判据见字段声明处那三条）⇒ 整块删掉。
            //   ⚠️ **2026-10-18 收口（铁律 5）**：本段原来拿 `TermOr("MenuDeck/HUD/DefaultCardback", …)`
            //      **当函数名引用** —— 那个函数随后也被删了（本文件 `TermOr` 已归零）⇒ 这里改成描述**当年那句**
            //      的写法，不再引用一个不存在的符号。
            //   ⚠️ **右键装备这条交互没变**（`HandleCosmeticClick` / `EquipCardback`）：
            //      原版靠抽屉里**已装备那张图**说话（`Cosmetic Drawer` 的 `cosmeticImage`），
            //      换成功了图就变 —— 我们那条链一样在（上面 `_cosmDrawerBack.SetTexture` 那一段）。
            // ⚠️ `Empty Collection Warning` 的显隐**不在这里** —— 它是 `_cosmOnly` 的一员，
            //    由 `RefreshTabVisibility` 一处判（两处各写一次迟早不一致）。
        }

        /// <summary>把侧栏「已装备」那张按**贴图自己的比例内接**进 `DrawerW×DrawerH` ——
        /// 原版那颗 `Image` 带 **`m_PreserveAspect = 1`**（口径/判据全文 → `CosmeticPreview.PreserveAspectSize`）。
        /// <para>🔴 **2026-10-18（`A994③`）**：这里原来两句都是 `SetAspect(DrawerW / DrawerH)`
        /// （= 335.31/400 = **0.83828**）—— 那是**拉伸**。实测 `Resources/Art/cardbacks/` 那 233 张
        /// 比例 **0.6188~0.7652**，**全部 &lt; 0.83828** ⇒ 一律**高定**、宽缩成 `400 × 比例`
        /// （例 `Cardback_AM_Shield of Humanity` 707×981 ⇒ **288.28**×400，原来画 335.31 宽 ⇒ 横向拉宽 16%）。</para>
        /// ⚠️ **换图之后必须再调一次**：`ImageQuad.SetTexture` 的**单参**重载会把 `_aspect` 冲成新贴图的比例
        /// （`WorldW = WorldH × _aspect` ⇒ 不补这一步就是**静默**画错比例）。
        /// 取不到贴图 / 尺寸为 0 ⇒ 返回 `false`、⛔ **不改几何**（同 `PreserveAspectSize` 的口径）。</summary>
        bool FitDrawerBack(Texture2D tex)
        {
            if (_cosmDrawerBack == null) return false;
            float w, h, dox, doyUp;
            if (!CosmeticPreview.PreserveAspectSize(tex, DrawerW, DrawerH, out w, out h, out dox, out doyUp)) return false;
            _cosmDrawerBack.SetWorldHeight(U(h));        // ⚠️ 先改高、再拉比例 ⇒ `WorldW = WorldH × _aspect`
            _cosmDrawerBack.SetAspect(w / h);
            // 🆕 **2026-10-19（B2）**：画心**不居中** —— 原版按 sprite 的 `padding` 偏
            //   （算式/判据 → `Core/CardbackFace.cs`）。这一件的**基准** = 抽屉中心（`RefreshCosmeticDrawer`
            //   建它时那句 `Pos(DrawerX + DrawerW*0.5f, DrawerY + DrawerH*0.5f)`），**每次换图重算**
            //   （⛔ 不在现位置上累加 —— 换两次图就偏两次，静默）。`Pos` 的 y 向上 ⇒ `doyUp` 要**减**。
            _cosmDrawerBack.transform.localPosition =
                Pos(DrawerX + DrawerW * 0.5f + dox, DrawerY + DrawerH * 0.5f - doyUp);
            return true;
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
            MarkDeckDirty();                                   // 🆕 A363：**只标脏**（原来这里就落盘 —— 原版是 `Drop.c:26` 那句 `[+0x60] = 0`）
            RefreshCosmetics();
            RefreshDeckList();
            Say("卡背已换成 " + name);
            return true;
        }

        /// <summary>点卡背格：**照原版只有右键装备**（`DeckEditingWindow__OnCosmeticClick.c`）。
        /// 左键**单独点**仍然什么都不做（那是原版行为、不是漏了）—— 2026-10-17 起它多了一个用途：
        /// **左键按下 = 拖拽那条路的起点**（只记下起点，起不起拖由 `HandlePointer` 里那道
        /// 「移动过阈值 + `IsScrollDragThreshold`」判；松手没动过 ⇒ 还是什么都不做）。
        /// 命中用**与摆位同一条式子**反算行列（`ToPx` 是 `Pos` 的严格逆函数），不另存一份矩形。</summary>
        bool HandleCosmeticClick(Vector2 px, bool right)
        {
            if (_tab != 2) return false;
            if (px.x < CosmoX || px.x > CosmoX + CosmoW) return false;
            if (px.y < CosmoY || px.y > CosmoY + CosmoH) return false;
            var name = CosmeticNameAt(px);
            if (name == null) return false;                      // 最后一行之后的空白 / 落在两侧留白里
            if (right) EquipCardback(name);
            // 🆕 **2026-10-17：左键按下不再「什么都不做」——它是【拖拽那条路】的起点**
            //   （原版左键单独点仍然什么都不做：`OnCosmeticClick` 只认右键；松手没动过 ⇒ 我们这里也不做）
            //   ⇒ 这里只**记下起点**，起不起拖由 `HandlePointer` 里那道「移动越过阈值 + `IsScrollDragThreshold`」判。
            else { _cosmArmName = name; _armPx = px; }
            return true;                                        // 左键也吃掉（原版就是什么都不做）
        }

        /// <summary>卡背页**现在铺哪几张**（阵营筛选之后）—— **判据只此一份**：
        /// 铺格（`RefreshCosmetics`）、命中（`CosmeticNameAt`）、计数（`UiCosmoShownCount`）三处都用它。
        /// 🔴 2026-10-17 之前铺格与命中**各写了一份**、命中那份漏了筛选 ⇒ 那是个真缺陷（见 `CosmeticNameAt`）。</summary>
        string[] CosmeticList()
        {
            return CardbackTable.NamesFor(_cosmoFilter.Faction, CardArt.CosmeticNames());
        }

        /// <summary>点 `px` 落在哪一张卡背上（**判据只此一份**：铺格、右键装备、拖拽起点都用它）。
        /// 🔴 **2026-10-17 顺手修掉一处真缺陷**：原来这里是两套 —— `RefreshCosmetics` 铺的是
        /// **筛过阵营**的 `CardbackTable.NamesFor(_cosmoFilter.Faction, …)`，而本命中走的是**没筛**的
        /// `CardArt.CosmeticNames()` ⇒ **开着 `Cosmetic FIlter` 的 `Army Filter` 时，右键装备的是另一张**
        /// （铁律 11：与原版不符 ⇒ 记录并复刻；本类白名单内，就地改）。
        /// 返回 `null` = 这一点上没有卡背。</summary>
        string CosmeticNameAt(Vector2 px)
        {
            if (px.x < CosmoX || px.x > CosmoX + CosmoW) return null;
            if (px.y < CosmoY || px.y > CosmoY + CosmoH) return null;
            var names = CosmeticList();
            if (names.Length == 0) return null;
            int c = Mathf.FloorToInt((px.x - (CosmoX + CosmoPadX)) / CosmoCellW);
            if (c < 0 || c >= CosmoCols) return null;           // 落在两侧留白里
            int r = Mathf.FloorToInt((px.y - CosmoY + _cosmScroll) / CosmoCellH);
            int idx = r * CosmoCols + c;
            if (idx < 0 || idx >= names.Length) return null;     // 最后一行之后的空白
            return names[idx];
        }

        // ============================================================ 拖拽：起拖 / 跟指针 / 松手投递  2026-10-17
        //
        // 原版这套东西的**判据全部现读**（反编译 + 解包资产 + VA 反汇编），逐条：
        //  · **通用件** = `Core/DraggableController.cs`（`DraggableController<T>` / `Draggable<T>` /
        //    `IDropHandler<T>` 三件；四个方法体是 **VA 反汇编**读出来的，见那个文件的文件头）
        //  · **卡背那一支**：节点 `Content Area > Cosmetic Display > Cosmetic Drag Controller`
        //    （GO −4418684799642833116 · RT −3820370437395452124 = **100×100** · 绝对矩形
        //     **[993.59,525.50]–[1093.59,625.50]**）；它挂 `CosmeticDraggingController`
        //    （MB −4326558023866323164，`m_Script` = MonoScript 5187419560979717525）
        //  · **预览本体** = 它的子节点 `Collection Cosmetic`（GO −6967555497338671324，
        //    **出厂 `m_IsActive: false`**）· RT 3595378309407108900 = **250×405 · `m_LocalScale` 0.6** ·
        //    anchor(0,1) · pos(75,−121.5) ⇒ 绝对矩形 **[943.59,444.50]–[1193.59,849.50]** ·
        //    🔴 **2026-10-18（A944）就地更正**：原来这里写「**视觉框 = 布局框 × 0.6 = 150×243**」——
        //    **错了**：`Collection Cosmetic` 只是**容器**，它下面还有两层
        //    `content`（`sizeDelta` **0×0** · anchor 0,0→1,1 撑满）> `Image_−6633270251387818204`
        //    （`sizeDelta` **220×330** · anchor 居中 · ap (0,0.5) · `m_Type=0` · **`m_PreserveAspect=1`**）
        //    ⇒ **画出来的那一格 = 220×330 × 0.6 = `132×198`**（外框的 250×405 从来不是那张图）。
        //    我们原先拿外框当图 ⇒ 画成 150×243（**宽 18 · 高 45**）—— 这条就是 A944。
        //  · **落点那一栏** = `Sidebar/Deck Details`（GO −1322417011089150172）·
        //    绝对矩形 **[0.25,360.97]–[335.56,1010.03]**（335.31×649.06）—— **上面挂着原版那个
        //    `IDropHandler<CosmeticItem>` 的 `DeckEditingPanel`**（MB −2895006486255833308，
        //    字段集与 `dump.cs:72263-72290` 逐条吻合）。松手落在这里才装备。
        //    ⚠️ uGUI 的 `hovered` **含祖先链**（`BaseInputModule.HandlePointerExitAndEnter`）⇒ 「落点在那一栏的矩形里」
        //       与「命中了它的某一颗子孙」是同一件事 —— 见 `CollectHovered`。
        //  · **起拖音** = 两个实例**共用**一条 `AudioCue`（PathID −4308815958917459268）；
        //    经 `数据/索引/anim_address_map.json` 的 `guid_to_asset` 反查 ⇒ 名 = **`CardStartDrag`**
        //  · **「拖 vs 滚动」那一跳** = `SupportMethods.IsScrollDragThreshold`，整条算出来了（见下）
        //  · **卡牌那一支**：节点 `Content Area > Card Drag Controller` 同矩形
        //    （**[993.59,525.50] 100×100**，**是 `Card Display` 的兄弟** —— 不在 `Card Display` 里）；
        //    子件 = 拖影卡行 `Deck Selector Card Info button`（**287.9 × 55.7**，出厂 INACT）
        const float DragNodeX = 993.59f, DragNodeY = 525.50f, DragNodeW = 100f, DragNodeH = 100f;
        /// <summary>`Collection Cosmetic` 自己的布局框 + 它烤的 `m_LocalScale`（原版实读 250×405 · 0.6）。
        /// ⚠️ **它是容器、不是那张图** —— 里面还有 `content` > `Image_…`（见 <see cref="CosmImgW"/>）。</summary>
        const float PrevW = 250f, PrevH = 405f, PrevScale = 0.6f;
        /// <summary>🆕 **2026-10-18（A944）**：`Collection Cosmetic > content > Image_−6633270251387818204`
        /// 里**那颗 `Image` 自己的 `sizeDelta`** = **220×330**（原版实读；`m_Type=0` Simple ·
        /// **`m_PreserveAspect=1`** ⇒ 它的宽高比就是 **220/330**）。
        /// 🔴 **它才是「画出来那一格」的尺寸** ⇒ 画出来 = 220×330 × `PrevScale` 0.6 = **132×198**。</summary>
        const float CosmImgW = 220f, CosmImgH = 330f;
        // 预览起手（= 还没拖过）时那个静态矩形，原版实读 [943.59,444.50]–[1193.59,849.50]
        const float PrevX1 = 943.59f, PrevY1 = 444.50f, PrevX2 = 1193.59f, PrevY2 = 849.50f;
        const float DropFieldX = 0.25f, DropFieldY = 360.97f, DropFieldW = 335.31f, DropFieldH = 649.06f;
        /// <summary>拖影卡行的原版尺寸（`Deck Selector Card Info button`）。</summary>
        const float CardGhostW = 287.9f, CardGhostH = 55.7f;
        /// <summary>起拖音 —— 原版两个实例**共用**同一条 `AudioCue`（字段 PathID 都是 −4308815958917459268）。
        /// 🔴 **这一格解出来了**（不是「没查清」）：经 `数据/索引/anim_address_map.json` 的
        /// `guid_to_asset` 反查（guid `60fe1fac8e31f4b59ab7cb8e53696707`）⇒ 名 = **`CardStartDrag`**
        /// （`bundle_soundcollection_assets_all/MonoBehaviour/CardStartDrag.json`）。
        /// ⚠️ 本工程的声音载体是 cue 名（同 `Battle/AnimFXController` 里那条 cue 名口径）；
        ///    取不到时 `DraggableController.PlayDragSound` 会**出声一次**（不是静默）。</summary>
        const string DragCue = "CardStartDrag";

        /// 🔶 自己设计（非原版）：拖拽预览的渲染队列 `QDragPreview = 3050` · 原版靠 Canvas 层级，本工程世界空间 quad 必须显式分层（原版判据拿不到） · TODO(谁定的)｜原注：<summary>⚠️ **我们挑的一档队列**（原版靠 Canvas 层级；本工程的世界空间 quad 必须显式分层）。
        /// 取在筛选抽屉（`QFlt` 3020 那族）**之上**（原导入弹窗那档 3100 已随 T23 删件删掉）。</summary>
        const int QDragPreview = 3050;
        /// <summary>「按住之后动了多少才算起拖」—— **与行拖出那条同一个阈值**：`0.15` 世界单位 × 108
        /// = **16.2 设计像素**（`_draggingMoved` 那一句用的就是 `0.15`；本仓铁律：同一条规则只留一份，
        /// 这里把数写成常量、并在那条注释里指过来）。uGUI 侧对应的量 = `EventSystem.pixelDragThreshold`。</summary>
        const float ArmPxSlop = 16.2f;

        CosmeticDraggingController _cosmDrag;      // 卡背那一支（原版 `DeckEditingWindow.cosmeticDrag`）
        CosmeticPreview _cosmPreview;
        GameObject _cosmDragNode, _cosmPreviewGo;
        CardDraggingController _cardDrag;          // 卡牌那一支（原版 `DeckEditingPanel.draggable`）
        CardPreview _cardPreview;
        GameObject _cardDragNode, _cardGhostGo;
        /// <summary>左键已经按在某一格卡背上（= 原版「格子上的 `Draggable` 把 beginDrag 送进 relay」那一刻）。
        /// 非 `null` 就是卡背那一支在等位移。</summary>
        string _cosmArmName;
        /// <summary>左键已经按在卡池某一格上（卡牌那一支在等位移）。</summary>
        CardDef _cardArmDef;
        /// <summary>按下的那一点（设计像素）—— 两支**共用**：一次只会有一个在等。</summary>
        Vector2 _armPx;

        /// <summary>建 `Cosmetic Drag Controller` 那一棵（原版形状：**空节点 + 一个关着的预览子节点**）。
        /// 出厂：预览 **inactive**（原版 `Collection Cosmetic` 就是 `m_IsActive: false`）。
        /// ⚠️ 本工程 `Txt` / `Img` 那些助手带 `parent` 时把 `Pos(...)` 当**局部**位置用
        /// （`Label.Create`/`ImageQuad.Create` 里那句 `go.transform.localPosition = pos`）
        /// ⇒ 「父件在原点」才是它们的既有前提，`Root` 本身也是这个前提。
        /// 拖拽时 `OnDrag` 把**预览根**挪到指针上，整棵子树跟着走（子件的局部位置不变）。
        /// <para>🔴 **2026-10-19（A1151）就地订正（铁律 5）**：本句原来写「**两个容器都摆在场景原点**」——
        /// 拖拽节点（`cosm_drag`）仍在原点，但**预览根**（`cosm_preview`）已改摆在**布局框中心**
        /// `Pos(pcx, pcy)` 上（判据与后果见下面那一句的注释）⇒ 按旧写法画心会
        /// **= 指针 + 布局框中心**。</para></summary>
        void BuildCosmeticDrag()
        {
            _cosmDragNode = NewGo("cosm_drag");
            _cosmDrag = _cosmDragNode.AddComponent<CosmeticDraggingController>();

            float pcx = (PrevX1 + PrevX2) * 0.5f, pcy = (PrevY1 + PrevY2) * 0.5f;
            _cosmPreviewGo = NewGo("cosm_preview");
            _cosmPreviewGo.transform.SetParent(_cosmDragNode.transform, false);
            // 🔴 **2026-10-19（A1151）**：「布局框中心」这个偏置放在**预览根**上 —— 它就是原版
            //   `Collection Cosmetic` 那一件（`Draggable`/`CosmeticPreview` 挂的那一件，也就是
            //   起手没拖、和拖起来之后 `OnDrag` 每帧**整句覆盖 `localPosition`** 的那一件）。
            //   判据（原版方法体，逐句还原 → `Core/DraggableController.cs` 文件头 ②）：
            //     `OnDrag` = `RectTransformUtility.ScreenPointToLocalPointInRectangle(draggable.transform.parent, …)`
            //     ＋ `this.draggable.transform.localPosition = local`
            //   ⇒ **偏置在原版是放在「预览根」上的**（拖拽那一拍被指针位置覆盖掉）⇒ 原版画心**贴着指针**。
            //   ⚠️ 我们原来把它记在**子件 quad** 的 `localPosition` 上（那句 `ImageQuad.Create(…, Pos(pcx, pcy), …)`），
            //     而 `OnDrag` 改的是**根** ⇒ 画心 = 指针 **+ `Pos(pcx, pcy)`**
            //     = 指针 **+ (108.59 px 右, 107.0 px 下)**（⚠️ `Pos(1068.59, 647)` 是「**以屏幕中心为原点**」
            //     的世界坐标 = `(1.0055, −0.9907)` 世界单位，⛔ **不是**「1086 px 那么大的偏置」——
            //     把它当 px 偏置来算会看出「画到屏幕外」那种量级）。
            //   📌 **2026-10-19 本件顺带订正第八会话 `B6` 的一处量级**：`B6` 记的是「画心 ≈ (1308, 1410)、
            //     几乎画到屏幕外」（它把 `Pos` 的**入参**当成了世界/px 偏置）—— 真实偏置见上一行，
            //     **「偏置归属错了」这个结论不变**，只是没到「跑出屏幕」那么多。
            //   🔴 **本条只到「读代码链」这一层、没有实况证据**（第八会话 `B6` 现读报的 ——
            //     出处 `资料/普查产出_第八会话/B6_DeckScene卡背期望值同步.md` §2·2）⇒ 照此措辞，
            //     ⛔ 别把这句写成「原版就是这样（已验）」。
            _cosmPreviewGo.transform.localPosition = Pos(pcx, pcy);
            // 🆕 **2026-10-18（A944）**：原版是**三层**（`Collection Cosmetic` > `content` > `Image_…`）
            //   ⇒ 补建中间那一层 `content`。它的**原版参数**：`sizeDelta` **0×0** · anchor **0,0→1,1**
            //   （= **撑满父件**）· 自己**不挂任何图** ⇒ **视觉零影响**（⛔ **别拿它当判据**；
            //   建它只为「原版有的就照原版结构做」）。
            //   ⚠️ 它和父件一样摆在这棵树的**原点**：`ImageQuad.Create` 那批助手把一个**局部**位置
            //   直接写进 `go.transform.localPosition` ⇒ 「父件在原点」是它们的既有前提。
            var contentGo = NewGo("content");
            contentGo.transform.SetParent(_cosmPreviewGo.transform, false);
            // 预览本体：原版那颗 `Image` 的 `sizeDelta` = **220×330** · `m_LocalScale 0.6`
            //   ⇒ **外接框 = 132×198**。🔴 **2026-10-18（`A994③`）就地订正**：原来这里写「**画出来 = 132×198**」——
            //   把「外接框」当成了「真画出来的矩形」。那颗 `Image` 还带 **`m_PreserveAspect = 1`**
            //   ⇒ 真画出来的是**按贴图自己的比例内接进 132×198**（例：`Cardback_AM_Shield of Humanity`
            //   707×981 ⇒ **132×183.16**，比拉满矮 14.84px）。
            //   这一跳的实现在 `Core/DraggableController.cs` 的 `CosmeticPreview.Initialize`
            //   （`PreserveAspectSize`，本仓唯一一份口径）—— 本处只负责建「外接框」，
            //   `BindView` 之后由它按**真图**改。⚠️ 预览出厂就 `SetActive(false)`（下面那句），
            //   所以这里摆成外接框**不会被人看见**；⛔ 别把本处这跳当成「比例已经对了」。
            //   🔴 **2026-10-19（A1151）**：quad 建在**局部零点**（= 预览根的中心）—— 「布局框中心」
            //   那一跳挪到上面 `_cosmPreviewGo.transform.localPosition = Pos(pcx, pcy)` 去了（判据见那里）。
            //   ⚠️ 本工程助手把 `pos` 当**局部**位置写（`ImageQuad.Create` 末尾那句
            //   `go.transform.localPosition = pos`）⇒ 这里给 `Vector3.zero` 才是「贴着根」。
            var quad = ImageQuad.Create(contentGo.transform, CardArt.Solid(), Vector3.zero,
                                        U(CosmImgH * PrevScale), new Vector2(0.5f, 0.5f), "cosm_preview_img");
            if (quad != null)
            {
                quad.SetAspect(CosmImgW / CosmImgH);     // = 外接框比例（出厂占位；`Initialize` 会按真图内接）
                quad.SetRenderQueue(QDragPreview);
            }
            _cosmPreview = _cosmPreviewGo.AddComponent<CosmeticPreview>();
            _cosmPreview.BindView(quad, CosmeticTexOrWarn);        // 取图走**同一条**会出声的路
            if (quad == null)
                Debug.LogWarning("[Drag] 预览那块 `ImageQuad` 没建起来（`CardArt.Solid()` 取不到？）"
                                 + " ⇒ 起拖时**看不见预览**（**不是静默**）");
            _cosmDrag.Bind(_cosmPreview, DragCue);
            _cosmPreviewGo.SetActive(false);                       // 原版：出厂 `m_IsActive: false`
        }

        /// <summary>建 `Card Drag Controller` 那一棵（同形状：空节点 + 一条关着的拖影卡行 287.9×55.7）。
        /// 卡行三件照原版三件：`Background`（同卡组行的九宫格底）· `Card Name` · `Cost`。
        /// <para>⚠️ **卡行摆在节点中心**，不是照 prefab 那个子件矩形 —— 那条 `[414,555 288x56]` 落在节点
        /// `[993.59,525.50] 100×100` **之外**，是「布局跑之前的模板位」（铁律 10 第 3 条那类；同
        /// `Clear filters` 那个坑）。原版跑起来在哪儿量不到（回收列表），**这是我们挑的位置**。</para>
        /// <para>🔴 **2026-10-19（A1170）**：**「节点中心」那一跳现在记在拖影根自己的 `localPosition` 上**，
        /// 三个子件改成**相对根中心**的局部坐标 ⇒ 静态画出来的位置与改动前**逐像素相同**，
        /// 而拖起来**不再带那个偏置**（判据与量级见函数里那一段）。</para></summary>
        void BuildCardDrag()
        {
            _cardDragNode = NewGo("card_drag");
            _cardDrag = _cardDragNode.AddComponent<CardDraggingController>();

            float gcx = DragNodeX + DragNodeW * 0.5f, gcy = DragNodeY + DragNodeH * 0.5f;
            float gx1 = gcx - CardGhostW * 0.5f, gy1 = gcy - CardGhostH * 0.5f;
            _cardGhostGo = NewGo("card_ghost");
            _cardGhostGo.transform.SetParent(_cardDragNode.transform, false);
            // 🔴 **2026-10-19（A1170）**：「布局框中心」这个偏置放在**拖影根**上 —— 它就是原版
            //   `Card Drag Controller > Deck Selector Card Info button` 那一件（`CardPreview` 挂的那一件，
            //   也就是拖起来之后 `OnDrag` 每帧**整句覆盖 `localPosition`** 的那一件）。
            //   判据（原版方法体，逐句还原 → `Core/DraggableController.cs` 文件头 ②；与同形的 `A1151`
            //   `BuildCosmeticDrag` 用的是**同一份**——同一个 `OnDrag`、同一个形状）：
            //     `OnDrag` = `RectTransformUtility.ScreenPointToLocalPointInRectangle(draggable.transform.parent, …)`
            //     ＋ `this.draggable.transform.localPosition = local`
            //   ⇒ **偏置在原版是放在「拖影根」上的**（拖拽那一拍被指针位置整句覆盖）⇒ 原版画心**贴着指针**。
            //   ⚠️ 我们原来把它摊在**子件**的 `localPosition` 上（子件三件全按**设计 px 绝对坐标**摆：
            //     行底 `PxRect(gx1, gy1, …)` · 圆心 `gx1+17` · 名字 `gx1+62`），而 `OnDrag` 改的是**根**
            //     ⇒ 画心 = 指针 + `Pos(gcx, gcy)` = 指针 **+(83.59 px 右, 35.5 px 下)**
            //     （`gcx = DragNodeX + DragNodeW*0.5 = 1043.59` · `gcy = DragNodeY + DragNodeH*0.5 = 575.50`；
            //      `Pos(1043.59, 575.5) = (0.7739, −0.3287)` 世界单位 —— ⛔ **别把 `Pos` 的入参当成 px 偏置**
            //      「拖出屏幕 1043 px」是误读，同 `BuildCosmeticDrag` 那条 `B6` 订正）。
            //   📌 下面三个子件因此改成 `relativeToParent: true`（矩形相对**根中心**、仍是设计 px、y 向下）；
            //     行底那一颗走 `MenuDraw.Nine`，它的 `MenuDraw.Local(parent, …)` 本来就会减掉父件的设计位置
            //     ⇒ **它天然就是相对的**，只是要求根的 `localPosition` 在它**之前**摆好（就是本行）。
            //   🔴 **本条只到「读代码链」这一层、没有实况证据**（第九会话 `P2` 报告 §③ 的「顺带发现」，
            //     出处 `资料/普查产出_第九会话/P2_战斗表现族.md`）⇒ 照此措辞，⛔ 别写成「原版就是这样（已验）」。
            _cardGhostGo.transform.localPosition = Pos(gcx, gcy);

            // 行底走**公共件**（`NineSlice` 那个助手只会挂到 `Root` 下 ⇒ 这里直调 `MenuDraw.Nine`，
            // 让它在拖影容器里 —— 同一条九宫格参：原版行底 `40k_deck_cardlist_bg` border=(150,0,150,0)）
            var rowBg = Ui("40k_deck_cardlist_bg");
            if (rowBg != null)
                MenuDraw.Nine(_cardGhostGo.transform, rowBg,
                              new PxRect(gx1, gy1, gx1 + CardGhostW, gy1 + CardGhostH),
                              new Vector4(150f, 0f, 150f, 0f), rowBg.width, rowBg.height,
                              QDragPreview, null, true, "card_ghost_bg");
            // 三件的相对位置照卡组行那一套（名字 x 62 · 字号档 1）——
            // ⚠️ **2026-10-20 就地订正（铁律 5）**：这一段原来写的是「（**`RowCostX 17`** / 名字 x 62 …）」，
            //   而 `RowCostX` 当天已按实拍改成 **12.1**、边长 `RowCostS` 改成 **46.2**（判据 → 那条常数的 doc）。
            //   拖影这一颗**没有跟着动**（仍是 `lx1 + 17` / 38）—— 它是**独立的一件**（原版
            //   `Card Drag Controller > Deck Selector Card Info button` 那条拖影），位置「我们挑的」，
            //   ⛔ 本轮不动它（判据不在本件范围内）；**但它与卡组行那颗圆现在不一致**，已记进报告当新账。
            // 坐标一律**相对拖影根中心**（`lx1/ly1` = 行框左上，见上面那段判据）
            float lx1 = -CardGhostW * 0.5f, ly1 = -CardGhostH * 0.5f;
            Img("card_ghost_circle", Ui("Card_Frame_Cost_Icon"),
                lx1 + 17f, ly1 + (CardGhostH - 38f) * 0.5f, 38f, 38f,
                QDragPreview + 1, false, _cardGhostGo.transform, relativeToParent: true);
            var nm = Txt("card_ghost_n", "", lx1 + 62f, ly1, CardGhostW - 70f, CardGhostH,
                         1, Ink, QDragPreview + 2, _cardGhostGo.transform, relativeToParent: true);
            // 🔴 **2026-10-10（A1212 · 块 1 #52）**：接上 `m_enableAutoSizing`（四格 = **2 / 38 / 27.69 / 0**）。
            //   判据 = `R6` §2·F #52（对应件 = 卡组行那颗 `…/Background/Text fill/Card Name`；
            //   ⚠️ `R6` §8·4 记着原版有**两套同名兄弟件**，取的是**带 `Background`** 的那一套 ⇒ 与 #41 同值）。
            //   ⚠️ 折行 = `0` ⇒ 还原（同 `hdr_back_t`）。
            if (nm != null)
            {
                nm.SetAutoFitBox(U(CardGhostW - 70f), U(CardGhostH), 2f, 38f, 27.69f);
                nm.SetWrappingMode(0);
            }
            var cs = Txt("card_ghost_c", "", lx1 + 17f, ly1 + (CardGhostH - 38f) * 0.5f, 38f, 38f,
                         1, Ink, QDragPreview + 2, _cardGhostGo.transform, relativeToParent: true);
            // 🔴 **2026-10-10（A1212 · 块 1 #53）**：接上 `m_enableAutoSizing`（四格 = **18 / 50 / 32 / 1**）。
            //   判据 = `R6` §2·F #53（同上一族那颗 `Cost Image/Cost`）。折行 = `1` ⇒ 不必还原。
            if (cs != null) cs.SetAutoFitBox(U(38f), U(38f), 18f, 50f, 32f);
            _cardPreview = _cardGhostGo.AddComponent<CardPreview>();
            _cardPreview.BindView(nm, cs);
            _cardDrag.Bind(_cardPreview, DragCue);
            _cardGhostGo.SetActive(false);                         // 原版：出厂 INACT
        }

        /// <summary>原版 `SupportMethods.IsScrollDragThreshold(Vector2 dragInput)` —— **整条算出来了**
        /// （常量不是推测：`工具/read_literal.py` 直读 `GameAssembly.dll`）。
        /// <para>反汇编（`decomp_full/SupportMethods__IsScrollDragThreshold.c`）逐句：
        /// `v = Vector2.right`（`DAT_1842d9aa8 + 0xb8 → static + 0x28`；`dump.cs:537615` 把 Vector2 的
        ///  statics 排成 zero(0x0)/one(0x8)/up(0x10)/down(0x18)/left(0x20)/**right(0x28)** ⇒ 就是 right）；
        /// `if (|dragInput| * |v| &lt; 1e-15) return false;`（实测 1.0000000037e-15 = **`Vector2.kEpsilonNormalSqrt`**，
        ///  `dump.cs:537620` —— 而它**正是 `Vector2.Angle` 内部那道除零守卫**）；
        /// `cos = Clamp(dot/(|a||b|), −1, 1)`（两个常量实读 = −1 / 1）；
        /// `deg = Acos(cos) * 57.2958`（= `Mathf.Rad2Deg`）；
        /// `return deg &gt; 60 &amp;&amp; deg &lt; 120`（`_DAT_1834b30c0` = **60** · `_DAT_1834b3268` = **120**）。
        /// ⇒ 整条 = **`Vector2.Angle(dragInput, Vector2.right) ∈ (60°, 120°)`**（`Vector2.Angle` 自带那道守卫，
        /// 逐行等价 ⇒ 直接用公共件，不自己再写一遍 acos）。</para>
        /// <para>语义：位移与**水平轴**的夹角落在 60°~120°（= 竖向为主）⇒ 判成**滚动**，**不起拖**；
        /// 只有基本水平的位移才算拖拽。原版**两条链用的是同一个函数**（`CheckCosmeticDrag` 与 `CheckCardDrag`）
        /// ⇒ 我们也只写这一份。</para></summary>
        static bool IsScrollDragThreshold(Vector2 dragInput)
        {
            float deg = Vector2.Angle(dragInput, Vector2.right);
            return deg > 60f && deg < 120f;
        }

        /// <summary>合一件 `PointerEventData` 给控制器用。
        /// ⚠️ **本仓没有 UGUI `EventSystem`**（`EventSystem.current` 恒 null）—— `BaseEventData` 只把这个引用
        /// 存下来、从不调用它 ⇒ 传 `null` 安全。
        /// ⚠️ `pressEventCamera` 是**只读**属性（ugui 只给 `get`）⇒ 这里填不了；`OnDrag` 那一支在它为 null 时
        /// 退回 `Camera.main` —— 本窗的 UI 相机**就是** `Camera.main`（`Build()` 开头那句 `_cam = Camera.main`）
        /// ⇒ 与「原版读 `pressEventCamera`」同解。</summary>
        PointerEventData DragEvent(Vector2 px, Vector2 delta, bool withHovered)
        {
            var ed = new PointerEventData(null);
            ed.position = PxToScreen(px);
            ed.delta = delta;
            if (withHovered) CollectHovered(px, ed.hovered);
            return ed;
        }

        /// <summary>设计像素 → 屏幕像素（`ToPx` 的严格逆）。给**合成事件**用（批处理没有真鼠标）。
        /// ⚠️ 正交相机下 `WorldToScreenPoint`/`ScreenToWorldPoint` 是严格互逆的一对
        /// （`LayoutSpace.ScreenToWorld` 那一支读的也是同一台相机）。</summary>
        Vector2 PxToScreen(Vector2 px)
        {
            if (_cam == null) return px;
            var wp = Pos(px.x, px.y);
            var sp = _cam.WorldToScreenPoint(wp);
            return new Vector2(sp.x, sp.y);
        }

        /// <summary>`eventData.hovered` 的**等价物** —— 原版那一份是 uGUI `BaseInputModule` 填的：
        /// 射线命中那一件 **+ 它的整条祖先链**（`HandlePointerExitAndEnter`，本机 uGUI `…/InputModules/BaseInputModule.cs`，
        /// `m_SendPointerHoverToParent` 默认 `true`，同文件 `:45`）。
        /// <para>本仓没有 uGUI 射线 ⇒ 判据 = **落点在不在 `Deck Details` 那一栏的矩形里**
        /// （原版那个 `IDropHandler&lt;CosmeticItem&gt;` 的 `DeckEditingPanel` 就挂在那一件上；
        ///  它任一子孙命中都等价于「落点在这一栏里」）⇒ 命中了就把本对象（= 我们那个 panel）放进去。
        /// 落在别处 ⇒ **空表** ⇒ 松手什么都不发生（原版同：在对面的卡池区上松手不装备）。</para></summary>
        void CollectHovered(Vector2 px, List<GameObject> dst)
        {
            if (Root == null) return;
            if (px.x < DropFieldX || px.x > DropFieldX + DropFieldW) return;
            if (px.y < DropFieldY || px.y > DropFieldY + DropFieldH) return;
            dst.Add(Root.gameObject);
        }

        /// <summary>起拖那一跳。= 原版 `CheckCosmeticDrag` 通过之后那两句
        /// （`cosmeticDrag.SetDraggable(item)` + 控制器 `OnBeginDrag`；⚠️ 那一段里**没有**
        /// `ExecuteEvents.Execute`、也**没有**写 `eventData.pointerDrag` —— 与 `CheckCardDrag` 不对称，实测如此）。
        /// <para>闸门顺序照原版：① **`IsScrollDragThreshold`** 分「拖 vs 滚动」→ ② 取内容 →（原版还有
        /// `InventoryManager.HasItem` 判**拥有**，本工程按用户口径「卡池全解锁」⇒ 恒真，**不写这一跳** ——
        /// 写一个恒真的假判据比不写更糟）。</para>
        /// 返回「真的起拖了吗」。</summary>
        bool BeginCosmeticDragAt(Vector2 px, Vector2 deltaSincePress)
        {
            if (_cosmDrag == null || _tab != 2) return false;
            if (IsScrollDragThreshold(deltaSincePress)) return false;   // 判成滚动 ⇒ 不起拖
            var name = CosmeticNameAt(px);
            if (name == null) return false;
            _cosmDrag.SetDraggable(name);                               // 原版 `SetDraggable`（RVA 0x18153A0）
            _cosmDrag.OnBeginDrag(DragEvent(px, deltaSincePress, false)); // 原版 `OnBeginDrag`（RVA 0x1814690）：显示预览 + 出声
            Debug.Log("[Drag] 起拖（卡背 `" + name + "`）· 位移 " + deltaSincePress
                      + " · 与水平轴夹角 " + Vector2.Angle(deltaSincePress, Vector2.right).ToString("F1") + "°");
            return true;
        }

        /// <summary>松手那一跳 = 原版 `OnEndDrag`（RVA 0x1814E90）：关预览 + `hovered` 上逐个 `Drop(CurrentItem)`。
        /// 返回「真的投出去一次了吗」（`DropCalls` 涨了 = `b__6_2` 跑到了 `Drop` 那一句）。</summary>
        bool EndCosmeticDragAt(Vector2 px, Vector2 delta)
        {
            if (_cosmDrag == null || !_cosmDrag.Dragging) return false;
            string item = _cosmDrag.DraggedItem;
            int before = _cosmDrag.DropCalls;
            _cosmDrag.OnEndDrag(DragEvent(px, delta, true));            // `hovered` 只在松手这一跳用得上
            bool dropped = _cosmDrag.DropCalls > before;
            Debug.Log("[Drag] 松手（卡背 `" + item + "` · 落点 " + px + "）⇒ " + (dropped ? "投递了" : "落在落点之外，没投"));
            return dropped;
        }

        /// <summary>卡牌那一支的起拖（与卡背**同一个**阈值函数 —— 原版两条链用的就是同一个）。
        /// <para>⚠️ 顺序照原版 `DeckEditingWindow__CheckCardDrag.c`：① 先算 `IsScrollDragThreshold`
        /// ② 再算 `CardDeck.CanAddCard` ③ 判成滚动 ⇒ **两样都不做**；不是滚动又不能加 ⇒
        /// **弹一条错误消息**（`UIMessageController.ShowError`）**且不起拖**；两关都过才 `SetDraggable`。</para></summary>
        bool BeginCardDragAt(Vector2 px, Vector2 deltaSincePress, CardDef def)
        {
            if (_cardDrag == null || def == null) return false;
            bool scroll = IsScrollDragThreshold(deltaSincePress);
            var err = State.CanAdd(def);                 // 原版 `CardDeck.CanAddCard(deck, card, out msg)`
            if (scroll) return false;                    // 滚动的手势：不弹错、也不起拖
            if (err != DeckError.None)
            {
                SayError(State.AddErrorText(def), "拖不进去：");   // 🆕 2026-10-26（B9 · 卡组:325）：日志与屏幕**分开** —— 屏幕那句 = `DeckError` 词条本身（原版 `ShowError(out msg)`），前缀只留在日志里
                Debug.Log("[Drag] `CanAdd` 没过（" + err + "）⇒ **不起拖**（原版这一支 = `UIMessageController.ShowError`）");
                return false;
            }
            _cardDrag.SetDraggable(def);
            _cardDrag.OnBeginDrag(DragEvent(px, deltaSincePress, false));
            // 🆕 2026-10-17（`N42`）：按 `CardDef` 取 —— 非中英语档日志里的卡名也跟语档
            Debug.Log("[Drag] 起拖（卡 `" + CardText.Name(def) + "`）");
            return true;
        }

        /// <summary>卡牌那一支的松手（同一条 `OnEndDrag`）。</summary>
        bool EndCardDragAt(Vector2 px, Vector2 delta)
        {
            if (_cardDrag == null || !_cardDrag.Dragging) return false;
            int before = _cardDrag.DropCalls;
            _cardDrag.OnEndDrag(DragEvent(px, delta, true));
            bool dropped = _cardDrag.DropCalls > before;
            Debug.Log("[Drag] 松手（拖影卡行）⇒ " + (dropped ? "投递了" : "落在落点之外，没投"));
            return dropped;
        }

        // ---- 两个 `IDropHandler<T>`（= 原版 `DeckEditingPanel` 上那两个接口的等价物）----
        /// <summary>原版 `DeckEditingPanel.Drop(CosmeticItem)`（`DeckEditingPanel__Drop.c`）：
        /// 写 `deck.cardbackId`（`+0x28`）→ 标脏（`+0x60 = 0`）→ `UpdateDrawers()`。
        /// 我们的等价物 = <see cref="EquipCardback"/>（那三件事它一件不少）。
        /// ⚠️ **两条路（右键 / 拖拽）在这一跳合流** —— 照原版。</summary>
        public void Drop(string content)
        {
            bool changed = EquipCardback(content);
            Debug.Log("[Drag] Drop(卡背 `" + content + "`) ⇒ " + (changed ? "已装备" : "没变（同名 / 不认识这个名字）"));
        }

        /// <summary>原版 `DeckEditingPanel.Drop(RawCardScript)`（`Slot: 4`）—— 加牌那条路的终点。
        /// 本工程等价物 = <see cref="TryAddCard"/>（右键那条路走的也是它）。</summary>
        public void Drop(CardDef card)
        {
            if (card == null) return;
            // `TryAddCard` 自己会 `Say`（进了 / 为什么没进），这里只补一行**拖拽那条路**的日志
            TryAddCard(card);
            Debug.Log("[Drag] Drop(卡 `" + CardText.Name(card) + "`)（成没成见上一条）");
        }

        // ------------------------------------------------------------ 导入卡组弹窗（🔴 T23 已删，见下）

        // 🔴 **2026-10-17（T23 删件）：`BuildImportPopup()` 整棵树 + `_imp*` 字段 + `_modalOnly` 全部删掉了。**
        //   它建的 `imp_shade` / `imp_bg` / `imp_title` / `imp_input(_bg)` / `imp_err` / `imp_ok(_t)` /
        //   `imp_close(_x)` 那十件**只剩自检能开**（生产入口 `info_import` 那颗圆钮 2026-10-17 已按 D35 删，
        //   `UiOpenImport()` 的调用点全在 `Editor/DeckScene.cs`）。生产那一份是外壳的
        //   `Shell/ImportDeckPopup.cs`（`CollectionWindow.OpenImportPopup()`）—— **那才是原版这个动作的家**
        //   （`SelectDecksTab.ImportDeck` → `ImportDeckPopup`）。
        //   判据 → `资料/普查产出_1017/UI/R_界面线剩余条目现核.md` §二·13（`T23`）。
        //   ⛔ **别照旧实现补回来** —— 同一扇窗有两套实现时，那颗 `Confirm` 的文案就先分家了（A891 的教训）。


        // 🔴 **2026-10-17（D7/D10/D35 删件）：`BuildNotice()` 整个删掉了。**
        //   它原来建三件，**三件原版都没有**（判据 → `对账_卡组编辑部分_差异与待办.md` D7/D9/D10/D35）：
        //     · `side_title`「卡组编辑」（左栏顶部标题，D7：原版侧栏顶部第一件东西就是页签图标）；
        //     · `store_err`（底部报错横幅，D10：那是自检造写盘失败时才会亮的一件）；
        //     · `notice`（底部提示行，D35：`Say(...)` 那句的屏幕落点）。
        //   ⇒ ⛔ 别「顺手补回来」；`Say()` 那条**消息通道**仍在（`Debug.Log` + `UiLastSay`）。
        //   ⚠️ `Library.LastError` 也**没有**丢掉出口：`SaveAndSay()` 会把它念出来
        //      （`Say(...)` 那句日志、`DeckScene` 的 A398/A547 两条断言读的就是它）。
        //   🔴 **2026-10-18（`G8`）**：`LastError` 从那一天起装的是**诊断串**（`DeckStore` 不再拼中文整句）
        //      ⇒ 显示层**不许**再拿它当主文案：给玩家看的人话走 `Loc.T(TermSaveFailed)`
        //      （`SaveFailReason()`，`LastError` 只缀在括号里当兜底诊断）；
        //      读存档那条路走 `DeckLibrary.LastLoadIssueTerm`（从错误码来，见那个属性）。

        // ============================================================ 刷新

        public void RefreshAll()
        {
            RefreshPool();
            RefreshDeckList();
            RefreshHeader();
            RefreshFilters();
        }

        /// <summary>卡组里那些条目（督军 / 防御卡 / 普通卡……）。
        /// <para>🔴 **2026-10-20 改正两处**（与用户参考图《卡组编辑界面参考》并排比出来的）：</para>
        /// <para>**① 一张卡只占一行**。原来这一路把 `Deck.CardIds` **逐条**摆上去 ⇒
        /// N 张同名牌摆 N 行，而 `RefreshDeckList` 每行又印 `CountOf(id)`
        /// ⇒ **一张卡印两行、份数还印两遍**（我们那 11 行里有 4 对同名、每对都写 `x2`；
        /// 参考图 10 行**零重名**）。<br/>
        /// 原版做法（现读反编译）：`DeckListDrawer__Initialize.c` 遍历卡表时先问
        /// `_list.Any(行 ⇒ 行.CurrentCard == 这张)`，命中就 `UICardInfoItem__SetCount(行, 行.Count + 1)`
        /// （**把份数加在同一行上**），没命中才 `SpawnCardInfo` 建新行。</para>
        /// <para>**② 排序 = 照原版：`费用 → 本地化名的【当前文化】比较 → Id`。**
        /// 🔴 **2026-10-18（铁律 5 就地订正）**：本节旧记录写「`Reorder` 的两个匿名函数方法体
        /// **没反编译出来**、键只能拿用户参考图反推」—— 这条【不成立】。全量反编译里两个方法体各有文件：
        /// `d:/2/tools/decomp_full/DeckListDrawer.__c___Reorder_b__22_0.c` / `…_b__22_1.c`。现读：<br/>
        /// · 键1 = `*(int*)(*(longlong*)(行 + 0x78) + 0x78)` = **`RawCardScript.manaCost`**（费用；
        ///   `+0x78` 偏移的出处 = `资料/AI_原版反编译_0917.md:42`）⇒ 与当时从参考图反推的「费用」一致；<br/>
        /// · 键2 = `RawCardScript__GetLocalizedCardName(…)` = **本地化卡名** ⇒ 与反推的「显示名」一致。<br/>
        /// ⚠️ **关键在于比较器**：`OrderBy/ThenBy` 调用点**没传 `IComparer`** ⇒ 键2 用的是
        /// `Comparer&lt;string&gt;.Default` = `string.CompareTo` = **文化相关**比较
        /// （= `string.Compare(a, b, StringComparison.CurrentCulture)`）。
        /// 🔴 **旧实现用的是 `CompareOrdinal`（码点序）—— 那是一条偏离**；用户 2026-10-18 拍板「照原版」，
        /// 本笔已改回文化比较。⚠️ **文化相关的意思**：中文 Windows（zh-CN）下汉字按**拼音**排；
        /// 换一种 OS 语言顺序会变 —— 这正是原版行为（复刻它，而不是挑一个固定的码点序）。
        /// 实测证据 / 与参考图的对照 → `资料/普查产出_1018/R_卡组排序_照原版.md`。</para>
        /// <para>完整键（**全序** —— `List.Sort` 不稳定，末尾必须落到唯一的 `Id`）：
        /// <list type="number">
        /// <item>**督军（`WarlordId`）恒第 0 行**、**防御卡（`DefensiveId`）恒紧随其后**（有督军 ⇒ 第 1 行；
        /// 没有督军 ⇒ 防御卡就是第 0 行）—— 这两行**不进排序**；
        ///   判据 = 原版 `DeckListDrawer__Reorder.c` 末尾那三句 `SetSiblingIndex`：督军 `0`、
        ///   防御卡 `hero 存在 ? 1 : 0`。我们这两行本来就在排序**之外**显式 `Add` 在最前。</item>
        /// <item>其余卡：**费用升序**（`CardDef.Cost`）→ **显示名**（`CardText.Name(c)`）的
        ///   `StringComparison.CurrentCulture` 比较 → `Id` 的**序数**比较（兜底）。</item>
        /// </list>
        /// `Id` 在池里唯一（1,127 条无重复）⇒ 第三档保证任意两张都能分出先后，排序结果逐拍一致。</para>
        /// <para>⚠️ **本函数是「卡组行」唯一的数据源**（画 / 命中 / 拖拽 / 悬停 / 滚动全长在它上面）
        /// <summary>🔴 **2026-10-18（第五十二会话 · `DeckScene` 判别式）**：**卡组排序的第二把键** ——
        /// 显示名的**当前文化**比较（原版 `OrderBy/ThenBy` 没传 comparer ⇒ `Comparer&lt;string&gt;.Default`
        /// = `string.CompareTo` = `StringComparison.CurrentCulture`）。
        /// <para>抽成 `public` 的**唯一原因**：自检要能**直接对这把键**做「文化序 vs 码点序」判别。
        /// 原来那个判别式依赖「这副牌里恰好有一对方向相反的名字」，而官方中文名落地后该前提不成立了
        /// （2026-10-18 `DeckScene` 假红）。⛔ 生产排序**必须**走它（两处写同一条规则 = 迟早不一致）。</para></summary>
        public static int CompareDisplayName(string a, string b)
            => string.Compare(a ?? "", b ?? "", System.StringComparison.CurrentCulture);

        /// <summary>……按「费用 → 显示名（当前文化）→ Id」排好序的卡组行（去重后的）。
        /// ⇒ 去重与排序**只能放这里**；放在 `RefreshDeckList` 里会让**命中与画面对不上**。</para></summary>
        public List<CardDef> DeckEntries()
        {
            var shown = new List<CardDef>();
            if (State.Deck.WarlordId != null) shown.Add(State.Find(State.Deck.WarlordId));
            if (State.Deck.DefensiveId != null) shown.Add(State.Find(State.Deck.DefensiveId));

            // ① 按卡去重 —— 同一张卡只留一行（份数照旧由 `RefreshDeckList` 印 `CountOf`）。
            //   按 `Id` 认同一张卡（与 `State.Deck.CountOf` / `TryRemove` 同一把尺）。
            //   ⚠️ 督军 / 防御卡那两个 id 也先记进 `seen`：它们已经占着最前面两行了，
            //     万一 `CardIds` 里也混进同一条（旧存档 / 手改），不再出第二行
            //     （`DeckScene` ⑧ 的「`ASH83` 只出现 1 次」那条断言管这个）。
            var seen = new HashSet<string>();
            if (!string.IsNullOrEmpty(State.Deck.WarlordId)) seen.Add(State.Deck.WarlordId);
            if (!string.IsNullOrEmpty(State.Deck.DefensiveId)) seen.Add(State.Deck.DefensiveId);
            var cards = new List<CardDef>();
            foreach (var id in State.Deck.CardIds)
            {
                if (string.IsNullOrEmpty(id) || !seen.Add(id)) continue;
                var d = State.Find(id);
                if (d != null) cards.Add(d);
            }

            // ② 照原版：费用升序 → 显示名（**当前文化**比较）→ `Id`。
            //   🔴 第二把键**必须是文化相关比较**：原版 `OrderBy/ThenBy` 没传 `IComparer` ⇒
            //   `Comparer<string>.Default` ⇒ `string.CompareTo`（= `StringComparison.CurrentCulture`）。
            //   ⛔ 不许换回 `CompareOrdinal`（码点序）—— 那是造成「同费用内顺序看着毫无规律」的那条偏离
            //   （用户 2026-10-18 拍板照原版；实测证据见 `资料/普查产出_1018/R_卡组排序_照原版.md`）。
            //   最后那一档 `Id`（**序数**）是为了**全序**：`List.Sort` 是**不稳定**排序，
            //   没有它，同费用同名的两张会随实现抖动（`CardDef` 按 `Id` 建 ⇒ 这一档恒真）。
            cards.Sort((a, b) =>
            {
                int c = a.Cost.CompareTo(b.Cost);
                if (c != 0) return c;
                c = CompareDisplayName(CardText.Name(a), CardText.Name(b));
                if (c != 0) return c;
                return string.CompareOrdinal(a.Id, b.Id);
            });

            shown.AddRange(cards);
            shown.RemoveAll(x => x == null);
            return shown;
        }

        // ============================================================ 🆕 **2026-10-25（`C` 卡组编辑域）**
        /// <summary>这张卡是不是**异画**（判据 = 原版 `RawCardScript.IsAlternateArt()`；我们查静态表）。
        /// <para>⚠️ **如实标**：原版那个标记是卡 SO 上的字段；我们的异画是**单独一批卡 id**
        /// （`CollectionWindow.AltArtCards`，7 条）⇒ 这里查表，**语义等价但机制不同**。</para></summary>
        public static bool IsAltArtCard(CardDef d)
        {
            if (d == null || string.IsNullOrEmpty(d.Id)) return false;
            var arr = CollectionWindow.AltArtCards;
            for (int k = 0; k < arr.Length; k++) if (arr[k].CardId == d.Id) return true;
            return false;
        }

        /// <summary>卡组列表**真正要铺的行** = <see cref="DeckEntries"/> ＋ **两个空位占位行**。
        ///
        /// <para>判据 = `DeckListDrawer.ShowEmptySlotsIfNeeded.c:1061-1080`（逐句现读）：
        /// 那两个 `UICardInfoItem` 是**预先摆在抽屉里的行实例**，开关条件是
        /// `SetActive(!deck.HasDefensiveCard)` 与 `SetActive(deck.deckHero == null)`
        /// ⇒ **没督军时第 0 槽留一行空的、没防御卡时那一槽也留一行空的**
        /// （空 = 名字空、不印费用/份数）。原来我们只在有 id 时加那两行 ⇒ 新开的空卡组
        /// **一行都没有**（`Empty Warning` 之外什么都没有）。</para>
        ///
        /// <para>⚠️ **槽序**：原版抽屉里那两行是**常驻**的（前两槽），卡行才另起 ⇒ 我们这里
        /// 槽 0 = 督军（缺 ⇒ `null`）、槽 1 = 防御（缺 ⇒ `null`），其后才是卡。
        /// 缺 id **或 id 查不到**都当空位（后者原版是「那行留着但不画内容」的同一种观感）。</para>
        ///
        /// <para>🔴 **为什么新开一个口、不改 `DeckEntries()`**：后者被 8 处消费（滚动上界 / 行命中 /
        /// 拖拽 / 自检读数），那些地方要的是「**有内容的卡**」；只有「行渲染 + 行命中/拖拽」这一族
        /// 需要空位行。两件事分开，免得把「行数」这一条口径污染到滚动与自检上。</para></summary>
        List<CardDef> DeckRows()
        {
            var rows = new List<CardDef>();
            // 槽 0：督军
            rows.Add(string.IsNullOrEmpty(State.Deck.WarlordId) ? null : State.Find(State.Deck.WarlordId));
            // 槽 1：防御卡
            rows.Add(string.IsNullOrEmpty(State.Deck.DefensiveId) ? null : State.Find(State.Deck.DefensiveId));
            // 槽 2+：卡（跳过已经在前面出现的那两个 id —— 与 `DeckEntries` 同一个口径）
            foreach (var d in DeckEntries())
            {
                if (d == null) continue;
                if (!string.IsNullOrEmpty(State.Deck.WarlordId) && d.Id == State.Deck.WarlordId
                    && ReferenceEquals(d, rows[0])) continue;
                if (!string.IsNullOrEmpty(State.Deck.DefensiveId) && d.Id == State.Deck.DefensiveId
                    && ReferenceEquals(d, rows[1])) continue;
                rows.Add(d);
            }
            return rows;
        }

        /// <summary>行列表里**真正的卡**有几行（空位占位行不算）—— `Empty Warning` 的判据。</summary>
        int DeckRowCardCount(List<CardDef> rows)
        {
            int n = 0;
            if (rows != null) foreach (var d in rows) if (d != null) n++;
            return n;
        }

        void RefreshDeckList()
        {
            bool cards = DeckListPage, info = InfoTab;
            var shown = DeckRows();

            // 🔴 **2026-10-21（`A1721`）**：滚动上界 = 原版 `ScrollRect` 那条 **`Content 高 − 视口高`**，
            //   而 `Content 高 = n × 57.7 − 2.0`（`n` 行 + `n−1` 个 `m_Spacing`）⇒ **要减掉那 2px**
            //   （只改 `RowPitch` 不改这一句 ⇒ 多给 2px；反过来漏了 `− RowSpacing` 就是本条）。
            //   ⚠️ 与 `MaxDeckScrollPx`（自检口）**同一条式子**，两处一起改。
            float maxScroll = Mathf.Max(0f, shown.Count * RowPitch - RowSpacing - ListH);
            _deckScroll = Mathf.Clamp(_deckScroll, 0f, maxScroll);
            int firstRow = Mathf.FloorToInt(_deckScroll / RowPitch);
            float off = _deckScroll - firstRow * RowPitch;

            for (int i = 0; i < RowVisible; i++)
            {
                bool on = cards && (firstRow + i) < shown.Count;
                // 🆕 **2026-10-25（`C` 卡组编辑域）**：这一槽是**空位占位行**（没督军 / 没防御卡）——
                //   原版 `DeckListDrawer.ShowEmptySlotsIfNeeded.c:1061-1080` 就是这两行常驻、按「有没有」开。
                bool emptySlot = on && shown[firstRow + i] == null;
                // 🔴 **2026-10-20（A1527）**：**督军行不印费用圆** —— 这一颗原来只按
                //   「这一行显不显示」开（`SetOn(_deckRowCost[i], on)`）⇒ 督军行也印了一颗 `0`
                //   （实拍 `_tmp_view/play17/D2_deck.png` 第 0 行）。**三条独立判据**：
                //   ① **参考图第 0 行没有费用圆**（`桌面/warpforge参考图/卡组编辑界面参考.png`：
                //      第 0 行 = 督军 —— 只有名字、字更大、无圆、无 `x2`）。
                //   ② **prefab 分三族，只有督军那一族没有这一颗** —— 原版按卡种选预制体：
                //      `DeckListDrawer__SpawnCardInfo.c:29 / :40 / :51`（`card+0x94 == 10` ⇒ Hero 族 ·
                //      `card+0x94 == 0x14 && card+0x90 == 0xd2` ⇒ Defensive 族 · 否则 Card Info 族）；
                //      而 `Deck Selector Hero Card Info button/Content` **根本没有 `Cost Image` 子件**
                //      （`资料/说明书/04_界面UI/菜单全树.md:9321-9329`），
                //      Defensive（`:9337`）与 Card Info（`:9348`）两族**都有**。
                //   ③ 原版那颗的开关 = `UICardInfoItem__Initialize.c:76` 的
                //      `SetActive(coin, (uint)*(int*)(card + 0x78) >> 31 ^ 1)`（= **`manaCost >= 0`**；
                //      `card+0x78` 就是 `RawCardScript.manaCost` —— `资料/AI_原版反编译_0917.md:42`）。
                //   ⚠️ **为什么不能只照抄 `Coin >= 0` 这一条**：我们表里**督军 `cost` 恒 `0`**，
                //      那是**填值、不是原版值**（`工具/gen_cards_engine.py:314-333`：13 阵营
                //      56 张督军卡图**逐张**核过，56/56 右上**没有**蓝色费用六边形，费用才填 `0`）
                //      ⇒ 在我们这份数据上 `cost >= 0` **恒真、一颗都藏不住**；
                //      真正能分辨的仍是 **② 的卡种族**（原版那三族预制体就是按它选的）。
                //   🔴 **防御卡必须【与督军分开判】**：防御卡**卡面上真有费用六边形**
                //      （`d:/2/Warpforge部队卡片/Aeldari/5防御卡/Warpforge_65_Inspiring-Legacy.png` = `0` ·
                //      `Warpforge_67_Aspect-Shrine.png` = `1`，逐张开图核过），它的 `cost` 是**真值**、
                //      且 `>= 0` ⇒ 原版 **Defensive 那一族（有 `Cost Image`）是印出来的**
                //      ⇒ ⛔ 别顺手连防御卡一起藏（那会造出一条新的偏离）。
                //      ⚠️ 若将来真查到「原版 Defensive 族那颗也是负的」，改法只有**一处** ——
                //      把下面那两句的 `!heroRow` 换成 `!(heroRow || rowDefence)`（两处同时改）。
                //   🔴 **两颗一起藏**：我们这一行是**一个模板复用**（原版是三套预制体），
                //      所以「`Cost Image` 那一族没有」这件事**要落成两句** ——
                //      `row_c*`（蓝六边形 = 原版 `Cost Image`）与 `row_cnt*`（圆里那个数 = 原版 `Cost`）
                //      **是两颗独立物体**（`:1413` / `:1441`），只关一颗会在督军行上留一颗**飘着的数字**。
                //   🔴 **2026-10-14（`DIAG_deck七条红.md` ⑥）：上面那条判据【今天才真正生效】。** 光在 4123 那一句
                //      `SetOn(_deckRowCost[i], on && !heroRow)` **还不够** —— 同一趟循环末尾那句
                //      `ClipRowToBand(...)` 的 ① 会 `SetActive(true)` 把刚关掉的圆**又点亮**
                //      （`PieceShared` 原来恒被处理）⇒ **第四实参必须与这一句同值**（已补：`on && !heroRow`）。
                //      旁证：`deck.log` 里 `row_c0` 红（圆亮着）而 `row_cnt0` 绿（数没亮）——
                //      「一开一关」正是「圆在 `_rowPieces` 里、数不在」的形状。
                //   🔴 **2026-10-14（`A2006②` 现核 —— 把「原版会印」那条疑问一次关死）：**
                //      `A2006` 的说法是「原版**会**印（`UICardInfoItem` 无督军分支、只看 `manaCost >= 0`），
                //      而我们反而藏了 ⇒ **可能反向偏离**」。**逐条现核后：这条疑问【不成立】** ——
                //      它只看了解析代码、**没看那份代码跑在哪个预制体上**（「一个值 ≠ 全部情况」）。
                //      三条独立判据（2026-10-14 亲读）：
                //      ① **预制体里根本没有这颗** —— 真包现读（`工具/menu_dump.py bundle_menus_assets_all
                //         "Deck Selector Hero Card Info button" --depth 6`）：
                //         `Deck Selector Hero Card Info button/Content` 的孩子是
                //         Background · Rarity Gradient · Background Border · Card Name · Count · Ban Icon ·
                //         Background Slot Free · Slot Free Text —— **没有 `Cost Image`**；
                //         而 `Deck Selector Card Info button/Content/Background` 与
                //         `Deck Selector Defensive Card Slot/Content` **两族都有**
                //         `Cost Image → Cost`（`资料/说明书/04_界面UI/菜单全树.md:9337` / `:9348`）。
                //      ② **那一格在预制体里就是 `null`** —— 英雄族 `UICardInfoItem.cardCost`
                //         = `{m_FileID: 0, m_PathID: 0}`（`bundle_menus_assets_all/MonoBehaviour/
                //         MonoBehaviour_5640407100770658749.json`），而卡行族那颗指着一个真对象
                //         （同目录 `…6564024808884203300.json` 里 `cardCost` = `-8034090272549376220`）
                //         ⇒ `UICardInfoItem__Initialize` 里那句
                //         `SetActive(cardCost, manaCost >= 0)` **前面还有一道判空**（`op_Implicit`）
                //         ⇒ **英雄族那一段整个不执行**（连 `SetText` 都不会跑）。
                //      ③ 选哪一族由**卡种**定：`DeckListDrawer__SpawnCardInfo.c`（现读）第 29/40/51 行
                //         `card+0x94 == 10`（= `CardTypeOptions.Hero`）⇒ 英雄族。
                //      ⇒ **原版督军行【不印】费用**，我们这两句 `on && !heroRow` **是对的**（`A2006②` 判为**无偏离、不改**）。
                bool heroRow = on && !emptySlot && shown[firstRow + i].Type == "hero";
                // 🔴 **2026-10-21（`A1720`）：槽 0 那**两套件**二选一** ——
                //   本行装的是督军（`heroRow`）就开督军档 `row_0/row_g0/row_b0`，否则开卡行档
                //   `row_0c/row_g0c/row_b0c`。**两个判断只差 `on`**：
                //   · `heroRow` 已经含 `on`（`on && Type=="hero"`）⇒ 督军档直接用它；
                //   · 卡行档要 `on && !heroRow` —— 写成 `!heroRow` 会在**这一槽没有数据**时也开
                //     （`on == false` ⇒ `heroRow == false`）⇒ 空槽上留一条卡行底板（静默）。
                //   ⚠️ 槽 1+ 没有第二套件（它们恒是卡行档）⇒ 仍走原来那两句 `on`。
                bool heroPiece = heroRow || (emptySlot && firstRow + i == 0);
                bool cardPiece = on && !heroRow && !(emptySlot && firstRow + i == 0);
                if (i == 0)
                {
                    SetOn(_deckRowBg[0], heroPiece); SetOn(_deckRowGrad[0], heroPiece);
                    SetOn(_deckRowBorder[0], heroPiece);
                    SetOn(_row0cBg, cardPiece); SetOn(_row0cGrad, cardPiece); SetOn(_row0cBorder, cardPiece);
                }
                else
                {
                    SetOn(_deckRowBg[i], cardPiece); SetOn(_deckRowGrad[i], cardPiece);
                    SetOn(_deckRowBorder[i], cardPiece);
                }
                SetOn(_deckRowCost[i], on && !heroRow);
                SetOn(_deckRowName[i], on);
                SetOn(_deckRowCostText[i], on && !heroRow);
                // 🔴 **2026-10-20**：份数那颗 `row_x*` **先关掉** —— 它开不开由下面算出来的
                //   张数定（原版 `SetCount` 那句 `SetActive(1 < count)`），而张数只有 `on` 的行才有。
                //   ⚠️ 不能只在 `on` 那一支里关：**这些标签不是行那一组的孩子**（都挂在 `Root` 上）
                //   ⇒ 滚动露头那些 `!on` 的行如果不在这里关，屏上会留一颗**孤儿字**。
                SetOn(_deckRowCountText[i], false);
                if (!on) continue;

                var def = shown[firstRow + i];
                float y = ListY + i * RowPitch - off;
                MoveRow(i, y);
                if (emptySlot)
                {
                    // 🆕 **2026-10-25（`C` 卡组编辑域）**：原版 `DeckListDrawer.ShowEmptySlotsIfNeeded.c:1061-1080`
                    //   —— 没督军 / 没防御卡时，那一槽**留一行空的**（底板照画、名字空、**不印费用与份数**）。
                    //   ⛔ 别在这里 `continue` 之前把行底板关掉：原版那一行是**看得见的空槽**（不是隐形）。
                    _deckRowName[i].SetText("");
                    SetOn(_deckRowCost[i], false);          // 蓝六边形（原版 `Cost Image`）
                    SetOn(_deckRowCostText[i], false);      // 圆里那个数（原版 `Cost`）
                    SetOn(_deckRowCountText[i], false);     // 份数（原版 `Count`）
                    ClipRowToBand(i, y, heroPiece, false);  // 第 4 个实参 = 「费用圆该不该显示」⇒ 空位行不带
                    ClipRowLabels(i);
                    continue;
                }
                // 🆕 **2026-10-25（`C` 域 · E 查出的 C-P2 ①）**：**异画卡的名单后面加 `*`** ——
                //   判据 `UICardInfoItem__Initialize.c:116-119`（现读）：`if (card.IsAlternateArt()) text = 名字 + "*";`。
                //   ⚠️ 「是不是异画」在我们这边**没有 `IsAlternateArt()` 这个字段** ⇒ 只能查那份静态异画表
                //   `CollectionWindow.AltArtCards`（7 条，唯一真值）。⛔ 别在别处另抄一张表。
                _deckRowName[i].SetText(CardText.Name(def) + (IsAltArtCard(def) ? "*" : ""));
                // 🔴 **2026-10-20（行名左对齐）**：**左沿的落点在这里** —— `SetAlignLeft()`
                //   只管「真折行时每行在块内怎么排」，把**整块**挪到左沿的是 `AlignLeftOn`
                //   （两个口的分工 → `Battle/Label.cs` 那两段 doc）。
                //   ⚠️ 必须排在 `SetText` **之后**（它要现量 `WorldW`），且**每次刷新都要调**：
                //   `SetText` 会重排、把 x 写回块心（同 `RefreshHeader` 里那一句的理由）。
                _deckRowName[i].AlignLeftOn(LayoutSpace.FromPixel(RowNameX, 0f).x);
                int n = (def.Type == "hero" || def.Type == "defence") ? 1 : State.Deck.CountOf(def.Id);
                // 🔴 **2026-10-20 换了喂进去的东西（两处一起）**：
                //   · 圆里那颗（原版 `…/Cost Image/Cost`）= **费用**（`CardDef.Cost`，字段名现核：
                //     `RuleEngine/Core/CardDef.cs` 的 `public int Cost { get; }`）；
                //   · 行右缘那颗（原版 `Count`）= **份数** `"x" + n`。
                //   ⚠️ **改之前两颗都喂的是份数**（份数印在费用圆里、而**费用哪都没印**）——
                //     这正是本件要修的那一处（原版 `UICardInfoItem` 里 `Cost Image/Cost` 是费用、
                //     `Count` 才是份数：`UICardInfoItem__SetCount.c` 的字面量 = `x{0}`）。
                _deckRowCostText[i].SetText(def.Cost.ToString());
                // ⚠️ 这两句的**先后不能换**：TMP 在**没激活**的物体上量不出尺寸（`textBounds` 是垃圾）
                //   ⇒ 先 `SetText` 再激活的话，这颗字露出来时会被按错误尺寸摆一次（`Battle/Label.cs` 那个已知坑）。
                bool showX = n > 1;                     // 原版：`1 < count` 才显示（督军 / 防御卡行 = 1 ⇒ 藏）
                SetOn(_deckRowCountText[i], showX);
                if (showX)
                {
                    _deckRowCountText[i].SetText("x" + n);
                    // 🔴 **2026-10-11（`A1528` 第二轮）：**墨迹右缘**由**这一句**钉死**（= `RowCountRight` = 317.48）。
                    //   判据 = 原版 `Count` 的 `m_HorizontalAlignment = **4(Right)**` + 它是 `Text fill`
                    //   （`HorizontalLayoutGroup`）的**最后一个**子件 ⇒ 框右沿 = `Text fill` 右沿
                    //   = 行右缘 − 7.917（完整判据 → `RowCountRightInset` 那段）。
                    //   ⚠️ **必须排在 `SetText` 之后**（它要现量 `WorldW`），且**每次刷新都要调** ——
                    //   `SetText` 会重排、把 x 写回块心（同 `_deckRowName[i].AlignLeftOn` 那一句）。
                    //   ⚠️ 原版那一档叫「右对齐」，我们这里用的是**挪整块**的口（`AlignRightOn`）——
                    //   为什么不是 `SetAlignRight`（`Battle/Label.cs` 没这个口）→ 见 `BuildDeckList` 那段。
                    _deckRowCountText[i].AlignRightOn(LayoutSpace.FromPixel(RowCountRight, 0f).x);
                }
                // 🆕 **2026-10-17（附加条）**：行上那**两件**都按稀有度色上色（2026-10-18 `N102` 起 = `CardUIDisplayColorsSO`，见 `Core/RarityColors.cs`） ——
                //   原版 `UICardInfoItem.imagesToChangeColorByRarity` 那两颗数组元素**逐颗解出来就是**
                //   `Rarity Gradient`（= `_deckRowGrad`）与 `Background Border`（= `_deckRowBorder`）。
                //   ⇒ 原来只给色条上了色、边框没上，**两件一起才对**（判据 → `RarityTint` 的 doc ③）。
                var rowRarity = RarityTint(def.Rarity);
                // 🔴 **2026-10-21（`A1720`）**：这两件也走「**现在开着的那一份**」（与行底同一个判据，见 `RowBgGo`）——
                //   ⛔ 原来写死 `_deckRowGrad[i]` / `_deckRowBorder[i]`：槽 0 滚到卡行档时**染的是关着的那一套**，
                //   而真正画出来的 `row_g0c` / `row_b0c` **停在出厂白**（静默：只有槽 0 的色条/描边颜色不对）。
                RowGradGo(i).SetTint(rowRarity);
                TintTree(RowBorderGo(i), rowRarity);         // 九宫格：**整棵树一起**（只染一块会留 8 块白）
                // 🔴 **2026-10-21（`A1727`）：最后一步 —— 把这一行裁到视口内。**
                //   必须排在**搬完（`MoveRow`）+ 定完着色**之后：`ClipRowToBand` 读的是各 quad
                //   **现在这一刻**的世界矩形（`MenuDraw.QuadRectPx` / `ClipCellToBand`），搬之前跑会按旧位裁。
                //   ⚠️ 它只动**几何与可见性**（截矩形 + 截 uv，整块出框的只 `SetActive(false)`），
                //   着色/文字都在上面已经落定 ⇒ 两边互不覆盖。
                // 🔴 **2026-10-14（红 6）**：第 4 个实参 = 「**与档无关那几件（费用圆）该不该显示**」——
                //   必须与上面 `SetOn(_deckRowCost[i], on && !heroRow)` 那句**同一个值**
                //   （⛔ 别写死 `true`：那样督军行的圆会被裁切那一支重新点亮）。
                ClipRowToBand(i, y, heroPiece, on && !heroRow);
                ClipRowLabels(i);
            }

            SetOn(_emptyWarn, cards && DeckRowCardCount(shown) == 0);
            if (_emptyWarnGo != null) _emptyWarnGo.SetActive(cards && DeckRowCardCount(shown) == 0);
            RefreshTabVisibility();

            var curve = State.CostCurve();
            for (int c = 0; c < _curveFill.Count; c++)
            {
                int n = c < curve.Length ? curve[c] : 0;
                // 🆕 **2026-10-17（D33）**：最后一格 = **费用 ≥ 8 全部**（原版把超出的夹进最后一格，
                //   见 `CurveLastBucket` 那段判据）。⚠️ 这一句**必须与 `CurveCostText` 的 `+` 成对**：
                //   只印 `8+` 却不并计数 ⇒ 那行数字比实际少（静默）。
                if (c == CurveLastBucket)
                    for (int j = c + 1; j < curve.Length; j++) n += curve[j];
                float fw = Mathf.Clamp01(n / 10f) * CurveBarW;                   // 原版按 10 张归一化
                SetQuadWidth(_curveFill[c], fw);
                // 🔴 **2026-10-11（A1497 ③）**：宽度是**围绕锚点(0.5)往两边长**的 ⇒ 只调宽度会让柱子
                //   左边跑到槽外。每帧把中心摆回「槽左 + 宽/2」（基线 = 建的时候那个 x，= 槽左 + 0.5）。
                var fq = _curveFill[c];
                if (fq != null && c < _curveFillX0.Count)
                {
                    var lp = fq.transform.localPosition;
                    lp.x = _curveFillX0[c] + LayoutSpace.Px(fw * 0.5f - 0.5f);
                    fq.transform.localPosition = lp;
                }
                // 🔴 左列（`Card Cost`）的字**也在这里写**（原来只在建的时候写过一次）——
                //   两列从此**同一个出处**（`CurveCostText`），`+` 那半边才测得到。
                if (_curveLabel[c * 2] != null) _curveLabel[c * 2].SetText(CurveCostText(c));
                _curveLabel[c * 2 + 1].SetText(n.ToString());
            }
        }

        List<ImageQuad> AllRowParts(int i) { return null; }     // 已废弃（改用显式列表，见上）
        readonly Dictionary<string, ImageQuad> _named = new Dictionary<string, ImageQuad>();
        ImageQuad Lookup(string key) { ImageQuad q; return _named.TryGetValue(key, out q) ? q : null; }

        void MoveRow(int i, float y)
        {
            float cy = y + RowH * 0.5f;
            // 🔴 **2026-10-21（`A1700`）**：矩形**分两档**（与 `BuildDeckList` 同一对函数）——
            //   ⛔ 别在这里写 `ListX + RowW/2`：那是**抽屉**中心，行底/描边会被摆到行外（左右各 8px）。
            // 🔴 **2026-10-21（`A1720`）**：档位 = **这一槽的主套件属哪一档** —— 槽 0 的主套件是
            //   **督军档**（它的卡行档那一套 `row_0c*` **不在这里搬**：几何由 `ClipRowToBand`
            //   按当前 y 现算，`MenuDraw.Local` 会减掉父件自己的位置 ⇒ 根停在哪都不影响落点）。
            bool hero = (i == 0);
            float bgx, bgw, gx, gw;
            RowBgRect(hero, out bgx, out bgw);
            RowGradRect(hero, out gx, out gw);
            Move(_deckRowBg[i], bgx + bgw * 0.5f, cy, QRowBg);
            Move(_deckRowGrad[i], gx + gw * 0.5f, cy, QGrad);         // 右对齐那一段（判据同上）
            Move(_deckRowBorder[i], bgx + bgw * 0.5f, cy, QBorder);
            Move(_deckRowCost[i], RowCostX + RowCostS * 0.5f, cy, QRow);
            // 🔴 **2026-10-20**：这一行原来搬的是 `_deckRowCntIc[i]`（那颗 `row_k*` 方块贴图，已删）；
            //   现在是行右缘那颗**份数**字。🆕 **2026-10-11（`A1528` 第二轮）**：喂的是
            //   **框中心**（`RowCountX + RowCountW/2` = **292.48**），而**真正的 x 随后会被
            //   `RefreshDeckList` 里那句 `AlignRightOn` 覆盖**（右沿钉在 `RowCountRight` = 317.48）——
            //   这一句现在只负责 **y 与渲染队列**（与下一行行名同一个口径）。
            MoveLabel(_deckRowCountText[i], RowCountX + RowCountW * 0.5f, cy, QText);
            // 🔴 2026-10-20：这一句只管 **y 与渲染队列**（喂的是框心）——
            //   **x 随后会被 `RefreshDeckList` 里那句 `AlignLeftOn` 覆盖**（行名现在是左对齐）。
            MoveLabel(_deckRowName[i], RowNameX + RowNameW * 0.5f, cy, QText);
            MoveLabel(_deckRowCostText[i], RowCostX + RowCostS * 0.5f, cy, QText);
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

        /// <summary>卡组行的**行矩形**（画布 px）—— 命中判定用它，**别拿九宫格里某一块 quad 的尺寸**。
        /// 🔴 **2026-10-21（`A1700`）**：左缘/宽改用**行节点**那一档（`RowNodeX` / `RowNodeW` = 8.4 / 309），
        /// 不再是**抽屉**的 `ListX` / `RowW`（0.4 / 325）。判据 = 原版 prefab 的 `Content` 有
        /// `VerticalLayoutGroup pad (8,8,0,0)`，而那颗 `EverguildButton` 挂在**行节点**上
        /// （见 `RowNodeX` 那段）。⚠️ **行底板的左右缘与它不再是同一个数**（卡行底板还要再内缩 17.255，
        /// 督军行不缩）—— 命中区一律按**行节点**算，与画法无关。</summary>
        bool RowRectAt(int i, out float x, out float y)
        {
            x = RowNodeX; y = ListY + i * RowPitch - _deckScroll;
            return y + RowH > ListY && y < ListY + ListH;
        }

        /// <summary>一个世界点是否落在第 `i` 行里（**按行矩形**判，与画法无关）。</summary>
        bool HitRow(int i, Vector3 wp)
        {
            float x, y;
            if (!RowRectAt(i, out x, out y)) return false;
            var p = ToPx(wp);
            return p.x >= x && p.x <= x + RowNodeW && p.y >= y && p.y <= y + RowH;
        }

        // ---- 🆕 2026-10-04（A24）卡组行的悬停变暗（判据见 `RowHoverK`）----

        /// <summary>指针底下那一行（`-1` = 没有）。只在 **Cards 页 + 没在拖** 时成立
        /// （其余情况下那几行要么不显示、要么被模态挡着）。</summary>
        int RowUnder(Vector2 px)
        {
            if (!DeckListPage || _dragging) return -1;
            if (px.x < RowNodeX || px.x > RowNodeX + RowNodeW) return -1;
            // 🔴 **2026-10-21（`A1727`）**：**射线也要吃那道裁切**（`D5` 留的「没查清」本条就地结清）。
            //   判据 = 原版那颗视口上是 **`RectMask2D`**（本批现读 `工具/menu_dump.py "Deck List drawer" --depth 5`：
            //   `2 Viewport 0.4 366.0 325.4 1010.0 … RectMask2D,Image`），而 **`RectMask2D` 四边都裁：
            //   渲染走 `IClipper`、射线走 `IsRaycastLocationValid`**（同 `Shell/MenuScroll.cs` 的 `ClipNode`
            //   那段判据；⛔ 与「`Mask`（模板/`showGraphic`，只裁渲染那一半）」是**两种**，别混）。
            //   ⇒ 压在**视口外那一段残行**上的指针**不该命中任何行**（不挡的话：滚到非 0 时指针停在
            //   页头上、而第 0 行的**未裁切**矩形伸进来 ⇒ 悬停会亮一行「其实没画出来」的行底）。
            if (px.y < DeckListBand.y1 || px.y > DeckListBand.y2) return -1;
            for (int i = 0; i < _deckRowBg.Count; i++)
            {
                var bgGo = RowBgGo(i);
                if (bgGo == null || !bgGo.activeSelf) continue;
                float x, y;
                if (!RowRectAt(i, out x, out y)) continue;
                if (px.y >= y && px.y <= y + RowH) return i;
            }
            return -1;
        }

        /// <summary>🔴 **2026-10-21（`A1720`）：第 `i` 行【现在开着的那一份行底】** ——
        /// 槽 0 有两套（督军档 `_deckRowBg[0]` / 卡行档 `_row0cBg`），其余槽只有一套。
        /// <para>为什么要有它：悬停那三处（命中 `RowUnder` / 上色 `ApplyRowHover` / 自检读数 `UiRowBgTint`）
        /// 原来一律读 `_deckRowBg[i]` —— 槽 0 滚到卡行档时那一份是**关着的** ⇒
        /// ① 命中会**跳过第 0 行**（指针压在它上面却判「没有行」）；② 上色会去染一颗看不见的件。
        /// ⇒ 三处**共用这一份**（⛔ 别再各写一遍「i==0 就用另一颗」）。</para></summary>
        GameObject RowBgGo(int i)
        {
            if (i < 0 || i >= _deckRowBg.Count) return null;
            if (RowIsCardPiece(i)) return _row0cBg;
            return _deckRowBg[i];
        }

        /// <summary>槽 0 现在用的是**卡行档**那一套吗（= 行底那一份开着）。
        /// <para>⚠️ **判据取「行底那一份根节点的 `activeSelf`」**，⛔ 不是取色条/自己的 `activeSelf` ——
        /// 那几颗会被 `ClipCellToBand` **单独关掉**（整块滚出视口时），拿它判档会把「被裁掉了」
        /// 误读成「换了档」（静默指向另一套件）。九宫格**根节点**只有 `SetOn` 会关 ⇒ 它是干净的判据。</para></summary>
        bool RowIsCardPiece(int i) { return i == 0 && _row0cBg != null && _row0cBg.activeSelf; }

        /// <summary>第 `i` 行**现在开着的那一份**稀有度色条（同 `RowBgGo` 的道理）。</summary>
        ImageQuad RowGradGo(int i)
        {
            if (RowIsCardPiece(i)) return _row0cGrad;
            return (i >= 0 && i < _deckRowGrad.Count) ? _deckRowGrad[i] : null;
        }

        /// <summary>第 `i` 行**现在开着的那一份**描边（同 `RowBgGo` 的道理）。</summary>
        GameObject RowBorderGo(int i)
        {
            if (RowIsCardPiece(i)) return _row0cBorder;
            return (i >= 0 && i < _deckRowBorder.Count) ? _deckRowBorder[i] : null;
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
            var shown = DeckRows();
            int firstRow = Mathf.FloorToInt(_deckScroll / RowPitch);
            for (int i = 0; i < _deckRowBg.Count; i++)
            {
                // 🔴 **2026-10-21（`A1720`）**：染**现在开着的那一份**行底（槽 0 两套 —— 见 `RowBgGo`）。
                var go = RowBgGo(i);
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
        /// 遮挡口径也照点击那条路（`HandlePointer` 里那几条早退）：左抽屉开着时它盖住的那条竖带不认；
        ///   行拖拽中不认；模态件与普通件的互斥收在 `HitBtn` → `KeyLive`（唯一出处）。</summary>
        WindowButton HoverTargetUnder(Vector2 px)
        {
            if (_dragging) return null;
            if ((FltStripOn || CosmoFltStripOn) && px.x < FltX + FltW) return DrawerHoverUnder(px);
            foreach (var k in ClickOrder)
            {
                WindowButton wb;
                if (!_hoverBtns.TryGetValue(k, out wb)) continue;   // 只认接了悬停换图的那几颗
                // 🔴 **成组显隐的件由 `HitBtn` → `KeyLive` 判**（唯一出处；2026-10-17 T23 删件之后
                //   本窗只剩 `hdr_clear` 那一支）—— 悬停与点击都走它，这里不用再判一次
                //   （两处写同一条规则 = 迟早不一致）。
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
            // 🔴 **2026-10-12（A365）：`Validate()` 必须先跑** —— 它**有副作用**：卡组合法时会把
            //    空名字补成**督军卡名**（原版 `DeckUtility__ValidateDeck.c:70-76`，逐条见
            //    `DeckEditorState.Validate` 的注释）。名字那两件（文本 + 占位提示）在它**之后**取，
            //    否则这一帧还印着空名字、下一帧才跳过来。下面 `foot_hl` 用的就是**同一个** err
            //    （一次调用两处用 —— 与 A330 那条「灯亮 ⟺ 放行是同一判据」一致）。
            var err = State.Validate();

            _deckNameText.SetText(State.Deck.Name);
            _deckNameHint.gameObject.SetActive(string.IsNullOrEmpty(State.Deck.Name));
            // 🔴 **2026-10-17（D34）**：**左对齐那半边的【位置】在这里落** —— `SetAlignLeft()` 只管
            //   「真折行时每行在块内怎么排」，**移块**的是 `AlignLeftOn(左沿世界 x)`（同 `Battle/Label.cs`
            //   那条「两个口管两件事」的口径）。⚠️ 必须在 `SetText` **之后**调（它要量 `WorldW`）。
            //   ⚠️ 每次刷新都要调：`SetText` 会重排、把 x 写回框中心（同 `RefreshFilterTitles` 那条）。
            _deckNameText.AlignLeftOn(LayoutSpace.FromPixel(NameTxX, 0f).x);
            _deckNameHint.AlignLeftOn(LayoutSpace.FromPixel(NameTxX, 0f).x);
            // 🔴 **2026-10-17（D7 删件）**：这里原来还有一句 `_title.gameObject.SetActive(!_filtersOpen);`
            //   —— 那颗「卡组编辑」标题**原版没有**、已删（判据 → D7）。

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

            // 🔴 **2026-10-20：`Clear filters` 的显隐落在这里**（判据/推断/为什么走 `SetActive`
            //   而不是原版那个 −550 平移 → 全部写在 `HdrClearX` 上面那一大段注释里，⛔ 别在这里再抄一份）。
            //   · 判据 = **本文件已有的那个「筛选是不是空的」读法**：`DeckFilter.IsEmpty`
            //     （`Deck/DeckEditorState.cs`，五个筛选字段全空；**不含 `Owned` / `Upgradable`** 那两个开关）
            //     —— ⛔ 别另造一个「有没有筛选」的概念。
            //   · **与它自己的动作同一条判据**：`case "hdr_clear": ClearFilters()` 清的正是 `State.Filter`
            //     那五件 ⇒ 这颗钮「管的是卡牌那一套筛选」，所以显隐也读 `State.Filter`。
            //     ⚠️ 卡背页（`_tab == 2`）那一份 `_cosmoFilter` 算不算数 —— **判据不足**（原版只有一颗钮、
            //     而它归哪个 controller 没读出来）⇒ 今天**只管卡牌那份**，如实标注在这里。
            //   · 🔴 **命中闸必须跟着走**（`KeyLive` 里那一支）：藏起来却还能点到 = 静默失败（本仓红线）。
            bool clearFiltersOn = !State.Filter.IsEmpty;
            SetOn(_hdrClear, clearFiltersOn);
            SetOn(_hdrClearTx, clearFiltersOn);

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
            //      （`PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Graphic.cs` 里那一族）
            //      ⇒ **只动 alpha，RGB 两个态都保持红**（本机 uGUI `…/UI/Core/Toggle.cs` 的 `Toggle.DoStateTransition`）。
            //   同工程早就做对过一次：`Shell/MenuWindowBase.cs` 里那处 `Highlight` + `(1,0,0,1)` 用同一张图 + `(1,0,0,1)`。
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
            // 🆕 **`A1514`**：类型计数抽屉（部队 / 策略）与上面那颗**同一拍**重算 ——
            //   判据 = 原版 `DeckEditingPanel__UpdateDrawers.c`（它无条件依次 `Initialize` 三个抽屉，
            //   其中就有 `DeckTypeCounterDrawer`；见类级 `Tc*` 常量那一段）。
            RefreshTypeCounter();
            // ⚠️ `err` 在**本函数第一句**就算好了（A365 起：`Validate()` 有「补空名字」那个副作用，
            //    名字文本得排它后面）—— ⛔ 别在这里再调一次 `State.Validate()`。
            // 🔴 **2026-10-17（D35/D10 删件）**：这里原来还有两行 ——
            //   `_verdict.SetText(...)` / `_verdict.SetColor(...)`（页脚那行「合法/不合法」判词，D35）
            //   与 `_storeErr.SetText(Library.LastError ?? "")`（底部报错横幅，D10）。
            //   两件**原版都没有** ⇒ 已删。⚠️ `err` 这个量**仍然要用**（下面 `foot_hl` 那颗灯），别把上面那句删了。
            //   ⚠️ `Library.LastError` 的出口没丢：`SaveAndSay()` 会 `Say(...)` 念出来
            //      （`DeckScene` 的 A398/A547 读的就是那条）。
            // 🔴 **A330（2026-10-11）**：这一句与 `SaveAndSay()` 那道**保存闸**用的是**同一条判据**
            //   （原版 `__UpdateDoneButton.c:24` 与 `__TrySaveDeck.c:80` 是逐参数相同的同一个
            //   `DeckUtility.ValidateDeck(deck, out err, 1, 0)`）⇒ **灯亮 ⟺ Done/ESC 会真的存下去**。
            ToggleDoneHighlight(err == DeckError.None);     // 原版 `Done Highlight` 就是这个开关（2026-10-27 接线：淡入淡出）
            // 同一判据、另一个载体：Done 那颗按钮上的 **Shiny 扫光**（原版 `DeckEditingWindow.UpdateDoneButton`
            // 里 `controller.Toggle(ValidateDeck(...), …)` ⇒ **合法才扫**，而且那颗是 `m_WrapMode 1 (Loop)` 一直扫）。
            FootShine(err == DeckError.None);

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

            // 🔴 **2026-10-17（A893）**：页头那颗 `hdr_army` 要**跟着这副卡组的阵营换**（判据与出处
            //   写在 `BuildHeader` 那一行 + `DeckArmyIcon` 的 doc 上）—— 换督军/清督军都会走到这里
            //   （`TryAddCard` → `MarkDeckDirty()` → 本函数；`ClearWarlord` 那条链同理走 `RefreshAll`）。
            //   `SetSprite` **同一张图不再设**，所以这里每次刷新调它不花代价。
            SetSprite(Lookup("hdr_army"), DeckArmyIcon(State));
            // 🔴🔴 **2026-10-25（`C` 卡组编辑域）：原版还有一条「跟着列表走」的换图路径 —— 本轮补上。**
            //   判据 = `DeckEditorCollectionDisplay__CheckFocusedArmy.c`（与收藏窗 `CardCollectionDisplay` 同形：
            //   `RectExtensions.OverlapsAny(referenceCardPoint, Cells 的 RectTransform, out 命中格)` ⇒
            //   `WildcardDisplay.Initialize(命中格那张卡的 cardArmy)`），入口 = `…__OnListDrag.c`
            //   （`OnListDrag(Vector2)` ⇒ `CheckFocusedArmy()`）。
            //   ⚠️ **必须在上面那句之后调**（那一句是「这副卡组的阵营」那条稳定态；本句按滚动位置**覆盖**它）。
            //   ⛔ 早先 `DeckArmyIcon` 的 doc 里写着「本笔没做」—— **本句就是那个「没做」的补齐**（铁律 5）。
            // 🔴 **2026-10-17（D9 删件）**：这里原来还有一句 `_poolInfo.SetText("卡池 N / M 张")`
            //   —— 卡池左上那块**黄色水印**（图 `pool_info_bg` + 字 `pool_info`）**原版没有**、已删
            //   （它是自检读数的可视化；读数走 `State.VisibleCards()` / `UiPoolCardAt(...)`，不需要画在屏幕上）。
            CheckFocusedArmy();
        }

        // ============================================================ 🆕 **2026-10-25（`C` 卡组编辑域）**
        /// <summary>卡组编辑窗的 `Reference Card Pointer` 参考矩形。
        /// 判据 = `python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 4 --no-sprite`
        /// 现读（2026-10-25）：`Card Display ▸ Scroll View ▸ Reference Card Pointer` =
        /// **417.7,261.0 → 611.5,306.0**（193.84×45.09，`INACT` = 不画但矩形照用）。
        /// ⚠️ 本组数与我们的卡池视口 `PoolX/PoolY = 330.2,156` **同一帧**（同一份 dump 的 `Scroll View` 就是
        /// 330.2,156.0）⇒ 可直接与 `UiPoolCellRect` 的返回值比，⛔ 不要加父链偏移。
        /// 🔴 **注意别和收藏窗那一颗混了**：收藏窗（`CardsTab`）的是 **402.3,236.4 → 596.1,281.4**
        /// （见 `Shell/CollectionWindow.WcRefL/T/R/B`）—— K3 报告里记成 417.69,260.95→611.53,306.05 那一组
        /// **其实是本窗（卡组编辑）这一颗**，两个 prefab 各有一颗、不是同一颗的两种 dump 口径。</summary>
        public const float PoolRefL = 417.7f, PoolRefT = 261.0f, PoolRefR = 611.5f, PoolRefB = 306.0f;

        /// <summary>原版 `DeckEditorCollectionDisplay.CheckFocusedArmy` 的等价物：
        /// 拿固定参考矩形与**当前在屏的卡池格**求交，**第一个**相交那格的阵营交给页头 `hdr_army`。
        /// <para>命中序 = `RectExtensions.OverlapsAny` = **按传入序返回第一个相交的**（两份反编译一致，
        /// 见 `Shell/CollectionWindow.CheckFocusedArmy` 的 doc）。⚠️ **本窗不像收藏窗那样「最多命中一格」**：
        /// 参考矩形横向 `417.7…611.5` 压过第 0/1 列分界（`330.2 + 262.5 = 592.7`）约 19px
        /// ⇒ 同屏可能有**两格**同时相交；我们按**可见序**（`_poolViews[0]` 恒为最上一行）取第一个，
        /// 而原版按 `RecyclableScrollRect.Cells` 的**池序**（滚动后是环形的，可能取到另一格）——
        /// 差别只在「正好压在列分界上」那一小段，**如实记**（不为了逐位对齐去模拟回收池的旋转）。</para>
        /// <para>未命中 ⇒ **什么都不做**（原版 `CheckFocusedArmy` 走完循环直接返回 ⇒ 保留上一次那张；
        /// 而上面那句 `DeckArmyIcon` 已经把稳定态铺好了）。</para></summary>
        void CheckFocusedArmy()
        {
            var q = Lookup("hdr_army");
            if (q == null) return;
            var list = State.VisibleCards();
            for (int i = 0; i < _poolViews.Count; i++)
            {
                if (!UiPoolCellRect(i, out float cx, out float cy, out float w, out float h)) continue;
                float x1 = cx - w * 0.5f, x2 = cx + w * 0.5f;
                float y1 = cy - h * 0.5f, y2 = cy + h * 0.5f;
                // 原版 `RectExtensions.Overlaps` 是**严格不等**的 AABB（贴边不算相交）
                if (!(x1 < PoolRefR) || !(PoolRefL < x2)) continue;
                if (!(y1 < PoolRefB) || !(PoolRefT < y2)) continue;
                int ci = UiPoolSlotCardIndex(i);
                if (ci < 0 || ci >= list.Count) return;      // 空槽/越界 ⇒ 当「没命中」
                SetSprite(q, FactionIcon(list[ci].Faction));
                return;                                       // ★ 第一个命中的
            }
        }

        void Say(string s)
        {
            _noticeText = s ?? "";
            // 🆕 2026-09-24：**打一条日志** —— 这行字以前只画在屏幕上，日志里看不见，
            //    于是「点了一下、屏幕上闪了一句」在 `ClickLog` 的「实际触发了什么」那一栏里是**空的**。
            // 🔴 **2026-10-17（D35 删件）**：**屏幕上那行字（`notice`）已删**（原版侧栏没有它）——
            //    本函数现在的产物 = 这份 `_noticeText` 状态 + 下面这条日志（自检读 `UiLastSay`）。
            //    ⛔ 别顺手把 `Debug.Log` 也删了 —— 那就是**静默失败**。
            Debug.Log("[Deck] " + _noticeText);
        }

        /// <summary>自检口：最近一次 <see cref="Say"/> 说了什么（屏幕上的那行字已按 D35 删掉，
        /// 「说过话没有」只能从这里读）。空串 = 还没说过。</summary>
        public string UiLastSay { get { return _noticeText; } }

        /// <summary>自检口：某个具名 quad **现在贴的是哪张图**（`null` = 没建 / 没图）。
        /// 与 `UiFilterCellTex` 同一形状，只是那一份读的是**格子**（不进 `_named` 的那批）。</summary>
        public string UiQuadTex(string key)
        {
            var q = Lookup(key);
            return q != null && q.Texture != null ? q.Texture.name : null;
        }

        /// <summary>🆕 **2026-10-17（附加条）**：自检口 —— 第 `i` 行**那两件**现在染的是什么色
        /// （色条 `Rarity Gradient` + 边框 `Background Border`；原版 `imagesToChangeColorByRarity` 那两颗）。
        /// `false` = 这一行没建出来。⛔ 期望值不许从这里读回来 —— 判据是 `Core/RarityColors.cs` 里那张 `CardUIDisplayColorsSO` 的六个字面量。</summary>
        public bool UiRowRarityTint(int i, out Color grad, out Color border)
        {
            grad = border = Color.white;
            if (i < 0) return false;
            bool ok = false;
            if (i < _deckRowGrad.Count && _deckRowGrad[i] != null) { grad = _deckRowGrad[i].Tint; ok = true; }
            if (i < _deckRowBorder.Count && _deckRowBorder[i] != null)
            {
                var q = _deckRowBorder[i].GetComponentInChildren<ImageQuad>(true);
                if (q != null) { border = q.Tint; ok = true; }
            }
            return ok;
        }

        /// <summary>自检口：整条 shell 顶栏那几件（D13/D14）。`null` = 没建出来（**那条断言必红**）。</summary>
        public TopBar.Parts UiTopBar { get { return _topBar; } }
        /// <summary>自检口：顶栏立绘跟着 `ProfileData.AvatarIndex` 换一次
        /// （批处理没有帧循环 ⇒ `Update` 不跑，走**与 `Update` 同一个函数**）。</summary>
        public void UiRefreshTopAvatar() { TopBar.RefreshTopAvatarIfChanged(_topBar, NoteMissingArt); }

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

            // ---- 🆕 2026-10-17：**两条拖拽链**（原版 `DraggableController<T>` 的两个实例）----
            //  ⚠️ 位置在这一段是死的：必须排在**重建界面那些早退之前**、又排在下面那串点击派发**之前**
            //     （拖拽中不许再让点击链吃事件 —— 同 `_dragging` 那一支的形状）。
            if (_cosmDrag != null && _cosmDrag.Dragging)
            {
                _cosmDrag.OnDrag(DragEvent(px, Vector2.zero, false));          // 原版 `OnDrag`：预览贴指针
                if (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed)
                    { EndCosmeticDragAt(px, Vector2.zero); return; }           // 原版 `OnEndDrag`：关预览 + 投递
                return;
            }
            if (_cardDrag != null && _cardDrag.Dragging)
            {
                _cardDrag.OnDrag(DragEvent(px, Vector2.zero, false));
                if (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed)
                    { EndCardDragAt(px, Vector2.zero); return; }
                return;
            }

            // ---- 🆕 已经按下的那两种「等一个位移」（原版 = 格子上 `Draggable` 起拖 → relay → `CheckXxxDrag`）----
            //  判据两跳：① 位移够大（与行拖出同一条 `0.15` 世界单位 ≈ 16.2 设计像素）
            //            ② `IsScrollDragThreshold` 说这不是滚动。
            //  ⛔ 松手时的处置**两支不同**：卡背 —— **什么都不做**（原版左键就是什么都不做）；
            //     卡牌 —— **弹详情窗**（原版左键 = `OnCardClick`，而 uGUI 的 click 是**抬起**那一帧才发）。
            if (_cosmArmName != null || _cardArmDef != null)
            {
                bool held = mouse.leftButton.isPressed && !mouse.leftButton.wasReleasedThisFrame;
                if (!held)
                {
                    Vector2 d0 = px - _armPx;
                    if (_cardArmDef != null && Mathf.Abs(d0.x) <= ArmPxSlop && Mathf.Abs(d0.y) <= ArmPxSlop)
                        OpenCardWindow(_cardArmDef);                             // 没怎么动 = 点击（照原版）
                    _cosmArmName = null; _cardArmDef = null;
                    return;
                }
                Vector2 d = px - _armPx;
                if (Mathf.Abs(d.x) > ArmPxSlop || Mathf.Abs(d.y) > ArmPxSlop)
                {
                    string armedName = _cosmArmName;
                    CardDef armedDef = _cardArmDef;
                    Vector2 from = _armPx;
                    _cosmArmName = null; _cardArmDef = null;
                    bool started = armedName != null ? BeginCosmeticDragAt(from, d)
                                                     : BeginCardDragAt(from, d, armedDef);
                    if (!started)
                        Debug.Log("[Drag] 没起拖 —— "
                                  + (IsScrollDragThreshold(d)
                                     ? ("位移判成**滚动**（与水平轴夹角 " + Vector2.Angle(d, Vector2.right).ToString("F1")
                                        + "° 落在 (60°,120°) 里；判据 = 原版 `SupportMethods.IsScrollDragThreshold`）")
                                     : "这一拖本来就不合法（原版这一支弹错误消息，见上一条 `Say`）"));
                }
                return;
            }

            if (!downL && !downR) return;

            // 🆕 **2026-10-19（第五十六会话 · `A1701`）诊断探针重新插回 —— 只打印、⛔ 不改任何行为。**
            //   用途：查「卡组编辑里**要先滚动一下才能点/拖**」到底是**哪一道闸**把它挡住的。
            //   位置是**故意**选的 —— 在**所有闸之前**（`ModalPopupOpen` / `OtherWindowUp` / 两条抽屉），
            //   所以**任何一条闸为真它也照样打**：拿它的读数与下面 `HandlePoolClick` 里「到了卡池」那一行
            //   **成对**看，就能分开「被闸拦下」（只有第一行）与「到了但没命中」（两行都有）。
            //   ⚠️ 真机复现要走**真来路**（直起 `-wfscene DeckEditor` 没有壳 ⇒ `OtherWindowUp()` 那条闸
            //     读的是 `WindowsManager.Instance`，直起时恒 null ⇒ 复现不出来）。
            //   📌 原探针 2026-10-17（`N17`）被删过；本次按 `资料/普查产出_第二十四会话/D2_卡组编辑线施工件.md`
            //     §三 的判别表**原样重插**，只去掉 `import=` 那一格（导入弹窗 2026-10-17 已随 `T23` 整棵删除，
            //     `_importOpen` 字段已不存在）。
            //   🧹 真机读数定案之后，这两处 `Debug.Log` 连注释一起删掉（它只是探针，不是功能）。
            if (downL || downR)
            {
                var ow = OtherWindowUp();
                Debug.Log("[A1701] px=(" + px.x.ToString("F1") + "," + px.y.ToString("F1") + ")"
                  + " 窗=" + (ow != null ? ow.name : "-")
                  + " modal=" + ModalPopupOpen + " otherWin=" + (ow != null)
                  + " flt=" + FltStripOn + " cosmoFlt=" + CosmoFltStripOn
                  + " | dragRow=" + _dragging + " dragCosm=" + (_cosmDrag != null && _cosmDrag.Dragging)
                  + " dragCard=" + (_cardDrag != null && _cardDrag.Dragging)
                  + " armed=" + (_cosmArmName != null || _cardArmDef != null)
                  + " | inPoolX=" + (px.x >= PoolX && px.x <= PoolX + PoolW)
                  + " rawMouse=" + mouse.position.ReadValue().ToString("F1") + " focus=" + Application.isFocused);
            }

            // 🆕 **2026-10-12（A364）：模态消息窗开着 ⇒ 本窗**不吃点击**（点哪儿都不穿透）。
            //   判据 = 原版那扇窗的 `Menu Dark Background` 是 **`m_RaycastTarget = 1` 的整屏图**
            //   （4574.6×2572.36）⇒ UGUI 那一层就把下面的命中全吞了；我们这套没有 UGUI，
            //   等价物 = 这里早退 + `MenuDraw.Absorb` 那颗吸收命中区（`Shell/PopUpGameWindow.cs`）。
            //   ⚠️ 与（已删的）导入弹窗那条模态口径**同一个位置、同一种形状**（T23），
            //   ⛔ 别在这儿转发到弹窗那两颗钮上 —— 它们归 `PointerLayer` 派发（两处派发 = 迟早不一致）。
            //   🔴 改坏法：删掉这一句 ⇒ `Editor/DeckScene.cs` 的「A364 ★ 弹窗开着时点下面的钮**没人吃**」红。
            if (ModalPopupOpen) return;
            // 🆕 **2026-10-17（B25①）**：**别处有窗开着**（顶栏那 4 颗钮开出来的那几扇 / 任何 `WindowsManager`
            //   的窗）⇒ 本窗让位（判据与「为什么」→ 上面 `OtherWindowUp` 那一段长注释）。
            //   ⚠️ 位置就在 `ModalPopupOpen` 那条闸**旁边**（同一个形状、同一个位置）—— 它高一层：
            //     模态消息窗由本类自己管，别的窗**本类一概不碰**（它们自己有 `PointerLayer` 派发）。
            if (OtherWindowUp() != null) return;

            // 🔴 **2026-10-17（T23 删件）**：这里原来还有「导入弹窗是模态 ⇒ 只认它自己」那道闸
            //   （`if (_importOpen) { if (downL) HandleButtons(px); return; }`）—— 那扇窗整棵删了
            //   （见 `BuildImportPopup` 那一节）。

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
            // Cosmetics 页的卡背格：**右键装备 / 左键记下起点**（原版 `OnCosmeticClick` 只认右键；
            //  左键那一下原版**什么都不做**，我们用它当**拖拽那条路**的起点，见 `HandleCosmeticClick`）
            if (HandleCosmeticClick(px, downR)) return;
            if (downL && HandleButtons(px)) return;
        }

        /// <summary>卡池：**左键放大 / 右键加牌**（照原版 `DeckEditingWindow__OnCardClick.c:16,42`）。
        /// 🆕 2026-10-17：左键改成**按下只记起点、抬起才弹放大窗**（uGUI 的 click 本来就是**抬起**那一帧发；
        /// 而且按下到抬起之间**拖出去 = 加牌**那条路，原版 `CheckCardDrag`）—— 见下面 `_cardArmDef` 那两句。
        /// <para>🔴 **2026-10-13（A1702 ③）：命中要先过视口那道框**（见方法体头两句 + <see cref="PoolViewClip"/>）。
        /// 原版那颗 `RectMask2D` 把**射线**一起裁掉了 ⇒ 滚出视口的卡**不画也点不到**；
        /// 我们原来只判 `px.x`（左右） ⇒ 滚到视口上沿之外的卡**照样点得到**（点页头空带会弹出那张卡的详情窗）。</para></summary>
        bool HandlePoolClick(Vector3 wp, Vector2 px, bool right)
        {
            // 🆕 **2026-10-19（第五十六会话 · `A1701`）诊断探针重新插回 —— 只打印、⛔ 不改行为**
            //   （成对的那一行在 `HandlePointer` 里）。它答的是「**点击链走到卡池这一站了没有**」：
            //   上面那行有、这行没有 ⇒ 被某道闸拦下了；两行都有 ⇒ 到了卡池这一站，只是**没命中**
            //   （点在格与格之间的空处 / 卡被视口框挡了）。
            //   ⚠️ 不加任何条件：本方法**只从 `HandlePointer` 的按下那一支**进（`downL || downR` 已在那头上判过）。
            //   🧹 真机读数定案之后连注释一起删。
            Debug.Log("[A1701] 到了卡池 px=(" + px.x.ToString("F1") + "," + px.y.ToString("F1") + ")"
                      + " right=" + right
                      + " inViewport=" + (px.x >= PoolX && px.x <= PoolX + PoolW
                                          && px.y >= PoolY && px.y <= PoolY + PoolH));

            // 🔴 **2026-10-13（A1702 ③）**：四边框都要判（原来**只有 `px.x` 那一条**）。
            //   判据 = 与渲染那一刀**同一个矩形**（`PoolViewClip`）⇒ 两处不会错开。
            //   ⚠️ 断在**指针**上、不是断在卡上：一半露在视口里的卡，点它**露出来的那半**仍然算命中
            //     （原版 `RectMask2D` 的射线裁剪就是这个语义 —— 它裁的是「能不能收到这一点」）。
            //   🔴 改坏法：删掉下面第二句 ⇒ `Editor/DeckScene.cs` 的「A1702③ 灭自证②」红。
            if (px.x < PoolX || px.x > PoolX + PoolW) return false;
            if (px.y < PoolY || px.y > PoolY + PoolH) return false;
            // 🔴 **2026-10-21（`A1740`）**：原来是 `for (i &lt; _poolViews.Count)` 逐格问
            //   —— 卡池是规则网格 ⇒ 直接**反解**出「指针在第几格」只问那一格
            //   （等价性三条证明 → <see cref="PoolCellAt"/> 的 doc；本方法**只在按下那一支进**，
            //    本来就不是每帧的活，顺带一起收口）。
            // 🔴 2026-10-11 修（用户实测「卡组编辑里点空白处 ⇒ NullReferenceException」· 见账 `A1495`）：
            //   `_poolViews` 里**可以合法地存在 `null` 槽** —— `RefreshPool` 是**先 `Add(null)` 再决定建不建视图**
            //   ⇒ 「先取 `v`、逐条判空」这个次序本身没变，⛔ 别把 `v.gameObject` 排进 `||` 链的第一个。
            int i;
            if (!PoolCellAt(px, out i)) return false;
            if (i < 0 || i >= _poolViews.Count) return false;
            var v = _poolViews[i];
            if (v == null || !v.gameObject.activeSelf || i >= _poolIndex.Count || _poolIndex[i] < 0) return false;
            if (!v.Contains(wp)) return false;
            var def = State.VisibleAt(_poolIndex[i]);
            if (def == null) return true;
            if (right) TryAddCard(def);
            // 🆕 **2026-10-17（卡牌那一支的起点）**：左键按下**不再立刻弹详情窗** ——
            //   原版「加牌有两条路」（右键点 / **拖拽**），而 uGUI 的 click 是**抬起**那一帧才发
            //   （`IPointerClickHandler`；本仓既有口径见 `BattleDriver` 里 `IPointerClickHandler` 那条）
            //   ⇒ 我们这里也改成「先记下，抬起时若没怎么动才弹窗」（与卡组行那条拖拽**同一种形状**）。
            //   ⚠️ 不变的是：**右键那条路仍然是按下就加牌**（它是原版的主力路，别动）。
            else { _cardArmDef = def; _armPx = px; }
            return true;
        }

        /// <summary>卡组条目：**左键放大**（原版**不是删除**）；按住拖出侧栏 = 删除。</summary>
        bool HandleDeckRowClick(Vector3 wp, Vector2 px, bool left)
        {
            if (!DeckListPage) return false;
            if (px.x > RowNodeX + RowNodeW || px.y < ListY || px.y > ListY + ListH) return false;
            for (int i = 0; i < _deckRowBg.Count; i++)
            {
                // 🔴 **2026-10-21（`A1720`）**：读**现在开着的那一份**行底（槽 0 两套 —— 见 `RowBgGo`）。
                //   ⛔ 原来写死 `_deckRowBg[i]`：槽 0 滚到卡行档时那一份是关着的 ⇒ **第 0 行点不动、
                //   也拖不出去**（静默：点上去什么都不发生）。
                var rowGo = RowBgGo(i);
                if (rowGo == null || !rowGo.activeSelf || !HitRow(i, wp)) continue;
                if (!left) return true;
                var shown = DeckRows();
                int idx = Mathf.FloorToInt(_deckScroll / RowPitch) + i;
                // 🆕 2026-10-25（C 卡组编辑域）：空位占位行（`DeckRows` 里的 null）不给拖/点
                if (idx >= shown.Count || shown[idx] == null) return true;
                StartRowDrag(idx, i, wp);
                return true;
            }
            return false;
        }

        /// <summary>离场。**「按来路回」** —— 从哪扇窗进来的就回哪扇：来源由**入口**写进
        /// `CollectionData.SetReturnIntent`（🔴 **2026-10-12（`A1697`）就地订正（铁律 5）**：下面这句原来写
        /// 「全仓**仅有**的两处 `LoadScene("DeckEditor")`」—— **现在是三处**（`A1697` 新开的
        /// `CollectionWindow.GoNewDeck` 是第三处，同样写 `Collection`）；
        /// `CollectionWindow.GoEdit` 写 `Collection` · `LiveOpsEventWindow.CreateDeckInMode` 写 `LiveOpsEvent`）。
        /// ⛔ **不由离场方式决定**：三处调用点（① 关闭钮干净那一支 ②「丢弃改动」那颗左钮
        /// ③ `A364` 那扇不合法窗的左钮）都只是「关闭这条路上的编辑器」，它们分不出来路。
        /// ⇒ 本方法**只在没人写过意图时兜一个默认**（= 直接按 Play 打开 `DeckEditor.unity`、
        /// 或自检里裸调本方法那条路），兜的是**收藏**那条路；⛔ **有意图时绝不覆盖**
        /// （覆盖了「按来路回」就退化成「一律回收藏」）。
        /// 🔴 **2026-10-12（A399）**：本方法**只离场、不保存**（原来这里写「『返回』= **存盘之后**离场」——
        /// 关闭钮按原版改成 `TryClose` 之后**不再保存**，见 <see cref="TryClose"/>）。
        ///
        /// <para>🔴🔴 **2026-10-14（`A1132` 第 4 步）：本方法现在有【两条腿】，判据 = `HostWindow`。**
        /// <list type="number">
        /// <item>**被窗托管那条腿**（`HostWindow != null`，= 第 3 步起那三处生产入口的路）：
        ///   **关本窗就完事** —— ⛔ **不切场景**。原版正是这一条（`DeckEditingWindow__Close.c:19-20`：
        ///   `NavigationPanelController.Toggle(1,1,0)` 放回导航栏 → `GameWindow.Close()`）：关掉之后
        ///   底下那扇**一直开着的** `CollectionScreen` 由 `WindowsManager.ShowPreviousWindow` 带回来
        ///   （**页签从未被切过**）。回程意图也**就地消费**（原来那一跳只在 `MainMenuRuntime.Build` 里，
        ///   而那条路**只有真的换过场景才轮得到**）⇒ 见 <see cref="LeaveByHostWindow"/>。</item>
        /// <item>**独立场景那条腿**（`HostWindow == null`，= 直起 `-wfscene DeckEditor` 与自检里裸建本类）：
        ///   **与第 4 步之前逐位一致** —— `LoadScene("MainMenu")`，由到达端 `MainMenuRuntime.Build`
        ///   照意图重开收藏窗（⛔ 那条链**一个字都没改**：它是直起 / 批处理的兜底）。</item>
        /// </list>
        /// ⚠️ **批处理下不切场景**（自检要靠同一个进程跑完；切了会把后面的断言全带走）
        /// —— 🔴 **所以意图的写入必须在 `isBatchMode` 那道闸【之前】**，否则自检根本观测不到（同 `LeaveCount`）。
        /// ⚠️ `MainMenu` 必须在 Build Settings 里（`MainMenuScene.BuildAndSaveScene` 会加）。</para></summary>
        public void BackToMenu()
        {
            LeaveCount++;        // 🆕 A364：自检口（模态窗那颗「Discard」= `HidePopUp()` + 关窗，关窗的等价物就是这一步）
            // 🆕 2026-10-18（A855）：回程意图兜底（⚠️ 有意图时**不覆盖** —— 判据见上面 doc 里「按来路回」那一段）
            if (CollectionData.PendingReturn.Source == DeckExitSource.None)
                CollectionData.SetReturnIntent(DeckExitSource.Collection, WindowTabType.CollectionDecks);

            // 🆕 **2026-10-14（`A1132` 第 4 步）—— 被窗托管那条腿：关本窗，⛔ 不切场景。**
            //   判据 = `HostWindow != null`（谁写它 → `Shell/DeckEditingWindow.Open()`）。
            //   ⚠️ **排在那道 `isBatchMode` 闸【之前】**：这一跳**本来就不切场景**（批处理里照样安全），
            //     而且自检正是靠「批处理里也能跑通它」来验这条腿的（同 `LeaveCount` 那条口径）。
            //   ⛔ **它只覆盖「有宿主窗」这一支** —— 没宿主窗的独立场景那条腿（下面那三行）**逐字未动**。
            if (HostWindow != null) { LeaveByHostWindow(); return; }

            // ===== 以下 = **独立场景那条腿**（`HostWindow == null`）：与第 4 步之前**逐位一致**，⛔ 一个字别改 =====
            if (Application.isBatchMode) { Debug.Log("[Deck] （批处理：不切场景）返回 = 主菜单场景 `MainMenu`（回到那里会重开**收藏窗的卡组页**）"); return; }
            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            else
                Debug.LogWarning("[Deck] 返回失败：`MainMenu` 不在 Build Settings 里（跑一次 `MainMenuScene.BuildAndSaveScene`）");
        }

        /// <summary>🆕 **2026-10-14（`A1132` 第 4 步）**：**被窗托管那条离场腿** ——
        /// ① 关掉宿主窗（= 原版 `DeckEditingWindow__Close`）· ② **就地消费**回程意图（原版是一扇叠着的窗，
        /// 没有「回程」这回事；我们这一跳的等价物原来落在 `MainMenuRuntime.Build` 里）。
        ///
        /// <para>🔴 **顺序不是随便排的 —— 「作废库缓存」必须在 `Close()`【之前】**：
        /// 关窗那一拍 `WindowsManager.ShowPreviousWindow` 会对底下那扇收藏窗走**完整 `TryOpen`**
        /// （`state == Closed` ⇒ `SetActive(true)` + `state = Open` + **`Open()` 重建内容**）⇒
        /// `CollectionWindow.Build` → `RebuildDeckCells` → `CollectionData.DeckAt(i)` → **进程级缓存**
        /// `CollectionData._lib`。缓存若不先作废，重建出来的列表画的还是**编辑前**那一份（静默、看着像没保存）。
        /// ⚠️ 这正是 `MainMenuRuntime.Build` 那条老路里 `InvalidateLibrary()` 排在建窗之前的**同一条理由**
        /// （判据 → `CollectionData.InvalidateLibrary` 的注释），只是这里换了触发点。</para>
        ///
        /// <para>⚠️ **`Collection` 那一支只调 `ChangeTab`、⛔ 不走 `OpenCollection()`**：
        /// 那一扇此刻**已经开着**（刚被 `ShowPreviousWindow` 带回来）—— 再 `OpenCollection()` 等于
        /// 「再开一次窗」（它的入口带 `closeAll` 记账，会把刚回来的那扇先收掉再重开，闪一下且放大
        /// `A1709` 那一族的风险）。判据 = 用户 2026-10-14 的口径 + `MainMenuRuntime` 那条老路的对照
        /// （那边窗口**不在场**，所以那边**必须**开窗 —— 两条腿的差别就在这里，⛔ 别合并）。</para>
        ///
        /// <para>⚠️ **`LiveOpsEvent` 那一支今天仍不做那一跳**（与 `MainMenuRuntime` 那边同一条理由：
        /// 本地没有事件数据，回不到那扇事件窗）—— 只作废缓存、只出声。</para></summary>
        void LeaveByHostWindow()
        {
            CollectionData.InvalidateLibrary();     // ⚠️ 必须排在 `Close()` 之前（理由见上面那段）

            var win = HostWindow;
            win.Close();                            // = 原版 `DeckEditingWindow__Close.c:19-20` 的 `GameWindow.Close()`
            Debug.Log("[Deck] 离场：关掉宿主窗「" + win.name + "」⇒ 底下那扇由 `ShowPreviousWindow` 带回来"
                      + "（⛔ 不切场景 —— 原版就是「叠窗 + 关窗」）");

            var ret = CollectionData.TakePendingReturn();
            if (ret.Source == DeckExitSource.Collection)
            {
                // ⚠️ 落页走**显式**的 `ChangeTab`，⛔ 不靠 `CollectionWindow.Open()` 里那句 `Click(0)` 的巧合
                //    （理由与 `MainMenuRuntime` 那一处逐字相同）。
                var tab = ret.Tab;
                if (tab == WindowTabType.None)
                {
                    tab = WindowTabType.CollectionDecks;      // 兜底，但**出声**（不许静默失败）
                    Debug.LogWarning("[Deck] 回程意图只写了来源 `Collection`、没写页签 ⇒ 兜回卡组页"
                                     + "（写意图那一处该补 `WindowTabType.CollectionDecks`）");
                }
                // 上一扇必须**就是**刚被带回来那扇收藏窗（`ShowPreviousWindow` 把 `currentWindow` 设成列表尾）
                var wm = WindowsManager.Instance;
                var coll = wm != null ? wm.TopWindow as CollectionWindow : null;
                if (coll != null) coll.ChangeTab(tab);
                else
                    Debug.LogWarning("[Deck] 回程意图是「收藏」，可关窗之后**顶上那扇不是收藏窗**"
                                     + "（`TopWindow = " + (wm != null && wm.TopWindow != null ? wm.TopWindow.name : "<none>")
                                     + "`）⇒ 这一下落页**没做**（⛔ 不在这里另开一扇来凑 —— 那会与"
                                     + "「已经开着的那扇」两扇并存）。");
            }
            else if (ret.Source == DeckExitSource.LiveOpsEvent)
                Debug.Log("[Deck] 回程意图是**事件窗**：本地没有事件数据 ⇒ **回不到那扇窗**"
                          + "（与 `MainMenuRuntime.Build` 那边同一条如实标注；只作废库缓存、不开窗）。");
        }

        /// <summary>记下拖拽起点。**鼠标与自检走同一个函数**（删牌那条路只有「拖出」一条，必须验到）。
        /// ⚠️ 本行原来**写错位置**了（挂在 `BackToMenu` 的 doc 上面，两个 `<summary>` 连在一起）——
        /// 2026-10-14（`A1132` 第 4 步）重写 `BackToMenu` 时**就地挪回它自己的宿主**（铁律 5：知识别丢）。</summary>
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
                var shown0 = DeckRows();
                if (row >= 0 && row < shown0.Count) OpenCardWindow(shown0[row]);
                RefreshDeckList();
                return;
            }
            // 拖出侧栏 = 删除（照原版 `DeckEditingPanel__CheckCardSlotDrag.c:59,66`）
            // 🔴 `screenX` 是**设计坐标（1920 宽）**，不是屏幕像素 —— 传进来的是 `ToPx()` 的结果。
            //    原来这里又乘了一次 `Screen.width / ScreenW`（那是给真实像素用的）⇒
            //    批处理下 `Screen.width` 不是 1920，阈值被缩小，**拖到侧栏里面也会删**
            //    （自检抓到：往 x=150 拖，卡组照样少一张）。
            // ⚠️ **2026-10-21（`A1700`）如实标注**：`ListX + RowW` = **325.4** 是**抽屉**（那一竖栏）的右缘，
            //    而**行节点**右缘是 **317.4**（见 `RowNodeW`）⇒ 两者之间那 **8px** 的带子原版算不算「已出栏」
            //    本件**没查**（判据 = `CheckCardSlotDrag.c` 比的是哪一个 rect，本地读不到那一段）。
            //    **保持 325.4 不动**（= 旧行为，且自检按它写的），⛔ 别顺手改成 317.4。
            if (screenX > ListX + RowW)
            {
                var shown = DeckRows();
                if (row >= 0 && row < shown.Count && shown[row] != null)   // 🆕 空位行不是卡，不能删
                {
                    var def = shown[row];
                    if (State.TryRemove(def)) { MarkDeckDirty(); Say("已移出卡组：" + CardText.Name(def)); }
                }
            }
            RefreshAll();
        }

        /// <summary>🆕 2026-10-04（A37 ④）：**点击与悬停共用同一条命中顺序**（唯一出处）。
        /// 为什么要有它：原来**点击**走下面 `HandleButtons` 的 if 链、**悬停**走 `_btns` 的**登记顺序**，
        ///   而 `HoverTargetUnder` 的注释却写着「命中口径 = 和点击同一条」—— **声明与实现不符**。
        ///   照 `CLAUDE.md` 那条「两处写同一条规则 = 迟早不一致」，不能留两份。
        /// 🔴 **顺序判据 = 「谁画在上面，谁先吃到这一下」**（可见层），⛔ 不是原 if 链的字面次序 ——
        ///   本表最初是逐字照搬 if 链的，而那条链**有一处与可见层不符**：`name_clear` 的矩形整个落在
        ///   `name_box` 里（见 `BuildHeader` 的 `:714-716`），而清空图标画在输入框底**之上**
        ///   （图标 `QBorder = 3005` > 输入框底 `QPanel = 3004`）⇒ **2026-10-12（A396）把
        ///   `name_clear` 挪到 `name_box` 前面**（原来在它后面 ⇒ 那颗钮**用鼠标点不到**，点下去是进改名态）。
        /// 顺序 = 原 if 链逐字照搬（页签排最后：`tab_*` 的矩形最大，压到别人这件事只有它在最后才算对）。
        /// ⚠️ **2026-10-12（A396）就地订正（铁律 5）**：这段原来还写着「**本窗矩形今天互不重叠**
        ///   ⇒ 两套顺序行为一致（看不出分叉）」—— **那句与事实不符**（`name_clear` ⊂ `name_box`，
        ///   而且正是它把上面那颗钮盖住的）。判据只有一条：**按可见层排**，⛔ 不是「反正不重叠」。
        /// 🔴 **新接一颗悬停 / 点击按钮，必须把 key 写进这张表** —— `Editor/DeckScene.cs` 有一条断言盯着
        ///   「接了悬停换图的 key ⊆ 这张表」，漏写就红。</summary>
        static readonly string[] ClickOrder = {
            "hdr_filters", "hdr_clear", "hdr_back", "foot_done",
            "name_clear", "name_box",
            // 🔴 **2026-10-17（D35/T23 删件）**：这里原来还有 `"info_share", "info_import"` 两项
            //   （那两颗圆钮原版没有）与 `imp_input` / `imp_ok` / `imp_close` / `imp_shade` 四项
            //   （导入弹窗那棵树，重复实现）—— **已全部删掉**，连它们的登记一起删。
            "tab_0", "tab_1", "tab_2",
        };

        /// <summary>这一点上**按 `ClickOrder` 第一个吃到它的 key**（没有 = `null`）—— 点击与悬停都走它。</summary>
        string TopKeyAt(Vector2 px)
        {
            foreach (var k in ClickOrder) if (HitBtn(k, px)) return k;
            return null;
        }

        /// <summary>页头那颗「清空卡组名」（`name_clear`）的**动作本体**。
        ///
        /// <para>🔴 **2026-10-12（A363）**：原来它写作 `case "name_clear": State.SetDeckName(...);
        /// CommitDeck(); …` —— 那个 `CommitDeck()` 已按原版撤掉（突变只标脏），动作抽到这里，
        /// 让**点击**与**自检**走同一段（⛔ 别在 `HandleButtons` 里再写一遍）。</para>
        ///
        /// <para>🔴 **2026-10-12（A396）已修那颗钮的命中区**：`Btn_("name_clear", 277.2, 316, 35, 40)` 整个落在
        /// `Btn_("name_box", 9.5, 311, 307.7, 50)` 里面，而 `ClickOrder` 里原来 `name_box` **排在前面**
        /// ⇒ **用真鼠标点那颗清空图标命中的是 `name_box`（进改名态）**，这个钮点不到。
        /// 修法 = 照**可见层**把 `name_clear` 挪到 `name_box` **前面**（清空图标 `QBorder = 3005` 画在
        /// 输入框底 `QPanel = 3004` 之上）—— 已落地，`ClickOrder` 那段注释里记了判据。
        /// ⚠️ 原来这里写的是「**本件只报未改**，修它是另一件」—— 那句**已经过期**，就地订正（铁律 5）。</para></summary>
        void ClearDeckName()
        {
            // 🔴 **2026-10-18（双语线 · 波 1 · P1）**：清名之后那串「新卡组」走词条
            //   `MenuDeck/NewDeckName`（**与 `Deck/DeckEditorState.NewDeck` 是同一条键** ——
            //   铁律 6：同一件事只留一条键）。中文列 = 改之前写死的 `新卡组` ⇒ 中文档零变化。
            State.SetDeckName(Loc.T("MenuDeck/NewDeckName"));
            MarkDeckDirty();                 // 🆕 A363：**只标脏**，落盘等 Done
        }

        /// <summary>自检入口：清空卡组名（**直接调动作本体**，不走命中）。
        /// 🔴 **2026-10-12（A396）起那颗钮已经点得到了**（`name_clear` 挪到了 `name_box` 前面）
        /// ⇒ 自检**优先走真鼠标那条路**（`UiClickPx` 点图标中心，见 `Editor/DeckScene.cs` 的 A396 那几条），
        /// 本入口留着给「只想驱动状态、不想碰命中」的场合。
        /// ⚠️ 原来这里写「⛔ 不能用 `UiClickPx` 代替 —— 命中区被 `name_box` 盖住」—— 那句**已经过期**（铁律 5）。</summary>
        public void UiNameClear() { ClearDeckName(); }

        /// <summary>🆕 **2026-10-17（音效线 S01 写手 G）**：卡组编辑**逐按钮 cue** ——
        /// 原版 `Deck Editing Menu` prefab 的 `m_onClickSound`（12 颗，判据 =
        /// `资料/普查产出_1017/UI/R3_S01按钮cue映射.md` §二·C-1 `Deck Editing Menu`）。
        /// <para>🔴 **本窗不走 `MenuDraw.Hit`**：点击在 `HandlePointer → HandleButtons` 这一条链上
        /// （原版那 12 颗也是 `EverguildButton|EverguildToggle`，只是我们的替身不是 `WindowButton`）
        /// ⇒ cue 只能在**派发点**播。表收 `m_onClickSound` **非空**的那 4 个 id 之外，
        /// **还要收 3 颗页签**（见下一条注释）：
        /// `hdr_back`(`Header/Close`) · `hdr_clear`(`Header/Filters/Generic Simplified UI Button_updated`) ·
        /// `foot_done`(`Sidebar/Footer/Done`) → **Positive**；`hdr_filters`(`Header/Filters` Toggle) → **Select Toggle**。
        /// ⚠️ 其余 8 颗 Toggle（6 颗卡牌筛选 + 2 颗卡背筛选；第 9 颗 = 表里那颗 `hdr_filters`）在
        /// `HandleFilterClick` / `HandleCosmoFltClick` 里统一播 `SelectToggleCue`（它们按格子走、没有 id）。
        /// 🔴 **2026-10-18 就地订正（铁律 5 · 音效线缺口 `AUD` §三 A3）**：这一段原来写
        /// 「表外的 key（页签 `tab_*` / 改名框）**原版没有显式 cue** ⇒ 不播（保持现状）」——
        /// **推理不成立**：原版「没有显式 cue」**不等于静音**。判据（`AUD` 现核 prefab 实读）：
        /// `Deck Editing Menu/Content Area/Sidebar/Window Options/Buttons/{Cards,Cosmetics,Info}` =
        /// `EverguildToggle`，`m_onClickSound` 空 + **`useDefaultAudioClipIfNull = 1`** ⇒ 切到别的页时
        /// 播**默认 `ButtonClick`**；再点当前页则被 `allowClickWhenAlreadySelected = 0` 整下吃掉
        /// （连声音一起）。⇒ `tab_0..2` 收进本表映射 `DefaultButtonCue`，且 `HandleButtons`
        /// **先判「已选中」再播音**。⚠️ 改名框（`name_box`）确实不是按钮（输入框）⇒ 仍然不播，是对的。</para></summary>
        static readonly Dictionary<string, string> ClickCueById = new Dictionary<string, string>
        {
            { "hdr_back",    WindowSounds.PositiveCue },      // Content Area/Header/Close
            { "hdr_clear",   WindowSounds.PositiveCue },      // …/Header/Filters/Generic Simplified UI Button_updated
            { "foot_done",   WindowSounds.PositiveCue },      // Content Area/Sidebar/Footer/Done
            { "hdr_filters", WindowSounds.SelectToggleCue },  // …/Header/Filters（Toggle）
            // 🆕 2026-10-18（`AUD` §三 A3）：3 颗页签 = 空 cue + `useDefaultAudioClipIfNull = 1` ⇒ 默认音
            { "tab_0",       WindowSounds.DefaultButtonCue }, // …/Sidebar/Window Options/Buttons/Cards
            { "tab_1",       WindowSounds.DefaultButtonCue }, // …/Buttons/Info（Deck info）
            { "tab_2",       WindowSounds.DefaultButtonCue }, // …/Buttons/Cosmetics
        };

        /// <summary>按 id 播这一颗的 cue（表外 / null = **不播** —— 见 `ClickCueById` 的注释）。</summary>
        static void PlayClickCue(string id)
        {
            string cue;
            if (id != null && ClickCueById.TryGetValue(id, out cue))
                WindowSounds.PlayFx(cue, $"卡组编辑按钮 `{id}`");
        }

        bool HandleButtons(Vector2 px)
        {
            var key = TopKeyAt(px);
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 C9）**：原版三颗页签是 `EverguildToggle` +
            //   **`allowClickWhenAlreadySelected = 0`** ⇒ `EverguildToggle__OnPointerClick.c` 第一段
            //   「已选中 ⇒ 整下点击被吃掉」—— **连按钮音也不响、`onValueChanged` 也不发**。
            //   ⇒ 守卫必须排在 `PlayClickCue` **之前**（改前是先播音、再由 `SetTab` 重复设一次 = 多响一声）。
            if (key != null && key.Length == 5 && key.StartsWith("tab_", System.StringComparison.Ordinal))
            {
                int want = key[4] - '0';
                if (want >= 0 && want <= 2 && want == _tab) return true;
            }
            PlayClickCue(key);
            switch (key)
            {
                case "hdr_filters": ToggleFilters(); return true;
                case "hdr_clear": ClearFilters(); return true;
                // 🔴 **A330（2026-10-11）**：`foot_done` 走的是 `SaveAndSay()`，而它的**第一件事**现在是一道
                //   校验闸（原版 `DeckEditingWindow__TrySaveDeck.c:80`）⇒ 卡组不合法时**不落盘、只出声**。
                //   `ESC` 走的是**同一个函数**（见 `EscPressed()`）。
                // 🔴 **A399（2026-10-12）改向**：`hdr_back`（= 原版 `Content Area/Header/Close`，文本 'Back'）
                //   **不再走 `SaveAndSay()`** —— 原版**关闭钮**走的是 `DeckEditingWindow__TryClose`：
                //   **脏 ⇒ 弹「丢弃改动」那扇两钮窗、干净 ⇒ 直接关**（判据逐句抄在 `TryClose()` 的注释里）。
                //   它**从不保存、也从不校验卡组**（原来我们写的是「先 `SaveAndSay()` 再离场」= 我们的偏离）。
                case "hdr_back": TryClose(); return true;
                case "foot_done": SaveAndSay(); return true;
                case "name_box": BeginNameEdit(); return true;
                case "name_clear": ClearDeckName(); return true;
                // 🔴 **2026-10-17（D35/T23 删件）**：这里原来还有
                //   `case "info_share": ShareDeckString();` / `case "info_import": OpenImport();`
                //   （那两颗圆钮原版这扇窗里没有）与 `imp_input/imp_ok/imp_close/imp_shade` 四支
                //   （导入弹窗那棵树，重复实现）—— **已全部删掉**。
                //   ⚠️ `ShareDeckString()` 当时因此**没有生产入口** —— 🔴 **2026-10-23（W65 · 1c）已整段删掉**（0 调用点）。
                case "tab_0": SetTab(0); return true;
                case "tab_1": SetTab(1); return true;
                case "tab_2": SetTab(2); return true;
                default: return false;      // 没命中（`TopKeyAt` 返回 null）
            }
        }

        /// <summary>保存 = **原版 `DeckEditingWindow.TrySaveDeck`** —— `Done` 钮与 `ESC` **共用这一个函数**
        /// （`ESC` 那一路：`d:/2/tools/decomp_full/DeckEditingWindow__ESCPressed.c:5` 就一句
        /// `DeckEditingWindow__TrySaveDeck(param_1, 0)`）。
        ///
        /// <para>🔴 **2026-10-11（A330）补上原来缺掉的那道闸**：原版 `TrySaveDeck` 的**第一件事**就是
        /// `cVar3 = DeckUtility__ValidateDeck(deck, out err, /*validateOwnership*/1, 0);`
        /// （`DeckEditingWindow__TrySaveDeck.c:80`）——
        /// **不合法 ⇒ `:81-98` 弹错误窗（`WindowsManager.ShowPopUp`）后 `return`，绝不走到 `:100` 的
        /// `UploadDeck`**；合法才上传。
        /// 我们原来只有 `RefreshHeader` 拿 `State.Validate()` 去点亮 `Done Highlight`，
        /// **保存这条路一点校验都没有** ⇒ 非法卡组照存（差异由 W7 查出，本件补）。</para>
        ///
        /// <para>判据**只此一处**（铁律 6）：这里调 `State.Validate()`（→ `DeckRules.Validate`，
        /// 见 `Deck/DeckEditorState.cs` 的 `Validate`），与 `RefreshHeader` 里点亮 `foot_hl` 那一句是**同一个调用**。
        /// 原版那边也是同一个 `ValidateDeck`（`__UpdateDoneButton.c:24` 与 `__TrySaveDeck.c:80`
        /// 逐参数相同：`(deck, out err, 1, 0)`）⇒ **「灯亮」与「放行」是同一条判据**，不是两条。
        /// ⛔ 别在这里另写一套「张数对不对」。</para>
        ///
        /// <para>⚠️ **2026-10-12（A364）就地订正（铁律 5）**：这里原来写「原版不合法时弹的是一扇**模态窗**
        /// `PopUpGameWindow` …… **我们退一档**：页脚那句 `_verdict` + 这行 `Say(...)`…… **要做成模态窗是另一件**」。
        /// **那条「退一档」已经不成立** —— 那扇窗现在**建了**（`Shell/PopUpGameWindow.cs`，照原版
        /// `MessagePopupWindow{,_2Buttons}` 两扇 prefab），本函数不合法那一支**弹它**（见 `ShowInvalidDeckPopUp`）。
        /// 页脚那句 `_verdict` 与 `Say(...)` **留着**（它们是本窗自己的页脚读数，原版没有；退一档那句不再成立、
        /// 但它们不是「退而求其次」的替代品）。</para>
        ///
        /// <para>改坏法：把下面那个 `if` 整段删掉（或改成 `if (false)`）⇒
        /// `Editor/DeckScene.cs` 的「A330 ★ 不合法 ⇒ Done 不落盘」「A330 ★ …ESC 也不落盘」双双变红；
        /// 把 `ShowInvalidDeckPopUp(err)` 那一句删掉 ⇒ A364 那一节的「不合法 ⇒ 弹出模态窗」变红。</para></summary>
        void SaveAndSay()
        {
            var err = State.Validate();
            if (err != DeckError.None)
            {
                Say("卡组不合法，没有保存：" + DeckErrorText(err));
                ShowInvalidDeckPopUp(err);                 // 🆕 A364：= 原版 `__TrySaveDeck.c:94 ShowPopUp(...)`
                return;                                    // ⛔ 不调 `CommitDeck()` —— 原版这里 return，不 UploadDeck
            }
            if (!CommitDeck())
            {
                // ⛔ 不静默：写不进去就**别**说「已保存」（脏标记也留着 —— 下次 Done 还会再试）
                Say("保存失败：" + SaveFailReason());
                return;
            }
            HideDeckPopUp();                               // 🆕 A364：= 原版 `__TrySaveDeck.c:103 HidePopUp`
            Say("已保存");
        }

        /// <summary>`DeckError` → **给玩家看的那句人话**（`Describe` 出键、`Loc.T` 取词）。
        ///
        /// 🔴 **2026-10-18（`G9`）新开这一个口，就是为了「同一语义只有一条路径」**：
        /// `RuleEngine/DeckRules.Describe` 从这一天起**只出词条键**（⛔ 引擎层不再产人话），
        /// 而所有显示点都得做「键 → 当前语言那句话」这一跳。⛔ **别在调用点手写 `Loc.T(Describe(...))`**
        /// —— 那会散成好几处，将来加一层（比如字体闸）就得每处都改。
        /// ⚠️ 本类**没法**替 `DeckEditorState.TryAdd` 收口：那个 `out string why` 出的也是键
        /// （`Deck/DeckEditorState.cs` 不在本笔白名单里）⇒ 谁显示它谁负责过一道 `Loc.T`。
        /// </summary>
        public static string DeckErrorText(DeckError e)
        {
            if (e == DeckError.None) return "";
            // 🟢 **2026-10-25（`E1` · 第八段）：「上屏用哪个键」的判据收在 `DeckRules.ErrorTextKey`（只此一处）。**
            //   原版真实路径（cpp2il `DeckUtility.cs:149-170` 现读）：`term = $"MenuDeck/Error/{枚举}"`
            //   —— `{0}` 是**枚举本身（名字）**，取不到就退 `MenuDeck/Error/InvalidDeck`。
            //   ⇒ 🔴 **数字族 `MenuDeck/Error/{2,4,5}` 那条判据已被推翻**（官方 I2 表一条数字键都没有），⛔ 别再走它。
            //   三档：① 原版有具名键 ⇒ 具名；② 我们自己的槽位判据（`WarlordAlreadySet`/`DefensiveAlreadySet`）⇒ 保留具名；
            //        ③ 其余 10 档 ⇒ 映射到**原版那 6 个枚举名**再走具名族
            //           （`NoWarlord→MissingHero` · `TooFew/TooMany→IncompleteDeck` · 其余→`InvalidDeck`）。
            //   ⚠️ `MenuDeckErrorNumber` / `MenuDeckErrorKey`（数字族）**保留**：`DeckRuntime.cs` 另一处弹窗还在用，
            //     但它只是「取不到就退 `InvalidDeck`」，与 `ToRawLocalizationString` 的兜底同形 ⇒ 不是本函数的判据。
            //   ⛔ 别在调用点各写一份这个选择。
            return Loc.T(DeckRules.ErrorTextKey(e));
        }

        /// <summary>「卡组存档**写不进去**」那条**人话**的词条键（🔴 **2026-10-18 `G8` 自拟**）。
        ///
        /// ⚠️ **原版没有这一条** —— 原版的卡组存在**服务器**上（`CardDeck.syncedToServer`），
        /// 落盘失败这件事在本地不存在。四种载体都查过、**都查不到**：
        ///   · prefab `Localize.mTerm`：全库扫 `"mTerm": "CustomErrors/…"` 只命中 2 条
        ///     （`DuplicateConnection` / `ErrorSavingMatch`）、`MenuDeck/Error/*` 11 条里也没有「保存卡组失败」
        ///     （只有 `FailedToDelete`）；
        ///   · 代码字面量（`stringliteral.json`，26,507 条）：`CustomErrors/*` **13** 条 +
        ///     `MenuDeck/Error/*` **12** 条 + `MainMenu/General/*` **13** 条，**逐条看过**，无此语义；
        ///   · `decomp_full`：`CustomError` 枚举里最接近的是 `ErrorSavingMatch`（**保存对局**，不是卡组存档）
        ///     ⇒ 语义不同，⛔ 不借它；
        ///   · `assets_full`：同 prefab 那一路，无。
        /// ⇒ 键名与两列文案**都是我们起的**（形状照原版 `MenuDeck/Error/*` 那一族）。
        /// ⚠️ **中文那一列就是改之前那句写死的原话**（`"写不进存档文件"`）⇒ **今天中文档零变化**；
        /// 英文那一列是我们译的（原版英文在远端 I2 表，本地取不到）。</summary>
        public const string TermSaveFailed = "MenuDeck/Error/SaveFailed";

        /// 🔶 自己设计（非原版）：落盘失败提示「人话走 Loc、诊断串只当兜底」 · 原版提示文案在远端 I2 表；本地只剩诊断串 · TODO(谁定的)｜原注：<summary>落盘失败的原因 —— 🔴 **`2026-10-18（G8）` 起改成「人话走 `Loc`、诊断当兜底」**。
        ///
        /// **改之前**：`string.IsNullOrEmpty(Library.LastError) ? "写不进存档文件" : Library.LastError`
        /// —— 一句话全写死在这里，且**直接拿 `LastError` 当显示主路**。
        /// **现在**：
        ///   · **人话**那一半走词条（`Loc.T(TermSaveFailed)`，随语档）；
        ///   · `Library.LastError`（🔴 现在是一串**诊断**，见 `DeckStore.SaveAll` 的注释）
        ///     只当**兜底诊断**，缀在括号里 —— ① 「不许静默失败」要求原因**看得见**，
        ///     ② `Editor/DeckScene.cs` 的 A547 那条断言（「报的是**真原因**」）读的就是它出现在日志里。
        ///
        /// ⚠️ 与收藏窗那一半 `Shell/CollectionData.SaveFailReason()`（A503）原来是**逐字同一句**；
        /// 本笔把这一半改成走词条之后，两半在**英文档**下会不一样（那半边在 `Shell/**`，
        /// **不在本件白名单**）⇒ 如实记进交件报告，⛔ 不是「两处写同一条规则」被打破
        /// （中文档下两句**仍然逐字相同**，因为词条的中文列就是原话）。</summary>
        string SaveFailReason()
        {
            string why = Loc.T(TermSaveFailed);
            string diag = Library.LastError;
            return string.IsNullOrEmpty(diag) ? why : why + "（" + diag + "）";
        }

        // ============================================================ 关窗（原版 `DeckEditingWindow.TryClose`）
        //
        //  🆕 **2026-10-12（A399）** —— `DeckDirty` 的 **UI 出口**（A363 只补了脏标记这个前置条件）。
        //
        //  判据（**逐句**，`d:/2/tools/decomp_full/`；这条链**只由【关闭钮】走**）：
        //   `DeckEditingWindow__TryClose.c`
        //     :5   `if (param_1[0x23] == 0)` —— `0x23 × 8 = 0x118`：窗上那份**编辑中的** `CardDeck`
        //          （空引用直接炸 = 原版这里不留兜底）
        //     :9   `if (*(char *)(param_1[0x23] + 0x60) == '\0')` —— `+0x60` = **`syncedToServer`**，
        //          突变那几处写 0 = **脏** ⇒ 这个分支就是「**脏了**」
        //     :10      `DeckEditingWindow__ConfirmDiscard(param_1, 0)`；`:11 return` ⇒ **脏了先问**
        //     :15  否则（= 已同步）⇒ `(**(code **)(*param_1 + 0x1b8))(param_1, *(param_1 + 0x1c0))`
        //          —— 虚槽 `0x1b8` = `GameWindow.Close()` ⇒ **直接关**
        //   `DeckEditingWindow__ConfirmDiscard.c`
        //     :25/:41  右钮：`+0x10 = DAT_1842be120`（标签）· `:41 +0x18 =` 那颗委托（回调）
        //     :46/:49  左钮：`+0x10 = DAT_1842be418`（标签）· `:49 +0x18 =` 委托到**窗**上的
        //              `DAT_1842d13c0` = **`.<ConfirmDiscard>b__45_1`**
        //     :54  `WindowsManager__ShowPopUp(lVar4, DAT_1842d00e8, 1, 0, /*5th*/ 左=lVar2, /*6th*/ 右=lVar1, 0)`
        //          —— 正文键 `DAT_1842d00e8` 的**【地址表实读】**= **`MenuDeck/HUD/DiscardChanges`**
        //          （`d:/2/tools/il2cpp_out/stringliteral.json`；同一张表里
        //           `0x42BE418 → MainMenu/General/Discard` · `0x42BE120 → MainMenu/General/Cancel`，
        //           与 `Shell/PopUpGameWindow.cs` 文件头那两行**对得上** ⇒ 这套读法本身也被旁证过）
        //       两颗回调的**方法体**（逐句读过）：
        //         `DeckEditingWindow___ConfirmDiscard_b__45_1.c`（左 = `Discard`）
        //           = `WindowsManager.HidePopUp()` **+** 虚槽 `0x1b8` ⇒ **丢弃 + 关窗**
        //         `DeckEditingWindow.__c___ConfirmDiscard_b__45_0.c`（右 = `Cancel`）
        //           = **只有** `HidePopUp()` ⇒ **留在编辑器里**
        //   ⇒ **它从不保存**：干净就直接关、脏就「丢弃 / 取消」，没有第三条路。
        //     （保存只归 `Done` / `ESC` —— `__TrySaveDeck`；⛔ 别把这两条链混起来。）

        /// <summary>原版 `ConfirmDiscard` 那扇窗的**正文术语键**（`DAT_1842d00e8` 的地址表实读）。
        /// ⛔ 不自己编一句人话：这句键由 `PopUpGameWindow.Term()` 解析。
        /// 🔴 **2026-10-19（`A1012`）就地订正（铁律 5）**：这里原来写「I2 语言表在**远端**、本地没有
        /// ⇒ 今天画面上显示的就是**这个键**」—— **那句已经不成立**：`Term()` 现在转全工程的
        /// `Core/Loc.cs`，而 `MenuDeck/HUD/DiscardChanges` **就在那张表里**
        /// （键 `MenuDeck/HUD/DiscardChanges`：「放弃更改」/`Discard unsaved changes?` —— 2026-10-14 换官方 zh-CN 表，原「丢弃未保存的改动？」）⇒ 屏上印的是**这条词条**。</summary>
        public const string DiscardChangesKey = "MenuDeck/HUD/DiscardChanges";

        /// <summary>= 原版 `DeckEditingWindow.TryClose()`：**关闭钮**（`hdr_back`）那一下。
        /// <para>脏 ⇒ **先问**（那扇两钮模态窗）；干净 ⇒ **直接离场**。⛔ **两条路都不保存**。</para>
        /// <para>⚠️ **它不校验卡组**：原版这条链跟 `ValidateDeck` 没关系（校验只在 `TrySaveDeck` 里）
        /// ⇒ 一幅**不合法**的牌照样关得掉（丢改动 = 回到盘上那份）。</para>
        /// <para>改坏法（对应 `Editor/DeckScene.cs` 的 A399 那几条）：把 `!DeckDirty` 那一支删掉
        /// （干净也弹窗）⇒ 「干净时直接走、不弹窗」红；把脏那一支写成直接 `BackToMenu()`
        /// ⇒ 「脏了先问」红；把 `DiscardChangesAndLeave` 里的 `BackToMenu()` 换成 `SaveAndSay()`
        /// ⇒ 「Discard 一个字节都没写盘」红；换掉 <see cref="DiscardChangesKey"/> ⇒ 「正文键」红。</para></summary>
        public void TryClose()
        {
            if (!DeckDirty) { BackToMenu(); return; }   // = 原版 `:15` 那个虚槽 `0x1b8`（`GameWindow.Close`）
            ShowDiscardChangesPopUp();                  // = 原版 `:11` 的 `ConfirmDiscard`
        }

        /// <summary>= 原版 `DeckEditingWindow.ConfirmDiscard()`：开那扇「丢弃改动？」的两钮窗。
        /// <para>宿主与 `A364` 那扇**是同一扇**（`WindowsManager.ShowPopUp` → `PopUpGameWindow`；
        /// 同一时刻只可能有一扇消息弹窗在场 —— 原版认的是字段 `popUpWindow`），
        /// `_popup` 也照旧指向它（<see cref="ModalPopupOpen"/> 是全部指针动作的第一道闸）。</para></summary>
        void ShowDiscardChangesPopUp()
        {
            _popupMgr = WindowsManager.EnsureHost();
            _popup = _popupMgr.ShowMessagePopUp(
                DiscardChangesKey,
                PopUpGameWindow.KeyDiscard, DiscardChangesAndLeave,   // 左 = `.<ConfirmDiscard>b__45_1`
                PopUpGameWindow.KeyCancel, HideDeckPopUp);            // 右 = `.<ConfirmDiscard>b__45_0`
        }

        /// <summary>= 原版 `.&lt;ConfirmDiscard>b__45_1`（左钮 `Discard`）：`HidePopUp()` + 关窗
        /// ⇒ **丢掉未保存的改动、离场**。
        ///
        /// <para>🔴 **2026-10-13（A397）订正 —— 现在「丢掉」成色如何**（旧注释已就地改掉，铁律 5）：
        /// · **库里那份（内存 + 盘）从头到尾没被动过**：A363 起突变只标脏，A397 起编辑器改的又是
        ///   `LoadDeck` 装的**副本** ⇒ 未 Done 的改动**不会**经任何库级 `Save()` 漏进存档。
        ///   **这一半与原版等价**（原版也是「改副本 → 丢弃 = 丢掉副本」）。
        /// · **剩下的差别只有一处**：原版点 Discard 就是**关窗**（`HidePopUp()` + 虚槽 `0x1b8`），
        ///   窗一关那份 `EditingDeck` 副本随之销毁；我们这边 `BackToMenu()` 在真机上会切场景
        ///   （⇒ 等价），但**批处理 / 不切场景的路径上那份副本还留在内存里**（`State.Deck` 仍是
        ///   「被丢弃」的那份内容）。
        ///   🔴 **2026-10-14（A565）就地改向（铁律 5）** —— **那一处差别已经补上**：本方法现在**会回滚内存**
        ///   （见 <see cref="RollBackEditingCopy"/>）。旧注释在这里写着「本方法**仍不做内存回滚** …… 作为
        ///   **另一条账**记在 `资料/普查产出_1013/WD1b_编辑副本隔离.md` §六，由调度台排」——
        ///   **那条账就是 A565，本件做掉了**（判据/修法/受影响夹具就是 WD1b §六·1 列的三行）。
        ///   受影响的两节夹具（`Editor/DeckScene.cs` 的 A364 ⑥⑦ 与 A399 ③）**已同批改到位**。</para></summary>
        void DiscardChangesAndLeave()
        {
            HideDeckPopUp();
            RollBackEditingCopy();
            Say("已丢弃未保存的改动");
            BackToMenu();
        }

        /// <summary>🔴 **2026-10-14（A565）**：**把编辑器那份副本回滚成库里那份** —— 两颗「Discard」左钮
        /// 共用这一段（<see cref="DiscardChangesAndLeave"/> 与 <see cref="ShowInvalidDeckPopUp"/> 里那颗 lambda）。
        ///
        /// <para>判据（原版，两颗左钮的**方法体**逐句读过）：`HidePopUp()` + 虚槽 `0x1b8`（`GameWindow.Close()`）
        /// —— `.&lt;ConfirmDiscard>b__45_1`（见上面「关窗」那一节）与 `.&lt;TrySaveDeck>b__42_1`。
        /// **关窗 ⇒ 窗上那份 `EditingDeck`（`+0x118`）随窗销毁**，库里那份（`+0x40`）从头到尾没被动过
        /// ⇒ 「丢弃」之后编辑器里**不该**还留着被丢弃的内容。我们真机上 `BackToMenu()` 会切场景（等价），
        /// 但**批处理 / 不切场景**的路径上编辑器还活着 ⇒ 不显式回滚，按一次 `Done`
        /// （`SaveAndSay()` 合法就 `CommitDeck()`）会把**刚被丢弃的那份**写回库（= 今天之前的行为）。</para>
        ///
        /// <para>⚠️ **它换对象**：`State.LoadDeck()` 走的是 A397 那条「装副本」路（`deck.Clone()` 出新对象）
        /// ⇒ 任何**跨这一步**捕获的 `var live = _rt.State.Deck;` 别名都会**悬空**
        /// （`Editor/DeckScene.cs` 的 A364 那节因此在 Discard 之后**重新捕获**了一次 —— 见那边的 A565 注释）。</para>
        ///
        /// <para>⚠️ **不动盘、也不动库**（回滚方向是「库 → 副本」）：这里**不调** <see cref="CommitDeck"/>，
        /// 库里那份（内存 + 盘）一个字节都不变 —— A399 那节「Discard 一个字节都没写盘」照旧成立。</para>
        ///
        /// <para>改坏法：把下面第一行删掉（退回「只清脏标记」）⇒ `Editor/DeckScene.cs` 的 A565 那几条
        /// （A364 ⑥ 那三条 + A399 ③ 那两条）红。</para></summary>
        void RollBackEditingCopy()
        {
            // 🔴 **2026-10-12（`A1697`）**：新建那一路**库里没有对应的一套** ⇒ 回滚的目标是 `_newDeck`
            //    （= 原版开窗时传进来那一副、窗的 `+0x40`）。⚠️ **不能回退成 `Library.Current`** ——
            //    那会把**别的一套**（当前选中）当成「这一副的原样」贴进编辑器。
            //    ⚠️ **不清 `_newDeck`**：清了之后再按 `Done` 会走 `CommitCurrent` → 把内容写进库里**当前选中**那一套（别人的卡组）。
            State.LoadDeck(_newDeck != null ? _newDeck : Library.Current);
            DeckDirty = false;                   // 内容与库逐字节相同 ⇒ 不脏（与 `CommitDeck` 成功那一支同义）
            RefreshAll();                        // 批处理没有帧循环 ⇒ 视图要显式跟上
        }

        // ============================================================ 模态消息窗（A364）
        //
        // 判据（**逐句**，`d:/2/tools/decomp_full/DeckEditingWindow__TrySaveDeck.c`）：
        //   :80  `cVar3 = DeckUtility__ValidateDeck(deck, out err, 1, 0)`
        //   :81-98 不合法 ⇒ 建两颗 `GameWindowButton`（左 `MainMenu/General/Discard` / 右 `MainMenu/General/Cancel`）
        //          → `:94 WindowsManager.ShowPopUp(ToRawLocalizationString(err), localizeTexts:1, closeOnEsc:0, 左, 右)`
        //          → `:95 return`（**绝不走到 `:100` 的 `UploadDeck`**）
        //   :99-105 合法 ⇒ `:100 UploadDeck` + `:103 WindowsManager.HidePopUp(...)`（把还开着的错误窗收掉）
        // 两颗钮的**回调体**也读到了（`DeckEditingWindow___TrySaveDeck_b__42_1.c` / `..._b__42_0.c`）：
        //   左 = `HidePopUp()` + 虚槽 `0x1b8`（= `GameWindow.Close()` = **关掉卡组编辑窗**）
        //   右 = **只** `HidePopUp()`（留在编辑器里）
        // ⇒ 我们这边：左 = `HideDeckPopUp()` + `BackToMenu()`（我们是独立场景，原版那扇「窗」的等价物就是它）、
        //    右 = `HideDeckPopUp()`。**关窗那一半由调用方的回调做**（⛔ 窗自己不自动关）—— 原版就是这样。

        WindowsManager _popupMgr;
        PopUpGameWindow _popup;

        /// <summary>自检口：**离场**（<see cref="BackToMenu"/>）被调了几次。
        /// 它是那颗「Discard」唯一可观测的副作用 —— 批处理下 `BackToMenu` **不切场景**（只打一行日志）。</summary>
        public int LeaveCount { get; private set; }

        /// <summary>模态消息窗此刻**开着**吗（本类所有指针动作的第一道闸，见 `HandlePointer`）。</summary>
        public bool ModalPopupOpen { get { return _popup != null && _popup.gameObject.activeSelf; } }
        // ============================================================ 🆕 B25①：顶栏那几扇窗开着时，本窗不吃输入
        //
        // 🔴 **为什么需要它**：2026-10-17（B25）把顶栏那 4 颗钮**接上了点击**（接线在共用件 `TopBar.Build`）
        //    ⇒ 本场景里会**真的开出主菜单那一层的窗**（设置 / 信箱 / 档案）。而本类
        //    **故意不建指针层**、指针是**自己按区域派发**的（见 `Build()` 末尾那三条）—— 于是那些窗开着时：
        //      · **点击会穿透**：窗底下正好是卡组行 / 卡池 ⇒ 一次右键能从窗缝里**往卡组加一张牌**；
        //      · **ESC 会被两家同时吃**：`PointerLayer.KeyCancel` 关掉那扇窗（**这一跳是对的**），
        //        而本类 `HandleEscape` 同一帧还会 `SaveAndSay()` ⇒ **存盘 + 离场**（原版那一刻 ESC 只打给
        //        最上面那扇窗，`WindowsManager__Update.c` 那一条链，轮不到 `DeckEditingWindow`）。
        //    ⇒ 所以本类加一道闸：**别处有窗开着 ⇒ 本窗的指针与 ESC 都让位**。
        //
        // 🔴 **闸只加在【生产输入路】上**（`HandlePointer` / `HandleEscape` —— 它们只从 `Update` 进）：
        //    ⛔ **`UiClickPx` / `EscPressed()` 这两个自检直调口【不加】** —— 同 `UiPressDone` 那条先例
        //    （「它故意不做模态拦截，那正是用途」）：那两个口是「直接驱动状态」用的，自检要的正是
        //    「窗挂着的时候这个动作本身是什么样」；加了上去会把既有的一整片断言改成另一个语义。
        //
        // ⚠️ **本类那扇模态消息窗【不算】** —— 它有**自己的两道闸**（`ModalPopupOpen` 早退 + `EscPressed()` ③
        //    那一级转调 `_popup.ESCPressed()`），算进来的话 ③ 那一级就永远走不到了（A502 那一片会红）。
        //
        // ⚠️ **判据 `TopWindow`**（= `WindowsManager.currentWindow`，壳那边唯一那份「谁在最上面」的记账）·
        //    `IsOpen()` = 原版 `GameWindow.IsOpen()`（虚槽 `0x1f8`，`*(int*)(this+0x68) == 1`）。
        //
        // ⚠️ **上一帧那一份（`_winShieldPrev`）是给 ESC 用的**：`PointerLayer` 的 `Update` 排在
        //    本类**之前**（`[DefaultExecutionOrder(-50)]`）⇒ 按 ESC 那一帧它**已经把窗关掉了**，
        //    只问「此刻还开着吗」会得到 false ⇒ 本类照样存盘离场（正是要防的那件事）。
        //    ⇒ 记一份「上一帧末有没有窗开着」，两道闸读到它就知道「这一下 ESC 刚被那扇窗吃过」。

        /// <summary>上一帧末「别处有窗开着」的快照（见上面那段：给 ESC 用）。⚠️ 批处理下 `Update` 不跑 ⇒ 恒 false。</summary>
        bool _winShieldPrev;

        /// <summary>🆕 **2026-10-13（`A1132` 第 2 步）**：本类**挂在哪一扇窗下**（= 它的**宿主窗**）。
        ///
        /// <para>🔴 **为什么需要它**：第 3 步起卡组编辑**不再是独立场景**、而是**盖在壳上的一扇窗**
        /// （`Shell/DeckEditingWindow.cs` 的 `Open()`：`AddComponent&lt;DeckRuntime&gt;()` + `Build(...)`）。
        /// 那一拍之后**本窗自己就是 `wm.TopWindow`** ⇒ `OtherWindowUp()` 会**恒真**
        /// ⇒ `HandlePointer` 一进门就 `return`、`HandleEscape` 也早退 = **整个窗的点击 / 拖拽 / ESC 全死**。
        /// ⇒ 那道闸必须能分开「**自己那一扇**」与「**别的窗**」，判据就是本字段。</para>
        ///
        /// <para>**谁写它**：`Shell/DeckEditingWindow.Open()`（建 `DeckRuntime` 的那一拍、`Build` 之前）。
        /// **谁读它**：`OtherWindowUp()` **一处**。</para>
        ///
        /// <para>⚠️ **判据取「引用相等」（`ReferenceEquals(w, HostWindow)` 那一句），⛔ 不取名字** ——
        /// Unity 里名字可以重（同一个 prefab 开两扇就有两个同名 `GameObject`），而这里要答的问题是
        /// 「顶窗**是不是**我头上这一扇」，那是**对象身份**问题、不是字符串问题。</para>
        ///
        /// <para>⚠️ **独立场景那条路**（`-wfscene DeckEditor` 直起 / 批处理里裸建 `DeckRuntime`）
        /// **没有任何人写它** ⇒ 恒 `null` ⇒ `OtherWindowUp()` 与加这道豁免**之前逐位一致**
        /// （那条路上本来就没有宿主窗，`_popup` 那一档也没变）。</para>
        ///
        /// <para>⛔ **别拿它当「本类有没有被窗托管」的通用判据去做别的事** —— 它只服务这一道豁免。</para></summary>
        public GameWindow HostWindow;

        /// <summary>= 「别处（**不是**本类那扇模态窗、**也不是本窗自己那一扇**）有一扇窗正开着」→ 返回它，否则 `null`。
        /// 🔴 **唯一出处**：`HandlePointer` 与 `HandleEscape` 两道闸、以及自检口 `UiWindowShield` 都读它。</summary>
        GameWindow OtherWindowUp()
        {
            var wm = WindowsManager.Instance;
            var w = wm != null ? wm.TopWindow : null;          // Unity 的假 null 也会走这儿 ⇒ 下面那句判得住
            if (w == null || !w.IsOpen()) return null;
            if (_popup != null && ReferenceEquals(w, _popup)) return null;   // 本类那扇模态窗归它自己那两道闸
            // 🆕 **2026-10-13（`A1132` 第 2 步）：本窗自己那一扇也不算「**别的**窗」。**
            //   判据 = **引用相等**（⛔ 不是名字 —— 为什么见 `HostWindow` 的 doc）。
            //   🔴 **只豁免「自己」这一扇，⛔ 不许把整条闸删掉** —— 对「**别的窗**开着」它照样要拦：
            //     那正是 `Editor/DeckScene.cs` 里「别的窗开着 ⇒ 本窗让位」那两条断言（连同它的判别式）守的语义。
            //   ⚠️ 走到这里的 `w` **一定不是** `HostWindow` 的 Unity 假 null 情况：`HostWindow` 被销毁时
            //     上面 `!= null` 走 Unity 重载判假 ⇒ 直接跳过豁免、按老口径返回 `w`（安全的那一侧）。
            if (HostWindow != null && ReferenceEquals(w, HostWindow)) return null;
            return w;
        }

        /// <summary>🆕 **2026-10-13（`A1132` 第 2 步的**必然配套**）**：= 「这一下 ESC 已经被
        /// **本窗自己那一扇宿主窗**吃掉了」。
        ///
        /// <para>🔴 **为什么单开一格、⛔ 不能并进 `OtherWindowUp()`**：两个调用点要的方向**正好相反** ——
        /// · **指针**那道闸（`HandlePointer`）：本窗自己那一扇**必须放行**，否则窗一开整个点不动
        ///   （第 2 步那句豁免就是为它写的）；
        /// · **ESC**：`PointerLayer.KeyCancel`（`[DefaultExecutionOrder(-50)]`，**排在本类之前**）
        ///   那一帧已经把 ESC 打给了 `TopWindow.ESCPressed()`，而**本窗自己那一扇**
        ///   （`Shell/DeckEditingWindow.ESCPressed`）**就是把这一下转发回 `this.EscPressed()`** 的
        ///   ⇒ 本类若再来一次，**同一下 ESC 会被处理两遍**。
        /// 两个方向相反 ⇒ 只能各问各的（⛔ 别把这两个谓词合成一个）。</para>
        ///
        /// <para>🔴 **不修会怎样**（可推、且**现有 ESC 断言抓不到**）：同一下 ESC 被处理两遍 ——
        /// 第一遍由宿主窗转进来（保存 / 关最上面那扇窗），第二遍再落到 `EscPressed()` 的保存支
        /// ⇒ 一次按键**存两遍盘、甚至退两次编辑器**。
        /// ⚠️ 历史上这条闸的判别式是「**关导入弹窗**那一级」：第一遍关掉弹窗、第二遍 `_importOpen`
        /// 已为假 ⇒ 一路落到保存支（**既关弹窗、又把卡组存盘并退出**）。那一级 2026-10-17 已随
        /// `T23` 删件删掉 ⇒ 今天两支都落到保存支（症状变成「存两遍」），**闸本身照样必须有**。
        /// ⚠️ `Editor/DeckScene.cs` 那几条 ESC 断言**直接调 `_rt.EscPressed()`**、不经过 `PointerLayer`
        /// ⇒ 它们**看不见**这个双吃（挡住它的是 `UiEscEatenByHost` 那一对断言）。</para>
        ///
        /// <para>🔴 **哪条断言能抓住它（全仓只有这一条）** = <see cref="UiEscEatenByHost"/>
        /// （`Editor/DeckScene.cs` 第 2 步那一节，与 `UiWindowShield` 那条**配成一对**）。
        /// 已核过全仓：`Editor/DeckScene.cs` 里**所有** ESC 断言都是直调 `_rt.EscPressed()`（不经 `PointerLayer`），
        /// 而 `KeyCancel()` 那批断言（`Editor/ShellScene.cs` / `Editor/RewardsScene.cs`）**从没拿
        /// `DeckEditingWindow` + `DeckRuntime` 这一对跑过** ⇒ 删掉 <see cref="UiEscEatenByHost"/> 那一对断言
        /// 之后，本缺陷**没有任何自检能看见**（只能靠真机按一下 ESC + 开着导入弹窗）。</para>
        ///
        /// <para>⚠️ **独立场景那条路**（`HostWindow == null`）恒 `false`
        /// ⇒ 与加这一格之前**逐位一致**（那条路本来就只有本类一个人吃 ESC）。</para></summary>
        bool HostWindowEatsEsc()
        {
            if (HostWindow == null) return false;      // Unity 假 null 也走这儿（宿主窗被销毁 ⇒ 没人替本类吃 ESC）
            var wm = WindowsManager.Instance;
            var w = wm != null ? wm.TopWindow : null;
            if (w == null || !w.IsOpen()) return false;
            if (_popup != null && ReferenceEquals(w, _popup)) return false;   // 本类那扇模态窗：走 ③ 那条（同 `OtherWindowUp`）
            return ReferenceEquals(w, HostWindow);
        }

        /// <summary>自检口：这一刻本窗的指针/ESC**是不是该让位**（= 上面那道闸读的同一个谓词）。
        /// ⚠️ 批处理里驱动不了 `HandlePointer`（没有鼠标）/ `HandleEscape`（没有键盘）⇒ 自检只能问这个谓词
        /// + 走一遍「开窗 ⇒ 真 ⇒ 关窗 ⇒ 假」的循环；**「谁在读它」由代码本身保证**（两道闸就那两行）。
        /// <para>🔴 **2026-10-13（`A1132` 第 2 步）就地订正口径（铁律 5）**：本谓词从此**不再覆盖 ESC 的全部** ——
        /// `HandleEscape` 现在是 `UiWindowShield || UiEscEatenByHost`（见 <see cref="HostWindowEatsEsc"/> 的 doc：
        /// 宿主窗**自己**就是「ESC 已经被派发给它、它又转回本类」的那一档）。
        /// 本谓词的语义收窄成：**指针**那道闸读的判据 + ESC 的「**别的窗**」那一半
        /// （⛔ 原文「指针/ESC 是不是该让位」那句对它已不准确，故就地改掉而不是再写一句）。
        /// ⚠️ 两条**并存**才对得上原语义：本窗自己那一扇开着时 `UiWindowShield == false`（指针要放行），
        /// 而 `UiEscEatenByHost == true`（ESC 由宿主窗那条链处理）。</para></summary>
        public bool UiWindowShield { get { return OtherWindowUp() != null || _winShieldPrev; } }

        /// <summary>自检口：**这一下 ESC 是不是已经被本窗自己那一扇宿主窗吃掉了**
        /// （= `HandleEscape` 第二道闸读的谓词，见 <see cref="HostWindowEatsEsc"/>）。
        /// ⚠️ 存在的理由与 `UiWindowShield` 同款：批处理没有键盘 ⇒ `HandleEscape` 驱动不了，
        /// 只能问它读的同一个谓词 + 走一遍「有宿主窗且它在顶 ⇒ 真 / 没宿主窗 ⇒ 假」的循环。</summary>
        public bool UiEscEatenByHost { get { return HostWindowEatsEsc(); } }


        /// <summary>此刻那扇模态窗（自检读它；没开过 / 已关 = null）。</summary>
        public PopUpGameWindow ModalPopup { get { return ModalPopupOpen ? _popup : null; } }

        /// <summary>= 原版 `ShowPopUp(ToRawLocalizationString(err), …)` 那一句。
        /// <para>🔴 **文案是 I2 术语【键】**（`MenuDeck/Error/&lt;1..5>`；查不到词条时原版兜底**仍是另一个键**
        /// `MenuDeck/Error/InvalidDeck`）—— ⛔ **不自己编一句人话**，键由 `PopUpGameWindow.Term()` 解析。
        /// 🔴 **2026-10-19（`A1012`）就地订正（铁律 5）**：这里原来写「本地没有那张语言表（I2 在远端 CCD）
        /// ⇒ 画面今天显示的就是**键**，如实标注在 `Shell/PopUpGameWindow.cs` 文件头 ③」—— **两处都不对了**：
        /// ① `Term()` 现在转 `Core/Loc.cs`、`MenuDeck/Error/InvalidDeck` **就在那张表里**
        /// （键 `MenuDeck/Error/InvalidDeck`：「所选卡组无效，请编辑或选择其他卡组。」/`Invalid deck` —— 2026-10-14 换官方 zh-CN 表，原「卡组不合法」）⇒ 正文印的是**这条词条**；
        /// ② 那句「如实标注」的文件头编号是 **①**（本文件里也有一处写成 ③，同批改掉）。</para>
        /// <para>⚠️ **本类自己不是 `GameWindow`** ⇒ 开窗要现拿宿主：
        /// `WindowsManager.EnsureHost()` 是**幂等**的（没有就建一台 `WindowsManager` + 三颗锚点 + 指针层；
        /// 同族先例 = `Editor/RewardsScene.cs` 里那条 `EnsureHost` 最小改法注 那条「领奖窗落到第二台管理器」的修法）。
        /// 🔴 **2026-10-14（`A1132` 第 3/4 步）就地订正（铁律 5）**：本行原文写「本类不是 `GameWindow`
        /// （**独立场景**、不走 `WindowsManager` 那一套开窗链）」—— **后半句现在只对一半**：
        /// 第 3 步起本类在**生产路径**上是被一扇 `GameWindow` 托管的（`Shell/DeckEditingWindow`，
        /// 见 <see cref="HostWindow"/>）⇒ 它**确实在那条开窗链里**（开窗/关窗/ESC 都走窗口那一套）。
        /// 但**它本身仍然不是** `GameWindow` ⇒ 本类要开**自己**那扇模态消息窗（下面这扇）时照样得现拿宿主，
        /// 这一句的结论没变。
        /// ⛔ **不要把它建到 `_root` 底下** —— 窗要现建在**场景根**，否则本窗那些「所有可见 quad 都在可见区内」
        /// 的断言会被弹窗那块 4574.6×2572.36 的压暗层一起扫进去（`MenuDraw.Absorb` 的矩形比屏幕大得多）。</para></summary>
        void ShowInvalidDeckPopUp(DeckError err)
        {
            _popupMgr = WindowsManager.EnsureHost();
            _popup = _popupMgr.ShowMessagePopUp(
                MenuDeckErrorKey(err),
                // 🔴 **2026-10-14（A565）**：这颗左钮 = 原版 `.<TrySaveDeck>b__42_1`（`HidePopUp()` +
                //   虚槽 `0x1b8` = `GameWindow.Close()`）—— 与「丢改动」那颗左钮**是同一件事**
                //   （**关窗 ⇒ 窗上那份 `EditingDeck` 副本随之销毁**，库那份从头到尾没被动过）
                //   ⇒ 它也要走同一段内存回滚。⛔ 别只改 `DiscardChangesAndLeave` 那一处。
                PopUpGameWindow.KeyDiscard, () => { HideDeckPopUp(); RollBackEditingCopy(); BackToMenu(); },
                PopUpGameWindow.KeyCancel, () => { HideDeckPopUp(); });
        }

        /// <summary>= 原版 `WindowsManager.HidePopUp()`（`__TrySaveDeck.c:103` 那一句）。
        /// ⛔ **不自己判 `activeSelf`**：原版那句判的是「`currentWindow == popUpWindow` 吗」——
        /// 判据只留 `WindowsManager.HidePopUp` 一处（两处写同一条规则 = 迟早不一致）。</summary>
        void HideDeckPopUp()
        {
            if (_popupMgr != null) _popupMgr.HidePopUp();
        }

        // ---- 我们 `DeckError` → **原版 `DeckError` 的号**（键里那个 `{0}` 用的是**原版的号**，不是我们的下标）
        //
        // 原版枚举（`global-metadata.dat` 实读，同 `dump.cs:21813-21823`）：
        //   `None=0 · CardsNotOwned=1 · MissingHero=2 · InvalidDeckBannedCards=3 · InvalidDeck=4 · IncompleteDeck=5`
        /// <summary>原版 `DeckUtility.ToRawLocalizationString` 拼键用的**格式串**（`DAT_1842cfbf0` 的地址表读数，
        /// 见 `资料/普查产出_1011/WB1_A330.md` §2.4 的四行表）。</summary>
        public const string MenuDeckErrorKeyFmt = "MenuDeck/Error/{0}";
        /// <summary>原版那句 `GetTermData(键) == null` 时的**兜底**（`DAT_1842cf5f0`）——
        /// 🔴 **它也是一个【键】**，不是明文（原版永远不自己编文案）。</summary>
        public const string MenuDeckErrorKeyFallback = "MenuDeck/Error/InvalidDeck";
        // 逐条对位（判据行号见 `资料/普查产出_1011/WB1_A330.md` §2.2 那张 13 行的表）：
        //   · `NoWarlord`        → **2** `MissingHero`       —— 直接对应（`__ValidateDeck.c:32,88`）
        //   · `TooFew/TooMany`   → **5** `IncompleteDeck`    —— 原版那条是「张数**恰好等于** deckSize」（`:69` ⇒ `:80`）
        //   · `WarlordNotHero`   → **4**（`:36` ⇒ `:84`）· `WrongFaction` → **4**（`:58-62`）
        //   · `WarlordInCards`   → **4**（`:63-67`）· `CopyLimitExceeded` → **4**（`:111-121`）
        //   · 🆕 **2026-10-25（`E1` · 第六段）：`LegendaryTotalExceeded` → 也走 `default` = 4** ——
        //     它就是原版 `:111-121` 那一支（整副传奇张数），与 `CopyLimitExceeded`（同名上限）**共用同一个号 4**；
        //     两档的**文案**靠 `DeckRules.Describe` 分开（`MaxCopiesReached` vs `MaxLegendayCopiesReached`），
        //     ⛔ 不要为它单独编号（原版没有第 6 个号）。
        //   · `EffectOnlyCard`   → **4**（原版对应判据是 `inventoryOptions != InInventory` ⇒ `:68-72` 归 4）
        //   · **1（`CardsNotOwned`）与 3（`InvalidDeckBannedCards`）我们【永远不回】** ——
        //     前者是「持有数」、单机全解锁 ⇒ 恒真；后者是**事件禁卡表**（LiveOps 远端下发、本地没有）。
        //     ⚠️ 那两条正是「我们要不要显示 1 / 3」的答案：**没有那个数据源**，不是漏做
        //     （`资料/预组卡组_原版规格.md` §五之五 早记过同样两句）。
        //   · `DefensiveNotDefence` → **4**：⚠️ **这是我们自己的槽位判据**（原版不查防御卡，见
        //     `RuleEngine/Core/DeckRules.cs` 的 `DefensiveNotDefence`）⇒ 归原版的 catch-all，**如实标**不是原版的细分。
        //   · `UnknownCard` → **4**：🔴 **没查清**——原版那一路会**先**过 `ValidateDeckOwnership`（`:16-17`），
        //     而一个不在库里的 id 在那里**大概是**「持有数 0 ⇒ err=1」；但 `InventoryManager.GetOwnedCount`
        //     对未知 id 的返回值**本件没查到**（方法体在 `decomp_full` 里是那一大坨泛型/LINQ 展开）⇒
        //     **按原版的 catch-all（4）落账，不猜 1**。
        /// <summary>我们的 `DeckError` → **原版 `DeckError` 的号**（`MenuDeck/Error/{0}` 里那个 `{0}`）。
        /// ⛔ **别改我们的枚举去对齐原版**（下标被 `Describe` 的文案表与历史断言引用过）⇒ 映射只此一处。
        /// <para>🔴 **2026-10-25（`E1` · 第八段）：上面那张「逐条对位」表引的 `.c` 出处【待核】。**
        /// 复核时发现：`DeckUtility__CanAddCard.c` **在 `decomp_full` 里不存在**（只有 `CardDeck__CanAddCard.c`，183 行）；
        /// `DeckUtility__ValidateDeck.c`(96) / `ValidateDeckCards.c`(130) / `ValidateDeckWarlord.c`(54) **全部只 `return 0/1`（bool）**、
        /// 不返回错误号；而现表引的是 `__ValidateDeck.c:32,88 / :69 / :80 / :111-121` —— 行号**超出这三个文件的长度**。
        /// ⇒ **号码本身沿用旧结论（`A330` 报告），本轮未能独立复核**；下一批若要动它，
        /// **按内容认函数、别按文件名认**（G4「文件名与内容不符」那一族）。
        /// ⚠️ 但**今天不影响任何上屏**：号只在 `UsesNumericErrorText` 为真的 10 档上用，
        /// 而那 10 档的号里 `2` / `5` / `4` 三个值全部来自这张表 —— **6 个原版值先当真值用，⛔ 不要因为复核不了就改号**。</para></summary>
        public static int MenuDeckErrorNumber(DeckError e)
        {
            switch (e)
            {
                case DeckError.NoWarlord: return 2;         // MissingHero
                case DeckError.TooFewCards:
                case DeckError.TooManyCards: return 5;      // IncompleteDeck
                default: return 4;                          // InvalidDeck（原版的 catch-all）
            }
        }

        /// <summary>= 原版 `DeckUtility.ToRawLocalizationString(err)`（`DeckUtility__ToRawLocalizationString.c`）：
        /// `err==0` ⇒ 空串；否则 `string.Format("MenuDeck/Error/{0}", err)`；
        /// **该键在词条表里查不到 ⇒ 返回兜底【键】** `MenuDeck/Error/InvalidDeck`（地址表实读，见 `A330` §2.4）。
        /// 🔴 我们这边「查得到吗」= **`Loc.HasEntry(key)`**（= 全工程唯一那张语言表 `Core/Loc.cs`）。
        ///
        /// <para>🔴 **2026-10-19（`A1012`）就地改掉判据（铁律 5）**：原来查的是
        /// `PopUpGameWindow.Terms.ContainsKey(key)`，而**那张表是恒空的**（它自己文件头写着「故意留空」）
        /// ⇒ **今天恒判「查不到」、永远退成兜底键** `MenuDeck/Error/InvalidDeck`，
        /// **永远说不出真正的不合法原因**（看着有判据、判据却指向一张空表 —— 一处**静默失败**）。
        /// 换成 `Loc.HasEntry` 之后：`Loc` 表**里有的**键就真的用它。
        /// ✅ **2026-10-09 已补（第九会话 · `A1133`）：`MenuDeck/Error/{2,4,5}` 三条数字键现在就在 `Loc` 表里**
        ///   （键名照原版的**数字族**；判据 = `DeckUtility__ToRawLocalizationString.c` 的格式串 + 原版枚举
        ///   `DeckError.cs` 只返 2/4/5 ⇒ **不补 1/3**，补了就是永不触发的空键；
        ///   ⚠️ 那两列文案在**远端 I2** 取不到 ⇒ `Loc` 里这两条是**自拟**、已如实标注）。
        ///   ⇒ 原写「今天仍不在 `Loc` 表里 …… 补词条是**另一笔账**」**已作废**（铁律 5 就地订正）——
        ///   **兜底键不再是今天的可见结果**，号 2/5 现在真能说出「缺战将」/「张数不对」那两类原因。
        ///   ⚠️ 号 **1（`CardsNotOwned`）与 3（`InvalidDeckBannedCards`）仍然不回**（没有那个数据源，见上），
        ///   ⛔ 别为它们编号、也别在这里编文案。</para>
        /// <para>⚠️ **与 `PopUpGameWindow.Term` 的分工**：本函数只管「**用哪个键**」，
        /// 「键 → 屏上那句字」由 `Term()` 负责（`Shell/PopUpGameWindow.cs`）。
        /// ⇒ 两处都改成看 `Loc` 之后，**同一条表**说话，不会再出现「判据说没有、印的却有」。</para></summary>
        public static string MenuDeckErrorKey(DeckError e)
        {
            if (e == DeckError.None) return "";
            string key = string.Format(MenuDeckErrorKeyFmt, MenuDeckErrorNumber(e));
            return Loc.HasEntry(key) ? key : MenuDeckErrorKeyFallback;
        }

        // ============================================================ 分享 / 导入

        /// <summary>🔴 **分享的【唯一】实现** —— 造卡组串 → **写系统剪贴板** → 返回那一串。
        /// <para>**今天唯一的调用点** = 卡组线 Info 弹窗那颗 `Share` 圆钮
        /// （`Shell/DeckInfoPopup.ShareDeck("Share")`，2026-10-08 `A1040` 接上）。</para>
        /// <para>⚠️ **2026-10-23（W65 · 1c）就地订正（铁律 5）**：这段原来写「**两个**调用点都走这里 ——
        /// 卡组编辑那颗 `Share`（`ShareDeckString`）与 Info 弹窗那颗」—— **前半个已不存在**：卡组编辑窗侧栏那两颗钮
        /// （`info_share` / `info_import`）原版就没有、`D35` 已删 ⇒ `DeckRuntime.ShareDeckString()` 成了
        /// 全仓 **0 调用点**（连自检都没有）的死代码，本轮**整段删掉**（同 `OpenImport` 那一族的处理，
        /// 留痕见下面那段注释）。⛔ 别把侧栏那两颗钮加回来当它的入口。</para>
        /// <para>**返回空串** = `deck == null` ⇒ 这一副导不出卡组串 ⇒ 调用方**必须出声**（本项目红线：
        /// ⛔ 不许静默失败）。⚠️ 空串**不写剪贴板** —— 原来那份「空串也照样写、还说『已复制（0 字符）』」
        /// 是**假报**。</para>
        /// <para>⚠️ 原版第 ③ 句（那条 toast）**不在本函数里**：两个宿主的出声通道不同
        /// （卡组编辑 = `Say()` / Info 弹窗 = `WindowsManager.ShowPopUp`）⇒ 各出各的，但**文案共用**
        /// <see cref="ShareCopiedText"/>，⛔ 别各写一份。</para>
        /// <para>⚠️ **`GUIUtility.systemCopyBuffer` 在 `-batchmode -nographics` 下能不能真写进【系统】剪贴板 ——
        /// 本件【没查实】**（红线不许跑 Unity）。**这不构成「不做」的理由**：原版调的就是这个 API，照调即可；
        /// 同进程内写进去再读回来是准的（自检就靠这个读回）。</para></summary>
        public static string CopyDeckToClipboard(PlayerDeck deck)
        {
            string s = deck != null ? DeckLibrary.ExportString(deck) : "";
            if (string.IsNullOrEmpty(s)) return "";     // 空串 ⇒ 不写剪贴板（⛔ 别把空串塞进用户的剪贴板）
            GUIUtility.systemCopyBuffer = s;
            return s;
        }

        /// <summary>分享成功那一句提示的**唯一文案**（两个宿主共用 —— 原版只弹一条 `ShowMessage`）。</summary>
        public static string ShareCopiedText(int len)
        {
            return "卡组串已复制到剪贴板（" + len + " 字符）";
        }

        // 🔴 **2026-10-23（W65 · 1c）删件：`ShareDeckString()` 整段删掉了** —— 全仓 **0 调用点**
        //   （`grep -rn ShareDeckString CardPresentation/` 只命中它自己的定义与注释；`Editor/DeckScene.cs`
        //   的自检也从来没调过它）。它原来挂在卡组编辑窗侧栏那两颗 `info_share` / `info_import` 圆钮上，
        //   而那两颗钮**原版这扇窗里没有**、`D35` 已删 ⇒ 从那天起它就是死代码。
        //   **这不是「不做」**（铁律 11）：原版这个动作的家在 **`DeckInfoPopup`**
        //   （`decomp_full/DeckInfoPopup__ShareDeck.c`，卡组线那扇 Info 弹窗的 `Share` 圆钮）——
        //   ✅ **2026-10-08（`A1040`）那边已接上**：`Shell/DeckInfoPopup.ShareDeck("Share")` 调的是**同一个**
        //   `CopyDeckToClipboard`（共用件仍在上面，⛔ 别删）。方法本体已无存在理由 —— 留着只会让下一个会话
        //   以为「卡组编辑那边还有一条分享路」（它原来那份 doc 就是这么误导的：写着「保留（`public`，可自检调）」，
        //   而**没有任何自检调它**）。
        //   ⛔ 别把侧栏那两颗钮加回来当「权宜入口」。

        // 🔴 **2026-10-17（T23 删件）：`OpenImport()` / `CloseImport()` / `TryImport()` /
        //   `RefreshImportText()` 四个方法整段删掉了**（连同 `ImportOutcome` 枚举与 `LastImportOutcome`）。
        //   它们是 `BuildImportPopup` 那棵树的行为侧 —— 生产入口早已没有，只剩 `Editor/DeckScene.cs` 的
        //   自检在调。真正的导入链在外壳：`Shell/CollectionData.ImportDeck`（走 `DeckLibrary.ImportString`，
        //   A503 / A547 那一份，`CollectionScene` 有断言）。
        //   ⚠️ 分享那一族**没删**（`CopyDeckToClipboard` / `ShareCopiedText`）—— 它们的调用方是
        //   `Shell/DeckInfoPopup.ShareDeck("Share")`（2026-10-08 `A1040` 接上的那条链）。
        //   ⚠️ 2026-10-23（W65 · 1c）：同族的 `ShareDeckString()` 已删（它才是 0 调用点那个），见上面那段留痕。

        /// <summary>Deck info 页签开着吗 —— **`_infoOnly` 那批件的显隐判据（唯一出处）**。
        /// `RefreshTabVisibility` 与 `KeyLive` 共用它；不许各写一遍 `_tab == 1`
        /// —— 那正是 A41 ④「看不见却能点」的来源（两处写同一条规则 = 迟早不一致）。</summary>
        bool InfoTab { get { return _tab == 1; } }

        /// <summary>🆕 **2026-10-11（`A1516`）**：**卡组列表那一页**（= Cards）—— 原版 `Deck List drawer`
        /// 只在 Cards 页签下（`DeckEditingPanel__ToggleDeckInfo.c` 的 `deckList(0x28).SetActive(0)`；
        /// 字段名↔偏移按 `d:/2/tools/il2cpp_out/dump.cs` 的 `DeckEditingPanel` 实读：
        /// **`deckList` @ 0x28** · `deckCosmeticDrawer` @ 0x30 · **`deckCounterDrawer` @ 0x38** ·
        /// `deckCostDrawer` @ 0x40）。
        /// <para>它是**卡组行那一整套**（画 / 命中 / 悬停 / 滚轮）的页判据 —— 与 `InfoTab` 同一个形状。
        /// ⛔ **别在别处再写一遍 `_tab == 0` / `_tab != 0`**（铁律：两处写同一条规则 = 迟早不一致）。</para></summary>
        bool DeckListPage { get { return _tab == 0; } }

        /// <summary>🔴 **2026-10-11（`A1497④`）**：**原版 `DeckEditingWindow.ToggleDisplay(param)` 的那个入参**
        /// （= 「`Content Area/Card Display` 这一整页在不在」）—— **Cards 页与 Deck info 页都真、只有 Cosmetics 页假**。
        ///
        /// <para>**判据**（第一权威 = 反编译方法体，2026-10-11 主对话逐行现核）：
        /// `d:/2/tools/decomp_full/DeckEditingWindow__ToggleDisplay.c` —— 一个入参管三件：
        /// `cardDisplay(0x78).SetActive(param)` · `cosmeticDisplay(0x80).SetActive(param ^ 1)` ·
        /// `wildcards(0xd0).SetActive(param)`；三个调用点里 `DeckEditingStateController__ToggleDeckInfo.c`
        /// 与 `__ToggleDeckList.c` 传 **1**、`__ToggleDeckCosmetic.c` 传 **0**。
        /// 字段名↔偏移按 `_probe_deckinfo.py` 实读钉死（`cardDisplay`→GO「Card Display」·
        /// `cosmeticDisplay`→GO「Cosmetic Display」· `wildcards`→GO「WIldcard Counter」）。</para>
        ///
        /// <para>⚠️ **`Card Display` 那一整棵子树都吃这一个入参** —— 里面除卡池（`Scroll View`）之外还有
        /// `Card Filters` 与 `Card Drag Controller`（父链现核：`Card Drag Controller` 的父**就是** `Card Display`）。
        /// 本属性今天被 **① 卡池显隐**（`RefreshPool`）· **② 通配符计数条整条**（`RefreshTabVisibility`
        /// 经 `_cardsInfoOnly`）· **③ `Card Filters` 左抽屉**（`SyncFilterDrawerPage` →
        /// `_fltSlide.PageOn`，2026-10-11 补，`A1515`）读。
        /// ⚠️ 仍然没接的是 **`Card Drag Controller`**（我们挂在 `Root` 下 —— 行为等价，因为
        /// 卡池与卡组行都在别的页关着、起不了拖；如实登记，见 `DeckRuntime` 里 `BuildCardDrag` 的注释）。</para>
        ///
        /// <para>⛔ **别在别处再写一遍 `_tab != 2` / `_tab == 0`** —— 两处写同一条规则 = 迟早不一致。</para></summary>
        bool CardDisplayPage { get { return _tab != 2; } }

        /// <summary>🔴 **2026-10-11（`A1515`）：`Card Filters` 左抽屉的页判据 —— 唯一出处。**
        ///
        /// <para>**原来错在哪**：`_fltSlide.PageOn` **全文件从没被赋过值**（`FilterDrawer` 的字段初值是
        /// `true`）⇒ `_filtersOpen == true` 时切到 Cosmetics 页，**原版整页不画、我们照画**；
        /// 连带 `FltStripOn` 那一档也漏判（左侧那条竖带会被**卡牌筛选**吃掉，而那一刻该吃的是卡背筛选）。
        /// 卡背那半早就接了（`RefreshCosmoFilters` 里的 `_cosmoFltSlide.PageOn = (_tab == 2)`）。</para>
        ///
        /// <para>**判据**（同 `CardDisplayPage` 那段，原版是**一个** `SetActive(param)`）：
        /// `d:/2/tools/decomp_full/DeckEditingWindow__ToggleDisplay.c` 的 `cardDisplay(0x78)` —— 那棵
        /// `Card Display` 底下就挂着 `Card Filters`（`menu_dump.py bundle_menus_assets_all
        /// "Deck Editing Menu"` 的缩进：`2 Card Display` → `3 Card Filters`）⇒ 页一关，整栏跟着不画。
        /// ⚠️ 我们把它建在 `Root` 下（**没有**那格父容器）⇒ 页判据只能自己补在 `PageOn` 上。</para>
        ///
        /// <para>**为什么要单独一个函数**：`PageOn` 只在**一处**赋值；两个调用点
        /// （切页签 `RefreshTabVisibility` / 抽屉内容重建 `RefreshFilters`）都走它，⛔ 别各写一遍。</para></summary>
        void SyncFilterDrawerPage()
        {
            // ⚠️ 建之前（`BuildFilters` 排在 `Build()` 后段）会被调到 ⇒ 按「出声、不静默」处理
            //    （同 `RefreshFilters` / `RefreshCosmoFilters` 那两条同款守卫）。
            if (_fltSlide == null || _fltSlide.Node == null)
            {
                Debug.LogWarning("[Deck] `SyncFilterDrawerPage` 在 `BuildFilters` 之前被调了 —— 这一次忽略"
                                 + "（抽屉容器还没建，`Build()` 里的顺序别动）");
                return;
            }
            _fltSlide.PageOn = CardDisplayPage;
            // 页判据是**瞬时**的（照原版「整页 `SetActive`」）：推一次滑动引擎 ⇒ 显隐 / 命中一起对齐
            // （`ApplyDrawerSlide` 里 `live = (…滑过…) && d.PageOn`、`Interactive = … && d.PageOn`）。
            ApplyDrawerSlide(_fltSlide, _fltSlide.Slide);
        }

        /// <summary>页签决定「侧栏那三组东西谁显示」：卡组行(Cards) / 费用曲线+动作钮(Deck info) / 饰品页(Cosmetics)。</summary>
        void RefreshTabVisibility()
        {
            bool info = InfoTab, cosm = _tab == 2;
            foreach (var go in _infoOnly) if (go != null) go.SetActive(info);
            foreach (var go in _cosmOnly) if (go != null) go.SetActive(cosm);
            // 🆕 **2026-10-11（`A1497④`）**：**Cards 页 + Deck info 页**那一批（= 通配符计数条整条）——
            //   与卡池**同一个判据**（`CardDisplayPage`，原版同一个 `SetActive(param)`）。
            //   ⚠️ 它**不**进 `_infoOnly` / `_cosmOnly`：那两张表都是「单页」判据（`_tab == 1` / `_tab == 2`），
            //     本件是**两页**判据 —— 塞进去必错一页。
            foreach (var go in _cardsInfoOnly) if (go != null) go.SetActive(CardDisplayPage);
            ApplyCosmCellVisibility();          // 卡背格（同一个判据的第二个消费者，见那个方法）
            RefreshCosmoFilters();              // 🆕 2026-10-01：卡背那个抽屉（同样的判据：只在 Cosmetics 页露）
            // 🆕 **2026-10-11（`A1515`）**：**卡牌那栏左抽屉也吃同一个页判据** —— 原版它挂在
            //   `Card Display` 底下，页一关整栏跟着不画（判据 → `SyncFilterDrawerPage` 的 doc）。
            SyncFilterDrawerPage();
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
                var P = Pool;                      // 🆕 D24：档位由开关决定（与 `RefreshPool` 同一条式子）
                int rows = Mathf.Max(1, Mathf.CeilToInt(State.VisibleCards().Count / (float)P.Cols));
                // 与 `RefreshPool` 里那条**同一条式子**（原来两处各写一遍、还差一个 −16 的拍脑袋项）
                return Mathf.Max(0f, rows * P.CellH - PoolH);
            }
        }
        /// <summary>一屏铺几行（`RowVisible` 的自检读数 —— 自检里⛔ 别写死这个数，写死就会在它变的时候静默漏测）。
        /// 🔴 **2026-10-21（`A1727`）**：它是 **12**（原来 11 ⇒ 最后一行永远看不到，判据 → `RowVisible`）。</summary>
        public int DeckRowVisible { get { return RowVisible; } }

        /// <summary>卡组列表的**视口矩形**（画布 px · 左上/右下）—— 自检断「行被裁到框里」用。</summary>
        public void UiDeckBand(out float x1, out float y1, out float x2, out float y2)
        {
            x1 = DeckListBand.x1; y1 = DeckListBand.y1; x2 = DeckListBand.x2; y2 = DeckListBand.y2;
        }

        /// <summary>卡组列表能滚多远（自检口）。🔴 **2026-10-21（`A1721`）**：与 `RefreshDeckList` 里那条
        /// **同一条式子**（`n × 57.7 − 2.0 − 视口高`，减掉的是 `n−1` 个 `m_Spacing` 里的那一个）
        /// —— ⛔ 别在这里另写一份（两处写同一条规则迟早不一致）。</summary>
        public float MaxDeckScrollPx
        {
            get { return Mathf.Max(0f, DeckRows().Count * RowPitch - RowSpacing - ListH); }
        }

        public void UiToggleFilters() { ToggleFilters(); }

        // ---- 🆕 2026-10-01：卡背页那个抽屉（`Cosmetic FIlter`）的自检读数 ----
        /// <summary>卡背抽屉开着没有。</summary>
        public bool CosmoFiltersOpen { get { return _cosmoFltOpen; } }
        /// <summary>抽屉里现在几格（关着 / 不在 Cosmetics 页 = 0）。开着 = **13 个阵营 + 1 个 Owned = 14**。</summary>
        public int UiCosmoFilterCellCount { get { return (_cosmoFltOpen && _tab == 2) ? _cosmoFltCells.Count : 0; } }
        /// <summary>某个 key 的格子（屏幕绝对 px + 选中态）。key = `$fac:&lt;阵营名>` / `$owned`。
        /// 🔴 **2026-10-18（A889）**：这一口与**建库**同源 —— 走 <see cref="FltAbsCosmo"/>（**卡背自己的**
        /// 滚动量），⛔ **不是** `FltAbs`（那是卡牌抽屉的）。改一处不改另一处 = 读数与画面对不上。</summary>
        public bool UiCosmoFilterCell(string key, out float x, out float y, out float w, out float h, out bool on)
        {
            foreach (var c in _cosmoFltCells)
                if (c.Key == key)
                {
                    var r = FltAbsCosmo(c.R.x1, c.R.y1, c.R.x2, c.R.y2);
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
        /// <summary>按当前筛选**会铺出来几张卡背**（判据走 `CosmeticList()` 那一份，别在自检里另算）。</summary>
        public int UiCosmoShownCount { get { return CosmeticList().Length; } }
        /// <summary>自检用：走一遍抽屉里的点击（`$fac:X` / `$owned`）。</summary>
        public void UiCosmoFilterRow(string key) { ApplyCosmoFilter(key); }
        public void UiSetTab(int t) { SetTab(t); }
        public void UiScrollPool(float dy) { _poolScroll = Mathf.Max(0f, _poolScroll + dy); RefreshPool(); RefreshHeader(); }
        public void UiScrollDeck(float dy) { _deckScroll = Mathf.Max(0f, _deckScroll + dy); RefreshDeckList(); }
        /// <summary>🆕 2026-10-07（A92 的自检口）：滚**筛选抽屉**（`_fltScroll`）—— **这是自检口，不是生产路径**
        /// （生产那条是 `HandleScroll` 里筛选那一支的滚轮，本口子照抄它算 `max` 的那一句）。
        /// `dy > 0` = 往下滚（符号同 <see cref="UiScrollPool"/>）；夹在 `[0, 内容高 − 抽屉高]`。
        /// **返回夹完之后的新滚动位** —— 自检靠它把「读完整回 0」钉成一条断言，
        /// 而**不必再开第二个只读口**（滚 `1e6` 会停在 `ContentHFor(State) − FltH`，出参就是这个数）。
        /// 🔴 **为什么夹在这里、不夹进 `RefreshFilters`**：`RefreshFilters` 是**建库路径**，
        ///   而 `_fltScroll` 眼下只有 `HandleScroll` 那一句 `Clamp` 定了范围（另一个写点是 `ClearFilters` 的归 0）
        ///   ⇒ 照抄那一句，语义一致。
        /// 🔴 **2026-10-10（A224②）订正**：这里原来写「**它不改任何建库行为**：`RefreshFilterCells` 里
        ///   「滚出面板的不建」那句裁切照旧……Cost/Type 两族首格在滚到 0 时恒被那句 `continue` 跳过」——
        ///   **那句早退已经删掉了**（原版是全量 `Instantiate`，见 `RefreshFilterCells` 那段）。
        ///   今天这个口子的用途只剩一个：把自检**挪到别的滚动位**去读（滚到底 = `464.92`）。
        ///   ⚠️ 数字那一半仍然对：本窗 `Factions()` 恒 13 ⇒ Army 行 550 高
        ///   ⇒ `ContentH = TypeTop 1239.02 + 150 = 1389.02` > 可见 924.1 ⇒ **可滚 464.92**；
        ///   Cost/Type 两族首格在滚到 0 时的绝对 y = **1230.02 / 1470.02**（仍在带口
        ///   `[FltY, FltY+FltH] = [156, 1080.1]` 外 —— 只是现在它们**也建出来**了，只是不画）。</summary>
        public float UiScrollFilters(float dy)
        {
            float max = Mathf.Max(0f, FilterPanelModel.ContentHFor(State) - FltH);
            _fltScroll = Mathf.Clamp(_fltScroll + dy, 0f, max);
            RefreshFilters();     // 重建 = `ClearFilterCells` + **全量建**那些格子（越界的由 `ClipCellToBand` 截掉）
            return _fltScroll;
        }
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

        /// <summary>🆕 **2026-10-24（`W6` · `G1`）**：自检读数 —— 卡池第 `view` 屏位那条**张数条底板**
        /// 上**真正生效**的 `_GreyScale`（0 = 原色 / 1 = 全灰）。
        /// <para>返回 **`-1`** = 那一格没有底板 / 材质没有这个属性（与「0 = 不灰」区分开 ——
        /// 断「不灰」时**必须**先断 `>= 0`，否则通道没接上也会假绿）。</para>
        /// <para>⚠️ 它读的**只是底板**（`poolbar_*`）；旁边那个数字（`poolcnt_*`）是独立 `Label`、
        /// 原版就不灰（判据 → `G1` §2·B）。</para></summary>
        public float UiPoolCounterGrey(int view)
        {
            if (view < 0 || view >= _poolBars.Count || _poolBars[view] == null) return -1f;
            return _poolBars[view].Grey;
        }

        /// <summary>🆕 **2026-10-13（A1702②）**：自检读数 —— `RefreshPool` **上一趟**建了几张卡视图
        /// （`CardView.Create` 的调用次数；每次 `RefreshPool` 开头清零）。
        /// <para>用途是**数出「滚一格重建几张」**：桌面一屏 24 格、格高 384 ⇒ 往上/下滚**整整一行**时，
        /// 新进来的只有那一行 ⇒ 应当只建 **列数** 张（6），⛔ 不是 24。
        /// 同一条式子内滚（`firstRow` 没变）⇒ **0 张**。</para>
        /// <para>⚠️ 它**不是「原版参数」**，是本项目自己的**机制读数**（与 `Ui*ForTest` 那一族同性质）——
        /// 断它断的是「重建的张数」，不是版面对不对（版面另有 `UiPoolCellRect` 那一族管）。</para></summary>
        public int UiPoolCreatedLastRefresh { get { return _poolCreatedLast; } }

        /// <summary>卡组列表第 i 行现在**显示**的是哪张卡（那一行不可见就给 null —— 页签不对也算不可见）。</summary>
        public CardDef UiDeckRowAt(int i)
        {
            if (!DeckListPage) return null;
            var shown = DeckRows();
            int idx = Mathf.FloorToInt(_deckScroll / RowPitch) + i;
            return (i >= 0 && i < RowVisible && idx >= 0 && idx < shown.Count) ? shown[idx] : null;
        }

        /// <summary>某个具名图在不在（自检用来点验「Header/页签/Footer 这些件真的建了」）。
        /// ⚠️ 也认**场景里同名的一棵子树** —— 行底/行描边现在是**九宫格**（`CreateNineSlice` 建的是根 + 9 块，
        /// 根不进 quad 登记表），所以不能只查 `Lookup(key)`（2026-09-23 踩）。
        /// <para>🔴 **2026-10-07（A77 ⑭）：补齐第三步 `FindDeep`** —— 找法统一到
        /// `UiNodeRect`（A67）/ `UiQuadActive`（A57 ①）那三步：`Lookup` → `Root.Find` → **`FindDeep`**。
        /// `Transform.Find` **只认直接子件** ⇒ 挂在容器（`flt_drawer` / `cosmoflt_drawer`）底下的件
        /// （`flt_input` 那类九宫格/子树，**不进 `_named`**）以前在这里**静默答 `false`** = 谎报「没建」。
        /// ⚠️ 优先序**没动**（`Lookup` → 直接子件 → 深查找）⇒ 既有 key 找到的还是同一个节点、
        /// 既有断言**一个数都不变**；这一步只把「以前找不到」的那些变成**找得到**。
        /// ⚠️ 三步都找不到 ⇒ 照旧答 `false`（这是本读数**既有**的契约，不是本批新加的静默 ——
        /// 自检那条会把实得值打出来）。判据 → `资料/待办判据_审查发现_1005.md` §⑭。</para></summary>
        public bool UiHasQuad(string key)
        {
            return Lookup(key) != null
                || (Root != null && (Root.Find(key) != null || FindDeep(Root, key) != null));
        }

        /// <summary>世界坐标 → 画布 px（**静态**版：自检算版面用；`ToPx` 那份是实例版、走同一条式子）。
        /// 🔴 两处必须同一条式子 —— 2026-09-23 之前自检另抄了一份 `x*108+960`，已收口到 `LayoutSpace`。</summary>
        public static Vector2 PxOfWorld(Vector3 world)
        {
            return new Vector2(world.x * PxPerUnit + ScreenW * 0.5f, ScreenH * 0.5f - world.y * PxPerUnit);
        }

        /// <summary>卡池第 `i` 格的**卡位**矩形（自检比版面用）。
        /// ⚠️ 给的是**卡位**（桌面 262.5×384 / 小屏 393.75×576 —— 由开关决定，见 `Pool`），
        /// **不是卡本身** —— 卡缩到 `P.CardScale`，
        /// 那个由 `CardView` 的 `localScale` 管（自检另有 `CardViewScaleOf` 量它）。</summary>
        public bool UiPoolCellRect(int i, out float cx, out float cy, out float w, out float h)
        {
            cx = cy = w = h = 0f;
            var go = Root != null ? Root.Find("pool_" + i) : null;
            if (go == null || !go.gameObject.activeSelf) return false;
            var p = PxOfWorld(go.localPosition);
            var P = Pool;
            cx = p.x; cy = p.y; w = P.CellW; h = P.CellH;
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

        /// <summary>某个具名 `Label` 现在写的字（自检读它 —— `_named` 只登记 `ImageQuad`，文字得按名字找）。
        /// <para>🔴 **2026-10-09（A190）：补齐第二步 `FindDeep`** —— 它是**同族第五条读数**，而
        /// A77 ⑭ 那轮只把另外四条（`UiHasQuad` / `UiTextureName` / `UiQuadCount` / `UiQueueOf`）
        /// 统一到 `Lookup` → `Root.Find` → **`FindDeep`** 的找法，**漏了这一条**：
        /// 它当时连 `Lookup` 都没有、**只有一步 `Root.Find`**（`Transform.Find` 只认直接子件）
        /// ⇒ 对挂在**容器**（`flt_drawer` / `cosmoflt_drawer`）底下的 `Label`
        /// （`flt_input_t` · `flt_title_*` —— 都不在 `_named` 里）**静默答 `null`** = 谎报「这颗标签不存在」。
        /// ⚠️ 优先序**没动**（直接子件优先）⇒ 既有 key（`poolcnt_0` 那些直接子件）读到的是同一个节点、
        /// 既有断言**一个数都不变**；这一步只把「以前找不到」的那些变成**找得到**。
        /// ⚠️ 两步都找不到 ⇒ 照旧答 `null`（这是本读数**既有**的契约，不是本次新加的静默）。
        /// 判据 → `资料/待办判据_1007.md` §A190（同族形状见 `UiQuadActive` / `UiNodeRect`）。</para></summary>
        public string UiLabelText(string key)
        {
            if (Root == null || string.IsNullOrEmpty(key)) return null;
            Transform t = Root.Find(key);
            if (t == null)
            {
                var deep = FindDeep(Root, key);
                if (deep != null) t = deep.transform;
            }
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

        /// <summary>🆕 **2026-10-17（D35 删件）**：自检口 —— 某个 key **还登记在 `_btns` 里吗**。
        /// 专门给「**它不在了**」那类反向断言用（节点删了、矩形却还留着 = 「看不见却能点」那一族缺陷）。
        /// ⚠️ 与 `UiBtnRect` 是**两条判据**：`UiBtnRect` 答「矩形是什么」，本条答「有没有这一条」——
        /// 一条「X=0,Y=0,W=0,H=0 的空矩形」会骗过前者。</summary>
        public bool UiBtnRegistered(string key)
        {
            foreach (var b in _btns) if (b.Key == key) return true;
            return false;
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
        /// 所以自检要按**渲染矩形**比、别按几何中心比（正本 §③ 末条）。
        /// <para>🔴 **2026-10-11（`A1518②`）：补上「它现在露没露」—— 不显示时答 `false`。**
        /// 本条原来**只管量尺**（读 `_wcTxt[i].WorldW/WorldH`），**不看 `activeSelf`** ⇒ 拿它断
        /// 「Cosmetics 页上这 4 个数字**不显示**」**恒绿**（藏起来了照样量得出尺寸）—— 正是本仓最怕的
        /// 那族假读数。⚠️ 判据用 **`activeInHierarchy`**（不是 `activeSelf`）：4 颗数字的开关落在
        /// **它们自己**身上（`RefreshTabVisibility` 的 `_cardsInfoOnly` 逐件 `SetActive`）——
        /// 用 `activeInHierarchy` 是**更严**的那一档，将来若改成关父容器也照样成立
        /// （同 `UiQuadActive` 那条的理由）。</para></summary>
        public bool UiWcCounterRect(int i, out float cx, out float cy, out float w, out float h)
        {
            cx = cy = w = h = 0f;
            if (i < 0 || i >= 4 || _wcTxt[i] == null) return false;
            if (!_wcTxt[i].gameObject.activeInHierarchy) return false;   // 🆕 A1518②：藏着的量不出「渲染矩形」
            var p = ToPx(_wcTxt[i].transform.localPosition);
            cx = p.x; cy = p.y; w = _wcTxt[i].WorldW * PxPerUnit; h = _wcTxt[i].WorldH * PxPerUnit;
            return true;
        }

        /// <summary>通配符计数条第 `i` 个数字现在写的是什么（自检用）。</summary>
        public string UiWcText(int i) { return (i >= 0 && i < 4 && _wcTxt[i] != null) ? _wcTxt[i].Text : null; }

        /// <summary>一个具名节点（**含九宫格那种子树根**）里第一块 quad 的**贴图名** —— 自检查「用对了图没有」。
        /// <para>🔴 **2026-10-07（A77 ⑭）：补齐第三步 `FindDeep`**（同 `UiQuadActive` / `UiNodeRect`）——
        /// 容器下的件（如搜索框底 `flt_input`，**不在 `_named`**）以前在这里**静默答 `null`**。
        /// ⚠️「一棵树取第一块」的语义**没有变**：定位那一步仍是 `Lookup` → 直接子件 → 深查找（直接子件优先），
        /// 取 quad 仍是 `GetComponentInChildren&lt;ImageQuad>(true)`（九宫格那 9 块里先撞上的那块）。
        /// ⚠️ 三步都找不到 ⇒ 照旧答 `null`（既有契约）。判据 → `资料/待办判据_审查发现_1005.md` §⑭。</para></summary>
        public string UiTextureName(string key)
        {
            var q = Lookup(key);
            if (q == null && Root != null)
            {
                var go = Root.Find(key);
                if (go == null)
                {
                    var deep = FindDeep(Root, key);
                    if (deep != null) go = deep.transform;
                }
                if (go != null) q = go.GetComponentInChildren<ImageQuad>(true);
            }
            return q != null && q.Texture != null ? q.Texture.name : null;
        }

        /// <summary>一个具名节点里 quad 的**块数**（九宫格 = 9 ⇒ 用它判「是九宫格还是拉满」）。
        /// <para>🔴 **2026-10-07（A77 ⑭）：补齐第三步 `FindDeep`** —— 容器下的件（`flt_input` 那类）
        /// 以前在这里**静默答 `0`**（`Root.Find` 找不到、又不在 `_named` 里 ⇒ 落到最后那行 `0`）。
        /// ⚠️ **本条的两步优先序与 `UiQuadActive`/`UiNodeRect` 不同，是故意的**：
        /// 树那一支答的是「这棵子树里有几块」（九宫格 9 / 平铺 N），而 `_named` 那一支答的是**硬编码 1**
        /// —— 两支答的不是同一个问题，把 `Lookup` 提到最前会把「树里的块数」**换成 1**。
        /// 既有断言（`name_bg >= 9` · `hdr_sep == 3` · `tab_hi0 == 9` · `row_*`）全是在**树优先**下量的。
        /// ⇒ 深查找**插在 `Root.Find` 后面**：树里找得到的 key 一个数都不变，
        /// 只把「树里找不到、`_named` 也没有」的那些从 `0` 变成**真块数**。
        /// ⚠️ 三步都找不到 ⇒ 照旧答 `0`（既有契约）。判据 → `资料/待办判据_审查发现_1005.md` §⑭。</para></summary>
        public int UiQuadCount(string key)
        {
            if (Root != null)
            {
                var go = Root.Find(key);
                if (go == null)
                {
                    var deep = FindDeep(Root, key);
                    if (deep != null) go = deep.transform;
                }
                if (go != null) return go.GetComponentsInChildren<ImageQuad>(true).Length;
            }
            return Lookup(key) != null ? 1 : 0;
        }

        /// <summary>一个具名节点里第一块 quad 的**渲染队列**（自检比层序用）。
        /// <para>🔴 **2026-10-07（A77 ⑭）：补齐第三步 `FindDeep`**（同 `UiQuadActive` / `UiNodeRect`）——
        /// 容器下的件（如搜索框底 `flt_input`，**不在 `_named`**）以前在这里**静默答 `−1`**
        /// ⇒ 「层序比大小」那种断言会把「找不到」当成「队列最小」。
        /// ⚠️ 优先序没动（`Lookup` → 直接子件 → 深查找）⇒ 既有 key 的读数一个数都不变。
        /// ⚠️ 三步都找不到 ⇒ 照旧答 `−1`（既有契约）。判据 → `资料/待办判据_审查发现_1005.md` §⑭。</para></summary>
        public int UiQueueOf(string key)
        {
            var q = Lookup(key);
            if (q == null && Root != null)
            {
                var go = Root.Find(key);
                if (go == null)
                {
                    var deep = FindDeep(Root, key);
                    if (deep != null) go = deep.transform;
                }
                if (go != null) q = go.GetComponentInChildren<ImageQuad>(true);
            }
            return q != null ? q.RenderQueue : -1;
        }

        /// <summary>🔴 用**合成坐标**走一遍鼠标那条路（批处理没有真鼠标）。
        /// `Ui*()` 那组只驱动状态、**验不到命中矩形**；这条专门验「点在哪儿、命中谁」。</summary>
        public bool UiClickPx(float x, float y)
        {
            var px = new Vector2(x, y);
            if (ModalPopupOpen) return false;        // 🆕 A364：模态消息窗开着 ⇒ 与 `HandlePointer` 同一条闸
            if (FltStripOn && px.x < FltX + FltW) return HandleFilterClick(px);
            if (CosmoFltStripOn && px.x < FltX + FltW) return HandleCosmoFltClick(px);
            return HandleButtons(px);
        }

        /// <summary>🆕 **2026-10-13（A1702 ③）**：自检入口 —— **卡池那一条命中路**（`HandlePoolClick`）
        /// 吃不吃画布 px 这一点。形状同 <see cref="UiClickCosmetic"/> 那条先例（直调那个 `Handle*`）。
        ///
        /// <para>🔴 **为什么非有它不可**：`UiClickPx` 那条自检口**到不了卡池** —— 它过了模态 /
        /// 两条抽屉之后**直接 `return HandleButtons(px)`**，而真实指针链是
        /// `HandlePoolClick → HandleDeckRowClick → HandleCosmeticClick → HandleButtons`
        /// （那前三站在 `UiClickPx` 里**都没有**）⇒ 「滚出视口的卡点不点得到」这一条**只能**这样验
        /// （批处理没有真鼠标 ⇒ `Mouse.current == null` ⇒ `HandlePointer` 整条不跑）。</para>
        ///
        /// <param name="right">`true` = 走原版那条「右键加牌」（**有副作用：会改卡组里的牌**）；
        /// 自检量命中**一律传 `false`**（左键那一支只登记「按下了」，等抬起才弹窗）。</param>
        /// <returns>有人吃了这一下吗。</returns>
        /// <remarks>⚠️ **不留副作用**：左键那一支会写 `_cardArmDef` / `_armPx`（等抬起），返回前清掉
        /// —— 否则后面几段自检会带着一个「按着没放」的状态（同 `UiCardDragBeginAt` 开头那三句的处置）。
        /// ⚠️ 它**不加** `HandlePointer` 里那几道闸（模态 / 别的窗 / 抽屉）：本入口问的就是
        /// 「卡池这一站自己认不认这一点」；闸那半由 `UiWindowShield` / `UiClickPx` 各有一条在验。</remarks>
        public bool UiPoolHit(float px, float py, bool right = false)
        {
            bool ate = HandlePoolClick(Pos(px, py), new Vector2(px, py), right);
            _cosmArmName = null; _cardArmDef = null;
            return ate;
        }

        /// <summary>🆕 **2026-10-17（`A1495` 第二半）：自检入口 —— 走【完整】那条点击派发链**
        /// （<see cref="HandlePointer"/> 的下半段，⛔ 不是单站）。
        ///
        /// <para>🔴 **为什么还要第三口**（已有 `UiClickPx` 与 `UiPoolHit`）：
        /// · `UiClickPx` **跳过了前三站**（它过了模态/两条抽屉就直接 `HandleButtons`）；
        /// · `UiPoolHit` **只走第一站**（直调 `HandlePoolClick`）。
        /// ⇒ 「**卡池 → 卡组行 → 卡背 → 钮**」这一整条**按顺序**的派发（= 真实鼠标那条）
        /// **一个自检口都没有**。`A1495` 那道 NRE 恰恰出在**第一站返回 false 之后**、
        /// 继续往下走的那条路上 ⇒ 只直调第一站**永远抓不到**它（这就是「16k+ 断言全绿、
        /// 真机一进去就炸」的结构原因）。</para>
        ///
        /// <para>**逐句照 `HandlePointer`**（闸 → 卡池 → 卡组行 → 卡背 → 钮），左键那一支；
        /// ⛔ **不模拟抬起**：本口只回答「这一点**有人吃**吗」（抬起之后的弹窗/换牌不在这里验）。</para>
        ///
        /// <remarks>⚠️ **不留副作用**：`HandlePoolClick` / `HandleCosmeticClick` 的左键支会写
        /// `_cardArmDef` / `_cosmArmName`（等抬起）⇒ 返回前清掉，同 <see cref="UiPoolHit"/> 那两句。
        /// ⚠️ **卡组行那一站例外**：`HandleDeckRowClick(…, left: true)` 会 `StartRowDrag`（真实语义如此），
        /// 本口**不还原**（`EndDrag` 是私有的，没有「撤销起拖」的公开口）⇒ 自检别拿卡组行当探针点。</remarks>
        public bool UiClickPoolPx(float x, float y)
        {
            var px = new Vector2(x, y);
            var wp = Pos(x, y);
            // ---- 与 `HandlePointer` 逐句同序（左键：`downL = true`、`downR = false`）----
            if (ModalPopupOpen) return false;
            if (OtherWindowUp() != null) return false;
            if (FltStripOn && px.x < FltX + FltW) return HandleFilterClick(px);
            if (CosmoFltStripOn && px.x < FltX + FltW) return HandleCosmoFltClick(px);
            bool ate = false;
            if (HandlePoolClick(wp, px, false)) ate = true;
            else if (HandleDeckRowClick(wp, px, true)) ate = true;
            else if (HandleCosmeticClick(px, false)) ate = true;
            else ate = HandleButtons(px);
            _cosmArmName = null; _cardArmDef = null;
            return ate;
        }

        // ---- 自检读数：卡池槽与它那颗视图（`A1495` 第二半的不变式用）----
        /// <summary>卡池槽数（`_poolIndex` 与 `_poolViews` 长度相等 —— 两者在 `RefreshPool` 里同步 `Add`）。</summary>
        public int UiPoolSlotCount { get { return _poolIndex.Count; } }

        /// <summary>第 `i` 槽绑的是卡表第几张。<c>&lt; 0</c> = **空槽**；越界 = `-2`（自检别把它当空槽）。</summary>
        public int UiPoolSlotCardIndex(int i)
        {
            return (i >= 0 && i < _poolIndex.Count) ? _poolIndex[i] : -2;
        }

        /// <summary>第 `i` 槽那颗 `CardView` 的激活态：`null` = **根本没建**（A1495 那条 NRE 就出在这种槽上）；
        /// `true`/`false` = 建了、激活 / 没激活。越界同样 `null`。</summary>
        public bool? UiPoolSlotViewActive(int i)
        {
            if (i < 0 || i >= _poolViews.Count) return null;
            var v = _poolViews[i];
            if (v == null) return null;
            return v.gameObject.activeSelf;
        }

        /// <summary>🆕 A41 ④：这一点上「按 `ClickOrder` 第一个吃到的 key」，`null` = 没有一件命中。
        /// 与点击/悬停**同一条**（都过 `HitBtn` → `KeyLive`：看不见的钮不算）—— 自检直接问它，
        /// 免得靠副作用反推「到底谁被点到了」。</summary>
        public string UiTopKeyAt(float x, float y) { return TopKeyAt(new Vector2(x, y)); }

        // 🔴 **2026-10-17（T23 删件）**：自检读数 `ImportOpen` / `ImportText` / `ImportError`、
        //   `ImportOutcome` 枚举与其 `LastImportOutcome`、以及四个自检口
        //   `UiOpenImport` / `UiSetImportText` / `UiTryImport` / `UiCloseImport` **全部删掉了** ——
        //   它们服务的导入弹窗那一棵树整棵删了（为什么删见 `BuildImportPopup` 那一节）。
        //   ⛔ 别再按旧断言名去找它们；导入那条生产链在外壳（`Shell/CollectionData.ImportDeck`）。
        public string UiShareString() { return DeckLibrary.ExportString(State.Deck); }
        /// <summary>自检入口：**直调页脚 `Done`** = `HandleButtons()` 里 `case "foot_done"` 那一支
        /// （<see cref="SaveAndSay"/>，与 `UiNameClear()` ↔ `case "name_clear"` 同形；同族先例
        /// `Shell/CampaignTab.ClaimForTest` —— **直调、⛔ 不经那颗钮的命中**）。
        /// 🔴 **2026-10-15（A535）为什么非有它不可**：`SaveAndSay()` **成功支**那句
        /// <see cref="HideDeckPopUp"/>（原版 `DeckEditingWindow__TrySaveDeck.c:103`）**今天没有生产可达路径** ——
        /// `Done` 钮被 `HandlePointer` 里 `if (ModalPopupOpen) return;` 那道闸挡着、ESC 又被 `EscPressed()` ② 那一级
        /// 吃掉（原版那一刻同样轮不到 `DeckEditingWindow.ESCPressed`）⇒ **窗开着时到不了成功支**，
        /// 把那句删掉也**不会红**（= 零覆盖）。
        /// ⚠️ 本口**只绕开「命中 + 那两道模态闸」**，⛔ **不绕开 `SaveAndSay()` 本身**（它调的就是
        /// `case "foot_done"` 调的那个函数；哪天那一支换了函数，本口要跟着换）。
        /// ⚠️ 因此它**故意不做**模态拦截（那正是用途）—— 与 `UiClickPx` / `HandlePointer` 相反。
        /// 夹具：`Editor/DeckScene.cs` 的 A364 **⑦·b**（同一入口两种状态：不合法 ⇒ 窗留着 / 合法 ⇒ 窗收掉）。</summary>
        public void UiPressDone() { SaveAndSay(); }
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
                int rows = Mathf.Max(1, Mathf.CeilToInt(CosmeticList().Length / (float)CosmoCols));
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

        // ---- 自检入口：拖拽（2026-10-17）----
        // ⚠️ 与鼠标那条路**共用同一批函数**（`BeginCosmeticDragAt` / `OnDrag` / `EndCosmeticDragAt`）——
        //    批处理没有真鼠标（`Mouse.current == null` ⇒ `HandlePointer` 整条不跑），这是唯一能验到的路。
        /// <summary>原版 `SupportMethods.IsScrollDragThreshold`（自检直接用**原版那个函数**，
        /// 不在自检里另写一遍判据）。</summary>
        public bool UiIsScrollDragThreshold(float dx, float dy) { return IsScrollDragThreshold(new Vector2(dx, dy)); }

        /// <summary>第 `idx` 张卡背那一格的**中心**（原版 px）—— 用与铺格同一条式子反算，
        /// 自检别自己拍坐标。</summary>
        public bool UiCosmeticCellCenter(int idx, out float cx, out float cy)
        {
            cx = cy = 0f;
            int n = CosmeticList().Length;
            if (idx < 0 || idx >= n) return false;
            int r = idx / CosmoCols, c = idx % CosmoCols;
            float firstRow = Mathf.FloorToInt(_cosmScroll / CosmoCellH);      // 与 `RefreshCosmetics` 同一条
            if (r < firstRow || r >= firstRow + CosmoRows) return false;      // 不在这一屏里
            float off = _cosmScroll - firstRow * CosmoCellH;
            cx = CosmoX + CosmoPadX + c * CosmoCellW + CosmoCellW * 0.5f;
            cy = CosmoY + (r - firstRow) * CosmoCellH - off + CosmoCellH * 0.5f;
            return true;
        }

        /// <summary>🔴 **合成坐标走一遍卡背拖拽那条路**（按下 → 移动 → 松手），三步都进**鼠标那条路同一个函数**。
        /// 返回「松手时真的投出去一次了吗」。`fromPx` = 按下的那一点（卡背格中心）· `toPx` = 松手那一点。⚠️ 想验中间那两拍（预览显没显、跟没跟手）用下面那三个拆开的入口。</summary>
        public bool UiDragCosmetic(float fromX, float fromY, float toX, float toY)
        {
            if (!UiCosmeticDragBegin(fromX, fromY, toX, toY)) return false;
            UiCosmeticDragMove(toX, toY);
            return UiCosmeticDragEnd(toX, toY);
        }

        /// <summary>拆开的第一拍：按下那一点，并按「这一点 → `toPx`」的位移判起不起拖。
        /// `false` = 没起拖（位移被判成滚动 / 那一点上没有卡背）。</summary>
        public bool UiCosmeticDragBegin(float fromX, float fromY, float toX, float toY)
        {
            _cosmArmName = null; _cardArmDef = null; _dragging = false;
            var from = new Vector2(fromX, fromY);
            _cosmArmName = CosmeticNameAt(from);
            if (_cosmArmName == null) return false;
            bool started = BeginCosmeticDragAt(from, new Vector2(toX, toY) - from);
            if (!started) _cosmArmName = null;          // 没起拖 ⇒ 那份「按下了」的登记要清（同鼠标那条路）
            return started;
        }

        /// <summary>第二拍：指针挪到 `toPx`（原版 `DraggableController.OnDrag` —— 预览贴上去）。</summary>
        public void UiCosmeticDragMove(float toX, float toY)
        {
            if (_cosmDrag == null || !_cosmDrag.Dragging) return;
            _cosmDrag.OnDrag(DragEvent(new Vector2(toX, toY), Vector2.zero, false));
        }

        /// <summary>第三拍：在 `toPx` 松手（原版 `OnEndDrag` —— 关预览 + 投给指针下的 `IDropHandler&lt;T&gt;`）。</summary>
        public bool UiCosmeticDragEnd(float toX, float toY)
        {
            return EndCosmeticDragAt(new Vector2(toX, toY), Vector2.zero);
        }

        /// <summary>预览那一棵的根（自检拿它量渲染出来的矩形 —— `DeckScene.UnionQuadsPx`）。</summary>
        public GameObject UiCosmeticPreviewGo { get { return _cosmPreviewGo; } }
        /// <summary>拖影卡行那一棵的根（同上）。</summary>
        public GameObject UiCardGhostGo { get { return _cardGhostGo; } }

        /// <summary>预览件现在显示着没有（原版 `Collection Cosmetic.m_IsActive`；起拖那一拍才 true）。</summary>
        public bool UiCosmeticPreviewActive { get { return _cosmPreviewGo != null && _cosmPreviewGo.activeSelf; } }

        /// <summary>预览件中心在**设计 px** 的哪里（拿来验「跟着指针走」）。取不到返回 false。</summary>
        public bool UiCosmeticPreviewCenter(out float cx, out float cy)
        {
            cx = cy = 0f;
            if (_cosmPreview == null) return false;
            var p = ToPx(_cosmPreview.transform.position);
            cx = p.x; cy = p.y;
            return true;
        }

        /// <summary>预览件画出来的**宽高（设计 px）**—— 🔴 **不是 132×198**：
        /// 132×198 是**外接框**（`Image_…` 的 `sizeDelta` **220×330** × `m_LocalScale 0.6`）；
        /// 真画出来的是**按贴图自己的比例内接进那个框**之后的两维。
        /// <para>🔴 **2026-10-18（A944）更正**：原来这里写「布局框 250×405 × 0.6 = **150×243**」—— **错了**：
        /// 250×405 是**外框 `Collection Cosmetic`**（容器），那颗 `Image` 自己只有 220×330
        /// （判据全文 → `BuildCosmeticDrag` 上头那一段）。</para>
        /// <para>🔴 **2026-10-18（`A994③`）再更正**：`A944` 那一轮把上面的 132×198 当成了「画出来的矩形」，
        /// 其实它只是**外接框** —— 原版那颗 `Image` 还带 **`m_PreserveAspect = 1`**
        /// ⇒ 实绘 = **按贴图比例内接进 132×198**（半高定/宽定由贴图比例说了算；实现 →
        /// `Core/DraggableController.cs` 的 `CosmeticPreview.Initialize` / `PreserveAspectSize`）。
        /// 例：`Cardback_AM_Shield of Humanity` 707×981 ⇒ **132×183.16**。
        /// ⚠️ 本口量的是**当前这一拍**的实绘值（起拖前预览是关着的，量到的还是出厂占位那一份）。</para>
        /// ⚠️ 量的是 `ImageQuad` 自己那两格（`WorldW` / `WorldH`），不是 `localScale`（本工程没有继承缩放）。</summary>
        public bool UiCosmeticPreviewSize(out float w, out float h)
        {
            w = h = 0f;
            var q = _cosmPreviewGo != null ? _cosmPreviewGo.GetComponentInChildren<ImageQuad>(true) : null;
            if (q == null) return false;
            w = q.WorldW * PxPerUnit; h = q.WorldH * PxPerUnit;
            return true;
        }

        /// <summary>原版那两个节点/子件的**原版矩形**（判据写死在 `DeckRuntime` 那一段的注释里）——
        /// 自检拿它与 prefab 实读的数对账。`[0]` = 节点 100×100 左上、`[1]` = 预览布局框左上。</summary>
        public Vector4 DragNodeRectPx { get { return new Vector4(DragNodeX, DragNodeY, DragNodeW, DragNodeH); } }
        public Vector4 PreviewLayoutRectPx { get { return new Vector4(PrevX1, PrevY1, PrevX2 - PrevX1, PrevY2 - PrevY1); } }
        /// <summary>`Sidebar/Deck Details` 那一栏（= 原版挂着 `IDropHandler&lt;…&gt;` 的 `DeckEditingPanel` 那一件）。</summary>
        public Vector4 DropFieldRectPx { get { return new Vector4(DropFieldX, DropFieldY, DropFieldW, DropFieldH); } }
        /// <summary>起拖音 cue 名（原版那一格 `AudioCue` 的名字，反查出来的）。</summary>
        public string DragCueName { get { return DragCue; } }
        /// <summary>卡背那一支当前在不在拖（= 原版 `dragging` 那个私有布尔）。</summary>
        public bool UiCosmeticDragging { get { return _cosmDrag != null && _cosmDrag.Dragging; } }
        /// <summary>🔴 **合成坐标走一遍卡牌拖拽那条路**（按下 → 移动 → 松手）。返回「松手时投出去一次了吗」。
        /// `view` = 卡池第几屏位（起点坐标由 `UiPoolCellRect` 反算 —— 与 `HandlePoolClick` 命中同一张卡）。
        /// ⚠️ 想验中间那两拍（拖影显没显、跟没跟手）用下面那三个拆开的入口。</summary>
        public bool UiDragCard(int view, float toX, float toY)
        {
            float sx, sy, sw, sh;
            if (!UiPoolCellRect(view, out sx, out sy, out sw, out sh)) return false;
            if (!UiCardDragBeginAt(view, toX - sx, toY - sy)) return false;
            UiCardDragMove(toX, toY);
            return UiCardDragEnd(toX, toY);
        }

        /// <summary>卡牌那一支的第一拍：按下卡池第 `view` 屏位那一点，`dx,dy` = 这一次拖的位移
        /// （过不了 `IsScrollDragThreshold` 就不起拖 —— 与卡背那一支同一个函数、同一条闸）。</summary>
        public bool UiCardDragBeginAt(int view, float dx, float dy)
        {
            _cosmArmName = null; _cardArmDef = null; _dragging = false;
            var def = UiPoolCardAt(view);
            if (def == null) return false;
            float sx, sy, sw, sh;
            if (!UiPoolCellRect(view, out sx, out sy, out sw, out sh)) return false;
            return BeginCardDragAt(new Vector2(sx, sy), new Vector2(dx, dy), def);
        }
        /// <summary>第二拍：指针挪到 `toPx`（原版 `OnDrag` —— 拖影贴上去）。</summary>
        public void UiCardDragMove(float toX, float toY)
        {
            if (_cardDrag == null || !_cardDrag.Dragging) return;
            _cardDrag.OnDrag(DragEvent(new Vector2(toX, toY), Vector2.zero, false));
        }
        /// <summary>第三拍：在 `toPx` 松手（原版 `OnEndDrag`）。</summary>
        public bool UiCardDragEnd(float toX, float toY)
        {
            return EndCardDragAt(new Vector2(toX, toY), Vector2.zero);
        }
        /// <summary>拖影卡行现在显示着没有（原版 `Deck Selector Card Info button.m_IsActive`）。</summary>
        public bool UiCardGhostActive { get { return _cardGhostGo != null && _cardGhostGo.activeSelf; } }
        // 🔴 **2026-10-17（D35 删件）**：这里原来还有 `UiInfoActionsVisible`（读 `Lookup("info_import")`）
        //   （它上面那句 `<summary>` 是「Deck info 页签那两颗动作钮显示出来了没有」—— 随它一起删）。
        //   —— 那两颗钮删了，这个读数**恒为 false** ⇒ 一起删（留着一个恒假的读数是误导）。
        //   自检要断「Deck info 的东西在 Cards 页不露」走 `UiInfoOnlyActive`（费用曲线那一批仍在）。
        // 🔴 **2026-10-17（T23 删件）**：这里原来还有 `UiModalVisible`（读 `Lookup("imp_bg")`）——
        //   导入弹窗整棵删了 ⇒ 它也一起删（同上面 `UiInfoActionsVisible` 那条「恒假的读数是误导」）。

        /// <summary>🔴 **「该藏的东西藏住了吗」的通用守卫**（图 + 文字都算）。
        /// 为什么单开一条：`Lookup()` 只认 `ImageQuad` ⇒ 只验图的话**文字会漏**，
        /// 而漏了文字的表现是「别的页签上飘着一行字」—— 2026-09-20 就是这么漏的。
        /// <para>⚠️ 三张表各自一个读数（`_infoOnly` / `_cosmOnly` / `_cardsInfoOnly`）—— **一个都不能少**：
        /// 挑错表写断言 = 「该藏的那批**一件都没数**」⇒ 断言**恒绿**。判据是**哪一批**进哪张表
        /// （单页 / 单页 / 两页），见各表的 doc。</para></summary>
        public int UiInfoOnlyActive { get { int n = 0; foreach (var g in _infoOnly) if (g != null && g.activeSelf) n++; return n; } }
        /// <summary>🆕 **2026-10-11（`A1518③`）**：**`_cardsInfoOnly` 那一批**的通用守卫
        /// （= 通配符计数条整条，Cards 页 + Deck info 页都该显示、Cosmetics 页该整条关）。
        ///
        /// <para>**为什么必须补**：原来的通用守卫**只有两张表**（`UiInfoOnlyActive` 数 `_infoOnly`、
        /// `UiCosmOnlyActive` 数 `_cosmOnly`），而 2026-10-11 新加的 `_cardsInfoOnly` **不在这两张里**
        /// ⇒ 拿 `UiInfoOnlyActive == 0` 去断「Cards 页 / Cosmetics 页零泄露」时，
        /// **通配符计数条那 10 件一件都没被数到** ⇒ 那一半**恒绿**（本仓最怕的那族假读数）。</para>
        ///
        /// <para>⚠️ 与它**成对**的那一条是**读画面**的（`Editor/DeckScene.cs` 的 `WcBarShownCount()`，
        /// 按节点名读 `activeInHierarchy`）—— 两条腿都要有：本条读**登记表**（「该登记的登记了吗」），
        /// 那条读**画面**（「真的关/开了吗」）。只留本条 = 自证（表对了、树没动照样绿）。</para></summary>
        public int UiCardsInfoOnlyActive
        {
            get { int n = 0; foreach (var g in _cardsInfoOnly) if (g != null && g.activeSelf) n++; return n; }
        }
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
        // 🔴 **2026-10-17（T23 删件）**：这里原来还有 `UiModalActive`（数 `_modalOnly` 那批）——
        //   `_modalOnly` 随导入弹窗一起删了，这个读数也一并删掉。
        /// <summary>正在编辑文本（0 = 没有 · 1 = 改名 · 2 = 卡名筛选）。</summary>
        public int UiEditKind { get { return _editKind; } }

        /// <summary>🔴 模拟「按住卡组第 <paramref name="viewRow"/> 行 → 拖到屏幕 x = <paramref name="toScreenX"/> → 松开」。
        /// **走的是鼠标那条路**（`StartRowDrag` → `EndDrag`），批处理没有真鼠标、这是唯一能验到
        /// 「拖出侧栏 = 删除」的办法（删牌只有这一条路，原版 `DeckEditingPanel__CheckCardSlotDrag.c:59,66`）。</summary>
        public bool UiDragDeckRow(int viewRow, float toScreenX)
        {
            var shown = DeckRows();
            int idx = Mathf.FloorToInt(_deckScroll / RowPitch) + viewRow;
            if (viewRow < 0 || viewRow >= RowVisible || idx >= shown.Count) return false;
            if (shown[idx] == null) return false;      // 🆕 空位占位行（没督军/没防御卡）不能拖
            // 🔴 **2026-10-21（`A1720`）**：读**现在开着的那一份**行底（槽 0 两套 —— 见 `RowBgGo`）。
            //   ⛔ 原来写死 `_deckRowBg[viewRow]`：槽 0 滚到卡行档时那一份关着 ⇒ 本口**静默答 false**
            //   （自检会把它读成「这一行不能拖」）。
            var rowGo = RowBgGo(viewRow);
            if (rowGo == null || !rowGo.activeSelf) return false;
            StartRowDrag(idx, viewRow, rowGo.transform.position);
            _draggingMoved = true;                 // 模拟「拖动过阈值」⇒ 松开时按拖出处理
            EndDrag(toScreenX);
            return true;
        }

        /// <summary>模拟「点一下卡组第 viewRow 行」（不动 ⇒ 松开时弹放大窗，照原版）。</summary>
        public bool UiClickDeckRow(int viewRow)
        {
            var shown = DeckRows();
            int idx = Mathf.FloorToInt(_deckScroll / RowPitch) + viewRow;
            if (viewRow < 0 || viewRow >= RowVisible || idx >= shown.Count) return false;
            if (shown[idx] == null) return false;      // 🆕 空位占位行（没督军/没防御卡）点不出详情
            var rowGo = RowBgGo(viewRow);          // 同上（`A1720`：槽 0 两套件）
            if (rowGo == null || !rowGo.activeSelf) return false;
            StartRowDrag(idx, viewRow, rowGo.transform.position);
            EndDrag(rowGo.transform.position.x);       // 没动过 ⇒ 走「点击」那一支
            return true;
        }
        /// <summary>放大窗现在开着没有（`UiClickDeckRow` / 池卡左键都会开它）。
        /// 🔴 **2026-10-12（`A1698`）换口**：原来读的是战斗版那扇自己的 `Visible`（`CardDisplayWindow`）；
        /// 现在那扇换成了菜单版 `Shell/CardDetailPopup`（= `GameWindow`）⇒ 改读它的 `CurrentState`
        /// （`Closed` = 没开；`Open` / `Background` 都算「在场」—— 同原版 `Hide()` 把它藏起来时也只是不在最上面）。
        /// ⛔ 不要改成 `gameObject.activeSelf`：`Hide()` 与 `Close()` 都会同时写 state 与激活态，但那是**实现细节**，
        /// 而 `CurrentState` 才是这扇窗**对外承诺的状态**。</summary>
        public bool UiCardWindowVisible { get { return _cardWindow != null && _cardWindow.CurrentState != WindowState.Closed; } }

        /// <summary>放大窗里现在展的是哪一张（卡面上印的那个名字，同 `CardText` 那一条口径）。
        /// 🔴 **2026-10-12（`A1698`）换口**：原来读的是 `CardDisplayWindow.ShownTitle`（那扇自己算好存起来的）。</summary>
        public string UiCardWindowTitle
        {
            get
            {
                var c = _cardWindow != null ? _cardWindow.Card : null;
                return c != null ? CardText.Name(c) : null;
            }
        }

        /// <summary>🔴 **2026-10-12（`A1698`）新开**：菜单版详情窗**右侧那一列**现在建出来了几件
        /// （数的是这三个节点：`Card Counter` / `Wildcard Count 0` / `AltArt Title`）。
        ///
        /// <para>**为什么靠它断「开的是哪一版」**：原版两处摆放共用**同一个脚本**，差别就在 `options`
        /// 这一棵子树——菜单版非 null、竞技场 13/13 全 null（我们这边 = `Battle/CardDisplayWindow.cs:16-22`）
        /// ⇒ **战斗版摆放那扇恒 0**。判据全文 → `资料/普查产出_第二十三会话/R4_详情窗右侧与异画.md` §三。
        /// ⚠️ **按节点名数、不看 `activeSelf`**：异画面板在「这张卡没有异画」时是**建了但关着**（原版 `AlternateArtPanel__Initialize.c:33`）。</para></summary>
        public int UiCardWindowRightColumn
        {
            get
            {
                if (_cardWindow == null) return 0;
                int n = 0;
                foreach (var t in _cardWindow.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Card Counter" || t.name == "Wildcard Count 0" || t.name == "AltArt Title") n++;
                return n;
            }
        }

        /// <summary>关掉放大窗（同玩家点压暗层 / 按 ESC）。
        /// 🔴 **2026-10-12（`A1698`）换口**：原来是 `_cardWindow.Hide()`（那扇不是 `GameWindow`、不在
        /// `WindowsManager.openWindows` 里）；现在这扇**是** `WindowsManager` 的窗 ⇒ 必须走 `Close()`。
        /// ⚠️ **别换回 `Hide()`**：`Hide()` **不摎表**（同 `HideAllWindows` 的语义）⇒ 它留在 `openWindows` 里，
        /// 后面任何一次 `ShowPreviousWindow`（关掉别的窗时发生）都会**把它又叫回来**（实测：
        /// `Editor/DeckScene.cs` 的 B25 那一节开/关一扇设置窗 ⇒ 已关的详情窗弹回来）。</summary>
        public void UiCloseCardWindow() { if (_cardWindow != null) _cardWindow.Close(); }

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
            int oldTab = _tab; _tab = Mathf.Clamp(t, 0, 2);   // B9_W51SHINE2：旧页给扫光用
            _deckScroll = 0f;
            // ⚠️ 顺序：先 `RefreshDeckList`（它会 `RefreshTabVisibility`），再 `RefreshPool`
            //    —— 卡池的显隐判据里有 `_tab`（见 `RefreshPool`），切回 Cards 页要让它重开。
            RefreshDeckList(); RefreshPool(); RefreshHeader(); WildcardShineAfterTab(oldTab);   // B9_W51SHINE2：原版 `WildcardDisplay.OnEnable` 那一拍（见方法 doc）
        }

        // ============================================================ 交互：滚轮
        //
        // 🔴 **2026-10-21（`A1702①`）：滚轮一格滚多少 px —— 按【各台 ScrollRect 自己的】`m_ScrollSensitivity`。**
        //
        // ① **输入量纲**：`Mouse.current.scroll.ReadValue().y` 在本机**一格 = ±1**（= legacy 量纲）。
        //    🔴 **2026-10-22 真机实测就地订正（铁律 5）**：原来这里写「新输入系统在 Windows 上是**原始
        //    `WHEEL_DELTA`** ⇒ **一格 = ±120**」—— **不成立**：`WF_SCROLL_PROBE=1` 起 player 滚一格，
        //    日志 `[SCROLLPROBE] raw=1.0000`。按 120 除 ⇒ 一格 = 0.0083 格 ⇒ 卡池（100 px/格）只剩
        //    **0.83 px/格** = **慢 120 倍**。同源口径 = `Shell/MenuScroll.cs.ScrollUnitsPerNotch` /
        //    `Battle/TouchInputManager.ScrollUnitsPerNotch`（同一天都改成 1）。
        //    ⛔ `工具/wf_click.py:112` 的 `WHEEL_DELTA = 120` 是 **Win32 `SendInput` 那一层的单位**
        //    （合成 OS 滚轮事件时是对的），与 `Mouse.current` 的读数**不是同一个量纲**，别拿它当本处的判据。
        //    ⇒ 本函数一律先把它除成**格数**（除 1 = 恒等），再乘各台的 `px / 格`。
        // ② **各台的 `px / 格` = 原版 prefab 上那台 `ScrollRect` 的 `m_ScrollSensitivity`**
        //    （uGUI `OnScroll` 就是 `position += scrollDelta × scrollSensitivity`，而
        //     `PointerEventData.scrollDelta` 在 Windows 上**一格 = 1**）：
        //    · **卡池** `Deck Scroll View`（`RecyclableScrollRect`，RT `-1327870198181403947` 那颗 MB）
        //      = **100**（`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-1327870198181403947.json` 实读）
        //    · **左侧卡组列表** `Scroll View`（RT `1449735243363708708`）= **50**
        //    · **筛选栏** `Content Area/Card Display/Card Filters/Scroll View` = **50**
        //    · **外观/卡背页** `Content Area/Cosmetic Display/Scroll View` = **100**
        //      （后两条判据 → `资料/普查产出_1017/UI/D_N29_滚动灵敏度逐视图.md:38-39`；
        //       重扫证据 → `资料/普查产出_1017/UI/W_N29_scroll_sensitivity.md` §四·7）
        //    🔴 **2026-10-19（第五十六会话 · `N46` 收尾）就地订正（铁律 5）**：本段原文写
        //      「⚠️ **筛选栏 / 卡背那两处【查不到判据】**：那两棵树里没有 `ScrollRect`（全包按
        //      `m_ScrollSensitivity` 枚举过，没有挂在 Filter/Cosmetic 名下的东西）⇒ 保留**我们原来的**
        //      48 px/格（= 旧 `0.4 × 120`）并出声，⛔ 不是「原版就是这样」」—— **已过期**：
        //      `W_N29`（2026-10-17）按父链全路径重扫 `bundle_menus_assets_all` 后**找到了那两台**
        //      （`Card Filters/Scroll View` = **50**、`Cosmetic Display/Scroll View` = **100**）
        //      ⇒ 代码已按原版值改成 50 / 100，这两处**不再是「我们挑的」**。
        //      ⚠️ 错因：当初是按 `m_ScrollSensitivity` 的**名字**筛的（那两棵子树里没有名为
        //      Filter/Cosmetic 的 ScrollRect 对象），而不是按**父链全路径**筛。
        //
        // ⚠️ 原来三处**共用一个** `step = dy * 0.4f`（= **48 px / 格**）⇒ 卡池比原版**慢 2.08 倍**、
        //    卡组列表比原版**慢 1.04 倍**（那条 48 与原版 50 只差 4%，所以一直没被注意到）。

        /// <summary>滚轮一格 = 多少**输入单位** = **1**（2026-10-22 真机实测：`Mouse.current.scroll` 一格 ±1，
        /// 见上面 ①；⛔ 别再写 120 —— 那会让滚动慢 120 倍）。</summary>
        const float ScrollUnitsPerNotch = 1f;
        /// <summary>卡池那台 `Deck Scroll View` 的 `m_ScrollSensitivity`（原版 100 ⇒ **100 px / 格**）。</summary>
        const float PoolScrollPxPerNotch = 100f;
        /// <summary>左侧卡组列表那台 `Scroll View` 的 `m_ScrollSensitivity`（原版 50 ⇒ **50 px / 格**）。</summary>
        const float DeckListScrollPxPerNotch = 50f;
        /// <summary>筛选栏 `Card Filters/Scroll View` 的 `m_ScrollSensitivity`（原版 50 ⇒ **50 px / 格**）。
        /// 判据 → `资料/普查产出_1017/UI/D_N29_滚动灵敏度逐视图.md:39`（重扫证据 → `W_N29_scroll_sensitivity.md` §四·7）。</summary>
        const float FilterScrollPxPerNotch = 50f;
        /// <summary>外观/卡背页 `Cosmetic Display/Scroll View` 的 `m_ScrollSensitivity`（原版 100 ⇒ **100 px / 格**）。
        /// 判据 → `D_N29_滚动灵敏度逐视图.md:38`。
        /// ⚠️ **与筛选栏是两台不同的 `ScrollRect`**（50 vs 100）⇒ ⛔ 别共用一个常量。</summary>
        const float CosmeticScrollPxPerNotch = 100f;

        void HandleScroll()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            float dy = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(dy) < 1f) return;
            Vector2 px = ToPx(LayoutSpace.ScreenToWorld(mouse.position.ReadValue(), _cam));
            float notches = dy / ScrollUnitsPerNotch;      // 🔴 一格 = 1（见上面 ①）

            if (FltStripOn && px.x < FltX + FltW)
            {
                // 可滚范围 = 内容高 − 可见高（内容高**随阵营数变**：Army 行高 = 它自己的内容高）
                float max = Mathf.Max(0f, FilterPanelModel.ContentHFor(State) - FltH);
                _fltScroll = Mathf.Clamp(_fltScroll - notches * FilterScrollPxPerNotch, 0f, max);
                RefreshFilters();
            }
            else if (CosmoFltStripOn && px.x < FltX + FltW)
            {
                // 🆕 2026-10-01：卡背抽屉**不滚**（内容 628 < 抽屉 924，原版那棵树里也没有 Scroll View）
                // —— 但滚轮要**吃掉**，别穿透到后面的卡背网格上。
            }
            else if (_tab == 2)
            {
                // 🔴 2026-10-19：外观/卡背页 = 原版 `Cosmetic Display/Scroll View` 的 **100 px/格**
                //   （⛔ 不是筛选栏那台 50；两台不同的 `ScrollRect`，见常量 doc）。
                _cosmScroll = Mathf.Max(0f, _cosmScroll - notches * CosmeticScrollPxPerNotch);
                RefreshCosmetics();
            }
            // 🔴 **2026-10-11（`A1516`）**：侧栏那一带原来**不含页判据**（只判 x 落在 335px 里）
            //   ⇒ 在 **Deck info 页**（和 Cosmetics 页）上把滚轮放在侧栏那一条上，滚的是**那个不显示的
            //   卡组列表**（原版那一刻 `deckList(0x28).SetActive(0)` ⇒ 那台 `ScrollRect` **根本不激活**）。
            //   后果**不可见**（滚的是藏起来的件、`_deckScroll` 又不影响别的）⇒ 惰性缺陷，但**与原版不符**。
            //   ⚠️ **不是「让给卡池」**：原版这一带在别的页上**一台激活的 `ScrollRect` 都没有**
            //   （卡池那台的视口从 **330.2** 起，而侧栏到 **335.6** ⇒ 中间 5.4px 的重叠带按「侧栏在上」算）
            //   ⇒ 滚轮**什么都不做**（穿过底下去滚卡池同样不是原版行为）。
            //   判据 = `DeckEditingPanel__Toggle{DeckList,Info,Cosmetics}.c` 那三句 `SetActive`
            //   （见 `DeckListPage` / `CardDisplayPage` 的 doc）。
            else if (px.x < SideBgX + SideBgW)
            {
                if (DeckListPage)
                {
                    _deckScroll = Mathf.Max(0f, _deckScroll - notches * DeckListScrollPxPerNotch);
                    RefreshDeckList();
                }
            }
            else
            {
                _poolScroll = Mathf.Max(0f, _poolScroll - notches * PoolScrollPxPerNotch);
                RefreshPool(); RefreshHeader();
            }
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
                    // 🔴 **2026-10-12（A365）改向：空名字【照原版接受】** —— 判据
                    //   `d:/2/tools/decomp_full/DeckEditingPanel__ChangeName.c:6`（无条件写 `deck.deckName`）
                    //   与 `:9`（写完标脏）。原来这里有一句 `else Say("名字不能是空的")`，**方向反了**。
                    //   空名怎么补 ⇒ **不在这里**：下面 `MarkDeckDirty()` 里的 `RefreshHeader()` 会跑
                    //   `State.Validate()`，卡组合法时它用**督军卡名**把空名补上
                    //   （`DeckUtility__ValidateDeck.c:70-76`）⇒ 走到 `Say` 时名字多半已经不是空的了。
                    bool changed = State.SetDeckName(buf);
                    MarkDeckDirty();                          // 🆕 A363：改名**只标脏**，落盘等 Done
                    //   ⚠️ **名字没变也标脏** —— 原版 `DeckEditingPanel__ChangeName.c:9` 那一句是无条件的
                    //   （写完就 `[+0x60] = 0`），照它做；只出声那句按「到底变没变」如实分两种。
                    if (!changed) Say("卡组名没变");
                    else Say(string.IsNullOrEmpty(State.Deck.Name)
                             ? "卡组名已清空（这副牌还不合法 ⇒ 名字先空着；合法时原版会用督军卡名补上）"
                             : "卡组名已改");
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
            // 🔴 **2026-10-17（T23 删件）**：这里原来还有 `kind == 3`（导入框）那一支 —— 导入弹窗删了。
        }

        void OnEnable() { if (Keyboard.current != null) Keyboard.current.onTextInput += OnText; }
        void OnDisable() { if (Keyboard.current != null) Keyboard.current.onTextInput -= OnText; }
        void OnText(char c) { if (_nameEdit != null && !char.IsControl(c)) _typed.Add(c); }

        // ============================================================ 交互：ESC（原版 `DeckEditingWindow.ESCPressed`）

        /// <summary>🆕 **2026-10-11（A223）**：**ESC = 保存**（判据 = `d:/2/tools/decomp_full/`
        /// `DeckEditingWindow__ESCPressed.c:5` —— 那一行就是 `DeckEditingWindow__TrySaveDeck(param_1, 0)`；
        /// ⛔ **不是关窗**：关窗走 `DeckEditingWindow__TryClose.c`（没改动直接关 / 有改动先问），
        /// 那条路只由**关闭钮**走）。
        ///
        /// <para>三级顺序 —— **每一级都有实读判据**：
        /// ① **文本编辑中** ⇒ 不抢：`HandleTyping` 那条**既有**的路会把 ESC 当「取消编辑」
        ///   （本函数第一句就 `return`，保证同一帧里 ESC 只被用掉一次）；
        /// ② 🆕 **2026-10-13（A502）模态消息窗（`PopUpGameWindow`）开着 ⇒ ESC 归它、本函数什么都不做** ——
        ///   判据是**那扇窗自己的 `closeOnEsc`**，⛔ **不是「有没有弹窗」**：
        ///   · 原版 ESC **只打给最上面那扇窗、没有第二跳**：`WindowsManager__Update.c` 在
        ///     `GetKeyDown(0x1b)` 之后取 `+0x58`（= `currentWindow`），过了 `IsOpen()`（虚表 `0x1f8`）
        ///     就调它的 `ESCPressed()`（虚表 `0x1e8`）——**那条路走完就 `return`**；
        ///     而 `WindowsManager__OpenWindowCO.c:50` 的 `set_CurrentWindow` 写在 if/else **之外**
        ///     ⇒ **弹窗一开，`currentWindow` 就是它**（旁证：`HidePopUp` 第一句比的就是这两个字段）
        ///     ⇒ 那一刻 `DeckEditingWindow.ESCPressed`（= 保存）**根本轮不到**；
        ///   · 那扇窗的 `closeOnESC` 实读 **0**（`Shell/PopUpGameWindow` 里那个 `closeOnESC`，两扇 prefab 同一个值）
        ///     ⇒ `GameWindow__ESCPressed.c` 第二道门槛不过 ⇒ **什么都不做**（我们这一跳就把它的
        ///     `ESCPressed()` 原样转出去，门槛与「出声」都在那一处，⛔ 不在这里再写一份）。
        ///   ⚠️ **别一刀切成「只要有弹窗就什么都不做」**：门槛是**那扇窗自己的 `closeOnEsc`**
        ///   （`= 0` ⇒ 什么都不做；`= 1` ⇒ 它自己关掉）—— 这一级只管 `closeOnEsc = 0` 的那扇。
        ///   🔴 **2026-10-17（T23 删件）**：这里原来还有一级「② 导入弹窗开着 ⇒ 只关它、不保存」
        ///   （`closeOnESC = 1` 的现成例子）。导入弹窗那棵树整棵删了 ⇒ 那一级也删；
        ///   「`closeOnESC = 1` 的窗 ESC 归它自己」这条行为**仍然由外壳那扇窗负责**
        ///   （`Shell/ImportDeckPopup.cs`，`closeOnEsc` 由 `GameWindow` 那套统一处理）。
        /// ③ 否则 ⇒ **保存**，而且走的是 **`Done` 钮同一个函数** `SaveAndSay()`
        ///   （两处写同一条规则 = 迟早不一致 ⇒ 不给 ESC 另写一份）。
        ///   🔴 **A330（2026-10-11）**：那道「不合法就不落盘」的闸**在 `SaveAndSay()` 里面**
        ///   （原版 `__TrySaveDeck.c:80`）⇒ 本函数**自动跟着有闸**，⛔ 别在这里再写一份校验。</para>
        ///
        /// <para>⚠️ **为什么挂在本类、不挂外壳那套**：卡组编辑窗**不是** `WindowsManager` 的窗 ——
        /// `DeckRuntime : MonoBehaviour`、自己一个场景（`DeckEditor.unity`、`windowsPlacement = 10` 是原版窗的值）
        /// ⇒ 外壳的 `GameWindow.ESCPressed` / `PointerLayer.KeyCancel` 那一条链**够不着它**
        /// （判据：本类没有 `GameWindow` 祖先；A223 原文记的就是这条前置不成立）。
        /// 🔴 **A502 那一级补上之后，两条链在本窗里终于同向了**：弹窗开着时 `PointerLayer.KeyCancel`
        /// 打的是 `TopWindow`（= 那扇弹窗、`closeOnEsc = 0` ⇒ 什么都不做），而本函数原来会一路打到
        /// `SaveAndSay()` —— 同一帧两条链的结论相反（缺陷）；现在两边都是「什么都不做」。</para>
        ///
        /// <para>改坏法（对应 `Editor/DeckScene.cs` 那几条）：删掉 `Update()` 里那句 `HandleEscape()`
        /// ⇒ 「ESC 之后卡组真的提交回库」红；让 ESC 顺手把编辑态也清掉 ⇒
        /// 「编辑中 ESC 归输入框」红；🔴 **删掉 A502 那一级**（或把它写成只看 `ModalPopupOpen` 之外的条件）
        /// ⇒ 「丢改动窗开着时 ESC 不落盘」那条红（判据见 A502 那一节）。</para></summary>
        public void EscPressed()
        {
            if (_nameEdit != null) return;                        // ① 输入框优先（`HandleTyping` 会取消编辑）
            if (ModalPopupOpen)                                    // ② 模态消息窗（`closeOnESC = 0`）
            {
                // 判据 = **那扇窗自己**的 `closeOnEsc`（`= 0` ⇒ 什么都不做并出声；`= 1` ⇒ 它自己关掉），
                // 与 `GameWindow.ESCPressed` 那两道门槛**同一份实现**（⛔ 别在这里再判一次）。
                // 两支都**不再落到保存**：原版那一刻 ESC 已经被最上面那扇窗吃掉了（见方法头 ②）。
                _popup.ESCPressed();
                return;
            }
            SaveAndSay();                                          // ③ = Done = 我们的 `TrySaveDeck`
        }

        /// <summary>每帧问一次 ESC 键（**批处理没有键** ⇒ 自检直接调 `EscPressed()`，同一条路）。</summary>
        void HandleEscape()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            // 🆕 **2026-10-17（B25①）**：ESC 归**最上面那扇窗**（`PointerLayer.KeyCancel` 已经先吃过一次 ——
            //   它的 `Update` 排在本类之前）⇒ 本类**不许**再存盘离场（原版那一刻轮不到 `DeckEditingWindow`）。
            //   ⚠️ `_winShieldPrev` 就是为这一句记的：这一帧窗**已经被关掉了**，只问「此刻开着吗」会得到 false。
            if (UiWindowShield) return;
            // 🆕 **2026-10-13（`A1132` 第 2 步的必然配套）**：**本窗自己那一扇**宿主窗也不算例外 ——
            //   `PointerLayer.KeyCancel` 刚把这一下 ESC 打给了它，而 `Shell/DeckEditingWindow.ESCPressed`
            //   **就是把这一下转回 `this.EscPressed()`** 的 ⇒ 再跑一遍 = 同一下 ESC 被处理两遍。
            //   🔴 **不加这一句的坏法**（真缺陷、现有断言抓不到）：同一下 ESC 被处理两遍 ——
            //     第一遍由宿主窗转进来（保存 / 关最上面那扇窗），第二遍再落到 `SaveAndSay()`
            //     = 存两遍盘、甚至退两次编辑器。（历史上判别式是「关导入弹窗那一级」，
            //     那一级 2026-10-17 已随 T23 删件删掉；闸本身照旧必须有。）
            //   两个谓词方向相反（指针放行 / ESC 拦下）⇒ ⛔ 别把它们合成一个（见 `HostWindowEatsEsc` 的 doc）。
            if (UiEscEatenByHost) return;
            EscPressed();
        }

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
        // ⚠️ 原版这七行在一个 `Scroll View` 里、**全量建**（判据 → `RefreshFilterCells` 那段）；
        //    本窗 13 个阵营 ⇒ Army 行 550 高 ⇒ 内容总高 **1389.02** > 可见 924.1 ⇒ 可滚 **464.92**。
        //    越界那些格子**照建**、由 `ClipCellToBand` 截到带口内
        //    （= 原版 `Viewport` 上那颗 `Image,Mask`；机制选择与判据 → `FltBand` 那段）。
        //    🔴 **2026-10-10（A224②）订正**：这里原来写「它那个遮罩（`UIMask`）我们**没建** ——
        //    内容只溢出屏幕底 65px，看不见也不影响点击」—— **三处都不成立**：① 内容高不是 989.02
        //    而是 **1389.02**（那是 Army 行按内容高 550 算之前的老数）；② 滚动上界不是 64.92 而是 464.92；
        //    ③ 滚起来之后**真的会压进 Header**（`FltY = 156` 是面板顶、Header 就在它上面），
        //    不是「只溢出屏幕底」。⇒ 现在按原版把裁切补上了。
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

        /// <summary>🆕 **2026-10-18（A889）**：**卡背抽屉**那一族的面板内坐标 → 屏幕绝对坐标。
        /// 与 <see cref="FltAbs"/> **只差减哪个滚动量**（这里是 <see cref="_cosmoFltScroll"/>，今天恒 0）。
        /// <para>🔴 **为什么单开一个（而不是在三个调用点上直接「不减」）**：账原文 —— 卡背那三处
        /// （quad / 命中区 / `Label`）原来走 `FltAbs`，而 `FltAbs` 恒减 **`_fltScroll`（卡牌抽屉的）**，
        /// 卡背抽屉**自己没有滚动区**（`HandleScroll` 的卡背那一支只把滚轮吃掉）⇒ 只要 `_fltScroll ≠ 0`
        /// （在 Cards 页滚过卡牌抽屉再切到 Cosmetics 页就会 —— `SetTab` 不重置它），
        /// 卡背那两行就会被**别人的滚动量**整体推走。
        /// 直接写「不减」= 把「这一族有没有滚动量」这件事**散在三个点上**（将来真加滚动要改三处，
        /// 还得靠人记得；`UiCosmoFilterCell` 那个只读口也会漏一处）⇒ **两族各一个函数、各一个量**，
        /// 结构上串不起来。**形状照收藏窗那一侧**（`Shell/CollectionWindow.cs` 的
        /// `cosmo ? Abs(c.R) : _fltScroll.Shift(Abs(c.R))` —— 那边**本来就不借**）。</para>
        /// <para>⛔ 别把 `FltAbs` 改成「可传滚动量」的两用版：`FltAbs` 的 7 个调用点（搜索框 3 · 格子 3 ·
        /// 小标题 1）全是卡牌那一族，多一个可选参数只会多一次传错的机会。</para></summary>
        PxRect FltAbsCosmo(float x1, float y1, float x2, float y2)
        {
            return new PxRect(FltX + x1, FltY + y1 - _cosmoFltScroll, FltX + x2, FltY + y2 - _cosmoFltScroll);
        }

        /// <summary>🆕 **2026-10-10（A224②）：抽屉的裁切边界**（画布 px · 左上原点）= 面板自己那个矩形。
        /// <para>**判据（原版第一层）**：`bundle_menus_assets_all` 的 `Deck Editing Menu`
        /// → `Card Display > Card Filters > Scroll View > Viewport` 身上是 **`Image,Mask`**
        /// （**模板 / stencil 裁切**）· `showGraphic = 0` ⇒ 那层 mask 底图**不画**；
        /// **不是** `RectMask2D`（同包 `Cosmetic Display > Scroll View` 才是另一种 —— 抄判据前先看是哪一种）。
        /// 视口与面板同格（dump 实测：`Filters` 模板高 839.02、视口 **924.06** = 面板 924.1）。</para>
        /// <para>⇒ **我们要的是视觉结果一致**（越界的格子被裁掉、且不画底板）：我们这套是**自建 mesh**
        /// （没有 stencil 管线、`ImageQuad` 也不是 `MaskableGraphic`），所以用工程现成的裁切机制落地
        /// —— `MenuDraw.ClipRect`（截矩形 + 截 uv）+ `MenuDraw.ClipText`（截文字网格），
        /// 与 `Shell/*` 那些滚动区（`MenuScroll.Viewport` → `MenuWindowBase.Clip`）是**同一套**。</para>
        /// 🔴 **它不随滚动变**：滚动只改格子的绝对 y（`FltAbs` 减 `_fltScroll`），带口恒 = 面板矩形。</summary>
        PxRect FltBand { get { return new PxRect(FltX, FltY, FltX + FltW, FltY + FltH); } }

        /// <summary>🆕 **2026-10-10（A224②）**：把抽屉里的一格图示**截到带口内**
        /// （等效原版 `Viewport` 上那颗 `Image,Mask`；`FltBand` 那段记了「为什么是这一套机制」）。
        /// <para>**整块在带外 ⇒ 不删节点、只 `SetActive(false)`** —— 照 `MenuDraw.ClipNineChildren` 的口径
        /// （「树形/子块个数与未裁切时一致，自检按名字找得到」）。A224① 要的「格子数不随滚动变」
        /// 正是这一条：**全量建**是判据本身，裁切只是「不画」。⛔ 别拿「整块在外就不建」顶替。</para>
        /// <para>**裁法**（挪 + 缩 + 截 uv）与 `MenuDraw.ClipNineChildren` **同一条算式**：
        /// 截 uv 是必须的（只截几何不截 uv 会把图**压扁** —— 同 `ImageQuad.SetUvRect` 的注释）；
        /// ⚠️ 纵轴要翻一次（uv 的 y **自下而上**、`PxRect` **自上而下**）。
        /// 🔴 **射线那一半不在这个函数里**：原版那颗 `Mask` 自己也是 `ICanvasRaycastFilter`
        /// （`Mask.IsRaycastLocationValid`，本地 uGUI `…/UI/Core/Mask.cs`）⇒ **带口外的点判不中任何格**
        /// ⇒ 点击区在登记时单独裁（见 `RefreshFilterCells` 里 `_fltHit.Add` 那一段）。</para></summary>
        static void ClipCellToBand(ImageQuad q, PxRect band)
        {
            if (q == null) return;
            var c = PxOfWorld(q.transform.position);
            float hw = q.WorldW * PxPerUnit * 0.5f, hh = q.WorldH * PxPerUnit * 0.5f;
            var qr = new PxRect(c.x - hw, c.y - hh, c.x + hw, c.y + hh);
            PxRect cr;
            if (!MenuDraw.ClipRect(qr, band, out cr)) { q.gameObject.SetActive(false); return; }  // 整块在带外
            if (MenuDraw.SameRect(cr, qr)) return;                                                 // 整块在带内 ⇒ 一个字不动
            var uv = q.UvRect;
            float w = Mathf.Max(1e-6f, qr.W), h = Mathf.Max(1e-6f, qr.H);
            var lp = MenuDraw.Local(q.transform.parent, cr.x1, cr.y1, cr.x2, cr.y2);
            lp.z = q.transform.localPosition.z;                    // z 不动（同队列里还靠它排序）
            q.transform.localPosition = lp;
            q.SetWorldHeight(LayoutSpace.Px(cr.H));
            q.SetAspect(cr.W / Mathf.Max(1e-6f, cr.H));
            q.SetUvRect(new Rect(uv.x + uv.width * (cr.x1 - qr.x1) / w,
                                 uv.y + uv.height * (qr.y2 - cr.y2) / h,   // uv 的 y 自下而上 ⇒ 翻一次
                                 uv.width * cr.W / w,
                                 uv.height * cr.H / h));
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
            // 🔴 **2026-10-11（`A1515`）**：页判据（`PageOn`）**只由 `SyncFilterDrawerPage` 一处写** ——
            //   本函数每次重建内容都要重推一次（否则「窗口是带着 Cosmetics 页建的」那种路径会漏）。
            SyncFilterDrawerPage();
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
            // 🔴 **2026-10-21（`A1177`）**：位置走 `FltLocal`（= `Pos(画布 px) − 容器原地`）——
            //   原来直接给 `Pos(...)`，只有当「容器局部原点 = `Root` 原点」时才对（理由 → `DrawerLocal` 那段）。
            if (_fltInputRoot != null) _fltInputRoot.transform.localPosition = FltLocal(r.CX, r.CY);

            // 空的时候画的是**占位符**（原版 `Placeholder` TMP 原文 `Search`），在输入态时显示缓冲 + 光标
            string cur = State.Filter.Name;
            string txt = _editKind == 2 ? (_nameEdit ?? "") + "_"
                                       : (string.IsNullOrEmpty(cur) ? FilterPanelModel.InputPlaceholder : cur);
            var tr = FltAbs(taR.x1, taR.y1, taR.x2, taR.y2);
            if (_fltInputText != null)
            {
                _fltInputText.SetText(txt);
                // 🔴 **2026-10-07（A62 主表 #4 + A77①⑩）**：这一格**原来是拿收藏窗那对共用值 + 开自适应**画的
                //   （`FilterPanelModel.InputFontPx=30 / InputFontAutoMin=18`，还会被 `SetAutoFitBox` 内部**无条件开折行**）
                //   —— **三项都不是卡组编辑窗的原版**。现读判据（`md "Deck Editing Menu" --depth 14 --md` 原始行）：
                //     · `Placeholder` `'Search' 字号=26.0 对齐=Left/Middle **折行=0**`（**该行没有 `auto[…]` 段**）
                //     · `Text` `''（零宽空格）字号=26.0 … **折行=3**`
                //   ⇒ 原版：**标称 26 · `m_enableAutoSizing = 0`（`min18/max72` 是不生效的残留值）· 折不折行见下**。
                //   ⛔ 别把 `SetAutoFitBox` 继续挂在这里：它开自适应、还会顺带把 `sizeDelta` 两轴都改写。
                //     这里只要**框宽**（与原版 `Text Area` 那 231.28 一致），模式按原版自己那一档设。
                _fltInputText.SetWrapWidth(LayoutSpace.Px(tr.W));                    // 只取框宽（模式下面再按原版改回来）
                _fltInputText.SetWrappingMode(FilterPanelModel.DeckEditInputWrap);   // **3**（`Text` 的档；同时把折行关掉）
                _fltInputText.transform.localPosition = FltLocal(tr.CX, tr.CY);
                // 🔴 **必须左对齐**（原版 `Placeholder`/`Text` 在 `Text Area` 里是左对齐；
                //    收藏窗那份也是这么画的）—— 不摆的话 `Txt` 是**居中**，字会飘到框中间（2026-09-28 截图看出来的）。
                //    ⚠️ 它放在 `SetWrappingMode` **之后**：`SetWrappingMode` 会重排（A205 的 `ForceRelayout`），
                //       重排会改 `WorldW`，而 `AlignLeftOn` 正是按 `WorldW` 算位置的。
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

            // 🔴 **2026-10-17（D36–D38）**：三个格距/内缩**按窗显式传** —— 模型里那三个缺省常量
            //   （`ArmySpX` 7 / `CostSpX` 15 / `TypePadL` 15）**是收藏窗那一套**（已对上），
            //   卡组编辑这一棵原版**本来就不一样**（铁律 5·c：两棵 prefab 的序列化值不同）：
            //     · `Rarity Filter/Content` 的 `m_Spacing.x` = **5** ⇒ 步进 **105**（收藏窗 7 ⇒ 107）
            //     · `Cost Filter/Content`   的 `m_Spacing.x` = **7** ⇒ 步进 **72**（收藏窗 15 ⇒ 80）
            //     · `Type Filter/Content`   的 HLG `m_Padding.Left` = **40**（收藏窗 15）
            //   判据三条（全包逐实例枚举：`spacing.x=5` 的 9 颗里只有 1 颗是卡组编辑、`=7` 的 3 颗里同样只有 1 颗、
            //   `pad.Left=40` 的 3 颗里也只有 1 颗）→ `资料/普查产出_1017/对账_卡组编辑部分_差异与待办.md` D36–D38。
            //   ⛔ **别去改缺省常量** —— 改了会把收藏窗改歪（模型侧已按「新增专用常量 + 可选形参」落地）。
            // 🔴 **2026-10-14（账 `N14`）第四个按窗分的形参**：**费用那一行要不要督军才显示** ——
            //   卡组编辑传 `CostRowNeedsWarlordDeckEdit`（原版 `CardCostFilter__SetFilterToDeck.c:21`
            //   的 `SetActive(deckHero != null)`），缺省 `false` = 收藏窗（原版**从不**给它下发卡组
            //   ⇒ 那两份 `Cost Filter` 恒 active）。判据全文与那 3 个实例的父链 → `Core/FilterPanelModel.cs`
            //   的「费用那一行的显隐」整节。
            FilterPanelModel.Build(State, FltW, _fltCells,
                                   raritySpX: FilterPanelModel.RaritySpXDeckEdit,
                                   costSpX: FilterPanelModel.CostSpXDeckEdit,
                                   typePadL: FilterPanelModel.TypePadLDeckEdit,
                                   costRowNeedsWarlord: FilterPanelModel.CostRowNeedsWarlordDeckEdit);
            var band = FltBand;                    // 带口在这一趟里恒定（原版 `Viewport`；见 `FltBand`）
            foreach (var c in _fltCells)
            {
                var b = FltAbs(c.Bg.x1, c.Bg.y1, c.Bg.x2, c.Bg.y2);
                // 🔴🔴 **2026-10-10（A224②）：这里原来有一句「滚出面板的不建」，已删。**
                //   原版是【全量 `Instantiate`】——`CardRarityFilter__FillToggleList.c` 与
                //   `CardTypeFilter__FillToggleList.c`（同一个泛型的两次实例化、逐句同形）的循环体里
                //   **没有任何视口 / 滚动位置 / 可见性判据**（唯一那个 bool 是 option 自己的标志位），
                //   `CollectionFilterToggle__Initialize.c` 也只 `set_sprite` + 赋值
                //   ⇒ 七行 option **一次建完**、格子数**不随滚动变**（判据全文 → `资料/普查产出_1009/查证V1_原版prefab四件.md` §一）。
                //   ⚠️ 越界的那些**不是不建，是不画**：原版靠 `Viewport` 上那颗 `Image,Mask` 裁掉
                //   ⇒ 我们在下面用 `ClipCellToBand`（截几何 + 截 uv）兑现同一件事。
                //   ⛔ 别把「整块在带外就不建」换个地方写回来 —— **全量建是判据本身**
                //   （`Editor/DeckScene.cs` 那条「数量不随滚动变」的断言正盯着它）。
                // 🔴 2026-10-04（A24）：**开关那一类按状态换图** —— 原版那三颗是 `EverguildToggle`
                //   （`changeSpriteOnValueChange=1` · `onSprite=40_main_bt_toggle_on` · `offSprite=40_main_bt_toggle_off`
                //    · `spriteToChange` = 自己那个 `Image`）⇒ 关掉时**换成 off 那张**。
                //   原来恒画 `40_main_bt_toggle_on` 那张、只靠色偏 ⇒ **关掉的开关和打开的长得一模一样，只暗一点**。
                var tex = Ui(c.IconOff != null && !c.On ? c.IconOff : c.Icon);
                // 🔴 **图取不到就不登记点击区** —— 否则会出现「看不见却点得动」的空格
                //    （缺图由 `Ui()` 记账，最终由 `DeckScene` 的「一张不缺」断言兜住）
                if (tex == null) continue;
                var r = FltAbs(c.R.x1, c.R.y1, c.R.x2, c.R.y2);
                // 🔴 **2026-10-10（A224②）：点击区 = 格 ∩ 带口**（原来直接登记整格）。
                //   原版那颗 `Mask` **自己就是 `ICanvasRaycastFilter`**（`Mask.IsRaycastLocationValid`
                //   → `RectTransformUtility.RectangleContainsScreenPoint`，本地 uGUI `…/UI/Core/Mask.cs` 的 `Mask.IsRaycastLocationValid`），
                //   而 `Graphic.Raycast` 会**沿父链**逐个过 `ICanvasRaycastFilter` ⇒ **带口外的点判不中任何格**。
                //   整块在带外 ⇒ `ClipRect` 答 false ⇒ 不登记（= 与「被裁掉的那些点不到」一致，
                //   也就是删除早退**之前**那些格的既有行为）。
                PxRect hr;
                // 🔴 **2026-10-14（账 `N14`）**：`Hidden` 的格子（今天只有「卡组还没督军时的费用 8 档」）
                //   **一个点击区都不登记** —— 原版那颗 `Cost Filter` 节点一关，子树**连射线一起没了**
                //   （uGUI 的 `Graphic.Raycast` 沿父链判 `activeInHierarchy`）⇒ 我们兑现同一条。
                //   ⛔ 少了这一句就是本仓明令禁止的「**看不见却点得动**」的空格。
                if (!c.Hidden && MenuDraw.ClipRect(r, band, out hr))
                    _fltHit.Add(new Btn { Key = c.Key, X = hr.x1, Y = hr.y1, W = hr.W, H = hr.H });
                float w = b.W, h = Mathf.Max(1f, b.H);
                // 原版 `m_PreserveAspect`：按图自身宽高比**内接**进框、中心不动（与 `Img(keepAspect)` 同一条）
                if (tex.height > 0 && w > 0f) { float sa = (float)tex.width / tex.height, ra = w / h; if (sa > ra) h = w / sa; else w = h * sa; }
                var q = ImageQuad.Create(FltParent, tex, FltLocal(b.CX, b.CY), U(h), new Vector2(0.5f, 0.5f), "flt_cell");
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
                    // 🔴 2026-10-10（A224②）：**建的时候照常整格建**（原版全量 `Instantiate`），
                    //   建完这一刀把它截到带口内（等价原版 `Viewport` 的 `Mask`）。
                    //   ⚠️ 放在 `SetTint`/`HoverTint` **之后**：那一对只抓材质色/图，与几何无关。
                    // 🔴 **2026-10-14（账 `N14`）**：`Hidden` ⇒ **不画**。**节点照建**（与上面
                    //   「整块在带外 ⇒ 只关不删」同一口径：自检按 key 仍找得到这一格，
                    //   `UiFilterCellQuad` 能读它的 `activeSelf`）⇒ ⛔ 别改成「不建」。
                    if (c.Hidden) q.gameObject.SetActive(false);
                    else ClipCellToBand(q, band);
                    _fltCellObjs.Add(q.gameObject);
                    if (!string.IsNullOrEmpty(c.Key)) _fltCellQuads[c.Key] = q;   // 自检读它（`UiFilterCellTex`）
                }

                if (string.IsNullOrEmpty(c.Label)) continue;
                var lr = FltAbs(c.Lab.x1, c.Lab.y1, c.Lab.x2, c.Lab.y2);
                var lb = Label.Create(FltParent, c.Label, FltLocal(lr.CX, lr.CY), 1, CellTint(c),
                                      new Vector2(0.5f, 0.5f), "flt_lab");
                if (lb == null) continue;
                // 🆕 **2026-10-17（`A1024③`）**：原版 `Deck Editing Menu` 整棵 **0 个 Asar** ⇒ 筛选项 = **Pragati**
                lb.SetFace(TmpFont.Face.LatinCardBody);
                if (!string.IsNullOrEmpty(c.Key)) _fltCellLabels[c.Key] = lb;   // 自检读它（`UiFilterCellLabelLeft`）
                lb.SetRenderQueue(QFltText);
                lb.SetGlyphHeight(LayoutSpace.Px(c.LabelPx));
                // 原版那几行是 `auto(min-max)`：**不开自适应的话 `Legendary` 在 100px 格里冲出去**
                // 🔴 **2026-10-13（A407）**：上限与 base **也照 `Cell` 里那两格原版实读值传** —— 原来只传 4 参
                //   ⇒ 上限退回 `LabelPx`、base 退回「调用方那一档」。真偏离在**稀有度 / 类型**两族：
                //   原版 `auto[10~27] · base 36`、标称只有 `23.2` ⇒ 天花板矮 **3.8px**
                //   （= A333 在收藏窗那半抓到的同一处；四族的值与出处 → `Core/FilterPanelModel.Cell`，
                //     ⛔ **别在这里自己填数**）。
                //   `> 0f ? :` 这个兜底与**同族样张** `Shell/CollectionWindow.TextAligned` 逐字同源
                //   （那扇窗 2026-10-12 A333/A336④ 已收口）——「`0` = 不指定 ⇒ 旧行为」是同一条口径。
                if (c.LabelAutoMin > 0f)
                    lb.SetAutoFitBox(LayoutSpace.Px(lr.W), LayoutSpace.Px(lr.H), c.LabelAutoMin,
                                     c.LabelAutoMax > 0f ? c.LabelAutoMax : c.LabelPx, c.LabelBase);
                // 🔴 **2026-10-07（A62 主表 #5）**：折行按 `Cell.LabelWrap`（**逐族实读的原版 `m_TextWrappingMode`**）显式设 ——
                //   四族里只有**费用桶**是 `1`（开关/稀有度/类型都是 `0`），而 `SetAutoFitBox` 上面刚**无条件**把折行打开了
                //   ⇒ 不显式设的话这三族都是「碰巧错」。⛔ 别按 `LabelCenter` 反推（那会把稀有度/类型静默漏掉）。
                lb.SetWrapping(c.LabelWrap == 1);
                // 🔴 **2026-10-05（A32④）订正**：原来这句写「两个开关行的标签原版是 **hAlign=Center**（A3 §5·1）
                //   ⇒ 居中时不要再摆对齐」—— **`Center` 那个读数是错的**：全包 8 个 `Owned only`/`Upgradable only`
                //   的 `m_HorizontalAlignment` 实测都是 `1`(Left)（两扇窗都读过）
                //   ⇒ 模型里 `LabelCenter` 已改成 `false`，于是这三行走 `AlignLeftOn(lr.x1)`。
                if (!c.LabelCenter)
                {
                    float wx = c.LabelRight ? LayoutSpace.FromPixel(lr.x2, 0f).x : LayoutSpace.FromPixel(lr.x1, 0f).x;
                    if (c.LabelRight) lb.AlignRightOn(wx); else lb.AlignLeftOn(wx);
                }
                // 🔴 **2026-10-10（A224②）：标签吃同一道带口** —— 原版那颗 `Mask` 对**文字与图一视同仁**
                //   （掩码在 shader 里按像素裁）⇒ 压在带口的字会被切半个。
                //   ⚠️ **必须在最后调**：`SetGlyphHeight` / `SetAutoFitBox` / `SetWrapping` / 上面那两句对齐
                //     都会重排 TMP 的 mesh（`MenuDraw.ClipText` 那段注释写明了这一点）—— 放前面等于没裁。
                //   ⚠️ 整块在带外 ⇒ 同 quad：**只关不删**（`Visible` 判的是**标签矩形**；
                //     那种格子的图与点击区上面已经一起落空了）。
                // 🔴 **2026-10-14（账 `N14`）**：`Hidden` ⇒ 标签同样**不画**（节点照建；口径同上面那一格）。
                //   ⚠️ 它排在最后、在 `SetGlyphHeight` / `SetAutoFitBox` / `SetWrapping` / 对齐**之后**
                //     —— 「TMP 在非激活对象上量不出尺寸」那条老坑要求这些调用都在**激活态**里跑完。
                if (c.Hidden) lb.gameObject.SetActive(false);
                else if (!MenuDraw.Visible(lr, band)) lb.gameObject.SetActive(false);
                else MenuDraw.ClipText(lb, band, Vector2.zero);
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
            // 🔴🔴 **2026-10-14（账 `N14`）：费用那一行（`Energy Cost` 小标题 + 8 档格子）在
            //    【卡组还没有督军】时**不显示** —— 判据只此一处 = `FilterPanelModel.CostRowVisible`
            //    （原版 `CardCostFilter__SetFilterToDeck.c:21` 的 `SetActive(deckHero != null)`；
            //     整节点一关，它下面的 `Title` 与 `Content` 一起不画）。**
            //    ⚠️ 两条形状上的硬要求（都是本仓踩过的坑）：
            //    ① **`BuildTitles` 必须挪到下面那句 `if (!_filtersOpen) return;` 【之前】** ——
            //       否则抽屉收起/展开那 0.3 秒里 `titles` 还是空的、判据读不到，那一行会被 `SetOn`
            //       重新点亮（原版那颗节点是**持久**关着的，不会闪）。
            //    ② **`Title` 仍发四条**（`Hidden` 只是标记）—— 行的顺序/下标是两个宿主共用的映射，
            //       少发一条会把 `Type` 那行错配到 `Energy Cost` 的位置上。
            var titles = new List<FilterPanelModel.Title>();
            FilterPanelModel.BuildTitles(State, FltW, titles,
                                         costRowNeedsWarlord: FilterPanelModel.CostRowNeedsWarlordDeckEdit);
            for (int i = 0; i < _fltTitles.Count; i++)
            {
                var lb0 = _fltTitles[i];
                if (lb0 == null) continue;
                // ⚠️ 这一句**必须排在 `SetOn(lb, live)` 之前**（见上面 ①）
                if (i < titles.Count && titles[i].Hidden) { lb0.gameObject.SetActive(false); continue; }
                SetOn(lb0, live);
            }
            if (!_filtersOpen) return;
            var band = FltBand;           // 🆕 A289：与格子**同一道带口**（`FltBand` 那段记了判据）
            for (int i = 0; i < _fltTitles.Count && i < titles.Count; i++)
            {
                var lb = _fltTitles[i];
                if (lb == null) continue;
                // 🔴 **2026-10-14（账 `N14`）**：关着的那一行（今天只有 `Energy Cost`）**整条跳过** ——
                //   它此刻是 `SetActive(false)`，而下面 `ForceRelayout()` / `AlignLeft` 都要**量 TMP 宽度**
                //   （`Label.WorldW` → `EnsureMeasured`），**在非激活对象上量出来是 0** ⇒ 会写进一个错的
                //   x（本仓那条老坑：「TMP 在对象没激活时量不出尺寸」）。不摆位不影响它 ——
                //   重新显示的那一拍 `SetOn` 之后，本循环照样会重排 + 重对齐一次。
                if (titles[i].Hidden) continue;
                // 🔴🔴 **2026-10-11（A289）：这四个小标题也要吃带口那一刀** —— 它们与格子住在**同一个
                //   滚动内容里**（`FltAbs` 减 `_fltScroll` 摆位）⇒ 滚到中间位置时**会压进 `Header`**
                //   （算例：`_fltScroll = 200` ⇒ `Army` 小标题绝对 y = `156 + 179.02 − 200 = 135.02`，
                //    正落在 `Header` 带里）。原版那颗 `Viewport > Image,Mask` 对**文字与图一视同仁**。
                //   ⛔ **不许照格子那套直接调 `MenuDraw.ClipText`**：那个会**挂 `ClippedTextGuard`**，
                //     而守卫在**下一次 `RefreshBounds()`**（本函数第一句 `ForceRelayout()` 内部就有一次）
                //     就会抢跑一刀 —— 那一刻标签还停在**上一轮的**位置 ⇒ 在**已被夹过的网格上二次夹**
                //     （几何被夹第二次、uv 只按第一次的比例走 ⇒ 静默、越滚越坏，同 A225-② 那一族）。
                //   ⇒ 次序是**死的**（三步，别调换）：
                //     ① `ForceRelayout()` —— 复用件：先把上一轮**烘进 mesh** 的那一刀冲掉（TMP 重排一次，
                //        几何与 uv 都回到「没裁过」的状态）；
                //     ② 摆位 + 对齐 —— 两个都会动 mesh / 都要在裁剪**之前**（对齐按 `WorldW` 算）；
                //     ③ `ClipTextNow(band, Vector2.zero)` —— **不挂守卫**（硬裁，`Vector2.zero` = 无软边；
                //        原版那颗 `Mask` 的 `m_Softness` 就是 0）。
                lb.ForceRelayout();
                var r = FltAbs(titles[i].R.x1, titles[i].R.y1, titles[i].R.x2, titles[i].R.y2);
                lb.transform.localPosition = FltLocal(r.CX, r.CY);
                // 🔴 **2026-10-05：左对齐和摆位【一起】做** —— 原版这四行 `Title` 是 `Left/Middle`
                //   （判据 = `FilterPanelModel.TitleFontPx` 那段，两扇窗逐行实读）。
                //   ⚠️ 为什么不能只在**建的时候**对齐一次：`_fltTitles` 里的 `Label` 是**复用的**，
                //   而这个方法在**每次滚动**时都要重新摆一遍（`localPosition = Pos(...)` 会把 x 也写回中心）
                //   ⇒ 「建时对齐」滚一下就散。所以对齐必须挂在**同一处**、写在摆位后面。
                if (titles[i].Left) MenuDraw.AlignLeft(lb, r);
                // ⚠️ **整块在带外时：不关节点、也不删 —— 与格子那条「只关不删」**不同**，理由如下**
                //   （不是漏做，⛔ 别照格子那一段「统一」过来）：这四个 `Label` 是**复用件**，而
                //   本函数开头那句 `SetOn(lb, live)` 在**收起的那 0.3 秒**里会按 `DrawerLive` 把整栏
                //   重新点亮 —— 那一刻本函数已经 `return`（`!_filtersOpen`）⇒ **不会再裁一刀** ⇒
                //   被关掉的行会带着**上一轮的旧网格**重新出现（比不裁更糟）。
                //   而「字形全部落在带外」这一档，夹完之后**就是零面积（退化）⇒ 本来就看不见**；
                //   原版那颗 mask 也只是不画、**并不关节点** ⇒ 收在这儿。
                MenuDraw.ClipTextNow(lb, band, Vector2.zero);
            }
        }

        bool HandleFilterClick(Vector2 px)
        {
            foreach (var b in _fltHit)
                if (px.x >= b.X && px.x <= b.X + b.W && px.y >= b.Y && px.y <= b.Y + b.H)
                {
                    // 原版这一格是 `EverguildToggle`（`…/Filters/*/Content/Toggle`）⇒ `Select Toggle`
                    //（R3 §二·C-1 `Deck Editing Menu` 那 6 行；本窗这条点击链不走 `MenuDraw.Hit`）。
                    // ⚠️ `$name` = 搜索框（**输入聚焦，不是按钮**）⇒ 不播（R3 §五·5.4 的「输入框不传 cue」）。
                    if (b.Key != "$name")
                        WindowSounds.PlayFx(WindowSounds.SelectToggleCue, $"卡组编辑筛选 `{b.Key}`");
                    HandleFilterRow(b.Key);
                    return true;
                }
            return false;
        }

        // ============================================================ 卡背页那套抽屉（`Cosmetic FIlter`）

        /// <summary>画卡背页那个抽屉。内容只有两行（Army 13 格 + Owned 开关），
        /// 全高 `15 + **630** + 12.81 + 50 ≈ 707.8` &lt; 抽屉 924.06 ⇒ **不用滚动**（原版那棵树里也没有 Scroll View）。
        /// 🔴 **2026-10-17（G2/D39）**：这里原来写 `550`/`628` —— 那是「Army 行 `m_Spacing.y = 0`」的算法；
        /// **本窗**（卡组编辑）原版是 **20** ⇒ Army 行 **630**、全高 **707.8**（判据 → `FilterPanelModel` 的 Cosmo 那段）。
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
                // 🔴 **2026-10-09（A247）**：**本窗**卡背抽屉那颗 `'Owned only'` 的自适应下界 = 原版 **26**
                //   （`auto[26~32]`，判据与出处 → `FilterPanelModel.CosmoOwnedFontAutoMinDeckEdit`）；
                //   **收藏窗**那颗才是 `auto[18~32]`。按 A77⑩「按窗分参数」**只在本调用点传**
                //   —— ⛔ 别去动共用常量 `ToggleFontAutoMin`（改它会把收藏窗一起改歪）。
                // 🔴 **2026-10-17（G2 · D39/D40/D41/D42）**：本窗那棵树（`Deck Editing Menu > Content Area >
                //   Cosmetic Display > Cosmetic FIlter`）与收藏窗那份**又差三处**（铁律 5·c）：
                //   Army 行 `m_Spacing.y = 20`（⇒ 行高 630、`Owned` 行顶 813.81）·
                //   `Owned Toggle/Image` `sd(80,0)` · `Owned Toggle/Label` `sd(230,0)`（⇒ 25..255）。
                //   判据与「唯一的那一颗」三条 → `FilterPanelModel` 的 Cosmo 那段。
                //   ⛔ **只在【本调用点】传** —— 缺省那几个 = 收藏窗那份，动它会把收藏窗改歪
                //      （形状照 A247 那次 `CosmoOwnedFontAutoMinDeckEdit` 的先例）。
                //   ⚠️ 四个形参**全是 float** ⇒ 调换了编译器不报 ⇒ 一律写**具名实参**。
                // 🔴🔴 **2026-10-18（A890）：`Image` 那半的「锚法」现在落到【原始序列化字段】上了 —— 判据可复跑。**
                //   账 = 「D41 的锚法只落到矩形上」：当时只有 `menu_dump` 的**布局模拟行**，于是
                //   「面板宽可变时，那颗 `Image` 的右缘是**固定 308.9** 还是 `w−25`」判不出来
                //   （⚠️ 两棵树在各自真实宽度处**重合**）。**现读 RT 本体（不是模拟值）⇒ 答 `w − 25`：**
                //   · 本窗 `…/Cosmetic FIlter/Filters/Owned Toggle/Image`
                //     = `d:/2/新解包资源/assets_full/bundle_menus_assets_all/RectTransform/RectTransform_-5843448329795342556.json`
                //     `m_AnchorMin (1,0)` · **`m_AnchorMax (1,1)`** · `m_Pivot (1,0.5)` ·
                //     `m_AnchoredPosition (-25,0)` · **`m_SizeDelta (80,0)`**
                //     ⇒ **右锚**：右缘恒 = 父级右缘 − 25（**面板一宽它就跟着走**）；
                //     **固定的那个量是【宽 80】，不是右缘** —— `308.90` 只是 `w = 333.90` 那一刻的取值。
                //   · `…/Owned Toggle/Label` = `…/RectTransform_-6194547466839724252.json`：
                //     `m_AnchorMin (0,0)` · `m_AnchorMax (0,1)` · `m_Pivot (0,0.5)` · `apos (25,0)` · `sd (230,0)`
                //     ⇒ **左锚 + 固定宽** ⇒ 25..**255**，**不随 `w` 变**（⚠️ 同一行里两半**不一样**，别一刀切）。
                //   · 收藏窗那棵（`…Cardback Tab/Cardback Display/Cosmetic FIlter/Filters/Owned Toggle (1)`）
                //     = `…/RectTransform_1643317952186213077.json`：`AnchorMin (0.7,0)` · **`AnchorMax (1,1)`** ·
                //     `Pivot (1,0.5)` · `apos (-25,0)` · `sd (-30,0)` ⇒ **同样右锚**、宽 `0.3w−30`
                //     ⇒ **两棵树差的只是【宽】，右缘规则相同**（= `ToggleRowRects` 的 `ix2 = w − ToggleIconRightIn`）。
                //   ⇒ 模型里那句「右缘 `w−25` + 固定宽」**就是**原版锚法（不是近似、不是我们挑的）—— A890 可销。
                //   ★ 断言在 `Editor/DeckScene.cs` 卡背那节：**宽度一改，右缘必须跟着改同样的量、宽必须一动不动**
                //     （写成「固定右缘」⇒ 右缘差命 0 ⇒ 红；退回锚点式 ⇒ 宽差命 0 ⇒ 红）。
                FilterPanelModel.BuildCosmetics(State.Factions(), _cosmoFilter, FltW, _cosmoFltCells,
                                                labelAutoMin: FilterPanelModel.CosmoOwnedFontAutoMinDeckEdit,
                                                armySpY: FilterPanelModel.CosmoArmySpYDeckEdit,
                                                iconW: FilterPanelModel.CosmoIconWDeckEdit,
                                                labW: FilterPanelModel.CosmoLabWDeckEdit);
                foreach (var c in _cosmoFltCells)
                {
                    // 🔴 **2026-10-18（A889）**：卡背这一族**只此一处换算**（`FltAbsCosmo`）——
                    //   原来走 `FltAbs`，减的是**卡牌抽屉**的 `_fltScroll`（别人的滚动位）。
                    var b = FltAbsCosmo(c.Bg.x1, c.Bg.y1, c.Bg.x2, c.Bg.y2);
                    // 🔴 2026-10-04（A24）：卡背页那颗 `Owned Toggle` 同样**按状态换图**（判据同卡牌那一套：
                    //   原版 `… > Cosmetic FIlter > Filters > Owned Toggle` 的 `offSprite = 40_main_bt_toggle_off`）。
                    var tex = Ui(c.IconOff != null && !c.On ? c.IconOff : c.Icon);
                    // 同卡牌那套：**图取不到就不登记点击区**（不做「看不见却点得动」的空格）
                    if (tex == null) continue;
                    var r = FltAbsCosmo(c.R.x1, c.R.y1, c.R.x2, c.R.y2);
                    _cosmoFltHit.Add(new Btn { Key = c.Key, X = r.x1, Y = r.y1, W = r.W, H = r.H });
                    float w = b.W, h = Mathf.Max(1f, b.H);
                    if (tex.height > 0 && w > 0f) { float sa = (float)tex.width / tex.height, ra = w / h; if (sa > ra) h = w / sa; else w = h * sa; }
                    var q = ImageQuad.Create(CosmoFltParent, tex, CosmoFltLocal(b.CX, b.CY), U(h), new Vector2(0.5f, 0.5f), "cosmoflt_cell");
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
                    var lr = FltAbsCosmo(c.Lab.x1, c.Lab.y1, c.Lab.x2, c.Lab.y2);
                    var lb = Label.Create(CosmoFltParent, c.Label, CosmoFltLocal(lr.CX, lr.CY), 1, CellTint(c),
                                          new Vector2(0.5f, 0.5f), "cosmoflt_lab");
                    if (lb == null) continue;
                    // 🆕 **2026-10-17（`A1024③`）**：同上（Deck Editing Menu 整棵 0 个 Asar）⇒ Pragati
                    lb.SetFace(TmpFont.Face.LatinCardBody);
                    lb.SetRenderQueue(QFltText);
                    lb.SetGlyphHeight(LayoutSpace.Px(c.LabelPx));
                    // 🔴 **2026-10-13（A407）**：本行与上面 `RefreshFilterCells` 那处**同一条口径**
                    //   （上限 + base 照 `Cell` 里那两格原版实读值传；原来只传 4 参 ⇒ 两样都退回旧行为）。
                    //   ⚠️ **本族的数值上「看不出差别」**：卡背抽屉只有 `Owned only` 一族，
                    //   原版 `auto[18/26~32] · base 32` 而标称就是 **32**（`ToggleFontPx`）
                    //   ⇒ 上限与 base 都**恰好等于标称** ⇒ 接上它只是**把字段变成显式的**（不再靠巧合），
                    //   与上面那一处的 3.8px 真偏离不同。判据同上 → `Core/FilterPanelModel.Cell`。
                    if (c.LabelAutoMin > 0f)
                        lb.SetAutoFitBox(LayoutSpace.Px(lr.W), LayoutSpace.Px(lr.H), c.LabelAutoMin,
                                         c.LabelAutoMax > 0f ? c.LabelAutoMax : c.LabelPx, c.LabelBase);
                    // 🔴 **2026-10-07（A62 主表 #6）**：卡背页那颗 `'Owned only'` 原版是 **`折行=0`**
                    //   （`python 工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 18 --md`，
                    //   该行 `auto[26.0~32.0]`）⇒ 照 `Cell.LabelWrap` 显式关掉（`SetAutoFitBox` 上面刚无条件开过）。
                    lb.SetWrapping(c.LabelWrap == 1);
                    if (!string.IsNullOrEmpty(c.Key)) _cosmoFltLabels[c.Key] = lb;   // 自检读它（`UiCosmoFilterCellLabel`）
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
            _cosmoFltLabels.Clear();
            _cosmoFltHit.Clear();
            // 🆕 2026-10-05（A32②）：`$owned` 那颗悬停件跟着 quad 一起销毁了 ⇒ 登记也要撤
            //   （理由同 `ClearFilterCells`；卡背这一栏里**没有常驻件**，清空是安全的）。
            _cosmoHoverBtns.Clear();
        }

        bool HandleCosmoFltClick(Vector2 px)
        {
            foreach (var b in _cosmoFltHit)
                if (px.x >= b.X && px.x <= b.X + b.W && px.y >= b.Y && px.y <= b.Y + b.H)
                {
                    // 原版 `…/Cosmetic FIlter/Filters/{Owned Toggle,Army Filter/Content/Toggle}`
                    // = `EverguildToggle` ⇒ `Select Toggle`（R3 §二·C-1 `Deck Editing Menu` 那两行）。
                    WindowSounds.PlayFx(WindowSounds.SelectToggleCue, $"卡组编辑卡背筛选 `{b.Key}`");
                    ApplyCosmoFilter(b.Key);
                    return true;
                }
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

        /// <summary>🆕 **2026-10-07（A77①⑩ 的自检口）**：搜索框那个 `Label` **本身**
        /// —— 自检要读它的**换行模式**（原版第三档 `3`）与 `AutoSizing`（原版 `m_enableAutoSizing = 0`），
        /// 这两个都读不出来就没法断「字段说了、画面没变」那一档（A205）。
        /// ⚠️ 点阵后端（`_tmp == null`）读出来是 `WrappingMode = -1` / `AutoSizing = false` ⇒ **会红**，不静默。</summary>
        public Label UiFilterInputLabel { get { return _fltInputText; } }

        /// <summary>🆕 **2026-10-07（A62 #5 的自检口）**：某个筛选格标签的 `Label` **本身**（读换行模式用）。
        /// key 同 `UiFilterCellLabelLeft`。取不到返回 `null`。</summary>
        public Label UiFilterCellLabel(string key)
        {
            Label lb;
            return (key != null && _fltCellLabels.TryGetValue(key, out lb)) ? lb : null;
        }

        /// <summary>🆕 **2026-10-10（A224② 的自检口）**：某个筛选格**图示**的 `ImageQuad` **本身**
        /// （自检要量它的**渲染矩形** —— 「压在带口的格子被裁住了」那条断言）。
        /// key 同 `UiFilterCellLabelLeft`。取不到返回 `null`。
        /// <para>⚠️ 拿到的可能是**被 `SetActive(false)`** 的那一格（整块在带口外 —— 节点照建、只是不画，
        /// 见 `ClipCellToBand`）⇒ 量之前自己判 `gameObject.activeSelf`。</para>
        /// <para>⛔ 返回的是**节点本体**、不是从哪个常量算出来的矩形：自检量的必须是**真渲出来的几何**
        /// （`transform.position` + `WorldW/H`），量模型矩形就测不到「裁没裁」。</para></summary>
        public ImageQuad UiFilterCellQuad(string key)
        {
            ImageQuad q;
            return (key != null && _fltCellQuads.TryGetValue(key, out q)) ? q : null;
        }

        /// <summary>🆕 **2026-10-07（A62 #6 的自检口）**：**卡背页**筛选格标签的 `Label` 本身。
        /// ⚠️ 与卡牌那栏**分开存**（两栏可以同时存在、key 又都是 `$owned`，见 `_fltHoverBtns` 那条同族注释）。</summary>
        public Label UiCosmoFilterCellLabel(string key)
        {
            Label lb;
            return (key != null && _cosmoFltLabels.TryGetValue(key, out lb)) ? lb : null;
        }

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

        /// <summary>🆕 **2026-10-17**：把**整棵子树**上同一个色（九宫格那种：只染一块会留下另外 8 块）。
        /// 行边框（`_deckRowBorder`）与页签高亮（`_tabHiRoot`）是同一形状的需求 —— 两处共用这一份。</summary>
        static void TintTree(GameObject go, Color c)
        {
            if (go == null) return;
            foreach (var q in go.GetComponentsInChildren<ImageQuad>(true)) q.SetTint(c);
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

        /// <summary>🆕 **2026-10-07（A62 #2 的自检口）**：第 `i` 个页签名牌那行字的 `Label` **本身**
        /// （读换行模式用：原版三颗是 `1 / 1 / 0`，只有 `Cosmetics` 不折行）。取不到返回 `null`。</summary>
        public Label UiTabLabelAt(int i)
        {
            return (i >= 0 && i < _tabLabel.Count) ? _tabLabel[i] : null;
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
            x = rx; y = ry; w = RowNodeW; h = RowH;
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

        /// <summary>🆕 A24：第 `i` 行**行底**现在的色偏系数（`1` = 常态 · `0.6887` = 原版悬停 · `-1` = 取不到）。
        /// 🔴 **2026-10-21（`A1720`）**：量的是**现在开着的那一份**行底（槽 0 两套 —— 见 `RowBgGo`），
        /// ⛔ 不是写死 `_deckRowBg[i]`（槽 0 在卡行档时那一份是关着的，读数会停在**上一轮的色**上）。</summary>
        public float UiRowBgTint(int i)
        {
            if (i < 0 || i >= _deckRowBg.Count) return -1f;
            var bgGo = RowBgGo(i);
            if (bgGo == null) return -1f;
            var q = bgGo.GetComponentInChildren<ImageQuad>(true);
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
            if (State.TryAdd(def, out why)) { MarkDeckDirty(); RefreshAll(); Say("已加入：" + CardText.Name(def)); }
            // 🔴 **2026-10-18（A985①）**：`State.TryAdd` 的 `out why` **现在已经是人话**了
            //   （`DeckEditorState.TryAdd` 里头自己过了一遍 `DeckRuntime.DeckErrorText(e)`）——
            //   ⛔ 这里**不许再包一层** `Loc.T`：那会把一句中文当成键去查表 ⇒ 查不到 ⇒ 原样返回 +
            //   **记一次 `Loc.MissingCount` + 出一条「语言表里没有这个词条」的假告警**（静默坏账）。
            else SayError(why);   // 🆕 2026-10-26（B9 · 卡组:325）：加不进 ⇒ 屏幕错误横幅（原版 `OnCardClick` 那一支）
        }

        /// <summary>🆕 **2026-10-12（A363）**：**标脏 + 刷界面** —— 突变只走这一条，**不落盘**。
        /// 原版对应物 = 那三处 `deck[+0x60] = 0`（清单见 <see cref="DeckDirty"/>）。
        ///
        /// <para>为什么要顺手刷页头：原版每次突变之后 `Done` 那颗灯都会重算
        /// （`DeckEditingWindow__UpdateDoneButton` 里就是一句 `ValidateDeck`，`:24`），
        /// 而我们的 `foot_hl` 也挂在 `RefreshHeader()` 上 ⇒ 两件事一起做才不会出现
        /// 「内容变了、灯还是上一拍的」。</para>
        ///
        /// <para>⚠️ 这里**故意不**调 <see cref="CommitDeck"/>（那就是原来那个错）。</para></summary>
        public void MarkDeckDirty()
        {
            DeckDirty = true;
            RefreshHeader();
        }

        /// <summary>**真写回卡组库（落盘）** —— 全窗**只有 `SaveAndSay()` 一处**会走到它
        /// （🆕 A363 改向；判据 = 原版 `UploadDeck` 只由 `__TrySaveDeck` 的成功支调用）。
        /// 写成功才清脏标记；失败**不吞**（⚠️ **2026-10-18（`G8`）就地更正**：这里原来写「`Library.LastError`
        /// 由页头 `_storeErr` 显示出来」—— **那件 2026-10-17 已按 D10 删件删掉**（原版侧栏没有它），
        /// 见 `:611` 那段。今天这条通道是：`SaveAndSay()` 的 `Say(...)` 日志 + `UiLastSay`（自检读它）；
        /// 给玩家看的人话走词条 `Loc.T(TermSaveFailed)`，`LastError` 只当**兜底诊断**），
        /// 返回给调用方，让 `SaveAndSay()` 能如实出声「保存失败」而不是照样说「已保存」。
        ///
        /// <para>🔴 **2026-10-12（A363）**：原来那句注释写「原版是 `syncedToServer=false` 标脏 +
        /// Done 时才上传；**我们单机直接落盘**」—— 后半句就是我们偏离的地方，**已按原版改掉**。</para>
        ///
        /// <para>🔴 **2026-10-13（A397）**：这里就是原版那条链的**唯一写回点** ——
        /// `DeckEditingWindow__UploadDeck.c` 的 `CardDeck__CopyDeck(库那份 ← 编辑中那份)`
        /// （库那份 = 窗的 `+0x40`，编辑中那份 = `EditingDeck` 在 `+0x118`）。A397 起
        /// `State.Deck` 真的是**另一个对象**（`DeckEditorState.LoadDeck` 装副本）⇒
        /// 这一句现在才名副其实：「编辑中那份」与库里那份是两个对象，靠这里整份拷回去。
        /// ⛔ 别在别处（尤其别在突变那 5 个点）调它 —— 那正是 A363 推翻过的「改一下就落盘」。</para>
        ///
        /// <para>⚖️ **2026-10-14 已裁（A566）：维持现状 —— 这里【不加】守卫。**
        /// 那笔账问的是「**写回哪一副**没钉死」：原版写回的是**开窗时传进来那一副**（`__UploadDeck.c` 里那个
        /// `window + 0x40`），我们写回的是 `DeckLibrary.CommitCurrent` 的「**当前选中**那一套」
        /// （`_decks[_current]`）。两者等价的**前提** = 「编辑期间 `_current` 不变」—— `_current` 只可能被
        /// `Library.Select / Create / Duplicate / Add / Delete` 改，而 `DeckRuntime` 只在 `Build`（`Select`）
        /// 里调它们（`TryImport` 那条 `Add` 2026-10-17 已随 T23 删件删掉）⇒ **今天无可达路径**能改它
        /// （逐处复核表 → `资料/普查产出_1013/WD1b_编辑副本隔离.md` §六·2）。
        /// ⇒ 裁定 = **不加「`_current` 与开窗时不同就出声」的守卫、也不动 `DeckLibrary`**（后者在本件白名单外）；
        /// **理由**：守卫今天**零命中**（加它等于记一条测不到的分支），而真要钉死得先让 `DeckLibrary` 支持
        /// **按下标提交** —— 那是比这笔账大得多的一次改向。
        /// ⚠️ **这条裁定会过期**：谁将来真在编辑器生命周期里调了 `Library.Select`（或别的会挪 `_current`
        /// 的口），就按原版改成「写回开窗那一副」（守卫或按下标提交），⛔ 别当成「已经不用管了」。</para>
        ///
        /// <para>🔴 **2026-10-12（`A1697`）就地订正（铁律 5）：上面那条 A566 裁定【已作废】。**
        /// **错因 / 废止理由**：那条裁定的前提是「**新建卡组也走 `_current`**」—— 它把「写回哪一副」这件事
        /// 简化成「编辑期间 `_current` 不变」，于是「按下标提交」看着可以不做。而**原版根本不是那样**：
        /// 新建的卡组**压根不在库里**（`SelectDecksTab__CreateDeck.c` 从不入库，下标从头到尾没有过），
        /// `Done` 那一拍走的是**入库**（`__TrySaveDeck.c:100 UploadDeck` → `UploadUnsyncedDeck`）、**不是**「写回当前选中」。
        /// ⇒ 用户 2026-10-12 拍板**按原版复刻** ⇒ 本条裁定**作废**，`DeckLibrary` 这一笔**照改**
        /// （加了 `NewUnsaved` / `Insert` 两个口）；而写回的那一副由 `_newDeck` **显式钉住**（= 原版 `window + 0x40` 那一副），
        /// ⛔ 不再依赖「`_current` 恰好没变」这个巧合。
        /// ⚠️ **旧裁定里仍然成立的那半**：「编辑期间 `_current` 不变 ⇒ `CommitCurrent` 写的就是开窗那一副」
        /// —— 那一半**照旧成立**（`DeckRuntime` 仍然只在 `Build` 里动 `Library.Select`；`TryImport` 已随 T23 删），
        /// 只是它现在**只覆盖「编辑库里已有的卡组」那一条路**。</para>
        /// </summary>
        public bool CommitDeck()
        {
            // 🔴 **2026-10-12（`A1697`）**：新建那一路（`_newDeck != null`）走的是**入库**，
            //    照原版 `DeckEditingWindow__UploadDeck.c`（`:63 CardDeck__CopyDeck` → `:74 UploadUnsyncedDeck`）
            //    —— ⛔ **不是** `CommitCurrent`（那会把内容写进**当前选中**那一套；全新的卡组压根没有下标记得自己）。
            if (_newDeck != null)
            {
                bool ins = Library.Insert(State.Deck);
                // ⛔ **无论落盘成败都要翻篇**：`Insert` 已经把它加进 `_decks` 了（同 `Create` 的语义：
                //    内存里留着 + `LastError` 说得出原因）⇒ 不清 `_newDeck` 的话下一次 `Done` 会**再插一遍**
                //    （库里两套同名，静默）。
                _newDeck = null;
                // ⚠️ 换回「副本」形态（保住 A397 那条不变式：`State.Deck` 与库里那一份是**两个对象**）
                //    —— 否则从这一刻起编辑器改的就是**库里那个对象本身**，任何库级 `Save()` 都会把未 `Done` 的改动顺手写盘。
                State.LoadDeck(Library.Current);
                DeckDirty = !ins;        // 没落盘 ⇒ 仍然算脏（下次 `Done` 还会再试一次，同 A547 的口径）
                RefreshAll();            // 批处理没有帧循环 ⇒ 视图要显式跟上
                RefreshHeader();
                return ins;
            }
            bool ok = Library.CommitCurrent(State.Deck);
            if (ok) DeckDirty = false;
            RefreshHeader();
            return ok;
        }

        /// <summary>🔴 **2026-10-12（`A1697`）**：这一扇窗编辑的是哪一副？
        /// `null` = 编辑库里已有的那一套（`CommitDeck` → `CommitCurrent`）；
        /// **非 null** = 一副**还没入库**的新卡组（→ `CommitDeck` 走 **`Insert`** = 原版 `UploadUnsyncedDeck`）。
        /// 判据 → `SelectDecksTab__CreateDeck.c:17,26,28`（新建**不入库**）与 `CommitDeck` 里那段。</summary>
        PlayerDeck _newDeck;

        /// <summary>卡组编辑里点一张卡开的详情窗。
        ///
        /// <para>🔴 **2026-10-12（`A1698`）改向（铁律 5：原文留痕）** —— 这里原来写的是
        /// 「复用**对战**的放大窗（查证：原版放大窗与战斗里那个是同一套 `CardDisplayWindow`）」。
        /// **那句话只对了一半，而且正是这次踩坑的源头**：原版确实是**同一个脚本**，
        /// 但**两处摆放的 `options` 不同** —— 菜单版那扇的 `options` 非 null，而竞技场里 13/13 全是 `null`
        /// （我们这边 = `Battle/CardDisplayWindow.cs:16-22`）。原版卡组编辑**硬编码**要走**有 options 的菜单版**：
        /// `DeckEditingWindow__OpenCardInformation.c:56-58` 那句
        /// `CardDisplayWindow__ShowCard(win, card, tier, **1**(= showOptions), 0, …)`；
        /// 而 `CardDisplayWindow__ShowCard.c:159` 的守卫是 `options != null && card.field_0x4c == 0`
        /// ⇒ `:163 GameObject.SetActive(1)` + `:165 CardDisplayOptions__Initialize`（那一列 = 创建副本 / 升级 /
        /// 异画 / 计数 / 通配符）。⇒ **用户看到「右边那里没有了」的直接原因就在这一句**。
        /// 诊断全文 → `资料/普查产出_第二十三会话/R4_详情窗右侧与异画.md` §二(b)。</para>
        ///
        /// <para>⚠️ **改成调 `Shell/CardDetailPopup`**（= 我们的菜单版那一扇，收藏窗点卡走的也是它，
        /// 判据 → `Shell/CollectionWindow.OpenCardDetail`）—— 它是 `WindowsManager` 的一扇窗，所以要走
        /// `EnsureHost()` + `OpenWindow`（同 `ShowDiscardChangesPopUp` 那条先例；⛔ 别把它建到 `_root` 底下
        /// —— 同那里的理由：会被「所有可见 quad 都在可见区内」那类断言扫进去）。
        /// ⛔ **不改 `CardDetailPopup` 一个字节**（另有一个写手在改它）—— 只调它**既有的公开入口**。</para></summary>
        CardDetailPopup _cardWindow;
        WindowsManager _cardWinHost;

        void OpenCardWindow(CardDef def)
        {
            if (def == null) return;
            if (_cardWinHost == null) _cardWinHost = WindowsManager.EnsureHost();
            if (_cardWindow == null)
            {
                _cardWindow = CardDetailPopup.Create(_cardWinHost);
                _cardWinHost.OpenWindow(_cardWindow);
            }
            // 🆕 2026-09-28：**把卡表项一起传进去** —— 相关卡那一叠要按它算（判据 → `RelatedCards`）。
            //    ⚠️ 换卡靠 `ShowCard` 里那句**显式 `Build()`**（同窗再开不会重建内容）—— 判据 → 它的 doc。
            _cardWindow.ShowCard(def);
            // 🔴 **2026-10-25（`C` 卡组编辑域）：原版这一拍还会换页头徽记** ——
            //   `DeckEditingWindow__OpenCardInformation.c:35` = `WildcardDisplay.Initialize(wildcards, 这张卡的 cardArmy)`
            //   （`WildcardDisplay__Initialize` 只有「army == currentArmy 就早退」那一句 ⇒ 换图）。
            //   ⛔ 早先 `DeckArmyIcon` 的 doc 写着「本笔没做」的**第二条**就是它（铁律 5，已补齐）。
            //   ⚠️ 之后任何一次 `RefreshHeader` 都会把它按「这副卡组的阵营」重置回去 —— 与原版同（原版
            //   也只有开详情这一拍换，关掉详情不会换回来）。
            SetSprite(Lookup("hdr_army"), FactionIcon(def.Faction));
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
        /// 为什么必须有它：本窗有**成组显隐**的按钮（`RefreshHeader` 那两句 `SetOn` 关掉 `hdr_clear`），
        /// 而 `_btns` 里的矩形**从不跟着开关走** ⇒ 那些**看不见的钮照旧吃点击**（真缺陷，不是疑点）。
        /// 历史成因（留着当判据）：2026-10-04 首跑时这条闸是为两族建的 ——
        ///   · 侧栏那两颗圆钮（`info_share` / `info_import`，2026-10-17 已按 D35 删）；
        ///   · 导入弹窗那四颗 `imp_*`（2026-10-17 已按 T23 删 —— 那棵树整棵是外壳那份的重复实现）。
        /// ⇒ 今天只剩 `hdr_clear` 那一支。
        /// 判据 = **与显隐同一个谓词**（不许两份）：
        ///   · `hdr_clear` ↔ `!State.Filter.IsEmpty`（2026-10-20）—— 它与 `RefreshHeader`
        ///     里那两句 `SetOn` **是同一个谓词**（那边藏、这边挡命中）。
        /// 表里没有的 key = **恒在**（`hdr_back` / `hdr_filters` / `foot_done` / `name_*` / `tab_*`
        /// 都是常显件；⚠️ 2026-10-20 起 **`hdr_clear` 不再是常显件**）。
        /// ⚠️ 加一颗**成组显隐**的按钮，必须把它的名字写进这里 —— 否则又会出现「看不见却能点」。</summary>
        bool KeyLive(string key)
        {
            switch (key)
            {
                // 🆕 **2026-10-20**：`Clear filters` —— **一个筛选都没设时它被藏起来**（`RefreshHeader`
                //   里那两句 `SetOn` 用的就是这一个谓词）⇒ 这里必须**同样**判掉，否则「藏起来了却还能点到」
                //   （静默失败，本仓红线）。判据/推断/为什么走 `SetActive` → `HdrClearX` 上面那一大段。
                case "hdr_clear":
                    return !State.Filter.IsEmpty;
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
        ///   坐标仍是**屏幕绝对 px**（容器在原点、无缩放 ⇒ 两种父级下同一个数）。
        /// 🆕 **2026-10-19（A1170）**：`relativeToParent` —— `true` 时 `x/y/w/h` 一律**相对 `parent` 的局部原点**
        ///   （设计 px、左上原点、y 向下；口径与 `MenuDraw.Local(parent, x1,y1,x2,y2)` **同一条**），
        ///   ⛔ 不是屏幕绝对 px。给「**父件自己会动**」的容器用（拖影根 `card_ghost` —— 判据 → `BuildCardDrag`）。
        ///   默认 `false` = 与改动前**逐位相同**（`Pos(cx, cy)`）⇒ 既有调用点零影响。</summary>
        ImageQuad Img(string key, string sprite, float x, float y, float w, float h, int q,
                      bool keepAspect = false, Transform parent = null, bool relativeToParent = false)
        {
            return Img(key, Ui(sprite), x, y, w, h, q, keepAspect, parent, relativeToParent);
        }

        ImageQuad Img(string key, Texture2D tex, float x, float y, float w, float h, int queue,
                      bool keepAspect = false, Transform parent = null, bool relativeToParent = false)
        {
            if (tex == null) return null;      // 缺图由 `Ui()` 记账（别在这里按 key 再记一次）
            // `keepAspect` = 原版 `Image.m_PreserveAspect`：**按 sprite 自己的比例放进框**（不拉伸）。
            // 🔴 **2026-09-27 加（PA 普查）**：本文件原来**恒 `SetAspect(w/h)`（拉伸）**（全文件无一处等比）
            //   ⇒ 原版 PA=1 的件被我们画成拉伸。
            // 🔴 **2026-10-19（B2）就地订正（铁律 5）**：上面那句原来写「按图自身宽高比放进框、**居中**」，
            //   并说「算法与 `MenuDraw.Rect` **同一条**（内接…中心不动）」—— 两句现在都不准：
            //   uGUI 是**两段**（定框比 `m_Rect`、贴图再按 `padding` 内缩 ⇒ **画心不居中**），
            //   算式/判据全文 → **`Core/CardbackFace.cs`（本仓唯一一份）**，本处与 `MenuDraw.Rect` 都转调它。
            float cx = x + w * 0.5f, cy = y + h * 0.5f;
            if (keepAspect && w > 0f && h > 0f)
            {
                // 🔴 **2026-10-19（B2）就地收口（铁律 5）**：这一段原来内联「按**贴图自己**的比例内接、
                //   居中」—— 那只是 uGUI 的**第一段**。算式/判据全文 → `Core/CardbackFace.cs`（唯一一份）。
                //   ⚠️ **没登记的贴图**（本文件绝大多数调用点都不是卡背）在里面**逐字退化成原来那套**
                //   ⇒ 本函数的既有调用点零变化；`cx/cy` 的偏移也只有登记过的卡背才非 0。
                float fw2, fh2, fox, foy;
                if (CardbackFace.Fit(tex, w, h, out fw2, out fh2, out fox, out foy))
                { w = fw2; h = fh2; cx += fox; cy -= foy; }        // `Pos` 的 y 向上 ⇒ `foy`（向上为正）减
            }
            var q = ImageQuad.Create(parent != null ? parent : Root, tex,
                                     relativeToParent ? new Vector3(U(cx), -U(cy), 0f) : Pos(cx, cy),
                                     h > 0f ? U(h) : 0.01f, new Vector2(0.5f, 0.5f), key);
            if (q != null && h > 0f) q.SetAspect(w / h);
            if (q != null)
            {
                q.SetRenderQueue(queue);
                if (!string.IsNullOrEmpty(key)) _named[key] = q;
            }
            return q;
        }

        // 🔴 **2026-10-18 收口（铁律 5）**：这里原来有一个 `static string TermOr(string key, string fallback)`
        //   （= `Loc.HasEntry(key) ? Loc.T(key) : fallback` 的**取词闸门**，本文件 3 处调用它：
        //   `DragCardsTip` 那句 · `ImportEmpty`/`ImportBadString` 那两句）——
        //   **已删除** —— 理由：① 那 3 条键**早已全部进表**（`Core/Loc.cs` 实读：`MenuDeck/HUD/DragCardsTip` ·
        //   `MenuDeck/Error/{ImportEmpty,ImportBadString}`；连同第三句走的是 `Loc.HasEntry` 三元的
        //   `MenuDeck/Error/ImportNotPersisted`）⇒ 闸门**永远不会走到兜底那一支**；
        //   ② 闸门本质是**静默兜底**，与本工程「**不许静默失败**」冲突 —— 缺键时应当让屏上**明明白白印出键名**
        //   （`Loc.T` 对缺键就回**键名本身**），而不是悄悄退回一串写死的英文/中文。
        //   ⇒ 3 处调用点一并收成**裸 `Loc.T(键)`**（各处都留了「⚠️ 日期 更正」痕迹）。

        /// <summary>摆一段字（居中 pivot）。🆕 **2026-10-19（A1170）**：`relativeToParent` 同 `Img` ——
        /// `true` 时 `x/y/w/h` 相对 `parent` 的局部原点（设计 px、y 向下），⛔ 不是屏幕绝对 px。
        /// 默认 `false` = 与改动前逐位相同 ⇒ 既有调用点零影响。</summary>
        Label Txt(string key, string s, float x, float y, float w, float h, int scale, Color c, int queue,
                  Transform parent = null, bool relativeToParent = false)
        {
            float cy = h > 0f ? y + h * 0.5f : y;
            var l = Label.Create(parent != null ? parent : Root, s,
                                 relativeToParent ? new Vector3(U(x + w * 0.5f), -U(cy), 0f)
                                                  : Pos(x + w * 0.5f, cy),
                                 scale, c, new Vector2(0.5f, 0.5f), key);
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
        //   `trans=2(SpriteSwap)` **3** 颗（本文件要接的那 3 颗；导入弹窗那 2 颗随 T23 删件删掉）·
        //   `trans=1(ColorTint)` 12 颗（走 `m_Colors`，其中三颗页签另有 UGUI `Toggle.graphic` 那条路）·
        //   `trans=0` 14 颗（原版悬停什么都不变 ⇒ **我们也不接**）。
        //   逐颗表与出处 → `资料/普查产出_1003/卡组编辑器_按钮悬停图_普查.md`（块 A/B/C/D）。

        /// <summary>把一颗**已经画好的**按钮接上原版的「悬停 / 按下换图」（`Selectable.m_Transition = 2(SpriteSwap)`）。
        /// 两张高亮图默认由 `WindowButton` 的名字表推（`&lt;常态图&gt;_hover` / `_pressed` + 4+6 条特例），
        /// 逐颗不同的（例：常态图相同的两颗配不同高亮图）用参数显式覆盖。
        /// ⚠️ UGUI 一颗 `Selectable` 只有**一种** transition ⇒ `Bind` 会把色偏兜底关掉（原版 SpriteSwap 那档不叠色偏）。
        /// ⚠️ 只接 `trans=2` 的 —— `trans=1` 的原版**只变色**（那批还没接，见 `RowHoverK` 那段与报告）。
        /// </summary>
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
        /// ⛔ **不要**再调 `Bind`（那是换图那一档；UGUI 一颗 `Selectable` 只有一种 transition）。</summary>
        /// <param name="key">见 <see cref="Hover"/>；抽屉里那几格用它们的**点击 key**（`$name` / `$owned` / `$upgradable`）。</param>
        /// <param name="drawerReg">抽屉那两栏的登记表（`_fltHoverBtns` / `_cosmoHoverBtns`）—— 传 `null` = 登记进
        /// `_hoverBtns`（`ClickOrder` 那一档）。⚠️ **两份表必须分开**：卡牌栏与卡背栏可以同时存在，
        /// 而两边都有 `$owned` 这个 key ⇒ 合成一份会**互相盖掉**（后登记的赢，另一颗永不亮）。</param>
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
        ///   `Shell/WindowButton.cs`（⚠️ 2026-10-19 订正：原写 `Shell/PromptPopup.cs` —— `WindowButton` 2026-10-11 已拆成独立文件，「本轮白名单」指 2026-10-04 那一轮）⇒ 行悬停这一份自己实现，
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

        /// <summary>稀有色条 / 行边框的颜色 —— **转调 `Core/RarityColors`（全工程唯一一份表）**。
        ///
        /// <para>🔴 **2026-10-18（`N102` 收口）：这一份原来抄的是 `CardRarityColorsSO.json`，【SO 用错了】**。
        /// 本窗这一批行就是原版 `UICardInfoItem`（`Deck Editing Menu / … / Deck List drawer / Content` 里
        /// 那颗 + 督军那颗 + 防御卡槽那颗，逐颗现核），而 `bundle_menus_assets_all` 里**全部 15 颗**
        /// `UICardInfoItem` 的 `colorsByRarity` **逐颗都是 `CardUIDisplayColorsSO`**
        /// （pid `-2272266055686004801`）。
        /// `CardRarityColorsSO`（pid `764426219195092032`）的消费侧是
        /// **`CardInBoosterPack.cardRarityColors`**（`Booster Pack Open Window / … / CardInBoosterPack UI 1..5`），
        /// **不是一个用途** ⇒ ⛔ 别把那份表合回来。
        /// 判据全链（谁上色 / 哪两颗 / 哪张表 / 色值 / 15 颗实例现核）+ 两个保留项 → `Core/RarityColors.cs` 的文件头注。</para></summary>
        public static Color RarityTint(string rarity)
        {
            return RarityColors.Tint(rarity);
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
            // 🔴 **2026-10-19（U5 · 第五十五会话批次 7）：映射本体收口到 `Core/ArmyIcons`** ——
            //   那 13 条就是原版 `IconsByArmySO`（`sharedassets0.assets` 对象 422）逐字节解出来的，
            //   判据链（含「资产名 ≠ 类名」那个坑）写在 `Core/ArmyIcons.cs` 文件头。
            //   本函数**只留一层兜底**：认不出阵营（含 `null` / `Neutral`）⇒ 卡组图标。
            //   ⛔ 别把那 13 条 case 抄回这里（「两处写同一条规则 = 迟早不一致」）。
            return ArmyIcons.IconName(faction) ?? "40k_collection_bt_decks";
        }

        /// <summary>页头那颗 `hdr_army` 该贴哪张图 —— **这副卡组所属阵营的徽记**。
        ///
        /// <para>🔴 **判据 = 原版运行期那条链**（不是出厂值；理由与出处写在 `BuildHeader` 那一行上）：
        /// `DeckEditingWindow__FilterToDeck.c:24-28` 把 `EditingDeck.deckArmy` 喂给
        /// `WildcardDisplay.Initialize(army)` ⇒ 图 = `ArmyUtilities.GetArmyIcon(army)`；
        /// **`deckArmy == 0`（还没选督军）时原版那个字面量默认值是 `10` = `CardArmy.Ultramarines`**
        /// （`dump.cs:45640`）⇒ 出厂的 BlackLegion 徽记**在运行期永远走不到**。</para>
        ///
        /// <para>⚠️ 本函数**只答“稳定态”**（= 开窗 / 卡组变了）。原版还有两条**跟着指针走**的换图路径
        /// （卡池某格被指针压住 / 开卡片详情 ⇒ 换成**那张卡**的阵营徽记，
        /// `DeckEditorCollectionDisplay__CheckFocusedArmy.c` · `DeckEditingWindow__OpenCardInformation.c:35`）
        /// —— **本笔没做**，已记进 `资料/普查产出_1017/W_B31_徽记与上限.md` §④。</para></summary>
        public static string DeckArmyIcon(DeckEditorState state)
        {
            var wl = (state == null || state.Deck == null || string.IsNullOrEmpty(state.Deck.WarlordId))
                     ? null : state.Find(state.Deck.WarlordId);
            // ⚠️ 拿不到督军那张卡（卡池里查不到 id）时**也按原版那个默认档**走，别回退成 `FactionIcon(null)`
            //   （那是卡组图标，绝不是这一格该有的东西）。
            return FactionIcon(wl != null && !string.IsNullOrEmpty(wl.Faction) ? wl.Faction : NoArmyFaction);
        }

        /// <summary>原版 `DeckEditingWindow__FilterToDeck.c:24` 那句 `uVar3 = 10` ——
        /// `CardArmy.Ultramarines`（`dump.cs:45640`）：**卡组还没有阵营时**页头徽记显示的那一个。
        /// ⛔ 别改成 `""`/`null`（那会掉进 `FactionIcon` 的兜底 = 卡组图标）。</summary>
        public const string NoArmyFaction = "Ultramarines";

        /// <summary>自检/演示用的一副卡组：能凑合法就凑合法，凑不出就有什么用什么。</summary>
        public static PlayerDeck PlayerDeckForDemo(DeckEditorState state)
        {
            // 🔴 **2026-10-18（双语线 · 波 1 · P1）**：演示卡组名走词条 `MenuDeck/DemoDeckName`
            // 🔶 自己设计（非原版）：演示卡组名的词条键 `MenuDeck/DemoDeckName` · 原版两张表都查不到这条键，键名由调度台裁定自拟 · 调度台 2026-10-18 裁定｜原注：（键名 = 调度台 2026-10-18 裁定自拟；中文列 = 改之前写死的 `复仇者之刃` ⇒ 中文档零变化）。
            //   ⚠️ 它与 `MenuDeck/DefaultDeckName`（空库时替玩家建的那套）**是两条键、两个值**，⛔ 别合并。
            var deck = new PlayerDeck { Name = Loc.T("MenuDeck/DemoDeckName") };
            CardDef warlord = null;
            foreach (var c in state.Pool) if (c.Type == "hero") { warlord = c; break; }
            if (warlord == null) return deck;
            deck.WarlordId = warlord.Id;
            // 🔴 **2026-10-18：防御卡改成与 `工具/gen_prebuilt_decks.py:pick_defensive` 同一规则**
            //    （本阵营 `type == "defence"` 里 **cost 最低 → id 最小**）。
            //    原来取的是「池里第一张 defence」—— **结果随卡池顺序变**，而卡池在
            //    2026-10-14（`601005b`，955 张换 id）与 10-18（`K06`）两次换号里顺序都动过
            //    ⇒ 旧存档里那张 `ASH67` 就是这么写下来的（当时是 `Aspect Shrine`，
            //    换号后同一个 id 变成了 `Hornet`(unit)）。判据与口径 → `资料/预组卡组_原版规格.md` §五之七。
            //    ⚠️ 我们的 `CardDef` **没有 `spellType` 字段** ⇒ **`Type == "defence"` 是唯一等价判据**
            //      （落库前那一句 `dfc.Type != "defence"` 是断言，不是筛选）。
            CardDef dfc = null;
            foreach (var c in state.Pool)
            {
                if (c == null || c.Type != "defence") continue;
                if (!DeckRules.SameFaction(c.Faction, warlord.Faction)) continue;
                if (dfc == null || c.Cost < dfc.Cost
                    || (c.Cost == dfc.Cost && string.CompareOrdinal(c.Id, dfc.Id) < 0)) dfc = c;
            }
            if (dfc != null)
            {
                if (dfc.Type != "defence")
                    UnityEngine.Debug.LogError("[DeckRuntime] 演示卡组的防御卡挑到了非防御卡 `" + dfc.Id + "`（" + dfc.Type + "）"
                                             + " —— 不写进卡组（这不该发生，挑法见上一段注释）");
                else deck.DefensiveId = dfc.Id;
            }
            foreach (var c in state.Pool)
            {
                if (deck.CardIds.Count >= DeckRules.ClassicCards) break;
                if (c.Type != "unit" || !DeckRules.SameFaction(c.Faction, warlord.Faction)) continue;
                for (int i = 0; i < DeckRules.CopyLimit(c.Rarity) && deck.CardIds.Count < DeckRules.ClassicCards; i++)
                    deck.CardIds.Add(c.Id);
            }
            return deck;
        }

        // ==================================================================
        //  🆕 2026-10-26（B9 · W4K06 · 账 `卡组:325`）—— **加不进卡：日志 + 屏幕横幅**
        // ==================================================================
        /// <summary>加不进一张卡时**除了日志还要上屏**（原版两条拦截路径都调 `UIMessageController.ShowError`）。
        ///
        /// <para>**判据（逐条实读）**：
        /// · `d:/2/tools/decomp_full/DeckEditingWindow__OnCardClick.c:29-37` —— 点卡被 `CardDeck.CanAddCard`
        ///   拦下 ⇒ `UIMessageController.ShowError(out msg, 1)`（**左/右键那条路**，我们 = <see cref="TryAddCard"/>）；
        /// · `DeckEditingWindow__CheckCardDrag.c:91-96` —— 起拖被拦下 ⇒ 同一条 `ShowError`（我们 = `BeginCardDragAt`）；
        /// · `UIMessageController__ShowError.c` —— **只有一句转调** `ShowMessage(msg, errorColor, …)`
        ///   ⇒ 屏幕那句就是 `CanAddCard` 的 `out` 串**本身**，原版**没有**任何前缀。
        /// 宿主 = 外壳侧那颗同族实例（`Shell/MessageToast` → `ErrorMessageBanner.MenuPreset`；
        /// 原版主菜单场景里就序列化着一颗，判据 → `Shell/MessageToast.cs` 文件头 ②③）。</para>
        ///
        /// <para>⚠️ **为什么分成两个参数**：日志那句要带我们自己的前缀（`ClickLog` 的「实际触发了什么」
        /// 靠它认路），屏幕那句必须**只有词条本身**（与原版同形）⇒ 别把两者写成同一句。</para>
        ///
        /// <para>⛔ **只挂在这两个出口上** —— 别把 `Say()` 整个改成「凡说必弹」：`Say` 还承担
        /// 「已加入 / 已保存 / 已移出」这类**正常**提示（原版那些走的是页脚，不走 `ShowError`）。</para></summary>
        /// <param name="bannerText">屏幕上那句（= `DeckError` 词条的人话，不带前缀）。</param>
        /// <param name="logPrefix">只加在**日志**那句前面的前缀（`null` / 空 = 不加）。</param>
        void SayError(string bannerText, string logPrefix = null)
        {
            Say((logPrefix ?? "") + bannerText);
            // 屏幕横幅：宿主在壳里（`MessageToast`），**不依赖战斗上下文**；返回 false 时那边已经出过声（⛔ 不静默）
            ErrorMessageBanner.ShowShellError(bannerText);
        }

        // ═══════════ 2026-10-27（B9_W20WIRE）：原版 `Done Highlight` 的 `UIEffectTogglerImage` 接线 ═══════════
        // 判据（`资料/完全复刻/报告/B9_W14FX_特效组件两族.md` §二·3 A）：原版 `bundle_menus_assets_all` 的
        //   `Done Highlight` 那颗 GO 上同时有 `Image` + `UIEffectTogglerImage`（`images[0]` = 同节点那张 Image、
        //   `deactivateOnDisable = 1`），由**祖先**节点上的 `UIEffectTogglerController` 逐帧推。
        UIEffectTogglerController _doneHlCtl;
        UIEffectTogglerImage _doneHlTg;   // 2026-10-27（B9_W34FEED）：那颗 toggler 的句柄 —— 判「它还在不在控制器表里」（`Build()` 重建 `foot_hl` 后会失效）
        bool _doneHlCtlWarned;

        /// <summary>原版 `Done Highlight` 的 `UIEffectTogglerImage` 淡入淡出（替换硬 `SetActive`）。
        /// <para>🔴 2026-10-27（`B9_W34FEED`）：改成「**逐子件喂 + 收不到就出声**」（口径 = `DraftModeWindow.PackScanFeed`，
        /// ⛔ 不另发明第二种）。原因：`UIEffectTogglerImage.Add` 只 `GetComponent`、**不递归**（= 原版 `Image[] images` 的口径），
        /// 而 `MenuDraw.Nine` / `ImageQuad.CreateNineSlice` 返回的是**容器节点**（九宫块全在子件、根上没有 `ImageQuad`）
        /// ⇒ 只喂根节点 `TargetCount == 0`、`Toggle` **静默空转**。今天 `foot_hl` 是 `Img()` 建的**单块** `ImageQuad`
        /// （`Battle/ImageQuad.cs:127` `AddComponent<ImageQuad>()` 加在返回的 GO **自己**身上）⇒ 改前 `TargetCount` 已是 **1**、
        /// 不是 0；本改动是**抗将来变容器**的加固 + 把「挂不上」从静默变成出声。</para>
        /// <para>⚠️ 控制器必须挂在**常驻 active 的祖先**上：`deactivateOnDisable` 会把 `_doneHl` 自己
        /// `SetActive(false)`，控制器若在它身上，淡入永远 tick 不起来（`Toggle` 里那句 `enabled = true`
        /// 也救不了 inactive 的 GO）。🔶 挂在 `_doneHl.transform.parent` 是**我们的选择**、不是原版判据
        /// （原版那级父链没查到，见 B9_W14FX 报告 §三·2）· 调度台 2026-10-27 裁定。</para>
        /// <para>🔶 非运行期（编辑 / 批处理）`UIEffectTogglerController.Toggle` 会强制 `duration = 0`
        /// ⇒ 行为与本来的硬切**等价**（`DurationZeroIsInstant` = 那个组件的既有口径）。</para></summary>
        void ToggleDoneHighlight(bool on)
        {
            if (_doneHl == null) return;
            // 首次 / `Build()` 重建后（它把 `Root` 的子件全销毁重建）⇒ 目标件不在控制器表里就重走接线。
            if (_doneHlCtl == null || _doneHlTg == null || !_doneHlCtl.togglers.Contains(_doneHlTg))
                SetupDoneHighlight();
            if (_doneHlCtl != null) { _doneHlCtl.Toggle(on); return; }
            // 挂不上 / 收不到可淡件 ⇒ **退回硬切**（红线：不许静默失败；出声在 `SetupDoneHighlight` 里）。
            if (_doneHl.gameObject.activeSelf != on) _doneHl.gameObject.SetActive(on);
        }

        // ============================================================ Done 按钮的 Shiny 扫光（账 455 · 实例 F）
        //
        // 【原版怎么算（🥇 机器码真体，⛔ 不是转抄账文）】
        //   `DeckEditingWindow__UpdateDoneButton.c`：
        //     `valid = DeckUtility.ValidateDeck(deck, out err, 1, false)`（与 `Done Highlight` / 保存闸同一个调用）
        //     `if (param_2 != 0) controller.Toggle(valid, 0)`            // 静默那条：立即
        //     `else if (valid) controller.Toggle(true, 0.3f)` + `DOPunchScale`（点击那条：0.3 s 补间）
        //     `else controller.Toggle(false, 0.3f)`
        //   ⇒ **扫光跟着「这副卡组合不合法」走**，不合法就关。
        //   那颗 `Footer/Done` 上的 `UIEffect`(Shiny) + `UIEffectTweener` 的逐字段值（`报告/B9_W5COF_Coffee搬运.md` §2·5/§2·6 实例 F）：
        //     120° / 0.25 / 0.5 / **`m_TransitionColor = (3.4822,3.4822,3.4822,1)`**（5 个实例里唯一不同的那个色）·
        //     `m_Duration 1.0` · **`m_WrapMode 1 (Loop)`**（一直扫）⇒ `WildcardShineLayer.Params.DoneButton`。
        // ⚠️ 原版那个 `duration 0.3` 是喂给 `UIEffectTogglerController` 的补间时长；本类的 `DoUpdate` 是空桩
        //   （`RVA 0x4B33B0`）⇒ 对**画面**没有作用（真正动的是 tweener 自己的 1.0 s + Loop）。⛔ 别把它接成层时长。
        /// <summary>Done 按钮上的扫光层（惰性建；`foot_done` 还没建出来时是 null）。</summary>
        WildcardShineLayer _footShine;

        /// <summary>按「卡组合不合法」开/关 Done 那颗的扫光（`RefreshAll` 里紧挨 `ToggleDoneHighlight` 那一句）。</summary>
        void FootShine(bool on)
        {
            // 材质被 `WindowButton.RefreshGray`（禁用态灰化）换走了 ⇒ **只有该亮的时候**才重挂
            //   （灰着的时候重挂会立刻把灰底盖掉 —— 那颗钮就"看不见禁用"了）。
            if (_footShine != null && !_footShine.StillAttached)
            {
                if (!on) return;
                _footShine = null;
            }
            if (_footShine == null)
            {
                var q = Lookup("foot_done");
                if (q == null) return;          // 还没建到页脚（或本窗没建）⇒ 下一次刷新再来
                _footShine = WildcardShineLayer.AttachQuad(q, WildcardShineLayer.Params.DoneButton);
                if (_footShine == null) return; // shader 取不到（`WildcardShineLayer` 已经出过声了）
            }
            if (_footShine.Enabled == on) return;
            _footShine.SetEnabled(on);
            if (on) _footShine.Play(true);
        }

        /// <summary>自检读口：Done 那颗扫光到哪儿了（`-1` = 层没建出来）；`on` 回填开关态。</summary>
        public float DoneShineRate(out bool on)
        {
            on = _footShine != null && _footShine.Enabled;
            return _footShine != null ? _footShine.Rate : -1f;
        }

        // ---- 2026-10-27（B9_W34FEED）：Done Highlight 接线的加固版（照 `DraftModeWindow.PackScanFeed` 口径）----

        /// <summary>把 `Done Highlight` 接上 `UIEffectTogglerImage`（+ 祖先上的 `UIEffectTogglerController`）。
        /// <para>🔴 **逐子件喂、⛔ 不能只喂根节点**：`Img()` 建的是**单块** `ImageQuad`（就在返回的 GO 自己身上），
        /// 但同族宿主一旦换成 `MenuDraw.Nine` / `ImageQuad.CreateNineSlice`，九宫块全在**子件**、根上**没有** `ImageQuad`
        /// ⇒ `UIEffectTogglerImage.Add` 只 `GetComponent`、不递归（= 原版 `Image[] images` 的口径）⇒ `TargetCount == 0`、`Toggle` 静默空转。
        /// 口径与 `Shell/DraftModeWindow.PackScanFeed` 同一条（⛔ 不另发明）。</para>
        /// <para>⚠️ 控制器必须挂在**常驻 active 的祖先**上：`deactivateOnDisable` 会把 `_doneHl` 自己 `SetActive(false)`，
        /// 控制器若在它身上，淡入永远 tick 不起来。🔶 挂在 `_doneHl.transform.parent` 是**我们的选择**（原版那级父链没查到）· 调度台 2026-10-27 裁定。</para></summary>
        void SetupDoneHighlight()
        {
            _doneHlCtl = null; _doneHlTg = null;
            var host = _doneHl.transform.parent;
            if (host != null)
            {
                var tg = _doneHl.gameObject.GetComponent<UIEffectTogglerImage>();
                if (tg == null) tg = _doneHl.gameObject.AddComponent<UIEffectTogglerImage>();
                int n = DoneHlFeed(tg, _doneHl.gameObject);
                if (n > 0)
                {
                    var ctl = host.GetComponent<UIEffectTogglerController>();
                    if (ctl == null) ctl = host.gameObject.AddComponent<UIEffectTogglerController>();
                    // ⚠️ **不能省**：编辑 / 批处理模式下 `AddComponent` **不触发 `Awake`** ⇒ 控制器那份 `togglers` 恒空。
                    ctl.Rescan();
                    if (ctl.togglers.Contains(tg)) { _doneHlTg = tg; _doneHlCtl = ctl; }
                }
            }
            // ⛔ 不静默：收不到可淡件 / 控制器挂不上 ⇒ 当场出声（退回硬 `SetActive` 由调用方做）。
            if (_doneHlCtl == null && !_doneHlCtlWarned)
            {
                _doneHlCtlWarned = true;
                Debug.LogWarning("[DeckRuntime] `Done Highlight` 接不上 `UIEffectTogglerImage`"
                               + "（`foot_hl` 底下没有 `ImageQuad`，或没有可挂控制器的父节点）"
                               + "⇒ 退回硬 `SetActive`，淡入淡出不生效。");
            }
        }

        /// <summary>把「原版那一张 `Image`」映射成可淡件清单（同 `DraftModeWindow.PackScanFeed` 的逐子件口径）。
        /// 返回收进几件（0 = 一件都没收到，调用方出声）。<para>⚠️ `GetComponentsInChildren` **含自身** ⇒
        /// 今天 `foot_hl` 自己那块 `ImageQuad` 也会被收进来（`TargetCount` 仍 1，行为与改前逐位相同）。</para></summary>
        static int DoneHlFeed(UIEffectTogglerImage tg, GameObject target)
        {
            if (tg == null || target == null) return 0;
            tg.fadeTargets.Clear();
            var quads = target.GetComponentsInChildren<ImageQuad>(true);
            for (int i = 0; i < quads.Length; i++)
                if (quads[i] != null) tg.fadeTargets.Add(quads[i].gameObject);
            tg.RefreshTargets();
            return tg.fadeTargets.Count;
        }
        // ============================================================ 通配符计数条的 Shiny 扫光（原版 `WildcardDisplay ▸ shinyEffect`）
        //
        // 【原版怎么算（一手判据）】
        //   · 那颗 `UIEffect` 挂在 `Deck Editing Menu ▸ Content Area ▸ Header ▸ WIldcard Counter ▸ Background`
        //     （= 本文件 `BuildHeader` 里 `MenuDraw.Nine(...)` 建的那块九宫底板 `hdr_wcbg`）。
        //   · 逐字段真值（2026-10-27 现读）：rotation **120** · width **0.25** · softness **0.5** ·
        //     color **(1,1,1,1)** · filter **6** · glow 0 · reverse 0 · autoPlay 0 · rate 1 ·
        //     **duration 0.75 s** · **wrap 0 (Once)** —— 与收藏窗那颗**逐字段完全相同** ⇒
        //     直接用 `WildcardShineLayer.Params.WildcardDisplay`（⛔ 别新建 139 那套：那是 `Message Container ▸ Content`）。
        //     判据 → `资料/完全复刻/报告/B9_W36SHINEA_扫光断言与宿主.md` §2·4 / §2·5。
        //   · 播放时机 = 原版 `WildcardDisplay__OnEnable`：`if (currentArmy != 0) shinyEffect.Play(resetTime: true);`
        //     （`d:/2/tools/decomp_full/WildcardDisplay__OnEnable.c` 逐句：`if (*(int*)(this+0x50) != 0) {
        //      Coffee_UIEffects_UIEffectTweener__Play(this+0x48, 1, 0); SetCountersText(0); }`）。
        //     而 `OnEnable` 只在计数条**由关变开**那一拍才发 —— 判据链：
        //       ① `DeckEditingWindow__ToggleDisplay.c` 唯一一处 `wildcards(0xd0).SetActive(param)`
        //          （Cards / Deck info 页传 1、Cosmetics 页传 0）；
        //       ② Unity 对**已激活**的 GO 再 `SetActive(true)` **不发** `OnEnable` ⇒ Cards ↔ Deck info
        //          之间来回切**不重播**，只有 Cosmetics → Cards/Deck info 才播；
        //       ③ ♻ 开窗那一拍也不播：原版 `DeckEditingWindow__TryOpen.c` 里 `ToggleDisplay(1)` 排在
        //          `FilterToDeck`（→ `WildcardDisplay__Initialize(wildcards, army)`，它**只写 army、不 Play**）
        //          **之前** ⇒ 那一刻 `currentArmy` 还是出厂 0 ⇒ `OnEnable` 里那道闸是关的。
        //     ⚠️ 本窗没有「Neutral 也扫光」这一档要考虑：原版 `__TryOpen` 对 `deckArmy == 0` 传的是字面量
        //        **10（Ultramarines）** ⇒ Initialize 之后 `currentArmy` 恒非 0。
        //
        // 🔶 自己设计（非原版）：扫光材质**惰性到第一次「由关变开」时才换上**（原版那张 `Image` 的材质从建起就带
        //    这颗 UIEffect）。为什么：本件改动要求**零行数变化**（账里 74 处按 `文件:行号` 引用本文件），
        //    而挂载点 `hdr_wcbg` 在 `BuildHeader`（行号 1693 附近）里 ⇒ 改成按名字惰性取根节点。
        //    可见性影响 = **0**：静止态 `_TransitionRate = 1` ⇒ 掩码 0 ⇒ 两个材质逐像素相同。· 调度台 2026-10-27 裁定
        /// <summary>通配符计数条那颗扫光层（`null` = 还没挂 / 挂不上）。</summary>
        WildcardShineLayer _wcShine;
        /// <summary>`hdr_wcbg` 取不到 / 层挂不上时的那条警告只出一次。</summary>
        bool _wcShineWarned;

        /// <summary>页签切换那一拍（`SetTab` 末句）：把计数条的扫光按原版 `WildcardDisplay.OnEnable` 开/关。
        /// <para>⚠️ 参数 <paramref name="oldTab"/> = **切之前**那一页 —— 它同时答两件事：切前计数条露没露
        /// （`oldTab != 2`）、这次切换是不是「由关变开」。完整的判据链见上面那一段注释。</para></summary>
        void WildcardShineAfterTab(int oldTab)
        {
            if (CardDisplayPage)
            {
                // 切前就露着 ⇒ 原版 `SetActive(true)` 是空操作、不发 `OnEnable` ⇒ 不重播
                //   （`oldTab == 2` = 切前在 Cosmetics 页、整条计数条是关着的）
                if (oldTab != 2) return;
                if (_wcShine == null || !_wcShine.StillAttached)
                {
                    var bar = WildcardBarRoot();
                    if (bar == null)
                    {
                        if (!_wcShineWarned)
                        {
                            _wcShineWarned = true;
                            Debug.LogWarning("[DeckRuntime] 找不到 `hdr_wcbg`（通配符计数条底板）⇒ 那颗 **Shiny 扫光**"
                                           + "不会出现（原版 `WildcardDisplay.OnEnable` 的 `shinyEffect.Play`）。出声，不静默。");
                        }
                        return;
                    }
                    _wcShine = WildcardShineLayer.AttachNine(bar, WildcardShineLayer.Params.WildcardDisplay);
                    if (_wcShine == null)
                    {
                        if (!_wcShineWarned)
                        {
                            _wcShineWarned = true;
                            Debug.LogWarning("[DeckRuntime] `WIldcard Counter ▸ Background` 的扫光层建不出来"
                                           + "（`CardPresentation/WildcardShine` 取不到？）⇒ 那块底板保持原样（无扫光）。"
                                           + "见 `报告/B9_W16SHINE_扫光统一.md`。");
                        }
                        return;
                    }
                }
                _wcShine.Play(true);      // 原版 `UIEffectTweener.Play(resetTime: true)`：从头播一次（Once，0.75 s）
                HookWildcardUpdate();     // 原版 `OnEnable` 的另一半：`Signal.Register<WildcardUpdateSignal>(UpdateCounters)`
                return;
            }
            // Cosmetics 页：原版 `ToggleDisplay(0)` 把整条 `SetActive(false)` ⇒ `OnDisable` 停下扫光
            //   （材质仍在原处 —— 原版关的是 `Behaviour.enabled`，不是换图；⛔ 别改成隐藏/换材质）
            if (_wcShine != null) _wcShine.SetEnabled(false);
            UnhookWildcardUpdate();   // 原版 `OnDisable` 的 `Signal.Unregister`（计数条藏起来 ⇒ 不再跟着库存重播）
        }

        /// <summary>`hdr_wcbg` 的根节点（`MenuDraw.Nine` 建的**容器**：根上**没有** `ImageQuad`，九块全在子件）。
        /// <para>找法 = 直接子件 → `FindDeep`（与本文件 `UiHasQuad` 那三步同源，只是这里不需要 `Lookup`
        /// —— 根不进 `_named`）。</para></summary>
        Transform WildcardBarRoot()
        {
            if (Root == null) return null;
            var t = Root.Find("hdr_wcbg");
            if (t != null) return t;
            var g = FindDeep(Root, "hdr_wcbg");
            return g != null ? g.transform : null;
        }

        /// <summary>自检读口：通配符计数条那颗扫光到哪儿（0 = 带在起点 · 1 = 播完/不亮）；`-1` = 层还没挂。
        /// <para>🔴 `1` 有**两种**含义（「从没播」与「播完了」）⇒ 断「有没有在播」时要与
        /// <see cref="WcShineEnabled"/> **两条并排**（同 `CollectionWindow.WildcardShinePlayingForTest` 的口径）。</para></summary>
        public float WcShineRate { get { return _wcShine != null ? _wcShine.Rate : -1f; } }
        /// <summary>自检读口：那颗扫光现在是开着吗（原版 `Behaviour.enabled`）。没挂上 ⇒ false。</summary>
        public bool WcShineEnabled { get { return _wcShine != null && _wcShine.Enabled; } }

        // ============================================================ 万能卡库存变动 ⇒ 计数条重播扫光（原版 `WildcardDisplay.UpdateCounters`）
        //
        // 【原版（🥇 机器码真体）】`d:/2/tools/decomp_full/WildcardDisplay__UpdateCounters.c`：
        //   `if (shinyEffect(+0x48) != 0) { UIEffectTweener__Play(+0x48, 1, 0); SetCountersText(this, 0); }`
        //   （`+0x48` 为空时原版走那条 `FUN_1803f47a0()` 的**抛异常**支）。
        //   它的**唯一**触发 = `WildcardDisplay__OnEnable.c` 里那三行：
        //   `Signal.Register<WildcardUpdateSignal>(new UIGenericEventCatcher...SourceDelegate(UpdateCounters))`，
        //   而发这条信号的只有 `WildcardInventory__UpdateItemCollection.c`
        //   （`base.UpdateItemCollection(); Signal.Raise(updateSignal);`）；`OnDisable` 里 `Signal.Unregister`。
        //   ⇒ 「万能卡库存重建完 ⇒ 计数条**重播一次扫光** + 刷数字」。
        //
        // 【我们】`Shell/Inventory.cs` 的 `WildcardInventory` 已把那条信号落成 `Updated` 事件（同一句 `Raise`）。
        //   本窗的订阅/退订**与计数条的显隐同一拍**（`WildcardShineAfterTab` 的两支 = 原版 `OnEnable`/`OnDisable`）
        //   ⇒ 比收藏窗那一路更贴原版。
        //   ⚠️ **只做 `Play`、不做 `SetCountersText`**：那 4 个数字 2026-09-28 用户拍板恒定 `99`
        //   （缺口是**发放源**，不是这一跳），见 `Shell/CollectionWindow.cs` `OnWildcardsUpdated` 那一段。
        //   ⚠️ **发放源缺口（不在本文件）**：本工程唯一会动通配符库存的是
        //   `Shell/CardDetailPopup.cs` 的 `CardProgress.TrySpendWildcard`（合成扣 1 张），它**不调**
        //   `Inventories.Wildcards.UpdateItemCollection()` ⇒ 这条信号**在生产路径上今天不会响**
        //   （改动请求 → `报告/B9_W63WC_万能卡计数扫光.md` §五·1）。
        //   ⚠️ 三个小方法在 `Shell/CollectionWindow.cs` 有一份**同形**实现（那两个宿主都要吃同一条静态事件，
        //   而共用件 `Effects/UIEffectTogglerCoffeeEffects.cs` 本波有别的写手 ⇒ 不往那里收口）。
        //   🔶 自己设计（非原版）：信号层用 C# `event`（原版 `Scaffold.Core.Events.Signal`）、
        //   订阅点等价映射到「计数条显隐」那一拍 · 理由：我们没事件总线 · 调度台 2026-10-27 裁定
        bool _wcSignalOn;

        /// <summary>订阅 `WildcardInventory.Updated`（幂等：重复调不会挂两遍）。</summary>
        void HookWildcardUpdate()
        {
            if (_wcSignalOn) return;
            Inventories.Wildcards.Updated += OnWildcardsUpdated;
            _wcSignalOn = true;
        }
        /// <summary>退订（幂等）。`Inventories.Wildcards` 是**静态**单例 ⇒ 不退订 = 窗销毁后还被它按着。</summary>
        void UnhookWildcardUpdate()
        {
            if (!_wcSignalOn) return;
            Inventories.Wildcards.Updated -= OnWildcardsUpdated;
            _wcSignalOn = false;
        }
        /// <summary>`WildcardUpdateSignal` 那一跳（原版 `WildcardDisplay.UpdateCounters` 的等价物）。</summary>
        void OnWildcardsUpdated()
        {
            if (_wcShine == null) return;   // 层没挂上（shader 取不到 / 还没切到过计数条可见的那一页）⇒ 无画面可播
            _wcShine.Play(true);            // 原版 `UIEffectTweener.Play(_, 1, 0)` = `Play(resetTime: true)`
        }
        /// <summary>自检读口：`Updated` 订着吗（原版 = `Signal` 注册着）。</summary>
        public bool WcSignalHookedForTest { get { return _wcSignalOn; } }
    }
}
