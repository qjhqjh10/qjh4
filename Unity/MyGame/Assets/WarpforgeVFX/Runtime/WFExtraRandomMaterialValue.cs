// WFExtraRandomMaterialValue.cs — 原版 `InitiMaterialWithRandomValue` 的等价实现（1 挂载）
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
    /// <summary>原版 `InitiMaterialWithRandomValue`（1 挂载）：`Start` 里
    /// `material.SetFloat("_Random", Random.Range(minValue, maxValue))`（`InitiMaterialWithRandomValue.cs:19-30`）。
    /// ⚠️ 用 `.material` 的语义（实例化一份）—— 我们的 binder 已经把材质做成**运行时副本**，
    /// 所以直接写 `sharedMaterial` 不会污染资产。</summary>
    public class WFExtraRandomMaterialValue : MonoBehaviour
    {
        public float minValue = -1f;
        public float maxValue = 1f;
        public void Apply()
        {
            var r = GetComponent<Renderer>();
            if (r == null) r = GetComponentInChildren<Renderer>(true);
            if (r == null) return;
            var m = r.sharedMaterial;
            if (m == null || !m.HasProperty("_Random"))
            {
                Debug.LogWarning("[WarpforgeVFX] `InitiMaterialWithRandomValue`：材质上没有 `_Random` 属性 ⇒ 这一句不生效。");
                return;
            }
            m.SetFloat("_Random", UnityEngine.Random.Range(minValue, maxValue));
        }
    }
}
