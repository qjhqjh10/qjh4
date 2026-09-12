// RuleCodes.cs — 规则引擎的返回码
//
// 非法操作是**正常流程**（拖到非法格位本来就该被拒绝），所以用返回码而不是抛异常。
// 码值和 `d:/warpforge/scripts/rule_core.gd` 的常量**逐一对齐** —— 交叉验证时能直接对照。
using System.Collections.Generic;

namespace RuleEngine
{
    public static class RuleCodes
    {
        public const int OK = 0;
        public const int ErrBadHand = 1;          // 手牌索引非法
        public const int ErrCost = 2;             // 能量不足
        public const int ErrSlot = 3;             // 格位非法/被占
        public const int ErrNotTurn = 4;          // 非本方回合
        public const int ErrNotUnit = 5;          // 该格没有单位
        public const int ErrExhausted = 6;        // 已行动
        public const int ErrNoAttack = 7;         // 攻击力为 0 / Can't Attack
        public const int ErrSelf = 8;             // 不能攻击自己
        public const int ErrStunned = 9;          // 眩晕中无法行动
        public const int ErrTarget = 10;          // 目标不合法（Vanguard/Stealth/Flying）
        /// <summary>压制：无法执行**近战**攻击（规则书 :194）。
        /// ⚠️ 这个码值是**原版定的**（`rule_core.gd:38` `ERR_PINDOWN := 11`）——
        /// 原来 11 被我们的 `ErrUnimplemented` 占着，2026-09-12 把自定义码往后挪到 13 让位。</summary>
        public const int ErrPindown = 11;

        // ← 以下两条是本工程新增的（rule_core.gd 没有对应码）——
        //   不是因为规则不同，而是 v1 还没实现，需要让调用方**明确知道**是「没实现」而非「不允许」
        public const int ErrUnimplemented = 13;   // 该功能本版未实现（如战术卡效果）
        public const int ErrNoAbility = 14;       // 这个单位没有主动技能（卡上没写 `Ability:`）

        static readonly Dictionary<int, string> Names = new Dictionary<int, string>
        {
            { OK,             "OK" },
            { ErrBadHand,     "手牌索引非法" },
            { ErrCost,        "能量不足" },
            { ErrSlot,        "格位非法或被占" },
            { ErrNotTurn,     "不是你的回合" },
            { ErrNotUnit,     "该格没有单位" },
            { ErrExhausted,   "该单位本回合已行动" },
            { ErrNoAttack,    "该单位没有攻击力或不能攻击" },
            { ErrSelf,        "不能攻击自己" },
            { ErrStunned,     "该单位处于眩晕" },
            { ErrTarget,      "目标不合法（Vanguard / Stealth / Flying 限制）" },
            { ErrPindown,     "该单位被压制，无法进行近战攻击" },
            { ErrUnimplemented, "该功能本版未实现" },
            { ErrNoAbility,   "该单位没有主动技能" },
        };

        public static string Describe(int code)
        {
            string s;
            return Names.TryGetValue(code, out s) ? s : "未知错误码 " + code;
        }

        public static bool IsOk(int code) { return code == OK; }
    }
}
