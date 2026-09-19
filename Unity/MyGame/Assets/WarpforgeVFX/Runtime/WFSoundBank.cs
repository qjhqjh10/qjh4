// WFSoundBank.cs — 原版**音效 cue 表**（AnimFX `sounds` / `exitSounds` 指向的那一层）
//
// 为什么要有它：`sounds[i].sound` 指向的**不是 AudioClip**，是一个**随机化包装**
// （原版 `PlaySoundOnTime` 拿到的 `SoundCollection`）。同一个 cue 里有 1~3 条 clip，
// 播的时候随机挑一条，并且音高/音量各在自己那个区间里随机 —— 直接拿 clip 播会**每一下都一样**。
//
// 数据（`Resources/animfx_sounds.json`，由 `工具/import_original_sfx.py` 生成）：
//   cue 名 → { clips[], minPitch/maxPitch, minVolume/maxVolume, timeToPlayAgain }
//   ⚠️ **要读原始 bundle 才拿得到**：cue 里的 `clipList` 存的是 **PathID**，
//      解包出来的 `assets_full/...` 目录里没有 PathID↔文件名 的映射（实测过）。
//   ⚠️ clip 名有带**尾随空格**的（`Hero of the Empire `），导入时剥掉了，表里存的是剥过的。
//
// 音频本体在 `Resources/Art/audio/sfx/<clip 名>`（`Resources/Art/` 不进仓库，和美术/语音同规矩）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>一个随机化 cue（对应原版一个 `SoundCollection` 资产）。</summary>
    [Serializable]
    public class WFSoundCue
    {
        public string name = "";
        public string[] clips = new string[0];
        public float minPitch = 1f, maxPitch = 1f;
        public float minVolume = 1f, maxVolume = 1f;
        public float timeToPlayAgain = 0f;
    }

    [Serializable]
    public class WFSoundTable
    {
        public int version = 1;
        public string note = "";
        public WFSoundCue[] cues = new WFSoundCue[0];
    }

    /// <summary>cue 名 → cue 数据 → AudioClip。**带负缓存**（解不出的只报一次，不每帧刷屏）。</summary>
    public static class WFSoundBank
    {
        /// <summary>表在 `Resources/` 下的路径（与 `voice_lines` 同一处）。</summary>
        public const string TablePath = "animfx_sounds";
        /// <summary>clip 在 `Resources/` 下的前缀。</summary>
        public const string ClipRoot = "Art/audio/sfx/";

        static Dictionary<string, WFSoundCue> _byName;
        static readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        static readonly HashSet<string> _missingClip = new HashSet<string>();

        /// <summary>表里没有、或者 clip 一条都加载不出来的 cue 数（**累积**，自检读它）。</summary>
        public static int BadCues { get; private set; }
        /// <summary>表里查不到的 clip 名（去重后的个数）。</summary>
        public static int MissingClips { get { return _missingClip.Count; } }
        public static int CueCount { get { Ensure(); return _byName.Count; } }
        public static bool Loaded { get { Ensure(); return _byName.Count > 0; } }

        /// <summary>测试用：把缓存清掉（表文件改了之后也要靠它）。</summary>
        public static void Reset()
        {
            _byName = null;
            _clips.Clear();
            _missingClip.Clear();
            BadCues = 0;
        }

        static void Ensure()
        {
            if (_byName != null) return;
            _byName = new Dictionary<string, WFSoundCue>();

            var ta = Resources.Load<TextAsset>(TablePath);
            if (ta == null)
            {
                // 不许静默：表没了就等于**所有特效音效都不响**，而那看起来像「原版就没声音」。
                Debug.LogWarning($"[WarpforgeVFX] 音效表 `Resources/{TablePath}.json` 不在 —— " +
                                 "特效的所有 `sounds`/`exitSounds` 都不会播。" +
                                 "跑 `工具/import_original_sfx.py` 生成它。");
                return;
            }
            WFSoundTable tbl;
            try { tbl = JsonUtility.FromJson<WFSoundTable>(ta.text); }
            catch (Exception e)
            {
                Debug.LogWarning($"[WarpforgeVFX] 音效表解析不了：{e.Message}");
                return;
            }
            if (tbl == null || tbl.cues == null) return;
            foreach (var c in tbl.cues)
                if (c != null && !string.IsNullOrEmpty(c.name)) _byName[c.name] = c;
        }

        public static bool HasCue(string name)
        {
            Ensure();
            return !string.IsNullOrEmpty(name) && _byName.ContainsKey(name);
        }

        public static bool TryGetCue(string name, out WFSoundCue cue)
        {
            Ensure();
            cue = null;
            if (string.IsNullOrEmpty(name)) return false;
            if (_byName.TryGetValue(name, out cue)) return true;
            BadCues++;
            return false;
        }

        /// <summary>clip 名 → AudioClip（带负缓存）。加载不出来返回 null **并记一笔**。</summary>
        public static AudioClip Clip(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            AudioClip c;
            if (_clips.TryGetValue(name, out c)) return c;
            if (_missingClip.Contains(name)) return null;

            c = Resources.Load<AudioClip>(ClipRoot + name);
            if (c == null)
            {
                _missingClip.Add(name);
                Debug.LogWarning($"[WarpforgeVFX] 音效 clip 加载不到：`{ClipRoot}{name}`" +
                                 "（跑 `工具/import_original_sfx.py` 补）");
                return null;
            }
            _clips[name] = c;
            return c;
        }

        /// <summary>从 cue 里随机挑一条真 clip（挑不出来返回 null）。</summary>
        public static AudioClip PickClip(WFSoundCue cue, System.Random rng)
        {
            if (cue == null || cue.clips == null || cue.clips.Length == 0) return null;
            int start = rng != null ? rng.Next(cue.clips.Length) : 0;
            for (int i = 0; i < cue.clips.Length; i++)
            {
                var c = Clip(cue.clips[(start + i) % cue.clips.Length]);
                if (c != null) return c;               // 坏的那条跳过，别整条 cue 哑掉
            }
            return null;
        }
    }
}
