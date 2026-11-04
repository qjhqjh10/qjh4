// WFExtraSubEmitterManualTrigger.cs — 原版 `SubEmitterManualTrigger` 的等价实现（12 挂载 / 3 件 prefab）
//
// 🔴 **2026-10-26（第八十三会话 · 代理 X1）从 `WarpforgeVFX/Runtime/WFEffectExtras.cs` 拆出**：
//    Unity 只能为「类名 == 文件名」的类建出脚本资产；异名文件里的 MonoBehaviour 建不出资产
//    ⇒ 场景存盘会被烘成内联 `MonoScript`（`!u!115`）存根 ⇒ player 加载即崩（编辑器里一切正常）。
//    普查 = `工具/scene_class_filename_census.py`。
//
// ✅ 已复刻（2026-11-04 · B139-G · decomp_full/SubEmitterManualTrigger__Update.c:20-42）—— 条目 13-04 订正（铁律 5 保留更正痕迹）：
//    本类原来对**全部**子发射器逐下标 `TriggerSubEmitter(i)`。原版是**循环 `subEmitterIndexes` 数组、逐项触发**；
//    **数组为空 = 一次都不触发**。现读 12 个挂载里 **11 个数组为空**；唯一非空的是
//    `Sau_ParticleWhip/Beam#6/Necron Gauss ` 的 **`[1]`**，而它 `m_Enabled = 0`（组件禁用 ⇒ 实为 0 次）。
//    我们那版合计触发 **19 次**（`Sau_ParticleWhip` 那件 8 个子发射器全触发）。
//    ⇒ 现在只按数据里的下标数组触发（`WFEffectExtra.se`，由 `工具/gen_effect_extras.py` 烘）。
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>原版 `SubEmitterManualTrigger`（12 挂载 / 3 件 prefab）：
    /// 粒子在播时累计时间，到 `triggerOnSecond` **一次性**对 `subEmitterIndexes` 逐个 `TriggerSubEmitter`；
    /// 粒子停止时把计时与「已触发」复位（`SubEmitterManualTrigger.cs:17-50`）。</summary>
    public class WFExtraSubEmitterManualTrigger : MonoBehaviour, IWFEffectExtraTick
    {
        public float triggerOnSecond = 2f;
        int[] _idx;
        float _t;
        bool _done;
        int _fired;
        ParticleSystem _ps;

        /// <summary>累计 `TriggerSubEmitter` 的次数（断言读数；原版全库 = 0 —— 唯一非空那颗的组件 `m_Enabled = 0`）。</summary>
        public static int Triggered { get; private set; }
        /// <summary>**旧版「全下标」行为**被启用的次数（`indexes == null`，表里还没这个字段）—— 必须为 0。</summary>
        public static int LegacyAllIndexes { get; private set; }
        static bool _warnedLegacy;
        public static void ResetDiagnostics() { Triggered = 0; LegacyAllIndexes = 0; _warnedLegacy = false; }

        /// <param name="indexes">原版 `subEmitterIndexes`。
        /// <para>⚠️ `null` = **表里没有这个字段**（旧数据）⇒ 退回旧版「全下标」行为并**出声**（不静默）；
        /// **空数组** = 原版数组确实是空的 ⇒ **一次都不触发**（那是原版行为，⛔ 别当缺失）。</para></param>
        public void Apply(int[] indexes)
        {
            _ps = GetComponent<ParticleSystem>();
            if (_ps == null) _ps = GetComponentInChildren<ParticleSystem>(true);
            _t = 0f; _done = false; _fired = 0;

            if (indexes == null)
            {
                LegacyAllIndexes++;
                if (!_warnedLegacy)
                {
                    _warnedLegacy = true;
                    Debug.LogWarning("[WarpforgeVFX] `SubEmitterManualTrigger` 的 `subEmitterIndexes` **不在表里**" +
                                     "（`effect_extras.json` 的 `se` 字段）⇒ 退回旧版「触发全部子发射器」的**超集**行为。" +
                                     "重跑 `工具/gen_effect_extras.py` 即可消失（原版 12 个挂载里 11 个数组为空）。");
                }
                int n = _ps != null ? _ps.subEmitters.subEmittersCount : 0;
                _idx = new int[n];
                for (int i = 0; i < n; i++) _idx[i] = i;
            }
            else
            {
                _idx = indexes;                       // 原版数组（空 = 什么都不触发）
            }
        }

        public void ExtraTick(float dt)
        {
            if (_ps == null) return;
            if (_ps.isPlaying)
            {
                _t += dt;
                if (!_done && _t >= triggerOnSecond)
                {
                    for (int i = 0; i < _idx.Length; i++) { _ps.TriggerSubEmitter(_idx[i]); _fired++; Triggered++; }
                    _done = true;
                }
            }
            if (_ps.isStopped) { _t = 0f; _done = false; _fired = 0; }
        }

        /// <summary>本实例累计触发了几次（断言读数）。</summary>
        public int FiredCount { get { return _fired; } }
        /// <summary>本实例要按哪几个下标触发（数据原文；断言可逐值对）。</summary>
        public int[] Indexes { get { return _idx; } }
    }
}
