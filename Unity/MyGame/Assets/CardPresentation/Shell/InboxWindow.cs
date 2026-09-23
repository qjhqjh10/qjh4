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
//   · **红点**：原版 `Inbox.CheckNotification` = **未读条数**（`Repeat(new InboxBadge(), Count(!IsRead))`）。
//     单机没有消息 ⇒ 未读 = 0 ⇒ **红点 alpha 0**（见 `MainMenuRuntime` 里那一段）。
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

        public static readonly Color MdTitleTint = new Color(0.95f, 0.66f, 0.40f, 1f);

        public const int QShade = 3002, QPanel = 3006, QContent = 3010, QText = 3011, QOverlay = 3014;

        public readonly System.Collections.Generic.List<string> MissingArt =
            new System.Collections.Generic.List<string>();

        /// <summary>有没有消息。原版由 handler 给；**单机恒 false** ⇒ 走空态那条分支。</summary>
        public bool HasMessages { get { return DailyData.InboxCount > 0; } }

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

            var c = MenuDraw.Node(root, "Content", Content);
            MenuDraw.Rect(c, Art(ArtRedBg), RedBg, "Generic Window Red Background Big", QPanel);
            MenuDraw.Text(c, Title, DailyData.InboxTitle(), Color.white, "Title", 48f, QText);

            // `Message List`：**出厂 0 子件**（结构见正本 §六；单机没有服务端下发的条目）
            MenuDraw.Node(c, "Message List", MsgList);

            // `Message Display`：**空态下整块关**（`InboxWindow__Open.c:49`）
            var md = MenuDraw.Node(c, "Message Display", MsgDisplay);
            MenuDraw.Rect(md, Art(ArtDisplayBg), MsgDisplay, "Background", QPanel);
            MenuDraw.Text(md, MdTitle, DailyData.InboxMessageDisplayTitle(), Color.white, "Title", 58f, QText);
            MenuDraw.Rect(md, Art(ArtScrollbar), MdScrollbar, "Scrollbar Vertical", QContent);
            md.gameObject.SetActive(HasMessages);
            if (!HasMessages)
                Debug.Log("[Inbox] 没有消息 ⇒ **空态**：关 `Message Display`、开 `No News Warning`" +
                          "（原版 `InboxWindow__Open.c:49` 就是这条分支）");

            // `No News Warning`（空态文案）
            var warn = MenuDraw.Text(c, NoNews, DailyData.InboxNoNewsText(), Color.white, "No News Warning", 50f, QText);
            if (warn != null) warn.gameObject.SetActive(!HasMessages);

            // 关闭钮
            var close = MenuDraw.Node(c, "Generic Close Button Orange", CloseBtn);
            MenuDraw.Rect(close, Art(ArtCloseBg), CloseBg, "Background", QContent);
            MenuDraw.Rect(close, Art(ArtCloseIcon), CloseBg, "Icon", QOverlay);
            var x = MenuDraw.Rect(close, Art(ArtCloseX), CloseBg, "Icon (X)", QOverlay);
            if (x != null)
            {
                var hit = x.gameObject.AddComponent<WindowButton>();
                hit.onClick = () => Close();
            }

            // ⚠️ `Reset Button` **不建**（原版是死的，见文件头）

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Inbox] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));
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
