// WindowHolder.cs — 从 `WindowsManager.cs` 拆出来的独立文件（原处是**次要类**，Unity 建不出 MonoScript）。
//
// 🔴 **为什么必须单开一个文件**（2026-10-11 · `A1506` / `A1481`）：Unity 只给**文件名 == 类名**
//   的那个类建 `MonoScript` 资产；次要类没有 MonoScript ⇒ 存场景时 `m_Script` 指不到资产 ⇒
//   Unity **当场烘一块内联 `!u!115` 存根**写进 level 文件 ⇒ player 加载时 `Position out of bounds!`
//   ⇒ `The file '.../levelN' is corrupted!` + 崩溃。判据与实测 → `资料/交接_第十七会话.md` §三 ·
//   `资料/任务_卡组卡牌战斗.md` 的 `A1506`。
//
// 窗口锚点（`OnEnable → WindowsManager.RegisterAnchor`）。

using System.Collections.Generic;
using UnityEngine;
using WarpforgeVFX;      // `WFSoundPlayer`（特效/音效那条路唯一的播放器，`Assets/WarpforgeVFX/Runtime/`）

namespace CardPresentation
{
    public class WindowHolder : MonoBehaviour
    {
        public WindowsPlacement placement = WindowsPlacement.None;

        void OnEnable() { RegisterNow(); }
        void OnDisable() { WindowsManager.UnregisterAnchor(placement, transform); }

        /// <summary>🔴 **必须由建场景的代码在「赋完 placement 之后」显式调一次** ——
        /// `AddComponent` 的那一刻 `OnEnable` 就跑掉了，而 `placement` 是**下一行**才赋值的
        /// ⇒ 光靠 `OnEnable` 会拿 `None` 去注册（2026-09-22 第一版就这么白跑一轮，日志里那条
        /// 「placement = None」的报错就是它）。同一个节点重复注册是幂等的（`RegisterAnchor` 会直接返回）。</summary>
        public void RegisterNow() { WindowsManager.RegisterAnchor(placement, transform); }
    }
}