// EverguildGraphicRaycaster.cs — 执行写手 `B144-A`（2026-11-05）· B132 审计 #82
//
// ✅ 已复刻（2026-11-05 · B144-A · 原版 `EverguildGraphicRaycaster`，script guid `ab00d768289303400b8bca170be1aca6`）
//
// ============================ 它是什么 ============================
// 原版 13 场每场的 Canvas（根 `Canvas` + 4 件子画布）挂的不是 uGUI 基类 `GraphicRaycaster`，
// 而是这一颗子类。它只做一件事：把 `sortOrderPriority` **覆写掉**。
//
// ============================ 判据 ============================
//   · `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/EverguildGraphicRaycaster.cs:5-7`
//       `private new int priority;`
//       `public override int sortOrderPriority => priority;`
//   · 原版 `Battle Arena 1.unity` 的 4 个实例（`:228361` / `:232627` / `:237868` / `:238798`）序列化全是
//       `m_IgnoreReversedGraphics: 1` · `m_BlockingObjects: 0` · `m_BlockingMask: {m_Bits: 32}` · **`priority: 0`**
//   ⚠️ **两份反编译产物打架**：`assetripper …/Scripts/Assembly-CSharp/EverguildGraphicRaycaster.cs` 写的是
//      `public override int sortOrderPriority => 0;`（不带字段），而 cpp2il 写 `=> priority`。
//      **取 cpp2il 那份** —— 它保留了 `[SerializeField]` 字段，与场景 YAML 里那个 `priority: 0` 自洽；
//      而全部实例都是 0 ⇒ 两种写法在**当前数据下行为相同**，取信息量大的那份。
//
// ============================ 为什么必须覆写 ============================
//   uGUI `BaseRaycaster.sortOrderPriority` 的默认实现 = **`int.MinValue`**（`BaseRaycaster.cs:40`），
//   同一次 `EventSystem.RaycastAll` 里按它给 raycaster 排序。原版拉到 **0**（与 depth / sortingOrder 同档），
//   我们停在 `int.MinValue` ⇒ **排序垫底**。这是「值都抄对了、就是不在同一档」的那一类静默差异
//   （4 台 raycaster 的相对次序一旦与 `sortingOrder` 不平行，命中谁就变了）。
//
// ============================ 灭自证 ============================
//   `B144_A_PhysLayer_Assert` 的 E 组：把本类的覆写删掉 ⇒ `sortOrderPriority` 回 `int.MinValue` ⇒ 红；
//   把 `HudCanvas` 的 `AddComponent<EverguildGraphicRaycaster>()` 换回 `GraphicRaycaster` ⇒ 红。
//
// ⚠️ 覆盖边界：`m_BlockingObjects = 0`（None）⇒ `m_BlockingMask` **不参与射线**（B132 #84 判 ✅），
//    所以本类**不设** `blockingMask`（原版那份 32 是纯序列化差，运行时无行为）。
using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation
{
    /// <summary>原版 `EverguildGraphicRaycaster`：只把 <see cref="GraphicRaycaster.sortOrderPriority"/> 覆写成
    /// **序列化字段 `priority`**（13 场 4 个实例全 = 0）。⛔ 别删这个覆写 —— 基类默认是 `int.MinValue`。</summary>
    public class EverguildGraphicRaycaster : GraphicRaycaster
    {
        // ✅ 已复刻（2026-11-05 · B144-A · 原版 cpp2il `EverguildGraphicRaycaster.cs:5` 的 `[SerializeField] private new int priority`）
        // ⚠️ 原版带 `new`（它在隐藏基类的某个同名成员）；我们这边基类同名成员不可见 ⇒ 省掉 `new`（只是消一条 CS0109 警告）。
        [SerializeField] int priority;

        /// <summary>原版 `public override int sortOrderPriority => priority;`（4 个实例全 0）。</summary>
        public override int sortOrderPriority { get { return priority; } }
    }
}
