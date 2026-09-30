// ArenaOriginalMaterial.cs — 战场网格的「原版材质重建器」（**运行时**）
//
// 🔴 **为什么非要运行时不可**（2026-09-21 查实，别再走弯路）：
//    AssetBundle 里的 Shader **落不了工程资产** —— 建 Material 再 `AssetDatabase.CreateAsset(mat, …)` 时
//    **shader 引用会变成空 GUID**（`0000…`），重新导入就变成 `Hidden/InternalErrorShader`
//    ⇒ **整屏洋红**（实测复现：`WF_ORIGSHADER=1 ArenaBuilder.BuildFromCLI` + 渲染 = 全屏洋红）。
//    ⚠️ 所以旧注释那句「原版这批网格 shader 在我们工程里跑不起来」**是错的**：死因是**资产序列化**，
//    不是渲染。实证：探针 `ArenaBuilder.ProbeOriginalShader` 里 9 个原版 shader **全部**
//    `isSupported=True` · 编译消息 0 · `mat.SetPass(0)=True`。
//    ⇒ 唯一可行路径：**把「用哪个 shader + 全部属性值」烘进这个组件**，运行时 `new Material(shader)` 重建。
//
// 数据从哪来：`ArenaBuilder` 建场时从清单（`gen_unity_arena_manifest.py` 的 `props` 字段）灌进来。
// ⚠️ `props` 里 `t=="c"` 的那批同时在装 **Color** 和 **Vector4** 两种属性
//    （`_Color` vs `_MainTex_ST`）—— **建场时就把类型定死成 `"c"`/`"v"`**
//    （编辑器里能问 `ShaderUtil`，运行时问不了），运行时只管照 `t` 设，不再猜。
using System;
using UnityEngine;
using UnityEngine.Rendering;   // `RenderQueue`（透明那一档要显式设 renderQueue）

namespace WarpforgeVFX
{
    /// <summary>原版材质的一条属性。⚠️ 是**数组**不是字典 —— 场景/清单都是 `JsonUtility` 走的，它不吃字典。</summary>
    [Serializable]
    public class MatProp
    {
        /// <summary>属性名。</summary>
        public string k;
        /// <summary>`"f"` 标量（读 <see cref="f"/>）· `"c"` Color · `"v"` Vector4（读 <see cref="c"/>）。</summary>
        public string t;
        public float f;
        public float[] c;
    }

    /// <summary>
    /// 挂在一个战场网格（`MeshRenderer`）上：运行时用**原版 shader + 原版属性值**重建它的材质。
    ///
    /// 失败时**保持原材质**（构建期建的 `URP/Unlit` 那份）并报警 —— **绝不静默**。
    /// </summary>
    [DisallowMultipleComponent]
    public class ArenaOriginalMaterial : MonoBehaviour
    {
        [Tooltip("原版 shader 名（如 Everguild/Unlit Wind）。空 / 取不到 ⇒ 不动，保持构建期那份材质。")]
        public string shaderName;

        public MatProp[] props;

        // 渲染状态（照 `ApplyRenderState` 的判据）
        public int cull = 2, srcBlend = 5, dstBlend = 10;
        /// <summary>🆕 2026-09-22 晚：**原版材质写死的 `m_CustomRenderQueue`**（`-1` = 用 shader 的 tag）。
        /// 为什么必须带：**透明物体的绘制顺序由它决定**，而 arena1 那一族的真值是
        /// `results-mat` **2450** · `results-wind` **3000** · **`results-FX` **3002****
        /// （见 `数据/游戏数据/mat_renderqueue.tsv`）。原来这条没进清单 ⇒ 我们一律写 3000
        /// ⇒ 两张「烘焙光斑」（`Barrels Light FX` / `Generator 2 FX`）与烟雾/阴影接收板**打平**。</summary>
        public int queue = -1;
        public bool transparent, alphaClip, blendAuthoritative, applyAmbientColor;

        /// <summary>关掉它就完全不重建（A/B 用）。</summary>
        public bool useOriginal = true;

