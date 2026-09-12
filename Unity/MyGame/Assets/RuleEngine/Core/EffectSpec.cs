// EffectSpec.cs — 技能/触发的**效果**（一个极小的、我们自己定的文法）
//
// 为什么不解析原版 desc：`rule_core._resolve_text` 有 670 行，是个沼泽
// （见 `资料/规则引擎_设计.md` §1「v1 不做」）。而我们这套卡是**自己设计的**，
// 效果完全可以用一个**封闭文法**写清楚，一行一个，写错了自检会抓到。
//
// 文法（大小写不敏感，词之间空格分隔，顺序固定）：
//
//     <动词> [数值] <目标>        动词 ∈ Damage / Heal / Draw
//
//     Damage 2 EnemyUnit      → 对敌方一个单位造成 2 点（**不吃反击**，技能不是攻击）
//     Heal   3 OwnWarlord     → 己方督军回 3 点（不超过上限）
//     Draw   1                → 抽 1 张（Draw 不需要目标）
//
// 数值省略 = 1。目标：
//     Self / OwnWarlord / EnemyWarlord / EnemyUnit
//
// ⚠️ 目标必须**写全**（Draw 除外）—— 不给默认值。默认值会让 `Damage 2` 变成
//    「悄悄打了谁」，那种静默是这份工程最忌讳的（见 `资料/特效还原_进度与交接.md` 的教训）。
//    解析不出来就返回 `null`，卡面上那个关键词会被标成 `*`（未结算），不许装作能跑。
//
// ⚠️ 本文件属于 `Core/` —— **不允许依赖 UnityEngine**。解析失败**不打印**，
//    因为 Core 里没有 Debug；由调用方（自检 / 卡面的 `*` 标记）去暴露。
namespace RuleEngine
{
    /// <summary>效果目标的规范名（和文法里写的字符串一一对应，小写比较）</summary>
    public static class EffectTargets
    {
        public const string Self = "self";
        public const string OwnWarlord = "ownwarlord";
        public const string EnemyWarlord = "enemywarlord";
        public const string EnemyUnit = "enemyunit";

        /// <summary>要玩家/调用方**指定一个格位**的目标（其余目标自己就能定）</summary>
        public static bool NeedsPick(string target) { return target == EnemyUnit; }
    }

    public class EffectSpec
    {
        /// <summary>`damage` / `heal` / `draw`</summary>
        public string Verb;
        /// <summary>见 <see cref="EffectTargets"/>；`draw` 是空串</summary>
        public string Target;
        public int Amount;
        /// <summary>原文（日志和卡面用）。**保留原文**才好排查「到底写了什么」</summary>
        public string Source;

        public bool IsDamage { get { return Verb == "damage"; } }
        public bool IsHeal { get { return Verb == "heal"; } }

        /// <summary>卡面上那行小字：`DMG2 UNIT` / `HEAL1 OWN` / `DRAW1`</summary>
        public string Short()
        {
            string v = Verb == "damage" ? "DMG" : (Verb == "heal" ? "HEAL" : "DRAW");
            if (Verb == "draw") return v + Amount;
            return v + Amount + " " + ShortTarget(Target);
        }

        static string ShortTarget(string t)
        {
            switch (t)
            {
                case EffectTargets.Self: return "SELF";
                case EffectTargets.OwnWarlord: return "OWN";
                case EffectTargets.EnemyWarlord: return "FOE";
                case EffectTargets.EnemyUnit: return "UNIT";
                default: return "?";
            }
        }

        public override string ToString() { return Source ?? (Verb + " " + Amount + " " + Target); }

        // ==================================================================
        //  解析
        // ==================================================================

        /// <summary>
        /// 解析效果文本。**解析不出来返回 null**（不抛异常 —— 卡面文本是数据，不是代码）。
        /// 入参是 `':'` **后面**那半段，例：`"Damage 2 EnemyUnit"`。
        /// </summary>
        public static EffectSpec Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            string raw = text.Trim();
            if (raw.Length == 0) return null;

            var parts = raw.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return null;

            string verb = parts[0].ToLowerInvariant();
            if (verb != "damage" && verb != "heal" && verb != "draw") return null;

            int amount = -1;
            string target = null;

            for (int i = 1; i < parts.Length; i++)
            {
                string w = parts[i];
                int n;
                if (amount < 0 && int.TryParse(w, out n))
                {
                    if (n < 0) return null;              // 负数值 = 写错了，别猜
                    amount = n;
                    continue;
                }
                if (target == null)
                {
                    string t = CanonTarget(w);
                    if (t != null) { target = t; continue; }
                }
                // 出现了不认识的词 —— 宁可判失败，也别「跳过看不懂的部分接着跑」
                return null;
            }

            if (amount < 0) amount = 1;

            if (verb == "draw")
            {
                if (target != null) return null;         // Draw 不该有目标
            }
            else if (target == null)
            {
                return null;                             // 打谁 / 治谁必须写清楚
            }

            return new EffectSpec { Verb = verb, Target = target ?? "", Amount = amount, Source = raw };
        }

        static string CanonTarget(string w)
        {
            switch (w.ToLowerInvariant())
            {
                case "self": return EffectTargets.Self;
                case "ownwarlord": return EffectTargets.OwnWarlord;
                case "enemywarlord": return EffectTargets.EnemyWarlord;
                case "enemyunit": return EffectTargets.EnemyUnit;
                default: return null;
            }
        }
    }
}
