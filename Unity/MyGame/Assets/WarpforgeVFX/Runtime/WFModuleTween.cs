// WFModuleTween.cs — 原版 `AnimFXModuleTween` 的对应物（**138 实例 / 138 效果**）
//
// 语义出处（**照方法体写，不照名字猜**）：
//   · `资料/AnimFX_18类方法体_块1.md` §5「AnimFXModuleTween ‖ 单位补间（DOTween Sequence）」—— 逐方法体读解；
//     字段与实例数见 `资料/AnimFX_18类成表.md` §三。
//   · 本轮又对着**反编译原文**逐行核了一遍（结论一致），原文在：
//     `d:/2/Warpforge_tools/data/decomp_il2cpp_0827/decomp_out_ai/AnimFXModuleTween__{Initialize,PlayAnim,
//      PlayAnimCoroutine,OnDisable,.ctor}.c`
//     + `d:/2/tools/decomp_animfx_nested/AnimFXModuleTween.PlayAnimCoroutine_d__7__MoveNext.c`（协程体）。
//
// 原版方法体（下面每一句都是对着它写的）：
//   · `.ctor`          → `*(u16*)(0x40) = 0x101` ⇒ `playOnEnable = true` · `triggerOnlyOnce = true`
//   · `Initialize`     → `base.Initialize`；`if (playOnEnable /*0x40*/) StartCoroutine(PlayAnimCoroutine())`
//   · `PlayAnim()`     → 同一件事（建协程状态机 + `StartCoroutine`）—— **唯一的公开入口**
//   · `d__7.MoveNext`  → `if (!alreadytrigger /*0x42*/) { alreadytrigger = true; 下标 = 0;
//                          while (下标 < tweenAnims.Length) { var so = tweenAnims[下标];
//                            var seq = so.BuildSequence(controller.actingCard, controller.targetCard, null);
//                            if (so.waitAnimation) yield return seq.WaitForCompletion();
//                            下标++; } }`
//                        （反编译里 `tweenAnims` 被捕获进 `<>7__wrap1`、下标存在 `param_1+0x30`，
//                          resume 走 `state == 1` 那条支 ⇒ 与上面这个循环等价）
//   · `OnDisable`      → `*(0x42) = 0` ⇒ `alreadytrigger = false`（**这才是那个门闩的复位**）
//   · 🔴 `triggerOnlyOnce`（`0x41`）在 5 个方法体里**一次都没被读过** —— 原版真正的门闩是 `alreadytrigger`。
//     我们**只把它当数据留痕**（Inspector 里看得见原值），**不拿它做判断**：数据里 101/138 是 1，
//     若拿它当开关，一批效果的行为会变掉。
//
// ✅ 还原了什么
//   ① **数据侧**：`tweenAnims` 的每一条与先后顺序 · `playOnEnable` · `alreadytrigger` 门闩（含 `OnDisable` 复位）。
//   ② **调用侧**：`playOnEnable` 时的自动起播 · `PlayAnim()` 公开入口 · **「播到下一条」的顺序语义**
//      （这条要不要等，由 `UnitTweenSO.waitAnimation` 决定，见下面的钩子）。
//   ③ **门闩是「整串只跑一遍」**：`alreadytrigger` 置 1 之后再 `PlayAnim()` 什么也不做（原版如此）。
//
// ⚠️ 我们自己定的（显式标出）
//   · **不用协程，改事件驱动**：原版 `StartCoroutine` 的**第一帧是同步跑到第一个 `yield`**，
//     所以我们也在 `Initialize` 里同步起播（同构）；「等播完再续」那一步用下游回调。
//     理由：**批处理下没有帧循环**（工程约定）—— 协程在那条路上永远不推进，等于静默失效。
//   · **起播时机放 `Initialize`，不放 `OnEnable`**：`AddComponent` 会**先**触发 `OnEnable`，
//     那时字段还没被 `Configure` 填 ⇒ 那样会用空数据起播（与 `WFModuleScreenShake` 同一条理由）。
//   · **续跑的时机**：原版 `yield` 恢复最早也在**下一帧**；我们同构 —— 把续跑排到下一次
//     `ModuleTick`，**不在回调里递归**（见 `Pump()` / `MarkFinished()`）。
//   · **`DoDestroy()` 里也作废在飞的等待**：原版靠「Unity 销毁协程」达到同样效果，我们显式写出来
//     （`Kill()` 那条路不走 `Exit`，回调可能还在飞）。**这是我们加的，原版没有这个重写**。
//
// 🔴 未还原（这一半我们这边没有）
//   · **真正的补间本身**。原版 `UnitTweenSO.BuildSequence`
//     （`d:/2/tools/decomp_animfx_nested/UnitTweenSO__BuildSequence.c`：`DOTween.Sequence()` →
//      逐条 `TweenInfoBase.GetTween(source, target)` → 按 `muted`/`appendType` 决定 `Append` 还是 `Join`
//      → `useCallBack` 时 `InsertCallback`）与 6 个 tween 子类（`PunchTween` 32 · `ResetBodyTween` 11 ·
//     `RotateTween` 6 · `ShakeTween` 4 · `ScaleTween` 3 · `MoveTween` 3；数据在 `数据/游戏数据/tween/*.json`
//     74 个、被引用 24 个）—— **我们还没有运行时那份数据表**。
//     ⇒ 这里把「该播哪一条」路由到静态钩子 `OnInvoke`（照 `WFModuleScreenShake.OnShake` 的写法）。
//   · **`waitAnimation` 的判断**：它是 `UnitTweenSO` 的字段（`so+0x18`；24 个里 6 个为 1），我们读不到
//     ⇒ 由下游在 `OnInvoke` 的**返回值**里回答（只有它见过那份数据）。
//   · **动画事件通道**：`PlayAnim()` 在原版是给**动画片段事件**调的（零代码调用点，见块1 §附D）——
//     我们**还没有动画事件层** ⇒ `playOnEnable = 0` 的那 **84/138** 个实例现在**没人会调它**。
//     要接就接在牌局时序上（`BattleDriver.PlaySignal` 那条链），别另外发明一套。
using System;
using UnityEngine;

