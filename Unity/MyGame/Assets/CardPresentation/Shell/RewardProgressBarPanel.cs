// RewardProgressBarPanel.cs —— **活动奖励进度条面板**（原版 `RewardProgressBarPanel` + 它用到的 `MilestoneScoreBar`）
//
// ============================ 这是什么、判据在哪 ============================
// 判据（唯一来源）：
//   · `资料/完全复刻/判据/B9_日常C_施工单.tsv` 账行 **671 / 672 / 673**（U04 那一组的 ⑤）
//   · `资料/完全复刻/判据/泛型与启动链.md` §三 #1「Scoring/里程碑数据层」那一组
// 原版真身（VA 级反汇编，`d:/2/tools/decomp_full/`）：
//   `RewardProgressBarPanel__Initialize.c` · `__HasMilestoneToCollect.c` · `__CollectButtonClick.c` ·
//   `__Awake.c` · `__OnDestroy.c` · `RewardProgressBarPanel.__c___HasMilestoneToCollect_b__11_0.c` ·
//   `MilestoneScoreBar__Initialize.c` · `__SetProgressBar.c` · `__GetScoringThresholds.c`
//   字段表 = `d:/2/tools/il2cpp_out/dump.cs:63888-63922`（逐个抄）。
//
// 🔴 **「可领」的判据是 `state == Unlocked`，不是「没领过」** —— 谓词实体
//   `RewardProgressBarPanel.__c___HasMilestoneToCollect_b__11_0.c` 的**全部内容**就一句
//   `return *(int *)(milestone + 0x10) == 1;`，而 `GenericScoringMilestone` 的 `+0x10` 是 `state`
//   （`GenericScoringMilestone.cs`：`State { Locked=0, Unlocked=1, Collected=2 }`）⇒ **== Unlocked**。
//   ⚠️ 这条与 `ScoringEvent.GetState`（用 `ScoringMilestone.State` 那套值）**不是同一个枚举**，⛔ 别混。
//
// ⚠️ 与原版的差异（⛔ 不是「已经一样」）：
//   ① 原版是 MonoBehaviour，按钮监听在 `Awake` 挂、`OnDestroy` 摘。我们这边是普通类 ⇒
//      提供 `Attach()/Detach()` 两个显式口（语义与 Awake/OnDestroy 逐句对应），由窗体在开/关时调。
//   ② `Sprite resourceIcon` 我们没有 Sprite 资产句柄 ⇒ 收 `string resourceIconId`，
//      真画由 `OnSetIcon` 钩子交回窗体（窗体有 `MenuDraw` 才有图）。
//   ③ `MilestoneScoreBar`（原版另一个 MonoBehaviour）本批**只搬它被这里用到的两件**：
//      阈值表 `GetScoringThresholds`（= `milestones.Select(m =&gt; m.score).ToArray()`，判据
//      `MilestoneScoreBar__GetScoringThresholds.c`）与 `Initialize` 的入参转发；刻度线的摆位属另一条账，
//      ⛔ 本文件不假装画了它。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `MilestoneScoreBar`（`dump.cs:111238-111262`）的**最小可用**等价：
    /// 只做「阈值表 + 把三样入参转给钩子」这两件（差异 ③）。
    /// <para>`GetScoringThresholds` 判据（`.c` 逐句）：`if (milestones == null || milestones.Length == 0) return &lt;空表&gt;;
    /// return milestones.Select(m =&gt; m.score).ToArray();`</para></summary>
    [Serializable]
    public class MilestoneScoreBar
    {
        /// <summary>原版 `+0x20 progressBar`（`Slider`）的本地等价（写进度）。</summary>
        public Action<float> BarSetter;
        /// <summary>画刻度线 / 里程碑图标的钩子（原版 `SetScoringMilestones` 那一跳）。</summary>
        public Action<int[], string, int> OnSetMilestones;

        /// <summary>原版 `int[] GetScoringThresholds(GenericScoringMilestone[])`：取每档的 `score`。</summary>
        public static int[] GetScoringThresholds(IReadOnlyList<GenericScoringMilestone> milestones)
        {
            if (milestones == null || milestones.Count == 0) return Array.Empty<int>();
            var a = new int[milestones.Count];
            for (int i = 0; i < a.Length; i++) a[i] = milestones[i] != null ? milestones[i].score : 0;
            return a;
        }

        /// <summary>原版 `public void Initialize(int currentScore, GenericScoringMilestone[] milestones, Sprite resourceIcon)`。
        /// <para>`SetProgressBar` 的算式本批**没有**落到账行上（它读 `milestones[i].score` 与 `currentScore`），
        /// 因此这里只转阈值表 + 出声说明，⛔ 不自己发明一个进度算式。</para></summary>
        public void Initialize(int currentScore, IReadOnlyList<GenericScoringMilestone> milestones, string resourceIconId)
        {
            var thresholds = GetScoringThresholds(milestones);
            if (BarSetter == null)
                Debug.LogWarning("[RewardProgressBar] `MilestoneScoreBar.BarSetter` 没接 ⇒ 里程碑进度条不动"
                               + "（原版这一跳是 `SetProgressBar` 写 `Slider.value`；该方法的算式尚未落到账上）");
            if (OnSetMilestones != null) OnSetMilestones(thresholds, resourceIconId, currentScore);
        }
    }

    /// <summary>原版 `RewardProgressBarPanel`（MonoBehaviour → 普通类，差异 ①）。
    /// <para>字段名/偏移照 `dump.cs:63888-63903`：`+0x20 collectButton` / `+0x28 scoreBar` /
    /// `+0x30 scoreText` / `+0x38 scoringImage` / `+0x40 scoringTitleText` / `+0x48 layoutFixer` /
    /// `+0x50 onCollect`。</para></summary>
    [Serializable]
    public class RewardProgressBarPanel
    {
        /// <summary>原版 `+0x20 collectButton`（`EverguildButton`，是 `UnityEngine.UI.Selectable` 的子类）：
        /// 这里只用到 `interactable` 与 `onClick`（→ <see cref="Attach"/>/<see cref="Detach"/>）。</summary>
        public UnityEngine.UI.Selectable CollectButton;
        /// <summary>原版 `+0x28 scoreBar`。</summary>
        public MilestoneScoreBar ScoreBar;
        /// <summary>原版 `+0x30 scoreText`：写 `currentScore.ToString()`。</summary>
        public Behaviour ScoreText;
        /// <summary>原版 `+0x40 scoringTitleText`：写 `resourceName`。</summary>
        public Behaviour ScoringTitleText;
        /// <summary>原版 `+0x38 scoringImage`：`sprite = resourceIcon` + `enabled = (sprite != null)`。</summary>
        public Behaviour ScoringImage;
        /// <summary>原版 `+0x48 layoutFixer`（`LayoutGroupContentFixer`）：末尾那句 `FixLayout()`。</summary>
        public Action FixLayout;

        /// <summary>原版 `+0x50 onCollect`：`CollectButtonClick` 的唯一读者。</summary>
        public Action OnCollect;

        // ── 文本/图标的写口（原版直接写 `EverguildTextMeshPro.text` / `Image.sprite`；我们没有那两个句柄）──
        /// <summary>`(目标, 文本)`。窗体接上这两个口之后 `Initialize` 才能真把字写出去。</summary>
        public Action<Behaviour, string> SetText;
        /// <summary>`(目标, 图标 id)`：写图 + 按「有没有图」开关 `enabled`（原版那两句）。</summary>
        public Action<Behaviour, string> SetIcon;

        [NonSerialized] bool attached;
        [NonSerialized] bool warnedOnce;

        /// <summary>原版 `Awake()`：`collectButton.onClick.AddListener(CollectButtonClick)`。
        /// ⚠️ 我们这边没有 `UnityEvent`（差异 ①）⇒ 这一步只把 `attached` 置真，真正的事件绑定由
        /// 窗体在自己的点击回调里调 <see cref="CollectButtonClick"/>（窗体统一走 `MenuDraw.Hit`）。
        /// 保留这个方法名是为了「挂/摘成对」这条语义不丢。</summary>
        public void Attach() { attached = true; }

        /// <summary>原版 `OnDestroy()`：`collectButton.onClick.RemoveListener(CollectButtonClick)`。</summary>
        public void Detach() { attached = false; }

        /// <summary>是否还挂着（自检用：验 `Attach`/`Detach` 成对，防「关了窗还在响应点击」）。</summary>
        public bool IsAttached { get { return attached; } }

        /// <summary>原版 `private static bool HasMilestoneToCollect(GenericScoringMilestone[])`
        /// （`.c` 逐句）：`return milestones.Any(m =&gt; m.state == State.Unlocked);`
        /// （谓词实体 `b__11_0` = `*(int*)(m + 0x10) == 1`，见文件头那条 🔴）。
        /// ⚠️ 原版 `milestones` 为 null 时 `Any` 抛 `ArgumentNullException`（`Enumerable.Any` 的第一句）；
        /// 我们**出声 + false**（全工程口径：坏数据不让游戏崩）。</summary>
        public static bool HasMilestoneToCollect(IReadOnlyList<GenericScoringMilestone> milestones)
        {
            if (milestones == null)
            {
                Debug.LogError("[RewardProgressBar] HasMilestoneToCollect：milestones 是 null"
                             + "（原版 `Enumerable.Any` 会抛 ArgumentNullException）⇒ false");
                return false;
            }
            for (int i = 0; i < milestones.Count; i++)
            {
                var m = milestones[i];
                if (m != null && m.state == GenericScoringMilestoneState.Unlocked) return true;
            }
            return false;
        }

        /// <summary>原版 `private void CollectButtonClick()`（`.c` 的**全部内容**）：
        /// `onCollect?.Invoke();`（空判那一跳是 `*(this+0x50) != 0`）。
        /// <para>🔴 这是**唯一**的领奖入口 —— 原版没有任何「自己发奖」的代码，发奖在回调里
        /// （`ScoringEvent.PurchaseMilestone` / 窗体那一侧）。</para></summary>
        public void CollectButtonClick()
        {
            if (OnCollect != null) OnCollect();
            else if (!warnedOnce)
            {
                warnedOnce = true;
                Debug.LogWarning("[RewardProgressBar] CollectButtonClick：`OnCollect` 没接 ⇒ 点了什么都不发生"
                               + "（原版这里就是 `onCollect?.Invoke()`，没有别的）");
            }
        }

        /// <summary>原版 `public void Initialize(int currentScore, GenericScoringMilestone[] scoringMilestones,
        /// Action onCollect, Sprite resourceIcon, string resourceName)`（`…__Initialize.c`，**逐句**）：
        /// <code>
        /// this.onCollect(+0x50) = onCollect;
        /// collectButton.interactable = HasMilestoneToCollect(scoringMilestones);
        /// scoreBar.Initialize(currentScore, scoringMilestones, resourceIcon);
        /// scoreText.text = currentScore.ToString();
        /// scoringImage.sprite = resourceIcon;
        /// scoringImage.enabled = (scoringImage.sprite != null);        // Unity 空判 = 有没有图
        /// scoringTitleText.text = resourceName;
        /// layoutFixer.FixLayout();
        /// </code>
        /// ⚠️ 顺序原版就是这样（先 interactable，再 scoreBar，再三处文本/图，最后 FixLayout）——
        /// `FixLayout` **必须在最后**（它按当前内容重排，早调会被后面的写入作废）。</summary>
        public void Initialize(int currentScore, IReadOnlyList<GenericScoringMilestone> scoringMilestones,
                               Action onCollect, string resourceIconId, string resourceName)
        {
            OnCollect = onCollect;
            if (CollectButton != null)
                CollectButton.interactable = HasMilestoneToCollect(scoringMilestones);
            else
                Debug.LogWarning("[RewardProgressBar] Initialize：`CollectButton` 没接 ⇒ 可领状态没写（原版写 interactable）");

            if (ScoreBar != null) ScoreBar.Initialize(currentScore, scoringMilestones, resourceIconId);
            else Debug.LogWarning("[RewardProgressBar] Initialize：`ScoreBar` 没接（原版写里程碑进度）");

            if (SetText != null)
            {
                if (ScoreText != null) SetText(ScoreText, currentScore.ToString());
                if (ScoringTitleText != null) SetText(ScoringTitleText, resourceName ?? string.Empty);
            }
            else if (!warnedOnce)
            {
                warnedOnce = true;
                Debug.LogWarning("[RewardProgressBar] `SetText` 没接 ⇒ 分数/资源名两处文字不写"
                               + "（原版写 `EverguildTextMeshPro.text`；我们换成这个口）");
            }

            if (SetIcon != null) { if (ScoringImage != null) SetIcon(ScoringImage, resourceIconId); }
            else if (ScoringImage != null)
                // 原版：`enabled = (sprite != null)` —— 没有图就整颗关掉（不是留空图）
                Debug.LogWarning("[RewardProgressBar] `SetIcon` 没接 ⇒ 资源图标不写（原版写 `Image.sprite` 并"
                               + "按「有没有图」开关 `enabled`）");

            if (FixLayout != null) FixLayout();
        }
    }
}
