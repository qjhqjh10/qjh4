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
// ③ **`Content Area` 的第 4 个孩子 `Shadow (1)` 出厂 act=F** ⇒ ✅ **照建、但出厂 `SetActive(false)`**
//    （基类 `BuildShell` 统一建，2026-10-23 · A9）。
//    🔴 **就地订正（铁律 5）**：这一条原来写「⇒ **不建**（照 `MainMenuRuntime` 那条纪律③）」——
//    那条纪律已被推翻（`Shell/MainMenuRuntime.cs` 文件头 ③ 的 2026-10-11 订正 · `Shell/TopBar.cs:52`），
//    同族先例 = `Shell/ParentEventWindow.cs:668-671` + `Editor/MainMenuScene.cs:16364` 的「建了、且关着」。
//    ✅ **本扇也现读到了这一件**（`工具/menu_rect.py bundle_menus_assets_all "Social Submenu Variant" --depth 3`
//    ⇒ `Shadow (1)` 330.37,70.94→379.81,1080.00 · `menu_dump` 那行 `INACT` · 色 `(0,0,0,0.475)` · sprite `40k_main_tab_shadow`）。
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
        //    🔴 **2026-10-20 再搬到 4077–4079**（`Core/Tooltip.cs` 的 `QTipShadow/QTip/QTipText`；判据换成原版**排序层** `Blocking`(8) > `PopUps`(7) —— 旧「兄弟序」理由跨排序层、不成立；详见 `资料/普查产出_1020/W58_界面线_层序修正.md`）。
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
            // 🆕 2026-10-25：页对象已建出来 ⇒ 原版 lazy `SetupTabs` 那一跳（只跑一次）
            EnsureTabsSetup();
            // 默认落在 **Alliances**（`Alliances Tab` act T / `Friends Tab` act **F**，实测 `m_IsActive`）——
            // 走 `Click(0)` 而不是直接改字段：「点一下」才是状态的唯一入口（同奖励窗那条注释）。
            if (tabButtons != null) tabButtons.Click(0);
            RefreshHighlights();
            RefreshBadges();
            // 🔴 **2026-10-17（`A1641`）就地订正（铁律 5）**：本行原来写「原版 `updateNavPanel = 1` ⇒
            //   **开窗时**把左竖导航的 SOCIAL 键点亮」—— **那是错口径**。实读：`updateNavPanel`
            //   （`GameWindow` 偏移 `0x3A`）全库**只有两个读点、都在 ESC 那一支**
            //   （`GameWindow__ESCPressed` / `SearchingMatchWindowDemo__ESCPressed`），
            //   **没有任何「开窗时读它」的地方**；而 SOCIAL 那颗亮起来来自**导航钮自己的 `ToggleGroup` 单选**
            //   （全库**没有** `SetToggle(nav, 4)` 调用点）。⇒ 这里**只需要出声**，
            //   ⛔ 别照旧注释去补「开窗点亮」（那是我们编的口径）。
            Debug.Log("[Social] 社交窗已开（默认页 = Alliances）。原版**没有**「开窗时回点左竖导航」这一跳"
                    + "（`updateNavPanel` 只在 ESC 那一支被读；SOCIAL 亮起来自导航钮自己的 `ToggleGroup` 单选）。");
        }

        /// <summary>建整个窗口（**自检与运行时同一条路**）。外壳走基类 `BuildShell`（与奖励窗/商店同一份）。</summary>
        public void Build()
        {
            // 🔴 **2026-10-18（第七轮）**：传 `LocalizedButtons()`（建窗时把两颗页签的**文案**换成词条），
            //   ⛔ **不是**静态那份 `Buttons`（静态初始化早于任何语言设置，见 `LocalizedButtons` 的 doc）。
            //   节点名（`SocialTabButton_{i}`）与图标/字号/角标那些字段**一个字没变**。
// 🔴 **2026-10-11（`A1498` 同根因 · 第二扇窗）**：补 `lastIsMaster: false`。
//   判据（原版 prefab 现读 `bundle_menus_assets_all` 该窗的 `TabbedWindowComponents.tabButtons`）：
//   **它的 `tabButtonPrefab = {m_FileID:0, m_PathID:0}`（null）** ⇒ 左栏末项**不是母版**，
//   而基类原来无条件把末项当母版 `SetActive(false)` ⇒ **`Friends` 那颗根本不画**（屏上只剩 `Alliances`）。
//   ✅ 正例对照：`Rewards Base Submenu Variant`(TabButtons MB `638825230631048959`) 与
//      `Shop Menu Variant`(MB `764348890710493308`) 的 `tabButtonPrefab` **都非 0** ⇒ 那两扇窗必须走缺省。
//   ⚠️ 症状**静默**：末项照建、只是被关掉，而断言走 `FindChild`（含 inactive）照样找得到它 ——
//      所以本行下面对应的那条 `activeSelf` 断言是**成对**的，⛔ 别只留一条。
            // 视觉顺序 = 原版 `Tab Buttons` 下两个孩子的顺序（Alliances → Friends），也是 `tabs[]` 的顺序。
            // 🔴 **2026-10-17（`A1511(a)`）就地改序（铁律 5）**：这两句原来排在 `BuildShell` **之后** ——
            //   而 `BuildBar` 建每一颗键时就读 `visualTypes[i]` 写进 `TabButtons.options[i].type`
            //   （`Shell/MainMenuSubmenuWindow.cs` 的 `BuildShell`）⇒ 事后替换**不生效**。⇒ **必须排在 `BuildShell` 之前**。
            visualTypes.Clear();
            visualTypes.Add(WindowTabType.SocialAlliances);
            visualTypes.Add(WindowTabType.SocialFriends);

            var res = BuildShell(transform, LocalizedButtons(), "SocialTabButton_", "Tabs", lastIsMaster: false);
            tabButtons = res.buttons;
            _btnRoot = res.roots;
            _btnHighlight = res.highlight;
            _btnRes = res;
            _btnBadge = res.badge;

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

}
