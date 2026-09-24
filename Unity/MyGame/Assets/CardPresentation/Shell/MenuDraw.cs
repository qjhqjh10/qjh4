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
                var c = clip.Value;
                float w0 = x2 - x1;
                float cx1 = Mathf.Max(x1, c.x1), cx2 = Mathf.Min(x2, c.x2);
                // 整块在视口外 ⇒ 不建（也就不吃点击）
                if (w0 <= 0.01f || cx2 <= cx1 + 0.01f) return null;
                uv = new Rect((cx1 - x1) / w0, 0f, (cx2 - cx1) / w0, 1f);
                x1 = cx1; x2 = cx2;
            }
            var quad = ImageQuad.Create(parent, tex, Local(parent, x1, y1, x2, y2), LayoutSpace.Px(y2 - y1),
                                        new Vector2(0.5f, 0.5f), name);
            if (quad == null) return null;
            quad.SetAspect((x2 - x1) / Mathf.Max(1e-6f, y2 - y1));
            quad.SetRenderQueue(q);
            if (uv.x > 0.0005f || uv.width < 0.9995f) quad.SetUvRect(uv);
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
        /// <summary>阵营图标 **84.5×85.7**（作者系，贴右上内缩 8）。</summary>
        public const float DcFacW = 84.5f, DcFacH = 85.7f, DcFacIn = 8f;
        /// <summary>选中高亮 `Highlight Rounded Square` **289.8×427.3**，往格左上偏 (−1.4, −1.4)。</summary>
        public const float DcHiW = 289.8f, DcHiH = 427.3f, DcHiOff = -1.4f;

        /// <summary>画**一格卡组**（`r` = 已按缩放算好的显示矩形）。
        /// <paramref name="selected"/> = 画金框（原版 `Highlight Rounded Square`，色 (1,.773,0)）。</summary>
        public static Transform DeckCell(Transform parent, string name, PxRect r, CollectionData.DeckInfo info,
                                         bool selected, int q, int qText, int qOverlay, int qHit,
                                         System.Action onClick, PxRect? clip = null)
        {
            const float K = DeckCellK;
            var cell = Node(parent, name, r);

            Rect(cell, CardArt.MenuUi("40K_bt_deck"),
                 new PxRect(r.x1 + DcFrameX * K, r.y1 + DcFrameY * K,
                            r.x1 + (DcFrameX + DcFrameW) * K, r.y1 + (DcFrameY + DcFrameH) * K),
                 "Frame", q, null, false, clip);

            // ⚠️ **我们挑的**：原版这里放**玩家选的卡背**（`CollectionManager` 的 cosmetic）。
            //    我们还没有「玩家选哪张卡背」的数据源/入口 ⇒ 用该阵营的**默认卡背**顶着（出声）。
            var back = CardArt.CardBack(info.Faction);
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
                Rect(cell, CardArt.MenuUi(DeckRuntime.FactionIcon(info.Faction)),
                     new PxRect(r.x2 - (DcFacW + DcFacIn) * K, r.y1 + DcFacIn * K,
                                r.x2 - DcFacIn * K, r.y1 + (DcFacIn + DcFacH) * K),
                     "Faction", q, null, true, clip);

            if (selected)
                Rect(cell, CardArt.MenuUi("Highlight_Rounded_Square"),
                     new PxRect(r.x1 + DcHiOff, r.y1 + DcHiOff, r.x1 + DcHiW * K + DcHiOff, r.y1 + DcHiH * K + DcHiOff),
                     "Highlight Rounded Square", qOverlay, new Color(1f, 0.773f, 0f, 1f));

            if (onClick != null)
            {
                var hit = Node(cell, "Hit", r);
                var wb = hit.gameObject.AddComponent<WindowButton>();
                wb.onClick = onClick;
            }
            return cell;
        }
    }
}
