// SelfCheckTextMetrics.cs — 自检断言线的**文字量法唯一实现处**（🆕 2026-10-19 · `A1265`）
//
// 为什么单独一个文件：`ShellLabelRenderedPx` 这一族在 `Editor/` 下有 **5 份逐字相同的副本**
// （`MainMenuScene` / `CollectionScene` / `RewardsScene`（`LabelRenderedWH`）/ `ShopScene`（内联）/
// `ShellScene`），而**「框」那一半**（`ShellScene` 的 `ShellLabelBoxPx`）**全仓只有那一份**
// ⇒ 别的窗要断「哪一侧是紧的」时**没有可用的读数口**（`A1265` 报的就是这个）。
//
// 🔴 判定「哪一侧是紧的」为什么只能拿**框**与**渲出**两边比：
//   TMP 的自适应有**两条**降字号的闸 —— 竖界（`TextMeshProUGUI.cs` 的 `textHeight > marginHeight`）
//   与横界（`textWidth > marginWidth`），而 `marginWidth/marginHeight` **就是填进去的框**
//   （`ComputeMarginSize`）⇒ 收敛字号由**较紧的那一侧**决定。只断「渲出 ≤ 框」两条各自成立时，
//   分不出**是谁在夹**（这正是 `A1215`/`A1265` 那一族的病根）。
//
// ⚠️ **本文件只提供读数，不取任何期望值**：期望值一律由调用点按原版字面量给
//   （⛔ 拿这里量到的数当期望 = 自证）。
// ⚠️ 同前提：TMP 那一格的 `lossyScale` 为 1 且无旋转（旧口读的是**局部** `|textBounds.size|`，
//   本口走 `localToWorldMatrix` ⇒ 只有带缩放那一档会差）。
// ⚠️ ⛔ 别在别的 `Editor/*.cs` 里再长第 6 份 —— 要读框就调 `BoxPx`，要判「哪侧紧」就调 `TightSide`。

using CardPresentation;
using UnityEngine;

/// <summary>🆕 **2026-10-19（`A1265` · 铁律 11）**：自检用的**文字读数口**（渲出那块字 / 写进去的那个框）。
/// <para>🔴 唯一实现处 —— 见文件头。`Editor/ShellScene.cs` 的 `ShellLabelRenderedPx` / `ShellLabelBoxPx`
/// 现在**转调**这里（保留原名，调用点一个字没动）。</para></summary>
public static class SelfCheckTextMetrics
{
    /// <summary>一个 `Label` **渲出来**的像素宽高（画布 px · `Vector2(w, h)`）。
    /// <para>🔴 **走 `TMP.textBounds`**（TMP 自己渲出来那块），⛔ **不是** `Label.WorldW/H`
    /// （那是**字段缓存** `_tmpW/_tmpH`，只有 `RefreshBounds()` 写 —— 被测实现自己；
    /// `SetFontSize` / `SetCharSpacing` 这一族只重排 mesh、不刷缓存 ⇒ 旧口会报旧值）。</para>
    /// <para>⚠️ 量不到（`lb == null` / 点阵后端 / 底下没有 TMP）⇒ **退回旧口** `Label.WorldW/H`
    /// （点阵那条路本来就没有 `textBounds`）—— 与各宿主原来的行为逐字一致，⛔ 不是「静默吞掉新口」。</para>
    /// <para>⚠️ **它只给宽高**：调用方拿它算边缘时，**中心仍是节点位置**（`AlignLeft/Right` 把节点挪走那一档
    /// 一个字没动）。</para></summary>
    public static Vector2 RenderedPx(Label lb)
    {
        if (lb == null) return Vector2.zero;
        var tmp = lb.GetComponentInChildren<TMPro.TextMeshPro>(true);
        if (tmp == null) return new Vector2(lb.WorldW * 108f, lb.WorldH * 108f);
        var b = tmp.textBounds;                       // 局部空间的行盒（`Bounds`）
        var M = tmp.transform.localToWorldMatrix;
        float x1 = float.MaxValue, y1 = float.MaxValue, x2 = float.MinValue, y2 = float.MinValue;
        for (int c = 0; c < 4; c++)
        {
            var corner = M.MultiplyPoint3x4(new Vector3((c % 2 == 0) ? b.min.x : b.max.x,
                                                        (c < 2) ? b.min.y : b.max.y, 0f));
            float px = LayoutSpace.PxX(corner.x), py = LayoutSpace.PxY(corner.y);
            x1 = Mathf.Min(x1, px); x2 = Mathf.Max(x2, px);
            y1 = Mathf.Min(y1, py); y2 = Mathf.Max(y2, py);
        }
        return new Vector2(x2 - x1, y2 - y1);
    }

