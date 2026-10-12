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
