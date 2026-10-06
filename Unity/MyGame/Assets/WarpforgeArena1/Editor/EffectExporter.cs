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
    // 🆕 2026-10-03：`BundleDir` / 几个目录常量 / 一批导入助手**改成 `public`**（**只放开可见性、行为一字未改**）
    //   —— 供 `BoosterPackExporter`（卡包开包窗那件）复用。理由：贴图导入设置、渲染队列真值表、
    //   关键字剥离这些是**同一条规则**，抄第二份就是 CLAUDE.md §三 那条「两处写同一条规则 = 迟早不一致」。
    //   ⚠️ 产出的贴图/材质/网格/.anim 落在**本类这几个目录**里（共用一份池子）——
    //      所以 `EffectExporter.ClearGenerated()`（全量重导那条路）会把它们一起删掉，重导后要跟一次 `BoosterPackExporter.Run`。
    public const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string VfxBundleName = "battleprefabs_vfxandmisc_assets_all.bundle";
    const string Root = "Assets/WarpforgeVFX";
    public const string TexDir = Root + "/Textures";
    public const string MatDir = Root + "/Materials";
    public const string MeshDir = Root + "/Meshes";
    public const string PrefabDir = Root + "/Prefabs";
    // 🆕 2026-10-01：动画那一跳（`AnimationClip` → `.anim` · `AnimatorController` → `.controller`）
    const string AnimDir = Root + "/Animations";
    const string CtrlDir = Root + "/Animators";
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
    // 📌 2026-09-17 用过两次（都配合 `Resume=true`，见 `资料/特效还原_进度与交接.md` §〇之三 三之补二）：
    //    ① 诊断拖尾贴图丢件；② 诊断「导贴图抛 ArgumentNullException」。
    //    ⚠️ 必踩的坑：**必须先把报告里那几行删掉**（否则 `IsDone()` 当「已完成」跳过）。
    //    🔴 **2026-10-16 更正**：这里原来还写着「每跑一次都会在 Materials/ 里留下 `X 1.mat` `X 2.mat`
    //       的副本（`MatCache` 只在一次运行内有效）⇒ 收工要用一次 `Resume=false` 的全量重导把副本清干净」
    //       —— `ImportMaterial` / `ImportMesh` 已改**确定路径 + 原地覆盖**，**不再长副本**，那句作废。
    //       仍然成立的两条：① 这一路的产物会被**下一次全量重导**（`Run()` 先 `ClearGenerated()`）带走；
    //       ② 撞名不同源会出声（`MatByName`），跑完顺手 grep 一下 `撞名且内容不同`。
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

        // ---- 🆕 2026-10-02（兜底路第 2 族）：按 DXBC 逐条重写的自建版 ----
        // 🔴 它原来是**表里没有**的 ⇒ 导出期占位材质掉到 `URP/Unlit*`（approx=true，属性名全对不上，
        //    连 `Texture2D_F593E37E` 都灌不进去）⇒ 新克隆（无随包 shader bundle）时这一族会**整块画不出**。
        //    现在指到专用 shader：属性名与原版一致，占位材质也能把贴图/开关搬过去。
        //    逐条指令与槽位判据 → `Assets/WarpforgeVFX/Shaders/WFParticleDissolveAPB.shader` 文件头。
        { "Shader Graphs/Fx_ParticleDissolve_apb",                "WarpforgeVFX/FX/ParticleDissolveAPB" },
        { "Shader Graphs/Fx_RockDissolve",                        "WarpforgeVFX/FX/RockDissolve" },

        // ---- 🆕 2026-09-13 第三十三轮补的 10 个（原来全掉到兜底的 `URP/Particles/Unlit`）----
        // 出处：派子代理按技术构成定根因时查出「**这一类有 16 条效果**，症状是那个材质槽
        // **保留占位材质**」—— `Shader.Find` 找不到原版名、`Replacements` 里也没有 ⇒
        // `WarpforgeEffectBinder` 返回 null ⇒ 该槽留着导出时的 `URP/Particles/Unlit` 占位。
        // 占位能渲染，但 `_Color`/`_MainTex` 的处理和原版不一样（没有顶点色乘、没有预乘、
        // 没有软粒子），所以**看着不对**。
        // ⚠️ **全部是近似**（带 `*`）：我们没有这些 shader 的属性表，只能按名字挑最接近的自建 shader。
        //    真实的溶解 / UV 滚动 / 顶点流特效**没做** —— 这一点在报告里会标成「近似替代」。
        // 🆕 2026-10-02（兜底路第 2/3/4 族）：这三个已按 DXBC 逐条重写成专用自建 shader ⇒ **去掉 `*`**
        //    （判据 → 各自 `Assets/WarpforgeVFX/Shaders/WF*.shader` 的文件头）。
        { "Everguild/FX/Particle Dissolve Mask",                    "WarpforgeVFX/FX/ParticleDissolveMask" },
        { "Everguild/FX/Alpha Mask One Layer",                      "WarpforgeVFX/FX/AlphaMaskOneLayer" },
        { "Everguild/FX/Alpha Masks Two Layer",                     "WarpforgeVFX/FX/AlphaMasksTwoLayer" },
        { "Everguild/FX/Multi Ray",                                 "WarpforgeVFX/FX/MultiRay" },
        { "Everguild/FX/Particle Shine Custom Vertex Streams",      "WarpforgeVFX/FX/ParticleShineCVS" },
        { "Everguild/FX/Particle Premultiply Greyscale Coloring",   "WarpforgeVFX/FX/ParticlePremultiplyGreyscale" },
        // 🆕 2026-09-15：C 组「导出整个丢了」的 `Explosion_Ground` 用的就是这一个。
        //    它**不在任何我们随包走的 shader 包里**（grep `wf_shaders.bundle` 与
        //    `wf_shaders_extra.bundle`：只有 `…/Particle **Dissolve** Premultiply`，没有这个）
        //    —— 普查表把它记成「原版补充包兜底」是**记错了**。影响 **12 条效果**
        //    （`资料/普查产出_0913/效果_shader_对账.md:77,196`，原来在这张表和
        //    `WarpforgeShaderMap.Replacements` 里**都没有**）。
        { "Everguild/FX/Particle Premultiply",                      "WarpforgeVFX/Particles/Extra Color*" },
        { "Everguild/FX/Unlit UV scroll",                           "WarpforgeVFX/FX/UnlitUVScroll" },
        { "Everguild/FX/TrailShader_1",                             "WarpforgeVFX/FX/TrailShader1" },
        { "Everguild/FX/TrailShader_Fading",                        "WarpforgeVFX/Particles/Extra Color*" },
        { "Shader Graphs/Doomweaver effect",                        "WarpforgeVFX/Particles/Extra Color*" },

        // ⚠️ **2026-09-19 起，下面这 8 个内置管线名在运行时走「原件」了**（`WarpforgeShaderMap.UseOriginal`
        //    21 → 29，原件在 `StreamingAssets/WarpforgeVFX/wf_builtin.bundle`；642 处效果引用）。
        //    但**这张表保持不动**：它管的是**导出期占位材质**用什么，而占位材质**不能**是 bundle 里的 shader
        //    （`Shader.Find` 拿不到非工程资产）—— 这条分工写在 `WarpforgeShaderMap.UseOriginal` 的注释里。
        //    ⚠️ 所以这里那个 `*`（近似替代）现在是**导出期的**描述、不是运行时的：报告里看到它别当成「运行时也是近似」。
        { "Mobile/Particles/Additive",                      "WarpforgeVFX/Particles/Extra Color*" },
        { "Mobile/Particles/Alpha Blended",                 "WarpforgeVFX/Particles/Extra Color*" },
        { "Mobile/Particles/Multiply",                      "WarpforgeVFX/Particles/Multiply" },
        { "Particles/Standard Unlit",                       "WarpforgeVFX/Particles/Extra Color*" },
        { "Particles/Additive",                             "WarpforgeVFX/Particles/Extra Color*" },
        { "Legacy Shaders/Particles/Additive",              "WarpforgeVFX/Particles/LegacyAdditive" },
        { "Legacy Shaders/Particles/Alpha Blended",         "WarpforgeVFX/Particles/LegacyAlphaBlended" },
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

    // ── 🆕 2026-10-16：**撞名守卫**（确定路径的配套件 —— 见 `ImportMaterial` / `ImportMesh` 末尾）──────
    //
    // 那两个函数现在走**确定路径**（`目录/<Sanitize(名字)>.mat` / `.asset`）⇒ **同名必然落到同一份文件**，
    // 后写的盖掉先写的。而**撞名是原件就有的**：`Necron Skull` / `Embers` / `RockDebris` 在原版包里各 ×2
    // （`Embers` 那两条还在一个叫 `bundle_duplicateassetisolation_assets_all` 的包里 —— 包名本身
    //  就叫「重复资产隔离」）；亲测现存 `X N.mat` 里 **107 个文件 / 18 个名字**内容确实互不相同
    // （`Embers` 24 份 / 3 种 · `LightningTrail_intense` 12 / 2 · `RockDebris` 10 / 2 …，
    //  差异是真语义：不同贴图 / `_Cull` / `m_DoubleSidedGI` / `_BaseColor` 31.3 vs 0.31）。
    // ⇒ **不许静默覆盖**（铁律「不许静默失败」）：同名的**第一个**源记在这里，后来者**比内容**、不一样就出声。
    //   内容等价的**不出声** —— 实测 171 个名字 / 811 个文件正文等价，塌成一份无害。
    // 出处 → `资料/普查产出_1016/W20_mat副本根治.md` §④·1。
    static readonly Dictionary<string, (Material src, List<string> fields)> MatByName
        = new Dictionary<string, (Material, List<string>)>();
    static readonly Dictionary<string, (Mesh src, List<string> fields)> MeshByName
        = new Dictionary<string, (Mesh, List<string>)>();

    // ── 🆕 2026-10-01：**只被「模块字段」引用的材质**（挂在渲染器上看不见的那一批）────────────
    //
    // 起因：`AnimFXModuleChangeMaterial` 的 3 张卡材质导不进来。根因**不是**「本地没有」——
    // 它们在 bundle 里（`Vanguard_Frame VAT Dissolve` 在 battleprefabs 包；
    // `Card 3d Dissolve Blend Image Ambush` / `Card 3d Stealth` 在 battlesharedresources 包），
    // 而是导出器**只遍历渲染器**（下面 `Export()` 里的 `GetComponentsInChildren<Renderer>()`），
    // 只被组件字段引用的材质根本不在那圈里。
    //
    // 判据与三张材质的逐字段实读 → `资料/普查产出_1001/资产导入路三件_侦察.md` §①。
    // 表由 `工具/gen_animfx_modules.py` 生成（它本来就在读那几个模块的字段）。
    static readonly Dictionary<string, AssetBundle> LoadedPacks = new Dictionary<string, AssetBundle>();
    static Dictionary<string, string[]> _moduleMats;
    static Dictionary<string, string[]> _moduleMatSrc;      // 材质名 → [源包文件, 容器GUID 或 ""]
    const string ModuleMatPath = @"D:\4\Unity\数据\游戏数据\module_materials.tsv";
    const string ModuleMatSrcPath = @"D:\4\Unity\数据\游戏数据\module_material_sources.tsv";

    /// <summary>某个效果**只被模块字段引用**的材质名（没有就空数组）。</summary>
    static string[] ModuleMaterialsFor(string effectName)
    {
        if (_moduleMats == null)
        {
            _moduleMats = new Dictionary<string, string[]>();
            try
            {
                var acc = new Dictionary<string, List<string>>();
                foreach (var line in File.ReadAllLines(ModuleMatPath))
                {
                    if (line.Length == 0 || line[0] == '#') continue;
                    int t = line.IndexOf('\t');
                    if (t <= 0) continue;
                    string ef = line.Substring(0, t), mt = line.Substring(t + 1).Trim();
                    if (mt.Length == 0) continue;
                    List<string> lst;
                    if (!acc.TryGetValue(ef, out lst)) { lst = new List<string>(); acc[ef] = lst; }
                    if (!lst.Contains(mt)) lst.Add(mt);
                }
                foreach (var kv in acc) _moduleMats[kv.Key] = kv.Value.ToArray();
                Debug.Log($"模块引用的材质表：{_moduleMats.Count} 个效果（{ModuleMatPath}）");
            }
            catch (Exception e)
            {
                // ⚠️ 读不到就**什么都不加**（与改动前一致），但出声 —— 别静默少导材质
                Debug.LogWarning($"读不到模块材质表（{ModuleMatPath}）：{e.Message}" +
                                 " ⇒ 这一趟不会补导那批材质（先跑 工具/gen_animfx_modules.py）");
            }
        }
        string[] v;
        return _moduleMats.TryGetValue(effectName, out v) ? v : EmptyNames;
    }
    static readonly string[] EmptyNames = new string[0];

    /// <summary>按名字从**已加载的包**里取一个 Material（找不到返回 null，调用方出声）。
    ///
    /// 🔴 **不能只试 `LoadAsset&lt;Material&gt;(名字)`** —— 2026-10-01 探针实测（`ProbeModuleMaterials`），
    ///    这条路**对这个 build 是坏的**：`GetAllAssetNames()` 吐的是**容器键**（GUID），不是资产名，
    ///    所以按名字取**恒为 null**。三条取法按表来（表 = `数据/游戏数据/module_material_sources.tsv`，
    ///    由 `工具/gen_module_material_sources.py` 生成）：
    ///      ① 表里**有容器键** ⇒ 去那个包 `LoadAsset&lt;Material&gt;(键)`（实测可用）；
    ///      ② 表里**没有容器键**（非 addressable）⇒ 那它**只在重打的小包里才是可加载根**
    ///         （实测原包的 `LoadAllAssets&lt;Material&gt;()` = 0）⇒ 去那个包 `LoadAllAssets&lt;Material&gt;()`
    ///         按名字捞；
    ///      ③ 表里没有这一条（生成器没跑）⇒ 退到「遍历所有包按名字取」，出声。
    ///    判据全文 → `资料/普查产出_1001/资产导入路三件_侦察.md` §①。</summary>
    static Material FindMaterialInPacks(string name)
    {
        if (_moduleMatSrc == null)
        {
            _moduleMatSrc = new Dictionary<string, string[]>();
            try
            {
                foreach (var line in File.ReadAllLines(ModuleMatSrcPath))
                {
                    if (line.Length == 0 || line[0] == '#') continue;
                    var parts = line.Split('\t');
                    if (parts.Length < 2) continue;
                    _moduleMatSrc[parts[0].Trim()] = new[]
                    {
                        parts[1].Trim(),
                        parts.Length > 2 ? parts[2].Trim() : ""
                    };
                }
                Debug.Log($"模块材质来源表：{_moduleMatSrc.Count} 条（{ModuleMatSrcPath}）");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"读不到模块材质来源表（{ModuleMatSrcPath}）：{e.Message}" +
                                 " ⇒ 退到「遍历所有包按名字取」（慢，且对非 addressable 的取不到）");
            }
        }

        string[] src;
        if (_moduleMatSrc.TryGetValue(name, out src))
        {
            AssetBundle b;
            if (!LoadedPacks.TryGetValue(src[0], out b) || b == null)
            {
                Debug.LogWarning($"[EffectExporter] 材质 `{name}` 该在包 `{src[0]}` 里，但那个包没加载上");
                return null;
            }
            // ① 有容器键：按键取（本 build 唯一可用的精确取法）
            if (!string.IsNullOrEmpty(src[1]))
            {
                var m = b.LoadAsset<Material>(src[1]);
                if (m != null) return m;
                Debug.LogWarning($"[EffectExporter] 材质 `{name}` 按容器键 `{src[1]}` 在 `{src[0]}` 里没取到");
            }
            // ② 无容器键：在那个包里按名字捞（它只在重打的小包里是可加载根）
            foreach (var m in b.LoadAllAssets<Material>())
                if (m != null && m.name == name) return m;
            Debug.LogWarning($"[EffectExporter] 材质 `{name}` 在 `{src[0]}` 里 `LoadAllAssets<Material>()` 也捞不到"
                           + " —— 那个包可能没把它当可加载根打进去");
            return null;
        }

        // ③ 没有表项：退路（慢）
        foreach (var kv in LoadedPacks)
        {
            var b = kv.Value;
            if (b == null) continue;
            try { var m = b.LoadAsset<Material>(name); if (m != null) return m; } catch { }
        }
        return null;
    }

    /// <summary>🔬 探针（2026-10-01）：**为什么 `LoadAsset&lt;Material&gt;(名字)` 取不到那 3 张材质**。
    /// 2026-10-01 实测：`RunListed` 里 3 张全部报「所有已加载的包里都没取到」，而 UnityPy 直读源包
    /// 证明它们在（`battlesharedresources` 的容器里 2 张、`battleprefabs` 里 1 张且不在容器）。
    /// 本探针逐条量：名字在不在 `GetAllAssetNames()` 里 / 按**名字**取 / 按**容器 GUID**取 /
    /// `LoadAllAssets&lt;Material&gt;()` 能不能捞到。**结论出来了就把这段删掉**（它只是诊断）。
    /// 用法：`-executeMethod EffectExporter.ProbeModuleMaterials -logFile -`，筛 `^PM `</summary>
    public static void ProbeModuleMaterials()
    {
        string[] files =
        {
            "battlesharedresources_assets_all.bundle",
            "battleprefabs_vfxandmisc_assets_all.bundle",
        };
        string[] want = { "Card 3d Stealth", "Card 3d Dissolve Blend Image Ambush", "Vanguard_Frame VAT Dissolve" };
        // 容器 GUID（UnityPy 直读得来，见 `资料/普查产出_1001/资产导入路三件_侦察.md` §①）
        var guidOf = new Dictionary<string, string>
        {
            { "Card 3d Stealth", "bdbaf2a0fef8b4d0c875d8d1e65dbc8e" },
            { "Card 3d Dissolve Blend Image Ambush", "e025562dc019d43ca80948e26410a9ed" },
        };
        foreach (var f in files)
        {
            var path = Path.Combine(BundleDir, f);
            var b = AssetBundle.LoadFromFile(path);
            if (b == null) { Debug.Log($"PM `{f}` → **LoadFromFile 返回 null**"); continue; }
            var names = b.GetAllAssetNames();
            var matLike = names.Where(n => n.IndexOf("material", StringComparison.OrdinalIgnoreCase) >= 0)
                               .Take(3).ToArray();
            Debug.Log($"PM `{f}`（内部名 {b.name}）：GetAllAssetNames {names.Length} 条；"
                    + "里像材质的前 3 条 = " + string.Join(" | ", matLike));
            foreach (var w in want)
            {
                var hit = names.FirstOrDefault(n => n.ToLower().Contains(w.ToLower()));
                var byName = b.LoadAsset<Material>(w);
                string byGuid = "（无容器键）";
                string g;
                if (guidOf.TryGetValue(w, out g))
                {
                    var mg = b.LoadAsset<Material>(g);
                    byGuid = mg != null ? mg.name : "null";
                }
                Debug.Log($"PM   材质 `{w}`：在 GetAllAssetNames 里 = {(hit ?? "<没有>")}"
                        + $" ｜ LoadAsset<Material>(名字) = {(byName != null ? byName.name : "null")}"
                        + $" ｜ LoadAsset<Material>(容器GUID) = {byGuid}");
            }
            var all = b.LoadAllAssets<Material>();
            Debug.Log($"PM   LoadAllAssets<Material>() = {all.Length} 个；"
                    + $"命中要的 = {string.Join(", ", all.Where(m => want.Contains(m.name)).Select(m => m.name).ToArray())}");
        }
        Debug.Log("PM 探针结束");
    }

    /// <summary>🔴 **2026-10-02 加：把「VFX 贴图的导入设置」按**原版**逐张纠正**（幂等、可反复跑）。
    ///
    /// **为什么单开一个入口**：`ImportTexture` 里那段设置是**导出时**写进 `.meta` 的 ——
    /// 而**已经导出过的 PNG 早就在工程里了**。为一个设置改动去跑**全量重导**（`EffectExporter.Run`，
    /// 会连 prefab / 材质一起重写）风险大得多（见「生成资产别用 `GenerateUniqueAssetPath` + 先删光」
    /// 那个坑）。本方法**只改 `.meta`**：PNG 一个字节不动、prefab 不碰。
    ///
    /// 判据 = 原版 bundle 里那张 `Texture2D` 的 `mipmapCount`：
    ///   · 原版 **1 层（无 mip）** ⇒ 我们也**不许开 mip**。实读 346 张里 **64 张**属于这一类，
    ///     而旧代码把它们**全部**硬编码成开 mip ⇒ `Buff_DA_Forest_Self` 那条 2.80×：
    ///     拖尾是**极度拉伸的几何**，GPU 按 UV 导数采到很低的 mip ⇒ 整条拖尾被平均成一团糊
    ///     （并排图：原版一小段稀疏拖尾 vs 导出绕一整圈的闭环 —— **环是糊出来的，不是形状变了**）。
    ///   · 原版 >1 层 ⇒ 保持开。
    /// ⚠️ 顺带把 `maxTextureSize` 抬到「不小于原版尺寸」（Unity 默认 2048，超了会**静默缩一半**）。
    /// 跑法：`-executeMethod EffectExporter.FixTextureImportSettings`
    /// （⚠️ 会**重新导入**改到的那些贴图 ⇒ 别在自检批处理跑着的时候跑）。
    ///
    /// 🔴 **为什么读旁挂表而不是在 C# 里枚举**：`AssetBundle.LoadAllAssets&lt;Texture2D&gt;()`
    /// 对这个包**只返回 5 张**（UnityPy 实读是 **346 张**）—— 就是本工程记过档的
    /// 「**bundle 里非 addressable 的资产两条枚举路都拿不到**」（→ `资料/已知的坑.md`）。
    /// 所以走既定的**「旁挂数据 + 生成脚本」**路：
    /// `工具/gen_vfx_texture_mips.py` → `数据/游戏数据/vfx_texture_mips.tsv` → 本方法读它改 `.meta`。</summary>
    public static void FixTextureImportSettings()
    {
        const string tsv = @"d:/4/Unity/数据/游戏数据/vfx_texture_mips.tsv";
        if (!System.IO.File.Exists(tsv))
        {
            Debug.LogError($"[TEXFIX] 缺旁挂表 {tsv} —— 先跑 " +
                           "`\"D:/2/Warpforge_tools/py312/python.exe\" d:/4/Unity/工具/gen_vfx_texture_mips.py`");
            return;
        }
        int rows = 0, changed = 0, noMip = 0, noPng = 0, ok = 0;
        var changedNames = new System.Collections.Generic.List<string>();
        foreach (var line in File.ReadLines(tsv).Skip(1))
        {
            var c = line.Split('\t');
            if (c.Length < 4) continue;
            rows++;
            int w, h, mips;
            if (!int.TryParse(c[1], out w) || !int.TryParse(c[2], out h) || !int.TryParse(c[3], out mips))
            { Debug.LogWarning($"[TEXFIX] 这行解析不了，跳过：{line}"); continue; }

            var png = $"{TexDir}/{Sanitize(c[0])}.png";
            var ti = AssetImporter.GetAtPath(png) as TextureImporter;
            if (ti == null) { noPng++; continue; }
            bool wantMip = mips > 1;
            int wantMax = Mathf.Max(2048, Mathf.NextPowerOfTwo(Mathf.Max(w, h)));
            if (ti.mipmapEnabled == wantMip && ti.maxTextureSize == wantMax) { ok++; continue; }
            if (!wantMip) noMip++;
            ti.mipmapEnabled = wantMip;
            ti.maxTextureSize = wantMax;
            ti.SaveAndReimport();
            changed++;
            changedNames.Add($"{c[0]}({w}x{h},原版m{mips})");
        }
        AssetDatabase.Refresh();
        Debug.Log($"[TEXFIX] 表里 {rows} 行 · **改了 {changed}** · 本来就对 {ok} · " +
                  $"工程里没有对应 PNG {noPng} · 其中「原版无 mip 却被我们开了 mip」的 {noMip} 张");
        if (changedNames.Count > 0)
            Debug.Log("[TEXFIX] 改动清单：" + string.Join(" / ", changedNames.ToArray()));
    }

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
        // ---- 加载全部 bundle，保证跨包引用解析 ----
        AssetBundle vfx = null;
        int nb = 0;
        LoadedPacks.Clear();                       // 🆕 2026-10-01：`FindMaterialInPacks` 要用（按**文件名**做键）
        foreach (var f in Directory.GetFiles(BundleDir, "*.bundle"))
        {
            var b = AssetBundle.LoadFromFile(f);
            if (b == null) continue;
            nb++;
            LoadedPacks[Path.GetFileName(f)] = b;
            if (Path.GetFileName(f) == VfxBundleName) vfx = b;
        }
        // 🆕 2026-10-01：**我们自己重打的小包也要加载** —— 非 addressable 的那几件
        //   （`Card 3D Death Explosion` / `Vanguard Frame Animated VAT` / `Vanguard_Frame VAT Dissolve`）
        //   在原包里只是依赖、不是可加载根，**只有在这些包里才取得到**。
        //   `RunListed` 一直这么做，`Run` 原来没有 —— 于是「全量重导」反而会漏掉那几件。
        int nx0 = 0;
        try
        {
            foreach (var f in Directory.GetFiles(StreamDir, "*.bundle"))
            {
                var b = AssetBundle.LoadFromFile(f);
                if (b == null) continue;
                var fn = Path.GetFileName(f);
                if (LoadedPacks.ContainsKey(fn)) continue;
                LoadedPacks[fn] = b; nx0++;
            }
        }
        catch (Exception e) { Debug.LogWarning($"扫 StreamingAssets/WarpforgeVFX 失败：{e.Message}"); }
        if (nx0 > 0) Debug.Log($"bundle {nb} 个源包 + {nx0} 个重打的小包已加载");
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
        // 🆕 2026-10-11（A211）：**被这个过滤器挡掉的要出声** —— 它原来是**静默**的，
        //   而那正是 `Environmental Condition Particles Orbital` 整件没被导、却一整轮没人发现的原因
        //   （文档里还把它记成了「**有意**没导」——**误记**，真因就是下面这几行）。
        //   判据 = 同一个 `!childOf` 条件在 `all` 里的**补集**（不在别人子树里、但子树里没有一个粒子渲染器）。
        //   ⚠️ 它**只报不改**：真要用其中某一件，把它加进 `ListedPrefabs` 走 `RunListed()`（现成的正规通道）。
        var blocked = all.Where(g => !childOf.Contains(g) &&
                                     g.GetComponentsInChildren<ParticleSystemRenderer>(true).Length == 0)
                         .OrderBy(g => g.name).ToList();
        if (blocked.Count > 0)
            Debug.Log($"效果根过滤器**挡掉 {blocked.Count} 件**（不在别人子树里、但子树里没有一个 "
                    + "`ParticleSystemRenderer` ⇒ 不进 `Run()`）—— 要导得加进 `ListedPrefabs` 走 `RunListed()`："
                    + string.Join(" / ", blocked.Take(40).Select(g => "`" + g.name + "`").ToArray())
                    + (blocked.Count > 40 ? $" …（共 {blocked.Count} 件）" : ""));
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
            // 🔴 2026-09-17：这里原来写的是 `Report[p[0]] = p[1]`（**只取第 2 格**），而
            //    `SaveReport()` 每 10 个效果就把内存这份**照原样写回** ⇒ **任何 `Resume=true` 的续跑
            //    都会把「没被重导的那些行」的第 3 格（`材质定义N个；原 shader: …`）永久抹掉**。
            //    实测：一次续跑之后 958 行里**只剩 2 行**还带第 3 格（恰好就是那趟真重导的 2 个），
            //    而且 `工具/extract_missing_shaders.py` 是按 `p[2]` 取 shader 的 ⇒ **会静默地读空**。
            //    ⇒ 状态只认第 2 格，**第 3 格以后原样留着**。
            if (p.Length >= 2) Report[p[0]] = string.Join("\t", p.Skip(1));
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
        foreach (var p in new[] { "Assets", Root, TexDir, MatDir, MeshDir, PrefabDir, AnimDir, CtrlDir })
            if (!AssetDatabase.IsValidFolder(p))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(p).Replace('\\', '/'), Path.GetFileName(p));
    }

    /// <summary>清掉上一轮产物。
    /// ⚠️ **2026-10-16 更正**：这里原来给的理由是「不清的话 `GenerateUniqueAssetPath` 会不断产出
    ///   `X 1.mat` `X 2.mat` 累积下去」—— `ImportMaterial` / `ImportMesh` 已改**确定路径 + 原地覆盖**
    ///   （见各自函数末尾），**副本不再增加** ⇒ 那条理由作废（函数照旧要跑，为的是下面这一条）。
    ///   剩下的理由：旧贴图/网格会和新的一起被 binder 引用，排查问题时很误导。</summary>
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

    const string EX1 = "EX1 ";

    /// <summary>`RunListed` 要导的那几件（**与 bundle 里的 `m_Name` 逐字相同**）。
    /// 两件都是「原版真在用、但不是『效果根』」的 prefab —— 各有各的判据出处：
    ///   · `Card 3D Death Explosion` —— 阵亡爆散体（挂 `CardScript.cardDestroyFX`；
    ///     判据 → `资料/待办判据_战场与战斗视图.md` 末节第 9 条）
    ///   · `Vanguard Frame Animated VAT` —— `vanguardFrame` 那个状态框（同文件 Q8 第 3 条）
    /// ⚠️ 加新条目之前先确认它**真在这个 bundle 里**（跑完会打 `done/want`，对不上就是没找到）。</summary>
    static readonly string[] ListedPrefabs =
    {
        "Card 3D Death Explosion",
        "Vanguard Frame Animated VAT",
        // 🆕 **2026-09-30**：Ork 的「Night Attack」进攻卡要用的环境 prefab —— 42 条效果 SO 里
        //   **唯一一个没进工程**的（原始 43 个 prefab 里被「43 − 1 = 42」那个算式掩盖了一个）。
        //   它在 `battleprefabs_vfxandmisc_assets_all.bundle` 里（我核过 GameObject/ 目录里有），
        //   不是那种「非 addressable 拿不到」的情形 ⇒ 走 RunListed 的「按名字直接加载」应该取得到。
        //   判据 → `资料/普查产出_0930/§28逐场核_第一轮.md` + `数据/游戏数据/environment_conditions.json`
        //   的 `_unresolved`（生成器每次重跑都会自己报这条）。
        "Orks Environmental Condition Night",
        // 🆕 **2026-10-01**：`AnimFXModuleChangeMaterial` 的那 3 个效果 —— 它们要的材质
        //   原来**只被模块字段引用**、挂在渲染器上根本看不见，所以从来没过导出器
        //   （`Export()` 2026-10-01 起新增了一个 pass 收它们，见那段注释）。
        //   重导这 3 个 = 把材质塞进它们的 binder + 落 `.mat` + 导贴图。
        //   判据 → `资料/普查产出_1001/资产导入路三件_侦察.md` §①。
        "AmbushEffect",
        "StealthEffect",
        "VanguardIdleEffect",
        // 🆕 **2026-10-01 晚**：`Necrons death explosion` 与 `Card 3D Death Explosion` **共用同一个
        //   `AnimatorController`**，所以那处「`m_Controller` 是空 GUID」的毛病它**也有**
        //   （`prefab:24684` 逐行核过）。它本来归 `Run()` 全量导出，列进这里是为了
        //   **「只修动画那一跳」时不必全量重导**（全量那趟要重导 958 个效果）。
        "Necrons death explosion",
        // 🆕 **2026-10-11（A211）**：这件原版是**公共件**、被别的 prefab 内联成副本，
        //   而它自己**一个 `ParticleSystemRenderer` 都没有** ⇒ 被 `Run()` 的「效果根」过滤器**静默挡掉**。
        //   🔴 **记录订正（铁律 5）**：`资料/战场场景线_交接.md` 与 `资料/普查产出_0929/进攻卡_数据表.md`
        //   原来把它记成「**有意**没导」—— **那是误记**；判据 = 它子树里 `ParticleSystemRenderer` = **0** 个
        //   （实测，工具 `工具/a210_a211_gap.py` 每次重跑都会重算这条）。
        //   ⚠️ **导进来 ≠ 会跑**：`BuildStandaloneSpawners` 只在**实例化那件 prefab** 时才建；
        //   我们（和原版一样 —— 全库 `PrefabInstance = 0`）都不会实例化它。这一条消掉的是
        //   「**资产不在工程里**」这个假缺口，不是「那 4 条 spawner 会开始工作」。
        "Environmental Condition Particles Orbital",
        // 🆕 **2026-10-11（A210）**：原版有 **30 个 `ParticleSystemPoolable`**、落在那 **19 件** prefab 的
        //   子树节点上（表 = `PoolableSpots`，由 `工具/a210_a211_gap.py --cs` 生成、**别手抄**）。
        //   我们这 19 件**一个都没挂** —— 真因 = 导入那一跳：bundle 的 `m_Script` 解析不了 ⇒
        //   `StripMissingScripts` 把它删了；`Export()` 里已补 `AttachPoolables`，**但要重导才生效**。
        //   下面这 19 个根名就是为了让 `RunListed()` 覆盖到它们（不重导 = 脚本永远报 0 件）。
        //   ⚠️ `Orbital 5 repeat ` **带尾随空格**（原版就这么写的），⛔ 别 Trim。
        "Environmental Condition Dark Angels Asteroid Zone",
        "Environmental Condition Emperor's Children Empyric Rift",
        "Environmental Condition Emperor's Children Empyric Rift OLD",
        "Environmental Condition GSC Mining Tremors",
        "Environmental Condition Necrons Earthquake",
        "Environmental Condition Space Wolves Everstorm",
        "EnvironmentalCondition Saim Hann Infinity Circuit Overload",
        "EnvironmentalCondition Sororitas Raging Storm",
        "EnvironmentalCondition Sororitas Shrine Bombardment",
        "EnvironmentalCondition Ultramarines Aerial Clash",
        "EnvironmentalCondition Ultramarines Bombardment",
        "EnvironmentalCondition Ultramarines Thunderstorm",
        "GSC Rockfall",
        "Lightning burst webway",
        "Meteor Angled",
        "Orbital 3 repeat",
        "Orbital 4 repeat",
        "Orbital 5 repeat ",
        "Rockfall",
        // 🆕 **2026-10-13（A425②）**：领奖窗「收集」那一下的粒子 —— **同批已把它打进
        //   `wf_menus_extra.bundle`**（`工具/extract_missing_shaders.py` 的 `BOOSTER_GROUPS[0].roots` 加了那一行，
        //   `--prefabs` 重跑过、UnityPy 回读确认在包里）。
        //   判据 → `资料/普查产出_1012/H3_领取粒子与Blink公共件.md`。
        "RewardAppearParticle",
    };

    /// <summary>
    /// **只导指定的那几个 prefab**（按 GameObject 名匹配）。
    ///
    /// 为什么不复用 `Run()`：① 它的 `roots` 过滤是「不在别人子树里 **且** 子树里有粒子渲染器」——
    /// 挑的是**效果根**，这两件不在那份名单里；② 它开头会 `ClearGenerated()` 把整库删光
    /// （只有 `Resume = true` 才不删）⇒ 拿那条路导单个文件 = 把 958 个效果全删了重来。
    ///
    /// 用法：
    ///   unset ELECTRON_RUN_AS_NODE &amp;&amp; "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
    ///     -executeMethod EffectExporter.RunListed -logFile "d:/4/_tmp_view/exportone.log"
    /// 筛输出：grep "^EX1 "
    /// ⚠️ 导完**必须**跟一次「生成效果库」（`EffectLibraryBuilder`），否则新 prefab 进不了库。
    /// </summary>
    public static void RunListed()
    {
        EnsureFolders();
        AssetBundle vfx = null;
        int nb = 0;
        LoadedPacks.Clear();                       // 🆕 2026-10-01：`FindMaterialInPacks` 要用（按**文件名**做键）
        foreach (var f in Directory.GetFiles(BundleDir, "*.bundle"))
        {
            var b = AssetBundle.LoadFromFile(f);
            if (b == null) continue;
            nb++;
            LoadedPacks[Path.GetFileName(f)] = b;
            if (Path.GetFileName(f) == VfxBundleName) vfx = b;
        }
        // 🆕 2026-10-01：**把我们自己重打的包也扫进来**。
        //   起因：有几件在源包里**枚举不到**（`Card 3D Death Explosion` / `Vanguard Frame Animated VAT`
        //   —— 被卡预制体字段引用、自己不是 addressable ⇒ `GetAllAssetNames()` 与
        //   `LoadAllAssets<GameObject>()` 两条都不含，判据 → `资料/已知的坑.md` 同名那条）。
        //   做法 = `python 工具/extract_missing_shaders.py --prefabs`（把它们 + **整棵依赖树**
        //   重打成 `StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle`，并重写容器/预加载表）。
        //   ⚠️ 已经加载过的包在这里会返回 null（Unity 限制），属正常，不是错误。
        var packs = new List<AssetBundle>();
        if (vfx != null) packs.Add(vfx);
        int nx = 0;
        try
        {
            foreach (var f in Directory.GetFiles(StreamDir, "*.bundle"))
            {
                var b = AssetBundle.LoadFromFile(f);
                if (b == null) continue;
                if (packs.Contains(b)) continue;
                packs.Add(b); nx++;
                LoadedPacks[Path.GetFileName(f)] = b;   // 🆕 2026-10-01：按**文件名**登记
            }
        }
        catch (Exception e) { Debug.LogWarning(EX1 + "扫 StreamingAssets/WarpforgeVFX 失败：" + e.Message); }
        Debug.Log(EX1 + $"bundle 已加载：源包目录 {nb} 个（其中特效主包 {(vfx != null ? "√" : "×")}）+ 重打的小包 {nx} 个");
        if (vfx == null) Debug.LogWarning(EX1 + $"源包 `{VfxBundleName}` 没加载上 —— 检查 BundleDir 路径");
        CopyShaderBundle();

        int want = ListedPrefabs.Length, done = 0;
        var picked = new HashSet<string>();

        // ---- 第一遍：**按名字直接加载**（`LoadAsset<GameObject>(name)`）----
        //  非 addressable 的 prefab 走不了容器枚举，但按名字仍可能取到（重打过的包里必然能）。
        foreach (var t in ListedPrefabs)
        {
            GameObject g = null;
            string from = null;
            foreach (var b in packs)
            {
                try { g = b.LoadAsset<GameObject>(t); }
                catch (Exception e) { Debug.LogWarning(EX1 + $"`{b.name}` 按名字加载 `{t}` 抛了：{e.GetType().Name}: {e.Message}"); }
                if (g != null) { from = b.name; break; }
            }
            if (g == null) { Debug.Log(EX1 + $"按名字加载 `{t}` → **没取到**（下面再用枚举兜一次）"); continue; }
            if (!picked.Add(g.name)) continue;
            try { Export(g); done++; Debug.Log(EX1 + $"已导出 `{g.name}`（按名字取的，来自包 `{from}`）—— {LastDetail}"); }
            catch (Exception e) { Debug.LogError(EX1 + $"导出 `{g.name}` 失败：{e.GetType().Name}: {e.Message}"); }
        }

        // ---- 第二遍：枚举兜底（每个包都兜一遍）----
        //  🔴 **两个枚举 API 覆盖的范围不一样**（2026-09-29 实测）：
        //    · `GetAllAssetNames()` 只吐**容器（addressables 清单）里**的路径；
        //    · `LoadAllAssets<GameObject>()` 吐的是**可加载的资产根**（主包里 965 个，≈ 就是那些效果根）。
        //    两遍都不含的（例如 `Card 3D Death Explosion`）就只剩「按名字直接加载」那一条
        //    —— 所以第一遍才是主路，这里是兜底。
        int loaded = 0, nameHits = 0;
        var seen = new HashSet<string>();
        foreach (var b in packs)
        {
            string[] names = null; GameObject[] all = null;
            try { names = b.GetAllAssetNames(); } catch (Exception e) { Debug.LogWarning(EX1 + $"`{b.name}` 容器枚举抛了：{e.Message}"); }
            try { all = b.LoadAllAssets<GameObject>(); } catch (Exception e) { Debug.LogWarning(EX1 + $"`{b.name}` LoadAllAssets 抛了：{e.Message}"); }
            Debug.Log(EX1 + $"枚举 `{b.name}`：`GetAllAssetNames()` {(names == null ? -1 : names.Length)} 条 · "
                          + $"`LoadAllAssets<GameObject>()` {(all == null ? -1 : all.Length)} 个");
            if (all == null) continue;
            foreach (var g in all)
            {
                if (g == null || !seen.Add(g.name)) continue;
                loaded++;
                if (g.name != null && (g.name.Contains("Card 3D") || g.name.Contains("Death") || g.name.Contains("Vanguard")))
                { nameHits++; Debug.Log(EX1 + $"  候选（枚举）：`{g.name}`（包 `{b.name}`）"); }
                bool hit = false;
                foreach (var t in ListedPrefabs) if (g.name == t) hit = true;
                if (!hit || !picked.Add(g.name)) continue;
                try { Export(g); done++; Debug.Log(EX1 + $"已导出 `{g.name}`（枚举取的，来自包 `{b.name}`）—— {LastDetail}"); }
                catch (Exception e) { Debug.LogError(EX1 + $"导出 `{g.name}` 失败：{e.GetType().Name}: {e.Message}"); }
            }
        }
        Debug.Log(EX1 + $"枚举合计：去重后 {loaded} 个 GameObject（名字像卡体/先锋框的 {nameHits} 个）");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(EX1 + $"指定 prefab 导出完成：**{done}/{want}**"
                      + "（对不上 = 那件不在这些包里，或者加载失败 —— 上面有逐条日志）");
    }

    // ==================================================================
    //  🆕 2026-10-11（A210）：把原版的 `ParticleSystemPoolable` **补挂回去**
    //
    //  判据 → `资料/普查产出_1011/W9_A196_A210_A211.md` §二·A210：
    //    · 原版 **30 个** `ParticleSystemPoolable`（`m_Script` → 149267643601780667
    //      = `bundle_Waprforge_monoscripts` 的 `m_ClassName = ParticleSystemPoolable`），
    //      落点是**模板粒子自己那个 GameObject**；实测 **30/30** 的 `myParticleSystem` 就是
    //      **同一个 GameObject 上**那个 `ParticleSystem`（不是子件、不是别人）；
    //    · 我们 961 件 prefab 里 **0 个** —— 根因是**导入那一跳**：
    //      bundle 里的 `m_Script` 在我们工程里解析不了 ⇒ `StripMissingScripts(inst)` 会把它删掉。
    //  ⇒ 缺了不会「错」（`ParticleSystemAreaSpawner.CreatePooledItem` 拿不到会就地 `AddComponent` 补一个，
    //    原版 `.c` 里那条分支本来就在），但**每建一颗粒子刷一条 `LogError`（假警报）**，把真信号淹掉；
    //    而且「资产缺什么永远看不见」。
    //
    //  ⚠️ **顺序**：必须**在 `StripMissingScripts` 之后**（否则刚挂上的又会被那一趟当 missing 删掉）、
    //    `PrefabUtility.SaveAsPrefabAsset` 之前。
    //  ⚠️ **名字带尾随空格**（原版真有 `'Orbital 5 repeat '`）⇒ 逐级比较走
    //    `CardPresentation.EnvironmentApplier.Norm`（**Trim 后比**），⛔ 别用 `Transform.Find` 逐字比。
    //  ⚠️ **表由 `工具/a210_a211_gap.py --cs` 生成，别手抄**（原版包改了它就重跑一次）。
    //  ⚠️ `AddComponent` 在编辑器里会跑一次 `OnValidate`（⇒ 会顺手设 `stopAction = Callback`），
    //    与运行时 `AssignParticleSystemReference` 设的是同一个值 ⇒ 幂等，不是偏离。
    // ==================================================================
    static readonly string[][] PoolableSpots =
    {
        new[] { "Environmental Condition Dark Angels Asteroid Zone", "Environmental Condition Dark Angels Asteroid Zone/Asteroids crash tower" },
        new[] { "Environmental Condition Dark Angels Asteroid Zone", "Environmental Condition Dark Angels Asteroid Zone/Asteroids crash tower (1)" },
        new[] { "Environmental Condition Dark Angels Asteroid Zone", "Environmental Condition Dark Angels Asteroid Zone/Asteroids crash tower further" },
        new[] { "Environmental Condition Emperor's Children Empyric Rift", "Environmental Condition Emperor's Children Empyric Rift/Psychic_Lightning_down/Lightning Main" },
        new[] { "Environmental Condition Emperor's Children Empyric Rift OLD", "Environmental Condition Emperor's Children Empyric Rift OLD/Psychic_Lightning_down/Lightning Main" },
        new[] { "Environmental Condition GSC Mining Tremors", "Environmental Condition GSC Mining Tremors/Rockfall" },
        new[] { "Environmental Condition GSC Mining Tremors", "Environmental Condition GSC Mining Tremors/Rockfall Background" },
        new[] { "Environmental Condition Necrons Earthquake", "Environmental Condition Necrons Earthquake/Rockfall" },
        new[] { "Environmental Condition Space Wolves Everstorm", "Environmental Condition Space Wolves Everstorm/Psychic_Lightning_down/Lightning Main" },
        new[] { "EnvironmentalCondition Saim Hann Infinity Circuit Overload", "EnvironmentalCondition Saim Hann Infinity Circuit Overload/Pooling controller/Area Spawner/Lightning burst" },
        new[] { "EnvironmentalCondition Sororitas Raging Storm", "EnvironmentalCondition Sororitas Raging Storm/Psychic_Lightning_down/Lightning Main" },
        new[] { "EnvironmentalCondition Sororitas Shrine Bombardment", "EnvironmentalCondition Sororitas Shrine Bombardment/Psychic_Lightning_down/Explosion Left" },
        new[] { "EnvironmentalCondition Sororitas Shrine Bombardment", "EnvironmentalCondition Sororitas Shrine Bombardment/Psychic_Lightning_down/Explosion Right" },
        new[] { "EnvironmentalCondition Ultramarines Aerial Clash", "EnvironmentalCondition Ultramarines Aerial Clash/Bullet far controller (1)/Strafing Runs Bullets 1 (1)" },
        new[] { "EnvironmentalCondition Ultramarines Aerial Clash", "EnvironmentalCondition Ultramarines Aerial Clash/Bullet far controller/Strafing Runs Bullets 1 (1)" },
        new[] { "EnvironmentalCondition Ultramarines Aerial Clash", "EnvironmentalCondition Ultramarines Aerial Clash/Missile Controller/Missile Particle" },
        new[] { "EnvironmentalCondition Ultramarines Aerial Clash", "EnvironmentalCondition Ultramarines Aerial Clash/Shadow controller (1)/Thunderhawk_shadow" },
        new[] { "EnvironmentalCondition Ultramarines Aerial Clash", "EnvironmentalCondition Ultramarines Aerial Clash/Shadow controller (2)/Thunderhawk_shadow" },
        new[] { "EnvironmentalCondition Ultramarines Bombardment", "EnvironmentalCondition Ultramarines Bombardment/Orbital 3 repeat" },
        new[] { "EnvironmentalCondition Ultramarines Bombardment", "EnvironmentalCondition Ultramarines Bombardment/Orbital 4 repeat" },
        new[] { "EnvironmentalCondition Ultramarines Bombardment", "EnvironmentalCondition Ultramarines Bombardment/Orbital 5 repeat " },
        new[] { "EnvironmentalCondition Ultramarines Thunderstorm", "EnvironmentalCondition Ultramarines Thunderstorm/Psychic_Lightning_down/Lightning Main" },
        new[] { "GSC Rockfall", "GSC Rockfall" },
        new[] { "GSC Rockfall", "GSC Rockfall/Rockfall Background" },
        new[] { "Lightning burst webway", "Lightning burst webway" },
        new[] { "Meteor Angled", "Meteor Angled" },
        new[] { "Orbital 3 repeat", "Orbital 3 repeat" },
        new[] { "Orbital 4 repeat", "Orbital 4 repeat" },
        new[] { "Orbital 5 repeat ", "Orbital 5 repeat " },
        new[] { "Rockfall", "Rockfall" },
    };

    /// <summary>按**归一化名字**逐级下沉（`path` 的第 0 段 = 根名，从第 1 段开始往下找）。
    /// 比较走 `CardPresentation.EnvironmentApplier.Norm`（判据只留那一处）。</summary>
    static Transform FindChildByPath(Transform root, string path)
    {
        if (root == null || string.IsNullOrEmpty(path)) return null;
        var segs = path.Split('/');
        var t = root;
        for (int i = 1; i < segs.Length && t != null; i++)
        {
            Transform next = null;
            for (int c = 0; c < t.childCount; c++)
                if (CardPresentation.EnvironmentApplier.Norm(t.GetChild(c).name)
                    == CardPresentation.EnvironmentApplier.Norm(segs[i]))
                { next = t.GetChild(c); break; }
            t = next;
        }
        return t;
    }

    static void AttachPoolables(GameObject inst, string rootName)
    {
        if (inst == null) return;
        int n = 0, miss = 0;
        foreach (var e in PoolableSpots)
        {
            if (e[0] != rootName) continue;
            var tr = FindChildByPath(inst.transform, e[1]);
            if (tr == null)
            {
                miss++;
                Debug.LogWarning(EX1 + $"A210 表里的 `{e[1]}` 在 `{rootName}` 里找不到 —— 这一条没挂上（出声，不静默）");
                continue;
            }
            var pl = tr.gameObject.AddComponent<CardPresentation.ParticleSystemPoolable>();
            if (pl == null) { miss++; continue; }
            var ps = tr.GetComponent<ParticleSystem>();
            if (ps != null) pl.AssignParticleSystemReference(ps);
            else Debug.LogWarning(EX1 + $"A210 `{e[1]}` 上**没有 `ParticleSystem`** ⇒ 只挂了组件、没接引用"
                                       + "（`CreatePooledItem` 会就地补一个，与缺组件时同一条分支）");
            n++;
        }
        if (n > 0 || miss > 0)
            Debug.Log(EX1 + $"A210 `ParticleSystemPoolable`：`{rootName}` 挂上 **{n}** 个"
                          + (miss > 0 ? $"、**没对上 {miss} 个**（见上面的告警）" : ""));
    }

    static void Export(GameObject src)
    {
        var inst = UnityEngine.Object.Instantiate(src);
        inst.name = src.name;
        StripMissingScripts(inst);
        // 🆕 2026-10-11（A210）：**必须在 `StripMissingScripts` 之后**（见上面那段头注）。
        AttachPoolables(inst, src.name);

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
            var ps = r.GetComponent<ParticleSystem>();

            // ---- 粒子拖尾材质：**按渲染器序号记账，这一轮里每个渲染器必须记且只记一条** ----
            // 取用方是 `WarpforgeEffectBinder.Apply` 的 `trailSlots[ri]`（ri = 渲染器序号），
            // 所以它**绝不能被下面那个 `mats.Length == 0` 的提前 continue 挡在后面** ——
            // 原来就挡在后面：只要前面出现一个零材质的渲染器，后面**所有渲染器的拖尾槽整体错一格**，
            // 效果是拖尾被换成别人的材质、或者干脆不重建、留着没有贴图的占位材质（静默）。
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

            var mats = r.sharedMaterials;
            if (mats.Length == 0) continue;
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
        }

        // ---- 🆕 2026-10-01：**只被「模块字段」引用的材质**（挂在渲染器上看不见的那批）----
        //
        // 上面那圈只遍历渲染器 ⇒ `AnimFXModuleChangeMaterial` 那 3 张卡材质一条都收不到。
        // 它们**在 bundle 里**（不是「本地没有」），只是没人去取。三张 + 逐字段实读：
        // → `资料/普查产出_1001/资产导入路三件_侦察.md` §①。
        //
        // ⚠️ 只加进**这一个 prefab** 的 `defs`（= 它自己的 binder）。模块运行时就从**自己那个播放器**
        //    的 binder 按名字取（`WFModuleChangeMaterial.ResolveAssets`）—— 与原版「组件上挂着
        //    `AssetReferenceTyped<Material>`」同构，不是全局查找。
        foreach (var mName in ModuleMaterialsFor(src.name))
        {
            var om = FindMaterialInPacks(mName);
            if (om == null)
            {
                Debug.LogWarning($"[EffectExporter] 模块引用的材质 `{mName}`（效果 {src.name}）"
                               + "在所有已加载的包里都没取到 ⇒ 这个效果「换材质」那一下没有材质可用");
                continue;
            }
            int di = DefIndex(defs, defIndex, om);
            StripGlobalKeywords(defs[di], null, om.shader);
            ImportMaterial(om, out bool mApprox, out string mShader);
            if (mApprox) approx++;
            usedShaders.Add(mShader);
            Debug.Log($"[EffectExporter] 模块材质 `{om.name}` → 原 shader `{mShader}`"
                    + $"（效果 {src.name} · def#{di} · 近似={mApprox}）");
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

        // 🔴 **`textureSheetAnimation` 的 sprite 列表**（2026-09-19 补，出自
        // `资料/普查产出_0918/孤儿待办_五条查证.md` §一）。
        //
        // 和上面那两条**同一个病**：那些 sprite 同样是 **bundle 资产**，不导的话引用落不下来
        // ⇒ 序列化成 guid 全 0 的伪引用（实测 `Prefabs/CreateCard DA.prefab:1547` 就是
        // `sprites:` + `- sprite: {fileID: 0}`）⇒ 粒子贴图整个画不出来。
        // 影响面（同 §一，自己数的）：**143 个 PS / 99 个效果**
        // （原版 VFX 包 383 个 PS 带非空列表，其余是空表）。
        //
        // ⚠️ **卡背那批不在此列、别混**：原版 `CreateCard*` 家族的 tsa `sprites`
        //    **本来就是空**（单元素 `m_PathID: 0`）—— 卡背是**运行期**
        //    `AnimFXModuleCardback.Initialize` → `tsa.AddSprite` 塞的
        //    （原版 6 个 prefab 逐字节核过、我们导出来的和原版一致）。
        //    ⇒ 「卡背为空」的根因在**运行期没接线**（`WFModuleCardback.CardbackResolver`
        //      从没赋值），不在这里；两件事 2026-09-19 一起做，别只做一半。
        foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
        {
            var tsa = ps.textureSheetAnimation;
            int n = tsa.spriteCount;
            if (n <= 0) continue;
            for (int i = 0; i < n; i++)
            {
                var s = tsa.GetSprite(i);
                if (s == null) continue;
                var imported = ImportSprite(s);
                // ⚠️ **直接挂在属性上**（2026-09-19 实测：`TextureSheetAnimationModule` 是结构体，
                //    `AddSprite` 走局部副本**不生效**；`SetSprite` 实测有效，但**统一写法**，
                //    别让下一个人照抄了错的那一种。见 `WFModuleCardback.Apply` 里那段注释。）
                if (imported != null) ps.textureSheetAnimation.SetSprite(i, imported);
            }
        }

        // ---- 🆕 2026-10-01：**动画那一跳**（`Animator` 的控制器 / `Animation` 的片段）----
        // 和上面精灵那两条**同一个病**：控制器与片段都是 **bundle 资产**，不导的话
        // `PrefabUtility.SaveAsPrefabAsset` 落不下来 ⇒ 写成 guid 全 0 的伪引用、运行时是 null。
        // 实测受害者：`Card 3D Death Explosion`（`m_Controller: {…, guid: 00000000000000000000000000000000}`）
        // 与 `Necrons death explosion`（同一个控制器）⇒ **阵亡爆散体不会播动画**。
        // 🔴 记录订正：原来记的根因是「`extract_missing_shaders.py` 的依赖树没跟着 `Animator.m_Controller` 走」
        //    —— **不成立**（实测那件 `AnimatorController` 就在重打包产物里、`[P3]` 也列了它）。
        //    真正的断点在**导出这一跳**，正是这里。判据 → `资料/普查产出_1001/资产导入路三件_侦察.md` 续写。
        string origController = null;
        foreach (var an in inst.GetComponentsInChildren<Animator>(true))
        {
            var rc = an.runtimeAnimatorController;
            if (rc == null) continue;
            origController = rc.name;                    // 记下来写给 binder（运行时要按名字取原件）
            var proj = ImportAnimatorController(rc);
            if (proj != null) an.runtimeAnimatorController = proj;
        }

        // ---- 挂 binder：进游戏时用原版 shader 重建材质 ----
        var binder = inst.GetComponent<WarpforgeEffectBinder>();
        if (binder == null) binder = inst.AddComponent<WarpforgeEffectBinder>();
        binder.materials = defs.ToArray();
        binder.rendererSlots = slots.ToArray();
        binder.trailSlots = trailSlots.ToArray();
        binder.emissionOn = EmissionFlagFor(src.name);
        // 🆕 2026-10-01：这个 prefab 的 Animator 在原版里用的控制器名 —— 运行时
        //    `WarpforgeEffectBinder.BindOriginalAnimator` 按它从重打的小包里取**原件**。
        //    为什么非得运行时取：见 `WarpforgeAnimatorBridge.cs` 头部（muscle clip 落不了盘）。
        binder.animatorController = origController ?? "";

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
    // ── 逐效果的 `_EMISSION` 标记表（实测得出，见 `工具/gen_emission_flag.py`）────────────
    //
    // **为什么是一张表、而不是一个判据**：本轮把三种推断判据全试了、**全被实测推翻** ——
    //   · 「`_EMISSION` 是全局的 ⇒ 两侧等效」   → 被 Y 条件推翻（开回来让 5 个效果**正好回到 1.0000**）
    //   · 「按粒子系统 Emission 模块判」        → 被 Z≡Y（13/13 完全相同）推翻
    //   · 「按材质关键字 / `_EmissionColor` 非黑猜」 → 逐效果准确率只有 71–78%，误判上百条
    // 唯一靠得住的是**直接量**：把原版那趟的 `_EMISSION` 关掉再渲一遍，**数变了 = 原版在用**。
    // 表由那个脚本从两趟 sweep 数据生成 —— 它是**数据不是推断**；重导特效前若它比 sweep 旧，就重跑脚本。
    //
    // 实测分离度：E 组**偏暗**那侧 91% 落在「在用」、**偏亮**那侧 88% 落在「没用」。
    static Dictionary<string, bool> _emissionFlags;
    const string EmissionFlagPath =
        @"D:\4\Unity\数据\游戏数据\emission_flag.json";

    public static bool EmissionFlagFor(string effectName)
    {
        if (_emissionFlags == null)
        {
            _emissionFlags = new Dictionary<string, bool>();
            try
            {
                // 表是 `json.dump(..., indent=0)` 写的 ⇒ 一行一条 `"名字": true,`，逐行读即可（不引 JSON 依赖）
                foreach (var line in File.ReadAllLines(EmissionFlagPath))
                {
                    int q1 = line.IndexOf('"');
                    int q2 = line.LastIndexOf('"');
                    if (q1 < 0 || q2 <= q1) continue;
                    string k = line.Substring(q1 + 1, q2 - q1 - 1);
                    int c = line.IndexOf(':', q2);
                    if (c < 0) continue;
                    _emissionFlags[k] = line.IndexOf("true", c, StringComparison.OrdinalIgnoreCase) >= 0;
                }
                Debug.Log($"emission 标记表：{_emissionFlags.Count} 条（{EmissionFlagPath}）");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"读不到 emission 标记表（{EmissionFlagPath}）：{e.Message}" +
                                 " ⇒ 一律按 false（= 与改动前一致，不会静默改行为）");
            }
        }
        bool v;
        return _emissionFlags.TryGetValue(effectName, out v) && v;
    }

    public static void StripGlobalKeywords(WFMatDef d, ParticleSystem ps, Shader sh)
    {
        if (d == null) return;

        // ⚠️ **这一位要在 early-return 之前记**：它描述的是「原版这个材质自己带不带 `_EMISSION`」，
        //    与它挂在什么渲染器上无关（`WarpforgeEffectBinder` 拿它跟逐效果的 `emissionOn` 取交集）。
        if (d.keywords != null)
            foreach (var k in d.keywords)
                if (k == "_EMISSION") { d.hadEmissionKeyword = true; break; }

        if (ps == null || sh == null) return;

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

    // 🔴 **渲染队列真值表**（2026-09-19 晚，第三版判据 —— 前两版都不完备）
    //
    // 为什么不能再信 `Material.renderQueue`：它在材质**没有 override** 时返回的就是
    // `shader.renderQueue`，而**只在 bundle 里的 shader** 在编辑器环境里**解析不出队列**
    // ⇒ 返回兜底的 **2000（＝不透明队列）** ⇒ 运行时把粒子当不透明排 ⇒ 看起来暗/亮。
    //    · 第一版 `AssetDatabase.Contains(om.shader)` —— **恒为 true，等于没改**（重导一遍才发现）
    //    · 第二版 `om.renderQueue != om.shader.renderQueue` —— 判不出「材质恰好 override 成与 shader 同值」
    // ⇒ **第三版：直接读原始 JSON 的真值**（`工具/gen_renderqueue_truth.py` 生成两张 TSV）：
    //    **生效队列 = mat[材质名] 若 ≥0，否则 shader[shader名]；都没有 ⇒ −1**（运行时用 shader 默认）。
    //    验证：`Fx_RockDissolve` 的 SubShader QUEUE 是 `AlphaTest` = **2450**，
    //    正是 `Invoke Minion Hits Ground` 里 `rock` 实测到的原版值 ✓
    //    影响面：197 个 shader 里 **29 个的 QUEUE 不是 3000**（`Matcap/*` = 2000 那一族影响 190 条效果）。
    static Dictionary<string, int> _matQ, _shaderQ;

    static void LoadQueueTruth()
    {
        if (_matQ != null) return;
        _matQ = new Dictionary<string, int>();
        _shaderQ = new Dictionary<string, int>();
        const string baseDir = "d:/4/Unity/数据/游戏数据/";
        foreach (var pair in new[] {
            new KeyValuePair<string, Dictionary<string, int>>(baseDir + "mat_renderqueue.tsv", _matQ),
            new KeyValuePair<string, Dictionary<string, int>>(baseDir + "shader_renderqueue.tsv", _shaderQ) })
        {
            if (!File.Exists(pair.Key))
            {
                Debug.LogWarning($"[EffectExporter] 队列真值表缺失：{pair.Key} ⇒ 这一轮的 renderQueue 会退回 −1");
                continue;
            }
            foreach (var ln in File.ReadAllLines(pair.Key, System.Text.Encoding.UTF8))
            {
                var p = ln.Split('\t');
                if (p.Length < 2 || !int.TryParse(p[1], out var q)) continue;
                pair.Value[p[0]] = q;
            }
        }
        Debug.Log($"[EffectExporter] 队列真值表：材质 {_matQ.Count} · shader {_shaderQ.Count}");
    }

    /// <summary>原版材质的**生效渲染队列**：材质 override 优先，否则取 shader 的 SubShader QUEUE；都没有 ⇒ −1。</summary>
    static int TruthQueue(string matName, string shaderName)
    {
        LoadQueueTruth();
        if (_matQ.TryGetValue(matName, out var q) && q >= 0) return q;
        if (!string.IsNullOrEmpty(shaderName) && _shaderQ.TryGetValue(shaderName, out var q2)) return q2;
        return -1;
    }

    /// <summary>把原材质的 shader 名与全部属性值抓下来，供运行时重建</summary>
    public static int DefIndex(List<WFMatDef> defs, Dictionary<string, int> idx, Material om)
    {
        string key = om.name + "|" + (om.shader ? om.shader.name : "null");
        if (idx.TryGetValue(key, out var i)) return i;

        var d = new WFMatDef
        {
            name = om.name,
            shader = om.shader ? om.shader.name : "",
            renderQueue = TruthQueue(om.name, om.shader ? om.shader.name : ""),
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
            // 🔴 2026-09-17：原来是 `try { tl.Add(tn[ti]); tv.Add(ImportTexture(tt[ti])); } catch { }` ——
            //    `ImportTexture` 一旦抛异常，**名字加进去了、值没加**，两个列表长度就对不上；
            //    而运行时 `WarpforgeEffectBinder.Build` 是按
            //    `i < texNames.Length && i < texVals.Length` 走的 ⇒ **后面所有贴图一起失效**，
            //    而且同样是静默的。现在先把值算出来，失败也照样 Add(null)，并大声报。
            Texture2D imp = null;
            try { imp = ImportTexture(tt[ti]); }
            catch (Exception e)
            {
                // 2026-09-17：带上**调用栈前几帧** —— 分步诊断（ReadableCopy / EncodeToPNG）都没触发，
                // 说明抛在 `ImportTexture` 后半段（写 PNG / `AssetDatabase.ImportAsset` / `SaveAndReimport`），
                // 光看类型和消息分不出来，栈一看就知道。
                Debug.LogWarning($"[EffectExporter] 贴图导出抛异常，这张贴图会缺：材质 {om.name} / {tn[ti]}"
                               + $" = {(tt[ti] == null ? "<null>" : tt[ti].name)}；{e.GetType().Name}: {e.Message}"
                               + $" ｜栈：{FirstFrames(e)}");
            }
            tl.Add(tn[ti]);
            tv.Add(imp);
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

    public static void StripMissingScripts(GameObject go)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
        {
            var comps = t.GetComponents<Component>();
            int miss = comps.Count(c => c == null);
            for (int i = 0; i < miss; i++)
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        }
    }

    /// <summary>把一个材质**内容**摊成 `名字=值` 列表（撞名守卫用；只读、不落盘、不动资产）。
    /// 贴图只取**对象名** —— 那正是「两张材质是不是同一张图」的判据（包外取不到 guid），
    /// 而且只读 `.name`，不碰贴图的像素数据。</summary>
    static List<string> MatFields(Material m)
    {
        var f = new List<string> { "源名=" + m.name, "shader=" + ShaderOf(m), "renderQueue=" + m.renderQueue };
        var sh = m.shader;
        int n = sh != null ? sh.GetPropertyCount() : 0;
        for (int i = 0; i < n; i++)
        {
            var pn = sh.GetPropertyName(i);
            try
            {
                if (!m.HasProperty(pn)) continue;
                switch (sh.GetPropertyType(i))
                {
                    case ShaderPropertyType.Color:  f.Add(pn + "=" + m.GetColor(pn)); break;
                    case ShaderPropertyType.Vector: f.Add(pn + "=" + m.GetVector(pn)); break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:  f.Add(pn + "=" + m.GetFloat(pn)); break;
                    case ShaderPropertyType.Int:    f.Add(pn + "=" + m.GetInt(pn)); break;
                    case ShaderPropertyType.Texture:
                        var t = m.GetTexture(pn);
                        f.Add(pn + "=" + (t != null ? t.name : "<null>")); break;
                }
            }
            catch { f.Add(pn + "=<读不出>"); }   // 纯比对用的诊断 ⇒ 绝不能因为它把导出打断
        }
        return f;
    }

    /// <summary>`Mesh` 的关键字段（撞名守卫用）。**只读元数据、不碰顶点缓冲** ——
    /// 顶点数据在流式网格上本来就不可靠（见 `ImportMesh` 注释里那张表），拿它当判据会误报。</summary>
    static List<string> MeshFields(Mesh m)
    {
        int idxTotal = 0;
        var sub = new List<string>();
        for (int i = 0; i < m.subMeshCount; i++)
        {
            var sm = m.GetSubMesh(i);
            sub.Add(sm.indexStart + "+" + sm.indexCount);
            idxTotal += sm.indexCount;
        }
        return new List<string>
        {
            "源名=" + m.name, "顶点数=" + m.vertexCount, "子网格=" + m.subMeshCount,
            "索引格式=" + m.indexFormat, "索引合计=" + idxTotal, "各子网格=" + string.Join(",", sub),
            "通道=" + string.Join(",", m.GetVertexAttributes()
                        .Select(a => a.attribute + "/" + a.format + "x" + a.dimension + "@" + a.stream)),
            "bounds中心=" + m.bounds.center, "bounds尺寸=" + m.bounds.size,
            "blendShape=" + m.blendShapeCount, "bindpose=" + (m.bindposes != null ? m.bindposes.Length : 0),
        };
    }

    /// <summary>两份 `字段=值` 列表的差异（**按字段名对齐**，不是按下标）。返回空 = 内容相同。</summary>
    static List<string> FieldDiff(List<string> a, List<string> b)
    {
        var da = new Dictionary<string, string>();
        foreach (var s in a) { int i = s.IndexOf('='); if (i > 0) da[s.Substring(0, i)] = s.Substring(i + 1); }
        var diff = new List<string>();
        foreach (var s in b)
        {
            int i = s.IndexOf('=');
            if (i <= 0) continue;
            var k = s.Substring(0, i);
            var v = s.Substring(i + 1);
            if (!da.TryGetValue(k, out var av)) diff.Add($"{k}: （先到的没有这一项）→ {v}");
            else if (av != v) diff.Add($"{k}: {av} → {v}");
        }
        return diff;
    }

    static string ShaderOf(Material m) => m != null && m.shader != null ? m.shader.name : "<null>";

    /// <summary>材质撞名守卫 —— 判据见 `MatByName` 头部那段。记的是**第一个**源；
    /// 后来每一个**不同的**源都跟它比一次 ⇒ 18 个撞名名字会打出一串警告，**那是有意的**：
    /// 每个「输家」都要点名（⛔ 不许静默覆盖）。</summary>
    static void GuardMatName(string stem, Material src)
    {
        var fields = MatFields(src);
        if (MatByName.TryGetValue(stem, out var first))
        {
            if (ReferenceEquals(first.src, src)) return;      // 同一个源对象，不是撞名
            var diff = FieldDiff(first.fields, fields);
            if (diff.Count == 0) return;                      // 内容等价 ⇒ 塌成一份无害，不出声
            var shown = string.Join(" / ", diff.Take(6));
            Debug.LogWarning($"[EffectExporter] 材质**撞名且内容不同**：`{stem}` —— "
                           + $"先到 `{first.src.name}`（shader {ShaderOf(first.src)}）、"
                           + $"后到 `{src.name}`（shader {ShaderOf(src)}）；"
                           + $"确定路径写同一份 ⇒ **后到的赢，先到的那种在工程里没有对应文件**。"
                           + $"差异 {diff.Count} 处（最多列 6）：{shown}" + (diff.Count > 6 ? " …" : ""));
            return;
        }
        MatByName[stem] = (src, fields);
    }

    /// <summary>网格撞名守卫 —— 与 `GuardMatName` 同形（字段表见 `MeshFields`）。</summary>
    static void GuardMeshName(string stem, Mesh m)
    {
        var fields = MeshFields(m);
        if (MeshByName.TryGetValue(stem, out var first))
        {
            if (ReferenceEquals(first.src, m)) return;
            var diff = FieldDiff(first.fields, fields);
            if (diff.Count == 0) return;
            var shown = string.Join(" / ", diff.Take(6));
            Debug.LogWarning($"[EffectExporter] 网格**撞名且内容不同**：`{stem}` —— "
                           + $"先到 `{first.src.name}`、后到 `{m.name}`；"
                           + $"确定路径写同一份 ⇒ **后到的赢，先到的那种在工程里没有对应文件**。"
                           + $"差异 {diff.Count} 处（最多列 6）：{shown}" + (diff.Count > 6 ? " …" : ""));
            return;
        }
        MeshByName[stem] = (m, fields);
    }

    public static Material ImportMaterial(Material src, out bool approx, out string origShader)
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
        int dropped = 0;                       // 源材质上**真有值**、目标 shader 却不认 ⇒ 这是丢件，不是正常跳过
        var droppedWhat = new List<string>();
        for (int i = 0; i < n; i++)
        {
            var pn = names[i];
            var tp = MapProp(mat, pn);
            if (!mat.HasProperty(tp))
            {
                // 🔴 2026-09-17：这里原来是一句**裸 `continue`**。源 shader 的独有属性本来就该跳过
                //    （每张材质几十个，全报出来会淹掉真话），但**源上真有贴图/颜色却没搬过去**是丢件。
                //    拖尾那张就是这么丢的：`MapProp` 把 `_MainTex` 改成了 `_BaseMap`，而自建 shader
                //    只声明了 `_MainTex` ⇒ 目标材质一个都没有（既没有映射名，也没退回原名）。
                var st = srcSh.GetPropertyType(i);
                if (st == ShaderPropertyType.Texture)
                {
                    var lost = src.GetTexture(pn);
                    if (lost != null) { dropped++; droppedWhat.Add($"{pn}={lost.name}"); }
                }
                else if (st == ShaderPropertyType.Color)
                {
                    var lc = src.GetColor(pn);
                    if (lc != Color.white) { dropped++; droppedWhat.Add($"{pn}={lc}"); }
                }
                continue;
            }
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
                        if (tx != null)
                        {
                            var imported = ImportTexture(tx);
                            // 🔴 **2026-09-17 加守卫**：原来这里直接 `mat.SetTexture(tp, ImportTexture(tx))`，
                            //    `ImportTexture` 返回 null 时就**静默把贴图写成 null** ——
                            //    实测 `Plasma_basic_blue` 等效果的**拖尾材质**（`Trail_fading` / `LightningTrail`）
                            //    就是这样丢的，渲出来是一块实心拖尾（就是「黑方块」那个形状）。
                            //    ⇒ 导不出来就**保留原值 + 大声报**，绝不静默清掉。
                            if (imported != null) { mat.SetTexture(tp, imported); copied++; }
                            else Debug.LogWarning($"[EffectExporter] 贴图导不出来，**保留原值不清空**："
                                                + $"{src.name} / 属性 {pn} = {tx.name}（{tx.GetType().Name}）；"
                                                + $"目标材质 {mat.name} 的属性 {tp}");
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                // 🔴 2026-09-17：原来是**空 catch** —— 逐属性拷贝中途抛一次，这个材质**剩下的属性
                //    就全都不搬了**，而且一声不吭。抛异常这件事本身必须看得见。
                Debug.LogWarning($"[EffectExporter] 材质 {src.name} 的属性 {pn} 拷贝失败"
                               + $"（该材质后续属性可能一起没搬）：{e.GetType().Name}: {e.Message}");
            }
        }
        if (dropped > 0)
            Debug.LogWarning($"[EffectExporter] 目标 shader {sh.name} 认不出 {dropped} 个属性，"
                           + $"**这些值没搬过去**（材质 {src.name}）：{string.Join(" / ", droppedWhat)}");

        // 混合 / 渲染状态：原材质有就照搬，没有就从 shader 名推断
        ApplyRenderState(mat, src);

        // URP 粒子的派生属性（软粒子淡出参数等）原版材质里没序列化，不补的话
        // 占位材质在编辑器里也会渲染成全黑。和运行时的 binder 用同一套逻辑，避免两边不一致
        WarpforgeVFX.WarpforgeEffectBinder.ApplyDerivedParticleDefaults(mat, src.shader ? src.shader.name : null);

        // 🔴 **2026-10-16：确定路径 + 原地覆盖**（这两行原来写的是
        //    `GenerateUniqueAssetPath($"{MatDir}/{Sanitize(src.name)}.mat")` + `AssetDatabase.CreateAsset`）。
        //    病灶：**路径不确定** —— 目标被占就吐 `X 1.mat` / `X 2.mat` …，而 `CreateAsset` 每次新建、guid 换一批。
        //    两个成因并存：**(a) 同名不同源材质**（原件就有，见 `MatByName` 那一段）+ **(b) `RunListed()`
        //    不清产物**（`Run()` 那条路每次先 `ClearGenerated()`，现存副本会被下一次全量重导自己带走）
        //    —— 实测 `LightningTrail_intense` 原版只有 1 个源材质、我们却有 12 份。
        //    ⇒ 确定路径对 (b) 是**必需**的；(a) 则把「多份文件」换成「后写的赢」，由 `GuardMatName` 出声。
        //    🔴 **判据同时改过**：旧的「`* [0-9].mat` 清到 0」**作废** —— 原版本来就有以数字结尾的材质名
        //    （亲扫 1092 个原版 Material JSON 证实 10 个：`Lens Flare 1` · `Glow Sphere 01` · `Flare 3` ·
        //    `firewall 1` …）。新判据 = **「形如 `X N.mat` 且同目录存在 `X.mat`」**：那一族今天
        //    = **730 个文件 / 190 个基名**（旧 glob `* [0-9].mat` = 718 · 含两位数 = 749 · 全部 `.mat` = 1399；
        //    W20 亲数 729 —— 同一条判据差 1，属快照差、不是判据变）。
        //    改法与同文件的 `ImportClip` 同形（grep `原地覆盖：形状不变`），同族先例还有 `ArenaBuilder.SaveOrReuse`；
        //    差别只有一条：**这里不逐份 `SaveAssets`** —— `Run()` 每 10 个存一次、收尾再存一次，
        //    `RunListed()` 收尾存（逐材质存一遍 = 白等 958 次）。
        //    ⚠️ 副作用：`Run()` 下次全量重导会**换掉 Materials/ 下所有 guid**（它先 `ClearGenerated()`）
        //      ⇒ 跑完必须跟一次 `BoosterPackExporter.Run`（见本类头部 `:22`）。
        string stem = Sanitize(src.name);
        GuardMatName(stem, src);                       // 撞名出声（内容不同才响 —— 见 `GuardMatName`）
        string path = $"{MatDir}/{stem}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        Material asst;
        if (existing != null)
        {
            // 原地覆盖：**guid 不变**（`ImportClip` 头部那段说明 + `CLAUDE.md` §三 那条规矩）
            EditorUtility.CopySerialized(mat, existing);
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(mat);  // 与 `ImportClip` 同：不留临时对象
            asst = existing;
        }
        else
        {
            AssetDatabase.CreateAsset(mat, path);
            asst = mat;
        }
        MatCache[src] = asst;
        MatMeta[src] = (approx, origShader);
        return asst;
    }

    /// <summary>把**原版材质里的属性名**落到**这个目标材质真正认得的那个名字**上。
    ///
    /// 🔴 2026-09-17 修正：这张表原来是**无条件改名**的（原版那条链是 URP 的 `_MainTex`/`_Color`，
    ///    我们的替代 shader 里 URP 那批用的是 `_BaseMap`/`_BaseColor`，所以加了映射）。
    ///    但**自建 shader 用的恰恰是 `_MainTex`/`_Color`**（`WFParticlesExtraColor` 与
    ///    `WFSpritesAdditive` 都是），一改名目标材质就不认 ⇒ 调用点那句 `if (!HasProperty) continue`
    ///    会把**贴图和颜色整个丢掉，还一声不吭**。
    ///    实测（2026-09-17，扫 `Assets/WarpforgeVFX/Materials/*.mat` 共 767 个）：
    ///    落在 `WarpforgeVFX/Particles/Extra Color` 上的 **207 个材质，里面带贴图的是 0 个**；
    ///    而 `_MatCap`（名字没被改）那批活得好好的。拖尾材质 `FadingTrail_add*`（54 个 prefab 引用）
    ///    的 `_MainTex` 全是 `{fileID: 0}` —— 「拖尾没贴图、渲成一块实心」的成因就在这里，
    ///    **不在**渲染器遍历那一层。
    ///    ⇒ **映射名目标材质不认就退回原名**，两个都不认才算真的没有。</summary>
    static string MapProp(Material dst, string p)
    {
        string mapped;
        switch (p)
        {
            case "_MainTex": mapped = "_BaseMap"; break;
            case "_Color": mapped = "_BaseColor"; break;
            default: return p;
        }
        return (dst != null && !dst.HasProperty(mapped)) ? p : mapped;
    }

    /// <summary>从原 shader 名推断混合模式。
    /// legacy 粒子 shader（Mobile/Particles/Additive 等）把混合写死在 shader 里，
    /// 材质上根本没有 _SrcBlend/_DstBlend 属性 —— 照搬默认值会把加法发光渲染成不透明。</summary>
    /// <remarks>🔴 **2026-09-25 判据已搬到 `WarpforgeVFX.ArenaOriginalMaterial.InferBlendFromShaderName`**
    /// —— 粒子那条路（`ArenaBuilder.GetOrCreateParticleMaterial`）也要用**同一份**才不会再分成两套。
    /// 这里只转发，一个字都不改。</remarks>
    static (BlendMode sb, BlendMode db, float zwrite, bool transparent) InferFromShader(string shaderName)
    {
        int sb, db; float zw; bool tr;
        ArenaOriginalMaterial.InferBlendFromShaderName(shaderName, out sb, out db, out zw, out tr);
        return ((BlendMode)sb, (BlendMode)db, zw, tr);
    }

    /// <summary>转发到 `WarpforgeVFX.ArenaOriginalMaterial.SetBlend`（**判据只此一处** ——
    /// 2026-09-25 起粒子那条路 `ArenaBuilder.GetOrCreateParticleMaterial` 也用同一份，不能再分成两套）。
    ///
    /// 🔴 **预乘混合必须开 `_ALPHAPREMULTIPLY_ON`**（2026-09-19 补，E 组第三轮）。
    /// 原版那批材质里 `_SrcBlend=1(One) + _DstBlend=10(OneMinusSrcAlpha)` 就是**预乘 alpha**，
    /// 而着色器里那一段 `col.rgb *= col.a` 挂在 `#ifdef _ALPHAPREMULTIPLY_ON` 下
    /// ⇒ **不开这个关键字 = 按未预乘输出 ⇒ 偏亮**（alpha 越小倍数越大：a=0.2 时 5×）。
    /// 判据只看**混合状态**，不看 shader 名 —— 原版 `Universal Render Pipeline/Particles/Unlit`
    /// 自己也是这么开的（那 9 个材质里有一部分走的就是它）。
    ///
    /// 实测：我们的 `WFParticlesExtraColor` 那 316 个材质里 **9 个**是这一组
    /// （`Explosion_Color` · `FireRed` / `FireBlack` · `Flames Loop Red/Green` ·
    ///  `Waterfall_ExtraColor` · `Explosion_big_ground` · `Stealth_Icon` · `Default-Particle`），
    /// 而它们的 `m_ValidKeywords` **全是空**（关键字在导入时被丢掉，因为我们自建的 shader
    /// **没有声明这个变体** ⇒ 那一段代码从来没被编译进来过）。
    /// 出处与逐属性对照：`资料/普查产出_0918/E组_自建shader_WFParticlesExtraColor_逐属性.md` §2·①。</summary>
    static void SetBlend(Material m, BlendMode sb, BlendMode db, float zwrite)
    {
        ArenaOriginalMaterial.SetBlend(m, (int)sb, (int)db, zwrite);
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

        public static Texture2D ImportTexture(Texture tex)
    {
        if (tex == null) return null;
        if (tex is Cubemap) return null;                       // 立方图单独处理，先跳过
        if (TexCache.TryGetValue(tex, out var c) && c != null) return c;

        var src = tex as Texture2D;
        if (src == null) return null;

        // 🔴 2026-09-17：分段报错。原来这里是一个整体的 `try`，抛了只知道「某处抛了」
        //    （实测 `Sororitas_VFX_Mask` / `Battle Arena Space Wolves Floor` 抛 `ArgumentNullException`，
        //    但抛在 `ReadableCopy` 还是 `EncodeToPNG` 分不出来）。带上尺寸/格式/可读性才判得动。
        Texture2D readable;
        try { readable = ReadableCopy(src); }
        catch (Exception e)
        {
            Debug.LogWarning($"[EffectExporter] ReadableCopy 抛异常：{src.name} {src.width}x{src.height}"
                           + $" fmt={src.format} isReadable={src.isReadable} mip={src.mipmapCount}"
                           + $" —— {e.GetType().Name}: {e.Message}");
            throw;
        }
        if (readable == null) return null;

        // 🔴 2026-09-17 定位到的真因：**`EncodeToPNG` 对压缩格式（实测 DXT5）返回 `null`，而且「不抛异常」**
        //    ⇒ 下面那句 `File.WriteAllBytes(full, png)` 拿 null 当字节数组，抛
        //    `ArgumentNullException: Value cannot be null. Parameter name: bytes` ——
        //    贴图**整个丢**，而报出来的栈指向 `File.WriteAllBytes`，**看着跟贴图毫无关系**
        //    （所以它一直挂在「3 个材质导贴图抛异常」名下，没人知道是格式问题）。
        //    实测受害者：`Sororitas_VFX_Mask`（256², **DXT5**）与 `Battle Arena Space Wolves Floor`
        //    （3 个材质：`Sororitas_Flames_TwoLayer` · `Space Wolves BG First Light` · `BG Full Moon`）。
        //    ⚠️ 判据是**格式**不是可读性：同一次导出里 8192² 的 `Battle Arena Space Wolves Props 1`
        //    是 **RGBA32**、编码正常 —— 别把这条记成「大图导不出来」。
        //    ⇒ 先直接编码；返回 null 就**摊成未压缩 RGBA32 再编一次**；还不行就**大声报 + 返回 null**，
        //    绝不再让 `File.WriteAllBytes` 去抛。
        byte[] png = Encode(readable);
        if (png == null)
        {
            png = Encode(Flatten(readable));
            if (png == null)
            {
                Debug.LogWarning($"[EffectExporter] 贴图编不出来（摊平之后仍然编不出），这张贴图会缺："
                               + $"{src.name} {src.width}x{src.height} fmt={src.format} isReadable={src.isReadable}");
                return null;
            }
            Debug.Log($"[EffectExporter] 贴图 {src.name} 的格式 {src.format} 编不了 PNG，已摊成 RGBA32 后编出"
                    + $"（{src.width}x{src.height}）");
        }

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
            // 🔴🔴 **2026-10-02：`mipmapEnabled` 原来硬编码 `true` —— 这是错的，纠正为「照原版」。**
            //
            // **代价（实测）**：`Buff_DA_Forest_Self` 的拖尾槽差了 **2.80×**，两边**粒子位置逐位相同 ·
            // PS 模块一致 · 材质属性逐条一致 · shader 相同**，一路查到贴图才发现：
            //   原版 `Shine trail` = `{128x128, DXT5, m**1**, …}`（**1 层 = 无 mip**）
            //   导出 `Shine trail` = `{128x128, DXT5, m**8**, …}`（8 层 = 完整 mip 链）
            // 为什么差这么多：**拖尾是极度拉伸的几何** ⇒ GPU 按 UV 导数采到很低的 mip
            // ⇒ 有 mip 时整条拖尾被平均成一团糊（并排图：原版一小段稀疏拖尾 vs 导出绕一整圈的闭环 ——
            // 环是 15 个粒子各自被糊出来的，不是形状变了）。**这是「看着像形状/亮度差」的假象。**
            //
            // **影响面**（实读 `battleprefabs_vfxandmisc_assets_all.bundle` 的 346 张 `Texture2D`）：
            // `m_MipCount` 分布 = `{1: 64, 6: 2, 7: 12, 8: 61, 9: 106, 10: 55, 11: 32, 12: 12, 14: 2}`
            // ⇒ **64 张原版本来没有 mip**，我们全给补上了。
            //
            // ⚠️ **战场那边早就对了**（`ArenaBuilder.cs:1297` 硬编码 `false` + 注释「原版 `m_MipCount = 1`」）
            //    —— 是**效果这条链漏了**。这类「导入器静默改坏原版资产」是**本工程记过档的坑**
            //    （见 [[unity-import-mangles-original-assets]]），这是它第三次现形。
            // ⚠️ **属性名**：源是 `Texture2D.mipmapCount`（Unity 的只读属性），不是序列化字段 `m_MipCount`。
            //    非 2 的幂 / 非方形的贴图层数与「完整链」不同 ⇒ **不用算，直接读**。
            int srcMips = 1;
            try { srcMips = Mathf.Max(1, src.mipmapCount); } catch { }
            ti.mipmapEnabled = srcMips > 1;
            // 原版是 `Repeat`（见 `ArenaBuilder` 同族的对照）；不改。
            ti.wrapMode = TextureWrapMode.Repeat;
            // 🔴 顺手堵一个**同类**的静默截断：导入默认 `maxTextureSize = 2048`，超过就**悄悄缩一半**。
            //    实读这批 346 张最大正好 2048 ⇒ **今天改它没有效果**，所以不算「多改一个变量」；
            //    写在这里是为了将来真有更大的贴图时不再静默。
            int need = Mathf.NextPowerOfTwo(Mathf.Max(src.width, src.height));
            ti.maxTextureSize = Mathf.Max(2048, need);
            if (need > 2048)
                Debug.LogWarning($"[EffectExporter] 贴图 {src.name} 是 {src.width}x{src.height} " +
                                 $"⇒ `maxTextureSize` 提到 {ti.maxTextureSize}（Unity 默认 2048 会静默缩一半）");
            ti.SaveAndReimport();
            if (srcMips == 1)
                Debug.Log($"[EffectExporter] 贴图 {src.name} 原版**没有 mip**（m_MipCount=1）⇒ 按无 mip 导入");
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
    public static Sprite ImportSprite(Sprite s)
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
        // 🔴 2026-09-17：**crunch 压缩的纹理，就算 `isReadable == true` 也读不出像素** ——
        //    `GetPixels` 直接报错（「texture is crunch compressed while this function does not
        //    support crunched textures」）、`EncodeToPNG` **返回 null（不抛）**。
        //    实测受害者 `Battle Arena Space Wolves Floor`（4096², **DXT5Crunched**）。
        //    ⇒ 这类必须**强制走下面那条 GPU 路**（`Graphics.Blit` → `ReadPixels`），
        //    在 GPU 上采样是不受 crunch 限制的。
        if (src.isReadable && !IsCrunched(src.format)) return src;

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

    /// <summary>把 bundle 里的 `Mesh` 落成工程 `.asset`。
    ///
    /// 🔴 **不能用 `Object.Instantiate`**（2026-10-01 晚实测，代价 = 一条效果 16×）：顶点数据**走流式**的网格
    ///   （包里 `m_VertexData.m_DataSize == 0`、数据挂在 `m_StreamData` 的 `.resS` 上）**复制过来是空的/垃圾**：
    ///
    ///   | 资产 | 顶点 | 顶点极值 |
    ///   |---|---|---|
    ///   | `Spirt Stone 1` | 748 | **±1.3e38 / inf**（未初始化内存） |
    ///   | `Card_Remnant_HO Optimization 2` | 6541 | **inf** |
    ///   | `Card 3D Subdivided` | 1641 | **inf** |
    ///
    ///   其余 **199 个网格是好的** —— 区别就在「原件是不是流式」。`AnimClipProbe` 的同族探针
    ///   `MeshImportMethodProbe` 把 **8 种导法**并排量过（判据 = 落盘后从 `.asset` 的 YAML 里读回
    ///   `_typelessdata` 算顶点极值）：`Instantiate` / `UploadMeshData` / `CopySerialized` /
    ///   先碰缓冲再复制 …… **七种全给出全 0**，只有**「从 `GraphicsBuffer` 读回、`SetVertexBufferData` 重建」**是对的。
    ///
    ///   后果是**看得见的**：`RemnantBody3D Aeldari` 那颗石头渲成一团铺满画面的白，整条效果亮度顶到 **145×**
    ///   （台账里那条 16×）。判据链 → `资料/特效还原_进度与交接.md` §〇之四。
    ///
    /// ⚠️ **只在「形状简单」时才走深拷贝**（单顶点流 + 无 blendShape + 无 bindpose）——
    ///   深拷贝只搬**顶点流 0 与索引流**，形状复杂（多流/蒙皮/形变）的网格会丢数据
    ///   ⇒ 那种情况退回 `Instantiate` **并出声**（不许静默）。</summary>
    public static Mesh ImportMesh(Mesh m)
    {
        if (m == null) return null;
        if (MeshCache.TryGetValue(m, out var c) && c != null) return c;

        Mesh copy = null;
        bool simple = !m.GetVertexAttributes().Any(a => a.stream != 0)
                      && m.blendShapeCount == 0 && (m.bindposes == null || m.bindposes.Length == 0);
        if (simple)
        {
            try { copy = DeepCopyMesh(m); }
            catch (Exception e)
            {
                Debug.LogWarning($"[EffectExporter] 网格 `{m.name}` 深拷贝抛了（{e.GetType().Name}: {e.Message}）"
                               + " ⇒ 退回 `Instantiate`（顶点数据可能丢）");
                copy = null;
            }
        }
        else
        {
            Debug.LogWarning($"[EffectExporter] 网格 `{m.name}` 形状复杂（多顶点流 / 有 blendShape / 有 bindpose）"
                           + " ⇒ 退回 `Instantiate`；**如果它又是流式的，顶点数据会丢**（见 `ImportMesh` 注释）");
        }
        if (copy == null) copy = UnityEngine.Object.Instantiate(m);

        copy.name = Sanitize(m.name);
        // 🔴 **2026-10-16：确定路径 + 原地覆盖**（原来 = `GenerateUniqueAssetPath` + `CreateAsset`，
        //    与 `ImportMaterial` 是**同一个病**：`Meshes/` 亲数 **164** 个 `X N.asset` —— 账上只记了 Materials）。
        //    反证很干净：已走确定路径的 `Animations/`（`ImportClip`）与 `Controllers/` 亲数**都是 0** 个编号文件。
        //    判据侧同 `ImportMaterial`：新判据 = **「形如 `X N.asset` 且同目录存在 `X.asset`」**
        //    —— 那一族今天 = **102** 个（`Meshes/` 全部 `.asset` = 250；W20 记的 **164** = 只按
        //    「名字形如 `X N`」数的口径、没查基名在不在）。两条路是同一个写法，所以一起改。
        //    ⚠️ **计数坑**：判据换后缀时**正则要跟着换** —— 先前拿 `.mat` 的正则数 `.asset`，得到过假 0。
        //    ⚠️ **不逐份 `SaveAssets`**：由调用方批量存（与 `ImportMaterial` 同）。
        //    ⚠️ **这条路没实跑过**：`EditorUtility.CopySerialized` 落到**已有网格资产**上是**未验证**的。
        //       `MeshImportMethodProbe` 的 M3 只验过「从**包里的**网格 `CopySerialized` 进 `new Mesh()`」，
        //       那一条本来就该丢数据（源是流式的）—— 与这里「源是 `DeepCopyMesh` 出来的健康网格」
        //       **不是同一个输入**，所以 M3 的结论**不能直接外推**（也**不能**当成「这条路是安全的」）。
        //       ⇒ 第一次跑完导出器**必须回读** `.asset` 的 `_typelessdata` 看顶点极值
        //       （读法与判据见 `MeshImportMethodProbe.ReadBackVertexRange`：NaN/Inf 或 |值|>100 = 还是垃圾）。
        //       真出事的话退路是「把 `DeepCopyMesh` 改成往**已有资产**里 `SetVertexBufferData` 就地重灌」，
        //       而不是退回 `CreateAsset`（那会重新长出 `X N.asset`、并换 guid）。
        string stem = copy.name;
        GuardMeshName(stem, m);                        // 撞名出声（内容不同才响 —— 见 `GuardMeshName`）
        string path = $"{MeshDir}/{stem}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            // 原地覆盖：**guid 不变**（判据同 `ImportClip`）
            EditorUtility.CopySerialized(copy, existing);
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(copy);
            MeshCache[m] = existing;
            return existing;
        }
        AssetDatabase.CreateAsset(copy, path);
        MeshCache[m] = copy;
        return copy;
    }

    /// <summary>把网格**逐流读出来**再重建一份（顶点流 0 + 索引流 + 子网格 + bounds）。
    /// `GraphicsBuffer.GetData` 只认**基元数组** ⇒ 按 4 字节切（`uint[]`），再 `BlockCopy` 成字节。</summary>
    static Mesh DeepCopyMesh(Mesh src)
    {
        var dst = new Mesh();
        dst.name = src.name;

        using (var vb = src.GetVertexBuffer(0))
        {
            int stride = src.GetVertexBufferStride(0);
            int words = vb.count * stride / 4;
            var w = new uint[words];
            vb.GetData(w);
            var bytes = new byte[words * 4];
            Buffer.BlockCopy(w, 0, bytes, 0, bytes.Length);
            dst.SetVertexBufferParams(src.vertexCount, src.GetVertexAttributes());
            dst.SetVertexBufferData(bytes, 0, 0, bytes.Length, 0, MeshUpdateFlags.Default);
        }

        dst.indexFormat = src.indexFormat;
        // ⚠️ **索引总数要按「所有子网格的最大 end」算**，不能用 `GetIndexCount(0)` ——
        //    那个只给**第 0 个子网格**的条数。踩过：`Card_Remnant_HO Optimization 2` 有 2 个子网格
        //    （第 1 个 start=11703 count=3612），按 11703 建缓冲 ⇒
        //    `Invalid submesh index start/count values` ⇒ 整块退回 `Instantiate`、顶点又变成垃圾。
        int totalIdx = 0;
        for (int i = 0; i < src.subMeshCount; i++)
        {
            var sm = src.GetSubMesh(i);
            totalIdx = Mathf.Max(totalIdx, sm.indexStart + sm.indexCount);
        }
        using (var ib = src.GetIndexBuffer())
        {
            int words = (ib.count * ib.stride + 3) / 4;
            var w = new uint[words];
            ib.GetData(w);
            var bytes = new byte[words * 4];
            Buffer.BlockCopy(w, 0, bytes, 0, bytes.Length);
            int idxSize = src.indexFormat == UnityEngine.Rendering.IndexFormat.UInt16 ? 2 : 4;
            dst.SetIndexBufferParams(Mathf.Max(totalIdx, 1), src.indexFormat);
            dst.SetIndexBufferData(bytes, 0, 0, Mathf.Min(bytes.Length, Mathf.Max(totalIdx * idxSize, 4)),
                                   MeshUpdateFlags.Default);
        }

        dst.subMeshCount = src.subMeshCount;
        for (int i = 0; i < src.subMeshCount; i++) dst.SetSubMesh(i, src.GetSubMesh(i));
        dst.bounds = src.bounds;
        return dst;
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  动画那一跳（2026-10-01 新增）
    //  `AnimationClip` → `Assets/WarpforgeVFX/Animations/<名>.anim`
    //  `AnimatorController` → `Assets/WarpforgeVFX/Animators/<名>.controller`
    //
    //  🔴 **为什么必须做这一跳**：`Animator.m_Controller` 指的那份控制器是 **bundle 资产**，
    //     `PrefabUtility.SaveAsPrefabAsset` 落不下来 ⇒ 序列化成
    //     `m_Controller: {fileID: …, guid: 00000000000000000000000000000000}`（guid 全 0 的伪引用）
    //     ⇒ 运行时解析成 **null**、动画一帧都不播，而**编辑器里看不出来**（prefab 是「有 Animator」的）。
    //     与 `ImportSprite` 头部那段记载的是同一个病，只是资产类型不同。
    //     实测受害者两个（共用同一个控制器）：`Card 3D Death Explosion`（阵亡爆散体）·
    //     `Necrons death explosion`。判据 = 导出产物里那两行 `m_Controller`。
    //
    //  🔴 **不删旧文件、按确定路径原地覆盖**：
    //     ① `AssetDatabase.GenerateUniqueAssetPath` + 「每次先删光」会让 **guid 每跑一次就换一批**，
    //        而别的 prefab / 别的效果还指着上一批（这两个 prefab 就共用一份控制器）⇒ 引用变 null（工程规
    //        矩见 `CLAUDE.md` §三「生成资产别用 GenerateUniqueAssetPath + 每次先删光」）。
    //     ② 所以这里：路径 = `目录/<Sanitize(名字)>`，**有了就地覆盖**，没有才 `CreateAsset`。
    //
    //  🔴 **控制器为什么不能靠 `Instantiate` + `CreateAsset` 直接落**（2026-10-01 实测两次）：
    //     包里那份是**运行时格式**（`m_Controller` / `m_TOS` / `m_StateMachineArray`），
    //     工程 `.controller` 要的是**编辑器格式**（`m_AnimatorLayers` / `m_AnimatorParameters`）
    //     —— 落出来的资产读回来是 **`0 层 0 状态`** 的空壳（`CopySerialized` 覆盖那一趟也一样）。
    //     ⇒ 只能**按数据表照建**：`数据/游戏数据/animator_controllers.json`
    //       （生成器 `工具/gen_animator_controllers.py`，逐字段出自 `assets_full/…/AnimatorController/*.json`）。
    //     全库只有 **6** 份控制器，结构同构（1 层 / 1 状态 / 单节点 1D 混合树 / 0 参数 / 0 过渡）。
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static readonly Dictionary<AnimationClip, AnimationClip> ClipCache = new();
    static readonly Dictionary<RuntimeAnimatorController, RuntimeAnimatorController> CtrlCache = new();
    static readonly Dictionary<string, AnimationClip> ClipByName = new();

    const string CtrlTablePath = @"D:\4\Unity\数据\游戏数据\animator_controllers.json";

    [Serializable] class CtrlStateDesc
    {
        public string name; public string path;
        public float speed; public float cycleOffset;
        public bool mirror; public bool writeDefaultValues; public bool loop; public bool ikOnFeet;
        public int tagID; public int blendType; public string blendTypeName;
        public float blendDuration; public string clip;
    }
    [Serializable] class CtrlLayerDesc
    {
        public string name; public float defaultWeight; public int blendingMode; public bool ikPass;
        public int syncedLayerIndex; public int defaultState;
        public CtrlStateDesc[] states;
    }
    [Serializable] class CtrlParamDesc
    {
        public int nameID; public int type;
        public float defaultFloat; public int defaultInt; public bool defaultBool;
    }
    [Serializable] class CtrlDesc
    {
        public string name; public string source; public int pathId;
        public string[] clips; public CtrlParamDesc[] parameters; public CtrlLayerDesc[] layers;
    }
    [Serializable] class CtrlTable
    {
        public string _note; public string[] _unhandled; public int count; public CtrlDesc[] controllers;
    }

    static Dictionary<string, CtrlDesc> _ctrlDescs;

    static CtrlDesc ControllerDesc(string name)
    {
        if (_ctrlDescs == null)
        {
            _ctrlDescs = new Dictionary<string, CtrlDesc>();
            try
            {
                var t = JsonUtility.FromJson<CtrlTable>(File.ReadAllText(CtrlTablePath));
                foreach (var c in t.controllers) _ctrlDescs[c.name] = c;
                Debug.Log($"[EffectExporter] 控制器数据表：{t.count} 份"
                        + (t._unhandled != null && t._unhandled.Length > 0
                           ? $"（生成器报了 {t._unhandled.Length} 条「解不动」，见 `{CtrlTablePath}`）" : ""));
            }
            catch (Exception e)
            {
                Debug.LogError($"[EffectExporter] 读不了控制器数据表 `{CtrlTablePath}`（{e.GetType().Name}: "
                             + $"{e.Message}）⇒ 跑一次 `工具/gen_animator_controllers.py`；"
                             + "在这之前，凡是带 `Animator` 的效果都**没有动画**");
            }
        }
        return _ctrlDescs.TryGetValue(name, out var d) ? d : null;
    }

    /// <summary>在所有已加载的包里按名字找一份 `AnimationClip`（找不到返回 null、并出声）。</summary>
    static AnimationClip FindClipInPacks(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (ClipByName.TryGetValue(name, out var c) && c != null) return c;
        foreach (var kv in LoadedPacks)
        {
            AnimationClip[] all = null;
            try { all = kv.Value.LoadAllAssets<AnimationClip>(); }
            catch (Exception e) { Debug.LogWarning($"EX1 `{kv.Key}` 枚举 AnimationClip 抛了：{e.Message}"); }
            if (all == null) continue;
            foreach (var a in all)
                if (a != null && a.name == name) { ClipByName[name] = a; return a; }
        }
        Debug.LogError($"[EffectExporter] 已加载的包里没有名为 `{name}` 的 AnimationClip"
                     + " ⇒ 控制器里那一格动作为空（不是静默：动画不会播）");
        return null;
    }

    /// <summary>把 bundle 里的 `AnimationClip` 导出成工程 `.anim`。
    /// ⚠️ **不用 `AssetDatabase.CreateAsset(runtimeClip)` 直接建** —— bundle 里的对象已经是「持久对象」，
    /// 必须先 `Instantiate` 出一份再落盘（这是 Unity 的规矩，不是我们的选择）。
    /// ⚠️ `loop`：原版这个量在包里是状态上的 `m_Loop`（`AnimationClip` 自己没有这个字段），
    ///    工程侧等价物是 clip 的 `AnimationClipSettings.loopTime` ⇒ 由调用方传进来。</summary>
    public static AnimationClip ImportClip(AnimationClip src, bool? loop = null)
    {
        if (src == null) return null;
        if (ClipCache.TryGetValue(src, out var c) && c != null) return c;

        var copy = UnityEngine.Object.Instantiate(src);
        copy.name = Sanitize(src.name);
        string path = $"{AnimDir}/{copy.name}.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        AnimationClip asst;
        if (existing != null)
        {
            // 原地覆盖：形状不变、**guid 不变**（见上面那段说明）
            EditorUtility.CopySerialized(copy, existing);
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(copy);
            asst = existing;
        }
        else
        {
            AssetDatabase.CreateAsset(copy, path);
            asst = copy;
        }
        if (loop.HasValue)
        {
            var st = AnimationUtility.GetAnimationClipSettings(asst);
            if (st.loopTime != loop.Value)
            {
                st.loopTime = loop.Value;
                AnimationUtility.SetAnimationClipSettings(asst, st);
                EditorUtility.SetDirty(asst);
            }
        }
        ClipCache[src] = asst;
        AssetDatabase.SaveAssets();
        return asst;
    }

    /// <summary>把 bundle 里的 `AnimatorController` 落成工程 `.controller`。
    ///
    /// 🔴 **怎么落的**：**按数据表照建**（`数据/游戏数据/animator_controllers.json`）。
    ///   · 为什么不能直接搬：包里那份是**运行时格式**，工程 `.controller` 是**编辑器格式**（见上面那段），
    ///     `Instantiate`+`CreateAsset` 与 `CopySerialized` **两条路都实测过**、落出来都是空壳（0 层 0 状态）。
    ///   · 数据表由 `工具/gen_animator_controllers.py` 从 `assets_full/…/AnimatorController/*.json` 摊平，
    ///     逐字段（层名 / 状态名 / speed / cycleOffset / mirror / writeDefaultValues / loop / 动作 clip / 默认状态）。
    ///   · 已有同名资产时：**清空重建**而不是删文件 —— **guid 不变**
    ///     （`Card 3D Death Explosion` 与 `Necrons death explosion` 共用这一份）。
    ///
    /// 🔴 **动作那一格还要再跳一次**：数据表给的是 clip **名字**，要 `FindClipInPacks` 去包里取对象，
    ///   再 `ImportClip` 落成工程 `.anim`。漏了这一跳 = 控制器在、动作是空的，跟没导一样。</summary>
    static RuntimeAnimatorController ImportAnimatorController(RuntimeAnimatorController src)
    {
        if (src == null) return null;
        if (CtrlCache.TryGetValue(src, out var cached) && cached != null) return cached;

        var ac = src as UnityEditor.Animations.AnimatorController;
        if (ac == null)
        {
            Debug.LogWarning($"[EffectExporter] `{src.name}` 不是 AnimatorController（实为 {src.GetType().Name}）"
                           + " ⇒ 这一处动画**没导**，运行时那一格是空的");
            return null;
        }

        var desc = ControllerDesc(ac.name);
        if (desc == null)
        {
            Debug.LogError($"[EffectExporter] 控制器 `{ac.name}` 不在 `{CtrlTablePath}` 里 ⇒ "
                         + "那个效果不会有动画（跑一次 `工具/gen_animator_controllers.py` 补表）");
            return null;
        }

        string path = $"{CtrlDir}/{Sanitize(ac.name)}.controller";
        var dst = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);
        if (dst != null && dst.layers.Length == 0)
        {
            // 空壳（0 层）**清不掉**（没有层可清），只能删掉重建。
            // 来历：这一版之前用「Instantiate + CreateAsset / CopySerialized」落盘的失败产物。
            // ⚠️ 删文件会换 guid ⇒ **引用它的 prefab 必须跟着重导**（两个爆散体都在 `ListedPrefabs` 里）。
            Debug.LogWarning($"[EffectExporter] `{path}` 是空壳（0 层，旧版本的失败产物）⇒ **删掉重建**"
                           + "（guid 会换，所以引用它的 prefab 必须跟着重导）");
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            dst = null;
        }
        if (dst == null)
        {
            dst = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
            if (dst == null)
            {
                Debug.LogError($"[EffectExporter] 建不了控制器资产 `{path}`（那个效果会没有动画）");
                return null;
            }
        }
        else
        {
            // 清空重建（**保住 guid**）：留一层、把它下面所有状态删掉、参数清空
            while (dst.layers.Length > 1) dst.RemoveLayer(dst.layers.Length - 1);
            var sm0 = dst.layers[0].stateMachine;
            sm0.defaultState = null;
            foreach (var ch in sm0.states.ToArray()) sm0.RemoveState(ch.state);
            foreach (var p in dst.parameters.ToArray()) dst.RemoveParameter(p);
        }

        // ---- 层 / 状态 / 动作（照数据表建）----
        // ⚠️ **`AnimatorControllerLayer` 是结构体**：`dst.layers[li]` 是副本，改了**必须写回**，
        //    否则「层名/权重/混合模式设了等于没设」（静默）。状态机是类引用，改它不用写回。
        var dstLayers = dst.layers;
        while (dstLayers.Length < desc.layers.Length) { dst.AddLayer("Layer"); dstLayers = dst.layers; }
        int madeStates = 0, missingClip = 0;
        float durWarn = -1f; string durWarnName = null;
        for (int li = 0; li < desc.layers.Length && li < dstLayers.Length; li++)
        {
            var ld = desc.layers[li];
            var dl = dstLayers[li];
            dl.name = ld.name;
            dl.defaultWeight = ld.defaultWeight;
            dl.blendingMode = (UnityEditor.Animations.AnimatorLayerBlendingMode)ld.blendingMode;
            dl.iKPass = ld.ikPass;
            dstLayers[li] = dl;

            var dsm = dl.stateMachine;
            dsm.name = ld.name;
            var added = new List<UnityEditor.Animations.AnimatorState>();
            foreach (var sd in ld.states ?? new CtrlStateDesc[0])
            {
                var ns = dsm.AddState(string.IsNullOrEmpty(sd.name) ? "State" : sd.name);
                var raw = FindClipInPacks(sd.clip);
                if (raw == null) { missingClip++; }
                else
                {
                    var cl = ImportClip(raw, sd.loop);
                    ns.motion = cl;
                    // 原版的运动是「单节点 1D 混合树」，那个节点的时长就是这一条；直接用 clip
                    // 当动作时时长 = clip 自己的长度 ⇒ 两者对不上就等于**改了播放时长**，要出声。
                    if (sd.blendDuration > 0f && cl != null && Mathf.Abs(cl.length - sd.blendDuration) > 0.02f)
                    { durWarn = cl.length - sd.blendDuration; durWarnName = sd.name; }
                }
                ns.speed = sd.speed;
                ns.cycleOffset = sd.cycleOffset;
                ns.mirror = sd.mirror;
                ns.writeDefaultValues = sd.writeDefaultValues;
                ns.iKOnFeet = sd.ikOnFeet;
                added.Add(ns);
                madeStates++;
            }
            if (ld.defaultState >= 0 && ld.defaultState < added.Count) dsm.defaultState = added[ld.defaultState];
            else if (added.Count > 0)
                Debug.LogWarning($"[EffectExporter] 控制器 `{ac.name}` 层 `{ld.name}` 的默认状态下标 "
                               + $"{ld.defaultState} 越界（共 {added.Count} 个）⇒ 取第 0 个");
            if (added.Count > 0 && dsm.defaultState == null) dsm.defaultState = added[0];
        }
        dst.layers = dstLayers;

        EditorUtility.SetDirty(dst);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        int layers = dst.layers.Length;
        int states = dst.layers.Sum(l => l.stateMachine != null ? l.stateMachine.states.Length : 0);
        LastDetail = (LastDetail ?? "") + $" | 动画：控制器`{ac.name}`（{layers}层/{states}状态）";
        if (layers == 0 || states == 0)
            Debug.LogError($"[EffectExporter] 控制器 `{ac.name}` 建完仍是 **{layers} 层 / {states} 状态**"
                         + " ⇒ 这份资产是空的，动画不会播（停下来查，别当成功）");
        else
            Debug.Log($"[EffectExporter] 控制器 `{ac.name}` → `{path}`（照 "
                    + $"`{Path.GetFileName(CtrlTablePath)}` 建：{layers} 层 · {states} 状态）"
                    + (missingClip > 0 ? $" 🔴 {missingClip} 个动作没取到 clip" : ""));
        if (durWarnName != null)
            Debug.LogWarning($"[EffectExporter] 控制器 `{ac.name}` 状态 `{durWarnName}`："
                           + $"原版那条动作的时长（混合树节点）和我们这份 clip 的长度差 {durWarn:0.###} s"
                           + " ⇒ 播放时长会不一样（原版是 1D 混合树的单节点，我们直接挂 clip）");

        CtrlCache[src] = dst;
        return dst;
    }

    public static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "unnamed";
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace('/', '_').Trim();
    }

    /// <summary>「crunch 压缩」的几种格式。这类纹理**标记成可读也没用**：
    /// `GetPixels` / `GetRawTextureData` 都不支持，`EncodeToPNG` 返回 null。
    /// **只有走 GPU（`Graphics.Blit` → `ReadPixels`）才读得出来。**</summary>
    static bool IsCrunched(TextureFormat f)
    {
        return f == TextureFormat.DXT1Crunched || f == TextureFormat.DXT5Crunched
            || f == TextureFormat.ETC_RGB4Crunched || f == TextureFormat.ETC2_RGBA8Crunched;
    }

    /// <summary>异常栈的前几帧压成一行 —— 用来定位「到底抛在哪一步」。</summary>
    static string FirstFrames(Exception e)
    {
        var s = e.StackTrace;
        if (string.IsNullOrEmpty(s)) return "<无栈>";
        return string.Join(" ← ", s.Split('\n').Take(3).Select(x => x.Trim()));
    }

    /// <summary>`EncodeToPNG` 的**不抛版本**。
    /// 🔴 它对**压缩格式**（实测 DXT5）是**返回 null、不抛异常**的 —— 调用方**必须判 null**，
    /// 否则 `File.WriteAllBytes(path, null)` 会抛 `ArgumentNullException: bytes`，
    /// 报出来的栈指向 `File.WriteAllBytes`，跟贴图像没关系（2026-09-17 就是被这个绕了一圈）。</summary>
    static byte[] Encode(Texture2D t)
    {
        if (t == null) return null;
        try { return t.EncodeToPNG(); } catch { return null; }
    }

    /// <summary>把一张可读纹理摊成**未压缩的 RGBA32**（`GetPixels` 会把压缩格式解压出来）。
    /// 只在「不摊就编不出 PNG」时才调 —— 8192² 摊一次是 256 MB，别无条件摊。</summary>
    static Texture2D Flatten(Texture2D t)
    {
        if (t == null) return null;
        try
        {
            var px = t.GetPixels();
            var o = new Texture2D(t.width, t.height, TextureFormat.RGBA32, false);
            o.SetPixels(px);
            o.Apply();
            return o;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[EffectExporter] 摊平 {t.name} 失败：{e.GetType().Name}: {e.Message}");
            return null;
        }
    }
}
