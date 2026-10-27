// UIBorderGlow.cs — 边框辉光 / 呼吸（原版 `UIBorderGlow`）
//
// ════════════════════════════════════════════════════════════════════════════════════
//  判据（逐方法读过原版反编译 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/UIBorderGlow.cs`）
//
//  · 原版是 `UIBehaviour, IMeshModifier`：`ModifyMesh(VertexHelper)` **不重映射顶点**，只把
//    `uv0.zw` 写成「本件在屏幕上的中心」；**动画是那个材质浮点 `_Reveal`**（两件事，别混）：
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
//  · `graphicOriginalColor` 在 `Awake` 记下来（`.c:23-24`），**`DoAnimation` 末句读它还原 α**
//    ⇒ 原版播完 graphic 的 α = **原值**（⛔ 不是 0。原来这里写「不恢复」是错的，2026-10-27 订正）。
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
//  🔴 2026-10-27 就地订正（铁律 5）：原来这里写「`+68` 未知、本组件不复刻」——
//     **`decomp_full/UIBorderGlow__DoAnimation.c:42` 现读 = `ChangeGraphicAlpha(this + 0x68)`**，
//     而 `Awake` 把整个 `MyGraphic.color` 存进 `+0x5c..+0x6b`（`.c:23-24`，4 个 float）
//     ⇒ **`+0x68` 就是 `graphicOriginalColor.a`**：`DoAnimation` 末句把 graphic 的 α **还原成原值**。
// ════════════════════════════════════════════════════════════════════════════════════

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation
{
    /// <summary>边框辉光。原版 `UIBorderGlow`（`Assembly-CSharp`，无命名空间）。</summary>
    [DisallowMultipleComponent]
    public class UIBorderGlow : MonoBehaviour, UnityEngine.UI.IMeshModifier
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

        // 🔶 自己设计（非原版）：「材质没有 `_Reveal` 时退化成 α 渐隐/渐显」的替代路径 · 原版只有 reveal 那条路（原版判据拿不到） · TODO(谁定的)｜原注：── 🔴 替代路径（**不是原版口径**，默认关闭）────────────────────────────────
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
            CacheRefs(); Bookkeep();         // 原版 Awake 尾部：记 graphicOriginalColor（+0x5c..6b）+ 装 uv0.zw 载体
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
            bool got = CacheMaterial(); ChangeGraphicAlpha(alphaOnPlay);   // ⚠️ 顺序与原版不同，见 Anim 的注释
            PlayCount++;
            if (got) StartAnim();
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
                ChangeGraphicAlpha(_originalColor.a);   // 原版 `DoAnimation` 末句：α 还原成原值（与 reveal 无关）
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

            _inst.SetFloat(_revealId, revealFrom);          // 原版 `.c:34-35`：**先**置 −0.5，**再**等 animDelay
            ChangeGraphicAlpha(_originalColor.a);           // 原版 `.c:42`：末句把 graphic 的 α 还原成原值
            if (animDelay > 0f) yield return new WaitForSeconds(animDelay);
            float from = revealFrom, to = revealTo;
            float t = 0f;
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

        // ════════════════════════════════════════════════════════════════════════
        //  原版另外四个入口（2026-10-27 B9_W22GLOW 补齐）
        //  判据 = `d:/2/tools/decomp_full/UIBorderGlow__{CalculateViewPosition,ModifyMesh,
        //         OnRectTransformDimensionsChange,OnTransformParentChanged,Update}.c`
        //  ⚠️ 全部追加在类尾：账里本文件的落点都在 `:97`..`:205`，新增成员不许再把它们顶漂。
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>原版 `currentCamera`（字段 `+0x48`；`CalculateViewPosition.c:20,27`）。
        /// **取到就缓存** —— 原版只在它为 null 时才再问一次 `Camera.main`。</summary>
        Camera _currentCamera;

        /// <summary>原版 `graphicOriginalColor`（`+0x5c..+0x6b`，`UIBorderGlow__Awake.c:23-24`）。
        /// **只有 `.a` 被读**：`DoAnimation` 末句拿它把 graphic 的 α 还原回原值（`.c:42` 读 `+0x68`）。</summary>
        Color _originalColor = Color.white;

        /// <summary>`uv0.zw` 的载体。原版这件活是**组件自己做**的（它 `IMeshModifier`）；
        /// 我们的图块是 `ImageQuad`（`MeshRenderer`，不是 `Graphic`）⇒ 挂不上 uGUI 那条重建回调。
        /// 🔶 自己设计（非原版）：用旁挂的 `UIViewportUvWriter` 顶替 `ModifyMesh` 那一半 ·
        /// 为什么必须自己定：`ImageQuad.RebuildMesh` 只写 2 分量 uv、且没有 uGUI 重建回调可挂 ⇒
        /// 判据在「原版接口 vs 我们载体」这一层拿不到 · 调度台 2026-10-27 裁定。</summary>
        WarpforgeVFX.UIViewportUvWriter _uvCarrier;

        /// <summary>原版 `Awake` 尾部那两件事：把 `MyGraphic.color` 存进 `graphicOriginalColor`；
        /// 以及（我们的等价物）把 `uv0.zw` 载体装上。幂等，`Awake` 里与 `CacheRefs()` 同一步。</summary>
        void Bookkeep()
        {
            CacheRefs();
            _originalColor = CurrentColor();          // 原版 `Awake`：`graphicOriginalColor = MyGraphic.color`
            EnsureCarrier();
        }

        /// <summary>装上/取到 `uv0.zw` 载体。`_quad == null` 时不装 —— 真 uGUI `Graphic` 走本类自己的
        /// <see cref="ModifyMesh(VertexHelper)"/>，不需要旁挂件。</summary>
        void EnsureCarrier()
        {
            CacheRefs();
            if (_quad == null) return;
            if (_uvCarrier == null) _uvCarrier = GetComponent<WarpforgeVFX.UIViewportUvWriter>();
            if (_uvCarrier == null) _uvCarrier = gameObject.AddComponent<WarpforgeVFX.UIViewportUvWriter>();
        }

        /// <summary>当前可着色件的颜色（= `ChangeGraphicAlpha` 读的那一路：`ImageQuad.Tint` 或 `Graphic.color`）。</summary>
        Color CurrentColor()
        {
            CacheRefs();
            if (_quad != null) return _quad.Tint;
            if (_graphic != null) return _graphic.color;
            return Color.white;
        }

        /// <summary>原版 `CalculateViewPosition()`（`UIBorderGlow__CalculateViewPosition.c`）逐句：
        /// 懒取 `Camera.main` 并缓存（`:20-29`）→ 默认 **(0.5, 0.5)**（`:31-32` 两句 `0x3f000000`）
        /// → 有相机就 `WorldToViewportPoint(transform.position)` 取 **xy**（`:39-47`）。
        /// ⚠️ 算法本体**只有一份**（`UIViewportUvWriter.CalculateViewPosition`）——「两处写同一条规则 = 迟早不一致」，
        /// 这里只补原版的**相机缓存**语义（那一半在降级实现里没有）。</summary>
        public Vector2 CalculateViewPosition()
        {
            EnsureCarrier();                                                      // 懒兜底：Awake 时还没有 ImageQuad 也能补上
            if (!_currentCamera) _currentCamera = Camera.main;                    // `.c:24-29`
            if (_uvCarrier != null && _uvCarrier.cam == null) _uvCarrier.cam = _currentCamera;
            return WarpforgeVFX.UIViewportUvWriter.CalculateViewPosition(_currentCamera, transform.position);
        }

        /// <summary>原版 `ModifyMesh(Mesh)`：**空实现**（`UIBorderGlow.cs:284-286`；账里判 D）。
        /// `IMeshModifier` 要求它存在 ⇒ 照原版留空，⛔ 别往里加东西。</summary>
        public void ModifyMesh(Mesh mesh) { }

        /// <summary>原版 `ModifyMesh(VertexHelper)`（`UIBorderGlow__ModifyMesh.c`）逐句：
        /// `isActiveAndEnabled` 闸（`:44-45`）→ 算一次视口坐标（`:46`）→ 遍历**全部顶点**（`:68-92`），
        /// 每个顶点把 `uv0.z/.w` 写成 `view.x / view.y`、其余分量原样带回；**每个顶点写的是同一个值**。
        /// ⚠️ 我们的 `ImageQuad` 不是 `Graphic` ⇒ uGUI 永远不会调到这里（等价物 = <see cref="_uvCarrier"/>）；
        /// 这条实现在**真 uGUI `Graphic`** 上是原样生效的。</summary>
        public void ModifyMesh(VertexHelper verts)
        {
            if (!isActiveAndEnabled) return;                                       // `.c:44-45`
            if (verts == null) return;
            Vector2 view = CalculateViewPosition();                                // `.c:46`
            var v = new UIVertex();
            for (int i = 0, n = verts.currentVertCount; i < n; i++)                 // `.c:68-92`
            {
                verts.PopulateUIVertex(ref v, i);
                v.uv0 = new Vector4(v.uv0.x, v.uv0.y, view.x, view.y);
                verts.SetUIVertex(v, i);
            }
        }

        /// <summary>原版 `Update()`（`UIBorderGlow__Update.c:9-22`）：`transform.hasChanged` 才动手 →
        /// `MyGraphic.SetVerticesDirty()`（`:15-17`）→ 把这个标志清回 false（`:18-21`）。
        /// 等价物 = 让载体重写一次 `uv0.zw`（`ApplyNow` 自己会比对视口值，没变就不写）。
        /// ⚠️ 清 `hasChanged` 是**原版行为**（它是共享标志，原版同样清），照抄；⛔ 别改成「不清」。</summary>
        void Update()
        {
            if (!transform.hasChanged) return;
            if (_uvCarrier != null) _uvCarrier.ApplyNow();
            transform.hasChanged = false;
        }

        /// <summary>原版 `OnRectTransformDimensionsChange()`（`.c:1-8`）= `ModifyMesh(null)`（**空实现**）
        /// + `CalculateViewPosition()`（**返回值丢弃**）⇒ 净效果只有「刷新相机缓存」。
        /// ⚠️ 我们不是 `UIBehaviour`，但这是 Unity 的 RectTransform 消息，普通 `MonoBehaviour` 一样收得到；
        /// 而 `uv0.zw` 只依赖**世界位置**、不依赖尺寸 ⇒ 本来就不该在这里重写（原版也没重写）。</summary>
        void OnRectTransformDimensionsChange()
        {
            CalculateViewPosition();
        }

        /// <summary>原版 `OnTransformParentChanged()`（`.c:1-8`）：与上一条**机器码完全相同**
        /// （ILSpy 两个方法体也逐字相同）⇒ 那是 **ICF 折叠**，⛔ 别当成「文件名与内容不符」。
        /// 换父 ⇒ 世界位置变 ⇒ 载体的 `LateUpdate` 比对会在下一帧写上新的 `uv0.zw`。</summary>
        void OnTransformParentChanged()
        {
            CalculateViewPosition();
        }

        /// <summary>【断言 3】`uv0.zw` 那一半（原版 `ModifyMesh`）真的把值写进网格了。
        /// ⚠️ 前提硬检查在前、结论在后 —— ⛔ 不写成 `if (前提) Check(结论)`（那会静默跳过）。</summary>
        public bool SelfCheckViewUv()
        {
            EnsureCarrier();
            if (_uvCarrier == null)
            {
                Debug.LogError("[UIBorderGlow·断言3] 这个 GameObject 上没有 `UIViewportUvWriter`" +
                               "（`ImageQuad` 的 `uv0.zw` 没人写 ⇒ 扫光圆心恒在 (0,0)）");
                return false;
            }
            var mf = GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null || mf.sharedMesh.vertexCount <= 0)
            {
                Debug.LogError("[UIBorderGlow·断言3] 没有可用网格（`MeshFilter.sharedMesh` 为空）");
                return false;
            }
            _uvCarrier.ApplyNow();
            var uv = new System.Collections.Generic.List<Vector4>();
            mf.sharedMesh.GetUVs(0, uv);
            bool dimOk = uv.Count == mf.sharedMesh.vertexCount;
            Vector2 view = _uvCarrier.ViewPosition;
            bool valOk = dimOk;
            for (int i = 0; dimOk && i < uv.Count; i++)
                if (Mathf.Abs(uv[i].z - view.x) > 1e-4f || Mathf.Abs(uv[i].w - view.y) > 1e-4f) { valOk = false; break; }
            if (!dimOk || !valOk)
                Debug.LogError("[UIBorderGlow·断言3] uv0 四分量没写对：dimOk=" + dimOk + " valOk=" + valOk +
                               "（uv.Count=" + uv.Count + " 顶点=" + mf.sharedMesh.vertexCount + " view=" + view + "）");
            return dimOk && valOk;
        }
    }
}
