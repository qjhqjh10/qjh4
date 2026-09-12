// PlayProbe.cs — 隔离探针：**只改「粒子怎么推进」这一个变量**，看渲染结果差在哪
//
// 起因：白板自检里 20 个效果有 10 个全程 0 亮点，但它们在同一份台账里是「对得上 + 高置信」。
// 同一个 prefab，`EffectSweepBatch` 能渲出内容，白板路径渲不出来 —— 差别只可能在推进方式：
//
//   A 扫描法：Stop(清空) → Simulate(绝对时刻, restart:true) → Play     ← 台账用的，已验证
//   B 白板法：Clear → Play → 每步 Simulate(dt, restart:false, 不含子物体)  ← 增量推进
//   C 同上但含子物体（子发射器/嵌套粒子系统只推根会漏）
//   D 每步都 restart:true（等价于「每帧重播到当前时刻」）
//
// 四个变体跑同一批效果、同样的取景、同样的时刻，把亮点数并排打出来。
// **别凭这个是「发现了一个 bug」就下结论 —— 先看 A 和 B 到底差多少、差在哪些效果上。**
//
// 用法：
//   Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod PlayProbe.Run -logFile "d:/4/_tmp_view/playprobe.log"
//   筛输出：grep "^WPPROBE" d:/4/_tmp_view/playprobe.log
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WarpforgeVFX;

public static class PlayProbe
{
    const string P = "WPPROBE ";

    // 名字在 EffectSweepBatch.NameFilter 之外单独列 —— 这批里既有「白板渲不出来」的，也有正常的对照组
    static readonly string[] Targets =
    {
        "Atk_GrotGrenade",              // 白板 0，台账说 0.15s 有 3126
        "Atk_ExileGlaiveThrow Movement",// 同上
        "Attack_Stomp",                 // 白板 0，台账说 0.50s 起有 652→1111→1010
        "Antimatter Explosion",         // 对照组：白板正常
        "AlphaWarriorEffect",           // 对照组：白板正常
    };

    static readonly float[] Times = { 0.15f, 0.50f, 1.00f, 1.50f };
    const float Dt = 1f / 30f;
    const int W = 256, H = 256;
    static readonly Color Bg = new Color(0.07f, 0.08f, 0.10f, 1f);
    const string PrefabDir = "Assets/WarpforgeVFX/Prefabs";

    static Camera _cam;

    public static void Run()
    {
        Debug.Log(P + "=== 推进方式对照 开始 ===");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Cam");
        _cam = camGo.AddComponent<Camera>();
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = Bg;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("效果\t方式\t" + string.Join("\t", Times.Select(t => $"@{t:F2}s")));

        foreach (var name in Targets)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");
            if (prefab == null) { Debug.LogWarning(P + $"没有 {name}"); continue; }
            Frame(_cam, prefab);

            var a = MethodeA(prefab);
            var b = MethodeB(prefab, false);
            var c = MethodeB(prefab, true);
            var d = MethodeD(prefab);

            Debug.Log(P + $"{name}");
            Row(sb, name, "A 绝对时刻(扫描法)", a);
            Row(sb, name, "B 增量(不含子)", b);
            Row(sb, name, "C 增量(含子)", c);
            Row(sb, name, "D 每步 restart", d);

            // 顺带把实例的渲染器状态打出来 —— 万一是材质/发射器的问题，这里能看出来
            Describe(prefab, name);
        }

