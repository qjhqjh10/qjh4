// MenuDraw.cs — 菜单界面**共用的绘图助手**（原版像素矩形 → 世界坐标 → 摆一张图 / 一段字）
//
// ============================ 为什么要单独一份 ============================
// 阶段二这几层（`RewardsWindow` · `PromptPopup` · `DailyRewardPopup` · 后面还有锻造厂/商店/卡组线）
// 都要做同一件事：拿**原版 prefab 的像素矩形**（左上原点、y 向下，见 `资料/*_原版规格.md` 的表）
// 在场景里摆一个 `ImageQuad` / `Label`。这套换算**只能有一份**（CLAUDE.md §三：
// 「两处写同一条规则 = 迟早不一致」）。
//
// 🔴 **三条已经踩过的坑，都固化在这里**：
//   ① `ImageQuad.Create` / `Label.Create` 的 `pos` 是 **`localPosition`**（相对父节点），
//      而 `LayoutSpace.RectCenter` 给的是**世界坐标** ⇒ 必须减掉父节点的世界位置
//      （不减 = 父节点一有偏移就**双倍错位**，而断言量矩形中心、量不到）。
//   ② 分层用**渲染队列**、不能用 z（全是透明队列，按到相机的 3D 距离排序，屏幕中间的反而更近）
//      ⇒ 每个件都显式 `SetRenderQueue`。
//   ③ 原版 `Image.m_PreserveAspect = 1` 的那些件要**等比放进框、居中**（UGUI 用 `pivot` 定位，
//      这几处的 pivot 实测都是 (.5,.5)）；`Image` **没有 sprite 也会渲染**（回落 `Graphic.OnPopulateMesh`
//      画一块纯色矩形）—— 所以「没 sprite + 只有 `m_Color`」的件要照画。
using UnityEngine;

namespace CardPresentation
{
    public static class MenuDraw
    {
        /// <summary>原版像素矩形中心 → **相对 `parent` 的局部坐标**（见文件头坑①）。</summary>
        public static Vector3 Local(Transform parent, float x1, float y1, float x2, float y2)
            => LayoutSpace.RectCenter(x1, y1, x2, y2) - (parent != null ? parent.position : Vector3.zero);

        /// <summary>清空一个节点的全部子件。🔴 **批处理下必须 `DestroyImmediate`** —— 没有帧循环，
        /// `Destroy` 不会立刻消失，会和新净的叠在一起。
        /// ⚠️ 2026-09-27：这段原来只在 `MainMenuSubmenuWindow.DestroyChildren` 一处，
        /// 聊天窗（不是 `MainMenuSubmenuWindow` 的子类）也要用 ⇒ **收口到这里**，那边**转调**。</summary>
        public static void ClearChildren(Transform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var go = root.GetChild(i).gameObject;
#if UNITY_EDITOR
                if (!Application.isPlaying) { Object.DestroyImmediate(go); continue; }
#endif
                Object.Destroy(go);
            }
        }