        /// <summary>🆕 2026-09-25：**主贴图之外的贴图槽**（`_NoiseTex1` / `_MinTex` / `_SecondaryTex` / `_MatCap` …）。
        /// 数据来自 `arenas/&lt;场&gt;/&lt;场&gt;_texslots.json`（工具 `工具/gen_arena_texslots.py`），
        /// 由 `ArenaBuilder.AttachOriginalMaterial` 灌进来。
        ///
        /// 🔴 **为什么不能在建场期就贴上去**：建场期那份材质是 **`URP/Unlit`** 兜底（原版 shader 那条路
        ///    默认关着），它的属性表里**根本没有这些槽** —— 实测一次 `BuildAll` 会打 **50 条**
        ///    「没有槽 `_NoiseTex1`… 跳过」。真正用原版 shader 的是**这里**。
        /// 症状（不接就是它）：`battlearenadarkangels` 的天**品红** —— 同一点我们 `(180,118,190)`、
        ///    原版 `(62,112,187)`；`WF_HIDE==Background space noise` 后我们 `(63,110,186)` ⇒ 根因钉死。</summary>
        public string[] slotNames;
        public Texture[] slotTexs;

        // 🔴 **2026-09-22 晚加：一次性汇总（真包验证用）。**
        //   **为什么要它**：`Rebuild()` 原来**只在失败时出警告**，成功是**静默**的 ⇒ 真包里
        //   「组件没跑 / 跑失败 / 跑成功」三种结局在日志上**分不出来**。
        //   2026-09-22 真包验证踩到：arena1 整片战场是黑的（编辑器里正常），
        //   而日志里一条 `[Arena/OS]` 都没有 ⇒ 白查了一轮。
        //   ⇒ 一行汇总把三种结局摊开 —— **不许静默失败**（`项目任务.md` §一 工作方式）。
        static int _ok, _fail, _skip;
        static string _firstTex;

        void Awake() { Rebuild(); }

        /// <summary>每次场景加载完打一行汇总。
        /// ⚠️ **不能只在 `AfterSceneLoad` 打一次** —— 那只覆盖**启动场景**，而 player 后面还会
        /// `LoadScene` 切到 Battle（2026-09-22 实测：第一版探针就是这么打出一个假的「0/0/0」）。
        /// ⚠️ 也不能用 `Application.delayCall` —— **Unity 6.3 里没有这个成员**（CS0117，同日踩过）。
        /// 汇总里带 `场景里有 N 个组件`：用来区分「**组件不在包里**」与「**在但 Awake 没跑**」。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void HookRebuildSummary()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (sc, _) => LogRebuildSummary(sc.name);
            LogRebuildSummary("<启动场景>");
        }

        static void LogRebuildSummary(string where)
        {
            int found = FindObjectsByType<ArenaOriginalMaterial>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            Debug.Log($"[Arena/OS] 材质重建汇总（{where}）：场景里有 {found} 个组件 · "
                    + $"重建成功 {_ok} · 失败 {_fail} · 跳过 {_skip}"
                    + $" · 首个材质的贴图 = {(_firstTex == null ? "<一个都没重建>" : _firstTex.Length == 0 ? "<空！属性在但贴图为 null>" : _firstTex)}");
        }

