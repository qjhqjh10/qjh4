// CosmeticDraggingController.cs — 从 `DraggableController.cs` 拆出来的独立文件（原处是**次要类**，Unity 建不出 MonoScript）。
//
// 🔴 **为什么必须单开一个文件**（2026-10-11 · `A1506` / `A1481`）：Unity 只给**文件名 == 类名**
//   的那个类建 `MonoScript` 资产；次要类没有 MonoScript ⇒ 存场景时 `m_Script` 指不到资产 ⇒
//   Unity **当场烘一块内联 `!u!115` 存根**写进 level 文件 ⇒ player 加载时 `Position out of bounds!`
//   ⇒ `The file '.../levelN' is corrupted!` + 崩溃。判据与实测 → `资料/交接_第十七会话.md` §三 ·
//   `资料/任务_卡组卡牌战斗.md` 的 `A1506`。
//
// 卡背拖拽的具名子类（空类，行为全在 `DraggableController<T>`）。

using System.Collections.Generic;
using RuleEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using WarpforgeVFX;

namespace CardPresentation
{
    public class CosmeticDraggingController : DraggableController<string> { }
}