// UIEffectTogglerController.cs — 原版 `UIEffectTogglerController` 的运行时等价物（**驱动者**）
//
// 判据（逐方法读过 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/UIEffectTogglerController.cs`）：
//   `public class UIEffectTogglerController : MonoBehaviour`
//   字段：`List<UIEffectTogglerBase> currentEffectTogglers` · `float currentTime` · `float currentDuration`
//         · `bool? targetStateEnabled` · `bool doAnim`
//   · `FindEffectTogglersInChildren()` = `GetComponentsInChildren<UIEffectTogglerBase>()` → List
//   · `Toggle(bool option, float duration = 0.3f, bool forceChange = false)`：
//       `if (targetStateEnabled == option && !forceChange) return;`（同态不重播）
//       `doAnim = true; targetStateEnabled = option; currentDuration = duration;`
//       逐个 `toggler.Toggle(option, duration)`
//       `if (duration <= 0) { enabled = false; currentTime = float.MaxValue; Update(); return; }`（瞬间那档）
//       `currentTime = enabled ? (1 - currentTime/currentDuration) * duration : 0f;`（中途反向要接着走）
//       `enabled = true;` —— 靠 Unity 每帧调 `Update` 推进
//   · `Update()`：
//       `if (!doAnim) { enabled = false; return; }`
//       `currentTime += dt; n = clamp01(currentTime / currentDuration);`
//       逐个 `toggler.DoUpdate(currentTime, n)`
//       `if (currentTime > currentDuration) { enabled = false; doAnim = false; 逐个 toggler.AnimationFinished(); }`
//   ⚠️ 原版那两段 `foreach` 在反编译里是 `List.Enumerator` + 间接调用，实参逐个对过（`+178`=Toggle / `+188`=DoUpdate / `+198`=AnimationFinished）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>把 `UIEffectTogglerBase` 们按一条时长推进的驱动者（原版同名）。</summary>
    public class UIEffectTogglerController : MonoBehaviour
    {
        /// <summary>子件里的 toggler（`Awake` 时收一次；运行期新增的用 <see cref="Rescan"/>）。</summary>
        public List<UIEffectTogglerBase> togglers = new List<UIEffectTogglerBase>();

        float _currentTime;
        float _currentDuration = 0.3f;
        bool? _targetStateEnabled;
        bool _doAnim;

        /// <summary>`DoUpdate` 广播过的帧数（诊断/自检用）。</summary>
        public int TickCount { get; private set; }
        /// <summary>本段动画走完过几次（自检用）。</summary>
        public int CompletedCount { get; private set; }

        void Awake() { Rescan(); }

        /// <summary>原版 `FindEffectTogglersInChildren`（`GetComponentsInChildren&lt;UIEffectTogglerBase&gt;()`）。</summary>
        public void Rescan()
        {
            var found = GetComponentsInChildren<UIEffectTogglerBase>(true);
            togglers.Clear();
            for (int i = 0; i < found.Length; i++)
                if (found[i] != null) togglers.Add(found[i]);
        }

        /// <summary>原版 `Toggle(option, duration = 0.3f, forceChange = false)`。</summary>
        public void Toggle(bool option, float duration = 0.3f, bool forceChange = false)
        {
            if (_targetStateEnabled.HasValue && _targetStateEnabled.Value == option && !forceChange) return;
            if (!Application.isPlaying) duration = 0f;   // 🔴 2026-10-26（F3b）编辑器/批处理**没有帧循环** ⇒ 靠 `Update` 推进的补间永远走不完（`AnimationFinished` 不跑）⇒ 非运行期走**已有的瞬间档**（`duration<=0` 那条分支：`currentTime=MaxValue; Update()` ⇒ `DoUpdate(n=1)`+`AnimationFinished` 一次跑完）。先例 = `Shell/ForgeTab.FocusClaimable` 的 `!Application.isPlaying ⇒ FocusOn` · `Shell/InboxWindow.StartAnimateIn`。⚠️ 运行期行为零变化。
            _doAnim = true;
            _targetStateEnabled = option;
            _currentDuration = duration;

            for (int i = 0; i < togglers.Count; i++)
                if (togglers[i] != null) togglers[i].Toggle(option, duration);

            if (!(duration > 0f))
            {
                // 瞬间那档：先让 Update 空跑一次（原版：enabled=false; currentTime=MaxValue; Update()）
                enabled = false;
                _currentTime = float.MaxValue;
                Update();
                return;
            }

            _currentTime = enabled ? (1f - _currentTime / _currentDuration) * duration : 0f;
            enabled = true;
        }

        void Update()
        {
            if (!_doAnim)
            {
                enabled = false;
                return;
            }

            float dt = Time.deltaTime;
            _currentTime += dt;
            float n = Mathf.Clamp01(_currentTime / _currentDuration);

            for (int i = 0; i < togglers.Count; i++)
                if (togglers[i] != null) togglers[i].DoUpdate(_currentTime, n);
            TickCount++;

            if (_currentTime > _currentDuration)
            {
                enabled = false;
                _doAnim = false;
                CompletedCount++;
                for (int i = 0; i < togglers.Count; i++)
                    if (togglers[i] != null) togglers[i].AnimationFinished();
            }
        }
    }
}
