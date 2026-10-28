// GameWindowWithTabs.cs — 带页签的窗口（原版同名类，`GameWindowWithTabs : GameWindow`）。
// 🔴 **2026-10-13 从 `RewardsWindow.cs` 拆出**（A1506：Unity 要求 MonoBehaviour 的**类名与所在文件名相同**，
//    否则建不出脚本资产 ⇒ 场景存盘会把它烘成内联 `MonoScript` 存根 ⇒ player 加载即崩）。
//    判据 / 出处 / 纪律见 `RewardsWindow.cs` 的文件头（本块**逐字搬**，未改一字）。
// 🆕 **2026-10-25（复刻第 3 轮 · 代理 H）：补齐原版页签生命周期**。
//    判据 = `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/GameWindowWithTabs.cs` 与
//           `D:/2/tools/decomp_full/GameWindowWithTabs__*.c`（逐方法）：
//      · `SetupTabs`（:85-159）= 关母版 + `tabButtons.Initialize(this)` + 把 `tabs` 建成 `Type→tab` 缓存 + `ForceCloseTabs()`
//      · `Open`（:161-185）= 首次 `SetupTabs`（`initialized` 闸）→ `base.Open()` → 当前页为空则 `GetStartingTab()`
//        ＋ 触发 `OnTabChanged` → **`CurrentTab.TryOpenTab()`**（**不关别的页**）
//      · `OpenTabs`（:187-205）= 上面那段抽出来的（`Open` 之外的入口）
//      · `GetStartingTab`（:207-274）= `windowOptions` 若是 `GameWindowWithTabsOptions` 且带 `startTab` ⇒ 按类型查；
//        查不到 / 不是那种 options ⇒ `tabs.FirstOrDefault()`
//      · `ChangeTab<TTab>`（:276-306）/ `ChangeTab(WindowTabBase)`（:308-311）= → `ChangeTabCO(CurrentTab, tab)`
//      · `GetTab<TTab>`（:313-335）/ `GetTab(Type)`（:337-361）= 缓存查询，未命中再按 `ReflectionHelper.Is` 扫
//      · `ChangeTabCO`（:363-388）= **同一页** ⇒ 只 `TryOpenTab`；**换页** ⇒ `from.CloseTab()` → `CurrentTab = to` →
//        `to.TryOpenTab()` → `OnTabChange()`；**两条路都**触发 `OnTabChanged`（顺序：OnTabChange 在事件之前）
//      · `OnTabChange`（:390-392）虚方法
//      · `Close`（:394-401）= `base.Close()` + 当前页 `CloseTab()`
//      · `ForceCloseTabs`（:403-428）= 当前页 `CloseTab()` + **所有**页 `SetActive(false)`（**不清 `CurrentTab`**）
//      · `ToFocus`（:430-441）= 当前页 `ToFocus()` + 触发 `OnTabChanged`
//      · `CreateTabButton`（:443-457）= `tabButtons.AddTabButton(name, icon)` + `AssignTabButton(tab, button)`
//      · `DestroyTab`（:553-568）= 从 `tabs`/`tabButtons`/缓存里摘掉 + `Destroy(tab.gameObject)`
//    ⚠️ **与原版的差异（如实记，⛔ 不写成原版行为）**：
//      ① 原版 `CurrentTab` 是 **`WindowTabBase` 对象**；我们仓里早有一个 **`WindowTabType CurrentTab` 枚举**
//         （几十处自检/窗口在读）⇒ **枚举那个一个字不动**，对象版另起名 **`CurrentTabView`**（两者在
//         `ChangeTabCO` 里同步）。接实例时用对象版；枚举版是历史读口。
//      ② 原版 `Open` 里 `CurrentTab.TryOpenTab()` **不关别的页**（假设同一时刻只有一页开着）；我们的枚举
//         `ChangeTab` 一直有「先把所有页 `SetActive(false)`」的兜底 ⇒ **保留**（防止上一次异常留下的脏态）。
//      ③ `GetStartingTab` 原版读 `GameWindowWithTabsOptions.startTab`；我们**没有 options 资产**，回落到
//         `tabs.FirstOrDefault()`（= 原版回落那一条）。各窗自己的「回到上次页」规则仍在
//         `RewardsWindow.StartVisualIndex` 等处 —— 下一轮接线时收口到这里。
//      ④ 原版 `SetupTabs` 用 `TabbedWindowComponents`（`tabPrefab`/`tabButtons`/`tabHolder`）；我们的等价物是窗口自己的
//         三个同名字段（`tabButtons` / `tabHolder` / 🆕 `tabPrefab`），⛔ 不新建那个空壳组件类（本仓无 prefab 数据链）。
//      ⑤ 原版 `CreateTab<T>`（从 prefab 实例化）—— 🆕 **2026-10-28（B92_N5d）已建**（类尾；判据 = `__CreateTab_object_.c` 94 行）；
//         ⚠️ 仍有一条真缺口：图标（`Sprite`）没去路（`TabButtons.AddTabButton` 只吃名字）⇒ 见报告「差什么」。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>带页签的窗口。原版 `GameWindowWithTabs : GameWindow`。</summary>
    public class GameWindowWithTabs : GameWindow
    {
        /// <summary>`tabs`（原版字段名）—— 顺序 = 原版 `GameWindowWithTabs.tabs` = Missions/Forge/Campaign。</summary>
        public readonly List<WindowTabBase> tabs = new List<WindowTabBase>();

        /// <summary>左栏的**视觉顺序**（= 键表 `RewardsWindow.Buttons` 的顺序）：
        /// Missions / Campaign / Forge / **DailyStreak（🆕 A815 自建）** / BoosterPacks(母版，不是页签)。
        /// ⚠️ **最后一项必须是母版那一格**（`MainMenuSubmenuWindow.BuildBar` 把 `specs` 最后一项当
        /// `tabButtonPrefab` 并关掉）⇒ 想多一个可见键，就得把它**再往下一格挪**（A815 就是这么做的）。</summary>
        public readonly List<WindowTabType> visualTypes = new List<WindowTabType>
        {
            WindowTabType.Missions, WindowTabType.Campaign, WindowTabType.Forge,
            WindowTabType.DailyStreak, WindowTabType.None,
        };

        public TabButtons tabButtons;
        public Transform tabHolder;

        /// <summary>⚠️ **历史读口（枚举版）**。原版没有它 —— 原版 `CurrentTab` 是对象。两者在 `ChangeTabCO` 同步。</summary>
        public WindowTabType CurrentTab { get; private set; } = WindowTabType.None;

        // ======================================================== 原版生命周期（2026-10-25 补）

        /// <summary>原版 `CurrentTab`（`GameWindowWithTabs.cs:70-83`）的等价物 —— **页对象**版。</summary>
        public WindowTabBase CurrentTabView { get; private set; }

        /// <summary>原版字段 `OnTabChanged: Action&lt;WindowTabBase&gt;`（:55）。`TabButtons.Initialize` 订阅它。</summary>
        public Action<WindowTabBase> OnTabChanged;

        readonly Dictionary<Type, WindowTabBase> _cachedTabs = new Dictionary<Type, WindowTabBase>();
        bool _tabsInitialized;

        /// <summary>自检读口：`SetupTabs` 走过没有（= 生产 `Open()` 有没有走到 `EnsureTabsSetup`）。</summary>
        public bool TabsInitializedForTest { get { return _tabsInitialized; } }

        /// <summary>原版 `SetupTabs()`（:85-159）：母版隐藏由 `TabButtons.Initialize` 做；这里建缓存 + 全关。</summary>
        protected virtual void SetupTabs()
        {
            if (tabButtons != null)
            {
                tabButtons.Initialize(this);
                // 判据缺口补偿（见 TabButtons.Initialize 的注释）：原版靠 toggle 初始事件消费 firstTime。
            }
            _cachedTabs.Clear();
            for (int i = 0; i < tabs.Count; i++)
            {
                var t = tabs[i];
                if (t == null) continue;
                _cachedTabs[t.GetType()] = t;
            }
            ForceCloseTabs();
            _tabsInitialized = true;
        }

        /// <summary>🆕 **2026-10-25（用户纠正 ②）**：只做**一次** `SetupTabs`（`Open()` 是高频道入口 ⇒ 已建过就跳过）。
        /// <para>为什么不是每个 `Open` 都跑：`SetupTabs` 会 `ForceCloseTabs()`；而我们的窗每次 `Open` 都**重建页对象**
        /// （`tabs.Clear()` + 新 `AddComponent`）—— 每开一次都关一遍当前页会多出一次 `OnClose`，原版没有这一跳。
        /// 页对象缓存失效**不影响生产**：`ChangeTab(WindowTabType)` 走 `FindTab` **扫 `tabs` 活表**，随后 `ChangeTabCO`
        /// 会把 `CurrentTabView` 更新成新对象（`GetTab(Type)` 的旧缓存只有自检/将来运行期加页才会读）。</para></summary>
        protected void EnsureTabsSetup()
        {
            if (_tabsInitialized) return;
            SetupTabs();
        }

        /// <summary>原版 `Open()`（:161-185）。⚠️ 我们四扇带页签的窗现在**各自覆写 `Open`** ——
        /// 它们在建完页对象后调 `EnsureTabsSetup()`（= 原版 lazy `SetupTabs` 那一跳），再走各自的
        /// `Click(startVisual)` 落页（= 原版 `GetStartingTab`/当前页那一段）。</summary>
        public override void Open()
        {
            EnsureTabsSetup();
            base.Open();
            OpenTabs();
        }

        /// <summary>原版 `OpenTabs()`（:187-205）：当前页为空则取起始页并触发 `OnTabChanged`，然后**只重开当前页**。</summary>
        protected void OpenTabs()
        {
            if (tabs.Count <= 0) return;
            if (CurrentTabView == null)
            {
                CurrentTabView = GetStartingTab();
                CurrentTab = CurrentTabView != null ? CurrentTabView.Type : WindowTabType.None;
                if (OnTabChanged != null) OnTabChanged(CurrentTabView);
            }
            if (CurrentTabView != null) CurrentTabView.TryOpenTab();
        }

        /// <summary>原版 `GetStartingTab()`（:207-274）。见文件头差异③：没有 options 资产 ⇒ 回落第一页。</summary>
        protected virtual WindowTabBase GetStartingTab()
        {
            for (int i = 0; i < tabs.Count; i++) if (tabs[i] != null) return tabs[i];
            return null;
        }

        /// <summary>按**页类型**切页（原版 `ChangeTab&lt;TTab&gt;`，:276-306）。找不到 ⇒ 返回 null（并出声）。</summary>
        public TTab ChangeTab<TTab>() where TTab : WindowTabBase
        {
            var tab = GetTab<TTab>();
            if (tab == null)
            {
                Debug.LogWarning("[Tabs] `" + typeof(TTab).Name + "` 不在这扇窗的 `tabs` 里 ⇒ 没有切页（出声，不静默）");
                return null;
            }
            ChangeTab(tab);
            return tab;
        }

        /// <summary>按**页对象**切页（原版 `ChangeTab(WindowTabBase)`，:308-311）。</summary>
        public void ChangeTab(WindowTabBase tab)
        {
            ChangeTabCO(CurrentTabView, tab);
        }

        /// <summary>原版 `GetTab&lt;TTab&gt;`（:313-335）。</summary>
        public TTab GetTab<TTab>() where TTab : WindowTabBase
        {
            return GetTab(typeof(TTab)) as TTab;
        }

        /// <summary>原版 `GetTab(Type)`（:337-361）：先查缓存，未命中按「可赋值」扫。</summary>
        public WindowTabBase GetTab(Type type)
        {
            if (type == null) return null;
            WindowTabBase cached;
            if (_cachedTabs.TryGetValue(type, out cached)) return cached;
            for (int i = 0; i < tabs.Count; i++)
            {
                var t = tabs[i];
                if (t == null) continue;
                if (type.IsAssignableFrom(t.GetType())) return t;
            }
            return null;
        }

        /// <summary>原版 `ChangeTabCO`（:363-388）—— **逐句**：同页只重开；换页才关旧页 + `OnTabChange`；
        /// 两条路都触发 `OnTabChanged`（**在 `OnTabChange` 之后**）。</summary>
        void ChangeTabCO(WindowTabBase from, WindowTabBase to)
        {
            if (from == to && from != null)
            {
                CurrentTabView = from;
                CurrentTab = from.Type;
                from.TryOpenTab();
            }
            else
            {
                if (from != null) from.CloseTab();
                CurrentTabView = to;
                CurrentTab = to != null ? to.Type : WindowTabType.None;
                if (to != null) to.TryOpenTab();
                OnTabChange();
            }
            if (OnTabChanged != null) OnTabChanged(CurrentTabView);
        }

        /// <summary>原版 `OnTabChange()`（:390-392）。</summary>
        protected virtual void OnTabChange() { }

        /// <summary>原版 `Close()`（:394-401）：`base.Close()` **之后**还要关当前页（即使基类因「本来就没开」早退）。</summary>
        public override void Close()
        {
            base.Close();
            if (CurrentTabView != null) CurrentTabView.CloseTab();
        }

        /// <summary>原版 `ForceCloseTabs()`（:403-428）：关当前页 + **所有**页 `SetActive(false)`；**不清 `CurrentTab`**。</summary>
        public void ForceCloseTabs()
        {
            if (CurrentTabView != null) CurrentTabView.CloseTab();
            for (int i = 0; i < tabs.Count; i++)
                if (tabs[i] != null) tabs[i].gameObject.SetActive(false);
        }

        /// <summary>原版 `ToFocus()`（:430-441）：当前页 `ToFocus()` + 触发 `OnTabChanged`。
        /// 🆕 2026-10-25：`GameWindow` 现在有 `public virtual void ToFocus()`（原版虚表 `+0x1a8`）⇒ 这是 **override**。</summary>
        public override void ToFocus()
        {
            if (CurrentTabView != null) CurrentTabView.ToFocus();
            if (OnTabChanged != null) OnTabChanged(CurrentTabView);
        }

        /// <summary>原版 `CreateTabButton`（:443-457）：给一页挂一颗左栏键。
        /// ⚠️ 我们的 `TabButtons.AddTabButton` 只有「名字」这一半（原版还有 `Sprite` 图标）—— 见文件头差异⑤。</summary>
        public WindowButton CreateTabButton(WindowTabBase tabInstance, string tabName)
        {
            if (tabButtons == null)
            {
                Debug.LogWarning("[Tabs] `CreateTabButton`：本窗没有 `tabButtons` ⇒ 建不出键（出声，不静默）");
                return null;
            }
            var button = tabButtons.AddTabButton(tabName);
            if (button != null) tabButtons.AssignTabButton(tabInstance, button);
            return button;
        }

        /// <summary>原版 `DestroyTab`（:553-568）。判据 = `GameWindowWithTabs__DestroyTab.c` 全文（四句 + 一个 `try-get`）：
        /// ① `tabs.Remove(tab)`；② `tabButtons.RemoveTabButton(tab)`；③ **「缓存里那一条就是这一页」才摘键**
        /// （`Dictionary.TryGetValue(type, &cached)` 命中 **且** `Object.op_Equality(cached, tab)` 才 `Remove(type)`）；
        /// ④ `Destroy(tab.gameObject)`。
        /// <para>🔴 **2026-10-27（E9b · 账 318）就地订正（铁律 5）**，本轮改掉两处与原文不符的地方：
        /// · ③ 原来**无条件** `_cachedTabs.Remove(tab.GetType())` —— 若同类型的缓存条目是**另一个实例**
        ///   （重建过的那种），原版**不摘**、我们会把活的那条也摘掉 ⇒ 改成「命中且同引用」才摘。
        /// · ⑤ 原版 **不清 `CurrentTab` / `CurrentTabView`**（留悬空 —— 靠 Unity 的「已销毁对象 == null」语义自然失效）；
        ///   我们原来主动置 `null` / `None` ⇒ 「销毁当前页之后读 `CurrentTab`」两侧值不一样。已按原版删掉。</para></summary>
        public void DestroyTab(WindowTabBase tab)
        {
            if (tab == null) return;
            tabs.Remove(tab);
            WindowTabBase cached;
            if (_cachedTabs.TryGetValue(tab.GetType(), out cached) && ReferenceEquals(cached, tab))
                _cachedTabs.Remove(tab.GetType());      // ③ 只有「就是这一页」才摘键
            if (tabButtons != null) tabButtons.RemoveTabButton(tab);
            // ⑤ 原版**不清** `CurrentTab` / `CurrentTabView`（见 doc）—— ⛔ 别再加回来
            if (Application.isPlaying) Destroy(tab.gameObject);
            else DestroyImmediate(tab.gameObject);
        }

        // ======================================================== 历史入口（枚举版）

        /// <summary>换页（历史入口 · 枚举）。**行为与原版对齐后的实现**：找到枚举对应的页对象 ⇒ 走 `ChangeTabCO`
        /// （旧页 `CloseTab()` + 新页 `TryOpenTab()` + `OnTabChange` + `OnTabChanged`），再刷左栏高亮。
        /// <para>⚠️ 保留了两条旧保证：① `type == None` ⇒ **所有页关掉**（仓里 `MainMenuScene` 的负例探针在断这个）；
        /// ② 找不到对应页 ⇒ 所有页关掉 + `CurrentTab = type`（旧行为，**不触发** `OnTabChanged`）。</para></summary>
        public void ChangeTab(WindowTabType type)
        {
            if (type == WindowTabType.None)
            {
                if (CurrentTabView != null) CurrentTabView.CloseTab();
                CurrentTabView = null;
                CurrentTab = WindowTabType.None;
                for (int i = 0; i < tabs.Count; i++)
                    if (tabs[i] != null) tabs[i].gameObject.SetActive(false);
                if (OnTabChanged != null) OnTabChanged(null);
                OnTabChange();
                RefreshHighlights();
                return;
            }

            var to = FindTab(type);
            if (to == null)
            {
                for (int i = 0; i < tabs.Count; i++)
                    if (tabs[i] != null) tabs[i].gameObject.SetActive(false);
                CurrentTabView = null;
                CurrentTab = type;
                RefreshHighlights();
                return;
            }

            // 旧保证：同一时刻只留一页活着（原版 `ChangeTabCO` 只关 `from`，这里防脏态）
            for (int i = 0; i < tabs.Count; i++)
            {
                var t = tabs[i];
                if (t != null && t != to && t.gameObject.activeSelf) t.gameObject.SetActive(false);
            }
            ChangeTabCO(CurrentTabView, to);
            RefreshHighlights();
        }

        WindowTabBase FindTab(WindowTabType type)
        {
            for (int i = 0; i < tabs.Count; i++)
                if (tabs[i] != null && tabs[i].Type == type) return tabs[i];
            return null;
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

        /// <summary>自检壳：原版 `OpenTabs` 是 `protected`，`Editor/ShellScene.cs` 调不到 ⇒ 给一条只转发的口。
        /// ⚠️ **零逻辑**（同 `TabButtons.ToggleForTest` / `FirstTimeGateForTest` 的先例）；
        /// 追加在**类尾**以免动到账引用的行号。</summary>
        public void OpenTabsForTest() { OpenTabs(); }

        // ======================================================== 原版 `CreateTab<T>`（2026-10-28 · B92_N5d）
        // 判据 = `decomp_full/GameWindowWithTabs__CreateTab_object_.c`（94 行，全文现读）。
        // 原版签名（类桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/GameWindowWithTabs.cs:83`）：
        //   `public (T, EverguildToggle) CreateTab<T>(string tabName, Sprite icon, T tabPrefab = null) where T : WindowTabBase`
        // ⚠️ 与文件头差异④同源：原版 `tabPrefab` 住在 `TabbedWindowComponents`（`tabButtons` / `tabPrefab` / `tabHolder` 三格），
        //   我们把它摊成窗口自己的字段（前两格早有，本轮补第三格）—— ⛔ **不为此新建 `TabbedWindowComponents` 空壳类**
        //   （本仓没有那条 prefab 数据链，建了就是零使用者的空壳）。
        // 🔴 `.c` 里 `GetComponentFastPath` 之后那句 `param_5 = 0` 是**反编译器把 out 写丢了**（同「ILSpy 丢实参」那一族）
        //   ⇒ 真语义 = `tabPrefab = Components.TabPrefab.GetComponent<T>()`。
        // 🔴 末尾 `if (*(this+0x88) != 0) inst.<vtable+0x188>(this, …)`：`0x88` 那个 bool 与 slot `0x188` 的语义
        //   **本机解不出**（缺 `WindowTabBase` 的 vtable 表）；从参数形状 `(this)` 看是「把本窗交给页」⇒
        //   按 `Setup(GameWindow)`（`Shell/WindowTabBase.cs:45`，正文 `baseWindow = window; OnSetup();`）落，
        //   ⚠️ **这一格是推断，如实标注**（不是逐句）。

        /// <summary>`TabbedWindowComponents.tabPrefab` 的等价物（原版那颗页母版 GameObject）。
        /// <para>⚠️ **声明在类尾**：本文件在账里有 16 个 `文件:行号` 落点（`:80`…`:249`），往前面插字段会让它们整体漂移。</para></summary>
        public GameObject tabPrefab;

        /// <summary>建页时要不要 `tab.Setup(this)` —— 对应原版 `CreateTab` 末尾那个 `if (*(this+0x88) != 0)` 闸。
        /// 默认 `true`（页对象不绑窗口就没法用）；原版那个闸的**默认值本机解不出**，见上面那段注释。</summary>
        public bool setupTabOnCreate = true;

        /// <summary>原版 **`CreateTab&lt;T&gt;(string tabName, Sprite icon, T tabPrefab = null)`**：
        /// ① `tabPrefab == null` ⇒ 取 <see cref="tabPrefab"/> 母版上的 `T`（原版 `GetComponentFastPath(typeof(T))`）；
        /// ② `Instantiate(母版, Components.TabHolder)`；
        /// ③ `SetActive(true)` → `name = tabName` → `tabs.Add(inst)`；
        /// ④ `CreateTabButton(inst, tabName)`（原版还有 `icon`；我们的 `TabButtons.AddTabButton` 只有名字这一半 —— 差异⑤）；
        /// ⑤ 闸开 ⇒ `inst.Setup(this)`；
        /// ⑥ 返回 `(tab, button)`（原版是 `(T, EverguildToggle)` 元组）。
        /// <para>⛔ 每一处取不到都**出声**（红线：不许静默失败）；原版同格是 NRE。</para>
        /// <para>⚠️ `icon` 今天**没有去路**（`TabButtons.AddTabButton` 不吃图标）—— 留着形参是为了与原版签名同形，⛔ 别删。</para></summary>
        public (T tab, WindowButton button) CreateTab<T>(string tabName, Sprite icon = null, T tabPrefab = null)
            where T : WindowTabBase
        {
            if (tabPrefab == null)
            {
                if (this.tabPrefab == null)
                {
                    Debug.LogWarning("[Tabs] `CreateTab<" + typeof(T).Name + ">(" + (tabName ?? "?") + ")`："
                                   + "既没传母版、本窗也没有 `tabPrefab`（原版这一格走 `Components.TabPrefab`，取不到即 NRE）⇒ 不建");
                    return (null, null);
                }
                tabPrefab = this.tabPrefab.GetComponent<T>();
                if (tabPrefab == null)
                {
                    Debug.LogWarning("[Tabs] `CreateTab<" + typeof(T).Name + ">`：母版 `" + this.tabPrefab.name
                                   + "` 上没有 `" + typeof(T).Name + "` 组件（原版 `GetComponentFastPath` 之后直接 NRE）⇒ 不建");
                    return (null, null);
                }
            }
            var parent = tabHolder != null ? tabHolder : transform;   // 原版 `Components.TabHolder`
            var inst = Instantiate(tabPrefab, parent);                // 原版 `Object.Instantiate<T>(prefab, tabHolder)`
            inst.gameObject.SetActive(true);                          // 原版 `SetActive(true)`
            inst.name = tabName;                                      // 原版 `set_name(inst, tabName)`
            tabs.Add(inst);                                           // 原版 `List<WindowTabBase>.Add`
            var button = CreateTabButton(inst, tabName);              // 原版 `CreateTabButton(this, inst, tabName, icon)`
            if (setupTabOnCreate) inst.Setup(this);                   // 原版 `if (+0x88) inst.<vtable+0x188>(this)`
            return (inst, button);
        }
    }
}
