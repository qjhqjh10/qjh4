// Shade.cs — 从 `ShellParts.cs` 拆出来的独立文件（原处是**次要类**，Unity 建不出 MonoScript）。
//
// 🔴 **为什么必须单开一个文件**（2026-10-11 · `A1506` / `A1481`）：Unity 只给**文件名 == 类名**
//   的那个类建 `MonoScript` 资产；次要类没有 MonoScript ⇒ 存场景时 `m_Script` 指不到资产 ⇒
//   Unity **当场烘一块内联 `!u!115` 存根**写进 level 文件 ⇒ player 加载时 `Position out of bounds!`
//   ⇒ `The file '.../levelN' is corrupted!` + 崩溃。判据与实测 → `资料/交接_第十七会话.md` §三 ·
//   `资料/任务_卡组卡牌战斗.md` 的 `A1506`。
//
// 压暗层（原版 `Shade{onAlphaLevel, defaultTimeToSwitch}`）。

using System.Collections;
using UnityEngine;

namespace CardPresentation
{
    public class Shade : MonoBehaviour
    {
        public float onAlphaLevel = 0.8f;          // 实证
        public float defaultTimeToSwitch = 0.5f;   // 实证（秒）

        ImageQuad _quad;
        float _alpha;

        public float Alpha { get { return _alpha; } }

        void Awake() { Build(); }

        public void Build()
        {
            if (_quad != null) return;
            _quad = ImageQuad.Create(transform, CardArt.Solid(), Vector3.zero, LayoutSpace.DesignHeight,
                                     new Vector2(0.5f, 0.5f), "Shade");
            if (_quad == null) return;
            float w = LayoutSpace.DesignHeight * LayoutSpace.DesignAspect;
            _quad.SetAspect(w / LayoutSpace.DesignHeight);
            _quad.SetRenderQueue(3040);                 // 压在主界面之上、弹窗（3020）之下由窗口自己决定
            SetAlpha(0f);
        }

        public void SetAlpha(float a)
        {
            _alpha = Mathf.Clamp01(a);
            Build();
            if (_quad == null) return;
            _quad.SetTint(new Color(0f, 0f, 0f, _alpha));
            _quad.gameObject.SetActive(_alpha > 0.001f);   // 全透明时整个关掉，省一次绘制
        }

        /// <summary>开：淡到 `onAlphaLevel`。关：淡回 0。</summary>
        public Coroutine Switch(bool on, float seconds = -1f)
        {
            float d = seconds < 0f ? defaultTimeToSwitch : seconds;
            return StartCoroutine(FadeTo(on ? onAlphaLevel : 0f, d));
        }

        public IEnumerator FadeTo(float target, float seconds)
        {
            Build();
            float from = _alpha;
            if (seconds <= 0f) { SetAlpha(target); yield break; }
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetAlpha(Mathf.Lerp(from, target, t / seconds));
                yield return null;
            }
            SetAlpha(target);
        }

        /// <summary>自检用：不走协程直接推一步（批处理没有帧循环）。</summary>
        public void StepTo(float t01) { SetAlpha(Mathf.Lerp(_alpha, onAlphaLevel, Mathf.Clamp01(t01))); }
    }
}