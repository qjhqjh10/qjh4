// DistortProbe.cs — P1-a0 定位：原版的「抓屏扭曲」shader 到底往画面上写了什么？
//
// 背景：交接文档 P1-a0 的证据是「Spore Explosion @0.30s：原版 4612 亮点 / 导出 0」，
//       而 0.30s 时全场只有 1 个活粒子（在 BulletImpact/Distort 上）。原版 shader 的
//       HLSL 源码在打包时被剥掉了（`m_Script` 是 null）—— 但**字节码在**，
//       `Shader.compressedBlob` 解出来是 DXBC、资源名是明文，能查出它采哪张纹理。
//       本探针是在还没发现这一点时写的（当时误判「字节码也剥了」），保留它是因为
//       「实况渲染」和「读字节码」是两条独立的证据，互相印证才有说服力。
//
// 这个探针回答三个问题：
//   Q1 原版的输出**跟不跟背景走**？—— 跟 = 采样屏幕色；不跟 = 自己发光。
//      用三种纯色背景（黑/中灰/白）各渲一遍，看「相对空场景的贡献」变不变。
//   Q2 我们的输出跟不跟背景走？
//   Q3 导出的材质**混合状态**到底是什么？—— 原版材质上带着一批 Standard shader 的残留
//      （_SrcBlend=1 One / _DstBlend=0 Zero），而原版 shader 根本不声明这两个属性、
//      混合是写死在 pass 状态里的（SrcAlpha / OneMinusSrcAlpha）。我们的 WFDistortion
//      用的是 Blend [_SrcBlend][_DstBlend] 间接寻址 —— 一旦把残留值灌进来就变**不透明覆盖**。
//      这里把材质上真实生效的值打出来。
//
// 判据用两个数：
//   lit    = 和背景纯色不同的像素数（和交接文档里 4612/0 一个口径，便于对账）
//   contrib = 和「同一背景、不播效果」的空场景渲染不同的像素数（**效果真正的贡献**）
//
// 用法：
//   Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//     -executeMethod DistortProbe.Run -logFile "d:/4/_tmp_view/distort.log"
//   筛输出：grep "^DP " d:/4/_tmp_view/distort.log；图在 d:/4/_tmp_view/distort/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DistortProbe
{
    const string P = "DP ";
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string VfxBundleName = "battleprefabs_vfxandmisc_assets_all.bundle";
    const string PrefabDir = "Assets/WarpforgeVFX/Prefabs";
    const string OutDir = @"d:\4\_tmp_view\distort";

    const string Target = "Spore Explosion";
    const float SimTime = 0.30f;
    const int W = 512, H = 512;

    static readonly string[] BgNames = { "黑", "中灰", "白", "棋盘" };
    static readonly Color[] BgColors =
    {
        new Color(0f, 0f, 0f, 1f),
        new Color(0.5f, 0.5f, 0.5f, 1f),
        new Color(1f, 1f, 1f, 1f),
        new Color(0f, 0f, 0f, 1f),   // 棋盘：底色仍是黑，靠背后那块不透明 quad 提供内容
    };

    static Camera _cam;
    static readonly Vector3 Far = new Vector3(10000f, 0f, 0f);

    public static void Run()
    {
        Directory.CreateDirectory(OutDir);
        Debug.Log(P + "=== 抓屏扭曲定位探针 开始 ===");

        AssetBundle vfx = null;
        foreach (var f in Directory.GetFiles(BundleDir, "*.bundle"))
        {
            var b = AssetBundle.LoadFromFile(f);
            if (b != null && Path.GetFileName(f) == VfxBundleName) vfx = b;
        }
        if (vfx == null) { Debug.LogError(P + "特效 bundle 未加载"); return; }

        GameObject origPrefab = null;
        foreach (var n in vfx.GetAllAssetNames())
        {
            GameObject g = null;
            try { g = vfx.LoadAsset<GameObject>(n); } catch { }
            if (g != null && g.name == Target) origPrefab = g;
        }
        if (origPrefab == null) { Debug.LogError(P + $"原版没有 {Target}"); return; }

        var expPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{Target}.prefab");
        if (expPrefab == null) { Debug.LogError(P + $"导出没有 {Target}"); return; }

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Cam");
        _cam = camGo.AddComponent<Camera>();
        _cam.clearFlags = CameraClearFlags.SolidColor;

        var frame = FrameOn(origPrefab);
        ApplyFrame(frame);

        // 棋盘背景：一块**不透明**的 quad 放在效果后面。
        // 它和纯色 clear 的区别是关键的：`_CameraOpaqueTexture` 若不绑定，纯色背景和棋盘都采样不到；
        // 若绑定，扭曲 shader 应该把棋盘**错位地**画出来（不是一整块均匀色）
        var checker = MakeCheckerboard(frame);

        // ⚠️ 两个实例只建一次，靠「挪到画面外」隔离，**不能用 SetActive(false)**
        //    （停用再启用会把粒子系统重置，活粒子变 0 —— 见 CEmitProbe 的注释）
        var o = Make(origPrefab);
        o.name = "ORIG";
        var e = Make(expPrefab);
        e.name = "EXP";

        DumpMaterials(o, "原版 prefab");
        DumpMaterials(e, "导出 prefab");

        var om = Map(o);   // 原版材质
        var em = Map(e);   // 导出材质（binder 重建的）

        Assert(om, em);    // ← 这次的结论固化成断言，别再退回去

        // 四种组合：prefab(原版/导出) × 材质(原版/导出)
        var cases = new[]
        {
            new Case("原版prefab+原版材质", o, om),
            new Case("导出prefab+导出材质", e, em),
            new Case("导出prefab+原版材质", e, om),
            new Case("原版prefab+导出材质", o, em),
        };

        foreach (var bgIdx in Enumerable.Range(0, BgColors.Length))
        {
            _cam.backgroundColor = BgColors[bgIdx];
            checker.SetActive(bgIdx == 3);
            Debug.Log(P + $"########## 背景 = {BgNames[bgIdx]} {BgColors[bgIdx]} ##########");

            // 空场景基线：两个都挪开
            o.transform.position = Far; e.transform.position = Far;
            var empty = Render();

            foreach (var c in cases)
            {
                o.transform.position = Far; e.transform.position = Far;
                // 每次显式把「这一组要用的材质」换到主体身上 —— 上一轮可能换过，
                // 不还原的话下一轮的第一组会带着别人的材质（踩过）
                Swap(c.Subject, c.Mats);
                c.Subject.transform.position = Vector3.zero;

                var px = Render();
                int lit = CountDiff(px, BgColors[bgIdx]);
                var st = Stats(px, empty);
                Debug.Log(P + $"  {c.Name,-22} lit={lit,6}  contrib={st.count,6}"
                            + $"  改到的像素 RGB 均值=({st.mean.r:F3},{st.mean.g:F3},{st.mean.b:F3})"
                            + $" 最小=({st.min.r:F3},{st.min.g:F3},{st.min.b:F3})"
                            + $" 最大=({st.max.r:F3},{st.max.g:F3},{st.max.b:F3})");
                SavePng(px, $"{Target}_{BgNames[bgIdx]}_{c.Name}.png");
            }

            // 棋盘那一组额外做一遍「强制透明队列」：
            // URP 的 _CameraOpaqueTexture 是在**不透明 pass 之后**才拷的，材质若排在不透明队列里，
            // 它采样到的是没准备好的内容。排到透明队列(3000)才可能真的采到背后那张图 ——
            // 这一步用来判断「原版到底采不采屏幕色」。
            if (bgIdx == 3)
            {
                foreach (var m in om.Values) if (m != null) m.renderQueue = 3000;
                foreach (var m in em.Values) if (m != null) m.renderQueue = 3000;
                o.transform.position = Far; e.transform.position = Far;
                var empty2 = Render();
                Debug.Log(P + "  ---- 强制透明队列(t=3000) ----");
                foreach (var c in cases)
                {
                    o.transform.position = Far; e.transform.position = Far;
                    Swap(c.Subject, c.Mats);
                    c.Subject.transform.position = Vector3.zero;
                    var px = Render();
                    var st = Stats(px, empty2);
                    Debug.Log(P + $"  {c.Name,-22} 透明队列 contrib={st.count,6}"
                                + $" 均值=({st.mean.r:F3},{st.mean.g:F3},{st.mean.b:F3})"
                                + $" 最小=({st.min.r:F3},{st.min.g:F3},{st.min.b:F3})"
                                + $" 最大=({st.max.r:F3},{st.max.g:F3},{st.max.b:F3})");
                    SavePng(px, $"{Target}_棋盘_透明队列_{c.Name}.png");
                }
            }
        }

        // ---- 排查：原版读的到底是哪张屏幕贴图？----
        // 原版在我们这个场景里输出的是**恒定值**（不随背景、不随队列变），说明它采的那张全局贴图
        // 在本工程里**没被绑定**，硬件给的是默认灰贴图。这里把候选名字逐个设成一块**品红**贴图，
        // 哪个名字能让原版的输出变品红，就是它。
        // ✅ 2026-09-12 后来用 工具/dump_shader_blob.py 从字节码里查到了：
        //    原版采的是 **`_GrabPassTransparent`**（DXBC 资源名明文，而且**没有** `_CameraOpaqueTexture`）。
        //    当时这里写「查不到」是因为我误判字节码也被剥了。
        {
            _cam.backgroundColor = Color.black;
            checker.SetActive(false);
            o.transform.position = Far; e.transform.position = Far;

            var probe = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var fill = new Color[16];
            for (int i = 0; i < 16; i++) fill[i] = new Color(1f, 0f, 1f, 1f);
            probe.SetPixels(fill); probe.Apply();

            var candidates = new[]
            {
                "_CameraOpaqueTexture", "_CameraColorTexture", "_GrabTexture",
                "_SceneColorTex", "_ScreenTexture", "_DistortionSource", "_SceneColor", "_MainTex",
            };

            Debug.Log(P + "########## 候选屏幕贴图名排查（设成品红，看原版输出变不变）##########");
            foreach (var nm in candidates)
            {
                o.transform.position = Far; e.transform.position = Far;
                Swap(o, om); o.transform.position = Vector3.zero;
                var px = Render();
                var st0 = Stats(px, new Color[px.Length].Select(_ => Color.black).ToArray());

                Shader.SetGlobalTexture(nm, probe);
                var px2 = Render();
                var st1 = Stats(px2, new Color[px2.Length].Select(_ => Color.black).ToArray());
                Shader.SetGlobalTexture(nm, null);

                bool magenta = st1.count > 0 && st1.mean.r > 0.5f && st1.mean.b > 0.5f && st1.mean.g < 0.4f;
                Debug.Log(P + $"  设 {nm,-24} 前: {st0.count,6} 像素 / 均值=({st0.mean.r:F3},{st0.mean.g:F3},{st0.mean.b:F3})"
                            + $"   后: {st1.count,6} / ({st1.mean.r:F3},{st1.mean.g:F3},{st1.mean.b:F3})"
                            + (magenta ? "   ← **就是它**" : ""));
            }
            UnityEngine.Object.DestroyImmediate(probe);
        }

        UnityEngine.Object.DestroyImmediate(o);
        UnityEngine.Object.DestroyImmediate(e);
        Debug.Log(P + $"=== 断言：{_pass} 通过 / {_fail} 失败 ===");
        Debug.Log(P + "=== 结束 ===");
        if (_fail > 0) EditorApplication.Exit(1);
    }

    struct Stat { public int count; public Color mean, min, max; }

    static Stat Stats(Color[] px, Color[] baseline)
    {
        var st = new Stat { count = 0, mean = Color.black, min = Color.white, max = Color.black };
        double r = 0, g = 0, b = 0;
        var mn = new Color(9f, 9f, 9f); var mx = new Color(-9f, -9f, -9f);
        for (int i = 0; i < px.Length; i++)
        {
            var d = Mathf.Abs(px[i].r - baseline[i].r) + Mathf.Abs(px[i].g - baseline[i].g)
                  + Mathf.Abs(px[i].b - baseline[i].b);
            if (d <= Thr) continue;
            st.count++;
            r += px[i].r; g += px[i].g; b += px[i].b;
            mn = new Color(Mathf.Min(mn.r, px[i].r), Mathf.Min(mn.g, px[i].g), Mathf.Min(mn.b, px[i].b));
            mx = new Color(Mathf.Max(mx.r, px[i].r), Mathf.Max(mx.g, px[i].g), Mathf.Max(mx.b, px[i].b));
        }
        if (st.count > 0)
        {
            st.mean = new Color((float)(r / st.count), (float)(g / st.count), (float)(b / st.count));
            st.min = mn; st.max = mx;
        }
        else { st.min = Color.black; st.max = Color.black; }
        return st;
    }

    /// <summary>一块填满视野的**不透明**棋盘 quad，放在效果后面（进 _CameraOpaqueTexture 的那一层）</summary>
    static GameObject MakeCheckerboard(Bounds b)
    {
        const int N = 256, Cell = 32;
        var tex = new Texture2D(N, N, TextureFormat.RGB24, false);
        var px = new Color[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
                px[y * N + x] = ((x / Cell + y / Cell) % 2 == 0) ? Color.white : Color.black;
        tex.SetPixels(px); tex.Apply();

        var sh = Shader.Find("Universal Render Pipeline/Unlit");
        var mat = new Material(sh);
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Surface", 0f);      // Opaque
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_ZWrite", 1f);
        mat.renderQueue = 2000;            // 不透明 → 会进 _CameraOpaqueTexture

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "Checkerboard";
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        float radius = Mathf.Max(0.5f, b.extents.magnitude);
        const float fov = 40f;
        float dist = radius / Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f) * 1.15f;
        float d2 = dist + radius * 3f;
        float h = 2f * d2 * Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f);
        go.transform.position = b.center + new Vector3(0f, 0f, d2);
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = new Vector3(h * W / H, h, 1f);
        return go;
    }

    class Case
    {
        public readonly string Name;
        public readonly GameObject Subject;      // 谁留在画面里
        public readonly Dictionary<string, Material> Mats;
        public Case(string n, GameObject subject, Dictionary<string, Material> m)
        { Name = n; Subject = subject; Mats = m; }
    }

    // ---- 断言 ----
    static int _pass, _fail;
    /// <summary>「`_GrabPassTransparent` 填上了没有」只判一次（每帧都判会刷屏）</summary>
    static bool _checkedGrab;
    static void Check(bool ok, string what)
    {
        if (ok) { _pass++; Debug.Log(P + $"  ✅ {what}"); }
        else { _fail++; Debug.LogError(P + $"  ❌ {what}"); }
    }

    /// <summary>把 2026-09-12 定位到的结论固化成断言。**
    /// 背景：原版 `Everguild/FX/Particle Distortion Affect Transparents` 的**属性表里没有
    /// `_SrcBlend/_DstBlend/_ZWrite/_Surface`**，混合是写死在 pass 状态里的；而原版材质上
    /// 带着一批内置 Standard shader 的**残留值**（`_SrcBlend=1`/`_DstBlend=0`/`_ZWrite=1`）。
    /// 我们自建的那版曾经用 `Blend [_SrcBlend] [_DstBlend]` 间接寻址 → 残留值被灌进来 →
    /// 变成**不透明覆盖 + 写深度**（实测：棋盘背景上导出侧糊出一整块纯黑，把棋盘抠掉）。
    /// 所以这里断言「我们这边不能有这些属性」—— 有就说明间接寻址又回来了。</summary>
    static void Assert(Dictionary<string, Material> om, Dictionary<string, Material> em)
    {
        const string key = "BulletImpact/Distort";
        Material mo, me;
        if (!om.TryGetValue(key, out mo) || mo == null) { Check(false, $"原版没有 {key} 的材质"); return; }
        if (!em.TryGetValue(key, out me) || me == null) { Check(false, $"导出没有 {key} 的材质"); return; }

        Check(me.shader != null && me.shader.name == "WarpforgeVFX/FX/Distortion",
              $"{key} 走自建 shader（实际 {me.shader?.name}）");

        foreach (var n in new[] { "_SrcBlend", "_DstBlend", "_ZWrite", "_Surface", "_Blend", "_Cull" })
            Check(!me.HasProperty(n),
                  $"{key} 导出材质**不该有** {n} —— 原版把它写死在 pass 状态里，留着会被残留值污染");

        // 原版属性表（11 个）一个不能少，否则原版数值灌不进来
        foreach (var n in new[] { "_DistortionStrength", "_DistortTex", "_Depth_And_Fallof",
                                  "_SOFTPARTICLES", "_USEMASK", "_Mask", "_ANIMUVS",
                                  "_UVSpeed", "_UVScale", "_QueueOffset", "_QueueControl" })
            Check(me.HasProperty(n), $"{key} 自建 shader 有声明的原版属性 {n}");

        // 灌进来的数值要和原版一致
        Check(Mathf.Approximately(me.GetFloat("_DistortionStrength"), mo.GetFloat("_DistortionStrength")),
              $"_DistortionStrength 与原版一致（原版 {mo.GetFloat("_DistortionStrength")} / 导出 {me.GetFloat("_DistortionStrength")}）");
        foreach (var n in new[] { "_Depth_And_Fallof", "_UVSpeed", "_UVScale" })
        {
            var a = mo.GetVector(n); var b = me.GetVector(n);
            Check(Vector4.Distance(a, b) < 1e-4f, $"{n} 与原版一致（原版 {a} / 导出 {b}）");
        }
        var to = mo.GetTexture("_DistortTex"); var te = me.GetTexture("_DistortTex");
        Check(to != null && te != null && to.name == te.name,
              $"_DistortTex 与原版同图（原版 {(to ? to.name : "<null>")} / 导出 {(te ? te.name : "<null>")}）");

        // 关键字名必须和原版一样（原版是 `_SOFTPARTICLES`，**没有 `_ON` 后缀**）。
        // 名字写错的话 EnableKeyword 落空，等于这三个功能永远关着。
        foreach (var k in new[] { "_SOFTPARTICLES", "_USEMASK", "_ANIMUVS" })
        {
            me.EnableKeyword(k);
            Check(me.IsKeywordEnabled(k), $"自建 shader 认得关键字 {k}（名字照原版，无 _ON 后缀）");
            me.DisableKeyword(k);   // 别把后面渲染用的材质改脏（软粒子一开 alpha 会被深度差乘没）
        }
    }

    static void DumpMaterials(GameObject root, string tag)
    {
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r.sharedMaterials.Length == 0 || r.sharedMaterials[0] == null) continue;
            var m = r.sharedMaterials[0];
            string Get(string n) => m.HasProperty(n) ? m.GetFloat(n).ToString("F2") : "-";
            Debug.Log(P + $"  [{tag}] {PathOf(r.transform, root.transform),-32} "
                        + $"shader={m.shader.name} queue={m.renderQueue}"
                        + $" _SrcBlend={Get("_SrcBlend")} _DstBlend={Get("_DstBlend")}"
                        + $" _ZWrite={Get("_ZWrite")} _Surface={Get("_Surface")}"
                        + $" Strength={Get("_DistortionStrength")} SOFT={Get("_SOFTPARTICLES")}"
                        + $" ANIMUVS={Get("_ANIMUVS")} USEMASK={Get("_USEMASK")}");

            var ps = r.GetComponent<ParticleSystem>();
            if (ps != null)
                Debug.Log(P + $"        活粒子={ps.particleCount} 材质数={r.sharedMaterials.Length}");

            if (m.HasProperty("_DistortTex"))
            {
                var dt = m.GetTexture("_DistortTex");
                Debug.Log(P + $"        _DistortTex={(dt ? dt.name : "<null>")}"
                            + $" Scale={m.GetVector("_UVScale")} Speed={m.GetVector("_UVSpeed")}"
                            + $" DepthFalloff={m.GetVector("_Depth_And_Fallof")}");
            }
        }
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

    static Dictionary<string, Material> Map(GameObject root)
    {
        var d = new Dictionary<string, Material>();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            if (r.sharedMaterials.Length > 0)
                d[PathOf(r.transform, root.transform)] = r.sharedMaterials[0];
        return d;
    }

    static void Swap(GameObject root, Dictionary<string, Material> src)
    {
        if (root == null) return;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            Material m;
            if (!src.TryGetValue(PathOf(r.transform, root.transform), out m) || m == null) continue;
            var arr = r.sharedMaterials;
            for (int i = 0; i < arr.Length; i++) arr[i] = m;
            r.sharedMaterials = arr;
        }
    }

    static string PathOf(Transform t, Transform root)
    {
        var s = t.name;
        while (t.parent != null && t.parent != root) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    static Color[] Render()
    {
        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
        _cam.targetTexture = rt;
        _cam.Render();
        // 🆕 2026-09-13 第三十三轮：**确认那个全局纹理真的被填了**。
        //    `GrabPassTransparentFeature` 在透明物画完之后把它置 1；没挂那个 Feature 的场景是 0
        //    （着色器据此退回 `_CameraOpaqueTexture`）。**这是「原版读的那张屏幕贴图」接通了没有的唯一判据**，
        //    只看图看不出来（两边都是「有内容」）。
        var grabAvail = Shader.GetGlobalFloat("_GrabPassAvailable");
        if (!_checkedGrab)
        {
            _checkedGrab = true;
            Check(grabAvail > 0.5f,
                  $"**`_GrabPassTransparent` 被填上了**（`_GrabPassAvailable` = {grabAvail:F1}）—— "
                  + "P1-a0 的收尾：扭曲现在读的是**证实的**那张屏幕贴图（含透明物、全分辨率）");
        }
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        _cam.targetTexture = null;
        var px = tex.GetPixels();
        UnityEngine.Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        return px;
    }

    const float Thr = 6f / 255f * 3f;

    static int CountDiff(Color[] a, Color b)
    {
        int n = 0;
        foreach (var c in a)
            if (Mathf.Abs(c.r - b.r) + Mathf.Abs(c.g - b.g) + Mathf.Abs(c.b - b.b) > Thr) n++;
        return n;
    }

    static int CountDiff(Color[] a, Color[] b)
    {
        int n = 0;
        for (int i = 0; i < a.Length; i++)
            if (Mathf.Abs(a[i].r - b[i].r) + Mathf.Abs(a[i].g - b[i].g)
                + Mathf.Abs(a[i].b - b[i].b) > Thr) n++;
        return n;
    }

    static void SavePng(Color[] px, string name)
    {
        var t = new Texture2D(W, H, TextureFormat.RGB24, false);
        t.SetPixels(px);
        t.Apply();
        File.WriteAllBytes(Path.Combine(OutDir, name), t.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(t);
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
