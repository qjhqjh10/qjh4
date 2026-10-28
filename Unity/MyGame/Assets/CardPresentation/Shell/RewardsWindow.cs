// RewardsWindow.cs — 阶段二第 2 层「日常」：奖励窗口的**宿主 + 左栏页签 + 页签机制**
//
// ============================ 出处（唯一正本） ============================
// `资料/日常_原版规格.md` —— §〇 先决六条 · §一 窗口根 · §二 左栏四个键 · §三 页签页容器。
// 类名 / 字段名 / 枚举值**照原版**：`MainMenuRewardsWindow : BaseParentEventWindow : GameWindowWithTabs : GameWindow`
// + 根上的 `TabbedWindowComponents`(tabButtons/tabHolder/tabPrefab) + `TabButtons` 三件套 —— **没有一个叫
// `TabbedWindow` 的脚本**（正本 §〇·6）。
//
// 🔴 **三条纪律**：
//   ① **窗口根 = 满屏 1920×1080**（实证：主菜单三个 Holder 全是 `a=(0,0)-(1,1) pos=0 sz=0` 满屏拉伸）
//      ⇒ `Content Area` = **x 167.17..1920.01 · y 70.94..1080.00**（1752.83×1009.06）。
//      🔴 **这个数我手推错过两次**（把 `anchoredPosition.y` 的符号搞反），最后靠
//      `工具/menu_rect.py`（机械走链、独立实现）定案 —— **这类问题一律跑脚本，别手推**。见正本 §一。
//   ② **选了哪一页不是靠两套资产**：四键的 Highlight 出厂 `m_Enabled=1 / a=1`，`m_IsOn` 全 0，
//      可见性由运行时 `TabButtons` 驱动（正本 §二·3）。我们同样**只画选中的那一个**。
//   ③ **第 5 项（Booster Packs）不是页签** —— 它是 `tabButtonPrefab` 的**母版**，不在 `tabs` 里
//      （正本 §二·3）。🔴 **2026-10-16（A815 · 用户 2026-10-15 拍板）**：它原来排在第 **4** 项、
//      占的是左栏第 4 格；这一件**把第 4 格改成我们自建的「每日连胜」页**，母版顺位挪到**第 5 项**
//      （图标 / 文案 / 字号 / 红点偏置**逐值未动**，运行时照旧被 `TabButtons.Initialize` 关掉 ⇒ 屏幕上看不见）。
//      ⚠️ **原版侧**：奖励窗的 `tabs` 只有 Missions/Forge/Campaign 三条，**没有**第 4 个页签
//      （`bundle_menus_assets_all/MonoBehaviour_1349677669050291967.json`）；「每日连胜」在原版是
//      **另一扇全屏事件窗**（`DailyStreakWindow : LiveOpsEventWindow<MainMenuMission>`），由
//      `BaseParentEventWindow.SetupTabs` 在运行期把 live-ops 事件做成页签。**本地没有 live-ops 数据**
//      ⇒ 这个页签是**我们自建的**（用户拍板），只显示**本地累计的连续登录天数**，不做奖励、不接 LiveOps。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    // ============================================================ 页签机制
    // ⚠️ **2026-10-13（A1506）**：本条罩着的三个类已各拆到**同名文件** —— `WindowTabBase.cs` · `TabButtons.cs` ·
    //    `GameWindowWithTabs.cs`（本条下方仍留着 `WindowTabType` 枚举）。

    /// <summary>页签类型。**值照原版**（`TabButtons.options` 的三组 tab↔toggle + 第 4 键的导航语义）。</summary>
    public enum WindowTabType
    {
        None = 0,
        /// <summary>任务页。原版 = `MissionsTab`，`tabs[0]`，左栏第 1 键。</summary>
        Missions = 1,
        /// <summary>战役页。原版 = `CampaignWindowTab`，`tabs[2]`，左栏**第 2 键**（视觉顺序与 `tabs` 顺序不同！）。</summary>
        Campaign = 2,
        /// <summary>锻造厂页。原版 = `ForgeWindowTab`，`tabs[1]`，左栏**第 3 键**。第 3 层。</summary>
        Forge = 3,
        /// <summary>🆕 **每日连胜页（A815）** —— 左栏**第 4 格**。
        /// <para>🔴 **自建 · 用户 2026-10-15 拍板**：原版奖励窗**没有**这一页签（它的 `tabs` 只有
        /// Missions/Forge/Campaign），「每日连胜」在原版是**另一扇全屏事件窗**
        /// （`DailyStreakWindow : LiveOpsEventWindow&lt;MainMenuMission&gt;`，由 `BaseParentEventWindow.SetupTabs`
        /// 在运行期按 live-ops 事件做成页签 —— 判据 → `资料/普查产出_1015/R5_A815_A817_A819判据.md` §一）。
        /// **本地没有 live-ops 数据** ⇒ 照用户拍板自建一个同名页签，**只显示本地累计的连续登录天数**
        /// （不做奖励、不接 LiveOps、不连服务器）。</para>
        /// <para>📌 值取 **4**：紧跟上面那一组（1/2/3），与商店（10 起）/ 收藏（20 起）/ 档案（30 起）/ 社交（40 起）/
        /// 聊天（50 起）那几组**隔开**。</para></summary>
        DailyStreak = 4,
        /// <summary>🆕 商店的三个页（原版 `ShopTab` 的 `CardShopTab` / `DailyShopTab` / `ItemShopTab`）。
        /// **它不在这个枚举的「奖励窗」那一组里** —— 商店是**另一个窗口**（`Shop Menu Variant`），
        /// 只是共用 `TabButtons` 这套机制（原版也是同一个 `TabButtons` 类，字段 `tabButtons/tabPrefab/tabHolder`）。
        /// 值从 **10** 起，与上面那组隔开。</summary>
        ShopCards = 10, ShopDaily = 11, ShopItems = 12,
        /// <summary>🆕 收藏窗的四个页（原版 `CollectionScreen` 的 `SelectDecksTab` / `CardCollectionTab` /
        /// `CardbackCollectionTab` / `AlternateArtCardCollectionTab`）。**又是另一个窗口**
        /// （`Collection Menu Variant`，与商店/奖励窗共用同一套 `TabButtons` 机制）。
        /// 🔴 **页签的卡面文案是 `Decks/Cards/Cosmetics/Styles`**（不是页节点名，见正本 §二）。
        /// 值从 **20** 起，与上面两组隔开。</summary>
        CollectionDecks = 20, CollectionCards = 21, CollectionCosmetics = 22, CollectionStyles = 23,
        /// <summary>🆕 玩家档案窗的六个页（原版 `PlayerProfileMenu.tabs[]`：`ProfileTab` / `AvatarTab` /
        /// `TitleTab` / `BattleLogTab` / **`AchievementsMenu`** / `RankedTab`）。**又是另一个窗口**
        /// （`Player Profile Window`，与上面几扇一样共用 `TabButtons` 这套机制）。
        /// 🔴 **顺序 = 原版 `tabs[]` 的顺序**，也是左栏六个键的视觉顺序（正本 §2·1：Profile→Avatar→Title→
        /// Battle Log→Trophies→Ranking）。⚠️ 第 5 个键**名叫 `Trophies`、文案是 `Achievements`**、
        /// 第 6 个键**名叫 `Ranked`、文案是 `Ranking`**。
        /// 值从 **30** 起，与上面三组隔开。</summary>
        ProfileInfo = 30, ProfileAvatar = 31, ProfileTitle = 32,
        ProfileBattleLog = 33, ProfileTrophies = 34, ProfileRanking = 35,
        /// <summary>🆕 社交窗的两个页（原版 `SocialMenuWindow.tabs[]`：`AlliancesTab` / `FriendsTab`）。
        /// **又是另一个窗口**（`Social Submenu Variant`，与上面几扇一样共用 `TabButtons` 这套机制）。
        /// 🔴 **顺序 = 原版 `tabs[]` 的顺序**，也是左栏两个键的视觉顺序（`Alliances` → `Friends`）。
        /// ⚠️ **默认落在 `Alliances`**（`Alliances Tab` act T / `Friends Tab` act **F**，实测 `m_IsActive`）。
        /// 值从 **40** 起，与上面四组隔开。</summary>
        SocialAlliances = 40, SocialFriends = 41,
        /// <summary>🆕 聊天窗的两个频道页（原版 `ChatRoom { Global = 0, Alliance = 1 }`，见
        /// `资料/普查产出_0927/聊天窗与挑战弹窗.md` §A·2 与 §D·4）。
        /// 🔴 **原版的页签是运行期按服务器的订阅列表建的**（`TabButtons.AddTabButton`）——
        /// 本地一个都订阅不到 ⇒ **按枚举把两个频道都建出来是我们的选择**（否则整扇窗是空的）。
        /// 值从 **50** 起。</summary>
        ChatGlobal = 50, ChatAlliance = 51,
        /// <summary>🆕 **VIP 商店页（第三批 · 2026-10-27）** —— 商店窗的第 4 个页签。值从 **13** 起
        /// （紧跟 `ShopCards/Daily/Items` 那一组 10/11/12，不占别组的号段）。
        /// ⚠️ 本行只是**枚举门槛**：VIP 页本体归商店线（`Shell/Shop*`）实现，⛔ 别在本文件里加页内容。🆕 **2026-10-28（`B91_W5`）本行同时承载 `ShopCrates = 14`** = 商店**第 5 页 Boosters/Crates**（原版 `BoosterShopTab : ShopTab`，`ShopTabType.CratesShop = 0`）—— 同样只是枚举门槛，页本体在 `Shell/Shop*`（`ShopData.Pages[4]`）。</summary>
        ShopVip = 13, ShopCrates = 14,
    }

    // ============================================================ 奖励窗口本体

    /// <summary>
    /// `Rewards Base Submenu Variant` —— 主菜单左竖导航第 4 钮（REWARDS）开的那个窗。
    /// **`type=Fullscreen` · `placement=Canvas(5)` · `closeOnESC=false` · `extraScaleSmallScreen=1.0`**（正本 §一）。
    /// </summary>
    public class RewardsWindow : MainMenuSubmenuWindow
    {
        // ---- 🔴 外壳常量（`Content Area` / `Tab Buttons` / `Tabs` 的实测矩形 · 左栏底图 · 双色渐变 ·
        //      绘图助手 `Node`/`Rect`/`Text`/`Art`… ）**2026-09-23 已收口到 `MainMenuSubmenuWindow`** ——
        //      见 `Shell/MenuWindowBase.cs`。商店那件要写同一个壳，两处写会迟早不一致。
        //      常量名不变（继承过来的静态成员照样能写成 `RewardsWindow.ContentL` 等），**行为一字未改**。

        /// <summary>键表（**视觉顺序**）：图标名 · 文案 · 原版字号(px) · autosize 区间 · 实例 ID · 红点偏置。
        /// 🔴 **最后一项必须是母版**（`MainMenuSubmenuWindow.BuildBar` 的约定：`specs` 最后一项 = `tabButtonPrefab` 并关掉）
        /// ⇒ **可见的键 = 本表项数 − 1**。</summary>
        public static readonly TabBtnSpec[] Buttons =
        {
            // 图标出处：Campaign 用的是**主菜单那张导航图** —— `40K_rewards_bt_campaign` 不存在（正本 §〇·3）
            new TabBtnSpec("40K_rewards_bt_missions",   "Missions",       36.0f,  5f, 36f, "RewardsMenu_MissionsButton",
                           term: "Rewards Menu/Missions"),
            new TabBtnSpec("40k_main_bt_campaign",      "Campaign",       36.0f,  5f, 36f, "RewardsMenu_CampaignButton",
                           term: "MainMenu/General/Campaign"),
            new TabBtnSpec("40K_rewards_bt_forge",      "Forge",          36.0f,  5f, 36f, "RewardsMenu_ForgeButton",
                           term: "MainMenu/General/Forge"),
            // 🆕 **第 4 项 = 自建的「每日连胜」页**（A815 · **用户 2026-10-15 拍板**）。
            //   🔴 **原版这一格是母版按钮、不是页签**（见本表最后一项与文件头纪律③）⇒ 这一项**整个是我们加进来的**：
            //     · **图标 `40k_main_bt_rewards`（126×126）= 我们挑的**。原版「每日连胜」那枚
            //       `40k_main_gamemode_DailyStreak 1x1` 在 **live-ops 图包**里（`bundle_liveopsmenuimages_assets_all`），
            //       是 **1024²** 独立 PNG、与 126² 的键图标**不是一族**，而且**没进我们的 `Resources/`**
            //       （`工具/import_original_art.py` 的 `MENU_IMAGES` 里没有它）⇒ 取现有键图标里语义最近的一枚
            //       `40k_main_bt_rewards` —— 同一张图在本类里**已经是连登奖励格的图标**
            //       （`DailyData._streakIcon` 的 「1 Booster Pack」/「2 Booster Packs」两格）。
            //     · **文案 `Daily Streak`（键上渲成 `DAILY STREAK`）= 我们挑的**（原版那一条来自远端事件资产的标题；
            //       用户实拍那页的大标题就是「每日连胜」⇒ 取它的英文 `Daily Streak`）。
            //     · **字号 / autosize 区间 / 红点偏置照【母版】那一套**（25.65 · auto 12~33 · +47.9）——
            //       判据：原版的**事件页签就是母版克隆出来的**（`BaseParentEventWindow.SetupTabs` →
            //       `TabButtons.AddTabButton`），克隆件继承的正是母版这几个值 ⇒ 我们这一格与母版同格、照它取。
            //       ⚠️ 这一条是**推断**（本地没有 live-ops 资产可读它的运行时值），如实标、⛔ 不写成「照原版」。
            //     · **实例 ID 空**：原版只有那三颗真页签有实例 ID（正本 §二·2），母版没有。
            new TabBtnSpec("40k_main_bt_rewards",       "Daily Streak",   25.65f, 12f, 33f, "", 47.9f,
                           term: "Rewards Menu/DailyStreak/Title"),
            // 🔴 **第 5 项 = 原版的母版按钮**（`Menu Navigation Panel Button`；`TabButtons.tabButtonPrefab`）。
            //   **它不是页签**，出场即被 `TabButtons.Initialize` 关掉 ⇒ 屏幕上没有这一格。
            //   ⚠️ **2026-10-16（A815）就地改口径（铁律 5）**：这一项**原来排在第 4 项**（= 左栏第 4 格），
            //      A815 把第 4 格让给自建的「每日连胜」页之后，它顺位到**第 5 项** —— **本行六个值逐值未动**：
            //      图标 `40K_shop_bt_boosters` · 文案 `Booster Packs` · 字号 **25.65**（autosize 12→33 缩出来的）·
            //      红点偏置 **+47.9**（其余三键 −27.2）—— 全是实测值，原来写死在循环里
            //      `idx == 3 ? 47.9f : −27.2f` 的那条特例已抽进规格。
            //   ⚠️ 它现在建出来的 y 格 = `70.94 + 120 + 180×4 = 910.94`（**底边 1090.94 比栏底 1080 低 10.94px**）——
            //      ⛔ **屏幕上看不见**（运行期关着）；如实记在这里，别当没这回事（没有断言量「母版在栏内」）。
            new TabBtnSpec("40K_shop_bt_boosters",      "Booster Packs",  25.65f, 12f, 33f, "", 47.9f),
        };

        ImageQuad[] _btnHighlight = new ImageQuad[5];
        /// <summary>🆕 2026-10-03：高亮层要**整棵**开关（九宫格）⇒ 留一份 `BarResult`。</summary>
        MainMenuSubmenuWindow.BarResult _btnRes;
        /// <summary>各键的红点（⏭ 显隐靠 **alpha**，见 `RefreshBadges`）。长度 = `Buttons.Length`（含母版一格）。</summary>
        ImageQuad[] _btnBadge = new ImageQuad[5];
        Transform[] _btnRoot = new Transform[5];

        // ---------------------------------------------------------- 建

        public static RewardsWindow Create(WindowsManager mgr)
        {
            // 🔴 **2026-10-11（A218）**：窗口根是 `RectTransform` ＋ 写 `sizeDelta`。
            //    判据 = 原版同名 prefab 实读：`Rewards Base Submenu Variant` 的 `RectTransform`
            //    `anchor (0,0)-(1,1)` · `sizeDelta (0,0)` · pivot (0.5,0.5) · **绝对矩形 (0,0)-(1920,1080)**
            //    （`bundle_menus_assets_all`，2026-10-11 现读）⇒ 就是**整屏矩形**。
            //    ⚠️ 原版靠 stretch 拿父（Canvas）的尺寸，本工程没有 uGUI 父矩形 ⇒ 用
            //    「重合锚点 + 屏尺寸」表达**同一个矩形**（锚点不复刻，见 `MenuDraw.SetPxSize` 那段）。
            //    改坏法：删掉 `SetPxSize` 那句 ⇒ `Editor/RewardsScene.cs` §A218「奖励窗根 = 整屏矩形」红。
            var go = new GameObject("Rewards Base Submenu Variant", typeof(RectTransform));
            MenuDraw.SetPxSize(go.transform, LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);
            var win = go.AddComponent<RewardsWindow>();
            win.type = WindowType.Fullscreen;                 // 实证 type=0
            win.placement = WindowsPlacement.Canvas;          // 实证 windowsPlacement=5
            win.closeOnEsc = false;                           // 实证 closeOnESC=0
            win.extraScaleSmallScreen = 1f;                   // 实证 1.0
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open()
        {
            Build();
            // 原版 `MainMenuRewardsWindow.Open` 会回点导航钮：`GetToggle(3).SetIsOnWithoutNotify(true)`
            //（`DF:MainMenuRewardsWindow__Open.c:14-16`）。我们让主菜单侧自己处理，这里只报一句。
            BuildTabContents();
            // 🆕 2026-10-25：页对象已建出来 ⇒ 走原版 `GameWindowWithTabs.Open` 的 lazy `SetupTabs` 那一跳
            //   （缓存 tabs + `ForceCloseTabs`；只跑一次，见基类 `EnsureTabsSetup` 的注释）。
            EnsureTabsSetup();
            // 新实例默认落在 **Missions**（= 左栏第 1 键）。走 `Click` 这条路而不是直接改字段 ——
            // 那三个属性是只读的，而且「点一下」才是唯一的状态入口（两处写同一件事 = 迟早不一致）。
            // 🔴 **2026-10-19（`N117` 顺手 · 与 `CollectionWindow` 同一条判据）**：这里原来**恒 `Click(0)`**
            //   ⇒ 每次 `Open()` 都回第 0 页（`Hide` 之后被 `ShowPreviousWindow` 带回来也一样）。
            //   判据（原版 `GameWindowWithTabs__Open.c:9-21`）：**当前页签**（字段 `0x12`）非空就只重开那一页；
            //   只有它是**空**（= 新建实例）才走 `GetStartingTab()`（虚表 `0x218`，回落 = `tabs` 的
            //   `FirstOrDefault` = 第一页）。而 `WindowTabBase__CloseTab.c` 只 `SetActive(false)`、
            //   `GameWindowWithTabs__Close` 也不写 `0x12` ⇒ **隐藏 → 带回来这一路原版保留当前页**。
            //   ⚠️ 为什么必须是 `Click(visualIndex)` 而不是 `ChangeTab(CurrentTab)`：左栏高亮读的是
            //   `TabButtons.CurrentVisualIndex`（`RefreshHighlights`），只调 `ChangeTab` 会让高亮停在第 0 键上。
            int startVisual = StartVisualIndex();
            if (tabButtons != null) tabButtons.Click(startVisual);
            RefreshHighlights();
            RefreshBadges();
        }

        /// <summary>🔴 **2026-10-19（`N117` 顺手）**：`Open()` 该落在左栏第几颗 —— **与
        /// `CollectionWindow.StartVisualIndex` 同一条规则**（文件所有权所限没能收口到 `GameWindowWithTabs`，
        /// 两处逐值相同；⚠️ 改一处必须同步另一处）。
        /// <para>原版 `GameWindowWithTabs.Open`（`decomp_full/GameWindowWithTabs__Open.c:9-21`）：
        /// 当前页签（字段 `0x12`）**非空 ⇒ 重开它**；**空 ⇒ `GetStartingTab()`**（虚表 `0x218`，
        /// 回落 = `tabs` 的 `FirstOrDefault` = 第一页）。`Hide()` / `WindowTabBase.CloseTab()` 都**不清** `0x12`
        /// ⇒ 「藏起来 → 被 `ShowPreviousWindow` 带回来」这一路**保留当前页**。</para>
        /// <para>⚠️ 例外面（如实记）：若 `CurrentTab` 不在本窗的 `visualTypes` 里（绝不该发生），回落第 0 页。</para></summary>
        int StartVisualIndex()
        {
            if (CurrentTab == WindowTabType.None) return 0;
            int keep = visualTypes != null ? visualTypes.IndexOf(CurrentTab) : -1;
            return keep >= 0 ? keep : 0;
        }

        /// <summary>自检读口（静态断言用：⛔ 不为此真建一遍整窗）—— 见 <see cref="StartVisualIndex"/>。</summary>
        public int StartVisualIndexForTest { get { return StartVisualIndex(); } }

        /// <summary>建整个窗口（**自检与运行时同一条路**）。
        /// 🔴 **2026-09-23：外壳（Content Area + 渐变 Background + 左栏 + Tabs）改成调基类的 `BuildShell`**
        /// —— 商店那件要写同一个壳，两处写会迟早不一致。**几何与层次一字未改**（265 条断言守着）。</summary>
        public void Build()
        {
            // 🔴 **2026-10-11（`A1498`）显式表态**：原版 `Rewards Base Submenu Variant` 的
            //   `tabButtons.tabButtonPrefab` = MB `638825230631048959` 的 `m_PathID`（**非 0**）⇒ **末项【是】母版**。
            var res = BuildShell(transform, Buttons, "RewardsTabButton_", "Tabs", lastIsMaster: true);
            tabButtons = res.buttons;
            _btnRoot = res.roots;
            _btnHighlight = res.highlight;
            _btnRes = res;
            _btnBadge = res.badge;

            // ---- `Content Area/Shadow (1)`：**已由 `BuildShell` 建出来并 `SetActive(false)`**（2026-10-23 · A9）----
            //   🔴 **就地订正（铁律 5）**：这一行原来写「出厂 active=false ⇒ **不建**（照 `MainMenuRuntime` 纪律③）」——
            //   那条纪律已被推翻（见 `Shell/MainMenuRuntime.cs` 文件头 ③ 的 2026-10-11 订正 · `Shell/TopBar.cs:52`），
            //   四种窗共用同一个 `BuildShell` ⇒ 这一件在那边建一次、四扇都有（原版实测四扇同矩形同色，判据 → `MainMenuSubmenuWindow.Shadow1R`）。
            //   ⛔ 别在这里再建第二份。

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Rewards] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
        }

        /// <summary>
        /// 重算四个键的红点。原版由 **`UiBadgeNotificationManager.Refresh()`** 在通知变化时推
        /// （`Missions.CheckNotification` → `INotificationProvider&lt;MissionsBadge>`）—— **是事件驱动的**。
        /// ⚠️ **我们还没有通知总线** ⇒ 只在 `Open()`（和自检）里各调一次；
        /// 真接了通知源之后应该改成订阅（⏭ 记在 `资料/日常_画面逐项对_0923.md` 的 D3）。
        /// 🔴 **判据只此一份 = 每颗红点自己的 `UiBadgeNotification` 计重**（`Recount()`）；
        ///    ⛔ 本函数**不再**直写 quad 的材质色（2026-10-29 `B94_Z1`：旧写法有两条后果，见方法体里的说明）。
        /// </summary>
        public void RefreshBadges()
        {
            // 🔴 2026-10-29（B94_T1 §4·1 / B94_Z1 活 1）：**不再直写 quad 的材质色** —— 红点显隐的唯一写手是
            //   `UiBadgeNotification`（原版 `Awake` 的 `canvasGroup.alpha = 0` 那一层，只有组件自己写）。
            //   旧写法有两个后果（判据链见 `资料/完全复刻/报告/B94_T1.md` §5）：
            //   ① `idx==0 && RewardsHasBadge` 把 **Missions** 那颗**由一条布尔**点着 —— 它绕开通知系统，
            //      而组件补间一停（`_fading == false`）就不再写 ⇒ 那个 1 **永久留屏**（= 用户看到的「页签红点常亮」）；
            //   ② 同一个循环把 idx 1..3 **强写 0** ⇒ 反过来把 Campaign / Forge 的**真红点压灭**。
            //   ⇒ 现在只做「让四颗各按自己的计重重算一次」，⛔ 别在这里写任何颜色。
            for (int i = 0; i < _btnBadge.Length; i++)
            {
                if (_btnBadge[i] == null) continue;
                var comp = _btnBadge[i].GetComponent<UiBadgeNotification>();
                if (comp == null) continue;      // 原版这一键没有红点定义 ⇒ 没有组件 ⇒ alpha 恒 0（⛔ 不要在这里补写）
                comp.Recount();
                if (!Application.isPlaying) comp.SnapToTargetForTest();   // 批处理没有帧循环
            }
        }

        /// <summary>选中态：**只画选中的那一个**（原生四键出厂都亮，可见性由运行时驱动 —— 文件头纪律②）。
        /// 🔴 2026-09-23：加了 `override` —— 基类 `ChangeTab` 现在会调这个钩子（收藏窗当初就是漏了它），
        ///    不加 `override` 的话基类调的是**空实现**，本窗的高亮就不会跟着切页走了。</summary>
        public override void RefreshHighlights()
        {
            int sel = tabButtons != null ? tabButtons.CurrentVisualIndex : -1;
            // 🆕 2026-10-03：**整棵九宫格一起开关**（`Highlight` 现在是 `Sliced`，一棵树 9 个 quad）
            MainMenuSubmenuWindow.SetHighlight(_btnRes, sel);
        }

        // ---------------------------------------------------------- 页

        /// <summary>建四页。**Missions 是本层的活**；Forge/Campaign 是第 3 层 —— 先建**空页**并出声。
        /// 🆕 **2026-10-16（A815）**：第 4 页 = **自建的「每日连胜」**（用户 2026-10-15 拍板；
        /// 原版这一格是母版按钮、没有这一页，见 `WindowTabType.DailyStreak` 的注释）。</summary>
        void BuildTabContents()
        {
            tabs.Clear();
            var missions = Node(tabHolder, "Missions Tab", new PxRect(TabL, TabT, TabR, TabB));
            var mt = missions.gameObject.AddComponent<MissionsTab>();
            mt.SetHost(this, missions);
            tabs.Add(mt);

            // 页面矩形照原版：`Forge Tab` N(3, 0,0, 1,1, .5,.5, 81.76,0, -163.5,-0.359) ·
            //                 `Campaign Tab` 同锚点、`sz=(-163.5,0)`
            // 🔴 **2026-09-23 第 3 层**：`Forge Tab`（锻造厂）与 `Campaign Tab`（战役）都换成**真页**。
            var forgeR = TabRectFor(0f, -0.359f);
            var forge = Node(tabHolder, "Forge Tab", forgeR);
            var ft = forge.gameObject.AddComponent<ForgeTab>();
            ft.SetHost(this, forge);
            tabs.Add(ft);

            var campR = TabRectFor(0f, 0f);
            var camp = Node(tabHolder, "Campaign Tab", campR);
            var ct = camp.gameObject.AddComponent<CampaignTab>();
            ct.SetHost(this, camp);
            tabs.Add(ct);

            // 🆕 **`Daily Streak Tab`（自建 · A815）**：矩形取 `TabRectFor(0f, 0f)` —— 与 `Campaign Tab` 同一格。
            //    ⚠️ **原版没有这一页**（没有可照的矩形）⇒ 这一格是**我们挑的**（取同族页里最齐整的那一份：
            //    `Campaign Tab` 那套 `pos=(81.758,0) sz=(-163.517,0)`）。节点的名字也照同族取 `… Tab`。
            var dsR = TabRectFor(0f, 0f);
            var ds = Node(tabHolder, "Daily Streak Tab", dsR);
            var dt = ds.gameObject.AddComponent<DailyStreakTab>();
            dt.SetHost(this, ds);
            tabs.Add(dt);

            foreach (var t in tabs) t.Setup();
        }

        /// <summary>`Tabs` 下面一页的矩形。原版这两页的锚点相同（`a=(0,0)-(1,1) p=(.5,.5)`、
        /// `pos=(81.758,0)`），只有 `sizeDelta` 的 y 差一点（Forge −0.359 / Campaign 0）。</summary>
        static PxRect TabRectFor(float szY, float extraSzY)
        {
            var tabsRect = new PxRect(ContentL, ContentT, ContentR, ContentB);
            return UguiRect.Child(tabsRect, UguiRect.A00, UguiRect.A11, UguiRect.P50c,
                                  new Vector2(81.758f, szY), new Vector2(-163.517f, extraSzY));
        }

        public string Dump()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"Rewards：页签 {CurrentTab} · 开着的窗页 {tabs.Count} 个");
            sb.Append($" · 左栏 {_btnRoot.Length} 键（可见高亮 {CountVisibleHighlights()}）");
            sb.Append($" · 取不到的图 {MissingArt.Count} 张");
            return sb.ToString();
        }

        int CountVisibleHighlights()
        {
            int n = 0;
            foreach (var q in _btnHighlight)
                if (q != null && q.gameObject.activeInHierarchy) n++;
            return n;
        }
    }

}
