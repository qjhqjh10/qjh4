// UguiRect.cs — **UGUI `RectTransform` 的锚点公式**，C# 这一份，逐条照原版语义抄。
//
// 为什么要它：原版界面参数全是 `(anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta)` 五元组，
// 我们要把它们摊成「绝对像素矩形」才能摆世界空间 mesh。**手推这个公式我连错两次**
// （把 `anchoredPosition.y` 的符号搞反 ⇒ 整层偏 70.94，还因此改错了一份文档），
// ⇒ 判据做成**两份独立实现**：这一份（运行时）与 `工具/menu_rect.py`（回原始 JSON 复算）。
// **自检拿 Python 算出来的数当期望值**去比这一份的结果 —— 两边独立，对不上就是有一边错了。
//
// 公式（出处：UGUI `RectTransform` 的语义，不是猜的）：
//   refNorm = lerp(anchorMin, anchorMax, pivot)          ← **参考点是 pivot 在锚框里的插值处**
//   refPx   = parent.topLeft + (refNorm.x * pw, (1 - refNorm.y) * ph)
//   pivotPx = refPx + (pos.x, -pos.y)                    ← pos.y **向上为正**；本坐标系 y 向下 ⇒ 取负
//   size    = (|aMax.x-aMin.x| * pw + sizeDelta.x, |aMax.y-aMin.y| * ph + sizeDelta.y)
//   rect    = [pivotPx.x - pivot.x*w, pivotPx.x + (1-pivot.x)*w]
//           × [pivotPx.y - (1-pivot.y)*h, pivotPx.y + pivot.y*h]
//
// 坐标系：**左上原点、y 向下**（和原版 JSON / `菜单全树.md` / `资料/*_原版规格.md` 的表一致）。
// 转世界坐标只走 `LayoutSpace.RectCenter`。
//
// ⚠️ **不含 LayoutGroup**：被布局组排的子节点，公式给的是「布局跑之前的模板位」。
//    布局组的算法（`VerticalLayoutGroup` / `HorizontalLayoutGroup`）见 `UguiLayout`。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>UGUI 的一个矩形（左上原点、y 向下，单位 = 设计像素 1920×1080）。</summary>
    public struct PxRect
    {
        public float x1, y1, x2, y2;
        public PxRect(float x1, float y1, float x2, float y2) { this.x1 = x1; this.y1 = y1; this.x2 = x2; this.y2 = y2; }
        public float W { get { return x2 - x1; } }
        public float H { get { return y2 - y1; } }
        public float CX { get { return (x1 + x2) * 0.5f; } }
        public float CY { get { return (y1 + y2) * 0.5f; } }
        public override string ToString() { return $"({x1:F2},{y1:F2})-({x2:F2},{y2:F2}) {W:F2}x{H:F2}"; }
    }

    public static class UguiRect
    {
        /// <summary>按原版五元组算子节点的绝对矩形。`parent` 用左上原点、y 向下。</summary>
        public static PxRect Child(PxRect parent, Vector2 aMin, Vector2 aMax, Vector2 pivot,
                                   Vector2 pos, Vector2 sizeDelta)
        {
            float pw = parent.W, ph = parent.H;
            float refNx = aMin.x + (aMax.x - aMin.x) * pivot.x;
            float refNy = aMin.y + (aMax.y - aMin.y) * pivot.y;
            float refX = parent.x1 + refNx * pw;
            float refY = parent.y1 + (1f - refNy) * ph;

            float px = refX + pos.x;
            float py = refY - pos.y;                       // pos.y 向上为正

            float w = Mathf.Abs(aMax.x - aMin.x) * pw + sizeDelta.x;
            float h = Mathf.Abs(aMax.y - aMin.y) * ph + sizeDelta.y;

            return new PxRect(px - pivot.x * w, py - (1f - pivot.y) * h,
                              px + (1f - pivot.x) * w, py + pivot.y * h);
        }

        /// <summary>不传 `pivot` 的重载（默认 `(0.5,0.5)`）。</summary>
        public static PxRect Child(PxRect parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sizeDelta)
            => Child(parent, aMin, aMax, new Vector2(0.5f, 0.5f), pos, sizeDelta);

        // ---- 常用锚点的速记（原版表里出现最多的几种，写代码时少写一堆 Vector2）----
        public static readonly Vector2 A00 = new Vector2(0f, 0f);
        public static readonly Vector2 A10 = new Vector2(1f, 0f);
        public static readonly Vector2 A01 = new Vector2(0f, 1f);
        public static readonly Vector2 A11 = new Vector2(1f, 1f);
        public static readonly Vector2 AStretch = new Vector2(-1f, -1f);   // 哨兵：表示「锚框拉满」
        public static readonly Vector2 P00 = new Vector2(0f, 0f);
        public static readonly Vector2 P01 = new Vector2(0f, 1f);
        public static readonly Vector2 P11 = new Vector2(1f, 1f);
        public static readonly Vector2 P50 = new Vector2(0.5f, 0f);
        public static readonly Vector2 P51 = new Vector2(0.5f, 1f);
        public static readonly Vector2 P50c = new Vector2(0.5f, 0.5f);
    }

    /// <summary>
    /// UGUI 的布局组（只做我们真用到的那几种）。**算法照 UGUI 的语义**：
    /// `VerticalLayoutGroup` 从**上边**开始按 `padding.top` 起排，逐个累加 `spacing`；
    /// `HorizontalLayoutGroup` 从**左边**开始按 `padding.left` 起排。
    /// ⚠️ 原版大量用 `childControlWidth/Height` 决定「是不是把子节点强行撑到容器宽度」——
    /// 只写我们查到的那些组合，**没查到的组合不要猜**（宁可不出这个重载）。
    /// </summary>
    public static class UguiLayout
    {
        /// <summary>`VerticalLayoutGroup`：给第 i 个子节点（高 `childH`）在容器里的矩形。
        /// 只覆盖「`childControlWidth=1`（宽度撑满容器减 padding）+ `childControlHeight=0`（高度用子节点自己的）」
        /// —— 这正是原版 `Tab Buttons`（padTop 120 · spacing 0）与 `Daily Missions Holder`（spacing 18.55）的组合。</summary>
        public static PxRect VerticalChild(PxRect container, float childH, int index,
                                           float padTop, float padLeft, float padRight, float spacing)
        {
            float w = container.W - padLeft - padRight;
            float top = container.y1 + padTop + (childH + spacing) * index;
            return new PxRect(container.x1 + padLeft, top, container.x1 + padLeft + w, top + childH);
        }

        /// <summary>`HorizontalLayoutGroup`：第 i 个。`childH` = **子件自己的高**
        /// （原版这些组的 `childControlHeight = 0` ⇒ 不把子件撑到容器高）。
        /// 🔴 第一版拿「容器高 - padding」当子件高 ⇒ 里程碑（作者 40×40）被画成 **40×80 的竖椭圆**
        ///    （`40k_missions_milestone_on` 是 67×66 的圆，拉伸就成蛋）。**并排看图才发现，断言全绿。**
        /// 对齐取 **MiddleCenter**（原版这些组 `m_ChildAlignment` = 4）。</summary>
        public static PxRect HorizontalChild(PxRect container, float childW, float childH, int index,
                                             float padLeft, float spacing)
        {
            float left = container.x1 + padLeft + (childW + spacing) * index;
            float top = container.CY - childH * 0.5f;
            return new PxRect(left, top, left + childW, top + childH);
        }
    }
}
