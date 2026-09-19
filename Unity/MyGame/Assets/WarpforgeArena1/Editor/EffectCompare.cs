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
    const float SimTime = 1.2f;      // 统一模拟到 1.2 秒，保证可比

    // 只比对名字里含这些子串的效果（空数组 = 全量）。
    // 全量 958 个要跑 1~2 小时，验证某个改动时按关键字切一小批有用得多。
    // 当前留空 = 全量。做小批验证时照下面这样填，跑完记得清空：
    //   { "Dark Angels", "BulletImpact_DCannon", "Atk_GrotGrenade", "AmbushEffect",
    //     "BlastEffect", "Vortex Explosion Massive", "Godspear Warhead Full" }
    static readonly string[] NameFilter = {
        // 2026-09-19 用过：{ "Tap Plasma generator", "Vortex Explosion Massive" }（E 组「加色发光/抓屏」族取证）
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

        int ok = 0;
        foreach (var path in exports)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (!originals.TryGetValue(name, out var orig)) { Debug.LogWarning($"原版里找不到 {name}"); continue; }
            try
            {
                // 取景必须统一：用「原版」的包围盒算一次相机，两边共用，
                // 否则各自的自动构图不同，亮度差就没有可比性
                var cam = FrameCamera(orig);
                RenderOne(orig, $"{OutDir}/{name}__orig.png", cam);
                var exp = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                RenderOne(exp, $"{OutDir}/{name}__exp.png", cam);
                ok++;
                Debug.Log($"  比对 {name}");
            }
            catch (Exception e) { Debug.LogWarning($"  {name} 渲染失败: {e.Message}"); }
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

    static void RenderOne(GameObject prefab, string outPath, CamFrame frame)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var inst = UnityEngine.Object.Instantiate(prefab);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;

        // 编辑器里 Awake 不会跑，手动触发 binder：用原版 shader 重建材质
        var binder = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();

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
}