namespace WarpforgeVFX
{
    [WFModuleKind("AnimFXModuleTween")]
    public class WFModuleTween : WFEffectModule
    {
        /// <summary>一条「该播哪张 UnitTweenSO」的请求（= 原版 `tweenAnims[i]` + 当时的上下文）。</summary>
        [Serializable]
        public struct TweenRequest
        {
            /// <summary>UnitTweenSO 的**资产名**（数据里是 `@asset:MonoBehaviour:<名字>`，这里已剥掉前缀）。</summary>
            public string asset;

            /// <summary>在 `tweenAnims` 里的下标（0 起）。</summary>
            public int index;

            /// <summary>`tweenAnims` 的条数（诊断用）。</summary>
            public int count;

            /// <summary>哪个效果（诊断用）。</summary>
            public string effect;

            /// <summary>原版喂给 `BuildSequence` 的是 `controller.actingCard` / `targetCard`；
            /// 我们的 `WarpforgeEffectPlayer` **没有**这两个引用 ⇒ 只带得过去 `IsRetaliation`
            /// 这一条牌局信息，**卡由下游按当前对局自己取**（别在这儿猜）。</summary>
            public bool retaliation;
        }

        /// <summary>
        /// 🔑 **下游钩子**：表现层挂这里（我们这边该接 `CardPresentation` 那层的 UnitTween 数据表 +
        /// 6 个 tween 子类）。
        /// 返回值 = **这条要不要等它播完再播下一条**（原版 `UnitTweenSO.waitAnimation`）；
        /// 返回 `true` 时**必须**在播完时调 `onFinished`，否则后面的补间永远不会播
        /// （挂住的状态记在 `WaitingCount` 里，自检看它有没有降回去）。
        /// </summary>
        public delegate bool TweenInvoker(TweenRequest req, Action onFinished);

        /// <summary>下游。**没挂 = 整条不播**（计数 + 警告，不静默）。</summary>
        public static TweenInvoker OnInvoke;

        /// <summary>「要播但没人接 / 条目是空的」的次数（**按条计**）。自检看它是不是 0。</summary>
        public static int DroppedRequests;

        /// <summary>当前挂在「等下游播完」状态上的模块数。**只涨不落 = 下游忘了回调**。</summary>
        public static int WaitingCount;

        static int _warnedDrops;

        [Tooltip("原版 `tweenAnims:UnitTweenSO[]` —— 元素填 UnitTweenSO 的**资产名**（`@asset:` 引用串也认）")]
        public string[] tweenAnims = new string[0];

        [Tooltip("原版 `playOnEnable`：`Initialize` 时就播（.ctor 默认 true）")]
        public bool playOnEnable = true;

        [Tooltip("🔴 原版 `triggerOnlyOnce` —— **方法体里一次都没读过，是死字段**。" +
                 "这里只做数据留痕（Inspector 里看得见原值），**不参与任何判断**。")]
        public bool triggerOnlyOnce = true;

        [Tooltip("原版非序列化字段 `alreadytrigger`：**真正的门闩**（首次播前置 1，`OnDisable` 复位）。 " +
                 "公开出来是为了自检能断言它。")]
        public bool alreadytrigger;

        // ---- 链子状态（原版都藏在协程状态机 d__7 里：`<>7__wrap1` = 数组、+0x30 = 下标、state 0/1）----
        int _next;
        bool _running;
        bool _waiting;
        bool _resumeRequested;
        bool _dropCounted;

        /// <summary>正在等下游回调（诊断用）。</summary>
        public bool IsWaiting { get { return _waiting; } }

