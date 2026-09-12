// GivePayload.cs — `give` / `gain` / `lose` 的**载荷**怎么解释（属性增减益 还是 关键词授予）
//
// **权威语义来源**：`d:/warpforge/scripts/rule_core.gd` 的 `_apply_gain`（`:3266`）+ `GIVE_KW`（`:2408`）。
//
// 为什么单独一个文件：448 张战术卡里 `give X to Y` 有 **164 个分句、X 去重 78 个**，
// 而原版**不是 78 个独立实现** —— 全都走 `_apply_gain` 一个函数 + 一张 54 条的 `GIVE_KW` 前缀表。
// 照这个结构办：**一个入口，一张表**，78 个载荷一起通。
//
// `_apply_gain` 干四件事（`:3266`）：
//   ① **多属性拆分** —— 先按 `, ` 再按 ` and ` 递归（Autarch `"+1 melee, +1 ranged and +1 Health"` → 三段）
//   ② **关键词授予** —— `GIVE_KW` 前缀匹配；值 = 载荷里第一个数字，没有就是 1
//      （`armour` / `shield` / `stun` 三个还要**同步状态字段**，见 `RuleCore.ApplyGain`）
//   ③ **属性增减益** —— 一条正则吃下所有数值型载荷
//      `([+-]?\d+)\s+(ranged attack|attack|ranged|health|armor|melee|might|fist|strength)\b`
//   ④ **限时登记** —— 带时长的记进 `temp_buffs`，回合结束按 `end` 移除
//
// ⚠️ 本文件属于 `Core/` —— **不允许依赖 UnityEngine**。解析失败**不打印**，由调用方暴露。
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RuleEngine
{
    /// <summary>载荷里的一项：要么是**属性增减益**，要么是**关键词授予**。</summary>
    public class PayloadOp
    {
        /// <summary>属性增减益的目标字段：`attack` / `health` / `armour` / `ranged`。
        /// 关键词授予时为 null</summary>
        public string Attr;
        /// <summary>规范关键词名（`KeywordTable` 那套）。属性增减益时为 null</summary>
        public string Keyword;
        /// <summary>增减量（关键词授予时 = 关键词的 X 值，没有数字则 1）</summary>
        public int Value;
        /// <summary>原文那一小段（排查用）</summary>
        public string Source;

        public bool IsKeyword { get { return Keyword != null; } }

        public override string ToString()
        {
            return IsKeyword ? Keyword + " " + Value : (Value >= 0 ? "+" : "") + Value + " " + Attr;
        }
    }

    public static class GivePayload
    {
        /// <summary>
        /// `GIVE_KW` —— 关键词前缀表，**逐条照抄 `rule_core.gd:2408`（54 条）**。
        /// ⚠️ **顺序有意义**（前缀匹配，先命中先用）：多词变体排在单词前面，
        ///    `blood thirst` 排在 `bloodthirst` 前、`long range` 排在 `longrange` 前。
        /// </summary>
        public static readonly string[][] GiveKw =
        {
            new[] { "blood thirst", "bloodthirst" }, new[] { "long range", "longrange" },
            new[] { "hunt mark", "huntmark" }, new[] { "dark pact", "darkpact" },
            new[] { "shuriken", "shuriken" }, new[] { "vulnerable", "vulnerable" },
            new[] { "armour", "armour" }, new[] { "armor", "armour" },
            new[] { "camouflage", "camouflage" }, new[] { "flying", "flying" },
            new[] { "stealth", "stealth" }, new[] { "vanguard", "vanguard" },
            new[] { "flank", "flank" }, new[] { "pindown", "pindown" },
            new[] { "invulnerable", "invulnerable" }, new[] { "regeneration", "regeneration" },
            new[] { "shield", "shield" }, new[] { "blast", "blast" },
            new[] { "stun", "stun" }, new[] { "waystone", "waystone" },
            new[] { "sentry", "sentry" }, new[] { "fast", "fast" },
            new[] { "rally", "rally" }, new[] { "slay", "slay" }, new[] { "strike", "strike" },
            new[] { "stomp", "stomp" }, new[] { "unstable", "unstable" },
            new[] { "concussive", "concussion" }, new[] { "tide", "tide" },
            new[] { "markerlight", "markerlight" }, new[] { "oath", "oath" },
            new[] { "agenda", "agenda" }, new[] { "ecstasy", "ecstasy" },
            new[] { "companion", "companion" }, new[] { "teleport", "teleport" },
            new[] { "swarm", "swarm" }, new[] { "synapse", "synapse" },
            new[] { "mob", "mob" }, new[] { "pack", "pack" }, new[] { "regiment", "regiment" },
            new[] { "uprising", "uprising" }, new[] { "duty", "duty" },
            new[] { "ferocity", "ferocity" }, new[] { "pray", "pray" },
            new[] { "codex", "codex" }, new[] { "destroyer", "destroyer" },
            new[] { "talent", "talent" }, new[] { "sniper", "sniper" },
            new[] { "penitence", "penitence" }, new[] { "sabotage", "sabotage" },
            new[] { "cruelty", "cruelty" }, new[] { "bloodthirst", "bloodthirst" },
            new[] { "cantattack", "cantattack" }, new[] { "can't attack", "cantattack" },
        };

        /// <summary>属性增减益那条正则 —— **照抄 `rule_core.gd:3311`**。
        /// ⚠️ `might` / `fist` / `strength` 是**原版图标语义**（攻击强化 / 拳头图标），
        ///    都归到 `attack`（原版注释里标了出处：`Perfection` 机制 / `Enhanced Musculature`）。
        ///    `ranged attack` 要排在 `ranged` 前面，否则前缀短的那个先命中。</summary>
        static readonly Regex ReAttr = new Regex(
            @"([+-]?\d+)\s+(ranged attack|attack|ranged|health|armor|armour|melee|might|fist|strength)\b",
            RegexOptions.Compiled);

        static readonly Regex ReNumber = new Regex(@"(\d+)", RegexOptions.Compiled);

        /// <summary>`N energy` / `[N] energy`（方括号是付费标记，`rule_core.gd:2519` 会剥掉）</summary>
        static readonly Regex ReEnergy = new Regex(@"^(\d+)\s*energy$", RegexOptions.Compiled);

        /// <summary>裸 `+N` / `-N`（后面跟的是**图标**，图标在文本导出里丢了）</summary>
        static readonly Regex ReBareSigned = new Regex(@"^([+-]\d+)$", RegexOptions.Compiled);

        /// <summary>
        /// 拆一条载荷 → 若干 <see cref="PayloadOp"/>。
        /// **返回 null = 认不出来**（调用方要如实报，不许当成「无效果」）。
        /// </summary>
        public static List<PayloadOp> Parse(string payload)
        {
            var ops = new List<PayloadOp>();
            if (!ParseInto(payload, ops)) return null;
            return ops.Count > 0 ? ops : null;
        }

        /// <summary>多属性拆分 + 逐段解释。返回「有没有认出至少一段」。</summary>
        static bool ParseInto(string payload, List<PayloadOp> ops)
        {
            if (string.IsNullOrEmpty(payload)) return false;
            string w = payload.Replace("[", "").Replace("]", "").Trim();
            if (w.Length == 0) return false;
            // 冠词是噪声：卡面写 `Give a Dark Pact` / `Gain a random Dark Pact`，
            // 而 `GIVE_KW` 表里是 `dark pact`（原版在 `:2632` 单独特判了这一族）。
            // 统一在这里剥掉冠词与 `random`，比给每条加特判干净。
            w = Regex.Replace(w, @"^(?:a|an|the)\s+(?:random\s+)?", "").Trim();
            if (w.Length == 0) return false;

            // ① 多属性拆分 —— 先按 `, ` 再按 ` and `（`rule_core.gd:3274`，顺序照抄）
            if (w.Contains(" and ") || w.Contains(", "))
            {
                bool any = false;
                foreach (string s1 in w.Split(new[] { ", " }, System.StringSplitOptions.None))
                    foreach (string s2 in s1.Split(new[] { " and " }, System.StringSplitOptions.None))
                    {
                        string seg = s2.Trim();
                        if (seg.Length == 0) continue;
                        if (ParseInto(seg, ops)) any = true;
                    }
                return any;
            }

            // ② 关键词授予 —— `GIVE_KW` 前缀匹配（值 = 载荷里第一个数字，没有就是 1）
            foreach (var pair in GiveKw)
            {
                if (!w.StartsWith(pair[0])) continue;
                var m = ReNumber.Match(w);
                ops.Add(new PayloadOp
                {
                    Keyword = pair[1],
                    Value = m.Success ? int.Parse(m.Groups[1].Value) : 1,
                    Source = w,
                });
                return true;
            }

            // ③ 属性增减益
            var am = ReAttr.Match(w);
            if (am.Success)
            {
                string attr = am.Groups[2].Value;
                switch (attr)
                {
                    case "ranged attack": case "ranged": attr = "ranged"; break;
                    case "might": case "fist": case "strength": attr = "attack"; break;
                    case "armour": attr = "armour"; break;
                    case "armor": attr = "armour"; break;
                }
                ops.Add(new PayloadOp
                {
                    Attr = attr,
                    Value = int.Parse(am.Groups[1].Value),
                    Source = w,
                });
                return true;
            }

            // ④ 能量 —— `N energy`（`rule_core.gd:3114` 那条「裸数字 = 能量」的正规写法）
            var me = ReEnergy.Match(w);
            if (me.Success)
            {
                ops.Add(new PayloadOp { Attr = "energy", Value = int.Parse(me.Groups[1].Value), Source = w });
                return true;
            }

            // ⑤ 裸 `+N` / `-N` —— **数字后面跟的是图标，而图标在文本导出里丢了**。
            //
            //    证据（照工作准则第 3 条，参数从资源里抄、不猜）：
            //    `D:/2/Warpforge部队卡片/Dark Angels/4计策/Warpforge_57_March-of-Vengeance.png`
            //    卡面原文是 `Give +1 ⟨拳头图标⟩ to a friendly unit. Gain ⟨能量图标⟩ 2`
            //    —— 那个红圈拳头 = **近战攻击**（原版自己的映射见 `rule_core.gd:3325`：
            //    `fist/strength=拳头图标→attack`）。文本里只剩 `+1`，所以按近战攻击补。
            //    ⚠️ **原版 `_apply_gain` 对这种情况是失配的**（那条正则要求数字后面跟属性词）
            //       → 原版把这 12 张卡静默丢了。我们照卡面补上，并在注释里标明这是**补原版的漏**。
            var mb = ReBareSigned.Match(w);
            if (mb.Success)
            {
                ops.Add(new PayloadOp
                {
                    Attr = "attack",
                    Value = int.Parse(mb.Groups[1].Value),
                    Source = w,
                });
                return true;
            }

            return false;
        }

        /// <summary>
        /// 这条载荷**我们有没有机制去结算**。
        ///
        /// ⚠️ 和「能不能解析」是两件事（`资料/战术卡效果_移植方案.md` 第三节）：
        ///    `give Flank to a friendly troop` 解析得出来，但 `Flank` 没机制 → 这张卡
        ///    「能打但没用」。**两个数都要报**，只报一个会掩盖这类静默失效。
        /// </summary>
        public static bool Mechanized(string payload, out string why)
        {
            var ops = Parse(payload);
            if (ops == null) { why = "载荷词表里没有"; return false; }
            foreach (var op in ops)
            {
                if (!op.IsKeyword) continue;                 // 属性增减益：UnitState 有对应字段
                if (!KeywordTable.Implemented.Contains(op.Keyword))
                {
                    why = "关键词「" + op.Keyword + "」没有机制";
                    return false;
                }
            }
            why = null;
            return true;
        }
    }
}
