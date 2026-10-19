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
//   ② ~~**Exit 动画本体**~~ —— ✅ **2026-10-19（T12 / `A1729`）已修**：
//      原来我们导出的 prefab 上 `Animator.m_Controller = 0`、全工程 0 个 `.anim`/`.controller` ⇒ 播不出。
//      修法**不走「把 clip 落成工程资产」**（那条路实验已证死：运行时格式，拷出来是空壳）——
//      走**原版 shader/控制器同一条路**：把原版两条片段收进 `StreamingAssets/WarpforgeVFX/
//      wf_prefabs_extra.bundle`，按容器别名 `<效果名>|<状态名>` 取回，用 **Playables** 播。
//      🔴 **为什么不是 legacy `Animation`**：这两条片段实读 `m_Legacy = False`（muscle 格式），
//        `Animation` 组件只认 `legacy = true` 的片段 ⇒ 用 `AnimationClipPlayable` + `AnimationPlayableOutput`
//        指向同一个 `Animator`（它没有 controller 也能当输出目标）。
//      （判据链 = 原版 `SimpleAnimation.m_States` 实读：`Default→Shield Enter` · `Exit→Shield Exit`，
//      5 个 `ShieldTraitEffect_Idle*` 全同；表由 `工具/gen_simple_animation_clips.py` 生成。）
//      ⚠️ 这条只用别名解析得到的那 5 个效果；别的效果行为不变。
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

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
            // 🆕 2026-10-19（T12 / `A1729`）：把原版 `SimpleAnimation` 的「状态名 → clip」接回来。
            EnsureClips();
        }

        /// <summary>🆕 2026-10-19（T12 / `A1729`）：**从重打的小包按别名取回原版片段**并播 `Default`。
        ///
        /// 判据链：原版 `AnimFXModuleAnimation` 的 `simpleAnimation` 字段指向一个 **`SimpleAnimation`**
        ///   组件（AssetStore 插件，我们没有那个类）；它的序列化数据是
        ///   `m_States = [{name:'Default', clip:'Shield Enter'}, {name:'Exit', clip:'Shield Exit'}]`
        ///   + `m_PlayAutomatically = 1`（实读 5 个 `ShieldTraitEffect_Idle*`）。
        ///   ⇒ `工具/gen_simple_animation_clips.py` 扫出这张表 → `extract_missing_shaders.py --prefabs`
        ///   把两条 clip 收进 `wf_prefabs_extra.bundle` 并登记容器别名 `<效果名>|<状态名>`。
        ///
        /// ⚠️ **只有别名解析得到时才建 PlayableGraph**（别的效果没有这张表 ⇒ 行为与改前一致、不新增组件）。
        /// ⚠️ 片段是 **非 legacy**（`m_Legacy = False`，实读）⇒ 走 Playables，**不走** legacy `Animation`。</summary>
        void EnsureClips()
        {
            var node = animationNode != null ? animationNode : transform;
            if (node == null) return;
            _animator = node.GetComponent<Animator>();
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_animator == null) return;

            var def = WarpforgeAnimatorBridge.TryGetClip(EffectName + "|Default");
            var exit = WarpforgeAnimatorBridge.TryGetClip(EffectName + "|" + ExitState);
            if (def == null && exit == null) return;      // 这个效果没有 SimpleAnimation 表 ⇒ 保持旧行为
            _hasClips = true;
            if (def != null) PlayClip(def);               // 原版 `m_PlayAutomatically = 1`（实读 5/5）
        }

        bool _hasClips;
        Animator _animator;
        PlayableGraph _graph;

        /// <summary>用 Playables 播一条片段（指向本节点的 `Animator`；它有没有 controller 都行）。</summary>
        bool PlayClip(AnimationClip clip)
        {
            if (clip == null) return false;
            if (_animator == null)
            {
                var node = animationNode != null ? animationNode : transform;
                if (node != null) _animator = node.GetComponent<Animator>();
                if (_animator == null) _animator = GetComponent<Animator>();
            }
            if (_animator == null) { _failReason = "节点上没有 Animator，Playables 没有输出目标"; return false; }

            if (_graph.IsValid()) _graph.Destroy();
            _graph = PlayableGraph.Create("WFModuleAnimation");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var p = AnimationClipPlayable.Create(_graph, clip);
            p.SetApplyFootIK(false);
            var o = AnimationPlayableOutput.Create(_graph, "WFModuleAnimation", _animator);
            o.SetSourcePlayable(p);
            _graph.Play();
            return true;
        }

        void OnDestroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
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

        /// <summary>播一个状态名。**先 Playables（按别名取回的原版片段）**、后 Animator、再 legacy Animation。</summary>
        bool Play(string state)
        {
            var node = animationNode != null ? animationNode : transform;
            if (node == null) { _failReason = "连节点都没有"; return false; }

            // 🆕 2026-10-19（T12 / `A1729`）：原版片段那条路优先（`SimpleAnimation.m_States` 的语义）。
            if (_hasClips)
            {
                var clip = WarpforgeAnimatorBridge.TryGetClip(EffectName + "|" + state);
                if (clip != null && PlayClip(clip)) return true;
            }

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
