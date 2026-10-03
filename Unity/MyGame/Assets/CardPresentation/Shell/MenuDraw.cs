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

        /// <summary>按**原版像素矩形**摆一张图。`tex == null` = 纯色块（原版那种「没 sprite、只有 `m_Color`」的件）。</summary>
        /// <param name="keepAspect">原版 `Image.m_PreserveAspect`：按图自身宽高比放进框、**居中**（不拉伸）。</param>
        /// <param name="clip">🔴 **裁切边界**（画布像素 · 左上原点）。非空时越界部分**不画**、且 **uv 跟着截**
        /// —— 这是原版 `RectMask2D` 的等效物（滚动区画内容前给一次）。
        /// ⚠️ **不截 uv 只截矩形的话，那一格图会被压扁**（同 `ImageQuad.SetUvRect` 的注释：
        /// `SetTexture` 会把 `_aspect` 改成贴图自己的）。
        /// 🔴 2026-09-24：这段逻辑原来只在 `MenuWindowBase.Rect` 里，**这里又写一份就是两处同一条规则**
        /// ⇒ 现在**只有这一份**，`MenuWindowBase.Rect` 转调它。</param>
        public static ImageQuad Rect(Transform parent, Texture2D tex, PxRect r, string name, int q,
                                     Color? tint = null, bool keepAspect = false, PxRect? clip = null)
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
        public static GameObject Nine(Transform parent, Texture2D tex, PxRect r, Vector4 border,
                                      float texW, float texH, int q, Color? tint = null, bool fillCenter = true,
                                      string name = "Nine", Vector4? borderOutPx = null, PxRect? clip = null)
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
            const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;   // 108 px / 世界单位
            var rootPos = root.transform.position;
            foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
            {
                if (q == null) continue;
                // 子块中心 = 它自己的世界位置（网格以 transform 为中心、锚 (.5,.5)，见 `RebuildMesh`）
                Vector2 cpx = LayoutSpace.ToPixel(q.transform.position);
                float hw = q.WorldW * K * 0.5f, hh = q.WorldH * K * 0.5f;
                var qr = new PxRect(cpx.x - hw, cpx.y - hh, cpx.x + hw, cpx.y + hh);
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
            const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;   // 108 px / 世界单位
            var rootPos = root.transform.position;
            foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
            {
                if (q == null) continue;
                Vector2 cpx = LayoutSpace.ToPixel(q.transform.position);
                float hw = q.WorldW * K * 0.5f, hh = q.WorldH * K * 0.5f;
                var qr = new PxRect(cpx.x - hw, cpx.y - hh, cpx.x + hw, cpx.y + hh);
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
        public static GameObject Tiled(Transform parent, Texture tex, PxRect r, float tilePx, int q, string name,
                                       Color? tint = null, PxRect? clip = null)
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
        /// 点击区不会被建」是**写错了**）⇒ 那时滚出视口的条目**照样能点**（点上还是看不见的那一件）。</para></summary>
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
                                       bool keepAspect = false, PxRect? clip = null)
        {
            var tex = CardArt.MenuUi(art);
            var qd = Rect(parent, tex, r, name, q, tint, keepAspect, clip);
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

            // 卡名：⚠️ 文字**没法像图那样截 uv** ⇒ 只做「**整块在视口外就不建**」
            //（这条与 `MenuWindowBase.Text` 的 `Clip` 守卫同一条规矩；部分越界的字仍按原样画，是已知缺口）
            var nameR = new PxRect(r.x1 + DcNameX * K, r.y1 + DcNameY * K,
                                   r.x1 + (DcNameX + DcNameW) * K, r.y1 + (DcNameY + DcNameH) * K);
            if (!clip.HasValue || !(nameR.x2 <= clip.Value.x1 || nameR.x1 >= clip.Value.x2))
                Text(cell, nameR, info.Name, Color.white, "Deck Name", DcNamePx * K, qText);

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
