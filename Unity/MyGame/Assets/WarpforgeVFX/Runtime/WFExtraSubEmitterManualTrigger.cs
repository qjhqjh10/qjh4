// WFExtraSubEmitterManualTrigger.cs — 原版 `SubEmitterManualTrigger` 的等价实现（12 挂载 / 3 件 prefab）
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
    /// <summary>原版 `SubEmitterManualTrigger`（12 挂载 / 3 件 prefab）：
    /// 粒子在播时累计时间，到 `triggerOnSecond` **一次性**对 `subEmitterIndexes` 逐个 `TriggerSubEmitter`；
    /// 粒子停止时把计时与「已触发」复位（`SubEmitterManualTrigger.cs:17-50`）。
    /// ⚠️ `subEmitterIndexes` 原版是手挑的下标数组，我们取**全部子发射器**（`TriggerSubEmitter(i)` 逐 i）——
    /// 超集；与原版不同只在「原版只触发其中几个」。</summary>
    public class WFExtraSubEmitterManualTrigger : MonoBehaviour, IWFEffectExtraTick
    {
        public float triggerOnSecond = 2f;
        int[] _idx;
        float _t;
        bool _done;
        ParticleSystem _ps;

        public void Apply()
        {
            _ps = GetComponent<ParticleSystem>();
            if (_ps == null) _ps = GetComponentInChildren<ParticleSystem>(true);
            _t = 0f; _done = false;
            int n = _ps != null ? _ps.subEmitters.subEmittersCount : 0;
            _idx = new int[n];
            for (int i = 0; i < n; i++) _idx[i] = i;
        }

        public void ExtraTick(float dt)
        {
            if (_ps == null) return;
            if (_ps.isPlaying)
            {
                _t += dt;
                if (!_done && _t >= triggerOnSecond)
                {
                    for (int i = 0; i < _idx.Length; i++) _ps.TriggerSubEmitter(_idx[i]);
                    _done = true;
                }
            }
            if (_ps.isStopped) { _t = 0f; _done = false; }
        }
    }
}
