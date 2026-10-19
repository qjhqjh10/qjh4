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
        /// <summary>稳定 id —— **原版卡 id**（形如 `ASH83` / `SOR11`；`cards_engine.json` 现读 1127 张，
        /// **0 张 `id == name`**，而卡名本身有 3 个重复：`Terminator Champion` / `Maulerfiend` / `Terminator`）。
        /// ⚠️ 2026-10-19（`A1577`②）就地订正：原来这里写「现在用卡名 —— OCR 产物里同名卡罕见」，
        /// **与事实不符** —— `id` 从 `K06` 起已是原版真值，不是卡名、也不是 OCR 产物。</summary>
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
