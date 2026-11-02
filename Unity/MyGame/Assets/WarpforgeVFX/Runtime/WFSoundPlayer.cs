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
            LastCueThrottled = false;
            if (cue == null) return null;
            // 原版 `AudioCue.ConfigAudioSource.c:19-21` 那道冷却：**先写戳、再挑 clip**（`:31` 的次序）。
            // ⚠️ 排在 `!Enabled` **之外** —— 批处理自检关掉出声时仍然照旧冷却（与 `BattleSfx` 原写法一致）。
            if (!CanPlayByCooldown(cue)) { Throttled++; LastCueThrottled = true; return null; }
            if (!Enabled) return null;

            var clip = WFSoundBank.PickClip(cue, Rng);
            if (clip == null) return null;                  // 已经警告过，这里不再刷屏

            float pitch = Mathf.Lerp(cue.minPitch, cue.maxPitch, (float)Rng.NextDouble());
            float vol = Mathf.Lerp(cue.minVolume, cue.maxVolume, (float)Rng.NextDouble());

            var src = Rent();
            src.transform.position = is2d ? Vector3.zero : worldPos;
            src.spatialBlend = is2d ? 0f : 1f;
            src.outputAudioMixerGroup = FxGroup;
            src.pitch = pitch <= 0f ? 1f : pitch;
            // ⚠️ `PlayOneShot` 而不是 `Play`：同一帧里同一句可能连发（`loops` 多的条目；带 `timeToPlayAgain` 的 cue 已被上面那道闸拦住），
            //    而且池子轮转会**把上一条掐掉**。音高/音量是**发起那一刻**取值的，各条互不影响。
            src.PlayOneShot(clip, vol);
            return clip;
        }

        /// <summary>自检用：把计数清零（池子留着）。</summary>
        public static void ResetCounter() { Played = 0; }

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
        internal static bool CanPlayByCooldown(WFSoundCue cue)
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
    }
}
