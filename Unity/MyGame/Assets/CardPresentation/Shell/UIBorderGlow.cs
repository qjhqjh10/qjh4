// UIBorderGlow.cs — 边框辉光 / 呼吸（原版 `UIBorderGlow`）
//
// ════════════════════════════════════════════════════════════════════════════════════
//  判据（逐方法读过原版反编译 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/UIBorderGlow.cs`）
//
//  · 原版是 `UIBehaviour, IMeshModifier`：`ModifyMesh(VertexHelper)` 把边框顶点按
//    `_Reveal` 重映射，**动画就是那个材质浮点**：
//      `DoAnimation()`（:192-207）:
//         kill 旧补间 → `canvasRenderer.SetMaterial(instancedMaterial, 0)`
//         → `instancedMaterial.SetFloat(_Reveal, **-0.5f**)`
//         → `DOFloat(instancedMaterial, **1f**, _Reveal, **animTime**)` + `SetDelay(**animDelay**)`
//           + `SetEase(**Linear**)`
//         → 末尾 `ChangeGraphicAlpha(**0f**)`（把 graphic 自己的 α 置 0，只留材质画的辉光）
//  · `Play()`（:155-163）= `ChangeGraphicAlpha(0)` + `CacheMaterial()&&DoAnimation()`
//  · `OnEnable()`（:130-142）= `ModifyMesh` + **若 `playOnEnable`** 才走上面那一套
//  · `CacheMaterial()`（:164-190）：克隆 `CanvasRenderer.GetMaterial(0)`；**拿不到就等一帧再试**
//    （`DealyedAnim` 协程：`WaitForEndOfFrame` → 再 `CacheMaterial()` → `DoAnimation()`）
//  · `Stop()`（:219-225）：只 `DOTween.Kill(instancedMaterial)`（不还原 α、不还原 `_Reveal`）
//  · 字段：`animTime`（**ctor `:476` 默认 1f**）· `animDelay`（实例实测全 0）· `playOnEnable`（实例实测 86/89 = 0）
//  · 材质属性名：**`"_Reveal"`**（ctor `:477` `Shader.PropertyToID("_Reveal")`）
//  · `graphicOriginalColor` 在 `Awake` 记下来（`:127`）—— 但**全文件只出现这两处**
//    （声明 + 赋值），**没有任何地方用它还原** ⇒ 原版播完 graphic 就是 α0，**不恢复**。
//
//  🔴 **与本工程渲染模型的关系（必须看）**
//    我们 Shell/Battle 的 UI 是 `ImageQuad`（MeshRenderer + `Sprites/Default` 每件一份材质），
//    **没有 `_Reveal` 这个属性**。所以本组件分两条路：
//      ① **材质有 `_Reveal`**（将来换成带 reveal 的自建 shader）⇒ 走**原版口径**：−0.5 → 1 线性。
//      ② **材质没有 `_Reveal`** ⇒ **出声**（`Debug.LogWarning`，本工程红线：不静默失败），
//         并且**只有当 `alphaBreathing` 被显式打开**时才退化成「α 单向渐隐/渐显」。
//         ⛔ **原版没有这条替代** —— 它只把 graphic 的 α 置 0（`alphaOnPlay`），
//         所以 `alphaFrom/alphaTo` 的默认值**不是原版值**，是本组件给没有 reveal 的材质的兜底，
//         必须由接线方按观感自己给。默认 `alphaBreathing = false` ⇒ **不猜**。
//
//  ⚠️ **一处原版也没解出来的东西**：`DoAnimation` 里
//     `NoteDecompilerIssue("Unmanaged memory load: [this @ rcx (UIBorderGlow)+68]")`（:203）
//     是读取某个**对象字段**（偏移 +68，多半是 `instancedMaterial` 之外的那个），
//     反编译读成了未知 ⇒ 那一行**行为未知**；本组件不复刻那一行（也不猜它做什么）。
// ════════════════════════════════════════════════════════════════════════════════════

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation
{
    /// <summary>边框辉光。原版 `UIBorderGlow`（`Assembly-CSharp`，无命名空间）。</summary>
    [DisallowMultipleComponent]
    public class UIBorderGlow : MonoBehaviour
    {
        // ── 原版字段（值都标了出处）──────────────────────────────────────────────
        /// <summary>展开时长（秒）。原版 ctor `:476` 默认 **1f**。</summary>
        public float animTime = 1f;
        /// <summary>延迟（秒）。原版字段无初始化器；**89 个实例实测全是 0**。</summary>
        public float animDelay;
        /// <summary>`OnEnable` 时自动播。原版字段无初始化器；**实测 86/89 = 0**（只有 3 处奖励高亮 = 1）。</summary>
        public bool playOnEnable;

        // ── 原版直读的动画参数（DoAnimation `:196-200`）────────────────────────────
        /// <summary>材质浮点属性名。原版 ctor `:477` = **"_Reveal"**。</summary>
        public string shaderRevealProperty = "_Reveal";
        /// <summary>起始值。原版 `SetFloat(...,-0.5f)`（`:196`）。</summary>
        public float revealFrom = -0.5f;
        /// <summary>终值。原版 `DOFloat(...,1f,...)`（`:197`）。</summary>
        public float revealTo = 1f;
        /// <summary>播放瞬间把 graphic 自身 α 置成这个值。原版 `ChangeGraphicAlpha(0f)`（`:205`）。</summary>
        public float alphaOnPlay = 0f;

        // ── 🔴 替代路径（**不是原版口径**，默认关闭）────────────────────────────────
        /// <summary>材质没有 `_Reveal` 时，是否退化成「α 渐隐/渐显」。⛔ 默认 false = 不猜。</summary>
        public bool alphaBreathing;
        /// <summary>替代路径的起始 α（**非原版值**，接线方自己给）。</summary>
        public float alphaFrom = 1f;
        /// <summary>替代路径的终 α（**非原版值**，接线方自己给）。</summary>
        public float alphaTo = 0f;

        // ── 运行时 ────────────────────────────────────────────────────────────
        ImageQuad _quad;
        Graphic _graphic;
        Renderer _renderer;
        Material _inst;                 // 原版 instancedMaterial（克隆出来那份）
        Coroutine _anim;
        bool _warnedNoReveal;
        int _revealId;

        /// <summary>播放次数（断言用；⛔ 不是原版字段）。</summary>
        public int PlayCount { get; private set; }
        /// <summary>当前 `_Reveal` 值（读不到材质时为 <see cref="float.NaN"/>；断言用）。</summary>
        public float CurrentReveal
        {
            get
            {
                if (_inst == null || !_inst.HasProperty(_revealId)) return float.NaN;
                return _inst.GetFloat(_revealId);
            }
        }

        void Awake()
        {
            CacheRefs();
            _revealId = Shader.PropertyToID(shaderRevealProperty);
        }

        void OnEnable()
        {
            if (playOnEnable) Play();
        }

        void CacheRefs()
        {
            if (_quad == null) _quad = GetComponent<ImageQuad>();
            if (_graphic == null) _graphic = GetComponent<Graphic>();
            if (_renderer == null) _renderer = GetComponent<Renderer>();
        }

        // ════════════════════════════════════════════════════════════════════════
        //  原版 API
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>原版 `Play()`（:155）：先把 graphic α 置 <see cref="alphaOnPlay"/>，再起动画。</summary>
        public void Play()
        {
            CacheRefs();
            ChangeGraphicAlpha(alphaOnPlay);
            PlayCount++;
            if (CacheMaterial()) StartAnim();
        }

        /// <summary>原版 `Stop()`（:219）：只停补间 —— **不还原 α、不还原 `_Reveal`**（照原版）。</summary>
        public void Stop()
        {
            if (_anim != null) { StopCoroutine(_anim); _anim = null; }
        }

        /// <summary>把 graphic 自身 α 设成 <paramref name="target"/>（原版 `ChangeGraphicAlpha` :262）。
        /// ⚠️ 原版那两行也是 by-ref 未解，但从 `ChangeGraphicAlpha(0f)` 的用法可确定语义 = **只改 α**。</summary>
        public void ChangeGraphicAlpha(float target)
        {
            CacheRefs();
            if (_quad != null)
            {
                var c = _quad.Tint;
                c.a = target;
                _quad.SetTint(c);
                return;
            }
            if (_graphic != null)
            {
                var c = _graphic.color;
                c.a = target;
                _graphic.color = c;
            }
        }

        /// <summary>当前 graphic 的 α（断言用）。</summary>
        public float CurrentAlpha
        {
            get
            {
                CacheRefs();
                if (_quad != null) return _quad.Tint.a;
                if (_graphic != null) return _graphic.color.a;
                return float.NaN;
            }
        }

        /// <summary>原版 `CacheMaterial()`（:164）：克隆渲染件当前的材质；**拿不到就等一帧再试**。</summary>
        bool CacheMaterial()
        {
            CacheRefs();
            Material src = null;
            if (_renderer != null && _renderer.sharedMaterial != null) src = _renderer.sharedMaterial;
            else
            {
                var cr = GetComponent<CanvasRenderer>();
                if (cr != null) src = cr.GetMaterial(0);
            }
            if (src == null)
            {
                // 原版：起 DealyedAnim 协程（等一帧再 CacheMaterial → DoAnimation）
                if (_anim == null) _anim = StartCoroutine(DelayedAnim());
                return false;
            }
            _inst = new Material(src) { name = src.name + " (glow inst)" };
            if (_inst.HasProperty(_revealId)) _inst.SetFloat(_revealId, revealFrom);
            if (_renderer != null) _renderer.sharedMaterial = _inst;
            return true;
        }

        /// <summary>原版 `DealyedAnim()`（:208 / `_003CDealyedAnim_003Ed__18`）：等一帧 → 再取材质 → 起动画。</summary>
        IEnumerator DelayedAnim()
        {
            yield return new WaitForEndOfFrame();
            _anim = null;
            if (CacheMaterial()) StartAnim();
        }

        void StartAnim()
        {
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(Anim());
        }

        /// <summary>`_Reveal`：<see cref="revealFrom"/> → <see cref="revealTo"/>，延迟 <see cref="animDelay"/>，
        /// 时长 <see cref="animTime"/>，**线性**（原版 `SetEase(Ease.Linear)`）。</summary>
        IEnumerator Anim()
        {
            bool hasReveal = _inst != null && _inst.HasProperty(_revealId);
            if (!hasReveal)
            {
                if (!_warnedNoReveal)
                {
                    _warnedNoReveal = true;
                    Debug.LogWarning("[UIBorderGlow] 材质上没有 `" + shaderRevealProperty + "`（我们的 ImageQuad 用 " +
                                     "`Sprites/Default`）—— 原版那套 `_Reveal` 扫光**做不出来**。" +
                                     (alphaBreathing ? " 本件已开 alphaBreathing，退化成 α 渐变（**不是原版口径**）。"
                                                     : " alphaBreathing 未开 ⇒ 本件不会动（⛔ 不猜区间）。"));
                }
                if (!alphaBreathing) { _anim = null; yield break; }

                float a0 = alphaFrom, a1 = alphaTo;
                float e0 = 0f;
                ChangeGraphicAlpha(a0);
                while (e0 < animTime)
                {
                    e0 += Time.deltaTime;
                    ChangeGraphicAlpha(Mathf.Lerp(a0, a1, Mathf.Clamp01(e0 / Mathf.Max(1e-4f, animTime))));
                    yield return null;
                }
                ChangeGraphicAlpha(a1);
                _anim = null;
                yield break;
            }

            if (animDelay > 0f) yield return new WaitForSeconds(animDelay);
            float from = revealFrom, to = revealTo;
            float t = 0f;
            _inst.SetFloat(_revealId, from);
            while (t < animTime)
            {
                t += Time.deltaTime;
                _inst.SetFloat(_revealId, Mathf.Lerp(from, to, Mathf.Clamp01(t / Mathf.Max(1e-4f, animTime))));
                yield return null;
            }
            _inst.SetFloat(_revealId, to);
            _anim = null;
        }

        // ════════════════════════════════════════════════════════════════════════
        //  断言（两条 + 一条「灭自证」）
        //  ⚠️ **待自检恢复时跑**（用户 2026-10-25 暂停自检）
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>【断言 1】组件装上了：能找到渲染件（`ImageQuad` 或 `Graphic` 或 `Renderer`）。
        /// 找不到就**报错**（本工程红线：不静默失败）。</summary>
        public bool SelfCheckWired()
        {
            CacheRefs();
            bool ok = _quad != null || _graphic != null || _renderer != null;
            if (!ok) Debug.LogError("[UIBorderGlow·断言1] 这个 GameObject 上没有任何可着色件（ImageQuad/Graphic/Renderer）");
            return ok;
        }

        /// <summary>【断言 2 + 灭自证】播一次，检查**渐进**：
        /// <list type="bullet">
        /// <item>起播瞬间：`_Reveal`（有材质时）= <see cref="revealFrom"/>；无材质时 α = <see cref="alphaFrom"/></item>
        /// <item>**0.5×T 时：值必须严格落在 (from,to) 之间** —— 这条挡「直接置终值」（那就是自证：把动画删了断言照样绿）</item>
        /// <item>T + 容差后：值必须 == to</item>
        /// </list></summary>
        public IEnumerator SelfCheckProgression(float tolerance = 0.1f)
        {
            float T = Mathf.Max(0.05f, animTime);
            Play();
            yield return null;                                    // 让第一帧跑起来

            bool useReveal = _inst != null && _inst.HasProperty(_revealId);
            if (!useReveal && !alphaBreathing)
            {
                Debug.LogWarning("[UIBorderGlow·断言2] 材质没有 `_Reveal` 且 alphaBreathing 未开 ⇒ 无可断言（不判绿）");
                yield break;
            }
            float from = useReveal ? revealFrom : alphaFrom;
            float to = useReveal ? revealTo : alphaTo;
            System.Func<float> read = useReveal ? (System.Func<float>)(() => CurrentReveal) : (() => CurrentAlpha);

            float start = read();
            bool startOk = Mathf.Abs(start - from) < Mathf.Abs(to - from) * 0.25f;

            yield return new WaitForSeconds(T * 0.5f);
            float mid = read();
            bool midBetween = (mid - from) * (to - from) > 0f
                              && Mathf.Abs(mid - to) > Mathf.Abs(to - from) * 0.02f
                              && Mathf.Abs(mid - from) > Mathf.Abs(to - from) * 0.02f;

            yield return new WaitForSeconds(T * 0.5f + tolerance);
            float end = read();
            bool endOk = Mathf.Abs(end - to) < 1e-2f;

            if (!startOk || !midBetween || !endOk)
                Debug.LogError("[UIBorderGlow·断言2] startOk=" + startOk + " midBetween=" + midBetween +
                               " endOk=" + endOk + "（start=" + start + " mid=" + mid + " end=" + end +
                               " from=" + from + " to=" + to + "）");
        }
    }
}
