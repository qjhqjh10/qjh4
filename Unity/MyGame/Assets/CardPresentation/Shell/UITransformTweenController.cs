// UITransformTweenController.cs — 位移 / 缩放补间（原版 `UITransformTweenController`）
//
// ════════════════════════════════════════════════════════════════════════════════════
//  判据（逐方法读过原版反编译 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/UITransformTweenController.cs`，230 行）
//
//  · 字段（原版）：
//      `animTime`（**ctor 默认 1f**）· `animPosition` · `targetPosition` ·
//      `animScale` · `targetScale`（**ctor 默认 `Vector3.one`**）·
//      `initializeOnAwake` · `playOnEnable`
//  · `Awake()`（:32）：**仅当 `initializeOnAwake`** ⇒ 记下 `originalPosition/originalScale`
//  · `OnEnable()`（:48）：**仅当 `playOnEnable`** ⇒ 记原值 + 按 `animPosition/animScale` 分派
//      （两个都开 ⇒ 位置和缩放各起一条；只开一个 ⇒ 只起那一条）
//  · `Initialize()`（:70）：只记原值
//  · `DoDefaultAnim()`（:79）：同 OnEnable 的分派，不重新记原值
//  · `BackToDefault()` / `BackToDefault(float animTime)`（:110/:127）：补间回 `originalPosition/originalScale`
//  · `DoAnim(Vector2, Vector3, float)`（:146）：两条都起
//  · `DoAnimPosition(Vector2 desired, float animTime)`（:154）：
//      🔴 **原版有守卫** —— 拿**当前 `anchoredPosition` 与目标比较**（反编译里那段 `comisd` ——
//      "Not implemented instruction"），**相等就不起补间**；不等才 `Kill(旧)` + `DOAnchorPos(rect, desired, animTime)`。
//      ⚠️ **没有 `SetEase`** ⇒ DOTween **默认缓动 = `Ease.OutQuad`**（本类照此实现）。
//  · `DoAnimScale(Vector3 desiredScale, float animTime)`（:180）：同一套守卫，`DOScale(rect, desired, animTime)`。
//  · `OnValidate()`（:213）：只补 `rectTransform` 引用（编辑器用）。
//
//  🔴 **实例真值（现读 `menus_assets_all` 的 `Forge Menu Reward Button/RewardTransform`）**：
//      `animTime **1.5**` · `animPosition **1**` · `animScale **1**` · `initializeOnAwake **0**`
//      · `targetScale **(1.12, 1.12, 1.12)**` · `targetPosition **(0, 217.5)**`
//      ⇒ 「奖励图标 1.5 秒飞到槽位并放大到 1.12」。
//      （另 14 个实例在 Daily ×12 / Expansion Pass ×1 / Raid ×1 —— 都在暂停线；恢复时逐个现读即可。）
//
//  ⚠️ **本类没引 DOTween**：原版用 `DOAnchorPos`/`DOScale` + 默认 OutQuad；这里用协程手算同一条曲线
//     （`k = 1-(1-t)²`），并**把缓动显式暴露成 `Ease`** 以便将来对齐/对照。
// ════════════════════════════════════════════════════════════════════════════════════

