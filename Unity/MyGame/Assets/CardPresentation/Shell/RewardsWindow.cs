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
//   ③ **第 4 键（Booster Packs）不是页签** —— 它是 `tabButtonPrefab` 的母版，不在 `tabs` 里
//      （正本 §二·3）。它点下去应跳商店的卡包页，**属第 4 层商店**；本层按边界③如实提示。
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
    }

    /// <summary>一页。原版叫 `WindowTabBase`（`MissionsTab : WindowTabBase<MainMenuRewardsWindow>`）。</summary>
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
        /// 出场时是激活的，`Initialize` 第一件事就是把它关掉 ⇒ **左栏运行期只有 3 个键**。</summary>
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

        /// <summary>点左栏某一键（`visualIndex` = 视觉顺序 0..3）。第 4 个（Booster Packs）不是页签。</summary>
        public void Click(int visualIndex)
        {
            if (window == null) return;
            // 🔴 判据是「**这个位置挂的是不是页签**」，不是「下标越没越界」——
            //    `visualTypes[3]` 是 `None`（第 4 键是 `tabButtonPrefab` 母版，不在 `tabs` 里，正本 §二·3）。
            //    第一版按下标判，`Click(3)` 会走进正常分支 ⇒ `ChangeTab(None)` ⇒ **三页全关**（自检报 `CurrentTab=None`）。
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

        /// <summary>左栏的**视觉顺序**（= 原版 `Tab Buttons` 下四个子节点的顺序）：
        /// Missions / Campaign / Forge / BoosterPacks(=第 4 键，不是页签)。</summary>
        public readonly List<WindowTabType> visualTypes = new List<WindowTabType>
        {
            WindowTabType.Missions, WindowTabType.Campaign, WindowTabType.Forge, WindowTabType.None,
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

        /// <summary>点了还没做的件 —— **出声**（红线：不许静默失败）。</summary>
        public virtual void NotifyNotBuilt(string what)
        {
            Debug.Log($"[Rewards] `{what}` 还没实现（原版是跳商店的卡包页，属**阶段二第 3 层「商店」**）");
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

        /// <summary>四个键（**视觉顺序**）：图标名 · 文案 · 原版字号(px) · autosize 区间 · 实例 ID · 红点偏置。</summary>
        public static readonly TabBtnSpec[] Buttons =
        {
            // 图标出处：Campaign 用的是**主菜单那张导航图** —— `40K_rewards_bt_campaign` 不存在（正本 §〇·3）
            new TabBtnSpec("40K_rewards_bt_missions",   "Missions",       36.0f,  5f, 36f, "RewardsMenu_MissionsButton"),
            new TabBtnSpec("40k_main_bt_campaign",      "Campaign",       36.0f,  5f, 36f, "RewardsMenu_CampaignButton"),
            new TabBtnSpec("40K_rewards_bt_forge",      "Forge",          36.0f,  5f, 36f, "RewardsMenu_ForgeButton"),
            // 第 4 键：`Menu Navigation Panel Button`，字号 **25.65**（autosize 12→33 缩出来的），没有实例 ID；
            // 🔴 **红点偏置是 +47.9**（其余三键 −27.2）—— 实测值。原来写死在循环里 `idx == 3 ? 47.9f : −27.2f`，
            //    抽基类时挪进规格 ⇒ 商店那套键表不用再抄这条特例。
            new TabBtnSpec("40K_shop_bt_boosters",      "Booster Packs",  25.65f, 12f, 33f, "", 47.9f),
        };

        ImageQuad[] _btnHighlight = new ImageQuad[4];
        /// <summary>四个键的红点（⏭ 显隐靠 **alpha**，见 `RefreshBadges`）。</summary>
        ImageQuad[] _btnBadge = new ImageQuad[4];
        Transform[] _btnRoot = new Transform[4];

        // ---------------------------------------------------------- 建

        public static RewardsWindow Create(WindowsManager mgr)
        {
            var go = new GameObject("Rewards Base Submenu Variant");
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
            _btnBadge = res.badge;

            // ---- `Shadow (1)`：出厂 active=false ⇒ **不建**（照 `MainMenuRuntime` 那条纪律③）----

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Rewards] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
        }

        /// <summary>
        /// 重算四个键的红点。原版由 **`UiBadgeNotificationManager.Refresh()`** 在通知变化时推
        /// （`Missions.CheckNotification` → `INotificationProvider<MissionsBadge>`）—— **是事件驱动的**。
        /// ⚠️ **我们还没有通知总线** ⇒ 只在 `Open()`（和自检）里各调一次；
        /// 真接了通知源之后应该改成订阅（⏭ 记在 `资料/日常_画面逐项对_0923.md` 的 D3）。
        /// 🔴 判据**只此一份**：`idx == 0 && DailyData.RewardsHasBadge`。
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
            for (int i = 0; i < _btnHighlight.Length; i++)
                if (_btnHighlight[i] != null) _btnHighlight[i].gameObject.SetActive(i == sel);
        }

        // ---------------------------------------------------------- 页

        /// <summary>建三页。**Missions 是本层的活**；Forge/Campaign 是第 3 层 —— 先建**空页**并出声。</summary>
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
