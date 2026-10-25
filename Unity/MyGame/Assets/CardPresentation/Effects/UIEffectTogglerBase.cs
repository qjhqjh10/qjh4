// UIEffectTogglerBase.cs — 原版 `UIEffectTogglerBase` 的运行时等价物（基类）
//
// 判据（逐方法读过 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/UIEffectTogglerBase.cs`）：
//   `[Serializable] public abstract class UIEffectTogglerBase : MonoBehaviour`，两个字段 + 三个虚/抽象成员：
//     · `deactivateOnDisable`（默认 **true**）
//     · `targetStateEnabled`（protected；`Toggle` 只记这一格）
//     · `Toggle(bool option, float duration = 0.3f)` —— **虚方法，本体只有一句** `targetStateEnabled = option;`
//     · `abstract void DoUpdate(float currentTime, float normalizedTime)`
//     · `abstract void AnimationFinished()`
//
// 🔴 **谁驱动它**：`UIEffectTogglerController`（同目录）——原版 `Update` 里按 `currentTime/currentDuration`
//    算归一化时间、逐个 `DoUpdate`，走完再逐个 `AnimationFinished`。⛔ 基类自己不起协程。
//
// ⚠️ **与原版的唯一形态差**：原版三个子类是 `CanvasGroup` / `Graphic` / `Coffee` 三档；
//    我们这套 UI 是 `ImageQuad`（MeshRenderer + 每件一份材质）+ `Battle/Label`（TMP），**没有 `CanvasGroup`**
//    ⇒ 见 `UIEffectCanvasGroupToggler.cs` 的「同名字段、换实现」那一段（⛔ 不是没做，是换成了我们这套 UI 的等价物）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>UI 特效开关的抽象基类。**原版同名同语义**（`UIEffectTogglerBase`，`Assembly-CSharp`）。</summary>
    [System.Serializable]
    public abstract class UIEffectTogglerBase : MonoBehaviour
    {
        /// <summary>关掉之后是否把目标 `SetActive(false)`（原版默认 **true**）。</summary>
        public bool deactivateOnDisable = true;

        /// <summary>目标状态（true = 要显示）。`Toggle` 只负责记它。</summary>
        protected bool targetStateEnabled;

        /// <summary>原版 `Toggle` 的本体**只有一句** `targetStateEnabled = option;`（子类覆写它做别的事）。</summary>
        public virtual void Toggle(bool option, float duration = 0.3f)
        {
            targetStateEnabled = option;
        }

        /// <summary>`normalizedTime` = `currentTime / duration`，**已夹到 [0,1]**（由控制器算）。</summary>
        public abstract void DoUpdate(float currentTime, float normalizedTime);

        /// <summary>补间走完那一拍（控制器在本段动画结束时调一次）。</summary>
        public abstract void AnimationFinished();
    }
}
