// B106_W8_Assert.cs — 执行代理 `B106_W8`（2026-10-31 · 第一百零六会话）：
//   **命中区 = 可射线件的【并集】+ 各自 `m_RaycastPadding`**（账/待办行 → `判据/B104_本会话待办与裁定.md:342`
//   「命中区「并集 + `m_RaycastPadding`」⏳ 仍开」；判据正文 → `报告/B103_F2.md` §⑤-1 与 `Shell/MenuDraw.cs` 的
//   `Union(...)` doc：原版可点区 = 子树里每一颗 `m_RaycastTarget == 1` 的 Graphic 各按自己的
//   `m_RaycastPadding` 修正后**取并集**，⛔ 不是 `m_TargetGraphic` 那一处）。
//
// 被测实现 = `Shell/PointerLayer.cs` 的 `UnionOtherQuads`（类尾；唯一调用点 = `HitBoxPx`）。
// 本文件**不重算任何矩形**：期望值要么是**下夹具时我们自己写死的设计 px**，
// 要么是**生产口自己报的**（`PointerLayer.HitBoxForTest` / `ButtonAt`）——
// 重算 = 改坏实现时断言跟着一起错 = 自证（见 `B102HOVER_Assert.cs` 文件头同一条纪律）。
//
// 用法（宿主里加两行；`Run()` 自己建夹具、跑完拆掉，不依赖场景里有什么）：
//
//     B106_W8_Assert.Run();
//     pass += B106_W8_Assert.Pass; fail += B106_W8_Assert.Fail;
//
// ⚠️ 它会**临时把场上其它 `WindowButton` 的 `enabled` 置 false**（`HitQuad` 判 `isActiveAndEnabled`
//    ⇒ 那些钮立刻出局，夹具那一点不会被别的窗抢走），`finally` 里**原样还回去**。
//    ⛔ 别改成 `SetActive(false)`：那会动层级/触发别的宿主的断言。
// ==================================================================
//  🧨 灭自证（改坏法写在每条文案里）
//   · 把 `HitBoxPx` 里那句 `UnionOtherQuads(b, q, ref center, ref half);` 删掉 ⇒ **A1 / A2 红**（A5/A6 仍绿）。
//   · 把 `UnionOtherQuads` 的「跳过 primary」去掉 ⇒ 顶栏那 4 颗（负 pad）看不出来，但语义变错 ——
//     本文件**量不到**那一档（需要 `RegisterHitRect` 的夹具）⇒ 如实记在报告「没覆盖」里。
//   · 把 `QuadBoxPx` 里的 `activeInHierarchy` 判断删掉 ⇒ **A5 红**（停用的件被并进来）。
// ==================================================================
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

public static class B106_W8_Assert
{
    const string P = "[B106W8] ";

    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    // 夹具几何（**设计 px、左上原点、y 向下**；与 `PxRect` 同口径）
    const float Ax1 = 100f, Ay1 = 100f, Ax2 = 300f, Ay2 = 300f;      // 主件（= `HitQuad` 那一颗）
    const float Bx1 = 600f, By1 = 600f, Bx2 = 800f, By2 = 800f;      // 第二颗可射线件（只在并集里）
    const float Sx1 = 1300f, Sy1 = 100f, Sx2 = 1500f, Sy2 = 300f;    // 单件夹具

