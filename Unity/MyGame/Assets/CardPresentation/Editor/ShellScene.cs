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

    /// <summary>🆕 2026-10-18（第四会话）：断言计数器 + 输出口径**收口到共用件 `Editor/MenuCheck.cs`**
    /// （唯一实现处；本文件只剩同名的一行转发 ⇒ 5,534 个调用点一个字没动）。
    /// 🔴 **逐宿主一份 `CheckSink`**（⛔ 不是全局 static）—— 「拿别处的 `Check` 去断，失败会
    /// **记进别人的合计**里 ⇒ 静默」，判据见 `Editor/RewardWindowFixture.cs`。</summary>
    static readonly CheckSink _sink = new CheckSink(P)
    {
        Near = MenuNearStyle.Compact3,             // 本文件原来是 `（{got:F3} ≈ {want:F3}±{tol:F3}）`
        BlankNote = MenuBlankNote.EmphThisOne,     // 本文件原来那句「（**这一张按已知情况放行**）」
    };

    static void Section(string t) => MenuCheck.Section(_sink, t);

    static void Check<T>(T got, T want, string msg) => MenuCheck.Check(_sink, got, want, msg);

    static void CheckTrue(bool c, string msg) => MenuCheck.True(_sink, c, msg);

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
    static void CheckHoverSwap(Transform root, string what) => MenuCheck.HoverSwap(_sink, root, what);

    static void CheckNoMissingSwapArt(string what) => MenuCheck.NoMissingSwapArt(_sink, what);
    static void CheckNear(float got, float want, float tol, string msg) => MenuCheck.Near(_sink, got, want, tol, msg);

    /// <summary>🆕 **2026-10-17（`A1212` 块 10/12/13）**：一颗字的「四格 + 折行档 + 框」。
    /// <para>判据 = 原版 MB 原文（`m_enableAutoSizing` / `m_fontSizeMin/Max/Base` / `m_TextWrappingMode`），
    /// 期望值写字面量、逐颗现读；⛔ 不读被测类里的常量（自证）。</para>
    /// <para>`root` 用来把同名件（`Button Text` 全仓 447 份）限到**那一颗所在子树**。</para></summary>
    static void CheckA1212(Transform root, string name, string origNode,
                           float mnPx, float mxPx, float basePx, int wrap,
                           float boxW = -1f, float boxH = -1f)
    {
        var t = root != null ? FindChildIn(root, name) : null;
        var lb = t != null ? t.GetComponent<Label>() : null;
        CheckTrue(lb != null, $"（前提）`A1212` `{name}` 建出来了（原版 `{origNode}`）");
        if (lb == null) return;
        string tag = $"`{name}`（原版 `{origNode}`）";
        CheckTrue(lb.AutoSizing, $"★ A1212 {tag}：**`m_enableAutoSizing = 1`**（自适应开着）");
        CheckNear(lb.FontSizeToPxNow(lb.FontSizeMin, lb.ActualFace), mnPx, 0.05f,
                  $"★ A1212 {tag}：下界 = 原版 `m_fontSizeMin` {mnPx}px");
        CheckNear(lb.FontSizeToPxNow(lb.FontSizeMax, lb.ActualFace), mxPx, 0.05f,
                  $"★ A1212 {tag}：上界 = 原版 `m_fontSizeMax` {mxPx}px");
        CheckNear(lb.FontSizeToPxNow(lb.FontSizeBase, lb.ActualFace), basePx, 0.05f,
                  $"★ A1212 {tag}：二分起点 = 原版 `m_fontSizeBase` {basePx}px"
                + "（🧨 只开自适应、不给 base ⇒ 这一格红）");
        Check(lb.WrappingMode, wrap,
              $"★ A1212 {tag}：折行档 = 原版 `m_TextWrappingMode` {wrap}"
            + "（`SetAutoFitBox` 内部会无条件开 `Normal(1)` ⇒ 这一格抓「接完忘了还原」）");
        var tmp = t.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (tmp == null) { CheckTrue(false, $"（前提）A1212 {tag}：走 TMP 后端"); return; }
        float k = lb.IsUgui ? 1f : (LayoutSpace.DesignPxH / LayoutSpace.DesignHeight);
        if (boxW > 0f)
            CheckNear(Mathf.Abs(tmp.rectTransform.sizeDelta.x) * k, boxW, 0.05f,
                      $"★ A1212/A1226 {tag}：框宽 = 原版有效框 {boxW}px");
        if (boxH > 0f)
            CheckNear(Mathf.Abs(tmp.rectTransform.sizeDelta.y) * k, boxH, 0.05f,
                      $"★ A1212/A1226 {tag}：框高 = {boxH}px");
    }

    /// <summary>🆕 **2026-10-13（A497）**：比**一个经「px → 设计世界 → px」往返反推回来的** px 值时用的谓词
    /// —— 逐字段容差 **0.05px**。
    ///
    /// <para>🔴 **为什么不能直接用 `MenuDraw.SameRect`**：它是**四个字段逐位 `==`**（`Shell/MenuDraw.cs`），
    /// 而 `ViewportClip.ClipPx`（`Shell/ViewportClip.cs` 的 `MenuDraw.QuadRectPx`）与 `MenuDraw.QuadRectPx` 这类值**不是存下来的**
    /// —— 它们是「`ApplyPxRect` 写进去的那个 rect」经 `PosInDesignSpace` + `LayoutSpace.ToPixel` **反推**回来的
    /// ⇒ 写进去与读回来是两条路，中间隔着一次 float32 往返（残差 ~1e-4 px 量级，A497 实测 **−6.1e-5 px**）
    /// ⇒ 逐位比**恒不等**（假阴）。0.05px 对画面无意义，但足以把往返误差挡在外面
    /// （与 `MenuDraw.SameRectNear` 同一口径；判据 → `资料/普查产出_1012/D8_ShellScene五红诊断.md` §1.2）。</para>
    ///
    /// <para>⚠️ **`MenuDraw.SameRectNear` 是私有**（`MenuDraw.cs` 的 `SameRectNear`，无访问修饰符）且它服务的
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

    /// <summary>一棵软边树里有几块的矩形**越出**了裁切框（逐块走 `MenuDraw.QuadRectPx`，别再乘 108）。
    /// 🆕 **2026-10-18（A1003）**：本函数原来自己写了一遍**甲式**（裸 `ToPixel(q.transform.position)`）
    /// ⇒ 已收口到 `MenuDraw.QuadRectPx`（**乙式**：先除回父级 `lossyScale`）。
    /// ⚠️ **`k == 1`（小屏缩放开关出厂关）时两式逐位相同** ⇒ 出厂态读数零变化；带缩放的那一档以收口后为准。</summary>
    static int PiecesOutsideClip(GameObject root, PxRect clip, float tolPx)
    {
        int n = 0;
        foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
        {
            if (q == null || !q.gameObject.activeInHierarchy) continue;
            if (!MenuDraw.QuadRectPx(q, out float x1, out float y1, out float x2, out float y2)) continue;
            if (x1 < clip.x1 - tolPx || x2 > clip.x2 + tolPx
                || y1 < clip.y1 - tolPx || y2 > clip.y2 + tolPx) n++;
        }
        return n;
    }

    /// <summary>🆕 **2026-10-07（A140②）**：一个 `ImageQuad` **渲出来**的像素矩形。
    /// 🆕 **2026-10-18（A1003）**：**收口到 `MenuDraw.QuadRectPx`**（保留本名与本形参 ⇒ 调用点 0 改动）——
    /// 本函数原来自己写的是**甲式**（`LayoutSpace.ToPixel(q.transform.position)`），
    /// 而 `GHitRect` 那一支同年已改**乙式** ⇒ 本文件当时**自己分家**（那条「两者同一口径」的注释只对前两支成立）。
    /// 现在三支（本函数 · `PiecesOutsideClip` · `GHitRect`）**逐字同一口径**。
    /// ⚠️ 只量**这一颗**（不往子树钻）：软边切出来的子块要逐块量。
    /// ⚠️ 它量的是**建完那一刻**的几何 ⇒ 量软边宿主时必须拿「所有块的并集」（`PiecesOutsideClip` 那种扫法）。</summary>
    static bool QuadPxRect(ImageQuad q, out float x1, out float y1, out float x2, out float y2)
        => MenuDraw.QuadRectPx(q, out x1, out y1, out x2, out y2);

    /// <summary>🆕 A233 探针用：一段文字**渲出来**的宽（画布 px）。
    /// 🔴 量的是 **TMP 自己那份渲染网格的顶点跨度**（`textInfo.meshInfo[].vertices` —— 就是
    /// `MenuDraw.ClipTmpMesh` 写、`UpdateVertexData` 推给渲染的那一份；量法与 `MainMenuScene` 的
    /// `CountSoftFadedTextVerts` 同源），⛔ **不是** `Label.WorldW`（那是标签自己的框 ——
    /// 字号对而溢出时它照样「看着对」，见 `AutoFitBox` 那条教训）。
    /// 世界 → 画布 px 走 `LayoutSpace.ToPixel`（别再乘 108）。取不到网格 ⇒ 返回 **−1**
    /// （调用方必须先断它 > 0，否则「≤ 框宽」会被 −1 蒙过去）。
    /// 🆕 **2026-10-16（「文字量法三份」收口）**：这条算法**唯一一份实现在 <see cref="TmpSpanPx"/>**
    /// （本条 = 它的「只取宽」形），本函数**只剩转调** ⇒ ⛔ 别在这里加算法（同族再来一份就又要对账）。
    /// ⚠️ 返回 `−1` 的各档**逐位不变**：取不到网格 / 一个 `isVisible` 的顶点都没有 ⇒ `TmpSpanPx` 回 `false`
    /// 落第一句；`max == min`（零宽）⇒ 落第二句 —— 与旧体那两种写法逐值相同。</summary>
    static float TextMeshWidthPx(Label lb)
    {
        if (!TmpSpanPx(lb, out float minX, out _, out float maxX, out _)) return -1f;
        return maxX > minX ? maxX - minX : -1f;
    }

    /// <summary>🆕 **2026-10-13（A464 · B2/B3 探针）**：一段 TMP 文字**渲出来的**顶点范围
    /// （画布 px、左上原点）。与 <see cref="TextMeshWidthPx"/> **逐字同一套量法**
    /// （同一份 `characterInfo` 过滤 + 同一个 `LayoutSpace.ToPixel(TransformPoint(v))` 换算），
    /// 只是**四条边都给**：B2/B3 要的是「顶点有没有被夹到某个 x 上」，只给宽度分不出来。
    /// 🔴 读的仍是 `textInfo.meshInfo[mi].vertices`（= `MenuDraw.ClipTmpMesh` 写、`UpdateVertexData`
    /// 推给渲染的那一份数组）—— ⛔ 不是 `Label.WorldW`（那玩意儿**不随裁切变**，见 `TmpVertPx` 的注释）。
    /// ⚠️ 只认 `isVisible` 的字（TMP 给不可见的字写四角全 0、却照占 4 个槽 ⇒ 扫全数组会假红）。
    /// 返回 `false` = 取不到网格（⛔ 调用方别把 false 当成「范围是 0」）。
    /// <para>🔴 **2026-10-16（「文字量法三份」收口）**：本函数 = **全仓这一条量法唯一一份实现**
    /// （`internal static` 先例 = A512 把 `MainMenuScene.CloseModalPopups` 提到 `internal`）。收掉的是
    /// `Editor/MainMenuScene.cs` 里**两份逐字同算法的副本**：A781 那一节的局部 `SpanOf` 与 A822 那一节的
    /// 局部 `A822SpanX`（**按锚点认、别抄行号** —— 那条账是 A338：`bool SpanOf(Label lb, out float mnX, out float mxX)`
    /// / `bool A822SpanX(Label lb, out float mnX, out float mxX)`）—— 两者现在都只剩转调（⛔ 别让第三份长回来）。
    /// ⚠️ **三份本来就是同一套算法** ⇒ 收口**不改读数**；逐字唯一一处差别是 `A822SpanX` 取组件带
    /// `(true)`（含 inactive），已**保留**成下面那个可选形参（它显式传 `true`，另两处走默认 `false`）。</para>
    /// <para>⚠️ `includeInactive`（默认 `false` = 与 2026-10-13 起的 13 个调用点逐位相同）：`true` 时
    /// TMP 子件**关着也能取到**（取到的自然是**上一次生成的那份网格** —— 那是 `A822SpanX` 换口前的行为，
    /// 收口时原样保留；⚠️ 那种档位下读到的是**陈网格**，当判据前先确认节点是活的）。</para>
    /// <para>🆕 **2026-10-16（A844 · 跨文件那一半）**：这一族今天**收口到本文件**——
    /// `Editor/DeckScene.cs` 的 `TextMeshRectPx` 与本函数的**同名同契约**（逐句同算法，只差它一直传
    /// `includeInactive: true`）、`Editor/CollectionScene.cs` 的 `TextExtentPx`（扫子树 + 数顶点）
    /// 都已退化成**转调**。**有意不收**的同族与各自原因 = <see cref="SpanOfTmp"/> 的 doc（⛔ 别硬并）。</para>
    /// <para>🔴 **2026-10-18（A990）现核：这条量法只在 16:9 自洽。** 内层走的是 `LayoutSpace.ToPixel`，
    /// 而它的 **x** 在非 16:9 下**不是** `LayoutSpace.FromPixel` 的逆（差 `VisibleWidth / DesignWidth` 倍：
    /// 4:3 = **0.75** · 21:9 = **1.3125**），本工程**建件**走的却是 `FromPixel` ⇒ 非 16:9 下量出来的 px
    /// 与「原版设计 px」**不在同一套坐标里**。
    /// ⚠️ **12 个自检宿主全都把 `cam.aspect` 钉成 `DesignAspect`** ⇒ 既有读数一条都不受影响（= 本尺子在
    /// 今天的全部用法上都对）；⛔ **别在非 16:9 的断言里拿它当判据**。
    /// 算式 / 偏量表 /「同一族还有哪些读口没收口」→ `Shell/MenuDraw.cs` 的 `PixelOfDesign` 与本文件 §⑤·d-5（A990）。</para></summary>
    internal static bool TmpSpanPx(Label lb, out float minX, out float minY, out float maxX, out float maxY,
                                   bool includeInactive = false)
    {
        minX = minY = float.MaxValue; maxX = maxY = float.MinValue;
        int verts = 0;
        SpanOfTmp(lb != null ? lb.GetComponentInChildren<TMPro.TextMeshPro>(includeInactive) : null,
                  ref minX, ref minY, ref maxX, ref maxY, ref verts);
        return verts > 0;          // ⚠️ = 旧体那个 `any`（`any` 就是「k 循环里进过一次」⇒ 逐位等价）
    }

    /// <summary>🆕 **2026-10-16（A844 · 跨文件那一半）**：同上，但**扫一棵子树里的所有 `TMP`**
    /// （`GetComponentsInChildren&lt;TextMeshPro&gt;(true)`），并且**多给一个顶点个数**。
    ///
    /// <para>收掉的是 `Editor/CollectionScene.cs` 的 `TextExtentPx`（按锚点认：那一条量的是
    /// **一张卡整棵子树**上所有字的顶点范围 + 个数，判据在 A250 块「卡上没有一个字的顶点画到视口外」）——
    /// 它的内层循环与上面那个重载**逐句相同**（同一个 `isVisible` 过滤 / 同一个
    /// `LayoutSpace.ToPixel(TransformPoint(v))` 换算 / 同一套越界检查），只多这两件。</para>
    ///
    /// <para>🔴 **读数逐位不变**：两个重载共用下面这一份 <see cref="SpanOfTmp"/>；`min`/`max` 与计数
    /// **都是可交换的**（与遍历次序无关）⇒ 无论按哪种次序并进来，四个边 + `verts` 都与收口前**逐位相同**
    /// （⛔ 所以这不是 A490 说的「量法一变」—— 量法一个字没动，只是**换谁持有这段代码**）。</para>
    ///
    /// <para>⚠️ `includeInactive` **没有**这个重载的形参：`TextExtentPx` 那一档历来就是 `(true)`
    /// （整块关着的卡片也要量 —— 它的判据正是「关着的那张卡上有没有字画到框外」）⇒ 写死 `true`，
    /// ⛔ 别在这里改成可选（改成可选就要多一条「调用方传了哪个」的账）。</para></summary>
    internal static bool TmpSpanPx(Transform root, out float minX, out float minY, out float maxX, out float maxY,
                                   out int verts)
    {
        minX = minY = float.MaxValue; maxX = maxY = float.MinValue; verts = 0;
        if (root == null) return false;
        foreach (var tmp in root.GetComponentsInChildren<TMPro.TextMeshPro>(true))
            SpanOfTmp(tmp, ref minX, ref minY, ref maxX, ref maxY, ref verts);
        return verts > 0;
    }

    /// <summary>🔴 **这一条量法的内层只有这一份**（上面两个重载都走它）—— `isVisible` 过滤 +
    /// `materialReferenceIndex` 取槽 + `LayoutSpace.ToPixel(tmp.transform.TransformPoint(v))` 换算。
    /// ⛔ **别在别处再抄一遍这段循环**：`资料/普查产出_1016/W17_换口与量法收口.md` §④·4 收的就是这一族
    /// （W17 收的是**同一个文件里**那 4 份；本批收的是**跨文件**的 `Editor/CollectionScene.TextExtentPx`
    /// 与 `Editor/DeckScene.TextMeshRectPx`）。
    /// <para>有意**不收**的同族（各自的原因见那几处的注释）：
    /// 🔴 **2026-10-18 就地订正（`A851` 收口 · 铁律 5）**：这一栏原来把 `Editor/RewardsScene.cs` 的
    /// `TmpVertPx` / `TmpVertsAndAlpha` / `TmpGlyphUvW` 的**理由**写成「（写死 `meshInfo[0]`，要的是**逐点序列**）」——
    /// **前半条已作废**：那三处当天已改成**按 `chr[i].materialReferenceIndex` 取槽**（`A851`，报告 `资料/普查产出_1018/S2_A851槽号.md`）。
    /// ⇒ **它们仍然「不收」的理由只剩一条：要的是【逐点序列】而不是包围盒**（本函数的产物是 `min/max/verts`）。
    /// ⚠️ 下个会话别再照旧话把它们判成「写死 0 号槽」那一族。
    /// `Editor/MainMenuScene.cs` 的 `CountSoftFadedTextVerts`（另一种用途：按 y 带数 alpha 剖面）·
    /// `Editor/IconSizeProbe.cs` / `Editor/Round1015Probe.cs` 那两个**离树合成 TMP** 的字体标定探针
    /// （局部单位、不换算到画布 px）。</para></summary>
    static void SpanOfTmp(TMPro.TextMeshPro tmp, ref float minX, ref float minY, ref float maxX, ref float maxY,
                          ref int verts)
    {
        if (tmp == null) return;
        var ti = tmp.textInfo;
        if (ti == null || ti.characterInfo == null || ti.meshInfo == null) return;
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
                var p = LayoutSpace.ToPixel(tmp.transform.TransformPoint(vm[v + k]));   // 甲式保留：顶点量法（W57 §1·4 第 4 条）
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
                verts++;
            }
        }
    }

    /// <summary>🆕 **2026-10-15（A464 · B2b）**：一段 TMP 文字**渲出来的顶点**（画布 px、左上原点）与
    /// **那一份顶点色的 alpha**（0..1），**按同一个序**（逐点对得上）。
    /// 形状照抄 `Editor/RewardsScene.cs` 的 `TmpVertsAndAlpha`（A302 那一条、现读 `CardPresentation/Editor/RewardsScene.cs`）——
    /// 两个宿主**刻意各留一份**（分属不同场景，各自的 `-executeMethod` 是新进程；两份的判据同源，
    /// 逐句都是同一套 `isVisible` 过滤 + `LayoutSpace.ToPixel`）。
    /// <para>🔴 与 `RewardsScene` 那一份的**两处有意差别**（都为了让「位置 ↔ alpha」配得准）：
    /// ① 网格下标取 `ch.materialReferenceIndex`（与同文件的 <see cref="TmpSpanPx"/> 同一个取法）——
    ///    本节的牙口是「**这个角的位置** ↔ **这个角的 alpha**」，两个通道必须来自**同一份** `meshInfo`
    ///    （写死 `meshInfo[0]` 时，只要有一个字落在别的材质槽里，配出来的对就是错的、而且**静默**）；
    /// ② **整字跳过**（四个角里任一越界 ⇒ 这个字四个角一起不要）—— 保住「每字四角、`i % 4` 就是那个角」
    ///    这个步长，<see cref="RowAlphaRange"/> 靠它分「上沿那一行 / 下沿那一行」。
    /// ⚠️ 只认 `isVisible` 的字（理由同 `TmpSpanPx`：TMP 给不可见的字写四角全 0、却照占 4 个槽 ⇒ 扫全数组会假红）。</para>
    /// <returns>顶点个数；**取不到网格（含 `colors32 == null`）回 −1**（⛔ 别回 0 —— 那会与「一个字都没有」撞上）。</returns></summary>
    static int TmpVertsAndAlpha(Label lb, List<Vector2> px, List<float> alpha)
    {
        px.Clear(); alpha.Clear();
        var tmp = lb != null ? lb.GetComponentInChildren<TMPro.TextMeshPro>() : null;
        if (tmp == null || tmp.textInfo == null || tmp.textInfo.meshInfo == null
            || tmp.textInfo.meshInfo.Length == 0 || tmp.textInfo.characterInfo == null) return -1;
        var chr = tmp.textInfo.characterInfo;
        int n = Mathf.Min(tmp.textInfo.characterCount, chr.Length);
        for (int i = 0; i < n; i++)
        {
            var ch = chr[i];
            if (!ch.isVisible) continue;
            int mi = ch.materialReferenceIndex;
            if (mi < 0 || mi >= tmp.textInfo.meshInfo.Length) continue;
            var mesh = tmp.textInfo.meshInfo[mi];
            if (mesh.vertices == null || mesh.colors32 == null) return -1;
            int v = ch.vertexIndex;
            if (v < 0 || v + 3 >= mesh.vertices.Length || v + 3 >= mesh.colors32.Length) continue;   // 🔴 整字跳过
            for (int k = 0; k < 4; k++)
            {
                px.Add(LayoutSpace.ToPixel(tmp.transform.TransformPoint(mesh.vertices[v + k])));   // 甲式保留：顶点量法（W57 §1·4 第 4 条）
                alpha.Add(mesh.colors32[v + k].a / 255f);
            }
        }
        return px.Count;
    }

    /// <summary>🆕 **2026-10-15（A464 · B2b）**：把 <see cref="TmpVertsAndAlpha"/> 那串 alpha 按**顶点落在哪一行**
    /// 聚合 —— `row = 0` = **下沿那一行**（BL·BR）· `row = 1` = **上沿那一行**（TL·TR）。
    /// <para>角序 = TMP 把颜色写进 `MeshInfo.colors32` 的序（`BL·TL·TR·BR`；判据 → `MenuDraw.BaseCornerAlpha`
    /// 的注释那一串，`TMP_Text.cs:5566-5569`）⇒ 第 `i` 个顶点的角就是 `i % 4`（`0=BL · 1=TL · 2=TR · 3=BR`）。
    /// ⚠️ 这个前提成立的条件是**整字写进去**（四个角一个不落）—— `TmpVertsAndAlpha` 已经保证了，这里再显式查一次
    /// （`Count % 4 != 0` ⇒ 回 `false`，⛔ 不静默按错的边界分档）。</para>
    /// <returns>`false` = 这一行一个顶点都没有 / 顶点数不是 4 的倍数（⛔ 别把它当成「范围是 0」）。</returns></summary>
    static bool RowAlphaRange(List<float> alpha, int row, out float min, out float max)
    {
        min = float.MaxValue; max = float.MinValue;
        if (alpha == null || alpha.Count == 0 || (alpha.Count & 3) != 0) { min = max = 0f; return false; }
        int nHit = 0;
        for (int i = 0; i < alpha.Count; i++)
        {
            int k = i & 3;                                     // 0=BL · 1=TL · 2=TR · 3=BR
            if ((row == 1) != (k == 1 || k == 2)) continue;     // `row = 1` 只收上沿那两个角
            if (alpha[i] < min) min = alpha[i];
            if (alpha[i] > max) max = alpha[i];
            nHit++;
        }
        if (nHit == 0) { min = max = 0f; return false; }
        return true;
    }

    /// <summary>在子树里按名字找节点（**含 inactive** —— 自检里很多件是关着的）。</summary>
    static Transform FindChildIn(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    /// <summary>🆕 **2026-10-23（活动线 `L01`/`L02`/`L03` 收尾）**：**一扇活动窗左栏的「事件钮」那一组**。
    /// <para>判据：原版 `BaseParentEventWindow.SetupTabs` + `TabButtons.AddTabButton`（**四扇活动窗同一份**；
    /// 我们收在 `ParentEventWindow.SetupEventTabs` 一处）· `TabButtons__AddTabButton.c` 的 `set_name(label)` ·
    /// `TabButtons.Initialize` 的「高亮恒关」（事件钮点下去是**换窗**不是换页）·
    /// 兄弟序 `SetSiblingIndex(0)` + `SetAsLastSibling()` ⇒ **内容页在前、事件钮在后**。</para>
    /// <para>⛔ 期望值由调用方写死（`wantLabels`），**不读** `BuildEventTabs` 的结果 —— 否则是自证。</para></summary>
    static void CheckEventTabBar(IList<Transform> nodes, IList<Label> labels, IList<Transform> contentTabs,
                                 string winName, string[] wantLabels)
    {
        int n = nodes != null ? nodes.Count : -1;
        Check(n, wantLabels.Length,
              $"★★ 灭自证：{winName} 左栏**真的建出 {wantLabels.Length} 颗事件钮**"
            + $"（实测 {n}；⛔ 删掉 `Build()` 里那句 `SetupTabs(...)` ⇒ 0 颗 ⇒ 本条红）");
        Check(labels != null ? labels.Count : -1, wantLabels.Length,
              $"{winName}：事件钮文字与按钮**一一对应**（{wantLabels.Length} 条）");
        if (n != wantLabels.Length) return;      // 数目不对 ⇒ 下面按下标读会越界，先收

        for (int i = 0; i < n; i++)
        {
            var lb = labels != null && i < labels.Count ? labels[i] : null;
            string got = lb != null ? lb.Text : "<无文字组件>";
            Check(got, wantLabels[i],
                  $"{winName}：第 {i + 1} 颗事件钮的文字 = `{wantLabels[i]}`（顺序 = `EventWindowKind` 值序、跳过自己）");
            Check(nodes[i] != null ? nodes[i].name : "<节点没了>", wantLabels[i],
                      $"{winName}：第 {i + 1} 颗事件钮的**节点名 = 页签文字**（判据 = `AddTabButton` 的 `set_name(label)`）");
            var wb = nodes[i] != null ? nodes[i].GetComponentInChildren<WindowButton>(true) : null;
            CheckTrue(wb != null && wb.onClick != null && !wb.allowClickWhenAlreadySelected,
                      $"{winName}：第 {i + 1} 颗事件钮**点得动**（`WindowButton.onClick` 非空 + 不吞重复点 —— "
                    + "原版 `EverguildToggle.allowClickWhenAlreadySelected = 0`）");
            var hi = nodes[i] != null ? nodes[i].Find("Highlight") : null;
            CheckTrue(hi != null && !hi.gameObject.activeSelf,
                      $"{winName}：第 {i + 1} 颗事件钮的 `Highlight` **建着但关着**"
                    + "（事件钮没有「已选中」这一态 —— 点它是**换窗**，⛔ 别和内容页签的选中灯混成一条）");
            int sibE = nodes[i] != null ? nodes[i].GetSiblingIndex() : -1;
            int sibLastContent = -1;
            if (contentTabs != null) for (int k = 0; k < contentTabs.Count; k++)
                if (contentTabs[k] != null) sibLastContent = Mathf.Max(sibLastContent, contentTabs[k].GetSiblingIndex());
            CheckTrue(sibE > sibLastContent,
                      $"{winName}：第 {i + 1} 颗事件钮在**内容页签之后**（兄弟序 = 原版 `SetAsLastSibling`；"
                    + $"实测事件钮 {sibE} > 末颗内容页 {sibLastContent}）");
        }
    }

    /// <summary>把一组页签名拼成一串（灭自证「三扇窗名单两两不同」用）。</summary>
    static string JoinTabLabels(IList<Label> labels)
    {
        if (labels == null || labels.Count == 0) return "";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < labels.Count; i++)
        {
            if (i > 0) sb.Append(" | ");
            sb.Append(labels[i] != null ? labels[i].Text : "?");
        }
        return sb.ToString();
    }

    /// <summary>🆕 **2026-10-18（A1126 · `A1` 档）**：一个 `Label` **渲出来**的像素宽高（画布 px · `Vector2(w, h)`）。
    /// <para>🔴 **为什么不能用 `Label.WorldW/H`**：那两个是**被测实现自己写的字段缓存**（`_tmpW/_tmpH`，只有
    /// `RefreshBounds()` 写）—— `SetFontSize` / `SetCharSpacing` 这一族**只重排 mesh、不刷缓存**
    /// ⇒ 谁在末次刷缓存之后重排一次，旧口**照旧报旧值**（拿它当检测器 = **自证**，见 `CLAUDE.md` §二
    /// 那条 `AutoFitBox` 教训）。本助手走 TMP 自己那块 `textBounds`（**活值**）。</para>
    /// <para>⚠️ **量不到就退回旧口**（`lb == null` / 底下没有 TMP / **点阵后端** —— 那条路本来就没有
    /// `textBounds`）⇒ 退回与旧口**完全一样**的行为，⛔ 不是「静默吞掉新口」。</para>
    /// <para>⚠️ **同族四份同一份算法**（`Editor/MainMenuScene.cs` 的 `LabelRenderedPx` ·
    /// `Editor/CollectionScene.cs` · `RewardsScene.cs` · `ShopScene.cs`）—— 本文件原来**没有**这一份，
    /// 就地补一个（⛔ 本件白名单外的那几个文件一个字不改）。</para>
    /// <para>⚠️ **前提**：TMP 子节点那一格的 `lossyScale` 为 1 且无旋转（旧口读的是**局部**
    /// `|textBounds.size|`，本口走 `localToWorldMatrix` ⇒ 只有带缩放那一档会差）。</para></summary>
    static Vector2 ShellLabelRenderedPx(Label lb)
    {
        // 🔴 **2026-10-19（`A1265`）**：算法收口到共用件 `Editor/SelfCheckTextMetrics.cs`
        //   （本文件原来是 5 份逐字副本之一；⛔ 别再把函数体抄回来）。
        return SelfCheckTextMetrics.RenderedPx(lb);
    }

    /// <summary>🆕 **2026-10-10（`A1178` 断言重写）**：一颗 `Label` **写进去的那个框**（TMP 的
    /// `rectTransform.rect`）换到**画布 px**，`Vector2(宽, 高)`。
    /// <para>🔴 **换算与 <see cref="ShellLabelRenderedPx"/> 逐字同一条**（`localToWorldMatrix` +
    /// `LayoutSpace.PxX/PxY`）⇒ 两个读数**同源可比** —— 这正是「哪一侧是紧的」那条关系式需要的
    /// （拿它们去减出一个**框的余量**）。</para>
    /// <para>🔴 **为什么必须有它**：TMP 的自适应有**两条**降字号的闸 —— **竖界**
    /// （`TextMeshProUGUI.cs:3393` `textHeight &gt; marginHeight`）与 **横界**（`:3644`），
    /// 而 `marginWidth/marginHeight` **就是填进去的框**（`ComputeMarginSize`，`:2343`）
    /// ⇒ 收敛字号由**较紧的那一侧**决定；「较紧」只能拿**框**与**渲出**两边比出来。
    /// ⚠️ 这里**不取任何期望值** —— 它只提供「我们填进去的框」这个**输入量**（自证那条管的是
    /// 「期望值不许从实现里来」，不是「不许读实现写下的输入」）。</para>
    /// <para>⚠️ 点阵后端（那棵树里没有 TMP）⇒ 返回 `Vector2.zero`：调用方**必须先 gate 掉**，
    /// ⛔ 别让 0 混进减法里（那会变成「框宽 0 ⇒ 渲出宽超框」的**假红**）。</para>
    /// <para>⚠️ 同前提 = TMP 那一格的 `lossyScale` 为 1 且无旋转（同 `ShellLabelRenderedPx`）。</para></summary>
    static Vector2 ShellLabelBoxPx(Label lb)
    {
        // 🔴 **2026-10-19（`A1265`）**：算法收口到共用件 `Editor/SelfCheckTextMetrics.cs`
        //   （别的窗要断「哪一侧是紧的」直接调 `SelfCheckTextMetrics.BoxPx` / `TightSide`，
        //   ⛔ 别再各写一份 —— 本仓「两处写同一条规则 = 迟早不一致」）。
        return SelfCheckTextMetrics.BoxPx(lb);
    }

    /// <summary>🆕 **2026-10-09（`A1125`）**：一颗**关窗钮**的「命中区 + 换图层」四连断。
    /// <para>四个闸落在**四个不同对象**上（前提 / 命中区尺寸 / 命中区位置 / 换图层绑定）⇒ 改坏任一处只红其中一条：
    /// ① 按钮 / 命中 / 可见面三件都取得到（取不到就不许往下断 = 不静默变绿）；
    /// ② 命中区**尺寸** = 原版可射线件**并集**（⛔ 不是根矩形）；③ 命中区**中心** == 可见面渲染中心
    /// （可见面矩形与命中区**不同源** ⇒ 位置那一半的独立锚）；④ 换图层 = 原版 `m_TargetGraphic`
    /// 指到的那一颗（节点名 + 贴图名**两个条件**；`targetNodeName == null` ⇒ 只断**贴图名**）。</para>
    /// <para>`btnName == null` ⇒ 直接在**窗根**下找 `hitName` / `faceName`。用 `FindChildIn`（**含 inactive** ——
    /// `ProfileTab` 那颗在出厂 `SetActive(false)` 的 `ChooseNameWindow` 里）。
    /// ⛔ 期望值全是**原版 prefab 的读数**，不从被测实现里读。判据 → 同族五份 `A1125Close`。</para></summary>
    static void A1125Close(string win, Transform winRoot, string btnName, string hitName,
                           string faceName, string faceTex, string targetNodeName, float wPx, float hPx)
    {
        var btn = winRoot == null ? null : (btnName == null ? winRoot : FindChildIn(winRoot, btnName));
        CheckTrue(btn != null, $"（前提·不静默）A1125 {win}：关窗钮节点 `{btnName ?? "<窗根>"}` 拿得到"
                             + " —— ⛔ 取不到就不往下断（不静默变绿）");
        var hitG = btn != null ? FindChildIn(btn, hitName) : null;
        CheckTrue(hitG != null, $"（前提·不静默）A1125 {win}：命中节点 `{hitName}` 拿得到");
        var faceG = btn != null ? FindChildIn(btn, faceName) : null;
        CheckTrue(faceG != null, $"（前提·不静默）A1125 {win}：可见面子件 `{faceName}` 拿得到");
        var hq = hitG != null ? hitG.GetComponentInChildren<ImageQuad>(true) : null;
        float hx1 = 0f, hy1 = 0f, hx2 = 0f, hy2 = 0f;      // ⛔ 先归零：`&&` 短路时编译器要求 out 已赋值
        bool okH = hq != null && MenuDraw.QuadRectPx(hq, out hx1, out hy1, out hx2, out hy2);
        CheckTrue(okH && Mathf.Abs((hx2 - hx1) - wPx) <= 0.5f && Mathf.Abs((hy2 - hy1) - hPx) <= 0.5f,
                  $"★★ A1125 {win}：关窗钮**命中区** = 原版可射线件并集 **{wPx}×{hPx}**（设计 px）"
                + (okH ? $"（现读 {hx2 - hx1:F2}×{hy2 - hy1:F2}）" : "（命中 quad 取不到）")
                + "｜🧨 ① 改回根矩形 ⇒ 每边小 ~11（绿族 ~10.7/9.8）⇒ 红；② 改回子件裸矩形 ⇒ 每边小 20 ⇒ 红");
        var fq = faceG != null ? faceG.GetComponentInChildren<ImageQuad>(true) : null;
        float fx1 = 0f, fy1 = 0f, fx2 = 0f, fy2 = 0f;
        bool okF = fq != null && MenuDraw.QuadRectPx(fq, out fx1, out fy1, out fx2, out fy2);
        CheckTrue(okH && okF
                  && Mathf.Abs((hx1 + hx2) * 0.5f - (fx1 + fx2) * 0.5f) <= 0.6f
                  && Mathf.Abs((hy1 + hy2) * 0.5f - (fy1 + fy2) * 0.5f) <= 0.6f,
                  $"★★ A1125 {win}：命中区**中心** == 可见面 `{faceName}` 的渲染中心（独立锚）"
                + (okH && okF ? $"（{((hx1 + hx2) * 0.5f):F2},{((hy1 + hy2) * 0.5f):F2} vs "
                                + $"{((fx1 + fx2) * 0.5f):F2},{((fy1 + fy2) * 0.5f):F2}）" : "（取不到）")
                + "｜🧨 把命中节点整体搬走（`localPosition += (20,0,0)`）⇒ 只错位置不错尺寸 ⇒ 只这条红（与 ② 不同源）");
        var wb = hitG != null ? hitG.GetComponent<WindowButton>() : null;
        string got = wb == null || wb.target == null || wb.target.Texture == null
                   ? "<没绑>" : wb.target.gameObject.name + " / " + wb.target.Texture.name;
        CheckTrue(wb != null && wb.target != null && wb.target.Texture != null
                  && wb.target.Texture.name == faceTex
                  && (targetNodeName == null || wb.target.gameObject.name == targetNodeName),
                  $"★★ A1125 {win}：**换图层** = 子件 `{targetNodeName ?? faceName}`（图 `{faceTex}`；"
                + "= 原版 `m_TargetGraphic` 指到的那一颗）—— 现读「" + got + "」"
                + "｜🧨 把它传成根圆底盘（`UI_Button_Round_background`）⇒ 红（错因 = 只读 `m_Transition`、没读 `m_TargetGraphic`）");
    }

    /// <summary>🆕 **2026-10-18（`A1149` 第二半 · 第九会话 P6）**：关窗钮**圆底盘**那颗 quad 的
    /// **实绘矩形**两连断 —— 补 `A1125Close` 上面那个**静默缺口**（那四格读的是：命中区 quad 的矩形 /
    /// 命中区中心 vs 见面中心 / 可见面的**节点名** / 可见面的**贴图名** —— **没有一格量圆底盘的矩形成像**；
    /// 圆底盘等比后**仍居中** ⇒ 连「中心」那一格也抓不住它）。
    /// <para>⛔ **期望值全部来自原版 prefab 的逐字段直读**（⛔ 不是我们现在的实绘值、更不是从被测实现里读回来）：
    /// 圆底盘那几颗一律 `m_Type=0`(Simple) · **`m_PreserveAspect=1`** · `m_PixelsPerUnitMultiplier=1.0`；
    /// 贴图 `UI_Button_Round_background` 的 sprite `m_Rect` = **237×237 正方**、`m_Border` / `m_Offset` 全 0
    /// ⇒ 等比内接进「原版根那一格」= **实绘 min(框宽,框高) 见方**（橙族框 74.39×75.61 ⇒ **74.39×74.39**；
    /// 绿族框 75×75 ⇒ **75×75**）。逐窗读数 → `资料/普查产出_第八会话/B4_InboxWindow圆底盘归真.md` §1·2/§1·3
    /// （本件 P6 用 `d:/tmp/wf_b4probe/pa.py` 逐窗复跑核过）。</para>
    /// <para>`frameW` / `frameH` = **原版根那一格**的框（只进消息）；`basePx` = 期望边长。</para>
    /// <para>🔴 **两格各钉一轴**（① 实绘**宽** == `basePx`；② 实绘**宽 == 实绘高** = 等比不变量）⇒
    /// 两种改坏法**各红不同的一格**、**结构上不可能一起变绿**：把矩形改回**子件框** ⇒ 只 ① 红（那档也近正方 ⇒ ② 绿）；
    /// 去掉 `keepAspect` ⇒ 只 ② 红（宽没变 ⇒ ① 绿）。⛔ 别把高度也塞进 ①（否则 ① 两种改坏法都红、② 失去独立作用）。</para>
    /// <para>🆕 **取法 = 「先根后子」**：① 先取**根节点自己**身上那一颗（= 原版结构：原版那颗 `Image` 就长在根节点
    /// `Generic Close Button Orange` / `Generic Rounded Button Green` 上、**没有独立子件名**）；
    /// ② 根上没有、且调用点**显式给了**子件名 `baseChild` 时才退一步取 `btn/&lt;baseChild&gt;` 那一颗
    /// —— 那是 `A1149` 第一半现读出来的**已知偏离**（圆底盘被画在自造子件上），消息里点名。
    /// `baseChild == null` ⇒ **不许退**。⚠️ 用意：把圆底盘**归真到根节点上不会让本格变红**；
    /// ⛔ 两处都**不下钻**（不用 `GetComponentInChildren` —— 盲扫子树会把「搬到别的层」静默量成合格）。
    /// ⚠️ 本文件的取节点口是 `FindChildIn`（**递归含 inactive**）—— 与 `FindChild` 同一族、语义更宽。</para>
    /// <para>⚠️ 原版根那一格本身是**正方**的窗（绿族 75×75）第二格**恒真** —— 那种窗本来就没有「等比不变量」可丢，
    /// **不是缺口**（见各调用点注释）。</para></summary>
    static void A1125CloseBase(string win, Transform winRoot, string btnName, string baseTex,
                               float basePx, float frameW, float frameH, string baseChild = null)
    {
        var btn = winRoot == null ? null : (btnName == null ? winRoot : FindChildIn(winRoot, btnName));
        // 🔴 `GetComponent`（**本节点自己**那一颗）而不是 `GetComponentInChildren` —— 见上面 doc：
        //    `GetComponentInChildren` 会往下钻，把「圆底盘搬到别的层」那种偏离量成合格（静默）。
        var bq = btn != null ? btn.GetComponent<ImageQuad>() : null;
        bool baseOnRoot = bq != null && bq.Texture != null && bq.Texture.name == baseTex;
        if (!baseOnRoot && btn != null && baseChild != null)
        {
            // ⚠️ **只有调用点显式点名子件时才退这一步**（= `A1149` 第一半现读的已知偏离，消息里点名）
            var baseHost = FindChildIn(btn, baseChild);
            bq = baseHost != null ? baseHost.GetComponent<ImageQuad>() : null;
        }
        string baseWhere = baseOnRoot ? "根节点自己身上（= 原版结构）"
                         : (baseChild != null
                            ? "子件 `" + baseChild + "`（⚠️ **已知偏离**：原版长在根节点自己身上 —— `A1149` 第一半）"
                            : "根节点自己身上（⚠️ 那颗 quad 取不到）");
        string baseGot = bq == null ? "<没有 quad>" : (bq.Texture == null ? "<没贴图>" : bq.Texture.name);
        CheckTrue(bq != null && bq.Texture != null && bq.Texture.name == baseTex,
                  $"（前提·不静默）A1125 {win}：圆底盘 `{baseTex}` 那颗 quad 拿得到、且贴图就是原版那一张"
                + " —— ⛔ 取不到 / 取错就不往下断（不静默变绿）"
                + $"｜现读「{baseGot}」｜取处 = {baseWhere}"
                + "｜🧨 把圆底盘整颗删掉 / 换成别张图 ⇒ 本格红（⛔ 别改成盲扫子树去「修」它）");
        float bx1 = 0f, by1 = 0f, bx2 = 0f, by2 = 0f;      // ⛔ 先归零：`&&` 短路时编译器要求 out 已赋值
        bool okB = bq != null && MenuDraw.QuadRectPx(bq, out bx1, out by1, out bx2, out by2);
        float bw = bx2 - bx1, bh = by2 - by1;
        // 🔴 **本格【只钉宽】**（高由下面那格「宽==高」钉）—— 两格各钉一轴，**改坏法才各红一格**。
        CheckTrue(okB && Mathf.Abs(bw - basePx) <= 0.5f,
                  $"★★ A1125 {win}：**圆底盘实绘宽** = 原版 **{basePx:F2}**（设计 px；配下面那格「宽==高」"
                + $"⇒ 两条一起 = 原版 **{basePx:F2}×{basePx:F2}**）"
                + (okB ? $"（现读宽 {bw:F2}，高 {bh:F2}）" : "（圆底盘 quad 取不到）")
                + $"｜= 原版根那一格 **{frameW:F2}×{frameH:F2}** 的宽（正方贴图 + `m_PreserveAspect=1` 的等比内接）"
                + "｜🧨 把矩形改回**子件框**（橙族 56.86×58.13 / 绿族 56.37×54.50）⇒ 宽少 ~17 ⇒ **只本格红**"
                + "（那两档也近正方 ⇒ 下一格**仍绿**）");
        // 🔴 **本格 = 等比这条不变量**（比断绝对数抗「将来换贴图」：换成别的正方贴图它照旧成立）。
        CheckTrue(okB && Mathf.Abs(bw - bh) <= 0.5f,
                  $"★★ A1125 {win}：圆底盘**实绘宽 == 实绘高**（= **等比**这条不变量；原版根那一格本身是 "
                + $"{frameW:F2}×{frameH:F2} 的框 ⇒ 只有真等比才两轴相等）"
                + (okB ? $"（现读 {bw:F2}×{bh:F2}）" : "（圆底盘 quad 取不到）")
                + $"｜🧨 把 `keepAspect: true` 去掉 ⇒ 实绘变框那一格 {frameW:F2}×{frameH:F2}"
                + (Mathf.Abs(frameW - frameH) > 0.5f ? "（**宽≠高**）⇒ **只本格红**（宽没变 ⇒ 上一格绿）"
                                                     : "（= 同值）⇒ 本窗**框本身正方**，去掉 `keepAspect` 本就无差别")
                + "｜🔴 这就是「它用的是**根那一格** + 等比」的判别式 —— 少了等比，高度就顶到框高");
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
    /// 「这一点上谁吃得到」与具体数字无关。
    /// <para>🔴 **2026-10-20（`A1350` 第二批）换口**：本函数原来走**甲式** `LayoutSpace.ToPixel`，
    /// 与 `PointerLayer.HitBoxPx`（`LayoutSpace.ToDesignPixel(裸世界)` + `PxPerWorldX`）**不同帧**
    /// （非 16:9 下 x 差 `VisibleWidth / DesignWidth` 倍）= 真缺陷 ⇒ 现在走**指针帧**唯一读口
    /// <see cref="MenuCheck.PointerFramePx"/>。⛔ **不是**设计帧 `MenuCheck.DesignPx` —— 那个除父链缩放，
    /// 而指针帧**留着**（画出来多大就多大；判据 → `Shell/PointerLayer.cs` 的 `HitBoxPx` doc）。</para></summary>
    static Vector2 PxCenterOf(Transform node)
    {
        var q = node != null ? node.GetComponentInChildren<ImageQuad>(true) : null;
        if (q == null) return new Vector2(-99999f, -99999f);      // 取不到 ⇒ 一定不在任何命中点上 ⇒ 断言会红
        return MenuCheck.PointerFramePx(q.transform);
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
        foreach (var q in root.GetComponentsInChildren<ImageQuad>(true))
        {
            if (q == null) continue;
            // 🔴 **2026-10-20（`A1350` 第二批）换口**：本段原来自己写的是**甲式**
            //   （`LayoutSpace.ToPixel(q.transform.position)` + `WorldW/H × K`）⇒ 已收口到
            //   `MenuDraw.QuadRectPx`（**乙式**设计帧，唯一读口；与 `QuadPxRect` 逐字同一份）。
            //   ⚠️ `k == 1`（小屏缩放开关出厂关）时两式逐位相同 ⇒ 出厂态读数零变化。
            if (!MenuDraw.QuadRectPx(q, out float hx1, out float hy1, out float hx2, out float hy2)) continue;
            for (int i = 0; i < q.transform.childCount; i++)
            {
                var c = q.transform.GetChild(i).GetComponent<ImageQuad>();
                // ⚠️ **只认软边切出来的子块**（名字闸：`ApplySoftEdges` 起的是 `…_soft<i><j>`）——
                //    九宫格那 9 块是**兄弟**不是父子，但留一道名字闸更保险。
                if (c == null || c.name.IndexOf("_soft") < 0) continue;
                if (!MenuDraw.QuadRectPx(c, out float cx1, out float cy1, out float cx2, out float cy2)) continue;
                if (vertical)
                {
                    float vR = cx1;                          // 子块在**右** ⇒ 切线 = 子块左沿
                    float vL = cx2;                          // 子块在**左** ⇒ 切线 = 子块右沿
                    if (Mathf.Abs(vR - hx2) < 0.5f) { if (!OnClipEdge(vR)) cuts.Add(vR); }
                    else if (Mathf.Abs(vL - hx1) < 0.5f) { if (!OnClipEdge(vL)) cuts.Add(vL); }
                }
                else
                {
                    float vB = cy1;                          // 子块在**下** ⇒ 切线 = 子块上沿（y 向下 ⇒ 小 y）
                    float vT = cy2;                          // 子块在**上** ⇒ 切线 = 子块下沿（y 向下 ⇒ 大 y）
                    if (Mathf.Abs(vB - hy2) < 0.5f) { if (!OnClipEdge(vB)) cuts.Add(vB); }
                    else if (Mathf.Abs(vT - hy1) < 0.5f) { if (!OnClipEdge(vT)) cuts.Add(vT); }
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
    //      `t.parent.lossyScale`（`Shell/MenuDraw.cs` 的 `t.parent.lossyScale`），而 `p2 == M × p1` **只在「基准的父级就是
    //      那个被乘 M 的根、且那个根在世界原点」时成立**（`Shell/MenuDraw.cs` 自己写着适用范围）。
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
                + "`Shell/MenuDraw.cs`）—— 父级单位缩放时新旧两式**恒等**，这条断言就等于没查");
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
    /// <summary>截图 —— 本文件那一份的空图护栏走共用件（「已放行」那句文案 = 本文件原来那一句，
    /// 由 `_sink.BlankNote` 保住）。⚠️ `MeanBrightness` **仍留在本文件**：它与另外四份**浮点值不等价**
    /// （分母相除方式不同），合并会动 `✓` 行文本 —— 判据见 `Editor/MenuCheck.cs` 的 `Shoot` 文件头。</summary>
    static void Shoot(string file, bool allowBlank = false)
        => MenuCheck.Shoot(_sink, ShotDir, file, allowBlank, guardBlank: true, meanBrightness: MeanBrightness);

    static Transform Find(string name, Transform root)
    {
        foreach (var t in root != null ? root.GetComponentsInChildren<Transform>(true) : new Transform[0])   // 🆕 2026-10-28（B93_E27 · 调度台裁定）：null 根**返回 null 而不是 NRE**（一行内改，行数 0 变化，⛔ 别拆行 —— 本文件有账按行号引用）
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
        // 🔴 **2026-10-19（`A1534` 尾巴 · 铁律 11）**：批处理自检**不出声** —— `WFSoundPlayer.Enabled = false`
        //   时那个开关自己的契约是「**只记账不发声**：不建对象、不出声，但 `Played` 照样涨」
        //   ⇒「音效调度算对了没有」那一族断言**照旧有效**，而这一趟**不白建一堆 `AudioSource`、不白播**。
        //   先例 = `WarpforgeArena1/Editor/AnimFXCheck.cs`（同一个开关、同一个 `finally` 还原）。
        //   ⚠️ 它是**全局静态开关** ⇒ 必须在 `finally` 里还原；`RunCore` 本体保持原样（只换了名字）。
        bool prevSoundEnabled = WarpforgeVFX.WFSoundPlayer.Enabled;
        WarpforgeVFX.WFSoundPlayer.Enabled = false;
        try { RunCore(); }
        finally { WarpforgeVFX.WFSoundPlayer.Enabled = prevSoundEnabled; }
    }

    /// <summary>`Run()` 的**本体**（🆕 `A1534` 尾巴：`Run` 只负责「关音效 → 调它 → 还原」这一圈）。
    /// ⚠️ 入口仍是 `Run`（`-executeMethod ShellScene.Run` 一个字没变）。</summary>
    static void RunCore()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
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

        // ---------------- ①·b 🆕 2026-10-17（界面线 T16）：离屏烘焙件的**出厂态** ----------------
        //
        // 判据（现核 → `资料/普查产出_1017/UI/D_T16_UIBakeScreen判据.md`）：
        //   原版 `UIBakeScreen`（`d:/2/tools/decomp_full/UIBakeScreen__*.c`）+ 场景节点
        //   `Game UI ▸ Main Canvas ▸ Background Render Texture Holder`（RT 1118 / MB 1976 / RawImage 1977）
        //   + 材质 `UI Blur`（`Material_3.json` / `Shader_339`：真属性只有 `_MainTex` / `_Sigma 2.25` / `_steps 10`）。
        // 🔴 **这一段必须紧跟在 `Build()` 之后**（`Run` 里后面有几处 `baseMenu` 探针会把烘焙点亮 ⇒
        //    「出厂不显示」只有在这里才断得真）。
        // 🔴 期望值全是**原版字面量**（0.25 / 24 / 2.25 / 10 / `m_RaycastTarget 0`）；渲染队列那条写的是
        //    **梯子约束**（2890 < q < 2900 —— 两侧都是别的件现用的号），⛔ 不是拿我们自己的常量自证。
        Section("T16：离屏烘焙 + UI Blur —— 出厂态（不显示 / 无 RT / 不登记命中 / 材质属性照原版）");
        {
            var bake0 = shell.Bake;
            CheckTrue(bake0 != null, "壳里建了 `Background Render Texture Holder`（原版节点名）");
            CheckTrue(bake0 != null && bake0.image != null,
                      "它下面有那块整屏图（我们 = `ImageQuad`，原版 = uGUI `RawImage`）");
            if (bake0 != null && bake0.image != null)
            {
                var img = bake0.image;
                CheckNear(img.WorldH, LayoutSpace.DesignHeight, 0.0001f,
                          "整屏图的可见高 = 10 世界单位（= 设计高 1080px ⇒ 「整屏」）");
                CheckTrue(!bake0.Showing, "出厂**不显示**（原版 `Start`: `image.enabled = false`）");
                Check(bake0.RT, null,
                      "出厂**还没有 RT**（原版 `CreateRT` 只在第一次 `Toggle(true)` / 分辨率变化时跑）");
                CheckTrue(bake0.GetComponentsInChildren<WindowButton>(true).Length == 0,
                          "**不登记命中区**（原版 `RawImage.m_RaycastTarget = 0`）—— 子树里没有 `WindowButton`");

                var mr = img.GetComponent<MeshRenderer>();
                int q = mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.renderQueue : -1;
                CheckTrue(q > 2890 && q < 2900,
                          $"整屏快照的渲染队列 = {q}（必须 2890 < q < 2900："
                        + "高过压暗层 `FadeQueue`、低过全部窗 —— 否则要么盖住窗、要么被背景盖住）");

                var mat = bake0.blitBlurMaterial;
                CheckTrue(mat != null, "模糊材质建出来了（原版 `blitBlurMaterial` = `UI Blur`）");
                if (mat != null)
                {
                    Check(mat.shader != null ? mat.shader.name : "(null)", "CardPresentation/UIBlur",
                          "shader = 自建等价实现 `CardPresentation/UIBlur`（原版 `Shader_339`；等价、未逐条核字节码）");
                    CheckNear(mat.GetFloat("_Sigma"), 2.25f, 0.0001f, "`_Sigma = 2.25`（原版 `Material_3.json` 实读）");
                    CheckNear(mat.GetFloat("_steps"), 10f, 0.0001f, "`_steps = 10`（同上）");
                    CheckTrue(!mat.HasProperty("_BlurRadius") && !mat.HasProperty("_GridSize")
                           && !mat.HasProperty("_Iterations") && !mat.HasProperty("_xBlur")
                           && !mat.HasProperty("_yBlur") && !mat.HasProperty("_Spread"),
                              "原版材质里那些**旧 shader 残留死值**（`_BlurRadius`/`_GridSize`/`_Iterations`/"
                            + "`_xBlur`/`_yBlur`/`_Spread`）**一个都没接**（接了就是把死值当参数）");
                }
            }
        }

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

        // ---------------- ④·b 🆕 2026-10-17（N8）：**全屏黑场挡指针** ----------------
        //
        // 判据（原版实读 → `资料/普查产出_1017/UI/R4_黑场挡点击诊断.md` §四）：
        //   · 黑图 `Panel` 的 `Image.m_RaycastTarget = 1`（场景 `MonoBehaviour_1960.json:22`，prefab 同值）
        //     + `Game UI` 上的 `GraphicRaycaster`（`MonoBehaviour_2579.json`）
        //     ⇒ **原版靠 uGUI 射线把整屏点击吃在黑洞图上**（我们这条线不走 uGUI 射线 ⇒ 必须显式问一句）；
        //   · 同一条判据**只挡指针**：`d:/2/tools/decomp_full/WindowsManager__Update.c` 的 ESC 分支里
        //     **没有任何 loading / 遮挡门槛** ⇒ 键盘（ESC / 回车 / 文本框）不受影响。
        // 🔴 **灭自证**：下面每一条都是**同一坐标、翻一个状态**（出厂 / 遮罩 / 黑场 / 淡回后）——
        //    只断「黑场中挡」的话，一个「`HitButton` 恒返回 null」的实现也能全绿。
        Section("N8：全屏黑场（`LoadingMenu`）挡指针 —— 命中 / 滚轮 / 悬停被吃掉；键盘与直调口不受影响");
        {
            var plN8 = PointerLayer.Instance;
            var lmN8 = shell.Loading;
            CheckTrue(plN8 != null, "前置：指针层在场（下面每一条都走它这条唯一输入路）");
            CheckTrue(lmN8 != null && lmN8.Panel != null,
                      "前置：壳侧黑场 `LoadingMenu.Panel` 建出来了（取不到 ⇒ 本节全是空断）");

            // 探针：一颗真钮 + 一个滚动区（命中点是同一个 (400,400)，几态共用）
            var n8Go = new GameObject("N8Probe");
            var n8Hits = new GameObject("Hits").transform;
            n8Hits.SetParent(n8Go.transform, false);
            var n8Btn = MakeNavButton(n8Hits, "N8Btn", 400f, 400f, 80f, 40f);
            int n8Fired = 0;
            n8Btn.onClick = () => n8Fired++;
            var n8Scroll = MenuScroll.TopAligned(new PxRect(200f, 200f, 600f, 900f), 2000f);
            n8Scroll.Owner = n8Go;
            PointerLayer.RegisterScroll(n8Scroll);

            // ---- ① 出厂态（对照：证明这颗钮 / 这个滚动区**本来就可命中**）
            CheckTrue(!PointerLayer.InputBlocked, "① 出厂：`InputBlocked == false`（黑场 Panel 关着、遮罩没开）");
            CheckTrue(plN8.ButtonAt(400f, 400f) == n8Btn,
                      "① 出厂：同一坐标命中的是探针钮（= 下面「黑场中 == null」的对照）");
            float n8Off0 = n8Scroll.Offset;
            CheckTrue(plN8.WheelAt(400f, 400f, -1f), "① 出厂：滚轮滚得到（`WheelAt` 命中滚动区）");   // 2026-10-22 量纲 120→1（一格 = ±1）
            CheckTrue(n8Scroll.Offset > n8Off0, "① …而且偏移**真的动了**（返回 true 但没滚 = 假绿）");
            CheckTrue(plN8.HoverAt(400f, 400f) == n8Btn && n8Btn.Hovered, "① 出厂：悬停落到它身上");

            // ---- ② `BlockingOverlay`（降级件）也接上 —— 改前 `IsBlocking` 是**零消费者**
            shell.Blocker.StartSpinning();
            CheckTrue(PointerLayer.InputBlocked, "② `BlockingOverlay.StartSpinning()` ⇒ `InputBlocked == true`（把零消费者接上）");
            CheckTrue(plN8.ButtonAt(400f, 400f) == null, "② …同一坐标命不到了");
            CheckTrue(!plN8.WheelAt(400f, 400f, -1f), "② …滚轮也落空");   // 2026-10-22 量纲 120→1（一格 = ±1）
            shell.Blocker.StopSpinning();
            CheckTrue(!PointerLayer.InputBlocked, "② 停转之后放开（与上面那一对合起来才分得开）");
            CheckTrue(plN8.ButtonAt(400f, 400f) == n8Btn, "② …同一坐标恢复命中");

            // ---- ③ 黑场态
            lmN8.LoadingFade(true);
            lmN8.TickForTest(2f);                       // > FadeSeconds（批处理里 `Complete()` 已当场落终态）
            CheckTrue(lmN8.Panel.gameObject.activeInHierarchy,
                      "③ 黑场 Panel 在屏上（判别式 = `activeInHierarchy`，⛔ 不是 `IsShowing`："
                      + "淡入首帧 α=0 时原版的射线靶照样在）");
            CheckTrue(PointerLayer.InputBlocked, "③ 黑场期间 `InputBlocked == true`");
            CheckTrue(plN8.ButtonAt(400f, 400f) == null, "③ 黑场：同一坐标**命不到**探针钮");
            CheckTrue(!plN8.ClickAt(400f, 400f), "③ 黑场：`ClickAt` 也吃不到（命中既空 ⇒ 不派发）");
            Check(n8Fired, 0, "③ …探针钮一次都没被点");
            float n8Off1 = n8Scroll.Offset;
            CheckTrue(!plN8.WheelAt(400f, 400f, -1f),   // 2026-10-22 量纲 120→1（一格 = ±1）
                      "③ 黑场：滚轮落空（`HitScroll` 是**另一条**登记表路 —— 只拦 `HitButton` 时这里会静默地滚）");
            CheckNear(n8Scroll.Offset, n8Off1, 0.001f, "③ …偏移一个像素都没动");
            CheckTrue(plN8.HoverAt(400f, 400f) == null && plN8.HoveredForTest == null && !n8Btn.Hovered,
                      "③ 黑场：悬停被**退掉**（不是只把返回值改 null —— `HoveredForTest` 与 `Hovered` 两面都断）");

            // ---- ④ 键盘**不挡**（原版 `WindowsManager__Update.c` 的 ESC 分支没有 loading 门槛）
            var n8WinGo = new GameObject("N8EscWin");
            var n8Win = n8WinGo.AddComponent<GameWindow>();
            n8Win.type = WindowType.Fullscreen;
            n8Win.closeOnEsc = true;
            n8Win.placement = WindowsPlacement.Canvas;
            WindowsManager.AttachToAnchor(n8Win);
            shell.Windows.OpenWindow(n8Win);
            Check(n8Win.CurrentState, WindowState.Open, "④ 前置：探针窗开着（`closeOnEsc = true`）");
            CheckTrue(plN8.KeyCancel(),
                      "④ 黑场期间 **ESC 仍然关得掉窗**（原版键盘不走射线、不受黑场影响；⛔ 别顺手把 `KeyCancel` 也拦了）");
            Check(shell.Windows.TopWindow, null, "④ …那扇窗真的关了");
            shell.Windows.CloseAllWindows();            // 兜底：上面那条若红，别把窗留给 ⑤ 去数
            Object.DestroyImmediate(n8WinGo);

            // ---- ⑤ 直调口**不挡**（我们只拦射线命中，不拦 `Click()` 派发）
            int n8Before = n8Fired;
            n8Btn.ClickForTest();
            Check(n8Fired, n8Before + 1, "⑤ 黑场期间 `ClickForTest()` 仍然派发（自检/工具直调口不许被黑场挡掉）");

            // ---- ⑥ 淡回后（灭自证：同一坐标全部恢复；同时钉住 N8 的前置缺陷「黑场永不淡出」）
            lmN8.LoadingFade(false);
            lmN8.TickForTest(2f);
            CheckTrue(!lmN8.Panel.gameObject.activeInHierarchy, "⑥ `LoadingFade(false)` + 补间之后 Panel 关掉了");
            CheckTrue(!PointerLayer.InputBlocked, "⑥ `InputBlocked == false`（收尾之后不锁输入 —— 前置缺陷的反面）");
            CheckTrue(plN8.ButtonAt(400f, 400f) == n8Btn,
                      "⑥ 淡回后同一坐标**恢复命中**探针钮（⛔ 只断 ③ 不断这条 ⇒「恒返回 null」也能全绿）");
            float n8Off2 = n8Scroll.Offset;
            CheckTrue(plN8.WheelAt(400f, 400f, -1f), "⑥ 淡回后滚轮恢复");   // 2026-10-22 量纲 120→1（一格 = ±1）
            CheckTrue(n8Scroll.Offset > n8Off2, "⑥ …偏移真的又动了");
            CheckTrue(plN8.HoverAt(400f, 400f) == n8Btn && n8Btn.Hovered, "⑥ 淡回后悬停恢复");

            // 清场（批处理没有帧循环 ⇒ 一律 `DestroyImmediate`；先退悬停、再退滚动登记）
            plN8.HoverAt(10f, 10f);
            PointerLayer.UnregisterOwnedBy(n8Go);
            Object.DestroyImmediate(n8Go);
        }

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

        // ---------------- 🆕 2026-10-24（A11）：消息弹窗 = **销毁旧的 + 按按钮数重建** ----------------
        // 判据（第一权威 = 反编译方法体）→ `WindowsManager._LoadPopup_d__55__MoveNext.c:20-45`：
        //   `if (popUpWindow != null) { CloseWindow(this, popUpWindow, false); Destroy(popUpWindow.gameObject); }`
        //   → 按 `twoButtons` 挑 prefab `Instantiate` → 赋回 `*(this+0x60)`。
        // 改前（A416/A454）：**复用同一扇 + `RefreshLiveContent`** —— 不是原版，而且有一档**丢钮**：
        //   场上是 **1 按钮版**实例、这次要两颗 ⇒ 右钮那颗**画不出来**（A6 在调用点兜过底，见
        //   `资料/普查产出_1024/A6_传奇合成确认窗.md` §六·1）。
        // 灭自证：本节点同时咬三件 ——「换了新的一扇」「新的是 2 按钮版且右钮**有字**」「旧的那扇**被销毁**」。
        Section("A11：消息弹窗 = 销毁旧的 + 按按钮数重建（原版 `_LoadPopup` 那一支）");
        {
            CheckTrue(shell.Windows.popUpWindow == null, "（前提）场上没有遗留的弹窗（上一节收尾过）");
            var a11One = shell.Windows.ShowMessagePopUp("A11·一钮第一条", "确定", null);
            CheckTrue(a11One != null && !a11One.TwoButtons && ReferenceEquals(shell.Windows.popUpWindow, a11One),
                      "（前提）先开一扇 **1 按钮版**、它还开着、而且就是 `popUpWindow`");
            // 🔴 **致命档**：1 钮版**还开着**时再要两颗 —— 改前会复用那一扇 ⇒ 右钮画不出来
            var a11Two = shell.Windows.ShowMessagePopUp("A11·两钮第二条", "确定", null, "取消", null);
            CheckTrue(a11Two != null && !ReferenceEquals(a11Two, a11One),
                      "★ A11：1 钮版开着时再要两颗 ⇒ **换了一扇新的**（原版每次都 `Instantiate`；改回复用 ⇒ 返回的还是同一扇 ⇒ 本条红）");
            CheckTrue(a11Two != null && a11Two.TwoButtons,
                      "★ …而且新那扇是 **2 按钮版**（复用时它仍是 1 钮版 ⇒ 右钮**画不出来**，正是 A6 报的那一档）");
            Check(a11Two != null ? a11Two.SecondaryShown : null, Loc.T("MainMenu/General/Cancel"),
                  "★ …右钮**真画出来了**（= 第二颗 `LiveButtons` 的文案；改回「在 1 钮版上 `SetActive(true)`」⇒ 这条红）");
            CheckTrue(a11One == null,
                      "★ …**旧那一扇被销毁了**（原版 `Object.Destroy(Component.get_gameObject())`；"
                    + "改回「只 `Close()` 不 Destroy」⇒ 旧对象还在 ⇒ 本条红。"
                    + "⚠️ 批处理走 `DestroyImmediate` ⇒ 当场为 null；player 里 `Destroy` 是帧末生效）");
            Check(shell.Windows.openWindows.Count, 2, "（收尾）底窗 1 + 弹窗 1 = 2");
            // 再开一条**不同文案**的：仍要新建（顺带把「复用 + 刷新内容」那一版被悄悄改回来也挡住）
            var a11TwoB = shell.Windows.ShowMessagePopUp("A11·两钮第三条", "确定", null, "取消", null);
            CheckTrue(a11TwoB != null && !ReferenceEquals(a11TwoB, a11Two),
                      "★ …第二条弹窗同样**换新的一扇**（不复用）");
            Check(a11TwoB != null ? a11TwoB.MessageShown : null, "A11·两钮第三条",
                  "★ …正文跟着换成这一次的");
            CheckTrue(a11Two == null, "★ …中间那一扇也被销毁了（只在新建**之前**销毁旧的，⛔ 不留一串尸体）");
            // 收尾：**直调** `ShowMessagePopUp` 没有 `ShowPopUp` 那层关窗包装 ⇒ 自己 `Close()`（同 A569·S1 的写法）
            if (a11TwoB != null) a11TwoB.Close();
            CheckTrue(shell.Windows.popUpWindow == null, "（收尾）`popUpWindow` 位空着");
            Check(shell.Windows.openWindows.Count, 1, "（收尾）只剩底窗 1 个");
        }

        // ---------------- 🆕 2026-10-24（A11）：商店「买传奇」确认框 = **左 Cancel / 右 OK** ----------------
        // 判据（第一权威 = 反编译方法体）：`CatalogItemContainer__TryPurchase.c:30-50` 与
        //   `CraftingPanel__TryCrafting.c:26-49` **同形** —— `buttons[0] = Cancel`（左）· `buttons[1] = OK`（右）；
        //   而 `PopUpGameWindow.ConfigurePopUp` 的 `LiveButtons[0]` = `ButtonLeft`（= `ShowPopUp` 的 primary）。
        // 改前：`ShopTabPage.Buy` 把 `OK` 当 primary 传 ⇒ **左 OK / 右 Cancel**，与隔壁锻造确认窗
        //   （左 Cancel / 右 OK）自相矛盾（发现于 `资料/普查产出_1024/A6_传奇合成确认窗.md` §六·2）。
        // 灭自证：两条 ★ 断的是**左右两颗钮各自画的是什么字**（不是「我们传了哪个常量」）——
        //   把调用点的两个参数换回去 ⇒ 两条同时红；只对调回调不对调文字 ⇒ 「点左钮不买 / 点右钮买」红。
        Section("A11：商店「买传奇」确认框 —— 左 = Cancel · 右 = OK（原版 `buttons[0]/[1]` 顺序）");
        {
            CheckTrue(ShopData.NeedsLegendaryConfirm(0, 2),
                      "（前提）这一件真的是**传奇（Rarity 4）且 owned == 1**（`ShopData` 第 0 页第 2 件"
                    + " `Space Wolves Booster`）—— 否则开出来的根本不是那扇确认框");
            CheckTrue(shell.Windows.popUpWindow == null, "（前提）场上没有遗留的弹窗（上一节收尾过）");
            int a11OwnedBefore = ShopData.OwnedOf(0, 2);
            bool a11Confirmed = false;
            var a11Left = ShopTabPage.ShowLegendaryConfirm(shell.Windows, () => a11Confirmed = true);
            CheckTrue(a11Left != null && a11Left.TwoButtons,
                      "★ A11：确认框开出来了、而且是 **2 按钮版**（原版传了两颗 `GameWindowButton` ⇒ 两钮 prefab）");
            if (a11Left != null)
            {
                Check(a11Left.PrimaryShown, Loc.T("MainMenu/General/Cancel"),
                      "★ …**左钮 = Cancel**（原版 `buttons[0]`；改回「把 OK 当 primary 传」⇒ 本条与下一条一起红）");
                Check(a11Left.SecondaryShown, Loc.T("MainMenu/General/OK"),
                      "★ …**右钮 = OK**（原版 `buttons[1]`）");
                ClickPopUpButton(a11Left, "ButtonLeft");
                CheckTrue(!a11Confirmed,
                          "★ …点**左钮（Cancel）**⇒ **不买**（确认回调一条都没跑；两颗钮的回调接反 ⇒ 本条红）");
                Check(ShopData.OwnedOf(0, 2), a11OwnedBefore,
                      "★ …而且拥有数**没变**（取消不发货；判据 = `ShopData.OwnedOf` 现读，⛔ 不是看日志）");
                CheckTrue(!a11Left.gameObject.activeSelf,
                          "★ …窗自己关掉了（Cancel 那颗的回调是 null ⇒ 靠 `WindowsManager.ShowPopUp` 那层包装关窗；"
                        + "绕开它直调 `ShowMessagePopUp` ⇒ 这条红）");
            }
            Check(shell.Windows.openWindows.Count, 1, "（收尾）关掉之后只剩底窗 1 个");

            // 右钮那一半：两条合起来才钉住「**哪颗钮**调**哪个回调**」两边都对
            a11Confirmed = false;
            var a11Right = ShopTabPage.ShowLegendaryConfirm(shell.Windows, () => a11Confirmed = true);
            CheckTrue(a11Right != null && a11Right.TwoButtons, "（前提）重开一扇同样是 2 按钮版");
            if (a11Right != null)
            {
                ClickPopUpButton(a11Right, "ButtonRight");
                CheckTrue(a11Confirmed,
                          "★ …点**右钮（OK）**⇒ 确认回调真的执行了（生产路径传的是 `() => DoBuy(captured)`）");
                CheckTrue(!a11Right.gameObject.activeSelf, "★ …而且窗自己关掉了");
            }
            Check(ShopData.OwnedOf(0, 2), a11OwnedBefore,
                  "（复核）本节点只验**回调接线**、没有真去买 ⇒ 拥有数仍然不变"
                + "（⛔ 别把这里的 `() => a11Confirmed = true` 换成生产那个 `DoBuy`：那会把商店状态写脏）");
            Check(shell.Windows.openWindows.Count, 1, "（收尾）又只剩底窗 1 个");
        }

        // ---------------- 🆕 A672：`type` 忘了显式赋值 ⇒ **第二层出声**（有人绕过 `AttachToAnchor`）----------------
        // 生产侧（哨兵 `UnsetType` + 两层出声 + 兜底写回）→ `Shell/WindowsManager.cs`（写手 W8 落地；
        //   报告 = `资料/普查产出_1015/W8_WindowsManager_A672与A633.md`）。
        // 🔴 **第一层**（建窗那一刻 `AttachToAnchor`）的夹具断言在 `Editor/SettingsScene.cs` 的同名节；
        //   **本节点只守第二层**（`OpenWindow`）—— 两层各一处，⛔ 别在两边重复同一件事。
        // 挂在本宿主的原因：这里有一台**真的** `WindowsManager`（`shell.Windows`）+ 成对的开关窗夹具，
        //   而第二层那条守卫的兜底会让这一跳走**全屏支**（`HideAllWindows`）—— 本文件到处是
        //   `CloseAllWindows()` 那种「先隔离、后收尾」的写法（引用隔离）。
        Section("A672：`type` 忘了显式赋值 ⇒ 出声（② 绕过 `AttachToAnchor` 直接开）");
        {
            CheckTrue(shell.Windows.openWindows.Count == 1 && win.CurrentState == WindowState.Open,
                      "（前提）场上只有 ⑤ 那扇底窗、且它是 `Open`（本节点收尾会把它原样开回去）");
            shell.Windows.CloseAllWindows();      // 隔离：下面那一开走**全屏支** ⇒ 不清空的话会把别的窗一起藏掉
            var a672Go = new GameObject("probe window (type 未赋 + 绕过 AttachToAnchor)");
            var a672Win = a672Go.AddComponent<GameWindow>();
            CheckTrue(!a672Win.HasType,
                      "裸 `GameWindow` 的 `type` 出厂是**哨兵**（⛔ 没走 `AttachToAnchor` ⇒ 也没人给它补值）");
            var a672Errs = new List<string>();
            Application.LogCallback a672H = (msg, stack, type) =>
            { if (type == LogType.Error || type == LogType.Exception) a672Errs.Add(msg); };
            Application.logMessageReceived += a672H;
            try { shell.Windows.OpenWindow(a672Win); } finally { Application.logMessageReceived -= a672H; }
            Debug.Log(P + "  ⚠️ 上面那一开**故意**打了一行 `[Win] …` 的 LogError（就是「出声」本身）—— 那不是失败");
            CheckTrue(a672Errs.Count > 0 && a672Errs[0].Contains("`type` **还是哨兵**"),
                      "★ **绕过 `AttachToAnchor` 直接开 ⇒ 第二层也出声**（改坏法：把 `WindowsManager.OpenWindow`"
                    + " 里那段 `if (!win.HasType) { … }` 删掉 ⇒ 一声不吭 ⇒ 这条红）；实得 " + a672Errs.Count
                    + " 条：" + (a672Errs.Count > 0 ? a672Errs[0] : "**一条都没有**"));
            CheckTrue(a672Win.type == WindowType.Fullscreen && a672Win.HasType,
                      "…而且照**旧默认值 `Fullscreen`(0)** 兜底 + 写回（⛔ 不是「不等于 `Fullscreen` 就当弹窗」）");
            // 🔴 **灭自证的那一条**：删掉那段守卫 ⇒ 哨兵 `-1` 在 `if (win.type == Fullscreen)`（**肯定式**）下判假
            //   ⇒ 会落进**弹窗支**（把顶端那扇压到背景 + 把自己记成 `popUpWindow`）—— 那是与「静默走全屏支」
            //   **不同的另一种坏法**，而它同样不会有任何断言自己红。上面那条只证明「哨兵被兜底成了 Fullscreen」，
            //   这一条直接咬「**走的是哪一支**」（万一有人把兜底值改成 `Popup`，那条绿、这条红）。
            CheckTrue(shell.Windows.popUpWindow != a672Win && shell.Windows.currentWindow == a672Win,
                      "★ 走的是**全屏支**、不是弹窗支（`currentWindow` = 它、`popUpWindow` **不是**它）"
                    + "；改坏法：删掉那段守卫 ⇒ 哨兵落进弹窗支 ⇒ `popUpWindow` 变成它 ⇒ 这条红");
            shell.Windows.CloseAllWindows();
            Object.DestroyImmediate(a672Go);
            shell.Windows.OpenWindow(win);         // 收尾：把 ⑤ 那扇底窗原样开回去（下面 ⑤b 接着用这一片现场）
            CheckTrue(shell.Windows.openWindows.Count == 1 && win.CurrentState == WindowState.Open,
                      "（收尾）现场还原：还是只有 ⑤ 那扇底窗、且它是 `Open`");
        }

        // ---------------- 🆕 A569·S1：`ShowPopUp` / `ShowMessagePopUp` 这条路**会把旧句柄作废** ----------------
        //   为什么当年没扫到：普查脚本（`D:/tmp/wf569/scan8.py`）的触发集只内联**同文件内**的助手
        //   ⇒ `Shell` 侧这条开窗路（`WindowsManager.ShowPopUp` → `ShowMessagePopUp` → `OpenWindow`）
        //   **永远进不来**。现读核过 5 处夹具（`grep -n '\.ShowPopUp(\|\.ShowMessagePopUp(' Editor/*.cs`：
        //   `Editor/ShellScene.cs` 4 处 · `Editor/DeckScene.cs` 1 处）**今天都安全**（都从 `popUpWindow` 重取）
        //   —— 但「必须重取」这件事**没有任何断言守着**。本节点把它变成会红的断言（判据见下两条 ★）。
        Section("A569·S1：`ShowPopUp`/`ShowMessagePopUp` 关过再开 ⇒ 旧句柄不作数（必须重取）");
        {
            CheckTrue(shell.Windows.openWindows.Count == 1 && win.CurrentState == WindowState.Open,
                      "（前提）场上只有 ⑤ 那扇底窗（上一节收尾还原过；本节点收尾同样还回去）");
            var s1A = shell.Windows.ShowMessagePopUp("A569 S1 明文正文", "确定", null);
            CheckTrue(s1A != null && ReferenceEquals(shell.Windows.popUpWindow, s1A),
                      "（前提）第一扇弹窗开出来了、而且它就是 `popUpWindow`（`HidePopUp` 第一句比的就是这一位）");
            // ⚠️ 下面一律**空值安全**地取（照 ⑤·a 那节 `hp2 != null` 的写法）：`Create` 万一交回 `null`，
            //   `x.transform` 那一句就是**当场 NRE 崩**（后面整片都不跑 —— 比红更糟，同 A569·S1 在 DeckScene 那条前提）。
            var s1BtnA = s1A != null ? FindChildIn(s1A.transform, "Generic UI Button") : null;
            CheckTrue(s1BtnA != null, "（前提）窗内那颗钮找得到（下面拿它当「窗内句柄」的样本）");
            if (s1A != null) s1A.Close();
            CheckTrue(shell.Windows.popUpWindow == null,
                      "（前提）关掉之后 `popUpWindow` 位**空了**（`Close()` = 摘表 + 清位 + `SetActive(false)`；"
                    + "`HidePopUp` 第一句比的就是这一位）");
            CheckTrue(s1A == null || !s1A.gameObject.activeSelf, "（前提）……而且那一扇不激活了");
            var s1B = shell.Windows.ShowMessagePopUp("A569 S1 明文正文（第二次）", "确定", null);
            CheckTrue(s1B != null && s1B != s1A,
                      "★ A569·S1：关掉之后再开 ⇒ **新的一扇**（原版 `CloseWindowCO` 是 `Object.Destroy(win.gameObject)`"
                    + " ⇒ 关过就是没了 ⇒ 再开只能新建）—— 所以**上一拍抓的窗句柄不能接着用**"
                    + "；改坏法：让 `StillOpen` 认「实例还在」而不是「还在 `openWindows` 里」⇒ 复用同一扇 ⇒ 这条红");
            var s1BtnB = s1B != null ? FindChildIn(s1B.transform, "Generic UI Button") : null;
            // 🔴 本节的重点：**上一拍抓的窗句柄不能接着用**。`Close()` 本身只 `SetActive(false)`、**不销毁**
            //   （A123 那条老账），所以那一刻它还**不是** null；而**下一次 `ShowMessagePopUp` 会把它销毁**
            //   （2026-10-24 `A11` 照原版 `_LoadPopup` 改的，见上面那一节）⇒ 再往后它就成假 null 了。
            //   两种状态都挡不住 `if (x != null)` 那种守卫 —— 唯一正确的处置就是「**重取**」。
            CheckTrue(s1BtnB != null && s1BtnB != s1BtnA,
                      "★ …而且**窗内子树也是新的**（`s1BtnA` 从那一下就**不再是**这一扇窗里的钮了）"
                    + "；改坏法：把上面那句 `FindChildIn(s1B.transform, …)` 换成沿用 `s1BtnA` ⇒ 这条红");
            CheckTrue(s1A == null || !s1A.gameObject.activeSelf,
                      "★ …旧那一扇**始终不激活**（⛔ 别把「它还活着」当成「它还是当前那一扇」—— `!= null` 分不出这两态）");
            shell.Windows.CloseAllWindows();
            if (s1A != null) Object.DestroyImmediate(s1A.gameObject);
            if (s1B != null) Object.DestroyImmediate(s1B.gameObject);
            CheckTrue(shell.Windows.popUpWindow == null, "（收尾）`popUpWindow` 位空着 —— 别把弹窗留给后面的段");
            shell.Windows.OpenWindow(win);         // 现场还原（同上一节）
            CheckTrue(shell.Windows.openWindows.Count == 1 && win.CurrentState == WindowState.Open,
                      "（收尾）现场还原：还是只有 ⑤ 那扇底窗、且它是 `Open`");
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
            //      （`ShellScene.cs` 的 `MessageText`：`PanelH` = `MessageText` **实测**渲染高与 100 取大 + 110），不是拿本条算式反推。
            // ⚠️ 量的是**九块的并集**（`CreateNineSlice` 建的是「根 + 9 块」，只取第一块会量成某个角块 ——
            //   `Editor/SettingsScene.cs` 里那条「弹窗底量成 182×173」注 那条注释记的「弹窗底量成 182×173」当场踩的就是这个）。
            // ⚠️ 换算：🔴 **2026-10-20（`A1350` 第二批）已换口** —— 下面那段内联循环现在走 `QuadPxRect`
            //   （= `MenuDraw.QuadRectPx`，**乙式**设计帧）。（改前原文：「本处这一段仍是甲式
            //   （下面那段内联循环走 `LayoutSpace.ToPixel(q.transform.position)` + `WorldW/H × K`）」——
            //   换口后 16:9 + 本窗缩放恒 1 下甲乙两式逐位相同 ⇒ 读数不变。）
            //   🔴 **2026-10-18（A1003）就地订正（铁律 5）**：这一行原来写的是「**沿用本文件 `PiecesOutsideClip`
            //   的口径**」—— `PiecesOutsideClip` 已收口到 `MenuDraw.QuadRectPx`（**乙式**）⇒ **两者不再同口径**。
            //   ⚠️ 本窗那棵树的缩放恒为 1（下面两条理由）⇒ **今天甲乙两式逐位相同、读数不变**；
            //   本处这一份**不在 `A1003` 的收口清单里**（判据只列了 6 份宿主函数 + `ShellScene` 那三处）⇒ 如实记一笔。
            //   它假设窗口那棵树**没有缩放** —— `WindowsManager.AttachToAnchor` 把窗口摆成 `localScale = one`
            //   （`Shell/WindowsManager.cs` 的 `AttachToAnchor`，`localScale = Vector3.one` 在 `CardPresentation/Shell/WindowsManager.cs`）。
            //   🔴 **2026-10-12 就地订正（铁律 5 · A430）**：这一段原来还写着「`extraScaleSmallScreen` 在我们这套里
            //   **没有消费者**」+「小屏缩放器**没实现**」（引的 `:292-300` / `:76-82` 两处行号也已漂）——
            //   **两条都不成立了**：A165（2026-10-06）把缩放器做了出来（`Shell/TransformScalerBySmallScreenUI.cs`；
            //   开关 = 同文件的 `SmallScreenUI`，**出厂关**），它的消费者 = `GameWindow.ApplySmallScreenScale()`
            //   （`Shell/WindowsManager.cs` 的 `GameWindow.ApplySmallScreenScale`，由 `OpenByState` 在 `CardPresentation/Shell/WindowsManager.cs` 调 —— 就是「播音→激活→`Open()`」之后紧接的那句）。
            //   ✅ **本条断言的结论照旧成立**（本窗那棵树缩放恒为 1），只是**理由换了**（两条各自独立成立）：
            //   ① 本窗 `extraScaleSmallScreen = 1.0`（`Shell/PromptPopup.cs` 里 `win.extraScaleSmallScreen = 1f;` 那一句）= 原版「**不覆盖**」语义 ⇒
            //      `ApplySmallScreenScale` 走 `Initialize()` 那一支（`Shell/WindowsManager.cs` 的 `GameWindow.ApplySmallScreenScale` 里 `Initialize()` 那一支）；
            //   ② 本窗是**代码建**的 GO（`Shell/PromptPopup.cs` 的 `Create`），根上**没有**烤 `menuScale` 的缩放器 ⇒
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
                    float lx = float.MaxValue, ty = float.MaxValue, rx = float.MinValue, by = float.MinValue;
                    for (int i = 0; i < bgQuads.Length; i++)
                    {
                        var q = bgQuads[i];
                        if (q == null || !q.gameObject.activeInHierarchy) continue;
                        // 🔴 **2026-10-20（`A1350` 第二批）换口**：原来这里自己写甲式
                        //   （`LayoutSpace.ToPixel(q.transform.position)` + `WorldW/H × K`）⇒ 改走 `QuadPxRect`
                        //   （= `MenuDraw.QuadRectPx`，**乙式**设计帧，本文件唯一 quad 矩形读口）。
                        //   本窗那棵树缩放恒为 1（理由见本节上面那一段）⇒ 甲乙两式逐位相同、读数不变。
                        if (!QuadPxRect(q, out float qx1, out float qy1, out float qx2, out float qy2)) continue;
                        lx = Mathf.Min(lx, qx1); rx = Mathf.Max(rx, qx2);
                        ty = Mathf.Min(ty, qy1); by = Mathf.Max(by, qy2);
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

            // 🔴 **2026-10-19（`N117` 顺手）**：压暗层的**吃射线命中区**（`MenuDraw.Absorb`）。
            //   判据（原版 prefab + 反编译逐句）：`GenericPromptWindow/Menu Dark Background` 上挂着
            //   `BackgroundCloseButton`（`MonoBehaviour_627461394584887679.json`），但它的 `window = {0,0}`、
            //   `onClick.m_Calls = []`；而 `PromptPopup` 类**没有**任何 `backgroundCloseButton` 字段
            //   （`dump.cs:87347-87368`）、**没有 `Start`/`OnEnable`**，`PromptPopup__Open.c` 只给
            //   `okButton`/`cancelButton` 挂监听 ⇒ **原版点这一下什么都不发生**（不关窗），
            //   那块 `Image` 的 `m_RaycastTarget = 1` ⇒ 语义 = **吸收**。
            //   ⚠️ 改坏法：把它换回「无命中区」⇒ ①②红；换成 `ShadeHit`（关窗）⇒ ②③红。
            {
                var ppHit = pp.ShadeHitNode;
                CheckTrue(ppHit != null, "★ 压暗层命中区 `AbsorbHit` 建出来了（删掉 `Build()` 那句 `MenuDraw.Absorb` ⇒ 红）");
                CheckTrue(MenuDraw.WasAbsorb(ppHit),
                          "★ 它是**吸收**层（`MenuDraw.Absorb`），**不是** `ShadeHit`"
                          + "（原版这颗 `BackgroundCloseButton` 没接线 ⇒ 点暗处**不关窗**）");
                var ppHitQ = ppHit != null ? ppHit.GetComponentInChildren<ImageQuad>(true) : null;
                var ppShadeQ = shade != null ? shade.GetComponentInChildren<ImageQuad>(true) : null;
                CheckTrue(ppHitQ != null && ppShadeQ != null && ppShadeQ.RenderQueue < ppHitQ.RenderQueue,
                          "★ 命中区档 **严格高于**视觉压暗层档（同档会退化成枚举顺序）");
                CheckTrue(ppHitQ != null && okQ != null && ppHitQ.RenderQueue < okQ.RenderQueue,
                          "★ 命中区档 **严格低于**本窗内容命中区（按钮那颗）—— 钮仍然先吃到");
                var ppWb = ppHit != null ? ppHit.GetComponent<WindowButton>() : null;
                CheckTrue(ppWb != null && ppWb.absorbOnly, "★ `absorbOnly` 置上了（零悬停色偏、零「没绑动作」告警）");
                string ppWhy;
                CheckTrue(!MenuDraw.AbsorbTierWarned(ppHit, out ppWhy),
                          "★ 档合法（`qShade < 档 < 内容档`）" + (string.IsNullOrEmpty(ppWhy) ? "" : "：" + ppWhy));
                if (ppHit != null && PointerLayer.Instance != null)
                {
                    var before = pp.CurrentState;
                    var at5 = PointerLayer.Instance.ButtonAt(5f, 5f);
                    CheckTrue(at5 != null && MenuDraw.WasAbsorb(at5.transform)
                              && at5.transform.IsChildOf(pp.transform),
                              "★ 这一点命中的**就是本窗压暗层那颗吸收区**（不是别的窗/别的钮）");
                    CheckTrue(PointerLayer.Instance.ClickAt(5f, 5f),
                              "★ 这一点真点得下去（命中区确实盖住它 —— 改成「无命中区」⇒ 这条红）");
                    Check(before, WindowState.Open, "（前提）点之前窗是开着的");
                    Check(pp.CurrentState, WindowState.Open,
                          "★ 点压暗层 ⇒ **窗仍然开着**（原版这一颗不关窗；改坏法：换成 `ShadeHit(…, Close)` ⇒ 红）");
                }
            }
        }
        Shoot("04_原版提示窗.png");
        shell.Windows.CloseAllWindows();
        Check(shell.Windows.openWindows.Count, 0, "`CloseAllWindows()` 清空");
        CheckTrue(shell.Windows.currentWindow == null, "`currentWindow` 也清空（不留悬挂引用）");
        Object.DestroyImmediate(winGo);

        // ---------------- ⑤·N117 `DailyRewardPopup` 的压暗层命中区（🔴 2026-10-19 `N117` 顺手）
        // 🔴 **判据（原版 prefab 逐字段实读）**：`Daily Reward Popup/Menu Dark Background` 的组件只有
        //    `RectTransform` + `CanvasRenderer` + `Image`（`MonoBehaviour_6390879965494403023` = `Image`）
        //    —— **没有 `BackgroundCloseButton`**（同族对照：`Daily Streak Popup` 那块压暗层上**有**）；
        //    `DailyRewardPopup` 类也没有 `backgroundCloseButton` 字段（`dump.cs:68853-68868`），
        //    `Start` 只给 `closeButton`（0x90，圆形 X 钮）挂监听 ⇒ **原版点暗处什么都不发生**。
        //    ⇒ 我们接的是**吸收层**（`MenuDraw.Absorb`），⛔ 不是「点背景关窗」的 `ShadeHit`。
        // ⚠️ 改坏法：删掉 `Build()` 那句 `MenuDraw.Absorb` ⇒ ①②红；换成 `ShadeHit(…, Close)` ⇒ ③④红。
        Section("`DailyRewardPopup` 压暗层：吃射线的命中区（原版**不**关窗）");
        {
            var drShade = DailyRewardPopup.Create(shell.Windows);
            shell.Windows.OpenWindow(drShade);
            CheckTrue(drShade != null && drShade.CurrentState == WindowState.Open, "（前提）每日奖励窗开出来了");
            var drHit = drShade != null ? drShade.ShadeHitNode : null;
            CheckTrue(drHit != null, "① 压暗层命中区 `AbsorbHit` 建出来了");
            CheckTrue(MenuDraw.WasAbsorb(drHit),
                      "② 它是**吸收**层（`MenuDraw.Absorb`）—— 原版那块 `Image` 吃射线、但**没有**关窗处理器");
            var drShadeVisual = drShade != null ? FindChildIn(drShade.transform, "Menu Dark Background") : null;
            var drShadeQ = drShadeVisual != null ? drShadeVisual.GetComponentInChildren<ImageQuad>(true) : null;
            var drHitQ = drHit != null ? drHit.GetComponentInChildren<ImageQuad>(true) : null;
            CheckTrue(drHitQ != null && drShadeQ != null && drShadeQ.RenderQueue < drHitQ.RenderQueue,
                      "① 命中区档 **严格高于**视觉压暗层档（同档会退化成枚举顺序）");
            var drWb = drHit != null ? drHit.GetComponent<WindowButton>() : null;
            CheckTrue(drWb != null && drWb.absorbOnly, "② `absorbOnly` 置上了（零悬停色偏、零「没绑动作」告警）");
            string drWhy;
            CheckTrue(!MenuDraw.AbsorbTierWarned(drHit, out drWhy),
                      "② 档合法（`qShade < 档 < 内容命中区档`）" + (string.IsNullOrEmpty(drWhy) ? "" : "：" + drWhy));
            if (drHit != null && PointerLayer.Instance != null)
            {
                var at5 = PointerLayer.Instance.ButtonAt(5f, 5f);
                CheckTrue(at5 != null && MenuDraw.WasAbsorb(at5.transform)
                          && at5.transform.IsChildOf(drShade.transform),
                          "③ 这一点命中的**就是本窗压暗层那颗吸收区**（不是别的窗/别的钮）");
                CheckTrue(PointerLayer.Instance.ClickAt(5f, 5f),
                          "③ 这一点真点得下去（命中区确实盖住它 —— 改成「无命中区」⇒ 这条红）");
                Check(drShade.CurrentState, WindowState.Open,
                      "③ 点压暗层 ⇒ **窗仍然开着**（原版这一颗不关窗；改坏法：换成 `ShadeHit(…, Close)` ⇒ 红）");
            }
            if (drShade != null)
            {
                drShade.Close();
                Check(drShade.CurrentState, WindowState.Closed, "③（收尾）`Close()` 真把它关了");
                MainMenuSubmenuWindow.DestroySafe(drShade.gameObject);
            }
        }

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
        // 🔴 **2026-10-20（W58-S4FIX）**：`ON_TEXT_CHANGED` **发不发**由 TMP 自己的 awake/active 前提决定
        //   （`TextMeshPro.cs:2119` 第一句静默早退），批处理**不保证**（判据全文 → 本节 `n0` 那一段注释）。
        //   ⇒ 本节的判据改成「TMP 播报一次 ⇒ 守卫重裁一次」（显式播报 `TMPro_EventManager.ON_TEXT_CHANGED`），
        //   ⛔ 别再写成「调一次 `ForceMeshUpdate` 就该涨」。
        Section("共用件：文字裁切扛得住之后的重排（A38② —— 订 TMP 的 `TEXT_CHANGED_EVENT`）");
        {
            // 🔴 **2026-10-16（A799 · 夹具现核）：本节（含 ⑤·d-3 那两处）这 3 个 `MenuDraw.Text` 探针
            //   【不会】新裁 —— ⛔ 别把它们当成「漏补的 A799 站点」。** R2 全量表逐条解过父链
            //   （→ `资料/普查产出_1015/R2_A799全量表.md` §二 的三行 `Editor/ShellScene.cs` 的 `R2_A799` ——
            //   那是 R2 的读数，行号已往下漂；**锚点 = 三个 `new GameObject("ClipTextProbe" /
            //   "ReclipAfterPlaceProbe" / "ZeroSoftTextProbe")`**）⇒ 三处全判 **不会**；本写手现核同结论：
            //   三处的第一实参恒是**独立根** `new GameObject(...)` 的 `transform`（各自 `DestroyImmediate` 收尾、
            //   不挂任何场景子树）⇒ 父链止于探针自己、**一颗 `ViewportClip` 都没有** ⇒ A781 的那一刀落
            //   `Resolve` 第 3 支 ⇒ 这些探针**逐位不变**（它们量的本来就是 `ClipText` / 两条重裁路本身，与父链无关）。
            var txtGo = new GameObject("ClipTextProbe");
            var live = new PxRect(0f, 0f, 120f, 40f);          // 只有 120px 宽，下面那句字必然越界
            var lb = MenuDraw.Text(txtGo.transform, live, "WWWW WWWW WWWW WWWW WWWW", Color.white, "Probe", 30f, 3000);
            CheckTrue(lb != null, "文字探针建出来了");
            if (lb != null)
            {
                bool moved = MenuDraw.ClipText(lb, live, Vector2.zero);
                CheckTrue(moved, "`ClipText` 当场**真的切了**（否则下面那条等于没验）");
                var guard = lb.GetComponent<ClippedTextGuard>();
                CheckTrue(guard != null,
                          "★ 裁过的字上**挂着 `ClippedTextGuard`**（= 之后每一次重排都会自动重裁）");
                var tmp = lb.GetComponentInChildren<TMPro.TextMeshPro>();
                CheckTrue(tmp != null, "这一段字走的是 TMP 那条后端（守卫生效的那条）");
                if (tmp != null)
                {
                    // 🔴 **2026-10-20（W58-S4FIX）：判据改成「TMP **播报**一次重排 ⇒ 守卫重裁一次」，不再指望
                    //   `ForceMeshUpdate()` 自己发那一条。** 两路证据（实测 + 源码）：
                    //
                    //   ▸ **实测**（`ShellScene.Run` 2026-10-20 两趟，探针读数见
                    //     `普查产出_1020/W58_自检线_S4自动重裁.md`）：
                    //     在本 TMP 上量到 **`entry=0`** —— 而且**整条 `TEXT_CHANGED_EVENT` 一个订阅者都没被调到**
                    //     （「其它对象 0」）⇒ **事件真的没发**（ⓐ），不是「发了但这一刀是空操作」（ⓑ，
                    //     `wouldClip=True` 反而说明这一刀本来就有得可裁），也不是「残留守卫把 `FastAction`
                    //     链表截断」（ⓒ：`staleOn=0`，且第二趟仍然 `+0` —— ⓒ 的实证是「第二趟能收到」）。
                    //
                    //   ▸ **源码**：`ON_TEXT_CHANGED` 的发出点只有 TMP 自己的 `GenerateTextMesh()` 里那三处
                    //     （`Library/PackageCache/com.unity.ugui@…/Runtime/TMP/TextMeshPro.cs:2210 / :4169 / :5063`），
                    //     而它**唯一入口** `OnPreRenderObject()` 的第一句是**静默早退**：
                    //       `if (!m_isAwake || (this.IsActive() == false && m_ignoreActiveState == false)) return;`（`:2119`）
                    //     ⇒ **发不发**取决于 **TMP 自己的 awake/active 前提**，而 `-batchmode -executeMethod`
                    //     （编辑模式、没有帧循环）**不保证**那个前提 —— 这在本仓自己就是写死的口径：
                    //     `Shell/ClippedTextGuard.cs:40`「批处理里没有帧循环 ⇒ **没有重排事件**：自检别拿它自证」
                    //     · `Shell/MenuDraw.cs` 的 `ArmTextGuard` 同一句。（2026-10-18 那条绿 = TMP 当时恰好
                    //     满足前提；10-20 两趟都红 ⇒ **它本来就不可靠**，不是本批引入的回归。）
                    //     ⚠️ 也就是说：**生产侧没有「少发事件」**（这条事件从来不是我们发的），
                    //     而测试也**没有调错重排路径**（`ForceMeshUpdate` 正是 TMP 会发事件的那一条）——
                    //     红的是「**拿 TMP 的内部前提当我们的契约**」。
                    //
                    //   ⇒ **本件真正的契约**（`ClippedTextGuard` 存在的理由）= 「TMP 播报『文字已重排』⇒ 我们重裁一刀」。
                    //     所以这里**真重排一次**（`ForceMeshUpdate`，网格换成未裁的那一代）之后，
                    //     **显式播报 TMP 在正常路径里会发的那一条**（`TMPro_EventManager.ON_TEXT_CHANGED(tmp)`，
                    //     逐字等于 `TextMeshPro.cs:5063` 那一句），再由断言咬住「守卫收到 ⇒ 重裁」。
                    //     ⚠️ 断言仍写 `> n0`（不是 `== n0 + 1`）：TMP 若自己发了，这里会涨 2 次 —— 两条都对。
                    //
                    //   🔴 **牙口（改坏必红）**：删 `ClippedTextGuard.Arm` 里的
                    //     `TEXT_CHANGED_EVENT.Add(OnTextChanged)`（`ClippedTextGuard.cs:60`）⇒ 计数不涨 ⇒ 红；
                    //     删掉整颗守卫 ⇒ 红；把 `OnTextChanged` 里的 `ClipTextNow` 删掉 ⇒ 红。
                    //   ⛔ 别改回「只调 `ForceMeshUpdate` 就指望 TMP 自己发」——那测的是 TMP 的内部前提，不是本件。
                    //   ⚠️ **生产路径不受影响**：真实重排（`Label.SetText` / `SetAutoFitBox` / `ForceRelayout` …）
                    //     除了 TMP 这条事件，还经 `Label.RefreshBounds()` **末句** `guard.Reclip()` 落一刀
                    //     —— 下一节 `TextReclipAfterPlace` 断的就是那一条（今天 0→1，照旧绿）。
                    int n0 = MenuDraw.TextClipReapplied;
                    tmp.ForceMeshUpdate();                          // 真重排（把网格换回未裁的那一代）
                    // 🔴 **2026-10-20（W58-S4FIX 再订正）**：`OnTextChanged` 只认**守卫自己那颗** `_tmp`
                    //   （`obj != _tmp ⇒ return`）；而 `Arm` 那一刻 TMP 子件可能还没建好 ⇒ `_tmp == null`
                    //   ⇒ 用局部 `tmp` 播报会被静默丢掉。这里改用 `guard.TmpForTest`（它会按 `_lb`
                    //   重新解析），并先断「守卫认得出这一颗」——不成立时这条本来就等于没验。
                    var evTmp = guard != null ? guard.TmpForTest : null;
                    CheckTrue(evTmp != null, "（前提）守卫认得出这一颗 TMP（否则事件播报没有人接）");
                    if (evTmp != null) TMPro.TMPro_EventManager.ON_TEXT_CHANGED(evTmp);  // = TMP 重排完之后发的那一条（`TextMeshPro.cs:5063`）
                    CheckTrue(MenuDraw.TextClipReapplied > n0,
                              $"★ 重排之后**自动重裁了一次**（计数 {n0} → {MenuDraw.TextClipReapplied}）"
                              + " —— 拿掉守卫这里就不会涨");
                }
            }
            Object.DestroyImmediate(txtGo);
        }

        // ---------------- 🆕 2026-10-17（A1024③ 第 0 批）：字体面漏斗的口径 ----------------
        //
        // 判据 = `资料/普查产出_1017/UI/R_A1024_字体按用途普查.md` §三「第 0 批」：
        //   给 `MenuDraw.Text` / `TextBox`（经 `TextCore`）/ `GameWindow.Text` / `MainMenuSubmenuWindow.Text|TextBox`
        //   / `HudCanvas.MakeText` 加**可选** `face` 形参，**默认 `Cjk`** ⇒ 旧调用点零行为变化。
        // 本节守三件事（⛔ 不读 `TmpFont.FaceResource` 那张表 = 不自证 —— 期望值写**原版资产名**）：
        //   ① 不传 `face` ⇒ 仍是 `NotoSerifCJK-Regular SDF`（旧调用点逐位不变）；
        //   ② 传了 `face` ⇒ TMP 真的指向原版那一份（`Asar-Regular SDF` / `Pragati-Regular SDF`）；
        //   ③ **汉字闸**：指派了拉丁面、文本里有汉字 ⇒ 实际生效回 CJK（`Label.ActualFace`，不许画方框）。
        // **改坏法**：把 `MenuDraw.TextCore` 里那句 `if (face != TmpFont.Face.Cjk) lb.SetFace(face);` 删掉 ⇒ ② 立刻红；
        //   把 `Label.ActualFace` 的汉字闸去掉 ⇒ ③ 立刻红（且 ① 仍绿 ⇒ 三条分得开）。
        Section("A1024③ 第 0 批：字体面漏斗（默认 Cjk 不变 · 显式 face 生效 · 汉字闸）");
        {
            var faceGo = new GameObject("FaceFunnelProbe");
            var faceR = new PxRect(0f, 0f, 400f, 60f);
            string FontName(Label l)
            {
                var t = l != null ? l.GetComponentInChildren<TMPro.TextMeshPro>() : null;
                return t != null && t.font != null ? t.font.name : "(null)";
            }

            // ① `MenuDraw.Text` 默认档：仍是 CJK 那一份（旧调用点零行为变化）
            var def = MenuDraw.Text(faceGo.transform, faceR, "Default", Color.white, "FaceDefault", 30f, 3000);
            CheckTrue(def != null, "① 默认档探针建出来了");
            if (def != null)
            {
                Check(FontName(def), "NotoSerifCJK-Regular SDF",
                      "★ ① `MenuDraw.Text` **不传 `face`** ⇒ 仍是 CJK 那一份（旧调用点逐位不变）");
                Check(def.Face, TmpFont.Face.Cjk, "① …`Label.Face` 也是 Cjk（默认值没被偷偷改）");
            }

            // ② 显式 `face` ⇒ TMP 真的换到原版那一份
            var ui = MenuDraw.Text(faceGo.transform, faceR, "Latin", Color.white, "FaceUi", 30f, 3000,
                                   face: TmpFont.Face.LatinUi);
            CheckTrue(ui != null, "② 显式 `LatinUi` 探针建出来了");
            if (ui != null)
                Check(FontName(ui), "Asar-Regular SDF",
                      "★ ② `MenuDraw.Text(..., face: LatinUi)` ⇒ TMP 指向**原版 `Asar-Regular SDF`**");

            var body = MenuDraw.TextBox(faceGo.transform, faceR, "Body", Color.white, "FaceBody", 30f,
                                        face: TmpFont.Face.LatinCardBody);
            CheckTrue(body != null, "② `TextBox` 探针建出来了");
            if (body != null)
                Check(FontName(body), "Pragati-Regular SDF",
                      "★ ② `MenuDraw.TextBox(..., face: LatinCardBody)` ⇒ TMP 指向**原版 `Pragati-Regular SDF`**");

            // ① 的另一半：`TextBox` 默认档也没被换掉
            var defBox = MenuDraw.TextBox(faceGo.transform, faceR, "Default body", Color.white, "FaceBodyDefault", 30f);
            CheckTrue(defBox != null, "① `TextBox` 默认档探针建出来了");
            if (defBox != null)
                Check(FontName(defBox), "NotoSerifCJK-Regular SDF",
                      "★ ① `MenuDraw.TextBox` **不传 `face`** ⇒ 仍是 CJK 那一份");

            // ③ 汉字闸：指派拉丁面 + 文本含汉字 ⇒ 实际生效回 CJK（那三份拉丁资产没有汉字字形）
            var cjk = MenuDraw.Text(faceGo.transform, faceR, "汉字", Color.white, "FaceCjkGate", 30f, 3000,
                                    face: TmpFont.Face.LatinUi);
            CheckTrue(cjk != null, "③ 汉字闸探针建出来了");
            if (cjk != null)
            {
                Check(cjk.Face, TmpFont.Face.LatinUi, "③ 指派值仍是 LatinUi（闸在 `ActualFace`，不是改指派）");
                Check(cjk.ActualFace, TmpFont.Face.Cjk,
                      "★ ③ 文本含汉字 ⇒ **实际生效回 CJK**（否则会画一排方框、而且静默）");
                Check(FontName(cjk), "NotoSerifCJK-Regular SDF", "③ …TMP 真的挂回 CJK 那一份");
            }

            Object.DestroyImmediate(faceGo);
        }
        //
        // 🔴 **缺口**（`资料/普查产出_1012/S4_外壳共用件_开账现核.md` §二·2「顺手发现」）：`MenuDraw.TextReclipAfterPlace`
        //    （A225-② 加的计数器，`Shell/MenuDraw.cs` 的 `TextReclipAfterPlace` 声明、`ClippedTextGuard.Reclip()` 自增）
        //    全仓 **0 个读者** ⇒ A206 那条「裁的那一刀要落在**文字真正被画的位置**上」的路**没有直接断言**
        //    （可见面只由 `MainMenuScene.Run` 的「卡组名也吃软边」间接守着）。
        //    ⚠️ **它与上一节（`TextClipReapplied`）是两条路，别混**：
        //      · 上一节 = **事件路**：TMP 的 `ON_TEXT_CHANGED`（只在 `ForceMeshUpdate()` **里面**发）
        //        ⇒ 那一刻 TMP 子节点**还停在旧位置**（A206 的根因）；
        //      · 本节 = **摆完再补那一刀**：`Battle/Label.cs` 的 `RefreshBounds()` **末句** `guard.Reclip()`
        //        （`Battle/Label.cs` 的 `RefreshBounds` 末句），而那是**每一条定版面的路的末句**（`SetText` / `SetAutoFitBox` /
        //        `ForceRelayout` / 建标签…）⇒ 这一刀才落在文字**真正被画**的地方。
        // 🔴 **判据 = 计数器【差分】**（读前 / 读后），⛔ 不拿实现里的期望值当判据（那是自证）。
        //    🔴 **改坏法**：把 `Battle/Label.cs` 的 `RefreshBounds()` 末句那两句（`GetComponent<ClippedTextGuard>()`
        //      + `guard.Reclip()`）删掉 ⇒ **本条立刻红**（计数不涨）；而上一节那条（事件路）**照样绿**
        //      ⇒ 两条分得开（这就是「补一条有牙口的断言」的意思）。
        Section("共用件：`Label` **摆完版面又补了一刀**（A386 —— `TextReclipAfterPlace` 的计数差分）");
        {
            // A799 现核：本探针【不会】新裁（独立根 ⇒ 父链上无 `ViewportClip`）—— 理由见 ⑤·d 第一处探针那段。
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
            // A799 现核：本探针【不会】新裁（独立根 ⇒ 父链上无 `ViewportClip`）—— 理由见 ⑤·d 第一处探针那段。
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

        // ---------------- ⑤·d-5 🆕 **2026-10-18（A990）**：`ClipQuad` 的 x 往返在【非 16:9】下必须闭合
        //
        // 🔴 **判据** = 原版 uGUI 只有【一条】世界↔画布 px 的换算（`RectTransform`）；本工程这一条 =
        //   `LayoutSpace.FromPixel` —— 建件（`MenuDraw.Local` / `RectCenter`）用的就是它 ⇒
        //   「把这个角夹到框沿」夹到的**必须是画面里那条框沿**。
        // 🔴 **病灶**（A990 原文）：写回走 `FromPixel`（x 用**实测** `VisibleWidth`）、读回走 `ToPixel`
        //   （x **写死 108px/世界单位**）⇒ 两条只 **16:9** 重合，别的宽高比下差 `VisibleWidth / DesignWidth` 倍。
        //   算式（`px` = 设计 px · `w` = 世界 x）：
        //     写 `w = (px / 1920 − 0.5) × VisibleWidth` · 读 `px' = 960 + 108 × w`
        //     ⇒ 往返倍率 = `108 × VisibleWidth / 1920` = `VisibleWidth / DesignWidth` = `aspect ÷ (16/9)`
        //     ⇒ 16:9 **1.0000（闭合）** · 4:3 **0.75** · 21:9 **1.3125**
        //   ⇒ 4:3 上「贴着框外左沿的角」会被**放过去**（画到框外）；21:9 上「框内近左沿的角」会被**误夹**。
        // 🔴 **两态**：**非 16:9（4:3 / 21:9）= 旧实现红** · **16:9 = 旧实现也过（= 零回归的对照档）**。
        //   ⚠️ **建 UI 的自检宿主一律在「建树之前」把 `cam.aspect` 钉成 `DesignAspect`** ⇒ 这一条**只能自己把
        //   aspect 摆成非 16:9**，摆完**必须还原**（本场景余下的几何全是按 16:9 建的：位置/尺寸在建的那一刻算一次）。
        // ⚠️ 探针**绕开 `Label`/TMP**（字形落在哪由字体度量定、控不住）：直接调公共件 `MenuDraw.ClipQuad`
        //   喂四个**由 `FromPixel` 造出来**的角 —— 那正是「建件那条换算」造出来的形状。
        // ⚠️ 期望值 = `FromPixel(Clamp(设计 px))`（**建件那一条换算**给的），⛔ **不从被测实现读**（灭自证）。
        Section("★ A990：`ClipQuad` 的 x 往返在非 16:9（4:3 · 21:9）下闭合（判据 = `FromPixel` 的逆）");
        {
            var a990Cam = LayoutSpace.Cam;
            CheckTrue(a990Cam != null, "（前提）`LayoutSpace.Cam` 在（`VisibleWidth` 由它给；不在 ⇒ 本节等于没验）");
            if (a990Cam != null)
            {
                var a990Back = a990Cam.aspect;
                var a990Host = new GameObject("A990 ClipQuad Probe");   // 无父件 ⇒ 局部坐标 = 世界坐标（探针直接喂世界点）
                // 框**故意摆得偏左**（框心 800 < 画布中心 960）：两条换算的偏差正比于「离画布中心有多远」
                var a990Box = new PxRect(600f, 200f, 1000f, 600f);
                // 探针的 x（设计 px）：框外左 40 · **贴着框外左沿的 560** · **框内近左沿的 640** · 框内 950 · 框外右 1400
                //（下面那道 γ 另用 640 / 680）。
                //   ⚠️ 560 与 640 是**有意挑的鉴别点**（推导写在报告里）：4:3 只有 560 那一条会红、
                //   21:9 只有 640/680 那一条会红 —— 两个档各有一处牙口，且都不是「按结果凑出来的数」。
                //   y 一律 260 / 540（**整条落在框的 200..600 里**）⇒ 本节只考 x 那一半（y 本来就闭合）。
                float[] a990T = { 40f, 560f, 640f, 950f, 1400f };
                try
                {
                    foreach (float asp in new[] { 4f / 3f, 21f / 9f, LayoutSpace.DesignAspect })
                    {
                        a990Cam.aspect = asp;
                        float ratio = LayoutSpace.VisibleWidth / LayoutSpace.DesignWidth;
                        bool isDesign = Mathf.Abs(asp - LayoutSpace.DesignAspect) < 1e-4f;
                        if (isDesign)
                            CheckNear(ratio, 1f, 1e-4f,
                                      "（对照档 16:9）`VisibleWidth / DesignWidth` = 1 ⇒ 两条换算在这一档**重合**"
                                      + "（本节余下几条在这一档也必须过 —— 那就是「改动只动非 16:9」的证明）");
                        else
                            CheckTrue(Mathf.Abs(ratio - 1f) > 0.1f,
                                      $"（前提）宽高比 {asp:F4} 下 `VisibleWidth / DesignWidth` = {ratio:F4} —— **偏离 1 超过 10%**"
                                      + "（≈1 就说明这一档已经是 16:9 ⇒ 两条换算重合 ⇒ 本节没有鉴别力）");
                        // 探针**本来就跨在框的两侧**（字面量判的，不经过任何换算）⇒ 下面那条不是「全在框内」的空断言
                        int nOut = 0, nIn = 0;
                        float worst = 0f, worstOld = 0f;
                        foreach (float t in a990T)
                        {
                            if (t < a990Box.x1 || t > a990Box.x2) nOut++; else nIn++;
                            // 角序 **BL · TL · TR · BR**（公共件的口径）：左沿 = t、右沿 = t + 40
                            var p = new Vector3[4];
                            p[0] = LayoutSpace.FromPixel(t, 540f);
                            p[1] = LayoutSpace.FromPixel(t, 260f);
                            p[2] = LayoutSpace.FromPixel(t + 40f, 260f);
                            p[3] = LayoutSpace.FromPixel(t + 40f, 540f);
                            var uv = new Vector2[] { new Vector2(0f, 0f), new Vector2(0f, 1f),
                                                     new Vector2(1f, 1f), new Vector2(1f, 0f) };
                            MenuDraw.ClipQuad(a990Host.transform, p, uv, a990Box, Vector2.zero, null);
                            // 期望值：**夹在设计 px 空间里**，再经建件那条换算换回世界（⛔ 不读被测实现）
                            float wantL = LayoutSpace.FromPixel(Mathf.Clamp(t, a990Box.x1, a990Box.x2), 0f).x;
                            float wantR = LayoutSpace.FromPixel(Mathf.Clamp(t + 40f, a990Box.x1, a990Box.x2), 0f).x;
                            float d = Mathf.Max(Mathf.Max(Mathf.Abs(p[0].x - wantL), Mathf.Abs(p[1].x - wantL)),
                                                Mathf.Max(Mathf.Abs(p[2].x - wantR), Mathf.Abs(p[3].x - wantR)));
                            if (d > worst) worst = d;
                            // 只打数、不判（A990 的牙口有多大）：**旧读法**（`LayoutSpace.ToPixel` 的 x）会把同一个角
                            //   判到哪个世界 x 上。⚠️ 全局把 `ToPixel` 收口成同一条换算的那天这个数会变 0 ——
                            //   那时本节**照旧有效**（期望值走的是 `FromPixel`）⇒ 所以它不该被断死。
                            float wReal = LayoutSpace.FromPixel(t, 0f).x;
                            float oldPx = LayoutSpace.ToPixel(new Vector3(wReal, 0f, 0f)).x;   // 甲式保留：A990 两式对照（W57 §1·4 第 6 条，只打数不判）
                            worstOld = Mathf.Max(worstOld,
                                Mathf.Abs(LayoutSpace.FromPixel(Mathf.Clamp(oldPx, a990Box.x1, a990Box.x2), 0f).x - wantL));
                        }
                        CheckTrue(nOut > 0 && nIn > 0,
                                  $"（前提）宽高比 {asp:F4}：探针里 nOut = {nOut} 个在框外、nIn = {nIn} 个在框内"
                                  + "（两头都要有 —— 只断「夹到框沿」会把「一律缩到框沿」那类实现放过去）");
                        CheckTrue(worst <= 0.002f,
                                  $"★★ A990：宽高比 {asp:F4} 下**每个角都落在 `FromPixel(Clamp(设计 px))` 上**"
                                  + $"（最大偏差 {worst:F5} 世界单位 ≤ 0.002 —— 那 0.002 只是 float 往返的余量："
                                  + "量纲上 0.002 世界单位 ≈ 0.22px）"
                                  + $"｜旧读法在这一档会把某些角判到别处（最大 {worstOld:F4} 世界单位 = 鉴别力，只打数不判）"
                                  + "｜改坏法：把 `ClipQuad` 的读回换回 `LayoutSpace.ToPixel`（或把写回那半边改成常量 108）"
                                  + " ⇒ 非 16:9 两档立刻红、**16:9 那档仍绿**（那正是 A990 的病灶）");
                        // γ：整块在框内（640..680 全在 600..1000 里）⇒ 协议要求**返回 false**（一个字节都不用动）
                        var gp = new Vector3[4];
                        gp[0] = LayoutSpace.FromPixel(640f, 540f);
                        gp[1] = LayoutSpace.FromPixel(640f, 260f);
                        gp[2] = LayoutSpace.FromPixel(680f, 260f);
                        gp[3] = LayoutSpace.FromPixel(680f, 540f);
                        var guv = new Vector2[] { new Vector2(0f, 0f), new Vector2(0f, 1f),
                                                  new Vector2(1f, 1f), new Vector2(1f, 0f) };
                        bool gmoved = MenuDraw.ClipQuad(a990Host.transform, gp, guv, a990Box, Vector2.zero, null);
                        float gw = LayoutSpace.FromPixel(640f, 0f).x;
                        CheckTrue(!gmoved && Mathf.Abs(gp[0].x - gw) <= 0.002f,
                                  $"★ A990：宽高比 {asp:F4} 下**整块在框内的四角一个都不许动**（返回 {gmoved}；"
                                  + $"左沿实得 {gp[0].x:F5} ≈ 期望 {gw:F5}）—— 21:9 上旧读法会把这两个角误判成越界"
                                  + "（读回被放大 1.3125 倍、推出框外）⇒ 返回 true ⇒ 红");
                    }
                }
                finally
                {
                    // ⚠️ **必须还原**（见本节头那条）：余下每一段的几何都是按 16:9 建出来的
                    a990Cam.aspect = a990Back;
                    Object.DestroyImmediate(a990Host);
                }
            }
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
            // ⚠️ **A799 现核：本节这一族（`vpWin.Text` / `vpWin.TextBox`，本节共 8 处）不在 R2 的
            //   199 之内、也【不算新裁】** —— 理由与「A821 那条尾巴」写在 `TextBox` 那一处上面（⛔ 本族是 A821 的地界）。
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

            // ============================================================ B2b 软边削 `colors32.a`（`soft = (0,25)`）
            // 🔴 判据 = `资料/普查产出_1012/H25_ViewportClip守卫.md` §五·**5.2b** 那一行（原文：「给节点
            //   `soft = (0,25)`、把字压在 y 边界上 ⇒ 断言「贴边那两行顶点的 `colors32.a` 被削、
            //   框内不受影响」（判据 = `MenuDraw.ClipTmpMesh` 的软边那一半，⛔ 别只断几何）」）。
            // 🔑 **为什么非有它不可**：`softness` **一个顶点都不挪** —— 它只写 `textInfo.meshInfo[].colors32.a`
            //   （`Shell/MenuDraw.cs` 的 `ClipTmpMesh`：`want = BaseCornerAlpha(ch,k) × al[k]`，而
            //   `al[k]` 来自 `ClipQuad` 里的 `SoftAlpha(到最近那条边的距离 ÷ 带宽)`）。⇒ 把一路上那个
            //   `softPx` 改成 `Vector2.zero`（= 硬边：`SoftAlpha` 第一句就 `return 1f`）**B2/B3/B4/B5
            //   四条几何断言一条都不会红**，只有本节照得出来 —— 这正是 H25 那句「⛔ 别只断几何」。
            // 🔑 **摆位靠实测量、判据靠字面量**：字形落在矩形里的**位置**取决于字体度量（⛔ 不猜它）——
            //   先建一条**父链上没有节点**的对照字，从它自己的网格量出「字形高 + 字形中心相对矩形中心的
            //   偏移」，再用这两个实测量反推三条实验字各自该摆在哪。而**判据本身全是字面量**
            //   （`0f` / `1f` / 「严格夹在两者之间」）—— ⛔ 不在这里重算一遍 `SoftAlpha` 的公式
            //   （那是拿同一个公式验同一个公式 = 自证）。
            // ⚠️ 全节只碰临时件，结尾**整棵销毁**（理由同本节头：节点活着会把后面每一段挂在它父链下的
            //   文字都裁进它的框 —— 静默、且只在后面的段里现形）。
            {
                const string Short = "WWW";                  // 无下行字母、无小写升部 ⇒ 四角只有「上沿/下沿」两行
                const float Fx1 = 100f, Fx2 = 500f;          // 整段落在框的 x 里（`softness.x == 0` ⇒ 与本判据无关）
                const float Fy1 = 300f, Fy2 = 360f;          // 对照字那个矩形（只有**中心**参与定位）
                const float Band = 25f;                      // = 节点 `softness.y`（下面 `Hang` 里那个字面量）
                var softBox = new PxRect(100f, 300f, 500f, 500f);   // 上沿 300 = 软边要压的那条边

                // ① 对照字：`vpRoot` 上**没有** `ViewportClip`（与 B2 的 `lbFree` 同一个父件、同一条理由）
                var lbRaw = vpWin.Text(vpRoot, Short, Fx1, Fx2, Fy1, Fy2, 5, Color.white, "TRawB2b", 60f);
                float wMinX = 0f, wMinY = 0f, wMaxX = 0f, wMaxY = 0f;
                bool rawSpanned = lbRaw != null && TmpSpanPx(lbRaw, out wMinX, out wMinY, out wMaxX, out wMaxY);
                CheckTrue(rawSpanned,
                          "★ A464·B2b：（前提）对照字（**父链上没有节点**）建出来且量得到网格"
                          + "（⛔ 拿不到 ⇒ 本节余下几条等于没验；`TmpSpanPx` 直读 `textInfo.meshInfo[..].vertices`）");
                float gH = rawSpanned ? wMaxY - wMinY : 0f;                                  // 字形高（实测量）
                float gOff = rawSpanned ? (wMinY + wMaxY) * 0.5f - (Fy1 + Fy2) * 0.5f : 0f;  // 字形中心 − 矩形中心

                // ② 基准：对照字**没被任何裁切** ⇒ 四角 alpha 必须还是满值 `1f`。没有它，「实验组小下去」
                //   就没有比较对象（它同时证明 `colors32` 读得到 —— 取不到时那个读数是 −1）。
                var pxs = new List<Vector2>(); var als = new List<float>();
                int nRaw = lbRaw != null ? TmpVertsAndAlpha(lbRaw, pxs, als) : -1;
                bool rawFull = nRaw > 0;
                for (int i = 0; i < nRaw; i++) if (als[i] != 1f) rawFull = false;
                CheckTrue(rawFull,
                          $"★ A464·B2b：（基准）**没有节点**的对照字四角 alpha 全是满值 `1f`（读到 {nRaw} 个顶点"
                          + "；**−1 / 0 个** = `colors32` 取不到 ⇒ 本节余下几条都等于没验）");
                CheckTrue(gH > 20f,
                          $"★ A464·B2b：（前提）字形高 {gH:F1}px > 20px —— 否则「上沿那两角被夹到边界、"
                          + "下沿那两角还留在渐隐带里」这个形状不成立（两条判据会读到同一批角）");
                CheckTrue(rawSpanned && wMinX > softBox.x1 + 1f && wMaxX < softBox.x2 - 1f,
                          $"★ A464·B2b：（前提）这一段字**整段落在节点的 x 范围内**（实测 x {wMinX:F1}..{wMaxX:F1} ⊆ "
                          + $"{softBox.x1:F0}..{softBox.x2:F0}）—— 本判据只看 y 那一半（`softness.x == 0`）");

                // ③ 软边节点：`softness = (0, 25)`（x 向 0 = 硬边 · y 向 25px 带宽）
                var vcSoft = rawSpanned && rawFull && gH > 20f
                             ? ViewportClip.Hang(vpRoot, "ViewportSoft", softBox, Vector4.zero, new Vector2Int(0, 25))
                             : null;
                CheckTrue(vcSoft != null, "★ A464·B2b：（前提）软边视口节点建出来了（`softness = (0,25)`）");

                // ④ 三条实验字：**同一个节点**、同一段字、同一个 x ⇒ 唯一的变量是「字形离上沿有多深」。
                //   矩形 y 中心 = 想要的落点 − 字形高的一半 − 偏移（全是 ① 里的实测量，⛔ 不是估的）。
                Label lbIn = null, lbE1 = null, lbE2 = null;
                if (vcSoft != null)
                {
                    float cIn = 400f - gOff;                            // 字形中心落在框心 400 ⇒ 离上下两条边都 ≫ 带宽
                    float cE1 = softBox.y1 + 6f - gH * 0.5f - gOff;     // 下沿落在带内 **6px** 处
                    float cE2 = softBox.y1 + 18f - gH * 0.5f - gOff;    // 下沿落在带内 **18px** 处（比上一条深 12px）
                    // 🔴 **2026-10-15 订正（只读诊断 D · 铁律 5）**：下面三行的四个矩形实参**原来传错了顺序** ——
                    //   签名是 `Text(parent, text, x1, x2, y1, y2, …)`（`Shell/MenuWindowBase.cs` 的 `parent`），
                    //   而原写 `(Fx1, c∓60f, Fx2, c±60f)`（第 2、3 个实参互换）⇒ 三条实验字被摆到**不是**夹具算出来的落点上
                    //   （y 整体下移 78~138px、x 被挤到左边）⇒ B2b 那 6 条全红（**δ 夹具前提不成立、非实现缺陷**）。
                    //   同文件传对的先例：`本文件` / `本文件` / `本文件`。
                    lbIn = vpWin.Text(vcSoft.transform, Short, Fx1, Fx2, cIn - 60f, cIn + 60f, 5, Color.white, "TInB2b", 60f);
                    lbE1 = vpWin.Text(vcSoft.transform, Short, Fx1, Fx2, cE1 - 60f, cE1 + 60f, 5, Color.white, "TE1B2b", 60f);
                    lbE2 = vpWin.Text(vcSoft.transform, Short, Fx1, Fx2, cE2 - 60f, cE2 + 60f, 5, Color.white, "TE2B2b", 60f);
                }
                CheckTrue(lbIn != null && lbE1 != null && lbE2 != null,
                          "★ A464·B2b：（前提）三条实验字建出来了（同一个软边节点下的同形字 ⇒ 只差摆位）");
                if (lbIn != null && lbE1 != null && lbE2 != null)
                {
                    var p2 = new List<Vector2>(); var a1 = new List<float>();
                    var a2 = new List<float>(); var aIn = new List<float>();
                    int n1 = TmpVertsAndAlpha(lbE1, p2, a1);
                    int n2 = TmpVertsAndAlpha(lbE2, p2, a2);
                    int nIn = TmpVertsAndAlpha(lbIn, p2, aIn);

                    // （前提）贴边那条真**压在上沿上**：上沿那两角被夹到边界 ⇒ 像素 y 停在 300 上
                    float e1MinX = 0f, e1MinY = 0f, e1MaxX = 0f, e1MaxY = 0f;
                    bool e1Spanned = TmpSpanPx(lbE1, out e1MinX, out e1MinY, out e1MaxX, out e1MaxY);
                    CheckTrue(n1 > 0 && e1Spanned
                              && e1MinY <= softBox.y1 + 0.6f && e1MaxY > softBox.y1 + 1f,
                              $"★ A464·B2b：（前提）那条字**真压在上沿上**（实测 y {e1MinY:F2}..{e1MaxY:F2}，"
                              + $"上沿 {softBox.y1:F0}）：上沿那两角夹到边界、下沿那两角还留在带宽 {Band:F0}px 里");
                    float t1Min, t1Max, b1Min, b1Max;
                    bool okT1 = RowAlphaRange(a1, 1, out t1Min, out t1Max);
                    bool okB1 = RowAlphaRange(a1, 0, out b1Min, out b1Max);
                    CheckTrue(okT1 && okB1, "★ A464·B2b：（前提）贴边那条的**上沿 / 下沿两行**都读得到顶点");
                    // ★★ 贴边那一行：夹在边界上 ⇒ 该处 `SoftAlpha` = 0（= 原版掩码把框外当全透明）
                    CheckTrue(okT1 && t1Max <= 0f,
                              $"★★ A464·B2b：**贴边（压在节点上沿上）那一行顶点的 `colors32.a` 被削到 0**"
                              + $"（实测 max {t1Max:F3}）—— ⛔ 硬边实现（`softPx` 恒 `zero` ⇒ `SoftAlpha` 恒返回 1）"
                              + "在这里留下的是 **1**：字形被压扁贴在边界上、**全不透明**（= A38 之前那一版行为）"
                              + "⇒ 本条红，而 B2/B3/B4/B5 那四条几何断言照旧全绿");
                    // ★★ 带内那一行：**严格被削**（既不是「不削」= 1，也不是「一律清 0」= 0）
                    CheckTrue(okB1 && b1Min > 0f && b1Max < 1f,
                              $"★★ A464·B2b：**落在渐隐带里的那一行被按距离削**（实测 {b1Min:F3}..{b1Max:F3}，"
                              + $"严格夹在 0 与 1 之间；下沿落在带内 6px ÷ 带宽 {Band:F0}px ⇒ 线性斜坡）"
                              + "｜硬边实现在这里是 **1**、而「整段一律清 0」的实现是 **0** ⇒ 两种都红");

                    // ★★ **灭自证**：同一个节点下只把字形往框里挪 12px ⇒ 带内那一行整个更亮
                    float b2Min, b2Max;
                    bool okB2 = RowAlphaRange(a2, 0, out b2Min, out b2Max);
                    CheckTrue(n2 > 0 && okB2, "★ A464·B2b：（前提）第二条（更深 12px）的下沿那一行读得到");
                    CheckTrue(okB1 && okB2 && b2Min > b1Max,
                              "★★ A464·B2b（**灭自证** · 两态只差一个字形的落点）：**同一个节点**下的两条同形字，"
                              + $"唯一变量 = 字形落点差 12px ⇒ 带内那一行**整个更亮**（浅的 {b1Min:F3}..{b1Max:F3} < "
                              + $"深的 {b2Min:F3}..{b2Max:F3}）—— 这条钉的是「alpha 是【到边有多远】的函数」："
                              + "把结果写成一个**常数**（0.5、或「凡在带内一律削成某值」）的实现两态读数分不开 ⇒ 红");

                    // ★★ 另一半（H25 那句「框内不受影响」）：离上下两条边都 ≫ 带宽的那些字**一个字节都不许削**
                    float iMinX, iMinY, iMaxX, iMaxY;
                    bool inMoved = TmpSpanPx(lbIn, out iMinX, out iMinY, out iMaxX, out iMaxY);
                    CheckTrue(nIn > 0 && inMoved
                              && iMinY > softBox.y1 + Band + 1f && iMaxY < softBox.y2 - Band - 1f,
                              $"★ A464·B2b：（前提）「框内」那条真离**上下两条边都 > 带宽**"
                              + $"（实测 y {iMinY:F1}..{iMaxY:F1}；框 {softBox.y1:F0}..{softBox.y2:F0} · 带宽 {Band:F0}）"
                              + " —— 没有它，「框内不受影响」可能只是「它本来就在带外」");
                    float iTMin, iTMax, iBMin, iBMax;
                    bool okTI = RowAlphaRange(aIn, 1, out iTMin, out iTMax);
                    bool okBI = RowAlphaRange(aIn, 0, out iBMin, out iBMax);
                    CheckTrue(okTI && okBI && iTMin >= 1f && iBMin >= 1f,
                              $"★★ A464·B2b：**框内（离两条边都 ≫ 带宽）那一档不受影响**（上 {iTMin:F3}.. 下 {iBMin:F3}，"
                              + "期望都是 1）—— 与上面两条成对：少了它，「被削」可能只是「削一切」；"
                              + "而只留它一条，就是「硬边照样全 1」那种**恒真**的弱断言");
                }
                // ⚠️ **必须销毁**（理由见本节头）：节点与四条实验字/对照字都是临时件
                if (vcSoft != null) Object.DestroyImmediate(vcSoft.gameObject);
                if (lbRaw != null) Object.DestroyImmediate(lbRaw.gameObject);
            }

            // ⚠️ `TextBox` 是**另一条**建字的路（`MainMenuSubmenuWindow.TextBox`）—— **必须各一条**，
            //    只断 `Text` 会漏掉整整一族（H25 §三 那行「只改 `Text` 不改 `TextBox`」）。
            //    ⚠️ 它**限宽换行**（`SetWrapWidth(r.W)` = 1000）⇒ 字块的宽度与 `Text` 那条**不是一回事**
            //       ⇒ 控制组**也得各来一份**（否则「全在框内」可能只是「换行之后本来就没到框边」）。
            // ⚠️ **A799 现核：本族（`vpWin.Text` / `vpWin.TextBox`，本节共 8 处 —— 从 B2 那一段起）【不算「新裁」】——** 收口是 `MenuWindowBase.Text/TextBox`，
            //   而本口自己就先 `ViewportClip.Resolve` + `MenuDraw.ClipText`（A435① 2026-10-12，**早于** A781）
            //   ⇒ 它们**早就裁了**（判据 = `Shell/MenuWindowBase.cs` 的 `TextBox` 体 + R2 §三·3 的「已裁 ⇒ 幂等」
            //   那一档）；R2 的 199 是全量 `MenuDraw.*` ⇒ **收不到这一族**（所以账上从来没有它们）。
            //   ⚠️ 但 A781 之后内层 `MenuDraw.TextBox` 会**再裁一刀**（同框幂等）= 另一条账 **A821**，⛔ 不归本账。
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

            // ---- ⑤·b🆕 **A1709**：`Close()` 的收尾**次序** —— 「物体置灰」必须排在「摘表 / `ShowPreviousWindow`」之前
            //
            // 🔴 **判据（原版，第一权威）** = `d:/2/tools/decomp_full/`：
            //   · `GameWindow__Close.c`（Slot 8）：自己只做守卫 + 播关窗音，然后把活交给 `WindowsManager.CloseWindow`；
            //   · `WindowsManager__CloseWindowCO.c`：**先**调 Slot 9 `Hide()`（`0x1c8`；`Hide()` 逐句
            //     = `GameWindow__Hide.c`：守卫 → `state = Closed` → **`SetActive(false)`**）**之后才**
            //     `List.Remove` + `ShowPreviousWindow()`。
            //   ⇒ 原版「置灰」在**前**；我们原来写在 `NotifyClosed` **之后**（= A1709 的根因）。
            //
            // 🔴 **为什么这一条非有不可** —— 🔴 **2026-10-14（`A1714`）就地订正（铁律 5）：下面这半的【理由】已作废，作痕留本行**：
            //   原文（留痕）：「关掉列表里**最后一扇**时 `ShowPreviousWindow` 会走「列表空了」那一支
            //   （`Shell/WindowsManager.cs` = 原版 `CurrentWindow = baseMenu` + `OpenWindow(baseMenu)`）；
            //   而当**被关的正是 `baseMenu` 自己**时（`CloseAllWindows()` 里它下标 0、**最后**被关到），
            //   那一支把 `this` **重新 `SetActive(true)` + `state = Open`** ⇒ 老尾句再把它摁灰 ⇒
            //   `(state = Open, activeSelf = false)`」。
            //   🔴 **实况（`A1714` 之后）**：补上 `baseMenu` 那一支（**被关的那扇 == `Manager.BaseMenu`
            //   ⇒ 只 `Hide()` 就 `return`**，不 `Remove`、**不 `ShowPreviousWindow`**）之后，
            //   关 `baseMenu` **根本走不到那一支** ⇒ 「被开回来又摁灰」那条链**不存在了**。
            //   **错因** = 我们当时缺那一支（原版 `CloseWindowCO` 里它一直都在；判据逐句 → `WindowsManager.cs` 的 `Close()`）。
            //   ✅ **这一条【仍然成立】的那半**（⛔ 别一起删）：原版**普通窗**那一支确实还是
            //   「先 `Hide()`（= 置灰）→ 再 `List.Remove` → 再 `ShowPreviousWindow`」⇒
            //   **「置灰排在摘表之前」照原版必须有**（本段前两句的判据照旧），只是它今天**不再**为
            //   「`baseMenu` 被开回来又摁灰」背书 —— 因为那件事没有了。
            //   ⚠️ 生产路径上「关最后一扇 ⇒ 触发 `ShowPreviousWindow`」**仍然会发生**
            //   （`MainMenuRuntime.OpenSocial/OpenRewards` 两颗导航钮带 `closeAll: true`），
            //   只是那一刻**被关的不是 `baseMenu`**（是普通窗 ⇒ 走完整记账那一支）。
            //   ⚠️ 本段**用探针自造**这个处境（不改生产数据），所以它不依赖 `MainMenuWindow` 那一套树。
            //
            // ⚠️ 本段**只**摆「常驻主菜单 + 最后一扇」这一格；端到端那一格（开社交窗 ⇒ 主菜单被藏 ⇒ 关社交窗
            //    ⇒ 主菜单回来）在 `Editor/MainMenuScene.cs`（A1629 那两条）里。
            {
                var baseGo = new GameObject("A1709 baseMenu probe");
                var baseW = baseGo.AddComponent<GameWindow>();
                baseW.type = WindowType.Fullscreen;              // 与真那一扇同档（`MainMenuRuntime.cs` 的 `WindowType` 实读 = 0）
                baseW.placement = WindowsPlacement.Canvas;
                WindowsManager.AttachToAnchor(baseW);
                shell.Windows.SetBaseMenu(baseW);                // = 原版 `MainMenuWindow.Initialize` 那两句的前一句
                shell.Windows.OpenWindow(baseW);
                CheckTrue(baseGo.activeSelf && baseW.CurrentState == WindowState.Open && shell.Windows.openWindows.Contains(baseW),
                          "（前提）常驻主菜单登记好了：场上只有它一扇、`Open` + active、在册");

                baseW.Close();                                   // ← 关的就是**最后一扇**，且它**就是 baseMenu**
                // 🔴 **2026-10-14（`A1714`）就地订正（铁律 5）**：下面这两条原来断的是
                //   「关掉常驻主菜单那一拍 ⇒ 它**又被开回来了**」（`activeSelf == true` / `CurrentState == Open`）——
                //   **那钉的是一个错行为**。原版 `WindowsManager__CloseWindowCO.c` 逐句：被关的那扇 == `baseMenu`
                //   （`op_Equality(param_2, *(manager + 0x48))`）走的是**另一支** = 只虚调 Slot 9 `Hide()`
                //   就 **`return`**（**不 `List.Remove`、不 `ShowPreviousWindow`**，也不 `Destroy`）
                //   ⇒ **关 `baseMenu` 本来就不该把它开回来**（我们原来缺那一支，才让「被开回来」看着像对的）。
                //   ⚠️ 新期望值**从原版那一支推出来**（⛔ 不是把期望值改松来迎合实现）：
                //   `Hide()` 逐句 = `GameWindow__Hide.c` = 守卫 → `state = Closed` → `SetActive(false)`。
                CheckTrue(!baseGo.activeSelf,
                          "★ ⑤·b-1（**2026-10-14 `A1714` 订正**）：关掉常驻主菜单那一拍 ⇒ 它**只被 `Hide()` 掉**"
                        + "（`activeSelf == false`）—— **不再被 `ShowPreviousWindow` 开回来**"
                        + "（判据 = 原版 `CloseWindowCO` 的 `param_2 == baseMenu` 支：只虚调 Slot 9 `Hide()` 就 `return`）"
                        + "｜🧨 改坏法：删掉 `GameWindow.Close()` 里那一支 ⇒ 它走完整记账、被 `OpenWindow(baseMenu)` 开回来 ⇒ 本条红");
                Check(baseW.CurrentState, WindowState.Closed,
                      "…而且那一支只做 `Hide()` ⇒ `state` 写的是 `Closed`（= `GameWindow__Hide.c` 的第一句）");
                CheckTrue(baseGo.activeSelf == (baseW.CurrentState == WindowState.Open),
                          "★★ ⑤·b-2 **结构断言（两边不可能一起满足）**：`state` 与 `activeSelf` **必须同向** —— "
                        + "改坏法：把 `GameWindow.Close()` 的尾句 `gameObject.SetActive(false);` 挪回 `NotifyClosed` "
                        + "**之后**（A1709 之前那一版）⇒ 那一句会把**刚被开回来的** `baseMenu` 又摁灰 ⇒ "
                        + "`(Open, inactive)` ⇒ 本行红（⚠️ ⑤·b-1 也红：两条各管一件事，不是同义反复）");
                CheckTrue(shell.Windows.openWindows.Contains(baseW),
                          "…而且它**仍在册**（🔴 **2026-10-14 `A1714` 订正**：本条原写「被开回来就得**在册**"
                        + "（原版那一跳走的是完整 `OpenWindow` ⇒ `openWindows.Add`）」—— **理由换了**："
                        + "`baseMenu` 那一支**不 `List.Remove`** ⇒ 它**从头到尾没被摘掉**。"
                        + "⛔ 断的值一个字没改，改的是**为什么**）；⚠️ 判据 = `CloseWindowCO` 那一支只 `Hide()` 就 `return`");

                shell.Windows.CloseAllWindows();                 // ← A1709 的另一半：收尾**不许**把「开着的」摘成孤儿
                CheckTrue(!baseGo.activeSelf || shell.Windows.openWindows.Contains(baseW),
                          "★★ ⑤·b-3「**开着却不在册**」是非法组合：`CloseAllWindows()` 收尾只许摘**真的已 `Closed`**的"
                        + "（`null` / `Closed`）—— 改坏法：退回**无条件** `openWindows.Clear()` ⇒ 上一步刚被开回来的"
                        + "常驻主菜单（`Open` + active）被摘表 ⇒ 本行红。🔴 后果不是记账难看：`OpenWindow` 的全屏支是"
                        + "`if (openWindows.Count > 0) HideAllWindows();`（`WindowsManager.cs` 照原版）⇒ 表被清空 ⇒ "
                        + "主菜单那一棵**不会再被藏** ⇒ 一直露在新开的全屏窗底下");
                CheckTrue(baseGo.activeSelf == (baseW.CurrentState == WindowState.Open),
                          "★ ⑤·b-4 …而且这一拍之后 `state` / `activeSelf` **仍是同向的**"
                        + "（两条合法结局：`Closed` + 不活 / `Open` + 活 —— 不许出现第三种）");

                shell.Windows.SetBaseMenu(null);                 // 复原：本宿主原来没有 baseMenu（也顺带清表 + 清顶窗）
                Object.DestroyImmediate(baseGo);
            }

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
            //   根因：④（本段上面那句 `OpenWindow(gA)`）按 `Shell/WindowsManager.cs` 的 `OpenWindow` 里那句 `currentWindow.ToBackground()`
            //   （`if (currentWindow != null && currentWindow != win) currentWindow.ToBackground();`）
            //   把**当时的顶窗**（= `gB`）压成了 `Background` ⇒ 直接 `SetActive(false)` 的话，
            //   下面那条前提（`CurrentState == Open`）不成立，`OpenWindow(gB)` 也会命中 **`Background` 支**
            //   （`Shell/WindowsManager.cs` 的 `GameWindow.TryOpen(object)` 里 `Background` 那一支：只 `SetActive(true)` + 写 `state`，**不重建、不出声**）
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
                      + "（`OpenWindow` 会把当时的顶窗压到背景，`Shell/WindowsManager.cs` 的 `OpenWindow` 里那句 `currentWindow.ToBackground()`）—— A497 之前那条红就是它");
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

        // ---------------- ⑤·l 🆕 2026-10-20（`W58-ABSORB`）：吸收层候选点「设计帧 → 指针帧」
        //
        // 🔴 **为什么单开一段**：`MenuDraw.CheckAbsorbRule`（约 32 扇窗共用）里那个候选扫描
        //    （`FindOwnAbsorbPoint`）原来把**设计帧**候选点直接喂 `PointerLayer.ButtonAt`（**指针帧**）。
        //    今天各窗父链缩放 = 1 ⇒ 两帧逐位相同、**零可观测差**；一旦父链被
        //    `TransformScalerBySmallScreenUI` 乘 M（或窗内自带缩放），矩形离画布中心较远的候选
        //    **全部落空** ⇒ `found = false` ⇒ 每扇窗的红都会打在「找不到吸收层」上（潜伏缺陷）。
        //    判据 / 恒等式 / 退化守卫 → `Shell/MenuDraw.cs` 的 `PointerFramePxFromDesign`（**唯一实现**，
        //    `Editor/MenuCheck.PointerFramePxFromDesign` 只转调它）。
        //
        // 🔴 **夹具几何为什么是 (1200,320)-(1400,520)**：设计中心 (1300,420) 与画布中心 (960,540)
        //    的 x 偏移 340 > 半宽 100 × 2 ⇒ 父链 ×2 时**设计点全部落在指针帧命中区之外**
        //    （命中区 x ∈ [1440,1840]），而换算后的中心落在正中 (1640,300) ⇒ 两种实现分得出。
        Section("W58-ABSORB：吸收层候选点『设计帧 → 指针帧』（父链 ×2 夹具）—— `MenuDraw.PointerFramePxFromDesign`");
        {
            var plB = PointerLayer.Instance;
            CheckTrue(plB != null, "前提：场景里有指针层（没有的话下面几条等于没查）");
            // ⛔ 锁着的时候 `HitButton` 第一句就 return null ⇒ 下面几条会红在**错的原因**上
            CheckTrue(!PointerLayer.InputBlocked, "前提：指针层没被黑场 / 遮罩锁住（`InputBlocked == false`）");
            if (plB != null && !PointerLayer.InputBlocked)
            {
                // ① 单位缩放：换算口**逐位**返回入参（= 今天全部调用点的读数；新旧口同值可作精确断言）
                var unitRoot = new GameObject("AbsorbFrameProbeUnit");
                try
                {
                    unitRoot.transform.position = Vector3.zero;
                    var uLeaf = new GameObject("Leaf");
                    uLeaf.transform.SetParent(unitRoot.transform, false);   // 父级 lossyScale = 1
                    var unitDesign = new Vector2(1300f, 420f);              // 下面 ×2 那条的同一设计点
                    var same = MenuDraw.PointerFramePxFromDesign(unitDesign, uLeaf.transform);
                    CheckTrue(same.x == unitDesign.x && same.y == unitDesign.y,
                              "★ ① 单位缩放：换算口**逐位**返回入参（1300 / 420；`k == 1` 快路径，⛔ 不是「差一个浮点尾数」）"
                            + $"（实得 {same.x:F6},{same.y:F6}）");
                }
                finally { Object.DestroyImmediate(unitRoot); }

                // ② 父链 ×2 夹具：吸收层建在一个 `localScale = 2` 的根下
                var frameRoot = new GameObject("AbsorbFrameProbe");
                try
                {
                    frameRoot.transform.position = Vector3.zero;
                    frameRoot.transform.localScale = new Vector3(2f, 2f, 1f);
                    // 吸收层矩形 = 设计帧 (1200,320)-(1400,520)；档取 3999（压过真界面最高档 ~3362，
                    // 免得真界面上恰好有颗钮叠在同一点 —— 与 `CollectionScene` A1350 那批探针同一做法）。
                    var abs2 = MenuDraw.Absorb(frameRoot.transform, "AbsorbHit",
                                               new PxRect(1200f, 320f, 1400f, 520f), 2900, 4000);
                    CheckTrue(abs2 != null && MenuDraw.WasAbsorb(abs2),
                              "（前提）② 夹具的吸收层是公共件 `MenuDraw.Absorb` 建的（自己挂 `WindowButton` = 绕开被测那条路）");
                    var q2 = abs2 != null ? abs2.GetComponentInChildren<ImageQuad>(true) : null;
                    CheckTrue(q2 != null, "（前提）② …它下面有 `ImageQuad`（没有就进不了 `PointerLayer` 的命中表）");
                    if (q2 != null)
                    {
                        var frameDesign = new Vector2(1300f, 420f);   // 设计帧中心 = 原版矩形 (1200..1400)×(320..520) 的中心
                        var owner = q2.transform;                     // 设计矩形所属件（乘的是它**父级**那一级 lossyScale = 2）
                        // 期望值 = C + (design − C)·2 = (960 + (1300−960)×2, 540 + (420−540)×2) = (1640, 300)
                        var p2 = MenuDraw.PointerFramePxFromDesign(frameDesign, owner);
                        CheckNear(p2.x, 1640f, 0.01f,
                                  "★ ② 父链 ×2：换算 x = 画布中心 + (设计点 − 中心)×2 = 960 + 340×2 = **1640**"
                                + "（⛔ 不是整值乘 2：那会得 2600、多偏 960 —— `ToDesignPixel` 以画布中心为不动点）");
                        CheckNear(p2.y, 300f, 0.01f, "★ ② …y = 540 + (420−540)×2 = **300**（逐分量、无统一标量）");
                        // ③ 与【另一条独立的读口】互证：那颗 quad 自己的裸世界位置（`PointerLayer.HitBoxPx`
                        //    的中心项走同一条 `ToDesignPixel(裸世界)`）。
                        var viaQuad = MenuCheck.PointerFramePx(q2.transform);
                        CheckNear(viaQuad.x, p2.x, 0.01f,
                                  "★ ③ 换算口与 `MenuCheck.PointerFramePx(quad)`（裸世界坐标读口）**同帧**：x（容差 0.01）");
                        CheckNear(viaQuad.y, p2.y, 0.01f, "★ ③ …y（容差 0.01）");
                        // ④ 生产扫描（`FindOwnAbsorbPoint`）在 ×2 下找得到、且返回的是**指针帧**的点
                        bool found2 = MenuDraw.FindOwnAbsorbPointForTest(
                            frameRoot.transform, owner, 1200f, 320f, 1400f, 520f,
                            (tx, ty) => plB.ButtonAt(tx, ty), out float fx, out float fy);
                        CheckTrue(found2, "★ ④ `FindOwnAbsorbPoint` 在**父链 ×2** 夹具上**找得到**本窗吸收层"
                                        + "（候选点先换算再喂 `ButtonAt` ⇒ 中心候选 (1640,300) 落在命中区正中）");
                        if (found2)
                        {
                            CheckNear(fx, 1640f, 0.01f,
                                      "★ ④ 返回点 x = **指针帧** 1640（设计帧 1300 会被下一条否掉）");
                            CheckNear(fy, 300f, 0.01f, "★ ④ 返回点 y = **指针帧** 300");
                            var hit2 = plB.ButtonAt(fx, fy);
                            CheckTrue(hit2 != null && hit2.transform.IsChildOf(frameRoot.transform),
                                      "★ ④ `PointerLayer.ButtonAt(返回点)` 命中的就是本夹具那颗吸收层"
                                    + "（走生产命中路，不是另算一遍）"
                                    + "（实得 `" + (hit2 != null ? hit2.name : "<null>") + "`）");
                        }
                        // ⑤ 灭自证（**结构上不可能同时满足**）：本夹具的吸收层在指针帧命中区
                        //    x ∈ [1440,1840]，而设计帧点 (1300,420) 不在其中 ⇒
                        //    · 实现退回「设计点直接喂 `at`」⇒ ④ 的 `found` 变假（中心候选 1300 落空）；
                        //    · 想靠「把 ④ 的期望值也改成 1300」圆回去 ⇒ 那一点 `ButtonAt` 命不中本夹具 ⇒ 这一条红。
                        var bad = plB.ButtonAt(frameDesign.x, frameDesign.y);
                        CheckTrue(bad == null || !bad.transform.IsChildOf(frameRoot.transform),
                                  "★ ⑤ 灭自证：**设计帧点 (1300,420) 直接喂 `ButtonAt`** 命不中本夹具的吸收层"
                                + "（它的指针帧命中区 x ∈ [1440,1840]）⇒ 「实现不换算 + 期望值跟着改成设计点」"
                                + "两边不可能同时绿"
                                + "（实得 `" + (bad != null ? bad.name : "<null>") + "`）");
                    }
                }
                finally { Object.DestroyImmediate(frameRoot); }
            }
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
            // 🆕 U3（N70）：发送钮的悬停 / 按下 —— 判据 = 原版 `ChatPanel > Enter Text > Button`
            //   （`EverguildButton`：`m_Transition = 1 (ColorTint)` · `m_TargetGraphic` = **它自己那颗图标** ·
            //     `m_Colors` 是 UGUI 默认灰四格；逐字段现读 → `python -I 工具/menu_dump.py bundle_menus_assets_all
            //     "ChatPanel" --depth 4` 那一行印 `trans=1`）。
            //   我们这颗命中区是**另建的透明件** ⇒ 必须 `TintOn(图标)`，否则色偏全打在透明 quad 上
            //   （画面上零变化 = 原版 `trans=0` 的观感）。
            var sendNode = chatW.EnterTextNode != null ? chatW.EnterTextNode.Find("Button") : null;
            var sendImgT = sendNode != null ? sendNode.Find("Image") : null;
            var sendHitT = sendNode != null ? sendNode.Find("Hit") : null;
            var sendIcon = sendImgT != null ? sendImgT.GetComponent<ImageQuad>() : null;
            var sendWb = sendHitT != null ? sendHitT.GetComponent<WindowButton>() : null;
            CheckTrue(sendIcon != null && sendWb != null,
                      "（前提）聊天发送钮的**图标**与**命中区 WindowButton** 都在 —— 缺一个 ⇒ 下面三条等于没验");
            if (sendIcon != null && sendWb != null)
            {
                sendWb.Enter();
                CheckNear(sendIcon.Tint.r, WindowButton.HighlightK, 1e-4f,
                          "★ 聊天发送钮悬停 ⇒ 图标色偏 = 原版 `m_HighlightedColor` **0.9607843**"
                        + "；改坏法：不 `TintOn`（色偏打在透明命中件上）⇒ 1.0 ⇒ 红");
                sendWb.Press();
                CheckNear(sendIcon.Tint.r, WindowButton.PressedK, 1e-4f,
                          "★ 按下 ⇒ **0.7843137**（原版 `m_PressedColor`）；改坏法：只接悬停不接按下 ⇒ 0.9608 ⇒ 红");
                sendWb.Release(); sendWb.Exit();
                CheckNear(sendIcon.Tint.r, 1f, 1e-4f,
                          "★ 抬起 + 移开 ⇒ 回 Normal 纯白（灭自证：恒变色 / 只变不还原 ⇒ 红）");
            }

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
        //   （`RenderClip` = `Clip` 按 `ClipPad` 内缩 · 左沿 1010 · 反向右移那位）与 `Editor/RewardsScene.cs` 的 `RenderClip`
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
        //     （原文 → `Shell/WindowButton.cs` 的 `WindowButton.SetSelected` / `BtnState` / `SelectedK`（⚠️ 2026-10-19 订正：原写 `Shell/PromptPopup.cs`）
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
            // 🔴 **2026-10-24（真机 F13）起出厂就落在 `Profile` 页**（`PlayerProfileWindow.DefaultTabIndex = 0`）
            //    —— 原来这里写的是「出厂落 `Title` 页（`DefaultTabIndex = 2`）」，那句已过期。
            //    下面这句 `Click(0)` **保留**：它是幂等的，且让本段不依赖默认值（谁改了默认值都不会把这段搞红）。
            //    ⚠️ F13 的正断言就在下面两条 —— **开窗前什么键都没点过**，断的就是「出厂页」本身。
            Check(prof.CurrentTab, WindowTabType.ProfileInfo,
                  "★ F13 档案窗**新开一扇 ⇒ 落在个人资料页**（`DefaultTabIndex`；真机截图 55/57 原来落在「头衔」页）");
            var titlePage = prof.Page(WindowTabType.ProfileTitle);
            CheckTrue(titlePage != null && !titlePage.gameObject.activeSelf,
                      "★ F13 而且 `Title Tab`（「选择你的头衔」那一整页）**没有被自动打开**"
                    + "（`null` 也算红 —— 那说明页根本没建出来，上面那条会跟着失去意义）");
            prof.tabButtons.Click(0);
            Check(prof.CurrentTab, WindowTabType.ProfileInfo,
                  "前置：切到 `Profile` 页（幂等；不切的话那一页根是关着的，下面量不到东西）");
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
                // 🆕 2026-10-09（`A1125`）：**改名窗那颗关窗钮**的「命中区 + 换图层」四连断 ——
                //   🔴 关窗钮在 `ChooseNameWindow`（**本页的直接子节点**、出厂 `SetActive(false)`）里
                //   ⇒ 这一刻刚 `OpenNameWindow()` 过（上面那条前提），量完由下面那句 `plL.ClickAt` 关回去。
                //   判据（原版亲读）= `ChooseNameWindow > Generic Close Button Green` 那颗 `EverguildButton`
                //   的 `m_TargetGraphic` ⇒ GO = `Icon`、贴图 = `40k_bt_close`；吃射线的只有 `Icon`（56.37×54.50）
                //   按 `(-20)⁴` 外扩 ⇒ **96.37 × 94.50**。
                A1125Close("ProfileTab/ChooseNameWindow", ptab.transform, "Generic Close Button Green", "Hit",
                           "Icon", "40k_bt_close", "Icon", 96.37f, 94.50f);
                // 🆕 **2026-10-18（`A1149` 第二半）**：圆底盘**实绘矩形**两连断。绿族框 **75×75 正方**
                //   ⇒ 第二格恒真（那类窗没有等比不变量可丢）；第一格钉「用的是原版根那一格 **75**，
                //   ⛔ 不是子件框 56.37×54.50」。
                //   🔴 **2026-10-18（`A1149` 第一半 · 第九会话 P7）就地更正**：上面原来写「本窗圆底盘
                //   现读仍挂在自造子件 `Image` 上 … 显式点名子件」——**挂点已归真**
                //   （`Shell/ProfileTab.cs` 那颗 quad 现画在根节点 `Generic Close Button Green` 自己身上）
                //   ⇒ 第 8 实参（子件名）**已删**，走助手「先根后子」的**根那一路**。
                //   ⚠️ 必须摆在下面 `ClickAt` **之前**（那一下把改名窗关掉）。
                A1125CloseBase("ProfileTab/ChooseNameWindow", ptab.transform, "Generic Close Button Green",
                               "UI_Button_Round_background", 75.00f, 75.00f, 75.00f);
                plL.ClickAt(pPage.x, pPage.y);
                CheckTrue(!ptab.NameWindowOpen, "…（顺带：点压暗层真的把改名窗关上了 = `CancelNameWindow` 接在它身上）");
            }
            shell.Windows.CloseAllWindows();
            Object.DestroyImmediate(prof.gameObject);
        }

        // ---------------- ⑤·m 🆕 2026-10-07（A77⑲）：`disabled / soft-disable` 的【变灰】观感（A65②）
        //
        // 🔴 **判据（原版，全文 → `Shell/WindowButton.cs` 的 `WindowButton` 头部那段 + `SoftDisabled` 那一段；⚠️ 2026-10-19 订正：原写 `Shell/PromptPopup.cs`）**：
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

            // 🆕 **2026-10-13（A1719）**：**九宫格按钮的「一组」要认对**（正文 → `Shell/WindowButton.cs`
            //   的 `GrayTargets()` 那一段）。形状：可见底图被切成 9 块放在**一颗节点**上，而 `WindowButton`
            //   挂在【另一颗】透明命中区上、两者**互为兄弟**（真实站点 = `Shell/GenericOptionsPanel.cs`：
            //   `bgo = MenuDraw.Nine(bn, …)` 与 `Hit(bn, "Hit", …)` 都挂在 `bn` 下；`Shell/AllianceMemberOptionsPopup.cs`
            //   同形）。改前 `GrayTargets()` = 「`target` + 本钮子树」⇒ 只看得到那 9 块里的**第 1 块**；
            //   而换图那一路（`BindNine` 记下的 `_nine` → `SwapTo`）认的是**全部 9 块** ⇒ 灰/亮**不对称**。
            //   判据 = 原版 `EverguildButtonHelper.GetGraphicsInChildren(gameObject, includeInactive: 1)`：
            //   **按钮这一组图形件全部**（九宫格在原版是**一颗** `Image`，在我们这儿切成 9 张 ⇒ 同属一组）。
            {
                const int NineN = 9;
                var nRoot = new GameObject("A1719 NineProbe");
                var nBtn = new GameObject("Btn").transform;
                nBtn.SetParent(nRoot.transform, false);
                var nNine = new GameObject("Image");           // 可见底图那一颗（**命中区的兄弟**）
                nNine.transform.SetParent(nBtn, false);
                var nQs = new ImageQuad[NineN];
                int nMade = 0;
                for (int i = 0; i < NineN; i++)
                {
                    nQs[i] = ImageQuad.Create(nNine.transform, CardArt.Solid(), Vector3.zero,
                                              LayoutSpace.Px(20f), new Vector2(0.5f, 0.5f), "p" + i);
                    if (nQs[i] != null) nMade++;
                }
                var nHit = new GameObject("Hit");              // 透明命中区（`WindowButton` 挂在它身上）
                nHit.transform.SetParent(nBtn, false);
                ImageQuad.Create(nHit.transform, CardArt.Solid(), Vector3.zero, LayoutSpace.Px(20f),
                                 new Vector2(0.5f, 0.5f), "Hit");
                var nwb = nHit.AddComponent<WindowButton>();
                // ⚠️ 图名用**真名**（`40K_button` —— 全库 10 处 `BindNine` 里有 3 处在用它）：编一个不存在的
                //    名字会往 `WindowButton.MissingSwapArt` 那条红线表里塞一条（别的宿主断它为空）
                //    ⇒ 这里只制造被测的那一态。
                nwb.BindNine(nNine, "40K_button");
                Check(nMade, NineN, "前置：九宫格那 9 块都建出来了");
                CheckTrue(nwb.HoverTexForTest != null,
                          "前置：`40K_button_hover` 取得到（取不到的话下面「亮那一路认下 9 块」无从谈起）");

                var nOrigTex = new Texture[NineN];
                var nOrigMat = new Material[NineN];
                for (int i = 0; i < NineN; i++)
                {
                    if (nQs[i] == null) continue;
                    nOrigTex[i] = nQs[i].Texture;
                    var mr0 = nQs[i].GetComponent<MeshRenderer>();
                    nOrigMat[i] = mr0 != null ? mr0.sharedMaterial : null;
                }

                // ①【亮】那一路（悬停换图）认下几块 —— `BindNine` → `SwapTo` 逐块换图
                nwb.Enter();
                int brightSeen = 0;
                for (int i = 0; i < NineN; i++)
                    if (nQs[i] != null && nQs[i].Texture != nOrigTex[i]) brightSeen++;
                nwb.Exit();
                Check(brightSeen, NineN, "★ 悬停那一路换的是**全部 9 块**（`BindNine` 的 `_nine`）—— 本条的基准");

                // ②【灰】那一路必须认下**同一组**：改前 `GrayTargets()` 只灰得到第 1 块（= 1，不是 9）
                nwb.Interactable = false;
                int graySeen = 0;
                for (int i = 0; i < NineN; i++)
                    if (nQs[i] != null && ShaderNameOf(nQs[i]) == GreyName) graySeen++;
                Check(graySeen, brightSeen,
                      "★ **变灰认下的是与换图【同一组】图形件**（A1719：改前只认得到 9 块里的第 1 块）");
                Check(graySeen, NineN,
                      "★ …具体就是九宫格 **9 块全灰**（原版 `GetGraphicsInChildren(gameObject, 1)` = 按钮这一组全部）");
                CheckTrue(nwb.GrayedForTest, "★ …而且它自称灰了（`GrayedForTest`）");

                // ③【灭自证】还原那一路必须**逐块**把材质**原样还回去** —— 只断「都灰了」的话，
                //    把 `GrayTargets` 改回只灰一块、再顺手把 `GrayedForTest` 置真也照样绿。
                nwb.Interactable = true;
                int stillGrey = 0, matBack = 0;
                for (int i = 0; i < NineN; i++)
                {
                    if (nQs[i] == null) continue;
                    if (ShaderNameOf(nQs[i]) == GreyName) stillGrey++;
                    var mr1 = nQs[i].GetComponent<MeshRenderer>();
                    if ((mr1 != null ? mr1.sharedMaterial : null) == nOrigMat[i]) matBack++;
                }
                Check(stillGrey, 0, "★ 对照：置回可交互 ⇒ 9 块**逐块**都退出灰化（不是只退出第 1 块）");
                Check(matBack, NineN, "★ …且 9 块各自拿回**原来那一份材质对象**（逐块还原，不是只还原中心那格）");
                Object.DestroyImmediate(nRoot);
            }
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
                //   （`Shell/WindowButton.cs` 的 `WindowButton.onClick`；⚠️ 2026-10-19 订正：原写 `Shell/PromptPopup.cs`）⇒ 解引用不抛；而委托体 `BattleLogTab.OpenPopup`
                //   （`Shell/BattleLogTab.cs` 的 `WindowsManager.OpenByRef`）**一句实例状态都没读**（只有静态的 `WindowsManager.OpenByRef`
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
        // 🆕 2026-10-26（W11AU · 账行 69）：原版这个音源上挂着 `MusicVolumePlayerPrefsSetting`
        //   （`MusicVolumePlayerPrefsSetting__Start.c:16-27`）：`v = PlayerPrefs.GetFloat(key, 源自身音量)`，`volume = 源音量 × v`。
        //   ⇒ 期望值 = `0.65 × 存档量`（默认值是**源自身音量**，不是 1.0 ⇒ 初装 = 0.65×0.65 = 0.4225）。
        //   ⚠️ 期望值里那两个数都来自**原版判据**（0.65 出自 `AudioSource_261`；默认值语义出自 `.c`），不是我们自己编的常量互比。
        float musicPref = PlayerPrefs.GetFloat(WarpforgeAudio.MusicPrefKey, ShellRuntime.IntroVolume);
        CheckNear(shell.IntroAudioVolumeForTest, ShellRuntime.IntroVolume * musicPref, 0.0001f,
                  "开场视频音量 = 0.65 × 音乐存档量（原版 `MusicVolumePlayerPrefsSetting.Start`；没存档时 = 0.4225）");
        CheckNear(ShellRuntime.IntroVolume * ShellRuntime.IntroVolume, 0.4225f, 0.0001f,
                  "…且「初装无 `MusicVolume` 存档」这一档 = 0.65 × 0.65 = **0.4225**（默认值取源自身音量）");

        // ---- 🆕 2026-10-26（W11AU）：音乐**源**音量 vs mixer 侧的 ×0.2 ----
        // 🔴 这两条必须成对看（拆开看任何一条都会得出错结论）：
        //   · `PlayMusic` 的 Instant 档把 `source.volume` **强写 1.0**（`MusicManager__PlayMusic.c:71-78` 读静态表 `[0]`；
        //     原版两个音乐源的序列化 `m_Volume` 也是 1.0）⇒ **源侧没有 0.2**。
        //   · 0.2 是 `MusicManager.musicVolumeMultiplier`（预制体 `MonoBehaviour_-1722128571091716916.json` = 0.2），
        //     原版把它乘进 **mixer 的 `VolumeMusic`**（`PlayerDataManager__SetMusicAndFxVolumes.c` → `SoundManager__SetMixerVolume.c`，dB 换算）
        //     ⇒ 我们这一侧的对应物 = `WarpforgeAudio.MusicVolumeMultiplier`，**必须是 0.2**。
        //   ⇒ 总响度 = 1.0 × (存档量 × 0.2)。⛔ 别把 0.2 乘回源侧（那就是原版的 1/5）；也⛔ 别两边都不乘。
        CheckNear(ShellRuntime.MusicSourceVolume, 1.0f, 0.0001f,
                  "★ 菜单音乐**源**音量 = 1.0（原版 `MusicManager__PlayMusic.c:71-78` Instant 档 / `AudioSourcesController.MAX_VOLUME`；"
                + "改回 0.2 这里就红）");
        CheckNear(WarpforgeAudio.MusicVolumeMultiplier, 0.2f, 0.0001f,
                  "★ 音乐的 ×0.2 在 **mixer 侧**（`WarpforgeAudio.MusicVolumeMultiplier` = 原版 `MusicManager.musicVolumeMultiplier`，"
                + "预制体实证 0.2）—— 源侧已是 1.0，这一格不是 0.2 时**总响度**就与原版不等（少乘 = 响 5 倍）");

        // ---- 🆕 2026-10-26（W11AU · 账行 19·20·21·22·23·27·28·29·30·31）：`AudioSourcesController` 双源 3 s 交叉淡变 ----
        // 判据：`AudioSourcesController__{Blend,DecrementVolume,FadeIn,FadeOut,IncrementVolume,SetMaxVolume,SetMinVolume,Stop,
        // SwitchMainAudioSource,VolumeLerp}.c` + 三个状态机 `._*_d__{14,16,18}__MoveNext.c` + 常量 `__.cctor.c:9-11`。
        // ⚠️ **完全离线跑**：`ResetClockForTest()` 把钟切成手动档、`AdvanceForTest(dt)` 一帧一帧推
        //    （批处理没有帧循环 ⇒ 原版的 `Task`/协程都动不了；机制见 `Core/AudioSourcesController.cs` 文件头）。
        CheckNear(ShellRuntime.MusicQueueLeadSeconds, 3.0f, 0.0001f,
                  "排队下一曲**提前 3 s**（`MusicManager__PlayMusicQueued.c:43-52`；常量 0x1834b2e8c 实读 = 3.0f）");
        {
            var goA = new GameObject("AudioSourcesController 探针 A");
            var goB = new GameObject("AudioSourcesController 探针 B");
            var srcA = goA.AddComponent<AudioSource>();
            var srcB = goB.AddComponent<AudioSource>();
            var ctl = new AudioSourcesController();
            ctl.Init(srcA, srcB);
            CheckTrue(ctl.MainAudioSource == srcA, "`Init()` ⇒ 主源 = **first**（`AudioSourcesController__Init.c:5`）");
            CheckTrue(!srcB.enabled, "`Init()` ⇒ **second 停用**（同文件最后一句 `set_enabled(0)`）");

            ctl.ResetClockForTest();
            ctl.FadeIn(main);
            CheckNear(srcA.volume, 0f, 0.001f,
                      "FadeIn 起播音量 = **0**（`AudioSourcesController__FadeIn.c:31-32` 写静态表[1]）");
            ctl.AdvanceForTest(1.5f);
            CheckNear(srcA.volume, 0.5f, 0.02f,
                      "FadeIn **1.5 s ⇒ 0.5**（`LERP_DURATION` = 3 s 线性；上限常量 0x1834b2bb8 实读 = 1.0f）");
            ctl.AdvanceForTest(1.5f);
            CheckNear(srcA.volume, 1.0f, 0.001f,
                      "FadeIn **3 s ⇒ 1.0**（`._IncrementVolume_d__14__MoveNext.c` 末句 `set_volume(静态表[0])`）");
            Check(ctl.ActiveLerpCount, 0, "FadeIn 的补间 3 s 后**收工**（原版 `Task` 结束，不再写 volume）");

            var blended = ctl.Blend(idle);
            CheckTrue(blended == srcB && ctl.MainAudioSource == srcB,
                      "★ `Blend` ⇒ 主源切到**另一个**源，并返回它（`__Blend.c:14-29`）");
            CheckNear(srcB.volume, 0f, 0.001f, "…新源从 **0** 起播（Blend = FadeOut + 切主源 + FadeIn）");
            ctl.AdvanceForTest(1.5f);
            CheckNear(srcA.volume, 0.5f, 0.02f, "Blend 的**旧源**在淡出（1.5 s ⇒ 0.5）");
            CheckNear(srcB.volume, 0.5f, 0.02f, "Blend 的**新源**在淡入（1.5 s ⇒ 0.5）");
            ctl.AdvanceForTest(1.5f);
            CheckNear(srcA.volume, 0f, 0.001f,
                      "Blend 3 s ⇒ **旧源 = 0**（`._DecrementVolume_d__16__MoveNext.c` 末句）；改坏成硬切这里就红");
            CheckTrue(!srcA.enabled, "…而且旧源 `enabled = false`（同末句）");
            CheckNear(srcB.volume, 1.0f, 0.001f, "Blend 3 s ⇒ **新源 = 1.0**");

            ctl.FadeOut();
            ctl.AdvanceForTest(3f);
            CheckNear(srcB.volume, 0f, 0.001f, "`FadeOut()`（= `MusicManager.StopMusic(instant:false)`）3 s 后当前源 = 0");
            CheckTrue(!srcB.enabled, "…并 `enabled = false`（`AudioSourcesController__FadeOut.c` → Decrement 末句）");
            Object.DestroyImmediate(goA);
            Object.DestroyImmediate(goB);
        }

        // ---- 🆕 2026-10-26（W11AU · 账行 62·63·67）：排队下一曲 **可取消**（原版 `+0x40` 那个句柄）----
        CheckTrue(shell.MusicQueuedForTest,
                  "（前提）`PlayMenuMusic()` 之后 idle 已经**排进队列**（`MainMenuMusicController__Initialize.c` 第二句 `PlayMusicQueued`）");
        CheckTrue(shell.QueuedMusicClipForTest == idle, "…排的就是 `menu_idle_theme`（主主题先播、idle 接着上）");
        shell.CheckQueue();
        CheckTrue(!shell.MusicQueuedForTest,
                  "★ `CheckQueue()` ⇒ 排队的下一曲**取消**（`MusicManager__CheckQueue.c`：`StopCoroutine(+0x40)`；"
                + "改坏成「只置空不取消」或「根本没存句柄」这里就红）");
        shell.PlayMenuMusic();   // 复位：别让后面那几段跑在「没排队」的状态里


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
        //   （`Shell/PracticeModePopup.cs`）。
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

        // ---------------- ⑤·y-3 🆕 **2026-10-17（B11 · A883 / A884）**：导入窗两颗 TMP 的对齐 + 占位符的词条
        //
        // 判据 = **逐字段实读原版 prefab**（⛔ 不是抄 `资料/普查产出_0923/A1_外壳与弹窗.md` §4 那张表 ——
        //   那两行当年**没实读 TMP**，A1 上写着 `hAlign=Right`、是错的）：
        //   `python 工具/menu_dump.py bundle_menus_assets_all "Import Deck Popup" --depth 6`
        //   + 直接读 `bundle_menus_assets_all/MonoBehaviour/` 那几颗 MB（本轮亲读）：
        //     `Main Search message` → `MonoBehaviour_7476776257758560402.json`：`m_HorizontalAlignment = 2` / `m_VerticalAlignment = 512`
        //     `Error msg`           → `MonoBehaviour_-2174597011030277998.json`：`m_HorizontalAlignment = 2` / `m_VerticalAlignment = 512`
        //     `Placeholder`         → `MonoBehaviour_-1702962149292703598.json`：`m_HorizontalAlignment = 1` / `m_VerticalAlignment = 256`（D46 已改，本块不复核）
        //   两套枚举**不是同一套**（`TMP_Text.cs` 的 `HorizontalAlignmentOptions` / `VerticalAlignmentOptions`）：
        //   `Left = 1 · Center = 2 · Right = 4` · `Top = 256 · Middle = 512`。
        //
        // 🔴 **本块断三支，各带改坏法**：
        //   ① **TMP 字段 == 原版那两个字面量**（逐颗 HA / VA 共四条）。
        //      ⚠️ ⛔ **光这一支挡不住「改回右对齐」** —— `Label.AlignRightOn` 只挪**节点位置**、
        //      **不写** TMP 的 `m_HorizontalAlignment`（`Battle/Label.cs` 那两个 `Align*On` 的实读：
        //      体内只有 `transform.localPosition` + `ReclipNow`）⇒ 牙口全在 ②。
        //   ② **判别式（灭自证）**：量**渲出来那一块**的**水平中心**，必须落在**框心 960**。
        //      两处框都是关于屏幕中心对称的（`MsgL/MsgR = 610/1310` · `ErrL/ErrR = 593.05/1326.95`
        //      ⇒ 框心都是 **960**）⇒ 一个数同时管两颗。
        //      **结构上必然红**（下面的差值都是**算出来的**、不是量出来的）：右对齐档把块心摆到 `x2 − 块宽/2`
        //      ⇒ 与框心的差 = `(x2 − 960) − 块宽/2`，`Main Search message` 那边 = `350 − 块宽/2`
        //      ⇒ 只有那句字宽到 **≥ 660px**（框总共才 700 宽）才可能落进 20px 容差；
        //      `Error msg` 那边 = `366.95 − 块宽/2`（框 733.9 宽）—— 本支喂的那条错误文案是
        //      「这不是一条合法的卡组串」（12 个汉字 × fs28 ⇒ **估** ~336px，⚠️ 这个宽度**没实量**）
        //      ⇒ 差 ≈ 199px ⇒ 红。
        //      ⚠️ 量的是 **TMP 顶点**（`TmpSpanPx`），⛔ 不是节点自己的 rect（那等于拿写进去的值验写进去的值）。
        //   ③ 占位符走 `Loc`：**两语档下那句字必须不同** —— 写死英文 / 写死中文都过不了那一条。
        Section("B11 · A883/A884：`ImportDeckPopup` 两颗 TMP 的对齐（原版 HA 2 / VA 512）+ 占位符走 `Loc`");
        {
            var lang0 = Loc.Current;                        // 收尾要放回原值（后面的节可能吃它）
            var ipB = ImportDeckPopup.Create(shell.Windows);
            CheckTrue(ipB != null, "（前提）`Import Deck Popup` 建出来了");
            if (ipB != null)
            {
                // 判据档 = 出厂语言；**只改内存**（`RestoreForTest` 不碰 `PlayerPrefs`）
                Loc.RestoreForTest(AvailableLanguages.Chinese);
                CheckTrue(ipB.TryOpen(null), "（前提）窗开着 —— 下面几条才不是空断");

                // ============================================================ ① TMP 字段 = 原版字面量
                foreach (var nm in new[] { "Main Search message", "Error msg" })
                {
                    var node = FindChildIn(ipB.transform, nm);
                    var t = node != null ? node.GetComponentInChildren<TMPro.TextMeshPro>(true) : null;
                    CheckTrue(t != null, $"（前提）`{nm}` 上那颗 TMP 在（拿不到 ⇒ 它下面两条会假绿）");
                    if (t == null) continue;
                    Check((int)t.horizontalAlignment, 2,
                          $"`{nm}` 的 `m_HorizontalAlignment` = 原版字面量 **2**（Center）");
                    Check((int)t.verticalAlignment, 512,
                          $"`{nm}` 的 `m_VerticalAlignment` = 原版字面量 **512**（Middle）");
                }

                // ============================================================ ② 判别式：渲出来那块的**水平中心** = 框心 960
                {
                    var n = FindChildIn(ipB.transform, "Main Search message");
                    var l = n != null ? n.GetComponentInChildren<Label>(true) : null;
                    CheckTrue(TmpSpanPx(l, out float mnX, out _, out float mxX, out _),
                              "（前提）`Main Search message` 那句字**渲出来了**"
                            + "（量不到顶点 ⇒ 它下面那条会红，⛔ 不会假绿）");
                    CheckNear((mnX + mxX) * 0.5f, 960f, 20f,
                              "★ `Main Search message` 渲出来那一块的**水平中心** = 框心 **960**（原版 `HA=2` Center）"
                            + " —— 改坏法：把 `Align.Center` 换回 `Align.Right` ⇒ `AlignRightOn` 把块心推到 `x2 − 块宽/2`"
                            + "（差 = `350 − 块宽/2`）⇒ 红；⛔ 别只改 TMP 字段想换绿（那一支不受 `AlignRightOn` 影响，见本块头 ①）");
                }

                // ---- `Error msg` 同一条（它平时是空串、没有顶点 ⇒ 先走生产链喂一条**真的**失败串）----
                //   ⛔ 别去戳私有字段：那样验的是夹具、不是产物（`TryImport()` 走的就是玩家点 Confirm 那条路）。
                {
                    ipB.SetTextForTest("B11-not-a-deck");
                    CheckTrue(!ipB.TryImport(), "（前提）喂一条非法卡组串 ⇒ 导入**失败**（错误行才会被画出来）");
                    CheckTrue(!string.IsNullOrEmpty(ipB.ErrorText), "（前提）错误行有字了（`ErrorText` 非空）");
                    var n = FindChildIn(ipB.transform, "Error msg");
                    var l = n != null ? n.GetComponentInChildren<Label>(true) : null;
                    CheckTrue(TmpSpanPx(l, out float mnX, out _, out float mxX, out _),
                              "（前提）`Error msg` 那句字**渲出来了**（量不到顶点 ⇒ 它下面那条会红）");
                    CheckNear((mnX + mxX) * 0.5f, 960f, 20f,
                              "★ `Error msg` 渲出来那一块的**水平中心** = 框心 **960**（同上一支，原版 `HA=2`）"
                            + " —— 这一颗有**两个出生入口**（`Build` 与 `RebuildErrorLine`，⛔ 别按行号找），"
                            + " 本支走的正是 `RebuildErrorLine` 那一个");
                }

                // ============================================================ ③ 占位符走 `Loc`（两语档各断一个字面量）
                System.Func<string> placeholderNow = () =>
                {
                    var n = FindChildIn(ipB.transform, "Input Text");
                    var l = n != null ? n.GetComponentInChildren<Label>(true) : null;
                    return l != null ? l.Text : null;               // 缺件 ⇒ null（下面必红，⛔ 不静默）
                };
                Loc.RestoreForTest(AvailableLanguages.Chinese);
                ipB.Close();                                       // 走生产链回 `Closed` ⇒ `TryOpen` 才会重建（A437）
                CheckTrue(ipB.TryOpen(null), "（前提）换语言之后窗重开了（`Build()` 会重读一次词条）");
                string phZh = placeholderNow();
                Loc.RestoreForTest(AvailableLanguages.English);
                ipB.Close();
                CheckTrue(ipB.TryOpen(null), "（前提）第二趟同上");
                string phEn = placeholderNow();
                // 🔴 2026-10-14（第二十七会话）就地改期望值：MenuDeck/HUD/EnterText 原来是我们自译的「输入文字...」，官方 zh-CN = 「输入文本...」
                Check(phZh, "输入文本...", "★ 中文档的占位符 = `Loc.T(\"MenuDeck/HUD/EnterText\")` 的**中文列**"
                    + "（2026-10-14 官方 zh-CN 表 = 「输入文本...」，原「输入文字...」）");
                Check(phEn, "Enter text...", "★ 英文档的占位符 = `Loc.T(\"MenuDeck/HUD/EnterText\")` 的**英文列**"
                    + "（= 原版那颗 TMP 的 `m_text` 原文逐字符，含末尾那三个点）");
                CheckTrue(phZh != phEn, "★ 判别式：两语档下那句字**必须不一样**"
                    + " —— ⛔ 写死 `\"Enter text...\"`（改前那样）或写死中文，都过不了这一条");

                Object.DestroyImmediate(ipB.gameObject);
            }
            Loc.RestoreForTest(lang0);                              // 收尾：语言放回原值（⛔ 全程不写 `PlayerPrefs`）
        }

        // ---------------- ⑤·y 🆕 **2026-10-10（`A1085` + `A1103`）：两条「与尺子 / 与原版实况对过账」的词条**
        //
        //  **① `A1085`：提示行的【英文列】原来整条超过那把尺子。** 尺子 = `SearchingMatchPopup.HintLineWidth(句)`
        //    `> HintLineMaxWidth`（**80 个半宽字位**，`A1083` 定的）—— 而它**只对中文列算过**：
        //    英文档那几条路径改前实测 91 / 105 / 106 / 110~124 / 140 位 ⇒ **一条都装不进 80** ⇒ 英文档下每次
        //    `ShowHint` 都喊。本笔按「二选一」里的**改英文列的文案长度**收口：
        //    ⛔ 不改布局（那行字的框 700×148 与自适应 4~50px 是**照原版 prefab 读的**）· ⛔ 不截断
        //    （整句就是「告诉玩家刚才发生了什么」，截了就是把该玩家看的话吃掉）。
        //    ✅ 改后逐条：65 / 59 / 70 / 76 / 41 / 77 位 **全部 ≤ 80**，而且 **EN 列宽度已 ≈ ZH 列**。
        //    ⚠️ **仍超的两格如实留着**：夹着**对端原样文本**（`body`，协议钳到 40 字符）的那两条上界 = 99 / 81
        //      —— 那是协议层的钳、**与语档无关**（中文档同一格也一样超），不在本笔白名单里改（详见 `Loc.cs`）。
        //
        //  **② `A1103`：换牌那两句照【原版运行期真渲】的中文。** 判据 = 现存**唯一**一份原版真渲中文的记录
        //    （2026-09-14 运行期 dump：`资料/原版参照图/Unity参照管线_0825/data/panel_0914b/p3_mulligan_tree.tsv`
        //     的 `:15` / `:13`）。🔴 **本笔先核清了性质**：`数据/本地化/i18n/zh_CN.csv` **不是原版中文表**
        //    （它首行 `keys,type,zh_CN`、按**英文源串**索引；原版 I2 导出会按 `mTerm` 索引。旁证：原版客户端
        //     根本没有中文表，I2 词条在远端 CCD —— `资料/全量反编译复核_靠推断的清单.md` §2.2 那格）
        //    ⇒ **改的是我们自己的译文**，不是「改回原版词条」。
        //    🔴 **2026-10-14 更正（铁律 5）**：上面括号里那句「旁证：原版客户端根本没有中文表」
        //       **不成立** —— 原版**有**官方中文表，由**远端 Addressables 包 `localization_assets_all`** 下发，
        //       **2026-10-14 已解开**并落至 `Unity/数据/游戏数据/官方本地化_en+zh-CN.json`
        //       （判据全文 → `CLAUDE.md` §一·7 的「2026-10-14 更正」段）；它引的那格（§2.2）**已就地订正**。
        //       ⇒ **两半分开看**：
        //         · **「`zh_CN.csv` 不是原版中文表」【仍成立】**，但**理由只剩前半句** ——
        //           键索引形状不同（`keys` 首行 · 按英文源串索引 vs 原版按 `mTerm` 索引），
        //           **不是**「原版根本没有中文表」（那是错因：把「不在离线包体里」读成了「根本没有」）。
        //         · **本笔的结论「改的是我们自己的译文」【仍成立】**，但**「不是改回原版词条」这句要重看** ——
        //           官方 zh-CN 表已在手，那两句**现在可以拿去对官方词条**（⛔ 本笔没做，见 `A1103`）。
        //       ⚠️ 上面那句「现存**唯一**一份原版真渲中文的记录」**仍成立**（官方表是**词条**、不是**真渲画面**）；
        //          只是它的**地位变了**：原先它是唯一的判据，现在它是一条**旁证**。
        //
        //  🧨 **各带判别式（灭自证）** —— 见下面每一条的说明（期望值**全是字面量**，⛔ 不拿 `Loc.T` 当期望）。
        Section("★ A1085 + A1103：提示行的英文预算（≤ 80 位）+ 换牌那两句照原版真渲的中文");
        {
            // ============================================================ ① A1085 · 提示行英文列
            const int Budget = 80;   // ⛔ 字面量（⛔ 不用 `HintLineMaxWidth` —— 那会「改常量两边一起绿」）
            Check(SearchingMatchPopup.HintLineMaxWidth, Budget,
                  "★ 前提：实现那把尺子仍是 80 个半宽字位"
                + " —— 🧨 靠把阈值改大来「消掉」告警 ⇒ 本条立刻红");

            const string KB = "Settings/Online/Lobby/";
            string[] need =
            {
                "DeferToBattle", "PeerLostHint", "PeerLeftHint", "PeerLostFrag", "PeerLeftFrag",
                "MatchRevoked", "NotMatchingThisGame", "LobbyRestored",
            };
            int missKey = 0;
            foreach (var n in need) if (!Loc.HasEntry(KB + n)) missKey++;
            CheckTrue(missKey == 0,
                      $"（前提）这一族 {need.Length} 条词条都在表里（缺 {missKey} 条）"
                    + " —— 缺了下面取值会是 null（本块不拿 null 去 Replace）");
            if (missKey == 0)
            {
                string d = Loc.EnOf(KB + "DeferToBattle"), pl = Loc.EnOf(KB + "PeerLostHint");
                string ll = Loc.EnOf(KB + "PeerLeftHint"), lf = Loc.EnOf(KB + "PeerLostFrag");
                string le = Loc.EnOf(KB + "PeerLeftFrag"), rev = Loc.EnOf(KB + "MatchRevoked");
                string nm = Loc.EnOf(KB + "NotMatchingThisGame"), bk = Loc.EnOf(KB + "LobbyRestored");
                string body = new string('x', 20);   // 对端转述那一段（典型长度；协议上界 = 40 字符，见报告）

                void Row(string name, string s)
                {
                    int w = SearchingMatchPopup.HintLineWidth(s);
                    CheckTrue(w <= Budget, $"英文档提示行「{name}」占 {w} 位 ≤ {Budget} —— 整句「{s}」");
                }
                // 逐条按 `NetMatchmaking` 的拼法复现（`SetHint` 是私有 ⇒ 拼句在它内部；这里是同一形状）
                Row("_started·掉线  DeferToBattle{PeerLostFrag}",       d.Replace("{0}", lf));
                Row("_started·离开  DeferToBattle{PeerLeftFrag+body}",  d.Replace("{0}", le + body));
                Row("未开局·掉线 A  PeerLostHint{MatchRevoked}",        pl.Replace("{0}", rev));
                Row("未开局·掉线 B  PeerLostHint{NotMatchingThisGame}", pl.Replace("{0}", nm));
                Row("未开局·离开    PeerLeftHint{body,tail}",           ll.Replace("{0}", body).Replace("{1}", nm));
                Row("对面回来       LobbyRestored",                     bk);

                // 🧨 判别式：**改前那两句（逐字）必须超** —— 证明上面那几条有牙（不是「怎么改都绿」）
                const string OldDefer = "{0} — the match has already started, entering the arena (the battle layer will continue)";
                const string OldLost = "Opponent disconnected, {0} (both of you press Battle! again after they return)";
                CheckTrue(SearchingMatchPopup.HintLineWidth(OldDefer.Replace("{0}", lf)) > Budget,
                          "🧨 判别式：**改前**那版 `DeferToBattle` 英文列确实超 80（把它改回去 ⇒ 上面那条红）");
                CheckTrue(SearchingMatchPopup.HintLineWidth(OldLost.Replace("{0}", nm)) > Budget,
                          "🧨 判别式：**改前**那版 `PeerLostHint` 英文列确实超 80（同上）");

                // 中文列**一个字没动**（本笔只压英文列）—— 拿字面量钉住
                Check(Loc.ZhOf(KB + "PeerLostHint"), "对面掉线了，{0}（两边回来各点一次 `Battle!`）",
                      "★ 中文列**逐字未改**（本笔只动英文列）⇒ 中文档的长度账一条都没变");
                Check(Loc.ZhOf(KB + "LobbyRestored"), "对面回来了 —— 联机已恢复。要开这一局，两边重新各点一次 `Battle!`",
                      "★ 同上（`LobbyRestored`）");
            }

            // ============================================================ ② A1103 · 换牌那两句
            var langB = Loc.Current;
            Loc.RestoreForTest(AvailableLanguages.Chinese);          // ⛔ 只改内存（不写 `PlayerPrefs`）
            try
            {
                CheckTrue(Loc.HasEntry("Battle/Mulligan/secondTurn") && Loc.HasEntry("Battle/Mulligan/Instructions"),
                          "（前提）换牌那两条词条在表里");
                Check(Loc.T("Battle/Mulligan/secondTurn"), "你是第二个行动",
                      "★ 换牌「谁先手」那行 = **原版运行期真渲**那句（原版节点 `MulliganText/TurnText`）");
                Check(Loc.T("Battle/Mulligan/Instructions"), "选择要在首轮替换的牌",
                      "★ …提示行 = **原版运行期真渲**那句（原版节点 `MulliganText/Text`）");
                // 🧨 判别式：我们自己那版译法**必须已经不在这两条键上**（两边一起改回去 ⇒ 上面两条红）
                CheckTrue(Loc.T("Battle/Mulligan/secondTurn") != "你后手"
                       && Loc.T("Battle/Mulligan/Instructions") != "选择首局替换的卡牌",
                          "🧨 判别式：`你后手` / `选择首局替换的卡牌`（我们自己译的）**已不在这两条键上**");
                // ✅ 同族第三条本来就 = 原版真渲的 `继续` ⇒ 留住这一条，防「顺手把整片一起改掉」
                Check(Loc.T("Battle/Mulligan/ButtonDone"), "继续",
                      "★ 同族第三条（`Battle/Mulligan/ButtonDone`）本来就 = 原版真渲的 `继续`，**没被牵连**");
            }
            finally { Loc.RestoreForTest(langB); }                   // 收尾：语言放回原值
            // 英文列 = 原版 prefab 那颗 TMP 的原文，**逐字未改**
            Check(Loc.EnOf("Battle/Mulligan/secondTurn"), "You go second", "★ 英文列 = 原版 TMP 原文，未改");
            Check(Loc.EnOf("Battle/Mulligan/Instructions"), "Choose cards to replace in first hand", "★ 同上");
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
                    //   ⛔ 不删节点**（`Shell/MenuDraw.cs` 那一句；函数体与「不删节点」的写法都在同一文件）
                    //   ⇒ 不判这一下就会把**画面上不画**的底边条算进「画出来的下沿」
                    //   （实测 = 第 9 行的 856.00，而不是视口下沿 811.00）。
                    //   口径照**同文件**的 `GUnion`（下面 `本文件`）抄 —— 那一条本来就写着
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
            // 🔴 **2026-10-20（W58·D4 · 切片 3）**：建窗会走 `AllianceSearchTab.Build → ShowJoin()`
            //   ⇒ 原版那一段 `JoinAllianceMenu.Initialize` 的 `ShowLoading() → 重填 → 回调首句 HideLoading()`
            //   被完整走了一遍。本地数据同步 ⇒ **必须同帧收尾**（不留在屏上）。
            //   判据 → `资料/普查产出_1020/W58_界面线_联盟搜索形态判据.md` §一·3 / §四「切片 3」。
            CheckTrue(shell.Loading != null && shell.Loading.Controller != null
                      && !shell.Loading.Controller.IsShowing,
                      "★ W58·D4：社交窗建完（`ShowJoin` 走完 Show→重填→Hide）之后，转圈件**没有留在屏上**"
                    + "（原版 `FillOpenAlliances` 回调第一句就是 `HideLoading()`）"
                    + "｜🧨 把 `AllianceSearchTab.WithLoading` 的 `finally { loading.Hide(); }` 去掉 ⇒ 本条红");
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
            //    ⇒ 态一量到的右沿就是**被夹在框沿上**的那个值。判据：`Shell/InboxWindow.cs` 的 `MsgList`。
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

            // 🆕 **2026-10-18（A840）**：这颗视口**记过自己的设计矩形** —— 记录点 = `BuildArmySelector` 里
            //   `AddComponent<ViewportClip>()` 之后那句 `vc.CaptureNow();`（`Shell/LiveOpsEventWindow.cs` 的 `vc.CaptureNow();`）。
            //   口径 → `Shell/ViewportClip.cs` 文件头 §①；现场 → `资料/普查产出_1018/S1_A840与A867.md` §1/§2.1。
            //   🔴 **为什么非断不可**：本窗 `Open()` 每次都**整棵树重建**，而 `CaptureNow()` 抓的是「建树那一刻、
            //   `MenuDraw.Node` 刚写进这个节点的那个矩形」= 框的中心那一帧。⛔ 少了它 ⇒ 本节点**逐位回落到旧写法**
            //   （实时反推 + `LiveDerivations`）：节点或它的**祖先**被挪过之后，框与被比矩形就**不在同一帧**
            //   —— 那正是 A811 根治掉的那个病灶（`Shell/ViewportClip.cs` 文件头「A811 根治」那一节）。
            CheckTrue(armVc != null && armVc.HasBaseRect,
                      "★ A840：这颗视口**记过设计矩形**（`ViewportClip.HasBaseRect`）"
                    + "｜**改坏法**：删掉 `Shell/LiveOpsEventWindow.cs` 那句 `vc.CaptureNow();` ⇒ 本条红"
                    + (armVc != null ? "" : "（⛔ 节点没找到，本条按上面 A745 那条的红一起看）"));
            // 期望值 = **原版 prefab 字面量**（`Ranked Army Selector/Army Selector/Viewport`：
            // 1323.16,218.94 → 1870.28,882.03）—— ⛔ 不拿 `LiveOpsEventWindow.ArmView*` 当期望值：
            // 那是**被测实现传进 `Node(...)` 的实参**（拿它当期望值 = 自证）。
            var armFrameLit = new PxRect(1323.16f, 218.94f, 1870.28f, 882.03f);
            CheckTrue(NearPx(armFrame, armFrameLit, 0.05f),
                      "（前提）上面那个 `armFrame`（= `LiveOpsEventWindow.ArmView*` 四个常量）**逐值 = 原版 prefab 的实读值**"
                    + $"（{armFrameLit.x1:F2},{armFrameLit.y1:F2} → {armFrameLit.x2:F2},{armFrameLit.y2:F2}）"
                    + " —— 改坏法：改那四个常量里任一个 ⇒ 本条红；"
                    + "⛔ 上面 A745 那条拿 `armFrame` 当期望值，本条就是它「是不是原版值」的那条判据");
            if (armVc != null && armVc.HasBaseRect)
            {
                var armBr = armVc.BaseRect;
                bool armBrOk = Mathf.Abs(armBr.x1 - armFrameLit.x1) <= 0.05f && Mathf.Abs(armBr.y1 - armFrameLit.y1) <= 0.05f
                            && Mathf.Abs(armBr.x2 - armFrameLit.x2) <= 0.05f && Mathf.Abs(armBr.y2 - armFrameLit.y2) <= 0.05f;
                CheckTrue(armBrOk,
                          "★★ A840：记下的矩形 = **宿主写进这个节点的那份设计矩形**"
                        + $"（实测 {armBr.x1:F3},{armBr.y1:F3} → {armBr.x2:F3},{armBr.y2:F3}；期望 = 原版 prefab 的 "
                        + $"{armFrameLit.x1:F2},{armFrameLit.y1:F2} → {armFrameLit.x2:F2},{armFrameLit.y2:F2}）"
                        + "｜**改坏法**：把 `CaptureNow()` 挪到节点被改过之后再调（= 记成**错帧**）⇒ 本条红"
                        + "（那时 `HasBaseRect` 仍是 true ⇒ **上面那条抓不到它，只有本条抓得住**）");
            }
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

            // ============================================================ 🆕 A867：同一实例再 `Open()` ⇒ 登记表不涨
            //   （先例逐字同形的写法 → `Editor/RewardsScene.cs` 的 A510 那一条；
            //    现场 / 改坏法 → `资料/普查产出_1018/S1_A840与A867.md` §2.2 / §4.1）
            //   🔴 口径：本窗 `Open()` 每次都 `Build()`，而 `Build()` 是「把 `root` 的子件全清掉再重建」、
            //     会登记一颗**新**的 `MenuScroll`（`_armyScroll.Owner = gameObject`）—— 「关窗只 `SetActive(false)`、
            //     **不销毁**」+「`MenuScroll` 是**普通 C# 类**（`== null` 恒假）」⇒ `PointerLayer` 那两道
            //     自动清理（`PruneScrolls` 的 `s == null` / `s.Owner == null`）**都判不出旧条目已死**。
            //   ⚠️ 先重建一次**取基线** —— 那一次会顺手把待清的死条目吸掉，基线之后那一次才只反映「本窗重建涨不涨」。
            sk.Open();                                  // 吸基线（表里若还挂着别人的死条目，这一下清掉）
            int nScrollSk = PointerLayer.ScrollCountForTest;
            // 🔴 **前提（灭「假绿」）**：登记表**非空**才说明 `PointerLayer.Instance` 在、而且本窗真登记过 ——
            //    表一旦是空的，下面比的就是 **0 vs 0**（= 什么都没验，那种断言删掉实现也照样绿）。
            CheckTrue(nScrollSk > 0,
                      $"（前提）滚动登记表非空（基线 {nScrollSk} 条）—— ⛔ 为 0 ⇒ 下面那条比的是 0 vs 0，等于没验");
            sk.Open();                                  // ← 真·重建一次（生产侧这条 = `TryOpen` 从 `Closed` 支 → `Open()`）
            Check(PointerLayer.ScrollCountForTest, nScrollSk,
                  $"★★ A867：同一实例再 `Open()` 一次（= `Build()` 再跑一遍）⇒ 滚动登记表**一条都不涨**"
                + $"（{nScrollSk} → {PointerLayer.ScrollCountForTest}）"
                + "｜**改坏法**：删掉 `Shell/LiveOpsEventWindow.cs` 那句 "
                + "`PointerLayer.UnregisterOwnedBy(gameObject);` ⇒ 每重建一次**净涨 1 条**（本窗只有阵营条那一颗登记），"
                + "而且旧条目**还能被滚轮命中**（`OnChanged` 指向已销毁的节点）⇒ 本条红");
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
            // 🔴 **2026-10-20（`A1350` 第二批）收口**：函数体 = 一行转发 `MenuDraw.UnionQuadRectPx`。
            //   **逐项保留现闸**（⛔ 别顺手统一成别的 `QuadGate`）：`QuadGate.InHierarchy` + `searchInactive:true`
            //   就是本处原来手写的「`GetComponentsInChildren<ImageQuad>(true)` + 跳过 `!activeInHierarchy`」
            //   （两种口径**逐字等价**）。⚠️ 失败时四个 out 现在归 **0**（旧体是 MaxValue/MinValue）——
            //   调用点一律先判返回值再读 out（本段原有纪律），⛔ 别拿失败档的 out 去算。
            bool UnionPx(Transform node, out float x1, out float y1, out float x2, out float y2)
                => MenuDraw.UnionQuadRectPx(node, MenuDraw.QuadGate.InHierarchy, true,
                                             out x1, out y1, out x2, out y2);

            // 一颗节点**自己的** px 矩形（中心走**设计帧**读口 `MenuCheck.DesignPx`、尺寸走 `rect × K`）——
            // 🔴 **2026-10-20（`A1350` 第二批）换口**：中心从甲式 `LayoutSpace.ToPixel` 换成乙式
            //   `MenuCheck.DesignPx(t)` —— 它正是 `MenuCheck.NodeRectPx` 在 `RectCenterMode.Node` 档取的**同一个中心**
            //   （中心恒在传入节点）。⚠️ **`rt.rect` 的尺寸项单独保留**（R2：布局框 ≠ 实绘）：
            //   ⛔ 不能整函数转发 `MenuCheck.NodeRectPx` —— 它的尺寸取 `Label` / `ImageQuad` 的**实绘**大小，
            //   而本函数量的是一颗**命中框节点**的布局矩形（`MenuDraw.SetPxSize` 把锚点写成重合 ⇒ `rect` 只由
            //   `sizeDelta` 决定），两者本来就不是同一样东西。命中区那一路没有「主格/子块」这回事（它不吃软边）
            //   ⇒ 直接量节点本身，就是 `MenuDraw.ClipRectAbove` 的产物。
            bool NodeRectPx(Transform t, out float x1, out float y1, out float x2, out float y2)
            {
                const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;
                x1 = y1 = x2 = y2 = 0f;
                var rt = t as RectTransform;
                if (rt == null) return false;
                Vector2 c = MenuCheck.DesignPx(t);
                float hw = rt.rect.width * K * 0.5f, hh = rt.rect.height * K * 0.5f;
                x1 = c.x - hw; x2 = c.x + hw; y1 = c.y - hh; y2 = c.y + hh;
                return true;
            }

            // ---- 夹具：**不动玩家的真存档**（`DeckStore.OverridePath` 指到临时文件 + 先造几套卡组）----
            //   口径同 `Editor/CollectionScene.cs` 的 `Run` 里那段「不碰真存档」夹具 / `Editor/MainMenuScene.cs` 的 `Run` 里那段「不碰真存档」夹具 那两处。
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
            // 🔴 **2026-10-24（`W8` · P1 差异 #1）**：阵营轨改回原版的 **2 列**之后，第 0 格不再是
            // 「单列居中」的 116.98，而是 **75.98..157.98**（= `ArmVpX1 + GetStartOffset.x 6.56`）。
            // 期望值照旧是**原版字面量 + uGUI 算式**（`SetCellsAlongAxis` / `GetStartOffset`）现算，
            // ⛔ 不读 `PracticeModePopup.ArmyCellL`（那是被测实现的实参 = 自证）。
            //   2 列：`cellCountX = Floor((177.12 + 0.001) / 82) = 2` ·
            //        `startOffset.x = 0 + (177.12 − 164) × 0.5 = 6.56` ⇒ 第 0 列 75.98 / 第 1 列 157.98。
            const float ArmCellL = 75.98f, ArmCellT = 149.07f, ArmCellR = 157.98f, ArmCellB = 231.07f;
            const float DkCellL = 270.08f, DkCellT = 262.64f, DkCellR = 450.08f, DkCellB = 442.64f;
            const float ArmCutX = 116.98f;    // = 第 0 格 75.98..157.98 的中点
            const float DkCutX = 360.08f;     // = 格 270.08..450.08 的中点

            var pw = PracticeModePopup.Create(shell.Windows);
            shell.Windows.OpenWindow(pw);
            // 🔴 **2026-10-22（`W65` 收藏线与原版对齐）**：练习窗的默认模式已改回原版 **Skirmish(13)**
            //   （判据 = `decomp_full/PracticeModePopup__.ctor.c` 首句 `*(param_1+0x110) = 0xd`）。
            //   而本段夹具那 4 套卡组是 `lib.Create(name)` 建的（缺省 `gameMode = 0` = **Classic**）
            //   ⇒ 不切模式的话 `DeckRows` **一格都不建**、下面 ③ 那几态全塌（前提断言会先红）。
            //   ⛔ 别改成“让夹具建 Skirmish 卡组”：本段验的是**版式几何**，模式只要与夹具一致即可。
            pw.SetGameMode(RuleEngine.GameMode.Classic);
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

            // 🆕 **2026-10-18（A840）**：这两颗视口**各记过自己的设计矩形** —— 记录点 = `BuildArmySelector` 里
            //   `armyVc.CaptureNow();` 与 `BuildDeckRows` 里 `deckVc.CaptureNow();`（两处都在 `Shell/PracticeModePopup.cs`）。
            //   口径 → `Shell/ViewportClip.cs` 文件头 §①；现场 → `资料/普查产出_1018/S1_A840与A867.md` §1/§2.1。
            //   🔴 **两处必须逐颗各一条**：取状态那一路（`ViewportClip.Resolve` 第 2 步）是**沿父链找最近的那一颗**
            //   ⇒ **一处漏补 = 那一处静默回落到旧写法**（实时反推 + `LiveDerivations`），另一处补了也**管不到它**。
            //   ⛔ 所以 ⛔ **不写成「至少有一颗对」**那种弱断言。
            CheckTrue(armyVc != null && armyVc.HasBaseRect,
                      "★ A840：**阵营**视口（`Army Selector/Viewport`）记过设计矩形（`ViewportClip.HasBaseRect`）"
                    + "｜**改坏法**：删掉 `Shell/PracticeModePopup.cs` 那句 `armyVc.CaptureNow();` ⇒ 本条红");
            CheckTrue(deckVc != null && deckVc.HasBaseRect,
                      "★ A840：**卡组**视口（`Decks Scroll view/Viewport`）记过设计矩形（同上）"
                    + "｜**改坏法**：删掉 `Shell/PracticeModePopup.cs` 那句 `deckVc.CaptureNow();` ⇒ 本条红"
                    + "（⛔ 上面那条绿**管不到它** —— `Resolve` 找的是**最近**的那一颗）");
            // 期望值 = **原版字面量**（本段顶上那四个 `const float ArmVp*/DkVp*`，它们的出处逐条写在各常量注释里）
            // —— ⛔ 不读 `PracticeModePopup.ArmVp*` / `DecksVp*`：那是**被测实现传进 `Node(...)` 的实参**（= 自证）。
            if (armyVc != null && armyVc.HasBaseRect)
            {
                var aBr = armyVc.BaseRect;
                bool aBrOk = Mathf.Abs(aBr.x1 - ArmVpX1) <= 0.05f && Mathf.Abs(aBr.y1 - ArmVpY1) <= 0.05f
                          && Mathf.Abs(aBr.x2 - ArmVpX2) <= 0.05f && Mathf.Abs(aBr.y2 - ArmVpY2) <= 0.05f;
                CheckTrue(aBrOk,
                          "★★ A840：阵营视口记下的矩形 = **宿主写进这个节点的那份设计矩形**"
                        + $"（实测 {aBr.x1:F3},{aBr.y1:F3} → {aBr.x2:F3},{aBr.y2:F3}；期望 = 原版 "
                        + $"{ArmVpX1},{ArmVpY1} → {ArmVpX2},{ArmVpY2}）"
                        + "｜**改坏法**：把 `CaptureNow()` 挪到节点被改过之后再调（= 记成**错帧**）⇒ 红"
                        + "（`HasBaseRect` 仍 true ⇒ 上面那条抓不到它，只有本条抓得住）");
            }
            if (deckVc != null && deckVc.HasBaseRect)
            {
                var dBr = deckVc.BaseRect;
                bool dBrOk = Mathf.Abs(dBr.x1 - DkVpX1) <= 0.05f && Mathf.Abs(dBr.y1 - DkVpY1) <= 0.05f
                          && Mathf.Abs(dBr.x2 - DkVpX2) <= 0.05f && Mathf.Abs(dBr.y2 - DkVpY2) <= 0.05f;
                CheckTrue(dBrOk,
                          "★★ A840：卡组视口记下的矩形 = 同上一颗（口径逐字相同）"
                        + $"（实测 {dBr.x1:F3},{dBr.y1:F3} → {dBr.x2:F3},{dBr.y2:F3}；期望 = 原版 "
                        + $"{DkVpX1},{DkVpY1} → {DkVpX2},{DkVpY2}）"
                        + "｜**改坏法**：同上；⚠️ 两处的**可见面本来就不同**（阵营 177.12×764.21 vs 卡组 373.60×540.79）"
                        + "⇒ 「两颗记成同一份」也会被本条抓出来");
            }
            // 🔴 **第三个观测轴**（S1 §4.2 点名的那一个）：`ViewportClip.LiveDerivations` = 走「实时反推」那一支的计数。
            //   修后这两颗**一次都不该走那一支**。⚠️ 它是**全局静态、跨窗累计** ⇒ **只能在本段里前后差分**
            //   （⛔ 别横向比绝对值）；判据用法 `Shell/ViewportClip.cs` 自己就是这么写的。
            //   ⚠️ 与上面那两条**不是同一件事**：那两条看**状态**（`HasBaseRect` / 记下来的值），本条看**代码路径**
            //   （读框时到底进了哪一支）—— 两条都断了才排除「状态对、但取用那一趟仍走旧路」这一档。
            if (armyVc != null)
            {
                int ld0 = ViewportClip.LiveDerivations;
                var probeClip = armyVc.ClipPx;             // 读一次 ⇒ 记过矩形的那一支不涨、回落那一支 +1
                CheckTrue(probeClip.HasValue, "（前提）这颗视口给得出框（`ClipPx` 非 null）");
                Check(ViewportClip.LiveDerivations, ld0,
                      $"★ A840：读一次 `ClipPx` ⇒ **`LiveDerivations` 一条都不涨**（{ld0} → {ViewportClip.LiveDerivations}）"
                    + " —— 涨了 = 这颗节点**没有** `BaseRect`、又走回了实时反推那一支（A811 那个病灶）"
                    + "｜**改坏法**：删掉 `Shell/PracticeModePopup.cs` 那句 `armyVc.CaptureNow();` ⇒ 本条红");
            }
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
                      $"（前提）生产框下**建出卡组格**来了（{pw.DeckRows.Count} 格）"
                      + " —— 为 0 的话 ③ 那几态等于没验"
                      + "｜⚠️ 2026-10-22 订正（铁律 5）：原来这里写着「夹具 4 套 ⇒ 两列 2 行」—— **已过期**："
                      + "练习窗左列现在读原版 78 对 `DemoDeckInfoSO`（`demo_deck_pairs.json`），"
                      + "逻辑格数按「当前阵营筛选 6 对（+ 可能的首格「你的卡组」）」"
                      + "；视口内实建 = 与视口相交的那几格（RectMask2D 只裁不建）");

            // ============================================================ ② A754-a：阵营视口（`Army Selector/Viewport`）
            // 夹具 = 第 0 格（`Army_0`）：格 = **75.98,149.07 → 157.98,231.07**（2 列网格的第 0 列 +
            // `ArmView*` 的上沿）⇒ 它在生产框下**完整落在视口里**、被切到 116.98（格中点）之后**仍相交**。
            // 🔴 **2026-10-24（`W8` · P1 差异 #1）**：原来是**单列**（116.98..198.98，切点 157.98）——
            // 原版 `Filters` 是 `Flexible` 网格 ⇒ 177.12 宽里 **2 列**，上面那三个 `const` 已按 uGUI 算式改。
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
                          + $"**画出来**的并集 = 格矩形 75.98,149.07 → 157.98,231.07"
                          + (bgOk ? $"（实测 {ax1:F2},{ay1:F2} → {ax2:F2},{ay2:F2}）" : "（⛔ 并集量不到）")
                          + " —— 🔴 量的是**渲出来那几块 quad 的并集**（软边会把宿主切成主格 + 子块，"
                          + "⛔ 只量宿主那一颗会漏面积），⛔ 不是计数器");

                float hx1, hy1, hx2, hy2;
                bool hOk1 = NodeRectPx(hit0, out hx1, out hy1, out hx2, out hy2);
                CheckTrue(hOk1 && NearPx(hx1, ArmCellL, 0.1f) && NearPx(hx2, ArmCellR, 0.1f),
                          "★ A754-a 态一（对照）：命中区 = **整格**（75.98 → 157.98，宽 82）—— 生产框下它没被截"
                          + (hOk1 ? $"（实测 {hx1:F2} → {hx2:F2}）" : "（⛔ 量不到）"));

                // ⚠️ 防崩守卫：`OnChanged` 是 `System.Action`，直接 `.OnChanged()` 会在 null 上抛 NRE
                //    —— **自检崩掉比红更糟**（后面几条一起跑不到）。所以三个条件都进 if，缺了走 else 那条红。
                if (armyVc != null && pw.ArmyScroll != null && pw.ArmyScroll.OnChanged != null
                    && bg0 != null && bgOk && hOk1)
                {
                    // 🔴 **2026-10-24（`W8`）**：2 列之后「切窄」那一态会把**整条第 1 列**（x ≥ 157.98）筛掉
                    // ⇒ 建出来的格数**一定会变少**（单列时代它不变）。所以「还原后又建满」要比**切窄之前**
                    // 那个数（`armyCellsFull`），⛔ 不能比切窄之后的 `cellsNarrow`（那会恒红）。
                    int armyCellsFull = pw.ArmyCells.Count;
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
                              + "`ApplySoftEdges`）⇒ 并集右沿回到 **157.98** ⇒ 红"
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
                    int cellsNarrow = pw.ArmyCells.Count;
                    MenuDraw.ApplyPxRect(armyVc.transform, armyVc.transform.parent,
                                         new PxRect(ArmVpX1 + 4000f, ArmVpY1 + 4000f, ArmVpX2 + 4000f, ArmVpY2 + 4000f));
                    pw.ArmyScroll.OnChanged();
                    Check(pw.ArmyCells.Count, 0,
                          "★★ A754-a 态二（对照 = 上面的生产框）：把**节点**那个框挪到整块之外 ⇒ 同一句 "
                          + "`RebuildArmyCells` **一格都不建**"
                          + $"（实测 {pw.ArmyCells.Count} 格；切窄那一态建了 {cellsNarrow} 格 · 生产框那是 {armyCellsFull} 格）"
                          + " —— 🔴 **改坏法**：把那一句 `MenuDraw.VisibleAbove(holder, rr, null)` 改回 "
                          + "`MenuDraw.Visible(rr, view)`（= 迁移前的显式矩形那一路）⇒ 态二照旧建满 ⇒ 红"
                          + "｜⛔ 也别只把四处 `MenuDraw.Rect` 的 `clip` 改回 `view` —— 那是「两处状态」的老写法"
                          + "（而且节点会被形参盖住、静默），正是本迁移要消灭的东西");
                    MenuDraw.ApplyPxRect(armyVc.transform, armyVc.transform.parent,
                                         new PxRect(ArmVpX1, ArmVpY1, ArmVpX2, ArmVpY2));
                    pw.ArmyScroll.OnChanged();
                    CheckTrue(pw.ArmyCells.Count == armyCellsFull,
                              $"★ …把节点框**还原** ⇒ 又建出同样多格（实测 {pw.ArmyCells.Count}，生产框 {armyCellsFull}；"
                              + $"切窄那一态 {cellsNarrow} —— **比它少是应该的**：2 列网格切到第 0 格中点之后，"
                              + "整条第 1 列都落在框外）"
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

            // ============================================================ 🆕 2026-10-24（`W8`）：P1 差异清单可静态落地项
            // 判据 → `资料/普查产出_1024d/P1_练习窗原版对照审查.md` §四 差异 #1 / #2 / #3 / #5。
            // ⛔ 期望值一律**原版字面量**或**数据面现读**（⛔ 不读 `PracticeModePopup` 的常量 = 自证）。
            Section("★ W8：练习窗按原版补四件（阵营 2 列 · 阵营标题本地化 · 火漆印/描边只给「你的卡组」· 预组督军中文名）");
            {
                // ------------------------------------------------------------ D1：阵营轨 = 2 列、13 格一屏全见
                // 判据 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-3879242981144375492.json`
                //   （宿主 `Filters`）：cell 82×82 · spacing (0,26.38) · align=4 · **Flexible**，容器 177.12。
                // uGUI 现算（本地真源码 `GridLayoutGroup.SetCellsAlongAxis` + `LayoutGroup.GetStartOffset`）：
                //   `cellCountX = Floor((177.12 + 0.001)/82) = 2` · `startOffset.x = (177.12 − 164) × 0.5 = 6.56`
                //   ⇒ 第 0 列 75.98..157.98、第 1 列 157.98..239.98；行数 = ⌈13/2⌉ = 7；
                //   内容高 = (82 + 26.38) × 7 − 26.38 = **732.28 ≤ 视口 764.21** ⇒ 不滚。
                // 🧨 改坏法：`RebuildArmyCells` 里退回 `row = i`（单列）⇒ 下面 5 条同时红。
                Check(PracticeModePopup.ArmyCols, 2,
                      "★ W8-D1 阵营轨列数 = **2**（原版 `Flexible` 网格在 177.12 宽里现算；单列实现在这里红）");
                CheckTrue(pw.ArmyCells.Count == 13,
                          $"★ W8-D1 13 个阵营**一屏全建出来**（实测 {pw.ArmyCells.Count} 格 —— "
                        + "单列时代只建得出**前 8 格**（第 9 格起整块在视口外、被 `VisibleAbove` 筛掉）");
                CheckNear(pw.ArmyScroll.ContentX2 - pw.ArmyScroll.ContentX1, 732.28f, 0.6f,
                          "★ W8-D1 内容高 = (82 + 26.38) × 7 − 26.38 = **732.28**（⛔ 不是单列的 1382.56）");
                CheckTrue(pw.ArmyScroll.MaxOffset <= 0.001f,
                          $"★ W8-D1 内容高 ≤ 视口高 764.21 ⇒ **可滚范围 = 0**（实测 {pw.ArmyScroll.MaxOffset:F2}）"
                        + " —— 原版那一列本来就不滚（P1 §一·3）");
                if (pw.ArmyCells.Count >= 3)
                {
                    float c0x1, c0y1, c0x2, c0y2, c1x1, c1y1, c1x2, c1y2, c2x1, c2y1, c2x2, c2y2;
                    bool ok0 = NodeRectPx(pw.ArmyCells[0], out c0x1, out c0y1, out c0x2, out c0y2);
                    bool ok1 = NodeRectPx(pw.ArmyCells[1], out c1x1, out c1y1, out c1x2, out c1y2);
                    bool ok2 = NodeRectPx(pw.ArmyCells[2], out c2x1, out c2y1, out c2x2, out c2y2);
                    CheckTrue(ok0 && NearPx(c0x1, 75.98f, 0.6f) && NearPx(c0y1, 149.07f, 0.6f)
                                  && NearPx(c0x2, 157.98f, 0.6f) && NearPx(c0y2, 231.07f, 0.6f),
                              "★ W8-D1 `Army_0` = **75.98,149.07 → 157.98,231.07**（第 0 列左沿 = `69.42 + 6.56`）"
                            + (ok0 ? $"（实测 {c0x1:F2},{c0y1:F2} → {c0x2:F2},{c0y2:F2}）" : "（⛔ 量不到）"));
                    CheckTrue(ok1 && NearPx(c1x1, 157.98f, 0.6f) && NearPx(c1y1, 149.07f, 0.6f)
                                  && NearPx(c1x2, 239.98f, 0.6f) && NearPx(c1y2, 231.07f, 0.6f),
                              "★ W8-D1 `Army_1` = **第 1 列同一行**（157.98→239.98，y 与第 0 格相同 —— 单列实现在这里红）"
                            + (ok1 ? $"（实测 {c1x1:F2},{c1y1:F2} → {c1x2:F2},{c1y2:F2}）" : "（⛔ 量不到）"));
                    CheckTrue(ok2 && NearPx(c2x1, 75.98f, 0.6f) && NearPx(c2y1, 257.45f, 0.6f)
                                  && NearPx(c2x2, 157.98f, 0.6f) && NearPx(c2y2, 339.45f, 0.6f),
                              "★ W8-D1 `Army_2` = 回到第 0 列、下一行（y 差 **108.38** = 82 + spacing.y 26.38）"
                            + (ok2 ? $"（实测 {c2x1:F2},{c2y1:F2} → {c2x2:F2},{c2y2:F2}）" : "（⛔ 量不到）"));
                }
                else CheckTrue(false, "阵营格只有 " + pw.ArmyCells.Count + " 个 ⇒ 上面三条格位断言等于没查");

                // ------------------------------------------------------------ D2：阵营标题 = 官方本地化文案
                // 判据 = 原版 `ChangeSelectedArmy` → `LocalizationManager.GetTermTranslation(…)`
                //   ⇒ 词条 `Armies/<CardArmy 成员名>`；参考图 = **基因窃取者教团**（`练习界面参考图.png`）。
                // ⚠️ `Loc.T` 对缺键**返回键名本身** ⇒ 下面两条一起才有鉴别力：
                //   ① 13 条词条**全部**非空且 ≠ 键（缺 0）；
                //   ② 节点上那句 = 参考图那个值（写死原版值，⛔ 不重算 `SelectedArmyName()` = 自证）。
                // 🧨 改坏法：`SelectedArmyName` 退回 `ToUpperInvariant()` ⇒ ② 红（会印 `GENESTEALERS`）。
                {
                    var facsW = CollectionWindow.CardsState.Factions();
                    int miss = 0;
                    var missKeys = new List<string>();
                    for (int i = 0; i < facsW.Count; i++)
                    {
                        string k = CardText.ArmyTermPrefix + facsW[i];
                        string v = Loc.T(k);
                        if (string.IsNullOrEmpty(v) || v == k) { miss++; missKeys.Add(k); }
                    }
                    Check(miss, 0,
                          "★ W8-D2 13 个阵营**全部**取到真文案（缺失 " + miss + " 条"
                        + (miss > 0 ? "：" + string.Join(", ", missKeys.ToArray()) : "") + "）"
                        + " —— 缺键时 `Loc.T` 会把键名画上屏 = 静默失败");

                    int gsIdx = -1;
                    for (int i = 0; i < facsW.Count; i++)
                        if (RuleEngine.DeckRules.SameFaction(facsW[i], "Genestealers")) { gsIdx = i; break; }
                    CheckTrue(gsIdx >= 0, "（前提）阵营表里有 `Genestealers`（参考图选中的就是它）");
                    if (gsIdx >= 0)
                    {
                        pw.ArmyIndex = gsIdx;
                        // ⚠️ 标题的刷新点在 `RebuildDeckRows` 末尾（`RebuildArmyCells` 是**滚动**那条路、不刷标题）
                        // ⇒ 这里走自检口 `RebuildDeckListForTest()`，⛔ 别只调 `ArmyScroll.OnChanged()`（标题会停在旧值）。
                        pw.RebuildDeckListForTest();
                        Check(pw.ArmyTitleText, "基因窃取者教团",
                              "★ W8-D2 `Selected Army Title` = **基因窃取者教团**（原版本地化词条 `Armies/Genestealers`）"
                            + "（实测「" + pw.ArmyTitleText + "」；⛔ 不是 `GENESTEALERS`、也⛔ 不是键名 `Armies/Genestealers`）");
                    }
                }

                // ------------------------------------------------------------ D3：火漆印 / 描边**只给「你的卡组」那一格**
                // 判据 = `DeckSelectorMenuItemDemo__InitializeWithPracticeDeck.c`：开场 `set_enabled(false)`；
                //   玩家卡组支 `IsPlayerDeck.SetActive(true)`、预组支 `SetActive(false)`（两颗同理）。
                // 🧨 改坏法：`BuildDeckCell` 里去掉 `isPlayerDeck` 那两句 `SetActive` ⇒「其余格全关」红；
                //   一刀切永远关 ⇒「首格开」红（两条成对 ⇒ 两种错法都抓得到）。
                {
                    pw.ArmyIndex = -1;                      // 不过滤 ⇒ 首格「你的卡组」一定建出来
                    pw.RebuildDeckListForTest();
                    int onSeal = 0, onBorder = 0, mineSeen = 0, otherSeen = 0;
                    bool mineOk = false;
                    foreach (var row in pw.DeckRows)
                    {
                        if (row == null) continue;
                        bool isMine = row.name == PracticeModePopup.YourDeckCellName;
                        var seal = FindChildIn(row, "IsPlayerDeck");
                        var bd = FindChildIn(row, "Button border");
                        CheckTrue(seal != null && bd != null,
                                  "（前提）`" + row.name + "` 的两件（`IsPlayerDeck` / `Button border`）**都建出来了**"
                                + " —— 判据要求「建了再按 `isPlayerDeck` 开关」，⛔ 不是只给首格建");
                        if (seal == null || bd == null) continue;
                        if (seal.gameObject.activeSelf) onSeal++;
                        if (bd.gameObject.activeSelf) onBorder++;
                        if (isMine)
                        {
                            mineSeen++;
                            mineOk = seal.gameObject.activeSelf && bd.gameObject.activeSelf;
                        }
                        else
                        {
                            otherSeen++;
                            if (seal.gameObject.activeSelf || bd.gameObject.activeSelf)
                                CheckTrue(false, "★ W8-D3 预组格 `" + row.name + "` 的 `IsPlayerDeck` / `Button border` "
                                               + "**必须都关**（原版 `InitializeWithPracticeDeck` 的预组支就是 `SetActive(false)`）");
                        }
                    }
                    CheckTrue(mineSeen == 1 && otherSeen > 0,
                              $"（前提）首格「你的卡组」**恰好 1 格**、预组格**若干**（实测首格 {mineSeen} · 预组格 {otherSeen}）"
                            + " —— 预组格为 0 的话「其余格全关」那条等于没验");
                    CheckTrue(mineSeen == 1 && mineOk,
                              "★ W8-D3 **「你的卡组」那一格**的 `IsPlayerDeck` + `Button border` **都开着**"
                            + $"（找到 {mineSeen} 格；两件都开 = {mineOk}）");
                    Check(onSeal, mineSeen,
                          "★ W8-D3 全列**只有「你的卡组」那一格**的 `IsPlayerDeck` 亮"
                        + $"（实测亮 {onSeal} · 首格 {mineSeen} · 预组格 {otherSeen}；"
                        + "恒开 ⇒ " + (otherSeen + 1) + "，这条分得出两种状态）");
                    Check(onBorder, mineSeen,
                          "★ …`Button border` 同样**只有那一格**亮（实测亮 " + onBorder + "）");
                }

                // ------------------------------------------------------------ D4：督军名「两支不同源」
                // 判据 = 原版 `DeckGeneralInfoDemo`：**预组（演示）支** `RawCardScript.GetLocalizedCardName(hero)`（中文），
                //   **你的卡组支** `deckHero` **裸名**（英文）。参考图显示英文 = 当时选中的正是「你的卡组」那一支。
                // ⚠️ 只有**中文档**才看得出两支不同源 ⇒ 先断语档（默认 = 中文；被切走就显式红，⛔ 不静默跳过）。
                // 🧨 改坏法：把 `WarlordNameOf` 里 `PickedPrebuilt != null` 那条分叉删掉（两支都 `wl.Name`）⇒ 第一条红。
                {
                    Check(Loc.Current, AvailableLanguages.Chinese,
                          "（前提）当前语档 = 中文（D4 的「预组中文 / 你的卡组英文」只在中文档看得出来）");
                    PrebuiltDecks.Deck deckD = null;
                    RuleEngine.CardDef heroD = null;
                    int pairIdxD = -1;
                    for (int i = 0; i < PrebuiltDecks.Pairs.Count && deckD == null; i++)
                    {
                        var d0 = PrebuiltDecks.PairDeck(PrebuiltDecks.Pairs[i], pw.SelectedGameMode);
                        var h0 = d0 != null ? CollectionData.Card(d0.heroId) : null;
                        if (h0 != null && !string.IsNullOrEmpty(h0.NameZh)) { deckD = d0; heroD = h0; pairIdxD = i; }
                    }
                    CheckTrue(deckD != null && heroD != null,
                              "（前提）找得到一副「督军有官方中文名」的预组卡组");
                    if (deckD != null && heroD != null)
                    {
                        CheckTrue(heroD.Name != heroD.NameZh,
                                  "（前提）这张督军的**英文裸名 ≠ 中文名**（否则「不同源」这条没有鉴别力）");
                        pw.PickPair(pairIdxD);
                        Check(pw.WarlordNameText, heroD.NameZh,
                              "★ W8-D4 **预组**那一支的 `Warlord Name` = 本地化卡名「" + heroD.NameZh + "」"
                            + "（原版 `GetLocalizedCardName(hero)`；实测「" + pw.WarlordNameText + "」）");

                        // 「你的卡组」支：把**当前那一套**夹具挂上同一个督军，再**点首格**（走真点击链）
                        // ⇒ 那一支应显示**裸名（英文）**。
                        // ⚠️ 用点击而不是直调选择函数：`PickDeck` 是私有实现，点击是玩家真实入口（顺带验命中区）。
                        int curDeckIdx = CollectionData.CurrentIndex();
                        var libD = RuleEngine.DeckLibrary.Load();
                        CheckTrue(libD != null && curDeckIdx >= 0 && curDeckIdx < libD.Decks.Count,
                                  $"（前提）夹具卡组库非空且当前下标有效（现读 {curDeckIdx} / {libD?.Decks.Count ?? 0} 套）");
                        if (libD != null && curDeckIdx >= 0 && curDeckIdx < libD.Decks.Count)
                        {
                            libD.Decks[curDeckIdx].WarlordId = deckD.heroId;
                            libD.Save();
                            CollectionData.ResetForTest();
                            pw.RebuildDeckListForTest();
                            var ownRow = FindChildIn(pw.transform, PracticeModePopup.YourDeckCellName);
                            var ownHit = FindChildIn(ownRow, "Hit");
                            var ownWb = ownHit != null ? ownHit.GetComponent<WindowButton>() : null;
                            CheckTrue(ownWb != null, "（前提）首格「你的卡组」的命中区在（点它 = 原版那一格的选择动作）");
                            if (ownWb != null) ownWb.ClickForTest();
                            Check(pw.WarlordNameText, heroD.Name,
                                  "★ W8-D4 **「你的卡组」**那一支 = **英文裸名**「" + heroD.Name + "」"
                                + "（原版 `ShowPayerDeckInfo` 用的是 `deckHero` 裸名；实测「" + pw.WarlordNameText + "」）"
                                + " —— 与上面那条**不同源**才算接对");
                        }
                    }
                }
            }

            // ============================================================ 🆕 A867：同一实例再 `Open()` ⇒ 登记表不涨
            //   （先例 / 口径逐字同上：`Editor/RewardsScene.cs` 的 A510 那一条 ·
            //    现场 / 改坏法 → `资料/普查产出_1018/S1_A840与A867.md` §2.2 / §4.1）
            //   🔴 本窗**一处漏补 = 每次重建净涨 2 条**（它窗内**两处**登记：阵营条 + 卡组条）。
            //   ⚠️ 先重建一次**取基线**（那一次顺手把待清的死条目吸掉）⇒ 基线之后那次才只反映「本窗重建涨不涨」。
            pw.Open();
            int nScrollPw = PointerLayer.ScrollCountForTest;
            CheckTrue(nScrollPw > 0,
                      $"（前提）滚动登记表非空（基线 {nScrollPw} 条）—— ⛔ 为 0 ⇒ 下面那条比的是 0 vs 0，等于没验");
            pw.Open();
            Check(PointerLayer.ScrollCountForTest, nScrollPw,
                  $"★★ A867：同一实例再 `Open()` 一次 ⇒ 滚动登记表**一条都不涨**"
                + $"（{nScrollPw} → {PointerLayer.ScrollCountForTest}）"
                + "｜**改坏法**：删掉 `Shell/PracticeModePopup.cs` 那句 "
                + "`PointerLayer.UnregisterOwnedBy(gameObject);` ⇒ 每重建一次**净涨 2 条**（本窗两处视口）⇒ 本条红");

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

        // ============================================================ ★ A840 / A867：教程模式窗（`Tutorial Mode Menu`）
        // 现场与判据 → `资料/普查产出_1018/S1_A840与A867.md` §1（**第 7 处** —— `Shell/ViewportClip.cs` 文件头那份
        //   6 处清单当年漏掉的那一颗，靠 `grep -rn "AddComponent<ViewportClip>"` 现扫才看得见）
        //   · §2.2 第 4 处（那一句 `UnregisterOwnedBy` 是 S1 自己扫出来、超出简报点名的，**可单独回退**）。
        // ⚠️ **本窗在 `ShellScene` 里没有别的夹具** —— 生产侧它由主菜单点「教程」卡开出 ⇒ `Editor/MainMenuScene.cs`
        //   的 §A853 走的是那条**真路**（树 / 参数那些断言也归它）。本段只**直接 `Create` + `Open`**
        //   把这两笔的回归网挂上，⛔ **不重复断任何树结构**（两个宿主各写一份，迟早不一致）。
        Section("★ A840 / A867：教程模式窗（`Tutorial Mode Menu`）—— 视口框记录 + 重建不涨登记表");
        {
            var tutW = TutorialModePopup.Create(shell.Windows);
            shell.Windows.OpenWindow(tutW);
            CheckTrue(tutW != null && tutW.CurrentState == WindowState.Open,
                      "（前提）教程模式窗开起来了（本段末尾关掉）");

            // ---- A840：`Army Selector/Viewport` 那一颗记过自己的设计矩形（`Shell/TutorialModePopup.cs`）----
            //   矩形 = 原版 `Army Selector` 那一格（60,244.89 → 649.68,902.69）—— 期望值写的是**原版字面量**
            //   （出处 = `Shell/TutorialModePopup.cs` 那行注释里的实读值），⛔ 不读它自己的
            //   `ArmL/ArmT/ArmR/ArmB`（那是**被测实现传进 `Node(...)` 的实参** = 自证）。
            var tVpNode = tutW.transform.Find("Army Selector/Viewport");
            var tVp = tVpNode != null ? tVpNode.GetComponent<ViewportClip>() : null;
            CheckTrue(tVp != null,
                      "（前提·不静默）`Army Selector/Viewport` 上挂着 `ViewportClip`"
                    + "（= 原版 `RectMask2D` 的载体）—— ⛔ 拿不到 ⇒ 下面那两条**没跑**，不是绿");
            CheckTrue(tVp != null && tVp.HasBaseRect,
                      "★ A840：这颗视口**记过设计矩形**（`ViewportClip.HasBaseRect`）—— 记录点 = `BuildArmySelector` 里"
                    + " `AddComponent<ViewportClip>()` 之后那句 `vc.CaptureNow();`"
                    + "｜**改坏法**：删掉 `Shell/TutorialModePopup.cs` 那一句 ⇒ 本条红"
                    + "（该颗逐位回落到实时反推 + `LiveDerivations`）");
            if (tVp != null && tVp.HasBaseRect)
            {
                var tBr = tVp.BaseRect;
                bool tBrOk = Mathf.Abs(tBr.x1 - 60f) <= 0.05f && Mathf.Abs(tBr.y1 - 244.89f) <= 0.05f
                          && Mathf.Abs(tBr.x2 - 649.68f) <= 0.05f && Mathf.Abs(tBr.y2 - 902.69f) <= 0.05f;
                CheckTrue(tBrOk,
                          "★★ A840：记下的矩形 = **宿主写进这个节点的那份设计矩形**"
                        + $"（实测 {tBr.x1:F3},{tBr.y1:F3} → {tBr.x2:F3},{tBr.y2:F3}；"
                        + "期望 = 原版 `Army Selector` 那一格 60.00,244.89 → 649.68,902.69）"
                        + "｜**改坏法**：把 `CaptureNow()` 挪到节点被改过之后再调（= 记成**错帧**）⇒ 红"
                        + "（`HasBaseRect` 仍 true ⇒ 上面那条抓不到它，只有本条抓得住）");
            }

            // ---- A867：同一实例再 `Open()` ⇒ 滚动登记表不涨（`Shell/TutorialModePopup.cs` 的 `Open()`）----
            //   口径逐字同上（先例 → `Editor/RewardsScene.cs`）：`Build()` 只清子件、窗根不死，
            //   而 `MenuScroll` 是普通 C# 类 ⇒ `PointerLayer` 那两道自动清理**都判不出旧条目已死**。
            tutW.Open();                                // 吸基线（开窗那一次已经建过一遍）
            int nScrollTut = PointerLayer.ScrollCountForTest;
            CheckTrue(nScrollTut > 0,
                      $"（前提）滚动登记表非空（基线 {nScrollTut} 条）—— ⛔ 为 0 ⇒ 下面那条比的是 0 vs 0，等于没验");
            tutW.Open();
            Check(PointerLayer.ScrollCountForTest, nScrollTut,
                  $"★★ A867：同一实例再 `Open()` 一次 ⇒ 滚动登记表**一条都不涨**"
                + $"（{nScrollTut} → {PointerLayer.ScrollCountForTest}）"
                + "｜**改坏法**：删掉 `Shell/TutorialModePopup.cs` 那句 "
                + "`PointerLayer.UnregisterOwnedBy(gameObject);` ⇒ 每重建一次**净涨 1 条**（本窗只有关卡条那一颗登记）"
                + "⇒ 本条红");
            // ⚠️ 本窗**不 `DestroyImmediate`**（同族纪律：它进过 `WindowsManager` 的窗表 ⇒ 那会留一个 Unity 假 null，
            //   而下面 `shell.Dump()` 要遍历窗表）。
            tutW.Close();
            CheckTrue(tutW.CurrentState == WindowState.Closed, "收尾：教程模式窗关掉（别把整屏压暗层留给后面的段）");
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
                    //    `Shell/RankedTab.cs` 的 `Top4/content` 与
                    //    `AllFactions/scroll rect/viewport/content`（`_content`，**要找的就是它**）。
                    //    而 `FindChildIn`（`CardPresentation/Shell/RankedTab.cs`）是 `GetComponentsInChildren` **取第一个**、`Top4` 先建
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
            //   （`Shell/RewardsWindow.cs` 的 `Create` 只 `new GameObject` + 挂组件 + `AttachToAnchor`、⛔ 不调
            //   `mgr.OpenWindow`）⇒ 少了这一句 `CurrentState` 恒 `Closed`、`Open()` 从不跑、**树是空的**
            //   ⇒ `Tabs` / `Campaign Tab` / `Forge Tab` 全取不到 ⇒ 本段那条「（前提）`RewardsWindow` 的页
            //     都建出来了」红的根因（下面两条 `Vp4` 有 `if (ct2 != null)` / `if (ft2 != null)` 守着 ⇒ 不再级联）。
            //   对照：同段 ①（档案窗 `本文件`）/ ②（榜单 `本文件`）都是 `Create` **+ `OpenWindow`**，只这一处漏了。
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
            // 🔴 **2026-10-20（`A1350` 第二批）收口**：函数体 = 一行转发 `MenuDraw.UnionQuadRectPx`，
            //   **逐项保留现闸**（⛔ 别顺手统一）：`QuadGate.InHierarchy` + `searchInactive:true` 即本处原手写闸。
            //   ⚠️ 失败时四个 out 现在是 **0**（旧体是 MaxValue/MinValue）—— 调用点一律先判返回值（原有纪律）。
            bool GUnion(Transform node, out float x1, out float y1, out float x2, out float y2)
                => MenuDraw.UnionQuadRectPx(node, MenuDraw.QuadGate.InHierarchy, true,
                                             out x1, out y1, out x2, out y2);

            // 一颗节点**自己的** px 矩形（中心走**设计帧**读口 `MenuCheck.DesignPx`、尺寸走 `rect × K`）——
            // ⚠️ **2026-10-18（A1003）就地订正（铁律 5）**：这行原来写「与 `QuadPxRect` 同一份口径」——
            //   而 `QuadPxRect` 已收口成**设计帧读口**（`MenuDraw.QuadRectPx`：中心先除回父级缩放；
            //   ⛔ **尺寸项**在本函数里是取 `rt.rect`、在那边是取 `quad.WorldW/H`，本来也不是同一样东西）
            //   ⇒ 尺寸项只在本帧自洽。🔴 **2026-10-20（`A1350` 第二批）**：中心已从甲式
            //   `LayoutSpace.ToPixel` 换成乙式 `MenuCheck.DesignPx(t)`（与 `NodeRectPx` 同一档），
            //   尺寸项照旧 = `rt.rect × K`。⚠️ 本函数**当前零调用点**（无人用，保留待用）。
            //   （`MenuDraw.SetPxSize` 把锚点写成重合 ⇒ `rect` 只由 `sizeDelta` 决定。）
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
                Vector2 c = MenuCheck.DesignPx(t);
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
                x1 = y1 = x2 = y2 = 0f;
                if (hitNode == null) return false;
                var q = hitNode.GetComponentInChildren<ImageQuad>(true);
                if (q == null) return false;
                // 🆕 2026-10-18（A1003）：收口到 `MenuDraw.QuadRectPx`（保留本名与本形参 ⇒ 调用点 0 改动）。
                // 本函数**本来就与 `MenuDraw` 那一份同式**（乙式）⇒ 只去掉了本地那一份算式，读数不变。
                return MenuDraw.QuadRectPx(q, out x1, out y1, out x2, out y2);
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
            //   （`Shell/LeaderboardWindow.cs` 的 `AttachToAnchor` 只 `AttachToAnchor` 就 `return`）⇒ 少了这一句
            //   `Build()` 从不跑、`Army Selector` / `Army Content` / 那颗 `Viewport` 全是 null
            //   ⇒ 本段 **#18–#23 六条**（A769 结构 / 条目吃节点 / 命中区 / A768①）一起红。
            //   对照：本文件 ② 那一处**同一个窗口**是 `Create` **+ `OpenWindow(lb2)`**（`本文件`）⇒ 那边全绿。
            //   ⚠️ 判「实现侧有没有错」别被这一句盖住：`LeaderboardWindow.cs` 的
            //   `_armyContent = Node(vpVc.transform, "Army Content", …)` 与 `CardPresentation/Shell/LeaderboardWindow.cs` 的 `_armyScroll.ClipNode = vpVc;`
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
            //   · 按钮 = 136.36 × 121.59、中心 (317.17, 203.115)；🔴 **可射线的那颗是【子件 `Icon`】**（根上没有任何
            //     Graphic），`Icon` = **115.36 × 102.59**、相对根中心 `(-57.18,-51.80)→(58.18,50.80)`
            //     —— ⚠️ **不居中**：它的中心比按钮中心**偏右 0.5 / 偏上 0.5**（⛔ 别按「居中」推，那会两边各差 0.5）。
            //   · 🔴 **还要加 `Icon` 自己的 `m_RaycastPadding = (-11)⁴`（负 = 外扩）** ⇒ 真值 = **137.36 × 124.59**、
            //     绝对框 = `248.99,140.32→386.35,264.91`。
            //     ⚠️ **出处 `menu_dump.py "Army Item Button"` 印的是【按钮框】那一版**（x1..x2 只到 1 位小数、
            //        宽高到 2 位）—— **射线真值必须再加子件的 `m_RaycastPadding`**（那份由
            //        `rcunion.py bundle_menus_assets_all "Army Item Button" --depth 3` 印出来：`Icon` 的
            //        `Image[RT=1 pad=(-11,-11,-11,-11)]`），⛔ **别照按钮框算**（上一版就栽在这儿）。
            //   · 视口框 = `248.99,147.64→1671.01,258.59` ⇒ 相交之后**上裁 7.32 / 下裁 6.32px**（高 124.59 → 110.95）。
            var a0G = armContentG != null ? FindChildIn(armContentG, CampaignData.Armies[0]) : null;
            var h0G = a0G != null ? FindChildIn(a0G, "Hit") : null;
            float ax1, ay1, ax2, ay2;
            bool okHit = GHitRect(h0G, out ax1, out ay1, out ax2, out ay2);   // `GHitRect` 自带 null 挡（⛔ 别写成 `h0G != null && …`：短路会让这四格**没被赋值**）
            CheckTrue(okHit, "（前提·不静默）第 1 颗军种项的 `Hit` 节点拿得到（军种项 GO 名 = 阵营名）"
                             + " —— ⛔ 取不到就不往下断「量到的值」那几条（不静默变绿）");
            CheckTrue(okHit && NearPx(ax1, 248.99f, 0.5f) && NearPx(ax2, 386.35f, 0.5f)
                             && NearPx(ay1, 147.64f, 0.5f) && NearPx(ay2, 258.59f, 0.5f),
                      "★★★ A768①（条目级 · 态一）：第 1 颗军种项的**命中区** = 子件 `Icon`（外扩 11）∩ 视口框 = "
                      + (okHit ? $"{ax1:F2},{ay1:F2}→{ax2:F2},{ay2:F2}" : "（取不到）")
                      + "，期望 **248.99,147.64→386.35,258.59**（**高 110.95** —— 外扩后那颗真值 137.36 × 124.59，"
                      + "上裁 7.32 / 下裁 6.32px）"
                      + " —— 判据 = 原版 `RectMask2D` 的**射线那一面**（`IsRaycastLocationValid`：框外的点判不中任何东西）。"
                      + "🔴 **改坏法**：① 把 `ViewportClip.Hang` 改回 `Node(...)`（不挂组件）⇒ 命中区回到**外扩后那一整颗**"
                      + "（137.36 × 124.59、上沿 140.32）⇒ 两条 y 边一条都对不上 ⇒ 红；"
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
                          + " —— 🔴 **这条右沿是【节点框】给的、与命中区本身多大无关**：外扩 11 之后那颗右沿 386.35 仍 > 317.17"
                          + " ⇒ 截断值**照旧 317.17**（归真值这一件不动态二）"
                          + " —— 与态一成对：**只有真读节点才会动右沿**（⛔ 只断「态一那个数」是弱断言，分不出两种状态）");
                // 态三：还原节点框 + 重建
                GRestore(armVcG, keepArm);
                if (lbG.ArmyScroll.OnChanged != null) lbG.ArmyScroll.OnChanged();
                var a0r = FindChildIn(armContentG, CampaignData.Armies[0]);
                var h0r = a0r != null ? FindChildIn(a0r, "Hit") : null;
                float cx1, cy1, cx2, cy2;
                bool okBack = GHitRect(h0r, out cx1, out cy1, out cx2, out cy2);
                CheckTrue(okBack && NearPx(cx1, 248.99f, 0.5f) && NearPx(cx2, 386.35f, 0.5f)
                                 && NearPx(cy1, 147.64f, 0.5f) && NearPx(cy2, 258.59f, 0.5f),
                          "★★ A768①（条目级 · 态三）：节点框**还原 + 重建** ⇒ 命中区又回到外扩后那一条（真值宽 137.36，"
                          + (okBack ? $"{cx1:F2},{cy1:F2}→{cx2:F2},{cy2:F2}" : "（取不到）")
                          + "）—— 两态都断 ⇒ 态二那个 317.17 **不可能**来自别的原因（例如「那一页压根没建」）");
            }
            // ============================================================ ①·b 排行榜关窗钮（A1065③ + A1058）
            // 判据（原版 prefab 亲读）= `python -I d:/tmp/wf_hit/rcunion.py bundle_menus_assets_all
            //   "RankedSkirmishLeaderboardPopup" --depth 4`：
            //   根 `Generic Close Button Orange` 那颗 `Image`(`UI_Button_Round_background` 74.39×75.61)
            //   **带 `m_RaycastTarget = 0`** ⇒ **不吃射线**；吃射线的是**两个同矩形的子件**
            //   `Background`(`40k_general_bt_yellow`) 与 `Icon`(`40k_general_bt_yellow_close`)，两颗都带
            //   **`m_RaycastPadding = (-20)⁴`（负 = 外扩）** ⇒ 可点区 = 子件矩形 56.86×58.13 外扩 20
            //   = **原版真值 96.86 × 98.13**（口径见 `资料/普查产出_第四会话/普查_全仓命中区与关闭键族.md` §〇-1）。
            //   换图那一层（`A1058`）= 那颗 `EverguildButton` 的 **`m_TargetGraphic`** 指到的子件 **`Background`**
            //   （pid `7857352521433070052` → 读 `MonoBehaviour_<pid>.json` ⇒ 所属 GO 名 = `Background`、
            //    `m_Sprite` pid `5693181797853584851` → `40k_general_bt_yellow`）。
            //   ⚠️ **两条的改坏法都写在各条 msg 里**（只断「新写法对」不够 —— 见 `CLAUDE.md` §三「灭自证」）。
            {
                var clNodeG = FindChildIn(lbG.transform, "Generic Close Button Orange");
                var clHitG = clNodeG != null ? FindChildIn(clNodeG, "Hit") : null;
                CheckTrue(clHitG != null,
                          "（前提·不静默）排行榜关窗钮的 `Hit` 节点拿得到（`Generic Close Button Orange/Hit`）"
                          + " —— ⛔ 取不到就不往下断「量到的值」那几条（不静默变绿）");
                float hx1, hy1, hx2, hy2;
                bool okCl = GHitRect(clHitG, out hx1, out hy1, out hx2, out hy2);
                CheckTrue(okCl && NearPx(hx2 - hx1, 96.86f, 0.5f) && NearPx(hy2 - hy1, 98.13f, 0.5f),
                          "★★★ A1065③：排行榜关窗钮**命中区** = 吃射线那两颗子件（同矩形 56.86×58.13）"
                          + "按它们自己的 `m_RaycastPadding (-20)⁴` 外扩 = 原版真值 **96.86 × 98.13**"
                          + (okCl ? $"（现读 {hx2 - hx1:F2} × {hy2 - hy1:F2}）" : "（取不到）")
                          + " —— 🔴 **改坏法**：① 改回 `CloseInnerR` 裸矩形（56.86 × 58.13，= `E3b` 那一版）⇒ 每边小 20 ⇒ 红；"
                          + "② 改回根矩形 `CloseR`（74.38 × 75.61，= `E3b` 之前那一版）⇒ 每边小 11.2 ⇒ 红。"
                          + "⛔ 本条与上面 `A768①` 那三条**不同源**（那三条量的是军种条）——两条都在才挡得住「整个 `Build` 没跑」");
                var clWbG = clHitG != null ? clHitG.GetComponent<WindowButton>() : null;
                CheckTrue(clWbG != null && clWbG.target != null && clWbG.target.Texture != null
                          && clWbG.target.Texture.name == "40k_general_bt_yellow"
                          && clWbG.target.gameObject.name == "Background",
                          "★★ A1058：关窗钮**换图那一层** = 子件 `Background`（图 `40k_general_bt_yellow`；"
                          + "= 原版 `m_TargetGraphic` 指到的那一颗）—— 现读「"
                          + (clWbG == null || clWbG.target == null || clWbG.target.Texture == null
                             ? "<没绑>" : clWbG.target.gameObject.name + " / " + clWbG.target.Texture.name)
                          + "」。🔴 **改坏法**：把它传成那颗根圆底盘（`UI_Button_Round_background`；它的节点名现在**就是**"
                          + "`Generic Close Button Orange` 自己 —— ⚠️ **2026-10-18（A1149 第一半）已归真**：改前它挂在一颗"
                          + "自造子件 `Image` 上，那句旧注释里的 `Image` 已作废）"
                          + "⇒ 两个条件同时不成立 ⇒ 红（错因 = 只读 `m_Transition`、没读 `m_TargetGraphic`）");
                Check(clWbG != null && clWbG.HoverTexForTest != null ? clWbG.HoverTexForTest.name : "<null>",
                      "40k_general_bt_yellow_hover",
                      "换图的 `art` 实参 = `40k_general_bt_yellow`（原版 `m_SpriteState.m_HighlightedSprite` 逐字相同；"
                      + "`Shell/LeaderboardWindow.cs` 里由 `Bind` 的表外后备 `+_hover` 推出，⛔ 没有写死第二个字符串）"
                      + "—— 把那个 `art` 实参拿掉 ⇒ 这里取到 `<null>` ⇒ 红");
                // 🆕 **2026-10-18（`A1149` 第二半）**：关窗钮**圆底盘**那颗 quad 的**实绘矩形**两连断
                //   （期望值 = 原版逐字段直读：`python -I d:/tmp/wf_b4probe/pa.py bundle_menus_assets_all
                //   "RankedSkirmishLeaderboardPopup" 8` ⇒ 根自己带 `UI_Button_Round_background` ·
                //   `m_PreserveAspect=1` · 框 **74.38×75.61** · sprite `m_Rect` 237×237 正方 ⇒ 实绘 **74.38×74.38**）。
                //   本窗圆底盘**改前挂点就是对的**（`Shell/LeaderboardWindow.cs` 画在根节点上）⇒ 第 8 实参不传。
                //   🔴 **本格与上面 `A1065③` 那两条不同源**：那两条量的是**命中区**（96.86×98.13）与**换图层**，
                //   一条都不量圆底盘的矩形成像（B5 记的那个静默缺口）。
                A1125CloseBase("LeaderboardWindow", lbG.transform, "Generic Close Button Orange",
                               "UI_Button_Round_background", 74.38f, 74.38f, 75.61f);
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
            // ---- 🔴 2026-10-19（U4 · `N70` 续）：`Army Container` 的悬停目标 = `Army Image Background` ----
            // 判据（逐条实读）：
            //   · `Army Container` 那颗 `EverguildToggle`（MB `-3878836225173759943`）：
            //     `m_Transition = 1 (ColorTint)` · `m_Interactable = 1` · **`m_TargetGraphic` →
            //     pid `-8626703085771958215` = `Army Image Background` 那颗 `Image`**（⛔ 不是 `Hightlight`
            //     —— 那一颗是 `Toggle.graphic`）。四格 = 白 / 0.9608 / 0.7843 / (0.7843, α0.502) · fade 0.1。
            //   · 底图来源 = `PuchasePremiumArmyContainer__Initialize.c`：
            //     `ArmyUtilities.GetArmyMediumBackGround(army)` → `Image.set_sprite(...)` →
            //     `ArmyBackgroundsSO.mediumBackgroundAddressables`（`sharedassets0.assets` 对象 417 逐字节现读：
            //     13 阵营 + `0`，值 = `40K_profile_campaign_bg_*`）。
            //   · 原版 sprite **631×194** vs 容器 **531.23×163.33** ⇒ 比例 3.253 / 3.253（同一族）。
            // 🔴 **改坏法**：① 把映射写成「前缀 + 本仓阵营名」（Goff ⇒ `..._Goff`）⇒ 第一条红；
            //   ② 删掉 `cWb.TintOn(bgQ)` ⇒ 悬停后底图一个字节不变 ⇒ 红；
            //   ③ `TintOn` 接到 `Hightlight`（`Toggle.graphic`，不是 `m_TargetGraphic`）⇒ 底图不变 ⇒ 红。
            CheckTrue(PurchasePremiumWindow.ArmyBackgroundArt(10) == "40K_profile_campaign_bg_UM"
                   && PurchasePremiumWindow.ArmyBackgroundArt(0) == "40K_profile_campaign_bg_UM"
                   && PurchasePremiumWindow.ArmyBackgroundArt(20) == "40K_profile_campaign_bg_Orks"
                   && PurchasePremiumWindow.ArmyBackgroundArt(50) == "40K_profile_campaign_bg_Black_Legion"
                   && PurchasePremiumWindow.ArmyBackgroundArt(70) == "40K_profile_campaign_bg_Tau_Empire"
                   && PurchasePremiumWindow.ArmyBackgroundArt(90) == "40K_profile_campaign_bg_GenestealerCults"
                   && PurchasePremiumWindow.ArmyBackgroundArt(130) == "40K_profile_campaign_bg_SW"
                   && PurchasePremiumWindow.ArmyBackgroundArt(999) == null,
                      "★ 阵营 → 中号底图名 = `ArmyBackgroundsSO` 实读（**5 个不规则尾缀**："
                    + "Goff→Orks · BlackLegion→Black_Legion · TauEmpire→Tau_Empire · Genestealers→GenestealerCults · "
                    + "SpaceWolves→SW；Neutral(0) 与 10 同图；认不出 ⇒ null = 不顶图）");
            {
                var seenBg = new System.Collections.Generic.HashSet<string>();
                bool distinctBg = true;
                foreach (var a in new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130 })
                {
                    var n = PurchasePremiumWindow.ArmyBackgroundArt(a);
                    if (n == null || !seenBg.Add(n)) distinctBg = false;
                }
                CheckTrue(distinctBg && seenBg.Count == 13,
                          "★ 13 个阵营的底图名**互不相同**（挡「漏一个 case ⇒ 落到 default 顶别人的图」）"
                        + $"（现读 {seenBg.Count} 个不同名）");
            }
            // 模板（`o == null`）里那颗节点必须在（原版 prefab 有、`m_Sprite = <无图>`）——
            // 本地那 13 张还没导入 ⇒ **不画**；下面用自检探针把「画出来之后悬停色偏打在哪」验掉。
            CheckTrue(ppwG.ContainerTemplate != null
                   && FindChildIn(ppwG.ContainerTemplate, "Army Image Background") != null,
                      "★ `Army Container` 模板里 `Army Image Background` 节点在（原版 prefab 有这一颗）");
            {
                var bgProbe = new Texture2D(4, 4);
                PurchasePremiumWindow.ArmyBackgroundTexForTest = bgProbe;
                if (ppwG.Scroll.OnChanged != null) ppwG.Scroll.OnChanged();     // = `RebuildContainers`
                var pc0 = ppwG.Containers.Count > 0 ? ppwG.Containers[0] : null;
                var pc1 = ppwG.Containers.Count > 1 ? ppwG.Containers[1] : null;
                var bg0 = pc0 != null ? FindChildIn(pc0, "Army Image Background") : null;
                var bg1 = pc1 != null ? FindChildIn(pc1, "Army Image Background") : null;
                var q0 = bg0 != null ? bg0.GetComponent<ImageQuad>() : null;
                var q1 = bg1 != null ? bg1.GetComponent<ImageQuad>() : null;
                var hit0 = pc0 != null ? FindChildIn(pc0, "Hit") : null;
                var wb0 = hit0 != null ? hit0.GetComponent<WindowButton>() : null;
                CheckTrue(q0 != null && q0.Texture == bgProbe,
                          "★ 底图画出来了：`Army Container/Army Image Background` 是 `ImageQuad`、贴的正是该阵营那张"
                        + "（自检探针；`Tooltip` 那一族的 `_missArt` 在真缺图时会出声）");
                CheckTrue(q1 != null && wb0 != null,
                          "（前提 · 不静默）第 2 个容器也有底图、第 1 个容器的命中区上有 `WindowButton`（取不到 ⇒ 下面三条不跑）");
                if (q0 != null && q1 != null && wb0 != null)
                {
                    wb0.Enter();
                    CheckNear(q0.Tint.g, 0.9607843f, 1e-4f,
                              "★ 悬停 ⇒ **底图**色键 = 0.9608（原版 `m_Colors.m_HighlightedColor`；`m_Transition=1` 的落点 = `m_TargetGraphic` = 这颗底图）");
                    CheckNear(q1.Tint.g, 1f, 1e-4f,
                              "★ 灭自证：**另一格**的底图仍是 1.0（`TintOn` 只接自己那一颗 ⇒ 挡「把整列一起染了」）");
                    wb0.Press();
                    CheckNear(q0.Tint.g, 0.7843137f, 1e-4f, "★ 按下 ⇒ 0.7843（原版 `m_PressedColor`）");
                    wb0.Release(); wb0.Exit();
                    CheckNear(q0.Tint.g, 1f, 1e-4f, "★ 抬起 + 移开 ⇒ 回 **1.0**（灭自证：恒高亮 ⇒ 红）");
                }
                PurchasePremiumWindow.ArmyBackgroundTexForTest = null;
                if (ppwG.Scroll.OnChanged != null) ppwG.Scroll.OnChanged();     // 还原成「无图 ⇒ 只建节点」
                UnityEngine.Object.DestroyImmediate(bgProbe);
            }

            // ---- 🔴 2026-10-19（U5 · 批次 7）：`Army Info/Army Icon` 的**运行期灌图** ----
            // 判据（原版运行期那条链，逐字节现读）：
            //   · `PuchasePremiumArmyInfo__Initialize.c:24-33`：`armyIcon.sprite = ArmyUtilities.GetArmyIcon(army)`
            //     （`army` = 报价数据 `+0x10 → +0x2c`）。
            //   · `ArmyIconsSO.GetArmyIcon`（`ArmyIconsSO__GetArmyIcon.c`）= 遍历 `armyIcons` 列表、
            //     逐条 `TryGetItem(army)`；**表里没有 `Neutral(0)`** ⇒ 0/认不出返回 null。
            //   · 那份 SO 的落点 = `sharedassets0.assets` 对象 **422**（`m_Script = {1,660}` = `ArmyIconsSO`，
            //     **`m_Name = "IconsByArmySO"`** —— ⚠️ 资产名 ≠ 类名，这就是 U4 §五·2 没搜到它的原因）；
            //     `[count = 13]` + 13 条 `[int army][PPtr]`，PPtr 逐个解出来 = `Core/ArmyIcons.Table`。
            //     判据链全文 → `Core/ArmyIcons.cs` 文件头。
            // 🔴 **改坏法**：① 把 `Genestealers` / `SpaceWolves` 也拼上 `Faction` 前缀 ⇒ 下面第一条红；
            //   ② 只在 `Build()` 那一刻画一次、`SelectArmy` 不换图 ⇒ 第二条红（那是「静态能过、运行期错」那一档）；
            //   ③ 把 `Neutral` 兜底成某个阵营的图 ⇒ 第三条红。
            CheckTrue(ArmyIcons.IconName(10) == "40k_DeckSelection_icon_FactionUM"
                   && ArmyIcons.IconName(20) == "40k_DeckSelection_icon_FactionOrks"
                   && ArmyIcons.IconName(90) == "40k_DeckSelection_icon_Genestealers"
                   && ArmyIcons.IconName(130) == "40k_DeckSelection_icon_SpaceWolves"
                   && ArmyIcons.IconName(0) == null && ArmyIcons.IconName(999) == null
                   && ArmyIcons.IconName("Goff") == "40k_DeckSelection_icon_FactionOrks"
                   && ArmyIcons.IconName("Nope") == null,
                      "★ `CardArmy` → 徽记图名 = 原版 `IconsByArmySO` 逐条实读（13 条）——"
                    + "含两条**不带 `Faction`** 的（`Genestealers` / `SpaceWolves`）、`Goff→Orks` 那条改名，"
                    + "以及 `Neutral(0)` / 认不出 ⇒ `null`（原版 `TryGetItem` 失败那条路）");
            {
                var allIc = ArmyIcons.All;
                var seenIc = new System.Collections.Generic.HashSet<string>();
                bool distinctIc = true;
                foreach (var e in allIc)
                    if (string.IsNullOrEmpty(e.Sprite) || !seenIc.Add(e.Sprite)) distinctIc = false;
                CheckTrue(allIc.Length == 13 && distinctIc,
                          "★ 13 条阵营徽记**互不相同**、条数 = 13（原版 SO 的 `count` 实读 13；"
                        + "挡「漏一条 ⇒ 两条落同一张图」）" + $"（现读 {allIc.Length} 条 / {seenIc.Count} 个不同名）");
            }
            CheckTrue(ArmyIcons.Faction(20) == "Goff" && ArmyIcons.Id("Goff") == 20 && ArmyIcons.Id("Nope") == -1
                   && ArmyIcons.LocalizedName(20) == CardText.Faction("Goff"),
                      "★ 同一张表的另外三列：`20 ↔ Goff` 双向、认不出 ⇒ **-1**（⛔ 不是 0，0 是合法的 Neutral）、"
                    + "本地化名走 `CardText.Faction`（= 原版 `ArmyUtilities.GetLocalizedArmyName` 的 `Armies/<枚举名>`）");
            CheckTrue(DeckRuntime.FactionIcon("Goff") == "40k_DeckSelection_icon_FactionOrks"
                   && DeckRuntime.FactionIcon("Nope") == "40k_collection_bt_decks"
                   && DeckRuntime.FactionIcon(null) == "40k_collection_bt_decks",
                      "★ `DeckRuntime.FactionIcon` 仍保留**那一层兜底**（认不出 ⇒ 卡组图标 `40k_collection_bt_decks`），"
                    + "映射本体已收口到 `Core/ArmyIcons`（⛔ 别把 13 条 case 抄回去）");
            // 真实窗口：第 0 条报价 = `Ultramarines(10)` ⇒ 图块贴 UM 那张（清单里 13 张全部已导入
            // `Resources/Art/ui_deck/`，2026-10-19 现数 13/13 ⇒ 这一条在本地**取得到真图**）。
            var icNodeU = ppwG.ArmyIconNode;
            var icq0 = ppwG.ArmyIconQuad;
            CheckTrue(icNodeU != null && FindChildIn(icNodeU, "Image") != null
                      && icq0 != null && icq0.Texture != null,
                      "★（前提·不静默）`Army Info/Army Icon` 节点在、图块 `Image` 是它的子件（原版那一颗就是 `Image` 自己）"
                    + $"（现读 node={(icNodeU != null)} / quad={(icq0 != null)}"
                    + $"/ tex={(icq0 != null && icq0.Texture != null ? icq0.Texture.name : "<null>")}）");
            Check(icq0 != null && icq0.Texture != null ? icq0.Texture.name : "<null>",
                  "40k_DeckSelection_icon_FactionUM",
                  "★ `Army Info/Army Icon` = 第 0 条报价（`Ultramarines = 10`）的徽记 —— 原版 "
                + "`PuchasePremiumArmyInfo.Initialize` 的 `armyIcon.sprite = GetArmyIcon(army)`；⛔ 改坏法：只建节点不画 / 画成别的阵营 ⇒ 红");
            ppwG.SelectArmy(1);
            Check(ppwG.ArmyIconQuad != null && ppwG.ArmyIconQuad.Texture != null ? ppwG.ArmyIconQuad.Texture.name : "<null>",
                  "40k_DeckSelection_icon_FactionOrks",
                  "★ 选中第 1 条（`Goff = 20`）⇒ `Army Icon` **运行期换图**（Goff 的图名是 `FactionOrks`，"
                + "⛔ 不是 `FactionGoff`）—— 改坏法：只在 `Build()` 画一次 ⇒ 这里仍是 UM ⇒ 红");
            ppwG.SelectArmy(0);
            Check(ppwG.ArmyIconQuad != null && ppwG.ArmyIconQuad.Texture != null ? ppwG.ArmyIconQuad.Texture.name : "<null>",
                  "40k_DeckSelection_icon_FactionUM",
                  "★ 换回第 0 条 ⇒ 图又回 UM（灭自证：挡「换图是单向的 / 缓存了第一条」）");
            ppwG.Close();          // 先关掉本窗，再开下面那扇（避免两扇同时挂着干扰窗口管理器）
            {
                // `Neutral(0)`（原版表里没有）+ `ArmyName = null`（原版由 army 值本地化）两件事一起验。
                var ppwN = PurchasePremiumWindow.Create(shell.Windows, new[]
                {
                    new PurchasePremiumWindow.ArmyOffer { Army = 0, ArmyName = null,
                                                          PriceText = "0", PremiumUnlocked = false },
                    new PurchasePremiumWindow.ArmyOffer { Army = 130, ArmyName = null,
                                                          PriceText = "0", PremiumUnlocked = false },
                });
                ppwN.FinishPopForTest();
                CheckTrue(ppwN.ArmyIconNode != null && ppwN.ArmyIconQuad == null,
                          "★ `Army = Neutral(0)` ⇒ `Army Icon` **只留节点、不画图**（原版 `IconsByArmySO` 没有 0 那条；"
                        + "⛔ 改坏法：兜底成某个阵营的图 ⇒ 红）");
                ppwN.SelectArmy(1);
                Check(ppwN.ArmyIconQuad != null && ppwN.ArmyIconQuad.Texture != null ? ppwN.ArmyIconQuad.Texture.name : "<null>",
                      "40k_DeckSelection_icon_SpaceWolves",
                      "★ `Army = SpaceWolves(130)` ⇒ 贴 `40k_DeckSelection_icon_SpaceWolves`（那条**不带 `Faction`**）");
                var nmNd = ppwN.Containers.Count > 1 ? ppwN.Containers[1] : null;
                var nmLb = nmNd != null ? FindChildIn(nmNd, "Army Name") : null;
                var nmLbC = nmLb != null ? nmLb.GetComponent<Label>() : null;
                Check(nmLbC != null ? nmLbC.Text : "<null>",
                      CardText.Faction("SpaceWolves"),
                      "★ `ArmyName` 传空 ⇒ 容器 `Army Name` **按 army 值本地化**（原版 `PuchasePremiumArmyContainer.Initialize` "
                    + "就是 `I2(string.Format(<键>, army))`；⛔ 改坏法：退回 prefab 占位串 `Ultramarines` ⇒ 红）");
                ppwN.Close();
            }

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

            // ---- 🔴 2026-10-19（U5 · 批次 7）：`SimpleArmyImage` 的 `Army Icon` 运行期灌图 ----
            // 判据 = `SimpleArmyImage.Initialize`：`armyIcon.sprite = ArmyIconsSO.GetArmyIcon(army)` +
            //   `armyIcon.enabled = (sprite != null)`；prefab 那一格 `m_PreserveAspect = 1` · 框 222×223。
            //   映射 = `Core/ArmyIcons`（原版 `IconsByArmySO` 逐字节解出）。🔴 改坏法：退回「只建节点」⇒ 两条红。
            {
                Transform RCard(int idx)
                {
                    return rreG.ContentNode != null && rreG.ContentNode.childCount > idx
                         ? rreG.ContentNode.GetChild(idx) : null;
                }
                var ri0 = FindChildIn(RCard(0), "Army Icon");
                var ri1 = FindChildIn(RCard(1), "Army Icon");
                var rq0 = ri0 != null ? ri0.GetComponent<ImageQuad>() : null;
                var rq1 = ri1 != null ? ri1.GetComponent<ImageQuad>() : null;
                Check(rq0 != null && rq0.Texture != null ? rq0.Texture.name : "<null>",
                      "40k_DeckSelection_icon_FactionUM",
                      "★ 第 1 张阵营卡（`Army = 10 Ultramarines`）的 `Army Icon` = 运行期灌图的徽记"
                    + "（原版 `SimpleArmyImage.Initialize` 的 `armyIcon.sprite = GetArmyIcon(army)`）"
                    + " —— ⛔ 改坏法：退回「只建节点、不画」⇒ 本条红");
                Check(rq1 != null && rq1.Texture != null ? rq1.Texture.name : "<null>",
                      "40k_DeckSelection_icon_FactionOrks",
                      "★ 第 2 张（`Army = 20 Goff`）贴 `FactionOrks`（⛔ 不是 `FactionGoff`）——"
                    + " **两张不同**这条同时挡「所有卡都顶第一张图」");
            }

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

        // ============================================================ ★ A964：命中区覆盖探针（外壳侧）
        //
        // 判据（`资料/待办判据_1018.md` §A964 的一句话）：**凡参与点击命中的节点，
        //   它的【命中区矩形】必须覆盖它【画出来的矩形】。**
        //   · ⚠️ **量 quad、不量节点** —— 本仓**没有 UGUI `EventSystem`**（`Battle/CombatCameraZoom.cs` 的 `EventSystem` 实证），
        //     命中是 `Shell/PointerLayer.cs` 自己算世界坐标/矩形的；节点的 rect 与真正画出来的四边形**可以不一致**。
        //   · ⚠️ **要命中就必须带 `ImageQuad`**（`HitQuad` 走 `GetComponentInChildren<ImageQuad>`）。
        //   · ⚠️ **分层别靠 z**（用 `RenderQueue`）—— 与命中无关，但同一次普查里顺带核 E3。
        //
        // 🔴 **本段是【报告式】探针**（A964 续 ② 的裁定）：E1 / E3 **只列候选 + 落点/实绘证据、不做普适断言**
        //   —— 「画出来的矩形」在场景树里**没有唯一答案**：装饰件会**叠在同一矩形**上
        //   （实据 = `Shell/CampaignTab.cs` 的 `localScale` 的 `Premium Mark` 用 `localScale=(2,2,1)` 画在同一个 `r` 上）
        //   ⇒ 硬套「覆盖」判据会**假红**。逐条判由调度台做，确认下来的才补结构性断言。
        //
        // 🔴 **E2（`vis ≠ hr` 那一族）本段【故意不上】**：`MenuDraw.Hit` 那 46 处调用点里 `vis ≠ hr` 的
        //   **一处都没逐处核过**（侦察 §7·3）⇒ 上了就是一片假红。E2 只在战斗侧那一份探针里上。
        //
        // 🔴 **为什么必须先【逐扇窗开一次】再扫**：`PointerLayer.HitQuad` 恒 `null` 有**三个**原因
        //   （按符号认那六行，⛔ 别抄行号）：
        //     ① 钮自己不 `activeAndEnabled`（而且 `AllButtons()` 是 `FindObjectsInactive.Exclude`
        //        ⇒ **关着的窗里的钮根本不在表里**）；
        //     ② 它的子树里**没有 `ImageQuad`** —— **这才是 A8 那一族真缺陷**；
        //     ③ 有 quad、但 quad 不在激活链上（**多半是「那扇窗没开」**）。
        //   ⇒ 不逐扇开就**把 ③（窗没开）报成 ②（缺 quad）= 一片假红**。
        //   做法：`CloseAllWindows()` → 逐扇 `OpenWindow` → 扫 → 关，**一次只开一扇**
        //   （被压到 `Background` 的窗按 `PointerReachable` **本来就不参与指针命中**，不算进「不可命中」的账）。
        //
        // 🆕 **2026-10-18（A1001）：那一步之前先跑一张【建窗表】。** 逐扇开只覆盖「**早先某段顺手建过**」
        //   的那些窗 ⇒ 覆盖面是**副作用**（现核 = 真覆盖 **15** 个窗类 / 全库子类 **38** 个；账面那个 16
        //   还是假数，见下面 `ledger` 那道闸）。建窗表把 22 个「连实例都没有」的窗类**逐个建一遍**
        //   （6 扇走 `WindowsManager` 的 `Open*`、13 扇走 `Create(mgr)`、3 扇带小夹具），
        //   建完交给同一条 `FindObjectsByType` 现扫路，扫完 `DestroyImmediate` 拆掉。
        //   ⚠️ 逐条建成功/失败**都打**；建不出来的那条**留在 `notHere` 里**（覆盖率如实偏小，⛔ 不假装扫过）。
        //   判据与备选案（反射 / 只标抽样，都已否）→ `资料/普查产出_第四会话/查证_探针覆盖率与字体口径.md` §1。
        //
        // ⚠️ **截图截不出这一类缺陷**（「看着在钮上、点不动」在静态截图上完全正常）⇒ 本段**只打日志 + TSV**。
        //
        // 📌 输出 → `d:/4/_tmp_view/hitprobe/shell_hits.tsv`；日志打 **`扫了 N 颗 / E1 报 M / E3 报 K`**
        //    （**N 必打**，否则「0 条」不可信）+ **没覆盖到的窗类逐条**（`GameWindow` 子类 ≥20、构造入口各不相同）。
        Section("★ A964：命中区覆盖探针（外壳侧 · E1 不可命中 / E3 同队列求交 / 两条结构断言）");
        {
            const string HitProbeDir = "d:/4/_tmp_view/hitprobe";
            Directory.CreateDirectory(HitProbeDir);

            var rows = new List<string>();
            var e1 = new List<string>();          // E1：报了「参与命中却没有命中区」
            var e3 = new List<string>();          // E3：同队列的命中区相交（谁压谁）
            var badAnchor = new List<string>();   // 结构断言①：命中 quad 的 `anchor` 不是 (0.5,0.5)
            var sliceKid = new List<string>();    // 结构断言②：命中 quad 是九宫格/平铺的子块
            var covered = new List<string>();     // 开起来、真扫过的窗类
            var hasInstance = new List<string>(); // **本场景里有实例**的窗类（≠ 扫得到）
            var failed = new List<string>();      // 本场景有实例、但这一扇**没开起来**
            var emptyWin = new List<string>();    // 开起来了，但一颗 `WindowButton` 都没有（如实报，⛔ 不冒充「没问题」）
            int scanned = 0;                      // N：真扫过的钮数（**必打**）

            string Row(params string[] cells) { return string.Join("\t", cells); }
            const string Dash = "-";

            // 一个节点的**完整路径**（从壳根一路下来）—— 报告里唯一的可复查坐标。
            string NodePath(Transform t)
            {
                var parts = new List<string>();
                for (var p = t; p != null; p = p.parent) parts.Add(p.name);
                parts.Reverse();
                return string.Join("/", parts.ToArray());
            }

            // 结构断言②的判据：这颗 quad 是不是**九宫格 / 平铺的子块**。
            // 🔴 命名契约来自那两个工厂本身（`Battle/ImageQuad.cs` 的 `CreateNineSlice` / `CreateTiled`：
            //  子件一律叫 `{根名}_{i}{j}`、而**根上没有 quad**）⇒ `HitQuad` 的 `GetComponentInChildren`
            //  会取到**第一块角块** ⇒ 命中区只剩一个角（症状同 A8：看着在钮上、点不动）。
            // 🧨 **改坏法**：把某颗命中节点的子树换成 `ImageQuad.CreateNineSlice(...)` ⇒ 这条立刻报。
            bool IsSliceOrTileCell(ImageQuad q)
            {
                var pt = q != null ? q.transform.parent : null;
                if (pt == null) return false;
                string nm = q.name, pn = pt.name;
                return nm.Length == pn.Length + 3 && nm.StartsWith(pn, System.StringComparison.Ordinal)
                       && nm[pn.Length] == '_' && char.IsDigit(nm[pn.Length + 1]) && char.IsDigit(nm[pn.Length + 2]);
            }

            // 一颗钮「**会不会被 E1 报出来**」—— **全段唯一一份判据**（主扫与两条判别式夹具都走它）。
            // 三条筛选照生产：`absorbOnly`（= `Navigable` 那一句）· 钮自己要活着 · 命中 quad 取不到。
            // 🔴 **它量的是【生产那一份】**（`PointerLayer.HitQuadForTest` 严格转发私有的 `HitQuad`）——
            //  ⛔ 本段**不抄 `HitQuad` 的算式**：抄了就是「改坏实现时探针跟着一起错」= 自证。
            // 🧨 **改坏法**：把 `Shell/PointerLayer.cs` 的 `HitQuad` 换成「直接 `GetComponentInChildren<ImageQuad>()`」
            //  （不看激活链）⇒ 判别式②的**态二**不再报 ⇒ 那一对（报/不报）立刻裂开。
            bool WouldReport(WindowButton b)
                => b != null && !b.absorbOnly && b.isActiveAndEnabled
                   && PointerLayer.HitQuadForTest(b) == null;

            // 扫一个**作用域**（`win == null` = 常驻树 / 不属于任何窗的那些钮；否则 = 那一扇窗里的钮）。
            // 🔴 全程走 `PointerLayer.AllButtonsForTest()`（= **生产那张表**，`FindObjectsByType` 现扫）
            //  + `HitBoxForTest` / `HitQuadForTest`（= 生产算式）⇒ 判据只有一份。
            void ScanScope(string winName, string state, GameWindow win)
            {
                var all = PointerLayer.AllButtonsForTest();
                // 这一档里「可命中」的那些（E3 要按队列两两求交，所以先收起来）
                var live = new List<WindowButton>();
                var lq = new List<int>();
                var lr = new List<float[]>();
                for (int i = 0; i < all.Length; i++)
                {
                    var b = all[i];
                    if (b == null || b.absorbOnly) continue;                          // 吸收层不是按钮（照 `Navigable` 那句）
                    if (b.GetComponentInParent<GameWindow>(true) != win) continue;    // 只算本作用域的
                    scanned++;

                    var q = PointerLayer.HitQuadForTest(b);
                    if (q == null)
                    {
                        // ---- E1：**参与命中却没有命中区**。🔴 必须说清是三条原因里的哪一条
                        //      （否则「窗没开」会冒充「缺 quad」—— 侦察点名的那片假红）
                        string why;
                        var anyQuad = b.GetComponentInChildren<ImageQuad>(true);      // 含 inactive ⇒ 专门用来分 ②/③
                        if (!b.isActiveAndEnabled) why = "① 钮自己不 activeAndEnabled";
                        else if (anyQuad == null) why = "② 子树里根本没有 ImageQuad（**A8 那一族真缺陷**）";
                        else why = "③ 有 quad 但不在激活链上（多半是窗没开）";
                        e1.Add($"[{winName}] {NodePath(b.transform)} —— {why}");
                        rows.Add(Row(winName, state, NodePath(b.transform),
                                     Dash, Dash, Dash, Dash, Dash, Dash, Dash, Dash,
                                     Dash, Dash, Dash, Dash, Dash, "E1 " + why));
                        continue;
                    }

                    if (q.anchor != new Vector2(0.5f, 0.5f))
                        badAnchor.Add($"[{winName}] {NodePath(b.transform)} —— anchor=({q.anchor.x:F3},{q.anchor.y:F3})");
                    if (IsSliceOrTileCell(q))
                        sliceKid.Add($"[{winName}] {NodePath(b.transform)} —— quad=`{q.name}` 父=`{q.transform.parent.name}`");

                    if (!PointerLayer.HitBoxForTest(b, out var c, out var hh, out _)) continue;   // 与上面同一判据，理论上到不了
                    float hx1 = c.x - hh.x, hy1 = c.y - hh.y, hx2 = c.x + hh.x, hy2 = c.y + hh.y;
                    var r = new float[] { hx1, hy1, hx2, hy2 };
                    // 实绘矩形：`QuadPxRect` 报的是**设计帧**（🆕 `A1003` 起已收口到 `MenuDraw.QuadRectPx`：
                    //   中心先 `PosInDesignSpace` 除回父级缩放、**尺寸项不除**），
                    //   而 `HitBoxPx` 报的是**指针那一帧**（中心走裸世界坐标、尺寸项 × `lossyScale`，A167）
                    // ⇒ 窗根被乘 M（或本窗根自己带缩放 —— 如高级版窗的 pop 动画在批处理里停在 0.8）时两帧分家
                    //   —— 那正是本探针要盯的一档（`M == 1` 且父链无缩放时两式逐位相同）。
                    float dx1, dy1, dx2, dy2;
                    bool okD = QuadPxRect(q, out dx1, out dy1, out dx2, out dy2);
                    rows.Add(Row(winName, state, NodePath(b.transform),
                                 $"{hx1:F2}", $"{hy1:F2}", $"{hx2:F2}", $"{hy2:F2}",
                                 okD ? $"{dx1:F2}" : Dash, okD ? $"{dy1:F2}" : Dash,
                                 okD ? $"{dx2:F2}" : Dash, okD ? $"{dy2:F2}" : Dash,
                                 okD ? $"{hx1 - dx1:F2}" : Dash, okD ? $"{hy1 - dy1:F2}" : Dash,
                                 okD ? $"{dx2 - hx2:F2}" : Dash, okD ? $"{dy2 - hy2:F2}" : Dash,
                                 $"{q.RenderQueue}", ""));
                    live.Add(b);
                    lq.Add(q.RenderQueue);
                    lr.Add(r);
                }

                // ---- E3：**同一个渲染队列**的命中区两两求交 ⇒ 谁压谁由 z 决定（队列同号时是**枚举顺序**）
                //   ⚠️ 只比**命中区之间**（文字层不带命中区 ⇒ 不踩 `MenuDraw.cs` 那条「`Absorb` 与文字档同号是故意的」）
                for (int i = 0; i < live.Count; i++)
                    for (int j = i + 1; j < live.Count; j++)
                    {
                        if (lq[i] != lq[j]) continue;
                        float ox = Mathf.Min(lr[i][2], lr[j][2]) - Mathf.Max(lr[i][0], lr[j][0]);
                        float oy = Mathf.Min(lr[i][3], lr[j][3]) - Mathf.Max(lr[i][1], lr[j][1]);
                        if (ox <= 0.5f || oy <= 0.5f) continue;      // ≤0.5px 当相切（浮点残差，不是缺陷）
                        e3.Add($"[{winName}] 队列 {lq[i]}：{NodePath(live[i].transform)} ⇄ "
                               + $"{NodePath(live[j].transform)} —— 交 {ox:F2}×{oy:F2}px");
                    }
            }

            // ============================================================ 🆕 A1001：**建窗表**（探针前先逐类建一遍）
            //
            // 🔴 **为什么要它**：上面这段扫描只覆盖「本场景**当下有实例**」的 `GameWindow`，而实例是
            //   前面各探针段**顺手建**出来的（18 处、互相不相干）⇒ 覆盖面是**副作用**：任何一段被挪走 /
            //   删掉，覆盖率就**悄悄**变，而日志只会打个数。现核（`资料/普查产出_第四会话/
            //   查证_探针覆盖率与字体口径.md` §1·2）：真覆盖 **15** 个窗类 / 全库子类 **38** 个
            //   （账面那个 16 是**假数**：`hasInstance` 把夹具用的**裸 `GameWindow`** 也入了账，
            //   见下面那条 `ledger` 闸）。⇒ 把「建哪些窗」**显式写成一张表**，让覆盖面对改动免疫。
            //    🔴 **调度台已裁 = 方案 A**（另两案为什么不行 → 同那份查证 §1·4）。
            //
            // 🔴 **为什么不走反射**（方案 B，已否）：`Create` 的签名不齐（3 个带必填参、`TrophyInfoPopup`
            //   那个叫 `Open`）⇒ 反射只会**静默跳过**，正是本仓最忌的那种失败形态。
            // 🔴 **为什么不能只标「抽样」**（方案 C，已否）：22 个窗类永远是「没扫过」⇒ 账清不掉。
            //
            // ⚠️ **建法一律照生产那条路**：能走 `WindowsManager` 那 7 个 `Open*` 帮助函数的走它
            //   （`Shell/WindowsManager.cs`，= 原版菜单那几条开窗路的收编点），其余走各类自己的
            //   `Create(mgr)`；3 个带必填参的给**小夹具**（逐条判据与出处 → 那份查证 §1·3）。
            // ⚠️ **用完必须 `DestroyImmediate`**（批处理没有帧循环 ⇒ `Destroy` 不生效），而且只能在
            //   **本段扫描跑完之后**销毁 —— 它们要靠下面那条 `FindObjectsByType` 现扫进 `wins`。
            // ⚠️ 建失败的那几条**如实打**（红线：不许静默）⇒ 它会留在 `notHere` 里、覆盖率数字**如实偏小**，
            //   ⛔ 不是「假装扫过」。
            var allTypes = new List<string>();          // 全库 `GameWindow` 的**真子类**（非抽象）
            foreach (var t in typeof(GameWindow).Assembly.GetTypes())
                if (!t.IsAbstract && t.IsSubclassOf(typeof(GameWindow))) allTypes.Add(t.Name);
            allTypes.Sort();

            var a1001Built = new List<GameWindow>();
            var a1001Bad = new List<string>();
            var a1001Table = new (string Name, System.Func<WindowsManager, GameWindow> Make)[]
            {
                // ---- 6 扇：走 `WindowsManager` 的现成帮助函数（参数全用出厂值 / 空数据那一档）----
                ("BaseOfferPopup",                    m => WindowsManager.OpenBaseOfferPopup()),
                ("GenericOptionsPanel",               m => WindowsManager.OpenGenericOptionsPanel()),
                ("AllianceMemberOptionsPopup",        m => WindowsManager.OpenAllianceMemberOptions()),
                ("RankedRewardEventWindow",           m => WindowsManager.OpenRankedRewardEvent()),
                ("ReferralPopupWindow",               m => WindowsManager.OpenReferralPopup()),
                ("EnergySinglePlayerOnlyEventWindow", m => WindowsManager.OpenEnergyEvent()),
                // ---- 11 扇：`Create(mgr)` 单参 ----
                ("BoosterInfoPopup",     m => BoosterInfoPopup.Create(m)),
                ("BoosterPackOpenWindow", m => BoosterPackOpenWindow.Create(m)),
                ("CampaignRewardWindow", m => CampaignRewardWindow.Create(m)),
                ("DailyRewardPopup",     m => DailyRewardPopup.Create(m)),
                ("DailyStreakPopup",     m => DailyStreakPopup.Create(m)),
                ("ImportDeckPopup",      m => ImportDeckPopup.Create(m)),
                ("MissionRerollPopup",   m => MissionRerollPopup.Create(m)),
                ("RewardWindow",         m => RewardWindow.Create(m)),
                ("CardDetailPopup",      m => CardDetailPopup.Create(m)),
                ("ShopWindow",           m => ShopWindow.Create(m)),
                ("CollectionWindow",     m => CollectionWindow.Create(m)),
                // ---- 2 扇：可选参 → 传出厂值那一档（`onPicked` 不给 = 原版「没有回调」那一支）----
                ("DeckSelectionPopup",   m => DeckSelectionPopup.Create(m)),
                ("TrophyInfoPopup",      m => TrophyInfoPopup.Open(m)),
                // ---- 3 扇要小夹具 ----
                //  `DeckInfoPopup`：`(mgr, deckIndex, state)` ⇒ 索引给 0、态给 `Edit`（出厂值）
                //  `SearchingOpponentWindow`：`(mgr, deckIndex)` ⇒ 0（官方样例 → `RankedEventWindow.cs` 的 `SearchingOpponentWindow`）
                //  `DuelPopupWindow`：`(mgr, opponent)` ⇒ 对手名给 `"Goff"`（同族样例里的那一串）
                ("DeckInfoPopup",           m => DeckInfoPopup.Create(m, 0, DeckInfoPopup.DeckInfoState.Edit)),
                ("SearchingOpponentWindow", m => SearchingOpponentWindow.Create(m, 0)),
                ("DuelPopupWindow",         m => DuelPopupWindow.Create(m, "Goff")),
            };
            shell.Windows.CloseAllWindows();             // 建之前先清场（有几扇 `Create` 会顺手 `Open()`）
            var a1001Names = new List<string>();         // 逐类列名（给日志那一行；`ConvertAll` 对命名元组不灵）
            for (int i = 0; i < a1001Table.Length; i++)
            {
                var ent = a1001Table[i];
                a1001Names.Add(ent.Name);
                try
                {
                    var made = ent.Make(shell.Windows);
                    if (made == null) a1001Bad.Add(ent.Name + "（`Create` 返回 null）");
                    else a1001Built.Add(made);
                }
                catch (System.Exception ex)
                {
                    // ⛔ 不静默：建不出来的那条逐条打出来，并且**不假装它扫过了**
                    a1001Bad.Add(ent.Name + $"（建窗抛 {ex.GetType().Name}: {ex.Message}）");
                }
            }
            Debug.Log(P + $"   A1001：建窗表 **{a1001Built.Count}/{a1001Table.Length} 扇建起来了**"
                      + "（逐类：" + string.Join("、", a1001Names.ToArray()) + "）"
                      + (a1001Bad.Count > 0 ? "；⚠️ **建不出来的**：" + string.Join(" ；", a1001Bad.ToArray()) : ""));
            shell.Windows.CloseAllWindows();             // 建的过程中可能把主窗顶掉 ⇒ 收回来（第 0 步还会再清一次）

            // ---- 第 0 步：**常驻树**（不属于任何窗的那些钮 —— `PointerReachable` 说它们一直可点）
            shell.Windows.CloseAllWindows();
            ScanScope("(常驻·不属于任何窗)", "始终可点", null);

            // ---- 第 1..N 步：**逐扇窗开一次再扫**（一次只开一扇）
            var wins = Object.FindObjectsByType<GameWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < wins.Length; i++)
            {
                var w = wins[i];
                if (w == null) continue;
                string tn = w.GetType().Name;
                // 🔴 **2026-10-18（A1001）**：只有 `GameWindow` 的**真子类**才**入账** —— 那一批
                //   **裸 `GameWindow` 夹具**（前面各段 `AddComponent<GameWindow>()` 建的）**不是子类**，
                //   计进 `covered` 就是那个「覆盖 16 / 全库 38」的**假数**（16 + 23 = 39 > 38 这个
                //   算术不自洽就是它的症状；订正 → 上面那份查证 §1·2）。
                //   ⚠️ **只闸「记账」，不闸「扫描」**：那几颗夹具上的钮照旧扫、照旧进 TSV/E1/E3
                //   （它们本来就在读数里 —— 少扫会**丢证据**），只是不再冒充「一个窗类被覆盖了」。
                bool ledger = allTypes.Contains(tn);
                if (ledger && !hasInstance.Contains(tn)) hasInstance.Add(tn);

                shell.Windows.CloseAllWindows();          // 隔离：上一扇不许留在场上（一次只开一扇）
                string why = null;
                try
                {
                    shell.Windows.OpenWindow(w);          // 走**生产那条开窗路**
                    if (!w.gameObject.activeSelf) why = "`OpenWindow` 之后物体仍不活";
                    else if (w.CurrentState != WindowState.Open) why = $"`OpenWindow` 之后 `CurrentState={w.CurrentState}`（不是 `Open`）";
                }
                catch (System.Exception ex)
                {
                    why = $"开窗抛异常 {ex.GetType().Name}: {ex.Message}";
                }
                if (why != null)
                {
                    if (ledger) failed.Add($"{tn}（{why}）");
                    Debug.LogWarning(P + $"   A964：`{tn}` **没覆盖到** —— {why}"
                                          + (ledger ? "" : "（⚠️ 它不是 `GameWindow` 的子类 ⇒ 不计入覆盖率账目）"));
                    try { shell.Windows.CloseAllWindows(); } catch { }
                    continue;
                }

                int before = scanned;
                ScanScope(tn, "窗:Open", w);
                // 🔴 A1001：`covered` / `emptyWin` 都只认**真子类**（裸 `GameWindow` 夹具不入账，见上面那句话）
                if (ledger && !covered.Contains(tn)) covered.Add(tn);
                if (ledger && scanned == before) emptyWin.Add(tn + "（开出来了，但**一颗 `WindowButton` 都没有**）");
                try { shell.Windows.CloseAllWindows(); } catch { }
            }
            shell.Windows.CloseAllWindows();

            // ---- 🆕 A1001：**拆掉建窗表建出来的那些**（⛔ 批处理没有帧循环 ⇒ 只能 `DestroyImmediate`）。
            //   必须在**上面那条扫描跑完之后**做（那才是它们存在的理由），也必须在这里做 ——
            //   它们会留在 `FindObjectsByType` 的**现扫**表里，污染下面那两条判别式夹具与后续断言
            //   （同 `AllButtonsForTest` 的注释：夹具用完必须立刻拆）。
            int a1001Torn = 0;
            for (int i = 0; i < a1001Built.Count; i++)
            {
                var b = a1001Built[i];
                if (b == null) continue;
                Object.DestroyImmediate(b.gameObject);
                a1001Torn++;
            }
            Debug.Log(P + $"   A1001：建窗表用过的 {a1001Torn} 扇**已销毁**（`DestroyImmediate`）"
                      + $"—— 留着的就是没建起来的 {a1001Bad.Count} 条（上面那行逐条列了）。");

            // ---- 判别式（🔴 **探针必须先在这两个【已知阳性】上把「报得出来」证明掉**，否则「扫完 0 条」不可信）
            //   判据 → `资料/待办判据_1018.md` §A964 的「已知阳性」两条（A8 / E12）。
            //   ⚠️ **两个旧样本今天都不是活体阳性**（`A8` 修于 2026-10-04 的 `MenuDraw.MakeHitQuad`；
            //      `E12` 修于 2026-10-18 的 `BattleDriver`）⇒ 按侦察报告 §4·3 的口径**改当判别式用**：
            //      造一颗**同样形状**的坏件 ⇒ 探针**必须报**；把它**修好** ⇒ **必须不报**。
            //   ⚠️ 夹具用完**必须 `DestroyImmediate`**（批处理没有帧循环 ⇒ `Destroy` 不生效），
            //      而且命中表是 `FindObjectsByType` **现扫** ⇒ 夹具留着会污染后面的断言。
            shell.Windows.CloseAllWindows();              // 让夹具落在「常驻树」那一档（= 一定在扫描范围内）
            var fx = new GameObject("A964 判别式·裸节点命中区");
            fx.transform.SetParent(root, false);
            var fb = fx.AddComponent<WindowButton>();
            CheckTrue(WouldReport(fb),
                      "★ A964 判别式①（A8 那一族）· 态一：**只有 `WindowButton`、子树里没有 `ImageQuad`** 的节点 "
                      + "⇒ 探针**必须报**（这一条红了 = 「扫完 0 条」不可信）—— 🧨 改坏法：让 `HitQuad` 恒返回 `null` "
                      + "⇒ 判别式①态二与判别式②态一**同时**红（两态配对断，不是单条）");
            var fq = ImageQuad.Create(fx.transform, CardArt.Solid(), Vector3.zero,
                                      LayoutSpace.Px(40f), new Vector2(0.5f, 0.5f), "Hit");
            CheckTrue(fq != null, "（前提）判别式①态二：那颗命中 quad 建出来了（建不出来 ⇒ 下一条等于没查）");
            if (fq != null) { fq.SetAspect(1f); fq.SetTint(new Color(0f, 0f, 0f, 0f)); fq.SetRenderQueue(3000); }
            CheckTrue(!WouldReport(fb),
                      "★ A964 判别式①（A8 那一族）· 态二：照生产那一路补上 `ImageQuad` ⇒ **必须不报**"
                      + " —— 两态都断 ⇒「报得出来」不可能来自「探针恒报」；🧨 改坏法：让 `HitQuad` 恒返回非 null ⇒ 态一红");
            Object.DestroyImmediate(fx);

            //  判别式②（`E12` 那一族 = 「藏起来还响不响应」在外壳侧的等价物）：
            //  🔴 **「藏起来」在本仓是【改在 quad 上】、不是改在钮上** —— `AllButtons()` 是
            //     `FindObjectsInactive.Exclude`，钮一关就**根本不在表里**、连报都不会报（那是原版行为，不是缺陷）
            //     ⇒ 判别式必须做成「**钮活着、它的命中 quad 藏起来**」：`HitQuad` 第二句那句
            //     `!q.gameObject.activeInHierarchy` 正是为它设的。
            //  🧨 **改坏法：删掉 `Shell/PointerLayer.cs` 里那句 `!q.gameObject.activeInHierarchy`**
            //     ⇒ 态二不再报 ⇒ 这一对立刻裂开。
            //     这条同时是**灭自证**那一条：探针的「报」走 `HitQuadForTest`（转发生产），
            //     而这里**期望值是手写的 `true`/`false` 字面量**（⛔ 不是从被测实现读出来的）
            //     ⇒ 「两边一起改回旧写法」不可能同时让两态都绿。
            var fx2 = new GameObject("A964 判别式·命中 quad 不在激活链");
            fx2.transform.SetParent(root, false);
            var fb2 = fx2.AddComponent<WindowButton>();
            var fh2 = new GameObject("Hit");
            fh2.transform.SetParent(fx2.transform, false);
            var fq2 = ImageQuad.Create(fh2.transform, CardArt.Solid(), Vector3.zero,
                                       LayoutSpace.Px(40f), new Vector2(0.5f, 0.5f), "Hit Quad");
            CheckTrue(fq2 != null, "（前提）判别式②态一：那颗命中 quad 建出来了");
            if (fq2 != null) { fq2.SetAspect(1f); fq2.SetTint(new Color(0f, 0f, 0f, 0f)); fq2.SetRenderQueue(3000); }
            CheckTrue(!WouldReport(fb2), "★ A964 判别式②· 态一：quad 活着 ⇒ **不报**");
            fh2.SetActive(false);
            CheckTrue(WouldReport(fb2),
                      "★ A964 判别式②· 态二：**钮活着、命中 quad 被藏起来 ⇒ 必须报** —— 只有它能证明探针真的读了"
                      + "**激活链**、而不是只看「子树里有没有 `ImageQuad`」（后者会把「quad 藏了」当成没问题）");
            Object.DestroyImmediate(fx2);

            // ---- 两条结构断言（🔴 这两条**是**断言，不是报告：它们今天成立、且改坏必有症状）
            CheckTrue(badAnchor.Count == 0,
                      $"★★ A964 结构断言①：**参与命中的那颗 quad 的 `anchor` 必须是 `(0.5,0.5)`** —— 它是"
                      + "`PointerLayer.HitBoxPx` 拿 `q.transform.position` 当**矩形中心**的前提（不是 (0.5,0.5) 时"
                      + "画出来的块是偏的、而命中区仍按中心算 ⇒ 症状正是「看着在钮上、点不动」）。"
                      + $"共 {badAnchor.Count} 条"
                      + (badAnchor.Count > 0 ? "：" + string.Join(" ；", badAnchor.ToArray()) : ""));
            CheckTrue(sliceKid.Count == 0,
                      $"★★ A964 结构断言②：**参与命中的那颗 quad 不能是九宫格/平铺的子块** —— "
                      + "`ImageQuad.CreateNineSlice`/`CreateTiled` 的**根上没有 quad** ⇒ `HitQuad` 会取到**第一块角块**"
                      + "（命中区只剩一个角）。共 " + sliceKid.Count + " 条"
                      + (sliceKid.Count > 0 ? "：" + string.Join(" ；", sliceKid.ToArray()) : ""));

            // ---- 覆盖率（🔴 **如实打**：子类 ≥20、构造入口各不相同 —— ⛔ 别让「没扫到」看起来像「没问题」）
            //   ⚠️ **`allTypes` 的算法已上提到建窗表之前那一处**（A1001：扫描循环里那句
            //      `if (!allTypes.Contains(tn)) continue;` 要用它）—— ⛔ 别在这里再算第二份。
            var notHere = new List<string>();
            foreach (var t in allTypes) if (!hasInstance.Contains(t)) notHere.Add(t);

            // 🔴 **2026-10-18（A1001）**：**账面自洽** —— `covered + notHere` 不可能超过全库子类数。
            //   改前它是 **16 + 23 = 39 > 38**（订正 → `资料/普查产出_第四会话/查证_探针覆盖率与字体口径.md`
            //   §1·2：`hasInstance` 把夹具用的**裸 `GameWindow`** 也入了账，而它**不是** `GameWindow` 的子类）。
            //   今天那道闸在扫描循环里（`if (!allTypes.Contains(tn)) continue;`）⇒ 本条绿。
            //   🧨 **改坏法 = 把那句闸删掉** ⇒ 「覆盖 16」那个假数立刻回来（39 > 38）、实得非子类 1 个 ⇒ 本条红。
            //   📌 本条**只管账面自洽**，不管「哪些窗真扫过」（那是 `notHere` / `failed` / `emptyWin` 三行的事）。
            int a1001NotSub = 0;
            foreach (var nm in hasInstance) if (!allTypes.Contains(nm)) a1001NotSub++;
            foreach (var nm in covered) if (!allTypes.Contains(nm)) a1001NotSub++;
            CheckTrue(a1001NotSub == 0 && covered.Count + notHere.Count <= allTypes.Count,
                      $"★ A1001：覆盖面的账**自洽** —— `covered`({covered.Count}) + `notHere`({notHere.Count})"
                    + $" ≤ 全库 `GameWindow` 子类 {allTypes.Count} 个，且**没有一个非子类的名字**混进来"
                    + $"（实得 {a1001NotSub} 个）。🔴 改坏法：删掉扫描循环里那句 `if (!allTypes.Contains(tn)) continue;`"
                    + " ⇒ **裸 `GameWindow` 夹具**（`AddComponent<GameWindow>()` 那种，**不是子类**）又会被计进"
                    + " `covered` ⇒ 账变成 16 + 23 = 39 > 38、实得非子类 1 个 ⇒ 本条立刻红"
                    + "（改前那趟日志就是那个数）。");

            rows.Insert(0, Row("窗类", "窗态", "节点路径",
                               "命中x1", "命中y1", "命中x2", "命中y2",
                               "实绘x1", "实绘y1", "实绘x2", "实绘y2",
                               "左差", "上差", "右差", "下差", "队列", "判定"));
            File.WriteAllLines(HitProbeDir + "/shell_hits.tsv", rows.ToArray());

            Debug.Log(P + $"   A964：**扫了 {scanned} 颗 / E1 报 {e1.Count} / E3 报 {e3.Count}**"
                      + $"（覆盖 {covered.Count} 个窗类 · 本场景有实例 {hasInstance.Count} 个 · "
                      + $"全库 `GameWindow` 子类 {allTypes.Count} 个 · 明细 {rows.Count - 1} 行 → {HitProbeDir}/shell_hits.tsv）");
            foreach (var s in e1) Debug.Log(P + "   A964·E1：" + s);
            int e3Shown = 0;
            foreach (var s in e3)
            {
                if (e3Shown++ >= 40) { Debug.Log(P + $"   A964·E3：……（还有 {e3.Count - 40} 条，见 TSV）"); break; }
                Debug.Log(P + "   A964·E3：" + s);
            }
            if (e1.Count == 0 && e3.Count == 0)
                Debug.Log(P + "   A964：E1/E3 **零条** —— ⚠️ 这不等于「没问题」：先看上面那三行覆盖率，"
                          + "`covered` 之外的那些窗类本段**根本没扫到**（构造入口各不相同）。");
            // 🔴 没覆盖到的**逐条打**（两类分开：本场景连实例都没有 vs 有实例但没开起来/扫到 0 颗）
            if (failed.Count > 0) Debug.LogWarning(P + $"   A964：**有实例但没覆盖到**（{failed.Count} 个）：" + string.Join(" ；", failed.ToArray()));
            if (emptyWin.Count > 0) Debug.LogWarning(P + "   A964：**开出来了却一颗钮都没有**（扫了 0 颗 ⇒ 对覆盖率没贡献）：" + string.Join(" ；", emptyWin.ToArray()));
            if (notHere.Count > 0) Debug.Log(P + $"   A964：**本场景连实例都没有**（{notHere.Count}/{allTypes.Count} 个窗类，本段扫不到）：" + string.Join("、", notHere.ToArray())
                                              + "　⚠️ 其中 `GameWindowWithTabs` 是**基类**（原版没有它自己的 prefab）⇒"
                                              + "它**永远**留在这个名单里，那不是缺口（A1001 建窗表建的是它能建出来的 22 个子类）。");
            if (covered.Count == 0) Debug.LogWarning(P + "   A964：**一个窗类都没覆盖到** ⇒ 上面的「0 条」没有意义（看 `failed` 那两行）。");
        }

        // ============================================================ 🆕 2026-10-09（`A1139`）新 toast 通道的**外壳档**（0 覆盖）
        // 判据 = `Shell/MessageToast.cs` 的**代码链**（⛔ 不是「哪扇窗在旁边」）：
        //   `Ensure()` 的父节点优先级 = ① 传进来的 `parent` → ② `ShellRuntime.Instance.transform`（只在 `Awake` 里赋、
        //   **批处理下恒 null**）→ ③ `WindowsManager.Instance.transform.parent`（= **壳根**，
        //   `ShellRuntime.Build → WindowsManager.EnsureHost(root)` 里 `SetParent(root, false)`）；
        //   定下 `host` 之后 **`host.Find("Safe area Only Horizontal")`**（`Transform.Find`：**只找直接子件**），
        //   而 `ShellRuntime` 把 `safeH` 挂成**壳根的直接子件** ⇒ 这一支**只有 `ShellScene` 这个宿主**落得到。
        //   🔴 **反例**：`Editor/MainMenuScene.cs` 的 `EnsureHost(menu.transform)` 根 = **菜单根**（它下面**没有**那个节点）
        //   ⇒ 会退回把横幅挂在**菜单根**上 —— ① 那条断言就是为这个反例准备的。
        // 🔴 期望值 = 原版主菜单那一颗的**档**（`ErrorMessageBanner.MenuPreset`；四个数的出处 → `Shell/MessageToast.cs` 文件头 ④）。
        // ⚠️ 落点在 `Run()` 的**最后一段**是有意的：本节新加一颗 GO + 若干 `ImageQuad`，
        //   而上面那些**全场景现扫**（`:…FindObjectsByType` / `PointerLayer.AllButtonsForTest()` / A1001 拆窗表 / 夹具拆）
        //   **全部在本节之前** ⇒ 零影响。
        Section("🆕 A1139：toast（`MessageToast`）—— 挂点 / 菜单档 / 弹一条 / 战斗档逐位不变");
        {
            // P1 干净起点（`Instance` 是**静态**的、跨节存活 ⇒ 不干净就会量到上一节留的那颗）
            if (MessageToast.Instance != null) MessageToast.DisposeForTest();
            CheckTrue(MessageToast.Instance == null, "（前提）A1139：起点干净（`MessageToast.Instance == null`）");
            // P3 前提·有壳（= 上面那条父节点链的**唯一**活支）
            CheckTrue(WindowsManager.Instance != null,
                      "（前提·不静默）A1139：`WindowsManager.Instance` 在（`Ensure()` 父节点链的唯一活支；不在 ⇒ 本节等于没验）");
            var a1139 = MessageToast.Ensure();
            // P2 建得出来
            CheckTrue(a1139 != null && a1139.Built,
                      "（前提）A1139：`Ensure()` 建出来了（`Built` = `Banner != null && ItemCount > 0`）");
            if (a1139 != null)
            {
                // ① 挂在哪（原版那条判据：父节点名 + 节点名）
                CheckTrue(a1139.transform.parent != null
                          && a1139.transform.parent.name == MessageToast.ParentNodeName
                          && a1139.name == MessageToast.NodeName,
                          "★★ A1139①：toast 挂在**原版那个父节点** `" + MessageToast.ParentNodeName + "` 下、节点名 = `"
                        + MessageToast.NodeName + "`（现读父 = `"
                        + (a1139.transform.parent == null ? "<无父>" : a1139.transform.parent.name) + "`）"
                        + "｜🧨 把 `Ensure()` 里 `host.Find(ParentNodeName)` 那段删掉（直接挂场景根）⇒ 红");
                // ② 弹一条
                bool a1139Ok2 = MessageToast.Show("w6.selftest", false, "x");
                CheckTrue(a1139Ok2 && a1139.ShowCount == 1,
                          "★ A1139②：`Show(..., localize:false)` 返 true 且 `ShowCount == 1`（现读 " + a1139.ShowCount + "）");
                // ③ 条心 = 菜单档（⛔ 不把算式在断言里再抄一遍：一个是**布局产物**（世界坐标回读）、一个是**档常量的算式**，不同源）
                float a1139Y = a1139.Banner != null ? a1139.Banner.ItemCenterPxOf(0).y : float.NaN;
                CheckTrue(!float.IsNaN(a1139Y) && Mathf.Abs(a1139Y - a1139.SingleItemCenterYpx) < 0.25f,
                          "★★ A1139③：单条**条心** == 档的算式（`RootTopPx + RootH − ItemHPx/2`）—— 现读 "
                        + a1139Y.ToString("F4") + " vs " + a1139.SingleItemCenterYpx.ToString("F4")
                        + "｜🧨 把 `ErrorMessageBanner.SingleItemCenterYpx` 的 `− ItemHPx/2` 丢掉（改成 `RootTopPx + RootH`）⇒ 红");
                // ④ 灭自证（本条的牙）：只断 ③ 的话，把 `MenuPreset` **整个换成** `BattlePreset` ⇒ **两边一起变** ⇒ 照样绿
                CheckTrue(Mathf.Abs(a1139.Preset.ItemHPx - 50f) < 1e-4f
                          && Mathf.Abs(a1139.Preset.RootTopPx - 0f) < 1e-4f
                          && Mathf.Abs(a1139.Preset.FontPx - 36f) < 1e-4f,
                          "★★★ A1139④（灭自证）：壳里这颗取的是**菜单档**（`ItemHPx 50` / `RootTopPx 0` / `FontPx 36`）—— 现读 "
                        + $"ItemHPx={a1139.Preset.ItemHPx:F4} RootTopPx={a1139.Preset.RootTopPx:F4} FontPx={a1139.Preset.FontPx:F4}"
                        + "｜🧨 把 `Shell/MessageToast.cs` 里那个 `MenuPreset` 换回 `BattlePreset` ⇒ ④红（而 ③ 仍绿 —— 这正是 ④ 的牙）");
                // 🆕 2026-10-20（W58 · D3）：**实际那一颗横幅**的队列 = 菜单档（4035）——
                //   原版 `UI Error Message Controller` = `PopUps` / order 1 ⇒ **在全部窗之上**
                //   （判据 → `资料/普查产出_1020/W58_界面线_Toast黑场次序判据.md` §2·3）。
                //   ⛔ 断的是实例的档（`Banner.BarQueue`），不是只查 `MenuPreset` 常量 —— 否则把 `Create(..., MenuPreset)`
                //   换成 `BattlePreset` 时两边一起变、看不出来（灭自证）。
                CheckTrue(a1139.Banner != null
                          && a1139.Banner.BarQueue == ErrorMessageBanner.MenuQueue
                          && a1139.Banner.BarQueue == ErrorMessageBanner.MenuPreset.Queue
                          && a1139.Banner.BarQueue > AlliancePanelWindow.QText,
                          "★★ W58-D3：壳里这台横幅的**实际队列** = " + (a1139.Banner != null ? a1139.Banner.BarQueue.ToString() : "<空>")
                        + $"（菜单档 {ErrorMessageBanner.MenuQueue}）—— 高于窗带最高 {AlliancePanelWindow.QText}"
                        + "（原版 `PopUps`/1 > `3 - PopUp Holder` `PopUps`/0）"
                        + "｜🧨 把 `MenuPreset.Queue` 改回 4001 ⇒ 红（4001 < 窗 4030）");
                // ⑤ 两档不同档（把「菜单档」与「战斗档」钉成两个不同的值）
                CheckTrue(a1139.Preset.ItemHPx != ErrorMessageBanner.BattlePreset.ItemHPx
                          && Mathf.Abs(ErrorMessageBanner.BattlePreset.RootTopPx - 213.5997314453125f) < 1e-4f,
                          "★★ A1139⑤：**菜单档 ≠ 战斗档**（项高 50 vs 80）且战斗档根上缘仍是 `213.5997314453125`"
                        + "｜🧨 把 `BattlePreset.ItemHPx` 也改成 50 ⇒ ⑤红（+ ⑥红）");
                // ⑥ 战斗档条心逐位不变（回归闸）—— 与 `Editor/BattleScene.cs` 那条既有断言**同一个数**
                CheckTrue(Mathf.Abs(ErrorMessageBanner.BattlePreset.RootTopPx + 336.6400146484375f
                                    - ErrorMessageBanner.BattlePreset.ItemHPx * 0.5f - 510.23974609375f) < 1e-4f,
                          "★ A1139⑥（回归闸）：战斗档条心仍是 **510.23974609375**（`Editor/BattleScene.cs` 那条既有断言同一个数；"
                        + "⛔ 别去改那一条，这里只做**异地互证**）");
            }
            // ⑦ 收尾（`Instance` 静态、跨节存活 —— 本节必须自己收）
            MessageToast.DisposeForTest();
            CheckTrue(MessageToast.Instance == null,
                      "（收尾）A1139：`DisposeForTest()` 之后 `Instance` 归 null（下一节不会被这一颗污染）");
        }

        // ============================================================ 🆕 2026-10-17（`N20`/`N21`/`N23`）Toast 控制器
        // 判据：`资料/普查产出_1017/UI/R_界面线剩余条目现核.md` §二·4/5 + §N21 —— **下面三条期望值全是
        // 原版字面量 / 原版树形**，⛔ 不从 `ToastNotifications` 自己的常量算式里取。
        // 🔴 **2026-10-20 订正（铁律 5）**：那份 1017 报告的 `N21①`（「Toast 在黑场之上 / 已经同序」）**不成立**
        //    —— 它只比了 `m_SortingOrder`、漏了 `m_SortingLayerID`。现行判据 =
        //    `资料/普查产出_1020/W58_界面线_Toast黑场次序判据.md`（原版 `Toast` = PopUps/900 · `LoadingMenu` = Blocking/0
        //    ⇒ **黑场在 toast 之上**），下面 ① 已按它重写，并补了 D1/D2/D3 的**层序梯子**。
        Section("🆕 N20/N21/N23：Toast 控制器 —— 队列低于加载黑场 / 两颗死模板已删 / 根高按文案现量 / 完成态显隐");
        {
            ToastNotifications.DisposeForTest();
            var tn = ToastNotifications.Build(root);
            CheckTrue(tn != null && tn.Built,
                      "（前提）Toast 控制器建出来了（`Safe area Only Horizontal ▸ Toast Notification Controller ▸ Holder`）");
            if (tn != null)
            {
                // ---- ① `N21①`：层序 —— **原版判据** + 我们的梯子（2026-10-20 · W58 判据重写，铁律 5） ----
                // 🔴 **原版判据**（`资料/普查产出_1020/W58_界面线_Toast黑场次序判据.md` §1·1 / §2·3）：
                //   `Toast Notification Controller` = 排序层 **`PopUps`(7) / order 900**；
                //   `LoadingMenu` = 排序层 **`Blocking`(8) / order 0**。Unity **先比排序层、再比 order**
                //   （`globalgamemanagers/TagManager/TagManager_3.json`）⇒ **黑场在 toast 之上**。
                //   ⛔ **别只比 order**（900 > 0 会得出反的结论 —— 旧版 N21① 就是这么错的，`R_界面线剩余条目现核.md:33`）。
                CheckTrue(ToastNotifications.QHit < LoadingMenu.QPanel,
                          $"★ N21①（原版判据）：toast 队列 = {ToastNotifications.QShade}..{ToastNotifications.QHit} —— "
                        + $"**整族都在加载黑场 `LoadingMenu.QPanel = {LoadingMenu.QPanel}` 之下**"
                        + "（原版 `Toast` = PopUps/900 · `LoadingMenu` = Blocking/0；排序层**先于** order 比较）"
                        + "｜🧨 把 toast 改回 5000（压在黑场之上）⇒ 本条红");
                // ---- ①a `N21①`：转圈件的层序 —— **原版判据** + 我们的梯子（2026-10-20 · W58 · D4，铁律 5） ----
                // 🔴 **原版判据**（`资料/普查产出_1020/W58_界面线_联盟搜索形态判据.md` §一·4）：
                //   `MenuLoadingController` 在 `Game UI` 画布的 **`Default`(0) 层**（自己**没有** Canvas；
                //   唯一子 Canvas 是 `Cog` 的 `Canvas_1078`，`m_OverrideSorting = false`），且是
                //   `Safe area Only Horizontal` 的**最后一个孩子**（兄弟序 11）⇒ 压在主窗（placement 5/10）之上、
                //   在全部 **PopUps**（窗/横幅/教学/toast）与 **Blocking**（tooltip/黑场）之下。
                //   ⛔ 断的不是「转圈在上」—— 旧版这里写 `Toast.QShade < LoadingController.QDark`，那**不是**原版判据
                //   （只是本仓旧梯子），本次 D4 已把它降回原版该在的那一档。
                CheckTrue(LoadingController.QCog < ToastNotifications.QShade
                          && LoadingController.QCog < Tooltip.Queue
                          && LoadingController.QCog < LoadingMenu.QPanel,
                          $"★ N21①a（原版判据）：转圈件（QDark/QSkull/QCog = {LoadingController.QDark}/"
                        + $"{LoadingController.QSkull}/{LoadingController.QCog}）**在 toast({ToastNotifications.QShade}) / "
                        + $"tooltip({Tooltip.Queue}) / 黑场({LoadingMenu.QPanel}) 之下** —— "
                        + "原版 `MenuLoadingController` = `Default`(0)、`Safe area` 最后孩子；toast = `PopUps`/900 · "
                        + "tooltip/黑场 = `Blocking`"
                        + "｜🧨 把转圈件改回 4080–4082 ⇒ 本条红");
                // ⚠️ **我们的梯子 = [我们挑的]**：原版按排序层排，我们这张显式队列表里没有对应号段 ⇒ 取
                //   `TrophyInfoPopup.QHit(3326)` 与 `GenericOptionsPanel.QShade(3330)` 之间的空隙 **3327–3329**：
                //   必须**压在社交窗整带之上**（页 3200–3209 · 奖杯格 3210–3299 · 聊天 3300–3308），
                //   否则联盟搜索的转圈会被 Social 窗自己盖住；又不落进任何窗的号段。
                CheckTrue(LoadingController.QDark > SocialWindow.QPageBase + 99
                          && LoadingController.QCog < GenericOptionsPanel.QShade,
                          $"★ N21①a（我们的梯子，[我们挑的]）：转圈 = {LoadingController.QDark}..{LoadingController.QCog} —— "
                        + $"高于社交窗整带（页基 {SocialWindow.QPageBase} + 99）且低于下一档窗 "
                        + $"`GenericOptionsPanel.QShade({GenericOptionsPanel.QShade})`"
                        + "｜🧨 把 `LoadingController.QDark/QCog` 改回 4080/4082 ⇒ 本条红（4080 > 3299 与 4082 > 3330 两侧都越界）");
                // 🔶 自己设计（非原版）：加载转圈队列梯子 3327–3329 的绝对值回归期望 · 原版只给相对次序，绝对号段是我们挑的（原版判据拿不到） · TODO(谁定的)｜原注：绝对值的回归闸（[我们挑的]）—— 上一条挡「转圈 + 社交窗两边一起挪」，这条只钉住这张表本身
                CheckTrue(LoadingController.QDark == 3327 && LoadingController.QSkull == 3328
                          && LoadingController.QCog == 3329,
                          $"（回归闸）W58·D4 的绝对值：[我们挑的] 转圈 QDark/QSkull/QCog = "
                        + $"{LoadingController.QDark}/{LoadingController.QSkull}/{LoadingController.QCog}"
                        + "（期望 3327/3328/3329）");

                // ---- ①a2 🆕 2026-10-20（W58 · 遗留 1）：**降级遮罩 `BlockingOverlay` 也落回加载档** ----
                // 判据同 ①a：原版加载遮罩（`Default`(0) / `Blocking`）**不在 toast（`PopUps`/900）之上**；
                // 本件是**零运行时调用点的降级路径**（`ShellRuntime.Build` 建 · `PointerLayer` 只读 `IsBlocking`），
                // 早先的 **4070/4071** 是「压过全部 shell UI 与 toast」的旧口径 ⇒ 已过期（铁律 5 订正）。
                // [我们挑的] 梯子 = 转圈件 3327–3329 同族里**唯一剩下的双槽空隙**：
                //   `GenericOptionsPanel` 带尾（`QHit = 3337`）与 `AllianceMemberOptionsPopup` 带首（`QShade = 3340`）之间
                //   ⇒ 3338（dim）/ 3339（旧圈）。再往上 3340→3487 只剩单槽空隙，容不下两件。
                CheckTrue(BlockingOverlay.QDim == 3338 && BlockingOverlay.QSpinner == 3339,
                          $"（回归闸）W58 遗留 1：降级遮罩 `BlockingOverlay` QDim/QSpinner = "
                        + $"{BlockingOverlay.QDim}/{BlockingOverlay.QSpinner}（期望 3338/3339）"
                        + "｜🧨 改回 4070/4071 ⇒ 本条红");
                CheckTrue(BlockingOverlay.QDim > GenericOptionsPanel.QHit
                          && BlockingOverlay.QSpinner < AllianceMemberOptionsPopup.QShade
                          && BlockingOverlay.QSpinner < ToastNotifications.QShade,
                          $"★ 降级遮罩 {BlockingOverlay.QDim}..{BlockingOverlay.QSpinner} 落在 "
                        + $"`GenericOptionsPanel.QHit({GenericOptionsPanel.QHit})` 与 "
                        + $"`AllianceMemberOptionsPopup.QShade({AllianceMemberOptionsPopup.QShade})` 之间的空隙里、"
                        + $"且低于 toast({ToastNotifications.QShade}) —— 不落进任何窗号段"
                        + "｜🧨 改回 4070/4071 ⇒ 本条红（4071 > 3340）");

                // ---- ①b 🆕 2026-10-20（W58 · D1/D2/D3）：三层相对次序 = **原版排序层判据** ----
                // 原版（层序低→高）：窗 `PopUps`/0 < 错误横幅 `PopUps`/1 < 教学高亮 `PopUps`/601
                //   < toast `PopUps`/900 < `Blocking` 层（tooltip / 加载黑场）。
                // 🔑 **相对次序 = 原版判据；绝对值（4035 / 4040 / 4077–4079）= [我们挑的]**。
                // 🔴 **灭自证**：下面这条梯子的**两端钉在外部件的常量上**（窗带最高 `AlliancePanelWindow.QText = 4030`、
                //   黑场 `LoadingMenu.QPanel = 4090`）⇒ **把 D1/D2/D3 任意一处改回旧值，整条链必红**
                //   （旧值：tooltip 3606 < toast · 教学 4100 > 黑场 · 横幅 4001 < 窗 4030）。
                int qWinMax = AlliancePanelWindow.QText;     // 已知的最高窗队列（= 4030）
                int qBanner = ErrorMessageBanner.MenuPreset.Queue;
                int qTut = MenuTutorialHighlight.QShade;
                int qToastLo = ToastNotifications.QShade, qToastHi = ToastNotifications.QHit;
                int qTip = Tooltip.Queue;
                CheckTrue(qWinMax < qBanner && qBanner < qTut && qTut < qToastLo
                          && qToastHi < qTip && qTip < LoadingMenu.QPanel,
                          "★★★ W58 层序梯子（原版判据 = 排序层 PopUps/0 → 1 → 601 → 900 → Blocking）："
                        + $" 窗({qWinMax}) < 横幅({qBanner}) < 教学({qTut}) < toast({qToastLo}..{qToastHi}) "
                        + $"< tooltip({qTip}) < 黑场({LoadingMenu.QPanel})"
                        + "｜🧨 任一处改回旧值（tooltip 3606 / 教学 4100 / 横幅 4001）⇒ 本条红");
                // 逐条拆开（红的时候能一眼看出是哪一层坏了）
                CheckTrue(qTip > qToastHi,
                          $"★ W58-D1：tooltip({qTip}) **在 toast({qToastHi}) 之上** —— 原版 `Blocking`(8) > `PopUps`(7)"
                        + "（`TooltipManager` = Blocking/0 · `Toast` = PopUps/900；排序层先比较）"
                        + "｜🧨 改回 3606 ⇒ 红");
                CheckTrue(qTut < qToastLo && qTut < LoadingMenu.QPanel,
                          $"★ W58-D2：教学高亮({qTut}) **在 toast({qToastLo}) 与黑场({LoadingMenu.QPanel}) 之下** —— "
                        + "原版 `Tutorial highlight` = PopUps/601 < toast PopUps/900 < Blocking"
                        + "｜🧨 改回 4100 ⇒ 红");
                CheckTrue(qBanner > qWinMax && qBanner < qTut,
                          $"★ W58-D3：错误横幅({qBanner}) **在全部窗({qWinMax} 及以下)之上、教学({qTut})之下** —— "
                        + "原版 `UI Error Message Controller` = PopUps/1 > `3 - PopUp Holder` PopUps/0"
                        + "｜🧨 改回 4001 ⇒ 红（4001 < 窗 4030）");
                // 绝对值的回归闸（[我们挑的]）—— 上一条挡「两边一起挪」，这条只钉住我们这张表本身
                CheckTrue(qTip == 4078 && qTut == 4040 && qBanner == 4035,
                          $"（回归闸）W58 的绝对值：[我们挑的] tooltip {qTip}（期望 4078）· 教学 {qTut}（4040）· 横幅 {qBanner}（4035）");

                // ---- ② `N21②③`：两颗死模板**不在** `Templates` 下 ----
                int dead = 0, tplKids = tn.Templates != null ? tn.Templates.childCount : -1;
                if (tn.Templates != null)
                    foreach (Transform c in tn.Templates)
                        if (c.name == "Collector Level Tooltip" || c.name == "Resource Counter Item") dead++;
                Check(dead, 0, $"★ N21②③：`Templates` 下**没有**那两颗死模板（现读 {dead} 颗；"
                             + $"`Templates` 子件共 {tplKids} —— 应为 **4**：四个 toast 模板）");

                // ---- ③ `N23`：根高按文案现量（同一文案两长度 ⇒ 根高不同；模板常量不再当选） ----
                float pxOfWorld = 1f / LayoutSpace.Px(1f);      // 世界单位 → 设计 px（= ×108）
                string shortT = "Hi";
                string longT = "Congratulations on a very long achievement line that has to wrap onto more than one line of the toast body";
                ToastNotifications.Show(ToastNotifications.ToastNotificationType.Achievement, shortT);
                ToastNotifications.Show(ToastNotifications.ToastNotificationType.Achievement, longT);
                int n = tn.Holder != null ? tn.Holder.childCount : 0;
                Check(n, 2, $"（前提）两条 toast 都进 `Holder` 了（现读 {n}）");
                if (n >= 2)
                {
                    var a0 = (RectTransform)tn.Holder.GetChild(0);
                    var a1 = (RectTransform)tn.Holder.GetChild(1);
                    float h0 = a0.sizeDelta.y * pxOfWorld, h1 = a1.sizeDelta.y * pxOfWorld;
                    CheckTrue(h1 > h0 + 1f,
                              $"★ N23：**长文案的根高 > 短文案**（短 {h0:F2}px → 长 {h1:F2}px）"
                            + "｜🧨 改回常量 `108.8638`（两条都同高）⇒ 本条红");
                    // 单行那条的根高 = `padT(12) + iconH(89.8638) + 文本首选高 + padB(7)`（原版三字段，字面量）
                    var t0 = a0.Find("Text");
                    var lb0 = t0 != null ? t0.GetComponent<Label>() : null;
                    float textH0 = lb0 != null ? lb0.WorldH * pxOfWorld : -1f;
                    CheckTrue(lb0 != null && textH0 > 0f
                              && Mathf.Abs(h0 - (12f + 89.8638f + textH0 + 7f)) < 0.05f,
                              $"★ N23：单行根高 = `12 + 89.8638 + 文本首选高 + 7`（现读 根 {h0:F2} / 文本 {textH0:F2}px）"
                            + "｜🧨 把 `ToastRootHeight` 里的任一 padding 或 icon 高改掉 ⇒ 本条红");

                    // ---- ⑤ 🆕 2026-10-23（T16 · `D10`）：Toast **根高探针** —— 只出声、不断言、⛔ 不猜数 ----
                    // 判据缺口：**原版 player** 的 `Toast` 根 `RectTransform` 运行时 `rect.height`（= `sizeDelta.y`，
                    //   本表那一列）本地没有读数；静态推导有两条互相打架的口径（`N23`：常量 108.8638 vs 文本驱动高）。
                    // 🔴 本段**只做探针**：
                    //   ① 把**我们这一侧**的根高读数打出来（`[D10PROBE]` 前缀 ⇒ 批处理 / 真机日志直接可收）；
                    //   ② **原版读数留空**（`origRootH = 0f` = 未采集）并**出声**；
                    //   ③ ⛔ 不写断言、不填「大概是」的数字 —— 原版值没回来之前 `D10` 只能是 `◐`。
                    // 探针方案（原版侧，主对话执行）：`d:/2/unity_run_ref` + SceneJumpShot → 进主菜单触发一条 `Toast`
                    //   ⇒ dump `Toast Notification Controller/Holder` 下 `Toast` 根的 `RectTransform.sizeDelta.y`；
                    //   **一行 / 多行两种文案各一次**（出处 → `资料/普查产出_1023c/界面线_清点报告.md:85`）。
                    {
                        const string D10 = "[D10PROBE]";
                        float origRootH = 0f;      // 🔴 原版真机读数：**留空**（0 = 未采集）。⛔ 别填猜的数。
                        Debug.Log(D10 + $" 我们这一侧：一行 {h0:F3} px ／ 多行 {h1:F3} px"
                                      + "（`Toast` 根 `RectTransform.sizeDelta.y` × 108）");
                        if (origRootH <= 0f)
                            Debug.LogWarning(D10 + " **原版真机 `rect.height` 未采集（读数留空）** ⇒ 本次只报我们这一侧；"
                                           + "⛔ 不据此改常量、不猜数字。`D10` 仍为 `◐`；"
                                           + "采集 = 原版 player + SceneJumpShot，一行 / 多行两种文案各一次。");
                    }
                }

                // ---- ④ `N20②`：完成态 ⇒ 三件 info display 全关（`displayRule = 1`） ----
                var cHalf = new ToastNotifications.Context("Deal 500 damage to enemy units",
                                                           ToastNotifications.ToastNotificationType.MissionProgress);
                cHalf.ShowProgress = true; cHalf.Progress01 = 0.5f;
                cHalf.ProgressText = "5/10"; cHalf.RewardText = "300";
                cHalf.RewardArt = "40K_genearl_icon_Campaign_points_big";
                ToastNotifications.ShowNotification(cHalf);
                var cDone = cHalf; cDone.Progress01 = 1f;
                ToastNotifications.ShowNotification(cDone);
                int m = tn.Holder != null ? tn.Holder.childCount : 0;
                Check(m, 4, $"（前提）两条 mission toast 也进 `Holder` 了（现读 {m}）");
                if (m >= 4)
                {
                    var half = tn.Holder.GetChild(m - 2);
                    var done = tn.Holder.GetChild(m - 1);
                    var halfBar = half.Find("Mission Milestones Progress Bar");
                    var doneBar = done.Find("Mission Milestones Progress Bar");
                    var halfPB = halfBar != null ? halfBar.Find("Progress Bar") : null;
                    var donePB = doneBar != null ? doneBar.Find("Progress Bar") : null;
                    var halfPg = halfBar != null ? halfBar.Find("progress") : null;
                    var donePg = doneBar != null ? doneBar.Find("progress") : null;
                    var halfRw = half.Find("Rewards");
                    var doneRw = done.Find("Rewards");
                    CheckTrue(halfPB != null && halfPB.gameObject.activeSelf
                              && halfPg != null && halfPg.gameObject.activeSelf
                              && halfRw != null && halfRw.gameObject.activeSelf,
                              "★ N20②（未完成档）：`Progress Bar` / `progress` / `Rewards` **都在画**"
                            + "（原版 `displayRule = 1` = 未完成才显示）");
                    CheckTrue(donePB != null && !donePB.gameObject.activeSelf
                              && donePg != null && !donePg.gameObject.activeSelf
                              && doneRw != null && !doneRw.gameObject.activeSelf,
                              "★★ N20②（完成档）：三件**全关**（判据 = `MB {499,500,501}.json` 的 `displayRule = 1`）"
                            + "｜🧨 删掉 `Fill` 里那个 `complete` 判据 ⇒ 本条红");
                    var donePgLb = donePg != null ? donePg.GetComponent<Label>() : null;
                    Check(donePgLb != null ? donePgLb.Text : null, Loc.T("Missions/Completed"),
                          "★ N20②：完成档的计数文案 = 词条 `Missions/Completed`（原版 `completedMessage`，"
                        + "`MB 499` 实读；⛔ 不是 `5/10`）");
                }
            }
            ToastNotifications.DisposeForTest();
            CheckTrue(ToastNotifications.Instance == null,
                      "（收尾）`DisposeForTest()` 之后 `Instance` 归 null（下一节不会被这一颗污染）");
        }

        // ============================================================ 🆕 2026-10-20（W58）Toast 规格与生命周期
        // 判据：资料/普查产出_1020/W58_界面线_Toast黑场次序判据.md §五 D5/D6/D7/D9；
        //       落地与逐条对照 → 资料/普查产出_1020/W58_界面线_Toast规格与生命周期.md
        //  ⚠️ 本节只验「我们照判据做到了」；原版真机 `rect.height`（D10）本地没有读数 ⇒ 不在本节断。
        Section("🆕 W58：toast 生命周期 / hover 色偏 / Player Level 折行 / 错误色转发");
        {
            ToastNotifications.DisposeForTest();
            MessageToast.DisposeForTest();
            var w58 = ToastNotifications.Build(root);
            CheckTrue(w58 != null && w58.Built, "（前提）W58：Toast 控制器建出来了");

            // ---- ① D5：ClearLive() 只清在飞；DisposeForSceneChange() 连控制器一起销毁 ----
            ToastNotifications.Show(ToastNotifications.ToastNotificationType.Achievement, "w58.a");
            ToastNotifications.Show(ToastNotifications.ToastNotificationType.Achievement, "w58.b");
            CheckTrue(w58.LiveCount == 2 && w58.FreeSpot == 2,
                      $"（前提）W58-D5：两条在飞（现读 Live={w58.LiveCount} / freeSpot={w58.FreeSpot}）");
            ToastNotifications.ClearLive();
            CheckTrue(w58.LiveCount == 0 && w58.FreeSpot == 0 && w58.ScheduledCount == 0
                      && ToastNotifications.Instance == w58,
                      $"★★ W58-D5①：`ClearLive()` 清空在飞/排队/格位、**控制器还在**"
                    + $"（现读 Live={w58.LiveCount} / sched={w58.ScheduledCount} / freeSpot={w58.FreeSpot}）");

            ToastNotifications.Show(ToastNotifications.ToastNotificationType.Achievement, "w58.c");
            var w58Ctrl = w58.gameObject;
            ToastNotifications.DisposeForSceneChange();
            CheckTrue(ToastNotifications.Instance == null && w58Ctrl == null,
                      "★★ W58-D5②：`DisposeForSceneChange()` ⇒ 控制器节点**整棵销毁**、`Instance` 归 null"
                    + "（= 原版『Toast 控制器只在主菜单场景，切场景即销毁』；13 个战场 0 处）"
                    + "｜🧨 只清在飞、不销毁控制器 ⇒ 本条红");
            CheckTrue(ShellRuntime.SceneCleanupHookInstalledForTest,
                      "★ W58-D5③：`ShellRuntime.Build` 挂上了『任何 sceneLoaded ⇒ 清 toast』的兜底订阅"
                    + "（覆盖不走 `LoadSceneWithFade` 的直调 `LoadScene` 站点）"
                    + "｜🧨 删掉 `Build()` 里 `EnsureSceneCleanupHook()` ⇒ 红");

            // ---- ② D6：Toast/LevelUp/GetXP 悬停乘 0.9608 / 按下 0.7843；Mission 不接 ----
            w58 = ToastNotifications.Build(root);          // ② 之前刚整棵销毁过 ⇒ 重新建
            ToastNotifications.Show(ToastNotifications.ToastNotificationType.Achievement, "w58.hover");
            var aToast = w58.Holder != null ? w58.Holder.GetChild(w58.Holder.childCount - 1) : null;
            var aBg = aToast != null ? aToast.Find("Background") : null;
            var aQuads = aBg != null ? aBg.GetComponentsInChildren<ImageQuad>(true) : null;
            var aHit = aToast != null ? aToast.GetComponentInChildren<WindowButton>(true) : null;
            CheckTrue(aQuads != null && aQuads.Length > 0 && aHit != null,
                      "（前提）W58-D6：toast 有 `Background` quad 和命中 `WindowButton`");
            if (aQuads != null && aQuads.Length > 0 && aHit != null)
            {
                var aBase = aQuads[0].Tint;
                aHit.Enter();                 // 编辑模式 ⇒ `SetTarget` 直接落目标色（不走 0.1s 补间）
                float k = aQuads[0].Tint.g / Mathf.Max(1e-6f, aBase.g);
                CheckTrue(Mathf.Abs(k - WindowButton.HighlightK) < 1e-3f,
                          $"★★ W58-D6①：悬停后**可见底图**乘 0.9608（现读 k={k:F4}，期望 {WindowButton.HighlightK:F4}）"
                        + "（原版 `MonoBehaviour_504.json`：`m_Transition = 1` · `m_HighlightedColor = 0.9608` · fade 0.1）"
                        + "｜🧨 删掉 `ShowInternal` 里那句 `hitBtn.TintOn(bgQuads)` ⇒ 本条红（色只打在透明命中区上）");
                aHit.Press();
                float kp = aQuads[0].Tint.g / Mathf.Max(1e-6f, aBase.g);
                CheckTrue(Mathf.Abs(kp - WindowButton.PressedK) < 1e-3f,
                          $"★ W58-D6②：按下后乘 0.7843（现读 k={kp:F4}，期望 {WindowButton.PressedK:F4}）"
                        + "｜🧨 把 `TintOn` 换成只给命中区上色 ⇒ 红");
                aHit.Release(); aHit.Exit();
            }
            var mission = new ToastNotifications.Context("w58 mission",
                                                          ToastNotifications.ToastNotificationType.MissionProgress);
            mission.ShowProgress = true; mission.Progress01 = 0.5f; mission.ProgressText = "1/2";
            ToastNotifications.ShowNotification(mission);
            var mToast = w58.Holder.GetChild(w58.Holder.childCount - 1);
            var mBg = mToast.Find("Background");
            var mQuad = mBg != null ? mBg.GetComponentInChildren<ImageQuad>(true) : null;
            var mHit = mToast.GetComponentInChildren<WindowButton>(true);
            CheckTrue(mQuad != null && mHit != null, "（前提）W58-D6：mission toast 有 `Background` quad 和命中钮");
            if (mQuad != null && mHit != null)
            {
                var mBase = mQuad.Tint;
                mHit.Enter();
                CheckTrue(Mathf.Abs(mQuad.Tint.g - mBase.g) < 1e-4f,
                          $"★★ W58-D6③（灭自证）：**Mission 悬停不变色**（现读 {mBase.g:F4}→{mQuad.Tint.g:F4}）"
                        + "（原版 `MonoBehaviour_520.json`：`m_Transition = 0 (None)`、`target = 0`）"
                        + "｜🧨 去掉 `if (ctx.Type != MissionProgress)`（四颗一起接）⇒ 本条红");
                mHit.Exit();
            }

            // ---- ③ D7：`Player Level Text` 折行 = Normal(1) ----
            var lvlText = w58.Templates != null
                        ? w58.Templates.Find(ToastNotifications.LevelUpPrefabName + "/Player Level (1)/Player Level Text")
                        : null;
            var lvlLb = lvlText != null ? lvlText.GetComponent<Label>() : null;
            Check(lvlLb != null ? lvlLb.WrappingMode : -1, 1,
                  "★ W58-D7：`Player Level Text` 的 `m_TextWrappingMode` = 1 (Normal)（原版 `MB_505`）"
                + "｜🧨 调用点改回 `wrap: false`、或 `Tmp()` 里去掉 `lb.SetWrapping(wrap)` ⇒ 本条红");

            ToastNotifications.DisposeForTest();

            // ---- ④ D9：`MessageToast.ShowError` 转发错误色（0.8,0,0,1）；白档仍是白 ----
            var mt = MessageToast.Ensure();
            CheckTrue(mt != null && mt.Built, "（前提）W58-D9：`MessageToast` 建出来了");
            if (mt != null && mt.Built)
            {
                bool okRed = MessageToast.ShowError("w58.err", false, "x");
                var red = mt.Banner.TextColorOf(0);
                CheckTrue(okRed && Mathf.Abs(red.r - 0.8f) < 1e-4f
                          && red.g < 1e-4f && red.b < 1e-4f && Mathf.Abs(red.a - 1f) < 1e-4f,
                          $"★★ W58-D9①：`ShowError` 文字色 = (0.8,0,0,1)（现读 {red.r:F4},{red.g:F4},{red.b:F4},{red.a:F4}）"
                        + "（原版 `UIMessageController.ShowError` → `errorColor`）"
                        + "｜🧨 让 `ShowError` 改走 `ShowMessage` 白档 ⇒ 本条红");
                bool okWhite = MessageToast.Show("w58.msg", false, "x");
                var white = mt.Banner.TextColorOf(1);
                CheckTrue(okWhite && Mathf.Abs(white.r - 1f) < 1e-4f
                          && Mathf.Abs(white.g - 1f) < 1e-4f
                          && Mathf.Abs(white.b - 1f) < 1e-4f,
                          $"★ W58-D9②（反证）：白档仍是 (1,1,1,1)（现读 {white.r:F4},{white.g:F4},{white.b:F4}）");
            }
            MessageToast.DisposeForTest();
        }

        Debug.Log(P + shell.Dump());
        // ============================================================ 🆕 2026-10-09（`A1025`）本批键全 `Loc.HasEntry` + 两语档取真文案
        // 判据（坑表 #18）：**「原版有词条」≠「我们表里有键」** —— 键不在 ⇒ `Loc.T` 返回**键名本身**、界面上就印键名。
        // 🔴 **与 `A1057(f)`（`Editor/NetSelfTest.cs` 那张**表级**扫描）不是同一条、方向相反**：那条是 **表 → 表**
        //   （表自身健康），本条是 **代码 → 表**（**本批代码引的键**有没有落进表）—— 表级扫描永远看不见后者。
        // 🔴 **Net/ 那批已有等价物**（`Editor/NetSelfTest.cs` 的 `TestNetTermBilingual`）⇒ 本笔不重复覆盖它。
        // 数组 = 本批生产文件里出现的**活键字面量** ∩ `Core/Loc.cs` 的表键（超集无害、且更严）。
        // 本宿主覆盖的生产文件 = `Shell/{InboxWindow,DeckSelectionPopup,ImportDeckPopup,SettingsWindow,ProfileTab,LeaderboardWindow,BattleLogPopup,CampaignTab}.cs`。
        // 🧨 改坏法：① `Loc.cs` 删掉本批任一条键 ⇒ ①红；② 某条**英文列**填中文/全角空格 ⇒ ③红；
        //   ③ **中文列**清空 ⇒ ②红；④ 值改成键名本身 ⇒ ②红；⑤ 表删掉一半 ⇒ ④红（`EntryCount` 掉到基线之下）；
        //   ⑥「把实现与期望一起改回写死中文 **并** 把表里那条删掉」⇒ ①红（数组里那条键仍在、`HasEntry` 假）—— 这正是本数组存在的唯一理由。
        {
            string[] a1025Keys =
            {
                "Demo/MainMenu/ExitButton", "Demo/MainMenu/ExitGame", "MainMenu/General/Confirm",
                "MainMenu/General/OK", "MainMenu/General/Off", "MainMenu/General/On",
                "MainMenu/RankedWindow/LeaderboardOfflineNote", "MainMenu/Settings/ButtonLabel/Exit_Game", "MainMenu/Settings/ButtonLabel/SelectLanguage",
                "MainMenu/Settings/SettingLabel/Music", "MainMenu/Settings/SettingLabel/SoundFx", "MenuCollection/NoDecksFound",
                "MenuDeck/Button/Random", "MenuDeck/Error/NoUsablePrebuilt", "MenuDeck/Error/PrebuiltMissing",
                "MenuDeck/HUD/EnterText", "MenuDeck/Share/PasteDeck", "MenuDeck/Tip/SelectDeckAgainst",
                "Settings/General/DisableBots", "Settings/General/DisableNotifications", "Settings/General/Flash/Language",
                "Settings/General/LangHasNoTable", "Settings/General/RedeemCode", "Settings/General/RedeemCodeUnavailable",
                "Settings/General/Title", "Settings/General/TouchInput", "Settings/Graphics/AutoZoom",
                "Settings/Graphics/EnableSuperSampling", "Settings/Graphics/Flash/AutoZoom", "Settings/Graphics/Flash/Fps",
                "Settings/Graphics/Flash/Quality", "Settings/Graphics/Flash/SmallScreenUI", "Settings/Graphics/Flash/SuperSampling",
                "Settings/Graphics/Flash/Vsync", "Settings/Graphics/FpsText/Unlimited", "Settings/Graphics/FpsText/Value",
                "Settings/Graphics/FrameLimit", "Settings/Graphics/IncreaseUISize", "Settings/Graphics/SelectQuality",
                "Settings/Graphics/Title", "Settings/Graphics/UnlimitedFPS", "Settings/Graphics/Vsync",
                "Settings/Media/Title", "Settings/Media/VoiceOvers",
                "Settings/Online/CheckConnection", "Settings/Online/ClickAgain", "Settings/Online/HostFailed",
                "Settings/Online/HostReady", "Settings/Online/HostReadyNoPassword", "Settings/Online/HostReadyToFriend",
                "Settings/Online/HowToConnect/DontUseTestSite", "Settings/Online/HowToConnect/Intro", "Settings/Online/HowToConnect/Lan",
                "Settings/Online/HowToConnect/LocalCheckTitle", "Settings/Online/HowToConnect/NoHolePunching", "Settings/Online/HowToConnect/PublicDirect",
                "Settings/Online/HowToConnect/PublicV6No", "Settings/Online/HowToConnect/PublicV6Yes", "Settings/Online/HowToConnect/UpnpNote",
                "Settings/Online/HowToConnect/VirtualLan", "Settings/Online/HowToConnect/VirtualNicNo", "Settings/Online/HowToConnect/VirtualNicYes",
                "Settings/Online/IpLabel", "Settings/Online/IpPlaceholder", "Settings/Online/IsV6",
                "Settings/Online/LocalAddr", "Settings/Online/NetRuntimeMissing", "Settings/Online/NoNicFound",
                "Settings/Online/PasswordLabel", "Settings/Online/PasswordPlaceholder", "Settings/Online/ProbingPublicAddress",
                "Settings/Online/PublicAddress/BothOk", "Settings/Online/PublicAddress/LocalV6", "Settings/Online/PublicAddress/Mismatch",
                "Settings/Online/PublicAddress/None", "Settings/Online/PublicAddress/NotFound", "Settings/Online/PublicAddress/Title",
                "Settings/Online/PublicAddress/V4", "Settings/Online/PublicAddress/V6", "Settings/Online/Refresh",
                "Settings/Online/RoleClient", "Settings/Online/RoleHost", "Settings/Online/Save",
                "Settings/Online/StatusNoSession", "Settings/Online/TestPublicIp", "Settings/Online/Title",
                "Settings/Online/TitleNote", "Settings/Online/TitleNoteBody", "Settings/Online/VirtualNic",
            };
            foreach (var k in a1025Keys)
                CheckTrue(Loc.HasEntry(k), $"★（A1025）本批键都在表里：`{k}`");
            // ② 两语档各取一次、都非空且 ≠ 键名（只断中文档 = 半边绿）
            var a1025LangWas = Loc.Current;
            foreach (var lang in new[] { AvailableLanguages.Chinese, AvailableLanguages.English })
            {
                Loc.RestoreForTest(lang);
                int bad = 0; string firstBad = null;
                foreach (var k in a1025Keys)
                {
                    string v = Loc.T(k);
                    if (string.IsNullOrEmpty(v) || v == k) { bad++; if (firstBad == null) firstBad = k; }
                }
                CheckTrue(bad == 0, $"★（A1025）`{lang}` 档下 {a1025Keys.Length} 条**全部取到真文案**（缺 {bad} 条"
                          + (firstBad == null ? "" : $"，第一条 `{firstBad}`") + "）"
                          + " —— 取不到时会印**键名本身**，那就是静默失败");
            }
            Loc.RestoreForTest(a1025LangWas);      // ⛔ 只改内存、不写 `PlayerPrefs`（不是 `SetLanguage`）
            // ③ 灭自证 C1：英文列不许含汉字（`Loc.HasCjk` 的区间含 `0x3000-0x303F` 与 `0xFF00-0xFFEF`）
            {
                int cjk = 0; string firstCjk = null;
                foreach (var k in a1025Keys)
                {
                    string en = Loc.EnOf(k);
                    if (!string.IsNullOrEmpty(en) && Loc.HasCjk(en)) { cjk++; if (firstCjk == null) firstCjk = k; }
                }
                CheckTrue(cjk == 0, $"★（A1025）本批 {a1025Keys.Length} 条的**英文列无 CJK**（坏 {cjk} 条"
                                  + (firstCjk == null ? "" : $"，第一条 `{firstCjk}`") + "）");
            }
            // ④ 灭自证 D（表基线）：开工前实测 `EntryCount == 429`
            CheckTrue(Loc.EntryCount >= 429,
                      $"★（A1025）表基线：`Loc.EntryCount` = {Loc.EntryCount} ≥ **429**（开工前实测）"
                    + "｜🧨 把键删掉、断言也一起删 ⇒ 这条红");
        }

        // ============================================================ ★ A1126 · `A1` 档（6 处 autosize 接线）
        //
        // 🔴 **这一节盯的是什么**：`A1126` 的**真判据** = 「那处调用的实参**真的生效**」**或**「调用之后对该
        //   `Label` 调过 `SetAutoFitBox`」—— ⛔ **不是**旧说法「调用方没传 `autoMinPx` 就不接 autosize」
        //   （那条**只是必要条件、已被现核推翻**：全仓有一批是「调用后另调 `SetAutoFitBox`」这一形态
        //   ——简报口径 **45 处**，本件**未自己数过**；**我逐字核过的那一条**是
        //   `Shell/LiveOpsEventWindow.cs` 一个 autosize 实参都没传、却调了 ⇒ 它是**接上的**）。
        //   两条关键事实（都在 `Shell/MenuDraw.cs`）：
        //     ① `TextCore` 里 `SetAutoFitBox` **关在 `if (wrapPx > 0f)` 里面**（`CardPresentation/Shell/MenuDraw.cs` 是那道闸、`CardPresentation/Shell/MenuDraw.cs` 才调）
        //        ⇒ **传了 `autoMinPx` 但没传 `wrapPx` 照样不生效**（死实参）；
        //     ② `SetWrapWidth` **无条件**把折行模式写成 `Normal(1)` ⇒ 原版 `折行=0` 的件必须紧跟 `SetWrapping(false)`。
        // 🔴 **期望值全是原版那一颗的实读字段**（`m_enableAutoSizing` / `m_fontSizeMin` / `m_fontSizeMax` /
        //   `m_fontSizeBase` / `m_TextWrappingMode`），**逐颗现读**（命令与读数写在各处实现侧的注释里）——
        //   ⛔ 不读我们传进去的实参、⛔ 不读实现里的常量（那是**自证**）。
        // 🔴 **为什么必须断「渲出来 ≤ 框」**（`CLAUDE.md` §二 那条 `AutoFitBox` 教训）：只比字号 ⇒
        //   「字号字段对、字却溢出框」照样全绿。量的东西 = TMP 自己那块 `textBounds`（`ShellLabelRenderedPx`），
        //   ⛔ 不是 `Label.WorldW/H`（**被测实现自己写的字段缓存** ⇒ 拿它当检测器 = 自证）。
        // ⚠️ **本节只覆盖 5 处运行时 + 1 处源码文本**：第 6 处（`Shell/CardDetailPopup.cs` 的 `Craft Explanation`）
        //   走**源码文本断言**（见 ④）。
        //   🔴 **2026-10-18 就地订正（铁律 5）**：本行原来写的「两块面板按用户 2026-09-27 拍板整块不建 ·
        //   `BuildCrafting` **全库零调用点** ⇒ 现有夹具够不到那颗 `Label`」**已过期** —— 2026-10-12 用户裁定
        //   「请你恢复」，`Build()` 现在对可合成的稀有度会调 `BuildCrafting`；运行时那一侧由
        //   `Editor/CollectionScene.cs` 的「卡片详情窗」那节直接断节点与文案。
        // 🆕 **2026-10-20（A1192）**：本块又接了 **1 处运行时** —— `Shell/CardDetailPopup.cs` 的
        //   `Buy Original Card`（`Alternate Art Panel` 那颗价签）= `P4` §7·2 登记的那 4 处里**唯一活在
        //   生产路径上**的那一处 ⇒ 见 ⑤（同族另 3 处在 `BuildUpgrade` 里，
        //   按 `A1181` 那条先例**如实登记、不为它造测试路径**）。
        //   🔴 **2026-10-18 就地订正（铁律 5）**：本段原来给那 3 处标的「**全库零调用点**」**已过期** ——
        //   2026-10-12 用户裁定恢复两块面板后，`BuildUpgrade` 也**活在路径上**（每一张卡的详情窗都会建它）。
        // 🔴 **改坏法（逐条）**：把任一处末尾那串 autosize 实参删回默认（或 `wrapPx` 删回 `0f`）⇒ 该处的
        //   `AutoSizing` / `m_fontSizeMin/Max` / `m_fontSizeBase` 那几条红；删掉两颗 `折行=0` 处紧随的
        //   `SetWrapping(false)` ⇒ 折行档那条红；把字号调大到框外 ⇒ 「渲出来 ≤ 框」那条红。
        Section("★ A1126 · `A1` 档 6 处接上 autosize（5 处运行时 + 1 处源码文本）＋ A1192 补 1 处（`Buy Original Card`）");
        {
            // 逐条量法：节点上的 `Label` 找得到 ⇒ 断「自适应开了 / 上下限 = 原版 / base = 原版 / 折行档 = 原版 /
            //   渲出来 ≤ 框」。⚠️ 五个期望值**全是原版字面量**（⛔ 不读实现里的常量名，那是同义反复）。
            System.Action<string, Transform, float, float, float, float, float, int, string> fitCase =
                (host, node, boxW, boxH, minPx, maxPx, basePx, wrapMode, evidence) =>
            {
                var lb = node != null ? node.GetComponentInChildren<Label>(true) : null;
                CheckTrue(lb != null, $"（前提·不静默）A1126 · {host}：那一颗的 `Label` 拿得到"
                                    + " —— ⛔ 拿不到就**不往下断**（不静默变绿）");
                if (lb == null) return;
                CheckTrue(lb.AutoSizing,
                          $"★ A1126 · {host}：**真的开了自适应**（原版 `m_enableAutoSizing = 1`；{evidence}）"
                        + "｜改坏法：把那处调用末尾那串 autosize 实参删回默认（或 `wrapPx` 删回 `0f`）⇒ 本行红");
                // 🔴 **2026-10-18（`N55`）**：口径由静态 Cjk 口改成**按这颗标签实际生效的面** ——
                //   `EmPerFontSize` 自本日起按面分两支（CJK 汉字墨高 0.0948 / 拉丁面原版真 em 0.1），
                //   本助手横扫全壳窗口（含 `LatinUi`/`LatinCardBody` 的站）⇒ 静态口会恒差 5.2%。
                //   CJK 标签的读数逐位不变。判据 → `R4_Rewards残差.md` §五·B ＋ `D1` §四 方案 B(b)。
                CheckNear(lb.FontSizeToPxNow(lb.FontSizeMin, lb.ActualFace), minPx, 0.05f,
                          $"A1126 · {host}：自适应下限 = 原版 `m_fontSizeMin` **{minPx}px**"
                        + "（⛔ 不是我们那一侧的字号常量）");
                CheckNear(lb.FontSizeToPxNow(lb.FontSizeMax, lb.ActualFace), maxPx, 0.05f,
                          $"A1126 · {host}：自适应上限 = 原版 `m_fontSizeMax` **{maxPx}px**"
                        + "｜改坏法：第 10 参不传 ⇒ 退回旧行为「上限 = 标称字号」那一档（本处两者同值，故本条咬的是 min/传参那两处）");
                CheckNear(lb.FontSizeToPxNow(lb.FontSizeBase, lb.ActualFace), basePx, 0.05f,
                          $"A1126 · {host}：`m_fontSizeBase`（自适应二分起点）= 原版 **{basePx}px**"
                        + "（`Label.FontSizeBase` 直读 TMP 的 `m_fontSizeBase`）");
                Check(lb.WrappingMode, wrapMode,
                      $"A1126 · {host}：折行档 = 原版 `m_TextWrappingMode = {wrapMode}`"
                    + (wrapMode == 0
                       ? "｜改坏法：删掉紧跟 `MenuDraw.Text` 的那句 `SetWrapping(false)` ⇒ 回 1（`SetWrapWidth` 会无条件开折行）"
                       : ""));
                var sz = ShellLabelRenderedPx(lb);
                // ⚠️ **2026-10-20（A1192）：「量得到吗」那道闸补上【上界】**—— TMP 在**未重排 / 空串 /
                //    从没生成过字形**时给的是**哨兵天文数字**（`Battle/Label.cs` 的 `HasMeasuredWidth`
                //    文件头实测 4.29e9 一档；`Editor/BattleScene.cs` 那条 A1192 上界闸记的是 4.6e11）
                //    ⇒ 那种读数**不是「字太宽」**，是**量法没生效**：分开报，⛔ 别让它冒充「溢出」
                //    （冒充会把下一个会话引到错方向 —— 同 `资料/已知的坑.md` 那条「量法没生效别当成缺陷」）。
                bool szOk = sz.x > 0.5f && sz.x < 100000f && sz.y > 0.5f && sz.y < 100000f;
                CheckTrue(szOk && sz.x <= boxW + 1.5f && sz.y <= boxH + 1.5f,
                          szOk
                          ? $"A1126 · {host}：**渲出来 {sz.x:F1}×{sz.y:F1}px ≤ 框 {boxW:F2}×{boxH:F2}**"
                          + "（容差 1.5px = TMP 二分收敛粒度；只比字号会漏掉「字号对而溢出」那一族）"
                          : $"A1126 · {host}：**字块量不到**（哨兵/未重排 ⇒ 这一条**没跑**，不是绿）"
                          + $" —— 实得 {sz.x:F0}×{sz.y:F0}px（本闸上界 100000px）");
            };

            // ---------------- ① `Shell/SkirmishEventWindow.cs`（两处）----------------
            // 🔴 **2026-10-24（`E15` 之后的本节前提）**：遭遇战窗现在**分两版左列** ——
            //   `HasActiveSkirmishEvent=false`（本地现况）画**经典式**、`true` 才画 `Reward Display`。
            //   本节量的是**活动期那套 prefab 的尺子**（`Reward Help` / `Vicotries title` / `Reward Tile` …）
            //   ⇒ 必须显式把判据钉成「有在期活动」，否则节点根本不存在、四条全红（且是**因为错的原因**）。
            //   ⚠️ 跑完必须还原（`finally` 口径见本节末尾），⛔ 别漏到别的宿主。
            SkirmishEventWindow.ForceActiveEventForTest = true;
            // 期望值出处 = `工具/menu_dump.py bundle_menus_assets_all "SkirmishModeEventWindow" --md` 逐颗现读。
            shell.Windows.CloseAllWindows();
            var a1126Sk = SkirmishEventWindow.Create(shell.Windows);
            shell.Windows.OpenWindow(a1126Sk);
            CheckTrue(a1126Sk != null && a1126Sk.CurrentState == WindowState.Open,
                      "（前提）遭遇战窗开起来了（`Reward Help` / `Vicotries title` 两颗挂在它下面）");
            if (a1126Sk != null)
            {
                fitCase("`Shell/SkirmishEventWindow.cs` 的 `Reward Help`（`TextBox`）",
                        FindChildIn(a1126Sk.transform, "Reward Help"),
                        592.774f, 100.99f, 18f, 38f, 36f, 1,
                        "原版 `…/Reward Display/Reward Help` 实读 `auto[18~38]`（`m_fontSize 38`）");
                fitCase("`Shell/SkirmishEventWindow.cs` 的 `Vicotries title`",
                        FindChildIn(a1126Sk.transform, "Vicotries title"),
                        236.63f, 50f, 18f, 48f, 36f, 1,
                        "原版 `…/Player victories/Vicotries title` 实读 `auto[18~48]`（`m_fontSize 48`）");
                // ---- A1179（2026-10-19）：本窗**另外两处**同一把尺子（⛔ 不抄第二份 `fitCase`）----
                // 期望值出处 = `python -I d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all
                //   "SkirmishModeEventWindow" --depth 6 --relative --no-sprite --no-layout` 逐颗现读。
                // 改坏法：把 `Shell/SkirmishEventWindow.cs` 那两处调用末尾的 autosize 实参删回默认 ⇒ 各红 4 条。
                fitCase("A1179 · `Shell/SkirmishEventWindow.cs` 的 `Reward Tile`",
                        FindChildIn(a1126Sk.transform, "Reward Tile"),
                        522.00f, 54.69f, 18f, 45.87f, 36f, 1,
                        "原版 `Reward Display/Reward Tile` 实读 `auto[18~45.87]`（`m_fontSize 45.87` · 基准 36 · 折行 1 · 框 522.01×54.68）");
                fitCase("A1179 · `Shell/SkirmishEventWindow.cs` 的 `Total Victories`",
                        FindChildIn(a1126Sk.transform, "Total Victories"),
                        180.84f, 50f, 18f, 48f, 36f, 1,
                        "原版 `…/Player victories/Skull Victories/Total Victories` 实读 `auto[18~48]`（框 180.84×50.00 与原版逐位同值）");
                // ---- A1190（2026-10-19）：本窗**再一处**同一把尺子（⛔ 不抄第二份 `fitCase`）----
                // 期望值出处 = `python -I d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all
                //   "SkirmishModeEventWindow" --depth 8 --md` 逐颗现读：
                //   `…/Banned card in deck/Text` = `'The deck has banned cards'` · 字号 **50.0** ·
                //   基准 **36.0** · `auto[18.0~50.0]` · 对齐 `Center/Midline` · **折行 = 1** · 框 **678.30×53.93**。
                // 改坏法：把 `Shell/SkirmishEventWindow.cs` 的 `BuildBanned` 那处 `MenuDraw.Text` 末尾那串
                //   autosize 实参删回默认（或 `wrapPx` 删回 `0f`）⇒ 本处红 4 条。
                // ⚠️ 这一颗的节点出厂被我们**主动关着**（`b.gameObject.SetActive(false)`）⇒ `FindChildIn`
                //   含 inactive 捞得到；量的是它建的时候（整棵树当时是活的）生成、关掉之后**仍缓存着**的
                //   `textBounds`（`ShellLabelRenderedPx` 读的就是它）。
                // 🔴 **2026-10-19（`A1198` · 铁律 11）：「可能假绿」两说的裁定 + 修法** ——
                //   · 说 (a)：TMP 的 `textBounds` 是**缓存字段**，`OnDisable` 不清它 ⇒ 量到的是**建树时的真值**
                //     （建树那一趟整棵树是活的、`SetText` 走过 `ForceMeshUpdate`）⇒ 今天的绿是**真绿**；
                //   · 说 (b)：若哪一版 TMP 在 `OnDisable` 里把它清成 0 ⇒ 现在 `fitCase` 的 `szOk`
                //     （`sz.x > 0.5f` 那道闸，见函数头）会**当场红**（报「字块量不到」）⇒ **也不再是假绿**。
                //   ⚠️ **两说静态判不出哪一版 TMP 是真**（`Unity.TextMeshPro.dll` 源码不在工程里）。
                //   ⇒ **不赌**：量之前**临时点亮 + 手动重排一次**、量完**还原**（先例 = `Editor/BattleScene.cs`
                //     里 `QPText_Me` / `QPText_Foe` 那对夹具（注释「先点亮、量完还原」）—— 同一形状：
                //     `SetActive(true)` → 量 → `SetActive(was)`；⛔ 别按行号找，那个函数一直在长）。
                //     ⛔ 不改产品显隐逻辑（它继续是出厂关着的 —— 那是原版的运行时行为）。
                var a1190Banned = FindChildIn(a1126Sk.transform, "Banned card in deck");
                bool a1190BannedWas = a1190Banned != null && a1190Banned.gameObject.activeSelf;
                if (a1190Banned != null)
                {
                    a1190Banned.gameObject.SetActive(true);
                    // 批处理没有帧循环 ⇒ 点亮之后要**显式推一次**版面（`ForceRelayout` = `ForceMeshUpdate`
                    // + `RefreshBounds`，幂等；口径同 `Label.ForceRelayout` 的 doc）。
                    var a1190Lb = a1190Banned.GetComponentInChildren<Label>(true);
                    if (a1190Lb != null) a1190Lb.ForceRelayout();
                }
                try
                {
                    fitCase("A1190 · `Shell/SkirmishEventWindow.cs` 的 `Banned card in deck/Text`",
                            a1190Banned,
                            678.30f, 53.93f, 18f, 50f, 36f, 1,
                            "原版 `…/Banned card in deck/Text` 实读 `auto[18~50]`（`m_fontSize 50` · 基准 36 · 折行 1 · 框 678.30×53.93）");
                }
                finally
                {
                    // 还原出厂态（产品逻辑 = 原版运行时按「这副牌有没有禁用卡」开关；本地没有那份表 ⇒ 恒关）
                    if (a1190Banned != null) a1190Banned.gameObject.SetActive(a1190BannedWas);
                }
                // ---- 🆕 2026-10-17（`N31`）：`Score Levels` 五行是 VLG `reverse=1` —— 树序第一颗 `Level 1` 在最【下】----
                //   判据 = `python -I d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "SkirmishModeEventWindow"
                //   --depth 10 --relative --no-sprite` 现读：`Score Levels … reverse=1`；
                //   `Level 1` y **731.9..807.9** · `Level 5` y **406.4..482.4**（设计 px，上到下）。
                //   改坏法：把 `SkirmishEventWindow.ScoreLevelRowCy` 改回旧公式 `LevelY0 + LevelRowH*(i+0.5)`
                //   （下标越大越靠下）⇒ 下面两条都红。
                CheckTrue(SkirmishEventWindow.ScoreLevelRowCy(0) > SkirmishEventWindow.ScoreLevelRowCy(4),
                          "★ N31：`Level 1`（树序第一颗）的 y **大于** `Level 5` —— `Score Levels` VLG `reverse=1` ⇒ 第一颗在最下");
                var skLv1 = FindChildIn(a1126Sk.transform, "Score Bar Line Level 1");
                var skLv5 = FindChildIn(a1126Sk.transform, "Score Bar Line Level 5");
                CheckTrue(skLv1 != null && skLv5 != null && skLv1.position.y < skLv5.position.y,
                          "★ N31：屏上 `Level 1` 在 `Level 5` **下面**（世界 y 向上 ⇒ 更小）；镜像改回去 ⇒ 红");
                a1126Sk.Close();
                CheckTrue(a1126Sk.CurrentState == WindowState.Closed,
                          "收尾：遭遇战窗关掉（它的压暗层是整屏的）");
            }

            // 🔴 **2026-10-24（`E15`）**：还原活动期探针（本节量的是活动期那套 prefab，见本节开头）。
            //   新版左列的断言（非活动期 = 经典式）在 `SkirmishWindowAssertE15` 里，另行挂载。
            SkirmishEventWindow.ResetEventProbeForTest();

            // 🆕 2026-10-24（执行代理 `E15`）：遭遇战窗**两版左列**的断言
            //   A=非活动期（经典式，本地现况）/ B=活动期（走生产判据链注入合成事件）/ C=两族节点逐名异或。
            SkirmishWindowAssertE15.Run(shell.Windows);
            _sink.Pass += SkirmishWindowAssertE15.Pass; _sink.Fail += SkirmishWindowAssertE15.Fail;

            // ---------------- ② `Shell/TutorialModePopup.cs`（一处）----------------
            // 期望值出处 = `工具/menu_dump.py bundle_menus_assets_all "Tutorial Mode Menu" --depth 8 --md`。
            shell.Windows.CloseAllWindows();
            var a1126Tut = TutorialModePopup.Create(shell.Windows);
            shell.Windows.OpenWindow(a1126Tut);
            CheckTrue(a1126Tut != null && a1126Tut.CurrentState == WindowState.Open,
                      "（前提）教程模式窗开起来了（`PlayTutorialButton/Button Text` 在它下面）");
            if (a1126Tut != null)
            {
                fitCase("`Shell/TutorialModePopup.cs` 的 `PlayTutorialButton/Button Text`",
                        FindChildIn(FindChildIn(a1126Tut.transform, "PlayTutorialButton"), "Button Text"),
                        408.77f, 106.49f, 10f, 74.25f, 12f, 0,
                        "原版 `Tutorial Mode Menu/…/PlayTutorialButton/Button Text` 实读 `auto[10~74.25]` · 折行 0");
                a1126Tut.Close();
                CheckTrue(a1126Tut.CurrentState == WindowState.Closed, "收尾：教程模式窗关掉");
            }

            // ---------------- ③′ `A1262`（2026-10-21）：CJK 字体资产的 `m_Scale` 与「有效行盒」----------------
            // 期望值**全部取自原版资产**（`d:/2/新解包资源/assets_full/bundle_fonts_assets_all/MonoBehaviour/
            // NotoSerifCJK-Regular SDF.json` 的 `m_FaceInfo`：pointSize 50 · lineHeight 71.85 · **scale 0.9**），
            // ⛔ 不取我们自己那份 —— 否则就是「拿实现证明实现」。
            // 判据链：TMP 的一行字盒 = `(ascentLine − descentLine)/pointSize × faceInfo.scale`
            //   （包源码 `TMP_Text.cs:4188/4190` 把 `fontFace.scale` 乘进 `adjustedScale`，
            //    `:4391/:4396` 再用它算 ascender/descender）⇒ 有效行盒 = 1.437 × 0.9 = **1.2933 em**。
            // ⚠️ **本组是 `A1262` 唯一会动的断言**：它读的是**资产字段**。画面/版面那一侧
            //   **逐位不变**（见下面 A1178 那段 2026-10-21 的补注：px 口径按**实测字形高**折算，
            //   而字形高与行盒同时乘 `m_Scale` ⇒ 比值与 `m_Scale` 无关）。
            {
                var cjkFa = Resources.Load<TMPro.TMP_FontAsset>("Fonts/NotoSerifCJK-Regular SDF");
                CheckTrue(cjkFa != null,
                          "（前提·不静默）A1262：`Resources/Fonts/NotoSerifCJK-Regular SDF` 取得到"
                        + " —— ⛔ 取不到就**不往下断**（不静默变绿）");
                if (cjkFa != null)
                {
                    float cjkPs = cjkFa.faceInfo.pointSize;
                    CheckNear(cjkPs, 50f, 1e-3f,
                              "★ A1262：CJK 资产 `m_PointSize` = **50**（原版 `m_FaceInfo.m_PointSize`）");
                    CheckNear(cjkFa.faceInfo.scale, 0.9f, 1e-4f,
                              "★ A1262：CJK 资产 `m_Scale` = 原版 **0.9**（原版 `m_FaceInfo.m_Scale`；"
                            + "我们原来是 1 ⇒ 同一 `m_fontSize` 下汉字大 11%、行盒高 11%）"
                            + "｜实得 " + cjkFa.faceInfo.scale.ToString("F4")
                            + "｜🧨 把 `Resources/Fonts/…SDF.asset` 或 `TmpSetup` 那一刀改回 1 ⇒ 本行红");
                    if (cjkPs > 0f)
                    {
                        CheckNear(cjkFa.faceInfo.lineHeight / cjkPs, 1.437f, 0.002f,
                                  "★ A1262：**face 行盒** `lineHeight ÷ pointSize` = 1.437 em"
                                + "（原版同一份 TTF 度量：hhea 1151/−286 @upem 1000；`71.85 ÷ 50`）");
                        CheckNear(cjkFa.faceInfo.lineHeight / cjkPs * cjkFa.faceInfo.scale, 1.2933f, 0.005f,
                                  "★ A1262：**有效行盒** = face 行盒 × `m_Scale` = **1.2933 em**"
                                + "（判据 = `TMP_Text.cs:4188/4190` 的 `… / pointSize * faceInfo.scale`；"
                                + "原版运行期三条旁证 1.2944 / 1.2931 / 1.2959，见 `panel_0914b/p3_*.tsv`）"
                                + "｜🧨 少了 `m_Scale` 那一格 ⇒ 本行红（我们曾经就是这一档）");
                    }
                }
            }

            // ---------------- ③ `Shell/EnergySinglePlayerOnlyEventWindow.cs`（两处）----------------
            // 期望值出处 = `工具/menu_dump.py bundle_menus_assets_all "EnergySinglePlayerOnlyEventWindow" --depth 6 --md`。
            // ⚠️ 原版这两颗的**框宽都是 0**（+ `ContentSizeFitterMinMax(h:PreferredSize)` ⇒ 运行时框宽 = 文字的
            //   preferred width，「算不出」）⇒ 下面那两个框宽是**我们摆的那一份**（⛔ 不是原版数字，已如实标注；
            //   残差 → `资料/普查产出_第十会话/W2_A1126A1.md` §五）。
            shell.Windows.CloseAllWindows();
            var a1126En = WindowsManager.OpenEnergyEvent();
            CheckTrue(a1126En != null,
                      "（前提）能量活动窗开出来了（`Timer` / `Victories title` 两颗挂在它下面）");
            if (a1126En != null)
            {
                // 🔴 **2026-10-10（A1178）**：这两颗的**框**从「我们摆的固定宽」改成了「**建树期量出来的
                //   preferred 宽**」（原版是 `ContentSizeFitter(h:PreferredSize)` ⇒ 原版运行期框跟文字走）。
                //   ⇒ **框那两格在这里【故意给到整屏】**：真框是**运行时量出来的**，拿它当字面量断
                //   就是**同义反复**（⛔ 本仓明令：期望值不许从实现里来）。牙口搬到下面的**关系**断言上。
                fitCase("`Shell/EnergySinglePlayerOnlyEventWindow.cs` 的 `Timer`",
                        FindChildIn(a1126En.transform, "Timer"),
                        1920f, 1080f, 10f, 38f, 38f, 0,
                        "原版 `…/Timer/Timer` 实读 `auto[10~38]` · 折行 0（框宽 **0 + CSF**）；"
                      + "⚠️ 框那两格**故意给整屏** —— 见下面 A1178 的关系断言");
                fitCase("`Shell/EnergySinglePlayerOnlyEventWindow.cs` 的 `Vicotries title`",
                        FindChildIn(a1126En.transform, "Vicotries title"),
                        1920f, 1080f, 18f, 53.5f, 36f, 1,
                        "原版 `…/PLayer Victories/Vicotries title` 实读 `auto[18~53.5]`（框宽 **0 + CSF**）；"
                      + "⚠️ 框那两格**故意给整屏** —— 见下面 A1178 的关系断言");
                // 🔴 **2026-10-10（A1178）**：这两颗的牙口 = 下面那段**关系断言**（⛔ 旧形态
                //   「收敛字号 ≈ `m_fontSizeMax`」**已废** —— 它是 (α) 断言错，错因写在下面那段头里）。
                //   🔑 **这一节的来历**：「`Timer` 长期被压小却全绿」（`P-I` 查实：它需要 ≈330–396px、
                //   而旧框只有 `289.72` ⇒ 实得 27.8–33.4px 而非 38px；上面 `fitCase` 那条
                //   「渲出来 ≤ 框」因为框那两格**故意给整屏**，本来也挡不住这一档）。
                // 🔴 **2026-10-10（A1178）·【关系断言】（⛔ 不是字面量、⛔ 不是「撞上限」）**。
                //   **原形态是错的**（本件现核 · `A1178` 那两条红 = (α) 断言错）：它断的是
                //   「**收敛字号 ≈ 窗口上限**」=「字没被缩」，**前提只覆盖了一半** —— 它默认
                //   「框已按 preferred 撑开 ⇒ 宽度不再是约束 ⇒ TMP 只剩上限可撞」。而 `A1178`
                //   撑开的**只有宽**（`Shell/EnergySinglePlayerOnlyEventWindow.cs` 的 `txFit`
                //   只改左沿 + 宽度，`y1/y2` 原样）⇒ **紧的其实是【高度】**。
                //   机制（判据 = 工程自带的包源码）：TMP 有**两条**降字号的闸 ——
                //   **竖界** `TextMeshProUGUI.cs:3393` `if (textHeight > marginHeight + 0.0001f)`
                //   （降到 `:3424-3430`，`*20+0.5)/20` = 1/20 粒度）与 **横界** `:3644-3650`；
                //   `marginHeight/Width` **就是我们填进去的框**（`ComputeMarginSize`，`:2343`）
                //   ⇒ **收敛值由「较紧的那一侧」决定**，「较紧」只能拿**框**与**渲出**比出来。
                //
                //   🔴 **框高是不是原版的？（本件现核，⛔ 不是转述）** —— 命令
                //     `python -I d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all
                //      "EnergySinglePlayerOnlyEventWindow" --depth 8 --md --no-sprite`：
                //     · `…/Timer/Timer`（TMP）= `m_SizeDelta = (0, **55.905**)` · 锚 `(0,1)→(0,1)`
                //       （**锚点不拉伸 ⇒ `rect.height` 逐位 = `sizeDelta.y`**）· `auto[10~38]` · 基准 38 · 折行 0；
                //     · `…/PLayer Victories/Victories title` = `m_SizeDelta = (0, **73.385**)` · 同锚 ·
                //       `auto[18~53.5]` · 基准 36 · 折行 1。
                //     ⇒ **框高是原版的**（我们摆的是 55.90 / 73.38，差 0.005px）⇒ **同一把框高，原版装得下、我们装不下**：
                //     原版收敛在上限 38 / 53.5 ⇒ 它的字体行盒 **≤ 55.905 ÷ 38 = 1.47em**（否则竖界早就把它压下来）；
                //     而**我们那份正文字体行盒 ≈ 1.52em** ⇒ `38 × 1.52 = 57.8 > 55.905` ⇒ **被竖界压住**
                //     （四条独立读数：1.5170 / 1.5252 / 1.5470 / 1.5252，见 `D1_九条红诊断.md` §证据 A）
                //     ⇒ 我们只能收敛到 36.851 / 48.111。**这是字体度量的差（`NotoSerifCJK` 对
                //     `Pragati`）、不是框摆错了**，残差登记在 `W2_A1126A1.md` §五；⚠️ 「行盒 1.52em vs
                //     原版 ≤1.47em」这条**系统性**差异要落 `项目任务.md` §三 正本（`D1` §建议处置 3 明写）——
                //     **本件白名单里没有正本** ⇒ 只在此处标出处，不动正本。
                //
                //   ⇒ 所以本处改成**两条关系式**（期望值一个都不取自实现）：
                //     ① **高度那一侧是【紧】的**：渲出高 ≈ **原版那一颗的框高**（±1.5px = TMP 二分粒度）
                //        —— 「收敛字号由框高决定」这句的可判形式；
                //     ② **宽度那一侧【不是】约束**：**框宽 − 渲出宽 ≥ 1.5px**（**有余量**，不是刚好顶住）。
                //
                //   ⚠️ **改坏法（逐条）**：把 `Shell/EnergySinglePlayerOnlyEventWindow.cs` 那两处的框
                //     **改回旧常量**（`Timer` `289.72f` / `Victories title` `254.112f`）⇒ 宽度重新变成约束：
                //     · `Timer`：收敛掉到 ≈29.6px ⇒ 渲出高 ≈44.8px ⇒ **① 红（离 55.905 差 11px）**；
                //     · `Victories title`：**这一档观察不到**（见下），**① / ② 都不该红**（真话，不是漏断）。
                //     框高改错 / 字被任何一侧压小 ⇒ **① 红**。
                //   ⚠️ **② 的牙口比 ① 弱，如实写清**：宽度真变成约束时，`框宽 − 渲出宽` 的余量
                //     不是 0 而是「**不到一个自适应步长**」（TMP 二分停在 1/20 fontSize 单位 ⇒
                //     对 `Timer` 那条串≈5.0px）⇒ ② 只有余量 < 1.5px 时才咬得到。
                //     ⛔ **不把阈值抬到 5px 去换满咬合**：那时正确态的余量（`Timer` ≈11.3px）只剩
                //     3.7px 富余，没跑过 Unity 就定这么紧 = 拿**假红**换牙口。真正兜住这一档的是 ①。
                //   ⚠️ **已知够不到的一档（如实登记 · 铁律 2/5·b，⛔ 不是漏断）**：`Victories title`
                //     把框改回 `254.112` 在**画面上等价** —— 它渲染只占 234.6px，而**竖界要的 234.6
                //     才是紧的那一侧**（`254.112 > 234.6` ⇒ 宽度侧允许的字号 52.1px 仍 > 高度侧的 48.1px
                //     ⇒ 收敛值与版面逐位不变）。⇒ 这一档**只能**靠「框宽 == TMP 的 preferred 宽」那种
                //     **同义反复**去咬（本仓明令不用，见 `CLAUDE.md` §三「灭自证」）⇒ **本件不写**、
                //     如实记进报告。真正的回归面由 ① 兜住（`Timer` 那一半是硬的）。
                foreach (var nm in new[] { "Timer", "Vicotries title" })
                {
                    // 原版那一颗的 `m_SizeDelta.y`（现读 · 见上面那段命令与读数）
                    float boxHOrig = nm == "Timer" ? 55.905f : 73.385f;
                    var t2 = FindChildIn(a1126En.transform, nm);
                    var l2 = t2 != null ? t2.GetComponentInChildren<Label>(true) : null;
                    CheckTrue(l2 != null, $"（前提·不静默）A1178 · `{nm}` 的 `Label` 拿得到"
                                        + " —— ⛔ 拿不到就**不往下断**（不静默变绿）");
                    if (l2 == null) continue;
                    var tmpB = l2.GetComponentInChildren<TMPro.TextMeshPro>(true);
                    CheckTrue(tmpB != null, $"（前提·不静默）A1178 · `{nm}` 底下有 TMP（量框要用它）"
                                          + " —— 点阵后端量不出框 ⇒ 本颗这一节**没跑**，不是绿");
                    if (tmpB == null) continue;
                    var rz2 = ShellLabelRenderedPx(l2);
                    bool rzOk = rz2.x > 0.5f && rz2.x < 100000f && rz2.y > 0.5f && rz2.y < 100000f;
                    CheckTrue(rzOk, $"（前提·不静默）A1178 · `{nm}` 的**渲出尺寸量得到**"
                                  + "（哨兵 / 没重排 = 本颗这一节**没跑**，不是绿）"
                                  + $" —— 实得 {rz2.x:F1}×{rz2.y:F1}px（本闸上界 100000px）");
                    if (!rzOk) continue;
                    var bx2 = ShellLabelBoxPx(l2);
                    // 🔴 **2026-10-20（W58-UIFIX-A · 铁律 5 就地订正）**：A1178 这三条 2026-10-19 起是红的 ——
                    //   **（α）断言过期**，不是画面不对。根因链（判据 → `普查产出_1020/W58_诊断_界面五宿主.md` §一 S1–S3）：
                    //   ① P05（2026-10-19）把这两颗的文字换成**中文**（`Loc.T`：`Timer` =
                    //      `Event_Description/General/EndsInPlusOrangeTime`「结束于：{0}」·
                    //      `Vicotries title` = `Ranked/Victories`「胜利：」—— `Shell/EnergySinglePlayerOnlyEventWindow.cs:755-757 / :625`）；
                    //   ② `Label.ActualFace` 的**汉字闸**把实际生效面翻回 `Cjk`（`Battle/Label.cs:1010-1013`）；
                    //   ③ `NotoSerifCJK` 行盒 ≈ **1.52em**（四条独立读数 1.5170 / 1.5252 / 1.5470 / 1.5252，
                    //      见 `D1_九条红诊断.md` §证据 A），而原版框高 55.905 / 73.385 是按**原版行盒 ≤1.47em**
                    //      摆的 ⇒ TMP 的竖界先压字号：实得 **36.851 / 48.111**（`shell.log:33139 / 33215`）。
                    //   ⇒ 旧的期望（`38 ± 0.06`、「宽紧 + 高松」两条关系式）**只对拉丁面成立** ⇒ 全部换成
                    //      「**字号 ∈ [下限, 上限] + 不溢出框**」。上下限仍然只从**原版那一份**推：
                    //        · 上限 = 原版 `m_fontSize`（38 / 53.5，= 上面 `fitCase` 那条 dump 读数）；
                    //        · 下限 = **原版框高 ÷ 1.55em**（CJK 行盒实测上界 1.547em 向上取整）——
                    //          字号被任何一侧压到「连 CJK 行盒都装不下原版框高」的那一档 ⇒ 红；
                    //        · 不溢出 = 渲出宽/高各自 ≤ 框 + 1.5px（TMP 二分粒度）。
                    //   ⚠️ **系统性残差如实登记**（本报告）：`NotoSerifCJK` 行盒 1.52em vs 原版 ≤1.47em。
                    //      它该落 `项目任务.md` §三（本件白名单里没有正本；残差账 → `W2_A1126A1.md` §五）。
                    //   🔴 **2026-10-21（`A1262` · 铁律 5 就地订正）—— 上面那两句「原版回退字体行盒本地拿不到 /
                    //      没有判据」+「残差 1.52em vs 原版 ≤1.47em」【已过期】，⛔ 别照它推**：
                    //      · 原版那份 CJK 资产**本地就有**（`bundle_fonts_assets_all/MonoBehaviour/
                    //        NotoSerifCJK-Regular SDF.json`），它带 `m_FaceInfo.m_Scale = 0.9` ⇒
                    //        **有效行盒 1.2933 em**（face 1.437 × 0.9）。我们 2026-10-21 起已按它对齐
                    //        （资产 + `Editor/TmpSetup.cs` 的烘焙参数都改了，见上面 ③′ 那组断言）。
                    //      · ⛔ **但「改成 0.9 就会回到 38 / 53.5」不成立**，所以本处**故意不收紧**成 `38 ± 0.06`：
                    //        本工程的 px 口径 = **汉字墨高**（`Label.FontSizeToPx` = `fontSize × WorldGlyphPerFontSize × 108`，
                    //        而那个比值是**运行时实测**的 `TmpFont.MeasureGlyph('国')`）⇒ 在 `SetAutoFitBox` 里
                    //        `fontSizeMax = maxPx ÷ (Wglyph × 108)`，而一行字的世界高 = `fontSize × face 行盒 × m_Scale × 0.1`
                    //        ⇒ `Wglyph` 与行盒**同时**乘 `m_Scale`，**乘积与 `m_Scale` 无关**（渲染逐位不变）。
                    //        实测坐实：渲出比 **1.52 = 1.437(face 行盒) ÷ 0.948(汉字墨高/em)** —— 两个因子都含
                    //        `m_Scale`，约掉了；这也正是四条读数 1.5170 / 1.5252 / 1.5470 / 1.5252 的来历。
                    //      · ⇒ 真正的残差是**口径**（我们的「px」= 汉字墨高，而原版 `m_fontSize` 的墨高只有
                    //        `0.948 × 0.9 = 0.853 × px`），**不是缺 `m_Scale`**。要回到 38 / 53.5 得改口径或
                    //        逐站改 `minPx/maxPx` 实参（全仓 510+ 站、实渲缩 ≈15%）⇒ 属**另一笔**、须单独一批 + 真机验收。
                    //        **本轮只落资产字段（铁律 11「先完全复刻」）并把口径残差记账** ⇒ 本处上下限
                    //        **维持 2026-10-20 那一档**，⛔ 别为「让断言看着更紧」去改期望值（那会是假红/假绿）。
                    //      判据全文 → `资料/普查产出_1021/W61b_界面_A1262_mScale.md`。
                    float fontHi = nm == "Timer" ? 38f : 53.5f;   // 原版 `m_fontSize`（dump 现读）
                    float fontLo = boxHOrig / 1.55f;              // 原版框高 ÷ CJK 行盒上界（1.55em）
                    CheckTrue(l2.FontPxNow >= fontLo - 0.06f && l2.FontPxNow <= fontHi + 0.06f,
                              $"★ A1178 · `{nm}`：字号 ∈ **[{fontLo:F2}, {fontHi:F2}]px**（实得 `{l2.FontPxNow:F3}`）"
                            + $"｜下限 = 原版框高 {boxHOrig:F3} ÷ CJK 行盒上界 1.55em；上限 = 原版 `m_fontSize`"
                            + "｜🧨 框高/框宽改错到让它被压 / 字被任何一侧压小 ⇒ 本行红");
                    CheckTrue(rz2.x <= bx2.x + 1.5f && rz2.y <= bx2.y + 1.5f,
                              $"★ A1178 · `{nm}`：**不溢出框**（渲出 `{rz2.x:F2}×{rz2.y:F2}` vs 框 `{bx2.x:F2}×{bx2.y:F2}`，"
                            + "每轴 ≤ 框 + 1.5px = TMP 二分粒度）");
                    // ② `Timer`：宽度那一侧【不是】约束 —— 框宽比渲出宽有余量
                    //   （`Vicotries title` 那一档在中文下**宽度已不是紧的**（渲出 152.28 / 框 169.56），
                    //    它的收敛值现在由**框高**决定 ⇒ 由上面那两条兜住，⛔ 不再单独断「宽紧」）
                    if (nm == "Timer")
                        CheckTrue(bx2.x - rz2.x >= 1.5f,
                                  $"★ A1178 · `{nm}`：**宽度那一侧不是约束** —— 框宽 `{bx2.x:F2}` − 渲出宽 `{rz2.x:F2}`"
                                + $" = `{bx2.x - rz2.x:F2}`px ≥ 1.5px（有余量 ⇒ 收敛值不是被宽度顶住的；"
                                + "同一句也兜住「字溢出框」那一族 —— 上面 `fitCase` 那两条的框那两格是"
                                + "**故意给整屏**的、挡不住溢出）｜改坏法：把框改回旧常量 `289.72f` ⇒ 红");
                }
                // ---- A1179（2026-10-19）：本窗**另外四处**同一把尺子（⛔ 不抄第二份 `fitCase`）----
                // 期望值出处 = 上面 §③ 那条 `menu_dump.py` 命令（`EnergySinglePlayerOnlyEventWindow`）逐颗现读。
                // 前两处是**本件改的**（改坏法 = 把 `Shell/EnergySinglePlayerOnlyEventWindow.cs` 那两处调用末尾的
                //   autosize 实参删回默认 ⇒ 各红 4 条）；后两处**本件现核 = 早已接上**（`TextBox` 那条路：
                //   `SetWrapWidth(r.W)` 无条件 + 两个实参都过了 `fontPx > autoMinPx` 那道闸），
                //   这四条是**把它钉住**（改坏法 = 把那两串实参删回默认 ⇒ 各红 4 条）。
                fitCase("A1179 · `Shell/EnergySinglePlayerOnlyEventWindow.cs` 的 `Reward Tile`",
                        FindChildIn(a1126En.transform, "Reward Tile"),
                        594.81f, 54.69f, 18f, 45.87f, 36f, 1,
                        "原版 `Reward Tile` 实读 `auto[18~45.87]`（`m_fontSize 45.87` · 基准 36 · 折行 1 · 框 594.81×54.68）");
                fitCase("A1179 · `Shell/EnergySinglePlayerOnlyEventWindow.cs` 的 `Total Victories`",
                        FindChildIn(a1126En.transform, "Total Victories"),
                        213.99f, 73.38f, 18f, 77f, 36f, 1,
                        "原版 `…/PLayer Victories/Total Victories` 实读 `auto[18~77]`（框宽 **0 + CSF** ⇒ 这个 213.99 是我们摆的）");
                fitCase("A1179 · `Shell/EnergySinglePlayerOnlyEventWindow.cs` 的 `Scoring Instructions`",
                        FindChildIn(a1126En.transform, "Scoring Instructions"),
                        547.26f, 155.82f, 18f, 50f, 36f, 1,
                        "原版 `Scoring Instructions` 实读 `auto[18~50]` · 基准 36 · 折行 1（框 547.25×155.83）；"
                      + "本处**本件现核 = 原来就接着**（`MenuDraw.TextBox(…, 41.1f, 18f, QText, 50f, 36f)`）");
                fitCase("A1179 · `Shell/EnergySinglePlayerOnlyEventWindow.cs` 的 `Energy Instructions`",
                        FindChildIn(a1126En.transform, "Energy Instructions"),
                        547.26f, 168.13f, 18f, 72f, 50f, 1,
                        "原版 `Energy Instructions` 实读 `auto[18~72]` · 基准 **50**（⛔ 不是 36）· 折行 1（框 547.26×168.14）；"
                      + "本处**本件现核 = 原来就接着**（`MenuDraw.TextBox(…, 44.35f, 18f, QText, 72f, 50f)`）");
                // ---- A1190（2026-10-19）：本窗**再三处**（共 7 颗）同一把尺子（⛔ 不抄第二份 `fitCase`）----
                // 期望值出处 = `python -I d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all
                //   "EnergySinglePlayerOnlyEventWindow" --depth 10 --md` 逐颗现读。
                // 改坏法：把 `Shell/EnergySinglePlayerOnlyEventWindow.cs` 那三处调用末尾的 autosize
                //   实参删回默认（② 那一处是**一行建 5 颗** ⇒ 删一次红 5 条）⇒ 各红 4 条。
                // ① `Score`：原版 `…/Score Bar Line Level 1..5/Score` —— **5 颗逐值完全一致**
                //    （字号 48.0 · 基准 36.0 · `auto[18.0~48.0]` · 对齐 `Right/Midline` · 折行 1 · 框 133.32×57.39）
                //    ⇒ 一行建 5 颗、**一套实参就够**（现核过，⛔ 不是「5 颗不同值」那一族）。5 颗逐颗断。
                for (int k = 1; k <= 5; k++)
                {
                    var rowK = FindChildIn(a1126En.transform, "Score Bar Line Level " + k);
                    CheckTrue(rowK != null,
                              $"（前提·不静默）A1190 · 能源窗第 {k} 档 `Score Bar Line Level {k}` 建出来了"
                            + " —— ⛔ 找不到就**不往下断**（不静默变绿）");
                    // 🔴 **2026-10-10（A1197）**：下面三个字号期望值**已乘上该子树的祖先刻度积 `1.1478264`**
                    //    （`Score` ← `Skull`(`0.8695654273033142`) ← … ← `Scoring Bar Event Score Info`(`1.32`)）。
                    //    判据 = 原版 autosize 是在**本地单位**里二分的（`TextMeshProUGUI.cs:2342` 的 `m_marginWidth`
                    //    是**局部量**）⇒ **屏上字号 = 本地字号 × 祖先刻度积**；本地 18/48/36 ⇒ 屏值 20.6609/55.0957/41.3218。
                    //    ⚠️ **框那两格不动**（133.32×57.39 本来就是屏值）。改坏法见上面那段总注。
                    fitCase($"A1190 · `Shell/EnergySinglePlayerOnlyEventWindow.cs` 的 `Score`（`Level {k}`）",
                            FindChildIn(rowK, "Score"), 133.32f, 57.39f, 20.6609f, 55.0957f, 41.3218f, 1,
                            $"原版 `…/Score Bar Line Level {k}/Score` 实读 `auto[18~48]` × 祖先刻度 1.1478264"
                          + "（本地 `m_fontSize 48` · 基准 36 · 折行 1 · 框 133.32×57.39；5 颗逐值一致）");
                }
                // ② `Collect` 的 `Button Text` —— 🔴 本件**唯一**「原版折行 = 0」的一颗 ⇒ 期望 0，
                //    改坏法：删掉紧跟 `MenuDraw.Text` 的那句 `ct.SetWrapping(false)` ⇒ 折行档那一格红
                //    （`SetAutoFitBox` 内部的 `SetWrapWidth` 会**无条件**开成 `Normal`）。
                // 🔴 **2026-10-10（A1197）**：下面三个字号期望值**已乘上该子树的祖先刻度 `1.32`**
                //    （`Button Text` ← `Generic Simplified UI Button` ← `Scoring Bar Event Score Info`(`1.32`)）。
                //    本地 10/55/12 ⇒ 屏值 13.2/72.6/15.84。⚠️ **框那两格不动**（280.88×73.17 本来就是屏值）。
                fitCase("A1190 · `Shell/EnergySinglePlayerOnlyEventWindow.cs` 的 `Collect/Button Text`",
                        FindChildIn(FindChildIn(a1126En.transform, "Scoring Bar Event Score Info"),
                                    "Button Text"),
                        280.88f, 73.17f, 13.2f, 72.6f, 15.84f, 0,
                        "原版 `…/Generic Simplified UI Button/Button Text` 实读 `auto[10~55]` × 祖先刻度 1.32"
                      + " · 基准 **12**（⛔ 不是 36）· **折行 = 0**（框 280.88×73.17）");
                // ③ `Factions Title` —— ⚠️ 原版 `m_fontSize`(44.65) ≠ `m_fontSizeMax`(45.87) ⇒ 上限那一格
                //    专门咬「拿标称字号顶上限」那一档（A333 那条）。
                fitCase("A1190 · `Shell/EnergySinglePlayerOnlyEventWindow.cs` 的 `Factions Title`",
                        FindChildIn(a1126En.transform, "Factions Title"),
                        563.61f, 54.68f, 18f, 45.87f, 36f, 1,
                        "原版 `…/Army Selector Panel/Factions Title` 实读 `auto[18~45.87]`（`m_fontSize 44.65` · 基准 36 · 折行 1 · 框 563.61×54.68）");
            }
            shell.Windows.CloseAllWindows();

            // ---------------- ④ `Shell/CardDetailPopup.cs` 的 `Craft Explanation`（源码文本断言）----------------
            //
            // 🔴 **为什么这里是静态断言（并且仍然留着）**：本节的职责是「TMP 那 5 个实参逐值对账」，
            //  静态读数最直接、不重开一扇窗。
            //  🔴 **2026-10-18 就地订正（铁律 5）**：本段原来写的「两块面板按用户 2026-09-27 拍板整块不建
            //  / `BuildCrafting` **全库零调用点** / 那一处**没有生产路径**」**已过期** —— 2026-10-12 用户裁定
            //  「请你恢复」两块面板，`CardDetailPopup.Build()` 现在对**可合成的稀有度**会调 `BuildCrafting`
            //  ⇒ 那一处**活在路径上**。运行时是否真的生效，另由 `Editor/CollectionScene.cs` 的
            //  「卡片详情窗」那两条直接断节点与文案（`Crafting` 标题 · `Craft Label` = 创建副本）。
            //  ⚠️ **如实标注判别力**：这一条仍是**静态**的（读源码文本）—— 它只证明「那一行的实参写全了」，
            //  **证明不了运行时生效**（其余 5 条是运行时量 TMP 的 `textBounds`）⇒ ⛔ 别当强度相同的第 6 条。
            {
                const string cdPath = "Assets/CardPresentation/Shell/CardDetailPopup.cs";
                string cdSrc = File.Exists(cdPath) ? File.ReadAllText(cdPath) : "";
                CheckTrue(cdSrc.Length > 0,
                          $"（前提·不静默）A1126：读得到 `{cdPath}` —— 读不到 ⇒ 下面那条**没跑**，不是绿");
                // 期望值 = 原版 `Explanation`（`bundle_scenes_scenes_mainmenuwarpforge`，按 `m_text` 认人：
                //   `MonoBehaviour_1830.json`）实读：`m_enableAutoSizing 1` · min **10** · max **40** · base **36** · 折行 **1**。
                // 🔴 **2026-10-17（R1 现核）**：源码那一行在 `A1024③` 之后多了一个 `face:` 实参
                //   （当时是 `…, 36f, face: TmpFont.Face.LatinCardBody);`）—— 旧期望 `"36f);"` 已过期。
                // 🔴 **2026-10-18 用户口径（判据 → `资料/普查产出_1018/W14_详情窗按钮与副本文案.md` §二·3）**：
                //   这一句的文案改中文（`Loc.T("MenuDeck/Craft/CraftCost")` = 「这将消耗一个通配符」）⇒
                //   `face` 从 `LatinCardBody` 换成 **`Cjk`**（纯中文串必须走 CJK 面：字号按 `ActualFace`
                //   的 em 换算，拉丁面下中文的实绘尺寸与本仓其它中文不是一档）。
                //   四档 autosize 实参（`wrapPx` / min 10 / max 40 / base 36）**逐值不动**。
                //   ⛔ 别改回 `LatinCardBody`（那是英文串那一档）。
                const string wantArgs = "1726f - 1307.5f, 10f, 40f, 36f, face: TmpFont.Face.Cjk);";
                CheckTrue(cdSrc.Contains(wantArgs),
                          "★ A1126 · `Shell/CardDetailPopup.cs` 的 `Craft Explanation`：那一行**带上了 autosize 实参**"
                        + $"（`wrapPx` / min 10 / max 40 / base 36 + `face: Cjk` —— 原版 `Explanation` 实读 `auto[10.0~40.0]` · `m_fontSizeBase 36`；"
                        + "中文串走 CJK 面 = 用户 2026-10-18 口径 W14 §二·3）"
                        + $"｜改坏法：把那 4 个实参删回默认（`Text(…, 40f, QCdText);`）⇒ 本行红"
                        + "｜⚠️ **静态断言**：证明不了运行时生效（运行时那一侧另由 `Editor/CollectionScene.cs`"
                        + "「创建副本」那两条直接断节点与文案）");
            }

            // ---------------- ⑤ 🆕 2026-10-20（A1192 · `A1126`「`A2` 档」残余）：
            //   `Shell/CardDetailPopup.cs` 的 `Buy Original Card`（`Alternate Art Panel` 那颗价签）----------------
            //
            // 🔴 **为什么放在 `ShellScene.Run`**：`P4` §7·2 已经把话说全了 —— 它登记的那 4 处里
            //  `:776` 是**唯一活在路径上**的一处，而那扇窗**只有本宿主建**（`CardDetailPopup.Create`
            //  全库只在两处出现：本文件 `本文件` 的 A1001 建窗表，与 `Shell/CollectionWindow.cs` 里
            //  `OpenCardDetail` 的 `CardPresentation/Shell/CollectionWindow.cs`；后者是 `Shell/**`、不在本件白名单内）。
            // 🔴 **期望值全是原版字面量**（判据 = `P4` §2 #5 / `R2` §3 注① 的逐颗现读）：
            //  原版 `mainmenuwarpforge ▸ Alternate Art Panel/Buy Original Card Button/Generic UI Button/
            //  Price Display/text` = `m_enableAutoSizing 1` · min **12** · max **54** · base **39** · 折行 **0**；
            //  框宽 **328.71 × 73.38** = 原版那颗钮的实绘矩形（出处 `资料/阶段二_卡片详情窗_原版规格.md:78`
            //  「`Buy Original Card Button`(PriceDisplayButton) **328.71×73.38**」）——
            //  ⛔ **不读** `Shell/CardDetailPopup.cs` 的 `bw`/`bh` 常量、⛔ 不读它的 `MenuDraw.Text` 实参
            //  （那是被测实现 = 自证）。
            // ⚠️ **必须喂一张「有异画」的卡**：`BuildAltArt` 的末句是 `p.gameObject.SetActive(has)`
            //  （`has = HasAltArtStyle(Card.Id)`，判据 → `AlternateArtPanel__Initialize.c:22-38`）——
            //  拿一张**没有异画**的卡进这一节，整块是关的、TMP 不给它重排 ⇒ 那条守卫量到的是**哨兵**。
            //  取卡的写法**逐字照** `Editor/CollectionScene.cs` 那一节的先例（那份不在本件白名单内，
            //  只当样张；⛔ 没改它一个字）。
            // **改坏法**：把 `Shell/CardDetailPopup.cs` 那一行末尾的 4 个实参删回缺省（`wrapPx` 缺省 = 0
            //  ⇒ `MenuDraw.TextCore` 里 `if (wrapPx > 0f)` 整段不执行）⇒ 前四条红；删掉紧随的
            //  `SetWrapping(false)` ⇒ 折行档那条红；把 `bw`（或那一行的 wrapPx）调大到框外 ⇒
            //  「渲出来 ≤ 框 328.71×73.38」那条红。
            shell.Windows.CloseAllWindows();
            var cdW = CardDetailPopup.Create(shell.Windows);
            shell.Windows.OpenWindow(cdW);
            CheckTrue(cdW != null && cdW.CurrentState == WindowState.Open,
                      "（前提）卡片详情窗开起来了（`Buy Original Card` 挂在它的 `Alternate Art Panel` 下）");
            string altId = null;
            foreach (var a in CollectionWindow.AltArtCards)
                if (CollectionData.Card(a.CardId) != null) { altId = a.CardId; break; }
            CheckTrue(altId != null,
                      "（前提·不静默）异画表里至少有一张卡**在本地卡池里**"
                    + " —— 拿不到 ⇒ 下面那条成空转（⛔ 别把「没有卡可喂」当成绿）");
            if (cdW != null)
            {
                if (altId != null)
                {
                    cdW.ShowCard(CollectionData.Card(altId));
                    var altPanel = FindChildIn(cdW.transform, "Alternate Art Panel");
                    CheckTrue(altPanel != null && altPanel.gameObject.activeSelf,
                              $"（前提·不静默）异画面板**是开着的**（这张卡 `{altId}` 在异画表里"
                            + " ⇒ 原版 `SetActive(…, 1)`）—— 它是关的 ⇒ 下面那条守卫量到的是哨兵，"
                            + "**不是**「没溢出」");
                    fitCase("A1192 · `Shell/CardDetailPopup.cs` 的 `Buy Original Card`",
                            FindChildIn(altPanel, "Buy Original Card"),
                            328.71f, 73.38f, 12f, 54f, 39f, 0,
                            "原版 `Alternate Art Panel/Buy Original Card Button/…/Price Display/text` 实读 "
                          + "`auto[12~54]` · `m_fontSizeBase 39` · 折行 0 · 框 328.71×73.38");
                }
                cdW.Close();
            }
            shell.Windows.CloseAllWindows();
        }

        // ---------------- 🆕 2026-10-13（`A1132` 第 1 步）：卡组编辑那一扇窗（`DeckEditingWindow`）----------------
        //
        // 🔴 **本步 = 纯新增窗、生产路径零调用**（那条 `grep` 证据写在验收报告里；窗是 `Shell/DeckEditingWindow.cs`）。
        //    判据（原版真身）= `资料/普查产出_第二十四会话/D10_卡组编辑改成窗_施工件.md` **§一**：
        //      · 类 = `DeckEditingWindow : GameWindow`（**直接子类**）
        //      · `type = 0 (Fullscreen)` · `windowsPlacement = 10 (World)` —— **这两个值原版只有 prefab
        //        序列化字段有、代码里没赋** ⇒ 我们显式赋（哨兵会被 `AttachToAnchor` 出声 + 兜底）
        //      · 根 prefab 名 = `Deck Editing Menu`（248 节点那棵树**我们不重建**）
        //      · **ESC = 保存**（原版覆写整条 `ESCPressed` ⇒ 基类那两道门槛 + `Close()` 都不跑）
        //
        // ⚠️ **这一节只验「窗建得起来 / 开得起来 / 关得掉」**（第 1 步的「能停」中间态）。
        //    它**不验**入口、不验回程、不验遮让 —— 那三件是第 3/4/5 步（见 D10 §三那张表）。
        Section("A1132 第 1 步：卡组编辑窗（`DeckEditingWindow`）—— 建窗 / `Open()` / `Close()`（纯新增、零生产调用）");
        {
            shell.Windows.CloseAllWindows();      // 隔离：本窗是**全屏窗** ⇒ 开它会 `HideAllWindows()` 把别的窗藏掉

            // 🔴 **必须先换成临时存档**（与 `Editor/DeckScene.cs` 同一条纪律）：`Open()` → `DeckRuntime.Build`
            //    在**空库**时会 `Library.Create(...)` + `CommitCurrent(...)`（见那个 `Build()` 开头那段）
            //    ⇒ 不换路径就会**写玩家的真卡组**。判据 = `RuleEngine/Data/DeckStore.cs`：
            //    `Path = OverridePath ?? persistentDataPath/WarpforgeDecks.json`。
            string tmpDeck = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath, "../Temp/_shelldeckwin_selftest.json"));
            RuleEngine.DeckStore.OverridePath = tmpDeck;
            RuleEngine.DeckStore.DeleteFile();

            DeckEditingWindow deW = null;
            try
            {
                // ---- ① 建窗：两个必填参数**是不是原版那两个值** ----
                // 🔴 **灭自证的那一条**（本步特意设计）：`AttachToAnchor` 对哨兵 `type` 的**兜底值恰好也是**
                //    `Fullscreen`(0) ⇒ 只断 `type == 0` **挡不住**「忘了显式赋」（哨兵会被静默兜成同一个数、
                //    没有任何断言会红）。⇒ 另一条必须咬**出声本身**：「显式赋过的窗，建窗那一刻
                //    **一条点到它的 LogError 都没有**」—— 兜底支必然打一条 `… `placement` 没有显式赋值…`。
                //    （同族先例 = 本文件 A166 那条在 `Editor/SettingsScene.cs` 里的夹具，它断的是反面。）
                var buildErrs = new List<string>();
                Application.LogCallback hBuild = (msg, stack, type) =>
                { if (type == LogType.Error || type == LogType.Exception) buildErrs.Add(msg); };
                Application.logMessageReceived += hBuild;
                try { deW = DeckEditingWindow.Create(shell.Windows); }
                finally { Application.logMessageReceived -= hBuild; }

                CheckTrue(deW != null, "（前提·不静默）窗建出来了（`Create` 返回空 ⇒ 下面全是空转）");
                if (deW == null) { /* 上面那条已经红了；不往下走，免得一串 NRE 把「不静默」变成别的形状 */ }
                else
                {
                    Check(deW.name, DeckEditingWindow.NodeName,
                          "★ 根节点名照原版 prefab = `Deck Editing Menu`（`bundle_menus_assets_all`）");
                    Check((int)deW.type, 0,
                          "★ `type` = **0 (`Fullscreen`)**（原版 prefab 序列化字段实证；⛔ 不是模态那扇 `PopUpGameWindow`）");
                    Check((int)deW.placement, 10,
                          "★ `placement` = **10 (`World`)**（原版 prefab 序列化字段实证；兜底值 `Popup`(15) ⇒ 这条能咬住「忘赋」）");
                    CheckTrue(deW.HasType && deW.HasPlacement,
                              "…两个字段都是**显式赋过的**（`HasType`/`HasPlacement` 判的是「赋没赋过」，⛔ 不判值合不合法）");
                    string named = null;
                    for (int i = 0; i < buildErrs.Count; i++)
                        if (buildErrs[i].IndexOf(deW.name, System.StringComparison.Ordinal) >= 0) { named = buildErrs[i]; break; }
                    CheckTrue(named == null,
                              "★ **建窗那一刻没有一条 LogError 点到这扇窗** —— 哨兵兜底会出声"
                            + "（`[Win] `…` 的 `placement` 没有显式赋值…`）；这一条是「`type == 0` **不是**兜底出来的」"
                            + "的**唯一鉴别式**（兜底值恰好也是 `Fullscreen`(0) ⇒ 上面那条挡不住）；实得 "
                            + buildErrs.Count + " 条"
                            + (named != null ? "，其中点到本窗的是：" + named : ""));
                    var worldAnchor = WindowsManager.GetWindowAnchor(WindowsPlacement.World);
                    CheckTrue(worldAnchor != null && deW.transform.parent == worldAnchor,
                              "…而且 `AttachToAnchor` 真把它挂到了**锚点 10 (World)** 下"
                            + "（`placement` 兜错档 = 挂到弹窗锚点 ⇒ 这条红）");

                    // ---- ② `Open()`：内容由 `DeckRuntime` 建在**本窗窗根**下 ----
                    int nodesBefore = deW.transform.GetComponentsInChildren<Transform>(true).Length - 1;
                    Check(nodesBefore, 0,
                          "（前提）开窗之前窗根下**一个子件都没有** —— 原版那棵 248 节点的 prefab 树我们**不重建**"
                        + " ⇒ 下面的节点只可能来自 `Open()`（这条不成立的话，后面的「节点数 > 0」等于没验）");
                    shell.Windows.OpenWindow(deW);        // 走生产那条开窗路（`TryOpen` → 闭→开那一档 → `Open()`）
                    CheckTrue(deW.IsOpen(),
                              "★ 开窗后 `IsOpen()` 为真（= 原版 `GameWindow.IsOpen()`：`CurrentState == 1`）");
                    CheckTrue(deW.gameObject.activeSelf, "…物体是活的");
                    int nodesAfter = deW.transform.GetComponentsInChildren<Transform>(true).Length - 1;
                    CheckTrue(nodesAfter > 0,
                              $"★ `Open()` 之后**子树节点数 > 0**（实得 {nodesAfter}）—— 这是「`Open()` 真的建了内容」的判据"
                            + "（数的是**树里的节点**、含 inactive：原版全量 `Instantiate` 也不管它在不在视口里）");
                    CheckTrue(deW.Runtime != null && deW.Runtime.Root == deW.transform,
                              "…`DeckRuntime.Root` **就是本窗窗根**（那棵树我们不重建 ⇒ 中间少一层；"
                            + "`Root = transform` 是 `DeckRuntime.Build` 那一句，见 D10 §二）");
                    CheckTrue(deW.Runtime != null && deW.Runtime.State != null && deW.Runtime.State.PoolCount > 1000,
                              "…而且卡池**真建出来了**（张数 > 1000）—— 只数节点的话，「建了一堆空壳」也能过");
                    CheckTrue(deW.transform.Find("pool_0") != null,
                              "…卡池第 0 屏位 `pool_0` 在窗根下（`DeckRuntime.RefreshPool` 里名字 = `pool_` + 屏位号）");
                    // 🆕 **2026-10-19（第五十六会话 · `T23`）**：窗化档**不许**再建第二条顶栏 ——
                    //   ① 原版 `Deck Editing Menu` prefab 根只有一个孩子 `Content Area`、**树里没有顶栏**
                    //      （`资料/普查产出_第二十四会话/D10_prefab树.md` §结论 1）；
                    //   ② 本窗 `placement = World(10)` = `1 - Below Upper Bar Holder`
                    //      （`任务/T01_卡组编辑窗.md` §2·1）⇒ 原版的顶栏是**外壳那一条**
                    //      （`Shell/MainMenuRuntime.cs`，它是主菜单窗的兄弟、`HideAllWindows` 藏不到）。
                    //   我们再建一条（同 `TopBar.Build`、同 localPosition=0、同队列 2986–2997）= **双画**。
                    //   🧨 改坏法：删掉 `DeckRuntime.BuildTopBar` 开头那句 `if (HostWindow != null) return;`
                    //   ⇒ 本格红（`UiTopBar` 非 null）。
                    CheckTrue(deW.Runtime != null && deW.Runtime.UiTopBar == null,
                              "★ 窗化档 `DeckRuntime` **不建自己的顶栏**（外壳顶栏已在；原版 prefab 根没有顶栏）");

                    // ---- ③ `Close()`：两条并存（`state` 与激活态）----
                    // 🔴 **这两条是配对写的**（本步的第二个「灭自证」）：只断 `!IsOpen()`（读 `CurrentState`）
                    //    挡不住「物体被置灰了但 state 还写着 Open」，只断 `!activeSelf` 挡不住反过来的那一半
                    //    ⇒ **两条同时成立**才等价于「`GameWindow.Close()` 那两步都跑了」。
                    deW.Close();
                    CheckTrue(!deW.IsOpen(), "★ `Close()` 之后 `IsOpen()` 为假（`CurrentState` 那一步跑了）");
                    CheckTrue(!deW.gameObject.activeSelf, "★ …**而且物体不活**（`SetActive(false)` 那一步也跑了）");
                    CheckTrue(!shell.Windows.openWindows.Contains(deW),
                              "…而且从 `openWindows` 里摘掉了（= 原版 `CloseWindowCO` 的 `List.Remove`）");
                }
            }
            finally
            {
                shell.Windows.CloseAllWindows();
                if (deW != null && deW.gameObject != null) Object.DestroyImmediate(deW.gameObject);
                RuleEngine.DeckStore.DeleteFile();        // ⚠️ 必须在还原路径**之前**删（`Path` 读的是 `OverridePath`）
                RuleEngine.DeckStore.OverridePath = null; // 还原成玩家目录（本节唯一的全局副作用）
            }
        }

        // ==================================================================
        //  🆕 2026-10-17（`A1212` 块 10 / 12 / 13）：三扇窗里那 4 站的「实参 = 原版四格 + 折行 + 框」。
        //
        //  判据 = 原版 MB 逐颗现读（`bundle_menus_assets_all`；`menu_dump … --no-layout --no-ancestor-scale`）：
        //    块 10 #33 `Daily Streak Reward Popup Entry ▸ NormalReward ▸ Reward Name`
        //           = 18 / 40 / 36 / 折行 1；未缩放有效框 338.17×32.29
        //           （锚 0.0652709~0.934729 × 父 389.658 + sd −0.6235 · 高锚 0.110363~0.175637 × 503.556 − 0.576）
        //    块 10 #34 `… ▸ Collect ▸ Collect Text` = 18 / 72 / 36 / 折行 1；未缩放有效框 249.74×50.07
        //           （锚 0.033~0.939869 × 父 273.641 + sd 1.584）
        //    块 12 #30 `Reward Window ▸ Content ▸ Collect Button ▸ Button Text`
        //           = 10 / 40 / 12 / 折行 0；有效框 220.81×57.52
        //    块 13 #21 `Campaign Tab ▸ Premium Panel ▸ Generic Simplified UI Button ▸ Button Text`
        //           = 10 / 55 / 12 / 折行 0；有效框 238.06×46.79
        //  ⚠️ 连登窗那两站还有**三层祖先缩放**（条目根 1.2、首格再 ×1.2 ⇒ 1.44；`NormalReward` 0.8；
        //    `Collect` 0.7）—— 四格与框**都要乘进去**（原版 TMP 里存的是局部值；屏上是乘积）。
        //    `A1226` 表 B #33/#34 的 `−67.78 / −49.38` 是 2026-10-10 那次拿「未缩放帧」比「屏上帧」
        //    得到的两把尺混用；`A1280`（10-17）把缩放乘进实参之后，**两边现在是同一把尺**（下面直接钉死）。
        // ==================================================================
        Section("★ A1212 块 10/12/13：`DailyStreakPopup` / `RewardWindow` / `CampaignTab` 的四格＋折行＋框");
        {
            // ---------------- 块 10：`Daily Streak Reward Popup Entry`（两站 × 7 格）----------------
            // 🔴 夹具：把「可领那一格」的领取位**显式置成未领取**（`_streakClaimed` 是静态、可能吃上一段残留），
            //   结束后按起点还原（同 `RewardsScene` 的 A509 那节的做法；⛔ 不靠「出厂默认」）。
            shell.Windows.CloseAllWindows();
            int a12StreakDay = DailyData.StreakCollected;
            bool a12StreakClaimed0 = DailyData.StreakRewardClaimed(a12StreakDay);
            DailyData.ForceStreakClaimedForTest(a12StreakDay, false);
            var a12Ds = DailyStreakPopup.Create(shell.Windows);
            shell.Windows.OpenWindow(a12Ds);
            CheckTrue(a12Ds != null && a12Ds.CurrentState == WindowState.Open,
                      "（前提）每日连胜窗开起来了（奖格轨才建得出来）");
            if (a12Ds != null && a12Ds.CurrentState == WindowState.Open)
            {
                CheckTrue(!DailyData.StreakFailed(),
                          "（前提）本窗处于「连胜」态（`StreakFailed() == false`）—— 断签态不建奖格轨");
                int first = DailyData.StreakCollected;       // 首格 = `scaleMultiplierFirstElement` 作用的那一格
                // 🔴 **奖格轨只建【视口内】那几格**（`GameWindow.Text` → `MenuDraw.Visible`：整块在视口外 ⇒ 不建）。
                //   出厂 `StreakCollected = 5` ⇒ `FocusCurrentDay()` 恒不移动（`5 < 5` 假）⇒ 第 5/6 格在屏外。
                //   把轨滚到最右（`SetOffset(1000)` 会被夹到上限 ≈ 979.61）⇒ 第 4/5/6 格进视口；
                //   **只对「真的建出来了」的格断言**，并**显式要求首格（`first`）在里面**（⛔ 不静默跳过）。
                if (a12Ds.TrackScroll != null) a12Ds.TrackScroll.SetOffset(1000f);
                int a12Checked = 0;
                for (int i = 0; i < DailyData.StreakDays; i++)
                {
                    var ent = FindChildIn(a12Ds.transform, "Daily Streak Reward Popup Entry (" + i + ")");
                    if (ent == null) continue;
                    if (FindChildIn(ent, "Reward Name") == null) continue;   // 视口外的格：节点在、标签不建
                    a12Checked++;
                    // 条目根 1.2 × NormalReward 0.8 = 0.96；首格再 ×1.2 ⇒ 1.152。
                    float nr = 0.8f * (i == first ? 1.44f : 1.2f);
                    CheckA1212(ent, "Reward Name", "NormalReward ▸ Reward Name",
                               18f * nr, 40f * nr, 36f * nr, 1,
                               boxW: 338.17f * nr, boxH: 32.29f * nr);
                    // `Collect Text` 只在「已解锁且未领取」那一格建（本数据 = 第 `StreakCollected` 格）。
                    if (i == first)
                        CheckA1212(ent, "Collect Text", "Collect ▸ Collect Text",
                                   18f * 0.7f * 1.44f, 72f * 0.7f * 1.44f, 36f * 0.7f * 1.44f, 1,
                                   boxW: 249.74f * 0.7f * 1.44f, boxH: 50.07f * 0.7f * 1.44f);
                }
                CheckTrue(a12Checked >= 2,
                          $"★ 滚到最右之后**至少 2 格**进了视口并建出 `Reward Name`（实测 {a12Checked} 格）"
                        + " —— 少了就是「滚了但没重建」或视口裁切口径变了");
                var a12FirstEnt = FindChildIn(a12Ds.transform, "Daily Streak Reward Popup Entry (" + first + ")");
                CheckTrue(a12FirstEnt != null && FindChildIn(a12FirstEnt, "Collect Text") != null,
                          $"★ 首格（第 {first} 格）**在视口里**且 `Collect Text` 建出来了 —— 上面那两条 `Collect Text`"
                        + " 的四格/框断言吃这一条（否则它们等于没跑）");
            }
            shell.Windows.CloseAllWindows();
            if (a12Ds != null) Object.DestroyImmediate(a12Ds.gameObject);
            DailyData.ForceStreakClaimedForTest(a12StreakDay, a12StreakClaimed0);   // 还原起点（静态残留会污染后面的节）

            // ---------------- 块 12：`RewardWindow ▸ Collect Button ▸ Button Text` ----------------
            shell.Windows.CloseAllWindows();
            var a12Rw = RewardWindow.Create(shell.Windows);
            shell.Windows.OpenWindow(a12Rw);
            CheckTrue(a12Rw != null && a12Rw.CurrentState == WindowState.Open,
                      "（前提）奖励窗开起来了（`Collect Button` 那一颗才建得出来）");
            if (a12Rw != null && a12Rw.CurrentState == WindowState.Open)
            {
                var a12RwBtn = FindChildIn(a12Rw.transform, "Collect Button");
                CheckTrue(a12RwBtn != null, "（前提）`Collect Button` 在");
                if (a12RwBtn != null)
                    CheckA1212(a12RwBtn, "Button Text", "Content ▸ Collect Button ▸ Button Text",
                               10f, 40f, 12f, 0, boxW: 220.81f, boxH: 57.52f);
            }
            shell.Windows.CloseAllWindows();
            if (a12Rw != null) Object.DestroyImmediate(a12Rw.gameObject);

            // ---------------- 块 13：`CampaignTab ▸ Premium Panel ▸ … ▸ Button Text` ----------------
            shell.Windows.CloseAllWindows();
            var a12Rw2 = RewardsWindow.Create(shell.Windows);
            shell.Windows.OpenWindow(a12Rw2);
            var a12Ct = a12Rw2 != null ? FindChildIn(a12Rw2.transform, "Campaign Tab") : null;
            CheckTrue(a12Ct != null, "（前提）`Rewards Window ▸ Tabs ▸ Campaign Tab` 建出来了");
            if (a12Ct != null)
            {
                var a12Btn = FindChildIn(a12Ct, "Generic Simplified UI Button");
                CheckTrue(a12Btn != null, "（前提）`Premium Panel ▸ Generic Simplified UI Button` 在");
                if (a12Btn != null)
                    CheckA1212(a12Btn, "Button Text", "Premium Panel ▸ Generic Simplified UI Button ▸ Button Text",
                               10f, 55f, 12f, 0, boxW: 238.06f, boxH: 46.79f);
            }
            shell.Windows.CloseAllWindows();
            if (a12Rw2 != null) Object.DestroyImmediate(a12Rw2.gameObject);
        }

        // ---------------- 🆕 2026-10-17（界面线 T16）：离屏烘焙 + UI Blur ----------------
        //
        // 判据与出厂态 → **本文件 ①·b 那一节**（同一份判据，⛔ 不在这里抄第二份）。
        // 本节只管**运行时状态机**（`Toggle` / `CreateRT` / `ResolutionChanged`）：
        // 🔴 灭自证：`Showing` 每一态都与**相反态**成对（归零关 / Toggle 开 / Toggle 关 / 压背景开 / 回前台关）。
        Section("T16：`Toggle` / `CreateRT` / 分辨率变化（0.25 倍 · depth 24 · 两趟）");
        {
            var bake = shell.Bake;
            CheckTrue(bake != null && bake.image != null,
                      "（前提）模糊快照件在场（出厂态已在 ①·b 断过）");
            if (bake != null && bake.image != null)
            {
                // ⚠️ 前面的 `baseMenu` 探针（A1709 那一节）会点亮烘焙 ⇒ 这里先归零，下面每一态都从「关」开始。
                bake.Toggle(false);
                CheckTrue(!bake.Showing, "（归零）`Toggle(false)` 之后不显示");

                // ---- ① `Toggle(true)`：建 RT（ceil(屏 × 0.25) · depth 24）+ 显示 ----
                bake.Toggle(true);
                CheckTrue(bake.RT != null, "`Toggle(true)` 之后 RT 在位（没有就现建 —— 原版 `if (rt == null) CreateRT()`）");
                if (bake.RT != null)
                {
                    Check(bake.RT.width, Mathf.CeilToInt(Screen.width * 0.25f),
                          "RT 宽 = ceil(屏宽 × **0.25**（原版 `RENDER_TEXTURE_MULTIPLIER`）)");
                    Check(bake.RT.height, Mathf.CeilToInt(Screen.height * 0.25f),
                          "RT 高 = ceil(屏高 × 0.25)");
                    // 🔴 **2026-10-17（R1 现核）**：代码传的就是 24（`UIBakeScreen.cs`），
                    //   而 `RenderTexture.depth` 在 D24S8 平台上**回报实际分配 = 32**（24 深度 + 8 模板）
                    //   ⇒ 断「常量 = 24」+「实际分配 ≥ 24」，⛔ 别拿平台回报值当原版字段。
                    Check(UIBakeScreen.RTDepthBits, 24,
                          "RT 深度常量 = **24**（原版 `new RenderTexture(w, h, 0x18, 0)`）");
                    CheckTrue(bake.RT.depth >= 24,
                              $"…实际分配的 `RT.depth` = {bake.RT.depth}（D24S8 平台回报 32 = 24 深度 + 8 模板）");
                }
                CheckTrue(bake.Showing, "`Toggle(true)` 之后整屏快照**显示**（= 主菜单不在前台）");

                // ---- ② `Toggle(false)`：关 ----
                bake.Toggle(false);
                CheckTrue(!bake.Showing, "`Toggle(false)` 之后关掉（= 回前台）");

                // ---- ③ 分辨率变化（原版 `ResolutionChanged`）：不显示才重建 ----
                var rt0 = bake.RT;
                bake.ResolutionChanged();
                CheckTrue(bake.RT != null && !object.ReferenceEquals(bake.RT, rt0),
                          "`ResolutionChanged()` 在**不显示**时重建 RT（原版 `image.enabled == false ⇒ CreateRT`）");
                bake.Toggle(true);
                var rt1 = bake.RT;
                bake.ResolutionChanged();
                CheckTrue(object.ReferenceEquals(bake.RT, rt1),
                          "**显示中** `ResolutionChanged()` 不动 RT（原版那一支直接 return）");
                bake.Toggle(false);
            }
        }

        // ---------------- 🆕 2026-10-17（界面线 T16）：开关时机（`MainMenuWindow` 那四跳）----------------
        //
        // 原版逐句（`d:/2/tools/decomp_full/MainMenuWindow__*.c`）：
        //   `ToBackground()` = `if (state == Background) return;` → **`Toggle(true)`** → `base.ToBackground()` → `SetActive(false)`
        //   `ToFocus()`      = `SetActive(true)` → … → **`Toggle(false)`**
        //   `Hide()`         = **`Toggle(true)`** → `base.Hide()`
        //   `Close()`        = **`Toggle(true)`** → `base.Close()`
        // 这四跳在本仓落在 `GameWindow` 的共同基类里，闸门 = 「是不是 `WindowsManager.BaseMenu`」
        // （原版只有 `MainMenuWindow` 带 `screenBaker`）。
        Section("T16：开关时机（压背景 / 隐藏 / 关闭 ⇒ 先烘再藏；回前台 ⇒ 关）");
        {
            var bake = shell.Bake;
            CheckTrue(bake != null && bake.image != null, "（前提）模糊快照件在场（上一节已建）");
            if (bake != null && bake.image != null)
            {
                shell.Windows.CloseAllWindows();
                var prevBase = shell.Windows.BaseMenu;

                // 探针 = 一颗**真的**常驻主菜单（原版字段：type 0 / placement 0 / closeOnESC 0 —— 照 `MB1927` 实读）
                var mmGo = new GameObject("T16 MainMenu Probe");
                var mm = mmGo.AddComponent<MainMenuWindow>();
                mm.type = WindowType.Fullscreen;
                mm.placement = WindowsPlacement.None;
                mm.closeOnEsc = false;
                WindowsManager.AttachToAnchor(mm);
                shell.Windows.SetBaseMenu(mm);
                shell.Windows.OpenWindow(mm);
                CheckTrue(mm.CurrentState == WindowState.Open && mm.gameObject.activeSelf,
                          "（前提）探针常驻主菜单开起来了（= `WindowsManager.baseMenu`）");
                CheckTrue(!bake.Showing, "回前台 ⇒ 烘焙**关**（`TryOpen` 的 `Closed` 支 = 原版 `ToFocus`）");

                // ---- ① 弹窗把主菜单压到背景（`OpenWindow` 的弹窗支 → `ToBackground`）----
                shell.Windows.ShowPopUp("T16 探针弹窗", "确定");
                Check(mm.CurrentState, WindowState.Background,
                      "① 弹窗开 ⇒ 主菜单被压到 `Background`（原版 `ToBackground`）");
                CheckTrue(!mm.gameObject.activeSelf, "① …主菜单整棵藏掉（`MainMenuWindow.ToBackground` 末句）");
                CheckTrue(bake.Showing, "① …**先烘再藏**：模糊快照显示（= 上一帧主菜单的糊底）");
                CheckTrue(bake.BakeCount > 0, "① …而且**真的烘过一趟**（`BakeCount > 0`；只翻状态不渲染 ⇒ 这条红）");
                ClickPopUpButton(shell.Windows.popUpWindow, "Generic UI Button");
                CheckTrue(mm.gameObject.activeSelf && mm.CurrentState == WindowState.Open,
                          "① 关掉弹窗 ⇒ 主菜单回前台（`Background` 支）");
                CheckTrue(!bake.Showing, "① …回前台 ⇒ 烘关闭掉（原版 `ToFocus`: `SetActive(true)` → `Toggle(false)`）");

                // ---- ② 全屏窗走 `HideAllWindows()` → `Hide()` ----
                var fsGo = new GameObject("T16 Fullscreen Probe");
                var fs = fsGo.AddComponent<GameWindow>();
                fs.type = WindowType.Fullscreen;
                fs.placement = WindowsPlacement.Canvas;
                WindowsManager.AttachToAnchor(fs);
                shell.Windows.OpenWindow(fs);
                Check(fs.CurrentState, WindowState.Open, "② 全屏窗开着");
                CheckTrue(!mm.gameObject.activeSelf && mm.CurrentState == WindowState.Closed,
                          "② 主菜单被 `HideAllWindows()` 藏成 `Closed`（原版 `Hide()` 的第一句就是 `Toggle(true)`）");
                CheckTrue(bake.Showing, "② …隐藏也留一张模糊底");
                fs.Close();
                CheckTrue(mm.gameObject.activeSelf && mm.CurrentState == WindowState.Open,
                          "② 关掉全屏窗 ⇒ 主菜单被开回来（`Closed` 支重建内容）");
                CheckTrue(!bake.Showing, "② …回前台 ⇒ 烘关闭掉");

                // ---- ③ 闸门：**只有常驻主菜单**才烘（原版只有 `MainMenuWindow` 带 `screenBaker`）----
                fs.ToBackground();      // 它是普通窗、不是 `baseMenu`
                CheckTrue(!bake.Showing,
                          "③ 别的窗 `ToBackground()` **不碰**快照（闸门 = `WindowsManager.BaseMenu`）");

                // ---- ④ 主菜单自己 `Close()`（原版 `MainMenuWindow.Close` 也是 `Toggle(true)` → base）----
                mm.Close();
                CheckTrue(bake.Showing, "④ 主菜单自己 `Close()` ⇒ 留一张模糊底（原版 `MainMenuWindow.Close`）");
                CheckTrue(!mm.gameObject.activeSelf, "④ …而且自己藏掉了（`baseMenu` 支 = `Hide()`）");

                // 收尾：开回来（灭自证：同一条路能把快照关掉），然后把探针与 `baseMenu` 还原
                shell.Windows.OpenWindow(mm);
                CheckTrue(!bake.Showing, "（收尾）再开一次 ⇒ 快照关掉（上面 ④ 不是「关了就再也关不掉」）");
                shell.Windows.SetBaseMenu(prevBase);   // 还原（本节之前的 `baseMenu`，通常是 null）
                Object.DestroyImmediate(mmGo);
                Object.DestroyImmediate(fsGo);
            }
        }

            // ---------------- 🆕 2026-10-21（W59）：切非菜单场景 ⇒ 外壳常驻件必须关；回主菜单放回 ----------------
            //
            // 缺陷 / 根因 / 原版判据（逐条 dump 证据）→ `Shell/ShellRuntime.cs` 的「切场景 ⇒ 外壳常驻件显隐」那一节。
            // 🔴 **为什么落在 `ShellScene`**：壳就是这两件（模糊快照 / 左栏那三件）的**宿主**，本节要的夹具正是壳本身。
            // 🔴 **批处理验不了的那一格（如实写，⛔ 不是造假绿）**：`SceneManager.LoadScene` 在编辑模式下不允许
            //    ⇒ **跨场景那一跳**（`sceneLoaded` → `OnSceneLoadedApplyChrome`）批处理跑不到。本节断的是两件能断的：
            //      ① 那一跳**唯一调用的** `ShellRuntime.ApplyMenuChrome(false/true, …)` 真的把件关了 / 放回来了；
            //      ② 两只钩子的订阅挂上了（`SceneChromeHookInstalledForTest`）。
            //    **真机复核步骤**（`-wfscene Shell` 起 player）：等进主菜单 → 点模式卡 → 选卡组 → `Battle!` ⇒
            //      **进战场后整屏是 3D 战场**（不是主菜单那张糊图）、**左侧没有 玩/收藏/商店/奖励/社交 五钮**；
            //      投降 / 打完 / `LeaveBattle` 回主菜单 ⇒ 左栏与顶栏**都回来**、快照仍是关的。
            Section("W59：进非菜单场景 ⇒ 模糊快照 + 左栏 / 顶栏 / 聊天 全关；回主菜单放回");
            {
                var safeH = shell.SafeAreaOnlyHorizontal;
                CheckTrue(safeH != null, "（前提）壳的 `Safe area Only Horizontal` 在场");
                // 三件「名字 = 原版节点名」的**替身**：`ShellScene` 这一档**不跑** `MainMenuRuntime.Build`
                // ⇒ 左栏/顶栏/聊天本来不存在。这里只为验**显隐那一段**（⛔ 不验它们的版面 —— 版面归 `MainMenuScene`）。
                var probes = new List<GameObject>();
                foreach (var n in new[] { "Navigation Panel", "Upper bar", "ChatPreview" })
                {
                    var g = new GameObject(n);
                    g.transform.SetParent(safeH, false);
                    probes.Add(g);
                }
                var anchors = safeH.Find("Window Anchors");
                bool anchorsWereActive = anchors == null || anchors.gameObject.activeSelf;
                var fadeRoot = shell.transform.Find("FadeBackground");
                CheckTrue(fadeRoot != null, "（前提）压暗四条 `FadeBackground` 在场");

                shell.Bake.Toggle(true);          // 模拟「主菜单被压背景 / 隐藏 / 关闭」那一拍（原版：先烘再藏）
                CheckTrue(shell.BakeVisibleForTest, "（前提）模糊快照先**点亮**（= 主菜单不在前台那一刻）");

                ShellRuntime.ApplyMenuChrome(false, "自检：模拟切到战斗场景");
                CheckTrue(!shell.BakeVisibleForTest,
                          "★ 进非菜单场景 ⇒ 模糊快照**关**（缺陷 A：不关 ⇒ 整屏盖着主菜单的糊图、3D 战场被挡死）"
                        + " —— 改坏法：删掉 `ApplyMenuChromeNow` 里那个 `Toggle(false)` ⇒ 这条红");
                CheckTrue(!shell.NavPanelVisibleForTest,
                          "★ …左竖导航 `Navigation Panel` **关**（缺陷 B：原版战斗 dump 里 0 处）");
                foreach (var g in probes)
                    CheckTrue(!g.activeSelf,
                              $"★ …`{g.name}` 也**关**（判据 → `runtime_ui_dump_Battle_Arena_1.tsv` 里 0 处）");
                CheckTrue(fadeRoot != null && !fadeRoot.gameObject.activeSelf,
                          "★ …压暗四条 `FadeBackground` 也**关**（原版战斗 dump 里 `Fade` 0 处；"
                        + "不关 ⇒ 快照一撤就露出左右各 205.8px 的纯黑渐变）");

                ShellRuntime.ApplyMenuChrome(true, "自检：模拟回主菜单");
                foreach (var g in probes)
                    CheckTrue(g.activeSelf,
                              $"★ 回主菜单 ⇒ `{g.name}` **放回** —— 挡「进战斗一次性关掉、永不恢复」那种写法");
                CheckTrue(!shell.BakeVisibleForTest,
                          "★ 回主菜单 ⇒ 快照**保持关**（原版 `ToFocus` 那一侧就是 `Toggle(false)`；"
                        + "这条挡住「恢复时顺手把糊图点亮」）");
                CheckTrue(fadeRoot == null || fadeRoot.gameObject.activeSelf,
                          "★ 回主菜单 ⇒ 压暗四条**放回**（原版开场 / 主菜单都有它）");

                // 灭自证 / 范围：不该跟着关的件必须原样（`Window Anchors` = 三个 Holder 的宿主，战斗里开弹窗全靠它）
                if (anchors != null)
                    CheckTrue(anchors.gameObject.activeSelf == anchorsWereActive,
                              "★ 范围：`Window Anchors`（窗口三锚点）**没被动** —— 战斗里开设置等弹窗还靠它");
                CheckTrue(ShellRuntime.SceneChromeHookInstalledForTest,
                          "（接线）切场景那两只钩子挂上了（`Build` 里 `EnsureSceneCleanupHook`）");

                foreach (var g in probes) Object.DestroyImmediate(g);
            }

            // ---------------- 🆕 2026-10-21（W59 · 顺手发现①）：回主菜单**不许累积**旧件 ----------------
            //
            // 根因：`MainMenuRuntime.Build` 开头那轮清场（`DestroySafe(_root.GetChild(i))`）**只清主菜单场景自己的孩子**，
            //   而 `Navigation Panel` / `Upper bar` / `ChatPreview` 建在**壳的** `Safe area Only Horizontal` 下
            //   （`MainMenuRuntime.cs:506/515/516`；壳 `DontDestroyOnLoad`）⇒ 不清就是「每回一次主菜单 +1 套」。
            //   旧件上的 `WindowButton` 闭包还指着**已销毁的** `MainMenuRuntime`（纯泄漏 + 悬空回调）。
            // 🔴 这一节**走真的 `MainMenuRuntime.Build()` 两趟**（⛔ 不是拿帮手函数糊过去）：只有这样才守得住
            //   「**调用点**被删掉」那种改坏法 —— 只测 `ClearStaleMenuChrome` 本身的话，调用点删了它照样绿。
            // ⚠️ 这一档用的是**有壳**那一支（场上有一台 `ShellRuntime`）⇒ 与运行期那条路逐级一致。
            Section("W59（续）：连跑两次 `MainMenuRuntime.Build` ⇒ `safeH` 下每类外壳常驻件仍只有 1 个");
            {
                var safeH = shell.SafeAreaOnlyHorizontal;      // 上一节那个 `safeH` 只活在它自己那对花括号里
                CheckTrue(safeH != null, "（前提）壳的 `Safe area Only Horizontal` 在场");
                System.Func<string, int> countChrome = what =>
                {
                    int cnt = 0;
                    for (int i = 0; i < safeH.childCount; i++)
                        if (safeH.GetChild(i).name == what) cnt++;
                    return cnt;
                };
                var chrome = new[] { "Navigation Panel", "Upper bar", "ChatPreview" };
                var prevBase = shell.Windows.BaseMenu;
                var mmGo = new GameObject("W59 MainMenu Probe");
                var mmRt = mmGo.AddComponent<MainMenuRuntime>();
                try
                {
                    foreach (var n in chrome) Check(countChrome(n), 0, $"（前提）第 1 趟之前 `safeH` 下没有 `{n}`");

                    mmRt.Build();     // 第 1 趟：0 → 1
                    foreach (var n in chrome)
                        Check(countChrome(n), 1, $"★ 第 1 趟 `MainMenuRuntime.Build` ⇒ `safeH` 下 `{n}` = 1 个");

                    mmRt.Build();     // 第 2 趟：**不清场就会变成 2 个**
                    foreach (var n in chrome)
                        Check(countChrome(n), 1,
                              $"★ 第 2 趟 `MainMenuRuntime.Build` 之后 `safeH` 下的 `{n}` —— **仍只有 1 个**"
                            + "（🧨 删掉 `MainMenuRuntime.Build` 开头那句 `ShellRuntime.ClearStaleMenuChrome` ⇒ 变 2 ⇒ 这条红）");
                }
                finally
                {
                    // 收尾：探针树 / `baseMenu` / 安全区 / 快照 / 本节建出来的三件 + 探针那颗 toast —— 全还原
                    shell.Windows.SetBaseMenu(prevBase);       // 清表 + 还原（同上面 T16 那一节的收尾口径）
                    Object.DestroyImmediate(mmGo);
                    shell.ReclaimSafeZones();                  // `Safe area All` 刚被搬进探针树（可能已被一起销毁）⇒ 收回/补建
                    shell.Bake.Toggle(false);                  // 探针窗口若经 `Close()`/`Hide()` 烘过一张 ⇒ 关掉
                    ShellRuntime.ClearStaleMenuChrome(safeH);  // 清掉这一节自己建出来的三件（不出声 = 自检收尾，不是生产路径）
                    ToastNotifications.DisposeForSceneChange();// 探针 `Build` 会在 `safeH` 下建一颗 toast 控制器
                }
            }

            // ---------------- 🆕 U3（`A1322` 残留）：这一局被撤 ⇒ `SearchingOpponentWindow` 自己关 ----------------
            // 判据/口径 → `资料/普查产出_1019/L1_报告.md` §6·5 第 1 条；信号 **不改 `Net/**`** ——
            //   用现有公开状态 `NetMatchmaking.Waiting` / `HasOpponent`（`ClearMatch()` 两格一起清 ⇒ 两个都假 = 撤了）。
            // 自检没有联机会话夹具 ⇒ 用 `NoteMatchLiveForTest()` 把「开窗时真有一局」那一格推到真（生产路径由 `Open()` 置），
            //   再走**公开的** `NetMatchmaking.Reset()`（它做的正是 `ClearMatch()`：两格清空）模拟「这一局被撤」。
            Section("A1322 残留：这一局被撤 ⇒ `SearchingOpponentWindow` 自己关（U3）");
            {
                CardPresentation.Net.NetMatchmaking.Reset();          // 基线（清干净）
                var sWin = SearchingOpponentWindow.Create(shell.Windows, 0);
                sWin.NetWatch = true;
                sWin.Open();
                sWin.NoteMatchLiveForTest();
                CheckTrue(sWin.gameObject.activeSelf, "（前提）「找到对手」那扇窗是开着的");
                CardPresentation.Net.NetMatchmaking.Reset();          // = 这一局被撤（两格清空）
                sWin.Tick(0f);
                CheckTrue(!sWin.gameObject.activeSelf,
                          "★ 这一局被撤（`Waiting` / `HasOpponent` 都变假）⇒ 窗**自己关**"
                        + " —— 改坏法：删掉 `Tick` 里那一支 ⇒ 窗还开着（玩家得自己按 Esc/Cancel）⇒ 红");
                Object.DestroyImmediate(sWin.gameObject);

                // 灭自证的另一半：**从没有过对局**的窗不许被同一个信号关掉
                //   —— 挡的是「Tick 里无条件 Close()」那种写法（它会让上面那条假绿）。
                var sWin2 = SearchingOpponentWindow.Create(shell.Windows, 0);
                sWin2.NetWatch = true;
                sWin2.Open();
                CardPresentation.Net.NetMatchmaking.Reset();
                sWin2.Tick(0f);
                CheckTrue(sWin2.gameObject.activeSelf,
                          "★（灭自证）从没有过对局的窗 **不会** 被这个信号关掉 —— 无条件 `Close()` 的实现在这条上红");
                Object.DestroyImmediate(sWin2.gameObject);
                CardPresentation.Net.NetMatchmaking.Reset();          // 收尾：别把静态状态带到后面
            }

            // ---------------- 🆕 2026-10-23（W62 · 界面线）：ESC 关全屏匹配窗 ⇒ **取消搜索** ----------------
            //  **病灶**（真机 + 静态都对上）：本窗 `Close()` 原来只 `ReleaseHold()`（放掉「切场景前那一口气」）、
            //    **不取消搜索** ⇒ 按 ESC 关掉之后，匹配那一局还挂着（`Searching` / 常驻 ticker / 联机的账），
            //    而屏幕上**一个字都不剩**（玩家看不到任何提示）。
            //  🔴 **原版判据**（第一权威 = 反编译，**本类自己的** `Close` 覆写）：
            //    · `d:/2/tools/decomp_full/SearchingOpponentWindow__Close.c` 全文两句 =
            //      `GameWindow__Close(this)` + `MatchMakerManager__CancelSearch(manager, 0)` ⇒ **原版这扇窗关就取消**；
            //    · `SearchingOpponentWindow__CancelButtonClick.c` = 尾调 `Close()`（虚表 `0x1b8`）；
            //    · `SearchingOpponentWindow__OnDisable.c` **不取消**（只 `RemoveListener`）；
            //    · 旁证（弹窗那一支）：`SearchingMatchWindowDemo__ESCPressed.c:24` 末尾**无条件** `CancelSearch()`。
            //  ⇒ 三条一起断：① ESC 取消（**一次**）· ② **程序关窗不取消**（灭自证）· ③ `Cancel` 钮取消（**一次**）。
            //    ② 是这一组的「成对」，而且是**我们故意与原版不同的一处**（⛔ 别按原版把它改回去）：
            //    原版那个 `MatchMakerManager.CancelSearch` 是**有闸的轻动作**（只停协程），我们的 `OnCancel` =
            //    `RankedEventWindow.CancelSearch`（`AbortSearch` + 联机 `match.cancel` + 自己关窗）⇒ 无条件调会把
            //    「配到人 / 这一局被撤」两条**原版没有的**程序关窗路打成回归（往对面重发取消 / 弹「取消失败」）。
            Section("W62：ESC 关全屏匹配窗 ⇒ 取消搜索（原版 `SearchingOpponentWindow__Close.c` = 基类 Close + `MatchMakerManager.CancelSearch`）");
            {
                // ① ESC ⇒ 取消，且只一次
                var eWin = SearchingOpponentWindow.Create(shell.Windows, 0);
                int nEsc = 0;
                eWin.OnCancel = () => nEsc++;
                eWin.Open();
                CheckTrue(eWin.gameObject.activeSelf, "（前提）全屏匹配窗开出来了");
                bool escClosed = eWin.ESCPressed();
                CheckTrue(escClosed && !eWin.gameObject.activeSelf, "★ ESC ⇒ 窗关上");
                Check(nEsc, 1,
                      "★ **ESC 关窗 ⇒ 取消搜索**（原版 `SearchingMatchWindowDemo__ESCPressed.c:24` 末尾**无条件** `CancelSearch()`）"
                    + "｜🧨 改坏法：删掉 `SearchingOpponentWindow.ESCPressed` 覆写（退回「`Close()` 只放那一口气」）⇒ 0 次 ⇒ 红");
                // 幂等：原版那条 `CancelSearch` **自己还会再 `Close()` 一次**（`…__CancelSearch.c` 末尾）；我们这边是
                //   `OnCancel → RankedEventWindow.CancelSearch → _searchWin.Close()` 也会回来再关一次
                //   ⇒ 不幂等就是**无限递归**。这里再按一次 ESC，取消次数**不许涨**。
                eWin.ESCPressed();
                Check(nEsc, 1,
                      "★（幂等）再按一次 ESC ⇒ **不重复取消**（`CancelSearchOnce` 那道闸；生产上 `OnCancel` 那条链"
                    + "会反过来再关一次这扇窗 —— 不幂等就绕成递归）");
                Object.DestroyImmediate(eWin.gameObject);

                // ② 灭自证：**程序关窗不取消**（= `RankedEventWindow.OnSearchFinished`「配到人」与
                //   本窗 `Tick` 的 `A1322`「这一局被撤」那两条路）
                var pWin = SearchingOpponentWindow.Create(shell.Windows, 0);
                int nProg = 0;
                pWin.OnCancel = () => nProg++;
                pWin.Open();
                pWin.Close();
                Check(nProg, 0,
                      "★（灭自证）**程序关窗（配到人 / 这一局被撤）不取消** —— 把 `CancelSearch` **无条件**塞进 `Close()` 的"
                    + "实现在这条上红：那会让「刚配上的这一局」被自己撤掉、也会往对面重发一次取消"
                    + "（⚠️ 这是**我们故意与原版不同**的一处：原版 `SearchingOpponentWindow__Close.c` 确实无条件取消，"
                    + "但它那个 `MatchMakerManager.CancelSearch` 只是停协程，我们这条会发 `match.cancel`）");
                Object.DestroyImmediate(pWin.gameObject);

                // ③ `Cancel` 钮：原版 `CancelMatchMatchmaking → MatchMakerManager.CancelSearch` ⇒ 取消（且只一次）
                var cWin = SearchingOpponentWindow.Create(shell.Windows, 0);
                int nBtn = 0;
                cWin.OnCancel = () => nBtn++;
                cWin.Open();
                cWin.Cancel();
                Check(nBtn, 1, "★ `Cancel` 钮 ⇒ **取消一次**（`Cancel()` 与它触发的 `Close()` 不许各算一次）");
                CheckTrue(!cWin.gameObject.activeSelf, "…并把窗关掉（原版 `CancelMatchMatchmaking` 那条 `Close()`）");
                Object.DestroyImmediate(cWin.gameObject);
            }

            // ---------------- 🆕 2026-10-23（W62 · 界面线）：左栏 `Shadow` 的 tint / 顶边收进基类 ----------------
            //  **判据（原版 prefab 现读，2026-10-23 逐窗重读）**：
            //   · 收藏窗 `Collection Menu Variant`：`Tab Buttons/Shadow` = 167.2,158.6→214.8,1080 ·
            //     `40k_main_tab_shadow` · **Simple (0,0,0,0.475)**
            //     （`资料/普查产出_1022/W62_换牌压暗与收藏白条修复.md` §②）；
            //   · 社交窗同族同一行：`(0,0,0,0.475)`（`普查产出_0927/社交_联盟与好友页.md:45`，但它**高 0**）；
            //   · 🔴 **奖励窗 / 商店（本轮补读，原来只读到矩形）**：
            //     `python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Rewards Base Submenu Variant"
            //      --depth 5 --relative --no-sprite` ⇒ `Shadow … Simple (0,0,0,0.475)`；
            //     `"Shop Menu Variant"` 同一行**逐值相同**（连未激活的 `Shadow (1)` 也是这个色）。
            //     精确 float32 = `0.4745098054409027`（= 121/255），即 `(0,0,0,0.4745098)`。
            //  ⇒ 四扇窗**同一个色** ⇒ 判据收在基类一处（`BarShadowTint` / `BarShadowTop` 两个 virtual）。
            //  🔴 **2026-10-23（W62②）：基类默认值已并成原版那个色** —— 奖励 / 商店不覆写也拿到 `(0,0,0,0.4745)`；
            //     收藏窗的显式覆写与它等价（`0.475` vs `0.4745098`，差 0.0005 ⇒ 亚像素级，同一原版字段）。
            Section("W62：左栏 `Shadow` 的 tint / 顶边收进基类（一处判据 · 四扇窗同一个原版色）");
            {
                const System.Reflection.BindingFlags Flags =
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
                  | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly;

                // ① 结构：两把尺子在**基类**上、而且是 virtual
                var pTint = typeof(MainMenuSubmenuWindow).GetProperty("BarShadowTint", Flags);
                var pTop = typeof(MainMenuSubmenuWindow).GetProperty("BarShadowTop", Flags);
                var gTint = pTint != null ? pTint.GetGetMethod(true) : null;
                var gTop = pTop != null ? pTop.GetGetMethod(true) : null;
                CheckTrue(gTint != null && gTint.IsVirtual,
                          "★ 基类 `MainMenuSubmenuWindow` 上有 **virtual** 的 `BarShadowTint`（左栏阴影的色 —— 一处判据）");
                CheckTrue(gTop != null && gTop.IsVirtual,
                          "★ …同上的 `BarShadowTop`（原版 `Shadow` 的**顶边逐窗不同**：奖励/商店 70.94 · 收藏 158.64 —— 铁律 5·c）");
                CheckTrue(typeof(CollectionWindow).GetMethod("RebuildTabBarShadow", Flags) == null,
                          "★ 收藏窗那份**临时补丁** `RebuildTabBarShadow` 已删 —— 同一条 `Shadow` 只许有**一个生产者**"
                        + "｜🧨 改坏法：把「删掉基类那件、再按原版重建一份」加回来 ⇒ 两处生产者 ⇒ 红");

                if (gTint != null && gTop != null)
                {
                    // ② 值：**收藏窗**（显式覆写）＝ 原版字面量（⛔ 不从被测类里读常量）
                    var cw = CollectionWindow.Create(shell.Windows);
                    var cTint = (Color?)gTint.Invoke(cw, null);
                    float cTop = (float)gTop.Invoke(cw, null);
                    CheckTrue(cTint.HasValue
                              && Mathf.Abs(cTint.Value.r) < 2f / 255f && Mathf.Abs(cTint.Value.g) < 2f / 255f
                              && Mathf.Abs(cTint.Value.b) < 2f / 255f && Mathf.Abs(cTint.Value.a - 0.475f) < 2f / 255f,
                              "★ 收藏窗的 `Shadow` tint = 原版 **`(0,0,0,0.475)`**（`Collection Menu Variant` 的 `Image.m_Color`）"
                            + "｜🧨 不给 tint（= 白）⇒ 屏上就是那条浅白带 ⇒ 红");
                    CheckTrue(Mathf.Abs(cTop - 158.64f) < 0.01f,
                              $"★ 收藏窗的 `Shadow` 顶边 = 原版 **158.64**（本窗 `Tab Buttons` 自己的顶边；实测 {cTop:F2}）");
                    MainMenuSubmenuWindow.DestroySafe(cw.gameObject);

                    // ③ 🔴 **2026-10-23（W62②）：不覆写的两扇（奖励 / 商店）也吃到了原版色** ——
                    //    判据是**本轮现读的原版 `Image.m_Color`**（不是「按同族推断」）：
                    //    奖励 `Rewards Base Submenu Variant/Tab Buttons/Shadow` = `(0,0,0,0.475)` ·
                    //    商店 `Shop Menu Variant/Tab Buttons/Shadow` = 同值。
                    var rw = RewardsWindow.Create(shell.Windows);
                    var rTint = (Color?)gTint.Invoke(rw, null);
                    float rTop = (float)gTop.Invoke(rw, null);
                    CheckTrue(rTint.HasValue
                              && Mathf.Abs(rTint.Value.r) < 2f / 255f && Mathf.Abs(rTint.Value.g) < 2f / 255f
                              && Mathf.Abs(rTint.Value.b) < 2f / 255f && Mathf.Abs(rTint.Value.a - 0.4745098f) < 2f / 255f,
                              "★★ 奖励窗（**不覆写**）⇒ 基类默认 tint = 原版 **`(0,0,0,0.4745098)`**（= 121/255；"
                            + "本轮 `menu_dump.py` 现读奖励窗 `Tab Buttons/Shadow` 的 `m_Color`）"
                            + "｜🧨 把基类默认改回 `null` ⇒ 左栏又成一条白带 ⇒ 本条红（这正是 W62 真机报的那条白带）");
                    CheckTrue(Mathf.Abs(rTop - 70.94f) < 0.01f,
                              $"★ 奖励窗的 `Shadow` 顶边 = 原版 **70.94**（= `ContentT`，随 `Tab Buttons` 铺满 `Content Area`；实测 {rTop:F2}）");
                    MainMenuSubmenuWindow.DestroySafe(rw.gameObject);

                    // ③-b 商店：同一套壳、同一个色（只断色；矩形那一条在 `ShopScene` 里）
                    var sw = ShopWindow.Create(shell.Windows);
                    var sTint = (Color?)gTint.Invoke(sw, null);
                    CheckTrue(sTint.HasValue
                              && Mathf.Abs(sTint.Value.r) < 2f / 255f && Mathf.Abs(sTint.Value.g) < 2f / 255f
                              && Mathf.Abs(sTint.Value.b) < 2f / 255f && Mathf.Abs(sTint.Value.a - 0.4745098f) < 2f / 255f,
                              "★★ 商店窗（**不覆写**）⇒ 同一个基类判据 = 原版 **`(0,0,0,0.4745098)`**"
                            + "（`Shop Menu Variant/Tab Buttons/Shadow` 现读 `(0,0,0,0.475)`）"
                            + "｜🧨 只在收藏窗那一族给 tint（= 子类打补丁）⇒ 本条红");
                    MainMenuSubmenuWindow.DestroySafe(sw.gameObject);
                }
            }

            // ---------------- 🆕 2026-10-22：排位升段窗的三条 legacy 动画 clip ----------------
            // 判据链 → `工具/gen_division_clips.py` 文件头 + `Shell/RankedDivisionChangeWindow.cs` 的「动画」段。
            // 本段验四件事：① 数据表 ② 曲线条数守恒（原生 + 手动 = JSON）③ 两档播的**不是同一条**
            // ④ 播完停在 clip 末帧（不是 Build 时的出厂姿态）。
            Section("🆕 2026-10-22 排位升段窗：三条 legacy 动画 clip（`Division Config` / `Rank Up` / `Division Up`）");
            {
                CheckTrue(RankedDivisionChangeWindow.ClipsLoaded,
                          "★ `Resources/division_clips.json` 载进来了"
                        + (RankedDivisionChangeWindow.ClipsError != null ? "（错：" + RankedDivisionChangeWindow.ClipsError + "）" : ""));
                Check(RankedDivisionChangeWindow.ClipCount, 3, "表里 3 条 clip（= 原版 `m_Animations` 那三条）");

                var dCfg = RankedDivisionChangeWindow.FindClipData("divisionConfig");
                var dRank = RankedDivisionChangeWindow.FindClipData("rankUp");
                var dDiv = RankedDivisionChangeWindow.FindClipData("divisionUp");
                CheckTrue(dCfg != null && dRank != null && dDiv != null,
                          "三个键都在（`divisionConfig` / `rankUp` / `divisionUp`）—— 运行期只按 key 查，⛔ 不按名字");
                if (dCfg != null && dRank != null && dDiv != null)
                {
                    Check(dCfg.curves.Length, 14, "`Division Config` 曲线 14 条（3 缩放分量 + 11 float）");
                    Check(dRank.curves.Length, 16, "`Rank Up` 曲线 16 条（6 缩放分量 + 10 float）");
                    Check(dDiv.curves.Length, 14, "`Division Up` 曲线 14 条（3 缩放分量 + 11 float）");
                    CheckTrue(dCfg.legacy && dRank.legacy && dDiv.legacy,
                              "★ 三条都是 legacy（`m_Legacy=true` · `m_MuscleClipSize=0`）—— 不是的话根本重建不出来");
                    CheckNear(dRank.length, 3.9166667f, 0.0005f,
                              "`Rank Up` 长度 = 它 `AnimationFinished` 事件的时刻");
                    CheckNear(dDiv.length, 3.8333333f, 0.0005f, "`Division Up` 长度 = 3.8333 s");
                    CheckNear(dCfg.length, 0.2166667f, 0.0005f, "`Division Config` 长度 = 0.2167 s");

                    // ② 建 clip：**原生 + 手动 = JSON 条数**（改坏法：生成器少导一条 / 这里漏接一族 ⇒ 红）
                    var clipRank = RankedDivisionChangeWindow.BuildClip(dRank);
                    int nNative = RankedDivisionChangeWindow.LastNativeCurveCount;
                    int nManual = RankedDivisionChangeWindow.LastManualCurveCount;
                    int nErr = RankedDivisionChangeWindow.LastCurveErrors;
                    CheckTrue(clipRank != null && clipRank.legacy, "`Rank Up` 能建出 legacy `AnimationClip`");
                    Check(nErr, 0, "`SetCurve` 一条都没抛");
                    Check(nNative + nManual, dRank.curves.Length,
                          "★ 曲线条数守恒：原生 " + nNative + " + 手动 " + nManual + " = JSON " + dRank.curves.Length
                        + "（改坏法：少接一族通道 / 生成器少导一条 ⇒ 红）");
                    Check(nNative, 8, "原生交给 `SetCurve` 的 = 8 条（6 缩放分量 + 2 `m_IsActive`）");
                    var clipCfg = RankedDivisionChangeWindow.BuildClip(dCfg);
                    Check(RankedDivisionChangeWindow.LastNativeCurveCount, 5,
                          "`Division Config` 原生 5 条（3 缩放分量 + 2 `m_IsActive`）");
                    CheckTrue(clipCfg != null, "`Division Config` 也能建出来");
                }

                // ③ 两档各开一扇窗，推过 promote 延迟，看播的是哪一条
                var winRank = RankedDivisionChangeWindow.Create(shell.Windows,
                                    new RankedDivisionChangeWindow.Context { IsDivisionChange = false });
                CheckTrue(winRank != null, "（前提）开出一扇升段窗");
                if (winRank != null)
                {
                    Check(winRank.PlayingClipKey, RankedDivisionChangeWindow.ClipKeyDivisionConfig,
                          "`Open()` 立刻播 `Division Config`（= 原版 `Open()` 里那句 `Play(\"Division Config\")`）");
                    CheckNear(winRank.PromoteDelayRemaining, RankedDivisionChangeWindow.PromoteDelay(RankedDivisionChangeWindow.StepR.Length), 0.02f,
                              "promote 延迟 = `GetTotalAnimationTime() − 0.25`（4 颗印玺 ⇒ "
                            + RankedDivisionChangeWindow.PromoteDelay(RankedDivisionChangeWindow.StepR.Length).ToString("F2") + " s）");
                    CheckTrue(winRank.NodeForTest("Particles Rank Up") != null,
                              "★ `Particles Rank Up` 节点在（原版 prefab 有、我们原来漏建；缺了那两条曲线会静默空转）");
                    CheckTrue(winRank.NodeForTest("Particles DivisionUp/Sparks Burn") != null,
                              "★ `Particles DivisionUp/Sparks Burn` 节点在（clip 的 `ShapeModule.m_Scale` 指向它）");

                    winRank.TickForTest(RankedDivisionChangeWindow.PromoteDelay(RankedDivisionChangeWindow.StepR.Length) + 0.001f);
                    Check(winRank.PlayingClipKey, RankedDivisionChangeWindow.ClipKeyRankUp,
                          "★ 升段（`IsDivisionChange=false`）⇒ 播 `Rank Up`");
                    CheckTrue(winRank.IsPlaying, "`Rank Up` 正在播（自己的计数器）");
                    CheckTrue(winRank.NativeAnimation != null, "窗上挂着 `Animation` 组件，三条 clip 都 `AddClip` 进去了");

                    // 🧨🧨 **中段峰值断言 —— 判别式：它与下面 ④ 的末帧 1.0「结构上不可能由同一个静态值同时满足」**
                    //   为什么必须补：④ 那两条（`Division Image` 1.0 / `Rank Icon` 1.0）**都只断末值**，而
                    //   `TickAnimation` 播完会调 `_anim.Stop()`（`Shell/RankedDivisionChangeWindow.cs:851`）——
                    //   若当前 Unity 版本让 `Animation.Stop()` 把 `localScale` 复位到**出厂 1.0**，那两条会**碰巧绿**：
                    //   「静态出厂值 1.0」正好同时满足它们，失去抓「播完永久大 9.02%」的能力。
                    //   **判别式**：峰值那一刻是 **2.043**、末帧是 **1.0** —— **同一个静态值不可能同时是这两个数**
                    //   ⇒ 只有「本条绿 **且** 末帧绿」才证明**动画真的在推进**、并且停在正确末帧（单靠末帧证明不了）。
                    //   期望值 **2.043 = 原版判据**：原版 clip 峰值 `2.2275424` ÷ 原版 prefab 静止 `1.0903292`
                    //   （判据 → `报告/B9_W53DIV_段位clip缩放修正.md` §二那张四键表；⛔ **不拿本 JSON 的峰值反算**，那是自证）。
                    //   ⛔ **时刻**按 `division_clips.json` 的键读（现读 t=2.5999999 s），**不写死**。
                    float peakT = -1f, peakV = 0f;
                    if (dRank != null && dRank.curves != null)
                        for (int ci = 0; ci < dRank.curves.Length; ci++)
                        {
                            var cu = dRank.curves[ci];
                            if (cu.path != "Division Image/Rank Icon" || cu.property != "localScale.x" || cu.keys == null) continue;
                            for (int ki = 0; ki < cu.keys.Length; ki++)
                                if (cu.keys[ki].v > peakV) { peakV = cu.keys[ki].v; peakT = cu.keys[ki].t; }
                        }
                    CheckTrue(peakT > 0f && peakV > 1.5f,
                              "（前提）从 `division_clips.json` 的 `Rank Up` → `Division Image/Rank Icon` → `localScale.x` 读到峰值键 "
                            + "t=" + peakT.ToString("F4") + " s · v=" + peakV.ToString("F4")
                            + "（⛔ 时刻不写死；读不到 / 峰值还是个静态 1.0 ⇒ 数据表本身就不对）");
                    // 数据侧再钉一次峰值键（期望值 = 原版 2.2275424 ÷ 1.0903292，⛔ 不是本 JSON 自己的值）：
                    //   它挡「有人直接手改 `Resources/division_clips.json`、没重跑生成器」这一档。
                    CheckNear(peakV, 2.043f, 0.005f,
                              "★ 数据侧：`division_clips.json` 的峰值键 = **2.043**（原版 2.2275424 ÷ 1.0903292；"
                            + "🧨 撤掉 `gen_division_clips.py` 的 ÷1.0903292 ⇒ 峰值回 2.2275 ⇒ 差 0.18 ⇒ 红）");
                    // 批处理没有帧循环 ⇒ 动画只能**手动推**（`TickForTest` 累加 `_animT`，不是「设到」）：推到峰值那一拍再读。
                    float dtToPeak = peakT - winRank.AnimationTime;
                    CheckTrue(dtToPeak > 0f && winRank.IsPlaying,
                              "（前提）峰值那一拍还在当前进度**之后**（现 AnimationTime=" + winRank.AnimationTime.ToString("F3")
                            + " s < 峰值 " + peakT.ToString("F3") + " s）—— 否则推不到峰值，下面那条就是空转");
                    winRank.TickForTest(Mathf.Max(0f, dtToPeak));
                    CheckNear(winRank.AnimationTime, peakT, 0.001f,
                              "（前提）手动推到峰值时刻了（推不到 ⇒ 下一条读数无意义）");
                    var rankIconPeakS = winRank.NodeScaleForTest("Division Image/Rank Icon");
                    CheckTrue(winRank.NodeForTest("Division Image/Rank Icon") != null && rankIconPeakS != Vector3.zero,
                              "（前提）峰值那一帧 `Division Image/Rank Icon` 找得到、`NodeScaleForTest` 给出真实缩放（不是「找不到」时退回的 (0,0,0)）");
                    // 容忍 0.02 ≈ 峰值 2.043 的 **1%**：上升段斜率 ≈ 0.47/s（1.0 → 2.043 跨 2.217 s）⇒ 相当于 ≈ 43 ms 的进度差，
                    //   远大于 float 累加误差（`_animT += dt` 的 ~1e-7 s），又小到能抓住「整段缩放没被驱动」。
                    CheckNear(rankIconPeakS.x, 2.043f, 0.02f,
                              "★★ 中段峰值（判别式）：`Rank Icon` 在峰值键 t=" + peakT.ToString("F3") + " s 放大到 **2.043**"
                            + "（= 原版静止 1.0 的 2.043 倍 · 现读 " + rankIconPeakS.x.ToString("F4") + "）"
                            + "｜🧨 删掉 `ApplyChannels` 的缩放段（`:863-874`）/ 让节点全程停在静态 1.0 ⇒ 本条红；"
                            + "与末帧 1.0 **结构上不可能同时**由一个静态值满足 ⇒ 两条一起绿才算动画真在播"
                            + "（⛔ 不是「`Stop()` 复位到 1.0 也绿」那种假绿）");

                    // ④ 播完停在 clip 末帧（⛔ 不是 Build 时的出厂值）
                    winRank.TickForTest(winRank.AnimationLength + 1f);
                    CheckTrue(!winRank.IsPlaying, "★ 播完 `IsPlaying` 变假（不会一直挂着）");
                    Check(winRank.LastFinishedClipKey, RankedDivisionChangeWindow.ClipKeyRankUp,
                          "播完记的是 `Rank Up`");
                    CheckNear(winRank.NodeScaleForTest("Division Image").x, 1.0f, 0.005f,
                              "★ 播完停在末帧：`Division Image` 缩放 = 1.0（起播那一档是 0.8）");
                    CheckNear(winRank.NodeAlphaForTest("Title"), 1.0f, 0.005f,
                              "★ 播完 `Title` alpha = 1（`Rank Up` 3.1833 s 那一档）");
                    CheckTrue(winRank.NodeActiveForTest("Particles Rank Up"),
                              "`Rank Up` 末帧 `Particles Rank Up` = 开着（`m_IsActive` 在 0.3833 s 拨到 1）");
                    CheckTrue(!winRank.NodeActiveForTest("Particles DivisionUp"),
                              "同一帧 `Particles DivisionUp` = 关着（另一条 `m_IsActive` 曲线）");

                    // 🧨 灭自证：`Rank Up` 播完 `Rank Icon` 必须回到**静止缩放 1.0**（旧数据末键 1.0903292 ⇒ 播完永久大 9.02%）。
                    //   原版该节点静止 `localScale = 1.0903292`（prefab tran `224743393880019028`），那 9.02% 已烘进 `RankIconR` 的
                    //   73.51 px 视觉框 ⇒ 本窗节点静止缩放 = 1；数据侧也按同一因子 ÷1.0903292（`工具/gen_division_clips.py`）。
                    var rankIconS = winRank.NodeScaleForTest("Division Image/Rank Icon");
                    CheckTrue(winRank.NodeForTest("Division Image/Rank Icon") != null && rankIconS != Vector3.zero,
                              "（前提）`Division Image/Rank Icon` 找得到、`NodeScaleForTest` 给出真实缩放（不是「找不到」时退回的 (0,0,0)）");
                    CheckNear(rankIconS.x, 1.0f, 0.005f,
                              "★ 播完停在末帧：`Rank Icon` 缩放 = 1.0（原版静止 1.0903292 已烘进 73.51 px 矩形 ⇒ 节点静止 1；"
                            + "🧨 撤掉 `gen_division_clips.py` 的 ÷1.0903292 ⇒ 末键回 1.0903292 ⇒ 差 0.09 ⇒ 红）");
                }

                var winDiv = RankedDivisionChangeWindow.Create(shell.Windows,
                                    new RankedDivisionChangeWindow.Context { IsDivisionChange = true });
                CheckTrue(winDiv != null, "（前提）开出一扇分段变化窗");
                if (winDiv != null)
                {
                    winDiv.TickForTest(RankedDivisionChangeWindow.PromoteDelay(RankedDivisionChangeWindow.StepR.Length) + 0.001f);
                    Check(winDiv.PlayingClipKey, RankedDivisionChangeWindow.ClipKeyDivisionUp,
                          "★ 分段变化（`IsDivisionChange=true`）⇒ 播 `Division Up`");
                    // 🔴 灭自证：**两档播的不是同一条**（「永远播第一条」的实现在这条上红）
                    CheckTrue(winRank != null && winDiv.PlayingClipKey != winRank.PlayingClipKey,
                              "★ 灭自证：升段 / 分段变化播的是**两条不同的 clip**");
                    winDiv.TickForTest(winDiv.AnimationLength + 1f);
                    CheckTrue(winDiv.NodeActiveForTest("Particles DivisionUp"),
                              "`Division Up` 末帧 `Particles DivisionUp` = 开着（与 `Rank Up` 那一档相反）");
                    CheckTrue(!winDiv.NodeActiveForTest("Particles Rank Up"),
                              "同一帧 `Particles Rank Up` = 关着");
                }

                if (winRank != null) Object.DestroyImmediate(winRank.gameObject);
                if (winDiv != null) Object.DestroyImmediate(winDiv.gameObject);
            }

        // ---------------- 🆕 2026-10-23（活动线 L01/L02/L03 收尾 + 界面线 T28 尾巴）：活动窗的【页签】与【入口】
        //
        // 判据（`d:/2/tools/decomp_full/`，方法体完整现读；全文 → `Shell/ParentEventWindow.cs` 文件头「页签与入口」）：
        //  · `BaseParentEventWindow__SetupTabs.c`：liveops 事件列表 → 逐事件 `HasComponent(ev,13 /*TabButton*/)`
        //    ⇒ `TabButtons.AddTabButton(label, sprite)` + 点击**开另一扇窗**；`HasComponent(ev,11 /*Tab*/)`
        //    ⇒ `CreateEventTab` = **本窗内的一个内容页**。一个事件都没有 ⇒ 基类 `SetupTabs(0)`（左栏空）。
        //  · `TabButtons__AddTabButton.c`：`set_name(label)`（**节点名 = 页签文字**）· `Image.sprite = 图标`。
        //  · `OpenEventButton__OpenWindow.c`：事件钮 → `WindowsManager.OpenWindow(GetComponentReference(ev,10), ev)`。
        //  · `bundle_menus_assets_all/AssetBundle/AssetBundle_1.json` 的 `m_Container`（键 = GUID）→ PathID →
        //    UnityPy 读名：Parent 事件 `MainWindow` = `Parent Event Window` · Scoring 事件 `Tab` = `Expansion Pass Tab`。
        // 🔴 **期望值一律独立**：时刻用**写死的 UTC 字面量**（⛔ 不读被测实现的 `NowUtc`）、页签名对**官方词条**、
        //    数目对**本机四扇活动窗**（不是对实现里的常量）；下面还带两条**灭自证**（见 ★★ 那两条）。
        Section("活动线：`SetupTabs` 事件钮（原版 `BaseParentEventWindow.SetupTabs` 等价物）+ 唯一入口 `OpenEventHub`");
        {
            var utcExpired = new System.DateTime(2026, 10, 23, 0, 0, 0, System.DateTimeKind.Utc);  // 今天：13 份 SO 全过期
            var utcOrks    = new System.DateTime(2025, 4, 15, 0, 0, 0, System.DateTimeKind.Utc);   // ExpansionOrks1 在期
            var utcSW      = new System.DateTime(2025, 10, 25, 0, 0, 0, System.DateTimeKind.Utc);  // SWRelease(Scoring) 在期

            // ---- ① 名单：除本窗以外的那几扇；顺序 = `EventWindowKind` 的值序 ----
            var tForParent = ParentEventWindow.BuildEventTabs(ParentEventWindow.EventWindowKind.Parent, utcExpired);
            Check(tForParent.Count, 3, "`BuildEventTabs(Parent)` = **3** 颗（本机 4 扇活动窗，去掉本窗自己）");
            CheckTrue(tForParent.Count == 3
                   && tForParent[0].Kind == ParentEventWindow.EventWindowKind.Gacha
                   && tForParent[1].Kind == ParentEventWindow.EventWindowKind.Raid
                   && tForParent[2].Kind == ParentEventWindow.EventWindowKind.ExpansionPass,
                      "顺序 = `EventWindowKind` 的值序（原版取 `GetAllEvents` 的返回序；本机按窗登记表，如实标）");
            bool allOpenable = true;
            foreach (var t in tForParent) if (t.Open == null) allOpenable = false;
            CheckTrue(allOpenable, "每一颗都有**去向**（原版 `b__2` = `WindowsManager.OpenWindow(...)`；null ⇒ 点了没反应）");

            // ---- ② 页签名（今天全过期 ⇒ 走官方词条兜底）----
            CheckTrue(tForParent.Count == 3
                   && tForParent[0].Label == Loc.T(GachaEventWindow.TermTitle)
                   && tForParent[1].Label == Loc.T(RaidEventWindow.TermTabTitle)
                   && tForParent[2].Label == Loc.T(ExpansionPassEventWindow.TermTabTitle),
                      "页签名逐个对上各自那扇窗的官方词条（`Event_Description/Gacha/TabTitle` · "
                    + "`Event_Description/General/Raid` · `Event_Description/ExpansionPass/TabTitle`）");
            CheckTrue(tForParent.Count == 3
                   && tForParent[0].Label != tForParent[1].Label
                   && tForParent[1].Label != tForParent[2].Label
                   && tForParent[0].Label != tForParent[2].Label,
                      "★★ 灭自证：三颗的文字**互不相同**（把三颗都写死成同一个字串的实现在这条上红）");
            CheckTrue(tForParent.Count == 3 && tForParent[0].Label != GachaEventWindow.TermTitle,
                      "页签名**不是键名本身**（词条查不到时 `Loc.T` 会回键 —— 那会印一串 `Event_Description/…` 到界面上）");
            CheckTrue(tForParent.Count == 3 && string.IsNullOrEmpty(tForParent[0].EventId)
                   && string.IsNullOrEmpty(tForParent[2].EventId),
                      "今天（2026-10-23）13 份 SO **全部过期** ⇒ 不带任何活动实例 id"
                    + "（用户边界：过期活动不做、⛔ 不拿过期事件顶空态）");

            // ---- ③ 在期事件那一支：页签名与 id 取活动实例的（label 4 ?? label 0）----
            var tForGacha = ParentEventWindow.BuildEventTabs(ParentEventWindow.EventWindowKind.Gacha, utcOrks);
            CheckTrue(tForGacha.Count == 3 && tForGacha[0].Kind == ParentEventWindow.EventWindowKind.Parent
                   && tForGacha[0].EventId == "ExpansionOrks1",
                      "在期时刻（2025-04-15 UTC）：Parent 那颗取到本地活动实例 `ExpansionOrks1`（`LiveOpsLocalCatalog`）");
            CheckTrue(tForGacha.Count == 3 && tForGacha[0].Label != ParentEventWindow.P1TitleText,
                      "在期时页签名取**活动实例的 label**（不是兜底那句出厂标题）⇒ 「有实例就走实例」这条路由是活的");
            var tForGacha2 = ParentEventWindow.BuildEventTabs(ParentEventWindow.EventWindowKind.Gacha, utcSW);
            CheckTrue(tForGacha2.Count == 3
                   && tForGacha2[0].EventId == "SWRelease"
                   && tForGacha2[2].EventId == "SWReleaseScoring",
                      "同一时刻 Parent / Scoring 两类各挑各的在期实例（`SWRelease` / `SWReleaseScoring`）");

            // ---- ④ 窗口上真的建出来了：内容页签 + 事件钮（两张表分家）----
            var pe = WindowsManager.OpenParentEvent();
            CheckTrue(pe != null && pe.CurrentState == WindowState.Open, "（前提）活动承载窗开得出来");
            if (pe != null)
            {
                Check(pe.TabButtonNodes.Count, 2,
                      "**内容页签**仍是 2 颗（`tabs` 两个 PPtr：`Select Team Tab` / `Event On Going Tab`）"
                    + " —— 事件钮不许混进这张表");
                Check(pe.EventTabNodes.Count, 3,
                      "★★ 灭自证：**事件钮 = 3 颗**（把 `Build()` 里那句 `SetupTabs(...)` 删掉 ⇒ 0 颗 ⇒ 这条红）");
                Check(pe.EventTabLabels.Count, pe.EventTabNodes.Count, "事件钮的页签文字与按钮一一对应");
                Check(pe.EventTabHighlights.Count, pe.EventTabNodes.Count, "每颗事件钮都带 `Highlight` 节点");

                int hitOk = 0, nameOk = 0, hiOff = 0;
                for (int i = 0; i < pe.EventTabNodes.Count && i < pe.EventTabLabels.Count; i++)
                {
                    var node = pe.EventTabNodes[i];
                    var lb = pe.EventTabLabels[i];
                    if (node != null && lb != null && !string.IsNullOrEmpty(lb.Text))
                    {
                        nameOk++;                                  // 文字非空
                        if (node.name == lb.Text) hitOk++;         // 原版 `set_name(label)`
                    }
                    if (i < pe.EventTabHighlights.Count && pe.EventTabHighlights[i] != null
                        && !pe.EventTabHighlights[i].gameObject.activeSelf) hiOff++;
                }
                Check(nameOk, pe.EventTabNodes.Count, "每颗事件钮的文字都**非空**（词条缺键 + 没兜底 ⇒ 空字页签）");
                Check(hitOk, pe.EventTabNodes.Count,
                      "节点名 = 页签文字（判据 = `TabButtons__AddTabButton.c` 那句 `set_name(label)`）");
                Check(hiOff, pe.EventTabNodes.Count,
                      "事件钮的 `Highlight` **恒关**（选中态只给内容页 —— 事件钮点下去是**换窗**不是换页；如实标）");

                // 事件钮的命中区：每颗一个 `WindowButton`，且「已选中也不吞点击」（原版 `allowClickWhenAlreadySelected = 0`）
                int wbOk = 0;
                foreach (var node in pe.EventTabNodes)
                {
                    var wb = node != null ? node.GetComponentInChildren<WindowButton>(true) : null;
                    if (wb != null && wb.onClick != null && !wb.allowClickWhenAlreadySelected) wbOk++;
                }
                Check(wbOk, pe.EventTabNodes.Count, "每颗事件钮都有可点的 `WindowButton`（`onClick` 非空 + 不吞重复点）");

                // 内容页签照旧能切页（两种页签是**两条不同的机制**，⛔ 别混成一条）
                pe.SwitchPage(1);
                Check(pe.CurrentPageIndex, 1, "（内容页签那一支）`SwitchPage(1)` ⇒ 当前页 = 1");
                CheckTrue(pe.OnGoingPage != null && pe.OnGoingPage.gameObject.activeSelf
                       && pe.ChooseSidePage != null && !pe.ChooseSidePage.gameObject.activeSelf,
                          "第 2 页开着、第 1 页关着（= 原版 `ChangeTab` 的 `SetActive` 语义）");
                pe.SwitchPage(0);
                Check(pe.CurrentPageIndex, 0, "切回第 1 页");

                // ---- ⑤ 点事件钮 = **开另一扇窗**（原版 `b__2` 那一跳）----
                var beforeGacha = GachaEventWindow.LastOpened;
                Transform gachaTab = null;
                for (int i = 0; i < pe.EventTabNodes.Count && i < pe.EventTabLabels.Count; i++)
                    if (pe.EventTabLabels[i] != null && pe.EventTabLabels[i].Text == Loc.T(GachaEventWindow.TermTitle))
                        gachaTab = pe.EventTabNodes[i];
                CheckTrue(gachaTab != null, "（前提）找得到 Gacha 那颗事件钮");
                if (gachaTab != null)
                {
                    var wb = gachaTab.GetComponentInChildren<WindowButton>(true);
                    CheckTrue(wb != null && wb.onClick != null, "（前提）那颗钮有回调");
                    if (wb != null) wb.Click();
                    var gw = GachaEventWindow.LastOpened;
                    CheckTrue(gw != null && gw != beforeGacha,
                              "★ 点 Gacha 事件钮 ⇒ **Gacha 抽卡窗真的开了**（`GachaEventWindow.LastOpened` 换了）");
                    CheckTrue(gw != null && gw.name == GachaEventWindow.RootName,
                              "开出来的那扇根名 = `" + GachaEventWindow.RootName + "`（prefab 根名，两处同源）");
                    CheckTrue(gw != null && shell.Windows.openWindows.Contains(gw),
                              "它进了 `WindowsManager.openWindows`（窗口管理器认账，不是只建了个物体）");
                }
            }

            // ---- ⑥ 唯一入口 `OpenEventHub`（= 原版 `OpenEventButton.OpenWindow` 那一跳）----
            CheckTrue(WindowsManager.OpenEventHub(ParentEventWindow.EventWindowKind.Raid) is RaidEventWindow,
                      "入口：`OpenEventHub(Raid)` ⇒ `RaidEventWindow`（原版 = 事件钮 → `OpenWindow(MainWindow)`）");
            CheckTrue(WindowsManager.OpenEventHub(ParentEventWindow.EventWindowKind.ExpansionPass)
                      is ExpansionPassEventWindow,
                      "入口：`OpenEventHub(ExpansionPass)` ⇒ `ExpansionPassEventWindow`");
            CheckTrue(WindowsManager.OpenEventHub(ParentEventWindow.EventWindowKind.Gacha) is GachaEventWindow,
                      "入口：`OpenEventHub(Gacha)` ⇒ `GachaEventWindow`");
            CheckTrue(WindowsManager.OpenEventHub() is ParentEventWindow,
                      "入口：无参 `OpenEventHub()` ⇒ `ParentEventWindow`（默认那扇）");

            // ---- ⑦ 三扇活动窗各自的页签（L01/L02/L03 的「页签」那一半）----
            var gw2 = GachaEventWindow.LastOpened;
            if (gw2 != null)
            {
                Check(gw2.TabButtonNodes.Count, 1, "Gacha：页签 **1** 颗（原版该窗只有 `The Vault` 这一页）");
                CheckTrue(gw2.TabLabels.Count == 1 && gw2.TabLabels[0] != null
                       && gw2.TabLabels[0].Text == Loc.T(GachaEventWindow.TermTitle),
                          "Gacha：页签名 = `Event_Description/Gacha/TabTitle`（官方词条）");
                CheckTrue(gw2.TabHighlights.Count == 1 && gw2.TabHighlights[0] != null
                       && gw2.TabHighlights[0].gameObject.activeSelf,
                          "Gacha：唯一那一页**恒选中**（`Highlight` 开着）");
            }
            var rd = WindowsManager.OpenRaidEvent();
            Check(rd.TabButtonNodes.Count, 1, "Raid：页签 **1** 颗");
            CheckTrue(rd.TabLabels.Count == 1 && rd.TabLabels[0] != null
                   && rd.TabLabels[0].Text == Loc.T(RaidEventWindow.TermTabTitle),
                      "Raid：页签名 = `Event_Description/General/Raid`（官方词条）");
            var ep = WindowsManager.OpenExpansionPassEvent();
            Check(ep.TabButtonNodes.Count, 2, "Expansion Pass：页签 **2** 颗（Pass / Missions）");
            Check(ep.ActivePage, 0, "Expansion Pass：出厂落在第 0 页（Pass）");
            var epWb = ep.TabButtonNodes.Count > 1 ? ep.TabButtonNodes[1].GetComponentInChildren<WindowButton>(true) : null;
            CheckTrue(epWb != null && epWb.onClick != null, "（前提）Missions 那颗页签有回调");
            if (epWb != null) epWb.Click();
            Check(ep.ActivePage, 1, "★ 点 Missions 页签 ⇒ 当前页 = 1（**这一支是换页**，与事件钮换窗不同）");
            CheckTrue(ep.MissionsPageNode != null && ep.MissionsPageNode.gameObject.activeSelf
                   && ep.PageNode != null && !ep.PageNode.gameObject.activeSelf,
                      "★★ 灭自证：Missions 页开着、Pass 页关着（`ShowPage` 什么都不做的实现在这条上红）");
            if (ep.TabButtonNodes.Count > 0)
            {
                var epWb0 = ep.TabButtonNodes[0].GetComponentInChildren<WindowButton>(true);
                if (epWb0 != null) epWb0.Click();
                Check(ep.ActivePage, 0, "点回 Pass 页签 ⇒ 当前页 = 0");
                CheckTrue(ep.PageNode != null && ep.PageNode.gameObject.activeSelf
                       && ep.MissionsPageNode != null && !ep.MissionsPageNode.gameObject.activeSelf,
                          "Pass 页开着、Missions 页关着（两页互斥）");
            }

            // ---- ⑧ 🆕 2026-10-23：**三扇兄弟窗各自也有左栏事件钮**（原版 `BaseParentEventWindow.SetupTabs`
            //      是**基类**方法 ⇒ Gacha / Raid / Expansion Pass 任何一扇的左栏都列全部 live 事件）----
            // 判据：`BaseParentEventWindow__SetupTabs.c` 的 `HasComponent(ev,13 /*TabButton*/)` 那一支
            //   ＋ `TabButtons__AddTabButton.c`；四扇窗共用 `ParentEventWindow.SetupEventTabs` **一份实现**
            //   （⛔ 谁要是各抄一份，这里还看不出差别 —— 但「名单跳过自己」那两条会先红）。
            // ⛔ 名单与文字**写死在这**（不读 `BuildEventTabs` 的返回 —— 读了就是自证）。
            if (gw2 != null)
            {
                var wantGacha = new[] { ParentEventWindow.P1TitleText,
                                        Loc.T(RaidEventWindow.TermTabTitle),
                                        Loc.T(ExpansionPassEventWindow.TermTabTitle) };
                var wantRaid  = new[] { ParentEventWindow.P1TitleText,
                                        Loc.T(GachaEventWindow.TermTitle),
                                        Loc.T(ExpansionPassEventWindow.TermTabTitle) };
                var wantPass  = new[] { ParentEventWindow.P1TitleText,
                                        Loc.T(GachaEventWindow.TermTitle),
                                        Loc.T(RaidEventWindow.TermTabTitle) };

                CheckEventTabBar(gw2.EventTabNodes, gw2.EventTabLabels, gw2.TabButtonNodes, "Gacha 抽卡窗", wantGacha);
                CheckEventTabBar(rd.EventTabNodes,  rd.EventTabLabels,  rd.TabButtonNodes,  "Raid 团队窗", wantRaid);
                CheckEventTabBar(ep.EventTabNodes,  ep.EventTabLabels,  ep.TabButtonNodes,  "Expansion Pass 战令窗", wantPass);

                // 内容页那一张表**没被事件钮污染**（否则 `ShowPage` / `SwitchPage` 会按页号去点事件钮的灯）
                Check(gw2.TabButtonNodes.Count, 1, "Gacha：内容页签仍 **1** 颗（事件钮不许混进这张表）");
                Check(rd.TabButtonNodes.Count, 1, "Raid：内容页签仍 **1** 颗（同上）");
                Check(ep.TabButtonNodes.Count, 2, "Expansion Pass：内容页签仍 **2** 颗（Pass / Missions）");

                // ★★ 灭自证 A：三扇窗的名单**两两不同**（每扇都跳过自己那一种）。
                //   谁把三处都写成 `BuildEventTabs(Parent)`、或写死同一份名单 ⇒ 三张表一模一样 ⇒ 本条红。
                string jG = JoinTabLabels(gw2.EventTabLabels);
                string jR = JoinTabLabels(rd.EventTabLabels);
                string jP = JoinTabLabels(ep.EventTabLabels);
                CheckTrue(jG != jR && jR != jP && jG != jP,
                          "★★ 灭自证：三扇窗的事件名单**两两不同**（每扇都排除自己那一种）"
                        + "\n    Gacha=[" + jG + "]\n    Raid=[" + jR + "]\n    Pass=[" + jP + "]");

                // ★★ 灭自证 B：**点的是别的窗** —— 在 Gacha 窗里点它那颗 Raid 事件钮 ⇒ 最上面那扇变成 Raid 窗。
                //   把 `Open` 接成「开自己」/接错窗 ⇒ 本条红。
                //   ⚠️ **不拿「`LastOpened` 换没换」当判据**：`OpenByRef` 对「还在 `openWindows` 里」的那扇走
                //      **复用、不重建**（照原版 `automaticallyLoadedWindows`），而 `HideAllWindows` 藏起来的窗
                //      **本来就不摘表**（`GameWindow.Close` 首句 `if (!gameObject.activeSelf) return;` 也关不掉它）
                //      ⇒ `LastOpened` 可能**不换** —— 那是判据本身的形状、不是缺陷。**顶窗是谁**才是判据。
                Transform raidTabInGacha = null;
                for (int i = 0; i < gw2.EventTabNodes.Count && i < gw2.EventTabLabels.Count; i++)
                    if (gw2.EventTabLabels[i] != null
                        && gw2.EventTabLabels[i].Text == Loc.T(RaidEventWindow.TermTabTitle))
                        raidTabInGacha = gw2.EventTabNodes[i];
                CheckTrue(raidTabInGacha != null, "（前提）Gacha 窗左栏里找得到 Raid 那颗事件钮");
                if (raidTabInGacha != null)
                {
                    var wbRaid = raidTabInGacha.GetComponentInChildren<WindowButton>(true);
                    CheckTrue(wbRaid != null && wbRaid.onClick != null, "（前提）那颗钮有回调");
                    if (wbRaid != null) wbRaid.Click();
                    var wmTop = WindowsManager.Instance != null ? WindowsManager.Instance.TopWindow : null;
                    CheckTrue(wmTop is RaidEventWindow,
                              "★ 在 Gacha 窗里点 Raid 事件钮 ⇒ **Raid 窗真的开到了最上面**"
                            + "（实测顶窗 = " + (wmTop != null ? wmTop.name : "(null)") + "）");
                    CheckTrue(wmTop != null && wmTop != gw2,
                              "★★ 灭自证：开出来的是**别的窗**，不是它自己（事件钮接成开自己 ⇒ 本条红）");
                }
            }

            // 收尾：把本段开的四扇活动窗拆掉（本段是 RunCore 的最后一段，拆完只余「合计」那一行）
            //   ⚠️ **只在这一个循环里拆**（别先单独拆 ep/rd 再循环 —— 对已销毁的组件读 `.gameObject` 会抛
            //   `MissingReferenceException`）；`openWindows` 只有 `NotifyClosed` 会改，`DestroyImmediate` 不改它。
            for (int i = shell.Windows.openWindows.Count - 1; i >= 0; i--)
            {
                var w = shell.Windows.openWindows[i];
                if (w == null) continue;                       // Unity 假 null（已被拆掉 / 换了场景）
                if (w is GachaEventWindow || w is ParentEventWindow || w is RaidEventWindow
                    || w is ExpansionPassEventWindow)
                    Object.DestroyImmediate(w.gameObject);
            }
        }

        // ============================================================ ★ A1520（2026-10-25）：壳卡死 —— 六条状态不变量
        //
        // 判据（只读代理 F 现查闭合）→ `资料/普查产出_1025/F_判据_界面与台账订正.md` §任务1；
        // 可疑机制本体 → `资料/普查产出_1024/C3_界面卡牌活动判据总表.md:237-251`。
        //   链：`MainMenuRuntime.OpenHome` → `OpenWindow(BaseMenu, closeAll:true)` → `WindowsManager.CloseAllWindows`
        //   → `GameWindow.Close()` 首句守卫（`!activeSelf ⇒ return`，**一旦脏态永久**）→ `:776`（`BaseMenu == this` ⇒ `Hide(); return`）。
        //   冻结链：`PointerLayer.KeyCancel`（顶窗非 `Open` ⇒ ESC 直接 false）·
        //           `MainMenuRuntime` 的模态闸（`top.BlocksLeftNav` ⇒ 左栏命中区降到 `QNavHitBlocked`）。
        // 🔴 本段只断**不变量**（= 批处理能断的那半边）；「冻结态在真机上可不可达」是另一半 → 按 C3 的 H2/H3 真机跑。
        // ⚠️ 本段**自己建一棵真 `MainMenuRuntime`**：I3 要它那批**真命中区 quad**（`ShellScene` 这一档本来没有左栏），
        //    I5 要它按原版登记出来的 `BaseMenu`。收尾照 W59（续）那一节同一套（还原 `BaseMenu` · 拆探针树 ·
        //    收回安全区 · 关快照 · 清壳常驻件 · 拆探针建的那颗 toast）。
        Section("★ A1520：壳卡死六条状态不变量（I1 状态对偶 · I2 顶窗 · I3 导航闸 · I4 清场 · I5 OpenHome · I6 ESC 必关）");
        {
            var wm = shell.Windows;
            CheckTrue(wm != null && WindowsManager.Instance == wm,
                      "（前提·不静默）`WindowsManager` 在、且就是壳那一台（不在 ⇒ 下面整段空转 —— 这条红就是证据）");
            if (wm != null)
            {
                var prevBase = wm.BaseMenu;
                var probes = new List<GameObject>();
                System.Func<string, WindowType, WindowsPlacement, bool, GameWindow> mkProbe =
                    (name, ty, pl, esc) =>
                    {
                        var go = new GameObject(name);
                        probes.Add(go);
                        var w = go.AddComponent<GameWindow>();
                        w.type = ty; w.placement = pl; w.closeOnEsc = esc;
                        WindowsManager.AttachToAnchor(w);
                        return w;
                    };

                // ---- I1 的检测器（纯函数）：∀win，`activeSelf == (CurrentState != Closed)` ----
                // 🔴 **唯一一处例外 = 常驻主菜单**：原版 `MainMenuWindow.ToBackground` 的末句就是 `SetActive(false)`
                //   （判据 → `Shell/MainMenuWindow.cs` 文件头 ④）⇒ 它的对偶是 `activeSelf == (state == Open)`
                //   （`Background` 与 `Closed` 都不活）。⛔ 这不是放宽断言：两者各自逐句对上原版。
                //   ⚠️ 其余窗（含 `Background`）**物体是活的** —— 基类 `ToBackground` 只写状态、不关物体。
                System.Func<IList<GameWindow>, List<string>> dualityBad = list =>
                {
                    var bad = new List<string>();
                    foreach (var w in list)
                    {
                        if (w == null) continue;                    // Unity 假 null（已销毁 / 换了场景）
                        bool active = w.gameObject.activeSelf;
                        bool wantActive = (w == wm.BaseMenu)
                            ? (w.CurrentState == WindowState.Open)
                            : (w.CurrentState != WindowState.Closed);
                        if (active != wantActive)
                            bad.Add(w.name + "：activeSelf=" + active + " / state=" + w.CurrentState);
                    }
                    return bad;
                };

                // ---- 被检查的「宇宙」：册上每一扇 + `BaseMenu` + 本段每一颗探针 ----
                // （⛔ 不扫全场景：前面各段留下的 inactive 窗不归这一段负责，那是另一条口径。）
                System.Func<List<GameWindow>> universe = () =>
                {
                    var u = new List<GameWindow>();
                    foreach (var w in wm.openWindows) if (w != null && !u.Contains(w)) u.Add(w);
                    if (wm.BaseMenu != null && !u.Contains(wm.BaseMenu)) u.Add(wm.BaseMenu);
                    foreach (var g in probes)
                    {
                        if (g == null) continue;
                        var w = g.GetComponent<GameWindow>();
                        if (w != null && !u.Contains(w)) u.Add(w);
                    }
                    return u;
                };

                try
                {
                    // ---- 基线：空场（上一段刚拆完活动窗；这里再收一次，把假 null 摘干净）----
                    wm.CloseAllWindows();
                    Check(wm.openWindows.Count, 0, "（基线）空场：`openWindows` = 0");

                    // ---- 夹具：一棵**真** `MainMenuRuntime`（I3 的命中区 quad / I5 的 `BaseMenu` 都从它来）----
                    var mmGo = new GameObject("A1520 MainMenu Probe");
                    probes.Add(mmGo);
                    var mmRt = mmGo.AddComponent<MainMenuRuntime>();
                    mmRt.Build();
                    var baseWin = wm.BaseMenu;
                    CheckTrue(baseWin != null && baseWin.gameObject != null,
                              "（前提·不静默）`MainMenuRuntime.Build()` 把常驻主菜单登记上了（空 ⇒ I5 全空转）");
                    CheckTrue(baseWin != null && baseWin.gameObject.activeSelf && baseWin.IsOpen(),
                              "（前提）登记之后主菜单 = 活的 + `Open`（原版 `MainMenuWindow.Initialize` 末尾 `OpenWindow(this)`）");
                    CheckTrue(mmRt.NavHitQueueForTest != -1,
                              "（前提）左栏命中区建出来了（`-1` = 左栏没建 ⇒ I3 没有可观察对象，⛔ 不许当成通过）");

                    var fs = mkProbe("A1520 非模态探针", WindowType.Fullscreen, WindowsPlacement.World, true);
                    var pop = mkProbe("A1520 模态探针", WindowType.Popup, WindowsPlacement.Popup, true);

                    // ============================================================ I5 · `OpenHome()`
                    // 先把场面搞乱：全屏窗（`HideAllWindows` ⇒ 主菜单 `Closed`）+ 弹窗（全屏窗被压到 `Background`）
                    wm.OpenWindow(fs);
                    wm.OpenWindow(pop);
                    CheckTrue(wm.openWindows.Count >= 3, "（前提）I5 之前至少 3 扇在册（主菜单 + 全屏 + 弹窗）");
                    {
                        // 灭自证的前提：`OpenHome` 之前场上**真的不止一扇活着** —— 否则「恰好 1 扇」可能是
                        // 「本来就只剩一扇」蒙对的（两态对照：先 ≥2，之后 =1）。
                        int activeBefore = 0;
                        foreach (var w in universe()) if (w != null && w.gameObject.activeSelf) activeBefore++;
                        CheckTrue(activeBefore >= 2,
                                  "（前提）`OpenHome()` **之前**活着的窗 ≥ 2 扇（实测 " + activeBefore + "）"
                                + " —— 少了 ⇒ 下面「恰好 1 扇」是空转，抓不到「`closeAll` 没关别人」");
                    }
                    mmRt.OpenHome();        // = 左栏第 1 颗 PLAY/Home（原版 `closeOtherMenus = 1` 那一跳）
                    CheckTrue(baseWin != null && baseWin.gameObject.activeSelf && baseWin.IsOpen(),
                              "★ I5-1 `OpenHome()` 之后主菜单 = `activeSelf` + `Open`"
                            + "（`CloseAllWindows` 之后由 `TryOpen` 的 `Closed` 支重开；🧨 少关一扇 / 关完不开回来 ⇒ 红）");
                    Check(wm.TopWindow, baseWin, "★ I5-2 …而且它就是顶窗（`OpenWindow(BaseMenu, closeAll: true)`）");
                    {
                        var u5 = universe();
                        int activeOpen = 0;
                        foreach (var w in u5) if (w != null && w.gameObject.activeSelf && w.IsOpen()) activeOpen++;
                        Check(activeOpen, 1, "★ I5-3 **恰好 1 扇** `active + Open` 的窗"
                            + "（🧨 改坏法：`OpenHome` 的 `closeAll` 传 false / `CloseAllWindows` 漏关一扇 ⇒ ≥2 ⇒ 红）");
                        foreach (var w in u5)
                        {
                            if (w == null || w == baseWin) continue;
                            CheckTrue(!w.gameObject.activeSelf && w.CurrentState == WindowState.Closed,
                                      "★ I5-4 其余窗（`" + w.name + "`）全 `inactive + Closed`"
                                    + "（原版 `closeOtherMenus = 1` 的语义：回主菜单时别的窗一扇都不许留着）");
                        }
                        CheckTrue(u5.Count >= 3, "（前提）I5 的宇宙 ≥ 3 扇（少了 ⇒「其余全关」那几条是空转）");
                    }

                    // ============================================================ I1 · 状态对偶（两个方向）
                    {
                        var bad0 = dualityBad(universe());
                        CheckTrue(bad0.Count == 0,
                                  "★ I1-1 干净态：∀win `activeSelf == (state != Closed)`"
                                + "（常驻主菜单按它自己那一档 `== (state == Open)`；`Shell/MainMenuWindow.cs` 文件头 ④）"
                                + (bad0.Count > 0 ? " —— 违规：" + string.Join("、", bad0.ToArray()) : "")
                                + "｜🧨 改坏法：`Hide()` 少写 `state = Closed` / `ToBackground` 顺手关物体 ⇒ 红");
                        CheckTrue(universe().Count >= 3, "（前提）I1 的宇宙 ≥ 3 扇（少了 ⇒ 上面那条是空转）");

                        // 灭自证：绕过 `Close()`/`Hide()` 手工置灰（生产路径零个站）⇒ 检测器**必须**报它
                        var dirtyGo = new GameObject("A1520 脏态探针");
                        probes.Add(dirtyGo);
                        var dirty = dirtyGo.AddComponent<GameWindow>();
                        dirty.type = WindowType.Fullscreen;
                        dirty.placement = WindowsPlacement.Canvas;
                        dirty.closeOnEsc = true;
                        WindowsManager.AttachToAnchor(dirty);
                        wm.OpenWindow(dirty);
                        CheckTrue(dirty.gameObject.activeSelf && dirty.IsOpen(), "（前提）脏态探针先真的开着");
                        dirtyGo.SetActive(false);                    // ← 非法写法：物体灰了、状态还是 `Open`
                        var badDirty = dualityBad(new List<GameWindow> { dirty });
                        CheckTrue(badDirty.Count == 1 && badDirty[0].StartsWith(dirty.name),
                                  "★ I1-2 灭自证：手工 `SetActive(false)` 而状态仍 `Open` ⇒ 检测器**必须**只报这一扇"
                                + "（" + (badDirty.Count > 0 ? badDirty[0] : "<空>") + "）"
                                + "｜🧨 检测器改成「只看 state」或「只看 activeSelf」⇒ 本条红（那就抓不到「绕过两条路置灰」）");
                        Object.DestroyImmediate(dirtyGo);            // 脏窗只能拆（`Close()` 被首句守卫挡回）
                        wm.CloseAllWindows();                        // 顺带把假 null 从册上摘掉
                        Check(wm.openWindows.Count, 0, "（收尾）清场 ⇒ `openWindows` = 0");
                    }

                    // ============================================================ I2 · 顶窗
                    // `TopWindow == null || TopWindow.IsOpen() || TopWindow == BaseMenu`
                    System.Func<List<string>> topBad = () =>
                    {
                        var bad = new List<string>();
                        var top = wm.TopWindow;
                        if (top == null) return bad;         // 没有顶窗 = 合法（原版那一刻什么都不做）
                        if (top.IsOpen()) return bad;         // `Open` ⇒ ESC 打得通
                        if (top == wm.BaseMenu) return bad;   // 常驻主菜单那一支：`Close()` 只 `Hide()`、不摘 `currentWindow`
                        bad.Add(top.name + "：state=" + top.CurrentState + "（既非 `Open`、也不是 `BaseMenu`）");
                        return bad;
                    };
                    CheckTrue(wm.TopWindow == null, "（前提）I2 基线：没有顶窗（`CloseAllWindows` 收尾清了顶窗记账）");
                    CheckTrue(topBad().Count == 0, "★ I2-1 没有顶窗 ⇒ 合法");
                    wm.OpenWindow(baseWin);
                    Check(wm.TopWindow, baseWin, "（前提）顶窗 = 常驻主菜单");
                    CheckTrue(topBad().Count == 0, "★ I2-2 常驻主菜单在 `Open` 态 ⇒ 合法（ESC 那一跳走得通）");
                    wm.OpenWindow(pop);
                    Check(wm.TopWindow, pop, "（前提）弹窗成为顶窗");
                    CheckTrue(topBad().Count == 0, "★ I2-3 顶窗在 `Open` 态 ⇒ 合法");
                    // 原版 `GameWindow.Close` 的 baseMenu 支（`GameWindow.cs:776`）：只 `Hide()` 就 return ⇒ `currentWindow` **仍指着它**
                    pop.Close();
                    baseWin.Close();
                    Check(baseWin.CurrentState, WindowState.Closed, "（前提）常驻主菜单进了 `Closed`（baseMenu 支的 `Hide()`）");
                    Check(wm.TopWindow, baseWin, "（前提）`currentWindow` **仍指着它**（baseMenu 支不摘 `currentWindow` —— 原版 `CloseWindowCO` 逐句）");
                    CheckTrue(topBad().Count == 0,
                              "★ I2-4 顶窗 = `BaseMenu` 但**不在 `Open` 态** ⇒ **合法**（第三个别支）"
                            + "｜🧨 把 `|| top == BaseMenu` 删掉 ⇒ 本条红 —— 而那一支**不是缺陷**：原版 `CloseWindowCO` 的 baseMenu 支就只 `Hide()`");
                    wm.OpenWindow(baseWin);
                    // 灭自证：**普通**窗停在 `Background` 却仍是顶窗 ⇒ 检测器必须报（= 「ESC 直接 return false」的预演）
                    wm.OpenWindow(fs);
                    Check(wm.TopWindow, fs, "（前提）顶窗 = 全屏探针");
                    fs.ToBackground();                  // 造「顶窗不在 `Open` 态」的处境
                    var badTop = topBad();
                    CheckTrue(badTop.Count == 1 && badTop[0].StartsWith(fs.name),
                              "★ I2-5 灭自证：顶窗停在 `Background` 且不是 `BaseMenu` ⇒ 检测器**必须**报"
                            + "（" + (badTop.Count > 0 ? badTop[0] : "<空>") + "）"
                            + "｜🧨 删掉 `top.IsOpen()` 那半个判据 ⇒ 本条红；这一格正是「ESC 什么都不做」的预演");
                    fs.TryOpen();                       // `Background` 支 → 回 `Open`（A217②，不重建内容）
                    CheckTrue(topBad().Count == 0, "（收尾）`TryOpen()` 把它带回 `Open` ⇒ 检测器归零");
                    wm.CloseAllWindows();

                    // ============================================================ I3 · 导航闸对偶（两个方向都断）
                    // `NavHitQueueForTest == (top != null && top.BlocksLeftNav ? QNavHitBlocked : QNavHit)`
                    // 🔴 期望值从**两处独立来源**取：档号来自 `MainMenuRuntime.QNavHit*ForTest`，翻面依据来自
                    //    **顶窗自己的** `BlocksLeftNav`（原版 prefab 的 `type` / `windowsPlacement` 算的）——
                    //    ⛔ 不是拿同一个式子自证；实际读数 = 命中区 quad 的 `RenderQueue`（`NavHitQueueForTest`）。
                    System.Func<int> expectQ = () =>
                    {
                        var top = wm.TopWindow;
                        return (top != null && top.BlocksLeftNav)
                            ? MainMenuRuntime.QNavHitBlockedForTest
                            : MainMenuRuntime.QNavHitForTest;
                    };
                    CheckTrue(MainMenuRuntime.QNavHitBlockedForTest < MainMenuRuntime.QNavHitForTest,
                              "（前提）`QNavHitBlocked` 必须**严格低于** `QNavHit`（差 = "
                            + (MainMenuRuntime.QNavHitForTest - MainMenuRuntime.QNavHitBlockedForTest) + "）");
                    Check(mmRt.NavHitQueueForTest, MainMenuRuntime.QNavHitForTest,
                          "★ I3-1 **无窗** ⇒ 左栏命中区 = `QNavHit`（可点）");
                    wm.OpenWindow(baseWin);
                    CheckTrue(!baseWin.BlocksLeftNav, "（前提）常驻主菜单（`type=0` / `placement=0`）是**非模态**");
                    int qNonModal = mmRt.NavHitQueueForTest;
                    Check(qNonModal, expectQ(), "★ I3-2 非模态顶窗（`BaseMenu`）⇒ 仍是 `QNavHit`");
                    wm.OpenWindow(pop);
                    CheckTrue(pop.BlocksLeftNav, "（前提）弹窗探针（`type=1` / `placement=15`）是**模态**");
                    Check(mmRt.NavHitQueueForTest, expectQ(),
                          "★ I3-3 **模态顶窗** ⇒ 降到 `QNavHitBlocked`（左栏不可点）"
                        + "｜🧨 删掉 `RefreshNavHitQueue` 的模态判断 ⇒ 本条红");
                    CheckTrue(mmRt.NavHitQueueForTest != qNonModal,
                              "★ I3-3″ 灭自证：**同一个读口**在「非模态 → 模态」之间**真的翻了面**（"
                            + qNonModal + " → " + mmRt.NavHitQueueForTest + "）"
                            + "｜🧨 读口恒返回同一个数 / `RefreshNavHitQueue` 没被 `OpenWindow` 调到 ⇒ 本条红");
                    Check(mmRt.NavHitQueueForTest, MainMenuRuntime.QNavHitBlockedForTest,
                          "★ I3-3′ …而且确实等于 `QNavHitBlocked` 自己（与 I3-2 期望值**反向** ⇒ 两个方向都断住了）");
                    pop.Close();
                    Check(mmRt.NavHitQueueForTest, expectQ(), "★ I3-4 关掉模态窗 ⇒ 回到 `QNavHit`（两态对照）");
                    wm.OpenWindow(fs);
                    CheckTrue(!fs.BlocksLeftNav, "（前提）全屏探针（`type=0` / `placement=10`）非模态");
                    Check(mmRt.NavHitQueueForTest, expectQ(),
                          "★ I3-5 非模态窗开着 ⇒ 仍是 `QNavHit`（🧨 一刀切成「有窗就挡」⇒ 本条红；2026-10-18 那条修复不许回退）");
                    wm.CloseAllWindows();

                    // ============================================================ I4 · `CloseAllWindows()` 之后
                    wm.OpenWindow(baseWin);
                    wm.OpenWindow(fs);
                    wm.OpenWindow(pop);
                    CheckTrue(wm.openWindows.Count >= 3, "（前提）I4 之前至少 3 扇在册");
                    var regBefore = new List<GameWindow>(wm.openWindows);
                    wm.CloseAllWindows();
                    Check(wm.openWindows.Count, 0,
                          "★ I4-1 之后 `openWindows` **空**（原版收尾 `List.Clear()` 的等价净效果）");
                    {
                        var badActive = new List<string>();
                        var badState = new List<string>();
                        foreach (var w in regBefore)
                        {
                            if (w == null) continue;
                            if (w.gameObject.activeSelf) badActive.Add(w.name);
                            if (w.CurrentState != WindowState.Closed) badState.Add(w.name + "=" + w.CurrentState);
                        }
                        CheckTrue(badActive.Count == 0,
                                  "★ I4-2 收掉的每一扇都**不活**"
                                + (badActive.Count > 0 ? " —— 还活着的：" + string.Join("、", badActive.ToArray()) : "")
                                + "｜🧨 改坏法：`CloseAllWindows` 漏关一扇 ⇒ 红");
                        CheckTrue(badState.Count == 0,
                                  "★ I4-3 收掉的每一扇 `state` 都是 `Closed`（`Hide()` 与 `Close()` 两条路都写）"
                                + (badState.Count > 0 ? " —— 没写的：" + string.Join("、", badState.ToArray()) : ""));
                    }
                    // 灭自证：「活着却不在册」的孤儿（= `A1709` 那种）⇒ 检测器必须报
                    var orph = mkProbe("A1520 孤儿探针", WindowType.Fullscreen, WindowsPlacement.Canvas, true);
                    wm.OpenWindow(orph);
                    wm.openWindows.Remove(orph);        // ← 只摘册、不收窗
                    var obs = new List<string>();
                    foreach (var w in universe())
                        if (w != null && w.gameObject.activeSelf && !wm.openWindows.Contains(w)) obs.Add(w.name);
                    CheckTrue(obs.Count == 1 && obs[0] == orph.name,
                              "★ I4-4 灭自证：活窗被从册上摘掉 ⇒ **检出孤儿**"
                            + "（" + (obs.Count > 0 ? string.Join("、", obs.ToArray()) : "<空>") + "）"
                            + "｜🧨 把「活 + 不在册」那半个判据删掉 ⇒ 本条红");
                    orph.Close();
                    wm.CloseAllWindows();

                    // ============================================================ I6 · `KeyCancel()` 不许静默 false
                    var pl = PointerLayer.Instance;
                    CheckTrue(pl != null, "（前提）`PointerLayer` 在（不在 ⇒ ESC 这条路根本没人打）");
                    CheckTrue(PointerLayer.InputEnabled, "（前提）输入层启用（原版第一道门槛；停用 ⇒ ESC 必然 false，那是另一档）");
                    wm.OpenWindow(baseWin);             // 顶窗保持「不是 `BaseMenu`」，ESC 才走 `ESCPressed` 那条路
                    var escWin = mkProbe("A1520 ESC 探针", WindowType.Popup, WindowsPlacement.Popup, true);
                    wm.OpenWindow(escWin);
                    Check(wm.TopWindow, escWin, "（前提）ESC 探针是顶窗（≠ `BaseMenu`）");
                    CheckTrue(escWin.IsOpen() && escWin.closeOnEsc, "（前提）它在 `Open` 态、`closeOnEsc = true`");
                    bool escClosed = pl != null && pl.KeyCancel();
                    CheckTrue(escClosed,
                              "★ I6-1 `closeOnEsc` 的 `Open` 窗上 `KeyCancel()` **必须返回 true**"
                            + "｜🧨 `PointerLayer.KeyCancel` 改回恒 `false` / `ESCPressed` 里那句 `Close()` 删掉 ⇒ 红"
                            + "（⛔ 不许静默 false：「按了没反应」必须能被断言抓到）");
                    Check(escWin.CurrentState, WindowState.Closed, "★ I6-2 …它真的进了 `Closed`");
                    CheckTrue(!escWin.gameObject.activeSelf, "★ I6-3 …物体也不活了（两条一起 = 真的关了，不是只改状态）");
                    CheckTrue(!wm.openWindows.Contains(escWin), "★ I6-4 …而且从 `openWindows` 摘掉了（`NotifyClosed` 那一跳）");
                    // 阴性对照：`closeOnEsc = false` ⇒ **原版就什么都不做**（那不是缺陷）
                    var noEsc = mkProbe("A1520 ESC 阴性探针", WindowType.Popup, WindowsPlacement.Popup, false);
                    wm.OpenWindow(noEsc);
                    Check(wm.TopWindow, noEsc, "（前提）阴性探针是顶窗");
                    CheckTrue(pl == null || !pl.KeyCancel(),
                              "★ I6-5 阴性对照：`closeOnEsc = false` ⇒ `KeyCancel()` = false（原版第二道门槛）");
                    CheckTrue(noEsc.IsOpen(), "★ I6-6 …窗**还开着**（与 I6-1 合起来 = 两种状态分得开，不是「永远关不掉 / 永远关得掉」）");
                    noEsc.Close();
                    wm.CloseAllWindows();
                }
                finally
                {
                    // 收尾（照 W59（续）那一节的同一套）：`BaseMenu` 还原 · 探针树拆掉 · 安全区收回 ·
                    //   快照关掉 · 壳常驻件（左栏/顶栏/聊天）清掉 · 探针 `Build` 建的那颗 toast 控制器拆掉
                    wm.SetBaseMenu(prevBase);
                    for (int i = 0; i < probes.Count; i++)
                        if (probes[i] != null) Object.DestroyImmediate(probes[i]);
                    PointerLayer.SetInputEnabled(true);
                    shell.ReclaimSafeZones();
                    shell.Bake.Toggle(false);
                    ShellRuntime.ClearStaleMenuChrome(shell.SafeAreaOnlyHorizontal);
                    ToastNotifications.DisposeForSceneChange();
                }
            }
        }

        // ============================================================ 通用件 + 页签生命周期
        // 🆕 **2026-10-25（复刻第 3 轮 · 代理 H）**：原版三个「无通用件」+ `GameWindowWithTabs` 页签生命周期。
        //    判据 = 反编译方法体（`out_cpp2il_dev_ilspy/{UIImageMaterialColorChanger,UIGroupTint,UIScaleToFit,
        //    GameWindowWithTabs,WindowTabBase,TabButtons}.cs` + `decomp_full/UIScaleToFit__Fit.c`），
        //    每一条断言的「期望值」都写着它对着哪一句。
        //    🔴 本轮**只建通用件 + 断行为，不接 666/306/183 处实例**（用户明确分两轮）。
        Section("🆕 复刻第 3 轮：三个原版通用件 + 页签生命周期 —— 对着方法体断行为（未接实例）");
        var probeRoot = new GameObject("_replica_generic_probe");
        try
        {
            void Ck(Color got, Color want, string what)
            {
                CheckNear(got.r, want.r, 2e-4f, what + " · r");
                CheckNear(got.g, want.g, 2e-4f, what + " · g");
                CheckNear(got.b, want.b, 2e-4f, what + " · b");
                CheckNear(got.a, want.a, 2e-4f, what + " · a");
            }

            // ---------------- ① UIImageMaterialColorChanger（原版 666 处）
            // 判据：`UIImageMaterialColorChanger.cs:53-81`（DoColorChange）、`:31-51`（Update 顺序）、
            //       `:83-88`（SetColor）、`:21-24`（Awake 出厂不跑）、`:26-29`（销毁实例材质）。
            {
                var go = new GameObject("MatProbe", typeof(MeshRenderer));
                go.transform.SetParent(probeRoot.transform, false);
                var mr = go.GetComponent<MeshRenderer>();
                var mat0 = new Material(Shader.Find("Sprites/Default"));
                mat0.color = Color.white;
                mr.sharedMaterial = mat0;

                var ch = UIImageMaterialColorChanger.Attach(mr);
                CheckTrue(ch != null, "① `UIImageMaterialColorChanger` 建出来了");
                CheckTrue(!ch.Running && !ch.enabled, "① 出厂不跑（原版 `Awake`: `enabled = false`）");

                ch.DoColorChange(Color.red, "_Color", 1f, instant: true);
                Ck(mr.sharedMaterial.color, Color.red, "① `instant = true` ⇒ **直接写**目标色（原版 :76-80）");
                CheckTrue(ch.InstancedMaterial != null && !ReferenceEquals(mr.sharedMaterial, mat0),
                          "① 先 `new Material(image.material)` 再换上去（原版 :58-64；🧨 直接改原材质 ⇒ 红）");
                Ck(mat0.color, Color.white, "① **原材质一个字节没动**（这才是实例化的意义；🧨 没实例化 ⇒ 红）");

                // 补间：起始色 = 当前（红）→ 目标（蓝），1 秒
                ch.DoColorChange(Color.blue, "_Color", 1f, instant: false);
                CheckTrue(ch.Running, "① `instant = false` ⇒ 起补间（原版 :67-75 置 enabled = true）");
                Ck(ch.OriginalColor, Color.red, "① 起始色 = 起补间那一刻的材质色（原版 `GetColor`）");
                ch.Step(0.5f);
                Ck(mr.sharedMaterial.color, Color.red,
                   "① **`t` 先用、时间后加**：第一次 Step(0.5) 时 `currentTime` 还是 0 ⇒ 写起始色"
                 + "（原版 :35-50；🧨 先累加再算 t ⇒ 这里会是中间色 ⇒ 红）");
                ch.Step(0.5f);
                Ck(mr.sharedMaterial.color, Color.Lerp(Color.red, Color.blue, 0.5f),
                   "① 中间值 = `Lerp(起始, 目标, 0.5)`（🧨 写成固定终值 ⇒ 红）");
                ch.Step(0.001f);
                Ck(mr.sharedMaterial.color, Color.blue, "① 到点写终值（原版 t 夹到 1）");
                CheckTrue(!ch.Running && !ch.enabled, "① 到点**自己停**（原版 `Update`: `enabled = false`）");
                ch.SetColor(Color.green, "_Color");
                Ck(mr.sharedMaterial.color, Color.green, "① `SetColor` = 直接写（原版 :83-88）");

                // 同一 Renderer 上第二颗（不同属性/不同节奏）⇒ **共用同一份实例材质**（接实例前置；否则后建的会把先建的写入弄丢）
                var ch2 = go.AddComponent<UIImageMaterialColorChanger>();
                ch2.Bind(mr);
                ch2.DoColorChange(Color.yellow, "_Color", 1f, instant: true);
                CheckTrue(ReferenceEquals(ch.InstancedMaterial, ch2.InstancedMaterial),
                          "① 同一 Renderer 上两颗组件**共用一份实例材质**（🧨 各建一份 ⇒ 第一颗写入静默丢失 ⇒ 红）");
                CheckTrue(ReferenceEquals(mr.sharedMaterial, ch.InstancedMaterial),
                          "① `renderer.sharedMaterial` 就是那一份共享实例材质");
                Ck(mr.sharedMaterial.color, Color.yellow, "① 第二颗写入生效（共享材质上最后一次写赢）");
            }

            // ---------------- ② UIGroupTint（原版 306 处）
            // 判据：`UIGroupTint.cs:41-82`（Refresh 收整棵含未激活）、`:84-114`（DoColorChange）、
            //       `:116-141`（Update 阈值 9.9999994e-11 + 自停）、`:148-176`（SetColor 补间）、`:143-146`（Toggle）。
            {
                var grp = new GameObject("GroupProbe");
                grp.transform.SetParent(probeRoot.transform, false);
                var q1 = ImageQuad.Create(grp.transform, Texture2D.whiteTexture, new Vector3(-1f, 0f, 0f),
                                          1f, new Vector2(0.5f, 0.5f), "q1");
                var q2 = ImageQuad.Create(grp.transform, Texture2D.whiteTexture, new Vector3(1f, 0f, 0f),
                                          1f, new Vector2(0.5f, 0.5f), "q2");
                CheckTrue(q1 != null && q2 != null, "② （前提）两颗 `ImageQuad` 建出来了");
                var base1 = new Color(0.8f, 0.6f, 0.4f, 0.5f);
                var base2 = new Color(0.2f, 0.4f, 0.8f, 1f);
                q1.SetTint(base1);
                q2.SetTint(base2);
                q2.gameObject.SetActive(false);          // 原版 `GetComponentsInChildren(true)` ⇒ **未激活也收**

                var tint = grp.AddComponent<UIGroupTint>();
                // 🔴 **2026-10-25（用户纠正）**：原版 `Refresh()` 全库零调用点 ⇒ 组件**空转**；我们同为空转。
                //    先钉住这一条，再**显式** `Refresh()` 单独验算法（测试专用入口，原版从不会走到）。
                Check(tint.RendererCount, 0,
                      "② 没调 `Refresh` 时列表是**空的**（原版 `Refresh` 零调用点 + 306 实例 persistentCalls 全空 ⇒ 空转）");
                tint.Toggle(true);
                Check(tint.RendererCount, 0, "② `Toggle` **不自动收**（原版也不收；🧨 加自动 Refresh ⇒ 红）");
                tint.SetColor(new Color(0.5f, 0.5f, 0.5f, 0.5f), 0f);
                tint.Step(0f);
                Ck(q1.Tint, base1,
                   "② 空转：`SetColor` **一件都不改**（原版同；🧨 在 SetColor 里自动 Refresh ⇒ 这里会变 ⇒ 红）");
                tint.Refresh();                          // ← 显式（算法单元测试；生产永远不调）
                Check(tint.RendererCount, 2, "② 显式 `Refresh` 收到 **2** 颗（含未激活那颗；原版 :55 `includeInactive: true`）");
                tint.SetColor(Color.white, 0f);          // 把上面那步「空转试验」留在 tint 里的 0.5 复位
                Check(tint.TintColor, Color.white, "② 序列化默认 `tintColor` = 白（三份 prefab 实例现读）");

                tint.SetColor(new Color(0.5f, 0.5f, 0.5f, 0.5f), 0f);
                tint.Step(0f);
                Ck(q1.Tint, UIGroupTint.Multiply(base1, new Color(0.5f, 0.5f, 0.5f, 0.5f)),
                   "② 写下去的是 **基色 × tint**（0.8×0.5 = 0.4；🧨 只写 tint 本身 ⇒ 红）");
                CheckNear(q1.Tint.r, 0.4f, 2e-4f, "② 灭自证：`q1.Tint.r` 必须**小于** tint.r（0.4 < 0.5）");
                Ck(q2.Tint, UIGroupTint.Multiply(base2, new Color(0.5f, 0.5f, 0.5f, 0.5f)),
                   "② **未激活的那颗也刷到了**（原版同一条 `DoColorChange` 循环）");
                CheckTrue(!tint.Step(0f) && !tint.enabled,
                          "② 刷完**下一帧自停**（原版 `Update`: `|last−tint|² < 9.9999994e-11` ⇒ `enabled = false`；"
                        + "🧨 永远停在 enabled ⇒ 红）");

                // 补间：0.5 灰 → 白，0.2 秒
                tint.SetColor(Color.white, 0.2f);
                tint.Step(0.1f);
                Ck(q1.Tint, UIGroupTint.Multiply(base1, new Color(0.75f, 0.75f, 0.75f, 0.75f)),
                   "② 补间中点 = `Lerp(0.5, 1, 0.5) = 0.75` × 基色（原版 DOTween 改的就是 `tintColor`）");
                tint.Step(0.1f);
                Ck(q1.Tint, base1, "② 补间终点 = 基色（tint = 白 ⇒ 不改变基色）");
                CheckTrue(!tint.Step(0f) && !tint.enabled, "② 补间结束后同样自停");

                tint.Toggle(false);
                CheckTrue(!tint.enabled, "② `Toggle(false)` ⇒ `enabled = false`（原版 :143-146）");
                tint.Toggle(true);
                CheckTrue(tint.enabled, "② `Toggle(true)` ⇒ 重新启用");
            }

            // ---------------- ③ UIScaleToFit（原版 183 处）
            // 判据：`UIScaleToFit.cs:108-182` + `decomp_full/UIScaleToFit__Fit.c:30-67`（算式与两道保护）。
            {
                var pgo = new GameObject("ScaleParent", typeof(RectTransform));
                pgo.transform.SetParent(probeRoot.transform, false);
                var prt = (RectTransform)pgo.transform;
                prt.sizeDelta = new Vector2(400f, 200f);

                var cgo = new GameObject("ScaleChild", typeof(RectTransform));
                cgo.transform.SetParent(prt, false);
                var crt = (RectTransform)cgo.transform;
                crt.sizeDelta = new Vector2(100f, 100f);

                var fit = UIScaleToFit.Attach(crt);
                CheckTrue(fit.targetRect == prt, "③ `fitInParent = true` ⇒ 目标 = 父节点（原版 `Awake`:39-46）");
                CheckTrue(fit.Fit(), "③ `Fit()` 真的写了 `localScale`");
                CheckNear(crt.localScale.x, 2f, 2e-4f, "③ 400×200 装 100×100 ⇒ **min(4,2) = 2**（🧨 用 max ⇒ 4 ⇒ 红）");
                CheckNear(crt.localScale.y, 2f, 2e-4f, "③ 等比：y 也是 2");
                CheckNear(crt.localScale.z, 2f, 2e-4f, "③ z 同比例（原版写 `Vector3(s,s,s)`；uGUI 下 z 不参与布局）");

                prt.sizeDelta = new Vector2(300f, 150f);
                fit.Fit();
                CheckNear(crt.localScale.x, 1.5f, 2e-4f,
                          "③ 父框变成 300×150 ⇒ **值跟着源变**成 1.5（min(3,1.5)；🧨 只算一次/缓存不刷新 ⇒ 红）");

                fit.padding = new Vector2(-100f, -50f);
                fit.paddingType = UIScaleToFit.PaddingType.Flat;
                fit.Fit();
                CheckNear(crt.localScale.x, 1f, 2e-4f,
                          "③ `Flat`：目标 = 300−100 = 200 × 150−50 = 100 ⇒ min(2,1) = 1（原版是**加** padding）");

                prt.sizeDelta = new Vector2(400f, 200f);
                fit.padding = new Vector2(0.25f, 0.25f);
                fit.paddingType = UIScaleToFit.PaddingType.Multiplier;
                fit.Fit();
                CheckNear(crt.localScale.x, 0.5f, 2e-4f,
                          "③ `Multiplier`：目标 = 400×0.25 = 100 × 200×0.25 = 50 ⇒ min(1,0.5) = 0.5（原版是**乘** padding）");

                var orphan = new GameObject("ScaleOrphan", typeof(RectTransform));
                orphan.transform.SetParent(probeRoot.transform, false);   // 父节点没有 RectTransform
                var fit2 = UIScaleToFit.Attach((RectTransform)orphan.transform);
                CheckTrue(!fit2.Fit() && float.IsNaN(fit2.LastScale),
                          "③ 没有目标框 ⇒ **不写**并出声（原版 :180 `Trying to scale ui without target`；🧨 静默写 1 ⇒ 红）");
            }

            // ---------------- ④ 页签生命周期（GameWindowWithTabs / WindowTabBase / TabButtons）· 夹具页 `ProbeTab` 住 `Shell/ProbeTab.cs`（⚙ 必须在**非 `Editor/`** 目录：编辑器程序集里的 MonoBehaviour `AddComponent` 恒返 null，2026-10-27 修）
            // 判据：`WindowTabBase.cs:54-110` · `GameWindowWithTabs.cs:85-470` · `TabButtons.cs:83-492`。
            {
                var wgo = new GameObject("TabsWin");
                wgo.transform.SetParent(probeRoot.transform, false);
                var tabWin = wgo.AddComponent<GameWindowWithTabs>();

                var a = new GameObject("TabA").AddComponent<ProbeTab>();
                a.type = WindowTabType.Missions;
                a.transform.SetParent(wgo.transform, false);
                a.gameObject.SetActive(false);
                var b = new GameObject("TabB").AddComponent<ProbeTab>();
                b.type = WindowTabType.Forge;
                b.transform.SetParent(wgo.transform, false);
                b.gameObject.SetActive(false);
                tabWin.tabs.Add(a);
                tabWin.tabs.Add(b);
                CheckTrue(tabWin.CurrentTabView == null, "④ 初始没有当前页（原版字段 `0x12` = 空）");

                var tbgo = new GameObject("Tab Buttons");
                tbgo.transform.SetParent(wgo.transform, false);
                var tb = tbgo.AddComponent<TabButtons>();
                tabWin.tabButtons = tb;
                tb.Initialize(tabWin);
                CheckTrue(!tb.FirstTimeGateForTest,
                          "④ `Initialize` 之后 `firstTime` 已消费（原版靠 group 初始 toggle 事件；我们没有事件 ⇒ 见 TabButtons 注释）");

                int ev = 0; WindowTabBase lastEv = null;
                tabWin.OnTabChanged += t => { ev++; lastEv = t; };

                tabWin.ChangeTab(WindowTabType.Missions);
                CheckTrue(a != null && a.gameObject.activeSelf && !b.gameObject.activeSelf,
                          "④ 切到 Missions ⇒ 只有 A 活（`TryOpenTab` 的 `SetActive(true)`）");
                Check(a.opened, 1, "④ A 的 `OnOpen` 被调了 1 次（原版 `TryOpenTab` 第二句）");
                CheckTrue(tabWin.CurrentTabView == a && tabWin.CurrentTab == WindowTabType.Missions,
                          "④ 对象版 / 枚举版 `CurrentTab` 同步 = A / Missions");
                Check(tb.CurrentType, WindowTabType.Missions, "④ `OnTabChanged` → `TabButtons.UpdateTab` 同步了 CurrentType");
                Check(tb.CurrentVisualIndex, 0, "④ 视觉下标 = 0（`visualTypes` 第 1 位）");
                Check(ev, 1, "④ `OnTabChanged` 触发 1 次");
                CheckTrue(lastEv == a, "④ 事件参数 = 新页对象");

                tabWin.ChangeTab(a);                 // **同一页**
                Check(a.opened, 2, "④ 同一页再切 ⇒ **只重开**（原版 `ChangeTabCO` 的 `from == to` 分支）");
                Check(a.closed, 0, "④ 同一页再切 ⇒ **不关**（🧨 走换页分支 ⇒ closed 会变 1）");
                Check(ev, 2, "④ 同一页再切 ⇒ `OnTabChanged` **也触发**（原版 :383-387 在分支外）");

                tabWin.ChangeTab(WindowTabType.Forge);
                CheckTrue(!a.gameObject.activeSelf && b.gameObject.activeSelf, "④ 换页 ⇒ 旧页关、新页开");
                Check(a.closed, 1, "④ 旧页 `CloseTab()` ⇒ `OnClose` 1 次（原版 :372-375）");
                Check(b.opened, 1, "④ 新页 `OnOpen` 1 次");
                Check(tabWin.CurrentTab, WindowTabType.Forge, "④ 枚举版 CurrentTab = Forge");
                Check(tb.CurrentVisualIndex, 2, "④ 视觉下标同步成 2（Forge 在 `visualTypes` 第 3 位）");

                CheckTrue(tabWin.GetTab(typeof(ProbeTab)) == a, "④ `GetTab(Type)` 命中（两条都是 ProbeTab ⇒ 取第一条）");
                CheckTrue(tabWin.GetTab<ProbeTab>() == a, "④ `GetTab<T>()` 同一条");
                var back = tabWin.ChangeTab<ProbeTab>();
                CheckTrue(back == a && tabWin.CurrentTabView == a,
                          "④ `ChangeTab<T>()` 返回并切到该页（原版 :276-306）");

                tb.ToggleForTest(true, b);
                CheckTrue(tabWin.CurrentTabView == b && b.opened == 2 && a.closed == 2,
                          "④ 按键这条路 `Toggle(true, tab)` ⇒ `window.ChangeTab(tab)`（原版 `TabButtons.Toggle` :269-282）");
                tb.ToggleForTest(false, b);
                CheckTrue(tabWin.CurrentTabView == b, "④ `state = false` ⇒ **不切页**（原版 `if (state && tab)`）");

                tabWin.ForceCloseTabs();
                CheckTrue(!a.gameObject.activeSelf && !b.gameObject.activeSelf,
                          "④ `ForceCloseTabs()` ⇒ **所有页**都不活（原版 :409-422）");
                Check(b.closed, 2, "④ 当前页被 `CloseTab()` 过一次（原版第一句）");
                CheckTrue(tabWin.CurrentTabView == b, "④ `ForceCloseTabs` **不清** `CurrentTab`（原版也不清 ⇒ 开回来还是这页）");

                tabWin.ChangeTab(WindowTabType.None);
                CheckTrue(tabWin.CurrentTabView == null && tabWin.CurrentTab == WindowTabType.None,
                          "④ `None` ⇒ 没有当前页（**我们历史入口的分支**，原版没有这个入口）");
                CheckTrue(!a.gameObject.activeSelf && !b.gameObject.activeSelf, "④ `None` ⇒ 全部不活（历史保证）");
                Check(tb.CurrentVisualIndex, -1, "④ `OnTabChanged(null)` ⇒ 按键容器清成 -1/None");

                // `AssignTabButton` / `ToggleInteractable` / `RemoveTabButton`
                var kgo = new GameObject("KeyProbe", typeof(RectTransform));
                kgo.transform.SetParent(tbgo.transform, false);
                ImageQuad.Create(kgo.transform, Texture2D.whiteTexture, Vector3.zero, 0.1f,
                                 new Vector2(0.5f, 0.5f), "keyArt");
                var wb = kgo.AddComponent<WindowButton>();
                tb.AssignTabButton(a, wb);
                CheckTrue(tb.ButtonFor(a) == wb, "④ `AssignTabButton` 登记进 `options` 并可反查（原版 :409-438）");
                tb.ToggleInteractable(a, false);
                CheckTrue(!wb.Interactable, "④ `ToggleInteractable(a,false)` ⇒ 键 **不可交互**（原版 :468-492）");
                tb.ToggleInteractable(a, true);
                CheckTrue(wb.Interactable, "④ `ToggleInteractable(a,true)` ⇒ 恢复");
                tb.RemoveTabButton(a);
                CheckTrue(wb == null, "④ `RemoveTabButton` 把键 **销毁**（原版 :440-466 的 `Destroy(button.gameObject)`）");
                CheckTrue(tb.ButtonFor(a) == null, "④ 且从 `options` 里摘掉");
            }

            // ---------------- ⑤ 按件拖拽（PointerLayer 的 move / up —— 🆕 2026-10-25 纯加法）
            // 判据（用户 2026-10-25 派单 + `报告/C_收藏_第1轮.md` §③ 请求 1 ②）：
            //   「按下落在某个 `WindowButton` 上且该件带可拖标记 ⇒ 之后每次 move 发 `onDrag(pagePx)`，
            //     抬起发 `onDrop`；**与滚动区互斥、可拖件优先**」。
            {
                var pl = PointerLayer.Instance != null ? PointerLayer.Instance : PointerLayer.Ensure();
                CheckTrue(pl != null, "⑤ （前提）`PointerLayer` 在（不在 ⇒ 拖动这条路根本没人派发）");
                if (pl != null)
                {
                    var dgo = new GameObject("DragProbe");
                    dgo.transform.SetParent(probeRoot.transform, false);
                    var dq = ImageQuad.Create(dgo.transform, Texture2D.whiteTexture, Vector3.zero, 1f,
                                              new Vector2(0.5f, 0.5f), "dragImg");
                    dq.SetRenderQueue(3900);                    // 压过壳里其余件（命中取队列最高的那块）
                    var db = dgo.AddComponent<WindowButton>();
                    int clicks = 0, drags = 0, drops = 0;
                    Vector2 lastDrag = Vector2.zero, lastDrop = Vector2.zero;
                    db.onClick = () => clicks++;
                    PointerLayer.RegisterDrag(db,
                        p => { drags++; lastDrag = p; },
                        p => { drops++; lastDrop = p; });
                    Check(PointerLayer.DragCountForTest, 1, "⑤ `RegisterDrag` 登记成功（登记表 1 条）");

                    CheckTrue(pl.PressAt(960f, 540f) == db, "⑤ 按下命中那颗可拖件（`PressAt` 返回它）");
                    CheckTrue(pl.ItemPressActive, "⑤ 按下后可拖件进入「按住」态（与滚动区互斥）");
                    CheckTrue(pl.DraggingScroll == null, "⑤ 可拖件优先：同时落在滚动区上时也**不**接管滚动（`DraggingScroll == null`）");
                    CheckTrue(!pl.MoveTo(960f, 545f), "⑤ 位移 ≤ `DragThreshold`(10) ⇒ 还不算拖动");
                    Check(drags, 0, "⑤ 阈值内不发 `onDrag`（否则轻轻一碰就变拖拽）");
                    CheckTrue(pl.MoveTo(960f, 560f), "⑤ 越过 10px ⇒ 进入拖动（🧨 没加 move 分发 ⇒ 这里恒 false）");
                    Check(drags, 1, "⑤ 越阈值那一下**也发**一次 `onDrag`");
                    CheckTrue(pl.ItemDragging && pl.DraggingItemButton == db, "⑤ `ItemDragging` / `DraggingItemButton` 读口对");
                    pl.MoveTo(960f, 580f);
                    Check(drags, 2, "⑤ 之后**每次移动**都发 `onDrag`");
                    CheckNear(lastDrag.y, 580f, 1e-3f, "⑤ `onDrag` 收到的是**指针当前设计 px**（y = 580）");
                    CheckTrue(!pl.ReleaseAt(960f, 580f), "⑤ 拖动抬起 ⇒ **不算点击**（返回 false）");
                    Check(drops, 1, "⑤ 抬起发 **一次** `onDrop`（🧨 没加 up 分发 ⇒ 这里 0）");
                    CheckNear(lastDrop.y, 580f, 1e-3f, "⑤ `onDrop` 收到的也是设计 px");
                    Check(clicks, 0, "⑤ 拖动这一下**没有派发** `onClick`（同 UGUI：拖拽吃掉了点击）");

                    // 阴性对照：不越过阈值 ⇒ 还是普通点击
                    pl.PressAt(960f, 540f);
                    CheckTrue(pl.ReleaseAt(960f, 540f), "⑤ （阴性对照）按下→抬起同一点 ⇒ 仍算点击");
                    Check(clicks, 1, "⑤ 阴性对照：`onClick` 派发了 1 次");
                    Check(drops, 1, "⑤ 阴性对照：**没有** `onDrop`（没拖起来过）");

                    PointerLayer.UnregisterDrag(db);
                    Check(PointerLayer.DragCountForTest, 0, "⑤ `UnregisterDrag` 摘掉登记");
                }
            }

            // ---------------- ⑥ MenuDraw.DeckCell 的 sortMode（拖拽排序前置件）
            // 判据（`报告/C_收藏_第1轮.md` §③ 请求 1 ③）：`CollectionDeck.SetSortMode` ⇒ content `localScale = 0.8`
            //   （常量 `DAT_1834b2fa0` 实读），并**压制 hover 1.05 与选中高亮**（后者另有
            //   `CollectionDeck__SelectedHighlight.c` 的 `if (!sortMode)` 闸）。
            {
                var dparent = new GameObject("DeckCellProbe");
                dparent.transform.SetParent(probeRoot.transform, false);
                var info = new CollectionData.DeckInfo { Name = "probe", Faction = "", WarlordId = "", CardbackId = "", Count = 0 };
                var rN = new PxRect(0f, 0f, 225f, 364.5f);
                var normal = MenuDraw.DeckCell(dparent.transform, "CellNormal", rN, info, true,
                                               3000, 2999, 3001, 3002, 3003,
                                               // 🔴 **2026-10-26（`B8_BN`）**：这里原来传的是 `null` —— 而 `MenuDraw.DeckCell` 的命中钮
                                               //   **整块包在 `if (onClick != null)` 里**（`Shell/MenuDraw.cs` 的 `DeckCell`，「点击区」那一段：
                                               //   `if (onClick != null) { … AddComponent<WindowButton>() … }`）⇒ `null` = **这一格根本没有 Hit/WindowButton**，
                                               //   下面「格子里有命中钮」必然红（那条断言本身没错，是**夹具少给了一个实参**）。
                                               //   生产两个调用点**都传非空**：`Shell/CollectionWindow.cs:5286` 的 `() => { … SelectDeck(idx); }` ·
                                               //   `Shell/DeckSelectionPopup.cs:964` 的 `() => UpdateInfo(pick)` ⇒ 夹具照生产给一个空动作即可（⛔ 别改成放宽断言）。
                                               () => { });
                CheckTrue(normal != null, "⑥ （前提）非排序列建出来了");
                CheckNear(normal.localScale.x, 1f, 1e-4f, "⑥ 非排序列：不缩放");
                CheckTrue(FindChildIn(normal, "Highlight Rounded Square") != null,
                          "⑥ （前提）非排序列 + `selected` ⇒ 画了高亮");
                var wbN = normal != null ? normal.GetComponentInChildren<WindowButton>(true) : null;
                CheckTrue(wbN != null, "⑥ （前提）格子里有命中钮");
                if (wbN != null)
                {
                    wbN.onEnter();
                    CheckNear(normal.localScale.x, MenuDraw.DeckCellHoverK, 1e-4f,
                              "⑥ 非排序列 hover ⇒ **1.05**（原版 `CollectionDeck.HoverHighlight`）");
                    wbN.onExit();
                    CheckNear(normal.localScale.x, 1f, 1e-4f, "⑥ 非排序列离开 ⇒ 还原 1.0");
                }

                var sorted = MenuDraw.DeckCell(dparent.transform, "CellSorted",
                                               new PxRect(300f, 0f, 525f, 364.5f), info, true,
                                               3000, 2999, 3001, 3002, 3003,
                                               () => { },     // 同上一格：`onClick == null` ⇒ `DeckCell` 不建命中钮（见上一条的注释）
                                               maskPad: Vector4.zero, sortMode: true);
                CheckTrue(sorted != null, "⑥ （前提）排序列建出来了");
                CheckNear(sorted.localScale.x, MenuDraw.DeckCellSortK, 1e-4f,
                          "⑥ 排序列 ⇒ content `localScale = 0.8`（🧨 没接 `sortMode` ⇒ 1.0 ⇒ 红）");
                CheckNear(sorted.localScale.y, MenuDraw.DeckCellSortK, 1e-4f, "⑥ 等比：y 也是 0.8");
                CheckTrue(FindChildIn(sorted, "Highlight Rounded Square") == null,
                          "⑥ 排序列**压制选中高亮**（原版 `SelectedHighlight` 的 `if (!sortMode)`；"
                        + "🧨 只改缩放不改这条 ⇒ 红）");
                var wbS = sorted != null ? sorted.GetComponentInChildren<WindowButton>(true) : null;
                CheckTrue(wbS != null, "⑥ （前提）排序列里也有命中钮");
                if (wbS != null)
                {
                    wbS.onEnter();
                    CheckNear(sorted.localScale.x, MenuDraw.DeckCellSortK, 1e-4f,
                              "⑥ 排序列 hover **不放大**（必须仍是 0.8；🧨 照旧 1.05 ⇒ 红）");
                    wbS.onExit();
                    CheckNear(sorted.localScale.x, MenuDraw.DeckCellSortK, 1e-4f,
                              "⑥ 排序列离开后仍是 0.8（🧨 被 onExit 还原成 1.0 ⇒ 红）");
                }
            }

            // ---------------- ⑦ 资源条 overlay（原版 `ResourcesBarController.ToggleOverlay`）
            // 判据：`ResourcesBarController__ToggleOverlay.c` —— 卡片详情窗打开时把资源条抬到那扇窗**之上**，
            //   关掉时还原（调用点 = `CardDisplayWindow.ShowCard/Close/ToBackground/ToFocus`）。
            //   我们没有 uGUI Canvas ⇒ 等价物 = 把资源条子树的渲染队列整体 **+134**（2986 → 3120，高于详情窗最高的 3118）。
            {
                // 🔴 **2026-10-26（`B8_BN`）**：这一段原来读 `TopBar.LastBuilt` 当夹具 —— 而 **`ShellScene` 这场里根本没有顶栏**：
                //    顶栏只由 `Shell/MainMenuRuntime.cs` 的 `Build()`（`TopBar.Build(safeH, …)`）与 `Shell/DeckRuntime.cs` 的
                //    `BuildTopBar`（`if (HostWindow != null) return;`）建；本宿主的 `Build()` 只造 `ShellRuntime`（壳），
                //    全场没有任何一处调 `TopBar.Build` ⇒ `LastBuilt` 恒 `null`（= 那条「（前提）顶栏建出来了」红，
                //    而下面 5 条全被它**静默跳过**（`if (bar != null && bar.Root != null)`）⇒ 一段 6 条断言等于没跑）。
                //    ⇒ 夹具**自己建一条**（生产同一个入口 `TopBar.Build`；`MainMenuScene` 的 A374 也是这么另开一台实例的），
                //      跑完销毁 + 把 `LastBuilt` 还回去（⛔ 别留给后面的段：`CardDetailPopup` 那几个 `ToggleOverlay(TopBar.LastBuilt, …)`
                //      会读到这个夹具，销毁后就成假 null 的幽灵）。
                var keepLastBuilt = TopBar.LastBuilt;
                var tbProbe = new GameObject("TopBarProbe");
                tbProbe.transform.SetParent(probeRoot.transform, false);
                var bar = TopBar.Build(tbProbe.transform, null);
                CheckTrue(bar != null && bar.Root != null,
                          "⑦ （前提）顶栏建出来了（**夹具** `TopBar.Build` —— `ShellScene` 自己从来不建顶栏，"
                        + "顶栏只由 `MainMenuRuntime.Build` / `DeckRuntime.BuildTopBar` 建；⛔ 不是放宽：本条只负责给下面 5 条一个真顶栏）");
                if (bar != null && bar.Root != null)
                {
                    var q0 = bar.Root.GetComponentInChildren<ImageQuad>(true);
                    CheckTrue(q0 != null, "⑦ （前提）顶栏里至少有一颗 `ImageQuad`");
                    if (q0 != null)
                    {
                        int before = q0.RenderQueue;
                        TopBar.ToggleOverlay(bar, true);
                        CheckTrue(bar.OverlayOn && TopBar.OverlayTargetCountForTest(bar) > 0,
                                  "⑦ overlay 打开 ⇒ 有一批件被抬高（🧨 方法空转 ⇒ 这里红）");
                        Check(q0.RenderQueue, before + (TopBar.QBarOverlayBase - TopBar.QBarPanel),
                              "⑦ 队列抬到 `+134`（2986 → 3120，高于卡片详情窗最高 3118）");
                        TopBar.ToggleOverlay(bar, false);
                        CheckTrue(!bar.OverlayOn, "⑦ 关掉 ⇒ overlay 态清掉");
                        Check(q0.RenderQueue, before, "⑦ 队列**逐件还原**（🧨 不还原 ⇒ 资源条永远压在窗上 ⇒ 红）");
                    }
                }
                TopBar.LastBuilt = keepLastBuilt;      // 还原（见上面那段：别给后面的段留一个指向夹具的幽灵）
                Object.DestroyImmediate(tbProbe);      // 夹具连它登记的命中区一起消失（`PointerLayer` 的死条目会被顺手清掉）
            }
        }
        finally
        {
            Object.DestroyImmediate(probeRoot);
        }

        MenuCheck.RaycastFilterAsserts(_sink); MenuCheck.ToastChannelAsserts(_sink); B97ProfileAssert.Run(_sink);   // 🆕 2026-10-28（B89-R3d · 账 `未分类_其他` 230）：教程期命中白名单那 3 条（空表=不过滤 / 死码口 / 注册清空成对）｜🆕 2026-10-28（B89-E16d）：**横幅 ≠ 模态窗**那 4 条（决斗窗没卡组 / 删卡组失败 / 点 Player ID / 导入支源码扫描）—— 同 R3d 口径：**追加在同一行末**，行数 0 变化、落点零漂移。都在合计行**之前** ⇒ 计数进本宿主的 `_sink`
        Debug.Log(P + "LiveOpsFilterSelfCheck 跑 " + LiveOpsFilterSelfCheck.Run((ok, msg) => CheckTrue(ok, msg)) + " 条断言");   // 🆕 2026-10-28（B93_E19）：`Shell/LiveOpsFilter.cs` 的 `LiveOpsFilterSelfCheck.Run` 原来**全仓 0 调用方**（那一批断言一条都不跑）。接在**合计行之前** ⇒ 其后无账落点（本文件最大账落点 = `ShellScene.cs:10351`），本行 = 文件行数 **+1**、其下游只位移 `+1`。**打印返回条数 = 自证它真被跑到**（`Run` 返回实际执行的断言数）。
        // ---------------- ④·c 🆕 2026-10-28（B93_E24）：**输入层停用 = 原版 `eventSystem.enabled = false`** ----------------
        //
        // ⚠️ **位置说明（B93_E27 落地时改过一次）**：E24 报告建议「紧跟 ESC 三门槛那一节（`:3765` 之后）」。
        //   但本文件有账按 `文件:行号` 引用（最大落点 = `ShellScene.cs:10351`）⇒ **在 `:3765` 插 80 行会让
        //   `:10351` 之后所有落点整体位移 = 漂移暴增**（铁律：新断言一律加在**最后一条账落点之后**）。
        //   ⇒ 改为**合计行之前**（`RunCore` 尾部，`:10351` 之后）—— 断言照样被跑到（同一方法体内），落点零漂移。
        //   判据（`判据/B93_Q12_SetInputEnabled语义对齐.md` §二）：原版 `EventSystemController.Toggle(false)` 写的是
        //   `eventSystem.enabled` ⇒ `EventSystem.Update()` 整段不跑（`EventSystem.cs:472-531`）⇒ `Process()` 里那四条支路
        //   （`StandaloneInputModule.cs:294-315`：悬停 / 点击 / 滚轮 / 方向键 / 回车 / 打字）**一整帧都不跑**；
        //   同时 `OnDisable → DeactivateModule → ClearSelection`（`EventSystem.cs:429-444` · `PointerInputModule.cs:394-406`）
        //   派 OnPointerExit、清指针缓存、清选中。
        // 🔴 **灭自证**：②③④ 与 ⑥⑦⑧ 用的是**同一颗钮、同一个坐标、同一个滚动区**，只翻 `InputEnabled` 一位，
        //   期望值**相反** ⇒ 五种坏实现各自红在不同的条上：
        //   · `HitButton` 恒返回 null（命中表坏）      ⇒ ① 与 ③ 红
        //   · 闸压根没接（改前现状）                  ⇒ ⑥⑦⑧⑨ 红
        //   · 只挡指针、不挡键盘（= 判据的方案 B）      ⇒ ⑨ 里**只有**「键盘那条」红
        //   · 只挡 ESC（改前的旧口径）                ⇒ ⑥⑦⑧ 红
        //   · 只加两行 `return null`、不清状态        ⇒ ⑩⑪ 红（⑥⑦⑧ 全绿 —— 这正是「只改命中口」能骗过的那一档）
        Section("B93-E24：输入层停用（= 原版 `eventSystem.enabled = false`）—— 指针 / 滚轮 / 悬停 / 键盘全停 + 清选中与指针缓存");
        {
            var plIn = PointerLayer.Instance;
            CheckTrue(plIn != null, "前置：指针层在场（下面每一条都走它这条唯一输入路）");
            PointerLayer.SetInputEnabled(true);      // 本节自己收拾干净，别受上面那一节（ESC 三门槛）影响
            CheckTrue(PointerLayer.InputEnabled, "前置：`InputEnabled == true`（本节从「开着」这一态起步）");

            // 探针：一颗真钮 + 一个滚动区（命中点固定 (400,400)，几态共用 —— 与 N8 同形）
            var inGo = new GameObject("B93E24Probe");
            var inHits = new GameObject("Hits").transform;
            inHits.SetParent(inGo.transform, false);
            var inBtn = MakeNavButton(inHits, "B93E24Btn", 400f, 400f, 80f, 40f);
            int inFired = 0;
            inBtn.onClick = () => inFired++;
            var inScroll = MenuScroll.TopAligned(new PxRect(200f, 200f, 600f, 900f), 2000f);
            inScroll.Owner = inGo;
            PointerLayer.RegisterScroll(inScroll);

            // ---- ① 前提（**硬检查**：⛔ 不写成 `if (前提) Check(结论)` —— 前提不成立时那条会静默空转）
            CheckTrue(plIn.ButtonAt(400f, 400f) == inBtn, "① 前置：命中表是活的（同一坐标命中探针钮）");
            CheckTrue(plIn.ClickAt(400f, 400f), "① 前置：`ClickAt` 真的派发");
            Check(inFired, 1, "① …探针钮被点了一次");
            float inOff0 = inScroll.Offset;
            CheckTrue(plIn.WheelAt(400f, 400f, -1f), "① 前置：滚轮滚得到（`HitScroll` 那条登记表路是活的）");
            CheckTrue(inScroll.Offset > inOff0, "① …偏移真的动了（返回 true 但没滚 = 假绿）");
            CheckTrue(plIn.HoverAt(400f, 400f) == inBtn && inBtn.Hovered, "① 前置：悬停落到它身上");
            plIn.Select(inBtn);
            CheckTrue(plIn.Selected == inBtn, "① 前置：选中放得进探针钮（⑩「停用后选中被清」的对照态）");
            CheckTrue(plIn.KeySubmit(), "① 前置：回车/空格**派发得出去**（键盘那条活着）");
            Check(inFired, 2, "① …又点了一次（`KeySubmit` → `Button.Click()`）");

            // ---- ② 停用（**只翻这一位**）
            PointerLayer.SetInputEnabled(false, "ShellScene/B93E24");
            CheckTrue(!PointerLayer.InputEnabled, "② `SetInputEnabled(false)` ⇒ `InputEnabled == false`");
            CheckTrue(plIn.ButtonAt(400f, 400f) == null, "② 同一坐标**命不到**探针钮（= 原版 `Process()` 整段不跑）");
            CheckTrue(!plIn.ClickAt(400f, 400f), "② `ClickAt` 也吃不到");
            Check(inFired, 2, "② …而且**一次都没派发**（只把返回值改 false、照样派发 = 假绿）");
            float inOff1 = inScroll.Offset;
            CheckTrue(!plIn.WheelAt(400f, 400f, -1f), "② 滚轮也落空（`HitScroll` 是**另一条**登记表路 —— 只改 `HitButton` 时这里会静默地滚）");
            CheckTrue(!plIn.WheelAt(400f, 400f, new Vector2(0f, -1f)), "② 同上的 `Vector2` 重载也落空（`Update` 用的就是它）");
            CheckNear(inScroll.Offset, inOff1, 0.001f, "② …偏移一个像素都没动");
            CheckTrue(plIn.HoveredForTest == null && !inBtn.Hovered,
                      "② = `HandlePointerExitAndEnter(pointer, null)`：**悬停被退掉**（`HoveredForTest` 与 `Hovered` 两面都断，"
                    + "只把返回值改 null 不算）");
            CheckTrue(plIn.Selected == null,
                      "② = `ClearSelection` 的 ③：**选中被清掉**（只在那两口加 `return null`、不做状态清理的实现在这里红）");
            CheckTrue(!plIn.KeySubmit(), "② 键盘那条也停（`KeySubmit` = 原版 `SendSubmitEventToSelectedObject`）");
            Check(inFired, 2, "② …回车一次都没派发（挡「只挡指针」那一半 = 判据的方案 B）");

            // ---- ③ 翻回（灭自证：同一坐标 / 同一颗钮全部恢复）
            PointerLayer.SetInputEnabled(true, "ShellScene/B93E24 收尾");
            CheckTrue(PointerLayer.InputEnabled, "③ `SetInputEnabled(true)` ⇒ 恢复");
            CheckTrue(plIn.ButtonAt(400f, 400f) == inBtn,
                      "③ 同一坐标**恢复命中**（⛔ 只断 ② 不断这条 ⇒「`HitButton` 恒返回 null」也能全绿）");
            CheckTrue(plIn.ClickAt(400f, 400f), "③ 点击恢复派发");
            Check(inFired, 3, "③ …探针钮又点了一次");
            float inOff2 = inScroll.Offset;
            CheckTrue(plIn.WheelAt(400f, 400f, -1f), "③ 滚轮恢复");
            CheckTrue(inScroll.Offset > inOff2, "③ …偏移真的又动了");
            CheckTrue(plIn.HoverAt(400f, 400f) == inBtn && inBtn.Hovered, "③ 悬停恢复");

            // 清场（批处理没有帧循环 ⇒ 一律 `DestroyImmediate`；先退悬停、再退滚动登记）
            plIn.HoverAt(10f, 10f);
            PointerLayer.UnregisterOwnedBy(inGo);
            Object.DestroyImmediate(inGo);
            PointerLayer.SetInputEnabled(true, "ShellScene/B93E24 兜底");
        }

        // ============================================================ B93_U1：原版 uGUI 矩形工具族
        //   判据：decomp_full/UIExtension__{GetCorners,MaxX,MaxY,MinX,MinY}.c ·
        //         decomp_full/RectExtensions__GetWorldRect.c:20-29
        //   🔴 两条【灭自证】：
        //      ① 把件**转 30°** 之后断 MaxX == corners[2].x —— 轴对齐时「取下标」与「取最大值」同值，
        //         只有旋转过的矩形能把「写成 Mathf.Max 扫描」抓出来；
        //      ② 断「两次调用的返回值是同一个数组实例」—— 抓「改成每次 new」。
        Section("B93_U1：UIExtension / RectExtensions.GetWorldRect（原版 uGUI 矩形工具族）");
        {
            var probe = new GameObject("b93u1_rectProbe", typeof(RectTransform));
            try
            {
                var rt = (RectTransform)probe.transform;
                rt.SetParent(null, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(200f, 100f);
                rt.anchoredPosition = Vector2.zero;   // 🆕 2026-10-28（B93_E27 · 调度台裁定 A）：原来是 (123,45) —— `UIExtension.MaxX/MinX/MaxY/MinY` 取的是**世界空间**角（`GetWorldCorners`），而 `rt.rect.*` 是**局部空间** ⇒ 带偏移时两者恒差那个偏移（夹具瑕疵，不是实现缺陷）⇒ 探针归零，让 ③ 只测「无关偏移时两者相等」；④ 的「转 30° 后 `MaxX == corners[2].x`」灭自证**不受影响**（它只跟旋转有关）
                rt.localRotation = Quaternion.identity;
                Canvas.ForceUpdateCanvases();

                // ① 前提（硬检查）：四角真的拿到了、且角序是 Unity 的「左下→左上→右上→右下」
                var c1 = UIExtension.GetCorners(rt);
                Check(c1 != null, true, "① 前提：GetCorners 返回非 null");
                Check(c1 != null ? c1.Length : -1, 4, "① 前提：四角数组长度 = 4");
                CheckTrue(c1[2].x > c1[0].x && c1[1].y > c1[0].y,
                          "① 前提：角序 = 左下→左上→右上→右下（c2.x > c0.x 且 c1.y > c0.y）");
                // ② 灭自证：静态复用数组（两次调用是同一个实例）
                CheckTrue(ReferenceEquals(c1, UIExtension.GetCorners(rt)),
                          "② 灭自证：GetCorners 复用同一个静态数组（原版 DAT_18429f330）—— 改成每次 new 会红");
                // ③ 轴对齐档：Max/Min 与 PxRect 四边同值
                CheckNear(UIExtension.MaxX(rt), rt.rect.xMax, 0.01f, "③ MaxX = rect.xMax（轴对齐）");
                CheckNear(UIExtension.MinX(rt), rt.rect.xMin, 0.01f, "③ MinX = rect.xMin（轴对齐）");
                CheckNear(UIExtension.MaxY(rt), rt.rect.yMax, 0.01f, "③ MaxY = rect.yMax（轴对齐）");
                CheckNear(UIExtension.MinY(rt), rt.rect.yMin, 0.01f, "③ MinY = rect.yMin（轴对齐）");
                // ④ 灭自证（旋转档）：MaxX 必须等于 corners[2].x，而不是四角里最大的 x
                rt.localRotation = Quaternion.Euler(0f, 0f, 30f);
                Canvas.ForceUpdateCanvases();
                var cr = UIExtension.GetCorners(rt);
                float maxOfAllX = Mathf.Max(Mathf.Max(cr[0].x, cr[1].x), Mathf.Max(cr[2].x, cr[3].x));
                CheckNear(UIExtension.MaxX(rt), cr[2].x, 0.01f,
                          "④ MaxX = corners[2].x（**转 30° 后**；写成 Max 扫描 ⇒ 这里会差出来）");
                CheckTrue(Mathf.Abs(cr[2].x - maxOfAllX) > 0.5f,
                          "④ 灭自证前提：转 30° 后「右上角的 x」确实不是四角最大 x（差 > 0.5）");
                // ⑤ GetWorldRect = 以 c0 基点、c2 对角 → **中心写进 x/y 槽** + 对角线尺寸
                // 🔴 **2026-10-28（B93_E30 · 按 `B93_DG2` 裁定的「α 断言错」改）**：原版把**中心**写进
                //   `Rect.m_XMin` / `m_YMin`（`decomp_full/RectExtensions__GetWorldRect.c:20-29` = `*p = c0.x + w*0.5`、
                //   `p[1] = c0.y + h*0.5`；ILSpy `cpp2il/out_cpp2il_dev_ilspy/RectExtensions.cs:246-277` 的 `m_XMin = xMin`），
                //   我们的实现**逐字照抄**（`Core/UguiRect.cs:345-352`，它自己的 doc 就写着「x, y = 中心」）
                //   ⇒ 读 `Rect.center` 会把半宽半高**再加一次**（探针归零后实得 `61.603/93.301` = `c2.x/c2.y`）。
                //   ⛔ 别改实现去写「中心 − 半宽」：那才偏离原版（原版消费者 `UICulling.cs:182-200` 比的也是 `m_XMin`）。
                Rect wr = RectExtensions.GetWorldRect(rt);
                CheckNear(wr.x, (cr[0].x + cr[2].x) * 0.5f, 0.01f, "⑤ GetWorldRect 的 **x 槽 = 中心 x** = (c0+c2)/2（原版写 `m_XMin`，⛔ 不是 Unity 习惯的左下角）");
                CheckNear(wr.y, (cr[0].y + cr[2].y) * 0.5f, 0.01f, "⑤ GetWorldRect 的 **y 槽 = 中心 y** = (c0+c2)/2（同上，写进 `m_YMin`）");
                CheckNear(wr.width, cr[2].x - cr[0].x, 0.01f, "⑤ GetWorldRect 宽 = c2.x − c0.x（轴对齐包围盒）");
                CheckNear(wr.height, cr[2].y - cr[0].y, 0.01f, "⑤ GetWorldRect 高 = c2.y − c0.y");
                // ⑥ 灭自证：null 入参必须抛（原版 null-check throw），⛔ 不许静默返回空数组
                bool threw = false;
                try { UIExtension.GetCorners(null); } catch (System.ArgumentNullException) { threw = true; }
                CheckTrue(threw, "⑥ 灭自证：GetCorners(null) 抛 ArgumentNullException —— 改成静默返回会红");
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
        }

        // 判据：decomp_full/EmptyGraphic__OnPopulateMesh.c（全文 = VertexHelper.Clear）⇒ 图形在、画面无。
        // 我们的等价 = MenuDraw.Hit 建的命中节点把 quad 设成全透明（Shell/MenuDraw.cs:2292）。
        // 灭自证：断的是 **tint 的 alpha == 0**（画面不可见），⛔ 不是断「建了个 ImageQuad」——后者改成有色也会绿。
        Section("B93_U1：EmptyGraphic 等价物 —— 命中区全透明");
        {
            var host = new GameObject("b93u1_hitProbe", typeof(RectTransform));
            try
            {
                var hit = MenuDraw.Hit(host.transform, "HitProbe",
                                       new PxRect(0f, 0f, 100f, 50f), 4000, null);
                CheckTrue(hit != null, "① 前提：MenuDraw.Hit 建出了节点");
                var iq = hit != null ? hit.GetComponentInChildren<ImageQuad>(true) : null;
                CheckTrue(iq != null, "① 前提：命中节点下**必须有 ImageQuad**（PointerLayer 只认它）");
                CheckNear(iq != null ? iq.Tint.a : 1f, 0f, 0.001f,
                          "② 命中 quad 全透明（= 原版 EmptyGraphic 的『不画但有 Graphic』）");
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }
        // ================================================================================================
        //  🆕 2026-10-28（B93_D1 · 账 批次 B93-4）—— 活动窗 A：开关图 / 战斗钮 interactable /
        //  Collect interactable / 里程碑状态投影 / 生存标记 / 阵营条 / 三个同名入口。
        //  判据全文 → 资料/完全复刻/报告/B93_D1.md §1
        // ================================================================================================
        Section("B93_D1 · 活动窗 A（排位开关图 / Battle interactable / Collect interactable / 生存标记 / 阵营条）");
        {
            // ---- ① 排位窗开关粒：图按 `RankedData.IsRanked` 换（原版 `ToggleDisplay.Setup`）----
            {
                // 🔴 **2026-10-28（B93_E30 · 按 `B93_DG2` 裁定的「δ 夹具前提」改）**：原来是 `Create(null)`，两处都不对：
                //   ① 没有 `WindowsManager`（② 那三条入口靠它 `OpenWindow`）；② **建树在 `Open()` 里**
                //   —— `Create` 只 `Init`（赋窗口参数 + `AttachToAnchor`，`Shell/LiveOpsEventWindow.cs:538-547`），
                //   `Build()` 的唯一入口是 `Open()`（`:564`）⇒ 树是空的，`ChangeRankedToggle` 当然找不到。
                //   ⚠️ 同文件别的窗夹具一律 `Create(...)` 之后立刻 `Open()`（`:9314-9317` 等）。
                var rk = RankedEventWindow.Create(shell.Windows);
                CheckTrue(rk != null, "（前提）排位窗建出来了");
                CheckTrue(TryOpenForSelfCheck(rk, "RankedEventWindow/①"),
                          "（前提）`Open()` 跑通（树在它里面建；抛异常 ⇒ 只跳过本小段，不影响其它批次）");
                // 纯函数那一半：两个方向的映射都钉住（改坏法：把 on/off 调回来 ⇒ 这两条同时红）
                Check(RankedEventWindow.ToggleKnobArt(false), "40_main_bt_toggle_off",
                      "状态 false ⇒ `40_main_bt_toggle_off`（判据 = `MonoBehaviour_-3197939917699577936.json` 的 toggleOffSprite）");
                Check(RankedEventWindow.ToggleKnobArt(true), "40_main_bt_toggle_on",
                      "状态 true ⇒ `40_main_bt_toggle_on`（同一个 prefab 的 toggleOnSprite）");
                // 🔴 **2026-10-28（B93_E27 · 调度台裁定）**：夹具查找一律**先硬检查、拿不到只跳过本小段** ——
                //    ⛔ 绝不让 `Find(...)` / `FindChildIn(...)` 的 NRE 逃出 `RunCore`：那会打断**后面所有批次**（一条都不跑）。
                //    （`FindChildIn` 对 null 父件本来返回 null；`Find` 的 null 根也已就地改成返回 null。）
                var knobNode = rk != null ? FindChildIn(rk.transform, "ChangeRankedToggle") : null;
                CheckTrue(knobNode != null, "★ 前提：`ChangeRankedToggle` 节点存在（拿不到 ⇒ **只跳过本小段**，不影响其它批次）");
                if (knobNode != null)
                {
                    var knob = Find("Image", knobNode);
                    var knobQ = knob != null ? knob.GetComponent<ImageQuad>() : null;
                    CheckTrue(knobQ != null, "（前提）`ChangeRankedToggle/Image`（= 原版 `ToggleDisplay.toggleImage`）在");
                    // 落到节点上：出厂 `BuildLeftColumn` 收尾就会 `SetRankedToggle()` ⇒ 图必须等于「当前状态」那一张
                    bool ranked = RankedData.IsRanked;
                    CheckTrue(knobQ != null && knobQ.Texture != null
                              && knobQ.Texture.name == (ranked ? "40_main_bt_toggle_on" : "40_main_bt_toggle_off"),
                              "★ 开关粒的**当前贴图** = 当前状态那一张（改坏法：把 `BuildLeftColumn` 的 `SetRankedToggle()` "
                            + "换回恒 `40_main_bt_toggle_on` ⇒ 未排名时这条红；实得 "
                            + (knobQ != null && knobQ.Texture != null ? knobQ.Texture.name : "<null>") + "）");
                }
                if (rk != null) RewardsWindow.DestroySafe(rk.gameObject);
            }

            // ---- ② 三个同名入口真的接到那三条链上（原版同名方法 = 点击入口）----
            {
                // 🔴 **2026-10-28（B93_E30 · 同 ① 的 δ）**：`Manager` 必须是真的那一台 —— 三条 `Open*` 的**首句**
                //   就是 `Manager == null ⇒ 出声 + return null`（`Shell/LiveOpsEventWindow.cs:1890 / :1932 / :1534`），
                //   上一轮的日志里那三条警告就是它。`shell.Windows` = 本宿主那台真的（`:1030` 建、`:1554` 的注释写明）。
                var rk = RankedEventWindow.Create(shell.Windows);
                CheckTrue(TryOpenForSelfCheck(rk, "RankedEventWindow/②"), "（前提）`Open()` 跑通");
                // ⚠️ **前提**：`View Deck Button` 那条链自己有一道**空态闸**（`OpenDeckInfo`：`PickedPrebuilt == null && DeckIndex < 0`
                //   ⇒ 出声、不开窗）⇒ 「本窗模式 ∧ 本阵营有一套卡组」是下面那条断言的**前提**；空态时它是红的，
                //   而原因**不在入口接线**（下面这条把它分开报）。
                CheckTrue(rk.DeckIndex >= 0 || rk.PickedPrebuilt != null,
                          "（前提）排位窗有可看的卡组（`DeckIndex >= 0` || 预选预组，实得 `DeckIndex = " + rk.DeckIndex + "`）");
                rk.ChangeDeckButtonClick();
                CheckTrue(LiveOpsEventWindow.LastDeckSelection != null,
                          "★ `ChangeDeckButtonClick()` ⇒ 开 `DeckSelectionPopup`（= 原版 `OpenWindow<DeckSelectionPopup>`）");
                rk.ViewDeckButtonClick();
                CheckTrue(LiveOpsEventWindow.LastDeckInfo != null,
                          "★ `ViewDeckButtonClick()` ⇒ 开 `DeckInfoPopup`（= 原版 `OpenWindow<DeckInfoPopup>`）");
                rk.HelpButtonClick();
                CheckTrue(LiveOpsEventWindow.LastHelpPopup != null,
                          "★ `HelpButtonClick()` ⇒ 开事件说明弹窗（= 原版 `HasComponent(HelpPopup)` → `OpenWindow`）");
                // 灭自证：把上面任一个入口改成空体（不转调）⇒ 对应那条立刻红；三条互相独立，不是同一条的三种说法。
                // 🔴 **2026-10-28（B93_E30）**：那三扇弹窗是走 `Manager.OpenWindow` 开的（**登记在 `openWindows` 里**）
                //   ⇒ 收尾必须 `CloseAllWindows()`（逐扇 `Close()` 摘表再清），⛔ 别只 `DestroyImmediate` 窗根
                //   （那会在窗表里留一个 Unity 假 null）。纪律同 `:5830` / `:8849` / `:8947`。
                shell.Windows.CloseAllWindows();
                RewardsWindow.DestroySafe(rk.gameObject);
            }

            // ---- ③ 小冲突窗：`Battle!` interactable 与「卡组不可用」那一行 = 同一个布尔 ----
            {
                SkirmishEventWindow.ResetEventProbeForTest();
                // 🔴 **2026-10-28（B93_E30）· 本节新增的夹具**（超出 `B93_DG2` 清单的那一处，理由见报告）：给
                //   「遭遇模式 ∧ 本阵营」造**一副自有卡组**。为什么非有不可 —— 原版 **no-deck 那一档两处都不亮**
                //   （`Shell/SkirmishEventWindow.cs:773-777`：`valid = false` ⇒ 钮不可点；`deck == null` ⇒
                //   「卡组不可用」那一行**隐藏**）⇒ 空库时下面那条双条件**恒假**（`false == true`）。
                //   ⚠️ 上一轮它是**假绿**：当时没有 `Open()` ⇒ `BattleButtonWb == null` ⇒ `BattleInteractable`
                //   按实现**恒 true**（`LiveOpsEventWindow.cs:2328` 的注释写着「没有按钮 ⇒ true」）⇒ `true == true`。
                //   ⛔ 别改成「放宽断言」或「让 `BattleInteractable` 空态返 false」—— 那会动原版语义（`Shell/*` 不在本笔）。
                //   夹具走本文件既有的「不碰玩家真存档」那一套（`:5888-5903` / `:8851-8950`）。
                string keepDeckPath3 = RuleEngine.DeckStore.OverridePath;
                try
                {
                    RuleEngine.DeckStore.OverridePath = "d:/4/_tmp_view/shell/_b93e30_decks.json";
                    try { System.IO.File.Delete(RuleEngine.DeckStore.OverridePath); } catch { }
                    var lib3 = RuleEngine.DeckLibrary.Load();
                    var fd3 = lib3.Create("B93E30夹具·遭遇", (int)RuleEngine.GameMode.Skirmish);
                    fd3.WarlordId = "ASH1";     // SaimHann 督军 ⇒ 阵营与「本机当前那套」一致；**0 张卡 = 故意不合法**（走「有卡组但校不过」那一档）
                    lib3.Save();
                    CollectionData.ResetForTest();   // `CollectionData._lib` 是进程级缓存 ⇒ 必须作废，才读得到这份临时库

                    var sk = SkirmishEventWindow.Create(shell.Windows);
                    bool skOpened = TryOpenForSelfCheck(sk, "SkirmishEventWindow/③");
                    CheckTrue(skOpened, "（前提）`Open()` 跑通（抛异常 ⇒ 只跳过本小段，不影响其它批次）");
                    CheckTrue(skOpened && sk.BattleButtonWb != null,
                              "（前提）`Battle!` 那颗 `WindowButton` 拿得到（`To Battle Button/BattleHit`）");
                    var ban = sk != null ? FindChildIn(sk.transform, "Banned card in deck") : null;   // 🆕 2026-10-28（B93_E27 · 调度台裁定）：夹具拿不到时不许 NRE 逃出 RunCore
                    var banText = ban != null ? FindChildIn(ban, "Text") : null;
                    CheckTrue(banText != null, "（前提）`Banned card in deck/Text` 在");
                    bool lineOn = banText != null && banText.gameObject.activeSelf;
                    CheckTrue(sk.BattleInteractable == !lineOn,
                              "★ 战斗钮可点 ⟺ 「卡组不可用」那一行**关着**（两处消费者同一个 `TryValidateCurrentDeck`；"
                            + "改坏法：`interactable` 另写一份判据 ⇒ 这两格会分叉）");
                    sk.SetBattleInteractable(false);
                    CheckTrue(!sk.BattleInteractable, "★ 置假 ⇒ `Battle!` 不可点（= 原版 `battleButton.interactable = valid`）");
                    sk.SetBattleInteractable(true);
                    CheckTrue(sk.BattleInteractable, "★ 置真 ⇒ 恢复可点（灭自证：上面两条只翻这一个维度）");
                    RewardsWindow.DestroySafe(sk.gameObject);
                }
                finally
                {
                    RuleEngine.DeckStore.DeleteFile();          // ⚠️ 必须在还原路径**之前**删（`Path` 读的是 `OverridePath`）
                    RuleEngine.DeckStore.OverridePath = keepDeckPath3;
                    CollectionData.ResetForTest();
                }
            }

            // ---- ④ 里程碑状态投影（原版 `RefreshScore` 的 lambda）----
            {
                var a = SkirmishEventWindow.MilestoneStates(25, 0, new[] { 10, 20, 30, 40, 50 });
                Check(a[0], 1, "到达但未领 ⇒ Unlocked(1)（原版 `(i < collectedRewards) + 1`）");
                Check(a[1], 1, "第二档同样 Unlocked(1)");
                Check(a[2], 0, "没到（25 < 30）⇒ Locked(0)");
                var b = SkirmishEventWindow.MilestoneStates(50, 2, new[] { 10, 20, 30, 40, 50 });
                Check(b[1], 2, "已领两档 ⇒ Collected(2)（判据 = `.c` 的 `*(int*)(ev+0x70)`，⛔ 不是 ILSpy 印的 0）");
                Check(b[2], 1, "第三档没领 ⇒ Unlocked(1)（只翻「已领数」一个维度 ⇒ 灭自证）");
                CheckTrue(SkirmishEventWindow.HasMilestoneToCollect(null) == false,
                          "`null`（本地无活动数据）⇒ 一档都没解锁 = 不可领");
                CheckTrue(SkirmishEventWindow.HasMilestoneToCollect(new[] { 0, 0, 0 }) == false, "全 Locked ⇒ 不可领");
                CheckTrue(SkirmishEventWindow.HasMilestoneToCollect(new[] { 0, 1, 0 }), "有一档 Unlocked ⇒ 可领");
            }

            // ---- ⑤ `Collect` 钮 interactable 跟着里程碑走（活动期形态才有这颗钮）----
            {
                SkirmishEventWindow.ForceActiveEventForTest = true;
                try
                {
                    SkirmishEventWindow.ForceMilestoneStatesForTest = null;              // 一档都没解锁
                    // 🔴 **2026-10-28（B93_E30 · 按 `B93_DG2` 裁定的「δ 夹具前提」改）**：`Create(null)` + 没有 `Open()`
                    //   ⇒ 树是空的（`CollectHit` 在 `BuildScoreBar` 里，只在 `Build()`/`Open()` 那一拍建，
                    //   `Shell/SkirmishEventWindow.cs:574`）；`Manager` 也必须是那台真的。
                    var skA = SkirmishEventWindow.Create(shell.Windows);
                    CheckTrue(TryOpenForSelfCheck(skA, "SkirmishEventWindow/⑤A"), "（前提）活动期形态的 `Open()` 跑通");
                    var hA = FindChildIn(skA.transform, "CollectHit");
                    var wA = hA != null ? hA.GetComponent<WindowButton>() : null;
                    CheckTrue(wA != null, "（前提）活动期形态建出了 `CollectHit`");
                    CheckTrue(wA != null && !wA.Interactable,
                              "★ 一档都没解锁 ⇒ `Collect` **不可点**（原版 `HasMilestoneToCollect` = false；改坏法：删掉那句 "
                            + "`cw.Interactable = …` ⇒ 这条红）");
                    RewardsWindow.DestroySafe(skA.gameObject);

                    SkirmishEventWindow.ForceMilestoneStatesForTest = new[] { 0, 0, 1, 0, 0 };   // 只翻这一个维度
                    var skB = SkirmishEventWindow.Create(shell.Windows);   // 🆕 B93_E30：同 ⑤A（原来 `Create(null)` + 无 `Open()` ⇒ 树空）
                    CheckTrue(TryOpenForSelfCheck(skB, "SkirmishEventWindow/⑤B"), "（前提）活动期形态的 `Open()` 跑通");
                    var hB = FindChildIn(skB.transform, "CollectHit");
                    var wB = hB != null ? hB.GetComponent<WindowButton>() : null;
                    CheckTrue(wB != null && wB.Interactable,
                              "★ 有一档可领 ⇒ `Collect` 可点（同一条 Build 路径，只翻里程碑状态 ⇒ 灭自证）");
                    RewardsWindow.DestroySafe(skB.gameObject);
                }
                finally { SkirmishEventWindow.ResetEventProbeForTest(); }
            }

            // ---- ⑥ 生存事件「敌方标记」宿主（行为 1:1；本工程还没有生存事件窗 ⇒ 只验行为）----
            {
                var host = new GameObject("B93D1_SurvivalHost").transform;
                var hidden = new GameObject("HiddenEnemyVisual"); hidden.transform.SetParent(host, false);
                var revealed = new GameObject("RevealedEnemyVisual"); revealed.transform.SetParent(host, false);
                var nameLb = MenuDraw.Text(host, new PxRect(0f, 0f, 400f, 60f), "", Color.white,
                                           "EnemyNameText", 36f, 3114, face: TmpFont.Face.LatinCardBody);
                CheckTrue(nameLb != null && hidden != null && revealed != null,
                          "（前提）三个视觉件都建出来了（名字文本 / 隐藏标记 / 已揭示标记）");
                var mk = SurvivalEventWindowEnemyMarker.Bind(nameLb, hidden, revealed);
                CheckTrue(mk.HiddenVisible && !mk.RevealedVisible,
                          "★ `Awake` = 隐藏标记开 / 已揭示标记关（`SurvivalEventWindowEnemyMarker__Awake.c`）");
                revealed.SetActive(true);                       // 先把已揭示标记**手动打开**
                mk.SetHiddenEnemy();
                CheckTrue(!mk.RevealedVisible,
                          "★ `SetHiddenEnemy` 会把**已揭示标记关掉**（灭自证：上一句先置真，漏了那句 SetActive(false) 就红）");
                CheckTrue(mk.HiddenVisible, "…同时把隐藏标记打开");
                mk.SetEnemyName("Hive Tyrant");
                Check(mk.EnemyName, "Hive Tyrant", "★ `SetEnemyName` 写名字（原版 `EnemyNameText.text = name`）");
                CheckTrue(!mk.RevealedVisible,
                          "★ `SetEnemyName` **不碰** `RevealedEnemyVisual`（原版也没碰 ⇒ 别顺手改成「揭示」）");
                CheckTrue(mk.HiddenVisible, "…且隐藏标记仍开着（那句 false→true 是重播动画，不是关掉）");
                RewardsWindow.DestroySafe(host.gameObject);
            }

            // ---- ⑦ 活动阵营图标行（组件已建；接线方 = 能量窗，见报告 §5·2）----
            {
                var pRoot = new GameObject("B93D1_ArmiesHost").transform;
                var panel = ShowEventArmiesPanel.Build(pRoot);
                CheckTrue(FindChildIn(pRoot, "Army Selector Panel") != null,
                          "（前提）`Army Selector Panel` 建出来了（原版这四个静态节点）");
                panel.Initialize(new[] { 10, 20 });             // Ultramarines / Goff
                Check(panel.Count, 2, "★ `Initialize(2 个阵营)` ⇒ 两颗徽记（原版 `foreach cardArmies`）");
                Check(panel.ItemSpriteName(0), "40k_DeckSelection_icon_FactionUM",
                      "第 1 颗 = UM 的徽记（= 原版 `ArmyUtilities.GetArmyIcon(10)`；判据 = `Core/ArmyIcons.cs` 的 13 条表）");
                Check(panel.ItemSpriteName(1), "40k_DeckSelection_icon_FactionOrks", "第 2 颗 = 兽人徽记");
                panel.Initialize(new[] { 10, 20, 30 });
                Check(panel.Count, 3, "★ 再 `Initialize` ⇒ **先 Reset 再加**（Count = 3 而不是 5；改坏法：删掉那句 Reset ⇒ 5）");
                panel.Reset();
                Check(panel.Count, 0, "★ `Reset` ⇒ 清空");
                RewardsWindow.DestroySafe(pRoot.gameObject);
            }
        }

        Debug.Log(P + "B93D4SelfCheck 跑 " + B93D4SelfCheck.Run((ok, msg) => CheckTrue(ok, msg)) + " 条断言");   // 🆕 2026-10-28（B93_D4）：断言本体在 `Editor/ShellScene_B93D4SelfCheck.cs`。打印返回条数 = 自证真被跑到。
        Debug.Log(P + "LiveOpsB93N2SelfCheck 跑 " + LiveOpsB93N2SelfCheck.Run((ok, msg) => CheckTrue(ok, msg)) + " 条断言");
        Debug.Log(P + "B94S2SelfCheck 跑 " + B94S2SelfCheck.Run((ok, msg) => CheckTrue(ok, msg)) + " 条断言");   // 🆕 2026-10-29（B94_S2 · 日常/奖励）：断言本体在 `Editor/ShellScene_B94S2SelfCheck.cs`。接在**合计行之前** ⇒ 其后无账落点（本文件最大账落点 = `ShellScene.cs:10351`）。**打印返回条数 = 自证它真被跑到**。
        B93S3Assert.Run(); _sink.Pass += B93S3Assert.Pass; _sink.Fail += B93S3Assert.Fail; B94Y6_Assert.Run(); _sink.Pass += B94Y6_Assert.Pass; _sink.Fail += B94Y6_Assert.Fail; B94B4Assert.Run((ok, msg) => CheckTrue(ok, msg)); B94B6Assert.Run(); _sink.Pass += B94B6Assert.Pass; _sink.Fail += B94B6Assert.Fail; B94B17Assert.Run(); _sink.Pass += B94B17Assert.Pass; _sink.Fail += B94B17Assert.Fail; B97W2Assert.Run(); _sink.Pass += B97W2Assert.Pass; _sink.Fail += B97W2Assert.Fail; B97_DAILYAssert.Run((ok, msg) => CheckTrue(ok, msg)); B97MISC2Assert.Run(); _sink.Pass += B97MISC2Assert.Pass; _sink.Fail += B97MISC2Assert.Fail; B97_MENU_Assert.RunAll(); B97MINI2Assert.Run(); _sink.Pass += B97MINI2Assert.Pass; _sink.Fail += B97MINI2Assert.Fail; Debug.Log(P + $"=== 合计：{_sink.Pass} 通过 / {_sink.Fail} 失败 ===");   // 🆕 2026-10-29（B97_DAILY · 日常/奖励）：账 `745` 的接线锁（IL 层「方法体恰好一处 call `ModifyLocalization`」）+ `510` 的 `displayRule` 三档；断言本体在 `Editor/B97_DAILY_Assert.cs`。接在**合计行之前**，同一行追加 ⇒ 行数零变化   // 🆕 2026-10-29（调度台代接线）：B94_B4 的日常线断言（GridMissionLayout 7 条 + 信号触发器 6 条 + 周常倒计时 3 条）—— `Run` 自带 sink 回调 ⇒ 直接进 `_sink`   // 🆕 2026-10-29（调度台代接线）：`B93S3Assert`（124 条）· `B94Y6_Assert`（收件箱占位符/ConfigProvider/GameNews）—— 此前全仓 0 宿主，接在合计行之前，**同一行追加 ⇒ 行数零变化**
        // 🔴 **2026-10-11（A350 · 调度台裁定）**：这一串是**失败表的【重列】**（每条失败在
        //   `Check` 里**已经打过一次**）⇒ 行首标记必须是 `失败重列：`、⛔ **不能再是 `✗`** ——
        //   原来是 `✗` 时日志里 `✗` 行数 = 失败数 **×2**，连「按行首标记数」都数不准
        //   （`✗` 还会出现在断言文案里，见 `资料/已知的坑.md` 那条「别用 `grep -c ✗` 数失败」）。
        //   同族四处一起改：`CollectionScene` / `RewardsScene` / `ShopScene`（同一句形状）。
        if (_sink.Fail > 0) foreach (var f in _sink.Failures) Debug.LogError(P + "   失败重列：" + f);
        EditorApplication.Exit(_sink.Fail > 0 ? 1 : 0);
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
        SceneStubAudit.Run(ScenePath);   // 🆕 A1506：存盘后自证「不许有 !u!115 存根」（干净时零输出）
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
        SceneStubAudit.Run(ScenePath);   // 🆕 A1506：存盘后自证「不许有 !u!115 存根」（干净时零输出）
        Debug.Log(P + $"  场景 {ScenePath}");
        // ⚠️ 同上：这一张拍在 `Build` 之后、开场刚起 —— 画面上确实什么都没有
        Shoot("00_外壳.png", allowBlank: true);
        Debug.Log(P + shell.Dump());
    }

    /// <summary>🆕 **2026-10-28（B93_E30）**：夹具开窗的**统一护栏** —— `Open()` 抛异常时**只返回 false**，
    /// ⛔ 不让异常逃出 `RunCore`（那会打断**后面所有批次**，一条都不跑；同一条纪律 = `B93_D1` 那一节里
    /// `B93_E27` 的裁定「⛔ 绝不让 `Find(...)` / `FindChildIn(...)` 的 NRE 逃出 `RunCore`」）。
    /// <para>⚠️ 调用点一律 `CheckTrue(TryOpenForSelfCheck(w, "窗名"), "（前提）…")` —— 红了要说得出「是这一步塌的」，
    /// ⛔ 别写成 `if (w != null) Check(...)`（前提不成立就静默跳过，`资料/已知的坑` 那条同族）。</para></summary>
    static bool TryOpenForSelfCheck(GameWindow w, string who)
    {
        if (w == null) { Debug.LogError(P + "夹具 `" + who + "` 是 null ⇒ 没法 `Open()`"); return false; }
        try { w.Open(); return true; }
        catch (System.Exception e) { Debug.LogError(P + "夹具 `" + who + "`.Open() 抛异常：" + e); return false; }
    }
}
