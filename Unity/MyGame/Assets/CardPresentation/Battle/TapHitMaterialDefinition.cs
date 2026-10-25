// TapHitMaterialDefinition.cs — 「点地面时这条碰撞体该放什么」的数据 + 播放体（原版同名类）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【判据正本】`Unity/资料/完全复刻/判据/Tap族_地面点击反馈.md`（§2 类结构 / §3.3 PlayParticle）。
//   反编译原文（**第一权威**，逐行读过）：
//     · `D:/2/tools/decomp_full/TapHitMaterialDefinition.ParticleCollisionDefinition__PlayParticle.c`（149 行）
//     · `D:/2/tools/decomp_full/TapHitMaterialDefinition__Update.c`（11 行）
//     · `D:/2/tools/decomp_full/TapHitMaterialDefinition__Awake.c` / `__OnDestroy.c`
//     · `D:/2/tools/decomp_full/AudioCue__CanPlay.c`（cue 自身冷却的判据）
//   ⚠️ **播放行为体是 `ParticleCollisionDefinition.PlayParticle`**；`TapHitMaterialDefinition.TryPlayParticles`
//      只是**零调用点**的包装层（`grep` 全量 `.c`/`.cs` 无外部引用）⇒ ⛔ 别照它写。
//      `TapParticleController__Update.c:356` 直接调 `PlayParticle`。
//
// 【它做什么（照 `.c`，不是照文档）】
//   `PlayParticle(position, normal, parent)`：
//     ① `particles` / `audio` 任一无效 ⇒ **整条 return**（不是「只播其中一个」）
//     ② **粒子实例按 prefab 全局缓存一件**（`static Dictionary`），**反复挪位置**；不是每次 `Instantiate`
//     ③ `parent == null` ⇒ `ps.transform.position = position + normal * 0.1f`（常量 `0x1834b2dc4` = 0.1，
//        `资料/…/判据` §3.3 已直读）、`rotation = Quaternion.LookRotation(normal)`
//        否则 ⇒ `SetParent(parent, worldPositionStays:false)` + `localPosition = zero` + `localRotation = identity`
//        ⇒ **锚点语义 = 「本地零位」**（特效出现在锚点处，与你点在哪儿无关）
//     ④ `ps.Play()` ⑤ **音效在【射线命中点】** 播 3D（不是加过偏移的那个点）
//   `TapHitMaterialDefinition.Update`：每帧 `remainingNotPlayTime -= dt`，`<= 0` 就 `enabled = false`
//     （`Awake` 里先 `enabled = false` —— 所以它平时不耗帧）。
//   `OnDestroy`：**任意一个定义被销毁就整表清空**（原版那句 `Clear()` 作用在静态字典上）。
//
// 【两个死字段（别「顺手补逻辑」）】
//   · `useTargetAnchor`：本 build **运行时从不读**（全量 ilspy 只有声明一处）。真正生效的是
//     `targetAnchor != null`。这里照抄只为「清单 == 资产」可逐字段复核。
//   · `instancedParticle`（原版 `+0x20`）：本 build **从不读写**（`.c` 里 `param_1+0x20` 零出现，
//     用的是局部变量存缓存实例）⇒ **本类不设这个字段**（设了就是给死字段补逻辑）。
//
// 【我们这边的落地差异（都是「原版用 Addressables、我们用工程内直连」）】
//   · 原版 `particles` 是 `AssetReferenceTyped<GameObject>`、`audio` 是 `AssetReferenceTyped<AudioCue>`；
//     我们：`particles` = **prefab 直连**（建场侧 `ArenaBuilder` 从 `WarpforgeVFX/Prefabs/<名>.prefab` 赋），
//     `audio` = **cue 名**（走 `WFSoundBank.TryGetCue`，同一张表）。
//   · 原版 cue 的 `timeToPlayAgain` 冷却按 **cue 资产实例**计（`AudioCue.CanPlay` 读 `+0x30/+0x34`）；
//     `WFSoundPlayer.Play` **没有**这层冷却 ⇒ 本类用 `static Dictionary<WFSoundCue,float>` **逐 cue** 补上
//     （⛔ 不改 `WFSoundPlayer` —— 那是全项目共用件）。
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;
using WarpforgeVFX;