    public static void Run()
    {
        Pass = 0; Fail = 0;
        var pl = PointerLayer.Ensure();
        if (!Ok(pl != null, "A0 前提：`PointerLayer.Ensure()` 拿得到实例")) return;
        PointerLayer.SetInputEnabled(true);          // 别的宿主可能关过输入层（同 `B102HOVER_Assert`）
        if (!Ok(!PointerLayer.InputBlocked, "A0 前提：输入层没被黑场/转圈件挡着（`InputBlocked == false`）")) return;
        if (!Ok(CardArt.Solid() != null, "A0 前提：`CardArt.Solid()` 取得到（`ImageQuad.Create` 的贴图）")) return;

        // 冻结场上其它钮（它们在别的窗里、可能盖住夹具那几点）
        var all = Object.FindObjectsByType<WindowButton>(FindObjectsSortMode.None);
        var frozen = new List<WindowButton>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || !all[i].enabled) continue;
            all[i].enabled = false; frozen.Add(all[i]);
        }

        var root = new GameObject("B106W8 fixture", typeof(RectTransform));
        try
        {
            // ---- 夹具 1：一颗钮 + 两颗 quad（第二颗**只在并集里**）----
            var node = new GameObject("Btn", typeof(RectTransform));
            node.transform.SetParent(root.transform, false);
            var primary = MakeQuad(node.transform, "Primary", Ax1, Ay1, Ax2, Ay2);
            var second = MakeQuad(node.transform, "Second", Bx1, By1, Bx2, By2);
            if (!Ok(primary != null && second != null, "A0 前提：夹具那两颗 `ImageQuad` 建出来了")) return;
            if (!Ok(primary == node.GetComponentsInChildren<ImageQuad>(true)[0],
                    "A0 前提：主件就是 `HitQuad` 会取到的那一颗（`GetComponentsInChildren[0]` = 自优先）")) return;
            var wb = node.AddComponent<WindowButton>();
            wb.onClick = () => { };

            Vector2 c, half; ImageQuad q0;
            bool got = PointerLayer.HitBoxForTest(wb, out c, out half, out q0);
            if (!Ok(got, "A0 前提：`HitBoxForTest` 量得到这颗钮（不可命中 ⇒ 后面几条全空转）")) return;
            if (!Ok(q0 == primary, "A0 前提：`HitBoxForTest` 报回来的主件 = 第一颗 quad")) return;

            // ---- A1 ★ 并集外接盒 = (100,100)-(800,800) ⇒ 中心 (450,450)、半宽半高 (350,350) ----
            // 🧨 删掉 `HitBoxPx` 里那句 `UnionOtherQuads(...)` ⇒ 这里变成 (200,200)/(100,100) ⇒ 红
            Ok(Near(c.x, 450f) && Near(c.y, 450f) && Near(half.x, 350f) && Near(half.y, 350f),
               "A1★ 命中区 = 两颗可射线件的**外接盒**（中心 (" + c.x.ToString("F2") + "," + c.y.ToString("F2")
             + ")，半宽半高 (" + half.x.ToString("F2") + "," + half.y.ToString("F2") + ")，期望 (450.00,450.00)/(350.00,350.00)）"
             + "｜🧨 删掉 `UnionOtherQuads` 调用 ⇒ 只剩主件那一块 ⇒ 红");

            // ---- A2 ★ 端到端：**只在第二颗里**的那一点必须命中这颗钮（生产命中路 `ButtonAt`）----
            var hitB = pl.ButtonAt(700f, 700f);
            Ok(hitB == wb, "A2★ 只落在第二颗可射线件里的点 (700,700) 命中这颗钮（`ButtonAt` = "
                         + NameOf(hitB) + "）｜🧨 单件模型下这里恒 null = 原版「点了没反应」那个缺陷");

            // ---- A3 主件里那一点照旧命中（并集**没有**把原有行为改坏）----
            var hitA = pl.ButtonAt(150f, 150f);
            Ok(hitA == wb, "A3 主件里的点 (150,150) 照旧命中（`ButtonAt` = " + NameOf(hitA) + "）");

            // ---- A4 外接盒**之外**那一点不命中（并集不是「谁都能吃到」）----
            // ⚠️ 如实记（不是缺陷）：外接盒**之内**的空白（例 (700,150)：在 (100,100)-(800,800) 里、
            //    两颗矩形之外）我们**照样命中** —— 这是「并集**外接盒**」这个近似（`MenuDraw.Union` 的 doc
            //    写的就是外接盒），原版是逐矩形判的**真并集** ⇒ 那一小块是已知的宽出量。本条不拿它当判据。
            var hitOut = pl.ButtonAt(1000f, 1000f);
            Ok(hitOut != wb, "A4 外接盒之外的点 (1000,1000) **不**命中这颗钮（`ButtonAt` = " + NameOf(hitOut)
                            + "；⚠️ 盒内空白（如 (700,150)）会命中 = 外接盒近似的已知宽出量，别当判据）");

            // ---- A5 第二颗**停用** ⇒ 它立刻退出并集（缓存的是节点表、激活态每次现判）----
            // 🧨 把 `QuadBoxPx` 里的 `activeInHierarchy` 判断删掉 ⇒ 这一条红
            second.gameObject.SetActive(false);
            var hitOff = pl.ButtonAt(700f, 700f);
            Ok(hitOff != wb, "A5 第二颗 `SetActive(false)` 之后 (700,700) **不再**命中（`ButtonAt` = " + NameOf(hitOff)
                            + "）｜🧨 不判 `activeInHierarchy` ⇒ 停用的件照样吃指针 ⇒ 红");
            second.gameObject.SetActive(true);
            Ok(pl.ButtonAt(700f, 700f) == wb, "A5b 复原（`SetActive(true)`）之后又命中 —— 缓存**不是**一次性快照");

            // ---- A6 单件钮：命中区**逐位**还是那一块（并集不许把单件也改掉）----
            var node2 = new GameObject("BtnSingle", typeof(RectTransform));
            node2.transform.SetParent(root.transform, false);
            if (!Ok(MakeQuad(node2.transform, "Only", Sx1, Sy1, Sx2, Sy2) != null, "A0 前提：单件夹具建出来了")) return;
            var wb2 = node2.AddComponent<WindowButton>();
            wb2.onClick = () => { };
            Vector2 c2, h2; ImageQuad q2;
            bool got2 = PointerLayer.HitBoxForTest(wb2, out c2, out h2, out q2);
            Ok(got2 && Near(c2.x, 1400f) && Near(c2.y, 200f) && Near(h2.x, 100f) && Near(h2.y, 100f),
               "A6 单件钮的命中区 = 那一块本身（中心 (" + c2.x.ToString("F2") + "," + c2.y.ToString("F2")
             + ")，半宽半高 (" + h2.x.ToString("F2") + "," + h2.y.ToString("F2") + ")，期望 (1400.00,200.00)/(100.00,100.00)）"
             + "｜出厂态绝大多数钮都是这一档 ⇒ 本批对它们是**逐位不变**");

            // ---- A7 灭自证的**结构**那一半：并集盒必须**严格**比主件大（万一 A1 的期望值被人改小也骗不过）----
            Ok(half.x > 100f + 0.01f && half.y > 100f + 0.01f,
               "A7🧨 并集盒严格大于主件盒（半宽半高 = " + half.x.ToString("F2") + "/" + half.y.ToString("F2")
             + "，主件 = 100/100）—— 「加上了第二颗」这件事本身被钉住，⛔ 别只信 A1 那四个数");
        }
        finally
        {
            Object.DestroyImmediate(root);
            for (int i = 0; i < frozen.Count; i++) if (frozen[i] != null) frozen[i].enabled = true;
            PointerLayer.InvalidateHitUnion(null);      // 夹具销毁了 ⇒ 缓存里那两条也清掉
            PointerLayer.SetInputEnabled(true);
        }
    }

    /// <summary>建一颗**透明**命中用 quad（像素框入参；与 `MenuDraw.Hit` 摆 quad 的方式同一套换算）。</summary>
    static ImageQuad MakeQuad(Transform parent, string name, float x1, float y1, float x2, float y2)
    {
        float w = x2 - x1, h = y2 - y1;
        if (w <= 0f || h <= 0f) return null;
        var q = ImageQuad.Create(parent, CardArt.Solid(), LayoutSpace.FromPixel((x1 + x2) * 0.5f, (y1 + y2) * 0.5f),
                                 LayoutSpace.Px(h), new Vector2(0.5f, 0.5f), name);
        if (q == null) return null;
        q.SetAspect(w / h);
        q.SetTint(new Color(0f, 0f, 0f, 0f));
        return q;
    }

    static bool Near(float a, float b) { return Mathf.Abs(a - b) <= 0.05f; }

    static string NameOf(WindowButton b) { return b != null ? "`" + b.name + "`" : "null"; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }
}
