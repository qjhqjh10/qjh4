// BuiltinShaderProbe.cs — 「内置管线那批原版 shader 到底渲不渲得出」（2026-09-19）
//
// **为什么要它**：这 8 个名（`Mobile/Particles/*` · `Legacy Shaders/Particles/*` ·
// `Particles/Standard Unlit`）一直用自建 shader 近似替代，理由是「Built-in 老 shader 在 URP
// 工程里渲染不了」—— 而那**条论断从没实测过**（`项目任务.md` ⛔ 行 ④ 原文就写着
// 「可验的一条路 = 直接挂一次看渲不渲得出（约半小时）」）。
// 2026-09-19 把原版包里的原件（`wf_builtin.bundle`，构建版本 6000.2.6f2）拉进了工程
// ⇒ 现在必须回答三个问题：
//   ① 原件拿得到吗？（`Shader.Find` 拿到的是**引擎自带**的那一份，不是原件 —— 要显式分开量）
//   ② 原件挂上去**渲得出内容**吗？（不是洋红、不是空）
//   ③ 原件 vs 自建替代，渲出来差多少？（这条只打印 —— 差异要靠全量 sweep 才定得下来，
//      而且台账的判定级噪声底是「E ±1、|ln| ≲ 0.6 分不出真假」，单帧比值不当断言）
//
// **做法**（照 `MatIsoProbe` 那套取景/读数，尺子先自己验一遍）：
//   512×512 · **中灰底**（不能近黑 —— 黑方块和「压根没渲」会混在一起）·
//   一块铺满的四边形（**顶点色显式写白**：这批 shader 里有两个天然要乘两次顶点色，
//   不写白会把「乘法正确」读成「渲出来是黑的」，那是尺子的假象）·
//   一张棋盘贴图（有内容才看得出渲没渲）· 材质属性一次灌全（`_MainTex`/`_Color`/`_TintColor`/`_InvFade`，
//   谁认谁取）· renderQueue=3000。
//
// **判据（断言）**：
//   · 对照组（`URP/Particles/Unlit`）必须 lit>0 且洋红<5% ⇒ **尺子自己先站住**
//   · 每个「bundle 里有原件」的名字：**原件那次 lit > 0**、洋红占比 < 5%
//   · 打印「原件 sum / 自建 sum」，**不设阈值**（差异归全量 sweep 判）
//
// 用法：
//   unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod BuiltinShaderProbe.Run -logFile "d:/4/_tmp_view/builtin_shader.log"
//   筛输出：grep "^BSP " d:/4/_tmp_view/builtin_shader.log
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WarpforgeVFX;

public static class BuiltinShaderProbe
{
    const string P = "BSP ";
    const int W = 512, H = 512;

    /// <summary>中灰底 —— 「没渲出来」和「渲成黑块」必须能分开</summary>
    static readonly Color Bg = new Color(0.5f, 0.5f, 0.5f, 1f);

    /// <summary>点名的 8 个内置管线名（`Particles/Additive` 不在里面：它是死条目，包里也没有）</summary>
    static readonly string[] LegacyNames =
    {
        "Mobile/Particles/Additive",
        "Mobile/Particles/Alpha Blended",
        "Mobile/Particles/Multiply",
        "Particles/Standard Unlit",
        "Legacy Shaders/Particles/Additive",
        "Legacy Shaders/Particles/Alpha Blended",
        "Legacy Shaders/Particles/Alpha Blended Premultiply",
        "Legacy Shaders/Particles/Anim Alpha Blended",
    };

    /// <summary>对照：工程自带的 URP 粒子 shader（已知能渲、已知是对的）—— 它不过就说明**尺子坏了**</summary>
    const string ControlName = "Universal Render Pipeline/Particles/Unlit";

    static Camera _cam;
    static GameObject _quad;
    static Material _mat;
    static Texture2D _checker;

    struct Reading
    {
        public bool got;        // shader 拿得到吗
        public string shaderName;
        public int lit;         // 与背景不同的像素数
        public double sum;      // 这些像素的亮度和
        public double magenta;  // lit 里洋红占的比例
    }

