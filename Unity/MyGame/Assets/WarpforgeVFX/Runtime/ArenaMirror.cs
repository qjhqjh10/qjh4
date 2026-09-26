// ArenaMirror.cs — 战场的**平面反射**（照原版 `kTools.Mirrors.Mirror` 的做法重建）
//
// 🔴 **为什么需要它**（2026-09-25 查实）：
//   地板材质 `Ground` 用的 shader 是 `Everguild/FX/Floor Planar Reflections Grainny`，
//   它除了 `_BaseMap` 还采样一张 **`_ReflectionMap`** —— 而那张图**不在材质属性表里**
//   （原版是 `CommandBuffer.SetGlobalTexture` 灌的**全局贴图**）⇒ 我们这边**一直是采到默认贴图**，
//   表现为「**地板反射偏强**」：`WF_NOREFL=1` 一比就露（`battlearena3` 整图比 1.103 → 1.027）。
//   ⚠️ 材质**数值**本身与原版**逐值一致**（38 floats + 8 colors + `_BaseMap`，46 项 0 差异）⇒ 别再调数值。
//
// **原版怎么做的**（判据 = 本地反编译 `d:/2/tools/decomp_full/kTools.Mirrors.Mirror__*.c`，31 个方法体）：
//   · 组件挂在场景里那台 `reflection camera` 上（13 场里 **8 场有**：arena3 / aeldari / darkangels /
//     emperorschildren / genestealers / sororitas / spacewolves / tauviorla）；
//   · `GetMirrorPlane` 用的是 `Transform.get_forward` 当**镜面法线**，再沿它偏移 `m_Offset`(=0.01)；
//   · `RenderReflection` 渲进一张 `textureScale` 倍的 RT，再 `SetGlobalTexture` 灌下去；
//   · 档位实读（`HKCU\Software\Everguild\Warpforge` 的 `UnityGraphicsQuality` = **2**）：
//     `enabled 1` · `grainyReflections 1` · `textureScale 0.3` · `blurIterations 1`。
//
// **本实现做了 / 没做**：
//   ✅ 镜像相机（位置与朝向都关于镜面镜像）· 用主相机的**投影矩阵**（我们那台是物理相机，
//      `fieldOfView` 被忽略 —— 直接抄 `projectionMatrix` 才稳）· 反剔除（标准平面反射做法）·
//      渲进 RT · **两级 Blit**（原版那两台 shader，2026-09-26 接上）· 全局贴图 `_ReflectionMap`。
//   ✅ **2026-09-26 补的两件**（原来记在「没做」里）：`blurIterations`/`grainyReflections` 那两样
//      —— 现在**不是自建近似**，而是原版那两台 shader 本体（见 `ExecuteCommand`）：
//      `Hidden/Everguild/PlanarReflectionsBlit`（深度淡出 `_DepthFadeBottom/Top`）与
//      `Hidden/Everguild/GrainyBlurForPlanarReflections`（颗粒模糊 `_Iterations`/`_BlurRadius`）。
//      四个数值来自**原版组件的序列化字段**（不是从反编译抄的，反编译把值那个实参丢了）。
//   ❌ **仍然没做**：原版的**斜切近平面**（`Camera.CalculateObliqueMatrix`）——
//      试过、**已回退**（见 `RenderReflection` 里那段注释：地板按**屏幕 UV** 采 `_ReflectionMap`，
//      斜切会把投影歪掉 ⇒ 反射与屏幕对不齐，实测三场反而变差）。
//      ⚠️ 但**原版是用的**（`__RenderReflection.c:131`）⇒ 这是个**已知偏离**，不是「原版没有」。
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;   // 汇总那行用

