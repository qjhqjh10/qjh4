// BattleMusic.cs — **战斗 BGM**（原版 `BattleMusicManager` + 三个 `AudioSource` 的强度分轨）
//
// ============================================================================================
//  判据（逐条亲读，全文 → `资料/普查产出_1016战斗/T06d_战斗BGM判据.md`）
// ============================================================================================
//  ① **曲子不是一张全局歌单** —— 每个战场场景自带三个子 GO（`Music Low/Medium/High Intensity`），
//     各挂一个 `AudioSource(playOnAwake, loop)`；曲子的 `AudioClip` **烘在场景里**
//     （🔴 指针在 `AudioSource.m_Resource` 上、`m_audioClip` 恒 0 —— 本仓已记的坑）。
//  ② 三条轨**从 0 秒起同时播**，靠混音器快照**硬切**哪一条听得见（−80 dB = 静音）。
//  ③ 档位判据（`BattleMusicManager__CheckMusicState.c:24-78`）：
//       `h = min(双方督军【当前生命】)`
//       `h > midHealthChange(18)` → Low · `highHealthChange(10) < h <= 18` → Medium · `h <= 10` → High
//     ⚠️ 取**两者里较低的那个**；`midHealthChange` 字段名骗人，**它就是血线阈值、不是 Δ**。
//     ⚠️ 13 场序列化一律 **18 / 10**（C# 里 `midHealthChange` 的初值 20 **不是**落地值）。
//  ④ 时序：进场**瞬时静音** → **3 s 淡入 Low** → 每 **5 s** 查一次档、**5 s** 淡变 →
//     结算 `FinishMatch()` 回 **Low（5 s，不是静音）** → 开门 `FadeOut()` → **2 s** 静音。
//  ⑤ `useMultipleTracks == false` ⇒ 原版 `Initialize()` 直接 `enabled = useMultipleTracks`
//     ⇒ **组件停用、永不切档**（单曲从头放到尾）。⚠️ 与「有几首曲子」**不是一回事**
//     （astra 是 `=1` 但只有一首；tau/genestealers/sororitas 是 `=0`）。
//  ⑥ 旁带件 `AudioSourceTogglerByVolume`（**省 CPU**）：某条轨所在组的音量 `<= -60 dB` 且在播 ⇒ `Pause()`；
//     `> -60 dB` 且没在播 ⇒ **从兄弟轨取 `timeSamples` 对齐后** `Play()`。
//
// ============================================================================================
//  🔴 **2026-10-31（`B109_E17`）就地订正（铁律 5）**：原来这里写「我们不动 mixer 资产、改用代码线性淡变」——
//     **不成立了**。`Resources/Audio/` 现读三份 mixer（`Main Mixer` + `Music` + `FX`），三条轨挂
//     **Music 子 mixer 的 `BattleGroupLow/Medium/High`**，切档 = `AudioMixerSnapshot.TransitionTo(t)`
//     （时长照抄 3/5/5/2 秒），与原版同构。⚠️ 三条轨**常驻在播**、源音量恒 1.0，响度全由组音量说了算
//     （否则「代码音量 × 快照 −80 dB」= 静音 ⇒ Medium/High 两条轨永远听不见）。
//     ⚠️ 音乐滑块仍生效：Music 子 mixer 的输出挂在主 mixer 的 `Music` 组下（`VolumeMusic` 在那一级）。
//
//  数据：`Resources/battle_bgm.json`（产出方 `工具/import_battle_bgm.py`，逐场从 shipped bundle 实读）；
//  clip 在 `Resources/Art/audio/bgm/<clip 名>.ogg`（`Resources/Art/` 整个被 `.gitignore` 排除）。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>战斗 BGM 播放器。**自建于自己的根节点**（不挂在 HUD 上 —— 它要活得比 HUD 收起来久）。</summary>
    public class BattleMusic : MonoBehaviour
    {
        // ---- 时长常数（逐条来自判据 ④；`timeToCheck` 见 ①·③ 那张表的 `timeToCheck = 5`） ----
        public const float FadeInLowSeconds = 3f;      // 进场：瞬时静音 → 3 s 淡入 Low
        public const float FadeToLevelSeconds = 5f;    // 切档：5 s 交叉淡变
        public const float FadeFinishSeconds = 5f;     // `FinishMatch()`：回 Low，5 s（**不是静音**）
        public const float FadeOutSeconds = 2f;        // 开门：2 s 淡到静音
        /// <summary>「静音」那一档（原版 `NoBattleMusic` 快照）。</summary>
        public const int LevelSilent = -1;
        public const int LevelLow = 0, LevelMedium = 1, LevelHigh = 2;

        public const string TablePath = "battle_bgm";
        public const string ClipRoot = "Art/audio/bgm/";

        [Serializable] public class Row
        {
            public string arena = "";
            public int useMultipleTracks;
            public float midHealthChange = 18f, highHealthChange = 10f;
            public float timeToChange = 5f, timeToCheck = 5f;
            public string low = "", medium = "", high = "";
        }
        /// <summary>表的外层（`arena` 那 13 行）。⚠️ `public` 是**给自检读表**用的
        /// （`JsonUtility` 只认固定字段 + 数组 ⇒ 表必须有个能 FromJson 的类型；
        ///  ⛔ 别把它挪成 private，那会让 `BattleScene` 那边只剩「再抄一个形状」这条路）。</summary>
        [Serializable] public class Table { public Row[] arenas = new Row[0]; }

        // ---- 状态 ----
        AudioSource[] _src = new AudioSource[3];
        Row _row;
        float _checkAcc;
        int _level = LevelSilent;         // 当前**听得见**的档（-1 = 静音）
        int _fadeTo = LevelSilent;        // 淡变目标
        float _fadeT, _fadeDur;           // 淡变进度 / 总时长
        float[] _to = new float[3];       // 档位 → 哪一条该响（1 = 该响）。源音量恒 1.0，响度由 mixer 组音量定
        bool _ended;                      // `FinishMatch()` 之后不再自动切档（原版 `hasMatchEnded`）

        /// <summary>表里那一行（没载入 = null）。</summary>
        public Row Config { get { return _row; } }
        /// <summary>自检读：当前听得见的档（`-1` = 静音 / 还没开始）。</summary>
        public int CurrentLevel { get { return _level; } }
        public bool Ended { get { return _ended; } }
        /// <summary>自检读：第 i 条轨**该不该响**（1 = 该响 / 0 = 不响）。⚠️ 2026-10-31 起**不再是音量** —— 响度由 mixer 组音量（快照）说了算、源音量恒 1.0。</summary>
        public float TargetVolume(int i) { return _to[Mathf.Clamp(i, 0, 2)]; }
        /// <summary>自检读：第 i 条轨挂上去的 clip 名。</summary>
        public string ClipName(int i)
        {
            var s = _src[Mathf.Clamp(i, 0, 2)];
            return (s != null && s.clip != null) ? s.clip.name : null;
        }
        /// <summary>自检读：第 i 条轨现在是 Pause 着的吗（判据 ⑥ 那一半）。</summary>
        public bool Paused(int i) { return _src[Mathf.Clamp(i, 0, 2)] != null && !_src[Mathf.Clamp(i, 0, 2)].isPlaying; }

        /// <summary>建一个（**自建根节点**）。取不到表也建得出来（那一档就静音 + 出声）。</summary>
        public static BattleMusic Create()
        {
            var go = new GameObject("BattleMusic");
            go.hideFlags = HideFlags.DontSave;          // 运行时对象，别被场景/构建持久化（同 `WFSoundPlayer` 那条）
            return go.AddComponent<BattleMusic>();
        }

        /// <summary>按**战场 key**（`ArenaRuntimeLoader.ResolveArenaKey` 的返回值，如 `battlearena1`）起三条轨。</summary>
        public void Init(string arenaKey)
        {
            var ta = Resources.Load<TextAsset>(TablePath);
            if (ta == null)
            {
                Debug.LogWarning($"[BattleMusic] 表 `Resources/{TablePath}.json` 不在 ⇒ **战斗没有 BGM**"
                               + "（跑一次 `python 工具/import_battle_bgm.py`）。");
                return;
            }
            Table t;
            try { t = JsonUtility.FromJson<Table>(ta.text); }
            catch (Exception e) { Debug.LogWarning($"[BattleMusic] 表解析不了：{e.Message}"); return; }
            if (t == null || t.arenas == null) return;

            foreach (var r in t.arenas) if (r != null && r.arena == arenaKey) { _row = r; break; }
            if (_row == null)
            {
                // 不许静默：认不出这一场就等于「这一局没 BGM」，而那看起来像「原版就没有」。
                Debug.LogWarning($"[BattleMusic] 表里没有 `{arenaKey}` 这一场 ⇒ 本局无 BGM"
                               + "（表里有的：见 `Resources/battle_bgm.json`；⚠️ key 要与 `ArenaRuntimeLoader` 那个一致）");
                return;
            }

            string[] names = { _row.low, _row.medium, _row.high };
            for (int i = 0; i < 3; i++)
            {
                var clip = string.IsNullOrEmpty(names[i]) ? null : Resources.Load<AudioClip>(ClipRoot + names[i]);
                if (clip == null)
                {
                    Debug.LogWarning($"[BattleMusic] 第 {i} 条轨的 clip 加载不到：`{ClipRoot}{names[i]}`"
                                   + "（`Resources/Art/` 是 gitignore 的 ⇒ 新克隆要跑 `工具/import_battle_bgm.py`）");
                    continue;
                }
                var s = gameObject.AddComponent<AudioSource>();
                s.clip = clip;
                s.loop = true;
                s.playOnAwake = false;
                s.volume = 0f;
                AudioSourceMixerGroupFixerInstaller.Attach(s, i == 0 ? MixerType.BattleGroupLow : i == 1 ? MixerType.BattleGroupMedium : MixerType.BattleGroupHigh);   // 🔴 原版：三条轨各挂 BattleGroup Low/Medium/High（2026-11-02 `B120-W11`：改走 fixer 唯一映射口）
                s.Play();                                              // 三条**同时从 0 秒起播**（判据 ②）
                s.Pause();                                             // 先全停（进场那一刻才淡入）
                _src[i] = s;
            }
            if (_row.useMultipleTracks == 0)
                Debug.Log($"[BattleMusic] `{arenaKey}`：`useMultipleTracks = 0` ⇒ **本场不切档**（判据 ⑤）");
        }

        /// <summary>开局：**瞬时静音 → 3 s 淡入 Low**（判据 ④ 前两拍；原版第 0 帧静音、第 1 帧才淡入）。</summary>
        public void BeginBattle()
        {
            _ended = false;
            _checkAcc = 0f; _level = LevelSilent;
            var mute = WarpforgeAudio.GetSnapshot(WarpforgeAudio.SnapshotsType.MusicBattleMusicNoMusic); if (mute != null) mute.TransitionTo(0f);   // 原版：进场先**瞬时静音**
            WarpforgeAudio.ToggleSFX(false, true); _openBeatPending = true;   // 第 0 拍：FX 总线瞬时压到 −80 dB（原版 `_Start_d__13` state 0 的 `ToggleSFX(false, instant:true)`）；第 1 拍留给下一帧 `Tick`（state 1）
        }

        /// <summary>每帧推进（⚠️ **必须挂在 `BattleDriver.AdvanceTimeline` 那个泵上** —— 批处理没有帧循环）。</summary>
        public void Tick(float dt, float heroMinHealth)
        {
            if (_openBeatPending) { _openBeatPending = false; WarpforgeAudio.ToggleSFX(true); StartFade(LevelLow, FadeInLowSeconds); }   // 第 1 拍 = 原版 `_Start_d__13` state 1：FX **2 s 淡回**（`instant=false`）+ Low **3 s 淡入**。⚠️ 必须在下面 `_row == null` 早退**之前** —— 否则 BGM 表缺失时 FX 会永久压在 −80 dB
            if (_row == null) return;
            StepFade(dt);
            UpdatePauseByVolume();   // 判据 ⑥：每帧按**组音量 dB** 决定 Pause/Play（原版 `AudioSourceTogglerByVolume.Update`）
            if (_ended || _row.useMultipleTracks == 0) return;   // 判据 ⑤：单曲场永不切档
            _checkAcc += dt;
            float every = _row.timeToCheck > 0f ? _row.timeToCheck : FadeToLevelSeconds;
            if (_checkAcc < every) return;
            _checkAcc = 0f;
            int want = LevelFor(heroMinHealth, _row.midHealthChange, _row.highHealthChange);
            if (want == _fadeTo) return;                          // 目标没变 ⇒ 什么都不做（不重起淡变）
            StartFade(want, _row.timeToChange > 0f ? _row.timeToChange : FadeToLevelSeconds);
        }

        /// <summary>结算：**回 Low，5 s**（判据 ④；⛔ 不是「静音」—— 那是 `FadeOut()` 的活）。</summary>
        public void FinishMatch()
        {
            if (_row == null) return;
            _ended = true;                                        // 之后不再自动切档（原版 `hasMatchEnded`）
            StartFade(LevelLow, FadeFinishSeconds);
        }

        /// <summary>开门：**2 s 淡到静音**（判据 ④ 最后一拍）。</summary>
        public void FadeOut()
        {
            if (_row == null) return;
            _ended = true;
            StartFade(LevelSilent, FadeOutSeconds);
        }

        /// <summary>🔴 **纯函数**：血线 → 档位（判据 ③）。拆出来是为了能断言（不用跑一局）。
        /// ⚠️ 调它之前**先自己取 min(双方督军生命)** —— 见 <see cref="HeroMinHealth"/>。</summary>
        public static int LevelFor(float heroMinHealth, float midHealthChange, float highHealthChange)
        {
            if (heroMinHealth > midHealthChange) return LevelLow;
            if (heroMinHealth > highHealthChange) return LevelMedium;
            return LevelHigh;
        }

        /// <summary>`min(双方督军【当前生命】)`（判据 ③ 的第一句）。取不到 = `float.MaxValue`
        /// （⇒ 判成 Low —— 与原版「生命满着」同档）。</summary>
        public static float HeroMinHealth(RuleEngine.BattleContext ctx)
        {
            if (ctx == null) return float.MaxValue;
            float a = HeroHealth(ctx, 0), b = HeroHealth(ctx, 1);
            return Mathf.Min(a, b);
        }

        static float HeroHealth(RuleEngine.BattleContext ctx, int side)
        {
            var board = ctx.Players[side].Board;
            var hero = board[RuleEngine.BoardSpec.WarlordSlot];
            // 原版 = `EntityScript.<currentHealth>`（督军那张卡上的当前生命）
            return hero != null ? hero.Health : float.MaxValue;
        }

        // ---- 淡变：**快照过渡**（与原版同机制）＋ `_to[]` 记账（哪一条该响，供 `TargetVolume` 与 Pause 判据读） ----

        void StartFade(int target, float dur)
        {
            _fadeTo = target;
            _fadeT = 0f;
            _fadeDur = Mathf.Max(0.0001f, dur);
            var snap = WarpforgeAudio.GetSnapshot(SnapOf(target));
            if (snap != null) snap.TransitionTo(dur);              // 原版 = mixer 快照过渡（取不到会在 GetSnapshot 里出声）
            _to[0] = target == LevelLow ? 1f : 0f;
            _to[1] = target == LevelMedium ? 1f : 0f;
            _to[2] = target == LevelHigh ? 1f : 0f;
            // 判据 ⑥：三条轨**常驻在播**（快照抬组音量时要有声音）、源音量恒 1.0。
            // 🔴 2026-11-02（`B120-W5b`）：**这里不再「三条全部 UnPause + 对齐 timeSamples」** ——
            //   原版是另一颗 `AudioSourceTogglerByVolume` 每帧按**组音量 dB** 决定，见 `UpdatePauseByVolume`。
            //   `StartFade` 只负责「源音量恒 1.0」与 `_to[]` 记账。
            for (int i = 0; i < 3; i++)
            {
                var s = _src[i]; if (s == null) continue; s.volume = 1f;
            }
        }

        // ============================================================================================
        //  🔴 2026-11-02（`B120-W5b`）：**判据 ⑥ 的原版实现** —— `AudioSourceTogglerByVolume`
        // ============================================================================================
        //  判据（逐行现读 `d:/2/tools/decomp_full/AudioSourceTogglerByVolume__Update.c`）：
        //    ```
        //    mixer.GetFloat(audiomixerVolumeLabel, &v);           // 读**这个源所在混音组**的音量（dB）
        //    if (!src.isPlaying) { if (-60 < v) { ts = GetActiveBrotherTimeSample(); src.timeSamples = ts; src.Play(); } }
        //    else                { if (v <= -60) src.Pause(); }
        //    ```
        //  常量 `DAT_1834b3278 = -60`（dB，`工具/read_literal.py` 4B 实读）。
        //  `audiomixerVolumeLabel` 三个值 = **`TrackLowVolume` / `TrackMediumVolume` / `TrackHighVolume`**
        //  （判据 = 13 场战场场景里那三颗 `AudioSourceTogglerByVolume` 的序列化字段，2026-11-02 实读
        //   `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_{4445,4306,4741}.json`；
        //   `audioSource` 分别指向 1597/1599/1601 = Low/Medium/High 三条轨，`timeSyncAudioSources` = 另外两条）。
        //  ⚠️ 我们的 `Resources/Audio/Music.mixer` **正好暴露了这三个名字**（`m_ExposedParameters`，现读），
        //     所以 `mixer.GetFloat` 能读到快照过渡中的实时 dB —— 不需要自己插值。
        //  ⚠️ `timeSyncAudioSources` 的顺序是「除自己以外的两条」，取值逻辑 = **第一个在播的**兄弟的 `timeSamples`，
        //     一个都没在播 ⇒ **0**（`GetActiveBrotherTimeSample` 的默认返回）。我们的 `ActiveTimeSamples` 同一语义。
        // ⛔ **别把 −80 dB 的「NoBattleMusic 快照」当成阈值** —— 阈值是 −60，−80 只是那个快照的落点。
        // ============================================================================================

        /// <summary>三个混音组暴露参数名（顺序 = Low / Medium / High，与 `_src[]`、`BattleGroupGroup(i)` 同序）。
        /// 判据 = 那三颗 `AudioSourceTogglerByVolume.audiomixerVolumeLabel` 的序列化值。</summary>
        static readonly string[] TrackVolumeLabels = { "TrackLowVolume", "TrackMediumVolume", "TrackHighVolume" };

        /// <summary>暂停/恢复阈值（dB）。判据 = `AudioSourceTogglerByVolume__Update.c` 的 `DAT_1834b3278`。
        /// 语义：组音量 **&gt; −60** 且没在播 ⇒ 对齐兄弟轨后 `Play()`；**&lt;= −60** 且在播 ⇒ `Pause()`。</summary>
        public const float VolumePauseThresholdDb = -60f;

        /// <summary>原版 `AudioSourceTogglerByVolume.Update` 的逐帧等价实现（每帧一次，见 `Tick`）。
        /// ⚠️ 与改前的差别：改前是「淡变**结束时**把不响的那几条 Pause、`StartFade` 时三条**全部** UnPause」
        /// ⇒ ① 暂停时刻晚于原版（原版在 dB 穿越 −60 那一刻）；② 起淡变时会把**还没升起来**的轨也解暂停。
        /// 现在完全按组音量 dB 判，两条都对上。</summary>
        void UpdatePauseByVolume()
        {
            for (int i = 0; i < 3; i++)
            {
                var s = _src[i];
                if (s == null) continue;
                float db;
                if (!TryGetTrackDb(s, TrackVolumeLabels[i], out db)) continue;   // 没有混音组/参数名对不上 ⇒ 不动它（已在别处出声）
                if (ShouldBePlaying(db))
                {
                    if (!s.isPlaying)
                    {
                        int ts = ActiveTimeSamples(i);        // 没有兄弟在播 ⇒ -1 ⇒ 原版取 0
                        if (ts < 0) ts = 0;
                        if (s.clip != null && ts < s.clip.samples) s.timeSamples = ts;
                        s.Play();
                    }
                }
                else if (s.isPlaying)
                {
                    s.Pause();
                }
            }
        }

        /// <summary>**纯函数**（拆出来是为了能在批处理里断言，不必真让 mixer 过渡）：组音量 dB ⇒ 这一轨该不该在播。
        /// 判据 = `AudioSourceTogglerByVolume__Update.c` 两条分支：`-60 < v` ⇒ 该播；`v <= -60` ⇒ 该停。
        /// ⚠️ 阈值是 **−60**（`DAT_1834b3278`），⛔ 不是 −80（那是 `NoBattleMusic` 快照的落点，不是判据）。</summary>
        public static bool ShouldBePlaying(float groupDb) { return VolumePauseThresholdDb < groupDb; }

        /// <summary>读某个源**所在混音组**的当前音量（dB）。原版做法一致：`src.outputAudioMixerGroup.audioMixer.GetFloat(label, …)`。
        /// 取不到（没挂组 / mixer 为 null / 参数名没暴露）⇒ `false`（调用方跳过这一轨，⛔ 不猜一个 dB）。</summary>
        static bool TryGetTrackDb(AudioSource s, string label, out float db)
        {
            db = 0f;
            var group = s != null ? s.outputAudioMixerGroup : null;
            if (group == null) return false;
            var mixer = group.audioMixer;
            if (mixer == null) return false;
            return mixer.GetFloat(label, out db);
        }

        void StepFade(float dt)
        {
            if (_fadeDur <= 0f) return;
            _fadeT = Mathf.Min(_fadeDur, _fadeT + dt);
            float k = _fadeT / _fadeDur;
            // 🔴 2026-10-31（`B109_E17`）：**不再逐轨插值音量** —— 响度由 Music 子 mixer 的
            //   `BattleGroupLow/Medium/High` 说了算、由 `StartFade` 里的快照 `TransitionTo` 驱动；
            //   源音量恒 1.0，`_to[]` 只表示「哪一条该响」。
            // 🔴 2026-11-02（`B120-W5b`）：**这里也不再「淡变结束时把不响的那几条 Pause」** —— 暂停/恢复
            //   归 `UpdatePauseByVolume`（按组音量 dB 穿越 −60 那一刻，= 原版 `AudioSourceTogglerByVolume.Update`）。
            if (k < 1f) return;
            _level = _fadeTo;
            _fadeDur = 0f;
        }

        /// <summary>别的轨的 `timeSamples`（用来把刚淡入的那条对齐过去）；没有在播的 = `-1`。</summary>
        int ActiveTimeSamples(int except)
        {
            for (int i = 0; i < 3; i++)
                if (i != except && _src[i] != null && _src[i].isPlaying) return _src[i].timeSamples;
            return -1;
        }

        public void OnDestroy()
        {
            if (_openBeatPending) { _openBeatPending = false; WarpforgeAudio.ToggleSFX(true); }   // 安全网：⛔ 绝不把整条 FX 总线留在 −80 dB（第 1 拍还没解就销毁 ⇒ 这里兜底；原版没有这个口，因为它的协程绑在不会被提前销毁的场景组件上）
            for (int i = 0; i < 3; i++) _src[i] = null;
        }

        /// <summary>档位 → mixer 快照（原版 `BattleMusicManager` 就是按档切这四条快照；判据 → `B109_R11` §1.4）。
        /// ⚠️ `LevelSilent`（−1）走 `NoBattleMusic`；其余非法值一律按 `BattleMusicLow`。</summary>
        static WarpforgeAudio.SnapshotsType SnapOf(int level)
        {
            if (level == LevelSilent) return WarpforgeAudio.SnapshotsType.MusicBattleMusicNoMusic;
            if (level == LevelMedium) return WarpforgeAudio.SnapshotsType.MusicBattleMusicMedium;
            if (level == LevelHigh) return WarpforgeAudio.SnapshotsType.MusicBattleMusicHigh;
            return WarpforgeAudio.SnapshotsType.MusicBattleMusicLow;
        }

        // ---- 类尾追加（⛔ 别插进上面任何成员之间：`账/音频_语音.tsv` 有 4 行按行号引用本文件）----

        /// <summary>开场两拍的第 2 拍还没解（`BeginBattle` 置真、下一次 `Tick` / `OnDestroy` 清）。
        /// 判据 = 原版 `BattleMusicManager._Start_d__13.MoveNext`：state 0 结尾 `yield WaitForEndOfFrame`，
        /// state 1 才 `ToggleSFX(true)`（2 s 淡回）+ `battleLow.TransitionTo(3f)`（`decomp_full/..._MoveNext.c:34,37-38,85,88`）。</summary>
        bool _openBeatPending;

        /// <summary>自检读：第 1 拍还欠着吗（`true` = FX 总线当前被压在 `NoVolume`、BGM 还在静音）。</summary>
        public bool OpenBeatPendingForTest { get { return _openBeatPending; } }
    }
}
