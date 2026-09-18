// WarpforgeEffectBinder.cs — 挂在导出的特效 prefab 根上，进游戏时把占位材质换成原版 shader 重建的材质
//
// 导出器会把「原材质名 + 原 shader 名 + 全部属性值 + 贴图引用」烘进这个组件，
// 并按渲染器遍历顺序记录每个材质槽用哪个材质定义。运行时 Awake 里重建即可。
using System;
using System.Collections.Generic;
using UnityEngine;

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
        /// ⇒ **两个条件取交集**：`emissionOn && hadEmissionKeyword`。</summary>
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
        /// **背景**：`EffectExporter.StripGlobalKeywords`（`EffectExporter.cs:374-391`）在**导出时**
        /// 把 `_EMISSION` 从每个材质的 keywords 里剔掉了，于是「该不该开」这件事不重导 958 个 prefab 就没法 A/B。
        /// 判据、实测数据与实验设计见 `资料/普查产出_0918/E组_共享资产筛_与EMISSION线索.md` §四。</summary>
        public static readonly bool EnableEmissionKeyword =
            System.Environment.GetEnvironmentVariable("WFBIND_EMISSION") == "1";

        /// <summary>🔬 实验覆盖（2026-09-18）：`WFBIND_NOEMISSION=1` → 重建材质时**一律不开** `_EMISSION`。
        /// 用来量「某批效果的变化到底是不是这一位造成的」（不用重导 prefab 就能 A/B）。</summary>
        public static readonly bool DisableEmissionKeyword =
            System.Environment.GetEnvironmentVariable("WFBIND_NOEMISSION") == "1";
        static readonly HashSet<string> LoggedShaders = new HashSet<string>();

        void Awake() { Apply(); }

        /// <summary>重建并挂上材质。编辑器里也可以主动调，用来预览真实效果。</summary>
        public void Apply()
        {
            if (materials == null || materials.Length == 0) return;

            var rends = GetComponentsInChildren<Renderer>(true);
            var built = new Material[materials.Length];     // 不开 _EMISSION 的那一份
            var builtEm = new Material[materials.Length];   // 开了 _EMISSION 的那一份
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
                            var m = em
                                ? (builtEm[idx] ?? (builtEm[idx] = Build(materials[idx], true)))
                                : (built[idx] ?? (built[idx] = Build(materials[idx], false)));
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
        }

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

        /// <param name="emissionOn">该材质挂着的那个发射器，Emission 模块是不是开着的。
        /// **由调用方（`Apply`）逐渲染器算出来传进来** —— 判据的解释与实测见 `Apply` 里的注释。</param>
        static Material Build(WFMatDef d, bool emissionOn = false)
        {
            if (d == null) return null;
            string key = d.name + "|" + d.shader + (emissionOn ? "|em" : "");
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
            for (int i = 0; i < d.floatNames.Length && i < d.floatVals.Length; i++)
                if (m.HasProperty(d.floatNames[i])) m.SetFloat(d.floatNames[i], d.floatVals[i]);
            for (int i = 0; i < d.colorNames.Length && i < d.colorVals.Length; i++)
                if (m.HasProperty(d.colorNames[i])) m.SetColor(d.colorNames[i], d.colorVals[i]);
            for (int i = 0; i < d.texNames.Length && i < d.texVals.Length; i++)
                if (m.HasProperty(d.texNames[i]) && d.texVals[i] != null) m.SetTexture(d.texNames[i], d.texVals[i]);

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

            if (d.renderQueue >= 0) m.renderQueue = d.renderQueue;

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
