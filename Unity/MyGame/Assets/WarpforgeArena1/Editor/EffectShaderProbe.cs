// EffectShaderProbe.cs — 判定「导出的材质到底解析到了哪个 shader」，并把两种加载条件分开测
//
// 背景：EffectCompare / EffectDiag / EffectIso 都会先把 BundleDir 下 84 个源 bundle 全量加载。
// 而 WarpforgeShaderLoader 要加载的 StreamingAssets/WarpforgeVFX/wf_shaders.bundle
// 是 shaders_assets_all.bundle 的**副本**。同一份内容、两个路径，AssetBundle.LoadFromFile
// 可能返回 null（见 WarpforgeShaderLoader 里的注释）。
//
// 于是有个悬而未决的问题：导出的 prefab 在编辑器里渲染成白块，
// 到底是「自建 shader 没覆盖到这个原版 shader」，还是「加载冲突导致退回了占位材质」？
// 这两件事的修法完全不同，所以必须分开测。
//
// 本探针跑两轮，唯一变量是「有没有先加载那 84 个源 bundle」：
//   轮 A：不加载源 bundle  → 相当于「干净的运行时条件」
//   轮 B：先全量加载源 bundle → 相当于 EffectCompare 的条件
// 每轮都记录 WarpforgeShaderLoader.Ready、每个渲染器的 shader 名、关键贴图绑定情况，并渲一张图。
//
// 用法：
//   Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod EffectShaderProbe.Run -logFile -
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EffectShaderProbe
{
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string PrefabDir = "Assets/WarpforgeVFX/Prefabs";
    const string OutDir = @"d:\4\_tmp_view\probe_shader";

    // 要探的效果
    // 后三个用来验证「补充 shader 包」：它们分别用到 Rays For Trail / Multi Ray /
    // Fx_ParticleDissolve_apb —— 这三个都在 wf_shaders_extra.bundle 里
    static readonly string[] Targets = {
        "Environmental Condition Dark Angels Void Combat",
        "Environmental Condition Dark Angels Asteroid Zone",
        "Atk_GrotGrenade",
        "BulletImpact_DCannon",
        "BlastEffect",
    };

    // 直接点名查这些 shader 能不能解析（覆盖补充包里几个大头）
    static readonly string[] WatchShaders = {
        "Everguild/FX/Rays For Trail",
        "Everguild/FX/Multi Ray",
        "Everguild/FX/Particle Dissolve Mask",
        "Everguild/FX/Particle Shine Custom Vertex Streams",
        "Spine/Special/HiddenPass",
        "Everguild/FX/Particle Premultiply",
        "Shader Graphs/Fx_ParticleDissolve_apb",
        "Everguild/FX/ShieldVfx",
        "Everguild/FX/Alpha Masks Two Layer",
    };

    // 每个渲染器都关心的贴图属性（有就报，没有就跳过）
    static readonly string[] WatchTex = { "_MainTex", "_NoiseTex1", "_NoiseTex2", "_BaseMap" };

    const int W = 512, H = 512;
    const float SimTime = 1.2f;

    /// <summary>行首前缀。批量模式下每条 Debug.Log 后面都会跟一坨 Unity 堆栈，
    /// 给行首加个可 grep 的标记，筛日志时用 grep "^WFPROBE" 就干净了。</summary>
    const string P = "WFPROBE ";

    public static void Run()
    {
        Directory.CreateDirectory(OutDir);
        Debug.Log(P + "=== EffectShaderProbe 开始 ===");

        // ---------- 轮 A：不加载源 bundle（干净的运行时条件）----------
        Debug.Log(P + "########## 轮 A：不加载源 bundle ##########");
        Probe("A_clean");

        // ---------- 轮 B：先全量加载源 bundle（EffectCompare 的条件）----------
        Debug.Log(P + "########## 轮 B：先全量加载 84 个源 bundle ##########");
        int nb = 0;
        foreach (var f in Directory.GetFiles(BundleDir, "*.bundle"))
        {
            if (AssetBundle.LoadFromFile(f) != null) nb++;
        }
        Debug.Log(P + $"  [B] 源 bundle 已加载 {nb} 个");
        Probe("B_after_srcbundles");

        Debug.Log(P + "=== EffectShaderProbe 结束 ===");
    }

    static void Probe(string tag)
    {
        // 每轮都重新问一次 loader 的状态。注意 loader 内部有 _tried 缓存，
        // 第一轮问过之后第二轮不会重新尝试 —— 这本身就是「谁先谁赢」的机制，
        // 正好用来复现 EffectCompare 的实际行为。
        ForceReloadLoader();
        Debug.Log(P + $"  [{tag}] WarpforgeShaderLoader.Ready = {WarpforgeVFX.WarpforgeShaderLoader.Ready}");
        var names = new List<string>();
        foreach (var n in WarpforgeVFX.WarpforgeShaderLoader.ShaderNames) names.Add(n);
        Debug.Log(P + $"  [{tag}] shader bundle 里 {names.Count} 个 shader");

        // 点名查：这些是原来运行时解析不到、靠 wf_shaders_extra.bundle 补上的
        foreach (var sn in WatchShaders)
        {
            bool ok = WarpforgeVFX.WarpforgeShaderLoader.TryGetShader(sn, out var sh);
            Debug.Log(P + $"  [{tag}] LOOKUP {(ok ? "OK  " : "缺失")} {sn}" +
                          (ok ? $" (id={sh.GetInstanceID()})" : ""));
        }

        foreach (var name in Targets) ProbeOne(name, tag);
    }

    /// <summary>把 loader 的静态缓存全部清掉，让每一轮都能真正重新尝试加载。
    ///
    /// ⚠️ 必须连 _byName 一起清。只清 _tried/_bundle 的话，第二轮虽然 Ready=False，
    /// 但名字字典还留着第一轮的结果，TryGetShader 照样能命中 —— 负对照就失效了
    /// （第一次跑这个探针就踩了这个坑，两轮都渲染成功，看不出条件差异）。</summary>
    static void ForceReloadLoader()
    {
        var t = typeof(WarpforgeVFX.WarpforgeShaderLoader);
        const System.Reflection.BindingFlags SF =
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
        foreach (var fn in new[] { "_tried", "_bundle" })
        {
            var f = t.GetField(fn, SF);
            if (f != null) f.SetValue(null, fn == "_tried" ? (object)false : null);
        }
        var byName = t.GetField("_byName", SF);
        if (byName != null)
        {
            var d = byName.GetValue(null) as System.Collections.IDictionary;
            if (d != null) d.Clear();
        }
    }

    static void ProbeOne(string effectName, string tag)
    {
        var path = $"{PrefabDir}/{effectName}.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { Debug.LogWarning($"  [{tag}] 找不到导出预制 {effectName}"); return; }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var inst = UnityEngine.Object.Instantiate(prefab);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;

        var rends = inst.GetComponentsInChildren<Renderer>(true);

        Debug.Log(P + $"  [{tag}] ---- {effectName}：{rends.Length} 个渲染器 · 占位材质（binder 前）----");
        DumpShaders(rends, "占位");

        var binder = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();
        else Debug.LogWarning($"  [{tag}] 没有 WarpforgeEffectBinder");

        Debug.Log(P + $"  [{tag}] ---- {effectName}：binder 重建后 ----");
        DumpShaders(rends, "重建");

        // 顺手看看 binder 里记的原 shader 名单（这才是「应该」解析到的东西）
        if (binder != null)
        {
            var used = new HashSet<string>();
            foreach (var d in binder.materials) if (d != null) used.Add(d.shader);
            Debug.Log(P + $"  [{tag}] binder 里记录的原 shader（{used.Count} 个）：{string.Join(", ", used.OrderBy(x => x))}");
        }

        // 渲染一张
        foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Simulate(SimTime, withChildren: true, restart: true, fixedTimeStep: false);
            ps.Play();
        }

        var frame = FrameOf(inst);
        var camGo = new GameObject("Cam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.08f, 0.10f, 1f);
        cam.fieldOfView = frame.fov;
        cam.nearClipPlane = frame.near;
        cam.farClipPlane = frame.far;
        camGo.transform.position = frame.pos;
        camGo.transform.LookAt(frame.look);

        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;

        var outPath = $"{OutDir}/{effectName}__{tag}.png";
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Debug.Log(P + $"  [{tag}] 已写出 {outPath}");

        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(inst);
    }

    static void DumpShaders(Renderer[] rends, string phase)
    {
        var tally = new Dictionary<string, int>();
        var texState = new Dictionary<string, string>();
        foreach (var r in rends)
        {
            foreach (var m in r.sharedMaterials)
            {
                string sn = (m == null || m.shader == null) ? "<null>" : m.shader.name;
                tally[sn] = tally.TryGetValue(sn, out var c) ? c + 1 : 1;

                if (m == null || m.shader == null) continue;
                var parts = new List<string>();
                foreach (var tn in WatchTex)
                {
                    if (!m.HasProperty(tn)) continue;
                    var t = m.GetTexture(tn);
                    parts.Add($"{tn}={(t == null ? "<null>" : t.name)}");
                }
                if (parts.Count > 0)
                {
                    var key = sn + " | " + string.Join(" ", parts);
                    texState[key] = "x";
                }
            }
        }
        Debug.Log(P + $"    [{phase}] shader 分布：");
        foreach (var kv in tally.OrderByDescending(x => x.Value))
            Debug.Log(P + $"    [{phase}] SHADER {kv.Value,4}x {kv.Key}");
        Debug.Log(P + $"    [{phase}] 贴图绑定（去重后 {texState.Count} 种组合）：");
        foreach (var k in texState.Keys.OrderBy(x => x))
            Debug.Log(P + $"    [{phase}] TEX {k}");
    }

    struct CamFrame { public Vector3 pos, look; public float fov, near, far; }

    static CamFrame FrameOf(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>(true);
        bool has = false;
        var b = new Bounds();
        foreach (var r in rends)
        {
            // 只量「有实际内容」的渲染器：粒子的 Renderer.bounds 是按存活粒子算的，
            // 没发射的发射器不会把包围盒撑大 —— 这点和 EffectCompare 的取景口径一致
            if (r is ParticleSystemRenderer)
            {
                var ps = r.GetComponent<ParticleSystem>();
                if (ps != null && !ps.isPlaying && !ps.isPaused) continue;
            }
            if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
        }
        if (!has || b.size.magnitude < 1e-4f) b = new Bounds(Vector3.zero, Vector3.one * 3f);

        float radius = Mathf.Max(b.extents.magnitude, 0.5f);
        var dir = new Vector3(0.6f, 0.45f, -1f).normalized;
        return new CamFrame
        {
            pos = b.center + dir * radius * 2.6f,
            look = b.center,
            fov = 40f,
            near = 0.01f,
            far = radius * 100f,
        };
    }
}
