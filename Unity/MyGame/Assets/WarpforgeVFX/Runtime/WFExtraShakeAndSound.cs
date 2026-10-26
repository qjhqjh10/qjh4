// WFExtraShakeAndSound.cs — 原版 `ShakeAndSoundObject` 的等价实现（35 挂载 / 35 件 prefab）
//
// 🔴 **2026-10-26（第八十三会话 · 代理 X1）从 `WarpforgeVFX/Runtime/WFEffectExtras.cs` 拆出**：
//    Unity 只能为「类名 == 文件名」的类建出脚本资产；异名文件里的 MonoBehaviour 建不出资产
//    ⇒ 场景存盘会被烘成内联 `MonoScript`（`!u!115`）存根 ⇒ player 加载即崩（编辑器里一切正常）。
//    普查 = `工具/scene_class_filename_census.py`。
//
// ⚠️ 本块**逐字搬**（只把缩进重新对齐到 namespace 里）。表 `effect_extras.json` 的读取与
//    `AttachAll` 装配仍在 `WFEffectExtras.cs`；`IWFEffectExtraTick` / `WFEffectExtra` /
//    `WFSoundSlot` 也在那里（同一 `WarpforgeVFX` 命名空间）。
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>原版 `ShakeAndSoundObject`（35 挂载 / **35 件 prefab**，全部落在**环境卡**那批）：
    /// · `OnEnable` → 逐个 `onEnableShakes[i].Play()`（= `OverwriteCameraShakePreset` → `CameraShakerManager.DoShake`）
    /// · `Update` → 逐条 `sounds[i].Update(Time.deltaTime, transform)`（= `PlaySoundOnTime`：第 `time` 秒起、
    ///   每 `interval` 秒、共 `n = repeat ? loops : 1` 次）
    /// 判据 → `ShakeAndSoundObject.cs:11-40`。
    /// ⚠️ **屏震**走 `WFModuleScreenShake.PlayPresetByName`（我们唯一的屏震收口，下游 = `CardFeel.ShakeCamera`）；
    ///    **声音**走 `WFSoundBank` + `WFSoundPlayer`（与 `AnimFXController.sounds` 同一套 cue 表）。
    ///    ⚠️ 原版那 7 件带屏震的 prefab 里 `presetSO` 只有两张资产：`Shake Earthquake` / `Shake Hit Medium`。</summary>
    public class WFExtraShakeAndSound : MonoBehaviour, IWFEffectExtraTick
    {
        public string[] presets;
        public WFSoundSlot[] sounds;

        float _t;
        int[] _played;
        static readonly HashSet<string> _badCues = new HashSet<string>();

        public void Apply()
        {
            _t = 0f;                                   // 原版 `OnEnable` 起算
            if (presets != null)
                for (int i = 0; i < presets.Length; i++)
                    WFModuleScreenShake.PlayPresetByName(presets[i], "ShakeAndSoundObject");
            _played = new int[sounds != null ? sounds.Length : 0];
        }

        public void ExtraTick(float dt)
        {
            if (sounds == null || sounds.Length == 0) return;
            _t += dt;
            for (int i = 0; i < sounds.Length; i++)
            {
                var s = sounds[i];
                if (s == null || s.total <= 0 || string.IsNullOrEmpty(s.cue)) continue;
                // `while` 而不是 `if` —— 一帧跨过好几个区间时不丢拍（与 `WarpforgeEffectPlayer.TickSounds` 同口径）
                while (_played[i] < s.total && _t >= s.time + _played[i] * s.interval)
                {
                    _played[i]++;
                    WFSoundCue cue;
                    if (!WFSoundBank.TryGetCue(s.cue, out cue))
                    {
                        if (_badCues.Add(s.cue))
                            Debug.LogWarning($"[WarpforgeVFX] `ShakeAndSoundObject` 的音效 cue `{s.cue}` 在音效表里没有"
                                           + " —— 这一条不会响。跑 `工具/import_original_sfx.py` 重生成表。");
                        continue;
                    }
                    WFSoundPlayer.Play(cue, transform.position, s.is2d != 0);
                }
            }
        }
    }
}
