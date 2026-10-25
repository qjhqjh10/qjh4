// UIEffectCanvasGroupToggler.cs — 原版 `UIEffectCanvasGroupToggler` 的运行时等价物
//
// 判据（逐方法读过 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/UIEffectCanvasGroupToggler.cs`）：
//   `public class UIEffectCanvasGroupToggler : UIEffectTogglerBase`，字段 `CanvasGroup[] canvasGroups`。
//     · `Toggle(bool option, float duration = 0.3f)`：
//         `targetStateEnabled = option;`
//         `bool instant = !(0f < duration);`      // duration <= 0 ⇒ 瞬间切
//         `ToggleGameObjects(instant ? option : true);`   // 🔴 要淡出时先 SetActive(true)，否则看不见淡出
//         （补间本身由控制器逐帧调 `DoUpdate`）
//     · `DoUpdate(currentTime, normalizedTime)`：
//         `from = targetStateEnabled ? 0 : 1` · `to = targetStateEnabled ? 1 : 0`
//         `alpha = from + (to - from) * clamp01(normalizedTime)`；逐个 `canvasGroups[i].alpha = alpha`
//     · `AnimationFinished()`：`if (deactivateOnDisable && !targetStateEnabled) ToggleGameObjects(false);`
//     · `ToggleGameObjects(bool option)`：逐个 `canvasGroups[i].gameObject.SetActive(option)`
//
// 🔴 **我们这一侧的关键形态差（如实记）**：本工程 UI **没有 `CanvasGroup`**（`ImageQuad` = MeshRenderer +
//    每件一份材质；文字是 `Battle/Label` 的 TMP），所以「一个 α 乘到整组」要自己乘：
//      `ImageQuad.SetTint(a) / Tint` · `Label.SetColor(c) / color` · uGUI `Graphic.color`（兜底分支）。
//    ⇒ 每件渲染件在**第一次**被这个组件接管时记下自己的**原 α**，之后一律 `原α × fade`
//      （原版 `CanvasGroup.alpha` 是独立乘子，不动子件自己的 α —— 这里用「乘原值」等价实现）。
//    ⚠️ 若某个渲染件的 α 在这期间被**别人**改（例如整卡淡出），本组件不知情 ⇒ 以它记的原值为基。
//       当前唯一消费点（Forge 的 `Ready for level up`）没有别的 α 写手。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation
{
    /// <summary>把一组 UI 渲染件**一起淡入/淡出**（原版 `UIEffectCanvasGroupToggler`）。
    /// <para>目标的收集口径 = 自己 + 所有子件上的 `ImageQuad` / `Label` / uGUI `Graphic`（含未激活的）。
    /// `fadeTargets` 非空时**只淡它列的那些**（调用方自己给，用于精确控制）。</para></summary>
    public class UIEffectCanvasGroupToggler : UIEffectTogglerBase
    {
        /// <summary>显式指定要淡的渲染件；空 = 自动收集（自己 + 子件）。</summary>
        public List<GameObject> fadeTargets = new List<GameObject>();

        /// <summary>上一次 `DoUpdate` 算出来的 fade（0..1，**乘子**，不是最终 α）。自检读它。</summary>
        public float CurrentFade { get; private set; }

        /// <summary>`DoUpdate` 被调过几次 / `AnimationFinished` 几次（诊断/自检用）。</summary>
        public int UpdateCount { get; private set; }
        public int FinishedCount { get; private set; }

        readonly List<ImageQuad> _quads = new List<ImageQuad>();
        readonly List<Label> _labels = new List<Label>();
        readonly List<Graphic> _graphics = new List<Graphic>();
        readonly List<float> _quadBase = new List<float>();
        readonly List<Color> _labelBase = new List<Color>();
        readonly List<Color> _graphicBase = new List<Color>();
        bool _captured;

        void Awake() { Capture(); }

        /// <summary>收集目标并记下各自的原 α（幂等）。</summary>
        public void Capture()
        {
            if (_captured) return;
            _captured = true;
            _quads.Clear(); _labels.Clear(); _graphics.Clear();
            _quadBase.Clear(); _labelBase.Clear(); _graphicBase.Clear();

            if (fadeTargets != null && fadeTargets.Count > 0)
            {
                foreach (var go in fadeTargets)
                {
                    if (go == null) continue;
                    Add(go);
                }
                return;
            }
            Add(gameObject);
            var all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++) Add(all[i].gameObject);
        }

        void Add(GameObject go)
        {
            if (go == null) return;
            var q = go.GetComponent<ImageQuad>();
            if (q != null && !_quads.Contains(q)) { _quads.Add(q); _quadBase.Add(q.Tint.a); }
            var l = go.GetComponent<Label>();
            if (l != null && !_labels.Contains(l)) { _labels.Add(l); _labelBase.Add(l.color); }
            var g = go.GetComponent<Graphic>();
            if (g != null && !_graphics.Contains(g)) { _graphics.Add(g); _graphicBase.Add(g.color); }
        }

        /// <summary>有没有可淡的件（自检/接线方判空用；**空了要出声** —— 由调用方做）。</summary>
        public int TargetCount { get { Capture(); return _quads.Count + _labels.Count + _graphics.Count; } }

        /// <summary>原版 `Toggle`：记目标态 + 按「是否瞬间」决定先不先 `SetActive(true)`。</summary>
        public override void Toggle(bool option, float duration = 0.3f)
        {
            Capture();
            targetStateEnabled = option;
            bool instant = !(0f < duration);
            ToggleGameObjects(instant ? option : true);
        }

        /// <summary>原版 `DoUpdate`：`alpha = from + (to - from) * clamp01(normalizedTime)`，逐个写回去。</summary>
        public override void DoUpdate(float currentTime, float normalizedTime)
        {
            Capture();
            UpdateCount++;
            float from = targetStateEnabled ? 0f : 1f;
            float to = targetStateEnabled ? 1f : 0f;
            float f = Mathf.Clamp01(normalizedTime);
            CurrentFade = from + (to - from) * f;

            for (int i = 0; i < _quads.Count; i++)
            {
                var q = _quads[i];
                if (q == null) continue;
                var c = q.Tint; c.a = _quadBase[i] * CurrentFade; q.SetTint(c);
            }
            for (int i = 0; i < _labels.Count; i++)
            {
                var l = _labels[i];
                if (l == null) continue;
                var c = _labelBase[i]; c.a = _labelBase[i].a * CurrentFade; l.SetColor(c);
            }
            for (int i = 0; i < _graphics.Count; i++)
            {
                var g = _graphics[i];
                if (g == null) continue;
                var c = _graphicBase[i]; c.a = _graphicBase[i].a * CurrentFade; g.color = c;
            }
        }

        /// <summary>原版 `AnimationFinished`：关完那一拍按需 `SetActive(false)`。</summary>
        public override void AnimationFinished()
        {
            FinishedCount++;
            if (deactivateOnDisable && !targetStateEnabled) ToggleGameObjects(false);
        }

        /// <summary>原版 `ToggleGameObjects`：逐个 `gameObject.SetActive(option)`。</summary>
        public void ToggleGameObjects(bool option)
        {
            Capture();
            if (_quads.Count == 0 && _labels.Count == 0 && _graphics.Count == 0) return;
            for (int i = 0; i < _quads.Count; i++) if (_quads[i] != null) _quads[i].gameObject.SetActive(option);
            for (int i = 0; i < _labels.Count; i++) if (_labels[i] != null) _labels[i].gameObject.SetActive(option);
            for (int i = 0; i < _graphics.Count; i++) if (_graphics[i] != null) _graphics[i].gameObject.SetActive(option);
        }
    }
}
