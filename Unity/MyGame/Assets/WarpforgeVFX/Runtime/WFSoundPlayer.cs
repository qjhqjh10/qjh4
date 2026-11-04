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

        const int PoolSize = 20;   // F-5 判据 = `Assets/SoundManager.prefab` 的 `amountToPool: 20`（原版实读）；改前 24 是我们自选的数、无判据
        static AudioSource[] _pool;
        static GameObject _root;

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
                //   它名下 `AudioSource` 的**颗数 = 那一趟真租过几颗**（HEAD 5 颗 → 某趟 6 颗；`PoolSize` 封顶 —— 2026-11-02 `B120-W5` 起 = 20）。
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
            // 🔴 **2026-11-02（`B120-W5` · F-6）**：**照原版从下标 0 起扫**，取第一个「不活跃」的。
            //   判据 = `d:/2/tools/decomp_full/SoundManager__GetAudioSource.c`（`param_1+0x28` 那个 `List<AudioSource>`
            //   从 `i = 0` 遍历到 `+0x78` 的 Count，取第一个 `gameObject.activeInHierarchy == false` 的）；
            //   回收器 = `SoundManager__Update.c`（`isPlaying == false` ⇒ 把那一颗的 `gameObject.SetActive(false)`）。
            //   ⇒ 我们用 `AudioSource.isPlaying` 当「活跃」判据（与原版「那颗 GameObject 还挂着」是同一个状态的两面）。
            //   改前是「轮转挑一颗」（`_next`），既不是原版、也没有判据。
            for (int i = 0; i < PoolSize; i++)
            {
                if (_pool[i] == null)
                {
                    var src = _root.AddComponent<AudioSource>();
                    src.playOnAwake = false;
                    // ---------------------------------------------------------------
                    //  F-2 · 3D 衰减模板（判据 = `Assets/Audio Source Template.prefab`
                    //  的 `!u!82 &82724804207875695` 本轮现读）：MinDistance 15.61 · MaxDistance 500 ·
                    //  rolloffMode 0(Logarithmic) · Priority 128 · DopplerLevel 1 · Spatialize 0。
                    //  ⚠️ 改前 `AddComponent<AudioSource>()` 之后**一个衰减参数都没设** ⇒ Unity 默认 `minDistance = 1`
                    //  ⇒ 889 条 `is2d = 0` 的 3D 音，**满音量的半径差了 15.6 倍**（原版 15.61 世界单位内满音量）。
                    //  ⚠️ 这里是**唯一**的建源点 ⇒ 两条 `Play` 重载（`AudioClip` 那条与 cue 那条）都吃这五项。
                    // ---------------------------------------------------------------
                    src.minDistance = SrcMinDistance;
                    src.maxDistance = SrcMaxDistance;
                    src.rolloffMode = SrcRolloff;
                    src.priority = SrcPriority;
                    src.dopplerLevel = SrcDopplerLevel;
                    src.spatialize = false;
                    _pool[i] = src;
                }
                if (!_pool[i].isPlaying && DebugForceFullRents <= 0) return _pool[i];   // 🆕 2026-11-03 `B123-C4`：自检注入「池满」时必须扫完再落 `OnPoolExhausted`（改前注入被 i=0 的提前 return 吃掉 ⇒ `B120_W5` C1 / `B121_WA` B2 实际没验到池满那一支）
            }
            // 原版：全活跃 ⇒ `CustomDebug.LogError("There are not enough audio sources!")` 并 `return 0`
            // ⇒ `SoundManager.Play` 把默认值往外交（**这一声不播**）。改前我们是「轮转盖掉最早那条」。
            return OnPoolExhausted();
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
            if (src == null) return;        // F-6：池满 ⇒ 原版 `GetAudioSource` 回 null ⇒ **这一声不播**（`OnPoolExhausted` 已出声）
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
            LastCueThrottled = false;
            if (cue == null) return null;
            // 原版 `AudioCue.ConfigAudioSource.c:19-21` 那道冷却：**先写戳、再挑 clip**（`:31` 的次序）。
            // ⚠️ 排在 `!Enabled` **之外** —— 批处理自检关掉出声时仍然照旧冷却（与 `BattleSfx` 原写法一致）。
            if (!CanPlayByCooldown(cue)) { Throttled++; LastCueThrottled = true; return null; }
            return PlayPrepared(cue, worldPos, is2d, null);
        }

        /// <summary>🆕 **2026-11-02（`B120-W5b`）· 给「已经自己判过冷却」的调用方用的播放口** ——
        /// 当前唯一调用方 = `Shell/WindowSounds`（它要自己做 `Played`/`Throttled` 记账，所以先判一次）。
        /// <para>**判据**：原版 `SoundManager.Play` 里 `AudioCue.ConfigAudioSource`（= `timeToPlayAgain` 那道闸）
        /// **只判一次**（`SoundManager__Play.c` → `ConfigureAudioSourceAndPlay` → `AudioCue__ConfigAudioSource.c:19-21`）。
        /// 改前 `WindowSounds` 先按 cue 名挡一次、再进 `Play` 被按 cue 实例挡第二次 ⇒ **多一层**，
        /// 造成「`Played++` 了却没响」（同一条 cue 被别的调用方先播过时）。⇒ 它判完直接走这里。</para>
        /// <para>⚠️ `mixerGroup` 只服务壳侧那两条 **Jingles**（原版 `MixerType.Jingles = 3`，
        /// `SoundManager__ConfigureAudioSourceAndPlay.c` 的 `param_7 == 3` 取 `+0x60`）；**传 null = `FxGroup`**。
        /// ⛔ 别拿它给特效那 900+ 条开「可以接错组」的口子 —— 主入口 `Play(...)` 仍写死 `FxGroup`
        /// （`WarpforgeAudio.PlayJingle` 那段注释里的「别加带组参数的重载」指的就是主入口）。</para>
        /// <para>⛔ **本方法不判冷却** —— 传进来的调用方必须已经判过（否则同一条 cue 会被连播）。</para></summary>
        public static AudioClip PlayWithoutCooldown(WFSoundCue cue, Vector3 worldPos, bool is2d,
                                                    AudioMixerGroup mixerGroup = null)
        {
            Played++;
            LastCueThrottled = false;
            if (cue == null) return null;
            return PlayPrepared(cue, worldPos, is2d, mixerGroup);
        }

        /// <summary>两个入口共用的「挑 clip → 租源 → 配置 → 播」（冷却与 `Enabled` 由调用方各自判）。</summary>
        static AudioClip PlayPrepared(WFSoundCue cue, Vector3 worldPos, bool is2d, AudioMixerGroup mixerGroup)
        {
            if (!Enabled) return null;

            var clip = WFSoundBank.PickClip(cue, Rng);
            if (clip == null) return null;                  // 已经警告过，这里不再刷屏

            float pitch = Mathf.Lerp(cue.minPitch, cue.maxPitch, (float)Rng.NextDouble());
            float vol = Mathf.Lerp(cue.minVolume, cue.maxVolume, (float)Rng.NextDouble());

            var src = Rent();
            if (src == null) return null;   // F-6：池满 ⇒ 原版 `GetAudioSource` 回 null ⇒ **这一声不播**（`OnPoolExhausted` 已出声）
            src.transform.position = is2d ? Vector3.zero : worldPos;
            src.spatialBlend = is2d ? 0f : 1f;
            src.outputAudioMixerGroup = mixerGroup != null ? mixerGroup : FxGroup;
            src.pitch = pitch <= 0f ? 1f : pitch;
            // ⚠️ `PlayOneShot` 而不是 `Play`：同一帧里同一句可能连发（`loops` 多的条目；带 `timeToPlayAgain` 的 cue 已被上面那道闸拦住），
            //    而且**池满时原版丢新声**（不改写已在响的那条）。音高/音量是**发起那一刻**取值的，各条互不影响。
            //    （2026-11-02 `B120-W5`：改前这句写「池子轮转会**把上一条掐掉**」—— 轮转已按原版删除，见 `Rent`。）
            src.PlayOneShot(clip, vol);
            return clip;
        }

        /// <summary>自检用：把计数清零（池子留着）。⚠️ 2026-11-02 `B120-W5`：`Dropped`（池满丢弃次数）也是「计数」⇒ 一并清。</summary>
        public static void ResetCounter() { Played = 0; Dropped = 0; }

        // ============================================================================================
        //  `timeToPlayAgain` 冷却 —— **全项目唯一的一处实现**（2026-11-02 `B117-WSOUNDCOOL` 收口）
        // ============================================================================================
        //  判据（第一权威，逐行读过）：
        //    · `D:/2/tools/decomp_full/AudioCue__ConfigAudioSource.c:19-21` ——
        //        `if ((Time.time - last < timeToPlayAgain) && (last != 0)) return 0;`
        //      `:31` —— **先把 `last = Time.time` 写下去，然后才去挑 clip**（挑不出 clip 也算「这次播过」）。
        //    · `D:/2/tools/decomp_full/AudioCue__CanPlay.c` —— 同一道式子的独立等价体（`CanPlay` 只判不播）。
        //    · 调用点：`SoundManager__Play`（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/SoundManager.cs:257`
        //      `if (!((AudioCue)position).ConfigAudioSource(ref audioSource2))`）—— 原版**所有按 cue 播**的路径
        //      （`Play2D` / `Play3D` / `PlaySoundOnTime.Update` / `Play2DDelayed` …）都从这一个口过
        //      ⇒ **一处实现就覆盖全部**，⛔ 调用方别各自再判一遍（2026-11-02 之前 `BattleSfx` 按**字段名**、
        //      `TapHitMaterialDefinition` 按 cue 各写了一份，已删）。
        //    · 键 = **cue 资产实例**（`lastTimePlayed` 就住在 `AudioCue` 上，偏移 `+0x34`；`AudioCue__OnEnable.c`
        //      把它写回 0）。我们这里用 `Dictionary<WFSoundCue, float>` 按**引用身份**计 —— `WFSoundBank` 对同名
        //      cue 只给**同一个**实例 ⇒ 「同名 = 同一条冷却」，与 `ScriptableObject` 的共享语义一致。
        //  ⚠️ 原版那个 `&& (last != 0)` **不是**冗余：0 = 从没播过（`OnEnable` 把上次时刻清成 0），
        //     没有它就会「第一次也被拦」。`timeToPlayAgain == 0` 的行天然永远放行（差值不会 < 0）。
        static readonly System.Collections.Generic.Dictionary<WFSoundCue, float> _lastAt =
            new System.Collections.Generic.Dictionary<WFSoundCue, float>();

        /// <summary>自检用：因为 `timeToPlayAgain` 被挡掉几次（**不是错** —— 原版就这么节流）。</summary>
        public static int Throttled { get; private set; }

        /// <summary>自检/诊断：**最近一次**按 cue 的 `Play` 是否被冷却挡掉。
        /// `BattleSfx.DrainOne` 用它把自己那一层的 `Throttled` 记起来（⛔ 别拿它在别处当「能不能播」的门 ——
        /// 门只有 <see cref="CanPlayByCooldown"/> 一处）。</summary>
        public static bool LastCueThrottled { get; private set; }

        /// <summary>原版 `AudioCue__CanPlay.c` 逐行等价：还在窗口内**且之前真的播过** ⇒ 不许播（不写戳）；
        /// 否则**先写戳**再放行（原版 `ConfigAudioSource.c:31` 的次序 —— 后面挑不出 clip 也算播过）。</summary>
        public static bool CanPlayByCooldown(WFSoundCue cue)   // ✅ 2026-11-05（B144-B2 · B131 C6）：`internal` → **`public`** —— 这是**全项目唯一那张冷却表**（原版 `lastTimePlayed` 只住在 cue 资产上）；`WindowSounds` / `RemnantSfx` / `BattleSfx` 全部转调它，Editor 自检也要能直接调它验「同名 cue 跨路径共享同一个窗口」
        {
            return CanPlayByCooldown(_lastAt, cue, cue.timeToPlayAgain);
        }

        /// <summary>🆕 **2026-11-02（`B117-WCOOL3`）**：上面那条式子的**唯一实现** —— 所有按 `timeToPlayAgain`
        /// 节流的路径（FX cue · Jingles cue · `RemnantSfx` · `WindowSounds`）都转调这里，⛔ 别各自再写一遍（铁律 6）。
        /// <para>判据 = `d:/2/tools/decomp_full/AudioCue__ConfigAudioSource.c:19-21`
        /// （`if ((Time.time - last &lt; timeToPlayAgain) &amp;&amp; (last != 0)) return 0;`）+ `:31` 的「先写戳、再挑 clip」。</para>
        /// <para>⚠️ `last != 0` **不是冗余**：0 = 从没播过（`AudioCue__OnEnable.c` 把上次时刻清成 0），
        /// 漏掉它就会在 `Time.time` 还很小时把**第一次**也拦掉。`timeToPlayAgain == 0` ⇒ 差值不会 &lt; 0 ⇒ 永远放行。</para>
        /// <para>⚠️ 时钟 = **`Time.time`**（原版口径；`WindowSounds` 原来用 `Time.realtimeSinceStartup`，已随本条改过来）；⚠️ 批处理里 `Time.time` 恒 0 ⇒ 由 <see cref="CooldownClock"/> 兜底（见类尾 `B117-FIXA`）。</para></summary>
        internal static bool CanPlayByCooldown<TKey>(System.Collections.Generic.Dictionary<TKey, float> table,
                                                     TKey key, float timeToPlayAgain)
        {
            float now = CooldownClock;
            float last;
            if (table.TryGetValue(key, out last) && last != 0f && now - last < timeToPlayAgain)
                return false;
            table[key] = now;
            return true;
        }

        /// <summary>自检用：把冷却表与计数清零（= 原版 `AudioCue.OnEnable` 把 `lastTimePlayed` 写回 0 的同义口）。
        /// ⚠️ `BattleSfx.ResetCounters()` 会调它 —— 那份契约本来就写着「清节流表」。</summary>
        public static void ResetThrottle()
        {
            _lastAt.Clear();
            Throttled = 0;
            LastCueThrottled = false;
        }

        // ============================================================================================
        //  冷却时钟（2026-11-02 `B117-FIXA`）—— 批处理里 `Time.time` 恒 0 的兜底 + 自检注入口
        // ============================================================================================
        //  现场：`_tmp_view/shop.log` / `battle.log` 里 `Time.time` = **0.000**（`-executeMethod` 批处理没有帧循环）。
        //  后果：原版 `AudioCue__ConfigAudioSource.c:19-21` 的 `&& (last != 0)` 是「从没播过」的哨兵，
        //  而 `last` 恰好在 0 时刻被写成 0 ⇒ **每一次播放都被读成「从没播过」** ⇒ 冷却一秒都挡不住、
        //  `Throttled` 永远是 0。这不是实现错（真实播放环境 `Time.time` 从第一帧起就 > 0），
        //  是**冷却的时钟口径不适配批处理**。两条配套：① 只在 `Time.time <= 0` 时兜底（生产语义不变）
        //  ② 给自检一个显式注入口，让「窗口内 / 窗口外」两态能精确测（不再靠把 `timeToPlayAgain` 压到 0）。
        // ============================================================================================

        /// <summary>自检用：把冷却时钟**钉成给定时刻**（&lt; 0 = 用真实时钟）。断言给出 `1000 / 1000.05 / 1200`
        /// 这样的**显式时刻序列**，就能确定性地测「窗口内被挡 / 窗口外放行」。
        /// ⚠️ 生产路径**从不**给它赋值 ⇒ 恒 -1（用真实时钟）。</summary>
        public static float ClockOverride = -1f;

        /// <summary>冷却用的「当前时刻」。原版 = `Time.time`（`AudioCue__ConfigAudioSource.c:19`）；
        /// 但**批处理 / 编辑模式没有帧循环，`Time.time` 恒 0.000** ⇒ 照抄原版会让 `last != 0` 哨兵恒失效。
        /// 所以只在 `Time.time &lt;= 0` 时回退到 `Time.realtimeSinceStartup`（同一量纲：启动以来的秒数，
        /// 批处理里真的在走 —— 那也是 `WindowSounds` 改回 `Time.time` 之前一直在用的时钟）。
        /// <para>🔶 自己设计（非原版）：这条回退 · 为什么必须自定 = 判据 `AudioCue__ConfigAudioSource.c:19` 写的是
        /// `Time.time`，而批处理里它恒 0、冷却行为无从验证；回退只服务「无帧循环」的宿主，**真实播放环境
        /// `Time.time` 从第一帧起就 &gt; 0 ⇒ 这条回退永远不走**（行为与原版逐位相同）· 调度台 2026-11-02 裁定
        /// （`B117-FIXA`；现场 = `_tmp_view/{shop,battle}.log` 的 `Time.time` = 0.000）。</para></summary>
        public static float CooldownClock
        {
            get
            {
                if (ClockOverride >= 0f) return ClockOverride;
                float t = Time.time;
                return t > 0f ? t : Time.realtimeSinceStartup;
            }
        }

        // ============================================================================================
        //  FX 音源池的三件原版事实（2026-11-02 `B120-W5` · F-2 / F-5 / F-6）—— 判据全是**原版 Asset 现读**
        // ============================================================================================
        //   · **容量 20** = `d:/2/tools/assetripper_out/ExportedProject/Assets/SoundManager.prefab`
        //       `amountToPool: 20`（现读）。⚠️ `assets_full` 里只有 `GameObject/SoundManager.json`
        //       （不带组件字段）⇒ 按名字在 `assets_full` 里搜不到这个数，判据只能走 AssetRipper 导出的原版工程。
        //   · **3D 模板** = `.../Assets/Audio Source Template.prefab` 的 `!u!82 &82724804207875695`（现读）：
        //       `MinDistance: 15.61` · `MaxDistance: 500` · `rolloffMode: 0`(Logarithmic) ·
        //       `Priority: 128` · `DopplerLevel: 1` · `Spatialize: 0` · `Volume/Pitch: 1` · `Loop: 0` · `PlayOnAwake: 0`。
        //   · **耗尽策略** = `d:/2/tools/decomp_full/SoundManager__GetAudioSource.c`：
        //       池（`+0x28` 的 `List<AudioSource>`，Count 在 `+0x78`）从**下标 0** 起扫，取第一个
        //       `gameObject.activeInHierarchy == false` 的；**全活跃 ⇒ `CustomDebug.LogError("There are not enough
        //       audio sources!")` 并 `return 0`** ⇒ `SoundManager.Play` 把默认值往外交（**这一声不播**）。
        //       回收器 = `SoundManager__Update.c`（`isPlaying == false` ⇒ 把那一颗的 `gameObject.SetActive(false)`）。
        // ============================================================================================

        /// <summary>池容量（自检读口）。判据 = 原版 `SoundManager.prefab` 的 `amountToPool: 20`。</summary>
        public static int PoolCapacity { get { return PoolSize; } }

        /// <summary>3D 衰减模板 · `MinDistance`（判据 = 原版 `Audio Source Template.prefab`，逐字段现读）。</summary>
        public const float SrcMinDistance = 15.61f;
        /// <summary>3D 衰减模板 · `MaxDistance`。</summary>
        public const float SrcMaxDistance = 500f;
        /// <summary>3D 衰减模板 · `rolloffMode: 0` = Logarithmic。</summary>
        public const AudioRolloffMode SrcRolloff = AudioRolloffMode.Logarithmic;
        /// <summary>3D 衰减模板 · `Priority`。</summary>
        public const int SrcPriority = 128;
        /// <summary>3D 衰减模板 · `DopplerLevel`。</summary>
        public const float SrcDopplerLevel = 1f;

        /// <summary>自检用：因为**池满**而「这一声没播」的次数（原版同一路径 = `GetAudioSource` 回 0）。
        /// ⚠️ 与 <see cref="Played"/>（请求次数）分开记 —— 池满时 `Played` 照样涨。</summary>
        public static int Dropped { get; private set; }

        /// <summary>🔶 自检注入口（非原版）：把「池已满」强制成给定次数，让 F-6 那条策略能被**确定性**量到。
        /// 为什么必须自定 = 批处理里没法真让 20 条 `AudioSource.isPlaying` 同时为真（没有帧循环）；
        /// 每调一次 `Rent()` 消费一次，**生产路径恒 0**（那一段代码永不进入）。先例 = 本类 `ClockOverride`。
        /// 调度台 2026-11-02 裁定（`B120-W5`）。</summary>
        public static int DebugForceFullRents;

        /// <summary>自检用：直接租一颗源（走 `Rent()` 的**生产分支**，含建池与五项衰减参数）。
        /// ⚠️ 只给自检读「源上的参数对不对」；⛔ 别在玩法代码里用（它绕过 `Play` 的音量/混音组设置）。</summary>
        public static AudioSource DebugRent() { return Rent(); }

        /// <summary>池满时原版的动作：`LogError` + **这一声不播**（`d:/2/tools/decomp_full/SoundManager__GetAudioSource.c` 末尾）。
        /// <para>🆕 2026-11-03（`B123-C4` · `B121-AUD-02`）：**删掉了** 2026-11-02 `B120-W5` 那条
        /// `🔶 自己设计（非原版 · 只服务无帧循环的宿主）` 的「批处理复用第 0 条」兜底 ——
        /// 现读确认**没有任何 `-executeMethod` 宿主会连播 > 20**（8 个自检宿主全部先
        /// `WFSoundPlayer.Enabled = false`，关掉时 `WarpforgeAudio.cs:154` 直接返 null ⇒ 一次 `Rent()` 都不发生；
        /// `RuleEngineTest` / `NetSelfTest` / `NetBattleTest` 音频 0 命中；`DebugRent()` 全仓真实调用只有
        /// `B120_W5_Assert` 3 处 + `B121_WA_Assert` 3 处 = 6 次 < 池 20）⇒ 回归原版：批处理也走同一条「报错 + 不播」。</para></summary>
        static AudioSource OnPoolExhausted()
        {
            if (DebugForceFullRents > 0)
            {
                // 自检注入：走**生产**那一路（回 null + 出声），与本机是不是批处理无关。
                DebugForceFullRents--;
                Dropped++;
                Debug.LogError("[WFSoundPlayer] There are not enough audio sources!"
                             + "（原版同一句；自检注入 `DebugForceFullRents`）—— 这一声**不播**");
                return null;
            }
            Dropped++;
            Debug.LogError("[WFSoundPlayer] There are not enough audio sources!"
                         + "（原版 `SoundManager.GetAudioSource` 同一句）—— 池 " + PoolSize + " 条全在响 ⇒ **这一声不播**");
            return null;
        }
    }
}