        /// <summary>空节点（**有矩形语义** —— 原版每个节点都有自己的 rect，位置要摆对，
        /// 否则自检量不到、将来做点击/滚动也会算错）。</summary>
        public static Transform Node(Transform parent, string name, PxRect r)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = Local(parent, r.x1, r.y1, r.x2, r.y2);
            return t;
        }

        // ============================================================ 裁切（等效 `RectMask2D`）

        /// <summary>🔴 **“越界怎么算”只此一份** —— 把 `r` 与裁切边界 `clip` 求交。
        /// 返回 **false = 整块在框外 ⇒ 调用方一律「不建」**（画不出、也点不到，与原版被裁掉的部分一致）。
        /// `outRect` = 实际要画 / 要吃点击的那块；**无裁切、或整块在框内 ⇒ 原样返回 `r`**
        /// （`SameRect(outRect, r)` 就能判出「没被截」）。滚动区把 `Viewport` 给进来，等价原版
        /// `Viewport` 上那个 `RectMask2D`。
        ///
        /// 🔴 **判据 = 原版 `RectMask2D`**（它有**两副面孔**，两副都要等效）：
        ///   · **渲染**：走 `IClipper` 管线，越界的像素画不出来 ⇒ 我们截矩形 + 截 uv（只截矩形不截 uv 会把图压扁）；
        ///   · **射线**：它还实现 `ICanvasRaycastFilter`，`IsRaycastLocationValid`
        ///     （UGUI 源码 `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/RectMask2D.cs:178-185`）
        ///     就是一句 `RectTransformUtility.RectangleContainsScreenPoint(...)` ⇒ **框外的点判不中任何东西**
        ///     ⇒ 我们这边的**命中区也要截到框内**（`Hit` / `DeckCell` 用它）。
        /// ⇒ `Rect` / `Nine` / `Hit` / `DeckCell` **全部转调这一份**（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
        /// ⚠️ 退化矩形（宽或高 ≤ 0.01）在**有裁切**时一律判不可见 —— 这是 `Rect` 原来就有的行为，别丢。</summary>
        public static bool ClipRect(PxRect r, PxRect? clip, out PxRect outRect)
        {
            outRect = r;
            if (!clip.HasValue) return true;
            if (r.W <= 0.01f || r.H <= 0.01f) return false;
            var c = clip.Value;
            // 整块在框内 ⇒ **原样返回 `r`**（不新建矩形 ⇒ 调用方能靠逐字段比判出「没截」）
            if (r.x1 >= c.x1 && r.x2 <= c.x2 && r.y1 >= c.y1 && r.y2 <= c.y2) return true;
            float x1 = Mathf.Max(r.x1, c.x1), x2 = Mathf.Min(r.x2, c.x2);
            float y1 = Mathf.Max(r.y1, c.y1), y2 = Mathf.Min(r.y2, c.y2);
            if (x2 <= x1 + 0.01f || y2 <= y1 + 0.01f) return false;
            outRect = new PxRect(x1, y1, x2, y2);
            return true;
        }

        /// <summary>两个 `PxRect` 逐字段相等没有（`PxRect` 是 `struct`、没重写 `Equals`；
        /// 用来判「`ClipRect` 有没有真的截掉一块」—— 自检也可以拿它量「没人动过这个矩形」）。</summary>
        public static bool SameRect(PxRect a, PxRect b)
            => a.x1 == b.x1 && a.x2 == b.x2 && a.y1 == b.y1 && a.y2 == b.y2;

        /// <summary>🔴 **「整块在框外 ⇒ 不建」的公共函数**（A25④ 收口 · 2026-10-04）——
        /// `true` = 还有可见部分（**不保证整块在框内**）、`false` = 整块在框外 ⇒ 调用方**一律不建**。
        /// 谁该用它：**建不出几何/uv 的那些件**（文字、纯逻辑节点）—— 图那一路走 `ClipRect`（它还要 `outRect`）。
        ///
        /// 🔴 **和 `ClipRect` 的关系（别把两者合并）**：判据同一条（两轴都判），差别只有一处 ——
        ///   `ClipRect` 多一条「**退化矩形**（宽或高 ≤ 0.01）在有裁切时一律判不可见」的守卫，
        ///   那是 `Rect` / `Nine` / `Hit` 那条路要的（那种尺寸建不出 quad）；
        ///   本函数**不引入**那条 —— 文字/节点建得出来，收口前的四处内联也从来没有这条。
        ///   ⇒ **逐字保留原有语义**（收口不许顺手改行为，铁律 12 的精神）。
        /// <para>⚠️ 这与 `MenuScroll.Intersects`（只判**滚动轴**）**不是同一件事** ——
        /// 那个只适合「按滚动方向整块剔除」的构建循环，别互相替代。</para></summary>
        public static bool Visible(PxRect r, PxRect? clip)
        {
            if (!clip.HasValue) return true;
            var c = clip.Value;
            return !(r.x2 <= c.x1 || r.x1 >= c.x2 || r.y2 <= c.y1 || r.y1 >= c.y2);
        }

        // ============================================================ 软边遮罩（原版 `RectMask2D.m_Softness`）
        //
        // 🔴 **原版是什么**：`RectMask2D` 除了硬裁，还有 `m_Softness`（`Vector2Int`，**逐处不同**：
        //    商店 `Packs Scroll View/Viewport` = **(0,25)** · `Forge Army Selector/Viewport` = **(42,0)** ·
        //    练习窗 `Army Selector` = (0,50) · `Reward Window`/`Campaign Reward Window` = (200,0) ·
        //    `Chat Tab/Viewport` = (0,22) · 其余多数是 (0,0)）⇒ 掩码边上有 `softness` 像素宽的**渐隐带**
        //    （`x` 管左右、`y` 管上下；0 = 硬边）。
        //    判据 = uGUI `RectMask2D.m_Softness`（`Runtime/UGUI/UI/Core/RectMask2D.cs:71,284-301`：
        //    `UpdateClipSoftness()` 把 `m_Softness` 交给每个 clippable）→ 由 `UI/Default` shader 的
        //    `_ClipRect` + `_UIMaskSoftnessX/Y` 落成 `saturate(该像素到最近边的距离 ÷ softness)`。
        //    ⚠️ 我们这套**没有 canvas / 没有那个 shader**（全是世界空间 `ImageQuad` + `Sprites/Default`）——
        //    下面是**几何等效**：把 quad 沿渐隐带的**内沿**切开，带宽那一块用**逐顶点 alpha**（`SetCornerColors`，
        //    `Sprites/Default` 会把 `顶点色 × 材质色 × 贴图` 乘起来）做**线性**斜坡。
        //    ⚠️ 与原版的差别只有一处，且**如实记**：原版是逐像素的（含透视插值修正），我们是**线性斜坡 +
        //    分段线性顶点色** —— 对**轴对齐的矩形内容**两者**逐像素等价**（带内 alpha 都是线性的），
        //    只对斜放/带旋转的内容有细微差别（本壳全线轴对齐）。
        //
        // 🔴 **为什么必须切几何，不能只给整块调顶点色**：content 常常**远大于**带宽（商店一格 475px、带 25px）
        //    —— 只给 4 个角做斜坡，整格都会渐变（原版只有靠边的 25px 渐变）。所以按「带的内沿」切开，
        //    带宽那一块单独做斜坡，其余保持 alpha=1。
        //
        // ⚠️ **代价（如实记，别当没发生）**：切出来的**额外块挂成原 quad 的子物体** ——
        //    原 quad 之后若被 `SetTint` / `SetAspect`／`SetActive`：
        //      · `SetActive` ✅ 子物体跟着（正合语义）；`Destroy` ✅ 跟着；
        //      · `SetTint` ❌ 只有原块跟着（子块保持建它那一刻的 tint）—— 本壳**没有**在软边区里
        //        建完再改 tint 的调用点（`Rect` 的 tint 是一次性入参），但**这是真缺口**，已记进交接报告。
        //    ⇒ 画软边区里的东西时，tint/图 **一律走 `Rect`/`Nine` 的入参**，别建完再改。

        /// <summary>软边的 alpha 剖面（单轴）：**离最近的那条框边有多少距离** ÷ 带宽，夹到 [0,1]。
        /// 0 = 正好压在框边上（原版这里 alpha 也是 0）、1 = 已经进到带宽以内。
        /// 🔴 判据 = uGUI `UI/Default` 的 `half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(...)) * IN.mask.zw)`
        /// —— `_ClipRect.zw - _ClipRect.xy` 是框的尺寸、`abs(...)` 是「到框中心的 2 倍距离」
        /// ⇒ 化简后就是「到**最近那条边**的距离」，乘的 `mask.zw` ≈ `1 / softness`。**线性**。</summary>
        static float SoftAlpha(float v, float c1, float c2, float soft)
        {
            if (soft <= 0f) return 1f;
            return Mathf.Min(Mathf.Clamp01((v - c1) / soft), Mathf.Clamp01((c2 - v) / soft));
        }

        /// <summary>一个 quad 在画布 px 里的矩形（世界 → px 只走 `LayoutSpace.ToPixel`，见 `ClipNineChildren`）。</summary>
        static PxRect QuadRectPx(ImageQuad q)
        {
            const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;   // 108 px / 世界单位
            Vector2 cpx = LayoutSpace.ToPixel(q.transform.position);
            float hw = q.WorldW * K * 0.5f, hh = q.WorldH * K * 0.5f;
            return new PxRect(cpx.x - hw, cpx.y - hh, cpx.x + hw, cpx.y + hh);
        }

        /// <summary>软边带宽的**内沿**落在 `[a1,a2]` 内部的那两个断点（最多两个，已排序）。
        /// 只切「真正落在本块内部」的：整块都在带里、或整块都在带外 ⇒ **不切**（剖面在块内是线性的）。</summary>
        static int SoftCuts(float[] dst, float a1, float a2, float p, float q)
        {
            int n = 0;
            if (p > a1 + 0.01f && p < a2 - 0.01f) dst[n++] = p;
            if (q > a1 + 0.01f && q < a2 - 0.01f && (n == 0 || Mathf.Abs(q - p) > 0.01f)) dst[n++] = q;
            if (n == 2 && dst[0] > dst[1]) { float t = dst[0]; dst[0] = dst[1]; dst[1] = t; }
            return n;
        }

        /// <summary>把一个 quad 按软边剖面处理：**必要时沿带的内沿切开**，每块的四角带上 alpha 斜坡。
        /// 最外层的 quad（`vis` = 它**已经硬裁过**的那块）**留在原节点上**（改成「含矩形中心的那一格」），
        /// 其余格建**子 quad**（同图/同队列/同 tint）。`softPx` 两个分量都 0 ⇒ 立刻返回（硬边 = 现有行为）。
        /// ⚠️ **返回后原 quad 的矩形可能不再等于 `vis`**（它是其中一格）—— 断言要按「所有块的并集」量。</summary>
        public static void ApplySoftEdges(ImageQuad q, PxRect vis, PxRect clip, Vector2 softPx)
        {
            if (q == null) return;
            if (softPx.x <= 0f && softPx.y <= 0f) return;
            if (vis.W <= 0.01f || vis.H <= 0.01f) return;

            var cutX = new float[2]; int nx = SoftCuts(cutX, vis.x1, vis.x2, clip.x1 + softPx.x, clip.x2 - softPx.x);
            var cutY = new float[2]; int ny = SoftCuts(cutY, vis.y1, vis.y2, clip.y1 + softPx.y, clip.y2 - softPx.y);
            if (nx == 0 && ny == 0)
            {
                // 整块都在同一个「线性段」里 ⇒ 不动几何，只上四角 alpha（含「整块都在带里」那种）
                SetRamp(q, vis, clip, softPx);
                return;
            }
            var xs = new float[4]; xs[0] = vis.x1; xs[1 + nx] = vis.x2;
            for (int i = 0; i < nx; i++) xs[1 + i] = cutX[i];
            var ys = new float[4]; ys[0] = vis.y1; ys[1 + ny] = vis.y2;
            for (int i = 0; i < ny; i++) ys[1 + i] = cutY[i];
            int sx = nx + 1, sy = ny + 1;                     // 段数

            // 原节点留给**含矩形中心**的那一格（确定性）；其余格建子 quad
            int mi = 0, mj = 0;
            for (int i = 0; i < sx; i++)
                if (vis.CX >= xs[i] && (vis.CX < xs[i + 1] || i == sx - 1)) { mi = i; break; }
            for (int j = 0; j < sy; j++)
                if (vis.CY >= ys[j] && (vis.CY < ys[j + 1] || j == sy - 1)) { mj = j; break; }

            var uv0 = q.UvRect;
            var oldTint = q.Tint;
            int oldQ = q.RenderQueue;
            string baseName = q.gameObject.name;

            // ⚠️ **主格先摆**：子块是挂在**本节点**下的，它的世界位置要当参考原点 —— 主格没摆好之前
            //    算出来的偏移会差「原中心 → 新中心」那一段（会静默地整体错位）。
            var mainCell = new PxRect(xs[mi], ys[mj], xs[mi + 1], ys[mj + 1]);
            PlaceCell(q, mainCell, vis, uv0);
            SetRamp(q, mainCell, clip, softPx);

            for (int i = 0; i < sx; i++)
                for (int j = 0; j < sy; j++)
                {
                    if (i == mi && j == mj) continue;
                    var cell = new PxRect(xs[i], ys[j], xs[i + 1], ys[j + 1]);
                    if (cell.W <= 0.01f || cell.H <= 0.01f) continue;
                    var sub = ImageQuad.Create(q.transform, q.Texture,
                                               LayoutSpace.RectCenter(cell.x1, cell.y1, cell.x2, cell.y2) - q.transform.position,
                                               LayoutSpace.Px(cell.H), new Vector2(0.5f, 0.5f),
                                               baseName + "_soft" + i + j);
                    if (sub == null) continue;
                    PlaceCell(sub, cell, vis, uv0);
                    sub.SetTint(oldTint);
                    sub.SetRenderQueue(oldQ);
                    SetRamp(sub, cell, clip, softPx);
                }
        }

        /// <summary>把一块（`cell`，是 `vis` 的一格）摆到它该在的位置：几何 = 格子、uv = 从 `uv0` 里对应的一小块。</summary>
        static void PlaceCell(ImageQuad q, PxRect cell, PxRect vis, Rect uv0)
        {
            var lp = Local(q.transform.parent, cell.x1, cell.y1, cell.x2, cell.y2);
            lp.z = q.transform.localPosition.z;               // z 不动（同队列里还靠它排序）
            q.transform.localPosition = lp;
            q.SetWorldHeight(LayoutSpace.Px(cell.H));
            q.SetAspect(cell.W / Mathf.Max(1e-6f, cell.H));
            // 本格在 `vis` 里的相对位置 → 映射进 `uv0`（uv 的 y **自下而上**、`PxRect` 自上而下 ⇒ 翻一次，
            // 与 `ClipNineChildren` / `MenuDraw.Rect` 的 uv 那几行同一条规矩）
            float w = Mathf.Max(1e-6f, vis.W), h = Mathf.Max(1e-6f, vis.H);
            q.SetUvRect(new Rect(uv0.x + uv0.width * (cell.x1 - vis.x1) / w,
                                 uv0.y + uv0.height * (vis.y2 - cell.y2) / h,
                                 uv0.width * cell.W / w,
                                 uv0.height * cell.H / h));
        }

        /// <summary>一块的四角 alpha 斜坡：**只乘 alpha**（rgb 不动 —— 乘了 rgb 会在 `Sprites/Default`
        /// 的 `c.rgb *= c.a` 之外再暗一次）。四角都 ≥ 1 且本来就没有顶点色 ⇒ **一个字节都不改**。</summary>
        static void SetRamp(ImageQuad q, PxRect cell, PxRect clip, Vector2 softPx)
        {
            float bl = SoftAlpha(cell.x1, clip.x1, clip.x2, softPx.x) * SoftAlpha(cell.y2, clip.y1, clip.y2, softPx.y);
            float br = SoftAlpha(cell.x2, clip.x1, clip.x2, softPx.x) * SoftAlpha(cell.y2, clip.y1, clip.y2, softPx.y);
            float tr = SoftAlpha(cell.x2, clip.x1, clip.x2, softPx.x) * SoftAlpha(cell.y1, clip.y1, clip.y2, softPx.y);
            float tl = SoftAlpha(cell.x1, clip.x1, clip.x2, softPx.x) * SoftAlpha(cell.y1, clip.y1, clip.y2, softPx.y);
            if (bl >= 0.9999f && br >= 0.9999f && tr >= 0.9999f && tl >= 0.9999f) return;
            var c = q.CornerColors;
            Color b0 = c != null && c.Length == 4 ? c[0] : Color.white;
            Color b1 = c != null && c.Length == 4 ? c[1] : Color.white;
            Color b2 = c != null && c.Length == 4 ? c[2] : Color.white;
            Color b3 = c != null && c.Length == 4 ? c[3] : Color.white;
            q.SetCornerColors(new Color(b0.r, b0.g, b0.b, b0.a * bl),
                              new Color(b1.r, b1.g, b1.b, b1.a * br),
                              new Color(b2.r, b2.g, b2.b, b2.a * tr),
                              new Color(b3.r, b3.g, b3.b, b3.a * tl));
        }

        // ============================================================ 文字的部分越界（原版 `RectMask2D` 对 TMP）
        //
        // 🔴 **原版**：`RectMask2D` 对文字和图片**一视同仁**（掩码在 shader 里按像素裁）。
        //   我们这套是「文字没法像图那样截 uv」——TMP 的 mesh 每个字就是一个四边形，
        //   **可以按同样办法切**：把落在框外的那些顶点**夹到框边上**、并**按比例改它的 uv**
        //   （判据 = 同一个四边形：`uv` 是顶点位置的仿射函数 ⇒ 位置夹多少、uv 就跟多少）。
        //   ⇒ 压在视口边上的那几个字**会被切半个**，与原版同形（此前是整块照画出去）。
        // ⚠️ **必须在最后调**：`SetText` / `SetGlyphHeight` / `SetAutoFitBox` 任何一次重排都会把 mesh 重算回去
        //   （`Label` 内部走 `ForceMeshUpdate`）。各调用点都在「建完 + 定完字号」之后才调它。

        /// <summary>把一段文字的渲染网格**裁到框内**（同时按需叠软边 alpha 斜坡）。
        /// 两条后端都管：TMP（正常那条）与点阵兜底（那是一个贴图四边形 ⇒ 同一套「夹顶点 + 改 uv」）。
        /// 两条都拿不到网格时**出声**（`TextClipUnavailable` 计数 + 一条警告），不静默。
        /// 返回 true = 真的动过（自检拿它断「这条路带电」）。</summary>
        public static bool ClipText(Label lb, PxRect? clip, Vector2 softPx)
        {
            if (lb == null || !clip.HasValue) return false;
            var c = clip.Value;
            if (softPx.x < 0f) softPx.x = 0f;
            if (softPx.y < 0f) softPx.y = 0f;
            var tmp = lb.GetComponentInChildren<TMPro.TextMeshPro>();
            if (tmp != null) return ClipTmpMesh(tmp, c, softPx);
            var mf = lb.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) return ClipQuadMesh(lb.transform, mf.sharedMesh, c, softPx);
            TextClipUnavailable++;
            if (TextClipUnavailable <= 3)
                Debug.LogWarning($"[MenuDraw] 「{lb.name}」没有可裁的渲染网格（TMP 与点阵兜底都没建起来）"
                               + " —— 这一段文字**不会被裁到框内**（原版 `RectMask2D` 会）。");
            return false;
        }

        /// <summary>`ClipText` 拿不到网格的次数（自检可以断它 == 0）。</summary>
        public static int TextClipUnavailable;

        /// <summary>TMP 那条：逐字夹顶点 + 改 uv（+ 软边 alpha）。</summary>
        static bool ClipTmpMesh(TMPro.TextMeshPro tmp, PxRect clip, Vector2 softPx)
        {
            var ti = tmp.textInfo;
            if (ti == null || ti.characterInfo == null || ti.meshInfo == null) return false;
            int n = Mathf.Min(ti.characterCount, ti.characterInfo.Length);
            bool any = false;
            var p = new Vector3[4];
            var uv = new Vector2[4];
            var al = new float[4];
            for (int ci = 0; ci < n; ci++)
            {
                var ch = ti.characterInfo[ci];
                if (!ch.isVisible) continue;
                int mi = ch.materialReferenceIndex;
                if (mi < 0 || mi >= ti.meshInfo.Length) continue;
                var mesh = ti.meshInfo[mi];
                if (mesh.vertices == null || mesh.uvs0 == null) continue;
                int v = ch.vertexIndex;
                if (v < 0 || v + 3 >= mesh.vertices.Length || v + 3 >= mesh.uvs0.Length) continue;
                p[0] = mesh.vertices[v]; p[1] = mesh.vertices[v + 1];
                p[2] = mesh.vertices[v + 2]; p[3] = mesh.vertices[v + 3];
                for (int k = 0; k < 4; k++) { uv[k] = new Vector2(mesh.uvs0[v + k].x, mesh.uvs0[v + k].y); }
                bool moved = ClipQuad(tmp.transform, p, uv, clip, softPx, al);
                int cut = 0;
                for (int k = 0; k < 4; k++) if (al[k] < 0.9999f) cut++;
                if (!moved && cut == 0) continue;                 // 一个字都不用动（含「框内、也不在带里」）
                any = true;
                if (moved)
                    for (int k = 0; k < 4; k++)
                    {
                        mesh.vertices[v + k] = p[k];
                        mesh.uvs0[v + k] = new Vector4(uv[k].x, uv[k].y, mesh.uvs0[v + k].z, mesh.uvs0[v + k].w);
                    }
                // 🔴 **软边对「没被切、但落在渐隐带里」的字**同样要削 alpha（原版掩码是按像素来的，
                //    与「这个字有没有被切」无关）—— 只把 alpha 乘上去，rgb 不动。
                if (cut > 0 && mesh.colors32 != null && v + 3 < mesh.colors32.Length)
                    for (int k = 0; k < 4; k++)
                    {
                        if (al[k] >= 0.9999f) continue;
                        var cc = mesh.colors32[v + k];
                        cc.a = (byte)Mathf.Clamp(Mathf.RoundToInt(cc.a * al[k]), 0, 255);
                        mesh.colors32[v + k] = cc;
                    }
            }
            if (any)
                tmp.UpdateVertexData(TMPro.TMP_VertexDataUpdateFlags.Vertices
                                   | TMPro.TMP_VertexDataUpdateFlags.Uv0
                                   | TMPro.TMP_VertexDataUpdateFlags.Colors32);
            return any;
        }

        /// <summary>点阵兜底那条：`Label` 自己那个四边形（贴图 = 整段字的点阵图）⇒ 同一套夹法。</summary>
        static bool ClipQuadMesh(Transform tr, Mesh mesh, PxRect clip, Vector2 softPx)
        {
            var vs = mesh.vertices;
            var uvs = mesh.uv;
            if (vs == null || uvs == null || vs.Length < 4 || uvs.Length < 4) return false;
            // `Label.RebuildMesh` 的顶点序 = **BL · BR · TR · TL**（0=左下 … 3=左上）、uv = (0,0)(1,0)(1,1)(0,1)
            // ⇒ 重排成 `ClipQuad` 的口径 **BL · TL · TR · BR**（**别照抄顺序**：搞错了「左右」就成了对角平均）
            var p = new[] { vs[0], vs[3], vs[2], vs[1] };
            var uv = new[] { uvs[0], uvs[3], uvs[2], uvs[1] };
            var al = new float[4];
            if (!ClipQuad(tr, p, uv, clip, softPx, al)) return false;
            vs[0] = p[0]; vs[3] = p[1]; vs[2] = p[2]; vs[1] = p[3];
            uvs[0] = uv[0]; uvs[3] = uv[1]; uvs[2] = uv[2]; uvs[1] = uv[3];
            mesh.vertices = vs; mesh.uv = uvs; mesh.RecalculateBounds();
            return true;
        }

        /// <summary>🔴 **四边形夹进裁切框的唯一一份实现**（TMP 与点阵两条后端共用）。
        /// `p` / `uv` 都是 4 个、顺序 **BL · TL · TR · BR**；`alphaOut` 回传四角的软边 alpha。
        /// 返回 false = 四角一个都不用动（调用方**别回写**）。
        /// 夹完之后 uv 按**同一个仿射关系**跟着走（`u = uL + (x−xL)/(xR−xL)·(uR−uL)`，
        /// `v = vT + (y−yT)/(yB−yT)·(vB−vT)` —— 注意 uv 的 v **自下而上**、而 px 的 y 向下 ⇒ 要翻）。</summary>
        static bool ClipQuad(Transform tr, Vector3[] p, Vector2[] uv, PxRect clip, Vector2 softPx, float[] alphaOut)
        {
            var q = new Vector2[4];
            for (int i = 0; i < 4; i++) q[i] = LayoutSpace.ToPixel(tr.TransformPoint(p[i]));
            float xL = (q[0].x + q[1].x) * 0.5f, xR = (q[2].x + q[3].x) * 0.5f;
            float yT = (q[1].y + q[2].y) * 0.5f, yB = (q[0].y + q[3].y) * 0.5f;
            float uL = (uv[0].x + uv[1].x) * 0.5f, uR = (uv[2].x + uv[3].x) * 0.5f;
            float vT = (uv[1].y + uv[2].y) * 0.5f, vB = (uv[0].y + uv[3].y) * 0.5f;
            float dx = xR - xL, dy = yB - yT;
            bool moved = false;
            for (int i = 0; i < 4; i++)
            {
                float x = Mathf.Clamp(q[i].x, clip.x1, clip.x2);
                float y = Mathf.Clamp(q[i].y, clip.y1, clip.y2);
                if (alphaOut != null)
                    alphaOut[i] = SoftAlpha(x, clip.x1, clip.x2, softPx.x) * SoftAlpha(y, clip.y1, clip.y2, softPx.y);
                if (x == q[i].x && y == q[i].y) continue;
                moved = true;
                if (Mathf.Abs(dx) > 1e-6f) uv[i].x = uL + (x - xL) / dx * (uR - uL);
                if (Mathf.Abs(dy) > 1e-6f) uv[i].y = vT + (y - yT) / dy * (vB - vT);
                float zOld = p[i].z;                    // ⚠️ `FromPixel` 给的是 **z=0 的世界平面** ⇒ 回写前把原 z 留回去
                p[i] = tr.InverseTransformPoint(LayoutSpace.FromPixel(x, y));
                p[i].z = zOld;
            }
            return moved;
        }

        /// <summary>按**原版像素矩形**摆一张图。`tex == null` = 纯色块（原版那种「没 sprite、只有 `m_Color`」的件）。</summary>
        /// <param name="keepAspect">原版 `Image.m_PreserveAspect`：按图自身宽高比放进框、**居中**（不拉伸）。</param>
        /// <param name="clip">🔴 **裁切边界**（画布像素 · 左上原点）。非空时越界部分**不画**、且 **uv 跟着截**
        /// —— 这是原版 `RectMask2D` 的等效物（滚动区画内容前给一次）。
        /// ⚠️ **不截 uv 只截矩形的话，那一格图会被压扁**（同 `ImageQuad.SetUvRect` 的注释：
        /// `SetTexture` 会把 `_aspect` 改成贴图自己的）。
        /// 🔴 2026-09-24：这段逻辑原来只在 `MenuWindowBase.Rect` 里，**这里又写一份就是两处同一条规则**
        /// ⇒ 现在**只有这一份**，`MenuWindowBase.Rect` 转调它。</param>
        /// <param name="clipSoftness">🆕 2026-10-04：**原版 `RectMask2D.m_Softness`**（画布像素：x 管左右、y 管上下；
        /// `(0,0)` = 硬边 = 本函数原来那套行为）。非 0 时按剖面**把 quad 沿渐隐带的内沿切开**并给四角上 alpha 斜坡
        /// —— 见 `ApplySoftEdges` 那一大段（判据/代价都在那儿）。⚠️ 只在 `clip` 有值时有意义。</param>
        public static ImageQuad Rect(Transform parent, Texture2D tex, PxRect r, string name, int q,
                                     Color? tint = null, bool keepAspect = false, PxRect? clip = null,
                                     Vector2 clipSoftness = default(Vector2))
        {
            if (tex == null) return null;
            float x1 = r.x1, x2 = r.x2, y1 = r.y1, y2 = r.y2;
            if (keepAspect && tex.height > 0)
            {
                float sprAspect = (float)tex.width / tex.height, rectAspect = (x2 - x1) / Mathf.Max(1e-6f, y2 - y1);
                if (sprAspect > rectAspect) { float nh = (x2 - x1) / sprAspect, d = ((y2 - y1) - nh) * 0.5f; y1 += d; y2 -= d; }
                else { float nw = (y2 - y1) * sprAspect, d = ((x2 - x1) - nw) * 0.5f; x1 += d; x2 -= d; }
            }
            Rect uv = new Rect(0f, 0f, 1f, 1f);
            if (clip.HasValue)
            {
                // 🔴 2026-10-03：**求交那一段收口到 `ClipRect`**（`Hit` / `Nine` / `DeckCell` 共用同一份判据）——
                //    旧写法在这里内联了一遍 `Max/Min`，正是「两处写同一条规则」那一类。行为一字未改。
                float w0 = x2 - x1, h0 = y2 - y1;
                PxRect cr;
                // 整块在视口外（任一轴）⇒ 不建（也就不吃点击）—— 照原版 `RectMask2D` 的语义
                if (!ClipRect(new PxRect(x1, y1, x2, y2), clip, out cr)) return null;
                // uv 的 y 轴是**自下而上**，而 `PxRect` 是自上而下 ⇒ 上下要翻过来
                //（顶点序：0=左下 1=右下 2=右上 3=左上，`ImageQuad.RebuildMesh`；
                //  `MenuScroll.Place` 那一份只做横向，纵向这一份是新的）
                uv = new Rect((cr.x1 - x1) / w0, (y2 - cr.y2) / h0, cr.W / w0, cr.H / h0);
                x1 = cr.x1; x2 = cr.x2; y1 = cr.y1; y2 = cr.y2;
            }
            var quad = ImageQuad.Create(parent, tex, Local(parent, x1, y1, x2, y2), LayoutSpace.Px(y2 - y1),
                                        new Vector2(0.5f, 0.5f), name);
            if (quad == null) return null;
            quad.SetAspect((x2 - x1) / Mathf.Max(1e-6f, y2 - y1));
            quad.SetRenderQueue(q);
            if (uv.x > 0.0005f || uv.width < 0.9995f
                || uv.y > 0.0005f || uv.height < 0.9995f) quad.SetUvRect(uv);
            if (tint.HasValue) quad.SetTint(tint.Value);
            // 🆕 2026-10-04：软边（在 tint 之后 —— 切出来的子块要抄这份 tint；顶点色与材质色互不干扰）
            if (clip.HasValue && (clipSoftness.x > 0f || clipSoftness.y > 0f))
                ApplySoftEdges(quad, new PxRect(x1, y1, x2, y2), clip.Value, clipSoftness);
            return quad;
        }

        /// <summary>原版 `Image.Type = Sliced`：九宫格。`border` 是**贴图像素**的四边（L,B,R,T）。
        /// 🆕 2026-10-03 加 `borderOutPx` —— **画出来的角块长**（不传 = 与 `border` 相同）。
        /// 为什么要有它：原版 `Image` 的 **`m_PixelsPerUnitMultiplier`** 会**缩放画出来的角块**
        /// （例：左栏 `Highlight` = `40k_main_bt_selected BW` 71² · `m_Border 30` · **ppuMul 0.92**
        ///  ⇒ 画出来是 **30 ÷ 0.92 = 32.61px**）。只给一个量的话，UV 切分或角块大小必有一个是错的。
        /// 判据 → `资料/每日…`（B2）与 `ImageQuad.CreateNineSlice` 的同名参数注释。
        /// <para>🆕 **2026-10-03：这条九宫格路也吃 `Clip` 了**（此前**完全没接** —— 滚动区里画九宫格
        /// 会一直画到视口外）。做法与 `Rect` **同一条判据**（`ClipRect`），但**不能只缩目标矩形**：
        /// 九宫格的**角块位置是按整块算的**，把目标矩形先缩小 ⇒ 角块会跑到视口边上（画错）。
        /// ⇒ 照原版 `RectMask2D` 的语义：**整棵树仍按原矩形建**，建完再**逐子块**截（见 `ClipNineChildren`）。
        /// ⚠️ **根节点的位置/矩形不动**（原版 `RectMask2D` 也只裁渲染、不挪 `RectTransform`）——
        ///    自检量「这一层在哪」时读到的是原矩形，不是被截过的那块。</para></summary>
        /// <param name="clip">裁切边界（画布像素 · 左上原点）。整块在框外 ⇒ **不建**（返回 null）。</param>
        /// <param name="clipSoftness">🆕 2026-10-04：软边（原版 `m_Softness`，画布像素）。非 0 时**逐子块**
        /// 再按剖面处理（`ApplySoftEdges`）—— 九块各自都可能落在渐隐带里/跨过带的内沿。同 `Rect`。</param>
        public static GameObject Nine(Transform parent, Texture2D tex, PxRect r, Vector4 border,
                                      float texW, float texH, int q, Color? tint = null, bool fillCenter = true,
                                      string name = "Nine", Vector4? borderOutPx = null, PxRect? clip = null,
                                      Vector2 clipSoftness = default(Vector2))
        {
            if (tex == null) return null;
            PxRect vis;
            if (!ClipRect(r, clip, out vis)) return null;          // 整块在视口外 ⇒ 连节点一起不建
            bool partial = clip.HasValue && !SameRect(vis, r);     // 部分越界 ⇒ 建完要逐子块截
            var go = ImageQuad.CreateNineSlice(parent, tex, border, texW, texH,
                                               Local(parent, r.x1, r.y1, r.x2, r.y2),   // ⚠️ 用**整块**矩形建
                                               LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), name,
                                               borderOutPx ?? border, fillCenter);
            if (go == null) return null;
            foreach (var q2 in go.GetComponentsInChildren<ImageQuad>())
            {
                if (tint.HasValue) q2.SetTint(tint.Value);
                q2.SetRenderQueue(q);
            }
            if (partial) ClipNineChildren(go, clip.Value);
            if (clip.HasValue && (clipSoftness.x > 0f || clipSoftness.y > 0f))
            {
                // 🔴 **逐子块**上软边（顺序在 `ClipNineChildren` **之后** —— 那一趟会挪/缩子块，
                //    先上软边的话切开的位置就作废了）。
                foreach (var q2 in go.GetComponentsInChildren<ImageQuad>(true))
                {
                    if (q2 == null || !q2.gameObject.activeSelf) continue;   // 整块在框外的那几块已被关掉
                    ApplySoftEdges(q2, QuadRectPx(q2), clip.Value, clipSoftness);
                }
            }
            return go;
        }

        /// <summary>**九宫格的部分越界**处理：把每一块子 quad 截到框内（`Nine` 里 `partial` 时才走）。
        /// 整块在框外的子件直接 `SetActive(false)` —— **不删节点**（树形/子块个数与未裁切时一致，
        /// 自检按名字找得到；`PointerLayer` 本来就会跳过未激活的）。
        /// <para>每一块自己的 uv 里对应「裁剩的那段」：子块的 uv 底边 ↔ 它的**下边**（`ImageQuad.RebuildMesh`
        /// 顶点序 0=左下 ⇒ `vMin` 在下），而 `PxRect` 自上而下 ⇒ 纵向要翻一次。</para>
        /// ⚠️ 平铺（`Tiled`）那条同族见 **`ClipTiledChildren`** —— 形状与这里一样，**只有 uv 的换算基准不同**
        /// （平铺按**节距**、不按本块宽高）。改这一份时**顺手看一眼那一份**。
        /// ⚠️ 世界↔像素只走 `LayoutSpace.ToPixel/FromPixel`（别在别处再乘 108）。</summary>
        static void ClipNineChildren(GameObject root, PxRect clip)
        {
            var rootPos = root.transform.position;
            foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
            {
                if (q == null) continue;
                // 子块中心 = 它自己的世界位置（网格以 transform 为中心、锚 (.5,.5)，见 `RebuildMesh`）
                // 🆕 2026-10-04：这一段（世界 → px 反推矩形）收口到 `QuadRectPx`（软边那条路也用它）
                var qr = QuadRectPx(q);
                PxRect cr;
                if (!ClipRect(qr, clip, out cr)) { q.gameObject.SetActive(false); continue; }   // 这一块整块在框外
                if (SameRect(cr, qr)) continue;                                                 // 整块在框内 ⇒ 不动
                var uv = q.UvRect;
                float w = Mathf.Max(1e-6f, qr.W), h = Mathf.Max(1e-6f, qr.H);
                float u1 = uv.x + uv.width * (cr.x1 - qr.x1) / w;
                float u2 = uv.x + uv.width * (cr.x2 - qr.x1) / w;
                float v1 = uv.y + uv.height * (qr.y2 - cr.y2) / h;      // uv 的 y 自下而上 ⇒ 翻
                float v2 = uv.y + uv.height * (qr.y2 - cr.y1) / h;
                var lp = LayoutSpace.FromPixel(cr.CX, cr.CY) - rootPos;
                lp.z = q.transform.localPosition.z;                     // z 不动（同队列里还靠它排序）
                q.transform.localPosition = lp;
                q.SetWorldHeight(LayoutSpace.Px(cr.H));
                q.SetAspect(cr.W / Mathf.Max(1e-6f, cr.H));
                q.SetUvRect(new Rect(u1, v1, u2 - u1, v2 - v1));
            }
        }

        /// <summary>**平铺的部分越界**处理（`Tiled` 里 `partial` 时才走）—— `ClipNineChildren` 的同族：
        /// 形状一模一样（整块在框外的子件 `SetActive(false)`、**不删节点**；部分越界的那块**挪 + 缩 + 截 uv**，
        /// 挪缩那四行也逐字相同）。**要动脑的只有下面那两行 uv**。
        /// <para>🔴 **规则**：平铺的每一块 u 方向是**重复采样**，几何 1:1 对应的**不是一个「块」而是一个「节距」**
        /// ⇒ uv 的增量按**节距**换算（`Δuv = Δpx ÷ 节距`）。
        /// 判据 = 原版 UGUI `Image.GenerateTiledSprite`
        /// （`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Image.cs:1352-1361`）：
        /// 末列那块写的是 `clipped.x = uvMin.x + (uvMax.x − uvMin.x) · (xMax − x1) / (x2 − x1)` ——
        /// **分母 `x2 − x1` 就是整格宽 `tileWidth`**（:1354-1355 的 `x1/x2`）、分子是「离本格左边的距离」
        /// ⇒ 就是「按节距截 uv」。
        /// ⚠️ 更关键的一条：原版 **`RectMask2D` 根本不改这份 mesh**（掩码在 shader 里裁像素）
        /// ⇒ **平铺的相位不因裁切而变**：格子零点仍钉在矩形的左下角，被裁掉的只是像素。
        /// 我们「截几何 + 截 uv」是它的等效物 ⇒ **相位必须照原样留** ——
        /// ⛔ 别把平铺重新对齐到视口边（那会让花纹在视口边上错位），
        /// ⛔ 也别把 uv 拉成整张 `(0,0,1,1)`（那会把被裁剩的一小块花纹**压扁**）。</para>
        /// <para>节距从**子块自己**反推，不去碰调用方那个 `tilePx`：`CreateTiled` 给每一块设的 uv 就是
        /// `(0, 0, 本块世界宽/节距, 本块世界高/节距)`（`ImageQuad.cs:293`）
        /// ⇒ **`节距 = 本块几何宽 ÷ 本块 uv 宽`**，对**末格被截短**的那一块同样成立（它的宽与 uv 宽同比例缩过）。
        /// ⚠️ **别直接拿 `tilePx` 当画布 px 用**：`CreateTiled` 内部按 `ImageQuad.PixelsPerUnit`（**100**）
        /// 折世界尺寸，而画布是 **108** px/世界单位（`LayoutSpace`）⇒ 直接拿它当节距会**差 8%**
        /// （uv 与几何脱钩，花纹在裁切处被缩放 1.08）。用「几何宽 ÷ uv 宽」就不必管这个换算。
        /// 📌 **亲算过（2026-10-03）**：按上面这条定义，本式的 `Δuv` 与 `ClipNineChildren` 那条
        /// `uv.width · Δpx / qr.W` **数值上恒等**（因为 `uv.width ≡ qr.W ÷ 节距`）。
        /// 所以这里**不是**「另一套公式」，而是同一个斜率的另一种写法 —— 写成「÷节距」是为了**直接对着判据读**，
        /// 且对**末格那块**（`qr.W ≠ 节距`）一眼能看明白为什么不能拿本块宽当分母。</para>
        /// ⚠️ 纵轴同 `ClipNineChildren`：uv 的 y **自下而上**、而 `PxRect` 自上而下 ⇒ 翻一次。</summary>
        static void ClipTiledChildren(GameObject root, PxRect clip)
        {
            var rootPos = root.transform.position;
            foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
            {
                if (q == null) continue;
                var qr = QuadRectPx(q);          // 同上：矩形反推只此一份
                PxRect cr;
                if (!ClipRect(qr, clip, out cr)) { q.gameObject.SetActive(false); continue; }   // 这一块整块在框外
                if (SameRect(cr, qr)) continue;                                                 // 整块在框内 ⇒ 不动
                var uv = q.UvRect;
                // 🔴 节距（画布 px）= 本块几何宽 ÷ 本块 uv 宽 —— 见上面第 2 段（末格截短的那块也是这个数）
                float pitchX = qr.W / Mathf.Max(1e-6f, uv.width);
                float pitchY = qr.H / Mathf.Max(1e-6f, uv.height);
                float u1 = uv.x + (cr.x1 - qr.x1) / pitchX;
                float u2 = uv.x + (cr.x2 - qr.x1) / pitchX;
                float v1 = uv.y + (qr.y2 - cr.y2) / pitchY;      // uv 的 y 自下而上 ⇒ 翻
                float v2 = uv.y + (qr.y2 - cr.y1) / pitchY;
                var lp = LayoutSpace.FromPixel(cr.CX, cr.CY) - rootPos;
                lp.z = q.transform.localPosition.z;                     // z 不动（同队列里还靠它排序）
                q.transform.localPosition = lp;
                q.SetWorldHeight(LayoutSpace.Px(cr.H));
                q.SetAspect(cr.W / Mathf.Max(1e-6f, cr.H));
                q.SetUvRect(new Rect(u1, v1, u2 - u1, v2 - v1));
            }
        }

        /// <summary>原版 `Image.Type = Tiled`：按贴图原始尺寸重复铺。
        /// 🆕 **2026-10-03：这条路现在也吃 `Clip` 了** —— ⚠️ **就地订正**：同一天的旧注释写着
        /// 「这条路**仍不吃** `Clip`（本轮只把九宫格那条接上）」，**那是上一批的状态、已过期**（当时只改了注释没接线）。
        /// 接法与 `Nine` **完全同一套**：`ClipRect` 判整块 ⇒ 不建（返回 null）；部分越界 ⇒
        /// **照原矩形建完再逐子块截**（见 `ClipTiledChildren`，那里写着「平铺为什么不能只缩矩形」）。
        /// ⚠️ **5 处调用（`DuelPopupWindow:129` · `ProfileTab:683` · `RankedEventWindow:64` ·
        /// `SearchingMatchPopup:175` · `SettingsWindow:770`）都没传 `clip`** —— 它们全在弹窗 / 页头、
        /// 没有一个在滚动视口内 ⇒ 加了这道能力之后**它们的画面与点击一字未变**（要接就在调用处补一个 `Clip` 实参）。</summary>
        /// <param name="clipSoftness">🆕 2026-10-04：软边（原版 `m_Softness`）。非 0 时**逐格**按剖面处理
        /// （`ApplySoftEdges`）。⚠️ 平铺的**相位不因裁切/软边而变**（见 `ClipTiledChildren` 第 2 段）——
        /// 子块被切开只影响几何与顶点色，格子零点仍钉在矩形左下角。</param>
        public static GameObject Tiled(Transform parent, Texture tex, PxRect r, float tilePx, int q, string name,
                                       Color? tint = null, PxRect? clip = null,
                                       Vector2 clipSoftness = default(Vector2))
        {
            if (tex == null) return null;
            PxRect vis;
            if (!ClipRect(r, clip, out vis)) return null;          // 整块在视口外 ⇒ 连节点一起不建
            bool partial = clip.HasValue && !SameRect(vis, r);     // 部分越界 ⇒ 建完要逐子块截
            var go = ImageQuad.CreateTiled(parent, tex, tilePx, tilePx,
                                           Local(parent, r.x1, r.y1, r.x2, r.y2),   // ⚠️ 用**整块**矩形建
                                           LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), name);
            if (go != null)
            {
                foreach (var q2 in go.GetComponentsInChildren<ImageQuad>())
                {
                    // 🔴 **tint 不能丢**：活动窗的 `Noise` 原版是 col(0.311,0.127,0,0.718)，
                    //    不染就是**全白全不透明**一条 —— 2026-09-24 找茬子代理抓到的。
                    if (tint.HasValue) q2.SetTint(tint.Value);
                    q2.SetRenderQueue(q);
                }
                if (partial) ClipTiledChildren(go, clip.Value);
                if (clip.HasValue && (clipSoftness.x > 0f || clipSoftness.y > 0f))
                    foreach (var q2 in go.GetComponentsInChildren<ImageQuad>(true))
                    {
                        if (q2 == null || !q2.gameObject.activeSelf) continue;
                        ApplySoftEdges(q2, QuadRectPx(q2), clip.Value, clipSoftness);
                    }
            }
            return go;
        }

        /// <summary>按原版 TMP 的 `m_fontSize`（画布像素）摆一段字。
        /// `wrapPx > 0` ⇒ **限宽换行**（原版 `m_TextWrappingMode = 1`）；`autoMinPx > 0` ⇒ 开自适应。
        /// 🔴 **别用 `SetFontSize(px/108)`** —— 那会大 2.7 倍；走 `SetGlyphHeight`。</summary>
        public static Label Text(Transform parent, PxRect r, string text, Color color, string name,
                                 float fontPx, int q, float wrapPx = 0f, float autoMinPx = 0f)
        {
            var lb = Label.Create(parent, text, Local(parent, r.x1, r.y1, r.x2, r.y2), 5, color,
                                  new Vector2(0.5f, 0.5f), name);
            if (lb == null) return null;
            lb.SetRenderQueue(q);
            if (fontPx > 0f) lb.SetGlyphHeight(LayoutSpace.Px(fontPx));
            if (wrapPx > 0f)
            {
                lb.SetWrapWidth(LayoutSpace.Px(wrapPx));
                if (autoMinPx > 0f && fontPx > autoMinPx)
                    lb.SetAutoFitBox(LayoutSpace.Px(wrapPx), LayoutSpace.Px(r.H), autoMinPx, fontPx);
            }
            return lb;
        }

        /// <summary>限宽换行 + 可选自适应字号的一段文字（原版 `m_TextWrappingMode = 1` + autosize）。
        /// 🔴 **别用 `SetFontSize(px/108)`** —— 那会大 2.7 倍；`Text` 走的是 `SetGlyphHeight`。
        /// 🔴 2026-09-24 从 `MainMenuSubmenuWindow.TextBox` 收口过来（那边**转调**，行为一字未改）。</summary>
        public static Label TextBox(Transform parent, PxRect r, string text, Color color, string name,
                                    float fontPx, float autoMinPx = 0f, int q = QText)
        {
            var lb = Text(parent, r, text, color, name, fontPx, q);
            if (lb == null) return null;
            lb.SetWrapWidth(LayoutSpace.Px(r.W));
            if (autoMinPx > 0f && fontPx > autoMinPx)
                lb.SetAutoFitBox(LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), autoMinPx, fontPx);
            return lb;
        }

        /// <summary>文字队列的默认档（原版那批件的文字在同一档里；`MainMenuSubmenuWindow` 用的是 3011）。</summary>
        public const int QText = 3011;

        /// <summary>**左对齐**到 `r` 的左边缘。原版这批 TMP 实测多为 `m_HorizontalAlignment = 1 (Left)`
        /// （`Label` 默认把文字块**居中**放在锚点上，不对齐就会与右对齐的件叠字）。</summary>
        public static void AlignLeft(Label lb, PxRect r)
        {
            if (lb != null) lb.AlignLeftOn(LayoutSpace.FromPixel(r.x1, 0f).x);
        }

        /// <summary>**右对齐**到 `r` 的右边缘。</summary>
        public static void AlignRight(Label lb, PxRect r)
        {
            if (lb != null) lb.AlignRightOn(LayoutSpace.FromPixel(r.x2, 0f).x);
        }

        /// <summary>一个**透明点击区**（整块矩形）+ `WindowButton`，返回那个节点。
        /// 原版这一层就是按钮自己的 `RectTransform`；我们这套没有 uGUI 事件 ⇒ 单独一个透明 quad 当命中区
        /// —— **`PointerLayer` 扫的就是它**（`GetComponentInChildren&lt;ImageQuad&gt;()` 拿矩形）。
        /// 🔴 2026-09-24：这段原来只有 `MainMenuSubmenuWindow.AddHit` 一份，新的活动窗/搜索弹窗也要
        /// ⇒ 收口到这里，那边**转调**（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
        /// 🆕 **2026-10-03（A17）**：加 `target` / `art` / `hoverArt` / `pressedArt` —— 悬停**换图**
        /// （原版 `SpriteSwap` 那 630 颗）。⚠️ **`Hit` 节点上没有常态图**（我们是「画底 + 建透明命中区」
        /// 两步走）⇒ **高亮图必须由调用方把「画底那个 quad」和「常态图名」传进来**；
        /// 不传 = 这一颗维持原样（色偏兜底），**原版是 ColorTint / `trans=0` 的那些就该不传**。
        /// <para>🆕 **2026-10-03：`clip` —— 视口外的命中区不建、压在视口边上的截到视口内**。
        /// 判据 = 原版 `RectMask2D` 的**射线那一面**（`IsRaycastLocationValid`：框外的点判不中任何东西，
        /// 见 `ClipRect` 的注释）。⚠️ 本工程**从前没有这道守卫**（老注释里那句「Clip 生效时视口外的
        /// 点击区不会被建」是**写错了**）⇒ 那时滚出视口的条目**照样能点**（点上还是看不见的那一件）。
        /// 🆕 **2026-10-04：本函数【故意】不收 `clipSoftness`** —— 原版的软边只改**渲染**
        /// （掩码在 shader 里削 alpha），**射线那一面只看矩形**（`IsRaycastLocationValid` 用的是
        /// 未削软的 `rectTransform` + `m_Padding`）⇒ 命中区**不许**跟着缩。别顺手给它加上。</para></summary>
        public static Transform Hit(Transform parent, string name, PxRect r, int q, System.Action onClick,
                                    ImageQuad target = null, string art = null,
                                    string hoverArt = null, string pressedArt = null, PxRect? clip = null)
        {
            PxRect hr;
            // 整块在视口外 ⇒ **连节点一起不建**（返回 null；`AddHit` 的调用方都不接返回值）
            if (!ClipRect(r, clip, out hr)) return null;
            // ⚠️ 命中区那个**节点自己**摆在父原点（`localPosition = 0`）、quad 摆在矩形中心 ——
            //    照抄 `MainMenuSubmenuWindow.AddHit` 原来的写法**一字不改**
            //    （那边的自检有 1000+ 条断言，换个写法就是改行为）。
            //    部分越界时 quad 摆在**截过那块**的中心 ⇒ `PointerLayer` 用它的中心 + 宽高做命中，自动就跟着截了。
            var hit = new GameObject(name).transform;
            hit.SetParent(parent, false);
            var hq = ImageQuad.Create(hit, CardArt.Solid(), Local(hit, hr.x1, hr.y1, hr.x2, hr.y2),
                                      LayoutSpace.Px(hr.H), new Vector2(0.5f, 0.5f), "Hit");
            if (hq != null)
            {
                hq.SetAspect(hr.W / Mathf.Max(1e-6f, hr.H));
                hq.SetTint(new Color(0f, 0f, 0f, 0f));
                hq.SetRenderQueue(q);
            }
            var wb = hit.gameObject.AddComponent<WindowButton>();
            wb.onClick = onClick;
            if (target != null) wb.Bind(target, art, hoverArt, pressedArt);
            return hit;
        }

        /// <summary>🆕 **2026-10-03（A17）**：`Hit` 的「一步到位」版本 —— **画底 + 建命中区**一次做完。
        /// 给「底就是一张图、没有别的装饰」的按钮用（大部分按钮都是这个形状）；
        /// 底上还要压图标/文字的那些仍走 `Rect` + `Hit` 两步。
        /// 返回**画底那个 `ImageQuad`**（调用方要压东西就用它）。
        /// ⚠️ `clip` 生效且整块在视口外时**底与命中区一起不建 ⇒ 返回 null**（调用方判空）。</summary>
        public static ImageQuad Button(Transform parent, string name, string art, PxRect r, int q,
                                       System.Action onClick, Color? tint = null,
                                       string hoverArt = null, string pressedArt = null,
                                       bool keepAspect = false, PxRect? clip = null,
                                       Vector2 clipSoftness = default(Vector2))
        {
            var tex = CardArt.MenuUi(art);
            var qd = Rect(parent, tex, r, name, q, tint, keepAspect, clip, clipSoftness);
            Hit(parent, name + "Hit", r, q, onClick, qd, art, hoverArt, pressedArt, clip);
            return qd;
        }

        // ============================================================ 卡组格（两页共用）
        //
        // 🔴 **2026-09-24 收口**：原版 **`Collection Deck`**（收藏窗 Deck 页）与
        //    **`Collection Deck With Highlight`**（`Deck Selection Popup`）是**同一份 prefab 几何的两个变体**
        //    —— 逐个字段 diff 过，**唯一差别是根组件的 `useSelectedHighlight`（1 / 0）**，其余差异全是子引用 pid。
        //    ⇒ 两处画法**只能有一份**（CLAUDE.md §三）。出处：`资料/普查产出_0923/A2_Deck页.md` §三
        //    与 `A1_外壳与弹窗.md` §3；2026-09-24 亲核：逐项**吻合到 &lt;0.5px**。
        //
        // 作者尺寸 **250×405**，RSR 把它们缩到 **225×364.5**（`_cellWidth/_cellHeight`）⇒ 内部每一件都 **×0.9**。

        /// <summary>格内各件相对格左上的比例（作者 250×405 下的值 ×0.9 已在调用处用 `K`）。</summary>
        public const float DeckCellK = 0.9f;
        /// <summary>原版 `Frame 40K_bt_deck` 在作者系里的矩形（246×368 @ (2,17)）。</summary>
        public const float DcFrameX = 2f, DcFrameY = 17f, DcFrameW = 246f, DcFrameH = 368f;
        /// <summary>卡背图（`Deck Image`，228×306 @ (11,26.1)）。</summary>
        public const float DcBackX = 11f, DcBackY = 26.1f, DcBackW = 228f, DcBackH = 306f;
        /// <summary>`Deck Name`（fs32，@ (20,344.2) 宽 210）。</summary>
        public const float DcNameX = 20f, DcNameY = 344.2f, DcNameW = 210f, DcNameH = 35.8f, DcNamePx = 32f;
        /// <summary>阵营图标 **84.5×85.7** @ **(-10.5, 273.7)**（作者系 250×405 下的值）。
        /// 🔴 **2026-09-26 就地更正**：原来写「贴右上内缩 8」、并拿 `r.x2` 当基准 —— **那是错的**，原版在**左下**：
        /// `资料/说明书/04_界面UI/卡组界面说明书.md:56` 原文「`Faction Icon [-10,274 85x86]` ← **左下阵营图标**」；
        /// `资料/普查产出_0923/A2_Deck页.md:123` 给的是 `-10.5,273.7,84.5,85.7`（`工具/menu_rect.py` 实读同值）。
        /// 这一处**从来没有任何断言盯过** ⇒ 错到现在才被发现。</summary>
        public const float DcFacX = -10.5f, DcFacY = 273.7f, DcFacW = 84.5f, DcFacH = 85.7f;
        /// <summary>难度角标 `DificultyLevel`（**原版拼错了**）**84.93×84.01** @ **(159.19, 15.74)**（作者系，**右上**）。
        /// 出厂 `INACT` —— 由 `DeckCollectionDisplay.displayDifficultyLabel` 运行时打开（**预组页 = 1 · 我的卡组页 = 0**）。</summary>
        public const float DcDiffX = 159.19f, DcDiffY = 15.74f, DcDiffW = 84.93f, DcDiffH = 84.01f;
        /// <summary>模式图标 `Game Mode Icon` **84.5×85.75** @ **(170.5, 273.68)**（作者系，**右下**）。</summary>
        public const float DcModeX = 170.5f, DcModeY = 273.68f, DcModeW = 84.5f, DcModeH = 85.75f;

        /// <summary>难度四档 → 三张图（`CollectionDeck__Config.c:117-136`）：`0/5 → Gallons_1` · `10 → _2` · `15 → _3`，其余给 null（原版也是 null）。</summary>
        public static string DifficultyMarkFile(int difficulty)
        {
            switch (difficulty)
            {
                case 0: case 5: return "Menu_Icon_Gallons_1";
                case 10: return "Menu_Icon_Gallons_2";
                case 15: return "Menu_Icon_Gallons_3";
                default: return null;
            }
        }

        /// <summary>模式 → 图标（原版 `ScriptableObjectsCollectionsUtilities.GetGameModeIcon(playMode)`，
        /// `DeckDrawer__Draw.c` 用 `PrebuiltDeck.gameMode`）。只认我们有的两张；其余给 null ⇒ **整层不建**
        /// （对应原版那句 `enabled = (icon != null)`，`资料/普查产出_0923/A2_Deck页.md:139`）。</summary>
        public static string GameModeIconFile(int gameMode)
        {
            switch (gameMode)
            {
                case 0: return "40k_gamemode_icon_classic";    // `PlayModes.Classic`
                case 13: return "40k_gamemode_icon_skirmish";  // `PlayModes.Skirmish`
                default: return null;
            }
        }
        /// <summary>选中高亮 `Highlight Rounded Square` **289.8×427.3**，往格左上偏 (−1.4, −1.4)。</summary>
        public const float DcHiW = 289.8f, DcHiH = 427.3f, DcHiOff = -1.4f;

        /// <summary>画**一格卡组**（`r` = 已按缩放算好的显示矩形）。
        /// <paramref name="selected"/> = 画金框（原版 `Highlight Rounded Square`，色 (1,.773,0)）。
        /// <para>🆕 **2026-09-26：补上原版 `<Deck>` 下本来就有、我们此前漏画的两层** ——
        /// **难度角标**（`showDifficulty` 为真才画，对应原版 `DeckCollectionDisplay.displayDifficultyLabel`）
        /// 与**模式图标**（`gameMode` 给了才画；图取不到就整层不建 = 原版那句 `enabled = (icon != null)`）。
        /// 两个参数都给 `null` = 这一格没有这两个概念（收藏窗/我的卡组就是这样）——
        /// 原版对「我的卡组」页也是把难度角标的总开关关掉的。</para>
        /// 🔴 同时**修掉阵营图标的位置**（原来画在**右上**、原版在**左下**，见 `DcFacX` 的注释）。
        /// <para>🆕 **2026-10-03**：`clip` 现在**也管点击区** —— 整格在视口外 ⇒ 连 `Hit` 一起不建，
        /// 压在边上的 ⇒ 命中区截到视口内（判据同 `Hit`：原版 `RectMask2D` 的射线那一面）。</para></summary>
        public static Transform DeckCell(Transform parent, string name, PxRect r, CollectionData.DeckInfo info,
                                         bool selected, int q, int qText, int qOverlay, int qHit,
                                         System.Action onClick, PxRect? clip = null,
                                         int? gameMode = null, int? difficulty = null, bool showDifficulty = false)
        {
            const float K = DeckCellK;
            var cell = Node(parent, name, r);

            Rect(cell, CardArt.MenuUi("40K_bt_deck"),
                 new PxRect(r.x1 + DcFrameX * K, r.y1 + DcFrameY * K,
                            r.x1 + (DcFrameX + DcFrameW) * K, r.y1 + (DcFrameY + DcFrameH) * K),
                 "Frame", q, null, false, clip);

            // ✅ **2026-09-24 起这里画的是「玩家选的卡背」**（`PlayerDeck.CardbackId`，
            //    在卡组编辑的 Cosmetics 页里右键选）；**没选过**的卡组退回该阵营的默认卡背 ——
            //    与原版一致（`CardDeck.GetDeckCardback()`：`cardbackId` 空 ⇒ `GetDefaultCardback(army)`）。
            //    原来那句「我们挑的 / 没有数据源」已随第 46 行做完而作废。
            var back = CardArt.DeckCardback(info.CardbackId, info.Faction);
            if (back != null)
                Rect(cell, back,
                     new PxRect(r.x1 + DcBackX * K, r.y1 + DcBackY * K,
                                r.x1 + (DcBackX + DcBackW) * K, r.y1 + (DcBackY + DcBackH) * K),
                     "CardBack", q, null, false, clip);

            // 卡名：⚠️ 文字没法像图那样截 uv ⇒ **按原版 `RectMask2D` 切成半个字**（`ClipText`）
            //（🆕 2026-10-04：此前只做「**整块**在视口外就不建」、压在视口边上的字照画出去 —— 那条缺口已补）
            var nameR = new PxRect(r.x1 + DcNameX * K, r.y1 + DcNameY * K,
                                   r.x1 + (DcNameX + DcNameW) * K, r.y1 + (DcNameY + DcNameH) * K);
            if (Visible(nameR, clip))
            {
                var nl = Text(cell, nameR, info.Name, Color.white, "Deck Name", DcNamePx * K, qText);
                if (clip.HasValue) ClipText(nl, clip, Vector2.zero);   // 「整块在框外」也由 `ClipText` 兜底（切到 0 宽）
            }

            if (!string.IsNullOrEmpty(info.Faction))
                // ⚠️ 走 `CardArt.MenuUi`（三级兜底 `ui_menu/ → ui_deck/ → ui/`）—— 与收藏窗那边原来那条路一致
                // 🔴 位置 = **左下**（作者系 `-10.5, 273.7`），**别再改回右上**（2026-09-26 更正，见 `DcFacX` 注释）
                Rect(cell, CardArt.MenuUi(DeckRuntime.FactionIcon(info.Faction)),
                     new PxRect(r.x1 + DcFacX * K, r.y1 + DcFacY * K,
                                r.x1 + (DcFacX + DcFacW) * K, r.y1 + (DcFacY + DcFacH) * K),
                     "Faction", q, null, true, clip);

            // 🆕 **模式图标**（作者系 `170.5, 273.68`，右下）—— 原版 `enabled = (icon != null)`：
            //    图取不到就**整层不建**（`Rect` 遇 null 直接 return null，天然满足）
            if (gameMode.HasValue)
                Rect(cell, CardArt.MenuUi(GameModeIconFile(gameMode.Value)),
                     new PxRect(r.x1 + DcModeX * K, r.y1 + DcModeY * K,
                                r.x1 + (DcModeX + DcModeW) * K, r.y1 + (DcModeY + DcModeH) * K),
                     "Game Mode Icon", q, null, true, clip);

            // 🆕 **难度角标**（作者系 `159.19, 15.74`，右上）—— 四档三张图（`0/5 一条杠 · 10 两条 · 15 三条`）。
            //    ⚠️ 原版节点名叫 `DificultyLevel`（**拼错了**，照抄别改，断言要按这个名字找）
            if (showDifficulty && difficulty.HasValue)
                Rect(cell, CardArt.MenuUi(DifficultyMarkFile(difficulty.Value)),
                     new PxRect(r.x1 + DcDiffX * K, r.y1 + DcDiffY * K,
                                r.x1 + (DcDiffX + DcDiffW) * K, r.y1 + (DcDiffY + DcDiffH) * K),
                     "DificultyLevel", qOverlay, null, true, clip);

            if (selected)
                Rect(cell, CardArt.MenuUi("Highlight_Rounded_Square"),
                     new PxRect(r.x1 + DcHiOff, r.y1 + DcHiOff, r.x1 + DcHiW * K + DcHiOff, r.y1 + DcHiH * K + DcHiOff),
                     "Highlight Rounded Square", qOverlay, new Color(1f, 0.773f, 0f, 1f));

            // 点击区：**视口外的不建、压在视口边上的截到视口内** —— 判据与 `Hit` 同一条
            //（原版 `RectMask2D` 的**射线那一面**：滚出视口的格子**点不到**，见 `ClipRect` 的注释）。
            // 🔴 **查出来的另一件事（不在本批范围）**：这一颗是 `Node` 建的**裸节点**（不带 quad），
            //    而 `PointerLayer.CollectHits` 取的是「按钮下第一个 `ImageQuad`」（`Shell/PointerLayer.cs:492`）
            //    ⇒ **裸节点进不了命中表** ⇒ 收藏窗/选卡组窗里的卡组格**当前真鼠标点不动**
            //    （`WindowButton.onClick` 在着，自检直调 `wb.Click()` 也过 —— 所以自检看不出来）。
            //    ⚠️ 别顺手在这里补 quad：那会改掉那两扇窗的点击面，得连断言一起做（**已记账**）。
            if (onClick != null)
            {
                PxRect hr;
                if (ClipRect(r, clip, out hr))
                {
                    var hit = Node(cell, "Hit", hr);
                    var wb = hit.gameObject.AddComponent<WindowButton>();
                    wb.onClick = onClick;
                }
            }
            return cell;
        }
    }
}
