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
        // ⚠️ **弹窗要再高一档** —— 🔴 **2026-10-04 更正（Y5 查出，铁律 5）**：这里原来写「`MessagePopupWindowDuel` 用 **3250+**」——
        //    **`MessagePopupWindowDuel` 就是 `DuelPopupWindow`**（`DuelPopupWindow.cs:2,33`），它的 **`QBase = 3400`**（`:38`），
        //    而**全工程没有一处用 3250**（唯一出现就是这句注释）。`RewardsScene` 那条「弹窗 > 页 > 窗」的断言仍然成立（3400 > 3200）。
        //    📌 **层带地图**（2026-10-04 实测扫过全工程的 `const int Q*`）：3002–3044 · 3070–3103 · 3104–3119 · 3120–3135 ·
        //    3140–3169 · 3170–3197 · **3200–3209（本页）** · **3210–3299（奖杯格「按格号错开」那一段，最多 15 格 × 6 号）** ·
        //    **3300–3308（聊天窗）** · 3400–3405 · 3450–3458 · 3500–3520 · **3600–3604（顶栏）** ·
        //    **3605–3607（tooltip —— 2026-10-04 从 3199–3201 搬来，判据是原版兄弟序里 `TooltipManager` 排在 `Upper bar`/`3 - PopUp Holder` 之后）**。
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
        /// <summary>🆕 2026-10-03：高亮层要**整棵**开关（九宫格）⇒ 留一份 `BarResult`。</summary>
        MainMenuSubmenuWindow.BarResult _btnRes;
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
            _btnRes = res;
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
            // 🆕 2026-10-03：**整棵九宫格一起开关**（`Highlight` 现在是 `Sliced`，一棵树 9 个 quad）
            MainMenuSubmenuWindow.SetHighlight(_btnRes, sel);
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
        /// 滚动区在画内容**之前**设一次、画完清掉（照 `ForgeTab.BuildRewardCells` 的用法）。
        /// 🔴 **写入口 = `SetClip`（下面那个）** —— 别把这个字段改成 `public`：`MainMenuSubmenuWindow.Clip`
        /// （`MenuWindowBase.cs` 那一份）那种「谁都能直接写」的写法在这里会绕过「画完清掉」那条纪律，
        /// 而本页的 `Clip` 是**逐次临时**的。</summary>
        protected PxRect? Clip;

        /// <summary>给 `SocialView` 读的只读口（子视图没有自己的 `Clip`）。</summary>
        public PxRect? ClipNow { get { return Clip; } }

        /// <summary>🆕 **2026-10-03（A25①）：给本页 `Clip` 补的写入口。**
        /// <para>**为什么原来没有**：`Clip` 是 `protected`，而**滚动视口归子视图（`SocialView`）所有**
        /// （`AllianceSearchTab` 的 `Open Alliances>Viewport`、`AllianceGeneralDetails` 的
        /// `MemberList>Scroll View>Viewport`），子视图**不是** `SocialPage` 的派生类 ⇒ 按 C# 的 `protected`
        /// 规则**碰不到这个字段** ⇒ 全仓**一处赋值都没有**，`Rect` / `Text` / `Hit` /
        /// `Cosmetic` 四处转发过去的 `Clip` **恒为 null = 恒 no-op**。</para>
        /// <para>用法（照别的页：**画内容之前设一次、画完清掉**）：
        /// <c>SetClip(vpR); 建内容; SetClip(null);</c>
        /// —— 不清的话，后面画的件会**继续**吃这道裁切（那一类失败是静默的）。</para>
        /// <para>✅ **2026-10-03（A25④）三个调用点都接上了**（本行原来写「还没接上、不要以为现在已经生效」，
        /// 那句已过期）：本批把三处「建行/建格」的前后各加了 `SetClip(视口)` / `SetClip(null)` ——
        /// ① `AlliancesTab.cs` 的 `AllianceSearchTab.BuildRows`（`Open Alliances>Viewport`）；
        /// ② `FriendsTab.cs` 的 `BuildRows`（`Friends Container>Viewport`）；
        /// ③ `AllianceMemberTab.cs` 的 `AllianceMemberRow.BuildAll`（`MemberList>Scroll View>Viewport`）。
        /// 本页的 `Text` 也在同一批改成走 `MenuDraw.ClipRect`（原来**只判横轴** —— 三处视口全是纵向，
        /// 不补的话纵向越界的文字照样画到框外）。</para>
        /// <para>三处视口都是**原版那一格的真值**（普查 §A·1）：
        /// ① `Open Alliances>Viewport` = 360.99,337.29→1874.90,1079.77（原版身上是 `Image + RectMask2D`）；
        /// ② `Friends Container>Viewport` = 332.15,314.80→1875.80,1080.06（原版身上是 `RectMask2D`）；
        /// ③ `MemberList>Scroll View>Viewport` = 369.67,493.63→1880.67,1080.05（原版身上是 `Image + Mask`）。
        /// 接法 = 在那三处「建行/建格」的前后各一行（`SetClip(视口)` / `SetClip(null)`），
        /// 子视图里用 `SocialView.SetClip`（它转调这里）。</para>
        /// <para>⚠️ **`public`（不是 `internal`）** —— 本想让调用面收窄成 `internal`，**实测不成立**：
        /// 自检在**编辑器程序集**（`Editor/MainMenuScene.cs`）里，跨程序集看不到 `internal`
        /// ⇒ `csc` 报 `CS1061 …未包含 SetClip 的定义`（2026-10-03 跑 `工具/typecheck.sh` 得到）。
        /// 那三处生产调用点与本类同程序集，`internal` 对它们够用、对自检不够 ⇒ 只能 `public`。</para></summary>
        public void SetClip(PxRect? r) { Clip = r; }

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

        /// <summary>九宫格（原版 `Image.Type = Sliced`）。`border` 按**贴图原始像素**给（L,B,R,T）。
        /// 🆕 **2026-10-03（A25①）**：`Clip` 也传下去了 —— 与 `MenuWindowBase.Nine` **同一件事**
        /// （那边 `:198-215` 的注释写着为什么必须由「持有 `Clip` 的那一层」转传：各页拿不到窗口的 `Clip`）。
        /// 本页原来 `Rect` / `Text` / `Hit`（子视图那边还有 `Cosmetic`）都传了、**唯独九宫格这一路漏了**
        /// —— 截图上看不出来，因为断言量的是「节点在不在」、量不到「画多出去了」。
        /// `Clip` 为空时行为一字不变。</summary>
        public GameObject Nine(Transform parent, string art, PxRect r, Vector4 border, string name, int qOff,
                               Color? tint = null, bool fillCenter = true)
        {
            var tex = Win.Art(art);
            if (tex == null) return null;
            return MenuDraw.Nine(parent, tex, r, border, tex.width, tex.height, Q + qOff, tint, fillCenter, name,
                                 clip: Clip);
        }

        /// <summary>限宽换行 + 可选自适应字号（原版 `m_TextWrappingMode=1` + autosize）。
        /// 🔴 别用 `SetFontSize(px/108)` —— 那会大 2.7 倍；这条路走 `SetGlyphHeight`。
        /// <para>🆕 **2026-10-03（A25④）求交只剩一份**：这一处原来**自己判了一遍横轴**
        /// （`r.x2 &lt;= Clip.x1 || r.x1 &gt;= Clip.x2`）⇒ 与 `Rect` / `Nine` / `Hit` / `Cosmetic` 那四路
        /// **不是同一条判据**（那四路都转调 `MenuDraw.ClipRect`）。在**横向**滚动区里两种写法等价，
        /// 而社交这三处视口**全是纵向**的 ⇒ 纵向越界的文字照样画到框外，而且**是静默的**
        /// （断言量「节点在不在」，量不到「画多出去了」）—— 这正是把三处滚动接上之后会**真的**现形的缺陷。
        /// ⇒ 收口成 `MenuDraw.ClipRect`（它**转调** `MenuDraw.Visible` —— **全工程唯一一份**求交），
        /// 与 `MenuWindowBase.Text` 同一口径。⚠️ 2026-10-07 更正（铁律 5 / A12①）：原文只写到 `ClipRect`
        /// 为止；收口后唯一一份是 `Visible`，`ClipRect` = 它的「顺带夹出可见矩形」版。
        /// ⚠️ `Clip == null`（绝大多数时候）时行为一字不变：`ClipRect` 第一句就是 `return true`。
        /// ⚠️ 仍然是「**整块**在框外就不建」（文字没法截 uv；部分越界的字按原样画）—— 这条缺口在
        /// `MenuWindowBase.Clip` 的注释里记着（`项目任务.md` §三 第 29 条 A9），本处**同一条口径**、不是新缺口。</para></summary>
        public Label Text(Transform parent, PxRect r, string text, Color color, string name, float fontPx,
                          int qOff, float autoMinPx = 0f, bool alignLeft = true)
        {
            // 🔴 求交那一份 = `MenuDraw.Visible`（本行走它的夹取版 `ClipRect`；别在这儿再写一遍 `Max/Min`）。
            if (!MenuDraw.ClipRect(r, Clip, out _)) return null;
            var lb = MenuDraw.TextBox(parent, r, text, color, name, fontPx, autoMinPx, Q + qOff);
            if (lb != null && alignLeft) MenuDraw.AlignLeft(lb, r);
            return lb;
        }

        /// <summary>一个**透明点击区** + `WindowButton`（`PointerLayer` 扫的就是它）。
        /// 🆕 **2026-10-03**：把本页的 `Clip` 传下去 —— 视口外的命中区**不建**、压在视口边上的**截到视口内**
        /// （判据 = 原版 `RectMask2D` 的射线那一面，见 `MenuDraw.Hit` / `ClipRect`）。
        /// 本页的 `Rect` 早就传了 `Clip`，**只有这里漏了** —— 一旦给 `Clip` 赋了值，
        /// 这两处的行为就会**不一致**（图会裁、点击区不裁），补上才是同一套。
        /// ⚠️ **2026-10-03（A25①）更正**：原文写「`Clip` 全仓**没有一处给它赋过值**…这一处今天是空转」
        /// —— 前半句当时是对的，**现在补了写入口**（`SocialPage.SetClip`，`SocialView` 也有一个转调它）；
        /// **后半句在 A25④（同一批的下一步）也已经不成立**：三个滚动视口的调用点**都接上了**
        /// （清单在 `SetClip` 的注释里）⇒ 生产路径上这一处**已经带电**，子视图画的件也跟着吃到。</summary>
        public Transform Hit(Transform parent, string name, PxRect r, int qOff, System.Action onClick,
                             ImageQuad target = null, string art = null,
                             string hoverArt = null, string pressedArt = null)
        { return MenuDraw.Hit(parent, name, r, Q + qOff, onClick, target, art, hoverArt, pressedArt, Clip); }

        /// <summary>点了**还没接**的东西 —— 一律出声（红线：不许静默失败）。</summary>
        public static void Say(string what)
        { Debug.Log("[Social] " + what); }

        /// <summary>🆕 2026-10-03（A25④）：把社交这三处的滚动区登记给指针层（滚轮才会找到它）——
        /// **三处都走这一份**（收口，别在页里各写一遍 `PointerLayer.RegisterScroll`）。
        /// 🔴 **不静默**：指针层还不在场景里时 `PointerLayer.RegisterScroll` 是**空转**（登记表都没建）⇒
        /// 滚轮永远落不到这一格上，而画面看着完全正常（正是本批要治的那类缺陷）。
        /// 正常路径上它一定在：`PointerLayer` 与窗口管理器**同生共死**
        /// （`WindowsManager.Awake` / `EnsureHost` 都调 `PointerLayer.Ensure`）。
        /// 🔴 **2026-10-04（A35⑧）补第二条判据：`Owner` 也要判** —— `PointerLayer.PruneScrolls`
        /// （`PointerLayer.cs:218-227`）把 `Owner == null` 当**死的**条目删掉（那条注释写着
        /// 「`s.Owner == null` 有两个来源：**指向的对象被销毁**（Unity 的假 null）与**从没设过**」）
        /// ⇒ 忘设 `Owner` 的滚动区**下一次登记时会被悄悄删掉**，而这条路原来一声不响
        /// （画面照样正常，只是滚轮永远落不上去）。**出声，但仍然登记**（不改行为）。</summary>
        public static void RegisterScroll(MenuScroll s)
        {
            if (s == null) return;
            if (PointerLayer.Instance == null)
            {
                Debug.LogWarning("[Social] 指针层还不在场景里 ⇒ 滚动区**没登记上**（滚轮不会落到它上面）；"
                               + "先走 `WindowsManager.EnsureHost()` 再开窗。");
                return;
            }
            if (s.Owner == null)
                Debug.LogWarning("[Social] 登记滚动区时 `Owner` 是空的 ⇒ 它会被 `PointerLayer.PruneScrolls` "
                               + "当**死条目**删掉（滚轮不会落到它上面）。登记之前要先 "
                               + "`s.Owner = 宿主 GameObject`（本页三处 + `BattleLogPopup` 都设了）。");
            PointerLayer.RegisterScroll(s);
        }
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

        /// <summary>🆕 **2026-10-03（A25①）**：本视图画内容时的裁切边界 —— **转给宿主页**
        /// （`Clip` 归页所有，`Rect`/`Text`/`Hit`/`Cosmetic` 四件都转发到页上）。
        /// **滚动视口建在视图这一层**（`AllianceSearchTab` / `AllianceGeneralDetails`）⇒ 由视图设、视图清：
        /// <c>SetClip(_vpR); 建行; SetClip(null);</c>
        /// ⚠️ 页没接上时**不静默**：喊一声再去改（`Page == null` = 谁忘了 `Page = this`）。
        /// 为什么是 `public`（而不是 `internal`）——见 `SocialPage.SetClip` 最后一段。</summary>
        public void SetClip(PxRect? r)
        {
            if (Page == null)
            {
                Debug.LogWarning("[Social] 某个 `SocialView` 调 `SetClip` 时 `Page` 是 null —— 裁切没生效"
                               + "（视图的宿主页没接上，见 `SocialWindow.cs` 的 `SocialPage.SetClip`）");
                return;
            }
            Page.SetClip(r);
        }

        public ImageQuad Rect(Transform parent, string art, PxRect r, string name, int qOff,
                                 Color? tint = null, bool keepAspect = false)
        { return Page.Rect(parent, art, r, name, QOff + qOff, tint, keepAspect); }

        public GameObject Nine(Transform parent, string art, PxRect r, Vector4 border, string name, int qOff,
                                  Color? tint = null, bool fillCenter = true)
        { return Page.Nine(parent, art, r, border, name, QOff + qOff, tint, fillCenter); }

        public Label Text(Transform parent, PxRect r, string text, Color color, string name, float fontPx,
                             int qOff, float autoMinPx = 0f, bool alignLeft = true)
        { return Page.Text(parent, r, text, color, name, fontPx, QOff + qOff, autoMinPx, alignLeft); }

        public Transform Hit(Transform parent, string name, PxRect r, int qOff, System.Action onClick,
                             ImageQuad target = null, string art = null,
                             string hoverArt = null, string pressedArt = null)
        { return Page.Hit(parent, name, r, QOff + qOff, onClick, target, art, hoverArt, pressedArt); }

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
