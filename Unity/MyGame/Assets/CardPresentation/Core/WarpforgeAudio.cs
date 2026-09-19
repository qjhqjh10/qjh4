// WarpforgeAudio.cs — 音频通道：三根音量滑块 → `AudioMixer`（原版 `SoundManager` 的对应物）
//
// 原版做法（`资料/普查产出_0918/第18行_UI三小条_规格.md` §②，逐条有反编译出处）：
//   · 写的是 **`AudioMixer.SetFloat("Volume" + MixerType 枚举名, dB)`** ——
//     既不是 `AudioListener.volume`，也不是直接改 `AudioSource.volume`
//   · `dB = (v == 0f) ? -80f : Mathf.Log10(v) * 20f`，`v` 是线性 0..1
//   · 四通道入参：`Music ← 音乐量 × 倍率` · `Voices ← 语音量` · `FX ← 音效量` · `Jingles ← 音效量`（**与 FX 同值**）
//   · **存档只存音乐**：`PlayerPrefs` 只有一个 key `"MusicVolume"`
//     （音效/语音**不落盘**，每次进来都是满音量 —— 这是原版行为，不是漏做）
//   · 默认值三者都是 **1.0（满音量）**
//
// mixer 本体：`Resources/Audio/Main Mixer`（自己建的，见 `Assets/CardPresentation/Editor/AudioSetup.cs`
// 与 `工具/setup_audio_mixer.py` 的三步说明）。组名 `Master/FX/Music/Voices/Jingles` 与原版一致。
using UnityEngine;
using UnityEngine.Audio;
using WarpforgeVFX;

namespace CardPresentation
{
    public static class WarpforgeAudio
    {
        /// <summary>`Resources.Load<AudioMixer>(...)` 的路径。</summary>
        public const string MixerResource = "Audio/Main Mixer";
        /// <summary>原版 `GameStaticData.MUSIC_PLAYER_PREFS_VOLUME` —— **唯一**落盘的那个。</summary>
        public const string MusicPrefKey = "MusicVolume";
        /// <summary>原版 `MusicManager.musicVolumeMultiplier` 在预制体里**查不到默认值** ⇒ 按 1.0 处理。</summary>
        public const float MusicVolumeMultiplier = 1f;

        /// <summary>线性 0..1。⚠️ **取值前先 `Ensure()`** —— 这三个是**自动属性**，`Ensure()` 之前它们是
        /// **0**（不是 1）。踩过：设置面板在 `Ensure()` 之前建，三根滑块全画在 0 上。</summary>
        public static float Music { get { Ensure(); return _music; } }
        public static float SoundFx { get { Ensure(); return _fx; } }
        public static float VoiceOver { get { Ensure(); return _vo; } }
        static float _music = 1f, _fx = 1f, _vo = 1f;

        static AudioMixer _mixer;
        static bool _tried;
        public static bool Ready { get { Ensure(); return _mixer != null; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void AutoInit() { Ensure(); }

        public static void Ensure()
        {
            if (_tried) return;
            _tried = true;

            // ⚠️ 这里**必须用后备字段**，不能走上面那三个属性 —— 它们会再调 `Ensure()`，无限递归。
            _music = _fx = _vo = 1f;
            // 只有音乐有存档（原版行为）
            if (PlayerPrefs.HasKey(MusicPrefKey)) _music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicPrefKey, 1f));

            _mixer = Resources.Load<AudioMixer>(MixerResource);
            if (_mixer == null)
            {
                // 不许静默失败：没有 mixer = 三根滑块**一点作用都没有**
                Debug.LogWarning($"[Audio] `Resources/{MixerResource}` 加载不到 —— 三根音量滑块不会起作用。" +
                                 "建法见 `Assets/CardPresentation/Editor/AudioSetup.cs` 文件头的三步。");
                return;
            }

            ApplyAll();
            RouteEffectSounds();
        }

        static void ApplyAll()
        {
            Write("VolumeMusic", _music * MusicVolumeMultiplier);
            Write("VolumeVoices", _vo);
            Write("VolumeFX", _fx);
            Write("VolumeJingles", _fx);       // 原版：Jingles 与 FX 同值
        }

