// WarpforgeEffectBinder.cs — 挂在导出的特效 prefab 根上，进游戏时把占位材质换成原版 shader 重建的材质
//
// 导出器会把「原材质名 + 原 shader 名 + 全部属性值 + 贴图引用」烘进这个组件，
// 并按渲染器遍历顺序记录每个材质槽用哪个材质定义。运行时 Awake 里重建即可。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;   // `VertexAttribute.Color`（逐渲染器顶点色分流要用）

namespace WarpforgeVFX
{
    [Serializable]
    public class WFMatDef
    {
        public string name;
        public string shader;
        public int renderQueue = -1;
        public string[] keywords = new string[0];
        public string[] floatNames = new string[0];
        public float[] floatVals = new float[0];
        public string[] colorNames = new string[0];
        public Color[] colorVals = new Color[0];
        public string[] texNames = new string[0];
        public Texture[] texVals = new Texture[0];

        /// <summary>**原版这个材质自己带不带 `_EMISSION`**（导出时从 bundle 材质的 `m_ValidKeywords` 读的）。
        ///
        /// 为什么要有它：`emissionOn` 是**逐效果**的（见 `WarpforgeEffectBinder.emissionOn`），
        /// 而一个效果里往往只有一部分材质该开 ⇒ 光按效果开会把**本来没开的材质**也点亮。
        /// 实测（2026-09-18）：只按 `emissionOn` 开 ⇒ 修好 88 条、却又把 **48 条原本「对得上」的**
        /// 打成偏亮（47 条都是「效果的 `emissionOn` 为真、但材质本身没这个关键字」）。
        /// ⇒ **两个条件取交集**：`emissionOn &amp;&amp; hadEmissionKeyword`。</summary>
        public bool hadEmissionKeyword;
    }

    [DisallowMultipleComponent]
    public class WarpforgeEffectBinder : MonoBehaviour
    {
        [Tooltip("去重后的材质定义")]
        public WFMatDef[] materials = new WFMatDef[0];

        [Tooltip("按 GetComponentsInChildren<Renderer>(true) 遍历顺序展开的材质槽下标，-1 表示该槽留空")]
        public int[] rendererSlots = new int[0];

        [Tooltip("同上顺序，每个渲染器的 trailMaterial 槽（非粒子为 -1）")]
        public int[] trailSlots = new int[0];

        /// <summary>这个效果在原版里**有没有在用 `_EMISSION`** —— 由导出器按实测表写进来
        /// （`工具/gen_emission_flag.py` → `数据/游戏数据/emission_flag.json`）。
        ///
        /// **为什么是「逐效果」而不是逐材质、也不是按 Emission 模块判**：三种推断判据本轮全被实测推翻
        /// （详见 `资料/普查产出_0918/E组_共享资产筛_与EMISSION线索.md` §十）：
        ///   · 「`_EMISSION` 是全局的 ⇒ 两侧等效」 —— 被 Y 条件推翻（开回来让 5 个效果**正好回到 1.0000**）；
        ///   · 「按粒子系统 Emission 模块判」 —— 被 Z≡Y（13/13 完全相同）推翻；
        ///   · 按材质 `m_ValidKeywords`/`_EmissionColor` 猜 —— 逐效果准确率只有 71–78%。
        /// 唯一靠得住的是**直接量**：把原版那趟的 `_EMISSION` 关掉再渲一遍，数变了的 = 原版在用。
        ///
        /// 实测效果：偏暗那侧 91% 落在「在用」、偏亮那侧 88% 落在「没用」⇒ 这一位就把 E 组两侧分开了。</summary>
        public bool emissionOn;

        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        /// <summary>调试用：把每个原版 shader 解析到了哪里打出来</summary>
        public static bool LogResolve = true;

        /// <summary>🔬 实验覆盖（2026-09-18）：`WFBIND_EMISSION=1` → 重建材质时**无条件**打开 `_EMISSION`。
        ///
        /// ⚠️ **默认关，而且它已经不是正常路径了** —— 正常路径是 `Apply()` 里**逐渲染器按粒子系统的
        /// Emission 模块**判（那才是与原版一致的判据）。这个开关只留着做 A/B：
        /// 它复现的是「无条件开」那个条件（实测会让 `BlindEffect` 1.176 → 1.739 变坏）。
        ///
        /// **背景**：`EffectExporter.StripGlobalKeywords`（`WarpforgeArena1/Editor/EffectExporter.cs` 的 `StripGlobalKeywords`）在**导出时**
        /// 把 `_EMISSION` 从每个材质的 keywords 里剔掉了，于是「该不该开」这件事不重导 958 个 prefab 就没法 A/B。
        /// 判据、实测数据与实验设计见 `资料/普查产出_0918/E组_共享资产筛_与EMISSION线索.md` §四。</summary>
        public static readonly bool EnableEmissionKeyword =
            System.Environment.GetEnvironmentVariable("WFBIND_EMISSION") == "1";

