// ArenaPrefabData.cs — 挂在**战场 prefab 根**上的一小块「逐场数据」（§27 架构）
//
// 为什么要它：`RenderSettings`（雾 / 环境光 / 天空盒 / `m_Sun` / halo·flare）与相机光学
// **是场景级的、没法放进 prefab** ⇒ 建 prefab 时把这一套值**序列化在 prefab 里**带过去，
// 运行时实例化之后由 `ArenaRuntimeLoader` 应用（判据全在 `ArenaSceneState.Apply`）。
using UnityEngine;

namespace CardPresentation
{
    public class ArenaPrefabData : MonoBehaviour
    {
        /// <summary>逐场场景级状态（由 `ArenaBuilder.FromManifest` 在建 prefab 时填）。</summary>
        public ArenaSceneState state;

        /// <summary>场名（`battlearena3` 这种键）。取不到时返回空串（**调用方要出声**）。</summary>
        public string ArenaKey { get { return state != null ? state.arena : ""; } }
    }
}
