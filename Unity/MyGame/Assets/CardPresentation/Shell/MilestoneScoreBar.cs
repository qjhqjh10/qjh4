// MilestoneScoreBar.cs —— 原版 `MilestoneScoreBar`（MonoBehaviour：竖/横进度条 + N 行里程碑）
//
// ============================ 这是什么 ============================
// 「账号级记分条」：一条 `Slider` 进度 + 一排里程碑行（`GenericScoreBarLine`，prefab 里 5 行）。
// 三个宿主窗（B6 已证，⛔ 不是 `AllianceScoreBar` 的别名）：
//   `SkirmishModeEventWindow / Reward Display / Scoring Bar Event Score Info`
//   `EnergySinglePlayerOnlyEventWindow / Reward Progress Panel / Scoring Bar Event Score Info`
//   `Two Sides Event Window / Event On Going Tab / Reward Display / Scoring Bar Event Score Info`
//
// ============================ 判据（三源现读，2026-10-29 B17）============================
//   · 字段/签名 = `d:/2/tools/il2cpp_out/dump.cs:111238-111262`
//       `[SerializeField] private Slider progressBar;                  // 0x20`  ← 节点 `Progress Bar`
//       `[SerializeField] private GenericScoreBarLine[] scoringLines;  // 0x28`  ← 5 行 `Score Bar Line Level 1..5`
//       `public void Initialize(int currentScore, GenericScoringMilestone[] scoringMilestones, Sprite resourceIcon)`
//       `private void SetScoringMilestones(...)` / `SetProgressBar(...)` / `int[] GetScoringThresholds(...)`
//     🔴 **`progressBar` 是 `Slider`、不是 `ProgressBar`** —— B6 的判据文件 §5 写成 `PPtr<ProgressBar>`，
//        那是**误读**（`dump.cs` 明写 `private Slider progressBar; // 0x20`，而 `SetProgressBar` 里读写的
//        `wholeNumbers / maxValue / normalizedValue` 也全是 `Slider` 的成员）。⛔ 别照那句去接一个 `ProgressBar`。
//     🔴 本类**没有 `resourceIcon` 字段**（3 个实例逐个读，字段只有那两格）—— 但 `Initialize` **有**那个形参
//        （它只往下转给每一行）。「方法有实参 ≠ prefab 存字段」，照建时⛔别去找那颗 icon 引用。
//   · 方法体 = `d:/2/tools/decomp_full/MilestoneScoreBar__{Initialize,SetProgressBar,SetScoringMilestones}.c`
//       `Initialize`：
//         `SetProgressBar(milestones, currentScore);` 然后**内联**一份与 `SetScoringMilestones` 相同的循环
//         （⇒ `SetScoringMilestones` 在本 build **没有调用者**，见下面那个方法的注释）
//       `SetProgressBar`：
//         `progressBar.wholeNumbers = false;`
//         `progressBar.maxValue = thresholds.LastOrDefault();`                 ← 空表 ⇒ 0
//         `progressBar.normalizedValue = SupportMethods.InterpolateMilestones(
//                                          currentScore, thresholds.Prepend(0).ToList());`
//       `GetScoringThresholds`：`milestones == null || Length == 0 ⇒ 空表`，否则 `Select(m => m.score).ToArray()`
//       🔴 那句 `InterpolateMilestones` 是**按档位等分**的，**不是** `current/max` 线性 ——
//          判据与唯一实现 = `CardPresentation.AllianceEventScoreInfo.InterpolateMilestones`（⛔ 别在这里再写一份）。
//   · 越界那句错误串（ASCII 逐字，`stringliteral.json` 地址 `0x42a1ae0`）：
//       `"[ERROR] Alliances scoring bar: There are more scoring lines that thresholds"`
//
// ⚠️ 差异（如实标）：
//   ① `progressBar` 保留 uGUI `UnityEngine.UI.Slider`（与 `dump.cs` 同类型）；本工程菜单线没有 uGUI 运行时
//      （A92 普查）⇒ 网格那一档的宿主走 `AllianceEventScoreInfo.SetMilestones`（既有实现）。
//   ② 原版 `scoringLines == null` / `progressBar == null` / 数组元素 null 都是 **NRE**；
//      我们出声 + 跳过（全工程口径：坏引用不让游戏崩）。
using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation.ScoreBars
{
    /// <summary>原版 `MilestoneScoreBar`：进度条 + N 行里程碑。
    /// <para>⚠️ 命名空间是 `CardPresentation.ScoreBars` —— `CardPresentation` 下**已经有一个同名的非 MonoBehaviour**
    /// `MilestoneScoreBar`（`Shell/RewardProgressBarPanel.cs:39`，那是 `RewardProgressBarPanel` 的纯数据等价物）。
    /// 两个类**不同命名空间、互不遮蔽**；本类的 `GetScoringThresholds` **直接转发**给那一份（⛔ 不抄第二份规则）。</para></summary>
    public class MilestoneScoreBar : MonoBehaviour
    {
        /// <summary>「行数多于阈值」那句错误串（原版 ASCII 逐字，含 `that` 那个语法错）。</summary>
        public const string ErrorMoreLinesThanThresholds =
            "[ERROR] Alliances scoring bar: There are more scoring lines that thresholds";

        // 🔴 字段名逐字照 `dump.cs`（断言按反射钉死）。
        [SerializeField] private Slider progressBar;                       // 原版 `+0x20`（节点 `Progress Bar`）
        [SerializeField] private GenericScoreBarLine[] scoringLines;       // 原版 `+0x28`（5 行）

        /// <summary>绑宿主建好的两件（原版由 prefab 序列化给）。</summary>
        public void Bind(Slider bar, GenericScoreBarLine[] lines)
        {
            progressBar = bar;
            scoringLines = lines;
        }

        public Slider ProgressBar { get { return progressBar; } }
        public GenericScoreBarLine[] ScoringLines { get { return scoringLines; } }

        /// <summary>最近一次算出来的阈值表（自检读口；= `GetScoringThresholds` 的产物）。</summary>
        public int[] LastThresholds { get; private set; }

        /// <summary>原版 `public void Initialize(int currentScore, GenericScoringMilestone[] scoringMilestones,
        /// Sprite resourceIcon)` —— 两段（判据见文件头）。</summary>
        public void Initialize(int currentScore, GenericScoringMilestone[] scoringMilestones,
                               Texture2D resourceIcon = null)
        {
            SetProgressBar(scoringMilestones, currentScore);
            InitializeLines(scoringMilestones, resourceIcon);
        }

        /// <summary>原版 `private void SetProgressBar(GenericScoringMilestone[] milestones, int currentScore)`
        /// —— 三句，逐句实读。</summary>
        private void SetProgressBar(GenericScoringMilestone[] milestones, int currentScore)
        {
            int[] thresholds = GetScoringThresholds(milestones);
            if (progressBar == null)
            {
                Debug.LogWarning("[MilestoneScoreBar] `progressBar` 没绑 ⇒ 进度条不动（原版这一跳是 NRE；我们出声继续）");
                return;
            }
            // ① `progressBar.wholeNumbers = false;`（**常量 false**，不是形参）
            progressBar.wholeNumbers = false;
            // ② `progressBar.maxValue = thresholds.LastOrDefault();`（空表 ⇒ 0）
            progressBar.maxValue = thresholds.Length > 0 ? thresholds[thresholds.Length - 1] : 0f;
            // ③ `normalizedValue = InterpolateMilestones(currentScore, thresholds.Prepend(0).ToList())`
            //    ⚠️ 那个 `Prepend(0)` 由 `AllianceEventScoreInfo.InterpolateMilestones` 内部补 —— 传**裸阈值表**。
            progressBar.normalizedValue =
                AllianceEventScoreInfo.InterpolateMilestones(currentScore, thresholds);
        }

        /// <summary>原版 `private int[] GetScoringThresholds(GenericScoringMilestone[] milestones)`：
        /// `null / 空 ⇒ 空表`，否则 `Select(m =&gt; m.score).ToArray()`。
        /// <para>🔴 **转发**给 `CardPresentation.MilestoneScoreBar.GetScoringThresholds`（`Shell/RewardProgressBarPanel.cs:47`）——
        /// 同一个规则只留一处。⚠️ 差异：那一份对**数组元素为 null** 给 0，原版是 NRE（见 `B94_B6` §「我们这一层」口径）。</para></summary>
        private int[] GetScoringThresholds(GenericScoringMilestone[] milestones)
        {
            LastThresholds = CardPresentation.MilestoneScoreBar.GetScoringThresholds(milestones);
            return LastThresholds;
        }

        /// <summary>`Initialize` 里那一段循环（原版是**内联**在这里的同一份代码）。
        /// <para>判据 = `MilestoneScoreBar__Initialize.c` 的 `while` 循环：</para>
        /// <code>
        /// for (i = 0; scoringLines != null; i++) {
        ///     if (scoringLines.Length &lt;= i) return;                        // 行用完了 ⇒ 静默返回
        ///     if (scoringMilestones == null) break;                         // ⇒ 原版落到 NRE
        ///     if (scoringMilestones.Length &lt;= i) { LogError(那句错误串); return; }
        ///     scoringLines[i].Initialize(scoringMilestones[i], resourceIcon);
        /// }
        /// </code></summary>
        private void InitializeLines(GenericScoringMilestone[] milestones, Texture2D resourceIcon)
        {
            if (scoringLines == null)
            {
                Debug.LogWarning("[MilestoneScoreBar] `scoringLines` 没绑 ⇒ 一行里程碑都不初始化"
                               + "（原版这一跳是 NRE；我们出声继续）");
                return;
            }
            for (int i = 0; i < scoringLines.Length; i++)
            {
                if (milestones == null)
                {
                    // 原版这里是 `break` 出 while 然后落到 `FUN_1803f47a0`（NRE）；我们出声 + 返回
                    Debug.LogWarning("[MilestoneScoreBar] 阈值表是 null ⇒ 后面的行不初始化"
                                   + "（原版这一跳是 NRE；我们出声继续）");
                    return;
                }
                if (milestones.Length <= i)
                {
                    Debug.LogError(ErrorMoreLinesThanThresholds);
                    return;                                   // 原版也是 return（后面的行不初始化）
                }
                var line = scoringLines[i];
                if (line == null)
                {
                    Debug.LogWarning("[MilestoneScoreBar] `scoringLines[" + i + "]` 是 null ⇒ 跳过这一行（原版是 NRE）");
                    continue;
                }
                line.Initialize(milestones[i], resourceIcon);
            }
        }

        /// <summary>原版 `private void SetScoringMilestones(GenericScoringMilestone[] scoringMilestones,
        /// int currentScore, Sprite resourceIcon = null)` —— 体与 <see cref="InitializeLines"/> 逐句相同。
        /// <para>🔴 **本 build 里它没有调用者**（`Initialize` 是**内联**了同一段，不是转调它）——
        /// 判据 = `decomp_full/MilestoneScoreBar__Initialize.c` 里那段 `while` 与
        /// `MilestoneScoreBar__SetScoringMilestones.c` 逐句同形，而 `Initialize` **没有** `call` 到后者。
        /// 仍然照建：① 它可能是被剥掉的调试入口；② 将来有宿主想「只刷行、不动进度条」时它是现成的口。
        /// ⛔ 别把它删了当成「死代码清理」—— 那是推测，不是判据。</para></summary>
        private void SetScoringMilestones(GenericScoringMilestone[] scoringMilestones, int currentScore,
                                          Texture2D resourceIcon = null)
        {
            // `currentScore` 在原版这个方法的体里**没被用**（三个形参里只有两个进循环）—— 照抄签名，⛔ 别自己加活。
            InitializeLines(scoringMilestones, resourceIcon);
        }
    }
}
