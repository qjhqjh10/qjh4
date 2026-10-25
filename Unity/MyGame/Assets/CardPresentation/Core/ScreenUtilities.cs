// ScreenUtilities.cs — 原版 `ScreenUtilities`（3 个纯函数）的等价物
//
// ============================ 判据（唯一一份，逐句照抄） ============================
// 出处 = `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/ScreenUtilities.cs`（il2csharp，
//   **方法体完整**，本仓 2026-10-25 实测该目录 2785 个 `.cs`、`throw null` **0 个**）。
//
//   ① `GetScreenPhysicalSize()`：
//        int n = Screen.height * Screen.height + Screen.width * Screen.width;   // 对角线像素²
//        return Sqrt(n) / (100.0f > Screen.dpi ? 100.0f : Screen.dpi);           // ⇒ **英寸**
//      ⚠️ 分母是「dpi 下限 100」：原版就写了这道 `max`（`0x1834b2bb4` 那一族常数里 100 是字面量），
//        ⛔ 别改成裸 `Screen.dpi`（`Screen.dpi` 在部分平台回 0 ⇒ 会除出 Infinity）。
//      ⚠️ `n` 原版是 **int**（两万级分辨率以内不会溢出；照抄，别自作主张换 long —— 换了就**不是**同一个数）。
//
//   ② `GetScreenDPI()`：`return 100.0f > Screen.dpi ? 100.0f : Screen.dpi;`（= ①的分母，同一档下限）
//
//   ③ `GetDiagonalSizeReferenceValue()`：
//        float t = (GetScreenPhysicalSize() - 4.7f) / 0.9000001f;
//        return 0f > t ? 0f : t <= 1f ? t : 1f;      // = clamp01((英寸 − 4.7) / 0.9)
//      ⚠️ 反编译体里那层 `clamp` 写了两遍（内一次外一次）—— **语义就是一次 clamp01**，
//        此处按一次落地；`0.9000001f` 是原版的 float 字面量（不是 0.9 的笔误），照抄。
//
// ============================ 谁在用（原版）============================
//   · `PlayerHand.cs:163` / `:187` —— 手牌布局按 `GetDiagonalSizeReferenceValue()` 在两套间距里插值
//   · `VarsDevice.cs:19`     —— `GetScreenPhysicalSize() > 7.0f` 走「大屏」那一档手牌 Y 偏移
//     （`GetCardInHandYOffsetTouchInput`：大屏给 `cardInHandShownYOffsetMin = 20`，否则 `cardInHandShownYOffset`）
//
// ============================ 我们这边（如实标）============================
// 🔴 **今天零消费者**：手牌布局在 `Hand/**`（N4 线），`VarsDevice` 没有对应类。
//   ⇒ 账 `卡牌面_渲染` 那三条（`GetScreenDPI` / `GetScreenPhysicalSize` / `GetDiagonalSizeReferenceValue`）
//     记 **B（已实现未核）**，⛔ 不记 A —— 函数对了，但**原版那两处观感还没接上**。
//   📌 接的时候**只准转调本类**（「两处写同一条规则 = 迟早不一致」）：⛔ 别在 `Hand/**` 里自己再写一遍
//     `Mathf.Sqrt(w*w+h*h)`。
// ⚠️ 与本工程「Small Screen UI」**无关**：那一套的判据里**没有任何 dpi / 物理英寸**
//   （`transformScalerBySmallScreenUI` 那段文件头写着，且原版 5 处写 `smallScreenUI` 的地方没有一处读屏）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `ScreenUtilities`（屏幕 dpi / 物理对角线，两档下限都写死 100）。</summary>
    public static class ScreenUtilities
    {
        /// <summary>= 原版 `GetScreenDPI()`：`Screen.dpi` 的**下限 100**（0 当 100）。
        /// ⚠️ 原版 `Screen.dpi` 为 0 时用它当分母 ⇒ ①也不会除零。</summary>
        public static float GetScreenDPI()
        {
            return 100.0f > Screen.dpi ? 100.0f : Screen.dpi;
        }

        /// <summary>= 原版 `GetScreenPhysicalSize()`：**屏幕物理对角线长度（英寸）**
        /// = `sqrt(w²+h²) / max(100, dpi)`。判据与两条 ⚠️ 见文件头 ①。</summary>
        public static float GetScreenPhysicalSize()
        {
            int n = Screen.height * Screen.height + Screen.width * Screen.width;
            float px = Mathf.Sqrt(n);                 // = 原版 `(float)Math.Sqrt(num1)`
            return px / GetScreenDPI();
        }

        /// <summary>= 原版 `GetDiagonalSizeReferenceValue()`：把「4.7 英寸 ~ 5.6 英寸」归一成 **0~1**
        /// （= `clamp01((physicalSize − 4.7) / 0.9000001)`）。判据见文件头 ③。</summary>
        public static float GetDiagonalSizeReferenceValue()
        {
            float t = (GetScreenPhysicalSize() - 4.7f) / 0.9000001f;
            return Mathf.Clamp01(t);
        }
    }
}
