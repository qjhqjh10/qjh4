// MatIsoProbe.cs — 逐材质槽定位：把「导出的材质」一个一个换到「原版 prefab」上，看哪个槽把亮度抬上去了
//
// 起因：CEmitProbe 的交叉对照发现 — 同一个原版 prefab、同一批粒子，
//   配原版材质渲出 2936 个亮点，配导出材质渲出 13722 个 —— **差 4.7 倍**。
// 这说明导出侧的重建材质整体偏亮（E 组 170 个高置信里 85 个是偏亮）。
// 整体比只能知道「偏亮」，这个工具是把它**摊到每个材质槽上**：一次只换一个槽，
// 谁换上去亮度就跳，谁就是嫌疑。
//
// ⚠️ 判据看的是**全画面的亮度和（sum）**，不是亮点数 —— 亮点数会饱和，
//    而偏亮这种问题恰恰是「面积没变、每个像素更亮了」。
//
// 用法：
//   Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod MatIsoProbe.Run -logFile "d:/4/_tmp_view/matiso.log"
//   筛输出：grep "^MI " d:/4/_tmp_view/matiso.log
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MatIsoProbe
{
    const string P = "MI ";
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string VfxBundleName = "battleprefabs_vfxandmisc_assets_all.bundle";
    const string PrefabDir = "Assets/WarpforgeVFX/Prefabs";

    static readonly string[] Targets = { "ArtificeEffect",
        // 🆕 2026-09-15：C 组最后一个（「只有原版有内容」的唯一一个）——
        //    结构侧 `CEmitProbe` 已经证明**导出和原版逐渲染器完全一致**（粒子数/材质/shader/queue 全同），
        //    所以差的一定在**材质属性值**或粒子的 `startColor`/`startSize` 上，用这个探针逐个材质槽量。
        "PinDownEffect" };

    const float SimTime = 1.2f;
    const int W = 512, H = 512;
    static readonly Color Bg = new Color(0.07f, 0.08f, 0.10f, 1f);

    static Camera _cam;

    public static void Run()
    {
        Debug.Log(P + "=== 逐材质槽对照 开始 ===");

        AssetBundle vfx = null;
        foreach (var f in Directory.GetFiles(BundleDir, "*.bundle"))
        {
            var b = AssetBundle.LoadFromFile(f);
            if (b != null && Path.GetFileName(f) == VfxBundleName) vfx = b;
        }
        if (vfx == null) { Debug.LogError(P + "特效 bundle 未加载"); return; }

        var originals = new Dictionary<string, GameObject>();
        foreach (var n in vfx.GetAllAssetNames())
        {
            GameObject g = null;
            try { g = vfx.LoadAsset<GameObject>(n); } catch { }
            if (g != null) originals[g.name] = g;
        }

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Cam");
        _cam = camGo.AddComponent<Camera>();
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = Bg;

        foreach (var name in Targets)
        {
            GameObject orig, exp;
            if (!originals.TryGetValue(name, out orig)) { Debug.LogWarning(P + $"原版没有 {name}"); continue; }
            exp = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");
            if (exp == null) { Debug.LogWarning(P + $"导出没有 {name}"); continue; }

            Debug.Log(P + $"########## {name} ##########");
            ApplyFrame(FrameOn(orig));

            var o = Make(orig);
            var e = Make(exp);
            e.transform.position = new Vector3(10000f, 0f, 0f);   // 挪开，别混进画面

            var baseline = Render();
            Debug.Log(P + $"  [基准] 原版 prefab + 原版材质：亮点 {baseline.lit} 亮度和 {baseline.sum:F0}");

            // 原版 prefab + 导出材质（整体）
            var oMap = Snapshot(o);
            Swap(o, Map(e));
            var all = Render();
            double allPct = (all.sum - baseline.sum) / Math.Max(1.0, baseline.sum) * 100.0;
            Debug.Log(P + $"  [整体] 原版 prefab + **全部**导出材质：亮点 {all.lit} 亮度和 {all.sum:F0}"
                        + $"  → 相对基准 {allPct:+0.0;-0.0}%");

            // 逐个槽换回来对比：一次只把一个槽换成导出材质
            foreach (var pair in Match(o, e))
            {
                var or = pair.Item1;
                var saved = or.sharedMaterials;
                var em = pair.Item2.sharedMaterials;
                var arr = new Material[saved.Length];
                for (int i = 0; i < arr.Length; i++) arr[i] = i < em.Length ? em[i] : saved[i];
                or.sharedMaterials = arr;
                var one = Render();
                or.sharedMaterials = saved;
                double pct = (one.sum - baseline.sum) / Math.Max(1.0, baseline.sum) * 100.0;
                Debug.Log(P + $"    {PathOf(or.transform, o.transform),-40} 亮点 {one.lit,6} 亮度和 {one.sum,10:F0}"
                            + $"  {pct,+7:F1}%  {(Math.Abs(pct) > 50 ? "★ 嫌疑" : "")}");
            }

            // 恢复并销毁
            Swap(o, oMap);
            UnityEngine.Object.DestroyImmediate(o);
            UnityEngine.Object.DestroyImmediate(e);
        }

        Debug.Log(P + "=== 结束 ===");
    }

    static GameObject Make(GameObject prefab)
    {
        var inst = UnityEngine.Object.Instantiate(prefab);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;
        var binder = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();
        foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Simulate(SimTime, withChildren: true, restart: true, fixedTimeStep: false);
            ps.Play();
        }
        return inst;
    }

    /// <summary>按层级路径把「原版的渲染器」和「导出的渲染器」配起来</summary>
    static List<Tuple<Renderer, Renderer>> Match(GameObject o, GameObject e)
    {
        var byPath = new Dictionary<string, Renderer>();
        foreach (var r in e.GetComponentsInChildren<Renderer>(true))
            byPath[PathOf(r.transform, e.transform)] = r;

        var pairs = new List<Tuple<Renderer, Renderer>>();
        foreach (var r in o.GetComponentsInChildren<Renderer>(true))
        {
            Renderer er;
            if (byPath.TryGetValue(PathOf(r.transform, o.transform), out er)) pairs.Add(Tuple.Create(r, er));
        }
        return pairs;
    }

    static Dictionary<Renderer, Material[]> Snapshot(GameObject root)
    {
        var d = new Dictionary<Renderer, Material[]>();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true)) d[r] = r.sharedMaterials;
        return d;
    }

    static void Swap(GameObject root, Dictionary<Renderer, Material[]> map)
    {
        foreach (var kv in map) if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
    }

    static Dictionary<Renderer, Material[]> Map(GameObject root)
    {
        var d = new Dictionary<Renderer, Material[]>();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true)) d[r] = r.sharedMaterials;
        return d;
    }

    static string PathOf(Transform t, Transform root)
    {
        var s = t.name;
        while (t.parent != null && t.parent != root) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    static (int lit, double sum) Render()
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
        int lit = 0; double sum = 0;
        const float thr = 6f / 255f * 3f;
        foreach (var c in px)
            if (Mathf.Abs(c.r - Bg.r) + Mathf.Abs(c.g - Bg.g) + Mathf.Abs(c.b - Bg.b) > thr)
            { lit++; sum += c.r + c.g + c.b; }

        UnityEngine.Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        return (lit, sum);
    }

    static void ApplyFrame(Bounds b)
    {
        float radius = Mathf.Max(0.5f, b.extents.magnitude);
        const float fov = 40f;
        float dist = radius / Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f) * 1.15f;
        _cam.fieldOfView = fov;
        _cam.nearClipPlane = 0.01f;
        _cam.farClipPlane = radius * 100f;
        _cam.transform.position = b.center + new Vector3(0f, 0f, -dist);
        _cam.transform.LookAt(b.center);
    }

    static Bounds FrameOn(GameObject prefab)
    {
        var tmp = UnityEngine.Object.Instantiate(prefab);
        tmp.transform.position = Vector3.zero;
        tmp.transform.rotation = Quaternion.identity;
        foreach (var ps in tmp.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Simulate(SimTime, withChildren: true, restart: true, fixedTimeStep: false);
        }
        Bounds b = new Bounds(Vector3.zero, Vector3.one);
        bool first = true;
        foreach (var r in tmp.GetComponentsInChildren<Renderer>(true))
        {
            if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
        }
        UnityEngine.Object.DestroyImmediate(tmp);
        return b;
    }
}
