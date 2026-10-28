// MilestonesStepDisplay.cs — **进度点串（milestones step 串）的统一建造件**（原版 `MilestonesStepDisplay`）。
//
// ============================ 判据（第一权威 = 反编译，本地现读）============================
//   · `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/MilestonesStepDisplay.cs`（`Initialize` / `GetTotalAnimationTime`）
//   · `d:/2/tools/decomp_full/MilestonesStepDisplay__Initialize.c`（控制流最终判据）
//   · 资产侧：`MilestoneStep` 全库 10 处实例 / 8 文件（`MilestonesStepDisplay` 挂其中若干窗）
//
// 原版 `Initialize(int currentPoints, int totalPoints, Action<MilestoneStep,int> setupAction,
//                  bool playSounds = true, bool playAnim = false)` 逐句：
//   DestroyAllChildren(stepsHolder.transform);
//   for (int i = 0; i < totalPoints; i++) {
//       var prefab = Enumerable.ElementAtOrDefault(stepPrefab, i) ?? stepPrefab.Last(); // 缺则用最后一个
//       var step = Instantiate(prefab, stepsHolder);
//       currentSteps.Add(step);
//       step.Toggle(currentPoints > i, i, playSounds, playAnim);   // filled = 已完成数 > 下标
//       setupAction?.Invoke(step, i);
//   }
//
// ⚠️ 本工程界面是**过程化网格**（`ImageQuad` / `MenuDraw`），没有 `MilestoneStep` 这套 prefab 类 ⇒
//    本件把「与具体 step 类型 / 渲染无关」的**算法核**落成泛型方法：宿主用委托提供「销毁子件 / 克隆 prefab /
//    Toggle 一颗 / setup 回调」，件本身逐句照抄上面的循环（含 prefab 下标 last-fallback、filled 判据、回调顺序）。
//    这样 8 处实例将来都能收敛到**同一条规则**（⛔ 别再各窗各写一遍）。
// 🔴 与原版一致的**原版怪癖**：`currentSteps` **不清空**（原版只 `DestroyAllChildren`、不清 list）
//    ⇒ 重复 `Initialize` 会让 list 累积旧的（已销毁）项。照抄，⛔ 别在这里「顺手修好」（铁律：原版有 bug 照抄）。
//    ⚠️ 待接：2 个原版消费者（`RankedDivisionChangeWindow` / `MainMenuRuntime`）不在本批白名单 ⇒
//      宿主改用是**转派项**（本件先落地算法核）。
using System.Collections.Generic;

namespace CardPresentation
{
    /// <summary>原版 `MilestonesStepDisplay` 的算法核（与 step 具体类型 / 渲染无关）。
    /// 账 437：`Initialize(currentPoints, totalPoints, setupAction, playSounds, playAnim)`。</summary>
    public static class MilestonesStepDisplay
    {
        /// <summary>第 `stepIndex` 颗是否已填（原版 `currentPoints &gt; i`）。</summary>
        public static bool IsFilled(int stepIndex, int currentPoints) => currentPoints > stepIndex;

        /// <summary>第 `stepIndex` 颗该用 `stepPrefab` 的哪一格 —— 越界时回落到**最后一格**
        /// （原版 `ElementAtOrDefault(stepPrefab, i) ?? stepPrefab.Last()`）。`prefabCount &lt;= 0` 时返回 -1（调用方应先挡）。</summary>
        public static int PrefabIndexFor(int stepIndex, int prefabCount)
            => prefabCount <= 0 ? -1 : (stepIndex < prefabCount ? stepIndex : prefabCount - 1);

        /// <summary>原版 `MilestonesStepDisplay.Initialize` 的算法核。
        /// <para>按 `totalPoints` 逐颗建：第 i 颗用 `stepPrefab[PrefabIndexFor(i, stepPrefab.Length)]` 克隆，
        /// 追加进 `currentSteps`，`Toggle(filled = currentPoints &gt; i, i, playSounds, playAnim)`，再 `setupAction(step, i)`。</para>
        /// <para>委托说明：<paramref name="destroyChildren"/> = 清 `stepsHolder` 的全部子件（原版 `DestroyAllChildren`）；
        /// <paramref name="instantiate"/> = 把一个 prefab 克隆到 `stepsHolder` 下并返回克隆体；
        /// <paramref name="toggle"/> = 一颗 step 的开关（= 原版 `MilestoneStep.Toggle`）；<paramref name="setupAction"/> = 逐颗回调。</para></summary>
        public static void Initialize<T>(
            int currentPoints, int totalPoints,
            T[] stepPrefab,
            List<T> currentSteps,
            System.Action destroyChildren,
            System.Func<T, T> instantiate,
            System.Action<T, bool, int, bool, bool> toggle,
            System.Action<T, int> setupAction,
            bool playSounds, bool playAnim) where T : class
        {
            if (destroyChildren != null) destroyChildren();          // 原版第一句：DestroyAllChildren(stepsHolder)
            if (totalPoints <= 0) return;                            // 原版 `if (totalPoints > 0)` 才建

            int prefabCount = (stepPrefab == null) ? 0 : stepPrefab.Length;
            if (prefabCount <= 0)
            {
                // 原版 `stepPrefab.Last()` 在空数组上**抛异常**；我们出声后不建（坏数据不该让游戏崩）
                UnityEngine.Debug.LogError("[MilestonesStepDisplay] Initialize：`stepPrefab` 为空"
                                         + "（原版 `Enumerable.Last()` 会抛）⇒ 不建");
                return;
            }

            int last = prefabCount - 1;
            for (int i = 0; i < totalPoints; i++)
            {
                // 原版 ElementAtOrDefault(stepPrefab, i) ?? stepPrefab.Last()：越界回落到最后一格
                T prefab = stepPrefab[(i < prefabCount) ? i : last];
                T step = instantiate != null ? instantiate(prefab) : default(T);
                if (currentSteps != null) currentSteps.Add(step);   // 原版：currentSteps.Add(step)（不清空 —— 见文件头怪癖）
                if (toggle != null) toggle(step, currentPoints > i, i, playSounds, playAnim); // filled = currentPoints > i
                if (setupAction != null) setupAction(step, i);
            }
        }
    }
}
