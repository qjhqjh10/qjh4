// BoosterPackAssets.cs — 卡包（锻造 / 开包）3D 外观的【原件取件口】（2026-10-29 · 第九十七会话 · `B97_W23`）
//
// ============================ 它解决什么 ============================
// 原版 shader `Everguild/Cards/Shader Graphs/Booster Pack` **不在我们工程里**（`grep -l Booster` 全 `*.shader` = 0），
// 而它是 ShaderGraph **生成体**（`m_Script = null`、没有 `.shader` 源），只有 `compressedBlob` 里的编译字节码
// ⇒ **既不能手抄成工程 shader，也不能建成 `.mat` 资产**（`.mat` 存一份指向非工程资产的 shader ⇒ 空 GUID ⇒ 品红）。
// 唯一合法的取法是**运行时从 bundle 里取原件**（本工程既有口径，见 `WarpforgeVFX/WarpforgeShaderLoader.cs` 文件头）。
//
// ============================ 🔴 本件现核出来的三条硬事实 ============================
// ① **shader 与它的 16 个材质、4 张共用贴图、17 棵 prefab 全在同一个包里**：
//    `StreamingAssets/WarpforgeVFX/wf_boosters_extra.bundle`（5,972,456 B · 2026-10-22 `--prefabs` 产物）。
//    UnityPy 实读（1.24.2）：`Shader` ×1（`m_ParsedForm.m_Name` 逐字 = 本类 `ShaderName`）·
//    `Material` ×24（其中 16 个 `Card Booster Pack*` 的 `m_Shader` **在包内可解**）·
//    `Texture2D` ×28 · `Mesh` ×1（BoosterPack）· `GameObject` ×367 / 根 ×17。
//    16 个材质的 4 张贴图（`_MainTex` / `_ExplodeTex` / `_MatCap` / `_Normal_map`）**逐个在包内**，
//    逐张与 `报告/B97_W17.md` §四 的表对上（0 悬空）。
// ② **但它**（shader）**既不在 `m_Container` 里、也不在 `m_PreloadTable` 里**：
//    容器 38 条 / 预载表 19 条，**全是 17 棵 prefab + 2 条 clip**（UnityPy 实读，逐条点名过）。
//    ⇒ `LoadAsset<Shader>("Everguild/…")` 与 `LoadAllAssets<Shader>()` **两条都拿不到它** ——
//      这与本工程已记录的口径一致（`工具/extract_missing_shaders.py` 文件头：
//      「`LoadAllAssets()` 988 个结果里 0 个 Shader」「容器空 ⇒ 原始拷贝 0 个、重打之后 15 个全在」）。
// ③ **它仍可经 prefab 传递拿到**：`LoadAsset<GameObject>("Booster Pack Standard")` 会连带解出
//    那棵树的依赖 —— MeshRenderer → `Card Booster Pack Standard`（材质）→ **本 shader** + 4 张贴图。
//    ⇒ 本类**全部取件都走这一跳**；`TryGetShader` 也是（从模板材质上读 `.shader`，⛔ 不是去 LoadAllAssets 撞运气）。
//
// ============================ 为什么不是「建 16 个 .mat」============================
// `.mat` 里的 shader 引用是 **guid**，而 bundle 里的 Shader 没有工程 guid（`WarpforgeShaderLoader` 文件头：
// 「CreateAsset(.asset) 报 `Shader is already an asset at ''`；存 .mat 会让 shader 引用变成空 GUID，
//  重导入后是 `Hidden/InternalErrorShader`」）⇒ **16 个 `.mat` 资产这条路在结构上就不可行**。
// 正确形状 = **运行时建材质**：
//   · 想「就按原版画」 ⇒ `TryGetPrefab(...)` 直接 `Instantiate`，材质/网格/粒子一起带走，**零属性拷贝**；
//   · 想「只要一份材质」 ⇒ `CloneTemplateMaterial(...)`（`new Material(原件)` 会整块拷
//     shader + 32 个 float + 7 个 color + 4 张贴图 + 关键字，与本工程 `ArenaOriginalShaderRebind` 同一思路）。
//   ⛔ 两者都**不许**把结果存成工程资产。
//
// ============================ 判据（全部可复查）============================
//   · shader 的 `m_ParsedForm`：`assets_full/bundle_boosterpacks_assets_all/Shader/Shader_-7650550828973362251.json`
//     —— `m_Name` = `Everguild/Cards/Shader Graphs/Booster Pack` ·
//        subShader tag `RenderPipeline = UniversalPipeline` / `ShaderGraphTargetId = UniversalUnlitSubTarget` /
//        `QUEUE = Geometry` / `RenderType = Opaque` ⇒ **URP 的 unlit**，在本工程（URP）里能渲，不是「内置管线老 shader」那一档。
//        5 个 pass：`Universal Forward` / `DepthOnly` / `MotionVectors` / `DepthNormalsOnly` / `ShadowCaster`。
//   · 材质：`assets_full/bundle_boosterpacks_assets_all/Material/Material_*.json`（16 份，`m_Name` 以 `Card Booster Pack` 开头）；
//     逐条数值表 → `资料/完全复刻/报告/B97_W17.md` §四（⛔ 别在本文件里抄第二份）。
//   · 包结构（容器 / 预载表 / 依赖解析）：UnityPy 1.24.2 实读，逐条记在
//     `资料/完全复刻/报告/B97_W23.md` §二。
//
// ============================ 谁在用 ============================
// 现在 **0 个调用方**（用户 2026-10-22 口径：那 17 棵 prefab「只导出备用，不接运行时」，开包窗画的是整屏纯色背景）。
// 接线时（`Shell/BoosterPackOpenWindow.cs` 里那件 `Booster Animation Parent` / 原版 `boosterPackAnchor`）：
//   `if (BoosterPackAssets.TryGetPrefab(BoosterPackAssets.PrefabForArmy(army), out var pf)) { var go = Instantiate(pf, anchor); }`
//   —— 拿不到时**出声**（`TryGet*` 都带 `Debug.LogWarning`），⛔ 不许静默退回纯色。
// 自检入口 = `CardPresentation/Editor/B97W23_Assert.cs`（`-executeMethod B97W23Assert.Run`）。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>卡包 3D 外观（原版 `Booster Pack Standard*` 那一族 prefab）的运行时取件口。见文件头。</summary>
    public static class BoosterPackAssets
    {
        /// <summary>产物包（`工具/extract_missing_shaders.py --prefabs` 的 `BOOSTER_GROUPS[1]`）。
        /// 与 `WarpforgeShaderLoader` 那 5 个包**不是同一批** —— 那个类目前**没有**加载本包
        /// （`WarpforgeShaderLoader.EnsureLoaded` 只认 `wf_shaders` / `wf_shaders_extra` / `wf_builtin` /
        ///  `wf_tmp_shaders` / `wf_arena_*.bundle`），所以 shader 走不到它那条链。见报告 §四「跨文件请求」。</summary>
        public const string BundleRelPath = "WarpforgeVFX/wf_boosters_extra.bundle";

        /// <summary>原版 shader 名（**逐字**，`m_ParsedForm.m_Name`）。</summary>
        public const string ShaderName = "Everguild/Cards/Shader Graphs/Booster Pack";

        /// <summary>通用那一档（`Booster Pack All Armies Variant` 与它共用 `Card Booster Pack Standard` 材质）。</summary>
        public const string StandardPrefab = "Booster Pack Standard";

        /// <summary>🔴 **表只此一份（运行期这一侧）** —— 17 个根名，**逐字照 bundle `m_Name`**（⛔ 别 Trim、别改大小写）。
        /// 编辑期那份 `WarpforgeArena1/Editor/BoosterPackExporter.cs` 的 `WindowRoots` 目前**自己抄了一份**
        /// （它那份还混着两扇窗 `Booster Pack Open Window` / `Booster Info Popup`，那是 `wf_menus_extra.bundle` 的）。
        /// ⚠️ 两份并存违反「表只此一份」⇒ 报告 §四 已提「让导出器改成转调本表」的跨文件请求（本代理不改别人的文件）。
        /// 出处 = UnityPy 实读 `wf_boosters_extra.bundle` 的 17 个根 `Transform.m_GameObject.m_Name`。</summary>
        public static readonly string[] PrefabNames =
        {
            "Booster Pack Standard",
            "Booster Pack All Armies Variant",
            "Booster Pack Standard Aeldari SaimHann",
            "Booster Pack Standard Astra Militarum",
            "Booster Pack Standard Chaos SM Black Legion",
            "Booster Pack Standard Emperors Children",
            "Booster Pack Standard Genestealers",
            "Booster Pack Standard Necrons Sautekh",
            "Booster Pack Standard Orks Goff",
            "Booster Pack Standard Orks Goff Expansion",
            "Booster Pack Standard Sororitas",
            "Booster Pack Standard Space Marines Dark Angels",
            "Booster Pack Standard Space Marines Space Wolves",
            "Booster Pack Standard Space Marines Ultramarines",
            "Booster Pack Standard Space Marines Ultramarines Expansion 1",
            "Booster Pack Standard TauEmpire",
            "Booster Pack Standard Tyranids Leviathan",
        };

        static bool _tried;
        static AssetBundle _bundle;
        static Shader _shader;
        static readonly Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();

        /// <summary>清空加载状态，让下一次取件重走一遍（**只给自检 / 编辑器工具用**）。
        /// ⚠️ 不清空的话，`EffectSweepBatch` 那类「先加载源包、再 UnloadAllAssetBundles」的两阶段模式
        /// 会把第一阶段那份**已被销毁**的 Shader 缓存住 —— 同族坑见 `WarpforgeShaderLoader.Reset`。</summary>
        public static void Reset()
        {
            _tried = false;
            _bundle = null;
            _shader = null;
            _prefabs.Clear();
        }

        /// <summary>包加载上了没有（加载失败会**出声**，⛔ 不静默）。</summary>
        public static bool TryEnsureLoaded()
        {
            if (_tried) return _bundle != null;
            _tried = true;

            var path = Path.Combine(Application.streamingAssetsPath, BundleRelPath);
            if (!File.Exists(path))
            {
                Debug.LogWarning("[BoosterPack] 找不到卡包 bundle：`" + path + "`"
                                 + "（跑 `python 工具/extract_missing_shaders.py --prefabs` 重新打）⇒ 卡包 3D 外观取不到。");
                return false;
            }

            try { _bundle = AssetBundle.LoadFromFile(path); }
            catch (Exception e)
            {
                Debug.LogWarning("[BoosterPack] `AssetBundle.LoadFromFile` 抛异常：`" + path + "` → " + e.Message);
                _bundle = null;
            }

            // 🔴 同一份内容已在进程里加载过时 `LoadFromFile` 返回 **null**（Unity 限制）。
            //    本工程既有做法 = **从已加载的包里认领**（同 `WarpforgeShaderLoader.HarvestFromLoadedBundles`
            //    与 `ScenarioBlendables.MenuClipBundle`）。判据用「它能不能按容器键取出通用 prefab」，
            //    ⛔ 不用 `bundle.name` —— 那是包内 `m_Name`（`4313a007….bundle`，与文件名无关）。
            if (_bundle == null)
            {
                foreach (var b in AssetBundle.GetAllLoadedAssetBundles())
                {
                    if (b == null) continue;
                    GameObject probe = null;
                    try { probe = b.LoadAsset<GameObject>(StandardPrefab); } catch { }
                    if (probe != null) { _bundle = b; Debug.Log("[BoosterPack] 本包已被别的系统加载过，直接认领。"); break; }
                }
                if (_bundle == null)
                {
                    Debug.LogWarning("[BoosterPack] 卡包 bundle 加载失败，且已加载的包里也没有它 ⇒ 卡包 3D 外观取不到。");
                    return false;
                }
            }

            Debug.Log("[BoosterPack] 卡包 bundle 已加载：`" + BundleRelPath + "`（17 棵 prefab · shader `" + ShaderName + "`）。");
            return true;
        }

        /// <summary>拿到底层 `AssetBundle`（诊断 / 自检用）。⛔ 别 `Unload` 它 —— 本类缓存着里面的资产。</summary>
        public static bool TryGetBundle(out AssetBundle bundle)
        {
            bundle = null;
            if (!TryEnsureLoaded()) return false;
            bundle = _bundle;
            return bundle != null;
        }

        /// <summary>按容器键取一棵原版 prefab（**返回的是包里的资产本体，⛔ 别 Destroy**；
        /// 要挂进场景就自己 `Instantiate`）。取不到 ⇒ `false` + 一行警告。</summary>
        public static bool TryGetPrefab(string prefabName, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrEmpty(prefabName)) return false;
            if (!TryEnsureLoaded()) return false;
            if (_prefabs.TryGetValue(prefabName, out prefab) && prefab != null) return true;

            GameObject go = null;
            try { go = _bundle.LoadAsset<GameObject>(prefabName); }
            catch (Exception e)
            {
                Debug.LogWarning("[BoosterPack] `LoadAsset<GameObject>(\"" + prefabName + "\")` 抛异常：" + e.Message);
            }
            if (go == null)
            {
                Debug.LogWarning("[BoosterPack] 包里没有 prefab `" + prefabName + "`"
                                 + "（容器 38 条 = 17 prefab + 2 clip；名字必须**逐字**照 bundle `m_Name`）");
                return false;
            }
            _prefabs[prefabName] = go;
            prefab = go;
            return true;
        }

        /// <summary>从某棵 prefab 上取**原版材质本体**（`MeshRenderer.sharedMaterial`）。
        /// 这就是 16 个 `Card Booster Pack*` 之一 —— shader / 4 张贴图 / 32 float / 7 color 全是原版的。
        /// ⚠️ 返回的是**包里的资产**，⛔ 别 Destroy、⛔ 别改它（要改就先 `CloneTemplateMaterial`）。</summary>
        public static bool TryGetTemplateMaterial(string prefabName, out Material material)
        {
            material = null;
            GameObject prefab;
            if (!TryGetPrefab(prefabName, out prefab) || prefab == null) return false;

            var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var mr = renderers[i];
                if (mr == null || mr.sharedMaterial == null) continue;
                material = mr.sharedMaterial;
                return true;
            }
            Debug.LogWarning("[BoosterPack] prefab `" + prefabName + "` 的子树里没有带材质的 `MeshRenderer`"
                             + "（预期：根上那颗挂 `Card Booster Pack …`）");
            return false;
        }

        /// <summary>取原版 shader 本体。
        /// 🔴 **只能经 prefab 传递拿到** —— 它既不在包的 `m_Container` 也不在 `m_PreloadTable` 里（见文件头 ②）
        /// ⇒ `LoadAsset<Shader>(名字)` 与 `LoadAllAssets<Shader>()` 两条都取不到，⛔ 别改成那样撞运气。</summary>
        public static bool TryGetShader(out Shader shader)
        {
            shader = null;
            if (_shader != null) { shader = _shader; return true; }

            Material template;
            if (!TryGetTemplateMaterial(StandardPrefab, out template) || template == null) return false;
            if (template.shader == null)
            {
                Debug.LogWarning("[BoosterPack] 模板材质 `" + template.name + "` 的 shader 是空的"
                                 + "（包内依赖断了？判据 → 报告 §二）");
                return false;
            }
            _shader = template.shader;
            shader = _shader;
            return true;
        }

        /// <summary>克隆一份**运行期材质**（`new Material(原版材质)` —— 整块拷 shader + 全部属性 + 关键字，
        /// 本工程同思路：`ArenaOriginalShaderRebind` / `ArenaOriginalMaterial.Rebuild`）。
        /// ⛔ **不许存成工程 `.mat`**（理由见文件头「为什么不是建 16 个 .mat」）。</summary>
        public static Material CloneTemplateMaterial(string prefabName, string name = null)
        {
            Material template;
            if (!TryGetTemplateMaterial(prefabName, out template) || template == null) return null;
            var m = new Material(template);
            if (!string.IsNullOrEmpty(name)) m.name = name;
            return m;
        }

        /// <summary>阵营 → 该用哪棵 prefab。**对照关系是从名字上读出来的，不是挑的**：
        /// 每棵 prefab 根上那颗 `MeshRenderer` 挂的材质就叫 `Card Booster Pack <阵营>`（UnityPy 逐棵实读，
        /// 逐条表 → 报告 §三），所以「哪棵配哪阵营」没有自由度。
        ///
        /// ⚠️ **两侧的名字口径不同，必须归一化后再匹配**：
        ///   · 我们的阵营 id（`Shell/ForgeData.cs:40-42` 的 `Armies`）= **不带空格**：
        ///     `Ultramarines` `Goff` `SaimHann` `BlackLegion` `DarkAngels` `Genestealers` `TauEmpire`
        ///     `Sautekh` `AstraMilitarum` `Leviathan` `Sororitas` `EmperorsChildren` `SpaceWolves` `Neutral`；
        ///     （`BoosterPackOpenWindow.ArmyOfArt` 用的也是这一套。）
        ///   · 原版 prefab / 材质名 = **带空格**且有历史拼写：`Space Marines Dark Angels` ·
        ///     `Chaos SM Black Legion` · `TauEmpire`（这一条原版自己没空格）· `Aledari`（正字 Aeldari）。
        /// ⇒ 做法 = **两侧都去掉空格 + 转小写**再比子串；⛔ 别按「原版名里有没有空格」写死。
        /// 认不出 ⇒ `false` + 一行警告（调用方自己决定退回哪一棵，⛔ 别静默拿错图）。</summary>
        public static bool TryGetPrefabForArmy(string army, out string prefabName)
        {
            prefabName = null;
            if (string.IsNullOrEmpty(army)) { prefabName = StandardPrefab; return true; }

            string key = army.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();

            // ⚠️ 顺序有讲究：**更具体的先判**（`Ultramarines Expansion` 早于 `Ultramarines`；
            //    `Goff Expansion` 早于 `Goff`；`Astra Militarum` 早于任何含 `Militarum` 的写法）。
            if (Has(key, "neutral") || Has(key, "generic")) prefabName = StandardPrefab;
            else if (Has(key, "guilliman") || Has(key, "ultramarinesexpansion")) prefabName = "Booster Pack Standard Space Marines Ultramarines Expansion 1";
            else if (Has(key, "ultramarine")) prefabName = "Booster Pack Standard Space Marines Ultramarines";
            else if (Has(key, "darkangel")) prefabName = "Booster Pack Standard Space Marines Dark Angels";
            else if (Has(key, "spacewolve") || Has(key, "wolve")) prefabName = "Booster Pack Standard Space Marines Space Wolves";
            else if (Has(key, "blacklegion") || Has(key, "chaos")) prefabName = "Booster Pack Standard Chaos SM Black Legion";
            else if (Has(key, "emperor")) prefabName = "Booster Pack Standard Emperors Children";
            else if (Has(key, "genestealer")) prefabName = "Booster Pack Standard Genestealers";
            else if (Has(key, "sautekh") || Has(key, "necron")) prefabName = "Booster Pack Standard Necrons Sautekh";
            else if (Has(key, "beastsnagga") || Has(key, "goffexpansion")) prefabName = "Booster Pack Standard Orks Goff Expansion";
            else if (Has(key, "goff") || Has(key, "ork")) prefabName = "Booster Pack Standard Orks Goff";
            else if (Has(key, "sororitas")) prefabName = "Booster Pack Standard Sororitas";
            else if (Has(key, "astra") || Has(key, "militarum")) prefabName = "Booster Pack Standard Astra Militarum";
            else if (Has(key, "saimhann") || Has(key, "saim") || Has(key, "aeldari") || Has(key, "aledari")) prefabName = "Booster Pack Standard Aeldari SaimHann";
            else if (Has(key, "tau")) prefabName = "Booster Pack Standard TauEmpire";
            else if (Has(key, "leviathan") || Has(key, "tyranid")) prefabName = "Booster Pack Standard Tyranids Leviathan";

            if (prefabName == null)
            {
                Debug.LogWarning("[BoosterPack] 认不出阵营 `" + army + "` 该用哪棵 prefab（17 棵的对照表 → 报告 §三）");
                return false;
            }
            return true;
        }

        static bool Has(string normalizedKey, string needle)
        {
            return normalizedKey.IndexOf(needle, StringComparison.Ordinal) >= 0;
        }
    }
}
