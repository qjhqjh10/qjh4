// InboxWindow.cs — 「日常」第 2 层：收件箱（原版 `Inbox Menu`）
//
// ============================ 出处（唯一正本） ============================
// `资料/日常_原版规格.md` §六（层 × 参数表）· `资料/日常_调用链_三窗.md` §三（调用链 + 空态判据；⚠️ 更正：原来指 `资料/日常_调用链_Inbox.md`，2026-10-10 已并入）。
// **类名照原版**：`InboxWindow : BaseInboxWindow : GameWindow`（MB `-4166270025616161998`）。
// prefab 根 pid `-4892976514573368526`。
//
// **窗口参数（实证）**：`type=1(Popup)` · `windowsPlacement=15` · `closeOnESC=1` · `updateNavPanel=1`。
//
// **矩形出处**：`工具/menu_rect.py bundle_menus_assets_all "Inbox Menu" --depth 9 --relative` **机械走链**算出。
//
// ---- 🔴 空态（本层要做的就是这个）----
// `DF:Everguild.LiveOps.InboxWindow__Open.c`：从 handler 取 `Messages`；
// **空 ⇒ 开 `noNewsWarning`、关 `MessageDisplay`**（`:49`）；非空 ⇒ `MessageDisplay.Initialize` + `MessageList.Initialize`。
// 原版 `Message List` 那条 **出厂就是 0 个子件**（结构/条目 prefab 在 `bundle_menus_assets_all/GameObject/Message Container.json`
// —— 由服务端下发的事件数据决定，单机拿不到）⇒ **我们照空态建**。
//
// ---- ✅ 门（主菜单顶栏的 `InboxBtn`）----
// 开窗**只有一条路**：`InboxBtn` 上的 **`OpenWindowButton`**（`windowToOpenPrefab.m_AssetGUID` 已实证
// 指向 `Inbox Menu` 根 pid）。**点名 `InboxWindow` 的代码一处都没有** —— 原版是 Addressables 加载 + 虚函数派发。
//
// ---- ⚠️ 我们挑的 / 不建的 ----
//   · **`Reset Button`：✅ 2026-10-18 起建壳、并由 `Build()` 末尾 `SetActive(false)` 关着**（照原版运行态）——
//     它出厂就 `active`，但**是死的** —— `m_OnClick` 持久调用表**是空的**、
//     全量反编译里 `DebugReset` **零调用者**、而且 `Open()` **首句**把它 `SetActive(false)`（`InboxWindow__Open.c:28-29`；
//     另见 `资料/日常_调用链_三窗.md` §三·B）。🔴 **2026-10-19（批次 D）就地订正（铁律 5）**：这里原来写「**不建**」，
//     已过期（实现建了壳并隐藏；`RewardsScene` 那条断言也已改成「建了但关着」）。
//     ⛔ 别改成 `SetActive(true)` —— 那会多出一颗原版运行时看不到的钮。
//   · **`Message Display` 的正文几件（`Header`/`Message`）不建**；**`Claim Button` ✅ 2026-10-18 起建壳**
//     （见 `BuildMessageDisplay`；空态下整块 `Message Display` 是关的 ⇒ 平时看不见）。正文内容由**消息对象**驱动，
//     单机没有消息。**框架照建、内容缺就说**（红线：不许静默失败）。
//   · **列表条目（`Message Container`）**：✅ **2026-10-08（A182）建了**（几何/字号/色/对齐逐条照 dump）——
//     ⚠️ **点击那半仍未做**：原版点条目 = 右侧 `Message Display` 显示正文，而正文那几件没建
//     ⇒ 命中区上的 `onClick` **如实出声**说「没有画面变化」（⛔ 不挂空 lambda）。
//     ✅ **2026-10-09（A273）**：**悬停换图已补**（原版那颗 `Selectable` 是 `trans=2` SpriteSwap：
//     `40K_settings_button` → `40K_settings_button_hover`）。详见 `BuildRow` 与
//     `ArtRowHover` / `QRowHit` 的注释 —— 那两处的坑是「显式传悬停图」与「命中区档要高过吸收层」。
//   · **红点**：原版 `Inbox.CheckNotification` = **未读条数**（`Repeat(new InboxBadge(), Count(!IsRead))`）。
//     单机没有消息 ⇒ 未读 = 0 ⇒ **红点 alpha 0**（见 `MainMenuRuntime` 里那一段）。
//
// ---- 🔴 列表的视口 / 裁切 / 滚动（**2026-10-08（A182）建的**）----
// 原版 `Inbox Menu/Content/Message List` 底下是三层：`ScrollRect`（**h=0 v=1** Elastic，活的）
// → **`Viewport`(RectMask2D `m_Softness=(0,25)` `m_Padding=(0,0,0,0)`)** → `Content`(VLG, spacing 7, reverse=1)；
// 三层同矩形 99.57,211.98 → 728.28,990.77（`menu_dump … "Inbox Menu" --depth 6` 实读；
// 掩码字段 = `d:/4/_tmp_view/q1_rm2d.txt:250-251`）。**本件之前这三层一层都没建**
// （连 `Viewport` 节点都没有、全文件 0 处 `Clip`）。现在按上面那些字段建全，见 `BuildMessageList`。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`Inbox Menu` —— 收件箱。单机没有服务器 ⇒ **只做空态**（原版没消息时也是这个样子）。</summary>
    public class InboxWindow : GameWindow
    {
        // ---------------- 窗口根（正本 §六 表 #1~#2）
        public static readonly PxRect Shade = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        public static readonly PxRect Content = new PxRect(72.07f, 40f, 1847.93f, 1080f);
        public static readonly PxRect RedBg = new PxRect(76.09f, 36.80f, 1856.86f, 1068.04f);
        public static readonly PxRect Title = new PxRect(121.47f, 115f, 371.47f, 175f);
        public static readonly PxRect MsgList = new PxRect(99.57f, 211.98f, 728.28f, 990.77f);
        public static readonly PxRect MsgDisplay = new PxRect(735.82f, 93.91f, 1795.59f, 990.78f);
        public static readonly PxRect MdTitle = new PxRect(791.02f, 132.13f, 1750.79f, 192.55f);
        public static readonly PxRect MdScrollbar = new PxRect(1755.59f, 214.87f, 1775.59f, 973.78f);
        public static readonly PxRect CloseBtn = new PxRect(1785.54f, 33.03f, 1859.92f, 108.63f);
        public static readonly PxRect CloseBg = new PxRect(1793.69f, 41.01f, 1850.55f, 99.13f);
        public static readonly PxRect NoNews = new PxRect(249.66f, 520f, 1670.34f, 600f);
        // ---- 🆕 **2026-10-18（音效线转来的界面缺口）**：`Reset Button` 与 `Claim Button` 的矩形 ----
        // 判据 = `工具/menu_dump.py bundle_menus_assets_all "Inbox Menu" --depth 9` 的树（读数逐字抄）：
        //   · `Inbox Menu/Content/Reset Button`            = **(399,126) 142×71** ⇒ 399..541 / 126..197
        //   · `…/Reset Button/Button Text`                 = **(407,133) 125×57** ⇒ 407..532 / 133..190（'DEBUG RESET'）
        //   · `…/Message Display/Content/Scroll View/Viewport/Content/Claim Button`（容器） = **(306,883) 959×216**
        //   · `…/Claim Button`（里那颗钮）                  = **(663,910) 245×68** ⇒ 663..908 / 910..978
        //   · `…/Claim Button/Button Text`                 = **(675,917) 221×54** ⇒ 675..896 / 917..971（'Claim'）
        // ⚠️ 树里 `Viewport` 那几层的框是 **0×0 / 2×0**（布局组跑之前的作者态）——见 `ClaimChain*` 那几个的注释。
        public static readonly PxRect ResetBtn  = new PxRect(399f, 126f, 541f, 197f);
        public static readonly PxRect ResetTx   = new PxRect(407f, 133f, 532f, 190f);
        public static readonly PxRect MdContent = new PxRect(786f, 215f, 1776f, 991f);
        // 🔴 **2026-10-25（N17）就地订正（铁律 5）**：这三格原来写 `ClaimBox=(306,883,1265,1099)` /
        //   `ClaimBtn=(663,910,908,978)` / `ClaimTx=(675,917,896,971)` —— **整块落在 `Message Display`
        //   的下沿之外**（display 是 93.91..990.78），也就是说「建出来也画在面板外面」。
        //   错因：照抄了 `menu_dump` 里那几格 y（1246/1273.6）**减去父容器 `Content` 的 y**，
        //   而那颗 `Content` 自身是 **`0×0` 视口下的模板位**（y 从 990.78 往下排）⇒ 减出来的不是真位置。
        //   现在按**原版 VLG 的排布规则**重算：`Claim Button`(容器) 958.86×216.21 **贴 `Content` 底**，
        //   钮在容器里**水平居中、距顶 27.58**，`Button Text` 距容器 (369.63, 32.63)-(590.4, 90.13)，
        //   `Claim reward warning` 在钮**下方** 103.1 处（判据 = 同一棵树里那四格的**相对偏移**）。
        //   ⚠️ 这是「按布局规则算」，不是抄 rect（原版 rect 是布局跑之前的模板位）。
        public static readonly PxRect ClaimBox  = new PxRect(306f, 774.8f, 1265f, 991f);
        public static readonly PxRect ClaimBtn  = new PxRect(662.9f, 802.4f, 907.9f, 870.0f);
        public static readonly PxRect ClaimTx   = new PxRect(675.6f, 807.4f, 896.4f, 864.9f);
        /// <summary>🆕 **2026-10-25（N17）**：正文那三件（原版 `MessageDisplay` 的 `headerImage` /
        /// `body` / `footerImage`）。
        /// <para>判据（**本件亲跑** `工具/menu_dump.py bundle_menus_assets_all "Inbox Menu" --depth 9 --md --no-sprite`）：
        /// `Message` = 字号 **45.0** · 基准 **45.0** · **对齐 `Left/Top`** · **折行 1** · 色 `(1,1,1,1)` · **字距 1** ·
        /// 宽 **974.31**；`Header` / `Footer` = `Image` **`preserveAspect`**、各 **972.80×243.20**。
        /// ⚠️ 那三格的 y 是**模板位**（`Content` 的 VLG 跑之前），实高由 `ContentSizeFitter(PreferredSize)` 撑
        /// ⇒ 我们按 `Content` 的**可见框**(786,215→1776,991) 自己排：正文从顶往下、`Claim Button` 贴底。
        /// ⚠️ 本地两条出厂消息的 `Header/Footer` **都没有图**（`LiveOpsLocalAsset` 那两张没导入）⇒
        /// 两件**建着但关**（= 原版 `HasAsset` 假那一支 `SetActive(false)`），⛔ 不是省略。</para></summary>
        public static readonly PxRect MdHeader = new PxRect(794.6f, 220f, 1767.4f, 463.2f);
        public static readonly PxRect MdBody   = new PxRect(794.4f, 220f, 1768.7f, 768.8f);
        public static readonly PxRect MdFooter = new PxRect(794.6f, 747.8f, 1767.4f, 991f);
        /// <summary>`Claim reward warning`：宽 958.86、启 `Center/Top`、**折行 1**、字号 **37**、**字距 1**、
        /// 位置 = 钮**下方** 103.1（同一棵树里的相对偏移）。原版文案照 prefab 原文：
        /// 'You will be able to claim your purchase once the content is released.\nStay tuned!'</summary>
        public static readonly PxRect MdWarn = new PxRect(306f, 877.9f, 1265f, 967.9f);
        public const float MdBodyFont = 45f, MdBodyAutoBase = 45f, MdBodyCharSpacing = 1f;
        public const float MdWarnFont = 37f, MdWarnCharSpacing = 1f;
        public const string TxtRewardWarning =
            "You will be able to claim your purchase once the content is released.\nStay tuned!";

        public const string ArtRedBg = "UI_Deck_Information_Back";
        public const string ArtDisplayBg = "UI_Deck_Information_submenu_Back";
        /// <summary>⚠️ 工程里的切片名是**下划线**版（导入器把空格换成下划线）。</summary>
        public const string ArtScrollbar = "40k_menu_scroll_bar_bg";
        public const string ArtCloseBg = "UI_Button_Round_background";
        public const string ArtCloseIcon = "40k_general_bt_yellow";
        public const string ArtCloseX = "40k_general_bt_yellow_close";
        /// <summary>🔴 **2026-10-18（A1053）**：关窗钮那两颗子件（`Background` 黄面 / `Icon` 叉，同矩形
        /// `CloseBg`）**自己的** `m_RaycastPadding`（原版实读 `(-20)⁴`；L,B,R,T · **负 = 外扩**）
        /// ⇒ 命中区 = 子件矩形外扩 20 = **96.86 × 98.13**（⛔ 不是子件裸矩形 56.86×58.13、
        /// 也不是根矩形 `CloseBtn` 的 74.38×75.60）。算式只走 `MenuDraw.PaddedRect`；
        /// 口径 → `资料/普查产出_第四会话/普查_全仓命中区与关闭键族.md` §〇-1。</summary>
        static readonly Vector4 ClosePad = new Vector4(-20f, -20f, -20f, -20f);

        // ============================================================ 🔴 消息列表的视口 / 裁切 / 滚动
        // **2026-10-08（A182）建的**。判据（都是直读原版）：
        //   · 结构（`menu_dump.py bundle_menus_assets_all "Inbox Menu" --depth 6` 实读）：
        //     `Content` → **`Message List`(`ScrollRect,MessageList` · h=0 **v=1** mode=1(Elastic)
        //     · inertia=1 · elasticity 0.1 · decel 0.135)** → **`Viewport`(`RectMask2D`)** →
        //     `Content`(`VerticalLayoutGroup` + `CSF v:MinSize=0` · spacing **7** · align **0(UpperLeft)** ·
        //     **`reverse=1`**)；
        //   · 视口字段（逐处表 `d:/4/_tmp_view/q1_rm2d.txt:250-251`，路径
        //     `Inbox Menu/Content/Message List/Viewport`）= `soft=(0,25) pad=(0.0,0.0,0.0,0.0) en=1`；
        //   · 条目模板 = `bundle_menus_assets_all/GameObject/Message Container`（下面那组 `Row*` 常量）。
        // ⚠️ 同窗 `Message Display/Content/Scroll View/Viewport` 那颗掩码的 **`m_Enabled = 0`**
        //    （原版自己就关着）⇒ 那是**正文**那条路，不在本件范围（我们正文几件也没建）。
        /// <summary>原版 `RectMask2D.m_Softness` = **(0,25)**（x 管左右 · y 管上下 ⇒ **只上下渐隐**）。
        /// ⚠️ **不是 `(25,0)`** —— 这条列表是**纵向**滚的，渐隐带在**上下**两条边上（写反了会竖切）。
        /// 🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2）：类型从 `Vector2` 改成 `Vector2Int`** ——
        ///   它现在**就是**喂给 `ViewportClip.softness` 的那个值，而原版那个字段的类型逐字是
        ///   `RectMask2D.m_Softness: Vector2Int`（判据 → `Shell/ViewportClip.cs` 的字段注释）。</summary>
        public static readonly Vector2Int ListSoft = new Vector2Int(0, 25);
        /// <summary>原版 `RectMask2D.m_Padding` = **(0,0,0,0)**（UGUI 的 `(L,B,R,T)`；正 = 缩小）。</summary>
        public static readonly Vector4 ListPad = Vector4.zero;

        /// <summary>`Message List/Viewport/Content` 的 `VerticalLayoutGroup`：**spacing = 7**。</summary>
        public const float RowSpacing = 7f;
        /// <summary>条目模板 `Message Container` 的尺寸（原版 `menu_rect` 实读 —— 宽**正好等于**视口宽 628.71）。</summary>
        public const float RowW = 628.71f, RowH = 140.7f;
        public static float RowPitch { get { return RowH + RowSpacing; } }

        /// <summary>条目内四件（**条目局部坐标**，原点 = 条目左上角；出处 = `menu_dump` 的
        /// `Message Container` 那一棵：`Content` / `Title` / `Date` / `New`）。</summary>
        public static readonly PxRect RowBg = new PxRect(0f, 0f, RowW, RowH);
        public static readonly PxRect RowTitle = new PxRect(20f, 20.3f, 608.7f, 83f);
        public static readonly PxRect RowDate = new PxRect(20f, 85.5f, 418.7f, 125.5f);
        public static readonly PxRect RowNew = new PxRect(418.7f, 83f, 608.7f, 128f);
        /// <summary>条目底图（原版那件 `Image` 是 `trans=2(SpriteSwap)`：常态 `40K_settings_button`
        /// → 悬停 `40K_settings_button_hover`；色 `(1,0.572,0,1)`）。</summary>
        public const string ArtRowBg = "40K_settings_button";
        /// <summary>🆕 **2026-10-25（N17）**：条目底图的**选中图**。判据 = 原版 `MessageContainer` 的两个字段
        /// `normalMessageSprite` / `selectedMessageSprite`，在 `Refresh()` / `ToggleSelection(bool)` 里二选一
        /// （`MessageContainer.cs:112-128`：`background.sprite = selected ? selectedMessageSprite : normalMessageSprite`）；
        /// 两张图的真身 = `40K_settings_button` / **`40K_settings_button_selected`**（账 `社交_聊天.tsv` 那一轮的实读；
        /// 本地 `Resources/Art/ui_menu/` 两张都在）。
        /// ⚠️ `WindowButton.HoverNames` 里也有一条 `40K_settings_button → …_selected`（`ChatPanel` 页签那处），
        /// ⛔ 别靠表推 —— 这里是**选中态**、与「悬停图」无关。</summary>
        public const string ArtRowSelected = "40K_settings_button_selected";
        /// <summary>🆕 **2026-10-09（A273）**：条目底图的**悬停图**。判据 = 原版那颗
        /// `Message Container/Content` 上的 `Selectable` 原文
        /// （`python 工具/menu_dump.py bundle_menus_assets_all "Message Container" --depth 4` 实读：
        /// `40K_settings_button 168×156 否 | Simple (1,0.572,0,1) | **trans=2** target=5058986616577207217
        /// interactable=1 | **HL=40K_settings_button_hover** P=40K_settings_button_pressed`）。
        /// 🔴 **必须显式传它** —— ⛔ 不能只写 `Bind(…, ArtRowBg)` 让 `WindowButton` 查表推名字：
        /// `WindowButton.HoverNames` 里有一条 `40K_settings_button → 40K_settings_button_selected`
        /// 的逐颗覆盖（那条来自 `ChatPanel` 的页签 `Toggle`，注释写着「⚠️ 不是 `_hover`」）——
        /// **同一张常态图在不同 prefab 上配的高亮图不一样**（那处是 `_selected`、这一处是 `_hover`）
        /// ⇒ 靠表推会**静默换错一张图**（画面上是「悬停后底图变了个不认识的亮度」）。</summary>
        public const string ArtRowHover = "40K_settings_button_hover";
        /// <summary>按下图 = 原版那颗 `m_SpriteState.m_PressedSprite`（`40K_settings_button_pressed`）。
        /// ⚠️ **这张图本地没有**（`Resources/Art/ui_menu/` 里只有 `40K_settings_button` / `_hover` / `_selected`
        /// 三张）⇒ `WindowButton.Bind` 会把它记进 `MissingPressedArt` —— 那一档按既有口径
        /// **只出声、不当缺点断**（原版那 1276 颗里「悬停图空 ⇔ 按下图空」实测 0 处不一致，
        /// 而我们的 `Press()` 取不到按下图时**退回高亮图**，所以按下仍有画面变化）。判据见那张表的注释。</summary>
        public const string ArtRowPressed = "40K_settings_button_pressed";
        /// <summary>🆕 **2026-10-09（A273）**：条目**命中区**的档。
        /// 🔴 **它必须高于本窗的吸收层**：`MenuDraw.Absorb` 给吸收层的档 = `qContentMin − 1`
        /// = `QOverlay − 1` = **3013**，而条目底图只是 `QPanel`(3006) ——
        /// `PointerLayer.HitButton` 挑赢家是**队列大的先**（同队列再比 z）⇒ 挂在 `QPanel` 上
        /// 会被吸收层**整个盖掉**（悬停在条目上命中的是吸收层，换图**永远不会发生** = 假实现）。
        /// 本窗既有的内容命中区（`Generic Close Button Orange`）就在 `QOverlay`，口径同它一份。</summary>
        public const int QRowHit = QOverlay;
        public const float RowTitleFont = 50f, RowDateFont = 40f, RowNewFont = 40f;
        public const float RowTitleAutoMin = 18f, RowDateAutoMin = 18f, RowNewAutoMin = 18f;
        /// <summary>🆕 **2026-10-12（A459）**：行模板那三颗 TMP 的 **`m_fontSizeBase`** —— **三颗都是 `41.0`**
        /// （⛔ 别按 `Font` 那三档推，它们各不相同：50 / 40 / 40）。
        /// <para>判据（**本件亲跑，读数逐字抄自输出**）：
        /// `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Message Container" --depth 8 --md --no-sprite`
        /// 的三行原文 —— `Title`「字号=50.0 **基准=41.0** auto[18.0~50.0]」·
        /// `Date`「字号=40.0 **基准=41.0** auto[18.0~40.0]」·
        /// `New`「字号=40.0 **基准=41.0** auto[18.0~40.0]」
        /// （🔴 行模板**不在 `Inbox Menu` 那棵树里**，是另一个 prefab —— 按 `Inbox Menu` 找会白跑一次）。
        /// ⚠️ **`m_fontSizeMax` 那三档（50/40/40）我们早就是等价的**（`MenuDraw.Text` 的 `autoMaxPx &lt;= 0`
        /// 会退回 `fontPx`）⇒ **不用传、别动**（A459 只补 base 这一项）。
        /// ⚠️ 影响面（如实说）：`autoBasePx` 按 `MenuDraw.Text` 的头注释只改自适应的**二分起点**、
        /// 终点两侧都收敛 ⇒ 渲染差 ≤ 0.05 fontSize 单位。按铁律 11 仍要补（「影响小」只决定先后，不决定做不做）。</para></summary>
        public const float RowTitleAutoBase = 41f, RowDateAutoBase = 41f, RowNewAutoBase = 41f;
        /// <summary>🆕 **2026-10-12（A468）**：本窗**另外三颗** TMP 的自适应档 —— 原来这三处
        /// **一个自适应都没接**（`MenuDraw.Text(...)` 不传 `autoMinPx`），而原版**三颗都开着**
        /// （`menu_dump` 印出 `auto[…]` 就说明 `m_enableAutoSizing` 为真 —— `工具/menu_dump.py:607` 那一列
        /// **只在开着时才打**）。三颗的 `m_fontSizeBase` **都是 36.0**（= TMP 序列化默认值 ⇒ 原版没显式设过）。
        /// <para>判据（**本件亲跑**，读数逐字抄自输出 ——
        /// `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Inbox Menu" --depth 6 --md --no-sprite`）：
        ///   · `Inbox Menu/Content/Title` = `'Inbox' 字号=48.0 **基准=36.0** auto[**18.0**~48.0] 对齐=Left/Middle 折行=0`；
        ///   · `…/Content/Message Display/Title` = `'WELCOME TO WARPFORGE CLOSED ALPHA!' 字号=58.0 **基准=36.0**
        ///     auto[**10.0**~58.0] 对齐=Left/Middle 折行=0`；
        ///   · `…/Content/No News Warning` = `'Game announcements will be displayed here' 字号=50.0 **基准=36.0**
        ///     auto[**12.0**~50.0] 对齐=Center/Middle 折行=0 色=(1,1,1,1) 字距=2`。</para>
        /// <para>⚠️ **三颗的 `min` 各不相同**（18 / 10 / 12）—— ⛔ 别按窗统一挑一个数。
        /// ⚠️ **`autoMaxPx` 三处都不传**：原版那三档 `m_fontSizeMax`（**48 / 58 / 50**）**逐颗等于**我们传的
        /// `fontPx`，而 `MenuDraw.Text` 在 `autoMaxPx &lt;= 0` 时**退回 `fontPx`** ⇒ **本来就等价**
        /// （同 `RowTitleAutoBase` 那条的口径；A333 单列那个形参只在两者**不等**时才必须传）。</para>
        /// <para>🔴 同族的 `折行 = 0` 见 `RowText`（`SetAutoFitBox` 会**无条件**开折行 ⇒ 这三处也要还原）。</para></summary>
        public const float TitleAutoMin = 18f, MdTitleAutoMin = 10f, NoNewsAutoMin = 12f;
        /// <summary>`m_fontSizeBase` —— 原版那三颗**都是 36.0**（TMP 出厂默认值，见上一条的判据行）。</summary>
        public const float WindowLabelAutoBase = 36f;
        /// <summary>🆕 **2026-10-12（A471）**：`Content/No News Warning` 的**字距** = 原版 `m_characterSpacing = **2**`
        /// （我们原来**没设** ⇒ 0）。
        /// <para>判据（**本件亲跑**，读数逐字抄自输出 ——
        /// `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Inbox Menu" --depth 6 --md --no-sprite`）：
        /// `Inbox Menu/Content/No News Warning` = `'Game announcements will be displayed here' 字号=50.0 基准=36.0
        /// auto[12.0~50.0] 对齐=Center/Middle 折行=0 色=(1,1,1,1) **字距=2**`。
        /// ⚠️ 那一列**就是** `m_characterSpacing` 本身（`工具/menu_dump.py:612-613` 原样打印，值为 `0` 时不印）
        /// —— ⛔ 不是我们自己的常量。</para>
        /// <para>走**现成的口** `Label.SetCharSpacing`（`Battle/Label.cs` 的 `SetCharSpacing`，**原样传、不换算**；
        /// 点阵后端没有「字距」这回事 ⇒ 它会 `Debug.Log` 出声，不静默）。
        /// ⚠️ 这颗是空态下**唯一画出来的那颗字**（`05_收件箱_空态.png` 那张）⇒ 观感可见。</para></summary>
        public const float NoNewsCharSpacing = 2f;
        public static readonly Color RowBgTint = new Color(1f, 0.572f, 0f, 1f);
        public static readonly Color RowTitleColor = new Color(0.808f, 0.808f, 0.808f, 1f);
        public static readonly Color RowDateColor = new Color(0.953f, 0.663f, 0.404f, 1f);
        public static readonly Color RowNewColor = new Color(0.996f, 0.745f, 0.314f, 1f);
        /// <summary>`New!` 的文案 = **prefab 里的 TMP 原文**（原版是 `Localize` 词条
        /// `AddSpacesToJoinedLanguages…` 那一挂，词条表在远端 ⇒ 照本工程惯例印 prefab 原文）。</summary>
        public const string TxtUnread = "New!";

        public static readonly Color MdTitleTint = new Color(0.95f, 0.66f, 0.40f, 1f);

        public const int QShade = 3002, QPanel = 3006, QContent = 3010, QText = 3011, QOverlay = 3014;
        /// <summary>🆕 **2026-10-19（`T29` 剩余五屏 · 收件箱）**：`Message Display/Background` 单独一档。
        /// <para>🔴 **改前**：它（`UI_Deck_Information_submenu_Back`，**α 0.52 半透明**）与整窗红底
        /// `Generic Window Red Background Big`（`UI_Deck_Information_Back`，**α 254 不透明**）**同为 `QPanel`(3006)、
        /// z 都是 0** ⇒ 同队列同 z，谁盖谁不确定（本仓命中/排序口径见 `CLAUDE.md` §三）。
        /// 原版靠**兄弟序**：`Content` 的子件序是 `Generic Window Red Background Big` → … → `Message Display`
        /// ⇒ 半透明显示板**必须压在红底之上**。</para>
        /// <para>取 `QPanel + 1`：仍严格低于本窗内容档 `QContent`(3010)。</para></summary>
        public const int QDisplayBg = QPanel + 1;

        public readonly System.Collections.Generic.List<string> MissingArt =
            new System.Collections.Generic.List<string>();

        /// <summary>一条消息的**视图形态**（原版 `IGameNews` 在列表/正文那两处的投影）。
        /// <para>🔴 **2026-10-25（N17）**：数据源接上了 —— `Build()` 走 <see cref="LoadFromStore"/> 从
        /// `InboxData` 灌（`InboxData` 的字段对位 = 原版 `BaseInboxEvent` 的
        /// `GetTitle` / `GetMessage` / `GetRewards` / `IsRead` / `StartDate` / `RedeemableAt`）。
        /// 三参构造**保留**：`Editor/RewardsScene.cs` 用它灌假消息验裁切（那是自检的夹具，⛔ 别删）。</para>
        /// <para>`Id` / `Body` / … 这几格是**新增**的（三参构造不填 ⇒ `Id` 为 null）：
        /// 有 `Id` 的行走真数据（可选中、可领、可标已读）；没有 `Id` 的（自检假消息）只铺行、点行出声。</para></summary>
        public class Message
        {
            public string Title, Date;
            public bool Unread;
            // ---- 🆕 2026-10-25：真数据源那几个字段（判据见 `InboxData.Msg` 的注释）----
            public string Id;                 // 原版 eventId（`InboxData.Msg.Id`）
            public string Body;               // `GetMessage()`
            public string HeaderArt, FooterArt;
            public long RedeemableAtMs;       // `RedeemableAt()`
            public bool Claimable;            // `MessageDisplay.CheckMessageCollectable()` 当前这一拍的结果
            public Message(string title, string date, bool unread) { Title = title; Date = date; Unread = unread; }
        }

        readonly System.Collections.Generic.List<Message> _messages =
            new System.Collections.Generic.List<Message>();
        /// <summary>列表条目的行节点（自检量用；顺序 = **树序** = 数据序的倒序，见 `BuildMessageRows`）。</summary>
        public readonly System.Collections.Generic.List<Transform> Rows =
            new System.Collections.Generic.List<Transform>();
        /// <summary>列表的滚动区（**全壳唯一一份滚动实现** = `MenuScroll`）。
        /// 原版那颗 `ScrollRect` **是活的**（`h=0 **v=1** mode=1`，dump 里没有 `m_Enabled=0`）
        /// ⇒ **登记给 `PointerLayer`**（滚轮落得到它身上）。空列表时它的可滚范围是 0（`ClampLo/Hi` 都夹到 0）。</summary>
        MenuScroll _listScroll;
        Transform _listContent;
        /// <summary>`Message Display` 那块（空态下整块关）与 `No News Warning` —— 两条分支的落点
        /// （`ApplyEmptyState` 一份，`Build()` 与 `Initialize()` 都走它）。</summary>
        Transform _display; Label _warn;
        public MenuScroll ListScroll { get { return _listScroll; } }

        // ---- 🆕 **2026-10-25（N17）：正文那半 + 选中态 + 条目动画** ----
        /// <summary>`Message Display/Title` 那颗（原版 `MessageDisplay.title`，`Initialize` 里写
        /// `message.GetTitle()`）—— 建的时候存一份引用，选中消息时改写它。</summary>
        Label _mdTitleLb;
        /// <summary>`Message Display/Content/…/Message`（原版 `MessageDisplay.body`）。</summary>
        Label _mdBody;
        /// <summary>原版 `MessageDisplay.headerImage` / `footerImage`（`HasAsset` 为假 ⇒ 整件关）。</summary>
        Transform _mdHeader, _mdFooter;
        /// <summary>原版 `MessageDisplay.rewardWarningText`（`CheckMessageCollectable` 里
        /// 「隐藏阵营/扩展 ⇒ 开它并返回 false」那两条支；我们全解锁 ⇒ 恒关，但**建着**、
        /// 将来接 FeatureConfig 时只改判据）。</summary>
        Label _mdWarn;
        /// <summary>每条行的底图 quad / 行节点 / 底图上的扫光层，**按消息的数据下标（`idx`）索引**
        /// （⚠️ 行是倒序建的，用 `List.Add` 会与 `_messages[i]` 错位 —— 这里按 idx 直接下标写）。
        /// 底图换图那一位 = `MessageContainer.ToggleSelection` 的 `background.sprite = selected ? … : …`。
        /// <para>🆕 2026-10-27（B9_W51SHINE2）：第三张表原来是 `Label[] _rowNew`（**未读那一下闪它的字**）——
        /// 现在改成扫光层（原版那颗 `UIEffect` 扫的就是**底图**那张 `Image`，字恒定不动，判据见 `RowShineParams`）。</para></summary>
        ImageQuad[] _rowBgs = new ImageQuad[0];
        Transform[] _rowNodes = new Transform[0];
        WildcardShineLayer[] _rowShine = new WildcardShineLayer[0];
        /// <summary>当前选中的消息下标（`-1` = 没选）。原版 `MessageContainer.selected` 的那一位。</summary>
        int _current = -1;
        /// <summary>`Message List` 的滚动偏移变了 ⇒ 重建行；重建后**按 id 复原选中**（原版选中态长在
        /// 条目实例上，我们每次重建 ⇒ 必须自己记）。</summary>
        string _selectedId;

        // ---- 条目入场动画（原版 `MessageContainer.AnimateIn`）----
        // 参数逐字照原版 prefab（`Message Container` 的 MB `-9188051066216764495`）：
        //   `animationDelay = 0.1` · `animationTime = 0.15` · `curve` = **原版那条 3 键曲线**（逐键见类尾 `AnimCurve`）。
        //   ✅ 2026-10-27（B9_W63WC）就地订正（铁律 5）：原文写「曲线**没解出数值** ⇒ 用 `EaseOutQuad` 等价」——**已过期**：
        //   那份 MB 的 `curve` 是**完整 3 键 Clamp 曲线**（原版还有 `DOScale(底图, localScale).From(localScale×0.5)`；缓动**只挂在 DOScale 上**，`DOFade` 用 DOTween 默认 `Ease.OutQuad`）。
        public const float AnimDelay = 0.1f, AnimTime = 0.15f, AnimScaleFrom = 0.5f;
        float _animT = -1f;                      // <0 = 没在播
        int _animRows;                           // 这一轮有几行要入场
        bool _animPlayed;                        // 「入场播过没有」—— 行重建/滚动/选中都**不重播**（见 StartAnimateIn）

        // ---- 未读行「扫一下」（原版 `MessageContainer.Update` 的 `shinyEffect.Play`）----
        // 判据（🥇 机器码真体 = `d:/2/tools/decomp_full/Everguild.LiveOps.MessageContainer__Update.c`）：
        //   `if (message != null) { if (!IsRead(message)) { timeSinceLastShine += dt;
        //    if (timeSinceLastShine >= shineTime) { timeSinceLastShine = 0;
        //      shineTime = effectTimer × (1 + Random.Range(-0.2, 0.2)); shinyEffect.Play(resetTime: true); } } }`
        //   `effectTimer` = prefab 序列化 **3.0**（`bundle_menus_assets_all/MonoBehaviour/
        //   MonoBehaviour_-9188051066216764495.json` 实读）；`shineTime` / `timeSinceLastShine` 是**非序列化**字段
        //   （出厂 0）⇒ **建行后第一帧就扫一次**，之后每 3.0×(1±0.2) 秒一次。
        // 🔴 **2026-10-27（B9_W51SHINE2）就地订正（铁律 5）**：这里原来写「原版那件是 Coffee.UIEffects 的
        //   `UIEffectTweener`（扫光）；**本工程没有那一层** ⇒ 等价实现 = 把 `New!` 那颗字的**亮度**闪一档」——
        //   **已过期**：扫光层（`Effects/UIEffectTogglerCoffeeEffects.cs` 的 `WildcardShineLayer`）与
        //   `Resources/Shaders/WildcardShine.shader` 都已落地 ⇒ 现在走**真扫光**，挂在每条未读行的底图上
        //   （参数见 `RowShineParams`）。那颗 `New!` 字**不再改色**（原版那颗字的颜色是恒定的）。
        public const float ShineBase = 3.0f, ShineJitter = 0.2f;
        float _shineTimer, _shineNext;      // `_shineNext` 出厂 0 ⇒ 第一帧就扫（= 原版那两个非序列化字段）
        readonly System.Random _rng = new System.Random(20261025);   // 定种子（不碰 UnityEngine.Random）

        /// <summary>有没有消息。🔴 **2026-10-25 起 = 真数据源的条数**（原版 `InboxWindow__Open` 的
        /// 「`messages.Count == 0` ⇒ 空态」那一支）；`DailyData.InboxCount` 不再参与
        /// （它现在反过来转发 `InboxData.UnreadCount()`，是**红点**的口，不是本窗的口）。</summary>
        public bool HasMessages { get { return _messages.Count > 0; } }

        /// <summary>🆕 **2026-10-05（A81）**：压暗层的**命中区**节点（「点窗外关窗」）—— 自检用
        /// （`MenuDraw.ShadeRuleOk(darkHit, qShade, qContentMin, out why)` 的 `darkHit`）。</summary>
        public Transform ShadeHit { get { return transform.Find("BackgroundHit"); } }

        public static InboxWindow Create(WindowsManager mgr)
        {
            var go = new GameObject("Inbox Menu");
            var win = go.AddComponent<InboxWindow>();
            win.type = WindowType.Popup;                  // 实证 type=1
            win.placement = WindowsPlacement.Popup;       // 实证 windowsPlacement=15
            win.closeOnEsc = true;                        // 实证 closeOnESC=1
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open() { Build(); }

        public void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);
            MissingArt.Clear();

            // 🆕 **2026-10-19（`T29` 剩余五屏 · 收件箱）**：alpha 原来是 `0.77f`，原版 `Menu Dark Background`
            //   的 `m_Color = (0,0,0,0.773)`（prefab 实读）⇒ 对齐成 0.773f（同 `DailyRewardPopup` D6 那一族）。
            MenuDraw.Rect(root, CardArt.Solid(), Shade, "Menu Dark Background", QShade,
                          new Color(0f, 0f, 0f, 0.773f));
            // 🔴 **2026-10-05（A81）**：压暗层的**点击区**（「点窗外关窗」）—— **原来零 `ShadeHit(`**
            //   （只有 `:87` 那一层压暗的 `Image`，点了什么也不发生）。
            //   判据：原版是压在 `Menu Dark Background` **自身节点**上的 `BackgroundCloseButton`，
            //   由窗口类自己挂/摘 —— `Everguild.LiveOps.InboxWindow__Open.c:95-109` 的**同一段**
            //   给 `param_1[0x11]`（关窗钮的 `Button.m_OnClick`，`+0x100`）与 `param_1[0x14]`
            //   （`BackgroundCloseButton.onClick`，`+0x28`）**挂的是同一个处理函数**（虚表 `*(*param_1+0x1c0)`）；
            //   `InboxWindow__Close.c:15-25` 又把这两条**成对摘掉**。
            //   ⇒ 动作照抄本窗那颗关窗钮（`本文件` 的 `Close()`），两颗行为**必须一致**。
            //   档 = **压暗层自己那一档 `QShade`(3002)**，**严格低于**本窗内容命中区档 `QOverlay`(3014)
            //   （本窗唯一的内容命中区 = `Generic Close Button Orange` 那颗，在 `QOverlay`）。
            //   出处 → `资料/待办判据_阶段二与联机.md` §A81 · 公共件规矩 → `MenuDraw.ShadeHit` 的注释。
            MenuDraw.ShadeHit(root, Shade, QShade, QOverlay, () => Close(), "BackgroundHit");

            var c = MenuDraw.Node(root, "Content", Content);
            MenuDraw.Rect(c, Art(ArtRedBg), RedBg, "Generic Window Red Background Big", QPanel);
            // 🆕 **2026-10-06（A94）：红底那块面板吸收点击**。判据 = 原版 prefab
            //   `Inbox Menu > Content > Generic Window Red Background Big` 那颗 `Image` 的
            //   **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读）—— 射线打到它自己、
            //   父链上没有点击处理器（关窗那颗 `BackgroundCloseButton` 在压暗层上）⇒ 原版**什么都不做**。
            //   ⚠️ 矩形用 `RedBg`（那颗 `Image` 的 rect），**不是**容器 `Content` —— 两者差十几 px，
            //   原版吸收到的是 `Image` 那一圈。
            MenuDraw.Absorb(root, "AbsorbHit", RedBg, QShade, QOverlay);
            // 🆕 **2026-10-12（A468）**：这颗原版是 **`auto[18.0~48.0]` · `基准=36.0` · `折行=0`**
            //   ⇒ 接上自适应（`wrapPx` = 框宽、`autoMinPx` = 18、`autoBasePx` = 36；`autoMaxPx` 不传，
            //   原版那一档 = 48 = 我们传的 `fontPx` ⇒ 等价），并把 `SetAutoFitBox` **无条件打开**的折行
            //   还原成原版的 `0`。判据/口径 → `TitleAutoMin` 那一段的注释（三颗的 min 各不相同）。
            var titleLb = MenuDraw.Text(c, Title, DailyData.InboxTitle(), Color.white, "Title", 48f, QText,
                                        Title.W, TitleAutoMin, autoBasePx: WindowLabelAutoBase, face: TmpFont.Face.LatinCardBody);
            if (titleLb != null) titleLb.SetWrapping(false);
            // 🆕 **2026-10-12（A470）**：原版这一颗是 **`对齐=Left/Middle`**（我们原来落在框心）——
            //   走 `TitleLeft`（它把「必须在 `SetWrapping(false)` **之后**」这条顺序要求写死在方法上）。
            //   判据 → `TitleLeft` 的注释（本件亲跑的 dump 两行原文）。
            TitleLeft(titleLb, Title);

            // ---- `Message List`：原版三层 = `Message List`(ScrollRect) → `Viewport`(RectMask2D) → `Content`(VLG) ----
            // 🔴 **2026-10-08（A182）：原来只建了 `Message List` 一个空节点**（`Viewport` / 掩码 / 滚动都没有，
            //   而原版这三层是齐全的）。我们**单机没有消息**（`DailyData.InboxCount` 恒 0）⇒ 名单照旧是**空的**
            //   （这一点与原版 prefab 出厂一致：`Content` 出厂 0 子件、`ContentSizeFitter` 给的高 = 0），
            //   但**结构、裁切、滚动三件都按原版建出来**：`Initialize(messages)` 一有数据就照原版铺。
            // 🔴 **2026-10-25（N17）：这里原来写 `_messages.Clear()`（单机恒空）** —— 现在改成
            //   **从本地消息源灌**（`InboxData`；等价物，判据见那个文件的头注释）。
            //   原版 `InboxWindow__Open.c`：`messages = LiveOpsManager.GetHandler<Inbox>().Messages`；
            //   空 ⇒ `noNewsWarning` 开 + `display` 关；非空 ⇒ `display.Initialize(当前)` +
            //   `list.Initialize(messages, 当前)` + `list.OnMessageSelected += OpenMessage`（`:49-113`）。
            //   我们这三步在 `LoadFromStore()` / `ApplyEmptyState()` / `BuildRow` 的命中回调里各有一份。
            LoadFromStore();
            var msgList = MenuDraw.Node(c, "Message List", MsgList);
            // 🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2）**：这一颗是**视口节点**，裁切状态就挂在它身上
            //   （= 原版 `RectMask2D` 挂 `Inbox Menu/Content/Message List/Viewport`；契约 → `Shell/ViewportClip.cs`）。
            //   走 `ViewportClip.Hang` ⇒ **框（节点自己的 rect）· `padding` · `softness` 三样一次写死**：
            //   `ListPad = (0,0,0,0)` · `ListSoft = (0,25)`（原版实读，见那两个常量的 doc）。
            //   ⛔ **别改回 `Node(...)`**：那样这颗节点上就没有状态了
            //   （`Shell/WindowsManager.cs` 的 `ClipSoftness` 那段表里，本窗这一条就是 `(0,25)`）。
            var listVp = ViewportClip.Hang(msgList, "Viewport", MsgList, ListPad, ListSoft).transform;
            _listContent = MenuDraw.Node(listVp, "Content", new PxRect(MsgList.x1, MsgList.y1, MsgList.x2, MsgList.y1));
            BuildMessageList();

            // `Message Display`：**空态下整块关**（`InboxWindow__Open.c:49`）
            _display = MenuDraw.Node(c, "Message Display", MsgDisplay);
            MenuDraw.Rect(_display, Art(ArtDisplayBg), MsgDisplay, "Background", QDisplayBg);
            // 🆕 **2026-10-12（A468）**：原版 `auto[10.0~58.0]` · `基准=36.0` · `折行=0`（判据 → `TitleAutoMin`）
            // 🔴 **2026-10-25（N17）**：这颗现在**就是消息标题的载体** —— 原版 `MessageDisplay.Initialize`
            //   里 `title.text = message.GetTitle()`（`MessageDisplay.cs:102-105`），所以建完要**存引用**，
            //   选中消息时改写它（占位串照原版 prefab 的 'WELCOME TO WARPFORGE CLOSED ALPHA!'）。
            _mdTitleLb = MenuDraw.Text(_display, MdTitle, DailyData.InboxMessageDisplayTitle(),
                                       Color.white, "Title", 58f, QText,
                                       MdTitle.W, MdTitleAutoMin, autoBasePx: WindowLabelAutoBase, face: TmpFont.Face.LatinCardBody);
            if (_mdTitleLb != null) _mdTitleLb.SetWrapping(false);
            // 🆕 **2026-10-12（A470）**：同上一颗 —— 原版 `对齐=Left/Middle`（判据 → `TitleLeft` 的注释）。
            //   ⚠️ 必须排在**上面那句 `SetWrapping(false)` 之后**（理由同 `TitleLeft`）。
            TitleLeft(_mdTitleLb, MdTitle);
            MenuDraw.Rect(_display, Art(ArtScrollbar), MdScrollbar, "Scrollbar Vertical", QContent);
            // 🔴 **2026-10-18（音效线转来的界面缺口）：`Claim Button` 建壳。**
            //   原版层级（树实读）：`Message Display ▸ Content ▸ Scroll View ▸ Viewport ▸ Content ▸ Claim Button`（容器）
            //   ▸ `Claim Button`（钮）▸ `Button Text`（'Claim'）。内容由**消息对象**驱动
            //   （`MessageDisplay.Initialize(message)` / `InboxWindow.GetCurrentMessage`），单机没有消息
            //   ⇒ 整块 `Message Display` 空态下是**关着的**（`ApplyEmptyState`）⇒ 这几件建出来也**看不见**。
            //   `EverguildButton` 原版值（MB `8813341034201514802`）：`m_onClickSound` = pid **8549838806721213354**
            //   = **`ButtonClick Positive`** · `useDefaultAudioClipIfNull = 1` · `m_OnClick.m_Calls` **空**（动作在代码里）。
            //   ⚠️ 树里 `Viewport` = **0×0**、`Content` = **2×0**（布局组跑之前的作者态）——
            //     我们按 `Scroll View` 的框给 `Viewport`、给 `Content` 一个零高（`MsgContent` 的 y1=y2），
            //     **如实记：这两层的尺寸是「照形状」不是原版数值**（原版运行时由布局组撑）。
            var mdContent = MenuDraw.Node(_display, "Content", MdContent);
            var mdScroll = MenuDraw.Node(mdContent, "Scroll View", MdContent);
            var mdVp = MenuDraw.Node(mdScroll, "Viewport", MdContent);
            var mdVpContent = MenuDraw.Node(mdVp, "Content",
                                            new PxRect(MdContent.x1, MdContent.y1, MdContent.x2, MdContent.y1));
            // ============================================================ 🆕 **2026-10-25（N17）：正文那三件**
            // 原版层级（`menu_dump … "Inbox Menu" --depth 9` 实读）：
            //   `Message Display ▸ Content ▸ Scroll View ▸ Viewport ▸ Content` 底下依次
            //   `Header`(Image 972.80×243.20) → `Message`(TMP 974.31 宽 · 字号 45 · 基准 45 ·
            //   **Left/Top** · **折行 1** · 字距 1) → `Claim Button`(容器 958.86×216.21) → `Footer`(972.80×243.20)。
            // 判据 → 本文件 `MdHeader` / `MdBody` / `MdFooter` / `MdWarn` 那几行的注释。
            // ⚠️ `MessageDisplay.Initialize` 对 Header/Footer 的判据是 `LiveOpsAssetUtility.HasAsset(...)`：
            //   **没有图 ⇒ 整件 `SetActive(false)`**（`MessageDisplay.cs:109-134`）—— 我们本地两条消息都没有
            //   头/尾图 ⇒ 建着、关着（⛔ 不是省略；将来导入了图只填 `Message.HeaderArt`）。
            _mdHeader = MenuDraw.Node(mdVpContent, "Header", MdHeader);
            MenuDraw.Rect(_mdHeader, null, MdHeader, "Image", QContent, null, true);
            if (_mdHeader != null) _mdHeader.gameObject.SetActive(false);
            // `Message`：正文。**折行开着**（原版 1）、**不做自适应**（dump 里没有 `auto[…]` ⇒
            //   `m_enableAutoSizing = 0`）⇒ `autoMinPx` 传 0（`MenuDraw.Text` 只有 >0 时才装自适应）。
            _mdBody = MenuDraw.Text(mdVpContent, MdBody, "", Color.white, "Message", MdBodyFont, QText,
                                    MdBody.W, 0f, 0f, 0f, face: TmpFont.Face.LatinCardBody);
            if (_mdBody != null)
            {
                // 原版 `字距=1`（`m_characterSpacing`）。同 `_warn` 那条：`SetCharSpacing` 不刷缓存的宽高
                // ⇒ 接着一句 `ForceRelayout()`（幂等）。
                _mdBody.SetCharSpacing(MdBodyCharSpacing);
                _mdBody.ForceRelayout();
                // 原版 `对齐=Left/Top`：水平那半走现成的口（`MenuDraw.AlignLeft`），
                // 垂直那半 = `Label.VAlign.Top`（`MenuDraw.SetVAlign`）。
                MenuDraw.AlignLeft(_mdBody, MdBody);
                MenuDraw.SetVAlign(_mdBody, Label.VAlign.Top, MdBody); _mdHit = MenuDraw.Hit(mdVpContent, "MessageText Hit", MdBody, QText + 1, null); if (_mdHit != null) { var _wb = _mdHit.GetComponent<WindowButton>(); if (_wb != null) { _wb.tintOnHover = false; _wb.playClickSound = false; _wb.onDown = OnMessageBodyClick; } }   // 🆕 B90-N3：正文链接（原版 `MessageDisplay.OnPointerClick`）
            }
            _mdFooter = MenuDraw.Node(mdVpContent, "Footer", MdFooter);
            MenuDraw.Rect(_mdFooter, null, MdFooter, "Image", QContent, null, true);
            if (_mdFooter != null) _mdFooter.gameObject.SetActive(false);
            var claimBox = MenuDraw.Node(mdVpContent, "Claim Button", ClaimBox);
            var claimBtn = MenuDraw.Node(claimBox, "Claim Button", ClaimBtn);
            var claimBg = MenuDraw.Nine(claimBtn, Art("UI_Button_Mulligan"), ClaimBtn,
                                        new Vector4(333f, 96f, 333f, 96f), 410f, 124f, QContent, null, true, "Image");
            var claimTxt = MenuDraw.Text(claimBtn, ClaimTx, Loc.T("MainMenu/General/Claim"),
                                         Color.white, "Button Text", 40f, QText,
                                         ClaimTx.W, 10f, 40f, 12f, face: TmpFont.Face.LatinCardBody);
            if (claimTxt != null) claimTxt.SetWrapping(false);
            // 原版 `Button Text` 的 `m_VerticalAlignment = 8192 (Capline)`（TMP 字段实读）
            if (claimTxt != null) MenuDraw.SetVAlign(claimTxt, Label.VAlign.Capline, ClaimTx);
            MenuDraw.Hit(claimBtn, "Hit", ClaimBtn, QOverlay, OnClaimClick,
                         claimBg != null ? claimBg.GetComponentInChildren<ImageQuad>() : null,
                         "UI_Button_Mulligan", clickCue: WindowSounds.PositiveCue);
            // 🆕 **2026-10-25（N17）**：`Claim reward warning`（原版在 `Claim Button` 容器里、钮的下方）。
            //   判据 = `MessageDisplay.CheckMessageCollectable` 的两条支：`FeatureConfig.IsArmyHidden` /
            //   `IsExpansionHidden` ⇒ **开这颗** + 返回 false（`MessageDisplay.cs:278-288`）。
            //   ⚠️ 我们**全解锁**（用户 2026-09-17 边界②）⇒ 这两支恒不命中 ⇒ 出厂**关着**；
            //   建它是为了「将来接 FeatureConfig 时只改判据」，也是原版那棵树的一部分。
            _mdWarn = MenuDraw.Text(mdVpContent, MdWarn, TxtRewardWarning, Color.white, "Claim reward warning",
                                    MdWarnFont, QText, MdWarn.W, 0f, MdWarnFont, MdWarnFont,
                                    face: TmpFont.Face.LatinCardBody);
            if (_mdWarn != null)
            {
                _mdWarn.SetCharSpacing(MdWarnCharSpacing);
                _mdWarn.ForceRelayout();
                MenuDraw.SetVAlign(_mdWarn, Label.VAlign.Top, MdWarn);
                _mdWarn.gameObject.SetActive(false);
            }

            // `No News Warning`（空态文案）
            // 🆕 **2026-10-12（A468）**：原版 `auto[12.0~50.0]` · `基准=36.0` · `折行=0`（判据 → `TitleAutoMin`）
            _warn = MenuDraw.Text(c, NoNews, DailyData.InboxNoNewsText(), Color.white, "No News Warning", 50f, QText,
                                  NoNews.W, NoNewsAutoMin, autoBasePx: WindowLabelAutoBase, face: TmpFont.Face.LatinCardBody);
            if (_warn != null) _warn.SetWrapping(false);
            // 🆕 **2026-10-12（A471）**：原版这一颗**还有 `字距=2`**（我们原来是 0）—— 判据 → `NoNewsCharSpacing`。
            //   ⚠️ 这一颗原版是 `Center/Middle` ⇒ **不做对齐**（与我们一致）；只需要把字距接上。
            //   🔴 尾上那句 `ForceRelayout` 不是装饰：`SetCharSpacing` 只把 mesh 重排一次、
            //     **不刷新 `Label` 自己缓存的 `_tmpW/_tmpH`**（`Battle/Label.cs` 的 `SetCharSpacing`）⇒ 少了它，
            //     这颗的 `WorldW/WorldH` 会停在**字距生效之前**的宽度（断言量宽度时读到旧值 = 静默不一致）。
            //     `ForceRelayout` 是**幂等**的（传当前字号 ⇒ setter 早退，只推一次重排，见它的头注释）。
            if (_warn != null)
            {
                _warn.SetCharSpacing(NoNewsCharSpacing);
                _warn.ForceRelayout();
            }
            ApplyEmptyState();        // 🆕 A182：这条分支原来写在 `Build()` 里，现在收成一份（`Initialize` 也要用）
            // 🆕 **2026-10-25（N17）**：原版 `InboxWindow__Open` 非空那一支的 `display.Initialize(当前)`
            //   （`GetCurrentMessage()` = 最后一条已读 / 否则第一条）。空表时 `ShowDetail(null)` 只把正文关掉。
            ShowDetail(_current >= 0 && _current < _messages.Count ? _messages[_current] : null);

            // ---- 🔴 **2026-10-18（音效线转来的界面缺口）：`Reset Button` 建壳** ----
            //   原版：`Inbox Menu/Content/Reset Button`（[399,126] 142×71 · 图 `UI_Button_Mulligan` 九宫 333,96,333,96 ·
            //   `Button Text` = 字面量 **'DEBUG RESET'**，fs 23.2 · auto[10~40] · base 12 · Center/Capline · 折行 0）。
            //   `EverguildButton`（MB `7902057683169346354`）：`m_onClickSound` = pid **8549838806721213354**
            //   = **`ButtonClick Positive`** · `useDefaultAudioClipIfNull = 1` · `m_OnClick.m_Calls` **空**。
            //   🔴 **它是一颗死钮**（两条独立判据，都现核过）：
            //     ① `InboxWindow__Open.c` **首句**就是 `resetButton(0x90).SetActive(false)`（字段偏移现读 `dump.cs:129414`）
            //        ⇒ 运行时**永远不显示**；
            //     ② `InboxWindow__DebugReset` 全量反编译**零调用者**（`grep -rn DebugReset decomp_full/` 只有它自己）。
            //   ⇒ 我们照原版的**运行态**建好再藏（⛔ 不激活 —— 那会多出一颗原版看不到的钮），
            //     命中 + 音先接好（将来原版那条路若要放开，一句 `SetActive(true)` 即可）。
            var resetGo = MenuDraw.Node(c, "Reset Button", ResetBtn);
            var resetBg = MenuDraw.Nine(resetGo, Art("UI_Button_Mulligan"), ResetBtn,
                                        new Vector4(333f, 96f, 333f, 96f), 410f, 124f, QOverlay, null, true, "Image");
            var resetTx = MenuDraw.Text(resetGo, ResetTx, "DEBUG RESET", Color.white, "Button Text", 23.2f, QText,
                                        ResetTx.W, 10f, 40f, 12f, face: TmpFont.Face.LatinCardBody);
            if (resetTx != null) resetTx.SetWrapping(false);
            if (resetTx != null) MenuDraw.SetVAlign(resetTx, Label.VAlign.Capline, ResetTx);
            MenuDraw.Hit(resetGo, "Hit", ResetBtn, QOverlay, OnResetClick,
                         resetBg != null ? resetBg.GetComponentInChildren<ImageQuad>() : null,
                         "UI_Button_Mulligan", clickCue: WindowSounds.PositiveCue);
            resetGo.gameObject.SetActive(false);

            // 关闭钮
            // 🔴🔴 **2026-10-18（B3）节点名与内容【归位】—— 改前三个名字整体错开一格、是一处真偏离。**
            //   **改前**：叫 `Background` 的那颗画**圆底盘**、叫 `Icon` 的才画**黄面**、叫 `Icon (X)` 的才画叉。
            //   **原版实读**（读数逐字抄自 `rcunion` 输出 ——
            //     `python -I d:/tmp/wf_hit/rcunion.py bundle_menus_assets_all "Inbox Menu" --depth 8`）：
            //     根 `Generic Close Button Orange` 那颗 `Image` = `UI_Button_Round_background`
            //       （**圆底盘 · `RT=0` ⇒ 不吃射线**）；
            //     子件 **`Background` = `40k_general_bt_yellow`**（黄面 · `RT=1` pad `(-20)⁴`）；
            //     子件 **`Icon` = `40k_general_bt_yellow_close`**（叉 · `RT=1` pad `(-20)⁴`）。
            //   ⇒ **按原版的名字给**：`Background` 画黄面 · `Icon` 画叉。
            //   ⛔ **别把圆底盘那颗也叫 `Background`**：那会和黄面那颗**同名兄弟**（`MenuCheck.FindChild`
            //     不认识路径、按名字取 ⇒ **静默取错一颗**）。本件把它画到根节点上，这一条自然消掉了。
            //   · ⚠️ 本窗**没有** `Icon (X)` 这个节点名 —— 那是改前的**自造名**，全仓只此一处
            //     （`grep '"Icon (X)"'` = 0 命中，`Editor/` 里也没有任何断言碰过它）⇒ 改名不红任何既有件。
            //   🔴 **换图那一层 = 画黄面的那颗**（下面 `Bind` 的 `target` 实参）—— 它的名字现在**正好就是**
            //   原版的 `Background`，与 `m_TargetGraphic` 逐字一致（判据 → 下面 A1058 那一段）。
            // ═══════════════════════════════════════════════════════════════════════════════════════
            // 🔴🔴 **2026-10-18（B4）圆底盘【归真】：矩形 + 缩放 + 挂点三样一起改。**
            //   **改前**：圆底盘拿**子件矩形 `CloseBg`（56.86×58.13）**、**没传 `keepAspect`**，
            //     画在一颗**自造子件** `Base` 上 ⇒ 拉成 56.86×58.13（比原版**小约 24%**）。
            //   **原版实读**（B4 亲跑 **逐字段**，⛔ 不是推的 ——
            //     `python -I d:/tmp/wf_b4probe/pa.py bundle_menus_assets_all "Inbox Menu" 6`）：
            //     · 根 `MonoBehaviour_3213954248950252338.json`：
            //       `m_Sprite=2381704724431365035`(`UI_Button_Round_background`) · `m_Type=0`(Simple) ·
            //       **`m_PreserveAspect=1`** · `m_PixelsPerUnitMultiplier=1.0` · `m_RaycastTarget=0`
            //       · 矩形 = **74.39×75.60**（= 本窗 `CloseBtn`）；
            //     · 子件 `Background`(`MonoBehaviour_5609434692533257010.json`) 与
            //       子件 `Icon`(`MonoBehaviour_-3135620231419100366.json`)：**同样 `m_PreserveAspect=1`** ·
            //       `m_Type=0` · `m_PixelsPerUnitMultiplier=1.0` · 矩形 56.86×58.13（= 本窗 `CloseBg`）。
            //     ⚠️ `m_PixelsPerUnitMultiplier` 只对 `Sliced`/`Tiled` 有意义（本仓口径：角块 = `m_Border ÷ ppuMul`）
            //       —— 这三颗都是 **`m_Type=0` Simple**、且 sprite 的 `m_Border=(0,0,0,0)` ⇒ **在这三颗上是死值**。
            //     ⚠️ 原版这三张 sprite 的 `m_Rect`（`d:/2/新解包资源/assets_full/*/Sprite/*.json`，两份拷贝同值）：
            //       `UI_Button_Round_background` = **237×237** · `40k_general_bt_yellow` = **71×71** ·
            //       `40k_general_bt_yellow_close` = **71×71**，三者 `m_Border` 与 `m_Offset` **全 0**
            //       ⇒ `PA=1` 实绘 = **正方形内接**：圆底盘 **74.39×74.39**、黄面/叉 **56.86×56.86**
            //       （改前我们画 56.86×58.13 ⇒ 高多 ≈2.2%）。
            //   · **挂点归真**：原版这颗圆底盘长在**根节点 `Generic Close Button Orange` 自己**身上（**无独立子件名**）
            //     ⇒ 照**兄弟窗先例** `Shell/BaseOfferPopup.cs` 与 `Shell/ReferralPopupWindow.cs`
            //     **抄形状**：`Rect(parent, …)` **直接画在根节点上**、图取不到时才退回 `MenuDraw.Node`
            //     （两处**都是** `Rect` 在前、`Node` 作兜底）。⇒ 自造名 `Base` **随之取消**。
            //     ⛔ **别照 `Shell/BoosterInfoPopup.cs` 那颗子件 `Base`** —— B4 现读：`Booster Info Popup`
            //     与 `Base Offer Popup` 的**原版结构逐字相同**（圆底盘都在根节点上）⇒ 那颗 `Base` 是**它自己**
            //     偏离原版，不是可抄的先例（它不在本件白名单内、本件没动它）。
            //   🔴 三颗**都传 `keepAspect: true`**（= 原版三颗一致的 `m_PreserveAspect=1`）。
            //     算式只有一份 → `MenuDraw.Rect` → `Core/CardbackFace.Fit`（「按 sprite 的 `m_Rect` 定框」+
            //     「按 `padding` 内缩」两段）。这三张贴图**没登记**在 `Resources/Cardbacks.json`（233 条，无命中）
            //     ⇒ 兜底 = 拿**贴图自己的宽高**当 `m_Rect`、`padding=0`；而本工程导入的贴图尺寸
            //     （`CardArt.MenuUi` → `Resources/Art/{ui_menu,ui_deck,ui}`，实读）**恰好等于**原版 `m_Rect`
            //     （237×237 / 71×71 / 71×71）⇒ **实绘与原版逐像素同值**（圆底盘 74.39×74.39、黄面/叉 56.86×56.86）。
            //   ⚠️ 队列**不变**：圆底盘仍 `QContent`(3010)、黄面/叉仍 `QOverlay`(3014)
            //     ⇒ 压暗层那条「`QShade`(3002) 严格低于内容命中区档 `QOverlay`」的不变量不受影响。
            //   ⚠️ 命中区**不受本改动影响**：它由 `MenuDraw.Hit` 按 `PaddedRect(CloseBg, ClosePad)` 独立建
            //     （96.86×98.13），与这三颗的矩形成像无关（中心也没动 ⇒ 中心那条断言同样不受影响）。
            var closeQ = MenuDraw.Rect(c, Art(ArtCloseBg), CloseBtn, "Generic Close Button Orange", QContent, null, true);
            var close = closeQ != null ? closeQ.transform : MenuDraw.Node(c, "Generic Close Button Orange", CloseBtn);
            var closeFaceQ = MenuDraw.Rect(close, Art(ArtCloseIcon), CloseBg, "Background", QOverlay, null, true);
            MenuDraw.Rect(close, Art(ArtCloseX), CloseBg, "Icon", QOverlay, null, true);
            {
                // 🆕 A17：原版 `Content>Generic Close Button Orange` 是 SpriteSwap，HL = `40k_general_bt_yellow_hover`
                // 🔴 **2026-10-14（A810③）：第一格原来传的是 `null`** —— `WindowButton.Bind`（`Shell/WindowButton.cs`；⚠️ 2026-10-19 订正：原写 `PromptPopup.Bind`）里
                //   `PressedNameFor(null) = null` ⇒ 往 `MissingPressedArt` 记下一条**认不出是谁**的
                //   `" → "`（审计日志上就表现为「缺 1 条、但指不出是哪一颗」）。**现在传它自己的常态图名。**
                //   判据 = 本件亲跑 `python 工具/menu_dump.py bundle_menus_assets_all "Inbox Menu" --depth 6`：
                //   `Generic Close Button Orange … UI_Button_Round_background 237×237 … | trans=2
                //    target=5609434692533257010 interactable=1 | HL=40k_general_bt_yellow_hover
                //    P=40k_general_bt_yellow_pressed` ⇒ 被换的是这颗**圆底**、它的常态图就是 `UI_Button_Round_background`。
                //    ⛔⛔ **这一句已被下面 2026-10-18 那条更正推翻**（`target=` 那个 pid 解出来是**黄面**）——
                //    保留原文只为留订正痕，⛔ 别再照它推。
                //   ⚠️ 本地没有它对应的按下图（`PressedNames` 表里也没这一条）⇒ 表里**仍会留一条有名字的**记录
                //   （`UI_Button_Round_background → UI_Button_Round_background_pressed`）—— 那是「如实出声」
                //   那一档、不是缺陷（口径见 `WindowButton.MissingPressedArt` 的注释）。
                //   ⚠️ **如实记一处偏离**：原版那一格 `P=40k_general_bt_yellow_pressed`（**本地有这张图**），
                //   我们**没接** —— 与同族 5 颗关闭钮一致，都走 `Press()` 的「取不到按下图 ⇒ 退回高亮图」；
                //   ⛔ 不在 A810③ 的口径内，本件不动（要接 = 另立一笔账）。
                //   🔴 **2026-10-18 更正（A1058 · 第六会话批 2 · 铁律 5）**：上面那条 A810③ 把
                //   `target=5609434692533257010` 读成了「**圆底**」—— **那句是错的**。把那个 pid 解出来
                //   （`python -I d:/tmp/wf_hit/tgt.py bundle_menus_assets_all 5609434692533257010`）⇒
                //   **所属 GO 名 = `Background`、贴图 = `40k_general_bt_yellow`**（= **黄面**那一层，⛔ 不是圆底）。
                //   旁证：`rcunion.py bundle_menus_assets_all "Inbox Menu" --depth 8` 那三行 ——
                //   根 `Image[UI_Button_Round_background RT=0]`（圆底盘**不吃射线**）、
                //   子件 `Background[40k_general_bt_yellow RT=1 pad(-20)⁴]`、子件 `Icon[40k_general_bt_yellow_close RT=1 pad(-20)⁴]`。
                //   ⇒ 换图目标改成**画着 `40k_general_bt_yellow` 的那颗 quad**（🔴 **2026-10-18（B3）起它的
                //   节点名就是原版的 `Background`** —— 改前那个错位的名字是 `Icon`，见上面 `Build()` 里那段），
                //   `art` = 它的**常态图名** `ArtCloseIcon` ⇒ 按下图也顺带推出来了
                //   （`PressedNames`: `40k_general_bt_yellow → 40k_general_bt_yellow_pressed`，与 `P` 那一格逐字相同）
                //   ⇒ `MissingPressedArt` 里那条 `UI_Button_Round_background → …_pressed` **不再产生**。
                //
                // 🔴 **2026-10-18（A1053 · 第六会话批 2）：这一处**原来**没有独立命中区** ——
                //   `WindowButton` 是**直接挂在【画出来的叉】那颗 quad 上**的（`AddComponent`），
                //   于是命中区 = 子件矩形 56.86×58.13、**每边比原版小 20**。
                //   原版吃射线的是**同矩形的两颗子件**按各自的 `m_RaycastPadding (-20)⁴` 外扩
                //   ⇒ **96.86 × 98.13**（根那颗 `UI_Button_Round_background` 带 `m_RaycastTarget=0`
                //   ⇒ 它**不吃射线**）。⇒ 改走公共件 `MenuDraw.Hit` 另建一颗**透明命中区**
                //   （同族的 16 颗关窗钮全是这个写法：`LeaderboardWindow` / `TrophyInfoPopup` /
                //    `BaseOfferPopup` / `PlayerProfileWindow` …），⛔ 那颗**画出来的叉不再兼命中区**。
                //   判据 = `python -I d:/tmp/wf_hit/rcpad.py bundle_menus_assets_all "Inbox Menu" --depth 8
                //   --substr "Generic Close Button"`（实读 `96.86 x 98.13`）。
                //   ⚠️ 队列仍用本窗内容命中区那一档 `QOverlay`(3014)（⛔ 保持不变：压暗层那条
                //   「`QShade`(3002) 严格低于内容命中区档」的不变量由 `RewardsScene` 那条断言钉着）。
                var closeHit = MenuDraw.Hit(close, "Hit", MenuDraw.PaddedRect(CloseBg, ClosePad),
                                            QOverlay, () => Close(), closeFaceQ, ArtCloseIcon,
                                            "40k_general_bt_yellow_hover",
                                            clickCue: WindowSounds.DefaultCloseCue);
                if (closeHit == null)
                    Debug.LogWarning("[Inbox] 关窗钮的命中区没建出来（`MenuDraw.Hit` 返回 null）"
                                   + " ⇒ **点它关不了窗**（只能点窗外或 ESC）。");
            }

            // ✅ `Reset Button` 已在本函数前段**建壳并 `SetActive(false)`**（原版运行态；⛔ 不是不建、也不是显示）

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Inbox] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        /// <summary>空态/非空态那两条分支（**原版 `InboxWindow__Open.c:49`**）：空 ⇒ 开 `No News Warning`、
        /// 关 `Message Display`；非空 ⇒ 反过来。`Build()` 与 `Initialize()` 都走这一份（⛔ 别两处各写一遍）。
        /// ⚠️ 本机恒空（`DailyData.InboxCount` = 0）⇒ 那句出声日志照打，并把列表侧的真实状态说清。</summary>
        void ApplyEmptyState()
        {
            bool has = HasMessages;
            if (_display != null) _display.gameObject.SetActive(has);
            if (_warn != null) _warn.gameObject.SetActive(!has);
            if (!has)
                Debug.Log("[Inbox] 没有消息 ⇒ **空态**：关 `Message Display`、开 `No News Warning`"
                          + "（原版 `InboxWindow__Open.c:49` 就是这条分支）；"
                          + "`Message List` 的**结构/裁切/滚动照原版建全**（`Viewport` + `RectMask2D` 等效物 "
                          + "(0,25) + `MenuScroll`），只是**没有条目**。"
                          + "⚠️ 2026-10-25 起消息来自 `InboxData`（本地消息源，两条出厂消息）⇒ 正常情况下这条分支**不该再走到**；"
                          + "走到这里说明消息表空了（自检可以灌空表造这一态）。");
        }

        // ============================================================ 🆕 2026-10-25：数据源 → 列表 / 正文

        /// <summary>从 `InboxData` 灌消息（原版 `InboxWindow__Open` 取 `handler.Messages` 那一跳的等价物）。
        /// <para>字段对位：`Title`=`GetTitle()`（`InboxData.Msg.TitleText` 优先、词条键在 `TitleTerm` 时已解析）、
        /// `Date`=`FormatDate(StartDate())`、`Unread`=`!IsRead()`、`Body`=`GetMessage()`、
        /// `Claimable`=`CheckMessageCollectable()`、`Id`=`eventId`。</para>
        /// <para>选中项：原版 `GetCurrentMessage()` =「最后一条**已读**，否则第一条」
        /// （`InboxWindow.cs:293-317`）⇒ 我们照它；若 `_selectedId` 还在（滚动重建）就保持玩家选的那条。</para></summary>
        void LoadFromStore()
        {
            _animPlayed = false;                     // 换了一批消息 ⇒ 重新入场（见 StartAnimateIn）
            _shineTimer = 0f; _shineNext = 0f;       // B9_W51SHINE2：换消息 ⇒ 扫光也从第一帧重来（原版每条都是新条目实例、计时器归零）
            _messages.Clear();
            var src = InboxData.Messages;
            for (int i = 0; i < src.Count; i++)
            {
                var m = src[i];
                _messages.Add(new Message(InboxData.Title(m), InboxData.FormatDate(m.StartMs), !m.Read)
                {
                    Id = m.Id,
                    Body = InboxData.Body(m),
                    HeaderArt = m.HeaderArt,
                    FooterArt = m.FooterArt,
                    RedeemableAtMs = m.RedeemableAtMs,
                    Claimable = InboxData.CanCollect(m),
                });
            }
            _current = IndexOfCurrent();
        }

        /// <summary>原版 `GetCurrentMessage()` 的等价：先找 `_selectedId`（重建时的粘性），
        /// 再落到 `InboxData.CurrentMessage`（=「最后一条已读，否则第一条」，判据见那个属性）。
        /// 🔴 **派生规则只留一处**：⛔ 别在这里再写一遍 `for (i = Count-1; …)`。</summary>
        int IndexOfCurrent()
        {
            if (!string.IsNullOrEmpty(_selectedId))
                for (int i = 0; i < _messages.Count; i++)
                    if (_messages[i].Id == _selectedId) return i;
            var cur = InboxData.CurrentMessage;
            if (cur != null)
                for (int i = 0; i < _messages.Count; i++)
                    if (_messages[i].Id == cur.Id) return i;
            return _messages.Count > 0 ? 0 : -1;
        }

        /// <summary>点一行 = 原版 `MessageContainer.Select()` → `UiContainer.HandleClick()` →
        /// `OnEventClick` → `MessageList.OnContainerClicked`：取消上一条选中 + 本条 `ToggleSelection(true)` +
        /// `OnMessageSelected(message)` → `InboxWindow.OpenMessage` → `display.Initialize(message)`
        /// （`MessageContainer.cs:106-121` · `InboxWindow.cs:319-322`）。
        /// <para>另外把「已读」记上：原版那一下走云脚本 `CloudHandle.OpenInbox` 再由
        /// `UiBadgeNotificationManager.Refresh&lt;InboxBadge&gt;()` 刷红点（`GameNewsEvent.SelectEvent`）。
        /// 本地等价 = `InboxData.MarkRead`，红点由 `InboxCount` 现算（见 `DailyData.InboxCount`）。</para></summary>
        void SelectMessage(int index)
        {
            if (index < 0 || index >= _messages.Count) return;
            _current = index;
            var m = _messages[index];
            _selectedId = m.Id;
            if (!string.IsNullOrEmpty(m.Id))
            {
                var src = InboxData.Find(m.Id);
                if (src != null && !src.Read) { InboxData.MarkRead(src); m.Unread = false; }
            }
            RefreshSelection();
            ShowDetail(m);
            // 未读标记会变 ⇒ 行上的 `New!` 要跟着重建（原版 `MessageContainer.Refresh()`）
            BuildMessageRows();
        }

        /// <summary>换底图（原版 `MessageContainer.ToggleSelection(bool selected)` =
        /// `background.sprite = selected ? selectedMessageSprite : normalMessageSprite`）。
        /// 两张图 = `40K_settings_button`（常态）与 `40K_settings_button_selected`（选中）；
        /// 🔴 2026-10-26（`F1`·β2）：换底图**必须同时**改这颗钮的 `_normalTex` —— 见 `SetRowNormalTex`。</summary>
        void RefreshSelection()
        {
            for (int i = 0; i < _rowBgs.Length && i < _rowNodes.Length; i++)
            {
                var q = _rowBgs[i];
                if (q == null) continue;
                bool sel = i == _current;
                var tex = Art(sel ? ArtRowSelected : ArtRowBg);   // ⚠️ `_selected` 那张在 `WindowButton.HoverNames` 里只被 `ChatPanel` 页签用，⛔ 别让表去推
                if (tex != null) { q.SetTexture(tex); SetRowNormalTex(i, tex); }
                q.SetTint(sel ? Color.white : RowBgTint);
            }
        }

        /// <summary>原版 `InboxWindow.OpenMessage(message)` = `display.Initialize(message)`。
        /// `MessageDisplay.Initialize` 的六步（`MessageDisplay.cs:80-198`）逐条对位：
        /// ① 标题 = `GetTitle()`；② 正文 = `GetMessage()`；③ 头/尾图按 `HasAsset` 开关；
        /// ④ `rewardWarningText.SetActive(false)`；⑤ `hasReward` → `claimButtonHolder.SetActive` +
        /// `collectButton.interactable = hasReward &amp; CheckMessageCollectable()`；⑥ `layoutFixer.FixLayout()`。
        /// ⚠️ 第 ⑥ 步（uGUI 布局组重排）在我们的手算布局里**没有对应物**（每次都用固定框建）⇒ 如实不写。</summary>
        void ShowDetail(Message m)
        {
            if (m == null) { if (_display != null) _display.gameObject.SetActive(false); return; }
            if (_display != null) _display.gameObject.SetActive(true);
            if (_warn != null) _warn.gameObject.SetActive(false);
            if (_mdTitleLb != null) { _mdTitleLb.SetText(m.Title ?? ""); TitleLeft(_mdTitleLb, MdTitle); }   // 🔴 2026-10-26（F1·β3）：`SetText` 会以**新宽度**重新居中（`Label.RefreshBounds`）⇒ 必须再左对齐一次（`TitleLeft` 的那个平移是按**旧宽**算的）
            if (_mdBody != null) _mdBody.SetText(m.Body ?? "");
            bool hh = !string.IsNullOrEmpty(m.HeaderArt);
            bool hf = !string.IsNullOrEmpty(m.FooterArt);
            if (_mdHeader != null)
            {
                _mdHeader.gameObject.SetActive(hh);
                if (hh) MenuDraw.Rect(_mdHeader, Art(m.HeaderArt), MdHeader, "Image", QContent, null, true);
            }
            if (_mdFooter != null)
            {
                _mdFooter.gameObject.SetActive(hf);
                if (hf) MenuDraw.Rect(_mdFooter, Art(m.FooterArt), MdFooter, "Image", QContent, null, true);
            }
            // Claim 那半：原版 `claimButtonHolder.SetActive(hasReward)`（容器整块），
            // 且 `interactable = hasReward & CheckMessageCollectable()`（钮的灰/亮）。
            // ⚠️ 我们的 `Claim Button`（容器）与钮都建在 `Build()` 里 ⇒ 这里只切 **容器**的显隐 + 钮的交互态。
            var box = _display != null ? _display.transform.Find("Content/Scroll View/Viewport/Content/Claim Button") : null;
            if (box != null) box.gameObject.SetActive(m.Claimable || InboxData.HasReward(m.Id));
            var btn = box != null ? box.Find("Claim Button") : null;
            var wb = btn != null ? btn.GetComponentInChildren<WindowButton>() : null;
            if (wb != null) wb.Interactable = m.Claimable;
        }


        // ============================================================ 🆕 2026-10-18：两颗「原版有、我们刚建」的钮

        /// <summary>`Reset Button` 的点击 —— **原版这颗是死钮**（`m_OnClick.m_Calls` 空 +
        /// `InboxWindow__DebugReset` 零调用者 + `Open()` 首句就把它 `SetActive(false)`）。
        /// 我们照原版**不接任何行为**，但**不静默**：点得到的话把「为什么什么都不发生」说出来。</summary>
        void OnResetClick()
        {
            Debug.Log("[Inbox] `Reset Button` 点了 ⇒ **什么都不做**（原版这颗钮：`m_OnClick.m_Calls` 空 · "
                    + "`InboxWindow__DebugReset` 全量反编译零调用者 · `Open()` 首句 `SetActive(false)` ⇒ "
                    + "运行时根本不该看得见它；本地也没有 `LiveOpsManager` 的事件可重置）");
        }

        /// <summary>`Claim Button` 的点击 —— 原版 `MessageDisplay.OnEnable` 把 `TryCollect` 挂在这颗钮上
        /// （`MessageDisplay.cs:291-296`），`TryCollect` = 先 `interactable = false` 再走
        /// `message.ClaimRewards()`（云脚本 → `RewardService.Collect`）。
        /// <para>本地等价：`InboxData.TryClaim(当前消息)` —— 发 `Wallet.Grant` + 开领奖窗 + 置已领 + 落盘，
        /// 完成后重建行/正文（原版 `&lt;ClaimRewards&gt;g__OnComplete|7_0` 之后界面那半由事件刷新）。
        /// ⚠️ **不静的失败**：不可领时把 `why` 原样说出来（原版是服务端回错）。</para></summary>
        void OnClaimClick()
        {
            var m = (_current >= 0 && _current < _messages.Count) ? _messages[_current] : null;
            var src = m != null ? InboxData.Find(m.Id) : null;
            if (src == null)
            {
                Debug.LogWarning("[Inbox] `Claim Button` 点了 ⇒ **当前没有可领的消息**（选中的那条没有数据源 id；"
                               + "自检灌的假消息就会走到这里）。");
                return;
            }
            string why;
            if (!InboxData.TryClaim(src, out why))
            {
                Debug.LogWarning("[Inbox] `Claim Button` 点了 ⇒ **领不到**：" + why);
                return;
            }
            // 领完这一拍：这条从「可领」变成「已领」⇒ 正文里的 Claim 容器要收起、行要重铺（原版刷新那半）。
            m.Claimable = false;
            ShowDetail(m);
            BuildMessageRows();
        }

        // ============================================================ 消息列表（`Message List`）

        /// <summary>**照原版 `MessageList.Initialize(messages)`** —— 原版由 `InboxWindow__Open` 在
        /// 「非空」那条分支上调它（`:49` 那两句的 else 支）。⚠️ 单机路径**不会**走到（没有服务端数据源）；
        /// 本方法在那儿是**生产 API**（将来接了真数据源，`Build()` 里那句 `_messages.Clear()` 换成喂真数据即可
        /// —— 原版就是这个形状）。`null`/空 = 回到空态。</summary>
        public void Initialize(System.Collections.Generic.IList<Message> messages)
        {
            _animPlayed = false;                     // 同上：生产 API 灌一批新消息 ⇒ 重新入场
            _shineTimer = 0f; _shineNext = 0f;       // B9_W51SHINE2：同上（扫光也从头，见 LoadFromStore 那一句）
            _messages.Clear();
            if (messages != null)
                for (int i = 0; i < messages.Count; i++) if (messages[i] != null) _messages.Add(messages[i]);
            _current = IndexOfCurrent();
            _selectedId = _current >= 0 ? _messages[_current].Id : null;
            BuildMessageList();
            ApplyEmptyState();          // 照原版：有消息 ⇒ 右侧 `Message Display` 跟着开
            // 原版 `InboxWindow__Open`：非空那一支是 `display.Initialize(当前) + list.Initialize(messages, 当前)`。
            ShowDetail(_current >= 0 ? _messages[_current] : null);
        }

        /// <summary>建（或重建）列表的**滚动区**+ 内容容器的高度。
        /// 内容高 = `n × 条目高 + (n−1) × spacing`（原版 `ContentSizeFitter m_VerticalFit = 2(MinSize)`
        /// + `VerticalLayoutGroup spacing = 7`）；内容**上对齐**（`align = 0 UpperLeft`）。
        /// 🔴 重建时先 `UnregisterOwnedBy` 把旧的撤掉（`PointerLayer` 的登记表**只增不减**是踩过的雷）。</summary>
        void BuildMessageList()
        {
            if (_listContent == null) return;
            float contentH = _messages.Count <= 0 ? 0f : _messages.Count * RowPitch - RowSpacing;
            _listScroll = MenuScroll.TopAligned(MsgList, contentH, 1f);   // 原版 `Inbox Menu/Content/Message List` = 1.0（D_N29）
            _listScroll.Owner = gameObject;
            // 偏移一变就重建条目（**`OnChanged` 指向幂等函数**：先清后建 —— 规格 §四 第 1 条那个坑）
            _listScroll.OnChanged = BuildMessageRows;
            PointerLayer.UnregisterOwnedBy(gameObject);
            PointerLayer.RegisterScroll(_listScroll);
            BuildMessageRows();
        }

        /// <summary>铺条目。🔴 **树序 = 数据序的倒序** —— 原版那层 `VerticalLayoutGroup` 的
        /// **`m_ReverseArrangement = 1`**（`menu_dump` 那一行 `**reverse=1**（主轴按树序倒排：最后一个子件在最左/最上）`）
        /// ⇒ 视觉第 0 行（最上）= 树的**最后一个**子件。所以这里**从数据末尾往前建**，
        /// 每个条目按**自己的数据下标**算 y（`y = 视口顶 + i × (条目高 + 7)`）—— 两条算下来语义是
        /// 「最新那条（列表尾）在最上面」，与原版的倒排一致。
        /// ⚠️ 条目**节点全建**（`Rows.Count` 恒 = 消息数）；整块落在视口外的那些**子件**由 `ClipRect` 各自
        /// 「不建」—— 这与原版「实例化全部条目、由掩码裁像素」同形。</summary>
        void BuildMessageRows()
        {
            if (_listContent == null) return;
            for (int i = _listContent.childCount - 1; i >= 0; i--)
                RewardsWindow.DestroySafe(_listContent.GetChild(i).gameObject);
            Rows.Clear();
            // 🆕 2026-10-25：选中态/动画那三张登记表**每次重建都重填**（按数据下标索引，见字段注释）
            _rowBgs = new ImageQuad[_messages.Count];
            _rowNodes = new Transform[_messages.Count];
            _rowShine = new WildcardShineLayer[_messages.Count];

            // 🔴🔴 **2026-10-13（A435 甲 · A198② 阶段 2）：那六行没了。** 旧写法 = 「存 `Clip`/`ClipSoftness`/
            //   `ClipPad` 三件 → 设 `MsgList` / `ListSoft(0,25)` / `ListPad(0,0,0,0)` → 建完成对还原」。
            //   现在状态长在**视口节点**上（`Build()` 里 `ViewportClip.Hang(msgList, "Viewport", MsgList,
            //   ListPad, ListSoft)` 那一句已经把框 / pad / soft 一次写死）⇒ 本函数**一个字都不设**
            //   （节点是**常驻**状态 —— 原版 `RectMask2D` 就长在视口上、不会每次重排被打一次再还原）。
            //   🔴 **改坏法**（现在唯一能红的地方）= 改 `Build()` 里 `ViewportClip.Hang(…)` 那两个实参；
            //      ⛔ 别再把那六行加回来 —— 那会让 `Editor/RewardsScene.cs` 的 A489
            //      （`NodeShadowedByParam == 0`）红。
            for (int i = _messages.Count - 1; i >= 0; i--)       // 树序倒排，见本方法的注释
            {
                float y1 = MsgList.y1 + i * RowPitch;            // 内容坐标（第 i 条在视觉第 i 行）
                var r = new PxRect(MsgList.x1, y1, MsgList.x1 + RowW, y1 + RowH);
                if (_listScroll != null) r = _listScroll.Shift(r);
                Rows.Add(BuildRow(_listContent, r, i, _messages[i]));
            }
            // 🆕 **2026-10-25（N17）**：重建之后两件事 ——
            //   ① 复原选中态（原版选中态长在条目实例上；我们每次重建 ⇒ 自己按 `_current` 重画）；
            //   ② 播入场（原版 `MessageList.Initialize` 里逐个 `AnimateIn(index)`）。
            RefreshSelection();
            StartAnimateIn();
        }

        // ============================================================ 🆕 2026-10-25：入场动画 + 未读行闪一下

        /// <summary>原版 `MessageContainer.AnimateIn(int index)`（`MessageContainer.cs:130-170`）：
        /// `canvasGroup.alpha = 0` → `AppendInterval((index + 1) × animationDelay)` → 播 slotting 音 →
        /// `DOFade(1, animationTime)` + `DOScale(底图, localScale).From(localScale × 0.5)`（带 curve 缓动）。
        /// <para>参数：`animationDelay = 0.1` · `animationTime = 0.15` · 缩放起点 = 原值 × **0.5**
        /// （见 `AnimDelay` / `AnimTime` / `AnimScaleFrom` 的注释；曲线逐键见类尾 `AnimCurve`，
        /// ⚠️ 它**只作用在 `DOScale`** 上 —— `DOFade` 那条没有 `SetEase`，走 DOTween 默认 `Ease.OutQuad`）。</para>
        /// <para>⚠️ 批处理下没有帧循环 ⇒ 与 `RewardWindow` 同一个做法：**本窗自带 `Tick(dt)`**，
        /// `Update()` 只是把 `Time.deltaTime` 转进去（自检可以直调 `Tick`）。</para></summary>
        void StartAnimateIn()
        {
            // 🔴 只播**一次**：行是每次滚动/选中都重建的（`BuildMessageRows` 的幂等设计），
            //   原版只在 `MessageList.Initialize` 那一刻 `AnimateIn` 一遍 ⇒ 这里用一个「播过没有」的位挡住重播
            //   （`LoadFromStore` / `Initialize` 会把它清掉 = 换了一批消息才重新入场）。
            if (_animPlayed || !Application.isPlaying) { for (int i = 0; i < _rowNodes.Length; i++) ApplyRowAnim(i, 1f, 1f); return; }   // 🔴 2026-10-26（B8_BN）：批处理/编辑器**没有帧循环** ⇒ `Tick` 一次都不会被推 ⇒ 入场动画**根本播不动**，行会**永久停在起点**（`localScale = AnimScaleFrom = 0.5`、底图 alpha 0）。实测代价：`Editor/ShellScene.cs` 的 A743 三态量到的文字右沿恰好是「全尺寸值 ×0.5 绕格心」= 571.10 / 429.32 / 684.38（原版全尺寸下是 728.28 / cutX / 954.83）⇒ 三条红，且建场截图里条目是**半尺寸 + 透明**。⇒ 非 Play 直接落到终态（`p = 1`）。⚠️ 同族口径：`Shell/CollectionWindow.cs:1337` 的「生产路径用 `Application.isPlaying`」· `Shell/DraftModeWindow.cs:2548` 的 `if (!Application.isPlaying) { node.localScale = Vector3.one; return; }`
            _animPlayed = true;
            _animRows = _messages.Count;
            _animT = _animRows > 0 ? 0f : -1f;
            // 起点：全透明 + 半尺寸（原版先写 alpha 0、再在 interval 之后才动；我们这里先把**初值**摆好，
            // `Tick` 里按「每行自己的 0.15s 窗口」推进 —— 等价于原版那条 Sequence）。
            for (int i = 0; i < _rowNodes.Length; i++) ApplyRowAnim(i, 0f, 0f); _animSoundPlayed = new bool[_rowNodes.Length];   // 🆕 2026-10-28 B92_N6：每行一次的 slotting 音闸（原版每颗 `MessageContainer` 各播一次；判据 → 文件尾 `PlaySlottingIfVisible`）
        }

        /// <summary>按进度摆一行的两个量。**两个进度分开传**（原版的缓动**只挂在缩放上**，淡入是另一条 ease）：
        /// <paramref name="pFade"/> = 底图 alpha 的进度（`Ease.OutQuad`）·
        /// <paramref name="pScale"/> = 缩放进度（原版 `curve.Evaluate`，见类尾 `AnimCurve`）。</summary>
        void ApplyRowAnim(int i, float pFade, float pScale)
        {
            if (i < 0 || i >= _rowNodes.Length) return;
            var node = _rowNodes[i];
            if (node != null)
            {
                float s = Mathf.Lerp(AnimScaleFrom, 1f, pScale);
                node.localScale = new Vector3(s, s, 1f);
            }
            float a = Mathf.Clamp01(pFade);
            var q = i < _rowBgs.Length ? _rowBgs[i] : null;
            if (q != null)
            {
                var c = q.Tint; c.a = (i == _current ? 1f : RowBgTint.a) * a; q.SetTint(c);
            }
        }

        void AnimTick(float dt)
        {
            if (_animT < 0f) return;
            _animT += dt;
            for (int i = 0; i < _rowNodes.Length; i++)
            {
                // 第 i 行：延迟 (i + 1) × AnimDelay 之后开始，AnimTime 内推到 1（淡入 = OutQuad · 缩放 = 原版 curve）。
                float tc = Mathf.Clamp01((_animT - (i + 1) * AnimDelay) / AnimTime);
                float pf = 1f - (1f - tc) * (1f - tc);                  // 淡入：DOTween 默认 `Ease.OutQuad`（原版 `DOFade` 没 SetEase）
                ApplyRowAnim(i, pf, AnimCurveEase(tc)); if (i < _animSoundPlayed.Length && !_animSoundPlayed[i] && _animT >= (i + 1) * AnimDelay) { _animSoundPlayed[i] = true; PlaySlottingIfVisible(i); }   // 缩放：原版 `SetEase(curve)` —— **只挂在 DOScale 上**｜🆕 2026-10-28 B92_N6：该行窗口刚开那一拍播 slotting 音（原版 `AnimateIn` 的 `AppendCallback`；判据与 cue → 文件尾 `PlaySlottingIfVisible`）
            }
            if (_animT > (AnimRows + 1f) * AnimDelay + AnimTime)
            {
                for (int i = 0; i < _rowNodes.Length; i++) ApplyRowAnim(i, 1f, 1f);
                RefreshSelection();          // 动画把 tint 写过 ⇒ 收尾按选中态再刷一次（含底图）
                _animT = -1f;
            }
        }

        int AnimRows { get { return _animRows; } }

        /// <summary>原版 `MessageContainer.Update` 的「未读行扫一下」：`timeSinceLastShine` 累加到
        /// `shineTime` 就重置并把 `shineTime` 摇成 **3.0 × (1 + Random(-0.2, 0.2))**、播那颗 `shinyEffect`
        /// （真扫光；参数/偏差口径见 `RowShineParams` 与那一整段注释）。</summary>
        void ShineTick(float dt)
        {
            _shineTimer += dt;
            if (_shineTimer < _shineNext) return;
            _shineTimer = 0f;
            _shineNext = ShineBase * (1f + ((float)_rng.NextDouble() * 2f - 1f) * ShineJitter);
            PlayRowShine();
        }

        /// <summary>本窗的动画时钟（入场 + 未读闪共用；由 `Update` 推）。</summary>
        public void Tick(float dt)
        {
            AnimTick(dt);
            ShineTick(dt);
        }

        void Update() { Tick(Time.deltaTime); }

        /// <summary>一个条目（原版 `Message Container`）：底图 + `Title` + `Date` + [`New!`]。
        /// 三件几何/字号/颜色/对齐**逐条照 dump**（`Row*` 常量）。
        /// 🔴 **2026-10-09（A273）就地订正（铁律 5）**：这一段原来写「**不接点击** ⇒ 于是也**不做悬停换图**
        /// （没按钮就没它）」—— **前半句仍然成立、后半句是错的**：
        /// 悬停换图**不依赖「点了有事做」**，它就是那条 `Selectable`（`m_Transition = 2` SpriteSwap）
        /// 的**悬停那一档**，所以它是一条**真的缺漏**（A273）。现在：底图接上悬停换图
        /// （`Bind` 挂在**底图那颗 quad** 上、命中区是另一颗透明 quad，见下），**点击仍如实出声**。
        /// <para>⚠️ **点击这一半仍未做**：原版点条目 = 选中它、右侧 `Message Display` 显示正文 ——
        /// 而本窗正文的 `Header`/`Message` **没建**（服务端数据驱动；`Claim Button` 自 2026-10-18 起已建壳，
        /// 但没有消息 ⇒ 空态下整块关着）⇒ 这一下**没有画面变化**。按本仓对「没做的功能」的既有做法（先例：主菜单的 `ReplayButton`
        /// = 「点了会**出声说回放没做**」）挂一句 `Debug.LogWarning` —— ⛔ **不挂空 lambda**
        /// （那才是「假件」：点了什么都没发生、也不出声）。</para></summary>
        Transform BuildRow(Transform parent, PxRect row, int idx, Message m)
        {
            var e = MenuDraw.Node(parent, "Message Container_" + idx, row);
            // 🔴 **条目局部坐标（原点 = 条目左上角）→ 画布坐标**：`row` 已经是**屏幕坐标**（含滚动偏移）
            //    ⇒ 加的是 `row.x1 / row.y1` 这两个**绝对**数，⛔ 不是「相对视口的偏移」
            //    （`MenuDraw.Rect` / `ImageQuad.Create` 吃的是**画布坐标**，内部自己减父节点世界位置；
            //     写成 `row.x1 − MsgList.x1` 会让整条带子偏掉「视口左上角那一段」，而矩形断言**量不到内部的相对摆位**）。
            float dx = row.x1, dy = row.y1;
            // ⚠️ C# 的**局部函数不能重载** ⇒ 两个名字（坐标版 / 矩形版），别合成一个。
            PxRect O(float x1, float y1, float x2, float y2)
                { return new PxRect(dx + x1, dy + y1, dx + x2, dy + y2); }
            PxRect OR(PxRect r) { return O(r.x1, r.y1, r.x2, r.y2); }

            var bg = DrawRect(e, Art(ArtRowBg), OR(RowBg), "Background", QPanel, RowBgTint);
            // 🆕 **2026-10-09（A273）**：条目接上**悬停换图**（原版那颗 `Selectable` 是 `trans=2` SpriteSwap）。
            //   · **换图那一层 = 底图那颗 quad**（原版 `m_TargetGraphic` 指的就是 `Content` 自己那张 `Image`
            //     —— 这颗 `Background` 就是它的等价物），而**命中区是另一颗透明 quad**（本仓通用形状，
            //     `MenuDraw.Hit` 建的）⇒ 命中区可以单独放到高档、底图仍旧按 `QPanel` 画在文字之下。
            //   · 🔴 **命中区档 = `QRowHit`（`QOverlay`）**：低于它就会被本窗吸收层（3013）整个盖掉 ——
            //     那样悬停**永远不会发生**（判据 → `QRowHit` 的注释）。改回 `QPanel` ⇒ 自检那条
            //     「真鼠标悬停命中的就是这一颗」立刻红。
            //   · 🔴 **悬停图必须显式传 `ArtRowHover`**（⛔ 不能靠 `WindowButton.HoverNames` 那张表推）——
            //     判据 → `ArtRowHover` 的注释（同一张常态图在别处配的是 `_selected`）。
            //   · `onClick` **如实出声**（正文那几件没建，见上面 BuildRow 的注释）。
            //   · `onClick` —— 🔴 **2026-10-25（N17）起是真动作**：原版 `MessageContainer.Select()` →
            //     `HandleClick` → `MessageList.OnContainerClicked` → `InboxWindow.OpenMessage` →
            //     `display.Initialize(message)`。我们走 `SelectMessage(idx)`（选中 + 标已读 + 刷红点 + 填正文）。
            if (bg != null)
            {
                int captured = idx;                      // ⚠️ 闭包捕获：`idx` 是每行自己的数据下标
                AddHit(e, RowHitName, OR(RowBg), QRowHit,
                       () => SelectMessage(captured),
                       bg, ArtRowBg, ArtRowHover, ArtRowPressed);
            }
            // 🆕 选中态要用的三样（原版 `MessageContainer.background` 那张 `Image` / 条目节点 / `unreadMark`）：
            //   行是**每次重建**的（滚动一偏就重铺）⇒ 登记表也每次重填（`BuildMessageRows` 首句按消息数重分配）。
            //   ⚠️ **按 `idx` 下标写**（⛔ 不能 `Add` —— 行是倒序建的，`Add` 会与 `_messages[i]` 错位）。
            if (idx >= 0 && idx < _rowNodes.Length)
            {
                _rowBgs[idx] = bg;
                _rowNodes[idx] = e;
            }
            // `bg == null` 有**两种**来路，只有一种要出声：
            //   · 整条落在视口外 ⇒ `MenuDraw.Rect` 的 `ClipRect` **本来就不建**（正确裁切，不是失效）；
            //   · 底图名取不到 ⇒ 走的是 `Art()`，它已经记进 `MissingArt`（自检那条 `Check(…MissingArt.Count, 0)` 盯着）。
            // ⛔ 别在这里无条件出声 —— 视口外那些条目会刷一屏假警告。
            // 原版三件的对齐（dump 实读）：`Title` / `Date` 是 `Left/Capline`·`Left/Middle`、
            // `New` 是 `Right/Middle`。⚠️ `Label` 建出来默认是**居中**在锚点上 ⇒ 对齐要显式给。
            // 🆕 **2026-10-12（A459）**：三处各补一个 `autoBasePx`（= 原版 `m_fontSizeBase` **41.0**，判据见
            //   `RowTitleAutoBase` 那一行的注释）。⛔ 命名实参（`align` 是 `int` 位置参，插在它前面会静默绑错，
            //   见 `GameWindow.Text` 的头注释）；`autoMaxPx` **不传**（`<= 0` ⇒ 退回 `fontPx`，与原版 max 等价）。
            // 🆕 **2026-10-12（A467）**：改走 `RowText`（多一步「还原原版的 `折行=0`」，且对齐要排在它**之后**，
            //   见那个方法的头注释）—— 三个调用点的**期望值一个都没变**（对齐仍是 1 / 1 / 2）。
            RowText(e, OR(RowTitle), m.Title ?? "", RowTitleColor, "Title", RowTitleFont,
                    RowTitleAutoMin, RowTitleAutoBase, 1, Label.VAlign.Capline);   // A712 阶段 2：原版 `Left/Capline`
            RowText(e, OR(RowDate), m.Date ?? "", RowDateColor, "Date", RowDateFont,
                    RowDateAutoMin, RowDateAutoBase, 1);
            if (m.Unread)
                RowText(e, OR(RowNew), TxtUnread, RowNewColor, "New", RowNewFont,
                        RowNewAutoMin, RowNewAutoBase, 2);
            return e;
        }

        /// <summary>🆕 **2026-10-12（A470）**：把一颗**窗级 `Title`** 左对齐到它自己框的左沿 ——
        /// 本窗**两颗**（`Content/Title` · `Content/Message Display/Title`）原版都是
        /// **`m_HorizontalAlignment = 1 (Left)`**，我们原来是**框心居中**（`Title` 之外那颗也走它）。
        /// <para>判据（**本件亲跑**，读数逐字抄自输出 ——
        /// `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Inbox Menu" --depth 6 --md --no-sprite`）：
        ///   · `Inbox Menu/Content/Title` = `'Inbox' 字号=48.0 基准=36.0 auto[18.0~48.0] 对齐=**Left**/Middle 折行=0 色=(1,1,1,1)`；
        ///   · `…/Content/Message Display/Title` = `'WELCOME TO WARPFORGE CLOSED ALPHA!' 字号=58.0 基准=36.0
        ///     auto[10.0~58.0] 对齐=**Left**/Middle 折行=0 色=(0.953,0.663,0.404,1)`。
        /// 列名 → 字段的映射：`工具/menu_dump.py:609-610` 直接打印 `m_HorizontalAlignment` / `m_VerticalAlignment`，
        /// 枚举表在 `:100`（`H_ALIGN = {1:'Left', 2:'Center', 4:'Right', …}` ⇒ `Left` 就是 **1**）。
        /// ⚠️ 同窗第三颗 `No News Warning` 原版是 **`Center/Middle`** ⇒ **与我们一致、不动**（它只缺字距，见 `NoNewsCharSpacing`）。
        /// </para>
        /// <para>⚠️ **不传对齐就是居中**：`MenuDraw.Text` 根本没有对齐形参，而 `TmpFont.NewText` 把**所有** TMP
        /// 统一建成 `Center`（`Core/TmpFont.cs` 的 `NewText` 里 `t.alignment = TextAlignmentOptions.Center` 那一句）⇒ 这两颗原来都落在框心（`Inbox` 那颗往右偏
        /// `(框宽 250 − 文字宽)/2` px；`Message Display` 那颗往右偏 `(959.77 − 文字宽)/2` px）。</para>
        /// <para>🔴 **必须排在 `SetAutoFitBox` / `SetWrapping(false)` 【之后】**：`MenuDraw.AlignLeft` 走
        /// `Label.AlignLeftOn`，它按**当时的 `WorldW`**（刚量到的渲染宽）反推整块的位置，而
        /// `SetAutoFitBox` / `SetWrapping` 都会改字号与量到的宽度 ⇒ 先对齐、后改折行 = 左缘被推走
        /// （同族坑与判据：`Battle/Label.cs` · `LiveOpsEventWindow` 那处注释「顺序不能反」·
        /// 本文件 `RowText` 的同一段推理）。垂直那一半（`Middle`）**本来就是对的** —— `RefreshBounds`
        /// 会把整块**居中**摆进矩形（`Battle/Label.cs` 的 `RefreshBounds`）⇒ 这里只管水平。</para>
        /// <para>⚠️ 走的是**现成的口**（`Shell/MenuDraw.cs` 的 `AlignLeft` 的 `MenuDraw.AlignLeft`），⛔ 没有就地重写一份；
        /// 点阵后端（`_tmp == null`）`AlignLeftOn` 直接返回 ⇒ **什么都不做**（如实：那后端没有「对齐」这回事）。</para></summary>
        static void TitleLeft(Label lb, PxRect r)
        {
            if (lb != null) MenuDraw.AlignLeft(lb, r);
        }

        /// <summary>一行文字（`Title` / `Date` / `New`）—— **在 `GameWindow.Text` 之上多两步，顺序固定**：
        /// `SetAutoFitBox` → **还原折行** → **对齐**。
        /// <para>🔴 **`折行` 为什么必须还原（A467）**：原版那三颗 `m_TextWrappingMode = **0**`（`NoWrap`）——
        /// 判据 = `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Message Container" --depth 8 --md --no-sprite`
        /// 那三行的 `折行=` 列**逐颗都是 `0`**（**本件亲跑，逐字抄**：`Title`「字号=50.0 基准=41.0
        /// auto[18.0~50.0] 对齐=Left/Capline **折行=0**」· `Date`「… 对齐=Left/Middle **折行=0**」·
        /// `New`「… 对齐=Right/Middle **折行=0**」），
        /// 而 `Label.SetAutoFitBox` 内部那条 `SetWrapWidth` **无条件把模式设成 `Normal`**
        /// （`Core/TmpFont.cs` 的 `SetWrapWidthRect`，它自己写着「调用方靠紧跟的 `SetWrapping(false)` 还原自己那一档」）
        /// ⇒ 不还原 = **悄悄打开折行**（A34-F4 / A303② / A212 那一族）。**走的是现成的口**
        /// `Label.SetWrapping(false)`（`Battle/Label.cs` 的 `SetWrapping` → `SetWrappingMode(0)`；它内部 `ForceRelayout`
        /// 把版面真推下去，A205）—— ⛔ 别去硬改 `SetAutoFitBox`/`SetWrapWidth`（那是共用件，且另有几处
        /// **要**折行）。</para>
        /// <para>🔴 **对齐必须排在它【之后】**：`SetWrapping` 会把版面与**量到的宽度**一起换掉，而
        /// `MenuDraw.AlignLeft/AlignRight` 是按**当前**宽度反推整块的位置（`Battle/Label.cs` 的 `AlignLeftOn`）
        /// ⇒ 先对齐再改折行 ⇒ 左/右边缘偏 `(旧宽 − 新宽)/2`（短文案看不出来、长文案才现形）。
        /// ⚠️ **为什么自己排这三步**：`GameWindow.Text` 把对齐放在它内部（`Shell/WindowsManager.cs` 的 `GameWindow.Text` 里 `align` 那段）
        /// ⇒ 走它就没法插在中间；这里给它 **`align: 0`**（= 它不动对齐），再由本方法调**同一个**
        /// `MenuDraw.AlignLeft/AlignRight`。**裁切不受影响**：每一刀之后 `RefreshBounds()` 都会重裁一次
        /// （`Battle/Label.cs` 的 `RefreshBounds`）。
        /// ⚠️ 这**不是**把 `GameWindow.Text` 抄回来（那份的合并见 A241）：本方法只**转调**它 + 补两步。</para>
        /// <para>🔴 **2026-10-16（A712 阶段 2）**：尾参 `vTier` = 原版那颗的 `m_VerticalAlignment`。
        /// 三颗**逐颗实读**（判据 = 本方法上面那条亲跑的 dump，逐字抄的三行）：
        /// `Title` = `Left/**Capline**` · `Date` = `Left/**Middle**` · `New` = `Right/**Middle**`
        /// ⇒ **只有 `Title` 一颗要传**（另两颗原版就是 `Middle`，不传才对）。
        /// 默认 `Middle` ⇒ 老调用点形状不变（本窗只有这一处调用三颗）。
        /// ⚠️ 排在【最后】—— 与 `align` 同一段理由（位移按**收敛后**的字号算）。</para></summary>
        Label RowText(Transform parent, PxRect r, string s, Color color, string name, float fontPx,
                      float autoMinPx, float autoBasePx, int align,
                      Label.VAlign vTier = Label.VAlign.Middle)
        {
            var lb = Text(parent, r, s, color, name, fontPx, QText, r.W, autoMinPx, 0, autoBasePx: autoBasePx);
            if (lb == null) return null;
            lb.SetWrapping(false);              // ← 原版那一档 `m_TextWrappingMode = 0`（A467）
            if (align == 1) MenuDraw.AlignLeft(lb, r);
            else if (align == 2) MenuDraw.AlignRight(lb, r);
            MenuDraw.SetVAlign(lb, vTier, r);
            return lb;
        }

        // ⚠️ **本窗原来在这里就地抄了一份 `Text(...)` 薄包装**（收「整块在视口外 ⇒ 不建 / 压在边上 ⇒ 裁 /
        //    对齐要在裁之前」那三步）。🆕 **2026-10-11（A241）已上移到共同基类 `GameWindow.Text`**
        //    （`Shell/WindowsManager.cs`）—— 本文件的**调用点一个都没改**（包括行尾那条传 `align` 的）。
        //    ⛔ 别在本文件再抄回来（判据 → `资料/待办判据_1008.md` §A241）。

        Texture2D Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name)) MissingArt.Add(name);
            return t;
        }

        public string Dump()
        {
            return $"Inbox：{(HasMessages ? "有消息" : "**空态**")} · 取不到的图 {MissingArt.Count} 张";
        }

        // ============================================================ 🆕 2026-10-26（F1）：选中态换底图必须同步按钮的常态图

        /// <summary>条目命中区节点的名字 —— `BuildRow` 建它（`AddHit(e, RowHitName, …)`）、
        /// `SetRowNormalTex` 反查它。**一处定义、两处引用**（⛔ 别在两处各写一个字符串字面量）。
        /// ⚠️ 自检侧也在按这个名字找（`Editor/RewardsScene.cs` 的 A273 那一段 `FindChild(rowHov, "RowHit")`）
        /// ⇒ 改名要同时改那边，否则「悬停还原」那条会变成「找不到按钮」的绿。</summary>
        const string RowHitName = "RowHit";

        /// <summary>🔴 **2026-10-26（`F1`·β2 修复）**：把第 <paramref name="row"/> 行那颗 `WindowButton` 的
        /// **常态底图**同步成当前的底图。
        ///
        /// <para><b>为什么必须有这一步</b>：原版 `MessageContainer.ToggleSelection(bool)` 换的是**同一个
        /// `Image` 的 `sprite`**（`background.sprite = selected ? selectedMessageSprite : normalMessageSprite`）
        /// —— 那颗 `Image` 既是「常态底图」又是「选中底图」，两档长在**同一个字段**上。
        /// 我们这边被拆成了两层：底图 quad 的贴图（`RefreshSelection` 换的）＋ `WindowButton._normalTex`
        /// （`Bind()` 那一刻记下的常态图、`Exit()`/`Release()` 还原时用它）。**只换前者 ⇒ 悬停离开时
        /// 按钮把底图还原成「未选中」那张** ⇒ 选中的那一行被悬停一次就掉色（`AuditHoverSwap` 报
        /// 「离开**没还原**」）。</para>
        ///
        /// <para>⚠️ 反查走节点名（`RowHitName`），⛔ 不另建一张 `WindowButton[]` 登记表 —— 那要多一个字段
        /// 与一次分配，而行表本来就是「每次滚动/选中都重建」（`BuildMessageRows`），多一张表只会多一处能忘。</para>
        /// <para>⚠️ 找不到（整条落在视口外 ⇒ `MenuDraw.Hit` 按 `ClipRect` 返回 null、那一行的命中区**根本没建**）
        /// 是**合法**的，静默跳过；此时那一行也看不见，没有「还原成错图」这回事。</para></summary>
        void SetRowNormalTex(int row, Texture t)
        {
            if (t == null || row < 0 || row >= _rowNodes.Length) return;
            var node = _rowNodes[row];
            if (node == null) return;
            var hit = node.Find(RowHitName);
            if (hit == null) return;                       // 视口外：那一行的命中区本来就没建（见上面的注释）
            var wb = hit.GetComponent<WindowButton>();
            if (wb != null) wb.SetNormalTex(t);
        }

        // ============================================================ 未读条目的 Shiny 扫光（原版 `MessageContainer.shinyEffect`）
        //
        // 【为什么参数写在这、不往 `WildcardShineLayer.Params` 里加预设】
        //   本件只服务**一个**宿主（收件箱每条未读行的底图），而 `Effects/UIEffectTogglerCoffeeEffects.cs`
        //   本波有别的写手在动 ⇒ 不往那个共用件里加成员（它也不在本件的白名单里）。
        //
        // 【逐字段真值（2026-10-27 现读，⛔ 别套 120 那组）】
        //   `MonoBehaviour_-7763153431244756047.json`（`Message Container ▸ Content` 上那颗 `UIEffect`）＋
        //   `MonoBehaviour_-3271889046122976335.json`（同 GO 的 `UIEffectTweener`，= `MessageContainer` 组件
        //   MB `-9188051066216764495` 的 `shinyEffect` 指向的那一个）：
        //     rotation **139** · width **0.213** · softness **0.533** · color **(2,2,2,1)** · filter **6** ·
        //     glow 0 · reverse 0 · keepAspect 1 · autoPlay 0 · rate 1.0 · **duration 1.0 s** · **wrap 0 (Once)**。
        //   🔴 **⛔ 别换成 `Params.WildcardDisplay`（120 / 0.25 / 0.5 / 白 / 0.75 s）** —— 那是收藏窗与
        //   Deck 编辑窗那颗（判据 → `报告/B9_W36SHINEA_扫光断言与宿主.md` §2·4 / §2·5）。
        //   挂在哪：`menu_dump.py bundle_menus_assets_all "Message Container" --depth 6` 的组件列实读
        //   ⇒ `Content` = `Image, EverguildButton, EverguildButtonMaterialModifier, UIEffect, UIEffectTweener`
        //   ⇒ 我们的等价物 = 每一行那颗底图 quad（`_rowBgs[i]`，原版 `Content` 那张 `Image`）。
        //
        // 【如实标注的两处偏差】
        //   ① **计时器在【窗】上、原版在【行实例】上** —— 我们的行每次滚动/选中都重建（`BuildMessageRows`），
        //      行级计时器会被重建不断重置 ⇒ 用一个窗级计时器（口径同改前的 `ShineTick`），重建只影响「扫哪几行」。
        //   ② **随机源 = 定种子 `System.Random`**（`_rng`，2026-10-25 `N17` 定的），原版是 `UnityEngine.Random.Range`。
        static readonly WildcardShineLayer.Params RowShineParams = new WildcardShineLayer.Params
        {
            rotation = 139f, width = 0.213f, softness = 0.533f, duration = 1f,
            color = new Color(2f, 2f, 2f, 1f), colorFilter = 6f, colorGlow = 0f,
            reverse = false, loop = false,
        };

        /// <summary>这一拍「扫一遍」：给每条**未读**行的底图挂层（第一次 / 行重建后）并 `Play(true)`。
        /// <para>层随行一起死（`WildcardShineLayer.StepAll` 的 `PruneDead` 回收）⇒ 这里只判「层还在不在」：
        /// `StillAttached` 假 = 那颗 quad 已经换了/没了 ⇒ 重挂一层。</para></summary>
        void PlayRowShine()
        {
            if (_rowShine.Length != _rowBgs.Length) _rowShine = new WildcardShineLayer[_rowBgs.Length];
            for (int i = 0; i < _rowBgs.Length; i++)
            {
                var q = _rowBgs[i];
                if (q == null) continue;
                // ⚠️ 只扫**未读行**（原版 `Update` 那条也在 `!IsRead` 分支里）
                if (i >= _messages.Count || !_messages[i].Unread) continue;
                var l = _rowShine[i];
                if (l == null || !l.StillAttached)
                {
                    l = WildcardShineLayer.AttachQuad(q, RowShineParams);
                    _rowShine[i] = l;
                    if (l == null) return;       // shader 取不到（`WildcardShineLayer.Attach` 已出过声）
                }
                l.Play(true);
            }
        }

        /// <summary>自检读口：第 <paramref name="row"/> 行那颗扫光到哪儿（0 = 带在起点 · 1 = 播完/不亮）；
        /// `-1` = 这一行还没有层（已读 / 没建出来 / shader 取不到）。
        /// <para>🔴 `1` 有**两种**含义（「没播」与「播完了」）⇒ 与 <see cref="RowShinePlaying"/> 并排看。</para></summary>
        public float RowShineRate(int row)
        {
            return (row >= 0 && row < _rowShine.Length && _rowShine[row] != null) ? _rowShine[row].Rate : -1f;
        }

        /// <summary>自检读口：第 <paramref name="row"/> 行那颗扫光还在播吗（Once 播完 ⇒ false）。没层 ⇒ false。</summary>
        public bool RowShinePlaying(int row)
        {
            return row >= 0 && row < _rowShine.Length && _rowShine[row] != null && _rowShine[row].Playing;
        }

        /// <summary>自检读口：现在挂着几层扫光（> 0 才算「这一窗的未读行确实挂上了」）。</summary>
        public int RowShineCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _rowShine.Length; i++) if (_rowShine[i] != null) n++;
                return n;
            }
        }

        // ============================================================ 入场曲线（原版 `MessageContainer.curve`，逐键照抄）
        //
        // 【判据（2026-10-27 `B9_W63WC` 现读，🥇 一手）】
        //   `d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-9188051066216764495.json`
        //   的 `curve` 字段 —— **完整 3 键**、`m_PreInfinity = m_PostInfinity = 2 (Clamp)`、`m_RotationOrder = 4`：
        //     ① t 0.0         v 0.0         in/outSlope  0.0            （`weightedMode 0` ⇒ `inWeight/outWeight` 不参与）
        //     ② t 0.35165453  v 0.62446737  in/outSlope  1.53013289
        //     ③ t 1.00219727  v 1.0         in/outSlope -0.40184420
        //   谁在用它（🥇 机器码）：`Everguild.LiveOps.MessageContainer__AnimateIn.c` 里
        //   `SetEase(DOScale(...), *(this + 0x78), …)` —— `+0x78` 就是本字段，**只喂给 `DOScale` 那一条**；
        //   同一个 Sequence 里 `DOFade` 那条是 `Append(DOTweenModuleUI.DOFade(canvasGroup))`，**没有 `SetEase`**
        //   ⇒ 淡入走 DOTween 默认 `Ease.OutQuad`（判据 = 原版 `DOTween.dll` v1.2.705 的
        //   `DG.Tweening.DOTween.defaultEaseType = Ease.OutQuad`，且 `Tweener.DoStartup` 把它写进每一条 tween；⛔ 别把两条都换成曲线）。
        //
        // ⚠️ 末键 t = **1.00219727**（不是 1）—— 原版逐字如此，照抄：`curve.Evaluate(1)` 因此 ≈ **1.00088**
        //   （略过 1）。收尾那一拍 `AnimTick` 会 `ApplyRowAnim(i, 1f, 1f)` 把缩放钉回 1，与原版 tween
        //   结束时落到目标 `localScale` 等价 ⇒ **不要为了「看起来整齐」把末键改成 1.0**（那是改判据）。
        static readonly AnimationCurve AnimCurve = BuildAnimCurve();

        static AnimationCurve BuildAnimCurve()
        {
            var c = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(0.35165453f, 0.62446737f, 1.53013289f, 1.53013289f),
                new Keyframe(1.00219727f, 1f, -0.40184420f, -0.40184420f));
            c.preWrapMode = WrapMode.Clamp;      // prefab `m_PreInfinity = 2`
            c.postWrapMode = WrapMode.Clamp;     // prefab `m_PostInfinity = 2`
            return c;
        }

        /// <summary>原版那条曲线的取值（= DOTween `SetEase(curve)` 内部做的 `curve.Evaluate(t)`）。
        /// <para>⚠️ **不裁到 [0,1]**：`t = 1` 给 ≈ 1.00088，原版也是这个值（见上面末键那条注释）。</para></summary>
        static float AnimCurveEase(float t) { return AnimCurve.Evaluate(t); }

        /// <summary>自检读口：原版曲线在某点的值（断言**期望值不取自被测实现** —— 逐键判据见上面注释）。</summary>
        public static float AnimCurveAtForTest(float t) { return AnimCurveEase(t); }

        // ============================================================ 🆕 2026-10-28（B90 · N3，追加在类尾）
        //  **原版 `MessageDisplay.OnPointerClick(PointerEventData)`** —— 正文里的链接。
        //  判据（第一权威 = 反汇编）：
        //   · `Everguild.LiveOps.MessageDisplay__OnPointerClick.c` **全 body 一句**：
        //       `TextLinkProcessing.TryProcessText(this.text(+0x28), eventData);`
        //   · `TextLinkProcessing__TryProcessText.c`：`TMP_TextUtilities.FindIntersectingLink(text, 指针, Camera.main)`
        //       → `-1` ⇒ 什么都不做；否则 `textInfo.linkInfo[i].GetLinkID()` → `Split(';')` → 取第 0 段：
        //         **`int.TryParse` 失败 ⇒ `Application.OpenURL(那一段)` 并 return true**；
        //         成功 ⇒ `List.RemoveAt(0)` 后 return true —— 而下一个消费者
        //         `TextLinkProcessing__ProcessText.c` 是**共享空桩**（机器码只有 `ret`）⇒ **数字型链接点了什么都不做**。
        //  ⇒ 我们只实现 URL 那一支（可观测行为的全部）；数字型那一支原版自己就是空桩。
        //  ⚠️ 命中那一步**不写第二份**：走 `Label.LinkAt(world, cam)`（`Battle/Label.cs` → `TmpFont.LinkAt`，
        //    内部就是同一个 `TMP_TextUtilities.FindIntersectingLink`）；先例 = `Shell/PopUpGameWindow.CheckLinkText`。

        /// <summary>正文那颗命中区（`MessageText Hit`）。自检拿它核「建出来了 + `onDown` 接的是本方法」。</summary>
        Transform _mdHit;
        /// <summary>自检口：正文命中区（没建出来 ⇒ null）。</summary>
        public Transform MessageHitNode { get { return _mdHit; } }

        /// <summary>原版 `MessageDisplay.OnPointerClick` —— 指针压在正文的 `<link=…>` 上就打开那个 URL。
        /// 没压中链接 ⇒ 什么都不做（原版 `FindIntersectingLink == -1` 那一支）。</summary>
        public void OnMessageBodyClick()
        {
            if (_mdBody == null) return;
            var cam = Camera.main;
            if (cam == null) return;                        // 原版 `GetUICamera()` 取不到 ⇒ 不做
            Vector3 m = PointerScreenForLink();
            float z = cam.WorldToScreenPoint(_mdBody.transform.position).z;
            string url = _mdBody.LinkAt(cam.ScreenToWorldPoint(new Vector3(m.x, m.y, z)), cam);
            if (string.IsNullOrEmpty(url)) return;           // 没压在链接上 ⇒ 不动作（⛔ 不出声：原版这一支完全静默）
            if (int.TryParse(url, out int _))
            {
                // 原版数字型链接的下一个消费者是**空桩**（`TextLinkProcessing__ProcessText.c` 只有 `ret`）⇒ 照抄「什么都不做」。
                Debug.Log("[Inbox] 正文链接 `" + url + "` 是**数字型动作** —— 原版 `TextLinkProcessing.ProcessText` 是空桩 ⇒ 不动作");
                return;
            }
            Application.OpenURL(url);
            Debug.Log("[Inbox] 正文链接 ⇒ 打开 URL「" + url + "」（原版 `TextLinkProcessing.TryProcessText`）");
        }

        /// <summary>屏幕指针（**屏幕像素、原点左下**，与 legacy `Input.mousePosition` 同口径）。
        /// 🔴 本工程 `activeInputHandler: 1`（只启用 Input System 包）⇒ **不许读 `Input.mousePosition`**（一调就抛）。
        /// 读数口径照抄 `Shell/PopUpGameWindow.PointerScreenForLink`（同一条链的既有实现）。
        /// ⚠️ 批处理/自检里没有鼠标设备 ⇒ 退回 `(0,0)` ⇒ 这条链**只能在真机上点**（自检改核「命中区建出来 + `onDown` 接的是本方法」）。</summary>
        static Vector3 PointerScreenForLink()
        {
            var m = UnityEngine.InputSystem.Mouse.current;
            if (m != null) return m.position.ReadValue();
            var ts = UnityEngine.InputSystem.Touchscreen.current;
            if (ts != null) return ts.primaryTouch.position.ReadValue();
            return Vector3.zero;
        }

        // ============================================================ 🆕 2026-10-28（B92_N6 · 追加在类尾；⛔ 别插进已有成员之间 —— 本文件 `:954`/`:971`/`:1321` 被账引用）
        /// <summary>每行一次的「入场 slotting 音」闸（<c>true</c> = 这一行已经试过播了，不再重试）。</summary>
        bool[] _animSoundPlayed = new bool[0];

        /// <summary>入场时那一颗 slotting 音 —— 原版 `MessageContainer.__c__DisplayClass23_0.&lt;AnimateIn&gt;b__0`
        /// （🥇 `out_il2csharp/Assembly-CSharp/Everguild/LiveOps/MessageContainer.cs:147-154`；`.c` 同体 = `decomp_full/Everguild.LiveOps.MessageContainer.__c__DisplayClass23_0___AnimateIn_b__0.c`）：
        /// <code>
        /// if (RendererExtensions.IsVisibleFrom(this.content.GetComponent&lt;RectTransform&gt;(), Camera.main))
        ///     SoundManager.Instance.Play2D((this.&lt;&gt;4__this).slottingSound, MixerType.FX);
        /// </code>
        /// 其中 <c>content</c> = `background.transform` = **本窗每一行那颗底图 quad**（`_rowBgs[i]`，见 `BuildMessageRow` 那条注释）。
        /// <para>🔴 **`slottingSound` 的 cue = `CardClick 2`** —— prefab 实读
        /// `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-9188051066216764495.json` 的
        /// `slottingSound = PPtr(fileID 6, pid 3481876391266756950)`，而 pid `3481876391266756950` 就是
        /// `CardInBoosterPack.clickSound`（`Shell/WindowSounds.cs` 头部那张表）⇒ **同一个 cue 资产**，
        /// 直接复用 <see cref="WindowSounds.BoosterCardClickCue"/>，⛔ 不在本文件另写 cue 名（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。</para>
        /// <para>🔴 **混音组 = FX（MixerType 0）** —— 判据 = 上面那行 `Play2D(..., MixerType.FX)` ⇒ 走 `WindowSounds.PlayFx`。</para>
        /// <para>⚠️ **时机**：原版那条回调挂在 `AppendInterval((index + 1) × animationDelay)` **之后**、`DOFade/DOScale` **之前**
        /// ⇒ 本窗在 `AnimTick` 里以「`_animT` 首次越过该行的起点」为一拍，**每行只播一次**（`_animSoundPlayed` 闸）。</para>
        /// <para>⚠️ **可见性为假 ⇒ 静默不播**（原版那一支什么都不做，⛔ 不是「换成别的音」）。</para>
        /// <para>📌 prefab 里还有一个 `maxSounds = 6`，但 `out_il2csharp` 里**全类只出现在声明处**（0 处读取）
        /// ⇒ 原版这一版是死字段，我们**不实现** 6 声上限（照抄「它没被读」这一事实，并在账 `B92_N6_new.tsv` 里写明）。</para></summary>
        void PlaySlottingIfVisible(int i)
        {
            if (i < 0 || i >= _rowBgs.Length) return;
            var q = _rowBgs[i];
            if (q == null) return;
            var rt = q.transform as RectTransform;
            var cam = Camera.main;
            if (rt == null || cam == null) return;                       // 批处理/无相机 ⇒ 不播（原版这一档 NRE，我们降级）
            if (!RendererExtensions.IsVisibleFrom(rt, cam)) return;      // 原版：不在屏幕内 ⇒ 静默
            WindowSounds.PlayFx(WindowSounds.BoosterCardClickCue, "InboxWindow 入场 slotting（原版 MessageContainer.slottingSound）");
        }
    }
}