    /// <summary>一颗 `Label` **写进去的那个框**（TMP 的 `rectTransform.rect`）换到**画布 px**。
    /// <para>🔴 **换算与 <see cref="RenderedPx"/> 逐字同一条**（`localToWorldMatrix` + `LayoutSpace.PxX/PxY`）
    /// ⇒ 两个读数**同源可比** —— 这正是「哪一侧是紧的」那条关系式需要的（拿它们去减出一个**框的余量**）。</para>
    /// <para>⚠️ 点阵后端（那棵树里没有 TMP）⇒ 返回 `Vector2.zero`：调用方**必须先 gate 掉**
    /// （⛔ 别让 0 混进减法里 —— 那会变成「框宽 0 ⇒ 渲出宽超框」的**假红**）。</para></summary>
    public static Vector2 BoxPx(Label lb)
    {
        if (lb == null) return Vector2.zero;
        var tmp = lb.GetComponentInChildren<TMPro.TextMeshPro>(true);
        if (tmp == null) return Vector2.zero;
        var r = tmp.rectTransform.rect;               // 局部空间的框（锚点不拉伸时 = `sizeDelta`）
        var M = tmp.transform.localToWorldMatrix;
        float x1 = float.MaxValue, y1 = float.MaxValue, x2 = float.MinValue, y2 = float.MinValue;
        for (int c = 0; c < 4; c++)
        {
            var corner = M.MultiplyPoint3x4(new Vector3((c % 2 == 0) ? r.min.x : r.max.x,
                                                        (c < 2) ? r.min.y : r.max.y, 0f));
            float px = LayoutSpace.PxX(corner.x), py = LayoutSpace.PxY(corner.y);
            x1 = Mathf.Min(x1, px); x2 = Mathf.Max(x2, px);
            y1 = Mathf.Min(y1, py); y2 = Mathf.Max(y2, py);
        }
        return new Vector2(x2 - x1, y2 - y1);
    }

    /// <summary>「**哪一侧是紧的**」—— 框减去渲出那块字之后的**剩余余量**，哪一轴更小就是谁在夹字号。
    /// <para>返回 `false` = 量不到（`lb == null` / 点阵后端 / 框是 0）⇒ 调用方**必须报红或显式跳过**，
    /// ⛔ 不许把 0 当余量（那会让「没量到」静默变成「这一侧紧」）。</para>
    /// <para>典型用法：`SelfCheckTextMetrics.TightSide(lb, out var wMargin, out var hMargin)`
    /// ⇒ `wMargin &lt; hMargin` 说明宽度在夹，反之高度在夹（容差由调用点给 —— 两侧接近时**别判**）。</para></summary>
    public static bool TightSide(Label lb, out float widthMarginPx, out float heightMarginPx)
    {
        widthMarginPx = heightMarginPx = float.NaN;
        if (lb == null) return false;
        if (lb.GetComponentInChildren<TMPro.TextMeshPro>(true) == null) return false;   // 点阵后端：没有「框」这回事
        var box = BoxPx(lb);
        if (box.x <= 0f || box.y <= 0f) return false;
        var ren = RenderedPx(lb);
        if (ren.x < 0f || ren.y < 0f) return false;
        widthMarginPx = box.x - ren.x;
        heightMarginPx = box.y - ren.y;
        return true;
    }
}