        /// <summary>特效那条路（`WFSoundPlayer`）统一挂在 FX 组上。</summary>
        static void RouteEffectSounds()
        {
            var groups = _mixer.FindMatchingGroups("FX");
            if (groups != null && groups.Length > 0) WFSoundPlayer.FxGroup = groups[0];
            else Debug.LogWarning("[Audio] mixer 里找不到 `FX` 组 —— 特效音效不会受音效滑块影响");

            var vg = _mixer.FindMatchingGroups("Voices");
            if (vg != null && vg.Length > 0) VoicesGroup = vg[0];
            else Debug.LogWarning("[Audio] mixer 里找不到 `Voices` 组 —— 单位语音不会受语音滑块影响");
        }

        /// <summary>单位语音那条路（`UnitChatPanel` / `CardDisplayWindow`）挂的组。
        /// ⚠️ 那两个 `AudioSource` 是**在建面板时** `AddComponent` 出来的，所以它们读这个属性时
        /// 可能早于 `Ensure()` —— 属性会**先 `Ensure()` 再取**，顺序上不会漏。</summary>
        public static AudioMixerGroup VoicesGroup
        {
            get { Ensure(); return _voicesGroup; }
            private set { _voicesGroup = value; }
        }
        static AudioMixerGroup _voicesGroup;

        static void Write(string param, float v)
        {
            if (_mixer == null) return;
            _mixer.SetFloat(param, v <= 0f ? -80f : Mathf.Log10(v) * 20f);
        }

        // ---- 三根滑块的入口（值都是线性 0..1）----

        public static void SetMusic(float v)
        {
            Ensure();
            _music = Mathf.Clamp01(v);
            Write("VolumeMusic", _music * MusicVolumeMultiplier);
            PlayerPrefs.SetFloat(MusicPrefKey, _music);     // 原版只存这一个
            PlayerPrefs.Save();
        }

        public static void SetSoundFx(float v)
        {
            Ensure();
            _fx = Mathf.Clamp01(v);
            Write("VolumeFX", _fx);
            Write("VolumeJingles", _fx);                    // 原版：与 FX 同值，且**不落盘**
        }

        public static void SetVoiceOver(float v)
        {
            Ensure();
            _vo = Mathf.Clamp01(v);
            Write("VolumeVoices", _vo);                     // 原版：**不落盘**
        }

        /// <summary>四个暴露参数认不认得（自检用）。**名字对不上 `GetFloat` 会返回 false** ——
        /// 这是「滑块到底连没连到 mixer」唯一能在批处理里验到的硬证据。</summary>
        public static bool ParamsOk
        {
            get
            {
                Ensure();
                if (_mixer == null) return false;
                float v;
                return _mixer.GetFloat("VolumeFX", out v)
                    && _mixer.GetFloat("VolumeMusic", out v)
                    && _mixer.GetFloat("VolumeVoices", out v)
                    && _mixer.GetFloat("VolumeJingles", out v);
            }
        }

        /// <summary>自检用：把三档读回来（`ApplyAll` 之后再读，验「设进去的确实到了 mixer」）。</summary>
        public static string Dump()
        {
            Ensure();
            if (_mixer == null) return "mixer=null";
            float m, v, f, j;
            bool okM = _mixer.GetFloat("VolumeMusic", out m);
            bool okV = _mixer.GetFloat("VolumeVoices", out v);
            bool okF = _mixer.GetFloat("VolumeFX", out f);
            bool okJ = _mixer.GetFloat("VolumeJingles", out j);
            return $"Music={Music:F2}{(okM ? $"({m:F1}dB)" : "(**)")} · FX={SoundFx:F2}{(okF ? $"({f:F1}dB)" : "(**)")}"
                 + $" · Jingles{(okJ ? $"({j:F1}dB)" : "(**)")} · Voices={VoiceOver:F2}{(okV ? $"({v:F1}dB)" : "(**)")}";
        }
    }
}
