// EffectSweepBatch.cs — 批量时序采样：把一批效果在多个时间点各渲一次，只为拿到数字
//
// 为什么需要它
// ------------
// EffectCompare 只在 Simulate(1.2s) 采一帧。于是有 280 个效果（占 29%）「两边都是空的」——
// 那不是还原失败，是采样点不对（闪电/子弹命中这类瞬时特效 1.2s 早播完了）。
// **拿着一把不准的尺子逐个量，量出来的清单是错的。** 这个工具就是把尺子修好：
// 每个效果在 8 个时间点各渲一次，原版/导出各记「亮点数」和「亮度和」。
//
// 关键：两趟分开跑，不能合并
// --------------------------
// 原版那侧要从 battleprefabs 包里取 prefab，且**必须加载全部 84 个包**才能解析到原版 shader
// （只加载它一个包时原版渲染成品红，品红很亮 → 比值假性掉到 0.2）。
// 导出那侧**不能加载任何源 bundle** —— wf_shaders_extra.bundle 正是从 battleprefabs 抽出来的，
// 一旦 battleprefabs 在场，Unity 按「内容相同」判定重复，补充包加载不了，导出侧退回占位材质。
// 两边的条件互相冲突，所以必须分两个进程跑，把结果按效果名合并（见 工具/analyze_sweep.py）。
//
// Side="orig"：加载全部源 bundle，渲染原版，并把每个效果的取景缓存到 sweep_frames.tsv
// Side="exp" ：不加载源 bundle（= 真实运行时条件），读取景缓存，渲染导出
//
// ⚠️ 这两个坑都是实测踩出来的，两个方向都会把 900+ 个效果误判成「严重偏暗」。
//
// 用法（两趟都要跑，顺序不能反 —— exp 依赖 orig 产出的取景缓存）：
//   Unity.exe -batchmode -quit -projectPath ... -executeMethod EffectSweepBatch.Run -logFile -
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EffectSweepBatch
{
    // 要扫哪一侧。两趟的条件互相冲突，必须分进程跑（详见文件头）：
    //   orig：加载全部源 bundle（否则原版渲染成品红）→ 顺带产出取景缓存
    //   exp ：不加载源 bundle（否则补充 shader 包被顶掉）→ 读 orig 留下的取景缓存
    // **顺序不能反**：exp 依赖 orig 产出的 sweep_frames.tsv。
    static readonly string Side = "exp";     // "exp" | "orig"

    const string P = "WFSWEEP ";

    /// <summary>结果按侧分文件：sweep_orig.tsv / sweep_exp.tsv，最后在 Python 里按效果名合并。
    /// 放在 `资料/比对基线/` 而不是临时目录 —— 这是台账的原始数据，要跟着仓库走。</summary>
    static string ResultPath { get { return $@"d:\4\Unity\资料\比对基线\sweep_{Side}.tsv"; } }

    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string PrefabDir = "Assets/WarpforgeVFX/Prefabs";
    const string OutDir = @"d:\4\_tmp_view\sweep_batch";
    const string FrameCachePath = @"d:\4\Unity\资料\比对基线\sweep_frames.tsv";

    // 要扫的效果：每行一个效果名。文件不存在时用 NameFilter（子串匹配），
    // 两个都空 = 全量 958 个（很久，慎用）
    const string TargetFile = @"d:\4\_tmp_view\sweep_targets.txt";
    static readonly string[] NameFilter = { };

    static readonly float[] Times = { 0.15f, 0.3f, 0.5f, 0.75f, 1.0f, 1.5f, 2.0f, 3.0f };

    const int W = 256, H = 256;

    // 是否同时落 PNG。排查「数字和肉眼看的不一致」时必须打开：
    // tex.GetPixels() 拿到的是**线性**值，而 EncodeToPNG 写的是 sRGB ——
    // 两边亮度差 a 倍时，线性口径下的比值会变成 a^2.2（0.5 → 0.22）。
    // 只记数字很容易把这种口径差当成真缺陷。
    static readonly bool WritePng = false;

    static readonly Color Bg = new Color(0.07f, 0.08f, 0.10f, 1f);

    public static void Run()
    {
        Debug.Log(P + $"=== 批量时序采样 开始（Side={Side}）===");
        Directory.CreateDirectory(OutDir);
        bool doOrig = Side == "orig";

        // 取景必须两边一致，否则亮度差没有可比性（踩过）。但导出侧那趟**不能**加载源 bundle，
        // 拿不到原版 prefab 去量包围盒 —— 所以由原版那趟把每个效果的取景算好缓存下来，
        // 导出侧直接读缓存。
        var frames = LoadFrames();

        Dictionary<string, GameObject> originals = null;
        if (doOrig)
        {
            // ⚠️ 原版这趟必须**加载全部 84 个包**。
            //    原版 prefab 的材质同样要从包里解析 shader，而 battleprefabs 里的 shader
            //    并不是可加载资产（LoadAllAssets<Shader>() 返回 0），只加载它一个包时
            //    解析不到 → 原版渲染成**品红** → 品红很亮 → 原版亮度和虚高 →
            //    「导出/原版」比值假性掉到 0.2，看起来像一大片「严重偏暗」。
            //    实测踩过：先渲染出品红才发现。
            AssetBundle vfx = null;
            foreach (var f in Directory.GetFiles(BundleDir, "*.bundle"))
            {
                var b = AssetBundle.LoadFromFile(f);
                if (b != null && Path.GetFileName(f) == "battleprefabs_vfxandmisc_assets_all.bundle") vfx = b;
            }
            if (vfx == null) { Debug.LogError(P + "特效 bundle 未加载"); return; }

            originals = new Dictionary<string, GameObject>();
            foreach (var n in vfx.GetAllAssetNames())
            {
                GameObject g = null;
                try { g = vfx.LoadAsset<GameObject>(n); } catch { }
                if (g != null) originals[g.name] = g;
            }
            Debug.Log(P + $"原版效果 {originals.Count} 个（已加载全部源 bundle）");
        }
        else if (frames.Count == 0)
        {
            Debug.LogError(P + "没有取景缓存 —— 必须先跑一趟 Side=\"orig\" 生成 " + FrameCachePath);
            return;
        }

        // ---- 目标清单 ----
        var targets = new List<string>();
        if (File.Exists(TargetFile))
        {
            foreach (var l in File.ReadAllLines(TargetFile))
            {
                var s = l.Trim();
                if (s.Length > 0 && !s.StartsWith("#")) targets.Add(s);
            }
            Debug.Log(P + $"从 {TargetFile} 读到 {targets.Count} 个目标");
        }
        else
        {
            var all = Directory.GetFiles(PrefabDir, "*.prefab")
                               .Select(Path.GetFileNameWithoutExtension).OrderBy(x => x).ToList();
            targets = NameFilter.Length > 0
                ? all.Where(x => NameFilter.Any(k => x.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)).ToList()
                : all;
            Debug.Log(P + $"没有目标文件，改用 NameFilter，命中 {targets.Count} 个");
        }

        int shaderN = WarpforgeVFX.WarpforgeShaderLoader.ShaderNames.Count();
        Debug.Log(P + $"原版 shader {shaderN} 个可用");

        // ---- 一个场景一个相机，复用到底 ----
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Cam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Bg;

        var sb = new StringBuilder();
        sb.AppendLine("effect\ttime\tlit\tsum\tlit_srgb\tsum_srgb");

        int done = 0, skipped = 0;
        foreach (var name in targets)
        {
            GameObject src = null;
            if (doOrig)
            {
                if (!originals.TryGetValue(name, out src)) { Debug.LogWarning(P + $"原版里没有 {name}"); skipped++; continue; }
            }
            else
            {
                src = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");
                if (src == null) { Debug.LogWarning(P + $"导出里没有 {name}"); skipped++; continue; }
            }

            CamFrame frame;
            if (doOrig)
            {
                frame = FrameCamera(src);
                frames[name] = frame;          // 缓存给导出那趟用
            }
            else if (!frames.TryGetValue(name, out frame))
            {
                Debug.LogWarning(P + $"取景缓存里没有 {name}"); skipped++; continue;
            }
            ApplyFrame(cam, frame);

            var brief = new List<string>();
            foreach (var t in Times)
            {
                var png = WritePng ? $"{OutDir}/{Sanitize(name)}_{t:F2}__{Side}.png" : null;
                var (lit, sum, litS, sumS) = RenderAt(src, t, cam, png);
                sb.AppendLine($"{name}\t{t.ToString("F2", CultureInfo.InvariantCulture)}\t{lit}\t{sum:F0}\t{litS}\t{sumS:F0}");
                brief.Add($"{t:F2}:{litS}");
            }
            done++;
            // 每 20 个刷一次盘，随时可中断
            if (done % 20 == 0)
            {
                File.WriteAllText(ResultPath, sb.ToString());
                if (doOrig) SaveFrames(frames);
            }
        }

        File.WriteAllText(ResultPath, sb.ToString());
        if (doOrig) SaveFrames(frames);
        Debug.Log(P + $"=== 结束：成功 {done}，跳过 {skipped}，结果 {ResultPath} ===");
    }

    // ---- 取景缓存（原版那趟写，导出那趟读）----
    static Dictionary<string, CamFrame> LoadFrames()
    {
        var d = new Dictionary<string, CamFrame>();
        if (!File.Exists(FrameCachePath)) return d;
        foreach (var l in File.ReadLines(FrameCachePath).Skip(1))
        {
            var p = l.Split('\t');
            if (p.Length < 11) continue;
            d[p[0]] = new CamFrame
            {
                pos = new Vector3(float.Parse(p[1], CultureInfo.InvariantCulture),
                                  float.Parse(p[2], CultureInfo.InvariantCulture),
                                  float.Parse(p[3], CultureInfo.InvariantCulture)),
                look = new Vector3(float.Parse(p[4], CultureInfo.InvariantCulture),
                                   float.Parse(p[5], CultureInfo.InvariantCulture),
                                   float.Parse(p[6], CultureInfo.InvariantCulture)),
                fov = float.Parse(p[7], CultureInfo.InvariantCulture),
                near = float.Parse(p[8], CultureInfo.InvariantCulture),
                far = float.Parse(p[9], CultureInfo.InvariantCulture),
            };
        }
        return d;
    }

    static void SaveFrames(Dictionary<string, CamFrame> frames)
    {
        var sb = new StringBuilder();
        // 第 10 列留给将来扩展；CamFrame 是 9 个字段，读的时候按 >=11 判断会漏，
        // 所以这里补一列占位，保持列数一致
        sb.AppendLine("effect\tpx\tpy\tpz\tlx\tly\tlz\tfov\tnear\tfar\t_");
        foreach (var kv in frames)
        {
            var f = kv.Value;
            sb.AppendLine(string.Join("\t", new[] {
                kv.Key,
                f.pos.x.ToString("R", CultureInfo.InvariantCulture), f.pos.y.ToString("R", CultureInfo.InvariantCulture), f.pos.z.ToString("R", CultureInfo.InvariantCulture),
                f.look.x.ToString("R", CultureInfo.InvariantCulture), f.look.y.ToString("R", CultureInfo.InvariantCulture), f.look.z.ToString("R", CultureInfo.InvariantCulture),
                f.fov.ToString("R", CultureInfo.InvariantCulture), f.near.ToString("R", CultureInfo.InvariantCulture), f.far.ToString("R", CultureInfo.InvariantCulture),
                "0",
            }));
        }
        File.WriteAllText(FrameCachePath, sb.ToString());
    }

    static void ApplyFrame(Camera cam, CamFrame f)
    {
        cam.fieldOfView = f.fov;
        cam.nearClipPlane = f.near;
        cam.farClipPlane = f.far;
        cam.transform.position = f.pos;
        cam.transform.LookAt(f.look);
    }

    /// <summary>渲染一帧，返回（亮点数, 亮度和）—— **原始值** 和 **按 sRGB 编码后的值** 各一份。
    ///
    /// 为什么要两份：`tex.GetPixels()` 拿到的是**线性**值（ARGB32 的 RenderTexture 在
    /// 线性色彩空间工程里就是线性的），而人眼和 PNG 看到的是 sRGB。两边亮度差 a 倍时，
    /// 线性口径下比值会变成 a^2.2 —— 0.5 的差看起来像 0.22，会被误判成「严重偏暗」。
    /// 实测踩过：957 个效果里 749 个被误判成「亮度/密度不对」。</summary>
    static (int lit, double sum, int litS, double sumS) RenderAt(GameObject prefab, float time, Camera cam, string pngPath)
    {
        var inst = UnityEngine.Object.Instantiate(prefab);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;

        // 编辑器里 Awake 不跑，手动触发 binder
        var binder = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();

        foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Simulate(time, withChildren: true, restart: true, fixedTimeStep: false);
            ps.Play();
        }

        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;

        if (pngPath != null) File.WriteAllBytes(pngPath, tex.EncodeToPNG());

        var px = tex.GetPixels();
        int lit = 0, litS = 0; double sum = 0, sumS = 0;
        const float thr = 6f / 255f * 3f;
        foreach (var c in px)
        {
            if (Mathf.Abs(c.r - Bg.r) + Mathf.Abs(c.g - Bg.g) + Mathf.Abs(c.b - Bg.b) > thr)
            { lit++; sum += c.r + c.g + c.b; }

            float r = ToSrgb(c.r), g = ToSrgb(c.g), b = ToSrgb(c.b);
            if (Mathf.Abs(r - Bg.r) + Mathf.Abs(g - Bg.g) + Mathf.Abs(b - Bg.b) > thr)
            { litS++; sumS += r + g + b; }
        }

        UnityEngine.Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        UnityEngine.Object.DestroyImmediate(inst);
        return (lit, sum, litS, sumS);
    }

    /// <summary>线性 → sRGB</summary>
    static float ToSrgb(float c)
    {
        if (c <= 0f) return 0f;
        return c <= 0.0031308f ? c * 12.92f : 1.055f * Mathf.Pow(c, 1f / 2.4f) - 0.055f;
    }

    static string Sanitize(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace('/', '_').Trim();
    }

    struct CamFrame { public Vector3 pos, look; public float fov, near, far; }

    /// <summary>取景用「整个时间轴上的最大包围盒」，保证所有采样时刻都框得住。
    /// 原版/导出共用同一个取景 —— 各自构图会让亮度差失去可比性（踩过）</summary>
    static CamFrame FrameCamera(GameObject prefab)
    {
        var tmp = UnityEngine.Object.Instantiate(prefab);
        tmp.transform.position = Vector3.zero;
        tmp.transform.rotation = Quaternion.identity;

        Bounds b = new Bounds(Vector3.zero, Vector3.one); bool first = true;
        foreach (var t in Times)
        {
            foreach (var ps in tmp.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Simulate(t, withChildren: true, restart: true, fixedTimeStep: false);
                ps.Play();
            }
            foreach (var r in tmp.GetComponentsInChildren<Renderer>(true))
            {
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
        }
        float radius = Mathf.Max(0.5f, b.extents.magnitude);
        const float fov = 40f;
        float dist = radius / Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f) * 1.15f;
        var f = new CamFrame
        {
            look = b.center,
            pos = b.center + new Vector3(0f, 0f, -dist),
            fov = fov,
            near = 0.01f,
            far = radius * 100f,
        };
        UnityEngine.Object.DestroyImmediate(tmp);
        return f;
    }
}