    public static void Run()
    {
        int pass = 0, fail = 0;
        var problems = new List<string>();

        try
        {
            BuildRig();

            // ── ① 尺子先自验：对照组 ──────────────────────────────────────────
            var ctl = Render(Shader.Find(ControlName));
            Log("对照 " + ControlName + " → " + Fmt(ctl));
            if (!(ctl.got && ctl.lit > 0 && ctl.magenta < 0.05))
            {
                fail++;
                problems.Add("对照组没渲出来（尺子坏了，下面的结论都不可信）");
            }
            else pass++;

            // ── ② 逐个名字：原件 vs 自建 ───────────────────────────────────
            Debug.Log(P + "名称 ‖ 原件(bundle) ‖ 自建替代 ‖ 原件sum/自建sum");
            foreach (var n in LegacyNames)
            {
                Shader orig;
                bool hasOrig = WarpforgeShaderLoader.TryGetShader(n, out orig) && orig != null;
                var rOrig = hasOrig ? Render(orig) : default(Reading);

                Shader mine = null;
                if (WarpforgeShaderMap.Replacements.TryGetValue(n, out var mineName))
                    mine = Shader.Find(mineName);
                var rMine = (mine != null) ? Render(mine) : default(Reading);

                // 现状解析（改完顺序之后应当 = 原件）
                Shader cur; string src;
                bool curOk = WarpforgeShaderMap.TryResolve(n, out cur, out src);

                string ratio = (rOrig.sum > 0 && rMine.sum > 0)
                    ? (rOrig.sum / rMine.sum).ToString("0.###") : "n/a";
                Debug.Log(P + n + " ‖ 原件 " + (hasOrig ? Fmt(rOrig) : "**bundle 里没有**")
                          + " ‖ 自建 " + (mine != null ? Fmt(rMine) : "**没有映射**")
                          + " ‖ " + ratio
                          + " ‖ 现状解析=" + (curOk ? src : "**解析不到**"));

                // 断言：包里有原件的，原件必须**渲得出内容**、且不是洋红
                if (hasOrig)
                {
                    if (rOrig.lit > 0 && rOrig.magenta < 0.05) pass++;
                    else
                    {
                        fail++;
                        problems.Add(n + " 的原件没渲出来（lit=" + rOrig.lit
                                      + " 洋红=" + (rOrig.magenta * 100).ToString("0.0") + "%）");
                    }
                }
                // 断言：现状解析必须走到「原版bundle」（白名单生效）
                if (!(curOk && src != null && src.StartsWith("原版bundle")))
                {
                    fail++;
                    problems.Add(n + " 现状没走成原件（" + (curOk ? src : "解析不到") + "）");
                }
            }
        }
        finally { Teardown(); }

        Debug.Log(P + "合计：通过 " + pass + " / 失败 " + fail);
        foreach (var s in problems) Debug.Log(P + "失败项：" + s);
        Debug.Log(P + (fail == 0 ? "=== 通过 ===" : "=== 不通过 ==="));
        if (Application.isBatchMode) EditorApplication.Exit(fail == 0 ? 0 : 1);
    }

    static string Fmt(Reading r)
    {
        if (!r.got) return "**拿不到**";
        return "lit=" + r.lit + " sum=" + r.sum.ToString("0") +
               " 洋红=" + (r.magenta * 100).ToString("0.0") + "%";
    }

