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

    /// <summary>在子树里按名字找节点（**含 inactive** —— 自检里很多件是关着的）。</summary>
    static Transform FindChildIn(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    // ---------------- 🆕 2026-10-05（A78①）：软边**切线**的扫描器
    //
    // 软边的实现是「按渐隐带的内沿把这块 quad **切开**」（`MenuDraw.ApplySoftEdges`）——
    // 原节点留一格、其余格建**子 quad**（名字 `…_soft<i><j>`）⇒ 「宿主与子块共享的那条边」就是带的内沿。
    // ⇒ **看到切线 = 这条软边真的接上了**；期望值一律写成「视口边 ± 原版 `m_Softness`」的**算式结果**，
    //   ⛔ 不读被测实现里的任何常量（软边留在 0 就一条切线都没有；值写错切线就不在算出来的位置上）。
    // ⚠️ 本工程的自检辅助函数**按 Scene 各留一份**（`CollectionScene` / `RewardsScene` 各有一条同形的）
    //   —— 那三个文件各有各的自检入口，这里不跨文件共享（本文件只多这一份，两处实现只有这一份会被改）。
    static List<float> ScanSoftCuts(Transform root, bool vertical)
    {
        var cuts = new List<float>();
        if (root == null) return cuts;
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
                    if (Mathf.Abs((cc.x - cw) - (hc.x + hw)) < 0.5f) cuts.Add(cc.x - cw);        // 子块在**右** ⇒ 切线 = 子块左沿
                    else if (Mathf.Abs((cc.x + cw) - (hc.x - hw)) < 0.5f) cuts.Add(cc.x + cw);   // 子块在**左** ⇒ 切线 = 子块右沿
                }
                else
                {
                    if (Mathf.Abs((cc.y - ch) - (hc.y + hh)) < 0.5f) cuts.Add(cc.y - ch);        // 子块在**下** ⇒ 切线 = 子块上沿
                    else if (Mathf.Abs((cc.y + ch) - (hc.y - hh)) < 0.5f) cuts.Add(cc.y + ch);   // 子块在**上** ⇒ 切线 = 子块下沿
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

        var btn = popup != null ? popup.GetComponentInChildren<WindowButton>() : null;
        if (btn != null) btn.ClickForTest();
        CheckTrue(okFired, "点确定**回调真的执行了**（不是只关窗）");
        Check(shell.Windows.openWindows.Count, 1, "弹窗关掉之后只剩 1 个窗");

        // ---------------- ⑤b `PromptPopup`（照原版 `GenericPromptWindow` 重做的那个，正本 §七）
        Section("`PromptPopup`（原版 `GenericPromptWindow` prefab 规格）");
        // ⚠️ 2026-10-04 更正：这条示例文案原来写「暂无服务器：多人功能还没接（边界③）」—— **已过期**
        //    （P2P 联机 2026-09-26 就整条打通了，见 `资料/联机P2P_设计与交接.md`）。它只是自检的示例正文、
        //    **不参与任何判据**，但留着会误导 ⇒ 换成一句**不会过期**的事实句。后面那两条断言都是**现算**的
        //    （`Mathf.Max(PromptPopup.MsgMinH, msgLb.WorldH*108f) + BtnRowH`），换文案不影响它们。
        shell.Windows.ShowPopUp("（自检示例文案：本窗只负责排版，正文由调用方给。）", "知道了", null);
        var pp = shell.Windows.popUpWindow as PromptPopup;
        CheckTrue(pp != null, "`ShowPopUp` 开的是 `PromptPopup`（**照原版 prefab 搭的**，不是自建版面）");
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
            //   （`Shell/WindowsManager.cs:292-300`，`localScale = one` 在 `:299`）；而 `extraScaleSmallScreen` 在我们这套里**没有消费者**
            //   （`GameWindow.TryOpen` 只播音→激活→`Open()`，见 `Shell/WindowsManager.cs:76-82`；小屏缩放器没实现）
            //   ⇒ 全链路缩放恒为 1（本窗该值也是 1.0，`PromptPopup.cs:81`）。
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

        // ---------------- ⑤·b 共用件：`RectMask2D.m_Padding`（A9/A15 尾巴，2026-10-04 建模）
        //
        // 🔴 **判据 = 本地 UGUI 源码**：`RectMask2D.m_Padding`（`Runtime/UGUI/UI/Core/RectMask2D.cs:51,60-65`）
        //   **全文件只用在一处** —— `IsRaycastLocationValid`（同文件 `:178-185`）
        //   ⇒ 它**只改「点不点得到」，不改「画到哪儿」**（渲染那一面 `PerformClipping` 压根不读它）。
        //   符号约定（正 = 缩小 / 负 = 扩大）与逐处真值 → `MenuDraw.PaddedHitRect` 上面那一段注释。
        //   期望值全部是**原版 mask 的实读字面量**（全量表 `d:/4/_tmp_view/q1_rm2d.txt`），不是我们自己的常量。
        Section("共用件：`RectMask2D.m_Padding`（只改命中区 · 判据 = UGUI `RectMask2D.IsRaycastLocationValid`）");
        {
            // ① 正值 = 缩小：锻造轨道 `Forge Tab/Rewards Scroll View/Viewport` 的 `m_Padding` 实读 = (10,0,0,0)
            var pr0 = new PxRect(100f, 200f, 300f, 500f);
            var pad1 = MenuDraw.PaddedHitRect(pr0, new Vector4(10f, 0f, 0f, 0f));
            CheckNear(pad1.x1, 110f, 0.001f, "`m_Padding=(10,0,0,0)`（锻造轨道原版值）⇒ 命中区**左边收进 10px**");
            CheckTrue(Mathf.Abs(pad1.x2 - 300f) < 0.001f && Mathf.Abs(pad1.y1 - 200f) < 0.001f
                      && Mathf.Abs(pad1.y2 - 500f) < 0.001f, "…其余三边**一动不动**");
            // ② 负值 = 扩大：战役轨道 / 战役阵营条原版 `m_Padding` 实读 = (−8,−5,−8,−5)（(L,B,R,T)）
            var pad2 = MenuDraw.PaddedHitRect(pr0, new Vector4(-8f, -5f, -8f, -5f));
            CheckNear(pad2.x1, 92f, 0.001f, "`m_Padding=(−8,−5,−8,−5)`（战役轨道原版值）⇒ 左右各**外扩 8**");
            CheckNear(pad2.y1, 195f, 0.001f, "…上边外扩 5（UNITY 的 `w`=Top）");
            CheckNear(pad2.y2, 505f, 0.001f, "…下边外扩 5（`y`=Bottom；`PxRect` 是**y 向下**，所以落在 y2 上）");
            // ③ 端到端：`MenuDraw.Hit` 真的吃这一份（拿掉 pad 的转发它就红）—— 按真实用法带一个 `clip`
            var padGo = new GameObject("PaddingProbe");
            var padHit = MenuDraw.Hit(padGo.transform, "PadHit", new PxRect(0f, 0f, 100f, 100f), 3000,
                                      () => { }, null, null, null, null, new PxRect(0f, 0f, 200f, 200f),
                                      new Vector4(10f, 20f, 5f, 4f));
            var padQ = padHit != null ? padHit.GetComponentInChildren<ImageQuad>() : null;
            CheckTrue(padQ != null, "带 pad 的命中区**建出来了**（`PointerLayer` 只认 quad —— 裸节点点不动）");
            if (padQ != null)
            {
                CheckNear(padQ.WorldW * 108f, 85f, 0.5f, "命中 quad 宽 = 100 − 左 10 − 右 5 = **85**（pad 真的进了命中区）");
                CheckNear(padQ.WorldH * 108f, 76f, 0.5f, "命中 quad 高 = 100 − 上 4 − 下 20 = **76**");
            }
            Object.DestroyImmediate(padGo);
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

        // ---------------- ⑤·e 🆕 A49：键盘导航（ESC 关当前窗 · 方向键选 · 回车确认）
        //
        // 🔴 **判据 = 本地 UGUI 源码 + 原版反编译**（行号、原文与「哪几条是我们挑的」→
        //    `Shell/PointerLayer.cs` 的「键盘导航（A49）」那一节，别在这里抄第二份）：
        //    · 方向键/回车 = `StandaloneInputModule.Process()` 那三跳 + 它的两个节流值
        //      （`m_RepeatDelay = 0.5` · `m_InputActionsPerSecond = 10`）。**原版真的跑这一套**：
        //      它的输入模块 `EverguildInput` 是 `StandaloneInputModule` 的子类，`Process()` 第一句就是 `base.Process()`。
        //    · ESC = 原版 `WindowsManager.Update`（打给**最上面那扇窗**）+ `GameWindow.ESCPressed` 里
        //      那句 `if (closeOnESC(0x39) == 0) return;`。
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
            Check(PointerLayer.ButtonCountForTest, 5,
                  "前置：此刻场上**只有探针这 5 颗**可导航按钮（否则「第一颗 / 正下方那颗」都不唯一、下面几条等于没查）");
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

            var popGo = new GameObject("KeyProbePopup");
            var pop = popGo.AddComponent<GameWindow>();
            pop.type = WindowType.Popup;
            pop.closeOnEsc = true;
            pop.placement = WindowsPlacement.Popup;
            WindowsManager.AttachToAnchor(pop);
            shell.Windows.OpenWindow(pop);
            Check(shell.Windows.TopWindow, pop,
                  "`TopWindow` = 最后开的那扇（= 原版 `currentWindow` 的等价物：原版 `OpenWindowCO` 对弹窗也会 `set_CurrentWindow`）");
            CheckTrue(pl.KeyCancel(), "★ 叠了一扇之后，ESC 关的是**最上面**那扇");
            Check(shell.Windows.openWindows.Count, 1, "…底下那扇**还在**（ESC 只吃最上面一层）");
            Check(probe.CurrentState, WindowState.Background, "…它被压到背景态（不是被关掉）");

            shell.Windows.CloseAllWindows();
            CheckTrue(!pl.KeyCancel(),
                      "★ 一扇窗都没有时 ESC 什么都不做（原版 `WindowsManager.Update` 在 `currentWindow == null` 时直接 return）");

            Object.DestroyImmediate(popGo);
            Object.DestroyImmediate(probeGo);
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
            //    档照公共件的规矩传「压暗档 2900 / 内容档 3000」⇒ 它自己算成 2999（不触发 `AbsorbTierWarns`）。
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

        // ---------------- ⑤·f 🆕 A78①：非 `MenuWindowBase` 族怎么够到软边 —— 聊天窗 `Chat Tab/Viewport` (0,22)
        //
        // 🔴 **待办原来那句「给它加一行 `ClipSoftness`」是接不上的**：`ChatPanel : GameWindowWithTabs
        //   : GameWindow : MonoBehaviour`，而 `Clip` / `ClipSoftness` / `ClipPad` 三兄弟**只长在
        //   `MenuWindowBase` 上**（按**名字**找那三个字段 —— ⛔ 别写行号，它会过期）⇒ 本窗既没有那个字段、也没处设。
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
            CheckSoftCuts(ScanSoftCuts(chatContent, false), new[] { 183f, 889f }, 0.6f,
                          "聊天页的软边（原版 `m_Softness = (0,22)` ⇒ 带内沿 **161+22=183** / **911−22=889**）");
            Check(ScanSoftCuts(chatContent, true).Count, 0,
                  "…而**一条竖切线都没有** —— `(0,22)` 的 `x = 0` ⇒ 左右是硬边（原版只渐变上下）");
            Check(MenuDraw.SoftEdgeUvDrifts, 0,
                  "…整页 `_soft` 子块的 uv 面积和恒等于「整张图」（`SoftEdgeUvDrifts` 没涨）");
            // 收尾：清数据 + 关窗（它的压暗层是整屏的，留着会顶掉后面那些真命中路）
            SocialData.ChatMessages.Clear();
            chatW.RefreshMessages();
            chatW.Close();
            Check(chatW.CurrentState, WindowState.Closed, "收尾：聊天窗关掉");
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

        Debug.Log(P + shell.Dump());
        Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
        if (_fail > 0) foreach (var f in _failures) Debug.LogError(P + "   ✗ " + f);
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
