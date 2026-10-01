// MeshSanityProbe.cs —— 一个网格「看着对不对」的体检：**顶点极值 + 单独渲一张**
//
// 为什么要它（2026-10-01 晚，查 `RemnantBody3D Aeldari` 那 16×）：
//   用 `EffectCompare`（`WFCMP_ISO=all`）把多出来的亮度锁到**渲染器 11 = `To remnant/Spirit Stone Appear`**
//   （145×，其余 23 个渲染器全在 1.00），接着逐项排掉了：
//     · 材质属性（`WFCMP_MATDUMP` 逐属性 diff —— 只差 renderQueue，而 A/B 证明它不是原因）
//     · shader 解析（白名单 A/B —— 像素**逐位不变**）
//     · 粒子模块（`ParticleModuleProbe` —— 0 个模块字段不同）
//     · 变换与世界包围盒（两侧**逐位相同**：0.69 × 0.89 × 0.14）
//   ⇒ 只剩「**网格资产的顶点数据本身**」没量过（包围盒是**元数据**，顶点坏了它照样对得上）。
//   本探针就是把这一层量掉：**顶点极值**（坏顶点会顶到 ±1e38 之类）+ 每个网格单独渲一张图。
//
// 用法：
//   unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod MeshSanityProbe.Run -logFile "d:/4/_tmp_view/meshprobe.log"
//   筛输出：grep "^MS "；图 → `d:/4/_tmp_view/meshprobe/`
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MeshSanityProbe
{
    const string Tag = "MS ";
    const string OutDir = @"d:\4\_tmp_view\meshprobe";
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string VfxBundleName = "battleprefabs_vfxandmisc_assets_all.bundle";
    const string PrefabPath = "Assets/WarpforgeVFX/Prefabs/RemnantBody3D Aeldari.prefab";
    const string TargetGo = "Spirit Stone Appear";
    /// <summary>探针要看的网格名（工程里按文件名前缀找、源包里按 `m_Name` 找）。</summary>
    static readonly string[] MeshNames = { "Spirt Stone" };

    static void Report(string where, Mesh m)
    {
        if (m == null) { Debug.Log(Tag + $"{where}: **null**"); return; }
        var b = m.bounds;
        string extra = $"顶点 {m.vertexCount} · 子网格 {m.subMeshCount} · bounds 中心={b.center.ToString("F4")} 尺寸={b.size.ToString("F4")}"
                     + $" · 可读={m.isReadable} · 混合权重={m.blendShapeCount}";
        if (m.isReadable)
        {
            try
            {
                var v = m.vertices;
                if (v.Length > 0)
                {
                    var mn = new Vector3(v.Min(x => x.x), v.Min(x => x.y), v.Min(x => x.z));
                    var mx = new Vector3(v.Max(x => x.x), v.Max(x => x.y), v.Max(x => x.z));
                    // 有坏顶点时 min/max 会顶到 ±1e38 这种量级 ⇒ 一眼就看出来
                    extra += $" · 顶点 min={mn.ToString("F3")} max={mx.ToString("F3")}";
                }
            }
            catch (Exception e) { extra += $" · 读顶点抛了：{e.GetType().Name}"; }
        }
        Debug.Log(Tag + $"{where}: `{m.name}` {extra}");
    }

    static void Shoot(string name, Mesh m)
    {
        if (m == null) return;
        Directory.CreateDirectory(OutDir);
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
            UnityEditor.SceneManagement.NewSceneMode.Single);
        var go = new GameObject("probe");
        go.AddComponent<MeshFilter>().sharedMesh = m;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mr.sharedMaterial.SetColor("_BaseColor", Color.white);
        if (mr.sharedMaterial.HasProperty("_BaseMap")) mr.sharedMaterial.SetTexture("_BaseMap", null);

        var b = m.bounds;
        float r = Mathf.Max(b.extents.magnitude, 0.001f) * 1.6f;
        var camGo = new GameObject("cam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.09f, 1f);
        camGo.transform.position = b.center + new Vector3(r * 0.9f, r * 0.7f, -r * 1.8f);
        camGo.transform.LookAt(b.center);
        cam.fieldOfView = 40f;

        var rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        cam.targetTexture = null;
        rt.Release();
        Debug.Log(Tag + $"渲了一张：`{OutDir}\\{name}.png`（相机框到 bounds 尺寸 {b.size.ToString("F3")}）");
    }

    public static void Run()
    {
        // ---- ① 工程 prefab 上那个 MeshFilter **实际引用**的网格 ----
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogError(Tag + $"找不到 `{PrefabPath}`"); return; }
        var t = prefab.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == TargetGo);
        if (t == null) { Debug.LogError(Tag + $"prefab 里没有 `{TargetGo}`"); return; }
        var mf = t.GetComponent<MeshFilter>();
        var expMesh = mf != null ? mf.sharedMesh : null;
        Report("① 我们 prefab 引用的网格", expMesh);
        Shoot("exp_" + (expMesh != null ? expMesh.name : "null"), expMesh);

        // ---- ② 原版 prefab 上同一个物体用的网格（⚠️ 不能按名字 `LoadAllAssets<Mesh>()` 捞：
        //        网格不是容器的根 ⇒ 捞不到；必须**从原版 prefab 的 MeshFilter 上取**）----
        var b = AssetBundle.LoadFromFile(Path.Combine(BundleDir, VfxBundleName));
        if (b == null) Debug.LogWarning(Tag + "源包加载不了（可能已被别人加载）");
        else
        {
            // ⚠️ `GetAllAssetNames()` 吐的是**容器键（GUID）**、不是资产名 ⇒ 只能加载出来再按 `name` 认。
            GameObject origPrefab = null;
            try
            {
                foreach (var g in b.LoadAllAssets<GameObject>())
                    if (g != null && g.name == "RemnantBody3D Aeldari") { origPrefab = g; break; }
            }
            catch (Exception e) { Debug.LogWarning(Tag + "枚举抛了：" + e.Message); }
            if (origPrefab == null) Debug.LogWarning(Tag + "源包里没取到原版 prefab（不在容器里？）");
            else
            {
                var ot = origPrefab.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == TargetGo);
                var omf = ot != null ? ot.GetComponent<MeshFilter>() : null;
                var om = omf != null ? omf.sharedMesh : null;
                Report("② 原版 prefab 上那个网格", om);
                Shoot("orig_" + (om != null ? om.name : "null"), om);

                // ---- ④⑤ 把「坏在哪一跳」钉死：`Instantiate` 出来（内存里）就坏，还是落盘才坏 ----
                if (om != null)
                {
                    var mem = UnityEngine.Object.Instantiate(om);
                    mem.name = "mem_copy";
                    Report("④ 只 `Instantiate`（还没落盘）", mem);
                    Shoot("mem_" + mem.name, mem);

                    Directory.CreateDirectory(@"d:\4\_tmp_view\meshprobe");
                    const string tmp = "Assets/WarpforgeVFX/Meshes/_probe_copy.asset";
                    AssetDatabase.DeleteAsset(tmp);
                    AssetDatabase.CreateAsset(mem, tmp);
                    AssetDatabase.SaveAssets();
                    var back = AssetDatabase.LoadAssetAtPath<Mesh>(tmp);
                    Report("⑤ 落盘再读回来", back);
                    Shoot("disk_" + (back != null ? back.name : "null"), back);
                    AssetDatabase.DeleteAsset(tmp);
                }
            }
        }

        // ---- ③ 工程 Meshes/ 下同名的那几个资产（都看一眼，坏的那个会露出来）----
        foreach (var n in MeshNames)
        {
            foreach (var guid in AssetDatabase.FindAssets(n + " t:Mesh", new[] { "Assets/WarpforgeVFX/Meshes" }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var m = AssetDatabase.LoadAssetAtPath<Mesh>(p);
                if (m == null || !Path.GetFileName(p).StartsWith(n)) continue;
                Report($"③ 工程资产 `{p}`", m);
                Shoot("proj_" + m.name.Replace(' ', '_'), m);
            }
        }
        Debug.Log(Tag + "结束");
    }
}