        /// <summary>重建并挂上材质。返回新材质；没做成返回 null（调用方不用管，原材质还在）。</summary>
        public Material Rebuild()
        {
            if (!useOriginal || string.IsNullOrEmpty(shaderName)) { _skip++; return null; }

            Shader sh;
            if (!WarpforgeShaderLoader.TryGetShader(shaderName, out sh) || sh == null)
            {
                Debug.LogWarning($"[Arena/OS] 取不到原版 shader `{shaderName}`（{name}）—— 保持构建期那份材质", this);
                _fail++;
                return null;
            }

            // 🆕 2026-09-25：**精灵也走这条路**（`SpriteRenderer`）—— 原版那批 `Fake Light Glow`
            //    用的是 `Universal Render Pipeline/Particles/Unlit` 与 `Everguild/FX/Extra Color`；
            //    建场期只能用兜底 shader（原版 shader 运行时才加载得进来、存不进场景）。
            Renderer mr = GetComponent<MeshRenderer>();
            if (mr == null) mr = GetComponent<SpriteRenderer>();
            // 🆕 **2026-09-30（⑨）：粒子也走这条路** —— 原版粒子材质的关键字/属性槽
            //    （`_NOISE1CHANNEL_R` 那族）在**建场期**设了也没用（那时只能用兜底的 `URP/Particles/Unlit`，
            //    它没有那两个槽）；而**运行时**不落盘、可以用 bundle 里的原版 shader ⇒ 这里也认
            //    `ParticleSystemRenderer`。判据 → `资料/战场13场_逐场对账_0920.md` §一 ①-m 的第 1 条。
            if (mr == null) mr = GetComponent<ParticleSystemRenderer>();
            var old = mr != null ? mr.sharedMaterial : null;
            var mat = new Material(sh) { name = (old != null ? old.name : name) + "_orig" };
            // 🆕 2026-09-28 诊断：`WF_SHADOWTEST=1` —— 把 shader 名里带 `shadows receiver` 的材质
            //   换成 `URP/Lit`（它**一定**收实时阴影），用来判「**阴影图到底渲没渲**」。
            //   背景：blacklegion 前景比原版亮 +29~+37，而那批网格用的 shader 就叫
            //   `Everguild/Misc/Unlit shadows receiver`（材质带 `_ShadowColor`）；我们把投射者打开后画面
            //   **逐格几乎不变**（mean 48.89→48.85）⇒ 要么阴影图是空的、要么这份 shader 没采。
            //   这个开关只回答「阴影图活不活」，**是诊断不是修法**。
            if (System.Environment.GetEnvironmentVariable("WF_SHADOWTEST") == "1"
                && shaderName != null && shaderName.ToLower().Contains("shadows receiver"))
            {
                var lit = Shader.Find("Universal Render Pipeline/Lit");
                if (lit != null) { mat.shader = lit; Debug.Log($"[Arena/OS] WF_SHADOWTEST：`{name}` 换成 URP/Lit"); }
            }
            CopyCommon(old, mat);
            ApplySlots(mat);
            if (_firstTex == null)
            {
                var t0 = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
                if (t0 == null && mat.HasProperty("_MainTex")) t0 = mat.GetTexture("_MainTex");
                if (t0 == null && slotTexs != null && slotTexs.Length > 0) t0 = slotTexs[0];
                _firstTex = t0 != null ? t0.name : "";
            }
            _ok++;
            ApplyProps(mat, props);
            ApplyRenderState(mat, cull, srcBlend, dstBlend, transparent, alphaClip,
                             blendAuthoritative, applyAmbientColor, queue);
            // 🆕 2026-09-28 诊断开关：`WF_MESHKEYWORDS=_SOFT[,<kw>…]` —— 把原版材质的**关键字**补到网格材质上。
            //
            // 🔴 **为什么要有它**：清单的 `meshes[]` **不带 `matKeywords`**（只有 `particles[]` 带），
            //    ⇒ **网格这条路一个原版关键字都没设过**，而 `ApplyRenderState` 只管
            //    `_SURFACE_TYPE_TRANSPARENT` / `_ALPHABLEND_ON` / `_ALPHATEST_ON` / `_APPLYAMBIENTCOLOR`
            //    这四个自算的，**原版自己开的关键字一律丢**。
            //    实测受害：`battlearenaleviathan` 的 `Toxic Pool Glow`（材质 `Toxic Pool Up light`，
            //    `bundle_battlesharedresources_assets_all/Material/Material_-6239187414147824738.json`）
            //    原版 `m_ValidKeywords = ['_SOFT','_SURFACE_TYPE_TRANSPARENT']`、`_EMISSION` 在 `m_InvalidKeywords`（**关**）；
            //    我们只设了后者 ⇒ 那颗网格把整屏罩成一片亮黄绿（**leviathan 亮度比的 2/3 出在它身上**）。
            //    ⚠️ **这是诊断开关，不是修法** —— 真修要走「生成器/旁挂把 mesh 的关键字也带出来」那条路。
            var mk = System.Environment.GetEnvironmentVariable("WF_MESHKEYWORDS");
            if (!string.IsNullOrEmpty(mk))
            {
                foreach (var kw in mk.Split(','))
                {
                    var k = kw.Trim();
                    if (k.Length > 0) mat.EnableKeyword(k);
                }
                Debug.Log($"[Arena/OS] WF_MESHKEYWORDS：给 `{name}` 补了 [{mk}]（诊断）");
            }
            if (mr != null) mr.sharedMaterial = mat;
            // 🔴 **顺序坑（⑨）**：`ArenaParticleKeywords.Apply()` 干的是「**复制当时的材质** + 设关键字」
            //    ⇒ 它若先跑、我们这里后换材质，**关键字就丢了**（`_EMISSION` 那族是 `+0.48/通道` 的差别）。
            //    关键字表仍然**只有 `ArenaParticleKeywords` 那一处**（别在这儿抄第二份）——
            //    这里只是**换完材质之后叫它再补一次**。
            if (mr is ParticleSystemRenderer)
            {
                var kw = GetComponent<ArenaParticleKeywords>();
                if (kw != null) kw.Apply();
            }
            return mat;
        }

