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

            var mr = GetComponent<MeshRenderer>();
            var old = mr != null ? mr.sharedMaterial : null;
            var mat = new Material(sh) { name = (old != null ? old.name : name) + "_orig" };
            CopyCommon(old, mat);
            if (_firstTex == null)
            {
                var t0 = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
                if (t0 == null && mat.HasProperty("_MainTex")) t0 = mat.GetTexture("_MainTex");
                _firstTex = t0 != null ? t0.name : "";
            }
            _ok++;
            ApplyProps(mat, props);
            ApplyRenderState(mat, cull, srcBlend, dstBlend, transparent, alphaClip,
                             blendAuthoritative, applyAmbientColor, queue);
            if (mr != null) mr.sharedMaterial = mat;
            return mat;
        }

        /// <summary>把「构建期那份材质」上的贴图搬过来（属性名两边都试：`URP/Unlit` 是 `_BaseMap`，原版这批是 `_MainTex`）。
        /// 颜色不搬 —— `props` 里有原版真值，会盖过它。</summary>
        public static void CopyCommon(Material from, Material to)
        {
            if (from == null || to == null) return;
            var tex = from.HasProperty("_BaseMap") ? from.GetTexture("_BaseMap") : null;
            if (tex == null && from.HasProperty("_MainTex")) tex = from.GetTexture("_MainTex");
            if (tex == null) return;
            if (to.HasProperty("_BaseMap")) to.SetTexture("_BaseMap", tex);
            if (to.HasProperty("_MainTex")) to.SetTexture("_MainTex", tex);
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
