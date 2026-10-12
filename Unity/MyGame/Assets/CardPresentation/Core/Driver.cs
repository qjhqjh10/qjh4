// Driver.cs —— 从 `Core/ClickLog.cs` 里拆出来的（A1506：MonoBehaviour 的类名【必须】与文件名相同，
// 否则 Unity 建不出 MonoScript ⇒ 存场景时被烘成内联 `!u!115` 存根 ⇒ player 加载即崩）。零行为改动。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>帧末收尾用的小驱动（隐藏、不销毁、不画任何东西）。</summary>
    class Driver : MonoBehaviour
    {
        void LateUpdate() { ClickLog.Flush(); }
    }
}
