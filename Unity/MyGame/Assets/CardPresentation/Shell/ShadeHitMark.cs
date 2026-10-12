// ShadeHitMark.cs —— 从 `Shell/MenuDraw.cs` 里拆出来的（A1506：MonoBehaviour 的类名【必须】与文件名相同，
// 否则 Unity 建不出 MonoScript ⇒ 存场景时被烘成内联 `!u!115` 存根 ⇒ player 加载即崩）。零行为改动。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>🔴 **2026-10-05（A71④）换判法**：原来这里是一张 `static HashSet&lt;Transform&gt;`，
    /// **只 `Add`、从不 `Clear`** ⇒ 窗口反复重建时 ① 无上限增长、② 长期持住**已销毁对象的托管壳**、
    /// ③ `instanceID` 复用时会**假阳性**（新节点被判成「ShadeHit 建的」）。
    /// 现在改成**挂在节点自己身上的一颗空标记**：节点跟着窗口一起销毁 ⇒ 上面三条一次都不成立
    /// （不再需要 `Clear`，也不再有一张全局表）。
    /// ⚠️ `ShadeHit` 的**既有行为一字未改**（那一句 `if (qShade >= qContentMin) …` 仍然那样告警；
    /// 🔴 只有计数那一半换了载体：`ShadeHitTierWarns++` → 往同一颗节点上挂 `ShadeHitTierWarn`）。</summary>
    sealed class ShadeHitMark : MonoBehaviour { }
}