namespace WarpforgeVFX
{
    [DisallowMultipleComponent]
    public class ArenaMirror : MonoBehaviour
    {
        /// <summary>镜面一点（世界坐标）。数据 = `arenas/&lt;场&gt;/&lt;场&gt;_mirror.json`（工具 `工具/gen_arena_mirror.py`）。</summary>
        public float[] planePos = { 0f, 0f, 0f };
        /// <summary>镜面法线（世界坐标，原版取那台对象的 `forward`）。</summary>
        public float[] planeNormal = { 0f, 1f, 0f };
        /// <summary>RT 相对屏幕的比例（原版档 2 = **0.3**）。</summary>
        public float textureScale = 0.3f;
        /// <summary>档位里的 `blurIterations`（档 2 = **1**）—— **C# 这一层**的意思（模糊做几遍）。
        /// ⚠️ **别和 `shaderIterations` 混**：那是喂给 shader 的 `_Iterations`（见下）。</summary>
        public int blurIterations = 1;
        public bool grainy = true;

        // ---- 🆕 2026-09-26：**两个 Blit 材质要喂的四个数** ----
        //    判据（唯一）= 原版 `Mirror` 组件**自己的序列化字段**：
        //    `m_depthFadeBottom` / `m_depthFadeTop` / `m_blurIterations` / `m_blurRadius`
        //    （实读 `scenes_scenes_<场>.bundle` 里那个 MonoBehaviour；8 场逐字段相同）。
        //    为什么不能从反编译抄：`__ExecuteCommand.c` 里 `Material.SetFloat(id, ?)` 的**值那个实参被
        //    Ghidra 丢了**（只留下 PropertyToID 的 id）—— 靠猜就会把 0.832/0.002 编成别的数。
        //    四个 id 的来源：`__ctor.c:22-31`（`Shader.PropertyToID`），名字由字面量表解出
        //    （`_DepthFadeBottom` / `_DepthFadeTop` / `_Iterations` / `_BlurRadius`）。
        /// <summary>`_DepthFadeBottom`（原版字段 `m_depthFadeBottom`，8 场都是 **0.832**）。</summary>
        public float depthFadeBottom = 0.832f;
        /// <summary>`_DepthFadeTop`（原版字段 `m_depthFadeTop` = **1.0**）。</summary>
        public float depthFadeTop = 1f;
        /// <summary>喂给 shader 的 `_Iterations`（原版字段 `m_blurIterations` = **3**；
        /// ⚠️ **不是**档位里的 `blurIterations`（=1），两个字段、两回事）。</summary>
        public int shaderIterations = 3;
        /// <summary>`_BlurRadius`（原版字段 `m_blurRadius` = **0.002**）。</summary>
        public float blurRadius = 0.002f;

        static readonly int PropId        = Shader.PropertyToID("_ReflectionMap");
        static readonly int DepthBottomId = Shader.PropertyToID("_DepthFadeBottom");
        static readonly int DepthTopId    = Shader.PropertyToID("_DepthFadeTop");
        static readonly int IterationsId  = Shader.PropertyToID("_Iterations");
        static readonly int BlurRadiusId  = Shader.PropertyToID("_BlurRadius");

        Camera _refl;
        RenderTexture _rt;          // 原版 `+0x68`：反射相机渲进去的那张
        RenderTexture _rtBlit;      // 原版 `+0x70`：两级 Blit 的中转（**同尺寸同格式**）
        Material _blitMat;          // `Hidden/Everguild/PlanarReflectionsBlit`
        Material _blurMat;          // `Hidden/Everguild/GrainyBlurForPlanarReflections`
        int _lastFrame = -1;
        bool _rendering;
        static int _rendered;      // 汇总用（「不许静默」）

        bool _hooked;

        void OnEnable() { Apply(); }

        /// <summary>显式挂上钩子（幂等）。
        /// 🔴 **编辑器批处理里 `OnEnable` 不会跑**（没标 `ExecuteAlways`）—— 与 `ArenaOriginalMaterial`
        ///    同一个坑：预览/量测路径必须由 `ArenaBuilder.PrepareSceneMeasure()` 显式调一次，
        ///    否则组件「建了、但从不渲」而且**一声不响**（2026-09-25 实测：arena3 建了反射相机、
        ///    渲出来与没建**逐像素一样**）。</summary>
        public void Apply()
        {
            if (_hooked) return;
            _hooked = true;
            RenderPipelineManager.beginCameraRendering += OnBegin;
            Debug.Log($"[Arena/Mirror] Apply：钩子挂上了（{name}）");
        }

