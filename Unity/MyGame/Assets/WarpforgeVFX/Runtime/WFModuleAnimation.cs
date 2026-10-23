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
// ✅ **2026-10-23（A1 R24）已还原「动画事件通道」**：`AnimationExitEnd` 由 `ModuleTick` 在
//   **Exit clip 播完**（按 `clip.length` 数）时调用 —— 原版是 Exit clip 的动画事件调的
//   （块2 §2 标注为推断；这里用「时长到点」做等价替代，⛔ 不要给 `Default` 也上膛）。
//   判据 → `资料/普查产出_1022/A1_自造与兜底_特效动画.md` R24。
//
// 🔴 未还原（只剩一条，在 Exit 时打一次性警告，不静默）：
//   ① ~~**动画事件通道**~~ —— ✅ 见上，2026-10-23 已接。
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

        /// <summary>**播不出东西**的次数（自检用 —— 见文件头「未还原 ②」）。
        /// 🆕 2026-10-23（A1）：现在有两处会加 —— ① `Exit()` 播 `Exit` 失败；
        ///   ② `EnsureClips()` 里**别名一条都取不到**（见下）。两处都意味着「这个模块不会按原版动」。</summary>
        public static int PlayFailures;
        /// <summary>🆕 2026-10-23（A1）：**片段别名取不到**的次数（`<效果名>|Default` 与 `<效果名>|Exit` 都没命中）。
        /// 单开一格是因为它和「`Exit` 播失败」根因不同（别名拼错 / 表没登记 / 效果名带 `(Clone)`），
        /// 混在一个数里就分不出来 —— 而这次 R25/R24 之前，这种失败是**完全静默**的。</summary>
        public static int ClipAliasMisses;
        public static void ResetDiagnostics() { PlayFailures = 0; ClipAliasMisses = 0; }

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

            // 🔴 2026-10-23（A1）：这两条失败以前**完全静默**（`return` 之后一路掉到
            //    「Animator 没有 controller / 没有 clip」，现象像「这个效果本来就没动画」）。
            //    走到 `EnsureClips()` 的模块**一定**有 `simpleAnimation` 引用（`Initialize` 在
            //    `simpleAnimationRef` 为空时第 78 行就 return 了）⇒ 别名取不到**一定是缺陷**，不是正常态。
            var effName = EffectName;                       // ⚠️ 只取一次：它可能带一次「没走效果库」的警告
            if (string.IsNullOrEmpty(effName))
            {
                AliasMiss("拿不到效果名（`EffectName` 为空 —— 模块在 `Initialize` 之前被读了？）");
                return;
            }

            var def = WarpforgeAnimatorBridge.TryGetClip(effName + "|Default");
            var exit = WarpforgeAnimatorBridge.TryGetClip(effName + "|" + ExitState);
            if (def == null && exit == null)
            {
                AliasMiss($"片段别名 `{effName}|Default` 与 `{effName}|{ExitState}` **一条都取不到**" +
                          "（这张 `SimpleAnimation` 状态表没登记，或**效果名拼错了**）");
                return;
            }
            _hasClips = true;
            if (def != null) PlayClip(def);               // 原版 `m_PlayAutomatically = 1`（实读 5/5）
        }

        /// <summary>🆕 2026-10-23（A1）：**「片段别名取不到」的统一出口** —— 计入 `ClipAliasMisses`
        /// **和** `PlayFailures`（后者与 `Exit()` 那条失败同口径：都表示「这个模块不会按原版动」），
        /// 并按实例只报一次（`_warned`），把根因写进 `_failReason` 供后续 `Exit()` 的告警复用。</summary>
        void AliasMiss(string why)
        {
            ClipAliasMisses++;
            PlayFailures++;
            if (_warned) return;
            _warned = true;
            _failReason = why;
            Debug.LogWarning($"🔴 未还原：《{EffectName}》的 AnimFXModuleAnimation —— {why} ⇒ 这个模块不会播原版片段。" +
                             "（原版用的是 AssetStore 的 `SimpleAnimation` 状态表；我们靠 " +
                             "`工具/gen_simple_animation_clips.py` 登记的 `<效果名>|<状态名>` 别名取回。）");
        }

        bool _hasClips;
        Animator _animator;
        PlayableGraph _graph;

        // 🆕 2026-10-23（A1 R24）：**动画事件通道** —— 原版 `AnimationExitEnd` 由 Exit clip 的
        //   动画事件调（块2 §2 推断）；我们没有事件层，改成「**按 clip 真实时长数到结束**再调」。
        //   ⚠️ 只对 **Exit 这条** 上膛（`Default` 是待机，播完不能销毁）。
        AnimationClip _playing;
        float _elapsed;
        bool _endOnFinish, _finished;

        /// <summary>用 Playables 播一条片段（指向本节点的 `Animator`；它有没有 controller 都行）。</summary>
        /// <param name="endDestroys">true = 这条是 Exit，播完要调 `AnimationExitEnd()`（原版动画事件）。</param>
        bool PlayClip(AnimationClip clip, bool endDestroys = false)
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
            _playing = clip;
            _elapsed = 0f;
            _endOnFinish = endDestroys;
            _finished = false;
            return true;
        }

        /// <summary>数 Exit clip 的时长，到点就调原版的 `AnimationExitEnd()`（动画事件的等价物）。</summary>
        public override void ModuleTick(float dt)
        {
            if (!_endOnFinish || _finished || _playing == null) return;
            _elapsed += dt;
            if (_elapsed < _playing.length) return;
            _finished = true;
            AnimationExitEnd();
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
        /// 🆕 2026-10-23（A1 R24）：触发源已接 —— `ModuleTick` 按 Exit clip 的真实时长数到结束就调它
        /// （原版由 clip 的动画事件调；我们没有事件层，用「时长到点」等价替代）。</summary>
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
                if (clip != null && PlayClip(clip, state == ExitState)) return true;
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