        var outPath = @"d:/4/_tmp_view/playprobe.tsv";
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllText(outPath, sb.ToString());
        Debug.Log(P + $"=== 结束，表在 {outPath} ===");
    }

    static void Row(System.Text.StringBuilder sb, string name, string how, int[] lits)
    {
        Debug.Log(P + $"    {how,-22} " + string.Join("  ", lits.Select(x => $"{x,6}")));
        sb.AppendLine($"{name}\t{how}\t" + string.Join("\t", lits));
    }

    // ---- A：扫描工具那一套（绝对时刻 + restart）----
    static int[] MethodeA(GameObject prefab)
    {
        var inst = UnityEngine.Object.Instantiate(prefab);
        var binder = inst.GetComponent<WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();

        var outv = new int[Times.Length];
        for (int i = 0; i < Times.Length; i++)
        {
            foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Simulate(Times[i], withChildren: true, restart: true, fixedTimeStep: false);
                ps.Play();
            }
            outv[i] = Render();
        }
        UnityEngine.Object.DestroyImmediate(inst);
        return outv;
    }

    // ---- B/C：增量推进（白板那一套），withChildren 是唯一变量 ----
    static int[] MethodeB(GameObject prefab, bool withChildren)
    {
        var inst = UnityEngine.Object.Instantiate(prefab);
        var binder = inst.GetComponent<WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();

        var systems = inst.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems) { ps.Clear(true); ps.Play(true); }

        var outv = new int[Times.Length];
        int si = 0;
        float t = 0f;
        while (si < Times.Length && t < Times[Times.Length - 1] + 0.001f)
        {
            foreach (var ps in systems)
                if (ps != null) ps.Simulate(Dt, withChildren: withChildren, restart: false, fixedTimeStep: false);
            t += Dt;
            while (si < Times.Length && t >= Times[si] - 1e-4f) { outv[si] = Render(); si++; }
        }
        UnityEngine.Object.DestroyImmediate(inst);
        return outv;
    }

    // ---- D：每步 restart（等于「每帧从 0 重播到当前时刻」）----
    static int[] MethodeD(GameObject prefab)
    {
        var inst = UnityEngine.Object.Instantiate(prefab);
        var binder = inst.GetComponent<WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();

        var systems = inst.GetComponentsInChildren<ParticleSystem>(true);
        var outv = new int[Times.Length];
        int si = 0;
        float t = 0f;
        while (si < Times.Length && t < Times[Times.Length - 1] + 0.001f)
        {
            t += Dt;
            foreach (var ps in systems)
                if (ps != null) ps.Simulate(t, withChildren: true, restart: true, fixedTimeStep: false);
            while (si < Times.Length && t >= Times[si] - 1e-4f) { outv[si] = Render(); si++; }
        }
        UnityEngine.Object.DestroyImmediate(inst);
        return outv;
    }

    static void Describe(GameObject prefab, string name)
    {
        var inst = UnityEngine.Object.Instantiate(prefab);
        var rends = inst.GetComponentsInChildren<Renderer>(true);
        var psr = inst.GetComponentsInChildren<ParticleSystemRenderer>(true);
        int enabled = rends.Count(r => r.enabled);
        int noMat = rends.Count(r => r.sharedMaterials.Length == 0 || r.sharedMaterials.All(m => m == null));
        var paused = inst.GetComponentsInChildren<ParticleSystem>(true).Count(s => !s.isPlaying && !s.isPaused);
        Debug.Log(P + $"    [实例] 渲染器 {rends.Length}（启用 {enabled}，无材质 {noMat}）"
                    + $"  粒子系统 {psr.Length}  未在播 {paused}");
        foreach (var r in rends.Take(4))
        {
            var m = r.sharedMaterials.FirstOrDefault();
            string sh = m != null && m.shader != null ? m.shader.name : "(无)";
            string mm = r is ParticleSystemRenderer p2 ? $"renderMode={p2.renderMode}" : "";
            Debug.Log(P + $"      {r.GetType().Name} '{r.name}' enabled={r.enabled} bounds={r.bounds.size:F2} shader={sh} {mm}");
        }
        UnityEngine.Object.DestroyImmediate(inst);
    }

    static int Render()
    {
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
        int lit = 0;
        const float thr = 6f / 255f * 3f;
        foreach (var c in px)
            if (Mathf.Abs(c.r - Bg.r) + Mathf.Abs(c.g - Bg.g) + Mathf.Abs(c.b - Bg.b) > thr) lit++;

        UnityEngine.Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        return lit;
    }

    static void Frame(Camera cam, GameObject prefab)
    {
        var tmp = UnityEngine.Object.Instantiate(prefab);
        var binder = tmp.GetComponent<WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();

        Bounds b = new Bounds(Vector3.zero, Vector3.one);
        bool first = true;
        foreach (var t in Times)
        {
            foreach (var ps in tmp.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Simulate(t, withChildren: true, restart: true, fixedTimeStep: false);
            }
            foreach (var r in tmp.GetComponentsInChildren<Renderer>(true))
            {
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
        }
        UnityEngine.Object.DestroyImmediate(tmp);

        float radius = Mathf.Max(0.5f, b.extents.magnitude);
        const float fov = 40f;
        float dist = radius / Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f) * 1.15f;
        cam.fieldOfView = fov;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = radius * 100f;
        cam.transform.position = b.center + new Vector3(0f, 0f, -dist);
        cam.transform.LookAt(b.center);
    }
}