        void OnDisable()
        {
            if (_hooked) RenderPipelineManager.beginCameraRendering -= OnBegin;
            _hooked = false;
            if (_rt != null) { _rt.Release(); DestroyImmediate(_rt); _rt = null; }
            if (_rtBlit != null) { _rtBlit.Release(); DestroyImmediate(_rtBlit); _rtBlit = null; }
            if (_blitMat != null) { DestroyImmediate(_blitMat); _blitMat = null; }
            if (_blurMat != null) { DestroyImmediate(_blurMat); _blurMat = null; }
            if (_refl != null) { DestroyImmediate(_refl.gameObject); _refl = null; }
        }

        void OnBegin(ScriptableRenderContext ctx, Camera cam)
        {
            // ⚠️ **别用 `cam.targetTexture != null` 当守卫** —— 预览路径那台相机正是渲进 RT 的，
            //    加那条守卫会把它一起排除掉（实测：反射一点没生效、还不报错）。
            //    防递归只靠两样：`cam == _refl` 与 `_rendering` 标志。
            if (cam == null || cam.cameraType != CameraType.Game) return;
            if (_refl != null && cam == _refl) return;
            if (_rendering) return;
            var add = cam.GetUniversalAdditionalCameraData();
            // ⚠️ 这个 URP 版本里属性叫 `renderType`（**不是** `cameraType` —— 2026-09-25 编译报错才对出来，
            //    判据 `Library/PackageCache/com.unity.render-pipelines.universal@*/Runtime/UniversalAdditionalCameraData.cs:567`）
            if (add != null && add.renderType == CameraRenderType.Overlay) return;   // UI 那台跳过
            if (Time.frameCount == _lastFrame) return;                       // 一帧只渲一次
            _lastFrame = Time.frameCount;

            EnsureTargets(cam);
            RenderReflection(ctx, cam);
            // 🆕 2026-09-26：**逐句照搬原版 `kTools.Mirrors.Mirror` 那条链**
            //    （`__BeginCameraRendering.c:186-187`：`RenderReflection(...)` → `ExecuteCommand(...)`）。
            //    ⚠️ **顺序不能换** —— `ExecuteCommand` 里那个 blit 材质采 `_CameraDepthTexture`，
            //    而那正是**上一步反射相机自己渲出来的深度**（原版给那台设了 `depthTextureMode = Depth`）。
            ExecuteCommand(cam);
            _rendered++;
            if (_rendered <= 2)
                Debug.Log($"[Arena/Mirror] 渲了第 {_rendered} 帧：主相机={cam.name} · 镜像位={_refl.transform.position} · "
                        + $"RT={_rt.width}×{_rt.height} · 两级 Blit {(HasMaterials ? "已接" : "**没接**（缺 shader/材质）")}"
                        + $" · 全局贴图 {PropId} 已设");
        }

