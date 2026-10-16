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
                // 🔴 **2026-10-11（`A1534`）**：`hideFlags = DontSave` —— **这一句是给【批处理自检】兜底的**。
                //   自检宿主走 `-executeMethod`（**编辑模式**）⇒ 这里 `new GameObject` 建出来的东西
                //   **落在当时打开的那个场景里**，收尾 `SaveScene()` 会把它**连同音源池一起烘进 `.unity`**。
                //   实测（`DeckEditor.unity`）：跑一趟 `DeckScene.Run` ⇒ 场景里多一个 `WFSoundPlayer` 节点，
                //   它名下 `AudioSource` 的**颗数 = 那一趟真租过几颗**（HEAD 5 颗 → 某趟 6 颗；`PoolSize = 24` 封顶）。
                //   批处理下没有帧循环 ⇒ `isPlaying` 永不转 false ⇒ 每多播一条就多租一颗 ⇒
                //   **场景文件的内容成了「那趟跑过什么」的函数**（跨趟不可复现）。
                //   ⇒ `DontSave` =「我是运行时对象、**任何场景 / 构建都别持久化我**」（也含 `DontSaveInEditor`）。
                //   ⚠️ 另一条路是自检侧开 `Enabled = false`（那个开关本注释就写着「批处理自检用」）——
                //     **本仓 8 个自检宿主一个都没开**（只有老的 `WarpforgeArena1/Editor/AnimFXCheck.cs:342` 开过）。
                //     那条路是「别做这件事」，这一句是「做了也别留痕」；**根因是后者**，故修在这里。
                _root.hideFlags = HideFlags.DontSave;
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

        /// <summary>直接播一条 clip（**不走 cue 表**）。给那些**不在 AnimFX 表里**的原版音效用
        /// —— 目前只有加时的 `OvertimeStart`：原版是 `OvertimeUi.enteringOvertimeSound` 那个 AudioCue，
        /// 参数照抄资产（`bundle_soundcollection_assets_all/MonoBehaviour/OvertimeStart.json`：
        /// `minPitch = maxPitch = 1.0` · `minVolume = maxVolume = 1.0` · 单个 clip），所以这里**不加随机**。</summary>
        public static void Play(AudioClip clip, bool is2d, float volume = 1f, float pitch = 1f)
        {
            Played++;
            if (!Enabled || clip == null) return;

            var src = Rent();
            src.transform.position = Vector3.zero;
            src.spatialBlend = is2d ? 0f : 1f;
            src.outputAudioMixerGroup = FxGroup;
            src.pitch = pitch <= 0f ? 1f : pitch;
            src.PlayOneShot(clip, volume);
        }

        /// <summary>按 cue 播一条（随机挑 clip、随机音高/音量，照原版的区间）。
        /// **返回真的播出去的那条 clip**（没播 = null）—— 调用方要记「刚才响的是哪条」时**必须用这个返回值**：
        /// 再自己 `PickClip(cue, null)` 一次**会拿到另一条**（那个重载 `rng == null` 恒取第一条）
        /// ⇒ 记下来的名字当场就是假的（`T06` 2026-10-16 实测过，见 `BattleSfx.PlayCore`）。</summary>
        public static AudioClip Play(WFSoundCue cue, Vector3 worldPos, bool is2d)
        {
            Played++;
            if (!Enabled || cue == null) return null;

            var clip = WFSoundBank.PickClip(cue, Rng);
            if (clip == null) return null;                  // 已经警告过，这里不再刷屏

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
            return clip;
        }

        /// <summary>自检用：把计数清零（池子留着）。</summary>
        public static void ResetCounter() { Played = 0; }
    }
}
