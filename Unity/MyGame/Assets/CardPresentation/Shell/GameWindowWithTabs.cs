// GameWindowWithTabs.cs — 带页签的窗口（原版同名类，`GameWindowWithTabs : GameWindow`）。
// 🔴 **2026-10-13 从 `RewardsWindow.cs` 拆出**（A1506：Unity 要求 MonoBehaviour 的**类名与所在文件名相同**，
//    否则建不出脚本资产 ⇒ 场景存盘会把它烘成内联 `MonoScript` 存根 ⇒ player 加载即崩）。
//    判据 / 出处 / 纪律见 `RewardsWindow.cs` 的文件头（本块**逐字搬**，未改一字）。
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
}