        /// <summary>把「构建期那份材质」上的贴图搬过来。
        /// 颜色不搬 —— `props` 里有原版真值，会盖过它。
        ///
        /// 🆕 **2026-09-25：改成搬「所有贴图槽」，不再只搬 `_BaseMap` / `_MainTex` 那两张。**
        ///   原版有一批材质**主贴图是空的、内容全在别的槽里** —— 实测 `battlearenadarkangels` 的
        ///   `Background space noise`：材质 `Glow Space Dark Angels` 的 `_MainTex` 就是 `FileID 0`，
        ///   真正的数据在 **`_NoiseTex1` / `_NoiseTex2`**。只搬两张的旧写法会把这类材质**整个丢空**
        ///   （画面上就是那片**品红**的天：`WF_HIDE==Background space noise` 后与原版逐值吻合到
        ///   `(63,110,186)` vs `(62,112,187)` —— 铁证）。</summary>
        public static void CopyCommon(Material from, Material to)
        {
            if (from == null || to == null) return;
            var names = from.GetTexturePropertyNames();
            if (names == null || names.Length == 0) return;
            foreach (var p in names)
            {
                if (string.IsNullOrEmpty(p) || !to.HasProperty(p)) continue;
                var t = from.GetTexture(p);
                if (t != null) to.SetTexture(p, t);
            }
        }

        /// <summary>把旁挂表点名的**非主贴图槽**贴到重建出来的材质上。槽不在就**报出来**（不许静默）。</summary>
        void ApplySlots(Material mat)
        {
            if (mat == null || slotNames == null || slotTexs == null) return;
            int n = Mathf.Min(slotNames.Length, slotTexs.Length);
            for (int i = 0; i < n; i++)
            {
                if (string.IsNullOrEmpty(slotNames[i]) || slotTexs[i] == null) continue;
                if (!mat.HasProperty(slotNames[i]))
                {
                    Debug.LogWarning($"[Arena/OS] {name}: 原版 shader `{shaderName}` 上没有槽 `{slotNames[i]}`"
                                   + $"（要求贴 `{slotTexs[i].name}`）—— 跳过");
                    continue;
                }
                mat.SetTexture(slotNames[i], slotTexs[i]);
            }
        }

        /// <summary>
        /// 把清单里的整张属性表灌进材质。**按原版材质的原值**，不猜。
        ///
        /// 🔴 **一律用 `SetVector`，不许用 `SetColor`**（2026-09-21 实测）：
        ///    工程是 **Linear** 色彩空间（`ProjectSettings.m_ActiveColorSpace: 1`），而
        ///    `Material.SetColor` 会**再做一次 sRGB→线性转换**；可原版材质的序列化值
        ///    （`m_SavedProperties.m_Colors`）**本来就是给 shader 的原值** —— 再转一次是**双重转换**。
        ///    · `_Color` = (1,1,1) 时转不转都一样 ⇒ **所以这个错一直没被发现**；
        ///    · `_EmissiveColor` = (1.604, 0.41, 0, 0)（sororitas 烛光，HDR）转完 ≈ (2.9, 0.15, 0) ⇒
        ///      **蜡烛亮出整整一档**（实测：换 shader 后 sororitas 亮度比 1.138 → 1.215，画面上一排蜡烛爆亮）。
        ///    另：`LinearToSRGB(_Color)` 出现在原版 shader 的 frag 里 —— 说明它**期望 `_Color` 是线性的**，
        ///    灌原值正好对上。
        /// </summary>
        public static void ApplyProps(Material mat, MatProp[] props)
        {
            if (mat == null || props == null) return;
            foreach (var p in props)
            {
                if (p == null || string.IsNullOrEmpty(p.k) || !mat.HasProperty(p.k)) continue;
                if (p.t == "v" || p.t == "c")
                {
                    var v = (p.c != null && p.c.Length >= 4)
                          ? new Vector4(p.c[0], p.c[1], p.c[2], p.c[3]) : Vector4.zero;
                    mat.SetVector(p.k, v);          // ⚠️ **不是 SetColor** —— 见上面的双重转换
                }
                else mat.SetFloat(p.k, p.f);
            }
        }

