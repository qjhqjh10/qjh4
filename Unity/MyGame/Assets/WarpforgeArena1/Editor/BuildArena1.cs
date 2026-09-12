// BuildArena1.cs — 从 arena1_manifest.json 重建 Warpforge battlearena1 战场
//
// 用法（编辑器菜单）：Tools > Warpforge > 构建 battlearena1
// 用法（命令行）：
//   Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod BuildArena1.BuildFromCLI
//
// 数据来源：Assets/WarpforgeArena1/arena1_manifest.json（由 gen_unity_arena_manifest.py 生成）
// 坐标：清单里已是 Unity 世界坐标，直接使用，不做任何手性转换。

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BuildArena1
{
    const string RootDir     = "Assets/WarpforgeArena1";
    const string ManifestPath= RootDir + "/arena1_manifest.json";
    const string ModelDir    = RootDir + "/Models";
    const string TexDir      = RootDir + "/Textures";
    const string MatDir      = RootDir + "/Materials";
    const string ScenePath   = RootDir + "/Scenes/BattleArena1.unity";

    // ---------- JSON 结构（与 Python 侧 schema 严格对应）----------
    [System.Serializable] public class CameraData
    {
        public string name; public float[] pos; public float[] rot;
        public float fov; public float near; public float far; public float lensShiftY;
    }
    [System.Serializable] public class LightData
    {
        public string name; public float[] pos; public float[] rot;
        public float[] color; public float intensity;
    }
    [System.Serializable] public class AmbientData
    {
        public float[] sky; public float[] ground; public float intensity;
    }
    [System.Serializable] public class MeshEntry
    {
        public string go; public string obj; public string objFile;
        public float[] pos; public float[] rot; public float[] scale;
        public string tex; public string texFile;
        public float[] baseColor; public float[] emission;
        public int blend; public int cull; public bool transparent;
        // A2 修复：清单里这组才是权威渲染判据（旧 `blend` 字段在 Warpforge 的自定义
        // shader 里恒为 0，会把 126/350 个真 Alpha 混合材质误判成不透明）
        public int srcBlend; public int dstBlend;
        public bool alphaClip; public bool forceBlend;
        public float texOpaquePct; public float texClearPct;
    }
    [System.Serializable] public class BurstEntry
    {
        public float time; public float count; public int cycles;
        public float interval; public float probability;
    }
    [System.Serializable] public class ParticleEntry
    {
        public string go;
        public float[] pos; public float[] rot; public float[] scale;
        public string tex; public string texFile;
        public float duration; public bool looping; public bool prewarm;
        public float[] startLifetime; public float[] startSpeed; public float[] startSize;
        public float[] startColor; public float gravityModifier; public int maxParticles;
        public float emissionRate; public int shapeType; public float shapeRadius;
        public float shapeAngle; public float shapeArc; public int renderMode; public int simulationSpace;
        public BurstEntry[] bursts;
        // 翻页图集（原版粒子贴图是 8x8 / 6x6 / 5x5 这样的精灵图集，
        // 不开 Texture Sheet Animation 就会把整张图集贴到每个粒子上，渲染成一格格白方块）
        public bool uvEnabled; public int tilesX; public int tilesY;
        public int uvAnimationType; public int uvTimeMode; public float uvFps;
        public float uvCycles; public int uvRowIndex;
    }
    [System.Serializable] public class Manifest
    {
        public string scene;
        public CameraData camera; public LightData light; public AmbientData ambient;
        public MeshEntry[] meshes; public ParticleEntry[] particles;
    }

    // ---------- 入口 ----------
    [MenuItem("Tools/Warpforge/构建 battlearena1")]
    public static void Build() { BuildInternal(); }

    public static void BuildFromCLI() { BuildInternal(); }

    /// <summary>把场景里的相机渲一张图出来，用于无人值守验证</summary>
    public static void RenderPreview()
    {
        const int W = 1280, H = 720;
        var scene = EditorSceneManager.OpenScene(ScenePath);

        var cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null) { Debug.LogError("[Arena1] 预览失败：场景里没有相机"); return; }

        // 批处理下没有 Update 循环，粒子不会自己推进 —— 手动模拟几秒，
        // 否则预览图里粒子全是空的（看起来像没建出来）
        int nSim = 0;
        foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
        {
            ps.Simulate(3f, withChildren: true, restart: true, fixedTimeStep: false);
            nSim++;
        }
        Debug.Log($"[Arena1] 已推进 {nSim} 个粒子系统的模拟");

        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        var prevTarget = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = prevTarget;

        var outPath = $"{RootDir}/preview_arena1.png";
        File.WriteAllBytes(outPath, tex.EncodeToPNG());

        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Debug.Log($"[Arena1] 预览图已保存：{outPath}（{W}x{H}）");
        AssetDatabase.Refresh();
    }

    static void BuildInternal()
    {
        if (!File.Exists(ManifestPath))
        {
            Debug.LogError($"[Arena1] 清单不存在：{ManifestPath}");
            return;
        }

        var json = File.ReadAllText(ManifestPath);
        var mf = JsonUtility.FromJson<Manifest>(json);
        if (mf == null) { Debug.LogError("[Arena1] 清单解析失败"); return; }

        // 新建空场景（用 URP 的话可以改成 URP 模板场景）
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 先清掉上一轮的材质，否则 GenerateUniqueAssetPath 会不断产出 "mat 1" "mat 2" 累积下去
        Directory.CreateDirectory(MatDir);
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { MatDir }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.StartsWith(MatDir)) AssetDatabase.DeleteAsset(p);
        }

        var root = new GameObject("Warpforge_battlearena1");
        int nMesh = 0, nMeshSkip = 0, nPs = 0;

        // ---- 材质缓存 ----
        var matCache = new Dictionary<string, Material>();
        Directory.CreateDirectory(MatDir);

        // ---- 3D 网格 ----
        if (mf.meshes != null)
        {
            foreach (var e in mf.meshes)
            {
                var goName = string.IsNullOrEmpty(e.go) ? "(unnamed)" : e.go;
                var holder = new GameObject(goName);
                holder.transform.SetParent(root.transform, false);
                ApplyTransform(holder.transform, e.pos, e.rot, e.scale);

                if (!string.IsNullOrEmpty(e.objFile))
                {
                    var modelPath = $"{ModelDir}/{e.objFile}";
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                    if (prefab != null)
                    {
                        var src = prefab.GetComponentInChildren<MeshFilter>();
                        if (src != null && src.sharedMesh != null)
                        {
                            var mfGo = new GameObject("mesh");
                            mfGo.transform.SetParent(holder.transform, false);
                            mfGo.AddComponent<MeshFilter>().sharedMesh = src.sharedMesh;
                            var mr = mfGo.AddComponent<MeshRenderer>();
                            mr.shadowCastingMode = ShadowCastingMode.Off;   // 原版战场是烘焙的，无实时阴影
                            mr.receiveShadows = false;
                            mr.sharedMaterial = GetOrCreateMaterial(matCache, e);
                            nMesh++;
                        }
                        else { nMeshSkip++; Debug.LogWarning($"[Arena1] {goName}: OBJ 里没有 MeshFilter -> {modelPath}"); }
                    }
                    else { nMeshSkip++; Debug.LogWarning($"[Arena1] {goName}: 找不到模型 {modelPath}"); }
                }
                else { nMeshSkip++; Debug.LogWarning($"[Arena1] {goName}: 清单里没有 objFile（跳过网格）"); }
            }
        }

        // ---- 粒子特效 ----
        if (mf.particles != null)
        {
            foreach (var p in mf.particles)
            {
                var go = new GameObject(string.IsNullOrEmpty(p.go) ? "PS" : p.go);
                go.transform.SetParent(root.transform, false);
                ApplyTransform(go.transform, p.pos, p.rot, p.scale);

                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.duration        = Mathf.Max(0.01f, p.duration);
                main.loop            = p.looping;
                main.prewarm         = p.prewarm;
                main.startLifetime   = Curve(p.startLifetime, 1f);
                main.startSpeed      = Curve(p.startSpeed, 1f);
                main.startSize       = Curve(p.startSize, 1f);
                main.startColor      = ToColor(p.startColor);
                main.gravityModifier = p.gravityModifier;
                main.maxParticles    = Mathf.Max(1, p.maxParticles);
                main.simulationSpace = (ParticleSystemSimulationSpace)Mathf.Clamp(p.simulationSpace, 0, 2);

                var em = ps.emission; em.rateOverTime = Mathf.Max(0f, p.emissionRate);
                // 爆发发射：原版有 14/34 个粒子靠 burst 驱动（emissionRate=0），
                // 不设这里它们在 Unity 里一个粒子都不发
                if (p.bursts != null && p.bursts.Length > 0)
                {
                    var bursts = new ParticleSystem.Burst[p.bursts.Length];
                    for (int i = 0; i < p.bursts.Length; i++)
                    {
                        var b = p.bursts[i];
                        // 注意：Burst 的构造参数是 _time/_minCount/... 带下划线前缀，不能用命名参数
                        short cnt = (short)Mathf.Clamp(Mathf.RoundToInt(b.count), 0, 65535);
                        bursts[i] = new ParticleSystem.Burst(
                            b.time,
                            cnt,
                            cnt,
                            Mathf.Max(1, b.cycles),
                            Mathf.Max(0.01f, b.interval));
                        bursts[i].probability = Mathf.Clamp01(b.probability <= 0f ? 1f : b.probability);
                    }
                    em.SetBursts(bursts);
                }

                var sh = ps.shape;
                sh.enabled = true;
                sh.shapeType = (ParticleSystemShapeType)Mathf.Clamp(p.shapeType, 0, 20);
                sh.radius  = p.shapeRadius;
                sh.angle   = p.shapeAngle;
                // Unity 的 ShapeModule.arc 单位是「度」，而清单里存的是 Unity 原始的「弧度」值。
                // 用数值大小兜底：<= 2π+ε 当弧度转，否则认为已经是度。
                if (p.shapeArc > 0f)
                {
                    float arcDeg = p.shapeArc <= (Mathf.PI * 2f + 0.01f)
                                 ? p.shapeArc * Mathf.Rad2Deg
                                 : p.shapeArc;
                    sh.arc = Mathf.Clamp(arcDeg, 0f, 360f);
                }

                // 翻页图集：按 UVModule 的网格切分，否则整张精灵图集会被贴在每个粒子上
                if (p.uvEnabled && p.tilesX > 0 && p.tilesY > 0 && (p.tilesX > 1 || p.tilesY > 1))
                {
                    var tsa = ps.textureSheetAnimation;
                    tsa.enabled     = true;
                    tsa.numTilesX   = p.tilesX;
                    tsa.numTilesY   = p.tilesY;
                    tsa.animation   = (ParticleSystemAnimationType)Mathf.Clamp(p.uvAnimationType, 0, 1);
                    tsa.timeMode    = (ParticleSystemAnimationTimeMode)Mathf.Clamp(p.uvTimeMode, 0, 2);
                    if (p.uvFps > 0f)    tsa.fps = p.uvFps;
                    if (p.uvCycles > 0f) tsa.cycleCount = Mathf.Max(1, Mathf.RoundToInt(p.uvCycles));
                    tsa.rowIndex = Mathf.Max(0, p.uvRowIndex);
                }

                var rend = go.GetComponent<ParticleSystemRenderer>();
                // 只处理公告板家族（0=Billboard 1=Stretch 2=Horizontal 3=Vertical）；
                // 4=Mesh 需要指定网格，清单里没带，降级成 Billboard 免得渲染不出来
                int rm = Mathf.Clamp(p.renderMode, 0, 4);
                rend.renderMode = rm == 4 ? ParticleSystemRenderMode.Billboard
                                          : (ParticleSystemRenderMode)rm;
                rend.material   = GetOrCreateParticleMaterial(texName: p.texFile);
                rend.shadowCastingMode = ShadowCastingMode.Off;
                rend.receiveShadows = false;

                ps.Play();
                nPs++;
            }
        }

        // ---- 相机 ----
        if (mf.camera != null)
        {
            var camGo = new GameObject(string.IsNullOrEmpty(mf.camera.name) ? "BoardCamera" : mf.camera.name);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView    = mf.camera.fov > 0 ? mf.camera.fov : 46.4f;
            cam.nearClipPlane  = mf.camera.near > 0 ? mf.camera.near : 0.3f;
            cam.farClipPlane   = mf.camera.far  > 0 ? mf.camera.far  : 300f;
            cam.lensShift      = new Vector2(0f, mf.camera.lensShiftY);
            ApplyTransform(camGo.transform, mf.camera.pos, mf.camera.rot, new float[] { 1, 1, 1 });
            camGo.AddComponent<AudioListener>();
        }

        // ---- 灯光 ----
        if (mf.light != null)
        {
            var lGo = new GameObject(string.IsNullOrEmpty(mf.light.name) ? "Directional Light" : mf.light.name);
            var li = lGo.AddComponent<Light>();
            li.type      = LightType.Directional;
            li.color     = ToColor(mf.light.color);
            li.intensity = mf.light.intensity > 0 ? mf.light.intensity : 1f;
            li.shadows   = LightShadows.Soft;
            ApplyTransform(lGo.transform, mf.light.pos, mf.light.rot, new float[] { 1, 1, 1 });
        }

        // ---- 环境光 ----
        if (mf.ambient != null)
        {
            RenderSettings.ambientMode      = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor    = ToColor(mf.ambient.sky);
            RenderSettings.ambientGroundColor = ToColor(mf.ambient.ground);
            RenderSettings.ambientIntensity   = mf.ambient.intensity > 0 ? mf.ambient.intensity : 0.41f;
            RenderSettings.fog = false;
        }

        // ---- 保存场景 ----
        Directory.CreateDirectory($"{RootDir}/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Arena1] 完成：网格 {nMesh} 个（跳过 {nMeshSkip}）、粒子 {nPs} 个、相机 {(mf.camera != null)}、灯光 {(mf.light != null)}");
        Debug.Log($"[Arena1] 场景已保存：{ScenePath}");
    }

    // ---------- 工具 ----------
    static void ApplyTransform(Transform t, float[] pos, float[] rot, float[] scale)
    {
        t.localPosition = ToVec3(pos, Vector3.zero);
        t.localRotation = ToQuat(rot);
        t.localScale    = ToVec3(scale, Vector3.one);
    }

    static Vector3 ToVec3(float[] a, Vector3 fallback)
        => (a != null && a.Length >= 3) ? new Vector3(a[0], a[1], a[2]) : fallback;

    static Quaternion ToQuat(float[] a)
        => (a != null && a.Length >= 4) ? new Quaternion(a[0], a[1], a[2], a[3]) : Quaternion.identity;

    static Color ToColor(float[] a)
        => (a != null && a.Length >= 3)
             ? new Color(a[0], a[1], a[2], a.Length >= 4 ? a[3] : 1f)
             : Color.white;

    static ParticleSystem.MinMaxCurve Curve(float[] mm, float fallback)
    {
        if (mm != null && mm.Length >= 2) return new ParticleSystem.MinMaxCurve(mm[0], mm[1]);
        if (mm != null && mm.Length == 1) return new ParticleSystem.MinMaxCurve(mm[0], mm[0]);
        return new ParticleSystem.MinMaxCurve(fallback, fallback);
    }

    /// <summary>读取纹理，找不到就返回 null（用白图兜底）</summary>
    static Texture GetTexture(string texFile)
    {
        if (string.IsNullOrEmpty(texFile)) return null;
        var path = $"{TexDir}/{texFile}";
        var t = AssetDatabase.LoadAssetAtPath<Texture>(path);
        if (t == null) Debug.LogWarning($"[Arena1] 找不到贴图：{path}");
        return t;
    }

    /// <summary>
    /// 源图是否带 alpha 通道。
    /// 背景板那几张（Battle Arena 1 Background / Back Background）的透明区 RGB 是白色，
    /// 若按不透明渲染就会糊成大白板 —— 必须靠这个判定走 alpha 混合。
    /// </summary>
    static bool TextureHasAlpha(string texFile)
    {
        if (string.IsNullOrEmpty(texFile)) return false;
        var path = $"{TexDir}/{texFile}";
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        return ti != null && ti.DoesSourceTextureHaveAlpha();
    }

    /// <summary>为本体网格建 URP/Unlit 材质（原版战场是烘焙贴图 + 无光照，Unlit 最接近）</summary>
    static Material GetOrCreateMaterial(Dictionary<string, Material> cache, MeshEntry e)
    {
        var key = $"{e.texFile}|{e.transparent}|{e.blend}|{e.cull}";
        if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Texture");
        var mat = new Material(shader) { name = Sanitize(string.IsNullOrEmpty(e.tex) ? e.go : e.tex) };

        var tex = GetTexture(e.texFile);
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", ToColor(e.baseColor));
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", ToColor(e.baseColor));

        // 描边剔除：Unity cull 0=Off 1=Front 2=Back
        if (e.cull == 0) mat.SetFloat("_Cull", (float)CullMode.Off);
        else if (e.cull == 1) mat.SetFloat("_Cull", (float)CullMode.Front);
        else mat.SetFloat("_Cull", (float)CullMode.Back);

        // 自发光（原版地面/围栏 emission=0.19 就是靠这个提亮）
        var em = ToColor(e.emission);
        if (em.maxColorComponent > 0.001f)
        {
            if (mat.HasProperty("_EmissionColor")) mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", em);
        }

        // 渲染模式判据（踩过两次坑，结论如下）：
        //  1. 不能用旧 `blend` 字段 —— Warpforge 的自定义 shader 里它恒为 0，
        //     会把 srcBlend=5/dstBlend=10 的真 Alpha 混合材质误判成不透明（审计 A2）。
        //  2. 也不能优先用 Alpha 裁剪 —— 贴图是**共享图集**（BattleArena1 Texture Baked 整张
        //     71.6% 是空白透明区），但单个网格只用自己的 UV 那一小块，整图统计不代表本网格；
        //     而且裁剪会把软边切成硬边并打出镂空。
        //  3. 最终取「优先混合」：alpha=1 时 Alpha 混合是恒等变换，不会破坏不透明区域，
        //     而透明区能正确透出背景 —— 这是视觉效果最稳的一档。
        if (e.transparent || e.forceBlend || e.alphaClip || TextureHasAlpha(e.texFile))
            MakeTransparent(mat);

        cache[key] = mat;
        var path = $"{MatDir}/{mat.name}.mat";
        path = AssetDatabase.GenerateUniqueAssetPath(path);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    /// <summary>粒子材质：透明 Unlit，用清单里指定的贴图</summary>
    static Material GetOrCreateParticleMaterial(string texName)
    {
        // ⚠️ 必须用 **Particles/Unlit**，不能用 `URP/Unlit`：后者**不乘粒子顶点色**，
        //    于是 startColor 里的灰度/透明度全丢，烟和蒸汽渲出来是一坨白方块（实拍踩过）。
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        var mat = new Material(shader) { name = "PS_" + Sanitize(texName) };
        var tex = GetTexture(texName);
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }
        MakeTransparent(mat);
        var path = AssetDatabase.GenerateUniqueAssetPath($"{MatDir}/{mat.name}.mat");
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    /// <summary>Alpha 裁剪（对应原版 _ALPHATEST_ON 的材质，如背景建筑板）</summary>
    static void MakeAlphaClip(Material mat)
    {
        if (mat.HasProperty("_Surface"))   mat.SetFloat("_Surface", 0f);   // 仍是 Opaque
        if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 1f);
        if (mat.HasProperty("_Cutoff"))    mat.SetFloat("_Cutoff", 0.5f);
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHABLEND_ON");
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.One);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
        if (mat.HasProperty("_ZWrite"))   mat.SetFloat("_ZWrite", 1f);
        mat.renderQueue = (int)RenderQueue.AlphaTest;
    }

    static void MakeTransparent(Material mat)
    {
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);          // 0=Opaque 1=Transparent
            mat.SetFloat("_Blend", 0f);            // Alpha
            mat.SetFloat("_AlphaClip", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
        }
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite"))   mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = (int)RenderQueue.Transparent;
    }

    static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "unnamed";
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace('/', '_').Trim();
    }
}