    /// <summary>诊断：把 `wf_builtin.bundle` 直接加载，逐个打印里面 Shader 的**真实名字**。
    ///
    /// 为什么要它：`WarpforgeShaderLoader` 是**按名字**建索引的（`LoadShadersFrom` 里
    /// `if (string.IsNullOrEmpty(s.name)) continue;`）。如果包里这批的 `name` 是空的
    /// （**解包侧实测：原版 Shader 的真名在 `m_ParsedForm.m_Name`，顶层 `m_Name` 是空的**），
    /// 它们就会被**静默跳过** —— 表现正是「包加载成功、但按名字查不到」。</summary>
    public static void Diagnose()
    {
        // 二分实验用：`BSP_DIR` 指向一个装满小包的目录时，逐个加载（**崩了也还剩前面的输出**，
        // 所以每加载一个之前先把它打印出来）。不设这个变量就只查正式的那个包。
        var dir = Environment.GetEnvironmentVariable("BSP_DIR");
        if (!string.IsNullOrEmpty(dir) && System.IO.Directory.Exists(dir))
        {
            var files = new List<string>(System.IO.Directory.GetFiles(dir, "*.bundle"));
            files.Sort(StringComparer.Ordinal);
            Log("诊断：逐包加载 " + files.Count + " 个（目录 " + dir + "）");
            foreach (var f in files)
            {
                Log("诊断：── 正在加载 " + System.IO.Path.GetFileName(f));
                var bb = AssetBundle.LoadFromFile(f);
                if (bb == null) { Log("诊断：    **包加载不进来**"); continue; }
                Shader[] ss = null;
                try { ss = bb.LoadAllAssets<Shader>(); }
                catch (Exception e) { Log("诊断：    抛了 " + e.GetType().Name + ": " + e.Message); }
                Log("诊断：    载入 " + (ss == null ? -1 : ss.Length) + " 个 shader："
                    + string.Join(" / ", Array.ConvertAll(ss ?? new Shader[0],
                        x => x == null ? "<null>" : (string.IsNullOrEmpty(x.name) ? "<空名>" : x.name))));

                // 材质那一路：材质会**带出它引用的 shader** —— 用来判「是 shader 本身加载就崩，
                // 还是只有被容器直接点名才崩」
                Material[] ms = null;
                try { ms = bb.LoadAllAssets<Material>(); }
                catch (Exception e) { Log("诊断：    材质抛了 " + e.GetType().Name + ": " + e.Message); }
                if (ms != null)
                    foreach (var m in ms)
                    {
                        string shName = "<取不到>";
                        try { shName = m == null || m.shader == null ? "<null shader>" : m.shader.name; }
                        catch (Exception e) { shName = "<抛 " + e.GetType().Name + ">"; }
                        Log("诊断：    材质 " + (m == null ? "<null>" : m.name) + " → shader " + shName);
                    }
                bb.Unload(false);
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
            return;
        }

        var path = System.IO.Path.Combine(Application.streamingAssetsPath,
                                          WarpforgeShaderLoader.BuiltinBundleRelPath);
        Log("诊断：走 " + path + "（存在=" + System.IO.File.Exists(path) + "）");
        var b = AssetBundle.LoadFromFile(path);
        if (b == null) { Log("诊断：**包加载不进来**"); if (Application.isBatchMode) EditorApplication.Exit(1); return; }

        Shader[] sh = null;
        try { sh = b.LoadAllAssets<Shader>(); }
        catch (Exception e) { Log("诊断：LoadAllAssets<Shader> 抛了 " + e.GetType().Name + ": " + e.Message); }
        int n = sh == null ? -1 : sh.Length;
        Log("诊断：LoadAllAssets<Shader>() = " + n + " 个");
        if (sh != null)
            foreach (var s in sh)
                Log("诊断：  name=「" + (s == null ? "<null 对象>" : (s.name ?? "<null>")) + "」");

        int inLoader = 0;
        foreach (var s in sh ?? new Shader[0])
            if (s != null && WarpforgeShaderLoader.TryGetShader(s.name ?? "", out var got) && got == s) inLoader++;
        Log("诊断：其中能被加载器**按名字**查回来的 = " + inLoader + " / " + n);

        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    static void Log(string s) { Debug.Log(P + s); }

    // ── 渲染 ────────────────────────────────────────────────────────────────

    static Reading Render(Shader sh)
    {
        var r = new Reading();
        if (sh == null) return r;
        r.got = true;
        r.shaderName = sh.name;

        _mat.shader = sh;
        // 材质属性一次灌全：谁认谁取（这两批 shader 用的名字不一样）
        _mat.SetTexture("_MainTex", _checker);
        _mat.SetTexture("_BaseMap", _checker);
        _mat.SetColor("_Color", new Color(1f, 0.6f, 0.35f, 1f));
        _mat.SetColor("_TintColor", new Color(1f, 0.6f, 0.35f, 0.6f));
        _mat.SetFloat("_InvFade", 1f);
        _mat.renderQueue = 3000;

        _quad.GetComponent<Renderer>().sharedMaterial = _mat;

        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
        _cam.targetTexture = rt;
        _cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        _cam.targetTexture = null;

        var px = tex.GetPixels();
        const float thr = 6f / 255f * 3f;
        int magenta = 0;
        foreach (var c in px)
        {
            if (Mathf.Abs(c.r - Bg.r) + Mathf.Abs(c.g - Bg.g) + Mathf.Abs(c.b - Bg.b) <= thr) continue;
            r.lit++;
            r.sum += c.r + c.g + c.b;
            if (c.r > 0.8f && c.b > 0.8f && c.g < 0.2f) magenta++;
        }
        r.magenta = r.lit > 0 ? (double)magenta / r.lit : 0;

        UnityEngine.Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        return r;
    }

    // ── 台子 ────────────────────────────────────────────────────────────────

    static void BuildRig()
    {
        // 相机：正交、中灰底
        var camGo = new GameObject("BSP_Cam");
        _cam = camGo.AddComponent<Camera>();
        _cam.orthographic = true;
        _cam.orthographicSize = 0.5f;
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = Bg;
        _cam.nearClipPlane = 0.01f;
        _cam.farClipPlane = 10f;
        camGo.transform.position = new Vector3(0f, 0f, -2f);
        camGo.transform.rotation = Quaternion.identity;

        // 四边形：手搓 mesh（不用 CreatePrimitive，省得依赖 physics 模块）
        // **顶点色显式写白** —— `Legacy Shaders/Particles/*` 有两个天然乘两次顶点色，
        // 不写白会把「乘法正确」读成「渲出来是黑的」（那就是尺子的假象）
        var mesh = new Mesh();
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),   new Vector3(-0.5f, 0.5f, 0f),
        };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
        mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };

        _quad = new GameObject("BSP_Quad");
        _quad.AddComponent<MeshFilter>().sharedMesh = mesh;
        _quad.AddComponent<MeshRenderer>();

        _mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        _quad.GetComponent<Renderer>().sharedMaterial = _mat;

        // 棋盘贴图（64×64，亮/暗各半）
        _checker = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var cp = new Color32[64 * 64];
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                cp[y * 64 + x] = ((x / 8 + y / 8) % 2 == 0)
                    ? new Color32(255, 255, 255, 255) : new Color32(64, 64, 64, 255);
        _checker.SetPixels32(cp);
        _checker.Apply();
    }

    static void Teardown()
    {
        if (_quad != null) UnityEngine.Object.DestroyImmediate(_quad);
        if (_cam != null) UnityEngine.Object.DestroyImmediate(_cam.gameObject);
        if (_mat != null) UnityEngine.Object.DestroyImmediate(_mat);
        if (_checker != null) UnityEngine.Object.DestroyImmediate(_checker);
    }
}
