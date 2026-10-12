// AbsorbMark.cs —— 从 `Shell/MenuDraw.cs` 里拆出来的（A1506：MonoBehaviour 的类名【必须】与文件名相同，
// 否则 Unity 建不出 MonoScript ⇒ 存场景时被烘成内联 `!u!115` 存根 ⇒ player 加载即崩）。零行为改动。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>挂在吸收层节点上的**空标记**（照 <see cref="ShadeHitMark"/> 的形状：
    /// 节点跟着窗口一起销毁 ⇒ 不需要 `Clear`、也不会有全局表的假阳性）。</summary>
    sealed class AbsorbMark : MonoBehaviour { }
}
