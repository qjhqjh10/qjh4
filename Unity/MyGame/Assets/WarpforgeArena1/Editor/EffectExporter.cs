// EffectExporter.cs — 从 Warpforge 原版 AssetBundle 导出特效为本地 Unity 资产
//
// 能 1:1 搬过来的：粒子系统全部模块、贴图、材质数值、网格
// 搬不过来的：自定义 shader（源码构建时被剥离，只剩字节码）→ 用 URP 等价物顶，并标记为「近似」
//
// 用法：Unity.exe -batchmode -quit -projectPath ... -executeMethod EffectExporter.Run -logFile -
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using WarpforgeVFX;

public static class EffectExporter
{
    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string VfxBundleName = "battleprefabs_vfxandmisc_assets_all.bundle";
    const string Root = "Assets/WarpforgeVFX";
    const string TexDir = Root + "/Textures";
    const string MatDir = Root + "/Materials";
    const string MeshDir = Root + "/Meshes";
    const string PrefabDir = Root + "/Prefabs";
    // 原版 shader 无法落成工程资产，只能随包带着运行时加载
    const string ShaderBundleSrc = "shaders_assets_all.bundle";
    const string StreamDir = "Assets/StreamingAssets/WarpforgeVFX";
    const string ShaderBundleDst = StreamDir + "/wf_shaders.bundle";

    // ---- 分片控制（按「过滤后的效果序号」切片，续跑时序号稳定）----
    // 全量导出建议每片 100~200 个跑一次；中断后把 SliceFrom 往后挪即可续跑，
    // 已完成的效果会被跳过，不需要重跑。
    const int SliceFrom = 0;
    const int SliceCount = 0;          // 0 = 到末尾
    const bool Resume = false;         // true = 不清产物、载入既有报告继续

    // 只导出名字里含这些子串的效果（空数组 = 不过滤）。按关键字导出比按字母序切前 N 个有用得多。
    static readonly string[] NameFilter = { };

    // 原版 shader 名 → 目标 shader 名。值里带 * 表示「近似替代」
    //
    // ⚠️ 这张表和运行时的 WarpforgeShaderMap.Replacements **是两份，改一份必须同步另一份**。
    //    这张管：占位材质用什么 shader + 报告里标不标「近似替代」
    //    那张管：运行时 binder 重建材质时解析到哪个 shader
    //    只改一张的后果：编辑器里看着对、进游戏不对（或者反过来）
    static readonly Dictionary<string, string> ShaderMap = new Dictionary<string, string>
    {
        { "Universal Render Pipeline/Particles/Unlit",      "Universal Render Pipeline/Particles/Unlit" },
        { "Universal Render Pipeline/Particles/Simple Lit", "Universal Render Pipeline/Particles/Simple Lit" },
        { "Universal Render Pipeline/Unlit",                "Universal Render Pipeline/Unlit" },
        { "Universal Render Pipeline/Lit",                  "Universal Render Pipeline/Lit" },
        { "Sprites/Default",                                "Sprites/Default" },
        { "Sprites/Mask",                                   "Sprites/Mask" },
        { "UI/Default",                                     "UI/Default" },

        // 非粒子的 Everguild shader —— 必须映射到 URP/Unlit。
        // 让它们掉进默认的 URP **Particles**/Unlit 会连粒子专用逻辑一起套上，实测过曝 4 倍
        { "Everguild/UnlitAmbient",                         "WarpforgeVFX/UnlitAmbient" },
        { "Everguild/UnlitAmbient Emissive Flickker",       "WarpforgeVFX/UnlitAmbient" },
        { "Everguild/Unlit Wind",                           "WarpforgeVFX/UnlitAmbient" },

        // ---- 自建替代 shader ----
        { "Everguild/FX/Extra Color",                       "WarpforgeVFX/Particles/Extra Color" },
        { "Everguild/Sprites/Sprite Additive",              "WarpforgeVFX/Sprites/Additive" },
        { "Everguild/FX/Particle Distortion Affect Transparents", "WarpforgeVFX/FX/Distortion" },
        { "Everguild/Matcap/Matcap Full Options",           "WarpforgeVFX/Matcap/Matcap" },
        { "Everguild/Matcap/Matcap With Texture",           "WarpforgeVFX/Matcap/Matcap" },

        { "Mobile/Particles/Additive",                      "WarpforgeVFX/Particles/Extra Color*" },
        { "Mobile/Particles/Alpha Blended",                 "WarpforgeVFX/Particles/Extra Color*" },
        { "Mobile/Particles/Multiply",                      "WarpforgeVFX/Particles/Extra Color*" },
        { "Particles/Standard Unlit",                       "WarpforgeVFX/Particles/Extra Color*" },
        { "Particles/Additive",                             "WarpforgeVFX/Particles/Extra Color*" },
        { "Legacy Shaders/Particles/Additive",              "WarpforgeVFX/Particles/Extra Color*" },
        { "Legacy Shaders/Particles/Alpha Blended",         "WarpforgeVFX/Particles/Extra Color*" },
        { "Legacy Shaders/Particles/Alpha Blended Premultiply", "WarpforgeVFX/Particles/Extra Color*" },
        { "Legacy Shaders/Particles/Anim Alpha Blended",    "WarpforgeVFX/Particles/Extra Color*" },
        { "UI/Additive",                                    "WarpforgeVFX/Particles/Extra Color*" },
    };

