// ContentSizeFitterMinMax.cs — 原版 uGUI 件 `ContentSizeFitterMinMax`（在 ContentSizeFitter 之后按 min/max 夹尺寸）
// 🆕 2026-10-29 · 执行代理 `F5` · 账 `未分类_其他:195/196/197`
//
// ── 判据（三源同读，2026-10-29）──────────────────────────────────────────────────
//  · 🥇 方法体：`decomp_full/ContentSizeFitterMinMax__ClampSize.c`（全文）
//              `…__SetLayoutHorizontal.c` / `…__SetLayoutVertical.c`（各 3 句）
//  · 🥈 带字段名的那份：`cpp2il/out_cpp2il_dev_ilspy/ContentSizeFitterMinMax.cs:1-28`（六个字段名 + `m_Rect` 缓存）
//  · 字段偏移互证（`.c`）：`+0x38`=clampWidth · `+0x3c`=widthMin · `+0x40`=widthMax ·
//                        `+0x44`=clampHeight · `+0x48`=heightMin · `+0x4c`=heightMax
//
// ── 它到底算什么（逐句对 `.c`）────────────────────────────────────────────────────
//  `SetLayoutHorizontal` / `SetLayoutVertical` = **先调基类那一句、再 ClampSize()**（`.c` 全文就 3 句）。
//  `ClampSize()`：
//      r = rectTransform.sizeDelta;
//      若 clampWidth  ⇒ r.x = (r.x < widthMin) ? widthMin : (r.x > widthMax ? widthMax : r.x);
//      若 clampHeight ⇒ r.y = 同上（heightMin / heightMax）;
//      rectTransform.sizeDelta = r;
//  ⚠️ `.c` 里那两条是**短路 && 链 + 赋值副作用**展开出来的形状
//     （`if ((enabled) && (v = min, v <= x) && (v = x, max < x)) v = max;`）——
//     语义逐条对完就是 **`Mathf.Clamp(x, min, max)`**（`min > x ⇒ min`；否则 `max < x ⇒ max`；否则原值）。
//     本实现直接写 `Mathf.Clamp`（同一个式子，少两次比较往返）；⛔ 别按「必须逐句照抄 && 链」改回去。
//  ⚠️ `rectTransform` 为空 / 取不到 ⇒ 原版走 `FUN_1803f47a0`（= **抛 NRE**）。本实现照抄该语义：
//     直接让 `rectTransform` 的成员访问抛 NRE —— ⛔ 不加兜底（加了就与原版不同）。
//
// ── 挂载点（如实记）──────────────────────────────────────────────────────────
//  原版实例出现在**远端 LiveOps prefab** 里（`判据/B90_C2b_远端prefab几何.md:203/238`：
//  `Timer Description` · `Individual rating value` 两处 TMP + `ContentSizeFitterMinMax`）。
//  **我们今天的窗口没有挂它**：那两处所属的远端活动窗（P2/P3 档）尚未复刻到那一步
//  ⇒ 本类今天**零挂载点**。仍要建：判据 = 铁律 11（原版有的行为都要有，只分先后）。
using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation
{
    /// <summary>原版 `ContentSizeFitterMinMax : ContentSizeFitter` —— 基类（PreferredSize/Unconstrained 那一跳）
    /// 跑完之后，再按**每轴各自的** min/max 把 `sizeDelta` 夹一道。
    /// <para>⚠️ 两轴各有自己的开关（`clampWidth` / `clampHeight`），**不是同一个** —— 只开一轴时另一轴原样通过。</para></summary>
    [AddComponentMenu("Layout/Content Size Fitter MinMax")]
    public class ContentSizeFitterMinMax : ContentSizeFitter
    {
        /// <summary>原版 `clampWidth`（`+0x38`）。</summary>
        [SerializeField] private bool clampWidth;
        /// <summary>原版 `widthMin`（`+0x3c`）。</summary>
        [SerializeField] private float widthMin;
        /// <summary>原版 `widthMax`（`+0x40`）。</summary>
        [SerializeField] private float widthMax;
        /// <summary>原版 `clampHeight`（`+0x44`）。</summary>
        [SerializeField] private bool clampHeight;
        /// <summary>原版 `heightMin`（`+0x48`）。</summary>
        [SerializeField] private float heightMin;
        /// <summary>原版 `heightMax`（`+0x4c`）。</summary>
        [SerializeField] private float heightMax;

        /// <summary>原版 `m_Rect`（`[NonSerialized]`，第一次取到就缓存）。</summary>
        [System.NonSerialized] private RectTransform m_Rect;

        /// <summary>原版私有属性 `rectTransform`：`m_Rect ??= GetComponent&lt;RectTransform&gt;()`（**不**用 `TryGetComponent`，
        /// 取不到就是 `null` —— 后面成员访问照原版抛 NRE）。</summary>
        private RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null) m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        /// <summary>原版 `SetLayoutHorizontal()`：**先调基类**（`ContentSizeFitter.SetLayoutHorizontal`），**再**夹。</summary>
        public override void SetLayoutHorizontal()
        {
            base.SetLayoutHorizontal();
            ClampSize();
        }

        /// <summary>原版 `SetLayoutVertical()`：同上一档（先基类、再夹）。</summary>
        public override void SetLayoutVertical()
        {
            base.SetLayoutVertical();
            ClampSize();
        }

        /// <summary>原版私有 `ClampSize()` —— **每轴独立**按各自的开关夹 min/max（= `Mathf.Clamp`，见文件头）。</summary>
        private void ClampSize()
        {
            RectTransform rt = rectTransform;
            Vector2 size = rt.sizeDelta;
            if (clampWidth) size.x = Mathf.Clamp(size.x, widthMin, widthMax);
            if (clampHeight) size.y = Mathf.Clamp(size.y, heightMin, heightMax);
            rt.sizeDelta = size;
        }

        // ============================================================ 自检口（只读；⛔ 生产路径别调）
        /// <summary>把「进来时的 sizeDelta」喂一遍本件会算出的 sizeDelta（**纯函数**，不碰 `rectTransform`、不调基类）。
        /// <para>为什么要这一口：`SetLayoutHorizontal` 的第一句是**基类**（`ContentSizeFitter` 会按 PreferredSize 改
        /// `sizeDelta`），自检宿主里没有 uGUI 的布局重建 ⇒ 直接比对「输入 → 夹完」这一层才是可断言的那一半
        /// （与 `UICornersGradient.ColorAt` 同一思路）。</para>
        /// <para>⚠️ 两轴都**没开开关**时原值返回（含 NaN/Infinity 也原样过 —— 原版那一条 `Mathf.Clamp` 根本不会跑）。</para></summary>
        public Vector2 ClampForTest(Vector2 sizeDelta)
        {
            if (clampWidth) sizeDelta.x = Mathf.Clamp(sizeDelta.x, widthMin, widthMax);
            if (clampHeight) sizeDelta.y = Mathf.Clamp(sizeDelta.y, heightMin, heightMax);
            return sizeDelta;
        }

        /// <summary>自检用：把六个字段按原版名摆好（**只在自检里调**）。</summary>
        public void ConfigureForTest(bool clampW, float wMin, float wMax, bool clampH, float hMin, float hMax)
        {
            clampWidth = clampW; widthMin = wMin; widthMax = wMax;
            clampHeight = clampH; heightMin = hMin; heightMax = hMax;
        }
    }
}
