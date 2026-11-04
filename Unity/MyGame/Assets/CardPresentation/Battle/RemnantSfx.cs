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
        //  `AudioCue` 字段里抽出来；🆕 2026-11-03 `B123-C4`（`B121-AUD-01`）起**唯一来源 = cue 表**
        //  `Resources/remnant_sounds.json`（`WFSoundBank` 第三张表，键 = cue 资产名）——
        //  旧形状 `Resources/Art/audio/sfx/remnant_cue_props.json` **不再被读**（仍是生成器产物；退役要连生成器一起改）。
        //  ⚠️ **一个 cue 一条区间** —— 灵族的「出现」与「收集」共用同一个 clip，但原版是两条 cue（区间可以不一样）。
        //  ⚠️ cue 表取不到**不算错** —— 那时按 1.0/1.0 播，并**只提示一次**（不是每条都刷屏）。
        // ==================================================================

        [System.Serializable]
        public class CueProps
        {
            public string key;
            public float minPitch = 1f, maxPitch = 1f;
            public float minVolume = 1f, maxVolume = 1f;
            /// <summary>重播冷却（秒）。🔴 2026-11-02 `B120-W5b`：默认 `0` → **`0.1`**，对齐原版 `AudioCue` 的 ctor 默认
            /// （`AudioCue__ctor.c`：`*(float *)(this + 0x30) = 0x3dcccccd` = 0.1）。
            /// **行为影响 = 0**：cue 表 `remnant_sounds.json` 现读 **6/6 都显式写了 0.1**（`JsonUtility` 总会盖上来）。</summary>
            public float timeToPlayAgain = 0.1f;
        }

        static CueProps[] _props;
        static bool _propsTried;

        /// <summary>自检用：随机区间表读到了几**档**。🆕 2026-11-03 `B123-C4`（`B121-AUD-01`）：来源 = cue 表
        /// （`Resources/remnant_sounds.json`，`WFSoundBank` 第三张表）—— 旧 `remnant_cue_props.json` **不再被读**
        /// （文件仍由生成器产出；退役要连 `工具/import_remnant_sfx.py` 一起改，不在本批）。`_props` 只是它的**派生视图**，
        /// 只服务本读口与 `Editor/BattleScene.cs` 那条「区间 = 0.8~1.24 / 音量 0.3」的断言。</summary>
        public static int PropsLoaded { get { LoadProps(); return _props != null ? _props.Length : 0; } }

        static void LoadProps()
        {
            if (_propsTried) return;
            _propsTried = true;
            LoadCues();                                            // 🆕 2026-11-03 `B123-C4`：唯一来源 = cue 表（旧 `remnant_cue_props.json` 不再被读）
            if (_cues == null) return;
            var list = new System.Collections.Generic.List<CueProps>(6);
            for (int i = 0; i < 6; i++)
            {
                var c = _cues[i]; if (c == null) continue;       // 缺这一条 cue ⇒ 这一档没有区间（`Play` 按 1.0 播，`LoadCues` 已出声）
                list.Add(new CueProps { key = (i % 2 == 0 ? "Aeldari/" : "Necrons/") + MomentKey((Moment)(i / 2)), minPitch = c.minPitch, maxPitch = c.maxPitch, minVolume = c.minVolume, maxVolume = c.maxVolume, timeToPlayAgain = c.timeToPlayAgain });
            }
            _props = list.Count > 0 ? list.ToArray() : null;
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
        /// `NecronsCardReanimate` / `RemnantsDestroyed` 全部不在）⇒ **当时拿不到 `WFSoundCue` 实例**（已由第三张表补上，见下一段）。</para>
        /// <para>本 build 里 6 条 cue ↔ `(阵营, 时机)` **一一对应**（派生的区间视图 `_props` 就按这个分行）
        /// ⇒ 用这个**值类型**当键：零字符串、零分配、不会因为改名/拼错而静默分叉（改前是字符串键）。</para>
        /// <para>🆕 2026-11-03（`B121-WA` · `B120-F21`）：6 条 cue 已由 `B120-W17` 收进**第三张表**
        /// `Resources/remnant_sounds.json`（键 = cue 资产名）⇒ 冷却键 = 原版 cue 实例，⚠️ 2026-11-05（`B144-B2` · `B131` C6）那张表已并进 `WFSoundPlayer` 的**共享表**（本类自己的 `_lastAtCue` 已删），
        /// 这个下标只在**表缺失**时兜底。⚠️ 若将来两条 cue 共用同一个 `(阵营, 时机)`，只有兜底那一档会分叉。</para></summary>
        static int CueIndex(Moment m, bool aeldari) { return (int)m * 2 + (aeldari ? 0 : 1); }

        /// <summary>各档上一次播的时刻 —— **兜底表**（键 = `(阵营, 时机)` 下标，见 `CueIndex`）：只在 `Resources/remnant_sounds.json` 缺失、
        /// 拿不到原版 cue 实例时才用。🆕 2026-11-05（`B144-B2` · `B131` C6）：正常路径走 `WFSoundPlayer` 的**共享表**（`CanPlayByCooldown(cue)`），本类不再自持 cue 表。</summary>
        static readonly System.Collections.Generic.Dictionary<int, float> _lastAt =
            new System.Collections.Generic.Dictionary<int, float>();
        /// <summary>自检用：因为 `timeToPlayAgain` 被挡掉了几次。</summary>
        public static int Throttled { get; private set; }
        /// <summary>自检用：把节流记录清掉（每段之前调）。</summary>
        public static void ResetThrottle() { _lastAt.Clear(); WarpforgeVFX.WFSoundPlayer.ResetThrottle(); Throttled = 0; }   // ✅ 2026-11-05（B144-B2 · B131 C6）：cue 那一档已并进 `WFSoundPlayer` 的共享表 ⇒ 清它那本就等于清 cue 那一档（`_lastAtCue` 已删）


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
            var cue = CueOf(m, aeldari);            // 🆕 2026-11-03（`B121-WA` · `B120-F21`）：冷却**键**改成原版 cue 资产实例（判据 = `AudioCue.lastTimePlayed` 住在 cue 上，偏移 `+0x34`）；cue 表缺失才退回旧下标键 `_lastAt`。🆕 2026-11-03 `B123-C4`（`B121-AUD-01`）：随机区间也**只读它**（唯一真相源 = `Resources/remnant_sounds.json`；旧 `remnant_cue_props.json` 已不再被读）
            // 🆕 2026-11-02（`B117-WCOOL3`）：冷却公式收口到 `WFSoundPlayer.CanPlayByCooldown`（全项目唯一实现，铁律 6）
            //   判据 = `AudioCue__ConfigAudioSource.c:19-21` 的闸 + `:31` 的「先写戳、再挑 clip」次序，两半都在那一处；
            //   `last != 0` 那半句也在那里（0 = 从没播过，漏掉它会把 `Time.time` 还小时的第一次也拦掉）。
            //   ⚠️ cue 表取不到 / `timeToPlayAgain == 0` ⇒ 传 0 ⇒ 天然永远放行（与原版一致）。
            float ttpa = cue != null ? cue.timeToPlayAgain : 0f;
            if (!(cue != null ? WarpforgeVFX.WFSoundPlayer.CanPlayByCooldown(cue) : WarpforgeVFX.WFSoundPlayer.CanPlayByCooldown(_lastAt, CueIndex(m, aeldari), ttpa)))   // ✅ 已复刻（2026-11-05 · B144-B2 · `B131` C6：**冷却表合并成一张** —— 原版 `lastTimePlayed` 只住在 cue 资产上、全项目一张表；cue 那一支改走 `WFSoundPlayer` 的**共享表**，⛔ 别再用本类自己那本（`_lastAtCue` 已删）。⚠️ `_lastAt`（下标键）是「cue 表缺失」的兜底档，不参与合并
            {
                Throttled++;
                return false;
            }

            // ⚠️ 用 `UnityEngine.Random`（**表现层**的随机，不进引擎状态）—— 引擎那边那条
            //    「只用 `System.Random(seed)`」管的是**对局可复现**，音高不参与。
            //    🔴 2026-11-03 `B123-C4`：音高/音量**只从 cue 取**（`_props` 是它的派生视图，只给自检读口）。
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

        // ==================================================================
        //  🆕 2026-11-03（`B121-WA` · `B120-F21`）：冷却键 = **原版 cue 资产实例**
        //  —— 追加在**类尾**（⛔ 别往中间插：本文件被 `账/音频_语音.tsv:157/159` 按 `文件:行号` 引用）
        // ==================================================================
        //  判据：原版 `lastTimePlayed` 就住在 `AudioCue` 上（偏移 `+0x34`，`AudioCue__OnEnable.c` 写回 0），
        //  闸 = `AudioCue__ConfigAudioSource.c:19-21` ⇒ **冷却的键是 cue 资产实例**，不是「第几档」。
        //  2026-11-02 `B120-W17`（`F15`）已把这 6 条 cue 收进 `Resources/remnant_sounds.json`
        //  （`WFSoundBank` 第三张表，键 = cue 资产名）⇒ 现在拿得到真实例，本批把键切过去。
        //  ⚠️ 旧 `(阵营, 时机)` 下标表（`_lastAt`）**保留为兜底**：那份 json 缺失时仍要能节流。
        //  ✅ 2026-11-03 `B123-C4`（`B121-AUD-01`）：旧 `remnant_cue_props.json` 已**不再被读** —— `PropsOf`/`PropsLoaded` 改成
        //     cue 表的派生视图，`B120_W17_Assert` A3 与 `B117_WCOOL3_Assert` A 组同批切到 cue 表（旧文件仍由生成器产出）。
        // 🔴 2026-11-05（`B144-B2` · `B131` C6）：本类原来那张 `_lastAtCue` 已【删除】—— 冷却表合并成一张（原版形状：`lastTimePlayed` 只住在 cue 资产上、全项目一处）⇒ cue 那一支直接调 `WFSoundPlayer.CanPlayByCooldown(cue)`。
        //    与 `BattleSfx` / `WindowSounds` 共用同一本表（同名 cue 经不同路径播时**共享**同一个 0.1 s 窗口），⛔ 别建回来。

        /// <summary>这一档对应的原版 **cue 资产名**（⚠️ 与 clip 名**不同**：灵族「收集」cue = `Aeldari Waystone Collect`、
        /// clip = `Aeldari To Waystone Death`；死灵「收集」cue = `NecronsCardReanimate`、clip = `CardReanimate`）。
        /// 判据 = `RemnantBody.cs:17,26` / `RemnantAeldari.cs` / `RemnantNecrons.cs` 四格序列化的 `AudioCue` 字段，
        /// 六个名字逐字取自 `Resources/remnant_sounds.json`（生成器 `工具/import_remnant_sfx.py`）。</summary>
        public static string CueNameOf(Moment m, bool aeldari)
        {
            switch (m)
            {
                case Moment.Collect: return aeldari ? "Aeldari Waystone Collect" : "NecronsCardReanimate";
                case Moment.Death:   return aeldari ? "Aeldari Waystone Destruction" : "RemnantsDestroyed";
                default:             return aeldari ? "Aeldari To Waystone" : "CardShatter";
            }
        }

        static WarpforgeVFX.WFSoundCue[] _cues;
        static bool _cuesTried;
        /// <summary>自检用：因为 cue 表缺失、退回下标键的档数（6 档里几条没解析到）。</summary>
        public static int CueMisses { get; private set; }

        /// <summary>这一档的原版 cue 实例（`WFSoundBank` 三张表里的第三张）。取不到 = null ⇒ 调用方退回下标键。</summary>
        static WarpforgeVFX.WFSoundCue CueOf(Moment m, bool aeldari)
        {
            LoadCues();
            int i = CueIndex(m, aeldari);
            return _cues != null && i < _cues.Length ? _cues[i] : null;
        }

        static void LoadCues()
        {
            if (_cuesTried) return;
            _cuesTried = true;
            _cues = new WarpforgeVFX.WFSoundCue[6];
            int miss = 0;
            for (int mi = 0; mi < 3; mi++)
            {
                for (int a = 0; a < 2; a++)
                {
                    string name = CueNameOf((Moment)mi, a == 0);
                    WarpforgeVFX.WFSoundCue c;
                    // ⚠️ 先 `HasCue` 再 `TryGetCue`：`TryGetCue` 的 miss 会累加 `WFSoundBank.BadCues`（那是别人的读口，别被我们污染）。
                    if (WarpforgeVFX.WFSoundBank.HasCue(name) && WarpforgeVFX.WFSoundBank.TryGetCue(name, out c))
                        _cues[mi * 2 + a] = c;
                    else miss++;
                }
            }
            CueMisses = miss;
            if (miss > 0)
                Debug.LogWarning($"[Remnant] {miss}/6 条残骸 cue 不在音效表里（`Resources/{WarpforgeVFX.WFSoundBank.RemnantTablePath}.json`）"
                               + " ⇒ 冷却退回 `(阵营, 时机)` 下标键。跑一次 `python 工具/import_remnant_sfx.py --write`（⛔ 别手改生成物）");
        }

        /// <summary>自检用：这一档的原版 cue 名（表缺失也返回名字 —— 断言要能验「映射表本身」）。</summary>
        public static string DebugCueName(Moment m, bool aeldari) { return CueNameOf(m, aeldari); }
        /// <summary>自检用：这一档解析到的 cue 实例（取不到 = null）。</summary>
        public static WarpforgeVFX.WFSoundCue DebugCue(Moment m, bool aeldari) { return CueOf(m, aeldari); }
        /// <summary>自检用：6 档里解析到了几条 cue（表缺失 = 0）。</summary>
        public static int DebugCueResolved
        {
            get
            {
                LoadCues();
                if (_cues == null) return 0;
                int n = 0;
                foreach (var c in _cues) if (c != null) n++;
                return n;
            }
        }
    }
}
