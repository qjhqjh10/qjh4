// MilestoneLocalSimulation.cs —— 「5 档里程碑」的**本地模拟表**（两扇活动窗的记分条共用）
//
// ============================ 这是什么 ============================
// 原版 `MilestoneScoreBar.Initialize(int currentScore, GenericScoringMilestone[] scoringMilestones, Sprite resourceIcon)`
// 的第 2 个实参**唯一来源 = LiveOps 服务端**（`ScoringEvent<TS,TD>` 的里程碑表；本仓等价物见
// `Shell/ScoringEvent.cs` 的 `GenericScoringMilestone`，消费点 = `Shell/RewardProgressBarPanel.cs` 的
// `RewardProgressBarPanel.Initialize`）。服务器已关服、13 份活动 SO 全部过期、
// `Shell/LiveOpsLocalCatalog.cs` 里**没有任何** milestone 数据（现读 0 命中）⇒ 判据在远端、本机永远拿不到。
//
// 🔶 自己设计（非原版）：两扇活动窗（`Shell/SkirmishEventWindow.cs` / `Shell/EnergySinglePlayerOnlyEventWindow.cs`）
//    的 5 档里程碑表 · 为什么必须自定：服务器已关服，`GenericScoringMilestone[]` 的判据在远端 LiveOps 事件实例上 ·
//    调度台 2026-10-29 裁定（`资料/完全复刻/待接线与排队项.md` 的 `未分类:430` 那一行）
//
// ============================ 取值口径（为什么是这几个数）============================
// 这张表只做一件事：让「**把真组件接上**」这一步**不改变任何可见像素** ——
//   · `score = 1256`：原版 prefab 里 5 行 `Score` 的 TMP **出厂原文都是 `'1256'`**
//     （判据 = `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "SkirmishModeEventWindow"
//       --depth 13 --relative --no-sprite`（同命令换 `"EnergySinglePlayerOnlyEventWindow"`）：
//       `Score Bar Line Level 1..5 ▸ Skull ▸ Score` 逐颗 `'1256'`）。
//     `GenericScoreBarLine.Initialize` 会把 `milestone.score` 写进那一格 ⇒ 取 1256 才能与改前逐字相同。
//   · `state = Unlocked`：prefab 出厂 `Highlight Crate` 的 `act = 1`（金色高亮**是亮着的**，
//     `Simple (0.98,0.801,0.0588,1)`）⇒ 原版出厂态就是「可领」。`GenericScoreBarLine.Initialize` 的
//     `Unlocked` 支 = **闭箱图**（`originalChest`）+ 高亮开，与今天两扇窗画的东西逐件相同
//     （`SkirmishEventWindow` 那颗高亮的 tint 我们本来画 alpha 0 ⇒ 开/关都看不见；
//       `EnergySinglePlayerOnlyEventWindow` 那颗画的是不透明金 ⇒ 保持开着）。
//   · `rewards = 空数组`：本地没有奖励对象（原版 `RewardService.Preview` 那一跳本来就走远端 LiveOps）。
//   · `currentScore = 0`：出厂 `Fill` 现读 0 宽（= 进度 0）⇒ `InterpolateMilestones(0, …) = 0`。
// ⛔ **别把这张表当成原版数据** —— 原版这一格来自服务器；接上真数据时把调用点换成服务端那份表即可。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation.ScoreBars
{
    /// <summary>`GenericScoringMilestone[]` 的本地模拟（判据/口径见文件头）。</summary>
    public static class MilestoneLocalSimulation
    {
        /// <summary>`Score` 那一格的 prefab 出厂原文（= 原版 5 行 TMP 的 `m_text`）。</summary>
        public const int FactoryScore = 1256;

        /// <summary>出厂进度 = 0（`Fill` 现读 0 宽）。</summary>
        public const int CurrentScore = 0;

        /// <summary>行数 = prefab 的 `scoringLines` 长度（`Score Bar Line Level 1..5`）。</summary>
        public const int LineCount = 5;

        static bool _warned;

        /// <summary>镜像 prefab 出厂态的 5 档表（口径见文件头）。</summary>
        public static GenericScoringMilestone[] CreateTable()
        {
            var a = new GenericScoringMilestone[LineCount];
            for (int i = 0; i < a.Length; i++)
                a[i] = new GenericScoringMilestone
                {
                    score = FactoryScore,
                    state = GenericScoringMilestoneState.Unlocked,      // = 出厂「高亮亮着」
                    rewards = Array.Empty<CampaignData.RewardSpec>(),
                };
            return a;
        }

        /// <summary>原版 `LiveOpsEventWindow.RefreshScore` 里那条**状态投影**：逐档算 `GenericScoringMilestone`。
        /// <para>判据 = `decomp_full/SkirmishEventWindow.__c__DisplayClass18_0___RefreshScore_b__0.c:22-30`：
        /// `currentScore &lt; scoreToUnlock ⇒ Locked`，否则 `i &lt; collectedRewards ? Collected : Unlocked`
        /// （同形实现见 `Shell/SkirmishEventWindow.cs` 的 `MilestoneStates` —— 本方法只是把它包成
        /// `GenericScoringMilestone[]`，⛔ 规则不写第二份）。</para>
        /// <para>⚠️ 这份**表的数据本身仍是服务器来的**（`thresholds` / `collectedRewards` 由调用方给）——
        /// 本方法只做「投影」这一跳，不发明数值。</para></summary>
        public static GenericScoringMilestone[] FromScore(int currentScore, IList<int> thresholds, int collectedRewards)
        {
            int n = thresholds == null ? 0 : thresholds.Count;
            var a = new GenericScoringMilestone[n];
            for (int i = 0; i < n; i++)
            {
                int state = currentScore < thresholds[i] ? 0 : (i < collectedRewards ? 2 : 1);
                a[i] = new GenericScoringMilestone
                {
                    score = thresholds[i],
                    state = (GenericScoringMilestoneState)state,
                    rewards = Array.Empty<CampaignData.RewardSpec>(),
                };
            }
            return a;
        }

        /// <summary>出声一次（每扇窗各一次没必要 ⇒ 全进程一次）：说明屏幕上那个 5 档表是**模拟的**。</summary>
        public static void LogOnce(string who)
        {
            if (_warned) return;
            _warned = true;
            Debug.LogWarning("[" + who + "] 记分条的 5 档里程碑用的是**本地模拟表**"
                           + "（`🔶 自己设计`，见 `Shell/MilestoneLocalSimulation.cs`）—— "
                           + "原版 `MilestoneScoreBar.Initialize` 的 `GenericScoringMilestone[]` 由 LiveOps 服务端下发，"
                           + "服务器已关服、本机 0 份活动数据 ⇒ 取值**镜像 prefab 出厂态**"
                           + "（score " + FactoryScore + " / state Unlocked / 进度 " + CurrentScore + "），"
                           + "⛔ 不是原版数据。");
        }

        /// <summary>自检用：清掉「已出声」标记。</summary>
        public static void ClearForTest() { _warned = false; }
    }
}
