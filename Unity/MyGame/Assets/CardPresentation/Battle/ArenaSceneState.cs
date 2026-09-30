// ArenaSceneState.cs — **逐场「场景级」状态**（雾 / 环境光 / 天空盒 / 太阳 / 相机光学）
//
// 为什么要独立成一份（§27 架构 · 施工图 → `资料/§27架构_施工图.md`）：
//   这些量**不是 GameObject 的内容**、**没法放进 prefab** —— `RenderSettings` 是**场景级**的
//   （雾的 mode/线性范围、环境光、天空盒、`RenderSettings.sun`、halo/flare 强度）。
//   现在它们由 `ArenaBuilder` 在**建场时写进场景文件**；§27 要变成「1 份 Battle 场景 + 13 套数据、
//   运行时实例化」⇒ 这些量必须能在**运行时**按当前战场重新应用一遍（**换场要先还原上一场的值**）。
//
// 🔴 **这一段是从 `ArenaBuilder.ApplyLightAndAmbient` 原样搬过来的**（2026-09-30）：
//   `ArenaBuilder` 现在**调这里**（判据只留一处 —— 建场期与运行期走同一份代码）。
//   搬的时候**连注释一起搬**，因为那些注释就是判据出处（原版实读 / 实况 A/B）。
using UnityEngine;
using UnityEngine.Rendering;      // `AmbientMode` 在这里（`UnityEngine.Rendering`）

namespace CardPresentation
{
    /// <summary>逐场场景级状态。字段 = 建场时**写进场景文件**、运行时又必须能重放的那些。</summary>
    [System.Serializable]
    public class ArenaSceneState
    {
        public string arena;

        // ---- 光（内容侧那盏 `Directional Light` 由 prefab 带；这里只管**场景级**的关联）----
        public bool hasSun;                 // 原版 8 场有 `m_Sun`，5 场是空的（见 `SunIsNullFor`）

        // ---- 环境光 ----
        public int ambientMode;             // 0=Flat 1=Trilight 2=Skybox 3=Custom
        public Color ambientSky;            // `defaultEnv.ambientColor`（**运行时真值**，不是场景里的 m_AmbientSkyColor）
        public float ambientIntensity;
        public float ambientBlend;          // `_AmbientColorBlend`（13 场全是 0 ⇒ 原版那层 tint 是恒等）

        // ---- 雾（**形状**取自场景；开关与运行时值取自 `defaultEnv`）----
        public int fogMode;                 // 1=Linear 2=Exponential
        public Color fogShapeColor;         // 场景值
        public float fogShapeDensity;       // 场景值
        public float fogLinearStart, fogLinearEnd;   // 只有 Linear 用得上
        public Color fogRunColor;           // `defaultEnv.fogColor`（运行时值）
        public float fogRunDensity;         // `defaultEnv.fogDensity`

        // ---- 天空盒 / 耀斑 ----
        public Material skybox;             // 原版 13 场共用同一张 `Skybox Clouds Dusk`（prefab 里带引用）
        public float haloStrength, flareStrength;   // 原版 13 场都是 0（Unity 默认 0.5/1）

        // ---- 相机光学（`ConfigureBoardCamera` 用；战斗场景那台 `BoardCamera` 是场景自己的）----
        public float camSensorX, camSensorY, camFocalLength, camNear, camFar, camFov;

        /// <summary>原版 `RenderSettings.m_Sun` 为空的 5 场（判据 → `ArenaBuilder.ApplyLightAndAmbient`）。
        /// ⚠️ 这张名单是**原版实读**的结果，别按「有没有灯」推断。</summary>
        public static bool SunIsNullFor(string scene)
            => scene == "battlearena2" || scene == "battlearena3" || scene == "battlearenaaeldari"
            || scene == "battlearenablacklegion" || scene == "battlearenaleviathan";

