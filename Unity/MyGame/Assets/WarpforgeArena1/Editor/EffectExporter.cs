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
    // 🆕 2026-10-09（`A1089` 的另一半）：**不是导出器生成**的那两张 VAT 贴图的**真身常驻目录**。
    //    `Runtime/` 在 `.gitignore` 白名单里（`:27`）、且 `ClearGenerated` **只管 Mat/Tex/Mesh/Prefab 四个目录**
    //    ⇒ 放这儿不会被清。文件名**与 `Textures/` 里那份逐字相同**（只换目录）。
    const string RuntimeVatDir = Root + "/Runtime/Vat";
    static readonly string[] VatTexNames = {
        "Vanguard Frame Animation VAT_PositionTex",
        "Vanguard Frame Animation VAT_RotationTex",
        // 🆕 2026-10-19（V3）：死灵残骸体那 5 份 VAT 的贴图（每份 1 位置 + 1 旋转 = 10 张）。
        //   尺寸 150×15（位置，零件 75 × 2 列）/ 75×15（旋转）—— 与 Vanguard 一样是 **NPOT 线性数据贴图**，
        //   所以 `RestoreVatTextures` 那套导入项（npotScale=None / sRGB=false / 无 mip / 不压缩）**对它们同样适用**。
        "Card Remnant_Remnant Anim 1_PositionTex", "Card Remnant_Remnant Anim 1_RotationTex",
        "Card Remnant_Remnant Anim 3_PositionTex", "Card Remnant_Remnant Anim 3_RotationTex",
        "Card Remnant_Remnant Anim 5_PositionTex", "Card Remnant_Remnant Anim 5_RotationTex",
        "Card Remnant_Remnant Anim 8_PositionTex", "Card Remnant_Remnant Anim 8_RotationTex",
        "Card Remnant_Remnant Anim 9_PositionTex", "Card Remnant_Remnant Anim 9_RotationTex",
    };
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
    //       ② 撞名不同源会出声（`ResolveVariantPath` / `WarnCollision`），跑完顺手 grep 一下 `撞名且内容不同`；
    //          收尾还会打一行 **A845(b)「一源一份」** 的产物断言（`ReportVariantPaths`）。
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
        // 🔴 **2026-10-19 晚（T02）：EC 单开 `… Extra Color EC`** —— 与运行时表
        //    `WarpforgeShaderMap.Replacements` 同步（两张表必须一致）。理由见那个 shader 的文件头：
        //    原版 EC 多一跳 `LinearToSRGB(_Color)`，而内置族那一份用了 `_Color` 会坏。
        { "Everguild/FX/Extra Color",                       "WarpforgeVFX/Particles/Extra Color EC" },
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

        // ---- 🆕 2026-10-21（`A1325` 第三跳）：**TMP 那两跳的材质**（`CardPrefab` 的 9 种描边材质）----
        // 不写这一条也不坏：`ImportMaterial` 在表里查不到时会退到 `Shader.Find(src.shader.name)`
        // （`:2740`），而 `TextMeshPro/Distance Field` 在工程里**确实有**
        // （`Assets/TextMesh Pro/Shaders/TMP_SDF.shader:1`）⇒ 照旧 `approx = false`。
        // 显式写出来是为了让「TMP 族」在表里可见 —— 与下面那条近似档放在一起才看得出区别。
        { "TextMeshPro/Distance Field",                     "TextMeshPro/Distance Field" },
        // ⚠️ **近似**：原版 `CardPrefab` 里 `Pragati-Regular White_thick Offset for 3D`（4 条）用的就是它，
        //    而本地 TMP Essentials 的 13 张 shader 里**没有这个名字**（只有 `Distance Field`，没有 `Offset`）。
        //    判据与运行时表**同一条** → `WarpforgeVFX/Runtime/WarpforgeShaderMap.cs:338-343`
        //    （那里也写了「不映射的话运行时报找不到 shader ⇒ 那个材质槽保留占位 ⇒ 整块渲成洋红」）。
        //    ⛔ 别把它漏给下面的兜底：它不含 `Particles`/`FX/` ⇒ 会掉进 `Universal Render Pipeline/Unlit`，
        //    那是**文字**材质，套 Unlit 等于这一格画不出来。
        { "TextMeshPro/Distance Field Offset",              "TextMeshPro/Distance Field*" },
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
    //
    // 🔴 **2026-10-10（`A845(b)`）：上面只做了「出声」那一半，这里补上另一半 —— 一源一份。**
    //    原来：撞名且内容不同 ⇒ **只打告警**，两份源仍写同一个确定路径、后到的赢
    //    ⇒ **先到的那种在工程里没有对应文件**（静默画错）。
    //    现在：同一个 `stem` 下**每一种内容各得一份文件** ——
    //      · 内容等价 ⇒ 塌成一份（无害；实测 171 个名字 / 811 个文件正文等价）；
    //      · 内容不同 ⇒ **两份都带指纹**：先到的从裸名 `X.mat` **挪**到 `X__{哈希}.mat`
    //        （`AssetDatabase.MoveAsset` **保 guid** ⇒ 已经写好的引用不会断），后到的直接落指纹名。
    //    ⇒ 结果 = **一源一份**、且**与遍历顺序无关**（谁先到都得到同一组文件名）。
    //    ⛔ **必须标成「这是我们挑的」**（铁律 3）：**原版没有「材质资产」这个概念**
    //      （材质活在 bundle 里、各有自己的 pathID）⇒ 这一格**没有原版规格可照**；
    //      能照的只有方向 ——「一源一份」比「两个源抢一个文件」更接近原版。
    //      候选命名 `{stem}__{内容短哈希}` 出自 `资料/普查产出_1018/R-ASSET_资产族现核.md` §3.2。
    //    ⚠️ 指纹**不含 `源名=`**（那是身份、不是内容）；哈希 = 自己实现的 FNV-1a 32 位（8 位十六进制），
    //      ⛔ 不引新依赖、⛔ 不用 `GetHashCode()`（那个跨进程不稳定）。同一台机器上稳定即可。
    //    ⚠️ **上一次导出留下的指纹文件是最常见的情形、不是边角** ⇒ `ResolveVariantPath` 会**先查盘上有没有
    //      `{stem}__*` 兄弟**：有就直接落指纹名（否则「先占裸名 → 撞名 → 再挪走」会撞上上一次那份）。
    //    ⚠️ 判据**量在产物上**：收尾 `ReportVariantPaths()` 断言「多内容的名字里没有一份占裸名、
    //      路径互不相同、盘上都在」—— 删掉挪走那一句，第一条必红（灭自证）。
    static readonly Dictionary<string, List<(List<string> fields, string path)>> MatVariants
        = new Dictionary<string, List<(List<string>, string)>>();
    static readonly Dictionary<string, List<(List<string> fields, string path)>> MeshVariants
        = new Dictionary<string, List<(List<string>, string)>>();

    // 盘上「已经有 `{stem}__*` 指纹兄弟」的基名（每目录建一次）—— 见 `ResolveVariantPath` 的 ⚠️ 那条
    static readonly Dictionary<string, HashSet<string>> FingerprintedStems
        = new Dictionary<string, HashSet<string>>();

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
    /// 判据 = 原版 bundle 里那张 `Texture2D` 的 `mipmapCount`、`m_TextureSettings.{m_WrapU,m_WrapV,m_FilterMode,m_Aniso}`
    ///   与 `m_Width/m_Height`（🆕 2026-10-19 T07：wrap/filter/aniso 四格也照抄）。
    ///   🆕 2026-10-19 FX-2（账 V1）：**`nPOTScale` 也照原版 = `None(0)`**（判据 → FX-1 报告 §B：
    ///     原版 runtime 尺寸就是 NPOT ⇒ 原版导入器没做 POT 缩放；旁挂表 NPOT 给 `0`、POT 给 `-1` 不动）。
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
    /// <summary>旁挂表某列 → int；越界/解析不了返回 **-1（未知）**。
    /// 🆕 2026-10-19（T07）：`vfx_texture_mips.tsv` 加了 `wrapU/wrapV/filterMode/aniso` 四列；
    /// 老表没有这四列时返回 -1 ⇒ 调用方**不动**那一格。</summary>
    static int ParseOr(string[] c, int idx)
    {
        if (c == null || idx < 0 || idx >= c.Length) return -1;
        int v;
        return int.TryParse(c[idx], out v) ? v : -1;
    }

    public static void FixTextureImportSettings()
    {        const string tsv = @"d:/4/Unity/数据/游戏数据/vfx_texture_mips.tsv";
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
            // 🆕 2026-10-19（T07）：wrap/filter/aniso 三格也照原版（列 5..8）。
            //   -1 = 老表 / 读不到 ⇒ **不动**（不许拿「未知」覆盖）。
            // 🔴 **2026-10-19 晚（本批）：U/V 分开写。**
            //   原来写的是单一 `ti.wrapMode`（Unity 的 TextureImporter 的合并口径），
            //   而**原版确实有 U≠V 的贴图**（2984 张里 4 张：`Shine trail` / `Gauss Rays` /
            //   `Gauss Rays Noise` / `40k_voicelines_radio_wave equalizer`）。按 U 写会把 V 的 Clamp 丢掉：
            //   实测 `Shine trail` = 拖尾贴图，shader 把 V 放大 2.11 ⇒ V 出界 ⇒
            //   Clamp 应只露中段 ~26%，Repeat 把亮带卷到两端 ~57% ⇒ `Buff_DA_Forest_Self` 差 2.8×。
            //   Unity 6000.3 的 `TextureImporter` 有独立的 `wrapModeU/V/W`（本机 DLL 实读确认）。
            int wu = ParseOr(c, 5), wv = ParseOr(c, 6), fm = ParseOr(c, 7), an = ParseOr(c, 8);
            // 🆕 2026-10-19（本波 FX-2 · 账 V1）：第 10 列 `npotScale` —— 原版真值 = **None(0)**。
            //   判据：`TextureImporter` 是编辑器类、**不随 player build 打包**（全库 `grep nPOTScale` = 0 文件），
            //   但原版 **runtime `Texture2D` 的尺寸就是 NPOT**（84 包实读 2,984 张里 53 张真 NPOT；
            //   例 `Glow UI W40K` **123×123**、12 张 `trait_icon_*` **80×80**）—— 若原版是 `ToNearest`/`ToLarger`，
            //   runtime 会是 64/128。⇒ **原版 `nPOTScale = None`**。
            //   旁挂表：NPOT ⇒ `0`；POT ⇒ `-1`（POT 上该字段不生效、**不动**，沿用 `ParseOr` 的 -1 约定）。
            //   判据全文 → `资料/普查产出_1019/W57_特效线_T15_V1_T02静态.md` §B。
            int np = ParseOr(c, 9);
            var wantWrapU = (wu >= 0) ? (TextureWrapMode)wu : ti.wrapModeU;
            var wantWrapV = (wv >= 0) ? (TextureWrapMode)wv : ti.wrapModeV;
            var wantFilter = (fm >= 0) ? (FilterMode)fm : ti.filterMode;
            int wantAniso = (an > 0) ? an : ti.anisoLevel;
            bool same = ti.mipmapEnabled == wantMip && ti.maxTextureSize == wantMax
                        && ti.wrapModeU == wantWrapU && ti.wrapModeV == wantWrapV
                        && ti.filterMode == wantFilter
                        && ti.anisoLevel == wantAniso
                        && (np < 0 || (int)ti.npotScale == np);   // np=0 ⇒ None；-1 ⇒ 未知/不动
            if (same) { ok++; continue; }
            if (!wantMip) noMip++;
            // 只有一格读得到时**出声**（另一格保持现值，不猜）
            if (wu >= 0 && wv < 0)
                Debug.LogWarning($"[TEXFIX] `{c[0]}` 原版只有 wrapU({wu})、wrapV 读不到 ⇒ V 保持现值 {ti.wrapModeV}");
            if (wv >= 0 && wu < 0)
                Debug.LogWarning($"[TEXFIX] `{c[0]}` 原版只有 wrapV({wv})、wrapU 读不到 ⇒ U 保持现值 {ti.wrapModeU}");
            ti.mipmapEnabled = wantMip;
            ti.maxTextureSize = wantMax;
            ti.wrapModeU = wantWrapU;
            ti.wrapModeV = wantWrapV;
            ti.filterMode = wantFilter;
            ti.anisoLevel = wantAniso;
            if (np >= 0) ti.npotScale = (TextureImporterNPOTScale)np;   // 0 = None（原版真值）
            ti.SaveAndReimport();
            changed++;
            changedNames.Add($"{c[0]}({w}x{h},原版m{mips},wrap{wu}/{wv},f{fm},a{an},np{np})");
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
            RestoreVatTextures();      // 🆕 2026-10-09（`A1089` 的另一半）：补回**不是导出器生成**的那两张 VAT 贴图
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
        ReportVariantPaths();          // 🆕 A845(b)：撞名分文件**量产物**的收尾断言（判据见函数头部）
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

    /// <summary>🔴 **2026-10-09 新增（`A1089` 的另一半：把上次没覆盖到的那一半补上）** ——
    /// 清完产物之后，把 **VAT 那两张贴图补回 `Textures/`**。
    ///
    /// **为什么必须有这一步**：`ClearGenerated()`（`Resume=false` 时**无条件**跑）会**清空 `Textures/` 整个目录**，
    /// 而这两张贴图**不是导出器生成的** —— 原版里它们只被 `Vanguard Frame Animation VAT` 那个组件的字段引用，
    /// **不在任何「效果根」的依赖树里** ⇒ 全量重导一次就**永久丢掉**。丢了之后 `LoadVatTex` 只能**出声**、
    /// 把驱动的 `PositionsTex`/`RotationsTex` 写成 `null` ⇒ **`VanguardIdleEffect` 与出牌那一件都画不出来**。
    /// （2026-10-09 全量重导时实测踩到：两张 PNG 被删、prefab 里 `PositionsTex: {fileID: 0}`。）
    ///
    /// **做法**：真身常驻 <see cref="RuntimeVatDir"/>（不会被清，见那里的注释），
    /// 这里只做**拷贝 + 按原资产设导入项**。⚠️ 每次全量重导都会重新拷一遍 ⇒ 与源目录**永远一致**。
    ///
    /// 🔴 **导入项必须显式设、不能吃默认值**（原资产实测）：**28×41 / 14×41 是 NPOT**
    ///    （默认 `ToNearest` 会把它缩放 ⇒ 布局全毁）· `m_ColorSpace: 0` ⇒ **线性数据贴图** ·
    ///    无 mip · **RGBA32 不压缩**。
    ///    ⛔ **别改成走 `ImportTexture`**：它写死 `sRGBTexture = true`、mip 照原资产，一导就毁。
    /// </summary>
    static void RestoreVatTextures()
    {
        if (!AssetDatabase.IsValidFolder(RuntimeVatDir))
        {
            Debug.LogWarning(EX1 + $"A1089 VAT 贴图源目录 `{RuntimeVatDir}` 不在 ⇒ 驱动照样挂，"
                           + "但 `VanguardIdleEffect` 那一件**画不出来**（出声，不静默）");
            return;
        }
        int ok = 0;
        foreach (var nm in VatTexNames)
        {
            string srcRel = $"{RuntimeVatDir}/{nm}.png";
            string dstRel = $"{TexDir}/{nm}.png";
            string srcAbs = Path.Combine(Directory.GetCurrentDirectory(), srcRel);
            string dstAbs = Path.Combine(Directory.GetCurrentDirectory(), dstRel);
            if (!File.Exists(srcAbs))
            {
                Debug.LogWarning(EX1 + $"A1089 VAT 贴图源缺 `{srcRel}` —— 这一张补不回来（出声，不静默）");
                continue;
            }
            if (!File.Exists(dstAbs)) File.Copy(srcAbs, dstAbs, true);
            AssetDatabase.ImportAsset(dstRel, ImportAssetOptions.ForceUpdate);
            var ti = AssetImporter.GetAtPath(dstRel) as TextureImporter;
            if (ti == null)
            {
                Debug.LogWarning(EX1 + $"A1089 `{dstRel}` 不是 `TextureImporter` ⇒ 导入项没配上");
                continue;
            }
            ti.textureType = TextureImporterType.Default;
            ti.npotScale = TextureImporterNPOTScale.None;   // 🔴 NPOT：默认 ToNearest 会缩放、布局全毁
            ti.mipmapEnabled = false;
            ti.sRGBTexture = false;                          // 线性数据贴图（原资产 m_ColorSpace: 0）
            ti.alphaIsTransparency = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            ok++;
        }
        Debug.Log(EX1 + $"A1089 VAT 贴图补回 **{ok}** 张（源目录 `{RuntimeVatDir}`）");
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
        // 🆕 **2026-10-21（第六十会话）**：修了导出器两处缺口后要重导的那几件 ——
        //   · 3 条 OrkBuff：骷髅发射器 `UVModule.sprites` 全 guid 0（图集 `Skull Particle icons`）
        //     🔴 **2026-10-21 就地订正（铁律 5 · W61 现核）**：本行原来写「走新加的已知图集兜底」——
        //     **实际走的是 ③ `m_RenderDataMap`**（`export_listed_1021.log:5574/5596/6387/6409`），
        //     导出的 4 张 PNG 与原图集逐像素相同；`KnownUvModuleSprites` 那支只在 `GetSprite` 读成 null 时兜底。
        //     判据 → `资料/普查产出_1021/W61_特效_E60_OrkBuff.md` §2。
        //   · `Explosion Hive Fleet Arrival Tendrils OLD`：4 个 `SkinnedMeshRenderer.m_Mesh` 全 guid 0
        //     （新加 SMR 分支）。
        "OrkBuff_Recall",
        "OrkBuffGreenWaaagh",
        "OrkBuff_Snakebitez_2_Intense",
        // 🆕 **2026-10-21（W61 顺手发现）**：同族另外两件**同一个病**（骷髅 `UVModule.sprites`
        //   仍是 `{fileID: <原包 pathID>, guid: 0}` 的伪引用；prefab 停在 **10-10 16:08** 那一版）。
        //   判据 → `资料/普查产出_1021/W61_特效_E60_OrkBuff.md` §5：全工程这样的一共 **35 件 / 50 格**，
        //   这两件先跟本批一起重导（其余见报告，别在这份名单里一把梭）。
        "OrkBuff_KrumpDaGitz",
        "OrkBuff_Snakebitez_2",
        "Explosion Hive Fleet Arrival Tendrils OLD",
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
        // 🆕 **2026-10-16（`A2080`）**：暗黑天使 **`DA48`** 那张卡的 `ability` 档在
        //   `Resources/card_vfx_by_card.json` 里指的就是这一件（**生成器自己标了 `"inLibrary": false`**，
        //   `conf = authoritative`）⇒ `VfxMap.Resolve(Ability, DarkAngels, DA48)` 命中它、
        //   `WarpforgeEffectPlayer` 打「效果库里没有」（= 那一拍**没有画面**）。
        //   原版本地有这件（`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/` 里）；
        //   它被 `Run()` 的「效果根」过滤器挡掉的理由**与 `Environmental Condition Particles Orbital`（`A211`）同款**
        //   （子树里 0 个 `ParticleSystemRenderer`）—— 那条先例的注释见上面。
        //   判据 → `资料/普查产出_1016战斗/A2080_特效库缺名普查.md` §二。
        "Teleport Trigger Board Effect Only",
        // 🆕 **2026-10-21（`A1325` 收口）**：`CardPrefab.prefab` 也是要「按名字导」的一件 ——
        //   它的 `Image.m_Sprite` 6 条 / TMP `m_fontAsset` 38 条 guid 全 0，落码的那一跳
        //   （本文件 `A1325` 那两支）只有在**重新导出这一件**时才会跑；而它子树里没有
        //   `ParticleSystemRenderer` ⇒ `Run()` 的「效果根」过滤器永远筛不到它（与上面
        //   `Teleport Trigger Board Effect Only` 同款理由）。
        //   ⇒ 加进本名单、走 `RunListed()`（**不清产物、固定路径覆盖写 ⇒ guid 不变**）。
        //   ✅ **2026-10-21 当天补齐（`A1325` 第二半）**：那 5 条 uGUI `Image.m_Material` 现在**也接了**
        //   （`Export()` 里 `m_Material` 那一支，读序列化字段 + `ImportMaterial`），下面这条不再是缺口。
        //   ⚠️ 验它得给 `check_prefab_refs.py` 传 `--fields … ,m_Material`，默认字段集看不到那一列。
        "CardPrefab",
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
        ReportVariantPaths();          // 🆕 A845(b)：撞名分文件**量产物**的收尾断言（判据见函数头部）
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

    // ==================================================================
    //  🆕 2026-10-08（A1089）：把原版的 `StoryProgramming.VATGPUPlayer` **补挂回去**
    //
    //  判据 → `资料/普查产出_第六会话/W_VAT驱动.md` §④·1（那一轮落下了替身组件
    //    `WarpforgeVFX.WarpforgeVatDriver` 并手写进两个 prefab，但**没动本文件** —— 它当时在白名单之外）。
    //
    //  🔴 **不补会怎样（静默）**：原版那个组件的 `m_Script` 指向 `StoryProgramming.VATGPUPlayer`
    //    （pathID `3946243950355606049`）—— 我们工程里没有这个脚本 ⇒ 下一趟 `Export()` 的
    //    `StripMissingScripts(inst)`（`:1053`）把它**连数据整条删掉**、**不报错**；于是
    //    `_State` 永远停在手写值、那件 mesh 的顶点永远采贴图第 0 行 ⇒ **一点都不动**。
    //    与 A210 的 `ParticleSystemPoolable` **同一个根因、同一条改法**（见上面那段头注）。
    //
    //  ✅ **为什么把 12 个字段抄成表、而不是从包里读回来**：那份数据活在一个 `MonoBehaviour`
    //    （`StoryProgramming.VATAnimation`，`m_Script = -974199562677010219`）上，它的 `m_Script`
    //    在我们工程里**同样解析不了** ⇒ `LoadAsset` 拿不到类型化对象。
    //    ⇒ 本表就是**重导时的唯一来源**，值**逐字照抄原资产**（2026-10-08 亲读）：
    //      `d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/
    //       Vanguard Frame Animation VAT.json`（另有实例值 `.../MonoBehaviour_-6996853648599967635.json`）
    //      —— 与 `PoolableSpots` 同一形态（那张表同样是「从原版包里摘出来、写在本文件里」）。
    //    ⚠️ 全库这类 `VATAnimation` 共 **6 件**（Vanguard 1 + `Card Remnant_Remnant Anim {1,3,5,8,9}`）。
    //       🆕 **2026-10-19（V3）就地订正（铁律 5）**：原文写「另外那 5 件挂在一个我们还没有驱动的物件上 …
    //       表里现在只该有 Vanguard 那两条；Remnant 那条要先把『驱动指向 5 条里的哪一条 / 谁来换』查清才能加」——
    //       **那个问题已经查清，Remnant 那一行已补（本表 ③）**。判据（三条，都是实读）：
    //         ① 宿主 = `RemnantBody3D Necrons/Card Remnant`（`VATGPUPlayer` 实例的 `m_GameObject` 父链）；
    //         ② 5 件是**同一个宿主上的随机池**，不是 5 个宿主 —— `RemnantNecrons`（根上）的
    //            `deathAnimationList` = [1,3,5,8,9]，而 `VATGPUPlayer._vatAnimation` 序列化写死 = **8**；
    //         ③ 换法 = `RemnantNecrons__Toggle.c`：`Random.Range(0, list.Count)` → `vatPlayer._vatAnimation = list[i]`
    //            ⇒ 我们补了 `WarpforgeVatPoolPicker`（`Runtime/WarpforgeVatPoolPicker.cs`，`OnEnable` 挑一次）。
    //       10 张贴图已随本批进工程（`Runtime/Vat/` 真身 + `Textures/` 副本），`VatTexNames` 已登记。
    //
    //  ⚠️ **顺序**：与 `AttachPoolables` 一样 —— 必须**在 `StripMissingScripts` 之后**
    //    （否则刚挂上的又会被那一趟当 missing 删掉）、`PrefabUtility.SaveAsPrefabAsset`（`:1283`）之前。
    //  ⚠️ **两个 Vanguard 宿主已在 `ListedPrefabs` 里** ⇒ 不需要为它们再改名单。
    //    🆕 `RemnantBody3D Necrons` **不在** `ListedPrefabs`（它是**效果根**：16 个 ParticleSystem、
    //    已在 `WarpforgeEffectLibrary.asset` 里）⇒ 它由 `Run()` 全量导出覆盖；
    //    要**只**重导它时，临时把名字加进 `ListedPrefabs` 走 `RunListed()`（`Export()` 幂等，重复无害）。
    //  ⚠️ **两张 VAT 贴图不是被任何材质引用的** ⇒ 导出器**永远不会**自己导它们
    //    （它们是**手放进** `Textures/` 的，导入设置照原资产手设过：28×41 / 14×41 是 **NPOT** ⇒
    //     `nPOTScale: 0`、`sRGBTexture: 0`（线性数据贴图）、无 mip、RGBA32 不压缩）。
    //    ⇒ 这里**只按路径取、取不到就出声**，⛔ **不拿 `ImportTexture` 重导** ——
    //      `ImportTexture` 写死 `sRGBTexture = true`、mip 照原资产，一导就把那套手设的导入设置冲掉
    //      （2026-10-19 FX-2 起 `ImportTexture` 也无条件写 `nPOTScale = None`，**这一格不再冲突**；
    //       但 sRGB / mip / 不压缩那三格仍会被冲 ⇒「别用它重导 VAT」这条**照旧成立**）。
    //
    //  ⚠️ **本支【不管】的另一处**（同一趟重导也会抹掉、但**不是本题**）：`VanguardIdleEffect` 那条
    //    legacy `Animation.m_Animation` / `m_Animations[0]` 的 guid 现在是人手补的（`:59081/59083`），
    //    重导会**再变回 guid 全 0**。全库同类共 **45 个 `Animation` 组件 / 40 件 prefab**（2026-10-08 实测）
    //    ⇒ 那是**一件通用 A 项**（形状 = 照 `:1194` 那段 `Animator` 的处理补一支「legacy 片段」），
    //    在这里给单个 prefab 打补丁 = CLAUDE.md §三「两处写同一条规则」。已记进交件报告。
    // ==================================================================

    /// <summary>一条 VAT 补挂记录 = 一个宿主物件 + 它上面的 VAT 数据（**可能不止一份**）。</summary>
    sealed class VatSpot
    {
        /// <summary>宿主 prefab 名（= `Export()` 的 `src.name`；与 `PoolableSpots[0]` 同一个键）。</summary>
        public string Root;
        /// <summary>驱动要挂到的那个物件路径（从根起分段）—— **原版 `VATGPUPlayer` 就在那个 `MeshRenderer`
        /// 同一个 GameObject 上**（判据：`GameObject/Vanguard Frame Animated VAT.json` 的 4 个组件）。</summary>
        public string Path;
        /// <summary>原版这个宿主上的 VAT 数据。**`SerializedDefault` 那条 = 原版 prefab 里
        /// `VATGPUPlayer._vatAnimation` 写死的那一份**；多于一条时，运行时由
        /// `WarpforgeVatPoolPicker` 随机挑（判据 → `RemnantNecrons__Toggle.c`）。</summary>
        public VatDataSet[] Sets;
    }

    /// <summary>一份 VAT 数据。字段名 / 顺序 = `WarpforgeVFX.WarpforgeVatData`（照原资产序列化序）。</summary>
    sealed class VatDataSet
    {
        /// <summary>原版那个 `VATAnimation` 资产名 = 两张贴图的**文件名前缀**
        /// （`&lt;DataName&gt;_PositionTex.png` / `&lt;DataName&gt;_RotationTex.png`）。</summary>
        public string DataName;
        public Vector3 BoundsCenter, BoundsExtents, StartBoundsCenter, StartBoundsExtents;
        /// <summary>帧数（= 贴图**高**）。Vanguard 41 / Remnant 15。</summary>
        public float Frames;
        /// <summary>时长（秒）。Vanguard 1.35 / Remnant 3.0。</summary>
        public float Duration;
        /// <summary>部件数（= 旋转贴图宽 = 位置贴图宽的一半）。Vanguard 14 / Remnant 75。</summary>
        public int PartsCount;
        /// <summary>原版 `HighPrecisionPositionMode` —— 本地 6 件**全是 0**。</summary>
        public bool HighPrecisionPositionMode;
        /// <summary>原版 `PartsIdsInUV3` —— 本地 6 件**全是 0**（部件号在网格 `COLOR.w`）。</summary>
        public bool PartsIdsInUV3;
        /// <summary>原版该宿主 prefab 上**序列化写死**的那一份（`VATGPUPlayer._vatAnimation`）。
        /// 每行**必须恰有一条**（多于一条 / 一条都没有 ⇒ 出声）。</summary>
        public bool SerializedDefault;
    }

    /// <summary>VAT 驱动补挂表。**值逐字照抄原资产**，出处见上面那段头注。
    /// ⚠️ 加新行时先去 `assets_full/&lt;包&gt;/MonoBehaviour/&lt;名&gt;.json` 读一遍，⛔ 别手抄别猜。</summary>
    static readonly VatSpot[] VatSpots =
    {
        // ① 独立件：驱动在**根**上（原版那个 GameObject 就是根，4 个组件里第 4 个 = `VATGPUPlayer`）。
        new VatSpot
        {
            Root = "Vanguard Frame Animated VAT",
            Path = "Vanguard Frame Animated VAT",
            Sets = new[]
            {
                new VatDataSet
                {
                    DataName = "Vanguard Frame Animation VAT",
                    BoundsCenter      = new Vector3(-0.00015115737915039062f, 1.5765403509140015f, -0.10219991207122803f),
                    BoundsExtents     = new Vector3(1.3951337337493896f, 1.9750405550003052f, 0.9605523347854614f),
                    StartBoundsCenter = new Vector3(0f, 1.6361364126205444f, -0.3536794185638428f),
                    StartBoundsExtents= new Vector3(1.2230337858200073f, 1.6361366510391235f, 0.5935254693031311f),
                    Frames = 41f, PartsCount = 14, Duration = 1.35f,
                    HighPrecisionPositionMode = false, PartsIdsInUV3 = false,
                    SerializedDefault = true,
                },
            },
        },
        // ② 待机效果：同名**子物件**（**直接**子件 —— 实测父链 `Vanguard Frame Animated VAT` → `VanguardIdleEffect`）。
        //    🔴 出牌与待机是**两个入口** —— 只补一处 = 另一个入口静默失效（CLAUDE.md §十 第 5 条那种坑）。
        new VatSpot
        {
            Root = "VanguardIdleEffect",
            Path = "VanguardIdleEffect/Vanguard Frame Animated VAT",
            Sets = new[]
            {
                new VatDataSet
                {
                    DataName = "Vanguard Frame Animation VAT",
                    BoundsCenter      = new Vector3(-0.00015115737915039062f, 1.5765403509140015f, -0.10219991207122803f),
                    BoundsExtents     = new Vector3(1.3951337337493896f, 1.9750405550003052f, 0.9605523347854614f),
                    StartBoundsCenter = new Vector3(0f, 1.6361364126205444f, -0.3536794185638428f),
                    StartBoundsExtents= new Vector3(1.2230337858200073f, 1.6361366510391235f, 0.5935254693031311f),
                    Frames = 41f, PartsCount = 14, Duration = 1.35f,
                    HighPrecisionPositionMode = false, PartsIdsInUV3 = false,
                    SerializedDefault = true,
                },
            },
        },
        // ③ 🆕 2026-10-19（V3）：死灵残骸体 —— **5 份数据挂在同一个物件上**，不是 5 个宿主。
        //    判据（逐条实读，不是推断）：
        //      · 宿主：`MonoBehaviour_7929411139690315314.json`（`VATGPUPlayer`）的 `m_GameObject`
        //        = `RemnantBody3D Necrons/Card Remnant`（UnityPy 解父链现读），其 `_vatAnimation` = `… Anim 8`。
        //      · 池：`MonoBehaviour_9217091644567076402.json`（`RemnantNecrons`，在**根**上）的
        //        `deathAnimationList` = [`Anim 1`, `Anim 3`, `Anim 5`, `Anim 8`, `Anim 9`]（顺序照原资产）。
        //      · 换法：`d:/2/tools/decomp_full/RemnantNecrons__Toggle.c` —— 转成残骸那一支里
        //        `i = Random.Range(0, deathAnimationList.Count); vatPlayer._vatAnimation = list[i];`
        //        ⇒ 运行时由 `WarpforgeVatPoolPicker` 复现（本表只提供数据）。
        //      · 数值：五份各读自 `assets_full/bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/
        //        Card Remnant_Remnant Anim {1,3,5,8,9}.json`（2026-10-19 亲读；`Frames` 15 / `PartsCount` 75 / `Duration` 3.0）。
        //    ⚠️ 两张贴图（每份 1 位置 + 1 旋转，共 10 张）**不是任何材质的依赖** ⇒ 导出器不会自己导；
        //       真身已放 `Runtime/Vat/`，`VatTexNames` 已登记，`RestoreVatTextures()` 会按原设置补进 `Textures/`。
        new VatSpot
        {
            Root = "RemnantBody3D Necrons",
            Path = "RemnantBody3D Necrons/Card Remnant",
            Sets = new[]
            {
                new VatDataSet
                {
                    DataName = "Card Remnant_Remnant Anim 1",
                    BoundsCenter      = new Vector3(-0.21308410167694092f, 1.4203670024871826f, -0.15055632591247559f),
                    BoundsExtents     = new Vector3(1.6701942682266235f, 1.5525238513946533f, 1.4408702850341797f),
                    StartBoundsCenter = new Vector3(-0.003141164779663086f, 1.4864453077316284f, 0f),
                    StartBoundsExtents= new Vector3(1.0450413227081299f, 1.4864453077316284f, 0.9423092603683472f),
                    Frames = 15f, PartsCount = 75, Duration = 3f,
                    HighPrecisionPositionMode = false, PartsIdsInUV3 = false,
                },
                new VatDataSet
                {
                    DataName = "Card Remnant_Remnant Anim 3",
                    BoundsCenter      = new Vector3(-0.1979382038116455f, 1.3912087678909302f, -0.4062849283218384f),
                    BoundsExtents     = new Vector3(1.6773707866668701f, 1.581681489944458f, 1.4272024631500244f),
                    StartBoundsCenter = new Vector3(-0.003141164779663086f, 1.4864453077316284f, 0f),
                    StartBoundsExtents= new Vector3(1.0450413227081299f, 1.4864453077316284f, 0.9423092603683472f),
                    Frames = 15f, PartsCount = 75, Duration = 3f,
                    HighPrecisionPositionMode = false, PartsIdsInUV3 = false,
                },
                new VatDataSet
                {
                    DataName = "Card Remnant_Remnant Anim 5",
                    BoundsCenter      = new Vector3(-0.10512745380401611f, 1.4044116735458374f, -0.18900752067565918f),
                    BoundsExtents     = new Vector3(1.74807608127594f, 1.568479061126709f, 1.2947776317596436f),
                    StartBoundsCenter = new Vector3(-0.003141164779663086f, 1.4864453077316284f, 0f),
                    StartBoundsExtents= new Vector3(1.0450413227081299f, 1.4864453077316284f, 0.9423092603683472f),
                    Frames = 15f, PartsCount = 75, Duration = 3f,
                    HighPrecisionPositionMode = false, PartsIdsInUV3 = false,
                },
                new VatDataSet
                {
                    // 原版 prefab 里序列化写死的那一份（`VATGPUPlayer._vatAnimation`）。
                    DataName = "Card Remnant_Remnant Anim 8",
                    BoundsCenter      = new Vector3(-0.13422656059265137f, 1.393722414970398f, -0.051880598068237305f),
                    BoundsExtents     = new Vector3(1.5435593128204346f, 1.5791678428649902f, 1.2677791118621826f),
                    StartBoundsCenter = new Vector3(-0.003141164779663086f, 1.4864453077316284f, 0f),
                    StartBoundsExtents= new Vector3(1.0450413227081299f, 1.4864453077316284f, 0.9423092603683472f),
                    Frames = 15f, PartsCount = 75, Duration = 3f,
                    HighPrecisionPositionMode = false, PartsIdsInUV3 = false,
                    SerializedDefault = true,
                },
                new VatDataSet
                {
                    DataName = "Card Remnant_Remnant Anim 9",
                    BoundsCenter      = new Vector3(-0.02241051197052002f, 1.3983813524246216f, -0.3329894542694092f),
                    BoundsExtents     = new Vector3(1.511764645576477f, 1.574509620666504f, 1.505840539932251f),
                    StartBoundsCenter = new Vector3(-0.003141164779663086f, 1.4864453077316284f, 0f),
                    StartBoundsExtents= new Vector3(1.0450413227081299f, 1.4864453077316284f, 0.9423092603683472f),
                    Frames = 15f, PartsCount = 75, Duration = 3f,
                    HighPrecisionPositionMode = false, PartsIdsInUV3 = false,
                },
            },
        },
    };

    /// <summary>把 VAT 驱动补挂回去。**照 `AttachPoolables` 的同一套写法**：
    /// 表 → `FindChildByPath` 逐级下沉（归一化名字比较）→ `AddComponent` → 填字段 → **没对上就出声**。
    /// 与它并列、互不影响（⛔ 不改它一行）。</summary>
    static void AttachVatDrivers(GameObject inst, string rootName)
    {
        if (inst == null) return;
        int n = 0, miss = 0;
        foreach (var e in VatSpots)
        {
            if (e.Root != rootName) continue;
            var tr = FindChildByPath(inst.transform, e.Path);
            if (tr == null)
            {
                miss++;
                Debug.LogWarning(EX1 + $"A1089 VAT 表里的 `{e.Path}` 在 `{rootName}` 里找不到 —— 这一条没挂上（出声，不静默）");
                continue;
            }
            if (AttachOneVat(tr.gameObject, e) == null) { miss++; continue; }
            n++;
        }
        if (n > 0 || miss > 0)
            Debug.Log(EX1 + $"A1089 `WarpforgeVatDriver`：`{rootName}` 挂上 **{n}** 个"
                          + (miss > 0 ? $"、**没对上 {miss} 个**（见上面的告警）" : ""));
    }

    /// <summary>挂一个驱动并把原版那份数据**逐字**填进去（含两张贴图的引用）。
    /// 幂等：物件上已经有了就不再 `AddComponent`（`WarpforgeVatDriver` 带 `[DisallowMultipleComponent]`，
    /// 重复 `AddComponent` 在编辑器里会**报错并返回 null**）；数据**每次新建一份** ——
    /// 表是静态的、两个宿主共用同一行数值，⛔ 不能把同一个实例挂到两处（会互相串）。</summary>
    static WarpforgeVatDriver AttachOneVat(GameObject go, VatSpot e)
    {
        if (e == null || e.Sets == null || e.Sets.Length == 0)
        {
            Debug.LogWarning(EX1 + $"A1089 VAT 表里 `{(e == null ? "?" : e.Root)}` 那一行没有数据（`Sets` 空）");
            return null;
        }
        var drv = go.GetComponent<WarpforgeVatDriver>();
        if (drv == null) drv = go.AddComponent<WarpforgeVatDriver>();
        if (drv == null) return null;

        var datas = new WarpforgeVatData[e.Sets.Length];
        int defIdx = -1, defCount = 0;
        for (int i = 0; i < e.Sets.Length; i++)
        {
            var s = e.Sets[i];
            datas[i] = new WarpforgeVatData
            {
                BoundsCenter            = s.BoundsCenter,
                BoundsExtents           = s.BoundsExtents,
                StartBoundsCenter       = s.StartBoundsCenter,
                StartBoundsExtents      = s.StartBoundsExtents,
                Frames                  = s.Frames,
                PartsCount              = s.PartsCount,
                Duration                = s.Duration,
                HighPrecisionPositionMode = s.HighPrecisionPositionMode,
                PartsIdsInUV3           = s.PartsIdsInUV3,
                PositionsTex            = LoadVatTex(s.DataName, "_PositionTex"),
                RotationsTex            = LoadVatTex(s.DataName, "_RotationTex"),
                // 原资产这两项本来就是空：`PositionsTexB` = `m_PathID: 0`、`HighPrecisionPositionMode` = 0。
                PositionsTexB           = null,
            };
            if (s.SerializedDefault) { defIdx = i; defCount++; }
        }
        // 每行**必须恰有一条** —— 多了/少了都是表写错，出声（⛔ 别默认取第一条蒙混过去）
        if (defCount != 1)
            Debug.LogWarning(EX1 + $"A1089 VAT 表 `{e.Root}` 的 `SerializedDefault` 有 **{defCount}** 条"
                                  + "（每行必须恰有 1 条）⇒ 暂按 "
                                  + (defIdx >= 0 ? "第一条命中的" : "`Sets[0]`") + " 填 `_vatAnimation`");
        // 原版实例的序列化值（`MonoBehaviour_-6996853648599967635.json`：`_state: 0.0`、`_animationSpeed: 1.0`）
        drv._vatAnimation = datas[defIdx >= 0 ? defIdx : 0];
        drv._state = 0f;
        drv._animationSpeed = 1f;
        // `_stateCurveKeys`（0.07 → 1.0 / 1.7916666 s）与 `_useBuiltInCurveWhenNoClip` 走字段初始化器，
        // `AddComponent` 时会跑 ⇒ 不用在这里再写一遍（⛔ 写第二份 = 两处规则迟早不一致）。

        // 多份数据 = 原版那个「随机池」（`RemnantNecrons.Toggle`：`Random.Range(0, list.Count)` 后写 `_vatAnimation`）
        if (datas.Length > 1)
        {
            var picker = go.GetComponent<WarpforgeVatPoolPicker>();
            if (picker == null) picker = go.AddComponent<WarpforgeVatPoolPicker>();
            if (picker == null)
            {
                Debug.LogWarning(EX1 + $"A1089 `{e.Root}`：`WarpforgeVatPoolPicker` 没挂上 ⇒ "
                                      + $"{datas.Length} 份 VAT 只用序列化默认那一份（不会随机）");
            }
            else
            {
                picker.driver = drv;
                picker.animations = datas;
                picker.pickOnEnable = true;   // 原版在「变成残骸那一拍」挑；我们的实例化即那一拍
            }
        }
        return drv;
    }

    //  ═══ 2026-10-19（V3）：`ParticleForceForceFieldController` 补挂 ═══
    //
    //  原版类（`ParticleForceForceFieldController.cs` 签名桩 + `__Update.c` 方法体）：
    //      [SerializeField] ParticleSystemForceField forceField;
    //      [SerializeField] float force;
    //      Update(): gravity = MinMaxCurve(force);
    //  ⇒ 等价物 = `WarpforgeVFX.WarpforgeParticleForceFieldController`。
    //
    //  宿主判据（逐条实读）：
    //    · `Will Of Gork.prefab` 根上的 legacy `Animation`（`m_PlayAutomatically: 1`）播 `Will of Grok`；
    //      该 clip 里 `attribute: force`、`path: ""`（classID 114）⇒ 绑到**根**上的组件。
    //    · 原版那个组件的 GameObject 父链 = `Will Of Gork`（根）；`forceField` 指向 `Force Field`
    //      子物件上的 `ParticleSystemForceField`（原版 `m_PathID: 577730043536102203`）。
    //    · `force` 的序列化值 = **0.0**（`MonoBehaviour_1249487700440863547.json`）。
    //  ⚠️ 与 `AttachVatDrivers` 一样必须**在 `StripMissingScripts` 之后**（否则刚挂上的又会被删）。

    sealed class ForceFieldSpot
    {
        /// <summary>宿主 prefab 名（= `Export()` 的 `src.name`）。</summary>
        public string Root;
        /// <summary>控制器挂哪个物件（原版在**根**上 —— 曲线 `path` 为空）。</summary>
        public string Path;
        /// <summary>`forceField` 指向的物件（原版 `Force Field`）。</summary>
        public string FieldPath;
    }

    static readonly ForceFieldSpot[] ForceFieldSpots =
    {
        new ForceFieldSpot { Root = "Will Of Gork", Path = "Will Of Gork", FieldPath = "Force Field" },
    };

    /// <summary>把 `ParticleForceForceFieldController` 补挂回去（形状照 `AttachVatDrivers`）。</summary>
    static void AttachForceFieldControllers(GameObject inst, string rootName)
    {
        if (inst == null) return;
        int n = 0, miss = 0;
        foreach (var e in ForceFieldSpots)
        {
            if (e.Root != rootName) continue;
            var tr = FindChildByPath(inst.transform, e.Path);
            if (tr == null)
            {
                miss++;
                Debug.LogWarning(EX1 + $"A1089 力场控制器：`{e.Root}` 上找不到 `{e.Path}` ⇒ `force` 曲线没有接收者");
                continue;
            }
            var ctl = tr.GetComponent<WarpforgeParticleForceFieldController>();
            if (ctl == null) ctl = tr.gameObject.AddComponent<WarpforgeParticleForceFieldController>();
            if (ctl == null)
            {
                miss++;
                Debug.LogWarning(EX1 + $"A1089 力场控制器：`{e.Path}` 上挂不上 `WarpforgeParticleForceFieldController`");
                continue;
            }
            var ft = FindChildByPath(inst.transform, e.FieldPath);
            ctl.forceField = ft == null ? null : ft.GetComponent<ParticleSystemForceField>();
            if (ctl.forceField == null)
            {
                miss++;
                Debug.LogWarning(EX1 + $"A1089 力场控制器：`{e.Root}` 上找不到 `{e.FieldPath}` 的 "
                                      + "`ParticleSystemForceField` ⇒ 控制器挂上了、但每一帧只会出声");
                continue;
            }
            ctl.force = 0f;   // 原版序列化值（`MonoBehaviour_1249487700440863547.json`）
            n++;
        }
        if (n > 0 || miss > 0)
            Debug.Log(EX1 + $"A1089 `WarpforgeParticleForceFieldController`：`{rootName}` 挂上 **{n}** 个"
                          + (miss > 0 ? $"、**没对上 {miss} 个**（见上面的告警）" : ""));
    }

    /// <summary>取一张 VAT 贴图（工程资产）。**取不到就出声**，⛔ 静默返回 null 不许。</summary>
    static Texture2D LoadVatTex(string dataName, string suffix)
    {
        string rel = $"{TexDir}/{dataName}{suffix}.png";
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(rel);
        if (t == null)
            Debug.LogWarning(EX1 + $"A1089 VAT 贴图 `{rel}` 不在工程里 ⇒ 驱动挂上了、但这一件**画不出来**"
                           + "（贴图引用是 null）。"
                           + "🆕 **2026-10-09 起这条不该再出现** —— `Run` 在 `ClearGenerated()` 之后会调 "
                           + "`RestoreVatTextures()` 自动补回（真身常驻 `" + RuntimeVatDir + "`）。"
                           + "真出现了 ⇒ 先看 `RestoreVatTextures` 那两条出声里的哪一条。"
                           + "（判据：28×41 / 14×41 是 **NPOT** ⇒ `npotsScale: None`、`sRGBTexture: 0` 线性、无 mip、RGBA32 不压缩；"
                           + "⛔ 别走 `ImportTexture`：它写死 `sRGBTexture = true`，一导就把布局毁了。）");
        return t;
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    //  🆕 2026-10-10：**`m_Script` 那一跳**（`A1298` 持久修法 · `A1299` 守卫的洞）——
    //  **两个导出器共用这一份**（`EffectExporter` 与 `BoosterPackExporter` 的 prefab 保存路各一行调用）。
    //
    //  病灶：`PrefabUtility.SaveAsPrefabAsset` 对「`m_Script` 指着**包里的** `MonoScript`（不是工程资产）」
    //    的组件只能写成 `{fileID: <原版 pathID>, guid: 000…0, type: 0}` ⇒ 重开 prefab 那个组件是
    //    **Missing (Mono Script)**；构建 player 时报 `Script attached to '<节点>' … is missing`
    //    （实测那一趟 **140 条**，含 `CardPrefab.prefab` 的 `NameTextTacticBig` / `DescTextUnit` /
    //      `Armour Text` / `Melee Attack Text` / `HealText` / `DescTacticSmall` —— 那些 TMP 在真包里是**死的**）。
    //  受害面（2026-10-10 现读**产物**，不是内存）= `Assets/WarpforgeBooster/Prefabs/Booster Info Popup.prefab`
    //    **24** 条 ＋ `Assets/WarpforgeVFX/Prefabs/` 963 件里 **3 件共 51 条**（`CardPrefab` 49/50 ·
    //    `HuntMark_IdleEffect` 1 · `MarkerlightIdle` 1）。三种类（pathID 出处 =
    //    `assets_full/bundle_Waprforge_monoscripts/MonoScript/MonoScript_<pid>.json` 的 `m_ClassName`，
    //    2026-10-10 现读核过三种）= `Image` 11 · `TextMeshProUGUI` 24 · `TextMeshPro` 16。
    //
    //  🔴 **为什么必须是「两腿」**：内存里的判据**会骗人** —— 实测被引用的那 7 个 TMP 组件在内存里
    //    是真 `TMP_Text`（`GetComponentsInChildren<TMP_Text>` 找得到、`m_fontAsset` 也读得出），
    //    可落盘那一条还是 guid 全 0（`A1299` 那笔账：`BoosterPackExporter` 的守卫报「脚本未解析 0」，
    //    而同一份 prefab 盘上就有 24 条 guid0）⇒ **唯一靠得住的判据是「把产物读回来数」。**
    //  腿①  落盘前（根因修、通用）：`m_Script` 不是工程资产的组件，按它**运行期解析出来的类**
    //       （`c.GetType()`）去工程里找 `MonoScript`（判据 = `MonoScript.GetClass() == c.GetType()`；
    //       同名文件不止一份时靠它挑准 —— 实测 `Image.cs` 在 `com.unity.ide.visualstudio` 里也有一份）
    //       ⇒ 写回 `m_Script`。**不认 pathID、不查类名表** ⇒ 上面三种（以及将来任何一族）同一条路。
    //  腿②  落盘后（兜底 + 出声）：按**字节**读回刚写出的 prefab，数 `m_Script` 的 guid0 条数；
    //       >0 就按 `KnownScriptPids` 改回来，并把**不在表里**的 pid 点名报出来（⛔ 不猜）。
    //  ⛔ **必须标成「这是我们挑的」**（铁律 3）：原版没有「工程 prefab」这一格 ⇒ 无原版规格可照；
    //    能照的只有方向 ——「让原版引用的那个脚本类仍然被引用」。
    //  ⚠️ **顺序**：腿①必须在 `StripMissingScripts` **之后**（那时才会把「脚本完全找不到」的组件删掉）、
    //    `PrefabUtility.SaveAsPrefabAsset` **之前** —— 本函数就是那个位置。
    // ==========================================================================================
    const string ZeroGuid = "00000000000000000000000000000000";

    /// <summary>把 `Assets/…` 这种 **AssetDatabase 路径**换成**真实文件路径**（`File.*` 系列要的是后者）。
    /// ⚠️ 本文件里既有代码是直接拿 `Assets/…` 去 `File.*` 的（`SaveReport` 的 `ReportPath`）——
    /// 那**赌的是进程 CWD = 工程根**；新代码不赌（批处理怎么起的由不得我们）。</summary>
    static string AbsFile(string assetPath)
        => assetPath.StartsWith("Assets/", StringComparison.Ordinal)
           ? Path.Combine(Application.dataPath,
                          assetPath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar))
           : assetPath;

    /// <summary>腿② 认的三种（判据 = 包内 `MonoScript.m_ClassName`，出处见上面那段）。
    /// ⚠️ **不在表里的 pid 照样出声**（⛔ 不猜、也不静默）—— 要补就照同样的出处核一遍再加一行。</summary>
    static readonly (long pid, string cls)[] KnownScriptPids =
    {
        (350208831926335389L,  "UnityEngine.UI.Image"),
        (7477354737935883349L, "TMPro.TextMeshProUGUI"),
        (6627610752530350053L, "TMPro.TextMeshPro"),
    };

    /// <summary>「落盘 ＋ 修 ＋ 按产物核对」的**唯一入口** —— 两个导出器的 prefab 保存路都走它（一处口径）。
    /// 返回：(内存里接回几条, 落盘后 guid0 几条, 按表改回几条, 改完后还剩几条)。</summary>
    public static (int fixedInMemory, int zeroOnDisk, int repaired, int left)
        SavePrefabAndRepairScripts(GameObject inst, string path)
    {
        var unfixable = new List<string>();
        int fixedInMemory = FixUnresolvedScripts(inst, unfixable);
        PrefabUtility.SaveAsPrefabAsset(inst, path);
        var nm = Path.GetFileNameWithoutExtension(path);
        if (fixedInMemory > 0)
            Debug.Log($"[EffectExporter] A1298 `{nm}`：落盘前把 **{fixedInMemory}** 条指不到工程脚本的 `m_Script` "
                    + "接成了工程脚本（判据 = `MonoScript.GetClass() == 组件运行期类型`）");
        if (unfixable.Count > 0)
            Debug.LogWarning($"[EffectExporter] A1299 `{nm}` 有 {unfixable.Count} 个组件的脚本**连运行期类型都没解析出来**"
                           + $" ⇒ 这一格接不回来（如实记、⛔ 不猜一个类顶上去）：{string.Join(" / ", unfixable)}");

        var sample = new List<string>();
        int zeroOnDisk = CountZeroGuidScripts(path, sample);
        int repaired = 0;
        int left = zeroOnDisk;                      // ⚠️ 只有真出事时才再读一遍（963 件 prefab × 每件上百 KB）
        var leftSample = new List<string>();
        if (zeroOnDisk > 0)
        {
            var unknown = new List<string>();
            repaired = RepairZeroGuidScripts(path, unknown);
            if (unknown.Count > 0)
                Debug.LogWarning($"[EffectExporter] A1298 `{nm}` 有**不在表里**的 guid0 `m_Script` ⇒ 那几条改不回来："
                               + string.Join(" / ", unknown));
            left = CountZeroGuidScripts(path, leftSample);
            Debug.LogWarning($"[EffectExporter] A1299 `{nm}`：**落盘后现读产物**有 **{zeroOnDisk}** 条 `m_Script` 是 guid0"
                           + "（内存里的守卫报 0 也没用 —— 这一条量的是产物）；按表改回 **{repaired}** 条，"
                           + $"改完还剩 **{left}** 条。" + $"前几条：{string.Join(" / ", sample)}"
                           + (left > 0 ? $" ｜ 剩下的：{string.Join(" / ", leftSample)}" : ""));
        }
        return (fixedInMemory, zeroOnDisk, repaired, left);
    }

    /// <summary>腿①：把「`m_Script` 指不到工程脚本」的组件接成**它运行期那个类的**工程脚本。
    /// 返回接回几条；`unfixable` 收「连运行期类型都没解析出来 / 工程里没有这个类 / 写不进去」的那些（点名）。
    /// ⚠️ 判据用的是 `c is MonoBehaviour` ＋ `c.GetType()`：**不读 `m_Script` 的当前值**
    ///   （那一格读出来可能是 null —— 正是 `A1299` 那个洞），所以这条路**不受它影响**。</summary>
    public static int FixUnresolvedScripts(GameObject root, List<string> unfixable)
    {
        int n = 0;
        if (root == null) return 0;
        foreach (var c in root.GetComponentsInChildren<Component>(true))
        {
            if (c == null) continue;
            if (!(c is MonoBehaviour)) continue;      // 非脚本组件（Transform/Renderer/ParticleSystem…）没有 `m_Script`
            var cur = ScriptFieldOf(c);
            if (cur != null && AssetDatabase.Contains(cur)) continue;   // 已经指着工程脚本 ⇒ 落得下来
            var t = c.GetType();
            // 先试**最便宜、最精确**的那条：组件自己的 `MonoScript`（`A1117` 那轮手工改产物时用的就是它）。
            // 拿到的若不是工程资产（包里的 `MonoScript`）或拿不到，再按**运行期类型**找（见 `ProjectScriptFor`）。
            MonoScript ms = null;
            try { ms = MonoScript.FromMonoBehaviour(c as MonoBehaviour); } catch { ms = null; }
            if (ms == null || !AssetDatabase.Contains(ms))
                ms = t != typeof(MonoBehaviour) ? ProjectScriptFor(t) : null;
            if (ms == null)
            {
                if (unfixable.Count < 8)
                    unfixable.Add(t == typeof(MonoBehaviour)
                        ? $"{c.gameObject.name}(连类型都没解析出来)"
                        : $"{c.gameObject.name}({t.Name}：工程里找不到这个类的 MonoScript)");
                continue;
            }
            if (SetScriptFieldOf(c, ms)) n++;
            else if (unfixable.Count < 8) unfixable.Add($"{c.gameObject.name}({t.Name}：写 `m_Script` 失败)");
        }
        return n;
    }

    static UnityEngine.Object ScriptFieldOf(Component c)
    {
        try
        {
            using (var so = new SerializedObject(c))
            {
                var p = so.FindProperty("m_Script");
                if (p == null || p.propertyType != SerializedPropertyType.ObjectReference) return null;
                return p.objectReferenceValue;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[EffectExporter] 读 `{c.GetType().Name}.m_Script` 失败（按「没接」处理）："
                           + $"{e.GetType().Name}: {e.Message}");
            return null;
        }
    }

    /// <summary>写回 `m_Script`，**写完当场回读核对**（`ApplyModifiedPropertiesWithoutUndo` 返回 true 也可能没落值）。</summary>
    static bool SetScriptFieldOf(Component c, MonoScript ms)
    {
        try
        {
            using (var so = new SerializedObject(c))
            {
                var p = so.FindProperty("m_Script");
                if (p == null || p.propertyType != SerializedPropertyType.ObjectReference) return false;
                p.objectReferenceValue = ms;
                if (!so.ApplyModifiedPropertiesWithoutUndo()) return false;
            }
            return ReferenceEquals(ScriptFieldOf(c), ms);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[EffectExporter] 写 `{c.GetType().Name}.m_Script` = `{ms.name}` 失败："
                           + $"{e.GetType().Name}: {e.Message}");
            return false;
        }
    }

    static readonly Dictionary<Type, MonoScript> ProjectScripts = new Dictionary<Type, MonoScript>();

    /// <summary>工程里**那个类**的 `MonoScript`（判据 = `GetClass() == t`，⛔ 不按文件名猜）。
    /// ⚠️ 同名脚本可能不止一份（实测 `Image.cs` 在 `com.unity.ide.visualstudio` 里也有一份）⇒ 靠 `GetClass()` 挑。
    /// ⛔ 找不到返回 null（调用方点名出声），**绝不新建**。</summary>
    static MonoScript ProjectScriptFor(Type t)
    {
        MonoScript hit;
        if (ProjectScripts.TryGetValue(t, out hit)) return hit;
        hit = null;
        try
        {
            hit = FindProjectScript(AssetDatabase.FindAssets(t.Name + " t:MonoScript"), t);
            if (hit == null)
            {
                // ⚠️ 兜底：名字过滤没命中就全库扫一遍（**很慢**、只在这一格用一次；正常一次都不会走到）
                Debug.LogWarning($"[EffectExporter] 名字过滤 `{t.Name}` 没命中 ⇒ 全库扫 `t:MonoScript` 找 `{t.FullName}`（慢）");
                hit = FindProjectScript(AssetDatabase.FindAssets("t:MonoScript"), t);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[EffectExporter] 找类 `{t.FullName}` 的工程 MonoScript 失败（按「没有」处理）："
                           + $"{e.GetType().Name}: {e.Message}");
        }
        ProjectScripts[t] = hit;
        return hit;
    }

    /// <summary>在候选 `MonoScript` 资产里挑 `GetClass() == t` 的那一份（⛔ 不按文件名猜）。</summary>
    static MonoScript FindProjectScript(string[] guids, Type t)
    {
        foreach (var g in guids)
        {
            var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(g));
            if (ms != null && ms.GetClass() == t) return ms;
        }
        return null;
    }

    /// <summary>腿②的对照表：`"UnityEngine.UI.Image"` → `System.Type`（在本进程已加载的程序集里找）。</summary>
    static readonly Dictionary<string, Type> TypeByName = new Dictionary<string, Type>();

    static Type TypeOf(string fullName)
    {
        Type t;
        if (TypeByName.TryGetValue(fullName, out t)) return t;
        t = null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try { t = asm.GetType(fullName, false); } catch { t = null; }
            if (t != null) break;
        }
        TypeByName[fullName] = t;
        if (t == null) Debug.LogWarning($"[EffectExporter] 本进程的程序集里没有 `{fullName}` ⇒ 这一类改不回来");
        return t;
    }

    /// <summary>腿②：把刚写出的 prefab 里**已知三种** pid 的 guid0 `m_Script` 改成工程脚本。
    /// **字节级替换**（pattern 与替换串都是纯 ASCII ⇒ 不碰编码/BOM/行尾）—— 见 `ReplaceAsciiBytes`。
    /// `unknown` 收「表里有、但工程里找不到这个类」的（点名）。</summary>
    static int RepairZeroGuidScripts(string path, List<string> unknown)
    {
        int done = 0;
        foreach (var k in KnownScriptPids)
        {
            var cls = TypeOf(k.cls);
            var ms = cls != null ? ProjectScriptFor(cls) : null;
            if (ms == null) { unknown.Add($"{k.cls}（工程里找不到这个类的 MonoScript）"); continue; }
            var ap = AssetDatabase.GetAssetPath(ms);
            string guid; long lid;
            if (string.IsNullOrEmpty(ap) || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ms, out guid, out lid))
            { unknown.Add($"{k.cls}（取不到路径 `{ap}` 的 guid/localFileID）"); continue; }
            var oldS = $"m_Script: {{fileID: {k.pid}, guid: {ZeroGuid}, type: 0}}";
            var newS = $"m_Script: {{fileID: {lid}, guid: {guid}, type: 3}}";
            int c = ReplaceAsciiBytes(path, oldS, newS);
            if (c > 0) { done += c; Debug.Log($"[EffectExporter] A1298 落盘后改回 **{c}** 条 `m_Script` = `{k.cls}`（guid {guid}）"); }
        }
        if (done > 0) AssetDatabase.ImportAsset(path);   // 让 AssetDatabase 认这几次改（否则它手里还是旧文本）
        return done;
    }

    /// <summary>在文件里做**字节级**的 ASCII 串替换（pattern / 替换串都必须纯 ASCII）。
    /// 为什么不按文本读写：编码/BOM/行尾一律不动 —— 源码里踩过「文本模式写回翻行尾」好几次。</summary>
    static int ReplaceAsciiBytes(string path, string oldAscii, string newAscii)
    {
        var b = File.ReadAllBytes(AbsFile(path));
        var ob = System.Text.Encoding.ASCII.GetBytes(oldAscii);
        var nb = System.Text.Encoding.ASCII.GetBytes(newAscii);
        int n = 0;
        var outB = new List<byte>(b.Length);
        for (int i = 0; i < b.Length; )
        {
            if (i + ob.Length <= b.Length && MatchAt(b, i, ob)) { outB.AddRange(nb); i += ob.Length; n++; }
            else { outB.Add(b[i]); i++; }
        }
        if (n > 0) File.WriteAllBytes(AbsFile(path), outB.ToArray());
        return n;
    }

    static bool MatchAt(byte[] hay, int at, byte[] pat)
    {
        for (int i = 0; i < pat.Length; i++) if (hay[at + i] != pat[i]) return false;
        return true;
    }

    /// <summary>🔴 **A1299 的堵法**：数刚写出的 prefab 里 `m_Script` 是 guid0 的行数 —— **量产物**。
    /// `sample` 收前 6 行原文（点名用，日志用 UTF-8 解，只影响可读性）。</summary>
    public static int CountZeroGuidScripts(string path, List<string> sample)
    {
        var b = File.ReadAllBytes(AbsFile(path));
        var key = System.Text.Encoding.ASCII.GetBytes("m_Script:");
        var zero = System.Text.Encoding.ASCII.GetBytes("guid: " + ZeroGuid);
        int n = 0;
        for (int i = 0; i + key.Length <= b.Length; i++)
        {
            if (!MatchAt(b, i, key)) continue;
            int e = i;
            while (e < b.Length && b[e] != (byte)'\n') e++;     // 这一行
            bool hit = false;
            for (int j = i; j + zero.Length <= e; j++) if (MatchAt(b, j, zero)) { hit = true; break; }
            if (!hit) continue;
            n++;
            if (sample.Count < 6) sample.Add(System.Text.Encoding.UTF8.GetString(b, i, e - i).Trim());
        }
        return n;
    }

    static void Export(GameObject src)
    {
        var inst = UnityEngine.Object.Instantiate(src);
        inst.name = src.name;
        StripMissingScripts(inst);
        // 🆕 2026-10-11（A210）：**必须在 `StripMissingScripts` 之后**（见上面那段头注）。
        AttachPoolables(inst, src.name);
        // 🆕 2026-10-08（A1089）：**同一个根因的第二种组件** —— 原版 `StoryProgramming.VATGPUPlayer`
        //   也解析不了、也被 `StripMissingScripts` 连数据一起删掉（且不报错）。同样必须在它之后。
        AttachVatDrivers(inst, src.name);
        // 🆕 2026-10-19（V3）：**第三种被 `StripMissingScripts` 删掉的组件** ——
        //   原版 `ParticleForceForceFieldController`（`m_Script` pathID 2858552349204820270）。
        //   它被删 ⇒ `Will of Grok` 的 `force` 曲线（classID:114）没有接收者。
        //   判据 → `资料/普查产出_1019/V3_报告.md` §一·3。同样必须在 `StripMissingScripts` 之后。
        AttachForceFieldControllers(inst, src.name);

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

        // 🆕 **2026-10-21（审计现读）：`SkinnedMeshRenderer` 的网格** —— 上面那两条**都收不到它**
        //   （它既不挂 `MeshFilter`，也不是 `ParticleSystemRenderer.mesh`）。不导的话
        //   `PrefabUtility.SaveAsPrefabAsset` 落下来是 `m_Mesh: {fileID: 0, guid: 000…0}` ⇒ 整件不画。
        //   实测受害者：`Explosion Hive Fleet Arrival Tendrils OLD` 的 4 个 SMR
        //   （`Cone.001`–`Cone.004`）`m_Mesh` 全是 guid 全 0，台账上那一件因此 **0.20×**。
        //   原版这 4 个 = 1789 verts / 17 bindpose / **非流式**（走 `Instantiate` 是安全的；
        //   因为带 bindpose，`ImportMesh` 会按「形状复杂」打一条 Warning 后退回 `Instantiate` —— 属预期）。
        foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (smr.sharedMesh != null) smr.sharedMesh = ImportMesh(smr.sharedMesh);

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
                if (s == null)
                {
                    // 🆕 **2026-10-21（W61）：`s == null` 这一支不再静默 `continue`。**
                    //   三件 OrkBuff 的骷髅发射器就是在这里丢的：旧循环一句 `continue` ⇒ `ImportSprite` 根本没跑
                    //   ⇒ prefab 里留下 `sprites: - sprite: {fileID: 0}`（或从实例带过来的
                    //   `{fileID: <原包 pathID>, guid: 0}` 伪引用）⇒ 运行时粒子退化成**白色方块**
                    //   ⇒ 台账里那三条「偏亮 1.46 / 1.48 / 1.54×」。
                    //   分工：
                    //     ① 这条 PS 的路径命中 `KnownUvModuleSprites`（**原包里逐槽核过的 16 个槽**）⇒
                    //        按图集真值 rect 直接裁一张精灵补上（`spriteCount == 1` 才走，绝不按序号猜）；
                    //     ② 表外的 null 槽：**只有**「模块启用 + mode = Sprites」才出声（其余是 Unity 的
                    //        默认单元素空表，本来就不该有精灵 —— 出声只会把日志刷满）。
                    //   ⛔ 两条都不拿别的图顶上（那等于静默画错一张）。
                    if (n == 1 && i == 0 && tsa.mode == ParticleSystemAnimationMode.Sprites
                        && TryKnownUvModuleSprite(inst, ps, out var knownName, out var knownRect))
                    {
                        var ktex = FindTextureInPacks(KnownSkullAtlasTexture);
                        if (ktex != null && InAtlasBounds(ktex, knownRect))
                        {
                            var knownPivot = new Vector2(knownRect.width * 0.5f, knownRect.height * 0.5f);
                            var importedKnown = ImportSpriteFromRect(knownName, ktex, knownRect, 100f,
                                                                    knownPivot, knownRect.size, Vector4.zero);
                            if (importedKnown != null)
                            {
                                ps.textureSheetAnimation.SetSprite(i, importedKnown);
                                Debug.Log($"[EffectExporter] `UVModule.sprites` 第 {i} 格在 "
                                        + $"`{inst.name}/{TransformRelPath(inst.transform, ps.transform)}` 上是 null "
                                        + $"⇒ **走已知槽位表**补成 `{knownName}`（图集 `{ktex.name}` · 裁切 "
                                        + $"({knownRect.x},{knownRect.y},{knownRect.width},{knownRect.height})）");
                                continue;
                            }
                        }
                        Debug.LogWarning(EX1 + $"`UVModule.sprites` 第 {i} 格在 "
                                       + $"`{inst.name}/{TransformRelPath(inst.transform, ps.transform)}` 上是 null；"
                                       + $"已知槽位表说它该是 `{knownName}`，但图集纹理 `{KnownSkullAtlasTexture}` "
                                       + "取不到或 rect 越界 ⇒ 这一格留空");
                        continue;
                    }
                    if (tsa.mode == ParticleSystemAnimationMode.Sprites && tsa.enabled)
                        Debug.LogWarning(EX1 + $"`UVModule.sprites` 第 {i} 格在 "
                                       + $"`{inst.name}/{TransformRelPath(inst.transform, ps.transform)}` 上是 null"
                                       + $"（spriteCount={n} · 模块已启用）⇒ 这一格留空"
                                       + "（若是原版非空槽，先按原包证明清楚再加进 `KnownUvModuleSprites`）");
                    continue;
                }
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

        // ---- 🆕 2026-10-08（A1095）：**legacy `Animation` 的片段那一跳** ----
        // 上面那支只做了 `Animator` 一半 —— 而它自己的头注（`:1185`）写的就是
        // 「动画那一跳（`Animator` 的控制器 / `Animation` 的片段）」。**`Animation` 这一半一直是漏的**，
        // 而且不是漏一处、是**系统性**地漏：
        //   实测扫 `WarpforgeVFX/Prefabs/*.prefab` **963 件** ⇒ `--- !u!111`（`Animation`）共 **50 个**，
        //   其中 **45 个**的 `m_Animation` 是 `{fileID: …, guid: 00000000000000000000000000000000, type: 0}`
        //   （`m_Animations[]` 里那一条**同样是 guid 全 0**），分布在 **40 件** prefab
        //   （`AcidSpraySweepAttack` / `BolterSweep` / `CardPrefab` / `EC Heldrake Attack` /
        //    一串 `Environmental Condition *` / `RemnantBody3D *` / `Sau_*` / `Valkyrie_*`…）。
        //   ⇒ 上一件**手改**好的那一条（`VanguardIdleEffect.prefab:59081/59083`，指到
        //     `WarpforgeVFX/Animations/Vanguard Frame Animation.anim`）**重导就会被打回 guid 全 0**，
        //     而其余 44 条**从来就没好过**（那一趟只补了手改的那一件，没补任何厂里的路）。
        //   判据 → `资料/普查产出_第六会话/W_VAT收口_地雷与另5件.md` §③·1（逐条普查在本文件调用点那份报告里）。
        // ⚠️ **剩下 4 个 `Animation` 不算**：它们的 `m_Animation` 是 `fileID: 0`、`m_Animations` 是空表或
        //    `[- {fileID: 0}]` —— 那是**原版本来就没接片段**（逐条核过 `assets_full/…/Animation/Animation_*.json`：
        //    包里同样有 4 个 `m_Animation = 0`）⇒ 别给它造一个。
        // ⚠️ **两条入口都要写、缺一不可**：`m_Animation`（默认那条）与 `m_Animations[]`（真正被
        //    `Animation.Play("名字")` 查的那张表）。实测两者常常是同一个 clip，但 `Pray_Idle`
        //    （`[Pray Enter, Pray Exit]`）与 `RemnantBody3D Necrons`（`[To Remnant Necrons, From Remnant Necrons]`）
        //    **明显不止一条** ⇒ 只补 `m_Animation` 会**静默漏掉第二条**。
        // ⚠️ **不传 `loop`**（这一点和 `Animator` 那支**有意不同**）：那支要按控制器状态的 `m_Loop` 显式设，
        //    而 legacy clip 自己的 `m_WrapMode` / `m_AnimationClipSettings` 会随 `Instantiate` 一起过来
        //    （实测落出来的 `Vanguard Frame Animation.anim` 就是 `m_WrapMode: 1` / `m_LoopTime: 0`，与原版一致）
        //    ⇒ 传 `loop` 反而变成**我们替它做决定**。
        // ⚠️ **跨包的也取得到**：`ScytheAssault.prefab` 那条 clip **不在特效主包里**，在
        //    `cardanimsgeneral_assets_all.bundle`（`AnimationClip_-5462575267959983568.json`，`m_Name: ScytheAssault`）。
        //    本函数加载段（`:681`）是「`BundleDir` 下**全部** `*.bundle` 都载进来」⇒ 跨包引用跟着一起解析，
        //    而 `ImportClip` 也不关心它来自哪个包。
        int clipOk = 0, clipMiss = 0;
        foreach (var an in inst.GetComponentsInChildren<Animation>(true))
        {
            // ① `m_Animations[]`。⚠️ `AnimationUtility.GetAnimationClips(Animation)` 那个重载**已 obsolete**
            //    （Unity 文档原文：「is obsolete and has been replaced with GetAnimationClips(GameObject)」）
            //    ⇒ 走 `GameObject` 那个（非 obsolete、语义相同：取该物件上 `Animation` 的片段表）。
            var clips = AnimationUtility.GetAnimationClips(an.gameObject);
            var def = an.clip;        // ② `m_Animation`：**先取下来**，别指望写回数组之后它还认得原来那条
            if (clips != null && clips.Length > 0)
            {
                var imported = new AnimationClip[clips.Length];
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i] == null) continue;      // 空槽 ⇒ 原样留空（原版就是空槽的那种，不算「丢」）
                    var a = ImportClip(clips[i]);
                    if (a == null)
                    {
                        clipMiss++;
                        Debug.LogWarning(EX1 + $"A1095 `{src.name}` / `{an.gameObject.name}` 上 `Animation` 的"
                            + $"第 {i} 条片段（`{clips[i].name}`）落不成工程 `.anim` ⇒ **这一格留空**"
                            + "（⛔ 不拿空 clip 顶上 —— 那等于静默造一个假动作）");
                        continue;
                    }
                    imported[i] = a; clipOk++;
                }
                AnimationUtility.SetAnimationClips(an, imported);
            }
            // ③ `m_Animation` 写在**数组之后**（顺序有讲究）：先让它指的那条已经在 `m_Animations` 里，
            //    再设默认值 ⇒ 不依赖「`clip` setter 会不会顺手往数组里补一条」这个没查证的细节。
            if (def != null)
            {
                var d = ImportClip(def);
                if (d != null) an.clip = d;
            }
        }
        if (clipOk > 0 || clipMiss > 0)
            Debug.Log(EX1 + $"A1095 `{src.name}` legacy `Animation` 片段：接回 **{clipOk}** 条"
                          + (clipMiss > 0 ? $"、**没接上 {clipMiss} 条**（见上面的告警）" : ""));

        // ---- 🆕 2026-10-19（A1325）：**uGUI `Image.m_Sprite` 与 TMP `m_fontAsset` 两跳** ----
        // 写法照抄 `BoosterPackExporter.Export()` 里同名那两支（`Image.m_Sprite` ≈ `:512`、TMP 字体 ≈ `:563`）。
        // **为什么必须补**：两者都是 **bundle 资产**，不导的话落盘会写成 `guid 全 0` 的伪引用
        // （现读 `Prefabs/CardPrefab.prefab`：`m_Sprite` guid0 **6** · `m_fontAsset` guid0 **38**；
        //  `m_Script` 已是 **0**，那条由 `SavePrefabAndRepairScripts` 负责，不在这里）。
        // ⚠️ 纪律与既有的精灵跳一致：**接不上就留空 + 点名出声**，⛔ 绝不拿别的图/别的字体顶上。
        // ⚠️ TMP 字体一律按**序列化字段**读（`SerializedFieldRef`），⛔ 不用 `t.font` getter ——
        //    那个 getter 在 `m_fontAsset == null` 时会回落到 `TMP_Settings.defaultFontAsset`
        //    （同 `Image.material` 那个坑），会把「本来没有字体」的也数成有。
        //    字体按**资产名**到工程里找同名 `TMP_FontAsset`（`CardPresentation/Resources/Fonts/` 下已有
        //    `Pragati-Regular SDF` / `Asar-Regular SDF`）⇒ 将来谁把别的原版字体导进来，这里不用改代码；
        //    ⛔ 找不到返回 null（留空），**绝不新建**、更不 `CreateFontAsset` 现造一份。
        //    附注：`t.font = f` 会走 TMP 的 `LoadFontAsset()`，它会在 `m_sharedMaterial` 为空/图集不匹配时
        //    把 `m_sharedMaterial` 指到字体资产自己的材质 ⇒ 顺带把 `m_sharedMaterial` 的 guid0 也修掉。
        int uiSpriteOk = 0, uiSpriteMiss = 0;
        foreach (var img in inst.GetComponentsInChildren<UnityEngine.UI.Image>(true))
        {
            if (img.sprite == null) continue;      // 原版本来就空的那种（不算「丢」）
            var imported = ImportSprite(img.sprite);
            if (imported == null)
            {
                uiSpriteMiss++;
                Debug.LogWarning($"[EffectExporter] A1325 `{src.name}` / `{img.gameObject.name}` 上的 `Image.m_Sprite`"
                    + $"（`{img.sprite.name}`）落不成工程 sprite ⇒ **这一格留空**"
                    + "（⛔ 不拿别的图顶上 —— 那等于静默画错一张）");
                continue;
            }
            img.sprite = imported; uiSpriteOk++;
        }

        // ---- 🆕 2026-10-21（`A1325` 第二半）：**uGUI `Image.m_Material` 那一跳** ----
        // 头注（`:1950` 那段）写的「`A1325` 两跳」其实只有 `m_Sprite` 与 TMP `m_fontAsset`；
        // `m_Material` **一直没人接** ⇒ 现读 `Prefabs/CardPrefab.prefab` 里 `m_Material` guid 全 0 **5 条**
        // （`Card Backs SDF` ×1 · `Card Image Overflow` ×1 · `Card Frame SDF` ×1 ·
        //  `Card Frame and Cardback for combat` ×2）。写法与 `BoosterPackExporter.Export()` 里
        // 同名那一支（(2) 段）**同形**：
        // 🔴 **不能读 `img.material`** —— 那是**带回落**的 getter：`m_Material == null` 时它返回
        //    **内建默认材质**（带 alpha 分离贴图的精灵还会返回 `defaultETC1GraphicMaterial`）
        //    ⇒ 照它取值会把「本来没有材质」的 Image 也数成有、再给它们各写一份工程副本
        //    （= **静默改掉原始数据**）。必须按**序列化字段名**读（`SerializedFieldRef`）。
        // ⚠️ 四份源材质里三份工程里**已有** `WarpforgeVFX/Materials/*.mat`（`ImportMaterial` 走
        //    **固定路径 + 原地覆盖**，guid 不变），`Card Frame and Cardback for combat` 没有 ⇒ 由它落一份。
        //    shader 全是 Everguild 私有 ⇒ 会落**近似替代**（`ImportMaterial` 已出声，这里再点名一次）。
        int uiMatOk = 0, uiMatMiss = 0, uiMatApprox = 0;
        foreach (var img in inst.GetComponentsInChildren<UnityEngine.UI.Image>(true))
        {
            var om = SerializedFieldRef(img, "m_Material") as Material;
            if (om == null) continue;              // 原版本来就空（绝大多数 Image）
            var nm = ImportMaterial(om, out bool uiApprox, out string uiShader);
            if (nm == null)
            {
                uiMatMiss++;
                Debug.LogWarning($"[EffectExporter] A1325 `{src.name}` / `{img.gameObject.name}` 上的 `Image.m_Material`"
                    + $"（`{om.name}` · 原 shader `{uiShader}`）导不成工程材质 ⇒ **这一格留空**"
                    + "（⛔ 不拿空材质顶上）");
                continue;
            }
            img.material = nm; uiMatOk++;
            if (uiApprox)
            {
                uiMatApprox++;
                Debug.LogWarning($"[EffectExporter] A1325 `{src.name}` / `{img.gameObject.name}` 的材质 `{om.name}`"
                    + $"（原 shader `{uiShader}`）**是按近似 shader 替代的** —— 接上了，但观感不是原版");
            }
        }

        // ---- 🆕 2026-10-21（`A1325` 第三跳）：**TMP 的 `m_sharedMaterial`（第二跳的材质半边）** ----
        // 上面那段头注写的「两跳」只有 `m_Sprite` 与 TMP `m_fontAsset`；**材质这一跳一直没人显式接**，
        // 而是靠 `t.font = f` 的**副作用**顺带改 —— 而那个副作用是**有条件的**：
        // `TextMeshProUGUI.cs:1256-1263` 只在「`m_sharedMaterial` 为 null / 没有 `_MainTex` /
        //  `_MainTex` 不是 `m_fontAsset.atlasTexture`」时才把它改成 `m_fontAsset.material`。
        // ⇒ 原版 `CardPrefab` 里 38 条 `m_sharedMaterial` 指向 **9 种描边材质**，被**统一成 2 种**
        //   （两份字体各自自带的那一份）—— 描边粗细 / 描边色 / `_FaceColor` / `_GradientScale` 全部丢掉。
        //   现读产物 `Prefabs/CardPrefab.prefab` 正是这个结果：38 条只剩 **2** 个 guid
        //   （`Pragati-Regular Atlas Material` ×30 · `Asar-Regular Atlas Material` ×8）。
        //   ⚠️ `check_prefab_refs.py` **看不出来**（refs 都解析得到，只是指错了东西）——
        //   判据得数「不同的 guid 个数 = 9」，见报告 §6。
        // 写法照 `BoosterPackExporter.cs:600-620`：逐条 `ImportMaterial` → `fontSharedMaterial`。
        // 🔴 **顺序**：必须**先读 `m_sharedMaterial`、再设 `t.font`** —— 设字体那一下会把序列化字段覆盖掉，
        //    反过来的话读到的永远是字体自带材质（= 把原版值读没了，而且**看着完全正常**）。
        // ⚠️ 纪律同上面几支：**接不上就留空 + 点名出声**，⛔ 不拿别的材质顶上。
        int tmpFontOk = 0, tmpFontMiss = 0;
        int tmpMatOk = 0, tmpMatMiss = 0, tmpMatApprox = 0, tmpMatAtlasRebound = 0, tmpMatAlreadyProject = 0;
        var missingFonts = new Dictionary<string, int>();
        var missingMats = new Dictionary<string, int>();
        // `ShaderUtilities.ID_MainTex` 是 `GetShaderPropertyIDs()` 里填的（TMP 自己在 `LoadFontAsset()` 开头也调它）；
        // 这里显式先调一次，免得「字体没接上 ⇒ 没走进 LoadFontAsset ⇒ ID 还是 0」时读错属性。
        TMPro.ShaderUtilities.GetShaderPropertyIDs();
        foreach (var t in inst.GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            // ① 先读原版材质（见上面那段「顺序」）。
            var srcMat = SerializedFieldRef(t, "m_sharedMaterial") as Material;

            // ② 字体那一跳（原样保留）。
            var srcFont = SerializedFieldRef(t, "m_fontAsset") as TMPro.TMP_FontAsset;
            TMPro.TMP_FontAsset f = null;
            if (srcFont != null)
            {
                f = FindProjectAsset<TMPro.TMP_FontAsset>(srcFont.name);
                if (f != null) { t.font = f; tmpFontOk++; }
                else
                {
                    tmpFontMiss++;
                    missingFonts[srcFont.name] =
                        (missingFonts.TryGetValue(srcFont.name, out int c0) ? c0 : 0) + 1;
                }
            }

            // ③ 材质那一跳。
            if (srcMat == null) continue;          // 原版本来就空的那种（不算「丢」）
            // 3D `TMPro.TextMeshPro` 的渲染器在 `:1769` 那圈里**已经导过**（材质池是同一个）⇒ 直接复用。
            // ⛔ 别在这里二次 `ImportMaterial` —— 源已经是工程资产时那等于「自己导自己」。
            if (AssetDatabase.Contains(srcMat))
            {
                t.fontSharedMaterial = srcMat; tmpMatOk++; tmpMatAlreadyProject++;
                continue;
            }
            var pm = ImportMaterial(srcMat, out bool tmpApprox, out string tmpShader);
            if (pm == null)
            {
                tmpMatMiss++;
                missingMats[srcMat.name] =
                    (missingMats.TryGetValue(srcMat.name, out int c1) ? c1 : 0) + 1;
                Debug.LogWarning($"[EffectExporter] A1325 `{src.name}` / `{t.gameObject.name}` 上的 TMP `m_sharedMaterial`"
                    + $"（`{srcMat.name}` · 原 shader `{tmpShader}`）导不成工程材质 ⇒ **这一格留空**"
                    + "（⛔ 不拿空材质顶上；⚠️ TMP 随后会按 `LoadFontAsset()` 回落成**字体自带材质** ——"
                    + " 那是近似，不是原版那一份描边）");
                continue;
            }
            // 🔴 **图集守卫**：TMP 认材质的前提是「`_MainTex` == 该字体的图集」——
            //    否则下次 `LoadFontAsset()`（运行时 Awake/OnEnable 就会走）又会把这一格改回字体自带材质，
            //    **9 种描边在游戏里重新塌成 2 种，而且一声不吭**。
            //    今天两者通常是同一份资产（`ImportTexture` 落到的路径恰好就是字体图集那份 `.png`），
            //    所以这一支一般是空转；写在这里是**把「恰好」变成「保证」**（字体资产一换名就会踩）。
            //    ⚠️ 同一条资产时 `ImportTexture` 的 `SaveAndReimport()` 会把贴图对象换掉，而
            //       `TMP_FontAsset.atlasTexture` 是**缓存过的** ⇒ 这里读到的可能是个已销毁对象，
            //       `wantAtlas != null` 为 false ⇒ **静默跳过**。这一档恰好就是「本来就匹配」的那档，
            //       跳过无害；真要改绑时（两份不同资产）字体图集不会被重导，读到的一定是活对象。
            if (f != null)
            {
                var wantAtlas = f.atlasTexture;
                var haveAtlas = pm.GetTexture(TMPro.ShaderUtilities.ID_MainTex);
                if (wantAtlas != null && haveAtlas != wantAtlas)
                {
                    pm.SetTexture(TMPro.ShaderUtilities.ID_MainTex, wantAtlas);
                    EditorUtility.SetDirty(pm);
                    tmpMatAtlasRebound++;
                }
            }
            t.fontSharedMaterial = pm; tmpMatOk++;
            if (tmpApprox)
            {
                tmpMatApprox++;
                Debug.LogWarning($"[EffectExporter] A1325 `{src.name}` / `{t.gameObject.name}` 的 TMP 材质 `{srcMat.name}`"
                    + $"（原 shader `{tmpShader}`）**是按近似 shader 替代的** —— 接上了，但观感不是原版");
            }
        }
        if (uiSpriteOk > 0 || uiSpriteMiss > 0)
            Debug.Log($"[EffectExporter] A1325 `{src.name}` uGUI `Image.m_Sprite`：接回 **{uiSpriteOk}** 条"
                    + (uiSpriteMiss > 0 ? $"、**没接上 {uiSpriteMiss} 条**（见上面的告警）" : ""));
        if (uiMatOk > 0 || uiMatMiss > 0)
            Debug.Log($"[EffectExporter] A1325 `{src.name}` uGUI `Image.m_Material`：接回 **{uiMatOk}** 条"
                    + (uiMatOk > 0 ? $"（其中近似替代 {uiMatApprox} 条）" : "")
                    + (uiMatMiss > 0 ? $"、**没接上 {uiMatMiss} 条**（见上面的告警）" : ""));
        if (tmpFontOk > 0 || tmpFontMiss > 0)
        {
            var mv = new List<string>();
            foreach (var kv in missingFonts.OrderByDescending(k => k.Value))
                mv.Add($"`{kv.Key}`×{kv.Value}");
            Debug.Log($"[EffectExporter] A1325 `{src.name}` TMP `m_fontAsset`：接回 **{tmpFontOk}** / 留空 **{tmpFontMiss}**"
                    + (mv.Count > 0 ? $"（工程里没有这些 TMP_FontAsset：{string.Join(" · ", mv)}）" : ""));
        }
        if (tmpMatOk > 0 || tmpMatMiss > 0)
        {
            var mm = new List<string>();
            foreach (var kv in missingMats.OrderByDescending(k => k.Value))
                mm.Add($"`{kv.Key}`×{kv.Value}");
            Debug.Log($"[EffectExporter] A1325 `{src.name}` TMP `m_sharedMaterial`：接回 **{tmpMatOk}** 条"
                    + (tmpMatAlreadyProject > 0 ? $"（其中 {tmpMatAlreadyProject} 条本来就是工程资产，直接复用）" : "")
                    + (tmpMatApprox > 0 ? $"（其中近似 shader {tmpMatApprox} 条）" : "")
                    + (tmpMatAtlasRebound > 0 ? $"（图集改绑回字体资产 {tmpMatAtlasRebound} 条）" : "")
                    + (tmpMatMiss > 0 ? $"、**留空 {tmpMatMiss} 条**（见上面的告警）" : ""));
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
        // 🆕 2026-10-10（A1298/A1299）：落盘前把指不到工程脚本的 `m_Script` 接成工程脚本，
        // 落盘后**按产物**数一遍 guid0 并在需要时按表改回 —— 这三件事就这一行（判据见函数头部那段）。
        var sfx = SavePrefabAndRepairScripts(inst, path);
        UnityEngine.Object.DestroyImmediate(inst);

        LastDetail = $"材质定义{defs.Count}个；原 shader: {string.Join(", ", usedShaders.OrderBy(x => x))}" +
                     (approx > 0 ? $"；近似替代 {approx} 处" : "") +
                     (sfx.fixedInMemory > 0 ? $"；m_Script 接回 {sfx.fixedInMemory} 条" : "") +
                     (sfx.zeroOnDisk > 0 ? $"；**m_Script 落盘guid0 {sfx.zeroOnDisk}→改回{sfx.repaired}→剩{sfx.left}**" : "");
    }

    /// <summary>🆕 A1325：按**序列化字段名**读一个对象引用（⛔ 不用带回落的 getter —— 例如
    /// `TMP_Text.font` 在 `m_fontAsset == null` 时会回落到 `TMP_Settings.defaultFontAsset`、
    /// `Image.material` 会回落到内建默认材质 ⇒ 照 getter 取值会把「本来没有」的也数成有）。
    /// 与 `BoosterPackExporter.SerializedFieldRef` 同形；读失败**出声**并按「没有」处理。</summary>
    static UnityEngine.Object SerializedFieldRef(UnityEngine.Object comp, string field)
    {
        try
        {
            using (var so = new SerializedObject(comp))
            {
                var p = so.FindProperty(field);
                if (p == null || p.propertyType != SerializedPropertyType.ObjectReference) return null;
                return p.objectReferenceValue;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[EffectExporter] A1325 读 `{comp.GetType().Name}.{field}` 失败"
                           + $"（这一格按「没有」处理）：{e.GetType().Name}: {e.Message}");
            return null;
        }
    }

    /// <summary>🆕 A1325：按**资产名**到工程里找一份同名资产；⛔ 找不到返回 `null`
    /// （调用方**留空 + 出声**），**绝不新建**。与 `BoosterPackExporter.FindProjectAsset` 同形。
    /// 结果按 `(类型, 名字)` 缓存 —— 一份 prefab 里同一份字体要被问上百次。</summary>
    static readonly Dictionary<string, UnityEngine.Object> ProjAssetCacheA1325 =
        new Dictionary<string, UnityEngine.Object>();

    static T FindProjectAsset<T>(string name) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(name)) return null;
        string key = typeof(T).Name + "|" + name;
        if (ProjAssetCacheA1325.TryGetValue(key, out var hit)) return hit as T;
        T found = null;
        try
        {
            foreach (var g in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p) != name) continue;
                found = AssetDatabase.LoadAssetAtPath<T>(p);
                if (found != null) break;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[EffectExporter] A1325 找工程资产 `{name}`（{typeof(T).Name}）失败"
                           + $"（按「没有」处理）：{e.GetType().Name}: {e.Message}");
        }
        ProjAssetCacheA1325[key] = found;
        return found;
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

    /// <summary>🔴 **一源一份**（`A845(b)`）：同名多源时，给每一个**内容不同**的源各定一个确定路径。
    /// 判据与设计理由见 `MatVariants` 头部那段；材质与网格**共用这一份规则**（只传不同的表 / 后缀），
    /// ⛔ 别在两处各写一套（`CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」）。
    /// 返回该源该落的路径；`collision` = 本次是不是「同名但内容不同」（调用方据此出声）。
    /// `moved` 收「先到的那份被就地改名」的记账（`X.mat → X__h.mat`）。</summary>
    static string ResolveVariantPath(Dictionary<string, List<(List<string> fields, string path)>> reg,
                                    string dir, string ext, string stem, List<string> fields,
                                    List<string> moved, out bool collision)
    {
        collision = false;
        List<(List<string> fields, string path)> lst;
        if (!reg.TryGetValue(stem, out lst)) { lst = new List<(List<string>, string)>(); reg[stem] = lst; }

        // ① 内容等价 ⇒ 复用已经落好的那一份（塌成一份无害，正是原来「不出声」的那种）
        foreach (var v in lst) if (FieldDiff(v.fields, fields).Count == 0) return v.path;

        string bare = $"{dir}/{stem}{ext}";
        // ② 这个名字下的第一个内容 —— 除非**上一次导出**已经留下了指纹兄弟（那说明它撞过名）
        if (lst.Count == 0)
        {
            string p0 = HasFingerprintedSibling(dir, stem, ext) ? FingerprintPath(dir, stem, ext, fields) : bare;
            lst.Add((fields, p0));
            return p0;
        }

        // ③ 又来一种不同的内容 ⇒ 先到的若还占着裸名，就地挪到**它的**指纹名（`MoveAsset` 保 guid）
        var first = lst[0];
        if (first.path == bare)
        {
            var fp = FingerprintPath(dir, stem, ext, first.fields);
            MoveAssetKeepGuid(bare, fp);
            lst[0] = (first.fields, fp);
            moved.Add($"{stem}{ext} → {Path.GetFileName(fp)}");
        }
        collision = true;
        var np = FingerprintPath(dir, stem, ext, fields);
        lst.Add((fields, np));
        return np;
    }

    /// <summary>`{dir}/{stem}__{内容短哈希}{ext}` —— 撞名分文件的命名（**我们挑的**，见 `MatVariants` 头部）。</summary>
    static string FingerprintPath(string dir, string stem, string ext, List<string> fields)
        => $"{dir}/{stem}__{ShortHash(fields)}{ext}";

    /// <summary>内容指纹：**自己实现的 FNV-1a 32 位**（8 位十六进制）。
    /// ⛔ 不用 `GetHashCode()`（跨进程/跨运行不保证稳定）；⛔ 不引新依赖。
    /// ⚠️ **排除 `源名=`** —— 那是「这是谁」，不是「内容是什么」：同名多源比的就是内容。
    /// ⚠️ 字段值是 `Color.ToString()` 那类与本机 culture 有关的格式 —— **同一台机器上稳定**即可
    ///（这个哈希只用来给同一次导出里的同内容分组，不做跨机校验）。</summary>
    static string ShortHash(List<string> fields)
    {
        uint h = 2166136261u;                       // FNV-1a 32 位
        foreach (var f in fields)
        {
            if (f.StartsWith("源名=", StringComparison.Ordinal)) continue;
            foreach (var ch in f) { h ^= ch; h *= 16777619u; }
            h ^= (uint)'\n'; h *= 16777619u;
        }
        return h.ToString("x8");
    }

    /// <summary>这个名字**在盘上**是不是已经有 `{stem}__{8位十六进制}{ext}` 的指纹兄弟了。
    /// 为什么需要它（`A845(b)` 的顺序无关性）：`Run()` 清产物 + 逐根遍历 ⇒ 本轮第一个撞名源
    /// 在**检测到撞名之前**就已经占了裸名。而**上一次导出留下的指纹文件是常态**（名字集合是稳定的）
    /// ⇒ 先占裸名、再撞名、再挪走，就一定会撞上「目标已存在」。
    /// 判据 = 只看「`__` 后面恰好是 8 位十六进制」的那些（**避开**源名自己带 `__` 的假阳性）。
    /// ⚠️ 每个目录只扫一次（`Run()` 的 `ClearGenerated()` 在任何导入之前 ⇒ 缓存不会过期）。</summary>
    static bool HasFingerprintedSibling(string dir, string stem, string ext)
    {
        HashSet<string> stems;
        if (!FingerprintedStems.TryGetValue(dir, out stems))
        {
            stems = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                foreach (var p in Directory.GetFiles(AbsFile(dir)))
                {
                    var f = Path.GetFileName(p);
                    if (!f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) continue;
                    int i = f.IndexOf("__", StringComparison.Ordinal);
                    while (i > 0)
                    {
                        var tail = f.Substring(i + 2, f.Length - i - 2 - ext.Length);
                        if (tail.Length == 8 && tail.All(ch => (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f')))
                            stems.Add(f.Substring(0, i));
                        i = f.IndexOf("__", i + 1, StringComparison.Ordinal);
                    }
                }
            }
            catch (Exception e) { Debug.LogWarning($"[EffectExporter] 扫 `{dir}` 找指纹兄弟失败（当没有）：{e.Message}"); }
            FingerprintedStems[dir] = stems;
        }
        return stems.Contains(stem);
    }

    /// <summary>把一份**已经在盘上**的产物挪到指纹名。`AssetDatabase.MoveAsset` **保 guid** ⇒ 引用不断。
    /// ⚠️ 目标已存在（上一次导出留下的、同内容的指纹文件）⇒ **删掉旧的再把本次的挪过去**：
    /// 保的是**本次**那份的 guid（它刚刚被写进本次的 prefab，是活的）；上一次那份只可能被上一次的产物
    /// 引用，而那些产物本次会重写。⛔ 静默不许 —— 这一步一定出声。</summary>
    static void MoveAssetKeepGuid(string from, string to)
    {
        if (AssetDatabase.LoadMainAssetAtPath(to) != null)
        {
            Debug.LogWarning($"[EffectExporter] 指纹名 `{Path.GetFileName(to)}` 已被上一次导出占着（同内容）"
                           + " ⇒ 删掉旧的那份、把本次的挪过去（保**本次**的 guid；上一次引用旧 guid 的产物本次会重写）");
            AssetDatabase.DeleteAsset(to);
        }
        var err = AssetDatabase.MoveAsset(from, to);
        if (!string.IsNullOrEmpty(err))
            Debug.LogError($"[EffectExporter] 挪 `{from}` → `{to}` 失败：{err} ⇒ 这个名字下会有两份内容抢一个文件名");
    }

    /// <summary>撞名出声（**内容不同才响**）。🔴 `A845(b)` 之后那句话**改了**（铁律 5：就地改掉错记录）：
    /// 原来写的是「后到的赢，先到的那种在工程里没有对应文件」—— 现在**两份各有一份文件**。</summary>
    static void WarnCollision(string kind, string stem, List<(List<string> fields, string path)> variants,
                              List<string> newer, string newerName, List<string> moved)
    {
        var first = variants[0];
        var diff = FieldDiff(first.fields, newer);
        var shown = string.Join(" / ", diff.Take(6));
        Debug.LogWarning($"[EffectExporter] {kind}**撞名且内容不同**：`{stem}` —— "
                       + $"先到 `{NameOfVariant(first.fields)}`、后到 `{newerName}`；"
                       + $"差异 {diff.Count} 处（最多列 6）：{shown}" + (diff.Count > 6 ? " …" : "")
                       + $" ⇒ **一份一个源**（这是我们挑的：原版没有「{kind}资产」这个概念）："
                       + string.Join(" · ", variants.Select(v => "`" + Path.GetFileName(v.path) + "`"))
                       + (moved.Count > 0 ? $"；先到的那份已就地改名（`MoveAsset` **保 guid**）：{string.Join(" · ", moved)}" : ""));
    }

    /// <summary>`源名=X` 那一项里的名字（`MatFields`/`MeshFields` **约定第 0 项就是它**）。</summary>
    static string NameOfVariant(List<string> fields)
    {
        var s = fields.Count > 0 ? fields[0] : "";
        return s.StartsWith("源名=", StringComparison.Ordinal) ? s.Substring(3) : "<?>";
    }

    /// <summary>🔴 **灭自证断言**（`A845(b)`）—— **量产物、不量我们自己的表**。三条：
    /// ① 多内容的名字里**没有任何一份占着裸名** `X.mat`（**删掉「把先到的挪走」那一句 ⇒ 这一条必红**）；
    /// ② 各变体路径互不相同；③ 每一条路径在盘上真的存在。
    /// 跑在 `Run()` / `RunListed()` 收尾（各一行调用）。⚠️ 它**只在本次导出的名字集合上**成立 ——
    /// 分片续跑（`Resume`）时只有本片碰过的名字在表里，别把它读成「全库都核过了」。</summary>
    public static void ReportVariantPaths()
    {
        var problems = new List<string>();
        int names = 0, files = 0;
        CheckVariantPaths(MatVariants, MatDir, ".mat", problems, ref names, ref files);
        CheckVariantPaths(MeshVariants, MeshDir, ".asset", problems, ref names, ref files);
        if (names == 0)
        {
            Debug.Log("[EffectExporter] A845(b) 撞名分文件：本次**没有**「同名不同内容」的源（0 个名字）");
            return;
        }
        if (problems.Count == 0)
            Debug.Log($"[EffectExporter] A845(b) **一源一份**已达成：{names} 个名字各有 ≥2 种内容、共 {files} 份文件 —— "
                    + "① 没有一份占着裸名 `X` ② 路径互不相同 ③ 每份在盘上都在 ✅");
        else
            Debug.LogError($"[EffectExporter] A845(b) **一源一份没达成**（{problems.Count} 条）："
                         + string.Join(" / ", problems.Take(10)));
    }

    static void CheckVariantPaths(Dictionary<string, List<(List<string> fields, string path)>> reg,
                                  string dir, string ext, List<string> problems, ref int names, ref int files)
    {
        foreach (var kv in reg)
        {
            if (kv.Value.Count < 2) continue;
            names++;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var v in kv.Value)
            {
                files++;
                if (v.path == $"{dir}/{kv.Key}{ext}") problems.Add($"`{kv.Key}` 仍有一份占着裸名 {Path.GetFileName(v.path)}");
                if (!seen.Add(v.path)) problems.Add($"`{kv.Key}` 两份落在同一路径 {Path.GetFileName(v.path)}");
                if (AssetDatabase.LoadMainAssetAtPath(v.path) == null) problems.Add($"`{kv.Key}` 的 {Path.GetFileName(v.path)} 不在盘上");
            }
        }
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
        //    两个成因并存：**(a) 同名不同源材质**（原件就有，见 `MatVariants` 那一段）+ **(b) `RunListed()`
        //    不清产物**（`Run()` 那条路每次先 `ClearGenerated()`，现存副本会被下一次全量重导自己带走）
        //    —— 实测 `LightningTrail_intense` 原版只有 1 个源材质、我们却有 12 份。
        //    ⇒ 确定路径对 (b) 是**必需**的；**(a) 由 `A845(b)` 补成「一源一份」**（先到的挪到指纹名、
        //    后到的直接带指纹；`MoveAsset` 保 guid）—— 🔴 **记录订正（2026-10-10）**：这里原来写的是
        //    「(a) 则把『多份文件』换成『后写的赢』，由 `GuardMatName` 出声」—— 那是**只出声、没修**的
        //    半成品，`A845(b)` 已经把它修掉了（旧写法见 `ResolveVariantPath` 头部）。
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
        // 🔴 `A845(b)` **一源一份**：同名不同内容时两份各带指纹（先到的会被就地挪走，`MoveAsset` 保 guid）。
        //    判据/理由见 `MatVariants` 头部；**网格那条路是同一个函数**（别在两处各写一套）。
        var mFields = MatFields(src);
        var mMoved = new List<string>();
        bool collided;
        string path = ResolveVariantPath(MatVariants, MatDir, ".mat", stem, mFields, mMoved, out collided);
        if (collided) WarnCollision("材质", stem, MatVariants[stem], mFields, src.name, mMoved);
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
            // 🔴 2026-10-19（本波 FX-2 · 账 V1）：**无条件 `nPOTScale = None`**（原版真值）。
            //   判据同 `FixTextureImportSettings`：原版 runtime 的 `Texture2D` 尺寸就是 NPOT
            //   （`Glow UI W40K` 123×123 · `trait_icon_*` 80×80），⇒ 原版导入器**没做 POT 缩放**；
            //   本工程这 13 张历史上被 Unity 默认的 `ToNearest(1)` 缩成 64²/128²。
            //   对 POT 输入是 **no-op**（本来就是 2 的幂）⇒ 无条件写不引入差异。
            //   ⛔ 方向别弄反：**不是**把 NPOT 补成 POT。
            //   判据全文 → `资料/普查产出_1019/W57_特效线_T15_V1_T02静态.md` §B。
            ti.npotScale = TextureImporterNPOTScale.None;
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
            // 🔴 **2026-10-19（T07）更正：原来这里写死 `wrapMode = Repeat` —— 那句是错的。**
            //   实测（84 个包 · 2984 张 Texture2D 的 `m_TextureSettings`）：**Repeat 2410 / Clamp 67 / Mirror 7**
            //   ⇒ 有 **74 张**原版不是 Repeat。典型代价 = `Data-mat-_MainTex-atlas-0`
            //   （`EnvironmentalCondition Leviathan *` 渲染器 0 那张 4096×2048）：原版 **Mirror**、
            //   我们 Repeat ⇒ 四边形 UV 触到边缘时**把对侧像素卷回来** ⇒ 左右各 2px 细边（能量比 1.011）。
            //   ⇒ 现在**照抄原版四格**：wrapU/wrapV/filterMode/aniso。
            //   🔴 **2026-10-19 晚更正（T02）：这里原来写「Unity 的 TextureImporter 只有一个 `wrapMode`，
            //   原版 U≠V 时取 U」—— 那个前提是错的（本机 6000.3 有独立的 `wrapModeU/V/W`）。
            //   按 U 写的实际代价：`Shine trail`（原版 U=Repeat/V=**Clamp**）的 V 被写成 Repeat，
            //   而拖尾 shader 把 V 放大 2.11 ⇒ V 出界 ⇒ 亮带被卷到两端 ⇒ `Buff_DA_Forest_Self` 差 2.8×。
            //   现在 **U/V 各写各的**；只有一格读不到时，那一格保持现值并出声。
            int wu = -1, wv = -1, fm = -1, aniso = -1;
            try { wu = (int)src.wrapModeU; wv = (int)src.wrapModeV; } catch { }
            try { fm = (int)src.filterMode; } catch { }
            try { aniso = src.anisoLevel; } catch { }
            if (wu >= 0) ti.wrapModeU = (TextureWrapMode)wu;
            else ti.wrapModeU = TextureWrapMode.Repeat;           // 读不到 = 保持旧行为，不静默改成别的
            if (wv >= 0) ti.wrapModeV = (TextureWrapMode)wv;
            else
            {
                ti.wrapModeV = TextureWrapMode.Repeat;            // 读不到 = 保持旧行为
                Debug.LogWarning($"[EffectExporter] 贴图 {src.name} 原版 wrapV 读不到 ⇒ 按 Repeat 写（U={wu}）");
            }
            if (wu >= 0 && wv >= 0 && wv != wu)
                Debug.Log($"[EffectExporter] 贴图 {src.name} 原版 wrapU({wu}) ≠ wrapV({wv}) —— 按 U/V 分别写");
            if (fm >= 0) ti.filterMode = (FilterMode)fm;
            if (aniso > 0) ti.anisoLevel = aniso;
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
        var rect = s.textureRect;                       // 在图集里的像素矩形（左下原点）
        // 🔴 **2026-10-21（`A1325` 第二半）：图集精灵（SpriteAtlas 打包）那一支。**
        //   原来这里是一句 `if (tex == null) return null;` —— 而**图集精灵的 `m_RD.texture` 是
        //   `PPtr(pathID = 0)`**（本次四张：`Card Text smooth background` / `pedestal_icon_armor` /
        //   `Card Frame Cost Icon` / `1_40k_cardframe_rarity_common`，`m_SpriteAtlas` = `4765312961718699286`
        //   = `0_GeneralUI Atlas`）⇒ `CardPrefab` 那 **6 条 `Image.m_Sprite` 静默落空**（guid 全 0）。
        //   现在改走「先问图集要这一格」：图集纹理与这一格在图集里的矩形都在 `SpriteAtlas` 里
        //   （`m_RenderDataMap`：Card Text=(1491,196,32,32) · Cost Icon=(0,1742,248,244) ·
        //    pedestal=(516,0,248,306) · rarity=(1491,116,84,76)），裁切/编码仍走下面**同一条**路。
        if (tex == null && !TryResolveAtlasSprite(s, out tex, out rect))
            return null;                                // ← 里面已按精灵名**出声**（⛔ 不静默）

        var asset = ImportSpriteFromRect(s.name, tex, rect, s.pixelsPerUnit, s.pivot, s.rect.size, s.border);
        SpriteCache[s] = asset;
        return asset;
    }

    /// <summary>🆕 **2026-10-21（W61）**：`ImportSprite` 的「已知名字 + 图集纹理 + 像素矩形」入口。
    /// 给「原版实例上 `tsa.GetSprite(i)` 是 null、但槽位名已从**原包**里证明」的那一格用
    /// （`KnownUvModuleSprites` / `TryKnownUvModuleSprite`）。
    /// ⚠️ 它和 `ImportSprite` 走的是**同一段**裁切 / 落 PNG / 导入代码 —— ⛔ 别抄第二份（两份迟早不一致）。</summary>
    static Sprite ImportSpriteFromRect(string name, Texture2D tex, Rect rect, float ppu,
                                       Vector2 pivot, Vector2 fullSize, Vector4 border)
    {
        int w = Mathf.RoundToInt(rect.width), h = Mathf.RoundToInt(rect.height);
        if (w <= 0 || h <= 0) { rect = new Rect(0, 0, tex.width, tex.height); w = tex.width; h = tex.height; }

        var readable = ReadableCopy(tex);
        if (readable == null)                       // 🆕 A1325：这几种 return null 原来都是**静默**的
        {
            Debug.LogWarning(EX1 + $"精灵 `{name}` 的纹理 `{tex.name}`（{tex.format}）**读不出像素** ⇒ 这一格留空");
            return null;
        }

        var px = readable.GetPixels(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), w, h);
        var sub = new Texture2D(w, h, TextureFormat.RGBA32, false);
        sub.SetPixels(px);
        sub.Apply();
        byte[] png = null;
        try { png = sub.EncodeToPNG(); } catch { }
        UnityEngine.Object.DestroyImmediate(sub);
        if (readable != tex) UnityEngine.Object.DestroyImmediate(readable);
        if (png == null)
        {
            Debug.LogWarning(EX1 + $"精灵 `{name}` 从 `{tex.name}` 裁 ({rect.x},{rect.y},{w}x{h}) 后"
                           + " **`EncodeToPNG` 返回 null** ⇒ 这一格留空（多半是纹理格式读不了，见 `ReadableCopy` 头部）");
            return null;
        }

        var path = $"{TexDir}/{Sanitize(name)}_sprite.png";
        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), png);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu > 0f ? ppu : 100f;
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
            var sz = fullSize;
            ti.spritePivot = new Vector2(
                sz.x > 0f ? pivot.x / sz.x : 0.5f,
                sz.y > 0f ? pivot.y / sz.y : 0.5f);
            // 🆕 **2026-10-21（`A1325` 第二半）：九宫格边框。** 这一支原来**完全没设 `spriteBorder`**
            //   —— 而 `Card Text smooth background` 的原版 `m_Border = (7,0,7,9)`、`CardPrefab` 里
            //   用到它的 Image 是 `m_Type: 1`（Sliced）⇒ 不补边框的话精灵接上了也会被**拉成一整块**。
            //   ⚠️ Sliced 还要求精灵网格是 **FullRect**（Tight 会把边框区裁掉 ⇒ 九宫格画不对），
            //   所以这里连 `spriteMeshType` 一起改；**只在 border 非零时**动，别的精灵一字不变。
            if (border != Vector4.zero)
            {
                ti.spriteBorder = border;
                var tsx = new TextureImporterSettings();
                ti.ReadTextureSettings(tsx);
                if (tsx.spriteMeshType != SpriteMeshType.FullRect)
                {
                    tsx.spriteMeshType = SpriteMeshType.FullRect;
                    ti.SetTextureSettings(tsx);
                }
                Debug.Log($"[EffectExporter] A1325 精灵 `{name}` 写九宫格 border={border}"
                        + "（网格同时改 FullRect —— Sliced 下 Tight 会把边框区裁掉）");
            }
            ti.SaveAndReimport();
        }
        else
            Debug.LogWarning(EX1 + $"精灵 `{name}` 的 PNG 已写到 `{path}`，但**取不到 `TextureImporter`**"
                           + " ⇒ 它不会被改成 Sprite 类型（下面多半加载成 null）");

        var asset = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (asset == null)
            Debug.LogWarning(EX1 + $"精灵 `{name}` 的 PNG 落到 `{path}` 后**加载不成 `Sprite`** ⇒ 这一格留空");
        return asset;
    }

    // ═══ `A1325` 第二半：**图集精灵**（SpriteAtlas 打包）的解析 ═══════════════════════════════════
    //
    // 为什么得单开一支：`Sprite.texture` 对图集精灵**恒为 null**（`m_RD.texture` 是 `PPtr(pathID = 0)`），
    //   而它的纹理与「这一格在图集里的矩形」只有 `SpriteAtlas` 那边有。按顺序试：
    //   ① 精灵自己的 `m_SpriteAtlas` **PPtr**（序列化字段；bundle 载着 ⇒ 按需解析，不要求图集是「可加载根」）
    //      → `SpriteAtlas.GetSprite(name)` / `GetSprites(...)` 拿**打包后的精灵**（纹理 + textureRect 都对）；
    //   ② ① 取不到图集对象时，扫**已加载的包**里的 `SpriteAtlas`（`m_AtlasTags` 里的名字优先）再走同一套 API；
    //   ③ 那两个 API 给不出这一格时 **无条件**落到 `TryAtlasRectFromMap`：读 `SpriteAtlas.m_RenderDataMap`，
    //      定位顺序 = Hash128（`m_RenderDataKey`）→ 名字表下标（`m_PackedSpriteNamesToIndex`）→
    //      **硬编码兜底表**（`KnownRects_GeneralUI`，只覆盖 `0_GeneralUI Atlas` 那四张，来源见那里的注释）。
    // 🔴 **2026-10-21 实证（`export_cardprefab2.log`）**：bundle 里载入的图集，`GetSprite`/`GetSprites`
    //   **对每一张都返回空**（日志：`已登记图集 25 个`、每个精灵都「取到了但没给出这一格」）⇒ ①② 基本是空的，
    //   真正的活路是 ③。上一版 ③ 的第一句是 `if (SpriteRenderDataKey(s) == null) return false;` ——
    //   Hash128 一读不到就**静默**跳过整条退路（日志里连 `退路走不通` 都没有，就是它），本轮已修。
    // 全失败 ⇒ **点名出声 + 逐层实况**（`diag`）、返回 false（调用方留空）。⛔ 绝不拿别的图/别的格顶上。
    static readonly HashSet<string> AtlasMissLogged = new HashSet<string>();
    static List<UnityEngine.U2D.SpriteAtlas> _atlases;
    static int _atlasScanStamp = -1;

    /// <summary>把一张图集精灵解析成「图集纹理 + 这一格在图集里的像素矩形」。
    /// 成功时打一行 `A1325 图集精灵 …` 说明走的哪条路；失败时**出声**（每个精灵名只吼一次）。</summary>
    static bool TryResolveAtlasSprite(Sprite s, out Texture2D tex, out Rect rect)
    {
        tex = null; rect = default;
        var diag = new System.Text.StringBuilder();

        // ---- ① 精灵自己指的那个图集（`m_SpriteAtlas` 序列化字段，bundle 内 PPtr）----
        var atlas = SerializedFieldRef(s, "m_SpriteAtlas") as UnityEngine.U2D.SpriteAtlas;
        if (atlas != null)
        {
            int sc = -1; try { sc = atlas.spriteCount; } catch { }
            diag.Append($"图集=`{atlas.name}`(spriteCount={sc}); ");
            if (TryPackedSprite(atlas, s, out tex, out rect, out string how1, diag))
            {
                Debug.Log($"[EffectExporter] A1325 图集精灵 `{s.name}` 走 {how1}（图集 `{atlas.name}` · "
                        + $"纹理 `{tex.name}` {tex.width}x{tex.height}）⇒ 裁切 "
                        + $"({rect.x},{rect.y},{rect.width},{rect.height})");
                return true;
            }
            // ③ 退路：`GetSprite`/`GetSprites` 给不出这一格 ⇒ 直接翻 `m_RenderDataMap`
            //    🔴 **无条件走这里**（上一版在 `SpriteRenderDataKey` 读不到时静默 return，四张全落空）
            if (TryAtlasRectFromMap(atlas, s, out tex, out rect, diag))
            {
                Debug.Log($"[EffectExporter] A1325 图集精灵 `{s.name}` **走退路** `m_RenderDataMap`"
                        + $"（图集 `{atlas.name}` · 纹理 `{tex.name}` {tex.width}x{tex.height}）⇒ 裁切 "
                        + $"({rect.x},{rect.y},{rect.width},{rect.height})｜{diag}");
                return true;
            }
        }
        else diag.Append("m_SpriteAtlas=null; ");

        // ---- ② `m_SpriteAtlas` 取不到（或那两条路都没给出这一格）⇒ 扫**已加载的包**里的图集 ----
        //  ⚠️ 这一步会扫 84 个包，**只在上面失败时才走**（happy path 一次都不扫）。
        var tags = AtlasTagsOf(s);
        var cands = new List<UnityEngine.U2D.SpriteAtlas>();
        foreach (var a in LoadedAtlases())
        {
            if (a == null || a == atlas) continue;
            bool tagHit = false;
            foreach (var t in tags) if (a.name == t) tagHit = true;
            if (tagHit) cands.Insert(0, a); else cands.Add(a);       // 同名图集排前面
        }
        diag.Append($"扫包{cands.Count}个; ");
        foreach (var a in cands)
        {
            if (TryPackedSprite(a, s, out tex, out rect, out string how2))
            {
                Debug.Log($"[EffectExporter] A1325 图集精灵 `{s.name}` 走 {how2}（图集 `{a.name}` · 扫包找到 · "
                        + $"纹理 `{tex.name}` {tex.width}x{tex.height}）⇒ 裁切 "
                        + $"({rect.x},{rect.y},{rect.width},{rect.height})");
                return true;
            }
            if (TryAtlasRectFromMap(a, s, out tex, out rect))
            {
                Debug.Log($"[EffectExporter] A1325 图集精灵 `{s.name}` **走退路** `m_RenderDataMap`"
                        + $"（图集 `{a.name}` · 扫包找到 · 纹理 `{tex.name}` {tex.width}x{tex.height}）⇒ 裁切 "
                        + $"({rect.x},{rect.y},{rect.width},{rect.height})");
                return true;
            }
        }

        // ---- ④ 已知图集兜底（🆕 2026-10-21 · **只对表里那四个名字生效**）----
        //  三件 OrkBuff 的骷髅发射器引用的是 `UVModule.sprites`（= `textureSheetAnimation` 的 sprite 列表，
        //  序列化键名就叫 `UVModule`）里的四张精灵，全部来自图集 `Skull Particle icons`。表里是真值
        //  （见 `KnownRects_SkullParticleIcons`）。
        //  🔴 **2026-10-21 就地订正（铁律 5 · W61 现核）**：这里原来写「本 build 上 ①②③ 对这一格
        //     **都给不出来**」—— **不成立**。③ 在 `SpriteRenderDataKey` 那句静默 return 修掉之后就给得出
        //     （`export_listed_1021.log:5574/5596/6387/6409`），三件 prefab 的 8 个槽现在都是真 guid
        //     （W61 报告 §2）。⇒ ④ 只是**再一层兜底**；真正的「null 槽」由 `KnownUvModuleSprites` 那支接。
        if (TryKnownAtlasFallback(s, out tex, out rect)) return true;

        if (AtlasMissLogged.Add(s.name))
            Debug.LogWarning($"[EffectExporter] A1325 图集精灵 `{s.name}` **解析不出来** ⇒ 这一格留空"
                           + $"（`m_SpriteAtlas` {(atlas == null ? "没取到" : "取到了但没给出这一格")}"
                           + $" · `m_AtlasTags` [{string.Join(",", tags)}]"
                           + $" · 已登记图集 {_atlases?.Count ?? 0} 个"
                           + $"）逐层实况【{diag}】"
                           + "—— ⛔ 不拿别的图顶上（那等于静默画错一张）");
        // 🔴 **返回 false 时把两个 out 清干净**（2026-10-21 顺手堵）：调用方那句是
        //   `if (tex == null && !TryResolveAtlasSprite(s, out tex, out rect)) return null;`
        //   —— 「失败却把 tex 留下非空」会让整句短路成 false，接着拿 `rect = default`（0×0）
        //   走 `ImportSprite` 的「rect 非法 ⇒ 改成整张纹理」那一支，**把整张图集导成一张精灵**（静默画错）。
        tex = null; rect = default;
        return false;
    }

    /// <summary>`Sprite.m_AtlasTags`（原版这四张都写着 `["0_GeneralUI Atlas"]`）。读不到返回空表、**不出声**
    /// —— 它只是②那条路的提示，不是判据。</summary>
    static string[] AtlasTagsOf(Sprite s)
    {
        try
        {
            using (var so = new SerializedObject(s))
            {
                var p = so.FindProperty("m_AtlasTags");
                if (p == null || !p.isArray || p.arraySize == 0) return new string[0];
                var r = new string[p.arraySize];
                for (int i = 0; i < r.Length; i++)
                {
                    var e = p.GetArrayElementAtIndex(i);
                    r[i] = e.propertyType == SerializedPropertyType.String ? e.stringValue : "";
                }
                return r;
            }
        }
        catch { return new string[0]; }
    }

    /// <summary>已加载包里的全部 `SpriteAtlas`（**惰性扫一次**，按 `LoadedPacks` 的规模失效）。
    /// ⚠️ 两条取法都要：这个 build 的 `LoadAllAssets&lt;T&gt;()` 在某些包上**只吐可加载根**，
    ///   而 `GetAllAssetNames()` 吐的是**容器键** ⇒ 按容器键 `LoadAsset&lt;SpriteAtlas&gt;` 再兜一遍。</summary>
    static List<UnityEngine.U2D.SpriteAtlas> LoadedAtlases()
    {
        if (_atlases != null && _atlasScanStamp == LoadedPacks.Count && _atlases.All(a => a != null))
            return _atlases;
        _atlases = new List<UnityEngine.U2D.SpriteAtlas>();
        _atlasScanStamp = LoadedPacks.Count;
        int packs = 0, viaAll = 0, viaKey = 0;
        foreach (var kv in LoadedPacks)
        {
            var b = kv.Value;
            if (b == null) continue;
            packs++;
            int before = _atlases.Count;
            try
            {
                foreach (var a in b.LoadAllAssets<UnityEngine.U2D.SpriteAtlas>())
                    if (a != null) _atlases.Add(a);
            }
            catch (Exception e)
            {
                Debug.LogWarning(EX1 + $"`{kv.Key}` 的 `LoadAllAssets<SpriteAtlas>()` 抛了：{e.GetType().Name}: {e.Message}");
            }
            if (_atlases.Count > before) { viaAll++; continue; }
            // 兜底：按容器键逐个试（只在 `LoadAllAssets` 没捞到时才走，慢）
            string[] names = null;
            try { names = b.GetAllAssetNames(); } catch { }
            if (names == null) continue;
            foreach (var n in names)
            {
                try
                {
                    var a = b.LoadAsset<UnityEngine.U2D.SpriteAtlas>(n);
                    if (a != null && !_atlases.Contains(a)) { _atlases.Add(a); viaKey++; }
                }
                catch { }
            }
        }
        Debug.Log($"[EffectExporter] A1325 图集登记：{_atlases.Count} 个 `SpriteAtlas`"
                + $"（扫 {packs} 个包 · `LoadAllAssets` 命中 {viaAll} 个包 · 按容器键补 {viaKey} 个）");
        return _atlases;
    }

    /// <summary>问图集要「这一格的打包精灵」，再把它换算成（图集纹理 + 图集内矩形）。
    /// 两条 API 都试：`GetSprite(name)`（按名字取克隆）；不行再用
    /// `GetSprites(...)`（**它把精灵克隆填进你给的数组、返回填了几个**，所以要自己按名字挑回来）。
    /// ⚠️ 2026-10-21 实测（`export_cardprefab2.log`）：**bundle 里载入的图集这两条都返回空/不可用**
    ///   ⇒ 本函数返回 false 是常态，真正的兜底在同族的 `TryAtlasRectFromMap`。每次结果都写进 `diag`。</summary>
    static bool TryPackedSprite(UnityEngine.U2D.SpriteAtlas atlas, Sprite s,
                                out Texture2D tex, out Rect rect, out string how,
                                System.Text.StringBuilder diag = null)
    {
        tex = null; rect = default; how = null;
        Sprite hit = null;
        try { hit = atlas.GetSprite(s.name); }
        catch (Exception e)
        {
            Debug.LogWarning(EX1 + $"图集 `{atlas.name}`.GetSprite(`{s.name}`) 抛了：{e.GetType().Name}: {e.Message}");
            diag?.Append($"GetSprite抛{e.GetType().Name}; ");
        }
        diag?.Append($"GetSprite→{DescribePacked(hit)}; ");
        if (UsablePacked(hit, out tex, out rect)) { how = "`GetSprite`"; return true; }
        tex = null; rect = default;

        hit = null;
        try
        {
            // ⚠️ `GetSprites` 的语义是**「把图集里的精灵克隆一份，填进你给的数组」**（返回填了几个），
            //    不是「返回打包精灵」⇒ 传进去的数组是**输出**，要自己按名字挑回来。
            var one = new Sprite[8];
            atlas.GetSprites(one, s.name);
            for (int i = 0; i < one.Length; i++)
                if (one[i] != null && one[i].name == s.name) { hit = one[i]; break; }
            if (hit == null)
            {
                var all = new Sprite[Mathf.Max(1, atlas.spriteCount)];
                int n = atlas.GetSprites(all);
                diag?.Append($"GetSprites→填{n}/{all.Length}(spriteCount={atlas.spriteCount}); ");
                for (int i = 0; i < n && i < all.Length; i++)
                    if (all[i] != null && all[i].name == s.name) { hit = all[i]; break; }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(EX1 + $"图集 `{atlas.name}`.GetSprites(`{s.name}`) 抛了：{e.GetType().Name}: {e.Message}");
            diag?.Append($"GetSprites抛{e.GetType().Name}; ");
        }
        diag?.Append($"GetSprites选中→{DescribePacked(hit)}; ");
        if (!UsablePacked(hit, out tex, out rect)) { tex = null; rect = default; return false; }

        // 防御：矩形必须落在纹理里，否则宁可当失败也别裁一块空白出来
        if (rect.x < 0f || rect.y < 0f
            || rect.x + rect.width > tex.width + 0.5f || rect.y + rect.height > tex.height + 0.5f)
        {
            Debug.LogWarning(EX1 + $"图集 `{atlas.name}` 给 `{s.name}` 的矩形 "
                           + $"({rect.x},{rect.y},{rect.width},{rect.height}) **不在纹理 `{tex.name}` "
                           + $"{tex.width}x{tex.height} 里** ⇒ 这一格按失败处理（⛔ 不裁一块空白顶上）");
            tex = null; rect = default; return false;
        }
        how = "`GetSprites`";
        return true;
    }

    static bool UsablePacked(Sprite hit, out Texture2D tex, out Rect rect)
    {
        tex = null; rect = default;
        if (hit == null) return false;
        tex = hit.texture as Texture2D;
        if (tex == null) return false;
        rect = hit.textureRect;
        return rect.width > 0f && rect.height > 0f;
    }

    /// <summary>**退路**：直接读图集的 `m_RenderDataMap`（序列化里是 `[Key, SpriteAtlasData]` 表），
    /// 拿这一格的 `texture`（图集纹理）+ `textureRect`（在图集里的矩形）。**三层定位，逐层降级**：
    ///   ① `Sprite.m_RenderDataKey`（Hash128）== 记录 Key 里的 Hash128 —— 两边是同一个值；
    ///   ② 读不到 Hash128 时改用**下标**：`m_PackedSpriteNamesToIndex[i] == 精灵名` ⇒ 表里第 i 条
    ///      （解包态实测两者同序：`Card Text smooth background` = 第 110 条，rect = (1491,196,32,32) ✔）；
    ///   ③ 还定不到位 ⇒ 从**任意一条**记录里捞图集纹理（同一张图集的每一格共用一个 `texture`），
    ///      再配 `KnownRects_GeneralUI` 那张**从解包真值抄来的**硬编码表（⛔ 不是通用算法，见那里的注释）。
    /// 🔴 **2026-10-21 修**：上一版这里第一句是 `if (key == null) return false;` —— Hash128 一读不到就
    ///    **静默整条退路不跑**（`export_cardprefab2.log` 里四张全落空、却一条 `退路走不通` 都没有，就是它）。
    ///    现在**任何**失败都写进 `diag` / 打 Warning，且 ②③ 不依赖 Hash128。</summary>
    static bool TryAtlasRectFromMap(UnityEngine.U2D.SpriteAtlas atlas, Sprite s,
                                    out Texture2D tex, out Rect rect,
                                    System.Text.StringBuilder diag = null)
    {
        tex = null; rect = default;
        try
        {
            using (var so = new SerializedObject(atlas))
            {
                var map = so.FindProperty("m_RenderDataMap");
                if (map == null)
                {
                    diag?.Append("m_RenderDataMap=找不到; ");
                    Debug.LogWarning(EX1 + $"图集 `{atlas.name}` 上找不到 `m_RenderDataMap` ⇒ 退路 ①②③ 全走不通");
                    return false;
                }
                if (!map.isArray || map.arraySize == 0)
                {
                    // 不是数组 ⇒ ①② 的下标定位做不了，但 ③ 还能用（纹理可以从整棵子树里捞一张）
                    diag?.Append($"map非数组(type={map.propertyType}·isArray={map.isArray}); ");
                }
                else
                    diag?.Append($"map={map.arraySize}条; ");
                bool mapArr = map.isArray && map.arraySize > 0;

                // ---- ① 名字 → 下标（`m_PackedSpriteNamesToIndex`）：**先走这条**，它是一次查表；
                //      Hash128 那条要逐条扫整张表（139 条 × 25 个图集 = 很贵），而且它未必读得到 ----
                string key = SpriteRenderDataKey(s);
                diag?.Append($"RenderDataKey={(key ?? "null")}; ");
                int idx = -1;
                if (mapArr)
                {
                    idx = AtlasNameIndex(so, s.name);
                    if (idx >= 0) diag?.Append($"名字表idx={idx}; ");
                }

                // ---- ② Hash128 兜底（名字表读不出来时才扫）----
                if (mapArr && idx < 0 && key != null)
                {
                    for (int i = 0; i < map.arraySize; i++)
                    {
                        bool hh = false; Texture2D t0 = null; Rect r0 = default;
                        ScanRenderRecord(map.GetArrayElementAtIndex(i), key, ref hh, ref t0, ref r0);
                        if (hh) { idx = i; break; }
                    }
                    diag?.Append($"Hash128命中idx={idx}; ");
                }

                if (mapArr && idx >= 0 && idx < map.arraySize)
                {
                    Texture2D t1 = null; Rect r1 = default;
                    ReadRenderRecord(map, idx, out t1, out r1);        // 取不到也算，值仍留着
                    if (t1 == null) t1 = FirstMapTexture(map);          // 那条里没 `texture` ⇒ 从别条捞一张同款
                    if (t1 != null && InAtlasBounds(t1, r1))
                    {
                        tex = t1; rect = r1;
                        return true;
                    }
                    diag?.Append($"第{idx}条读不出(texture={(t1 == null ? "null" : t1.name)}"
                               + $" rect={r1.width}x{r1.height}); ");
                }

                // ---- ③ 硬编码兜底（纹理从任意一条记录里捞，全表共用一张）----
                var kt = KnownAtlasRect(atlas.name, s.name);
                if (kt.HasValue)
                {
                    var any = FirstMapTexture(map);
                    diag?.Append($"兜底表rect={kt.Value.width}x{kt.Value.height}·纹理="
                               + $"{(any == null ? "null" : any.name)}; ");
                    if (any != null && InAtlasBounds(any, kt.Value))
                    {
                        tex = any; rect = kt.Value;
                        Debug.LogWarning($"[EffectExporter] A1325 图集精灵 `{s.name}` **用的是【硬编码兜底表】**"
                                       + $"（从解包 `SpriteAtlas.m_RenderDataMap` 抄的真值，⛔ 不是算出来的）："
                                       + $"rect=({kt.Value.x},{kt.Value.y},{kt.Value.width},{kt.Value.height})"
                                       + $" · 纹理 `{any.name}` {any.width}x{any.height}");
                        return true;
                    }
                }
                else diag?.Append("无兜底表项; ");
            }
        }
        catch (Exception e)
        {
            diag?.Append($"读map抛{e.GetType().Name}; ");
            Debug.LogWarning(EX1 + $"读 `{atlas.name}.m_RenderDataMap` 失败：{e.GetType().Name}: {e.Message}");
        }
        return false;
    }

    /// <summary>矩形必须在纹理里（⛔ 宁可不导，也别裁一块图外空白出来）。</summary>
    static bool InAtlasBounds(Texture2D t, Rect r)
        => t != null && r.width > 0f && r.height > 0f
           && r.x >= 0f && r.y >= 0f
           && r.x + r.width <= t.width + 0.5f && r.y + r.height <= t.height + 0.5f;

    /// <summary>从 `m_RenderDataMap` 的第 `i` 条里抠出 `texture` + `textureRect`（不按 Hash128 匹配）。</summary>
    static bool ReadRenderRecord(SerializedProperty map, int i, out Texture2D tex, out Rect rect)
    {
        tex = null; rect = default;
        if (map == null || !map.isArray || i < 0 || i >= map.arraySize) return false;
        bool hh = false;
        ScanRenderRecord(map.GetArrayElementAtIndex(i), null, ref hh, ref tex, ref rect);
        return tex != null && rect.width > 0f && rect.height > 0f;
    }

    /// <summary>表里任意一条的 `texture`（同一张图集的所有格共用同一张纹理 ⇒ 前几条里拿一条就够）。
    /// 表**不是数组**时退到「整棵子树里找第一张 `texture`」—— 只为了给 ③ 兜底表配一张纹理。</summary>
    static Texture2D FirstMapTexture(SerializedProperty map, int probe = 4)
    {
        if (map == null) return null;
        if (map.isArray && map.arraySize > 0)
        {
            int n = Mathf.Min(map.arraySize, Mathf.Max(1, probe));
            for (int i = 0; i < n; i++)
            {
                bool hh = false; Texture2D t = null; Rect r = default;
                ScanRenderRecord(map.GetArrayElementAtIndex(i), null, ref hh, ref t, ref r);
                if (t != null) return t;
            }
            return null;
        }
        bool hh2 = false; Texture2D t2 = null; Rect r2 = default;
        ScanRenderRecord(map, null, ref hh2, ref t2, ref r2);
        return t2;
    }

    /// <summary>`SpriteAtlas.m_PackedSpriteNamesToIndex` 里这张精灵的下标；读不出返回 −1。
    /// 解包态实测：它是**与 `m_RenderDataMap` 同序的名字表**（139/139 一一对应）。</summary>
    static int AtlasNameIndex(SerializedObject so, string name)
    {
        try
        {
            var p = so.FindProperty("m_PackedSpriteNamesToIndex");
            if (p == null) return -1;
            if (!p.isArray) return -1;
            for (int i = 0; i < p.arraySize; i++)
            {
                var e = p.GetArrayElementAtIndex(i);
                if (e.propertyType == SerializedPropertyType.String)
                {
                    if (e.stringValue == name) return i;
                    continue;
                }
                // `map<string,int>` 被摊成「每条一对」的情形：找 string 子字段 + int 子字段
                string str = null; int val = -1;
                ScanNamePair(e, ref str, ref val);
                if (str == name && val >= 0) return val;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(EX1 + $"读 `m_PackedSpriteNamesToIndex` 失败：{e.GetType().Name}: {e.Message}");
        }
        return -1;
    }

    static void ScanNamePair(SerializedProperty rec, ref string str, ref int val)
    {
        var end = rec.GetEndProperty(true);
        var p = rec.Copy();
        InspectNamePairField(p, ref str, ref val);
        while (p.Next(true) && !SerializedProperty.EqualContents(p, end))
            InspectNamePairField(p, ref str, ref val);
    }

    static void InspectNamePairField(SerializedProperty p, ref string str, ref int val)
    {
        try
        {
            if (p.propertyType == SerializedPropertyType.String && str == null) str = p.stringValue;
            else if (p.propertyType == SerializedPropertyType.Integer && val < 0) val = p.intValue;
        }
        catch { }
    }

    // 🔴 **从解包真值抄来的硬编码兜底（2026-10-21）** —— ⛔ **它不是通用算法**，只是这四张的实测真值：
    //    来源 = `d:/2/新解包资源/assets_full/bundle_duplicateassetisolation_assets_all/SpriteAtlas/
    //    SpriteAtlas_4765312961718699286.json`（`0_GeneralUI Atlas`），按 `m_PackedSpriteNamesToIndex`
    //    下标与 `m_RenderDataMap[i].textureRect` 逐条对上（同批已用 `m_PackedSprites` 的 pathID 与
    //    `CardPrefab` 里那 4 个 `m_Sprite` fileID 交叉验证过）。
    //    **只在**「图集名 = `0_GeneralUI Atlas`」且 ①② 都没把这格读出来时用；用了一定打 Warning 说明。
    static readonly Dictionary<string, Rect> KnownRects_GeneralUI = new Dictionary<string, Rect>
    {
        { "Card Text smooth background",   new Rect(1491f, 196f,  32f,  32f) },
        { "Card Frame Cost Icon",          new Rect(0f,    1742f, 248f, 244f) },
        { "pedestal_icon_armor",           new Rect(516f,  0f,    248f, 306f) },
        { "1_40k_cardframe_rarity_common", new Rect(1491f, 116f,  84f,  76f) },
    };

    static Rect? KnownAtlasRect(string atlasName, string spriteName)
    {
        if (!string.Equals(atlasName, "0_GeneralUI Atlas", StringComparison.Ordinal)) return null;
        return KnownRects_GeneralUI.TryGetValue(spriteName, out var r) ? r : (Rect?)null;
    }

    // 🔴 **`Skull Particle icons` 图集的硬编码兜底（🆕 2026-10-21）** —— 与上面 `KnownRects_GeneralUI` 同族，
    //    差别是这一张**按精灵名索引**、**不要求图集对象解析得到**：受害的三件 prefab
    //    （`OrkBuff_Recall` / `OrkBuffGreenWaaagh` / `OrkBuff_Snakebitez_2_Intense`）连图集那一步都拿不到这一格。
    //    真值来源 = 该图集的 `m_RenderDataMap`（pathID 1583738769577704870），纹理
    //    `sactx-0-1024x512-BC7-Skull Particle icons`；⛔ **不是算出来的**，别改数。
    //    ⚠️ **只对这四个名字生效**（`TryKnownAtlasFallback` 先查名字，表外直接 false）——
    //       ⛔ 别把它扩成「任何图集精灵都套一张表」（那等于静默画错一张）。
    //    🔴 **2026-10-21 就地订正（铁律 5 · W61 现核）**：本段原来写「连图集那一步都拿不到这一格」
    //       —— **不成立**。同一天修掉 `TryAtlasRectFromMap` 里 `SpriteRenderDataKey(s) == null` 那句
    //       **静默 return** 之后，③（`m_RenderDataMap` → `m_PackedSpriteNamesToIndex`）当场就能给出
    //       这四格；`export_listed_1021.log:5574/5596/6387/6409` 四条 `名字表idx=4/3/1/0` 就是它，
    //       导出的 PNG 与原图集逐像素相同（W61 报告 §2）。⇒ 本表（④）现在只是**兜底**，正常路径不走它。
    static readonly Dictionary<string, Rect> KnownRects_SkullParticleIcons = new Dictionary<string, Rect>
    {
        { "Skulltop",  new Rect(638.08f, 259.01f, 175.91f, 178.91f) },
        { "Skull_jaw", new Rect(647.02f,   0.08f, 232.91f, 222.91f) },
        { "BW_Trim2",  new Rect(  0.08f,   0.08f, 226.85f, 333.87f) },
        { "BW_Trim3",  new Rect(437.08f,  49.08f, 211.85f, 276.85f) },
    };
    /// <summary>`KnownRects_SkullParticleIcons` 那四格所在图集的纹理名（解包真名）。</summary>
    const string KnownSkullAtlasTexture = "sactx-0-1024x512-BC7-Skull Particle icons";

    // ═══ 🆕 **2026-10-21（W61）：`UVModule.sprites` 的「已知槽位」表** ═══════════════════════════
    //
    // **只覆盖 OrkBuff 家族这五件的骷髅发射器（16 个槽）—— 表外一律不猜。** 触发条件见 `Export()` 里那段：
    //   只有 `tsa.GetSprite(i) == null`（原版实例上这一格读不出来）**且** `spriteCount == 1` 才会用它。
    //
    // 真值来源（逐槽在原包里核过，**不是按名字猜左右**）：
    //   `d:/2/10.13游戏/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/battleprefabs_vfxandmisc_assets_all.bundle`
    //   → 每个 `ParticleSystem` 的 `UVModule.sprites[i].sprite`（PPtr）→ 解到**同一个 CAB**
    //   （`CAB-d47690319398b604c3bb5a35a8ed2499`）里的 `Sprite`，读它的 `m_Name`。
    //   十六个槽**每个只有一张精灵**（`spriteCount == 1`）：
    //     · `OrkBuff_Recall`                `Sparks/Skulls` = Skulltop · `Sparks/Jaw` = Skull_jaw
    //     · `OrkBuffGreenWaaagh`            `Border Glow/Skulls` = Skulltop ·
    //                                       `Border Glow/Skulls/Jaw` = Skull_jaw ·
    //                                       `Border Glow/Skulls/Left` = **BW_Trim3** ·
    //                                       `Border Glow/Skulls/Right` = **BW_Trim2**
    //     · `OrkBuff_Snakebitez_2_Intense`  `Sparks/Skulls` = Skulltop · `Sparks/Skulls/Jaw` = Skull_jaw
    //     · **同族另外两件**（2026-10-21 W61 现读：它们的 prefab 还是 **10-10 16:08** 那一版、
    //       `sprites` 仍是 `{fileID: <原包 pathID>, guid: 0}` 的伪引用 ⇒ 重导即修，本表同时兜底）：
    //       `OrkBuff_KrumpDaGitz`            `Border Glow/Skulls` = Skulltop ·
    //                                        `Border Glow/Skulls/{Jaw,Left,Right}` = Skull_jaw / BW_Trim3 / BW_Trim2
    //       `OrkBuff_Snakebitez_2`           `Sparks/Skulls` = Skulltop ·
    //                                        `Sparks/Skulls/{Jaw,Left,Right}` = Skull_jaw / BW_Trim3 / BW_Trim2
    //   ⚠️ `Left` = **BW_Trim3**、`Right` = **BW_Trim2**（原包就是这样，⛔ 别按「左小右大」去猜）。
    //   ⚠️ 键 = `prefab 根名 + "/" + 相对根的 Transform 路径`（`TransformRelPath`）。
    //   🔴 这十六条是**唯一**的映射关系；要加新条目，先把原包那一条的 PPtr 解出来再写。
    static readonly Dictionary<string, string> KnownUvModuleSprites = new Dictionary<string, string>
    {
        { "OrkBuff_Recall/Sparks/Skulls",                        "Skulltop"  },
        { "OrkBuff_Recall/Sparks/Jaw",                           "Skull_jaw" },
        { "OrkBuffGreenWaaagh/Border Glow/Skulls",               "Skulltop"  },
        { "OrkBuffGreenWaaagh/Border Glow/Skulls/Jaw",           "Skull_jaw" },
        { "OrkBuffGreenWaaagh/Border Glow/Skulls/Left",          "BW_Trim3"  },
        { "OrkBuffGreenWaaagh/Border Glow/Skulls/Right",         "BW_Trim2"  },
        { "OrkBuff_Snakebitez_2_Intense/Sparks/Skulls",          "Skulltop"  },
        { "OrkBuff_Snakebitez_2_Intense/Sparks/Skulls/Jaw",      "Skull_jaw" },
        { "OrkBuff_KrumpDaGitz/Border Glow/Skulls",              "Skulltop"  },
        { "OrkBuff_KrumpDaGitz/Border Glow/Skulls/Jaw",          "Skull_jaw" },
        { "OrkBuff_KrumpDaGitz/Border Glow/Skulls/Left",         "BW_Trim3"  },
        { "OrkBuff_KrumpDaGitz/Border Glow/Skulls/Right",        "BW_Trim2"  },
        { "OrkBuff_Snakebitez_2/Sparks/Skulls",                  "Skulltop"  },
        { "OrkBuff_Snakebitez_2/Sparks/Skulls/Jaw",              "Skull_jaw" },
        { "OrkBuff_Snakebitez_2/Sparks/Skulls/Left",             "BW_Trim3"  },
        { "OrkBuff_Snakebitez_2/Sparks/Skulls/Right",            "BW_Trim2"  },
    };

    /// <summary>`t` 相对 `root` 的 Transform 路径（`/` 连接，**不含** `root` 自己）。</summary>
    static string TransformRelPath(Transform root, Transform t)
    {
        var seg = new List<string>();
        for (var cur = t; cur != null && cur != root; cur = cur.parent) seg.Add(cur.name);
        seg.Reverse();
        return string.Join("/", seg);
    }

    /// <summary>null 槽兜底：`prefab 根名/相对路径` 命中 `KnownUvModuleSprites` ⇒ 给出精灵名 + 图集矩形
    /// （矩形来自 `KnownRects_SkullParticleIcons`）。**只认表里那八条**，表外直接 false。</summary>
    static bool TryKnownUvModuleSprite(GameObject root, ParticleSystem ps, out string spriteName, out Rect rect)
    {
        spriteName = null; rect = default;
        if (root == null || ps == null) return false;
        string key = root.name + "/" + TransformRelPath(root.transform, ps.transform);
        if (!KnownUvModuleSprites.TryGetValue(key, out spriteName)) return false;
        if (KnownRects_SkullParticleIcons.TryGetValue(spriteName, out rect)) return true;
        spriteName = null; rect = default;
        return false;   // 表里写了名字但矩形表没有 ⇒ 当成不命中（⛔ 不编一个 rect）
    }

    /// <summary>④ 已知图集兜底：精灵名命中 `KnownRects_SkullParticleIcons` ⇒ 按名字找那张图集纹理、
    /// 配上表里的 rect。**只认表里那四个名字**；命中、落空都出声（⛔ 不静默）。</summary>
    static bool TryKnownAtlasFallback(Sprite s, out Texture2D tex, out Rect rect)
    {
        tex = null; rect = default;
        if (s == null || s.name == null) return false;
        if (!KnownRects_SkullParticleIcons.TryGetValue(s.name, out var r)) return false;

        // ⚠️ 只往局部变量里放，**成功才回填 out**（失败时 out 必须还是 null —— 见 `TryResolveAtlasSprite`
        //    末尾那条注释：失败却留下非空 tex 会让调用方拿整张纹理顶上去）
        var t = FindTextureInPacks(KnownSkullAtlasTexture);
        if (t == null)
        {
            Debug.LogWarning(EX1 + $"走已知图集兜底：`{s.name}` 命中兜底表，但图集纹理 "
                           + $"`{KnownSkullAtlasTexture}` 在内存与已加载的包里**都取不到** ⇒ 这一格留空");
            return false;
        }
        if (!InAtlasBounds(t, r))
        {
            Debug.LogWarning(EX1 + $"走已知图集兜底：`{s.name}` 的 rect ({r.x},{r.y},{r.width},{r.height}) "
                           + $"**落在纹理 `{t.name}`（{t.width}x{t.height}）之外** ⇒ 这一格留空"
                           + "（⛔ 不裁图外空白）");
            return false;
        }
        tex = t; rect = r;
        Debug.Log($"[EffectExporter] A1325 图集精灵 `{s.name}` **走已知图集兜底：{s.name}**"
                + $"（纹理 `{t.name}` {t.width}x{t.height} · 真值抄自 `m_RenderDataMap`）⇒ 裁切 "
                + $"({r.x},{r.y},{r.width},{r.height})");
        return true;
    }

    static readonly Dictionary<string, Texture2D> TexByNameCache = new Dictionary<string, Texture2D>();

    /// <summary>按名字找一张**已加载**的纹理：先看内存里的对象（`Resources.FindObjectsOfTypeAll`，
    /// 不再加载任何东西），再逐包 `LoadAllAssets&lt;Texture2D&gt;()` 兜底。
    /// ⚠️ 只给「已知图集兜底」用（表里就四个名字，且只在正常解析失败时才走到）—— ⛔ 别放进热路径。</summary>
    static Texture2D FindTextureInPacks(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (TexByNameCache.TryGetValue(name, out var cached) && cached != null) return cached;

        foreach (var t in Resources.FindObjectsOfTypeAll<Texture2D>())
            if (t != null && t.name == name) { TexByNameCache[name] = t; return t; }

        foreach (var kv in LoadedPacks)
        {
            var b = kv.Value;
            if (b == null) continue;
            Texture2D[] all = null;
            try { all = b.LoadAllAssets<Texture2D>(); }
            catch (Exception e)
            {
                Debug.LogWarning(EX1 + $"按名字找纹理时 `{kv.Key}` 的 `LoadAllAssets<Texture2D>()` 抛了："
                               + $"{e.GetType().Name}: {e.Message}");
                continue;
            }
            if (all == null) continue;
            foreach (var t in all)
                if (t != null && t.name == name) { TexByNameCache[name] = t; return t; }
        }
        return null;
    }

    /// <summary>一句话描述某个打包精灵（写 `diag` 用）：名字 / 纹理 / 矩形大小。</summary>
    static string DescribePacked(Sprite x)
    {
        if (x == null) return "null";
        var t = x.texture;
        var rr = x.textureRect;
        return $"`{x.name}`(texture={(t == null ? "null" : t.name + " " + t.width + "x" + t.height)}"
             + $" · textureRect={rr.width}x{rr.height})";
    }

    /// <summary>在 `m_RenderDataMap` 的一条记录里找三样东西：Hash128（匹配用，`key=null` 时不用）、
    /// `texture`、`textureRect`。⚠️ 用 `Next(true)`/`GetEndProperty(true)`（**含隐藏字段**）——
    /// 这些是原生结构，`m_Hash` 之类的子字段不一定是「可见」的。</summary>
    static void ScanRenderRecord(SerializedProperty rec, string key,
                                 ref bool hashHit, ref Texture2D tex, ref Rect rect)
    {
        var end = rec.GetEndProperty(true);
        var p = rec.Copy();
        InspectRenderField(p, key, ref hashHit, ref tex, ref rect);
        while (p.Next(true) && !SerializedProperty.EqualContents(p, end))
            InspectRenderField(p, key, ref hashHit, ref tex, ref rect);
    }

    static void InspectRenderField(SerializedProperty p, string key,
                                   ref bool hashHit, ref Texture2D tex, ref Rect rect)
    {
        try
        {
            if (key != null && p.propertyType == SerializedPropertyType.Hash128)
            {
                if (p.hash128Value.ToString() == key) hashHit = true;
                return;
            }
            // `SpriteAtlasData.texture` / `.alphaTexture` —— 只认 `texture` 那一格（alphaTexture 同形时是 0）
            if (p.propertyType == SerializedPropertyType.ObjectReference && p.name == "texture" && tex == null)
            { tex = p.objectReferenceValue as Texture2D; return; }
            if (p.propertyType == SerializedPropertyType.Rect && p.name == "textureRect" && rect.width <= 0f)
                rect = p.rectValue;
        }
        catch { }
    }

    /// <summary>`Sprite.m_RenderDataKey` 里那个 Hash128（`"…"` 字符串；读不到返回 null）。
    /// 它和 `SpriteAtlas.m_RenderDataMap` 的 Key 是**同一个值** —— 退路① 的唯一依据。
    /// ⚠️ **读不到不等于退路作废**：上一版就是在这里 `return null` 之后被调用方**静默跳过**整条退路
    /// （`export_cardprefab2.log` 四张全空、一条声都没有）。现在读不到只会写进 `diag`，退路改走 ②③。</summary>
    static string SpriteRenderDataKey(Sprite s)
    {
        try
        {
            using (var so = new SerializedObject(s))
            {
                var p = so.FindProperty("m_RenderDataKey");
                if (p == null) return null;
                if (p.propertyType == SerializedPropertyType.Hash128) return p.hash128Value.ToString();
                var end = p.GetEndProperty(true);
                var it = p.Copy();
                while (it.Next(true) && !SerializedProperty.EqualContents(it, end))
                    if (it.propertyType == SerializedPropertyType.Hash128) return it.hash128Value.ToString();
            }
        }
        catch { }
        return null;
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
        // 🔴 `A845(b)` **一源一份** —— 与 `ImportMaterial` 走**同一个** `ResolveVariantPath`（判据见那里）
        var gFields = MeshFields(m);
        var gMoved = new List<string>();
        bool collided;
        string path = ResolveVariantPath(MeshVariants, MeshDir, ".asset", stem, gFields, gMoved, out collided);
        if (collided) WarnCollision("网格", stem, MeshVariants[stem], gFields, m.name, gMoved);
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
        // 🆕 2026-10-19（V2 / T10）：**把 `_state` 曲线的脚本引用接回工程脚本**。
        //   为什么必须接：bundle 里那条 legacy clip 的 `_state` 曲线指向原版
        //   `StoryProgramming.VATGPUPlayer`（`script.fileID = 3946243950355606049`）⇒
        //   `Instantiate` 之后那条 PPtr 的 guid 变成 `000…0`（**断链**）。legacy `Animation` 是按
        //   `script` 的**类型**解析绑定的 ⇒ 曲线**永远绑不上**，`_state` 停在序列化值 0；
        //   而 `WarpforgeVatDriver.ClipIsUsable()` 又因为「Animation 上有 clip」而不走内置兜底曲线
        //   ⇒ **两条路都不动**（网格静止在第 0 行）。
        //   ⚠️ 这里只接 `_state` 一条：映射有唯一判据（原版 `VATGPUPlayer` 的 11 个属性名
        //      ⇄ `WarpforgeVatDriver` 逐条对过）。其余 `classID:114` 曲线
        //      （`lutBlend` / `bloomIntensity` / `betweenLUTsBlend` / `customLUT2` …）的宿主脚本
        //      还没逐条坐实，**本轮不接**（账 → `资料/普查产出_1019/V2_报告.md`）。
        //
        // 🆕 2026-10-19（V3 / 批次 3）：**其余 38 条 classID:114 已逐条坐实宿主**（普查 →
        //   `资料/普查产出_1019/V3_报告.md` §一）。能接的两族在这里一并接上：
        //     · `AnimFXModulePostProcess`（26 条 float）→ `WFModulePostProcess`（字段逐条同名）；
        //     · `TMPro.TextMeshPro`（4 条 `m_fontColor.*`）→ 工程里那件 TMP 脚本（按类型找）。
        //   接不了的三族（`AnimateUIMaterialProperty` / `NestedFadeGroup` /
        //   `ParticleForceForceFieldController`）**故意留断**：工程里没有对应组件类，
        //   接上去也绑不到任何东西（账在同报告 §一·3）。
        //   `customLUT2` 那条是 `m_PPtrCurves`（不在 `m_FloatCurves`），且值是 `LUT Red Hell`、
        //   与序列化字段同值（解析：UnityPy 读 pathID -6000566073584760436 = `LUT Red Hell` 256×16）
        //   ⇒ **接不接都没有行为差别**，helper 也只处理 float 曲线。
        RemapDeadScriptCurves(asst, new[] { "_state" }, "Assets/CardPresentation/Battle/WarpforgeVatDriver.cs");
        RemapDeadScriptCurves(asst, PostProcessCurveAttributes, "Assets/WarpforgeVFX/Runtime/WFModulePostProcess.cs");
        RemapDeadScriptCurves(asst, FontColorCurveAttributes, typeof(TMPro.TextMeshPro));
        RemapDeadScriptCurves(asst, ForceFieldCurveAttributes,
                              "Assets/WarpforgeVFX/Runtime/WarpforgeParticleForceFieldController.cs");
        ClipCache[src] = asst;
        AssetDatabase.SaveAssets();
        return asst;
    }

    /// <summary>把 `m_FloatCurves` 里**脚本引用已经断链**（bundle 里原版 MonoScript 落不到工程 ⇒ guid 0）
    /// 的那条曲线的 `script` 接回给定工程脚本。
    ///
    /// 只在该 `attribute` 的曲线**存在**、且当前 `script` **解析为空**时动手（已经能解析的一律不碰）；
    /// 找不到脚本 / 序列化属性取不到都**出声**，⛔ 不静默。
    /// 判据与影响面 → `资料/普查产出_1019/V2_报告.md`（T10）＋ `V3_报告.md` §一。</summary>
    static void RemapDeadScriptCurves(AnimationClip clip, string[] attributes, string scriptPath)
    {
        if (clip == null) return;
        var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
        if (mono == null)
        {
            Debug.LogWarning(EX1 + $"曲线脚本接回：找不到 `{scriptPath}` ⇒ `{clip.name}` 的 "
                                  + $"[{string.Join(", ", attributes)}] 曲线保持断链");
            return;
        }
        RemapDeadScriptCurves(clip, attributes, mono);
    }

    /// <summary>同上的**按类型**版本：用 `AssetDatabase` 按类型找那件 MonoScript
    /// （TMP 这类脚本在 package 里，路径会被包版本号改掉，⛔ 别写死路径）。</summary>
    static void RemapDeadScriptCurves(AnimationClip clip, string[] attributes, Type scriptType)
    {
        if (clip == null) return;
        MonoScript mono = null;
        foreach (var guid in AssetDatabase.FindAssets(scriptType.Name + " t:MonoScript"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(p);
            if (ms != null && ms.GetClass() == scriptType) { mono = ms; break; }
        }
        if (mono == null)
        {
            Debug.LogWarning(EX1 + $"曲线脚本接回：按类型 `{scriptType.FullName}` 找不到 MonoScript ⇒ "
                                  + $"`{clip.name}` 的 [{string.Join(", ", attributes)}] 曲线保持断链");
            return;
        }
        RemapDeadScriptCurves(clip, attributes, mono);
    }

    /// <summary>🆕 2026-10-19（V3）：见上两条。本方法是落地那一份（`SerializedObject` 改 `script`）。</summary>
    static void RemapDeadScriptCurves(AnimationClip clip, string[] attributes, MonoScript mono)
    {
        if (clip == null || mono == null || attributes == null || attributes.Length == 0) return;
        try
        {
            var so = new SerializedObject(clip);
            var arr = so.FindProperty("m_FloatCurves");
            if (arr == null)
            {
                Debug.LogWarning(EX1 + $"曲线脚本接回：`{clip.name}` 取不到 `m_FloatCurves` ⇒ "
                                      + $"[{string.Join(", ", attributes)}] 曲线保持断链");
                return;
            }
            int fixedN = 0;
            for (int i = 0; i < arr.arraySize; i++)
            {
                var e = arr.GetArrayElementAtIndex(i);
                var at = e.FindPropertyRelative("attribute");
                if (at == null || Array.IndexOf(attributes, at.stringValue) < 0) continue;
                var sc = e.FindPropertyRelative("script");
                if (sc == null || sc.objectReferenceValue != null) continue;   // 已经能解析 ⇒ 不碰
                sc.objectReferenceValue = mono;
                fixedN++;
            }
            if (fixedN > 0)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(clip);
                Debug.Log(EX1 + $"曲线脚本接回：`{clip.name}` 接回 {fixedN} 条曲线 → `{mono.GetClass()}`");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(EX1 + $"曲线脚本接回失败（`{clip.name}` / [{string.Join(", ", attributes)}]）：{ex.Message}");
        }
    }

    /// <summary>V3：`AnimFXModulePostProcess` 的曲线属性（原版 28 条 classID:114 里落在这 5 个名字上的全部）。</summary>
    static readonly string[] PostProcessCurveAttributes =
        { "lutBlend", "bloomIntensity", "bloomThreshold", "betweenLUTsBlend", "animateBloomIntensity" };

    /// <summary>V3：`TMPro.TextMeshPro` 的 `m_fontColor` 四个通道（`InBattleDamageCounter Variation 1`）。</summary>
    static readonly string[] FontColorCurveAttributes =
        { "m_fontColor.r", "m_fontColor.g", "m_fontColor.b", "m_fontColor.a" };

    /// <summary>V3：`ParticleForceForceFieldController` 的曲线属性 —— 全库只有 `Will Of Grok` 这一条。</summary>
    static readonly string[] ForceFieldCurveAttributes = { "force" };


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
