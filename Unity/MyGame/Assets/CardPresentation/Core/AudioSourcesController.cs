// AudioSourcesController.cs — 原版 `AudioSourcesController`（**普通 `[Serializable]` 类，不是 MonoBehaviour**）
//
// ============================ 出处（唯一判据） ============================
// 第一权威 = `d:/2/tools/decomp_full/AudioSourcesController__*.c`（逐方法体）+ 三个异步状态机：
//   `AudioSourcesController._IncrementVolume_d__14__MoveNext.c` / `._DecrementVolume_d__16__MoveNext.c` /
//   `._VolumeLerp_d__18__MoveNext.c`；常量 = `AudioSourcesController__.cctor.c:9-11`。
// 类形状（不是 MonoBehaviour）= `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AudioSourcesController.cs:9`
//   （`[Serializable] public class AudioSourcesController`）＋ 原版 `MusicManager` 预制体里它是一段
//   **内嵌序列化对象**（`bundle_staticgeneralassets_assets_all/MonoBehaviour/MonoBehaviour_-1722128571091716916.json`
//   的 `_audioSources: { _firstAudioSource, _secondAudioSource }`），**全库没有它的 MonoBehaviour 资产**。
//   ⇒ 它自己**没有** `MonoBehaviour` 的协程能力，所以原版这里用的是 `async`/`Task`（不是协程）—— 这也解释了
//     `VolumeLerp` 的返回值是 `Task` 而不是 `IEnumerator`。
//
// ============================ 三个常量（原版静态表，逐值） ============================
// `AudioSourcesController__.cctor.c:9-11` 往静态 `float[]` 里写三个值（字节序实读）：
//   `[0] = 0x3f800000` = **1.0f**（MAX_VOLUME）· `[1] = 0` = **0.0f**（MIN_VOLUME）· `[2] = 0x40400000` = **3.0f**（LERP_DURATION）
// ⚠️ 原版这三个是 `private static float`（由 `.cctor` 赋值），不是 `const`；这里写成 `const` 只为编译期可用，
//    数值逐位相同。
//
// ============================ 与「当前音量」有关的一条实读结论（⛔ 别照常识改） ============================
// `DecrementVolume` / `IncrementVolume` 传给 `VolumeLerp` 的起点**是常量、不是源的当前音量**：
//   · `DecrementVolume.c` ⇒ `VolumeLerp(audioSource, 表[0]=1.0f, 表[1]=0.0f)`（1 → 0）
//   · `IncrementVolume.c` ⇒ `VolumeLerp(audioSource, 表[1]=0.0f, 表[0]=1.0f)`（0 → 1）
// ⇒ 「从**当前**音量线性降到 0」那句说法（`判据/B9_音频C_施工单.tsv` 账行 20 的「原版逐条步骤 ②」）**与反编译不符**，
//    这里照反编译实现（= 起点恒 1.0 / 恒 0.0）。可达影响：同一个源上「淡出到一半又淡出」会先跳回 1.0 —— 原版就是
//    这样（`Blend` 的旧源正常情况恒为 1.0，看不出来）。
//
// ============================ 执行模型（🔶 自己设计，机制等价） ============================
// 🔶 自己设计（非原版）：用「绝对时刻 + 每帧一次 `Tick(now)`」替代原版的 `async Task` + 每帧 `await Task.Yield()` ·
//    判据上两者等价（原版 `VolumeLerp` 也是每帧读 `Time.time`、t 夹到 [0,1]），但**批处理/自检里没有帧循环、
//    也起不了协程/同步上下文**，而本工程的规矩是「任何行为都要能在 `-executeMethod` 自检里离线验」
//    ⇒ 必须有一个能被外部时钟推的口（`AdvanceForTest`）· 调度台 W11AU 2026-10-26 裁定（照派活包 A-01 的验收口
//    「断言（不需要 Unity）：FadeIn 后 0/1.5/3 s 三点读 volume」）。
// ⚠️ 时钟：播放态 `Now` = `Time.time`（**与原版同一个钟**，`VolumeLerp` 的起点就是 kickoff 那一刻的 `Time.time`）；
//    自检先调 `ResetClockForTest()` 把 `Now` 切成手动档，再用 `AdvanceForTest(dt)` 推。
//
// ⚠️ **同时起多条补间时互不取消**（原版也是这样：每次调用新建一个 `Task`）—— 同一个源上并发两条会互相覆盖写，
//    原版同样如此。⛔ 别"顺手"加取消逻辑（那会改掉 `Blend` 的观感）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `MusicTransitionType`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/MusicTransitionType.cs` 实读）：
    /// `FadeIn = 0 · Blend = 1 · Instant = 2`。⚠️ 这是**值类型**（不是 MonoBehaviour）⇒ 与 `AudioSourcesController`
    /// 同文件不会触发「类名必须 == 文件名」那条坑（那条只针对 `MonoBehaviour`/`ScriptableObject`）。</summary>
    public enum MusicTransitionType
    {
        FadeIn = 0,
        Blend = 1,
        Instant = 2,
    }

    /// <summary>原版 `AudioSourcesController`：两个 `AudioSource` 互为「主/备」，换曲时交叉淡变（3 s 线性）。
    /// <para>方法名单与可见性逐条照 ILSpy 桩（`Assembly-CSharp/AudioSourcesController.cs:137-191`）：
    /// `Init / PlayInstant / FadeIn / FadeOut / Blend / Stop` 是 `public`；`SwitchMainAudioSource / PlayClip /
    /// IncrementVolume / SetMaxVolume / DecrementVolume / SetMinVolume / VolumeLerp` 是 `private`。</para></summary>
    [Serializable]
    public class AudioSourcesController
    {
        // ---- 三个常量：`AudioSourcesController__.cctor.c:9-11` ----
        /// <summary>原版静态表 `[0] = 0x3f800000`。</summary>
        public const float MAX_VOLUME = 1.0f;
        /// <summary>原版静态表 `[1] = 0`。</summary>
        public const float MIN_VOLUME = 0.0f;
        /// <summary>原版静态表 `[2] = 0x40400000`（3 s）。</summary>
        public const float LERP_DURATION = 3.0f;

        // ---- 原版字段（+0x10 / +0x18 / +0x20；ILSpy 名字实读） ----
        [SerializeField] AudioSource _firstAudioSource;
        [SerializeField] AudioSource _secondAudioSource;
        AudioSource _mainAudioSource;

        /// <summary>当前主源（原版 `_mainAudioSource`，字段偏移 `+0x20`）。
        /// 原版所有方法都先取它、再判空（`FadeIn.c:22` / `Stop.c:6` …）。</summary>
        public AudioSource MainAudioSource { get { return _mainAudioSource; } }

        /// <summary>另一个源（不是当前主源的那一个）。**原版没有这个属性**，只为自检/诊断读。
        /// ⚠️ 只读；⛔ 别拿它绕过 `SwitchMainAudioSource` 改状态。</summary>
        public AudioSource OtherAudioSource
        {
            get { return _mainAudioSource == _firstAudioSource ? _secondAudioSource : _firstAudioSource; }
        }

        /// <summary>正在跑的补间条数（0 = 静）。**原版没有**，只给自检/诊断用。</summary>
        public int ActiveLerpCount { get { return _lerps.Count; } }

        // ---- 补间队列（= 原版 `VolumeLerp` 的 `Task`；见文件头「执行模型」） ----
        sealed class LerpJob
        {
            public AudioSource source;
            public float startValue, endValue, startTime;
            public Action onDone;
        }
        readonly List<LerpJob> _lerps = new List<LerpJob>();

        // ---- 时钟（见文件头「执行模型」） ----
        float _testNow = -1f;
        /// <summary>当前时刻。播放态 = `Time.time`（与原版同一个钟）；自检调过 `ResetClockForTest` 之后 = 手动档。</summary>
        public float Now { get { return _testNow >= 0f ? _testNow : Time.time; } }

        // ============================================================ 原版方法（逐条照 .c）

        /// <summary>原版 `Init()`（`AudioSourcesController__Init.c`）：`_mainAudioSource = _firstAudioSource`；
        /// 然后 `_secondAudioSource.enabled = false`（**第二个源出厂停用**）。
        /// <para>⚠️ 原版两源是**序列化字段**（预制体里挂好的）；我们的源是运行时建的 ⇒ 另给一个装配重载。</para></summary>
        public void Init()
        {
            _mainAudioSource = _firstAudioSource;
            if (_secondAudioSource != null) _secondAudioSource.enabled = false;
        }

        /// <summary>装配口（**原版没有**：原版的 `_firstAudioSource/_secondAudioSource` 是预制体里连好的）。
        /// = 赋值 + `Init()`，两步别拆开调。</summary>
        public void Init(AudioSource first, AudioSource second)
        {
            _firstAudioSource = first;
            _secondAudioSource = second;
            Init();
        }

        /// <summary>原版 `PlayInstant(AudioClip)`（`__PlayInstant.c`）：`volume = 表[0]`(1.0) → `enabled = true`
        /// → `clip = param` → `Play()`；返回当前主源。**无淡入**。</summary>
        public AudioSource PlayInstant(AudioClip audioClip)
        {
            var src = _mainAudioSource;
            if (src == null) { Fail("PlayInstant"); return null; }
            src.volume = MAX_VOLUME;
            src.enabled = true;
            src.clip = audioClip;
            src.Play();
            return src;
        }

        /// <summary>原版 `FadeIn(AudioClip)`（`__FadeIn.c`）：`volume = 表[1]`(0.0) → `enabled = true` → `clip = param`
        /// → `Play()` → 起 `IncrementVolume`（3 s 0→1）；返回当前主源。</summary>
        public AudioSource FadeIn(AudioClip audioClip)
        {
            var src = _mainAudioSource;
            if (src == null) { Fail("FadeIn"); return null; }
            src.volume = MIN_VOLUME;         // .c:31-32
            src.enabled = true;              // .c:34
            src.clip = audioClip;            // .c:36
            src.Play();                      // .c:38
            IncrementVolume(src);            // .c:64-65
            return src;
        }

        /// <summary>原版 `FadeOut()`（`__FadeOut.c`）：取当前主源，起 `DecrementVolume`（3 s 1→0，到点 `enabled=false`）。</summary>
        public void FadeOut()
        {
            var src = _mainAudioSource;
            if (src == null) { Fail("FadeOut"); return; }
            DecrementVolume(src);
        }

        /// <summary>原版 `Blend(AudioClip)`（`__Blend.c`）：`FadeOut()` → 切主源（与 `SwitchMainAudioSource` 同一句）
        /// → `FadeIn(clip)`；返回**新的**当前主源。</summary>
        public AudioSource Blend(AudioClip audioClip)
        {
            FadeOut();
            SwitchMainAudioSource();
            return FadeIn(audioClip);
        }

        /// <summary>原版 `Stop()`（`__Stop.c`）：`current.enabled = false` → `current.Stop()`。
        /// <para>⚠️ `MusicManager.StopMusic(instant: true)` 就是这两句的**内联**（`MusicManager__StopMusic.c:14-18`）——
        /// ⛔ 那边别再写第二份。</para></summary>
        public void Stop()
        {
            var src = _mainAudioSource;
            if (src == null) { Fail("Stop"); return; }
            src.enabled = false;             // .c:6
            src.Stop();                      // .c:8
        }

        // ---------------------------------------------------------- private（原版 private，逐条照抄）

        /// <summary>原版 `SwitchMainAudioSource()`（`__SwitchMainAudioSource.c`）：
        /// `current == _firstAudioSource ? _secondAudioSource : _firstAudioSource`（`Object.op_Equality`）。
        /// <para>⚠️ 原版是 `private` ⇒ 保持 private（自检走 `Blend` 间接覆盖它）。</para></summary>
        void SwitchMainAudioSource()
        {
            _mainAudioSource = _mainAudioSource == _firstAudioSource ? _secondAudioSource : _firstAudioSource;
        }

        /// <summary>原版 `PlayClip(AudioClip)`（`__PlayClip.c`）：`enabled = true` → `clip = param` → `Play()`（**不改音量**）。
        /// <para>⚠️ 原版全库**无调用点**（`grep -rl AudioSourcesController__PlayClip d:/2/tools/decomp_full` = 1，
        /// 只有它自己）⇒ 我们也没有调用点。留着是照抄类形状（它可能被 MonoBehaviour 的 `m_MethodName` 之类引用，
        /// 本批未查）⇒ 与 `SetMaxVolume` 一样标记为「原版即无 C# 调用点」。</para></summary>
        void PlayClip(AudioClip audioClip)
        {
            var src = _mainAudioSource;
            if (src == null) { Fail("PlayClip"); return; }
            src.enabled = true;
            src.clip = audioClip;
            src.Play();
        }

        /// <summary>原版 `IncrementVolume(AudioSource)`（`__IncrementVolume.c`）：起 `VolumeLerp(src, 0.0f, 1.0f)`；
        /// **到点写 `volume = 表[0]`(1.0)**（`.c` 的 MoveNext 末尾那一句）。</summary>
        void IncrementVolume(AudioSource audioSource)
        {
            if (audioSource == null) { Fail("IncrementVolume"); return; }
            StartLerp(audioSource, MIN_VOLUME, MAX_VOLUME, () =>
            {
                if (audioSource != null) audioSource.volume = MAX_VOLUME;
            });
        }

        /// <summary>原版 `SetMaxVolume(AudioSource)`（`__SetMaxVolume.c`）：`volume = 表[0]`(1.0)。
        /// ⚠️ 原版全库**无调用点**（同 `PlayClip`）—— 保持 private、照抄。</summary>
        void SetMaxVolume(AudioSource audioSource)
        {
            if (audioSource == null) { Fail("SetMaxVolume"); return; }
            audioSource.volume = MAX_VOLUME;
        }

        /// <summary>原版 `DecrementVolume(AudioSource)`（`__DecrementVolume.c`）：起 `VolumeLerp(src, 1.0f, 0.0f)`；
        /// **到点写 `volume = 表[1]`(0.0) 且 `enabled = false`**（MoveNext 末尾那一句）。</summary>
        void DecrementVolume(AudioSource audioSource)
        {
            if (audioSource == null) { Fail("DecrementVolume"); return; }
            StartLerp(audioSource, MAX_VOLUME, MIN_VOLUME, () =>
            {
                if (audioSource == null) return;
                audioSource.volume = MIN_VOLUME;
                audioSource.enabled = false;
            });
        }

        /// <summary>原版 `SetMinVolume(AudioSource)`（`__SetMinVolume.c`）：`volume = 表[1]`(0.0)。
        /// ⚠️ 原版无调用点 —— 保持 private、照抄。</summary>
        void SetMinVolume(AudioSource audioSource)
        {
            if (audioSource == null) { Fail("SetMinVolume"); return; }
            audioSource.volume = MIN_VOLUME;
        }

        /// <summary>原版 `VolumeLerp(AudioSource, float startValue, float endValue)`（`__VolumeLerp.c` +
        /// `._VolumeLerp_d__18__MoveNext.c`）：在 `LERP_DURATION`(3 s) 内线性 `start → end`，**每帧写一次 `volume`**；
        /// `t = (Time.time − startTime) / LERP_DURATION`，**夹到 [0, 1]**（上限常量 `0x1834b2bb8 = 1.0f`，本批实读）；
        /// `startTime + 3 <= Time.time` 即结束。
        /// <para>⚠️ 原版返回 `Task`（`DecrementVolume`/`IncrementVolume` 会 `await` 它，**结束之后**才写终值）；
        /// 这里由 `Tick` 在补间结束时回调 `onDone` 完成同一件事（顺序一致）。</para></summary>
        void VolumeLerp(AudioSource audioSource, float startValue, float endValue)
        {
            StartLerp(audioSource, startValue, endValue, null);
        }

        void StartLerp(AudioSource audioSource, float startValue, float endValue, Action onDone)
        {
            if (audioSource == null) return;
            _lerps.Add(new LerpJob
            {
                source = audioSource,
                startValue = startValue,
                endValue = endValue,
                startTime = Now,            // 原版：kickoff 那一刻的 Time.time
                onDone = onDone,
            });
        }

        // ============================================================ 泵 + 自检口

        /// <summary>推一帧：把所有在飞的补间按**绝对时刻** `now` 更新一次（原版每帧 `MoveNext` 做的是同一件事）。
        /// <para>调用点：`ShellRuntime.Update`（播放态）· `AdvanceForTest`（自检）。⛔ 别在别处再写一份插值。</para></summary>
        public void Tick(float now)
        {
            for (int i = _lerps.Count - 1; i >= 0; i--)
            {
                var j = _lerps[i];
                if (j.source == null) { _lerps.RemoveAt(i); continue; }

                float t = LERP_DURATION > 0f ? (now - j.startTime) / LERP_DURATION : 1f;
                bool done = t >= 1f;                    // = 原版 `startTime + LERP_DURATION <= now`
                if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
                j.source.volume = (j.endValue - j.startValue) * t + j.startValue;

                if (!done) continue;
                _lerps.RemoveAt(i);
                if (j.onDone != null) j.onDone();       // 原版：Task 结束之后，调用方写终值（+ 关 enabled）
            }
        }

        /// <summary>自检专用：把时钟切到**手动档**并置为 <paramref name="t"/>（此后再调 `AdvanceForTest` 才前进）。
        /// <para>⚠️ 只在自检里调；播放态**别调**（调了之后 `Now` 不再跟 `Time.time`，音乐会卡住不动）。</para></summary>
        public void ResetClockForTest(float t = 0f) { _testNow = t; }

        /// <summary>自检专用：手动推进 <paramref name="dt"/> 秒并推一帧（= 原版每帧 `await Task.Yield()` 那一步）。</summary>
        public void AdvanceForTest(float dt)
        {
            if (_testNow < 0f) _testNow = 0f;
            _testNow += dt;
            Tick(_testNow);
        }

        /// <summary>不许静默失败：原版这些方法在源为 null 时是**空引用桩**（`FUN_1803f47a0` 不返回）。
        /// 我们改成「出声 + 不做」，理由：MonoBehaviour 里的音乐不该因为一个 null 把整个壳炸掉。</summary>
        static void Fail(string what)
        {
            Debug.LogError("[AudioSourcesController] `" + what + "` 的 `AudioSource` 是 null —— 原版这里是空引用桩"
                         + "（`AudioSourcesController__" + what + ".c` 末尾 `FUN_1803f47a0`）⇒ 本次**什么也没做**");
        }
    }
}
