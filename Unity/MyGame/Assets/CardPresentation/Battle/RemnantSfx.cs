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

        /// <summary>`Resources.Load<AudioClip>` 的目录（`Resources/Art/` 被 `.gitignore` 排除）。</summary>
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

            _src.PlayOneShot(clip, WarpforgeAudio.SoundFx);
            Played++;
            LastClip = clip.name;
            LastMoment = m;
            return true;
        }
    }
}
