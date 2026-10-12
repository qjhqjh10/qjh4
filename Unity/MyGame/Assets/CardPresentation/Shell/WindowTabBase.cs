// WindowTabBase.cs — 一页的公共底座（页签机制那一族的基类）。
// 🔴 **2026-10-13 从 `RewardsWindow.cs` 拆出**（A1506：Unity 要求 MonoBehaviour 的**类名与所在文件名相同**，
//    否则建不出脚本资产 ⇒ 场景存盘会把它烘成内联 `MonoScript` 存根 ⇒ player 加载即崩）。
//    判据 / 出处 / 纪律见 `RewardsWindow.cs` 的文件头（本块**逐字搬**，未改一字）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一页。原版叫 `WindowTabBase`（`MissionsTab : WindowTabBase&lt;MainMenuRewardsWindow>`）。</summary>
    public abstract class WindowTabBase : MonoBehaviour
    {
        public abstract WindowTabType Type { get; }
        /// <summary>建内容。**换页时不重建**（原版是 `SetActive` 切换）。</summary>
        public virtual void Setup() { }
        /// <summary>每次切到本页时调（原版 `OnOpen`）。</summary>
        public virtual void OnOpen() { }
    }
}
