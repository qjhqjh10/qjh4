// WFSoundPlayer.cs — 特效音效的**播放器**（原版 `SoundManager` 在特效这条路用到的部分）
//
// 为什么单独一个类、而不是在 `WarpforgeEffectPlayer` 里 `PlayOneShot`：
//   · 原版 `sounds[i].is2d` 决定 **2D / 3D** —— 889 条是 `is2d=0`（**3D 定位音**），
//     48 条是 `is2d=1`（2D）。3D 那批要**在特效的世界坐标上**响，不能挂在 UI 上。
//   · 特效对象播完就销毁（`DestroyNow`），音还在响 → 必须要一个**活得比它久**的音源。
//   · 音量三滑块走 `AudioMixer` ⇒ 这里要能挂 `outputAudioMixerGroup`。
//
// ⚠️ **不许静默失败**：cue 解不出、clip 加载不到，一律 `Debug.LogWarning` + 计数（`WFSoundBank`）。
using UnityEngine;
using UnityEngine.Audio;

namespace WarpforgeVFX
{
    public static class WFSoundPlayer
    {
        /// <summary>关掉之后**只记账不发声**（批处理自检用 —— 不建对象、不出声，但 `Played` 照样涨，
        /// 所以「调度算对了没有」仍然验得到）。默认开。</summary>
        public static bool Enabled = true;

        /// <summary>FX 通道（音量三滑块的 `VolumeFX` 就在这个组上）。没有 mixer 时为 null。</summary>
        public static AudioMixerGroup FxGroup;

        /// <summary>累计请求播放次数（**不受 `Enabled` 影响**，自检读它）。</summary>
        public static int Played { get; private set; }

        const int PoolSize = 24;
        static AudioSource[] _pool;
        static GameObject _root;
        static int _next;

        /// <summary>固定的伪随机序列（挑 clip / 音高 / 音量）。用 `System.Random` 而不是
        /// `UnityEngine.Random` —— 本工程那条「对局可复现」的规矩对**表现层**一视同仁，
        /// 免得「同一段录屏重跑一遍音不一样」这种查不出来的差异。</summary>
        static readonly System.Random Rng = new System.Random(20260919);

        static AudioSource Rent()
        {
            if (_root == null)
            {
                _root = new GameObject("WFSoundPlayer");
                if (Application.isPlaying) Object.DontDestroyOnLoad(_root);
                _pool = new AudioSource[PoolSize];
            }
            for (int i = 0; i < PoolSize; i++)
            {
                int k = (_next + i) % PoolSize;
                if (_pool[k] == null)
                {
                    var src = _root.AddComponent<AudioSource>();
                    src.playOnAwake = false;
                    _pool[k] = src;
                }
                // 轮转：优先挑一个**没在响**的，全在响就按轮转盖掉最早那个
                if (!_pool[k].isPlaying || i == PoolSize - 1)
                {
                    _next = (k + 1) % PoolSize;
                    return _pool[k];
                }
            }
            return _pool[0];
        }

        /// <summary>按 cue 播一条（随机挑 clip、随机音高/音量，照原版的区间）。</summary>
        public static void Play(WFSoundCue cue, Vector3 worldPos, bool is2d)
        {
            Played++;
            if (!Enabled || cue == null) return;

            var clip = WFSoundBank.PickClip(cue, Rng);
            if (clip == null) return;                       // 已经警告过，这里不再刷屏

            float pitch = Mathf.Lerp(cue.minPitch, cue.maxPitch, (float)Rng.NextDouble());
            float vol = Mathf.Lerp(cue.minVolume, cue.maxVolume, (float)Rng.NextDouble());

            var src = Rent();
            src.transform.position = is2d ? Vector3.zero : worldPos;
            src.spatialBlend = is2d ? 0f : 1f;
            src.outputAudioMixerGroup = FxGroup;
            src.pitch = pitch <= 0f ? 1f : pitch;
            // ⚠️ `PlayOneShot` 而不是 `Play`：同一帧里同一个 cue 可能播两次（`loops` 多的条目），
            //    而且池子轮转会**把上一条掐掉**。音高/音量是**发起那一刻**取值的，各条互不影响。
            src.PlayOneShot(clip, vol);
        }

        /// <summary>自检用：把计数清零（池子留着）。</summary>
        public static void ResetCounter() { Played = 0; }
    }
}
