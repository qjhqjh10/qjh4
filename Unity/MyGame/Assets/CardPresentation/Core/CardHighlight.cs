// CardHighlight.cs — 卡牌的 6 种状态色
//
// 原版的 5 色态（可打出/不可打出/选中/合法目标/悬停）是**结构**参考 ——
// 状态划分沿用，具体颜色自己定（原版的配色是它的美术资产）。
//
// ⚠️ 现在只用**整卡着色**表达状态，够原型验证用。正式版该换成描边/流光
//    （一张卡同时是「可打出」又「被悬停」的话，单靠颜色分不开）——
//    那时改这张表 + 换一个支持描边的 shader 即可，状态机不用动。
using UnityEngine;

namespace CardPresentation
{
    public enum CardHighlightState
    {
        Normal,        // 常规
        Playable,      // 可打出 —— 费用够、轮次对
        Unplayable,    // 不可打出 —— 置灰
        Selected,      // 已选中
        ValidTarget,   // 合法目标（选目标阶段）
        Hover,         // 鼠标悬停
    }

    public static class CardHighlight
    {
        public static Color ColorOf(CardHighlightState s)
        {
            switch (s)
            {
                case CardHighlightState.Playable:    return new Color(1.00f, 1.00f, 0.88f);  // 暖白，像打了光
                case CardHighlightState.Unplayable:  return new Color(0.55f, 0.55f, 0.58f);  // 置灰
                case CardHighlightState.Selected:    return new Color(0.78f, 0.88f, 1.00f);  // 偏冷蓝
                case CardHighlightState.ValidTarget: return new Color(0.70f, 1.00f, 0.75f);  // 偏绿
                case CardHighlightState.Hover:       return new Color(1.00f, 1.00f, 1.00f);  // 原色（抬起靠位移表达）
                default:                             return Color.white;
            }
        }

        /// <summary>悬停/选中时额外放大多少（手感，不是必需）</summary>
        public static float ScaleOf(CardHighlightState s)
        {
            switch (s)
            {
                case CardHighlightState.Hover:
                case CardHighlightState.Selected: return 1.08f;
                default: return 1f;
            }
        }
    }
}
