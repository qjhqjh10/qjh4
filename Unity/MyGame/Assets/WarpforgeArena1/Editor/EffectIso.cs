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

    /// <summary>跑哪一侧 —— **两趟必须分进程**（两侧条件互斥，与 `EffectSweepBatch` 文件头记的是同一件事）：
    ///   · `orig` = **加载全部 84 个源包**。只加载特效包一个包时，原版 prefab 的 shader 解析不到
    ///     ⇒ 原版侧渲**品红**、图不可判（2026-10-01 实测：`Buff_DA_Forest_Self_00_Sparks#0__orig.png` 2729 px 纯 (255,0,255)）。
    ///   · `exp`（默认）= **不加载任何源包**（真实运行时条件）。源包在场会把 StreamingAssets 那份
    ///     `wf_shaders_extra.bundle` 顶掉 ⇒ binder 退回 `Shader.Find`。
    /// 取景（`CamFrame`）与槽位标签由 orig 趟写进缓存、exp 趟读 —— 两侧逐槽位可比。
    ///   WFISO_SIDE=orig  →  第一趟（顺带出 `__multi_*` / `__origx工程shader` / 各诊断）
    ///   （不设）          →  第二趟（出 `__exp.png` + 两侧各自的逐槽位数字）
    /// 一键：`bash d:/4/Unity/工具/run_iso.sh`</summary>
    static readonly string Side = System.Environment.GetEnvironmentVariable("WFISO_SIDE") ?? "exp";
    static readonly bool DoOrig = Side == "orig";
    const string FrameCachePath = @"d:\4\Unity\资料\比对基线\iso_frames.tsv";
    static string StatsPath { get { return $@"d:\4\Unity\资料\比对基线\iso_stats_{Side}.tsv"; } }

    // 要隔离的效果
    static readonly string[] Targets = { "ArtificeEffect", "EnvironmentalCondition Tau Solar Eclipse",
        // 🆕 2026-09-17：C 组最后一个。渲染器层（`CEmitProbe` 五项全同）与材质层（`MatIsoProbe` 整体 0.0%）
        // 都已排除，粒子模块值（`ParticleModuleProbe`）也**逐字段 0 处不同** —— 那三个探针**都是从「原版 prefab」
        // 出发换零件**，从来没直接比过「导出 prefab 原样渲出来」。逐发射器隔离就是为了回答「**哪个发射器暗**」。
        "PinDownEffect",
        // 🆕 2026-10-01 晚：E 组里还剩三条**没有已定根因**的（台账 2026-10-01：`Buff_DA_Forest_Self` |ln| 0.788 ·
        //    `UM_CardDraw` 0.641 · `SAU_CardDraw` 0.613）。正本给的下一步就是「`EffectIso` 逐槽隔离找主导槽」。
        "Buff_DA_Forest_Self", "UM_CardDraw", "SAU_CardDraw" };

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
        Debug.Log($"=== 逐发射器隔离渲染 开始（Side={Side}）===");
        Directory.CreateDirectory(OutDir);
        EnsureIsolateLayer();
        if (File.Exists(StatsPath)) File.Delete(StatsPath);   // 每趟重写本侧的数字（AppendStats 是追加写）

        // 🔴 2026-10-02 起：两侧**分进程**跑（见 `Side` 的注释）。
        //   原来这里只加载特效包一个包（旧注释写「不要加载全部 84 个包」）——
        //   那正是原版侧渲成品红、图不可判的原因；`WarpforgeShaderLoader.Reset()` 那条冲突改由分进程规避。
        var frames = LoadFrames();
        Dictionary<string, GameObject> originals = null;
        if (DoOrig)
        {
            if (File.Exists(FrameCachePath)) File.Delete(FrameCachePath);   // 本趟重写缓存
            AssetBundle vfx = null;
            int loaded = 0;
            foreach (var f in Directory.GetFiles(BundleDir, "*.bundle"))
            {
                var b = AssetBundle.LoadFromFile(f);
                if (b == null) continue;
                loaded++;
                if (Path.GetFileName(f) == VfxBundleName) vfx = b;
            }
            if (vfx == null) { Debug.LogError($"特效 bundle 未加载: {VfxBundleName}"); return; }
            Debug.Log($"[iso] 已加载全部源 bundle {loaded} 个");

            originals = new Dictionary<string, GameObject>();
            foreach (var n in vfx.GetAllAssetNames())
            {
                GameObject g = null;
                try { g = vfx.LoadAsset<GameObject>(n); } catch { }
                if (g != null) originals[g.name] = g;
            }
        }
        else if (frames.Count == 0)
        {
            Debug.LogError($"[iso] 没有取景缓存 —— 必须先跑一趟 WFISO_SIDE=orig 生成 {FrameCachePath}");
            return;
        }
        Debug.Log($"binder shader 解析来源: {WarpforgeVFX.WarpforgeShaderMap.Describe()}");

        foreach (var name in Targets)
        {
            GameObject orig = null;
            if (DoOrig && (originals == null || !originals.TryGetValue(name, out orig)))
            { Debug.LogError($"原版里找不到 {name}"); continue; }
            var exp = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");
            if (exp == null) { Debug.LogError($"导出 prefab 不存在: {name}"); continue; }

            ListBundleShaders();

            // 相机取景用原版的整体包围盒（与 EffectCompare 一致，保证两边可比）；
            // exp 趟拿不到原版 prefab ⇒ 读 orig 趟写下的缓存（含逐槽位标签，保证两侧编号一致）
            CamFrame cam;
            List<string> labels;
            if (DoOrig)
            {
                cam = FrameCamera(orig);
                labels = BuildLabels(orig, exp);
                SaveFrame(name, cam, labels);
            }
            else
            {
                if (!frames.TryGetValue(name, out cam))
                { Debug.LogError($"[iso] 取景缓存里没有 {name}（先跑 orig 趟）"); continue; }
                labels = LoadLabels(name);
                if (labels == null) { Debug.LogError($"[iso] 取景缓存里 {name} 没有槽位标签"); continue; }
            }

            // 先把两边的「运行时材质」逐属性 diff 一遍（比看图快得多）
            if (DoOrig && RunDiagnostics) CompareMaterials(orig, exp);

            var origRends = DoOrig ? Collect(orig) : new List<Renderer>();
            var expRends = Collect(exp);
            if (DoOrig) Debug.Log($"[{name}] 原版渲染器 {origRends.Count} 个，导出渲染器 {expRends.Count} 个");

            if (DoOrig)
            {
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
            }

            for (int i = 0; i < labels.Count; i++)
            {
                string label = labels[i];
                if (DoOrig)
                {
                    var o = i < origRends.Count ? origRends[i] : null;
                    var e = i < expRends.Count ? expRends[i] : null;

                    RenderIsolated(orig, o, $"{OutDir}/{name}_{i:00}_{label}__orig.png", cam, true, name, i);
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
                else
                {
                    if (i >= expRends.Count) { Debug.Log($"  [{i}] {label}  导出侧没有这个槽位（跳过）"); continue; }
                    var e = expRends[i];
                    RenderIsolated(exp, e, $"{OutDir}/{name}_{i:00}_{label}__exp.png", cam, false, name, i);
                    Debug.Log($"  [{i}] {label}  导出={ShaderOf(e)}");
                }
            }
        }
        Debug.Log($"=== 逐发射器隔离渲染 结束（Side={Side}）===");
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
        PinSeed(inst);

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

    /// <summary>把一条 MinMaxCurve 打成短字符串（模式 + 常数 + 前 6 个关键帧）。
    /// ⚠️ `mode != Curve` 时读 `.curve` 会告警/抛 —— 包住，读不到就记 `&lt;读不到&gt;`，别静默当成 0。</summary>
    static string CurveStr(ParticleSystem.MinMaxCurve c, int maxKeys = 6)
    {
        var sb = new List<string> { $"mode={c.mode}", $"const={c.constant:F4}" };
        try
        {
            if (c.mode == ParticleSystemCurveMode.TwoConstants) sb.Add($"min={c.constantMin:F4} max={c.constantMax:F4}");
            var curve = c.curve;
            if (curve != null && curve.length > 0)
            {
                int n = Math.Min(curve.length, maxKeys);
                for (int i = 0; i < n; i++)
                {
                    var k = curve[i];
                    sb.Add($"[{k.time:F3}]={k.value:F4}");
                }
                if (curve.length > n) sb.Add($"…共{curve.length}键");
            }
        }
        catch { sb.Add("<曲线读不到>"); }
        return string.Join(" ", sb);
    }

    /// <summary>MinMaxGradient → 短字符串（颜色键 + alpha 键）。</summary>
    static string GradStr(ParticleSystem.MinMaxGradient g)
    {
        try
        {
            var gr = g.gradient;
            if (gr != null && g.mode != ParticleSystemGradientMode.Color && g.mode != ParticleSystemGradientMode.TwoColors)
            {
                var parts = new List<string> { $"mode={g.mode}" };
                foreach (var k in gr.colorKeys) parts.Add($"C[{k.time:F2}]{k.color}");
                foreach (var k in gr.alphaKeys) parts.Add($"A[{k.time:F2}]{k.alpha:F2}");
                return string.Join(" ", parts);
            }
        }
        catch { }
        return $"mode={g.mode} col={g.color} min={g.colorMin} max={g.colorMax}";
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
            var ar = a.GetComponent<ParticleSystemRenderer>();
            var br = b.GetComponent<ParticleSystemRenderer>();
            // 🆕 2026-10-02 加深（上一版漏掉这一整片 —— `UM/SAU_CardDraw` 的「渲染形状不同、
            //   而账面 PS一致」就是这么漏过去的）：**曲线 / 渐变 / 三维尺寸本身**才是能改渲染形状的东西。
            Cmp("main.startSize3D", am.startSize3D, bm.startSize3D);
            Cmp("main.startSize.mode", am.startSize.mode, bm.startSize.mode);
            Cmp("main.startLifetime.mode", am.startLifetime.mode, bm.startLifetime.mode);
            Cmp("main.simulationSpeed", am.simulationSpeed, bm.simulationSpeed);
            Cmp("main.simulationSpace", am.simulationSpace, bm.simulationSpace);
            Cmp("main.scalingMode", am.scalingMode, bm.scalingMode);
            Cmp("main.randomSeed/auto", $"{a.useAutoRandomSeed}", $"{b.useAutoRandomSeed}");
            Cmp("sizeOverLifetime.size", CurveStr(a.sizeOverLifetime.size), CurveStr(b.sizeOverLifetime.size));
            Cmp("rotationOverLifetime.z", CurveStr(a.rotationOverLifetime.z), CurveStr(b.rotationOverLifetime.z));
            Cmp("rotationOverLifetime.separateAxes", a.rotationOverLifetime.separateAxes, b.rotationOverLifetime.separateAxes);
            Cmp("rotationOverLifetime.x", CurveStr(a.rotationOverLifetime.x), CurveStr(b.rotationOverLifetime.x));
            Cmp("rotationOverLifetime.y", CurveStr(a.rotationOverLifetime.y), CurveStr(b.rotationOverLifetime.y));
            Cmp("colorOverLifetime.gradient", GradStr(a.colorOverLifetime.color), GradStr(b.colorOverLifetime.color));
            Cmp("colorOverLifetime.enabled+", $"{a.colorOverLifetime.enabled}", $"{b.colorOverLifetime.enabled}");
            Cmp("colorBySpeed.enabled", a.colorBySpeed.enabled, b.colorBySpeed.enabled);
            Cmp("sizeBySpeed.enabled", a.sizeBySpeed.enabled, b.sizeBySpeed.enabled);
            Cmp("sizeBySpeed.size", CurveStr(a.sizeBySpeed.size), CurveStr(b.sizeBySpeed.size));
            Cmp("emission.rateOverTime.curve", CurveStr(a.emission.rateOverTime), CurveStr(b.emission.rateOverTime));
            Cmp("emission.rateOverDistance", CurveStr(a.emission.rateOverDistance), CurveStr(b.emission.rateOverDistance));
            for (int k = 0; k < Math.Min(a.emission.burstCount, b.emission.burstCount); k++)
                Cmp($"burst[{k}].countCurve", CurveStr(a.emission.GetBurst(k).count), CurveStr(b.emission.GetBurst(k).count));
            Cmp("velocityOverLifetime.enabled+", $"{a.velocityOverLifetime.enabled}", $"{b.velocityOverLifetime.enabled}");
            Cmp("velocity.space", a.velocityOverLifetime.space, b.velocityOverLifetime.space);
            Cmp("velocity.x", CurveStr(a.velocityOverLifetime.x), CurveStr(b.velocityOverLifetime.x));
            Cmp("velocity.y", CurveStr(a.velocityOverLifetime.y), CurveStr(b.velocityOverLifetime.y));
            Cmp("velocity.z", CurveStr(a.velocityOverLifetime.z), CurveStr(b.velocityOverLifetime.z));
            Cmp("shape.position", a.shape.position, b.shape.position);
            Cmp("shape.scale", a.shape.scale, b.shape.scale);
            Cmp("shape.rotation", a.shape.rotation, b.shape.rotation);
            Cmp("shape.radiusThickness", a.shape.radiusThickness, b.shape.radiusThickness);
            Cmp("uvSheet", $"{a.textureSheetAnimation.enabled}/{a.textureSheetAnimation.mode}/{a.textureSheetAnimation.numTilesX}x{a.textureSheetAnimation.numTilesY}",
                            $"{b.textureSheetAnimation.enabled}/{b.textureSheetAnimation.mode}/{b.textureSheetAnimation.numTilesX}x{b.textureSheetAnimation.numTilesY}");
            Cmp("uvSheet.frameOverTime", CurveStr(a.textureSheetAnimation.frameOverTime), CurveStr(b.textureSheetAnimation.frameOverTime));
            Cmp("uvSheet.startFrame", a.textureSheetAnimation.startFrame.constant, b.textureSheetAnimation.startFrame.constant);
            Cmp("uvSheet.cycleCount", a.textureSheetAnimation.cycleCount, b.textureSheetAnimation.cycleCount);
            if (ar != null && br != null)
            {
                Cmp("psr.pivot", ar.pivot, br.pivot);
                Cmp("psr.alignment", ar.alignment, br.alignment);
                Cmp("psr.renderMode", ar.renderMode, br.renderMode);
                Cmp("psr.sortingFudge", ar.sortingFudge, br.sortingFudge);
                Cmp("psr.mesh", ar.mesh == null ? "<null>" : ar.mesh.name, br.mesh == null ? "<null>" : br.mesh.name);
                Cmp("psr.sharedMaterial", ar.sharedMaterial == null ? "<null>" : ar.sharedMaterial.name,
                                           br.sharedMaterial == null ? "<null>" : br.sharedMaterial.name);
            }
            Cmp("colorOverLifetime", a.colorOverLifetime.enabled, b.colorOverLifetime.enabled);
            Cmp("sizeOverLifetime", a.sizeOverLifetime.enabled, b.sizeOverLifetime.enabled);
            Cmp("velocityOverLifetime", a.velocityOverLifetime.enabled, b.velocityOverLifetime.enabled);
            Cmp("noise", a.noise.enabled, b.noise.enabled);
            Cmp("trails", a.trails.enabled, b.trails.enabled);
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

            // ③ 🆕 2026-10-02：**连 shader 一起换** —— `CopyFrom` 只拷属性、**不换 shader**（读代码才发现），
            //    所以「不是材质的锅」那个结论一直缺这一档。导出侧的 shader 常被换成自建的（如 `Extra Color`），
            //    要判「渲染形状不同」到底在 prefab 还是在 shader，必须有这一档。
            RenderCross(exp, eR[i], m => { CopyFrom(m, om); m.shader = om.shader; }, cam,
                        $"{OutDir}/{name}_cross{i:00}_{face}__exp_with_ORIG_shader.png");

            // ④ 🆕 2026-10-02：**在原版材质上关掉软粒子**（`_SOFTPARTICLES`，原版关键字名**没有 _ON 后缀**）。
            //    用于判「原版渲出的形状/亮度差」里有多少来自软粒子 —— 空场景里它按深度淡出，
            //    会把粒子"削"掉一块（`UM/SAU_CardDraw` 的 `Shine Square` 槽就是这么差出 2× 的）。
            if (om.IsKeywordEnabled("_SOFTPARTICLES"))
                RenderCross(orig, oR[i], m => m.DisableKeyword("_SOFTPARTICLES"), cam,
                            $"{OutDir}/{name}_cross{i:00}_{face}__orig_no_softparticles.png");

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
        PinSeed(inst);
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

    /// <summary>钉死粒子随机种子 —— 判据同 `EffectSweepBatch.cs:406-433`：
    /// `useAutoRandomSeed` 默认**开**，每次重播换种子 ⇒ 不钉的话两侧比的是**两次不同的随机抽取**
    /// （尺寸/旋转/发射数都会不同），逐槽位数字里混着噪声。2026-10-02 补上。</summary>
    const uint FixedSeed = 12345;
    static void PinSeed(GameObject inst)
    {
        foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.useAutoRandomSeed = false;
            ps.randomSeed = FixedSeed;
        }
    }

    static void RenderIsolated(GameObject prefab, Renderer keep, string outPath, CamFrame frame, bool isOriginal,
                               string target, int slot)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var inst = UnityEngine.Object.Instantiate(prefab);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;
        PinSeed(inst);

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

        // 🆕 逐槽位数字（③「找主导槽」就靠它，不靠人眼看图）：
        //   lit     = 与纯色背景 (0.07,0.08,0.10) 相差 >2/255 的像素数；
        //   contrib = Σ|像素 − 背景| / 255（**相对空背景的贡献**）。
        //   ⚠️ 第一版把 sum 写成「全图 r+g+b 之和」—— 背景占 99%+ ⇒ 比值恒 1.000、什么也看不出来
        //      （2026-10-02 实测踩过）。判读**用 contrib**。
        int lit = 0; double contrib = 0;
        {
            var px = tex.GetPixels32();
            byte br = (byte)Mathf.RoundToInt(0.07f * 255f), bg = (byte)Mathf.RoundToInt(0.08f * 255f), bb = (byte)Mathf.RoundToInt(0.10f * 255f);
            for (int k = 0; k < px.Length; k++)
            {
                var c = px[k];
                contrib += (Mathf.Abs(c.r - br) + Mathf.Abs(c.g - bg) + Mathf.Abs(c.b - bb)) / 255.0;
                if (Mathf.Abs(c.r - br) > 2 || Mathf.Abs(c.g - bg) > 2 || Mathf.Abs(c.b - bb) > 2) lit++;
            }
        }
        AppendStats(target, slot, Path.GetFileNameWithoutExtension(outPath), isOriginal ? "orig" : "exp", lit, contrib);

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

    // ===== 两侧分进程用的缓存（取景 + 槽位标签 + 逐槽位数字）=====
    //   iso_frames.tsv        : name \t px \t py \t pz \t lx \t ly \t lz \t fov \t near \t far \t label0|label1|…
    //   iso_stats_<side>.tsv  : target \t slot \t 图名 \t side \t lit \t sum
    //   ⚠️ 全部 InvariantCulture（本机是中文区，小数点是「.」没错，但别赌。）

    static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;

    /// <summary>槽位标签：与旧版「两边取 max、标签取 o ?? e」完全同一条规则 —— 分进程后 exp 趟靠它对齐编号。</summary>
    static List<string> BuildLabels(GameObject orig, GameObject exp)
    {
        var oR = Collect(orig); var eR = Collect(exp);
        var labels = new List<string>();
        for (int i = 0; i < Math.Max(oR.Count, eR.Count); i++)
        {
            var any = i < oR.Count ? (Renderer)oR[i] : (i < eR.Count ? eR[i] : null);
            labels.Add(any == null ? $"slot{i}" : Sanitize(PathOf(any.transform, any.transform.root)) + $"#{i}");
        }
        return labels;
    }

    static void SaveFrame(string name, CamFrame cam, List<string> labels)
    {
        File.AppendAllText(FrameCachePath,
            name + "\t" +
            cam.pos.x.ToString("R", Inv) + "\t" + cam.pos.y.ToString("R", Inv) + "\t" + cam.pos.z.ToString("R", Inv) + "\t" +
            cam.look.x.ToString("R", Inv) + "\t" + cam.look.y.ToString("R", Inv) + "\t" + cam.look.z.ToString("R", Inv) + "\t" +
            cam.fov.ToString("R", Inv) + "\t" + cam.near.ToString("R", Inv) + "\t" + cam.far.ToString("R", Inv) + "\t" +
            string.Join("|", labels) + "\n");
    }

    static Dictionary<string, CamFrame> LoadFrames()
    {
        var d = new Dictionary<string, CamFrame>();
        if (!File.Exists(FrameCachePath)) return d;
        foreach (var l in File.ReadAllLines(FrameCachePath))
        {
            var c = l.Split('\t');
            if (c.Length < 10) continue;
            d[c[0]] = new CamFrame
            {
                pos = new Vector3(float.Parse(c[1], Inv), float.Parse(c[2], Inv), float.Parse(c[3], Inv)),
                look = new Vector3(float.Parse(c[4], Inv), float.Parse(c[5], Inv), float.Parse(c[6], Inv)),
                fov = float.Parse(c[7], Inv), near = float.Parse(c[8], Inv), far = float.Parse(c[9], Inv),
            };
        }
        return d;
    }

    static List<string> LoadLabels(string name)
    {
        if (!File.Exists(FrameCachePath)) return null;
        foreach (var l in File.ReadAllLines(FrameCachePath))
        {
            var c = l.Split('\t');
            if (c.Length >= 11 && c[0] == name) return c[10].Split('|').ToList();
        }
        return null;
    }

    static void AppendStats(string target, int slot, string image, string side, int lit, double sum)
    {
        if (!File.Exists(StatsPath))
            File.AppendAllText(StatsPath, "target\tslot\timage\tside\tlit\tcontrib\n");
        File.AppendAllText(StatsPath,
            target + "\t" + slot + "\t" + image + "\t" + side + "\t" + lit + "\t" + sum.ToString("F3", Inv) + "\n");
    }

    struct CamFrame { public Vector3 pos, look; public float fov, near, far; }

    static CamFrame FrameCamera(GameObject prefab)
    {
        var tmp = UnityEngine.Object.Instantiate(prefab);
        tmp.transform.position = Vector3.zero;
        tmp.transform.rotation = Quaternion.identity;
        PinSeed(tmp);
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
        foreach (var g in all) PinSeed(g);

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
