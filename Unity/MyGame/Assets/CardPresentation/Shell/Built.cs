// Built.cs —— 从 `Shell/RewardWindow.cs` 里拆出来的（A1506：MonoBehaviour 的类名【必须】与文件名相同，
// 否则 Unity 建不出 MonoScript ⇒ 存场景时被烘成内联 `!u!115` 存根 ⇒ player 加载即崩）。零行为改动。
// ⚠️ 它原来的外层是 `RewardClaimFx`（**不是** `RewardWindow`）⇒ 引用处写的是 `Built`（同类内不限定）⇒ 一律不用改。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>挂在 `Reward Claim` 根上：这一棵**建齐了没有**（= 5 个材质全取到）+ 建出几个材质。
    /// （`SetVisible` 读 `Ready` 决定要不要真开；自检也可以直接读。）</summary>
    public sealed class Built : MonoBehaviour
    {
        public bool Ready;
        public int Materials;
    }
}