        /// <summary>🔬 实验覆盖（2026-09-18）：`WFBIND_NOEMISSION=1` → 重建材质时**一律不开** `_EMISSION`。
        /// 用来量「某批效果的变化到底是不是这一位造成的」（不用重导 prefab 就能 A/B）。</summary>
        public static readonly bool DisableEmissionKeyword =
            System.Environment.GetEnvironmentVariable("WFBIND_NOEMISSION") == "1";

        /// <summary>🔬 实验覆盖（2026-10-01 晚）：`WFBIND_NOQUEUE=1` → 重建材质时**不覆盖** `renderQueue`。
        /// 用途 = A/B「队列是不是某个偏亮/偏暗的原因」。⚠️ 只在排查时用（正常路径照队列真值表）。</summary>
        public static readonly bool SkipRenderQueue =
            System.Environment.GetEnvironmentVariable("WFBIND_NOQUEUE") == "1";

        /// <summary>🔬 实验覆盖（2026-10-19 · T05）：`WFBIND_NO_PROP_FILTER=1` → **不过滤孤儿值**
        /// （退回改前的「`HasProperty` 就灌」行为）。
        /// 用途 = A/B「丢掉内置 Standard 残留值到底改变了哪些效果」，不用改代码、只换环境变量。</summary>
        public static readonly bool SkipPropFilter =
            System.Environment.GetEnvironmentVariable("WFBIND_NO_PROP_FILTER") == "1";

        /// <summary>这个属性名是不是**原版 shader 自己声明过**的。
        /// `declared == null` ⇒ 属性表读不到 ⇒ **放行**（不知道就别动，见 `Build` 里那段判据）。
        /// `X_ST` / `X_TexelSize` 是 Unity 按纹理隐式生成的，不在 `m_PropInfo` 里 ⇒ 按**基名** `X` 判。</summary>
        static bool DeclaredByOriginal(System.Collections.Generic.HashSet<string> declared, string name)
        {
            if (declared == null) return true;
            if (string.IsNullOrEmpty(name)) return false;
            if (declared.Contains(name)) return true;
            if (name.EndsWith("_ST", StringComparison.Ordinal)
                && declared.Contains(name.Substring(0, name.Length - 3))) return true;
            if (name.EndsWith("_TexelSize", StringComparison.Ordinal)
                && declared.Contains(name.Substring(0, name.Length - 10))) return true;
            return false;
        }

        /// <summary>🔬 实验覆盖（2026-10-19 · T04）：`WFBIND_NO_VERTEXCOLOR=1` → **不开**顶点色分流闸
        /// （`_VertexColorLerpOn` 恒 0 = 改前的旧行为）。
        /// 用途 = A/B「那 6 个带颜色通道的渲染器换了分支到底差多少」，**不用改代码**：
        ///   · 默认（不设）        ⇒ 新行为（网格有颜色通道的走 `lerp(tex, vcol, vcol.a)`）
        ///   · `WFBIND_NO_VERTEXCOLOR=1` ⇒ 旧行为（一律 `tex * _Color`）
        /// ⚠️ 只在排查时用。配合 `WFBIND_FORCE_BUILTIN="UnlitAmbient"` 才量得到（正常态走原件）。</summary>
        public static readonly bool SkipVertexColorLerp =
            System.Environment.GetEnvironmentVariable("WFBIND_NO_VERTEXCOLOR") == "1";
        static readonly HashSet<string> LoggedShaders = new HashSet<string>();

        /// <summary>🆕 2026-10-01：这个 prefab 的 `Animator` 在原版里用的**控制器名**（导出器写的）。
        ///
        /// 🔴 为什么要运行时再挂一次：bundle 里的控制器是**运行时格式**，落不成工程 `.controller`
        ///   （里面的 `AnimationClip` 是 muscle/streamed 格式，曲线数据根本不在包里）⇒
        ///   工程里那份是**曲线为空的占位**。真正播的动画由 `WarpforgeAnimatorBridge`
        ///   从 `StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle` 里把**原版那份**取回来换上。
        ///   详见 `WarpforgeAnimatorBridge.cs` 头部（含三条落盘路径的实测结果）。
        ///
        /// 空串 = 这个效果本来就没有动画（绝大多数效果都这样）。</summary>
        public string animatorController;

        void Awake() { Apply(); }

