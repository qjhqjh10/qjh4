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
//     ⚠️ **不接点击**（原版点条目 = 右侧显示正文，而正文那几件没建 ⇒ 挂空动作只会变成
//     「命中区没绑动作」的假件）⇒ 相应也**不做悬停换图**。出处在 `BuildRow` 的注释。
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
        /// ⚠️ **不是 `(25,0)`** —— 这条列表是**纵向**滚的，渐隐带在**上下**两条边上（写反了会竖切）。</summary>
        public static readonly Vector2 ListSoft = new Vector2(0f, 25f);
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
        public const float RowTitleFont = 50f, RowDateFont = 40f, RowNewFont = 40f;
        public const float RowTitleAutoMin = 18f, RowDateAutoMin = 18f, RowNewAutoMin = 18f;
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
            MenuDraw.Text(c, Title, DailyData.InboxTitle(), Color.white, "Title", 48f, QText);

            // ---- `Message List`：原版三层 = `Message List`(ScrollRect) → `Viewport`(RectMask2D) → `Content`(VLG) ----
            // 🔴 **2026-10-08（A182）：原来只建了 `Message List` 一个空节点**（`Viewport` / 掩码 / 滚动都没有，
            //   而原版这三层是齐全的）。我们**单机没有消息**（`DailyData.InboxCount` 恒 0）⇒ 名单照旧是**空的**
            //   （这一点与原版 prefab 出厂一致：`Content` 出厂 0 子件、`ContentSizeFitter` 给的高 = 0），
            //   但**结构、裁切、滚动三件都按原版建出来**：`Initialize(messages)` 一有数据就照原版铺。
            _messages.Clear();                 // 单机数据源（原版这里是 handler.Messages）
            var msgList = MenuDraw.Node(c, "Message List", MsgList);
            var listVp = MenuDraw.Node(msgList, "Viewport", MsgList);
            _listContent = MenuDraw.Node(listVp, "Content", new PxRect(MsgList.x1, MsgList.y1, MsgList.x2, MsgList.y1));
            BuildMessageList();

            // `Message Display`：**空态下整块关**（`InboxWindow__Open.c:49`）
            _display = MenuDraw.Node(c, "Message Display", MsgDisplay);
            MenuDraw.Rect(_display, Art(ArtDisplayBg), MsgDisplay, "Background", QPanel);
            MenuDraw.Text(_display, MdTitle, DailyData.InboxMessageDisplayTitle(), Color.white, "Title", 58f, QText);
            MenuDraw.Rect(_display, Art(ArtScrollbar), MdScrollbar, "Scrollbar Vertical", QContent);

            // `No News Warning`（空态文案）
            _warn = MenuDraw.Text(c, NoNews, DailyData.InboxNoNewsText(), Color.white, "No News Warning", 50f, QText);
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

            var prevClip = Clip;
            var prevSoft = ClipSoftness;
            var prevPad = ClipPad;
            Clip = MsgList;                 // = 原版 `Viewport` 的矩形
            ClipSoftness = ListSoft;        // (0,25)
            ClipPad = ListPad;              // (0,0,0,0)
            for (int i = _messages.Count - 1; i >= 0; i--)       // 树序倒排，见本方法的注释
            {
                float y1 = MsgList.y1 + i * RowPitch;            // 内容坐标（第 i 条在视觉第 i 行）
                var r = new PxRect(MsgList.x1, y1, MsgList.x1 + RowW, y1 + RowH);
                if (_listScroll != null) r = _listScroll.Shift(r);
                Rows.Add(BuildRow(_listContent, r, i, _messages[i]));
            }
            Clip = prevClip;
            ClipSoftness = prevSoft;
            ClipPad = prevPad;
        }

        /// <summary>一个条目（原版 `Message Container`）：底图 + `Title` + `Date` + [`New!`]。
        /// 三件几何/字号/颜色/对齐**逐条照 dump**（`Row*` 常量）。
        /// ⚠️ **不接点击**：原版点条目 = 选中它、右侧 `Message Display` 显示正文 —— 而本窗正文那几件
        /// （`Header`/`Message`/`Claim Button`）**没建**（服务端数据）⇒ 挂个空动作只会变成
        /// 「命中区没绑动作」那种假件（红线：不许静默失败）⇒ **如实不挂**，记在报告里。
        /// ⚠️ 于是也**不做悬停换图**（`40K_settings_button_hover` 是那颗 `SpriteSwap` 的图，没按钮就没它）。</summary>
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

            DrawRect(e, Art(ArtRowBg), OR(RowBg), "Background", QPanel, RowBgTint);
            // 原版三件的对齐（dump 实读）：`Title` / `Date` 是 `Left/Capline`·`Left/Middle`、
            // `New` 是 `Right/Middle`。⚠️ `Label` 建出来默认是**居中**在锚点上 ⇒ 对齐要显式给。
            Text(e, OR(RowTitle), m.Title ?? "", RowTitleColor, "Title", RowTitleFont, QText,
                 RowTitle.W, RowTitleAutoMin, 1);
            Text(e, OR(RowDate), m.Date ?? "", RowDateColor, "Date", RowDateFont, QText,
                 RowDate.W, RowDateAutoMin, 1);
            if (m.Unread)
                Text(e, OR(RowNew), TxtUnread, RowNewColor, "New", RowNewFont, QText,
                     RowNew.W, RowNewAutoMin, 2);
            return e;
        }

        /// <summary>摆一段字，**吃本窗的裁切**：① 整块在视口外 ⇒ 不建；② 压在视口边上 ⇒ 裁。
        /// `align`：**0 = 居中（`Label` 默认）· 1 = 左 · 2 = 右**（原版 TMP 的 `m_HorizontalAlignment`）。
        /// 🔴 **对齐必须在 `ClipText` 之前** —— `ClipText` 夹的是**世界坐标**的顶点，先裁再挪会把裁好的块挪出框。
        /// 🔴 与 `Shell/DailyStreakPopup.cs` / `Shell/CampaignRewardWindow.cs` 那两份是**同一条规则的三份副本**
        /// —— 三扇窗都是 `GameWindow` 直系、够不到 `MainMenuSubmenuWindow.Text` 那一份，而共同基类
        /// `GameWindow` 在 `Shell/WindowsManager.cs`（**不在本件白名单**）⇒ 就地实现 + 记进报告
        /// （「该上移到 `GameWindow`」的第二批，先例 = A78② 上移的 `DrawRect`/`DrawNine`/`AddHit`）。</summary>
        Label Text(Transform parent, PxRect r, string s, Color color, string name, float fontPx, int q,
                   float wrapPx = 0f, float autoMinPx = 0f, int align = 0)
        {
            if (!MenuDraw.Visible(r, RenderClip)) return null;
            var lb = MenuDraw.Text(parent, r, s, color, name, fontPx, q, wrapPx, autoMinPx);
            if (lb == null) return null;
            if (align == 1) MenuDraw.AlignLeft(lb, r);
            else if (align == 2) MenuDraw.AlignRight(lb, r);
            if (RenderClip.HasValue) MenuDraw.ClipText(lb, RenderClip, ClipSoftness);
            return lb;
        }

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
