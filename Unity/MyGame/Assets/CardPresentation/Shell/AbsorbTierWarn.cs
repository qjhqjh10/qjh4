// AbsorbTierWarn.cs —— 从 `Shell/MenuDraw.cs` 里拆出来的（A1506：MonoBehaviour 的类名【必须】与文件名相同，
// 否则 Unity 建不出 MonoScript ⇒ 存场景时被烘成内联 `!u!115` 存根 ⇒ player 加载即崩）。零行为改动。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`q &lt;= qShade` 时的告警正文（**按窗记账**：挂在**那颗吸收层节点**上，照 `ShadeHitTierWarned` 的形状）。
    /// 🔴 **2026-10-09（A221④）**：原来是一个全局累积计数器 `AbsorbTierWarns`（**从不复位**）——
    /// 任一窗告警会让**后面每一条** `CheckAbsorbRule` 都红、文案却指着别的窗（与 A77⑬⑦ 修掉的
    /// `ShadeHitTierWarns` 同族；那一轮只点了 `ShadeHit` 那一头，本字段当时没被点）。
    /// 现在记在节点自己身上 ⇒ 查询天然按窗、也没有跨窗/跨次状态。
    /// ⚠️ 全工程不变量不变：**每扇窗各自都必须是「没报过」**。
    /// 什么算不合法：`qContentMin - 1 &lt;= qShade`（该窗**没有空档** ⇒ 吸收层会与压暗层同档/越档，
    /// 赢家退化成枚举顺序 ⇒ 症状是「点窗内空白处**有时**会关窗」）。</summary>
    sealed class AbsorbTierWarn : MonoBehaviour { public string Why; }
}
