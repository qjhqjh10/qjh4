// WarpforgeEffectPlayer.cs — 特效的播放入口 + 生命周期
//
// 没有它，958 个 prefab 进场景就是「不会死的对象」：既没有「播一个特效」的 API，
// 也没有任何人负责销毁。这两件事才是「研究产物 → 游戏资产」的差距所在。
//
// 生命周期语义（从原版 AnimFXController 的签名桩 + 2346 个组件的参数值反推，见下）
// ------------------------------------------------------------------
//   原版字段（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AnimFXController.cs`）：
//     bool  preventDestroy    —— preventDestroy=true 时 destroyTime 带 [HideIf]，即「不由自己管销毁」
//     float destroyTime       —— 多久后自己销毁
//     float exitDestroyTime   —— "Time that will last the effect living until it gets destroy after exit"
//     const float SAFE_DESTROY_TIME = 3f
//
//   数据实测（`数据/游戏数据/animfx_components.json`，990 个实例 / 912 个效果）：
//     destroyTime      中位 3.0，范围 0–17，按效果逐个调过
//     exitDestroyTime  956/990 是 3.0 —— 基本是个常量默认值
//     preventDestroy   97 个实例是 1
//
//   两条时间线**是并行的两条路，不是串起来的**，依据是上面那组分布：
//     · 自己管销毁的（preventDestroy=false）：到 destroyTime 就走一次 Exit()，再留 exitDestroyTime 收尾
//     · 交给外部的（preventDestroy=true）：原版不管，由牌局/技能代码在合适时机调 Exit()
//   因为 exitDestroyTime 几乎是个常量，而 destroyTime 是逐个调过的 —— 若两者是串联的
//   「先活 destroyTime 再活 exitDestroyTime」，你不会看到这种分布。
//
// ⚠️ 这一条是**反推的，不是反编译来的**（Ghidra 工具链没重建）。判错的话实拍一比就露馅：
//    拿原版游戏（`d:/2/unity_run_ref/`，带 SceneJumpShot mod）拍同一个效果，比存活时长即可。
//
// 用法：
//    WarpforgeEffectPlayer.Play("Antimatter Explosion", parent: cardRoot);
//    WarpforgeEffectPlayer.Play("Lightning_Green", cardRoot, scale: 1.5f, autoDestroySeconds: 2f);
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    [DisallowMultipleComponent]
    public class WarpforgeEffectPlayer : MonoBehaviour
    {
        /// <summary>原版常量 SAFE_DESTROY_TIME —— 原版既没有 destroyTime 也没有可算的自然时长时的兜底。
        /// 它保证「不管什么情况都不会留一个永远不死的对象」。</summary>
        public const float SafeDestroyTime = 3f;

        /// <summary>正在播放的实例（用来证「播完真的消失了」，也是 KillAll 的抓手）。</summary>
        static readonly List<WarpforgeEffectPlayer> Active = new List<WarpforgeEffectPlayer>();

        /// <summary>当前活着的特效数。白板测试靠它证「播 → 消失」闭环，不靠肉眼。</summary>
        public static int ActiveCount { get { return Active.Count; } }

        /// <summary>当前所有活着的实例（只读快照，调用方别改）。</summary>
        public static IReadOnlyList<WarpforgeEffectPlayer> ActivePlayers { get { return Active; } }

        /// <summary>全部立刻销毁。切场景 / 退出牌局时兜底用。</summary>
        public static void KillAll()
        {
            for (int i = Active.Count - 1; i >= 0; i--)
                if (Active[i] != null) Active[i].Kill();
            Active.Clear();
        }

        /// <summary>播完时触发（销毁之前）。</summary>
        public event Action OnFinished;

        [Tooltip("自己跟着 Update 推进。外部驱动时（白板 / 批处理自检）关掉，由调用方 Tick —— " +
                 "两边都推会走两倍速")]
        public bool autoTick = true;

        WFEffectEntry _entry;

        float _time;            // 从播放到现在
        float _lifetime;        // 到点调 Exit()；float.PositiveInfinity = 不自动收
        float _exitAt = -1f;    // 调 Exit() 的时刻
        float _exitDelay;       // Exit() 之后再活多久
        bool _exiting;
        bool _destroyed;

        public string EffectName { get { return _entry != null ? _entry.name : name; } }
        public WFEffectEntry Entry { get { return _entry; } }
        public bool IsExiting { get { return _exiting; } }
        public bool IsPlaying { get { return !_exiting && !_destroyed; } }

        /// <summary>预计总存活时长（含 Exit 后的收尾）。不自动收的是 Infinity。</summary>
        public float ExpectedLifetime
        {
            get { return float.IsPositiveInfinity(_lifetime) ? float.PositiveInfinity : _lifetime + _exitDelay; }
        }

        // ---- 播放入口 ----

        /// <summary>按名字播一个特效。名字来自 WarpforgeEffectLibrary（= 原版 prefab 名）。
        /// 找不到时打一条警告并返回 null —— 不抛异常，特效缺失不该让玩法直接崩。</summary>
        public static WarpforgeEffectPlayer Play(
            string effectName,
            Transform parent = null,
            Vector3 localPosition = default(Vector3),
            float scale = 1f,
            float autoDestroySeconds = -1f)
        {
            var lib = WarpforgeEffectLibrary.Instance;
            if (lib == null)
            {
                Debug.LogWarning("[WarpforgeVFX] 没有效果库 —— 先跑菜单 Tools > Warpforge > 生成效果库");
                return null;
            }
            WFEffectEntry entry;
            if (!lib.TryGet(effectName, out entry))
            {
                Debug.LogWarning($"[WarpforgeVFX] 效果库里没有 '{effectName}'");
                return null;
            }
            return Play(entry, parent, localPosition, scale, autoDestroySeconds);
        }

        /// <summary>照着台账判定的名字播 —— 找不到精确名时退化成模糊匹配，并把命中的名字打出来。</summary>
        public static WarpforgeEffectPlayer PlayFuzzy(
            string keyword, Transform parent = null, Vector3 localPosition = default(Vector3),
            float scale = 1f, float autoDestroySeconds = -1f)
        {
            var lib = WarpforgeEffectLibrary.Instance;
            if (lib == null) { Debug.LogWarning("[WarpforgeVFX] 没有效果库"); return null; }
            var hit = lib.Find(keyword);
            if (hit.Count == 0) { Debug.LogWarning($"[WarpforgeVFX] 没有名字含 '{keyword}' 的效果"); return null; }
            if (hit.Count > 1)
                Debug.Log($"[WarpforgeVFX] '{keyword}' 命中 {hit.Count} 个，取最短的 '{hit[0].name}'");
            return Play(hit[0], parent, localPosition, scale, autoDestroySeconds);
        }

        public static WarpforgeEffectPlayer Play(
            WFEffectEntry entry,
            Transform parent = null,
            Vector3 localPosition = default(Vector3),
            float scale = 1f,
            float autoDestroySeconds = -1f)
        {
            if (entry == null)
            {
                Debug.LogWarning("[WarpforgeVFX] Play() 拿到空的 entry");
                return null;
            }
            if (entry.prefab == null)
            {
                // ⚠️ 这条**必须出声**。prefab 引用为空最常见的原因是「重导过 prefab，
                //    但没重建效果库」—— 重导会重新生成 GameObject 的 fileID，
                //    库里的 (guid, fileID) 引用就全废了（GUID 还对得上，所以很难发现）。
                //    修法：跑一次菜单 Tools > Warpforge > 生成效果库。
                Debug.LogWarning($"[WarpforgeVFX] 效果 '{entry.name}' 的 prefab 引用是空的 —— "
                               + "多半是重导过 prefab 之后没重建效果库（Tools > Warpforge > 生成效果库）");
                return null;
            }

            var go = Instantiate(entry.prefab, parent);
            go.name = entry.name;                 // 去掉 Instantiate 加的 "(Clone)"，日志里好认
            var t = go.transform;

            // ⚠️ 位置必须归位，旋转和缩放**不能动**。三个值分别有实测依据：
            //   · localPosition：原版有一批效果把根节点停在**场外 x≈100**（"后台"位置，
            //     Unity 里常见的做法）。实测 Atk_GrotGrenade / Atk_ExileGlaiveThrow Movement
            //     根节点在 (99.59, 1.24, -0.37)、Attack_Stomp 在 (100, 0.725, 0)。
            //     不归位的话，这些效果直接播在场景外面 —— 看起来就像「什么都没渲染」。
            //   · localRotation：**是效果作者定的一部分**，不是噪音。实测 Antimatter Explosion
            //     根节点是 X=-90°。归零会把一批效果整体转 90°。
            //   · localScale：同理（Atk_GrotGrenade 是 0.5）。调用方传的 scale 是**乘数**，
            //     乘在作者设定的缩放之上；传 1（默认）就原样不动。
            t.localPosition = localPosition;
            if (!Mathf.Approximately(scale, 1f)) t.localScale = t.localScale * scale;

            var p = go.GetComponent<WarpforgeEffectPlayer>();
            if (p == null) p = go.AddComponent<WarpforgeEffectPlayer>();
            p.Init(entry, autoDestroySeconds);
            return p;
        }

        void Init(WFEffectEntry entry, float autoDestroySeconds)
        {
            _entry = entry;

            // 材质重建：play 模式下由 binder 的 Awake 干了，编辑器里 Awake 不跑（工具/白板要手动）
            var binder = GetComponent<WarpforgeEffectBinder>();
            if (binder != null && !Application.isPlaying) binder.Apply();

            _exitDelay = entry.exitDestroyTime > 0f ? entry.exitDestroyTime : 0f;

            if (autoDestroySeconds >= 0f) _lifetime = autoDestroySeconds;            // 调用方说了算
            else if (entry.preventDestroy) _lifetime = float.PositiveInfinity;       // 原版不让它自己死
            else _lifetime = entry.AutoLifetime();

            foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Clear(true);
                ps.Play(true);
            }

            Active.Add(this);
        }

        void Update()
        {
            if (!autoTick) return;
            // Time.deltaTime（受 Time.timeScale 影响）：特效节奏跟着游戏走才对
            Tick(Time.deltaTime);
        }

        /// <summary>推进。Update 调它；批处理/测试也直接调它 —— 这样不用进 play 模式就能确定性地验生命周期。</summary>
        public void Tick(float dt)
        {
            if (_destroyed) return;
            _time += dt;

            if (!_exiting)
            {
                if (_time >= _lifetime) Exit();
            }
            if (_exiting && _time - _exitAt >= _exitDelay)
            {
                DestroyNow();
            }
        }

        /// <summary>停止发射、进入收尾（对应原版 AnimFXController.Exit）。
        /// 收尾时长 = 原版 exitDestroyTime（默认 3s：让已经飞出去的粒子自然死掉）。</summary>
        public void Exit()
        {
            if (_destroyed || _exiting) return;
            _exiting = true;
            _exitAt = _time;

            // StopEmitting（不是 StopEmittingAndClear）：现有粒子继续飘完，看起来才不像「啪一下没了」
            foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
                if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        /// <summary>立刻销毁。</summary>
        public void Kill()
        {
            if (_destroyed) return;
            _exiting = true;
            DestroyNow();
        }

        void DestroyNow()
        {
            if (_destroyed) return;
            _destroyed = true;
            Active.Remove(this);
            var cb = OnFinished;
            OnFinished = null;
            if (cb != null) cb();

            // ⚠️ 不销毁材质。binder 重建出来的材质走的是**静态共享缓存**
            //    （WarpforgeEffectBinder.Cache，键是「材质名|原 shader 名」），
            //    在这里 Destroy(material) 会把同时在场的别的实例一起打成粉红。
            //    缓存本身是有界的（条目数 = 不同材质的数量，不随播放次数增长）。
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        void OnDestroy()
        {
            // 手工 Destroy 掉整个对象（比如切场景）时也要从表里摘干净，否则 ActiveCount 会虚高
            if (!_destroyed)
            {
                _destroyed = true;
                Active.Remove(this);
            }
        }

        /// <summary>诊断用：把当前活着的实例列出来（谁没被销毁一目了然）。</summary>
        public static string DescribeActive()
        {
            if (Active.Count == 0) return "没有活着的特效";
            var sb = new System.Text.StringBuilder($"活着的特效 {Active.Count} 个：");
            foreach (var p in Active)
                if (p != null) sb.Append($"\n  {p.EffectName}  存活 {p._time:F2}s / 预计 {Describe(p.ExpectedLifetime)}");
            return sb.ToString();
        }

        static string Describe(float v)
        {
            return float.IsPositiveInfinity(v) ? "不自动收" : $"{v:F2}s";
        }
    }
}
