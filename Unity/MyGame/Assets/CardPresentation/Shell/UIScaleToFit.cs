// UIScaleToFit.cs — 通用件：**等比缩放到目标框**（原版同名类，183 处实例）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【它在什么时候改什么（一句话判据）】
//   `Fit()` 取**目标矩形**的尺寸（`fitInParent` ⇒ 父节点的 `RectTransform`），按 `paddingType` 减去/乘上 `padding`，
//   再取 `min(目标宽/自身宽, 目标高/自身高)` 写进 `rect.localScale = (s,s,s)`；
//   **目标为空 ⇒ `CustomDebug.LogWarning("Trying to scale ui without target")` 并返回**；
//   `s` 是 NaN/±∞、或 `|s| < Mathf.Epsilon × 8` ⇒ **不写**（原版那两道保护）。
//   调用时机：`Start` / `OnEnable` 各一次 · `Update` 里 `transform.hasChanged` 时一次 ·
//   `SetLayoutHorizontal` / `SetLayoutVertical`（= 布局重建时）各一次。
//
// 【判据（第一权威 = 反编译）】
//   · `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/UIScaleToFit.cs`（:28-198）
//   · 算式逐句对过原始指令流：`D:/2/tools/decomp_full/UIScaleToFit__Fit.c`
//     （`paddingType == Flat`：`目标宽 + padding.x` · `目标高 + padding.y`；
//      `Multiplier`：`目标宽 * padding.x` · `目标高 * padding.y`；
//      `s = min(宽比, 高比)`；`ABS(s) < 0x7f800001 && ABS(s) != INFINITY` 才继续；
//      `|s| < epsilon*8` 时不写；写入前 `DrivenRectTransformTracker.Add(..., localScale)`）
//   · `Awake`（:28-53）/ `InitializeComponents`（:84-106）都做同一件事：`rect = GetComponent<RectTransform>()`，
//     `fitInParent` 时 `targetRect = parent.GetComponent<RectTransform>()`
//   · `Update`（:73-82）用 `Transform.hasChanged` 去抖，跑完把它置回 false
//   · `OnRectTransformDimensionsChange`（:184-188）= `LayoutRebuilder.MarkLayoutForRebuild(rect)`
//
// 【与我们的世界的**结构性适配**（如实标，不是复刻）】
//   · 我们**没有 uGUI 布局系统**：`ILayoutSelfController` / `DrivenRectTransformTracker` /
//     `LayoutRebuilder` 三样都不存在（uGUI 包不在工程里）。本件保留 `SetLayoutHorizontal/Vertical → Fit()`
//     两个入口（自检/将来的布局驱动可以直调），`OnRectTransformDimensionsChange` 直接 `Fit()`
//     （原版是「标脏 → 稍后重建 → 再回调」，**终态相同**；⚠️ 这条是等价推断，不是逐句复刻）。
//   · 🔴 **冲突（按用户要求先停下来写进报告，不在这里发明折中）**：我们的节点树里**大量父节点是
//     `0×0` 的锚点/容器**（`MenuDraw.Node` 只为定位建节点，尺寸在子件上），而原版那 183 处全都靠
//     uGUI 布局把父框撑出来。⇒ **照抄「fitInParent ⇒ 父节点」在这类节点上会算成 0/0**，原版的两道保护
//     会让它**静默不缩放**。本件的处理：**不猜父框**，只在「自身宽/高 ≤ 0」或「目标宽/高 ≤ 0」时
//     **出声一次**并返回；需要缩放时由调用方显式 `Target = <真正那个框>`（或 `SetTargetSizeForTest`）。
//     下一轮接实例时逐处确认父框是否有尺寸 —— 没有就把调用点改成显式目标，⛔ 不许在这里自动往上找。
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

namespace CardPresentation
{
    /// <summary>等比缩放到目标框（原版 `UIScaleToFit`）。判据见文件头。</summary>
    public sealed class UIScaleToFit : MonoBehaviour
    {
        /// <summary>原版 `PaddingType`（0 = Flat · 1 = Multiplier）。</summary>
        public enum PaddingType { Flat, Multiplier }

        /// <summary>原版 `fitInParent = true`（序列化默认，反编译 :16）。</summary>
        public bool fitInParent = true;
        /// <summary>原版 `targetRect`（`fitInParent` 时由 `Awake` 填成父节点）。</summary>
        public RectTransform targetRect;
        /// <summary>原版 `padding`（`Flat` 时**加**、`Multiplier` 时**乘**；默认 `(0,0)` / 实现里按类型取默认）。</summary>
        public Vector2 padding;
        /// <summary>原版 `paddingType`（默认 `Flat=0`）。</summary>
        public PaddingType paddingType = PaddingType.Flat;

        RectTransform _rect;
        bool _initialized;
        bool _warnedZero;
        float _lastScale = float.NaN;
        bool _lastFitValid;

        /// <summary>原版 `rect`（自身 `RectTransform`）。</summary>
        public RectTransform Rect { get { return _rect; } }
        /// <summary>最近一次 `Fit()` 算出的缩放（没算过 / 没写 = NaN）。</summary>
        public float LastScale { get { return _lastScale; } }
        /// <summary>最近一次 `Fit()` 是否真的写了 `localScale`。</summary>
        public bool LastFitValid { get { return _lastFitValid; } }

