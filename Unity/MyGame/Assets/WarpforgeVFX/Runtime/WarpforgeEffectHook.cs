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
                if (fx == null) Debug.Log($"[WarpforgeVFX] 特效没播出来：{name} @ {pos}");
            };
        }
    }
}
