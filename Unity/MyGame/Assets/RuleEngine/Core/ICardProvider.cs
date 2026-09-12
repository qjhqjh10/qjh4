// ICardProvider.cs — 卡牌基座唯一依赖的接口
//
// 为什么单开一个接口而不是直接传 `CardDef`：
//   `CardPresentation` 是**表现层**，它不该知道规则引擎的内部结构。
//   接上这个接口之后，卡面要的每一项（费用/攻/血/关键词）都有出处，
//   而「这张牌现在能不能打出」**不在这个接口里** —— 那需要上下文（谁的能量、哪一格），
//   是引擎的查询（`RuleCore.CanPlayCard`），不是卡自己的数据。
//
// 对应 `资料/自研游戏_特效与卡牌基座_设计.md` 里说的「基座只依赖一个接口」。

namespace RuleEngine
{
    public interface ICardProvider
    {
        /// <summary>稳定 id（现在用卡名 —— OCR 产物里同名卡罕见，真出问题再加 faction 前缀）</summary>
        string Id { get; }

        /// <summary>显示名</summary>
        string Title { get; }

        int Cost { get; }
        int Attack { get; }
        int Health { get; }

        /// <summary>远程攻击力（卡面左下紫圆）。近战/远程是两套数值，别混</summary>
        int RangedAttack { get; }

        /// <summary>单位还是战术 —— 决定它占不占格位、能不能拖到战场上</summary>
        bool IsUnit { get; }

        /// <summary>规范化后的关键词及 X 值（`armour`→2、`flying`→1）</summary>
        System.Collections.Generic.IReadOnlyDictionary<string, int> Keywords { get; }
    }
}
