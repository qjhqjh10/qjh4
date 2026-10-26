// WFExtraShaderPropertyFloat.cs — 原版 `ShaderProperySetter` 的等价实现（2 挂载 / 2 件 prefab）
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
    /// <summary>原版 `ShaderProperySetter`（2 挂载 / 2 件 prefab）：`applyOnStart` 时把
    /// `floatsProperties[]` 里的 `propertyName → floatValue` 逐条 `material.SetFloat`
    /// （`ShaderProperySetter.cs` 的 `Start` / `ShaderPropertyDefinitionFloat.ApplyParameter`）。
    /// ⚠️ 原版还支持 `applyOnUpdate`；出货数据里我们这 2 处只有 `applyOnStart`（普查只取了这两个 flag）——
    /// 真出现 `applyOnUpdate` 会在 `Apply` 里出声。</summary>
    public class WFExtraShaderPropertyFloat : MonoBehaviour
    {
        public string propertyName;
        public float value;
        public bool applyOnUpdate;

        public void Apply()
        {
            if (applyOnUpdate)
                Debug.LogWarning("[WarpforgeVFX] `ShaderProperySetter.applyOnUpdate=1` 这一档**未实现**"
                               + "（本工程只见到 `applyOnStart`）⇒ 只会写一次。");
            if (string.IsNullOrEmpty(propertyName)) return;
            var r = GetComponent<Renderer>();
            if (r == null) r = GetComponentInChildren<Renderer>(true);
            if (r == null || r.sharedMaterial == null) return;
            if (!r.sharedMaterial.HasProperty(propertyName))
            {
                Debug.LogWarning($"[WarpforgeVFX] `ShaderProperySetter`：材质 `{r.sharedMaterial.name}` 上没有属性 "
                               + $"`{propertyName}` ⇒ 这一句不生效。");
                return;
            }
            r.sharedMaterial.SetFloat(propertyName, value);
        }
    }
}
