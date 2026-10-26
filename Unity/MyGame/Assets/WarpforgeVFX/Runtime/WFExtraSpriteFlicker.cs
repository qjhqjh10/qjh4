// WFExtraSpriteFlicker.cs — 原版 `SpriteFlickerEffect` 的等价实现（2 挂载 / 1 件 prefab）
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
    /// <summary>原版 `SpriteFlickerEffect`（2 挂载 / 1 件 prefab）：与 `MaterialFlickerEffect` **同一套四正弦算式**，
    /// 只是写 `SpriteRenderer.color` 的 α（不是材质）。判据 → `SpriteFlickerEffect.cs:60-100` 的 `Update`
    /// （算式与 `MaterialFlickerEffect__Update.c` 逐项相同，`WFMaterialFlicker` 文件头有机器码级推导）。
    /// ⚠️ 出货 2 处实例都 `playOnAwake=1`（普查只取了 5 个字段）⇒ 这里按「播放即 `fade=1`」落地；
    ///    若将来出现 `playOnAwake=0` 的，会从 1 起而不是 0（已在 `Apply` 里注释）。</summary>
    public class WFExtraSpriteFlicker : MonoBehaviour, IWFEffectExtraTick
    {
        public float amplitude = 1f, frequency = 1f, fadeInTime = 1f, fadeOutTime = 1f;
        public bool randomStart;

        SpriteRenderer _sr;
        Color _base;
        float _origAlpha = 1f, _fade = 1f, _t, _desync;

        /// <summary>最近一次算出来的 alpha（自检读它）。</summary>
        public float CurrentAlpha { get; private set; }
        /// <summary>材质原本的 alpha（`alpha = (noise+1) * 原α * fade系数` 里那个「原α」）。</summary>
        public float OrigAlpha { get { return _origAlpha; } }

        public void Apply()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = GetComponentInChildren<SpriteRenderer>(true);
            if (_sr == null) return;
            _base = _sr.color;
            _origAlpha = _base.a;
            _fade = 1f;                       // 原版 `Awake`：`playOnAwake ? 1 : 0`（见类注释的限定）
            _t = 0f;
            if (randomStart) _desync = UnityEngine.Random.Range(0f, 100f);
        }

        public void ExtraTick(float dt)
        {
            if (_sr == null) return;
            _t += dt;
            float th = (_t + _desync) * frequency;
            float x = th + 0.25f * Mathf.Sin(0.35f * th);
            float noise = amplitude * (0.35f * Mathf.Sin(1.67f * x + 0.15f)
                                     + 0.59f * Mathf.Sin(x + 0.52f)
                                     + 0.28f * Mathf.Sin(3.24f * x - 0.11f)
                                     + 0.17f * Mathf.Sin(7.02f * x - 0.42f));
            float a = (noise + 1f) * _origAlpha * (_fade - 0.25f) / 0.75f;
            CurrentAlpha = a;
            var c = _base;
            c.a = a;
            _sr.color = c;
        }
    }
}
