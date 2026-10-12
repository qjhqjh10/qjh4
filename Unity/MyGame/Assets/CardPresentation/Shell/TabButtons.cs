// TabButtons.cs — 左栏页签容器（原版同名类）。
// 🔴 **2026-10-13 从 `RewardsWindow.cs` 拆出**（A1506：Unity 要求 MonoBehaviour 的**类名与所在文件名相同**，
//    否则建不出脚本资产 ⇒ 场景存盘会把它烘成内联 `MonoScript` 存根 ⇒ player 加载即崩）。
//    判据 / 出处 / 纪律见 `RewardsWindow.cs` 的文件头（本块**逐字搬**，未改一字）。
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
}