        void EnsureTargets(Camera cam)
        {
            int w = Mathf.Max(16, Mathf.RoundToInt(cam.pixelWidth * Mathf.Clamp(textureScale, 0.05f, 1f)));
            int h = Mathf.Max(16, Mathf.RoundToInt(cam.pixelHeight * Mathf.Clamp(textureScale, 0.05f, 1f)));
            if (_rt == null || _rt.width != w || _rt.height != h)
            {
                if (_rt != null) { _rt.Release(); DestroyImmediate(_rt); }
                if (_rtBlit != null) { _rtBlit.Release(); DestroyImmediate(_rtBlit); }
                // 两张 RT **同尺寸同格式**（原版 `__BeginCameraRendering.c:158,180` 用的是**同一份
                // `RenderTextureDescriptor`** 各 `GetTemporary` 一次）。
                _rt = new RenderTexture(w, h, 16, RenderTextureFormat.DefaultHDR)
                { name = "ArenaReflection", useMipMap = false, autoGenerateMips = false };
                _rt.Create();
                _rtBlit = new RenderTexture(w, h, 0, RenderTextureFormat.DefaultHDR)
                { name = "ArenaReflectionBlit", useMipMap = false, autoGenerateMips = false };
                _rtBlit.Create();
            }
            if (_refl == null)
            {
                var go = new GameObject("reflection camera (ours)");
                go.transform.SetParent(transform, false);
                _refl = go.AddComponent<Camera>();
                _refl.enabled = false;                                       // 只手动 Render，不进相机栈
                // 原版给那台设的是 `cameraType = Reflection`（`__OnEnable.c:96` 的 `set_cameraType(1)`）——
                // 这样它**天然不进 URP 的相机栈**，不必靠 `enabled=false` 一个人扛。
                _refl.cameraType = CameraType.Reflection;
                var rad = _refl.GetUniversalAdditionalCameraData();
                if (rad != null) { rad.renderPostProcessing = false; rad.renderShadows = true; }
            }
            _refl.targetTexture = _rt;
            // 🔴 **2026-09-26 补**：原版给反射相机设了 `depthTextureMode = Depth`
            //    （`__OnEnable.c:108` 的 `set_depthTextureMode(1)`）—— 这不是可选项：
            //    `PlanarReflectionsBlit` 那个材质**要采 `_CameraDepthTexture`** 做深度淡出，
            //    而这张深度图正是**反射相机自己这一趟**渲出来的。不设的话那两级 Blit 里的深度淡出
            //    取到的是上一帧/主相机的深度（画面会**静默**不对）。
            _refl.depthTextureMode = DepthTextureMode.Depth;
        }

        /// <summary>两个 Blit 材质都建出来了吗（建不出来要**出声**，不许静默退回旧行为）。</summary>
        public bool HasMaterials { get { return _blitMat != null && _blurMat != null; } }

        /// <summary>建那两个材质 —— 两台 shader 来自 `wf_arena_mirror.bundle`
        /// （`工具/extract_mirror_shaders.py` 从 `globalgamemanagers.assets` 搬出来的，
        /// 走 `WarpforgeShaderLoader` 的通配 `wf_arena_*.bundle`）。</summary>
        void EnsureMaterials()
        {
            if (_blitMat != null && _blurMat != null) return;
            Shader b = null, g = null;
            WarpforgeShaderLoader.TryGetShader("Hidden/Everguild/PlanarReflectionsBlit", out b);
            WarpforgeShaderLoader.TryGetShader("Hidden/Everguild/GrainyBlurForPlanarReflections", out g);
            if (b == null || g == null)
            {
                Debug.LogWarning($"[Arena/Mirror] ⚠️ 两级 Blit 的原版 shader **没拿到**"
                               + $"（PlanarReflectionsBlit={(b == null ? "缺" : "有")} · "
                               + $"GrainyBlur={(g == null ? "缺" : "有")}）"
                               + " —— 反射会退回「只降采样升采样」，**比原版亮**。"
                               + " 检查 `StreamingAssets/WarpforgeVFX/wf_arena_mirror.bundle` 在不在。");
                return;
            }
            _blitMat = new Material(b) { name = "ArenaMirror Blit" };
            _blurMat = new Material(g) { name = "ArenaMirror GrainyBlur" };
            // `_ReflectionMap_ST` 设成**恒等**（1,1,0,0）。
            // 理由：Unity 只在 shader **声明了属性**（`m_Props`）时才替你把 `_ST` 填好，而这两台
            // Hidden shader 的 `m_Props` **是空的**（属性表被剥）⇒ 不设就是默认值。
            // ⚠️ **实测（2026-09-26）：这一条对亮度比【没有影响】**（设之前/之后同为 117.52）。
            //    留着是因为「采样 uv 被默认值打成一团」这类隐患**只会在别的机器/别帧现形**，
            //    设成恒等是零代价的保险 —— **但它不是缺口，别在别处把它写成「修好了偏亮」**。
            var st = new Vector4(1f, 1f, 0f, 0f);
            _blitMat.SetVector("_ReflectionMap_ST", st);
            _blurMat.SetVector("_ReflectionMap_ST", st);
        }

