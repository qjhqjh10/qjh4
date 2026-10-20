// CameraIntro.cs — 战斗线 `T12 演出镜头时序`：**开局那段 2.5 s 的相机飞回本位**。
//
// ─────────────────────────────────────────────────────────────────────────────
// 【原版语义 · 判据链】（全文 → `资料/普查产出_1020/W58_战斗线_T12判据.md` §1·2 / §1·3）
//   `BattleManager.OnInitializationComplete` → `StartBattleSequence` 第 0 步：
//       LoadingMenu.LoadingFade(false, null)     // 进战斗黑场 1.25 s 淡出
//       cameraAnim.Play()                        // 🔴 **无参**，播 Animation.clip
//   `cameraAnim` = 挂在 `Cinemachine Vcam` 上的 `Animation` 组件（13 场各一个），
//   它的 `clip` 就是该场唯一那条 `<场名> Camera Intro` —— **legacy 曲线**、
//   `m_SampleRate=60`、`m_WrapMode=1`(= `WrapMode.Once`)、`m_MuscleClipSize=0`。
//   ⇒ 位置/旋转/光晕三组曲线本地全有，**运行时用 `AnimationClip.SetCurve` 重建**，
//     不导入 `.anim`、不从 bundle 取原件（理由 → 判据 §3·1：批处理可逐帧推、可断言、无 GUID）。
//
// 【数据从哪来】`Resources/camera_intro_clips.json`（283 KB）——
//   由 `工具/gen_camera_intro_clips.py` 从 `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena*/`
//   生成（全量件另存 `Unity/数据/游戏数据/camera_intro_clips.json`）。
//   🔴 **键 = 场键**（`battlearena1` 这种，与 `ArenaRuntimeLoader.ResolveArenaKey` 同源）。
//   ⛔ **绝不按 `m_Name` 找 clip**：spacewolves 与 emperorschildren 的名字**一模一样**
//      （都叫 `Battle Arena Emperors Children Camera Intro`）；leviathan 没有 `Camera Intro` 后缀。
//
// 【坐标基准 · 一动就整体偏 100】
//   原版 clip 的 `path=""` 值是**相对 vcam 父节点（原版 x=+100）**的局部值；
//   我们的 `BoardCamera` 建在场景根（identity）下、本位 = `(0, 2.222075, −13.57198)`
//   = 原版 `camera.pos − (100,0,0)` ⇒ **clip 的局部值可以逐字当 `boardCam.localPosition` 用**。
//   谁要是动了 `BoardCamera` 的父级/本位，运镜会整体偏 100（自检 §3·5⑦ 盯着这个前提）。
//
// 【切线：⛔ 别照抄「全 0」那句】
//   调查文件 §3·2 写「原 JSON 的 `inSlope/outSlope` 全 0、与 `new Keyframe(t,v)` 默认一致」——
//   **现核不成立**（2026-10-20 逐键读）：2 键位置曲线、leviathan 的 4 键位置曲线、
//   aeldari 的 3 键 euler、以及 4 条四元数旋转曲线都带**非零切线**。
//   （例：EC/sororitas/spacewolves 的 position 首键 `outSlope` = 割线 ⇒ 原版是「起步即匀速、到本位减速停」；
//     用 0 切线重建会变成 smoothstep，端点仍对得上 ⇒ 端点断言抓不到。）
//   ⇒ 生成器把 `si`/`so` 逐键存进 JSON，本文件逐键写回 `new Keyframe(t, v, inTangent, outTangent)`。
//
// 【光晕 · 原版是「每帧相乘」不是插值】
//   原版 `CameraLensFlareIntenistyAnimation.LateUpdate` 逐句：
//       lensFlare.intensity = flareIntensityMultiplier * lensFlare.intensity;
//       lensFlare.enabled   = lensFlare.intensity != 0;
//   ⇒ 曲线 1→0 那一段是**乘法式衰减**，归零后 flare **整局关**（原版行为，⛔ 别「修」成 lerp / 别加恢复）。
//   `flareIntensityMultiplier` 同时**写进 clip**（原版 float 曲线就是绑在同 GameObject 的自定义组件上）
//   与**在 `Sample` 里再求一次**：后者是兜底 —— 万一本 Unity 版本对「legacy 曲线绑自定义 MonoBehaviour 字段」
//   的 `SampleAnimation` 行为有出入，消费端仍逐帧确定（批处理里没有帧循环，本来也不指望动画系统自己跑）。
//
// 【泵】`Tick` 挂在 `BattleDriver.AdvanceTimeline` 上（紧挨 `TickLoadingFade`）：
//   真机由 `Update` 推、批处理由 `BattleScene.Step` 推 —— 同本仓「不许只在一种环境下活」的口径。
//   🔴 **2026-10-20（W58）生产修复②**：**批处理的生产路径彻底停用**（`PlayCore` 里
//   `Application.isBatchMode ⇒ _batchSuppressed`）—— `BattleScene.Run` 量测前有大量 `Step`，
//   一旦 `_playing == true` 就会把 `boardCam` 推进 clip 中段，屏幕量测/射线/落点往返全按错位姿算，
//   一片红（实测数值探针卡屏外、落点往返 0/18、敌方行 −32.5%、T09 `RaycastAll` 首中无 ……）。
//   自检要验时序的走**显式**口 `StartForTest` / `BattleDriver.StartCameraIntroForTest()`（仍在批处理下真播）。
//   ⚠️ 与 `CardFeel.ShakeCamera` 共用相机 transform 写权；开场期间没有战斗事件 ⇒ 不会互相踩。
//   ⚠️ **回放/教程是否播运镜：原版 `OnInitializationComplete` 没有按 `matchType` 分支（推断播），未实况**。
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using UnityEngine;
// ⚠️ **用别名、别 `using UnityEngine.Rendering;`** —— 那个命名空间里有 `ObjectPool<T>`，
//    与 `UnityEngine.Pool` 撞名（本仓 `FlareScenarioToggler.cs` 记过这条）。
using LensFlareComponentSRP = UnityEngine.Rendering.LensFlareComponentSRP;

