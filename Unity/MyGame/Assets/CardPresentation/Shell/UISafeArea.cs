// UISafeArea.cs — 安全区适配。
// 🔴 **2026-10-13 从 `ShellParts.cs` 拆出**（A1506：Unity 要求 MonoBehaviour 的**类名与所在文件名相同**，
//    否则建不出脚本资产 ⇒ 场景存盘会把它烘成内联 `MonoScript` 存根 ⇒ player 加载即崩）。
//    出处与判据**逐字搬自**原文件头与本类原有的 XML doc，未改一字（原文件头仍留着 Shade / BlockingOverlay
//    那两条注释 —— 那两个类早就有自己的同名文件了）。
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
}
