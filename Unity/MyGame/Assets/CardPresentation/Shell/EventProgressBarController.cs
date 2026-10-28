// EventProgressBarController.cs —— **活动进度条**（原版 `EventProgressBarController` + 嵌套 `ScoreObject`）
//
// ============================ 这是什么、判据在哪 ============================
// 判据（唯一来源）：
//   · `资料/完全复刻/判据/B9_日常C_施工单.tsv` 账行 **181 / 182 / 183 / 184 / 185 / 186**（U04 那一组的 ④）
//     —— 那一行把语义写全了：「`NextLevelScore == 0` ⇒ 整条关；`PercentageToNextLevel = (C−P)/(N−P)`
//     负值归 0；端帽只 `0 < p < 1` 显；`Toggle` 同时开关 `scoreText` + `iconHolder`；`ToggleRectMask`
//     给 `ViewportClip` 一个开关口」
// 原版真身（VA 级反汇编，`d:/2/tools/decomp_full/`，**方法体全部可读**）：
//   `EventProgressBarController__Initialize.c` · `__PercentageToNextLevel.c` · `__SetBarFill.c` ·
//   `__ToggleRectMask.c` · `EventProgressBarController.ScoreObject__Initialize.c` · `…ScoreObject__Toggle.c`
//
// 🔴 **端帽那个阈值已经从二进制里读出来了**：`DAT_1834b2bb8` 按 4 字节 float 读 = **1.0**
//   （`工具/read_literal.py d:/2/unity_run_ref/GameAssembly.dll 0x1834b2bb8` → `4B float 1`）⇒
//   `<see cref="EndCapMaxRatio"/>` 不是「我们挑的」。
//
// ⚠️ 与原版的三处如实差异（⛔ 不是「已经一样」）：
//   ① 原版是 **MonoBehaviour**（`Slider` / `Behaviour` 都是序列化字段）。我们的活动窗走 `MenuDraw` 建节点、
//      **不用 uGUI**（`Shell/` 一侧没有 `Slider`）⇒ 本类改成**普通 C# 类**，进度值走
//      `Action<float> BarSetter` 这个口（= 原版 `Slider.set_value`）。语义不变、载体换了。
//   ② `ObtainableItem`（图标/名字那件资产）我们没有 ⇒ 本类收 **`string` itemId**，
//      真正画图标由 `ScoreObject.OnInitialize` 这个钩子交回给窗体（窗体有 `MenuDraw` 才有图）。
//   ③ 原版 `ScoreObject` 还持有一个 `ItemDrawer`（`ItemDrawer.Draw(…)` 现开抽屉）；那属 B38 的 `ItemDrawer`，
//      本批**不接**（钩子留在这里，接的时候不用改本文件）。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `EventProgressBarController.ScoreObject`（嵌套类，字段照 `dump.cs:65033-65042`）：
    /// `+0x10 scoreText`（`EverguildTextMeshPro`）/ `+0x18 iconHolder`（**`Transform`**，不是 GameObject）/
    /// `+0x20 contentFixer` / `+0x28 scoreDrawer`（`ItemDrawer`）。
    /// <para>`Toggle(bool)` 判据（`EventProgressBarController.ScoreObject__Toggle.c`，**逐句**）：
    /// `scoreText.enabled = option; iconHolder.gameObject.SetActive(option);`</para></summary>
    [Serializable]
    public class EventProgressBarScoreObject
    {
        /// <summary>原版 `+0x10`：分数文字（`EverguildTextMeshPro`，这里只用到 `enabled`）。</summary>
        public Behaviour ScoreText;
        /// <summary>原版 `+0x18`：图标容器（**`Transform`** —— 反汇编那一跳是
        /// `Component.get_gameObject` 再 `SetActive`）。</summary>
        public Transform IconHolder;

        /// <summary>画「分数 / 图标」的钩子（原版这一跳是 `ItemDrawer.Draw`）。
        /// null 时**出声一次**（⛔ 不静默：分数会停在出厂占位值）。</summary>
        public Action<ScoringDataInfo, string, bool> OnInitialize;

        [NonSerialized] bool warned;

        /// <summary>原版 `ScoreObject.Toggle(bool)`：**两句**，逐句照抄
        /// （空判：`scoreText` 那半反汇编里**没有** Unity 空判、`iconHolder` 那半有 —— 我们两边都判，出声口径同全工程）。</summary>
        public void Toggle(bool option)
        {
            if (ScoreText != null) ScoreText.enabled = option; else Debug.LogError("[EventProgressBar] ScoreObject.Toggle：scoreText 是 null（原版这一跳 NRE）");
            if (IconHolder != null) IconHolder.gameObject.SetActive(option); else Debug.LogError("[EventProgressBar] ScoreObject.Toggle：iconHolder 是 null（原版这一跳 NRE）");
        }

        /// <summary>原版 `ScoreObject.Initialize(ScoringDataInfo, ObtainableItem, bool isLastMilestone)`
        /// （`…ScoreObject__Initialize.c`）：原版在这里灌分数与图标 —— 具体画法在 `ItemDrawer` 那一层，
        /// 本类只把三个入参转给 <see cref="OnInitialize"/>。</summary>
        public void Initialize(ScoringDataInfo scoringInfo, string scoreItemId, bool isLastMilestone)
        {
            if (OnInitialize != null) { OnInitialize(scoringInfo, scoreItemId, isLastMilestone); return; }
            if (!warned)
            {
                warned = true;
                Debug.LogWarning("[EventProgressBar] `ScoreObject.OnInitialize` 没接（原版这一跳是 `ItemDrawer.Draw`，"
                               + "属 B38）⇒ 分数与图标停在出厂占位值。接法：`scoreObject.OnInitialize = (info, id, last) => …`");
            }
        }
    }

    /// <summary>原版 `EventProgressBarController`（MonoBehaviour → 我们这边是普通类，见文件头差异 ①）。
    /// <para>字段名与偏移**照 `dump.cs:65057-65068` 逐个抄**：`+0x20 scoreObject` /
    /// `+0x28 progressFillBar(Slider)` / `+0x30 rectMask2D(RectMask2D)` / `+0x38 tipSprite(Image)` /
    /// `+0x40 originalWidth(float)`。
    /// ⚠️ 本条就地订正过一次口径（铁律 5）：`+0x38` 原写成「端帽 endCap」，字段表的真名是
    /// **`tipSprite: Image`** —— 就是进度条末端的那个小圆头/箭头，`0 &lt; p &lt; 1` 才显。</para></summary>
    [Serializable]
    public class EventProgressBarController
    {
        /// <summary>🔴 端帽的显隐上界 = 原版 `DAT_1834b2bb8` = **1.0f**（判据见文件头）。</summary>
        public const float TipSpriteMaxRatio = 1.0f;

        /// <summary>原版 `+0x20`：分数/图标那一组。</summary>
        public EventProgressBarScoreObject ScoreObject;
        /// <summary>原版 `+0x28 progressFillBar`（`Slider`）的本地等价：**写进度的唯一口**
        /// （= `Slider.set_value`）。null ⇒ 出声一次、不画。</summary>
        public Action<float> BarSetter;
        /// <summary>原版 `+0x30 rectMask2D`（`RectMask2D`）：`ToggleRectMask` 直接开关它。</summary>
        public Behaviour RectMask;
        /// <summary>原版 `+0x38 tipSprite`（`Image`）：末端那个小圆头，`0 &lt; p &lt; 1` 才显示
        /// （用的是 `Behaviour.enabled`，不是 `SetActive`）。</summary>
        public Behaviour TipSprite;
        /// <summary>原版 `+0x40 originalWidth`（`float`）：`SetBarFill` **不读**它（全文件唯一读者在别处，
        /// 本批没有落到它的行）⇒ 保留字段、照原版给 0 初值。</summary>
        public float OriginalWidth;
        /// <summary>原版 `gameObject`（`Initialize` 里 `SetActive(NextLevelScore != 0)`）。</summary>
        public GameObject Root;

        [NonSerialized] bool warnedBar;

        /// <summary>原版 `float PercentageToNextLevel(ScoringDataInfo scoringInfo)`
        /// （`EventProgressBarController__PercentageToNextLevel.c`，**逐句**）：
        /// <code>
        /// int d = info.Current(-&gt;+0x10) - info.Previous(+0x14);
        /// if (d &lt; 1) d = 0;                                  // ★ 负值/0 都归 0
        /// return (float)d / (float)(info.Next(+0x18) - info.Previous);
        /// </code>
        /// 🔴 **分母不设防**：`Next == Previous` 时原版就是 `x / 0f`（= `Infinity` / `NaN`）；
        /// 调用方要么先判 `Next != 0`（`Initialize` 就是这么做的），要么自己接住。照抄、⛔ 不偷偷加保护。</summary>
        public static float PercentageToNextLevel(ScoringDataInfo scoringInfo)
        {
            if (scoringInfo == null) return 0f;      // 原版 NRE；我们出声口径见调用方（这条只是不崩）
            int d = scoringInfo.CurrentScore - scoringInfo.PreviousLevelScore;
            if (d < 1) d = 0;
            return (float)d / (float)(scoringInfo.NextLevelScore - scoringInfo.PreviousLevelScore);
        }

        /// <summary>原版 `private void SetBarFill(Slider progressBar, float percentage)`
        /// （`EventProgressBarController__SetBarFill.c`，**逐句**）：
        /// <code>
        /// progressBar.value = percentage;                                  // +0x428 是 set_value
        /// tipSprite.enabled = (0f &lt; percentage) &amp;&amp; (percentage &lt; 1.0f);  // DAT_1834b2bb8 = 1.0f
        /// </code>
        /// ⚠️ 两处签名差异：① `(Slider, float)` → `(float)`（`Slider` 那一格换成实例上的 `BarSetter`，差异 ①）；
        /// ② 原版是 **private**、我们放开成 public（自检要直接验「p=0 / p=0.5 / p=1 时 tipSprite 的显隐」，
        /// 而原版那条只能透过 `Initialize` 间接验）。</summary>
        public void SetBarFill(float percentage)
        {
            if (BarSetter == null)
            {
                if (!warnedBar)
                {
                    warnedBar = true;
                    Debug.LogWarning("[EventProgressBar] `BarSetter` 没接（原版这一跳是 `progressFillBar.value = p`）"
                                   + "⇒ 进度条不动（tipSprite 那一半照常算）。");
                }
            }
            else BarSetter(percentage);

            if (TipSprite != null) TipSprite.enabled = percentage > 0f && percentage < TipSpriteMaxRatio;
        }

        /// <summary>原版 `void ToggleRectMask(bool option)`（`…__ToggleRectMask.c`）：**一句**
        /// `rectMask2D.enabled = option;`。</summary>
        public void ToggleRectMask(bool option)
        {
            if (RectMask != null) RectMask.enabled = option;
        }

        /// <summary>原版 `void Initialize(ScoringDataInfo scoringInfo, ObtainableItem scoreItem, bool isLastMilestone)`
        /// （`EventProgressBarController__Initialize.c`，**逐句**）：
        /// <code>
        /// gameObject.SetActive(scoringInfo.Next(-&gt;+0x18) != 0);      // ★ Next==0 ⇒ 整条关
        /// if (scoreObject != null) scoreObject.Initialize(scoringInfo, scoreItem, isLastMilestone);
        /// int d = Current - Previous; if (d &lt; 1) d = 0;
        /// float p = d / (float)(Next - Previous);
        /// progressBar.value = p;
        /// endCap.enabled = (0 &lt; p) &amp;&amp; (p &lt; 1);
        /// </code>
        /// ⚠️ 两个重载（`账行 181/182`）原版是**同一个方法体**，只是第二参的类型名不同
        /// （`ObtainableItem` 在 ILSpy 里两次反编译成了不同别名）⇒ 我们**只留一个**
        /// （收 `string` itemId）。⛔ 不要为了「凑两个」再造一个空转的重载。</summary>
        public void Initialize(ScoringDataInfo scoringInfo, string scoreItemId, bool isLastMilestone)
        {
            if (scoringInfo == null)
            {
                Debug.LogError("[EventProgressBar] Initialize：scoringInfo 是 null（原版这一跳 NRE）⇒ 不改任何东西");
                return;
            }
            if (Root != null) Root.SetActive(scoringInfo.NextLevelScore != 0);
            if (ScoreObject != null) ScoreObject.Initialize(scoringInfo, scoreItemId, isLastMilestone);
            SetBarFill(PercentageToNextLevel(scoringInfo));
        }

        /// <summary>原版没这个方法：把「整条关」这一格单独开出来，给「下一档是 0 但我不想动 Root」的调用方用
        /// （等于 `Root.SetActive(info.NextLevelScore != 0)`）。
        /// <para>🔶 自己设计（非原版）：原版那句只出现在 `Initialize` 里；我们三扇窗（Raid / 扩张 / 双阵营）
        /// 都可能在 `Initialize` 之后再刷一次里程碑，需要单独重放这一格。语义与原版那句**逐字相同**。
        /// 调度台 C2 2026-10-27 定。</para></summary>
        public void ToggleWholeBar(ScoringDataInfo scoringInfo)
        {
            if (Root != null) Root.SetActive(scoringInfo != null && scoringInfo.NextLevelScore != 0);
        }

        // 🔴 账行 185 的 `Toggle(bool option)` **不是本类的**：`dump.cs:65057-65086` 里
        // `EventProgressBarController` 只有 `Initialize` / `ToggleRectMask` / `PercentageToNextLevel` /
        // `SetBarFill` 四个方法，**没有 `Toggle`** —— `Toggle` 是嵌套类
        // `EventProgressBarController.ScoreObject` 的那一个（`dump.cs:65050`，见本文件上半）。
        // ⛔ 别在父类补一个同名空转方法（那会变成原版没有的东西）。
    }
}
