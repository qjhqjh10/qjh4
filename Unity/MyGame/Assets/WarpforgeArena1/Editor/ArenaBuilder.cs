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

    /// <summary>本局用哪个战场 —— **原版判据 = `BattleArenaByArmySO.GetBattleArena(督军阵营)`**
    /// （`SearchOpponentManager__StartBattle.c:57` → `:61 EverguildSceneManager.LoadScene(名字)`）。
    ///
    /// 🔴 **表本体、14 条内容、复现办法、调用链，判据只写一处 ⇒ `ArenaByArmy`（运行时）**
    /// 与 `资料/普查产出_0920/场景光照与后处理_原版规格.md` §六。
    /// ⚠️ 这里**只是转发**：表放运行时是因为**使用点在开战时**（战斗入口那几扇窗），
    /// 而本类是 Editor 类，运行时引用不到。
    ///
    /// ⚠️ **原版存的是带空格的 Addressables 场景名**（`Battle Arena Aeldari`），不是我们的 `battlearenaX` 键。
    /// ⚠️ **兜底 = `Battle Arena 1`**（原版 `defaultBattleArena`），不是「随便挑一个」。
    ///
    /// ✅ **2026-09-25：「一局一战场」已接上** —— 方案定为**一场一份 `Battle_<场>.unity`**（**不是**运行时实例化；
    /// 三条依据见 `资料/阶段二_战斗入口_原版规格.md` §7·1），13 份已建并核验；
    /// 运行时的选场与回落判据在 `ArenaByArmy.BattleSceneNameFor`。**这里只是转发查表，不重复那套判据。**</summary>
    public static string ArenaForArmy(string army) => ArenaByArmy.SceneFor(army);

    /// <summary>⚠️ **只是兜底值**，不是「本局该用哪个战场」—— 那个判据在 `ArenaByArmy.SceneFor`。
    /// 保留它给「不涉及某一局」的编辑器入口用（`Build` / `AbRender` / 探针）。**别在别处再写死。**</summary>
    public const string DefaultArena = "battlearena1";

    public static string ArenaDir(string scene)   => RootDir + "/arenas/" + scene;
    public static string ManifestPath(string s)   => ArenaDir(s) + "/" + s + "_manifest.json";
    public static string ModelDir(string scene)   => ArenaDir(scene) + "/Models";
    public static string TexDir(string scene)     => ArenaDir(scene) + "/Textures";
    public static string MatDir(string scene)     => ArenaDir(scene) + "/Materials";
    public static string ProfileDir(string scene) => ArenaDir(scene) + "/Profiles";
    public static string ScenePath(string scene)  => ScenesDir + "/" + scene + ".unity";

    /// <summary>缓存：`(场, OBJ 文件名)` → 网格。`AssetDatabase` 本身也有缓存，这里只是为了少拼字符串。</summary>
    static readonly System.Collections.Generic.Dictionary<string, Mesh> _psMeshCache
        = new System.Collections.Generic.Dictionary<string, Mesh>();

    /// <summary>🔴 2026-09-25：`renderMode = 4 (Mesh)` 的粒子要用的网格 —— 从 `Models/&lt;objFile&gt;` 里取。
    /// 取不到就返回 **null**（调用方退回 Billboard **并出声** —— 不许静默）。</summary>
    static Mesh ParticleMesh(string objFile, string scene)
    {
        if (string.IsNullOrEmpty(objFile)) return null;
        var key = scene + "/" + objFile;
        if (_psMeshCache.TryGetValue(key, out var cached)) return cached;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir(scene)}/{objFile}");
        var src = prefab != null ? prefab.GetComponentInChildren<MeshFilter>() : null;
        var m = src != null ? src.sharedMesh : null;
        _psMeshCache[key] = m;
        return m;
    }

    // ---------- JSON 结构（与 Python 侧 schema 严格对应）----------
    [System.Serializable] public class CameraData
    {
        public string name; public float[] pos; public float[] rot;
        public float fov; public float near; public float far; public float lensShiftY;
        // 🆕 **2026-09-27：逐场相机的光学参数（旁挂 `工具/gen_arena_camera.py` 灌进来）。**
        //    🔴 **必须逐场** —— 原版 13 台 `BoardCamera` 里 `m_SensorSize.x` **只有两个取值**：
        //    `emperorschildren` / `genestealers` / `spacewolves` = **37.2**，其余十台 = **41.5**。
        //    默认值 41.5/24 = 那十场的值（**对那三场是错的**，见 `ApplyCameraSidecar`）。
        public float sensorSizeX = 41.5f; public float sensorSizeY = 24f;
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
        // 🔴 **2026-09-30 新增：雾的「形状」三项**（`m_FogMode` / 线性雾起止）。
        //    为什么非带不可：`ScenarioEnvironmentConditionSO` 的字段表里**只有 fogColor / fogDensity**，
        //    而 `ScenarioEnvironmentConditionSO__ToggleFog.c` 全文只是 `RenderSettings.set_fog(fogDensity > 0)`
        //    ⇒ **雾的形状只能来自场景**，而它**逐场不同**：`fogMode` 11 场 = 2 (Exponential) ·
        //    2 场 = 1 (Linear：aeldari / blacklegion)；线性范围 5 种（7.47/8.75 ×8 · 4.4/27.6 ·
        //    0/155.8 · 65.3/181.5 ×2 · 51.6/132.3）。
        //    判据全文 → `资料/普查产出_0930/§28逐场核_第一轮.md` §三。
        public int fogMode; public float linearFogStart; public float linearFogEnd;
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
    // ⚠️ `MatProp` **不在这里定义** —— 它得能被**运行时**序列化，所以在
    //    `WarpforgeVFX/Runtime/ArenaOriginalMaterial.cs` 里（本类是 Editor 程序集，运行时够不着）。
    [System.Serializable] public class SubMatEntry
    {
        public string tex; public string texFile;
        public float[] baseColor; public float[] emission;
        public int cull; public bool transparent;
        public int srcBlend; public int dstBlend;
        public bool alphaClip; public bool forceBlend;
        /// <summary>🆕 2026-09-21：**整张属性表的原样副本**（`[{k,t,f,c}]`）——
        /// 「运行时用原版 shader 重建材质」时按原版把参数灌回去（`ApplyProps`）。
        /// 为什么非它不可：`Unlit Wind` 的三个 `Vector1_&lt;guid&gt;`、`Unlit UV scroll` 的两组滚动向量、
        /// `_ClipThreshold` 这些**都不在具名字段里**（实测只搬贴图+颜色时 arena1 会多出一片白块、风也不动）。</summary>
        public WarpforgeVFX.MatProp[] props;
        /// <summary>2026-09-20：这个材质的混合**由 shader 属性表决定** —— 属性表里没声明
        /// `_SrcBlend` ⇒ 材质上那两个值是内置 Standard shader 的**残留值**，真值在 pass 的
        /// `rtBlend0`（硬编码）。为真时下方那两条「看贴图整图统计」的兜底
        /// （`forceBlend` / `TextureHasAlpha`）**一律让位** —— 否则会把一块真不透明的地板
        /// 又拉回透明（实例 = sororitas 的 `Floor`，见 `普查产出_0920/…原版规格.md` §13.1）。</summary>
        public bool blendAuthoritative;
        /// <summary>🆕 2026-09-22 晚：原版材质写死的 `m_CustomRenderQueue`（`-1` = 没写，用 shader 的 tag）。
        /// 透明物体的**绘制顺序**由它决定 —— 见 `ArenaOriginalMaterial.queue` 的说明与
        /// `数据/游戏数据/mat_renderqueue.tsv`（arena1 那族是 2450 / 3000 / **3002**）。</summary>
        public int queue = -1;
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
        /// <summary>🆕 2026-09-30（①-l）：逐对象的**阴影标志**，由旁挂 `ApplyShadowSidecar` 灌进来
        /// （`<场>_shadowflags.json`，按**名字 + 世界位置**匹配 —— 与 `_negscale.json` 同一套 `SameObject`）。
        /// `hasShadow = false`（旁挂没覆盖到这个对象）= **退回旧行为**（`Off/false`，或诊断开关 `WF_SHADOWCAST`）。
        /// ⚠️ **必须靠 `hasShadow` 判**，不能拿 `shadowCast == 0` 当「没覆盖」——
        /// 原版真的有对象就是 `m_CastShadows = 0`（`blacklegion` 67 个里 52 开 / 15 关）。</summary>
        public bool hasShadow; public int shadowCast; public int shadowReceive;
        /// <summary>🆕 2026-09-21：**整张属性表的原样副本** —— 见 `SubMatEntry.props`（同一份判据）。</summary>
        public WarpforgeVFX.MatProp[] props;
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
        /// <summary>🔴 2026-09-25：**这份网格的顶点已经烘死在「世界坐标」里**（Unity 静态合批的产物）
        /// ⇒ 构建侧**不能再乘一遍对象的 transform**。
        /// 不判它的后果（实测 `battlearena2`）：31 个静态合批对象全被再平移 + 放大一次
        /// （pos 92 + scale 188 ⇒ 顶点推到 x≈16000）⇒ **整批飞出画面**，画面上只剩天空盒，
        /// 而这场又恰恰是最暗的那场 ⇒ 整图偏亮 **1.086**（全 13 场最差）。
        /// 判据：`Combined Mesh (root: scene) 2` 顶点 z∈[91.47,**168.30**]，
        /// 它只有两个用户 `Background`(z=**168.3**) / `Skyline`(z=95.9) —— 逐值吻合。
        /// ⚠️ 旧清单没有这个字段 ⇒ false ⇒ 老行为（其余 12 场就是这么走的，逐场不变）。</summary>
        public bool worldBaked;
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
        /// <summary>🔴 2026-09-25：**`renderMode = 4 (Mesh)` 时用的网格**（`Models/` 里的 OBJ 文件名）。
        /// 由 `工具/gen_arena_psmesh.py`（解 `m_Mesh` 的 PPtr → 网格名）+ 生成器（网格名 → OBJ）补上。
        /// 空 = 没有它的网格 info ⇒ 建场时**如实退回 Billboard**（见 `ParticleMesh` 的调用点）。
        /// ⚠️ 旧清单没有这个字段 ⇒ null ⇒ 老行为。</summary>
        public string mesh;
        public float duration; public bool looping; public bool prewarm;
        /// <summary>🔴 **判「清单里到底有没有这个模块」的唯一判据 = 这几个布尔量。**
        ///
        /// 踩过的大坑（2026-09-22，**静默、而且吃掉了全部 13 场**）：
        /// 生成器对「原版关着的模块」写的是 `"velocity": null`，而
        /// **`JsonUtility.FromJson` 会把 `null` 整棵子树物化** —— 实测 `velocity != null`、
        /// **连 `velocity.x != null` 都是 True**（`ArenaBuilder.ParticleSanity` 里的探针钉死的）。
        /// ⇒ **任何 null 判断都失效**：`BuildContent` 给 **34/34 颗**粒子都打开了
        /// `velocityOverLifetime` 与 `limitVelocityOverLifetime`，后者的 `limit` 落到兜底值 **1**
        /// ⇒ **把所有粒子速度钳到 1 单位/秒**。
        /// 症状：arena1 烟囱 `#22` 的粒子活了 5.7 秒却只离发射体 **0.83 单位**、`|v|≈1.13`
        /// （清单初速写的是 2~3；把它覆盖成 10 也没用 —— 被 limit 钳住了）。
        /// 铁证：建场日志原来打「velocity **34** · clampVelocity **34** · noise **34** · rotation **34**」
        /// —— 34/34 在原版不可能出现（2026-09-21 实测 leviathan 是 **26 / 10 / 6 / 7**）。
        /// ⚠️ 同一个坑在 `emissionRateCurve` 上早就踩过一次（判 `!= null` ⇒ 断言数出假绿），
        /// **这一族一律走这几个布尔量，别看 `!= null`。**
        /// 出处：`资料/战场13场_逐场对账_0920.md`。</summary>
        public bool hasVelocity, hasClampVelocity, hasNoise, hasRotation, hasSubEmitters;
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
        // 🆕 2026-09-21：原版关着的对象不要再建（`m_IsActive` 逐层与起来，见生成器 `active_in_hierarchy`）。
        //    实测受害：arena3 的 `TorchEffectNecron/Fire/Light` ×2（淡绿、原版 aih=False）⇒ 「arena3 绿光溢出」。
        public bool active = true;
        // 🆕 2026-09-21：**两个我们原来完全没建的模块** + 几条渲染器字段。
        //    判据与实测见 `资料/战场13场_逐场对账_0920.md`：
        //    `sizeOverLifetime` 缺 ⇒ 全程满尺寸（原版 `Gas` 前段只有 23%）；`colorOverLifetime` 缺 ⇒
        //    整条命按 startColor.a 实心播放（原版 0→1→…→0）；`emissionRateCurve` 缺 ⇒ 把曲线拍成峰值常数。
        public SizeOverLifetimeData sizeOverLifetime;
        public GradientData colorOverLifetime;   // 🆕 2026-09-21 下半场：改成**完整 RGBA**（原来只有 alpha）
        public CurveData emissionRateCurve;
        public CurveData gravityCurve;
        // 🆕 2026-09-21 下半场：**原来一个都没建的 VFX 模块**（判据：原版 leviathan 71 个对象里
        //    Velocity 26 · ClampVelocity 10 · SubEmitter 7 · Noise 6 · Rotation 7 个开着）
        public VelocityData velocity;
        public ClampVelocityData clampVelocity;
        public NoiseData noise;
        public RotationData rotationOverLifetime;
        public SubEmitterData[] subEmitters;
        public int scalingMode = 1; public int renderAlignment;
        public float sortingFudge; public float lengthScale = 2f;
        public float maxParticleSize = 0.5f; public float minParticleSize;
        public float[] matColor;                 // 粒子材质 `_BaseColor`（原版 SmokeySteam01 = 0.6038 灰）
        // 🔴 **2026-09-24 新增：原版粒子材质的【身份 + 渲染状态】。**
        //   原来只带了 `matColor` + 贴图名，shader / 关键字 / 整张属性表**全丢** ⇒ Unity 侧只能
        //   对所有粒子硬编一套（`URP/Particles/Unlit` + SrcAlpha/OneMinusSrcAlpha、无自发光）。
        //   **代价（arena1 实测）**：`SmokeySteam01` 开着 `_EMISSION`（`_EmissionColor=0.4811`）
        //   ⇒ 原版蒸汽每通道**加 0.4811**、是白的，我们渲成深灰（全图均值 89.5 vs 138.4）；
        //   `Embers 1` 是加性混合（`_DstBlend=1`），被我们渲成 alpha 混合。
        //   判据全文 → `资料/战场13场_逐场对账_0920.md` §一 ①-c · `资料/已知的坑.md`。
        /// <summary>原版材质名（如 `SmokeySteam01`）—— **材质的身份**，我们按它一份一份地建材质。</summary>
        public string matName;
        /// <summary>原版 shader 名。粒子这批实测**全是** `Universal Render Pipeline/Particles/Unlit`。</summary>
        public string matShader;
        /// <summary>原版材质的 `m_ValidKeywords`（`_EMISSION` / `_FLIPBOOKBLENDING_ON` / `_SOFTPARTICLES_ON` …）。</summary>
        public string[] matKeywords;
        /// <summary>原版材质的**整张属性表**（`_BaseColor` / `_EmissionColor` / `_SrcBlend` / `_DstBlend` …）。
        /// 由 `ArenaOriginalMaterial.ApplyProps` 灌（**`SetVector`**）。</summary>
        public WarpforgeVFX.MatProp[] matProps;
        /// <summary>原版材质的 `m_CustomRenderQueue`（`−1` = 用 shader 的 tag）。</summary>
        public int matQueue = -1;
        // 🔴 **2026-09-22 新增：一批「原版有、生成器从来没抽」的字段**（判据 = `工具/arena_particle_audit.py`，
        //    它把原版 JSON 逐字段摊开并标 `[已接]/[未接]`）。这一批是用户圈出来的
        //    「烟囱是一大团黑色实心球 / 地面火又小又暗」的直接嫌疑（正本 §一 第 1 条末 + 第 7 条）：
        //    · `simulationSpeed` —— 原版 **19/34 个对象 ≠ 1.0**（烟囱 `SmokeEffect` 是 **0.1**、
        //      `Bullets Controller` 是 4.41、`Dust Floor` 一族 0.5）。不接就一律按 1.0 播。
        //    · `startRotation` —— **弧度**。原版 `Steam`/`Dust Floor`/烟囱 `SmokeEffect` 是
        //      `TwoConstants(0, 2π)`（每个粒子随机朝向）+ `randomizeRotationDirection=0.5`。
        //      **不接 ⇒ 所有粒子同一朝向 ⇒ 一堆同样朝向的烟贴片叠起来就是一「坨」**，
        //      而不是原版那种散开的缕 —— 这条最像「实心球」的成因。
        //    · `ShapeModule` 的 `m_Position` / `m_Rotation` / `m_Scale` / `radiusThickness` ——
        //      `Embers` 一族是 `m_Rotation=(-90,0,0)`（**发射方向差 90°**）、`WildFire` 的
        //      `m_Position=(0,-0.53,0)` 且 `radiusThickness=0`、`RisingSteam` 的 z 缩放是 0.5。
        public float simulationSpeed = 1f;
        public CurveData startDelay;
        public CurveData startRotation;                    // 弧度
        public bool rotation3D;                            // 为真时用 startRotationX/Y/Z
        public CurveData startRotationX, startRotationY, startRotationZ;
        public float randomizeRotationDirection;
        public bool size3D;                                // 为真时 startSize 只给 X，Y/Z 另有值
        public CurveData startSizeY, startSizeZ;
        public float[] shapePos, shapeRot, shapeScale;     // ShapeModule 的 localspace 位置/欧拉角/缩放
        public float shapeRadiusThickness = 1f;
    }

    /// <summary>🆕 2026-09-21：`MinMaxCurve` 的**统一形状**（生成器 `mm()` 的产物）。
    /// `isConst` 为真时用 `c`/`cMin`，否则用 `mult`/`keys`（+ 可选的 `multMin`/`minKeys`）。
    /// ⚠️ 字段名要和清单 JSON **逐字一致**（`JsonUtility` 对不上的字段静默丢弃）。</summary>
    [System.Serializable] public class VelocityData
    {
        public bool inWorldSpace;
        public CurveData x, y, z, radial, speedModifier;
        public CurveData orbitalX, orbitalY, orbitalZ;
        public CurveData orbitalOffsetX, orbitalOffsetY, orbitalOffsetZ;
    }
    [System.Serializable] public class ClampVelocityData
    {
        public bool separateAxis, inWorldSpace, multiplyDragByParticleSize, multiplyDragByParticleVelocity;
        public float dampen = 1f;          // ⚠️ Unity API 里是 float
        public CurveData drag, x, y, z, magnitude;
    }
    [System.Serializable] public class NoiseData
    {
        public bool separateAxes, damping, remapEnabled;
        public float frequency = 2f;       // ⚠️ Unity API 里是 float
        public float octaveMultiplier, octaveScale;
        public int octaves, quality;
        public CurveData scrollSpeed, strength, strengthY, strengthZ, positionAmount, rotationAmount, sizeAmount;
    }
    [System.Serializable] public class RotationData
    {
        public bool separateAxes;
        public CurveData x, y, z;
    }
    [System.Serializable] public class SubEmitterData
    {
        public string target;      // 被引用那个 ParticleSystem 的 **GameObject 名字**（同一份清单里找）
        public int type;           // 0=Birth 1=Collision 2=Death 3=Trigger 4=Manual
        public float emitProbability = 1f;
    }
    [System.Serializable] public class GradientData
    {
        public ColorKey[] colors; public AlphaKey[] alphas;
    }
    [System.Serializable] public class ColorKey { public float t; public float r = 1f, g = 1f, b = 1f; }
    /// <summary>曲线上的一个键。⚠️ **必须是对象、不能是 `float[]`** ——
    /// 清单是 `JsonUtility.FromJson` 读的，**它不支持交错数组**（`float[][]`）：
    /// 写成 `[[t,v],…]` 整份清单会解析失败（`资料/战场13场_逐场对账_0920.md` 记过这一坑）。</summary>
    [System.Serializable] public class CurveKey { public float t; public float v; }
    [System.Serializable] public class AlphaKey { public float t; public float a; }
    /// <summary>清单里的 `sizeOverLifetime`：`{separateAxes, x, y, z}`（单轴态时 x=y=z）。
    /// ⚠️ **字段名必须和 JSON 一模一样** —— `JsonUtility` 对不上的字段**静默丢弃**，
    /// 写成 `CurveData` 会得到一份 `keys=null` 的空数据（曲线悄悄没了、不报错）。</summary>
    [System.Serializable] public class SizeOverLifetimeData
    {
        public bool separateAxes;
        public CurveData x; public CurveData y; public CurveData z;
    }
    /// <summary>曲线态 MinMaxCurve 的原始数据（生成器 `curve_raw()` 的产物）——
    /// `mult` 是曲线乘数、`keys` 是时间→值。构建侧用 `CurveFrom()` 还原成 `MinMaxCurve`。</summary>
    [System.Serializable] public class CurveData
    {
        // 🆕 2026-09-21 下半场：常量态（生成器 `mm()` 的产物）—— VFX 那几个模块的参数大多是常量
        public bool isConst; public float c = 1f, cMin = 1f;
        public float mult = 1f;
        public CurveKey[] keys;
        public float multMin = 1f; public CurveKey[] minKeys;   // TwoCurves 才有
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
    /// （默认 `DefaultArena`；`-executeMethod` 本身收不了参数）。菜单项固定建默认那个。
    /// 🆕 **2026-09-25 起也是「一局一份对战场景」那条链的唯一选场入口** ——
    /// `BattleScene.ScenePath` / `BattleScene.BoardArena` 都转发到这里（判定只留一份）。
    /// ⚠️ 名字不认识时**出声**并回退 `DefaultArena`（`DefaultArena` = 原版 `defaultBattleArena`，
    /// 见 `ArenaByArmy`）。</summary>
    public static string ArenaFromEnv()
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

    // ==================================================================
    //  §27 架构 · 第 2 步：逐场把战场内容建成 **prefab**（施工图 → `资料/§27架构_施工图.md`）
    //
    //  为什么：现在 13 份 `Battle_<场>.unity` 各自把战场烘进场景，共享构建代码一改要重打 13 份。
    //  目标 = **1 份 `Battle.unity` + 13 套数据、运行时实例化**（运行时那一侧 = `ArenaRuntimeLoader`）。
    //  · 放 `Assets/Resources/ArenaPrefabs/`：运行时**按名字**取（`Resources.Load<GameObject>`），
    //    不需要 addressables、也不需要一张 13 项的表；
    //  · prefab 里 = `BuildContent` 建的那**整棵树**（网格 / 粒子 / 灯 / 后处理 Volume）＋ 一个
    //    `CardPresentation.ArenaPrefabData`（带**场景级**那套值：雾 / 环境光 / 天空盒 / 相机光学）；
    //  · ⚠️ `BuildContent` 里那几处 `RenderSettings.*` 是**建 prefab 时的副作用**（写在当前那个空场景上），
    //    不影响 prefab 内容 —— 真正应用在运行时（判据只留一处：`ArenaSceneState.Apply`）。
    // ==================================================================
    public const string PrefabDir = "Assets/Resources/ArenaPrefabs";
    public static string ArenaPrefabPath(string scene) => PrefabDir + "/" + scene + ".prefab";

    /// <summary>13 场全建一遍（串行；每场都新建空场景再存 prefab）。</summary>
    public static void BuildArenaPrefabs()
    {
        Directory.CreateDirectory(PrefabDir);
        int ok = 0, bad = 0;
        foreach (var s in AllArenas)
            if (BuildOneArenaPrefab(s)) ok++; else bad++;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Arena] §27 战场 prefab：建好 **{ok}** 件" + (bad > 0 ? $"、**失败 {bad} 件**" : "")
                + $" → `{PrefabDir}/`");
    }

    static bool BuildOneArenaPrefab(string sceneName)
    {
        var mf = LoadManifest(sceneName);
        if (mf == null) return false;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Directory.CreateDirectory(MatDir(sceneName));
        _builtMatNames.Clear();

        var root = BuildContent(null, mf);
        if (root == null) { Debug.LogError($"[Arena] 🔴 {sceneName}：`BuildContent` 没返回根节点 ⇒ 这一件没建"); return false; }

        // 🔴 **层要在这里刷** —— 原来这一步是 `BattleScene` 对**烘进场景**的内容做的
        //   （`SetLayerRecursive(arenaGo, ArenaLayer)` + 把 Volume 还原到 `Default` 层）。
        //   §27 改成运行时实例化之后，场景里没有内容可刷 ⇒ **必须烘进 prefab**：
        //   · 整棵树 → `ArenaSlots.ArenaLayer`（HUD 相机不吃这一层，透视相机专画它）；
        //   · **Volume 子物体回 `Default` 层** —— 原版那是 Default 层、只被 `BoardCamera` 的
        //     `m_VolumeLayerMask = 1` 够得着；不还原的话**后处理会静默失效**（不报错、只是没效果）。
        int nLayer = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) { t.gameObject.layer = CardPresentation.ArenaSlots.ArenaLayer; nLayer++; }
        int nVol = 0;
        foreach (var v in root.GetComponentsInChildren<UnityEngine.Rendering.Volume>(true)) { v.gameObject.layer = 0; nVol++; }

        var data = root.AddComponent<CardPresentation.ArenaPrefabData>();
        data.state = FromManifest(mf);

        var path = ArenaPrefabPath(sceneName);
        bool ok;
        PrefabUtility.SaveAsPrefabAsset(root, path, out ok);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Arena] §27 prefab：{sceneName} → `{path}`"
                + $"（网格/粒子/灯/Volume ＋ ArenaPrefabData：雾 mode {data.state.fogMode} 密度 {data.state.fogShapeDensity:F4}、"
                + $"环境光 ({(int)(data.state.ambientSky.r * 255)},{(int)(data.state.ambientSky.g * 255)},{(int)(data.state.ambientSky.b * 255)})、"
                + $"天空盒 {(data.state.skybox != null ? data.state.skybox.name : "(null)")}；"
                + $"层：{nLayer} 个对象 → ArenaLayer({CardPresentation.ArenaSlots.ArenaLayer})、{nVol} 个 Volume → Default(0)）ok={ok}");
        return ok;
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
    /// <summary>🔴 **打开已存盘的战场场景之后、要「量」或「渲」之前，必须先调这个。**
    ///
    /// 原版材质是**运行时重建**的（bundle 里的 shader 落不了工程资产，见 `AttachOriginalMaterial`），
    /// 而 `OpenScene` 在**编辑态不跑 `Awake`** ⇒ 不调这一步，看到的永远是构建期那份 `URP/Unlit` 兜底。
    /// **踩过（2026-09-22）**：`ProbeMeshMaterials` 因此把 28 个网格**全报成 `URP/Unlit`**
    /// （而同一场景 `RenderPreview` 的重建日志是「成功 28 · 失败 0」）——
    /// **工具在说谎，人去读它就会查错方向**。判据只此一处：所有入口共用这一个 `Rebuild()`。</summary>
    static void PrepareSceneMeasure(out int ok, out int fail)
    {
        // 🆕 2026-09-22 晚：顺带把原版那条 `ApplyAmbientColor` 的全局量也灌下去
        //    （`Shader.SetGlobalFloat` 不随场景存盘 ⇒ 光在建场时设没用）。见 `ArenaEnvGlobal`。
        foreach (var e in UnityEngine.Object.FindObjectsByType<WarpforgeVFX.ArenaEnvGlobal>(FindObjectsSortMode.None))
            e.Apply();
        ok = 0; fail = 0;
        foreach (var c in UnityEngine.Object.FindObjectsByType<WarpforgeVFX.ArenaOriginalMaterial>(FindObjectsSortMode.None))
        { if (c.Rebuild() != null) ok++; else fail++; }

        // 🆕 2026-09-25：平面反射也**必须显式挂钩子** —— 编辑器批处理里 `OnEnable` 不跑
        //    （与上面那两条同一个坑。实测：arena3 建了反射相机、渲出来与没建**逐像素一样**，且不报错）
        foreach (var m in UnityEngine.Object.FindObjectsByType<WarpforgeVFX.ArenaMirror>(FindObjectsSortMode.None))
            m.Apply();

        // 🆕 2026-09-24：粒子材质那条同款 —— 原版关键字（含 `_EMISSION`）在**运行时**补到材质实例上
        //    （写不进 `.mat`，见 `ArenaParticleKeywords` 与 `资料/已知的坑.md`）。
        //    与网格那套同理：`OpenScene` 在编辑态**不跑 `Awake`** ⇒ 预览/量测必须显式刷一遍。
        int pkw = WarpforgeVFX.ArenaParticleKeywords.ApplyAllInScene();
        if (pkw > 0) Debug.Log($"[Arena/PK] 粒子关键字：场景里刷了 {pkw} 个粒子渲染器");

        // 🆕 2026-09-22：闪烁族 —— **批处理下没有帧循环**，`Update` 不会被调 ⇒ 预览里 alpha 停在构建值。
        //    要**固定相位渲一张**（A/B 或自检）就设 `WF_FLICKERPHASE=<秒>`，这里替它推一次。
        //    与 `WF_TIMESHIFT`（灌假 `_Time` 验风/滚 UV）是同一套做法。不设就**不动**（保持可复现）。
        var fl = System.Environment.GetEnvironmentVariable("WF_FLICKERPHASE");
        float ft;
        if (!string.IsNullOrEmpty(fl) && float.TryParse(fl, out ft))
        {
            WarpforgeVFX.WFMaterialFlicker.TimeOverride = ft;
            var fxs = UnityEngine.Object.FindObjectsByType<WarpforgeVFX.WFMaterialFlicker>(FindObjectsSortMode.None);
            for (int i = 0; i < fxs.Length; i++) fxs[i].Tick(ft);
            Debug.Log($"[Arena] WF_FLICKERPHASE={ft}：推了 {fxs.Length} 个闪烁组件一次（alpha 现在按 t={ft} 算）");
        }

        // 🆕 2026-09-30：**4 环境（13×4 = 52 态）的验收尺** —— `WF_ENV="<SO 名>"` 把那条环境**当场应用**再渲。
        //   ⚠️ 走的是**真机同一个执行器**（`CardPresentation.Battle.EnvironmentApplier`），不是另写一套 ——
        //   否则「量的」与「玩的」会是两回事（同 `CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」）。
        //   配 `WF_PSFIXSEED=1` + `RenderPreviewFromCLI` 即可**逐态出图**（出图路径仍是 `preview_*.png`）。
        ApplyEnvProbeIfAny();
    }

    /// <summary>`WF_ENV="<SO 名>"`：把那条环境**当场应用**（instant）——**【4 环境】的验收尺**。
    /// ⚠️ **两条渲染路都要调**（`PrepareSceneMeasure` 与 `RenderPreview` **不是同一条** ——
    ///   本文件里已经为「平面反射」「粒子关键字」各踩过一次这个坑）。</summary>
    static void ApplyEnvProbeIfAny()
    {
        var envName = System.Environment.GetEnvironmentVariable("WF_ENV");
        if (string.IsNullOrEmpty(envName)) return;
        var it = CardPresentation.EnvironmentConditions.Find(envName);
        if (it == null)
        {
            Debug.LogWarning($"[Arena] WF_ENV=\"{envName}\" 在 `Resources/EnvironmentConditions.json` 里查不到"
                           + " ⇒ 环境不动（跑一次 `python 工具/gen_environment_conditions.py`）");
            return;
        }
        var probeGo = new GameObject("EnvProbe(临时)");
        probeGo.AddComponent<CardPresentation.EnvironmentApplier>().Apply(it, true);   // instant（批处理没帧循环）
        Debug.Log($"[Arena] WF_ENV=\"{envName}\"：已应用 · blendTime={it.blendTime:F1}s"
                + $" · fog={it.fogDensity:F4} · ambientBlend={it.ambientBlend:F3}"
                + $" · 物件={(CardPresentation.EnvironmentConditions.HasPrefab(it) ? it.prefabName : "无")}");
    }

    public static void ProbeTransparent()
    {
        var s = ArenaFromEnv();
        if (!File.Exists(ScenePath(s))) { Debug.LogError($"[PT] 没有场景 {ScenePath(s)}"); return; }
        EditorSceneManager.OpenScene(ScenePath(s));
        PrepareSceneMeasure(out _, out _);      // 🔴 不调这个，报出来的 shader 是兜底那份（见它的说明）
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

    /// <summary>诊断：把**场景里每个 MeshRenderer 的材质实况**打出来（**不限队列**，与 `ProbeTransparent` 互补）。
    ///
    /// 为什么要它（2026-09-21 查「验尺子」）：同一个对象在**独立预览场景**里渲成「图集没裁掉」的灰矩形、
    /// 在**对战场景**里正常，而两边引用的 `.mat` guid 逐字相同、盘上那份也确实带着 `_ALPHATEST_ON`
    /// ⇒ **只能看渲染那一刻的实况**，信盘上的文件会得出自相矛盾的结论。
    /// `WF_SCENE` 给场景资产路径（默认 = 独立战场场景）；**两个场景跑同一段代码**，diff 出来的差异才是真差异。
    /// 用法：`WF_SCENE="Assets/CardPresentation/Scenes/Battle.unity" ... -executeMethod ArenaBuilder.ProbeMeshMaterials`</summary>
    public static void ProbeMeshMaterials()
    {
        var path = System.Environment.GetEnvironmentVariable("WF_SCENE");
        if (string.IsNullOrEmpty(path)) path = ScenePath(DefaultArena);
        if (!File.Exists(path)) { Debug.LogError($"[PM] 没有场景 {path}"); return; }

        EditorSceneManager.OpenScene(path);
        // 🔴 **2026-09-22 晚修：探针必须先调一次 `ArenaOriginalMaterial.Rebuild()`。**
        //    原版材质是**运行时重建**的（bundle 里的 shader 落不了工程资产），而 `OpenScene` 在
        //    **编辑态不跑 `Awake`** ⇒ 编辑态看到的永远是构建期那份 `URP/Unlit` 兜底
        //    ⇒ **这个探针一直在报错的 shader**（实测：它把 28 个网格全报成 `URP/Unlit`，
        //    而同一场景 `RenderPreview` 的重建日志是「成功 28 · 失败 0」）。
        //    与 `RenderPreview` 里那段**共用同一个 `Rebuild()`**（判据只此一处）。
        PrepareSceneMeasure(out int rebuilt, out int rfail);
        Debug.Log($"[PM] === 场景 {path} ===（原版材质重建：成功 {rebuilt} · 失败 {rfail}）");
        // 🆕 2026-09-22 晚：把「环境色那一族」的**运行时真值**一并读出来 ——
        //   `_APPLYAMBIENTCOLOR` 那层 tint 到底乘没乘，只有这几个数能回答（别靠读算式猜）。
        Debug.Log($"[PM] 环境色实况：`_AmbientColorBlend`(全局) = {Shader.GetGlobalFloat("_AmbientColorBlend"):F4}"
              + $" · RenderSettings.ambientMode={RenderSettings.ambientMode}"
              + $" ambientLight={RenderSettings.ambientLight} ambientIntensity={RenderSettings.ambientIntensity:F3}"
              + $" · fog={(RenderSettings.fog ? 1 : 0)}");

        foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            Debug.Log($"[PM] CAM {cam.name} enabled={(cam.enabled ? 1 : 0)} clear={cam.clearFlags} mask={cam.cullingMask}"
                    + $" depth={cam.depth} hdr={(cam.allowHDR ? 1 : 0)} msaa={(cam.allowMSAA ? 1 : 0)}"
                    + $" post={(cam.GetUniversalAdditionalCameraData()?.renderPostProcessing == true ? 1 : 0)}"
                    + $" pos=({cam.transform.position.x:F2},{cam.transform.position.y:F2},{cam.transform.position.z:F2})");

        // 🆕 2026-09-22 晚：**混合状态的运行时真值** —— 「透明没生效」这类事只有读回来才算数
        foreach (var mr in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            var nm = PathOf(mr.transform);
            if (nm.IndexOf("Light FX", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            var m0 = mr.sharedMaterial;
            if (m0 == null) { Debug.Log($"[PM] BLEND {nm} 材质为 null"); continue; }
            string F1(string k) => m0.HasProperty(k) ? m0.GetFloat(k).ToString("F1") : "无此属性";
            Debug.Log($"[PM] BLEND {nm} shader={m0.shader.name} rq={m0.renderQueue}"
                  + $" _Surface={F1("_Surface")} _Blend={F1("_Blend")} src={F1("_SrcBlend")} dst={F1("_DstBlend")}"
                  + $" srcA={F1("_SrcBlendAlpha")} dstA={F1("_DstBlendAlpha")} _ZWrite={F1("_ZWrite")}"
                  + $" kwTransparent={m0.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")}");
        }

        var rows = new List<string>();
        foreach (var mr in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            var m = mr.sharedMaterial;
            var tex = m != null ? m.GetTexture("_BaseMap") as Texture2D : null;
            rows.Add($"[PM] {PathOf(mr.transform),-52} mat={(m != null ? m.name : "(null)"),-34}"
                   + $" shade={(m != null && m.shader != null ? m.shader.name : "(null)")}"
                   + $" q={(m != null ? m.renderQueue : -1)} clip={(m != null ? m.GetFloat("_AlphaClip") : -1f):F0}"
                   + $" cut={(m != null ? m.GetFloat("_Cutoff") : -1f):F2} zw={(m != null ? m.GetFloat("_ZWrite") : -1f):F0}"
                   + $" kw=[{(m != null ? string.Join(",", m.shaderKeywords) : "")}]"
                   + $" tex={(tex != null ? tex.name : "(无)")} fmt={(tex != null ? tex.format.ToString() : "-")}"
                   + $" L={mr.gameObject.layer} en={(mr.enabled ? 1 : 0)}");
        }
        rows.Sort();
        foreach (var r in rows) Debug.Log(r);
        Debug.Log($"[PM] === 共 {rows.Count} 个 MeshRenderer ===");

        // 粒子渲染器也一并打 —— 2026-09-21 查到「粒子播出来就是一坨纯中性灰 (200,200,200)」，
        // 而贴图本身有 alpha（`smokeysteam.png` 55.9% 透明）、材质也设了混合 ⇒ **必须看实况**。
        var prows = new List<string>();
        foreach (var pr in UnityEngine.Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.None))
        {
            var m = pr.sharedMaterial;
            var tex = m != null ? m.GetTexture("_BaseMap") as Texture2D : null;
            var ps = pr.GetComponent<ParticleSystem>();
            prows.Add($"[PM] PS {pr.name,-34} mat={(m != null ? m.name : "(null)"),-24}"
                    + $" shade={(m != null && m.shader != null ? m.shader.name : "(null)")}"
                    + $" q={(m != null ? m.renderQueue : -1)} surf={(m != null ? m.GetFloat("_Surface") : -1f):F0}"
                    + $" src={(m != null ? m.GetFloat("_SrcBlend") : -1f):F0} dst={(m != null ? m.GetFloat("_DstBlend") : -1f):F0}"
                    + $" zw={(m != null ? m.GetFloat("_ZWrite") : -1f):F0}"
                    // 🆕 2026-09-24：加两列 —— `ArenaBuilder` 按「贴图名」落盘材质（`SaveOrReuse` 用
                    //   `fresh.name` 当路径），而缓存 key 里**含颜色** ⇒ 同一贴图不同 `matColor` 会互相覆盖。
                    //   实测 arena1 清单里 13 个「贴图×颜色」组合只落了 **11** 个 `.mat`、且 `_BaseColor` 全是白的
                    //   ⇒ 这两列就是判「颜色到底灌进去没有」的一手判据。
                    + $" hasBC={(m != null && m.HasProperty("_BaseColor") ? 1 : 0)}"
                    + $" bc={(m != null && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "-")}"
                    + $" kw=[{(m != null ? string.Join(",", m.shaderKeywords) : "")}]"
                    + $" tex={(tex != null ? tex.name : "(无)")} fmt={(tex != null ? tex.format.ToString() : "-")}"
                    + $" startCol={(ps != null ? ps.main.startColor.color.ToString() : "-")}"
                    + $" playing={(ps != null && ps.isPlaying ? 1 : 0)}"
                    + $" alive={(ps != null ? ps.particleCount : -1)}");
        }
        prows.Sort();
        foreach (var r in prows) Debug.Log(r);
        Debug.Log($"[PM] === 共 {prows.Count} 个 ParticleSystemRenderer ===");
    }

    /// <summary>把一个 Transform 的**全路径**拼出来（`根/父/自己`）—— 诊断输出里用来定位对象。</summary>
    static string PathOf(Transform t)
    {
        var s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    /// <summary>诊断：**用同一段代码、同一分辨率**把「独立战场场景」与「对战场景」各渲一张。
    ///
    /// 为什么要它（2026-09-21「验尺子」）：两张图肉眼不一样，但两个场景的**对象 / mesh guid / 材质 guid /
    /// 材质实况 / RenderSettings 逐项相同**（`ProbeMeshMaterials` 实测）⇒ 剩下的变量只有
    /// ①场景本身 ②渲染方式与分辨率。**只有把渲染方式与分辨率固定住**，才能判到底是不是场景的锅。
    /// 产物：`_tmp_view/arena_ab/ab_{standalone,battle}.png`（同尺寸，可以直接逐像素比）。</summary>
    public static void AbRender()
    {
        const int W = 1280, H = 720;
        var dir = "d:/4/_tmp_view/arena_ab";
        Directory.CreateDirectory(dir);

        ShotToFile(ScenePath(DefaultArena), c => true, $"{dir}/ab_standalone.png", W, H);
        ShotToFile($"Assets/CardPresentation/Scenes/{BattleSceneName}", c => c.depth < 0, $"{dir}/ab_battle.png", W, H);
        Debug.Log("[AB] === 两张已出，路径 " + dir + " ===");
    }

    /// <summary>对战场景的资产名（`BattleScene.BuildAndSaveScene` 存的那个）—— 只在这里写一次。</summary>
    const string BattleSceneName = "Battle.unity";

    static void ShotToFile(string scenePath, System.Func<Camera, bool> pick, string outPath, int W, int H)
    {
        if (!File.Exists(scenePath)) { Debug.LogError($"[AB] 没有场景 {scenePath}"); return; }
        EditorSceneManager.OpenScene(scenePath);
        PrepareSceneMeasure(out _, out _);      // 🔴 渲染路径同样要先重建（否则 A/B 用的是兜底材质）
        Camera cam = null;
        foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (pick(c)) { cam = c; break; }
        if (cam == null) { Debug.LogError($"[AB] {scenePath}：没挑到相机"); return; }

        cam.aspect = (float)W / H;

        // `WF_PSSTEP=1` 时**按 1/60 推 2 秒**再渲（与 `RenderPreview` 同口径）——
        // 用来判「粒子造出来的那片灰幕」到底只在预览里，还是游戏里也有。
        if (System.Environment.GetEnvironmentVariable("WF_PSSTEP") == "1")
            foreach (var ps in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(false);
                for (int i = 0; i < 120; i++) ps.Simulate(1f / 60f, false, false, false);
            }

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
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(rt);
        Debug.Log($"[AB] {scenePath} → {outPath}（相机 {cam.name} clear={cam.clearFlags} mask={cam.cullingMask}）");
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
        // 🆕 2026-09-21：尺寸可换（`WF_W` / `WF_H`，默认 1280×720）。
        //    为什么要这个：原版那批真渲图是 **1920×1080**、`arena_compare.py` 把它**缩到 1280×720** 再比，
        //    而我们是**原生 1280×720** ⇒ 两边的高频细节本来就不一样。追「整体偏亮」那条线时要能
        //    **同分辨率对照**，否则分不清「渲染差异」和「重采样差异」。
        int W = 1280, H = 720;
        int.TryParse(System.Environment.GetEnvironmentVariable("WF_W"), out W);
        int.TryParse(System.Environment.GetEnvironmentVariable("WF_H"), out H);
        if (W < 64) W = 1280;
        if (H < 64) H = 720;
        var scenePath = ScenePath(scene);
        EditorSceneManager.OpenScene(scenePath);

        // 🆕 2026-09-21：网格上挂着 `ArenaOriginalMaterial` 的，**在这里重建一次原版材质**。
        //    为什么预览要手动调：原版材质是**运行时**重建的（bundle shader 落不了工程资产，
        //    见 `AttachOriginalMaterial` 的注释），而 `OpenScene` 在**编辑态**不会走 `Awake`。
        //    ⚠️ 这是**唯一一份判据** —— 运行时走 `Awake`、预览走这里，两边调的是同一个 `Rebuild()`。
        //    A/B 旧的 `URP/Unlit` 路径：`WF_URPUNLIT=1`。
        if (System.Environment.GetEnvironmentVariable("WF_URPUNLIT") != "1")
        {
            var comps = Object.FindObjectsByType<WarpforgeVFX.ArenaOriginalMaterial>(FindObjectsSortMode.None);
            int ok = 0, fail = 0;
            foreach (var c in comps) { if (c.Rebuild() != null) ok++; else fail++; }
            if (comps.Length > 0)
                Debug.Log($"[Arena/OS] 原版材质重建：**成功 {ok} 个** · 失败 {fail} 个（场景共挂 {comps.Length} 个）");

            // 🆕 2026-09-25：平面反射同款（**这是第二条路** —— `RenderPreview` 不复用 `PrepareSceneMeasure`，
            //   所以两边都得挂一次；实测只加在 `PrepareSceneMeasure` 里 ⇒ 预览里反射**一点没生效**、
            //   而且**一声不响**：渲出来与没建**逐像素一样**）。
            foreach (var m in Object.FindObjectsByType<WarpforgeVFX.ArenaMirror>(FindObjectsSortMode.None))
                m.Apply();

            // 🆕 2026-09-24：粒子那条同款（这里是**第二条路** —— `RenderPreview` 不复用
            //   `PrepareSceneMeasure`）。原版关键字（含 `_EMISSION`）在运行时补到材质实例上，见
            //   `WarpforgeVFX.ArenaParticleKeywords` 与 `资料/已知的坑.md`。
            int pkw = WarpforgeVFX.ArenaParticleKeywords.ApplyAllInScene();
            if (pkw > 0) Debug.Log($"[Arena/PK] 粒子关键字：刷了 {pkw} 个粒子渲染器");

            // 🆕 2026-09-30：**环境验收尺**（`WF_ENV="<SO 名>"`）—— **这条是第二条路，别只加在
            //   `PrepareSceneMeasure` 里**（本文件已经为「平面反射」「粒子关键字」各踩过一次这个坑）。
            ApplyEnvProbeIfAny();

            // 🆕 2026-09-22：**逐项隔离开关**（诊断用，改完自动还原）。
            //   用途：换原版 shader 之后有几场变亮（sororitas 1.138→1.213）——
            //   要分清是哪一项贡献的，只能**一项一项关掉再渲**：
            //     · `WF_NOEMIT=1`  把 `_EmissiveColor` 归零（自发光）
            //     · `WF_NOAMB=1`   关掉 `_APPLYAMBIENTCOLOR` 关键字
            //     · `WF_NOREFL=1`  把 `_ReflectionStrength` 归零（`Floor` 的平面反射；那条链要 `_CameraOpaqueTexture`）
            foreach (var c in comps)
            {
                var mr = c.GetComponent<MeshRenderer>();
                var mat = mr != null ? mr.sharedMaterial : null;
                if (mat == null) continue;
                if (System.Environment.GetEnvironmentVariable("WF_NOEMIT") == "1")
                { if (mat.HasProperty("_EmissiveColor")) mat.SetVector("_EmissiveColor", Vector4.zero);
                  if (mat.HasProperty("_EmissionColor")) mat.SetVector("_EmissionColor", Vector4.zero); }
                if (System.Environment.GetEnvironmentVariable("WF_NOAMB") == "1")
                    mat.DisableKeyword("_APPLYAMBIENTCOLOR");
                if (System.Environment.GetEnvironmentVariable("WF_NOREFL") == "1" && mat.HasProperty("_ReflectionStrength"))
                    mat.SetFloat("_ReflectionStrength", 0f);
            }
            // ⚠️ `Shader.SetGlobalFloat` 那侧也一并关掉环境色混合（原版是逐场景 `SetGlobalFloat` 灌的，
            //    我们从不设 ⇒ 默认 0；这里显式写 0 是为了排除「别处有人设过」）
            if (System.Environment.GetEnvironmentVariable("WF_NOAMB") == "1")
                Shader.SetGlobalFloat("_AmbientColorBlend", 0f);

            // 🆕 2026-09-22：**`maxParticleSize` 诊断开关**（`WF_MAXPS=<值>`）——
            //    原版这批多是 **20（= 不限幅）**，我们吃 Unity 默认 **0.5**。
            //    正本记「照抄反而更差（arena3 1.032→1.181）」所以**故意留着没设**；
            //    但现在 arena1 的**烟是一大团黑球、火很小**，正是「尺寸被幅面卡住」的症状 ⇒ 单独量一次。
            var maxps = System.Environment.GetEnvironmentVariable("WF_MAXPS");
            if (!string.IsNullOrEmpty(maxps) && float.TryParse(maxps, out float mv))
            {
                int nps = 0;
                foreach (var pr in Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.None))
                { pr.maxParticleSize = mv; nps++; }
                Debug.Log($"[Arena] WF_MAXPS={mv}：改了 {nps} 个粒子渲染器");
            }
        }

        var cam = Object.FindFirstObjectByType<Camera>();

        // 🆕 2026-09-28 诊断：**阴影链探针** `WF_SHADOWPROBE=1` —— 在相机前放一对
        //   「投射体（`PrimitiveType.Cube`，castShadows On）+ 受影板（`PrimitiveType.Plane`，`URP/Lit`）」，
        //   用来看**这台工程的 URP 实时阴影链到底活不活**。
        //   背景（别删）：`battlearenablacklegion` 前景比原版亮 +29~+37，而那批网格用的是
        //   `Everguild/Misc/Unlit shadows receiver`（材质带 `_ShadowColor = (0.451,0.349,0.394)`）——
        //   **原版是真的在投/收实时阴影**（该场 67 个 `MeshRenderer` 里 52 个 `m_CastShadows=1 / m_ReceiveShadows=1`，
        //   见 `bundle_scenes_scenes_battlearenablacklegion/MeshRenderer/`）。
        //   而把我们的投射者打开（`WF_SHADOWCAST=1`）后画面**逐格几乎不变**（48.89→48.85），
        //   连把地板换成 `URP/Lit` 也**一个投影都没有** ⇒ **不是 blacklegion 的事，是这条链整体没跑**。
        //   这个探针只回答「活不活」，**是诊断不是修法**。
        if (System.Environment.GetEnvironmentVariable("WF_SHADOWPROBE") == "1")
        {
            var spFwd = cam.transform.forward;
            var basePos = new Vector3(cam.transform.position.x, 0f, cam.transform.position.z) + spFwd * 8f;
            var plate = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plate.name = "ShadowProbe_Plate";
            plate.transform.position = new Vector3(basePos.x, 0.05f, basePos.z);   // ⚠️ 必须**在地板之上**（第一次摆在 y=−0.78，被地板埋了）
            plate.transform.localScale = Vector3.one * 3f;
            plate.GetComponent<MeshRenderer>().sharedMaterial =
                new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            plate.GetComponent<MeshRenderer>().receiveShadows = true;
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "ShadowProbe_Cube";
            cube.transform.position = new Vector3(basePos.x, 2.5f, basePos.z);
            cube.transform.localScale = Vector3.one * 2f;
            cube.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            Debug.Log($"[Arena] WF_SHADOWPROBE：板 {plate.transform.position} · 方块 {cube.transform.position}"
                    + $" · 光 shadows={(Object.FindFirstObjectByType<Light>() != null ? ((int)Object.FindFirstObjectByType<Light>().shadows).ToString() : "无灯")}"
                    + $" · RP={(GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "无")}"
                    + $" · 质量档={QualitySettings.GetQualityLevel()}/{QualitySettings.names.Length}");
        }

        if (cam == null) { Debug.LogError("[Arena] 预览失败：场景里没有相机"); return; }

        if (debugHoles)
        {
            RenderSettings.skybox = null;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 1f, 0f);   // 亮绿 = 洞
        }

        // 🆕 2026-09-21：**逐物体可见性筛** —— `WF_HIDE=<名字子串>[,<子串>…]` 把匹配的对象关掉再渲一张。
        //   用途：「这一块画面到底是谁画的」只有把它单独关掉、再渲一张才能定案
        //   （`资料/战场13场_逐场对账_0920.md` 里那条「整片灰白矩形」的待查就是这么查的）。
        //   判据 = `GameObject.name` **包含**子串（大小写不敏感）；**诊断用**，正常预览不受影响（渲完还原）。
        var hideArg = System.Environment.GetEnvironmentVariable("WF_HIDE");
        var hidden = new System.Collections.Generic.List<GameObject>();
        if (!string.IsNullOrEmpty(hideArg))
        {
            var pats = hideArg.Split(',');
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (!go.activeSelf) continue;
                foreach (var pat in pats)
                {
                    var t = pat.Trim();
                    if (t.Length == 0) continue;
                    // 前缀 `=` ⇒ **精确匹配**（否则子串）。为什么要这个：
                    //   场景里有 `Background` / `Background Building 1/2` / `Back background` 四个近名对象，
                    //   子串匹配一次会全关掉 ⇒ **一次改一个变量**这条准则就废了。
                    bool hit = GoMatches(go, pat);
                    if (hit) { go.SetActive(false); hidden.Add(go); break; }
                }
            }
            // ⚠️ 日志打**层级路径**不是名字 —— 场景里有**完全重名**的对象（arena1 有两个 `SmokeEffect`），
            //    只打名字看不出关掉的是哪一个（2026-09-22 踩过）。
            Debug.Log($"[Arena] WF_HIDE=\"{hideArg}\" 关掉了 {hidden.Count} 个对象："
                      + string.Join(" / ", hidden.ConvertAll(g => GoPath(g)).ToArray()));
        }

        // 🆕 2026-09-22：**按下标点名** —— `WF_HIDEIDX=<清单下标>[,<下标>…]`。
        //   重名对象（arena1 三个 `SmokeEffect`）与平铺层级下，名字/路径都分不开，只有下标能。
        //   判据 = 清单 `arenas/<场>/<场>_manifest.json` 的 `particles[]` 顺序
        //   （挂在对象上的 `ArenaParticleIndex` 就是它 —— 保存进场景的那一份）。
        var idxArg = System.Environment.GetEnvironmentVariable("WF_HIDEIDX");
        if (!string.IsNullOrEmpty(idxArg))
        {
            var byIdx = new System.Collections.Generic.Dictionary<int, GameObject>();
            foreach (var c in Object.FindObjectsByType<WarpforgeVFX.ArenaParticleIndex>(FindObjectsSortMode.None))
                if (c != null && c.gameObject != null) byIdx[c.index] = c.gameObject;
            var hit = new System.Collections.Generic.List<string>();
            foreach (var tok in idxArg.Split(','))
            {
                int idx;
                if (!int.TryParse(tok.Trim(), out idx)) continue;
                GameObject g;
                if (!byIdx.TryGetValue(idx, out g) || g == null)
                { Debug.LogWarning($"[Arena] WF_HIDEIDX：场景里没有下标 {idx} 的粒子（本场共 {byIdx.Count} 个）"); continue; }
                g.SetActive(false);
                hidden.Add(g);
                hit.Add($"#{idx}={g.name}");
            }
            Debug.Log($"[Arena] WF_HIDEIDX=\"{idxArg}\" 关掉了 {hit.Count} 个：{string.Join(" / ", hit.ToArray())}");
        }

        // 🆕 2026-09-21：**只留指定对象**（`WF_ONLY=<名字子串>[,<子串>…]`，前缀 `=` 精确匹配）——
        //   「这个网格到底画在哪、画成什么」的正向隔离。与 `WF_HIDE` 对称，两个可以一起用。
        //   用它查过/要查的：arena1 那块 `Background` 板、以及裁掉透明区之后**缺掉的那批道具**。
        var onlyArg = System.Environment.GetEnvironmentVariable("WF_ONLY");
        if (!string.IsNullOrEmpty(onlyArg))
        {
            var pats = onlyArg.Split(',');
            int kept = 0, off = 0;
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.transform.parent != null) continue;      // 只从**根**开始判，避免把子节点单独关掉
                bool keep = false;
                foreach (var pat in pats)
                {
                    if (GoMatches(go, pat)) { keep = true; break; }
                }
                if (!keep) { go.SetActive(false); hidden.Add(go); off++; }
                else kept++;
            }
            Debug.Log($"[Arena] WF_ONLY=\"{onlyArg}\" 只留 {kept} 个根节点、关掉 {off} 个");
        }

        // 诊断：`WF_PSMODE=billboard|nolength|orig` 改**所有**粒子渲染器的渲染模式后再渲。
        // 2026-09-21 加 —— 查「一个叫 `Battle` 的粒子（贴图 `fighter jets.png`）在上空糊成一大片灰幕」：
        // 它 `renderMode=1`（Stretch）。原版那批 `ParticleSystemRenderer` 的 **`m_LengthScale` 逐条不同
        // （0 / 0.05 / 1.0 / 3.88 / 12.1）**，而我们**从来没搬过这个字段** ⇒ 用的是 Unity 默认 2。
        var psMode = System.Environment.GetEnvironmentVariable("WF_PSMODE");
        if (!string.IsNullOrEmpty(psMode))
        {
            int n = 0;
            foreach (var pr in Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.None))
            {
                if (psMode == "billboard") pr.renderMode = ParticleSystemRenderMode.Billboard;
                else if (psMode == "nolength") { pr.renderMode = ParticleSystemRenderMode.Stretch; pr.lengthScale = 0f; pr.velocityScale = 0f; }
                n++;
            }
            Debug.Log($"[Arena] WF_PSMODE={psMode} 改了 {n} 个粒子渲染器");
        }

        // 诊断：`WF_NOPOST=1` —— 把相机的后处理关掉再渲，用来判「后处理那趟到底跑没跑」。
        // 2026-09-21 加：数据驱动的太阳耀斑是在 **uber 最终后处理那趟**里画的
        // （`PostProcessPass.cs` 的 `RenderFinalPass`，`if (useLensFlare)`），
        // 耀斑没出现 ⇒ 要么这趟没跑，要么 `useLensFlare` 在那一刻是假。
        if (System.Environment.GetEnvironmentVariable("WF_NOPOST") == "1")
        {
            var uacd2 = cam.GetUniversalAdditionalCameraData();
            if (uacd2 != null) { uacd2.renderPostProcessing = false; Debug.Log("[Arena] WF_NOPOST：关掉相机的后处理"); }
        }

        // 诊断：`WF_FLARENOOCC=1` —— 把太阳耀斑的**遮挡**关掉再渲。
        // 2026-09-21 加：耀斑建好后组件已注册（`LensFlareCommonSRP.IsEmpty=False`）、也在视口内，
        // 却不出现在画面上 ⇒ 要分开「被几何遮住了」和「后处理那趟没画」这两件事。
        if (System.Environment.GetEnvironmentVariable("WF_FLARENOOCC") == "1")
        {
            int nf = 0;
            foreach (var lf in UnityEngine.Object.FindObjectsByType<LensFlareComponentSRP>(FindObjectsSortMode.None))
            { lf.useOcclusion = false; nf++; }
            Debug.Log($"[Arena] WF_FLARENOOCC：关掉 {nf} 个太阳耀斑的遮挡");
        }

        // 诊断：`WF_NOBLOOM=1` / `WF_NOVIGNETTE=1` —— 只关掉 Volume 里的**一个**效果。
        // 2026-09-21 加：实测发现 **关掉整条后处理之后 max 回到 255（p99 223），开着只有 201（p99 185）**，
        // 而**原版（同样有 Bloom+Vignette）max 是 254.8 / p99 214.4** ⇒ 是我们的后处理把亮端压掉了。
        // 要分清是 Bloom 还是 Vignette（还是别的），只能一个一个关。
        var vol = UnityEngine.Object.FindFirstObjectByType<Volume>();
        var prof = vol != null ? vol.sharedProfile : null;
        var toggled = new List<VolumeComponent>();
        if (prof != null)
        {
            if (System.Environment.GetEnvironmentVariable("WF_NOBLOOM") == "1" && prof.TryGet<Bloom>(out var bl2))
            { bl2.active = false; toggled.Add(bl2); Debug.Log("[Arena] WF_NOBLOOM：关掉 Bloom"); }
            if (System.Environment.GetEnvironmentVariable("WF_NOVIGNETTE") == "1" && prof.TryGet<Vignette>(out var vg2))
            { vg2.active = false; toggled.Add(vg2); Debug.Log("[Arena] WF_NOVIGNETTE：关掉 Vignette"); }
        }

        // 批处理下没有 Update 循环，粒子不会自己推进 —— 得**手动推进**，
        // 否则预览图里粒子全是空的（看起来像没建出来）。
        //
        // 🔴 **2026-09-21 修：原来是一步跳 3 秒**
        //    （`ps.Simulate(3f, withChildren: true, restart: true, fixedTimeStep: false)`），
        //    那等于**按 3 秒的 dt 积分一次**：粒子瞬间被拉伸到荒谬的尺寸、颜色曲线停在末段
        //    ⇒ 预览图上冒出一批**灰白硬边大方块**。
        //    **实测（arena1，同一场景、同一相机、同一分辨率）**：改成小步推进后
        //    `gray%`（>190 的中性灰）**5.61 → 0.07**、`hi%`（V>200）**2.42 → 0.00**。
        //    ⚠️ 也就是说 `资料/战场13场_逐场对账_0920.md` 里记的「**我们多画了白雾/白烟**」
        //    「**还剩 1.25% 中性灰**」「**对比度被压缩**」里，**有相当一部分是这个 bogus 推进造出来的**，
        //    不是场景的问题 —— 那条线**必须用修好的尺子重测**（见 `资料/战场13场_逐场对账_0920.md` §七）。
        //    正确口径 = **按真实播放小步推进**，与战斗场景 `BattleScene` 的 `ps.Simulate(dt)` 一致。
        //
        // 🔴 **2026-09-22：再加两样 —— `prewarm` 的等价物 + 可调推进时长**（`WF_PSSEC` / `WF_PSPREWARM`）。
        //   起因：arena1 的烟囱（清单 `#22` = 原版 `Scenario/Particles/SmokeEffect`）我们渲成
        //   **一大团黑色实心球**，原版是**细烟缕**。`WF_HIDEIDX=22` 一关，黑球消失、画面立刻对上。
        //   那颗的参数是 `simulationSpeed=0.1` + `prewarm=true` + `duration=10` + `startSpeed=2~3`：
        //   **`Simulate(2 秒)` 是「真实秒」，而 0.1 倍速 ⇒ 系统时间只走了 0.2 秒**
        //   ⇒ 粒子几乎没离开发射口 ⇒ 几张大贴片（`startSize=4~5`）叠成一坨。
        //   而原版那张真渲图是**跑起来之后**拍的：prewarm 已生效、系统早在稳态。
        //   ⇒ **这一条先要分清「场景错」还是「尺子错」**，所以两样都做成可调的：
        //     · `WF_PSPREWARM=0` 关掉预热等价物（默认开）
        //     · `WF_PSSEC=<真实秒>` 改推进时长（默认 2.0，与进战场后 2 秒同口径）
        var pssArg = System.Environment.GetEnvironmentVariable("WF_PSSEC");
        float psSec = 2f;
        if (!string.IsNullOrEmpty(pssArg) && float.TryParse(pssArg, out float psv) && psv > 0f) psSec = psv;
        bool emulatePrewarm = System.Environment.GetEnvironmentVariable("WF_PSPREWARM") != "0";
        // 🆕 2026-09-22：两个**诊断覆盖**（只为定位用，别拿它当修法）——
        //   烟囱 #22 的实测：粒子活了 5.7 秒却只离发射体 **0.83 单位**，而 `初速` 写的是 2~3
        //   ⇒ 要么「初速没生效」，要么「倍速把位移吃掉了」。这两个开关一次问清：
        //     · `WF_PSSPEED=<v>`  把所有系统的 `startSpeed` 覆盖成常数 v
        //     · `WF_PSSIMSPEED=<v>` 把所有系统的 `simulationSpeed` 覆盖成 v
        float forceSpeed = -1f, forceSim = -1f;
        float.TryParse(System.Environment.GetEnvironmentVariable("WF_PSSPEED"), out forceSpeed);
        float.TryParse(System.Environment.GetEnvironmentVariable("WF_PSSIMSPEED"), out forceSim);
        // 🔴 **2026-09-22 晚新增：`WF_PSFIXSEED=1` 固定粒子随机种子（诊断用，默认关）。**
        //   为什么非要有它：原版这批粒子是 **`autoRandomSeed = true`**（我们照抄 = 语义对），
        //   ⇒ **每渲一次粒子位置都不一样** ⇒ 同一份构建 arena3 的亮度比给过
        //   **1.095 / 1.213 / 1.236**、arena1 烟囱块给过 **−4.6 / −11.6 / −29.7**。
        //   于是「开了某个开关」和「换了次种子」**分不开** —— A/B 全是噪声（这一条当天踩过）。
        //   固定种子只影响**测量**，产品语义仍是原版的 `autoRandomSeed=true`。
        bool fixSeed = System.Environment.GetEnvironmentVariable("WF_PSFIXSEED") == "1";
        const float dt = 1f / 60f;
        int steps = Mathf.Max(1, Mathf.RoundToInt(psSec / dt));   // 2.0 秒 ≈ 进战场后稳定下来的样子
        int nSim = 0, nPre = 0, seedCounter = 0;
        float maxWarm = 0f;
        foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            // ⚠️ 这两个在 **`ParticleSystem` 自己**身上（不是 `MainModule`）：`useAutoRandomSeed` / `randomSeed`。
            //    **必须在 `Play()` 之前设**（Unity 在 Play 那一刻抽种子）。
            //    🔴 种子**按 `ArenaParticleIndex` 的下标取**，不要用循环计数器 ——
            //    `FindObjectsByType` 的返回**顺序不保证**，用计数器会让「同一份场景两次跑的种子不同」，
            //    固定种子就白做了（实测：用计数器时差>8 的像素还有 2.5%，改成按下标后才是真确定）。
            if (fixSeed)
            {
                ps.useAutoRandomSeed = false;
                var ai = ps.GetComponent<WarpforgeVFX.ArenaParticleIndex>();
                ps.randomSeed = (uint)(1000 + (ai != null ? ai.index : Mathf.Abs(ps.name.GetHashCode()) % 9973));
            }
            ps.Play(false);
            var mmMain = ps.main;
            if (forceSpeed > 0f) mmMain.startSpeed = new ParticleSystem.MinMaxCurve(forceSpeed);
            if (forceSim > 0f) mmMain.simulationSpeed = forceSim;
            // `main.prewarm` 的 Unity 语义 = **开局先跑满一个 `duration` 的系统时间**。
            // ⚠️ 批处理/编辑态下它不生效（下面这段是它的等价物）；对 **looping** 系统多推一点
            //    只会换相位、不会推坏稳态，所以开着是安全的。
            if (emulatePrewarm && mmMain.prewarm && mmMain.loop)
            {
                float spd = Mathf.Max(0.001f, mmMain.simulationSpeed);
                // `Simulate` 收的是**真实秒**，要推进 `duration` 的**系统时间**就得除以倍速
                float warmReal = Mathf.Min(mmMain.duration / spd, 300f);
                int wsteps = Mathf.Min(Mathf.RoundToInt(warmReal / dt), 20000);
                for (int i = 0; i < wsteps; i++)
                    ps.Simulate(dt, withChildren: false, restart: false, fixedTimeStep: false);
                nPre++;
                if (warmReal > maxWarm) maxWarm = warmReal;
            }
            for (int i = 0; i < steps; i++)
                ps.Simulate(dt, withChildren: false, restart: false, fixedTimeStep: false);
            nSim++;
        }
        Debug.Log($"[Arena] 已按 1/60 步长推进 {nSim} 个粒子系统各 {psSec:F1} 秒"
                  + $"（其中 {nPre} 个带 prewarm，最长预热推了 {maxWarm:F1} 真实秒）");
        // 🆕 2026-09-22：**把「活粒子数」打出来** —— 判「烟是一坨」到底是
        //   「粒子太少/太聚」还是「粒子够多但没散开」，这是唯一的一手判据（别靠看缩略图猜）。
        foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
        {
            var mm2 = ps.main;
            // 再打**位置跨度 + 速度**：判「粒子没散开」是「不动」还是「动得对但贴片太大」，
            // 只有这一手数据能分开（缩略图分不开）。`ParticleSystem.Particle` 收的是**粒子本地空间**。
            var buf = new ParticleSystem.Particle[Mathf.Max(1, ps.particleCount)];
            int got = ps.GetParticles(buf);
            if (got > 0)
            {
                var mn = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                var mx = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                float vsum = 0f, ageSum = 0f;
                for (int i = 0; i < got; i++)
                {
                    mn = Vector3.Min(mn, buf[i].position);
                    mx = Vector3.Max(mx, buf[i].position);
                    vsum += buf[i].velocity.magnitude;
                    ageSum += Mathf.Max(0f, buf[i].startLifetime - buf[i].remainingLifetime);
                }
                var span = mx - mn;
                Debug.Log($"[Arena] 粒子数 {ps.name} = {got} / max {mm2.maxParticles}"
                          + $"（simSpeed {mm2.simulationSpeed:F2} · 寿命 {mm2.startLifetime.constantMin:F1}~{mm2.startLifetime.constantMax:F1}"
                          + $" · 初速 {mm2.startSpeed.constantMin:F1}~{mm2.startSpeed.constantMax:F1}"
                          + $" · 尺寸 {mm2.startSize.constantMin:F1}~{mm2.startSize.constantMax:F1}）"
                          + $" 跨度 {span.x:F1}×{span.y:F1}×{span.z:F1} 平均速度 {vsum / got:F2}"
                          + $" 平均已存活 {ageSum / got:F1}");
                // 只对点名的那一颗再多打几行：**粒子的世界位置 vs 发射体位置** ——
                // 「粒子到底动没动」只有把两者并排看才算数（跨度小也可能是「跑出去又回来」）。
                var dump = System.Environment.GetEnvironmentVariable("WF_PSDUMP");
                if (!string.IsNullOrEmpty(dump) && ps.name.IndexOf(dump, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Debug.Log($"[Arena]   PSDUMP {ps.name} 发射体世界位置 {ps.transform.position}"
                              + $" localScale {ps.transform.lossyScale} simSpace {mm2.simulationSpace} scalingMode {mm2.scalingMode}");
                    for (int i = 0; i < Mathf.Min(got, 8); i++)
                        Debug.Log($"[Arena]   PSDUMP #{i} age {(buf[i].startLifetime - buf[i].remainingLifetime):F2}"
                                  + $" size {buf[i].GetCurrentSize(ps):F2} pos {buf[i].position}"
                                  + $" vel {buf[i].velocity} |v| {buf[i].velocity.magnitude:F2}");
                }
            }
        }

        // 🔴 **2026-09-21 记一笔（别重走）**：数据驱动的太阳耀斑是在 `RenderFinalPass` 里画进**后备缓冲**的
        //    （`PostProcessPassRenderGraph.cs` 的 `if (useLensFlare)`，目标写死 `backBufferColor`），
        //    而那趟**只在 `cameraData.resolveFinalTarget` 为真时才跑** ⇒
        //    **相机一旦渲到 RenderTexture（我们所有预览/截图都是这样），耀斑就永远不会出现。**
        //    试过「`targetTexture = null` 渲到后备缓冲 + `CommandBuffer.Blit(CameraTarget, rt)` 拷回来」
        //    —— **在 `-batchmode` 下卡死**（批处理没有可用的后备缓冲），所以**这条验证路走不通**。
        //    ⇒ **要看耀斑只能进带窗口的 Play**（那一趟才有后备缓冲；`BattleScene.Shot`/`DumpCam` 同样吃不到）。
        // 诊断：`WF_HDRRT=1` —— 用 **HDR（ARGBHalf）** 的 RT 渲，而不是默认的 LDR（ARGB32）。
        // 2026-09-21 加：实测发现 **关掉整条后处理 max 回到 255、开着只有 ~201**，而逐项关
        // Bloom / Vignette / 默认 Volume Profile / 改 HDR 分级**都不管用** ⇒ 嫌疑落到
        // 「**LDR 的 RT 让后处理链走了一条压高光的路**」。原版那批截图是**游戏直接上屏**（HDR）拿的。
        var wantHdrRt = System.Environment.GetEnvironmentVariable("WF_HDRRT") == "1";
        var rt = new RenderTexture(W, H, 24, wantHdrRt ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32);
        var prevTarget = cam.targetTexture;
        cam.targetTexture = rt;

        // 🔴 **2026-09-21：必须先「预热」几帧再取图。**
        //    症状：**同一条命令连着跑几次，出来的图不一样** —— 实测同一场景同一个 arena1
        //    分别得到 `mean/dark%` = **105.93/10.8**（有灰幕）、**141.96/0.0**（网格整个没画）、
        //    **98.40/12.3**（正常）三种结果。`OpenScene` 返回时**场景里的贴图/网格还没全部就绪**，
        //    立刻 `Render()` 会画出一部分、或者干脆什么都不画 —— 而**尺子就是靠这张图**，
        //    ⇒ 之前那条线上的「我们几乎没有高光 / 独立场景和对战场景不一样」很可能是**这个竞态**造出来的。
        //    做法：**连渲 `WF_WARMUP`（默认 4）帧，只用最后一帧**，并把每帧的平均亮度打出来当证据
        //    （几帧的数不收敛就说明还有别的东西没就绪，别信那张图）。
        int warmup = 4;
        int.TryParse(System.Environment.GetEnvironmentVariable("WF_WARMUP"), out warmup);
        if (warmup < 1) warmup = 4;

        // 🆕 2026-09-21：**时间位移**（`WF_TIMESHIFT=<秒>`）—— 用来验「风 / 滚 UV 是不是真的在动」。
        //    静帧看不出一张图的动效，所以灌两个时刻各渲一张、比像素：
        //      `WF_TIMESHIFT=0` 渲一张、`WF_TIMESHIFT=20` 再渲一张 ⇒ **旗子/光带的像素必须不一样**。
        //    同时灌 `_Time` 与 `_TimeParameters`（原版那批 shader 读的是 `_TimeParameters`
        //    —— 反汇编里统一是 `cb0[19].x`，见 `资料/战场13场_逐场对账_0920.md` 末的「附录 · 战场 shader 逐族算式」）。
        //    ⚠️ 这是**诊断开关**，不改产品路径；`WF_TIMESHIFT` 不设 = 原样。
        var tsEnv = System.Environment.GetEnvironmentVariable("WF_TIMESHIFT");
        if (!string.IsNullOrEmpty(tsEnv) && float.TryParse(tsEnv, out float ts))
        {
            Shader.SetGlobalVector("_TimeParameters", new Vector4(ts / 20f, ts, ts * 2f, ts * 3f));
            Shader.SetGlobalVector("_Time", new Vector4(ts / 20f, ts, ts * 2f, ts * 3f));
            Debug.Log($"[Arena] WF_TIMESHIFT={ts}（验证动效用：与另一时刻各渲一张，比像素）");
        }

        var probe = new Texture2D(W, H, TextureFormat.RGB24, false);
        for (int i = 0; i < warmup; i++)
        {
            cam.Render();
            if (i == warmup - 1) break;          // 最后一帧留着下面正式读
            RenderTexture.active = rt;
            probe.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            probe.Apply();
            RenderTexture.active = null;
            var pp = probe.GetPixels();
            double sum = 0;
            foreach (var c in pp) sum += 0.299 * c.r + 0.587 * c.g + 0.114 * c.b;
            Debug.Log($"[Arena] 预热第 {i + 1}/{warmup} 帧：mean={sum / pp.Length * 255.0:F2}");
        }
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = prevTarget;
        UnityEngine.Object.DestroyImmediate(probe);

        // ⚠️ 判据要**把所有「改了可见性」的开关都算上**：原来只认 `WF_HIDE`，
        //    于是 `WF_HIDEIDX` 那一趟照旧写 `preview_*.png`，而我去比没变的 `hide_*.png`
        //    ⇒ 得出「关掉 #22 / #21 / #7 / 三颗 Steam 全都一模一样」的**假结论**
        //    （2026-09-22 🪤 又踩一次坑 ⑧，这次是**新开关没进命名判据**）。
        var outPath = debugHoles
            ? $"{ArenaDir(scene)}/holes_{scene}.png"
            : ((!string.IsNullOrEmpty(hideArg) || !string.IsNullOrEmpty(idxArg))
                ? $"{ArenaDir(scene)}/hide_{scene}.png"
             : (!string.IsNullOrEmpty(onlyArg) ? $"{ArenaDir(scene)}/only_{scene}.png"
                                               : $"{ArenaDir(scene)}/preview_{scene}.png"));
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        // 🔴 **出图路径一律打出来** —— 「拿错文件比」这一族错（坑 ⑧）的唯一根治办法是
        //    让**每次跑都自己报出文件名**，别靠人去猜哪张是哪张。
        Debug.Log($"[Arena] 出图 → {outPath}");

        foreach (var go in hidden) if (go != null) go.SetActive(true);   // 还原（诊断不该留下副作用）
        foreach (var vc in toggled) if (vc != null) vc.active = true;    // Volume 是**共享资产**，更要还原

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

        // 🔴 **目录要在开头建**（`SaveOrReuse` 会往里写），但**清历史垃圾挪到末尾** ——
        //    判据要用的「本次落盘的合法名字」= `_builtMatNames`，得等构建跑完才知道（见 `ClearMaterialDir`）。
        Directory.CreateDirectory(MatDir(sceneName));
        _builtMatNames.Clear();

        BuildContent(null, mf);
        BuildSceneTail(mf, scene);

        ClearMaterialDir(sceneName);

        // 🔴 **2026-09-27 加：每场构建结束时显式落盘。**
        //   起因（当场 A/B 抓的，**不是推测**）：`GetOrCreateParticleMaterial` 里凡是在 `SaveOrReuse`
        //   **之后**才写进材质的属性（`SetBlend` 推出来的 `_SrcBlend/_DstBlend/_SrcBlendAlpha/_DstBlendAlpha`）
        //   在**一次跑 13 场的大构建**里**没落到 `.mat` 上**（磁盘上留的是 shader 默认值 5/10/1/10），
        //   而**单场重建同一份代码就落对了**（arena3 `PS_citclr_light.mat` 实测 `_DstBlend: 1`）。
        //   ⇒ 判为**资产落盘时机**问题（`SetDirty` 只做标记，真正写文件要在 SaveAssets / 场景保存时）。
        //   ⚠️ 这条**只在磁盘上看得出来** —— 「写完当场回读」那条断言（同函数里）是**内存**读回，它一直是绿的。
        //   判据：跑完全场之后 `grep` 一遍 `arenas/*/Materials/PS_*.mat` 的 `_DstBlend`（应当 = 推断值）。
        AssetDatabase.SaveAssets();
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
            // 🔴 **2026-09-21 修：这里原来是 `false`，是个真 bug。**
            //    Unity 的语义是「**false ⇒ 可以压成不带 alpha 的格式**」（BC1/DXT1）——
            //    那么采出来 `a` 恒为 1，**`_ALPHATEST_ON` 的裁剪永远不触发**，
            //    贴图里**全透明的区域就按它自己的 RGB 画出来**。
            //    **实测症状**（arena1 并排图）：所有用 `BattleArena1 Texture Baked`（71.6% 全透明）
            //    和 `Battle Arena 1 Background`（25.9% 全透明）的道具，外面套着一圈
            //    **亮灰矩形**（透明区的 RGB 恰好是 **(240,240,240)**，乘完环境光就是画面上那 203~207 的灰）；
            //    那个叫 `Background` 的板更把**整片天空**盖掉了 —— 这就是「我们几乎没有高光」的真因
            //    （原版 13 场都有 234~254 的高光，我们一律 197~209：**因为我们最亮的像素就是这块灰板**）。
            //    ⚠️ 原来那行注释写的「别让 Unity 用最近邻补透明区的 RGB」是**反的** ——
            //       `true` 才会做那个「把 RGB 扩进透明区」的处理，而那个处理对**裁剪型贴图正是要的**
            //       （不扩的话边缘会渗出透明区的颜色）。
            ti.alphaIsTransparency = true;
            // 别被 Unity 默认的 2048 砍掉 —— 按源图最大边取到 2 的幂（原版地板就是 4096²）
            ti.GetSourceTextureWidthAndHeight(out int srcW, out int srcH);
            int maxSide = Mathf.Max(srcW, srcH);
            ti.maxTextureSize = Mathf.Clamp(Mathf.NextPowerOfTwo(maxSide), 512, 8192);
            ti.SaveAndReimport();
            n++;
        }
        Debug.Log($"[Arena] {sceneName} 贴图导入设置已按原版实读值重设：{n} 张（无 mipmap · 不缩分辨率）");
    }

    /// <summary>原版那支太阳耀斑（`LensFlareDataSRP`）在我们工程里的资产路径。</summary>
    public const string SunFlareAssetPath = "Assets/WarpforgeArena1/flares/Sun Flare 1.asset";

    /// <summary>把原版 `Sun_Flare_1` 的 **7 个元素**建成一份**工程资产**（幂等：已存在就复用，保住 guid）。
    ///
    /// **为什么照原版建、而不是我们挑一个**：这是原版自己的做法 —— 13 个战场里有 **6 个**
    /// 挂着一个叫 `Sun flare` 的对象（URP `LensFlareComponentSRP` + `LensFlareDataSRP`），
    /// 我们**一个都没建过** ⇒ 画面上没有那个日盘（arena1 最亮处：原版 **246** / 我们 **204**，
    /// 而**两者位置逐点相同**）。数据（元素 / 贴图 / 每场组件值）全部照抄，
    /// 出处与生成方式见 **`工具/gen_sunflare_cs.py`** 头注释。
    ///
    /// ⚠️ **不需要**在 Volume 里再开什么：URP 的 `useLensFlare` 只看三件事 ——
    /// ①场景里有启用的 `LensFlareComponentSRP` ②URP 资产的 `supportDataDrivenLensFlare`
    /// （我们的 `Settings/PC_RPAsset.asset` **已经是 1**）③相机开 `renderPostProcessing`（**已经是 1**）。
    /// 判据：`PostProcessPass.cs:123`（URP 包内）。</summary>
    public static LensFlareDataSRP EnsureSunFlareAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<LensFlareDataSRP>(SunFlareAssetPath);
        if (existing != null) return existing;

        var dir = Path.GetDirectoryName(SunFlareAssetPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var so = ScriptableObject.CreateInstance<LensFlareDataSRP>();
        so.elements = SunFlareData.Elements();
        AssetDatabase.CreateAsset(so, SunFlareAssetPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Arena] 建了太阳耀斑资产 {SunFlareAssetPath}（{so.elements.Length} 个元素）");
        return so;
    }

    /// <summary>诊断：把 `Sun flare` 的**运行时实况**打出来（2026-09-21 加 —— 太阳耀斑建好后
    /// 画面上**没出现**，要判是「没注册」「被遮」还是「不在视锥里」）。
    /// 用法：`WF_ARENA=<场> ... -executeMethod ArenaBuilder.ProbeSunFlare`</summary>
    /// <summary>🔴 2026-09-22：**粒子的最小对照** —— 在**同一个批处理环境**里新建一颗参数已知的粒子系统，
    /// 推 2 秒，看它到底动没动。
    ///
    /// 为什么要它：arena1 的烟囱（清单 `#22`）实测「活了 5.7 秒却只离发射体 **0.83 单位**」，
    /// 而清单写的初速是 2~3；把 `startSpeed` **强行覆盖成 10** 也**一点没变**（跨度还是 2.3、
    /// |v| 还是 1.24）。⇒ 必须回答一个二选一：
    ///   · **A** 我们这个跑法（`Stop` → `Play` → 手推 `Simulate(dt)`）本身就不搬位置 ⇒ 尺子坏了；
    ///   · **B** 尺子是好的 ⇒ 是**我们建的那颗**有问题。
    /// 这个入口用**全是默认值 + 只改几个数**的裸 ParticleSystem 来分：它要也不动，就是 A。
    ///
    /// 跑法：`... -executeMethod ArenaBuilder.ParticleSanity -logFile -`，筛 `[PS] `。
    /// </summary>
    public static void ParticleSanity()
    {
        // 🔴 先钉死一件事：**`JsonUtility` 对 `"velocity": null` 到底给什么**。
        //    这决定了判据能不能用 `!= null`（2026-09-22：全场 34/34 都判成「有模块」，
        //    而原版实测 leviathan 只有 26/10/6/7 ⇒ 判据错了整整一轮）。
        var probe = JsonUtility.FromJson<ParticleEntry>(
            "{\"go\":\"probe\",\"velocity\":null,\"clampVelocity\":null,\"noise\":null,\"rotationOverLifetime\":null}");
        Debug.Log($"[PS] JsonUtility 探针：velocity==null? {probe.velocity == null}"
              + $" · velocity.x==null? {(probe.velocity == null ? "n/a" : (probe.velocity.x == null).ToString())}"
              + $" · clampVelocity==null? {probe.clampVelocity == null}"
              + $" · noise==null? {probe.noise == null}"
              + $" · rotationOverLifetime==null? {probe.rotationOverLifetime == null}"
              + $"\n[PS] ⇒ **结论：`JsonUtility` 把 `null` 整棵子树物化，任何 null 判断都失效**，"
              + $"判据只能是清单里的 `hasXxx` 布尔量（见 `ParticleEntry` 的说明）");

        Debug.Log("[PS] ---- 裸对照（全默认 + 只改几个数）----");
        BuildAndMeasure(null);

        // 🔴 **逐项 bisect**：把 arena1 清单 `#22`（烟囱 `Scenario/Particles/SmokeEffect`）的参数
        //    一项一项加回去，看**哪一项把 `startSpeed` 吃掉了**。一次跑完，别来回猜。
        //    实测症状：全套参数下粒子活了 5.7 秒却只离发射体 0.83 单位、|v|≈1.2，
        //    而把 `startSpeed` 覆盖成 10 也不动 ⇒ 一定是某个模块在改速度。
        string[] cases = {
            "全套(=#22)",
            "- 去掉 noise",
            "- 去掉 rotationOverLifetime",
            "- 去掉 sizeOverLifetime",
            "- 去掉 colorOverLifetime",
            "- 去掉 simulationSpeed=0.1",
            "- 去掉 scalingMode=Local",
            "- 去掉 simulationSpace=World",
            "- 去掉 shape 的 rotation/scale/radiusThickness",
            "- 去掉 prewarm",
            "- 只留 startSpeed（其余全默认）",
            "全套 + 物体摆在 #22 的位姿",
            "全套 + 只有旋转",
            "全套 + 存盘再读回来",
        };
        foreach (var c in cases)
        {
            Debug.Log($"[PS] ---- {c} ----");
            BuildAndMeasure(c);
        }

        // 🎯 最后一块拼图：**把场景里真的那一颗摊开**（克隆怎么都对，只可能是它）。
        //    逐个模块打 `enabled` —— 「哪个模块把速度吃了」只有这一张表能回答。
        Debug.Log("[PS] ---- 场景里的 #22 ----");
        EditorSceneManager.OpenScene(ScenePath(SceneName));
        foreach (var c in Object.FindObjectsByType<WarpforgeVFX.ArenaParticleIndex>(FindObjectsSortMode.None))
        {
            if (c == null || c.index != 22) continue;
            var p = c.GetComponent<ParticleSystem>();
            if (p == null) { Debug.Log("[PS]   #22 上没挂 ParticleSystem"); break; }
            DumpModules(p, "#22");
            Measure(p, "#22");
            break;
        }
    }

    const string SceneName = "battlearena1";

    static void Measure(ParticleSystem ps, string tag)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play(false);
        for (int i = 0; i < 120; i++) ps.Simulate(1f / 60f, false, false, false);
        var buf = new ParticleSystem.Particle[2000];
        int got = ps.GetParticles(buf);
        var mn = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var mx = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        float v = 0f, age = 0f;
        for (int i = 0; i < got; i++)
        { mn = Vector3.Min(mn, buf[i].position); mx = Vector3.Max(mx, buf[i].position);
          v += buf[i].velocity.magnitude; age += Mathf.Max(0f, buf[i].startLifetime - buf[i].remainingLifetime); }
        var span = mx - mn;
        Debug.Log($"[PS]   {tag} → 粒子数 {got} · 平均 |v| {(got > 0 ? v / got : 0f):F2}"
                  + $" · 跨度 {span.x:F2}×{span.y:F2}×{span.z:F2} · 平均已存活 {(got > 0 ? age / got : 0f):F2}");
    }

    /// <summary>把每一族的 `enabled` 打出来 —— 判「谁在动这颗粒子的速度」的唯一一张表。</summary>
    static void DumpModules(ParticleSystem ps, string tag)
    {
        var m = ps.main;
        Debug.Log($"[PS] {tag} 模块开关：shape={ps.shape.enabled} emission={ps.emission.enabled}"
              + $" sizeOverLifetime={ps.sizeOverLifetime.enabled} colorOverLifetime={ps.colorOverLifetime.enabled}"
              + $" velocityOverLifetime={ps.velocityOverLifetime.enabled} limitVelocity={ps.limitVelocityOverLifetime.enabled}"
              + $" force={ps.forceOverLifetime.enabled} externalForces={ps.externalForces.enabled}"
              + $" inheritVelocity={ps.inheritVelocity.enabled} noise={ps.noise.enabled}"
              + $" rotationOverLifetime={ps.rotationOverLifetime.enabled} sizeBySpeed={ps.sizeBySpeed.enabled}"
              + $" colorBySpeed={ps.colorBySpeed.enabled} collision={ps.collision.enabled}"
              + $" subEmitters={ps.subEmitters.enabled} trail={ps.trails.enabled}"
              + $" textureSheetAnimation={ps.textureSheetAnimation.enabled}"
              + $" customData={ps.customData.enabled} lights={ps.lights.enabled} lifetimeByEmitterSpeed={ps.lifetimeByEmitterSpeed.enabled}");
        Debug.Log($"[PS] {tag} main：dur={m.duration} loop={m.loop} prewarm={m.prewarm}"
              + $" startLifetime={m.startLifetime.mode}/{m.startLifetime.constantMin}~{m.startLifetime.constantMax}"
              + $" startSpeed={m.startSpeed.mode}/{m.startSpeed.constantMin}~{m.startSpeed.constantMax}"
              + $" startSize={m.startSize.mode}/{m.startSize.constantMin}~{m.startSize.constantMax}"
              + $" gravity={m.gravityModifier.mode}/{m.gravityModifier.constant}"
              + $" simSpeed={m.simulationSpeed} simSpace={m.simulationSpace} scaling={m.scalingMode}"
              + $" maxParticles={m.maxParticles} startRotation={m.startRotation.mode}/{m.startRotation.constant}");
        var lv = ps.limitVelocityOverLifetime;
        Debug.Log($"[PS] {tag} limitVelocity 详情：limit={lv.limit.mode}/{lv.limit.constantMin}~{lv.limit.constantMax}"
              + $" separateAxes={lv.separateAxes} space={lv.space} dampen={lv.dampen}"
              + $" drag={lv.drag.mode}/{lv.drag.constantMin}~{lv.drag.constantMax}");
        var vo = ps.velocityOverLifetime;
        Debug.Log($"[PS] {tag} velocityOverLifetime 详情：x={vo.x.mode}/{vo.x.constantMin}~{vo.x.constantMax}"
              + $" y={vo.y.constantMin}~{vo.y.constantMax} z={vo.z.constantMin}~{vo.z.constantMax}"
              + $" speedModifier={vo.speedModifier.mode}/{vo.speedModifier.constantMin}~{vo.speedModifier.constantMax}"
              + $" radial={vo.radial.constantMin}~{vo.radial.constantMax} space={vo.space}");
        var nz = ps.noise;
        Debug.Log($"[PS] {tag} noise 详情：strength={nz.strength.constantMin}~{nz.strength.constantMax}"
              + $" damping={nz.damping} frequency={nz.frequency} octaves={nz.octaveCount}"
              + $" scroll={nz.scrollSpeed.constantMin}~{nz.scrollSpeed.constantMax}"
              + $" positionAmount={nz.positionAmount.constantMin}~{nz.positionAmount.constantMax}");
        var shp = ps.shape;
        Debug.Log($"[PS] {tag} shape 详情：type={shp.shapeType} angle={shp.angle} radius={shp.radius}"
              + $" radiusThickness={shp.radiusThickness} position={shp.position} rotation={shp.rotation} scale={shp.scale}"
              + $" alignToDirection={shp.alignToDirection} randomDirectionAmount={shp.randomDirectionAmount}"
              + $" sphericalDirectionAmount={shp.sphericalDirectionAmount}");
    }

    /// <summary>`caseName` 为 null = 裸对照；否则按名字决定「加回哪些项」。</summary>
    static void BuildAndMeasure(string caseName)
    {
        bool all = caseName != null && caseName.StartsWith("全套");
        bool no = caseName != null && caseName.Contains("去掉");
        bool onlySpeed = caseName != null && caseName.Contains("只留 startSpeed");
        bool Use(string what) => all || (no && !caseName.Contains(what));
        if (onlySpeed) all = false;

        var go = new GameObject("SanityPS");
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 5f; main.loop = true; main.prewarm = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(2.5f);
        main.startSize     = new ParticleSystem.MinMaxCurve(1f);
        main.startColor    = Color.white;
        main.maxParticles  = 100;
        main.simulationSpeed = 1f;
        var em = ps.emission; em.rateOverTime = new ParticleSystem.MinMaxCurve(10f);
        var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = 5f; sh.radius = 0.1f;

        if (all || no)
        {
            main.duration = 10f; main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 10f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(3f, 2f);      // 清单原样：minScalar 3 / scalar 2
            main.startSize     = new ParticleSystem.MinMaxCurve(4f, 5f);
            main.maxParticles  = 1000;
            if (Use("simulationSpeed=0.1")) main.simulationSpeed = 0.1f;
            if (Use("simulationSpace=World")) main.simulationSpace = ParticleSystemSimulationSpace.World;
            if (Use("scalingMode=Local")) main.scalingMode = ParticleSystemScalingMode.Local;
            if (Use("shape 的 rotation/scale/radiusThickness"))
            { sh.position = Vector3.zero; sh.rotation = Vector3.zero; sh.scale = Vector3.one; sh.radiusThickness = 1f; }
            em.rateOverTime = new ParticleSystem.MinMaxCurve(10f);
            sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 3.679f; sh.radius = 0.0895f;
            if (Use("sizeOverLifetime"))
            {
                var sol = ps.sizeOverLifetime; sol.enabled = true;
                var c = new AnimationCurve(); c.AddKey(0f, 0.274725f); c.AddKey(1f, 1f);
                sol.size = new ParticleSystem.MinMaxCurve(2f, c);
            }
            if (Use("colorOverLifetime"))
            {
                var col = ps.colorOverLifetime; col.enabled = true;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(new Color(0.32f, 0.29f, 0.28f), 0f),
                                  new GradientColorKey(new Color(0.18f, 0.15f, 0.14f), 1f) },
                          new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f),
                                  new GradientAlphaKey(0.65f, 0.83f), new GradientAlphaKey(0f, 1f) });
                col.color = new ParticleSystem.MinMaxGradient(g);
            }
            if (Use("rotationOverLifetime"))
            {
                var rot = ps.rotationOverLifetime; rot.enabled = true;
                rot.z = new ParticleSystem.MinMaxCurve(-0.523599f, 0.523599f);
            }
            if (Use("noise"))
            {
                var nz = ps.noise; nz.enabled = true;
                nz.damping = true; nz.frequency = 0.5f; nz.octaveCount = 1;
                nz.octaveMultiplier = 0.5f; nz.octaveScale = 2f;
                nz.quality = ParticleSystemNoiseQuality.High;
                nz.scrollSpeed = new ParticleSystem.MinMaxCurve(0.2f);
                nz.strength = new ParticleSystem.MinMaxCurve(1f);
                nz.strengthY = new ParticleSystem.MinMaxCurve(1f);
                nz.strengthZ = new ParticleSystem.MinMaxCurve(1f);
                nz.positionAmount = new ParticleSystem.MinMaxCurve(1f);
                nz.rotationAmount = new ParticleSystem.MinMaxCurve(0f);
                nz.sizeAmount = new ParticleSystem.MinMaxCurve(0f);
            }
            main.startRotation = new ParticleSystem.MinMaxCurve(6.283185f, -6.283185f);
        }
        if (caseName == "全套(=#22)") DumpModules(ps, "克隆全套");
        // 把场景里 #22 的**位姿**也算进来 —— 它是唯一还没被排除的一项
        if (caseName != null && caseName.Contains("#22 的位姿"))
        { go.transform.localPosition = new Vector3(82.91f, 13.92f, 79.25f);
          go.transform.localRotation = new Quaternion(0.587f, 0.579f, 0.541f, 0.166f); }
        if (caseName != null && caseName.Contains("只有旋转"))
          go.transform.localRotation = new Quaternion(0.587f, 0.579f, 0.541f, 0.166f);
        // 🎯 剩下的嫌疑：**存盘/读回来这一趟**（场景里的对象正是这么来的）。
        //    走一遍 Prefab 往返 = 完整序列化 + 反序列化，和存 `.unity` 同一套。
        if (caseName != null && caseName.Contains("存盘再读回来"))
        {
            const string tmp = "Assets/_PSRoundTrip.prefab";
            var pf = PrefabUtility.SaveAsPrefabAsset(go, tmp);
            Object.DestroyImmediate(go);
            go = (GameObject)PrefabUtility.InstantiatePrefab(pf);
            AssetDatabase.DeleteAsset(tmp);
            ps = go.GetComponent<ParticleSystem>();
            main = ps.main;
        }

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play(false);
        for (int i = 0; i < 120; i++) ps.Simulate(1f / 60f, false, false, false);
        var buf = new ParticleSystem.Particle[2000];
        int got = ps.GetParticles(buf);
        var mn = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var mx = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        float v = 0f, age = 0f;
        for (int i = 0; i < got; i++)
        { mn = Vector3.Min(mn, buf[i].position); mx = Vector3.Max(mx, buf[i].position);
          v += buf[i].velocity.magnitude; age += Mathf.Max(0f, buf[i].startLifetime - buf[i].remainingLifetime); }
        var span = mx - mn;
        Debug.Log($"[PS]   {caseName ?? "裸对照"} → 粒子数 {got} · 平均 |v| {(got > 0 ? v / got : 0f):F2}"
                  + $" · 跨度 {span.x:F2}×{span.y:F2}×{span.z:F2} · 平均已存活 {(got > 0 ? age / got : 0f):F2}");
        Object.DestroyImmediate(go);
    }

    public static void ProbeSunFlare()
    {
        var s = ArenaFromEnv();
        if (!File.Exists(ScenePath(s))) { Debug.LogError($"[SF] 没有场景 {ScenePath(s)}"); return; }
        EditorSceneManager.OpenScene(ScenePath(s));

        var go = GameObject.Find("Sun flare");
        Debug.Log($"[SF] GameObject={(go != null)} activeInHierarchy={(go != null && go.activeInHierarchy)}");
        if (go != null)
        {
            var lf = go.GetComponent<LensFlareComponentSRP>();
            var data = lf != null ? lf.lensFlareData : null;
            Debug.Log($"[SF] component={(lf != null)} enabled={(lf != null && lf.enabled)} "
                    + $"data={(data != null ? data.name : "(null)")} "
                    + $"elements={(data != null && data.elements != null ? data.elements.Length : -1)} "
                    + $"tex0={(data != null && data.elements != null && data.elements.Length > 0 && data.elements[0].lensFlareTexture != null ? data.elements[0].lensFlareTexture.name : "(null)")} "
                    + $"intensity={(lf != null ? lf.intensity : -1f)} useOcclusion={(lf != null && lf.useOcclusion)} "
                    + $"allowOffScreen={(lf != null && lf.allowOffScreen)}");
        }

        var cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            var vp = go != null ? cam.WorldToViewportPoint(go.transform.position) : Vector3.zero;
            var uacd = cam.GetUniversalAdditionalCameraData();
            Debug.Log($"[SF] 相机 {cam.name} post={(uacd != null && uacd.renderPostProcessing)} "
                    + $"allowHDR={(cam.allowHDR ? 1 : 0)} clear={cam.clearFlags} mask={cam.cullingMask}");
            if (go != null)
                Debug.Log($"[SF] 耀斑 viewport=({vp.x:F3},{vp.y:F3},{vp.z:F3}) 距离={Vector3.Distance(cam.transform.position, go.transform.position):F1} "
                        + $"maxAttenuationDistance={go.GetComponent<LensFlareComponentSRP>()?.maxAttenuationDistance}");
        }

        var inst = LensFlareCommonSRP.Instance;
        Debug.Log($"[SF] LensFlareCommonSRP.Instance={(inst != null)} IsEmpty={(inst != null && inst.IsEmpty())} "
                + $"maxLensFlareWithOcclusion={LensFlareCommonSRP.maxLensFlareWithOcclusion} "
                + $"occlusionRT={(LensFlareCommonSRP.occlusionRT != null)}");
    }

    /// <summary>**渲到「屏幕」**再拷进 RT —— 判「游戏里到底长什么样」的唯一办法。
    ///
    /// 🔴 2026-09-21 加，起因是两条互相印证的发现：
    ///  ① **数据驱动的太阳耀斑只在 `RenderFinalPass` 里画进后备缓冲** ⇒ 渲到 RenderTexture 永远看不到；
    ///  ② **开着后处理时整幅图的高光被压**（同一场景：关掉后处理 max 255 / `hi%` 5.39，
    ///     开着只剩 201 / 0.00，而**原版开着后处理是 254.8 / 2.57**）——
    ///     已排除 Bloom / Vignette（含都关）· 默认 Volume Profile · `ColorGradingMode` · RT 格式。
    ///  两条都指向同一件事：**URP 的后处理在「渲到 RT」和「渲到屏幕」两条路上行为不同**，
    ///  而我们**所有**截图（预览 + `BattleScene.Shot`/`DumpCam`）都走的是前者。
    ///
    /// ⚠️ **必须在「不开 `-batchmode`」的编辑器里跑** —— 批处理没有可用的后备缓冲（实测**直接卡死**）。
    /// 跑法：`... -executeMethod ArenaBuilder.ShotFromScreen`（**不加 `-batchmode`**）。</summary>
    public static void ShotFromScreen()
    {
        const int W = 1280, H = 720;
        EditorSceneManager.OpenScene(ScenePath(DefaultArena));
        var cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (cam == null) { Debug.LogError("[SS] 场景里没有相机"); return; }

        cam.targetTexture = null;          // ← 关键：渲到「屏幕」（编辑器里是 Game View 的后备缓冲）
        cam.aspect = (float)W / H;
        for (int i = 0; i < 4; i++) cam.Render();   // 前几帧就当预热

        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        var cmd = new CommandBuffer { name = "ScreenGrab" };
        cmd.Blit(BuiltinRenderTextureType.CameraTarget, rt);
        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Release();

        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        Directory.CreateDirectory("d:/4/_tmp_view/screen");
        var outPath = "d:/4/_tmp_view/screen/from_screen.png";
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Debug.Log($"[SS] 已保存（**从屏幕后备缓冲抓的**）：{outPath}");
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(rt);
        EditorApplication.Exit(0);
    }

    /// <summary>诊断：把**后处理实际拿到的 Volume 栈**打出来。
    ///
    /// 为什么要它：实测「开着后处理高光被压」（纯白 255 → 中性灰 205），而 Bloom / Vignette /
    /// 默认 Profile / ColorGradingMode / RT 格式**全部排除**了 —— 那就要看**栈里到底有什么**，
    /// 而不是猜 profile 文件里写了什么（文件写了 ≠ 栈里生效，优先级/覆盖会变）。
    /// 判据：`VolumeManager.instance.Update(stack, point, 1f)` —— 与 URP 自己取栈的方式一致。
    /// 用法：`WF_ARENA=<场> ... -executeMethod ArenaBuilder.ProbePostStack`</summary>
    public static void ProbePostStack()
    {
        var s = ArenaFromEnv();
        if (!File.Exists(ScenePath(s))) { Debug.LogError($"[PS] 没有场景 {ScenePath(s)}"); return; }
        EditorSceneManager.OpenScene(ScenePath(s));

        var cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (cam == null) { Debug.LogError("[PS] 场景里没有相机"); return; }

        // ⚠️ `VolumeManager.instance.CreateStack()` **必须先让流水线跑过一次**才能调
        //    （否则 `InvalidOperationException: ... before the VolumeManager is initialized`）。
        //    所以先渲一帧到临时 RT（顺便把 shader/贴图 也预热了）。
        var warm = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = warm;
        cam.Render();
        cam.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(warm);

        var stack = VolumeManager.instance.CreateStack();
        VolumeManager.instance.Update(stack, cam.transform, new LayerMask { value = ~0 });

        // 🔴 2026-09-22：**先看运行时拿到的 profile 里到底有哪几个组件** —— 分「资产里没有」与
        //    「资产里有但栈没覆盖」两种病（实测踩过：`ColorLookup` 在文件里写着、栈里却是默认值）。
        var vol = UnityEngine.Object.FindFirstObjectByType<Volume>();
        var prof = vol != null ? vol.sharedProfile : null;
        if (prof == null) Debug.LogWarning("[PS] 🔴 场景里的 Volume 没挂 profile");
        else
        {
            var names = new System.Text.StringBuilder();
            foreach (var c in prof.components) names.Append(c == null ? "<null> " : c.GetType().Name + " ");
            Debug.Log($"[PS] 运行时 profile = `{prof.name}`（{prof.components.Count} 个组件）：{names}");
        }

        var tm = stack.GetComponent<Tonemapping>();
        Debug.Log($"[PS] Tonemapping: active={(tm != null && tm.active)} mode={(tm != null ? tm.mode.value.ToString() : "-")}");
        var ca = stack.GetComponent<ColorAdjustments>();
        Debug.Log($"[PS] ColorAdjustments: active={(ca != null && ca.active)} postExposure={(ca != null ? ca.postExposure.value : 0f)} "
                + $"contrast={(ca != null ? ca.contrast.value : 0f)} sat={(ca != null ? ca.saturation.value : 0f)} filter={(ca != null ? ca.colorFilter.value.ToString() : "-")}");
        var lg = stack.GetComponent<LiftGammaGain>();
        Debug.Log($"[PS] LiftGammaGain: active={(lg != null && lg.active)} lift={(lg != null ? lg.lift.value.ToString() : "-")} "
                + $"gamma={(lg != null ? lg.gamma.value.ToString() : "-")} gain={(lg != null ? lg.gain.value.ToString() : "-")}");
        var sm = stack.GetComponent<ShadowsMidtonesHighlights>();
        Debug.Log($"[PS] ShadowsMidtonesHighlights: active={(sm != null && sm.active)} shadows={(sm != null ? sm.shadows.value.ToString() : "-")} "
                + $"midtones={(sm != null ? sm.midtones.value.ToString() : "-")} highlights={(sm != null ? sm.highlights.value.ToString() : "-")} "
                + $"limits=({(sm != null ? sm.shadowsStart.value : 0f):F2},{(sm != null ? sm.shadowsEnd.value : 0f):F2},"
                + $"{(sm != null ? sm.highlightsStart.value : 0f):F2},{(sm != null ? sm.highlightsEnd.value : 0f):F2})");
        var st = stack.GetComponent<SplitToning>();
        Debug.Log($"[PS] SplitToning: active={(st != null && st.active)} shadows={(st != null ? st.shadows.value.ToString() : "-")} "
                + $"highlights={(st != null ? st.highlights.value.ToString() : "-")} balance={(st != null ? st.balance.value : 0f)}");
        var cl = stack.GetComponent<ColorLookup>();
        Debug.Log($"[PS] ColorLookup: active={(cl != null && cl.active)} texture={(cl != null && cl.texture.value != null ? cl.texture.value.name : "(null)")} "
                + $"contribution={(cl != null ? cl.contribution.value : -1f)}");
        var bl = stack.GetComponent<Bloom>();
        Debug.Log($"[PS] Bloom: active={(bl != null && bl.active)} intensity={(bl != null ? bl.intensity.value : -1f)} "
                + $"threshold={(bl != null ? bl.threshold.value : -1f)} scatter={(bl != null ? bl.scatter.value : -1f)}");
        var vg = stack.GetComponent<Vignette>();
        Debug.Log($"[PS] Vignette: active={(vg != null && vg.active)} intensity={(vg != null ? vg.intensity.value : -1f)} smoothness={(vg != null ? vg.smoothness.value : -1f)}");
        var cc = stack.GetComponent<ColorCurves>();
        Debug.Log($"[PS] ColorCurves: active={(cc != null && cc.active)} master={(cc != null && cc.master.value != null)}");
        var wb = stack.GetComponent<WhiteBalance>();
        Debug.Log($"[PS] WhiteBalance: active={(wb != null && wb.active)} temperature={(wb != null ? wb.temperature.value : 0f)} tint={(wb != null ? wb.tint.value : 0f)}");
        var cm = stack.GetComponent<ChannelMixer>();
        Debug.Log($"[PS] ChannelMixer: active={(cm != null && cm.active)} redOutRedIn={(cm != null ? cm.redOutRedIn.value : -1f)}");
    }

    /// <summary>把战场内容（网格 + 粒子）建进**当前场景**，返回根节点。
    /// 独立场景（`BuildInternal`）与战斗场景（`BattleScene.BuildScene`）**共用这一段** —— 判据只留一处。</summary>
    /// <summary>🔴 **判「清单里到底有没有这个模块」的唯一判据 = 这几个布尔量。**
    ///
    /// 踩过的大坑（2026-09-22，**静默、而且吃掉了全部 13 场**）：
    /// 生成器对「原版关着的模块」写的是 `"velocity": null`，而 **`JsonUtility.FromJson`
    /// 会把 `null` **整棵子树**物化** —— 实测 `velocity != null`、**连 `velocity.x != null` 都是 True**
    /// （`ArenaBuilder.ParticleSanity` 里的探针钉死的）。
    /// ⇒ **任何 null 判断都失效**：`BuildContent` 给 **34/34 颗**粒子都打开了
    /// `velocityOverLifetime` 与 `limitVelocityOverLifetime`，后者的 `limit` 落到兜底值 **1**
    /// ⇒ **把所有粒子速度钳到 1 单位/秒**。
    /// 症状：arena1 烟囱 `#22` 的粒子活了 5.7 秒却只离发射体 **0.83 单位**、`|v|≈1.13`
    /// （清单初速写的是 2~3；把它覆盖成 10 也没用 —— 被 limit 钳住了）。
    /// 铁证：建场日志原来打「velocity **34** · clampVelocity **34** · noise **34** · rotation **34**」
    /// —— 34/34 在原版不可能出现（2026-09-21 实测 leviathan 是 **26 / 10 / 6 / 7**）。
    /// ⚠️ 同一个坑在 `emissionRateCurve` 上早就踩过一次（判 `!= null` ⇒ 断言数出假绿），
    /// **这一族一律走这几个布尔量，别看 `!= null`。**
    /// 出处：`资料/战场13场_逐场对账_0920.md`。</summary>
    /// <summary>`WF_HIDEIDX=<清单下标>[,<下标>…]` 用的表 —— **下标 = 清单 `particles[]` 的下标**
    /// （跳过的对象填 `null`，所以下标永远对得上）。每次 `BuildContent` 重建。
    ///
    /// 🔴 **为什么必须有它**：建出来的粒子是**平铺**在根节点下的（每个都 `SetParent(root)`，
    /// 不还原原版 `Scenario/Particles/…` 的嵌套），而原版里有**完全重名**的对象
    /// —— arena1 有 **3 个都叫 `SmokeEffect`**（其中两个贴图不同：`smokeysteam` / `SmokePuff01`）。
    /// ⇒ `WF_HIDE`（名字/路径）**分不开**：日志会把三条路径打成一模一样，
    /// 「一次只改一个变量」这条准则直接失效（2026-09-22 踩过）。
    /// 用 `WF_HIDEIDX` 点名，配合 `工具/arena_particle_audit.py`/清单就能精确到具体那一个。</summary>
    public static readonly System.Collections.Generic.List<GameObject> BuiltParticlesByIndex
        = new System.Collections.Generic.List<GameObject>();

    public static GameObject BuildContent(Transform parent, Manifest mf)
    {
        var root = new GameObject("Warpforge_" + mf.scene);
        // 🔴 把原版那条 `ApplyAmbientColor` 的**全局量**带进场景（`Shader.SetGlobalFloat` 不随场景存盘，
        //    只在建场时设一次等于没设）—— 见 `ArenaEnvGlobal` 的说明。
        root.AddComponent<WarpforgeVFX.ArenaEnvGlobal>().ambientBlend =
            mf.defaultEnv != null ? mf.defaultEnv.ambientBlend : 0f;
        if (parent != null) root.transform.SetParent(parent, false);
        int nMesh = 0, nMeshSkip = 0, nPs = 0, nPsNoTex = 0, nPsNoTexMeshOk = 0, nPsInactive = 0, nPsNone = 0, nSol = 0, nColLife = 0,
            nMeshQuality = 0, nPsQuality = 0;
        int nMeshKw = 0;      // 🆕 2026-09-30：挂上「原版关键字」组件的网格数（旁挂 `_meshkeywords.json`）
        // 🆕 2026-09-21 下半场：VFX 那几个模块建了多少个（自检要按它比）
        int nVel = 0, nClamp = 0, nNoise = 0, nRot = 0, nSubLinked = 0;
        _emissionDropped = 0;
        // 子发射器引用的是**别的 ParticleSystem** ⇒ 全部建完才能连，先攒着
        var pendingSub = new System.Collections.Generic.List<(string go, SubEmitterData[] subs)>();
        var byName = new System.Collections.Generic.Dictionary<string, ParticleSystem>();

        // 贴图导入设置：**必须在这里做**（两处调用者都走 BuildContent）——
        // 分辨率上限不对的话，地板会被 Unity 默认的 2048 砍成一半（见方法头）。
        ApplyTextureImportSettings(mf.scene);

        // ---- 材质缓存 ----
        var matCache = new Dictionary<string, Material>();
        Directory.CreateDirectory(MatDir(mf.scene));

        // 🆕 2026-09-25：主贴图之外的贴图槽（旁挂表；没有就空跑）。建完报一句，别静默。
        // 🆕 2026-09-25：按画质档开关的对象（原版 `ObjectTogglerByQuality`）—— 见 LoadInactiveByQuality
        var inactiveByQuality = LoadInactiveByQuality(mf.scene);
        if (inactiveByQuality.Count > 0)
            Debug.Log($"[Arena] {mf.scene}：本档**不建**的对象 {inactiveByQuality.Count} 个（原版 `ObjectTogglerByQuality`）—— "
                    + string.Join(" / ", new List<string>(inactiveByQuality).ToArray()));
        // 🆕 2026-09-30：**网格的原版材质关键字**（旁挂，`工具/gen_arena_meshkeywords.py`）——
        //    清单 `meshes[]` 不带 `matKeywords`，所以只能走这条。判据见 `LoadMeshKeywords` 的注释。
        var meshKw = LoadMeshKeywords(mf.scene);
        // 🆕 2026-09-30 晚：**原版顶点色**（旁挂 `_vcol.json`，由 `工具/gen_arena_vertexcolors.py` 产）
        var vcStat = new MeshVertexColors.Stat();
        if (meshKw.Count == 0)
            Debug.LogWarning($"[Arena] {mf.scene}：没有网格关键字旁挂（`{mf.scene}_meshkeywords.json` 缺失或为空）"
                           + " ⇒ 网格只有我们自算的四个关键字，**原版的 `_SOFT` / `_USEDISTORT` 那族一个都不设**。"
                           + " 跑一次 `python 工具/gen_arena_meshkeywords.py` 生成。");
        else
            Debug.Log($"[Arena] {mf.scene}：原版关键字旁挂点了 {meshKw.Count} 个网格");
        var psTexSlots = new Dictionary<string, TexSlotEntry[]>();     // 🆕 2026-09-30：粒子那一支
        var texSlots = LoadTexSlots(mf.scene, psTexSlots);
        if (texSlots.Count > 0 || psTexSlots.Count > 0)
        {
            var slotNames = new List<string>(texSlots.Keys);
            Debug.Log($"[Arena] {mf.scene}：主贴图之外的贴图槽 —— 网格 {texSlots.Count} 个 · 粒子 {psTexSlots.Count} 个"
                    + (slotNames.Count > 0
                       ? $"（{string.Join(" / ", slotNames.GetRange(0, Mathf.Min(4, slotNames.Count)).ToArray())}…）"
                       : ""));
        }

        // ---- 3D 网格 ----
        if (mf.meshes != null)
        {
            foreach (var e in mf.meshes)
            {
                var goName = string.IsNullOrEmpty(e.go) ? "(unnamed)" : e.go;
                // 🆕 2026-09-25：原版按画质档关着的（`ObjectTogglerByQuality`）—— 本档不建
                if (inactiveByQuality.Contains(goName)) { nMeshQuality++; continue; }
                var holder = new GameObject(goName);
                holder.transform.SetParent(root.transform, false);
                // 🔴 `worldBaked`（静态合批那 31 个）：**网格顶点已烘在世界坐标里 ⇒ holder 保持 identity**，
                //    绝不能再 `ApplyTransform` —— 再乘一遍等于平移 + 放大两次，全批飞出画面。
                //    见 `MeshEntry.worldBaked` 的注释（判据 + 实测后果都在那儿）。
                if (!e.worldBaked) ApplyTransform(holder.transform, e.pos, e.rot, e.scale);

                if (!string.IsNullOrEmpty(e.objFile))
                {
                    var modelPath = $"{ModelDir(mf.scene)}/{e.objFile}";
                    // 🔴 多子网格（原版一个网格多个材质）走**拆文件**那条路：
                    //    实测**Unity 的 OBJ 导入器不切子网格**（原文件只有 `g` 组 ⇒ 1 个子网格；
                    //    补上 `usemtl` 之后**仍然是 1 个**）⇒ 只能按组拆成多个 OBJ，
                    //    一个子网格建一个网格对象、各配各的材质（几何无损：共用同一段顶点块与同一个 Transform）。
                    //    见生成器 `split_obj_by_group`。踩过：圣女战场 `Floor`（2 个材质，
                    //    第 0 个透明、第 1 个不透明）整个人套第 0 个 ⇒ 地板整片透掉，屏幕上是一块均匀灰。
                    // 🔴 2026-09-25：**`subFiles` 长度为 1 也要走这条路** —— 那是**静态合批**的形态
                    //    （原版每个渲染器挂 `m_StaticBatchInfo = {firstSubMesh, subMeshCount=1}`，
                    //    只画共享合并网格里的**第 N 段**；清单里 `subFiles = ["<...>__smN.obj"]`）。
                    //    改之前这里是 `Length > 1` ⇒ 那 31 个对象每个都挂了**整张** `Combined Mesh`（31 组几何）
                    //    ⇒ 全场被叠 31 遍、又是透明混合 ⇒ **整场洗白偏亮**（battlearena2 1.086，全 13 场最差）。
                    if (e.subFiles != null && e.subFiles.Length >= 1)
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
                            subGo.AddComponent<MeshFilter>().sharedMesh =
                                MeshVertexColors.ApplyIfAny(mf.scene, e.subFiles[i], subSrc.sharedMesh, goName, subPath, vcStat);
                            var subMr = subGo.AddComponent<MeshRenderer>();
                            // 🔴 **2026-09-30 补**：`subFiles` 这条分支（静态合批拆出来的）**必须与另一条对称** ——
                            //    原来这里漏了两样（① ①-l 的逐对象阴影真值 ② 网格的原版关键字），
                            //    症状是「`battlearena2` 旁挂点了 53 个网格、只挂上 20 个」（31 个静态合批对象没挂）。
                            //    ⇒ 两条分支都改，别再只改一条。
                            if (e.hasShadow) ApplyShadowFlags(subMr, e.shadowCast > 0, e.shadowReceive > 0);
                            else             ApplyShadowFlags(subMr);
                            var m = (e.subMats != null && i < e.subMats.Length)
                                  ? GetOrCreateMaterial(mf.scene, matCache, e.subMats[i], e.go)
                                  : GetOrCreateMaterial(mf.scene, matCache, e);
                            subMr.sharedMaterial = m;
                            if (meshKw.TryGetValue(goName, out var mkwSub) && mkwSub.Length > 0)
                            {
                                var kwc = subMr.gameObject.AddComponent<WarpforgeVFX.ArenaParticleKeywords>();
                                kwc.keywords = mkwSub;
                                // 🔴 **网格用并集**（`additive = true`）—— 整表替换实测把 `sororitas`
                                //    从 1.028 推到 **1.060**（透明件丢了 `_SURFACE_TYPE_TRANSPARENT` ⇒ 过曝）。
                                //    判据 → `ArenaParticleKeywords.additive` 的注释。
                                kwc.additive = true;
                                nMeshKw++;
                            }
                            // 🆕 2026-09-21：挂上「运行时用原版 shader 重建材质」的组件
                            //    （**必须在 `sharedMaterial = m` 之后** —— 它要拿这份材质里的贴图）
                            var sm = (e.subMats != null && i < e.subMats.Length) ? e.subMats[i] : null;
                            AttachOriginalMaterial(subMr, mf.scene,
                                sm != null ? sm.shader : e.shader,
                                sm != null ? sm.props : e.props,
                                sm != null ? sm.cull : e.cull,
                                sm != null ? sm.srcBlend : e.srcBlend,
                                sm != null ? sm.dstBlend : e.dstBlend,
                                sm != null ? sm.transparent : e.transparent,
                                sm != null ? sm.alphaClip : e.alphaClip,
                                sm != null ? sm.blendAuthoritative : e.blendAuthoritative,
                                sm != null ? sm.queue : -1,
                                texSlots.TryGetValue(e.go, out var eSlots) ? eSlots : null);
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
                                mfGo.AddComponent<MeshFilter>().sharedMesh =
                                    MeshVertexColors.ApplyIfAny(mf.scene, e.objFile, src.sharedMesh, goName, modelPath, vcStat);
                                var mr = mfGo.AddComponent<MeshRenderer>();
                                // 🆕 2026-09-30（①-l）：**逐对象的原版真值**优先；旁挂没覆盖到这个对象
                                //   才退回旧行为（全 Off ＋ 诊断开关 `WF_SHADOWCAST`）。
                                if (e.hasShadow) ApplyShadowFlags(mr, e.shadowCast > 0, e.shadowReceive > 0);
                                else             ApplyShadowFlags(mr);
                                // ⚠️ 单文件（Unity 导成 1 个子网格）时，清单里的 `subMats` 若有多条也没用
                                //    —— 那是「原版有多个材质但拆不出来」的残留，这里如实报一句。
                                if (e.subMats != null && e.subMats.Length > 1)
                                    Debug.LogWarning($"[Arena] 🔴 {goName}：清单给了 {e.subMats.Length} 个材质，"
                                                   + "但这个 OBJ 没拆出多个子网格 ⇒ 只用了第 0 个");
                                mr.sharedMaterial = GetOrCreateMaterial(mf.scene, matCache, e);
                                // 🆕 2026-09-30：**原版材质关键字**（清单 `meshes[]` 不带 ⇒ 走旁挂）。
                                //    ⚠️ 关键字**写不进 `.mat`**（`EnableKeyword` / `shaderKeywords=` 存盘就丢）
                                //    ⇒ 只能**在运行时给材质实例设** ⇒ 复用粒子那个组件（已放宽成任意 `Renderer`）。
                                //    挂在这里（设完材质之后）；运行时若 `ArenaOriginalMaterial` 又换了材质，
                                //    它换完会**再调一次** `Apply()`（顺序坑见那个文件里的注释）。
                                if (meshKw.TryGetValue(goName, out var mkw) && mkw.Length > 0)
                                {
                                    var kwc = mr.gameObject.AddComponent<WarpforgeVFX.ArenaParticleKeywords>();
                                    kwc.keywords = mkw;
                                    kwc.additive = true;   // 网格必须并集（实测：整表替换会把 sororitas 推到 1.060）
                                    nMeshKw++;
                                }
                                // 🆕 2026-09-21：挂上「运行时用原版 shader 重建材质」的组件（必须在设完材质之后）
                                AttachOriginalMaterial(mr, mf.scene, e.shader, e.props, e.cull, e.srcBlend, e.dstBlend,
                                                       e.transparent, e.alphaClip, e.blendAuthoritative,
                                                       (e.subMats != null && e.subMats.Length > 0) ? e.subMats[0].queue : -1,
                                                       texSlots.TryGetValue(e.go, out var eSlots2) ? eSlots2 : null);
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

        // ---- 🆕 2026-09-25：精灵（`SpriteRenderer`）--------------------------------------------
        // 必须**排在闪烁族前面** —— `AttachFlickers` 按名字找对象，这 9+9 个「假光晕」正是闪烁族的一员
        // （`FlickerData.gen.cs` 里早就有它们，只是一直没有对象可挂 ⇒ 自检每次报 `0/9`）。
        BuildSprites(mf.scene, root);

        // 🆕 2026-09-25：平面反射（原版那台 `reflection camera`）—— 地板采 `_ReflectionMap` 靠它
        BuildMirror(mf.scene, root);

        // ---- 闪烁族（原版 `MaterialFlickerEffect`）--------------------------------------------
        // 🆕 2026-09-22：13 场里 **38 个**对象挂着它（火把 / 地面 / 塔楼 / 木桶 / 发电机自发光），
        //    生成器从来没抽过 ⇒ 那些光**永远是死的**，包括 arena1 那两块「烘焙光斑」。
        //    数据表 = `Editor/FlickerData.gen.cs`（生成器 `工具/gen_flicker_cs.py`，逐条来自原版场景）；
        //    算式与实况判据 = `资料/战场13场_逐场对账_0920.md` §一 ①-a。
        {
            int nFlick = AttachFlickers(root.transform, mf);
            Debug.Log($"[Arena] 闪烁族：本场挂上 {nFlick} 个（原版 `MaterialFlickerEffect`）");
        }

        // ---- 粒子特效 ----
        if (mf.particles != null)
        {
            BuiltParticlesByIndex.Clear();
            for (int pi = 0; pi < mf.particles.Length; pi++)
            {
                var p = mf.particles[pi];
                // 下标与清单 `particles[]` **一一对应**（跳过的填 null）—— `WF_HIDEIDX` 靠它点名，
                // 见 `BuiltParticlesByIndex` 的说明。
                BuiltParticlesByIndex.Add(null);
                // 🔴 2026-09-20：**原版材质没有贴图的粒子，一律不建**。
                //    这些是**扭曲 / 叠加辉光**类（`Heat Distortion` · `Muzzle Flash view distort` ·
                //    `Light` · `Necrons Close Monolith Rays` · `Lance Fire`），原版靠自己的
                //    screen-grab / additive shader 出效果，**材质里本来就没有贴图**。
                //    我们原来给它们建了 `URP/Particles/Unlit` + 空贴图 ⇒ **渲成一坨不透明白方块**
                //    （blacklegion 那 4 条横白板就是这么来的，和原版真渲图一比就露）。
                //    **宁可不建、也不画错的**（本项目的「不许静默失败」）—— 但**必须报出来**，见下面的汇总。
                //
                // 🆕 2026-09-28 **给 `renderMode = 4 (Mesh)` 开一个豁免口**（判据见下）：
                //    这道闸门比 Mesh 那条分支**早**，而 **Mesh 模式靠网格 + `matShader` 出效果、不靠贴图**
                //    ⇒ 原来它把 `arena3` 的 `Necrons Close Monolith Rays` / `…(1)` **整族吃掉**了：
                //    清单里 `active=true`、`renderMode=4`、`mesh="Sphere.obj"`（网格**抽得出来**、
                //    `Models/Sphere.obj` 也在），可场景里 `Close Monolith` 出现 **0 次** —— 一笔挂了很久的老账。
                //    ⚠️ 豁免**只给「网格真的解析得到」的那些**（`ParticleMesh` 返回 null 照旧不建并出声）。
                bool meshModeOk = NoTexMeshModeOk(p, mf.scene);
                if (string.IsNullOrEmpty(p.texFile) && !meshModeOk)
                {
                    nPsNoTex++;
                    continue;
                }
                if (string.IsNullOrEmpty(p.texFile) && meshModeOk) nPsNoTexMeshOk++;
                // 🆕 2026-09-21：**原版关着的，不要建**（判据 = 生成器沿 `m_Father` 逐层与 `m_IsActive`）。
                //    实测受害：arena3 的 `TorchEffectNecron/Fire/Light` ×2（淡绿 0.769/1.0/0.808、prewarm）
                //    —— 原版 `activeInHierarchy=False`，我们开着 ⇒ 一开场两个绿球挂在火把上。
                if (!p.active)
                {
                    nPsInactive++;
                    continue;
                }
                // 🆕 2026-09-25：原版按画质档关着的（`ObjectTogglerByQuality`）—— 本档不建
                if (inactiveByQuality.Contains(p.go))
                {
                    nPsQuality++;
                    continue;
                }
                // 🆕 2026-09-21：`renderMode = 5 (None)` = **原版根本不画这个对象**
                //    （实例：leviathan 的 `Smoke_trail_2`）。原来 `Mathf.Clamp(...,0,4)` 把它
                //    变成 Billboard ⇒ 多画一个。
                if (p.renderMode == 5)
                {
                    nPsNone++;
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
                // 🔴 2026-09-22：`simulationSpeed` —— **原版 19/34 个对象 ≠ 1.0**
                //    （烟囱 `Scenario/Particles/SmokeEffect` = **0.1**、`Bullets Controller` = 4.41、
                //    `Dust Floor` 一族 = 0.5）。原来吃 Unity 默认 1.0 ⇒ 飘移/曲线推进/相位全不对。
                if (p.simulationSpeed > 0f) main.simulationSpeed = p.simulationSpeed;
                main.startDelay      = CurveFrom(p.startDelay, new ParticleSystem.MinMaxCurve(0f));
                main.startLifetime   = Curve(p.startLifetime, 1f);
                main.startSpeed      = Curve(p.startSpeed, 1f);
                main.startSize       = Curve(p.startSize, 1f);
                // 🔴 2026-09-22：**`startRotation`（单位是弧度）+ 每颗粒子的随机朝向** ——
                //    「烟是一坨 / 蒸汽是实心球」的头号嫌疑：原版 `Steam`/`Dust Floor`/烟囱 `SmokeEffect`
                //    都是 `TwoConstants(0, 2π)` + `randomizeRotationDirection=0.5`，
                //    我们原来一颗都没设 ⇒ **所有烟贴片朝向完全相同、叠成一坨**。
                //    判据 = 原版 JSON（`arena_particle_audit.py` 摊出来逐字段可复查）。
                if (p.rotation3D)
                {
                    main.startRotation3D = true;
                    main.startRotationX = CurveFrom(p.startRotationX, new ParticleSystem.MinMaxCurve(0f));
                    main.startRotationY = CurveFrom(p.startRotationY, new ParticleSystem.MinMaxCurve(0f));
                    main.startRotationZ = CurveFrom(p.startRotationZ, new ParticleSystem.MinMaxCurve(0f));
                }
                else
                {
                    main.startRotation = CurveFrom(p.startRotation, new ParticleSystem.MinMaxCurve(0f));
                }
                main.randomizeRotationDirection = Mathf.Clamp01(p.randomizeRotationDirection);
                // 🆕 2026-09-22：`size3D`（原版 `Droppods/Glow` 开了，Y=3.5）—— 不开就三轴同尺寸
                if (p.size3D)
                {
                    main.startSize3D = true;
                    main.startSizeY = CurveFrom(p.startSizeY, new ParticleSystem.MinMaxCurve(1f));
                    main.startSizeZ = CurveFrom(p.startSizeZ, new ParticleSystem.MinMaxCurve(1f));
                }
                main.startColor      = ToColor(p.startColor);
                // 🆕 2026-09-21：曲线态优先（原版 Fire Right 是 0→−0.662×0.05，不是常数 +0.05）
                main.gravityModifier = CurveFrom(p.gravityCurve, new ParticleSystem.MinMaxCurve(p.gravityModifier));
                main.maxParticles    = Mathf.Max(1, p.maxParticles);
                main.simulationSpace = (ParticleSystemSimulationSpace)Mathf.Clamp(p.simulationSpace, 0, 2);
                // 🆕 `scalingMode` 决定父物体 scale 影不影响粒子大小（原版 leviathan 4 个 Hierarchy / 3 个 Shape）
                main.scalingMode     = (ParticleSystemScalingMode)Mathf.Clamp(p.scalingMode, 0, 2);

                var em = ps.emission;
                // 🆕 2026-09-21：曲线态优先 —— 原版多数是「只在循环前 ~2.5s 发射」，拍成峰值常数会多 2~3 倍粒子
                em.rateOverTime = CurveFrom(p.emissionRateCurve, new ParticleSystem.MinMaxCurve(Mathf.Max(0f, p.emissionRate)));
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
                // 🔴 2026-09-22：ShapeModule 的**位置 / 欧拉角 / 缩放 / 边缘厚度**（原来一个都没设）。
                //    判据（原版 JSON，`工具/arena_particle_audit.py` 可复查）：
                //    · `m_Rotation` —— `Embers` 一族与 `WildFire` 都是 `(-90, 0, 0)`
                //      ⇒ **发射方向差 90°**（例如本该水平喷的火，我们朝天喷）；
                //    · `m_Position` —— `WildFire` 是 `(0, -0.53, 0)`；
                //    · `m_Scale` —— `RisingSteam` z=0.5 · `TinyFlames` (0.8, 0.44, 1.0) · `WildFire` z=0.6
                //      （**兜底必须是 (1,1,1)** —— 原版 25/34 就是默认值，写 0 会把形状缩成一点）；
                //    · `radiusThickness` —— `WildFire` 是 **0.0**（只从边缘发射），其余 1.0。
                sh.position = ToVec3(p.shapePos, Vector3.zero);
                sh.rotation = ToVec3(p.shapeRot, Vector3.zero);
                sh.scale    = ToVec3(p.shapeScale, Vector3.one);
                sh.radiusThickness = Mathf.Clamp01(p.shapeRadiusThickness);
                // Unity 的 ShapeModule.arc 单位是「度」，而清单里存的是 Unity 原始的「弧度」值。
                // 用数值大小兜底：<= 2π+ε 当弧度转，否则认为已经是度。
                if (p.shapeArc > 0f)
                {
                    float arcDeg = p.shapeArc <= (Mathf.PI * 2f + 0.01f)
                                 ? p.shapeArc * Mathf.Rad2Deg
                                 : p.shapeArc;
                    sh.arc = Mathf.Clamp(arcDeg, 0f, 360f);
                }

                // 🆕 2026-09-21：**sizeOverLifetime**（原来完全没建）—— 原版用它做「随生命长大」：
                //    leviathan 的 `Gas`/`Fume Burst` 从 0.23 长到 1.0，`Green Vapours` 的 X 另有 0.5 乘数。
                //    不建 ⇒ 全程满尺寸，平均大 2~4 倍（对着原版真渲图一眼能看出来）。
                if (HasSizeCurve(p.sizeOverLifetime))
                {
                    var sol = ps.sizeOverLifetime;
                    sol.enabled = true;
                    sol.separateAxes = p.sizeOverLifetime.separateAxes;
                    if (p.sizeOverLifetime.separateAxes)
                    {
                        sol.x = CurveFrom(p.sizeOverLifetime.x, new ParticleSystem.MinMaxCurve(1f));
                        sol.y = CurveFrom(p.sizeOverLifetime.y, new ParticleSystem.MinMaxCurve(1f));
                        sol.z = CurveFrom(p.sizeOverLifetime.z, new ParticleSystem.MinMaxCurve(1f));
                    }
                    else
                    {
                        sol.size = CurveFrom(p.sizeOverLifetime.x, new ParticleSystem.MinMaxCurve(1f));
                    }
                    nSol++;
                }
                // 🆕 2026-09-21：**colorOverLifetime**（原来完全没建）—— 出生淡入 / 死亡淡出。
                //    下半场改成**完整 RGBA**（原来只建 alpha）⇒ 火焰的「白→黄→橙→烟」这才对。
                var grad = GradientFrom(p.colorOverLifetime);
                if (grad != null)
                {
                    var col = ps.colorOverLifetime;
                    col.enabled = true;
                    col.color = new ParticleSystem.MinMaxGradient(grad);
                    nColLife++;
                }

                // 🆕 2026-09-21 下半场：**VFX 那 5 个模块**（原来一个都没建）。
                //    判据（原版 leviathan 71 个 ParticleSystem 里开着的个数）：Velocity 26 ·
                //    ClampVelocity 10 · SubEmitter 7 · Noise 6 · Rotation 7。
                //    不建的后果肉眼可见：**烟不飘（一坨浓白）· 火没有火星 · 雾不扭**。
                // 🔴 2026-09-22：**先把所有可选模块显式关掉，再按清单开** ——
                //    `p.xxx != null` 是**恒真**的（`JsonUtility` 把清单的 `null` 物化成空对象），
                //    见 `HasVel` 那一族的说明。这里再加一道「关干净」的保险，并把
                //    「清单里没有、却还开着」的模块直接归零（`limit=1` 那种兜底值会静默改行为）。
                // ⚠️ 这些模块的 getter 返回的是**结构体**，不能直接 `ps.x.enabled = …`（CS1612）
                //    —— 一律先接进局部变量再改。
                var voOff = ps.velocityOverLifetime; voOff.enabled = false;
                var lvOff = ps.limitVelocityOverLifetime; lvOff.enabled = false;
                var nzOff = ps.noise; nzOff.enabled = false;
                var roOff = ps.rotationOverLifetime; roOff.enabled = false;
                var foOff = ps.forceOverLifetime; foOff.enabled = false;
                var efOff = ps.externalForces; efOff.enabled = false;
                var ivOff = ps.inheritVelocity; ivOff.enabled = false;
                var sbsOff = ps.sizeBySpeed; sbsOff.enabled = false;
                var cbsOff = ps.colorBySpeed; cbsOff.enabled = false;
                var clOff = ps.collision; clOff.enabled = false;
                var trOff = ps.trails; trOff.enabled = false;
                var ltOff = ps.lights; ltOff.enabled = false;
                var cdOff = ps.customData; cdOff.enabled = false;
                var lbOff = ps.lifetimeByEmitterSpeed; lbOff.enabled = false;

                if (p.hasVelocity)
                {
                    var v = ps.velocityOverLifetime;
                    v.enabled = true;
                    v.space = p.velocity.inWorldSpace
                            ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
                    v.x = CurveFrom(p.velocity.x, new ParticleSystem.MinMaxCurve(0f));
                    v.y = CurveFrom(p.velocity.y, new ParticleSystem.MinMaxCurve(0f));
                    v.z = CurveFrom(p.velocity.z, new ParticleSystem.MinMaxCurve(0f));
                    v.radial = CurveFrom(p.velocity.radial, new ParticleSystem.MinMaxCurve(0f));
                    v.speedModifier = CurveFrom(p.velocity.speedModifier, new ParticleSystem.MinMaxCurve(1f));
                    v.orbitalX = CurveFrom(p.velocity.orbitalX, new ParticleSystem.MinMaxCurve(0f));
                    v.orbitalY = CurveFrom(p.velocity.orbitalY, new ParticleSystem.MinMaxCurve(0f));
                    v.orbitalZ = CurveFrom(p.velocity.orbitalZ, new ParticleSystem.MinMaxCurve(0f));
                    v.orbitalOffsetX = CurveFrom(p.velocity.orbitalOffsetX, new ParticleSystem.MinMaxCurve(0f));
                    v.orbitalOffsetY = CurveFrom(p.velocity.orbitalOffsetY, new ParticleSystem.MinMaxCurve(0f));
                    v.orbitalOffsetZ = CurveFrom(p.velocity.orbitalOffsetZ, new ParticleSystem.MinMaxCurve(0f));
                    nVel++;
                }
                if (p.hasClampVelocity)
                {
                    var cv = ps.limitVelocityOverLifetime;
                    cv.enabled = true;
                    cv.separateAxes = p.clampVelocity.separateAxis;
                    cv.space = p.clampVelocity.inWorldSpace
                             ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
                    cv.dampen = p.clampVelocity.dampen;   // ⚠️ float，不是 MinMaxCurve
                    cv.drag = CurveFrom(p.clampVelocity.drag, new ParticleSystem.MinMaxCurve(0f));
                    cv.multiplyDragByParticleSize = p.clampVelocity.multiplyDragByParticleSize;
                    cv.multiplyDragByParticleVelocity = p.clampVelocity.multiplyDragByParticleVelocity;
                    cv.limit = CurveFrom(p.clampVelocity.magnitude, new ParticleSystem.MinMaxCurve(1f));
                    cv.limitX = CurveFrom(p.clampVelocity.x, new ParticleSystem.MinMaxCurve(1f));
                    cv.limitY = CurveFrom(p.clampVelocity.y, new ParticleSystem.MinMaxCurve(1f));
                    cv.limitZ = CurveFrom(p.clampVelocity.z, new ParticleSystem.MinMaxCurve(1f));
                    nClamp++;
                }
                if (p.hasNoise)
                {
                    var nz = ps.noise;
                    nz.enabled = true;
                    nz.separateAxes = p.noise.separateAxes;
                    nz.damping = p.noise.damping;
                    nz.remapEnabled = p.noise.remapEnabled;
                    nz.frequency = p.noise.frequency;   // ⚠️ float，不是 MinMaxCurve
                    nz.octaveCount = Mathf.Clamp(p.noise.octaves, 1, 4);
                    nz.octaveMultiplier = p.noise.octaveMultiplier;
                    nz.octaveScale = p.noise.octaveScale;
                    nz.quality = (ParticleSystemNoiseQuality)Mathf.Clamp(p.noise.quality, 0, 2);
                    nz.scrollSpeed = CurveFrom(p.noise.scrollSpeed, new ParticleSystem.MinMaxCurve(0f));
                    nz.strength = CurveFrom(p.noise.strength, new ParticleSystem.MinMaxCurve(1f));
                    nz.strengthY = CurveFrom(p.noise.strengthY, new ParticleSystem.MinMaxCurve(1f));
                    nz.strengthZ = CurveFrom(p.noise.strengthZ, new ParticleSystem.MinMaxCurve(1f));
                    nz.positionAmount = CurveFrom(p.noise.positionAmount, new ParticleSystem.MinMaxCurve(1f));
                    nz.rotationAmount = CurveFrom(p.noise.rotationAmount, new ParticleSystem.MinMaxCurve(0f));
                    nz.sizeAmount = CurveFrom(p.noise.sizeAmount, new ParticleSystem.MinMaxCurve(0f));
                    nNoise++;
                }
                if (p.hasRotation)
                {
                    var rot = ps.rotationOverLifetime;
                    rot.enabled = true;
                    rot.separateAxes = p.rotationOverLifetime.separateAxes;
                    // ⚠️ `separateAxes=false` 时 Z 是「绕 Z 轴转」那一个（清单里存在 z）
                    rot.z = CurveFrom(p.rotationOverLifetime.z, new ParticleSystem.MinMaxCurve(0f));
                    rot.x = CurveFrom(p.rotationOverLifetime.x, new ParticleSystem.MinMaxCurve(0f));
                    rot.y = CurveFrom(p.rotationOverLifetime.y, new ParticleSystem.MinMaxCurve(0f));
                    nRot++;
                }
                // 子发射器**要等全部粒子建完再连**（它引用的是别的 ParticleSystem）⇒ 记下来，
                // 循环结束后统一 `WireSubEmitters()`。这里只落名。
                if (p.hasSubEmitters && p.subEmitters != null && p.subEmitters.Length > 0)
                    pendingSub.Add((p.go, p.subEmitters));

                // 翻页图集：按 UVModule 的网格切分，否则整张精灵图集会被贴在每个粒子上
                // 🆕 2026-09-24：条件提到这里 —— 材质那边也要用同一个判据开 `_FLIPBOOKBLENDING_ON`
                //   （原版 `SmokeySteam01.mat` 带这个关键字 + `_FlipbookBlending: 1`，我们原来是 OFF）。
                bool flipbook = p.uvEnabled && p.tilesX > 0 && p.tilesY > 0
                                && (p.tilesX > 1 || p.tilesY > 1);
                if (flipbook)
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
                // 只处理公告板家族（0=Billboard 1=Stretch 2=Horizontal 3=Vertical）。
                // 🔴 2026-09-25：**`4 = Mesh` 现在真的接网格了** —— 原来一律降级成 Billboard
                //    （当时的理由「清单里没带网格」已不成立：`mesh` 字段由
                //    `工具/gen_arena_psmesh.py` + 生成器补齐，网格在 `Models/<mesh>`）。
                //    实测规模 **19 颗 / 4 场**（tauviorla 10 · blacklegion 6 · arena3 2 · darkangels 1），
                //    其中 tauviorla（1.033）与 blacklegion（1.076）都还在 >±5% 名单里。
                //    ⚠️ 清单没给 `mesh` 时**照旧降级**（如实，不静默画错）。
                int rm = Mathf.Clamp(p.renderMode, 0, 4);
                var psMesh = ParticleMesh(p.mesh, mf.scene);
                if (rm == 4 && psMesh != null)
                {
                    rend.renderMode = ParticleSystemRenderMode.Mesh;
                    rend.mesh = psMesh;
                }
                else
                {
                    if (rm == 4)
                        Debug.LogWarning($"[Arena] {p.go}：`renderMode=4 (Mesh)` 但清单没给网格"
                                       + $"（mesh=\"{p.mesh}\"）⇒ 退回 Billboard。跑 `工具/gen_arena_psmesh.py` 补上。");
                    rend.renderMode = rm == 4 ? ParticleSystemRenderMode.Billboard
                                              : (ParticleSystemRenderMode)rm;
                }
                // 🆕 2026-09-21：这几条原来都没设，全吃 Unity 默认值 ——
                //    `maxParticleSize`（默认 0.5，原版多为 20 = 不限幅 ⇒ 大粒子被裁）、
                //    `renderAlignment`（默认 View，原版 sororitas Fire/Glow 是 World）、
                //    `lengthScale`（Stretch 模式的拉伸长度，原版 5.0 vs 默认 2.0）、`sortingFudge`。
                if (p.renderAlignment != 0) rend.alignment = (ParticleSystemRenderSpace)p.renderAlignment;
                // 🔴 **`maxParticleSize` 故意不设**（2026-09-21 实测，别当漏了）：
                //    原版这几个字段是 12 / 20（Unity 默认 0.5），照抄会把画面**显著改亮** ——
                //    `battlearena3` 实测：设了之后整场亮度比 **1.032 → 1.181**，
                //    而把受影响的那 5 颗（`Stuff` / `Stuff (1)` / `Chasm glow` / `Scarab swarm 1/2`）
                //    `WF_HIDE` 掉，比值**正好回到 1.037** ⇒ **退步全出在这一个字段上**。
                //    ⇒ 字段语义还没吃透（Unity 的 `maxParticleSize` 到底是「裁掉超限的」还是
                //    「把超限的缩到上限」，两种读法结论相反），**按铁律 4「实况与字段冲突以实况为准」：
                //    维持 Unity 默认 0.5**。数据仍在清单里（`maxParticleSize`），吃透了再开。
                // ⚠️ 另注：`renderAlignment` / `lengthScale` / `sortingFudge` **照原版设了**，
                //    本次 A/B 里它们没有被牵进来（把上面那 5 颗关掉就够了）。
                if (p.minParticleSize > 0f) rend.minParticleSize = p.minParticleSize;
                if (p.lengthScale > 0f)     rend.lengthScale = p.lengthScale;
                if (p.sortingFudge != 0f)   rend.sortingFudge = p.sortingFudge;
                rend.material   = GetOrCreateParticleMaterial(mf.scene, p);
                rend.shadowCastingMode = ShadowCastingMode.Off;
                rend.receiveShadows = false;

                ps.Play();
                BuiltParticlesByIndex[pi] = go;
                // 🆕 2026-09-22：把**清单下标**刻在对象上 —— 保存进场景后 `RenderPreview`
                //    （它走 `OpenScene`，不跑 `BuildContent`）也还能按下标点名，见
                //   `ArenaParticleIndex` 的说明与 `WF_HIDEIDX`。
                go.AddComponent<WarpforgeVFX.ArenaParticleIndex>().index = pi;
                // 🆕 2026-09-24：**原版关键字挂在场景组件上、运行时补** ——
                //   `_EMISSION` 这类关键字写不进 `.mat`（见 `ArenaParticleKeywords` 与 `资料/已知的坑.md`），
                //   但**组件字段是能序列化进场景的** ⇒ 运行时给材质【实例】设，绕开落盘。
                if (p.matKeywords != null && p.matKeywords.Length > 0)
                    go.AddComponent<WarpforgeVFX.ArenaParticleKeywords>().keywords = p.matKeywords;
                // 🆕 **2026-09-30（⑨）：粒子也交给「运行时原版材质重建」** —— 建场期只能用兜底
                //   `URP/Particles/Unlit`（原版 shader 落不了工程资产），而原版那批材质的关键字/贴图槽
                //   （`_NOISE1CHANNEL_R` / `_NOISE2CHANNEL_R` 那族）在兜底 shader 上**根本不存在**
                //   ⇒ 设了等于没设（探针实读：关键字设上了、shader 没那两个槽）。
                //   理论修法 = 运行时用 bundle 里的原版 shader 重建（不落盘，没有那个限制）。
                //
                // ✅ **2026-09-30 当天：第一版 A/B 变差 → 查到真根因 → 已修 → 已铺开（默认开）**
                //   （原来这段写的是「A/B 实测变差、默认关着」—— 那是**修之前**的状态，已作废。）
                //
                //   🔴 **真根因 = 主贴图槽名不同 ⇒ 贴图整张丢掉**：兜底 shader 认 `_BaseMap`，
                //   而原版 `Everguild/FX/Alpha Mask One Layer  Color Ramp` **只有 `_MainTex`**，
                //   运行时搬贴图那一步（`ArenaOriginalMaterial.CopyCommon`）是**按名字逐项搬**的
                //   ⇒ 换上去的原版 shader 采样它自己的**默认白**贴图 ⇒ `darkangels` 的 **#9
                //   `Generator Emit`**（startSize 100、加法混合的大网格）**爆成一大块亮青**
                //   （青 = 该材质自己的 `_Main_Color`）。
                //   修法 = **`ArenaOriginalMaterial.BridgeMainTexture`（主贴图桥，网格/粒子共用一处）**。
                //
                //   实测（`WF_PSFIXSEED=1`，罐子区 x0-200/y100-300 平均亮度）：
                //     改前 **65.399** → 第一版 **71.679（+6.28）** → 修完 **65.411**；
                //     `WF_HIDEIDX=9` 单独关掉 #9 后回到 **65.399（与改前逐位相同）** ⇒ 增量 100% 出在这一颗。
                //   对原版真渲图（`资料/原版实拍/arena_0920/shot_Battle_Arena_Dark_Angels.png`）：
                //     罐子区 +11.40 → +11.42 · 烟那条带 **+7.99 → +5.59** · 全图 |差| 平均绝对差
                //     **14.991 → 14.772**（每一项都不比改前差，多数变好）⇒ **铺开**。
                //   判据与全过程 → **`资料/战场场景线_交接.md` §二 ⑨** 与 `资料/战场13场_逐场对账_0920.md` §一 ①-m。
                //
                //   ⏭ **同族还开着一条**：**粒子的非主贴图槽没接**（例：`Glow Charge Lines` 的
                //     `_SecondaryTex → NoiseContrast`）—— 旁挂 `<场>_texslots.json` 现在只有 `meshes` 键。
                //
                // ⚠️ **`WF_ORIGPART=0` 是回退开关**（默认**开**）：只给 A/B 用，不是常态。
                if (System.Environment.GetEnvironmentVariable("WF_ORIGPART") != "0")
                    AttachOriginalMaterial(rend, mf.scene, p.matShader, p.matProps,
                                           (int)PropF(p.matProps, "_Cull", 2f),
                                           (int)PropF(p.matProps, "_SrcBlend", 5f),
                                           (int)PropF(p.matProps, "_DstBlend", 10f),
                                           PropF(p.matProps, "_Surface", 0f) > 0.5f,
                                           PropF(p.matProps, "_AlphaClip", 0f) > 0.5f,
                                           true, p.matQueue,
                                           // 🆕 2026-09-30：**粒子的非主贴图槽**（`_EmissionMap` / `_SecondaryTex` /
                                           //   `_DistortTex` / `_NoiseTex1,2` / `_BumpMap` / `_MinTex` / `_Mask`）——
                                           //   以前粒子用的是兜底 shader、这些槽在上面根本不存在 ⇒ 不接也看不出来；
                                           //   ⑨ 之后改用原版 shader ⇒ 不接就采样默认图（多为白）。
                                           //   13 场普查 40 个槽，一次性接上（生成器 → `<场>_texslots.json` 的 `particles` 键）。
                                           psTexSlots.TryGetValue(p.go, out var pSlots) ? pSlots : null);
                // 记进「按名字找」的表 —— 子发射器要靠名字连（同名的多个只留最后一个，
                // 这是清单能给的极限；同族对象参数本来就一致）
                byName[go.name] = ps;
                nPs++;
            }
        }

        // 🆕 2026-09-21 下半场：**连子发射器**（必须等全部粒子建完 —— 它引用的是**别的** ParticleSystem）。
        //    连不上的**要报出来**（不许静默失败）：原版 7 个对象有它。
        foreach (var (pgo, subs) in pendingSub)
        {
            ParticleSystem host;
            if (!byName.TryGetValue(pgo, out host) || host == null)
            {
                Debug.LogWarning($"[Arena] 🔴 子发射器宿主 `{pgo}` 没建出来 —— 这组子发射器没连上");
                continue;
            }
            var se = host.subEmitters;
            for (int i = 0; i < subs.Length; i++)
            {
                ParticleSystem target;
                if (!byName.TryGetValue(subs[i].target, out target) || target == null)
                {
                    Debug.LogWarning($"[Arena] 🔴 子发射器目标 `{subs[i].target}`（宿主 `{pgo}`）不在本场 —— 跳过");
                    continue;
                }
                if (i == 0) se.enabled = true;
                se.AddSubEmitter(target, (ParticleSystemSubEmitterType)Mathf.Clamp(subs[i].type, 0, 4),
                                 (ParticleSystemSubEmitterProperties)0,
                                 Mathf.Clamp01(subs[i].emitProbability));
                nSubLinked++;
            }
        }

        if (nPsNoTex > 0)
            Debug.LogWarning($"[Arena] 🔴 {mf.scene}：**{nPsNoTex} 个粒子没建** —— 它们的原版材质没有贴图"
                           + "（扭曲/叠加辉光类，靠 screen-grab / additive shader 出效果）。"
                           + "硬建会渲成不透明白方块 ⇒ 宁可不建。**这是已知缺口**，不是「做完了」："
                           + "要还原得给它们接对应的原版 shader（见 `资料/普查产出_0920/场景光照与后处理_原版规格.md` §十一）。");
        // 🆕 2026-09-28：无贴图但**走 Mesh 模式**、因而被放行建出来的那批（`arena3` 的 `Close Monolith Rays` 一族）。
        //   这条**要出声**：它是「原版靠网格 + shader 出效果」的正当路径，不是缺口。
        if (nPsNoTexMeshOk > 0)
            Debug.Log($"[Arena] {mf.scene}：**{nPsNoTexMeshOk} 个粒子没有贴图但走 `renderMode = 4 (Mesh)`**"
                    + "（网格抽得到）⇒ 按 Mesh 模式建出来（豁免口的判据写在「无贴图不建」那段注释里）。");

        // 🆕 2026-09-30 晚：**原版顶点色**的落地情况（**一个都不许静默** —— 贴不上要报出来）
        if (vcStat.colored > 0 || vcStat.skipNoRead > 0 || vcStat.missVert > 0)
            Debug.Log($"[Arena] {mf.scene}：原版顶点色 → {vcStat}");
        if (vcStat.skipNoRead > 0 || vcStat.missVert > 0)
            Debug.LogWarning($"[Arena] 🔴 {mf.scene}：顶点色没贴全（{vcStat}）—— "
                           + "判据与旁挂在 `工具/gen_arena_vertexcolors.py` / `arenas/{场}/{场}_vcol.json`");

        Debug.Log($"[Arena] 内容：网格 {nMesh} 个（跳过 {nMeshSkip} · 按画质档不建 {nMeshQuality} · "
                + $"挂原版关键字 {nMeshKw} 个）、粒子 {nPs} 个"
                + $"（另跳过无贴图 {nPsNoTex} · 原版关着 {nPsInactive} · renderMode=None {nPsNone} 个）；"
                + $"其中 sizeOverLifetime {nSol} · colorOverLifetime(RGBA) {nColLife} · "
                + $"velocity {nVel} · clampVelocity {nClamp} · noise {nNoise} · rotation {nRot} · 子发射器 {nSubLinked}");
        // 🔴 自发光丢掉的那批**必须报出来**（原版有值、我们设不上去 —— 见 `_emissionDropped` 的说明）
        if (_emissionDropped > 0)
            Debug.LogWarning($"[Arena] 🔴 {mf.scene}：**{_emissionDropped} 个材质的自发光没设上去** —— "
                           + "我们用的 `URP/Unlit` 属性表里没有 `_EmissionColor`/`_EmissiveColor`"
                           + "（原版 `UnlitAmbient Emissive Flickker` 用的是 `_EmissiveColor`）。"
                           + "**这是已知缺口**，不是「做完了」：要还原得让这些材质走一个带自发光项的 shader。");
        _emissionDropped = 0;

        // ---- 太阳耀斑（原版 `Sun flare`：URP `LensFlareComponentSRP`）----
        // 🔴 **2026-09-21 才建**：13 场里有 **6 场**挂着这个对象（arena1/2/aeldari/astramilitarum/
        //    leviathan/tauviorla），我们**一个都没建过** ⇒ 画面上**根本没有那个带光芒的日盘**。
        //    实测（arena1）：最亮的那个像素**原版 246 / 我们 204**，而且**两者位置逐点相同**
        //    （相对坐标都是 x=0.33 / y=0.21）⇒ 这就是「高光全缺」里的那一半。
        //    数据（7 个元素 + 6 张贴图 + 每场的组件值）**全部照抄原版**，出处见 `工具/gen_sunflare_cs.py`。
        //    ⚠️ 坐标用**局部**：与清单里那些网格/粒子同口径（= 原版的世界坐标，我们战场根节点在原点，
        //       战斗场景再靠 `Arena3D` 整体 −100 对齐）。
        int nFlare = 0;
        for (int i = 0; i < SunFlareData.Specs.Length; i++)
        {
            var sp = SunFlareData.Specs[i];
            if (sp.arena != mf.scene) continue;
            var flare = EnsureSunFlareAsset();
            var flGo = new GameObject("Sun flare");
            flGo.transform.SetParent(root.transform, false);
            flGo.transform.localPosition = sp.pos;
            flGo.transform.localRotation = sp.rot;
            var lf = flGo.AddComponent<LensFlareComponentSRP>();
            lf.lensFlareData               = flare;
            lf.intensity                   = sp.intensity;
            lf.maxAttenuationDistance      = sp.maxAttenuationDistance;
            lf.maxAttenuationScale         = sp.maxAttenuationScale;
            lf.distanceAttenuationCurve    = sp.distanceAttenuationCurve;
            lf.scaleByDistanceCurve        = sp.scaleByDistanceCurve;
            lf.attenuationByLightShape     = sp.attenuationByLightShape;
            lf.radialScreenAttenuationCurve = sp.radialScreenAttenuationCurve;
            lf.useOcclusion                = sp.useOcclusion;
            lf.useBackgroundCloudOcclusion = sp.useBackgroundCloudOcclusion;
            lf.environmentOcclusion        = sp.environmentOcclusion;
            lf.useWaterOcclusion           = sp.useWaterOcclusion;
            lf.occlusionRadius             = sp.occlusionRadius;
            lf.sampleCount                 = sp.sampleCount;
            lf.occlusionOffset             = sp.occlusionOffset;
            lf.scale                       = sp.scale;
            lf.allowOffScreen              = sp.allowOffScreen;
            lf.volumetricCloudOcclusion    = sp.volumetricCloudOcclusion;
            nFlare++;
            Debug.Log($"[Arena] 太阳耀斑：{sp.arena} pos=({sp.pos.x:F2},{sp.pos.y:F2},{sp.pos.z:F2}) "
                    + $"intensity={sp.intensity} 元素 {flare.elements.Length} 个");
        }

        ApplyLightAndAmbient(root.transform, mf);
        // 🆕 2026-10-11（A191）：**原版的【分组节点】+ 子树归属** —— 必须在「网格 / 粒子 / 灯」都建完之后，
        //    因为它要按「名字 + 最近位置」把**已经建出来的对象**改挂回去（见那个方法的头注）。
        ApplyGroupNodes(mf, root);
        ApplyPostFx(root.transform, mf);
        SetupSkybox();
        return root;
    }

    // ==================================================================
    //  🆕 2026-10-11（A191）· 原版的【分组节点】+ 子树归属
    //
    //  我们一直把战场**平铺**建（每个清单对象直接挂 `Warpforge_<场>` 根下），而原版是有父链的。
    //  后果不是「不好看」，是**几个组件永远解析不到**（旁挂 `_missingTargets` 逐条记着）：
    //    · `ScenarioAnimationBlend.myAnimation`      —— 原版挂在 `Battle Arena Dark Angels baked` 上；
    //    · `TauCannonAnimationStopper.animationComponent` —— 挂在 `Railgun Turret 1/2` 上；
    //    · `TauCannonAnimationStopper.animFXController`   —— 挂在 `…/Cylinder.00N/Railgun turret` 上；
    //    · `LookAtConstrainWIP.target`               —— 指 `Railgun Turret N Target`。
    //
    //  🔴 **为什么必须是【真父节点】、不能建个空壳**（这是判据不是偏好）：
    //    原版 `Dark Angels Void Combat animations`（assetGUID `aac3fe87a4618f5478ceaad364650105`）
    //    那条 clip 的曲线 `m_Path` 是**相对 `Animation` 组件那个 GameObject** 的，实读四条：
    //      `Turret 1 barrel` · `Turret 2 barrel` · `Turret missile joint` · `Turret 1 barrel/Lance Fire (5)`
    //    —— 它们**正是 `Battle Arena Dark Angels baked` 的直接/间接子件**。空壳 = 这条 clip 一帧都动不了。
    //    （判据 = 那个 `AnimationClip` 的 `m_PositionCurves[].path` / `m_RotationCurves[].path` /
    //      `m_FloatCurves[].path` 在 `bundle_battleprefabs_vfxandmisc_assets_all` 里直读。）
    //
    //  旁挂（`工具/gen_arena_groups.py`，**直读原版场景包**，不是手抄）：`<场>_groups.json`
    //    · `nodes[]`   = 要**新建**的节点（**已按浅→深排序**），local TRS = **原版 local**；
    //    · `targets[]` = **已建**的对象要改挂到 `parent` 那条路径下
    //                    （`pos` / `parentPos` = **与清单同一套**的无缩放世界链，用来按「名字 + 最近位置」对上）；
    //    · `adds[]`    = 已建对象**不改挂**、只补 `Animation` 组件（`Directional Light` 就是这条）；
    //    · `animation` = 原版这个 GO 上有 `Animation` 组件。
    //
    //  🔴 **改挂一律 `SetParent(..., worldPositionStays: true)`** —— 世界位姿**逐字不变**，
    //     所以这一步**不改变任何画面**（只改树形）。⛔ 别用 `false`：那会把子件按父级缩放**再乘一遍**
    //     （实测 tau 的 `Cylinder.001` 世界缩放是 (53.4, 57.6, 75.8)，它下面那些粒子会当场炸开）。
    //  🔴 **`worldBaked` 的对象绝不能改挂**：那些网格的顶点**已经烘在世界坐标里**、holder 必须 identity
    //     ⇒ 挂到任何非 identity 的父节点下都会被**变换第二次**。今天不触发（tau / darkangels 实测
    //     `worldBaked` 各 0 个），留着是防下一次静默出错。
    //  🔴 **不许静默**：node 的父找不到 / 要改挂的对象找不到，都**逐条出声**并计数（`LastGroupNodeMissed`）。
    // ==================================================================

    /// <summary>上一次 `ApplyGroupNodes` 的结果（自检断言直接读它）。
    /// · `Created` = 新建了几个节点 · `Moved` = 改挂成功几个 · `AnimAdded` = 补了几个 `Animation` ·
    /// · `Missed` = 有几条没对上（**出声**点名在 `MissedWhat` 里）。</summary>
    public static int LastGroupNodeCreated, LastGroupNodeMoved, LastGroupNodeAnimAdded, LastGroupNodeMissed;
    public static readonly System.Collections.Generic.List<string> LastGroupNodeMissedWhat
        = new System.Collections.Generic.List<string>();

    public static int ApplyGroupNodes(Manifest mf, GameObject root)
    {
        LastGroupNodeCreated = 0; LastGroupNodeMoved = 0; LastGroupNodeAnimAdded = 0;
        LastGroupNodeMissed = 0; LastGroupNodeMissedWhat.Clear();
        if (mf == null || root == null) return 0;
        var sc = LoadGroups(mf.scene);
        if (sc == null || sc.nodes == null || sc.nodes.Length == 0) return 0;   // 这一场本来就没有要补的

        // 顶点已烘进世界坐标的网格：**不许改挂**（见头注那条 🔴）
        var frozen = new HashSet<string>();
        if (mf.meshes != null)
            foreach (var e in mf.meshes)
                if (e.worldBaked && !string.IsNullOrEmpty(e.go)) frozen.Add(CardPresentation.EnvironmentApplier.Norm(e.go));

        // ---- ① 建节点（旁挂已按「浅 → 深」排序 ⇒ 父一定先于子出现）----
        var created = new Dictionary<string, Transform>();
        foreach (var n in sc.nodes)
        {
            if (n == null || string.IsNullOrEmpty(n.path)) continue;
            var parentTr = ResolveGroupParent(root.transform, created, n.parent, n.parentPos, out var why);
            if (parentTr == null)
            {
                LastGroupNodeMissed++; LastGroupNodeMissedWhat.Add($"node `{n.path}`: {why}");
                parentTr = root.transform;                 // 挂到场根下（**出声过了**，不静默）
            }
            var go = new GameObject(n.name);
            go.transform.SetParent(parentTr, false);
            go.transform.localPosition = ToVec3(n.localPos, Vector3.zero);
            go.transform.localRotation = ToQuat(n.localRot);
            go.transform.localScale    = ToVec3(n.localScale, Vector3.one);
            if (n.animation != 0 && go.GetComponent<Animation>() == null)
            { go.AddComponent<Animation>(); LastGroupNodeAnimAdded++; }
            created[n.path] = go.transform;
            LastGroupNodeCreated++;
        }

        // ---- ② 已建对象改挂到它的原版父节点下 ----
        foreach (var t in sc.targets ?? new GroupTarget[0])
        {
            if (t == null || string.IsNullOrEmpty(t.name)) continue;
            var tr = FindBuilt(root.transform, t.name, t.pos);
            if (tr == null)
            {
                LastGroupNodeMissed++;
                LastGroupNodeMissedWhat.Add($"target `{t.name}`（旁挂 pos {Vec3Str(t.pos)}）在我们建出来的树里找不到"
                                            + "（**很可能是上游闸门没建** —— 见同一场日志里「内容：…另跳过无贴图 N · "
                                            + "原版关着 M · renderMode=None K 个」那行；闸门 = 本文件 `BuildContent` 的四道）");
                continue;
            }
            if (frozen.Contains(CardPresentation.EnvironmentApplier.Norm(t.name)))
            {
                LastGroupNodeMissed++;
                LastGroupNodeMissedWhat.Add($"target `{t.name}` 是 `worldBaked`（顶点已烘在世界坐标里）⇒ **不改挂**");
                continue;
            }
            var parentTr = ResolveGroupParent(root.transform, created, t.parent, t.parentPos, out var why);
            if (parentTr == null)
            {
                LastGroupNodeMissed++; LastGroupNodeMissedWhat.Add($"target `{t.name}`: {why}");
                continue;
            }
            tr.SetParent(parentTr, true);                  // 🔴 true = 世界位姿逐字不变（画面零变化）
            LastGroupNodeMoved++;
            if (t.animation != 0 && tr.GetComponent<Animation>() == null)
            { tr.gameObject.AddComponent<Animation>(); LastGroupNodeAnimAdded++; }
        }

        // ---- ③ 不改挂、只补 `Animation` 的那几条 ----
        foreach (var t in sc.adds ?? new GroupTarget[0])
        {
            if (t == null || string.IsNullOrEmpty(t.name)) continue;
            var tr = FindBuilt(root.transform, t.name, t.pos);
            if (tr == null)
            {
                LastGroupNodeMissed++;
                LastGroupNodeMissedWhat.Add($"add `{t.name}`（旁挂 pos {Vec3Str(t.pos)}）在我们建出来的树里找不到"
                                            + "（**很可能是上游闸门没建** —— 见同一场日志里「内容：…另跳过无贴图 N · "
                                            + "原版关着 M · renderMode=None K 个」那行；闸门 = 本文件 `BuildContent` 的四道）");
                continue;
            }
            if (t.animation != 0 && tr.GetComponent<Animation>() == null)
            { tr.gameObject.AddComponent<Animation>(); LastGroupNodeAnimAdded++; }
        }

        Debug.Log($"[Arena] A191 分组节点（{mf.scene}）：新建 {LastGroupNodeCreated} 个节点 · 改挂 {LastGroupNodeMoved} 个对象 · "
                + $"补 `Animation` {LastGroupNodeAnimAdded} 个 · 没对上 {LastGroupNodeMissed} 条"
                + (LastGroupNodeMissed > 0 ? "（**出声**）：" + string.Join(" / ", LastGroupNodeMissedWhat.ToArray()) : ""));
        return LastGroupNodeMoved;
    }

    /// <summary>把旁挂里的 `parent` 路径翻成一个 Transform：
    ///  ① 本表**刚建出来**的节点（`created`，键 = 原版整条路径）；
    ///  ② 否则它必须是我们**已建出来的对象**（清单里的网格/粒子/灯/相机）——
    ///     按**叶子名 + 最近位置**找（同名对象不止一个，`pos` 是清单同一套的无缩放世界链）。
    /// 找不到 ⇒ 回 null + 一句「为什么」（调用方出声）。</summary>
    static Transform ResolveGroupParent(Transform root, Dictionary<string, Transform> created,
                                        string path, float[] pos, out string why)
    {
        why = null;
        if (string.IsNullOrEmpty(path)) return root;                  // 父 = 场根
        if (created != null && created.TryGetValue(path, out var ct) && ct != null) return ct;
        var leaf = path.Substring(path.LastIndexOf('/') + 1);
        var t = FindBuilt(root, leaf, pos);
        if (t != null) return t;
        why = $"父路径 `{path}` 既不在本表新建的节点里、也不在我们已建的对象里（叶子 `{leaf}` + pos {Vec3Str(pos)}）"
            + "（**父可能是被上游闸门挡掉的** —— 见同一场日志里「内容：…另跳过…」那行；"
            + "⚠️ 旁挂里被闸门挡掉的父会进 `nodes[]`（新建空节点）而**不是** `targets[]`，两处口径已在 `工具/gen_arena_groups.py` 对齐）";
        return null;
    }

    /// <summary>在已建出来的树里按「**归一化名字 + 最近位置**」找对象。
    /// 🔴 名字比较走 `EnvironmentApplier.Norm`（**Trim 后比**）—— 原版真有带**尾随空格**的名字
    ///   （`'Railgun Turret 1 Target '`），而 Unity 存得下它、我们建的时候也照抄了；不 Trim 会比不上。
    /// 🔴 位置比较用 `Transform.position`（真世界位置）：**改挂前后它都不变**（见头注那条 `true`）⇒
    ///   匹配与「先建节点还是先改挂」的次序无关。
    /// 判据出处：同一套做法在本仓已有两处 —— `EnvironmentApplier.FindNearest`（运行时）与
    ///   `ArenaBuilder.SameObject`（清单并表，容差 0.05）。这里**不写新口径**，容差与 `SameObject` 一致。</summary>
    static Transform FindBuilt(Transform root, string name, float[] pos)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        string want = CardPresentation.EnvironmentApplier.Norm(name);
        var all = root.GetComponentsInChildren<Transform>(true);
        Transform best = null; float bd = float.MaxValue;
        for (int i = 0; i < all.Length; i++)
        {
            var t = all[i];
            if (CardPresentation.EnvironmentApplier.Norm(t.name) != want) continue;
            if (pos == null || pos.Length < 3) return t;
            var p = t.position;
            float dx = p.x - pos[0], dy = p.y - pos[1], dz = p.z - pos[2];
            float d = dx * dx + dy * dy + dz * dz;
            if (d < bd) { bd = d; best = t; }
        }
        return best;
    }

    static string Vec3Str(float[] a)
        => (a == null || a.Length < 3) ? "(?)" : $"({a[0]:F3},{a[1]:F3},{a[2]:F3})";

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
        RenderSettings.skybox = SkyboxMaterial();
    }

    /// <summary>天空盒材质（**建 `RenderSettings.skybox` 与 §27 的「逐场状态」共用这一份**）。
    /// 原版 13 场全指同一张 `Skybox Clouds Dusk`；规格逐值实读见上面 `SetupSkybox` 的注释。</summary>
    public static Material SkyboxMaterial()
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
                    return null;
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
            if (sh == null) { Debug.LogError("[Arena] 🔴 找不到内置 shader `Skybox/Cubemap`"); return null; }
            mat = new Material(sh) { name = "Skybox Clouds Dusk" };
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.SetTexture("_Tex", cube);
        mat.SetColor("_Tint", new Color(0.5490196f, 0.5061521f, 0.4039216f, 0.5f));
        mat.SetFloat("_Exposure", 0.67f);
        mat.SetFloat("_Rotation", 0f);
        return mat;
    }

    /// <summary>灯光 + 环境光 —— **独立场景与战斗场景共用**（判据只留一处）。
    /// 🔴 2026-09-20 从 `BuildSceneTail` 挪进来：原来这两段只在建**独立战场场景**时执行，
    /// 而战斗场景走的是 `BuildContent`（`BattleScene.cs:3872`）⇒ **灯根本没跟过来**
    /// （`BattleScene.cs` 全篇没有 `AddComponent&lt;Light&gt;`、也没有 `RenderSettings`）。
    /// 值全来自清单（= 原版实读），不在这里写死任何数值。</summary>
    /// <summary>灯光 + 环境光 —— **独立场景与战斗场景共用**（判据只留一处）。
    /// 🔴 2026-09-20 从 `BuildSceneTail` 挪进来：原来这两段只在建**独立战场场景**时执行，
    /// 而战斗场景走的是 `BuildContent`（`BattleScene.cs:3872`）⇒ **灯根本没跟过来**
    /// （`BattleScene.cs` 全篇没有 `AddComponent&lt;Light&gt;`、也没有 `RenderSettings`）。
    ///
    /// 🆕 **2026-09-30（§27 架构）**：这里**只剩「内容」那一半 —— 造那盏灯**（灯属于战场物件、跟着 prefab 走）。
    /// 另一半（雾 / 环境光 / 天空盒 / `RenderSettings.sun` / halo·flare 强度）**是场景级的、进不了 prefab**，
    /// 已搬进 `CardPresentation.ArenaSceneState.Apply` —— **建场期与运行期走同一份判据**
    /// （§27 要「运行时按当前战场重放一遍」，两套判据迟早不一致）。清单 → 那套状态在 `FromManifest`。
    /// 值全来自清单（= 原版实读），不在这里写死任何数值。</summary>
    public static void ApplyLightAndAmbient(Transform parent, Manifest mf)
    {
        // ---------------- 灯光（**内容**）----------------
        Light sunLight = null;
        // ⚠️ **2026-09-22 记一笔**：下面这几处 `mf.camera/light/ambient/defaultEnv != null`
        //    与粒子那族**是同一个陷阱的形状**（`JsonUtility` 会把清单里的 `null` 物化成整棵子树，
        //    见 `ParticleEntry.hasVelocity` 的说明）—— 只要生成器哪天写出 `"camera": null`，
        //    这些判断就会**静默为真**、拿一个空对象去当相机/灯用。
        //    **当前 13 场实测这五个键全不为 None**（工具核对过），所以还没炸；
        //    真要动生成器输出这几个键，先给它们也加 `hasXxx` 布尔量。
        if (mf.light != null)
        {
            var lGo = new GameObject(string.IsNullOrEmpty(mf.light.name) ? "Directional Light" : mf.light.name);
            if (parent != null) lGo.transform.SetParent(parent, false);
            var li = lGo.AddComponent<Light>();
            sunLight = li;
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

        // ---------------- 场景级那几项 → 共用件 ----------------
        //   判据（雾的形状与开关 / 环境光 / 天空盒 / `m_Sun` / halo·flare）**全在那边**，这里只搬运。
        CardPresentation.ArenaSceneState.Apply(FromManifest(mf), parent != null ? parent.gameObject : null);
    }

    /// <summary>清单 → 「逐场场景状态」（§27 架构）。**判据全在 `CardPresentation/ArenaSceneState.cs`**，
    /// 这里只搬运 + 出声（缺字段、异常值都要报出来，不许静默）。</summary>
    public static CardPresentation.ArenaSceneState FromManifest(Manifest mf)
    {
        var st = new CardPresentation.ArenaSceneState();
        st.arena = mf.scene;
        // 原版 `RenderSettings.m_Sun`：8 场有值、5 场是空的（判据名单在 `ArenaSceneState.SunIsNullFor`）
        st.hasSun = !CardPresentation.ArenaSceneState.SunIsNullFor(mf.scene);
        // 13 场 `m_HaloStrength` / `m_FlareStrength` 全是 0.0（Unity 默认 0.5 / 1 ⇒ 必须显式写 0）
        st.haloStrength = 0f;
        st.flareStrength = 0f;
        st.skybox = SkyboxMaterial();

        if (mf.ambient != null)
        {
            st.ambientMode = mf.ambient.mode;
            // 颜色取 `defaultEnv.ambientColor`（**运行时真值**），不是场景里的 `m_AmbientSkyColor`
            if (mf.defaultEnv != null && mf.defaultEnv.ambientColor != null && mf.defaultEnv.ambientColor.Length >= 3)
                st.ambientSky = ToColor(mf.defaultEnv.ambientColor);
            else
            {
                Debug.LogWarning("[Arena] 🔴 清单里没有 defaultEnv（原版运行时环境光的真源）"
                               + " ⇒ 退回场景里的 m_AmbientSkyColor —— **这个值原版运行时不用**，重跑生成器");
                st.ambientSky = ToColor(mf.ambient.sky);
            }
            st.ambientIntensity = mf.ambient.intensity;
            st.ambientBlend = mf.defaultEnv != null ? mf.defaultEnv.ambientBlend : 0f;

            // 雾：**形状无条件照原版场景写**（mode / 线性起止 / 颜色 / 密度），开关与运行时值再按 `defaultEnv`
            st.fogMode = mf.ambient.fogMode;
            st.fogShapeColor = ToColor(mf.ambient.fogColor);
            st.fogShapeDensity = mf.ambient.fogDensity;
            st.fogLinearStart = mf.ambient.linearFogStart;
            st.fogLinearEnd = mf.ambient.linearFogEnd;
            st.fogRunColor = ToColor(mf.defaultEnv != null ? mf.defaultEnv.fogColor : mf.ambient.fogColor);
            st.fogRunDensity = mf.defaultEnv != null ? mf.defaultEnv.fogDensity : mf.ambient.fogDensity;
        }
        else Debug.LogWarning("[Arena] 🔴 清单里没有 ambient —— 环境光用 Unity 默认值");

        if (mf.camera != null)
        {
            st.camSensorX = mf.camera.sensorSizeX;
            st.camSensorY = mf.camera.sensorSizeY;
            st.camFocalLength = 28f;
            st.camNear = mf.camera.near;
            st.camFar = mf.camera.far;
            st.camFov = mf.camera.fov;
        }
        return st;
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
        // 🆕 **2026-09-27：`sensorSize` 改成逐场**（原来写死 41.5×24）。判据见 `CameraData.sensorSizeX` 那段：
        //    三场是 **37.2**、十场是 41.5；`gateFit = Horizontal` 下 x 直接决定 hFOV
        //    ⇒ 写死会把那三场**等比放大 10.36%**（这就是「三场与原版差 ~10%」的真因）。
        c.sensorSize          = new Vector2((d != null && d.sensorSizeX > 0f) ? d.sensorSizeX : 41.5f,
                                            (d != null && d.sensorSizeY > 0f) ? d.sensorSizeY : 24f);
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

    /// <summary>LUT 的导入设置 —— **原版实读**（`bundle_scenes_scenes_battlearenasororitas` 的 `Texture2D`：
    /// `m_ColorSpace = 0`(Linear) · `m_MipCount = 9`(满链) · `m_FilterMode = 1`(Bilinear) ·
    /// `m_WrapMode = None`(Clamp) · `m_TextureFormat = 3`(RGB24，**未压缩**)）。
    /// 🔴 LUT 是**数据不是颜色纹理** —— `sRGBTexture` 必须 **false**，压缩必须 **Uncompressed**，
    ///    否则那张表会被解码/量化坏掉（画面表现为整场偏色，且不容易联想到 LUT）。</summary>
    static void EnsureLutImportSettings(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        bool ok = !ti.sRGBTexture && ti.mipmapEnabled && ti.filterMode == FilterMode.Bilinear
                  && ti.wrapMode == TextureWrapMode.Clamp && ti.npotScale == TextureImporterNPOTScale.None
                  && ti.textureCompression == TextureImporterCompression.Uncompressed
                  && ti.maxTextureSize >= 256;
        if (ok) return;
        ti.textureType          = TextureImporterType.Default;
        ti.sRGBTexture          = false;
        ti.mipmapEnabled        = true;
        ti.filterMode           = FilterMode.Bilinear;
        ti.wrapMode             = TextureWrapMode.Clamp;
        ti.npotScale            = TextureImporterNPOTScale.None;
        ti.textureCompression   = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize       = 256;
        ti.SaveAndReimport();
    }

    /// <summary>后处理（2026-09-20 新增）。原版战场在 **`BoardCamera` 这个 GameObject 上挂了一个全局
    /// `Volume`**（`m_IsGlobal:1` / priority 0 / weight 1），profile 名 `&lt;场景&gt; PostProcessing`，
    /// 里面 3 个 override：
    ///   · **Bloom**      threshold 1.15 · intensity 5.0 · scatter 1.0 · skipIterations 6
    ///   · **Vignette**   color 黑 · center (0.5,0.5) · intensity 0.297
    ///   · **ColorLookup** ⚠️ **2026-09-22 更正**：原来写「LUT `LUT Normal` 实测严格 identity ⇒ 不接」——
    ///     **那是量错了纹理**。真相：**7 个战场各有一张自己的 `LUT Battle Arena &lt;场&gt;.png`**
    ///     （256×16；与恒等 LUT 的偏差 **中位 7/255 · p90 11/255**，且是 16 级量化 ⇒ **是温和调色、不是恒等**）。
    ///     **现在照接**（见下面的分支）。实测：这 7 场里有 6 场在 13 场亮度表里偏亮 1.06~1.21。
    ///     🔴 **13 场逐场现读的那组判据**（`texture.m_Value` 的 `m_FileID` 7 场 `0` / 6 场 `9`+共享 PathID，
    ///     以及「那 6 场**不是**没有 `ColorLookup`」这条防误读）**只写一处**：下面
    ///     `else if (c.type == "ColorLookup")` 那一段的注释（2026-10-11 A293 补）。
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

        // 🔴 **LUT 的导入设置必须先做完**（它会 `SaveAndReimport()` 触发一次资源刷新）——
        //    实测：把这一步夹在「建 profile → 加组件」中间，**刚加进去的 ColorLookup 会被冲掉**
        //    （症状 = 运行时栈里 `ColorLookup texture=(null) contribution=0`，而构建日志写着「接上了」）。
        foreach (var cc in mf.postFx.components)
        {
            if (cc.type != "ColorLookup") continue;
            string base0 = (mf.postFx.profile ?? "").Replace(" PostProcessing", "").Trim();
            string lut0  = $"Assets/WarpforgeArena1/Textures/LUT/LUT {base0}.png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(lut0) != null) EnsureLutImportSettings(lut0);
        }

        // 🔴 **2026-09-25 修：不再 `DeleteAsset` + `CreateAsset`。**
        //   原来那两行会**换掉 guid、并给每个子资产重新编号**（`!u!114 &<fileID>` 全变）⇒
        //   **每次构建后 `git status` 多十几条噪声 diff**（内容逐字等价、只有编号与块序变）；
        //   更要紧的是**存过盘的场景指着旧 guid**，一旦哪次没跟着更新就会「场景指向旧 profile」
        //   （与 `SaveOrReuse`/`GenerateUniqueAssetPath` 那条同源，见 `CLAUDE.md` §三）。
        //   改成**原地重建**：根对象原地留着；子组件**按类型复用旧的那份**，
        //   用 `CopySerialized` 把值灌进去（**fileID 因此不变 ⇒ 一条 diff 都不产生**），
        //   只有原版新加的组件类型才真新建；本次清单里没有的旧组件才摘掉销毁。
        var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        var keep = new Dictionary<System.Type, VolumeComponent>();
        var used = new HashSet<System.Type>();
        if (prof == null)
        {
            prof = ScriptableObject.CreateInstance<VolumeProfile>();
            prof.name = string.IsNullOrEmpty(mf.postFx.profile) ? "ArenaPostFx" : mf.postFx.profile;
            AssetDatabase.CreateAsset(prof, path);
        }
        else
        {
            var old = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int oi = 0; oi < old.Length; oi++)
            {
                var vc = old[oi] as VolumeComponent;
                if (vc != null && vc != prof) keep[vc.GetType()] = vc;   // ⚠️ 只登记，**先别销毁**
            }
            prof.components.Clear();                                    // 引用表清空，对象还活着
        }

        foreach (var c in mf.postFx.components)
        {
            if (c.type == "Bloom")
            {
                var b = NewComp<Bloom>();
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
                CommitComp(prof, b, keep, used);
            }
            else if (c.type == "Vignette")
            {
                var v = NewComp<Vignette>();
                v.active = true;
                v.color.value     = ToColor(c.color);
                v.center.value    = (c.center != null && c.center.Length >= 2)
                                    ? new Vector2(c.center[0], c.center[1]) : new Vector2(0.5f, 0.5f);
                v.intensity.value = c.intensity;
                v.smoothness.overrideState = false;             // 原版没打勾 ⇒ 用 URP 默认
                v.rounded.overrideState    = false;
                CommitComp(prof, v, keep, used);
            }
            else if (c.type == "ColorLookup")
            {
                // 🆕 2026-09-22：**原来那句「LUT 恒等 ⇒ 不接」是量错了纹理**（量的是另一张 `LUT Normal`）。
                //    真相：**7 个战场各有一张自己的 `LUT Battle Arena <场>.png`**（实测与恒等 LUT 的偏差
                //    中位 7/255 · p90 11/255 · 16 级量化 ⇒ 是一层温和调色，不是恒等），
                //    命名与 profile 严格一一对应：`Battle Arena X PostProcessing` → `LUT Battle Arena X`。
                //    ⚠️ 其余 6 场（含 `Battle Arena 1/2/3/4`）**没有专属 LUT** ⇒ 本地找不到就如实报、不接。
                // 🔴 **2026-10-11（A293）补判据 + 一句防误读**（13 场**逐场现读**原版 `ColorLookup.*.json`）：
                //    · **7 场** `m_FileID = 0`（= **本条包内**的 LUT）—— 与上面那句「各有一张」逐一对应，
                //      旁证 = `bundle_scenes_scenes_<场>/Texture2D/` 里那 7 个包**各只有一张** `LUT <场>.png`；
                //    · **6 场** `m_FileID = 9`（`arena2` 是 **10** —— 那是 **externals 表下标**，别当常量）
                //      + `PathID 382974660631151556` = `battlesharedresources` 里那份**共享** `LUT Normal`
                //      （**那张才是严格恒等**）；旁证 = `battlearena1` 包的 `Texture2D/` 里**一张 LUT 都没有**；
                //    · **13/13 场** `active = 1` · `contribution = 1.0` · `texture.overrideState = 1`。
                //    ⚠️ **那 6 场【不是】「没有 `ColorLookup`」** —— 原版 13 场**场场都有**这个槽，只是它们
                //      贴的是恒等那张；**没有槽的是我们的 profile**（构建时按「找不到专属 LUT 就不接」省掉了）。
                //      ⛔ 别再把这条读成「原版那几场静态 LUT 也是空的」（`Battle/BattlePostFx.cs` 文件头第 ② 条
                //      同日已就地订正 —— 那句错记就是从别处照抄的）。
                string baseName = (mf.postFx.profile ?? "").Replace(" PostProcessing", "").Trim();
                string lutName  = "LUT " + baseName;
                string lutPath  = $"Assets/WarpforgeArena1/Textures/LUT/{lutName}.png";
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(lutPath) == null)
                {
                    Debug.Log($"[Arena] ColorLookup：`{lutName}` 本地没有（原版只有 7 场带专属 LUT）⇒ 这场不接");
                }
                else
                {
                    // ⚠️ 导入设置**已在方法开头做完**（那里必须早于建 profile，见上面的注释）
                    var cl = NewComp<ColorLookup>();
                    cl.active = true;
                    cl.texture.overrideState = true;
                    cl.texture.value = AssetDatabase.LoadAssetAtPath<Texture2D>(lutPath);
                    cl.contribution.overrideState = true;
                    cl.contribution.value = c.contribution <= 0f ? 1f : c.contribution;
                    CommitComp(prof, cl, keep, used);
                    Debug.Log($"[Arena] ColorLookup：接上原版 LUT `{lutName}`（contribution {cl.contribution.value}）");
                }
            }
            else
            {
                // 其余未知类型如实报出来，不静默
                Debug.LogWarning($"[Arena] 清单里有个我们不认识的后处理组件 `{c.type}` —— 没接");
            }
        }

        // 本次清单里**没有**的旧组件才摘掉销毁（比如原版删掉了某个组件、或这场换了 profile 结构）
        int dropped = 0;
        foreach (var kv in keep)
        {
            if (used.Contains(kv.Key) || kv.Value == null) continue;
            AssetDatabase.RemoveObjectFromAsset(kv.Value);
            UnityEngine.Object.DestroyImmediate(kv.Value, true);
            dropped++;
        }
        if (dropped > 0) Debug.Log($"[Arena] PostFx：摘掉 {dropped} 个清单里已经没有的旧组件");

        AssetDatabase.SaveAssets();

        var go = new GameObject("PostFx（原版挂在 BoardCamera 上的全局 Volume）");
        if (parent != null) go.transform.SetParent(parent, false);
        var vol = go.AddComponent<Volume>();
        vol.isGlobal     = mf.postFx.isGlobal;
        vol.priority     = mf.postFx.priority;
        vol.weight       = mf.postFx.weight;
        vol.sharedProfile = prof;
    }

    /// <summary>造一个「待填」的后处理组件：默认值 + **所有 override 置 true**。
    /// 语义与 `VolumeProfile.Add&lt;T&gt;(true)` 一致（它内部就是 `CreateInstance` + `SetAllOverridesTo(true)`），
    /// 但**不挂进 profile** —— 由 `CommitComp` 决定「灌进旧的那份」还是「新建一份」。</summary>
    static T NewComp<T>() where T : VolumeComponent
    {
        var c = ScriptableObject.CreateInstance<T>();
        c.SetAllOverridesTo(true);
        return c;
    }

    /// <summary>把一个「待填组件」落到 profile 上。
    ///
    /// 🔴 **优先灌进【已存在的同类型子资产】**（`CopySerialized`，它的 **fileID 因此不变**）——
    ///   这是「每次构建 `git status` 多十几条噪声 diff」的根治办法；没有才真新建。
    ///   判据 → `资料/已知的坑.md`「生成资产的落盘路径必须与去重 key 同口径」同族的 guid 抖动那一族。</summary>
    static void CommitComp<T>(VolumeProfile prof, T fresh,
                              Dictionary<System.Type, VolumeComponent> keep, HashSet<System.Type> used)
        where T : VolumeComponent
    {
        VolumeComponent oldOne;
        if (keep.TryGetValue(typeof(T), out oldOne) && oldOne != null)
        {
            // ⚠️ `CopySerialized` 连 `m_Name` 一起覆盖（新对象的名字是空的）⇒ 先把旧名字留住，
            //    否则 `.asset` 里会多出一条 `m_Name:` 的 diff（实测就这么差 2 行）。
            var oldName = oldOne.name;
            EditorUtility.CopySerialized(fresh, oldOne);       // 值灌进旧对象 ⇒ fileID 不变
            oldOne.name = oldName;
            UnityEngine.Object.DestroyImmediate(fresh, true);
            prof.components.Add(oldOne);
        }
        else
        {
            prof.components.Add(fresh);
            AssetDatabase.AddObjectToAsset(fresh, prof);
        }
        used.Add(typeof(T));
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
        ApplyCameraSidecar(sceneName, mf);
        ApplyNegScaleSidecar(sceneName, mf);
        ApplyShadowSidecar(sceneName, mf);          // 🆕 2026-09-30（①-l）：逐对象的投/收阴影标志
        return mf;
    }

    public static string CameraSidecarPath(string s) => ArenaDir(s) + "/" + s + "_camera.json";
    public static string NegScaleSidecarPath(string s) => ArenaDir(s) + "/" + s + "_negscale.json";

    // ---------------------------------------------------------------
    //  🆕 2026-10-11（A191）：原版的【分组节点】旁挂（`<场>_groups.json`）
    //  生成器 = `工具/gen_arena_groups.py`（**直读原版场景包**）；消费方 = `ApplyGroupNodes`（头注在那儿）。
    //  ⚠️ 用 `File.ReadAllText` 读（与 `LoadManifest` 同一个口）⇒ **不需要 Unity 先导入它**；
    //      `.meta` 由编辑器的下一次刷新生成，与别的旁挂（`_camera.json` / `_negscale.json`）同一套。
    // ---------------------------------------------------------------
    public static string GroupsPath(string s) => ArenaDir(s) + "/" + s + "_groups.json";

    /// <summary>要**新建**的一个节点。`local*` = **原版 local TRS**（原样写下去 —— 与清单的
    /// `pos` 那套「无缩放世界链」**是两回事**，这里**不引入第二套坐标口径**）。</summary>
    [System.Serializable] public class GroupNode
    {
        public string path; public string name;
        public float[] localPos; public float[] localRot; public float[] localScale;
        public string parent; public float[] parentPos; public int animation;
    }

    /// <summary>**已建**出来的对象要怎么处理：改挂到 `parent`（`targets`）或只补组件（`adds`）。
    /// `pos` / `parentPos` = **与清单同一套**的无缩放世界链（判据 → `gen_arena_groups.py` 的 `world_of`）。</summary>
    [System.Serializable] public class GroupTarget
    {
        public string name; public float[] pos; public string parent; public float[] parentPos; public int animation;
    }

    [System.Serializable] public class GroupsSidecar
    {
        public string scene; public GroupNode[] nodes; public GroupTarget[] targets; public GroupTarget[] adds;
    }

    /// <summary>读 `<场>_groups.json`。**文件不存在 ⇒ 回 null 并刻意不出声**：13 场里只有
    /// `battlearenadarkangels` / `battlearenatauviorla` 有这件旁挂（A191 只点名了那几个宿主），
    /// 「没有」是**正常态**，不是缺陷。真出问题（建不出来 / 对不上）由 `ApplyGroupNodes` 逐条出声。</summary>
    public static GroupsSidecar LoadGroups(string sceneName)
    {
        var path = GroupsPath(sceneName);
        if (!File.Exists(path)) return null;
        var sc = JsonUtility.FromJson<GroupsSidecar>(File.ReadAllText(path));
        if (sc == null) Debug.LogError($"[Arena] 分组节点旁挂解析失败：{path}");
        return sc;
    }

    /// <summary>旁挂：**镜像（负缩放）对象**（`工具/gen_arena_negscale.py` 从原版场景包直读）。</summary>
    [System.Serializable] public class NegScaleItem { public string go; public float[] pos; public float[] scale; }
    [System.Serializable] public class NegScaleSidecar { public string arena; public NegScaleItem[] items; }

    /// <summary>🆕 2026-09-28：把旁挂里的**带符号**世界缩放盖回清单的 `scale`。
    ///
    /// 🔴 **为什么非要有它**：`gen_unity_arena_manifest.py` 的 `world_scale()` 返回的是
    ///   **世界矩阵的列长**（`sqrt(Σ M[i][j]²)`）—— **按构造永远是正数**，于是**负缩放的符号被吃掉**；
    ///   而那个文件头写着「不做任何手性/镜像转换」⇒ **说法与实现对不上**。
    ///   负行列式 = 该对象**镜像**，Unity 下会连带把**三角形绕序翻过来**
    ///   ⇒ 在 `Cull Back` 的材质上**该藏的那一面被画出来**。
    ///   实测（`battlearenaleviathan` 的 `Toxic Pool Glow` ×2，原版 `(1, −1, 1)`）：
    ///   清单写成 `(1, 1, 1)` ⇒ 那颗网格把整屏罩成一片亮黄绿，该场亮度比 **1.065**（>±5%）、
    ///   逐块差在一条横带上 **+71~+107**；Y 改回 −1 重跑 ⇒ **1.065 → 1.023**，横带消失。
    ///   旁挂走工程既有的「**旁挂数据 + `工具/gen_arena_*.py`**」那套（**不动 `d:/2/` 的生成器**）。
    /// ⚠️ 缺文件时**出声** —— 没有旁挂 = 回到「清单的列长」= 那个镜像对象仍然是错的。
    /// ⚠️ 旁挂只覆盖**它列到的对象**；13 场实测合计 17 条（`battlearena2/3` · `blacklegion` ·
    ///   `leviathan` · `sororitas`），另有 5 个镜像对象**判不出**（旋转非轴对齐）⇒ **如实记着**。</summary>
    static void ApplyNegScaleSidecar(string sceneName, Manifest mf)
    {
        if (mf == null) return;
        var p = NegScaleSidecarPath(sceneName);
        if (!File.Exists(p))
        {
            Debug.LogWarning($"[Arena] 没有镜像旁挂 `{p}` ⇒ 镜像对象的负缩放仍然按清单的正数列长画"
                           + "（**会不会画错取决于那场有没有镜像对象**）。跑一次 `python 工具/gen_arena_negscale.py` 生成。");
            return;
        }
        var sc = JsonUtility.FromJson<NegScaleSidecar>(File.ReadAllText(p));
        if (sc == null || sc.items == null)
        {
            Debug.LogWarning($"[Arena] 镜像旁挂解析失败：{p}");
            return;
        }
        int hit = 0;
        foreach (var it in sc.items)
        {
            if (it == null || string.IsNullOrEmpty(it.go) || it.scale == null) continue;
            foreach (var e in mf.meshes)
                if (SameObject(e.go, e.pos, it.go, it.pos)) { e.scale = it.scale; hit++; }
            foreach (var e in mf.particles)
                if (SameObject(e.go, e.pos, it.go, it.pos)) { e.scale = it.scale; hit++; }
        }
        Debug.Log($"[Arena] 镜像旁挂：{sceneName} 有 {sc.items.Length} 条，命中清单里 {hit} 个对象");
    }

    /// <summary>清单条目与旁挂条目是不是同一个对象 —— **名字 + 世界位置**都比（同名对象可能不止一个）。</summary>
    static bool SameObject(string aName, float[] aPos, string bName, float[] bPos)
    {
        if (aName != bName) return false;
        if (aPos == null || bPos == null || aPos.Length < 3 || bPos.Length < 3) return false;
        for (int i = 0; i < 3; i++) if (Mathf.Abs(aPos[i] - bPos[i]) > 0.05f) return false;
        return true;
    }

    public static string ShadowSidecarPath(string s) => ArenaDir(s) + "/" + s + "_shadowflags.json";

    /// <summary>旁挂：**逐对象的阴影标志**（`工具/gen_arena_shadowflags.py` 从原版场景包直读）。</summary>
    [System.Serializable] public class ShadowFlagItem { public string go; public float[] pos; public int cast; public int receive; }
    [System.Serializable] public class ShadowFlagSidecar { public string arena; public ShadowFlagItem[] items; }

    /// <summary>🆕 2026-09-30（①-l）：把旁挂里的**投/收阴影标志**并进清单的网格条目。
    ///
    /// 🔴 **为什么非要有它**：`ApplyShadowFlags` 原来**两处都硬写死 `Off / false`**，理由写的是
    ///   「原版战场是烘焙的、无实时阴影」—— **那句是错的**：`blacklegion` 的 67 个 `MeshRenderer` 里
    ///   **52 个 `m_CastShadows = 1` 且 `m_ReceiveShadows = 1`**（其余 15 个 0/0），而且它地板/管道的
    ///   shader 名字就叫 **`Everguild/Misc/Unlit shadows receiver`**（就是来收阴影的）
    ///   ⇒ 我们关掉投/收之后阴影贴图是空的、地板**全亮**：实测该场前景（y≥480 三条带）
    ///   比原版亮 **+29 ~ +37**、逐块差中位 **+4.06** —— 这是它亮度比 **1.074**（全 13 场唯一还在 ±5% 外）的大头。
    /// **原版不是全场统一**（52 开 / 15 关）⇒ 一刀切开也会错 ⇒ 必须逐对象搬。
    /// 旁挂走工程既有的「**旁挂数据 + 工具/gen_arena_*.py**」那套（**不动 `d:/2/` 的生成器**）。
    ///
    /// ⚠️ 缺文件 / 匹配不上时**出声**，并退回旧行为（`Off/false`）—— 不静默。
    /// ⚠️ 旁挂**只覆盖网格**（原版那边收的是 `MeshRenderer` + `SkinnedMeshRenderer`）；
    ///   粒子的投/收仍是硬写的 `Off/false`（原版粒子多半也是 0，但**没逐颗核过**，如实记着）。
    /// ⚠️ **深层那条仍在**：**这台工程的 URP 实时阴影链整条没跑**（`WF_SHADOWCAST=1` 逐格几乎不变 ·
    ///   `WF_SHADOWPROBE=1` 的方块一个投影都没有）⇒ 标志接对了**也未必看得见影子**；
    ///   「是批处理预览不渲、还是工程整体不渲」要出 player / 进 Play 才能定性 → `资料/真Play待验清单.md` **E9**。</summary>
    static void ApplyShadowSidecar(string sceneName, Manifest mf)
    {
        if (mf == null) return;
        var p = ShadowSidecarPath(sceneName);
        if (!File.Exists(p))
        {
            Debug.LogWarning($"[Arena] 没有阴影旁挂 `{p}` ⇒ 网格的投/收阴影仍按旧行为（**全 Off**）。"
                           + " 跑一次 `python 工具/gen_arena_shadowflags.py` 生成"
                           + "（`blacklegion` 那 52 个对象就是靠它）。");
            return;
        }
        var sc = JsonUtility.FromJson<ShadowFlagSidecar>(File.ReadAllText(p));
        if (sc == null || sc.items == null)
        {
            Debug.LogWarning($"[Arena] 阴影旁挂解析失败：{p}");
            return;
        }
        int hit = 0, miss = 0;
        foreach (var it in sc.items)
        {
            if (it == null || string.IsNullOrEmpty(it.go)) continue;
            bool any = false;
            if (mf.meshes != null)
                foreach (var e in mf.meshes)
                    if (SameObject(e.go, e.pos, it.go, it.pos))
                    { e.hasShadow = true; e.shadowCast = it.cast; e.shadowReceive = it.receive; any = true; hit++; }
            if (!any) miss++;
        }
        Debug.Log($"[Arena] 阴影旁挂：{sceneName} 有 {sc.items.Length} 条，命中清单里 {hit} 个网格"
                + (miss > 0 ? $"；**{miss} 条在清单里找不到同名同位置的对象**（出声，不猜）" : ""));
    }

    /// <summary>旁挂：**逐场相机的光学参数**（`工具/gen_arena_camera.py` 从原版场景包直读）。</summary>
    [System.Serializable] public class CameraSidecar
    {
        public string arena;
        public float sensorSizeX, sensorSizeY, focalLength; public int gateFit;
    }

    /// <summary>🆕 2026-09-27：把旁挂里的 `sensorSize` 并进 `mf.camera`。
    ///
    /// 🔴 **为什么非要有它**：`m_SensorSize.x` **逐场不同** —— 13 台 `BoardCamera` 里
    ///   `emperorschildren` / `genestealers` / `spacewolves` 是 **37.2**，其余十台 **41.5**，
    ///   而 `ConfigureBoardCamera` 原来**写死 41.5**。`gateFit = Horizontal` 下 `sensorSize.x`
    ///   直接决定 hFOV（vFOV 按画幅联动）⇒ 那三场被**等比放大 10.36%**
    ///   （`tan(vFOV/2)`：41.5→0.41685 · 37.2→0.37366 · 比值 0.8964）。
    ///   这正是文档里记了很久的「**这三场的原版实拍图不能用、与授权取景差 ~10%**」的真因 ——
    ///   🔴 **那条结论已作废**：**实拍图是好的，是我们的相机写错了**（判据 → `资料/战场13场_逐场对账_0920.md` §一 ①-j 结论四）。
    ///   旁挂走的是工程既有的「**旁挂数据 + `工具/gen_arena_*.py`**」那套（**不动 `d:/2/` 的生成器**）。
    /// ⚠️ 缺文件时**用默认 41.5×24 并出声** —— 那是十场的值，对那三场是错的。</summary>
    static void ApplyCameraSidecar(string sceneName, Manifest mf)
    {
        if (mf == null || mf.camera == null) return;
        var p = CameraSidecarPath(sceneName);
        if (!File.Exists(p))
        {
            Debug.LogWarning($"[Arena] 没有相机旁挂 `{p}` ⇒ `sensorSize` 用默认 41.5×24。"
                           + "**EC / genestealers / spacewolves 三场的原版是 37.2**，用默认会把它们放大 10.36%"
                           + "（跑一次 `python 工具/gen_arena_camera.py` 生成）。");
            return;
        }
        var sc = JsonUtility.FromJson<CameraSidecar>(File.ReadAllText(p));
        if (sc == null || sc.sensorSizeX <= 0f)
        {
            Debug.LogWarning($"[Arena] 相机旁挂解析失败：{p}（`sensorSize` 用默认 41.5×24）");
            return;
        }
        mf.camera.sensorSizeX = sc.sensorSizeX;
        mf.camera.sensorSizeY = sc.sensorSizeY;
    }

    /// <summary>同一个场景里，**粒子按贴图名共用一份材质**（见 `GetOrCreateParticleMaterial`）。</summary>
    static readonly Dictionary<string, Material> _psMatCache = new Dictionary<string, Material>();

    /// <summary>🆕 2026-09-21：**自发光设不上去的材质个数** —— URP/Unlit 没有 `_EmissionColor`，
    /// 原版那批（如 sororitas 的 24 个烛光）的自发光会被丢掉。**不许静默**：建完在 `BuildContent` 里报一句。</summary>
    static int _emissionDropped;

    /// <summary>把材质存成资产 —— **路径确定（`Materials/<名字>.mat`）、已有就原地覆盖**。
    ///
    /// 🔴 2026-09-20 修（这条是个**静默**的坑）：原来走 `AssetDatabase.GenerateUniqueAssetPath`
    ///    + `ClearMaterialDir` 每次重建先删光 ⇒ **每次重跑都会换一批 guid**，
    ///    而**早先存过盘的场景还指着旧 guid** ⇒ 那些渲染器的 `sharedMaterial` 变成 null。
    ///    表现：`BattleScene.Run` 报「**34 个粒子的材质没贴图**」—— 看着像贴图丢了，
    ///    其实是**材质整个没了**（`Battle.unity` 存的还是上一次构建的 guid）。
    ///    改成「确定路径 + `CopySerialized` 原地覆盖」之后 guid 稳定，重建不再打翻别的场景。</summary>
    /// <summary>🆕 2026-09-27：**本次构建真正落盘的材质名**。
    /// `ClearMaterialDir` 靠它区分「合法名字」与「`GenerateUniqueAssetPath` 留下的 `… 1.mat`」
    /// —— 原版材质名里本来就有 `PS_Embers 1` / `PS_Lens Flare 1` 这一族，光看「末段是整数」会误删（见那里的说明）。</summary>
    static readonly HashSet<string> _builtMatNames = new HashSet<string>();

    static Material SaveOrReuse(string sceneName, Material fresh)
    {
        _builtMatNames.Add(fresh.name);
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
    /// 确定路径的那批**留着复用**。
    ///
    /// 🔴 **2026-09-27 修一条真缺陷：判据少了「且这个名字不在本次清单里」**（`项目任务.md` §三 第 3 条 第 11 项 ⑤）。
    ///   原版材质名本来就有一族**末段正好是整数** —— `PS_Embers 1` · `PS_Lens Flare 1` ·
    ///   `PS_Glow Sphere 01`（`PS_` + `Sanitize(原版材质名)`，原版名就叫 `Embers 1` / `Lens Flare 1` / `Glow Sphere 01`）。
    ///   光按「末段是整数」判 ⇒ **每次 `BuildAll` 都把这 4 份当垃圾删掉**，后果不只是少几份材质：
    ///   它们下次走 `SaveOrReuse` 的「新建」分支 ⇒ **换一次 guid**，而 `Battle_&lt;场&gt;.unity` 正引用着它们
    ///   ⇒ 漏一步 `BattleScene.BuildAndSaveScene` 就**静默变 null**
    ///   （与 `CLAUDE.md` §三「生成资产别用 `GenerateUniqueAssetPath`」是同一件事）。
    ///
    /// ⚠️ **它现在从「构建开头」挪到「构建末尾」调用**：判据要用的「本次清单」= `_builtMatNames`，
    ///   得等 `SaveOrReuse` 都跑完才有。**目录创建留在开头**（`Directory.CreateDirectory`）。</summary>
    static void ClearMaterialDir(string sceneName)
    {
        var matDir = MatDir(sceneName);
        int n = 0, kept = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { matDir }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (!p.StartsWith(matDir)) continue;
            var stem = Path.GetFileNameWithoutExtension(p);
            int sp = stem.LastIndexOf(' ');
            if (sp <= 0 || !int.TryParse(stem.Substring(sp + 1), out _)) continue;   // 不是 `… <数字>` 形状
            if (_builtMatNames.Contains(stem)) { kept++; continue; }                 // 🔴 本次要用的**合法**名字
            AssetDatabase.DeleteAsset(p); n++;
        }
        if (n > 0)
            Debug.Log($"[Arena] 清掉历史重复材质 {n} 份（`… 1.mat` 那一族）；"
                    + $"另保住 {kept} 份末段是数字的**合法**材质（`PS_Embers 1` 那一族）");
        else if (kept > 0)
            Debug.Log($"[Arena] 保住 {kept} 份末段是数字的合法材质（`PS_Embers 1` 那一族）—— 没误删");
    }

    // ---------- 工具 ----------
    /// <summary>诊断开关 `WF_HIDE` / `WF_ONLY` 的匹配器。三种前缀：
    /// · `=` —— 名字**精确**匹配（场景里有 `Background` / `Background Building 1/2` / `Back background`
    ///   这类近名对象，子串匹配一次全关掉）；
    /// · `@` —— **层级路径后缀**匹配（`Arena/Scenario/Particles/SmokeEffect` 用
    ///   `@Particles/SmokeEffect` 点得到，而 `…/TinyFlames/SmokeEffect` 点不到）；
    /// · 其余 —— 名字**子串**匹配（大小写不敏感）。
    ///
    /// 🔴 **为什么要 `@`**：arena1 里有**两个完全都叫 `SmokeEffect`** 的粒子
    /// （`Scenario/Particles/SmokeEffect` 与 `Scenario/Particles/TinyFlames/SmokeEffect`，
    /// 贴图/参数完全不同）⇒ 2026-09-22 之前只能整片一起关，
    /// 「一次只改一个变量」这条准则就废了。出处：`资料/战场13场_逐场对账_0920.md` §一 第 1 条。</summary>
    static bool GoMatches(GameObject go, string pat)
    {
        var t = pat.Trim();
        if (t.Length == 0) return false;
        if (t[0] == '=')
            return string.Equals(go.name, t.Substring(1), System.StringComparison.OrdinalIgnoreCase);
        if (t[0] == '@')
            return GoPath(go).EndsWith(t.Substring(1), System.StringComparison.OrdinalIgnoreCase);
        return go.name.IndexOf(t, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>对象的**层级路径**（`根/…/自己`）—— 重名对象靠它区分，日志与 `@` 匹配都用它。</summary>
    static string GoPath(GameObject go)
    {
        var sb = new System.Text.StringBuilder(go.name);
        for (var t = go.transform.parent; t != null; t = t.parent)
        {
            sb.Insert(0, "/");
            sb.Insert(0, t.name);
        }
        return sb.ToString();
    }

    /// <summary>🆕 2026-09-28：**「无贴图的粒子要不要建」这一条判据，构建侧与自检侧共用这一份**（判据只此一处）。
    ///
    /// 背景：原版有一批粒子材质**本来就没有贴图**（扭曲/叠加辉光类），我们给它们建 `URP/Particles/Unlit` + 空贴图
    /// 会渲成**不透明白方块** ⇒ 2026-09-20 起一律不建。
    /// 🔴 但**`renderMode = 4 (Mesh)` 是例外**：那条路靠**网格 + `matShader`** 出效果、不吃贴图，
    ///    而这道闸门比 Mesh 分支**早** ⇒ 原来把 `battlearena3` 的 `Necrons Close Monolith Rays` / `…(1)`
    ///    **整族吃掉**（清单 `active=true`、`mesh="Sphere.obj"` 抽得出来，场景里却出现 **0 次** —— 挂很久的老账）。
    /// ⚠️ **豁免只给「网格真的解析得到」的**（`ParticleMesh` 返回 null 照旧不建并出声）。
    /// ⚠️ 自检里那三条计数断言（`BattleScene`）**必须调这一个函数** —— 否则又是「清单要 N / 实得 M」的假红。</summary>
    public static bool NoTexMeshModeOk(ParticleEntry p, string scene)
        => p != null && p.renderMode == 4 && ParticleMesh(p.mesh, scene) != null;

    static void ApplyTransform(Transform t, float[] pos, float[] rot, float[] scale)
    {
        t.localPosition = ToVec3(pos, Vector3.zero);
        t.localRotation = ToQuat(rot);
        t.localScale    = ToVec3(scale, Vector3.one);
    }

    /// <summary>🆕 2026-09-28：**网格的阴影标志**（原来两处都硬写死 `Off / false`）。
    ///
    /// 🔴 **原来那句注释「原版战场是烘焙的，无实时阴影」是错的**：原版 `battlearenablacklegion` 的
    ///   67 个 `MeshRenderer` 里 **52 个 `m_CastShadows = 1` 且 `m_ReceiveShadows = 1`**（其余 15 个是 0/0），
    ///   而且地板/管道用的 shader 名字就叫 **`Everguild/Misc/Unlit shadows receiver`** —— 它**就是来收阴影的**。
    ///   ⇒ 我们关掉投/收之后阴影贴图是空的、地板**全亮**：实测 blacklegion 前景（y≥480 三条带）
    ///   比原版亮 **+29 ~ +37**（该场亮度比 1.074 的大头），逐块差的中位数 **+4.06**。
    ///
    /// ⚠️ **本函数现在只认诊断开关** `WF_SHADOWCAST=1`（两者都开）——
    ///   **正式做法**是逐对象的真值走旁挂（照工程既有的「旁挂 + `gen_arena_*.py`」那套），
    ///   因为原版**不是全场统一**（52 开 / 15 关），一刀切开也会错。
    ///   缺旁挂时退回旧行为（`Off/false`）并**出声**（见 `ApplyShadowSidecar`）。</summary>
    static void ApplyShadowFlags(MeshRenderer mr)
    {
        bool on = System.Environment.GetEnvironmentVariable("WF_SHADOWCAST") == "1";
        ApplyShadowFlags(mr, on, on);
    }

    /// <summary>逐对象的真值由旁挂 `ApplyShadowSidecar` 灌进来（`<场>_shadowflags.json`，按名字+位置匹配）。</summary>
    static void ApplyShadowFlags(MeshRenderer mr, bool cast, bool receive)
    {
        mr.shadowCastingMode = cast ? ShadowCastingMode.On : ShadowCastingMode.Off;
        mr.receiveShadows = receive;
    }

    static Vector3 ToVec3(float[] a, Vector3 fallback)
        => (a != null && a.Length >= 3) ? new Vector3(a[0], a[1], a[2]) : fallback;

    static Quaternion ToQuat(float[] a)
        => (a != null && a.Length >= 4) ? new Quaternion(a[0], a[1], a[2], a[3]) : Quaternion.identity;

    static Color ToColor(float[] a)
        => (a != null && a.Length >= 3)
             ? new Color(a[0], a[1], a[2], a.Length >= 4 ? a[3] : 1f)
             : Color.white;

    static ParticleSystem.MinMaxCurve Curve(float[] mm, float fallback)    {
        if (mm != null && mm.Length >= 2) return new ParticleSystem.MinMaxCurve(mm[0], mm[1]);
        if (mm != null && mm.Length == 1) return new ParticleSystem.MinMaxCurve(mm[0], mm[0]);
        return new ParticleSystem.MinMaxCurve(fallback, fallback);
    }

    /// <summary>🔴 **判断「这条曲线到底有没有数据」只能看键，不能看 `!= null`。**
    ///
    /// 踩过的坑（2026-09-21）：`JsonUtility` 会把 JSON 里的 `null` **物化成一个默认构造的空对象**，
    /// 于是 `p.emissionRateCurve != null` **恒为真** —— 断言按它计数就会「34 个要、34 个建」**假绿**
    /// （实际清单里只有 1 个真有曲线）。⇒ 一律走这个判据。</summary>
    public static bool HasKeys(CurveData cd)
        => cd != null && cd.keys != null && cd.keys.Length > 0;

    /// <summary>`sizeOverLifetime` 里有**任意一条轴**带曲线键（`null` 物化那件事同 `HasKeys`）。</summary>
    public static bool HasSizeCurve(SizeOverLifetimeData s)
        => s != null && (HasKeys(s.x) || HasKeys(s.y) || HasKeys(s.z));

    /// <summary>🆕 2026-09-21 下半场：把清单里的**完整 RGBA 渐变**还原成 `Gradient`。
    /// 原来只建 alpha ⇒ **火焰「白→黄→橙」的渐变全丢**，渲出来永远是一坨白。</summary>
    static Gradient GradientFrom(GradientData gd)
    {
        if (gd == null) return null;
        int nc = gd.colors != null ? gd.colors.Length : 0;
        int na = gd.alphas != null ? gd.alphas.Length : 0;
        if (nc < 2 && na < 2) return null;
        var g = new Gradient();
        var ck = new GradientColorKey[Mathf.Max(nc, 1)];
        var ak = new GradientAlphaKey[Mathf.Max(na, 1)];
        if (nc >= 1)
            for (int i = 0; i < nc; i++)
                ck[i] = new GradientColorKey(new Color(gd.colors[i].r, gd.colors[i].g, gd.colors[i].b), gd.colors[i].t);
        else ck[0] = new GradientColorKey(Color.white, 0f);
        if (na >= 1)
            for (int i = 0; i < na; i++)
                ak[i] = new GradientAlphaKey(gd.alphas[i].a, gd.alphas[i].t);
        else ak[0] = new GradientAlphaKey(1f, 0f);
        g.SetKeys(ck, ak);
        return g;
    }

    /// <summary>🆕 2026-09-21：把清单里的**曲线态** MinMaxCurve 原样还原
    /// （生成器 `curve_raw()` 的产物）。没有曲线数据时退回常量域（`fallback` = 单值字段）。
    ///
    /// 为什么非要有这条：`emissionRate` 原版多为 `(0,1)(0.41,0.35)(0.45,0)(1,0)×15`
    /// —— **只在循环前 ~2.5 秒发射**；拍成一个数（峰值 15）会让存活粒子多 2~3 倍。
    /// 判据与逐对象实测见 `资料/战场13场_逐场对账_0920.md`。</summary>
    static ParticleSystem.MinMaxCurve CurveFrom(CurveData cd, ParticleSystem.MinMaxCurve fallback)
    {
        if (cd == null) return fallback;
        // 🆕 2026-09-21 下半场：常量态（VFX 那几个模块的参数大多是常量）
        // 🔴 **2026-09-22 修：原来写的是 `MinMaxCurve(cd.c, cd.cMin)` —— min/max 传反了**
        //    （`mm()` 里 `c` = `scalar` = **max**、`cMin` = `minScalar` = **min**，
        //    而 `MinMaxCurve(a, b)` 的形参是 `(min, max)`）。
        //    症状：`startRotation`（清单 `TwoConstants(0, 2π)`）建出来是 `constantMin=6.28 / constantMax=0`。
        //    对 `Random.Range` 这种「取区间内随机」的语义**当前看不出差别**（正数负数都在同一区间里），
        //    但它是**错的**，而且会被断言/探针读出来（`BattleScene` 那条 startRotation 断言就是被它判红的）。
        if (cd.isConst) return new ParticleSystem.MinMaxCurve(cd.cMin, cd.c);
        if (!HasKeys(cd)) return fallback;
        var c = new AnimationCurve();
        foreach (var k in cd.keys)
            if (k != null) c.AddKey(k.t, k.v);
        if (c.length == 0) return fallback;
        if (cd.minKeys != null && cd.minKeys.Length > 0)
        {
            var c2 = new AnimationCurve();
            foreach (var k in cd.minKeys)
                if (k != null) c2.AddKey(k.t, k.v);
            if (c2.length > 0)
            {
                // ⚠️ Unity **没有** `MinMaxCurve(mult, min, multMax, max)` 这个四参构造（编译期就报错）。
                //    TwoCurves 只能先拿 `(mult, max)` 建、再改 `mode` 与 `curveMin`。
                var mm2 = new ParticleSystem.MinMaxCurve(cd.mult, c);
                mm2.mode = ParticleSystemCurveMode.TwoCurves;
                mm2.curveMax = c;
                mm2.curveMin = c2;
                return mm2;
            }
        }
        return new ParticleSystem.MinMaxCurve(cd.mult, c);
    }

    /// <summary>读取纹理，找不到就返回 null（用白图兜底）
    /// 🆕 2026-09-21：加一条**兜底路径** —— `WarpforgeVFX/Textures/` 里有同一批原版贴图。
    /// 生成器的 `copy_assets` 漏拷了 `Default-Particle.png`（实测只有 `battlearena2` 与
    /// `battlearenaleviathan` 缺，运行期日志 `[Arena] 找不到贴图：…/Textures/Default-Particle.png`），
    /// 缺了会**按白图渲染**（原版是 64×64 的白色径向团）。**兜底要报出来**，别静默。</summary>
    static Texture GetTexture(string sceneName, string texFile)
    {
        if (string.IsNullOrEmpty(texFile)) return null;
        var path = $"{TexDir(sceneName)}/{texFile}";
        var t = AssetDatabase.LoadAssetAtPath<Texture>(path);
        if (t == null)
        {
            var alt = $"Assets/WarpforgeVFX/Textures/{texFile}";
            t = AssetDatabase.LoadAssetAtPath<Texture>(alt);
            if (t != null)
                Debug.Log($"[Arena] 贴图 `{texFile}` 本场没拷到，用兜底路径 `{alt}`（外观一致；"
                        + "要根治就重跑该场的清单生成器，它的 copy_assets 会补上）");
            else
                Debug.LogWarning($"[Arena] 找不到贴图：{path}（兜底 `{alt}` 也没有）");
        }
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

    /// <summary>
    /// 🆕 **2026-09-21：查「原版网格 shader 为什么整屏洋红」。**
    ///
    /// 上一版探针（<see cref="ProbeArenaShaders"/>）只看了 `Shader.isSupported`，**那是错的读法**：
    /// `isSupported` 只回答「当前平台编不编得出来」，**不回答「这个变体在不在包里」**。
    /// 洋红是 Unity 的**错误 shader** —— **变体选不出来**时才会换上它。
    /// 所以这里逐条量四样（后两样才是真判据）：
    ///   ① `ShaderUtil.GetShaderMessageCount`（编译期消息条数，>0 就是编不过）
    ///   ② 两个 shader 是不是**同一个对象**（`ReferenceEquals` —— 防止 `TryGetShader` 拿回来的是引擎自带那份）
    ///   ③ 材质建出来时**默认开着哪些关键字**（变体是按关键字选的，关键字不对就选不出来）
    ///   ④ 🔴 **`mat.SetPass(0)` 成不成立** —— 变体选不出来时它是 **false**，这才是洋红的直接判据
    ///
    /// 用法：`... -executeMethod ArenaBuilder.ProbeOriginalShader -logFile -`（筛 `[OS]`）
    /// </summary>
    public static void ProbeOriginalShader()
    {
        string[] names = {
            "Everguild/UnlitAmbient", "Everguild/Unlit Wind",
            "Everguild/Misc/Unlit shadows receiver", "Everguild/FX/Unlit UV scroll",
            "Everguild/UnlitAmbient Emissive Flickker",
            "Everguild/FX/Floor Planar Reflections Grainny", "Everguild/FX/Vortex",
            "Everguild/FX/Tyranids/Pulsating Mesh", "Everguild/FX/Tyranids/Tyranid Tentacle",
        };
        int bad = 0;
        foreach (var n in names)
        {
            Shader sh;
            if (!WarpforgeVFX.WarpforgeShaderLoader.TryGetShader(n, out sh) || sh == null)
            { Debug.Log($"[OS] {n,-52} ❌ 取不到"); bad++; continue; }

            int msgs = -1;
            try { msgs = ShaderUtil.GetShaderMessageCount(sh); } catch (System.Exception) { msgs = -2; }

            Debug.Log($"[OS] {n,-52} sh='{sh.name}' isSupported={sh.isSupported} passes={sh.passCount}"
                    + $" 编译消息={msgs}"
                    + $" kw={(sh.keywordSpace != null && sh.keywordSpace.keywordNames != null ? sh.keywordSpace.keywordNames.Length : -1)}");

            var mat = new Material(sh) { name = "OSProbe" };
            bool passOk = false;
            try { passOk = mat.SetPass(0); } catch (System.Exception e) { Debug.Log($"[OS]      SetPass 抛异常: {e.Message}"); }
            Debug.Log($"[OS]      材质 shader='{mat.shader.name}' 关键字=[{string.Join(", ", mat.shaderKeywords)}]"
                    + $" **SetPass(0)={passOk}**");
            if (!passOk) bad++;
            Object.DestroyImmediate(mat);
        }
        // 顺带报告一个**必须知道**的事实：`WarpforgeShaderLoader` 主包里只有 45 个 shader，
        // 而原版 84 个包里一共有 135 个 —— 两个 Tyranid 族**只在 battlesharedresources 里**（没随包）。
        Debug.Log($"[OS] === 探针结束：{names.Length} 个里 **{bad} 个拿不到或用不了** ===");
    }

    /// <summary>
    /// 🆕 **2026-09-21：给网格挂上「运行时用原版 shader 重建材质」的组件**
    /// （<see cref="WarpforgeVFX.ArenaOriginalMaterial"/>）。
    ///
    /// 🔴 **为什么必须运行时**：bundle 里的 Shader **落不了工程资产** —— 建 Material 再
    ///    `AssetDatabase.CreateAsset(mat, …)` 时 **shader 引用会变成空 GUID**，重新导入就是
    ///    `Hidden/InternalErrorShader` ⇒ **整屏洋红**（实测复现：`WF_ORIGSHADER=1 BuildFromCLI` + 渲染 = 全屏洋红）。
    ///    死因是**资产序列化**那一层，**不是渲染**：探针（<see cref="ProbeOriginalShader"/>）实测
    ///    9 个原版 shader 全部 `isSupported=True` · 编译消息 0 · **`SetPass(0)=True`**。
    ///    ⚠️ 旧注释那句「原版这批网格 shader 在我们工程里跑不起来」**是错的**，已就地更正。
    ///
    /// ⚠️ **必须在 `mr.sharedMaterial = &lt;构建期材质&gt;` 之后调** —— 组件重建时要拿那份材质里的**贴图**
    /// （贴图是我们导入好的工程资产，能引用；而 shader 不能）。
    /// </summary>
    /// <summary>把原版 `MaterialFlickerEffect` 挂回它该在的对象上（判据 = `FlickerData.Specs`）。
    /// ⚠️ 名字找不到就**如实报**（不静默）；命中多个时**报出来并用第一个**（原版这两块是唯一的，真撞了要人看）。
    /// 出处与算式见 `WarpforgeVFX.WFMaterialFlicker` 与 `资料/战场13场_逐场对账_0920.md` §一 ①-a。</summary>
    /// <summary>本场闪烁族的**账面**（`AttachFlickers` 每次建场时重写）。**自检读它**
    /// —— 用来把「**本场原版根本就没有闪烁件**」（如 `battlearenasororitas`，`FlickerData` 里 0 条）
    /// 和「该挂的没挂上」**分开**。🔴 2026-09-25 踩过：那条断言原来写的是「本局战场 ≥1 个」，
    /// 而那是 **arena1 的情况** ⇒ 换成 sororitas 必红（它原版就没有）。</summary>
    public static int LastFlickerWant, LastFlickerAttached;
    /// <summary>该挂却找不到对象的那些（原版有、我们没搬 → 见 `项目任务.md` §三 第 3 条 第 9 项）。</summary>
    public static readonly System.Collections.Generic.List<string> LastFlickerMissing
        = new System.Collections.Generic.List<string>();

    static int AttachFlickers(Transform root, Manifest mf)
    {
        int n = 0;
        LastFlickerWant = 0; LastFlickerAttached = 0; LastFlickerMissing.Clear();
        foreach (var spec in FlickerData.Specs)
        {
            if (spec.arena != mf.scene) continue;
            LastFlickerWant++;
            Transform hit = null; int hits = 0;
            FindDeep(root, spec.go, ref hit, ref hits);
            if (hits == 0)
            {
                LastFlickerMissing.Add(spec.go);
                Debug.LogWarning($"[Arena] 🔴 闪烁族：{mf.scene} 里找不到对象 `{spec.go}`"
                               + "（原版这个对象上挂着 `MaterialFlickerEffect`）—— 没挂上");
                continue;
            }
            if (hits > 1)
                Debug.LogWarning($"[Arena] ⚠️ 闪烁族：`{spec.go}` 命中 {hits} 个，用了第一个 `{hit.name}`");
            var fx = hit.GetComponent<WarpforgeVFX.WFMaterialFlicker>();
            if (fx == null) fx = hit.gameObject.AddComponent<WarpforgeVFX.WFMaterialFlicker>();
            fx.amplitude   = spec.amplitude;
            fx.frequency   = spec.frequency;
            fx.fadeInTime  = spec.fadeInTime;
            fx.fadeOutTime = spec.fadeOutTime;
            fx.randomStart = spec.randomStart;
            fx.desync      = spec.desync;
            fx.playOnAwake = true;                 // 38 个原版全是 1（生成器里也只收 6 字段齐全的）
            n++; LastFlickerAttached++;
        }
        return n;
    }

    static void FindDeep(Transform t, string name, ref Transform hit, ref int hits)
    {
        if (t == null) return;
        if (t.name == name) { hits++; if (hit == null) hit = t; }
        for (int i = 0; i < t.childCount; i++) FindDeep(t.GetChild(i), name, ref hit, ref hits);
    }

    static void AttachOriginalMaterial(Renderer r, string sceneName, string shaderName, WarpforgeVFX.MatProp[] props,
                                       int cull, int srcBlend, int dstBlend,
                                       bool transparent, bool alphaClip, bool blendAuthoritative,
                                       int queue = -1, TexSlotEntry[] slots = null)
    {
        // ⚠️ 形参 2026-09-30 从 `MeshRenderer` 放宽成 `Renderer`（**⑨：粒子也要挂**）——
        //    函数体只用得到 `gameObject` / `name`，组件自己会去 `GetComponent` 认渲染器种类。
        if (r == null || string.IsNullOrEmpty(shaderName)) return;
        // 🔴 **清单里没有 `props` 就不挂**（老清单没有这个字段）—— 宁可不换，也不能拿一份
        //    **属性全默认**的材质去顶（实测那会多出一片白块：`_ClipThreshold` 不在就会整块画出来）。
        //    退回构建期那份 `URP/Unlit` 是**已知的、可接受的**状态；换了才是未知的。
        if (props == null || props.Length == 0)
        {
            Debug.LogWarning($"[Arena/OS] {r.name}：清单里没有 `props`（老清单？）⇒ **不挂原版材质**，"
                           + $"保持 URP/Unlit。跑一次 `gen_unity_arena_manifest.py --arena <场>` 补上。");
            return;
        }
        var c = r.gameObject.AddComponent<WarpforgeVFX.ArenaOriginalMaterial>();
        c.shaderName = shaderName;
        c.props = props;                 // ⚠️ **原样带过去** —— `t` 是 "c"(Color) 还是 "v"(Vector4) 都走 `SetVector`
        c.cull = cull; c.srcBlend = srcBlend; c.dstBlend = dstBlend;
        c.transparent = transparent; c.alphaClip = alphaClip; c.blendAuthoritative = blendAuthoritative;
        c.queue = queue;                 // 🆕 2026-09-22 晚：原版写死的 renderQueue（透明物体的绘制顺序）
        c.applyAmbientColor = HasAmbientColorProp(props);
        // 🆕 2026-09-25：**非主贴图槽**（`_NoiseTex1` / `_MinTex` / `_SecondaryTex` / `_MatCap` …）——
        //    必须带到这里（**不能只在建场期贴**：那时材质还是 `URP/Unlit` 兜底、没有这些槽，
        //    实测一次 BuildAll 会打 50 条「没有槽」跳过）。
        if (slots != null && slots.Length > 0)
        {
            var names = new List<string>(); var texs = new List<Texture>();
            foreach (var s in slots)
            {
                if (s == null || string.IsNullOrEmpty(s.slot) || string.IsNullOrEmpty(s.texFile)) continue;
                var t = GetTexture(sceneName, s.texFile);
                if (t == null) { Debug.LogWarning($"[Arena] {r.name}: 槽 `{s.slot}` 的贴图 `{s.texFile}` 找不到"); continue; }
                names.Add(s.slot); texs.Add(t);
            }
            if (names.Count > 0) { c.slotNames = names.ToArray(); c.slotTexs = texs.ToArray(); }
        }
    }

    /// <summary>`_APPLYAMBIENTCOLOR` 在原版材质上是个 float（多为 1）—— 它**同时还是个 keyword**，得单独开。
    /// （原版材质绝大多数都带着它；正本 §五 已坐实它乘的那层因 `_AmbientColorBlend≡0` 是恒等，
    ///  所以开不开画面都一样 —— 这里**照原版开**。）</summary>
    static bool HasAmbientColorProp(WarpforgeVFX.MatProp[] props)
    {
        if (props == null) return false;
        foreach (var p in props)
            if (p != null && p.k == "_APPLYAMBIENTCOLOR" && p.f > 0.5f) return true;
        return false;
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

    // ---- 🆕 2026-09-25：**主贴图之外的贴图槽** -------------------------------------------------
    /// <summary>旁挂文件 `arenas/&lt;场&gt;/&lt;场&gt;_texslots.json` 的一条（由 `工具/gen_arena_texslots.py` 从原版包里按
    /// `pathID` 解出来）。</summary>
    [System.Serializable] public class TexSlotEntry { public string slot; public string tex; public string texFile; }
    [System.Serializable] public class TexSlotMesh  { public string go; public TexSlotEntry[] slots; }
    // 🆕 2026-09-30：多一个 `particles` 键 —— 粒子那一支以前**一条槽都没接**（判据 → `资料/战场场景线_交接.md` §二 ⑨）。
    [System.Serializable] public class TexSlotFile  { public string scene; public TexSlotMesh[] meshes; public TexSlotMesh[] particles; }

    // ---- 🆕 2026-09-30：**网格的原版材质关键字**（旁挂 `gen_arena_meshkeywords.py`）------------------
    /// <summary>一条：某个 GameObject 的材质在原版里**开着**哪些关键字。</summary>
    [System.Serializable] public class MeshKeywordItem { public string go; public string mat; public string[] keywords; }
    [System.Serializable] public class MeshKeywordFile { public string scene; public MeshKeywordItem[] meshes; }

    /// <summary>读 `<场>_meshkeywords.json` → `go → 关键字[]`（文件不在就返回空表；**出声在调用处**）。
    ///
    /// 🔴 **为什么必须有它**：清单的 `meshes[]` **不带 `matKeywords`**（只有 `particles[]` 带）
    ///   ⇒ **网格这一路一个原版关键字都没设过** —— 我们能设的只有 `ApplyRenderState` **自己算的四个**
    ///   （`_SURFACE_TYPE_TRANSPARENT` / `_ALPHABLEND_ON` / `_ALPHATEST_ON` / `_APPLYAMBIENTCOLOR`）。
    ///   **实测受害**：`battlearenaleviathan` 的 `Toxic Pool Glow`（材质 `Toxic Pool Up light`，
    ///   原版 `m_ValidKeywords = ['_SOFT','_SURFACE_TYPE_TRANSPARENT']`）⇒ 我们少了 **`_SOFT`**
    ///   ⇒ 那块大辉光面**把整屏罩成一片亮黄绿**（该场亮度比的三分之二出在它身上；
    ///   诊断开关 `WF_MESHKEYWORDS=_SOFT` 实测修好过 —— 那开关自己写着「**是诊断，不是修法**」）。
    /// 13 场实测面：**684 个网格**带原版关键字（`_APPLYAMBIENTCOLOR` 635 · `_SURFACE_TYPE_TRANSPARENT` 619 ·
    ///   `_ALPHATEST_ON` 80 · **`_USEDISTORT` 68** · `_RECEIVESHADOWS` 17 · `_SOFT` 2 …）。
    /// ⚠️ `WF_MESHKEYWORDS` 那个诊断开关**仍然留着**（A/B 用），两处没冲突。</summary>
    static Dictionary<string, string[]> LoadMeshKeywords(string sceneName)
    {
        var res = new Dictionary<string, string[]>();
        var path = $"{ArenaDir(sceneName)}/{sceneName}_meshkeywords.json";
        if (!File.Exists(path)) return res;
        var f = JsonUtility.FromJson<MeshKeywordFile>(File.ReadAllText(path));
        if (f == null || f.meshes == null) return res;
        foreach (var m in f.meshes)
            if (m != null && !string.IsNullOrEmpty(m.go) && m.keywords != null && m.keywords.Length > 0)
                res[m.go] = m.keywords;
        return res;
    }

    // ---- 🆕 2026-09-25：**SpriteRenderer 那一族** -------------------------------------------------
    /// <summary>`arenas/&lt;场&gt;/&lt;场&gt;_sprites.json` 的一条（`工具/gen_arena_sprites.py` 从原版包里抽）。
    /// 重建器原来**只支持 MeshRenderer / ParticleSystem**，`SpriteRenderer` 全文 0 处 ⇒
    /// `battlearenaastramilitarum` / `battlearenatauviorla` 各 **9 个 `Fake Light Glow`**（远景假光晕）一个都没搬。</summary>
    [System.Serializable] public class SpriteMatEntry
    {
        public string name; public string shader; public long shaderPathId; public int queue = -1;
        public WarpforgeVFX.MatProp[] props;
    }
    [System.Serializable] public class SpriteEntry
    {
        public string go; public string chain;
        public float[] pos; public float[] rot; public float[] scale; public float[] world;
        public string sprite; public string tex; public float ptu;
        public float[] rect; public float[] pivot; public float[] color; public float[] size;
        public int drawMode; public int sortOrder; public int sortLayer;
        public bool flipX; public bool flipY;
        /// <summary>原版 `activeInHierarchy`（沿父链逐层与）。
        /// 🔴 **必须判**：tauviorla 那 9 个 `Fake Light Glow` 原版是 **False**（astra 的 9 个是 True）——
        ///    照清单全建会**多画 9 个发光体**（实测 tauviorla 亮度比 1.017 → **1.029**，反而更差）。
        ///    同粒子那条规矩：**原版关着的，不要建**。</summary>
        public bool active = true;
        public SpriteMatEntry[] mats;
    }
    [System.Serializable] public class SpriteFile { public string scene; public SpriteEntry[] sprites; }

    /// <summary>🆕 2026-09-25：**平面反射**（原版 `kTools.Mirrors.Mirror` 那台 `reflection camera`）。
    /// 数据 = `arenas/&lt;场&gt;/&lt;场&gt;_mirror.json`（工具 `工具/gen_arena_mirror.py`，从原始包按父链算世界镜面）。
    /// **13 场里 8 场有**（arena3 / aeldari / darkangels / emperorschildren / genestealers / sororitas /
    /// spacewolves / tauviorla），参数一致（档 2：`textureScale 0.3` / `blurIterations 1` / grainy）。
    /// 不做的话地板采到默认贴图 ⇒ **反射偏强**（`battlearena3` 关掉反射 1.103 → 1.027）。</summary>
    [System.Serializable] public class MirrorFile
    {
        public string scene; public string go;
        public int quality; public bool enabled; public bool grainy;
        public float textureScale = 0.3f; public int blurIterations = 1;
        // 🆕 2026-09-26：两个 Blit 材质要喂的四个数（原版组件**自己的序列化字段**，逐场读）
        public float depthFadeBottom = 0.832f; public float depthFadeTop = 1f;
        public int shaderIterations = 3; public float blurRadius = 0.002f;
        public float[] pos; public float[] normal;
    }

    static int BuildMirror(string sceneName, GameObject root)
    {
        var path = $"{ArenaDir(sceneName)}/{sceneName}_mirror.json";
        if (!File.Exists(path)) return 0;
        var mf = JsonUtility.FromJson<MirrorFile>(File.ReadAllText(path));
        if (mf == null) return 0;
        if (!mf.enabled)
        {
            Debug.Log($"[Arena] {sceneName}：原版那台平面反射在**本档画质下是关的**（quality {mf.quality}）⇒ 不建");
            return 0;
        }
        var go = new GameObject(string.IsNullOrEmpty(mf.go) ? "reflection camera" : mf.go);
        go.transform.SetParent(root.transform, false);      // 镜面用世界坐标，位置由组件自己镜像，这里不摆
        var c = go.AddComponent<WarpforgeVFX.ArenaMirror>();
        c.planePos    = mf.pos ?? new[] { 0f, 0f, 0f };
        c.planeNormal = mf.normal ?? new[] { 0f, 1f, 0f };
        c.textureScale = mf.textureScale > 0f ? mf.textureScale : 0.3f;
        c.blurIterations = mf.blurIterations;
        c.grainy = mf.grainy;
        c.depthFadeBottom = mf.depthFadeBottom;
        c.depthFadeTop = mf.depthFadeTop;
        c.shaderIterations = mf.shaderIterations;
        c.blurRadius = mf.blurRadius;
        Debug.Log($"[Arena] {sceneName}：平面反射建了 —— 镜面 y={c.planePos[1]:F3} · textureScale={c.textureScale}"
                + $"（原版档 {mf.quality}）· 两级 Blit 参数 depthFade={mf.depthFadeBottom:F3}..{mf.depthFadeTop:F3}"
                + $" · _Iterations={mf.shaderIterations} · _BlurRadius={mf.blurRadius:F4}");
        return 1;
    }

    /// <summary>🆕 2026-09-25：**按画质档开关**的对象（原版 `ObjectTogglerByQuality`）。
    /// 数据 = `arenas/&lt;场&gt;/&lt;场&gt;_qualitytoggle.json`（工具 `工具/gen_arena_quality_toggle.py`，
    /// 按原版反编译 `ObjectTogglerByQuality__OnEnable.c` 的算式在**我们用的档**上算好，只落定论）。
    /// 🔴 **不接就是「两个都建」**：`battlearenaaeldari` 的 `Portal High quality` 与 `Portal Low Quality`
    /// 同名同位置、清单里**都是 `active=True`** ⇒ 门的效果翻倍（档 2 原版只开 High）。</summary>
    [System.Serializable] public class QualityToggleObj { public string go; public bool active = true; }
    [System.Serializable] public class QualityToggleFile { public string scene; public int quality; public QualityToggleObj[] objects; }

    public static HashSet<string> LoadInactiveByQuality(string sceneName)
    {
        var set = new HashSet<string>();
        var path = $"{ArenaDir(sceneName)}/{sceneName}_qualitytoggle.json";
        if (!File.Exists(path)) return set;
        var f = JsonUtility.FromJson<QualityToggleFile>(File.ReadAllText(path));
        if (f == null || f.objects == null) return set;
        foreach (var o in f.objects)
            if (o != null && !string.IsNullOrEmpty(o.go) && !o.active) set.Add(o.go);
        return set;
    }

    /// <summary>精灵贴图**必须按 Sprite 导入**（`pixelsPerUnit` 决定世界尺寸）——
    /// ⚠️ 不能放 `Textures/`：`ApplyTextureImportSettings` 会把那目录里的一切强制设成 `Default`。</summary>
    static Sprite LoadSpriteAsset(string sceneName, string tex, float ptu)
    {
        if (string.IsNullOrEmpty(tex)) return null;
        var path = $"{ArenaDir(sceneName)}/Sprites/{tex}.png";
        if (!File.Exists(path)) { Debug.LogWarning($"[Arena] 精灵贴图不在：{path}"); return null; }
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null && (ti.textureType != TextureImporterType.Sprite
                           || ti.spriteImportMode != SpriteImportMode.Single
                           || Mathf.Abs(ti.spritePixelsPerUnit - ptu) > 0.01f))
        {
            ti.textureType        = TextureImporterType.Sprite;
            ti.spriteImportMode   = SpriteImportMode.Single;   // 原版 `m_Rect` 就是整张图
            ti.spritePixelsPerUnit = ptu;                      // 原版 `m_PixelsToUnits`（这批是 25 / 100）
            ti.spritePivot        = new Vector2(0.5f, 0.5f);   // 原版 `m_Pivot` = 0.5,0.5
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled      = false;
            ti.SaveAndReimport();
        }
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp == null) Debug.LogWarning($"[Arena] 读不出 Sprite：{path}");
        return sp;
    }

    /// <summary>精灵用哪份材质。
    ///
    /// 🔴 **建场期一律用「工程里能存进场景」的兜底 shader**（`URP/Particles/Unlit`），
    ///    **绝不在这里用原版 shader** —— 原版 shader 是**运行时从 bundle 加载**的（落不成工程资产），
    ///    写进场景会在存盘时丢掉 ⇒ 渲染成**品红色块**（2026-09-25 实测：astra 的 9 个光晕
    ///    在预览里全是大粉色矩形）。原版那份走 `ArenaOriginalMaterial` **运行时重建**（与网格同一条路）。
    /// ⚠️ 材质**没解出来**的（清单 `mats` 为空）⇒ **如实报出来**，不许当它照原版建了。</summary>
    static Material GetOrCreateSpriteMaterial(string sceneName, SpriteEntry e)
    {
        var sm = (e.mats != null && e.mats.Length > 0) ? e.mats[0] : null;
        if (sm == null)
            Debug.LogWarning($"[Arena] {e.go}: 清单里没有材质（原版那个材质对象在所有解包 bundle 里都没落盘）"
                           + $" ⇒ 用 URP/Particles/Unlit 兜底，属性是空的 —— **不是原版值**");
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        var mat = new Material(sh) { name = (sm != null ? sm.name : e.go) + "_sprite" };
        if (e.color != null && e.color.Length >= 3 && mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", ToColor(e.color));
        if (sm != null && sm.props != null && sm.props.Length > 0)
        {
            WarpforgeVFX.ArenaOriginalMaterial.ApplyProps(mat, sm.props);
            WarpforgeVFX.ArenaOriginalMaterial.ApplyRenderState(
                mat, 2, (int)PropF(sm.props, "_SrcBlend", 5f), (int)PropF(sm.props, "_DstBlend", 10f),
                transparent: true, alphaClip: false, blendAuthoritative: true,
                applyAmbientColor: false, queue: sm.queue);
        }
        return mat;
    }

    static float PropF(WarpforgeVFX.MatProp[] props, string key, float fallback)
    {
        if (props == null) return fallback;
        foreach (var p in props) if (p != null && p.k == key && p.t == "f") return p.f;
        return fallback;
    }

    /// <summary>把 `_sprites.json` 里的对象建出来（挂在 `root` 下）。返回建成功的个数。</summary>
    static int BuildSprites(string sceneName, GameObject root)
    {
        var path = $"{ArenaDir(sceneName)}/{sceneName}_sprites.json";
        if (!File.Exists(path)) return 0;
        var f = JsonUtility.FromJson<SpriteFile>(File.ReadAllText(path));
        if (f == null || f.sprites == null) return 0;
        int n = 0, nSkip = 0, nInactive = 0;
        foreach (var e in f.sprites)
        {
            if (e == null || string.IsNullOrEmpty(e.go)) continue;
            if (!e.active) { nInactive++; continue; }        // 原版关着的，不建（见 SpriteEntry.active）
            var sp = LoadSpriteAsset(sceneName, e.tex, e.ptu);
            if (sp == null) { nSkip++; continue; }
            var go = new GameObject(e.go);
            go.transform.SetParent(root.transform, false);
            ApplyTransform(go.transform, e.pos, e.rot, e.scale);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite        = sp;
            sr.color         = ToColor(e.color);
            sr.flipX         = e.flipX;
            sr.flipY         = e.flipY;
            sr.sortingOrder  = e.sortOrder;
            sr.sortingLayerID = e.sortLayer;
            sr.drawMode      = (SpriteDrawMode)Mathf.Clamp(e.drawMode, 0, 3);
            sr.sharedMaterial = GetOrCreateSpriteMaterial(sceneName, e);
            // 原版那份材质 → 运行时用原版 shader 重建（与网格同一条路：见 ArenaOriginalMaterial）
            var sm2 = (e.mats != null && e.mats.Length > 0) ? e.mats[0] : null;
            if (sm2 != null && sm2.props != null && sm2.props.Length > 0 && !string.IsNullOrEmpty(sm2.shader))
            {
                var c = go.AddComponent<WarpforgeVFX.ArenaOriginalMaterial>();
                c.shaderName = sm2.shader;
                c.props = sm2.props;
                c.cull = 2;
                c.srcBlend = (int)PropF(sm2.props, "_SrcBlend", 5f);
                c.dstBlend = (int)PropF(sm2.props, "_DstBlend", 10f);
                c.transparent = true; c.alphaClip = false; c.blendAuthoritative = true;
                c.queue = sm2.queue;
            }
            n++;
        }
        Debug.Log($"[Arena] {sceneName}：精灵（SpriteRenderer）建了 {n} 个"
                + (nInactive > 0 ? $"，**原版关着不建 {nInactive} 个**" : "")
                + (nSkip > 0 ? $"，跳过 {nSkip} 个（贴图读不出）" : ""));
        return n;
    }

    /// <summary>读旁挂的贴图槽表（没有就返回空表 —— 老场照旧）。</summary>
    /// <summary>读 `<场>_texslots.json`。返回**网格**那份（按 `go` 索引）；
    /// 传了 `particles` 就顺带把**粒子**那份填进去（🆕 2026-09-30）。
    ///
    /// 🔴 **为什么要分开两份**：网格与粒子可能**撞名**（都叫 `Glow` / `Light` 这种），所以各存各的。
    /// 谁会用到粒子那份：粒子循环里 `AttachOriginalMaterial(..., slots)` ——
    /// ⑨ 之后粒子改用**原版 shader** 重建，`_EmissionMap` / `_SecondaryTex` / `_DistortTex` /
    /// `_NoiseTex1,2` / `_BumpMap` / `_MinTex` / `_Mask` 这些槽**第一次真正被采样**，
    /// 不接就采样 shader 自带的默认图（多为白）。13 场普查：40 个槽、原来一个都没接。</summary>
    static Dictionary<string, TexSlotEntry[]> LoadTexSlots(string sceneName,
                                                           Dictionary<string, TexSlotEntry[]> particles = null)
    {
        var res = new Dictionary<string, TexSlotEntry[]>();
        var path = $"{ArenaDir(sceneName)}/{sceneName}_texslots.json";
        if (!File.Exists(path)) return res;
        var f = JsonUtility.FromJson<TexSlotFile>(File.ReadAllText(path));
        if (f == null) return res;
        if (f.meshes != null)
            foreach (var m in f.meshes)
                if (m != null && !string.IsNullOrEmpty(m.go)) res[m.go] = m.slots;
        if (particles != null && f.particles != null)
            foreach (var m in f.particles)
                if (m != null && !string.IsNullOrEmpty(m.go)) particles[m.go] = m.slots;
        return res;
    }

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
        // 🔴 **2026-09-21 实测的坏消息**：`Universal Render Pipeline/Unlit` **属性表里根本没有
        //    `_EmissionColor`**（实读包内 `Shaders/Unlit.shader`：只有 `_BaseMap`/`_BaseColor`/`_Cutoff`/…），
        //    所以下面那两行 **`HasProperty` 恒为假 ⇒ 自发光一直被静默丢掉**。
        //    受影响最明显的是 sororitas 的 **24 个烛光材质**（原版 shader 是
        //    `Everguild/UnlitAmbient **Emissive Flickker**`，自发光属性叫 **`_EmissiveColor`**，多一个 s；
        //    生成器原来也只读 `_EmissionColor` ⇒ **连清单里都没这个值**，两边都漏）。
        //    ⇒ **现在改成「读得到就设、读不到就报出来」**，不再装作设过了。
        var em = ToColor(emission);
        if (em.maxColorComponent > 0.001f)
        {
            if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", em); }
            if (mat.HasProperty("_EmissiveColor")) { mat.EnableKeyword("_EMISSIVE_ON"); mat.SetColor("_EmissiveColor", em); }
            if (!mat.HasProperty("_EmissionColor") && !mat.HasProperty("_EmissiveColor"))
                _emissionDropped++;
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
            // ⚠️ **这条只是 A/B 用的备用路**（`WF_ORIGSHADER=1`，默认关）——
            //    正路是挂在网格上的 `ArenaOriginalMaterial` 组件（**运行时**重建，见 `AttachOriginalMaterial`）。
            //    这里留着是为了能一键回退对比；**它存盘后必死**（bundle shader 的引用会变空 GUID ⇒ 整屏洋红），
            //    所以**不要**把它当产品路径。
            // 渲染状态的判据**共用一份**：`ArenaOriginalMaterial.ApplyRenderState`。
            // ⚠️ **不能调 `MakeTransparent`** —— 那个函数是按 URP/Unlit 的属性写的。
            WarpforgeVFX.ArenaOriginalMaterial.ApplyRenderState(
                mat, cull, srcBlend, dstBlend, wantTransparent, alphaClip,
                blendAuthoritative, ArenaApplyAmbientColor);
        }
        else if (wantTransparent)
        {
            MakeTransparent(mat);
        }

        // 🔴 **2026-09-21 修：这一句原来根本不存在 —— `MakeAlphaClip` 是**死代码**（全仓零调用者）。**
        //    ⇒ 清单里 `alphaClip: true` 的网格（`Background Building 1/2`、以及所有用共享图集
        //    `BattleArena1 Texture Baked`（**71.6% 全透明**）的道具 —— 沙袋/油桶/炮/旗 …）
        //    **材质上 `_AlphaClip` 还是 0、也没有 `_ALPHATEST_ON`** ⇒ Unity 把整张图当不透明画，
        //    **全透明区就按它自己的 RGB 显出来**。
        //    **实测症状**（arena1 并排图）：每个道具外面套一圈**亮灰矩形**（透明区 RGB 恰好
        //    **(240,240,240)**，乘完环境光 = 画面上那 203~207 的灰）；那块叫 `Background` 的板
        //    更是把**整片天空**盖掉了 ⇒ **这就是「我们几乎没有高光」的真因**：
        //    原版 13 场最高 234~254，我们一律 197~209 —— **因为我们最亮的像素就是这块灰板**。
        //    裁剪与透明混合**互斥**（原版也是二选一）⇒ `wantTransparent` 为真时优先混合，不裁剪。
        if (!wantTransparent && alphaClip) MakeAlphaClip(mat);

        cache[key] = mat;
        return SaveOrReuse(sceneName, mat);
    }

    /// <summary>`URP/Particles/Unlit` 声明为 `shader_feature_local(_fragment)` 的那一族关键字 ——
    /// 取自 `ParticlesUnlit.shader` 的 pragma 表。重建粒子材质时**先全关掉、再照原版列表打开**。
    ///
    /// 🔴 **已知极限：这一族里的 `_EMISSION` 存不进 `.mat`**（2026-09-24 用五个探针才钉死，
    ///   判据全文 → `资料/已知的坑.md`「Unity 在进程里存不住粒子材质的 `_EMISSION` 关键字」）：
    ///   三条 API（`EnableKeyword` / `SetKeyword(LocalKeyword)` / `shaderKeywords =`）内存里都成、
    ///   存盘就丢；进程内写文本 + `ImportAsset` 会被内存对象覆盖回 `[]`；
    ///   关掉 Unity 用 python 改文件能改对、Unity 也读得到，**但它退出时又写回 `[]`**。
    ///   ⚠️ **同批另外四条（`_FLIPBOOKBLENDING_ON` / `_SOFTPARTICLES_ON` / `_FADING_ON` /
    ///   `_SURFACE_TYPE_TRANSPARENT`）和全部属性值都存得住** ⇒ 不是写法问题，是这一个关键字。
    ///   后果：原版蒸汽靠 `_EMISSION` 每通道加 `_EmissionColor`(0.4811)，我们加不上
    ///   （固定种子实测全图均值 **89.5 vs 138.4**）。**这条还没解**，两条候选下一脚见上。</summary>
    static readonly string[] ParticleManagedKeywords = {
        "_EMISSION", "_FLIPBOOKBLENDING_ON", "_SOFTPARTICLES_ON", "_FADING_ON", "_DISTORTION_ON",
        "_ALPHATEST_ON", "_SURFACE_TYPE_TRANSPARENT", "_ALPHAPREMULTIPLY_ON", "_ALPHAMODULATE_ON",
        "_COLOROVERLAY_ON", "_COLORCOLOR_ON", "_COLORADDSUBDIFF_ON",
    };

    /// <summary>原版关键字名 → 我们 shader 认的名字。目前**只有一条**（`项目任务.md` §三 第 3 条 第 11 项 ⑧）。
    ///
    /// 🔴 **`_SOFTPARTICLES` → `_SOFTPARTICLES_ON`**：原版 **Everguild 系**那几个 shader
    ///   （`Everguild/FX/Extra Color` · `Everguild/FX/Particle Distortion Affect Transparents`）
    ///   开的关键字拼写是 **`_SOFTPARTICLES`**（无 `_ON`），而我们现在给粒子用的
    ///   `Universal Render Pipeline/Particles/Unlit` 认的是 **`_SOFTPARTICLES_ON`**
    ///   （自建的 `WarpforgeVFX/Particles/Extra Color` 也一样：`[Toggle(_SOFTPARTICLES_ON)] _SOFTPARTICLES`）。
    ///   照搬原名 ⇒ **关键字设了但不生效** ⇒ 那几颗本该是软粒子的不是。
    ///
    /// **实测面（2026-09-27，13 场清单全量）**：原版带 `_SOFTPARTICLES` 的 **32 条**（Everguild 系）·
    ///   带 `_SOFTPARTICLES_ON` 的 **84 条**（URP 那批，本来就对）。
    /// ⚠️ `ParticleManagedKeywords` 里只有 `_ON` 那一条 ⇒ 先关再开**不会互相打架**；
    ///   也因为它不在那张表里，**不映射的话原名会一直挂在那儿**（不会报错，只会静默不生效）。</summary>
    static string MapParticleKeyword(string k)
        => k == "_SOFTPARTICLES" ? "_SOFTPARTICLES_ON" : k;

    /// <summary>粒子材质：**照「原版材质」的身份 + 整张属性表 + 关键字重建**（`URP/Particles/Unlit`）。
    ///
    /// 🔴 **2026-09-24 重写。** 原来只接一个 `matColor`、其余全是我们硬编 ⇒ 这是两条实测缺陷的**共同根因**
    /// （判据全文：`资料/战场13场_逐场对账_0920.md` §一 ①-c 与 `资料/已知的坑.md`）：
    ///   · **关键字**：原版 `SmokeySteam01` 开着 `_EMISSION` + `_EmissionColor = (0.4811,…,1)`，
    ///     而 URP `ShaderLibrary/Unlit.hlsl:22` 是 `finalColor = half4(albedo + surfaceData.emission, alpha)`
    ///     ⇒ **每通道加 0.4811**。我们没接 ⇒ arena1 那团蒸汽渲成**深灰**（原版是白的；全图均值 89.5 vs 138.4）。
    ///   · **混合**：原版 `Embers 1` 是 `_Blend=2 / _DstBlend=1(One)`（**加性**），我们一律 `DstBlend=10`
    ///     ⇒ `Embers` / `Bullets Controller` 那族火花被渲成 alpha 混合、暗一大截。
    ///     （`ParticlesUnlit.shader` 的 ForwardLit 是 `Blend[_SrcBlend][_DstBlend],[_SrcBlendAlpha][_DstBlendAlpha]`
    ///      —— **间接寻址**，所以设 float 真的管用；原版还带 `_SrcBlendAlpha=1` 预乘，我们原来也没设。）
    ///   · **颜色**：`_BaseColor` 走 `ApplyProps`（内部是 **`SetVector`**）—— **不能用 `SetColor`**：
    ///     工程是 Linear，`SetColor` 会**再做一次 sRGB→线性**（见 `ArenaOriginalMaterial.ApplyProps` 的注释）。
    ///
    /// ⚠️ **材质身份 = 原版材质名**（`ParticleEntry.matName`，如 `SmokeySteam01` / `SmokeLight Fade` / `Embers 1`）
    /// —— 「**一个原版材质 → 一份我们的材质**」。之前按「贴图」或「贴图×颜色×翻页」推，那个维度**漏了材质本身**
    /// （先踩「太粗：互相覆盖」，后踩「太细」，两次都见 `资料/已知的坑.md`）；用原版材质名则天然对齐
    /// `SaveOrReuse` 的「按名字落盘」。</summary>
    static Material GetOrCreateParticleMaterial(string sceneName, ParticleEntry p)
    {
        string texName = p.texFile;
        // 老清单没有 `matName` ⇒ 退回「贴图+主色」当身份，并**出声**（不许静默）。
        string ident = !string.IsNullOrEmpty(p.matName)
                     ? p.matName
                     : texName + "_" + ((p.matColor != null && p.matColor.Length >= 3)
                                        ? $"{p.matColor[0]:F3}" : "-");
        string key = sceneName + "|" + ident;
        Material cached;
        if (_psMatCache.TryGetValue(key, out cached) && cached != null) return cached;

        // ⚠️ 必须用 **Particles/Unlit**，不能用 `URP/Unlit`：后者**不乘粒子顶点色**，
        //    于是 startColor 里的灰度/透明度全丢，烟和蒸汽渲出来是一坨白方块（实拍踩过）。
        //
        // 🔴 **2026-09-27（②）：改成走工程已有的解析链 `WarpforgeShaderMap.TryResolve`，不再硬写。**
        //   原来一律 `Shader.Find("Universal Render Pipeline/Particles/Unlit")` ⇒ **非 URP 的那批
        //   原版 shader 全被换掉**，而 `WarpforgeShaderMap` 里**早就有**它们该去哪一条。
        //   实测面（2026-09-27 逐场清单统计，**12 场 / 52 个「场×材质」/ 216 颗粒子**）：
        //     · `Everguild/FX/Extra Color`（17 材质 / 155 颗）→ `WarpforgeVFX/Particles/Extra Color`
        //     · `Mobile/Particles/Additive`（6 材质 / 30 颗）→ 白名单**走原版 bundle 原件**
        //     · `Everguild/FX/Alpha Mask One Layer`（3/8）· `…Alpha Masks Two Layer`（1/2，`Sand Storm Dust`）
        //     · `Everguild/FX/Particle Distortion Affect Transparents`（2/9）· `Everguild/UnlitAmbient`（2/2）
        //     · `Everguild/FX/Specific/Necrons Rays`（1/2）· `Spine/Special/HiddenPass`（1/2）
        //     · `Everguild/FX/Alpha Mask One Layer  Color Ramp`（1/1）· `<null>`（1/3）
        //   ⚠️ **判据只此一处** —— 解析链（自建替换表 → 改走原件白名单 → 工程自带同名 → 原版 bundle）
        //   全在 `WarpforgeShaderMap.TryResolve` 里，**这里只转发、不另写一套**（同 `CLAUDE.md` §三
        //   「两处写同一条规则 = 迟早不一致」）。
        //   ⚠️ **解析不到就退回 URP/Particles/Unlit 并出声**（不许静默换 shader）。
        //   🔴🔴 **2026-09-27 实测踩到的硬约束：**这里**只能用「工程资产 shader」**（解析来源 = 自建 / 工程自带），
        //   **不能用 `UseOriginal` 白名单那条（原版 bundle）** —— 材质是要 `AssetDatabase.CreateAsset` 落盘的，
        //   而 **bundle 里的 shader 落不了工程资产**（同 `资料/` 里那条已知的坑；`Shader.Find` 也拿不到非工程资产）。
        //   **症状（本轮 A/B 当场抓的，不是推测）**：解析到白名单的 `Sand Storm` / `Toxic Pool Vapor` /
        //   `Sand Storm Intense` 三份写盘时报
        //   `Could not extract GUID in text file …/PS_Sand Storm.mat at line 11`，
        //   `m_Shader` 成了悬空引用 ⇒ 那几颗粒子渲染错 ⇒ **arena2 亮度比 1.022 → 1.084（被推出 ±5%）**。
        //   （运行时那台 `ArenaOriginalMaterial.Rebuild` 走 bundle **是对的** —— 它不落盘，只挂在 Renderer 上。）
        //   ⇒ 白名单那批（`Mobile/Particles/Additive` 等 8 个内置名 + Everguild 原件）在这个路径上**照旧兜底**。
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        string shaderSrc = "兜底 URP/Particles/Unlit";
        if (WarpforgeVFX.WarpforgeShaderMap.TryResolve(p.matShader, out var resolved, out var rsrc)
            && resolved != null
            && (rsrc == "自建" || rsrc == "工程自带"))
        {
            shader = resolved; shaderSrc = rsrc;
        }
        else if (!string.IsNullOrEmpty(p.matShader))
        {
            Debug.Log($"[Arena/PM] {sceneName}/{p.go}：原版 shader `{p.matShader}` 只解析到 `{rsrc}`"
                    + " —— **那是 bundle 里的 shader，落不了 `.mat` 资产** ⇒ 退回 `URP/Particles/Unlit`"
                    + "（属性名可能对不上，这一颗的观感会偏；运行时那台不受影响）。");
        }
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        var mat = new Material(shader) { name = "PS_" + Sanitize(ident) };
        if (shaderSrc != "兜底 URP/Particles/Unlit")
            Debug.Log($"[Arena/PM] {sceneName}/{p.go}：材质 `{ident}` 用 `{shader.name}`（解析来源：{shaderSrc}）。");
        var tex = GetTexture(sceneName, texName);
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }

        if (p.matProps != null && p.matProps.Length > 0)
        {
            WarpforgeVFX.ArenaOriginalMaterial.ApplyProps(mat, p.matProps);
            mat.renderQueue = (int)RenderQueue.Transparent;   // 原版这批是 3000（`m_CustomRenderQueue` = −1 ⇒ 用 shader tag）
        }
        else
        {
            Debug.LogWarning($"[Arena/PM] {sceneName}/{p.go}：清单里没有 `matProps`（老清单？）"
                           + " ⇒ 退回老行为（硬编 alpha 混合 + 只设 `_BaseColor`）。"
                           + "**重跑生成器**才能拿到原版的关键字 / 混合 / 自发光。");
            if (p.matColor != null && p.matColor.Length >= 3)
            {
                var c = new Vector4(p.matColor[0], p.matColor[1], p.matColor[2],
                                    p.matColor.Length >= 4 ? p.matColor[3] : 1f);
                if (mat.HasProperty("_BaseColor")) mat.SetVector("_BaseColor", c);   // 不是 SetColor，见 summary
                if (mat.HasProperty("_Color"))     mat.SetVector("_Color", c);
            }
            MakeTransparent(mat);
        }

        var reused = SaveOrReuse(sceneName, mat);

        if (p.matProps != null && p.matProps.Length > 0)
        {
            // 关键字：**先把这一族关掉，再照原版列表打开**。
            // 🔴 **已知极限（2026-09-24 用五个探针钉死，见 `资料/已知的坑.md`）**：
            //   这三条 API 都能把关键字写进内存、**其中 4 条（`_FADING_ON` / `_FLIPBOOKBLENDING_ON` /
            //   `_SOFTPARTICLES_ON` / `_SURFACE_TYPE_TRANSPARENT`）能存进 `.mat`**，
            //   **但 `_EMISSION` 存不住** —— Unity 把材质写回文件时它是空的。
            //   后果：原版蒸汽靠 `_EMISSION` 每通道**加 `_EmissionColor`(0.4811)**，我们加不上
            //   （固定种子实测全图均值 89.5 vs 138.4）。**这条还没解**，两条候选路见 `项目任务.md` §三 第 3 条。
            foreach (var k in ParticleManagedKeywords) reused.DisableKeyword(k);
            if (p.matKeywords != null)
                foreach (var k in p.matKeywords)
                {
                    if (string.IsNullOrEmpty(k)) continue;
                    string kk = MapParticleKeyword(k);
                    if (kk != k)
                        Debug.Log($"[Arena/PM] {sceneName}/{p.go}：原版关键字 `{k}` 映射成 `{kk}`"
                                + "（原版 Everguild 系写 `_SOFTPARTICLES`，URP 的 ParticlesUnlit 与我们自建的"
                                + " `WarpforgeVFX/Particles/Extra Color` 认的都是 `_SOFTPARTICLES_ON`）。");
                    reused.EnableKeyword(kk);
                }
            EditorUtility.SetDirty(reused);

            // 🆕 **2026-09-25：legacy 内置粒子 shader / 内置 Standard 的「残留混合值」必须丢掉，改从原版 shader 名推断。**
            //
            // 原版有些材质是**用 Standard 建的、后来换了 shader**（换成 `Mobile/Particles/Additive` 这类
            // **把混合写死在 pass 里**的 legacy 粒子 shader）⇒ 材质上仍留着 `_SrcBlend=1/_DstBlend=0`，
            // **全是死值**。而 URP 的 `ParticlesUnlit.shader` 是 `Blend[_SrcBlend][_DstBlend]` **间接寻址**
            // ⇒ 照搬死值 = **不透明**（`CLAUDE.md` §三 那条「原版材质上带着内置 Standard 的残留值」的同一个坑）。
            //
            // **踩过（2026-09-25 查 ⑪ 几场拆因时抓的）**：arena3 `Additional Glow`（材质 `citclr_light`，
            // 原版 shader `Mobile/Particles/Additive`）渲成**一大块绿方块**，逐块有符号差 **+107.75** ——
            // `ring4` 那张图是**靠 alpha 成环**的，不透明渲染把整块 quad 的绿色铺满。
            // ⚠️ **放在关键字循环之后**（`SetBlend` 会开 `_SURFACE_TYPE_TRANSPARENT`，
            //    放前面会被上面那个 `DisableKeyword` 循环关掉）。
            // **判据只此一处**：`WarpforgeVFX.ArenaOriginalMaterial.ShouldInferParticleBlend`（与网格那条路共用）。
            if (WarpforgeVFX.ArenaOriginalMaterial.ShouldInferParticleBlend(p.matProps, p.matShader))
            {
                int sb, db; float zw; bool tr;
                WarpforgeVFX.ArenaOriginalMaterial.InferBlendFromShaderName(p.matShader, out sb, out db, out zw, out tr);
                WarpforgeVFX.ArenaOriginalMaterial.SetBlend(reused, sb, db, zw);
                Debug.Log($"[Arena/PM] {sceneName}/{p.go}：材质 `{p.matName}`（原版 shader `{p.matShader}`）"
                        + "⇒ 混合**不从 props 取**（legacy 内置粒子 shader 把混合写在 pass 里 / props 是内置 Standard 的残留值），"
                        + $"按 shader 名推断成 `Blend {sb}/{db}`（zwrite {zw} · transparent {tr}）。");
                EditorUtility.SetDirty(reused);
                // 🔴 **不许静默失败：写完当场回读**（2026-09-27 加）。
                //    起因：磁盘上 `PS_Fire1.mat` / `PS_citclr_light.mat` 的 `_DstBlend` 是 **10**、`_SrcBlend` 是 **5**、
                //    `_DstBlendAlpha` 是 **10** —— **全是 shader 的默认值**，也就是 `SetBlend` 这次的写入看着**没生效**，
                //    而日志说它「推断成 5/1」⇒ **只有这一条回读能把它和「写进去了」区分开**。
                //    ⚠️ 判据：`HasProperty` 为真时必须回读得到刚写的值；为假要**说出来**（那说明这颗 shader 不吃这一套）。
                if (reused.HasProperty("_DstBlend"))
                {
                    float back = reused.GetFloat("_DstBlend");
                    if (Mathf.Abs(back - db) > 0.5f)
                        Debug.LogWarning($"[Arena/PM] {sceneName}/{p.matName}：`SetBlend` 写了 `_DstBlend={db}`，"
                                       + $"**回读却是 {back}** ⇒ 写没生效（shader `{shader.name}`）。");
                }
                else
                {
                    Debug.LogWarning($"[Arena/PM] {sceneName}/{p.matName}：shader `{shader.name}` **没有 `_DstBlend` 属性**"
                                   + $" ⇒ 推断出来的 `Blend {sb}/{db}` 落不到它身上（这类 shader 的混合写在 pass 里，"
                                   + "只能靠换 shader，靠设 float 是没用的）。");
                }
            }
        }

        // 🆕 2026-09-27（①）：**队列 = 3000 + 原版 `_QueueOffset`**（原来一律写死 3000）。
        //
        // **判据（2026-09-27 逐材质实读 `assets_full/**/Material/*.json`，9/9 全中）**：
        //   `m_CustomRenderQueue` = **3000 + `_QueueOffset`**
        //   —— TinyFlame 5→3005 · SmokeySteam No Color 3→3003 · Embers Circle 4→3004 ·
        //      Glow Additive Soft 4→3004 · wispySmoke Black 1→3001 · SmokeySteam Black 3→3003 ·
        //      SandParticle 1→3001 · **Smoke02 −5→2995**（负号也对上）· SandParticle 0→3000。
        //   3000 = 这批 shader 的 `Queue` tag（Transparent）。
        //   🔴 **队列会改绘制顺序** —— 粒子之间谁压谁由它定，所以不能一律 3000。
        //
        // 🔴 **为什么不信 `p.matQueue`**（这同时结掉了名单上「`matQueue` 为什么粒子这条全是 −1」那条悬案）：
        //   它由生成器从 **`mats[0].custom_queue`** 读（`gen_unity_arena_manifest.py:1413`），
        //   而 `TinyFlame` 那类材质的**本体不在本场包里**（在 `bundle_battlesharedresources_assets_all`）
        //   ⇒ 读不到 ⇒ 一律落到 `-1`（**实测 527/578 条是 −1**，有真值的只有「材质就在本包内」那 51 条）。
        //   网格那条对，正是因为场景材质大多在本包内。
        //   ⇒ 这里改从 `matProps` 取 `_QueueOffset`（那份**按 pathID 解外链**，拿得到真值）；
        //   **两个来源打架就出声**（不许静默）。⚠️ 生成器那条根因**仍在**，它只影响 `matQueue` 这一列。
        int qoff = 0; bool hasQoff = false;
        if (p.matProps != null)
            foreach (var pr in p.matProps)
                if (pr != null && pr.k == "_QueueOffset") { qoff = (int)pr.f; hasQoff = true; break; }
        int wantQueue = 3000 + qoff;
        if (p.matQueue >= 0 && p.matQueue != wantQueue)
            Debug.LogWarning($"[Arena/PM] {sceneName}/{p.go}：材质 `{p.matName}` 的**队列两个来源打架** —— "
                           + $"生成器 `matQueue={p.matQueue}` vs `3000+_QueueOffset`={wantQueue}"
                           + $"（offset={(hasQoff ? qoff.ToString() : "属性表里没有")}）；按后者。");
        // 「不透明」那一档（`SetBlend` 会把它设成 −1）不套队列。
        if (reused.renderQueue >= 0) reused.renderQueue = wantQueue;

        _psMatCache[key] = reused;
        return reused;
    }

    /// <summary>Alpha 裁剪（对应原版 `_ALPHATEST_ON` 的材质，如背景建筑板、共享图集里的各种道具）。
    ///
    /// 🔴 **2026-09-21 修：这个函数原来一次都没被调用过（死代码）** —— 见调用点那段说明。
    /// ⚠️ 阈值要**两个属性名都试**：URP/Unlit 叫 `_Cutoff`，而**原版 `Everguild/UnlitAmbient` 叫
    ///    `_ClipThreshold`**（`工具/dump_shader.py` 实读：属性表里有 `_ClipThreshold Range def=0.5`、
    ///    **根本没有 `_Cutoff`**）—— 只设 `_Cutoff` 的话用原版 shader 时阈值完全没被设，
    ///    靠默认值 0.5 蒙对（今天是对的，但那是巧合，别留着）。</summary>
    static void MakeAlphaClip(Material mat)
    {
        if (mat.HasProperty("_Surface"))   mat.SetFloat("_Surface", 0f);   // 仍是 Opaque
        if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 1f);
        if (mat.HasProperty("_Cutoff"))        mat.SetFloat("_Cutoff", 0.5f);
        if (mat.HasProperty("_ClipThreshold")) mat.SetFloat("_ClipThreshold", 0.5f);
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
