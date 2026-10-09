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
        //    **`MessagePopupWindowDuel` 就是 `DuelPopupWindow`**（`DuelPopupWindow.cs`：类名 + 它自己的 `QBase` 常量），它的 **`QBase = 3400`**，
        //    而**全工程没有一处用 3250**（唯一出现就是这句注释）。`RewardsScene` 那条「弹窗 > 页 > 窗」的断言仍然成立（3400 > 3200）。
        //    📌 **层带地图**（2026-10-04 实测扫过全工程的 `const int Q*`；🔴 **2026-10-11（A307）就地订正**）：
        //    **2986–2998（顶栏 —— 原来写 `3600–3604`：同日的 A283 按用户裁定「照原版」把整条顶栏降到
        //      【每一扇窗之下】，判据 → `MainMenuRuntime.QBarPanel` 那段。⚠️ **2026-10-18 订正**：区间原来写
        //      `2994–2998` —— 2026-10-17 F5 把下沿放宽到 `2986`，**唯一出处 = `Shell/TopBar.cs` 那张表**）**
        //      · 3002–3044 · 3070–3103 ·
        //    3104–3119 · 3120–3135 · 3140–3169 · 3170–3197 · **3200–3209（本页）** ·
        //    **3210–3299（奖杯格「按格号错开」那一段，最多 15 格 × 6 号）** ·
        //    **3300–3308（聊天窗）** · 3310–3326（`TrophyInfoPopup`，2026-10-07 A95 加的）·
        //    3400–3405 · 3450–3458 · 3500–3520 ·
        //    **3605–3607（tooltip —— 2026-10-04 从 3199–3201 搬来，判据是原版兄弟序里 `TooltipManager` 排在 `Upper bar`/`3 - PopUp Holder` 之后）**。
        //    ⚠️ **`2999–3011` 不在本图上**（`Deck/DeckRuntime.cs` 那一套：`QSep=2999` / `QSide=3000` / `QDoneHl=3001`）——
        //      卡组编辑器与主菜单**不同场**（独立场景）⇒ 今天不冲突；顶栏取 **2986–2998** 正好压在它**下面**（方向是对的，
        //      ⚠️ **2026-10-18 订正**：这里原写 `2994–2998`；F5 放宽了下沿 ⇒ 顶栏更低、这条结论更宽）。
        //      哪天两者同场，这张图要补那一段（A283 报告 §⑥·7）。
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

        /// <summary>🆕 **2026-10-18（第七轮）**：一颗页签标签对应的词条键（= 原版那颗 `TabButtonLabel` 的
        /// `Localize.mTerm` 原文；父链 `TabButtonLabel &lt; Label &lt; {Alliances,Friends} Tab Button &lt; Tab Buttons
        /// &lt; Content Area &lt; Social Submenu Variant` ⇒ 与我们这两颗**逐节同名**）。
        /// **`null` = 名字对不上任何键** ⇒ 保持原样（⛔ 不静默编词条）。</summary>
        static string LabelTermFor(string label)
        {
            switch (label)
            {
                case "Alliances": return "SocialMenu/Alliances";   // 1 颗 · TMP 原文 `Alliances`
                case "Friends":   return "SocialMenu/Friends";     // 1 颗 · TMP 原文 `Friends`
                default:          return null;
            }
        }

        /// <summary>🔴 **2026-10-18（第七轮）**：把静态那份 <see cref="Buttons"/> 的**文案**换成词条
        /// —— **必须在【建窗时】做**，不能在 `Buttons` 的初始化式里调 `Loc.T`：
        /// 那是 **`static` 初始化**，只在类首次被加载时求值一次 ⇒ 之后换语言它**不会变**，而且比任何界面都早。
        /// <para>只替换 `Label` 一个字段；`Art` / `InstId` / 字号 / `BadgeDy` / `AutoBase` **原样带过去**
        /// （`TabBtnSpec` 是 `struct`，`Shell/MenuWindowBase.cs` 那个构造带缺省参数）。</para>
        /// <para>⚠️ 渲染时基类会 `ToUpperInvariant()`：英文档印 `ALLIANCES` / `FRIENDS`（照原版），
        /// 中文（CJK）不受影响。</para></summary>
        static TabBtnSpec[] LocalizedButtons()
        {
            var src = Buttons;
            var dst = new TabBtnSpec[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                var s = src[i];
                var term = LabelTermFor(s.Label);
                dst[i] = new TabBtnSpec(s.Art, term == null ? s.Label : Loc.T(term),
                                        s.FontPx, s.AutoMin, s.AutoMax, s.InstId, s.BadgeDy, s.AutoBase);
            }
            return dst;
        }

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
            // 🔴 **2026-10-18（第七轮）**：传 `LocalizedButtons()`（建窗时把两颗页签的**文案**换成词条），
            //   ⛔ **不是**静态那份 `Buttons`（静态初始化早于任何语言设置，见 `LocalizedButtons` 的 doc）。
            //   节点名（`SocialTabButton_{i}`）与图标/字号/角标那些字段**一个字没变**。
            var res = BuildShell(transform, LocalizedButtons(), "SocialTabButton_", "Tabs");
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
        /// 联盟页的 `ChatPreview` 那颗钮、以及主菜单右上角那颗，都走到**同一套「按引用复用」**上。
        /// 🔴 **2026-10-11（A177）就地订正（铁律 5）**：本行原文写「**入口只此一份**」—— **不成立**
        /// （主菜单右上角那颗走的是 `MainMenuRuntime.OpenChat`，是**第二份**入口），而且两条入口
        /// **原来各自直调 `Create`** ⇒ 同一扇窗连点两次 = **叠出两扇**。现在改走
        /// `WindowsManager.OpenByRef`（= 原版 `automaticallyLoadedWindows` 命中就复用的等价物，A177 收编）。
        /// ⚠️ 如实记一条**未合并**的尾巴：`MainMenuRuntime` 那一份缓存今天还在（红线不许本件动那个文件）
        /// ⇒ 「先从这里开、再从主菜单那颗点」**暂时**仍是两扇 —— 与改前一样、没变得更糟；
        /// 合并办法写在 `WindowsManager.OpenByRef` 的注释里。</summary>
        public ChatPanel OpenChat()
        {
            var chat = WindowsManager.OpenByRef(WindowsManager.PrefabRefChat, m => ChatPanel.Create(m));
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

        // 🔴 **2026-10-14（A753 = A744 的「全删」）：本类原来那个 `Clip` 字段（连同 `ClipNow` / `SetClip`
        //   这一对读写口）【已整体删掉】** —— 本类里那四处转发（`Rect` / `Nine` / `Text` / `Hit` / `Cosmetic`）
        //   现在一律传 `clip = null`，裁切由**父链上那颗 `ViewportClip` 节点**说了算
        //   （三段优先级见 `Shell/ViewportClip.Resolve`；⛔ 别再给本类补回「显式覆盖」那一档）。
        //
        //   📌 **三处视口的原版真值（判据，⛔ 别删）**（普查 §A·1；⇐ **那三颗节点就是照这三个矩形建的**）：
        //   ① `Open Alliances>Viewport` = 360.99,337.29→1874.90,1079.77（`Shell/AlliancesTab.cs` 的 `OpenViewportR`）；
        //   ② `Friends Container>Viewport` = 332.15,314.80→1875.80,1080.06（`Shell/FriendsTab.cs` 的 `ContainerR`）；
        //   ③ `MemberList>Scroll View>Viewport` = 369.67,493.63→1880.67,1080.05（`Shell/AllianceMemberTab.cs` 的 `MemberScroll.Viewport`）。
        //   （另：奖杯那一格 `TrophiesWindow>Scroll Rect` 的视口 = `AllianceMemberTab.TrophyScrollR`。）
        //   ⚠️ 三处**软边**：只有奖杯那一格非 0（`(0,50)`）—— 现在写在节点上（`TrophyClipSoftness`）。

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
            // 🔴 **2026-10-14（A753）**：尾参原来是本页的 `Clip`（显式覆盖那一档）—— 字段已整体删掉
            //    ⇒ 走缺省 `clip = null`：由**父链上那颗 `ViewportClip`** 说了算。
            return MenuDraw.Rect(parent, tex, r, name, Q + qOff, tint, keepAspect);
        }

        /// <summary>九宫格（原版 `Image.Type = Sliced`）。`border` 按**贴图原始像素**给（L,B,R,T）。
        /// 🔴 **2026-10-14（A753）**：从前这里把本页的 `Clip` 当**尾参**传下去（2026-10-03 A25① 才补上 ——
        /// 当时 `Rect` / `Text` / `Hit` / `Cosmetic` 都传了、**唯独九宫格这一路漏了**，图会画到视口外）。
        /// 那个字段已整体删掉 ⇒ **现在不传 `clip`**（走 `MenuDraw.Nine` 的缺省 `null`），裁切由
        /// **父链上那颗 `ViewportClip`** 说了算 —— 与 `Rect` / `Text` / `Hit` / `Cosmetic` 同一条路。
        /// ⚠️ 这一路（与那四路）都是「**整块**在框外 ⇒ 连节点一起不建」，⛔ 不是逐像素裁。</summary>
        public GameObject Nine(Transform parent, string art, PxRect r, Vector4 border, string name, int qOff,
                               Color? tint = null, bool fillCenter = true)
        {
            var tex = Win.Art(art);
            if (tex == null) return null;
            return MenuDraw.Nine(parent, tex, r, border, tex.width, tex.height, Q + qOff, tint, fillCenter, name);
        }

        /// <summary>限宽换行 + 可选自适应字号（原版 `m_TextWrappingMode=1` + autosize）。
        /// 🔴 别用 `SetFontSize(px/108)` —— 那会大 2.7 倍；这条路走 `SetGlyphHeight`。
        /// <para>🆕 **2026-10-03（A25④）求交只剩一份**：这一处原来**自己判了一遍横轴**
        /// （`r.x2 &lt;= Clip.x1 || r.x1 &gt;= Clip.x2` —— 那句里的 `Clip` 就是本页那个**已于 A753 删掉的**
        /// 显式覆盖字段）⇒ 与 `Rect` / `Nine` / `Hit` / `Cosmetic` 那四路
        /// **不是同一条判据**（那四路都转调 `MenuDraw.ClipRect`）。在**横向**滚动区里两种写法等价，
        /// 而社交这三处视口**全是纵向**的 ⇒ 纵向越界的文字照样画到框外，而且**是静默的**
        /// （断言量「节点在不在」，量不到「画多出去了」）—— 这正是把三处滚动接上之后会**真的**现形的缺陷。
        /// ⇒ 收口成 `MenuDraw.ClipRect`（它**转调** `MenuDraw.Visible` —— **全工程唯一一份**求交），
        /// 与 `MenuWindowBase.Text` 同一口径。⚠️ 2026-10-07 更正（铁律 5 / A12①）：原文只写到 `ClipRect`
        /// 为止；收口后唯一一份是 `Visible`，`ClipRect` = 它的「顺带夹出可见矩形」版。
        /// ⚠️ **2026-10-14（A753）**：那个「显式覆盖」的 `Clip` 字段已整体删掉 ⇒ 本口现在**只**走
        /// 「父链节点」那一支（`clip` 实参恒传 `null`）；没有节点时与旧行为一字不变
        /// （`ViewportClip.Resolve` 第 3 支返回 `null` ⇒ `ClipRect` 第一句就是 `return true`）。
        /// ⚠️ 仍然是「**整块**在框外就不建」（文字没法截 uv；部分越界的字按原样画）—— 这条缺口在
        /// `MenuWindowBase.Clip` 的注释里记着（`项目任务.md` §三 第 29 条 A9），本处**同一条口径**、不是新缺口。</para>
        /// <para>🔴 **2026-10-08（A213）新增 `wrap`**：本行**恒折行**是错的 —— `MenuDraw.TextBox` 第一句就是
        /// `SetWrapWidth(框宽)`，而那个**无条件**把 `m_TextWrappingMode` 设成 `Normal(=1)`
        /// （`Core/TmpFont.cs` 的 `SetWrapWidth`）⇒ 凡走这里的件**一律折行**，可**原版逐件不同**。
        /// 判例（A213 点名的那一处）：`Shell/AlliancesTab.cs` 建盟页 `Price Display Button > Price Display > text`
        /// （出厂文本 `'1000'`）原版实读 = **`折行=0 · auto[13.46~40] · Center/Capline`**
        /// （`python 工具/menu_dump.py bundle_menus_assets_all "Social Submenu Variant" --depth 16 --md`）
        /// ⇒ 我们原来**真偏离**。
        /// 📋 **`AlliancesTab` 那 15 处逐条核过的原版 `折行`**（同一条 dump 的 `折行=` 列）：
        /// **0** = `Alliance Header Buttons/Tab buttons/Generic Tab UI Button {Search,Create}/Button Text`
        /// （`Join`/`Create`，fs60）· `List View/Search Field/Text Area/Placeholder`（`Search`，fs50）·
        /// 行尾 `Join`/`Reject` 的 `Button Text`（fs36.65/44）· `Price Display Button/…/Price Display/text`；
        /// **1** = `Invitations/Title` · `Open Alliances/Title` · 行里的 `Title`/`Region`/`Members Header`/
        /// `Member Count`/`Ranking Header`/`Ranking Value` · `Create Alliance Text` ·
        /// `Name input title` / `Desc input title` / `Select Language` / `Select Privacy`。
        /// ⇒ 只有那 **4** 处改传 `wrap: false`。
        /// <para>🔴 **2026-10-11（A317 · 已落地）：`autoMinPx` 与 `wrap` 的缺省值【删掉、形参必填】。**
        /// 判据 = `资料/普查产出_1010/调度台_口径裁定_1011.md` §A258 · `V4a_壳与共用件口径.md` §Q4：
        /// V4a **亲跑 `menu_dump`** 量过三族原版（`Profile` 112/44 · `Social` **56/44** · `Collection` 26/28）
        /// —— **取值本来就是混的** ⇒「统一到哪一套」是**伪问题**（缺省值**不是原版概念**，原版只有
        /// **逐个节点**的真值）；而缺省值今天**真在载荷**（不传就跟着它走）⇒ 去掉它才能**倒逼逐处现读**。
        /// ⛔ **别改成某一边的缺省值**（往任一边统一都当场回归另一批站点）。
        /// ⚠️ C# 要求**必填形参排在所有可选形参【之前】**（`CS1737`）⇒ `wrap` 必填就得连它前面的
        /// `autoMinPx` 一起定（前例 = 同族的 `CollectionWindow.TextAligned` **三个形参全去掉缺省**，
        /// 但那口只有两个调用点）。</para>
        /// <para>✅ **`alignLeft` 的缺省【已删 · 形参必填】—— 2026-10-12（A323 · 收尾半落地）**：
        /// 判据与理由同 `wrap`（缺省值**不是原版概念**，原版只有**逐个节点**的真值）⇒ 去掉它才能**倒逼逐处现读**。
        /// 全仓 **13 处**阻塞点各补了一句实参（**零行为变化**：缺省本来就是 `true`，而那 13 处原版**全是 `Left`**）：
        /// · `Shell/AlliancesTab.cs` **9 处** = `Placeholder`（`List View/Search Field/Text Area`）·
        ///   `Invitations/Title` · `Open Alliances/Title` · 行 `Title` · 行 `Region` · 行 `Ranking Value` ·
        ///   `Create Alliance Text` · `Field` 的标题（`Name`/`Desc` 两处共用）· `Dropdown` 的标题
        ///   （`Language`/`Privacy` 两处共用）；
        /// · `Shell/FriendsTab.cs` **4 处** = `Placeholder` · `Search Player` · `Friends Title` · 行 `Friend name`。
        /// 📌 逐处真值 = **本批现读** `python 工具/menu_dump.py bundle_menus_assets_all "Social Submenu Variant"
        /// --depth 16 --md` 的 `对齐=` 列（`AlliancesTab` 9 处 = `Left/Midline`×4 + `Left/Middle`×5 ——
        /// ⚠️ 两个标题各被 `Name`/`Desc`、`Language`/`Privacy` **共用**，**两两都是 `Left`**）；
        /// `Friend name` 那处在**独立根**上（`menu_dump … "Friend Info Item" --depth 6 --md` ⇒ `Left/Midline`），
        /// 同批 `普查产出_1011/W5_A307_A255_A258.md` §五·4 已核过；这一族的逐处真值**不再另抄一份**。
        /// <para>⚠️ **上一轮（C1）的记账 —— 保留痕迹**：A323 立项时以为**唯一的阻塞点是 `AllianceMemberTab` 那 13 处**；
        /// A319 把**那 13 处逐条现读补全**（真值表 → `Shell/AllianceMemberTab.cs` 的 `Toggle` 上方），
        /// 但**真删缺省才露出真阻塞点** —— C1 实跑类型检查（`TMPDIR=/tmp/wf_c1 bash d:/4/Unity/工具/typecheck.sh`）
        /// 后 `CS7036` 一共 **13 条**，**全部**落在 `AlliancesTab.cs` / `FriendsTab.cs` 这两个**当时白名单外**的文件上。</para>
        /// <para>⚠️ 上一批（A258 · W5）已把「还没显式声明 `wrap`」的 **21 处逐处按原版实读补齐**
        /// （`AlliancesTab` **11** · `FriendsTab` **3** · `AllianceMemberTab` **7**）⇒ 那三个文件
        /// **加了缺省值之后一个字都不用再动**；先前那几批已经传 `false` 的 **11 处**也一个字没动。
        /// 📋 真值出处 = `资料/普查产出_1008/波C3_A212其余_A213_A214.md` §A213 表 A/B。
        /// ⚠️ `wrap: true` = `MenuDraw.TextBox` 里那句 `SetWrapWidth` 的既有效果（`Normal`）
        /// ⇒ 与原来的缺省逐字等价。</para>
        /// </para>
        /// <para>🔴 **本批（A317）唯一的调用点改动 = 一处「探针」**：
        /// `Editor/MainMenuScene.cs` 的 `A25①` `Clip` 探针（原来是 7 实参 ⇒ `CS7036`）补成 9 实参。
        /// （本文件里还有一条同形的探针，但那一条**写在注释里**、不是真调用点。）
        /// ⛔ **别在这里再补一个「转调专用」的重载来绕开必填** —— 那是把唯一的口子藏起来。</para>
        /// <para>⚠️ 同族的 `CollectionWindow.TextAligned`（2026-10-08 已去掉缺省、档位还换成了原版
        /// `m_TextWrappingMode` 的 **`int`** 原文）与 `PlayerProfileWindow.ProfilePage.Text`
        /// （🆕 **2026-10-12（A323）`alignLeft` 也已去掉缺省** —— 它的 4 处阻塞点
        /// `AchievementsMenu`(3) + `BattleLogTab`(1) 本批进了白名单、各补了 `alignLeft: false`）
        /// ⇒ **本口是这一族里最后一个去掉 `alignLeft` 缺省的**（2026-10-12 · A323 收尾半）
        /// ⇒ **三处 `wrap` 的类型仍不统一**（一处 `int`、两处 `bool`）
        /// —— 这一条按调度台口径**如实留着**，不自己拍。</para>
        /// </para>
        /// <para>⚠️ 本口只表达 0 / 1 两档；第三档 `3` 由调用点自己在 `Text(...)` 之后
        /// <see cref="Label.SetWrappingMode"/>（先例 = `Deck/DeckRuntime.cs` 的搜索框）。</para>
        /// <para>🔴 **2026-10-16（A822）：本口【不】自己调 `MenuDraw.ClipText` —— 这是有意的，别补。**
        /// 全文件 `grep ClipText` = **0 命中**（本注释**写进来之前**跑的；今天再跑会命中本注释自身）
        /// —— R2 的 A799 全量表顺带查出，见
        /// `资料/普查产出_1015/R2_A799全量表.md` §4·2：账的「16 组包装器」里只写了 `SocialWindow.Text`，
        /// 没往下解）。**「部分越界的那半边」由下面 `MenuDraw.TextBox` 的末尾那一句裁掉**
        /// （A781 · 2026-10-13：`if (_st.RenderClip.HasValue) ClipText(lb, clip, clipSoftness);`，
        /// 见 `Shell/MenuDraw.cs` 的 `TextBox`）—— 本口恒传 `clip = null` ⇒ 走**父链上那颗 `ViewportClip`**。
        /// ⛔ **别在这儿再补一句 `ClipText`**：那就成了「同一颗字裁两刀」（= 另一条账 **A821** 记的那 7 个包装器）
        /// —— 两刀**同框幂等、画面无差异**这一点照旧。
        /// ⚠️ **2026-10-16 就地订正（铁律 5）**：这一句后面原来接着写「但会把 `TextClipUnavailable` /
        /// `TextClipUploadSkipped` 两个诊断计数**数两遍** —— 而 `MenuDraw.TextCore` 那个内层正是为了躲这个
        /// 才拆出来的」。**「数两遍」那半已过期**：A821（2026-10-15）已把口径从「次数」改成「**唯一标签**数」，
        /// 同一颗标签**只记一次**（`Battle/Label.cs` 的 `Label.TakeClipFailMark`；两处调用点 =
        /// `MenuDraw` 里那两处 `TakeClipFailMark` 调用点）⇒ **计数不再虚高**
        /// （口径原文 → `资料/已知的坑.md` 的 2026-10-15 §1）。⚠️ `TextCore` 那个内层**照旧拆着** ——
        /// 它免掉的是那一刀**本身**（白做一次 + 被重排冲掉），**不只是计数**
        /// （同口径 → `Shell/MenuDraw.cs` 的 `TextCore` 头 `:1674-1676`）。
        /// 🔴 **它真的带电**：三个使用者（`AllianceMemberTab` / `AlliancesTab` / `FriendsTab`）**各挂一颗**
        /// `ViewportClip`（A435 阶段 2），本口恒传 `clip = null` ⇒ 框由父链那颗节点解析（不是「没人管」）。
        /// 本口第一句 `ClipRectAbove` 那是**另一件事**：**整块**在框外 ⇒ 连节点一起不建
        /// （`MenuDraw.Visible` 那一份求交；A25④ 收口 · A798 把同一道闸也加进了 `MenuDraw.Text`/`TextBox`）。</para>
        /// <para>⚠️ **A822（2026-10-15 查出）**：上面这条「顺带被 `MenuDraw.TextBox` 裁上」的链路
        /// **没有任何断言钉着** —— `Editor/MainMenuScene.cs` 的 A25① 探针只验了 `Rect`（跨边被截）、
        /// **整块**在外的 `Text`（不建）、`Hit`（跨边夹到视口沿）；**缺的是**
        /// 「`SocialPage.Text` 建出来的、**部分越界**的那一段字，它的 mesh 被截到视口沿」这一条。
        /// 断言宿主 `Editor/MainMenuScene.cs` 不在写手白名单里 ⇒ 由调度台另派（判据 = 上面的 A781 那一句）。</para>
        /// <para>🔴 **2026-10-12（A406）：追加尾参 `autoMaxPx` / `autoBasePx`** —— 语义、量纲、缺省行为
        /// **与 `MenuDraw.TextBox` 的同名形参逐字相同**（判据与全量说明 → `Shell/MenuDraw.cs` 的 `Text` 头）：
        /// 原版那一颗的 `m_fontSizeMax` / `m_fontSizeBase`（**画布 px**）；**都 `&lt;= 0` ⇒ 旧行为**
        /// （上限 = `fontPx`、base = 调用方那一档）。逐站实读值只填在 `Shell/AllianceMemberTab.cs`
        /// （A406 那一批的白名单内）。</para>
        /// <para>🔴 **2026-10-13（A414）就地订正 —— 原来这一句写的是「⇒ 本文件与 `AlliancesTab` /
        /// `FriendsTab` 的既有调用点**一个都不用改**」，**那是错的**（铁律 5：两份说法打架比没有更糟）。
        /// 真相：那句话只对「**加形参本身**不会让调用点编不过」成立（缺省 `&lt;= 0` ⇒ 旧行为，**编译上**
        /// 一个都不用改）；它**不等于**「那 19 处**没有真值可填**」。A414 逐处现读原版后确认：
        /// **`AlliancesTab` 15 处 + `FriendsTab` 4 处**里 **17 处**都有**不等于**旧行为的真值
        /// （`m_fontSizeMax` ≠ 我们传的 `fontPx`，或 `m_fontSizeBase` ≠ 调用方那一档），**已全部填上**。
        /// 逐处表（含 2 处「不适用」）→ `资料/普查产出_1013/W403_A414_对齐与字号.md` §四。</para>
        /// </summary>
        public Label Text(Transform parent, PxRect r, string text, Color color, string name, float fontPx,
                          int qOff, float autoMinPx, bool wrap, bool alignLeft,
                          float autoMaxPx = 0f, float autoBasePx = 0f)
        {
            // 🔴 求交那一份 = `MenuDraw.Visible`（本行走它的夹取版 `ClipRect`；别在这儿再写一遍 `Max/Min`）。
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）→ 🆕 2026-10-14（A753 · A744 的「全删」）**：走 **`ClipRectAbove`**
            //   （节点态那一版）——
            //   三处生产站点的 `SetClip(...)` **已整对删除**、那个「显式覆盖」字段也**已整体删掉**
            //   ⇒ 这里恒传 `null`：框由**父链上那颗 `ViewportClip`** 给。沿用裸 `ClipRect` 的话
            //   这条守卫会**永远放行**（静默：压在视口外的整段文字照建，而画面「看着没问题」）。
            if (!MenuDraw.ClipRectAbove(parent, r, null, out _)) return null;
            // 🔴 **2026-10-16（A799 · 生产 9/9）：这一句【会新裁】= 目的，⛔ 别「修」。**
            //   本口自己不调 `MenuDraw.ClipText`（A822 的有意选择，理由不在这儿抄第二份 —— 看文档头）
            //   ⇒ `clip` 恒 `null` ⇒ A781 起 `MenuDraw.TextBox` 末句沿父链解析到**使用者那一棵**的节点：
            //   `AllianceMemberTab` 的 `v.Text(row, …)`（`:1381` / `:1400` / `:1402` / `:1458` 四颗，
            //   `row` 在 `mcontent`（该文件 `:1137`）之下、VC = 该文件 `:1125` 那颗 `ViewportClip`；
            //   `AlliancesTab` / `FriendsTab` 同形）⇒ 压在视口边上的那一段字第一次被夹到视口沿。
            //   🔴 这一处是 **R2 新查出的第 9 处**（账的「16 组包装器」里只写了本口、没往下解父链，
            //   见 R2 §一② 与 §4·2）——「整族漏了」的意思：A781 顺带把它补上了，**这是好事**。
            //   ✅ 为什么可以：原版 `RectMask2D` 对文字与图片一视同仁（同页 `Rect` / `Hit` 早在裁）⇒ 更贴原版；
            //   下面两句都带 `lb != null` 守卫（A798 起「整块在框外 ⇒ 返回 `null`」）。
            //   ⚠️「这条链没有断言钉着」= **A822** 那条账（宿主 `Editor/MainMenuScene.cs` 不在本件白名单）；
            //   A799 这半边只要求「把『会新裁』记在站点上」。判据全文 → R2 §一① #9 / §4·2。
            var lb = MenuDraw.TextBox(parent, r, text, color, name, fontPx, autoMinPx, Q + qOff, autoMaxPx, autoBasePx);
            // 🔴 **2026-10-08（A213）**：`wrap: false` ⇒ 按原版把模式显式落成 `0`。
            //    ⚠️ 必须在 `TextBox`（里面已跑过 `SetWrapWidth` / `SetAutoFitBox`）**之后**、`AlignLeft` **之前**：
            //    `SetWrapping` 会重排并挪 TMP 子节点（`ForceRelayout`，A205），对齐要落在它之后。
            if (lb != null && !wrap) lb.SetWrapping(false);
            if (lb != null && alignLeft) MenuDraw.AlignLeft(lb, r);
            return lb;
        }

        /// <summary>一个**透明点击区** + `WindowButton`（`PointerLayer` 扫的就是它）。
        /// 🆕 **2026-10-03**：这一路也吃裁切 —— 视口外的命中区**不建**、压在视口边上的**截到视口内**
        /// （判据 = 原版 `RectMask2D` 的射线那一面，见 `MenuDraw.Hit` / `ClipRect`；本页的 `Rect`
        /// 早就带了、**只有这里漏了** ⇒ 补上才是同一套）。
        /// 🔴 **2026-10-14（A753 · A744 的「全删」）**：尾参那个「本页 `Clip`」已**整体删掉** ⇒ 本路与
        /// `Rect` / `Nine` / `Text` / `Cosmetic` 一样走缺省 `clip = null`：由**父链上那颗 `ViewportClip`**
        /// 说了算（`MenuDraw.Hit` 内部自己 `Resolve`，命中那一路要的是「裸框 + `pad`」）。
        /// <para>🔴 **2026-10-16（A822）**：本口与 `Text` **同一族** —— 本文件零 `ClipText`（有意的，理由写在
        /// `Text` 的头里，⛔ 别抄第二份）。命中那半边的裁法是 `R ∩ (V − pad)`（`MenuDraw.Hit` 自己算，
        /// 与文字那半边的 `ClipText` 不是同一条路），而且**它已经有断言**（`Editor/MainMenuScene.cs` 的
        /// `edgeHit` 那条：「跨在视口边上的命中区 quad 也被截到同一条边」）；**文字那半边没有**
        /// （= `A822` 要补的那一条，判据与落点见 `Text` 的头）。</para></summary>
        public Transform Hit(Transform parent, string name, PxRect r, int qOff, System.Action onClick,
                             ImageQuad target = null, string art = null,
                             string hoverArt = null, string pressedArt = null)
        { return MenuDraw.Hit(parent, name, r, Q + qOff, onClick, target, art, hoverArt, pressedArt); }

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
        /// （`PointerLayer.PruneScrolls`）把 `Owner == null` 当**死的**条目删掉（那条注释写着
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

        // 🔴 **2026-10-14（A753 · A744 的「全删」）：本视图那个 `SetClip`（转调宿主页）已整体删掉。**
        //   子视图画的件也一视同仁 —— 走去**父链上那颗 `ViewportClip`** 这一条路（见 `SocialPage` 顶上那段）。
        //   ⚠️ 原来那个 `Page == null` 的告警随方法一起去掉了；本类里没有任何「页没接上」的静默支路了。

        public ImageQuad Rect(Transform parent, string art, PxRect r, string name, int qOff,
                                 Color? tint = null, bool keepAspect = false)
        { return Page.Rect(parent, art, r, name, QOff + qOff, tint, keepAspect); }

        public GameObject Nine(Transform parent, string art, PxRect r, Vector4 border, string name, int qOff,
                                  Color? tint = null, bool fillCenter = true)
        { return Page.Nine(parent, art, r, border, name, QOff + qOff, tint, fillCenter); }

        /// <summary>转调宿主页的 <see cref="SocialPage.Text"/>（队列 = `QOff + qOff`）。
        /// 🆕 **2026-10-08（A213）**：`wrap` 一并转下去 —— `AllianceMemberTab` 那 **9** 处 `v.Text(...)`
        /// 走的就是这条路（真值见 `资料/普查产出_1008/波C3_A212其余_A213_A214.md` §A213 表 B②）；
        /// 同文件另 **4** 处（页签钮 / 奖杯页两行 / 聊天行）是**裸 `Text(...)`**、**不经调用方显式写 `v.`**。
        /// 🔴 **2026-10-12（A323）订正**：原文写那 4 处「走继承来的 **`SocialPage.Text`**、**不经过本函数**」——
        /// **后半句对（确实不走调用点那条 `v.Text(...)`）、前半句错**：`AllianceMemberTab : SocialView`、
        /// 而 **`SocialView : MonoBehaviour`**（与本文件 `:208` 的 `SocialPage : WindowTabBase` **没有继承关系**）
        /// ⇒ 那 4 处解析到的是**本函数（`SocialView.Text`）**、不是 `SocialPage.Text`。
        /// **行为上无差别**（两个口的签名与转调逐字同形），改成正确说法只为「文档 = 事实」。
        /// 🔴 **2026-10-11（A317）**：`autoMinPx` / `wrap` 的缺省值**已与 `SocialPage.Text` 同步删掉**
        /// （同一份判据与理由**只写在那边**，别在这儿再写一遍）。
        /// ✅ **2026-10-12（A323 · 收尾半 · 已落地）**：`alignLeft` 的缺省**也删掉了**（与 `SocialPage.Text`
        /// 同一批、同一天）—— 那 13 处阻塞点（`AlliancesTab.cs` 9 + `FriendsTab.cs` 4）已**逐处现读原版**
        /// （全 `Left`）各补 `alignLeft: true`。逐行清单与判据见 `SocialPage.Text` 的头，**别在这儿抄第二份**。
        /// ⚠️ 全仓**只有** `SocialPage` / `SocialView` / 继承它们的 `AlliancesTab`·`FriendsTab`·`AllianceMemberTab`
        /// 这几处会走到本函数 —— 上一批（A258）已把其中 **21 处**补成显式 `wrap:`。
        /// ⛔ **别在这里再补一个「转调专用」的重载来绕开必填** —— 那是把唯一的口子藏起来。</summary>
        public Label Text(Transform parent, PxRect r, string text, Color color, string name, float fontPx,
                             int qOff, float autoMinPx, bool wrap, bool alignLeft,
                             float autoMaxPx = 0f, float autoBasePx = 0f)
        { return Page.Text(parent, r, text, color, name, fontPx, QOff + qOff, autoMinPx, wrap, alignLeft,
                           autoMaxPx, autoBasePx); }

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
            // 🔴 **2026-10-14（A753）**：尾参原来是 `Page.ClipNow`（那个只读口已随 `Clip` 一起删掉）
            //    ⇒ 走缺省 `clip = null`：由**父链上那颗 `ViewportClip`** 说了算。
            return MenuDraw.Rect(parent, tex, r, name, Page.Q + QOff + qOff, null, keepAspect);
        }

        protected static void Say(string what) { SocialPage.Say(what); }
    }
}
