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
    }

    /// <summary>一页。原版叫 `WindowTabBase`（`MissionsTab : WindowTabBase&lt;MainMenuRewardsWindow>`）。</summary>
    public abstract class WindowTabBase : MonoBehaviour
    {
        public abstract WindowTabType Type { get; }
        /// <summary>建内容。**换页时不重建**（原版是 `SetActive` 切换）。</summary>
        public virtual void Setup() { }
        /// <summary>每次切到本页时调（原版 `OnOpen`）。</summary>
        public virtual void OnOpen() { }
    }

    /// <summary>
    /// 左栏页签容器。原版挂在 `Tab Buttons` 节点上，持一张 `options`（tab↔toggle 对照表）。
    /// ⚠️ 原版 `TabButtons.options` 的**顺序是 `tabs` 的顺序**（Missions/Forge/Campaign），
    /// **不是左栏的视觉顺序**（Missions/Campaign/Forge）—— 两者不同，正本 §二·3。
    /// </summary>
    public class TabButtons : MonoBehaviour
    {
        [System.Serializable]
        public class Option
        {
            public WindowTabType type;
            public WindowButton button;      // 原版是 EverguildToggle；我们只用它的点击
        }

        public List<Option> options = new List<Option>();
        public GameWindowWithTabs window;

        /// <summary>🔴 **原版字段名**。它不是「第 4 个页签」，是**运行期新增页签的克隆母版**
        /// （`GameWindowWithTabs.CreateTabButton` → `TabButtons.AddTabButton`，只被活动页/商店页/聊天页用）。
        /// 出场时是激活的，`Initialize` 第一件事就是把它关掉 ⇒ **左栏运行期只有页签数那么多键**。
        /// <para>⚠️ **2026-10-16（A815）口径**：我们这一份的母版 = 键表**最后一项**（`Buttons[4]`，
        /// 原版那枚 `Menu Navigation Panel Button`：图标 `40K_shop_bt_boosters` / 文案 `Booster Packs` /
        /// 25.65 / auto[12~33] / 红点偏置 +47.9，**逐值未动**）。它**原来是第 4 项**，A815 把第 4 格让给
        /// 我们自建的「每日连胜」页之后顺位到第 5 项 ⇒ 运行期可见的键是 **4** 个（母版照旧关着）。</para></summary>
        public GameObject tabButtonPrefab;

        /// <summary>原版 `TabButtons.Initialize` 里我们真正需要的那两件（其余是 toggle 事件接线，我们没有 toggle）。
        /// 🔴 **第一件就是关掉母版** —— 出处 `d:/2/tools/decomp_full/TabButtons__Initialize.c:35-44`（亲读指令流复核）：
        /// `op_Inequality(tabButtonPrefab, null)` → `Component__get_gameObject` → `GameObject__SetActive(go, 0)`。
        /// ⚠️ **2026-09-23 修（铁律 5·b）**：原来我们把第 4 键当**常显**的画出来了 —— 与实况不符。</summary>
        public void Initialize(GameWindowWithTabs w)
        {
            window = w;
            if (tabButtonPrefab != null) tabButtonPrefab.SetActive(false);   // 照原版：母版隐藏
        }

        public WindowTabType CurrentType { get; private set; } = WindowTabType.None;
        public int CurrentVisualIndex { get; private set; } = -1;

        /// <summary>点左栏某一键（`visualIndex` = 视觉顺序 0..键数-1）。**母版那一格不是页签**。
        /// <para>🔴 **2026-10-16（A815）口径**：奖励窗可见的键是 **4** 个 —— 第 4 格 = 我们自建的
        /// `DailyStreak` 页（用户 2026-10-15 拍板），第 5 格 = 原版母版（`None`，照旧关着、点不着）。</para></summary>
        public void Click(int visualIndex)
        {
            if (window == null) return;
            // 🔴 判据是「**这个位置挂的是不是页签**」，不是「下标越没越界」——
            //    母版那一格在 `visualTypes` 里是 `None`（它不在 `tabs` 里，正本 §二·3）。
            //    第一版按下标判，`Click(母版那一格)` 会走进正常分支 ⇒ `ChangeTab(None)` ⇒ **几页全关**
            //    （自检报 `CurrentTab=None`）。
            if (visualIndex < 0 || visualIndex >= window.visualTypes.Count
                || window.visualTypes[visualIndex] == WindowTabType.None)
            {
                window.NotifyNotBuilt("Booster Packs");
                return;
            }
            CurrentVisualIndex = visualIndex;
            CurrentType = window.visualTypes[visualIndex];
            window.ChangeTab(CurrentType);
        }
    }

    /// <summary>带页签的窗口。原版 `GameWindowWithTabs : GameWindow`。</summary>
    public class GameWindowWithTabs : GameWindow
    {
        /// <summary>`tabs`（原版字段名）—— 顺序 = 原版 `GameWindowWithTabs.tabs` = Missions/Forge/Campaign。</summary>
        public readonly List<WindowTabBase> tabs = new List<WindowTabBase>();

        /// <summary>左栏的**视觉顺序**（= 键表 `RewardsWindow.Buttons` 的顺序）：
        /// Missions / Campaign / Forge / **DailyStreak（🆕 A815 自建）** / BoosterPacks(母版，不是页签)。
        /// ⚠️ **最后一项必须是母版那一格**（`MenuWindowBase.BuildBar` 把 `specs` 最后一项当
        /// `tabButtonPrefab` 并关掉）⇒ 想多一个可见键，就得把它**再往下一格挪**（A815 就是这么做的）。</summary>
        public readonly List<WindowTabType> visualTypes = new List<WindowTabType>
        {
            WindowTabType.Missions, WindowTabType.Campaign, WindowTabType.Forge,
            WindowTabType.DailyStreak, WindowTabType.None,
        };

        public TabButtons tabButtons;
        public Transform tabHolder;

        public WindowTabType CurrentTab { get; private set; } = WindowTabType.None;

        /// <summary>换页。**只切 `activeSelf`，不重建**（原版是 `SetActive` 语义）。</summary>
        public void ChangeTab(WindowTabType type)
        {
            CurrentTab = type;
            foreach (var t in tabs)
                if (t != null) t.gameObject.SetActive(t.Type == type);
            foreach (var t in tabs)
                if (t != null && t.Type == type) t.OnOpen();
            // 🆕 2026-09-23：**切页之后刷一次左栏选中态** —— 原来只有商店在 `Open()` 里手动调，
            //    收藏窗就漏了（截图里高亮停在第 2 键上 ✗）。放在这里，谁都不必再记着调。
            RefreshHighlights();
        }

        /// <summary>左栏选中态：**只画选中的那一个**（原版四键出厂都亮、可见性由运行时 `TabButtons` 驱动）。
        /// 基类默认什么都不做；有左栏高亮的窗覆写它（商店 / 收藏）。</summary>
        public virtual void RefreshHighlights() { }

        /// <summary>点了还没做的件 —— **出声**（红线：不许静默失败）。
        /// ⚠️ 今天唯一会走到这里的是**母版那一格**（`RewardsWindow` 的键表第 5 项 = `Booster Packs`）：
        /// 它运行期**关着**（`TabButtons.Initialize`）⇒ 真鼠标点不到，只有自检会直接调 `Click(那一格)`。</summary>
        public virtual void NotifyNotBuilt(string what)
        {
            Debug.Log($"[Rewards] `{what}` 还没实现（原版是跳商店的卡包页，属**阶段二第 3 层「商店」**；"
                    + "它这一格是**母版**、运行期关着 ⇒ 屏幕上没有这个键可点）");
        }
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
        /// 🔴 **最后一项必须是母版**（`MenuWindowBase.BuildBar` 的约定：`specs` 最后一项 = `tabButtonPrefab` 并关掉）
        /// ⇒ **可见的键 = 本表项数 − 1**。</summary>
        public static readonly TabBtnSpec[] Buttons =
        {
            // 图标出处：Campaign 用的是**主菜单那张导航图** —— `40K_rewards_bt_campaign` 不存在（正本 §〇·3）
            new TabBtnSpec("40K_rewards_bt_missions",   "Missions",       36.0f,  5f, 36f, "RewardsMenu_MissionsButton"),
            new TabBtnSpec("40k_main_bt_campaign",      "Campaign",       36.0f,  5f, 36f, "RewardsMenu_CampaignButton"),
            new TabBtnSpec("40K_rewards_bt_forge",      "Forge",          36.0f,  5f, 36f, "RewardsMenu_ForgeButton"),
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
            new TabBtnSpec("40k_main_bt_rewards",       "Daily Streak",   25.65f, 12f, 33f, "", 47.9f),
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
            // 默认落在 **Missions**（= 左栏第 1 键）。走 `Click` 这条路而不是直接改字段 ——
            // 那三个属性是只读的，而且「点一下」才是唯一的状态入口（两处写同一件事 = 迟早不一致）。
            if (tabButtons != null) tabButtons.Click(0);
            RefreshHighlights();
            RefreshBadges();
        }

        /// <summary>建整个窗口（**自检与运行时同一条路**）。
        /// 🔴 **2026-09-23：外壳（Content Area + 渐变 Background + 左栏 + Tabs）改成调基类的 `BuildShell`**
        /// —— 商店那件要写同一个壳，两处写会迟早不一致。**几何与层次一字未改**（265 条断言守着）。</summary>
        public void Build()
        {
            var res = BuildShell(transform, Buttons, "RewardsTabButton_", "Tabs");
            tabButtons = res.buttons;
            _btnRoot = res.roots;
            _btnHighlight = res.highlight;
            _btnRes = res;
            _btnBadge = res.badge;

            // ---- `Shadow (1)`：出厂 active=false ⇒ **不建**（照 `MainMenuRuntime` 那条纪律③）----

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
        /// 🔴 判据**只此一份**：`idx == 0 &amp;&amp; DailyData.RewardsHasBadge`。
        /// </summary>
        public void RefreshBadges()
        {
            for (int i = 0; i < _btnBadge.Length; i++)
                if (_btnBadge[i] != null)
                    _btnBadge[i].SetTint(new Color(0.7373f, 0.7373f, 0.7373f,
                                                   (i == 0 && DailyData.RewardsHasBadge) ? 1f : 0f));
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

    // ============================================================ 🆕 每日连胜页（自建 · A815）

    /// <summary>🆕 **「每日连胜」页（A815）** —— 奖励窗左栏**第 4 格**。
    ///
    /// <para>🔴 **这一页整个是【自建】的**（**用户 2026-10-15 拍板**），三条边界：
    /// ① **只显示**「本地累计的连续登录天数」，**不做奖励**（不发卡包/金币/骷髅，不接 `Wallet`）；
    /// ② **不接 LiveOps**、不连服务器（原版「每日连胜」是一个 live-ops 事件
    /// `DailyStreakWindow : LiveOpsEventWindow&lt;MainMenuMission&gt;`，数据在远端 CCD）；
    /// ③ 天数**来自 `DailyData` 的本地记录**（跨天 +1 · 断天归 1 · 落盘），⛔ **不是写死的数**。</para>
    ///
    /// <para>⚠️ **原版侧的口径**（照 `资料/普查产出_1015/R5_A815_A817_A819判据.md` §一）：
    /// 原版奖励窗的 `tabs` **只有 Missions/Forge/Campaign 三条**，第 4 格是**母版按钮**；实拍里那个
    /// 「每日连胜」页签是 `BaseParentEventWindow.SetupTabs` 运行期按 live-ops 事件加出来的，
    /// 点下去开的是**另一扇全屏窗**（不是本窗的一页）。我们**没有**那套数据 ⇒ 按用户拍板做成**本窗的第 4 页**。</para>
    ///
    /// <para>⚠️ **凡本页里的数字/文案，出处都写在各处注释里**：矩形与字号**全是我们挑的**
    /// （原版没有这一页 ⇒ 没有可照的尺子），文案复用 `DailyData` 里那两条**已经标着「我们挑的」**的串。</para></summary>
    public class DailyStreakTab : WindowTabBase
    {
        public override WindowTabType Type { get { return WindowTabType.DailyStreak; } }

        RewardsWindow _win;
        Transform _root;

        /// <summary>页矩形：与 `Campaign Tab` 同一格（`RewardsWindow.TabRectFor(0f, 0f)` 算出来 = 330.69..1920 × 70.94..1080）。
        /// ⚠️ **我们挑的** —— 原版没有这一页，没有可照的矩形。</summary>
        public const float TabL = 330.69f, TabT = 70.94f, TabR = 1920f, TabB = 1080f;

        /// <summary>渲染队列：与同族「页」同档（`ForgeTab`/`CampaignTab` 那一带 3027/3064 之内、弹窗 3110+ 之下）。
        /// ⚠️ **我们挑的**（本页没有弹窗，取同族页的中间档）。
        /// 🔴 **`public`**：`Editor/RewardsScene.cs` 有一条「**弹窗的压暗层档必须高于所有页**」的断言，
        /// 2026-10-16（A815）起它把本页这一档也**算进那个比较**（只比常量、不量树）⇒ 得看得见。
        /// ⛔ 改这里的数会让那条断言跟着变，别把最高那一档抬到 3130 以上。</summary>
        public const int QTitle = 3020, QLabel = 3021, QValue = 3022, QInfo = 3023;

        /// <summary>大标题那条**我们挑的**文案（用户实拍那页的大标题是「每日连胜」；本壳一律英文）。</summary>
        public const string TitleText = "Daily Streak";
        /// <summary>三条句子的**画布矩形**（`x1,x2,y1,y2`）—— **全是我们挑的**（原版没有这一页）。
        /// 竖着排在页面中段：标题 → 「Current streak:」+ 数值 → 说明句。</summary>
        static readonly PxRect TitleR = new PxRect(600f, 320f, 1650f, 400f);
        static readonly PxRect LabelR = new PxRect(620f, 560f, 1180f, 640f);
        static readonly PxRect ValueR = new PxRect(1200f, 540f, 1520f, 660f);
        static readonly PxRect InfoR  = new PxRect(600f, 700f, 1650f, 760f);

        /// <summary>文字色：标题/数值取左栏文案那条 **#F4E1AC**（`MenuWindowBase.BuildTabButton` 用的同一个色），
        /// 「Current streak:」用白、说明句用灰 —— **我们挑的**。</summary>
        static readonly Color Gold = new Color(0.9569f, 0.8824f, 0.6745f);
        static readonly Color Grey = new Color(0.70f, 0.70f, 0.70f);

        Label _value;

        public void SetHost(RewardsWindow win, Transform root) { _win = win; _root = root; }

        public override void Setup() { Build(); }

        /// <summary>每次切到本页时**重读一次**本地记录（`DailyData.StreakLoggedDays`）。
        /// 🔴 **为什么必须刷**：连登数是**跨天变的**（进壳那一拍 +1），而本页的节点建一次就不重建
        /// （原版 `SetActive` 换页语义 ⇒ `ChangeTab` 只开关 activeSelf）⇒ 不刷就停在开窗那一刻的旧数上
        /// （静默、且只在「跨天之后没重开窗」时才现形）。</summary>
        public override void OnOpen() { RefreshValue(); }

        /// <summary>本页此刻该显示的数值串（**唯一读口**：`OnOpen` 与自检都走它 ⇒ 两处不会各说各的）。</summary>
        public static string ValueText() { return DailyData.StreakLoggedDays.ToString(); }

        /// <summary>自检用：本页**此刻画出来的**那个数（`null` = 那颗字没建出来）。</summary>
        public string DisplayedValue { get { return _value != null ? _value.Text : null; } }

        /// <summary>把数值那一颗字的正文换成 `ValueText()`（⛔ 不重建节点 —— 只改字）。</summary>
        public void RefreshValue()
        {
            if (_value != null) _value.SetText(ValueText());
        }

        /// <summary>建页内容（**可重复调**：先清空本页子件，与各页同一条纪律）。
        /// 🔴 **节点名一律带 `Daily Streak ` 前缀** —— 本仓自检大量用「按名字全树找」（`FindChild`），
        /// 而**连登弹窗**（`DailyStreakPopup`，同一场景里也会被建出来）已经有 `Current Streak` /
        /// `Current Streak Value` 这两颗同名节点 ⇒ 不加前缀就会**撞名**（`FindChild` 取第一个命中 ⇒
        /// 那些断言会静默量到**别人家的**节点）。⛔ 别把前缀去掉。</summary>
        public void Build()
        {
            MenuDraw.ClearChildren(_root);

            // 大标题（**我们挑的**）
            _win.Text(_root, TitleText, TitleR.x1, TitleR.x2, TitleR.y1, TitleR.y2, 6, Gold,
                      "Daily Streak Title", 70f);
            // 「Current streak:」—— 复用 `DailyData.StreakCurrentLabel()`（那条串自己标着「我们挑的」；
            // 🔴 **不在这里再写一份字面量**：字面量只留在 `DailyData` 那一处，与连登弹窗共用同一条）。
            _win.Text(_root, DailyData.StreakCurrentLabel(), LabelR.x1, LabelR.x2, LabelR.y1, LabelR.y2,
                      6, Color.white, "Daily Streak Label", 70f);
            // 数值 —— **本页唯一的数据件**，值 = `DailyData.StreakLoggedDays`
            _value = _win.Text(_root, ValueText(), ValueR.x1, ValueR.x2, ValueR.y1, ValueR.y2,
                               6, Gold, "Daily Streak Value", 90f);
            // 说明句（复用 `DailyData.StreakInfoText()`，同样「我们挑的」）
            _win.Text(_root, DailyData.StreakInfoText(), InfoR.x1, InfoR.x2, InfoR.y1, InfoR.y2,
                      6, Grey, "Daily Streak Info", 36f);
        }
    }

}
