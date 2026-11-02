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

        /// <summary>把**读口**清零（自检每段之前调）：`Played` / `LastClip` / `LastPitch` / `LastVolume`。⚠️ `LastMoment` **没有 null 档** ⇒ 读它之前先看 `Played`；节流表走 `ResetThrottle`（与 `B117-57` 同族：原来只清前两项）。</summary>
        public static void ResetCounters() { Played = 0; LastClip = null; LastPitch = 0f; LastVolume = 0f; }

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
            /// <summary>重播冷却（秒）。🔴 2026-11-02 `B120-W5b`：默认 `0` → **`0.1`**，对齐原版 `AudioCue` 的 ctor 默认
            /// （`AudioCue__ctor.c`：`*(float *)(this + 0x30) = 0x3dcccccd` = 0.1）。
            /// **行为影响 = 0**：`remnant_cue_props.json` 现读 **6/6 都显式写了 0.1**（`JsonUtility` 总会盖上来）。</summary>
            public float timeToPlayAgain = 0.1f;
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

        /// <summary>六条 cue 在**我们的表**里的稳定身份。
        /// <para>原版键 = `AudioCue` **资产实例**（`lastTimePlayed` 住在 cue 上，偏移 `+0x34`；`AudioCue__OnEnable.c` 写回 0）。
        /// ⚠️ 但那 6 条 remnant cue **不在** `animfx_sounds.json` 里（2026-11-02 现读 513 条零命中：
        /// `Aeldari To Waystone` / `Aeldari Waystone Collect` / `Aeldari Waystone Destruction` / `CardShatter` /
        /// `NecronsCardReanimate` / `RemnantsDestroyed` 全部不在）⇒ **拿不到 `WFSoundCue` 实例**。</para>
        /// <para>本 build 里 6 条 cue ↔ `(阵营, 时机)` **一一对应**（`remnant_cue_props.json` 就按这个分行）
        /// ⇒ 用这个**值类型**当键：零字符串、零分配、不会因为改名/拼错而静默分叉（改前是字符串键）。</para>
        /// <para>⚠️ 仍然不是「cue 资产实例」：若将来两条 cue 共用同一个 `(阵营, 时机)`，这里会与 cue 键分叉。
        /// 真正对齐的做法 = 把这 6 条 cue 收进 `animfx_sounds.json`（`工具/import_remnant_sfx.py` 的活、⛔ 别手改生成物），
        /// 再把本表换成 `Dictionary&lt;WFSoundCue, float&gt;`。已记在 `报告/B120_W5_执行.md` §六。</para></summary>
        static int CueIndex(Moment m, bool aeldari) { return (int)m * 2 + (aeldari ? 0 : 1); }

        /// <summary>各档上一次播的时刻（`WFSoundPlayer.CooldownClock`，原版 = `Time.time`）——用来实现 `timeToPlayAgain`。
        /// 🔴 2026-11-02 `B120-W5b`：键从**字符串**（`"Aeldari/collect"`）改成**值类型身份**（见 `CueIndex` 的 doc）。</summary>
        static readonly System.Collections.Generic.Dictionary<int, float> _lastAt =
            new System.Collections.Generic.Dictionary<int, float>();
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
                // 🆕 2026-11-02（`B120-W5`）：与 `WFSoundPlayer` 的 `A1534` 同一条兜底 —— 这个宿主是**运行时对象**，
                //   别让自检收尾的 `SaveScene()` 把它烘进 `.unity`（`B117_WCOOL3_Assert` 已经在替它手工销毁，这里是根因侧的保险）。
                _host.hideFlags = HideFlags.DontSave;
                _src = _host.AddComponent<AudioSource>();
                ApplyTemplate(_src);
            }

            if (at.HasValue) { _src.spatialBlend = 1f; _src.transform.position = at.Value; }
            else             { _src.spatialBlend = 0f; }

            // 🆕 2026-09-29 原版 `AudioCue` 的三个随机量（判据见上面那一节）。
            // ⚠️ `timeToPlayAgain` 挡掉的那次**返回 false 但不是错**（原版就是这么节流的）——
            //    调用方**别把它当失败报警**；要看节流了几次有 `Throttled`。
            var cue = PropsOf(m, aeldari);
            // 🆕 2026-11-02（`B117-WCOOL3`）：冷却公式收口到 `WFSoundPlayer.CanPlayByCooldown`（全项目唯一实现，铁律 6）
            //   判据 = `AudioCue__ConfigAudioSource.c:19-21` 的闸 + `:31` 的「先写戳、再挑 clip」次序，两半都在那一处；
            //   `last != 0` 那半句也在那里（0 = 从没播过，漏掉它会把 `Time.time` 还小时的第一次也拦掉）。
            //   ⚠️ `cue` 取不到（区间表缺失）或 `timeToPlayAgain == 0` ⇒ 传 0 ⇒ 天然永远放行（与原版一致）。
            //   🔴 2026-11-02 `B120-W5b`：键从字符串改成 `CueIndex(m, aeldari)`（见那张表的 doc）。
            float ttpa = cue != null ? cue.timeToPlayAgain : 0f;
            if (!WarpforgeVFX.WFSoundPlayer.CanPlayByCooldown(_lastAt, CueIndex(m, aeldari), ttpa))
            {
                Throttled++;
                return false;
            }

            // ⚠️ 用 `UnityEngine.Random`（**表现层**的随机，不进引擎状态）—— 引擎那边那条
            //    「只用 `System.Random(seed)`」管的是**对局可复现**，音高不参与。
            float pitch = cue != null ? Random.Range(cue.minPitch, cue.maxPitch) : 1f;
            float vol   = cue != null ? Random.Range(cue.minVolume, cue.maxVolume) : 1f;
            _src.pitch = pitch;

            // 🔴 **2026-11-02（`B120-W5b` · 分报告主表 #26）**：原版残骸音走 **FX 混音组**，不是「代码里乘音量」。
            //   判据（逐跳现读）：
            //     · `RemnantBody__DoDestroyByAttack.c:35` / `RemnantAeldari__CollectWaystoneEffect.c:29` /
            //       `RemnantNecrons__Toggle.c:64` 都是 `AudioCue__Play3D(cue, pos, mixerType)`；
            //     · `AudioCue__Play3D.c` 把 `param_4` 原样传给 `SoundManager__Play`；
            //     · `SoundManager__Play3D.c` 的实参是 `param_5`（默认 `MixerType.FX = 0`）；
            //     · `SoundManager__ConfigureAudioSourceAndPlay.c` 的 `param_7 == 0` ⇒ `set_outputAudioMixerGroup(源, SoundManager + 0x48)`
            //       = **FX 组**（1/2/3 才分别是 Music/Voices/Jingles 那三个字段）。
            //     · 音量：`AudioCue__ConfigAudioSource.c` 只把 cue 的随机 `volume` 写进源
            //       （`AudioSource.set_volume(Random.Range(minVolume, maxVolume))`）—— **不乘滑块**，滑块在组上。
            //   ⚠️ 改前我们 `PlayOneShot(clip, WarpforgeAudio.SoundFx * vol)` 且**源不挂任何组**：
            //     净增益等价（同一个音效滑块、一次衰减），但**绕过了 FX 组** ⇒ 不吃 `ToggleSFX` 的 2 s duck、
            //     也不吃组上的其它处理。现在照原版：挂组 + 只传 cue 的随机音量。
            //   ⚠️ 组取不到（没有 mixer）时退回「代码里乘滑块」，⛔ 不静默变成满音量。
            float slider = WarpforgeAudio.SoundFx;                        // 顺带 Ensure()：`FxGroup` 由 RouteEffectSounds 填
            var fxGroup = WarpforgeVFX.WFSoundPlayer.FxGroup;
            _src.outputAudioMixerGroup = fxGroup;
            _src.PlayOneShot(clip, fxGroup != null ? vol : slider * vol);
            Played++;
            LastClip = clip.name;
            LastMoment = m;
            LastPitch = pitch;
            LastVolume = vol;
            return true;
        }

        // ==================================================================
        //  🆕 2026-11-02（`B120-W5` · F-3）：残骸音效的 AudioSource 参数
        // ==================================================================
        //  判据 = **原版同一个池、同一份模板**：
        //    · `Assembly-CSharp/RemnantBody.cs:17,26`（`toRemnantSound` / `deathSound`）、
        //      `RemnantAeldari.cs` 的 `waystoneCollectSound`、`RemnantNecrons.cs` 的 `reanimateSound`
        //      —— 四格都是 **`AudioCue`**，调用点 = `RemnantBody__DoDestroyByAttack.c:35` /
        //      `RemnantAeldari__CollectWaystoneEffect.c:29` / `RemnantNecrons__Toggle.c:64` 的
        //      **`AudioCue__Play3D`** ⇒ 原版走 `SoundManager` 的 FX 池 ⇒ **吃 `Audio Source Template` 那份参数**。
        //    · 模板 = `d:/2/tools/assetripper_out/ExportedProject/Assets/Audio Source Template.prefab`
        //      的 `!u!82 &82724804207875695`（2026-11-02 现读）：`MinDistance 15.61` · `MaxDistance 500` ·
        //      `rolloffMode 0`(Logarithmic) · `Priority 128` · `DopplerLevel 1` · `Spatialize 0` · `PlayOnAwake 0`。
        //  ⚠️ 改前我们是 **Linear / 1 / 40** —— 曲线、起点、终点**三处全不同**：
        //      `max 40` 会让远处残骸声**直接消失**（原版到 500 才衰减完）；`min 1` 让满音量半径小 15.6 倍。
        //  数值**不在这里重写一遍**（铁律 6：同一条规则只留一处）—— 直接吃 `WFSoundPlayer` 的那组常量。
        // ==================================================================
        static void ApplyTemplate(AudioSource src)
        {
            src.playOnAwake = false;
            src.minDistance = WarpforgeVFX.WFSoundPlayer.SrcMinDistance;
            src.maxDistance = WarpforgeVFX.WFSoundPlayer.SrcMaxDistance;
            src.rolloffMode = WarpforgeVFX.WFSoundPlayer.SrcRolloff;
            src.priority = WarpforgeVFX.WFSoundPlayer.SrcPriority;
            src.dopplerLevel = WarpforgeVFX.WFSoundPlayer.SrcDopplerLevel;
            src.spatialize = false;
        }

        /// <summary>自检用：把那个（懒建的）音源建出来并返回 —— 好让断言能读「它身上的衰减参数对不对」。
        /// ⚠️ 只给自检；它**不播**、不依赖 `Resources/Art/`（那里是 `.gitignore` 的，clip 可能不在）。
        /// 用完请 `GameObject.DestroyImmediate(GameObject.Find("RemnantSfx"))` 收尾（宿主是 `DontSave`，不会进场景）。</summary>
        public static AudioSource DebugEnsureSource()
        {
            if (_src == null)
            {
                if (_host == null) _host = new GameObject("RemnantSfx");
                _host.hideFlags = HideFlags.DontSave;
                _src = _host.AddComponent<AudioSource>();
                ApplyTemplate(_src);
            }
            return _src;
        }
    }
}
