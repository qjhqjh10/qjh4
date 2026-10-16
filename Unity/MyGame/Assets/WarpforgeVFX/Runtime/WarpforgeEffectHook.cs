// WarpforgeEffectHook.cs — 把卡牌表现接到特效库上（**运行时会自动装**）
//
// 为什么需要它：`CardEffects.Play` 是个**静态委托**，而静态字段**不进场景、也不进 Play 模式**——
// 在编辑器建场景时接了也没用，按 Play 时它是 null，特效就只剩一行日志（踩过）。
// 所以用 `[RuntimeInitializeOnLoadMethod]` 在进 Play 时自己装上，不用场景里挂任何东西。
//
// 依赖方向：**特效库认识卡牌，卡牌不认识特效库**（`CardPresentation` 里没有一根
// `using WarpforgeVFX`，特效库不在时它照样跑）—— 所以这个文件放在特效这边。
using UnityEngine;

namespace WarpforgeVFX
{
    public static class WarpforgeEffectHook
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            CardPresentation.CardEffects.Play = (name, pos, parent) =>
            {
                if (string.IsNullOrEmpty(name)) return;
                if (WarpforgeEffectLibrary.Instance == null)
                {
                    Debug.LogWarning($"[WarpforgeVFX] 没有特效库，'{name}' 播不了");
                    return;
                }
                var fx = WarpforgeEffectPlayer.Play(name, parent, pos, 1f, -1f);
                if (fx == null) { Debug.Log($"[WarpforgeVFX] 特效没播出来：{name} @ {pos}"); return; }

                // 🆕 2026-10-16：**实例跟随父节点的层**（`parent != null` 时才管）。
                //   为什么需要：原版那些「挂在卡 / 棋盘上的特效」（落地扬尘、逐卡演出…）是由**透视战场相机**画的
                //   —— 它们跟卡活在同一层（`ArenaLayer`）。而 **Unity 的层不继承**：
                //   `Instantiate(prefab, parent)` 保的是 **prefab 自己的层**（我们那批是 0）⇒
                //   不补这一步，挂在卡上的特效会停在层 0、被 HUD 的正交相机画 ⇒ **位置和大小的量纲全错**。
                //   ⚠️ `parent = null` 的**根特效**不走这一步 —— 它们由谁画是另一笔账（`A367`），
                //     那一路的位置判据是「投到 HUD 平面」（见 `BoardLayout.DropTargetWorld`）。
                if (parent != null) SetLayerRec(fx.gameObject, parent.gameObject.layer);
            };
        }

        /// <summary>把这个实例整棵子树设到某一层（层**不会**从父节点继承，必须自己走一遍）。</summary>
        static void SetLayerRec(GameObject go, int layer)
        {
            go.layer = layer;
            var t = go.transform;
            for (int i = 0; i < t.childCount; i++) SetLayerRec(t.GetChild(i).gameObject, layer);
        }
    }
}