        /// <summary>重建并挂上材质。编辑器里也可以主动调，用来预览真实效果。</summary>
        public void Apply()
        {
            BindOriginalAnimator();                 // ⚠️ 必须在下面那个早退**之前**（没材质的效果也要挂动画）
            if (materials == null || materials.Length == 0) return;

            var rends = GetComponentsInChildren<Renderer>(true);
            var built = new Material[materials.Length];     // 不开 _EMISSION 的那一份
            var builtEm = new Material[materials.Length];   // 开了 _EMISSION 的那一份
            // 🔴 2026-10-19（T04）：`Everguild/UnlitAmbient` 的**顶点色分流**那一档。
            //   为什么必须另建一份、不能复用：`_VertexColorLerpOn` 是**逐渲染器**的
            //   （同一个材质挂在不同网格上，原版走的 VS 变体不同）⇒ 同一个 `WFMatDef` 需要两个材质实例。
            //   判据与后果 → `WFUnlitAmbient.shader` 里 `_VertexColorLerpOn` 的注释。
            var builtVc = new Material[materials.Length];
            var builtVcEm = new Material[materials.Length];
            int slot = 0, ri = 0;
            foreach (var r in rends)
            {
                // 🔴 `_EMISSION` **逐渲染器判**，别无条件开、也别一律剔掉。
                //
                //   原版是**引擎按粒子系统的 Emission 模块**开这个关键字的（URP 粒子 shader 的常规行为）
                //   ⇒ 同一个材质挂在不同发射器上时可以不同 ⇒ 判据只能在渲染器这一层取。
                //
                //   实测（`资料/普查产出_0918/E组_共享资产筛_与EMISSION线索.md` §四）：
                //     · 按 Emission 模块开 ⇒ `Buff_Red` 0.484 → **1.0000** · `Buff_Green` 0.624 → **1.0000**
                //       · `KhaineBuff` 0.498 → **1.0000** · `Buff_DiabolicStrength` 0.399 → **1.0000**
                //       —— **五个正好落在 1.0000**（缺的就是这一项）；
                //     · 而**无条件开**会让 `BlindEffect` 从 1.176 → **1.739（变坏）** ⇒ 判据不能一刀切。
                //
                //   ⚠️ 这也解释了 `EffectExporter.StripGlobalKeywords` 那条「剔掉它」为什么**两个方向都吃过亏**：
                //     剔掉 ⇒ 该开的丢了（上面那五个）；烘成局部 ⇒ 不该开的也开（`BlindEffect` 那一类）。
                // 🔴 `_EMISSION` 用**逐 prefab 的实测标记** `emissionOn`（见字段注释）。
                //    原先这里试过「逐渲染器按 Emission 模块判」—— **实测不成立**（Z≡Y，13/13 完全相同），已撤。
                bool em = emissionOn || EnableEmissionKeyword;

                int n = r.sharedMaterials.Length;
                if (n > 0)
                {
                    var arr = new Material[n];
                    for (int i = 0; i < n; i++)
                    {
                        int idx = (slot < rendererSlots.Length) ? rendererSlots[slot] : -1;
                        slot++;
                        if (idx >= 0 && idx < built.Length)
                        {
                            // 🔴 顶点色分流只对**基础 `Everguild/UnlitAmbient`** 生效（见 shader 里那段判据）：
                            //   Flickker 的 lerp 用的是 `v3` 且 `w = 0`（恒等）⇒ 不该开这个闸。
                            bool vc = !SkipVertexColorLerp && IsBaseUnlitAmbient(materials[idx]) && RendererHasVertexColor(r);
                            var m = vc
                                ? (em
                                    ? (builtVcEm[idx] ?? (builtVcEm[idx] = Build(materials[idx], true, true)))
                                    : (builtVc[idx] ?? (builtVc[idx] = Build(materials[idx], false, true))))
                                : (em
                                    ? (builtEm[idx] ?? (builtEm[idx] = Build(materials[idx], true)))
                                    : (built[idx] ?? (built[idx] = Build(materials[idx], false))));
                            if (m != null) { arr[i] = m; continue; }
                        }
                        arr[i] = r.sharedMaterials[i];     // 保持占位
                    }
                    r.sharedMaterials = arr;
                }

                var psr = r as ParticleSystemRenderer;
                if (psr != null && psr.trailMaterial != null)
                {
                    int idx = (ri < trailSlots.Length) ? trailSlots[ri] : -1;
                    if (idx >= 0 && idx < built.Length)
                    {
                        // 拖尾材质与它所属的效果同一档（`emissionOn` 是**逐效果**的实测结论，不是逐发射器）
                        var m = em
                            ? (builtEm[idx] ?? (builtEm[idx] = Build(materials[idx], true)))
                            : (built[idx] ?? (built[idx] = Build(materials[idx], false)));
                        if (m != null) psr.trailMaterial = m;
                    }
                }
                ri++;
            }

            // 🆕 2026-10-19（V2 / T10）：**重建材质之后必须让 VAT 驱动换一份缓存**。
            //   为什么：`WarpforgeVatDriver.Awake` 缓存的是 `renderer.materials`（实例材质），
            //   而上面 `r.sharedMaterials = arr` 换上去的是**另一批新建的材质**；组件顺序上驱动在
            //   本组件之前（prefab `m_Component` 第 4 项 = 驱动、第 5 项 = 本组件）⇒ 驱动每帧
            //   `SetFloat("_State")` / `SetTexture(...)` 全写进**已经不在渲染器上的孤儿材质**，
            //   而且**一声不响**（网格停在贴图第 0 行）。
            //   原版这件事本来就是**事件**做的：`Vanguard Frame Animation VAT` 上的
            //   `AnimFXModuleChangeMaterial.eventsToFire` 有一条 `VATGPUPlayer.UpdateMaterials(...)`。
            //   🔴 这里传 **`false`（= `renderer.materials`，逐实例）而不是 `true`（shared）**：
            //     本组件建出来的材质走的是**静态共享缓存**（`Cache`，键 = 材质名|shader）⇒ 传 `true`
            //     会让驱动把 `_State` / 两张顶点贴图写进**全效果共用的那一份**，串到别的 VAT 效果上；
            //     传 `false` 与驱动自己 `Awake` 里取的是同一档（`r.materials`），逐实例、不串。
            var _vatDriver = GetComponent<WarpforgeVatDriver>();
            if (_vatDriver != null) _vatDriver.UpdateMaterials(false);
        }