namespace CardPresentation
{
    // ============================================================ 数据表（与生成器 schema 逐字对应）
    /// <summary>一个关键帧：`t` 时间、`v` 值（逐分量）、`si`/`so` = inSlope/outSlope（逐分量）。
    /// 浮点曲线（flare）的数组长度是 1；位置/欧拉是 3；四元数是 4。</summary>
    [Serializable]
    public class CameraIntroKey
    {
        public float t;
        public float[] v;
        public float[] si;
        public float[] so;
    }

    /// <summary>这条 clip 是从哪个包/文件读出来的（诊断用；⛔ 运行期不参与绑定）。</summary>
    [Serializable]
    public class CameraIntroSrc
    {
        public string pkg;
        public string animation;
        public string clip;
        public int pid;
    }

    /// <summary>一场的开场运镜。`euler` 与 `rotation` **二选一**（生成器已保证）。</summary>
    [Serializable]
    public class CameraIntroClip
    {
        /// <summary>场键（`battlearena1` …）—— **唯一的查找键**。</summary>
        public string key;
        /// <summary>原版 `m_Name`（**只作诊断**；⛔ 不拿它查表）。</summary>
        public string name;
        public bool legacy;
        public float sampleRate;
        /// <summary>`"Once"` —— 原版 `m_WrapMode = 1`（`dump.cs:520174`：`WrapMode.Once = 1`）。</summary>
        public string wrapMode;
        /// <summary>**全部曲线最晚的键**（位置/旋转在 2.5 s 到位，多出来的时间是光晕淡出）。</summary>
        public float length;
        public CameraIntroSrc src;
        public CameraIntroKey[] position;
        public CameraIntroKey[] euler;
        public CameraIntroKey[] rotation;
        public CameraIntroKey[] flare;
    }

    [Serializable]
    public class CameraIntroFile
    {
        public int schema;
        public string note;
        public string source;
        public CameraIntroClip[] clips = new CameraIntroClip[0];
    }

    /// <summary>开局运镜的运行期播放器（挂在 `BoardCamera` 上）。
    /// **判据链 / 坑 → 文件头**。测试口见本类末尾。</summary>
    public class CameraIntro : MonoBehaviour
    {
        /// <summary>`Resources.Load&lt;TextAsset&gt;` 的路径（与 `CardAnimOrder.ResourcePath` 同惯例）。</summary>
        public const string ResourcePath = "camera_intro_clips";

