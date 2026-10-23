// ParentEventWindow.cs — **活动承载窗**（界面线 `T28`；原版 `BaseParentEventWindow` 那一族 / 具体 prefab = `Parent Event Window`）
//
// ============================ 出处（唯一判据，全部本地现读 · 2026-10-17）============================
// ① **全树 dump**（可复跑；本文件每个矩形/字号/颜色都能在那条输出里逐字找到）：
//      `d:/4/Unity/资料/普查产出_1017/UI/D_ParentEventWindow_dump.txt`
//      重跑：`python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Parent Event Window" --depth 10 --relative`
// ② **根组件**（逐字段实读）：`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-1838579764277826042.json`
//      （脚本 = `TwoSidesEventWindow`，见 `bundle_Waprforge_monoscripts/MonoScript/MonoScript_5025015628206364007.json`）
//      · `type = 0`(Fullscreen) · `windowsPlacement = 15`(Popup) · `closeOnESC = 0` · `updateNavPanel = 0`
//      🔴 **2026-10-23 注：`updateNavPanel` 我们**不取**这份（0），改为取基座 `Parent Event Window` 那份的 `1`
//         （MB `6349037533193994564`）—— 裁定与理由 → `ParamsUpdateNavPanel` 那段注释。
//         `closeOnEsc` 仍取本份（0），如实记（基座那份是 1）。
//      · `extraScaleSmallScreen = 1.0` · `useDefaultCloseSoundIfNull = 1` · `openSound/closeSound = {0,0}`
//      · `tabs` = **2 个 PPtr**：`5874186985141415430`(`TwoSidesEvent_ChooseSideTab`) ·
//        `-7528133727782064634`(`TwoSidesEvent_OnGoingTab`) · `backgroundCloseButton = 2610068799221640710`
// ③ **同一 GO 上的 `TabbedWindowComponents`**（MB `-2754843456145227452`，脚本 `9108590536014901877`）：
//      `tabButtons = 7645027671073194308`（`TabButtons` MB：`tabButtonPrefab = -5825802606871740092`、
//      `options = []`、`group = 7535499631002485060`(`ToggleGroup`，`m_AllowSwitchOff = 0`)）·
//      `tabPrefab = 1358808318654906692`（= `Sample Tab` 那颗模板的 GO）· `tabHolder = 6733376813114622276`（= `Tabs` 节点）
// ④ **行为**（反编译，方法体都在）：
//      · `BaseParentEventWindow__Open.c` → `GameWindowWithTabs.Open()` + 把导航栏拨到第 5 格
//      · `BaseParentEventWindow__SetupTabs.c` → 遍历 liveops 事件 → `TabButtons.AddTabButton(label, sprite)`
//      · `TabButtons__Initialize.c` → **母版 `tabButtonPrefab.SetActive(false)`**
//      · `TabButtons__AddTabButton.c` → `Instantiate(tabButtonPrefab, tabHolder)` → `SetActive(true)` →
//        `SetSiblingIndex(0)` → `SetName(label)` → `Image.sprite = 图标` → `Toggle.group = group` →
//        找子件文字组件并 `SetText(label)`
//      · `TwoSidesEventWindow__Open.c` = `GameWindowWithTabs.Open()` + `CheckAndOpenTab()`；
//        `CheckAndOpenTab` 按玩家是否已选阵营切到两个 tab 之一（`GetTab(name)` → `ChangeTab`）
//      · `TwoSidesEventWindow__OnEnable.c` 给 `backgroundCloseButton.onClick` 挂监听（同一个 delegate 也交给
//        表头返回钮）⇒ **点压暗层 = 关窗**（同 `EnergySinglePlayerOnlyEventWindow` 那条已订正的口径）
//
// ============================ 🆕 2026-10-23（活动线 L01/L02/L03 收尾）：页签与入口 ============================
// 🔴 **就地订正（铁律 5）**：本文件上面第 ④ 条只把「原版 `SetupTabs` → `AddTabButton`」**记成行为**、
//   **没有实现**（`资料/普查产出_1023c/工具与活动线_核销报告.md` §二 就是这么核销的）。现在补上了，
//   判据（`d:/2/tools/decomp_full/`，方法体完整现读）：
//   · `BaseParentEventWindow__SetupTabs.c`：`LiveOpsManager.GetAllEvents(2, …)` → `Where(谓词)` → 逐事件：
//       `HasComponent(ev, 13 /*TabButton*/)` ⇒ `label = GetLabel(ev, 0)` · `sprite = LoadAsset(ev, 0)` →
//         `TabButtons.AddTabButton(label, sprite)` → `transform.SetAsLastSibling()` → `EverguildToggle.add_OnClick(…)`；
//         那个回调整句 = **开另一扇窗**：`WindowsManager.OpenWindow(GetComponentReference(ev, 12 /*Popup*/), ev)`
//         （`BaseParentEventWindow.__c__DisplayClass4_0___SetupTabs_b__2.c`）
//       `HasComponent(ev, 11 /*Tab*/)` ⇒ `CreateEventTab(ev)` = **本窗内的一个内容页**
//         （label = `TryGetLabel(ev,4) ?? TryGetLabel(ev,0)`；组件 = `LoadComponent(ev,11)`）
//       一个事件都没有 ⇒ `GameWindowWithTabs.SetupTabs(0)`（基类 ⇒ 左栏空）
//   · `TabButtons__AddTabButton.c` 逐句：`Instantiate(tabButtonPrefab, tabHolder)` → `SetActive(true)` →
//     `SetSiblingIndex(0)` → **`set_name(label)`** → `Image.sprite = 图标` → `Toggle.group = group` →
//     找子件文字组件 `SetText(label)`
//   ⇒ **两种页签**：①「事件钮」= 左侧竖栏一颗、点了**开另一扇窗**；②「内容页」= 本窗里的一页。
//     本窗两种都有：内容页 = Two Sides 的两页（`PageNames`）；事件钮 = `SetupTabs(BuildEventTabs(...))`
//     建出来的**兄弟活动窗**（Gacha / Raid / Expansion Pass）。
//
// 🔴 **数据源（原版 vs 我们）**：原版读 `LiveOpsManager.GetAllEvents`（远端 PlayFab/CCD），本机没有那条链
//   ⇒ 事件钮名单 = 四扇活动窗里**除本窗以外**的那几扇；其中 Parent / Expansion Pass 两扇在
//   `LiveOpsLocalCatalog`（13 份活动 SO 的离线等价物）里有同类**在期且可选中**的实例时，页签名与 id 取那个事件。
//   · 页签名次序 = **label 4（NavigationBarTitle） > label 0（Title） > 该窗自己的官方词条兜底**
//     （`SetupTabs` 的事件钮那一支读 label 0、`CreateEventTab` / `OpenEventButton.SetButtonInfo` 读 4；
//      我们取 4 优先 —— 与 `TabLabelNote` 那条已定的口径一致）。
//   · 页签图标 = `LoadAsset(ev, 0)`（`EventAssetReferenceType.Icon`）。本机只有 GUID / 子件名、**没有可加载的
//     Sprite** ⇒ 留空（⛔ 不用母版 `40K_shop_bt_boosters` 冒充）。
//   · **没有在期事件时仍然建出事件钮** —— 这是**我们的一处有意偏离**（原版没有 live 事件就没有那颗钮），
//     理由 = 验收口径「页面能打开、看得到原版结构、空态出声」（与 `Shell/GachaEventWindow.cs` 文件头那条同口径）。
//   · **过期事件不参与**（用户边界「过期活动不做」）⇒ 今天（2026-10-23）13 份 SO **全部过期**，全走官方词条兜底。
//
// 🔴 **入口**：原版 = **左竖导航第 6 颗（`Event`）那颗「事件钮」**
//   `NavigationPanelController.BuildEventButtons`（每个 live 事件 `Instantiate(eventButtonPrefab)` →
//   `OpenEventButton.Initialize` → `SetButtonInfo`：图标 = `LoadAsset(ev,0)` · over-icon = `LoadAsset(ev,9)` ·
//   文字 = `TryGetLabel(4) ?? (0)` · 计时 = `LiveOpsUtility.IsTimedEvent`）
//   → 点它 = `OpenEventButton__ButtonClick` → `OpenWindow` →
//     `WindowsManager.OpenWindow(GetComponentReference(ev, 10 /*MainWindow*/), ev)`；
//   关回来 = `ToggleValueChange(false)` → 那扇窗的 `Close()`。
//   🔴 **`ev → 哪扇窗` 的映射已核过**（`bundle_menus_assets_all/AssetBundle/AssetBundle_1.json` 的
//     `m_Container`（键 = GUID）→ PathID → UnityPy 读名）：
//       `Parent` 事件 `MainWindow` = `9a26697357770d8458298d89467fd459` → PathID `-3290546486736551612`
//         = **`Parent Event Window`**（就是本文件这扇）；
//       `Scoring` 事件 `Tab` = `e6914cbdd45814f5f9bd40a854c12218` → PathID `-555883983194387681`
//         = **`Expansion Pass Tab`**（= `ExpansionPassEventWindow`）。
//   ⇒ 我们的等价物 = `WindowsManager.OpenEventHub(EventWindowKind)`（那一跳的唯一共用口）。
//   ✅ **2026-10-23：最后一跳【已接】** —— `Shell/MainMenuRuntime.cs` 的 `BuildNavigationPanel` 末尾建出第 6 颗
//     **`Event Navigation Button`**（原版 prefab 真名；`Main Menu Navigation Button` 那份里**没有**第 6 个），
//     `OnNavClick(5)` → `OpenEventHub(Parent)`；`SetNavToggle(Event)` 现在**真的点亮它**
//     （值序 = `NavigationPanelToggleType.Event = 5`，与原版 `SetToggle(nav,5)` 逐值相同）。
//
// ============================ 🔴 两个 prefab 的事实（照实记录，⛔ 别把它们混成一份）============================
// 本包里有**两份**：
//   · `Parent Event Window`（GO `-3290546486736551612`）：组件 = `TabbedWindowComponents` + `BaseParentEventWindow`
//     （MB `6349037533193994564`：`tabs = []` · `windowsPlacement = 5` · `closeOnESC = 1` · `updateNavPanel = 1`）。
//     **左侧竖页签栏 + 右侧页内容 + `Sample Tab` 模板**就是这一份的树（= D_ dump 那一张表）。
//   · `Two Sides Event Window`（GO `-2798723332429074938`）：组件 = `TwoSidesEventWindow`，**`tabs` = 2 个**，
//     两个 tab 内容（`Select Team Tab` / `Event On Going Tab`）**直接挂在这份 prefab 的根下**（没有左栏）。
// 本件按**任务简报**把两者合起来：**外壳照 `Parent Event Window` 的树**（左栏 + Tabs 容器 + 暗底关窗），
// **窗参 / 两页照 `Two Sides Event Window`**（`tabs` 那 2 个 PPtr）。
// ⚠️ 这是**我们挑的组合**（原版运行期 `BaseParentEventWindow.SetupTabs` 才会往左栏里 `AddTabButton`，
//   而 Two Sides 那份是**预置两页、不建按钮**）—— 如实记在这里，⛔ 别写成「原版 prefab 就是这样」。
//
// ============================ 🔴 数据缺口（已查实，⛔ 不编）============================
// 活动数据全部来自 **LiveOps（远端 PlayFab / CCD）**，本地离线包体里没有：
//   · 两个 tab 的**标题词条 / 图标 sprite**（`LiveOpsAssetUtility.GetLabel` / `AddTabButton(label, sprite)` 的实参）
//   · 队伍名 / 队长名 / 阵营图 / 当前分数 / 里程碑 / 奖励内容 / 倒计时截止时刻
//   · `GetTimeDescriptionTextKey()` 返回的「Ends in:」那一句（`TwoSidesEvent_OnGoingTab__OnOpen.c`）
// 判据：`官方本地化_en+zh-CN.json`（9,441 条）里 **0 条**命中 `Two Sides` / `Choose your side`；
//       事件 tab 的 label 也不在 `cards_engine.json` / 任何本地表里。
// ⇒ 本窗**照 prefab 出厂原文画**（`Red Team` / `Blue Team` / `#squidmar` / `1256` / `125` /
//   `BATTLE FOR WARPFORGE` / `Choose your side. Fight for glory!` …），并**出声一次**；
//   **页签文字**用两个 tab 的**节点名**（`Select Team Tab` / `Event On Going Tab`，本地判据）兜底；
//   **页签图标**留空（`AddTabButton` 的 sprite 实参本地没有）—— ⛔ 不拿 `40K_shop_bt_boosters` 冒充。
// 另有两张图**源图在本地、尚未导入工程**：`VS icon` / `40k_battle_Win Skull SDF`（源名带空格；落盘换下划线）
//   ⇒ 已在 `工具/sync_battle_ui_art.py` 的 `NAMES_MENU` 登记，跑一次同步即到位（到位前对应件不画 + 记账）。
//
// ============================ 我们侧的对位（逐件写清，不冒充原版）============================
// · **为什么不继承 `GameWindowWithTabs`**：我们那个类不是原版泛型基类的等价物 —— 它的 `tabs` 是
//   `List<WindowTabBase>` + `WindowTabType` 枚举，是**奖励窗/商店/收藏**那一族（固定键表、`visualTypes`）的
//   机制；本窗的两页是**数据驱动**的（`tabs` 两个 PPtr、由 liveops 事件决定落在哪页），套进去会**静默**
//   把奖励窗那套键表拖进来（同 `EnergySinglePlayerOnlyEventWindow` 文件头那条「不硬继承」的理由）。
//   ⇒ 直接挂 `GameWindow`，页签/换页自己实现（`SwitchPage`），等价关系如实记在这里。
// · **两个 tab 不各建一个 `MonoBehaviour` 子类**：原版 `WindowTabBase<T>` 的职责是「OnOpen 拉 liveops 数据」，
//   而那份数据本地没有（见下）⇒ 两页由本文件的 `BuildChooseSidePage` / `BuildOnGoingPage` 建树，
//   切页只做 `SetActive`（原版 `GameWindowWithTabs.ChangeTab` 的语义）。
// · **页签栏渐变**（`Tab Buttons` 的 `Gradient2`，MB `-6105840469599813308` 实读）：
//     `_gradientType = 1`(Vertical) · **`_blendMode = 0`(Override)** · `_modifyVertices = 1` · `_offset = 0` · `_zoom = 1`
//     色键 4 个（RGB/时间**无歧义**）：`0 → 黑` · `21974/65535=0.3353 → 白` · `47995/65535=0.7324 → 白` ·
//     `1 → (0.283,0.283,0.283)`；`m_NumAlphaKeys = 2`（`atime 0/65535`，alpha 取 `key0.a/key1.a` = **1/1**）
//     ⇒ **alpha 恒 1**；色标自己的 `key2.a/key3.a = 0` **不生效**（Unity `Gradient.Evaluate` 只取 alphaKeys）。
//   ✅ **2026-10-17（第四十八会话 · `N53①`）：已按四段逐带复刻**（`MenuDraw.GradientV`，`t=0` 在下）。
//     旧注释写「方法体被反编译打散 / 只能取两端」**已过期** —— `ModifyMesh` 945 行**完整在**
//     `d:/2/tools/decomp_full/UnityEngine.UI.Extensions.Gradient2__ModifyMesh.c`。
// · **`UICornersGradient`（两张队伍卡底，MB 实读）**：四角色值逐条照抄（A 红 / B 蓝）。
//   🔴 **2026-10-18 就地订正（铁律 5）**：本行原来写「SkewedImage 的 `skewVector = 0.09` 没做」——
//   **已过期**：`MenuDraw.Rect(..., skewX: 0.09f)` 在 `本文件`（`Background`）与 `本文件`（`General Red Background`）
//   **都已接**（`N53②` 落地）。`Mask` **不接**：本壳用 `ViewportClip` 等价，而这两件**没有裁切需求**（`本文件`）。
// · **`EverguildToggle` 的选中态**：原版靠 `m_IsOn` 换 on/off 色与 sprite；本窗只有**两颗页签**，
//   选中态 = **只显示当前页那颗的 `Highlight`**（红 `40k_main_bt_selected_BW`，MB 实读 `m_Color = (1,0,0,1)`）。
// · **`UIAnchorAvoidSafeArea`**（`Background` / `General Red Background` 各一颗）：本壳**没有**逐节点的安全区组件
//   ⇒ 用序列化矩形（与卡组编辑 / 其它菜单窗同一条处置）。`General Red Background` 自己的 rect 在 dump 里是**退化值**
//   （`634.3 → 474.6`，负高），所以那一族用**子件导出值**（红底 `93.6 → 1015.4`、vignette `-28.2 → 1082.3`）。
// · **倒计时**：`Timer With Time Description` 接 `TimerDisplay`（截止时刻本地没有 ⇒ `--` + 出声一次，⛔ 不编日期）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>活动承载窗（原版 `BaseParentEventWindow` 那一族；本件 = `Parent Event Window` 外壳 + Two Sides 的两页）。
    /// <para>🔴 **活动数据全在远端 LiveOps** ⇒ 照 prefab 出厂原文画 + 出声一次；页签文字用节点名兜底、
    /// 页签图标留空（判据 → 文件头「数据缺口」）。</para>
    /// <para>⚠️ 本窗**没有生产入口**：原版由 `ParentEventHandler` / liveops 事件链打开，我们**没有**那条链
    /// ⇒ 只建 + 注册（`WindowsManager.OpenParentEvent`），⛔ 不伪造调用点（同 `T26/T27` 口径）。</para></summary>
    public class ParentEventWindow : GameWindow
    {
        // ============================================================ 队列档（自成一档 · 3610–3629）
        // 相邻带：下一档 3935（`EnergySinglePlayerOnlyEventWindow`）· `Tooltip` 4077–4079（2026-10-20 从 3605–3607 搬来）。层带不许重叠
        //（口径 → `Shell/BattleLogPopup.cs` 里那段同族注释）。
        public const int QShade = 3610,       // `Menu Dark Background`（含「点暗底关窗」那颗命中区）
                         QContentBg = 3611,   // `Content Area/Background` 的整块渐变（**低于所有页**）
                         QPageBg = 3612,      // 页背景（红底 / 队伍卡底）
                         QPageArt = 3613,     // 页内装饰（noise / vignette / glow / 线）
                         QPageArt1 = 3614,    // 页内主体图（头像图 / 标题底 / 边框 / 骷髅 / 箱子）
                         QPageText = 3615,    // 页内文字
                         QBar = 3616,         // 记分条 / 进度条
                         QPanel = 3618,       // 左侧页签栏底板
                         QTabShadow = 3619,   // 页签栏的 Shadow / Shadow (1)
                         QTabHi = 3620,       // 页签选中高亮
                         QTabIcon = 3621,     // 页签图标
                         QTabName = 3622,     // 页签名牌底板
                         QTabLabel = 3623,    // 页签文字
                         QBadge = 3624,       // 红点底
                         QBadgeText = 3625;   // 红点文字
        // 🔴 **页签栏整族（3618–3625）必须高于页内全部件（≤3616）** —— 两页的矩形是**屏幕绝对**
        //   （从 x=0 起），会和左栏在 x 167–332 那一条**重叠**；队列反了 ⇒ 页内容盖住页签栏（2026-09-20
        //   A117 同族那次的症状：透明队列按到相机的距离排，**z 对不代表压对**）。
        /// <summary>本窗**内容命中区**那一档（压暗层命中区必须严格低于它）。
        /// 吸收层由 `MenuDraw.Absorb` 取 `QHit − 1`（3628）。</summary>
        public const int QHit = 3629;

        // ============================================================ 根 / 节点名
        /// <summary>prefab 根名（= `Create()` 里 `new GameObject(...)` 用的那个名字，**两处同源**）。</summary>
        public const string RootName = "Parent Event Window";

        // ============================================================ 窗参（MB `-1838579764277826042` 实读）
        // ⚠️ 见文件头「两个 prefab」：这一组里 `closeOnEsc` / `extraScaleSmallScreen` / `useDefaultCloseSound`
        //    是 **Two Sides 那份**的值（任务简报指定）；基座 `Parent Event Window` 自己那一份是
        //    `placement=5 / closeOnESC=1 / updateNavPanel=1 / tabs=[]`。
        // 🔴 **2026-10-23（界面线 W62② · 调度台裁定）：`updateNavPanel` 照【基座 `Parent Event Window`】对齐成 `1`。**
        //    判据（原版 MB 现读）：基座那份 = `MonoBehaviour_6349037533193994564.json`
        //    （GO `-3290546486736551612` = `Parent Event Window`）`updateNavPanel = 1`；
        //    Two Sides 那份 = `MonoBehaviour_-1838579764277826042.json`（GO `-2798723332429074938`）
        //    `updateNavPanel = 0`。语义 = `GameWindow__ESCPressed.c` 末段：ESC 真的关掉窗之后
        //    `updateNavPanel` 为真 ⇒ `SetMainToggle(Main)` ⇒ **左竖导航灯回 `Main`**
        //    （不改 ⇒ 从活动窗 ESC 关回主菜单后灯**停在 `Event`**）。
        //    ⚠️ **`closeOnEsc` 仍是 Two Sides 的 `0`**（本件外壳 + 两页是两份 prefab 的组合，见文件头）——
        //      那是**另一条口径**、不在本条裁定射程内（如实记：基座那份是 `1`）。
        public const bool ParamsCloseOnEsc = false;        // `closeOnESC = 0`（Two Sides 那份）
        public const bool ParamsUpdateNavPanel = true;     // `updateNavPanel = 1`（基座 `Parent Event Window` 那份）
        public const float ParamsExtraScale = 1f;          // `extraScaleSmallScreen = 1.0`
        public const bool ParamsUseDefaultCloseSound = true; // `useDefaultCloseSoundIfNull = 1`

        // ============================================================ 几何（D_ dump 现读 · 1920×1080 · 左上原点）
        public static readonly PxRect ShadeR = new PxRect(-1327.3f, -746.2f, 3247.3f, 1826.2f);   // 4574.60×2572.36
        public static readonly Color ShadeTint = new Color(0f, 0f, 0f, 0.772549f);                 // Image.m_Color 实读

        public const float TabsX = 167.2f, TabsY = 70.9f;
        public static readonly PxRect ContentAreaR = new PxRect(167.2f, 70.9f, 1920.0f, 1080.0f);  // 1752.83×1009.06
        public static readonly PxRect TabButtonsR = new PxRect(167.2f, 70.9f, 332.2f, 1080.0f);    // 165×1009.06
        public static readonly PxRect TabShadowR = new PxRect(167.2f, 70.9f, 214.8f, 1080.0f);     // 47.64
        public static readonly PxRect TabsR = new PxRect(167.2f, 70.9f, 1920.0f, 1080.0f);
        public static readonly PxRect Shadow1R = new PxRect(330.4f, 70.9f, 379.8f, 1080.0f);       // 49.43 · 出厂 INACT

        // `Content Area/Background` 的 `UIGradient`（MB `4376020486595051844` 实读）：
        //   m_color1 = (0.2235294, 0.0117647, 0.0196078, 1) · m_color2 = (0.0470588, 0, 0.0156863, 1)
        //   m_angle = 82 · m_ignoreRatio = 1
        // 🔴 与卡组编辑 `Content Area/Background` **逐位相同**；角度换算沿用那条已订正的结论（传 188，⛔ 不是 82）：
        //    原版 `UIGradient` 方向是 (sin,cos)、我们助手是 (cos,sin) ⇒ 差 90°；再翻 180°（亮色在右上）⇒ 188。
        //    判据全文 → `Deck/DeckRuntime.cs` 的 `BuildAreaBackground`。
        public static readonly Color BgGradC1 = new Color(0.2235294f, 0.0117647f, 0.0196078f, 1f);
        public static readonly Color BgGradC2 = new Color(0.0470588f, 0f, 0.0156863f, 1f);
        public const float BgGradAngle = 188f;

        // 页签栏底板 / 阴影（`Image` MB 实读）
        public const string ArtTabBg = "40k_main_tab_background";   // 185×4 Simple（prefab 就是拉伸它）
        public const string ArtTabShadow = "40k_main_tab_shadow";   // 51×4 Simple
        public const string ArtLine = "40k_main_line";              // 171×6 九宫 80,0,80,0（模板页分隔线）
        public const string ArtTabHi = "40k_main_bt_selected_BW";   // 71×71 九宫 30,30,30,30 · Sliced · ppuMul 0.92
        public const string ArtTabIcon = "40K_shop_bt_boosters";    // 126×126 preserveAspect（**母版**那一颗的出厂图）
        public const string ArtTabName = "40k_main_bt_nametag";     // 109×41
        public const string ArtBadge = "40K_notification_number";   // 65×65 preserveAspect
        public static readonly Color TabShadowTint = new Color(0f, 0f, 0f, 0.4745098f);  // MB 实读
        public static readonly Color TabHiTint = new Color(1f, 0f, 0f, 1f);              // MB 实读
        public static readonly Color TabBadgeTint = new Color(0.7358490f, 0.7358490f, 0.7358490f, 1f);
        public const float TabHiCorner = 30f / 0.92f;               // 角块 = m_Border ÷ ppuMul = 32.6087

        // 页签按钮（= `Menu Navigation Panel Button` prefab 的几何 · VLG top padding 120 ⇒ 第 1 颗 y 190.9）
        public const float TabW = 165f, TabH = 180f, TabTop = 190.9f;
        public static readonly PxRect TabNameR = new PxRect(172.2f, 315.2f, 327.2f, 353.1f);        // 155×37.86
        public static readonly PxRect TabBadgeR = new PxRect(283.9f, 215.5f, 318.9f, 250.5f);       // 35×35
        public static readonly PxRect TabBadgeTextR = new PxRect(283.9f, 216.5f, 318.9f, 251.5f);   // 35×35
        public const float TabLabelFont = 25.65f, TabLabelBase = 23f, TabLabelMin = 12f, TabLabelMax = 33f;
        public const float TabLabelSpacing = -3.5f;
        public static readonly Color TabLabelInk = new Color(0.9568628f, 0.8823530f, 0.6745098f, 1f);
        public const string MasterLabel = "Booster Packs";          // 母版出厂字（TMP MB 实读）

        // 页签栏 `Gradient2`（MB `-6105840469599813308` 实读；见文件头「近似」）
        public static readonly Color TabGradBottom = new Color(0f, 0f, 0f, 1f);
        public static readonly Color TabGradTop = new Color(0.2830189f, 0.2830189f, 0.2830189f, 0f);
        // 🆕 **2026-10-17（`N53①`）**：`Gradient2` 的 4 个色标（时间归一化 + RGB 原样；alpha 恒 1）。
        public static readonly float[] TabGradT = { 0f, 21974f / 65535f, 47995f / 65535f, 1f };
        public static readonly Color[] TabGradC =
        {
            new Color(0f, 0f, 0f, 1f),
            new Color(1f, 1f, 1f, 1f),
            new Color(1f, 1f, 1f, 1f),
            new Color(0.2830189f, 0.2830189f, 0.2830189f, 1f),
        };

        // ============================================================ 两页（tabs 的 2 个 PPtr）
        public enum Page { ChooseSide = 0, OnGoing = 1 }
        public const int PageCount = 2;
        /// <summary>页节点名（= 原版 prefab 里那两颗 tab 的 GameObject 名；同时是页签文字的本地兜底）。</summary>
        public static readonly string[] PageNames = { "Select Team Tab", "Event On Going Tab" };
        /// <summary>页签文字的来源说明（自检读口 / 出声用）。
        /// 🆕 **2026-10-19（`A1853②`）订正**：原来这里写「原版来自 `LiveOpsAssetUtility.GetLabel(事件)`（远端）⇒
        /// 本地拿不到」—— **那一半已过期**：`NavigationBarTitle`（label 4 = `Event_Description/ExpansionPass/TabTitle`）
        /// 就在本地 13 个活动 SO 里。现行取值次序 = **显式 `TabLabelTerms` > 本地活动目录（在期事件）> 节点名兜底**；
        /// 本窗是 Two Sides 事件（**不在**那 13 份 SO 里）⇒ 今天仍落到节点名，⛔ 不编活动文案。</summary>
        public const string TabLabelNote =
            "页签文字：显式 `TabLabelTerms` > 本地活动目录（`LiveOpsLocalCatalog`，label 4，只认在期事件）> 节点名兜底"
            + "（`Select Team Tab` / `Event On Going Tab`）。⛔ 不编活动文案、⛔ 不拿过期活动顶。";

        /// <summary>🆕 **2026-10-19（`A1853②`）**：本窗对应的**本地活动实例 id**（13 份活动 SO 之一）。
        /// 缺省 null = 不查本地目录（本窗建的是 Two Sides 事件，不在那 13 份里）。
        /// 声明后：页签文字取该事件的 `NavigationBarTitle`（label 4），**且只在该事件在期时可生效**。</summary>
        public string LocalLiveOpsEventId;

        /// <summary>🆕 **2026-10-19（`A1853②`）**：显式页签文字（null/空 ⇒ 走本地目录 ⇒ 再兜底节点名）。
        /// 调用方拿到活动实例的 `NavigationBarTitle` 时传进来 —— 与 `LocalLiveOpsEventId` 两条路等价，
        /// 优先级更高（调用方说了算）。</summary>
        public string[] TabLabelTerms;

        // ============================================================ 🆕 2026-10-23：事件钮（`SetupTabs` 那一族）
        /// <summary>本机四扇活动窗（= 原版 `LiveOpsManager.GetAllEvents` 结果里会出现的四种活动形态）。
        /// <para>值 = 「`SetupTabs` 建出来的一颗事件钮指向哪扇窗」；`EventWindowRootNames` 逐值同序
        /// （= 各窗 `Create()` 里那个根名，两处同源）。</para></summary>
        public enum EventWindowKind { Parent = 0, Gacha = 1, Raid = 2, ExpansionPass = 3 }

        /// <summary>四扇活动窗的**根名**（= prefab 根名 / `Create()` 用的名字；顺序 = <see cref="EventWindowKind"/>）。</summary>
        public static readonly string[] EventWindowRootNames =
        {
            RootName,                                   // "Parent Event Window"
            GachaEventWindow.RootName,                  // "Gacha Tab"
            RaidEventWindow.RootName,                   // "Raid Progress Tab"
            ExpansionPassEventWindow.RootName,          // "Expansion Pass Tab"
        };

        /// <summary>一颗「事件钮」= 原版 `TabButtons.AddTabButton(label, sprite)` 的一项 + 点它的去向。</summary>
        public sealed class EventTab
        {
            /// <summary>指向哪扇窗。</summary>
            public EventWindowKind Kind;
            /// <summary>页签文字（原版 `AddTabButton` 的 `label` 实参；**同时是节点名** —— `set_name(label)`）。</summary>
            public string Label;
            /// <summary>页签图标（原版 `Image.sprite = LoadAsset(ev,0)`；本机没有可加载的 liveops sprite ⇒ null）。</summary>
            public Texture2D Icon;
            /// <summary>本地活动实例 id（`LiveOpsLocalCatalog` 的在期事件）；**null = 本机没有那个实例**（如实记）。</summary>
            public string EventId;
            /// <summary>这一项凭什么出现（自检读口 / 出声用）。</summary>
            public string Note;
            /// <summary>点它的去向（原版 `b__2` = `WindowsManager.OpenWindow(...)`）。</summary>
            public System.Action Open;
        }

        // ---- 第 1 页（`TwoSidesEvent_ChooseSideTab`）矩形（D_ dump 的 derived 树逐行实读）----
        public static readonly PxRect P1ScanLinesR = new PxRect(615.9f, 148.0f, 1304.1f, 888.1f);
        public static readonly PxRect P1GlowR = new PxRect(764.2f, 378.0f, 1155.8f, 778.0f);
        public static readonly PxRect P1TeamAR = new PxRect(379.5f, 164.0f, 914.5f, 901.8f);
        public static readonly PxRect P1TeamBR = new PxRect(1025.5f, 164.0f, 1560.5f, 901.8f);
        public static readonly PxRect P1TeamANoiseR = new PxRect(332.7f, 101.3f, 1033.3f, 901.8f);
        public static readonly PxRect P1TeamBNoiseR = new PxRect(978.7f, 101.3f, 1679.3f, 901.8f);
        public static readonly PxRect P1TeamAGlowR = new PxRect(280.6f, 166.4f, 1013.4f, 899.2f);
        public static readonly PxRect P1TeamBGlowR = new PxRect(926.6f, 166.4f, 1659.4f, 899.2f);
        public static readonly PxRect P1TeamABottomTextR = new PxRect(381.6f, 793.4f, 912.4f, 899.3f);
        public static readonly PxRect P1TeamBBottomTextR = new PxRect(1027.6f, 793.4f, 1558.4f, 899.3f);
        public static readonly PxRect P1TeamANameR = new PxRect(360.7f, 752.3f, 890.0f, 830.1f);
        public static readonly PxRect P1TeamBNameR = new PxRect(1006.7f, 752.3f, 1536.0f, 830.1f);
        public static readonly PxRect P1TeamATapR = new PxRect(355.2f, 834.2f, 879.8f, 889.9f);
        public static readonly PxRect P1TeamBTapR = new PxRect(1001.2f, 834.2f, 1525.8f, 889.9f);
        public static readonly PxRect P1TeamATopR = new PxRect(404.0f, 164.0f, 937.2f, 264.0f);
        public static readonly PxRect P1TeamBTopR = new PxRect(1050.0f, 164.0f, 1583.2f, 264.0f);
        public static readonly PxRect P1TeamACaptainR = new PxRect(423.6f, 175.1f, 928.5f, 252.9f);
        public static readonly PxRect P1TeamBCaptainR = new PxRect(1069.6f, 175.1f, 1574.5f, 252.9f);
        public static readonly PxRect P1TitleBgR = new PxRect(465.8f, 31.0f, 1454.2f, 131.0f);
        public static readonly PxRect P1TitleR = new PxRect(291.16f, 31.0f, 1628.84f, 131.0f);
        public static readonly PxRect P1VsR = new PxRect(837.7f, 432.7f, 1082.3f, 685.3f);
        public static readonly PxRect P1DescR = new PxRect(160.8f, 921.6f, 1759.2f, 1068.4f);
        public static readonly PxRect P1BackR = new PxRect(35.0f, 995.8f, 99.2f, 1059.0f);
        public static readonly PxRect P1BackIconR = new PxRect(43.4f, 1004.0f, 90.8f, 1050.7f);
        public static readonly PxRect P1BackTextR = new PxRect(108.3f, 995.8f, 413.6f, 1059.0f);

        public const string ArtGlow = "Glow_UI_W40K";                 // 123×123
        public const string ArtNoise = "UI_Dirt_And_Noise_skratches"; // 1024×1024
        public const string ArtBottomGlow = "Smooth_background_lateral"; // 32×32 九宫 3,0,3,0
        public const string ArtBorder = "40k_square_border";          // 64×64 九宫 13,13,13,13
        public const string ArtVs = "VS_icon";                        // 369×381 —— 源图在本地（名字带空格），走 `sync_battle_ui_art.py` 导入
        public const string ArtBackRound = "UI_Button_Round_background";
        public const string ArtBackIcon = "40k_UI_bt_back";

        /// <summary>`Two Side Event Select Team A/Background` 的 `UICornersGradient`（MB 实读，四角色原样）。</summary>
        public static readonly Color TeamABl = new Color(0.5471698f, 0f, 0f, 1f);
        public static readonly Color TeamABr = new Color(0.4339623f, 0f, 0f, 1f);
        public static readonly Color TeamATr = new Color(0.7372549f, 0.1408424f, 0f, 1f);
        public static readonly Color TeamATl = new Color(0.8679245f, 0.2744284f, 0.1760413f, 1f);
        public static readonly Color TeamBBl = new Color(0f, 0.2371225f, 0.5490196f, 1f);
        public static readonly Color TeamBBr = new Color(0f, 0.0694290f, 0.4352941f, 1f);
        public static readonly Color TeamBTr = new Color(0f, 0.3001419f, 0.7372549f, 1f);
        public static readonly Color TeamBTl = new Color(0.1764706f, 0.5408802f, 0.8666667f, 1f);
        public static readonly Color TeamAGlowTint = new Color(0.4433962f, 0f, 0f, 1f);
        public static readonly Color TeamBGlowTint = new Color(0f, 0.1387351f, 0.4431373f, 1f);
        public static readonly Color TeamNoiseTint = new Color(0f, 0f, 0f, 0.5176471f);
        public static readonly Color TeamBorderTint = new Color(0.3411765f, 0.3411765f, 0.3411765f, 1f);

        public const string P1TitleText = "BATTLE FOR WARPFORGE";
        public const string P1DescText =
            "<color=#E27E1B>Choose your side. Fight for glory!</color> Score points for your team "
            + "and tip the balance of war. Only one shall triumph!";
        public const string P1TeamName = "Red Team";     // prefab 出厂原文（两颗都是 Red Team）
        public const string P1Captain = "#squidmar";
        public const string P1TapText = "Select";
        public static readonly Color P1TapInk = new Color(1f, 0.656f, 0.656f, 1f);
        public const float P1TitleFont = 72f, P1TitleBase = 36f, P1TitleMin = 18f, P1TitleMax = 72f, P1TitleSpacing = 5f;
        public const float P1DescFont = 48f, P1DescBase = 36f, P1DescMin = 18f, P1DescMax = 48f;
        public const float P1TeamFont = 82.1f, P1TeamBase = 36f, P1TeamMin = 18f, P1TeamMax = 86.4f;
        public const float P1TapFont = 46.74f, P1TapMin = 18f, P1TapMax = 46.74f;
        public const float P1BackFont = 45f, P1BackBase = 31.9f, P1BackMin = 1f, P1BackMax = 45f;

        // ---- 第 2 页（`TwoSidesEvent_OnGoingTab`）矩形 ----
        public static readonly PxRect P2RedBgR = new PxRect(0f, 93.6f, 1920f, 1015.4f);   // 子件导出值（父 rect 退化，见文件头）
        public static readonly PxRect P2VignetteR = new PxRect(-0f, -28.2f, 1920f, 1082.3f);
        public static readonly PxRect P2HeaderR = new PxRect(0f, 40.9f, 550f, 150.4f);
        public static readonly PxRect P2HeaderBgR = new PxRect(0f, 40.9f, 690.9f, 156.2f);
        public static readonly PxRect P2HeaderBg1R = new PxRect(-462.1f, 40.9f, 87.9f, 156.2f);
        public static readonly PxRect P2HeaderBackR = new PxRect(-24.4f, 42.9f, 143.5f, 154.2f);
        public static readonly PxRect P2TitleR = new PxRect(155.0f, 57.2f, 524.4f, 139.9f);
        public static readonly PxRect P2GameModeIconR = new PxRect(529.9f, 48.5f, 629.9f, 148.5f);
        public static readonly PxRect P2InfoTriggerR = new PxRect(542.1f, 62.7f, 617.6f, 134.4f);
        public static readonly PxRect P2BattleBgR = new PxRect(-134.8f, 145.4f, 1610.8f, 1013.1f);
        public static readonly PxRect P2TeamAR = new PxRect(-122.8f, 174.8f, 608.8f, 1045.9f);
        public static readonly PxRect P2TeamBR = new PxRect(795.9f, 174.8f, 1480.1f, 1045.9f);
        public static readonly PxRect P2TeamAImageR = new PxRect(-215.6f, 121.7f, 671.6f, 1008.9f);
        public static readonly PxRect P2TeamBImageR = new PxRect(635.9f, 121.7f, 1523.2f, 1008.9f);
        public static readonly PxRect P2TeamACaptainR = new PxRect(69.6f, 705.9f, 598.9f, 783.8f);
        public static readonly PxRect P2TeamBCaptainR = new PxRect(771.2f, 706.9f, 1300.5f, 784.8f);
        public static readonly PxRect P2RewardPanelR = new PxRect(1352f, 133.9f, 1990f, 946.1f);
        public static readonly PxRect P2ScoreBarR = new PxRect(1507.7f, 454.9f, 1794.3f, 829.3f);
        public static readonly PxRect P2ProgressR = new PxRect(1603.3f, 381.9f, 1698.7f, 902.7f);
        public static readonly PxRect P2ProgressBgR = new PxRect(1630.2f, 415.9f, 1671.8f, 868.7f);
        public static readonly PxRect P2CollectR = new PxRect(1507.4f, 889.8f, 1794.6f, 976.3f);
        public static readonly PxRect P2CollectTextR = new PxRect(1517.4f, 898.2f, 1784.7f, 967.8f);
        public static readonly PxRect P2RewardTileR = new PxRect(1390f, 151.3f, 1912f, 205.9f);
        public static readonly PxRect P2RewardHelpR = new PxRect(1374.6f, 205.9f, 1904.6f, 311.5f);
        public static readonly PxRect P2VictoriesR = new PxRect(1373.6f, 303.9f, 1920.2f, 394.4f);
        public static readonly PxRect P2VictoriesTitleR = new PxRect(1367.2f, 324.1f, 1603.8f, 374.1f);
        public static readonly PxRect P2VictoriesSkullR = new PxRect(1610.2f, 312.5f, 1683.6f, 385.8f);
        public static readonly PxRect P2VictoriesTotalR = new PxRect(1690.8f, 324.1f, 1871.6f, 374.1f);
        public static readonly PxRect P2ScoringSliderR = new PxRect(58.3f, 745.3f, 1288.6f, 918.7f);
        public static readonly PxRect P2ScoringBgR = new PxRect(58.3f, 788.7f, 1288.6f, 875.3f);
        public static readonly PxRect P2CenterMarkR = new PxRect(670.1f, 788.7f, 676.8f, 875.3f);
        public static readonly PxRect P2YourTeamAR = new PxRect(49.1f, 875.7f, 350.9f, 932.8f);
        public static readonly PxRect P2YourTeamBR = new PxRect(971.1f, 875.7f, 1272.9f, 932.8f);
        public static readonly PxRect P2TeamANameRowR = new PxRect(71.7f, 793.1f, 653.1f, 870.9f);
        public static readonly PxRect P2TeamBNameRowR = new PxRect(691.3f, 793.1f, 1272.7f, 870.9f);
        public static readonly PxRect P2TeamABonusR = new PxRect(71.7f, 782.0f, 171.7f, 882.0f);
        public static readonly PxRect P2TeamBBonusR = new PxRect(1187.6f, 782.0f, 1287.6f, 882.0f);
        public static readonly PxRect P2TeamANameR = new PxRect(171.7f, 793.1f, 668.0f, 870.9f);
        public static readonly PxRect P2TeamBNameR = new PxRect(691.3f, 793.1f, 1187.6f, 870.9f);
        public static readonly PxRect P2SkullSdfAR = new PxRect(46.4f, 765.4f, 194.2f, 902.8f);
        public static readonly PxRect P2SkullAR = new PxRect(83.7f, 794.9f, 159.0f, 870.1f);
        public static readonly PxRect P2SkullSdfBR = new PxRect(1162.2f, 765.4f, 1310.1f, 902.8f);
        public static readonly PxRect P2SkullBR = new PxRect(1199.6f, 794.9f, 1274.8f, 870.1f);
        public static readonly PxRect P2TimerR = new PxRect(540.6f, 921.1f, 806.3f, 946.1f);
        public static readonly PxRect P2TimerTextR = new PxRect(540.6f, 921.1f, 732.2f, 956.7f);
        public static readonly PxRect P2ClockIconR = new PxRect(540.6f, 921.1f, 571.6f, 952.1f);

        public const string ArtRedBg = "40k_general_popup_simple_red";   // 21×81 九宫 0,26,0,26
        public const string ArtCampaignHeader = "WF_Campaign_Info_Background"; // 740×167 九宫 335,0,395,0
        public const string ArtMenuBack = "UI_Button_Menu_Back";         // 187×124
        public const string ArtMulliganBtn = "UI_Button_Mulligan";       // 410×124 九宫 333,96,333,96
        public const string ArtMiniBar = "MiniBar_01";                   // 59×648
        public const string ArtSkull = "40k_battle_Win_Skull";           // 66×73
        public const string ArtSkullSdf = "40k_battle_Win_Skull_SDF";    // 128×128 —— 源图在本地（名字带空格），走 `sync_battle_ui_art.py` 导入
        public const string ArtInfo = "40K_generic_bt_info";             // 128×128
        public const string ArtClock = "WF_icon_clock";                  // 表头那颗时钟（31×31）
        public static readonly string[] CrateArt =
        {
            "40k_Crate_Tier1_Iron", "40k_Crate_Tier2_Copper", "40k_Crate_Tier3_Silver",
            "40k_Crate_Tier4_Gold", "40k_Crate_Tier5_Warp",
        };
        public static readonly Color NoiseTint = new Color(0.3113208f, 0.1271798f, 0f, 0.7176471f);
        public static readonly Color VignetteTint = new Color(0f, 0f, 0f, 0.5803922f);
        public static readonly Color ScoringBgTint = new Color(0.206f, 0.319f, 0.792f, 1f);
        public static readonly Color CenterMarkTint = new Color(0f, 0f, 0f, 0.165f);

        public const string P2HeaderTitle = "Game mode";          // prefab 出厂字（运行期由 liveop label 覆盖）
        public const string P2TeamName = "Blue Team";             // prefab 出厂字
        public const string P2Captain = "#squidmar";
        public const string P2RewardTile = "Progression";
        public const string P2RewardHelp = "Get skulls to progress in the event and unlock rewards.";
        public const string P2CollectText = "Collect";
        public const string P2VictoriesTitle = "Skulls:";
        public const string P2VictoriesTotal = "125";
        public const string P2ScoreText = "1256";                 // 5 行 `Score` 的出厂字
        public const float P2HeaderFont = 67.55f, P2HeaderBase = 36f, P2HeaderMin = 18f, P2HeaderMax = 67.55f;
        public const float P2TileFont = 45.87f, P2TileMin = 18f, P2TileMax = 45.87f;
        public const float P2HelpFont = 38f, P2HelpMin = 18f, P2HelpMax = 38f;
        public const float P2CollectFont = 55f, P2CollectBase = 12f, P2CollectMin = 10f, P2CollectMax = 55f;
        public const float P2VictoriesFont = 48f, P2VictoriesMin = 18f, P2VictoriesMax = 48f;
        public const float P2TeamFont = 82.1f, P2TeamMin = 18f, P2TeamMax = 86.4f;
        public const float P2YourTeamFont = 58f, P2YourTeamMin = 18f, P2YourTeamMax = 58f;
        public const float P2TimerFont = 32f, P2TimerBase = 30f, P2TimerMin = 10f, P2TimerMax = 32f;
        /// <summary>5 档记分行的纵向位置（dump 逐行实读；`Score Levels` 是 `reverse=1` 的 VLG）。</summary>
        public static readonly float[] ScoreRowTop = { 718.9f, 637.5f, 556.2f, 474.8f, 393.4f };
        public const float ScoreRowW = 371.38f, ScoreRowH = 75.98f;

        // ============================================================ 状态 / 自检读口
        public static ParentEventWindow LastOpened { get; private set; }

        Transform _shade, _contentArea, _background, _tabButtons, _tabShadow, _tabs, _shadow1, _sampleTab;
        Transform _tabMaster, _page0, _page1;
        readonly List<Transform> _tabButtonsList = new List<Transform>();
        readonly List<Transform> _tabHi = new List<Transform>();
        readonly List<Label> _tabLabels = new List<Label>();
        readonly List<Transform> _pages = new List<Transform>();
        // 🆕 2026-10-23：事件钮（`SetupTabs`）—— **与内容页那三张表分家**（见 `SetupTabs` 的 doc）
        readonly List<Transform> _eventTabs = new List<Transform>();
        readonly List<Transform> _eventHi = new List<Transform>();
        readonly List<Label> _eventLabels = new List<Label>();
        readonly List<Label> _p1TapLabels = new List<Label>();
        readonly List<Label> _p2ScoreLabels = new List<Label>();
        Label _p1Title, _p2HeaderTitle, _p2RewardTile, _p2Collect, _p2TotalVictories, _p2Timer;
        Transform _p1Back, _p2Back, _p2CollectBtn, _scoreLevels;
        bool _warned;
        bool _tabOverflowWarned;
        readonly List<string> _missArt = new List<string>();

        /// <summary>建不出来的图（非空 = 有件没画；自检读口）。</summary>
        public List<string> MissingArt { get { return _missArt; } }
        /// <summary>活动数据本地没有（恒 true —— 见文件头的数据缺口）。</summary>
        public bool DataMissing { get { return true; } }
        /// <summary>数据缺口那句话（自检读口）。</summary>
        public string DataNote
        {
            get
            {
                return "活动数据全在远端 LiveOps（两个 tab 的标题词条/图标、队伍名/队长/分数/里程碑/奖励、"
                     + "倒计时截止时刻、`GetTimeDescriptionTextKey` 的「Ends in:」）—— 本地离线包体里没有"
                     + "（`官方本地化_en+zh-CN.json` 9,441 条 0 命中 `Two Sides` / `Choose your side`）"
                     + " ⇒ 照 prefab 出厂原文画 + 页签文字用节点名兜底，⛔ 不编活动文案。";
            }
        }

        public Transform ShadeNode { get { return _shade; } }
        public Transform ContentAreaNode { get { return _contentArea; } }
        public Transform BackgroundNode { get { return _background; } }
        public Transform TabButtonsNode { get { return _tabButtons; } }
        public Transform TabShadowNode { get { return _tabShadow; } }
        public Transform TabsNode { get { return _tabs; } }
        public Transform Shadow1Node { get { return _shadow1; } }
        /// <summary>`Sample Tab`（= 原版 `tabPrefab` 那颗模板；建出来、**关着**）。</summary>
        public Transform SampleTabNode { get { return _sampleTab; } }
        public Transform TabMasterNode { get { return _tabMaster; } }
        /// <summary>运行期建出来的两颗页签（不含母版）。</summary>
        public List<Transform> TabButtonNodes { get { return _tabButtonsList; } }
        /// <summary>两颗页签的选中高亮（兄弟序同 <see cref="TabButtonNodes"/>）。</summary>
        public List<Transform> TabHighlights { get { return _tabHi; } }
        public List<Label> TabLabels { get { return _tabLabels; } }
        /// <summary>🆕 2026-10-23：**事件钮**（`SetupTabs` 建的那几颗；**不含**内容页签，兄弟序 = 事件名单序）。</summary>
        public List<Transform> EventTabNodes { get { return _eventTabs; } }
        public List<Transform> EventTabHighlights { get { return _eventHi; } }
        public List<Label> EventTabLabels { get { return _eventLabels; } }
        /// <summary>`SetupTabs` 那一声出声的原文（自检读口 / 人看着核名单用）。</summary>
        public string EventTabNote { get; private set; }
        /// <summary>两页节点（兄弟序 = `tabs` 顺序）。</summary>
        public List<Transform> PageNodes { get { return _pages; } }
        public Transform ChooseSidePage { get { return _page0; } }
        public Transform OnGoingPage { get { return _page1; } }
        public Label P1TitleLabel { get { return _p1Title; } }
        public Label P2HeaderTitleLabel { get { return _p2HeaderTitle; } }
        public Label P2RewardTileLabel { get { return _p2RewardTile; } }
        public Label P2CollectLabel { get { return _p2Collect; } }
        public Label P2TotalVictoriesLabel { get { return _p2TotalVictories; } }
        public Label P2TimerLabel { get { return _p2Timer; } }
        public List<Label> P2ScoreLabels { get { return _p2ScoreLabels; } }
        public List<Label> P1TapLabels { get { return _p1TapLabels; } }
        public Transform P1BackNode { get { return _p1Back; } }
        public Transform P2BackNode { get { return _p2Back; } }
        public Transform P2CollectNode { get { return _p2CollectBtn; } }
        public Transform ScoreLevelsNode { get { return _scoreLevels; } }
        public int CurrentPageIndex { get; private set; }
        public string CurrentPageName
        {
            get { return CurrentPageIndex >= 0 && CurrentPageIndex < _pages.Count ? _pages[CurrentPageIndex].name : null; }
        }

        // ============================================================ 开
        /// <summary>建一扇（`WindowsManager.OpenParentEvent` 的 `create` 那一跳）。</summary>
        public static ParentEventWindow Create(WindowsManager mgr, Page page = Page.ChooseSide)
        {
            var go = new GameObject(RootName);                       // 节点名照 prefab（`OpenByRef` 的键同源）
            var win = go.AddComponent<ParentEventWindow>();
            win.type = WindowType.Fullscreen;                        // MB 实读 `type = 0`
            win.placement = WindowsPlacement.Popup;                  // MB 实读 `windowsPlacement = 15`
            win.closeOnEsc = ParamsCloseOnEsc;                       // MB 实读 `closeOnESC = 0`（Two Sides 那份）
            win.updateNavPanel = ParamsUpdateNavPanel;               // MB 实读 `updateNavPanel = 1`（基座 `Parent Event Window` 那份；见 `ParamsUpdateNavPanel` 的注释）
            win.extraScaleSmallScreen = ParamsExtraScale;            // MB 实读 1.0（= 不覆盖）
            win.useDefaultCloseSoundIfNull = ParamsUseDefaultCloseSound;
            // `openSound` / `closeSound` 实读都是空 PPtr ⇒ 一个 clip 都不赋，关窗音走家族默认。
            win.Manager = mgr;
            win._initialPage = (int)page;
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[ParentEvent] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        int _initialPage;

        public override void Open()
        {
            LastOpened = this;
            Build();
            int page = (_initialPage >= 0 && _initialPage < PageCount) ? _initialPage : 0;
            // 原版 `CheckAndOpenTab` 按「玩家是否已选阵营」决定落在哪页；那份数据在远端 ⇒ 默认第 1 页并出声。
            SwitchPage(page);
            WarnOnce();
        }

        void WarnOnce()
        {
            if (_warned) return;
            _warned = true;
            Debug.LogWarning("[ParentEvent] " + DataNote);
            Debug.LogWarning("[ParentEvent] " + TabLabelNote);
            Debug.LogWarning("[ParentEvent] 页签**图标**留空：原版由 `TabButtons.AddTabButton(..., sprite)` 传入"
                           + "（liveops 资产里的 sprite），本地没有 ⇒ ⛔ 不用母版的 `40K_shop_bt_boosters` 冒充。");
            Debug.Log("[ParentEvent] 页签选中态：原版是 `EverguildToggle.m_IsOn` 驱动 on/off 色与 sprite；"
                    + "本件 = 只显示当前页那颗的 `Highlight`（红 `40k_main_bt_selected_BW`，MB 实读 `m_Color=(1,0,0,1)`）。");
        }

        // ============================================================ 建整棵树（顺序 = 原版兄弟序）
        public void Build()
        {
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            _tabButtonsList.Clear(); _tabHi.Clear(); _tabLabels.Clear(); _pages.Clear();
            ClearEventTabs(); _tabOverflowWarned = false;
            _p1TapLabels.Clear(); _p2ScoreLabels.Clear();
            _p1Title = _p2HeaderTitle = _p2RewardTile = _p2Collect = _p2TotalVictories = _p2Timer = null;
            _p1Back = _p2Back = _p2CollectBtn = _scoreLevels = null;

            // ---- 0) `Menu Dark Background`（整屏压暗 + 「点暗底关窗」）----
            //   原版 `TwoSidesEventWindow.OnEnable` 给 `backgroundCloseButton.onClick` 挂的监听与表头返回钮是
            //   **同一个 delegate** ⇒ 点压暗层 = 关窗（判据 → 文件头 ④）。
            _shade = MenuDraw.Node(transform, "Menu Dark Background", ShadeR);
            MenuDraw.Rect(_shade, CardArt.Solid(), ShadeR, "Image", QShade, ShadeTint);
            MenuDraw.ShadeHit(_shade, ShadeR, QShade, QHit, () => Close(), "BackgroundHit");

            // ---- 1) `Content Area`（167.2,70.9 → 1920,1080）----
            _contentArea = MenuDraw.Node(transform, "Content Area", ContentAreaR);

            // ---- 2) `Background`：`Image`(sprite=0) + `UIGradient`（纯代码渐变，不是图）----
            //   `Image.m_Type = 1`(Sliced) / ppuMul 0.01 在**没有 sprite** 时不影响观感；颜色全来自渐变。
            _background = MenuDraw.Node(_contentArea, "Background", ContentAreaR);
            var bgTex = CardArt.Gradient(BgGradC1, BgGradC2, BgGradAngle);
            MenuDraw.Rect(_background, bgTex, ContentAreaR, "Image", QContentBg, Color.white);

            // ---- 3) `Tab Buttons`（165×1009.06 · 40k_main_tab_background · VLG spacing0 align1 pad(0,0,120,0)）----
            _tabButtons = MenuDraw.Node(_contentArea, "Tab Buttons", TabButtonsR);
            // `Gradient2` 四段（判据/alpha 语义 → 文件头）。⚠️ 底色图的 UV 也按同一分段切。
            MenuDraw.GradientV(_tabButtons, Tex(ArtTabBg, "页签栏底板"), TabButtonsR, QPanel, "Image",
                               TabGradT, TabGradC);

            // `Shadow`（Tab Buttons 的子件；47.64×1009.06 · 40k_main_tab_shadow · (0,0,0,0.4745)）
            _tabShadow = MenuDraw.Node(_tabButtons, "Shadow", TabShadowR);
            MenuDraw.Rect(_tabShadow, Tex(ArtTabShadow, "页签栏阴影"), TabShadowR, "Image", QTabShadow, TabShadowTint);

            // 母版（原版 `TabButtons.Initialize` 第一件事就是 `tabButtonPrefab.SetActive(false)`）
            _tabMaster = BuildTabButton("Menu Navigation Panel Button", TabTop, MasterLabel, true);
            _tabMaster.gameObject.SetActive(false);

            // 两颗运行期页签（= `AddTabButton` 的等价物）—— 文字次序见 `TabLabelTerm`、图标留空（数据缺口）
            for (int i = 0; i < PageCount; i++)
            {
                var t = BuildTabButton(PageNames[i], TabTop + TabH * i, TabLabelTerm(i), false);
                int idx = i;
                _tabButtonsList.Add(t);
                // 🆕 **2026-10-18（音效线缺口 `AUD` §三 C9）**：原版页签是 `EverguildToggle` +
                //   **`allowClickWhenAlreadySelected = 0`** ⇒ 再点当前页时连按钮音一起跳过。
                var tabHit = MenuDraw.Hit(t, "Hit", new PxRect(TabButtonsR.x1, TabTop + TabH * i, TabButtonsR.x2,
                                                  TabTop + TabH * (i + 1)),
                             QHit, () => SwitchPage(idx), null, null, null, null, null,
                             clickCue: WindowSounds.DefaultButtonCue);
                var tabWb = tabHit != null ? tabHit.GetComponent<WindowButton>() : null;
                if (tabWb != null)
                {
                    tabWb.allowClickWhenAlreadySelected = false;
                    tabWb.isOn = () => CurrentPageIndex == idx;
                }
            }

            // 页签栏底板 / 压暗层之间的吸收：`Tab Buttons` 的 `Image.m_RaycastTarget = 1`、父链上没有点击处理器
            // ⇒ 原版点这里**什么都不做**（⛔ 别让它冒到压暗层去关窗）。
            MenuDraw.Absorb(_contentArea, "AbsorbHit", ContentAreaR, QShade, QHit);

            // ---- 4) `Tabs`（两页的容器；先放**模板页** `Sample Tab`，再放两页）----
            _tabs = MenuDraw.Node(_contentArea, "Tabs", TabsR);
            // `Sample Tab` = 原版 `TabbedWindowComponents.tabPrefab`（GO `1358808318654906692`）指向的那颗模板：
            //   布局组**空页**（`Header` + `NonDrawingGraphic` + 一条 `Separator Line`），运行期由
            //   `GameWindowWithTabs.CreateTab` 克隆；本窗两页是**预置**的（Two Sides 那份）⇒ 模板只建、**关着**。
            var sample = MenuDraw.Node(_tabs, "Sample Tab", Scr(new PxRect(167.2f, 70.9f, 1920f, 1080f)));
            var sampleHeader = MenuDraw.Node(sample, "Header", Scr(new PxRect(167.2f, 70.9f, 1920f, 155.9f)));
            MenuDraw.Nine(sampleHeader, Tex(ArtLine, "模板页的分隔线"), Scr(new PxRect(330.7f, 150.9f, 1920f, 160.9f)),
                          new Vector4(80f, 0f, 80f, 0f), 171f, 6f, QPageArt1, null, true, "Separator Line",
                          new Vector4(80f / 0.75f, 0f, 80f / 0.75f, 0f));
            sample.gameObject.SetActive(false);
            _sampleTab = sample;
            _page0 = MenuDraw.Node(_tabs, PageNames[0], Scr(new PxRect(0f, 0f, 1920f, 1080f)));
            BuildChooseSidePage(_page0);
            _page1 = MenuDraw.Node(_tabs, PageNames[1], Scr(new PxRect(0f, 0f, 1920f, 1080f)));
            BuildOnGoingPage(_page1);
            _pages.Add(_page0);
            _pages.Add(_page1);

            // ---- 5) `Shadow (1)`（**出厂 INACT**；dump 逐行实读）----
            _shadow1 = MenuDraw.Node(_contentArea, "Shadow (1)", Shadow1R);
            MenuDraw.Rect(_shadow1, Tex(ArtTabShadow, "页签栏阴影（右侧那条）"), Shadow1R, "Image",
                          QTabShadow, TabShadowTint);
            _shadow1.gameObject.SetActive(false);

            // 🆕 2026-10-23：左栏的「事件钮」（原版 `BaseParentEventWindow.SetupTabs`）—— **建在最后**，
            //   兄弟序 = 内容页签在前、事件钮在后（原版 `AddTabButton` 里那句 `SetAsLastSibling()`）
            SetupTabs(BuildEventTabs(EventWindowKind.Parent, LiveOpsLocalCatalog.NowUtc));

            if (MissingArt.Count > 0)
                Debug.LogWarning("[ParentEvent] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        // ============================================================ 页签文字（= 12 承载面 #7）
        /// <summary>第 <paramref name="page"/> 个页签的文字。次序（**唯一判据**，别再各写一份）：
        /// <list type="number">
        ///  <item><see cref="TabLabelTerms"/> 显式给了 ⇒ 用它（调用方说了算）；</item>
        ///  <item><see cref="LocalLiveOpsEventId"/> 指向本地活动目录里的一个事件，**且该事件可选中、在期**
        ///        ⇒ 用它 `NavigationBarTitle`（label 4 = `Event_Description/ExpansionPass/TabTitle`）；</item>
        ///  <item>否则 ⇒ 页节点名兜底（`PageNames`，prefab 实读；= 今天的实际行为）。</item>
        /// </list>
        /// 🔴 **过期事件不参与**（用户边界「过期活动不做」）—— 第 2 步的 `IsInPeriod(NowUtc)` 就是那道闸。</summary>
        public string TabLabelTerm(int page)
        {
            if (TabLabelTerms != null && page >= 0 && page < TabLabelTerms.Length
                && !string.IsNullOrEmpty(TabLabelTerms[page]))
                return TabLabelTerms[page];
            var ev = LiveOpsLocalCatalog.Find(LocalLiveOpsEventId);
            if (ev != null && ev.Selectable && ev.IsInPeriod(LiveOpsLocalCatalog.NowUtc))
            {
                LiveOpsLocalLabel l;
                if (ev.TryGetLabel((int)LiveOpsLabelRef.NavigationBarTitle, out l))
                {
                    string txt = LiveOpsLocalCatalog.TermText(l.Term);
                    if (!string.IsNullOrEmpty(txt)) return txt;
                }
            }
            return (page >= 0 && page < PageNames.Length) ? PageNames[page] : "";
        }

        // ============================================================ 页签按钮（= `Menu Navigation Panel Button` 模板）
        Transform BuildTabButton(string name, float top, string label, bool isMaster)
        {
            var btn = MenuDraw.Node(_tabButtons, name, new PxRect(TabButtonsR.x1, top, TabButtonsR.x2, top + TabH));

            // `Highlight`：整颗按钮同矩形 · 40k_main_bt_selected_BW（九宫 30,30,30,30 · ppuMul 0.92 · tint 红）
            var hi = MenuDraw.Node(btn, "Highlight", new PxRect(TabButtonsR.x1, top, TabButtonsR.x2, top + TabH));
            var hiRoot = MenuDraw.Nine(hi, Tex(ArtTabHi, "页签选中高亮"), new PxRect(TabButtonsR.x1, top, TabButtonsR.x2, top + TabH),
                                       new Vector4(30f, 30f, 30f, 30f), 71f, 71f, QTabHi, TabHiTint, true, "Image",
                                       new Vector4(TabHiCorner, TabHiCorner, TabHiCorner, TabHiCorner));
            if (hiRoot == null) MenuDraw.Rect(hi, CardArt.Solid(), new PxRect(TabButtonsR.x1, top, TabButtonsR.x2, top + TabH),
                                              "Image", QTabHi, TabHiTint);
            if (!isMaster) _tabHi.Add(hi);
            hi.gameObject.SetActive(false);          // 由 `SwitchPage` 打开（原版 `m_IsOn` 的等价物）

            // `Icon`：整颗按钮同矩形 · preserveAspect —— ⚠️ **运行期图标 = liveops sprite，本地没有 ⇒ 不画**。
            var icon = MenuDraw.Node(btn, "Icon", new PxRect(TabButtonsR.x1, top, TabButtonsR.x2, top + TabH));
            if (isMaster)
            {
                // 母版那一颗是 prefab 的**出厂图**（MB 实读 `40K_shop_bt_boosters` 126×126 preserveAspect）
                MenuDraw.Rect(icon, Tex(ArtTabIcon, "母版页签图标"), new PxRect(TabButtonsR.x1, top, TabButtonsR.x2, top + TabH),
                              "Image", QTabIcon, Color.white, true);
            }

            // `Label`（名牌底板 155×37.86 · 40k_main_bt_nametag）+ `TabButtonLabel`
            // 🔴 三颗模板件的矩形是**母版那一颗（y 190.9）的屏幕绝对值** ⇒ 第 2 颗要整体下移 `TabH`。
            float dy = top - TabTop;
            var nameR = new PxRect(TabNameR.x1, TabNameR.y1 + dy, TabNameR.x2, TabNameR.y2 + dy);
            var labelNode = MenuDraw.Node(btn, "Label", nameR);
            MenuDraw.Rect(labelNode, Tex(ArtTabName, "页签名牌底板"), nameR, "Image", QTabName, Color.white);
            var lb = MenuDraw.TextBox(labelNode, nameR, label, TabLabelInk, "TabButtonLabel",
                                      TabLabelFont, TabLabelMin, QTabLabel, TabLabelMax, TabLabelBase, face: TmpFont.Face.LatinCardBody);
            if (lb != null)
            {
                lb.SetWrapping(false);                       // prefab `m_TextWrappingMode = 0`(NoWrap)
                MenuDraw.SetVAlign(lb, Label.VAlign.Midline, nameR);   // `m_VerticalAlignment = 4096`
                lb.SetCharSpacing(TabLabelSpacing);          // `m_characterSpacing = -3.5`
                if (!isMaster) _tabLabels.Add(lb);
            }

            // `Badge Highlight`（35×35 · 40K_notification_number · tint 0.7358 · preserveAspect）+ `OneText`
            var badgeR = new PxRect(TabBadgeR.x1, TabBadgeR.y1 + dy, TabBadgeR.x2, TabBadgeR.y2 + dy);
            var badgeTextR = new PxRect(TabBadgeTextR.x1, TabBadgeTextR.y1 + dy, TabBadgeTextR.x2, TabBadgeTextR.y2 + dy);
            var badge = MenuDraw.Node(btn, "Badge Highlight", badgeR);
            MenuDraw.Rect(badge, Tex(ArtBadge, "页签红点"), badgeR, "Image", QBadge, TabBadgeTint, true);
            var one = MenuDraw.TextBox(badge, badgeTextR, "", Color.white, "OneText",
                                       34f, 18f, QBadgeText, 34f, 36f, face: TmpFont.Face.LatinCardBody);   // fs34 / base36 / auto[18~34] / Center/Middle
            if (one != null) MenuDraw.SetVAlign(one, Label.VAlign.Middle, badgeTextR);
            // ⚠️ 红点计数来自通知系统（远端）⇒ prefab 出厂是空字、底板照建（同 prefab 出厂态）。

            return btn;
        }

        // ============================================================ 🆕 2026-10-23：事件钮（`SetupTabs`）
        // 判据全在文件头那一节（`BaseParentEventWindow__SetupTabs.c` / `TabButtons__AddTabButton.c` /
        // `BaseParentEventWindow.__c__DisplayClass4_0___SetupTabs_b__2.c` / `OpenEventButton__*.c`）。

        /// <summary>**建左栏的「事件钮」** —— 原版 `BaseParentEventWindow.SetupTabs` + `TabButtons.AddTabButton` 的等价物。
        /// <para>⚠️ 本方法建的是**事件钮**那一种（点了**开另一扇窗**）；本窗**自己的页**是另一种（`Build()` 里那两颗
        /// 内容页签）。兄弟序 = 内容页在前、事件钮在后（原版 `SetAsLastSibling`）。</para>
        /// <para>⚠️ **内容页那三张表不被污染**：`TabButtonNodes` / `TabHighlights` / `TabLabels` 仍只含内容页
        /// （`Editor/MainMenuScene.cs` 的 T28 断言按 2 颗数的）⇒ 事件钮走
        /// <see cref="EventTabNodes"/> / <see cref="EventTabHighlights"/> / <see cref="EventTabLabels"/>。</para>
        /// <para>**幂等**：再调一次会把上一轮的事件钮全拆掉重建（内容页签一颗都不动）。</para></summary>
        /// <returns>建出几颗（0 = 名单空）。</returns>
        public int SetupTabs(IList<EventTab> tabs)
        {
            string note;
            int n = SetupEventTabs(tabs, PageCount,
                                   (name, top, label) => BuildTabButton(name, top, label, false),
                                   _eventTabs, _eventHi, _eventLabels, _tabHi, _tabLabels,
                                   QTabIcon, QHit, ref _tabOverflowWarned, out note);
            EventTabNote = note;
            return n;
        }

        // ============================================================ 🆕 2026-10-23：`SetupTabs` 的**共用实现**（四扇活动窗同一份）
        // 🔴 **为什么要共用**：原版 `BaseParentEventWindow.SetupTabs` 是**基类**方法 —— Gacha / Raid /
        //    Expansion Pass 三扇窗**任何一扇**的左栏都列全部 live 事件（`HasComponent(ev,13)` 那一支）。
        //    我们这四扇是**四个独立类**（不硬继承，理由见文件头「我们侧的对位」）⇒ 若各抄一份
        //    `SetupTabs`，**同一条规则就有四处实现**（迟早不一致，铁律）。⇒ 判据只留这一份：
        //    各窗只提供①自己的 `BuildTabButton`（版式本来就同源）②自己的渲染队列档 ③自己的内容页占几颗。

        /// <summary>**建左栏的「事件钮」**（原版 `BaseParentEventWindow.SetupTabs` + `TabButtons.AddTabButton` 的等价物）。
        /// <para>兄弟序 = **内容页在前、事件钮在后**（原版 `AddTabButton` 里那句 `SetAsLastSibling()`）。</para>
        /// <para>⚠️ **内容页那几张表不被污染**：`buildTabButton` 会把 `Highlight` / `Label` 记进**内容页**的表
        /// （那是各窗自己的 `BuildTabButton` 的行为）⇒ 本方法把它们**搬进事件钮自己的表**。</para>
        /// <para>**幂等**：再调一次会把上一轮的事件钮全拆掉重建（内容页签一颗都不动）。</para></summary>
        /// <param name="firstSlot">内容页占了前几颗（Gacha/Raid = 1 · ExpansionPass/Parent = 2）——
        /// 事件钮从 `TabTop + TabH * firstSlot` 往下排（原版是同一个 VLG）。</param>
        /// <param name="buildTabButton">各窗自己的 `BuildTabButton(name, top, label, isMaster:false)`。</param>
        /// <param name="eventTabs">事件钮的表（**与内容页那张分家**）。</param>
        /// <param name="contentHi">该窗的**内容页**高亮表（`buildTabButton` 会往里塞，本方法负责搬出来）。</param>
        /// <param name="iconQueue">该窗的页签图标渲染档（四扇窗的队列带**各不相同**，⛔ 不能取本类的）。</param>
        /// <param name="hitQueue">该窗的命中区档（= 该窗的 `QHit`）。</param>
        /// <param name="overflowWarned">「页签溢出左栏」那条告警的**一次性**开关（各窗自己的字段）。</param>
        /// <param name="note">出声原文（自检读口；各窗存进自己的 `EventTabNote`）。</param>
        /// <returns>建出几颗（0 = 名单空）。</returns>
        public static int SetupEventTabs(
            IList<EventTab> tabs,
            int firstSlot,
            System.Func<string, float, string, Transform> buildTabButton,
            List<Transform> eventTabs, List<Transform> eventHi, List<Label> eventLabels,
            List<Transform> contentHi, List<Label> contentLabels,
            int iconQueue, int hitQueue,
            ref bool overflowWarned,
            out string note)
        {
            ClearEventTabs(eventTabs, eventHi, eventLabels);
            if (tabs == null || tabs.Count == 0)
            {
                note = "`SetupTabs` 名单为空 ⇒ 左栏只有内容页签（原版 `GameWindowWithTabs.SetupTabs(0)` 那一支）。";
                Debug.Log("[ParentEvent] " + note);
                return 0;
            }

            var notes = new List<string>();
            for (int i = 0; i < tabs.Count; i++)
            {
                var t = AddEventTabButton(tabs[i], firstSlot + eventTabs.Count, buildTabButton,
                                          eventTabs, eventHi, eventLabels, contentHi, contentLabels,
                                          iconQueue, hitQueue, ref overflowWarned);
                if (t == null) continue;
                notes.Add(t.name + "（" + (string.IsNullOrEmpty(tabs[i].Note) ? "—" : tabs[i].Note) + "）");
            }
            note = "`SetupTabs` 建出 " + eventTabs.Count + " 颗事件钮：" + string.Join(" · ", notes.ToArray())
                 + "。⚠️ 名单 = 四扇活动窗里除本窗以外的那几扇（原版读 `LiveOpsManager.GetAllEvents` 的"
                 + "远端列表，本机没有那条链）；在期活动实例参与页签名，过期的一律不用。";
            Debug.Log("[ParentEvent] " + note);
            // 🔴 **图标那一格如实出声**（⛔ 不许静默留空）：原版每颗钮的 `Image.sprite = LoadAsset(ev, 0)`，
            //   是 **liveops 资产**（`LiveOpsAssetRef.Icon`）—— 本机只有 GUID / 子件名，**没有可加载的 Sprite**
            //   ⇒ 四扇窗的事件钮**全部留空**。全库唯一一张和它同名的图是 **`40k_main_bt_event`**（135×135，
            //   `bundle_liveopsicons_assets_all`；切好的 PNG 在
            //   `d:/2/Warpforge_tools/data/ui_extract/liveopsicons_assets_all/Sprite/40k_main_bt_event.png`），
            //   但**没导进 `Resources/`** ⇒ 补法 = 进 `工具/import_original_art.py` 的 `MENU_IMAGES` 后跑一次同步。
            //   ⛔ 在到位之前**不拿母版的 `40K_shop_bt_boosters` 或别的图冒充**。
            bool anyIcon = false;
            for (int i = 0; i < tabs.Count; i++) if (tabs[i] != null && tabs[i].Icon != null) anyIcon = true;
            if (!anyIcon)
                Debug.LogWarning("[ParentEvent] 事件钮**图标留空**（" + eventTabs.Count + " 颗）：原版是 "
                               + "`LoadAsset(ev, 0)` 的 liveops Sprite，本机没有可加载的那一份"
                               + "（同名的 `40k_main_bt_event` 尚未导入 `Resources/`）⇒ **这些钮没有图标**，"
                               + "⛔ 不用别的图冒充。");
            return eventTabs.Count;
        }

        /// <summary>四扇窗共用：**建一颗**事件钮（原版 `TabButtons__AddTabButton` 那一跳）。
        /// <para>⚠️ `buildTabButton` 会把高亮/文字记进**内容页**的表 ⇒ 这里按「调用前后多出来的那几条」
        /// 精确搬进事件钮的表（⛔ 不用「取最后一条」那种写法 —— 那颗文字没建出来时会**偷走内容页的**）。</para></summary>
        static Transform AddEventTabButton(
            EventTab tab, int slot,
            System.Func<string, float, string, Transform> buildTabButton,
            List<Transform> eventTabs, List<Transform> eventHi, List<Label> eventLabels,
            List<Transform> contentHi, List<Label> contentLabels,
            int iconQueue, int hitQueue,
            ref bool overflowWarned)
        {
            if (tab == null || buildTabButton == null) return null;
            float top = TabTop + TabH * slot;
            if (!overflowWarned && top + TabH > ContentAreaR.y2 + 0.5f)
            {
                overflowWarned = true;
                Debug.LogWarning("[ParentEvent] ⚠️ 第 " + (slot + 1) + " 颗页签的按钮矩形下沿 = "
                               + (top + TabH).ToString("F1") + "px，超出左栏底 " + ContentAreaR.y2.ToString("F1")
                               + "px（原版 `Tab Buttons` 是上对齐 VLG、**没有滚动** ⇒ 事件多的时候同样溢出）。"
                               + "**可见件（图标 / 名牌 / 红点）仍在上沿 +24.6~+162.2 之内**，探出去的只是按钮矩形底部那截空白。");
            }

            int hiBefore = contentHi.Count, lbBefore = contentLabels.Count;
            // 原版 `set_name(label)` ⇒ 节点名 = 页签文字（⛔ 别改成内部代号：自检与出声都按它认）
            var t = buildTabButton(tab.Label, top, tab.Label);
            if (t == null) return null;
            while (contentHi.Count > hiBefore) { eventHi.Add(contentHi[hiBefore]); contentHi.RemoveAt(hiBefore); }
            while (contentLabels.Count > lbBefore) { eventLabels.Add(contentLabels[lbBefore]); contentLabels.RemoveAt(lbBefore); }

            // 图标（原版 `Image.sprite = LoadAsset(ev, 0)`）：`Icon == null` ⇒ 留空（⛔ 不拿母版图冒充）
            if (tab.Icon != null)
            {
                var icon = t.Find("Icon");                     // 直接子件（`transform.Find` 只找直接子件）
                if (icon != null)
                    MenuDraw.Rect(icon, tab.Icon, new PxRect(TabButtonsR.x1, top, TabButtonsR.x2, top + TabH),
                                  "Image", iconQueue, Color.white, true);
            }

            eventTabs.Add(t);
            // 点它 = 原版那个 `b__2` 回调（开**另一扇**活动窗）。`allowClickWhenAlreadySelected = 0` 照
            // `EverguildToggle` 的口径；`isOn` 恒 false ⇒ 每一下都派发（事件钮没有「已选中」这一态）
            var hit = MenuDraw.Hit(t, "Hit", new PxRect(TabButtonsR.x1, top, TabButtonsR.x2, top + TabH), hitQueue,
                                   () => { if (tab.Open != null) tab.Open(); },
                                   null, null, null, null, null, clickCue: WindowSounds.DefaultButtonCue);
            var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
            if (wb != null) { wb.allowClickWhenAlreadySelected = false; wb.isOn = () => false; }
            return t;
        }

        /// <summary>一颗事件钮（原版 `TabButtons__AddTabButton` 那一跳：`Instantiate` → `SetActive(true)` →
        /// `SetSiblingIndex(0)` → `set_name(label)` → `Image.sprite = 图标` → `Toggle.group` → 文字 `SetText`）。
        /// <para>⚠️ **不建 `Highlight` 的选中态**：原版左栏是一个 `ToggleGroup`（`m_AllowSwitchOff = 0`）里
        /// 内容页与事件钮混排、恒有一颗亮；我们的窗是**一扇一页**（事件钮点下去是**换窗**，不是换页）
        /// ⇒ 选中态只给内容页，事件钮的 `Highlight` 建着但**恒关**（如实标，⛔ 别当成漏做）。</para></summary>
        public Transform AddTabButton(EventTab tab)
        {
            return AddEventTabButton(tab, PageCount + _eventTabs.Count,   // 内容页占 0..PageCount-1
                                     (name, top, label) => BuildTabButton(name, top, label, false),
                                     _eventTabs, _eventHi, _eventLabels, _tabHi, _tabLabels,
                                     QTabIcon, QHit, ref _tabOverflowWarned);
        }

        /// <summary>拆掉上一轮建的事件钮（`SetupTabs` 的第一句）。</summary>
        void ClearEventTabs()
        {
            ClearEventTabs(_eventTabs, _eventHi, _eventLabels);
            EventTabNote = null;
        }

        /// <summary>四扇窗共用：拆事件钮。
        /// ⚠️ **先过一遍 Unity 假 null** —— `Build()` 第一句就是 `MenuDraw.ClearChildren(transform)`，
        /// 上一轮那些节点**已经没了**，直接读 `.gameObject` 会抛 `MissingReferenceException`
        /// （第二次 `Open()` 才会现形，静默难度高）。</summary>
        static void ClearEventTabs(List<Transform> nodes, List<Transform> hi, List<Label> labels)
        {
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i] != null) Kill(nodes[i].gameObject);
            nodes.Clear(); hi.Clear(); labels.Clear();
        }

        /// <summary>批处理下必须 `DestroyImmediate`（没有帧循环；同 `MenuDraw.ClearChildren` 那条）。</summary>
        static void Kill(GameObject go)
        {
            if (go == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying) { Object.DestroyImmediate(go); return; }
#endif
            Object.Destroy(go);
        }

        /// <summary>原版 `LiveOpsManager.GetAllEvents(2, …).Where(…)` 的**离线等价物**：给出「除本窗以外」的事件钮名单。
        /// <para>① 名单来自四扇活动窗（`EventWindowKind`）里不是本窗的那几扇 —— 原版是「每个 live 事件一颗钮」，
        /// 而 Gacha / Raid 的**活动定义只在远端**（本机 13 份 SO 里没有这两类）⇒ 它们永远拿不到活动实例；</para>
        /// <para>② Parent / Expansion Pass 两扇在 <see cref="LiveOpsLocalCatalog"/> 里有同类**在期且可选中**的实例时，
        /// 页签名与 `EventId` 取那个事件的（`label 4 = NavigationBarTitle` ?? `label 0 = Title`）。</para>
        /// <para>⚠️ **过期事件不参与**（用户边界「过期活动不做」）⇒ 今天（2026-10-23）13 份 SO 全部过期，
        /// 全部走官方词条兜底；<paramref name="utc"/> 由调用方给（自检传显式时刻，⛔ 不读 `NowUtc` 去断）。</para></summary>
        public static List<EventTab> BuildEventTabs(EventWindowKind self, System.DateTime utc)
        {
            var list = new List<EventTab>(4);
            for (int i = 0; i < EventWindowRootNames.Length; i++)
            {
                var kind = (EventWindowKind)i;
                if (kind == self) continue;                 // 本窗自己的内容已经在内容页签里了
                list.Add(MakeEventTab(kind, utc));
            }
            return list;
        }

        static EventTab MakeEventTab(EventWindowKind kind, System.DateTime utc)
        {
            var tab = new EventTab { Kind = kind, Open = OpenerFor(kind) };

            var kinds = CatalogKinds(kind);
            LiveOpsLocalEvent ev = null;
            string why;
            if (kinds != null) LiveOpsLocalCatalog.TrySelectActive(kinds, utc, out ev, out why);
            else
                why = "本机 13 份活动 SO 里**没有**这一类活动实例（`LiveOpsLocalKind` 只有 Parent / Scoring / "
                    + "PremiumItemOffer 三种）—— Gacha / Raid 的活动定义只在远端 CCD";

            if (ev != null)
            {
                tab.EventId = ev.EventId;
                tab.Label = LabelFromEvent(ev);
                tab.Note = "本地活动目录的在期事件 `" + ev.EventId + "`（label 4 = NavigationBarTitle ?? label 0 = Title）";
            }
            else
            {
                tab.Label = FallbackTabLabel(kind);
                tab.Note = "本机没有在期事件（" + why + "）⇒ 页签名用「" + tab.Label + "」兜底（**我们挑的**，"
                         + "不是原版 liveops 文案；过期活动一律不用）";
            }
            // 最后一道兜底：词条表缺键时 `Loc.T` 会回键名/空串 ⇒ 用根名顶上并出声（⛔ 不许静默建一颗空字页签）
            if (string.IsNullOrEmpty(tab.Label))
            {
                tab.Label = EventWindowRootNames[(int)kind];
                Debug.LogWarning("[ParentEvent] 事件钮文字取不到（" + tab.Note + "）⇒ 用根名 `" + tab.Label + "` 顶上");
            }
            // 图标：原版 `LoadAsset(ev, 0)`；本机只有 GUID / 子件名，没有可加载的 Sprite ⇒ null（见文件头）
            tab.Icon = null;
            return tab;
        }

        /// <summary>事件形态 → 本地活动目录的那几类（`null` = 本机没有这一类）。</summary>
        static LiveOpsLocalKind[] CatalogKinds(EventWindowKind kind)
        {
            switch (kind)
            {
                case EventWindowKind.Parent: return new[] { LiveOpsLocalKind.Parent };
                case EventWindowKind.ExpansionPass: return new[] { LiveOpsLocalKind.Scoring };
                default: return null;                        // Gacha / Raid
            }
        }

        /// <summary>页签名（`label 4` 优先，其次 `label 0`）—— 原版 `CreateEventTab` / `SetButtonInfo` 的次序。</summary>
        static string LabelFromEvent(LiveOpsLocalEvent ev)
        {
            LiveOpsLocalLabel l;
            if (ev.TryGetLabel((int)LiveOpsLabelRef.NavigationBarTitle, out l))
            {
                var t = LiveOpsLocalCatalog.TermText(l.Term);
                if (!string.IsNullOrEmpty(t)) return t;
            }
            if (ev.TryGetLabel((int)LiveOpsLabelRef.Title, out l))
            {
                var t = LiveOpsLocalCatalog.TermText(l.Term);
                if (!string.IsNullOrEmpty(t)) return t;
            }
            return null;
        }

        /// <summary>没有活动实例时的页签名兜底（**我们挑的**，逐条注明判据；⛔ 别写成「原版就是这样」）。</summary>
        static string FallbackTabLabel(EventWindowKind kind)
        {
            switch (kind)
            {
                case EventWindowKind.Gacha: return Loc.T(GachaEventWindow.TermTitle);           // `Event_Description/Gacha/TabTitle`
                case EventWindowKind.Raid: return Loc.T(RaidEventWindow.TermTabTitle);         // `Event_Description/General/Raid`
                case EventWindowKind.ExpansionPass: return Loc.T(ExpansionPassEventWindow.TermTabTitle); // `…/ExpansionPass/TabTitle`
                // Two Sides 那扇：本机没有它的活动实例 ⇒ 用它 prefab 的**出厂标题**（`P1TitleText`，prefab 实读）
                default: return P1TitleText;
            }
        }

        /// <summary>点这颗钮去哪（原版 `b__2` = `WindowsManager.OpenWindow(GetComponentReference(ev, 12))`）。
        /// ⚠️ 我们按 `<see cref="EventWindowKind"/>` 直开对应那扇窗（没有事件实例可选 ⇒ 这是等价物，如实标）。</summary>
        static System.Action OpenerFor(EventWindowKind kind)
        {
            switch (kind)
            {
                case EventWindowKind.Gacha: return () => WindowsManager.OpenEventHub(EventWindowKind.Gacha);
                case EventWindowKind.Raid: return () => WindowsManager.OpenEventHub(EventWindowKind.Raid);
                case EventWindowKind.ExpansionPass: return () => WindowsManager.OpenEventHub(EventWindowKind.ExpansionPass);
                default: return () => WindowsManager.OpenEventHub(EventWindowKind.Parent);
            }
        }

        // ============================================================ 页切换
        /// <summary>切页（原版 `GameWindowWithTabs.ChangeTab` 的 `SetActive` 语义）。</summary>
        public void SwitchPage(int index)
        {
            if (_pages.Count < PageCount) return;
            if (index < 0 || index >= PageCount) return;
            CurrentPageIndex = index;
            for (int i = 0; i < _pages.Count; i++)
                if (_pages[i] != null) _pages[i].gameObject.SetActive(i == index);
            for (int i = 0; i < _tabHi.Count; i++)
                if (_tabHi[i] != null) _tabHi[i].gameObject.SetActive(i == index);
        }

        /// <summary>自检用：切到某一页（同 `SwitchPage`，语义更直白）。</summary>
        public void OpenPage(Page page) { SwitchPage((int)page); }

        // ============================================================ 第 1 页：Choose Side
        void BuildChooseSidePage(Transform page)
        {
            // `Scan Lines` / `Glow`（同一张 `Glow UI W40K`；Glow 染洋红 (1,0,0.883)）
            MenuDraw.Rect(page, Tex(ArtGlow, "选择阵营页的扫描线"), Scr(P1ScanLinesR), "Scan Lines", QPageArt, Color.white);
            MenuDraw.Rect(page, Tex(ArtGlow, "选择阵营页的光晕"), Scr(P1GlowR), "Glow", QPageArt,
                          new Color(1f, 0f, 0.8834677f, 1f));

            // 两张队伍卡（红 / 蓝）—— 底 = `UICornersGradient` 四角色
            BuildTeamCard(page, P1TeamAR, P1TeamANoiseR, P1TeamAGlowR, P1TeamABottomTextR, P1TeamANameR, P1TeamATapR,
                          P1TeamATopR, P1TeamACaptainR, true);
            BuildTeamCard(page, P1TeamBR, P1TeamBNoiseR, P1TeamBGlowR, P1TeamBBottomTextR, P1TeamBNameR, P1TeamBTapR,
                          P1TeamBTopR, P1TeamBCaptainR, false);

            // 标题底板 + 标题（`Title Background` = 40k_main_bt_nametag Sliced）
            var titleBg = MenuDraw.Node(page, "Title Background", Scr(P1TitleBgR));
            MenuDraw.Rect(titleBg, Tex(ArtTabName, "标题底板"), Scr(P1TitleBgR), "Image", QPageArt1);
            var titleNode = MenuDraw.Node(page, "Title", Scr(P1TitleR));
            _p1Title = MenuDraw.TextBox(titleNode, Scr(P1TitleR), P1TitleText, Color.white, "Title",
                                        P1TitleFont, P1TitleMin, QPageText, P1TitleMax, P1TitleBase, face: TmpFont.Face.LatinUi);
            if (_p1Title != null)
            {
                MenuDraw.SetVAlign(_p1Title, Label.VAlign.Capline, Scr(P1TitleR));
                _p1Title.SetCharSpacing(P1TitleSpacing);     // `m_characterSpacing = 5`
            }

            // VS（图 `VS_icon` 本工程未导 ⇒ 只建节点、不出图 + 记账）
            var vs = MenuDraw.Node(page, "VS Text", Scr(P1VsR));
            MenuDraw.Rect(vs, Tex(ArtVs, "VS 图标"), Scr(P1VsR), "Image", QPageArt1, Color.white, true);

            // 说明（Center/Middle · fs48 · base36 · auto[18~48]）
            var desc = MenuDraw.TextBox(page, Scr(P1DescR), P1DescText, Color.white, "Description",
                                        P1DescFont, P1DescMin, QPageText, P1DescMax, P1DescBase, face: TmpFont.Face.LatinCardBody);
            if (desc != null) MenuDraw.SetVAlign(desc, Label.VAlign.Middle, Scr(P1DescR));

            // 返回钮（圆底 + 图标 + 文字；命中区 = 整颗钮）
            var back = MenuDraw.Node(page, "Back button", Scr(P1BackR));
            _p1Back = back;
            MenuDraw.Rect(back, Tex(ArtBackRound, "返回钮圆底"), Scr(P1BackR), "Image", QPageArt1);
            MenuDraw.Rect(back, Tex(ArtBackIcon, "返回箭头"), Scr(P1BackIconR), "Icon", QPageArt1);
            var backTx = MenuDraw.TextBox(back, Scr(P1BackTextR), "Back", Color.white, "Text",
                                          P1BackFont, P1BackMin, QPageText, P1BackMax, P1BackBase, face: TmpFont.Face.LatinCardBody);
            if (backTx != null)
            {
                MenuDraw.AlignLeft(backTx, Scr(P1BackTextR));
                MenuDraw.SetVAlign(backTx, Label.VAlign.Middle, Scr(P1BackTextR));
            }
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 C3）**：原版 `Two Sides Event Window/Select Team Tab/Back button`
            //   = 空 cue + **`useDefaultAudioClipIfNull = 0`** ⇒ **静音**（点了只关窗，只有关窗音那一声）。
            var p1BackHit = MenuDraw.Hit(back, "Hit", Scr(P1BackR), QHit, () => Close());
            var p1BackWb = p1BackHit != null ? p1BackHit.GetComponent<WindowButton>() : null;
            if (p1BackWb != null) p1BackWb.playClickSound = false;
        }

        void BuildTeamCard(Transform page, PxRect card, PxRect noise, PxRect glow, PxRect bottomText,
                           PxRect nameR, PxRect tapR, PxRect topR, PxRect captainR, bool isA)
        {
            // `Background`（**SkewedImage** + Mask + UICornersGradient）：斜切已接（`skewVector=(0.09,0)`）；
            //   `Mask` 本壳用 `ViewportClip` 等价、这一件没有裁切需求 ⇒ 不接；四角色照抄。
            var bg = MenuDraw.Node(page, "Two Side Event Select Team " + (isA ? "A" : "B"), Scr(card));
            var bgQuad = MenuDraw.Rect(bg, CardArt.Solid(), Scr(card), "Background", QPageBg,
                                       null, false, null, default, skewX: 0.09f);
            if (bgQuad != null)
                bgQuad.SetCornerColors(isA ? TeamABl : TeamBBl, isA ? TeamABr : TeamBBr,
                                       isA ? TeamATr : TeamBTr, isA ? TeamATl : TeamBTl);
            MenuDraw.Tiled(bg, Tex(ArtNoise, "队伍卡的噪点"), Scr(noise), 1024f, QPageArt, "Background noise", TeamNoiseTint);
            // `Foreground Image`：`m_Sprite = null`（运行时灌阵营图）⇒ 只建容器，不出图（如实记录）
            MenuDraw.Node(bg, "Foreground Image", Scr(new PxRect(
                isA ? 200.2f : 846.2f, 164.0f, isA ? 1093.8f : 1739.8f, 1057.6f)));
            MenuDraw.Nine(bg, Tex(ArtBottomGlow, "队伍卡底部光晕"), Scr(glow),
                          new Vector4(3f, 0f, 3f, 0f), 32f, 32f, QPageArt, isA ? TeamAGlowTint : TeamBGlowTint,
                          true, "Bottom Glow");

            var bottom = MenuDraw.Node(bg, "Bottom Text", Scr(bottomText));
            var nm = MenuDraw.TextBox(bottom, Scr(nameR), P1TeamName, Color.white, "Team Name",
                                      P1TeamFont, P1TeamMin, QPageText, P1TeamMax, P1TeamBase, face: TmpFont.Face.LatinCardBody);
            if (nm != null) MenuDraw.SetVAlign(nm, Label.VAlign.Capline, Scr(nameR));
            var tap = MenuDraw.TextBox(bottom, Scr(tapR), P1TapText, P1TapInk, "Tap to select",
                                       P1TapFont, P1TapMin, QPageText, P1TapMax, P1TeamBase, face: TmpFont.Face.LatinCardBody);
            if (tap != null)
            {
                MenuDraw.SetVAlign(tap, Label.VAlign.Capline, Scr(tapR));
                _p1TapLabels.Add(tap);
            }

            // `Border`（40k_square_border · 九宫 13,13,13,13 · ppuMul 5 · fillCenter=0）
            //   🆕 **2026-10-17（`N53②`）**：原版这颗也挂 `SkewedImage skewVector=(0.09,0)` ⇒ 同斜切。
            MenuDraw.Nine(bg, Tex(ArtBorder, "队伍卡边框"), Scr(card), new Vector4(13f, 13f, 13f, 13f),
                          64f, 64f, QPageArt1, TeamBorderTint, false, "Border",
                          new Vector4(13f / 5f, 13f / 5f, 13f / 5f, 13f / 5f),
                          null, default, skewX: 0.09f);

            var top = MenuDraw.Node(bg, "Top", Scr(topR));
            var cap = MenuDraw.TextBox(top, Scr(captainR), P1Captain, Color.white, "Team captain",
                                       P1TeamFont, P1TeamMin, QPageText, P1TeamMax, P1TeamBase, face: TmpFont.Face.LatinCardBody);
            if (cap != null) MenuDraw.SetVAlign(cap, Label.VAlign.Capline, Scr(captainR));

            // 整张队伍卡是 `EverguildButton` —— 选阵营要走服务器（`PlayfabWrapper.GenericCloudScriptHandler(0x45b, …)`）
            // ⇒ 本地**点了只出声**（⛔ 不假装选中）。
            MenuDraw.Hit(bg, "Hit", Scr(card), QHit,
                         () => Debug.LogWarning("[ParentEvent] 选择阵营（" + (isA ? "A" : "B") + "）需要服务器："
                                              + "原版走 `TwoSidesEvent_ChooseSideTab.TeamSelected` → "
                                              + "`GenericCloudScriptHandler(0x45b, …)`；本地没有 LiveOps 后端 ⇒ **不假装选中**。"),
                         null, null, null, null, null, clickCue: WindowSounds.DefaultButtonCue);
        }

        // ============================================================ 第 2 页：On Going
        void BuildOnGoingPage(Transform page)
        {
            // `General Red Background` —— 🔴 **2026-10-17（`N53②` 现核）**：容器 rect **不是退化**，
            //   原版 RT 实读 = `anchor (0,0.5)-(1,0.5)` · `sizeDelta (0,−159.7153)` · `pos (0,−14.4843)`
            //   ⇒ 屏幕 **x 0..1920 · y 94.34..1014.63**；它自己**没有 Image 组件** ⇒ 不产生画面，
            //   真正画的是子件 `P2RedBgR`（= 同一范围，逐位吻合）。
            var gen = MenuDraw.Node(page, "General Red Background", Scr(new PxRect(0f, 94.34f, 1920f, 1014.63f)));
            // 页内那层 `Menu Dark Background` **出厂 INACT**（dump 实读）⇒ 建出来、关着。
            var pageShade = MenuDraw.Node(gen, "Menu Dark Background",
                                          Scr(new PxRect(-1327.3f, -759.2f, 3247.3f, 1813.2f)));
            MenuDraw.Rect(pageShade, CardArt.Solid(), Scr(new PxRect(-1327.3f, -759.2f, 3247.3f, 1813.2f)),
                          "Image", QPageBg, ShadeTint);
            pageShade.gameObject.SetActive(false);
            MenuDraw.Nine(gen, Tex(ArtRedBg, "活动页红底"), Scr(P2RedBgR),
                          new Vector4(0f, 26f, 0f, 26f), 21f, 81f, QPageBg, null, true, "Reward Background Get Reward");
            MenuDraw.Tiled(gen, Tex(ArtNoise, "活动页噪点"), Scr(P2RedBgR), 1024f, QPageArt, "Noise", NoiseTint);
            MenuDraw.Rect(gen, CardArt.Solid(), Scr(P2VignetteR), "Menu Vignette", QPageArt, VignetteTint);

            // 表头（`WindowHeaderWithBackButton`）+ 返回钮
            var header = MenuDraw.Node(page, "Game Mode Header With Back Button", Scr(P2HeaderR));
            var hdrBg = MenuDraw.Node(header, "Header Background", Scr(P2HeaderBgR));
            MenuDraw.Nine(hdrBg, Tex(ArtCampaignHeader, "表头底"), Scr(P2HeaderBgR),
                          new Vector4(335f, 0f, 395f, 0f), 740f, 167f, QPageArt1, null, true, "Image");
            var titleNode = MenuDraw.Node(hdrBg, "Window Title", Scr(P2TitleR));
            _p2HeaderTitle = MenuDraw.TextBox(titleNode, Scr(P2TitleR), P2HeaderTitle, Color.white, "Window Title",
                                              P2HeaderFont, P2HeaderMin, QPageText, P2HeaderMax, P2HeaderBase, face: TmpFont.Face.LatinUi);
            if (_p2HeaderTitle != null)
            {
                MenuDraw.AlignLeft(_p2HeaderTitle, Scr(P2TitleR));
                MenuDraw.SetVAlign(_p2HeaderTitle, Label.VAlign.Capline, Scr(P2TitleR));
                _p2HeaderTitle.SetCharSpacing(5f);           // `m_characterSpacing = 5`
            }
            // `Game Mode Icon`：`m_Color.a = 0`（出厂不可见），运行时由 `LiveopUIDrawer_GameModeIcon` 灌图
            var gmIcon = MenuDraw.Node(hdrBg, "Game Mode Icon", Scr(P2GameModeIconR));
            MenuDraw.Rect(gmIcon, Tex(ArtInfo, "活动模式提示图标"), Scr(P2InfoTriggerR), "tooltip trigger",
                          QPageArt1, Color.white, true);
            var hdrBg1 = MenuDraw.Node(header, "Header Background (1)", Scr(P2HeaderBg1R));
            MenuDraw.Nine(hdrBg1, Tex(ArtCampaignHeader, "表头底（左）"), Scr(P2HeaderBg1R),
                          new Vector4(335f, 0f, 395f, 0f), 740f, 167f, QPageArt1, null, true, "Image");
            var back = MenuDraw.Node(header, "Header Back Button", Scr(P2HeaderBackR));
            _p2Back = back;
            MenuDraw.Rect(back, Tex(ArtMenuBack, "返回钮"), Scr(P2HeaderBackR), "Image", QPageArt1);
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 B5）**：原版
            //   `Two Sides Event Window/Event On Going Tab/Game Mode Header With Back Button/Header Back Button`
            //   = 空 cue + **`useDefaultAudioClipIfNull = 1`** ⇒ **默认 `ButtonClick`**
            //   （改前我们传了 `DefaultCloseCue` = `ButtonClick Back`，是错的 ⇒ 现在不传）。
            MenuDraw.Hit(back, "Hit", Scr(P2HeaderBackR), QHit, () => Close());

            // `Battle Background`（`m_Sprite = null` ⇒ 只建容器）
            MenuDraw.Node(page, "Battle Background", Scr(P2BattleBgR));

            // 两张队伍卡（图 `m_Sprite = null`，运行时灌）
            BuildOngoingTeam(page, "Team A", P2TeamAR, P2TeamAImageR, P2TeamACaptainR, true);
            BuildOngoingTeam(page, "Teams B", P2TeamBR, P2TeamBImageR, P2TeamBCaptainR, false);

            BuildRewardDisplay(page);

            // `Scoring slider`（左右两队的比分条；`m_Value = 0.5`）
            var slider = MenuDraw.Node(page, "Scoring slider", Scr(P2ScoringSliderR));
            MenuDraw.Rect(slider, CardArt.Solid(), Scr(P2ScoringBgR), "Background", QPageArt1, ScoringBgTint);
            MenuDraw.Rect(slider, CardArt.Solid(), Scr(P2CenterMarkR), "Center Mark", QPageArt, CenterMarkTint);
            // `Fill Area/Fill` 出厂宽 0（= 进度 0）⇒ 只建节点、不出图（同 `Energy` 的出厂档）
            var fillArea = MenuDraw.Node(slider, "Fill Area", Scr(new PxRect(63.3f, 788.7f, 1293.4f, 875.3f)));
            MenuDraw.Node(fillArea, "Fill", Scr(new PxRect(58.3f, 875.3f, 58.4f, 875.3f)));

            // `Your Team` 两行（Left / Right · fs58 · Capline）
            var yta = MenuDraw.TextBox(page, Scr(P2YourTeamAR), "Your Team", Color.white, "Your Team Text_Team A",
                                       P2YourTeamFont, P2YourTeamMin, QPageText, P2YourTeamMax, 36f, face: TmpFont.Face.LatinCardBody);
            if (yta != null)
            {
                MenuDraw.AlignLeft(yta, Scr(P2YourTeamAR));
                MenuDraw.SetVAlign(yta, Label.VAlign.Capline, Scr(P2YourTeamAR));
            }
            var ytb = MenuDraw.TextBox(page, Scr(P2YourTeamBR), "Your Team", Color.white, "Your Team Text_Team B",
                                       P2YourTeamFont, P2YourTeamMin, QPageText, P2YourTeamMax, 36f, face: TmpFont.Face.LatinCardBody);
            if (ytb != null)
            {
                MenuDraw.AlignRight(ytb, Scr(P2YourTeamBR));
                MenuDraw.SetVAlign(ytb, Label.VAlign.Capline, Scr(P2YourTeamBR));
            }

            // 两行队名（骷髅加成 + 队伍名）
            BuildTeamNameRow(page, P2TeamANameRowR, P2TeamABonusR, P2SkullSdfAR, P2SkullAR, P2TeamANameR, true);
            BuildTeamNameRow(page, P2TeamBNameRowR, P2TeamBBonusR, P2SkullSdfBR, P2SkullBR, P2TeamBNameR, false);

            // 倒计时（`TimerDisplay`；截止时刻在远端 ⇒ `--` + 出声一次）
            var timerNode = MenuDraw.Node(page, "Timer With Time Description", Scr(P2TimerR));
            MenuDraw.Rect(timerNode, Tex(ArtClock, "时钟图标"), Scr(P2ClockIconR), "Clock Icon", QPageArt1, Color.white, true);
            var timerTx = MenuDraw.TextBox(timerNode, Scr(P2TimerTextR), "--", Color.white, "Timer Text",
                                           P2TimerFont, P2TimerMin, QPageText, P2TimerMax, P2TimerBase, face: TmpFont.Face.LatinCardBody);
            if (timerTx != null)
            {
                MenuDraw.AlignLeft(timerTx, Scr(P2TimerTextR));
                MenuDraw.SetVAlign(timerTx, Label.VAlign.Capline, Scr(P2TimerTextR));
                _p2Timer = timerTx;
                TimerDisplay.Attach(timerTx, null, "", "ParentEvent/OnGoing/Timer",
                                    "活动截止时刻在远端 LiveOps（`LiveOpsEvent.TimeDescriptionTextKey` + 事件数据），本地没有");
            }
        }

        void BuildOngoingTeam(Transform page, string name, PxRect card, PxRect image, PxRect captain, bool isA)
        {
            var n = MenuDraw.Node(page, name, Scr(card));
            // `Team Image` / `Team B Image`：`m_Sprite = null`（运行时灌）
            MenuDraw.Node(n, name == "Team A" ? "Team A Image" : "Team B Image", Scr(image));
            var cap = MenuDraw.TextBox(n, Scr(captain), P2Captain, Color.white,
                                       name == "Team A" ? "Team A captain name" : "Team B captain name",
                                       P2TeamFont, P2TeamMin, QPageText, P2TeamMax, 36f, face: TmpFont.Face.LatinCardBody);
            if (cap != null)
            {
                if (isA) MenuDraw.AlignLeft(cap, Scr(captain));
                else MenuDraw.AlignRight(cap, Scr(captain));
                MenuDraw.SetVAlign(cap, Label.VAlign.Capline, Scr(captain));
            }
        }

        void BuildRewardDisplay(Transform page)
        {
            var rp = MenuDraw.Node(page, "Reward Display", Scr(P2RewardPanelR));

            // `Scoring Bar Event Score Info`（MilestoneScoreBar：竖向进度条 + 5 档记分行）
            var bar = MenuDraw.Node(rp, "Scoring Bar Event Score Info", Scr(P2ScoreBarR));
            var prog = MenuDraw.Node(bar, "Progress Bar", Scr(P2ProgressR));
            MenuDraw.Rect(prog, Tex(ArtMiniBar, "进度条底"), Scr(P2ProgressBgR), "Background", QBar, Color.white);
            // `Fill Area/Fill` 出厂进度 0 ⇒ 不画（同 `Energy` 口径）

            _scoreLevels = MenuDraw.Node(bar, "Score Levels", Scr(new PxRect(1588.2f, 390.8f, 1713.8f, 797.5f)));
            for (int i = 0; i < ScoreRowTop.Length; i++)
            {
                float top = ScoreRowTop[i];
                var row = MenuDraw.Node(_scoreLevels, "Score Bar Line Level " + (i + 1),
                                        Scr(new PxRect(1465.3f, top, 1465.3f + ScoreRowW, top + ScoreRowH)));
                // `Highlight Crate` / `Chest` / `Skull` / `Score`（dump 的**视觉框**，已含嵌套 localScale）
                // 🔴 **2026-10-17（第四十八会话 · `N53` 三张图）就地订正（铁律 5）**：
                //   旧注释写「`Crate Border Highlight` **本工程没导入**」—— **过期**，图早在
                //   `Resources/Art/ui_menu/Crate_Border_Highlight.png`。
                //   判据（原版 Image MB `-2678701858188120570` 实读）：`m_Type = 0 (Simple)` ·
                //   `m_PreserveAspect = 0` · `m_Color = (0.98039, 0.80104, 0.05882, 1)`（金）·
                //   sprite `m_Border = (0,0,0,0)` ⇒ `m_FillCenter = 0` 对 **Simple** 不生效，
                //   原版就是「整块拉伸的金色版」、中心被上面的 `Chest` 盖住 ⇒ 用 `Rect` 照画。
                //   队列取 `QPageArt`（在 `Chest` 的 `QPageArt1` 之下）。
                MenuDraw.Rect(row, Tex("Crate_Border_Highlight", "奖励箱高亮框"),
                              Center(new PxRect(1642.3f, top - 52.9f, 1831.8f, top + 116.0f), 150.86f, 134.43f),
                              "Highlight Crate", QPageArt, new Color(0.9803922f, 0.8010414f, 0.0588235f, 1f));
                MenuDraw.Rect(row, Tex(CrateArt[i], "第 " + (i + 1) + " 档箱子"),
                              Center(new PxRect(1682.0f, top - 13.1f, 1796.4f, top + 88.9f), 99.45f, 88.70f),
                              "Chest", QPageArt1, Color.white);
                MenuDraw.Rect(row, Tex(ArtSkull, "骷髅"), Center(new PxRect(1555.6f, top + 0.9f, 1629.8f, top + 75.1f),
                                                                64.51f, 64.51f),
                              "Skull", QPageArt1, Color.white);
                var sc = MenuDraw.TextBox(row, Scr(new PxRect(1426.9f, top + 10.7f, 1553.8f, top + 65.3f)),
                                          P2ScoreText, Color.white, "Score", 48f, 18f, QPageText, 48f, 36f, face: TmpFont.Face.LatinCardBody);
                if (sc != null)
                {
                    MenuDraw.AlignRight(sc, Scr(new PxRect(1426.9f, top + 10.7f, 1553.8f, top + 65.3f)));
                    MenuDraw.SetVAlign(sc, Label.VAlign.Midline, Scr(new PxRect(1426.9f, top + 10.7f, 1553.8f, top + 65.3f)));
                    _p2ScoreLabels.Add(sc);
                }
            }

            // `Generic Simplified UI Button`（Collect）
            _p2CollectBtn = MenuDraw.Node(rp, "Generic Simplified UI Button", Scr(P2CollectR));
            MenuDraw.Rect(_p2CollectBtn, Tex(ArtMulliganBtn, "Collect 钮底"), Scr(P2CollectR), "Image",
                          QPageArt1, Color.white, true);
            _p2Collect = MenuDraw.TextBox(_p2CollectBtn, Scr(P2CollectTextR), P2CollectText, Color.white, "Button Text",
                                          P2CollectFont, P2CollectMin, QPageText, P2CollectMax, P2CollectBase, face: TmpFont.Face.LatinCardBody);
            if (_p2Collect != null) MenuDraw.SetVAlign(_p2Collect, Label.VAlign.Capline, Scr(P2CollectTextR));
            MenuDraw.Hit(_p2CollectBtn, "Hit", Scr(P2CollectR), QHit,
                         () => Debug.LogWarning("[ParentEvent] 领奖（Collect）需要服务器：原版走 "
                                              + "`TwoSidesEvent_OnGoingTab.CollectClicked` → `TwoSidesEvent.TryCollect` → "
                                              + "PlayFab；本地没有 LiveOps 后端 ⇒ **不假装领取**。"),
                         // 🆕 **2026-10-18（音效线缺口 `AUD` §三 B6）**：原版
                         //   `Two Sides Event Window/Event On Going Tab/Reward Display/Scoring Bar Event Score Info/
                         //   Generic Simplified UI Button` → **`ButtonClick Positive`**（pid 8549838806721213354）。
                         null, null, null, null, null, clickCue: WindowSounds.PositiveCue);

            // `Reward Tile` / `Reward Help`
            _p2RewardTile = MenuDraw.TextBox(rp, Scr(P2RewardTileR), P2RewardTile, Color.white, "Reward Tile",
                                             P2TileFont, P2TileMin, QPageText, P2TileMax, 36f, face: TmpFont.Face.LatinCardBody);
            if (_p2RewardTile != null) MenuDraw.SetVAlign(_p2RewardTile, Label.VAlign.Midline, Scr(P2RewardTileR));
            var help = MenuDraw.TextBox(rp, Scr(P2RewardHelpR), P2RewardHelp, Color.white, "Reward Help",
                                        P2HelpFont, P2HelpMin, QPageText, P2HelpMax, 36f, face: TmpFont.Face.LatinCardBody);
            if (help != null) MenuDraw.SetVAlign(help, Label.VAlign.Middle, Scr(P2RewardHelpR));

            // `Player victories`（Skulls: + 骷髅 + 125）
            var pv = MenuDraw.Node(rp, "Player victories", Scr(P2VictoriesR));
            var vt = MenuDraw.TextBox(pv, Scr(P2VictoriesTitleR), P2VictoriesTitle, Color.white, "Vicotries title",
                                      P2VictoriesFont, P2VictoriesMin, QPageText, P2VictoriesMax, 36f, face: TmpFont.Face.LatinCardBody);
            if (vt != null)
            {
                MenuDraw.AlignRight(vt, Scr(P2VictoriesTitleR));
                MenuDraw.SetVAlign(vt, Label.VAlign.Capline, Scr(P2VictoriesTitleR));
            }
            MenuDraw.Rect(pv, Tex(ArtSkull, "总骷髅图标"), Scr(P2VictoriesSkullR), "Skull Victories", QPageArt1);
            _p2TotalVictories = MenuDraw.TextBox(pv, Scr(P2VictoriesTotalR), P2VictoriesTotal, Color.white,
                                                 "Total Victories", P2VictoriesFont, P2VictoriesMin, QPageText,
                                                 P2VictoriesMax, 36f, face: TmpFont.Face.LatinCardBody);
            if (_p2TotalVictories != null)
            {
                MenuDraw.AlignLeft(_p2TotalVictories, Scr(P2VictoriesTotalR));
                MenuDraw.SetVAlign(_p2TotalVictories, Label.VAlign.Midline, Scr(P2VictoriesTotalR));
            }
        }

        void BuildTeamNameRow(Transform page, PxRect rowR, PxRect bonusR, PxRect sdfR, PxRect skullR,
                              PxRect nameR, bool isA)
        {
            var row = MenuDraw.Node(page, isA ? "Team A Name" : "Team B Name", Scr(rowR));
            // 顺序照原版兄弟序：A 行 = Bonus → Name；B 行 = Name → Bonus
            if (isA) BuildTeamBonus(row, bonusR, sdfR, skullR);
            var nm = MenuDraw.TextBox(row, Scr(nameR), P2TeamName, Color.white, isA ? "Team A Name" : "Team B Name",
                                      P2TeamFont, P2TeamMin, QPageText, P2TeamMax, 36f, face: TmpFont.Face.LatinCardBody);
            if (nm != null)
            {
                if (isA) MenuDraw.AlignLeft(nm, Scr(nameR));
                else MenuDraw.AlignRight(nm, Scr(nameR));
                MenuDraw.SetVAlign(nm, Label.VAlign.Capline, Scr(nameR));
            }
            if (!isA) BuildTeamBonus(row, bonusR, sdfR, skullR);
        }

        void BuildTeamBonus(Transform row, PxRect bonusR, PxRect sdfR, PxRect skullR)
        {
            var bonus = MenuDraw.Node(row, "Team Bonus", Scr(bonusR));
            // `Skull SDF`（128×128）**本工程未导入** ⇒ 只建节点、不出图 + 记账
            var sdf = MenuDraw.Node(bonus, "Skull SDF", Scr(sdfR));
            MenuDraw.Rect(sdf, Tex(ArtSkullSdf, "骷髅 SDF 光晕"), Scr(sdfR), "Image", QPageArt);
            MenuDraw.Rect(bonus, Tex(ArtSkull, "骷髅"), Scr(skullR), "Skull", QPageArt1);
        }

        // ============================================================ 坐标 / 取图
        /// <summary>屏幕绝对矩形 → `Tabs` 容器内部坐标（两页的 dump 坐标都是**屏幕绝对**的）。</summary>
        static PxRect Scr(PxRect r)
        {
            // 🔴 dump 的坐标**全是屏幕绝对（1920×1080 · 左上原点）**，而 `MenuDraw.Local` 只做
            //    「目标中心 − 父节点当前位置」⇒ **只要传进来的矩形与父节点处在【同一个坐标系】就成立**，
            //    与深度无关。本窗的**根就在原点**（`AttachToAnchor` 把它铺满屏）⇒ 整棵树一律用**绝对坐标**，
            //    这里就是恒等函数（留在调用点上是为了写明「这些数是屏幕绝对、别在某层再减一次 Tabs 原点」）。
            //    ⚠️ 曾经把它写成减 `(167.2, 70.9)` ⇒ 页会整体左上偏一个 Tabs 原点（**静默错位**）。
            return r;
        }

        /// <summary>以 `r` 的中心为心、换成 `w×h`（dump 的「视觉框」列用这个落位）。</summary>
        static PxRect Center(PxRect r, float w, float h)
        {
            float cx = (r.x1 + r.x2) * 0.5f, cy = (r.y1 + r.y2) * 0.5f;
            return new PxRect(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
        }

        static ImageQuad QuadOf(Transform t)
        {
            return t != null ? t.GetComponentInChildren<ImageQuad>(true) : null;
        }

        /// <summary>取图 —— **取不到必须出声**（红线；`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。</summary>
        Texture2D Tex(string art, string what)
        {
            var t = CardArt.MenuUi(art);
            if (t == null && !_missArt.Contains(art))
            {
                _missArt.Add(art);
                Debug.LogWarning("[ParentEvent] 图取不到：`" + art + "`（" + what + "）⇒ **这一件没画**"
                               + "（导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`）。");
            }
            return t;
        }
    }
}