        /// <summary>这个材质定义的原版 shader 是不是**基础那份** `Everguild/UnlitAmbient`。
        ///
        /// 为什么只认它：顶点色分流那条判据只对基础那份成立 —— `… Emissive Flickker` 的 PS lerp 用的是 `v3`，
        /// 而它的 COLOR VS 支把 `o3` 写成 `(cb0[61].xyz, 0)`（`w = 0`）⇒ lerp 恒等，开闸没有意义。
        /// 判据全文 → `WFUnlitAmbient.shader` 里 `_VertexColorLerpOn` 的注释与 `frag` 那一段。</summary>
        static bool IsBaseUnlitAmbient(WFMatDef d)
        {
            return d != null && string.Equals(d.shader, "Everguild/UnlitAmbient", StringComparison.Ordinal);
        }

        /// <summary>这个渲染器所在网格**有没有顶点色通道**（= 原版 `Everguild/UnlitAmbient` 会走哪一支 VS）。
        ///
        /// 🔴 判据 = 网格自己的 `m_Channels` 里颜色通道是否有维度（`Mesh.HasVertexAttribute(Color)`）；
        ///   **不是**「这个 prefab 里有几个网格有颜色」—— 2026-10-12 那笔记的就是 prefab 级口径不可用
        ///   （判据 → `资料/历史/项目任务_第二十五会话_搬出段.md` ⑨）。
        /// ⚠️ **粒子渲染器一律返回 false**：粒子颜色由 `ParticleSystem` 每帧供，"网格有没有 COLOR" 推不出
        ///   `IN.color` 的补值是不是 `(1,1,1,1)`；而原版那 13 个 UnlitAmbient 材质都挂在普通网格上
        ///   ⇒ 宁可走旧路径（`tex * _Color`），也别冒「整片渲白」的风险。
        /// ⚠️ 找不到网格（`sharedMesh == null`）也返回 false —— 那种情况原版同样没有顶点色。</summary>
        static bool RendererHasVertexColor(Renderer r)
        {
            if (r == null) return false;
            var smr = r as SkinnedMeshRenderer;
            if (smr != null) return MeshHasColor(smr.sharedMesh);
            if (r is MeshRenderer)
            {
                var mf = r.GetComponent<MeshFilter>();
                return mf != null && MeshHasColor(mf.sharedMesh);
            }
            return false;
        }

        static bool MeshHasColor(Mesh m)
        {
            return m != null && m.HasVertexAttribute(VertexAttribute.Color);
        }

        /// <summary>把 `animatorController` 指定的那份**原版控制器**从重打的小包里取回来挂上。
        ///
        /// 判据与做法 → `WarpforgeAnimatorBridge.cs` 头部（含「三条落盘路径全不通」的实测）。
        /// 只做三件事：找 `Animator` → 按名字取原件 → 换上；**取不到就报错**（不静默）。
        /// 已经挂的是原件时（例如重复 `Apply()`）不再取第二次。</summary>
        void BindOriginalAnimator()
        {
            if (_animatorBound || string.IsNullOrEmpty(animatorController)) return;
            var anim = GetComponent<Animator>();
            if (anim == null)
            {
                Debug.LogWarning($"[WarpforgeEffectBinder] `{name}` 上写了控制器 `{animatorController}`，"
                               + "但根上没有 `Animator` 组件 ⇒ 动画不会播（导出器与 prefab 对不上？）");
                return;
            }
            var rc = WarpforgeAnimatorBridge.TryGet(animatorController);
            if (rc == null) return;                           // TryGet 已经报过错了
            anim.runtimeAnimatorController = rc;
            // 换了控制器要重新起状态机：`Rebind()` 让它按新的默认状态从 t=0 开始播。
            anim.Rebind();
            _animatorBound = true;                            // 只做一次（重复 `Apply()` 不重复取）
            if (LogResolve)
                Debug.Log($"[WarpforgeEffectBinder] `{name}` 挂上原版控制器 `{animatorController}`"
                        + $"（{rc.animationClips.Length} 个 clip）");
        }

