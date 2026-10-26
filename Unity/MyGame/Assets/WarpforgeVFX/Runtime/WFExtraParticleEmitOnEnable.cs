// WFExtraParticleEmitOnEnable.cs — 原版 `ParticleEmitOnEnable` 的等价实现（2 挂载 / 2 件 prefab）
//
// 🔴 **2026-10-26（第八十三会话 · 代理 X1）从 `WarpforgeVFX/Runtime/WFEffectExtras.cs` 拆出**：
//    Unity 只能为「类名 == 文件名」的类建出脚本资产；异名文件里的 MonoBehaviour 建不出资产
//    ⇒ 场景存盘会被烘成内联 `MonoScript`（`!u!115`）存根 ⇒ player 加载即崩（编辑器里一切正常）。
//    普查 = `工具/scene_class_filename_census.py`。
//
// ⚠️ 本块**逐字搬**（只把缩进重新对齐到 namespace 里）。表 `effect_extras.json` 的读取与
//    `AttachAll` 装配仍在 `WFEffectExtras.cs`；`IWFEffectExtraTick` / `WFEffectExtra` /
//    `WFSoundSlot` 也在那里（同一 `WarpforgeVFX` 命名空间）。
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>原版 `ParticleEmitOnEnable`（2 挂载 / 2 件 prefab）：`OnEnable` 里
    /// `if (!ps.isPlaying) ps.Play()`（`ParticleEmitOnEnable.cs:7-13`）。</summary>
    public class WFExtraParticleEmitOnEnable : MonoBehaviour
    {
        public void Apply()
        {
            var ps = GetComponent<ParticleSystem>();
            if (ps == null) return;
            if (!ps.isPlaying) ps.Play();
        }
    }
}
