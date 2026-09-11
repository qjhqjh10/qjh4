// EffectTimeSweep.cs — 时序采样：把原版和导出在不同时刻各渲染一张，找出「是不是只是采样时刻没对上」
//
// EffectCompare 只拍 SimTime=1.2s 单帧。像「闪电从天而降」这类由技能触发的时序特效，
// 单帧经常两边都是空的 —— 那不是还原失败，是采样点不对。这个工具就是用来区分的。
//
// 用法：Unity.exe -batchmode -quit -projectPath ... -executeMethod EffectTimeSweep.Run -logFile -
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EffectTimeSweep
{
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string PrefabDir = "Assets/WarpforgeVFX/Prefabs";
    const string OutDir = @"d:\4\_tmp_view\sweep";

    // 要采样的效果（效果名的子串匹配）
    static readonly string[] Targets =
    {
        "Psychic_Lightning_down", "Offensive_Thunderstorm", "Psychic_Lightning",
        "Lightning burst webway", "Impact_blunt_lightning", "Lightning_Green",
    };

    static readonly float[] Times = { 0.25f, 0.5f, 0.75f, 1.0f, 1.5f, 2.0f, 3.0f };

    const int W = 320, H = 320;

    public static void Run()
    {
        Debug.Log("=== 时序采样 开始 ===");
        Directory.CreateDirectory(OutDir);

        // 只加载特效包：加载全部 84 个会顶掉 wf_shaders.bundle 的加载
        var vfxPath = Path.Combine(BundleDir, "battleprefabs_vfxandmisc_assets_all.bundle");
        var vfx = AssetBundle.LoadFromFile(vfxPath);
        if (vfx == null) { Debug.LogError($"特效 bundle 未加载: {vfxPath}"); return; }

        var originals = new Dictionary<string, GameObject>();
        foreach (var n in vfx.GetAllAssetNames())
        {
            GameObject g = null;
            try { g = vfx.LoadAsset<GameObject>(n); } catch { }
            if (g != null) originals[g.name] = g;
        }

        var exports = Directory.GetFiles(PrefabDir, "*.prefab")
                               .Select(p => Path.GetFileNameWithoutExtension(p)).ToList();

        foreach (var key in Targets)
        {
            var names = exports.Where(x => x.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            if (names.Count == 0) { Debug.LogWarning($"  导出里没找到含「{key}」的效果"); continue; }
            foreach (var name in names)
            {
                if (!originals.TryGetValue(name, out var orig)) { Debug.LogWarning($"  原版里没找到 {name}"); continue; }
                var exp = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");
                if (exp == null) continue;

                var cam = FrameCamera(orig);
                var counts = new List<string>();
                foreach (var t in Times)
                {
                    var (oa, ob) = RenderAt(orig, t, cam, $"{OutDir}/{Sanitize(name)}_{t:F2}__orig.png");
                    var (ea, eb) = RenderAt(exp, t, cam, $"{OutDir}/{Sanitize(name)}_{t:F2}__exp.png");
                    counts.Add($"{t:F2}s 原{oa} 导{ea}");
                }
                Debug.Log($"  [{name}] 亮点数 " + string.Join(" | ", counts));
            }
        }
        Debug.Log("=== 时序采样 结束 ===");
    }

    static (int lit, double sum) RenderAt(GameObject prefab, float time, CamFrame frame, string outPath)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var inst = UnityEngine.Object.Instantiate(prefab);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;

        var binder = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();

        // 统一推进到同一时刻
        foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Simulate(time, withChildren: true, restart: true, fixedTimeStep: false);
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

        // 顺便统计这一帧有没有内容（省一次读盘）
        var px = tex.GetPixels();
        var bg = new Color(0.07f, 0.08f, 0.10f);
        int lit = 0; double sum = 0;
        foreach (var c in px)
        {
            float d = Mathf.Abs(c.r - bg.r) + Mathf.Abs(c.g - bg.g) + Mathf.Abs(c.b - bg.b);
            if (d > 6f / 255f * 3f) { lit++; sum += c.r + c.g + c.b; }
        }

        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(inst);
        return (lit, sum);
    }

    struct CamFrame { public Vector3 pos, look; public float fov, near, far; }

    /// <summary>取景用「整个时间轴上的最大包围盒」，保证所有采样时刻都框得住</summary>
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

    static string Sanitize(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace('/', '_').Trim();
    }
}