namespace CardPresentation
{
    /// <summary>点地面反馈的「材质定义」：挂在**有 3D 碰撞体**的 GameObject 上。
    /// 由 `TapParticleController` 在射线命中后读取；自己不接收输入。</summary>
    [DisallowMultipleComponent]
    public class TapHitMaterialDefinition : MonoBehaviour
    {
        /// <summary>原版嵌套类 `TapHitMaterialDefinition.ParticleCollisionDefinition`（字段名逐字照抄资产）。</summary>
        [System.Serializable]
        public class ParticleCollisionDefinition
        {
            [Tooltip("粒子 prefab（原版 = AssetReferenceTyped<GameObject>；我们直连工程内 prefab）")]
            public GameObject particles;

            [Tooltip("音效 cue 名（原版 = AssetReferenceTyped<AudioCue>；我们走 WFSoundBank 同一个字典）")]
            public string audio = "";

            // ---- 运行时（原版 `instanceAudioCue` 那两格；我们不序列化）----
            /// <summary>粒子实例按 prefab 缓存（原版 `static Dictionary<AssetReference, ParticleSystem>`）。
            /// ⚠️ 值可能是 Unity-null（换场景时实例被销毁）⇒ 取用时自己判。</summary>
            static readonly Dictionary<GameObject, ParticleSystem> CacheParticles =
                new Dictionary<GameObject, ParticleSystem>();

            /// <summary>cue 的「上次播放时刻」（原版 `AudioCue.lastTimePlayed`，**按 cue 资产算**）。</summary>
            static readonly Dictionary<WFSoundCue, float> LastCuePlayed =
                new Dictionary<WFSoundCue, float>();

            /// <summary>自检/诊断：当前缓存了几件粒子实例。</summary>
            public static int CacheCountForTest { get { return CacheParticles.Count; } }

            /// <summary>自检/诊断：最后一次播的那件粒子实例。</summary>
            [System.NonSerialized] public ParticleSystem lastInstance;

            /// <summary>已经报过「cue 解不出」的定义（同一条只报一次，别每帧刷屏）。</summary>
            [System.NonSerialized] bool _warnedMissingCue;

            /// <summary>资源两侧都在吗（原版 `IsValid()` 是**零调用点**的死方法；
            /// 真正生效的是 `PlayParticle` 开头那两句内联的 `RuntimeKeyIsValid`）——
            /// 这里保留它只给自检用，⛔ 生产路径别拿它当门。</summary>
            public bool IsValid()
            {
                return particles != null && !string.IsNullOrEmpty(audio) && WFSoundBank.HasCue(audio);
            }

            /// <summary>照原版 `PlayParticle(position, normal, parent)`（`.c` 149 行）。
            /// <para>`parent == null` ⇒ 世界点 + 法线 × 0.1；非空 ⇒ 挂到锚点本地零位。</para>
            /// <para>⛔ 别在这里加「每次 Instantiate / 播完 Destroy」—— 原版是**缓存一件反复挪**。</para></summary>
            public void PlayParticle(Vector3 position, Vector3 normal, Transform parent)
            {
                // ① 原版：`particles` / `audio` 任一 `RuntimeKeyIsValid() == false` ⇒ return（整条都不播）
                if (particles == null)
                {
                    WarnOnce("`particles` 是空的（原版 `RuntimeKeyIsValid()==false` ⇒ 连粒子都不播）");
                    return;
                }
                WFSoundCue cue;
                if (string.IsNullOrEmpty(audio) || !WFSoundBank.TryGetCue(audio, out cue))
                {
                    WarnOnce($"cue `{audio}` 不在 `Resources/animfx_sounds.json` 里 ⇒ 照原版**整条不播**"
                           + "（跑 `工具/import_original_sfx.py` 补；见判据 §2.5）");
                    return;
                }

                // ② 粒子实例：按 prefab 缓存一件，反复挪位置（原版 `cacheParticles.TryGetValue` 那一路）
                ParticleSystem ps;
                if (!CacheParticles.TryGetValue(particles, out ps) || ps == null)
                {
                    var src = particles.GetComponent<ParticleSystem>();
                    if (src == null)
                    {
                        WarnOnce($"粒子 prefab `{particles.name}` 根上没有 `ParticleSystem`"
                               + "（原版 `go.GetComponent<ParticleSystem>()` 会在这里抛）⇒ 不播");
                        return;
                    }
                    ps = Object.Instantiate(src);          // 克隆整棵子树（原版 `Instantiate(src)`）
                    CacheParticles[particles] = ps;
                }

                // ③ 落点 / 朝向
                var t = ps.transform;
                if (parent == null)
                {
                    t.SetParent(null, false);
                    t.position = position + normal * TapParticleController.NormalOffset;   // 常量 0.1
                    t.rotation = Quaternion.LookRotation(normal);                          // 法线即朝向
                }
                else
                {
                    t.SetParent(parent, false);
                    t.localPosition = Vector3.zero;        // 「本地零位」—— 与你点在哪儿无关
                    t.localRotation = Quaternion.identity;
                }

                // ④ 播
                ps.Play();
                lastInstance = ps;

                // ⑤ 音效：**在射线命中点**响 3D（原版 `AudioCue.Play3D(position, 0)`，第 4 实参 0 = FX）
                if (CanPlayCue(cue))
                    WFSoundPlayer.Play(cue, position, is2d: false);
            }

