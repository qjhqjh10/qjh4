// ArenaBuilder.cs — 从清单 JSON 重建 Warpforge 战场（**13 个战场共用这一个搬运链**）
//
// 🔴 2026-09-20 改名 + 参数化：原来叫 `BuildArena1`、只建 battlearena1。现在逐场的东西全都参数化。
//
// 用法（编辑器菜单）：Tools > Warpforge > 构建 battlearena1
// 用法（命令行，用**环境变量 `WF_ARENA`** 选战场 —— `-executeMethod` 本身收不了参数）：
//   unset ELECTRON_RUN_AS_NODE && WF_ARENA=battlearenaspacewolves \
//     Unity.exe -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod ArenaBuilder.BuildFromCLI
//   全建一遍：-executeMethod ArenaBuilder.BuildAll
//
// 数据来源：Assets/WarpforgeArena1/arenas/<场景名>/<场景名>_manifest.json
//           （由 `gen_unity_arena_manifest.py --arena <场景名>` 生成）
// 坐标：清单里已是 Unity 世界坐标，直接使用，不做任何手性转换。
//
// 🔴 **灯 / 环境光 / 后处理**（2026-09-20 接的，正本 = `资料/普查产出_0920/场景光照与后处理_原版规格.md`）：
//    三样都写在 `BuildContent` 里 → 独立战场场景与战斗场景**共用同一段**（判据只留一处）。
//    原来灯与环境光只在 `BuildSceneTail`（只给独立场景用）⇒ **战斗场景里一盏灯都没有**。

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class ArenaBuilder
{
    // 🔴 2026-09-20：本类从「只建 battlearena1」改成「建**任意**原版战场」。
    //    逐场的东西全部参数化 —— 原版 13 个战场共用同一条搬运链，只有这几条路径不同。
    //    生成器侧同样是参数化的：`gen_unity_arena_manifest.py --arena <场景名>`。
    const string RootDir    = "Assets/WarpforgeArena1";
    const string ScenesDir  = RootDir + "/Scenes";

    /// <summary>**13 个原版战场场景名**（= Addressables key，逐字来自 `catalog.bin`）。</summary>
    public static readonly string[] AllArenas = {
        "battlearena1", "battlearena2", "battlearena3",
        "battlearenaaeldari", "battlearenaastramilitarum", "battlearenablacklegion",
        "battlearenadarkangels", "battlearenaemperorschildren", "battlearenagenestealers",
        "battlearenaleviathan", "battlearenasororitas", "battlearenaspacewolves",
        "battlearenatauviorla",
    };

    /// <summary>本局用哪个战场。
    /// 🔴 **原版的判据**：`SearchOpponentManager.StartBattle()` → `BattleArenaByArmySO.GetBattleArena(army)`
    /// → 场景名（army 默认 = 本地玩家的督军阵营；11 种竞技 matchType 且本地先手时覆盖为对手的）。
    /// ⚠️ **那张 `CardArmy → 场景名` 映射表本地没有**（`assets_full` 246,680 个文件全扫过，
    /// 只命中 `dump.cs` 的类型定义）⇒ **现在固定 arena1**，拿到表再换。
    /// 详见 `资料/普查产出_0920/场景光照与后处理_原版规格.md` §六。**判据只此一处，别在别处再写死。**</summary>
    public const string DefaultArena = "battlearena1";

    public static string ArenaDir(string scene)   => RootDir + "/arenas/" + scene;
    public static string ManifestPath(string s)   => ArenaDir(s) + "/" + s + "_manifest.json";
    public static string ModelDir(string scene)   => ArenaDir(scene) + "/Models";
    public static string TexDir(string scene)     => ArenaDir(scene) + "/Textures";
    public static string MatDir(string scene)     => ArenaDir(scene) + "/Materials";
    public static string ProfileDir(string scene) => ArenaDir(scene) + "/Profiles";
    public static string ScenePath(string scene)  => ScenesDir + "/" + scene + ".unity";

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
        // 2026-09-20 补：原版 13 场的 m_Shadows 各不相同（soft/hard/none × 强度四档），
        // 原来只写死 LightShadows.Soft、强度从没搬过。判据 = 原版 `Light.m_Shadows`。
        public int shadowType; public float shadowStrength; public float shadowBias;
    }
    [System.Serializable] public class AmbientData
    {
        // 2026-09-20 更正：原来只有 sky/ground/intensity。原版 13 场 m_AmbientMode **全是 3（Flat）**，
        // 不是 1（Trilight）—— 模式错了，抄对颜色也没用。equator/fog 一并带上。
        public int mode; public float[] sky; public float[] equator; public float[] ground;
        public float intensity; public bool fog; public float[] fogColor; public float fogDensity;
    }
    /// <summary>运行时真正的环境光来源（2026-09-20 新增）。
    /// 原版 `EnvironmentConditionsController/ScenarioEnvironmentConditionsManager.Awake()` 会用
    /// 它覆盖 `RenderSettings.ambientLight` —— 场景里的 `m_AmbientSkyColor` **不是**运行时值。
    /// 实况探针实测（arena1 / arena3）与全量反编译两条独立证据一致。</summary>
    [System.Serializable] public class EnvData
    {
        public string so; public string hostBundle; public long pathId;
        public float blendTime; public float[] ambientColor; public float ambientBlend;
        public float[] fogColor; public float fogDensity;
    }
    /// <summary>后处理（2026-09-20 新增）。原版战场在 BoardCamera 上挂了个全局 Volume。</summary>
    [System.Serializable] public class PostFxComp
    {
        public string type;
        public float[] color; public float[] center;
        public float skipIterations; public float threshold; public float intensity;
        public float scatter; public float contribution; public float maxIterations;
    }
    [System.Serializable] public class PostFxData
    {
        public string profile; public bool isGlobal; public float priority; public float weight;
        public PostFxComp[] components;
    }
    /// <summary>一个网格可能有**多个子网格、每个一个材质**（OBJ 里的 `g &lt;名&gt;_0` / `_1` …）。
    /// 🔴 2026-09-20：原来只取第 0 个材质，其余子网格拿不到材质 ⇒ **Unity 用默认灰材质**。
    /// 实测后果：圣女战场的 `Floor` 整个变成一片均匀灰（(106,101,96)，原版是红毯+大理石）。
    /// 13 场里 **6 场共 29 个网格**有多子网格（arena2 的 `Combined Mesh` 有 **31 个**）。</summary>
    [System.Serializable] public class SubMatEntry
    {
        public string tex; public string texFile;
        public float[] baseColor; public float[] emission;
        public int cull; public bool transparent;
        public int srcBlend; public int dstBlend;
        public bool alphaClip; public bool forceBlend;
        /// <summary>2026-09-20：这个材质的混合**由 shader 属性表决定** —— 属性表里没声明
        /// `_SrcBlend` ⇒ 材质上那两个值是内置 Standard shader 的**残留值**，真值在 pass 的
        /// `rtBlend0`（硬编码）。为真时下方那两条「看贴图整图统计」的兜底
        /// （`forceBlend` / `TextureHasAlpha`）**一律让位** —— 否则会把一块真不透明的地板
        /// 又拉回透明（实例 = sororitas 的 `Floor`，见 `普查产出_0920/…原版规格.md` §13.1）。</summary>
        public bool blendAuthoritative;
        /// <summary>2026-09-20：**原版 shader 名** —— 见 `MeshEntry.shader`。</summary>
        public string shader;
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
        /// <summary>2026-09-20：**原版 shader 名**（`Everguild/UnlitAmbient` 等）—— 建材质时拿它去
        /// `WarpforgeShaderLoader.TryGetShader` 取原版编译字节码。取不到就退回 `URP/Unlit`。
        /// 旧清单没有这个字段 ⇒ null ⇒ 全退回 URP/Unlit（= 老行为）。</summary>
        public string shader;
        /// <summary>见 `SubMatEntry.blendAuthoritative`（同一个判据，顶层材质那份）。</summary>
        public bool blendAuthoritative;
        public float texOpaquePct; public float texClearPct;
        /// <summary>每个子网格一个材质（顺序 = 原版 MeshRenderer 的 `m_Materials` 顺序）。
        /// ⚠️ 旧清单没有这个字段 ⇒ 反序列化成 null/空数组，走原来的「单材质」那条路。</summary>
        public SubMatEntry[] subMats;
        /// <summary>多子网格时，**按组拆出来的若干 OBJ**（与 `subMats` 一一对应）。
        /// 为什么要拆：实测 **Unity 的 OBJ 导入器不切子网格**（`g` 组和 `usemtl` 都不认，
        /// 一个文件永远只导 1 个子网格）—— 详见生成器 `split_obj_by_group`。</summary>
        public string[] subFiles;
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
        public EnvData defaultEnv;
        public PostFxData postFx;
        public MeshEntry[] meshes; public ParticleEntry[] particles;
    }

    // ---------- 入口 ----------
    /// <summary>`-executeMethod ArenaBuilder.BuildFromCLI` 用**环境变量 `WF_ARENA`** 选战场
    /// （默认 `DefaultArena`；`-executeMethod` 本身收不了参数）。菜单项固定建默认那个。</summary>
    static string ArenaFromEnv()
    {
        var s = System.Environment.GetEnvironmentVariable("WF_ARENA");
        if (string.IsNullOrEmpty(s)) return DefaultArena;
        if (System.Array.IndexOf(AllArenas, s) < 0)
        {
            Debug.LogError($"[Arena] WF_ARENA='{s}' 不是 13 个战场之一，回退 {DefaultArena}");
            return DefaultArena;
        }
        return s;
    }

    [MenuItem("Tools/Warpforge/构建 battlearena1")]
    public static void Build() { BuildInternal(DefaultArena); }

    public static void BuildFromCLI() { BuildInternal(ArenaFromEnv()); }

    /// <summary>把 13 个战场**全建一遍**（每个存一个独立场景）—— 批量入口，串行跑。</summary>
    public static void BuildAll()
    {
        foreach (var s in AllArenas) BuildInternal(s);
    }

    /// <summary>把 13 个战场各渲一张预览图（`arenas/&lt;场景&gt;/preview_&lt;场景&gt;.png`）。
    /// 用途 = **验收**：13 场搬过来了没有、建出来像不像（⚠️ 参数仍以清单/原始 JSON 为准，预览图只回答「像不像」）。</summary>
    public static void RenderAllPreviews()
    {
        foreach (var s in AllArenas)
        {
            if (!File.Exists(ScenePath(s))) { Debug.LogWarning($"[Arena] 没有场景 {ScenePath(s)}，跳过预览"); continue; }
            RenderPreview(s);
        }
    }

    /// <summary>只渲 `WF_ARENA` 指定的那一场（2026-09-20 加）—— `RenderAllPreviews` 要跑全 13 场，
    /// 单独验一场时太慢。与 `BuildFromCLI` 同款：用环境变量选场。</summary>
    public static void RenderPreviewFromCLI()
    {
        var s = ArenaFromEnv();
        if (!File.Exists(ScenePath(s))) { Debug.LogError($"[Arena] 没有场景 {ScenePath(s)}，无法渲预览"); return; }
        RenderPreview(s);
    }

    /// <summary>诊断：把所有**透明队列**的网格连同贴图格式、材质参数、世界包围盒列出来
    /// （2026-09-20 加 —— 查「sororitas 底部 `Foreground Decorations` 看不见」时用）。
    /// 判据 = `renderQueue >= 3000`（`MakeTransparent` 会把它们设成 3000）。
    /// 用法：`WF_ARENA=battlearenasororitas ... -executeMethod ArenaBuilder.ProbeTransparent`</summary>
    public static void ProbeTransparent()
    {
        var s = ArenaFromEnv();
        if (!File.Exists(ScenePath(s))) { Debug.LogError($"[PT] 没有场景 {ScenePath(s)}"); return; }
        EditorSceneManager.OpenScene(ScenePath(s));
        int n = 0;
        foreach (var mr in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            var m = mr.sharedMaterial;
            if (m == null || m.renderQueue < 3000) continue;
            n++;
            var tex = m.GetTexture("_BaseMap") as Texture2D;
            var b = mr.bounds;
            Debug.Log($"[PT] {mr.name,-34} q={m.renderQueue} tex={tex?.name ?? "(无)"} fmt={(tex != null ? tex.format.ToString() : "-")}"
                    + $" surf={m.GetFloat("_Surface")} src={m.GetFloat("_SrcBlend")} dst={m.GetFloat("_DstBlend")} zw={m.GetFloat("_ZWrite")}"
                    + $" kw=[{string.Join(",", m.shaderKeywords)}]"
                    + $" c=({b.center.x:F2},{b.center.y:F2},{b.center.z:F2}) sz=({b.size.x:F2},{b.size.y:F2},{b.size.z:F2})");
        }
        Debug.Log($"[PT] === 透明队列网格共 {n} 个（场景 {s}）===");
    }

    /// <summary>诊断用：**摘掉天空盒、把背景刷成亮绿**再渲一张 ⇒ **画面里绿的地方 = 没有几何的"洞"**
    /// （2026-09-20 加 —— 查「sororitas 底部两角看不见」时用；正常预览不受影响）。
    /// 用法：`WF_ARENA=battlearenasororitas ... -executeMethod ArenaBuilder.RenderHolesFromCLI`</summary>
    public static void RenderHolesFromCLI()
    {
        RenderPreview(ArenaFromEnv(), debugHoles: true);
    }

    /// <summary>把场景里的相机渲一张图出来，用于无人值守验证</summary>
    public static void RenderPreview(string scene, bool debugHoles = false)
    {
        const int W = 1280, H = 720;
        var scenePath = ScenePath(scene);
        EditorSceneManager.OpenScene(scenePath);

        var cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null) { Debug.LogError("[Arena] 预览失败：场景里没有相机"); return; }

        if (debugHoles)
        {
            RenderSettings.skybox = null;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 1f, 0f);   // 亮绿 = 洞
        }

        // 批处理下没有 Update 循环，粒子不会自己推进 —— 手动模拟几秒，
        // 否则预览图里粒子全是空的（看起来像没建出来）
        int nSim = 0;
        foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
        {
            ps.Simulate(3f, withChildren: true, restart: true, fixedTimeStep: false);
            nSim++;
        }
        Debug.Log($"[Arena] 已推进 {nSim} 个粒子系统的模拟");

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

        var outPath = debugHoles
            ? $"{ArenaDir(scene)}/holes_{scene}.png"
            : $"{ArenaDir(scene)}/preview_{scene}.png";
        File.WriteAllBytes(outPath, tex.EncodeToPNG());

        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Debug.Log($"[Arena] 预览图已保存：{outPath}（{W}x{H}）");
        AssetDatabase.Refresh();
    }

    static void BuildInternal(string sceneName)
    {
        var mf = LoadManifest(sceneName);
        if (mf == null) return;

        // 新建空场景（用 URP 的话可以改成 URP 模板场景）
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        ClearMaterialDir(sceneName);
        BuildContent(null, mf);
        BuildSceneTail(mf, scene);
    }

    /// <summary>给某个战场的贴图定导入设置。
    /// 🔴 **判据 = 原版运行时的 `Texture2D` 实读值**（2026-09-20 实测 `scenes_scenes_battlearena1.bundle`）：
    ///   · `Battle Arena 1 Floor` = **4096×4096** · **`m_MipCount = 1`（= 原版没有 mipmap）** · `m_IsReadable = false`
    ///   · `Battle Arena 1 Background` / `Back Background` = 2048×2048 · 同样 mips=1
    /// 而 Unity 默认导入是 **`maxTextureSize 2048` + `enableMipMap 1`**
    /// ⇒ 我们那张 4096² 的地板**被砍成一半**、还多生成了一整套 mipmap。**两处都与原版不符。**
    /// ⚠️ 这条以前没人管过（`ArtBaker.ApplyImportSettings` 只覆盖 `Resources/Art`，不含战场贴图）。</summary>
    public static void ApplyTextureImportSettings(string sceneName)
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexDir(sceneName) }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;

            ti.textureType         = TextureImporterType.Default;
            ti.mipmapEnabled       = false;                       // 原版 m_MipCount = 1
            ti.npotScale           = TextureImporterNPOTScale.None;
            ti.alphaIsTransparency = false;                       // 别让 Unity 用最近邻补透明区的 RGB
            // 别被 Unity 默认的 2048 砍掉 —— 按源图最大边取到 2 的幂（原版地板就是 4096²）
            ti.GetSourceTextureWidthAndHeight(out int srcW, out int srcH);
            int maxSide = Mathf.Max(srcW, srcH);
            ti.maxTextureSize = Mathf.Clamp(Mathf.NextPowerOfTwo(maxSide), 512, 8192);
            ti.SaveAndReimport();
            n++;
        }
        Debug.Log($"[Arena] {sceneName} 贴图导入设置已按原版实读值重设：{n} 张（无 mipmap · 不缩分辨率）");
    }

    /// <summary>把战场内容（网格 + 粒子）建进**当前场景**，返回根节点。
    /// 独立场景（`BuildInternal`）与战斗场景（`BattleScene.BuildScene`）**共用这一段** —— 判据只留一处。</summary>
    public static GameObject BuildContent(Transform parent, Manifest mf)
    {
        var root = new GameObject("Warpforge_" + mf.scene);
        if (parent != null) root.transform.SetParent(parent, false);
        int nMesh = 0, nMeshSkip = 0, nPs = 0, nPsNoTex = 0;

        // 贴图导入设置：**必须在这里做**（两处调用者都走 BuildContent）——
        // 分辨率上限不对的话，地板会被 Unity 默认的 2048 砍成一半（见方法头）。
        ApplyTextureImportSettings(mf.scene);

        // ---- 材质缓存 ----
        var matCache = new Dictionary<string, Material>();
        Directory.CreateDirectory(MatDir(mf.scene));

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
                    var modelPath = $"{ModelDir(mf.scene)}/{e.objFile}";
                    // 🔴 多子网格（原版一个网格多个材质）走**拆文件**那条路：
                    //    实测**Unity 的 OBJ 导入器不切子网格**（原文件只有 `g` 组 ⇒ 1 个子网格；
                    //    补上 `usemtl` 之后**仍然是 1 个**）⇒ 只能按组拆成多个 OBJ，
                    //    一个子网格建一个网格对象、各配各的材质（几何无损：共用同一段顶点块与同一个 Transform）。
                    //    见生成器 `split_obj_by_group`。踩过：圣女战场 `Floor`（2 个材质，
                    //    第 0 个透明、第 1 个不透明）整个人套第 0 个 ⇒ 地板整片透掉，屏幕上是一块均匀灰。
                    if (e.subFiles != null && e.subFiles.Length > 1)
                    {
                        for (int i = 0; i < e.subFiles.Length; i++)
                        {
                            var subPath = $"{ModelDir(mf.scene)}/{e.subFiles[i]}";
                            var subPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(subPath);
                            var subSrc = subPrefab != null ? subPrefab.GetComponentInChildren<MeshFilter>() : null;
                            if (subSrc == null || subSrc.sharedMesh == null)
                            { nMeshSkip++; Debug.LogWarning($"[Arena] {goName}[{i}]: 拆出来的 OBJ 读不出网格 -> {subPath}"); continue; }

                            var subGo = new GameObject($"mesh{i}");
                            subGo.transform.SetParent(holder.transform, false);
                            subGo.AddComponent<MeshFilter>().sharedMesh = subSrc.sharedMesh;
                            var subMr = subGo.AddComponent<MeshRenderer>();
                            subMr.shadowCastingMode = ShadowCastingMode.Off;
                            subMr.receiveShadows = false;
                            var m = (e.subMats != null && i < e.subMats.Length)
                                  ? GetOrCreateMaterial(mf.scene, matCache, e.subMats[i], e.go)
                                  : GetOrCreateMaterial(mf.scene, matCache, e);
                            subMr.sharedMaterial = m;
                            nMesh++;
                        }
                    }
                    else
                    {
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
                                // ⚠️ 单文件（Unity 导成 1 个子网格）时，清单里的 `subMats` 若有多条也没用
                                //    —— 那是「原版有多个材质但拆不出来」的残留，这里如实报一句。
                                if (e.subMats != null && e.subMats.Length > 1)
                                    Debug.LogWarning($"[Arena] 🔴 {goName}：清单给了 {e.subMats.Length} 个材质，"
                                                   + "但这个 OBJ 没拆出多个子网格 ⇒ 只用了第 0 个");
                                mr.sharedMaterial = GetOrCreateMaterial(mf.scene, matCache, e);
                                nMesh++;
                            }
                            else { nMeshSkip++; Debug.LogWarning($"[Arena] {goName}: OBJ 里没有 MeshFilter -> {modelPath}"); }
                        }
                        else { nMeshSkip++; Debug.LogWarning($"[Arena] {goName}: 找不到模型 {modelPath}"); }
                    }
                }
                else { nMeshSkip++; Debug.LogWarning($"[Arena] {goName}: 清单里没有 objFile（跳过网格）"); }
            }
        }

        // ---- 粒子特效 ----
        if (mf.particles != null)
        {
            foreach (var p in mf.particles)
            {
                // 🔴 2026-09-20：**原版材质没有贴图的粒子，一律不建**。
                //    这些是**扭曲 / 叠加辉光**类（`Heat Distortion` · `Muzzle Flash view distort` ·
                //    `Light` · `Necrons Close Monolith Rays` · `Lance Fire`），原版靠自己的
                //    screen-grab / additive shader 出效果，**材质里本来就没有贴图**。
                //    我们原来给它们建了 `URP/Particles/Unlit` + 空贴图 ⇒ **渲成一坨不透明白方块**
                //    （blacklegion 那 4 条横白板就是这么来的，和原版真渲图一比就露）。
                //    **宁可不建、也不画错的**（本项目的「不许静默失败」）—— 但**必须报出来**，见下面的汇总。
                if (string.IsNullOrEmpty(p.texFile))
                {
                    nPsNoTex++;
                    continue;
                }
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
                rend.material   = GetOrCreateParticleMaterial(mf.scene, texName: p.texFile);
                rend.shadowCastingMode = ShadowCastingMode.Off;
                rend.receiveShadows = false;

                ps.Play();
                nPs++;
            }
        }

        if (nPsNoTex > 0)
            Debug.LogWarning($"[Arena] 🔴 {mf.scene}：**{nPsNoTex} 个粒子没建** —— 它们的原版材质没有贴图"
                           + "（扭曲/叠加辉光类，靠 screen-grab / additive shader 出效果）。"
                           + "硬建会渲成不透明白方块 ⇒ 宁可不建。**这是已知缺口**，不是「做完了」："
                           + "要还原得给它们接对应的原版 shader（见 `资料/普查产出_0920/场景光照与后处理_原版规格.md` §十一）。");

        Debug.Log($"[Arena] 内容：网格 {nMesh} 个（跳过 {nMeshSkip}）、粒子 {nPs} 个（另跳过无贴图 {nPsNoTex} 个）");
        ApplyLightAndAmbient(root.transform, mf);
        ApplyPostFx(root.transform, mf);
        SetupSkybox();
        return root;
    }

    /// <summary>天空盒（2026-09-20 加）—— **独立战场场景与战斗场景共用**（判据只留一处）。
    ///
    /// 为什么：**原版 13 个战场的 `RenderSettings.m_SkyboxMaterial` 全指向同一个 `Skybox Clouds Dusk`**
    /// （`battlesharedresources_assets_all` pid `-246617819069842608`），而我们**一个都没设**
    /// ⇒ 战场上有洞的地方透出 **Unity 默认天空盒**（灰褐），原版那里是黄昏云。
    /// 实测最明显的是 sororitas：地板判据修好之后，红毯护栏之外那两角仍是灰褐。
    /// 见 `资料/普查产出_0920/场景光照与后处理_原版规格.md` §13.1。
    ///
    /// 规格**逐值实读，别改**：shader = **`Skybox/Cubemap`**（Unity 内置，在 `Warpforge_unitybuiltinassets` 里）·
    /// cubemap `Skybox Clouds`（256² · BC6H · 6 面 9 级 mip，由 `工具/gen_skybox_cubemap.py` 导出到
    /// `Assets/WarpforgeArena1/skybox/`）· `_Tint = (0.5490196, 0.5061521, 0.4039216, 0.5)` ·
    /// `_Exposure = 0.67` · `_Rotation = 0`。
    /// ⚠️ **面序 = `CubemapFace` 枚举序**（`+X, -X, +Y, -Y, +Z, -Z`）—— 导出的文件名 `pX/mX/pY/mY/pZ/mZ`
    /// 就是按这个顺序切的；**若画面里天空上下颠倒/左右镜像，第一个要查的就是这里**。</summary>
    public static void SetupSkybox()
    {
        const string dir = "Assets/WarpforgeArena1/skybox";
        const string cubePath = dir + "/Skybox Clouds.cubemap";
        const string matPath = dir + "/Skybox Clouds Dusk.mat";
        const int size = 256;

        var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(cubePath);
        if (cube == null)
        {
            var faces = new[] { "pX", "mX", "pY", "mY", "pZ", "mZ" };
            var order = new[] { CubemapFace.PositiveX, CubemapFace.NegativeX,
                                CubemapFace.PositiveY, CubemapFace.NegativeY,
                                CubemapFace.PositiveZ, CubemapFace.NegativeZ };
            cube = new Cubemap(size, TextureFormat.RGBA32, true);   // 原版有 9 级 mip ⇒ 生成 mip 链
            for (int i = 0; i < faces.Length; i++)
            {
                var p = $"{dir}/{faces[i]}.png";
                var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                if (ti != null && !ti.isReadable) { ti.isReadable = true; ti.SaveAndReimport(); }
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                if (t == null)
                {
                    Debug.LogError($"[Arena] 🔴 天空盒缺面 {p} —— 先跑 "
                                 + "`工具/gen_skybox_cubemap.py`（它从原版 bundle 导出 6 面）");
                    return;
                }
                cube.SetPixels(t.GetPixels(), order[i]);
            }
            cube.Apply(true);
            AssetDatabase.CreateAsset(cube, cubePath);
        }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            var sh = Shader.Find("Skybox/Cubemap");
            if (sh == null) { Debug.LogError("[Arena] 🔴 找不到内置 shader `Skybox/Cubemap`"); return; }
            mat = new Material(sh) { name = "Skybox Clouds Dusk" };
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.SetTexture("_Tex", cube);
        mat.SetColor("_Tint", new Color(0.5490196f, 0.5061521f, 0.4039216f, 0.5f));
        mat.SetFloat("_Exposure", 0.67f);
        mat.SetFloat("_Rotation", 0f);
        RenderSettings.skybox = mat;
    }

    /// <summary>灯光 + 环境光 —— **独立场景与战斗场景共用**（判据只留一处）。
    /// 🔴 2026-09-20 从 `BuildSceneTail` 挪进来：原来这两段只在建**独立战场场景**时执行，
    /// 而战斗场景走的是 `BuildContent`（`BattleScene.cs:3872`）⇒ **灯根本没跟过来**
    /// （`BattleScene.cs` 全篇没有 `AddComponent&lt;Light&gt;`、也没有 `RenderSettings`）。
    /// 值全来自清单（= 原版实读），不在这里写死任何数值。</summary>
    public static void ApplyLightAndAmbient(Transform parent, Manifest mf)
    {
        // ---------------- 灯光 ----------------
        if (mf.light != null)
        {
            var lGo = new GameObject(string.IsNullOrEmpty(mf.light.name) ? "Directional Light" : mf.light.name);
            if (parent != null) lGo.transform.SetParent(parent, false);
            var li = lGo.AddComponent<Light>();
            li.type      = LightType.Directional;
            li.color     = ToColor(mf.light.color);
            li.intensity = mf.light.intensity > 0 ? mf.light.intensity : 1f;
            // 阴影：原版 13 场 soft/hard/none 各有，强度四档（0.591 / 0.65 / 0.725 / 1.0）
            li.shadows = (LightShadows)Mathf.Clamp(mf.light.shadowType, 0, 2);
            if (mf.light.shadowStrength > 0f) li.shadowStrength = mf.light.shadowStrength;
            else Debug.LogWarning("[Arena] 清单里 light.shadowStrength <= 0（字段缺失？）⇒ 保留 Unity 默认 1.0");
            if (mf.light.shadowBias > 0f) li.shadowBias = mf.light.shadowBias;
            ApplyTransform(lGo.transform, mf.light.pos, mf.light.rot, new[] { 1f, 1f, 1f });
        }
        else Debug.LogWarning("[Arena] 🔴 清单里没有灯 —— 战场只剩环境光照亮（原版每场都有 1 盏平行光）");

        // ---------------- 环境光 ----------------
        if (mf.ambient != null)
        {
            var mode = (AmbientMode)mf.ambient.mode;
            RenderSettings.ambientMode = mode;
            if (mode != AmbientMode.Flat)
                Debug.LogWarning($"[Arena] 🔴 ambientMode = {mode}（不是 Flat）—— 只对 Flat 做过实况核对，"
                               + "这种模式下 `ambientLight` 未必是生效的那个量，**别当已定案**");

            // 🔴 颜色取 `defaultEnv.ambientColor`（**运行时真值**），不是场景里的 `m_AmbientSkyColor`。
            //    原版 `ScenarioEnvironmentConditionsManager.Awake()` →
            //    `ApplyEnvironment(defaultEnvironment, instant:true)` → `ApplyAmbientColor` →
            //    `RenderSettings.ambientLight = <SO>.ambientColor`。
            //    实况探针（2026-09-20，battlearena1 + battlearena3）实测：
            //      arena1 得 (1,1,1,α0)（SO α=0；场景 α=1）· arena3 得 (0.80660,0.95225,1)
            //      而 arena3 的场景值其实是 (0.6840,0.9229,1) ⇒ **运行时跟 SO，不跟场景**。
            if (mf.defaultEnv != null && mf.defaultEnv.ambientColor != null && mf.defaultEnv.ambientColor.Length >= 3)
            {
                RenderSettings.ambientLight = ToColor(mf.defaultEnv.ambientColor);
            }
            else
            {
                Debug.LogWarning("[Arena] 🔴 清单里没有 defaultEnv（原版运行时环境光的真源）"
                               + " ⇒ 退回场景里的 m_AmbientSkyColor —— **这个值原版运行时不用**，重跑生成器");
                RenderSettings.ambientLight = ToColor(mf.ambient.sky);
            }

            RenderSettings.ambientIntensity = mf.ambient.intensity > 0 ? mf.ambient.intensity : 0.41f;

            // 雾：原版 `ApplyFog` 的判据是 `0 < fogDensity` 才开；13 场的默认环境 fogDensity 都是 0
            float fd = mf.defaultEnv != null ? mf.defaultEnv.fogDensity : mf.ambient.fogDensity;
            RenderSettings.fog = fd > 0f;
            if (RenderSettings.fog)
            {
                RenderSettings.fogColor   = ToColor(mf.defaultEnv != null ? mf.defaultEnv.fogColor : mf.ambient.fogColor);
                RenderSettings.fogDensity = fd;
                RenderSettings.fogMode    = FogMode.Exponential;
            }
        }
        else Debug.LogWarning("[Arena] 🔴 清单里没有 ambient —— 环境光用 Unity 默认值");
    }

    /// <summary>战场相机的**光学部分** —— 独立战场场景与战斗场景**共用这一份**（判据只留一处）。
    /// 逐值照原版 `Camera_1461.json`：focal 28 / sensor 41.5×24 / gateFit Horizontal。
    /// 🔴 **必须开 `usePhysicalProperties`** —— `lensShift` 只在物理相机模式下生效，
    /// 不开的话它被 Unity **静默忽略**（战斗场景踩过一次：整个 3D 战场比原版低 ≈150 px；
    /// 独立场景又踩一次：预览比原版亮 42%、多出半屏天空）。
    /// `lensShiftY` 由调用方给 —— 战斗侧传 `BattleScene.BoardLensShiftY()`（原版运行时算法现算）。</summary>
    public static void ConfigureBoardCamera(Camera c, CameraData d, float lensShiftY)
    {
        c.orthographic        = false;
        c.usePhysicalProperties = true;
        c.focalLength         = 28f;
        c.sensorSize          = new Vector2(41.5f, 24f);
        c.gateFit             = Camera.GateFitMode.Horizontal;
        // ⚠️ 开了物理相机之后 `fieldOfView` 会被 Unity 忽略（视角由 focal/sensor/gateFit 决定），
        //    这里仍然写上是为了让 Inspector 里能看见原版那个数（46.397182）
        c.fieldOfView   = (d != null && d.fov  > 0f) ? d.fov  : 46.397182f;
        c.nearClipPlane = (d != null && d.near > 0f) ? d.near : 0.3f;
        c.farClipPlane  = (d != null && d.far  > 0f) ? d.far  : 300f;
        c.lensShift     = new Vector2(0f, lensShiftY);

        // 🆕 2026-09-20：**开后处理** —— 原版那台 `BoardCamera` 的
        // `UniversalAdditionalCameraData.m_RenderPostProcessing = 1`（而 `UI Camera` 是 Overlay + 关着）。
        // 不开这个，战场那个全局 Volume（Bloom 1.15/5.0/6 + Vignette 0.297）**一点作用都没有** ——
        // 又一次「值都抄对了、就是没生效」（和 `lensShift` 没开物理相机同一个形状）。
        // 🔴 2026-09-20 二踩：**独立战场场景那台相机漏了这一段** ⇒ 预览图四角比原版亮 ≈15%
        //    （vignette 没生效），拿它当验收靶子会得出「我们偏亮」的错结论。
        //    ⇒ 挪进这个**共用**方法里，两台相机一视同仁。
        var ad = c.GetUniversalAdditionalCameraData();
        ad.renderPostProcessing = true;
        ad.renderShadows       = true;      // 原版 `m_RenderShadows = 1`
        ad.volumeLayerMask     = 1 << 0;    // 原版 `m_VolumeLayerMask = {"m_Bits": 1}`（Default 层）
    }

    /// <summary>后处理（2026-09-20 新增）。原版战场在 **`BoardCamera` 这个 GameObject 上挂了一个全局
    /// `Volume`**（`m_IsGlobal:1` / priority 0 / weight 1），profile 名 `&lt;场景&gt; PostProcessing`，
    /// 里面 3 个 override：
    ///   · **Bloom**      threshold 1.15 · intensity 5.0 · scatter 1.0 · skipIterations 6
    ///   · **Vignette**   color 黑 · center (0.5,0.5) · intensity 0.297
    ///   · **ColorLookup** LUT `LUT Normal` —— ⚠️ **实测严格 identity**（256×16 条带，V 轴翻转后
    ///     与恒等映射 4096 个采样点偏差 **0/255**）⇒ **不产生任何分级效果，故不接**。
    /// 原版那台 `BoardCamera` 的 `UniversalAdditionalCameraData.m_RenderPostProcessing = 1`（开着），
    /// 而 `UI Camera` 是 **Overlay 类型且后处理关**（`m_CameraType=1` / `m_RenderPostProcessing=0`）。
    /// ⚠️ **已知差异（没接）**：原版是「base + overlay 相机栈」⇒ 后处理作用在**合成后**的整帧（UI 也被
    ///    压暗）；我们是两台各自独立的 base 相机 ⇒ 后处理只作用在 3D 那层，**HUD 不带 bloom/vignette**。
    ///    证据与取舍见 `资料/普查产出_0920/场景后处理_原版规格.md`。</summary>
    public static void ApplyPostFx(Transform parent, Manifest mf)
    {
        if (mf.postFx == null || mf.postFx.components == null || mf.postFx.components.Length == 0)
        {
            Debug.LogWarning("[Arena] 🔴 清单里没有 postFx —— 战场不会有原版的 Bloom/Vignette");
            return;
        }

        string dir  = ProfileDir(mf.scene);
        string path = $"{dir}/{(string.IsNullOrEmpty(mf.scene) ? "arena" : mf.scene)}_PostFx.asset";
        Directory.CreateDirectory(dir);
        AssetDatabase.DeleteAsset(path);                       // 幂等：每次重建，避免残留旧组件

        var prof = ScriptableObject.CreateInstance<VolumeProfile>();
        prof.name = string.IsNullOrEmpty(mf.postFx.profile) ? "ArenaPostFx" : mf.postFx.profile;
        AssetDatabase.CreateAsset(prof, path);

        foreach (var c in mf.postFx.components)
        {
            if (c.type == "Bloom")
            {
                var b = prof.Add<Bloom>(true);
                b.active = true;
                if (c.threshold > 0f) b.threshold.overrideState = true;
                b.threshold.value     = c.threshold;
                b.intensity.value     = c.intensity;
                b.scatter.value       = c.scatter;
                // ⚠️ **用 `maxIterations` 而不是 `skipIterations`**：URP 的 bloom pass 只读前者
                //    （`PostProcessPass.cs` 的 `m_Bloom.maxIterations.value`），后者在 URP 里是
                //    `[Obsolete(...)]`、**不生效**。原版 Bloom 的 `maxIterations` 是 6（没打勾，
                //    等于 URP 默认 6），`skipIterations` 打勾=6 但是死值 —— 抄错字段就白抄。
                if (c.maxIterations > 0f)
                    b.maxIterations.value = Mathf.Clamp(Mathf.RoundToInt(c.maxIterations), 2, 8);
                b.highQualityFiltering.overrideState = false;
                AssetDatabase.AddObjectToAsset(b, prof);
            }
            else if (c.type == "Vignette")
            {
                var v = prof.Add<Vignette>(true);
                v.active = true;
                v.color.value     = ToColor(c.color);
                v.center.value    = (c.center != null && c.center.Length >= 2)
                                    ? new Vector2(c.center[0], c.center[1]) : new Vector2(0.5f, 0.5f);
                v.intensity.value = c.intensity;
                v.smoothness.overrideState = false;             // 原版没打勾 ⇒ 用 URP 默认
                v.rounded.overrideState    = false;
                AssetDatabase.AddObjectToAsset(v, prof);
            }
            else
            {
                // ColorLookup 是 identity（见方法头）⇒ 不接；其余未知类型如实报出来，不静默
                if (c.type != "ColorLookup")
                    Debug.LogWarning($"[Arena] 清单里有个我们不认识的后处理组件 `{c.type}` —— 没接");
            }
        }
        AssetDatabase.SaveAssets();

        var go = new GameObject("PostFx（原版挂在 BoardCamera 上的全局 Volume）");
        if (parent != null) go.transform.SetParent(parent, false);
        var vol = go.AddComponent<Volume>();
        vol.isGlobal     = mf.postFx.isGlobal;
        vol.priority     = mf.postFx.priority;
        vol.weight       = mf.postFx.weight;
        vol.sharedProfile = prof;
    }

    /// <summary>独立场景模式的收尾：相机 / 存盘。</summary>
    static void BuildSceneTail(Manifest mf, Scene scene)
    {
        // ---- 相机 ----
        // 🔴 2026-09-20：这台原来是**旧的**（只设 fov/near/far/lensShift，**没开物理相机**）
        //    ⇒ `lensShift` 被 Unity 静默忽略 ⇒ 预览图取景偏上、比原版**亮 42%**（多出半屏天空）。
        //    现在与战斗场景那台**共用同一套判据**：`BattleScene.BuildBoardCamera` 的物理相机参数 +
        //    `BattleScene.BoardLensShiftY()`（原版 `CameraVerticalFramer.CalculateFraming` 的现算值）。
        if (mf.camera != null)
        {
            var camGo = new GameObject(string.IsNullOrEmpty(mf.camera.name) ? "BoardCamera" : mf.camera.name);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            ConfigureBoardCamera(cam, mf.camera, BattleScene.BoardLensShiftY());
            ApplyTransform(camGo.transform, mf.camera.pos, mf.camera.rot, new float[] { 1, 1, 1 });
            camGo.AddComponent<AudioListener>();
        }

        // ---- 保存场景 ----
        Directory.CreateDirectory(ScenesDir);
        EditorSceneManager.SaveScene(scene, ScenePath(mf.scene));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Arena] 场景已建（{mf.scene}）：相机 {(mf.camera != null)}、灯光 {(mf.light != null)}、环境光 {(mf.ambient != null)}");
        Debug.Log($"[Arena] 场景已保存：{ScenePath(mf.scene)}");
    }

    /// <summary>读战场清单（`arenas/&lt;场景&gt;/&lt;场景&gt;_manifest.json`）。失败时返回 null 并报错。</summary>
    public static Manifest LoadManifest(string sceneName)
    {
        var path = ManifestPath(sceneName);
        if (!File.Exists(path))
        {
            Debug.LogError($"[Arena] 清单不存在：{path}（先跑 gen_unity_arena_manifest.py --arena {sceneName}）");
            return null;
        }
        var mf = JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
        if (mf == null) Debug.LogError($"[Arena] 清单解析失败：{path}");
        return mf;
    }

    /// <summary>同一个场景里，**粒子按贴图名共用一份材质**（见 `GetOrCreateParticleMaterial`）。</summary>
    static readonly Dictionary<string, Material> _psMatCache = new Dictionary<string, Material>();

    /// <summary>把材质存成资产 —— **路径确定（`Materials/<名字>.mat`）、已有就原地覆盖**。
    ///
    /// 🔴 2026-09-20 修（这条是个**静默**的坑）：原来走 `AssetDatabase.GenerateUniqueAssetPath`
    ///    + `ClearMaterialDir` 每次重建先删光 ⇒ **每次重跑都会换一批 guid**，
    ///    而**早先存过盘的场景还指着旧 guid** ⇒ 那些渲染器的 `sharedMaterial` 变成 null。
    ///    表现：`BattleScene.Run` 报「**34 个粒子的材质没贴图**」—— 看着像贴图丢了，
    ///    其实是**材质整个没了**（`Battle.unity` 存的还是上一次构建的 guid）。
    ///    改成「确定路径 + `CopySerialized` 原地覆盖」之后 guid 稳定，重建不再打翻别的场景。</summary>
    static Material SaveOrReuse(string sceneName, Material fresh)
    {
        var path = $"{MatDir(sceneName)}/{fresh.name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            // 原地覆盖：**保住 guid**（这就是整件事的关键），参数跟着最新的走
            EditorUtility.CopySerialized(fresh, existing);
            UnityEngine.Object.DestroyImmediate(fresh);
            EditorUtility.SetDirty(existing);
            return existing;
        }
        AssetDatabase.CreateAsset(fresh, path);
        return fresh;
    }

    /// <summary>清掉**历史重复项**（`GenerateUniqueAssetPath` 时代留下的 `… 1.mat` / `… 2.mat`）。
    ///
    /// ⚠️ **不能再「全删」** —— 删了就换 guid、早先存过盘的场景立刻悬空（见 `SaveOrReuse` 那条）。
    /// 只删名字末尾带 ` &lt;数字&gt;` 的那种（那正是 `GenerateUniqueAssetPath` 的产物），
    /// 确定路径的那批**留着复用**。</summary>
    static void ClearMaterialDir(string sceneName)
    {
        var matDir = MatDir(sceneName);
        Directory.CreateDirectory(matDir);
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { matDir }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (!p.StartsWith(matDir)) continue;
            var stem = Path.GetFileNameWithoutExtension(p);
            int sp = stem.LastIndexOf(' ');
            if (sp > 0 && int.TryParse(stem.Substring(sp + 1), out _)) { AssetDatabase.DeleteAsset(p); n++; }
        }
        if (n > 0) Debug.Log($"[Arena] 清掉历史重复材质 {n} 份（`… 1.mat` 那一族）");
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
    static Texture GetTexture(string sceneName, string texFile)
    {
        if (string.IsNullOrEmpty(texFile)) return null;
        var path = $"{TexDir(sceneName)}/{texFile}";
        var t = AssetDatabase.LoadAssetAtPath<Texture>(path);
        if (t == null) Debug.LogWarning($"[Arena] 找不到贴图：{path}");
        return t;
    }

    /// <summary>
    /// 源图是否带 alpha 通道。
    /// 背景板那几张（Battle Arena 1 Background / Back Background）的透明区 RGB 是白色，
    /// 若按不透明渲染就会糊成大白板 —— 必须靠这个判定走 alpha 混合。
    /// </summary>
    static bool TextureHasAlpha(string sceneName, string texFile)
    {
        if (string.IsNullOrEmpty(texFile)) return false;
        var path = $"{TexDir(sceneName)}/{texFile}";
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        return ti != null && ti.DoesSourceTextureHaveAlpha();
    }

    /// <summary>为本体网格建 URP/Unlit 材质（原版战场是烘焙贴图 + 无光照，Unlit 最接近）。
    /// 🔴 2026-09-20：拆出下面那层**逐材质**的实现 —— 一个网格可能有**多个子网格、每个一个材质**
    /// （见 `BuildContent` 里多子网格那一段）。</summary>
    /// <summary>诊断：查**原版网格 shader 为什么渲成洋红**（2026-09-20 加）。
    /// 洋红 = Unity 的 shader 报错色 ⇒ 先看 `isSupported` 与 pass 数，再决定这条路能不能走。
    /// 用法：`... -executeMethod ArenaBuilder.ProbeArenaShaders`</summary>
    public static void ProbeArenaShaders()
    {
        string[] names = {
            "Everguild/UnlitAmbient", "Everguild/Unlit Wind", "Everguild/Misc/Unlit shadows receiver",
            "Everguild/FX/Unlit UV scroll", "Everguild/UnlitAmbient Emissive Flickker",
            "Everguild/FX/Floor Planar Reflections Grainny",
        };
        foreach (var n in names)
        {
            Shader sh = null;
            bool got = WarpforgeVFX.WarpforgeShaderLoader.TryGetShader(n, out sh);
            if (!got || sh == null) { Debug.Log($"[PS] {n,-52} ❌ 取不到"); continue; }
            Debug.Log($"[PS] {n,-52} sh='{sh.name}' isSupported={sh.isSupported} passes={sh.passCount}"
                    + $" kw={(sh.keywordSpace != null && sh.keywordSpace.keywordNames != null ? sh.keywordSpace.keywordNames.Length : -1)}"
                    + $" hideFlags={sh.hideFlags}");
        }
    }

    /// <summary>⚠️ 2026-09-20：**「用原版网格 shader」这条路当前是关的** —— 实测整屏洋红
    /// （原版这批网格 shader 在我们工程里跑不起来，详见 `GetOrCreateMaterial` 里的长注释）。
    /// 清单里的 `shader` 字段**已经带出来了**，改这个常量就能试。</summary>
    /// <summary>⚠️ 2026-09-20：**「用原版网格 shader」这条路当前默认关** —— 实测整屏洋红
    /// （Unity 的 shader 报错色），虽然探针说 6 个 shader 全都 `isSupported=True`。
    /// 用环境变量 `WF_ORIGSHADER=1` 打开（A/B 用，不必改代码）。
    /// 清单里的 `shader` 字段**已经带出来了**，开关一翻就能试。</summary>
    static bool ArenaUseOriginalShaders
        => System.Environment.GetEnvironmentVariable("WF_ORIGSHADER") == "1";
    /// <summary>是否给原版 shader 开 `_APPLYAMBIENTCOLOR`（原版材质绝大多数带着它）。
    /// ⚠️ 2026-09-20：与 `ArenaUseOriginalShaders` **分开**，这样才能用 A/B 分清
    /// 「洋红是原版 shader 本身跑不起来」还是「这个 keyword 引起的」。</summary>
    const bool ArenaApplyAmbientColor = false;

    static Material GetOrCreateMaterial(string sceneName, Dictionary<string, Material> cache, MeshEntry e)
        => GetOrCreateMaterial(sceneName, cache, e.tex, e.texFile, e.baseColor, e.emission,
                               e.transparent, e.forceBlend, e.alphaClip, e.cull, e.blend,
                               e.srcBlend, e.dstBlend, e.go, e.blendAuthoritative, e.shader);

    static Material GetOrCreateMaterial(string sceneName, Dictionary<string, Material> cache,
                                        SubMatEntry s, string goName)
        => GetOrCreateMaterial(sceneName, cache, s.tex, s.texFile, s.baseColor, s.emission,
                               s.transparent, s.forceBlend, s.alphaClip, s.cull, 0,
                               s.srcBlend, s.dstBlend, goName, s.blendAuthoritative, s.shader);

    static Material GetOrCreateMaterial(string sceneName, Dictionary<string, Material> cache,
                                        string tex, string texFile, float[] baseColor, float[] emission,
                                        bool transparent, bool forceBlend, bool alphaClip,
                                        int cull, int blend, int srcBlend, int dstBlend, string goName,
                                        bool blendAuthoritative = false, string origShader = null)
    {
        var key = $"{texFile}|{transparent}|{blend}|{cull}|{blendAuthoritative}|{origShader}";
        if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

        // ⚠️ 2026-09-20：**「用原版 shader」这条路当前是关的** —— 实测**整屏洋红**（Unity 的
        //    shader 报错色）：原版这批**网格** shader（`Everguild/UnlitAmbient` 等）在我们工程里
        //    **跑不起来**。⚠️ **别拿特效线的结论类推** —— 那边验过的是**粒子** shader
        //    （`BuiltinShaderProbe` 9/0），跟这批不是一回事。
        //    所以仍然走 `URP/Unlit`，代价是**暗部偏亮 1.43~1.89×**（原版 shader 那层环境光压暗丢了，
        //    实测六个区：暗部偏亮多、亮部只差 1.05×）—— **这是已知差异，如实记着**。
        //    要再试：先查这批 shader 的编译错误（`Shader.isSupported` / 编辑器里的报错），
        //    **别直接铺开到 13 场**。清单里的 `shader` 字段已经带出来了，开关一翻就能试。
        Shader shader = null;
        bool usingOriginal = false;
        if (ArenaUseOriginalShaders && !string.IsNullOrEmpty(origShader))
            WarpforgeVFX.WarpforgeShaderLoader.TryGetShader(origShader, out shader);
        usingOriginal = shader != null;
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
        }
        var mat = new Material(shader) { name = Sanitize(string.IsNullOrEmpty(tex) ? goName : tex) };

        var tx = GetTexture(sceneName, texFile);
        if (tx != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tx);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tx);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", ToColor(baseColor));
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", ToColor(baseColor));

        // 描边剔除：Unity cull 0=Off 1=Front 2=Back
        if (cull == 0) mat.SetFloat("_Cull", (float)CullMode.Off);
        else if (cull == 1) mat.SetFloat("_Cull", (float)CullMode.Front);
        else mat.SetFloat("_Cull", (float)CullMode.Back);

        // 自发光（原版地面/围栏 emission=0.19 就是靠这个提亮）
        var em = ToColor(emission);
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
        // 🔴 4. **但 shader 属性表说了算时（2026-09-20），第 3 条与 `forceBlend` 都要让位**：
        //     属性表里没声明 `_SrcBlend` 的 shader，pass 的混合是**硬编码**的，材质上那两个值是
        //     内置 Standard 的残留值。此时再拿「整图有多少透明像素」兜底，会把一块**真不透明**
        //     的地板拉成透明 —— 实例 = sororitas 的 `Floor`（图集 91.3% 全透明，而原版 pass
        //     硬编码 One/Zero + 写深度 ⇒ 不透明）。详见 `资料/普查产出_0920/场景光照与后处理_原版规格.md` §13.1。
        var wantTransparent = blendAuthoritative
            ? transparent
            : (transparent || forceBlend || alphaClip || TextureHasAlpha(sceneName, texFile));
        if (usingOriginal)
        {
            // 用**原版 shader** 时按**它自己的属性名**设混合（`HasProperty` 会滤掉这个 shader 没有的）。
            // ⚠️ **不能调 `MakeTransparent`** —— 那个函数是按 URP/Unlit 的属性写的。
            if (mat.HasProperty("_Surface"))  mat.SetFloat("_Surface", wantTransparent ? 1f : 0f);
            if (mat.HasProperty("_Blend"))    mat.SetFloat("_Blend", 0f);          // 0 = Alpha
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite"))   mat.SetFloat("_ZWrite", wantTransparent ? 0f : 1f);
            // `_APPLYAMBIENTCOLOR` 是那层「环境光压暗」的开关 —— 原版材质实读里绝大多数都带着它
            // （`Sororitas Atlas 2` 家族三个材质全有）。
            // ⚠️ 2026-09-20：**先不开** —— 第一次试开 + 用原版 shader 时整屏洋红；
            //    关掉它再 A/B 一次，才能分清洋红是「原版 shader 本身跑不起来」还是「这个 keyword 引起的」。
            if (ArenaApplyAmbientColor) mat.EnableKeyword("_APPLYAMBIENTCOLOR");
            else                        mat.DisableKeyword("_APPLYAMBIENTCOLOR");
            if (wantTransparent) { mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.EnableKeyword("_ALPHABLEND_ON"); }
            else                 { mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.DisableKeyword("_ALPHABLEND_ON"); }
            mat.renderQueue = wantTransparent ? (int)RenderQueue.Transparent : (int)RenderQueue.Geometry;
        }
        else if (wantTransparent)
        {
            MakeTransparent(mat);
        }

        cache[key] = mat;
        return SaveOrReuse(sceneName, mat);
    }

    /// <summary>粒子材质：透明 Unlit，用清单里指定的贴图</summary>
    static Material GetOrCreateParticleMaterial(string sceneName, string texName)
    {
        // 🔴 同一个贴图**只建一份材质**（原来每颗粒子各建一份 ⇒ 14 个用 `Glow.png` 的粒子
        //    就产出 14 份 `PS_Glow.png*.mat`，Materials/ 里堆了 204 个）。
        string key = sceneName + "|" + texName;
        Material cached;
        if (_psMatCache.TryGetValue(key, out cached) && cached != null) return cached;

        // ⚠️ 必须用 **Particles/Unlit**，不能用 `URP/Unlit`：后者**不乘粒子顶点色**，
        //    于是 startColor 里的灰度/透明度全丢，烟和蒸汽渲出来是一坨白方块（实拍踩过）。
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        var mat = new Material(shader) { name = "PS_" + Sanitize(texName) };
        var tex = GetTexture(sceneName, texName);
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }
        MakeTransparent(mat);
        var reused = SaveOrReuse(sceneName, mat);
        _psMatCache[key] = reused;
        return reused;
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
