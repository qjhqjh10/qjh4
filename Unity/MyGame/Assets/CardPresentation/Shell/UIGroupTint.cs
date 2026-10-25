// UIGroupTint.cs — 通用件：**整组染色**（原版同名类，306 处实例）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【它在什么时候改什么（一句话判据）】
//   `SetColor(target, time)` 把整棵子树（`GetComponentsInChildren(true)`，**含未激活**）每颗渲染件刷成 `tintColor`：
//   **`time == 0` ⇒ 直接写 `tintColor = target`**；**`time > 0` ⇒ 补间 `tintColor`**（原版走 DOTween，我们逐帧插值）。
//   真正把颜色写下去的是 `DoColorChange()`（逐件 `SetColor(tintColor)` + `lastColor = tintColor`），
//   而 `Update` 只在 `|lastColor − tintColor|² ≥ 9.9999994e-11` 时才调它，否则 **`enabled = false`**（自停）。
//   `OnEnable` 先刷一次；`Toggle(bool)` 直接改 `enabled`。
//
// 【判据（第一权威 = 反编译）】`D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/UIGroupTint.cs`
//   · `OnEnable`（:32）= `DoColorChange()`；`OnDestroy`（:37）空
//   · `Refresh`（:41-82）= 清空 `canvasRenderers` → `GetComponentsInChildren<CanvasRenderer>(true)` 全收 → `lastColor = 0`
//   · `DoColorChange`（:84-114）= 列表为 null 直接返回；逐件 `CanvasRenderer.SetColor(tintColor)`；`lastColor = tintColor`
//   · `Update`（:116-141）= `|lastColor − tintColor|² ≥ 9.9999994e-11` ⇒ `DoColorChange()` 并返回；否则 `enabled = false`
//   · `SetColor`（:148-176）= `enabled = true`；`time == 0` ⇒ `tintColor = color`；否则 DOTween 补间 `tintColor`
//   · `Toggle`（:143-146）= `enabled = option`
//   · 序列化默认 `tintColor` = **`(1,1,1,1)`**（`bundle_menus_assets_all` 三份实例现读，见报告）
//
// 🔴 **判据缺口（如实记，不补、不猜 —— 用户 2026-10-25 明确纠正）**：`Refresh()` 在反编译的 Assembly-CSharp 里**零调用点**
//   （`grep -l UIGroupTint__Refresh *.c` 只有它自己；唯一的消费者 `BasicCardUI__TintCard.c` 只调 `SetColor`；
//   306 个实例的 `m_PersistentCalls` 全空）⇒ **原版这个组件就是空转的**（列表只在 ctor 建、从不填充）。
//   ⇒ 按**铁律 11 例外①**（原版本身没有该行为），**我们同样空转**：
//     保留同名 `Refresh()` 供自检/将来显式调用，**⛔ 不在 `SetColor` / `Toggle` / `OnEnable` 里自动调它**
//     （那会变成「做点什么」= 自我设计，用户口径是「一切按原版、不做多余的自我设计」）。
//     306 处实例**本轮不接**；账里判 **D**（空转 = 行为一致）。
//
// 【与我们的世界的**结构性适配**（如实标，不是复刻）】
//   · 原版 `CanvasRenderer.SetColor` 是**乘在顶点色上的独立通道**（`Graphic.color` 仍保留）；
//     我们的 `ImageQuad.SetTint` / `Label.SetColor` **写的就是唯一那一路颜色** ⇒ 本件在 `Refresh` 时
//     **记下每件的基色**，之后写 `基色 × tint`。⚠️ **这就是一处静默风险**：若别的代码在本件生效后又改基色，
//     本件下一次覆盖会把它盖掉（原版两路互不覆盖）。⇒ 接实例时**本件必须是该子树最后一个写色的人**
//     （或接完实例后不再改那批基色）。
//   · 我们**没有 CanvasRenderer** ⇒ 收集的是 `ImageQuad` / `Label` / 其余 `Renderer`（`_Color` 属性）。
//     `ImageQuad`/`Label` 自己那颗 `MeshRenderer` **不重复收集**（否则同一通道写两遍、基色还会串）。
//   · 原版 DOTween 补间的是 `tintColor` 本身；我们逐帧插值（仓库无 DOTween）。
//   · 批处理没有帧循环 ⇒ `Step(dt)` 手推（与 `UiBadgeNotification.Step` 同口径）。
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>整组染色（原版 `UIGroupTint`）。判据见文件头。</summary>
    public sealed class UIGroupTint : MonoBehaviour
    {
        /// <summary>原版那条「颜色变了才刷」的阈值（`9.9999994E-11f`，反编译 :134 的字面量）。</summary>
        const float ColorEpsilonSq = 9.9999994e-11f;

        struct Entry
        {
            public ImageQuad quad;
            public Label label;
            public Renderer renderer;
            public int prop;
            public Color baseColor;
        }

        /// <summary>原版 `tintColor`（序列化默认见文件头；这里给白，= 不影响任何基色）。</summary>
        public Color tintColor = Color.white;

        readonly List<Entry> _entries = new List<Entry>();
        bool _hasLast;
        Color _last;

        // 补间（替代原版 DOTween）
        bool _tweening;
        Color _tweenFrom, _tweenTo;
        float _tweenTime, _tweenDuration;

        /// <summary>原版 `TintColor` 属性（读当前目标色）。</summary>
        public Color TintColor { get { return tintColor; } }
        /// <summary>自检读口：已经收集到多少颗渲染件。</summary>
        public int RendererCount { get { return _entries.Count; } }
        /// <summary>自检读口：还在补间吗。</summary>
        public bool Tweening { get { return _tweening; } }
        /// <summary>自检读口：`lastColor`（最近一次真正写下去的目标色）。</summary>
        public bool HasLastColor { get { return _hasLast; } }
        public Color LastColor { get { return _last; } }

        /// <summary>原版 `Refresh()`：重收整棵子树（**含未激活**），并**重新记基色**、清 `lastColor`。
        /// <para>⚠️ 重新记基色 = 以「此刻屏幕上看到的颜色」为新基色（原版 `lastColor = 0` 的等价意图：
        /// 让下一次 `Update` 一定刷一遍）。</para>
        /// <para>🔴 **原版它是 private 且全库零调用点** ⇒ 生产里**永远不会被调用**。我们保留同名方法
        /// （改成 public 只为自检/显式调用），**任何生命周期都不调它** —— 见文件头判据缺口。</para></summary>
        public void Refresh()
        {
            _entries.Clear();
            var quads = GetComponentsInChildren<ImageQuad>(true);
            for (int i = 0; i < quads.Length; i++)
            {
                if (quads[i] == null) continue;
                _entries.Add(new Entry { quad = quads[i], baseColor = quads[i].Tint });
            }
            var labels = GetComponentsInChildren<Label>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == null) continue;
                _entries.Add(new Entry { label = labels[i], baseColor = labels[i].color });
            }
            // 其余 Renderer（粒子/网格）：只收**有 `_Color` 属性**的；`ImageQuad`/`Label` 自己那颗跳过
            var rs = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                var r = rs[i];
                if (r == null) continue;
                if (r.GetComponent<ImageQuad>() != null || r.GetComponent<Label>() != null) continue;
                var m = r.sharedMaterial;
                if (m == null) continue;
                int id = Shader.PropertyToID(UIImageMaterialColorChanger.DefaultProperty);
                if (!m.HasProperty(id))
                {
                    Debug.LogWarning("[UIGroupTint] `" + r.name + "` 的材质 `" + (m.shader != null ? m.shader.name : "?")
                                   + "` 没有 `_Color` 属性 ⇒ **这一颗不参与整组染色**（出声，不静默）");
                    continue;
                }
                _entries.Add(new Entry { renderer = r, prop = id, baseColor = m.GetColor(id) });
            }
            _hasLast = false;
            _last = default(Color);
        }

        /// <summary>原版 `DoColorChange()`：把 `tintColor` **乘到每件基色上**写下去，并记 `lastColor`。
        /// ⚠️ `Refresh()` 从没被调用时 `_entries` 是空的 ⇒ 本方法是**空循环** —— 与原版一致（见文件头判据缺口）。</summary>
        public void DoColorChange()
        {
            if (_entries == null) return;
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                Color c = Multiply(e.baseColor, tintColor);
                if (e.quad != null) e.quad.SetTint(c);
                else if (e.label != null) e.label.SetColor(c);
                else if (e.renderer != null && e.renderer.sharedMaterial != null)
                    e.renderer.sharedMaterial.SetColor(e.prop, c);
            }
            _last = tintColor;
            _hasLast = true;
        }

        /// <summary>分量相乘（= 原版 `CanvasRenderer.SetColor` 乘在顶点色上）。</summary>
        public static Color Multiply(Color baseColor, Color tint)
        {
            return new Color(baseColor.r * tint.r, baseColor.g * tint.g,
                             baseColor.b * tint.b, baseColor.a * tint.a);
        }

        /// <summary>原版 `SetColor(color, time)`。`time == 0` 直接落值；否则补间。
        /// ⚠️ **不自动 `Refresh()`**（原版也不调 —— 见文件头判据缺口）。</summary>
        public void SetColor(Color color, float time)
        {
            enabled = true;
            if (time <= 0f)
            {
                _tweening = false;
                tintColor = color;
                return;
            }
            _tweenFrom = tintColor;
            _tweenTo = color;
            _tweenTime = 0f;
            _tweenDuration = time;
            _tweening = true;
        }

        /// <summary>原版 `Toggle(bool)`：只改 `enabled`（**不 Refresh**）。</summary>
        public void Toggle(bool option)
        {
            enabled = option;
        }

        void OnEnable()
        {
            DoColorChange();                 // 原版 OnEnable 只这一句（列表空 ⇒ 空循环，与原版一致）
        }

        void Update() { Step(Time.deltaTime); }

        /// <summary>推进一帧。**逐句照原版 `Update`**：先判要不要刷（刷完**这一帧仍然 enabled**），
        /// 再累加补间时间（原版 DOTween 独立推进 `tintColor`）。返回是否仍启用。
        /// 批处理无帧循环 ⇒ 自检用它手推。</summary>
        public bool Step(float dt)
        {
            if (_tweening)
            {
                _tweenTime += dt;
                float k = _tweenDuration > 0f ? Mathf.Clamp01(_tweenTime / _tweenDuration) : 1f;
                tintColor = Color.Lerp(_tweenFrom, _tweenTo, k);
                if (!(_tweenTime < _tweenDuration)) _tweening = false;
            }
            if (_entries != null)
            {
                if (!_hasLast || Sq(_last, tintColor) >= ColorEpsilonSq)
                {
                    DoColorChange();
                    return enabled;
                }
            }
            enabled = false;
            return false;
        }

        static float Sq(Color a, Color b)
        {
            float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b, da = a.a - b.a;
            return dr * dr + dg * dg + db * db + da * da;
        }

        /// <summary>自检用：把收集到的第 <paramref name="i"/> 件的当前颜色读回来（-1 = 读不到）。</summary>
        public Color CurrentColorAt(int i)
        {
            if (i < 0 || i >= _entries.Count) return new Color(-1f, -1f, -1f, -1f);
            var e = _entries[i];
            if (e.quad != null) return e.quad.Tint;
            if (e.label != null) return e.label.color;
            if (e.renderer != null && e.renderer.sharedMaterial != null) return e.renderer.sharedMaterial.GetColor(e.prop);
            return new Color(-1f, -1f, -1f, -1f);
        }
    }
}
