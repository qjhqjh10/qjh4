// CEmitProbe.cs — C 组定位探针：同一个取景、同一个时刻，把原版和导出的
// 「每个渲染器 / 每个粒子系统的**活粒子数** / 材质 shader」并排打出来。
//
// 为什么需要它：C 组是「原版有内容、导出什么都没有」，可能的原因有好几层，用一张张隔离图
// 去猜太慢（EffectIso 的取景对着批效果也不适用 —— 原版自己渲出来也是空的）。这里直接把
// 判据摆出来：
//   · 活粒子数 原版>0 导出=0  → **没发射**（粒子模块/发射器数据的问题）
//   · 活粒子数 两边都 >0，但导出没像素 → **没渲染**（材质/shader/渲染器状态的问题）
//
// 取景两边共用（按原版算一次），和 EffectCompare 一个口径。
//
// 用法：
//   Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod CEmitProbe.Run -logFile "d:/4/_tmp_view/cemit.log"
//   筛输出：grep "^CE " d:/4/_tmp_view/cemit.log
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CEmitProbe
{
    const string P = "CE ";
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string VfxBundleName = "battleprefabs_vfxandmisc_assets_all.bundle";
    const string PrefabDir = "Assets/WarpforgeVFX/Prefabs";

    // 2026-09-15：换成 C 组「导出整个丢了」剩下的那 3 个（台账第 2–4 行）。
    // 原来那份（Spore Explosion / Godspear Warhead Full …）是 P0-h 排查时用的，已完成使命。
    static readonly string[] Targets =
    {
        "Explosion_Ground",     // 高置信 · 峰值亮点 713 · 原版 0.15–1.50s
        "BlastEffect",          // 中置信 · 216 · 0.15–0.75s
        "PinDownEffect",        // 低置信 · 105 · 0.15–3.00s
    };

    const float SimTime = 0.30f;
    const int W = 512, H = 512;
    static readonly Color Bg = new Color(0.07f, 0.08f, 0.10f, 1f);

    static Camera _cam;

    public static void Run()
    {
        Debug.Log(P + "=== C 组发射/渲染分离探针 开始 ===");

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
            GameObject orig;
            if (!originals.TryGetValue(name, out orig)) { Debug.LogWarning(P + $"原版没有 {name}"); continue; }
            var exp = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");
            if (exp == null) { Debug.LogWarning(P + $"导出没有 {name}"); continue; }

            Debug.Log(P + $"########## {name} ##########");

            // 取景按原版算，两边共用
            var frame = FrameOn(orig);
            ApplyFrame(frame);

            var o = Make(orig);
            var e = Make(exp);

            // ⚠️ 隔离两个实例要用「挪到画面外」，**不能用 SetActive(false)** ——
            //    停用再启用会把粒子系统重置，活粒子直接变 0，于是「换了材质也渲不出来」
            //    这种假结论就出来了（踩过：导出侧 10 个粒子被 SetActive 清成 0）。
            var far = new Vector3(10000f, 0f, 0f);
            e.transform.position = far;
            int litO = Render();
            Dump(o, "原版");

            o.transform.position = far; e.transform.position = Vector3.zero;
            int litE = Render();
            Dump(e, "导出");

            // 交叉材质：把 A 的材质贴到 B 上再渲一次。
            // 这一步切开两件事：**是材质不对**（换了材质就能渲出来）还是
            // **prefab 本身/渲染环境不对**（换了材质照样渲不出来）。
            var om = Map(o);
            var em = Map(e);
            Swap(e, om);
            int litE_withOrig = Render();
            o.transform.position = Vector3.zero; e.transform.position = far;
            Swap(o, em);
            int litO_withExp = Render();

            Debug.Log(P + $"  亮点数：原版 {litO} / 导出 {litE}");
            Debug.Log(P + $"  交叉：导出prefab+原版材质 {litE_withOrig} / 原版prefab+导出材质 {litO_withExp}");
            Debug.Log(P + $"  → 结论：{(litE_withOrig > 100 ? "「导出 prefab + 原版材质」能渲出来 ⇒ **问题在材质**" : "换原版材质也渲不出来 ⇒ **问题不在材质**，在 prefab/渲染状态")}");

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

    /// <summary>层级路径 → 材质</summary>
    static Dictionary<string, Material> Map(GameObject root)
    {
        var d = new Dictionary<string, Material>();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var key = PathOf(r.transform, root.transform) + "#" + r.GetInstanceID();
            d[PathOf(r.transform, root.transform)] = r.sharedMaterials.Length > 0 ? r.sharedMaterials[0] : null;
        }
        return d;
    }

    static void Swap(GameObject root, Dictionary<string, Material> src)
    {
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            Material m;
            if (!src.TryGetValue(PathOf(r.transform, root.transform), out m) || m == null) continue;
            var arr = r.sharedMaterials;
            for (int i = 0; i < arr.Length; i++) arr[i] = m;
            r.sharedMaterials = arr;
        }
    }

    /// <summary>逐渲染器 dump：活粒子数 + 材质 shader + **渲染器状态**（layer / maskInteraction / sprite）</summary>
    static void Dump(GameObject inst, string tag)
    {
        foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
        {
            var psr = r as ParticleSystemRenderer;
            var ps = r.GetComponent<ParticleSystem>();
            int alive = ps != null ? ps.particleCount : -1;
            string m;
            if (r.sharedMaterials.Length > 0 && r.sharedMaterials[0] != null)
            {
                var mat = r.sharedMaterials[0];
                m = $"{mat.name} shader={mat.shader.name} queue={mat.renderQueue}";
            }
            else m = "(无材质)";

            // 渲染器状态：这几项任一不同都会让「粒子在、bounds 在、材质对」的东西渲不出来
            string extra = "";
            if (psr != null) extra = $" mask={psr.maskInteraction} align={psr.alignment} sortLayer={psr.sortingLayerID}/{psr.sortingOrder}";
            var sr = r as SpriteRenderer;
            if (sr != null) extra = $" sprite={(sr.sprite != null ? sr.sprite.name : "<null>")}";
            var sm = r.GetComponent<SpriteMask>();
            if (sm != null) extra = $" maskSprite={(sm.sprite != null ? sm.sprite.name : "<null>")}";

            Debug.Log(P + $"  [{tag}] {PathOf(r.transform, inst.transform),-40} enabled={r.enabled}"
                        + $" layer={r.gameObject.layer} 活粒子={alive,5} bounds={r.bounds.extents.magnitude:F2}"
                        + $" {(psr != null ? "mode=" + psr.renderMode : r.GetType().Name)}{extra}");
            Debug.Log(P + $"        {m}");
        }
    }

    static string PathOf(Transform t, Transform root)
    {
        var s = t.name;
        while (t.parent != null && t.parent != root) { t = t.parent; s = t.name + "/" + s; }
        return s;
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
