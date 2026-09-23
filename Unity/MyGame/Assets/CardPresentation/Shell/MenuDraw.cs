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

        /// <summary>空节点（**有矩形语义** —— 原版每个节点都有自己的 rect，位置要摆对，
        /// 否则自检量不到、将来做点击/滚动也会算错）。</summary>
        public static Transform Node(Transform parent, string name, PxRect r)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = Local(parent, r.x1, r.y1, r.x2, r.y2);
            return t;
        }

        /// <summary>按**原版像素矩形**摆一张图。`tex == null` = 纯色块（原版那种「没 sprite、只有 `m_Color`」的件）。</summary>
        /// <param name="keepAspect">原版 `Image.m_PreserveAspect`：按图自身宽高比放进框、**居中**（不拉伸）。</param>
        public static ImageQuad Rect(Transform parent, Texture2D tex, PxRect r, string name, int q,
                                     Color? tint = null, bool keepAspect = false)
        {
            if (tex == null) return null;
            float x1 = r.x1, x2 = r.x2, y1 = r.y1, y2 = r.y2;
            if (keepAspect && tex.height > 0)
            {
                float sprAspect = (float)tex.width / tex.height, rectAspect = (x2 - x1) / Mathf.Max(1e-6f, y2 - y1);
                if (sprAspect > rectAspect) { float nh = (x2 - x1) / sprAspect, d = ((y2 - y1) - nh) * 0.5f; y1 += d; y2 -= d; }
                else { float nw = (y2 - y1) * sprAspect, d = ((x2 - x1) - nw) * 0.5f; x1 += d; x2 -= d; }
            }
            var quad = ImageQuad.Create(parent, tex, Local(parent, x1, y1, x2, y2), LayoutSpace.Px(y2 - y1),
                                        new Vector2(0.5f, 0.5f), name);
            if (quad == null) return null;
            quad.SetAspect((x2 - x1) / Mathf.Max(1e-6f, y2 - y1));
            quad.SetRenderQueue(q);
            if (tint.HasValue) quad.SetTint(tint.Value);
            return quad;
        }

        /// <summary>原版 `Image.Type = Sliced`：九宫格。`border` 是**贴图像素**的四边（L,B,R,T）。</summary>
        public static GameObject Nine(Transform parent, Texture2D tex, PxRect r, Vector4 border,
                                      float texW, float texH, int q, Color? tint = null, bool fillCenter = true)
        {
            if (tex == null) return null;
            var go = ImageQuad.CreateNineSlice(parent, tex, border, texW, texH,
                                               Local(parent, r.x1, r.y1, r.x2, r.y2),
                                               LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), "Nine",
                                               border, fillCenter);
            if (go == null) return null;
            foreach (var q2 in go.GetComponentsInChildren<ImageQuad>())
            {
                if (tint.HasValue) q2.SetTint(tint.Value);
                q2.SetRenderQueue(q);
            }
            return go;
        }

        /// <summary>原版 `Image.Type = Tiled`：按贴图原始尺寸重复铺。</summary>
        public static GameObject Tiled(Transform parent, Texture tex, PxRect r, float tilePx, int q, string name)
        {
            if (tex == null) return null;
            var go = ImageQuad.CreateTiled(parent, tex, tilePx, tilePx,
                                           Local(parent, r.x1, r.y1, r.x2, r.y2),
                                           LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), name);
            if (go != null)
                foreach (var q2 in go.GetComponentsInChildren<ImageQuad>()) q2.SetRenderQueue(q);
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
    }
}
