// UIEffectTogglerImage.cs — 原版 `UIEffectTogglerImage` 的运行时等价物
//
// 判据（逐方法读过 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/UIEffectTogglerImage.cs`）：
//   `public class UIEffectTogglerImage : UIEffectTogglerBase`，字段 `Image[] images`。
//   四个成员的行为与 `UIEffectCanvasGroupToggler` **逐句同形**，只有两处差别：
//     ① 它淡的是**这一层自己的那几个 `Image`**（`images[]`，挂在同一个 GameObject 上），**不是整棵子树**；
//     ② 写的是每张图的 `color`（含 RGB 原值、只改 α），不是 `CanvasGroup.alpha` 这个独立乘子。
//   · `Toggle(option, duration=0.3f)`：记目标态；`duration<=0` 直接切，否则先 `SetActive(true)`
//   · `DoUpdate(currentTime, normalizedTime)`：`from = 1 − to` · `to = targetStateEnabled ? 1 : 0` ·
//     `n = Clamp01(normalizedTime)`，逐图 `c = image.color; c.a = (to − from) × n + from; image.color = c`
//     ⇒ **α 绝对值覆盖、RGB 取当前值**（原版没有「基线 α」这个概念，⛔ 不是乘性）
//   · `AnimationFinished()`：`deactivateOnDisable && !targetStateEnabled ⇒ ToggleGameObjects(false)`
//   · `ToggleGameObjects(option)`：逐个 `gameObject.SetActive(option)`
//
// ⚠️ **我们这一侧的形态差**（同 `UIEffectCanvasGroupToggler.cs` 文件头）：本工程没有 uGUI `Image`
//    那条主路（UI 是 `ImageQuad`）⇒ 这里按「**同一个 GameObject 上的** `ImageQuad` / `Label` / uGUI `Graphic`」
//    取（= 原版 `images[]` 那一格的口径：**不递归子件**）。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation
{
    /// <summary>淡**这一层自己的**那几张图（原版 `UIEffectTogglerImage`）。
    /// 与 <see cref="UIEffectCanvasGroupToggler"/> 的分工：那个淡整棵子树，这个只淡本节点。</summary>
    public class UIEffectTogglerImage : UIEffectTogglerBase
    {
        /// <summary>显式指定；空 = 自动取**本 GameObject 上**的 `ImageQuad` / `Label` / `Graphic`。</summary>
        public List<GameObject> fadeTargets = new List<GameObject>();

        /// <summary>上一次算出的 fade（0..1）。**原版口径 = 直接写进那张图的 α（绝对值）**，不是乘子 —— 见 <see cref="DoUpdate"/>。</summary>
        public float CurrentFade { get; private set; }
        public int UpdateCount { get; private set; }
        public int FinishedCount { get; private set; }

        readonly List<ImageQuad> _quads = new List<ImageQuad>();
        readonly List<Label> _labels = new List<Label>();
        readonly List<Graphic> _graphics = new List<Graphic>();
        // ⚠️ 2026-10-27（`B9_E2UDW`）：这三份基线**不再参与 α 的写入**（原版是绝对值覆盖，见 `DoUpdate`）——
        //   留着是因为它们记的是「接管前那一帧」的原值，自检/诊断要用（⛔ 别拿它当 α 的乘数）。
        readonly List<float> _quadBase = new List<float>();
        readonly List<Color> _labelBase = new List<Color>();
        readonly List<Color> _graphicBase = new List<Color>();
        bool _captured;

        void Awake() { Capture(); }

        /// <summary>收集目标并记原 α（幂等）。**默认只看本节点**（= 原版 `Image[] images` 的口径）。</summary>
        public void Capture()
        {
            if (_captured) return;
            _captured = true;
            _quads.Clear(); _labels.Clear(); _graphics.Clear();
            _quadBase.Clear(); _labelBase.Clear(); _graphicBase.Clear();
            if (fadeTargets != null && fadeTargets.Count > 0)
            {
                foreach (var go in fadeTargets) Add(go);
                return;
            }
            Add(gameObject);
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

        /// <summary>可淡的件数（自检/接线方判空用）。</summary>
        public int TargetCount { get { Capture(); return _quads.Count + _labels.Count + _graphics.Count; } }

        public override void Toggle(bool option, float duration = 0.3f)
        {
            Capture();
            targetStateEnabled = option;
            bool instant = !(0f < duration);
            ToggleGameObjects(instant ? option : true);
        }

        public override void DoUpdate(float currentTime, float normalizedTime)
        {
            Capture();
            UpdateCount++;
            float from = targetStateEnabled ? 0f : 1f;
            float to = targetStateEnabled ? 1f : 0f;
            CurrentFade = from + (to - from) * Mathf.Clamp01(normalizedTime);

            // 🔴 **2026-10-27（`B9_E2UDW`）修：α 是【绝对值覆盖】，不是「基线 × fade」**。
            //   判据（🥇 `decomp_full/UIEffectTogglerImage__DoUpdate.c:17-45` 逐句）：
            //     `to = targetStateEnabled(+0x21) ? 1 : 0` · `from = 1 − to` · `n = Clamp01(param_3)`；
            //     逐图：`c = image.color`（**每帧取当前值**）→ `c.a = (to − from) × n + from` → `image.color = c`
            //     ⇒ 只改 α、RGB 取当前值、**与原 α 无关**（原版根本没有「基线」这个概念）。
            //   ⚠️ 与同族的 `UIEffectCanvasGroupToggler` **不同**：那个淡的是 `CanvasGroup.alpha`（本身是**乘子**）
            //     ⇒ 我们映射到分层 α 时乘基线才对（那边保持原样，⛔ 别一起改）。
            //   灭自证：基线 α=0.5 的图在 `n=1, to=1` 时要读到 **1.0**（乘性写法只会给 0.5）。
            for (int i = 0; i < _quads.Count; i++)
            {
                var q = _quads[i];
                if (q == null) continue;
                var c = q.Tint; c.a = CurrentFade; q.SetTint(c);
            }
            for (int i = 0; i < _labels.Count; i++)
            {
                var l = _labels[i];
                if (l == null) continue;
                var c = l.color; c.a = CurrentFade; l.SetColor(c);
            }
            for (int i = 0; i < _graphics.Count; i++)
            {
                var g = _graphics[i];
                if (g == null) continue;
                var c = g.color; c.a = CurrentFade; g.color = c;
            }
        }

        public override void AnimationFinished()
        {
            FinishedCount++;
            if (deactivateOnDisable && !targetStateEnabled) ToggleGameObjects(false);
        }

        public void ToggleGameObjects(bool option)
        {
            Capture();
            for (int i = 0; i < _quads.Count; i++) if (_quads[i] != null) _quads[i].gameObject.SetActive(option);
            for (int i = 0; i < _labels.Count; i++) if (_labels[i] != null) _labels[i].gameObject.SetActive(option);
            for (int i = 0; i < _graphics.Count; i++) if (_graphics[i] != null) _graphics[i].gameObject.SetActive(option);
        }
    }
}