        /// <summary>原版那条 float 曲线绑的字段（`CameraLensFlareIntenistyAnimation.flareIntensityMultiplier`，`+0x20`）。
        /// 原版 `Awake`：`lensFlare == null` ⇒ **组件自己 `enabled = false`**。
        /// ⚠️ `[NonSerialized]`：本组件是**运行时**加到 `BoardCamera` 上的；这些值不进场景存档
        /// （否则 `BattleScene.Run` 存盘时会写进一条指向运行时战场实例的引用）。</summary>
        [NonSerialized] public float flareIntensityMultiplier = 1f;

        /// <summary>原版 `+0x28`（`LensFlareComponentSRP`）。只有 6 场有（arena1/2 · aeldari · astra · leviathan · tauviorla）。
        /// ⚠️ `[NonSerialized]` 同 <see cref="flareIntensityMultiplier"/>。</summary>
        [NonSerialized] public LensFlareComponentSRP lensFlare;

        // ---- 状态 ----
        Camera _cam;
        AnimationClip _clip;
        AnimationCurve _flareCurve;
        Vector3 _homePos;
        Quaternion _homeRot;
        bool _hasHome;
        float _t;
        float _len;
        bool _playing;
        // 🔴 批处理生产路径已停用（2026-10-20 W58）：`_playing == false` 且本标志为 true
        //    ⇒ `Tick` 整条不跑（连「播完后每帧乘光晕」那一支也不跑），相机/光晕都不动。
        //    自检口 `StartForTest` 会把它清掉（显式起播仍可逐帧推）。
        bool _batchSuppressed;
        string _arenaKey;
        // 光晕的「本场初始强度」：原版每局是**新场景** ⇒ flare 的 `intensity` 回到序列化值（本工程 prefab = 1）；
        // 我们的战场实例同一场**不重建**（`ArenaRuntimeLoader`）⇒ 上一局归零的 flare 会跨局残留。
        // 记下来、在**下一局起播时还原**，复刻「新场景」那一档；⚠️ **局内**仍按原版归零后整局关（⛔ 别改成恢复）。
        LensFlareComponentSRP _flareBound;
        float _flareBaseIntensity = 1f;
        bool _hasFlareBase;

        // ---- 表缓存 ----
        static CameraIntroClip[] _all;
        static Dictionary<string, CameraIntroClip> _byKey;
        static readonly HashSet<string> _warned = new HashSet<string>();

        /// <summary>表载进来了没有（没载 = 资源不在；<see cref="LastError"/> 有话说）。</summary>
        public static bool Loaded { get { EnsureLoaded(); return _all != null && _all.Length > 0; } }
        /// <summary>表里有几场。</summary>
        public static int ClipCount { get { EnsureLoaded(); return _all != null ? _all.Length : 0; } }
        /// <summary>载表/解析失败的原因（无错 = null）。</summary>
        public static string LastError { get; private set; }

        /// <summary>表里全部场键（自检对账用）。</summary>
        public static IEnumerable<string> Keys
        {
            get
            {
                EnsureLoaded();
                return _byKey != null ? (IEnumerable<string>)_byKey.Keys : new string[0];
            }
        }

        static void EnsureLoaded()
        {
            if (_all != null) return;
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                LastError = "`Resources/" + ResourcePath + ".json` 载不到 —— 跑一次 "
                          + "`python -I -X utf8 工具/gen_camera_intro_clips.py`";
                _all = new CameraIntroClip[0];
                _byKey = new Dictionary<string, CameraIntroClip>();
                return;
            }
            CameraIntroFile f = null;
            try { f = JsonUtility.FromJson<CameraIntroFile>(ta.text); }
            catch (Exception e) { LastError = "`" + ResourcePath + ".json` 解析失败：" + e.Message; }
            var items = f != null && f.clips != null ? f.clips : new CameraIntroClip[0];
            _all = items;
            _byKey = new Dictionary<string, CameraIntroClip>(items.Length);
            foreach (var c in items)
            {
                if (c == null || string.IsNullOrEmpty(c.key)) continue;
                if (!_byKey.ContainsKey(c.key)) _byKey[c.key] = c;
            }
            if (LastError == null && items.Length == 0)
                LastError = "`" + ResourcePath + ".json` 里一场都没有";
        }