        bool _animatorBound;

        /// <summary>
        /// **自检用**：把一个材质定义按真实路径建出来（走的是同一个 `Build`，**不另写一份**）。
        ///
        /// 为什么需要它：混合状态那条链路**踩过两次**（2026-09-12 P1-a0、2026-09-13 第三十三轮），
        /// 两次都是「判据选错 → 兜底不生效 → 加法发光变成不透明+写深度」。
        /// 要能在自检里**直接断言**建出来的材质是什么状态，就得有个口子把成品拿出来。
        /// </summary>
        public static Material BuildForProbe(WFMatDef d) { return Build(d); }

        /// <summary>同上，但能指定 `_EMISSION` 那一档 —— 自检要能断言「按 Emission 模块判」这条也生效。</summary>
        public static Material BuildForProbe(WFMatDef d, bool emissionOn) { return Build(d, emissionOn); }

        /// <summary>同上，再加**顶点色分流**那一档（`_VertexColorLerpOn`）—— 自检要能断言两条分支都建得出来。</summary>
        public static Material BuildForProbe(WFMatDef d, bool emissionOn, bool vertexColorLerp)
        { return Build(d, emissionOn, vertexColorLerp); }

        /// <param name="emissionOn">该材质挂着的那个发射器，Emission 模块是不是开着的。
        /// **由调用方（`Apply`）逐渲染器算出来传进来** —— 判据的解释与实测见 `Apply` 里的注释。</param>
        /// <param name="vertexColorLerp">该渲染器所在网格**有没有顶点色通道**（见 `RendererHasVertexColor`）。
        /// 只对基础 `Everguild/UnlitAmbient` 有意义 —— 判据 → `WFUnlitAmbient.shader` 的 `_VertexColorLerpOn`。</param>
        static Material Build(WFMatDef d, bool emissionOn = false, bool vertexColorLerp = false)
        {
            if (d == null) return null;
            string key = d.name + "|" + d.shader + (emissionOn ? "|em" : "") + (vertexColorLerp ? "|vc" : "");
            if (Cache.TryGetValue(key, out var c) && c != null) return c;

            Shader sh;
            string src;
            if (!WarpforgeShaderMap.TryResolve(d.shader, out sh, out src))
            {
                Debug.LogWarning($"[WarpforgeVFX] 找不到 shader '{d.shader}'（材质 {d.name}）");
                return null;
            }
            if (LogResolve && !LoggedShaders.Contains(d.shader))
            {
                LoggedShaders.Add(d.shader);
                Debug.Log($"[WarpforgeVFX] shader '{d.shader}' → {src} '{sh.name}' (id={sh.GetInstanceID()})");
            }

            var m = new Material(sh) { name = d.name };
            // 🆕 2026-10-19（T05）：「孤儿值」收口 —— **只灌原版 shader 自己声明过的属性**。
            //
            // 为什么需要：原版材质上常带着**内置 Standard 的残留值**（`_SrcBlend/_DstBlend/_ZWrite/_Surface/
            // `_Glossiness/_Metallic/_Mode` …）—— 原 shader 的属性表里**根本没有这些名字**，它们是**死值**。
            // 我们从前一律「`m.HasProperty(name)` 就灌」，判据只问**替代 shader 认不认这个名字** ——
            // 于是**同名不同义**时会静默灌错（`_EmissionColor` 那次把 13 个效果乘黑就是这个形状）。
            // 现在多一道闸：**原版 shader 的声明属性表**（`OriginalShaderBlendTable.OriginalProperties`，
            // 由 `工具/gen_shader_blend.py` 从 bundle 的 `m_PropInfo` 实读）里没有的名字 ⇒ 丢掉。
            //
            // ⚠️ 表读不到（没这个 shader / 属性表空）⇒ `declared == null` ⇒ **不过滤**（不许静默清空一切）。
            // ⚠️ `X_ST` / `X_TexelSize` 是 Unity 按纹理**隐式生成**的，不写进 `m_PropInfo` ⇒ 按基名 `X` 判。
            // 🔬 `WFBIND_NO_PROP_FILTER=1` 关掉这道闸（A/B 用）。
            var declared = SkipPropFilter ? null : OriginalShaderBlendTable.OriginalProperties(d.shader);
            for (int i = 0; i < d.floatNames.Length && i < d.floatVals.Length; i++)
                if (DeclaredByOriginal(declared, d.floatNames[i]) && m.HasProperty(d.floatNames[i]))
                    m.SetFloat(d.floatNames[i], d.floatVals[i]);
            for (int i = 0; i < d.colorNames.Length && i < d.colorVals.Length; i++)
                if (DeclaredByOriginal(declared, d.colorNames[i]) && m.HasProperty(d.colorNames[i]))
                    m.SetColor(d.colorNames[i], d.colorVals[i]);
            for (int i = 0; i < d.texNames.Length && i < d.texVals.Length; i++)
                if (DeclaredByOriginal(declared, d.texNames[i]) && m.HasProperty(d.texNames[i]) && d.texVals[i] != null)
                    m.SetTexture(d.texNames[i], d.texVals[i]);

            // 混合：**一律以 `WarpforgeShaderMap.InferBlend(原版 shader 名)` 为准**。
            //
            // 踩过两次，两次都是同一条根病 —— **判据选错了**：
            //   · 第一次（2026-09-12，P1-a0）：判「材质里有没有 `_SrcBlend`」。原版材质上带着一批
            //     **内置 Standard 的残留值**（`_SrcBlend=1`/`_DstBlend=0`/`_ZWrite=1`/`_Surface=0`），
            //     而原版 shader 的属性表里根本没有这些（混合写死在 pass 状态里）⇒ 那些值是**死值**。
            //     判据恒为真 ⇒ 兜底逻辑根本没机会跑。
            //   · 第二次（2026-09-13 第三十三轮，派子代理逐效果定根因时查出来）：
            //     改成判「**我们这边的 shader 认不认 `_SrcBlend`**」之后，P1-a0 那个案子是修好了，
            //     但 `WFParticlesExtraColor.shader:53-54` 自己就写着
            //     `Blend [_SrcBlend][_DstBlend]` + `ZWrite [_ZWrite]` —— **它认这个属性**，
            //     于是残留值又被灌进来 ⇒ 加法发光变成 **One/Zero 不透明 + 写深度**。
            //     实测重灾区：`line_light`(10 行) · `ray_light`(11) · `Fire1`(8) · `smokesoft_blend`(5)，
            //     子代理按技术构成统计出 **61 条**效果中这一条（精灵图 32 + Mesh/Matcap 29）。
            //
            // ⇒ 现在**不问材质、也不问我们这边认不认**：直接按**原版 shader 名**推出应有的混合状态，
            //    能推出来就写上去。推不出来的（`InferBlend` 返回 null）才让材质里的值留着 ——
            //    那种情况下原版 shader 的属性表确实是完整的，值也是活的。
            // ⚠️ 对我们没声明这些属性的 shader，`SetFloat` 是 **no-op**（状态在 pass 里写好了），
            //    所以「一律写」不会破坏 P1-a0 那次修好的东西。
            {
                var b = WarpforgeShaderMap.InferBlend(d.shader);
                if (b != null)
                {
                    if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", b[0]);
                    if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", b[1]);
                    if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", b[2]);
                    if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
                    if (b[0] == 1f) m.EnableKeyword("_ALPHAPREMULTIPLY_ON");   // One/OneMinusSrcAlpha
                }
            }

            // `_Cull`：**与 `InferBlend` 同一族问题、但它是独立的一档**（2026-10-02 晚补）。
            //   原版那几个内置粒子 shader 把 `Cull` **写死在 pass 状态里**（dump 出来 `name='<noninit>'`），
            //   材质上的 `_Cull` 是**死值**；我们那份 `Extra Color` 的默认是 2（Back）
            //   ⇒ `renderMode = Mesh` 的粒子**背面被剔掉一半**（实测三条效果、能量比 0.95）。
            //   **判据与实读表 → `WarpforgeShaderMap.InferCull` 的注释。**
            // ⚠️ 一定要放在 `InferBlend` 那个 `if` **外面**：两者是独立的判据，
            //    `InferBlend` 返回 null 不代表 cull 也该留着（`Particles/Standard Unlit` 就是）。
            {
                var cu = WarpforgeShaderMap.InferCull(d.shader);
                if (cu.HasValue && m.HasProperty("_Cull")) m.SetFloat("_Cull", cu.Value);
            }

            // 🆕 2026-10-19（T05）：**`zTest` 那一格终于有消费者了** —— 与上面 `_Cull` 同族的第三格。
            //   表从 2026-10-12 起就读到了 `zTest`（`OriginalShaderBlendTable.ZTestMap`），但一直没人查；
            //   我们自建替代 shader 的 `_ZTest` 默认是 4（LEqual），而原版有 11 条烘死值**不是 4**
            //   （`UI/Additive` 等 = 0 Disabled · `Hidden/VideoDecode` = 8 Always）。
            //   ⚠️ **只在表里查得到时才写**（= 原版 pass 里那一格是烘死的）。属性驱动的那种
            //     （`Particles/Standard Unlit` 的 pass2 读 `_ZTest`）**不在表里** ⇒ 保持材质活值，别覆盖。
            //   ⚠️ 目标 shader 没声明 `_ZTest` 时 `SetFloat` 是 no-op（状态写在 pass 里）—— 与上面同一条纪律。
            {
                var zt = WarpforgeShaderMap.InferZTest(d.shader);
                if (zt.HasValue && m.HasProperty("_ZTest")) m.SetFloat("_ZTest", zt.Value);
            }
            // 🔴 2026-10-12 新加：`Everguild/UnlitAmbient Emissive Flickker` 的「自发光闪烁」那一支。
            //
            // 为什么需要这一处：原版**两份** shader（`Everguild/UnlitAmbient` 与 `… Emissive Flickker`）
            // 在我们这边都映射到同一个自建 shader（本文件 `Replacements` 的那三行）。而闪烁那一支
            // **只在 Flickker 那份里** —— 把两份 shader 的 DXBC blob 分开数：Flickker 32 段里 3 段带闪烁块，
            // 基础那份 336 段里 **0 段**（逐条指令与槽位实读 → `Shaders/WFUnlitAmbient.shader` 文件头「2026-10-12」）。
            // 而且它在原版里**不是 keyword 门控的**（4 个前向变体逐字都有它）⇒ **没有现成的材质数据可依**，
            // 只能按【原版 shader 名】开。
            //
            // 🔑 **为什么用浮点开关、不用 keyword**：keyword 在 player 构建里可能被剥掉变体，而这个 keyword
            //    **没有材质数据能替它保活**（`_APPLYAMBIENTCOLOR` / `_ALPHATEST_ON` 那批是**原版材质自己带的**，
            //    这一条不是 —— 原版根本没有这个关键字）。浮点开关同样能单变量 A/B，且不增变体数。
            if (m.HasProperty("_EmissiveFlickerOn"))
                m.SetFloat("_EmissiveFlickerOn",
                    d.shader == "Everguild/UnlitAmbient Emissive Flickker" ? 1f : 0f);

            // 🔴 2026-10-19 新加（T04）：基础 `Everguild/UnlitAmbient` 的**顶点色分流闸**。
            //   判据与为什么不能用 keyword → `WFUnlitAmbient.shader` 里 `_VertexColorLerpOn` 的注释：
            //   原版基础 PS 是 `lerp(tex.rgb, v2.rgb, v2.w) * _Color.rgb`，而 `v2` 是不是真顶点色
            //   **取决于网格有没有 COLOR 流**（Unity 按网格选 VS 变体）⇒ 只能逐渲染器判。
            //   ⚠️ 对所有材质都写一次是无害的：没声明这个属性的 shader `SetFloat` 是 no-op。
            if (m.HasProperty("_VertexColorLerpOn"))
                m.SetFloat("_VertexColorLerpOn", vertexColorLerp ? 1f : 0f);

            foreach (var k in d.keywords)
                if (!string.IsNullOrEmpty(k)) m.EnableKeyword(k);

            ApplyDerivedParticleDefaults(m, d.shader,
                new HashSet<string>(d.floatNames ?? new string[0]),
                new HashSet<string>(d.colorNames ?? new string[0]));

            // `_EMISSION`：**两个条件取交集**（见 `WFMatDef.hadEmissionKeyword` 的注释）：
            //   ① `emissionOn`  = 逐效果的实测标记（这个效果的画面在原版里响不响应 emission）
            //   ② `d.hadEmissionKeyword` = 原版这个材质自己带不带这个关键字
            // 只按 ① 开会把本来没开的材质点亮（实测把 48 条原本「对得上」的打成偏亮）。
            // 🔬 `WFBIND_EMISSION=1` 仍是无条件覆盖（实验用，复现 Y 条件）。
            if (!DisableEmissionKeyword && ((emissionOn && d.hadEmissionKeyword) || EnableEmissionKeyword))
            {
                if (m.HasProperty("_EmissionColor")) m.EnableKeyword("_EMISSION");
            }
            else if (m.HasProperty("_EmissionColor")) m.DisableKeyword("_EMISSION");

            // 🔬 `WFBIND_NOQUEUE=1`（2026-10-01 晚加）→ **不覆盖** `renderQueue`（保留 shader 自己的队列）。
            //    用途 = A/B「队列是不是某个效果偏亮的原因」。第一例：`RemnantBody3D Aeldari` 那 16×
            //    （`EffectCompare` 的 exp 侧队列 3000、orig 侧 2000，其余属性逐项相同）。
            //    ⚠️ **只在排查时用**：正常路径照 `WFMatDef.renderQueue`（队列真值表，`EffectExporter.TruthQueue`）。
            if (!SkipRenderQueue && d.renderQueue >= 0) m.renderQueue = d.renderQueue;

            Cache[key] = m;
            return m;
        }

