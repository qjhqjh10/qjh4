// ShellScene.cs — 「游戏外壳」场景（`Shell.unity`）的**建 / 自检 / 存盘**入口
//
// 用法：
//   … -executeMethod ShellScene.Run              自检（结构 + 截图），退出码 0 = 全过
//   … -executeMethod ShellScene.BuildAndSaveScene 建出场景存盘（给人打开按 Play 用）
//
// 施工图：`资料/阶段二_Shell_原版规格.md`（§一 常驻件表 · §二 开场动画链 · §三 开窗口链 · §六 落地清单）。
// 🔴 每条断言后面都写了**它盯的是哪个原版值 + 出处** —— 不许拿我们自己写的常量断言我们自己写的常量（正本 §10·3 第 3 层）。
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ShellScene
{
    const string P = "[Shell] ";
    const string ScenePath = "Assets/CardPresentation/Scenes/Shell.unity";
    const string ShotDir = "d:/4/_tmp_view/shell";

    static int _pass, _fail;
    static readonly List<string> _failures = new List<string>();

    static void Section(string t) { Debug.Log(P + $"--- {t} ---"); }

    static void Check<T>(T got, T want, string msg)
    {
        if (EqualityComparer<T>.Default.Equals(got, want)) { _pass++; Debug.Log(P + $"   ✓ {msg}"); }
        else
        {
            _fail++;
            var line = $"{msg} —— 期望 [{want}]，实得 [{got}]";
            _failures.Add(line);
            Debug.LogError(P + $"   ✗ {line}");
        }
    }

    static void CheckTrue(bool c, string msg) { Check(c, true, msg); }

    /// <summary>🆕 **2026-10-12（A416）**：点弹窗上那颗钮（**走生产那条路**：`WindowButton.ClickForTest()`
    /// → `Click()` → `onClick`，= `PointerLayer` 派发时会调的那一个）。
    /// 🔴 **必须按名字取**：`GetComponentInChildren&lt;WindowButton&gt;()` 在 `PopUpGameWindow` 上会**先撞上
    /// 压暗层那颗吸收层**（`MenuDraw.Absorb` 建的、`absorbOnly = true`）⇒ `Click()` 头一句就早退，
    /// 看着「点了」其实什么都没发生（回调不触发 + 窗关不掉）。找不到 ⇒ 打一条红（⛔ 不静默）。
    /// 节点名照原版 prefab：2 按钮版 = `ButtonLeft` / `ButtonRight` · 1 按钮版 = `Generic UI Button`。</summary>
    static void ClickPopUpButton(GameWindow popup, string btnName)
    {
        var node = popup != null ? FindChildIn(popup.transform, btnName) : null;
        var wb = node != null ? node.GetComponentInChildren<WindowButton>(true) : null;
        CheckTrue(wb != null, $"（点钮）`{btnName}` 上挂着 `WindowButton`（拿不到 ⇒ 这扇窗的钮点不动）");
        if (wb != null) wb.ClickForTest();
    }

    /// <summary>🆕 A17：把一棵树里**接了悬停换图**的按钮逐个悬停一遍 —— 没换图、或离开没还原，都要红。
    /// ⚠️ 批处理没有帧循环 ⇒ `WindowButton.AuditHoverSwap` 直调 `Enter/Exit`（就是指针层调的那两个）。</summary>
    static void CheckHoverSwap(Transform root, string what)
    {
        int n; string bad = WindowButton.AuditHoverSwap(root, out n);
        CheckTrue(n > 0, what + "：**确实有**接了悬停换图的按钮（n=" + n + "，否则这条等于没查）");
        if (bad.Length > 0) CheckTrue(false, what + "：换图要「悬停换得动 + 离开还原得回」—— " + bad);
    }

    static void CheckNoMissingSwapArt(string what)
        => CheckTrue(WindowButton.MissingSwapArt.Count == 0,
                     what + "：**悬停图一张都不缺**（缺的会列在这里：" + string.Join("、", WindowButton.MissingSwapArt.ToArray()) + "）");
    static void CheckNear(float got, float want, float tol, string msg)
        => CheckTrue(Mathf.Abs(got - want) <= tol, $"{msg}（{got:F3} ≈ {want:F3}±{tol:F3}）");

    /// <summary>🆕 **2026-10-13（A497）**：比**一个经「px → 设计世界 → px」往返反推回来的** px 值时用的谓词
    /// —— 逐字段容差 **0.05px**。
    ///
    /// <para>🔴 **为什么不能直接用 `MenuDraw.SameRect`**：它是**四个字段逐位 `==`**（`Shell/MenuDraw.cs:241-242`），
    /// 而 `ViewportClip.ClipPx`（`Shell/ViewportClip.cs:169-191`）与 `MenuDraw.QuadRectPx` 这类值**不是存下来的**
    /// —— 它们是「`ApplyPxRect` 写进去的那个 rect」经 `PosInDesignSpace` + `LayoutSpace.ToPixel` **反推**回来的
    /// ⇒ 写进去与读回来是两条路，中间隔着一次 float32 往返（残差 ~1e-4 px 量级，A497 实测 **−6.1e-5 px**）
    /// ⇒ 逐位比**恒不等**（假阴）。0.05px 对画面无意义，但足以把往返误差挡在外面
    /// （与 `MenuDraw.SameRectNear` 同一口径；判据 → `资料/普查产出_1012/D8_ShellScene五红诊断.md` §1.2）。</para>
    ///
    /// <para>⚠️ **`MenuDraw.SameRectNear` 是私有**（`MenuDraw.cs:249`，无访问修饰符）且它服务的
    /// `SameRect` 有 **7 处生产调用点**（语义 =「一个字都不动」那种精确判据）⇒ 本件**就地**复制这一份口径，
    /// ⛔ 不去动 `MenuDraw` 那一份（改宽 = 静默改生产行为）。</para>
    ///
    /// <para>⛔ **别拿它去比「本该逐位相等」的量**（存下来的矩形 / 字面量 vs 纯算式）——
    /// 那会把真偏差一起放过去。判据只有一条：**这个值有没有进过世界往返**。</para></summary>
    static bool NearPx(PxRect a, PxRect b, float eps = 0.05f)
        => Mathf.Abs(a.x1 - b.x1) <= eps && Mathf.Abs(a.y1 - b.y1) <= eps
        && Mathf.Abs(a.x2 - b.x2) <= eps && Mathf.Abs(a.y2 - b.y2) <= eps;

    /// <summary>同上，逐分量版（拿**字面量**当期望值时用，⛔ 期望值照旧写字面量、不从被测实现读）。</summary>
    static bool NearPx(float got, float want, float eps = 0.05f) => Mathf.Abs(got - want) <= eps;

    /// <summary>🆕 **2026-10-07（A12①）求交探针**：同一个矩形，在**两条求交入口**上必须逐条同答 ——
    /// ① 纵向滚动区的 `MenuScroll.Intersects` ② 横向滚动区的 `MenuScroll.Intersects`
    /// ③ `MenuDraw.Visible`（唯一那一份求交）④ `MenuDraw.ClipRect`（它的可见性那一问）。
    /// `want` = **写死的期望值**，由判据（原版 `RectMask2D` **四边都裁**：渲染走 `IClipper`、
    /// 射线走 `IsRaycastLocationValid`，UGUI 源码 `Runtime/UGUI/UI/Core/RectMask2D.cs:178-185`）
    /// 直接读出来 —— ⛔ **不许**从被测实现算（那就成了「拿我们的实现证明我们的实现」）。</summary>
    static void CheckIntersect(MenuScroll vs, MenuScroll hs, PxRect vp, PxRect r, bool want, string what)
    {
        Check(vs.Intersects(r), want, $"`Intersects`（纵向滚动区）：{what}");
        Check(hs.Intersects(r), want, $"`Intersects`（横向滚动区）：{what}");
        Check(MenuDraw.Visible(r, vp), want, $"`MenuDraw.Visible`（唯一那一份）：{what}");
        Check(MenuDraw.ClipRect(r, vp, out _), want, $"`ClipRect` 的可见性判据与之一致：{what}");
    }

    /// <summary>一棵软边树里有几块的四角在指定色上（含主格；子块与主格同色才算跟上了）。</summary>
    static int PiecesWithTint(GameObject root, Color want, float tol)
    {
        int n = 0;
        foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
        {
            var c = q.Tint;
            if (Mathf.Abs(c.r - want.r) <= tol && Mathf.Abs(c.g - want.g) <= tol
                && Mathf.Abs(c.b - want.b) <= tol && Mathf.Abs(c.a - want.a) <= tol) n++;
        }
        return n;
    }

    /// <summary>一棵软边树里有几块的矩形**越出**了裁切框（世界 → 画布 px 走 `LayoutSpace.ToPixel`，别再乘 108）。</summary>
    static int PiecesOutsideClip(GameObject root, PxRect clip, float tolPx)
    {
        const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;
        int n = 0;
        foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
        {
            if (q == null || !q.gameObject.activeInHierarchy) continue;
            var c = LayoutSpace.ToPixel(q.transform.position);
            float hw = q.WorldW * K * 0.5f, hh = q.WorldH * K * 0.5f;
            if (c.x - hw < clip.x1 - tolPx || c.x + hw > clip.x2 + tolPx
                || c.y - hh < clip.y1 - tolPx || c.y + hh > clip.y2 + tolPx) n++;
        }
        return n;
    }

    /// <summary>🆕 **2026-10-07（A140②）**：一个 `ImageQuad` **渲出来**的像素矩形
    /// （世界 → 画布 px 走 `LayoutSpace.ToPixel`，⛔ 别再乘 108 —— 同上一条）。
    /// 形状与 `RewardsScene.QuadRectOf` 一致（那边写的是 `×108 + 960` 的直式，两者同一口径）。
    /// ⚠️ 只量**这一颗**（不往子树钻）：软边切出来的子块要逐块量。
    /// ⚠️ 它量的是**建完那一刻**的几何 ⇒ 量软边宿主时必须拿「所有块的并集」（`PiecesOutsideClip` 那种扫法）。</summary>
    static bool QuadPxRect(ImageQuad q, out float x1, out float y1, out float x2, out float y2)
    {
        const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;
        x1 = y1 = x2 = y2 = 0f;
        if (q == null) return false;
        Vector2 c = LayoutSpace.ToPixel(q.transform.position);
        float hw = q.WorldW * K * 0.5f, hh = q.WorldH * K * 0.5f;
        x1 = c.x - hw; x2 = c.x + hw;
        y1 = c.y - hh; y2 = c.y + hh;
        return true;
    }

    /// <summary>🆕 A233 探针用：一段文字**渲出来**的宽（画布 px）。
    /// 🔴 量的是 **TMP 自己那份渲染网格的顶点跨度**（`textInfo.meshInfo[].vertices` —— 就是
    /// `MenuDraw.ClipTmpMesh` 写、`UpdateVertexData` 推给渲染的那一份；量法与 `MainMenuScene` 的
    /// `CountSoftFadedTextVerts` 同源），⛔ **不是** `Label.WorldW`（那是标签自己的框 ——
    /// 字号对而溢出时它照样「看着对」，见 `AutoFitBox` 那条教训）。
    /// 世界 → 画布 px 走 `LayoutSpace.ToPixel`（别再乘 108）。取不到网格 ⇒ 返回 **−1**
    /// （调用方必须先断它 > 0，否则「≤ 框宽」会被 −1 蒙过去）。</summary>
    static float TextMeshWidthPx(Label lb)
    {
        var tmp = lb != null ? lb.GetComponentInChildren<TMPro.TextMeshPro>() : null;
        if (tmp == null) return -1f;
        var ti = tmp.textInfo;
        if (ti == null || ti.characterInfo == null || ti.meshInfo == null) return -1f;
        float min = float.MaxValue, max = float.MinValue;
        int n = Mathf.Min(ti.characterCount, ti.characterInfo.Length);
        for (int ci = 0; ci < n; ci++)
        {
            var ch = ti.characterInfo[ci];
            if (!ch.isVisible) continue;
            int mi = ch.materialReferenceIndex;
            if (mi < 0 || mi >= ti.meshInfo.Length) continue;
            var vm = ti.meshInfo[mi].vertices;
            if (vm == null) continue;
            int v = ch.vertexIndex;
            if (v < 0 || v + 3 >= vm.Length) continue;
            for (int k = 0; k < 4; k++)
            {
                float x = LayoutSpace.ToPixel(tmp.transform.TransformPoint(vm[v + k])).x;
                if (x < min) min = x;
                if (x > max) max = x;
            }
        }
        return max > min ? max - min : -1f;
    }

    /// <summary>🆕 **2026-10-13（A464 · B2/B3 探针）**：一段 TMP 文字**渲出来的**顶点范围
    /// （画布 px、左上原点）。与 <see cref="TextMeshWidthPx"/> **逐字同一套量法**
    /// （同一份 `characterInfo` 过滤 + 同一个 `LayoutSpace.ToPixel(TransformPoint(v))` 换算），
    /// 只是**四条边都给**：B2/B3 要的是「顶点有没有被夹到某个 x 上」，只给宽度分不出来。
    /// 🔴 读的仍是 `textInfo.meshInfo[mi].vertices`（= `MenuDraw.ClipTmpMesh` 写、`UpdateVertexData`
    /// 推给渲染的那一份数组）—— ⛔ 不是 `Label.WorldW`（那玩意儿**不随裁切变**，见 `TmpVertPx` 的注释）。
    /// ⚠️ 只认 `isVisible` 的字（TMP 给不可见的字写四角全 0、却照占 4 个槽 ⇒ 扫全数组会假红）。
    /// 返回 `false` = 取不到网格（⛔ 调用方别把 false 当成「范围是 0」）。</summary>
    static bool TmpSpanPx(Label lb, out float minX, out float minY, out float maxX, out float maxY)
    {
        minX = minY = float.MaxValue; maxX = maxY = float.MinValue;
        var tmp = lb != null ? lb.GetComponentInChildren<TMPro.TextMeshPro>() : null;
        if (tmp == null) return false;
        var ti = tmp.textInfo;
        if (ti == null || ti.characterInfo == null || ti.meshInfo == null) return false;
        bool any = false;
        int n = Mathf.Min(ti.characterCount, ti.characterInfo.Length);
        for (int ci = 0; ci < n; ci++)
        {
            var ch = ti.characterInfo[ci];
            if (!ch.isVisible) continue;
            int mi = ch.materialReferenceIndex;
            if (mi < 0 || mi >= ti.meshInfo.Length) continue;
            var vm = ti.meshInfo[mi].vertices;
            if (vm == null) continue;
            int v = ch.vertexIndex;
            if (v < 0 || v + 3 >= vm.Length) continue;
            for (int k = 0; k < 4; k++)
            {
                var p = LayoutSpace.ToPixel(tmp.transform.TransformPoint(vm[v + k]));
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
                any = true;
            }
        }
        return any;
    }

    /// <summary>在子树里按名字找节点（**含 inactive** —— 自检里很多件是关着的）。</summary>
    static Transform FindChildIn(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    /// <summary>🆕 2026-10-07（A77⑮/⑧a）：一颗命中区节点的**渲染队列档**（`ImageQuad.RenderQueue`）。
    /// 自检拿它核「哪一层压哪一层」——分层用的是**渲染队列、不是 z**（`CLAUDE.md` §三）。
    /// 取不到 ⇒ 返回 `int.MinValue`（那样任何「＞某档」的断言都会红，⛔ 不会静默当成通过）。</summary>
    static int QueueOf(Transform node)
    {
        var q = node != null ? node.GetComponentInChildren<ImageQuad>(true) : null;
        return q != null ? q.RenderQueue : int.MinValue;
    }

    /// <summary>🆕 2026-10-07（A77⑮/⑧a）：一颗节点的命中 quad **中心**的画布像素（左上原点，y 向下）
    /// —— `PointerLayer.ButtonAt/ClickAt` 吃的就是这一套口径。
    /// ⚠️ 一律**现算**、不写死坐标：这样「页面几何改了」不会把断言变成假红，而它要断的
    /// 「这一点上谁吃得到」与具体数字无关。</summary>
    static Vector2 PxCenterOf(Transform node)
    {
        var q = node != null ? node.GetComponentInChildren<ImageQuad>(true) : null;
        if (q == null) return new Vector2(-99999f, -99999f);      // 取不到 ⇒ 一定不在任何命中点上 ⇒ 断言会红
        return LayoutSpace.ToPixel(q.transform.position);
    }

    /// <summary>🆕 2026-10-07（A77⑲）：一颗 quad 头上那份材质用的 **shader 内部名**（没有材质就是 `&lt;没有材质&gt;`）。
    /// 变灰那一段核的就是**原版那张 shader 的名字**（`Everguild/UI/Greyscale`），⛔ 不是我们自己的常量。</summary>
    static string ShaderNameOf(ImageQuad q)
    {
        var mr = q != null ? q.GetComponent<MeshRenderer>() : null;
        var m = mr != null ? mr.sharedMaterial : null;
        return m != null && m.shader != null ? m.shader.name : "<没有材质>";
    }

    // ---------------- 🆕 2026-10-05（A78①）：软边**切线**的扫描器
    //
    // 软边的实现是「按渐隐带的内沿把这块 quad **切开**」（`MenuDraw.ApplySoftEdges`）——
    // 原节点留一格、其余格建**子 quad**（名字 `…_soft<i><j>`）⇒ 「宿主与子块共享的那条边」就是带的内沿。
    // ⇒ **看到切线 = 这条软边真的接上了**；期望值一律写成「视口边 ± 原版 `m_Softness`」的**算式结果**，
    //   ⛔ 不读被测实现里的任何常量（软边留在 0 就一条切线都没有；值写错切线就不在算出来的位置上）。
    // ⚠️ 本工程的自检辅助函数**按 Scene 各留一份**（`CollectionScene` / `RewardsScene` 各有一条同形的）
    //   —— 那三个文件各有各的自检入口，这里不跨文件共享（本文件只多这一份，两处实现只有这一份会被改）。
    //
    // 🔴 **2026-10-11（A232）：多了一个形参 `clip`** —— 「落在 `clip` **本轴**两条边上的切线【不算软边切口】」。
    //   为什么：`MenuDraw.ApplySoftEdges` 的切刀位置 = `clip.边 ± softness`，**某个分量 = 0 时那两条刀口
    //   正好落在框自己的两条边上**（那是**硬裁**边、不是渐隐带的内沿）⇒ 少了这一闸，「数竖切线 = 0」那几条
    //   的鉴别力就**依赖「实现侧恰好会裁」这个偶然性质**（判据原文 → `资料/普查产出_1008/波A_A225_三条红.md`
    //   顺手发现 §2；4 份同形副本一起补，做法见它的 §五·1）。
    //   ⚠️ **它是潜伏闸**：今天 `ApplySoftEdges` 入口先 `ClipVisToClip` 硬裁 ⇒ `vis ⊆ clip` ⇒ `SoftCuts`
    //   的「严格落在 `vis` 内部」**已经**挡掉了这类切口 ⇒ **今天一条读数都不会变**；它防的是「实现侧哪天
    //   不裁了」那一档 —— 那时它会**静默**把硬裁边当成「软边接上了」（本仓「弱断言分不出两种状态」那条）。
    //   ⚠️ 形参**必填**（⛔ 不给默认值）：漏传 = 这道闸静默失效 ⇒ 让编译期拦住。
    //   ⚠️ 传进来的 `clip` 一律是**原版字面量 / 已断言的视口局部量**，⛔ 不从被测实现里读。
    static List<float> ScanSoftCuts(Transform root, bool vertical, PxRect clip)
    {
        var cuts = new List<float>();
        if (root == null) return cuts;
        // A232 的闸：切刀落在本轴的两条 `clip` 边上 ⇒ 硬裁边，不算（容差同下面那 0.5px）
        bool OnClipEdge(float v) { return vertical
            ? (Mathf.Abs(v - clip.x1) <= 0.5f || Mathf.Abs(v - clip.x2) <= 0.5f)
            : (Mathf.Abs(v - clip.y1) <= 0.5f || Mathf.Abs(v - clip.y2) <= 0.5f); }
        const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;   // 108 px / 世界单位
        foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
        {
            if (q == null) continue;
            Vector2 hc = LayoutSpace.ToPixel(q.transform.position);
            float hw = q.WorldW * K * 0.5f, hh = q.WorldH * K * 0.5f;
            for (int i = 0; i < q.transform.childCount; i++)
            {
                var c = q.transform.GetChild(i).GetComponent<ImageQuad>();
                // ⚠️ **只认软边切出来的子块**（名字闸：`ApplySoftEdges` 起的是 `…_soft<i><j>`）——
                //    九宫格那 9 块是**兄弟**不是父子，但留一道名字闸更保险。
                if (c == null || c.name.IndexOf("_soft") < 0) continue;
                Vector2 cc = LayoutSpace.ToPixel(c.transform.position);
                float cw = c.WorldW * K * 0.5f, ch = c.WorldH * K * 0.5f;
                if (vertical)
                {
                    float vR = cc.x - cw;                    // 子块在**右** ⇒ 切线 = 子块左沿
                    float vL = cc.x + cw;                    // 子块在**左** ⇒ 切线 = 子块右沿
                    if (Mathf.Abs(vR - (hc.x + hw)) < 0.5f) { if (!OnClipEdge(vR)) cuts.Add(vR); }
                    else if (Mathf.Abs(vL - (hc.x - hw)) < 0.5f) { if (!OnClipEdge(vL)) cuts.Add(vL); }
                }
                else
                {
                    float vB = cc.y - ch;                    // 子块在**下** ⇒ 切线 = 子块上沿
                    float vT = cc.y + ch;                    // 子块在**上** ⇒ 切线 = 子块下沿
                    if (Mathf.Abs(vB - (hc.y + hh)) < 0.5f) { if (!OnClipEdge(vB)) cuts.Add(vB); }
                    else if (Mathf.Abs(vT - (hc.y - hh)) < 0.5f) { if (!OnClipEdge(vT)) cuts.Add(vT); }
                }
            }
        }
        return cuts;
    }

    /// <summary>切线清单的**逐条**判据：每条都必须落在 `want` 里（±`tol`），且 `want` 每一项**都出现过**。
    /// 空表直接报红（空表 = 这条软边没接上）。</summary>
    static void CheckSoftCuts(List<float> cuts, float[] want, float tol, string what)
    {
        CheckTrue(cuts.Count > 0, what + "：**有层被软边切开**（切线实测 "
            + (cuts.Count > 0 ? string.Join("、", cuts.ConvertAll(v => v.ToString("F2")).ToArray()) : "一条都没有")
            + "）—— **空表 = 这条软边没接上**（软边带宽留在 0）");
        var hit = new bool[want.Length];
        for (int i = 0; i < cuts.Count; i++)
        {
            int k = -1;
            for (int j = 0; j < want.Length; j++)
                if (Mathf.Abs(cuts[i] - want[j]) <= tol) { k = j; break; }
            CheckTrue(k >= 0, what + $"：切线 #{i + 1} 在 {cuts[i]:F2} ⇒ 必须是带的内沿（"
                + string.Join(" / ", System.Array.ConvertAll(want, v => v.ToString("F2"))) + "）");
            if (k >= 0) hit[k] = true;
        }
        for (int j = 0; j < want.Length; j++)
            CheckTrue(hit[j], what + $"：**{want[j]:F2} 这条切线确实出现**（少一条就说明那侧的软边没生效）");
    }

    // ============================================================ 🆕 A327：两态夹具（一条共用 · 四份【函数体】逐字同源）
    //
    // 🔴 **为什么要它**：2026-10-11（W4）把「世界 → 设计」那一族（`MenuDraw.PosInDesignSpace` / 各窗的
    //   `Local`·`Local3` / `CampaignTab.BuildLine` / `ShopWindow.BuildTimeCounter` / `CampaignTab.BuildArmyItems`）
    //   修完之后发现：**`k == 1`（小屏缩放开关出厂关着）时新旧两式逐位相同** ⇒ 那 8 处全是**潜伏缺陷**
    //   —— **今天一条现有断言都不会红**（不是「有断言挡着」，是**还没有断言**）。
    //   ⚠️ **2026-10-11（FX3）收窄一处口径**：那 8 处里**基准恰好就是窗根**的那几处，新旧两式在生产里
    //   **永远**逐位相同（`basis == 窗根` ⇒ 除的是 Holder，恒单位缩放）⇒ 是 **no-op**，不是「潜伏」；
    //   真带牙口的是**非根基准**那一族。展开见本段后面那条订正。
    //   判据 / 逐处清单 / 「该断言什么」→ `资料/普查产出_1011/W4_子3.md` §四·b。
    //
    // 🔴 **夹具形状**（判据给的就是这一条，⛔ 别另设计一套）：
    //   ① 态一 = 开关**关**（出厂态）⇒ 量一次 → `p1`；② 态二 = 开关**开** + **被乘的那一级**乘 M（走**生产那条路**
    //   `TransformScalerBySmallScreenUI`：`SetScale(M)` + `Tick()`，批处理没有帧循环）⇒ 再量同一个对象 → `p2`；
    //   ③ 断 **`p2 == M × p1`**（⛔ **一个我们自己的常量都不读** —— 只读 M）。
    //
    // 🔴 **2026-10-11（FX3）三处订正 —— 上一版夹具【自己把这条恒等式砸了】**（Shell 4 + Collection 4 条红；
    //   判据全文 → `资料/普查产出_1011/DIAG-A_Shell与Collection八条红.md`）：
    //   ① **M 加在【基准的父级】那一级**，⛔ **不是基准自己** —— `PosInDesignSpace` 除的正是
    //      `t.parent.lossyScale`（`Shell/MenuDraw.cs:74-78`），而 `p2 == M × p1` **只在「基准的父级就是
    //      那个被乘 M 的根、且那个根在世界原点」时成立**（`Shell/MenuDraw.cs:57-61` 自己写着适用范围）。
    //      上一版把 M 加在**窗根自己**身上、又把**窗根**挪到 (2,1.5) ⇒ 恒等式被夹具亲手破坏：
    //      偏差逐条 = `(1−M)×(2,1.5)` = **(−0.400, −0.300)**，与实现无关（日志里 8 条逐条对到小数点后 3 位）。
    //   ② **可观测余量** = 「**基准相对被乘那一级的位移** ≥1 设计单位」（⛔ 不是「离**世界原点**」——
    //      上一版量的就是后者，所以它逼着调用方去挪窗根）。坏式与好式相差 `M(M−1)×|那个位移|` ≈ `0.24 × |位移|`，
    //      容差 **0.02 单位（2.2px）** ⇒ `|位移| ≥ 1` 时偏差 ≥ 0.24 单位 = **26px**，远远超出容差 ⇒ 真会红。
    //      （四个调用点取 (2,1.5) ⇒ 0.6 单位 = **65px**。）
    //   ③ **态二的 `measure` 里必须【重建】**（`Open()` → `Build()` 首句清空子件）—— 不重建时被量的局部位置
    //      是 `k == 1` 那一趟**冻结**下来的值，新旧两式在那时**逐位相同** ⇒ 断言恒真（= 假绿）。
    //      带牙口的判据 → `资料/普查产出_1011/W4_子3.md:91-106`；同族先例 = `Editor/ShopScene.cs` 的 **A294** 那一段（同文件的两态探针）。
    // 🔴 **参数 1 = 被乘 M 的那一级**（`ShellScene`/`CollectionScene` 那四个调用点里它是**窗根的父级探针根**；
    //   ⚠️ 另两份副本（`RewardsScene` / `ShopScene`）传的是**窗根自己**、基准是窗根的子件 —— 2026-10-11（A350）四份已同步到**同一口径**；两族各自都对，⛔ 别按「哪一族更对」去改）。
    // ⚠️ **态二会把那一级乘 M 再还原**（`localScale` 放回 1 · 组件销毁 · 开关放回关）—— 直线写法，没有提前 return。
    static void CheckScaleTwo(GameObject scaleRoot, Transform basis, System.Func<Vector3> measure, float m, string what)
    {
        CheckTrue(scaleRoot != null && basis != null && measure != null, "（前提）" + what + "：夹具的件齐了");
        if (scaleRoot == null || basis == null || measure == null) return;
        SmallScreenUI.Set(false);                              // 态一：开关**关**（出厂态）
        Vector3 p1 = measure();
        Vector3 b1 = basis.position;                           // 态一的基准位置 = 它的**设计**位置（k == 1）
        Vector3 w1 = scaleRoot.transform.position;             // 被乘那一级的位置（态一；生产里 = 原点）
        // （前提②·可观测余量）**基准相对被乘那一级的位移** ≥1 设计单位 —— 基准落在那一级的原点上时
        // 「除不除缩放」两式**恒等** ⇒ 断言「什么都不中」也全绿。⛔ 上一版量的是「离**世界原点**」，
        // 逼着调用方去挪窗根、又把恒等式砸了（见本段文件头 ①②）。
        CheckTrue(Mathf.Abs(b1.x - w1.x) > 1f || Mathf.Abs(b1.y - w1.y) > 1f,
                  $"（前提）{what}：**基准相对被乘 M 那一级的位移 ≥1 设计单位**（实测 {b1.x - w1.x:F2},{b1.y - w1.y:F2}）"
                + " —— 位移≈0 时「除不除缩放」两式恒等 ⇒ 这一条会退化成假绿");
        CheckNear(scaleRoot.transform.localScale.x, 1f, 1e-4f, "（前提）" + what + "：态一那一级没被谁乘过");
        SmallScreenUI.Set(true);                               // 态二：开关**开** + 那一级乘 M
        var sc = scaleRoot.GetComponent<TransformScalerBySmallScreenUI>();
        if (sc == null) sc = scaleRoot.AddComponent<TransformScalerBySmallScreenUI>();
        sc.SetScale(m);
        sc.Tick();                                            // 批处理没有帧循环 ⇒ 手动推一次
        CheckNear(scaleRoot.transform.localScale.x, m, 1e-4f,
                  "（前提）" + what + "：态二那一级 `localScale` = M（真走的生产那条路）");
        // （前提①）**被除的那一级真的被乘了 M** —— `PosInDesignSpace` 除的是 `basis.parent.lossyScale`；
        // 那一级是单位缩放时新旧两式**逐位相同** ⇒ 下面那条 ★ 等于没查。改坏法：M 仍加在 `basis` 自己身上
        // （= 上一版那种塞法）⇒ 这条红。
        CheckNear(basis.parent != null ? basis.parent.lossyScale.x : 1f, m, 1e-3f,
                  "（前提）" + what + "：**基准的【父级】在态二被乘了 M**（`PosInDesignSpace` 除的正是这一级，"
                + "`Shell/MenuDraw.cs:74-78`）—— 父级单位缩放时新旧两式**恒等**，这条断言就等于没查");
        Vector3 p2 = measure();
        CheckNear(p2.x, m * p1.x, 0.02f,
                  $"★ {what}：**态二 == M × 态一**（x：{p2.x:F3} vs {m:F2}×{p1.x:F3}）"
                + " —— 两态合起来才证明「这一处的落位真的跟着被乘 M 的那一级缩放走」"
                + "（`k == 1` 时新旧两式逐位相同 ⇒ 只断态一的话，改坏了照样绿）");
        CheckNear(p2.y, m * p1.y, 0.02f, "★ " + what + "：……y 分量同理（只改 x 不改 y 时只有上一条红）");
        Object.DestroyImmediate(sc);                           // 还原
        scaleRoot.transform.localScale = Vector3.one;
        SmallScreenUI.Set(false);
        CheckNear(measure().x, p1.x, 0.02f, "（收尾）" + what + "：那一级放回 1 之后位置也回到态一那一份");
    }

    // ---------------- 🆕 A49 键盘导航的两个探针助手

    /// <summary>造一颗「键盘导航探针按钮」= 透明命中区 quad + `WindowButton`（形状照外壳里唯一的按钮做法
    /// `MenuWindowBase.AddHit` → `MenuDraw.Hit`）。`cxPx/cyPx` = **画布像素中心**（左上原点、y 向下），
    /// `wPx/hPx` = 命中区尺寸（px）。⚠️ 摆位是本节所有「方向 → 哪一颗」断言的**唯一依据**，所以写死在这里。</summary>
    static WindowButton MakeNavButton(Transform parent, string name, float cxPx, float cyPx, float wPx, float hPx)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var q = ImageQuad.Create(go.transform, CardArt.Solid(), Vector3.zero,
                                 LayoutSpace.Px(hPx), new Vector2(0.5f, 0.5f), name + " Quad");
        CheckTrue(q != null, $"探针按钮 `{name}` 的命中 quad 建出来了（没有它这颗就不可命中、也不可导航）");
        if (q != null)
        {
            q.SetAspect(wPx / hPx);
            q.SetTint(new Color(0f, 0f, 0f, 0f));        // 透明（照 `MenuDraw.Hit`）
            q.SetRenderQueue(3000);
            // ⚠️ `ImageQuad.Create` 的 `pos` 是 **localPosition** ⇒ 绝对摆位要在建完之后写 `position`
            //    （父链上有锚点偏移时，只写 localPosition 会整列挪位，而断言就会量到别处）。
            q.transform.position = LayoutSpace.FromPixel(cxPx, cyPx);
        }
        return go.AddComponent<WindowButton>();
    }

    /// <summary>此刻「选中」的名字（没有选中就是 `(无)`）。**期望值一律写成名字字面量** —— 见该节头那条纪律。</summary>
    static string SelName(PointerLayer pl)
        => pl != null && pl.Selected != null ? pl.Selected.name : "(无)";

    // ============================================================ 建场景

    static ShellRuntime Build(out Transform root)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 原版 `UI Camera`（`level0/Camera/Camera_258.json` 实证）：透视 fov 40 / near 0.3 / far 1000 /
        // ClearFlags 2(纯黑) / HDR + MSAA on。
        // ⚠️ **我们这条线用正交**（`LayoutSpace`：可见高度固定 10 单位）—— 见 `ShellRuntime.cs` 文件头那条说明。
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;        // 实证：ClearFlags 2 = 纯黑
        cam.nearClipPlane = 0.3f;                 // 实证
        cam.farClipPlane = 1000f;                 // 实证
        cam.allowHDR = true;                      // 实证
        cam.allowMSAA = true;                     // 实证
        cam.aspect = LayoutSpace.DesignAspect;    // ⚠️ 必须在建任何东西之前定死（踩过：批处理默认 4:3）
        LayoutSpace.Apply(cam);
        // 🔴 `AudioListener` **必须有一台**，否则整个游戏没声音（只会打一句警告）。
        //    原版 `UI Camera` 上就挂着一个（`level0/AudioListener_260.json`）—— 我们第一版漏了。
        camGo.AddComponent<AudioListener>();

        var rootGo = new GameObject("Shell");
        var rt = rootGo.AddComponent<ShellRuntime>();
        rt.Build();
        root = rootGo.transform;
        return rt;
    }

    /// <summary>一张图的平均亮度（0–255）。**空图护栏**用 —— 见 `Shoot` 里的说明。</summary>
    static float MeanBrightness(Texture2D t)
    {
        if (t == null) return 0f;
        var px = t.GetPixels32();
        if (px.Length == 0) return 0f;
        long sum = 0;
        for (int i = 0; i < px.Length; i += 7) sum += px[i].r + px[i].g + px[i].b++;   // 抽样（每 7 个取 1）
        return sum / 3f / ((px.Length + 6) / 7);
    }
    /// <param name="allowBlank">**已知会是全黑的**那几个状态显式放行（不是静音 —— 每一处都在调用点上写了原因）。
    /// 判据仍是「平均亮度 &gt; 3」，只是这几张本来就拍的是「屏幕上什么都没有」。</param>
    static void Shoot(string file, bool allowBlank = false)
    {
        var cam = Camera.main;
        if (cam == null) return;
        const int W = 1920, H = 1080;
        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        File.WriteAllBytes(Path.Combine(ShotDir, file), tex.EncodeToPNG());
        // 🔴 **空图护栏**（2026-09-23 踩到）：`Shoot` 原来是「拍完就写盘」，于是一张**全黑**的图
        //    也能安静地写出去（原因：上一个窗的 `CloseAllWindows()` 把要拍的那个窗也关了）。
        //    断言一条都不会报 —— 正是 `资料/已知的坑.md` 那条「自检截图可能是手工合成的」的同类。
        //    判据：**平均亮度**（0–255）；模板黑底大约 14~40，纯黑 ≈ 0。
        //    ⚠️ **必须在 `DestroyImmediate(tex)` 之前**（销毁之后 `tex == null`，护栏恒红）。
        float lum = MeanBrightness(tex);
        if (allowBlank) Debug.Log(P + $"  截图 {file} 平均亮度 {lum:F1}（**这一张按已知情况放行**）");
        else CheckTrue(lum > 3f, $"{file} 不是空图（平均亮度 {lum:F1} > 3）");

        Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        Debug.Log(P + $"  截图 {Path.Combine(ShotDir, file)}");
    }

    static Transform Find(string name, Transform root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    // ---------------- 压暗边的**顶点色**判据（A18）

    /// <summary>一块 `ImageQuad` 的顶点色，**网格顺序 = 左下 · 右下 · 右上 · 左上**
    /// （与 `ImageQuad.RebuildMesh` 的顶点同序 —— 读的是**真网格**，不是我们自己的字段）。</summary>
    static Color[] Verts(ImageQuad q)
    {
        var mf = q.GetComponent<MeshFilter>();
        var m = mf != null ? mf.sharedMesh : null;
        return (m != null && m.colors != null && m.colors.Length >= 4) ? m.colors : null;
    }

    /// <summary>四个顶点的 alpha 排成一行（**红了要能看出是哪一角错**）。</summary>
    static string Alphas(Color[] c)
        => $"BL {c[0].a:F3} · BR {c[1].a:F3} · TR {c[2].a:F3} · TL {c[3].a:F3}";

    /// <summary>指定那两个角的 alpha 是不是都等于某个**原版实测值**。值会打进消息里，红了不用再猜。</summary>
    static bool AlphaAt(Color[] c, int[] two, float want)
    {
        const float tol = 0.001f;
        return Mathf.Abs(c[two[0]].a - want) <= tol && Mathf.Abs(c[two[1]].a - want) <= tol;
    }

    /// <summary>顶点色的 rgb 是不是**纯黑**（原版 `Gradient2.colorKeys` 两个都是 (0,0,0)）。</summary>
    static bool IsBlackRgb(Color[] c)
    {
        foreach (var v in c)
            if (Mathf.Abs(v.r) > 0.001f || Mathf.Abs(v.g) > 0.001f || Mathf.Abs(v.b) > 0.001f) return false;
        return true;
    }

    /// <summary>材质 tint 是不是**纯白不透**（原版 `Image.m_Color = (1,1,1,1)` ⇒ 颜色只能走顶点色）。</summary>
    static bool IsWhiteTint(Color c)
    {
        return Mathf.Abs(c.r - 1f) < 0.001f && Mathf.Abs(c.g - 1f) < 0.001f
            && Mathf.Abs(c.b - 1f) < 0.001f && Mathf.Abs(c.a - 1f) < 0.001f;
    }

    /// <summary>
    /// 一条压暗边的**渐变 + 尺寸/位置**断言。
    /// · 渐变判据 = 原版 `Gradient2` 实测值（`工具/read_gradient2_level0.py`）：色键纯黑 ·
    ///   alpha 三键 **外端 1 · 拐点 0.709804 · 内端 0** · `Image.m_Color` 是白的；
    /// · 尺寸/位置判据 = 原版 `m_SizeDelta` / `m_AnchoredPosition` 实测（`level0/RectTransform_{…}.json`
    ///   + 运行期 dump），**由调用点以字面量传进来**（见 §⑧）。
    /// </summary>
    /// <param name="edgePx">它贴的那条屏幕边的坐标（左/右 ±960 · 上/下 ±540 —— **原版 `RectTransform` 实证**，
    /// 不是我们自己的常量）。</param>
    /// <param name="spanPx">**整条沿淡出方向**的长（= **渐变走的那一维**，原版 `m_SizeDelta` 实测 px，
    /// **全精度**）：左/右 = 宽 **205.809097** · 下 = 高 **146.33949** · 上 = 高 **146.339**。
    /// ⚠️ 上下两条的渐变走 y，所以这里是**高**、不是那条长边。</param>
    /// <param name="crossPx">**整条另一维**的长（原版实测，**全精度**）：左/右 = 高 **2585.45996** ·
    /// 下 = 宽 **4605.01025** · 上 = 宽 **4569.2998**（⚠️ **上下两条不一样** —— 原来两条都写 4605，是照抄了下条）。</param>
    /// <param name="crossOffPx">**整条**在另一维上的中心偏置（原版 `m_AnchoredPosition` 实测）：
    /// **上/下两条的 x = −4.500122**（不是 0）· 左/右两条的 y = 0（原版 −0.0001220703125 ≈ 0.0001px，
    /// **按 0 算**）。</param>
    /// <param name="horizontal">渐变沿 x（左/右两条）还是沿 y（上/下两条）。</param>
    /// <param name="opaqueAtMin">屏幕边在坐标**小**的一侧（Left / Bottom = true）。</param>
    /// <param name="expectActive">这条边**出厂的开关**（原版 `runtime_ui_dump_Intro.tsv` 的 activeSelf 实证：
    /// 左右两条**常开**、上下两条 **inactive**）。⚠️ **由调用点传字面量进来** ——
    /// 从 `ShellRuntime` 里读同一个开关就是自证（见 §⑦ 那条注释）。</param>
    static void CheckFadeEdge(ShellRuntime shell, int idx, string name, bool horizontal, bool opaqueAtMin,
                              float edgePx, float spanPx, float crossPx, float crossOffPx, bool expectActive)
    {
        var qo = shell.FadeSide(idx);          // 外侧半（贴屏幕边）
        var qi = shell.FadeSideInner(idx);     // 内侧半（靠屏幕中心）
        CheckTrue(qo != null && qi != null, $"{name}：外侧半 + 内侧半两块都在（`ShellRuntime.FadeSide`）");
        if (qo == null || qi == null) return;

        var co = Verts(qo); var ci = Verts(qi);
        CheckTrue(co != null && ci != null, $"{name}：两块都有**逐顶点色**（`SetCornerColors` 落进了网格）");
        if (co == null || ci == null) return;

        // ① **不是靠改材质 tint** —— 原版 `Image.m_Color` 就是白的，颜色全在顶点色里。
        //    这条同时挡住「退回 `SetTint(黑, 1)` 那一版」：那样两端的 alpha 都会是 1（= 不透明黑板）。
        CheckTrue(IsWhiteTint(qo.Tint) && IsWhiteTint(qi.Tint),
                  $"{name}：材质 tint 是**白**（原版 `Image.m_Color = (1,1,1,1)` —— 渐变全走顶点色，不靠 tint）");

        // ② 颜色 = **纯黑**（色键两个都是 (0,0,0)）
        CheckTrue(IsBlackRgb(co) && IsBlackRgb(ci), $"{name}：顶点色的 rgb **纯黑**（原版 colorKeys 都是 (0,0,0)）");

        // ③ 网格顶点顺序 = **BL · BR · TR · TL** ⇒ 按「屏幕边在小侧还是大侧」取两端的**两个角**
        int[] outerEnd, innerEnd;
        if (horizontal && opaqueAtMin) { outerEnd = new[] { 0, 3 }; innerEnd = new[] { 1, 2 }; }   // 屏幕边在 x 小侧
        else if (horizontal) { outerEnd = new[] { 1, 2 }; innerEnd = new[] { 0, 3 }; }             // 屏幕边在 x 大侧
        else if (opaqueAtMin) { outerEnd = new[] { 0, 1 }; innerEnd = new[] { 2, 3 }; }            // 屏幕边在 y 小侧
        else { outerEnd = new[] { 3, 2 }; innerEnd = new[] { 0, 1 }; }                             // 屏幕边在 y 大侧

        // ④ **两端 alpha 是 1 → 0**；中间那一跳是原版的拐点 0.709804（外侧半的里端 = 内侧半的外端）
        //
        // 🔴 2026-10-03 改判据（**这 3 条 × 4 条边 = 12 条原来是自证**）：
        //    原来这 3 条比的是 `ShellRuntime.FadeAlphaOuter / FadeAlphaMid / FadeAlphaInner`，
        //    而那三个常量（`Shell/ShellRuntime.cs` 的 `FadeAlphaOuter / FadeAlphaMid / FadeAlphaInner`）**正是 `FadeEdge` 写进顶点色的同一个来源**
        //    ⇒ **把常量改成任意值，网格跟着变、断言照样绿**（= 拿我们的常量断言我们自己写出来的值）。
        //    现在三个数**写成字面量**，出处 = **原版 `Gradient2` 实测**（不是我们挑的）：
        //    `工具/read_gradient2_level0.py` 从 `level0` 原始字节读出 alpha 三键 = **1 / 0.709804 / 0**，四条边一致。
        //    ⇒ 常量若被动过，这里必红（写法与 §② 的 `edgePx` 一族相同：期望值取原版实测的字面量）。
        //    ⚠️ 边界：`Gradient.Evaluate` 会不会**再乘一道色键 alpha 轨**这件事还没定 —— 它归
        //    §②·a 的「渐探针」去探（**探针只打数，不改这里的期望值**）。
        CheckTrue(AlphaAt(co, outerEnd, 1f),
                  $"{name}：**贴屏幕边那端 alpha = 1**（不透明黑）（{Alphas(co)}）");
        CheckTrue(AlphaAt(co, innerEnd, 0.709804f) && AlphaAt(ci, outerEnd, 0.709804f),
                  $"{name}：拐点两端都是 0.709804（外侧半 {Alphas(co)} · 内侧半 {Alphas(ci)}）");
        CheckTrue(AlphaAt(ci, innerEnd, 0f),
                  $"{name}：**靠屏幕中心那端 alpha = 0**（全透）（{Alphas(ci)}）");

        // ⑤ 方向（几何核）：**a=1 的那一端必须真的贴在那条屏幕边上**
        float halfO = horizontal ? qo.WorldW * 0.5f : qo.WorldH * 0.5f;
        float ctrO = horizontal ? qo.transform.position.x : qo.transform.position.y;
        CheckNear(ctrO + (opaqueAtMin ? -halfO : halfO), edgePx / 108f, 0.01f,
                  $"{name}：**不透明的那一端就贴在那条屏幕边上**（{edgePx}px）");

        // ⑥ 两块在拐点处**严丝合缝**（留缝会露出没压暗的一条；重叠会把拐点压深）
        float halfI = horizontal ? qi.WorldW * 0.5f : qi.WorldH * 0.5f;
        float ctrI = horizontal ? qi.transform.position.x : qi.transform.position.y;
        CheckNear(ctrO + (opaqueAtMin ? halfO : -halfO), ctrI + (opaqueAtMin ? -halfI : halfI), 0.001f,
                  $"{name}：两块在拐点处**严丝合缝**（不重叠也不留缝）");

        // ⑦ 开关：**两条半都拿「原版那条边的 activeSelf」当判据**（左右常开 · 上下关 —— 实证见 §② 那节标题）。
        //    ⚠️ 原来这里写的是 `Check(qi.activeSelf, qo.activeSelf, …)`（内侧半跟外侧半一致）——
        //    **它结构上恒真、不是判据**：`FadeEdge` 的 `for (int k…)` 里两块是用**同一个 `active` 变量**
        //    同一轮 `SetActive` 的（`Shell/ShellRuntime.cs` 的 `FadeEdge` 里、k 循环体内那一行）⇒ 这个等式永远成立。
        //    （真正的用法是「谁**本该**是什么状态」，所以判据必须来自**原版**，不能来自同一份实现。）
        //    现在：外侧半 / 内侧半**各自**去比调用点传进来的原版字面量。
        Check(qo.gameObject.activeSelf, expectActive,
              $"{name}：外侧半的出厂开关 = 原版（{(expectActive ? "开" : "关")}）");
        Check(qi.gameObject.activeSelf, expectActive,
              $"{name}：内侧半的出厂开关 = 原版（同一条边 ⇒ 必须与外侧半**同为** {(expectActive ? "开" : "关")}）");

        // ⑧ **尺寸与位置**（🆕 2026-10-03 补）：一条边 = 两块 ⇒ **两块沿淡出方向各占整条的一半**、
        //    **另一维与整条等长**、**另一维的中心 = 原版的偏置**（上下两条是 −4.5，不是 0）。
        //    判据 = 原版 `m_SizeDelta` / `m_AnchoredPosition` 的**实测字面量**（由调用点传进来）
        //    —— ⛔ **不是** `ShellRuntime` 里那几个常量，那正是「拿我们的常量断言我们的常量」（§② 那条老毛病）。
        //    这一节同时替掉原来挂在 `Find("Smooth background fade Top")` 上的「上边宽度 = 4605px」——
        //    那条量的是**外侧半**、数字还是**下条**的 4605（写成字面量也是错的：上条实测 4569.2998）
        //    ⇒ 「整条宽度」现在由下面 `crossPx` 那两条断（上 4569.2998 / 下 4605.01025，各自独立的字面量）。
        float spanO = horizontal ? qo.WorldW : qo.WorldH;
        float spanI = horizontal ? qi.WorldW : qi.WorldH;
        CheckNear(spanO, spanPx * 0.5f / 108f, 0.005f, $"{name}：外侧半沿淡出方向 = {spanPx}px ÷ 2（原版 m_SizeDelta 实测）");
        CheckNear(spanI, spanPx * 0.5f / 108f, 0.005f, $"{name}：内侧半沿淡出方向 = {spanPx}px ÷ 2（原版 m_SizeDelta 实测）");
        float crossO = horizontal ? qo.WorldH : qo.WorldW;
        float crossI = horizontal ? qi.WorldH : qi.WorldW;
        CheckNear(crossO, crossPx / 108f, 0.005f, $"{name}：外侧半另一维 = {crossPx}px（原版 m_SizeDelta 实测）");
        CheckNear(crossI, crossPx / 108f, 0.005f, $"{name}：内侧半另一维 = {crossPx}px（原版 m_SizeDelta 实测）");
        float crossCtrO = horizontal ? qo.transform.position.y : qo.transform.position.x;
        float crossCtrI = horizontal ? qi.transform.position.y : qi.transform.position.x;
        CheckNear(crossCtrO, crossOffPx / 108f, 0.005f, $"{name}：外侧半另一维的中心 = 原版 {crossOffPx}px");
        CheckNear(crossCtrI, crossOffPx / 108f, 0.005f, $"{name}：内侧半另一维的中心 = 原版 {crossOffPx}px");
        // 两块**合起来 = 整条**（**沿淡出方向**）：外侧半的外端贴着屏幕边（见 ⑤），内侧半的内端就落在**整条的另一头**
        // ⇒ 拆分不许把这条边的长度改掉（⚠️ 这条量的是**渐变那一维**；「上条宽度 4569.3」由上面 `crossPx` 那两条断）。
        float farEnd = opaqueAtMin ? ctrI + halfI : ctrI - halfI;
        CheckNear(farEnd, (opaqueAtMin ? edgePx + spanPx : edgePx - spanPx) / 108f, 0.005f,
                  $"{name}：两块合起来 = **整条 {spanPx}px**（内侧半的内端落在整条的另一头）");
    }

    // ============================================================ 自检

    public static void Run()
    {
        _pass = 0; _fail = 0; _failures.Clear();
        Directory.CreateDirectory(ShotDir);
        Debug.Log(P + "=== 外壳自检 开始 ===");

        var shell = Build(out var root);
        var cam = Camera.main;

        // ---------------- ① 骨架
        Section("骨架：相机 / 三个锚点（正本 §三 第 7 条：主菜单那层三个 Holder 缺一不可）");
        CheckTrue(cam != null && cam.orthographic, "相机是正交（本工程全线用 `LayoutSpace`）");
        CheckNear(cam.orthographicSize, LayoutSpace.DesignHeight * 0.5f, 0.001f, "可见高度 = 10 个世界单位（`LayoutSpace.DesignHeight`）");
        Check(cam.backgroundColor, Color.black, "清屏色纯黑（原版 `Camera_258` ClearFlags 2）");

        CheckTrue(WindowsManager.HasAnchor(WindowsPlacement.World),  "锚点 10 (Below Upper Bar) 注册上了");
        CheckTrue(WindowsManager.HasAnchor(WindowsPlacement.Canvas), "锚点 5 (Canvas Holder) 注册上了");
        CheckTrue(WindowsManager.HasAnchor(WindowsPlacement.Popup),  "锚点 15 (PopUp Holder) 注册上了");
        Check((int)WindowsPlacement.World, 10, "WindowsPlacement.World 的值照原版 = 10");
        Check((int)WindowsPlacement.Canvas, 5, "WindowsPlacement.Canvas 的值照原版 = 5");
        Check((int)WindowsPlacement.Popup, 15, "WindowsPlacement.Popup 的值照原版 = 15");

        // ---------------- ② 压暗层四边
        //
        // ⚠️ **一条边在我们这儿是两块**（`… outer` / `… inner`，见 `ShellRuntime.FadeEdge` 的注释）⇒
        //    **不能再用 `Find("Smooth background fade TOP")` 去量**：原版那条节点是**整条**（4569.2998×146.339），
        //    我们这两块**沿淡出方向各只拿一半** —— 顶着原版名的那块会让「按名字量」的人拿到**半条**
        //    （2026-10-03 之前正是如此）。
        //    尺寸/位置一律收口进 `CheckFadeEdge`（判据 = 原版 `m_SizeDelta` / `m_AnchoredPosition` 的**实测字面量**）。
        Section("FadeBackground 四边（尺寸实证：左右 205.809097×2585.45996 @ x=∓960 常开 · 下 4605.01025×146.33949 · 上 4569.2998×146.339 @ x=−4.500122 · y=∓540 关）");
        var fadeL = Find("Smooth background fade Left outer", root);
        var fadeT = Find("Smooth background fade TOP outer", root);
        CheckTrue(fadeL != null && fadeL.gameObject.activeSelf, "左边那条**出厂是开的**（`… Left outer`）");
        CheckTrue(fadeT != null && !fadeT.gameObject.activeSelf,
                  "上边那条**出厂是关的**（`… TOP outer`；原版 `m_IsActive: false` 实证）");
        // 🔴 名字契约（**我们自己的约定**，不是原版参数）：两块都带后缀 ⇒ **没有节点顶着原版那条整条的名字**。
        CheckTrue(Find("Smooth background fade TOP", root) == null,
                  "没有节点**顶着原版整条的名字**（原版那条是 4569.3×146.3 一整条；我们拆两块 ⇒ 都加 ` outer`/` inner` 后缀，"
                  + "否则按原版名量到的是**半条**）");
        // 🆕 2026-10-03（§三 第 29 条 B3）：四条要**贴着屏幕边**（原版 `level0` 的 pivot 是 (0,.5) / (.5,0)）
        //   原来按**中心 pivot** 摆在 ∓960 ⇒ **一半在屏外**（实测左条只有 102.9px 可见、右条同）。
        {
            var fadeR = Find("Smooth background fade Right outer", root);
            var ql = fadeL != null ? fadeL.GetComponentInChildren<ImageQuad>() : null;
            var qr = fadeR != null ? fadeR.GetComponentInChildren<ImageQuad>() : null;
            if (ql != null)
                CheckNear(ql.transform.position.x - ql.WorldW * 0.5f, -960f / 108f, 0.01f,
                          "左条的**左边缘 = 屏幕左边缘**（原版 `pivot=(0,.5)` + `pos.x=−960`）");
            if (qr != null)
                CheckNear(qr.transform.position.x + qr.WorldW * 0.5f, 960f / 108f, 0.01f,
                          "右条的**右边缘 = 屏幕右边缘**（原版 `pivot=(0,.5)` + `pos.x=+960` + `scale.x=−1`）");
        }

        // ---------------- ②·a 🔴 渐探针（2026-10-03 加）：探 **Unity `Gradient.Evaluate` 的语义**
        //
        // **为什么要有它**：我们四条压暗边的顶点色是**手写**的三键分段线性（`ShellRuntime.FadeEdge`：
        //   外端 1 → 拐点 0.709804 → 内端 0）。而原版那条 `Gradient2` 的**色键里第二个 key 的 color.a 也是 0.709804**
        //   （`工具/read_gradient2_level0.py` 读出来的原始字节）。⇒ 还剩一个没定的事实：
        //   Unity 的 `Gradient.Evaluate` 取 alpha 时，是**只取 `alphaKeys` 那条轨**，
        //   还是 **`colorKeys[i].color.a` 与 `alphaKeys` 再相乘**？两条都「讲得通」，但中点差 0.1。
        //
        // 🔴 **这个块不是断言**（不判、不计分、不阻塞）：它**只打数**。
        //   ⛔ **不许拿它的读数去改 `ShellRuntime` 的任何数值** —— 结论归上报那一方判（`ShellRuntime.cs` 那条实现在本批里是冻结的）。
        //   触发：跟着 `ShellScene.Run` 一起跑（`工具/_run_8_checks.sh` 的第 4 条，日志 `d:/4/_tmp_view/shell.log`）。
        //
        // **判读**（把日志里那三个 `[渐探针] t=… a=…` 对到下面任意一行）：
        //   · `0.854902 / 0.709804 / 0.354902` ⇒ **只取 alpha 轨**（= 我们现在的实现口径 ⇒ **那就是对的**）
        //   · `0.792938 / 0.606788 / 0.277706` ⇒ **两轨相乘**（⇒ t=0.5 那个中点得改成相乘后的值）
        //   · 结构判据：下面那行 `readback colorKeys[1].a` 若**回读成 0.709804**（= 被 alpha 键顶掉）
        //     ⇒ **两轨共用一份存储** ⇒ 当场闭合，不用再看比值。
        //     ⚠️ 但这条读法的**前提是**「我们写进色键的那个数**不是** 0.709804」—— 上面那段探针两个轨写的是**同一个数**，
        //     所以回读 0.709804 **两种假设都成立、区分不了**。因此紧跟了一行**区分版**（色键写 0.25、alpha 键仍 0.709804）：
        //     · 区分版回读 `colorKeys[1].a = 0.250000` + `alphaKeys[1].a = 0.709804` ⇒ 两轨**各存各的**（色键的 alpha 被完整保留）
        //     · 区分版回读 `colorKeys[1].a = 0.709804` ⇒ **两轨耦合**（色键的 alpha 被 alpha 键顶掉了）⇒ 与上面同一结论
        Section("渐探针：Unity `Gradient.Evaluate` 会不会再乘一道色键 alpha 轨（**只打数，不判、不改实现**）");
        {
            var g = new Gradient();
            g.colorKeys = new[]{ new GradientColorKey(new Color(0,0,0,1f), 0f),
                                 new GradientColorKey(new Color(0,0,0,0.709804f), 1f) };
            g.alphaKeys = new[]{ new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.709804f, 0.5f),
                                 new GradientAlphaKey(0f, 1f) };
            foreach (var t in new[]{0.25f, 0.5f, 0.75f}) Debug.Log($"[渐探针] t={t} a={g.Evaluate(t).a:F6}");
            Debug.Log($"[渐探针] readback colorKeys[1].a={g.colorKeys[1].color.a:F6} alphaKeys[1].a={g.alphaKeys[1].alpha:F6}");
            // 区分版（见上面「⚠️ 前提」那条）：色键那条轨换成**另一个数**再回读
            g.colorKeys = new[]{ new GradientColorKey(new Color(0,0,0,1f), 0f),
                                 new GradientColorKey(new Color(0,0,0,0.25f), 1f) };
            Debug.Log($"[渐探针] 区分版（色键写 0.25）readback colorKeys[1].a={g.colorKeys[1].color.a:F6} alphaKeys[1].a={g.alphaKeys[1].alpha:F6}");
        }

        // ---------------- ②·b 🆕 A18：压暗边是**黑→透明**的逐顶点色渐变（改之前画的是一块不透明纯黑板）
        //
        // 判据（**原版实测**，`工具/read_gradient2_level0.py` 从 `level0` 的原始字节复现，四条一致）：
        //   `Image.m_Color = (1,1,1,1)`（**白**）· 色键 2 个**都是纯黑** · alpha 三键 **1 / 0.709804 / 0** ·
        //   `_gradientType` 左/右 = 0（沿 x）· 上/下 = 1（沿 y）· `_modifyVertices = 1`。
        //   **哪一端不透明**：`Gradient2.ModifyMesh` 的 t=0 取坐标**小**的一侧，而四条的 pivot 都摆在屏幕边那一侧
        //   （Right 带 `scale.x=−1`、TOP 带 `scale.y=−1`，把「小侧」翻到屏幕边上）
        //   ⇒ **贴屏幕边那一端 a=1（不透明黑）、往屏幕中心淡到 0**。
        //   ⚠️ 一条边是**两块**（拐点在 t=0.5）：`ImageQuad` 的网格只有 4 个顶点，一条只能表达两键线性。
        Section("压暗层四条：**黑→透明**的逐顶点色渐变（原版 `Gradient2`：外端 1 · 拐点 0.709804 · 内端 0）");
        // 参数（**全是原版实测字面量**，出处 = `runtime_ui_dump_Intro.tsv:13-16` +
        // `level0/RectTransform_{282,283,286,277}.json`，两条源逐条吻合）：
        //   ① idx ② 名字（照**原版**节点名，⚠️ **上条的 `TOP` 是大写**）
        //   ③ `horizontal` ④ `opaqueAtMin` ⑤ 贴的那条屏幕边（±960 / ±540）
        //   ⑥ **整条沿淡出方向**的长（**渐变走的那一维** —— 左/右 = 宽 **205.809097** · 下 = 高 **146.33949** ·
        //      上 = 高 **146.339**）
        //   ⑦ 整条**另一维**的长（左/右 = 高 **2585.45996** · 下 = 宽 **4605.01025** · 上 = 宽 **4569.2998**）
        //   ⑧ 另一维的中心偏置（**上/下 = −4.500122**、左/右 = 0 —— 原版是 −0.0001220703125 ≈ 0.0001px，
        //      **我们按 0 算**，这是一处**已写明**的取舍，不是截断）⑨ 出厂开关（原版实证：左右常开、上下关）
        // 🔴 **2026-10-06（A39-A1）：这里原来写的是 TSV 的【1 位小数】**（`205.8` / `2585.5` / `146.3` / `4605.0` /
        //    `4569.3` / `−4.5`），而 TSV 是运行期 dump、**被四舍五入过**；原值要看序列化 JSON（上面那四份）。
        //    最大差 `146.33949 − 146.3 = 0.03949px`，**本函数容差 0.005 世界单位 = 0.54px ⇒ 结构上抓不到**
        //    （这就是 A-1 那条的原文）⇒ 现在**直接写原值**（铁律 11：与原版不符的一律改成一致）。
        //    ⚠️ 上/下两条的**高不一样**（`146.33949` vs `146.339`）—— 与「两条的宽不一样」同理，**别互推**。
        //    ⚠️ 左右两条的**高**原值是 `2585.45996`（**不是** `2585.5`）—— 也是这次一起订正的。
        // ⚠️ ⑥⑦ 别按「宽 / 高」想当然填 —— 上/下两条的**渐变走 y**，所以「沿淡出方向」是**高**、
        //    「另一维」才是**宽 4605.01025 / 4569.2998**（`ShellRuntime.FadeEdge` 里的 `spanPx`/`crossLen` 同此口径）。
        CheckFadeEdge(shell, 0, "Smooth background fade Left",   true,  true,  -960f, 205.809097f, 2585.45996f, 0f,        true);
        CheckFadeEdge(shell, 1, "Smooth background fade Right",  true,  false,  960f, 205.809097f, 2585.45996f, 0f,        true);
        CheckFadeEdge(shell, 2, "Smooth background fade Bottom", false, true,  -540f, 146.33949f,  4605.01025f, -4.500122f, false);
        CheckFadeEdge(shell, 3, "Smooth background fade TOP",    false, false,  540f, 146.339f,    4569.2998f,  -4.500122f, false);

        // ---------------- ②·c 🆕 A39-A1：**实现侧那几个常量本身** = 原版全精度原值
        //
        // **为什么单开一节**：上面 `CheckFadeEdge` 那几条尺寸断言的容差是 `0.005` 世界单位 = **0.54px**，
        //   而「常量被截成 1 位小数」的最大差只有 **0.03949px**（`146.33949 → 146.3`；其余
        //   `2585.45996 → 2585.5` 差 0.04 · `4605.01025 → 4605` 差 0.01025 · `205.809097 → 205.8` 差 0.009 ·
        //   `−4.500122 → −4.5` 差 0.000122）⇒ **结构上抓不到**（= `资料/待办判据_审查发现_1004.md` §A39
        //   的 A-1 原文）。所以这里**直接比常量本身**：主体 = `ShellRuntime` 那几个 `const`，
        //   期望值 = 原版 `level0/RectTransform_{282,283,286,277}.json` 的 `m_SizeDelta` / `m_AnchoredPosition`
        //   **全精度字面量**（第一手判据 —— TSV 那份是运行期 dump、被四舍五入过）。
        // ⛔ 这一节**不是**「拿我们的常量断言我们的常量」：期望值来自原版 JSON ⇒ 谁把常量改回 1 位小数，
        //   这里 **6 条一起红**（2026-10-06 之前正是 1 位小数，本节就是为它加的）。
        Section("压暗边常量 = 原版 JSON 的**全精度**原值（A39-A1：截成 1 位小数这里就红）");
        {
            // 容差 0.0005px —— 比最小的一处截断差（0.009px）还小 18 倍 ⇒ 任何一位截断都被抓住；
            // 常量本身是 `float` 字面量、原值也是 `float32` ⇒ 实际误差就是 0，留这点只是防浮点噪声。
            const float T = 0.0005f;
            CheckNear(ShellRuntime.FadeSideW, 205.809097f, T,
                      "左右两条的宽（`RectTransform_282/283.m_SizeDelta.x` = 205.80909729）");
            CheckNear(ShellRuntime.FadeSideH, 2585.45996f, T,
                      "左右两条的高（同上 `.y` = 2585.4599609375 —— ⚠️ **不是 2585.5**）");
            CheckNear(ShellRuntime.FadeBarH, 146.33949f, T,
                      "上下两条的高（`RectTransform_277.m_SizeDelta.y` = 146.33949279785156；"
                      + "⚠️ 上条原版是 146.33900451660156 —— 我们**共用一份**常量，差 0.0005px）");
            CheckNear(ShellRuntime.FadeBarX, -4.500122f, T,
                      "上下两条的 x 偏置（`RectTransform_277/286.m_AnchoredPosition.x` = −4.5001220703125）");
            CheckNear(ShellRuntime.FadeBottomW, 4605.01025f, T,
                      "下条的宽（`RectTransform_277.m_SizeDelta.x` = 4605.01025390625）");
            CheckNear(ShellRuntime.FadeTopW, 4569.2998f, T,
                      "上条的宽（`RectTransform_286.m_SizeDelta.x` = 4569.2998046875 —— ⚠️ 与下条**不是同一个数**）");
        }

        // ---------------- ③ 载入文案两条
        Section("Loading / Progress text（版式实证：1920×48 · y=70 常开 / y=21.8 关）");
        var load = Find("Loading text", root);
        var prog = Find("Progress text", root);
        CheckTrue(load != null && load.gameObject.activeSelf, "`Loading text` 出厂是开的");
        CheckTrue(prog != null && !prog.gameObject.activeSelf, "`Progress text` 出厂是关的（实证 inactive）");
        if (load != null)
        {
            // ⚠️ `Find` 拿到的是那条**包装节点**（名字就叫 `Loading text`，在原点）；
            //    Label 是它的子节点（名字 `Loading text Text`）—— 探错节点会得到 0（第一版就是这么红的）
            var lb = load.GetComponentInChildren<Label>();
            if (lb != null)
                // 🔴 2026-10-03 改判据：期望值原来是 `ShellRuntime.LoadingY` —— **那正是建它时用的那个常量**
                //    （`Shell/ShellRuntime.cs` 的 `LoadingY`，建 Label 的 y 就用它算）⇒ 常量改了断言跟着一起动，恒绿（自证）。
                //    现在写成**原版实测的字面量 70px**（出处：`runtime_ui_dump_Intro.tsv`，写在 `ShellRuntime.LoadingY` 的注释里；
                //    540 = 画布半高、108 = 1 像素/世界单位 —— 都是本文件里既有的约定换算）。
                CheckNear(lb.transform.localPosition.y, (70f - 540f) / 108f, 0.002f,
                          "`Loading text` 的 y = 70px（原版 TSV 实证）");
            else CheckTrue(false, "`Loading text` 下面挂着 Label");
        }

        // ---------------- ④ 压暗 / 遮罩
        Section("Shade（字段实证：onAlphaLevel 0.8 · defaultTimeToSwitch 0.5s） / BlockingOverlay");
        CheckNear(shell.Shade.onAlphaLevel, 0.8f, 0.0001f, "Shade.onAlphaLevel = 0.8（战场场景 13 处实证）");
        CheckNear(shell.Shade.defaultTimeToSwitch, 0.5f, 0.0001f, "Shade.defaultTimeToSwitch = 0.5s（同上）");
        shell.Shade.SetAlpha(0f);
        CheckNear(shell.Shade.Alpha, 0f, 0.001f, "SetAlpha(0) 之后 alpha = 0（= 不挡视线）");
        shell.Shade.SetAlpha(shell.Shade.onAlphaLevel);
        CheckNear(shell.Shade.Alpha, 0.8f, 0.001f, "SetAlpha(onAlphaLevel) 之后 alpha = 0.8");
        shell.Shade.SetAlpha(0f);

        CheckTrue(!BlockingOverlay.IsBlocking, "遮罩默认**不挡**输入");
        shell.Blocker.StartSpinning();
        CheckTrue(BlockingOverlay.IsBlocking, "`StartSpinning()` 之后挡输入");
        shell.Blocker.StopSpinning();
        CheckTrue(!BlockingOverlay.IsBlocking, "`StopSpinning()` 之后放开");

        // ---------------- ⑤ 窗口系统（照原版 `OpenWindowCO` 的判定顺序）
        Section("窗口系统：全屏窗 / 弹窗 / 关窗（正本 §三）");
        var winGo = new GameObject("TestWindow");
        var win = winGo.AddComponent<GameWindow>();
        win.type = WindowType.Fullscreen;
        win.placement = WindowsPlacement.Canvas;
        WindowsManager.AttachToAnchor(win);
        shell.Windows.OpenWindow(win, null);
        Check(shell.Windows.openWindows.Count, 1, "开了一个全屏窗之后 `openWindows` 有 1 个");
        Check(shell.Windows.currentWindow, win, "它成了 `currentWindow`");
        Check(win.transform.parent != null ? win.transform.parent.name : "(空) ",
              "2 - Canvas Holder Above upper bar", "它被挂到了 **Canvas(5)** 那个锚点下面（不是场景根）");

        bool okFired = false;
        shell.Windows.ShowPopUp("自检弹窗", "确定", () => okFired = true);
        Check(shell.Windows.openWindows.Count, 2, "弹窗开出来之后 `openWindows` 有 2 个");
        Check(win.CurrentState, WindowState.Background, "全屏窗被弹窗**压到背景**（原版 `ToBackground()`）");
        var popup = shell.Windows.popUpWindow;
        CheckTrue(popup != null, "`popUpWindow` 指向那个弹窗");
        if (popup != null)
            Check(popup.transform.parent != null ? popup.transform.parent.name : "(空) ",
                  "3 - PopUp Holder", "弹窗挂在 **Popup(15)** 锚点下面");
        Shoot("02_弹窗.png");

        // 🔴 **2026-10-12（A416）改点法**：宿主收编成 `PopUpGameWindow` 之后**不能**再拿「子树里第一颗
        //   `WindowButton`」去点 —— `PopUpGameWindow.Build()` 第 1 步就建了 `MenuDraw.Absorb` 那颗
        //   **吸收层**（`absorbOnly = true`，`WindowButton.Click` 头一句就早退）⇒ 那一下被吃掉：
        //   回调不触发、窗也关不掉（下面两条同时红）。⇒ 按**名字**点那颗真钮（1 按钮版叫 `Generic UI Button`）。
        ClickPopUpButton(popup, "Generic UI Button");
        CheckTrue(okFired, "点确定**回调真的执行了**（不是只关窗）");
        Check(shell.Windows.openWindows.Count, 1, "弹窗关掉之后只剩 1 个窗");

        // ---------------- ⑤·a 🆕 2026-10-12（A416）：`ShowPopUp` **收编**到原版那扇 ----------------
        // 逐条判据 / 为什么必须与 3 个自检宿主**一次性落地**（它们原来按 `PromptPopup` 这个【类型】找窗）
        // → `资料/普查产出_1012/H5_DeckScene红与ShowPopUp收编.md` §A416。
        Section("A416：`ShowPopUp` 的宿主 = 原版 `PopUpGameWindow`（`MessagePopupWindow{,2Buttons}`）");
        {
            var hp1 = popup as PopUpGameWindow;
            CheckTrue(hp1 != null,
                      "★ A416：`ShowPopUp` 开出来的宿主 = **`PopUpGameWindow`**（原版 `popupWindowOneButton` /"
                    + " `popupWindowTwoButtons` 那两扇 prefab）—— 改回 `PromptPopup.Create` ⇒ 这条红");
            if (hp1 != null)
            {
                Check(hp1.MessageShown, "自检弹窗",
                      "★ …正文 = 调用方给的**明文**（明文不是术语键 ⇒ `Term()` 原样返回，11 个生产调用点一行都不用改）");
                CheckTrue(!hp1.TwoButtons, "★ …只给一颗钮 ⇒ **1 按钮版** prefab");
                Check(hp1.PrefabName, "MessagePopupWindow", "★ …建的就是那一版 prefab（原版 `popupWindowOneButton`）");
                Check(hp1.PrimaryShown, "确定", "★ …那颗钮 = `okText`");
                var b1 = FindChildIn(hp1.transform, "Generic UI Button");
                CheckTrue(b1 != null && !MenuDraw.WasAbsorb(b1),
                          "（前提）这颗真钮**不是**压暗层那颗吸收层（吸收层叫 `AbsorbHit`；混了它 ⇒ 上面那条点了个寂寞）");
            }

            // 两颗钮那一档：左 = `okText`/`onOk` · 右 = `cancelText`/`onCancel`（原版 `LiveButtons[0]` = `ButtonLeft`）
            bool fired2 = false, canceled2 = false;
            shell.Windows.ShowPopUp("A416·两钮·明文正文", "确定", () => fired2 = true, "取消", () => canceled2 = true);
            var hp2 = shell.Windows.popUpWindow as PopUpGameWindow;
            CheckTrue(hp2 != null, "★ …给了 `cancelText` ⇒ 宿主还是 `PopUpGameWindow`");
            if (hp2 != null)
            {
                CheckTrue(hp2.TwoButtons, "★ …给了**两颗**钮 ⇒ **2 按钮版** prefab（原版按「按钮数组长度 > 1」挑 prefab）");
                Check(hp2.PrefabName, "MessagePopupWindow2Buttons", "★ …建的就是那一版 prefab");
                Check(hp2.MessageShown, "A416·两钮·明文正文", "★ …正文 = 明文（`Term()` 对明文恒等）");
                Check(hp2.PrimaryShown, "确定", "★ …左钮 = `okText`（原版 `LiveButtons[0]` = `ButtonLeft`）");
                Check(hp2.SecondaryShown, "取消", "★ …右钮 = `cancelText`（两颗传反 ⇒ 本条与前一条一起红）");
                ClickPopUpButton(hp2, "ButtonRight");
                CheckTrue(canceled2, "★ …点**右钮** ⇒ `onCancel` 真的执行了（两颗钮的**回调**接反 / 右钮没挂 ⇒ 这条红）");
                CheckTrue(!hp2.gameObject.activeSelf, "★ …而且窗自己关掉了");
                // 再开一扇同样的、点**左钮**那一边（两条合起来 ⇒ 「哪颗钮调哪个回调」两边都钉住）
                fired2 = false;
                shell.Windows.ShowPopUp("A416·两钮·明文正文", "确定", () => fired2 = true, "取消", () => canceled2 = true);
                var hp2b = shell.Windows.popUpWindow as PopUpGameWindow;
                CheckTrue(hp2b != null && hp2b.TwoButtons, "（前提）重开一扇同样是 2 按钮版");
                ClickPopUpButton(hp2b, "ButtonLeft");
                CheckTrue(fired2, "★ …点左钮 ⇒ **回调执行了**（那颗钮没挂 `onClick` / 挂成吸收层 ⇒ 这条红）");
                CheckTrue(hp2b == null || !hp2b.gameObject.activeSelf,
                          "★ …而且**窗自己关掉了**（收编时漏掉那层 `Close()` 包装 ⇒ 这条红）");
                Check(shell.Windows.openWindows.Count, 1, "（收尾）关掉之后只剩底窗 1 个");
            }

            // 🔴 第二颗钮**只要给了 `cancelText` 就必须挂回调**：这一档**故意不给** `onCancel`
            //    （`onClick` 若是 null ⇒ 那颗钮点了什么都不发生 ⇒ 与旧宿主「点 Cancel 就关」不等价）。
            shell.Windows.ShowPopUp("A416·两钮·没给 onCancel", "确定", null, "取消");
            var hp3 = shell.Windows.popUpWindow as PopUpGameWindow;
            CheckTrue(hp3 != null && hp3.TwoButtons, "（前提）这一档也是 2 按钮版");
            ClickPopUpButton(hp3, "ButtonRight");
            CheckTrue(hp3 == null || !hp3.gameObject.activeSelf,
                      "★ …**没给 `onCancel` 也关得掉**（右钮的回调按「有 `onCancel` 才挂」写 ⇒ 那颗钮 `onClick` 是 null ⇒ 这条红）");
            Check(shell.Windows.openWindows.Count, 1, "（收尾）又只剩底窗 1 个");
        }

        // ---------------- ⑤b `PromptPopup`（照原版 `GenericPromptWindow` 重做的那个，正本 §七）
        Section("`PromptPopup`（原版 `GenericPromptWindow` prefab 规格）");
        // ⚠️ 2026-10-04 更正：这条示例文案原来写「暂无服务器：多人功能还没接（边界③）」—— **已过期**
        //    （P2P 联机 2026-09-26 就整条打通了，见 `资料/联机P2P_设计与交接.md`）。它只是自检的示例正文、
        //    **不参与任何判据**，但留着会误导 ⇒ 换成一句**不会过期**的事实句。后面那两条断言都是**现算**的
        //    （`Mathf.Max(PromptPopup.MsgMinH, msgLb.WorldH*108f) + BtnRowH`），换文案不影响它们。
        // 🔴 **2026-10-12（A416）改夹具**：本节验的是 **`PromptPopup` 自己的版面**（照原版 `GenericPromptWindow`
        //   搭的那扇），而它**不再是** `ShowPopUp` 的宿主（那条链已收编到 `MessagePopupWindow`，见上面 ⑤·a）
        //   ⇒ **直建一扇来验**。⛔ 别再写成「`ShowPopUp` 一开、顺手把本节的窗也开出来」——收编之后那样写
        //   `pp` 恒为 null，本节那 15 条会**整段静默跳过**（本工程最怕的那种「绿得没道理」）。
        var pp = PromptPopup.Create(shell.Windows, "（自检示例文案：本窗只负责排版，正文由调用方给。）", "知道了", null, null, null);
        shell.Windows.OpenWindow(pp);
        CheckTrue(pp != null, "`PromptPopup` 建出来了（⚠️ 它**不再是** `ShowPopUp` 的宿主 —— 原版那条走 `MessagePopupWindow`，见 A416）");
        if (pp != null)
        {
            // 🆕 A17：`GenericPromptWindow` 的 `Ok`/`Cancel` 原版是 SpriteSwap（`40K_button` → `_hover`；
            //    普查那 5 块表漏了这扇窗，接线时按 prefab 反查补上的）
            CheckHoverSwap(pp.transform, "提示窗");
            Check(pp.type, WindowType.Popup, "`type` = 1 Popup（实证）");
            Check(pp.placement, WindowsPlacement.Popup, "`windowsPlacement` = 15 Popup（实证）");
            Check(pp.closeOnEsc, false, "`closeOnESC` = 0（**原版这条不是 ESC 关** —— 与上一版自建弹窗相反）");
            Check(pp.TwoButtons, false, "没给 cancel ⇒ 单按钮版（⚠️ 我们挑的，见 `PromptPopup` 文件头）");
            // 面板高 = `MessageText` 的实际渲染高（**下限 100**）+ `Buttons` 的 110
            // （原版是 VLG+CSF 运行时算，序列化里只有 `Window sz=(900,0)`；我们按同一套语义自己算 —— 见文件头「我们挑的」①）
            CheckTrue(pp.PanelH >= PromptPopup.MsgMinH + PromptPopup.BtnRowH - 0.5f,
                      $"面板高 ≥ 文案最小高 100 + 按钮行 110（实得 {pp.PanelH:F2}）");
            var msgNode = FindChildIn(pp.transform, "MessageText");
            var msgLb = msgNode != null ? msgNode.GetComponentInChildren<Label>() : null;
            if (msgLb != null)
                CheckNear(pp.PanelH, Mathf.Max(PromptPopup.MsgMinH, msgLb.WorldH * 108f) + PromptPopup.BtnRowH,
                          0.5f, "面板高 = max(文案**实测**渲染高, 100) + 110");
            var shade = FindChildIn(pp.transform, "Menu Dark Background");
            CheckTrue(shade != null, "`Menu Dark Background` 建了（无 sprite 的纯色矩形，色 α0.7725）");
            CheckTrue(FindChildIn(pp.transform, "Generic Popup Background") != null, "`Generic Popup Background` 建了（`40k_popup` 九宫格）");
            // 🆕 2026-10-05：**底板九宫格【渲出来】的宽高**（原来只断「建了」⇒ `CreateNineSlice` 第 7/8 实参
            //   写反照样全绿。实据：那条 bug 2026-10-05 才修，见 `PromptPopup.cs` 第 3 步那段注释）。
            // 🔴 期望值**不从被测实现里读**（读 `PromptPopup.PanelW…` 再算一遍 = 自证：常量被改坏时这条会跟着变）：
            //   ① **宽** = 原版 prefab 的两个字面量 —— `Window sz=(900,·)` + `Generic Popup Background sz=(100,100)`
            //      且四边锚点全 stretch ⇒ 左右各外扩 50 ⇒ **900 + 2×50 = 1000 px**（正本 §七 的节点表）；
            //   ② **高** = 面板高 + 上下各 50；`pp.PanelH` 唯一取自实现的那一项**已被上面那条独立断言钉住**
            //      （`ShellScene.cs:620`：`PanelH` = `MessageText` **实测**渲染高与 100 取大 + 110），不是拿本条算式反推。
            // ⚠️ 量的是**九块的并集**（`CreateNineSlice` 建的是「根 + 9 块」，只取第一块会量成某个角块 ——
            //   `Editor/SettingsScene.cs:71` 那条注释记的「弹窗底量成 182×173」当场踩的就是这个）。
            // ⚠️ 换算沿用本文件 `PiecesOutsideClip` 的口径（`LayoutSpace.ToPixel` + `WorldW/H × K`，别再乘 108）；
            //   它假设窗口那棵树**没有缩放** —— `WindowsManager.AttachToAnchor` 把窗口摆成 `localScale = one`
            //   （`Shell/WindowsManager.cs:942-958`，`localScale = Vector3.one` 在 `:958`）。
            //   🔴 **2026-10-12 就地订正（铁律 5 · A430）**：这一段原来还写着「`extraScaleSmallScreen` 在我们这套里
            //   **没有消费者**」+「小屏缩放器**没实现**」（引的 `:292-300` / `:76-82` 两处行号也已漂）——
            //   **两条都不成立了**：A165（2026-10-06）把缩放器做了出来（`Shell/TransformScalerBySmallScreenUI.cs`；
            //   开关 = 同文件的 `SmallScreenUI`，**出厂关**），它的消费者 = `GameWindow.ApplySmallScreenScale()`
            //   （`Shell/WindowsManager.cs:507`，由 `OpenByState` 在 `:466` 调 —— 就是「播音→激活→`Open()`」之后紧接的那句）。
            //   ✅ **本条断言的结论照旧成立**（本窗那棵树缩放恒为 1），只是**理由换了**（两条各自独立成立）：
            //   ① 本窗 `extraScaleSmallScreen = 1.0`（`PromptPopup.cs:81`）= 原版「**不覆盖**」语义 ⇒
            //      `ApplySmallScreenScale` 走 `Initialize()` 那一支（`Shell/WindowsManager.cs:515-518`）；
            //   ② 本窗是**代码建**的 GO（`PromptPopup.cs:76`），根上**没有**烤 `menuScale` 的缩放器 ⇒
            //      `menuScale` 保持 ctor 的 1.0 ⇒ `enabled = false`（`Initialize` 用**裸 `!=`**，见该文件头 ②）
            //   ⇒ **小屏开关开着也不放大**。
            {
                const float OrigPanelW = 900f, OrigBgPad = 50f;      // 原版字面量，**故意不读** `PromptPopup` 的常量
                CheckNear(PromptPopup.PanelW, OrigPanelW, 0.01f, "`PanelW` 仍是原版 `Window sz=(900,0)` 的 900");
                CheckNear(PromptPopup.BgPad, OrigBgPad, 0.01f,
                          "`BgPad` 仍是原版 `Generic Popup Background sz=(100,100)` 每边外扩的 50");
                float wantW = OrigPanelW + OrigBgPad * 2f;                                 // 1000
                float wantH = pp.PanelH + OrigBgPad * 2f;                                  // 面板高 + 100
                var bgNode = FindChildIn(pp.transform, "Generic Popup Background");
                var bgQuads = bgNode != null ? bgNode.GetComponentsInChildren<ImageQuad>(true) : null;
                CheckTrue(bgQuads != null && bgQuads.Length > 0,
                          "`Generic Popup Background` 底下**真有块**（九宫格建出了 `ImageQuad`，不是空节点）");
                if (bgQuads != null && bgQuads.Length > 0)
                {
                    const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;      // 画布 px / 世界单位
                    float lx = float.MaxValue, ty = float.MaxValue, rx = float.MinValue, by = float.MinValue;
                    for (int i = 0; i < bgQuads.Length; i++)
                    {
                        var q = bgQuads[i];
                        if (q == null || !q.gameObject.activeInHierarchy) continue;
                        var c = LayoutSpace.ToPixel(q.transform.position);
                        float hw = q.WorldW * K * 0.5f, hh = q.WorldH * K * 0.5f;
                        lx = Mathf.Min(lx, c.x - hw); rx = Mathf.Max(rx, c.x + hw);
                        ty = Mathf.Min(ty, c.y - hh); by = Mathf.Max(by, c.y + hh);
                    }
                    // 夹具**必须非方形**：宽 1000 与 高 PanelH+100 差 > 100px，两个方向都分开比 ⇒ 写反必红。
                    // （`PanelH` 有朝一日真到 900 时这条会响 —— 那是提醒换一条更长的示例文案，不是放宽断言。）
                    CheckTrue(Mathf.Abs(wantW - wantH) > 100f,
                              $"夹具非方形（宽 {wantW:F0} vs 高 {wantH:F0}，差 > 100px ⇒ 宽高写反必红）");
                    CheckNear(rx - lx, wantW, 1.5f,
                              "底板九宫格并集**宽** = 原版 900 + 左右各 50（写反 ⇒ 这里量到的是「面板高 + 100」）");
                    CheckNear(by - ty, wantH, 1.5f,
                              "底板九宫格并集**高** = 面板高 + 上下各 50（写反 ⇒ 这里量到的是 1000）");
                }
            }
            CheckTrue(FindChildIn(pp.transform, "Background fill") != null, "`Background fill` 建了（`40k_popup_texture` 平铺 64 一格）");
            CheckTrue(FindChildIn(pp.transform, "MessageText") != null, "`MessageText` 建了");
            CheckTrue(FindChildIn(pp.transform, "OkButton") != null, "`OkButton` 建了（`40K_button`，色 (0.3686,0.8941,0.5874,1)）");
            CheckTrue(FindChildIn(pp.transform, "CancelButton") == null, "单按钮版**没有** `CancelButton`");
            var okBtn = FindChildIn(pp.transform, "OkButton");
            var okQ = okBtn != null ? okBtn.GetComponentInChildren<ImageQuad>() : null;
            if (okQ != null)
                CheckNear(okQ.WorldW * 108f / (okQ.WorldH * 108f), 489f / 107f, 0.02f,
                          "`OkButton` 渲出来的宽高比 = `40K_button` 源图 489/107（`PreserveAspect`）");
        }
        Shoot("04_原版提示窗.png");
        shell.Windows.CloseAllWindows();
        Check(shell.Windows.openWindows.Count, 0, "`CloseAllWindows()` 清空");
        CheckTrue(shell.Windows.currentWindow == null, "`currentWindow` 也清空（不留悬挂引用）");
        Object.DestroyImmediate(winGo);

        // ---------------- ⑤·b 共用件：`RectMask2D.m_Padding`（A9/A15 尾巴 · **2026-10-07 A140② 收口**）
        //
        // 🔴 **判据 = 本地 UGUI 源码（两个文件，⛔ 别只 grep 一个）**：
        //   · **射线**：`RectMask2D.IsRaycastLocationValid`（`Runtime/UGUI/UI/Core/RectMask2D.cs:178-185`）
        //     → `RectangleContainsScreenPoint(rectTransform, sp, eventCamera, m_Padding)`；
        //   · **渲染**：`RectMask2D.PerformClipping()`（同文件 `:205`）→ `:226`
        //     `Culling/Clipping.FindCullAndClipWorldRect`（`Runtime/UGUI/UI/Core/Culling/Clipping.cs:17`）——
        //     该函数 `:26-30` 拿 `padding` **内缩**裁剪矩形（`:47` 的 `validRect = xMax > xMin && yMax > yMin`）。
        //   🔴 **2026-10-07 就地订正（铁律 5）**：本节原文写「该字段**全文件只用在一处**（`IsRaycastLocationValid`）
        //     ⇒ 它**只改「点不点得到」，不改「画到哪儿」**（渲染那一面 `PerformClipping` 压根不读它）」——
        //     **两句都错**，错因 = **只 grep 了 `RectMask2D.cs`**（渲染那一面的算式在另一个文件）。
        //     而且那处口误**盖住了一条真偏离**：带非零 padding 的 mask，我们的**渲染裁切比原版宽一个 padding**
        //     = **A140②**（下面 ③④ 就是它的断言）。
        //   ⇒ 符号（**正 = 缩小** / 负 = 扩大）**已坐实**，`[TODO-verify]` 已摘。
        //   期望值全部是**原版 mask 的实读字面量**（全量表 `d:/4/_tmp_view/q1_rm2d.txt`），不是我们自己的常量。
        Section("共用件：`RectMask2D.m_Padding`（**渲染 + 命中两副面孔都吃它** · 判据 = UGUI `Culling/Clipping.cs`）");
        {
            // ① 正值 = 缩小：锻造轨道 `Forge Tab/Rewards Scroll View/Viewport` 的 `m_Padding` 实读 = (10,0,0,0)
            var pr0 = new PxRect(100f, 200f, 300f, 500f);
            var pad1 = MenuDraw.PaddedHitRect(pr0, new Vector4(10f, 0f, 0f, 0f));
            CheckNear(pad1.x1, 110f, 0.001f, "`m_Padding=(10,0,0,0)`（锻造轨道原版值）⇒ 命中区**左边收进 10px**");
            CheckTrue(Mathf.Abs(pad1.x2 - 300f) < 0.001f && Mathf.Abs(pad1.y1 - 200f) < 0.001f
                      && Mathf.Abs(pad1.y2 - 500f) < 0.001f, "…其余三边**一动不动**");
            // ② 负值 = 扩大：原版**输入框 `/Text Area` 那一族**的 `m_Padding` 实读 = (−8,−5,−8,−5)（(L,B,R,T)）
            //    🔴 **2026-10-07 就地订正（铁律 5）**：这两条原来标的是「**战役轨道 / 战役阵营条**原版 `m_Padding`
            //    实读**」—— **错**。`MenuDraw` 的逐处表写明该值 ×38 的**路径末尾全是 `/Text Area`**（输入框/文本框
            //    那一族），**没有一处是 `Viewport` / `Scroll Rect`**；而战役轨道我们生产写的就是 `Vector4.zero`
            //    （`Shell/CampaignTab.cs`）。**这一条本身是纯函数单测（合成矩形）⇒ 行为一字未变**，改的只是标注。
            var pad2 = MenuDraw.PaddedHitRect(pr0, new Vector4(-8f, -5f, -8f, -5f));
            CheckNear(pad2.x1, 92f, 0.001f, "`m_Padding=(−8,−5,−8,−5)`（原版**输入框**那一族的值）⇒ 左右各**外扩 8**");
            CheckNear(pad2.y1, 195f, 0.001f, "…上边外扩 5（UNITY 的 `w`=Top）");
            CheckNear(pad2.y2, 505f, 0.001f, "…下边外扩 5（`y`=Bottom；`PxRect` 是**y 向下**，所以落在 y2 上）");
            // ③ 端到端：`MenuDraw.Hit` 真的吃这一份（拿掉 pad 的转发它就红）—— 按真实用法带一个 `clip`
            //    🔴 **2026-10-08（A188）就地订正（铁律 5）**：原来那两个期望值是 **85 / 76**，钉的是
            //    「pad 缩**命中区自己**那个矩形」（= `(R − pad) ∩ V`，**旧模型**）。
            //    按 uGUI 判据（`GraphicRaycaster.cs:327` ∧ `RectMask2D.cs:178-184`）=
            //    **`R ∩ (V − pad)`**：`PaddedClip((0,0,200,200),(10,20,5,4))` = `(10,4,195,180)`，
            //    再与 `r = (0,0,100,100)` 求交 ⇒ `(10,4,100,100)` ⇒ **宽 90 / 高 96**。
            //    ⛔ 别把这一段删掉 —— 它是「pad 真的进了命中那条路」的唯一端到端证据。
            var padGo = new GameObject("PaddingProbe");
            var padHit = MenuDraw.Hit(padGo.transform, "PadHit", new PxRect(0f, 0f, 100f, 100f), 3000,
                                      () => { }, null, null, null, null, new PxRect(0f, 0f, 200f, 200f),
                                      new Vector4(10f, 20f, 5f, 4f));
            var padQ = padHit != null ? padHit.GetComponentInChildren<ImageQuad>() : null;
            CheckTrue(padQ != null, "带 pad 的命中区**建出来了**（`PointerLayer` 只认 quad —— 裸节点点不动）");
            if (padQ != null)
            {
                CheckNear(padQ.WorldW * 108f, 90f, 0.5f,
                          "命中 quad 宽 = `r` 与 `V − pad` 求交后的宽 = **90**（`V − pad` = `(10,4,195,180)` ⇒ 左沿 10；"
                          + "右沿由 `r` 自己说了算 = 100）");
                CheckNear(padQ.WorldH * 108f, 96f, 0.5f,
                          "命中 quad 高 = **96**（`V − pad` 上沿 4、下沿由 `r` 自己说了算 = 100）");
            }
            Object.DestroyImmediate(padGo);

            // ============================================================ ③ 🆕 2026-10-07（A140②）**渲染那一面**
            // 判据 = 上面那条 UGUI 渲染路（`Clipping.FindCullAndClipWorldRect`）⇒ **渲染裁切框也要内缩**。
            // ① 纯函数那一层（`MenuDraw.PaddedClip`）：
            var cl0 = new PxRect(1000f, 200f, 1600f, 800f);
            var clPad = MenuDraw.PaddedClip(cl0, new Vector4(10f, 0f, 0f, 0f));
            CheckTrue(clPad.HasValue, "`PaddedClip` 遇到**非零** pad 时照常返回（只有「pad 比框还大」那一支才返回 null）");
            if (clPad.HasValue)
            {
                CheckNear(clPad.Value.x1, 1010f, 0.001f, "★ 渲染裁切框：`m_Padding=(10,0,0,0)` ⇒ **左边收进 10px**");
                CheckNear(clPad.Value.x2, 1600f, 0.001f, "…右边缘一动不动（`pad.z` = Right = 0）");
                CheckNear(clPad.Value.y1, 200f, 0.001f, "…上边缘一动不动（`pad.w` = Top = 0）");
                CheckNear(clPad.Value.y2, 800f, 0.001f, "…下边缘一动不动（`pad.y` = Bottom = 0）");
            }
            CheckTrue(MenuDraw.PaddedClip(cl0, Vector4.zero).HasValue
                      && MenuDraw.SameRect(MenuDraw.PaddedClip(cl0, Vector4.zero).Value, cl0),
                      "`pad` 全 0 ⇒ 裁切框**逐字段不变**（= 本壳绝大多数窗口：实读都是 `(0,0,0,0)`）");
            CheckTrue(!MenuDraw.PaddedClip(null, new Vector4(10f, 0f, 0f, 0f)).HasValue,
                      "无裁切框 ⇒ 还是无裁切框（⛔ 别为了设 padding 就凭空造一个框出来）");

            // ② **端到端**：`MenuWindowBase` 的**渲染包装**真的把 pad 传下去了没有。
            //    取一扇真窗（`RewardsWindow : MainMenuSubmenuWindow : MenuWindowBase` —— 生产上唯一带非零 pad 的
            //    `ForgeTab` 就是它的页），把 `Clip`/`ClipPad` 设成**锻造轨道那一组**，画一张
            //    **横跨裁切框左沿 50px** 的图：它渲出来的左边缘必须停在 `框左 + 10`。
            //    ⛔ 期望值写**原版字面量**（10 / 1010），不是我们的常量；⛔ 两条反向状态都断（弱断言分不出两态）。
            var padWin = RewardsWindow.Create(shell.Windows);
            var padRoot = new GameObject("PadRenderProbe").transform;
            padRoot.SetParent(padWin.transform, false);
            padWin.Clip = new PxRect(1000f, 200f, 1600f, 800f);
            padWin.ClipPad = new Vector4(10f, 0f, 0f, 0f);      // 原版 `Forge Tab/Rewards Scroll View/Viewport` 实读值
            CheckTrue(padWin.RenderClip.HasValue
                      && MenuDraw.SameRect(padWin.RenderClip.Value, new PxRect(1010f, 200f, 1600f, 800f)),
                      "`MenuWindowBase.RenderClip` = `Clip` 按 `ClipPad` 内缩（左沿 **1010** = 1000 + 10）");
            var straddle = new PxRect(950f, 300f, 1050f, 400f);  // 左半在框外 50px
            var q1 = padWin.Rect(padRoot, null, straddle, "PadRender", 3000);
            CheckTrue(q1 != null, "（前提）横跨框沿的探针图建出来了（`art=null` ⇒ 纯色块，不需要美术资源）");
            if (q1 != null)
            {
                float a, b, c, d;
                bool ok = QuadPxRect(q1, out a, out b, out c, out d);
                CheckTrue(ok, "（前提）量得到它的**渲染**矩形");
                if (ok)
                {
                    CheckNear(a, 1010f, 0.5f, "★ **渲染裁切吃了 `m_Padding`**：横跨框左沿 50px 的图停在 **1010**"
                                             + "（拿掉 `RenderClip` ⇒ 回 1000 ⇒ A140② 那个真偏离复活）");
                    CheckNear(c, 1050f, 0.5f, "…而 `pad.z`(Right) = 0 ⇒ 右边缘**一动不动**（1050）");
                    CheckNear(b, 300f, 0.5f, "…上边缘不动（`pad.w` = Top = 0）");
                    CheckNear(d, 400f, 0.5f, "…下边缘不动（`pad.y` = Bottom = 0）");
                }
            }
            padWin.ClipPad = Vector4.zero;                       // 🔴 反向那一态：pad 清 0
            var q2 = padWin.Rect(padRoot, null, straddle, "PadRender2", 3000);
            CheckTrue(q2 != null, "（前提）清掉 pad 之后同一张图也建出来了");
            if (q2 != null)
            {
                float a, b, c, d;
                if (QuadPxRect(q2, out a, out b, out c, out d))
                    CheckNear(a, 1000f, 0.5f, "★ 反向：`ClipPad` 清 0 ⇒ **同一张图**左边缘回到框沿 **1000**"
                                             + "（两条合起来才证明这 10px 位移**真由 pad 引起**）");
            }
            Object.DestroyImmediate(padRoot.gameObject);
            Object.DestroyImmediate(padWin.gameObject);
        }

        // ---------------- ⑤·c 共用件：软边切出来的子块（A38③）
        //
        // 🔴 **缺口**（Q1 审查顺手发现）：软边是「按渐隐带内沿把这个 quad 切开」的（`MenuDraw.ApplySoftEdges`）
        //   ⇒ 切出来的子块是**独立 quad**，父件之后 `SetTint`/改几何时它们**不跟**（静默：只有边带那一条不对）。
        //   修法：`ImageQuad.SetTint` 刷登记的软边子块；`SetAspect`/`SetWorldHeight` ⇒ 回调 `MenuDraw.ReapplySoftEdges` 重切。
        Section("共用件：软边子块**跟随**父件的 `SetTint` 与几何改动（A38③）");
        {
            var softGo = new GameObject("SoftProbe");
            var softClip = new PxRect(0f, 0f, 200f, 200f);     // 带宽 25 落在框内 ⇒ 切 3×3
            var softQ = MenuDraw.Rect(softGo.transform, CardArt.Solid(), softClip, "SoftProbe", 3000,
                                      null, false, softClip, new Vector2(25f, 25f));
            CheckTrue(softQ != null, "软边探针建出来了（`CardArt.Solid()` 取得到 ⇒ 纯色件不是 null）");
            if (softQ != null)
            {
                Check(softQ.SoftEdgeKidCount, 8, "200×200 的框 + 软边 25 ⇒ 切成 3×3：主格 1 + **子块 8**");
                // 🔴 **2026-10-04（首跑红了，就地订正）：期望值不能读「软边宿主自己的 `UvRect`」** ——
                //   宿主是**带着软边建的**，`MenuDraw.Rect` 在建的时候就把它切成主格那一份了
                //   （首跑实测：读到 0.5625 = 主格的 uv，而所有块的面积和是 1.000 ⇒ 报「1.000 ≈ 0.563」）。
                //   ⇒ 期望值改从**同几何、但不带软边**的另一颗 quad 取（它采的就是「整张图」那份 uv），
                //   **独立于软边那条实现路径**。
                var plainGo = new GameObject("SoftProbePlain");
                var plainQ = MenuDraw.Rect(plainGo.transform, CardArt.Solid(), softClip, "SoftProbePlain", 3000);
                float uvFull = plainQ != null ? plainQ.UvRect.width * plainQ.UvRect.height : -1f;
                Object.DestroyImmediate(plainGo);
                CheckTrue(uvFull > 0f, "（前提）同几何的无软边对照 quad 量得到 uv 面积");
                var wantTint = new Color(0.5f, 0.25f, 0.1f, 0.8f);
                softQ.SetTint(wantTint);
                Check(PiecesWithTint(softGo, wantTint, 0.001f), 9,
                      "★ 父件 `SetTint` ⇒ **9 块（主格 + 8 子块）全部换到同一个色**（改回不刷子块 ⇒ 这里只数得到 1）");

                int rebuilt0 = MenuDraw.SoftEdgeRebuilds;
                softQ.SetWorldHeight(LayoutSpace.Px(300f));    // 几何一变 ⇒ 必须重切
                CheckTrue(MenuDraw.SoftEdgeRebuilds > rebuilt0,
                          "★ 父件改几何（`SetWorldHeight`）⇒ **真的重切了**（`MenuDraw.SoftEdgeRebuilds` 涨了）");
                CheckTrue(softQ.SoftEdgeKidCount > 0, "重切之后**又切出了子块**（不是清空了事）");
                Check(PiecesWithTint(softGo, wantTint, 0.001f), 1 + softQ.SoftEdgeKidCount,
                      "★ 重切出来的新子块**抄的是父件当前的色**（不是出厂白）");
                // 🔴 **这一条与「重切用哪条映射」无关**（那条是**我们挑的**，见 `ReapplySoftEdges`）：
                //    不管怎么映射，**切出来的每一块都必须落在裁切框里**（原版 `RectMask2D` 一视同仁）。
                int outside = PiecesOutsideClip(softGo, softClip, 0.6f);
                Check(outside, 0, $"★ 重切之后**没有一块越出裁切框**（越界的：{outside} 块）"
                                  + " —— 变大了却不重新裁，边带就会画到框外");
                // 🆕 2026-10-04（F1 修完补的宿主断言；执行代理 F 提供、**R-F 审查后订正过一轮**）：
                //   软边的**每一次重切**都要从**整张图的 uv** 重新推导 —— 老的写法是 `var uv0 = q.UvRect;`
                //   （拿宿主**当前**那份 uv 再缩一次）⇒ 每切一次采样区再乘一次（0.75 → 0.5625 → 0.4219 …），
                //   整棵树采样的是原图一个**越缩越小**的子矩形（画面被放大/裁掉，**宿主与子块自洽所以看不出缝**）。
                // 🔴 **R-F 抓到的关键**：期望值**不能**用 `softQ.UvRect`（那还是实现自己写的那份）——
                //   每一块的 uv 都是 `PlaceCell` 从同一个 `uv0` 切出来的 ⇒ 面积**望远镜求和恒等**、四种情形都不动，
                //   是**空转断言**。⇒ 期望值取**出生时**抓的那一份（上面 `uv0AtBirth`），面积必须守恒：
                //   `uv0` 换回 `q.UvRect` ⇒ 得 0.5625 而期望 1.0 ⇒ **这里立刻红**。
                float uvAreaSum = 0f;
                foreach (var q in softGo.GetComponentsInChildren<ImageQuad>(true))
                {
                    if (q == null) continue;
                    var ur = q.UvRect;
                    uvAreaSum += ur.width * ur.height;
                }
                CheckNear(uvAreaSum, uvFull, 1e-4f,
                          "★ 软边树（宿主 + 全部子块）的 uv **面积和 == 整张图那份 uv 的面积**"
                          + " —— 把 `uv0` 换回 `q.UvRect` 这里立刻红（每重切一次再缩一次）");
                // ⚠️ **这一条是次要信号、别当成上面那条的替代**：`MenuDraw.CheckSoftEdgeUv` 的期望值是从
                //    本趟传入的 `uv0` 算的 ⇒ 它**抓不到**「`uv0` 传错成 `q.UvRect`」这一档（同义反复）；
                //    它有牙的只有「**宿主 uv 不是本趟写的那份**」那一档（`ReapplySoftEdges` 里不切分支的洞②）。
                // ⛔ **别把上一句改成「这里立刻红」** —— R-F 复核过：改回旧写法时这个计数**不动**。
                Check(MenuDraw.SoftEdgeUvDrifts, 0,
                      "软边树建完之后，宿主 uv 必须是**本趟写进去的那一份**（`SoftEdgeUvDrifts` 计数）");
            }
            Object.DestroyImmediate(softGo);
        }

        // ---------------- ⑤·c-2 🆕 A58-R7：软边**重切落进「整块不切」**那条支路（此前**零覆盖**）
        //
        // 🔴 **为什么要单开一条**（F 审查 R7）：`ApplySoftEdges` 的「整块不切」那一支里有两件事
        //   **只在重切时**才做 —— `PlaceCell(q, vis, vis, uv0)`（把宿主摆回**整个** `vis` + 把 uv 复位成
        //   「整张图」那一份）。而此前**所有**软边探针（含上面 ⑤·c）算下来**全落在【切开】分支**
        //   （框 200 + 带 25 ⇒ 两个断点 25/175 严格落在块内）⇒ 那两件事一次都没被走到，只有人工推演。
        //   ⚠️ 上面 ⑤·c 的 `SetWorldHeight(Px(300))` **不是**反例：宿主放大后那条映射算出来的 `vis`
        //   仍然横跨两个断点 ⇒ **还是切开分支**（`SoftEdgeRebuilds` 涨了、子块也重切了，但「不切」依旧零覆盖）。
        // 怎么逼它落进那一支：`SoftCuts` **只收「严格落在 `[vis.x1,vis.x2]` 内部」的断点** ⇒
        //   反过来把宿主**缩小**到视口正中一小块，「整张图」映射后就整个落在两个内沿**之间** ⇒ 一个断点都不收 ⇒ 不切。
        //   算式（本段的数）：框 200×200、带 25 ⇒ 内沿 25 / 175；第一刀 3×3，主格 = (25,25,175,175)（中心 100,100）；
        //   再把宿主缩到 **15px** ⇒ `sx = sy = 15/150 = 0.1` ⇒ 映射后
        //   `vis = (100+(0−100)×0.1, …, 100+(200−100)×0.1, …) = (90,90,110,110)`
        //   ⇒ 25 < 90、175 > 110，**两个断点都在 90..110 之外** ✅
        Section("软边**重切落进「整块不切」**那条支路（A58-R7 —— 此前零覆盖）");
        {
            var r7Go = new GameObject("SoftNoCutProbe");
            var r7Clip = new PxRect(0f, 0f, 200f, 200f);
            var r7Soft = new Vector2(25f, 25f);
            var r7 = MenuDraw.Rect(r7Go.transform, CardArt.Solid(), r7Clip, "NoCut", 3000,
                                   null, false, r7Clip, r7Soft);
            CheckTrue(r7 != null, "探针建出来了");
            if (r7 != null)
            {
                Check(r7.SoftEdgeKidCount, 8,
                      "前置：第一刀是**切开**（3×3 ⇒ 主格 1 + 子块 8）—— 第一刀就不切的话，下面测不到那条支路");
                // 「整张图那份 uv」的期望值：同几何、**不带软边**的另一颗（独立于软边那条实现路径）
                var r7PlainGo = new GameObject("SoftNoCutPlain");
                var r7Plain = MenuDraw.Rect(r7PlainGo.transform, CardArt.Solid(), r7Clip, "NoCutPlain", 3000);
                float r7UvFull = r7Plain != null ? r7Plain.UvRect.width * r7Plain.UvRect.height : -1f;
                Object.DestroyImmediate(r7PlainGo);
                CheckTrue(r7UvFull > 0f, "（前提）对照 quad 量得到「整张图」那份 uv 面积");

                int r7Reb0 = MenuDraw.SoftEdgeRebuilds;
                r7.SetWorldHeight(LayoutSpace.Px(15f));      // 15px ⇒ 重切落进「不切」（算式见本节头）
                CheckTrue(MenuDraw.SoftEdgeRebuilds > r7Reb0,
                          "★ 改几何 ⇒ 重切那条路**真的跑了**（`MenuDraw.SoftEdgeRebuilds` 涨了）");
                Check(r7.SoftEdgeKidCount, 0,
                      "★★ 这一刀落进**「整块不切」支路**（一块都不切 ⇒ 子块 0）—— 它是下面三条的**前提**；"
                      + "子块 > 0 就说明又走回切开分支了，下面那几条等于没验");
                CheckNear(r7.WorldH * 108f, 20f, 0.5f,
                          "★★ 宿主**被摆回整个 `vis`**（高 = 映射后那块 (90,90,110,110) 的 **20px**）"
                          + " —— 拿掉 `ApplySoftEdges` 里那一句 `PlaceCell`，它停在调用方给的 **15**");
                float r7Sum = 0f;
                foreach (var q in r7Go.GetComponentsInChildren<ImageQuad>(true))
                {
                    if (q == null) continue;
                    var ur = q.UvRect;
                    r7Sum += ur.width * ur.height;
                }
                CheckNear(r7Sum, r7UvFull, 1e-4f,
                          "★★ 宿主 uv **复位成「整张图」那一份**（拿掉那一句它停在上一刀的主格 uv ⇒"
                          + " **0.5625** vs 1.0）");
                Check(PiecesOutsideClip(r7Go, r7Clip, 0.6f), 0, "…而且一块都没有越出裁切框");
                Check(MenuDraw.SoftEdgeUvDrifts, 0,
                      "…`SoftEdgeUvDrifts` 也没涨（它就是这条支路唯一的既有信号：宿主 uv 不是本趟写的那一份）");
            }
            Object.DestroyImmediate(r7Go);
        }

        // ---------------- ⑤·d 共用件：文字裁切要扛得住之后的**重排**（A38②）
        //
        // 🔴 **缺口**：`MenuDraw.ClipText` 是**建的时候**裁一刀，而 `Label.SetText` / 改字号带来的重排
        //   会让 TMP **重算 mesh** ⇒ 那一刀被抹掉、压在视口边上的字**又画出去了**（静默）。
        //   修法：`ClippedTextGuard` 订 TMP 自己的「文字已重排」事件（`TMPro_EventManager.TEXT_CHANGED_EVENT`，
        //   `TextMeshPro.cs:5047-5063`），收到就照原参数**再裁一刀**。
        Section("共用件：文字裁切扛得住之后的重排（A38② —— 订 TMP 的 `TEXT_CHANGED_EVENT`）");
        {
            var txtGo = new GameObject("ClipTextProbe");
            var live = new PxRect(0f, 0f, 120f, 40f);          // 只有 120px 宽，下面那句字必然越界
            var lb = MenuDraw.Text(txtGo.transform, live, "WWWW WWWW WWWW WWWW WWWW", Color.white, "Probe", 30f, 3000);
            CheckTrue(lb != null, "文字探针建出来了");
            if (lb != null)
            {
                bool moved = MenuDraw.ClipText(lb, live, Vector2.zero);
                CheckTrue(moved, "`ClipText` 当场**真的切了**（否则下面那条等于没验）");
                CheckTrue(lb.GetComponent<ClippedTextGuard>() != null,
                          "★ 裁过的字上**挂着 `ClippedTextGuard`**（= 之后每一次重排都会自动重裁）");
                var tmp = lb.GetComponentInChildren<TMPro.TextMeshPro>();
                CheckTrue(tmp != null, "这一段字走的是 TMP 那条后端（守卫生效的那条）");
                if (tmp != null)
                {
                    int n0 = MenuDraw.TextClipReapplied;
                    tmp.ForceMeshUpdate();                     // = `SetText` 之后 TMP 会做的那件事（重排）
                    CheckTrue(MenuDraw.TextClipReapplied > n0,
                              $"★ 重排之后**自动重裁了一次**（计数 {n0} → {MenuDraw.TextClipReapplied}）"
                              + " —— 拿掉守卫这里就不会涨");
                }
            }
            Object.DestroyImmediate(txtGo);
        }

        // ---------------- ⑤·d-3 🆕 2026-10-12（A386）：**「摆完再补那一刀」那条路**的牙口
        //
        // 🔴 **缺口**（`资料/普查产出_1012/S4_外壳共用件_开账现核.md` §二·2「顺手发现」）：`MenuDraw.TextReclipAfterPlace`
        //    （A225-② 加的计数器，`Shell/MenuDraw.cs:980` 声明、`ClippedTextGuard.Reclip()` `:2289` 自增）
        //    全仓 **0 个读者** ⇒ A206 那条「裁的那一刀要落在**文字真正被画的位置**上」的路**没有直接断言**
        //    （可见面只由 `MainMenuScene.Run` 的「卡组名也吃软边」间接守着）。
        //    ⚠️ **它与上一节（`TextClipReapplied`）是两条路，别混**：
        //      · 上一节 = **事件路**：TMP 的 `ON_TEXT_CHANGED`（只在 `ForceMeshUpdate()` **里面**发）
        //        ⇒ 那一刻 TMP 子节点**还停在旧位置**（A206 的根因）；
        //      · 本节 = **摆完再补那一刀**：`Battle/Label.cs` 的 `RefreshBounds()` **末句** `guard.Reclip()`
        //        （`Label.cs:746-747`），而那是**每一条定版面的路的末句**（`SetText` / `SetAutoFitBox` /
        //        `ForceRelayout` / 建标签…）⇒ 这一刀才落在文字**真正被画**的地方。
        // 🔴 **判据 = 计数器【差分】**（读前 / 读后），⛔ 不拿实现里的期望值当判据（那是自证）。
        //    🔴 **改坏法**：把 `Battle/Label.cs` 的 `RefreshBounds()` 末句那两句（`GetComponent<ClippedTextGuard>()`
        //      + `guard.Reclip()`）删掉 ⇒ **本条立刻红**（计数不涨）；而上一节那条（事件路）**照样绿**
        //      ⇒ 两条分得开（这就是「补一条有牙口的断言」的意思）。
        Section("共用件：`Label` **摆完版面又补了一刀**（A386 —— `TextReclipAfterPlace` 的计数差分）");
        {
            var r8go = new GameObject("ReclipAfterPlaceProbe");
            var r8live = new PxRect(0f, 0f, 120f, 40f);        // 只有 120px 宽，下面那句字必然越界
            var r8lb = MenuDraw.Text(r8go.transform, r8live, "WWWW WWWW WWWW WWWW WWWW", Color.white, "Probe", 30f, 3000);
            CheckTrue(r8lb != null, "文字探针建出来了");
            if (r8lb != null)
            {
                bool r8moved = MenuDraw.ClipText(r8lb, r8live, Vector2.zero);
                CheckTrue(r8moved, "`ClipText` 当场**真的切了**（切不上就没有「重裁」可言 ⇒ 下面那条等于没验）");
                CheckTrue(r8lb.GetComponent<ClippedTextGuard>() != null,
                          "…而且这一段字上挂着 `ClippedTextGuard`（`Reclip()` 长在它身上）");
                var r8tmp = r8lb.GetComponentInChildren<TMPro.TextMeshPro>();
                CheckTrue(r8tmp != null, "…走的是 TMP 那条后端（守卫生效的那条）");
                if (r8tmp != null)
                {
                    // ⚠️ **先把「文字真正被画的位置」挪一下**（+240px = 往右一大截）：重裁那条路**是幂等的**
                    //    —— `MenuDraw.ClipTmpMesh` 返回的是 `any`（「这一刀有没有真的要改东西」），
                    //    同一个位置重裁**一个字节都不用改** ⇒ 它**本来就该**返回 false、计数器也就**不该**涨。
                    //    生产里位置会变，是因为 `RefreshBounds()` 自己把 TMP 子节点挪到位（A206 就是这件事）
                    //    ⇒ 这里显式造出那一步（⛔ 否则这条断言变成「断一件本不该发生的事」）。
                    //    ⚠️ 硬边（`softPx = 0`）下 `SoftAlpha` 恒 1 ⇒ 这一刀**只有顶点的位移**可写；
                    //    +240px 让「第一刀被夹住的那一角」无论原来夹在左沿还是右沿，**都还会被夹**（值必变）
                    //    ⇒ 不依赖「文字有多宽 / 夹在哪一侧」。
                    int r8n0 = MenuDraw.TextReclipAfterPlace;
                    r8lb.transform.localPosition += new Vector3(240f / 108f, 0f, 0f);
                    r8lb.RefreshBounds();          // = `SetText` / `SetAutoFitBox` / 建标签… 每一条定版面的路的末句
                    int r8n1 = MenuDraw.TextReclipAfterPlace;
                    CheckTrue(r8n1 > r8n0,
                              $"★ 位置变了之后**又补了一刀**（`TextReclipAfterPlace` {r8n0} → {r8n1}）——"
                              + " 拿掉 `Battle/Label.cs` 的 `RefreshBounds()` 末句那两句"
                              + "（`GetComponent<ClippedTextGuard>()` + `guard.Reclip()`）这里就不涨");
                }
            }
            Object.DestroyImmediate(r8go);
        }

        // ---------------- ⑤·d-2 🆕 A277：`clipSoftness = 0` 也**照样硬裁**（图形那半）
        //
        // 🔴 **缺口**（块4 顺手发现 · A277，与 A233 同族）：`MenuDraw.ApplySoftEdges` 入口原来第 3 行就是
        //   `if (softPx.x <= 0f && softPx.y <= 0f) return;` ⇒ **「`clip` 有值 + `soft = (0,0)`」时连硬裁都不做**
        //   （调用方传了 `clip` 也整块画到框外）。
        //   判据 = 原版 `RectMask2D`：**硬裁四边都求交、与 `m_Softness` 无关**
        //   （uGUI `Culling/Clipping.cs:17` `FindCullAndClipWorldRect`；`m_Softness` 只经 `UpdateClipSoftness()`
        //    当**渐隐带宽**）⇒ 「两分量都 0」= **没有渐隐带**，⛔ **不是「不裁」**。
        // ⚠️ 今天仍带着 `clipSoftness.x > 0 || clipSoftness.y > 0` 那道闸的只剩 `Rect`/`Nine`/`Tiled`
        //   三条路（它们**自己**先裁过 ⇒ 无害）；`PracticeModePopup.ImgTex` 那道闸本批已去掉（A233）
        //   ⇒ 本条对它**带电**，但今天没有调用点给它传 `(0,0)` ⇒ 实际行为仍**零变化**。
        //   本节的牙口 = **直接调公共件**（`ApplySoftEdges` 是 `public`），不受那几道闸影响。
        Section("共用件：`clipSoftness = 0` 也**照样硬裁**（A277 —— 判据 = 原版 `RectMask2D` 四边都求交）");
        {
            var zg = new GameObject("ZeroSoftProbe");
            var zFrame = new PxRect(400f, 300f, 700f, 500f);      // 裁切框（画布 px · 左上原点）
            var zRect = new PxRect(400f, 300f, 900f, 500f);       // 这一颗的**右沿越出框 200px**
            // ⚠️ **不带 `clip` 建**：`MenuDraw.Rect` 的 `clip` 那一路会**自己**先裁一刀 ⇒ 就测不到本条了
            var zq = MenuDraw.Rect(zg.transform, CardArt.Solid(), zRect, "Over", 3000);
            CheckTrue(zq != null, "（前提）越界那颗 quad 建出来了");
            if (zq != null)
            {
                float a0, b0, c0, d0;
                CheckTrue(QuadPxRect(zq, out a0, out b0, out c0, out d0) && Mathf.Abs(c0 - 900f) <= 0.2f,
                          $"（前提）调 `ApplySoftEdges` **之前**它真是原尺寸（右沿 {c0:F1} ≈ 900）——"
                          + " 少了这条，下面「裁到 700」可能只是「本来就在 700」");
                MenuDraw.ApplySoftEdges(zq, zRect, zFrame, Vector2.zero, new Rect(0f, 0f, 1f, 1f));
                float a, b, c, d;
                CheckTrue(QuadPxRect(zq, out a, out b, out c, out d), "（前提）量得到它的渲染矩形");
                CheckNear(c, 700f, 0.2f,
                          "★★ A277：`soft=(0,0)` **也硬裁** —— 右沿落在框沿 **700**"
                          + "（把那条早退 `if (softPx.x <= 0f && softPx.y <= 0f) return;` 加回去 ⇒ **900** ⇒ 这条红）");
                CheckNear(a, 400f, 0.2f, "…左沿仍在 400（框内那一半不动）");
                CheckNear(b, 300f, 0.2f, "…上沿不动");
                CheckNear(d, 500f, 0.2f, "…下沿不动");
                CheckNear(zq.UvRect.width, 0.6f, 0.002f,
                          "★ …而且 **uv 跟着截**（0.6 = 裁剩 300 ÷ 原 500）—— 只缩矩形不缩 uv 会把图**压扁**");
            }
            Object.DestroyImmediate(zg);

            // 成对（**判别力**）：同一个矩形、框**包住**它 ⇒ 一个字节都不动。
            //   少了这一条就分不出「按需裁」与「无条件缩到框沿」（后者也会让上面那条通过）。
            var zg2 = new GameObject("ZeroSoftProbeInside");
            var zInside = new PxRect(300f, 200f, 1000f, 600f);    // 完全包住 `zRect`
            var zq2 = MenuDraw.Rect(zg2.transform, CardArt.Solid(), zRect, "Inside", 3000);
            CheckTrue(zq2 != null, "（前提）包在内侧那颗 quad 建出来了");
            if (zq2 != null)
            {
                MenuDraw.ApplySoftEdges(zq2, zRect, zInside, Vector2.zero, new Rect(0f, 0f, 1f, 1f));
                float a, b, c, d;
                if (QuadPxRect(zq2, out a, out b, out c, out d))
                {
                    CheckNear(c, 900f, 0.2f, "★ 成对：框包住这颗 ⇒ **原尺寸**（右沿仍 900 —— 不是被无条件缩到框沿）");
                    CheckNear(zq2.UvRect.width, 1f, 0.002f, "…uv 也仍是整张（1.0）");
                }
            }
            Object.DestroyImmediate(zg2);
        }

        // ---------------- ⑤·d-3 🆕 A233：`clipSoftness = 0` 时**文字那半**也要硬裁
        //
        // 🔴 **缺口**（`Shell/PracticeModePopup.cs` 的 `Txt` / `ImgTex`）：那两处把整件事挂在
        //   `clipSoftness.x > 0f || clipSoftness.y > 0f` 上 ⇒ `(0,0)` 时那段字**既不硬裁也不建软边**
        //   （整段画到视口外）。原版 `RectMask2D` 是「**先硬裁、再按 `m_Softness` 渐隐**」两件事。
        // ✅ 修法（本批已落地）= 闸只留 `clip.HasValue`，把 `clipSoftness` **原样**传下去 ——
        //   `MenuDraw.ClipText(…, Vector2.zero)` 本身就是**纯硬裁**（`ClipQuad` 夹顶点 + 同仿射改 uv，
        //   `SoftAlpha` 在 soft ≤ 0 时恒 1 ⇒ 不削 alpha）。
        // ⚠️ **本节只钉住「机制那一半」**（soft=0 时那一刀真把字夹进了框、渲出来不越界）；
        //   「闸那一半」（`PracticeModePopup.Txt` 在 `(0,0)` 时到底叫不叫 `ClipText`）的断言宿主是
        //   `Editor/MainMenuScene.cs`（练习窗的探针都在那儿）—— 本批那一路在**别人的白名单**里 ⇒ 未落，
        //   见 `资料/普查产出_1009/写手W2_A277_A233.md`。
        Section("共用件：`clipSoftness = 0` 时**文字照样硬裁**（A233 机制半 —— `MenuDraw.ClipText(…, Vector2.zero)`）");
        {
            var tg = new GameObject("ZeroSoftTextProbe");
            var tFrame = new PxRect(0f, 0f, 120f, 40f);       // 只有 120px 宽 ⇒ 下面那段字必然越界
            var lb = MenuDraw.Text(tg.transform, tFrame, "WWWW WWWW WWWW WWWW WWWW", Color.white, "Probe", 30f, 3000);
            CheckTrue(lb != null, "（前提）文字探针建出来了");
            if (lb != null)
            {
                // ⚠️ 前提：这一刀**必须落在会被画出来的那份网格上**（不然就是「量到一个没上传的数组」）
                var tmp0 = lb.GetComponentInChildren<TMPro.TextMeshPro>();
                var mf0 = tmp0 != null ? tmp0.GetComponent<MeshFilter>() : null;
                CheckTrue(mf0 != null && mf0.sharedMesh != null,
                          "（前提）TMP 有一份在渲染的网格（`MeshFilter.sharedMesh` 在 ⇒ 下面量到的就是画面那份）");
                float wide0 = TextMeshWidthPx(lb);
                CheckTrue(wide0 > tFrame.W + 1f,
                          $"（前提）这段字**裁之前真的越界**：渲出 {wide0:F1}px > 框宽 {tFrame.W:F0}px"
                          + "（不越界 ⇒ 下面那条没有鉴别力）");
                int up0 = MenuDraw.TextClipUploadSkipped;
                bool moved = MenuDraw.ClipText(lb, tFrame, Vector2.zero);
                CheckTrue(moved, "`ClipText(…, Vector2.zero)` 当场**动了顶点**（⇒ soft=0 那条路是硬裁、不是空转）");
                Check(MenuDraw.TextClipUploadSkipped, up0,
                      "…而且这一刀**真上传了**（没落进「那一刻没在渲染的那份网格」那一档）");
                float wide1 = TextMeshWidthPx(lb);
                CheckTrue(wide1 <= tFrame.W + 0.5f,
                          $"★★ A233：裁完**渲染宽度 ≤ 框宽**（{wide1:F1} ≤ {tFrame.W:F0}）—— 删掉 `ClipQuad` 里那几句"
                          + "夹顶点、或让 `ClipText` 在 soft=0 时早退 ⇒ 宽度回到上面那个未裁值 ⇒ 立刻红"
                          + "｜⛔ 它不是「比字号」：量的是渲染网格的顶点跨度，不是 `Label.WorldW`");
            }
            Object.DestroyImmediate(tg);
        }

        // ---------------- ⑤·d-4 🆕 **2026-10-13（A464 · B2–B5）**：裁切状态长在【视口节点】上之后的行为
        //
        // 判据 = `Shell/ViewportClip.cs` 文件头（原版「**每个 `Viewport` 一个 `RectMask2D`**」）
        //   + `Shell/MenuWindowBase.cs` 的 `Text` / `TextBox` 那两处守卫
        //   （🆕 2026-10-12 A435① 改成吃 **`ViewportClip.Resolve` 解析后**的那一份）。
        // 🔴 **与 `Editor/RewardsScene.cs` 的「A464·B1」成对，而且【故意分在两个场景】**：
        //   那一条断「全仓不挂节点 ⇒ 两个静态计数恒 0」（= 阶段 1 的共存保证），
        //   而**本块自己会挂一个节点**（把两个计数顶起来）—— `-executeMethod` 每次是**新进程**，
        //   所以只有分场景才互不污染。⛔ 别把两块合并到一处。
        //   ⚠️ **同理，本场景里以后任何「`NodeResolutions` / `NodeShadowedByParam` == 0」那种断言必须排在
        //   本块【之前】**（本块之后它们已经是非 0 了）；想在后半段断那种不变量就另开场景。
        // ⚠️ 全程**只碰临时件**（`RewardsWindow.Create` 建的空窗 + 一颗临时根节点），结尾**全部销毁** ——
        //   节点留着会把**后面每一段**挂在它父链下的文字都裁到那个框里（静默、只在后面的段里现形）。
        //   ⛔ 也因为它必须销毁，本块排在 ⑤·d 那几段**之后**、⑤·e（键盘导航）**之前**。
        Section("★ A464·B2–B5 视口节点（`ViewportClip`）：文字吃节点态 · 守卫跟着节点走 · 两个节点态重载 · 漏删探测器带电");
        {
            // `RewardsWindow : MainMenuSubmenuWindow` —— `Text` / `TextBox` 那两处守卫的宿主
            // （同 §⑤·b 的 `padWin` 那种取法：`Create` 出来**不开窗**，直接当夹具用）。
            var vpWin = RewardsWindow.Create(shell.Windows);
            // 🔴 本窗**不设** `Clip`（= 形参 `null`）—— 那正是「由父链上的节点接管」那一格；
            //    本窗若设了 `Clip`，形参赢、节点永远不生效（`Resolve` 第 1 支）。
            vpWin.Clip = null;
            vpWin.ClipSoftness = Vector2.zero;
            vpWin.ClipPad = Vector4.zero;
            var vpRoot = new GameObject("VpClipProbe").transform;   // 临时根：节点**不挂**在窗根上（否则污染后面所有文字）
            vpRoot.SetParent(vpWin.transform, false);
            var vpBox = new PxRect(100f, 100f, 500f, 500f);         // 视口节点自己的 rect（画布 px、左上原点）
            var vc = ViewportClip.Hang(vpRoot, "Viewport", vpBox, Vector4.zero, Vector2Int.zero);
            CheckTrue(vc != null, "（前提）视口节点建出来了（`ViewportClip.Hang` ⇒ `MenuDraw.Node` 建的 `RectTransform`）");
            // 🔴 **2026-10-13（A497）**：这里必须用**带容差**的 `NearPx`，⛔ 不能用 `MenuDraw.SameRect`（逐位 `==`）——
            //   `ClipPx` 是**反推值**（节点世界坐标 → px），隔着一趟「px → 设计世界 → px」的 float32 往返，
            //   实测残差 **−6.1e-5 px**（A497 诊断 §1.2：`vpBox.x1` 写 100 ⇒ 读回 99.999939）⇒ 逐位比**恒假**（假阴）。
            //   改坏法：① 把 `NearPx` 换回逐位 `==` ⇒ 立刻红（就是 A497 之前那一版）；
            //           ② 让节点那个 rect 与 `vpBox` 真差 ≥0.05px ⇒ **仍红**（容差没把牙口磨掉）。
            PxRect? cp1 = vc != null ? vc.ClipPx : null;   // 先取一份：拿不到时下面的文案不解引用
            CheckTrue(cp1.HasValue && NearPx(cp1.Value, vpBox),
                      "（前提）节点那个框 = **它自己的 rect**（`ClipPx` 实读 "
                      + (cp1.HasValue ? $"{cp1.Value.x1:F5},{cp1.Value.y1:F5} → {cp1.Value.x2:F5},{cp1.Value.y2:F5}"
                                      : "⛔ 拿不到（节点不是 `RectTransform`？）")
                      + $"；期望 {vpBox.x1:F0},{vpBox.y1:F0} → {vpBox.x2:F0},{vpBox.y2:F0}）"
                      + " —— 原版 `RectMask2D` 用的也是自己那个 `rectTransform`"
                      + "｜⚠️ 两边打印差在**小数第 4 位**（如 99.999939 vs 100.000000）就是那趟往返的残差，"
                      + "不是几何错位 —— 容差 0.05px 的理由见 `NearPx` 的注释");

            // 一段**必然横跨节点左右两条边**的字：59 个字符（'W' 为主）× 60px ⇒ 不换行时宽 ≈ 3000px 量级，
            //   而框只有 400px 宽、字块中心落在 (500, 330)（= 框的右沿上）⇒ 两侧都远远压出框外。
            const string Wide = "WWWW WWWW WWWW WWWW WWWW WWWW WWWW WWWW WWWW WWWW WWWW WWWW";
            const float Tx1 = 0f, Tx2 = 1000f, Ty1 = 300f, Ty2 = 360f;   // 竖向整条都在框内（y 100..500）⇒ 只看 x 那一半

            // ============================================================ B2 文字吃节点态
            var lbNode = vpWin.Text(vc.transform, Wide, Tx1, Tx2, Ty1, Ty2, 5, Color.white, "TNode", 60f);
            CheckTrue(lbNode != null,
                      "★ A464·B2：节点态下这段**越界**的字**建出来了**（两侧都压着节点边界 ⇒ 必须有交集）");
            // 控制组：**同一段字、同一个矩形、父链上没有节点**（挂在临时根上）⇒ 不许被切。
            //   少了它，「实验组全在框内」可能只是「这段字本来就不宽」那种**恒真**的弱断言。
            var lbFree = vpWin.Text(vpRoot, Wide, Tx1, Tx2, Ty1, Ty2, 5, Color.white, "TFree", 60f);
            CheckTrue(lbFree != null, "★ A464·B2：（控制组）挂在**没有节点**的那个父节点下 ⇒ 也建出来了");
            float fMinX, fMinY, fMaxX, fMaxY;
            CheckTrue(TmpSpanPx(lbFree, out fMinX, out fMinY, out fMaxX, out fMaxY),
                      "★ A464·B2：（控制组）TMP 网格量得到（**直读** `textInfo.meshInfo[..].vertices`，⛔ 不是 `Label.WorldW`）");
            CheckTrue(fMinX < vpBox.x1 - 1f && fMaxX > vpBox.x2 + 1f,
                      $"★ A464·B2：（控制组）这段字**本来就压出节点框两侧**（实测 x {fMinX:F0}..{fMaxX:F0}，"
                      + $"框 {vpBox.x1:F0}..{vpBox.x2:F0}）—— 没有这一条，下面「全在框内」等于没断");
            float nMinX, nMinY, nMaxX, nMaxY;
            CheckTrue(TmpSpanPx(lbNode, out nMinX, out nMinY, out nMaxX, out nMaxY),
                      "★ A464·B2：实验组的 TMP 网格也量得到");
            CheckTrue(nMinX >= vpBox.x1 - 0.6f && nMaxX <= vpBox.x2 + 0.6f,
                      $"★★ A464·B2：节点那个框**真的作用在文字网格上**（实测 x {nMinX:F2}..{nMaxX:F2} ⊆ "
                      + $"{vpBox.x1:F0}..{vpBox.x2:F0}；y {nMinY:F0}..{nMaxY:F0} 本条不看 —— 竖向整条都在框内；"
                      + $"控制组是 {fMinX:F0}..{fMaxX:F0}）"
                      + " —— 把 `Shell/MenuWindowBase.cs` 里 `Text` 的 `_st.RenderClip` 改回 `RenderClip`"
                      + "（本窗 `Clip` 为 null ⇒ `if (RenderClip.HasValue)` 恒假 ⇒ **根本不调 `ClipText`**）⇒ 红");
            CheckTrue(nMinX <= vpBox.x1 + 0.6f && nMaxX >= vpBox.x2 - 0.6f,
                      $"★ …而且**两条边都真被夹过**（左沿 {nMinX:F2} ≈ {vpBox.x1:F0} · 右沿 {nMaxX:F2} ≈ {vpBox.x2:F0}）"
                      + " —— 只夹一边、或「整块照画出去」的实现这里红");
            CheckTrue(ViewportClip.NodeResolutions > 0,
                      $"★ …**这条路带电**：真走过「节点态」那一支 {ViewportClip.NodeResolutions} 次"
                      + "（只断「建出来了」的话，改坏实现不会红）");

            // ⚠️ `TextBox` 是**另一条**建字的路（`MainMenuSubmenuWindow.TextBox`）—— **必须各一条**，
            //    只断 `Text` 会漏掉整整一族（H25 §三 那行「只改 `Text` 不改 `TextBox`」）。
            //    ⚠️ 它**限宽换行**（`SetWrapWidth(r.W)` = 1000）⇒ 字块的宽度与 `Text` 那条**不是一回事**
            //       ⇒ 控制组**也得各来一份**（否则「全在框内」可能只是「换行之后本来就没到框边」）。
            var lbBox = vpWin.TextBox(vc.transform, new PxRect(Tx1, Ty1, Tx2, Ty2), Wide, Color.white, "TBNode", 60f);
            var lbBoxFree = vpWin.TextBox(vpRoot, new PxRect(Tx1, Ty1, Tx2, Ty2), Wide, Color.white, "TBFree", 60f);
            CheckTrue(lbBox != null && lbBoxFree != null,
                      "★ A464·B2：`TextBox` 那条路（限宽换行那族）节点态/无节点两扇都建出来了");
            if (lbBox != null && lbBoxFree != null)
            {
                float bMinX, bMinY, bMaxX, bMaxY, kMinX, kMinY, kMaxX, kMaxY;
                CheckTrue(TmpSpanPx(lbBoxFree, out kMinX, out kMinY, out kMaxX, out kMaxY),
                          "★ …（控制组）它的 TMP 网格量得到");
                CheckTrue(kMinX < vpBox.x1 - 1f && kMaxX > vpBox.x2 + 1f,
                          $"★ …（控制组）**换行之后**这一段也压出节点框两侧（实测 x {kMinX:F0}..{kMaxX:F0}）"
                          + " —— 没有它，下面那条可能只是「换行把它缩进框里了」");
                CheckTrue(TmpSpanPx(lbBox, out bMinX, out bMinY, out bMaxX, out bMaxY),
                          "★ …（实验组）它的 TMP 网格也量得到");
                CheckTrue(bMinX >= vpBox.x1 - 0.6f && bMaxX <= vpBox.x2 + 0.6f,
                          $"★★ A464·B2（`TextBox` 版）：换行那族的文字也**被夹进节点框**"
                          + $"（实测 x {bMinX:F2}..{bMaxX:F2} ⊆ {vpBox.x1:F0}..{vpBox.x2:F0}；"
                          + $"控制组 {kMinX:F0}..{kMaxX:F0}）"
                          + " —— ⛔ 只修 `Text`、不修 `TextBox` 的实现这里红（同一扇窗里一半文字吃节点、一半不吃）");
            }

            // ============================================================ B3 守卫跟着节点走
            // 🔴 判据 = `ClippedTextGuard` 存的是**实参**、重裁时**当场重新解析**（不是 `Arm` 那一刻的快照）。
            //   牙口**只能比顶点**：比 `lb.WorldW` 不行（它不随裁切变）、比计数器也不行
            //   （旧写法一样 +1 —— 那正是这条断言存在的全部理由）。
            //   两条从顶点之外的旁证：`NodeResolutions` 与 `NodeShadowedByParam` 的**增量方向**
            //   （重裁那一刀若真重新解析 ⇒ 走「节点态」那一支 ⇒ 前者 +、后者不动；若存的是快照 ⇒ 反过来）。
            var vpBox2 = new PxRect(100f, 100f, 300f, 500f);        // 右沿从 500 收到 300
            MenuDraw.ApplyPxRect(vc.transform, vpRoot, vpBox2);      // 挪节点（同一个节点、改它自己的 rect）
            // 🔴 A497：同 §B2 那条 —— `ClipPx` 是**反推值** ⇒ 带 0.05px 容差（⛔ 不再是 `MenuDraw.SameRect` 的逐位 `==`）。
            //   改坏法：① 把 `NearPx` 换回逐位 `==` ⇒ 红；② 让 `ApplyPxRect` 写进去的框与 `vpBox2` 真差 ≥0.05px ⇒ 仍红。
            PxRect? cp2 = vc.ClipPx;                  // 先取一份：拿不到时下面的文案不解引用
            CheckTrue(cp2.HasValue && NearPx(cp2.Value, vpBox2),
                      "（前提）节点那个框真的跟着挪了（`ClipPx` 现读 = 新矩形"
                      + (cp2.HasValue ? $" {cp2.Value.x1:F5},{cp2.Value.y1:F5} → {cp2.Value.x2:F5},{cp2.Value.y2:F5}"
                                      : " ⛔ 拿不到（节点不是 `RectTransform`？）")
                      + $"；期望 {vpBox2.x1:F0},{vpBox2.y1:F0} → {vpBox2.x2:F0},{vpBox2.y2:F0}）"
                      + " —— ⛔ 这里是「写进去 vs 读回来」，两边差在小数第 4 位是往返残差（A497 诊断 §1.2）"
                      + "，不是「没挪」：真没挪的话右沿会停在 **500** 而不是 300");
            int res0 = ViewportClip.NodeResolutions, shadow0b = ViewportClip.NodeShadowedByParam;
            lbNode.RefreshBounds();        // = 「定完版面」那条路的末句 ⇒ 内部会调 `ClippedTextGuard.Reclip()`
            float rMinX, rMinY, rMaxX, rMaxY;
            CheckTrue(TmpSpanPx(lbNode, out rMinX, out rMinY, out rMaxX, out rMaxY), "★ A464·B3：重裁之后网格还量得到");
            CheckTrue(rMaxX <= vpBox2.x2 + 0.6f,
                      $"★★ A464·B3：节点挪了之后重裁**用的是【当下】解析出来的框**（实测右沿 {rMaxX:F2} ≤ {vpBox2.x2:F0}）"
                      + " —— 拿 `Arm` 那一刻的快照（旧框右沿 500）重裁的实现这里会得 500.00；"
                      + "「挪完不重裁」的实现得上面那个未裁值 ⇒ 也红"
                      + "｜🔴 **若这条红了**：先查 `Shell/MenuDraw.cs` 的 `ClipText` 是不是又把被"
                      + " `clip = _st.RenderClip;` 覆盖过的 `clip`（= 解析后的快照）交给了 `ArmTextGuard`"
                      + " —— 那会让守卫按 `Arm` 那一刻的旧框重裁。**A484 修的就是那一处**（`clipArg`），"
                      + "一行修法见本节上面那段注释与 `资料/普查产出_1012/H40_ClipText形参修复.md`");
            CheckTrue(rMaxX >= vpBox2.x2 - 0.6f,
                      $"★ …而且**真贴在新右沿上**（{rMaxX:F2} ≈ {vpBox2.x2:F0}）—— 少了这条，「≤ 300」可能是"
                      + "「字本来就没到 300」（控制组那条已排除一次，这里再钉一次）");
            CheckTrue(Mathf.Abs(rMinX - nMinX) <= 0.6f,
                      $"★ …而**左沿没动**（{rMinX:F2} vs 重裁前 {nMinX:F2}）—— 这一刀是按新框**重算**的，不是把整块平移");
            // 🔴 **2026-10-13（H33）落地时，下面这三条【预期是红】—— 根因在 `Shell/MenuDraw.cs`
            //   （当时在本件白名单外、没改）**；✅ **【A484 · 2026-10-13】那一处已修**（`ClipText` 现在
            //   先留 `var clipArg = clip;`、`ArmTextGuard` 收的是 `clipArg`）⇒ **这三条预期转绿**
            //   （同步点跑 `ShellScene.Run` 认；改动清单 → `资料/普查产出_1012/H40_ClipText形参修复.md`）。
            //   根因原文**保留**（它现在就是本条断言的【改坏法】）：`ClipText` 里那句 `clip = _st.RenderClip;`
            //   把形参 `clip` 覆盖成了「解析后的框」⇒ 交给 `ArmTextGuard` 的成了**快照**、不是调用方原样那一份
            //   ⇒ `ClippedTextGuard._clipArg` 恒非空 ⇒ 每次重裁都落 `ViewportClip.Resolve` 的**第 1 支**
            //   （「形参赢、连父链都不走」），两个后果都静默：
            //   ① 节点挪了 / 后挂 ⇒ 重裁仍按 `Arm` 那一刻的**旧框**（= 上面第一条要钉的）；
            //   ② 每重裁一次 `NodeShadowedByParam` **+1**（把「漏删探测器」误报成「有旧设站点没删」）。
            //   **改坏法** = 把 `ArmTextGuard(lb, clipArg, …)` 改回传 `clip` ⇒ 这三条立刻红。
            CheckTrue(ViewportClip.NodeResolutions > res0,
                      $"★★ A464·B3（定位用，同一个根因）：重裁那一刀**重新走过「节点态」那一支**"
                      + $"（`NodeResolutions` {res0} → {ViewportClip.NodeResolutions}）—— 守卫存的是【实参】才会这样；"
                      + "存快照（= 把 `ArmTextGuard` 的 `clipArg` 改回 `clip` 那一档，A484 之前就是它）"
                      + "⇒ 走 `Resolve` 第 1 支 ⇒ 这个数一动不动");
            CheckTrue(ViewportClip.NodeShadowedByParam == shadow0b,
                      $"★★ A464·B3（定位用，同一个根因）：…而且**不该**被记成「形参盖住节点」"
                      + $"（实测 {shadow0b} → {ViewportClip.NodeShadowedByParam}，多了就说明守卫交出去的是"
                      + "**解析后的框**（= 快照）—— 那会让「漏删探测器」把守卫自己误报成「旧设站点没删」）");

            // ============================================================ B4 两个「节点态」重载
            // ⚠️ 这两个函数今天**零生产调用点**（H25 §六·2）⇒ 本节是**接口**验收，不是生产行为的验收。
            var outside = new PxRect(600f, 150f, 900f, 350f);        // 整块落在节点框（右沿 300）之外
            CheckTrue(!MenuDraw.VisibleAbove(vc.transform, outside, null),
                      "★ A464·B4：整块在节点框外 ⇒ `VisibleAbove` 说「不见」（= 调用方「不建」）");
            CheckTrue(MenuDraw.VisibleAbove(vpRoot, outside, null),
                      "★ A464·B4（成对 · 判别力）：**同一个矩形**、父链上没有节点 ⇒ 说「可见」"
                      + " —— 少了这一条就分不出「按节点判」与「一律返回 false」");
            var coverClip = new PxRect(590f, 140f, 950f, 360f);      // 显式覆盖：包住 `outside`
            CheckTrue(MenuDraw.VisibleAbove(vc.transform, outside, coverClip),
                      "★ A464·B4：**显式形参赢**（旧路那一份照旧覆盖节点）"
                      + " —— 把 `VisibleAbove` 里喂给 `Resolve` 的 `clip` 改成 `null` ⇒ 红（= 「显式覆盖被丢掉」）");
            PxRect o4;
            CheckTrue(MenuDraw.ClipRectAbove(vc.transform, new PxRect(200f, 100f, 900f, 400f), null, out o4),
                      "★ A464·B4：`ClipRectAbove` 压着节点框那一块 ⇒ true");
            // 🔴 A497：右沿那一份来自**节点框**（`ClipPx` 反推值 299.999939，见诊断 §1.2）⇒ 逐位 `== 300f` **恒假**。
            //   ⛔ 期望值照旧写**字面量**，⛔ 不在这里重写一遍 Max/Min（那是拿同一个公式验同一个公式）；
            //   只把比较换成 0.05px 容差（四个字段各自比）。
            //   改坏法：① 把 `NearPx` 换回 `==` ⇒ 红（假阴那一版）；② 让 `ClipRectAbove` 返回**没求交**的那一份
            //   （右沿 900）或节点框真换成别的值（差 ≥0.05px）⇒ **仍红**。
            CheckTrue(NearPx(o4.x1, 200f) && NearPx(o4.y1, 100f) && NearPx(o4.x2, 300f) && NearPx(o4.y2, 400f),
                      $"★ …`outRect` = **与节点那个框求交**（期望 200,100 → 300,400；实测 "
                      + $"{o4.x1:F5},{o4.y1:F5} → {o4.x2:F5},{o4.y2:F5}）"
                      + "｜期望值写**字面量** —— ⛔ 不在这里重写一遍 Max/Min（那是拿同一个公式验同一个公式）"
                      + "｜⚠️ 打印用 `:F5`（原来是 `:F0` ⇒ 把 299.999939 印成「300」、**期望与实测一模一样却仍红**，"
                      + "A497 之前那一版就是这么把人挡在门外的）；容差 0.05px 的理由见 `NearPx` 注释");
            CheckTrue(!MenuDraw.ClipRectAbove(vc.transform, outside, null, out o4),
                      "★ …而整块在框外那一个 ⇒ false（与 `VisibleAbove` 那条同判据）");

            // ============================================================ B5 漏删探测器（真的会数）
            // 🔴「旧设站点没删干净」的现场 = 父链上有节点、而这一处仍传着**非空**的显式形参
            //   ⇒ 节点白挂着、行为照旧、**不出声** ⇒ 只有这个计数留下痕迹（`ViewportClip` 文件头那段先后顺序）。
            //   ⚠️ 它与 `Editor/RewardsScene.cs` 的「A464·B1」（断它 == 0）是**同一条不变量的两面**：
            //      那边是「今天没有节点 ⇒ 必须 0」，这边是「有节点时它**真会响**」——⛔ 别把这边也断成 0。
            int shadow0 = ViewportClip.NodeShadowedByParam;
            var explicitClip = new PxRect(1000f, 1000f, 1100f, 1100f);
            var st5 = ViewportClip.Resolve(vc.transform, explicitClip, Vector2.zero, Vector4.zero);
            Check(ViewportClip.NodeShadowedByParam - shadow0, 1,
                  "★★ A464·B5：**漏删探测器真的会数** —— 父链上有节点、又传了非空形参 ⇒ `NodeShadowedByParam` **+1**"
                  + "（删掉 `Shell/ViewportClip.cs` 的 `if (FindAbove(parent) != null) NodeShadowedByParam++;` ⇒ 得 0 ⇒ 红）"
                  + "｜⚠️ 今天全仓无节点 ⇒ 那 0 是**恒真**的，**只有本块这一条**才让它带电");
            CheckTrue(!st5.FromNode && st5.Clip.HasValue && MenuDraw.SameRect(st5.Clip.Value, explicitClip),
                      "★ …而且这一刻取到的是**形参**那一份（`FromNode == false` + 逐字段 = 传进去那个框）"
                      + " —— 「形参非空 ⇒ 形参赢」是**设计行为**（迁移期靠它回退），⛔ 不是缺陷");
            CheckTrue(st5.RenderClip.HasValue && MenuDraw.SameRect(st5.RenderClip.Value, explicitClip),
                      "★ …渲染那一份 = `PaddedClip(形参, pad)`（这里 `pad = zero` ⇒ 逐字段还是那个框）");

            // ⚠️ **必须销毁**（理由见本节头）—— 批处理下 `Object.Destroy` 不生效 ⇒ `DestroyImmediate`。
            Object.DestroyImmediate(vpRoot.gameObject);
            Object.DestroyImmediate(vpWin.gameObject);
        }

        // ---------------- ⑤·e 🆕 A49：键盘导航（ESC 关当前窗 · 方向键选 · 回车确认）
        //
        // 🔴 **判据 = 本地 UGUI 源码 + 原版反编译**（行号、原文与「哪几条是我们挑的」→
        //    `Shell/PointerLayer.cs` 的「键盘导航（A49）」那一节，别在这里抄第二份）：
        //    · 方向键/回车 = `StandaloneInputModule.Process()` 那三跳 + 它的两个节流值
        //      （`m_RepeatDelay = 0.5` · `m_InputActionsPerSecond = 10`）。**原版真的跑这一套**：
        //      它的输入模块 `EverguildInput` 是 `StandaloneInputModule` 的子类，`Process()` 第一句就是 `base.Process()`。
        //    · ESC = 原版 `WindowsManager.Update`（打给**最上面那扇窗**）+ `GameWindow.ESCPressed` 里的**两道**门槛
        //      （🆕 A77㉑① 订正：原来是「一句 `closeOnESC(0x39)`」，实测**前面还有一道**
        //       `EventSystemController.Instance.eventSystem.enabled`；逐条反汇编 → `Shell/WindowsManager.cs`
        //       的 `ESCPressed`；⚠️ 判据原文 ㉑①-a 写的 `ChatPreviewMessage` **是错的**，见那段订正）。
        // ⚠️ **纪律**：本节期望值**全部是字面量**（按钮名 / 顺序 / 时间戳 / 两种状态各一条），
        //    **不从被测实现里读**。理由是本文件 §② 那 12 条的前车之鉴 —— 那批断言的期望值取自
        //    `ShellRuntime` 的常量，而那几个常量**正是写进网格的同一个来源** ⇒ 常量怎么改都恒绿（自证）。
        Section("A49 键盘导航：ESC 关当前窗（门槛 = 该窗自己的 `closeOnEsc`）· 方向键选 · 回车确认");
        {
            var pl = PointerLayer.Instance;
            CheckTrue(pl != null, "指针层在场景里（全壳唯一一条输入路；`Instance` 没有就现建一台）");
            shell.Windows.CloseAllWindows();          // 隔离：下面「第一颗 / 正下方那颗」才唯一

            // 探针窗：**4 颗竖排**（NavA→NavD 自上而下）+ **右边一颗干扰项**（NavR）
            // —— 干扰项专治「不判方向、只按层级顺序循环」那种假实现。
            var probeGo = new GameObject("KeyProbe");
            var probe = probeGo.AddComponent<GameWindow>();
            probe.type = WindowType.Fullscreen;
            probe.closeOnEsc = true;                  // ← 被测的那一格（整段中途会改）
            probe.placement = WindowsPlacement.Canvas;
            WindowsManager.AttachToAnchor(probe);

            var hits = new GameObject("Hits").transform;
            hits.SetParent(probeGo.transform, false);
            string fired = "";
            var navNames = new[] { "NavA", "NavB", "NavC", "NavD", "NavR" };
            var navX = new[] { 100f, 100f, 100f, 100f, 500f };    // 画布 px（左上原点）
            var navY = new[] { 100f, 200f, 300f, 400f, 100f };    // y 向下 ⇒ NavA 在最上方
            for (int i = 0; i < navNames.Length; i++)
            {
                var wb = MakeNavButton(hits, navNames[i], navX[i], navY[i], 80f, 40f);
                string nm = navNames[i];
                wb.onClick = () => fired += nm + " ";
            }

            shell.Windows.OpenWindow(probe);
            // 🆕 A77㉑⑤（去脆）：这里原来断的是**全场景**计数 `Check(PointerLayer.ButtonCountForTest, 5, …)`。
            //   全场景那个数会被**别的窗 / 别的宿主**顶高，而下面那串断言真正依赖的前提是「**探针这一扇窗里**只有 5 颗」
            //   ⇒ 改成：**子树**计数当判据（`ButtonCountUnder`，与 `FindInDirection` **同一份筛选** `Navigable`）、
            //   全场景那个数降级成**打印 + 上界**（⛔ 不写死相等 —— 写死就回到那条脆断言）。
            int sceneNav = PointerLayer.ButtonCountForTest;                  // 只报、不给判据
            Debug.Log($"[Chk] （打印·非判据）**全场景**可导航按钮数 = {sceneNav}；**探针窗内** = "
                      + $"{PointerLayer.ButtonCountUnder(probe.gameObject)}"
                      + "（全场景 ≠ 5 时，下面那串方向键断言的可信度按差值打折 —— ⛔ 但**不许**把它变回写死的判据）");
            Check(PointerLayer.ButtonCountUnder(probe.gameObject), 5,
                  "前置：**探针这一扇窗里**正好 5 颗可导航按钮（否则「第一颗 / 正下方那颗」都不唯一、下面几条等于没查）"
                  + "（判据 = `PointerLayer.ButtonCountUnder`，与 `FindInDirection` 同一份筛选 `Navigable`）");
            CheckTrue(sceneNav >= 5,
                      "上界：**全场景**可导航按钮数 ≥ 探针这 5 颗（降级成**上界** —— 全场景会被别家顶高，写死相等就脆了）"
                      + "（原版 `Selectable.s_Selectables` 本来就是**全局表**（`Selectable.cs:24`）⇒ 偏大是正常的）");
            Check(SelName(pl), "NavA",
                  "★ 开窗 ⇒ 默认选中**窗内层级序第一颗**（对位 `StandaloneInputModule.ActivateModule`。"
                  + "⚠️「第一颗」的定义**是我们挑的** —— 原版那个 `firstSelectedGameObject` 本地查不到，见 `PointerLayer` ①）");

            // ① 四个方向各走一步（期望值 = 名字字面量；摆位决定了只有这一个答案）
            pl.KeyMove(0f, -1f, 100f);                // ↓
            Check(SelName(pl), "NavB",
                  "★ ↓ = 从 `NavA` 到**正下方**那颗 `NavB`（同排右边那颗 `NavR` 是干扰项）");
            pl.KeyMove(0f, -1f, 100.2f);              // ↓，距上一次仅 200ms
            Check(SelName(pl), "NavB",
                  "★ 同方向连按 200ms **走不动**（原版 `m_RepeatDelay = 0.5` —— 这是上面那条的反面）");
            pl.KeyMove(0f, +1f, 100.2f);              // ↑，**同一时刻**、只换了方向
            Check(SelName(pl), "NavA",
                  "★ **换方向**在同一时刻**走得动**（原版那一档只按 `1 / m_InputActionsPerSecond = 0.1s` 节流）"
                  + " —— 与上一条合起来才把「同方向等 0.5s、换方向只等 0.1s」这条规则钉住");
            pl.KeyMove(+1f, 0f, 101f);                // →
            Check(SelName(pl), "NavR", "★ → = 到同排右边那颗 `NavR`（横轴真的在参与判定）");
            pl.KeyMove(-1f, 0f, 102f);                // ←
            Check(SelName(pl), "NavA", "★ ← = 回 `NavA`");
            pl.KeyMove(0f, +1f, 103f);                // ↑ —— 已经在最上面了
            Check(SelName(pl), "NavA",
                  "★ 最上面那颗再往上 ⇒ **停住不动**（原版 `FindSelectable` 找不到就什么都不选，不是绕回去）");
            pl.KeyMove(0f, -1f, 104f);                // ↓ 连按三次
            pl.KeyMove(0f, -1f, 104.6f);
            pl.KeyMove(0f, -1f, 105.2f);
            Check(SelName(pl), "NavD", "★ 连按三次 ↓ ⇒ 一路走到最下面那颗 `NavD`（每一跳都真的动了）");
            pl.KeyMove(0f, -1f, 105.8f);              // ↓ —— 已经在最下面了
            Check(SelName(pl), "NavD", "★ 最下面那颗再往下 ⇒ **停住不动**（边界）");

            // ② 回车：选中在谁身上就打给谁（两次打给不同的两颗 ⇒ 分得出「选中真的在动」）
            pl.KeyMove(0f, +1f, 110f);                // ↑ NavD → NavC
            Check(SelName(pl), "NavC", "前置：选中停在 `NavC`（下面那条要断的是「回车打中了谁」）");
            fired = "";
            CheckTrue(pl.KeySubmit(), "★ 回车**真的派发了**（`KeySubmit()` 返回 true）");
            Check(fired, "NavC ", "★ 回车打中的是**选中那一颗**（回调里当场记下自己的名字）");
            pl.KeyMove(0f, +1f, 111f);                // ↑ NavC → NavB
            pl.KeySubmit();
            Check(fired, "NavC NavB ", "★ 换一颗再回车 ⇒ 打中的是**另一颗**（不是「永远打第一颗」）");

            // ③ 没有选中 ⇒ 回车什么都不做（原版 `SendSubmitEventToSelectedObject` 第一句的 null 守卫）
            pl.Select(null);
            fired = "";
            CheckTrue(!pl.KeySubmit(),
                      "★ **没有选中**时回车什么都不做（原版那句 `currentSelectedGameObject == null ⇒ return false`）");
            Check(fired, "", "…而且一个回调都没响（不是「随便挑一颗打」）");
            pl.KeyMove(0f, -1f, 120f);
            Check(SelName(pl), "NavA",
                  "★ 没选中时按方向键 ⇒ **补一颗默认选中**（这一条是**我们挑的**：照抄原版的话"
                  + "「点一下空白 = 选中被清掉」之后键盘就死了，见 `PointerLayer` 的 ①(b)）");

            // ④ 鼠标**按下**也改「选中」（原版 `ProcessMousePress` → `DeselectIfSelectionChanged`）
            //    —— 这条是「键盘与鼠标共用一份选中、没有第二套命中逻辑」的正面证据
            pl.PressAt(100f, 400f);                   // `NavD` 的中心
            Check(SelName(pl), "NavD", "★ 鼠标按下也改「选中」（键鼠共用同一份）");
            fired = "";
            pl.ReleaseAt(100f, 400f);
            Check(fired, "NavD ", "…（顺带：抬起仍在同一颗上 ⇒ 那一下真的点中了，指针那条路没被改坏）");

            // ⑤ ESC：两扇窗、两种 `closeOnEsc` —— 必须分得开；而且打的是**最上面那扇**
            probe.closeOnEsc = true;
            CheckTrue(pl.KeyCancel(), "★ ESC 关掉了 `closeOnEsc = true` 的窗（返回 true）");
            Check(probe.CurrentState, WindowState.Closed, "…它真的进了 `Closed` 态");
            CheckTrue(!probeGo.activeSelf, "…物体也关掉了");
            Check(shell.Windows.openWindows.Count, 0, "…`openWindows` 里也不留它");
            CheckTrue(pl.Selected == null, "…那扇窗里的「选中」跟着作废（选中那颗已经不活了）");

            probe.closeOnEsc = false;
            shell.Windows.OpenWindow(probe);
            Check(shell.Windows.openWindows.Count, 1, "前置：`closeOnEsc = false` 的窗开出来了");
            CheckTrue(!pl.KeyCancel(),
                      "★ ESC 对 `closeOnEsc = false` 的窗**什么都不做**（返回 false；"
                      + "原版 `MissionRerollPopup` / `PromptPopup` / `RewardsWindow` 都是这一档）");
            Check(probe.CurrentState, WindowState.Open, "…它还好好地开着 —— **与上面那条合起来 = 分得出两种状态**");
            CheckTrue(probeGo.activeSelf, "…物体也还开着");

            // ③🆕 A77㉑③（**去自证**）+ 🔴 **2026-10-11（A217③）重写**：`TopWindow` 现在的定义 = `currentWindow`
            //   （= 原版 `get_CurrentWindow` 读字段 0x58；A217③ 之前是 `popUpWindow ?? currentWindow`）。
            //   仍然**跨三态、期望各不相同**地量 —— 任何一个「恒返回某一份字段」的实现都会在中间或最后一条上红：
            //   态一（只有全屏窗）= 底下那扇 → 态二（叠上弹窗）= 弹窗 → 态三（弹窗关掉）= 又回到底下那扇。
            Check(shell.Windows.TopWindow, probe,
                  "（态一：只有全屏窗）`TopWindow` = 它自己（原版 `currentWindow` 的等价物 —— 那一刻 `popUpWindow` 是 null）");

            var popGo = new GameObject("KeyProbePopup");
            var pop = popGo.AddComponent<GameWindow>();
            pop.type = WindowType.Popup;
            pop.closeOnEsc = true;
            pop.placement = WindowsPlacement.Popup;
            WindowsManager.AttachToAnchor(pop);
            shell.Windows.OpenWindow(pop);
            Check(shell.Windows.TopWindow, pop,
                  "（态二：叠上弹窗）`TopWindow` **换成弹窗** —— 与态一/态三的期望**不同** ⇒ 这一条不再恒真。"
                  + "🔴 A217③ 之后它是**直接**从 `currentWindow` 读出来的：原版 `OpenWindowCO` 的 `set_CurrentWindow(win)` "
                  + "写在 type 分支之外（弹窗也写）⇒ 本工程的 `OpenWindow` 已照做");
            Check(probe.CurrentState, WindowState.Background,
                  "（态二）底下那扇被压到 `Background`（原版 `OpenWindowCO` 对弹窗那一支调 `ToBackground()`）");
            CheckTrue(pl.KeyCancel(), "★ 叠了一扇之后，ESC 关的是**最上面**那扇");
            Check(shell.Windows.openWindows.Count, 1, "…底下那扇**还在**（ESC 只吃最上面一层）");
            Check(shell.Windows.TopWindow, probe,
                  "（态三：弹窗关掉）`TopWindow` **落回底下那扇**（= 原版 `ShowPreviousWindow` 之后 `currentWindow` 的角色）");
            Check(probe.CurrentState, WindowState.Open,
                  "…而且它被**带回 `Open`**（🆕 A77㉑①：原版 `CloseWindowCO` → `ShowPreviousWindow` → `TryOpen` 的非 `Closed` 支）"
                  + " —— ⚠️ 本条原来断的是 `Background`：那是**我们缺 `ShowPreviousWindow` 时的偏离**，按原版改成 `Open`");

            shell.Windows.CloseAllWindows();

            // ---- ④🆕 **A217③**：**全屏窗盖在弹窗上**（原版 `OpenWindowCO` 全屏支那一句 = `HideAllWindows()`）----
            //   判据 = `d:/2/tools/decomp_full/WindowsManager__OpenWindowCO.c`（全屏支 `HideAllWindows()`；弹窗支
            //   `ToBackground()`；两支之后统一 `set_CurrentWindow(win)`）+ `WindowsManager__HideAllWindows.c`
            //   + predicate `WindowsManager.__c__.<HideAllWindows>b__49_0.c`（`存活 && state != Closed`）
            //   + `GameWindow__Hide.c`（Slot 9：守卫 → `state = 0` → `SetActive(false)`，**不动表**）。
            //   🔴 **旧实现两处会红**：① 全屏支只 `Close()` 当前主窗 ⇒ 弹窗**照旧可见**（全屏窗盖不住它）；
            //   ② `TopWindow` 那时指向**被盖住的弹窗** ⇒ ESC / 键盘默认选中打错窗。
            shell.Windows.OpenWindow(probe);                 // 全屏窗
            shell.Windows.OpenWindow(pop);                   // 弹窗叠上去
            CheckTrue(pop.CurrentState == WindowState.Open && probe.CurrentState == WindowState.Background,
                      "（前提）叠态就绪：弹窗 `Open`、底下的全屏窗被压到 `Background`");
            var fullGo = new GameObject("KeyProbeFullscreen2");
            var full = fullGo.AddComponent<GameWindow>();
            full.type = WindowType.Fullscreen;
            full.closeOnEsc = true;
            full.placement = WindowsPlacement.Canvas;
            WindowsManager.AttachToAnchor(full);
            shell.Windows.OpenWindow(full);                  // ← 这一下走的是「全屏支」
            Check(shell.Windows.TopWindow, full,
                  "★ ④-1 全屏窗开起来 ⇒ `TopWindow` = **它自己**（旧实现 = `popUpWindow ?? currentWindow` ⇒ "
                  + "**指向被盖住的那扇弹窗** ⇒ 这一条红 —— ESC 会关错窗）");
            CheckTrue(pop.CurrentState == WindowState.Closed && !popGo.activeSelf,
                      "★ ④-2 …而被盖住的弹窗**被藏起来了**（原版 `HideAllWindows()` ⇒ `state = Closed` + "
                    + "`SetActive(false)`；改坏法：全屏支只 `Close()` 当前主窗 ⇒ 弹窗照旧可见 ⇒ 红）");
            CheckTrue(shell.Windows.openWindows.Contains(pop) && shell.Windows.openWindows.Contains(probe),
                      "★ ④-3 …但两者**都还挂在 `openWindows` 里**（照原版：`Hide()` **不摘表** —— "
                    + "`HideAllWindows` 的 predicate 只排除 `state == Closed` 的；改坏法：让 `Hide()` 顺手摘表 ⇒ "
                    + "关掉上面那扇之后底下那扇**回不来** ⇒ ④-5 红）");
            CheckTrue(pl.KeyCancel(), "★ ④-4 ESC 关掉的是**最上面那扇全屏窗**");
            Check(full.CurrentState, WindowState.Closed, "…它真的关了");
            Check(shell.Windows.TopWindow, pop,
                  "★ ④-5 …顶窗落回**那扇弹窗**（原版 `ShowPreviousWindow` 认的是**列表尾**；"
                  + "改坏法：把顶窗记成「最后一个非弹窗」（A217③ 之前那一格）⇒ 指向底下的全屏窗 ⇒ 红）");
            CheckTrue(pop.CurrentState == WindowState.Open && popGo.activeSelf,
                      "…而且那扇弹窗**又回来了**（`ShowPreviousWindow` ⑥ → 无参 `TryOpen()`；"
                    + "它刚才被 `Hide()` 成 `Closed` ⇒ 走 **`Closed` 支**、**内容被重建** —— 本节的 A437⑤ 有专门一条。"
                    + "⚠️ **2026-10-12（A437）就地订正（铁律 5）**：这里原来写「非 `Closed` 支 …… 走的是 "
                    + "`ReopenFromBackground`」—— A217② 之后**两句都不成立**（那一跳是完整 `TryOpen`、且它此刻是 `Closed`）");

            shell.Windows.CloseAllWindows();

            // ---- ⑤🆕 **A217①**：`Close()` 的首句守卫（原版 `GameWindow__Close.c` 第三句 `if (!activeSelf) return;`）----
            //   🔴 守卫本身**零可观测差异**（今天没有生产路径会在「本来就没开」时调 `Close()`），所以这一段的
            //   靶子是**配它一起加的那条出声** —— 没有它，守卫就是把「兜底」换成「静默留脏」（违红线）。
            var dirtyGo = new GameObject("KeyProbeDirty");
            var dirty = dirtyGo.AddComponent<GameWindow>();
            dirty.type = WindowType.Fullscreen;
            dirty.closeOnEsc = true;
            dirty.placement = WindowsPlacement.Canvas;
            WindowsManager.AttachToAnchor(dirty);
            shell.Windows.OpenWindow(dirty);
            dirtyGo.SetActive(false);                       // 绕过 `Close()`/`Hide()` 直接置灰 = 造出脏现场
            CheckTrue(shell.Windows.openWindows.Contains(dirty) && !dirtyGo.activeSelf,
                      "（前提）脏现场造出来了：物体**不活**、却还挂在 `openWindows` 里");
            var warn217 = new List<string>();
            Application.LogCallback h217 = (msg, stack, type) =>
            {
                if (type == LogType.Warning || type == LogType.Error) warn217.Add(msg);
            };
            Application.logMessageReceived += h217;
            try { dirty.Close(); } finally { Application.logMessageReceived -= h217; }
            CheckTrue(shell.Windows.openWindows.Contains(dirty),
                      "★ ⑤-1 不活的窗调 `Close()` ⇒ **首句守卫直接 return**（照原版）：表里**还留着它**；"
                    + "改坏法：删掉那句守卫 ⇒ 它走完整条记账（摘表 + 触发 `ShowPreviousWindow`）⇒ 这一条红");
            Check(dirty.CurrentState, WindowState.Open,
                  "…它的 `CurrentState` **原样**（= 守卫那一支什么都没写；旧实现会把它改成 `Closed`）");
            CheckTrue(warn217.Exists(m => m.Contains("openWindows")),
                      "★ ⑤-2 …而且**真的出了声**（那条日志必须点出「已经不活却还在 `openWindows` 里」）—— "
                    + "红线的要求：守卫不许把「兜底」换成**静默留脏**（实测这一下共 " + warn217.Count + " 条警告/错误）");
            Object.DestroyImmediate(dirtyGo);
            //   ⑤-3 阴性对照（**A217① 与 ③ 的交叉点**）：`Hide()` 藏起来的窗**也是**「不活 + 在表里」，
            //   但它的 `state` 是 `Closed` ⇒ 按原版那**是合法状态**（`Hide()` 不摘表、`CloseAllWindows()`
            //   逐扇 `Close()` 撞上的就是它）⇒ 调 `Close()` 照样早退，**但一声不吭**。
            var hidGo = new GameObject("KeyProbeHidden");
            var hid = hidGo.AddComponent<GameWindow>();
            hid.type = WindowType.Popup;
            hid.placement = WindowsPlacement.Popup;
            WindowsManager.AttachToAnchor(hid);
            shell.Windows.OpenWindow(hid);
            hid.Hide();                                     // = `HideAllWindows` 对每一扇做的那一步
            CheckTrue(!hidGo.activeSelf && shell.Windows.openWindows.Contains(hid)
                      && hid.CurrentState == WindowState.Closed,
                      "（前提）`Hide()` 之后它**不活、却仍在 `openWindows` 里**、`state` = `Closed`");
            var warn217b = new List<string>();
            Application.LogCallback h217b = (msg, stack, type) =>
            {
                if (type == LogType.Warning || type == LogType.Error) warn217b.Add(msg);
            };
            Application.logMessageReceived += h217b;
            try { hid.Close(); } finally { Application.logMessageReceived -= h217b; }
            CheckTrue(shell.Windows.openWindows.Contains(hid),
                      "…`Close()` 照旧早退（表里还留着它 —— 与 ⑤-1 同一句守卫）");
            CheckTrue(!warn217b.Exists(m => m.Contains("openWindows")),
                      "★ ⑤-3 **阴性对照**：合法藏起来的那一档（`state == Closed`）再调 `Close()` ⇒ **不许出声**"
                    + "（改坏法：把出声的判据放宽成「不活 + 在表里」⇒ `CloseAllWindows()` 会为每一扇藏起来的窗"
                    + "刷一条假警报 ⇒ 这一条红）");
            hidGo.SetActive(true);                          // 收尾：还原成活动，走正常那条路把它收掉
            hid.Close();
            Object.DestroyImmediate(hidGo);

            shell.Windows.CloseAllWindows();

            CheckTrue(!pl.KeyCancel(),
                      "★ 一扇窗都没有时 ESC 什么都不做 —— ⚠️ **这一条钉的是【我们的处境】，不是原版的处境**："
                      + "原版那一刻 `currentWindow` 是**常驻的 `baseMenu`**（`MainMenuWindow.ESCPressed` 覆写成开「退出游戏」弹窗"
                      + " ⇒ 原版那一下**会**开弹窗），而我们的主菜单**不是** `WindowsManager` 的窗（没有 baseMenu 字段）"
                      + "⇒ 「一扇窗都没有」这个处境在原版**不存在**；本行验的只是「`currentWindow == null ⇒ 直接 return`」那一句。"
                      + "📌 **已记账**（原版那条 `baseMenu` 链我们没建）。");

            Object.DestroyImmediate(popGo);
            Object.DestroyImmediate(probeGo);
        }

        // ============================================================ 🆕 A437：`TryOpen` 三档（A217② 的断言）
        //
        // 判据（第一权威 = 反编译；两个重载本地都没有 `.c`，用 VA 反汇编读出 —— 逐句写在
        //   `Shell/WindowsManager.cs` 的 `GameWindow.TryOpen` / `TryOpen()` 那两段注释里，出处
        //   `资料/普查产出_1012/V8_判据补查.md` §A217②）：
        //   `TryOpen` = ① `SetupData` → ② 读 `CurrentState` → ③ 按值分三档：
        //     `Closed`(0)     ⇒ 播音 + `SetActive(true)` + `state=Open` + **`Open()`（只有这一支重建内容）**
        //     `Background`(2) ⇒ 只 `state=Open` + 提前台（**不播音、不 `Open()`**）
        //     `Open`(1)       ⇒ **一个字段都不写就 `return`**
        // 🔴 **为什么这一段非有不可**：A217② 之前我们的 `TryOpen` **恒调 `Open()`**（同窗再开 = 重建内容），
        //   改成三档之后「不重建」是**新行为**，此前**零覆盖**。下面六条**两两成对**：
        //   ①② 断「不该重建时不重建」· ③ 断 `Background` 支只提前台 · ④ 是 ①② 的**阳性对照**
        //   （关过之后**要**重建 —— 缺了它，①② 分不出「不重建」与「根本不再建」）· ⑤ 断 `ShowPreviousWindow`
        //   带一扇 `Closed` 的窗回来**要重建** · ⑥ 断那条「早退」出声**只在该响的时候响**（阴性 + 阳性各一条）。
        Section("A437：`GameWindow.TryOpen` 三档（同窗再开**不重建内容** · `Background` 只提前台 · 只有 `Closed` 才 `Open()`）");
        {
            // 内容节点 = `PromptPopup` 的 `OkButton`（`PromptPopup.Open()` → `Build()` 里建的）。
            // ⚠️ 挑它是因为 A327 那一节已经在用它当「一次 `Build()` 换一批新节点」的探针（同一份判据，⛔ 不另立一套）。
            const string ContentNode = "OkButton";
            shell.Windows.CloseAllWindows();          // 隔离：这一节自己造出 `Background` / `Closed` 各种处境

            // ---- ①② 同窗再开**不重建**（判据 = `Open` 支 `ret`，一个字段都不写）----
            var gA = PromptPopup.Create(shell.Windows, "A437 同窗再开探针", "OK", null, null, null);
            shell.Windows.OpenWindow(gA);             // 态一：`Closed` ⇒ `Open()` ⇒ `Build()`
            var nA = FindChildIn(gA.transform, ContentNode);
            CheckTrue(nA != null, "（前提）A437①：`PromptPopup` 的内容节点 `" + ContentNode + "` 建出来了");
            var markA = new GameObject("A437 标记");   // 我们自己挂上去的「内容还在」标记（`Build()` 一跑就没了）
            if (nA != null) markA.transform.SetParent(nA, false);
            shell.Windows.OpenWindow(gA);             // 再开一次：`CurrentState == Open` ⇒ 原版那一支**早退**
            var nA2 = FindChildIn(gA.transform, ContentNode);
            int idA = nA != null ? nA.GetInstanceID() : 0;
            int idA2 = nA2 != null ? nA2.GetInstanceID() : -1;
            CheckTrue(idA != 0 && idA == idA2,
                      $"★ A437① 同窗再开 ⇒ **内容对象是同一份**（`GetInstanceID`：{idA} / {idA2}）"
                    + " —— 改坏法：把 `GameWindow.TryOpen` 退回「恒 `Open()`」⇒ `Build()` 首句销毁旧子件、"
                    + "同名的新节点出现 ⇒ 这一条红");
            CheckTrue(markA != null && FindChildIn(gA.transform, "A437 标记") != null,
                      "★ A437② …而且**那份内容真的还在**（我们自挂在内容节点上的 `A437 标记` 仍在 —— "
                    + "`Build()` 一跑就会把它连同子件一起销毁）—— 改坏法：把 `Open` 支写成「先清空子件再早退」"
                    + "⇒ ① 仍绿、② 红（两条各管一件事，不是同义反复）");

            // ---- ③ `Background` 支：只提回前台、**不重建** ----
            var gB = PromptPopup.Create(shell.Windows, "A437 背景支探针", "OK", null, null, null);
            shell.Windows.OpenWindow(gB);
            var nB = FindChildIn(gB.transform, ContentNode);
            CheckTrue(nB != null, "（前提）A437③：`PromptPopup` 的内容节点建出来了");
            gB.ToBackground();                        // = 原版 `OpenWindowCO` 对底窗那一句
            Check(gB.CurrentState, WindowState.Background, "（前提）A437③：`ToBackground()` 之后它真的在 `Background` 态");
            shell.Windows.OpenWindow(gB);             // 这一下走 `Background` 支
            Check(gB.CurrentState, WindowState.Open,
                  "★ A437③-a `Background` 的窗再开 ⇒ **被提回前台**（`state` 写回 `Open` —— 原版那一支两句里的第一句）");
            int idB = nB != null ? nB.GetInstanceID() : 0;
            var nB2 = FindChildIn(gB.transform, ContentNode);
            int idB2 = nB2 != null ? nB2.GetInstanceID() : -1;
            CheckTrue(idB != 0 && idB == idB2,
                      $"★ A437③-b …而且**内容没重建**（`Background` 支**不调** `Open()`；`GetInstanceID`：{idB} / {idB2}）"
                    + " —— 改坏法：把 `Background` 支写成也调 `Open()` ⇒ 内容节点被换成新的 ⇒ 红");

            // ---- ④ 阳性对照：`Close()` 之后再开 ⇒ **要重建** ----
            //   （没有它，①② 与「`TryOpen` 压根不再建任何东西」分不开）
            gA.Close();                               // `Closed` 态（物体也不活 —— 与 ③ 的 `Background` 正好分得开）
            Check(gA.CurrentState, WindowState.Closed, "（前提）A437④：`Close()` 之后它真的在 `Closed` 态");
            shell.Windows.OpenWindow(gA);
            var nA3 = FindChildIn(gA.transform, ContentNode);
            int idA3 = nA3 != null ? nA3.GetInstanceID() : -1;
            CheckTrue(idA3 != 0 && idA3 != idA,
                      $"★ A437④ **阳性对照**：`Close()` 之后再开 ⇒ **内容被重建**（新节点；`GetInstanceID`：{idA} → {idA3}）"
                    + " —— 改坏法：把 `Closed` 支写成早退（只剩 `SetActive(true)`）⇒ 这一条红"
                    + "（与 ①② 成对：那两条断「不该重建时不重建」，这一条断「该重建时真重建」）");

            // ---- ⑥ 那条「早退」出声**只在该响的时候响**（阴性 + 阳性各一条）----
            //   判据 = `Shell/WindowsManager.cs` 的 `OpenByState()`：`Open` 支**正常路径是静默早退**（照原版），
            //   只有「`state` 说开着、物体却不活」那个**坏局面**才出声（红线「不许静默失败」）。
            var warnNorm = new List<string>();
            Application.LogCallback hNorm = (msg, stack, type) =>
            {
                if (type == LogType.Warning || type == LogType.Error) warnNorm.Add(msg);
            };
            Application.logMessageReceived += hNorm;
            try { shell.Windows.OpenWindow(gA); } finally { Application.logMessageReceived -= hNorm; }
            CheckTrue(!warnNorm.Exists(m => m.Contains("TryOpen` 早退")),
                      "★ A437⑥ 阴性对照：正常路径（`Open` 态 + 物体活着）再开一次 ⇒ **不许**响那条"
                    + "「`TryOpen` 早退 …inactive」警告（改坏法：把那条警告的判据放宽成「命中 `Open` 支就响」⇒ 红）"
                    + $"（实测这一段共 {warnNorm.Count} 条警告/错误）");

            // 🔴 **2026-10-13（A497）**：造脏现场**之前**先把 `gB` 盖回 `Open` —— 本件就是修「前提被前一步推翻」。
            //   根因：④（本段上面那句 `OpenWindow(gA)`）按 `WindowsManager.cs:861`
            //   （`if (currentWindow != null && currentWindow != win) currentWindow.ToBackground();`）
            //   把**当时的顶窗**（= `gB`）压成了 `Background` ⇒ 直接 `SetActive(false)` 的话，
            //   下面那条前提（`CurrentState == Open`）不成立，`OpenWindow(gB)` 也会命中 **`Background` 支**
            //   （`WindowsManager.cs:470-478`：只 `SetActive(true)` + 写 `state`，**不重建、不出声**）
            //   ⇒ 那条「早退 + 物体不活」的警告**结构上不可能响** = A497 之前的红 4/5。
            //   ✅ 判据 = `Background` 支是**正确行为**（原版那一支就是这个语义），要修的是**夹具没造出它要的处境**。
            //   ⛔ 别把下面那条阳性对照删掉或改成恒绿：它与阴性对照成对，是「这条警告到底会不会响」的唯一鉴别力。
            //   改坏法：删掉这一行 ⇒ 红 4（前提：`CurrentState` 实得 `Background`）+ 红 5（阳性对照 0 条警告）都回来。
            if (gB.CurrentState != WindowState.Open) shell.Windows.OpenWindow(gB);
            gB.gameObject.SetActive(false);           // 绕过 `Close()`/`Hide()` 直接置灰 = 造出脏现场
            CheckTrue(gB.CurrentState == WindowState.Open && !gB.gameObject.activeSelf,
                      "（前提）A437⑥：脏现场造出来了（`CurrentState` 说开着、物体却不活；"
                      + $"实测 `CurrentState = {gB.CurrentState}` · `activeSelf = {gB.gameObject.activeSelf}`）"
                      + "｜🔴 这一条红了先看 `CurrentState`：**若是 `Background`**，说明「盖回 `Open`」那一句被绕过了"
                      + "（`OpenWindow` 会把当时的顶窗压到背景，`WindowsManager.cs:861`）—— A497 之前那条红就是它");
            var warnDirty = new List<string>();
            Application.LogCallback hDirty = (msg, stack, type) =>
            {
                if (type == LogType.Warning || type == LogType.Error) warnDirty.Add(msg);
            };
            // 开之前先记一份状态：`Open` 支与 `Background` 支**开完都是 `Open`** ⇒ 事后回读分不出走了哪一支，
            //   而「走了哪一支」正是这条阳性对照要鉴别的东西（A497 诊断 §2.2 第 5 跳）。
            var dirtyStateBefore = gB.CurrentState;
            Application.logMessageReceived += hDirty;
            try { shell.Windows.OpenWindow(gB); } finally { Application.logMessageReceived -= hDirty; }
            CheckTrue(warnDirty.Exists(m => m.Contains("TryOpen` 早退")),
                      "★ A437⑥ 阳性对照：脏现场再开 ⇒ **那条警告必须响**（红线「不许静默失败」）"
                    + $"（实测这一段共 {warnDirty.Count} 条警告/错误；开之前 `CurrentState = {dirtyStateBefore}`"
                    + " —— 这一格若是 `Background`，说明脏现场没造出来、这一段走的**不是** `Open` 支）"
                    + " —— 与上一条合起来 = **分得出两种情形**，不是一条恒绿或恒红的摆设"
                    + "｜改坏法：删掉上面那句「盖回 `Open`」⇒ `gB` 停在 `Background` ⇒ 这一下命中 `Background` 支"
                    + "（只激活 + 写 `state`、不出声）⇒ 本条与上面那条前提**同时红**（= A497 之前的红 4/5）");

            // 收尾：这一节自己的探针全清掉（`gB` 是**故意留成不活**的 ⇒ 先还原再关，同 A217① ⑤-3 的做法）
            if (gB != null) gB.gameObject.SetActive(true);
            shell.Windows.CloseAllWindows();
            Object.DestroyImmediate(gA.gameObject);
            Object.DestroyImmediate(gB.gameObject);

            // ---- ⑤ `ShowPreviousWindow` ⑥：带 `Closed` 的窗回来 ⇒ **要重建** ----
            //   判据 = 原版 `WindowsManager__ShowPreviousWindow` 末句调 `GameWindow.TryOpen` 的**无参**重载
            //   （`0x1808767C1`–`0x1808767D0` 那几句）；而 `Hide()` 藏起来的窗 `state == Closed`
            //   ⇒ 回来时走的就是**重建**那一支 —— 这一档在 A217③ 之后**很常见**（全屏窗开一次就会藏一批）。
            var gC = PromptPopup.Create(shell.Windows, "A437 被藏起来的探针", "OK", null, null, null);
            shell.Windows.OpenWindow(gC);
            var nC = FindChildIn(gC.transform, ContentNode);
            CheckTrue(nC != null, "（前提）A437⑤：被藏那一扇的内容节点建出来了");
            // ⚠️ **这个 id 必须在 `gD.Close()` 【之前】取** —— 那一跳会重建内容、把 `nC` 销毁
            //    （销毁之后 Unity 的假 null 会让 `nC != null` 为假 ⇒ 取到 0 ⇒ 下面那条会**假红**）。
            int idC = nC != null ? nC.GetInstanceID() : 0;
            var gDGo = new GameObject("A437 顶窗（全屏）");
            var gD = gDGo.AddComponent<GameWindow>();
            gD.type = WindowType.Fullscreen;
            gD.placement = WindowsPlacement.Canvas;
            WindowsManager.AttachToAnchor(gD);
            shell.Windows.OpenWindow(gD);             // 全屏支 ⇒ `HideAllWindows()` ⇒ `gC` 被 `Hide()`
            CheckTrue(gC.CurrentState == WindowState.Closed && !gC.gameObject.activeSelf
                      && shell.Windows.openWindows.Contains(gC),
                      "（前提）A437⑤：全屏窗开起来 ⇒ 底下那扇**被藏起来**（`state = Closed` + 不活 + 仍在 `openWindows` 里）");
            gD.Close();                               // 关掉上面那扇 ⇒ `NotifyClosed` → `ShowPreviousWindow()` → 列表尾 `TryOpen()`
            CheckTrue(gC.CurrentState == WindowState.Open && gC.gameObject.activeSelf,
                      "（前提）A437⑤：上面那扇关掉之后，底下那扇**回来了**（回 `Open` + 物体活）");
            var nC2 = FindChildIn(gC.transform, ContentNode);
            int idC2 = nC2 != null ? nC2.GetInstanceID() : -1;
            CheckTrue(idC != 0 && idC2 != 0 && idC2 != idC,
                      $"★ A437⑤ 被 `Hide()` 成 `Closed` 的窗经 `ShowPreviousWindow` 回来 ⇒ **内容被重建**（新节点；"
                    + $"`GetInstanceID`：{idC} → {idC2}）—— 改坏法：把 `ShowPreviousWindow` ⑥ 退回"
                    + "`prev.ReopenFromBackground()`（它只做「`SetActive(true)` + `state=Open`」、**不处理 `Closed`**）"
                    + "⇒ 这一条红（而且画面上那扇窗回来之后**是空的**）");
            Object.DestroyImmediate(gDGo);
            shell.Windows.CloseAllWindows();
            Object.DestroyImmediate(gC.gameObject);
        }

        // ---------------- ⑤·e2 🆕 A94：吸收层**不是按钮** —— `PointerLayer` 那四处配套（A139）
        //
        // 🔴 **为什么单开一段**：A94 相 1 在 `Shell/PointerLayer.cs` 补了四处「吸收层不算按钮」
        //    （`Select` / `SelectFirst` / `FindInDirection` / `ButtonCountForTest`），这四处**一条断言都没有**
        //    —— 谁把它们删掉，本文件照样全绿。相 1 自己写明这四处的性质是「**不改会静默出错**」
        //    （`资料/普查产出_1006/甲4_A94_相1.md` §3·1），相 2（`甲4b_A94_相2.md` §四·5）也**明说这四处它没写断言**
        //    ⇒ 本段就是来补这个缺口（A139）。
        //
        // 🔴 **判据（原版，逐条实读本工程的 UGUI 源码 `Library/PackageCache/com.unity.ugui@27635d171b1a/`）**：
        //   · **面板不是 `Selectable`** —— 原版窗内面板那颗 `Image` 的 `m_RaycastTarget = 1`，
        //     但它的父链上**没有任何 `Selectable` / `ISelectHandler`**（实读表 → `甲4_A94_相1.md` §2·2）。
        //   · ① 鼠标按下：`StandaloneInputModule.ProcessMousePress`（`StandaloneInputModule.cs:623`）调
        //     `DeselectIfSelectionChanged`（`PointerInputModule.cs:427`）—— 它往父链找 `ISelectHandler`，
        //     找不到 ⇒ `selectHandlerGO == null`，只要那一刻有选中就 `SetSelectedGameObject(null)`
        //     ⇒ **点窗内面板 = 取消选中**（不是「选中一块点不动的面板」）。
        //   · ②③ 方向键：候选表只有 `Selectable`（`Selectable.cs:24` 的 `s_Selectables`；
        //     遍历它的就是 `FindSelectable`，`Selectable.cs:794`）⇒ 面板**根本不在候选里**。
        //   · ④ `ButtonCountForTest` 自称「= `FindInDirection` 的候选集大小」⇒ 必须与 ③ 同一判据。
        //   ⇒ 「面板不是按钮」在原版是**结构性的**；我们那份 `WindowButton.absorbOnly` 就是它的等价物。
        //   ⚠️ 「**第一颗**」的定义是**我们挑的**（原版 `firstSelectedGameObject` 本地查不到，见 `PointerLayer` ①）
        //     —— 但 ② 断的那件事与定义无关：**不论取谁，原版都取不到面板**（它压根不是 `Selectable`）。
        Section("A94：吸收层**不是按钮** —— `Select` 按 null · `SelectFirst` 跳过 · `FindInDirection` 跳过 · `ButtonCountForTest` 不数");
        {
            var plA = PointerLayer.Instance;
            shell.Windows.CloseAllWindows();               // 隔离：这一段要「场上只有我这几颗」

            Check(PointerLayer.ButtonCountForTest, 0,
                  "前置（隔离）：这一段开工前场上**一颗可导航按钮都没有**（A49 那一段收尾已 `CloseAllWindows` + 销毁探针）"
                  + " —— 不为 0 的话下面「第一颗 / 正下方那颗 / 只 2 颗」都不唯一");

            var absGo = new GameObject("AbsorbProbe");
            var absWin = absGo.AddComponent<GameWindow>();
            absWin.type = WindowType.Fullscreen;
            absWin.closeOnEsc = false;
            absWin.placement = WindowsPlacement.Canvas;
            WindowsManager.AttachToAnchor(absWin);

            // 🔴 **吸收层建在最前**（= 窗根的**第一个子件** ⇒ `GetComponentsInChildren` 里**层级序第一颗**）——
            //    原版面板的处境正是这样：它在窗里排得很靠前，却**不是 `Selectable`**。
            //    矩形是**我们挑的探针几何**（⛔ 与「原版面板矩形」无关 —— 那一条归各宿主窗的 `CheckAbsorbRule` 管）：
            //    中心 (600,550)、600×500，**夹在 `NavP` 与 `NavQ` 中间**（用途见 ③）。
            //    档照公共件的规矩传「压暗档 2900 / 内容档 3000」⇒ 它自己算成 2999（**不触发**档位告警）。
            var absNode = MenuDraw.Absorb(absGo.transform, "AbsorbHit",
                                          new PxRect(300f, 300f, 900f, 800f), 2900, 3000);
            CheckTrue(absNode != null && MenuDraw.WasAbsorb(absNode),
                      "前置：探针的吸收层是**公共件 `MenuDraw.Absorb` 建的**（`MenuDraw.WasAbsorb`）"
                      + " —— 自己 `AddComponent<WindowButton>()` 再手置标志等于绕开被测的那条路");
            var absBtn = absNode != null ? absNode.GetComponent<WindowButton>() : null;
            CheckTrue(absBtn != null && absBtn.absorbOnly,
                      "前置：它那颗 `WindowButton.absorbOnly` **置了位**（没置位的话下面四条会红在错的原因上）");

            // 真按钮两颗：`NavP`（上）· `NavQ`（**远**下方）。摆位是 ③ 那一条的唯一依据。
            var absHits = new GameObject("Hits").transform;
            absHits.SetParent(absGo.transform, false);
            string firedA = "";
            var pBtn = MakeNavButton(absHits, "NavP", 400f, 200f, 80f, 40f);
            pBtn.onClick = () => firedA += "NavP ";
            var qBtn = MakeNavButton(absHits, "NavQ", 400f, 900f, 80f, 40f);
            qBtn.onClick = () => firedA += "NavQ ";

            // ② `SelectFirst`：`WindowsManager.OpenWindow` → `SelectFirstIn`（开窗默认选中）
            shell.Windows.OpenWindow(absWin);
            Check(SelName(plA), "NavP",
                  "★ ② 开窗 ⇒ 默认选中落到**层级序第一颗真按钮** `NavP`，**不是**排在它前面的那颗吸收层"
                  + "（判据 = 原版面板不是 `Selectable` ⇒ `ActivateModule` 那一刻不可能取到它；"
                  + "改坏法：把 `PointerLayer.SelectFirst` 里那句 `if (b.absorbOnly) continue;` 删掉 ⇒ 这条立刻红）");

            // 前置（**开窗之后**量：`FindObjectsByType` 默认不收没激活的件、`HitQuad` 也要求那颗是活的）
            // ⚠️ 这里只数**吸收层**（= 本段自己建的那一颗）—— ⛔ 别去断「场上总共几颗 `WindowButton`」：
            //    那个数含「没有命中 quad 的件」（外壳那棵常驻树里有没有，本段不负责），断死了会**假红**。
            var wbAll = Object.FindObjectsByType<WindowButton>(FindObjectsSortMode.None);
            int nAbs = 0;
            for (int i = 0; i < wbAll.Length; i++) if (wbAll[i].absorbOnly) nAbs++;
            Check(nAbs, 1, "前置：场上**正好一颗**吸收层（= 刚建的 `AbsorbHit`）"
                  + " —— 它不在场的话下面 ②③④ 三条都成了空断（没有可跳过的对象）");
            // 前置：三颗各自的**覆盖点**（① 与 ③ 的几何前提 —— 吸收层真的夹在 `NavP` 与 `NavQ` 之间）。
            // 🔴 这三条同时是 ④ 的前提：「三颗**都进得了命中表**」（用的是生产那条命中路，不是我另算一遍）
            //    ⇒ 吸收层只要不被跳过，`ButtonCountForTest` 就**必然**是 3。
            CheckTrue(plA.ButtonAt(400f, 200f) == pBtn, "前置：`NavP` 的中心 (400,200) 上命中的是它自己");
            CheckTrue(plA.ButtonAt(400f, 900f) == qBtn, "前置：`NavQ` 的中心 (400,900) 上命中的是它自己");
            CheckTrue(plA.ButtonAt(600f, 550f) == absBtn,
                      "前置：吸收层矩形的中心 (600,550) 上命中的**确实是那颗吸收层**（`ButtonAt` 只做命中、不派发）"
                      + " —— 不然下面 ① 那条会退化成「按到了 null 也是清空选中」的**假绿**");

            // ① `Select`：鼠标按在那块面板上（`PressAt` = `Update` 里「按下」那一路的同一条函数）
            Check(SelName(plA), "NavP", "前置：按下前选中在 `NavP`（下面那条断的是「按下**之后**」）");
            plA.PressAt(600f, 550f);                      // 吸收矩形 (300,300)-(900,800) 的**中心**，那一处没有真按钮
            Check(SelName(plA), "(无)",
                  "★ ① 鼠标按在**窗内面板**上 ⇒ **选中被清成 null**（原版 `ProcessMousePress` →"
                  + " `DeselectIfSelectionChanged`：面板父链上没有 `ISelectHandler` ⇒ `SetSelectedGameObject(null)`）"
                  + " —— 不是「选中一块点不动的面板」。改坏法：把 `PointerLayer.Select` 里那句"
                  + " `if (b != null && b.absorbOnly) b = null;` 删掉 ⇒ 这条立刻红");
            CheckTrue(absBtn != null && !absBtn.Pressed,
                      "…而且吸收层**没进 `Pressed` 态**（原版面板没有 `Selectable`；"
                      + "`WindowButton.Press` 里那句 `if (absorbOnly) return;` 拿掉这条就红）");
            plA.ReleaseAt(600f, 550f);                    // 收尾：把 `_pressed` 归位（这一下什么都不做）

            plA.PressAt(400f, 900f);                      // `NavQ` 中心 —— **对照**
            Check(SelName(plA), "NavQ",
                  "…**对照**：同样这一下按在**真按钮**上 ⇒ 选中照常跟过去（两条合起来才分得出"
                  + "「按面板 = 取消选中」与「选中这条链整个坏了」两种状态）");
            plA.ReleaseAt(400f, 900f);
            Check(firedA, "NavQ ", "…（顺带：那一颗的 `onClick` 真的派发了 ⇒ 指针那一路是活的）");

            // ③ `FindInDirection`：从 `NavP` 往**下**。
            //    按 `PointerLayer` 那条原版公式（`dot / |v|²`）算，**吸收层本来会赢** ——
            //    它是 330/148900 ≈ **0.00222**，而 `NavQ` 是 680/462400 ≈ **0.00147**（吸收层离得近、又在正下方）
            //    ⇒ 那句跳过一拿掉，方向键就跳到面板中心（一块点不动的面板）上。
            plA.Select(pBtn);
            plA.KeyMove(0f, -1f, 500f);                   // ↓（`time` 要给足：原版 `m_RepeatDelay = 0.5` 那条节流）
            Check(SelName(plA), "NavQ",
                  "★ ③ 方向键 ↓ = 到**正下方那颗真按钮** `NavQ`，**不是夹在中间的吸收层**"
                  + "（判据 = 原版 `FindSelectable` 的候选表只有 `Selectable`，`Selectable.cs:794`；"
                  + "改坏法：把 `PointerLayer.FindInDirection` 里那句 `|| b.absorbOnly` 删掉 ⇒ 这条立刻红）");

            // ④ `ButtonCountForTest`（自称「= `FindInDirection` 的候选集大小」⇒ 必须与 ③ 同一判据）
            Check(PointerLayer.ButtonCountForTest, 2,
                  "★ ④ 场上**可导航**的按钮 = **2 颗**（`NavP` / `NavQ`）—— 吸收层**不算**"
                  + "（改坏法：把 `PointerLayer.ButtonCountForTest` 里那个 `&& !b.absorbOnly` 删掉 ⇒ 变 3 ⇒ 红）；"
                  + "它同时是各宿主「场上只有 N 颗」那类**前置断言**的依据（本文件 A49 那段就有一处 `5`）");

            shell.Windows.CloseAllWindows();
            Object.DestroyImmediate(absGo);
        }

        // ---------------- ⑤·k 🆕 2026-10-09（A221④）：吸收层的档位告警**按窗记账** —— 判别力对照
        //
        // 🔴 **为什么非要有这一段**：A221④ 把 `MenuDraw.Absorb` 的「档不合法」告警从**全局累积计数器**
        //    （`AbsorbTierWarns`，**从不复位**）换成**挂在那一颗吸收层节点上的标记**
        //    （`AbsorbTierWarned(node, out why)`，照 `ShadeHitTierWarned` 的形状）。
        //    ⚠️ 但 5 份宿主窗的 `CheckAbsorbRule` 在自检里**永远是绿的**（没有一扇窗真的报过警）
        //    ⇒ **它们分不出「按窗记账」与「全局计数」这两种实现**（= 弱断言分不出两种状态）。
        //    这一段补的就是那条判别力：**故意造一颗档不合法的吸收层**，然后断言
        //    ① 它**自己**报了警、且 `why` 带正文；② **同一时刻**另一颗合法的仍是**干净**的
        //    ⇒ 退回「一个全局计数器」那种写法时 ② 会红。
        // ⚠️ 本段会**故意打一条 `Debug.LogWarning`**（`MenuDraw.Absorb` 那条）—— **那是预期的**；
        //    坏档那颗的名字里写了「自检故意造」，便于与真告警区分。
        // ⚠️ 收尾照 ⑤·j 那段的做法（`CloseAllWindows` + `DestroyImmediate`），不留给后面的段。
        Section("A221④：吸收层档位告警**按窗记账**（故意的坏档只报在它自己那颗上 · 好的那颗不受影响）");
        {
            var wgo = new GameObject("AbsorbTierProbe");
            var tierWin = wgo.AddComponent<GameWindow>();
            tierWin.type = WindowType.Fullscreen;
            tierWin.closeOnEsc = false;
            tierWin.placement = WindowsPlacement.Canvas;
            WindowsManager.AttachToAnchor(tierWin);

            // ① 合法的：`qShade 2900 / qContentMin 3000` ⇒ 算出来 2999 > 2900 ⇒ **不该**报警
            var okNode = MenuDraw.Absorb(wgo.transform, "AbsorbTierOk",
                                         new PxRect(300f, 300f, 900f, 800f), 2900, 3000);
            // ② **故意的坏档**：`qShade == qContentMin` ⇒ 算出来 `qContentMin − 1 <= qShade` ⇒ **必报警**
            var badNode = MenuDraw.Absorb(wgo.transform, "AbsorbTierBad（自检故意造）",
                                          new PxRect(1000f, 300f, 1600f, 800f), 3000, 3000);

            CheckTrue(okNode != null && badNode != null,
                      "前置：两颗吸收层都建出来了（任一颗没建出来的话下面两条红在错的原因上）");

            string okWhy, badWhy;
            bool okWarned = MenuDraw.AbsorbTierWarned(okNode, out okWhy);
            bool badWarned = MenuDraw.AbsorbTierWarned(badNode, out badWhy);
            CheckTrue(badWarned && !string.IsNullOrEmpty(badWhy),
                      "★ ① 那颗**故意传成 `qShade == qContentMin`** 的吸收层**报了警**，而且 `why` 带正文"
                      + "（⛔ 不是只回一个布尔 —— 文案要能指认「哪一颗、算出来几档」）"
                      + (badWarned ? "；实得：" + badWhy : "；⚠️ 它没报 ⇒ 告警那条路断了"));
            CheckTrue(!okWarned,
                      "★ ② **同一时刻**另一颗（2900 / 3000 ⇒ 算成 2999）**仍然是干净的** —— "
                      + "这一条就是「**按窗记账**」的判别力：退回「一个全局计数器」那种写法时它会红"
                      + (okWarned ? "；⚠️ 实得被带红了，告警正文：" + okWhy : ""));

            shell.Windows.CloseAllWindows();
            Object.DestroyImmediate(wgo);
        }

        // ---------------- ⑤·j 🆕 2026-10-07（A77㉑①②）：ESC 的三道原版门槛 + 关掉最上面那扇后底窗回 `Open`
        //
        // 🔴 **判据（**全部是反编译方法体 / 反汇编**；逐条写在实现那一侧，⛔ 别在这里抄第二份）**：
        //   · ① **输入系统启用** = `GameWindow.ESCPressed` 的**第一道**门槛
        //     （`EventSystemController.Instance.eventSystem.enabled`；反汇编 VA `0x180835ab0`，
        //      证据链 → `Shell/WindowsManager.cs` 的 `ESCPressed` 那段 + 报告 §2·①）。
        //     ⇒ 我们这一侧 = `PointerLayer.InputEnabled`（**我们挑的等价物** —— 本仓没有 UGUI `EventSystem`）。
        //   · ② **`IsOpen()`** = `WindowsManager.Update` 先问的那一跳（虚表 `0x1f8`，`0x68 == 1`；
        //     `GameWindow__IsOpen.c`）⇒ 我们这一侧 = `PointerLayer.KeyCancel` 的第二跳。
        //     （第三道门槛 `closeOnEsc` 的两态对照在 ⑤·e，不在这里重复。）
        //   · ③ **`ShowPreviousWindow`** = `CloseWindowCO` 在「关掉的正是当前窗」时那一跳 ⇒ 关掉弹窗之后，
        //     底下那扇从 `Background` **回到 `Open`**（`TryOpen` 的非 `Closed` 支，不重建内容）。
        // 🔴 **去自证的写法**：每条 ★ 都做成**两态对照**（同一扇窗、只改那一格 ⇒ 期望值翻面），
        //    ⛔ 期望值全是 `true` / `false` / 名字字面量，**不从被测实现里读**。
        // ⚠️ 本段**不建任何按钮**（只用窗）⇒ 不碰「场上只有 N 颗」那类前置，收尾也无需还原按钮。
        Section("A77㉑：ESC 三道原版门槛（输入层启用 / `IsOpen` / `closeOnEsc`）+ 关掉最上面那扇后底窗回 `Open`");
        {
            var plG = PointerLayer.Instance;
            Check(PointerLayer.InputEnabled, true, "前置：输入层出厂是**启用**的（原版那台 `EventSystem` 出厂也是启用的）");
            Check((int)WindowState.Closed, 0, "原版 `WindowState` 枚举值：`Closed = 0`（`WindowState.cs` 实读）");
            Check((int)WindowState.Open, 1, "…`Open = 1` —— 原版 `IsOpen()` 的判据就是字面量 `0x68 == 1` ⇒ **中间不许再插一枚**"
                  + "（改坏法：把 `Opening` 加回枚举中间 ⇒ 这一条立刻红）");
            Check((int)WindowState.Background, 2, "…`Background = 2`（`GameWindow.ToBackground` 反汇编写的就是 `[this+0x68] = 2`）");

            shell.Windows.CloseAllWindows();

            var aGo = new GameObject("GateProbeA");
            var a = aGo.AddComponent<GameWindow>();
            a.type = WindowType.Fullscreen; a.closeOnEsc = true; a.placement = WindowsPlacement.Canvas;
            WindowsManager.AttachToAnchor(a);
            var p1Go = new GameObject("GateProbeP1");
            var p1 = p1Go.AddComponent<GameWindow>();
            p1.type = WindowType.Popup; p1.closeOnEsc = true; p1.placement = WindowsPlacement.Popup;
            WindowsManager.AttachToAnchor(p1);
            var p2Go = new GameObject("GateProbeP2");
            var p2 = p2Go.AddComponent<GameWindow>();
            p2.type = WindowType.Popup; p2.closeOnEsc = true; p2.placement = WindowsPlacement.Popup;
            WindowsManager.AttachToAnchor(p2);

            // ---- ① 输入层门槛（原版第一道）：同一扇窗、只翻 `InputEnabled` 这一格 ⇒ 期望值翻面
            shell.Windows.OpenWindow(a);
            Check(a.CurrentState, WindowState.Open, "前置：全屏探针窗开着");
            PointerLayer.SetInputEnabled(false);
            CheckTrue(!plG.KeyCancel(),
                      "★ ① 输入层**停用** ⇒ ESC **什么都不做**（原版 `GameWindow.ESCPressed` 的第一道门槛 = "
                      + "`EventSystemController.Instance.eventSystem.enabled`）");
            Check(a.CurrentState, WindowState.Open, "…窗还开着（与下一条合起来 = **分得出两种状态**，不是「永远关不掉」）");
            PointerLayer.SetInputEnabled(true);
            CheckTrue(plG.KeyCancel(), "★ ① 输入层**恢复** ⇒ 同一扇窗 ESC **关得掉**（两态对照）");
            Check(a.CurrentState, WindowState.Closed, "…它真的进了 `Closed` 态");
            shell.Windows.CloseAllWindows();

            // ---- ② `IsOpen()` 门槛（原版 `WindowsManager.Update` 先问的那一跳）
            shell.Windows.OpenWindow(a);
            CheckTrue(a.IsOpen(), "前置：`Open` 态下 `IsOpen()` 为真（原版那句 `0x68 == 1`）");
            a.ToBackground();          // = 原版 `OpenWindowCO` 对底窗那一句 ⇒ 复现「顶窗不在 `Open` 态」的处境
            Check(a.CurrentState, WindowState.Background, "前置：把它压在 `Background`（原版那种处境的等价物）");
            Check(shell.Windows.TopWindow, a, "前置：它**仍是 `TopWindow`** ⇒ 下一条不是靠「压根没有顶窗」蒙对的");
            CheckTrue(!a.IsOpen(), "…此刻 `IsOpen()` 为假（两态分得开）");
            CheckTrue(!plG.KeyCancel(),
                      "★ ② 顶窗**不在 `Open` 态** ⇒ ESC 什么都不做"
                      + "（原版 `WindowsManager.Update`：`IsOpen()` 为假直接 return，**不调** `ESCPressed`）");
            Check(a.CurrentState, WindowState.Background, "…它还停在 `Background`（不是被关掉）");
            // 🔴 **2026-10-12（A437）就地换掉直调**：这里原来是 `a.ReopenFromBackground()` —— 它当时是
            //   「原版 `TryOpen` 非 `Closed` 支」的**替身**，而 A217② 已把那一支并回 `TryOpen`
            //   ⇒ 生产路径（`ShowPreviousWindow` ⑥）调的是**无参** `TryOpen()`。自检**走生产那条路**更有鉴别力
            //   （同族先例：`ShowPreviousWindow` 在 `Warpforge_code` 里是 `private`，自检**不直调**它）。
            //   ⚠️ 这一处是 `ReopenFromBackground` 的**最后一个调用点**；本行换掉之后那个方法**已无人调用**
            //   （先例：`CollectionScene` 的本地 `MakeHolder` —— 留着当形状存档，⛔ 别顺手删，删了要另开一件）。
            //   ⚠️ 语义逐位相同：它此刻在 `Background` 态 ⇒ `TryOpen()` 走 `Background` 支
            //   （`SetActive(true)` + `state = Open`），与 `ReopenFromBackground()` 逐句一样。
            a.TryOpen();                                   // = 原版那一跳：无参重载，只回 `Open`、不重建内容
            Check(a.CurrentState, WindowState.Open, "…无参 `TryOpen()` 把它带回 `Open`（原版那一支唯一写的一句）");
            CheckTrue(plG.KeyCancel(), "★ ② 回到 `Open` 之后 ESC **关得掉**（两态对照 —— 与上面那条只差这一格）");
            Check(a.CurrentState, WindowState.Closed, "…它真的关了");
            shell.Windows.CloseAllWindows();

            // ---- ③ `ShowPreviousWindow`：关掉最上面那扇 ⇒ 底下的**回 `Open`**（不是停在 `Background`）
            shell.Windows.OpenWindow(a);
            shell.Windows.OpenWindow(p1);
            Check(a.CurrentState, WindowState.Background, "前置：弹窗开 ⇒ 主窗被压到 `Background`（原版 `OpenWindowCO` 对弹窗那一支）");
            Check(p1.CurrentState, WindowState.Open, "前置：弹窗自己是 `Open`");
            CheckTrue(plG.KeyCancel(), "★ ③-1 ESC 关掉的是弹窗");
            Check(p1.CurrentState, WindowState.Closed, "…弹窗关了");
            Check(a.CurrentState, WindowState.Open,
                  "★ ③-2 **底下的主窗回到 `Open`**（原版 `CloseWindowCO` → `ShowPreviousWindow` → `TryOpen` 非 `Closed` 支）；"
                  + "改坏法：把 `WindowsManager.NotifyClosed` 里那句 `if (wasTop) ShowPreviousWindow();` 删掉 ⇒ 这里回到 `Background` ⇒ 红");
            CheckTrue(plG.KeyCancel(), "★ ③-3 再按一次 ESC ⇒ 关掉的正是这扇主窗（原版「关掉弹窗后再按 ESC 关底窗」那条链）");
            Check(a.CurrentState, WindowState.Closed, "…主窗也关了（⇒ ② 那道门槛没把底窗变成「关不掉」）");

            // ---- ④ 两层弹窗叠着：关掉上面那扇 ⇒ 回到**底下那扇弹窗**，不是最底下那扇主窗
            shell.Windows.OpenWindow(a);
            shell.Windows.OpenWindow(p1);
            shell.Windows.OpenWindow(p2);
            Check(p2.CurrentState, WindowState.Open, "前置：第三层 `p2` 开着");
            CheckTrue(plG.KeyCancel(), "★ ④-1 ESC 关掉 `p2`");
            Check(shell.Windows.TopWindow, p1, "★ ④-2 顶窗回到 **上一扇**（`p1`），不是最底下那扇主窗");
            Check(p1.CurrentState, WindowState.Open, "…而且 `p1` 被带回 `Open`（原版 `ShowPreviousWindow` 认的是**列表尾**）");
            Check(a.CurrentState, WindowState.Background, "…主窗仍在 `Background`（它上面还有 `p1`）");
            CheckTrue(plG.KeyCancel(), "★ ④-3 再按一次 ESC ⇒ 关掉 `p1`");
            Check(a.CurrentState, WindowState.Open, "…主窗又被带回 `Open`（一层一层退）");

            shell.Windows.CloseAllWindows();
            PointerLayer.SetInputEnabled(true);            // 兜底（① 里已还原过一次）
            Object.DestroyImmediate(p2Go);
            Object.DestroyImmediate(p1Go);
            Object.DestroyImmediate(aGo);
        }

        // ---------------- ⑤·f 🆕 A78①：非 `MenuWindowBase` 族怎么够到软边 —— 聊天窗 `Chat Tab/Viewport` (0,22)
        //
        // 🔴 **待办原来那句「给它加一行 `ClipSoftness`」是接不上的**：`ChatPanel : GameWindowWithTabs
        //   : GameWindow : MonoBehaviour`，而 `Clip` / `ClipSoftness` / `ClipPad` 三兄弟**当年只长在
        //   `MenuWindowBase` 上**（🔴 **2026-10-10 订正（A194）**：三兄弟**已上移到 `GameWindow`**（A78② 落地）
        //   ⇒ **今天够得着了**；⚠️ 本窗**仍走「逐件传」是 A78① 的裁定、不是回归**）（按**名字**找那三个字段 —— ⛔ 别写行号，它会过期）⇒ 本窗既没有那个字段、也没处设。
        //   ✅ **但软边的入口本来就在 `MenuDraw`**（`Rect` / `Nine` / `ClipText` 都收 `clipSoftness`，
        //   内部走 `ApplySoftEdges`）⇒ 本窗走**逐件传**（它连 `clip` 也是逐件传的，见 `ChatTab`），
        //   ⛔ 不新写第二份机制。判据：`MenuDraw` 那一份是全工程唯一一份实现。
        //   ⚠️ 全量表里 `GameWindow` 族（`bundle_generalgamewindows_assets_all` 那 5 个 `RectMask2D`）
        //   软边**全是 (0,0)** ⇒ 今天只有本窗需要这条路。
        //
        // 判据（原版真值，逐字实读）= `assets_full/bundle_mainmenualwaysloaded_assets_all/MonoBehaviour/
        //   MonoBehaviour_-7904774033703794794.json` —— 那个 `RectMask2D` 挂的 GO 就是 `Chat Tab/Viewport`：
        //   `m_Softness = {x:0, y:22}` · `m_Padding = (0,0,0,0)` · `m_Enabled = 1`
        //   （整包只有这 1 条；全量表 → `d:/4/_tmp_view/q1_rm2d.txt` 的 `bundle_mainmenualwaysloaded_assets_all` 一节）。
        // ⇒ 下面两条切线**全部由那个 22 算出来**（视口矩形先按原档字面量钉住：613.88,161 → 1813.88,911）：
        //   上带内沿 = **161 + 22 = 183** · 下带内沿 = **911 − 22 = 889**。
        // 🔴 **「真的生效」怎么证明**：软边的实现就是「按带的内沿把这个 quad 切开、带内那格逐顶点 alpha 斜坡」
        //   ⇒ **切线出现在算出来的位置上 = 生效**。两条反向判据：带宽改回 0 ⇒ **一条切线都没有**（空表直接红）；
        //   22 改成别的数 ⇒ 切线不在 183/889 上（`CheckSoftCuts` 逐条比位置）。
        Section("聊天窗 `Chat Tab/Viewport` 的软边 (0,22)（A78① —— 非 `MenuWindowBase` 族走 `MenuDraw` 那条路）");
        {
            shell.Windows.CloseAllWindows();
            SocialData.ChatMessages.Clear();               // 本地没有服务器 ⇒ 这一页出厂就是空的
            var chatW = ChatPanel.Create(shell.Windows);
            shell.Windows.OpenWindow(chatW);
            chatW.tabButtons.Click(0);                     // 让第 0 页**活着**（`RefreshMessages` 只重画 active 的那一页）
            var chatTab = chatW.tabs.Count > 0 ? chatW.tabs[0] as ChatTab : null;
            CheckTrue(chatTab != null, "第 0 页是 `ChatTab`（Global）");
            var chatScr = chatTab != null ? chatTab.RowsScroll : null;
            CheckTrue(chatScr != null, "这一页的滚动区在（软边的裁切边界就是它的 `Viewport`）");
            var chatVp = chatScr != null ? chatScr.Viewport : default(PxRect);
            // 切线是**算出来的** ⇒ 先把视口那三条边按原档字面量钉住（它们错了，183/889 就不是这两个数）
            CheckNear(chatVp.x1, 613.88f, 0.6f, "前置：视口左沿 = 原版 `Chat Tab/Viewport` 的 **613.88**");
            CheckNear(chatVp.y1, 161f, 0.6f, "…上沿 = **161**");
            CheckNear(chatVp.y2, 911f, 0.6f, "…下沿 = **911**");
            // 喂 20 条（60 行高 + 10 行距）：第 1 行压**上带**（166..226 里有 183）、第 11 行压**下带**（866..926 里有 889）
            for (int i = 0; i < 20; i++)
                SocialData.ChatMessages.Add(new SocialData.ChatMessage
                {
                    Channel = "Global", Sender = "SoftProbe" + i, Time = "0d 0h",
                    Text = "soft-" + i.ToString("00"), Mine = false, Height = 0f,
                    AvatarArt = ProfileData.AvatarArt,
                });
            chatW.RefreshMessages();
            CheckTrue(chatTab != null && chatTab.BuiltRows > 1,
                      "消息行真的建出来了（行数为 0 的话下面扫的是一棵空树，那就等于没验）");
            var chatContent = chatTab != null ? chatTab.transform.Find("Viewport/Content") : null;
            CheckTrue(chatContent != null, "`Chat Tab/Viewport/Content` 在（消息行挂它下面）");
            CheckSoftCuts(ScanSoftCuts(chatContent, false, chatVp), new[] { 183f, 889f }, 0.6f,
                          "聊天页的软边（原版 `m_Softness = (0,22)` ⇒ 带内沿 **161+22=183** / **911−22=889**）");
            Check(ScanSoftCuts(chatContent, true, chatVp).Count, 0,
                  "…而**一条竖切线都没有** —— `(0,22)` 的 `x = 0` ⇒ 左右是硬边（原版只渐变上下）");
            Check(MenuDraw.SoftEdgeUvDrifts, 0,
                  "…整页 `_soft` 子块的 uv 面积和恒等于「整张图」（`SoftEdgeUvDrifts` 没涨）");
            // 收尾：清数据 + 关窗（它的压暗层是整屏的，留着会顶掉后面那些真命中路）
            SocialData.ChatMessages.Clear();
            chatW.RefreshMessages();
            chatW.Close();
            Check(chatW.CurrentState, WindowState.Closed, "收尾：聊天窗关掉");
        }

        // ---------------- ⑤·g 🆕 A12①：**求交只剩一份**（`MenuScroll.Intersects` 只把 `Viewport` 绑进 `MenuDraw.Visible`）
        //
        // 🔴 **收口前有两套语义**（这就是本件要抓的）：`MenuDraw.Visible` 判**两轴**；
        //   `MenuScroll.Intersects` 判**只滚动轴**（纵向只判 y、横向只判 x）⇒ **同一个矩形**，
        //   纵向滚动区与横向滚动区会给出**相反**的答案。
        // 判据 = 原版 `RectMask2D`：**四边都裁**（渲染那一面走 `IClipper`、射线那一面走
        //   `IsRaycastLocationValid`，UGUI 源码 `Runtime/UGUI/UI/Core/RectMask2D.cs:178-185`）
        //   ⇒ 「整块在框外」= **两轴都无交集**才为真；任一轴还有交集 ⇒ 原版仍画得出被裁的那一截。
        // ⚠️ 下面那张表里带 ★ 的四条就是**两种语义的唯一分岔点**（整块落在**横轴**框外/落在**纵轴**框外）
        //   —— 表里每条还同时比 `ClipRect`（它的「有没有交集」那一问已转调 `Visible`）。
        // ⚠️ 收口对**可见行为**的影响：19 处构建循环多剔掉的那几块，本来就整块落在 `clip` 外
        //   （`ClipRect` / `Nine` / `Hit` 全转调同一份）⇒ **画不出、也点不到** ⇒ 只是少建几个节点。
        Section("共用件：求交只剩一份（判据 = 原版 `RectMask2D` 四边都裁 · A12①）");
        {
            var vp = new PxRect(100f, 200f, 300f, 400f);           // 夹具：假想视口 200×200
            var vs = MenuScroll.TopAligned(vp, 1000f);             // **纵向**滚动（收藏/榜单那一族）
            var hs = MenuScroll.LeftAligned(vp, 1000f);            // **横向**滚动（锻造轨道那一族）
            CheckTrue(MenuDraw.SameRect(vs.Viewport, vp) && MenuDraw.SameRect(hs.Viewport, vp),
                      "（前提）两个探针滚动区的 `Viewport` 都 == 夹具矩形（否则下面比不过同一件事）");

            CheckIntersect(vs, hs, vp, new PxRect(120f, 220f, 180f, 260f), true,  "整块在框内");
            CheckIntersect(vs, hs, vp, new PxRect(  0f, 220f, 120f, 260f), true,  "左半在框外（压左沿 · 原版仍画得出可见那截）");
            CheckIntersect(vs, hs, vp, new PxRect(280f, 220f, 400f, 260f), true,  "右半在框外（压右沿）");
            CheckIntersect(vs, hs, vp, new PxRect(120f, 100f, 180f, 250f), true,  "上半在框外（压上沿）");
            CheckIntersect(vs, hs, vp, new PxRect(120f, 380f, 180f, 500f), true,  "下半在框外（压下沿）");
            CheckIntersect(vs, hs, vp, new PxRect(  0f, 100f,  90f, 500f), false, "★整块在**左**框外（旧的「只判滚动轴」在纵向区里会放它过）");
            CheckIntersect(vs, hs, vp, new PxRect(320f, 100f, 400f, 500f), false, "★整块在**右**框外（同上）");
            CheckIntersect(vs, hs, vp, new PxRect(  0f, 100f, 400f, 180f), false, "★整块在**上**框外（旧的在横向区里会放它过）");
            CheckIntersect(vs, hs, vp, new PxRect(  0f, 420f, 400f, 500f), false, "★整块在**下**框外（同上）");
            CheckIntersect(vs, hs, vp, new PxRect(100f, 200f, 300f, 400f), true,  "与视口**逐边相等**（不算「在框外」）");
            CheckIntersect(vs, hs, vp, new PxRect(300f, 200f, 400f, 400f), false, "右边缘**贴着**视口右沿（零宽交集）");
            CheckIntersect(vs, hs, vp, new PxRect(100f, 400f, 300f, 500f), false, "下边缘**贴着**视口下沿（零高交集）");
            CheckIntersect(vs, hs, vp, new PxRect(  0f,   0f,  50f,  50f), false, "完全在左上角外");

            // ★ 「只有一份」的**直接**判据：两条入口逐个矩形逐字同答
            //   （任一处改回「只判一根轴」⇒ `diff` 立刻不为 0）
            int diff = 0, checkedN = 0;
            for (float x = 60f; x <= 340f; x += 13f)
                for (float y = 160f; y <= 440f; y += 13f)
                {
                    var r = new PxRect(x, y, x + 37f, y + 23f);     // 37/23 与步长 13 互质 ⇒ 逐格错开边界
                    checkedN++;
                    if (vs.Intersects(r) != MenuDraw.Visible(r, vp)) diff++;
                    if (hs.Intersects(r) != MenuDraw.Visible(r, vp)) diff++;
                }
            CheckTrue(checkedN >= 200, $"（前提）扫描样本够多（实得 {checkedN} 个矩形；少于 200 就是夹具被缩水了）");
            Check(diff, 0, "★ 两条求交入口**逐格同答**（19 处构建循环走 `Intersects`、各处内联走 `Visible`；"
                           + "任一处改回「只判一根轴」⇒ 这里立刻不为 0）");
            // `Visible` / `ClipRect` 的无裁切契约：**没有裁切框 ⇒ 一律可见**（`Clip == null` 的绝大多数时候）
            CheckTrue(MenuDraw.Visible(new PxRect(-500f, -500f, -400f, -400f), null),
                      "`Visible(r, null)` = true（无裁切框 ⇒ 不判 —— `MenuWindowBase` 没设 `Clip` 时走这条）");
            CheckTrue(MenuDraw.ClipRect(new PxRect(-500f, -500f, -400f, -400f), null, out var rawClipped)
                      && MenuDraw.SameRect(rawClipped, new PxRect(-500f, -500f, -400f, -400f)),
                      "`ClipRect(r, null, out o)` = true 且 `o` **原样回 `r`**（同一条契约）");
        }

        // ---------------- ⑤·h 🆕 2026-10-07（A9 / A38①）：活动窗阵营条的软边 —— `Ranked Army Selector/Army Selector/Viewport` **(0,52)**
        //
        // 🔴 **判据（原版真值，逐处实读）**：全量表 `d:/4/_tmp_view/q1_rm2d.txt` 里**三族各一份、
        //   值逐字相同**：`Ranked Army Selector/Army Selector/Viewport`（值 `:299` · 路径 `:300`，独立母版）·
        //   `SkirmishModeEventWindow/…`（`:55` / `:56`）· `RankedEventWindowV2/…`（`:269` / `:270`）——
        //   三条都是 `m_Softness = (0,52)` · `m_Padding = (0,0,0,0)`
        //   ⇒ **只有软边、没有 padding**（⛔ 别把锻造轨道那个 `(10,0,0,0)` 抄过来 —— 铁律 5·c）。
        // 🔴 **本窗不是 `MenuWindowBase` 族**（`LiveOpsEventWindow : GameWindow`）⇒ 够不着 `ClipSoftness`，
        //   照 `ChatPanel` 那条路（⑤·f）**逐件传**：`LiveOpsEventWindow.VpSoft` → `MenuDraw.Rect/Nine` 的
        //   `clipSoftness`。**机制只有 `MenuDraw.ApplySoftEdges` 那一份**，本窗不新写第二份。
        // ⇒ 切线**全部由那个 52 算出来**（视口矩形先按原版字面量钉住：1323.16,218.94 → 1870.28,882.03）：
        //   上带内沿 = 218.94 + 52 = **270.94** · 下带内沿 = 882.03 − 52 = **830.03**。
        // 🔴 **「真的生效」怎么证明**（同 ⑤·f）：软边的实现就是「按渐隐带内沿把这个 quad 切开、带内逐顶点
        //   alpha 斜坡」⇒ **切线出现在算出来的位置上 = 生效**。改坏法：`VpSoft` 改回 `(0,0)` ⇒
        //   **一条切线都没有**（`CheckSoftCuts` 的空表直接红）；52 改成别的数 ⇒ 切线不在 270.94/830.03 上。
        // ⚠️ **命中区故意不吃软边**（`MenuDraw.Hit` 不收这个形参）—— 原版 `m_Softness` 只改渲染
        //   （掩码在 shader 里削 alpha），射线那一面只看矩形 ⇒ 那一条这里**不断**（它本来就该是硬的）。
        Section("活动窗阵营条 `Ranked Army Selector/Army Selector/Viewport` 的软边 (0,52)（A9 / A38①）");
        {
            shell.Windows.CloseAllWindows();
            var sk = SkirmishEventWindow.Create(shell.Windows);
            shell.Windows.OpenWindow(sk);
            CheckTrue(sk != null && sk.CurrentState != WindowState.Closed,
                      "遭遇战窗开出来了（`SkirmishEventWindow` = 三族里我们真建出来的那一扇）");
            var asScr = sk.ArmyScroll;
            CheckTrue(asScr != null, "阵营条的滚动区在（软边的裁切边界就是它的 `Viewport`）");
            if (asScr != null)
            {
                var vp = asScr.Viewport;
                // 先按原档字面量钉住视口四边：它们错了，270.94 / 830.03 就不是这两个数
                CheckNear(vp.x1, 1323.16f, 0.6f, "前置：视口左沿 = 原版 `Army Selector/Viewport` 的 **1323.16**");
                CheckNear(vp.y1, 218.94f, 0.6f, "…上沿 = **218.94**");
                CheckNear(vp.x2, 1870.28f, 0.6f, "…右沿 = **1870.28**");
                CheckNear(vp.y2, 882.03f, 0.6f, "…下沿 = **882.03**");
            }
            var armyContent = sk.ArmyCells.Count > 0 ? sk.ArmyCells[0].parent : null;
            CheckTrue(armyContent != null, "`Army Content` 在（阵营格挂在它下面）—— 阵营格：" + sk.ArmyCells.Count + " 格");
            // （前提）上下两条渐隐带都真有格**横跨**内沿 —— 落不进这两支的话下面那两条切线等于没验。
            //   量的是**命中区**那颗 quad：它**不吃**软边、也没被切开 ⇒ 它的矩形就是整格那块。
            int crossTop = 0, crossBottom = 0;
            foreach (var c in sk.ArmyCells)
            {
                var hNode = FindChildIn(c, "Hit");
                var hq = hNode != null ? hNode.GetComponentInChildren<ImageQuad>() : null;
                float a, b, c2, d;
                if (!QuadPxRect(hq, out a, out b, out c2, out d)) continue;
                if (b < 270.94f && d > 270.94f) crossTop++;
                if (b < 830.03f && d > 830.03f) crossBottom++;
            }
            CheckTrue(crossTop > 0, $"（前提）有 {crossTop} 格**横跨上带内沿 270.94**（= 218.94 + 52）");
            CheckTrue(crossBottom > 0, $"（前提）有 {crossBottom} 格**横跨下带内沿 830.03**（= 882.03 − 52）");
            CheckSoftCuts(ScanSoftCuts(armyContent, false, new PxRect(1323.16f, 218.94f, 1870.28f, 882.03f)), new[] { 270.94f, 830.03f }, 0.6f,
                          "阵营条的软边（原版 `m_Softness = (0,52)` ⇒ 带内沿 **218.94+52=270.94** / **882.03−52=830.03**）");
            Check(ScanSoftCuts(armyContent, true, new PxRect(1323.16f, 218.94f, 1870.28f, 882.03f)).Count, 0,
                  "…而**一条竖切线都没有** —— `(0,52)` 的 `x = 0` ⇒ 左右是硬边（原版只渐变上下）");
            Check(MenuDraw.SoftEdgeUvDrifts, 0,
                  "…阵营条整条 `_soft` 子块的 uv 面积和恒等于「整张图」（`SoftEdgeUvDrifts` 没涨）");
            sk.Close();
            Check(sk.CurrentState, WindowState.Closed, "收尾：活动窗关掉（它的压暗层是整屏的，留着会顶掉后面那些截图）");
        }

        // ---------------- ⑤·i 🆕 2026-10-07（A78②）：裁切状态（`hitPad` + 软边）搬到**共同基类 `GameWindow`**
        //
        // 🔴 **缺口是什么**（判据原文 → `资料/待办判据_1006.md` §A78② · 原始出处 `资料/普查产出_1004/W6审查_共用件.md`）：
        //   「`MenuDraw.Hit` / `DeckCell` 的 **`hitPad`** 参数（= 原版 `RectMask2D.m_Padding`）**只有 `MenuWindowBase`
        //   家族能喂到** —— 今天不成问题（非该族那几扇窗的 mask 除输入框 `(−8,−5,−8,−5)` 外全是 `(0,0,0,0)`），
        //   但这正是**「下一个缺口」的形状**」；判据明写修法 = **把状态挪到 `GameWindow`，⛔ 不是每扇窗各抄一段**。
        //   ⇒ 三兄弟（`Clip` / `ClipSoftness` / `ClipPad`）+ 两份转发（`RenderClip` / `AddHit`）
        //     **2026-10-07 从 `MainMenuSubmenuWindow` 上移到 `GameWindow`**（全工程只声明一份 ⇒ 全族继承得到）。
        // 🔴 **判据（原版把这类状态放在哪一层）**：不是「窗口的字段」，而是**每个视口节点自己挂的 `RectMask2D`
        //   组件**（`m_Padding` / `m_Softness` / `m_Enabled` 三样都在组件上；逐处实读表 → `GameWindow.ClipSoftness`）。
        //   ⇒ 我们的等价物**必须能挂在任意一扇窗上**（任何一扇窗里都可能有视口）—— 这正是它属于 `GameWindow` 的理由。
        // ⚠️ 本节两条判据**都不读被测实现里的数**：
        //   ① **结构**：按**声明处**问（`DeclaredOnly`）—— 「每族/每窗各抄一段」那种改法会在这里红；
        //   ② **行为**：拿一扇**非 `MenuWindowBase` 族**的真窗（`PromptPopup : GameWindow`），喂**原版实读值**
        //      的 pad / 软边，量**真命中区 quad 的渲染矩形**与**软边切线位置**（期望值由 pad 值与探针字面量算出）。
        //      ⛔ 反向那几态都要断（只断一态 = 弱断言，分不出「接上了」与「恰好没接」）；
        //      ⛔ 别拿 `MenuDraw.PaddedHitRect` 自己算出来的值当期望（那就是同义反复）。
        // 🔴 **本件是「搬家」**：既有断言证明「零行为变化」——`MenuWindowBase` 家族那几条在下面 ⑤·b
        //   （`RenderClip` = `Clip` 按 `ClipPad` 内缩 · 左沿 1010 · 反向右移那位）与 `Editor/RewardsScene.cs:1511,1513,1520,2817`
        //   （锻造轨道命中宽 190.762 = 200.762 − 10 / 战役轨道不许被缩）；`AddHit` 那条转发路的调用点
        //   （`CollectionWindow` / `ForgeTab` / `CampaignTab` / `MissionsTab`）签名逐字未动 ⇒ 解析到同一份实现。
        Section("共用件：裁切状态（`hitPad` / 软边）长在共同基类 `GameWindow` 上（A78②）");
        {
            // ---- ① 结构判据：**声明**在 `GameWindow` 上，家族里不许再有第二份
            const System.Reflection.BindingFlags Decl = System.Reflection.BindingFlags.Public
                                                     | System.Reflection.BindingFlags.Instance
                                                     | System.Reflection.BindingFlags.DeclaredOnly;
            const System.Reflection.BindingFlags Inher = System.Reflection.BindingFlags.Public
                                                       | System.Reflection.BindingFlags.Instance;
            CheckTrue(typeof(MainMenuSubmenuWindow).BaseType == typeof(GameWindowWithTabs)
                      && typeof(GameWindowWithTabs).BaseType == typeof(GameWindow),
                      "（前提）继承链 = `MainMenuSubmenuWindow : GameWindowWithTabs : GameWindow`");
            foreach (var fld in new[] { "Clip", "ClipSoftness", "ClipPad" })
            {
                CheckTrue(typeof(GameWindow).GetField(fld, Decl) != null,
                          $"★ `GameWindow` **自己声明**了 `{fld}`（判据原文：「把状态挪到 `GameWindow`」）");
                CheckTrue(typeof(MainMenuSubmenuWindow).GetField(fld, Decl) == null,
                          $"★ …而 `MainMenuSubmenuWindow` **没有**自己的副本（「每族/每窗各抄一段」⇒ 这条立刻红）");
            }
            CheckTrue(typeof(GameWindow).GetProperty("RenderClip", Decl) != null
                      && typeof(GameWindow).GetMethod("AddHit", Decl) != null,
                      "★ 两份**转发**也在基类上：`RenderClip`（渲染那一份裁切，含 `ClipPad`）+ "
                      + "`AddHit`（裸 `Clip` + `ClipPad` → 命中区）—— **声明只有这一份**");
            // 全族逐个问一遍（含弹窗族那批「非 `MenuWindowBase`」的窗）：「任意一扇窗」都够得着 + 谁也不许再抄一份
            int nWin = 0, nBad = 0, nOwn = 0;
            foreach (var t in typeof(GameWindow).Assembly.GetTypes())
            {
                if (!typeof(GameWindow).IsAssignableFrom(t) || t.IsAbstract) continue;
                nWin++;
                if (t.GetField("Clip", Inher) == null || t.GetField("ClipSoftness", Inher) == null
                    || t.GetField("ClipPad", Inher) == null) nBad++;
                if (t != typeof(GameWindow) && (t.GetField("Clip", Decl) != null
                        || t.GetField("ClipSoftness", Decl) != null || t.GetField("ClipPad", Decl) != null)) nOwn++;
            }
            CheckTrue(nWin >= 20, $"（前提）本程序集里具体的 `GameWindow` 子类有 **{nWin}** 个（少于 20 个 = 扫描夹具坏了）");
            Check(nBad, 0, "★ 每一个 `GameWindow` 子类**都继承得到**那三样（含 `PromptPopup` / `SettingsWindow` / "
                         + "`LeaderboardWindow` / `SkirmishEventWindow` 这些**非该族**的窗）");
            Check(nOwn, 0, "★ **没有任何**子类再声明一份 `Clip` / `ClipSoftness` / `ClipPad`"
                         + "（= 「每扇窗各抄一段」那个缺口的判据；今天抄一份上去这条立刻红）");

            // ---- ② 行为：真拿一扇**非 `MenuWindowBase` 族**的窗，喂非零 `hitPad`（原版 mask 的 `m_Padding`）
            shell.Windows.CloseAllWindows();
            var gw = PromptPopup.Create(shell.Windows, "探针（A78②）", "确 定", null, null, null);
            CheckTrue(gw != null && !(gw is MainMenuSubmenuWindow),
                      "★ 探针窗 = `PromptPopup`（`GameWindow` 族的**普通弹窗**，**不是** `MenuWindowBase` 家族）");
            var probe = new GameObject("A78_2_Clip_Probe").transform;
            probe.SetParent(gw.transform, false);
            // 探针矩形 + 两档 pad 值都是**原版字面量**（⛔ 不用我们自己的常量）：
            //   (10,0,0,0) = 锻造轨道 `…/Forge Tab/Rewards Scroll View/Viewport`（正 = 缩小）；
            //   (−8,−5,−8,−5) = 输入框 `/Text Area` 那一族 ×38 处（负 = 扩大）。
            var pr = new PxRect(100f, 200f, 300f, 400f);
            var clipBig = new PxRect(50f, 150f, 350f, 450f);       // 比 `pr` 大一圈：这几态里它不该削任何东西
            System.Func<Vector4, PxRect?, ImageQuad> buildHit = (pad, clip) =>
            {
                gw.ClipPad = pad; gw.Clip = clip;
                MenuDraw.ClearChildren(probe);
                var h = gw.AddHit(probe, "ProbeHit", pr, 3000, null);
                return h != null ? h.GetComponentInChildren<ImageQuad>() : null;
            };
            float a, b, c, d;
            // ① 基线：pad 全 0 ⇒ 命中区就是那个矩形（= 不喂 pad 时那一态的字面行为）
            var q0 = buildHit(Vector4.zero, clipBig);
            CheckTrue(q0 != null, "（前提）探针命中区建出来了 —— `AddHit` 必须带 quad（`PointerLayer` 只认 quad，A26 那个真缺陷）");
            if (q0 != null && QuadPxRect(q0, out a, out b, out c, out d))
            {
                CheckNear(a, 100f, 0.5f, "① 基线（`ClipPad` = 0）：命中区左沿 = **100**（一字不动）");
                CheckNear(b, 200f, 0.5f, "…上沿 = **200**");
                CheckNear(c, 300f, 0.5f, "…右沿 = **300**");
                CheckNear(d, 400f, 0.5f, "…下沿 = **400**");
            }
            // ② 正值 = 缩小：喂原版锻造轨道那个 `(10,0,0,0)`
            //    🔴 **2026-10-08（A188）就地订正**：这一幕原来喂的是 `clipBig`（比 `pr` 大一圈 ⇒ pad 缩完还是
            //    盖住 `pr`）⇒ 旧模型与「pad 压根没接」都给同一个数（110），**分不出两态**。
            //    现在**把 `clip` 的左沿收进 `r` 里面 10px**（= `pr.x1 + 10`）⇒
            //    `V − pad` 的左沿 = 110 + 10 = **120**，而**不喂 pad** 时 `clip` 自己是 **110**
            //    ⇒ 「pad 真生效」与「pad 被丢掉」在这一幕下**读数不同**（判据 = `R ∩ (V − pad)`）。
            var clipPad2 = new PxRect(110f, 150f, 350f, 450f);
            var q1 = buildHit(new Vector4(10f, 0f, 0f, 0f), clipPad2);
            if (q1 != null && QuadPxRect(q1, out a, out b, out c, out d))
            {
                CheckNear(a, 120f, 0.5f, "★ ② `GameWindow` 族的窗喂 `m_Padding = (10,0,0,0)`（原版锻造轨道实读值）"
                                       + " ⇒ 命中区左沿 = `V.x1 + 10` = **120**（`V` 左沿 110 = `clip` 左沿）");
                CheckNear(b, 200f, 0.5f, "…上沿一动不动（`pad.w` = Top = 0）");
                CheckNear(c, 300f, 0.5f, "…右沿仍是 300（`V` 右沿 350 比它宽 ⇒ 不裁）");
                CheckNear(d, 400f, 0.5f, "…下沿一动不动（`pad.y` = Bottom = 0）");
            }
            // ③ 负值 = 扩大：喂原版输入框那一族那个 `(−8,−5,−8,−5)`
            //    🔴 **2026-10-08（A188）就地订正**（同 ② 的理由）：原来喂 `clipBig` ⇒ pad 被盖住、分不出两态。
            //    现在**把 `clip` 四条边各收进 8 / 5**（= `r` 外扩那一对的逆）⇒ `V − pad`（负值 = 扩大）
            //    = **正好 `r`** ⇒ 命中区 = `r` 本身；**不喂 pad** 时命中区 = `clip` 自己（108/205/292/395）。
            var clipPad3 = new PxRect(108f, 205f, 292f, 395f);
            var q2 = buildHit(new Vector4(-8f, -5f, -8f, -5f), clipPad3);
            if (q2 != null && QuadPxRect(q2, out a, out b, out c, out d))
            {
                CheckNear(a, 100f, 0.5f, "★ ③ 喂 `m_Padding = (−8,−5,−8,−5)`（原版**输入框**那一族实读值）⇒ "
                                       + "`V − pad` = 正好 `r` ⇒ 左沿 **100**（= `V` 左沿 108 外扩 8）");
                CheckNear(b, 200f, 0.5f, "…上沿 **200**（`w` = Top = −5 ⇒ `V.y1` 205 外扩 5）");
                CheckNear(c, 300f, 0.5f, "…右沿 **300**（`z` = Right = −8 ⇒ `V.x2` 292 外扩 8）");
                CheckNear(d, 400f, 0.5f, "…下沿 **400**（`y` = Bottom = −5；`PxRect` 是 **y 向下** ⇒ `V.y2` 395 外扩 5）");
            }
            // ④ 反向那一态：pad 清 0 ⇒ 同一条 `AddHit` 回原样（与 ① 同态 ⇒ 单独看它证明不了「位移由 pad 引起」，
            //    真正的**同 `clip` 两态对照**是下面 ④b（对 ②）与 ④c（对 ③）——
            //    🔴 2026-10-08（A188）：②③ 现在各喂**自己的** `clip` ⇒ ④ 这一条不再与它们同场景）
            var q3 = buildHit(Vector4.zero, clipBig);
            if (q3 != null && QuadPxRect(q3, out a, out b, out c, out d))
                CheckNear(a, 100f, 0.5f, "★ ④ 反向：`ClipPad` 清 0 ⇒ 同一条 `AddHit` 回 **100**（`clip` 宽 ⇒ 不裁）");
            // ④b **同场景两态对照（与 ② 同一个 `clip`）**：只把 pad 清 0 ⇒ 左沿回 **110**（= `V` 左沿，
            //    一句 `clip` 都不动）—— 不喂 pad 时 `V − pad` 退化成 `V` 本身 ⇒ 那 10px **只可能**来自 `m_Padding`。
            //    🔴 2026-10-08（A188）：这一条就是「② 的 pad 真生效」的反证面（缺了它，② 与「pad 被丢掉」同值）。
            var q3b = buildHit(Vector4.zero, clipPad2);
            if (q3b != null && QuadPxRect(q3b, out a, out b, out c, out d))
                CheckNear(a, 110f, 0.5f, "★ ④b 反向（与 ② 同一个 `clip`）：pad 清 0 ⇒ 左沿回 **110** = `clip` 左沿"
                                       + "（② 的 120 与这里的 110 合起来才证明那 10px 由 `m_Padding` 引起）");
            // ④c **同场景两态对照（与 ③ 同一个 `clip`）**：pad 清 0 ⇒ 命中区 = `clip` 自己（`clip ⊂ r`）
            var q3c = buildHit(Vector4.zero, clipPad3);
            if (q3c != null && QuadPxRect(q3c, out a, out b, out c, out d))
            {
                CheckNear(a, 108f, 0.5f, "★ ④c 反向（与 ③ 同一个 `clip`）：pad 清 0 ⇒ 左沿 = `clip` 左沿 **108**"
                                       + "（③ 的 100 与这里的 108 合起来才证明负 pad **真的在扩大**）");
                CheckNear(d, 395f, 0.5f, "…下沿 = `clip` 下沿 **395**（③ 那一幕整条 `clip` 就是 `r − pad` ⇒ 清 0 后由 `clip` 说了算）");
            }
            // ⑤ `Clip` 也在这份状态里（同一个三件套）：把裁切框收到比命中区窄 ⇒ 命中区**截到框沿**
            var q4 = buildHit(Vector4.zero, new PxRect(150f, 200f, 400f, 400f));
            if (q4 != null && QuadPxRect(q4, out a, out b, out c, out d))
            {
                CheckNear(a, 150f, 0.5f, "★ ⑤ `Clip` 左沿 150（> 命中区左沿 100）⇒ 命中区**被截到 150**"
                                        + "（与 ① 合起来证明 `Clip` 也在基类上、且照旧只裁到框内）");
                CheckNear(c, 300f, 0.5f, "…右沿仍是 300（`Clip` 右沿 400 比它宽 ⇒ 不裁）");
            }

            // ---- ③ 软边：同一扇窗喂 `ClipSoftness` ⇒ `MenuDraw.ApplySoftEdges` 按带宽**切开**
            // 带宽借原版 `m_Softness` 的一个实读值 **(0,22)**（`Chat Tab/Viewport`；同族的 `(0,25)` 是商店三页的）
            // —— 探针的切线位置**由探针视口那四个字面量 + 这个 22 算出**（⛔ 不读 `ApplySoftEdges` 里的任何数）。
            var vp = new PxRect(1000f, 200f, 1600f, 800f);
            System.Func<Vector2, ImageQuad> buildSoft = soft =>
            {
                gw.ClipPad = Vector4.zero; gw.Clip = vp; gw.ClipSoftness = soft;
                MenuDraw.ClearChildren(probe);
                return gw.DrawRect(probe, CardArt.Solid(), vp, "SoftProbe", 3000);
            };
            var sq1 = buildSoft(new Vector2(0f, 22f));
            CheckTrue(sq1 != null, "（前提）软边探针建出来了（`CardArt.Solid()` 取得到 ⇒ 纯色件不是 null；"
                                 + "建不出来下面那条 `CheckSoftCuts` 会报「一条切线都没有」）");
            CheckSoftCuts(ScanSoftCuts(probe, false, vp), new[] { 222f, 778f }, 0.6f,
                          "`GameWindow` 族窗喂软边 `(0,22)` ⇒ 带内沿 **200+22=222** / **800−22=778**");
            Check(ScanSoftCuts(probe, true, vp).Count, 0,
                  "…而**一条竖切线都没有** —— `(0,22)` 的 `x = 0` ⇒ 左右是硬边（原版只渐变上下）");
            var sq0 = buildSoft(Vector2.zero);                       // 反向那一态：软边清 0
            CheckTrue(sq0 != null, "（前提）清掉软边之后同一张图也建出来了");
            Check(ScanSoftCuts(probe, false, vp).Count, 0,
                  "★ 反向：`ClipSoftness` 清 0 ⇒ **一条切线都没有**（两态合起来才证明那两条切线真由软边引起）");
            Check(MenuDraw.SoftEdgeUvDrifts, 0,
                  "…软边切出来的子块 uv 面积和恒等于「整张图」（`SoftEdgeUvDrifts` 没涨）");

            // 收尾：状态清干净 + 探针与窗都拆掉（别给后面几节留一份非零状态）
            gw.Clip = null; gw.ClipPad = Vector4.zero; gw.ClipSoftness = Vector2.zero;
            Object.DestroyImmediate(probe.gameObject);
            Object.DestroyImmediate(gw.gameObject);
        }

        // ---------------- ⑤·k 🆕 2026-10-07（A77⑮①②④）：选中态视觉 · 主菜单 ESC（退出窗）· 背景窗不吃指针
        //
        // 🔴 **判据（全部是原版 / UGUI 的；逐条写在【实现那一侧】，⛔ 别在这里抄第二份）**：
        //   · ① **选中态** = UGUI `Selectable.currentSelectionState` 的**优先级**（Pressed ＞ Selected ＞
        //     Highlighted ＞ Normal）+ `DoStateTransition` 取的那一格 `m_Colors.m_SelectedColor`
        //     （原文 → `Shell/PromptPopup.cs` 的 `WindowButton.SetSelected` / `BtnState` / `SelectedK`
        //     那三段；那 1276 颗的实测分布也在那里）。
        //   · ② **主菜单 ESC** = `MainMenuWindow__ESCPressed.c` 那两句 → `Shell/MainMenuRuntime.cs` 的 `EscapePressed`
        //     （含「`closeOnESC = 0` ⇒ base 那一跳什么都不做」与三个词条 key 的实读）。
        //   · ④ **背景窗不吃指针** = 原版那一刻底窗被上层**整屏压暗层**盖住 → `Shell/PointerLayer.cs` 的 `PointerReachable`。
        //
        // 🔴 **去自证的写法**：每条 ★ 都做成**两态对照**（同一件事、只翻那一格 ⇒ 期望值翻面）；
        //    期望值全是**字面量**或**原版常量**（⛔ 不从被测实现里读「我刚写进去的那个值」）。
        Section("A77⑮①②④：选中态（原版 `Selectable.Selected`）· 主菜单 ESC ⇒ 退出窗 · 背景窗不吃指针");
        {
            var plK = PointerLayer.Instance;
            CheckTrue(plK != null, "指针层在场（下面三条都走它这一条唯一输入路）");
            shell.Windows.CloseAllWindows();

            Check((int)WindowButton.BtnState.Selected, 3,
                  "原版 `Selectable.SelectionState` 的值序：`Selected = 3`"
                  + "（`Selectable.cs` 那个嵌套枚举 Normal/Highlighted/Pressed/Selected/Disabled；我们少 `Disabled`，见实现那段）");
            CheckNear(WindowButton.SelectedK, 0.9607843f, 1e-6f,
                      "选中态色键 = **原版 `m_Colors.m_SelectedColor` 的默认那一族**（实测 1204/1276 颗都是 0.9607843）"
                      + " —— ⛔ 不是我们挑的颜色");
            CheckNear(WindowButton.HighlightK, WindowButton.SelectedK, 1e-6f,
                      "（旁证·原版就是这么定的）`m_HighlightedColor` 与 `m_SelectedColor` 的**默认值同值**"
                      + " ⇒ 默认族群上「悬停 ≈ 选中」；下面对照靠 **`State`** 分辨，不靠色键");

            // 探针窗：两颗横排（`SelA` 左 / `SelB` 右），开窗时 `SelectFirstIn` 会选中层级序第一颗
            var selGo = new GameObject("SelProbe");
            var selWin = selGo.AddComponent<GameWindow>();
            selWin.type = WindowType.Fullscreen;
            selWin.closeOnEsc = false;
            selWin.placement = WindowsPlacement.Canvas;
            WindowsManager.AttachToAnchor(selWin);
            var selHits = new GameObject("Hits").transform;
            selHits.SetParent(selGo.transform, false);
            var s1 = MakeNavButton(selHits, "SelA", 200f, 200f, 80f, 40f);
            var s2 = MakeNavButton(selHits, "SelB", 400f, 200f, 80f, 40f);

            shell.Windows.OpenWindow(selWin);
            Check(SelName(plK), "SelA", "前置：开窗默认选中层级序第一颗 `SelA`（`OpenWindow` → `SelectFirstIn`）");
            CheckTrue(s1.IsSelected, "★ ① 选中**有视觉状态了**：`SelA.IsSelected` 为真"
                  + "（改坏法：把 `PointerLayer.Select` 里那句 `b.SetSelected(true)` 删掉 ⇒ 这条立刻红）");
            Check((int)s1.State, (int)WindowButton.BtnState.Selected,
                  "★ ① 它的**合态** = `Selected`（原版 `currentSelectionState`）");
            CheckNear(s1.TintKForTest, 0.9607843f, 1e-4f,
                      "★ ① 色偏打的就是**选中那一格**（0.9607843 = 原版 `m_SelectedColor`）");
            CheckTrue(!s2.IsSelected && (int)s2.State == (int)WindowButton.BtnState.Normal,
                      "…另一颗**没被选中**（与上面合起来 = 分得出两种状态）");

            // 优先级：Pressed ＞ Selected（用 `PressedK` ≠ `SelectedK` 分辨）
            s1.Press();
            Check((int)s1.State, (int)WindowButton.BtnState.Pressed,
                  "★ ① **按下压过选中**（原版优先级 Pressed ＞ Selected）");
            CheckNear(s1.TintKForTest, 0.7843137f, 1e-4f,
                      "…色偏换成 `m_PressedColor` 那一格（0.7843137 ≠ 0.9607843 ⇒ 两态分得开）");
            s1.Release();
            Check((int)s1.State, (int)WindowButton.BtnState.Selected,
                  "★ ① **松开之后只要还选中着就回 `Selected`**（⛔ 不是无条件回 Normal —— 这一格最容易写错；"
                  + "改坏法：把 `Release` 里那句 `ApplyState()` 换回 `SetTarget(Hovered ? HighlightK : 1f)` ⇒ 这条红）");
            CheckNear(s1.TintKForTest, 0.9607843f, 1e-4f, "…而且色偏也回到选中那一格");

            // 优先级：Selected ＞ Highlighted（两者色键同值 ⇒ 只能靠 `State` 分辨）
            s1.Enter();
            Check((int)s1.State, (int)WindowButton.BtnState.Selected,
                  "★ ① **悬停 + 选中 ⇒ 合态仍是 `Selected`**（原版 Selected ＞ Highlighted）"
                  + " —— 期望值写成 `State` 而不是色键，正因为原版那两格默认同值");
            s1.Exit();
            Check((int)s1.State, (int)WindowButton.BtnState.Selected, "…离开之后还选中着（同上）");

            // 换人 / 取消：两颗都要收到通知（= 原版 `OnSelect` / `OnDeselect`）
            plK.Select(s2);
            CheckTrue(s2.IsSelected && !s1.IsSelected,
                      "★ ① 换成 `SelB` ⇒ **两颗都通知**：新的进选中态、**旧的退掉**"
                      + "（改坏法：把 `Select` 里那句 `prev.SetSelected(false)` 删掉 ⇒ 旧那颗的选中态**卡住不退** ⇒ 红）");
            plK.Select(null);
            CheckTrue(!s2.IsSelected, "★ ① 取消选中 ⇒ 退掉（原版 `SetSelectedGameObject(null)`）");

            // 生产路径：方向键选中的那一刻，视觉跟着走（这一条正是「选中态没有视觉反馈」那个缺口的反面）
            plK.Select(s1);
            plK.KeyMove(+1f, 0f, 900f);                 // →（`time` 给足：原版 `m_RepeatDelay = 0.5` 那条节流）
            Check(SelName(plK), "SelB", "前置：方向键把选中挪到右边那颗 `SelB`");
            CheckTrue(s2.IsSelected && !s1.IsSelected,
                      "★ ① **方向键选中的那一刻视觉就跟着走**（这条钉的是「选中是真的、但看不见」那个缺口已经被补上）");
            plK.Select(null);

            // ---- ④ 背景窗不吃指针（同一点上一先一后：全屏窗 → 弹窗压上去）
            var bgAGo = new GameObject("BgProbeA");
            var bgA = bgAGo.AddComponent<GameWindow>();
            bgA.type = WindowType.Fullscreen; bgA.closeOnEsc = false; bgA.placement = WindowsPlacement.Canvas;
            WindowsManager.AttachToAnchor(bgA);
            var bgAHits = new GameObject("Hits").transform;
            bgAHits.SetParent(bgAGo.transform, false);
            var aBtn = MakeNavButton(bgAHits, "BgA", 300f, 300f, 80f, 40f);
            // 🔴 **档必须高过下面那扇弹窗的压暗层** —— 否则这条断言**不成立也照样绿**（打不到「底窗抢走命中」那种实现）。
            //    实测：`PlayerProfileWindow.QHit = 3155` > `PromptPopup.QShade = 3140` / `DailyRewardPopup.QShade = 3002`
            //    ⇒ 底窗内容档高过上层压暗层是本工程**真实存在**的组合，不是构造出来的。
            var aQuad = aBtn.GetComponentInChildren<ImageQuad>();
            CheckTrue(aQuad != null, "前置：`BgA` 那颗按钮有命中 quad（没有它这颗本来就进不了命中表）");
            if (aQuad != null) aQuad.SetRenderQueue(3200);   // 比下面弹窗压暗层的 3100 高（⚠️ 先断非空再解引用：`Check` 不抛）

            var bgBGo = new GameObject("BgProbeB");
            var bgB = bgBGo.AddComponent<GameWindow>();
            bgB.type = WindowType.Popup; bgB.closeOnEsc = false; bgB.placement = WindowsPlacement.Popup;
            WindowsManager.AttachToAnchor(bgB);
            int bgClosed = 0;
            var bShade = MenuDraw.ShadeHit(bgB.transform, new PxRect(0f, 0f, 1920f, 1080f), 3100, 3110,
                                          () => bgClosed++, "BgShadeHit");
            CheckTrue(bShade != null, "前置：下面那扇弹窗的**整屏压暗层**命中区建出来了（`MenuDraw.ShadeHit`）");
            var bBtn = bShade != null ? bShade.GetComponent<WindowButton>() : null;
            CheckTrue(bBtn != null, "前置：它是可命中的（`WindowButton` 在）");

            shell.Windows.CloseAllWindows();
            shell.Windows.OpenWindow(bgA);
            Check(bgA.CurrentState, WindowState.Open, "前置：只有全屏窗 `BgA` 时它是 `Open`");
            CheckTrue(plK.ButtonAt(300f, 300f) == aBtn,
                      "（对照）同一点上命中的是 `BgA` 那颗（证明它本来是可命中的 —— 否则下面那条是假绿）");

            shell.Windows.OpenWindow(bgB);
            Check(bgA.CurrentState, WindowState.Background, "前置：弹窗开 ⇒ 全屏窗被压到 `Background`（原版 `ToBackground()`）");
            CheckTrue(plK.ButtonAt(300f, 300f) == bBtn,
                      "★ ④ 背景窗的按钮**不再吃指针**：这一点上换成了上层那扇的压暗层（档 3100 < 底窗那颗的 3200）"
                      + "（判据 = 原版那一刻底窗被上层的**整屏压暗层**盖住 → `PointerReachable`；"
                      + "改坏法：把 `PointerLayer.CollectHits` 里那句 `if (!PointerReachable(all[i]))` 删掉 ⇒ "
                      + "`HitButton` 按档挑赢家会挑中 3200 那颗 ⇒ 这条立刻红）");
            CheckTrue(plK.ButtonAt(300f, 300f) != aBtn, "…而且**绝不是** `BgA` 那颗（与上一条合起来才分得开）");
            Check(bgClosed, 0, "…这一下只是**问命中了谁**（`ButtonAt` 不派发）⇒ 压暗层的关窗回调没被触发");

            shell.Windows.CloseAllWindows();
            shell.Windows.OpenWindow(bgA);
            CheckTrue(plK.ButtonAt(300f, 300f) == aBtn,
                      "★ ④ 对照：弹窗关掉之后，**同一点上又**是 `BgA` 那颗（证明不是「它的命中区坏了」）");
            Object.DestroyImmediate(bgBGo);
            Object.DestroyImmediate(bgAGo);

            // ---- ② 主菜单 ESC ⇒ 「退出游戏」弹窗（原版 `MainMenuWindow.ESCPressed`）
            shell.Windows.CloseAllWindows();
            CheckTrue(!plK.KeyCancel(),
                      "（对照）场上**没有常驻主菜单**时，一扇窗都没有 ⇒ ESC 什么都不做");
            var mmGo = new GameObject("MainMenuProbe");
            mmGo.AddComponent<MainMenuRuntime>();       // ⚠️ 只挂组件、**不建界面**（`Start` 在编辑模式下不跑）
            CheckTrue(plK.KeyCancel(),
                      "★ ② 场上有**常驻主菜单** + 一扇窗都没有 ⇒ ESC **被用掉了**"
                      + "（原版那一刻 `currentWindow` 是常驻 `baseMenu` = `MainMenuWindow`，它 `closeOnESC = 0` ⇒ "
                      + "base 那一跳什么都不做、只开「退出游戏」弹窗）"
                      + "；改坏法：把 `PointerLayer.KeyCancel` 那支转调删掉 ⇒ 这条立刻红");
            var exitPop = shell.Windows.TopWindow;
            CheckTrue(exitPop != null, "★ ② …真的开出了一扇窗");
            CheckTrue(exitPop is PromptPopup, "★ ② …而且就是 `PromptPopup`（⚠️ **2026-10-12（A416）**：这一扇是"
                  + " `MainMenuRuntime.OpenExitGamePopup` **自己 `PromptPopup.Create`** 建的，"
                  + "⛔ **不是** `WindowsManager.ShowPopUp` 的宿主了 —— 后者已收编到 `PopUpGameWindow`）");
            Check(exitPop != null ? exitPop.closeOnEsc : false, true,
                  "★ ② …它 `closeOnEsc = true`（原版那一刻传的是 **`closeOnEsc: 1`** —— "
                  + "与 `PromptPopup.Create` 出厂那一档 `false` **不同**，所以这条能红）");
            Check(exitPop != null ? PointerLayer.ButtonCountUnder(exitPop.gameObject) : -1, 2,
                  "★ ② …**两颗钮**（原版 `ShowPopUp` 收的两个 `GameWindowButton`：`CancelButton` / `ExitButton`）");
            CheckTrue(plK.KeyCancel(), "★ ② 再按一次 ⇒ 关掉的是**这扇弹窗**（它 `closeOnEsc = 1` ⇒ 关得掉）");
            Check(shell.Windows.TopWindow, null, "…弹窗关掉了（场上没有顶窗了）");
            Object.DestroyImmediate(mmGo);
            CheckTrue(!plK.KeyCancel(),
                      "★ ② 对照：把常驻主菜单拿掉 ⇒ 同一点上 ESC **又什么都不做**（两条合起来 = 「ESC 被谁接走」分得开）");

            shell.Windows.CloseAllWindows();
            Object.DestroyImmediate(selGo);
        }

        // ---------------- ⑤·l 🆕 2026-10-07（A77⑧a）：改名窗压暗层的命中档 = **8**（本规矩**唯一**那条例外）
        //
        // 🔴 **判据 = 裁定**（不是原版某个数字）：那条「压暗层命中档 = 本窗压暗层自己那一档、且严格低于本窗任何
        //    内容命中区档」的规矩只适用于**整屏模态窗**；`Profile Tab > ChooseNameWindow` 在我们这边是
        //    **窗内浮层**（本页的直接子节点、打开时下层页面仍 active）⇒ 档要**夹在**「下层内容(7)」与
        //    「浮层内容(9)」之间、**有意取 8**（裁定与理由 → `资料/待办判据_阶段二与联机.md` §（一）⑥③ ·
        //    `资料/待办判据_审查发现_1005.md` §⑬②；实现那一侧也写了同一份，见 `Shell/ProfileTab.cs` 的 `BuildNameWindow`）。
        // 🔴 **⛔ 不许收口到 `MenuDraw.ShadeHit`** —— 那条路按「压暗层自己那一档」走 ⇒ 档会掉到页面内容**下面**，
        //    下层那些钮(7) 就把压暗层抢走（症状：改名窗开着，点窗外却打在本页的钮上）。
        // ⚠️ 档号是**绝对**的：本页 `Q = PlayerProfileWindow.QPageBase(3160) + PageIndex*10` ⇒ 7/8/9 就是 3167/3168/3169。
        Section("A77⑧a：`ChooseNameWindow` 的压暗层命中档 = 8（夹在下层内容 7 与浮层内容 9 之间）");
        {
            var plL = PointerLayer.Instance;
            shell.Windows.CloseAllWindows();
            var prof = PlayerProfileWindow.Create(shell.Windows);
            shell.Windows.OpenWindow(prof);
            // 🔴 **必须先切到 `Profile` 页**：出厂落在 **`Title`** 页（`DefaultTabIndex = 2`），
            //    而各页根是 `SetActive(i == DefaultTabIndex)` ⇒ **Profile 页那一整棵是关着的**
            //    （关着 ⇒ 它里面的命中区进不了命中表 ⇒ 下面每条都会红在一个看不出原因的地方）。
            prof.tabButtons.Click(0);
            Check(prof.CurrentTab, WindowTabType.ProfileInfo,
                  "前置：切到 `Profile` 页（出厂默认落在 `Title` 页；不切的话那一页根是关着的，下面量不到东西）");
            var ptab = prof.Page(WindowTabType.ProfileInfo) as ProfileTab;
            CheckTrue(ptab != null, "前置：档案窗的 `Profile` 页建出来了（`PlayerProfileWindow.Page`）");
            CheckTrue(ptab != null && !ptab.NameWindowOpen, "前置：改名窗出厂是关着的（原版 `ProfileTab.Start:19` 显式关它）");

            var shadeN = ptab != null ? FindChildIn(ptab.transform, "DarkBgHit") : null;
            var pageHitN = ptab != null ? FindChildIn(ptab.transform, "EditNameHit") : null;
            var panelHitN = ptab != null ? FindChildIn(ptab.transform, "ChangeNameHit") : null;
            CheckTrue(shadeN != null && pageHitN != null && panelHitN != null,
                      "前置：三颗命中区都在（`DarkBgHit` / `EditNameHit` / `ChangeNameHit`）");
            // 🔴 期望值写成**字面量**（3167/3168/3169）—— ⛔ 不读 `ProfileTab` 的 `L_Hit/L_NameBgHit/L_NameHit`：
            //    那三条正是本条要钉的东西，从被测实现里读 = 同义反复。三条**相对差**也一起断（更抗「整段搬家」）。
            Check(QueueOf(pageHitN), 3167, "★ 下层页面那颗命中区 = **3167**（= 本页内容档 `L_Hit = 7`）");
            Check(QueueOf(shadeN), 3168, "★ **改名窗压暗层 = 3168**（= `L_NameBgHit = 8`，本规矩唯一那条例外）");
            Check(QueueOf(panelHitN), 3169, "★ 改名窗**里**那颗钮 = **3169**（= `L_NameHit = 9`，必须最高）");
            Check(QueueOf(shadeN) - QueueOf(pageHitN), 1,
                  "★ …压暗层与下层内容**只差 1 档**（夹住，不是「随便取个更大的数」）");
            Check(QueueOf(panelHitN) - QueueOf(shadeN), 1, "★ …浮层内容与压暗层也只差 1 档（三档连号）");

            if (pageHitN != null && shadeN != null && panelHitN != null)
            {
                var pageBtn = pageHitN.GetComponent<WindowButton>();
                var shadeBtn = shadeN.GetComponent<WindowButton>();
                var panelBtn = panelHitN.GetComponent<WindowButton>();
                // 🔴 量点一律**从节点自己的命中 quad 现算**（画布 px），不写死坐标 —— 页面几何改了这条会跟着走，
                //    而它要断的「谁吃这一点」不依赖具体数字。
                var pPage = PxCenterOf(pageHitN);
                var pPanel = PxCenterOf(panelHitN);
                CheckTrue(plL.ButtonAt(pPage.x, pPage.y) == pageBtn,
                          "（对照）改名窗**关着**时：那一点命中的是下层页面那颗（`EditNameHit`）");
                ptab.OpenNameWindow();
                CheckTrue(ptab.NameWindowOpen, "前置：改名窗开出来了（`ProfileTab.OpenNameWindow`）");
                CheckTrue(plL.ButtonAt(pPage.x, pPage.y) == shadeBtn,
                          "★ 改名窗**开着**时：**同一点**命中的换成它的压暗层(8) —— "
                          + "改坏法：把 `L_NameBgHit` 改成 `L_Bg`（= 按通例收口）⇒ 下层那颗(7) 又把它抢走 ⇒ 这条立刻红");
                CheckTrue(plL.ButtonAt(pPage.x, pPage.y) != pageBtn, "…而且**不是**下层页面那颗（与上一条合起来才分得开）");
                CheckTrue(plL.ButtonAt(pPanel.x, pPanel.y) == panelBtn,
                          "★ 对照的另一半：面板**里**那颗钮(9) 仍然压得住压暗层(8) ⇒ **改名点得动**"
                          + "（这正是「不许收口」的实质：收口之后压暗层掉到 7 以下，面板里那些钮还在 9，"
                          + "看着没坏；坏的是上面那条）");
                plL.ClickAt(pPage.x, pPage.y);
                CheckTrue(!ptab.NameWindowOpen, "…（顺带：点压暗层真的把改名窗关上了 = `CancelNameWindow` 接在它身上）");
            }
            shell.Windows.CloseAllWindows();
            Object.DestroyImmediate(prof.gameObject);
        }

        // ---------------- ⑤·m 🆕 2026-10-07（A77⑲）：`disabled / soft-disable` 的【变灰】观感（A65②）
        //
        // 🔴 **判据（原版，全文 → `Shell/PromptPopup.cs` 的 `WindowButton` 头部那段 + `SoftDisabled` 那一段）**：
        //   · `EverguildButton.SoftDisable` / `Selectable.interactable = false` **两条路落到同一下**：
        //     把子树（+`target`）的图形件**材质换成 `Everguild/UI/Greyscale`**；
        //   · `SoftDisable` **自己一个颜色/alpha 都不写** ⇒ 灰是**逐像素**算的（`dot(col,(0.30,0.59,0.11))`），
        //     ⛔ 不是 UGUI 的 `m_DisabledColor`（那半透明 a=0.502 在那 8 颗上是**死值**）；
        //   · shader 取不到时**出声 + 不变灰**（⛔ 不挑个灰色顶上）—— 这条靠 `GrayShaderAvailable` / `MissingGrayArt` 兜。
        // ⚠️ **本段补的是「机制」这一面**：变灰的**站点**（每日 12 颗 `Collect`）在 `Editor/RewardsScene.cs` 另有一段
        //    （断的是 `AuditGrayLook` + `MissingGrayArt == 0`）；`Shell/DeckInfoPopup.cs` 那颗 `Edit Deck`
        //    那一段在 `MainMenuScene` 里、**本轮不在本文件范围**（如实记账）。
        Section("A77⑲：`WindowButton` 的变灰 = 材质换 `Everguild/UI/Greyscale`（**不改色/alpha**、可还原、灰了照样点不动）");
        {
            CheckTrue(WindowButton.GrayShaderAvailable,
                      "★ 原版灰化 shader **`Everguild/UI/Greyscale` 取得到**（随包 `wf_shaders.bundle`）"
                      + " —— 取不到的话所有变灰只会出声、**不变灰**（那正是「不静默失败」那条兜底）");

            var gGo = new GameObject("GrayProbe");
            var gq = ImageQuad.Create(gGo.transform, CardArt.Solid(), Vector3.zero, LayoutSpace.Px(40f),
                                      new Vector2(0.5f, 0.5f), "GrayQuad");
            CheckTrue(gq != null, "前置：探针那颗 quad 建出来了");
            var gwb = gGo.AddComponent<WindowButton>();
            int gFired = 0;
            gwb.onClick = () => gFired++;

            const string GreyName = "Everguild/UI/Greyscale";      // 原版那张 shader 的**内部名**（字面量，⛔ 不读我们的常量）
            CheckTrue(ShaderNameOf(gq) != GreyName, "前置：出厂**不是**灰材质（实得 `" + ShaderNameOf(gq) + "`）");

            gwb.SetSoftDisabled(true);
            CheckTrue(gwb.GrayedForTest, "★ `SetSoftDisabled(true)` ⇒ **真的灰了**（`GrayedForTest`）");
            Check(gwb.GrayQuadCountForTest, 1, "★ …灰的是**这颗钮子树里那一颗** quad（`GrayTargets`）");
            Check(ShaderNameOf(gq), GreyName,
                  "★ …它的材质换成了**原版那张 shader**（`Everguild/UI/Greyscale`，不是我们自建的灰 shader）");
            CheckNear(gwb.TintKForTest, 1f, 1e-6f,
                      "★ …**一个颜色/alpha 都没改**（`SoftDisable` 只换材质；⛔ 别照 `m_DisabledColor` 那半透明 a=0.502 做成「变淡」"
                      + "—— 那个值在我们记的那 8 颗上是**死值**）");
            int gn; string gbad = WindowButton.AuditGrayLook(gGo.transform, out gn);
            CheckTrue(gn == 1 && gbad.Length == 0,
                      "★ …照 `AuditGrayLook` 逐颗核也是对的（查了 1 颗，问题 0 条；它核的是**原版 shader 名**）");
            CheckTrue(gwb.onClick != null && gFired == 0, "前置：还没点过（下面那条要断「点了不生效」）");
            gwb.Click();
            Check(gFired, 1, "…灰的钮**照样点得动**（`SoftDisable` **不改** `interactable` —— 这正是 A65② 的题目）");

            gwb.SetSoftDisabled(false);
            CheckTrue(!gwb.GrayedForTest, "★ 对照：`SetSoftDisabled(false)` ⇒ **退灰**");
            CheckTrue(ShaderNameOf(gq) != GreyName, "…材质**还原**了（实得 `" + ShaderNameOf(gq) + "`）");

            gwb.Interactable = false;
            Check(ShaderNameOf(gq), GreyName, "★ 另一条路 `interactable = false` ⇒ **同一份灰**（原版两条路落到同一下）");
            gFired = 0;
            gwb.Click();
            Check(gFired, 0, "★ …而且这一档**点了不生效**（原版 `Selectable.OnPointerClick` 头一句就返回）");
            gwb.Interactable = true;
            CheckTrue(ShaderNameOf(gq) != GreyName && !gwb.GrayedForTest, "★ 对照：置回 `true` ⇒ 灰退掉、又能点了");
            gFired = 0;
            gwb.Click();
            Check(gFired, 1, "…真的又能点了（与上面那条合起来 = 分得出「灰着」与「可用」两种状态）");
            Object.DestroyImmediate(gGo);
        }

        // ---------------- ⑦🆕 A177：入口**收编进「按引用复用」**（`WindowsManager.OpenByRef`）----------------
        //   判据 → `资料/待办判据_1007.md` §A177 · `资料/普查产出_1007/波6_A123_入口复用.md` ·
        //   `资料/普查产出_1010/V4a_壳与共用件口径.md` §Q2 · `资料/普查产出_1010/调度台_口径裁定_1011.md`。
        //   原版三层：① 缓存 = **`WindowsManager.automaticallyLoadedWindows`**（键 = **prefab 引用**）；
        //   ② `OpenWindow` **第一件事**就是查它 ⇒ 同一扇窗点两次只有一个实例；③ **关窗会把缓存条目删掉**
        //   ⇒ 关过之后再点，走的是**新建**。
        //   A177 之前那几条入口（`SocialWindow.OpenChat` · `BattleLogTab.OpenPopup` ·
        //   `RankedEventWindow.OpenLeaderboard` · `LeaderboardRow.OnRowClicked`）**直调 `Create`**、
        //   完全不查缓存 ⇒ 连点两次叠两扇；现在都收编到同一份机制上（机制本体 2026-10-11 从
        //   `MainMenuRuntime` 挪进 `WindowsManager`，= **原版那个字段所在的位置**）。
        //   ⛔ 每条断言的期望值都是**上面那三层原版判据的行为**，不是我们自己的常量。
        Section("A177：入口「按引用复用」（机制三条 + 两条真入口端到端）");
        {
            // 局部小工具：按**名字**数「开着的窗」里有几扇、按**类型**取第一扇
            // （判据一律 = `WindowsManager.openWindows` —— ⛔ 不数锚点子件：我们的 `Close()` 不销毁对象）。
            int CountOpen(string onlyName)
            {
                int c = 0;
                foreach (var w in shell.Windows.openWindows)
                    if (w != null && w.name == onlyName) c++;
                return c;
            }
            T FirstOpen<T>() where T : GameWindow
            {
                foreach (var w in shell.Windows.openWindows)
                    if (w != null && w is T) return (T)w;
                return null;
            }
            int CountOpenOf<T>() where T : GameWindow
            {
                int c = 0;
                foreach (var w in shell.Windows.openWindows)
                    if (w != null && w is T) c++;
                return c;
            }

            shell.Windows.CloseAllWindows();
            WindowsManager.ClearReuseCacheForTest();          // 别的段用过的缓存别漏进来

            // ---- ① 机制：同一个引用连开两次 ⇒ **同一实例** + 场上只有 1 扇 ----
            System.Func<WindowsManager, GameWindow> mkProbe = m =>
            {
                var go = new GameObject("A177 Probe Window");
                var w = go.AddComponent<GameWindow>();
                w.type = WindowType.Popup;
                w.placement = WindowsPlacement.Popup;
                WindowsManager.AttachToAnchor(w);
                return w;
            };
            var pk1 = WindowsManager.OpenByRef("A177 Probe Window", mkProbe);
            var pk2 = WindowsManager.OpenByRef("A177 Probe Window", mkProbe);
            CheckTrue(pk1 != null && pk2 == pk1,
                      "★ ① 同一个 prefab 引用连开两次 ⇒ **同一扇**（原版 `automaticallyLoadedWindows` 命中复用，"
                    + "`TryGetValue` 在 `Instantiate` 之前）；改坏法：把机制换回「直调 `Create` + `OpenWindow`」⇒ 两扇 ⇒ 红");
            Check(CountOpen("A177 Probe Window"), 1,
                  "…而且「开着的窗」里那种窗只有 **1** 扇（复用那一支不许顺手再添一份）");

            // ---- ② 关掉再开 ⇒ **新建**（**只有这一条**区分得开「还开着才复用」与「实例还在就复用」）----
            pk1.Close();
            var pk3 = WindowsManager.OpenByRef("A177 Probe Window", mkProbe);
            CheckTrue(pk3 != null && pk3 != pk1,
                      "★ ② 关掉之后再开 ⇒ **新建一扇**（原版 `CloseWindowCO` 关窗会删缓存条目；我们的 `Close()` 只 "
                    + "`SetActive(false)`、对象还在 ⇒ 实现写成「实例还在就复用」时**只有这条红**）");
            pk3.Close();

            // ---- ③ `closeAll`（原版那颗入口钮的 `closeOtherMenus = 1`）⇒ **别的窗被收掉** ----
            var ok1 = WindowsManager.OpenByRef("A177 Probe Window", mkProbe);
            var ok2 = WindowsManager.OpenByRef("A177 Probe Other", m =>
            {
                var go = new GameObject("A177 Probe Other");
                var w = go.AddComponent<GameWindow>();
                w.type = WindowType.Popup;
                w.placement = WindowsPlacement.Popup;
                WindowsManager.AttachToAnchor(w);
                return w;
            });
            CheckTrue(ok2 != null && ok2.CurrentState == WindowState.Open, "（前提）另一扇探针窗开着");
            var ok3 = WindowsManager.OpenByRef("A177 Probe Window", mkProbe, true);   // ← `closeAll`
            CheckTrue(ok3 == ok1, "…带 `closeAll` 那一支走的仍是**复用**（没有因为它先清场就换成新建）");
            Check(ok2.CurrentState, WindowState.Closed,
                  "★ ③ `closeAll: true` ⇒ 开窗时先把**别的窗**收掉（原版 `closeOtherMenus = 1`）；"
                  + "改坏法：把那个实参丢掉 ⇒ 它照旧开着 ⇒ 红");
            Check(ok3.CurrentState, WindowState.Open, "…而被点的那一扇照旧开着（`closeAll` 不许把复用到的那扇也漏掉）");
            shell.Windows.CloseAllWindows();

            // ---- ④ **真入口端到端**：档案窗 `Battle Log` 页那颗「开对局历史弹窗」的钮 ----
            //   （= `Shell/BattleLogTab.cs` 的 `OpenPopup`，A177 收编的第二条入口）
            WindowsManager.ClearReuseCacheForTest();
            var profA = PlayerProfileWindow.Create(shell.Windows);
            shell.Windows.OpenWindow(profA);
            profA.tabButtons.Click(3);                        // 3 = `WindowTabType.ProfileBattleLog`
            Check(profA.CurrentTab, WindowTabType.ProfileBattleLog, "（前提）切到 `Battle Log` 页");
            var logBtnN = FindChildIn(profA.transform, "Open Log Popup Button");
            var logHitN = logBtnN != null ? FindChildIn(logBtnN, "Hit") : null;
            var logBtn = logHitN != null ? logHitN.GetComponent<WindowButton>() : null;
            CheckTrue(logBtn != null && logBtn.onClick != null,
                      "（前提）那颗入口钮接了点击（`Open Log Popup Button/Hit`）");
            if (logBtn != null && logBtn.onClick != null)
            {
                logBtn.onClick();
                var lw1 = FirstOpen<BattleLogPopup>();
                CheckTrue(lw1 != null && lw1.CurrentState == WindowState.Open,
                          "（前提）点一下 ⇒ 真的开出一扇对局历史弹窗");
                logBtn.onClick();
                CheckTrue(FirstOpen<BattleLogPopup>() == lw1,
                          "★ ④ 再点一下 ⇒ **还是同一扇**（`BattleLogTab.OpenPopup` 已收编到 `OpenByRef`）；"
                        + "改坏法：把它改回 `BattleLogPopup.Create(mgr)` 直建 ⇒ 换了一扇 ⇒ 红");
                Check(CountOpenOf<BattleLogPopup>(), 1, "…而且场上只有 **1** 扇（同 ①：不许顺手再添一份）");
                if (lw1 != null) lw1.Close();
                // 🔴 **2026-10-14（A673）：「关一扇窗」之后必须【现场重抓】那颗入口钮，别沿用旧句柄。**
                //   触发集 = 「任何窗的 `OpenWindow` / `TryOpen` / `.Close()`」（A674 订正的那条口径）：那一拍会把
                //   当时是 `Closed` 的窗 `Open()` ⇒ `Build()` 把根下子件整棵 `DestroyImmediate` 重建
                //   ⇒ 旧句柄成**死引用**（`Editor/MainMenuScene.cs` 的 A516 是同一条，那边已按这个改法落地）。
                //   ⚠️ **为什么「只加一个 null 守卫」不够**：守卫判假 ⇒ 这一下**根本不点** ⇒ 下面
                //   `lw2 != null && lw2 != lw1` 变**假红**（A516 那条踩过的坑就是「一下都没点」）。重抓两种情形都对。
                //   ⚠️ 今天这一处**恰好**不炸的原因（读出来的，不是猜的）：`WindowButton.onClick` 是**裸字段**
                //   （`Shell/PromptPopup.cs:326`）⇒ 解引用不抛；而委托体 `BattleLogTab.OpenPopup`
                //   （`Shell/BattleLogTab.cs:172-177`）**一句实例状态都没读**（只有静态的 `WindowsManager.OpenByRef`
                //   + 常量串 `Debug.Log`）⇒ 即便宿主已销毁也照旧开窗、不抛。**这是巧合、不是保证** ——
                //   哪天 `OpenPopup` 读一个实例字段就会抛 `MissingReferenceException`。
                var lbN2 = FindChildIn(profA.transform, "Open Log Popup Button");
                var lbH2 = lbN2 != null ? FindChildIn(lbN2, "Hit") : null;
                var logBtn2 = lbH2 != null ? lbH2.GetComponent<WindowButton>() : null;
                CheckTrue(logBtn2 != null && logBtn2.onClick != null,
                          "（前提）关掉那扇弹窗之后，入口钮**现场重抓得到**（跨窗重建会把旧句柄变死引用）");
                if (logBtn2 != null && logBtn2.onClick != null) logBtn2.onClick();
                var lw2 = FirstOpen<BattleLogPopup>();
                CheckTrue(lw2 != null && lw2 != lw1,
                          "★ ④ 关掉之后再点 ⇒ **新建一扇**（与 ② 同一判据：`StillOpen` 认的是 `openWindows`，"
                        + "不是「实例还在」）");
                if (lw2 != null) lw2.Close();
            }
            shell.Windows.CloseAllWindows();                  // 先收表（它还在 `openWindows` 里），再销毁对象
            Object.DestroyImmediate(profA.gameObject);

            // ---- ⑤ **真入口端到端**：聊天窗（`Shell/SocialWindow.cs` 的 `OpenChat`，A177 收编的第一条）----
            //   ⚠️ 本窗（社交窗）**不建整棵树**：`OpenChat` 收编之后**只读那一份静态机制、不读实例状态**
            //   （这正是「入口只此一份」那条注释要表达的东西）⇒ 裸挂一个组件就够，省一次整窗构建。
            WindowsManager.ClearReuseCacheForTest();
            var swStubGo = new GameObject("A177 social stub");
            var swStub = swStubGo.AddComponent<SocialWindow>();
            var c1 = swStub.OpenChat();
            var c2 = swStub.OpenChat();
            CheckTrue(c1 != null && c1.CurrentState == WindowState.Open, "（前提）`OpenChat` 真的开出一扇聊天窗");
            CheckTrue(c2 == c1, "★ ⑤ 再调一次 `SocialWindow.OpenChat()` ⇒ **同一扇**（原版 `automaticallyLoadedWindows` "
                              + "命中复用）；改坏法：改回 `ChatPanel.Create(wm)` 直建 ⇒ 红");
            Check(CountOpenOf<ChatPanel>(), 1, "…而且场上只有 **1** 扇聊天窗");
            if (c1 != null) c1.Close();
            var c3 = swStub.OpenChat();
            CheckTrue(c3 != null && c3 != c1, "★ ⑤ 关掉之后再点 ⇒ **新建**（同 ②）");
            if (c3 != null) c3.Close();
            Object.DestroyImmediate(swStubGo);
            shell.Windows.CloseAllWindows();
            WindowsManager.ClearReuseCacheForTest();          // 收尾：别让这一段的缓存影响后面的段
        }

        // ---------------- ⑥ 音频 / 开场
        Section("音频与开场动画（正本 §二 / §四）");
        var main = Resources.Load<AudioClip>("Art/audio/music/main_theme");
        var idle = Resources.Load<AudioClip>("Art/audio/music/menu_idle_theme");
        CheckTrue(main != null, "菜单主主题加载得到（`Main Theme.ogg`）");
        CheckTrue(idle != null, "菜单 idle 主题加载得到（`Menu Idle Theme.ogg`）");
        var intro = Resources.Load<UnityEngine.Video.VideoClip>("Art/videos/intro");
        if (intro == null)
        {
            // 诊断：把这个目录里**能加载到的**都打出来（区分「没导入」与「路径写错」）
            // ⚠️ 用拼接而不是 `$"…"` 插值 —— 里面要带中文引号/括号，插值串里再嵌 `"` 会直接编译不过（踩过）
            var all = Resources.LoadAll("Art/videos");
            var names = new System.Text.StringBuilder();
            foreach (var c in all) names.Append(" ").Append(c.name).Append("(").Append(c.GetType().Name).Append(")");
            Debug.Log(P + "  诊断：`Art/videos` 里的资源 = " + (all.Length == 0 ? "（一个都没有）" : names.ToString()));
        }
        CheckTrue(intro != null, "开场动画加载得到（`Warpforge Intro.mp4`）");
        if (intro != null)
            CheckTrue(intro.length > 1.0f, $"开场动画时长 > 1s（实测 {intro.length:F2}s）");
        CheckNear(ShellRuntime.IntroVolume, 0.65f, 0.0001f, "开场动画音量 = 0.65（`AudioSource_261` 实证）");
        CheckNear(ShellRuntime.MusicVolumeMultiplier, 0.2f, 0.0001f, "菜单音乐源音量系数 = 0.2（`MusicManager` 预制体实证）");

        Check(shell.Current, ShellRuntime.Phase.Intro, "一开始停在**开场**阶段");
        shell.SkipIntro();
        Check(shell.Current, ShellRuntime.Phase.Menu, "跳过之后进 **Menu** 阶段");

        // ⚠️ **这一张是已知全黑**（2026-09-23 加空图护栏时实测：**每一个采样点都是 (0,0,0)**）：
        //    此刻是 **Menu 阶段且一个窗都没开**，画面只有黑底的清屏色（`Camera_258` ClearFlags 2 = 纯黑）。
        //    ✅ **2026-10-03 查清（A18）**：原来挂着两种可能「① 四条本来就是黑→透明的渐变、压在黑底上就是黑 /
        //    ② 它们根本没渲染」—— 是 **①**（`工具/read_gradient2_level0.py` 读出 `m_Color` 白 + 顶点色渐变，
        //    见下面「②·b」那节）。⚠️ **现在四条已经真的是渐变，这张图**（黑渐变压在纯黑清屏色上）**照旧全黑** ——
        //    要看出渐变得先有底图。**别拿它当「渐变没生效」的证据。**
        Shoot("01_壳_空态.png", allowBlank: true);
        shell.Shade.SetAlpha(0.8f);
        // ⚠️ 同理已知全黑：`Shade.SetAlpha(0.8)` 是**纯黑 0.8** 压在本来就黑的画面上 —— 这一张本来就该是黑的
        Shoot("03_压暗.png", allowBlank: true);
        shell.Shade.SetAlpha(0f);

        // ============================================================ 点击记录器（`Core/ClickLog.cs`）
        // 🔴 **工具本身要先验证**（CLAUDE.md §一.6）—— 否则它自己就是一处「静默失败」：
        //    用户真点了一晚上、文件却是空的，而且没人知道。
        // 这里走一遍「开始 → 记命中 → 打一条日志 → 落盘」，逐样核对写出来的正文。
        {
            var probePath = Path.Combine(ShotDir, "_click_probe.txt");
            if (File.Exists(probePath)) File.Delete(probePath);
            ClickLog.OverridePath = probePath;
            ClickLog.Begin("ProbeScene", "自检探针", new Vector2(123.4f, 567.8f));
            ClickLog.Hit("命中 `测试件`", "onClick 已绑");
            Debug.Log("[Probe] 这一条应当出现在「实际触发的日志」里");
            ClickLog.End();
            var blk = ClickLog.LastBlock ?? "";
            CheckTrue(blk.Contains("ProbeScene") && blk.Contains("123.4") && blk.Contains("567.8"),
                      "点击记录：**点位**（场景名 + 画布像素）写进去了");
            CheckTrue(blk.Contains("测试件") && blk.Contains("onClick 已绑"),
                      "点击记录：**命中了谁**写进去了");
            CheckTrue(blk.Contains("这一条应当出现在"),
                      "点击记录：**同帧的日志被捕获**（= 「实际触发了什么」那一栏）");
            CheckTrue(File.Exists(probePath), "点击记录**真的落盘了**：" + probePath);
            ClickLog.OverridePath = null;
        }

        // ================= 🆕 2026-10-11（A327 · A306① + A306③）：两态夹具 =================
        //   判据 / 断言什么 / 为什么这个形状能照出它 → `资料/普查产出_1011/W4_子3.md` §四·b。
        //   🔴 **2026-10-11（FX3）订正一处说法（铁律 5）**：原来这两行写「两处都是**潜伏缺陷**」—— **不准确**。
        //   `basis == 窗根` ⇒ `PosInDesignSpace` 除的是窗根的**父级** = Holder（恒单位缩放，
        //   `Shell/WindowsManager.cs` 的 `MakeHolder`）、而 `AttachToAnchor` 把窗根钉在 `localPosition = 0`
        //   （同一文件里那句 `SetParent(anchor, false)` + `localPosition = zero`）⇒ 新旧两式在**该调用形状下永远逐位相同**（⛔ 不是「今天观测不到」）
        //   ⇒ A306①②③④ 那四处码的改动在生产里**是 no-op**（留着只因口径更对 / 防御性）。
        //   ⛔ **别再说成「修好了一个带电的潜伏缺陷」**；真带电的是**非根基准**那一族
        //   （`Shell/PracticeModePopup.cs:494/502/792/796/853/924/1238`）。
        //   ⚠️ 本夹具（M 加在**窗根的父级** + 态二**重建**）照的正是**那一族**的形状（判据 → 文件头 ①②③）。
        SmallScreenUI.PersistOverride = true;      // ⛔ 自检不许动玩家的真设置
        Section("A327 · A306①：`PromptPopup.Local` 的落位（关 = 逐值不变 / 开 = 设计点 × M）");
        {
            var ppA = PromptPopup.Create(shell.Windows, "A327 探针", "OK", null, null, null);
            CheckTrue(ppA != null, "（前提）`PromptPopup` 建出来了");
            if (ppA != null)
            {
                // 🔴 **2026-10-11（FX3）换夹具姿势**（原来是把**窗根**挪到 (2,1.5) ⇒ 那条恒等式必红，见文件头 ①②）：
                //   M 加在**窗根的父级**那一颗探针根上；窗根留在它**下面**、只给它一个非零位移 (2,1.5)。
                //   ⇒ 基准（= 窗根）相对被乘那一级的位移 = (2,1.5)（|·| = 2.5 ≥ 1）：坏式与好式相差
                //     `M(M−1)×2.5` = **0.6 世界单位 = 65px**（容差 0.02 = 2.2px）⇒ 真会红。
                var probeA = new GameObject("A327① probe root");          // ← 这一颗才是「被乘 M 的那一级」
                ppA.transform.SetParent(probeA.transform, false);         // 窗根留在被乘那一级**下面**……
                ppA.transform.localPosition = new Vector3(2f, 1.5f, 0f);  // ……并在它下面有一个非零位移
                CheckTrue(ppA.TryOpen(null), // 态一（开关关）建一遍 ⇒ p1 == 设计点
                          "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
                var okA = FindChildIn(ppA.transform, "OkButton");
                CheckTrue(okA != null, "（前提）`OkButton` 在（它就是 `Local(root, …)` 摆出来的那颗）");
                if (okA != null)
                    CheckScaleTwo(probeA, ppA.transform,
                                  () =>
                                  {
                                      // 🔴 **2026-10-12（A437）态二那次必须【先 `Close()` 再开】** ——
                                      //   A217② 把 `GameWindow.TryOpen` 按原版改成按 `CurrentState` 分三档之后，
                                      //   **同窗再开（`Open` 支）会早退、不重建** ⇒ 光再调一次 `TryOpen` 拿到的还是
                                      //   态一那颗 `OkButton`（旧节点）⇒ 下面那条「真的重建了」的前提先红、★ 退化成假绿。
                                      //   `Close()` 走的是**生产那条链**（`Close` → `NotifyClosed` → `state = Closed`）
                                      //   ⇒ 紧接着的 `TryOpen` 落回 `Closed` 支、重建照旧（且顺带覆盖 `Closed` 支那一拍）。
                                      //   ⛔ **别改成直调 `Open()`**：那会绕开 `Closed` 支（少覆盖一段）。
                                      ppA.Close();                    // ← A437：把 state 送回 `Closed`
                                      CheckTrue(ppA.TryOpen(null), // 🔴 态二**必须重建**（见文件头 ③）
                                                "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
                                      var n = FindChildIn(ppA.transform, "OkButton");
                                      // （前提③）**真的重建了**：拿到的是**新**节点 ⇒ 被量的局部位置是在 `k ≠ 1`
                                      // 那一趟**重算**出来的，不是态一冻结的那份。改坏法：`measure` 改回纯读
                                      // （`() => okA.position`）⇒ 这条红（而 ★ 会退化成假绿）。
                                      CheckTrue(n != null && n != okA,
                                                "（前提）…：态二的 measure **真的重建了** `OkButton`"
                                              + "（`Build()` 首句清空子件 ⇒ 拿到的是新节点）");
                                      if (n != null) okA = n;
                                      return n != null ? n.position : Vector3.zero;   // 缺件 ⇒ ★ 也会红，⛔ 不静默
                                  },
                                  1.2f,
                                  "A306① `PromptPopup.Local`（`OkButton` 的落位）"
                                + " —— 改坏法：`PromptPopup` 里把 `Local` 换回裸 `parent.position` ⇒ 偏"
                                + " `M(M−1)×|基准相对被乘那一级的位移 (2,1.5)|` = 0.24×2.5 = **0.6 单位 = 65px**"
                                + "（容差 0.02 = 2.2px）⇒ 红；⛔ 别把窗根挪走换绿（那是假绿）");
                Object.DestroyImmediate(ppA.gameObject);
                Object.DestroyImmediate(probeA);
            }
            SmallScreenUI.Set(false);
        }

        Section("A327 · A306③：`ImportDeckPopup.Local3` 的落位（关 = 逐值不变 / 开 = 设计点 × M）");
        {
            var ipA = ImportDeckPopup.Create(shell.Windows);
            CheckTrue(ipA != null, "（前提）`Import Deck Popup` 建出来了");
            if (ipA != null)
            {
                // 同 A306①：M 加在**窗根的父级**探针根上，窗根留在它下面、只给它一个 (2,1.5) 的位移。
                var probeC = new GameObject("A327③ probe root");          // ← 被乘 M 的那一级
                ipA.transform.SetParent(probeC.transform, false);
                ipA.transform.localPosition = new Vector3(2f, 1.5f, 0f);
                CheckTrue(ipA.TryOpen(null), "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
                var wA = FindChildIn(ipA.transform, "Window");
                CheckTrue(wA != null, "（前提）`Window` 在（那一层就是 `Local3(root, …)` 摆的）");
                if (wA != null)
                    CheckScaleTwo(probeC, ipA.transform,
                                  () =>
                                  {
                                      // 🔴 **2026-10-12（A437）**：态二必须先 `Close()` —— 理由与 A306① 那一处逐字相同
                                      //   （A217② 之后同窗再开走 `Open` 支、**早退不重建**；`Close()` 走生产链把 state 送回
                                      //   `Closed` ⇒ `TryOpen` 落回 `Closed` 支、重建照旧）。⛔ 别改成直调 `Open()`。
                                      ipA.Close();                    // ← A437：把 state 送回 `Closed`
                                      CheckTrue(ipA.TryOpen(null), // 🔴 态二**必须重建**（见文件头 ③）
                                                "（前提）重开之后窗真的开着 —— 下面那条才不是空断");
                                      var n = FindChildIn(ipA.transform, "Window");
                                      // （前提③）重建真的发生了 —— 改坏法：`measure` 改回纯读 ⇒ 这条红。
                                      CheckTrue(n != null && n != wA,
                                                "（前提）…：态二的 measure **真的重建了** `Window`"
                                              + "（`Build()` 首句清空子件 ⇒ 拿到的是新节点）");
                                      if (n != null) wA = n;
                                      return n != null ? n.position : Vector3.zero;   // 缺件 ⇒ ★ 也会红，⛔ 不静默
                                  },
                                  1.2f,
                                  "A306③ `ImportDeckPopup.Local3`（`Window` 那一层的落位）"
                                + " —— 改坏法：`ImportDeckPopup` 里把 `Local3` 换回裸 `basis.position` ⇒ 偏"
                                + " `M(M−1)×|基准相对被乘那一级的位移 (2,1.5)|` = 0.24×2.5 = **0.6 单位 = 65px**"
                                + "（容差 0.02 = 2.2px）⇒ 红；⛔ 别把窗根挪走换绿（那是假绿）");
                Object.DestroyImmediate(ipA.gameObject);
                Object.DestroyImmediate(probeC);
            }
            SmallScreenUI.Set(false);
            SmallScreenUI.PersistOverride = false;
            CheckTrue(!SmallScreenUI.Enabled, "（收尾）A327：自检跑完把开关放回**出厂值 关**");
        }

        // ---------------- ⑤·z 🆕 **2026-10-13（A435 阶段 2 · 丙）**：裁切状态长在【视口节点】上（丙块那几处）
        //
        // 判据 = `Shell/ViewportClip.cs` 文件头 + A435 迁移表
        //   （`资料/普查产出_1013/A435_迁移表.md` §二·A / §二·B 的「丙」那一栏：A1–A9 · A27–A32 · B3–B6 · B11–B13）
        //   + 各站点原版 `RectMask2D` 的实读值（`m_Padding` / `m_Softness`）。
        //
        // 🔴 **本块断两件事**（各带**改坏法**）：
        //   ① **迁移逐位不变**：每颗节点那个框 = 它【替换掉】的那个显式矩形（**字面量 / 公开访问器**，
        //      ⛔ 不从 `ViewportClip` 自己算出来的中间量里读）；`padding` / `softness` = 原版实读值。
        //   ② **节点真的在说话**（灭自证 · **两态**）：把节点那个框挪一格 ⇒ **同一批内容**的边界跟着挪。
        //      旧写法（把矩形当**显式 `clip`** 传进 `MenuDraw`）两态读数**一字不差** ⇒ 这条红。
        //      ⚠️ 量的是**渲出来那块**（`QuadPxRect` 读的是 `ImageQuad.WorldW/H` + 世界位置），
        //      ⛔ 不是断节点自己的 rect（那等于拿写进去的值验写进去的值）。
        //
        // ⚠️ **本块排在【最末】**（它自己建 6 扇窗）—— 结尾**全部关掉 / 销毁**（理由同 ⑤·d-4 那段：
        //   留着 `ViewportClip` 会把**后面每一段**挂在它父链下的件都裁到那个框里；本块之后只剩 `shell.Dump()`）。
        // 🔴 ⛔ **本块不断任何「计数 == 0」**：`NodeShadowedByParam` 在 ⑤·d-4（A464·B5）里已经被
        //   **故意 +1**（那正是它的牙口）⇒「迁完 == 0」那一条只能落在 `Editor/RewardsScene.cs`（甲的宿主）。
        Section("★ A435·丙：裁切状态迁到【视口节点】（聊天 / 社交 / 档案 / 榜单 / 对局历史 / 设置六扇窗）");
        {
            shell.Windows.CloseAllWindows();

            // 在 `root` 子树里找**那一颗**「框 ≈ `want`」的 `ViewportClip`（找不到 ⇒ null）。
            // ⚠️ 按**框**找而不是按名字找：本块要断的正是「框对不对」，按名字找会先假定名字对。
            ViewportClip NodeOfFrame(Transform root, PxRect want)
            {
                if (root == null) return null;
                foreach (var v in root.GetComponentsInChildren<ViewportClip>(true))
                {
                    var cp = v.ClipPx;
                    if (cp.HasValue && NearPx(cp.Value, want)) return v;
                }
                return null;
            }
            // 子树里**渲出来的**最低下沿（画布 px）。`QuadPxRect` 读的是 `ImageQuad` 自己的 `WorldW/H`
            // + 世界位置 ⇒ 截过的那一块量出来就是**截完的**尺寸（⛔ 不是节点矩形）。
            float MaxQuadBottomPx(Transform root)
            {
                float m = float.NegativeInfinity;
                if (root == null) return m;
                foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
                {
                    // 🔴 **2026-10-14（清单 #14 / D2 #1）**：`GetComponentsInChildren(true)` 连**关掉的**件一起捞，
                    //   而 `MenuDraw.ClipNineChildren` 对「整块在框外」的九宫子块的处理是 **`SetActive(false)`、
                    //   ⛔ 不删节点**（`Shell/MenuDraw.cs:1409` 那一句；函数体 `:1393`，「不删节点」写在 `:1386`）
                    //   ⇒ 不判这一下就会把**画面上不画**的底边条算进「画出来的下沿」
                    //   （实测 = 第 9 行的 856.00，而不是视口下沿 811.00）。
                    //   口径照**同文件**的 `GUnion`（下面 `:4516`）抄 —— 那一条本来就写着
                    //   「⚠️ 跳过 `!activeInHierarchy` 的件（画面上没有的东西不算「画出来的并集」）」。
                    //   ⇒ 两份同类读数器从这一刻起同口径（D2 §五·3 提的「建议收口成一份」仍开着，另立账）。
                    if (q == null || !q.gameObject.activeInHierarchy) continue;
                    float a, b, c, d;
                    if (QuadPxRect(q, out a, out b, out c, out d) && d > m) m = d;
                }
                return m;
            }
            // 一颗节点的三连核：框 + 两个原版参数。`why` = 那一处「原来是什么」。
            void NodeCheck(Transform root, string what, PxRect want, Vector2Int soft, string why)
            {
                var vc = NodeOfFrame(root, want);
                CheckTrue(vc != null,
                          $"★ {what}：**视口节点在**、而且框 ≈ {want.x1:F1},{want.y1:F1} → {want.x2:F1},{want.y2:F1}"
                          + $" —— {why}（改坏法：把那一句 `ViewportClip.Hang` / `AddComponent` 拿掉 ⇒ 红）");
                if (vc == null) return;
                CheckTrue(vc.padding == Vector4.zero,
                          $"★ {what}：`padding` = 原版实读的 `(0,0,0,0)`（现读 ({vc.padding.x},{vc.padding.y},"
                          + $"{vc.padding.z},{vc.padding.w})）");
                CheckTrue(vc.softness == soft,
                          $"★ {what}：`softness` = 原版实读的 ({soft.x},{soft.y})"
                          + $"（现读 ({vc.softness.x},{vc.softness.y})；⚠️ 这一格就是把 `m_Softness` 搬上节点的意义 ——"
                          + "原来它靠「逐件传 `clipSoftness`」）");
            }

            // ============================================================ ① 聊天窗（B5）
            // `Chat Tab/Viewport` 的 `m_Softness = (0,22)`（原档逐字实读 → `Shell/ChatPanel.cs` 的 `VpSoft`）。
            var chatW = ChatPanel.Create(shell.Windows);
            shell.Windows.OpenWindow(chatW);
            chatW.tabButtons.Click(0);                    // 第 0 页活着（`RefreshMessages` 只重画 active 那页）
            var chatTab = chatW.tabs.Count > 0 ? chatW.tabs[0] as ChatTab : null;
            CheckTrue(chatTab != null, "（前提）聊天窗第 0 页 = `ChatTab`");
            var chatScr = chatTab != null ? chatTab.RowsScroll : null;
            CheckTrue(chatScr != null, "（前提）这一页的滚动区在（它的 `Viewport` = 节点该有的那个框）");
            Transform chatAnchor = null;                  // `Viewport/Content`（消息行挂它下面）
            // ⚠️ `chatScr` 可能为 null（`Setup()` 还没跑）⇒ 进这一支之前必须先判 —— 否则
            //   `chatScr.Viewport` 会当场 NRE（自检崩掉 = 后面每一条都不跑，比红更糟）。
            if (chatTab != null && chatScr != null)
            {
                chatAnchor = chatTab.transform.Find("Viewport/Content");
                CheckTrue(chatAnchor != null, "（前提）`Chat Tab/Viewport/Content` 在");
                NodeCheck(chatTab.transform, "B5 · `Chat Tab/Viewport`", chatScr.Viewport, new Vector2Int(0, 22),
                          "迁移前是 `ChatMessageRow.Build(…, vpR, VpSoft)` 逐件传（`clip` + `clipSoftness`）");
                var vcC0 = ViewportClip.FindAbove(chatAnchor);
                CheckTrue(vcC0 != null && ReferenceEquals(vcC0, NodeOfFrame(chatTab.transform, chatScr.Viewport)),
                          "★ B5：**父链上那一颗**就是它（`FindAbove(Content)` 走一级命中 `Viewport`）"
                          + " —— 只断「子树里有一颗」不够：`Resolve` 沿的是**父链**");
            }
            else if (chatTab != null)
                CheckTrue(false, "（前提）`ChatTab.RowsScroll` 拿不到 ⇒ B5 没验到（⛔ 不静默跳过）");

            // ---- ①-b **两态（灭自证）**：节点那个框真的决定「渲到哪为止」
            // ⚠️ 拿不到节点就不能往下走（`vcC.transform` 会 NRE ⇒ 整个自检崩在那一条上，
            //   比红更糟）。上面那条 `NodeCheck` 只记账、**不中断** ⇒ 这里再显式判一次。
            var vcC = chatAnchor != null ? ViewportClip.FindAbove(chatAnchor) : null;
            if (chatAnchor != null && vcC == null)
                CheckTrue(false, "（前提）聊天页那颗 `Viewport` 节点拿不到 ⇒ ①-b 那一组两态断言**没跑**"
                               + "（⛔ 不静默：这里显式报一条）");
            if (chatTab != null && chatAnchor != null && vcC != null)
            {
                var vpProd = chatScr.Viewport;
                // 喂 20 条（行高 60 + 行距 10，视口 161..911）：行 10 压在视口**下沿**上、行 11 起整行在外
                for (int i = 0; i < 20; i++)
                    SocialData.ChatMessages.Add(new SocialData.ChatMessage
                    {
                        Channel = "Global", Sender = "NodeProbe" + i, Time = "0d 0h",
                        Text = "node-" + i.ToString("00"), Mine = false, Height = 0f,
                        AvatarArt = ProfileData.AvatarArt,
                    });
                chatW.RefreshMessages();
                CheckTrue(chatTab.BuiltRows > 1,
                          $"（前提）消息行真建出来了（{chatTab.BuiltRows} 行；为 0 的话下面量的是空树）");
                float b0 = MaxQuadBottomPx(chatAnchor);
                CheckTrue(!float.IsNegativeInfinity(b0), "（前提）态一量得到 quad（一颗都没建 ⇒ 下面两条等于没验）");
                CheckNear(b0, vpProd.y2, 0.6f,
                          "★ 态一（**节点框 = 生产值**）：压在下沿上的那一行被**截到视口下沿**"
                          + $"（实测最低下沿 {b0:F2}；期望 {vpProd.y2:F2}）");
                // ---- 态二：把**同一个节点**那个框的下沿收上来 100px（`ApplyPxRect` = `Hang` 建节点用的同一个口）
                var vpUp = new PxRect(vpProd.x1, vpProd.y1, vpProd.x2, vpProd.y2 - 100f);
                MenuDraw.ApplyPxRect(vcC.transform, vcC.transform.parent, vpUp);
                chatW.RefreshMessages();
                CheckTrue(chatTab.BuiltRows > 1,
                          "（前提）态二也把行建出来了（那道「整行滚出视口 ⇒ 不建」的粗筛用的是"
                          + "**滚动区自己的矩形**、没跟着节点走 —— 这正是本条的判别力所在）");
                float b1 = MaxQuadBottomPx(chatAnchor);
                CheckNear(b1, vpProd.y2 - 100f, 0.6f,
                          "★★ 态二：**同一批内容**，边界跟着【节点】挪了 100（实测最低下沿 "
                          + $"{b1:F2}；期望 {vpProd.y2 - 100f:F2}）"
                          + " —— 🔴 **改坏法**：把 `ChatPanel.cs` 那两句 `ChatMessageRow.Build(…, null, VpSoft)`"
                          + "改回传 `vpR`（= 迁移前的逐件传）⇒ 形参非空 ⇒ `Resolve` 第 1 支 ⇒ 两态读数**一字不差**"
                          + $"（都停在 {vpProd.y2:F2}）⇒ 本条红；而「节点建出来了」那条照旧绿（只断存在 = 弱断言）");
                CheckTrue(b0 - b1 > 99f,
                          $"★ …两条合起来才钉住「那 100px 只可能来自节点」（态一 {b0:F2} − 态二 {b1:F2} = "
                          + $"{b0 - b1:F2}）");
                // ---- 实参态：显式传非空 `clip` ⇒ 显式赢（迁移期可回退的那一档，⛔ 不是缺陷）
                var wide = new PxRect(vpProd.x1 - 500f, vpProd.y1 - 500f, vpProd.x2 + 500f, vpProd.y2 + 500f);
                PxRect oN, oE;
                CheckTrue(MenuDraw.ClipRectAbove(vcC.transform, wide, null, out oN) && NearPx(oN, vpUp),
                          $"★ 实参态（对照）：`clip = null` ⇒ 求交按**节点**那个框（实测 {oN.x1:F1},{oN.y1:F1} → "
                          + $"{oN.x2:F1},{oN.y2:F1}；期望 {vpUp.x1:F1},{vpUp.y1:F1} → {vpUp.x2:F1},{vpUp.y2:F1}）");
                CheckTrue(MenuDraw.ClipRectAbove(vcC.transform, wide, wide, out oE) && NearPx(oE, wide),
                          "★ 实参态：**显式形参非空 ⇒ 形参赢、连父链都不走**（同一个矩形、只多传一个 `clip`）"
                          + " —— 改坏法：把 `ViewportClip.Resolve` 第 1 支删掉 ⇒ 得节点那个框 ⇒ 红"
                          + "｜这一档是**迁移期可回退**的保证，⛔ 不是缺陷");
                MenuDraw.ApplyPxRect(vcC.transform, vcC.transform.parent, vpProd);   // 还原（下一个探针要用）
                chatW.RefreshMessages();
            }

            // ============================================================ ② 社交窗（A27–A30 + B13）
            var socW = SocialWindow.Create(shell.Windows);
            shell.Windows.OpenWindow(socW);
            var pa = socW.PageAlliances;
            var pf = socW.PageFriends;
            CheckTrue(pa != null && pf != null, "（前提）社交窗两页都在（`Alliances` / `Friends`）");
            // 四处视口的**原版真值**（普查 §A·1；节点就是照这四个矩形建的 —— 三个字面量取自
            // `AlliancesTab.OpenViewportR` / `FriendsTab.ContainerR` / `AllianceMemberTab.TrophyScrollR`，
            // 第四个走公开访问器 `MemberScroll.Viewport`）
            NodeCheck(pf != null ? pf.transform : null, "A30 · `Friends Container/Viewport`",
                      new PxRect(332.15f, 314.80f, 1875.80f, 1080.06f), Vector2Int.zero,
                      "迁移前是 `BuildRows` 里那一对 `SetClip(vpR)` / `SetClip(null)`");
            NodeCheck(pa != null ? pa.transform : null, "A29 · `Open Alliances/Viewport`",
                      new PxRect(360.99f, 337.29f, 1874.90f, 1079.77f), Vector2Int.zero,
                      "迁移前是 `BuildRows` 里那一对 `SetClip(sc.Viewport)` / `SetClip(null)`");
            NodeCheck(pa != null ? pa.transform : null, "A27 · `TrophiesWindow>Scroll Rect`（奖杯格）",
                      AllianceMemberTab.TrophyScrollR, new Vector2Int(0, 50),
                      "迁移前是 `BuildTrophyRows` 里 `_trophyClip = vpR` + `SetClip(vpR)` 那一对"
                      + "（⚠️ 这一格是社交四处里**唯一**带软边的：原版 `m_Softness = (0,50)`）");
            var mt = pa != null ? pa.Member : null;
            if (mt != null && mt.MemberScroll != null)
                NodeCheck(mt.transform, "A28 · `MemberList>Scroll View>Viewport`", mt.MemberScroll.Viewport,
                          Vector2Int.zero,
                          "迁移前是 `AllianceMemberRow.BuildAll` 里 `v.SetClip(vp0)` / `v.SetClip(null)` 那一对");
            else
                CheckTrue(false, "（前提）`AlliancesTab.Member` / `MemberScroll` 在 —— 拿不到 ⇒ 上面那条 A28 没验到"
                               + "（⛔ 不静默跳过：这正是「弱断言分不出两种状态」那一族）");
            // B13：社交页的裁切载体**已经是节点**（`SocialPage.Clip` / `SetClip` / `ClipNow` 那一整套显式覆盖口
            //   已由 A753 = A744 的「全删」**整体删掉**，⛔ 不再有「生产路径上有没有人写那个字段」可断）。
            //   ⇒ 换成**迁移之后的正确含义**：拿**真实的内容件**沿父链解析，落到的必须就是那颗视口节点
            //   （`Friends Container/Viewport/Content` —— `FriendsTab.Build()` 里那句话**空表也会建**）。
            //   ⚠️ 比原断言更强：原来只断「没人写那个字段」，现在断「四处转发走的 `Resolve` 真的落到这颗节点上」。
            //   ⚠️ **方向**：`ViewportClip.FindAbove` 是**沿父链向上**找 —— 起点必须是**视口里面**的件
            //   （拿 `pf.transform` 当起点是错的：视口节点是它的**后代**，向上走永远找不到）。
            var fVc = NodeOfFrame(pf != null ? pf.transform : null,
                                  new PxRect(332.15f, 314.80f, 1875.80f, 1080.06f));
            var fContent = fVc != null ? FindChildIn(fVc.transform, "Content") : null;
            var fAbove = fContent != null ? ViewportClip.FindAbove(fContent) : null;
            CheckTrue(fVc != null && fContent != null && fAbove == fVc,
                      "★ B13：好友页的裁切载体**是节点、而且接在正确那一颗上** —— 从真实内容件 "
                      + "`Friends Container/Viewport/Content` 沿父链 `ViewportClip.FindAbove` 落到的就是那颗 `Viewport`"
                      + "（A753 之后 `SocialPage.Clip` 那个显式覆盖口已整体删掉）"
                      + " —— 改坏法：把 `Shell/FriendsTab.cs` 里那句 `ViewportClip.Hang` 换回 `MenuDraw.Node(...)`"
                      + "、或把 `Content` 从 `Viewport` 底下挪走 ⇒ 这里 null ⇒ 红");

            // ============================================================ ③ 档案窗（A1–A9 + B3/B4）
            var profW = PlayerProfileWindow.Create(shell.Windows);
            shell.Windows.OpenWindow(profW);
            var pgAv = profW.Page(WindowTabType.ProfileAvatar) as AvatarTab;
            var pgTi = profW.Page(WindowTabType.ProfileTitle) as TitleTab;
            var pgTr = profW.Page(WindowTabType.ProfileTrophies) as AchievementsMenu;
            var pgRk = profW.Page(WindowTabType.ProfileRanking) as RankedTab;
            var pgBl = profW.Page(WindowTabType.ProfileBattleLog) as BattleLogTab;
            CheckTrue(pgAv != null && pgTi != null && pgTr != null && pgRk != null && pgBl != null,
                      "（前提）档案窗六个页都建出来了（本件用到其中五个）");
            // 🔴 `Avatar` / `Title` 两页**没有 `Viewport` 节点**（视口矩形建在 `Scroll Rect` 那一层上，
            //   原版树是 `…/Scroll Rect/Viewport/Item Drawer`）⇒ 走迁移表 §二·A 注① 的【低风险】路：
            //   `AddComponent<ViewportClip>` 到**现成的** `Scroll Rect` 上（零结构改动）。
            //   ⚠️ 两页的视口矩形**同值**（654.16,210.69 → 1680.12,855.46）⇒ 必须**按页取子树**
            //   （`NodeOfFrame(page.transform, …)`），否则会把另一页那颗当成这一页的（假绿）。
            NodeCheck(pgAv != null ? pgAv.transform : null, "A3/A4 · `Avatar Tab/Item Display Panel/Scroll Rect`",
                      new PxRect(654.16f, 210.69f, 1680.12f, 855.46f), new Vector2Int(0, 50),
                      "迁移前是 `Build`/`RebuildRows` 里那两对 `Clip = vp; ClipSoftness = VpSoft;` … 清掉");
            NodeCheck(pgTi != null ? pgTi.transform : null, "A5/A6 · `Title Tab/Item Display Panel/Scroll Rect`",
                      new PxRect(654.16f, 210.69f, 1680.12f, 855.46f), new Vector2Int(0, 50),
                      "同上（`TitleTab` 与 `AvatarTab` 逐位同形）");
            NodeCheck(pgTr != null ? pgTr.transform : null, "A1/A2 · `Trophies Tab/Scroll/Viewport`",
                      new PxRect(635.14f, 216.14f, 1746.98f, 891.69f), Vector2Int.zero,
                      "迁移前是 `Build`/`RebuildCells` 里那两对 `Clip = vp;` … `Clip = null;`");
            NodeCheck(pgRk != null ? pgRk.transform : null, "A7/A8 · `Ranking Tab/AllFactions/scroll rect/viewport`",
                      new PxRect(1305.29f, 288.62f, 1746.97f, 864.38f), Vector2Int.zero,
                      "迁移前是 `BuildAllFactions`/`RebuildRows` 里那两对 `Clip = scR;` … `Clip = null;`");
            if (pgBl != null && pgBl.RowsScroll != null)
                NodeCheck(pgBl.transform, "A9 · `Battle Log Tab/Matches/Viewport`", pgBl.RowsScroll.Viewport,
                          Vector2Int.zero, "迁移前是 `BuildRows` 里 `ctx.Clip = _scroll.Viewport` / `= null` 那一对");
            else
                CheckTrue(false, "（前提）`BattleLogTab.RowsScroll` 在 —— 拿不到 ⇒ A9 没验到（⛔ 不静默跳过）");

            // ============================================================ ④ 榜单（A32）· ⑤ 对局历史弹窗（A31）
            foreach (var kind in new[] { LeaderboardKind.Skirmish, LeaderboardKind.Embedded })
            {
                var lb = LeaderboardWindow.Create(shell.Windows, kind);
                shell.Windows.OpenWindow(lb);
                var ls = lb.RowsScroll;
                if (ls != null)
                    NodeCheck(lb.transform, $"A32 · `{kind} > Scroll View/Viewport`", ls.Viewport,
                              Vector2Int.zero,
                              "迁移前是 `RebuildRows` 里 `_rowCtx.Clip = _scroll.Viewport` / `= null` 那一对"
                              + "（⚠️ 两棵各自一颗节点 —— 一扇窗里几个视口本来就是几个 `ViewportClip`）");
                else
                    CheckTrue(false, $"（前提）`LeaderboardWindow({kind}).RowsScroll` 在 —— 拿不到 ⇒ A32 这一棵没验到");
                // ⚠️ **开了窗的只 `Close()`、不 `DestroyImmediate`** —— 那会让 `WindowsManager` 的窗表里
                //   留一个 Unity 假 null（本块末尾 `shell.Dump()` 要遍历它）。先例 = ⑤·f 的 `chatW.Close()`。
                lb.Close();
            }
            var blp = BattleLogPopup.Create(shell.Windows);
            shell.Windows.OpenWindow(blp);
            if (blp.RowsScroll != null)
                NodeCheck(blp.transform, "A31 · `Battle Log Popup/Matches/Viewport`", blp.RowsScroll.Viewport,
                          Vector2Int.zero,
                          "迁移前是 `BuildRows` 里 `_rowCtx.Clip = vpR` / `= null` 那一对");
            else
                CheckTrue(false, "（前提）`BattleLogPopup.RowsScroll` 在 —— 拿不到 ⇒ A31 没验到（⛔ 不静默跳过）");
            blp.Close();

            // ============================================================ ⑥ 设置窗（B6）—— **两态**：节点框决定「建不建」
            var setW = SettingsWindow.Create(shell.Windows);
            shell.Windows.OpenWindow(setW);
            var gfxContent = setW.GfxContent;
            var gfxScr = setW.GfxRowsScroll;
            CheckTrue(gfxContent != null && gfxScr != null, "（前提）设置窗画质那一列的 `Content` / 滚动区在"
                                                          + "（要先进 `Graphics` 页 —— 它是开窗时建的）");
            if (gfxContent != null && gfxScr != null)
            {
                var vpG = gfxScr.Viewport;
                NodeCheck(setW.transform, "B6 · `Scroll View/Viewport`", vpG, Vector2Int.zero,
                          "迁移前是「逐件把 `_gfxClip` 当 `clip` 传给 `Rect`/`Text`/`NineOut`/`Hit`」");
                // ⚠️ **量「渲出来几块」而不是「节点在不在」**：`BuildCheckRow` 第一句 `Node(content, …)`
                //    **无条件**建节点（框外那几行**节点照建**、只有图/字/命中区不建）⇒ 断 `FindChildIn(...)==null`
                //    会**恒假**（假红）。判别力只能落在 `ImageQuad` 的个数上。
                int QuadsUnder(Transform root)
                {
                    int k = 0;
                    if (root == null) return 0;
                    foreach (var q in root.GetComponentsInChildren<ImageQuad>(true)) k++;
                    return k;
                }
                // ---- 态一：生产态 ⇒ 那一行真建出图来
                setW.RebuildGfxRows();
                var rowA = FindChildIn(gfxContent, "Small Screen UI");
                int nA = QuadsUnder(rowA);
                CheckTrue(nA >= 2,
                          $"★ B6 态一（生产框）：`Small Screen UI` 那一行**建出来了**、而且有图"
                          + $"（实测 {nA} 块 `ImageQuad`：方框 + 命中区 ⇒ 至少 2）");
                // ---- 态二：把**节点**那个框挪到整列之外 ⇒ 同一句 `RebuildGfxRows()` 应当**一块图都不建**
                var vpAway = new PxRect(vpG.x1 + 4000f, vpG.y1 + 4000f, vpG.x2 + 4000f, vpG.y2 + 4000f);
                var vcG = NodeOfFrame(setW.transform, vpG);
                // ⚠️ 拿不到节点就**不能往下走**（`vcG.transform` 会 NRE ⇒ 整个自检崩在那一条上，
                //   比红更糟）。上面那条 `CheckTrue(vc != null …)` 只记账、**不中断**。
                if (vcG == null)
                    CheckTrue(false, "（前提）设置窗那颗 `Viewport` 节点拿不到 ⇒ B6 那两条两态断言**没跑**"
                                   + "（⛔ 不静默：这里显式报一条）");
                else
                {
                    MenuDraw.ApplyPxRect(vcG.transform, vcG.transform.parent, vpAway);
                    setW.RebuildGfxRows();
                    int nB = QuadsUnder(FindChildIn(gfxContent, "Small Screen UI"));
                    CheckTrue(nB == 0,
                              $"★★ B6 态二：把**节点**那个框挪走 ⇒ 同一句 `RebuildGfxRows()` 那一行**一块图都不建**"
                              + $"（实测 {nB} 块；态一是 {nA} 块）"
                              + " —— 改坏法：把那一列调用点的 `clip` 改回 `_gfxClip`（显式非空 ⇒ 形参赢、"
                              + "父链都不走）⇒ 态二照旧建出来 ⇒ 红；⛔ 也别只挪 `_gfxClip`（那是本地字段、"
                              + "与节点是两份状态 —— 正是本迁移要消灭的那种写法）");
                    MenuDraw.ApplyPxRect(vcG.transform, vcG.transform.parent, vpG);   // 还原
                    setW.RebuildGfxRows();
                    int nC = QuadsUnder(FindChildIn(gfxContent, "Small Screen UI"));
                    CheckTrue(nC >= 2,
                              $"★ …还原节点框 ⇒ 那一行**又建出图来**（实测 {nC} 块）—— 两态都断"
                              + " ⇒ 上面那个「0」不可能来自别的原因（例如「这一页压根没建」）");
                }
            }
            setW.Close();

            // ============================================================ 收尾
            Debug.Log($"[Chk] （打印·非判据）`ViewportClip` 三个静态计数：`NodeResolutions` = "
                    + $"{ViewportClip.NodeResolutions} · `NodeShadowedByParam` = {ViewportClip.NodeShadowedByParam}"
                    + $" · `UnusableNodes` = {ViewportClip.UnusableNodes}"
                    + "（⚠️ 本块**不**断 `NodeShadowedByParam == 0` —— ⑤·d-4 的 A464·B5 已经**故意**把它 +1；"
                    + "「迁完 == 0」那一条只能落在 `Editor/RewardsScene.cs`，见 A435 迁移表 §四/§七)");
            // ⚠️ 本块建的 **6 扇窗全部 `Create` + `OpenWindow` 过**（= 在 `WindowsManager` 的窗表里）
            //   ⇒ 收尾只 `Close()`、⛔ **不 `DestroyImmediate`**（那会让窗表里留一个 Unity 假 null，
            //   而下面 `shell.Dump()` 要遍历它）。先例 = ⑤·f 的 `chatW.Close()`。
            SocialData.ChatMessages.Clear();
            if (chatW != null) chatW.Close();
            if (socW != null) socW.Close();
            if (profW != null) profW.Close();
            shell.Windows.CloseAllWindows();
        }

        // ---------------- ⑤·z-2 🆕 **2026-10-13（A435 阶段 2 · 丁）**：B1（`GameWindow.Text`）+ A745（活动窗阵营条）
        //
        // 判据 = `Shell/ViewportClip.cs` 文件头 + `资料/普查产出_1013/A435_迁移表.md`
        //   §二·B·B1（`GameWindow.Text` 那两处守卫）· A745（丙块报出来的**漏网同族站点**，调度台已裁「算 A435 站点」）
        //   + 范本：`Shell/MenuWindowBase.cs` 的 `Text`（A435① 成品 —— B1 就是照它那两处逐字改的）
        //           `Shell/ChatPanel.cs` 的 `ChatTab/Viewport`（A745 照它改：**本节点本来就存在** ⇒ 只 `AddComponent`）。
        //
        // 🔴 **B1 为什么单开一段**：`GameWindow.Text` 是**全壳所有窗共用**的那一个
        //   （`MenuWindowBase` 族与 `LiveOpsEventWindow` 族**都**继承它）。A435① 只改了
        //   `MenuWindowBase.Text` / `TextBox` 那两处 ⇒ **基类这一处不改 = 非 `MenuWindowBase` 族的每一扇窗
        //   （`CampaignRewardWindow` / `DailyStreakPopup` / `InboxWindow` …）的文字一个像素都吃不到节点态**（静默）。
        //   ⇒ 本段拿**真窗 `InboxWindow`** 当夹具（它是 `GameWindow` 直系、文字走 `GameWindow.Text`、
        //     `Clip` 已由甲块置空、视口节点在 `Build()` 里 `ViewportClip.Hang`）—— ⛔ 全程只用它的**公开 API**。
        //
        // 🔴 **两处都断，而且两处各有独立的两态**（= 判据是「节点在说话」，⛔ 不是「结果非空」那种弱断言）：
        //    ① 第一处守卫（整块在框外 ⇒ 不建）：节点框**挪到整块之外** vs 生产框；
        //    ② 第二处守卫（压在边上 ⇒ `ClipText`）：节点框**切在字中间** vs **显式形参覆盖**（实参态）。
        //   ⚠️ 量的是**渲出来的顶点**（`TmpSpanPx` 直读 `textInfo.meshInfo[..].vertices`）——
        //      ⛔ 不是 `Label.WorldW`（它不随裁切变）、也⛔ 不是计数器（计数器分不出这两态）。
        //
        // ⚠️ 本段排在 **⑤·z（丙那段）之后**（它自己声明「排在最末」，但它建的窗结尾都 `Close()` 了）；
        //   本段照同一条纪律：自己建的窗结尾 `Close()`、动过的**节点框逐处还原**（⛔ 留着会裁掉后面段落的件）。
        Section("★ A435·丁：`GameWindow.Text`（B1 · 基类那一处）+ 活动窗阵营条（A745）的节点态");
        {
            // 在 `root` 子树里按**框**找那一颗 `ViewportClip`（同 ⑤·z 的口径：按框找，⛔ 不按名字找 ——
            // 本段要断的正是「框对不对」，按名字找会先假定名字对）。
            ViewportClip NodeOfFrame(Transform root, PxRect want)
            {
                if (root == null) return null;
                foreach (var v in root.GetComponentsInChildren<ViewportClip>(true))
                {
                    var cp = v.ClipPx;
                    if (cp.HasValue && NearPx(cp.Value, want)) return v;
                }
                return null;
            }

            // 条目里那颗 `Title` 的 `Label`（`FindChildIn` 给的是 `Transform`，而 `TmpSpanPx` 要 `Label`）。
            Label TitleOf(Transform row)
            {
                var t = row != null ? FindChildIn(row, "Title") : null;
                if (t == null) return null;
                var l = t.GetComponent<Label>();
                return l != null ? l : t.GetComponentInChildren<Label>();
            }

            // ============================================================ ① A743 / B1 —— `GameWindow.Text`
            var ib = InboxWindow.Create(shell.Windows);
            shell.Windows.OpenWindow(ib);
            var msgVc = NodeOfFrame(ib.transform, InboxWindow.MsgList);
            CheckTrue(msgVc != null,
                      "（前提）`Inbox Menu/Content/Message List/Viewport` 那颗节点在（框 = 原版 `MsgList` "
                      + $"{InboxWindow.MsgList.x1:F2},{InboxWindow.MsgList.y1:F2} → "
                      + $"{InboxWindow.MsgList.x2:F2},{InboxWindow.MsgList.y2:F2}）"
                      + " —— 它是 B1 这四态的夹具（`InboxWindow : GameWindow` ⇒ 它的文字走 `GameWindow.Text`）");
            CheckTrue(ib.Clip == null && ib.ClipPad == Vector4.zero && ib.ClipSoftness == Vector2.zero,
                      "（前提）本窗三兄弟全空（甲块已把旧设站点删干净）⇒ 文字只能靠**节点**裁"
                      + $"（现读 `Clip` = {(ib.Clip.HasValue ? "非 null" : "null")}）"
                      + " —— 它非空的话形参赢、节点白挂（`Resolve` 第 1 支，静默）");
            var inboxMsgs = new List<InboxWindow.Message>
            {
                new InboxWindow.Message("WWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWW", "2026-10-13", true),
            };
            ib.Initialize(inboxMsgs);
            CheckTrue(ib.Rows.Count == 1 && ib.Rows[0] != null,
                      $"（前提）条目建出来了（{ib.Rows.Count} 条；`InboxWindow.Initialize` 是原版同名生产 API）");
            var rowE = ib.Rows.Count > 0 ? ib.Rows[0] : null;
            var tlb = TitleOf(rowE);
            CheckTrue(tlb != null,
                      "（前提）条目里那颗 `Title` 标签在（**它就是 `GameWindow.Text` 建的** —— "
                      + "`InboxWindow.RowText` 直接转调基类那一份）");
            CheckTrue(tlb != null && tlb.GetComponent<ClippedTextGuard>() != null,
                      "★★ A743（B1 · **第二处守卫**）：本窗 `Clip` 为空、而父链上有节点 ⇒ 那一刀**照旧补上了**"
                      + "（`_st.RenderClip.HasValue` ⇒ 真调到了 `ClipText` ⇒ 标签上挂着 `ClippedTextGuard`）"
                      + " —— 🔴 **改坏法**：把 `Shell/WindowsManager.cs` 末句改回"
                      + " `if (RenderClip.HasValue) MenuDraw.ClipText(…)`（= B1 之前那一版）⇒ 本窗字段恒 `null`"
                      + " ⇒ **根本不调** ⇒ 这里红；而「标签建出来了」那条照旧绿（只断存在 = 弱断言）");
            float aMinX = 0f, aMinY = 0f, aMaxX = 0f, aMaxY = 0f;
            bool natOk = tlb != null && TmpSpanPx(tlb, out aMinX, out aMinY, out aMaxX, out aMaxY);
            CheckTrue(natOk && aMaxX > aMinX + 4f,
                      "（前提）态一量得到可用跨度（**直读** `textInfo.meshInfo[..].vertices`，⛔ 不是 `Label.WorldW`）"
                      + $"（实测 x {aMinX:F2}..{aMaxX:F2}）—— 跨度 ≤ 4px 的话下面态三 / 态四等于没验");

            // 🔴 **2026-10-14（清单 #12 的**断言半边**）**：态一**本来就压边** —— 它**不是**「未切值」。
            //    `MsgList` 的右沿是 728.28，而这段字（`InboxWindow` 的 42 个 `W`）的**自然右沿比它宽**
            //    ⇒ 态一量到的右沿就是**被夹在框沿上**的那个值。判据：`Shell/InboxWindow.cs:54` 的 `MsgList`。
            //    ⚠️ **更正痕迹（铁律 5）**：原来下面态三/态四把本条读数当「未切值」用（「态一未切时是 {aMaxX}」/
            //    「右沿回到未切值 aMaxX」）—— **那是错的**。**错因** = A804（`Label.AlignLeftOn`/`AlignRightOn`
            //    平移完**不重裁** ⇒ 裁切框被整块推走 `+123.16px`）把这个读数**伪装成了「未切值」**
            //    （旧实得 `851.44` = `728.28 + 123.16`，两个态的差值逐位相同）。A804（`Battle/Label.cs` 的
            //    `ReclipNow` 尾句）修完之后这里就**回到 728.28**，而**真·未裁值**（954.83 那一档）由下面态四给。
            if (natOk)
                CheckNear(aMaxX, InboxWindow.MsgList.x2, 1.0f,
                          "★★ A743 态一（**压边值** · ⚠️ 不是「未切值」）：节点框的右沿 = `MsgList.x2`、"
                          + "而这段字的自然右沿比它宽 ⇒ 右沿**被夹在框沿上**"
                          + $"（实测 {aMaxX:F2}；期望 {InboxWindow.MsgList.x2:F2}）"
                          + " —— 🔴 **改坏法**：把 `Battle/Label.cs` 的 `AlignLeftOn` / `AlignRightOn` 尾句"
                          + " `ReclipNow()` 拿掉（= A804 回归）⇒ 这里量到 `x2 + 123.16` ⇒ 红｜"
                          + "⚠️ 这一条**不能**写成「相对差」：A804 回归时态一与态三会**同量**偏 +123.16 ⇒ "
                          + "`态一右沿 − 态三右沿` 照样等于 `x2 − cutX`（差值被约掉）⇒ 相对差**分不出**回归。"
                          + "真·未裁值看下面态四");

            if (msgVc != null)
            {
                // ---- 态二（第一处守卫的两态之一）：把**同一个节点**那个框挪到整块之外 ⇒ 同一句 `Initialize`
                //      里那颗 `Title` 应当**连标签都不建**（**连带再挪回来**，两态都断）。
                var away = new PxRect(InboxWindow.MsgList.x1 + 4000f, InboxWindow.MsgList.y1 + 4000f,
                                      InboxWindow.MsgList.x2 + 4000f, InboxWindow.MsgList.y2 + 4000f);
                MenuDraw.ApplyPxRect(msgVc.transform, msgVc.transform.parent, away);
                ib.Initialize(inboxMsgs);
                var tlbAway = TitleOf(ib.Rows.Count > 0 ? ib.Rows[0] : null);
                CheckTrue(tlbAway == null,
                          "★★ A743 态二（B1 · **第一处守卫**）：把**节点**那个框挪到整块之外 ⇒ 同一句 `Initialize` 里"
                          + "那颗 `Title` **连标签都不建**"
                          + $"（实测 {(tlbAway == null ? "没建 ✅" : "还是建出来了 ✗")}）"
                          + " —— 🔴 **改坏法**：把 `GameWindow.Text` 第一句改回 `if (!MenuDraw.Visible(r, RenderClip))`"
                          + "（= B1 之前那一版；本窗字段恒 `null` ⇒ 一律判「可见」）⇒ 标签照建 ⇒ 红");
                MenuDraw.ApplyPxRect(msgVc.transform, msgVc.transform.parent, InboxWindow.MsgList);
                ib.Initialize(inboxMsgs);
                var tlbBack = TitleOf(ib.Rows.Count > 0 ? ib.Rows[0] : null);
                CheckTrue(tlbBack != null,
                          "★ …把节点框还原 ⇒ 那颗 `Title` **又建出来了** —— 两态都断 ⇒ 上面那个「没建」"
                          + "不可能来自别的原因（例如「这一批数据压根没铺」）");

                if (natOk)
                {
                    // ---- 态三（第二处守卫的**节点态**）：把同一个节点框的**右沿切到字中间**
                    //      ⇒ 同一段字渲出来的右沿应当被夹到那个框上。
                    float cutX = (aMinX + aMaxX) * 0.5f;
                    MenuDraw.ApplyPxRect(msgVc.transform, msgVc.transform.parent,
                                         new PxRect(InboxWindow.MsgList.x1, InboxWindow.MsgList.y1,
                                                    cutX, InboxWindow.MsgList.y2));
                    ib.Initialize(inboxMsgs);
                    var tlbCut = TitleOf(ib.Rows.Count > 0 ? ib.Rows[0] : null);
                    float c1 = 0f, c2 = 0f, c3 = 0f, c4 = 0f;
                    CheckTrue(tlbCut != null && TmpSpanPx(tlbCut, out c1, out c2, out c3, out c4),
                              "（前提）态三也把 `Title` 建出来了（这颗字块压在框内 ⇒ 不该被整块丢掉）");
                    CheckNear(c3, cutX, 1.0f,
                              $"★★★ A743 态三（B1 · **第二处守卫**的节点态）：**节点那个框真的作用在文字网格上**"
                              + $"（实测右沿 {c3:F2}；期望 {cutX:F2}；态一（同一条数据、框放到整条 `MsgList`）"
                              + $"压边量到 {aMaxX:F2}）"
                              + " —— 🔴 **改坏法**：把 `GameWindow.Text` 交给 `ClipText` 的 `_st.RenderClip` 改回"
                              + " `RenderClip`（本窗恒 `null` ⇒ 那一刀不补）⇒ 这里得未切值（态四量到的那个自然右沿）⇒ 红"
                              + "｜⚠️ 控制组 = 态一（**同一个窗、同一条数据、只挪节点**）"
                              + "｜⚠️ 2026-10-14 订正：原来括号里写「态一**未切**时是 {aMaxX:F2}」—— 那句是错的，"
                              + "`aMaxX` 本身就是**被裁过**的值；它现在的名字叫「压边值」（见上面那条）");
                    CheckTrue(Mathf.Abs(c1 - aMinX) <= 1.0f,
                              $"★ …而**左沿没动**（{c1:F2} vs 态一 {aMinX:F2}）—— 这一刀是按新框**重裁**，"
                              + "不是把整块平移过去（平移的实现这里红）");

                    // ---- 态四（**实参态对照**）：节点框**不动**，只把显式形参设成**一定包得住整段字** ⇒ 形参赢。
                    // 🔴 **2026-10-14（清单 #13 的**断言半边** · 铁律 5 订正）**：形参原来写的是 `aMaxX + 200f`，
                    //   而**那个 `aMaxX` 本身是被裁过的值**（A804 未修时 = `x2 + 123.16`）⇒ 两处都错：
                    //   ① 原来那句「右沿回到未切值」是**假的** —— 它拿到的是「形参框把字切在 `x2+200` 上」；
                    //   ② A804 修完（= `Label` 平移后补 `ReclipNow`）之后 `aMaxX` 回到 `x2`，`aMaxX + 200`
                    //      整个落进字里 ⇒ 那条期望更不成立。
                    //   ⇒ 形参改成**按节点框左右各放宽 4000px**：这个框一定包得住整段字 ⇒ 量到的就是**真·未裁值**
                    //      （= 诊断建议的「另测的未裁宽度」由态四自己给），而期望从「等于 `aMaxX`」改成
                    //      「**越过节点框右沿**」—— 「节点赢」的话它会正好落在 `MsgList.x2` 上，
                    //      而真·未裁值在那条框沿之外 ~226px（实测量级 954.8 vs 728.3）⇒ 判得出。
                    ib.Clip = new PxRect(aMinX - 4000f, aMinY - 200f, aMaxX + 4000f, aMaxY + 200f);
                    ib.Initialize(inboxMsgs);
                    var tlbParam = TitleOf(ib.Rows.Count > 0 ? ib.Rows[0] : null);
                    float d1 = 0f, d2 = 0f, d3 = 0f, d4 = 0f;
                    CheckTrue(tlbParam != null && TmpSpanPx(tlbParam, out d1, out d2, out d3, out d4),
                              "（前提）态四也把 `Title` 建出来了");
                    CheckTrue(d3 > InboxWindow.MsgList.x2 + 50f,
                              "★ A743 态四（**实参态对照**）：`Clip` 显式非空 ⇒ **形参赢、连父链都不走** ⇒ "
                              + "右沿**越过节点框**（真·未裁值）"
                              + $"（实测 {d3:F2} > `MsgList.x2` {InboxWindow.MsgList.x2:F2} + 50；"
                              + $"态三（节点）是 {c3:F2}；态一（节点）是 {aMaxX:F2}）"
                              + " —— 这个数就是态一 / 态三里那个「被夹住」的**控制组**（自然右沿真的比框宽）"
                              + " —— 改坏法：删掉 `ViewportClip.Resolve` 第 1 支 ⇒ 仍按节点框裁 ⇒ 得 "
                              + $"{InboxWindow.MsgList.x2:F2} ⇒ 红"
                              + "｜⚠️ **A804 回归不会让本条红**（那条路下框太宽、裁不到，只是整块偏 123.16）"
                              + "—— 那一档由上面态一那条**压边**断言盯（它才是判别式）"
                              + "｜🔴 这一档是**迁移期可回退**的保证（旧路还在设就旧路赢），⛔ 不是缺陷");
                    ib.Clip = null;    // 还原（⛔ 别留给后面几段：它会把整窗的图/命中区一起改）
                    MenuDraw.ApplyPxRect(msgVc.transform, msgVc.transform.parent, InboxWindow.MsgList);
                    ib.Initialize(inboxMsgs);
                }
                else
                    CheckTrue(false, "（前提）态一量不到可用跨度 ⇒ 态三 / 态四**没跑**"
                                   + "（⛔ 不静默跳过：那正是「弱断言分不出两种状态」那一族）");
            }
            else
                CheckTrue(false, "（前提）`Message List/Viewport` 那颗节点拿不到 ⇒ B1 那四态**没跑**"
                               + "（⛔ 不静默跳过）");
            ib.Close();

            // ============================================================ ② A745 —— 活动窗阵营条的 `Viewport`
            // 🔴 **迁移前 = 「逐件传 `view` + `VpSoft`」**（与 `Shell/ChatPanel.cs` 迁移前一模一样）⇒ 调度台裁定
            //    「**算 A435 站点**」。现在状态长在 `Army Selector/Viewport` 那颗节点上（`AddComponent`，
            //    ⛔ 不是 `Hang` —— 本节点**本来就存在**，`Army Content` 就挂在它下面 ⇒ 零结构改动）。
            var sk = SkirmishEventWindow.Create(shell.Windows);
            shell.Windows.OpenWindow(sk);
            var armFrame = new PxRect(LiveOpsEventWindow.ArmViewL, LiveOpsEventWindow.ArmViewT,
                                      LiveOpsEventWindow.ArmViewR, LiveOpsEventWindow.ArmViewB);
            var armVc = NodeOfFrame(sk.transform, armFrame);
            CheckTrue(armVc != null,
                      "★ A745：`Ranked Army Selector/Army Selector/Viewport` 那颗节点在（框 = 原版 "
                      + $"`Army Selector/Viewport` {armFrame.x1:F2},{armFrame.y1:F2} → {armFrame.x2:F2},{armFrame.y2:F2}）"
                      + " —— 改坏法：把 `BuildArmySelector` 里那三行 `AddComponent` / `padding` / `softness` 拿掉 ⇒ 红");
            CheckTrue(armVc == null || (armVc.padding == Vector4.zero && armVc.softness == new Vector2Int(0, 52)),
                      "★ A745：两个参数 = 原版三族实读的 `m_Padding (0,0,0,0)` · `m_Softness (0,52)`"
                      + (armVc != null
                         ? $"（现读 ({armVc.padding.x},{armVc.padding.y},{armVc.padding.z},{armVc.padding.w}) / "
                           + $"({armVc.softness.x},{armVc.softness.y})）"
                         : "（⛔ 节点没找到，本条按上一条的红一起看）"));
            // ⚠️ 软边那半边**另有专门一段**（⑤·h：`CheckSoftCuts` 断 270.94 / 830.03 两条切线位置）——
            //    A745 把「逐件传 `view`」换成节点之后，那一段**照旧带电**（节点 `softness = (0,52)` 与
            //    `VpSoft` 逐字同值）⇒ 本段不重复断它，只断**框那半边**（下面两态）。
            var armScr = sk.ArmyScroll;
            CheckTrue(armScr != null && armScr.OnChanged != null,
                      "（前提）阵营条的滚动区与它的重建回调都在（`OnChanged` = `RebuildArmyCells` 那一条）");
            if (armScr != null) armScr.OnChanged();
            int cellsA = sk.ArmyCells.Count;
            CheckTrue(cellsA > 0,
                      $"（前提）态一：生产框下**建出阵营格**来了（{cellsA} 格）—— 为 0 的话下面态二那条等于没验");
            if (armVc != null && armScr != null && cellsA > 0)
            {
                // ---- 态二：把节点框挪到整块之外 ⇒ 同一句重建应当**一格都不建**
                MenuDraw.ApplyPxRect(armVc.transform, armVc.transform.parent,
                                     new PxRect(armFrame.x1 + 4000f, armFrame.y1 + 4000f,
                                                armFrame.x2 + 4000f, armFrame.y2 + 4000f));
                armScr.OnChanged();
                Check(sk.ArmyCells.Count, 0,
                      "★★ A745 态二：把**节点**那个框挪走 ⇒ 同一句 `RebuildArmyCells` **一格都不建**"
                      + $"（实测 {sk.ArmyCells.Count} 格；态一是 {cellsA} 格）"
                      + " —— 🔴 **改坏法**：把 `Shell/LiveOpsEventWindow.cs` 那一句 "
                      + "`MenuDraw.VisibleAbove(holder, rr, null)` 改回 `MenuDraw.Visible(rr, view)`"
                      + "（= 迁移前的显式矩形那一路）⇒ 态二照旧建满 ⇒ 红"
                      + "｜⛔ 也别只把那 6 处 `MenuDraw.Rect/Nine` 的 `clip` 改回 `view` —— 那还是「两处状态」，"
                      + "正是本迁移要消灭的写法（而且节点会被形参盖住、静默）");
                MenuDraw.ApplyPxRect(armVc.transform, armVc.transform.parent, armFrame);
                armScr.OnChanged();
                CheckTrue(sk.ArmyCells.Count == cellsA,
                          $"★ …还原节点框 ⇒ **又建出同样多格**（实测 {sk.ArmyCells.Count}，态一 {cellsA}）"
                          + " —— 两态都断 ⇒ 上面那个「0」不可能来自别的原因（例如「这一批阵营表是空的」）");
            }
            sk.Close();

            // ============================================================ 收尾
            // ⚠️ 同上（⑤·z 那条纪律）：`ib` / `sk` 都 `Create` + `OpenWindow` 过（= 在 `WindowsManager` 的窗表里）
            //   ⇒ 收尾只 `Close()`、⛔ **不 `DestroyImmediate`**（那会让窗表里留一个 Unity 假 null，
            //   而下面 `shell.Dump()` 要遍历它）。上面对应的 `ib.Close()` / `sk.Close()` 已经逐扇关过。
            shell.Windows.CloseAllWindows();
        }

        // ============================================================ ★ A435·戊：练习窗两处视口
        // 🔴 **迁移前的形状 = 「逐件传 `view` + `ArmyClipSoft` / `DeckClipSoft`」的两处视口**，
        //   与 `Shell/ChatPanel.cs` / `Shell/LiveOpsEventWindow.cs` 迁移前**一模一样**
        //   （那两个文件当年 `grep ViewportClip` 也是零命中）⇒ 调度台裁定「**算 A435 站点、要做**」
        //   （迁移表 §八·5 自陈「`PracticeModePopup` 的视口本件没读全」—— 这一条就是它）。
        //   现在状态长在**本来就有**的那两颗 `Viewport` 节点上（`AddComponent`，⛔ **不是** `Hang` ——
        //   `Filters` / `Content` 本来就挂在它们下面 ⇒ **零结构改动**，同迁移表 §二·A 注①【低风险】那条路）。
        Section("★ A435·戊：练习窗（`Practice Mode Menu`）两处视口的节点态（A754 · 全壳第三处漏网同族站点）");
        {
            // ---- 只本段用的三个读数器（⛔ 不动本文件既有那几个 helper）----
            // 按**框**找那一颗 `ViewportClip`（同 ⑤·z / 丁段的口径：⛔ 不按名字找 —— 本段要断的正是「框对不对」）。
            ViewportClip NodeOfFrame(Transform root, PxRect want)
            {
                if (root == null) return null;
                foreach (var v in root.GetComponentsInChildren<ViewportClip>(true))
                {
                    var cp = v.ClipPx;
                    if (cp.HasValue && NearPx(cp.Value, want)) return v;
                }
                return null;
            }

            // 一棵子树**画出来**的并集（画布 px · 左上原点）。
            // 🔴 **必须取并集**：软边（`MenuDraw.ApplySoftEdges`）会把宿主切成「主格 + 若干子块」
            //   （子块是挂在宿主**下面**的 `ImageQuad`）⇒ 只量宿主那一颗会漏掉大部分面积。
            //   ⚠️ 跳过 `!activeInHierarchy` 的件（画面上没有的东西不算「画出来的并集」）。
            bool UnionPx(Transform node, out float x1, out float y1, out float x2, out float y2)
            {
                x1 = y1 = float.MaxValue; x2 = y2 = float.MinValue;
                bool any = false;
                if (node == null) return false;
                foreach (var q in node.GetComponentsInChildren<ImageQuad>(true))
                {
                    if (q == null || !q.gameObject.activeInHierarchy) continue;
                    float a, b, c, d;
                    if (!QuadPxRect(q, out a, out b, out c, out d)) continue;
                    if (a < x1) x1 = a; if (b < y1) y1 = b;
                    if (c > x2) x2 = c; if (d > y2) y2 = d;
                    any = true;
                }
                return any;
            }

            // 一颗节点**自己的** px 矩形（中心走 `LayoutSpace.ToPixel`、尺寸走 `rect × K`）——
            // 与 `QuadPxRect` **同一份口径**（`MenuDraw.SetPxSize` 把锚点写成重合 ⇒ `rect` 只由 `sizeDelta` 决定）。
            // 命中区那一路没有「主格/子块」这回事（它不吃软边）⇒ 直接量节点本身，就是 `MenuDraw.ClipRectAbove` 的产物。
            bool NodeRectPx(Transform t, out float x1, out float y1, out float x2, out float y2)
            {
                const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;
                x1 = y1 = x2 = y2 = 0f;
                var rt = t as RectTransform;
                if (rt == null) return false;
                Vector2 c = LayoutSpace.ToPixel(t.position);
                float hw = rt.rect.width * K * 0.5f, hh = rt.rect.height * K * 0.5f;
                x1 = c.x - hw; x2 = c.x + hw; y1 = c.y - hh; y2 = c.y + hh;
                return true;
            }

            // ---- 夹具：**不动玩家的真存档**（`DeckStore.OverridePath` 指到临时文件 + 先造几套卡组）----
            //   口径同 `Editor/CollectionScene.cs:760` / `Editor/MainMenuScene.cs:1206` 那两处。
            //   为什么本段**必须**造卡组：卡组视口那一处的格子来自 `CollectionData.DeckCount()`
            //   ⇒ 空库时「一格都不建」永远成立 = 那几条断言**没有鉴别力**（弱断言那一族）。
            //   卡组名**故意长**：名字条是自适应窗口（`ItemNameAutoMin/Max = 8/29`）⇒ 名字会铺满
            //   172px 那条带，下面「节点框切到中间的**文字**那一态」才有东西可切。
            string keepDeckPath = RuleEngine.DeckStore.OverridePath;
            try { System.IO.File.Delete("d:/4/_tmp_view/shell/_shell_a754_decks.json"); } catch { }
            RuleEngine.DeckStore.OverridePath = "d:/4/_tmp_view/shell/_shell_a754_decks.json";
            CollectionData.ResetForTest();
            {
                var lib = RuleEngine.DeckLibrary.Load();
                for (int i = 0; i < 4; i++) lib.Create("戊夹具卡组" + (i + 1) + "号很长很长");
                lib.Save();
            }
            CollectionData.ResetForTest();

            // 本段用的矩形与切点 —— **一律原版字面量**（⛔ 不读 `PracticeModePopup.ArmVp*` / `DecksVp*` /
            // `ArmyCellL` 那些常量：那是被测实现**传进去的实参**，拿它当期望值就是自证）。
            // 出处 = 各常量自己的注释（`bundle_menus_assets_all` 逐处实读 / uGUI `GridLayoutGroup` 现算）。
            const float ArmVpX1 = 69.42f, ArmVpY1 = 149.07f, ArmVpX2 = 246.54f, ArmVpY2 = 913.28f;
            const float DkVpX1 = 261.28f, DkVpY1 = 262.64f, DkVpX2 = 634.88f, DkVpY2 = 803.43f;
            const float ArmCellL = 116.98f, ArmCellT = 149.07f, ArmCellR = 198.98f, ArmCellB = 231.07f;
            const float DkCellL = 270.08f, DkCellT = 262.64f, DkCellR = 450.08f, DkCellB = 442.64f;
            const float ArmCutX = 157.98f;    // = 格 116.98..198.98 的中点
            const float DkCutX = 360.08f;     // = 格 270.08..450.08 的中点

            var pw = PracticeModePopup.Create(shell.Windows);
            shell.Windows.OpenWindow(pw);
            CheckTrue(pw != null && pw.CurrentState == WindowState.Open,
                      "（前提）开出一扇练习窗（本段末尾关掉）—— 它是 `GameWindow` 直系，但不是 `MenuWindowBase` 族");

            // ============================================================ ① 前提：两处视口节点与参数
            var armyVc = NodeOfFrame(pw.transform, new PxRect(ArmVpX1, ArmVpY1, ArmVpX2, ArmVpY2));
            CheckTrue(armyVc != null,
                      "★ A754-a：`Army Selector/Viewport` 那颗节点在（框 = 原版 "
                      + $"69.42,149.07 → 246.54,913.28）—— 它**本来就存在**（`Filters` 挂在它下面），"
                      + "本迁移只 `AddComponent<ViewportClip>()`"
                      + " —— 改坏法：把 `BuildArmySelector` 里那三行 `AddComponent` / `padding` / `softness` 拿掉 ⇒ 红");
            CheckTrue(armyVc == null || (armyVc.padding == Vector4.zero && armyVc.softness == new Vector2Int(0, 50)),
                      "★ A754-a：两个参数 = 原版 `…/Army Selector/Viewport` 那个 `RectMask2D` 的实读"
                      + " `m_Padding (0,0,0,0)` · `m_Softness (0,50)`"
                      + (armyVc != null
                         ? $"（现读 ({armyVc.padding.x},{armyVc.padding.y},{armyVc.padding.z},{armyVc.padding.w}) / "
                           + $"({armyVc.softness.x},{armyVc.softness.y})）"
                         : "（⛔ 节点没找到，本条按上一条的红一起看）")
                      + " —— 改坏法：改任一格 ⇒ 红");

            var deckVc = NodeOfFrame(pw.transform, new PxRect(DkVpX1, DkVpY1, DkVpX2, DkVpY2));
            CheckTrue(deckVc != null,
                      "★ A754-b：`Decks Scroll view/Viewport` 那颗节点在（框 = 原版 "
                      + "261.28,262.64 → 634.88,803.43）—— 同上，它**本来就存在**（`Content` 挂在它下面）"
                      + " —— 改坏法：拿掉 `BuildDeckRows` 里那三行 ⇒ 红");
            CheckTrue(deckVc == null || (deckVc.padding == Vector4.zero && deckVc.softness == new Vector2Int(0, 23)),
                      "★ A754-b：两个参数 = 原版 `…/Decks Scroll view/Viewport` 的实读 "
                      + "`m_Padding (0,0,0,0)` · `m_Softness (0,23)`"
                      + (deckVc != null
                         ? $"（现读 ({deckVc.padding.x},{deckVc.padding.y},{deckVc.padding.z},{deckVc.padding.w}) / "
                           + $"({deckVc.softness.x},{deckVc.softness.y})）"
                         : "（⛔ 节点没找到，本条按上一条的红一起看）")
                      + " —— 改坏法：改任一格 ⇒ 红");
            CheckTrue(armyVc != null && deckVc != null && !ReferenceEquals(armyVc, deckVc),
                      "★ 两处是**两颗不同的节点**（一扇窗里两个视口各自一份状态 —— 这正是 `ViewportClip` 相对"
                      + "「一扇窗一份 `GameWindow.Clip`」的意义；迁移前这一档只能靠「逐件传不同的 `view`」表达）");
            CheckTrue(armyVc == null || deckVc == null || armyVc.softness != deckVc.softness,
                      "★ 两处的软边**不是同一个值**（(0,50) vs (0,23)）—— 铁律 5·c：同一扇窗的两个视口逐处实读、"
                      + "⛔ 别互推｜改坏法：把两处合并成一个共享常量、或把 (0,50) 顶给两处 ⇒ 红"
                      + "（「一扇窗两个视口同时要不同参数」这个案子就是这条在守）");
            CheckTrue(pw.Clip == null && pw.ClipPad == Vector4.zero && pw.ClipSoftness == Vector2.zero,
                      "★ 本窗的**窗级三兄弟全空**（`GameWindow.Clip`/`ClipPad`/`ClipSoftness`）"
                      + " ⇒ 节点是**唯一**的裁切状态载体（本窗从来走的是「逐件传 `clip`」，当年就没设过窗级字段）"
                      + $"（现读 Clip = {(pw.Clip.HasValue ? "非 null" : "null")}）"
                      + " —— 改坏法：照着别的窗的样子往 `BuildArmySelector` 里加一句 `Clip = view` ⇒ "
                      + "**形参赢、节点白挂**（`Resolve` 第 1 支，静默）⇒ 下面 ②·态三 / ③·态三 那四条几何全红");

            CheckTrue(pw.ArmyCells.Count > 0,
                      $"（前提）生产框下**建出阵营格**来了（{pw.ArmyCells.Count} 格）—— 为 0 的话 ② 那几态等于没验");
            CheckTrue(pw.DeckRows.Count > 0,
                      $"（前提）生产框下**建出卡组格**来了（{pw.DeckRows.Count} 格；夹具 4 套 ⇒ 两列 2 行）"
                      + " —— 为 0 的话 ③ 那几态等于没验");

            // ============================================================ ② A754-a：阵营视口（`Army Selector/Viewport`）
            // 夹具 = 第 0 格（`Army_0`）：格 = **116.98,149.07 → 198.98,231.07**（`ArmyCellL` 那一段现算的
            // 横轴居中值 + `ArmView*` 的上沿）⇒ 它在生产框下**完整落在视口里**、被切到 157.98 之后**仍相交**。
            {
                var bg0 = FindChildIn(pw.ArmyCells[0], "Background");
                var hit0 = FindChildIn(pw.ArmyCells[0], "Hit");
                CheckTrue(bg0 != null && hit0 != null,
                          "（前提）第 0 格那两层（`Background` / `Hit`）都在（它们就是本态的读数对象）");

                float ax1, ay1, ax2, ay2;
                bool bgOk = UnionPx(bg0, out ax1, out ay1, out ax2, out ay2);
                CheckTrue(bgOk && NearPx(ax1, ArmCellL) && NearPx(ay1, ArmCellT)
                                && NearPx(ax2, ArmCellR) && NearPx(ay2, ArmCellB),
                          "★ A754-a 态一（**生产框** = 节点那个框 69.42,149.07→246.54,913.28）：第 0 格 `Background` "
                          + $"**画出来**的并集 = 格矩形 116.98,149.07 → 198.98,231.07"
                          + (bgOk ? $"（实测 {ax1:F2},{ay1:F2} → {ax2:F2},{ay2:F2}）" : "（⛔ 并集量不到）")
                          + " —— 🔴 量的是**渲出来那几块 quad 的并集**（软边会把宿主切成主格 + 子块，"
                          + "⛔ 只量宿主那一颗会漏面积），⛔ 不是计数器");

                float hx1, hy1, hx2, hy2;
                bool hOk1 = NodeRectPx(hit0, out hx1, out hy1, out hx2, out hy2);
                CheckTrue(hOk1 && NearPx(hx1, ArmCellL, 0.1f) && NearPx(hx2, ArmCellR, 0.1f),
                          "★ A754-a 态一（对照）：命中区 = **整格**（116.98 → 198.98，宽 82）—— 生产框下它没被截"
                          + (hOk1 ? $"（实测 {hx1:F2} → {hx2:F2}）" : "（⛔ 量不到）"));

                // ⚠️ 防崩守卫：`OnChanged` 是 `System.Action`，直接 `.OnChanged()` 会在 null 上抛 NRE
                //    —— **自检崩掉比红更糟**（后面几条一起跑不到）。所以三个条件都进 if，缺了走 else 那条红。
                if (armyVc != null && pw.ArmyScroll != null && pw.ArmyScroll.OnChanged != null
                    && bg0 != null && bgOk && hOk1)
                {
                    // ---- 态三（**节点框**右沿切到格中点）：同一个节点、同一句重建 ⇒ 画出来与点得到的都跟着收 ----
                    MenuDraw.ApplyPxRect(armyVc.transform, armyVc.transform.parent,
                                         new PxRect(ArmVpX1, ArmVpY1, ArmCutX, ArmVpY2));
                    pw.ArmyScroll.OnChanged();
                    CheckTrue(pw.ArmyCells.Count > 0,
                              $"（前提）切窄之后第 0 格**仍然建得出来**（{pw.ArmyCells.Count} 格）—— "
                              + "它是半格可见、不是整格在框外 ⇒ 下面两条不是空转");
                    var bg0c = pw.ArmyCells.Count > 0 ? FindChildIn(pw.ArmyCells[0], "Background") : null;
                    var hit0c = pw.ArmyCells.Count > 0 ? FindChildIn(pw.ArmyCells[0], "Hit") : null;
                    float cx1, cy1, cx2, cy2;
                    bool bgOkC = UnionPx(bg0c, out cx1, out cy1, out cx2, out cy2);
                    CheckTrue(bgOkC && NearPx(cx2, ArmCutX, 0.6f),
                              "★★★ A754-a 态三（**节点态**）：把**节点那个框**的右沿切到格中点 "
                              + $"(x2 = {ArmCutX:F2}) ⇒ 第 0 格 `Background` **画出来**的并集右沿也被切到它"
                              + (bgOkC ? $"（实测 {cx2:F2}；期望 {ArmCutX:F2}；态一是 {ax2:F2}）" : "（⛔ 并集量不到）")
                              + " —— 🔴 **改坏法**：把 `Shell/PracticeModePopup.cs` 的 `ImgTex` 里那句 "
                              + "`ViewportClip.Resolve` 删掉（退回裸 `clip` 形参 ⇒ 逐件传的 `null` 让它**一次都不调** "
                              + "`ApplySoftEdges`）⇒ 并集右沿回到 **198.98** ⇒ 红"
                              + "｜🔴 前提是「逐件传的 `clip` 已经是 `null`」这件事**真的生效了**："
                              + "若有人把那几处改回 `view`，节点会被形参盖住 ⇒ 同样红（`Resolve` 第 1 支）");
                    CheckTrue(bgOkC && NearPx(cx1, ArmCellL, 0.6f),
                              "★ …而**左沿没动**（" + (bgOkC ? $"{cx1:F2}" : "量不到") + $" vs 态一 {ax1:F2}）"
                              + " —— 这一刀是**按新框裁**，不是把整块平移过去（平移的实现这里红）");
                    float jx1, jy1, jx2, jy2;
                    bool hOkC = NodeRectPx(hit0c, out jx1, out jy1, out jx2, out jy2);
                    CheckTrue(hOkC && NearPx(jx2, ArmCutX, 0.1f) && NearPx(jx1, ArmCellL, 0.1f),
                              "★★ A754-a 态三（**命中区**那一半）：同一个框切窄 ⇒ 命中区也截到框内 "
                              + $"（{ArmCellL:F2} → {ArmCutX:F2}，宽 **41.00**）"
                              + (hOkC ? $"（实测 {jx1:F2} → {jx2:F2}）" : "（⛔ 量不到）")
                              + " —— 🔴 **改坏法**：把 `HitOn` 里那句 `MenuDraw.ClipRectAbove(parent, r, clip, out hr)` "
                              + "改回 `MenuDraw.ClipRect(r, clip, out hr)`（= 迁移前那一版；`clip` 现在是 `null` ⇒ "
                              + "`ClipRect` 第一句就原样放行）⇒ 命中区回到整格 82 宽 ⇒ 红。"
                              + "⚠️ 这一条与上一条**各咬一处**：上一条咬 `ImgTex` 的解析、这一条咬 `HitOn` 的求交口");

                    // ---- 态二（**粗筛**那半边）：把整块框挪到整块之外 ⇒ 同一句重建**一格都不建**；还原 ⇒ 又建满 ----
                    int cellsN = pw.ArmyCells.Count;
                    MenuDraw.ApplyPxRect(armyVc.transform, armyVc.transform.parent,
                                         new PxRect(ArmVpX1 + 4000f, ArmVpY1 + 4000f, ArmVpX2 + 4000f, ArmVpY2 + 4000f));
                    pw.ArmyScroll.OnChanged();
                    Check(pw.ArmyCells.Count, 0,
                          "★★ A754-a 态二（对照 = 上面的生产框）：把**节点**那个框挪到整块之外 ⇒ 同一句 "
                          + "`RebuildArmyCells` **一格都不建**"
                          + $"（实测 {pw.ArmyCells.Count} 格；切窄那一态建了 {cellsN} 格）"
                          + " —— 🔴 **改坏法**：把那一句 `MenuDraw.VisibleAbove(holder, rr, null)` 改回 "
                          + "`MenuDraw.Visible(rr, view)`（= 迁移前的显式矩形那一路）⇒ 态二照旧建满 ⇒ 红"
                          + "｜⛔ 也别只把四处 `MenuDraw.Rect` 的 `clip` 改回 `view` —— 那是「两处状态」的老写法"
                          + "（而且节点会被形参盖住、静默），正是本迁移要消灭的东西");
                    MenuDraw.ApplyPxRect(armyVc.transform, armyVc.transform.parent,
                                         new PxRect(ArmVpX1, ArmVpY1, ArmVpX2, ArmVpY2));
                    pw.ArmyScroll.OnChanged();
                    CheckTrue(pw.ArmyCells.Count == cellsN,
                              $"★ …把节点框**还原** ⇒ 又建出同样多格（实测 {pw.ArmyCells.Count}，切窄时 {cellsN}）"
                              + " —— 两态都断 ⇒ 上面那个「0」不可能来自别的原因（例如「这一批阵营表是空的」）");
                }
                else
                    CheckTrue(false, "（前提）`Army Selector/Viewport` 那颗节点、或第 0 格的 `Background` 拿不到"
                                   + " ⇒ A754-a 那四态**没跑**（⛔ 不静默跳过）");
                if (bg0 != null && !bgOk)
                    CheckTrue(false, "（前提）第 0 格 `Background` 量不到渲染并集（`UnionPx`）");
            }

            // ============================================================ ③ A754-b：卡组视口（`Decks Scroll view/Viewport`）
            // 夹具 = `DeckRow_0`：格 = **270.08,262.64 → 450.08,442.64**（`DecksCL` + pad 4 + `GetStartOffset` 4.02
            // 现算的第 1 列左沿 + `DecksVp*` 的上沿）。
            {
                var rowBg0 = pw.DeckRows.Count > 0 ? FindChildIn(pw.DeckRows[0], "Row Bg") : null;
                var rowHit0 = pw.DeckRows.Count > 0 ? FindChildIn(pw.DeckRows[0], "Hit") : null;
                CheckTrue(rowBg0 != null && rowHit0 != null,
                          "（前提）`DeckRow_0` 那两层（`Row Bg` / `Hit`）都在");

                float bx1, by1, bx2, by2;
                bool bgOk = UnionPx(rowBg0, out bx1, out by1, out bx2, out by2);
                CheckTrue(bgOk && NearPx(bx1, DkCellL) && NearPx(by1, DkCellT)
                                && NearPx(bx2, DkCellR) && NearPx(by2, DkCellB),
                          "★ A754-b 态一（**生产框** = 261.28,262.64→634.88,803.43）：`DeckRow_0` 的 `Row Bg` "
                          + "**画出来**的并集 = 格矩形 270.08,262.64 → 450.08,442.64"
                          + (bgOk ? $"（实测 {bx1:F2},{by1:F2} → {bx2:F2},{by2:F2}）" : "（⛔ 并集量不到）"));

                float hx1, hy1, hx2, hy2;
                bool hOk1 = NodeRectPx(rowHit0, out hx1, out hy1, out hx2, out hy2);
                CheckTrue(hOk1 && NearPx(hx1, DkCellL, 0.1f) && NearPx(hx2, DkCellR, 0.1f),
                          "★ A754-b 态一（对照）：命中区 = **整格**（270.08 → 450.08，宽 180）"
                          + (hOk1 ? $"（实测 {hx1:F2} → {hx2:F2}）" : "（⛔ 量不到）"));

                // 名字条的 TMP 顶点跨度（态一 = 未切）—— 态三要拿它当对照（两态都断）。
                var nm0 = pw.DeckRows.Count > 0 ? FindChildIn(pw.DeckRows[0], "Deck Name") : null;
                var nmLb0 = nm0 != null ? nm0.GetComponent<Label>() : null;
                float nmX1 = 0f, nmY1 = 0f, nmX2 = 0f, nmY2 = 0f;
                bool nmOk = nmLb0 != null && TmpSpanPx(nmLb0, out nmX1, out nmY1, out nmX2, out nmY2);
                CheckTrue(nmOk && nmX2 > DkCutX + 4f,
                          "（前提）态一那段卡组名**量得到顶点跨度**、而且**右沿越过切点**"
                          + (nmOk ? $"（实测 x {nmX1:F2}..{nmX2:F2}；切点 {DkCutX:F2}）" : "（⛔ 取不到网格）")
                          + " —— 它 ≤ 切点的话下面「被切掉一块」那条等于没验。"
                          + "🔴 量的是 **TMP 渲染网格的顶点**（`textInfo.meshInfo[].vertices`，就是 "
                          + "`MenuDraw.ClipTmpMesh` 写、`UpdateVertexData` 推给渲染的那一份），⛔ 不是 `Label.WorldW`");
                // ⚠️ **如实登记（同族已知坑）**：这一段字走 `Label.SetAutoFitBox`（`ItemNameAutoMin/Max = 8/29`）
                //   ⇒ **改坏法不一定把绿翻红**：若那把刀让自适应窗口跟着变，字号会重算、跨度跟着动，
                //   下面的容差可能刚好放过去。⇒ 上面那条「态一右沿越过切点」的**前提**就是给这一档兜底的
                //   （前提红 = 这一段没鉴别力，会被看见，⛔ 不会静默变绿）。

                if (deckVc != null && pw.DeckScroll != null && pw.DeckScroll.OnChanged != null
                    && rowBg0 != null && bgOk && hOk1)
                {
                    int rowsN = pw.DeckRows.Count;
                    MenuDraw.ApplyPxRect(deckVc.transform, deckVc.transform.parent,
                                         new PxRect(DkVpX1, DkVpY1, DkCutX, DkVpY2));
                    pw.DeckScroll.OnChanged();
                    CheckTrue(pw.DeckRows.Count > 0,
                              $"（前提）切窄之后 `DeckRow_0` **仍然建得出来**（现共 {pw.DeckRows.Count} 格 —— "
                              + "第 2 列那一格整块落在框外，所以**变少是应该的**）");
                    var rowBg0c = pw.DeckRows.Count > 0 ? FindChildIn(pw.DeckRows[0], "Row Bg") : null;
                    var rowHit0c = pw.DeckRows.Count > 0 ? FindChildIn(pw.DeckRows[0], "Hit") : null;
                    float cx1, cy1, cx2, cy2;
                    bool bgOkC = UnionPx(rowBg0c, out cx1, out cy1, out cx2, out cy2);
                    CheckTrue(bgOkC && NearPx(cx2, DkCutX, 0.6f),
                              "★★★ A754-b 态三（**节点态**）：节点框右沿切到格中点 "
                              + $"(x2 = {DkCutX:F2}) ⇒ `Row Bg` **画出来**的并集右沿也被切到它"
                              + (bgOkC ? $"（实测 {cx2:F2}；期望 {DkCutX:F2}；态一是 {bx2:F2}）" : "（⛔ 量不到）")
                              + " —— 🔴 **改坏法**：同 A754-a 那条（删 `ImgTex` 里的 `Resolve` ⇒ 并集右沿回 450.08）");
                    CheckTrue(bgOkC && NearPx(cx1, DkCellL, 0.6f),
                              "★ …而**左沿没动**（" + (bgOkC ? $"{cx1:F2}" : "量不到") + $" vs 态一 {bx1:F2}）");

                    float jx1, jy1, jx2, jy2;
                    bool hOkC = NodeRectPx(rowHit0c, out jx1, out jy1, out jx2, out jy2);
                    CheckTrue(hOkC && NearPx(jx2, DkCutX, 0.1f) && NearPx(jx1, DkCellL, 0.1f),
                              "★★ A754-b 态三（**命中区**那一半）：命中区也截到框内 "
                              + $"（{DkCellL:F2} → {DkCutX:F2}，宽 **90.00**）"
                              + (hOkC ? $"（实测 {jx1:F2} → {jx2:F2}）" : "（⛔ 量不到）")
                              + " —— 🔴 **改坏法**：把 `HitOn` 的 `ClipRectAbove` 改回 `ClipRect` ⇒ 命中区回到 180 宽 ⇒ 红");

                    // ---- 文字那一半：`Txt` 走的是 `MenuDraw.ClipText`（那条路**自己**解析节点）----
                    float kx1 = 0f, ky1 = 0f, kx2 = 0f, ky2 = 0f;
                    var nmc = pw.DeckRows.Count > 0 ? FindChildIn(pw.DeckRows[0], "Deck Name") : null;
                    var nmLbc = nmc != null ? nmc.GetComponent<Label>() : null;
                    bool nmOkC = nmLbc != null && TmpSpanPx(nmLbc, out kx1, out ky1, out kx2, out ky2);
                    CheckTrue(nmOkC && kx2 <= DkCutX + 1.5f,
                              "★★ A754-b 态三（**文字**那一半）：同一段卡组名的渲染顶点也被夹到框沿 "
                              + $"(≤ {DkCutX:F2})"
                              + (nmOkC ? $"（实测 x {kx1:F2}..{kx2:F2}）" : "（⛔ 取不到网格）")
                              + " —— 原版 `RectMask2D` 对 TMP **一视同仁**（掩码在 shader 里按像素裁）"
                              + "｜改坏法：把 `Txt` 里那句 `MenuDraw.ClipText(lb, clip, clipSoftness)` 删掉 ⇒ "
                              + "右沿回到未切值 ⇒ 红");
                    CheckTrue(nmOkC && nmOk && (nmX2 - kx2) >= 4f,
                              "★ …而且是**真被切掉了一块**（不是「本来就那么窄」）"
                              + (nmOkC && nmOk ? $"（态一右沿 {nmX2:F2} − 态三右沿 {kx2:F2} = {nmX2 - kx2:F2} px）"
                                               : "（⛔ 有一态量不到）")
                              + " —— 两态都断 ⇒ 上面那条 `≤ 切点` 不可能来自「这段字本来就短」");
                    CheckTrue(nmOkC && nmOk && NearPx(kx1, nmX1, 1.5f),
                              "★ …而**左沿没动**（" + (nmOkC && nmOk ? $"{kx1:F2} vs 态一 {nmX1:F2}" : "⛔ 有一态量不到")
                              + "）—— 是按新框重裁，不是把整段平移过去（平移的实现这里红）");

                    // ---- 态二（**粗筛**那半边）----
                    int rowsNow = pw.DeckRows.Count;
                    MenuDraw.ApplyPxRect(deckVc.transform, deckVc.transform.parent,
                                         new PxRect(DkVpX1 + 4000f, DkVpY1 + 4000f, DkVpX2 + 4000f, DkVpY2 + 4000f));
                    pw.DeckScroll.OnChanged();
                    Check(pw.DeckRows.Count, 0,
                          "★★ A754-b 态二（对照）：把**节点**那个框挪到整块之外 ⇒ 同一句 `RebuildDeckRows` "
                          + $"**一格都不建**（实测 {pw.DeckRows.Count} 格；切窄那一态是 {rowsNow} 格）"
                          + " —— 🔴 **改坏法**：把 `MenuDraw.VisibleAbove(holder, rr, null)` 改回 "
                          + "`MenuDraw.Visible(rr, view)` ⇒ 照旧建满 ⇒ 红");
                    MenuDraw.ApplyPxRect(deckVc.transform, deckVc.transform.parent,
                                         new PxRect(DkVpX1, DkVpY1, DkVpX2, DkVpY2));
                    pw.DeckScroll.OnChanged();
                    CheckTrue(pw.DeckRows.Count == rowsN,
                              $"★ …还原节点框 ⇒ 又建出同样多格（实测 {pw.DeckRows.Count}，态一 {rowsN}）"
                              + " —— 两态都断");
                }
                else
                    CheckTrue(false, "（前提）`Decks Scroll view/Viewport` 那颗节点、或 `DeckRow_0` 的 `Row Bg` 拿不到"
                                   + " ⇒ A754-b 那几态**没跑**（⛔ 不静默跳过）");
            }

            // ============================================================ 收尾
            // ⚠️ 同 ⑤·z / 丁段的纪律：`pw` 是 `Create` + `OpenWindow` 过（= 在窗表里）⇒ 只 `Close()`、
            //   ⛔ **不 `DestroyImmediate`**（那会让窗表里留一个 Unity 假 null，而下面 `shell.Dump()` 要遍历它）。
            // ⚠️ **存档还回去**（本段把那两处节点框逐处还原过；`OverridePath` 与 `CollectionData` 的缓存
            //   也要复位 —— 留着会让「读卡组」这件事在别的段里变成读这 4 套夹具）。
            pw.Close();
            RuleEngine.DeckStore.OverridePath = keepDeckPath;
            CollectionData.ResetForTest();
            shell.Windows.CloseAllWindows();
        }

        // ============================================================ ★ A465（W-A435己）：`MenuScroll.Intersects` 接上【视口节点】
        Section("★ A465（W-A435己）：构建循环那一行吃节点态 —— 与「画出来被裁掉」读同一份框（档案五页 · 榜单 · 战役/锻造四区）");
        {
            // 判据 / 契约 → `Shell/MenuScroll.cs` 的 `ClipNode` 注释（那一侧 = A465 的 `MenuScroll` 半边）：
            //   `Intersects(onScreen)` = `MenuDraw.Visible(onScreen, ClipNode != null ? ClipNode.State.RenderClip : Viewport)`。
            // 本段验的是**宿主那一行**：`Shell/{AvatarTab,TitleTab,AchievementsMenu,RankedTab,BattleLogTab}.cs` ·
            // `Shell/LeaderboardWindow.cs` · `Shell/CampaignTab.cs` · `Shell/ForgeTab.cs` 里各 `ClipNode = <视口节点>`。
            // 🔴 **四态 + 一条独立的结构判据**（少一条就可能「两边一起改回去还是全绿」）：
            //    ① 生产框 ⇒ 探针**可见**；② **把节点框整体搬走 +4000px** ⇒ 探针**不可见**（只有真读节点才会这样）；
            //    ③ `ClipNode = null`（**回落态**）⇒ 节点搬到哪都不管、探针**又可见**；
            //    ④ 节点还原 + `ClipNode` 放回 ⇒ 又**可见**（两态都断 ⇒ ② 那个 false 不可能来自别的原因）。
            // ⛔ 一条计数器都不读（`NodeResolutions` / `NodeShadowedByParam` / `UnusableNodes` 全不碰）；
            //    期望值全部独立算：探针 = `Viewport` **正中** 10×10（`Viewport` 是宿主写的设计矩形、与节点无关）。
            void Vp4(string who, MenuScroll sc, Transform content, string nodeName)
            {
                CheckTrue(sc != null && content != null,
                          $"（前提）{who}：滚动区与内容件都拿得到 —— ⛔ 拿不到就**不静默跳过**（下面还会单报一条）");
                if (sc == null || content == null)
                {
                    CheckTrue(false, $"（前提·不静默）{who}：`MenuScroll` 或内容件拿不到 ⇒ 那四态**没跑**");
                    return;
                }
                var vc = sc.ClipNode;
                var above = ViewportClip.FindAbove(content);
                CheckTrue(vc != null && vc == above && vc.name == nodeName,
                          $"★ {who}：`ClipNode` 就是本页那颗 `{nodeName}` 节点 —— 🔴 **独立判据** = 从内容件 "
                          + $"「{content.name}」沿父链 `ViewportClip.FindAbove` 找 = {(above != null ? above.name : "null")}"
                          + $"；`ClipNode` = {(vc != null ? vc.name : "null")}"
                          + "。改坏法：删掉宿主那一行 `ClipNode = …;`（⇒ null）或指到别的节点（⇒ 身份/名字对不上）");
                if (vc == null || vc != above) return;
                var keep = vc.ClipPx;
                if (!keep.HasValue)
                {
                    CheckTrue(false, $"（前提·不静默）{who}：那颗节点给不出框（`ClipPx == null`）⇒ 那四态**没跑**");
                    return;
                }
                // 前提：生产态下「节点框」必须 ≈ 宿主写进 `Viewport` 的那个矩形
                //（差 ~1e-4px 的浮点残差 —— 节点框是世界坐标反推回来的，A497 实测 −6.1e-5）
                CheckTrue(NearPx(keep.Value, sc.Viewport, 0.05f),
                          $"★ {who}：节点框与 `MenuScroll.Viewport` 在生产态同值（实测 "
                          + $"{keep.Value.x1:F3},{keep.Value.y1:F3}→{keep.Value.x2:F3},{keep.Value.y2:F3} vs "
                          + $"{sc.Viewport.x1:F3},{sc.Viewport.y1:F3}→{sc.Viewport.x2:F3},{sc.Viewport.y2:F3}）"
                          + " —— 这是下面「搬走 ⇒ 不可见」那条的前提（⛔ 别拿搬走的量当期望值）");
                var vpr = sc.Viewport;
                var probe = new PxRect(vpr.CX - 5f, vpr.CY - 5f, vpr.CX + 5f, vpr.CY + 5f);
                bool s0 = sc.Intersects(probe);
                MenuDraw.ApplyPxRect(vc.transform, vc.transform.parent,
                                     new PxRect(keep.Value.x1 + 4000f, keep.Value.y1 + 4000f,
                                                keep.Value.x2 + 4000f, keep.Value.y2 + 4000f));
                bool s1 = sc.Intersects(probe);
                var saved = sc.ClipNode;
                sc.ClipNode = null;                       // 回落态 = 迁移前的行为（只看 `Viewport` 那个常数）
                bool s2 = sc.Intersects(probe);
                sc.ClipNode = saved;
                MenuDraw.ApplyPxRect(vc.transform, vc.transform.parent, keep.Value);   // 还原
                bool s3 = sc.Intersects(probe);
                CheckTrue(s0 && !s1 && s2 && s3,
                          $"★★★ {who} 四态：生产框 **可见**(s0={s0}) · **节点搬走 ⇒ 不可见**(s1={s1}) · "
                          + $"**回落态**（`ClipNode = null`）⇒ **又可见**(s2={s2}) · 节点还原 ⇒ **可见**(s3={s3})"
                          + "（探针 = 视口正中 10×10px）"
                          + " —— 改坏法：① 把 `Intersects` 写回 `MenuDraw.Visible(onScreen, Viewport)` ⇒ **s1 变 true** ⇒ 红；"
                          + "② 把回落那一支删掉（`ClipNode == null` 就返回 false）⇒ **s2 变 false** ⇒ 红；"
                          + "③ 宿主不喂 `ClipNode` ⇒ 上面那条结构判据先红。⛔ 本段不读任何计数器");
            }

            // ---- ① 档案窗五页（Avatar / Title / Trophies / Ranking / BattleLog）----
            var profW2 = PlayerProfileWindow.Create(shell.Windows);
            shell.Windows.OpenWindow(profW2);
            var pgAv2 = profW2.Page(WindowTabType.ProfileAvatar) as AvatarTab;
            var pgTi2 = profW2.Page(WindowTabType.ProfileTitle) as TitleTab;
            var pgTr2 = profW2.Page(WindowTabType.ProfileTrophies) as AchievementsMenu;
            var pgRk2 = profW2.Page(WindowTabType.ProfileRanking) as RankedTab;
            var pgBl2 = profW2.Page(WindowTabType.ProfileBattleLog) as BattleLogTab;
            CheckTrue(pgAv2 != null && pgTi2 != null && pgTr2 != null && pgRk2 != null && pgBl2 != null,
                      "（前提）档案窗五个页都建出来了（本段要用到其中五条滚动区）");
            if (pgAv2 != null)
                Vp4("AvatarTab（`Avatar Tab/Item Display Panel/Scroll Rect`）",
                    pgAv2.RowsScroll, FindChildIn(pgAv2.transform, "Item Drawer"), "Scroll Rect");
            if (pgTi2 != null)
                Vp4("TitleTab（`Title Tab/Item Display Panel/Scroll Rect`）",
                    pgTi2.RowsScroll, FindChildIn(pgTi2.transform, "Item Drawer"), "Scroll Rect");
            if (pgTr2 != null)
                Vp4("AchievementsMenu（`Trophies Tab/Scroll/Viewport`）",
                    pgTr2.RowsScroll, FindChildIn(pgTr2.transform, "ContainerHolder"), "Viewport");
            if (pgRk2 != null)
                Vp4("RankedTab（`Ranking Tab/AllFactions/scroll rect/viewport`）",
                    pgRk2.RowsScroll,
                    // 🔴 **2026-10-14（清单 #16 / D2 #5）**：本页有**两颗**叫 `content` 的节点 ——
                    //    `Shell/RankedTab.cs:285` 的 `Top4/content` 与 `:417` 的
                    //    `AllFactions/scroll rect/viewport/content`（`_content`，**要找的就是它**）。
                    //    而 `FindChildIn`（`:229-235`）是 `GetComponentsInChildren` **取第一个**、`Top4` 先建
                    //    ⇒ 原来命中的是 `Top4/content`、它的父链上没有 `ViewportClip` ⇒ `FindAbove` 恒 null ⇒ 假红。
                    //    ⇒ 按内容限定到 `AllFactions` 那一支（该子树里 `content` 唯一）。
                    FindChildIn(FindChildIn(pgRk2.transform, "AllFactions"), "content"), "viewport");
            if (pgBl2 != null)
                Vp4("BattleLogTab（`Battle Log Tab/Matches/Viewport`）", pgBl2.RowsScroll,
                    FindChildIn(FindChildIn(pgBl2.transform, "Matches"), "Content"), "Viewport");
            profW2.Close();

            // ---- ② 榜单（弹窗那一棵 = A32 那颗节点；**每次 `Build()` 新建** `_scroll`，见宿主注释）----
            var lb2 = LeaderboardWindow.Create(shell.Windows, LeaderboardKind.Skirmish);
            shell.Windows.OpenWindow(lb2);
            Vp4("LeaderboardWindow（`Scroll View/Viewport`）", lb2.RowsScroll,
                FindChildIn(FindChildIn(lb2.transform, "Scroll View"), "Content"), "Viewport");
            lb2.Close();

            // ---- ③ 战役页 / 锻造页各两条滚动区（本段**自己 `Create` 一扇 `RewardsWindow`** ——
            //         ⛔ 不碰 `Editor/RewardsScene.cs`：那是甲块的文件）----
            var rw2 = RewardsWindow.Create(shell.Windows);
            // 🔴 **2026-10-14（清单 #17 / D2 #6）**：`RewardsWindow.Create` **不自开窗**
            //   （`Shell/RewardsWindow.cs:210` 只 `new GameObject` + 挂组件 + `AttachToAnchor`、⛔ 不调
            //   `mgr.OpenWindow`）⇒ 少了这一句 `CurrentState` 恒 `Closed`、`Open()` 从不跑、**树是空的**
            //   ⇒ `Tabs` / `Campaign Tab` / `Forge Tab` 全取不到 ⇒ 本段那条「（前提）`RewardsWindow` 的页
            //     都建出来了」红的根因（下面两条 `Vp4` 有 `if (ct2 != null)` / `if (ft2 != null)` 守着 ⇒ 不再级联）。
            //   对照：同段 ①（档案窗 `:4388`）/ ②（榜单 `:4422`）都是 `Create` **+ `OpenWindow`**，只这一处漏了。
            //   ⚠️ 纪律（D2 §五·2）：**`Create` 到底开不开窗 —— 逐窗现读、别按族推**
            //   （`LeaderboardWindow` / `RewardsWindow` / `InboxWindow` 不自开；走 `WindowsManager.OpenXxx`
            //    那一族与 `PurchasePremiumWindow.Create` 会开）。
            shell.Windows.OpenWindow(rw2);
            var tabHolder2 = FindChildIn(rw2.transform, "Tabs");
            var ctNode = tabHolder2 != null ? FindChildIn(tabHolder2, "Campaign Tab") : null;
            var ftNode = tabHolder2 != null ? FindChildIn(tabHolder2, "Forge Tab") : null;
            var ct2 = ctNode != null ? ctNode.GetComponent<CampaignTab>() : null;
            var ft2 = ftNode != null ? ftNode.GetComponent<ForgeTab>() : null;
            CheckTrue(ct2 != null && ft2 != null,
                      "（前提）`RewardsWindow` 的页都建出来了（`BuildTabContents` → 逐页 `Setup()`）");
            if (ct2 != null)
            {
                Vp4("CampaignTab 轨道（`Campaign Track/Viewport`）", ct2.TrackScroll,
                    FindChildIn(FindChildIn(ct2.transform, "Campaign Track"), "Content"), "Viewport");
                Vp4("CampaignTab 阵营条（`Campaign Army Selector/Viewport`）", ct2.ArmyScroll,
                    FindChildIn(FindChildIn(ct2.transform, "Campaign Army Selector"), "Army Content"), "Viewport");
            }
            if (ft2 != null)
            {
                Vp4("ForgeTab 奖励轨（`Rewards Scroll View/Viewport` —— 全壳唯一非零 pad `(10,0,0,0)`）",
                    ft2.TrackScroll,
                    FindChildIn(FindChildIn(ft2.transform, "Rewards Scroll View"), "Rewards Content"), "Viewport");
                Vp4("ForgeTab 阵营条（`Forge Army Selector/Viewport`）", ft2.ArmyScroll,
                    FindChildIn(FindChildIn(ft2.transform, "Forge Army Selector"), "Army Content"), "Viewport");
            }

            // ---- 收尾（同丁段 / 戊段那条纪律：只 `Close()`、⛔ 不 `DestroyImmediate` —— 下面 `shell.Dump()` 要遍历窗表）----
            rw2.Close();                                  // ⚠️ 本窗**没开过**（`Create` 出来当夹具用）⇒ 这句只是别把它留在复用表里
            shell.Windows.CloseAllWindows();
        }

        Section("★ A435·庚（W-A435庚）：三处「视口节点上没有 `ViewportClip`」的补挂（A768）＋ 榜单军种条的结构缺口（A769）＋ 高级版窗的注释订正（A770）");
        {
            // 🔴 本段验的是**三处补挂**（`Shell/LeaderboardWindow.cs` 的军种条 · `Shell/PurchasePremiumWindow.cs` ·
            //   `Shell/RankedRewardEventWindow.cs` 的 `Scroll View/Viewport`）—— 它们是 A435 阶段 2 的**最后一档**：
            //   那三颗 `Viewport` 节点上**根本没有 `ViewportClip`**（照旧 `Node(...)` 建）⇒ 「补一行 `ClipNode = …`」
            //   接不上，必须先**挂组件**（调度台按 A754 那条同族先例裁定「算 A435 站点 ⇒ 要做」）。
            //
            // 🎯 **三条判据全部回原版复核过**（⛔ 不是照抄 `Shell/*.cs` 里那些「实读」注释）——
            //   本件直接读 `bundle_menus_assets_all/MonoBehaviour/` 里那颗 `RectMask2D` 的 MB 本体，
            //   并按它的 `m_GameObject` → `m_Father` 父链上行认回节点（三份都走了一遍）：
            //     · `MonoBehaviour_3897231031841831396.json` = `RankedSkirmishLeaderboardPopup/Ranking Display/Content/Army Selector/Viewport`
            //       ⇒ `m_Padding (0,0,0,0)` · `m_Softness (42,0)`；
            //     · `MonoBehaviour_-8892924525201795015.json` = `Purchase Premium Window/Scroll View/Viewport`
            //       ⇒ `m_Padding (0,0,0,0)` · `m_Softness (0,0)`；
            //     · `MonoBehaviour_4030138604520103632.json` = `Ranked Boost Reward Event Window/window/Scroll View/Viewport`
            //       ⇒ `m_Padding (0,0,0,0)` · `m_Softness (55,0)`。
            //   三颗都是 `m_Enabled 1`、`m_Script.m_PathID = 536591447201701790`
            //   （= `bundle_Waprforge_monoscripts/MonoScript_536591447201701790.json` 的 `UnityEngine.UI.RectMask2D`）。
            //   ⇒ 三处的 `pad`/`soft` 断言里写的是**原版字面量**（⛔ 一处都没从被断的实现里读）。
            //
            // ⛔ **一条计数器都不读**（`NodeResolutions` / `NodeShadowedByParam` / `UnusableNodes` 全不碰）；
            //    期望值全部独立算（探针 = `MenuScroll.Viewport` 正中 10×10；条目级那几条 = 原版几何字面量）。
            // ⚠️ **本段从没跑过 Unity**（铁律 12：A 表清零前中途不跑）⇒ 第一次跑可能吃假红：
            //    **红了先量实得值、⛔ 别改期望值**。

            // ---- 只本段用的几个读数器（⛔ 不动本文件既有那几个 helper，也不动丙/丁/戊/己那几段）----
            // 按**框**找那一颗 `ViewportClip`（口径同戊段 / 己段：先按框认，再断别的 —— ⛔ 不按名字找）。
            ViewportClip GNode(Transform root, PxRect want)
            {
                if (root == null) return null;
                foreach (var v in root.GetComponentsInChildren<ViewportClip>(true))
                {
                    var cp = v.ClipPx;
                    if (cp.HasValue && NearPx(cp.Value, want)) return v;
                }
                return null;
            }

            // 一棵子树**画出来**的并集（画布 px · 左上原点）—— 口径同戊段：
            // 🔴 **必须取并集**：软边（`MenuDraw.ApplySoftEdges`）会把宿主切成「主格 + 若干子块」。
            // ⚠️ 跳过 `!activeInHierarchy` 的件（画面上没有的东西不算「画出来的并集」）。
            bool GUnion(Transform node, out float x1, out float y1, out float x2, out float y2)
            {
                x1 = y1 = float.MaxValue; x2 = y2 = float.MinValue;
                bool any = false;
                if (node == null) return false;
                foreach (var q in node.GetComponentsInChildren<ImageQuad>(true))
                {
                    if (q == null || !q.gameObject.activeInHierarchy) continue;
                    float a, b, c, d;
                    if (!QuadPxRect(q, out a, out b, out c, out d)) continue;
                    if (a < x1) x1 = a; if (b < y1) y1 = b;
                    if (c > x2) x2 = c; if (d > y2) y2 = d;
                    any = true;
                }
                return any;
            }

            // 一颗节点**自己的** px 矩形（中心走 `LayoutSpace.ToPixel`、尺寸走 `rect × K`）——
            // 与 `QuadPxRect` 同一份口径（`MenuDraw.SetPxSize` 把锚点写成重合 ⇒ `rect` 只由 `sizeDelta` 决定）。
            // 🔴 **2026-10-14 订正**：这行原来写着「命中区那一路不吃软边 ⇒ 直接量节点本身就是真值」——
            //   **后半句是错的**：`MenuDraw.Hit` 把节点摆在**父原点**、真矩形长在它的 quad 上（见 `GHitRect`）
            //   ⇒ 量节点**只有在**「父中心 == 框中心」时才对（态一撞对、态二差 `交集.W/2`）。
            //   命中区要吃的那一路请用 `GHitRect`；本函数留给「量一个**非命中区**节点的框」用。
            bool GNodeRect(Transform t, out float x1, out float y1, out float x2, out float y2)
            {
                const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;
                x1 = y1 = x2 = y2 = 0f;
                var rt = t as RectTransform;
                if (rt == null) return false;
                Vector2 c = LayoutSpace.ToPixel(t.position);
                float hw = rt.rect.width * K * 0.5f, hh = rt.rect.height * K * 0.5f;
                x1 = c.x - hw; x2 = c.x + hw; y1 = c.y - hh; y2 = c.y + hh;
                return true;
            }

            // 🆕 **2026-10-14**：命中区那颗 **quad** 的 px 矩形（**设计帧**）。
            //   🔴 **为什么不能量 `Hit` 节点本身**：`MenuDraw.Hit` **故意**把那个节点摆在**父原点**
            //   （只写尺寸、`localPosition = 0`；真鼠标也读 quad —— `Shell/PointerLayer.cs` 的 `HitBoxPx`
            //   就是「quad 世界中心 + `WorldW/H`」）⇒ 拿 `GNodeRect` 量节点得到的是「**父件中心** ± 命中尺寸/2」：
            //   态一恰好撞对（父中心 = 框中心），态二就差了 `交集.W/2`（实测 `+34.09px`，而那正是新视口宽的一半）。
            //   ⛔ **别去改实现**（把节点搬到矩形中心）—— `Editor/RewardsScene.cs` 的 A218 用
            //   `localPosition == 0` 钉着这条约定，改了那条立刻红。
            //   ⚠️ 尺寸用 `q.WorldW/H`（quad 自己的世界尺寸）、中心走 `MenuDraw.PosInDesignSpace`
            //   （**设计帧**；与 `GFit` / `ClipPx` 同帧 —— 本窗根在批处理里 scale = 0.8，世界帧会差一档）。
            bool GHitRect(Transform hitNode, out float x1, out float y1, out float x2, out float y2)
            {
                const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;
                x1 = y1 = x2 = y2 = 0f;
                if (hitNode == null) return false;
                var q = hitNode.GetComponentInChildren<ImageQuad>(true);
                if (q == null) return false;
                Vector2 c = LayoutSpace.ToPixel(MenuDraw.PosInDesignSpace(q.transform));
                float hw = q.WorldW * K * 0.5f, hh = q.WorldH * K * 0.5f;
                x1 = c.x - hw; x2 = c.x + hw; y1 = c.y - hh; y2 = c.y + hh;
                return true;
            }

            // 把某一颗视口节点的**框**改成 `r`（⛔ 它那两个字段一个都不碰），返回原框（收尾还原用）。
            PxRect GFit(ViewportClip vc, PxRect r)
            {
                var keep = vc.ClipPx;
                MenuDraw.ApplyPxRect(vc.transform, vc.transform.parent, r);
                return keep.HasValue ? keep.Value : r;
            }
            void GRestore(ViewportClip vc, PxRect keep)
            { MenuDraw.ApplyPxRect(vc.transform, vc.transform.parent, keep); }

            // 四态（口径逐字照 己段那个 `Vp4` —— **节点态与回落态都要断**）：
            //   ① 生产框 ⇒ 探针**可见**；② **把节点框整体搬走 +4000px** ⇒ 探针**不可见**（只有真读节点才会这样）；
            //   ③ `ClipNode = null`（**回落态**）⇒ 节点搬到哪都不管、探针**又可见**；④ 节点还原 + 放回 ⇒ **可见**。
            void GFour(string who, MenuScroll sc, ViewportClip vc, Transform content, string nodeName)
            {
                if (sc == null || vc == null || content == null)
                {
                    CheckTrue(false, $"（前提·不静默）{who}：`MenuScroll` / 视口节点 / 内容件三者有缺 ⇒ 那四态**没跑**");
                    return;
                }
                var above = ViewportClip.FindAbove(content);
                CheckTrue(above == vc && vc.name == nodeName,
                          $"★ {who}：从内容件「{content.name}」沿父链 `ViewportClip.FindAbove` 找到的**就是**"
                          + $"`ClipNode` 指的那颗 `{nodeName}` 节点 —— 🔴 **独立口**（父链查找 vs 宿主的显式赋值不同源）；"
                          + $"现读 FindAbove = {(above != null ? "「" + above.name + "」" : "null")} · ClipNode = 「{vc.name}」"
                          + "。改坏法：删掉宿主那一行 `ClipNode = …;`（⇒ null）或指到别的节点（⇒ 身份/名字对不上）");
                var keep = vc.ClipPx;
                if (!keep.HasValue)
                {
                    CheckTrue(false, $"（前提·不静默）{who}：那颗节点给不出框（`ClipPx == null`）⇒ 那四态**没跑**");
                    return;
                }
                CheckTrue(NearPx(keep.Value, sc.Viewport, 0.05f),
                          $"★ {who}：节点框与 `MenuScroll.Viewport` 在生产态同值（实测 "
                          + $"{keep.Value.x1:F3},{keep.Value.y1:F3}→{keep.Value.x2:F3},{keep.Value.y2:F3} vs "
                          + $"{sc.Viewport.x1:F3},{sc.Viewport.y1:F3}→{sc.Viewport.x2:F3},{sc.Viewport.y2:F3}）"
                          + " —— 这是下面「搬走 ⇒ 不可见」那条的前提（⛔ 别拿搬走的量当期望值）");
                var vpr = sc.Viewport;
                var probe = new PxRect(vpr.CX - 5f, vpr.CY - 5f, vpr.CX + 5f, vpr.CY + 5f);
                bool s0 = sc.Intersects(probe);
                MenuDraw.ApplyPxRect(vc.transform, vc.transform.parent,
                                     new PxRect(keep.Value.x1 + 4000f, keep.Value.y1 + 4000f,
                                                keep.Value.x2 + 4000f, keep.Value.y2 + 4000f));
                bool s1 = sc.Intersects(probe);
                var saved = sc.ClipNode;
                sc.ClipNode = null;                       // 回落态 = 迁移前的行为（只看 `Viewport` 那个常数）
                bool s2 = sc.Intersects(probe);
                sc.ClipNode = saved;
                MenuDraw.ApplyPxRect(vc.transform, vc.transform.parent, keep.Value);   // 还原
                bool s3 = sc.Intersects(probe);
                CheckTrue(s0 && !s1 && s2 && s3,
                          $"★★★ {who} 四态：生产框 **可见**(s0={s0}) · **节点搬走 ⇒ 不可见**(s1={s1}) · "
                          + $"**回落态**（`ClipNode = null`）⇒ **又可见**(s2={s2}) · 节点还原 ⇒ **可见**(s3={s3})"
                          + "（探针 = 视口正中 10×10px）"
                          + " —— 改坏法：① 把 `Intersects` 写回 `MenuDraw.Visible(onScreen, Viewport)` ⇒ **s1 变 true** ⇒ 红；"
                          + "② 把回落那一支删掉（`ClipNode == null` 就返回 false）⇒ **s2 变 false** ⇒ 红；"
                          + "③ 宿主不喂 `ClipNode` ⇒ 上面那条结构判据先红。⛔ 本段不读任何计数器");
            }

            // ============================================================ ① 榜单军种条（A768① + A769）
            // 判据 = 原版 prefab 的两份原始 JSON（本件现读）：
            //   · `MonoBehaviour_3897231031841831396.json` = 那颗 `RectMask2D`（`pad (0,0,0,0)` · `soft (42,0)` · `en 1`）；
            //   · `GameObject/Viewport_-2793685670958024220.json` 的 `m_Children` 里**就是 `Army Content`**
            //     ⇒ **原版是父子**（`Army Selector/{Separator Line, Viewport/Army Content}`）—— A769 那条结构缺口的判据。
            var lbG = LeaderboardWindow.Create(shell.Windows, LeaderboardKind.Skirmish);
            // 🔴 **2026-10-14（清单 #18 / D2 #7）**：`LeaderboardWindow.Create` **不自开窗**
            //   （`Shell/LeaderboardWindow.cs:210-224` 只 `AttachToAnchor` 就 `return`）⇒ 少了这一句
            //   `Build()` 从不跑、`Army Selector` / `Army Content` / 那颗 `Viewport` 全是 null
            //   ⇒ 本段 **#18–#23 六条**（A769 结构 / 条目吃节点 / 命中区 / A768①）一起红。
            //   对照：本文件 ② 那一处**同一个窗口**是 `Create` **+ `OpenWindow(lb2)`**（`:4421-4423`）⇒ 那边全绿。
            //   ⚠️ 判「实现侧有没有错」别被这一句盖住：`LeaderboardWindow.cs:488` 的
            //   `_armyContent = Node(vpVc.transform, "Army Content", …)` 与 `:498` 的 `_armyScroll.ClipNode = vpVc;`
            //   **本来就在**（D2 #10 明说），只是从没执行过。
            shell.Windows.OpenWindow(lbG);
            var armSelG = FindChildIn(lbG.transform, "Army Selector");
            var armContentG = armSelG != null ? FindChildIn(armSelG, "Army Content") : null;
            // 框 = 原版字面量（`Army Selector` 与它的子件 `Viewport` **同矩形**：249.0,147.6→1671.0,258.6）
            // ⛔ 不从 `LeaderboardWindow.ArmySelR` 读（那是被测实现传进去的实参 = 自证）。
            var armFrameG = new PxRect(248.99f, 147.64f, 1671.01f, 258.59f);
            var armVcG = GNode(lbG.transform, armFrameG);
            CheckTrue(armSelG != null && armContentG != null && armVcG != null,
                      "（前提·不静默）榜单军种条三件都在：`Army Selector` / `Army Content` / 那颗 `Viewport`"
                      + "（按框 248.99,147.64→1671.01,258.59 认）—— ⛔ 缺任一件下面那几条**不静默跳过**");
            CheckTrue(armVcG == null || (armVcG.padding == Vector4.zero && armVcG.softness == new Vector2Int(42, 0)),
                      "★ A768①：`Army Selector/Viewport` 上那颗组件的两个字段 = 原版 `RectMask2D` 的实读"
                      + " `m_Padding (0,0,0,0)` · `m_Softness (42,0)`"
                      + (armVcG != null
                         ? $"（现读 ({armVcG.padding.x},{armVcG.padding.y},{armVcG.padding.z},{armVcG.padding.w}) / "
                           + $"({armVcG.softness.x},{armVcG.softness.y})）"
                         : "（⛔ 节点没找到，本条按上一条的红一起看）")
                      + " —— 判据 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_3897231031841831396.json`"
                      + "（同族另两扇榜 `RankedClassic…Variant` / `Draft…` **逐位同值**）"
                      + "；改坏法：改任一格 ⇒ 红");
            CheckTrue(armVcG != null && armContentG != null && armContentG.parent == armVcG.transform,
                      "★★ A769（结构）：`Army Content` 是那颗 `Viewport` 的**子件**"
                      + "（原版 `GameObject/Viewport_-2793685670958024220.json` 的 `m_Children` 里就是它）—— 现读父件 = "
                      + (armContentG != null && armContentG.parent != null ? "「" + armContentG.parent.name + "」" : "（取不到）")
                      + "。改坏法：把它挂回 `Army Selector`（**兄弟**放法）⇒ 这条与下面「条目吃得到节点」那条一起红"
                      + "（兄弟放法下 `ViewportClip.FindAbove(条目)` 沿父链**跳过** `Viewport` ⇒ 静默不裁）");
            CheckTrue(armSelG == null || armSelG.Find("Army Content") == null,
                      "★★ A769（反向 · 判别式）：`Army Content` **不再是 `Army Selector` 的直接子件**"
                      + "（`Transform.Find` 只找直接子件 ⇒ 本条与上一条**不可能被旧结构同时蒙对**）");
            CheckTrue(armVcG != null && armContentG != null && ViewportClip.FindAbove(armContentG) == armVcG,
                      "★ 独立判据：从条目那一层（`Army Content`）沿父链找到的节点 == 宿主喂给 `ClipNode` 的那一颗"
                      + "（⛔ 与「宿主显式赋值」**不同源**）");
            CheckTrue(armVcG != null && lbG.ArmyScroll != null && lbG.ArmyScroll.ClipNode == armVcG,
                      "★ A465 那一行在榜单也补上了：`ArmyScroll.ClipNode` = 这颗节点"
                      + " —— 改坏法：删掉 `BuildArmySelector` 里那一行（或把它挪进 `if (_armyScroll == null)` 那一支、"
                      + "第二次 `Build()` 之后指向已销毁的组件）⇒ 红");
            if (armVcG != null && armContentG != null)
                GFour("榜单军种条（`Army Selector/Viewport`）", lbG.ArmyScroll, armVcG, armContentG, "Viewport");

            // ★ **A434（2026-10-14）：「显式不裁」哨兵** —— 自成一档、不依赖任何窗的状态（造一颗临时视口节点）。
            //   判据：① 传 `null` ⇒ **按父链解析**（父链上有节点 ⇒ 被裁 ⇒ `VisibleAbove` 为假）；
            //   ② 传 `MenuDraw.NoClip` ⇒ **明确的否定** ⇒ 照样可见（哪怕框整个在视口外）；
            //   ③ 计数 `ViewportClip.OptOuts` 要 **+1**（⛔ 只断 ② 不够 —— 今天大多数站点本来就不裁，
            //      那种断言「改坏实现不会红」；这条计数与 `NodeResolutions` 同一个用法）。
            {
                int opt0 = ViewportClip.OptOuts;
                var host434 = new GameObject("A434 探针宿主", typeof(RectTransform));
                var vp434 = ViewportClip.Hang(host434.transform, "Viewport",
                                              new PxRect(100f, 100f, 200f, 200f), Vector4.zero, Vector2Int.zero);
                var in434 = new GameObject("child", typeof(RectTransform)).transform;
                in434.SetParent(vp434.transform, false);
                var out434 = new PxRect(1000f, 1000f, 1100f, 1100f);      // 整个在视口外
                bool auto434 = MenuDraw.VisibleAbove(in434, out434, null);
                bool oo434 = MenuDraw.VisibleAbove(in434, out434, MenuDraw.NoClip);
                CheckTrue(!auto434 && oo434 && ViewportClip.OptOuts == opt0 + 1,
                          "★ A434：**「显式不裁」哨兵** —— 同一个框、同一个父链：传 `null` ⇒ "
                        + (auto434 ? "可见（**错**：父链上那颗视口应当把它裁掉）" : "被裁（✓ 父链生效）")
                        + $"；传 `MenuDraw.NoClip` ⇒ " + (oo434 ? "**不裁**（✓）" : "**仍被裁**（✗）")
                        + $"；`OptOuts` {opt0} → {ViewportClip.OptOuts}（期望 +1）"
                        + " —— 🔴 改坏法：把 `Resolve` 的 ①′ 支删掉 ⇒ 哨兵落进 ①/② 支 ⇒ 这一条红");
                UnityEngine.Object.DestroyImmediate(host434);
            }

            // ---- 条目级（**这一条才是 A768① + A769 的可见后果**）：军种项那颗 `Hit` ----
            // `MenuDraw.Hit(node, "Hit", r, QHit, …)` **没传 `clip`** ⇒ 它只能沿父链解析。
            // 期望值全是**原版字面量**：
            //   · 按钮 = 136.36 × 121.59、中心 (317.17, 203.115) ⇒ 框 `248.99,142.32→385.35,263.91`
            //     （出处 = `menu_dump.py "Army Item Button"`，与 `Editor/MainMenuScene.cs` 既有的世界坐标断言逐位同）；
            //   · 视口框 = `248.99,147.64→1671.01,258.59` ⇒ 相交之后**上下各被裁掉 ~5.3px**（高 121.59 → 110.95）。
            var a0G = armContentG != null ? FindChildIn(armContentG, CampaignData.Armies[0]) : null;
            var h0G = a0G != null ? FindChildIn(a0G, "Hit") : null;
            float ax1, ay1, ax2, ay2;
            bool okHit = GHitRect(h0G, out ax1, out ay1, out ax2, out ay2);   // `GHitRect` 自带 null 挡（⛔ 别写成 `h0G != null && …`：短路会让这四格**没被赋值**）
            CheckTrue(okHit, "（前提·不静默）第 1 颗军种项的 `Hit` 节点拿得到（军种项 GO 名 = 阵营名）"
                             + " —— ⛔ 取不到就不往下断「量到的值」那几条（不静默变绿）");
            CheckTrue(okHit && NearPx(ax1, 248.99f, 0.5f) && NearPx(ax2, 385.35f, 0.5f)
                             && NearPx(ay1, 147.64f, 0.5f) && NearPx(ay2, 258.59f, 0.5f),
                      "★★★ A768①（条目级 · 态一）：第 1 颗军种项的**命中区** = 按钮框 ∩ 视口框 = "
                      + (okHit ? $"{ax1:F2},{ay1:F2}→{ax2:F2},{ay2:F2}" : "（取不到）")
                      + "，期望 **248.99,147.64→385.35,258.59**（**高 110.95** —— 按钮自己 121.59，上下各被视口裁掉 ~5.3px）"
                      + " —— 判据 = 原版 `RectMask2D` 的**射线那一面**（`IsRaycastLocationValid`：框外的点判不中任何东西）。"
                      + "🔴 **改坏法**：① 把 `ViewportClip.Hang` 改回 `Node(...)`（不挂组件）⇒ 命中区回到整格 121.59 高 ⇒ 红；"
                      + "② 把 `Army Content` 挂回 `Army Selector`（兄弟）⇒ `FindAbove` 找不到这颗节点 ⇒ 同样红（**这一条正是 A769**）");
            if (okHit && armVcG != null && armContentG != null && lbG.ArmyScroll != null)
            {
                // 态二：把**节点框的右沿**切到第 0 颗的中心（317.17）⇒ 走生产那条重建路 ⇒ 命中区右沿跟着切
                var keepArm = GFit(armVcG, new PxRect(248.99f, 147.64f, 317.17f, 258.59f));
                if (lbG.ArmyScroll.OnChanged != null) lbG.ArmyScroll.OnChanged();   // = `RebuildArmyButtons`
                var a0c = FindChildIn(armContentG, CampaignData.Armies[0]);
                var h0c = a0c != null ? FindChildIn(a0c, "Hit") : null;
                float bx1, by1, bx2, by2;
                bool okCut = GHitRect(h0c, out bx1, out by1, out bx2, out by2);
                CheckTrue(okCut && NearPx(bx1, 248.99f, 0.5f) && NearPx(bx2, 317.17f, 0.5f)
                                 && NearPx(by1, 147.64f, 0.5f) && NearPx(by2, 258.59f, 0.5f),
                          "★★★ A768①（条目级 · 态二）：把**节点框**右沿切到第 0 颗的中心（317.17）后重建 ⇒ 命中区 = "
                          + (okCut ? $"{bx1:F2},{by1:F2}→{bx2:F2},{by2:F2}" : "（取不到）")
                          + "，期望 **248.99,147.64→317.17,258.59**（宽 68.18 = 半格，**左沿与两条 y 边一个像素都不动**）"
                          + " —— 与态一成对：**只有真读节点才会动右沿**（⛔ 只断「态一那个数」是弱断言，分不出两种状态）");
                // 态三：还原节点框 + 重建
                GRestore(armVcG, keepArm);
                if (lbG.ArmyScroll.OnChanged != null) lbG.ArmyScroll.OnChanged();
                var a0r = FindChildIn(armContentG, CampaignData.Armies[0]);
                var h0r = a0r != null ? FindChildIn(a0r, "Hit") : null;
                float cx1, cy1, cx2, cy2;
                bool okBack = GHitRect(h0r, out cx1, out cy1, out cx2, out cy2);
                CheckTrue(okBack && NearPx(cx1, 248.99f, 0.5f) && NearPx(cx2, 385.35f, 0.5f)
                                 && NearPx(cy1, 147.64f, 0.5f) && NearPx(cy2, 258.59f, 0.5f),
                          "★★ A768①（条目级 · 态三）：节点框**还原 + 重建** ⇒ 命中区又回到整格宽 136.36（"
                          + (okBack ? $"{cx1:F2},{cy1:F2}→{cx2:F2},{cy2:F2}" : "（取不到）")
                          + "）—— 两态都断 ⇒ 态二那个 317.17 **不可能**来自别的原因（例如「那一页压根没建」）");
            }
            lbG.Close();

            // ============================================================ ② 购买高级战役（A768② + A770）
            // 判据 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-8892924525201795015.json`：
            //   `Purchase Premium Window/Scroll View/Viewport` 那颗 `RectMask2D`
            //   = `m_Padding (0,0,0,0)` · `m_Softness (0,0)`（**硬边**）· `m_Enabled 1`。
            // ⚠️ A770 那件事（`RebuildContainers` 的注释写着「部分越界的件把 `clip` 显式喂给
            //   `MenuDraw.Nine/Rect/Hit`」而**一个 `clip` 实参都没传**）就在这一扇：滚到一半的容器
            //   本来是**不裁**画出来的 —— 挂上节点之后由父链解析接管（本段盯的就是那件事）。
            var ppwG = PurchasePremiumWindow.Create(shell.Windows, new[]
            {
                new PurchasePremiumWindow.ArmyOffer { Army = 10, ArmyName = "Ultramarines", Purchased = false,
                                                      PriceText = "300,00", PremiumUnlocked = true },
                new PurchasePremiumWindow.ArmyOffer { Army = 20, ArmyName = "Goff", Purchased = true,
                                                      PriceText = "300,00", PremiumUnlocked = false },
            });
            var ppwSvG = FindChildIn(ppwG.transform, "Scroll View");
            var ppwVpG = ppwSvG != null ? FindChildIn(ppwSvG, "Viewport") : null;
            var ppwVcG = ppwVpG != null ? ppwVpG.GetComponent<ViewportClip>() : null;
            CheckTrue(ppwVpG != null && ppwVcG != null,
                      "★ A768②：`Purchase Premium Window/Scroll View/Viewport` 那颗节点上挂了 `ViewportClip`"
                      + "（改前它是 `MenuDraw.Node` 建的**裸节点** ⇒ 这一棵子树里所有 `clip == null` 的件都不裁）"
                      + " —— 改坏法：把它改回 `MenuDraw.Node(...)` ⇒ 红");
            CheckTrue(ppwVcG == null || (ppwVcG.padding == Vector4.zero && ppwVcG.softness == Vector2Int.zero),
                      "★ A768②：两个字段 = 原版那颗 `RectMask2D` 的实读 `m_Padding (0,0,0,0)` · `m_Softness (0,0)`"
                      + (ppwVcG != null
                         ? $"（现读 ({ppwVcG.padding.x},{ppwVcG.padding.y},{ppwVcG.padding.z},{ppwVcG.padding.w}) / "
                           + $"({ppwVcG.softness.x},{ppwVcG.softness.y})）"
                         : "（⛔ 组件没挂上，本条按上一条的红一起看）")
                      + " —— 判据 = `MonoBehaviour_-8892924525201795015.json`（同一颗 MB 的 `m_GameObject` 按 `m_Father`"
                      + " 父链上行 = `Purchase Premium Window/Scroll View/Viewport`，本件现走一遍）；改坏法：改任一格 ⇒ 红");
            CheckTrue(ppwVcG != null && ppwG.Scroll != null && ppwG.Scroll.ClipNode == ppwVcG,
                      "★ A465 那一行在这一扇补上了：`Scroll.ClipNode` = 这颗节点"
                      + " —— 改坏法：删掉 `Build()` 第 7 步那一行 ⇒ 红");
            if (ppwVcG != null && ppwG.ContentNode != null)
                GFour("高级版窗（`Purchase Premium Window/Scroll View/Viewport`）",
                      ppwG.Scroll, ppwVcG, ppwG.ContentNode, "Viewport");

            // ---- 条目级（A770 的可见后果）：滚到一半的那个 `Army Container` 的命中区 ----
            // 🔴 这一条**故意不写死绝对坐标**：本窗的容器节点与它的子件**不在同一档坐标**里
            //    （`RebuildContainers` 传的是 `ContainerRect(i)` 那一份、而 `BuildContainer` 的子件走 `Abs(…)`，
            //     两者恰好差一个根原点 `167.175,70.94`）—— 那是**另一个账**（见报告 §七），本件只报不改。
            //    ⇒ 这里断的是**相对关系**：把节点框的下沿切到命中区中高 ⇒ **只该动 y2 那一条边**。
            var c0G = ppwG.Containers.Count > 0 ? ppwG.Containers[0] : null;
            var ch0G = c0G != null ? FindChildIn(c0G, "Hit") : null;
            float px1, py1, px2, py2;
            bool okCH = GHitRect(ch0G, out px1, out py1, out px2, out py2);
            CheckTrue(okCH && (py2 - py1) > 8f && (px2 - px1) > 8f,
                      "（前提 · 不静默）第 1 个 `Army Container` 的命中区量得到、且不是退化矩形"
                      + (okCH ? $"（现读 {px1:F2},{py1:F2}→{px2:F2},{py2:F2}）" : "（取不到 —— 下面两条不跑）"));
            if (okCH && ppwVcG != null && ppwG.Scroll != null && ppwG.ContentNode != null && ppwVcG.ClipPx.HasValue)
            {
                float cutY = (py1 + py2) * 0.5f;
                var frP = ppwVcG.ClipPx.Value;
                var keepP = GFit(ppwVcG, new PxRect(frP.x1, frP.y1, frP.x2, cutY));   // 只切**下沿**
                if (ppwG.Scroll.OnChanged != null) ppwG.Scroll.OnChanged();           // = `RebuildContainers`
                var c0c = ppwG.Containers.Count > 0 ? ppwG.Containers[0] : null;
                var ch0c = c0c != null ? FindChildIn(c0c, "Hit") : null;
                float qx1, qy1, qx2, qy2;
                bool okCut2 = GHitRect(ch0c, out qx1, out qy1, out qx2, out qy2);
                CheckTrue(okCut2 && NearPx(qx1, px1, 0.5f) && NearPx(qx2, px2, 0.5f)
                                 && NearPx(qy1, py1, 0.5f) && NearPx(qy2, cutY, 0.5f),
                          "★★★ A770（态二）：把**视口节点框的下沿**切到容器命中区的中高（"
                          + $"{cutY:F2}）后走生产重建路 ⇒ 命中区 = "
                          + (okCut2 ? $"{qx1:F2},{qy1:F2}→{qx2:F2},{qy2:F2}" : "（取不到）")
                          + $"；期望 **{px1:F2},{py1:F2}→{px2:F2},{cutY:F2}**"
                          + " —— 只有 `y2` 那一条边跟着节点框走、另外三条边**一个像素都不动**（容器整块还在视口右侧）。"
                          + "🔴 **改坏法**：把 `ViewportClip.Hang` 改回 `MenuDraw.Node(...)`（不挂组件）"
                          + " ⇒ `MenuDraw.Hit` 拿不到裁切状态（`clip == null` ⇒ `ClipRect` 首句原样放行）"
                          + " ⇒ **y2 不动**（还是态一那个值）⇒ 红 —— 那正是 A770 记的「滚到一半的容器不裁画出来」");
                // 态三：还原节点框 + 重建
                GRestore(ppwVcG, keepP);
                if (ppwG.Scroll.OnChanged != null) ppwG.Scroll.OnChanged();
                var c0r = ppwG.Containers.Count > 0 ? ppwG.Containers[0] : null;
                var ch0r = c0r != null ? FindChildIn(c0r, "Hit") : null;
                float rx1, ry1, rx2, ry2;
                bool okBack2 = GHitRect(ch0r, out rx1, out ry1, out rx2, out ry2);
                CheckTrue(okBack2 && NearPx(rx1, px1, 0.5f) && NearPx(rx2, px2, 0.5f)
                                 && NearPx(ry1, py1, 0.5f) && NearPx(ry2, py2, 0.5f),
                          "★★ A770（态三）：节点框**还原 + 重建** ⇒ 命中区四边全部回到态一那份（"
                          + (okBack2 ? $"{rx1:F2},{ry1:F2}→{rx2:F2},{ry2:F2}" : "（取不到）")
                          + "）—— 两态都断 ⇒ 态二那条「只有 y2 动了」不可能来自别的原因");
            }
            ppwG.Close();

            // ============================================================ ③ 排位奖励活动窗（A768③）
            // 判据 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_4030138604520103632.json`：
            //   `Ranked Boost Reward Event Window/window/Scroll View/Viewport` 那颗 `RectMask2D`
            //   = `m_Padding (0,0,0,0)` · `m_Softness (55,0)` · `m_Enabled 1`（左右两条软边）。
            var rreG = RankedRewardEventWindow.Create(shell.Windows, new RankedRewardEventWindow.BoostView
            {
                Armies = new[] { 10, 20 },
                ArmyNames = new[] { "Ultramarines", "Goff" },
                Label0 = "FEATURE FACTIONS",
                Label1 = "for every Ranked victory gained with a featured faction.",
                Label2 = "+{0} Classic points",
                PointsBonus = 20,
                DurationHours = 24,
                EndTime = Time.realtimeSinceStartup + 3600f * 23f + 60f * 34f,
            });
            var rreSvG = FindChildIn(rreG.transform, "Scroll View");
            var rreVpG = rreSvG != null ? FindChildIn(rreSvG, "Viewport") : null;
            var rreVcG = rreVpG != null ? rreVpG.GetComponent<ViewportClip>() : null;
            CheckTrue(rreVpG != null && rreVcG != null,
                      "★ A768③：`Ranked Boost Reward Event Window/window/Scroll View/Viewport` 那颗节点上挂了 `ViewportClip`"
                      + "（改前是裸节点 ⇒ 那张阵营卡上所有 `clip == null` 的层都不裁、**也没有那条 (55,0) 渐隐带**）"
                      + " —— 改坏法：改回 `MenuDraw.Node(...)` ⇒ 红");
            CheckTrue(rreVcG == null || (rreVcG.padding == Vector4.zero && rreVcG.softness == new Vector2Int(55, 0)),
                      "★ A768③：两个字段 = 原版那颗 `RectMask2D` 的实读 `m_Padding (0,0,0,0)` · `m_Softness (55,0)`"
                      + (rreVcG != null
                         ? $"（现读 ({rreVcG.padding.x},{rreVcG.padding.y},{rreVcG.padding.z},{rreVcG.padding.w}) / "
                           + $"({rreVcG.softness.x},{rreVcG.softness.y})）"
                         : "（⛔ 组件没挂上，本条按上一条的红一起看）")
                      + " —— 判据 = `MonoBehaviour_4030138604520103632.json`（同一颗 MB 的 `m_GameObject` 按 `m_Father`"
                      + " 父链上行 = `…/window/Scroll View/Viewport`，本件现走一遍）；改坏法：把 `55` 改成别的数、"
                      + "或写成 `(0,55)`（**两轴写反** —— 横向滚动的那条渐隐带立刻跑到上下两条边上）⇒ 红");
            CheckTrue(rreVcG != null && rreG.Scroll != null && rreG.Scroll.ClipNode == rreVcG,
                      "★ A465 那一行在这一扇补上了：`Scroll.ClipNode` = 这颗节点"
                      + " —— 改坏法：删掉 `Build()` 第 9 步那一行 ⇒ 红");
            if (rreVcG != null && rreG.ContentNode != null)
                GFour("排位奖励活动窗（`Ranked Boost Reward Event Window/window/Scroll View/Viewport`）",
                      rreG.Scroll, rreVcG, rreG.ContentNode, "Viewport");

            // ---- 条目级：第 1 张阵营卡**画出来**的并集（卡上那几层都是 `clip == null` 的）----
            // 🔴 这一条也**故意不写死绝对坐标**：只断「切一半 ⇒ 右沿跟着走、左沿不动」这个相对关系。
            Transform CardOf(Transform content)
            { return content != null && content.childCount > 0 ? content.GetChild(0) : null; }
            float ux1, uy1, ux2, uy2;
            bool okU = GUnion(CardOf(rreG.ContentNode), out ux1, out uy1, out ux2, out uy2);   // 同 `GNodeRect`：自带 null 挡
            CheckTrue(okU && (ux2 - ux1) > 40f,
                      "（前提 · 不静默）第 1 张阵营卡**画出来的并集**拿得到、且宽 > 40px"
                      + (okU ? $"（现读 {ux1:F2},{uy1:F2}→{ux2:F2},{uy2:F2}）" : "（取不到 —— 下面两条不跑）")
                      + " —— ⛔ 这条前提红了 = 本段这两条没有鉴别力（会被看见，不会静默变绿）");
            if (okU && rreVcG != null && rreG.Scroll != null && rreVcG.ClipPx.HasValue)
            {
                float cutX = (ux1 + ux2) * 0.5f;
                var frR = rreVcG.ClipPx.Value;
                var keepR = GFit(rreVcG, new PxRect(frR.x1, frR.y1, cutX, frR.y2));    // 只切**右沿**
                if (rreG.Scroll.OnChanged != null) rreG.Scroll.OnChanged();           // = `RebuildCards`
                float vx1, vy1, vx2, vy2;
                bool okCut3 = GUnion(CardOf(rreG.ContentNode), out vx1, out vy1, out vx2, out vy2);
                CheckTrue(okCut3 && NearPx(vx1, ux1, 0.6f) && NearPx(vx2, cutX, 0.6f),
                          "★★★ A768③（态二）：把**视口节点框的右沿**切到那张卡画出来那条并集的**正中**（"
                          + $"{cutX:F2}）后走生产重建路 ⇒ 并集 = "
                          + (okCut3 ? $"{vx1:F2},{vy1:F2}→{vx2:F2},{vy2:F2}" : "（取不到）")
                          + $"；期望 **左沿仍 {ux1:F2}、右沿 = {cutX:F2}**"
                          + " —— 🔴 **改坏法**：把 `ViewportClip.Hang` 改回 `MenuDraw.Node(...)`（不挂组件）"
                          + " ⇒ `MenuDraw.Nine/Rect` 拿不到裁切状态（`clip == null` ⇒ 不裁）⇒ 右沿回到态一那个值 ⇒ 红");
                GRestore(rreVcG, keepR);
                if (rreG.Scroll.OnChanged != null) rreG.Scroll.OnChanged();
                float wx1, wy1, wx2, wy2;
                bool okBack3 = GUnion(CardOf(rreG.ContentNode), out wx1, out wy1, out wx2, out wy2);
                CheckTrue(okBack3 && NearPx(wx1, ux1, 0.6f) && NearPx(wx2, ux2, 0.6f),
                          "★★ A768③（态三）：节点框**还原 + 重建** ⇒ 并集**又长回**态一那份（"
                          + (okBack3 ? $"{wx1:F2},{wy1:F2}→{wx2:F2},{wy2:F2}" : "（取不到）")
                          + "）—— 两态都断 ⇒ 态二那条「右沿被切」不可能来自别的原因");
            }
            rreG.Close();

            // ---- 收尾（同戊段 / 己段那条纪律：只 `Close()`、⛔ 不 `DestroyImmediate` —— 下面 `shell.Dump()` 要遍历窗表）----
            shell.Windows.CloseAllWindows();
        }

        Debug.Log(P + shell.Dump());
        Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
        // 🔴 **2026-10-11（A350 · 调度台裁定）**：这一串是**失败表的【重列】**（每条失败在
        //   `Check` 里**已经打过一次**）⇒ 行首标记必须是 `失败重列：`、⛔ **不能再是 `✗`** ——
        //   原来是 `✗` 时日志里 `✗` 行数 = 失败数 **×2**，连「按行首标记数」都数不准
        //   （`✗` 还会出现在断言文案里，见 `资料/已知的坑.md` 那条「别用 `grep -c ✗` 数失败」）。
        //   同族四处一起改：`CollectionScene` / `RewardsScene` / `ShopScene`（同一句形状）。
        if (_fail > 0) foreach (var f in _failures) Debug.LogError(P + "   失败重列：" + f);
        EditorApplication.Exit(_fail > 0 ? 1 : 0);
    }

    // ============================================================ 真 Play（验「壳 → 主菜单」这条链）
    //
    // ⚠️ **不能带 `-quit`** —— 带了会立刻退出、什么都跑不到（`VideoProbe.Play` 的注释里写过同一条）。
    // 用法： … -batchmode -executeMethod ShellScene.Play -logFile -
    //
    // 为什么不能只在编辑模式下验：**`SceneManager.LoadScene` 在编辑模式下不允许**
    // （编辑模式要用 `EditorSceneManager`）⇒ 「壳播完开场 → 载主菜单」这条链**只能进 Play 才跑得到**。

    public static void Play()
    {
        Directory.CreateDirectory(ShotDir);
        Build(out var root);
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        AddToBuildSettings(ScenePath);
        AddToBuildSettings("Assets/CardPresentation/Scenes/MainMenu.unity");

        Debug.Log(P + "=== 外壳「真 Play」链验证 开始（开场 → 载主菜单）===");
        // 🔴 两件事都是被「进 Play 会域重载」逼出来的：
        //   ① 收尾在**运行时侧**（`EditorApplication.update` 在批处理+Play 下不触发）
        //   ② 开关用 `SessionState`（**普通静态字段会被域重载清空**）
        SessionState.SetBool(ShellRuntime.ChainCheckKey, true);
        try { EditorApplication.EnterPlaymode(); }
        catch (System.Exception e)
        {
            Debug.LogError(P + "✗ 批处理里进不了 Play 模式：" + e.Message);
            EditorApplication.Exit(1);
        }
    }
    static void AddToBuildSettings(string path)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == path) return;
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log(P + $"  已加进 Build Settings：{Path.GetFileName(path)}");
    }

    // ============================================================ 存场景

    /// <summary>建出场景存盘，给人打开按 Play 用。</summary>
    public static void BuildAndSaveScene()
    {
        Directory.CreateDirectory(ShotDir);
        var shell = Build(out var root);

        // 🔴 场景里**必须挂着 `ShellRuntime`** —— 界面是它 `Start()` 建的（编辑器里 `Build` 只是直调）。
        //    忘了挂组件 = 按 Play 出来一片空，而自检看不见（它直调 `Build`，与 Play 不是同一个入口）—— 本项目踩过同形的坑。
        if (root == null || root.GetComponent<ShellRuntime>() == null)
        {
            Debug.LogError(P + "✗ 场景里没挂 `ShellRuntime` —— 按 Play 会是一片空");
            EditorApplication.Exit(1);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        Debug.Log(P + $"  场景 {ScenePath}");
        // ⚠️ 同上：这一张拍在 `Build` 之后、开场刚起 —— 画面上确实什么都没有
        Shoot("00_外壳.png", allowBlank: true);
        Debug.Log(P + shell.Dump());
    }
}