        /// <summary>建出来并绑好（原版靠 prefab 挂载；我们代码建 ⇒ 程序化入口）。</summary>
        public static UIScaleToFit Attach(RectTransform rt, RectTransform target = null)
        {
            if (rt == null) return null;
            var c = rt.gameObject.GetComponent<UIScaleToFit>();
            if (c == null) c = rt.gameObject.AddComponent<UIScaleToFit>();
            c._rect = rt;
            if (target != null)
            {
                c.targetRect = target;
                c.fitInParent = false;
                c._initialized = true;
            }
            else
            {
                c.InitializeComponents();        // fitInParent 的那条：目标 = 父节点（原版 Awake 同款）
            }
            return c;
        }

        void Awake() { InitializeComponents(); }
        void Start() { Fit(); }
        void OnEnable() { Fit(); }

        /// <summary>原版 `InitializeComponents`：自身 = 本节点的 `RectTransform`；`fitInParent` ⇒ 目标 = 父节点。</summary>
        public void InitializeComponents()
        {
            _rect = GetComponent<RectTransform>();
            if (fitInParent)
            {
                var p = transform.parent;
                targetRect = p != null ? p.GetComponent<RectTransform>() : null;
            }
            _initialized = true;
        }

        void Update()
        {
            // 原版 :73-82
            if (transform.hasChanged)
            {
                Fit();
                transform.hasChanged = false;
            }
        }

        /// <summary>显式换目标（我们的世界专用入口；换完立刻 `Fit`）。</summary>
        public void SetTarget(RectTransform target)
        {
            targetRect = target;
            fitInParent = false;
            Fit();
        }

        /// <summary>原版 `Fit()`（:108-182）。返回是否真的写了 `localScale`。</summary>
        public bool Fit()
        {
            if (!_initialized || _rect == null) InitializeComponents();
            _lastFitValid = false;
            if (targetRect == null)
            {
                // 原版 :180 `CustomDebug.LogWarning("Trying to scale ui without target")`（原文照抄）
                Debug.LogWarning("[UIScaleToFit] Trying to scale ui without target（`" + name + "`）");
                _lastScale = float.NaN;
                return false;
            }
            if (_rect == null)
            {
                Debug.LogWarning("[UIScaleToFit] `" + name + "`：本节点没有 `RectTransform` ⇒ 不缩放（出声，不静默）");
                _lastScale = float.NaN;
                return false;
            }

            var t = targetRect.rect;
            var o = _rect.rect;
            float tw = paddingType == PaddingType.Flat ? t.width + padding.x : t.width * padding.x;
            float th = paddingType == PaddingType.Flat ? t.height + padding.y : t.height * padding.y;

            if (o.width <= 0f || o.height <= 0f || tw <= 0f || th <= 0f)
            {
                if (!_warnedZero)
                {
                    _warnedZero = true;
                    Debug.LogWarning("[UIScaleToFit] `" + name + "`：目标/自身尺寸里有 0 或负数（目标 "
                                   + tw.ToString("F2") + "×" + th.ToString("F2") + " · 自身 "
                                   + o.width.ToString("F2") + "×" + o.height.ToString("F2")
                                   + "）⇒ **不缩放**。我们的父节点常常是 0×0 锚点 —— 需要缩放时请显式 `SetTarget(...)`");
                }
                _lastScale = float.NaN;
                return false;
            }

            float s = Mathf.Min(tw / o.width, th / o.height);
            if (float.IsNaN(s) || float.IsInfinity(s)) { _lastScale = float.NaN; return false; }
            if (Mathf.Abs(s) < Mathf.Epsilon * 8f) { _lastScale = s; return false; }

            _rect.localScale = new Vector3(s, s, s);   // 原版 :66 形状（z 分量的判据读不到；uGUI 下 z 不参与布局）
            _lastScale = s;
            _lastFitValid = true;
            return true;
        }

        // 原版 `ILayoutController` 两个入口（见文件头「没有 uGUI 布局」那条）
        public void SetLayoutHorizontal() { Fit(); }
        public void SetLayoutVertical() { Fit(); }

        protected void OnRectTransformDimensionsChange() { Fit(); }

        /// <summary>自检用：不写 `localScale`、只算这一次的缩放（NaN = 不该写）。</summary>
        public float ComputeScale()
        {
            if (!_initialized || _rect == null) InitializeComponents();
            if (targetRect == null || _rect == null) return float.NaN;
            var t = targetRect.rect;
            var o = _rect.rect;
            float tw = paddingType == PaddingType.Flat ? t.width + padding.x : t.width * padding.x;
            float th = paddingType == PaddingType.Flat ? t.height + padding.y : t.height * padding.y;
            if (o.width <= 0f || o.height <= 0f || tw <= 0f || th <= 0f) return float.NaN;
            float s = Mathf.Min(tw / o.width, th / o.height);
            return (float.IsNaN(s) || float.IsInfinity(s)) ? float.NaN : s;
        }
    }
}
