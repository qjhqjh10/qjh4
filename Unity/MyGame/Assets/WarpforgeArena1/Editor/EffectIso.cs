// EffectIso.cs — 逐发射器隔离渲染：把某个效果的每个 ParticleSystemRenderer 单独渲染一次，
// 原版与导出各出一张图，用来定位是「哪个发射器 / 哪个材质」对不上。
//
// 用法：Unity.exe -batchmode -quit -projectPath ... -executeMethod EffectIso.Run -logFile -
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class EffectIso
{
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string VfxBundleName = "battleprefabs_vfxandmisc_assets_all.bundle";
    const string PrefabDir = "Assets/WarpforgeVFX/Prefabs";
    const string OutDir = @"d:\4\_tmp_view\iso";

    // 要隔离的效果
    static readonly string[] Targets = { "ArtificeEffect", "EnvironmentalCondition Tau Solar Eclipse",
        // 🆕 2026-09-17：C 组最后一个。渲染器层（`CEmitProbe` 五项全同）与材质层（`MatIsoProbe` 整体 0.0%）
        // 都已排除，粒子模块值（`ParticleModuleProbe`）也**逐字段 0 处不同** —— 那三个探针**都是从「原版 prefab」
        // 出发换零件**，从来没直接比过「导出 prefab 原样渲出来」。逐发射器隔离就是为了回答「**哪个发射器暗**」。
        "PinDownEffect" };

    // 关掉诊断阶段：跑得多的时候只保留「隔离渲染 + 逐项 shader 对照」
    static readonly bool RunDiagnostics = true;

    const int W = 512, H = 512;
    const float SimTime = 1.2f;

    /// <summary>隔离渲染用的临时图层（相机只渲染它）</summary>
    const int IsolateLayer = 30;

    static void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        foreach (Transform c in t) SetLayerRecursive(c, layer);
    }

    /// <summary>图层 30 可能是空的，用之前先确保它存在且命名过（否则 cullingMask 无效）</summary>
    static void EnsureIsolateLayer()
    {
        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (asset == null || asset.Length == 0) { Debug.LogWarning("读不到 TagManager，图层可能无效"); return; }
        var so = new SerializedObject(asset[0]);
        var layers = so.FindProperty("layers");
        if (layers == null || layers.arraySize <= IsolateLayer) { Debug.LogWarning("layers 数组长度不足"); return; }
        var slot = layers.GetArrayElementAtIndex(IsolateLayer);
        if (string.IsNullOrEmpty(slot.stringValue))
        {
            slot.stringValue = "WarpforgeIso";
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log($"已启用图层 {IsolateLayer} = WarpforgeIso");
        }
    }

    public static void Run()
    {
        Debug.Log("=== 逐发射器隔离渲染 开始 ===");
        Directory.CreateDirectory(OutDir);
        EnsureIsolateLayer();

        // 只加载特效包（不要加载全部 84 个包：那样会把原版 shader 包也拉进内存，
        // 抢掉 StreamingAssets 里那份 wf_shaders.bundle 的加载，binder 就只能退回 Shader.Find）
        AssetBundle vfx = null;
        var vfxPath = Path.Combine(BundleDir, VfxBundleName);
        if (File.Exists(vfxPath)) vfx = AssetBundle.LoadFromFile(vfxPath);
        if (vfx == null) { Debug.LogError($"特效 bundle 未加载: {vfxPath}"); return; }
        Debug.Log($"binder shader 解析来源: {WarpforgeVFX.WarpforgeShaderMap.Describe()}");

        var originals = new Dictionary<string, GameObject>();
        foreach (var n in vfx.GetAllAssetNames())
        {
            GameObject g = null;
            try { g = vfx.LoadAsset<GameObject>(n); } catch { }
            if (g != null) originals[g.name] = g;
        }

        foreach (var name in Targets)
        {
            if (!originals.TryGetValue(name, out var orig)) { Debug.LogError($"原版里找不到 {name}"); continue; }
            var exp = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");
            if (exp == null) { Debug.LogError($"导出 prefab 不存在: {name}"); continue; }

            ListBundleShaders();

            // 相机取景用原版的整体包围盒（与 EffectCompare 一致，保证两边可比）
            var cam = FrameCamera(orig);

            // 先把两边的「运行时材质」逐属性 diff 一遍（比看图快得多）
            if (RunDiagnostics) CompareMaterials(orig, exp);

            var origRends = Collect(orig);
            var expRends = Collect(exp);
            Debug.Log($"[{name}] 原版渲染器 {origRends.Count} 个，导出渲染器 {expRends.Count} 个");

            if (RunDiagnostics) CompareParticleSystems(orig, exp);

            // A/B：把「原版 bundle 材质」贴到导出 prefab 的对应渲染器上。
            //   亮了 → 问题在导出材质（贴图/属性/变体）
            //   还黑 → 问题在 prefab 本体或渲染环境
            if (RunDiagnostics) TestCrossMaterial(name, orig, exp);

            // 定向实验：导出材质缺的几个属性，逐个补上试
            if (RunDiagnostics) TestPropertyFixups(name, orig, exp);

            ShaderProbe(name, orig, cam);

            // 多个发射器组合渲染：品红只在「不止一个发射器」时出现，逐个点亮找组合
            RenderMulti(orig, cam, $"{OutDir}/{name}__multi_ALL.png", null);
            RenderMulti(orig, cam, $"{OutDir}/{name}__multi_仅Main.png", r => r.name == "Lightning Main");
            RenderMulti(orig, cam, $"{OutDir}/{name}__multi_除Main.png", r => r.name != "Lightning Main");

            for (int i = 0; i < Math.Max(origRends.Count, expRends.Count); i++)
            {
                var o = i < origRends.Count ? origRends[i] : null;
                var e = i < expRends.Count ? expRends[i] : null;
                var any = o ?? e;
                string label = Sanitize(PathOf(any.transform, any.transform.root)) + $"#{i}";

                RenderIsolated(orig, o, $"{OutDir}/{name}_{i:00}_{label}__orig.png", cam, true);
                RenderIsolated(exp, e, $"{OutDir}/{name}_{i:00}_{label}__exp.png", cam, false);
                Debug.Log($"  [{i}] {label}  原版={(o == null ? "-" : ShaderOf(o))}  导出={(e == null ? "-" : ShaderOf(e))}");

                // 「原版材质 + 工程自带的同名 shader」：用来判断原版渲染里的品红块
                // 到底是「bundle 材质坏了」还是「工程 shader 渲染 bundle 材质坏了」
                if (o != null)
                {
                    var om = o.sharedMaterials.Length > 0 ? o.sharedMaterials[0] : null;
                    if (om != null && om.shader != null)
                    {
                        var builtin = Shader.Find(om.shader.name);
                        if (builtin != null && builtin != om.shader)
                            RenderCross(orig, o, m => m.shader = builtin, cam,
                                        $"{OutDir}/{name}_{i:00}_{label}__origx工程shader.png");
                    }
                }
            }
        }
        Debug.Log("=== 逐发射器隔离渲染 结束 ===");
    }

    /// <summary>把原版材质分别配上「bundle shader / 工程 shader / 导出材质」渲染，四格并排</summary>
    static void ShaderProbe(string name, GameObject orig, CamFrame cam)
    {
        var oR = orig.GetComponentsInChildren<Renderer>(true);
        Renderer target = null;
        foreach (var r in oR)
            if (r.name == "Lightning Main") { target = r; break; }
        if (target == null) { Debug.LogWarning("    找不到 Lightning Main"); return; }
        var om = target.sharedMaterials.Length > 0 ? target.sharedMaterials[0] : null;
        if (om == null) { Debug.LogWarning("    Lightning Main 没有材质"); return; }

        Debug.Log($"    [shader探针] 原版材质「{om.name}」 shader=「{om.shader.name}」 id={om.shader.GetInstanceID()}");
        var projSh = Shader.Find(om.shader.name);
        Debug.Log($"    [shader探针] Shader.Find 拿到 id={(projSh == null ? -1 : projSh.GetInstanceID())} " +
                  $"{(projSh == om.shader ? "（就是同一个）" : "（另一个对象）")}");

        RenderMulti(orig, cam, $"{OutDir}/{name}__probe_1_bundleShader.png", r => r.name == "Lightning Main");
        RenderCross(orig, target, m => m.shader = projSh, cam, $"{OutDir}/{name}__probe_2_工程Shader.png");
        RenderCross(orig, target, m => { }, cam, $"{OutDir}/{name}__probe_3_原样.png");

        // 导出侧：binder 重建的材质是什么 shader
        var exp = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");
        if (exp != null)
        {
            var inst = UnityEngine.Object.Instantiate(exp);
            var b = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
            if (b != null) b.Apply();
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                if (r.name == "Lightning Main" && r.sharedMaterials.Length > 0 && r.sharedMaterials[0] != null)
                {
                    var m = r.sharedMaterials[0];
                    Debug.Log($"    [shader探针] 导出材质「{m.name}」 shader=「{m.shader.name}」 id={m.shader.GetInstanceID()}");
                    break;
                }
            UnityEngine.Object.DestroyImmediate(inst);
        }
    }

    /// <summary>同时点亮匹配的渲染器（keep==null 表示全亮），其余用图层遮罩挡掉</summary>
    static void RenderMulti(GameObject prefab, CamFrame frame, string outPath, Func<Renderer, bool> keep)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var inst = UnityEngine.Object.Instantiate(prefab);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;

        var binder = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();

        foreach (var t in inst.GetComponentsInChildren<Transform>(true)) SetLayerRecursive(t, 0);
        foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            if (keep == null || keep(r)) SetLayerRecursive(r.transform, IsolateLayer);

        foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Simulate(SimTime, withChildren: true, restart: true, fixedTimeStep: false);
            ps.Play();
        }
        foreach (var sm in inst.GetComponentsInChildren<SpriteMask>(true)) sm.enabled = false;

        var camGo = new GameObject("Cam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.08f, 0.10f, 1f);
        cam.fieldOfView = frame.fov;
        cam.nearClipPlane = frame.near;
        cam.farClipPlane = frame.far;
        camGo.transform.position = frame.pos;
        camGo.transform.LookAt(frame.look);
        cam.cullingMask = 1 << IsolateLayer;

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

    /// <summary>列出 shader bundle 里的全部 shader，确认标准 URP shader 在不在里面</summary>
    static void ListBundleShaders()
    {
        var names = new List<string>();
        foreach (var n in WarpforgeVFX.WarpforgeShaderLoader.ShaderNames) names.Add(n);
        Debug.Log($"    [shader bundle] {names.Count} 个 shader：" +
                  "\n      " + string.Join("\n      ", names.OrderBy(x => x)));
    }

    /// <summary>按「渲染器遍历顺序」收集，导出侧必须与 binder 的 rendererSlots 同序</summary>
    static List<Renderer> Collect(GameObject root)
        => root.GetComponentsInChildren<Renderer>(true).ToList();

    static string ShaderOf(Renderer r)
    {
        var m = r.sharedMaterials.Length > 0 ? r.sharedMaterials[0] : null;
        return m == null || m.shader == null ? "<null>" : m.shader.name;
    }

    /// <summary>把原版与导出「binder 重建之后」的材质逐属性、逐关键字比一遍。
    /// 属性名两边一致（自建 shader 是照着原版属性名做的），所以可以直接按名字对齐。</summary>
    static void CompareMaterials(GameObject orig, GameObject exp)
    {
        var oi = UnityEngine.Object.Instantiate(orig);
        var ei = UnityEngine.Object.Instantiate(exp);
        var eb = ei.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
        if (eb != null) eb.Apply();

        var oR = oi.GetComponentsInChildren<Renderer>(true);
        var eR = ei.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < Math.Min(oR.Length, eR.Length); i++)
        {
            var om = oR[i].sharedMaterials.Length > 0 ? oR[i].sharedMaterials[0] : null;
            var em = eR[i].sharedMaterials.Length > 0 ? eR[i].sharedMaterials[0] : null;
            if (om == null || em == null) continue;
            if (om.shader == null || em.shader == null) continue;

            var diffs = new List<string>();
            if (om.shader.name != em.shader.name) diffs.Add($"shader: 原「{om.shader.name}」≠ 导「{em.shader.name}」");
            if (om.renderQueue != em.renderQueue) diffs.Add($"renderQueue: 原{om.renderQueue} ≠ 导{em.renderQueue}");

            // 关键字：用 shader 自己声明的关键字集合取并集来比
            var kws = new HashSet<string>();
            foreach (var kn in om.shader.keywordSpace.keywordNames) kws.Add(kn);
            foreach (var kn in em.shader.keywordSpace.keywordNames) kws.Add(kn);
            foreach (var k in kws)
            {
                bool a = false, b = false;
                try { a = om.IsKeywordEnabled(k); } catch { }
                try { b = em.IsKeywordEnabled(k); } catch { }
                if (a != b) diffs.Add($"关键字 {k}: 原={a} 导={b}");
            }

            int n = om.shader.GetPropertyCount();
            for (int p = 0; p < n; p++)
            {
                var pn = om.shader.GetPropertyName(p);
                var t = om.shader.GetPropertyType(p);
                string a, b;
                try
                {
                    switch (t)
                    {
                        case ShaderPropertyType.Color:   a = om.GetColor(pn).ToString("F4"); b = em.HasProperty(pn) ? em.GetColor(pn).ToString("F4") : "<无此属性>"; break;
                        case ShaderPropertyType.Vector:  a = om.GetVector(pn).ToString("F4"); b = em.HasProperty(pn) ? em.GetVector(pn).ToString("F4") : "<无此属性>"; break;
                        case ShaderPropertyType.Float:
                        case ShaderPropertyType.Range:   a = om.GetFloat(pn).ToString("F4"); b = em.HasProperty(pn) ? em.GetFloat(pn).ToString("F4") : "<无此属性>"; break;
                        case ShaderPropertyType.Int:     a = om.GetInt(pn).ToString(); b = em.HasProperty(pn) ? em.GetInt(pn).ToString() : "<无此属性>"; break;
                        case ShaderPropertyType.Texture:
                            var ta = om.GetTexture(pn); var tb = em.HasProperty(pn) ? em.GetTexture(pn) : null;
                            a = ta == null ? "<null>" : ta.name;
                            b = tb == null ? "<null>" : tb.name;
                            break;
                        default: continue;
                    }
                }
                catch { continue; }
                if (a != b) diffs.Add($"属性 {pn}: 原={a} 导={b}");
            }

            if (diffs.Count == 0)
                Debug.Log($"    [材质一致] {oR[i].name}「{om.name}」");
            else
                Debug.Log($"    [材质差异] {oR[i].name}「{om.name}」\n      " + string.Join("\n      ", diffs));

            // Back Glow 这种「一个发射器整块不出」的情况，光比属性不够，
            // 把两边材质上「shader 声明过、且当前是开的」关键字全列出来
            var oOn = OnKeywords(om); var eOn = OnKeywords(em);
            Debug.Log($"    [关键字] {oR[i].name}「{om.name}」\n      原: {oOn}\n      导: {eOn}");

            // 上面对比用的是 shader.GetPropertyCount()（只列 shader 声明的属性）。
            // 运行时设置过的、但没在 Properties 块里声明的（比如 URP 粒子自己塞的）
            // 这里用 GetPropertyNames() 再扫一遍，两边都打出来人工比对
            var oAll = AllProps(om); var eAll = AllProps(em);
            if (oAll != eAll)
                Debug.Log($"    [额外属性差异] {oR[i].name}「{om.name}」\n      原: {oAll}\n      导: {eAll}");
        }
        UnityEngine.Object.DestroyImmediate(oi);
        UnityEngine.Object.DestroyImmediate(ei);
    }

    static string OnKeywords(Material m)
    {
        var on = new List<string>();
        foreach (var k in m.shader.keywordSpace.keywordNames)
            try { if (m.IsKeywordEnabled(k)) on.Add(k); } catch { }
        return string.Join(",", on);
    }

    /// <summary>逐模块比两边的粒子系统。材质已经确认一致了，剩下的差异只能出在这里。</summary>
    static void CompareParticleSystems(GameObject orig, GameObject exp)
    {
        var oi = UnityEngine.Object.Instantiate(orig);
        var ei = UnityEngine.Object.Instantiate(exp);
        var oP = oi.GetComponentsInChildren<ParticleSystem>(true);
        var eP = ei.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < Math.Min(oP.Length, eP.Length); i++)
        {
            var a = oP[i]; var b = eP[i];
            var d = new List<string>();
            var am = a.main; var bm = b.main;
            void Cmp(string what, object x, object y)
            { if (!Equals(x, y)) d.Add($"{what}: 原={x} 导={y}"); }

            Cmp("duration", am.duration, bm.duration);
            Cmp("loop", am.loop, bm.loop);
            Cmp("prewarm", am.prewarm, bm.prewarm);
            Cmp("startDelay", $"{am.startDelay.mode}/{am.startDelay.constant}", $"{bm.startDelay.mode}/{bm.startDelay.constant}");
            Cmp("startLifetime", $"{am.startLifetime.mode}/{am.startLifetime.constant}", $"{bm.startLifetime.mode}/{bm.startLifetime.constant}");
            Cmp("startSpeed", $"{am.startSpeed.mode}/{am.startSpeed.constant}", $"{bm.startSpeed.mode}/{bm.startSpeed.constant}");
            Cmp("startSize", $"{am.startSize.mode}/{am.startSize.constant}", $"{bm.startSize.mode}/{bm.startSize.constant}");
            Cmp("startRotation", $"{am.startRotation.mode}/{am.startRotation.constant}", $"{bm.startRotation.mode}/{bm.startRotation.constant}");
            Cmp("startColor", $"{am.startColor.mode}/{am.startColor.color}", $"{bm.startColor.mode}/{bm.startColor.color}");
            Cmp("gravityModifier", am.gravityModifier.constant, bm.gravityModifier.constant);
            Cmp("simulationSpace", am.simulationSpace, bm.simulationSpace);
            Cmp("scalingMode", am.scalingMode, bm.scalingMode);
            Cmp("playOnAwake", am.playOnAwake, bm.playOnAwake);
            Cmp("maxParticles", am.maxParticles, bm.maxParticles);
            Cmp("emission.enabled", a.emission.enabled, b.emission.enabled);
            Cmp("emission.rateOverTime", $"{a.emission.rateOverTime.mode}/{a.emission.rateOverTime.constant}", $"{b.emission.rateOverTime.mode}/{b.emission.rateOverTime.constant}");
            Cmp("emission.burstCount", a.emission.burstCount, b.emission.burstCount);
            for (int k = 0; k < Math.Min(a.emission.burstCount, b.emission.burstCount); k++)
            {
                Cmp($"burst[{k}].count", a.emission.GetBurst(k).count.constant, b.emission.GetBurst(k).count.constant);
                Cmp($"burst[{k}].time", a.emission.GetBurst(k).time, b.emission.GetBurst(k).time);
            }
            Cmp("shape.enabled", a.shape.enabled, b.shape.enabled);
            Cmp("shape.shapeType", a.shape.shapeType, b.shape.shapeType);
            Cmp("shape.radius", a.shape.radius, b.shape.radius);
            Cmp("colorOverLifetime", a.colorOverLifetime.enabled, b.colorOverLifetime.enabled);
            Cmp("sizeOverLifetime", a.sizeOverLifetime.enabled, b.sizeOverLifetime.enabled);
            Cmp("velocityOverLifetime", a.velocityOverLifetime.enabled, b.velocityOverLifetime.enabled);
            Cmp("noise", a.noise.enabled, b.noise.enabled);
            Cmp("trails", a.trails.enabled, b.trails.enabled);
            var ar = a.GetComponent<ParticleSystemRenderer>();
            var br = b.GetComponent<ParticleSystemRenderer>();
            if (ar != null && br != null)
            {
                Cmp("renderMode", ar.renderMode, br.renderMode);
                Cmp("alignment", ar.alignment, br.alignment);
                Cmp("sortingOrder", ar.sortingOrder, br.sortingOrder);
                Cmp("pivot", ar.pivot, br.pivot);
            }

            if (d.Count == 0) Debug.Log($"    [PS一致] {PathOf(a.transform, oi.transform)}");
            else Debug.Log($"    [PS差异] {PathOf(a.transform, oi.transform)}\n      " + string.Join("\n      ", d));
        }
        UnityEngine.Object.DestroyImmediate(oi);
        UnityEngine.Object.DestroyImmediate(ei);
    }

    /// <summary>交叉材质测试：原版材质贴到导出 prefab 上渲染。
    /// 用来切开「材质不对」和「prefab / 渲染环境不对」这两类原因。</summary>
    static void TestCrossMaterial(string name, GameObject orig, GameObject exp)
    {
        var cam = FrameCamera(orig);
        var oR = orig.GetComponentsInChildren<Renderer>(true);
        var eR = exp.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < Math.Min(oR.Length, eR.Length); i++)
        {
            var om = oR[i].sharedMaterials.Length > 0 ? oR[i].sharedMaterials[0] : null;
            if (om == null || om.shader == null) continue;
            string face = Sanitize(PathOf(eR[i].transform, exp.transform));

            // ① 导出 prefab + 原版材质
            RenderCross(exp, eR[i], m => CopyFrom(m, om), cam, $"{OutDir}/{name}_cross{i:00}_{face}__exp_with_ORIG_material.png");

            // ② 原版 prefab + 导出 prefab 上的占位材质
            var em = eR[i].sharedMaterials.Length > 0 ? eR[i].sharedMaterials[0] : null;
            if (em != null)
                RenderCross(orig, oR[i], m => CopyFrom(m, em), cam, $"{OutDir}/{name}_cross{i:00}_{face}__orig_with_EXP_material.png");
        }
        Debug.Log($"    [交叉测试] 见 {OutDir} 下的 *_cross*.png");
    }

    /// <summary>把 src 的全部属性搬到 dst 上（两边属性名一致，逐名照搬）</summary>
    static void CopyFrom(Material dst, Material src)
    {
        if (src.shader == null) return;
        int n = src.shader.GetPropertyCount();
        for (int p = 0; p < n; p++)
        {
            var pn = src.shader.GetPropertyName(p);
            if (!dst.HasProperty(pn)) continue;
            try
            {
                switch (src.shader.GetPropertyType(p))
                {
                    case ShaderPropertyType.Color:  dst.SetColor(pn, src.GetColor(pn)); break;
                    case ShaderPropertyType.Vector: dst.SetVector(pn, src.GetVector(pn)); break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:  dst.SetFloat(pn, src.GetFloat(pn)); break;
                    case ShaderPropertyType.Int:    dst.SetInt(pn, src.GetInt(pn)); break;
                    case ShaderPropertyType.Texture:
                        var t = src.GetTexture(pn);
                        if (t != null) dst.SetTexture(pn, t);
                        break;
                }
            }
            catch { }
        }
        dst.renderQueue = src.renderQueue;
        foreach (var k in src.shaderKeywords) dst.EnableKeyword(k);
    }

    /// <summary>定向实验：导出材质比原版少了几个属性（导出侧是 0，原版不是）。
    /// 逐个单独补上渲染一张，看哪个是「整块不出」的致命项。</summary>
    static void TestPropertyFixups(string name, GameObject orig, GameObject exp)
    {
        var cam = FrameCamera(orig);
        var oR = orig.GetComponentsInChildren<Renderer>(true);
        var eR = exp.GetComponentsInChildren<Renderer>(true);
        var probe = new[] { "_CameraFadeParams", "_BaseColorAddSubDiff", "_SoftParticleFadeParams" };

        for (int i = 0; i < Math.Min(oR.Length, eR.Length); i++)
        {
            var om = oR[i].sharedMaterials.Length > 0 ? oR[i].sharedMaterials[0] : null;
            var em = eR[i].sharedMaterials.Length > 0 ? eR[i].sharedMaterials[0] : null;
            if (om == null || em == null || om.shader == null || em.shader == null) continue;
            if (om.shader.name != em.shader.name) continue;    // 只试 shader 相同的，差异才归因到属性

            string face = Sanitize(PathOf(eR[i].transform, exp.transform));
            RenderCross(exp, eR[i], m => { }, cam, $"{OutDir}/{name}_fix{i:00}_{face}__base.png");

            foreach (var pn in probe)
            {
                if (!om.HasProperty(pn) || !em.HasProperty(pn)) continue;
                var v = om.GetVector(pn);
                if (em.GetVector(pn) == v) continue;
                var captured = v;
                RenderCross(exp, eR[i], m => m.SetVector(pn, captured), cam,
                            $"{OutDir}/{name}_fix{i:00}_{face}__set{pn}.png");
            }

            RenderCross(exp, eR[i], m =>
            {
                foreach (var pn in probe)
                    if (om.HasProperty(pn) && m.HasProperty(pn)) m.SetVector(pn, om.GetVector(pn));
            }, cam, $"{OutDir}/{name}_fix{i:00}_{face}__setALL.png");

            // 附加探针：区分「binder 重建的材质本身不对」和「shader 变体/关键字在编辑器下失效」
            var eSh = em.shader;
            var oSh = om.shader;
            if (eSh != oSh)
                RenderCross(exp, eR[i], m => m.shader = oSh, cam,
                            $"{OutDir}/{name}_fix{i:00}_{face}__用原版shader.png");

            // URP 粒子 shader 的「派生属性」：原版材质里没序列化（引擎每次渲染现算），
            // binder 建新材质拿到的是默认值 0。逐个单独补，定位哪几个是致命的。
            if (eSh.name.Contains("Particles/Unlit"))
            {
                RenderCross(exp, eR[i], m => m.SetVectorSafe("_CameraFadeParams", new Vector4(0, 2000f, 0, 0)), cam,
                            $"{OutDir}/{name}_fix{i:00}_{face}__CamFadeInf.png");
                RenderCross(exp, eR[i], m => m.SetFloatSafe("_SoftParticlesEnabled", 0f), cam,
                            $"{OutDir}/{name}_fix{i:00}_{face}__SoftOff.png");
                RenderCross(exp, eR[i], m => m.SetVectorSafe("_SoftParticleFadeParams", new Vector4(0, 1, 0, 0)), cam,
                            $"{OutDir}/{name}_fix{i:00}_{face}__SoftFade01.png");
                RenderCross(exp, eR[i], m => m.SetVectorSafe("_BaseColorAddSubDiff", new Vector4(1, 0, 0, 0)), cam,
                            $"{OutDir}/{name}_fix{i:00}_{face}__AddSubDiffMul.png");
            }
        }

        // 并排对照：左 = binder 材质，右 = 原版 bundle 材质（用同一个 prefab）
        SideBySide(name, orig, exp, cam);
        Debug.Log($"    [定向实验] 见 {OutDir} 下的 *_fix*.png");
    }

    /// <summary>实例化 prefab、只渲染 keep、并对它当前的材质做一次 modify 后渲染</summary>
    static void RenderCross(GameObject prefab, Renderer keep, Action<Material> modify, CamFrame frame, string outPath)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var inst = UnityEngine.Object.Instantiate(prefab);
        var keepT = FindMatching(inst, keep);
        if (keepT == null) return;

        foreach (var t in inst.GetComponentsInChildren<Transform>(true))
            if (t == keepT) SetLayerRecursive(t, IsolateLayer);

        var kr = keepT.GetComponent<Renderer>();
        if (kr != null)
        {
            // 复制一份材质再改，别污染资产
            var arr = kr.sharedMaterials;
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] != null)
                {
                    var copy = new Material(arr[i]);
                    modify(copy);
                    arr[i] = copy;
                }
            kr.sharedMaterials = arr;
        }

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
        cam.cullingMask = 1 << IsolateLayer;

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

    /// <summary>把某个实例里 Back Glow 的材质全量属性打出来，用于逐项对照</summary>
    static void DumpBackGlow(string tag, GameObject inst)
    {
        foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.name.Contains("Back Glow")) continue;
            if (r.sharedMaterials.Length == 0 || r.sharedMaterials[0] == null) { Debug.Log($"    [dump {tag}] Back Glow 无材质"); return; }
            var m = r.sharedMaterials[0];
            Debug.Log($"    [dump {tag}] 「{m.name}」 shader={m.shader.name} queue={m.renderQueue}" +
                      $"\n      {AllProps(m)}" +
                      $"\n      关键字: {OnKeywords(m)}");
            return;
        }
    }

    static string AllProps(Material m)
    {
        var parts = new List<string>();
        var seen = new HashSet<string>();
        try
        {
            var names = new List<string>();
            names.AddRange(m.GetPropertyNames(MaterialPropertyType.Float));
            names.AddRange(m.GetPropertyNames(MaterialPropertyType.Vector));
            names.AddRange(m.GetPropertyNames(MaterialPropertyType.Texture));
            names.AddRange(m.GetPropertyNames(MaterialPropertyType.Int));
            foreach (var pn in names)
            {
                if (!seen.Add(pn)) continue;
                string v;
                try
                {
                    if (m.HasColor(pn)) v = m.GetColor(pn).ToString("F4");
                    else if (m.HasVector(pn)) v = m.GetVector(pn).ToString("F4");
                    else if (m.HasFloat(pn)) v = m.GetFloat(pn).ToString("F4");
                    else if (m.HasTexture(pn))
                    {
                        var t = m.GetTexture(pn);
                        v = t == null ? "<null>" : t.name;
                    }
                    else v = "?";
                }
                catch { v = "<err>"; }
                parts.Add($"{pn}={v}");
            }
        }
        catch { }
        parts.Sort();
        return string.Join(" ", parts);
    }

    static string DumpMat(Material m)
    {
        var parts = new List<string>();
        foreach (var pn in new[] { "_MainTex", "_Color", "_RendererColor", "_Flip", "_AlphaTex", "_EnableExternalAlpha", "_BaseMap", "_BaseColor" })
        {
            if (!m.HasProperty(pn)) continue;
            string v;
            try
            {
                if (m.HasTexture(pn)) { var t = m.GetTexture(pn); v = t == null ? "<null>" : t.name; }
                else if (m.HasColor(pn)) v = m.GetColor(pn).ToString("F3");
                else if (m.HasVector(pn)) v = m.GetVector(pn).ToString("F3");
                else v = m.GetFloat(pn).ToString("F3");
            }
            catch { v = "?"; }
            parts.Add($"{pn}={v}");
        }
        return string.Join(" ", parts);
    }

    static void RenderIsolated(GameObject prefab, Renderer keep, string outPath, CamFrame frame, bool isOriginal)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var inst = UnityEngine.Object.Instantiate(prefab);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;

        // 材质重建必须在裁剪/屏蔽之前做：rendererSlots 是按全量遍历顺序排的
        var binder = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();

        // 看一眼 binder 之后每个渲染器实际拿到的材质，排查「材质串位」
        var assign = new List<string>();
        foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
        {
            var mm = r.sharedMaterials.Length > 0 ? r.sharedMaterials[0] : null;
            assign.Add($"{r.name}→{(mm == null ? "null" : mm.name)}/{((mm == null || mm.shader == null) ? "null" : mm.shader.name)}");
        }
        Debug.Log($"    [{(isOriginal ? "原版" : "导出")}] 材质绑定: {string.Join(" | ", assign)}");

        // 隔离用图层遮罩（相机只渲染 IsolateLayer），不用 forceRenderingOff / Renderer.enabled。
        // 后两者都会连带影响粒子的发射，隔离图会假性全空 —— 排查时被这个坑过一次。
        if (keep != null)
        {
            var keepT = FindMatching(inst, keep);
            if (keepT == null) Debug.LogWarning($"隔离失败：找不到 {keep.name}");
            else
            {
                int moved = 0;
                foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                    if (t == keepT) { SetLayerRecursive(t, IsolateLayer); moved++; }
                Debug.Log($"    隔离 {keep.name}: 移到图层 {IsolateLayer}（{moved} 个节点）");
            }
        }

        foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Simulate(SimTime, withChildren: true, restart: true, fixedTimeStep: false);
            ps.Play();
        }
        // 粒子数：用于区分「没渲染出来」和「根本没发射」
        var counts = new List<string>();
        foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
            counts.Add($"{ps.name}={ps.particleCount}");
        Debug.Log($"    粒子数: {string.Join(" ", counts)}");

        // 隔离渲染必须关掉精灵遮罩，否则 SpriteMask 会把画面裁掉
        foreach (var sm in inst.GetComponentsInChildren<SpriteMask>(true)) sm.enabled = false;

        // 目标渲染器的粒子数与包围盒：判断「没渲染」还是「没发射」
        foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name != "Back Glow" && r.name != "Trait Icon") continue;
            var psx = r.GetComponent<ParticleSystem>();
            Debug.Log($"    [probe {r.name}] activeInHierarchy={r.gameObject.activeInHierarchy} " +
                      $"rendererEnabled={r.enabled} layer={r.gameObject.layer} " +
                      $"particles={(psx == null ? -1 : psx.particleCount)} " +
                      $"bounds(c={r.bounds.center} s={r.bounds.size})");
        }

        // 把「谁拿到哪个 shader」打出来 —— 品红问题关键就在这儿
        var who = new List<string>();
        foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
        {
            var mm = r.sharedMaterials.Length > 0 ? r.sharedMaterials[0] : null;
            if (mm == null || mm.shader == null) continue;
            who.Add($"{r.name}={mm.shader.name}#{mm.shader.GetInstanceID()}");
            Debug.Log($"    [mat {r.name}] {DumpMat(mm)}");
        }
        Debug.Log($"    [iso {Path.GetFileNameWithoutExtension(outPath)}] {string.Join(" ", who)}");

        var camGo = new GameObject("Cam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.08f, 0.10f, 1f);
        cam.fieldOfView = frame.fov;
        cam.nearClipPlane = frame.near;
        cam.farClipPlane = frame.far;
        camGo.transform.position = frame.pos;
        camGo.transform.LookAt(frame.look);
        cam.cullingMask = 1 << IsolateLayer;      // 只渲染隔离图层

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

    /// <summary>在实例里找到与给定渲染器对应的那个（按路径匹配，实例化后引用会变）</summary>
    static Transform FindMatching(GameObject inst, Renderer want)
    {
        string wantPath = PathOf(want.transform, want.transform.root);
        foreach (var t in inst.GetComponentsInChildren<Transform>(true))
            if (PathOf(t, inst.transform) == wantPath) return t;
        return null;
    }

    struct CamFrame { public Vector3 pos, look; public float fov, near, far; }

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

    static string PathOf(Transform t, Transform root)
    {
        var s = new List<string>();
        while (t != null && t != root) { s.Add(t.name); t = t.parent; }
        s.Reverse();
        return s.Count == 0 ? "(根)" : string.Join("/", s);
    }

    /// <summary>四方对照：同一场景里四个实例，唯一变量是「prefab 来源 × 材质来源」。
    ///   ① 原版 prefab + 原版材质   ② 原版 prefab + binder 材质
    ///   ③ 导出 prefab + 原版材质   ④ 导出 prefab + binder 材质
    /// 哪一格黑，就能定位到是 prefab 还是材质。</summary>
    static void SideBySide(string name, GameObject orig, GameObject exp, CamFrame cam)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var go1 = UnityEngine.Object.Instantiate(orig);
        var go2 = UnityEngine.Object.Instantiate(orig);
        var go3 = UnityEngine.Object.Instantiate(exp);
        var go4 = UnityEngine.Object.Instantiate(exp);
        var all = new[] { go1, go2, go3, go4 };
        for (int i = 0; i < 4; i++) all[i].transform.position = new Vector3(-9.6f + i * 6.4f, 0, 0);

        // ② ④：binder 材质
        var bundleMats = new Material[orig.GetComponentsInChildren<Renderer>(true).Length];
        var oR = orig.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < oR.Length; i++)
            bundleMats[i] = oR[i].sharedMaterials.Length > 0 ? oR[i].sharedMaterials[0] : null;

        foreach (var inst in new[] { go2, go4 })
        {
            var b = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
            if (b != null) b.Apply();
        }
        // ① ③：把 Back Glow 换回原版材质
        foreach (var inst in new[] { go1, go3 })
        {
            var rs = inst.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < Math.Min(rs.Length, bundleMats.Length); i++)
            {
                if (!rs[i].name.Contains("Back Glow") || bundleMats[i] == null) continue;
                var arr = rs[i].sharedMaterials;
                for (int k = 0; k < arr.Length; k++) arr[k] = bundleMats[i];
                rs[i].sharedMaterials = arr;
            }
        }
        foreach (var inst in all) SetLayerRecursive(inst.transform, IsolateLayer);
        foreach (var inst in all)
            foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Simulate(SimTime, withChildren: true, restart: true, fixedTimeStep: false);
                ps.Play();
            }

        var camGo = new GameObject("Cam");
        var sbsCam = camGo.AddComponent<Camera>();
        sbsCam.clearFlags = CameraClearFlags.SolidColor;
        sbsCam.backgroundColor = new Color(0.07f, 0.08f, 0.10f, 1f);
        sbsCam.fieldOfView = 50f;
        sbsCam.nearClipPlane = 0.01f;
        sbsCam.farClipPlane = 500f;
        camGo.transform.position = new Vector3(0, 0, -32f);
        camGo.transform.LookAt(new Vector3(0, 0, 0));
        sbsCam.cullingMask = 1 << IsolateLayer;
        sbsCam.aspect = 2f;

        var rt = new RenderTexture(W * 2, H, 24, RenderTextureFormat.ARGB32);
        sbsCam.targetTexture = rt;
        sbsCam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W * 2, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W * 2, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        sbsCam.targetTexture = null;
        File.WriteAllBytes($"{OutDir}/{name}_四方.png", tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);
        foreach (var o in all) UnityEngine.Object.DestroyImmediate(o);
        Debug.Log($"    [四方对照] ①原版prefab+原版材质 ②原版prefab+binder材质 ③导出prefab+原版材质 ④导出prefab+binder材质" +
                  $"，见 {OutDir}/{name}_四方.png");
    }

    static string ShaderOfIdx(GameObject inst, int idx)
    {
        var rs = inst.GetComponentsInChildren<Renderer>(true);
        if (idx < 0 || idx >= rs.Length) return "?";
        var m = rs[idx].sharedMaterials.Length > 0 ? rs[idx].sharedMaterials[0] : null;
        return m == null || m.shader == null ? "<null>" : $"{m.shader.name} instanceID={m.shader.GetInstanceID()}";
    }

    static Vector4 GetVectorSafe(this Material m, string pn)
        => m.HasProperty(pn) ? m.GetVector(pn) : new Vector4(float.NaN, float.NaN, float.NaN, float.NaN);
    static float GetFloatSafe(this Material m, string pn) => m.HasProperty(pn) ? m.GetFloat(pn) : float.NaN;

    static void SetFloatSafe(this Material m, string pn, float v)
    {
        if (m.HasProperty(pn)) m.SetFloat(pn, v);
    }

    static void SetVectorSafe(this Material m, string pn, Vector4 v)
    {
        if (m.HasProperty(pn)) m.SetVector(pn, v);
    }

    static string Sanitize(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace('/', '_').Trim();
    }
}
