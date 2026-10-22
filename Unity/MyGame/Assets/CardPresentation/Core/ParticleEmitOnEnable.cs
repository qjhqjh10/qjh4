// ParticleEmitOnEnable.cs — 原版同名组件的移植（我们全工程原来 0 命中）。
//
// 判据（一手）：`d:/2/tools/decomp_full/ParticleEmitOnEnable__OnEnable.c`
//   = `if (!ps.isPlaying) ps.Play();`（`ps` = 同节点上的 `ParticleSystem`）。
// 它挂在 `Invoke Minion Card Fade Default` 的 **`Trail`** 子节点上（`Trail` 的
// `rateOverTime = 0`、`stopAction = Disable`）⇒ **不补这个组件，`Trail` 就一发粒子都不出**：
//   · `Card Hand To Board` 在 **0.55** 把 `Trail` 那个 GameObject `SetActive(true)`；
//   · 没有本组件时：`playOnAwake` 只在**实例化那一刻**生效，而实例化时节点是**关着**的
//     ⇒ 之后每次 `SetActive(true)` 都不会起播；
//   · 有本组件时：每次 `OnEnable` 都 `Play()` ⇒ 与 clip 的开关对上。
// 判据与整条链 → `资料/普查产出_1022/W64_B04_ToBoardFX与粒子.md` §五·5·3 / §八·3。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>节点被打开时把同节点的 <see cref="ParticleSystem"/> 起播（原版 `ParticleEmitOnEnable`）。
    /// ⚠️ 已经 `isPlaying` 时**不**重复 `Play()`（原版就是 `if (!ps.isPlaying)`）。</summary>
    [DisallowMultipleComponent]
    public class ParticleEmitOnEnable : MonoBehaviour
    {
        ParticleSystem _ps;

        /// <summary>自检口：`OnEnable` 里**真的调过几次 `Play()`**。
        /// 🔴 2026-10-22：批处理（edit mode）下 `ParticleSystem.Play()` **不一定**立刻把
        /// `isPlaying` 置真、`Simulate` 也只在「已经在播」时推进 ⇒ **`isPlaying`/`particleCount`
        /// 在自检环境里观测不到**。调用计数是唯一与引擎状态无关的观测点（⛔ 不是放宽：
        /// 组件没挂 / 没进 `OnEnable` / 早退不调 `Play` ⇒ 计数恒 0 ⇒ 断言照红）。</summary>
        public int PlayCalls { get; private set; }

        /// <summary>自检口：`OnEnable` **进过几次**（用来区分「组件没挂 / 生命周期没跑」与
        /// 「跑进去了但 `isPlaying` 早退」——这两种在自检里长得一样）。</summary>
        public int OnEnableCalls { get; private set; }

        void Awake() { _ps = GetComponent<ParticleSystem>(); }

        void OnEnable() { OnEnableCalls++; Fire(); }

        /// <summary>原版 `OnEnable` 的**全部内容**（`ParticleEmitOnEnable__OnEnable.c` 亲读）：
        /// `if (ps == null) &lt;抛&gt;; if (ps.isPlaying) return; ps.Play();`
        /// —— **没有 `Emit(n)`、没有 `withChildren`、没动 `emission`**。
        /// <para>单独开成 public 只为自检能**确定性地**验这一句（edit mode 下 `AddComponent` 到未激活对象
        /// 再 `SetActive(true)` 的 `OnEnable` 触发在本宿主里实测**没发生** —— 见 `OnEnableCalls`）。
        /// ⛔ 产品路径只经 `OnEnable`，别在别处调它。</para></summary>
        public void Fire()
        {
            if (_ps == null) _ps = GetComponent<ParticleSystem>();
            if (_ps == null)
            {
                // ⛔ 不静默：这个组件挂错节点 = 特效永远不动
                Debug.LogWarning($"[ParticleEmitOnEnable] `{name}` 上没有 `ParticleSystem` ⇒ 本组件不起作用");
                return;
            }
            if (!_ps.isPlaying) { _ps.Play(); PlayCalls++; }
        }
    }
}
