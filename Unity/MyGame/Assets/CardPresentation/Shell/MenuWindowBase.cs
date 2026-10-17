// MenuWindowBase.cs — 主菜单「子菜单窗」的**公共外壳**（左栏 + 内容区 + 页签宿主 + 绘图助手）
//
// ============================ 为什么要抽这一层（2026-09-23） ============================
// `Rewards Base Submenu Variant`（日常/锻造厂/战役）与 `Shop Menu Variant`（商店）是**同一个壳**：
// **`Content Area` 的矩形实测完全一样**（`167.17,70.94 → 1920.01,1080.00`）· `Background` 同一套双色渐变 ·
// `Tab Buttons` 同一条左栏（165 宽 · `VLG padTop 120` · 每个键 180 高 · 同一张选中底图与名字条）。
// 商店那一件开工时要再写一遍这些 —— 而 CLAUDE.md §三写着「**两处写同一条规则 = 迟早不一致**」
// ⇒ 收口到这里，`RewardsWindow` 与 `ShopWindow` 都继承它。
//
// ⚠️ **这是一次纯抽取**：常量与算法**一字未改**（出处仍是 `资料/日常_原版规格.md` §一/§二 与
// `资料/主菜单_原版规格.md`），抽完靠 `RewardsScene` 那 265 条断言证明行为没变。
//
// 🔴 **两个窗口的差别只在三处**（各自填，别在基类里写死）：
//   ① `Buttons`（左栏键的图标/文案/字号）—— 日常那套 4 个键、商店那套是自己的页签；
//   ② 窗口参数（`type` / `windowsPlacement` / `closeOnESC`）—— **实测值逐窗不同**（见各自 `Create`）；
//   ③ 红点偏置 `TabBtnSpec.BadgeDy`（日常第 4 键是 `+47.9`、其余 `−27.2`）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{

    /// <summary>原版共用件 `WindowHeaderWithBackButton` 的**唯一一份**建法。四层，**兄弟序照原版**：
    /// `Header Background` → [`Game Mode Icon`] → `Header Background (1)` → `Header Back Button`；
    /// 其中 `Window Title` 挂在**底板** `Header Background` 底下（原版父链 —— 缩进 3 层）。
    ///
    /// <para>🔴 **为什么要收口**：全工程有 **4 扇窗各自把它抄了一遍** —— `TutorialModePopup.BuildHeader` ·
    /// `LiveOpsEventWindow.BuildHeader` · `DailyStreakPopup.BuildHeader` ·
    /// `EnergySinglePlayerOnlyEventWindow.BuildHeader`；建的是**同一个原版 prefab**
    /// （同 sprite `WF_Campaign_Info_Background` 740×167 · 同九宫 `(335,0,395,0)` · 同 `Window Title`
    /// `fs 67.55` + 字距 5 + auto `[18, 67.55]` base 36），**连常量都被抄成了两份**
    /// （`DailyStreakPopup` 与 `LiveOpsEventWindow` 各有一组逐值相同的 `HeaderBorder` / `HeaderTexW,H`）。
    /// CLAUDE.md §三：「两处写同一条规则 = 迟早不一致」。</para>
    ///
    /// <para>⚠️ **四份之间的差异一个都没被抹掉** —— 全变成 <see cref="Spec"/> 上的显式字段
    /// （某一扇有一档 `TitleVAlign` / 某一扇少一刀自适应 / 返回钮的三种接线 / 尖角那一颗的节点名…）。
    /// 逐格对照表、「哪些不一样、每一格怎么处置」→
    /// `资料/普查产出_1018/S5_A866窗头收口.md` §2 / §4。</para>
    ///
    /// <para>⛔ **它放在本文件里**是调度台的指定（A866 那一笔的白名单：优先复用已有的
    /// `Shell/MenuWindowBase.cs`，别为共用件再开一个新文件）。</para></summary>
    public static class WindowHeader
    {
        // ============================================================ 原版常量（`Sprite/*.json` + `menu_dump` 实读）

        /// <summary>根名：3 扇窗是这一颗（`TutorialModePopup` / `DailyStreakPopup` / `EnergySingle…`）。</summary>
        public const string RootNameBack = "Header With Back Button";
        /// <summary>根名：遭遇战 / 排位那一族是这一颗（`LiveOpsEventWindow`）。</summary>
        public const string RootNameGameMode = "Game Mode Header With Back Button";
        public const string PlateName = "Header Background";
        public const string TitleName = "Window Title";
        /// <summary>往左延伸的那颗尖角。⚠️ `menu_dump` 实读就是这个名字（同族四份共用）。</summary>
        public const string WingName = "Header Background (1)";
        public const string BackName = "Header Back Button";
        public const string BackHitName = "BackHit";
        /// <summary>`BackStyle.QuadInOwnNode` 那一档：返回钮的图是具名节点的**子件**，原版叫 `Bg`。</summary>
        public const string BackQuadInNodeName = "Bg";

        /// <summary>原版那 4 颗 `Window Title` 的**出厂字段原文**（逐值相同）：
        /// `m_fontSize 67.55` · `m_characterSpacing 5` · 自适应区间 `[18, 67.55]` · `m_fontSizeBase 36.0`。
        /// <para>🔴 `TitleAutoBasePx` 的判据（A305①）：扫 `bundle_menus_assets_all` 里 `auto[18~67.55]` 那一族
        /// **8 颗全是 `m_fontSizeBase 36.0`**（例 `MonoBehaviour_3324232435684942507.json`，`'Daily Streak'`）。</para></summary>
        public const float TitleFontPx = 67.55f;
        public const float TitleCharSpacing = 5f;
        public const float TitleAutoMinPx = 18f, TitleAutoMaxPx = 67.55f, TitleAutoBasePx = 36f;

        /// <summary>`WF_Campaign_Info_Background`：**740×167 · 九宫 `(335,0,395,0)`**、`m_PixelsPerUnitMultiplier`
        /// 缺省 = 1（⇒ 画出来的角块就是 335 / 395px，两轴都没被挤到 `scX/scY` 那一步，`borderOutPx` 不用单传）。
        /// <para>判据（两处互证，⛔ 不是抄表）：① 本仓实读的 `Sprite/*.json`；② A641 那次全包清点 ——
        /// `bundle_menus_assets_all/MonoBehaviour/` 里**用这张 sprite 的 20 个 `Image` 实例 `m_Type` 全是 1
        /// (`Sliced`)**，没有一处走 `Simple` ⇒ 建这一族**一律 `MenuDraw.Nine`，⛔ 别退回 `Rect`**。</para>
        /// <para>🔴 **2026-10-17（A866）**：这三个值原来被抄成**两份**（`DailyStreakPopup` 与
        /// `LiveOpsEventWindow` 各一组、逐值相同；`EnergySingle…` 还私有一份 `HdrBorder`）—— 已收口到这一处。</para></summary>
        public static readonly Vector4 PlateBorder = new Vector4(335f, 0f, 395f, 0f);
        public const float PlateTexW = 740f, PlateTexH = 167f;

        /// <summary>`Window Title` 的「字距 / 自适应」这两刀怎么下、按什么次序 ——
        /// **四扇窗各是一种，一一对应**（2026-10-17 逐份实读）。
        /// <para>🔴 **次序不是小事**：两刀都**改渲染宽度**，而 `AlignLeftOn` 是「量**当时的** `WorldW`
        /// 再反推位置」⇒ 排在它之后改宽 = 那一行按**旧宽**定位、字整体往左溢出，**且不出声**
        /// （A475 / A492 那两笔账；同族先例 = `Shell/InboxWindow.cs` 的 A471）。</para></summary>
        public enum TitleFit
        {
            /// <summary>只加字距、**不调自适应** —— `DailyStreakPopup` 那一档（今天就是这样，本笔**照旧**）。</summary>
            SpacingOnly,
            /// <summary>先字距、后自适应 —— `TutorialModePopup` / `LiveOpsEventWindow` 那一档。
            /// <para>🔴 **A492（2026-10-14）把这两句对调过**（原文是 `SetAutoFitBox` 在上、`SetCharSpacing` 在下，
            /// 账上的话是「自适应不含字距」）—— 现在是**先字距、后自适应**，与本仓口径一致。</para>
            /// <para>⚠️ **静态上两种次序应收敛到同一结果**：`SetCharSpacing` 尾句 `ForceMeshUpdate()` 会把
            /// `m_fontSize` 复位成 `Clamp(m_fontSizeBase, min, max)`、**带着新字距重跑一次自适应**
            /// （判据 + 逐行注解 → `Battle/Label.cs` 的 `SetCharSpacing` docstring，**静态读 TMP 源码**得出、
            /// **未实跑验证**）⇒ 那次对调是**口径对齐**，不是「改回来一个原本读错的值」。</para>
            /// <para>🔴 **无牙口（如实登记）**：判据里「原版实际收敛到多少」是**空的** ⇒ 这一档**没有断言咬得住**
            /// （两种次序下 `SetAutoFitBox` 之后的 `FontPxNow` / `WorldW` 同值，断不出差别）。
            /// ⛔ 别为了让这条账「有牙口」而自定一个原版值 —— 那是发明判据。
            /// ⚠️ 改本档次序时**必须逐窗改**（另两档是 `SpacingOnly` / `FitBeforeSpacing`，见上）。</para></summary>
            FitAfterSpacing,
            /// <summary>先自适应、后字距 —— `EnergySinglePlayerOnlyEventWindow` 那一档
            /// （它与上一档**静态收敛**，但**是原样保留的差异**，别替它选边）。</summary>
            FitBeforeSpacing,
        }

        /// <summary>返回钮的三种接线 —— **四扇窗共三种，⛔ 别合并**（结构与节点名都不同）。</summary>
        public enum BackStyle
        {
            /// <summary>一张图直接挂顶栏根（名 `Header Back Button`）+ 透明命中区节点 `BackHit`：
            /// `TutorialModePopup`（`BackSwapArt` 空 ⇒ 无换图）与 `LiveOpsEventWindow`（非空 ⇒ 有换图）都走这一档。</summary>
            QuadOnHeader,
            /// <summary>多一层具名节点 `Header Back Button`、图是它的子件 `Bg` ——
            /// `EnergySinglePlayerOnlyEventWindow` 那一档（它自检量的就是 `/Header Back Button` 与 `…/Bg` 两个节点）。</summary>
            QuadInOwnNode,
            /// <summary>图**自己**就是按钮（`WindowButton` 挂在图上，**没有 `BackHit` 节点**）——
            /// `DailyStreakPopup` 那一档。</summary>
            BoundOnQuad,
        }

        /// <summary>建一扇「带返回钮的窗头」要的那些数 —— **每扇窗照抄它自己那一档的原值**，
        /// ⛔ 别为了「统一」而改动（这些差异本身就是原版那一族的几个态）。</summary>
        public class Spec
        {
            /// <summary>根节点名：<see cref="RootNameBack"/>（3 扇）或 <see cref="RootNameGameMode"/>（遭遇战 / 排位族）。</summary>
            public string RootName = RootNameBack;
            /// <summary>顶栏根那一格。⚠️ 各窗自己的 rect **逐值不同**（含 y 的零点），照抄。</summary>
            public PxRect RootRect;
            /// <summary>`Header Background`（底板；`Window Title` 挂在**它**底下）。</summary>
            public PxRect PlateRect;
            /// <summary>`Window Title`。</summary>
            public PxRect TitleRect;
            public string TitleText;
            public TitleFit TitleMode = TitleFit.FitAfterSpacing;
            /// <summary>自适应那两个量（画布 px）—— **按各窗自己那句原式给**：
            /// `TutorialModePopup` 给 `TitleR - TitleL` / `TitleB - TitleT`、
            /// `EnergySinglePlayerOnlyEventWindow` 给 `TitleR.W` / `TitleR.H`、`LiveOpsEventWindow` 给字面量。
            /// ⚠️ 别在共件里从 `TitleRect` 现算 —— 两种写法可能差 1 ulp，而这个差会静默改自适应结果。</summary>
            public float TitleFitW, TitleFitH;
            /// <summary>非空 = 调一次 `MenuDraw.SetVAlign`（`DailyStreakPopup` 是 `Capline`）；
            /// 空 = **不调**（= `Label` 出厂档 `Middle`，另三扇都是这一档）。</summary>
            public Label.VAlign? TitleVAlign;
            /// <summary>`Header Background (1)`（往左延伸的尖角）。</summary>
            public PxRect WingRect;
            /// <summary>尖角那一颗的**节点名**。缺省 <see cref="WindowHeader.WingName"/>（= `"Header Background (1)"`）。
            /// <para>🔴 **2026-10-18（A994① · 铁律 5 订正）**：原文（留痕）写着「⚠️ `LiveOpsEventWindow` 今天沿用
            /// `MenuDraw.Nine` 的缺省名 **`"Nine"`**（见 §5 的差异清单）」—— **那半句已不成立**：`A967` 把
            /// `Shell/LiveOpsEventWindow.cs` 的 `Spec` 里那一行 `WingName = "Nine",` **删掉了**
            /// （原版那颗尖角本名就是 `Header Background (1)`，判据 = 本常量那份 `menu_dump` 实读；
            /// 当年那一行是照**改前的实现行为**（`MenuDraw.Nine` 不传名）抄的，属于**与原版不符**）。
            /// ⇒ **四扇都用共件缺省 `Header Background (1)`，今天没有哪一扇用它覆盖**；
            /// 本字段**留给将来真出现差异的窗**（今天谁写 = 与缺省同值，等于没写）。</para>
            /// <para>⚠️ 断言侧：`Header Background (1)` 那颗由 `Editor/MainMenuScene.cs` 逐窗钉
            /// （教程窗 / 遭遇战（`LiveOpsEventWindow` 家族） / 能源活动窗各一条，期望值写**字面量**、⛔ 不读本字段）。</para></summary>
            public string WingName = WindowHeader.WingName;
            public PxRect BackRect;
            public BackStyle BackButtonStyle = BackStyle.QuadOnHeader;
            /// <summary>返回钮那张图的 `m_PreserveAspect`（三扇是 1、`DailyStreakPopup` 是 **0**）。</summary>
            public bool BackKeepAspect;
            /// <summary>返回钮的**常态图名**（悬停 / 按下那两张由它查表，⛔ 不是贴图）：
            /// 空 = 这一颗**没有换图**（`TutorialModePopup`）；`BoundOnQuad` 那一档也用它（`WindowButton.BindSelf`）。</summary>
            public string BackSwapArt;
            /// <summary>底板与尖角**同一张图**（四扇窗都是 `WF_Campaign_Info_Background`）。
            /// ⚠️ 传**贴图**、不传图名：各窗的取图口（`Tex` / `Art`）各自带 `MissingArt` 记账，收口时
            /// **不许**把那份记账换掉（换掉 = 取不到图时静默不建，而清单上看不见）。</summary>
            public Texture2D PlateTex;
            /// <summary>返回钮那张图（`UI_Button_Menu_Back`）—— 同上，走各窗自己的取图口。</summary>
            public Texture2D BackTex;
            /// <summary>各层的渲染队列（**逐窗不同**，照抄各窗自己的那一档常量）。</summary>
            public int QPlate, QWing, QTitle, QBack, QHit;
            /// <summary>点返回钮做什么。原版那条链 = `WindowHeaderWithBackButton.BackButtonPressed`
            /// → 各窗注册的 `UnityEvent` 回调（多数就是 `Close()`；`DailyStreakPopup` 还要先
            /// `DailyData.StreakAutoCollect()` —— 原版 `Close()` = `LiveOp.TryCollect(() => base.Close())`）。</summary>
            public System.Action OnBack;
        }

        /// <summary>建出来的那几件。**只给需要的窗用**：今天只有 `EnergySinglePlayerOnlyEventWindow`
        /// 要把它们存进字段（`_titleLabel` / `_title` / `_back` / `_backBg`），其余三扇不接返回值。</summary>
        public struct Parts
        {
            public Transform Root;       // 顶栏根
            public Transform Plate;      // `Header Background`（`Game Mode Icon` 那一颗也挂它底下）
            public Label Title;          // `Window Title`
            public Transform BackNode;   // 返回钮的宿主（`QuadInOwnNode` 那一档 = 子节点，其余 = 根）
            public ImageQuad BackQuad;   // 返回钮那张图
            public Transform BackHit;    // `BackHit`（`BoundOnQuad` 那一档**恒 null** —— 它没有命中区节点）
        }

        /// <summary>照 <see cref="Spec"/> 建那一扇窗头。</summary>
        public static Parts WithBackButton(Transform root, Spec s)
        {
            var parts = new Parts();
            var hdr = MenuDraw.Node(root, s.RootName, s.RootRect);
            parts.Root = hdr;

            // ---- `Header Background`（底板）：原版 `Sliced` · 九宫 `(335,0,395,0)` · 贴图 740×167
            //      ⚠️ **先 `Node(具名)`、再把 `Nine` 挂进它**（⛔ 不是直接 `Nine(hdr, …, "Header Background")`
            //      —— 那是「节点自己就是 quad」的另一种形状，原版这一颗**外面还有一层具名节点**）。
            //      F3·D3 红① 那个实现缺陷正是漏了这层名：`Nine` 落到缺省名 `"Nine"` ⇒ 自检读到「节点不在」。
            //      ⚠️ 两种挂法**位置逐位相同**：`MenuDraw.Nine` / `MenuDraw.Text` 都走 `Local(parent, 矩形)`
            //      = 矩形中心 − 父件位置 ⇒ 多插一层同矩形的 `Node` 之后仍等于原来的中心。
            var plate = MenuDraw.Node(hdr, PlateName, s.PlateRect);
            parts.Plate = plate;
            MenuDraw.Nine(plate, s.PlateTex, s.PlateRect, PlateBorder, PlateTexW, PlateTexH, s.QPlate);

            // ---- `Window Title`（原版 hAlign = **Left**，不是居中）
            // 🔴 **2026-10-10（A1191）裁定：这里【故意不走】`MenuDraw.Text` 的 `wrapPx` / `auto*` 形参，⛔ 别把它「收口」成那一档。**
            //    判据 = 上面 `TitleFit` 那条 doc ＋ `MenuDraw.TextCore` 的实现：
            //    · `TextCore` 把自适应整段藏在 `wrapPx > 0f` 那道闸里，而且 **`Text` 的十个形参里根本没有 `charSpacing`**
            //      ⇒ 它**表达不出**本处那三档 —— `FitBeforeSpacing` / `FitAfterSpacing` 要的正是
            //      「`SetAutoFitBox`（改渲染宽度）与 `SetCharSpacing`（改字距）的先后」，`SpacingOnly` 更是**完全不要自适应**。
            //    · ⇒ `A1191` 原来把它记成「`A1173` 刚消灭掉的那种『两处写同一条规则』」—— **框定不成立**：
            //      这是**能力缺口**（`TextCore` 少两维），不是同一件事写了两遍。**与 `A1195` 同一族的裁定**（两条契约语义不同 ⇒ ⛔ 不统一形状）。
            //      ⚠️ 旁证：本仓「手工 `SetAutoFitBox`」是**既有且普遍**的写法（`Deck/DeckRuntime.cs` · `Battle/UnitChatPanel.cs` · …）
            //      —— 那些站各自的原版 `min/max/base` 都不同，规则本身只有 `Label.SetAutoFitBox` **一处**（没有第二份规则）。
            //    ✅ **本件的收口 = 把这条差异写在这里**（就是本段），⛔ 不是把形状统一。
            //    ⚠️ 真要合并，得先给 `MenuDraw.Text` 加「字距 + 次序」两维 —— 那是 `MenuDraw.cs`（**共用件**）的改动，
            //      会牵动所有走它的调用点（`TextCore` 也是 `TextBox` 的内层）⇒ **另立账**，⛔ 别在本件里顺手做。
            var title = MenuDraw.Text(plate, s.TitleRect, s.TitleText, Color.white, TitleName, TitleFontPx, s.QTitle, face: TmpFont.Face.LatinUi);
            parts.Title = title;
            if (title != null)
            {
                // 🔴 **次序不能反**：改**渲染宽度**的那两句必须排在 `AlignLeft` **之前**（判据见 `TitleFit`）。
                //    各窗那一档见 `s.TitleMode` —— 四扇里有三种，**都是原样保留的**。
                float fitW = LayoutSpace.Px(s.TitleFitW), fitH = LayoutSpace.Px(s.TitleFitH);
                switch (s.TitleMode)
                {
                    case TitleFit.FitBeforeSpacing:      // `EnergySingle…` 那一档
                        title.SetAutoFitBox(fitW, fitH, TitleAutoMinPx, TitleAutoMaxPx, TitleAutoBasePx);
                        title.SetCharSpacing(TitleCharSpacing);
                        break;
                    case TitleFit.FitAfterSpacing:       // `TutorialModePopup` / `LiveOpsEventWindow` 那一档
                        title.SetCharSpacing(TitleCharSpacing);
                        title.SetAutoFitBox(fitW, fitH, TitleAutoMinPx, TitleAutoMaxPx, TitleAutoBasePx);
                        break;
                    default:                             // `SpacingOnly` —— `DailyStreakPopup`
                        title.SetCharSpacing(TitleCharSpacing);
                        break;
                }
                // ⛔ 这里**不补** `ForceRelayout()` —— 与 `Shell/InboxWindow.cs` 那处（A471）不同：
                //    `AlignLeftOn` 自己头一句就是 `RefreshBounds()`（`Battle/Label.cs` 的 `AlignLeftOn`），
                //    那一次就按**含字距的** `textBounds` 重量了 `WorldW` ⇒ 再补一刀只是把同一件事做第二遍。
                //    ⚠️ 一旦哪一天把对齐挪到别处、而那里**不是** `AlignLeftOn`，这一刀就必须补回来。
                MenuDraw.AlignLeft(title, s.TitleRect);
                // ⚠️ `SetVAlign` 排在 `AlignLeft` **之后** —— 与 `DailyStreakPopup` 原来的次序逐句相同
                //    （两件事互不干涉：一个只动横向、一个只动纵向；`MenuDraw.AlignLeft` / `SetVAlign` 都 null 安全）。
                if (s.TitleVAlign.HasValue) MenuDraw.SetVAlign(title, s.TitleVAlign.Value, s.TitleRect);
            }

            // ---- `Header Background (1)`（往左延伸的尖角）
            MenuDraw.Nine(hdr, s.PlateTex, s.WingRect, PlateBorder, PlateTexW, PlateTexH,
                          s.QWing, null, true, s.WingName);

            // ---- `Header Back Button`（+ 它的命中区）
            var backHost = hdr;
            ImageQuad backQuad;
            if (s.BackButtonStyle == BackStyle.QuadInOwnNode)
            {
                backHost = MenuDraw.Node(hdr, BackName, s.BackRect);
                backQuad = MenuDraw.Rect(backHost, s.BackTex, s.BackRect, BackQuadInNodeName, s.QBack,
                                         null, s.BackKeepAspect);
            }
            else
            {
                backQuad = MenuDraw.Rect(hdr, s.BackTex, s.BackRect, BackName, s.QBack, null, s.BackKeepAspect);
            }
            parts.BackNode = backHost;
            parts.BackQuad = backQuad;

            if (s.BackButtonStyle == BackStyle.BoundOnQuad)
            {
                // 原版这一颗是 SpriteSwap（A17）：图**自己**就是按钮，**没有** `BackHit` 节点。
                if (backQuad != null)
                {
                    var wb = backQuad.gameObject.AddComponent<WindowButton>();
                    wb.onClick = s.OnBack;
                    wb.BindSelf(s.BackSwapArt);
                }
            }
            else
            {
                // 🔴 **`BackSwapArt` 为空时 `target` 也必须传 `null`** —— 只传 `target` 不传 `art`，
                //    `WindowButton.Bind` 会按 `HoverNameFor(null) = null` 去查一张并不存在的悬停图
                //    （在 `MissingSwapArt` 里记一条假账），并把 `tintOnHover` 关掉 ⇒ **这个按钮的悬停色偏没了**
                //    （`TutorialModePopup` 那一档今天就是「没有换图、靠色偏」）。
                parts.BackHit = MenuDraw.Hit(backHost, BackHitName, s.BackRect, s.QHit, s.OnBack,
                                             string.IsNullOrEmpty(s.BackSwapArt) ? null : backQuad,
                                             s.BackSwapArt);
            }
            return parts;
        }
    }
}