    const string ReportPath = Root + "/导出报告.tsv";

    /// <summary>效果名 → 状态。OK = 导出成功；FAIL	... = 失败原因。
    /// 用字典而不是列表：同一次会话里重复跑不会产生重复行，也方便续跑时跳过已完成的。</summary>
    static readonly Dictionary<string, string> Report = new Dictionary<string, string>();

    /// <summary>Export() 顺手记下的说明，由 Run() 写进 Report</summary>
    static string LastDetail = "";

    static readonly Dictionary<Material, Material> MatCache = new Dictionary<Material, Material>();
    // 缓存命中时也要知道这个材质当初是不是走了替代 shader，否则报告会漏计
    static readonly Dictionary<Material, (bool approx, string origShader)> MatMeta
        = new Dictionary<Material, (bool, string)>();
    static readonly Dictionary<Texture, Texture2D> TexCache = new Dictionary<Texture, Texture2D>();
    static readonly Dictionary<Mesh, Mesh> MeshCache = new Dictionary<Mesh, Mesh>();
    static readonly Dictionary<Sprite, Sprite> SpriteCache = new Dictionary<Sprite, Sprite>();

    public static void Run()
    {
        Debug.Log("=== 特效导出 开始 ===");
        EnsureFolders();

        // 分片导出：全量 958 个一次跑完很慢、中途崩了要重来。
        // SliceFrom/SliceCount 用来切片；每导完一个就立刻刷一次报告，随时可以中断续跑。
        // 续跑时保持 SliceFrom 往后挪即可 —— 已导出的会在下面的 IsDone() 里被跳过。
        if (!Resume)
        {
            ClearGenerated();
            if (File.Exists(ReportPath)) File.Delete(ReportPath);
        }
        Report.Clear();
        LoadReport();

        // ---- 加载全部 bundle，保证跨包引用解析 ----
        AssetBundle vfx = null;
        int nb = 0;
        foreach (var f in Directory.GetFiles(BundleDir, "*.bundle"))
        {
            var b = AssetBundle.LoadFromFile(f);
            if (b == null) continue;
            nb++;
            if (Path.GetFileName(f) == VfxBundleName) vfx = b;
        }
        Debug.Log($"bundle {nb} 个已加载");
        if (vfx == null) { Debug.LogError("特效 bundle 未加载"); return; }

        CopyShaderBundle();

        var all = new List<GameObject>();
        foreach (var n in vfx.GetAllAssetNames())
        {
            GameObject g = null;
            try { g = vfx.LoadAsset<GameObject>(n); } catch { }
            if (g != null) all.Add(g);
        }
        // 只取「效果根物体」：带粒子渲染器、且不是别的取样物体的子级
        var childOf = new HashSet<GameObject>();
        foreach (var g in all)
            foreach (var t in g.GetComponentsInChildren<Transform>(true))
                if (t.gameObject != g) childOf.Add(t.gameObject);

        var roots = all.Where(g => !childOf.Contains(g) &&
                                   g.GetComponentsInChildren<ParticleSystemRenderer>(true).Length > 0)
                       .OrderBy(g => g.name).ToList();
        if (NameFilter.Length > 0)
            roots = roots.Where(g => NameFilter.Any(k => g.name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();

        int total = roots.Count;
        // 分片：先按名字过滤，再切片。切片按「过滤后的序号」算，续跑时序号稳定
        var slice = roots.Skip(SliceFrom).Take(SliceCount > 0 ? SliceCount : int.MaxValue).ToList();
        int already = slice.Count(g => Report.ContainsKey(g.name) && Report[g.name].StartsWith("OK"));
        Debug.Log($"效果根物体 {total} 个" +
                  (NameFilter.Length > 0 ? $"（已按关键字过滤：{string.Join("/", NameFilter)}）" : "") +
                  $"，本片 [{SliceFrom}, {SliceFrom + slice.Count}) 共 {slice.Count} 个" +
                  (already > 0 ? $"，其中 {already} 个已完成将跳过" : ""));

        int done = 0, skipped = 0, failed = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < slice.Count; i++)
        {
            var g = slice[i];
            if (Report.TryGetValue(g.name, out var prev) && prev.StartsWith("OK")) { skipped++; continue; }
            try
            {
                Export(g);
                done++;
                Report[g.name] = "OK	" + LastDetail;
            }
            catch (Exception e)
            {
                failed++;
                Report[g.name] = $"FAIL\t{e.GetType().Name}: {e.Message}";
                Debug.LogWarning($"  导出失败 {g.name}: {e.GetType().Name}: {e.Message}");
            }
            // 每导完一个就刷盘：中断/崩溃都不丢进度
            if ((done + failed) % 10 == 0 || i == slice.Count - 1)
            {
                AssetDatabase.SaveAssets();
                SaveReport();
                Debug.Log($"  进度 {i + 1}/{slice.Count}（成功 {done} 失败 {failed} 跳过 {skipped}）" +
                          $" 用时 {sw.Elapsed.TotalMinutes:F1} 分");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SaveReport();
        Debug.Log($"=== 特效导出 结束：本片成功 {done} / 失败 {failed} / 跳过 {skipped}，" +
                  $"累计已收录 {Report.Count} / {total} ===");
    }

    static void LoadReport()
    {
        if (!File.Exists(ReportPath)) return;
        foreach (var line in File.ReadAllLines(ReportPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            var p = line.Split('\t');
            if (p.Length >= 2) Report[p[0]] = p[1];
        }
        Debug.Log($"已载入既有报告 {Report.Count} 条");
    }

    static void SaveReport()
    {
        var lines = new List<string> { "# 效果名\t状态/原因" };
        foreach (var kv in Report.OrderBy(k => k.Key))
            lines.Add($"{kv.Key}\t{kv.Value}");
        File.WriteAllLines(ReportPath, lines, new System.Text.UTF8Encoding(true));
    }

    static void EnsureFolders()
    {
        foreach (var p in new[] { "Assets", Root, TexDir, MatDir, MeshDir, PrefabDir })
            if (!AssetDatabase.IsValidFolder(p))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(p).Replace('\\', '/'), Path.GetFileName(p));
    }

    /// <summary>清掉上一轮产物。
    /// 不清的话 GenerateUniqueAssetPath 会不断产出 "X 1.mat" "X 2.mat" 累积下去，
    /// 而且旧贴图/网格会和新的一起被 binder 引用，排查问题时很误导。</summary>
    static void ClearGenerated()
    {
        foreach (var dir in new[] { MatDir, TexDir, MeshDir, PrefabDir })
        {
            if (!AssetDatabase.IsValidFolder(dir)) continue;
            foreach (var guid in AssetDatabase.FindAssets("", new[] { dir }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(p) && p.StartsWith(dir)) AssetDatabase.DeleteAsset(p);
            }
        }
        AssetDatabase.SaveAssets();
    }

    static void Export(GameObject src)
    {
        var inst = UnityEngine.Object.Instantiate(src);
        inst.name = src.name;
        StripMissingScripts(inst);

        int approx = 0;
        var usedShaders = new HashSet<string>();
        LastDetail = "";

        // ---- 材质：占位资产（编辑器里能看）+ 完整原版定义（运行时重建用）----
        var defs = new List<WFMatDef>();
        var defIndex = new Dictionary<string, int>();
        var slots = new List<int>();
        var trailSlots = new List<int>();

        foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            if (mats.Length == 0) continue;
            var ps = r.GetComponent<ParticleSystem>();
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var om = mats[i];
                if (om == null) { slots.Add(-1); continue; }
                int di = DefIndex(defs, defIndex, om);
                StripGlobalKeywords(defs[di], ps, om.shader);
                slots.Add(di);
                var nm = ImportMaterial(om, out bool wasApprox, out string origShader);
                if (nm != null) { mats[i] = nm; changed = true; usedShaders.Add(origShader); if (wasApprox) approx++; }
            }
            if (changed) r.sharedMaterials = mats;

            // 粒子拖尾材质走单独字段，不在 sharedMaterials 里
            var psr0 = r as ParticleSystemRenderer;
            if (psr0 != null && psr0.trailMaterial != null)
            {
                int di = DefIndex(defs, defIndex, psr0.trailMaterial);
                StripGlobalKeywords(defs[di], ps, psr0.trailMaterial.shader);
                trailSlots.Add(di);
                var tm = ImportMaterial(psr0.trailMaterial, out bool ta, out string to);
                if (tm != null) { psr0.trailMaterial = tm; if (ta) approx++; usedShaders.Add(to); }
            }
            else trailSlots.Add(-1);
        }

        // 网格：MeshFilter 的走一遍
        foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true))
            if (mf.sharedMesh != null) mf.sharedMesh = ImportMesh(mf.sharedMesh);