using System.Collections;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>位移/缩放补间。原版 `UITransformTweenController`（`Assembly-CSharp`，无命名空间）。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UITransformTweenController : MonoBehaviour
    {
        /// <summary>原版没有这条枚举（DOTween 的 `Ease`）；这里只放本类实现的两种曲线。</summary>
        public enum Ease { OutQuad, Linear }

        // ── 原版字段（值与出处见文件头）──────────────────────────────────────────
        /// <summary>时长（秒）。原版 ctor 默认 **1f**；Forge 实例 = **1.5**。</summary>
        public float animTime = 1f;
        /// <summary>是否补位置。</summary>
        public bool animPosition;
        /// <summary>位置目标（`anchoredPosition`，**uGUI 设计 px**）。Forge 实例 = **(0, 217.5)**。</summary>
        public Vector2 targetPosition;
        /// <summary>是否补缩放。</summary>
        public bool animScale;
        /// <summary>缩放目标。原版 ctor 默认 `Vector3.one`；Forge 实例 = **(1.12, 1.12, 1.12)**。</summary>
        public Vector3 targetScale = Vector3.one;
        /// <summary>`Awake` 时先记原值。</summary>
        public bool initializeOnAwake;
        /// <summary>`OnEnable` 时自动播。</summary>
        public bool playOnEnable;

        /// <summary>缓动。原版 `DOAnchorPos`/`DOScale` **没 `SetEase`** ⇒ DOTween 默认 = OutQuad。</summary>
        public Ease ease = Ease.OutQuad;

        // ── 运行时（对应原版同名字段）────────────────────────────────────────────
        RectTransform _rt;
        Vector2 _originalPosition;
        Vector3 _originalScale;
        bool _initialized;
        Coroutine _posTween, _scaleTween;

        /// <summary>起过多少次位置补间（断言用；⛔ 不是原版字段）。</summary>
        public int PositionTweenCount { get; private set; }
        /// <summary>起过多少次缩放补间（断言用）。</summary>
        public int ScaleTweenCount { get; private set; }
        /// <summary>是否正在补（断言用）。</summary>
        public bool IsAnimating { get { return _posTween != null || _scaleTween != null; } }

        void Awake()
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            if (initializeOnAwake) Initialize();
        }

        void OnEnable()
        {
            if (playOnEnable)
            {
                CaptureOriginals();
                DoDefaultAnim();
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  原版 API
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>原版 `Initialize()`：只记原值（`:70`）。</summary>
        public void Initialize() { CaptureOriginals(); }

        /// <summary>原版 `DoDefaultAnim()`：按开关分派（`:79`）。</summary>
        public void DoDefaultAnim()
        {
            if (animPosition && animScale)
            {
                DoAnimPosition(targetPosition, animTime);
                DoAnimScale(targetScale, animTime);
            }
            else if (animPosition) DoAnimPosition(targetPosition, animTime);
            else DoAnimScale(targetScale, animTime);
        }

        /// <summary>原版 `BackToDefault()`：补回原值（`:110`）。</summary>
        public void BackToDefault()
        {
            CaptureOriginals();
            DoAnimPosition(_originalPosition, animTime);
            DoAnimScale(_originalScale, animTime);
        }

        /// <summary>原版 `BackToDefault(float animTime)`（`:127`）。</summary>
        public void BackToDefault(float time)
        {
            CaptureOriginals();
            DoAnimPosition(_originalPosition, time);
            DoAnimScale(_originalScale, time);
        }

        /// <summary>原版 `DoAnim(Vector2, Vector3, float)`（`:146`）。</summary>
        public void DoAnim(Vector2 desiredPosition, Vector3 desiredScale, float time)
        {
            DoAnimPosition(desiredPosition, time);
            DoAnimScale(desiredScale, time);
        }

        /// <summary>原版 `DoAnimPosition`（`:154`）：**已在目标位就不起补间**（原版那段 `comisd` 守卫）。</summary>
        public void DoAnimPosition(Vector2 desiredPosition, float time)
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            if (_rt == null) { Debug.LogError("[UITransformTween] 没有 RectTransform"); return; }

            if (Near(_rt.anchoredPosition, desiredPosition)) return;   // 🔴 原版守卫：相等 ⇒ 不动

            if (_posTween != null) StopCoroutine(_posTween);
            PositionTweenCount++;
            _posTween = StartCoroutine(TweenPos(desiredPosition, time));
        }

        /// <summary>原版 `DoAnimScale`（`:180`）：同一条「已在目标就不动」的守卫。</summary>
        public void DoAnimScale(Vector3 desiredScale, float time)
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            if (_rt == null) { Debug.LogError("[UITransformTween] 没有 RectTransform"); return; }

            if (Near(_rt.localScale, desiredScale)) return;            // 🔴 原版守卫

            if (_scaleTween != null) StopCoroutine(_scaleTween);
            ScaleTweenCount++;
            _scaleTween = StartCoroutine(TweenScale(desiredScale, time));
        }

        /// <summary>停掉两条补间（⚠️ **原版没有这个方法** —— 它只在每次起新补间时 Kill 旧的；
        /// 这里补一个显式停止口给接线方用）。</summary>
        public void Stop()
        {
            if (_posTween != null) { StopCoroutine(_posTween); _posTween = null; }
            if (_scaleTween != null) { StopCoroutine(_scaleTween); _scaleTween = null; }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  内部
        // ════════════════════════════════════════════════════════════════════════

        void CaptureOriginals()
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            if (_rt == null) return;
            _originalPosition = _rt.anchoredPosition;
            _originalScale = _rt.localScale;
            _initialized = true;
        }

        static bool Near(Vector2 a, Vector2 b) { return (a - b).sqrMagnitude < 1e-4f; }
        static bool Near(Vector3 a, Vector3 b) { return (a - b).sqrMagnitude < 1e-6f; }

        float Curve(float t)
        {
            t = Mathf.Clamp01(t);
            return ease == Ease.Linear ? t : 1f - (1f - t) * (1f - t);   // OutQuad
        }

        IEnumerator TweenPos(Vector2 to, float time)
        {
            Vector2 from = _rt.anchoredPosition;
            if (time <= 0f) { _rt.anchoredPosition = to; _posTween = null; yield break; }
            float e = 0f;
            while (e < time)
            {
                e += Time.deltaTime;
                _rt.anchoredPosition = Vector2.LerpUnclamped(from, to, Curve(e / time));
                yield return null;
            }
            _rt.anchoredPosition = to;
            _posTween = null;
        }

        IEnumerator TweenScale(Vector3 to, float time)
        {
            Vector3 from = _rt.localScale;
            if (time <= 0f) { _rt.localScale = to; _scaleTween = null; yield break; }
            float e = 0f;
            while (e < time)
            {
                e += Time.deltaTime;
                _rt.localScale = Vector3.LerpUnclamped(from, to, Curve(e / time));
                yield return null;
            }
            _rt.localScale = to;
            _scaleTween = null;
        }

        // ════════════════════════════════════════════════════════════════════════
        //  断言（两条 + 两条「灭自证」）
        //  ⚠️ **待自检恢复时跑**（用户 2026-10-25 暂停自检）
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>【断言 1·守卫】原版那条「已在目标 ⇒ 不起补间」的守卫：
        /// 把目标设成**当前值**调一次，两条补间的计数都**不许增加**。
        /// <para>🔴 **灭自证**：如果有人把守卫删掉（无脑起补间），这条立刻红 —— 而「动画能跑」的断言照样绿。</para></summary>
        public bool SelfCheckGuards()
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            int p0 = PositionTweenCount, s0 = ScaleTweenCount;
            DoAnimPosition(_rt.anchoredPosition, 0.2f);
            DoAnimScale(_rt.localScale, 0.2f);
            bool ok = PositionTweenCount == p0 && ScaleTweenCount == s0;
            if (!ok)
                Debug.LogError("[UITransformTween·断言1·守卫] 目标==当前值时仍然起了补间（" +
                               (PositionTweenCount - p0) + "/" + (ScaleTweenCount - s0) + "）—— 原版是直接 return");
            return ok;
        }

        /// <summary>【断言 2 + 灭自证】跑一次位置补间，检查**渐进**：
        /// <list type="bullet">
        /// <item>**0.5×T 时：位置必须严格在起终点之间，且离终点还有明显距离** —— 这条挡「一帧到位 / 直接置终值」</item>
        /// <item>T + 容差后：位置必须 == 目标</item>
        /// </list></summary>
        public IEnumerator SelfCheckProgressionPosition(float tolerance = 0.1f)
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            float T = Mathf.Max(0.05f, animTime);
            Vector2 from = _rt.anchoredPosition;
            Vector2 to = from + new Vector2(100f, 0f);      // 造一个必然不同的目标
            DoAnimPosition(to, T);

            yield return new WaitForSeconds(T * 0.5f);
            Vector2 mid = _rt.anchoredPosition;
            float span = Vector2.Distance(from, to);
            bool midBetween = Vector2.Distance(from, mid) > span * 0.05f
                              && Vector2.Distance(mid, to) > span * 0.05f;

            yield return new WaitForSeconds(T * 0.5f + tolerance);
            bool endOk = Vector2.Distance(_rt.anchoredPosition, to) < 1f;

            if (!midBetween || !endOk)
                Debug.LogError("[UITransformTween·断言2] midBetween=" + midBetween + " endOk=" + endOk +
                               "（mid=" + mid + " to=" + to + "）");
            _rt.anchoredPosition = from;                    // 还原，别污染后续自检
        }

        /// <summary>【断言 3】`Initialize()` 之后 `BackToDefault()` 能回到**捕获那一刻**的值。</summary>
        public IEnumerator SelfCheckBackToDefault(float tolerance = 0.1f)
        {
            if (_rt == null) _rt = GetComponent<RectTransform>();
            Initialize();
            Vector2 home = _originalPosition;
            Vector3 homeS = _originalScale;
            float T = Mathf.Max(0.05f, animTime);
            DoAnimPosition(home + new Vector2(80f, 40f), T);
            DoAnimScale(homeS * 1.2f, T);
            yield return new WaitForSeconds(T + tolerance);
            BackToDefault(T);
            yield return new WaitForSeconds(T + tolerance);
            bool ok = Vector2.Distance(_rt.anchoredPosition, home) < 1f
                      && (Vector3.Distance(_rt.localScale, homeS) < 1e-3f);
            if (!ok)
                Debug.LogError("[UITransformTween·断言3] BackToDefault 没回原位（pos=" + _rt.anchoredPosition +
                               " 期望 " + home + " · scale=" + _rt.localScale + " 期望 " + homeS + "）");
        }
    }
}
