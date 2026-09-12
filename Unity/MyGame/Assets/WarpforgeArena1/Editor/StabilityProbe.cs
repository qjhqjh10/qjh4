// StabilityProbe.cs — 尺子稳定性探针：同一个进程里，同样的条件连渲三次，看数字动不动
//
// 为什么需要：`EffectSweepBatch` 同一份资产连跑两遍，有 ~60% 的采样行会变 ——
// 也就是说**任何小改动都测不出来**。已知的嫌疑逐个排除：
//   · `ps.Play()`  → 去掉后仍然不定
//   · `fixedTimeStep` → 改成 true 后仍然不定
//   · `useAutoRandomSeed` → 冻住种子后仍然不定
// 剩下两类可能，这个探针用来分开它们：
//   A. **进程内**就不稳定（比如自建 shader 里用了 `_Time`，UV 滚动/流光跟着墙钟走）
//      → 同一进程连渲三次就会各不相同
//   B. **跨进程**才不稳定（进程启动时机/初始化差异）
//      → 同一进程三次完全相同，但换个进程就变
//
// 用法：Unity.exe -batchmode -quit -projectPath ... -executeMethod StabilityProbe.Run -logFile -
//   筛输出：grep "^SP " d:/4/_tmp_view/stab.log
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StabilityProbe
{
    const string P = "SP ";
    const string PrefabDir = "Assets/WarpforgeVFX/Prefabs";
    const string FrameCache = @"d:\4\Unity\资料\比对基线\sweep_frames.tsv";

    static readonly string[] Targets =
        { "Antimatter Explosion", "Explosion_Ground", "Attack_Stomp", "Psychic_Lightning_down" };

    const int Rounds = 3;
    const float SimTime = 1.0f;
    const int W = 256, H = 256;
    static readonly Color Bg = new Color(0.07f, 0.08f, 0.10f, 1f);

    static Camera _cam;

    public static void Run()
    {
        Debug.Log(P + "=== 稳定性探针 开始（同进程连渲 " + Rounds + " 次）===");

        var frames = LoadFrames();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Cam");
        _cam = camGo.AddComponent<Camera>();
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = Bg;

        foreach (var name in Targets)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");
            if (prefab == null) { Debug.LogWarning(P + $"没有 {name}"); continue; }

            F f;
            if (frames.TryGetValue(name, out f)) ApplyFrame(f);
            else { ApplyFrame(FrameOn(prefab)); Debug.Log(P + $"  {name} 没有取景缓存，用自算的"); }

            var lits = new List<int>();
            var sums = new List<double>();
            for (int r = 0; r < Rounds; r++)
            {
                var (lit, sum) = OneShot(prefab);
                lits.Add(lit); sums.Add(sum);
            }
            bool same = lits.All(x => x == lits[0]);
            Debug.Log(P + $"  {name,-32} 三次亮点 {string.Join(", ", lits)}  {(same ? "" : "⚠️ 进程内就不稳")}");
            Debug.Log(P + $"  {"",-32} 三次亮度和 {string.Join(", ", sums.Select(s => s.ToString("F0")))}");
        }

        Debug.Log(P + "=== 结束 ===");
    }

    static (int lit, double sum) OneShot(GameObject prefab)
    {
        var inst = UnityEngine.Object.Instantiate(prefab);
        var binder = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();

        foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed = false;
            ps.randomSeed = 20260911;          // 显式钉死种子
            ps.Simulate(0f, withChildren: true, restart: true, fixedTimeStep: true);   // 先复位
            ps.Simulate(SimTime, withChildren: true, restart: false, fixedTimeStep: true);
        }

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
        int lit = 0; double sum = 0;
        const float thr = 6f / 255f * 3f;
        foreach (var c in px)
            if (Mathf.Abs(c.r - Bg.r) + Mathf.Abs(c.g - Bg.g) + Mathf.Abs(c.b - Bg.b) > thr)
            { lit++; sum += c.r + c.g + c.b; }

        UnityEngine.Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        UnityEngine.Object.DestroyImmediate(inst);
        return (lit, sum);
    }

    struct F { public Vector3 pos, look; public float fov, near, far; }

    static Dictionary<string, F> LoadFrames()
    {
        var d = new Dictionary<string, F>();
        if (!File.Exists(FrameCache)) return d;
        foreach (var l in File.ReadLines(FrameCache).Skip(1))
        {
            var p = l.Split('\t');
            if (p.Length < 11) continue;
            d[p[0]] = new F
            {
                pos = V(p, 1), look = V(p, 4),
                fov = float.Parse(p[7], CultureInfo.InvariantCulture),
                near = float.Parse(p[8], CultureInfo.InvariantCulture),
                far = float.Parse(p[9], CultureInfo.InvariantCulture),
            };
        }
        return d;
    }

    static Vector3 V(string[] p, int i)
    {
        return new Vector3(float.Parse(p[i], CultureInfo.InvariantCulture),
                           float.Parse(p[i + 1], CultureInfo.InvariantCulture),
                           float.Parse(p[i + 2], CultureInfo.InvariantCulture));
    }

    static void ApplyFrame(F f)
    {
        _cam.fieldOfView = f.fov;
        _cam.nearClipPlane = f.near;
        _cam.farClipPlane = f.far;
        _cam.transform.position = f.pos;
        _cam.transform.LookAt(f.look);
    }

    static F FrameOn(GameObject prefab)
    {
        var tmp = UnityEngine.Object.Instantiate(prefab);
        foreach (var ps in tmp.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.useAutoRandomSeed = false;
            ps.Simulate(SimTime, withChildren: true, restart: true, fixedTimeStep: true);
        }
        Bounds b = new Bounds(Vector3.zero, Vector3.one);
        bool first = true;
        foreach (var r in tmp.GetComponentsInChildren<Renderer>(true))
        {
            if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
        }
        UnityEngine.Object.DestroyImmediate(tmp);
        float radius = Mathf.Max(0.5f, b.extents.magnitude);
        const float fov = 40f;
        float dist = radius / Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f) * 1.15f;
        return new F { pos = b.center + new Vector3(0f, 0f, -dist), look = b.center, fov = fov, near = 0.01f, far = radius * 100f };
    }
}
