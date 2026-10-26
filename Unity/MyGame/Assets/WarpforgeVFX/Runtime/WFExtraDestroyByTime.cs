// WFExtraDestroyByTime.cs — 原版 `DestroyByTime` 的等价实现（9 挂载 / 7 件 prefab）
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
    /// <summary>原版 `DestroyByTime`（9 挂载 / 7 件 prefab）：`Start` 里 `Destroy(gameObject, lifeTime)`；
    /// **lifeTime == 0 时 warning + 退回 2 秒**（`DestroyByTime.cs:11-30`）。
    /// ⚠️ 原版是 `Start`（Awake 之后），我们在 `Apply()`（= 播放器 `Init`）里排 —— 同一拍，且批处理也走得到。</summary>
    public class WFExtraDestroyByTime : MonoBehaviour
    {
        public float lifeTime;
        public void Apply(WarpforgeEffectPlayer owner)
        {
            float t = lifeTime;
            if (t == 0f)
            {
                Debug.LogWarning("Animation lifeTime in DestroyByTime set to 0");
                t = 2f;                                  // 原版兜底（逐字）
            }
            // 归播放器管：它已经在管这个对象的一生（`_destroyed` 幂等）⇒ 用它的口，
            // 免得「Destroy 了但 Active 表还挂着」。
            if (owner != null) owner.DestroyAfter(t);
            else Destroy(gameObject, t);
        }
    }
}
