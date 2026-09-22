// ArenaEnvGlobal.cs —— 把**原版那条 `ApplyAmbientColor` 的全局量**带进场景。
//
// 🔴 **为什么需要它**（2026-09-22 晚查实）：
//   原版 `ScenarioEnvironmentConditionSO.ApplyAmbientColor` 干两件事：
//     ① `RenderSettings.ambientLight = ambientColor`
//     ② **`Shader.SetGlobalFloat(_AmbientColorBlend, ambientBlend)`**
//   而 **13 场的 `ambientBlend` 全是 0.0** ⇒ 原版那层 `tint` 是**恒等**：
//     `Everguild/UnlitAmbient` 的 `tint = lerp(1, sRGB(_ExtraAmbientColor) * unity_AmbientSky, _AmbientColorBlend)`。
//   ⚠️ **我们从来没设过这个全局量** ⇒ 运行时吃 `$Globals` 的默认值（**不是 0**）
//   ⇒ 真的乘上了一层 ≈0.78 的暗化。实测（定种子 A/B，`WF_NOAMB=1` 就等于把这一条还原成原版）：
//   arena1 背景右侧那块 **+106**、整图 **+8.75**。
//
// ⚠️ **`Shader.SetGlobalFloat` 是「进程级全局」，不随 `.unity` 存盘** ——
//   所以**不能只在建场时设**（建完就没了，预览/Play 都会退回默认值）。
//   正确做法 = 把值**存进场景**（这个组件），**运行时 `Awake` 与预览各调一次 `Apply()`**。
//   判据只此一处：`Apply()`；调用点是 `ArenaBuilder.RebuildOriginalMaterials`（预览侧）
//   与 `Awake`（运行时侧）。
using UnityEngine;

namespace WarpforgeVFX
{
    public class ArenaEnvGlobal : MonoBehaviour
    {
        /// <summary>原版 `ScenarioEnvironmentConditionSO.ambientBlend`（13 场实测全是 0）。</summary>
        public float ambientBlend;

        void Awake() { Apply(); }

        /// <summary>把原版那条链的全局量写下去。幂等。</summary>
        public void Apply()
        {
            Shader.SetGlobalFloat("_AmbientColorBlend", ambientBlend);
            // ⚠️ 只设这一个 —— `unity_AmbientSky`（= `RenderSettings.ambientLight`）由
            //    `BuildContent` 那段照 `defaultEnv.ambientColor` 设，别在这里重复一套判据。
        }
    }
}
