// WFModuleScreenShake.cs — 原版 `AnimFXModuleScreenShake` 的对应物
//   **434 实例 / 416 效果**（覆盖面最大的模块）
//
// 原版语义（方法体读解见 `资料/AnimFX_18类方法体_块1.md` §3）：
//   · `OnEnable`  → `foreach (cameraShakes)`：preset 非空就播（**自动档**）
//   · `AnimEventDoShake(i)`     → 播 `manualTriggerCameraShakes[i]`；**越界静默 return**
//   · `TriggerCameraShake(i)`   → 播 `manualTriggerCameraShakes[i]`；**越界 LogError**
//     🔴 **2026-09-18 更正**：这里原写「播 `cameraShakes[i]`」—— **读错了数组**（代码也跟着错，
//     已一并改）。实据：`__TriggerCameraShake.c:15` 与 `__AnimEventDoShake.c:7` **都读 `+0x40`**
//     （= `manualTriggerCameraShakes`）；**只有 `OnEnable` 读 `+0x38`**（= `cameraShakes`）。
//     ⇒ 两个「手动」入口**用的是同一个数组**。**434 个实例全受影响。**
//   · `OverwriteCameraShakePreset.GetModifiedPreset()`：从 presetSO 取值，再用 6 个
//     `overwrite*` 开关**逐项覆盖**（开关=0 用 preset 的，=1 用覆盖值）
//
// ⚠️ **一处有意的偏离**：原版把自动档放在 `OnEnable`，我们放在 `Initialize`。
//    原因是运行时装配的顺序 —— `AddComponent` 会**立刻**触发 `OnEnable`，而字段是
//    `AddComponent` 之后才 `Configure` 填的 ⇒ 放在 `OnEnable` 会用空参数播。
//    `Initialize` 是两条路（数据装配 / 手工挂）**都会走**的入口，放这儿两边都对。
//
// 🔴 **2026-10-14 订正（A394 欠账收口 / W10，铁律 5）**：这里原来写的是
//    「`AnimEventDoShake(i)` 是给**动画片段的事件**调的（W 组结论：触发源是动画挂点）」——
//    **那条旁证是错的**（行为没错，错的是触发源说反了）：
//      · 实测 `assets_full` 全量 **99 个 `AnimationClip`**、其中 **13 个**带事件，
//        **没有一个事件叫 `AnimEventDoShake`**（那 13 个的事件函数名一共 10 种，全是
//        `AnimationEndEvent` / `CardHandToBoardAnimationFinished` 这类，**零个含 `Shake`**）；
//        连 `Railgun BIG` 那条 clip 自己都是 `m_Events = []`
//        ⇒ 「动画挂点」这条**零命中**（判据：W10 亲跑 python 扫 `*/AnimationClip/*.json`）。
//      · 真通道 = **`AnimFXModuleCollisions.collisionEvent`（一个 UnityEvent）** —— 在 **5 个卡侧特效
//        prefab** 上连着 `AnimFXModuleScreenShake.AnimEventDoShake(0)`。判据 = 全库 grep
//        `AnimEventDoShake` **只命中 5 份** JSON、全在
//        `d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/`：
//        `MonoBehaviour_-1804495645565317640.json` · `…_-3758791311628888106.json` ·
//        `…_-4233369725324903401.json` · `…_5244723248536337755.json` · `…_8973829403083807152.json`。
//        ⚠️ **那条 UnityEvent 不在顶层字段上**，位置 = `collisionAndParticles[i].particleSystemsDefinition[j]
//        .collisionEvent.m_PersistentCalls.m_Calls[k]`（本件亲读第 1 份）。5 份**全都是**
//        `m_MethodName = "AnimEventDoShake"` · `m_Mode = 3 (Int)` · `m_IntArgument = 0`；
//        目标那份（`m_Target`）逐份不同、第一份 = `MonoBehaviour_2003263133734407672.json`，
//        它的字段表就是 `actionStart` / `cameraShakes` / `manualTriggerCameraShakes`（1 条）
//        ⇒ **确实连到 `AnimFXModuleScreenShake` 上**（本件亲核）。
//      **错因**：照「方法名像动画事件」推的触发源，没去数 `AnimationClip` 的事件表。
//    ⚠️ ~~**结论没变**（手动档今天不会自己播），理由换成真的那个：那 5 份 prefab 的 UnityEvent
//       **在我们这条链上没有订阅者** —— `数据/游戏数据/animfx_modules.json` 里 `collisionEvent`
//       这个键**一次都没出现**（实测 0 处；`collisionAndParticles` 1797 处）⇒ 数据装配出来的都是
//       空事件（见 `WFModuleCollisions.cs` 文件头 B 段）。要接得先把那一层收进数据。~~
//    🔴 **2026-10-23 就地订正（铁律 5 · `资料/普查产出_1022/A1_自造与兜底_特效动画.md` §四 D1）**：
//       上面那两句**已经过期**（错因 = 写在那层数据收进来之前，之后没回来改）。
//       现读 `数据/游戏数据/animfx_modules.json`：`collisionEvent` **5,906 处** ·
//       `collisionAndParticles` **7,703 处** · `AnimEventDoShake` **5 处**（= 那 5 份 prefab 的订阅，
//       `m_Mode = 3 (Int)` · `m_IntArgument = 0`）· `PlayAnim` 92 处。
//       ⇒ **手动档的通道已经通了**：`WFModuleCollisions.BindCollisionEvent` → `MakeAction` 的
//       `case "AnimEventDoShake"` 会在撞到东西时调到本类的 `AnimEventDoShake(0)`。
//       ⚠️ 「通了」≠「今天一定看得到」：这 5 份都在**碰撞驱动**的卡侧特效上（`AnimFXModuleCollisions`），
//          隔离渲染里没有碰撞目标 ⇒ 表现在只有真打一局才看得到（`T11` 真 Play 那一格）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    [WFModuleKind("AnimFXModuleScreenShake")]
    public class WFModuleScreenShake : WFEffectModule
    {
        /// <summary>一条屏震配置（原版 `OverwriteCameraShakePreset`）。</summary>
        [Serializable]
        public class ShakeEntry
        {
            [Tooltip("presetSO 的名字，如 `Shake Hit Small`。空 = 这条不播（原版就是这样判的）")]
            public string preset = "";

            public float delay;

            public int overwriteSustainTime; public float sustainTime;
            public int overwriteAttackTime;  public float attackTime;
            public int overwriteDecayTime;   public float decayTime;
            public int overwriteAmplitude;   public float amplitude;
            public int overwriteFrequency;   public float frequency;
            public int overwriteDirection;   public Vector3 direction;
        }

        /// <summary>一个 octave（原版 `NoiseSettings.TransformNoiseParams`）：X/Y/Z 三轴各一组
        /// `{Frequency, Amplitude, Constant}`。**拍平成字段**是因为 `JsonUtility` 解析不了嵌套字典
        /// （踩过：`presets` 只能是数组）。</summary>
        [Serializable]
        public class SignalOctave
        {
            public float fx, ax; public int cx;
            public float fy, ay; public int cy;
            public float fz, az; public int cz;
        }

        /// <summary>一份 `rawSignal`（原版 `Cinemachine.NoiseSettings`）。
        /// 采样算式（**照抄，⛔ 别改**，判据 → `资料/普查产出_1023b/R9_rawSignal_算式.md`）：
        /// 每轴每带 `u = Frequency × T`；`Constant == 1` ⇒ `A×0.5×sin(2πu)`、
        /// `Constant == 0` ⇒ `A×(Mathf.PerlinNoise(u, 0) − 0.5)`；**三条带相加**；
        /// `T = t × preset.frequency + 起播偏移`。
        /// ⚠️ `Constant` **不是**「是不是常数」—— 1 = 正弦、0 = Perlin（反直觉，机器码坐实）。</summary>
        [Serializable]
        public class SignalRec
        {
            public string name;
            public SignalOctave[] position;
            public SignalOctave[] orientation;
        }

        /// <summary>解析完的一次屏震（preset 值 + 覆盖已应用）。下游直接拿它播。</summary>
        [Serializable]
        public struct Request
        {
            public string preset;
            public float delay, sustainTime, attackTime, decayTime, amplitude, frequency;
            public Vector3 direction;
            /// <summary>🆕 2026-10-23（R9）：原版这个 preset 实际会播的**信号表**（已解好默认值）。
            /// `null` = 没有数据（退回下游的出厂表）。⚠️ 只带 `PositionNoise` 的 Y 轴是**必有的**，
            /// X/Z 与 `Orientation` 收着但 2D 版不用（见 `CardFeel.ShakeSignal`）。</summary>
            public SignalRec signal;
        }

        /// <summary>🔑 **下游钩子**：表现层挂这里。我们这边接 `CardFeel.ShakeCamera`
        /// （相机 + HudRoot 同向平移）。VFX 层不认识相机，所以用回调而不是直接调。</summary>
        public static Action<Request> OnShake;
        /// <summary>没有下游时的自检计数 —— 静默不播是事故，看得见才叫「不静默失败」。</summary>
        public static int DroppedRequests;

        public ShakeEntry[] cameraShakes = new ShakeEntry[0];
        public ShakeEntry[] manualTriggerCameraShakes = new ShakeEntry[0];

        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);
            cameraShakes = ReadList(def, "cameraShakes");
            manualTriggerCameraShakes = ReadList(def, "manualTriggerCameraShakes");
        }

        public override void Initialize(WarpforgeEffectPlayer controller)
        {
            base.Initialize(controller);
            // 自动档：原版在 OnEnable，我们在 Initialize（见文件头的偏离说明）
            for (int i = 0; i < cameraShakes.Length; i++) Play(cameraShakes[i], "自动档");
        }

        /// <summary>给 **`UnityEvent`** 用的手动档（原版 `AnimEventDoShake`；触发源订正见文件头 ——
        /// ⛔ **不是**动画片段的事件）。越界**静默 return**（原版如此）。</summary>
        public void AnimEventDoShake(int i)
        {
            if (i < 0 || i >= manualTriggerCameraShakes.Length) return;
            Play(manualTriggerCameraShakes[i], "手动档");
        }

        /// <summary>原版 `TriggerCameraShake(i)`：**与 `AnimEventDoShake` 读同一个数组**
        /// （`manualTriggerCameraShakes // 0x40`）。越界**报错**（原版如此）。
        /// 🔴 **2026-09-18 更正**：这里原来读的是 `cameraShakes // 0x38` —— **读错了数组**。
        /// 实据：`AnimFXModuleScreenShake__TriggerCameraShake.c:15` 与 `__AnimEventDoShake.c:7`
        /// **都读 `param_1 + 0x40`**（越界报错用的也是这个数组的长度）；
        /// `dump.cs:48610,48612`：`cameraShakes // 0x38` · `manualTriggerCameraShakes // 0x40`；
        /// **只有 `OnEnable` 读 `cameraShakes`**（`__OnEnable.c:8`）。⇒ **434 个实例全受影响**。</summary>
        public void TriggerCameraShake(int i)
        {
            if (i < 0 || i >= manualTriggerCameraShakes.Length)
            {
                Debug.LogError($"[WarpforgeVFX] 屏震下标 {i} 越界（{gameObject.name} 只有 " +
                               $"{manualTriggerCameraShakes.Length} 条手动档）");
                return;
            }
            Play(manualTriggerCameraShakes[i], "TriggerCameraShake");
        }

        void Play(ShakeEntry e, string from)
        {
            if (e == null || string.IsNullOrEmpty(e.preset)) return;   // 原版：preset 空就跳过
            var req = Resolve(e);
            if (OnShake == null)
            {
                DroppedRequests++;
                if (DroppedRequests <= 5)
                    Debug.LogWarning($"[WarpforgeVFX] 屏震 preset `{e.preset}`（{from}）**没有下游接** —— " +
                                     "表现层要挂 `WFModuleScreenShake.OnShake`（我们这边接 CardFeel.ShakeCamera）。" +
                                     "这条警告只报前 5 次。");
                return;
            }
            OnShake(req);
        }

        // ---- preset 表（从 Resources 读，懒加载一次）----
        // 🆕 **2026-10-23（R9 落地）**：`shake_presets.json` 现在**连波形本体一起带**了 ——
        //   `signals` 段是 4 份 `Cinemachine.NoiseSettings`（`Warpforge 6D Shake*`）的
        //   `PositionNoise`/`OrientationNoise` × 3 octave × X/Y/Z × `{Frequency,Amplitude,Constant}` 全表；
        //   每个 preset 的 `rawSignal` 是**信号名**（空 = 走 `defaultSignal`）。
        //   采样算式（机器码级）→ `资料/普查产出_1023b/R9_rawSignal_算式.md`；⛔ 别按「Constant 是不是常数」猜。
        //   ⚠️ 只有**下游**（`CardFeel.ShakeNoise01`）真正采它；本文件只负责「解析出这个 preset 该用哪张表」。
        [Serializable] class PresetRec
        {
            public string name;
            public float delay, sustainTime, attackTime, decayTime, amplitude, frequency;
            public float dirX, dirY, dirZ;
            public int hasRawSignal;
            /// <summary>信号名（空 = 用 `PresetDoc.defaultSignal`）。</summary>
            public string rawSignal;
        }
        [Serializable] class PresetDoc
        {
            public int count;
            public string defaultSignal;
            public SignalRec[] signals;
            public PresetRec[] presets;
        }

        static Dictionary<string, PresetRec> _presets;
        static Dictionary<string, SignalRec> _signals;
        static readonly List<SignalRec> _signalList = new List<SignalRec>();
        static SignalRec _defaultSignal;
        static readonly List<string> _missingPresets = new List<string>();
        static readonly List<string> _rawSignalPresets = new List<string>();
        static readonly List<string> _missingSignals = new List<string>();

        /// <summary>找不到的 preset 名（自检用）。</summary>
        public static IReadOnlyList<string> MissingPresets { get { return _missingPresets; } }
        /// <summary>2026-10-23（R9 落地后改口径）：**实际用到了非默认信号**（`rawSignal` 非空）的 preset 名 ——
        /// 5 个。⛔ 它**不再是「没实现」的标记**（波形已经真采了），是「这几个走的是 × 号信号」的读数。
        /// 判据 → `资料/普查产出_1023b/特效线_执行报告.md` §二。</summary>
        public static IReadOnlyList<string> RawSignalPresets { get { return _rawSignalPresets; } }
        /// <summary>🆕 数据里引用了、但 `signals` 段里找不到的信号名（**真空缺**，会一次性警告）。
        /// 非空 = json 与 preset 不同步（`工具/gen_shake_presets.py` 重跑一遍通常就好）。</summary>
        public static IReadOnlyList<string> MissingSignals { get { return _missingSignals; } }
        /// <summary>默认信号（没有 `rawSignal` 的 preset 落到它）。数据没读到时为 null。</summary>
        public static SignalRec DefaultSignal { get { EnsurePresets(); return _defaultSignal; } }
        /// <summary>数据里的**全部**信号表（4 份 `Warpforge 6D Shake*`；自检用它验结构与一致性）。
        /// ⚠️ 返回的是内部列表，只读用，⛔ 别改。</summary>
        public static IReadOnlyList<SignalRec> Signals { get { EnsurePresets(); return _signalList; } }
        public static void ResetDiagnostics()
        {
            _missingPresets.Clear(); _rawSignalPresets.Clear(); _missingSignals.Clear(); DroppedRequests = 0;
        }

        static void EnsurePresets()
        {
            if (_presets != null) return;
            _presets = new Dictionary<string, PresetRec>();
            _signals = new Dictionary<string, SignalRec>();
            _signalList.Clear();
            _defaultSignal = null;
            var ta = Resources.Load<TextAsset>("WarpforgeVFX/shake_presets");
            if (ta == null)
            {
                Debug.LogWarning("[WarpforgeVFX] 找不到 Resources/WarpforgeVFX/shake_presets.json —— " +
                                 "屏震只能退到兜底参数。生成它：工具/gen_shake_presets.py");
                return;
            }
            var doc = JsonUtility.FromJson<PresetDoc>(ta.text);
            if (doc == null) return;
            if (doc.signals != null)
                foreach (var s in doc.signals)
                    if (s != null && !string.IsNullOrEmpty(s.name))
                    {
                        _signals[s.name] = s;
                        _signalList.Add(s);
                    }
            if (!string.IsNullOrEmpty(doc.defaultSignal))
                _signals.TryGetValue(doc.defaultSignal, out _defaultSignal);
            if (doc.presets != null)
                foreach (var p in doc.presets)
                    if (p != null && !string.IsNullOrEmpty(p.name)) _presets[p.name] = p;
        }

        /// <summary>🆕 2026-10-23（R9）：preset 名 → 它该播的信号表（空/找不到 ⇒ 默认信号）。
        /// 找不到**非默认**信号时会记进 `MissingSignals` 并一次性警告（不静默退到默认 ——
        /// 那会让「换了波形」变成「悄悄用另一个波形」）。</summary>
        public static SignalRec SignalFor(string presetName)
        {
            EnsurePresets();
            PresetRec p;
            if (presetName != null && _presets.TryGetValue(presetName, out p)
                && p != null && !string.IsNullOrEmpty(p.rawSignal))
            {
                SignalRec s;
                if (_signals.TryGetValue(p.rawSignal, out s)) return s;
                if (!_missingSignals.Contains(p.rawSignal))
                {
                    _missingSignals.Add(p.rawSignal);
                    Debug.LogWarning($"[WarpforgeVFX] 屏震 preset `{presetName}` 引用的信号 `{p.rawSignal}` " +
                                     "在 shake_presets.json 的 `signals` 段里找不到 —— 退回默认信号" +
                                     "（重跑 工具/gen_shake_presets.py）。已知缺：" +
                                     string.Join(" / ", _missingSignals));
                }
            }
            return _defaultSignal;
        }

        /// <summary>preset 值 → 应用 6 个覆盖开关。**找不到 preset 要报**（不然屏震会「安静地不对」）。</summary>
        public static Request Resolve(ShakeEntry e)
        {
            EnsurePresets();
            var r = new Request { preset = e.preset };
            PresetRec p = null;
            if (!_presets.TryGetValue(e.preset, out p))
            {
                if (!_missingPresets.Contains(e.preset))
                {
                    _missingPresets.Add(e.preset);
                    Debug.LogWarning($"[WarpforgeVFX] 屏震 preset `{e.preset}` 在 shake_presets.json 里" +
                                     "找不到 —— 用兜底参数播（幅度/频率可能不对）。已知缺：" +
                                     string.Join(" / ", _missingPresets));
                }
            }
            // 🆕 2026-10-23（R9 落地）：这 5 个 preset 在原版**叠了自定义波形**（`rawSignal`）。现在
            //   **真采了** —— 信号表随 `Request.signal` 送到下游（`CardFeel.ShakeNoise01` 按机器码算式采样）。
            //   ⇒ 这里不再需要「没实现」的警告，只记一份读数（自检用它核「这 5 个确实走了非默认信号」）。
            //   判据 → `资料/普查产出_1023b/R9_rawSignal_算式.md`（算式）+ 同目录 `特效线_执行报告.md` §二（全表）。
            if (p != null && p.hasRawSignal != 0 && !_rawSignalPresets.Contains(e.preset))
                _rawSignalPresets.Add(e.preset);
            // 信号解析：空 / 找不到 ⇒ 默认信号（找不到**非默认**信号会在 `SignalFor` 里报警，不静默）
            r.signal = SignalFor(e.preset);

            r.sustainTime = p != null ? p.sustainTime : 0.1f;
            r.attackTime  = p != null ? p.attackTime  : 0f;
            r.decayTime   = p != null ? p.decayTime   : 0.3f;
            r.amplitude   = p != null ? p.amplitude   : 2f;
            r.frequency   = p != null ? p.frequency   : 0.05f;
            r.direction   = p != null ? new Vector3(p.dirX, p.dirY, p.dirZ) : Vector3.zero;
            r.delay       = p != null ? p.delay       : 0f;

            // 6 个覆盖开关：**非 0 才覆盖**（原版 `GetModifiedPreset` 的逐项语义）
            if (e.overwriteSustainTime != 0) r.sustainTime = e.sustainTime;
            if (e.overwriteAttackTime  != 0) r.attackTime  = e.attackTime;
            if (e.overwriteDecayTime   != 0) r.decayTime   = e.decayTime;
            if (e.overwriteAmplitude   != 0) r.amplitude   = e.amplitude;
            if (e.overwriteFrequency   != 0) r.frequency   = e.frequency;
            if (e.overwriteDirection   != 0) r.direction   = e.direction;
            if (e.delay > 0f) r.delay = e.delay;    // `delay` 没有 overwrite 开关，原版直接用它
            return r;
        }

        /// <summary>🆕 **2026-10-14（A394 欠账收口 / W10）**：`internal`（原为 `private`）——
        /// **场景侧**那份模块（`WFSceneModuleScreenShake`）在 `Configure` 里**复用这同一个读法**
        /// 把旁挂里 `modules.第 i 个模块.{cameraShakes|manualTriggerCameraShakes}[j].*` 装成 `ShakeEntry[]`。
        /// 🔴 **一处读法、两处调用**（本仓铁律 6）：⛔ 别在场景侧再抄一份 `ReadList`
        /// （键名/`presetSO` 的 `@asset:` 解析抄错一格就是**静默空轨道**）。
        /// ⚠️ **只放宽到 `internal`**，别再往上抬（两个类都在 `WarpforgeVFX` 程序集里，够用）。
        /// 键名语法：与 `数据/游戏数据/animfx_modules.json` 一致（`cameraShakes[0].presetSO`；
        /// 生成器 → `工具/gen_env_blendables.py` 的 `pack_module_fields`）。⚠️ 键里的下标写作中文「第 i 个模块」：裸尖括号在 XML 注释里会被当标签（CS1570）。</summary>
        internal static ShakeEntry[] ReadList(WFModuleDef def, string key)
        {
            if (def == null) return new ShakeEntry[0];
            // ⚠️ 用 `CountList`（按前缀数），**不能**用 `def.Has(key + "[n]")` ——
            //    数据里没有 `cameraShakes[0]` 这个键，只有 `cameraShakes[0].amplitude` 这种。
            int n = def.CountList(key);
            if (n == 0) return new ShakeEntry[0];
            var arr = new ShakeEntry[n];
            for (int i = 0; i < n; i++)
            {
                string p = key + "[" + i + "]";
                var e = new ShakeEntry
                {
                    preset = def.GetString(p + ".presetSO"),
                    delay = def.GetFloat(p + ".delay"),
                    overwriteSustainTime = def.GetInt(p + ".overwriteSustainTime"),
                    sustainTime = def.GetFloat(p + ".sustainTime"),
                    overwriteAttackTime = def.GetInt(p + ".overwriteAttackTime"),
                    attackTime = def.GetFloat(p + ".attackTime"),
                    overwriteDecayTime = def.GetInt(p + ".overwriteDecayTime"),
                    decayTime = def.GetFloat(p + ".decayTime"),
                    overwriteAmplitude = def.GetInt(p + ".overwriteAmplitude"),
                    amplitude = def.GetFloat(p + ".amplitude"),
                    overwriteFrequency = def.GetInt(p + ".overwriteFrequency"),
                    frequency = def.GetFloat(p + ".frequency"),
                    overwriteDirection = def.GetInt(p + ".overwriteDirection"),
                    direction = new Vector3(def.GetFloat(p + ".direction.x"),
                                            def.GetFloat(p + ".direction.y"),
                                            def.GetFloat(p + ".direction.z")),
                };
                // presetSO 在数据里是 `@asset:MonoBehaviour:Shake Hit Small` —— 取最后那段名字
                string kind, type, rest;
                WFModuleDef.SplitRef(e.preset, out kind, out type, out rest);
                if (kind == "asset") e.preset = rest;
                arr[i] = e;
            }
            return arr;
        }
    }
}