            /// <summary>原版 `AudioCue.CanPlay`（`.c` 直读）：
            /// `(Time.time - lastTimePlayed &lt; timeToPlayAgain) &amp;&amp; lastTimePlayed != 0` ⇒ 不许播；
            /// 否则记下 `lastTimePlayed = Time.time` 并放行。
            /// <para>⚠️ 状态**按 cue 资产算**（原版就存在 cue 资产自己身上）⇒ 这里用静态表键到 `WFSoundCue`。</para></summary>
            static bool CanPlayCue(WFSoundCue cue)
            {
                if (cue == null) return false;
                float now = Time.time;
                float last;
                if (LastCuePlayed.TryGetValue(cue, out last) && last != 0f
                    && now - last < cue.timeToPlayAgain)
                    return false;
                LastCuePlayed[cue] = now;
                return true;
            }

            void WarnOnce(string msg)
            {
                if (_warnedMissingCue) return;
                _warnedMissingCue = true;
                Debug.LogWarning($"[Tap] 粒子定义（prefab={(particles != null ? particles.name : "<null>")}"
                               + $" · cue={(string.IsNullOrEmpty(audio) ? "<空>" : audio)}）：{msg}");
            }

            /// <summary>原版 `TapHitMaterialDefinition.OnDestroy` → `cacheParticles.Clear()`（整表清空）。
            /// ⚠️ 这**是原版行为**（任意一个定义被销毁就清整表），⛔ 别「优化」成只删自己那一项。</summary>
            public static void ClearCache()
            {
                CacheParticles.Clear();
                LastCuePlayed.Clear();
            }
        }

        [Tooltip("粒子 + 音效（原版嵌套类；字段名与资产逐字一致）")]
        public ParticleCollisionDefinition particleDefinition = new ParticleCollisionDefinition();

        [Tooltip("两次播放的最小间隔（秒）。0 = 不限制。原版字段名 `delayTimeToPlayAgain`")]
        public float delayTimeToPlayAgain;

        [Tooltip("🔴 **死字段**：本 build 运行时从不读（生效的是 `targetAnchor != null`）。照抄只为与资产逐字段对齐")]
        public bool useTargetAnchor;

        [Tooltip("非空 ⇒ 粒子挂到这个锚点的**本地零位**（特效出现在锚点处，与点击位置无关）")]
        public Transform targetAnchor;

        [Tooltip("冷却计时（运行时；`Update` 每帧减 dt，≤0 就 enabled=false）")]
        public float remainingNotPlayTime;

        /// <summary>自检读口：现在是不是在冷却里（= `enabled` 且 `remainingNotPlayTime > 0`）。</summary>
        public bool CoolingForTest { get { return enabled && remainingNotPlayTime > 0f; } }

        // 原版 `Awake` 全文只有一句：`enabled = false`（`TapHitMaterialDefinition__Awake.c`）。
        // ⇒ 平时**不耗帧**；只有真播过一次、由控制器置 true 之后才每帧减冷却。
        void Awake()
        {
            enabled = false;
        }

        void Update()
        {
            // 原版 `TapHitMaterialDefinition__Update`（11 行）：减 dt，≤0 关掉自己。
            remainingNotPlayTime -= Time.deltaTime;
            if (remainingNotPlayTime <= 0f) enabled = false;
        }

        void OnDestroy()
        {
            // 原版：任意一个定义被销毁 ⇒ 整表清空（见 `ClearCache` 的注释）
            ParticleCollisionDefinition.ClearCache();
        }
    }
}