        /// <summary>从**原版 shader 名**推断混合 —— legacy 粒子 shader（`Mobile/Particles/Additive` 等）
        /// 把混合**写死在 pass 里**，材质上根本没有 `_SrcBlend/_DstBlend` 属性。</summary>
        /// <remarks>**判据只此一处** —— `EffectExporter.InferFromShader` 与粒子那条路（`ArenaBuilder`）共用，
        /// 两边都只是转发到这里。值取 `UnityEngine.Rendering.BlendMode`：
        /// `0=Zero · 1=One · 2=DstColor · 5=SrcAlpha · 10=OneMinusSrcAlpha`。</remarks>
        public static void InferBlendFromShaderName(string shaderName,
            out int srcBlend, out int dstBlend, out float zwrite, out bool transparent)
        {
            string n = (shaderName ?? "").ToLowerInvariant();
            if (n.Contains("additive") || n.Contains("/add") || n.Contains(" add "))
            { srcBlend = 5; dstBlend = 1;  zwrite = 0f; transparent = true; return; }
            if (n.Contains("premultiply"))
            { srcBlend = 1; dstBlend = 10; zwrite = 0f; transparent = true; return; }
            if (n.Contains("multiply"))
            { srcBlend = 2; dstBlend = 0;  zwrite = 0f; transparent = true; return; }
            if (n.Contains("alpha blended") || n.Contains("transparent"))
            { srcBlend = 5; dstBlend = 10; zwrite = 0f; transparent = true; return; }
            srcBlend = 1; dstBlend = 0; zwrite = 1f; transparent = false;   // 不明就按不透明
        }

        /// <summary>`props` 里的混合三件套是不是**内置 Standard 的残留值**。
        ///
        /// 🔴 **为什么要问这一句**（2026-09-25 实查）：原版有些材质是**用 Standard 建的、后来换了 shader**
        /// （换成了 `Mobile/Particles/Additive` 这类把混合写死在 pass 里的 legacy 粒子 shader）
        /// ⇒ 材质上仍留着 Standard 的 `_SrcBlend=1(One) / _DstBlend=0(Zero) / _ZWrite=1`，**全是死值**。
        /// 而我们的自建 shader 是 `Blend[_SrcBlend][_DstBlend]` **间接寻址** ⇒ 照搬死值 = **渲染成不透明**
        /// （`CLAUDE.md` §三 那条「原版材质上带着内置 Standard 的残留值」的**同一个坑**）。
        ///
        /// **判据** = 「`_SrcBlend==1 && _DstBlend==0` **且** 属性表里没有 `_Surface`」——
        /// URP 那批的反向指纹是**有** `_Surface`/`_Blend`，所以不会被误判；
        /// 与 `ApplyRenderState(dst, src)` 里 `explicitOpaque` 那一档**同一条判据**。
        ///
        /// **踩过**：arena3 `Additional Glow`（材质 `citclr_light`，原版 shader `Mobile/Particles/Additive`）
        /// 渲成**一大块绿方块**（逐块有符号差 **+107.75**）—— `ring4` 那张图是**靠 alpha 成环**的，
        /// 不透明渲染就把整块 quad 的绿色铺满。**同族 34 颗 / 6 场**（`Mobile/Particles/Additive` 30 ·
        /// `Legacy Shaders/Particles/Alpha Blended Premultiply` 3 · `Mobile/Particles/Alpha Blended` 1）。</summary>
        public static bool BlendPropsAreStandardResidue(MatProp[] props)
        {
            if (props == null || props.Length == 0) return false;
            bool hasSurface = false, hasSrc = false, hasDst = false;
            float src = 0f, dst = 0f;
            foreach (var p in props)
            {
                if (p == null || string.IsNullOrEmpty(p.k)) continue;
                if (p.k == "_Surface") hasSurface = true;
                else if (p.k == "_SrcBlend") { hasSrc = true; src = p.f; }
                else if (p.k == "_DstBlend") { hasDst = true; dst = p.f; }
            }
            return hasSrc && hasDst && !hasSurface
                && Mathf.Approximately(src, 1f) && Mathf.Approximately(dst, 0f);
        }

