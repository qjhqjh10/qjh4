// ShakeAndSoundObject.cs — 原版 `ShakeAndSoundObject`（环境效果 prefab 上「进场震屏 + 定时播环境音」的组件）
//
// 判据（第一权威 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ShakeAndSoundObject.cs` 全文 41 行；`decomp_full` 无同名产物）：
//   `OnEnable()`：`foreach (var s in onEnableShakes) s.Play();`
//   `Update()`  ：`foreach (var s in sounds) s.Update(**Time.deltaTime**, base.transform);`
//     🔴 第二实参是 `Time.deltaTime`（不是累计时间）—— 原版如此。对 `PlaySoundOnTime.Update` 的判据
//        （`currentTime < 已播*间隔 + 起始`）来说等于每帧只给 ~1/60 s ⇒ **只有 `time ≈ 0` 的条目播一次**。
//        我们的表里这 17 条环境音**全部 `time = 0.0`** ⇒ 正好各播一次。**照抄，⛔ 别「修」成累加。**
//
// ── 数据（🆕 2026-10-27 接线）─────────────────────────────────────────────────────────
//   表 = `Resources/env_shake_sounds.json`（`_schema = env_shake_sounds/1`；**生成器** `工具/gen_env_shake_sounds.py`，
//   同内容还有 `数据/游戏数据/` 那一份给人看）。`entries[]` 是扁平视图（运行时用 `JsonUtility` 读 —— 它不支持字典）。
//   逐条带 `prefab` / `sounds[{time,sound,soundPid,is2d,repeat,loops,timeInterval}]` /
//   `onEnableShakes[{preset,delay,sustainTime,amplitude,frequency,direction,attackTime,decayTime,各 overwrite*}]`。
//   🔴 **按 prefab 名取自己那条**（`tableEntryName`，默认 = `gameObject.name`）；⛔ 别按 cue 名反推 prefab（至少 5 处对不上）。
//   🔴 **`is2d` 逐条抄**（环境这批里有 6 条是 3D）。
//   `sound` 以**名字**填；名字是 `PID:<n>` 时说明原版那一格是空引用或解不出（生成器的 `_stats` 里有计数）⇒ **出声、不播**。
//
// ── 震屏那一半（本波**只解析 + 出声**，⛔ 没接）──────────────────────────────────────
//   我们的震屏入口 = **`Core/CardFeel.ShakeCamera(Camera, Transform hudRoot, float worldAmp, float delay, …)`**
//   （`CardFeel.cs:1378`；`CardFeel` 归 `E2UDW`，⛔ 本波不碰、也不猜它后面那几个形参）。
//   ⇒ 本件把表里的参数（含各 `overwrite*` 开关）**解析进 `shakeParams` 并读得到**，然后**每个 prefab 出声一次**：
//     「参数齐了，但把参数喂给 `CardFeel.ShakeCamera` 这一步待接」。⛔ 不假装震了。
//   ⚠️ 原版的 `presetSO`（`CameraShakePreset` SO）我们**不需要** —— 表里那份内联参数就是它的覆盖结果。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `ShakeAndSoundObject`：环境 prefab 上「进场震屏 + 按 <see cref="PlaySoundOnTime"/> 播环境音」。
    /// <para>本件的**声音那一半已接线**（读 `Resources/env_shake_sounds.json`）；**震屏那一半只解析 + 出声**（见文件头）。</para></summary>
    [DisallowMultipleComponent]
    public class ShakeAndSoundObject : MonoBehaviour
    {
        /// <summary>运行时表名（`Resources.Load&lt;TextAsset&gt;`）。</summary>
        public const string ResourcePath = "env_shake_sounds";

        /// <summary>要查表的那一条（`null` = 用 `gameObject.name`）。</summary>
        public string tableEntryName;

        /// <summary>定时音效条目（**原版同名字段**；原版 private 序列化，我们代码里建 ⇒ public）。由表填。</summary>
        public PlaySoundOnTime[] sounds;

        /// <summary>进场震屏参数（由表解析；**尚未喂给震屏**，见文件头）。</summary>
        public List<ShakeParam> shakeParams = new List<ShakeParam>();

        /// <summary>保留原版的「震屏 preset 名」入口（表里还没解析出名字时为空）。</summary>
        public string[] onEnableShakeNames;

        // ---- 自检读口 ----
        public int BoundSounds { get { return sounds != null ? sounds.Length : 0; } }
        public int BoundShakes { get { return shakeParams != null ? shakeParams.Count : 0; } }
        public int ShakesUnwired { get; private set; }
        public int UpdateTicks { get; private set; }
        public int SoundUpdates { get; private set; }
        public int EnableCount { get; private set; }
        /// <summary>绑表结果：`>0` = 成功条数 · `0` = 表里没这个 prefab · `-1` = 资源载不到。</summary>
        public int LastBindCount { get; private set; }
        /// <summary>绑表失败/未绑时的原因（`null` = 一切正常）。</summary>
        public string LastBindError { get; private set; }
        /// <summary>自检：所有条目的 `time` 是否都 ≈ 0（环境这批应为 `true`，配合 `deltaTime` 怪癖各播一次）。</summary>
        public bool AllTimesZero
        {
            get
            {
                if (sounds == null) return false;
                for (int i = 0; i < sounds.Length; i++) if (sounds[i] != null && Mathf.Abs(sounds[i].time) > 1e-4f) return false;
                return true;
            }
        }
        /// <summary>自检：第 i 条的 `is2d`（越界 ⇒ `false`）。</summary>
        public bool Is2dAt(int i) { return sounds != null && i >= 0 && i < sounds.Length && sounds[i] != null && sounds[i].is2d; }

        /// <summary>一条震屏参数（表里内联的原版 `OverwriteCameraShakePreset` 覆盖值）。</summary>
        [Serializable]
        public class ShakeParam
        {
            public string preset;
            public long presetPid;
            public float delay, sustainTime, amplitude, frequency, attackTime, decayTime;
            public Vector3 direction;
            public int overwriteSustainTime, overwriteAmplitude, overwriteFrequency;
            public int overwriteDirection, overwriteAttackTime, overwriteDecayTime;
        }

        // ---- 表的 DTO（`JsonUtility` 只认数组/类，不认字典）----
        [Serializable] class SoundRow
        {
            public float time; public string sound; public long soundPid; public bool is2d;
            public bool repeat; public int loops; public float timeInterval;
        }
        [Serializable] class ShakeRow
        {
            public string preset; public long presetPid;
            public float delay, sustainTime, amplitude, frequency, attackTime, decayTime;
            public Vector3 direction;
            public int overwriteSustainTime, overwriteAmplitude, overwriteFrequency;
            public int overwriteDirection, overwriteAttackTime, overwriteDecayTime;
        }
        [Serializable] class Entry
        {
            public string prefab, cls, kind, componentFile;
            public SoundRow[] sounds;
            public ShakeRow[] onEnableShakes;
        }
        [Serializable] class Doc { public Entry[] entries; }

        static Doc _doc;
        static bool _triedLoad;
        static readonly HashSet<string> _warnedShake = new HashSet<string>();

        void Awake()
        {
            // 只有「还没被别的路径填过」才自动绑（幂等；手工赋过 sounds 的不覆盖）
            if (sounds == null || sounds.Length == 0) BindFromTable();
        }

        /// <summary>读 `Resources/<see cref="ResourcePath"/>` 并填自己那一条。返回成功条数（`-1` = 表载不到）。
        /// <para>⚠️ 找不到自己那一条 ⇒ **出声**（`LastBindError` 说明用的是哪个 key）。⛔ 不静默空转。</para></summary>
        public int BindFromTable()
        {
            string key = string.IsNullOrEmpty(tableEntryName) ? gameObject.name : tableEntryName;
            if (!_triedLoad)
            {
                _triedLoad = true;
                var ta = Resources.Load<TextAsset>(ResourcePath);
                if (ta != null)
                {
                    try { _doc = JsonUtility.FromJson<Doc>(ta.text); }
                    catch (Exception e) { Debug.LogError("[ShakeAndSound] `" + ResourcePath + ".json` 解析失败：" + e.Message); }
                }
            }
            if (_doc == null || _doc.entries == null)
            {
                LastBindCount = -1;
                LastBindError = "Resources/" + ResourcePath + ".json 载不到（跑 `python 工具/gen_env_shake_sounds.py --apply`）";
                Debug.LogWarning("[ShakeAndSound] " + LastBindError + " ⇒ 本件**不播环境音**（⛔ 不静默）");
                return -1;
            }
            Entry hit = null;
            for (int i = 0; i < _doc.entries.Length; i++)
            {
                var e = _doc.entries[i];
                if (e != null && e.prefab == key) { hit = e; break; }
            }
            if (hit == null)
            {
                LastBindCount = 0;
                LastBindError = "表里没有 prefab `" + key + "`";
                Debug.LogWarning("[ShakeAndSound] " + LastBindError + " ⇒ 本件**不播环境音**（⛔ 不静默；"
                               + "`tableEntryName` 可以显式指定 key）");
                return 0;
            }

            int n = hit.sounds != null ? hit.sounds.Length : 0;
            var arr = new PlaySoundOnTime[n];
            for (int i = 0; i < n; i++)
            {
                var s = hit.sounds[i];
                arr[i] = new PlaySoundOnTime
                {
                    time = s.time,
                    sound = s.sound,          // 逐条抄，⛔ 别按 prefab 名猜
                    is2d = s.is2d,            // 🔴 逐条抄（有 3D 的）
                    repeat = s.repeat,
                    loops = s.loops,
                    timeInterval = s.timeInterval,
                };
                if (!string.IsNullOrEmpty(s.sound) && s.sound.StartsWith("PID:", StringComparison.Ordinal))
                {
                    // 原版那一格是空 PPtr / 解不出 ⇒ 出声，且**这条不播**（⛔ 不拿别的 cue 顶上）
                    Debug.LogWarning("[ShakeAndSound] prefab `" + key + "` 的第 " + i + " 条 `sound` = `" + s.sound
                                   + "`（原版引用为空/未解）⇒ 这一条**不播**（不静默，⛔ 不用别的 cue 顶）");
                    arr[i] = null;
                }
            }
            sounds = arr;

            shakeParams.Clear();
            if (hit.onEnableShakes != null)
                for (int i = 0; i < hit.onEnableShakes.Length; i++)
                {
                    var k = hit.onEnableShakes[i];
                    shakeParams.Add(new ShakeParam
                    {
                        preset = k.preset, presetPid = k.presetPid,
                        delay = k.delay, sustainTime = k.sustainTime, amplitude = k.amplitude,
                        frequency = k.frequency, attackTime = k.attackTime, decayTime = k.decayTime,
                        direction = k.direction,
                        overwriteSustainTime = k.overwriteSustainTime, overwriteAmplitude = k.overwriteAmplitude,
                        overwriteFrequency = k.overwriteFrequency, overwriteDirection = k.overwriteDirection,
                        overwriteAttackTime = k.overwriteAttackTime, overwriteDecayTime = k.overwriteDecayTime,
                    });
                }

            LastBindCount = n;
            LastBindError = null;
            return n;
        }

        /// <summary>原版 `OnEnable`：逐个 `preset.Play()`。**本波只出声**（震屏入口 = `CardFeel.ShakeCamera`，不猜签名）。</summary>
        void OnEnable()
        {
            EnableCount++;
            if (shakeParams.Count > 0)
            {
                ShakesUnwired += shakeParams.Count;
                if (_warnedShake.Add(gameObject.name))
                    Debug.LogWarning("[ShakeAndSound] `" + gameObject.name + "` 的 " + shakeParams.Count
                                   + " 条震屏参数**已解析但没接**：我们的入口是 `Core/CardFeel.ShakeCamera(Camera, Transform, worldAmp, delay, …)`"
                                   + "（`CardFeel.cs:1378`，归 `E2UDW`）。⇒ 参数在 `shakeParams` 里读得到（含各 overwrite* 开关），"
                                   + "喂给它那一步待接线；⛔ 本件不假装震了。");
            }
            if (onEnableShakeNames == null) return;
            for (int i = 0; i < onEnableShakeNames.Length; i++)
            {
                string nm = onEnableShakeNames[i];
                if (string.IsNullOrEmpty(nm)) continue;
                ShakesUnwired++;
                if (_warnedShake.Add(nm))
                    Debug.LogWarning("[ShakeAndSound] 震屏 `" + nm + "` **没接**（同上：入口在 `CardFeel.ShakeCamera`）");
            }
        }

        /// <summary>原版 `Update`：`sounds[i].Update(Time.deltaTime, transform)`。
        /// 🔴 **第二实参照原版传 `Time.deltaTime`**（见文件头）—— ⛔ 别改成累加。</summary>
        void Update()
        {
            UpdateTicks++;
            if (sounds == null || sounds.Length == 0) return;
            float dt = Time.deltaTime;
            var tr = transform;
            for (int i = 0; i < sounds.Length; i++)
            {
                var s = sounds[i];
                if (s == null) continue;
                s.Update(dt, tr);
                SoundUpdates++;
            }
        }

        /// <summary>清计数（自检每段之前调）。</summary>
        public void ResetCountersForTest()
        {
            ShakesUnwired = 0; UpdateTicks = 0; SoundUpdates = 0; EnableCount = 0;
            LastBindCount = 0; LastBindError = null;
            _warnedShake.Clear();
        }

        /// <summary>自检用：编辑模式下 `AddComponent` **不触发** `OnEnable`（见 `CLAUDE.md` 那条），手动跑一次。</summary>
        public void EnableForTest() { OnEnable(); }
        /// <summary>自检用：同上，手动推一帧。</summary>
        public void UpdateForTest() { Update(); }
    }
}
