// ShellParts.cs — 外壳的三个小件：安全区 / 压暗层 / 遮罩
//
// 出处：`资料/阶段二_Shell_原版规格.md` §一「常驻件表」。
// 🔴 这三个件在原版 `level0` 里的**脚本组件都导出失败**（只剩 `m_Script` 引用）⇒
//    · 有实证的：`Shade{onAlphaLevel:0.8, defaultTimeToSwitch:0.5}`（战场场景同脚本 13 处）
//      · `UISafeAreaManager{m_safeZones:[{rectTransform, applyWidth, applyHeight}]}`（主菜单场景实证）
//    · **查不到的**：`BlockingOverlay` / `Spinner` 的字段与 Spinner 的类（`dump.cs` 里没有同名类）
//    ⇒ 查不到的地方**按行为等价实现并标出来**，不假装是照抄（铁律 3）。
using System.Collections;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 安全区适配。**类名与字段名照原版** `UISafeAreaManager{m_safeZones[]}`；
    /// 原版挂在**根对象**（`Intro UI` / 主菜单的 `Game UI`）上，指向两个节点：
    /// `Safe area All`(applyWidth 1, applyHeight 1) 与 `Safe area Only Horizontal`(1, 0) —— 主菜单实证。
    /// </summary>
    public class UISafeArea : MonoBehaviour
    {
        [System.Serializable]
        public class Zone
        {
            public Transform rectTransform;
            public bool applyWidth = true;
            public bool applyHeight = true;
        }

        public Zone[] zones;

        /// <summary>最后一次应用的归一化安全区（x,y,w,h）。诊断用。</summary>
        public Rect LastSafe { get; private set; }
        public bool Applied { get; private set; }

        void OnEnable() { Apply(); }
        void Update() { if (!Applied || LastSafe != Screen.safeArea) Apply(); }

        /// <summary>按 `Screen.safeArea` 缩放/平移每个 zone。**自检直调这条路**（批处理里没有真实刘海）。</summary>
        public void Apply() { Apply(Screen.safeArea, Screen.width, Screen.height); }

        public void Apply(Rect safe, int screenW, int screenH)
        {
            LastSafe = safe; Applied = true;
            if (screenW <= 0 || screenH <= 0 || zones == null) return;

            float sx = safe.width / screenW, sy = safe.height / screenH;
            // 安全区中心的偏移（归一化 → 世界单位；可见高度 = `LayoutSpace.DesignHeight`）
            float ox = ((safe.x + safe.width * 0.5f) / screenW - 0.5f) * (LayoutSpace.DesignHeight * LayoutSpace.DesignAspect);
            float oy = ((safe.y + safe.height * 0.5f) / screenH - 0.5f) * LayoutSpace.DesignHeight;

            foreach (var z in zones)
            {
                if (z == null || z.rectTransform == null) continue;
                z.rectTransform.localScale = new Vector3(z.applyWidth ? sx : 1f,
                                                         z.applyHeight ? sy : 1f, 1f);
                z.rectTransform.localPosition = new Vector3(z.applyWidth ? ox : 0f,
                                                            z.applyHeight ? oy : 0f, 0f);
            }
        }

        public string Dump()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"UISafeArea：{zones?.Length ?? 0} 个 zone · 安全区 {LastSafe.x:F0},{LastSafe.y:F0} {LastSafe.width:F0}x{LastSafe.height:F0}");
            if (zones != null)
                foreach (var z in zones)
                    if (z != null && z.rectTransform != null)
                        sb.Append($" · {z.rectTransform.name}(w={z.applyWidth},h={z.applyHeight}) scale={z.rectTransform.localScale.x:F3}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// 半透明压暗层。**字段名与默认值照原版** `Shade{shade, shadeUI, onAlphaLevel:0.8, defaultTimeToSwitch:0.5}`
    /// （实证来源：13 个战场场景里的同脚本实例）。
    /// `SetAlpha(0)` = 全透明 = 不挡视线（和原版一样：alpha 0 时那条 Shade 画不出来）。
    /// </summary>
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

    /// <summary>
    /// 全屏遮罩：挡输入 + 可选转圈（原版 `BlockingOverlay` + `Spinner`）。
    /// 🔴 **参数查不到**（正本 §五 第 1 条：坑 `Spinner` 这个类在 `dump.cs` 里都没有同名类）⇒
    /// 这里只保证**行为**：`StartSpinning/StopSpinning/AutoStopSpinner` 三个入口 + 一个静态的 `IsBlocking`。
    /// ⚠️ 转圈那张图（`40K_menu_loading`）**还没导进工程** ⇒ 没有图时**只挡输入、不画圈**，并说一声。
    /// </summary>
    public class BlockingOverlay : MonoBehaviour
    {
        /// <summary>有没有东西在挡输入。菜单的点击/拖拽入口都要问它一句。</summary>
        public static bool IsBlocking { get; private set; }

        ImageQuad _dim, _spinner;

        void Awake() { Build(); SetActiveState(false); }

        void Build()
        {
            if (_dim != null) return;
            _dim = ImageQuad.Create(transform, CardArt.Solid(), Vector3.zero, LayoutSpace.DesignHeight,
                                    new Vector2(0.5f, 0.5f), "BlockingOverlay");
            if (_dim != null)
            {
                _dim.SetAspect(1f);
                _dim.SetTint(new Color(0f, 0f, 0f, 0.35f));
                _dim.SetRenderQueue(3060);      // 挡住一切
            }

            var tex = CardArt.MenuUi("40K_menu_loading");
            if (tex != null)
                _spinner = ImageQuad.Create(transform, tex, Vector3.zero, 128f / 108f,
                                            new Vector2(0.5f, 0.5f), "Spinner");
            if (_spinner != null) _spinner.SetRenderQueue(3061);
        }

        void SetActiveState(bool on)
        {
            IsBlocking = on;
            Build();
            if (_dim != null) _dim.gameObject.SetActive(on);
            if (_spinner != null) _spinner.gameObject.SetActive(on);
        }

        public void StartSpinning()
        {
            SetActiveState(true);
            if (_spinner == null)
                Debug.Log("[Shell] 转了圈但**没有转圈图**（`40K_menu_loading` 不在 `Resources/Art/ui_menu/`）" +
                          "—— 现在只挡输入、不画圈。导出器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
        }

        public void StopSpinning() { SetActiveState(false); }

        /// <summary>照原版 `AutoStopSpinner`：`sec` 秒后自己停。</summary>
        public void AutoStopSpinner(float sec) { StartCoroutine(AutoStop(sec)); }

        IEnumerator AutoStop(float sec)
        {
            yield return new WaitForSecondsRealtime(sec);
            StopSpinning();
        }

        void Update()
        {
            if (_spinner != null && _spinner.gameObject.activeSelf)
                _spinner.transform.Rotate(0f, 0f, -180f * Time.unscaledDeltaTime);   // ⚠️ 转速我们挑的（原版查不到）
        }
    }
}