        /// <summary>自检用：把两个计数清零。</summary>
        public static void ResetDiagnostics()
        {
            DroppedRequests = 0;
            WaitingCount = 0;
            _warnedDrops = 0;
        }

        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);
            if (def == null) return;
            playOnEnable = def.GetBool("playOnEnable", playOnEnable);
            triggerOnlyOnce = def.GetBool("triggerOnlyOnce", triggerOnlyOnce);   // 只留痕，不判断（见文件头）
            var list = def.GetList("tweenAnims");
            tweenAnims = new string[list.Count];
            for (int i = 0; i < list.Count; i++) tweenAnims[i] = AssetName(list[i]);
        }

        public override void Initialize(WarpforgeEffectPlayer controller)
        {
            base.Initialize(controller);
            // 原版：`if (playOnEnable) StartCoroutine(PlayAnimCoroutine())`
            if (playOnEnable) StartChain();
        }

        /// <summary>原版 `PlayAnim()` —— **唯一的公开入口**（动画事件 / 代码调）。</summary>
        public void PlayAnim() { StartChain(); }

        /// <summary>每帧只做一件事：把「等待中」的链子往下推。
        /// （原版没有这一步 —— 它是协程自己 resume；见文件头「续跑的时机」。）</summary>
        public override void ModuleTick(float dt)
        {
            if (_running && _resumeRequested) Pump();
        }

        // ---- 链子本体：`StartChain` = `PlayAnimCoroutine` 的 MoveNext；`Pump` = 那个 while 循环 ----

        void StartChain()
        {
            if (alreadytrigger) return;        // 原版 `if (!alreadytrigger)` 的反面
            alreadytrigger = true;
            _next = 0;
            _running = true;
            _resumeRequested = true;
            Pump();
        }

        void Pump()
        {
            _resumeRequested = false;
            while (_running && _next < tweenAnims.Length)
            {
                int idx = _next;
                string asset = tweenAnims[idx];
                _next = idx + 1;

                if (OnInvoke == null)
                {
                    Drop(asset, idx, "没有下游接（`WFModuleTween.OnInvoke` 是空的）");
                    return;
                }
                if (string.IsNullOrEmpty(asset))
                {
                    Drop("(空)", idx, "`tweenAnims[" + idx + "]` 是空的");
                    return;
                }

                var req = new TweenRequest
                {
                    asset = asset,
                    index = idx,
                    count = tweenAnims.Length,
                    effect = EffectName,
                    retaliation = Controller != null && Controller.IsRetaliation,
                };

                // 先挂上「在等」再调下游：下游**同步**回调（在 OnInvoke 里就调 onFinished）也不会漏。
                _waiting = true;
                WaitingCount++;
                bool wait = OnInvoke(req, MarkFinished);

                if (_resumeRequested)      // 下游已经同步回调过 ⇒ 这条结束，接着下一条（同一帧，和原版一样）
                {
                    ClearWait();
                    _resumeRequested = false;
                    continue;
                }
                if (!wait) { ClearWait(); continue; }
                return;                    // 真在等：等 `MarkFinished` 把它排进下一次 `ModuleTick`
            }
            _running = false;
            _resumeRequested = false;
        }

        /// <summary>下游说「这条播完了」。**只排期、不在这里递归**（原版协程最早也是下一帧才恢复）。</summary>
        void MarkFinished()
        {
            ClearWait();
            if (_running) _resumeRequested = true;
        }

        void ClearWait()
        {
            if (!_waiting) return;
            _waiting = false;
            WaitingCount--;
        }

        void OnDisable()
        {
            // 原版 `OnDisable` → `alreadytrigger = false`。
            // 顺带作废在飞的等待：原版靠「Unity 停协程」达到同一效果，我们显式写出来。
            alreadytrigger = false;
            _running = false;
            _resumeRequested = false;
            ClearWait();
        }

        /// <summary>原版**没有**重写 `DoDestroy`（基类是空实现）—— 这里只为显式掐掉还挂在等上的链子
        /// （`Kill()` 那条路不走 `Exit`，销毁后回调可能还在飞）。</summary>
        public override void DoDestroy()
        {
            _running = false;
            _resumeRequested = false;
            ClearWait();
        }

        void Drop(string asset, int idx, string why)
        {
            _running = false;              // 链子到此为止（没下游 / 条目是空的，再往下也没意义）
            DroppedRequests++;
            if (_dropCounted) return;      // 每条链只报一次（同一条效果里后续条目不再刷屏）
            _dropCounted = true;
            if (_warnedDrops >= 5) return;
            _warnedDrops++;
            Debug.LogWarning($"[WarpforgeVFX] tween `{asset}`（第 {idx} 条，效果 `{EffectName}`）**没播**：{why}。" +
                             "要接：表现层挂 `WFModuleTween.OnInvoke`（原版走 `UnitTweenSO.BuildSequence`）。" +
                             "这条警告只报前 5 次。");
        }

        /// <summary>`@asset:MonoBehaviour:Impact Light Tween` → `Impact Light Tween`
        /// （不是资产引用就原样返回 —— 手挂时可以直接填资产名）。</summary>
        static string AssetName(string v)
        {
            string kind, type, rest;
            WFModuleDef.SplitRef(v, out kind, out type, out rest);
            return kind == "asset" ? rest : (v ?? "");
        }
    }
}
