// SocialWindow.cs — 阶段二 · 多人界面那一批 第 4 件：**社交**（原版 `Social Submenu Variant`，类 `SocialMenuWindow`）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/社交_联盟与好友页.md` ——
//   §A·0 根定位（重名多份怎么认的）· §A·1 主树 **372 节点的「层 × 参数」表**（`menu_dump.py` 逐行，
//   值全部**直读原始 JSON**）· §A·2 四款行族的**独立根真 prefab** · §B 18 条判定 · §C 查不到的（含搜过的词）。
// 任务与口径 → `项目任务.md` §三 第 18 条 第 4 项。
//
// ---- 🔴 这一扇窗的四条判据（读原始 JSON 定的，别推翻）----
// ① **它就是「奖励窗/商店/收藏窗」那同一个壳**：`Content Area` = 167.17,70.94→1920,1080、
//    `Tab Buttons` = 165 宽 + `40k_main_tab_background` + `VerticalLayoutGroup`（padTop 120 · spacing 0 ·
//    align 1 UpperCenter · 子 165×180）—— 与 `MainMenuSubmenuWindow` 里那套**逐值相同**
//    ⇒ 直接 `: MainMenuSubmenuWindow`，**别再写一套左栏**（`grep` 出来的值：§A·1 第 28/30/31/38 行）。
// ② 🔴 **左栏那条 `Shadow` 原版是 0 高、静态看不见**：`Tab Buttons` 第 3 孩子 `Shadow`
//    `a=(0,1)-(0,1) p=(.5,.5) pos=(82.5,−480) sz=(165,0)`（§A·1 第 45 行 / §B·15）。
//    基类默认那条 47.64 宽是**奖励窗/商店**的实测值 ⇒ 这里覆写成 **0**（`BarShadowW`），否则**凭空多一条线**。
//    ⚠️ 这是铁律 5·c 的活例子：**一个值 ≠ 全部情况**。
// ③ **`Content Area` 的第 4 个孩子 `Shadow (1)` 出厂 act=F** ⇒ **不建**（基类 `BuildShell` 本来就不建，
//    照 `MainMenuRuntime` 那条纪律③）。
// ④ **窗口字段**（直读 MB `4523486740489232547`）：`type=0`(Fullscreen) · `windowsPlacement=5`(Canvas) ·
//    `closeOnESC=1` · `updateNavPanel=1` · `useDefaultCloseSoundIfNull=1` · `extraScaleSmallScreen=1.0` ·
//    `tabs[]` 两个（`AlliancesTab` → `FriendsTab`，顺序 = 左栏视觉顺序）。
//    🔴 **整棵树里没有关窗钮**（§B·14 全树查过）⇒ 关窗靠 ESC（`closeOnESC=1`）。
//
// ---- 数据（用户 2026-09-26 口径：「有什么复刻什么，具体的数据和排名这些可以空着」）----
// 联盟 / 成员 / 好友 / 邀请**全部本地无源**（原版读服务器，已关服）⇒ 列表**恒空**、留白，
// **不编数字、不自造空态文案**（原版也没有 `Empty*` 节点，§B §5·6 第 1 条）。
// 行模板四款照建（`§A·2·1..4` 的独立根），有数据那天直接长出来 —— 见 `SocialData.cs`。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `SocialMenuWindow`（`Social Submenu Variant`）—— 主菜单左竖导航第 5 钮（SOCIAL）开的窗。</summary>
    public class SocialWindow : MainMenuSubmenuWindow
    {
        // ============================================================ 队列档
        //
        // ⚠️ 分层一律用**渲染队列**、不用 z（`ImageQuad` 全是透明队列，按到相机的 3D 距离排序 ——
        //    屏幕中间的反而更近）。窗壳用基类的 3005/3010/3011/3014；**页**另起一档
        //    （照档案窗 `PlayerProfileWindow.QPageBase = 3160` 那条先例），每页 10 个号。
        // ⚠️ **弹窗要再高一档**（`MessagePopupWindowDuel` 用 3250+）—— `RewardsScene` 有一条断言
        //    钉住「弹窗 > 页 > 窗」这个次序。
        public const int QPageBase = 3200;

        // ============================================================ 左栏两键（原版 `Tab Buttons` 的两个孩子）
        //
        // 逐字段取值（§A·1 第 31–44 行）：
        //   · 图标：`40k_main_bt_alliances`（252×251）/ `40k_alliances_bt_friends v2`（252×251）
        //     🔴 **第二张的名字里是【空格】+ `v2`** —— 导入器把它落成 `40k_alliances_bt_friends_v2.png`
        //     （`CardArt.MenuUi` **不做空格→下划线**，传原名会静默取不到图）。
        //     ⚠️ 也**不是** `40k_main_bt_friends`（那是**左竖导航 SOCIAL 那颗钮**的图，另一张）。
        //   · 文案 `Alliances` / `Friends`，字号 **36**、`auto[5,36]`、色 (0.957,0.882,0.675,1)（基类那套）。
        //   · 角标 `Badge Highlight`：`pos=(51.7, **+47.9**)`（uGUI y 向上）⇒ `BadgeDy = −47.9`
        //     （基类按 `cy − BadgeDy` 摆，见 `TabBtnSpec.BadgeDy` 的注释）。两个键**同值**。
        public static readonly TabBtnSpec[] Buttons =
        {
            new TabBtnSpec("40k_main_bt_alliances",       "Alliances", 36f, 5f, 36f,
                           "SocialMenu_AlliancesButton", -47.9f),
            new TabBtnSpec("40k_alliances_bt_friends_v2", "Friends",   36f, 5f, 36f,
                           "SocialMenu_FriendsButton",   -47.9f),
        };

        /// <summary>🔴 原版这条 `Shadow` 是 **0 高**（见文件头 ②）⇒ 不建。基类默认 47.64 是奖励窗的值。</summary>
        protected override float BarShadowW { get { return 0f; } }

        // ============================================================ 自检读口

        /// <summary>最近一次开出来的那一扇（自检用，同 `PlayerProfileWindow.LastOpened` 那条先例）。</summary>
        public static SocialWindow LastOpened { get; private set; }

        public AlliancesTab PageAlliances { get; private set; }
        public FriendsTab PageFriends { get; private set; }

        ImageQuad[] _btnHighlight = new ImageQuad[2];
        ImageQuad[] _btnBadge = new ImageQuad[2];
        Transform[] _btnRoot = new Transform[2];

        /// <summary>两个键的红点（原版是 `UiBadgeNotification` 的 **alpha 补间**，不是 `SetActive`）。</summary>
        public ImageQuad[] Badges { get { return _btnBadge; } }

        // ============================================================ 建

        public static SocialWindow Create(WindowsManager mgr)
        {
            var go = new GameObject("Social Submenu Variant");
            var win = go.AddComponent<SocialWindow>();
            win.type = WindowType.Fullscreen;                 // 实证 type = 0
            win.placement = WindowsPlacement.Canvas;          // 实证 windowsPlacement = 5
            win.closeOnEsc = true;                            // 实证 closeOnESC = 1（整树没有关窗钮 ⇒ 只能 ESC）
            win.extraScaleSmallScreen = 1f;                   // 实证 1.0
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();
            BuildTabContents();
            // 默认落在 **Alliances**（`Alliances Tab` act T / `Friends Tab` act **F**，实测 `m_IsActive`）——
            // 走 `Click(0)` 而不是直接改字段：「点一下」才是状态的唯一入口（同奖励窗那条注释）。
            if (tabButtons != null) tabButtons.Click(0);
            RefreshHighlights();
            RefreshBadges();
            // 原版 `updateNavPanel = 1` ⇒ 开窗时把左竖导航的 SOCIAL 键点亮（回点导航钮）——
            // 我们让主菜单侧自己处理（同 `RewardsWindow.Open` 那条注释），这里只出声。
            Debug.Log("[Social] 社交窗已开（默认页 = Alliances）。⚠️ 原版 `updateNavPanel=1` 会回点左竖导航第 5 键，"
                    + "那一步归主菜单侧。");
        }

        /// <summary>建整个窗口（**自检与运行时同一条路**）。外壳走基类 `BuildShell`（与奖励窗/商店同一份）。</summary>
        public void Build()
        {
            var res = BuildShell(transform, Buttons, "SocialTabButton_", "Tabs");
            tabButtons = res.buttons;
            _btnRoot = res.roots;
            _btnHighlight = res.highlight;
            _btnBadge = res.badge;

            // 视觉顺序 = 原版 `Tab Buttons` 下两个孩子的顺序（Alliances → Friends），也是 `tabs[]` 的顺序
            visualTypes.Clear();
            visualTypes.Add(WindowTabType.SocialAlliances);
            visualTypes.Add(WindowTabType.SocialFriends);

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Social] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
        }

        void BuildTabContents()
        {
            tabs.Clear();
            // 🔴 **两页的根矩形 = `Content Area`/`Tabs` 那个矩形**（167.17,70.94→1920.00,1080.00），
            //    §A·1 第 47/380 行实测 —— ⚠️ **不是**基类那对 `TabL/TabT`（166.69,69.20，那是奖励窗的实测值）。
            var tabsR = new PxRect(ContentL, ContentT, ContentR, ContentB);

            var a = Node(tabHolder, "Alliances Tab", tabsR);
            PageAlliances = a.gameObject.AddComponent<AlliancesTab>();
            PageAlliances.SetHost(this);
            tabs.Add(PageAlliances);

            var f = Node(tabHolder, "Friends Tab", tabsR);
            PageFriends = f.gameObject.AddComponent<FriendsTab>();
            PageFriends.SetHost(this);
            tabs.Add(PageFriends);

            // 🔴 **顺序要紧**：`Setup()` 必须在**两个页都还 active** 的时候跑完 ——
            //    下面 `Click(0)` 会把 Friends 关掉，而 TMP 在对象没激活时量不出尺寸
            //    （`CLAUDE.md` §三 那条坑：`ForceMeshUpdate` 要在 `SetActive(true)` 之后）。
            //    奖励窗/档案窗都是这个次序，别调换。
            foreach (var t in tabs) t.Setup();
        }

        /// <summary>选中态：**只画选中的那一个**（原版两键出厂都亮、可见性由运行时 `TabButtons` 驱动）。</summary>
        public override void RefreshHighlights()
        {
            int sel = tabButtons != null ? tabButtons.CurrentVisualIndex : -1;
            for (int i = 0; i < _btnHighlight.Length; i++)
                if (_btnHighlight[i] != null) _btnHighlight[i].gameObject.SetActive(i == sel);
        }

        /// <summary>两个键的红点。原版由 `UiBadgeNotificationManager` 事件驱动 ——
        /// **我们没有任何通知源**（邀请/好友请求都在服务器）⇒ 恒 alpha 0（= 不显示），如实出声。
        /// 🔴 判据**只此一份**：本函数。别在页里再写一遍。</summary>
        public void RefreshBadges()
        {
            for (int i = 0; i < _btnBadge.Length; i++)
                if (_btnBadge[i] != null)
                    _btnBadge[i].SetTint(new Color(0.7373f, 0.7373f, 0.7373f, 0f));
        }

        /// <summary>开聊天窗（原版 `ChatPreview.OpenChat` → `WindowsManager.OpenWindow(chatWindow)`）。
        /// 联盟页的 `ChatPreview` 那颗钮、以及主菜单右上角那颗，都走这里 —— **入口只此一份**。</summary>
        public ChatPanel OpenChat()
        {
            var wm = Manager != null ? Manager : WindowsManager.Instance;
            if (wm == null) { Debug.LogWarning("[Social] 没有 `WindowsManager` ⇒ 聊天窗开不了"); return null; }
            var chat = ChatPanel.Create(wm);
            wm.OpenWindow(chat);
            Debug.Log("[Social] 开聊天窗（原版 `ChatPreview.OpenChat`，频道 = `ChatRoom.Global`）。");
            return chat;
        }
    }

    /// <summary>社交窗一页的公共底座。原版是 `WindowTabBase&lt;SocialMenuWindow&gt;`。
    /// 画图一律走宿主窗那套（`Win.Rect` / `Win.Art`）—— **画图层只有 `MenuDraw` 一份**，别在这再写一份。</summary>
    public abstract class SocialPage : WindowTabBase
    {
        /// <summary>宿主窗（`BuildTabContents` 建完就赋）。</summary>
        public SocialWindow Win;

        /// <summary>本页在 `Tabs` 表里的下标（0 = Alliances · 1 = Friends）—— 决定队列档。</summary>
        protected abstract int PageIndex { get; }

        /// <summary>本页的队列起档（两页各占 10 个号，互不重叠）。⚠️ **public 是给 `SocialView` 用的**。</summary>
        public int Q { get { return SocialWindow.QPageBase + PageIndex * 10; } }

        protected Transform Root { get { return transform; } }

        /// <summary>**裁切边界**（画布像素）。等价于原版 `Viewport` 上那个 `RectMask2D`。
        /// 滚动区在画内容**之前**设一次、画完清掉（照 `ForgeTab.BuildRewardCells` 的用法）。</summary>
        protected PxRect? Clip;

        /// <summary>给 `SocialView` 读的只读口（子视图没有自己的 `Clip`）。</summary>
        public PxRect? ClipNow { get { return Clip; } }

        /// <summary>取图（走宿主窗那一个入口；取不到会记进 `MissingArt`）。
        /// ⚠️ `SocialView`（子视图）也要用 ⇒ **public**。</summary>
        public Texture2D ArtOf(string name) { return Win != null ? Win.Art(name) : null; }

        protected static Transform Node(Transform parent, string name, PxRect r) { return MenuDraw.Node(parent, name, r); }
        protected Transform Node(string name, PxRect r) { return MenuDraw.Node(Root, name, r); }

        /// <summary>按矩形摆一张图（`art == null` = 纯色块）。取不到图 ⇒ 返回 null 并**记进宿主的 `MissingArt`**。
        /// ⚠️ **public 是给 `SocialView` 用的**（子视图没有自己的 `SocialPage` 基类）。</summary>
        public ImageQuad Rect(Transform parent, string art, PxRect r, string name, int qOff,
                              Color? tint = null, bool keepAspect = false)
        {
            var tex = art == null ? CardArt.Solid() : Win.Art(art);
            return MenuDraw.Rect(parent, tex, r, name, Q + qOff, tint, keepAspect, Clip);
        }

        /// <summary>九宫格（原版 `Image.Type = Sliced`）。`border` 按**贴图原始像素**给（L,B,R,T）。</summary>
        public GameObject Nine(Transform parent, string art, PxRect r, Vector4 border, string name, int qOff,
                               Color? tint = null, bool fillCenter = true)
        {
            var tex = Win.Art(art);
            if (tex == null) return null;
            return MenuDraw.Nine(parent, tex, r, border, tex.width, tex.height, Q + qOff, tint, fillCenter, name);
        }

        /// <summary>限宽换行 + 可选自适应字号（原版 `m_TextWrappingMode=1` + autosize）。
        /// 🔴 别用 `SetFontSize(px/108)` —— 那会大 2.7 倍；这条路走 `SetGlyphHeight`。</summary>
        public Label Text(Transform parent, PxRect r, string text, Color color, string name, float fontPx,
                          int qOff, float autoMinPx = 0f, bool alignLeft = true)
        {
            if (Clip.HasValue && (r.x2 <= Clip.Value.x1 || r.x1 >= Clip.Value.x2)) return null;
            var lb = MenuDraw.TextBox(parent, r, text, color, name, fontPx, autoMinPx, Q + qOff);
            if (lb != null && alignLeft) MenuDraw.AlignLeft(lb, r);
            return lb;
        }

        /// <summary>一个**透明点击区** + `WindowButton`（`PointerLayer` 扫的就是它）。</summary>
        public Transform Hit(Transform parent, string name, PxRect r, int qOff, System.Action onClick)
        { return MenuDraw.Hit(parent, name, r, Q + qOff, onClick); }

        /// <summary>点了**还没接**的东西 —— 一律出声（红线：不许静默失败）。</summary>
        public static void Say(string what)
        { Debug.Log("[Social] " + what); }
    }

    /// <summary>社交窗里的**子视图**（在某一页内部再分的那几支：`AllianceSearchTab` / `AllianceMemberTab` /
    /// `CreateAllianceMenu`…）。原版这些是挂在页根下面的**独立组件**，不是页。
    /// 它们共用宿主页那一套画图助手，只多一个**页内队列偏移**（`QOff`）：
    /// 一页 10 个号，子视图各占一段，互不重叠（同 `MenuWindowBase.QPanel/QContent` 那条纪律）。</summary>
    public abstract class SocialView : MonoBehaviour
    {
        /// <summary>宿主页（`AlliancesTab` / `FriendsTab`）—— 取图与队列档都走它。</summary>
        public SocialPage Page;

        /// <summary>本视图在**页内**的队列偏移（页那一档是 `SocialWindow.QPageBase + PageIndex*10`）。</summary>
        protected abstract int QOff { get; }

        protected Transform Root { get { return transform; } }

        /// <summary>宿主窗（取图、开别的窗都要它）。</summary>
        protected SocialWindow Win { get { return Page != null ? Page.Win : null; } }

        protected static Transform Node(Transform parent, string name, PxRect r) { return MenuDraw.Node(parent, name, r); }
        protected Transform Node(string name, PxRect r) { return MenuDraw.Node(Root, name, r); }

        public ImageQuad Rect(Transform parent, string art, PxRect r, string name, int qOff,
                                 Color? tint = null, bool keepAspect = false)
        { return Page.Rect(parent, art, r, name, QOff + qOff, tint, keepAspect); }

        public GameObject Nine(Transform parent, string art, PxRect r, Vector4 border, string name, int qOff,
                                  Color? tint = null, bool fillCenter = true)
        { return Page.Nine(parent, art, r, border, name, QOff + qOff, tint, fillCenter); }

        public Label Text(Transform parent, PxRect r, string text, Color color, string name, float fontPx,
                             int qOff, float autoMinPx = 0f, bool alignLeft = true)
        { return Page.Text(parent, r, text, color, name, fontPx, QOff + qOff, autoMinPx, alignLeft); }

        public Transform Hit(Transform parent, string name, PxRect r, int qOff, System.Action onClick)
        { return Page.Hit(parent, name, r, QOff + qOff, onClick); }

        /// <summary>**装饰品立绘**（`Resources/Art/avatars/` 那批，名字含空格、原样传）。
        /// 走 `CardArt.Cosmetics` 而**不是** `MenuUi` —— 两批的目录与命名规矩都不同。</summary>
        public ImageQuad Cosmetic(Transform parent, string art, PxRect r, string name, int qOff, bool keepAspect = true)
        {
            Texture2D tex = null;
            if (!string.IsNullOrEmpty(art))
            {
                tex = CardArt.Cosmetics(art);
                if (tex == null && Page != null && Page.Win != null && !Page.Win.MissingArt.Contains(art))
                    Page.Win.MissingArt.Add(art);
            }
            return MenuDraw.Rect(parent, tex, r, name, Page.Q + QOff + qOff, null, keepAspect, Page.ClipNow);
        }

        protected static void Say(string what) { SocialPage.Say(what); }
    }
}
