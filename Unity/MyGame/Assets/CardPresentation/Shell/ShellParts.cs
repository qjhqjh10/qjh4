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

    /// <summary>
    /// 全屏遮罩：挡输入 + 可选转圈（原版 `BlockingOverlay` + `Spinner`）。
    /// 🔴 **参数查不到**（正本 §五 第 1 条：坑 `Spinner` 这个类在 `dump.cs` 里都没有同名类）⇒
    /// 这里只保证**行为**：`StartSpinning/StopSpinning/AutoStopSpinner` 三个入口 + 一个静态的 `IsBlocking`。
    /// ⚠️ 转圈那张图（`40K_menu_loading`）**还没导进工程** ⇒ 没有图时**只挡输入、不画圈**，并说一声。
    /// </summary>
}
