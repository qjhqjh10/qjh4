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
//      渲进 RT · `Shader.SetGlobalTexture("_ReflectionMap", rt)`。
//   ❌ **没做**（如实记着，别当已还原）：`blurIterations` 的模糊、`grainyReflections` 的颗粒、
//      以及原版的**斜切近平面**（不做的话地面以下的几何也会进反射）。
//      ⚠️ 这三样要不要补，先看逐场对比数字再定。
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
        public int blurIterations = 1;
        public bool grainy = true;

        static readonly int PropId = Shader.PropertyToID("_ReflectionMap");

        Camera _refl;
        RenderTexture _rt;
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
            RenderReflection(cam);
            BlurIfNeeded();
            Shader.SetGlobalTexture(PropId, _rt);
            _rendered++;
            if (_rendered <= 2)
                Debug.Log($"[Arena/Mirror] 渲了第 {_rendered} 帧：主相机={cam.name} · 镜像位={_refl.transform.position} · "
                        + $"RT={_rt.width}×{_rt.height} · 全局贴图 {PropId} 已设");
        }

        void EnsureTargets(Camera cam)
        {
            int w = Mathf.Max(16, Mathf.RoundToInt(cam.pixelWidth * Mathf.Clamp(textureScale, 0.05f, 1f)));
            int h = Mathf.Max(16, Mathf.RoundToInt(cam.pixelHeight * Mathf.Clamp(textureScale, 0.05f, 1f)));
            if (_rt == null || _rt.width != w || _rt.height != h)
            {
                if (_rt != null) { _rt.Release(); DestroyImmediate(_rt); }
                _rt = new RenderTexture(w, h, 16, RenderTextureFormat.DefaultHDR)
                { name = "ArenaReflection", useMipMap = false, autoGenerateMips = false };
                _rt.Create();
            }
            if (_refl == null)
            {
                var go = new GameObject("reflection camera (ours)");
                go.transform.SetParent(transform, false);
                _refl = go.AddComponent<Camera>();
                _refl.enabled = false;                                       // 只手动 Render，不进相机栈
                var rad = _refl.GetUniversalAdditionalCameraData();
                if (rad != null) { rad.renderPostProcessing = false; rad.renderShadows = true; }
            }
            _refl.targetTexture = _rt;
        }

        /// <summary>原版那档 `blurIterations`（档 2 = **1**）—— 双线性**降采样再升采样**当模糊，
        /// 不写 shader（`Graphics.Blit` 的 bilinear 就是一次 2×2 盒式模糊）。
        /// ⚠️ **还没做**的是 `grainyReflections` 的颗粒。</summary>
        void BlurIfNeeded()
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

        void RenderReflection(Camera cam)
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
            try { _refl.Render(); }
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
