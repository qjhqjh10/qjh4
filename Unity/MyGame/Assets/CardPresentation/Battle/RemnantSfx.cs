// RemnantSfx.cs — 残骸体那六条原版音效（三时机 × 两阵营）
//
// 判据（2026-09-29 从 `dump.cs` 的**字段名** + 反编译用法逐条核实，**不是按 cue 名字猜**）：
//   · `RemnantBody.toRemnantSound`（`+0x38`）    = **出现**（单位变残骸那一刻）
//   · `RemnantBody.deathSound`（`+0x50`）        = **被打掉**
//   · `RemnantAeldari.waystoneCollectSound`（`+0x70`） = **收集**（灵族）
//   · `RemnantNecrons.reanimateSound`（`+0x90`） = **收集**（死灵 —— 它叫「复活」）
//
//   | 阵营 | 出现 | 收集 | 被打掉 |
//   |---|---|---|---|
//   | 灵族 | `Aeldari To Waystone` | `Aeldari Waystone Collect` | `Aeldari Waystone Destruction` |
//   | 死灵 | `CardShatter` | `NecronsCardReanimate` | `RemnantsDestroyed` |
//
// 🔴 **六条 cue → 五条真 clip**（2026-09-29 从 `soundcollection_assets_all.bundle` 的 `clipList` 解开）：
//   灵族的「出现」与「收集」**共用** `Aeldari To Waystone Death`；
//   死灵的收集 cue `NecronsCardReanimate` 指向的 clip 叫 **`CardReanimate`**（**cue 名 ≠ clip 名**）。
//   ⚠️ 所以**别按 cue 名去 `Resources` 里找文件** —— 找不到。
//
// 音频来源：`工具/import_remnant_sfx.py`（不是 `import_original_sfx.py` —— 那个只覆盖 AnimFX 的
// `sounds[*].sound`；残骸这六条是预制体上的**序列化 AudioCue 字段**，不在那份清单里）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>残骸体的三条音效时机。</summary>
    public static class RemnantSfx
    {
        public enum Moment { ToRemnant = 0, Collect = 1, Death = 2 }

        /// <summary>`Resources.Load&lt;AudioClip>` 的目录（`Resources/Art/` 被 `.gitignore` 排除）。</summary>
        const string Root = "Art/audio/sfx/";

        /// <summary>这一档用哪个 clip。**不是 cue 名** —— 见文件头那条更正。</summary>
        public static string ClipOf(Moment m, bool aeldari)
        {
            switch (m)
            {
                case Moment.Collect: return aeldari ? "Aeldari To Waystone Death" : "CardReanimate";
                case Moment.Death:   return aeldari ? "Aeldari Waystone Destruction" : "RemnantsDestroyed";
                default:             return aeldari ? "Aeldari To Waystone Death" : "CardShatter";
            }
        }

        // ⚠️ 静态的 UnityEngine.Object **场景一重载就是野指针**（这工程在共享材质上踩过同一类坑）
        //    ⇒ 每次用之前判一次 null（被销毁的对象 `== null` 为真，会自动重建）。
        static AudioSource _src;

        /// <summary>自检用：一共播过几次。</summary>
        public static int Played { get; private set; }
        /// <summary>自检用：最后一次播的 clip 名（没播过 = null）。</summary>
        public static string LastClip { get; private set; }
        /// <summary>自检用：最后一次播的是哪一档。</summary>
        public static Moment LastMoment { get; private set; }

        /// <summary>把计数清零（自检每段之前调）。</summary>
        public static void ResetCounters() { Played = 0; LastClip = null; }

        // ==================================================================
        //  🆕 2026-09-29 **原版 `AudioCue` 的随机区间**（音高 / 音量 / 重播间隔）
        //
        //  之前这里只做了「放哪条 clip」那一半 —— 判据正本里一直挂着一条
        //  「`RemnantSfx` 只做了『放哪条 clip』，原版 `AudioCue` 的 pitch/volume 随机区间 +
        //   `timeToPlayAgain` 我们没做」（`资料/待办判据_战场与战斗视图.md` §8b）。这一节补上。
        //
        //  数据由 `工具/import_remnant_sfx.py` 从 `soundcollection_assets_all.bundle` 的
        //  `AudioCue` 字段里抽成 `Resources/Art/audio/sfx/remnant_cue_props.json`。
        //  ⚠️ **键是 cue（`Army/when`）不是 clip** —— 灵族的「出现」与「收集」共用同一个 clip，
        //     但原版是两条 cue（区间可以不一样）。
        //  ⚠️ 表取不到**不算错**（`Resources/Art/` 是 `.gitignore` 的）—— 那时按 1.0/1.0 播，
        //     并**只提示一次**（不是每条都刷屏）。
        // ==================================================================

        [System.Serializable]
        public class CueProps
        {
            public string key;
            public float minPitch = 1f, maxPitch = 1f;
            public float minVolume = 1f, maxVolume = 1f;
            public float timeToPlayAgain;
        }

        [System.Serializable]
        class CueTable { public CueProps[] cues; }

        static CueProps[] _props;
        static bool _propsTried;

        /// <summary>自检用：随机区间表读到了几张。</summary>
        public static int PropsLoaded { get { LoadProps(); return _props != null ? _props.Length : 0; } }

        static void LoadProps()
        {
            if (_propsTried) return;
            _propsTried = true;
            var ta = Resources.Load<TextAsset>(Root + "remnant_cue_props");
            if (ta == null)
            {
                Debug.Log($"[Remnant] 随机区间表不在（`{Root}remnant_cue_props.json`）—— "
                        + "音高/音量按 1.0 播。跑一次 `python 工具/import_remnant_sfx.py` 补上");
                return;
            }
            try { _props = JsonUtility.FromJson<CueTable>(ta.text)?.cues; }
            catch (System.Exception ex) { Debug.LogWarning("[Remnant] 随机区间表解不开：" + ex.Message); }
        }

        /// <summary>自检用：这一档用的随机区间（读不到 = null ⇒ 按 1.0 播）。</summary>
        public static CueProps PropsOf(Moment m, bool aeldari)
        {
            LoadProps();
            if (_props == null) return null;
            string key = (aeldari ? "Aeldari/" : "Necrons/") + MomentKey(m);
            foreach (var p in _props) if (p != null && p.key == key) return p;
            return null;
        }

        static string MomentKey(Moment m)
        {
            switch (m)
            {
                case Moment.Collect: return "collect";
                case Moment.Death:   return "death";
                default:             return "toRemnant";
            }
        }

        /// <summary>自检用：最后一次用的音高 / 音量（好断言「区间真的生效了」—— 截图听不出来）。</summary>
        public static float LastPitch { get; private set; }
        public static float LastVolume { get; private set; }

        /// <summary>各档上一次播的时刻（`Time.time`）——用来实现 `timeToPlayAgain`。</summary>
        static readonly System.Collections.Generic.Dictionary<string, float> _lastAt =
            new System.Collections.Generic.Dictionary<string, float>();
        /// <summary>自检用：因为 `timeToPlayAgain` 被挡掉了几次。</summary>
        public static int Throttled { get; private set; }
        /// <summary>自检用：把节流记录清掉（每段之前调）。</summary>
        public static void ResetThrottle() { _lastAt.Clear(); Throttled = 0; }


        static GameObject _host;

        /// <summary>
        /// 播一条。**取不到 clip 就出声警告、不静默**（红线）。
        /// <paramref name="at"/> = 世界坐标（原版是 `AudioCue.Play3D`）；传 `null` 则按 2D 播。
        /// </summary>
        public static bool Play(Moment m, bool aeldari, Vector3? at = null)
        {
            string name = ClipOf(m, aeldari);
            var clip = Resources.Load<AudioClip>(Root + name);
            if (clip == null)
            {
                Debug.LogWarning($"[Remnant] 音效 `{Root}{name}` 取不到 —— 跑一次 " +
                                 "`python 工具/import_remnant_sfx.py`（`Resources/Art/` 是 gitignore 的）");
                return false;
            }

            if (_src == null)
            {
                // 宿主挂在一个独立节点上（**不挂在 BattleDriver 上** —— 那个随场景销毁，
                // 而这条音效的寿命只到这一次播放结束）。
                // 🔴 **绝不要 `DontDestroyOnLoad`** —— 它在**编辑器 / 批处理里会直接抛**
                //    （`InvalidOperationException: … can only be used in play mode`，
                //     2026-09-29 被 `BattleScene` 自检当场抓到，整条自检因此中断）。
                //    宿主随场景销毁**无所谓**：`_src == null` 时这里会重建。
                // 🔴 **2026-10-11（A218）判「不改」**（这处**故意**保持**裸 `Transform`**，⛔ 别补 `RectTransform`）：
                //    判据 = 原版 remnant 那一族实读**全是裸 `Transform`**（`bundle_battleprefabs_vfxandmisc_assets_all`：
                //    `RemnantBody3D Aeldari` · `RemnantBody3D Necrons` · `RemnantLight` · `To remnant` ·
                //    `Remnant Aeldari Collect particles` …），而本节点是**纯音频宿主**（只挂 `AudioSource`，
                //    3D 定位 `spatialBlend = 1`、按世界坐标摆，见下面几行）—— **没有任何矩形语义**
                //    ⇒ 写 `sizeDelta` 只会造一个**没有判据的数**（铁律 3）。
                if (_host == null) _host = new GameObject("RemnantSfx");
                _src = _host.AddComponent<AudioSource>();
                _src.playOnAwake = false;
                _src.spatialBlend = at.HasValue ? 1f : 0f;     // 3D / 2D
                _src.rolloffMode = AudioRolloffMode.Linear;
                _src.minDistance = 1f;
                _src.maxDistance = 40f;
            }

            if (at.HasValue) { _src.spatialBlend = 1f; _src.transform.position = at.Value; }
            else             { _src.spatialBlend = 0f; }

            // 🆕 2026-09-29 原版 `AudioCue` 的三个随机量（判据见上面那一节）。
            // ⚠️ `timeToPlayAgain` 挡掉的那次**返回 false 但不是错**（原版就是这么节流的）——
            //    调用方**别把它当失败报警**；要看节流了几次有 `Throttled`。
            var cue = PropsOf(m, aeldari);
            string key = (aeldari ? "Aeldari/" : "Necrons/") + MomentKey(m);
            if (cue != null && cue.timeToPlayAgain > 0f)
            {
                float last;
                if (_lastAt.TryGetValue(key, out last) && Time.time - last < cue.timeToPlayAgain)
                {
                    Throttled++;
                    return false;
                }
            }
            _lastAt[key] = Time.time;

            // ⚠️ 用 `UnityEngine.Random`（**表现层**的随机，不进引擎状态）—— 引擎那边那条
            //    「只用 `System.Random(seed)`」管的是**对局可复现**，音高不参与。
            float pitch = cue != null ? Random.Range(cue.minPitch, cue.maxPitch) : 1f;
            float vol   = cue != null ? Random.Range(cue.minVolume, cue.maxVolume) : 1f;
            _src.pitch = pitch;

            _src.PlayOneShot(clip, WarpforgeAudio.SoundFx * vol);
            Played++;
            LastClip = clip.name;
            LastMoment = m;
            LastPitch = pitch;
            LastVolume = vol;
            return true;
        }
    }
}