        /// <summary>原版 `kTools.Mirrors.Mirror.ExecuteCommand`（`__ExecuteCommand.c`）**逐句照搬**：
        /// <code>
        ///   blitMat.SetFloat(_DepthFadeBottom, m_depthFadeBottom);   // &amp;+0xbc
        ///   blitMat.SetFloat(_DepthFadeTop,    m_depthFadeTop);      // &amp;+0xc0
        ///   SetGlobalTexture(_ReflectionMap, RT_渲);                  // +0x68
        ///   Blit(→ RT_blit, blitMat);                                // +0x70
        ///   SetGlobalTexture(_ReflectionMap, RT_blit);
        ///   if (grainy) {                                            // 档位 +0x15
        ///     blurMat.SetFloat(_Iterations,  m_blurIterations);      // +0xc8
        ///     blurMat.SetFloat(_BlurRadius,  m_blurRadius);          // +0xcc
        ///     Blit(→ RT_渲, blurMat);
        ///     SetGlobalTexture(_ReflectionMap, RT_渲);
        ///   }
        /// </code>
        /// 🔴 **两级 Blit 的「源」是无关的**：两台 shader 采的都是**全局** `_ReflectionMap`
        ///   （`__ExecuteCommand.c` 里 `Blit` 那个源实参就是 0 —— 反编译和 shader 的常量表**两边都印证**了
        ///   这一点：它们谁都没用 `_MainTex`）。真正决定结果的是**目标 RT** 与**全局贴图**这两样。
        /// ⚠️ 原版那次 `SetGlobalTexture` 用的 id 是 `_ReflectionMap`（`+0xc4`，字面量表解出）——
        ///    **不是** `_MainTex`。</summary>
        void ExecuteCommand(Camera cam)
        {
            EnsureMaterials();
            if (!HasMaterials)
            {
                // 不静默：退回旧的「降采样升采样」当模糊，并把「这是退回」写在日志里（上面 EnsureMaterials 已出声）
                BlurFallback();
                Shader.SetGlobalTexture(PropId, _rt);
                return;
            }

            var cmd = new CommandBuffer { name = "ArenaMirror" };
            _blitMat.SetFloat(DepthBottomId, depthFadeBottom);
            _blitMat.SetFloat(DepthTopId, depthFadeTop);
            cmd.SetGlobalTexture(PropId, _rt);
            cmd.Blit(_rt, _rtBlit, _blitMat);
            cmd.SetGlobalTexture(PropId, _rtBlit);

            if (grainy)
            {
                _blurMat.SetFloat(IterationsId, shaderIterations);
                _blurMat.SetFloat(BlurRadiusId, blurRadius);
                cmd.Blit(_rtBlit, _rt, _blurMat);
                cmd.SetGlobalTexture(PropId, _rt);
            }
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }

        /// <summary>**退回**路径（两台原版 shader 拿不到时才会走到）：双线性**降采样再升采样**当模糊。
        /// 调用方会把「这是退回、比原版亮」写进日志（`EnsureMaterials` 里那句 warn）。</summary>
        void BlurFallback()
        {
            if (blurIterations <= 0 || _rt == null) return;
            int w = _rt.width, h = _rt.height;
            var small = RenderTexture.GetTemporary(Mathf.Max(8, w / 2), Mathf.Max(8, h / 2), 0, _rt.format);
            var back  = RenderTexture.GetTemporary(w, h, 0, _rt.format);
            try
            {
                Graphics.Blit(_rt, small);        // 降采样（双线性 = 糊一次）
                Graphics.Blit(small, back);       // 升采样回来
                Graphics.Blit(back, _rt);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(small);
                RenderTexture.ReleaseTemporary(back);
            }
        }

