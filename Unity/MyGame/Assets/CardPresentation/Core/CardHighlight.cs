// CardHighlight.cs — 卡牌的状态色
//
// 🔴 **2026-09-18 更正：原版那 5 个状态色已经找到了，不再是「自己定」。**
//    出处（**亲读**）：`d:/2/解包整理/08_预制体特效/战斗预制体/MonoBehaviour/MonoBehaviour_-3885077450169410624.json`
//    （卡预制体上的 `CardHighlight` 组件，1344 B）——
//      ValidTargetColor    (0.0, 1.0, 0.1294)
//      SelectedColor       (1, 1, 1, 1)
//      PlayableColor       (1, 1, 0)
//      SelectedTargetColor (1, 0.5176, 0)
//      RegularColor        (1, 1, 1, **0**)      ← alpha 0 = **完全不亮**
//      CardHighlightAnimTime = 0.1 · ScaleFactor = 1.05
//    旁证：与 `Unity/_资源评估_场景特效动画.md:506,617`（2026-09-10 录的）**逐值吻合** ⇒ 两条独立来源一致。
//    ⚠️ 原写「原版的 5 色态是**结构**参考、具体颜色自己定（原版配色是它的美术资产）」——
//       **版权红线 2026-09-18 已由用户取消**，而且值本来就在本地，**没有理由再自己定**。
//
// 🔴 **但原版值不能整表照搬**，原因有二（照搬会坏）：
//   ① 原版那 5 色是给 `FrameHighlight` / `FrameHighlightRemnant` **两个 SpriteRenderer（描边层）** 用的；
//      而本类的 `ColorOf` 在我们这里**同时喂给「整卡着色」(`SetTint`) 与「描边」(`_rim`)** 两个地方
//      （`CardView.cs:1171` 与 `:1181`）。
//   ② 原版 `Regular` 的 **alpha = 0**（= 不点亮）。把 `Normal` 直接改成 alpha 0，
//      整卡着色那条路会把常规卡**染成全透明**。
//   ⇒ 所以下面**按状态逐个映射**，并在每行注明它对应原版哪一个、以及哪些是**我们自己的状态**。
//
// ⚠️ 现在只用**整卡着色**表达状态，够原型验证用。正式版该换成描边/流光
//    （一张卡同时是「可打出」又「被悬停」的话，单靠颜色分不开）——
//    那时改这张表 + 换一个支持描边的 shader 即可，状态机不用动。
//    🔴 **原版还有我们没做的一层**：`CardBodyToScale × ScaleFactor` 的**缩放补间**（DOTween，
//       时长 `CardHighlightAnimTime`）。我们只放大、**没有补间** —— 见 `CardHighlightAnimTime` 常量。
using UnityEngine;

namespace CardPresentation
{
    public enum CardHighlightState
    {
        Normal,        // 常规          —— 对应原版 `regular`（原版值 alpha 0 = 不点亮；我们保留原色不染）
        Playable,      // 可打出 —— 费用够、轮次对      —— 对应原版 `playable`
        Unplayable,    // 不可打出 —— 置灰             —— ⚠️ **我们自己的状态**，原版没有对应的
        Selected,      // 已选中                        —— 对应原版 `selected`
        ValidTarget,   // 合法目标（选目标阶段）          —— 对应原版 `potentialTarget*`（手牌/场上两档共用同一色）
        Hover,         // 鼠标悬停                      —— ⚠️ **我们自己的状态**，原版没有对应的
        // ⚠️ 原版还有一个 `selectedTargetInBoard`（`SelectedTargetColor` = 橙 (1, 0.518, 0)），
        //    **我们没有这个状态** —— 要用的话得先加进枚举与状态机，别只加一行色。
    }

    public static class CardHighlight
    {
        /// <summary>原版 `CardHighlightAnimTime` —— 状态切换那一下的**补间时长（秒）**。
        /// ⚠️ **我们目前没有补间实现**（缩放是瞬变的）⇒ 这个常量先摆着，等做补间时用它。
        /// 有个常量在，比把 0.1 散落在调用点里强。</summary>
        public const float AnimTime = 0.1f;

        /// <summary>原版 `ScaleFactor` —— 高亮时卡体放大到多少倍（`CardBodyToScale`）。见 `ScaleOf`。</summary>
        public const float ScaleFactor = 1.05f;

        public static Color ColorOf(CardHighlightState s)
        {
            switch (s)
            {
                // ↓ 以下三行 = **原版实读值**，逐位照抄
                case CardHighlightState.Playable:    return new Color(1.00f, 1.00f, 0.00f);        // 原版 PlayableColor：黄
                case CardHighlightState.Selected:    return new Color(1.00f, 1.00f, 1.00f);        // 原版 SelectedColor：白
                case CardHighlightState.ValidTarget: return new Color(0.00f, 1.00f, 0.1294118f);   // 原版 ValidTargetColor：绿

                // ↓ 以下三行 = **我们自己的**（原版没有这三个状态；色沿用我们原来的）
                case CardHighlightState.Unplayable:  return new Color(0.55f, 0.55f, 0.58f);        // 置灰
                case CardHighlightState.Hover:       return new Color(1.00f, 1.00f, 1.00f);        // 原色（抬起靠位移表达）
                // 原版 `RegularColor` 是 (1,1,1,**0**)。**这里故意不照抄 alpha=0** ——
                // 本函数同时喂 `SetTint`，alpha 0 会把常规卡染成全透明。**原版的「不亮」由不上色实现，
                // 我们由「不调用」实现**（`CardView.cs:1177` 那条 `show` 判据）。
                default:                             return Color.white;
            }
        }

        /// <summary>高亮时卡体放大到多少倍。**原版 `ScaleFactor` = 1.05**（`CardBodyToScale` × `ScaleFactor`）。
        /// ⚠️ 原值 1.08 是**我们挑的**，2026-09-18 换成原版值。
        /// ⚠️ **当前没有任何调用点用到本函数**（grep 过）—— 换值不会改变现状，只是把尺子拨正。
        /// 做「描边/流光」那一版时会用上，**那时别忘了配 `AnimTime` 的补间**。</summary>
        public static float ScaleOf(CardHighlightState s)
        {
            switch (s)
            {
                case CardHighlightState.Hover:
                case CardHighlightState.Selected: return ScaleFactor;
                default: return 1f;
            }
        }
    }
}
