// InboxWindow.cs — 「日常」第 2 层：收件箱（原版 `Inbox Menu`）
//
// ============================ 出处（唯一正本） ============================
// `资料/日常_原版规格.md` §六（层 × 参数表）· `资料/日常_调用链_Inbox.md`（调用链 + 空态判据）。
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
//   · **`Reset Button` 不建**：它出厂就 `active`，但**是死的** —— `m_OnClick` 持久调用表**是空的**、
//     全量反编译里 `DebugReset` **零调用者**、而且 `Open()` 每次把它 `SetActive(false)`（`日常_调用链_Inbox.md` B 节）。
//   · **`Message Display` 的正文几件（`Header`/`Message`/`Claim Button`）不建**：它们的内容由**消息对象**驱动，
//     单机没有消息（而且空态下 `MessageDisplay` 整块是关的）。**框架照建、内容缺就说**（红线：不许静默失败）。
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

        public const string ArtRedBg = "UI_Deck_Information_Back";
        public const string ArtDisplayBg = "UI_Deck_Information_submenu_Back";
        /// <summary>⚠️ 工程里的切片名是**下划线**版（导入器把空格换成下划线）。</summary>
        public const string ArtScrollbar = "40k_menu_scroll_bar_bg";
        public const string ArtCloseBg = "UI_Button_Round_background";
        public const string ArtCloseIcon = "40k_general_bt_yellow";
        public const string ArtCloseX = "40k_general_bt_yellow_close";

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
        /// ⚠️ **`m_fontSizeMax` 那三档（50/40/40）我们早就是等价的**（`MenuDraw.Text` 的 `autoMaxPx <= 0`
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
        /// `fontPx`，而 `MenuDraw.Text` 在 `autoMaxPx <= 0` 时**退回 `fontPx`** ⇒ **本来就等价**
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
        /// <para>走**现成的口** `Label.SetCharSpacing`（`Battle/Label.cs:463`，**原样传、不换算**；
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

        public readonly System.Collections.Generic.List<string> MissingArt =
            new System.Collections.Generic.List<string>();

        /// <summary>一条消息（**原版由服务端 handler 下发** —— `InboxWindow__Open.c` 取 `handler.Messages`
        /// 再 `MessageList.Initialize(messages)`；本窗只建了列表条目要显示的那三项，
        /// 正文/领取那几件（`Message Display/Content` 树下）**不建**，见文件头「我们挑的 / 不建的」）。</summary>
        public class Message
        {
            public string Title, Date;
            public bool Unread;
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

        /// <summary>有没有消息。原版由 handler 给；**单机恒 false** ⇒ 走空态那条分支。</summary>
        public bool HasMessages { get { return _messages.Count > 0 || DailyData.InboxCount > 0; } }

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

            MenuDraw.Rect(root, CardArt.Solid(), Shade, "Menu Dark Background", QShade,
                          new Color(0f, 0f, 0f, 0.77f));
            // 🔴 **2026-10-05（A81）**：压暗层的**点击区**（「点窗外关窗」）—— **原来零 `ShadeHit(`**
            //   （只有 `:87` 那一层压暗的 `Image`，点了什么也不发生）。
            //   判据：原版是压在 `Menu Dark Background` **自身节点**上的 `BackgroundCloseButton`，
            //   由窗口类自己挂/摘 —— `Everguild.LiveOps.InboxWindow__Open.c:95-109` 的**同一段**
            //   给 `param_1[0x11]`（关窗钮的 `Button.m_OnClick`，`+0x100`）与 `param_1[0x14]`
            //   （`BackgroundCloseButton.onClick`，`+0x28`）**挂的是同一个处理函数**（虚表 `*(*param_1+0x1c0)`）；
            //   `InboxWindow__Close.c:15-25` 又把这两条**成对摘掉**。
            //   ⇒ 动作照抄本窗那颗关窗钮（`:121` 的 `Close()`），两颗行为**必须一致**。
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
                                        Title.W, TitleAutoMin, autoBasePx: WindowLabelAutoBase);
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
            _messages.Clear();                 // 单机数据源（原版这里是 handler.Messages）
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
            MenuDraw.Rect(_display, Art(ArtDisplayBg), MsgDisplay, "Background", QPanel);
            // 🆕 **2026-10-12（A468）**：原版 `auto[10.0~58.0]` · `基准=36.0` · `折行=0`（判据 → `TitleAutoMin`）
            var mdTitleLb = MenuDraw.Text(_display, MdTitle, DailyData.InboxMessageDisplayTitle(),
                                          Color.white, "Title", 58f, QText,
                                          MdTitle.W, MdTitleAutoMin, autoBasePx: WindowLabelAutoBase);
            if (mdTitleLb != null) mdTitleLb.SetWrapping(false);
            // 🆕 **2026-10-12（A470）**：同上一颗 —— 原版 `对齐=Left/Middle`（判据 → `TitleLeft` 的注释）。
            //   ⚠️ 必须排在**上面那句 `SetWrapping(false)` 之后**（理由同 `TitleLeft`）。
            TitleLeft(mdTitleLb, MdTitle);
            MenuDraw.Rect(_display, Art(ArtScrollbar), MdScrollbar, "Scrollbar Vertical", QContent);

            // `No News Warning`（空态文案）
            // 🆕 **2026-10-12（A468）**：原版 `auto[12.0~50.0]` · `基准=36.0` · `折行=0`（判据 → `TitleAutoMin`）
            _warn = MenuDraw.Text(c, NoNews, DailyData.InboxNoNewsText(), Color.white, "No News Warning", 50f, QText,
                                  NoNews.W, NoNewsAutoMin, autoBasePx: WindowLabelAutoBase);
            if (_warn != null) _warn.SetWrapping(false);
            // 🆕 **2026-10-12（A471）**：原版这一颗**还有 `字距=2`**（我们原来是 0）—— 判据 → `NoNewsCharSpacing`。
            //   ⚠️ 这一颗原版是 `Center/Middle` ⇒ **不做对齐**（与我们一致）；只需要把字距接上。
            //   🔴 尾上那句 `ForceRelayout` 不是装饰：`SetCharSpacing` 只把 mesh 重排一次、
            //     **不刷新 `Label` 自己缓存的 `_tmpW/_tmpH`**（`Battle/Label.cs:463-469`）⇒ 少了它，
            //     这颗的 `WorldW/WorldH` 会停在**字距生效之前**的宽度（断言量宽度时读到旧值 = 静默不一致）。
            //     `ForceRelayout` 是**幂等**的（传当前字号 ⇒ setter 早退，只推一次重排，见它的头注释）。
            if (_warn != null)
            {
                _warn.SetCharSpacing(NoNewsCharSpacing);
                _warn.ForceRelayout();
            }
            ApplyEmptyState();        // 🆕 A182：这条分支原来写在 `Build()` 里，现在收成一份（`Initialize` 也要用）

            // 关闭钮
            var close = MenuDraw.Node(c, "Generic Close Button Orange", CloseBtn);
            // 🔴 换图落在**圆底那一层**（原版三层 = 圆底 `UI_Button_Round_background` + 黄面 + 叉；
            //    `trans=2` 换的是它自己的 Image。2026-10-03 直接读 prefab 核过）
            var baseQ = MenuDraw.Rect(close, Art(ArtCloseBg), CloseBg, "Background", QContent);
            MenuDraw.Rect(close, Art(ArtCloseIcon), CloseBg, "Icon", QOverlay);
            var x = MenuDraw.Rect(close, Art(ArtCloseX), CloseBg, "Icon (X)", QOverlay);
            if (x != null)
            {
                var hit = x.gameObject.AddComponent<WindowButton>();
                hit.onClick = () => Close();
                // 🆕 A17：原版 `Content>Generic Close Button Orange` 是 SpriteSwap，HL = `40k_general_bt_yellow_hover`
                hit.Bind(baseQ, null, "40k_general_bt_yellow_hover");
            }

            // ⚠️ `Reset Button` **不建**（原版是死的，见文件头）

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
                          + "(0,25) + `MenuScroll`），只是**没有条目**（单机没有服务端数据源 ⇒ "
                          + "`Content` 与原版 prefab 出厂一样是空的；一旦 `Initialize(messages)` 有数据就照原版铺）");
        }

        // ============================================================ 消息列表（`Message List`）

        /// <summary>**照原版 `MessageList.Initialize(messages)`** —— 原版由 `InboxWindow__Open` 在
        /// 「非空」那条分支上调它（`:49` 那两句的 else 支）。⚠️ 单机路径**不会**走到（没有服务端数据源）；
        /// 本方法在那儿是**生产 API**（将来接了真数据源，`Build()` 里那句 `_messages.Clear()` 换成喂真数据即可
        /// —— 原版就是这个形状）。`null`/空 = 回到空态。</summary>
        public void Initialize(System.Collections.Generic.IList<Message> messages)
        {
            _messages.Clear();
            if (messages != null)
                for (int i = 0; i < messages.Count; i++) if (messages[i] != null) _messages.Add(messages[i]);
            BuildMessageList();
            ApplyEmptyState();          // 照原版：有消息 ⇒ 右侧 `Message Display` 跟着开
        }

        /// <summary>建（或重建）列表的**滚动区**+ 内容容器的高度。
        /// 内容高 = `n × 条目高 + (n−1) × spacing`（原版 `ContentSizeFitter m_VerticalFit = 2(MinSize)`
        /// + `VerticalLayoutGroup spacing = 7`）；内容**上对齐**（`align = 0 UpperLeft`）。
        /// 🔴 重建时先 `UnregisterOwnedBy` 把旧的撤掉（`PointerLayer` 的登记表**只增不减**是踩过的雷）。</summary>
        void BuildMessageList()
        {
            if (_listContent == null) return;
            float contentH = _messages.Count <= 0 ? 0f : _messages.Count * RowPitch - RowSpacing;
            _listScroll = MenuScroll.TopAligned(MsgList, contentH);
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
        }

        /// <summary>一个条目（原版 `Message Container`）：底图 + `Title` + `Date` + [`New!`]。
        /// 三件几何/字号/颜色/对齐**逐条照 dump**（`Row*` 常量）。
        /// 🔴 **2026-10-09（A273）就地订正（铁律 5）**：这一段原来写「**不接点击** ⇒ 于是也**不做悬停换图**
        /// （没按钮就没它）」—— **前半句仍然成立、后半句是错的**：
        /// 悬停换图**不依赖「点了有事做」**，它就是那条 `Selectable`（`m_Transition = 2` SpriteSwap）
        /// 的**悬停那一档**，所以它是一条**真的缺漏**（A273）。现在：底图接上悬停换图
        /// （`Bind` 挂在**底图那颗 quad** 上、命中区是另一颗透明 quad，见下），**点击仍如实出声**。
        /// <para>⚠️ **点击这一半仍未做**：原版点条目 = 选中它、右侧 `Message Display` 显示正文 ——
        /// 而本窗正文那几件（`Header`/`Message`/`Claim Button`）**没建**（服务端数据驱动）⇒
        /// 这一下**没有画面变化**。按本仓对「没做的功能」的既有做法（先例：主菜单的 `ReplayButton`
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
            if (bg != null)
            {
                AddHit(e, "RowHit", OR(RowBg), QRowHit,
                       () => Debug.LogWarning(
                           "[Inbox] 点了消息条目 `" + (m.Title ?? "") + "` —— 原版这一下 = **选中它**、"
                           + "右侧 `Message Display` **显示正文**；而正文那几件（`Header`/`Message`/"
                           + "`Claim Button`）由服务端消息驱动、本机没建 ⇒ **没有画面变化**（如实出声，不静默）。"),
                       bg, ArtRowBg, ArtRowHover, ArtRowPressed);
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
                    RowTitleAutoMin, RowTitleAutoBase, 1);
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
        /// 统一建成 `Center`（`Core/TmpFont.cs:158`）⇒ 这两颗原来都落在框心（`Inbox` 那颗往右偏
        /// `(框宽 250 − 文字宽)/2` px；`Message Display` 那颗往右偏 `(959.77 − 文字宽)/2` px）。</para>
        /// <para>🔴 **必须排在 `SetAutoFitBox` / `SetWrapping(false)` 【之后】**：`MenuDraw.AlignLeft` 走
        /// `Label.AlignLeftOn`，它按**当时的 `WorldW`**（刚量到的渲染宽）反推整块的位置，而
        /// `SetAutoFitBox` / `SetWrapping` 都会改字号与量到的宽度 ⇒ 先对齐、后改折行 = 左缘被推走
        /// （同族坑与判据：`Battle/Label.cs:786-805` · `LiveOpsEventWindow` 那处注释「顺序不能反」·
        /// 本文件 `RowText` 的同一段推理）。垂直那一半（`Middle`）**本来就是对的** —— `RefreshBounds`
        /// 会把整块**居中**摆进矩形（`Battle/Label.cs:721-734`）⇒ 这里只管水平。</para>
        /// <para>⚠️ 走的是**现成的口**（`Shell/MenuDraw.cs:1585` 的 `MenuDraw.AlignLeft`），⛔ 没有就地重写一份；
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
        /// （`Core/TmpFont.cs:238`，它自己写着「调用方靠紧跟的 `SetWrapping(false)` 还原自己那一档」）
        /// ⇒ 不还原 = **悄悄打开折行**（A34-F4 / A303② / A212 那一族）。**走的是现成的口**
        /// `Label.SetWrapping(false)`（`Battle/Label.cs:362` → `SetWrappingMode(0)`；它内部 `ForceRelayout`
        /// 把版面真推下去，A205）—— ⛔ 别去硬改 `SetAutoFitBox`/`SetWrapWidth`（那是共用件，且另有几处
        /// **要**折行）。</para>
        /// <para>🔴 **对齐必须排在它【之后】**：`SetWrapping` 会把版面与**量到的宽度**一起换掉，而
        /// `MenuDraw.AlignLeft/AlignRight` 是按**当前**宽度反推整块的位置（`Battle/Label.cs:787-805`）
        /// ⇒ 先对齐再改折行 ⇒ 左/右边缘偏 `(旧宽 − 新宽)/2`（短文案看不出来、长文案才现形）。
        /// ⚠️ **为什么自己排这三步**：`GameWindow.Text` 把对齐放在它内部（`Shell/WindowsManager.cs:354-355`）
        /// ⇒ 走它就没法插在中间；这里给它 **`align: 0`**（= 它不动对齐），再由本方法调**同一个**
        /// `MenuDraw.AlignLeft/AlignRight`。**裁切不受影响**：每一刀之后 `RefreshBounds()` 都会重裁一次
        /// （`Battle/Label.cs:736-746`）。
        /// ⚠️ 这**不是**把 `GameWindow.Text` 抄回来（那份的合并见 A241）：本方法只**转调**它 + 补两步。</para></summary>
        Label RowText(Transform parent, PxRect r, string s, Color color, string name, float fontPx,
                      float autoMinPx, float autoBasePx, int align)
        {
            var lb = Text(parent, r, s, color, name, fontPx, QText, r.W, autoMinPx, 0, autoBasePx: autoBasePx);
            if (lb == null) return null;
            lb.SetWrapping(false);              // ← 原版那一档 `m_TextWrappingMode = 0`（A467）
            if (align == 1) MenuDraw.AlignLeft(lb, r);
            else if (align == 2) MenuDraw.AlignRight(lb, r);
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
    }
}