        /// <summary>补上 URP 粒子 shader 的「派生属性」。
        ///
        /// 这几个属性原版材质里根本没序列化 —— 它们是 ParticleSystemRenderer 每次渲染时
        /// 按粒子的 Soft Particles / Camera Fading 模块现算、临时塞给材质的。
        /// 我们新建的 Material 拿到的是**默认值 0**，而 shader 会拿它们直接乘 alpha：
        ///
        ///     col.a *= saturate((sceneEye - partEye) / _SoftParticleFadeParams.y);
        ///     col.a *= saturate((_CameraFadeParams.y - partEye) / _CameraFadeParams.w);
        ///
        /// `_SoftParticleFadeParams.y` 默认 0 → 除法饱和成 0 → **整个发射器渲染成全黑**。
        /// 实测：Back Glow 原版 7420 亮像素、导出 0，就是这条。
        ///
        /// 这里的取值对齐 URP 的默认状态：软粒子关、相机淡出关。
        /// 注意不要把 _EMISSION 一起处理 —— 那是全局关键字，不属于这一组。</summary>
        public static void ApplyDerivedParticleDefaults(Material m) => ApplyDerivedParticleDefaults(m, null, null, null);

        /// <param name="originalShaderName">原版 shader 名。传 null 表示按目标 shader 名判断。
        /// 只有「原版确实是粒子 shader」或「目标确实是粒子 shader」才补这套值 ——
        /// 非粒子材质（比如 UnlitAmbient 掉到默认 URP Particles/Unlit 的）被补上会直接过曝。</param>
        /// <param name="recordedFloats">WFMatDef 里**已经记到**的 float 属性名。
        /// 传了的话就只补「没记到的」，已记到的一律不覆盖 —— 见下面 SetF/SetV 的注释。</param>
        /// <param name="recordedColors">同上，针对 Vector/Color 属性。</param>
        public static void ApplyDerivedParticleDefaults(Material m, string originalShaderName,
                                                        HashSet<string> recordedFloats = null,
                                                        HashSet<string> recordedColors = null)
        {
            if (m.shader == null) return;

            // ⚠️ 判据里用 "Particle"（单数）而不是 "Particles"（复数）。
            //    原版有一大批 shader 叫 `Everguild/FX/Particle Premultiply` / `Particle Dissolve Mask` /
            //    `Particle Shine Custom Vertex Streams` —— 全是**单数**，而且自建的
            //    `WarpforgeVFX/FX/Distortion` 也没有 "Particles/" 这一段。
            //    写成复数时这些**全都匹配不上**，于是 isParticle=false 直接 return，
            //    派生属性一个都不补。
            bool looksParticle = originalShaderName != null &&
                (originalShaderName.IndexOf("Particle", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 originalShaderName.IndexOf("Extra Color", System.StringComparison.OrdinalIgnoreCase) >= 0);

            bool isParticle = looksParticle ||
                m.shader.name.IndexOf("Particles/", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isParticle) return;

            // ⚠️ 只补「原版材质里根本没序列化、由 ParticleSystemRenderer 每次渲染现算」的那几个。
            //    已经被 WFMatDef 记到的**绝不能覆盖** —— 那些是原版的真实值。
            //
            //    这条是踩出来的：把 isParticle 改成能匹配单数之后，本来记不到属性的材质现在都记到了
            //    （`_SoftParticlesEnabled=1` / `_CameraFadingEnabled=1` 都是原版的真值），
            //    而这里**无条件**把它们改写成 0 → 原本正常的导出集体变亮。
            //    实测：偏亮从 101 涨到 130、比值 >2.5 的极端簇从 36 涨到 53，
            //    总达标数反而从 500 掉到 464。兜底逻辑只该在「真缺」的时候生效。
            void SetF(string n, float v)
            {
                if (recordedFloats != null && recordedFloats.Contains(n)) return;
                if (m.HasProperty(n)) m.SetFloat(n, v);
            }
            void SetV(string n, Vector4 v)
            {
                if (recordedColors != null && recordedColors.Contains(n)) return;
                if (m.HasProperty(n)) m.SetVector(n, v);
            }

            // 软粒子：关掉，并把淡出距离设成有效值（万一材质上还挂着 _SOFTPARTICLES_ON）
            SetF("_SoftParticlesEnabled", 0f);
            SetV("_SoftParticleFadeParams", new Vector4(0f, 1f, 1f, 0f));
            SetF("_SoftParticlesNearFadeDistance", 0f);
            SetF("_SoftParticlesFarFadeDistance", 1f);

            // 相机淡出：关掉，远端设成「无穷远」
            SetF("_CameraFadingEnabled", 0f);
            SetV("_CameraFadeParams", new Vector4(0f, 2000f, 1f, 0f));
            SetF("_CameraNearFadeDistance", 1f);
            SetF("_CameraFarFadeDistance", 2f);

            // 顶点色模式：默认 0 = 相乘，和原版一致
            SetF("_ColorMode", 0f);
            SetV("_BaseColorAddSubDiff", new Vector4(1f, 0f, 0f, 0f));
        }
    }
}