        // 粒子用网格走的是 ParticleSystemRenderer.mesh，不是 MeshFilter ——
        // 漏掉这一条，Mesh 模式发射的粒子（弹体/光环等）在导出后会整个消失
        foreach (var psr in inst.GetComponentsInChildren<ParticleSystemRenderer>(true))
            if (psr.mesh != null) psr.mesh = ImportMesh(psr.mesh);

        // 精灵：SpriteRenderer / SpriteMask 的 sprite 同样是 **bundle 资产**，
        // 不导的话引用落不下来（序列化成 guid 全 0 的伪引用，运行时解析成 null）。
        // 后果不止「精灵自己不显示」：**粒子渲染器的 maskInteraction=VisibleInsideMask
        // 完全靠 SpriteMask 的精灵裁形**，遮罩精灵一空，那些粒子一个像素都画不出来 ——
        // 整块效果渲染为空。实测 78 个 prefab 有被遮罩的粒子。
        // 定位依据见 CEmitProbe（「导出 prefab + 原版材质」照样 0 亮点 ⇒ 不是材质的锅）。
        foreach (var sr in inst.GetComponentsInChildren<SpriteRenderer>(true))
            if (sr.sprite != null) sr.sprite = ImportSprite(sr.sprite);
        foreach (var sm in inst.GetComponentsInChildren<SpriteMask>(true))
            if (sm.sprite != null) sm.sprite = ImportSprite(sm.sprite);