        /// <summary>把这一套逐场值灌进 `RenderSettings` / `RenderSettings.sun` / 相机。
        ///
        /// `arenaRoot` = 当前那件战场实例的根（用来找里面那盏 `Directional Light` 给 `m_Sun`）；
        /// `cam` = 要套光学参数的那台相机（战斗场景是 `BoardCamera`；传 null 就只做场景级那几项）。
        /// **幂等**：同一套值重复 `Apply` 结果相同 —— 换场时**先 `Apply` 新的一套**即可（每项都是显式赋值，
        /// 没有「只设不改」的分支），不会残留上一场的值。</summary>
        public static void Apply(ArenaSceneState s, GameObject arenaRoot, Camera cam = null)
        {
            if (s == null) { Debug.LogWarning("[ArenaState] 传了 null（战场数据没取到）⇒ 场景级状态没应用"); return; }

            // ---------------- 环境光 ----------------
            var mode = (AmbientMode)s.ambientMode;
            RenderSettings.ambientMode = mode;
            if (mode != AmbientMode.Flat)
                Debug.LogWarning($"[ArenaState] 🔴 ambientMode = {mode}（不是 Flat）—— 只对 Flat 做过实况核对，"
                               + "这种模式下 `ambientLight` 未必是生效的那个量，**别当已定案**");
            RenderSettings.ambientLight     = new Color(s.ambientSky.r, s.ambientSky.g, s.ambientSky.b, s.ambientSky.a);
            RenderSettings.ambientIntensity = s.ambientIntensity > 0f ? s.ambientIntensity : 0.41f;
            // `_AmbientColorBlend`：原版 `ApplyAmbientColor` 显式灌；13 场全是 0 ⇒ 那层 tint 是恒等。
            //   ⚠️ 不设的话吃 `$Globals` 默认值（不是 0）⇒ `Everguild/UnlitAmbient` 的
            //   `tint = lerp(1, …)` 会真的乘上一层暗化（实测 arena1 整图 +8.75）。
            Shader.SetGlobalFloat("_AmbientColorBlend", s.ambientBlend);

            // ---------------- 雾（**形状无条件先写**，再按原版判据切开关）----------------
            RenderSettings.fogMode          = (FogMode)s.fogMode;
            RenderSettings.fogColor         = s.fogShapeColor;
            RenderSettings.fogDensity       = s.fogShapeDensity;
            // ⚠️ **API 名与 YAML 名不一样**：YAML 是 `m_LinearFogStart/End`，C# 是 `fogStartDistance/fogEndDistance`。
            RenderSettings.fogStartDistance = s.fogLinearStart;
            RenderSettings.fogEndDistance   = s.fogLinearEnd;
            // 原版 `ApplyFog` 的判据 = `0 < fogDensity` 才开；13 场默认环境 fogDensity 全是 0
            RenderSettings.fog = s.fogRunDensity > 0f;
            if (RenderSettings.fog)
            {
                RenderSettings.fogColor   = s.fogRunColor;
                RenderSettings.fogDensity = s.fogRunDensity;
                // ⚠️ **不在这里写 `FogMode.Exponential`** —— 形状上面已照原版写过
                //    （写死那版对 aeldari / blacklegion 是错的：它们 Linear + 65.3/181.5、51.6/132.3）。
            }

            // ---------------- 天空盒 / 太阳 / 耀斑 ----------------
            RenderSettings.skybox = s.skybox;
            RenderSettings.haloStrength  = s.haloStrength;
            RenderSettings.flareStrength = s.flareStrength;
            // ⚠️ `RenderSettings.sun` 收的是 `Light` 组件、**不是 GameObject**（编译期就报错，别写错）
            Light sun = null;
            if (s.hasSun && arenaRoot != null)
            {
                sun = arenaRoot.GetComponentInChildren<Light>(true);
                if (sun == null)
                    Debug.LogWarning($"[ArenaState] `{s.arena}`：原版这场的 `m_Sun` 有值，"
                                   + "但实例里找不到 `Light` ⇒ `RenderSettings.sun` 留空（出声）");
            }
            RenderSettings.sun = sun;

            // ---------------- 相机光学 ----------------
            if (cam != null)
            {
                cam.orthographic        = false;
                cam.usePhysicalProperties = true;
                cam.focalLength         = s.camFocalLength > 0f ? s.camFocalLength : 28f;
                cam.sensorSize          = new Vector2(s.camSensorX > 0f ? s.camSensorX : 41.5f,
                                                      s.camSensorY > 0f ? s.camSensorY : 24f);
                cam.gateFit             = Camera.GateFitMode.Horizontal;
                cam.fieldOfView         = s.camFov > 0f ? s.camFov : 46.397182f;
                cam.nearClipPlane       = s.camNear > 0f ? s.camNear : 0.3f;
                cam.farClipPlane        = s.camFar  > 0f ? s.camFar  : 300f;
            }
        }
    }
}