        /// <summary>表里有没有这个场键（自检：13 场完整性）。</summary>
        public static bool HasArena(string arenaKey)
        {
            EnsureLoaded();
            return !string.IsNullOrEmpty(arenaKey) && _byKey.ContainsKey(arenaKey);
        }

        /// <summary>这一场 clip 的长度（秒）；没有 = −1。</summary>
        public static float LengthOf(string arenaKey)
        {
            var d = FindClip(arenaKey);
            return d != null ? d.length : -1f;
        }

        static CameraIntroClip FindClip(string arenaKey)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(arenaKey)) return null;
            CameraIntroClip d;
            return _byKey.TryGetValue(arenaKey, out d) ? d : null;
        }

        static void WarnOnce(string msg)
        {
            if (_warned.Count > 64 || !_warned.Add(msg)) return;
            Debug.LogWarning("[CameraIntro] " + msg);
        }

        // ============================================================ 播放
        /// <summary>起播这一场的开场运镜。
        /// ① 记本位 ② 按表建 clip ③ 绑光晕 ④ 真机 `Sample(0)` + `_playing = true`。
        /// 🔴 **批处理下一律不起播**：只做 ①~③ 的记录（`IsPlaying == false`、相机保持本位、`Tick` 不跑）；
        /// 自检要验时序请走 <see cref="StartForTest"/>。
        /// `flareHost` = 战场 prefab 实例（`loader.Current`；没有就传 null ⇒ 等价原版 `Awake` 的 disable）。</summary>
        public bool Play(string arenaKey, Camera cam, GameObject flareHost)
        {
            return PlayCore(arenaKey, cam, flareHost, allowInBatchMode: false);
        }

        /// <summary>「真的起播」的内部实现。`allowInBatchMode` **只有自检口**（<see cref="StartForTest"/>）
        /// 传 true —— 生产路径（`BattleDriver.StartCameraIntro`）在批处理下一律不起播，理由见下面
        /// 「批处理生产路径彻底停用」那一段。</summary>
        bool PlayCore(string arenaKey, Camera cam, GameObject flareHost, bool allowInBatchMode)
        {
            _playing = false;
            _batchSuppressed = false;
            _cam = cam;
            _arenaKey = arenaKey;
            // 先清掉上一次的曲线：即使这一场找不到（下面出声返回），`Tick` 也不会拿旧曲线继续乘光晕。
            _clip = null;
            _flareCurve = null;
            _len = 0f;
            if (cam == null)
            {
                WarnOnce("`cam` 是空的 ⇒ 这一局**没有开场运镜**（不许静默；检查 `BattleDriver.boardCam`）");
                return false;
            }
            _homePos = cam.transform.localPosition;
            _homeRot = cam.transform.localRotation;
            _hasHome = true;

            CameraIntroClip d = FindClip(arenaKey);
            if (d == null)
            {
                WarnOnce($"表里没有场键 `{arenaKey}` ⇒ 这一局**不播运镜**"
                       + $"（表里 {ClipCount} 场；键必须是 `battlearena1` 这种，⛔ 不是卡名/资产名）"
                       + (LastError != null ? " · 载表错误：" + LastError : ""));
                return false;
            }

            _clip = BuildClip(d);
            _len = d.length > 0f ? d.length : (_clip != null ? _clip.length : 0f);
            _flareCurve = d.flare != null && d.flare.Length > 0 ? BuildCurve(d.flare, 0) : null;

            // 光晕宿主：原版组件住在战场场景里，引用同一场景的 `LensFlareComponentSRP`；
            // 我们的战场是运行时 prefab 实例 ⇒ 从它子树里取（只有 6 场有）。
            lensFlare = flareHost != null
                      ? flareHost.GetComponentInChildren<LensFlareComponentSRP>(true)
                      : null;
            if (lensFlare != null)
            {
                if (!_hasFlareBase || _flareBound != lensFlare)
                {
                    // 第一次绑到这一件（= 原版读到的新场景）：它就是「序列化初始强度」。
                    _flareBound = lensFlare;
                    _flareBaseIntensity = lensFlare.intensity;
                    _hasFlareBase = true;
                }
                else
                {
                    // 同一件实例再开一局（`ArenaRuntimeLoader` 同一场不重建；原版这里会是新场景）
                    // ⇒ 还原到初始强度 + 重新打开，否则上一局归零后的 flare 会**永久不再出现**（跨局泄漏）。
                    lensFlare.intensity = _flareBaseIntensity;
                    lensFlare.enabled = true;
                }
                // 原版新场景里 flare 的序列化态就是**开着**（本工程 6 件 prefab 全 `m_Enabled: 1`）。
                lensFlare.enabled = true;
            }
            flareIntensityMultiplier = 1f;
            // 原版 `Awake`：`lensFlare == null` ⇒ **组件自己 `enabled = false`**。
            enabled = lensFlare != null;

            _t = 0f;
            // 🔴 2026-10-20（W58 · 生产修复②）：**批处理的生产路径彻底停用（不建播放态、不 tick、不写位姿）**。
            //   上一版只做到「起播时不 `Sample(0)`」（相机留在本位），但 `_playing` 仍是 true ⇒
            //   自检量测前那一串 `Step` 会经 `BattleDriver.AdvanceTimeline → Tick` 把相机推进 clip
            //   （实测 `battlearena1` 停在 clip 中段，本位是 `(0, 2.222, -13.572)`，起点是
            //   `(-7.14, 1.61, 1.07)`）⇒ 数值探针卡屏外（`stat2@(1087,57)` / `stat4@(1452,34)`）、
            //   落点往返 0/18、敌方行 −32.5%、`A421` `sensorSize`、T09 uGUI `RaycastAll` 首中无、
            //   T03 边界 0 …… 一片红。
            //   ⇒ 批处理下**只做记录**（本位 / clip / `ClipLength` / 光晕绑定，诊断与断言可读），
            //     **不置 `_playing`、不写位姿、`_batchSuppressed` 让 `Tick` 整条不跑**。
            //     真机**零差异**（`Update` 下一帧照旧 `Tick`）。
            //   ⚠️ 自检要验时序的走**显式**测试口 `StartForTest`（`BattleScene` T12 那几条已改成显式起播）。
            if (Application.isBatchMode && !allowInBatchMode)
            {
                _batchSuppressed = true;
                return true;
            }
            _playing = true;
            if (!Application.isBatchMode) Sample(0f);
            return true;
        }

        /// <summary>每帧推一次。挂在 `BattleDriver.AdvanceTimeline` 上（真机 `Update` / 批处理 `Step`）。</summary>
        public void Tick(float dt)
        {
            if (_cam == null) return;
            // 批处理生产路径已停用（见 `PlayCore` 末尾）：连「播完后每帧乘光晕」那一支也不跑。
            if (_batchSuppressed) return;
            if (_playing)
            {
                _t += dt;
                Sample(Mathf.Min(_t, _len));
                // 终帧仍是本位（clip 的位置/旋转都在 2.5 s 到位、之后 clamp），不需要回弹。
                if (_t >= _len) _playing = false;
            }
            else if (_flareCurve != null)
            {
                // 🔴 原版 `LateUpdate` 是**整局每帧**都跑的：曲线到 0 之后每帧再乘 0
                //   ⇒ 之后任何人（环境 blend 的 `FlareScenarioToggler`）再写 `intensity`，
                //   都会在下一帧被压回 0 并 disable（=「归零后整局关」）。
                ApplyFlare();
            }
        }

        /// <summary>停播。`resetHome` 时把本位写回去 —— `Begin()` 会被调多次（重开 / 录像），
        /// 新一局必须从本位起播，不能让上一局的终帧/残留 `_t` 混进来。</summary>
        public void Stop(bool resetHome)
        {
            _playing = false;
            _batchSuppressed = false;
            _t = 0f;
            if (resetHome && _hasHome && _cam != null)
            {
                _cam.transform.localPosition = _homePos;
                _cam.transform.localRotation = _homeRot;
            }
        }

        /// <summary>把 clip 采到 <paramref name="t"/>，并推一次光晕（原版 `LateUpdate` 的等价件）。</summary>
        void Sample(float t)
        {
            if (_clip != null && _cam != null) _clip.SampleAnimation(_cam.gameObject, t);
            if (_flareCurve != null)
            {
                // 兜底：显式求值（不依赖 legacy 曲线对自定义 MonoBehaviour 字段的绑定行为；见文件头）。
                flareIntensityMultiplier = _flareCurve.Evaluate(t);
                ApplyFlare();
            }
        }

        /// <summary>原版 `CameraLensFlareIntenistyAnimation.LateUpdate` 逐句（`intensity *= multiplier`，
        /// 归零即 `enabled = false`，**整局不再恢复**）。⛔ 别改成 lerp。</summary>
        public void ApplyFlare()
        {
            if (lensFlare == null) return;
            lensFlare.intensity *= flareIntensityMultiplier;
            lensFlare.enabled = lensFlare.intensity != 0f;
        }

        // ============================================================ 建 clip
        /// <summary>按表里一场的数据建一条 legacy clip（`SetCurve` + `SampleAnimation` 可逐帧推）。</summary>
        public static AnimationClip BuildClip(CameraIntroClip d)
        {
            if (d == null) return null;
            var clip = new AnimationClip
            {
                name = d.name,
                legacy = true,
                frameRate = d.sampleRate > 0f ? d.sampleRate : 60f,
                wrapMode = WrapMode.Once,          // 原版 `m_WrapMode = 1`
            };

            // ---- 位置（1~4 键；逐键保留切线）----
            SetVec3Curves(clip, "localPosition", d.position);

            // ---- 旋转 ----
            if (d.rotation != null && d.rotation.Length > 0)
            {
                // 原版本来就是 151 键 @60 fps 的四元数曲线 ⇒ 逐分量照写（含切线）。
                SetQuatCurves(clip, "localRotation", d.rotation);
            }
            else if (d.euler != null && d.euler.Length > 0)
            {
                // 原版是 euler 曲线。**按 60 fps 采样成四元数**再写：
                //   · 与原版那 4 条已采样场**表示形式一致**；
                //   · 绕开「`localEulerAnglesRaw.*` 能不能被运行时 SetCurve 绑上」这个不确定点（未 probe）。
                // 采样值由 `AnimationCurve.Evaluate` 给出（= Unity 自己的 Hermite 求值器，切线也照抄）。
                SetSampledEulerRotation(clip, d.euler, 60f);
            }

            // ---- 光晕：原版 float 曲线绑在同 GameObject 的自定义组件字段上 ----
            if (d.flare != null && d.flare.Length > 0)
                clip.SetCurve("", typeof(CameraIntro), "flareIntensityMultiplier", BuildCurve(d.flare, 0));

            return clip;
        }

        /// <summary>**自检口**：按场键建一条 clip（不碰任何相机）。表里没有 = null。</summary>
        public static AnimationClip BuildClipForTest(string arenaKey)
        {
            var d = FindClip(arenaKey);
            return d != null ? BuildClip(d) : null;
        }

        static void SetVec3Curves(AnimationClip clip, string prop, CameraIntroKey[] keys)
        {
            if (keys == null || keys.Length == 0) return;
            clip.SetCurve("", typeof(Transform), prop + ".x", BuildCurve(keys, 0));
            clip.SetCurve("", typeof(Transform), prop + ".y", BuildCurve(keys, 1));
            clip.SetCurve("", typeof(Transform), prop + ".z", BuildCurve(keys, 2));
        }

        static void SetQuatCurves(AnimationClip clip, string prop, CameraIntroKey[] keys)
        {
            if (keys == null || keys.Length == 0) return;
            clip.SetCurve("", typeof(Transform), prop + ".x", BuildCurve(keys, 0));
            clip.SetCurve("", typeof(Transform), prop + ".y", BuildCurve(keys, 1));
            clip.SetCurve("", typeof(Transform), prop + ".z", BuildCurve(keys, 2));
            clip.SetCurve("", typeof(Transform), prop + ".w", BuildCurve(keys, 3));
        }

        /// <summary>euler（度）→ 60 fps 四元数采样曲线。导数按各段 euler 曲线的 Hermite 求值后转四元数。</summary>
        static void SetSampledEulerRotation(AnimationClip clip, CameraIntroKey[] keys, float fps)
        {
            var cx = BuildCurve(keys, 0);
            var cy = BuildCurve(keys, 1);
            var cz = BuildCurve(keys, 2);
            float last = keys[keys.Length - 1].t;
            int n = Mathf.Max(1, Mathf.RoundToInt(last * fps));
            var kx = new Keyframe[n + 1];
            var ky = new Keyframe[n + 1];
            var kz = new Keyframe[n + 1];
            var kw = new Keyframe[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float t = i / fps;
                var q = Quaternion.Euler(cx.Evaluate(t), cy.Evaluate(t), cz.Evaluate(t));
                // 相邻键间隔 1/60 s ⇒ 0 切线带来的段内偏差 < 0.01°（端点逐位相同）。
                kx[i] = new Keyframe(t, q.x);
                ky[i] = new Keyframe(t, q.y);
                kz[i] = new Keyframe(t, q.z);
                kw[i] = new Keyframe(t, q.w);
            }
            clip.SetCurve("", typeof(Transform), "localRotation.x", new AnimationCurve(kx));
            clip.SetCurve("", typeof(Transform), "localRotation.y", new AnimationCurve(ky));
            clip.SetCurve("", typeof(Transform), "localRotation.z", new AnimationCurve(kz));
            clip.SetCurve("", typeof(Transform), "localRotation.w", new AnimationCurve(kw));
        }

        /// <summary>把 JSON 的一列关键帧（含 inSlope/outSlope）变成 `AnimationCurve`。
        /// 🔴 切线必须逐键写回：见文件头「切线」那一段。</summary>
        static AnimationCurve BuildCurve(CameraIntroKey[] keys, int comp)
        {
            if (keys == null || keys.Length == 0) return null;
            var ks = new Keyframe[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                var k = keys[i];
                float v = Comp(k.v, comp);
                float inT = Comp(k.si, comp);
                float outT = Comp(k.so, comp);
                ks[i] = new Keyframe(k.t, v, inT, outT);
            }
            return new AnimationCurve(ks);
        }

        static float Comp(float[] a, int i)
        {
            return a != null && i >= 0 && i < a.Length ? a[i] : 0f;
        }

        // ============================================================ 测试口
        /// <summary>自检：**显式**起播（= 允许在批处理下真的置播放态并逐帧推）。
        /// ⚠️ 与生产口 <see cref="Play"/> 的区别只有这一处：批处理下生产口停用、本口不停用。</summary>
        public bool StartForTest(string arenaKey, Camera cam, GameObject flareHost)
        {
            return PlayCore(arenaKey, cam, flareHost, allowInBatchMode: true);
        }
        /// <summary>自检：推一帧（= <see cref="Tick"/>）。</summary>
        public void TickForTest(float dt) { Tick(dt); }

        public bool IsPlaying { get { return _playing; } }
        public float Elapsed { get { return _t; } }
        /// <summary>这一局 clip 的时长（= JSON 的 `length`）。</summary>
        public float ClipLength { get { return _len; } }
        public string ArenaKey { get { return _arenaKey; } }
        public Vector3 HomePosition { get { return _homePos; } }
        public Quaternion HomeRotation { get { return _homeRot; } }
        /// <summary>这一场有没有光晕曲线（7 场有；真正挂 flare 的 6 场）。</summary>
        public bool HasFlareCurve { get { return _flareCurve != null; } }
        public LensFlareComponentSRP FlareForTest { get { return lensFlare; } }
        public float FlareMultiplierForTest { get { return flareIntensityMultiplier; } }
        /// <summary>跑出来的 clip（自检读 `legacy/frameRate/wrapMode/length`）。</summary>
        public AnimationClip ClipForTest { get { return _clip; } }

        /// <summary>自检：把相机采到 <paramref name="t"/> 并返回位置（不改变 `_playing/_t`）。</summary>
        public Vector3 PosePositionAtForTest(float t)
        {
            Sample(t);
            return _cam != null ? _cam.transform.localPosition : Vector3.zero;
        }
        /// <summary>自检：把相机采到 <paramref name="t"/> 并返回旋转。</summary>
        public Quaternion PoseRotationAtForTest(float t)
        {
            Sample(t);
            return _cam != null ? _cam.transform.localRotation : Quaternion.identity;
        }
        /// <summary>自检：同时取位置 + 旋转（等价 `PosePositionAtForTest` + `PoseRotationAtForTest`，一次采样）。</summary>
        public void PoseAtForTest(float t, out Vector3 pos, out Quaternion rot)
        {
            Sample(t);
            pos = _cam != null ? _cam.transform.localPosition : Vector3.zero;
            rot = _cam != null ? _cam.transform.localRotation : Quaternion.identity;
        }
    }
}
