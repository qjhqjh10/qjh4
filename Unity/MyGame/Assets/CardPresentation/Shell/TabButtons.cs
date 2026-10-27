// TabButtons.cs — 左栏页签容器（原版同名类）。
// 🔴 **2026-10-13 从 `RewardsWindow.cs` 拆出**（A1506：Unity 要求 MonoBehaviour 的**类名与所在文件名相同**，
//    否则建不出脚本资产 ⇒ 场景存盘会把它烘成内联 `MonoScript` 存根 ⇒ player 加载即崩）。
//    判据 / 出处 / 纪律见 `RewardsWindow.cs` 的文件头（本块**逐字搬**，未改一字）。
// 🆕 **2026-10-25（复刻第 3 轮 · 代理 H）：补齐原版页签按键生命周期**。
//    判据 = `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/TabButtons.cs`（:83-492）：
//      · `Initialize(window)`（:83-267）= 母版 `SetActive(false)` → `window.OnTabChanged += UpdateTab`
//        → 给 `options` 每颗 toggle 挂 `Toggle(state, tab)` 监听（**先 Remove 再 Add**）
//      · `Toggle(state, tab)`（:269-282）= **`firstTime` 闸**：首次调用只把闸放掉；之后 `state && tab` ⇒ `window.ChangeTab(tab)`
//      · `UpdateTab(tab)`（:284-312）= 逐颗 `toggle.isOn = (option.tab == tab)`
//      · `AddTabButton(name, icon)`（:314-407）= 实例化母版 → 激活 → `SetSiblingIndex(0)` → `name = tabName`
//        → 图标 → `group = group` → TMP 文字 = name
//      · `AssignTabButton(tab, toggle)`（:409-438）= `options.Add(new TabButton{tab,toggle})`
//      · `RemoveTabButton(tab)`（:440-466）= 找到第一条同 tab 的 → 从表里删 → `Destroy(按钮)`（**没有就不报错**）
//      · `ToggleInteractable(tab, state)`（:468-492）= 找到第一条同 tab 的 → `Selectable.interactable = state`
//    ⚠️ **与原版的差异（如实记，⛔ 不写成原版行为）**：
//      ① 我们把 `options` 的形状**扩了一点**：原版 `TabButton { WindowTabBase tab; Toggle toggle; }`；我们是
//         `Option { WindowTabType type; WindowButton button; WindowTabBase tab; }` —— 前两个字段是**历史字段**
//         （`MainMenuSubmenuWindow.BuildBar` / `ChatPanel` 在写），`tab` 是**这次补的原版字段**（可空）。
//      ② 原版给每颗 toggle 挂**点击/状态**监听；我们的 `WindowButton.onClick` 在 `BuildBar` 里**已经接了**
//         `Click(visualIndex)` ⇒ 这里**不再改 onClick**（改了会把那条生产接线顶掉）。`Toggle(state, tab)` 仍按原版
//         实现并可被直调（下一轮接实例时用它替换 `Click` 那条路）。⚠️ 这是**本轮唯一没接上的那一跳**，见报告。
//      ③ `firstTime`：原版由「group 归一化触发的初始 toggle 事件」消费掉。我们没有 toggle 事件 ⇒
//         **`Initialize` 末尾直接把它消费掉**（否则用户第一次真点击会被吞）。依据：原版 `Toggle` 的
//         `firstTime` 闸只在 `Initialize` 之后有实际意义（:269-282），而它是为初始化那一跳准备的。
//      ④ ~~`AddTabButton` 原版收 `Sprite icon` 并设 `image.sprite`；我们这边图是**按名字**取（`CardArt.Ui(...)`），
//         所以本轮只提供 `AddTabButton(string tabName)`；**图标那一半没做**~~
//         🔴 **2026-10-27（第三批 · E9c）就地订正（铁律 5）：已补上** —— 形参改成
//         `AddTabButton(string tabName, Texture2D tabIcon = null)`，取到 `ImageQuad` 就 `SetTexture`；
//         取不到就**出声**。`Toggle.group` 那一跳换载体（`options` + `ChangeTab`），⛔ 不是缺。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
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
            /// <summary>历史字段（`MainMenuSubmenuWindow.BuildBar` / `ChatPanel` 写）。</summary>
            public WindowTabType type;
            /// <summary>历史字段：我们只用它的点击；原版这里是 `Toggle`。</summary>
            public WindowButton button;
            /// <summary>🆕 原版字段 `TabButton.tab`（`TabButtons.cs:15`）。可空（历史 Option 没有它）。</summary>
            public WindowTabBase tab;
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

        /// <summary>原版 `firstTime`（`TabButtons.cs:79`）。见文件头差异③。</summary>
        bool _firstTime = true;
        GameWindowWithTabs _subscribed;

        /// <summary>原版 `TabButtons.Initialize`（:83-267）：关母版 + 订阅 `OnTabChanged` + 逐颗挂监听。
        /// <para>🔴 **第一件就是关掉母版** —— 出处 `d:/2/tools/decomp_full/TabButtons__Initialize.c:35-44`（亲读指令流复核）：
        /// `op_Inequality(tabButtonPrefab, null)` → `Component__get_gameObject` → `GameObject__SetActive(go, 0)`。
        /// ⚠️ **2026-09-23 修（铁律 5·b）**：原来我们把第 4 键当**常显**的画出来了 —— 与实况不符。</para>
        /// <para>⚠️ **可重复调用**：同一窗重复 `Initialize` 不会重复订阅（先摘旧的）。`SetupTabs` 只调一次。</para></summary>
        public void Initialize(GameWindowWithTabs w)
        {
            if (_subscribed != null && _subscribed != w && _subscribed.OnTabChanged != null)
                _subscribed.OnTabChanged -= UpdateTab;
            window = w;
            if (w != null)
            {
                if (w.OnTabChanged == null || !HasUpdateTab(w.OnTabChanged)) w.OnTabChanged += UpdateTab;
                _subscribed = w;
            }
            if (tabButtonPrefab != null) tabButtonPrefab.SetActive(false);   // 照原版：母版隐藏
            // 见文件头差异③：原版这一跳由 toggle 初始事件消费，我们没有事件 ⇒ 在这里消费
            _firstTime = false;
        }

        bool HasUpdateTab(System.Action<WindowTabBase> ev)
        {
            if (ev == null) return false;
            var list = ev.GetInvocationList();
            for (int i = 0; i < list.Length; i++)
                if (list[i] != null && list[i].Target == this && list[i].Method.Name == "UpdateTab") return true;
            return false;
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

        // ======================================================== 原版生命周期（2026-10-25 补）

        /// <summary>原版 private `Toggle(bool state, WindowTabBase tab)`（:269-282）。
        /// <para>`_firstTime` 那一刻**什么都不做**（只把闸放掉）；之后 `state && tab != null` ⇒ `window.ChangeTab(tab)`。</para></summary>
        void Toggle(bool state, WindowTabBase tab)
        {
            if (!_firstTime)
            {
                if (state && tab != null)
                {
                    if (window != null) window.ChangeTab(tab);
                    else Debug.LogWarning("[Tabs] `Toggle` 时 `window` 是 null ⇒ 切页没发生（出声，不静默）");
                }
                return;
            }
            _firstTime = false;
        }

        /// <summary>自检/下一轮接线用的**公开壳**（原版是 private，由 toggle 监听调）。
        /// ⚠️ 不要在生产路径上绕过 `window.OnTabChanged` 直接调它 —— 它只负责「按键 → 切页」这一跳。</summary>
        public void ToggleForTest(bool state, WindowTabBase tab) { Toggle(state, tab); }
        /// <summary>自检读口：`firstTime` 闸还在不在（应为 false = 已消费）。</summary>
        public bool FirstTimeGateForTest { get { return _firstTime; } }

        /// <summary>原版 `UpdateTab(WindowTabBase)`（:284-312）：按 `window.CurrentTabView` 同步本容器的选中态。
        /// <para>⚠️ 我们不写 `toggle.isOn`（没有 uGUI）：这里只同步**历史读口** `CurrentType` / `CurrentVisualIndex`
        /// —— 左栏高亮仍由各窗 `RefreshHighlights()` 画（它读的就是这两个字段）。</para></summary>
        void UpdateTab(WindowTabBase tab)
        {
            if (window == null) { CurrentType = WindowTabType.None; CurrentVisualIndex = -1; return; }
            if (tab == null) { CurrentType = WindowTabType.None; CurrentVisualIndex = -1; return; }
            CurrentType = tab.Type;
            CurrentVisualIndex = window.visualTypes != null ? window.visualTypes.IndexOf(tab.Type) : -1;
        }

        /// <summary>= 原版 `TabButtons.AddTabButton(string tabName, Sprite tabIcon)`（**逐句**，判据
        /// `TabButtons__AddTabButton.c` 全文 = `out_cpp2il_dev_ilspy/TabButtons.cs:314-407`）：
        /// ① `Instantiate(tabPrefab, tabHolder.transform)` → ② `SetActive(true)` → ③ `SetSiblingIndex(0)`
        /// → ④ `name = tabName` → ⑤ **`image.sprite = tabIcon`**（`GetComponentInChildren&lt;Image&gt;`，取不到就跳过）
        /// → ⑥ `toggle.group = tabHolder` → ⑦ 子件 TMP `SetText(tabName)` → ⑧ 返回那颗 toggle。
        /// <para>🔴 **2026-10-27（第三批 · E9c）补齐 ⑤**（原来只有名字那一半，见文件头差异④）：形参改成
        /// `Texture2D tabIcon` —— 我们这一栈的「Image」是 `ImageQuad`（`Image.sprite` 在我们这儿就是
        /// `SetTexture`），**图仍按名字取**（`CardArt.Ui(...)`）只是**取到之后从这里喂进去**。
        /// ⚠️ 母版里没有 `ImageQuad` 而调用方又给了图标 ⇒ **出声**（⛔ 不静默吞掉一张图）。</para>
        /// <para>⑥ 的等价物：我们没有 uGUI `Toggle`/`ToggleGroup` ⇒ **互斥选中**由 `options` 表 +
        /// `window.ChangeTab` 那条路实现（`Toggle(state, tab)` 已按原版实现，`firstTime` 闸见文件头差异③）
        /// ⇒ 这一跳不是缺，是换了载体。</para></summary>
        public WindowButton AddTabButton(string tabName, Texture2D tabIcon = null)
        {
            if (tabButtonPrefab == null)
            {
                Debug.LogWarning("[Tabs] `AddTabButton`：本窗没有 `tabButtonPrefab` 母版 ⇒ 建不出键（出声，不静默）");
                return null;
            }
            var go = Instantiate(tabButtonPrefab, transform);
            go.SetActive(true);
            go.transform.SetSiblingIndex(0);       // 原版 :339 `SetSiblingIndex(0)`
            go.name = tabName;                     // 原版 :340 `everguildToggle.name = tabName`
            // ⑤ `image.sprite = tabIcon`（原版 `UnityEngine_UI_Image__set_sprite(plVar8, param_3)`）
            if (tabIcon != null)
            {
                var icon = go.GetComponentInChildren<ImageQuad>(true);
                if (icon != null) icon.SetTexture(tabIcon);
                else Debug.LogWarning("[Tabs] `AddTabButton(" + tabName + ")`：母版里没有 `ImageQuad`"
                                    + " ⇒ **图标那一跳没落**（原版 `image.sprite = tabIcon`；出声，不静默）");
            }
            var label = go.GetComponentInChildren<Label>(true);
            if (label != null) label.SetText(tabName);
            else Debug.LogWarning("[Tabs] `AddTabButton(" + tabName + ")`：母版里没有 `Label` ⇒ 键上没有文字（出声，不静默）");
            var btn = go.GetComponentInChildren<WindowButton>(true);
            if (btn == null)
            {
                Debug.LogWarning("[Tabs] `AddTabButton(" + tabName + ")`：母版里没有 `WindowButton` ⇒ 这颗键点不动"
                               + "（出声，不静默）");
            }
            return btn;
        }

        /// <summary>原版 `AssignTabButton(tab, toggle)`（:409-438）：把页与键登记进 `options`。</summary>
        public void AssignTabButton(WindowTabBase tab, WindowButton button)
        {
            if (tab == null) return;
            options.Add(new Option { type = tab.Type, button = button, tab = tab });
        }

        /// <summary>原版 `RemoveTabButton(tab)`（:440-466）：找到第一条同页的 → 摘表 → 销毁键。
        /// **找不到就什么都不做**（原版 `FirstOrDefault` 为 null 即跳过，不出声）。</summary>
        public void RemoveTabButton(WindowTabBase tab)
        {
            if (tab == null) return;
            for (int i = 0; i < options.Count; i++)
            {
                var o = options[i];
                if (o == null) continue;
                if (o.tab != tab && o.type != tab.Type) continue;
                options.RemoveAt(i);
                if (o.button != null)
                {
                    if (Application.isPlaying) Destroy(o.button.gameObject);
                    else DestroyImmediate(o.button.gameObject);
                }
                return;
            }
        }

        /// <summary>原版 `ToggleInteractable(tab, state)`（:468-492）：找到第一条同页的键，改 `Selectable.interactable`。
        /// 我们的等价物 = `WindowButton.Interactable`（同一个语义：置灰 + 不可点）。</summary>
        public void ToggleInteractable(WindowTabBase tab, bool state)
        {
            if (tab == null) return;
            for (int i = 0; i < options.Count; i++)
            {
                var o = options[i];
                if (o == null || o.button == null) continue;
                if (o.tab != tab && o.type != tab.Type) continue;
                o.button.Interactable = state;
                return;
            }
        }

        /// <summary>自检读口：某页对应的键（没有 ⇒ null）。</summary>
        public WindowButton ButtonFor(WindowTabBase tab)
        {
            if (tab == null) return null;
            for (int i = 0; i < options.Count; i++)
            {
                var o = options[i];
                if (o == null) continue;
                if (o.tab == tab || o.type == tab.Type) return o.button;
            }
            return null;
        }
    }
}
