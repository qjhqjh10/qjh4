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
        /// <summary>重播冷却窗口（秒）。判据 = 原版 `AudioCue__ctor.c` 逐字节实读：
        /// `*(float *)(this + 0x30) = 0x3dcccccd` = **0.1**（同一支里 `+0x20/+0x24/+0x28/+0x2c` 四个 pitch/volume
        /// 默认都是 `0x3f800000` = 1.0）⇒ **原版 `AudioCue` 的默认值就是 0.1 s**。
        /// <para>⚠️ 2026-11-02 `B120-W5b` 改前这里是 `0f`（= 永不冷却）。**行为影响 = 0**：
        /// 本仓两份 cue 表 `animfx_sounds.json`(513) / `battle_sounds.json`(27) **逐条都带这一格**
        /// （现读 missing = 0）⇒ `JsonUtility` 总会把真值盖上来，这条只影响「生成器漏写这一格」的将来。</para></summary>
        public float timeToPlayAgain = 0.1f;
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
        /// <summary>🆕 2026-10-15（`T06` · 战斗音响）：**战斗那一族**的 cue 表。
        /// 取值链 = `BattleManager.soundCollection`(`+0x80`) → `SoundAssetCollection` 的**字段** →
        /// Addressables `AssetReference` → cue 资产（`AudioCue`）→ `clipList`。
        /// 🔴 **它的 `name` 列是【原版字段名】**（`startTurn` / `endTurnButton` …），不是 cue 资产名 —— 🆕 2026-11-03（`B121-D2`）**例外**：表里另有 `group=scene`(4) / `group=event`(2) 共 6 条，`name` 是 **cue 资产名**（`Victory`/`Defeat`/`OvertimeStart`/`Blackout`/两条活动 jingle），**手写**（引用者在场景/预制体序列化字段与活动 SO 上，无自动取材口）⇒ 重跑生成器会丢，由 `Editor/B121_D2_Assert.cs` C1b 逐名钉住。
        /// C# 那边照原版 `soundCollection.startTurn` 那句按**字段名**索引。
        /// 产出方 = `工具/import_battle_sfx.py`（A 组 `collection` 16 + B 组 `card` 11 = 自动取材；**C 组 = 手写组 `HAND_WRITTEN`**：`scene` 5 + `event` 2，引用者在场景/预制体/活动 SO 上、无自动取材口 ⇒ 只有「谁指向哪条 cue」手写，clips/音量/音高/ttp 仍当场从 cue 束重解）。⚠️ **2026-11-03 就地订正（`B121-FIX`）**：原文写「它**只产出 `collection`/`card` 两组**，上面那 6 条不在它的取材面里」——**已过期**：C 组已于同日 `B121-AUD-03` 并进生成器（现 7 条，含 `B121-AUD-05` 的 `Draw`）⇒ 重跑生成器**不再丢**它们。**本类管【三张表】**（`animfx_sounds` 513 条 · `battle_sounds` 现读 **34 条 / 四组** · `remnant_sounds` 6 条），⛔ 别在别处再维护第四份 cue 名单。</summary>
        public const string BattleTablePath = "battle_sounds";
        /// <summary>🆕 2026-11-02（`B120-W17` · `B120-F15`）：**残骸体那六条 cue** 的表。
        /// 改前那 6 个 cue 名（`Aeldari To Waystone` / `Aeldari Waystone Collect` / `Aeldari Waystone Destruction` /
        /// `CardShatter` / `NecronsCardReanimate` / `RemnantsDestroyed`）在 `animfx_sounds.json` 513 条里**零命中**
        /// ⇒ `RemnantSfx` 拿不到 `WFSoundCue` 实例，只能用 `(阵营, 时机)` 下标当冷却键。
        /// 这 6 条在原版是 `RemnantBody` / `RemnantAeldari` / `RemnantNecrons` 上**序列化的 `AudioCue` 字段**
        /// （`toRemnantSound` `+0x38` · `deathSound` `+0x50` · `waystoneCollectSound` `+0x70` · `reanimateSound` `+0x90`），
        /// 不在 AnimFX 的 `sounds[*].sound` 清单里 ⇒ 必须由**另一个取材口**产出。
        /// 产出方 = `工具/import_remnant_sfx.py`（⛔ 别手改生成物；裸跑 = 用法 + rc 2，写盘要显式 `--write`）。</summary>
        public const string RemnantTablePath = "remnant_sounds";
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

            // 🆕 2026-10-15（`T06`）：**三张表装进同一个字典**（一次查表 = 一条路，⛔ 不是三条）。
            //   ① `animfx_sounds` —— 特效 / 起拖 / 奖励窗那些（`import_original_sfx.py`）
            //   ② `battle_sounds`  —— 战斗「时机 → cue」28 条（`import_battle_sfx.py`，键 = **原版字段名**）
            //   🆕 2026-11-02（`B120-W17` · `F15`）：
            //   ③ `remnant_sounds` —— 残骸体那 6 条 `AudioCue`（`import_remnant_sfx.py`，键 = **cue 资产名**）
            foreach (var path in new[] { TablePath, BattleTablePath, RemnantTablePath })
            {
                var ta = Resources.Load<TextAsset>(path);
                if (ta == null)
                {
                    // 不许静默：表没了就等于**那一整族都不响**，而那看起来像「原版就没声音」。
                    Debug.LogWarning($"[WarpforgeVFX] 音效表 `Resources/{path}.json` 不在 —— " + MissingTableAdvice(path));
                    continue;
                }
                WFSoundTable tbl;
                try { tbl = JsonUtility.FromJson<WFSoundTable>(ta.text); }
                catch (Exception e)
                {
                    Debug.LogWarning($"[WarpforgeVFX] 音效表 `{path}` 解析不了：{e.Message}");
                    continue;
                }
                if (tbl == null || tbl.cues == null) continue;
                foreach (var c in tbl.cues)
                {
                    if (c == null || string.IsNullOrEmpty(c.name)) continue;
                    // 🔴 撞名**不许静默覆盖**（后装的会把先装的顶掉，而调用方按名字拿到的就是另一个 cue）
                    if (_byName.ContainsKey(c.name))
                    {
                        Debug.LogWarning($"[WarpforgeVFX] cue 名 `{c.name}` 在音效表里重复（后装的会被顶掉）" +
                                         $"（本张 = `{path}`）—— 保留先装的那一份，`{path}` 这一条被忽略。");
                        continue;
                    }
                    _byName[c.name] = c;
                }
            }
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

        // 🆕 2026-11-02（`B120-W17` · `F15`）：挂在**类尾**（⛔ 别往中间插）—— 本文件被 `账/*.tsv`
        //   按 `文件:行号` 引用（`账/音频_语音.tsv:4` 的行号 = `PickClip`）⇒ 新成员一律追加在它之后。
        /// <summary>表缺失时该跑哪个生成器（三张表 = 三个取材口，⛔ 别让人去跑错的那一个）。</summary>
        static string MissingTableAdvice(string path)
        {
            if (path == BattleTablePath)
                return "战斗那 28 条「时机 → cue」一条都不会播（战斗会变默片）。跑 `工具/import_battle_sfx.py` 生成它。";
            if (path == RemnantTablePath)
                return "残骸体那六条 cue 查不到（`RemnantSfx` 只能用 (阵营, 时机) 下标当冷却键）。"
                     + "跑 `python 工具/import_remnant_sfx.py --write` 生成它（⛔ 别手改；裸跑只打印用法）。";
            return "特效的所有 `sounds`/`exitSounds` 都不会播。跑 `工具/import_original_sfx.py` 生成它。";
        }
    }
}