        /// <summary>这 7 个是 Unity **内置管线**的粒子 shader —— 混合**写死在 pass 里**
        /// （`Mobile/Particles/Additive` = `Blend SrcAlpha One` · `…/Alpha Blended` = `Blend SrcAlpha OneMinusSrcAlpha` ·
        /// `…/Multiply` = `Blend DstColor Zero` · `Legacy Shaders/Particles/Alpha Blended Premultiply` = `Blend One OneMinusSrcAlpha`），
        /// **材质上根本不声明 `_SrcBlend/_DstBlend`** ⇒ 材质里若出现这两个键，一律是**内置 Standard 的残留值**。
        ///
        /// **判据来源**：`BuiltinShaderProbe.LegacyNames` 那份 8 个名单，**去掉 `Particles/Standard Unlit`**
        /// —— 它属于 Standard 家族、**确实读** `_SrcBlend`/`_DstBlend`，不能算进来。
        ///
        /// ⚠️ **别把 `InferFromShader` 无差别套到所有 shader 上**：它对认不出的名字返回「不透明」，
        /// 而 `Everguild/FX/*` 那 180 颗的混合属性是**权威的**（实测 `_Surface` 179/179 都有）。</summary>
        public static bool IsLegacyBuiltinParticleShader(string shaderName)
        {
            switch (shaderName)
            {
                case "Mobile/Particles/Additive":
                case "Mobile/Particles/Alpha Blended":
                case "Mobile/Particles/Multiply":
                case "Legacy Shaders/Particles/Additive":
                case "Legacy Shaders/Particles/Alpha Blended":
                case "Legacy Shaders/Particles/Alpha Blended Premultiply":
                case "Legacy Shaders/Particles/Anim Alpha Blended":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>粒子那条路（`ArenaBuilder.GetOrCreateParticleMaterial`）该不该**丢掉 props 里的混合、改从 shader 名推断**。
        ///
        /// 两条触发条件，**满足任一条即触发**：
        ///  ① **原版 shader 是那 7 个 legacy 内置粒子 shader**（见 `IsLegacyBuiltinParticleShader`）
        ///     —— 它们的混合写死在 pass 里，props 里有没有 `_SrcBlend/_DstBlend` 都不可信；
        ///  ② **props 是内置 Standard 的残留值**（`_SrcBlend==1 && _DstBlend==0` 且没有 `_Surface`）
        ///     —— 与 `ApplyRenderState(dst, src)` 里 `explicitOpaque` 那一档同一条判据
        ///     （防的是「非 legacy 名、但同样被 Standard 残留值污染」的材质）。
        ///
        /// **实测触发面（13 场全量普查）**：`Mobile/Particles/Additive` **30 颗 / 6 个材质** ·
        /// `Legacy Shaders/Particles/Alpha Blended Premultiply` **3 颗**（`Default-Particle`）·
        /// `Mobile/Particles/Alpha Blended` **1 颗**（`smokesoft_blend`）—— 合计 **34 颗 / 6 场**。
        /// 反向指纹很干净：URP + Everguild 那 **544 颗**全部**有** `_Surface` ⇒ 一颗都不会被误伤。</summary>
        public static bool ShouldInferParticleBlend(MatProp[] props, string originalShaderName)
            => IsLegacyBuiltinParticleShader(originalShaderName) || BlendPropsAreStandardResidue(props);

        /// <summary>设混合 + `_ZWrite` + 两个关键字（`_SURFACE_TYPE_TRANSPARENT` / `_ALPHAPREMULTIPLY_ON`）+ 队列。
        /// **判据只此一处** —— `EffectExporter.SetBlend` 与粒子那条路（`ArenaBuilder`）共用。</summary>
        public static void SetBlend(Material m, int srcBlend, int dstBlend, float zwrite)
        {
            if (m == null) return;
            if (m.HasProperty("_Surface"))   m.SetFloat("_Surface", zwrite < 0.5f ? 1f : 0f);
            if (m.HasProperty("_SrcBlend"))  m.SetFloat("_SrcBlend", (float)srcBlend);
            if (m.HasProperty("_DstBlend"))  m.SetFloat("_DstBlend", (float)dstBlend);
            // 🆕 2026-09-27（⑷）：**alpha 侧也要设** —— 我们的 shader 是
            //   `Blend [_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]`（**间接寻址**），
            //   只设彩色侧的话 alpha 侧会留 shader 默认值（`WarpforgeVFX/Particles/Extra Color` 是 1/0，
            //   URP 的 `ParticlesUnlit` 也是 1/0）⇒ **彩色对、alpha 通道错**。
            //   而这一族原版用的是 **legacy 内置粒子 shader 的单条写法** ——
            //   `Blend SrcAlpha One` 这种**一条 `Blend` 对彩色与 alpha 同时生效** ⇒ alpha 侧 = 彩色侧。
            //   （实测面：tau 那 8 颗原版 5/1、我们留 1/0。）
            //   ⚠️ 预乘那一档（1/10）本来就该两侧一致，所以「照抄彩色侧」在两种情况下都对。
            if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)srcBlend);
            if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)dstBlend);
            if (m.HasProperty("_ZWrite"))    m.SetFloat("_ZWrite", zwrite);
            // 🔴 **预乘混合必须开 `_ALPHAPREMULTIPLY_ON`**（2026-09-19 补，E 组第三轮）。
            //   原版那批材质里 `_SrcBlend=1(One) + _DstBlend=10(OneMinusSrcAlpha)` 就是**预乘 alpha**，
            //   而着色器里那一段 `col.rgb *= col.a` 挂在 `#ifdef _ALPHAPREMULTIPLY_ON` 下
            //   ⇒ **不开这个关键字 = 按未预乘输出 ⇒ 偏亮**（alpha 越小倍数越大：a=0.2 时 5×）。
            if (srcBlend == 1 && dstBlend == 10) m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            else                                 m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            if (zwrite < 0.5f) { m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");  m.renderQueue = (int)RenderQueue.Transparent; }
            else               { m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = -1; }
        }

        /// <summary>
        /// 原版 shader 的渲染状态。**判据只此一处** —— 建场那条路（`ArenaBuilder`）与运行时重建共用。
        ///
        /// ⚠️ **`blendAuthoritative=false` 时不要设 `_SrcBlend/_DstBlend`** —— 那个 shader 的属性表里
        ///    没声明这两个（pass 里硬编码），材质上存的是内置 Standard 的**残留值**，照着设等于拿垃圾值覆盖
        ///    （踩过：`Floor#sub1` 明确 `1/0` 被拉成透明 ⇒ 整块地板透掉）。
        /// </summary>
        public static void ApplyRenderState(Material mat, int cull, int srcBlend, int dstBlend,
                                            bool transparent, bool alphaClip, bool blendAuthoritative,
                                            bool applyAmbientColor, int queue = -1)
        {
            if (mat == null) return;
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", transparent ? 1f : 0f);
            if (blendAuthoritative)
            {
                if (mat.HasProperty("_Blend"))    mat.SetFloat("_Blend", 0f);          // 0 = Alpha
                if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)srcBlend);
                if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)dstBlend);
            }
            if (mat.HasProperty("_ZWrite"))    mat.SetFloat("_ZWrite", transparent ? 0f : 1f);
            if (mat.HasProperty("_Cull"))      mat.SetFloat("_Cull", cull);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", alphaClip ? 1f : 0f);
            if (alphaClip) mat.EnableKeyword("_ALPHATEST_ON"); else mat.DisableKeyword("_ALPHATEST_ON");

            if (transparent)
            {
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHABLEND_ON");
                // 不透明**不动 renderQueue** —— 让它落回 shader 子着色器的 tag（这批多是 `AlphaTest` 2450）。
            }
            // 🆕 2026-09-22 晚：**原版写死的队列优先**（`-1` = 没写，用上面那条兜底）。
            //   判据在 `数据/游戏数据/mat_renderqueue.tsv`：arena1 那族是 2450 / 3000 / **3002**。
            if (queue > 0) mat.renderQueue = queue;
            if (applyAmbientColor) mat.EnableKeyword("_APPLYAMBIENTCOLOR");
            else                   mat.DisableKeyword("_APPLYAMBIENTCOLOR");
        }
    }
}