        // ---- 挂 binder：进游戏时用原版 shader 重建材质 ----
        var binder = inst.GetComponent<WarpforgeEffectBinder>();
        if (binder == null) binder = inst.AddComponent<WarpforgeEffectBinder>();
        binder.materials = defs.ToArray();
        binder.rendererSlots = slots.ToArray();
        binder.trailSlots = trailSlots.ToArray();

        var path = $"{PrefabDir}/{Sanitize(src.name)}.prefab";
        PrefabUtility.SaveAsPrefabAsset(inst, path);
        UnityEngine.Object.DestroyImmediate(inst);

        LastDetail = $"材质定义{defs.Count}个；原 shader: {string.Join(", ", usedShaders.OrderBy(x => x))}" +
                     (approx > 0 ? $"；近似替代 {approx} 处" : "");
    }

    /// <summary>补齐「渲染器层面」的 shader 关键字。
    ///
    /// URP 粒子 shader 的 _EMISSION 是**全局**关键字：编辑器里默认关着，由 ParticleSystemRenderer
    /// 在运行时按粒子系统的 Emission 模块开关。而 Material.shaderKeywords 只含**局部**关键字，
    /// 抓不到它 —— 结果就是导出的材质丢了 Emission，凡是靠 Emission 发光的效果（Back Glow 之类）
    /// 整个变暗甚至看不见。这里按 Emission 模块的状态把它写进材质定义，运行时 binder 会重新打开。
    ///
    /// 注意判据必须是「渲染器层的状态」而不是 shader 名：shader 名只说明这个 shader 支持 _EMISSION，
    /// 不代表这个材质开了它。</summary>
    /// <summary>清掉那些其实是「全局关键字」、不该当局部关键字烘进材质的项。
    ///
    ///   判据必须是「渲染器层的状态」而不是 shader 名：shader 名只说明这个 shader 支持某关键字，
    ///   不代表这个材质开了它。别再凭 shader 名推断关键字，要动必须先有实测证据。</summary>
    static void StripGlobalKeywords(WFMatDef d, ParticleSystem ps, Shader sh)
    {
        if (ps == null || d == null || sh == null) return;

        // _EMISSION 在这个 shader 里是**全局**关键字（原版运行时由 ParticleSystemRenderer 按
        // Emission 模块开关）。但它会出现在 bundle 材质的 shaderKeywords 里，导出时被当成
        // **局部**关键字烘进 WFMatDef —— 运行时 Material.IsKeywordEnabled 是「局部 || 全局」，
        // 于是导出的材质**无条件**开着 Emission，整体偏亮。
        // 实测：把 _EMISSION 从导出里剔掉，12 个效果的中位逐像素 L1 从 0.11 掉到 0.0003。
        // 所以这里必须把它删掉，而不是补上 —— 方向别搞反。
        if (d.keywords != null)
        {
            int n = 0;
            for (int i = 0; i < d.keywords.Length; i++)
                if (d.keywords[i] != "_EMISSION") d.keywords[n++] = d.keywords[i];
            if (n != d.keywords.Length) Array.Resize(ref d.keywords, n);
        }
    }

    /// <summary>读材质上**实际保存下来**的属性名与值。
    ///
    /// ⚠️ 名字和**值**都必须从序列化数据里读，不能用 `Material.GetFloat/GetColor/GetTexture`。
    ///    原因：原版材质的 shader 来自 bundle，`Material.GetXxx()` 对「shader 没声明的属性」
    ///    会返回 0/黑 —— 实测 `_SrcBlend` 明明存着 5，`GetFloat` 却给 0，于是重建出来的材质
    ///    混合变成「源色×0」，整个效果不可见。名字读对了还不够，值也得走同一条路。
    ///
    /// 走 SerializedObject 直接读 `m_SavedProperties.*`：
    ///   - 不受 bundle shader 反射信息残缺的影响
    ///   - `MaterialPropertyType` 这个枚举在本工程 Unity 版本里没有 Color 成员，那条路编译不过</summary>
    static void ReadSaved(Material m, string which,
                          List<string> names, List<float> floats,
                          List<string> colors, List<Color> colorVals,
                          List<string> texes, List<Texture> texVals)
    {
        SerializedObject so;
        SerializedProperty arr;
        try
        {
            so = new SerializedObject(m);
            arr = so.FindProperty("m_SavedProperties." + which);
            if (arr == null || !arr.isArray) return;
        }
        catch { return; }

        for (int i = 0; i < arr.arraySize; i++)
        {
            var e = arr.GetArrayElementAtIndex(i);
            var keyP = e.FindPropertyRelative("first");
            var valP = e.FindPropertyRelative("second");
            var nm = keyP != null ? keyP.stringValue : e.displayName;
            if (string.IsNullOrEmpty(nm) || valP == null) continue;

            switch (which)
            {
                case "m_Floats":
                    names.Add(nm); floats.Add(valP.floatValue);
                    break;
                case "m_Colors":
                    colors.Add(nm); colorVals.Add(valP.colorValue);
                    break;
                case "m_TexEnvs":
                    var tp = valP.FindPropertyRelative("m_Texture");
                    var tex = tp != null ? tp.objectReferenceValue as Texture : null;
                    if (tex != null) { texes.Add(nm); texVals.Add(tex); }
                    break;
            }
        }
    }

    /// <summary>把原材质的 shader 名与全部属性值抓下来，供运行时重建</summary>
    static int DefIndex(List<WFMatDef> defs, Dictionary<string, int> idx, Material om)
    {
        string key = om.name + "|" + (om.shader ? om.shader.name : "null");
        if (idx.TryGetValue(key, out var i)) return i;

        var d = new WFMatDef
        {
            name = om.name,
            shader = om.shader ? om.shader.name : "",
            renderQueue = om.renderQueue,
        };
        var sh = om.shader;
        var fl = new List<string>(); var fv = new List<float>();
        var cl = new List<string>(); var cv = new List<Color>();
        var tl = new List<string>(); var tv = new List<Texture>();

        // ⚠️ 必须用**材质自己保存的属性表**驱动，不能用 shader 声明的属性表。
        //
        // 原版材质的 shader 是从 AssetBundle 里加载的，它的 `GetPropertyCount()` /
        // `GetPropertyName()` 对这类 shader **只返回残缺的一部分** —— 实测
        // `Everguild/FX/Particle Premultiply` 只报出 4 个 float（_ENABLEVERTEXSTREAMS /
        // _SOFTPARTICLES / _QueueOffset / _QueueControl），把 `_SrcBlend` / `_DstBlend` /
        // `_ZWrite` / `_Cull` / `_CameraFadingEnabled` 这些**全漏了**。
        //
        // 后果：WFMatDef 里没有 _SrcBlend，运行时 WarpforgeEffectBinder 只好退到
        // `InferBlend(按 shader 名猜)` —— 而"premultiply"猜出来是 One/OneMinusSrcAlpha，
        // 原版实际是 SrcAlpha/OneMinusSrcAlpha（5/10）。混合错了 + 相机淡出参数没补，
        // 效果就整个渲染不出来。实测 Explosion_Ground 全景取景下完全空白。
        //
        // `Material.GetPropertyNames()` 读的是材质序列化时真正存下来的值，
        // 不受 shader 反射信息残缺的影响 —— 这才是这个循环该用的数据源。
        ReadSaved(om, "m_Floats", fl, fv, cl, cv, tl, tv);
        ReadSaved(om, "m_Colors", fl, fv, cl, cv, tl, tv);
        // 贴图要过一遍 ImportTexture 落成工程资产，所以单独走
        var tn = new List<string>(); var tt = new List<Texture>();
        ReadSaved(om, "m_TexEnvs", fl, fv, cl, cv, tn, tt);
        for (int ti = 0; ti < tn.Count; ti++)
        {
            try { tl.Add(tn[ti]); tv.Add(ImportTexture(tt[ti])); } catch { }
        }
        d.floatNames = fl.ToArray(); d.floatVals = fv.ToArray();
        d.colorNames = cl.ToArray(); d.colorVals = cv.ToArray();
        d.texNames = tl.ToArray(); d.texVals = tv.ToArray();
        try { d.keywords = om.shaderKeywords ?? new string[0]; } catch { d.keywords = new string[0]; }

        i = defs.Count;
        defs.Add(d);
        idx[key] = i;
        return i;
    }

    /// <summary>把原版 shader bundle 复制到 StreamingAssets（运行时加载用）</summary>
    static void CopyShaderBundle()
    {
        var srcPath = Path.Combine(BundleDir, ShaderBundleSrc);
        if (!File.Exists(srcPath)) { Debug.LogWarning($"找不到 {ShaderBundleSrc}"); return; }
        if (!AssetDatabase.IsValidFolder("Assets/StreamingAssets"))
            AssetDatabase.CreateFolder("Assets", "StreamingAssets");
        if (!AssetDatabase.IsValidFolder(StreamDir))
            AssetDatabase.CreateFolder("Assets/StreamingAssets", "WarpforgeVFX");
        File.Copy(srcPath, Path.Combine(Directory.GetCurrentDirectory(), ShaderBundleDst), true);
        AssetDatabase.ImportAsset(ShaderBundleDst, ImportAssetOptions.ForceUpdate);
    }

    static void StripMissingScripts(GameObject go)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
        {
            var comps = t.GetComponents<Component>();
            int miss = comps.Count(c => c == null);
            for (int i = 0; i < miss; i++)
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        }
    }

    static Material ImportMaterial(Material src, out bool approx, out string origShader)
    {
        approx = false;
        origShader = src.shader ? src.shader.name : "<null>";
        if (MatCache.TryGetValue(src, out var cached) && cached != null)
        {
            if (MatMeta.TryGetValue(src, out var meta)) { approx = meta.approx; origShader = meta.origShader; }
            return cached;
        }

        string target = null;
        if (src.shader != null)
        {
            ShaderMap.TryGetValue(src.shader.name, out target);
            // 表里没有的，先试试工程里有没有同名 shader（标准 URP / Sprites 等）。
            // 不试的话会掉到下面的默认值，白白标成「近似替代」
            if (target == null && Shader.Find(src.shader.name) != null) target = src.shader.name;
        }
        if (target == null)
        {
            // 兜底：名字像粒子就用 URP 粒子 shader，否则用 URP Unlit。
            // 别一律用粒子 shader —— 给非粒子材质（比如场景贴图烘焙）套上粒子 shader，
            // 会连粒子专用的顶点色/淡化逻辑一起套进去，实测直接过曝 4 倍
            bool looksParticle = src.shader != null &&
                (src.shader.name.IndexOf("Particles", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 src.shader.name.IndexOf("FX/", StringComparison.OrdinalIgnoreCase) >= 0);
            target = looksParticle
                ? "Universal Render Pipeline/Particles/Unlit*"
                : "Universal Render Pipeline/Unlit*";
        }
        if (target.EndsWith("*")) { target = target.Substring(0, target.Length - 1); approx = true; }

        var sh = Shader.Find(target);
        if (sh == null) { Debug.LogWarning($"材质 {src.name} 找不到 shader {target}"); return null; }

        var mat = new Material(sh) { name = Sanitize(src.name) };

        // 逐属性搬数值（名字对得上就搬）
        var srcSh = src.shader;
        int n = srcSh != null ? srcSh.GetPropertyCount() : 0;
        var names = new string[n];
        for (int i = 0; i < n; i++) names[i] = srcSh.GetPropertyName(i);

        int copied = 0;
        for (int i = 0; i < n; i++)
        {
            var pn = names[i];
            var tp = MapProp(pn);
            if (!mat.HasProperty(tp)) continue;
            var t = srcSh.GetPropertyType(i);
            try
            {
                switch (t)
                {
                    case ShaderPropertyType.Color: mat.SetColor(tp, src.GetColor(pn)); copied++; break;
                    case ShaderPropertyType.Vector: mat.SetVector(tp, src.GetVector(pn)); copied++; break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range: mat.SetFloat(tp, src.GetFloat(pn)); copied++; break;
                    case ShaderPropertyType.Int: mat.SetInt(tp, src.GetInt(pn)); copied++; break;
                    case ShaderPropertyType.Texture:
                        var tx = src.GetTexture(pn);
                        if (tx != null) { mat.SetTexture(tp, ImportTexture(tx)); copied++; }
                        break;
                }
            }
            catch { }
        }

        // 混合 / 渲染状态：原材质有就照搬，没有就从 shader 名推断
        ApplyRenderState(mat, src);

        // URP 粒子的派生属性（软粒子淡出参数等）原版材质里没序列化，不补的话
        // 占位材质在编辑器里也会渲染成全黑。和运行时的 binder 用同一套逻辑，避免两边不一致
        WarpforgeVFX.WarpforgeEffectBinder.ApplyDerivedParticleDefaults(mat, src.shader ? src.shader.name : null);

        var path = AssetDatabase.GenerateUniqueAssetPath($"{MatDir}/{Sanitize(src.name)}.mat");
        AssetDatabase.CreateAsset(mat, path);
        MatCache[src] = mat;
        MatMeta[src] = (approx, origShader);
        return mat;
    }

    static string MapProp(string p)
    {
        switch (p)
        {
            case "_MainTex": return "_BaseMap";
            case "_Color": return "_BaseColor";
            default: return p;
        }
    }

    /// <summary>从原 shader 名推断混合模式。
    /// legacy 粒子 shader（Mobile/Particles/Additive 等）把混合写死在 shader 里，
    /// 材质上根本没有 _SrcBlend/_DstBlend 属性 —— 照搬默认值会把加法发光渲染成不透明。</summary>
    static (BlendMode sb, BlendMode db, float zwrite, bool transparent) InferFromShader(string shaderName)
    {
        string n = (shaderName ?? "").ToLowerInvariant();
        if (n.Contains("additive") || n.Contains("/add") || n.Contains(" add "))
            return (BlendMode.SrcAlpha, BlendMode.One, 0f, true);
        if (n.Contains("premultiply"))
            return (BlendMode.One, BlendMode.OneMinusSrcAlpha, 0f, true);
        if (n.Contains("multiply"))
            return (BlendMode.DstColor, BlendMode.Zero, 0f, true);
        if (n.Contains("alpha blended") || n.Contains("transparent"))
            return (BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha, 0f, true);
        return (BlendMode.One, BlendMode.Zero, 1f, false);        // 不明就按不透明
    }

    static void SetBlend(Material m, BlendMode sb, BlendMode db, float zwrite)
    {
        if (m.HasProperty("_Surface"))   m.SetFloat("_Surface", zwrite < 0.5f ? 1f : 0f);
        if (m.HasProperty("_SrcBlend"))  m.SetFloat("_SrcBlend", (float)sb);
        if (m.HasProperty("_DstBlend"))  m.SetFloat("_DstBlend", (float)db);
        if (m.HasProperty("_ZWrite"))    m.SetFloat("_ZWrite", zwrite);
        if (zwrite < 0.5f) { m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)RenderQueue.Transparent; }
        else               { m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = -1; }
    }

    static void ApplyRenderState(Material dst, Material src)
    {
        string sn = src.shader ? src.shader.name : "";
        bool hasBlend = src.HasProperty("_SrcBlend") && src.HasProperty("_DstBlend");
        float esb = hasBlend ? src.GetFloat("_SrcBlend") : -1f;
        float edb = hasBlend ? src.GetFloat("_DstBlend") : -1f;
        bool explicitOpaque = hasBlend && Mathf.Approximately(esb, 1f) && Mathf.Approximately(edb, 0f);

        if (hasBlend && !explicitOpaque)
        {
            // 原材质明确写了混合模式 → 照搬
            SetBlend(dst, (BlendMode)esb, (BlendMode)edb,
                     src.HasProperty("_ZWrite") ? src.GetFloat("_ZWrite") : 0f);
        }
        else if (src.HasProperty("_Surface"))
        {
            bool tr = src.GetFloat("_Surface") > 0.5f;
            SetBlend(dst, tr ? BlendMode.SrcAlpha : BlendMode.One,
                          tr ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero,
                     tr ? 0f : 1f);
        }
        else
        {
            // 没有混合属性 → 从 shader 名推断
            var (sb, db, zw, _) = InferFromShader(sn);
            SetBlend(dst, sb, db, zw);
        }

        if (dst.HasProperty("_Blend") && src.HasProperty("_Blend")) dst.SetFloat("_Blend", src.GetFloat("_Blend"));
        if (dst.HasProperty("_AlphaClip")) dst.SetFloat("_AlphaClip", src.HasProperty("_AlphaClip") ? src.GetFloat("_AlphaClip") : 0f);
        if (dst.HasProperty("_Cutoff")) dst.SetFloat("_Cutoff", src.HasProperty("_Cutoff") ? src.GetFloat("_Cutoff") : 0.5f);
        if (dst.HasProperty("_Cull")) dst.SetFloat("_Cull", src.HasProperty("_Cull") ? src.GetFloat("_Cull") : (float)CullMode.Back);
        if (dst.GetFloat("_AlphaClip") > 0.5f) dst.EnableKeyword("_ALPHATEST_ON");
    }

        static Texture2D ImportTexture(Texture tex)
    {
        if (tex == null) return null;
        if (tex is Cubemap) return null;                       // 立方图单独处理，先跳过
        if (TexCache.TryGetValue(tex, out var c) && c != null) return c;

        var src = tex as Texture2D;
        if (src == null) return null;

        var readable = ReadableCopy(src);
        if (readable == null) return null;

        byte[] png;
        try { png = readable.EncodeToPNG(); }
        catch { try { png = readable.EncodeToPNG(); } catch { return null; } }

        var path = $"{TexDir}/{Sanitize(tex.name)}.png";
        var full = Path.Combine(Directory.GetCurrentDirectory(), path);
        File.WriteAllBytes(full, png);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.SaveAndReimport();
        }
        var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        TexCache[tex] = asset;
        return asset;
    }

    /// <summary>把 bundle 里的 Sprite 导出成工程资产（一张独立的 PNG + Sprite 导入设置）。
    ///
    /// 为什么要单独导：SpriteRenderer / SpriteMask 的精灵是 bundle 资产，克隆成工程 prefab 时
    /// 引用落不下来 —— Unity 会写一个 guid 全 0 的伪引用，运行时解析成 **null**。
    /// 而 `ParticleSystemRenderer.maskInteraction = VisibleInsideMask` 的粒子**完全靠
    /// SpriteMask 的精灵裁形**，遮罩精灵一空就一个像素都画不出来（实测 78 个 prefab 中招，
    /// 也是 C 组「导出整个丢了」的主因之一）。
    ///
    /// 只导这张 sprite 用到的那一块（图集里的一格），轴心按原 sprite 平移过来。</summary>
    static Sprite ImportSprite(Sprite s)
    {
        if (s == null) return null;
        if (SpriteCache.TryGetValue(s, out var c) && c != null) return c;

        var tex = s.texture as Texture2D;
        if (tex == null) return null;

        var rect = s.textureRect;                       // 在图集里的像素矩形（左下原点）
        int w = Mathf.RoundToInt(rect.width), h = Mathf.RoundToInt(rect.height);
        if (w <= 0 || h <= 0) { rect = new Rect(0, 0, tex.width, tex.height); w = tex.width; h = tex.height; }

        var readable = ReadableCopy(tex);
        if (readable == null) return null;

        var px = readable.GetPixels(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), w, h);
        var sub = new Texture2D(w, h, TextureFormat.RGBA32, false);
        sub.SetPixels(px);
        sub.Apply();
        byte[] png = null;
        try { png = sub.EncodeToPNG(); } catch { }
        UnityEngine.Object.DestroyImmediate(sub);
        if (readable != tex) UnityEngine.Object.DestroyImmediate(readable);
        if (png == null) return null;

        var path = $"{TexDir}/{Sanitize(s.name)}_sprite.png";
        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), png);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = s.pixelsPerUnit > 0f ? s.pixelsPerUnit : 100f;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            // 轴心：Sprite.pivot 是「相对 rect 左下角的像素」，裁完之后相对位置不变，归一化即可。
            // 实测原版这几张（Card Sprite Mask 128²、Card Damage/Heal 256²、
            // Atlas_trait_icon_* 80²）轴心**全是居中** (0.5, 0.5) —— 所以下面按默认的
            // Center 对齐导入就是对的。**如果以后碰到自定义轴心的精灵**，得改用
            // `TextureImporterSettings.spriteAlignment = Custom` + `spritePivot`，
            // 否则精灵会被挪半张图的位置（对 SpriteMask 就是遮罩区域整体偏掉）。
            var sz = s.rect.size;
            ti.spritePivot = new Vector2(
                sz.x > 0f ? s.pivot.x / sz.x : 0.5f,
                sz.y > 0f ? s.pivot.y / sz.y : 0.5f);
            ti.SaveAndReimport();
        }

        var asset = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        SpriteCache[s] = asset;
        return asset;
    }

    /// <summary>拿一份「可读」的 Texture2D。原贴图有 read/write 就直接用，
    /// 否则 blit 到 RenderTexture 再读回来（bundle 里的贴图通常不可读）。</summary>
    static Texture2D ReadableCopy(Texture2D src)
    {
        if (src == null) return null;
        if (src.isReadable) return src;

        var rt = RenderTexture.GetTemporary(src.width, src.height, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(src, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var readable = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
        readable.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return readable;
    }

    static Mesh ImportMesh(Mesh m)
    {
        if (m == null) return null;
        if (MeshCache.TryGetValue(m, out var c) && c != null) return c;
        var copy = UnityEngine.Object.Instantiate(m);
        copy.name = Sanitize(m.name);
        var path = AssetDatabase.GenerateUniqueAssetPath($"{MeshDir}/{copy.name}.asset");
        AssetDatabase.CreateAsset(copy, path);
        MeshCache[m] = copy;
        return copy;
    }

    static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "unnamed";
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace('/', '_').Trim();
    }
}
