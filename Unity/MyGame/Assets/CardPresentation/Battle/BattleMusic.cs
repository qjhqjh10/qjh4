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
//  🔴 **我们这一侧是【等价物、不是原件】**（如实记，铁律 5）：原版靠 **mixer 的 4 个快照**
//     （`BattleMusicLow/Medium/High/NoBattleMusic`）硬切音轨、由 `AudioMixer.TransitionTo(t)` 做过渡；
//     我们**不动 mixer 资产**，改成**直接在代码里对三条轨的音量做线性淡变**（时长照抄 3/5/5/2 秒）。
//     ⚠️ **三条轨仍然挂在我们现有的 `Music` 组下**（`WarpforgeAudio.MusicGroup`）——
//       否则设置窗那根「音乐」滑块对 BGM **不生效**（那才是真偏离）。
//     ⚠️ 听感差异：mixer 的 `TransitionTo` 在 dB 域过渡，我们在线性振幅域 —— 起止点相同、中段略不同。
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
        float[] _from = new float[3], _to = new float[3];
        bool _ended;                      // `FinishMatch()` 之后不再自动切档（原版 `hasMatchEnded`）

        /// <summary>表里那一行（没载入 = null）。</summary>
        public Row Config { get { return _row; } }
        /// <summary>自检读：当前听得见的档（`-1` = 静音 / 还没开始）。</summary>
        public int CurrentLevel { get { return _level; } }
        public bool Ended { get { return _ended; } }
        /// <summary>自检读：第 i 条轨的**目标**音量（0..1；淡变途中会与实得值不同）。</summary>
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
                s.outputAudioMixerGroup = WarpforgeAudio.MusicGroup;   // 🔴 判据 见文件头「等价物」那段
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
            _checkAcc = 0f;
            _level = LevelSilent;
            StartFade(LevelLow, FadeInLowSeconds);
        }

        /// <summary>每帧推进（⚠️ **必须挂在 `BattleDriver.AdvanceTimeline` 那个泵上** —— 批处理没有帧循环）。</summary>
        public void Tick(float dt, float heroMinHealth)
        {
            if (_row == null) return;
            StepFade(dt);
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

        // ---- 淡变（我们这一侧的实现；原版是 mixer 快照过渡） ----

        void StartFade(int target, float dur)
        {
            _fadeTo = target;
            _fadeT = 0f;
            _fadeDur = Mathf.Max(0.0001f, dur);
            for (int i = 0; i < 3; i++) _from[i] = _src[i] != null ? _src[i].volume : 0f;
            _to[0] = target == LevelLow ? 1f : 0f;
            _to[1] = target == LevelMedium ? 1f : 0f;
            _to[2] = target == LevelHigh ? 1f : 0f;
            // 判据 ⑥ 那一半：要淡入的轨**先对齐兄弟轨的时间**再续播（三条轨是同步分轨）。
            for (int i = 0; i < 3; i++)
            {
                var s = _src[i];
                if (s == null || _to[i] <= 0f) continue;
                if (!s.isPlaying)
                {
                    int ts = ActiveTimeSamples(i);
                    if (ts >= 0 && s.clip != null && ts < s.clip.samples) s.timeSamples = ts;
                    s.UnPause();
                }
            }
        }

        void StepFade(float dt)
        {
            if (_fadeDur <= 0f) return;
            _fadeT = Mathf.Min(_fadeDur, _fadeT + dt);
            float k = _fadeT / _fadeDur;
            for (int i = 0; i < 3; i++)
                if (_src[i] != null) _src[i].volume = Mathf.Lerp(_from[i], _to[i], k);
            if (k < 1f) return;
            // 淡变落地：**静音下来那几条 Pause 掉**（判据 ⑥；省 CPU，也让 `timeSamples` 停在原处等下次对齐）
            for (int i = 0; i < 3; i++)
                if (_src[i] != null && _to[i] <= 0f && _src[i].isPlaying) _src[i].Pause();
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
            for (int i = 0; i < 3; i++) _src[i] = null;
        }
    }
}
