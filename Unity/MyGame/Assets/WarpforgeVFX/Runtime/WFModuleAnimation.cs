// WFModuleAnimation.cs — 原版 `AnimFXModuleAnimation` 的对应物
//   **5 实例**（全是 `ShieldTraitEffect_Idle` 系列：本体 + Emperor's Children / Necron / Sororitas / TAU 变体）
//
// 语义出处：`资料/AnimFX_18类方法体_块2.md` **§2**（3 个方法体：`.ctor` 空 / `Exit` / `AnimationExitEnd`）
//   · `Exit()`             → `simpleAnimation.Play("Exit")`；状态名是**硬编码常量** `EXIT_STATE = "Exit"`
//                            （字符串表核过：`0x1842ae790 → "Exit"`）
//   · `AnimationExitEnd()` → `Object.Destroy(gameObject)` —— **立即、无延时、销毁整个 GameObject**
//   · `Initialize` **没有重写**（这个类在 Initialize 时机不做事）
//   ⚠️ `AnimationExitEnd` 在全树里**查不到调用点** ⇒ 块2 判它是给「Exit 那个 clip 的动画事件」用的
//      （**推断，不是读到的**）。它的名字就是这个意思。
//
// 照方法体还原了什么：
//   · Exit 时机播硬编码状态名 `"Exit"`
//   · `AnimationExitEnd()` 的语义（立刻销毁整个 GameObject）做成**公开方法**，等动画事件来接
//
// 我们定的（显式标出）：
//   · 原版调的是 AssetStore 的 `SimpleAnimation`（**我们没有那个插件**，全工程搜不到这个类）
//     ⇒ 退到块2 §2 给的等价物：`Animator.Play("Exit")` 或 legacy `Animation.Play("Exit")`。
//     顺序是**先 Animator 后 Animation**（我们的 prefab 上两种都出现过）。
//   · 解析 `simpleAnimation` 时**只按节点解析、不按组件**：原版字段类型是 `SimpleAnimation`，
//     而数据里的引用是 `@node:MonoBehaviour:ShieldTraitEffect_Idle` —— 那个节点上
//     **第一个 MonoBehaviour 是 `WarpforgeEffectBinder`**，`GetComponent<MonoBehaviour>()`
//     会拿到它 ⇒ 必须 `ResolveNode<Transform>` 拿节点，再在节点上找 Animator / Animation。
//   · 销毁同样走「play → `Object.Destroy` / 否则 `DestroyImmediate`」（批处理没有帧末）。
//
// 🔴 未还原（两条，都在 Exit 时打一次性警告，不静默）：
//   ① **动画事件通道**：`AnimationExitEnd` 原版由 Exit clip 的动画事件调（块2 标注为推断）。
//      我们工程没有动画事件层 ⇒ 只能留一个公开方法等接线（与 `WFModuleScreenShake.AnimEventDoShake`
//      同款处置：方法在、触发源不在）。
//   ② **Exit 动画本体**：2026-09-18 实测我们导出的 prefab —— `ShieldTraitEffect_Idle.prefab:24464`
//      的 `Animator` **`m_Controller` 是 0**（没有控制器），而全工程 **0 个 `.anim` / 0 个 `.controller`**
//      ⇒ 现在的 `Play("Exit")` 大概率**播不出任何画面**。这要补导出侧（`EffectExporter` 不管
//      AnimationClip / AnimatorController），不是本模块能补的。
using UnityEngine;

namespace WarpforgeVFX
{
    [WFModuleKind("AnimFXModuleAnimation")]
    public class WFModuleAnimation : WFEffectModule
    {
        /// <summary>原版 `const EXIT_STATE`（硬编码，不从数据来）。</summary>
        public const string ExitState = "Exit";

        [Tooltip("原版 `simpleAnimation` 的引用（`@node:MonoBehaviour:…`）。我们只取它所在的节点")]
        public string simpleAnimationRef = "";

        [Tooltip("运行时解析出来的动画来源节点。空 = 退回本组件所在的节点")]
        public Transform animationNode;

        /// <summary>Exit 时播不出东西的次数（自检用 —— 见文件头「未还原 ②」）。</summary>
        public static int PlayFailures;
        public static void ResetDiagnostics() { PlayFailures = 0; }

        bool _warned;
        string _failReason = "";

        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);
            if (def != null) simpleAnimationRef = def.GetString("simpleAnimation", simpleAnimationRef);
        }

        public override void Initialize(WarpforgeEffectPlayer controller)
        {
            base.Initialize(controller);
            if (string.IsNullOrEmpty(simpleAnimationRef)) return;
            // ⚠️ 用 `Transform` 当类型参数：只验引用形状 + 解析路径，不挑组件
            //    （那个节点上的第一个 MonoBehaviour 是我们的 binder，不是 SimpleAnimation）
            animationNode = ResolveNode<Transform>(transform, simpleAnimationRef, "AnimFXModuleAnimation");
        }

        /// <summary>原版 `Exit()`：播硬编码状态名 `"Exit"`。播不了就**说出来**（文件头「未还原 ②」）。</summary>
        public override void Exit()
        {
            if (Play(ExitState)) return;

            PlayFailures++;
            if (_warned) return;
            _warned = true;
            Debug.LogWarning($"🔴 未还原：《{EffectName}》的 AnimFXModuleAnimation 要播 `{ExitState}`，" +
                             $"但播不出来 —— {_failReason}。" +
                             "（原版调的是 AssetStore 的 SimpleAnimation，我们没有那个插件；" +
                             "退到 Animator/legacy Animation 又碰上「没有 controller / 没有 clip」）");
        }

        /// <summary>原版 `AnimationExitEnd()`：**立即**销毁整个 GameObject（无延时）。
        /// ⚠️ 原版这条是给 Exit clip 的**动画事件**用的（块2 §2 标注：调用点查不到 ⇒ 推断）——
        ///    我们没有动画事件层，所以它**不会自己触发**，留在这里等接线。</summary>
        public void AnimationExitEnd()
        {
            if (this == null) return;
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        /// <summary>播一个状态名。先 Animator、后 legacy Animation（我们定的顺序，见文件头）。</summary>
        bool Play(string state)
        {
            var node = animationNode != null ? animationNode : transform;
            if (node == null) { _failReason = "连节点都没有"; return false; }

            var animator = node.GetComponent<Animator>();
            if (animator != null)
            {
                if (animator.runtimeAnimatorController != null)
                {
                    animator.Play(state);            // 原版等价物（块2 §2）
                    return true;
                }
                _failReason = $"`{node.name}` 上的 Animator **没有 AnimatorController**";
                // 同一个节点上可能还挂着 legacy Animation，继续往下试
            }

            var anim = node.GetComponent<Animation>();
            if (anim != null)
            {
                if (anim.GetClip(state) == null)
                {
                    _failReason = (string.IsNullOrEmpty(_failReason) ? "" : _failReason + "；")
                                + $"`{node.name}` 的 Animation 里**没有叫 `{state}` 的 clip**";
                    return false;
                }
                if (anim.Play(state)) return true;
                _failReason = (string.IsNullOrEmpty(_failReason) ? "" : _failReason + "；")
                            + $"`Animation.Play(\"{state}\")` 返回 false";
                return false;
            }

            if (string.IsNullOrEmpty(_failReason))
                _failReason = $"`{node.name}` 上既没有 Animator 也没有 legacy Animation";
            return false;
        }
    }
}
