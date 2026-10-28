// MaterialChangerByQuality.cs — 原版 `MaterialChangerByQuality`（账 `未分类/其他` :423 `Initialize()` / :424 `TryApply()`）
//
// ============================ 判据（一手，第九十四会话 R-G 现读） ============================
//  正本 → `资料/完全复刻/判据/B94_R-G_画质档位映射.md`
//    §〇 三句结论 · §二 映射链 · §四 给施工的结论（含**5 个实例的实测表**）
//  · 原版类体 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/MaterialChangerByQuality.cs`（全 96 行，逐句读过）
//    + `d:/2/tools/decomp_full/MaterialChangerByQuality__Initialize.c`
//    + `d:/2/tools/decomp_full/MaterialChangerByQuality.MaterialAndShaderQualityPresets__TryApply.c`
//  · **输入 = 静态字段 `GameStaticData.graphicsQuality`（`+0x120`，类型 `EverguildQualitySetting`）**，
//    ⛔ **不是** `QualitySettings.GetQualityLevel()`（R-G §〇 第 1 条：这一格我们工程早就有，
//    见 `Shell/GameStaticData.cs:106`，出厂 `Ultra`(4)）⇒ 不需要任何「画质档映射」来喂它。
//  · **命中规则只有一条**：`==` 精确相等、取 `presets` 里**第一个**匹配项、然后 `return`；
//    **不命中 = no-op**。🔴 出厂 `Ultra`(4) 下 5 个实例**必然不命中** ⇒ 出厂画面就是原版出厂画面，
//    ⛔ **别「修好」它**（R-G §〇 第 2 条 / §4.3：这不是缺陷）。
//  · 入口只有 `Initialize()`（没有 `OnEnable`、没有监听器）⇒ 运行中改档它**不会自己重跑**（照原版）。
//  · `GetComponent<MeshRenderer>()` **只取自身**，不查子节点（R-G §4.2 末条）。
//
// ============================ 两处「我们的适配」（平台差异，⛔ 不是我们改了行为） ============================
//  ① **shader 按【名字】存、运行时解析**：原版 `presets[].shader` 是**工程资产引用**；bundle 里的 `Shader`
//     **落不了工程资产**（`AssetDatabase.CreateAsset` 会把引用写成空 GUID ⇒ 重新导入变 `InternalErrorShader`
//     ⇒ 整屏洋红；判据 → `WarpforgeVFX/Runtime/ArenaOriginalMaterial.cs` 文件头 +
//     `WarpforgeArena1/Editor/ArenaBuilder.cs:4956-4965`）⇒ 本类多一个 `shaderName` 字段，
//     `Apply` 里用 `WarpforgeShaderLoader.TryGetShader` 换成真 `Shader`。
//     ⚠️ 原版那 5 个实例**只填 shader、`material` 全 null**（R-G §4.1 逐实例实测）⇒ 实际走的是
//     「换 `sharedMaterial.shader`」那条支，`SetMaterial` 那一支在我们的 13 场里**一次都不会命中**。
//  ② **`priority`**：原版序列化 `priority = 2 (UI)`；本仓 `InitializableBehaviour.priority` 是 private 的
//     `[SerializeField]`、没有 setter（`Shell/InitializableBehaviour.cs` 不在本次白名单）⇒ 施工侧用
//     `SerializedObject` 写（见文件尾 `AttachForArena` 的注释）。⚠️ 顺序上不依赖它：地块材质的重建
//     （`ArenaOriginalMaterial.Awake → Rebuild`）在 `Awake` 期就做完了，而本类在 `Start → SceneInitializer`
//     之后才 `Initialize()` ⇒ 任何 priority 都排在它后面。
//
// ============================ 那两处「原版自己就有的」行为（⛔ 别改） ============================
//  · `material == null` 且 `shader == null` 的预设行 ⇒ **命中后什么都不做、直接 return**（GoTo IL_016b）。
//  · 命中时若渲染器**连 sharedMaterial 都没有**：原版是 `m.shader = …` 直接 NRE（decomp 里那句
//    `throw new NullReferenceException()` 被误译成 `return (Version)…`）。本工程红线「不许崩宿主」
//    ⇒ 我们**出声 + no-op**（同 `GameStaticData.CheckIfNewCards` 对 null 列表的处理口径）。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `MaterialChangerByQuality : InitializableBehaviour`（`Initialize()` 按
    /// `GameStaticData.graphicsQuality` 给挂着的 `MeshRenderer` 换一体材质 / 换 shader）。
    /// <para>🔴 **不命中 = no-op**，而且出厂档 `Ultra`(4) 就落在「不命中」那一支 —— 那是**原版行为**。</para>
    /// <para>判据 → `资料/完全复刻/判据/B94_R-G_画质档位映射.md` §四（⛔ 别重新查、别重新设计）。</para></summary>
    public class MaterialChangerByQuality : InitializableBehaviour
    {
        /// <summary>原版内嵌类 `MaterialChangerByQuality.MaterialAndShaderQualityPresets`。
        /// ⚠️ 原版它和它的字段是 **`private`**；这里放成 `public` **只为**让施工侧（`ArenaBuilder`）与自检
        /// 填得进去（字段名 / 类型 / 顺序**逐字照原版**，⛔ 别改名）。</summary>
        [Serializable]
        public class MaterialAndShaderQualityPresets
        {
            public GameStaticData.EverguildQualitySetting quality;
            public Material material;
            public Shader shader;

            /// <summary>🔶 我们的适配（见文件头 ①）：`shader` 的**按名字**替代品。
            /// `shader` 有值时不采它；两者都空 ⇒ 命中后什么都不做（原版 IL_016b 那条）。</summary>
            public string shaderName;

            /// <summary>原版 `MaterialAndShaderQualityPresets.TryApply(EverguildQualitySetting qualityTest,
            /// MeshRenderer meshRenderer)`（`decomp_full/…__TryApply.c`）。
            /// <para>`qualityTest == quality` ⇒ 动手并返回 <c>true</c>；不等 ⇒ <c>false</c> 什么都不做。
            /// ⚠️ 原版**匹配后返回 true 并不代表真改了**（`material`/`shader` 都空时它也走到 IL_016b 返回 true）
            /// —— 这一点照抄，⛔ 别「修」成 `return false`。</para>
            /// <para>⚠️ 原版 `meshRenderer` 为 null ⇒ NRE；本工程**不崩宿主** ⇒ 返回 <c>false</c> 并出声。</para>
            /// </summary>
            public bool TryApply(GameStaticData.EverguildQualitySetting qualityTest, MeshRenderer meshRenderer)
            {
                if (qualityTest != quality) return false;
                if (meshRenderer == null)
                {
                    Debug.LogWarning("[MaterialChangerByQuality] `TryApply` 收到 null `MeshRenderer`"
                                   + "（原版在这里 NRE；本工程不崩宿主 ⇒ 当未命中）。");
                    return false;
                }
                Apply(meshRenderer);
                return true;
            }

            /// <summary>「命中之后到底做什么」—— 原版 `Initialize()` 内联的那一段**与 `TryApply` 逐句相同**
            /// （R-G §4.2 对照过），本类**只写这一份**（两处写同一条规则 = 迟早不一致）。
            /// <para>① `material` 有值 ⇒ `SetMaterial(material)`（实例材质那一支）；
            /// ② 否则 `shader` 有值 ⇒ 把 `shader` 写到渲染器**当前那份材质**上（`Application.isPlaying` ⇒
            /// `Renderer.material`，否则 `Renderer.sharedMaterial` —— ⚠️ 见下面那条存疑）；
            /// ③ 两者都空 ⇒ 什么都不做。</para>
            /// <para>🔴 **存疑（照 R-G §五④ 如实标注）**：原版那两个支的开关在反编译里是
            /// `UnityEngine_UIElements_BaseVerticalCollectionView__HandleItemNavigation(0)` = **符号错配
            /// （ICF 折叠）**，语义疑似 `Application.isPlaying`（运行期取 `.material`、编辑期取 `.sharedMaterial`），
            /// **没有拿到反证** ⇒ 本类按「疑似」实现。对 `material == null` 的那 5 个实例**两支等价**
            /// （都只改 `sharedMaterial`/`material` 的 `shader` 字段，而两者指向同一份材质时结果相同）⇒ 不影响本仓行为。</para>
            /// </summary>
            internal void Apply(MeshRenderer mr)
            {
                // ① 原版 `if (this.material) → Renderer.SetMaterial(this.material)`
                if (material != null)
                {
                    if (Application.isPlaying) mr.material = material;
                    else mr.sharedMaterial = material;
                    return;
                }

                // ② 原版 `else if (shader) → (isPlaying ? GetMaterial() : GetSharedMaterial()).shader = shader`
                Shader sh = ResolveShader();
                if (sh == null) return;      // ③ 两者都空 = 什么都不做（原版 IL_016b）

                // ⚠️ 原版这里直接 `.shader = …`（材质为 null 就 NRE）⇒ 本工程出声 + no-op（红线：不崩宿主）
                Material cur = Application.isPlaying ? mr.material : mr.sharedMaterial;
                if (cur == null)
                {
                    Debug.LogWarning("[MaterialChangerByQuality] 命中了预设，但 `" + mr.name
                                   + "` 的 MeshRenderer **没有材质** ⇒ 换不了 shader（原版在这里 NRE；"
                                   + "本工程不崩宿主 ⇒ 当没做，且出声）。");
                    return;
                }
                cur.shader = sh;
            }

            /// <summary>`shader` 优先；没有就按 `shaderName` 去 bundle 里取（见文件头 ①）。
            /// ⚠️ 名字给了却取不到 ⇒ **出声**（红线：不许静默失败），然后按「没有 shader」处理。</summary>
            Shader ResolveShader()
            {
                if (shader != null) return shader;
                if (string.IsNullOrEmpty(shaderName)) return null;
                Shader got;
                if (WarpforgeVFX.WarpforgeShaderLoader.TryGetShader(shaderName, out got) && got != null) return got;
                Debug.LogWarning("[MaterialChangerByQuality] 取不到原版 shader `" + shaderName
                               + "`（`WarpforgeShaderLoader.TryGetShader` 返回 false）⇒ 这条预设按「没有 shader」处理。");
                return null;
            }
        }

        /// <summary>原版 `MaterialChangerByQuality.presets`（`private MaterialAndShaderQualityPresets[]`，
        /// 序列化）。我们同样 private + `[SerializeField]`，施工侧走 <see cref="SetPresets"/>。</summary>
        [SerializeField] MaterialAndShaderQualityPresets[] presets;

        /// <summary>= 原版 `Initialize()`（`decomp_full/MaterialChangerByQuality__Initialize.c` 逐句）。
        /// <para>⚠️ **不命中 = 什么都不做**（原版就是这样：出厂 `Ultra` 下 5 个实例全不命中）。</para>
        /// <para>⚠️ 没有 `MeshRenderer` ⇒ `LogError("[ERROR] Trying to change the material to an object
        /// without mesh")` —— **报文逐字照原版**（含前缀 `[ERROR] `）。</para></summary>
        public override void Initialize()
        {
            var mr = GetComponent<MeshRenderer>();       // ⚠️ 只取自身，不查子节点
            if (mr == null)
            {
                Debug.LogError("[ERROR] Trying to change the material to an object without mesh");
                return;
            }
            if (presets == null || presets.Length == 0) return;   // 原版 null ⇒ NRE；本工程静默跳过（无预设 = 无活可干）

            for (int i = 0; i < presets.Length; i++)
            {
                var p = presets[i];
                if (p == null) continue;
                if (GameStaticData.graphicsQuality != p.quality) continue;   // ★ `==` 精确相等，无区间/回退
                p.Apply(mr);
                return;                                                      // ★ 命中第一行就停
            }
            // 走完一整圈 = 不命中 ⇒ no-op（原版行为，⛔ 别「修好」）
        }

        // ============================================================ 施工口（⛔ 不是原版 API；只为 ArenaBuilder / 自检）
        // 🔴 新成员一律追加在类尾（本文件是新建的，无既有账落点；纪律见 CLAUDE.md §18·b 的同族条款）。

        /// <summary>施工 / 自检读口（原版那格是 private 序列化字段，运行期没有人读它）。</summary>
        public MaterialAndShaderQualityPresets[] GetPresets() { return presets; }

        /// <summary>施工口：填预设（`ArenaBuilder` 建场期调）。</summary>
        public void SetPresets(MaterialAndShaderQualityPresets[] list) { presets = list; }

        /// <summary>原版 5 个实例的预设表 + 两个 shader 名字族（`R-G §4.1` 逐条实测，⛔ 别按印象改）。
        /// <para>🔴 **为什么表在代码里、不在 prefab 里**：原版是每个地块对象上序列化一份；我们的 13 场是
        /// **生成器建的**（`WarpforgeArena1/Editor/ArenaBuilder.cs`）⇒ 等价物就是这张表和那个建场期挂载口。
        /// 表里只有 **shader 名字字符串**（真 `Shader` 在运行时按名字解析，见文件头 ①）。</para></summary>
        public static class ArenaPresetTable
        {
            /// <summary>`Ground` / `Floor (1)` 那 4 个实例用的**普通版** shader 名（逐字照 `m_Name`）。</summary>
            public const string ShaderUnlitShadowsReceiver = "Everguild/Misc/Unlit shadows receiver";
            /// <summary>`quality == 2` 那一档用的**普通版** shader 名（逐字照 `m_Name`）。</summary>
            public const string ShaderFloorPlanarGrainny = "Everguild/FX/Floor Planar Reflections Grainny";
            /// <summary>`battlearenatauviorla` 那一份用的**顶点色版** low 档 shader 名。</summary>
            public const string ShaderUnlitShadowsReceiverVertexColor =
                "Everguild/Misc/Unlit shadows receiver Vertex Color Shadow Mask";
            /// <summary>`battlearenatauviorla` 那一份用的**顶点色版** `quality == 2` shader 名。</summary>
            public const string ShaderFloorPlanarGrainnyVertexColor =
                "Everguild/FX/Floor Planar Reflections Grainny Vertex color shadow mask";

            /// <summary>一条「哪个场、哪个对象、用哪一族 shader」的登记。
            /// ⚠️ `Arena` 为空串 ⇒ **共用包 `battlesharedresources` 里那两份**（见 <see cref="SharedBundleEntries"/>）。</summary>
            public struct Entry
            {
                public string Arena;            // 我们的场景名（= `WF_ARENA` 那一档）；空 = 共用包那两份
                public string ObjectName;       // 原版 GO 名（`Ground` / `Floor (1)` / `Floor`）
                public bool VertexColor;        // false = 普通版 shader 族；true = 顶点色版
                public string SourceObjectId;   // 原版 MonoBehaviour 的 pathID（可复查坐标）
            }

            /// <summary>**已确认归属**的 3 条（`R-G §4.1` 表里的 #3/#4/#5，各自点名了所在场景包）。</summary>
            public static readonly Entry[] ArenaEntries =
            {
                new Entry { Arena = "battlearena3",        ObjectName = "Ground",    VertexColor = false, SourceObjectId = "4523" },
                new Entry { Arena = "battlearenaaeldari",  ObjectName = "Floor (1)", VertexColor = false, SourceObjectId = "4902" },
                new Entry { Arena = "battlearenatauviorla", ObjectName = "Floor",     VertexColor = true,  SourceObjectId = "4753" },
            };

            /// <summary>🔴 **归属未查清的 2 条**：`R-G §4.1` 的 #1/#2 在共用包
            /// `bundle_battlesharedresources_assets_all` 里（`Ground` / `Floor (1)`），**哪几场用这份共用 prefab
            /// 还没查**（本代理按红线「卡住停手、不许发明口径」**没有把它们挂到任何一场上**）。
            /// 施工侧若要接这 2 条，先派只读代理查「arena ↔ 共用地面 prefab」的归属，再往
            /// <see cref="ArenaEntries"/> 补（⛔ 别按场景里有没有同名对象猜 —— `battlearena2/astra/…` 都有
            /// 自己的 `Floor`，但那是**本场自己的** MeshRenderer，不是这份共用件）。</summary>
            public static readonly Entry[] SharedBundleEntries =
            {
                new Entry { Arena = "", ObjectName = "Ground",    VertexColor = false, SourceObjectId = "-3128930142240273445" },
                new Entry { Arena = "", ObjectName = "Floor (1)", VertexColor = false, SourceObjectId = "-3393742555676221536" },
            };

            /// <summary>按（场景名, 原版 GO 名）查这一条登记。**只查 <see cref="ArenaEntries"/>**（共用包那两条见其上注释）。</summary>
            public static bool TryMatch(string arena, string objectName, out Entry entry)
            {
                entry = default(Entry);
                if (string.IsNullOrEmpty(arena) || string.IsNullOrEmpty(objectName)) return false;
                for (int i = 0; i < ArenaEntries.Length; i++)
                {
                    if (ArenaEntries[i].Arena == arena && ArenaEntries[i].ObjectName == objectName)
                    { entry = ArenaEntries[i]; return true; }
                }
                return false;
            }

            /// <summary>建一份预设数组：`0 VeryLow` / `1 Low` ⇒ low 档 shader；`2 Medium` ⇒ Grainny；
            /// **没有 3/4 两行**（原版 5 个实例逐条实测都只有这三行 ⇒ `High`/`Ultra` 不命中）。
            /// ⚠️ 三行的 `material` **全 null**（原版实测），只填 `shaderName`（见文件头 ①）。</summary>
            public static MaterialAndShaderQualityPresets[] BuildPresets(bool vertexColor)
            {
                string low = vertexColor ? ShaderUnlitShadowsReceiverVertexColor : ShaderUnlitShadowsReceiver;
                string grain = vertexColor ? ShaderFloorPlanarGrainnyVertexColor : ShaderFloorPlanarGrainny;
                return new[]
                {
                    new MaterialAndShaderQualityPresets
                    { quality = GameStaticData.EverguildQualitySetting.VeryLow, material = null, shader = null, shaderName = low },
                    new MaterialAndShaderQualityPresets
                    { quality = GameStaticData.EverguildQualitySetting.Low,     material = null, shader = null, shaderName = low },
                    new MaterialAndShaderQualityPresets
                    { quality = GameStaticData.EverguildQualitySetting.Medium,  material = null, shader = null, shaderName = grain },
                };
            }

            /// <summary>建场期挂载（`ArenaBuilder` 调）：命中了表就 `AddComponent` + 填预设，返回是否挂了。
            /// <para>⚠️ **调用点必须用 `Undo.AddComponent` / 直接 `AddComponent` 在编辑期**（与
            /// `ArenaBuilder.AttachOriginalMaterial` 同形）；`Start()` 不会在编辑期跑，所以挂上去不会立刻改材质。</para>
            /// <para>🔴 **`priority` 那一格要在施工侧补**（原版 = 2/UI）：基类的 `priority` 是 private 序列化字段、
            /// 本仓没有 setter ⇒ 调用点加两句
            /// <c>var so = new SerializedObject(c); so.FindProperty("priority").intValue = 2; so.ApplyModifiedPropertiesWithoutUndo();</c>
            /// （`InitializationPriority.UI` = 2）。顺序上不依赖它，见文件头 ②。</para></summary>
            public static bool AttachForArena(MeshRenderer mr, string arena, string objectName)
            {
                Entry e;
                if (!TryMatch(arena, objectName, out e)) return false;
                if (mr == null) return false;
                var c = mr.gameObject.AddComponent<MaterialChangerByQuality>();
                c.SetPresets(BuildPresets(e.VertexColor));
                return true;
            }
        }
    }
}
