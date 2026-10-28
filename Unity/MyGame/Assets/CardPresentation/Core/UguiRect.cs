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
        /// 对齐取 **MiddleCenter**（原版这些组 `m_ChildAlignment` = 4）。
        /// ⚠️ **本重载只对「子件全同宽」成立**（算式把「前面每一件的宽」当成了本件的宽）——
        /// 混宽列一律走下面那个 `float[] childWs` 的重载（🆕 A239）。
        /// 🔴 **2026-10-13（A537）**：当年举的「混宽列」实例（`CampaignRewardWindow` 那一列）**已翻案**
        /// —— 它不是混宽列（那三颗件 `m_IgnoreLayout = 1`、不进布局组）。全文见下面那个重载的注释。</summary>
        public static PxRect HorizontalChild(PxRect container, float childW, float childH, int index,
                                             float padLeft, float spacing)
            // 🔴 **算式逐字未改**（连求值顺序都一样：`container.x1 + padLeft + …` 在 C# 里就是
            // `(container.x1 + padLeft) + …`）⇒ 这一路（旧签名）的调用点**逐位零行为变化**。
            => HChild(container.x1 + padLeft + (childW + spacing) * index, container.CY, childW, childH);

        /// <summary>🔴 **2026-10-12（A239）：逐子件宽度版** —— 第 `index` 个子件（自己的宽 =
        /// `childWs[index]`、高 = `childH`）。算式
        /// `left = 容器左 + padLeft + Σ_{j&lt;index}(childWs[j] + spacing)`：
        /// **前面每一件按它【自己的】宽累加**，不是「一律拿本件的宽 × index」。
        ///
        /// <para>**上面那个等宽重载只对「子件全同宽」成立** —— 混宽时只有 `index 0` 是对的
        /// （算式把「前面每一件的宽」当成了本件的宽）。</para>
        ///
        /// <para>🔴🔴 **2026-10-13（A537）就地更正（铁律 5）：本节原来举的那个「实害」例子不成立。**
        /// 原文写「实害（`Shell/CampaignRewardWindow.cs` 的列：物品 200 / 按钮 245 / 徽标 100）：
        /// `Unlock Button` 实到 1325..1570（应 1280..1525）· `Badge` 实到 1430..1530（应 1550..1650）」
        /// —— **那个「混宽列」是错模型的产物**：原版 `CampaignRewardWindow` 那三颗件
        /// （`Unlock Button` / `Warning` / `Badge`）**全都带 `LayoutElement.m_IgnoreLayout = 1`**
        /// ⇒ 它们**根本不进** holder 的 `HorizontalLayoutGroup`（uGUI `LayoutGroup.cs:52-79` 建
        /// `m_RectChildren` 时跳过 `ILayoutIgnorer.ignoreLayout`；prefab 几何也反证：按钮居中在
        /// holder 中心、徽标落在 holder 左上角，布局组摆不出这两种）
        /// ⇒ 那一列的 `rectChildren` **只有物品抽屉、全同宽** ⇒ **A537 之后它不再是混宽列**。
        /// ⚠️ **本重载因此从「修缺陷」降级为「防线」**：`BuildColumn` 仍然用它（表 = `[ItemW]×n`），
        /// 是为了让「容器宽」与「子件落点」继续从**同一张表**派生；今天它与等宽重载**逐位同值**。
        /// ⛔ 但**别把这段当成「那一列是混宽的」的依据** —— 判据已经翻了（见上面那段与
        /// `Shell/CampaignRewardWindow.cs` 的 `BuildColumn`）。</para>
        ///
        /// <para>⚠️ **传进来的表必须与「子件次序」逐格对应**（`index` 是**这张表里的下标**，不是「第几个
        /// 建的」）：UGUI 的布局组**跳过 `activeSelf == false` 的子件** ⇒ 不参与排布的子件
        /// **一格都不能占**（占一格 = 后面每一件整体错位一格）；
        /// ⚠️ 同一个过滤里还有**第二条**判据（`ILayoutIgnorer.ignoreLayout`，A537 查实的那个）
        /// —— 两条都跳过，见 `LayoutGroup.cs:52-79`。</para>
        ///
        /// <para>与 <see cref="HorizontalContentW"/> 用**同一张表**算「容器内容宽」，两处再也对不上不成立
        /// （CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。</para>
        ///
        /// <para>越界（表里没有这一格）⇒ **出声**并返回退化矩形（⛔ 不静默：那是调用点的次序写错了，
        /// 画出来的是「堆在容器左沿的一个点」，一眼可见，见 CLAUDE.md「不许静默失败」）。</para></summary>
        public static PxRect HorizontalChild(PxRect container, float[] childWs, float childH, int index,
                                             float padLeft, float spacing)
        {
            if (childWs == null || index < 0 || index >= childWs.Length)
            {
                Debug.LogError($"[UguiLayout] HorizontalChild：子件宽度表有 {((childWs == null) ? 0 : childWs.Length)} 格，"
                               + $"取不到第 {index} 格 —— 逐子件宽度数组与子件次序对不上（判据见 Core/UguiRect.cs 该方法）");
                return new PxRect(container.x1, container.CY, container.x1, container.CY);
            }
            float left = container.x1 + padLeft;
            for (int j = 0; j < index; j++) left += childWs[j] + spacing;
            return HChild(left, container.CY, childWs[index], childH);
        }

        /// <summary>矩形的**唯一一份**造法：给「左边缘」+「子件自己的宽高」出矩形
        /// （两条算式只在「左边缘怎么算」那一格上不同，⛔ 别把 `container.CY − childH·0.5`
        /// 这个中心对齐式再抄一遍）。</summary>
        static PxRect HChild(float left, float cy, float childW, float childH)
        {
            float top = cy - childH * 0.5f;
            return new PxRect(left, top, left + childW, top + childH);
        }

        /// <summary>`HorizontalLayoutGroup` 的**另一种组合**（2026-09-23 查到的）：
        /// `childControlWidth = 0` + **`childControlHeight = 1`** + `MiddleCenter`
        /// （出处：`Campaign Reward Window` 两列的 `Rewards`，`MonoBehaviour_3702268942507023494.json`
        /// 与 `…_8535935147395751046.json` 原文 —— 两列各一份、字段完全一致）。
        /// <para>✅ **实测结论：这一组的结果与 `HorizontalChild` 完全一致，不必另写一个重载。**</para>
        /// <para>理由（`childControlHeight = 1` 的语义 = **用子件的「首选高」**，不是「撑满容器」）：
        /// UGUI 里 `ChildControlHeight` 为真时，子件高 = `LayoutUtility.GetPreferredHeight(子件)`；
        /// 而**没有 `ILayoutElement` 的纯 `RectTransform` 的首选高就是它自己的 `sizeDelta.y`**。
        /// 铁证（这一页自己的两个子件）：`Unlock Button` 的 `sizeDelta.y = 45`、`Badge` 是 `100`
        /// —— 若真是「撑满容器」，这两个会被拉成 505 高；而原版给它们写的就是 45 / 100。</para>
        /// ⚠️ **别照「childControlHeight=1 ⇒ 撑满」去实现** —— 那会把按钮画成 245×505 的竖条，
        /// 而且**没有任何断言会红**（矩形中心还对）。</summary>
        public static PxRect HorizontalChildOwnHeight(PxRect container, float childW, float childH, int index,
                                                      float padLeft, float spacing)
            => HorizontalChild(container, childW, childH, index, padLeft, spacing);

        /// <summary>同上，**逐子件宽度**版（🆕 2026-10-12 · A239）—— 转调
        /// <see cref="HorizontalChild(PxRect, float[], float, int, float, float)"/>，
        /// 两条路**共用一份算式**（⛔ 别在这里重写一遍偏移累加）。</summary>
        public static PxRect HorizontalChildOwnHeight(PxRect container, float[] childWs, float childH, int index,
                                                      float padLeft, float spacing)
            => HorizontalChild(container, childWs, childH, index, padLeft, spacing);

        /// <summary>`HorizontalLayoutGroup` 的**内容宽** = `padding.left + Σ子件宽 + spacing × (n−1) + padding.right`。
        /// 用在「容器自己带 `ContentSizeFitter`（`m_HorizontalFit = 1` = MinSize）」的场合 ——
        /// 那种容器出厂 `sizeDelta.x = 0`（宽是**布局跑完才有的**），所以它的矩形得**先算内容宽**才摆得出来。
        /// 出处：`Campaign Reward Window` 两列 `Rewards`（`MonoBehaviour_-5736334674743157626.json` =
        /// `m_HorizontalFit 1 / m_VerticalFit 0`）。</summary>
        public static float HorizontalContentW(float[] childWs, float padLeft, float padRight, float spacing)
        {
            if (childWs == null || childWs.Length == 0) return padLeft + padRight;
            float sum = 0f;
            for (int i = 0; i < childWs.Length; i++) sum += childWs[i];
            return padLeft + padRight + sum + spacing * (childWs.Length - 1);
        }

        /// <summary>`HorizontalLayoutGroup` + `ContentSizeFitter(H=MinSize)`，**容器自己锚在父的中心**
        /// （原版两处阵营条都是这个形状：`Army Content` 的五元组 = 「**父中心的一个零宽点**」——
        /// 锻造页 `N(0, .5,.5, .5,.5, .5,.5, 6.1e-05,0, 0,130)`、战役页
        /// `N(2, .5,1, .5,1, .5,.5, -0.0010376,-65, 0,130)`）。
        /// ⇒ 布局跑完之后内容**以中心对称展开** ⇒ 条目是**居中**排的、**不是从容器左边缘开始**。
        /// 🔴 2026-09-23 找茬查出（`项目任务.md` §三 第 15 条第 18 行）：锻造页与战役页**都**照
        /// `HorizontalChild(_selR, …)` 从选择条**左边缘**排 ⇒ 13 个条目右端溢到屏外、
        /// **最后几个阵营既看不见也点不到**（居中之后锻造页 13 个全落在 323.01..1927.69 内）。
        /// 用法：**先拿内容矩形**（本方法），再 `HorizontalChild(内容矩形, …)` 摆条目。</summary>
        public static PxRect HorizontalContentCentered(PxRect container, int count, float childW,
                                                      float padLeft, float padRight, float spacing)
        {
            float[] ws = count > 0 ? new float[count] : null;
            for (int i = 0; ws != null && i < count; i++) ws[i] = childW;
            float w = HorizontalContentW(ws, padLeft, padRight, spacing);
            return new PxRect(container.CX - w * 0.5f, container.y1, container.CX + w * 0.5f, container.y2);
        }
    }

    /// <summary>
    /// **矩形工具族** —— 逐条照原版 `RectExtensions` / `RendererExtensions` 抄（账 505 / 544 / 546）。
    /// 原版是 `RectTransform` + 世界坐标；本工程界面是 `ImageQuad` 过程化网格（**没有 uGUI RectTransform 层级**），
    /// ⇒ 判定本体做成 **`PxRect`（左上原点、y 向下，单位 = 设计像素）** 的纯算术，行为与原版逐句对位：
    ///   · `Overlaps` = **严格不等的 AABB**（贴边不算相交，`a.x1 &lt; b.x2 &amp;&amp; b.x1 &lt; a.x2 &amp;&amp; a.y1 &lt; b.y2 &amp;&amp; b.y1 &lt; a.y2`）。
    ///     判据 = `decomp_full/RectExtensions__OverlapsAny.c`（`fVar13+fVar11 &lt;= fVar12` 那三条「不交」取反）
    ///     ＋ `out_cpp2il_dev_ilspy/RectExtensions.cs:69-244`，两份一致；
    ///     旁证 = `Shell/CollectionWindow.cs:5116`（早先手写内联的就是这条同式）。
    ///   · `OverlapsAny` = **按传入 `rects` 的枚举顺序，返回第一个相交的**（原版 `if (bVar1) { *param_3 = …; return 1; }`），
    ///     ⛔ **不排序、不取最近**。
    /// </summary>
    public static partial class RectExtensions
    {
        /// <summary>两个 `PxRect` 是否**严格**相交（AABB；贴边不算）。原版 `RectExtensions.OverlapsAny` 的相交判据。</summary>
        public static bool Overlaps(PxRect a, PxRect b)
            => a.x1 < b.x2 && b.x1 < a.x2 && a.y1 < b.y2 && b.y1 < a.y2;

        /// <summary>在 `rects` 里找**第一个**与 `rect` 严格相交的矩形（按枚举序），命中就写进 `overlap` 并返回真。
        /// 原版 `RectExtensions.OverlapsAny(RectTransform, IEnumerable&lt;RectTransform&gt;, out RectTransform)`：
        /// **按传入序返回第一个相交的**，不相交就继续 —— 没有排序 / 没有取最近（判据见本类头）。</summary>
        public static bool OverlapsAny(PxRect rect, System.Collections.Generic.IEnumerable<PxRect> rects, out PxRect overlap)
        {
            if (rects != null)
            {
                foreach (var b in rects)
                {
                    if (Overlaps(rect, b)) { overlap = b; return true; }
                }
            }
            overlap = default(PxRect);
            return false;
        }

        /// <summary>只要「有没有命中」那一档（不需要命中物本体时的便捷重载）。</summary>
        public static bool OverlapsAny(PxRect rect, System.Collections.Generic.IEnumerable<PxRect> rects)
        {
            PxRect ignored;
            return OverlapsAny(rect, rects, out ignored);
        }
    }

    /// <summary>
    /// **屏幕可见性判定族** —— 逐条照原版 `RendererExtensions` 抄（账 544 / 546；545 属原版 D 行一并补齐）。
    /// 原版签名 = `(RectTransform, Camera)`：`GetWorldCorners` 取四角 → 逐个 `Camera.WorldToScreenPoint`
    /// → 统计落在**屏幕矩形 `[0,Screen.width) × [0,Screen.height)`** 内的角数。
    /// 判据 = `decomp_full/RendererExtensions__CountCornersVisibleFrom.c`（四个越界 `if` 依次排除，
    /// **只有四角全在屏内那一支才 `num4 + 1`**；il2csharp 版同名方法取值被反编译器搅乱，以 `.c` 为准）。
    /// ⚠️ 消费者 = 原版收件箱 `MessageContainer` 的「真在屏幕内才播 slotting 音效」；
    ///    本工程该宿主在 `Shell/InboxWindow.cs`（不在本批白名单）⇒ 方法先落地，接线另行。
    /// </summary>
    public static class RendererExtensions
    {
        /// <summary>四角里**落在屏幕矩形内**（`0 &lt;= sp.x &lt; width` 且 `0 &lt;= sp.y &lt; height`）的个数（0..4）。</summary>
        public static int CountCornersVisibleFrom(RectTransform rectTransform, Camera camera)
        {
            if (rectTransform == null || camera == null) return 0;
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            int w = Screen.width, h = Screen.height, count = 0;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 sp = camera.WorldToScreenPoint(corners[i]);
                if (sp.x >= 0f && sp.x < w && sp.y >= 0f && sp.y < h) count++;
            }
            return count;
        }

        /// <summary>至少有一个角在屏内（`Count &gt; 0`）。原版 `RendererExtensions.IsVisibleFrom`。</summary>
        public static bool IsVisibleFrom(RectTransform rectTransform, Camera camera)
            => CountCornersVisibleFrom(rectTransform, camera) > 0;

        /// <summary>四角全在屏内（`Count == 4`）。原版 `RendererExtensions.IsFullyVisibleFrom`（原版 D 行：出货 0 调用）。</summary>
        public static bool IsFullyVisibleFrom(RectTransform rectTransform, Camera camera)
            => CountCornersVisibleFrom(rectTransform, camera) == 4;
    }

    /// <summary>
    /// **`UIExtension`** —— 逐条照原版 `UIExtension` 抄（账 668～672）。
    /// 判据 = `decomp_full/UIExtension__GetCorners.c` ＋ `__MaxX.c` / `__MaxY.c` / `__MinX.c` / `__MinY.c`。
    /// 🔴 **四个取值器【不是】「扫四角取最大/最小」，是按固定下标取某一颗角**（`.c` 里是数组字节偏移，逐条核过）：
    ///   · `MaxX` = `corners[2].x`（偏移 `0x38` = `0x20` + 2×0xC）＝ **右上**的 x
    ///   · `MaxY` = `corners[1].y`（偏移 `0x30` = `0x20` + 1×0xC + 4）＝ **左上**的 y
    ///   · `MinX` = `corners[0].x`（`0x20`）· `MinY` = `corners[0].y`（`0x24`）＝ **左下**
    ///   （每个取值器的「长度校验」也逐条对上：MaxX 要 `2 < count`、MaxY 要 `1 < count`、MinX/MinY 要 `count != 0`。）
    ///   Unity `GetWorldCorners` 的角序 = 左下 → 左上 → 右上 → 右下（旁证 `Battle/HudPiece.cs:441`）。
    ///   ⇒ 名字叫 Max/Min，实际是「取那一颗角」；**轴对齐矩形上两者同值，旋转过的矩形上不同**
    ///   （铁律 5·c：一个值 ≠ 全部情况）。⛔ 别「优化」成 `Mathf.Max` 扫描 —— 那是另一个函数。
    /// ⚠️ 原版把四角数组建在**静态字段**里复用（`DAT_18429f330`）⇒ 这里同样复用一份，
    ///   语义（含「后一次调用会覆盖上一次返回的数组」）与原版一致。⛔ 别改成每次 `new`。
    /// ⚠️ 原版 `null` 入参走的是 `throw`（`FUN_1803f47a0` = null-check 抛出）⇒ 这里也抛，⛔ 不静默返回空数组。
    /// </summary>
    public static class UIExtension
    {
        static readonly Vector3[] _corners = new Vector3[4];

        /// <summary>`GetWorldCorners(4)` 的四角（**复用同一份数组**：左下→左上→右上→右下）。原版 `UIExtension.GetCorners`。</summary>
        public static Vector3[] GetCorners(RectTransform rectTransform)
        {
            if (rectTransform == null) throw new System.ArgumentNullException(nameof(rectTransform));
            rectTransform.GetWorldCorners(_corners);
            return _corners;
        }

        /// <summary>`corners[2].x`（右上）。原版 `UIExtension.MaxX`（⛔ 不是四角里的最大值）。</summary>
        public static float MaxX(RectTransform rectTransform) { GetCorners(rectTransform); return _corners[2].x; }
        /// <summary>`corners[1].y`（左上）。原版 `UIExtension.MaxY`（⛔ 不是四角里的最大值）。</summary>
        public static float MaxY(RectTransform rectTransform) { GetCorners(rectTransform); return _corners[1].y; }
        /// <summary>`corners[0].x`（左下）。原版 `UIExtension.MinX`。</summary>
        public static float MinX(RectTransform rectTransform) { GetCorners(rectTransform); return _corners[0].x; }
        /// <summary>`corners[0].y`（左下）。原版 `UIExtension.MinY`。</summary>
        public static float MinY(RectTransform rectTransform) { GetCorners(rectTransform); return _corners[0].y; }
    }

    public static partial class RectExtensions
    {
        /// <summary>**世界矩形**（中心 + 尺寸）。原版 `RectExtensions.GetWorldRect(RectTransform)`：
        /// `GetCorners` → 以 `corners[0]` 为基点、`corners[2]` 为对角 → 中心 = 基点 + 对角线 × 0.5、size = 对角线。
        /// 判据 = `decomp_full/RectExtensions__GetWorldRect.c:20-29`（`DAT_1834b2bb4` = **0.5**；用第 0 / 第 2 颗
        /// = **对角** ⇒ 旋转过的矩形上它也得到轴对齐包围盒的中心与尺寸）。
        /// <para>⚠️ 原版写的是 `return` 一个 `Rect`（`float*` 出参）⇒ 这里用 `UnityEngine.Rect` 直接返回，字段序一致
        /// （x, y = 中心；width, height = 尺寸）。</para></summary>
        public static Rect GetWorldRect(RectTransform rect)
        {
            Vector3[] c = UIExtension.GetCorners(rect);
            float w = c[2].x - c[0].x;
            float h = c[2].y - c[0].y;
            return new Rect(c[0].x + w * 0.5f, c[0].y + h * 0.5f, w, h);
        }
    }
}