        /// <summary>把世界空间的镜面换算到**这台相机的视图空间**，给 `Camera.CalculateObliqueMatrix` 用。</summary>
        static Vector4 CameraSpacePlane(Camera c, Vector3 pos, Vector3 normal, float sideSign)
        {
            Vector3 offset = pos + normal * 0.01f;                 // 抬一点，避免自身 z-fighting
            var m = c.worldToCameraMatrix;
            Vector3 cpos = m.MultiplyPoint(offset);
            Vector3 cn = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cn.x, cn.y, cn.z, -Vector3.Dot(cpos, cn));
        }

        void RenderReflection(ScriptableRenderContext ctx, Camera cam)
        {
            var n = new Vector3(planeNormal[0], planeNormal[1], planeNormal[2]);
            if (n.sqrMagnitude < 1e-8f) n = Vector3.up; else n.Normalize();
            var p0 = new Vector3(planePos[0], planePos[1], planePos[2]);

            Vector3 MirrorPoint(Vector3 v) => v - 2f * Vector3.Dot(v - p0, n) * n;
            Vector3 MirrorDir(Vector3 v) => v - 2f * Vector3.Dot(v, n) * n;

            _refl.transform.SetPositionAndRotation(
                MirrorPoint(cam.transform.position),
                Quaternion.LookRotation(MirrorDir(cam.transform.forward), MirrorDir(cam.transform.up)));
            // ⚠️ **抄投影矩阵**，不是抄 fieldOfView —— 我们那台是物理相机（`focalLength`/`sensorSize` 说了算）
            // 🔴 **斜切近平面试过、已回退**（2026-09-25）：`CalculateObliqueMatrix` 会把投影**歪掉**，
            //    而地板 shader 是按**屏幕 UV** 采 `_ReflectionMap` 的 ⇒ 反射与屏幕对不齐，
            //    实测三场反而变差（sororitas 1.058 → **1.084**、tauviorla 1.033 → 1.044、arena3 1.050 → 1.052）。
            //    代码留在 `CameraSpacePlane()` 里备查；**要用得先搞清原版那套是怎么保住对齐的**。
            _refl.projectionMatrix    = cam.projectionMatrix;
            _refl.nonJitteredProjectionMatrix = cam.nonJitteredProjectionMatrix;
            _refl.cullingMask         = cam.cullingMask;
            _refl.clearFlags          = cam.clearFlags;
            _refl.backgroundColor     = cam.backgroundColor;
            _refl.nearClipPlane       = cam.nearClipPlane;
            _refl.farClipPlane        = cam.farClipPlane;
            _refl.allowHDR            = cam.allowHDR;
            _refl.allowMSAA           = false;

            // 标准平面反射：镜像视图要**反剔除**，否则所有面都朝里
            var prev = GL.invertCulling;
            GL.invertCulling = true;
            _rendering = true;
            try
            {
                // 走 **SRP** 那条路（原版用的就是它：`__RenderReflection.c:186` 最后一句是
                // `UniversalRenderPipeline.RenderSingleCamera(ctx, reflectionCamera)`）。
                // ⚠️ **实测（2026-09-26）：换成它【没有】改变亮度比**（换之前/之后同为 117.52）。
                //    也就是说「原版这样渲」是**对齐做法**，不是任何缺口的根因 ——
                //    **别在别处把它写成「修好了偏亮」**（真正有效的那一条是那两级 Blit）。
                UniversalRenderPipeline.RenderSingleCamera(ctx, _refl);
            }
            finally { GL.invertCulling = prev; _rendering = false; }
        }

        /// <summary>场景加载后打一行汇总（与 `ArenaOriginalMaterial` 同一条「不许静默」的规矩）。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void HookSummary()
        {
            SceneManager.sceneLoaded += (sc, _) => Log(sc.name);
            Log("<启动场景>");
        }

        static void Log(string where)
        {
            var all = FindObjectsByType<ArenaMirror>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all.Length == 0) return;
            Debug.Log($"[Arena/Mirror] 平面反射（{where}）：场景里 {all.Length} 台 · 已渲 {_rendered} 帧 · "
                    + $"首个 textureScale={all[0].textureScale} normal={all[0].planeNormal[1]}");
        }
    }
}
