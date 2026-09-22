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
        }

        /// <summary>点了还没做的件 —— **出声**（红线：不许静默失败）。</summary>
        public virtual void NotifyNotBuilt(string what)
        {
            Debug.Log($"[Rewards] `{what}` 还没实现（原版是跳商店的卡包页，属阶段二第 4 层「商店」）");
        }
    }

    // ============================================================ 奖励窗口本体

    /// <summary>
    /// `Rewards Base Submenu Variant` —— 主菜单左竖导航第 4 钮（REWARDS）开的那个窗。
    /// **`type=Fullscreen` · `placement=Canvas(5)` · `closeOnESC=false` · `extraScaleSmallScreen=1.0`**（正本 §一）。
    /// </summary>
    public class RewardsWindow : GameWindowWithTabs
    {
        // ---- 出处：正本 §一 §二（**机械走链算的**，工具 `工具/menu_rect.py`）----
        /// <summary>`Content Area`：`a=(0,0)-(1,1) pos=(83.59,-35.47) sz=(-167.2,-70.94)` ⇒ **x 167.17..1920.01 · y 70.94..1080.00**</summary>
        public const float ContentL = 167.17f, ContentT = 70.94f, ContentR = 1920.01f, ContentB = 1080f;
        /// <summary>`Tab Buttons`：`a=(0,0)-(0,1) sz=(165,0)` ⇒ x 167.17..332.17 · 与 Content Area 同高。</summary>
        public const float BarW = 165f;
        /// <summary>`VerticalLayoutGroup`：padTop **120** · spacing **0** · UpperCenter · 子高 180</summary>
        public const float BarPadTop = 120f, TabBtnH = 180f;
        /// <summary>`Tab Buttons/Shadow`：`sz=(-117.4,0)` ⇒ 宽 **47.64**、贴左栏左边（实测 x 167.18..214.81）。</summary>
        public const float BarShadowW = 47.64f;
        /// <summary>`Missions Tab`（`Tabs` 的独子）：实测 **x 166.69..1920.00 · y 69.20..1080.00**（1753.31 × 1010.80）。</summary>
        public const float TabL = 166.69f, TabT = 69.20f, TabR = 1920f, TabB = 1080f;
        /// <summary>左栏底图 `40k_main_tab_background` 原版是 **Simple**（不是 Sliced），色白、ppuMul 1。</summary>
        public const string ArtBarBg = "40k_main_tab_background";
        public const string ArtBarShadow = "40k_main_tab_shadow";
        /// <summary>选中/未选中**共用同一张底图**（见文件头纪律②）：`40k_main_bt_selected BW` 纯红 #FF0000。
        /// ⚠️ 工程里的切片名是**下划线**版（导入器把空格换成下划线）：`40k_main_bt_selected_BW`。</summary>
        public const string ArtSelHighlight = "40k_main_bt_selected_BW";
        public const string ArtNametag = "40k_main_bt_nametag";

        /// <summary>`Content Area/Background` 的 `UIGradient`：c1 **#390503** · c2 **#0C0004** · angle **82**（正本 §一）。</summary>
        public static readonly Color GradC1 = new Color(0x39 / 255f, 0x05 / 255f, 0x03 / 255f);
        public static readonly Color GradC2 = new Color(0x0C / 255f, 0.0f, 0x04 / 255f);

        /// <summary>四个键（**视觉顺序**）：图标名 · 文案 · 原版字号(px) · autosize 区间。</summary>
        public static readonly TabBtnSpec[] Buttons =
        {
            // 图标出处：Campaign 用的是**主菜单那张导航图** —— `40K_rewards_bt_campaign` 不存在（正本 §〇·3）
            new TabBtnSpec("40K_rewards_bt_missions",   "Missions",       36.0f,  5f, 36f, "RewardsMenu_MissionsButton"),
            new TabBtnSpec("40k_main_bt_campaign",      "Campaign",       36.0f,  5f, 36f, "RewardsMenu_CampaignButton"),
            new TabBtnSpec("40K_rewards_bt_forge",      "Forge",          36.0f,  5f, 36f, "RewardsMenu_ForgeButton"),
            // 第 4 键：`Menu Navigation Panel Button`，字号 **25.65**（autosize 12→33 缩出来的），没有实例 ID
            new TabBtnSpec("40K_shop_bt_boosters",      "Booster Packs",  25.65f, 12f, 33f, ""),
        };

        public struct TabBtnSpec
        {
            public readonly string Art, Label, InstId;
            public readonly float FontPx, AutoMin, AutoMax;
            public TabBtnSpec(string art, string label, float fontPx, float autoMin, float autoMax, string instId)
            { Art = art; Label = label; FontPx = fontPx; AutoMin = autoMin; AutoMax = autoMax; InstId = instId; }
        }

        /// <summary>本层建出来的页（Missions 一定有；Forge/Campaign 是第 3 层）。</summary>
        public readonly List<string> MissingArt = new List<string>();

        ImageQuad[] _btnHighlight = new ImageQuad[4];
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
        }

        /// <summary>建一个**有矩形语义的容器节点**（摆在原版那个矩形的中心）。
        /// 原版每个节点都有自己的 rect；我们的世界空间里「容器」自己不带渲染，但**位置要摆对** ——
        /// 否则自检量不到、将来做点击/滚动也会算错。
        /// ⚠️ `localPosition` 是**相对父节点**的 ⇒ 必须减掉父的世界位置（第一版忘了减，
        ///    自检报「差 90.80px」—— 那正是父容器中心到原点的距离）。</summary>
        public static Transform Node(Transform parent, string name, PxRect r)
        {
            var t = New(parent, name);
            t.localPosition = Local(parent, r.x1, r.y1, r.x2, r.y2);
            return t;
        }

        /// <summary>建整个窗口（**自检与运行时同一条路**）。</summary>
        public void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) DestroySafe(root.GetChild(i).gameObject);
            MissingArt.Clear();

            var areaRect = new PxRect(ContentL, ContentT, ContentR, ContentB);

            // ---- Content Area（页签内容的父）----
            var area = Node(root, "Content Area", areaRect);
            // `Background`：原版是 `Image(sprite=null)` + **`UIGradient` 双色**（c1 #390503 · c2 #0C0004 · angle 82）
            // ⇒ 走 `CardArt.Gradient`（主菜单整屏背景是同一套做法）。
            // ⚠️ **不能先 `Rect()` 再 `SetTexture`** —— `SetTexture` 会把 `_aspect` 改成贴图自己的
            //    宽高比（128×128 = 1.0），把 `Rect` 刚设好的 1752.83/1009.06 冲掉。
            {
                float w = areaRect.W, h = areaRect.H;
                var q = ImageQuad.Create(area, CardArt.Gradient(GradC1, GradC2, 82f),
                                         Local(area, areaRect.x1, areaRect.y1, areaRect.x2, areaRect.y2),
                                         LayoutSpace.Px(h), new Vector2(0.5f, 0.5f), "Background");
                if (q != null) { q.SetAspect(w / h); q.SetRenderQueue(QPanel); }
            }

            // ---- 左栏 ----
            BuildTabButtons(area);

            // ---- 页签页容器 ----
            var tabsRect = new PxRect(ContentL, ContentT, ContentR, ContentB);   // `Tabs` 与 `Content Area` 同矩形（实证）
            var tabs = Node(area, "Tabs", tabsRect);
            tabHolder = tabs;

            // ---- `Shadow (1)`：出厂 active=false ⇒ **不建**（照 `MainMenuRuntime` 那条纪律③）----

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Rewards] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
        }

        /// <summary>左栏：底图 + 阴影 + 四个键（**每个键：Highlight / Icon / Label 底 + 文案 / Badge**）。</summary>
        void BuildTabButtons(Transform area)
        {
            var barRect = new PxRect(ContentL, ContentT, ContentL + BarW, ContentB);
            var bar = Node(area, "Tab Buttons", barRect);
            Rect(bar, ArtBarBg, barRect.x1, barRect.x2, barRect.y1, barRect.y2, "Background", QPanel);

            // `Tab Buttons/Shadow`：`sz=(-117.4,0)` ⇒ 宽 165-117.4 = 47.64，贴左栏左边（正本 §二·1）
            Rect(bar, ArtBarShadow, ContentL, ContentL + BarShadowW, ContentT, ContentB, "Shadow", QPanel);

            tabButtons = bar.gameObject.AddComponent<TabButtons>();
            tabButtons.window = this;
            tabButtons.options.Clear();

            var holder = New(bar, "Buttons");
            for (int i = 0; i < Buttons.Length; i++)
            {
                // `VerticalLayoutGroup`：padTop 120 从**栏顶**起排 ⇒ 第 i 键顶边 = 70.94 + 120 + 180i
                float top = ContentT + BarPadTop + TabBtnH * i, bot = top + TabBtnH;
                _btnRoot[i] = BuildTabButton(holder, i, Buttons[i], top, bot);
                tabButtons.options.Add(new TabButtons.Option
                {
                    type = visualTypes[i],
                    button = _btnRoot[i].GetComponentInChildren<WindowButton>(true),
                });
            }
        }

        /// <summary>一个键。子件几何**逐条照正本 §二·2 的公共参数表**。</summary>
        Transform BuildTabButton(Transform parent, int idx, TabBtnSpec spec, float y1, float y2)
        {
            var b = Node(parent, "RewardsTabButton_" + idx, new PxRect(ContentL, y1, ContentL + BarW, y2));
            float cx = ContentL + BarW * 0.5f;              // 键的水平中心（栏内）
            float cy = (y1 + y2) * 0.5f;

            // `Highlight`：整键矩形；`40k_main_bt_selected BW`，色 **#FF0000**，**出厂 en=1/a=1**
            _btnHighlight[idx] = Rect(b, ArtSelHighlight, ContentL, ContentL + BarW, y1, y2, "Highlight", QPanel,
                                      new Color(1f, 0f, 0f, 1f));

            // `Icon`：`a=(0,0)-(1,1) p=(.5,.7) sz=(0,0)` + **preserveAspect** ⇒ 等比放进 165×180 并居中
            //（UGUI 的 preserveAspect 是按 rect 居中收 padding，**不看 pivot**）
            var iconTex = Art(spec.Art);
            if (iconTex != null)
            {
                float side = Mathf.Min(BarW, TabBtnH);
                var q = ImageQuad.Create(b, iconTex, Local(b, cx, cy), LayoutSpace.Px(side),
                                         new Vector2(0.5f, 0.5f), "Icon");
                if (q != null) { q.SetAspect(1f); q.SetRenderQueue(QContent); }
            }
            else MissingArt.Add(spec.Art);

            // `Label` 底：`a=(.5,.5) p=(.5,0) pos=(0,-72.16) sz=(155,37.86)`
            const float labW = 155f, labH = 37.86f, labDy = -72.16f;
            float lb = cy - labDy;                          // pivot 在底边 ⇒ 底边 y
            Rect(b, ArtNametag, cx - labW * 0.5f, cx + labW * 0.5f, lb - labH, lb, "Text Background", QContent);

            // 文案：TMP 字号 = 原版 `m_fontSize`（画布像素）；色 **#F4E1AC**；`m_fontStyle=UpperCase`
            var txt = Text(b, spec.Label.ToUpperInvariant(), cx - labW * 0.5f, cx + labW * 0.5f, lb - labH, lb,
                           6, new Color(0.9569f, 0.8824f, 0.6745f), "Text", spec.FontPx);
            if (txt != null) txt.SetAutoFitBox(LayoutSpace.Px(labW), LayoutSpace.Px(labH), spec.AutoMin, spec.AutoMax);

            // `Badge Highlight`：pos **(51.7,-27.2)**（第 4 键是 **(51.7,+47.9)**）；35²；色 **#BCBCBC**
            float bdy = idx == 3 ? 47.9f : -27.2f;
            Rect(b, "40K_notification_number", cx + 51.7f - 17.5f, cx + 51.7f + 17.5f,
                 cy - bdy - 17.5f, cy - bdy + 17.5f, "Badge Highlight", QContent,
                 new Color(0.7373f, 0.7373f, 0.7373f, 1f));

            // 点击区：整键（原版是 `EverguildToggle`，我们只用它的点击语义）
            var hit = New(b, "Hit");
            var hq = ImageQuad.Create(hit, CardArt.Solid(), Local(hit, cx, cy), LayoutSpace.Px(TabBtnH),
                                      new Vector2(0.5f, 0.5f), "Hit");
            if (hq != null)
            {
                hq.SetAspect(BarW / TabBtnH);
                hq.SetTint(new Color(0f, 0f, 0f, 0f));
                hq.SetRenderQueue(QPanel);
            }
            int captured = idx;
            var wb = hit.gameObject.AddComponent<WindowButton>();
            wb.onClick = () => tabButtons.Click(captured);
            return b;
        }

        /// <summary>选中态：**只画选中的那一个**（原生四键出厂都亮，可见性由运行时驱动 —— 文件头纪律②）。</summary>
        public void RefreshHighlights()
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

            // 原版这两页出厂 active=false（正本 §二·4），我们照建但先不给内容 —— **不静默**：
            // 切过去是一块空面板 + 日志说明。
            // 页面矩形照原版：`Forge Tab` N(3, 0,0, 1,1, .5,.5, 81.76,0, -163.5,-0.359) ·
            //                 `Campaign Tab` 同锚点、`sz=(-163.5,0)`
            tabs.Add(EmptyTab("Forge Tab", WindowTabType.Forge, 0f, -0.359f));
            tabs.Add(EmptyTab("Campaign Tab", WindowTabType.Campaign, 0f, 0f));

            foreach (var t in tabs) t.Setup();
        }

        WindowTabBase EmptyTab(string name, WindowTabType type, float szY, float extraSzY)
        {
            var tabsRect = new PxRect(ContentL, ContentT, ContentR, ContentB);
            var r = UguiRect.Child(tabsRect, UguiRect.A00, UguiRect.A11, UguiRect.P50c,
                                   new Vector2(81.76f, szY), new Vector2(-163.5f, extraSzY));
            var go = Node(tabHolder, name, r);
            var t = go.gameObject.AddComponent<EmptyTabStub>();
            t.SetHost(this, go, type);
            return t;
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

        // ============================================================ 坐标与绘图工具
        //
        // 🔴 像素→世界的换算**只有 `LayoutSpace` 那一份**（`LayoutSpace.RectCenter` / `Px`）。
        //    这里只做「按像素矩形摆一张图 / 一段字」的包装。

        // ⚠️ 分层用**渲染队列**、不用 z（`ImageQuad` 全是透明队列，按到相机的 3D 距离排序 —— 屏幕中间的
        //    反而更近）。**同一个队列 + z 都是 0 ⇒ 谁盖谁完全不确定**（2026-09-22 踩过）⇒ 每层差 1 都行。
        //    数值取在 `PopUpGameWindow`(3020) **之下** —— 弹窗要能盖住本窗。
        public const int QPanel = 3005, QContent = 3010, QText = 3011, QOverlay = 3014;

        public static Transform New(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        public static void DestroySafe(GameObject go)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) { DestroyImmediate(go); return; }
#endif
            Object.Destroy(go);
        }

        /// <summary>取图（`CardArt.MenuUi` 会在 `ui_menu/ → ui_deck/ → ui/` 三批里兜底）。取不到记进 `MissingArt`。</summary>
        public Texture2D Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name)) MissingArt.Add(name);
            return t;
        }

        /// <summary>按**原版像素矩形**摆一张图。`art == null` = 纯色块（原版那种「没 sprite、只有 `m_Color`」的件）。
        /// `keepAspect` = 是否按图自身宽高比（原版 `preserveAspect`）；false = 拉伸到矩形。
        /// ⚠️ `ImageQuad.Create` 的 `pos` 是 **localPosition** ⇒ 这里要减掉父节点的世界位置
        /// （容器节点是有真实位置的，见 `Node`）。</summary>
        public ImageQuad Rect(Transform parent, string art, float x1, float x2, float y1, float y2, string name,
                              int q, Color? tint = null, bool keepAspect = false)
        {
            var tex = art == null ? CardArt.Solid() : Art(art);
            if (tex == null) return null;
            float w = x2 - x1, h = y2 - y1;
            var quad = ImageQuad.Create(parent, tex, Local(parent, x1, y1, x2, y2), LayoutSpace.Px(h),
                                        new Vector2(0.5f, 0.5f), name);
            if (quad == null) return null;
            quad.SetAspect(w / h);
            quad.SetRenderQueue(q);
            if (tint.HasValue) quad.SetTint(tint.Value);
            return quad;
        }

        /// <summary>原版像素矩形中心 → **相对 `parent` 的局部坐标**。</summary>
        public static Vector3 Local(Transform parent, float x1, float y1, float x2, float y2)
            => LayoutSpace.RectCenter(x1, y1, x2, y2) - (parent != null ? parent.position : Vector3.zero);

        /// <summary>原版像素**点** → 相对 `parent` 的局部坐标。
        /// 🔴 `ImageQuad.Create` / `Label.Create` 的 `pos` 都是 **localPosition** ——
        ///    直接喂 `LayoutSpace.FromPixel(...)`/`RectCenter(...)`（世界坐标）在父节点有偏移时会**双倍错位**。
        ///    第一版左栏四个图标、内容区渐变背景、进度条九宫格全栽在这上面，而且**断言全绿**
        ///    （断言量矩形中心/宽度，量不到「整块画到别处去了」）。</summary>
        public static Vector3 Local(Transform parent, float xPx, float yPx)
            => LayoutSpace.FromPixel(xPx, yPx) - (parent != null ? parent.position : Vector3.zero);

        /// <summary>按像素矩形摆一段文字（居中）。`fontPx` = **原版 TMP 的 `m_fontSize`**（画布像素）
        /// —— 内部走 `Label.SetGlyphHeight(px/108)`；🔴 **别用 `SetFontSize(px/108)`**，那会大 2.7 倍（正本 §七）。</summary>
        public Label Text(Transform parent, string text, float x1, float x2, float y1, float y2, int scale,
                          Color color, string name, float fontPx = 0f)
        {
            var lb = Label.Create(parent, text, Local(parent, x1, y1, x2, y2), scale, color,
                                  new Vector2(0.5f, 0.5f), name);
            if (lb == null) return null;
            lb.SetRenderQueue(QText);
            if (fontPx > 0f) lb.SetGlyphHeight(LayoutSpace.Px(fontPx));
            return lb;
        }

        /// <summary>
        /// **按原版 TMP 的规矩**摆一段文字：**限宽换行**（`m_TextWrappingMode = 1`）+ 可选的**自适应字号**
        /// （`m_enableAutoSizing`）。
        /// 🔴 **为什么必须有这个包装**（2026-09-23 并排看图发现的）：第一版只调了 `Label.Text`，
        ///    而 `Label` 内部把换行模式写死成 `NoWrap`（`Battle/Label.cs`）⇒
        ///    每日任务行那句 `Deal 500 damage to enemy units`（35px）直接**冲出卡外**，
        ///    **61 条断言一条都没报**（断言量的是矩形中心与宽度，量不到「字溢出了」）。
        ///    原版那条 TMP 的实测：`m_fontSize 35 · m_enableAutoSizing 1 · m_fontSizeMin 15 · m_fontSizeMax 35`。
        /// </summary>
        /// <param name="autoMinPx">原版 `m_fontSizeMin`（**画布像素**）。传 0 = 不开自适应（只换行）。</param>
        public Label TextBox(Transform parent, PxRect r, string text, Color color, string name, float fontPx,
                             float autoMinPx = 0f)
        {
            var lb = Text(parent, text, r.x1, r.x2, r.y1, r.y2, 5, color, name, fontPx);
            if (lb == null) return null;
            lb.SetWrapWidth(LayoutSpace.Px(r.W));
            // ⚠️ `SetAutoFitBox` 内部按**比例**算 min/max（`fontSizeMin/Max` 的单位和 `fontSize` 一样、
            //    不是世界单位 —— 直接填 px/108 会把字号压到 0.3px、整行看不见，2026-09-22 踩过）
            if (autoMinPx > 0f && fontPx > autoMinPx)
                lb.SetAutoFitBox(LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), autoMinPx, fontPx);
            return lb;
        }
    }

    /// <summary>还没做的那两页（Forge / Campaign）。**切过去要出声**，不是一块静悄悄的空面板。</summary>
    public class EmptyTabStub : WindowTabBase
    {
        WindowTabType _type = WindowTabType.None;
        GameWindowWithTabs _host;
        Transform _root;
        bool _told;

        public override WindowTabType Type { get { return _type; } }

        public void SetHost(GameWindowWithTabs host, Transform root, WindowTabType t)
        { _host = host; _root = root; _type = t; }

        public override void OnOpen()
        {
            if (_told) return;
            _told = true;
            Debug.Log($"[Rewards] `{_type}` 页**还没做**（阶段二第 3 层）—— 现在切过去只会看到一块空面板。" +
                      "出处：`资料/日常_原版规格.md` §二·4（原版该页出厂 `activeSelf=false`）");
        }
    }
}
