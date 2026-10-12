// ShadeHitTierWarn.cs —— 从 `Shell/MenuDraw.cs` 里拆出来的（A1506：MonoBehaviour 的类名【必须】与文件名相同，
// 否则 Unity 建不出 MonoScript ⇒ 存场景时被烘成内联 `!u!115` 存根 ⇒ player 加载即崩）。零行为改动。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>挂在**压暗层命中区节点**上的告警正文（形状照 `AbsorbTierWarn`）。
    /// `Why` = `MenuDraw.ShadeHit` 当场算出来的那条告警正文（**档不合法才挂**：`qShade >= qContentMin`）。
    /// 读法只有一处：`MenuDraw.ShadeHitTierWarned(node, out why)` —— 按窗记账（挂节点上、不是全局计数器）。
    /// ⚠️ 已销毁的节点 `node != null` 就是假 ⇒ 查询直接返回 false，不会去 `GetComponent`。
    /// 🆕 **2026-10-13 订正（A1506 拆文件那一批）**：本类头上原来挂的是 `MenuDraw.ShadeHit` **方法**的 doc
    /// （通篇讲 `qShade` / `qContentMin`）—— 那是**原文件里就写错位置**，已挪回方法头上；这里换成描述本类的一句。</summary>
    sealed class ShadeHitTierWarn : MonoBehaviour { public string Why; }
}
