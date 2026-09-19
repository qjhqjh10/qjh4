// EffectCompare.cs — 把「原版 bundle 里的特效」和「导出的本地预制」在同一时间点渲染出来做并排比对
// 用法：Unity.exe -batchmode -quit -projectPath ... -executeMethod EffectCompare.Run -logFile -
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class EffectCompare
{
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string VfxBundleName = "battleprefabs_vfxandmisc_assets_all.bundle";
    const string PrefabDir = "Assets/WarpforgeVFX/Prefabs";
    const string OutDir = @"d:\4\_tmp_view\cmp";

    const int W = 512, H = 512;
    /// <summary>统一模拟到这一刻，保证两边可比。**可用 `WFCMP_SIMT` 环境变量覆盖** ——
    /// 台账里各效果的内容时段不同（0.15–0.75s 的短效果在 1.2s 早播空了），
    /// 每次改源码 + 记得改回太容易出错，所以让它能从外面给。
    /// 取证短效果时**按台账「原版有内容时段」取中点**。</summary>
    static readonly float SimTime =
        float.TryParse(System.Environment.GetEnvironmentVariable("WFCMP_SIMT"), out var _st) ? _st : 1.2f;

    /// <summary>🔬 `WFCMP_ISO=all` —— **逐渲染器隔离**：每个效果额外渲 N 对图，
    /// 第 i 对里**只开第 i 个渲染器**（其余 `Renderer.enabled = false`，不动层级）。
    /// 为什么要它（2026-09-19 晚）：有些效果**材质层和粒子模块层都查不出差异**
    /// （`diff_matdump` / `ParticleModuleProbe` 都干净），但并排一看就是偏亮
    /// ⇒ 只能把「哪个渲染器贡献了多余的亮度」一个一个隔离出来。
    /// 输出：`<效果>__iso<i>_{orig,exp}.png`（两侧同序号 = 同一位置的渲染器）。</summary>
    static readonly bool IsoAll =
        System.Environment.GetEnvironmentVariable("WFCMP_ISO") == "all";

    // 只比对名字里含这些子串的效果（空数组 = 全量）。
    // 全量 958 个要跑 1~2 小时，验证某个改动时按关键字切一小批有用得多。
    // 当前留空 = 全量。做小批验证时照下面这样填，跑完记得清空：
    //   { "Dark Angels", "BulletImpact_DCannon", "Atk_GrotGrenade", "AmbushEffect",
    //     "BlastEffect", "Vortex Explosion Massive", "Godspear Warhead Full" }
    static readonly string[] NameFilter = {
        // 2026-09-19 用过：{ "Tap Plasma generator", "Vortex Explosion Massive" }（E 组「加色发光/抓屏」族取证）
        // 2026-09-19 晚用过：{ "CardPrefab", "Invoke Minion Hits Ground", "Invoke Minion Legendary ALT",
        //                      "StunEffect_proc", "BlastEffect" }
        // 跑完按惯例清空 = 全量。
    };

    public static void Run()
    {
        Debug.Log("=== 特效比对渲染 开始 ===");
        Directory.CreateDirectory(OutDir);

        AssetBundle vfx = null;
        foreach (var f in Directory.GetFiles(BundleDir, "*.bundle"))
        {
            var b = AssetBundle.LoadFromFile(f);
            if (b != null && Path.GetFileName(f) == VfxBundleName) vfx = b;
        }
        if (vfx == null) { Debug.LogError("特效 bundle 未加载"); return; }

        var originals = new Dictionary<string, GameObject>();
        foreach (var n in vfx.GetAllAssetNames())
        {
            GameObject g = null;
            try { g = vfx.LoadAsset<GameObject>(n); } catch { }
            if (g != null && g.GetComponentsInChildren<ParticleSystemRenderer>(true).Length > 0) originals[g.name] = g;
        }

        var exports = Directory.GetFiles(PrefabDir, "*.prefab").Select(p => p.Replace('\\', '/')).ToList();
        if (NameFilter.Length > 0)
            exports = exports.Where(p => NameFilter.Any(k =>
                Path.GetFileNameWithoutExtension(p).IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
        Debug.Log($"原版效果 {originals.Count} 个，导出预制 {exports.Count} 个" +
                  (NameFilter.Length > 0 ? $"（已按 {string.Join("/", NameFilter)} 过滤）" : ""));

        // ---- 第一趟：**只碰原版**。渲 `__orig` + 记取景 ----
        // 🔴 **顺序不能反**：`wf_shaders_extra.bundle` 的内容是从 `battleprefabs*` 抽出来的，
        //    **源 bundle 一加载，它就被 Unity 拒载**（"another AssetBundle with the same files"）⇒
        //    `WarpforgeShaderLoader` 只捡到已加载包里的 49 个、**白名单里那些「只在补充包里」的
        //    shader 全解析不到** ⇒ **导出侧拿占位材质渲**（`URP/Unlit` ⇒ 大块不透明白多边形），
        //    看起来像「我们画错了」。而且 `_tried` 是 static 一次性 ⇒ **先卸载再重试也没用**，
        //    只能让导出侧的渲染**排在卸载之后**。
        //    `EffectSweepBatch` 早就分两趟绕开了（它 `exp` 那趟不加载源 bundle）；这支 2026-09-19 才补上。
        //    **现场证据**：日志里 `找不到 shader 'Shader Graphs/Fx_ParticleDissolve_apb'（材质 Mat_Fx_ParticleSet_apb）`
        //    + `shader bundle 被同内容的包顶掉了，改从已加载的 bundle 里捡回 49 个 shader`。
        var frames = new Dictionary<string, CamFrame>();
        int ok = 0;
        foreach (var path in exports)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (!originals.TryGetValue(name, out var orig)) { Debug.LogWarning($"原版里找不到 {name}"); continue; }
            try
            {
                var cam = FrameCamera(orig);
                frames[name] = cam;
                // ⚠️ 这行**必须在 RenderOne 之前** —— `WFCMP_MATDUMP` 的 MD 行是在 RenderOne
                //    里面打的，`工具/diff_matdump.py` 靠这行切「原版段 / 导出段」。
                Debug.Log($"  比对 {name}（原版）");
                RenderOne(orig, $"{OutDir}/{name}__orig.png", cam);
                if (IsoAll)
                    for (int k = 0, n = CountRenderers(orig); k < n; k++)
                        RenderOne(orig, $"{OutDir}/{name}__iso{k}__orig.png", cam, k);
                ok++;
            }
            catch (Exception e) { Debug.LogWarning($"  {name} 渲染失败: {e.Message}"); }
        }

        // ---- 卸掉源 bundle：去掉「补充 shader 包被顶掉」这个条件，回到真实运行时 ----
        originals.Clear();
        vfx.Unload(true);
        Debug.Log("  源 bundle 已卸载 ⇒ 导出侧回到真实运行时条件");

        // ---- 第二趟：**只碰导出** ----
        foreach (var path in exports)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (!frames.TryGetValue(name, out var cam)) continue;
            try
            {
                var exp = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Debug.Log($"  比对 {name}（导出）");   // ⚠️ 同原版那趟：必须在 RenderOne 之前
                RenderOne(exp, $"{OutDir}/{name}__exp.png", cam);
                if (IsoAll)
                    for (int k = 0, n = CountRenderers(exp); k < n; k++)
                        RenderOne(exp, $"{OutDir}/{name}__iso{k}__exp.png", cam, k);
            }
            catch (Exception e) { Debug.LogWarning($"  {name} 导出侧渲染失败: {e.Message}"); }
        }
        Debug.Log($"=== 特效比对渲染 结束：{ok} 组 ===");
    }

    struct CamFrame { public Vector3 pos, look; public float fov, near, far; }

    /// <summary>按给定 prefab 的包围盒算一个取景（在临时实例上量）</summary>
    static CamFrame FrameCamera(GameObject prefab)
    {
        var tmp = UnityEngine.Object.Instantiate(prefab);
        tmp.transform.position = Vector3.zero;
        tmp.transform.rotation = Quaternion.identity;
        foreach (var ps in tmp.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Simulate(SimTime, withChildren: true, restart: true, fixedTimeStep: false);
            ps.Play();
        }
        Bounds b = new Bounds(Vector3.zero, Vector3.one); bool first = true;
        foreach (var r in tmp.GetComponentsInChildren<Renderer>(true))
        {
            if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
        }
        float radius = Mathf.Max(0.5f, b.extents.magnitude);
        float fov = 40f;
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

    /// <summary>数一个 prefab（含子物体）有几个 `Renderer` —— 逐渲染器隔离要按这个数循环。</summary>
    static int CountRenderers(GameObject prefab)
    {
        var tmp = UnityEngine.Object.Instantiate(prefab);
        int n = tmp.GetComponentsInChildren<Renderer>(true).Length;
        UnityEngine.Object.DestroyImmediate(tmp);
        return n;
    }

    /// <summary>`keep >= 0` ⇒ **只开第 keep 个渲染器**（逐渲染器隔离，见 `IsoAll`）。</summary>
    static void RenderOne(GameObject prefab, string outPath, CamFrame frame, int keep = -1)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var inst = UnityEngine.Object.Instantiate(prefab);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;

        // 编辑器里 Awake 不会跑，手动触发 binder：用原版 shader 重建材质
        var binder = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();

        // 🔬 `WFCMP_MATDUMP=1` —— 把**这一刻**每个渲染器材质的**全部属性**打出来。
        //    为什么要它：`ParticleModuleProbe` 读的是**磁盘上的 `.mat`（＝占位）**，
        //    **看不到 binder 重建之后的真实状态**；而「偏暗」这类锅常常就出在
        //    「某个 float/color/贴图没被灌进来、吃了默认值」上 ⇒ 只能在这里看。
        //    判据：**原版那一趟和导出的那一趟各打一份，逐属性 diff**（只打第一个渲染器就够了，
        //    同一 prefab 的材质往往共用；要看全部就放开下面的 `all`）。
        if (System.Environment.GetEnvironmentVariable("WFCMP_MATDUMP") == "1")
            DumpMaterials(inst);

        // 🔬 逐渲染器隔离（`WFCMP_ISO=all`）：只开第 keep 个 —— 用来找
        //    「哪个渲染器贡献了多余的亮度」（材质层/模块层都查不出差异时的那一手）。
        if (keep >= 0)
        {
            var rends = inst.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++) rends[i].enabled = (i == keep);
            Debug.Log($"  [iso] {Path.GetFileNameWithoutExtension(outPath)} 渲染器 {keep}/{rends.Length - 1}");
        }

        // 统一时间点：批处理下没有 Update，必须手动推进
        foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Simulate(SimTime, withChildren: true, restart: true, fixedTimeStep: false);
            ps.Play();
        }

        var camGo = new GameObject("Cam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.08f, 0.10f, 1f);
        cam.fieldOfView = frame.fov;
        cam.nearClipPlane = frame.near;
        cam.farClipPlane = frame.far;
        camGo.transform.position = frame.pos;
        camGo.transform.LookAt(frame.look);

        var lGo = new GameObject("Light");
        var li = lGo.AddComponent<Light>();
        li.type = LightType.Directional;
        li.intensity = 1.2f;
        li.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;

        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(inst);
    }

    /// <summary>把每个渲染器材质的**全部属性**打出来（`WFCMP_MATDUMP=1` 时才调）。
    ///
    /// **为什么必须有它**：`ParticleModuleProbe` 读的是**磁盘上的 `.mat`（＝导出时的占位）**，
    /// 看不到 `binder.Apply()` **重建之后**的真实状态 —— 而「偏暗/偏亮」这类锅，
    /// 很可能就出在「某个 float/color/贴图没被灌进来、吃了默认值」上。
    /// **判据**：原版那一趟与导出那一趟各打一份（`grep "^MD "`），**逐属性 diff**。</summary>
    static void DumpMaterials(GameObject root)
    {
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var path = PathOf(r.transform, root.transform);
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) { Debug.Log($"MD {path} | <null material>"); continue; }
                var sh = m.shader;
                var sb = new System.Text.StringBuilder();
                sb.Append($"MD {path} | mat={m.name} | shader={(sh == null ? "<null>" : sh.name)}")
                  .Append($" | queue={m.renderQueue} | keys=[{string.Join(",", m.shaderKeywords)}]");
                if (sh != null)
                {
                    int n = ShaderUtil.GetPropertyCount(sh);
                    for (int i = 0; i < n; i++)
                    {
                        var pn = ShaderUtil.GetPropertyName(sh, i);
                        switch (ShaderUtil.GetPropertyType(sh, i))
                        {
                            case ShaderUtil.ShaderPropertyType.Color:
                                var c = m.GetColor(pn);
                                sb.Append($" {pn}=({c.r:0.###},{c.g:0.###},{c.b:0.###},{c.a:0.###})"); break;
                            case ShaderUtil.ShaderPropertyType.Vector:
                                var v = m.GetVector(pn);
                                sb.Append($" {pn}=({v.x:0.###},{v.y:0.###},{v.z:0.###},{v.w:0.###})"); break;
                            case ShaderUtil.ShaderPropertyType.Float:
                            case ShaderUtil.ShaderPropertyType.Range:
                                sb.Append($" {pn}={m.GetFloat(pn):0.####}"); break;
                            case ShaderUtil.ShaderPropertyType.TexEnv:
                                var t = m.GetTexture(pn);
                                sb.Append($" {pn}={(t == null ? "<null>" : t.name + "(" + t.width + "x" + t.height + ")")}"); break;
                        }
                    }
                }
                Debug.Log(sb.ToString());
            }
        }
    }

    /// <summary>相对根的层级路径（与 `ParticleModuleProbe` 的 `A/B` 写法一致，方便对齐着看）</summary>
    static string PathOf(Transform t, Transform root)
    {
        var s = t.name;
        while (t.parent != null && t.parent != root) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
