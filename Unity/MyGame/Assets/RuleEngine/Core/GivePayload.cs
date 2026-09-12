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
        /// <summary>
        /// **变体名**。目前只有 `Dark Pact of X` 这一族用（`blood` / `excess` / `fate` / `resilience`）——
        /// 规则书 :179 那四种契约的效果**互不相同**，不记下来就只能随机给一种。
        /// 出处：`rule_core.gd:1655` 的 `DARK_PACT_FX` 表 + `:1696` 那条
        /// `give (?:a )?(?:random )?dark pact(?:\s+of\s+([a-z]+))?` 正则。
        /// </summary>
        public string Variant;
        /// <summary>
        /// 这一项不是关键词、也不是属性 —— 是**一整段效果文字**
        /// （`Give "💀 Backlash: Return to your hand" to a friendly troop`）。
        /// 内容形如 `backlash: Return to your hand`（**小写**的 `关键词: 效果原文`）。
        /// 谁来解释它见 `EffectResolver.GrantEmbeddedAbility`。
        /// </summary>
        public string Embedded;
        /// <summary>原文那一小段（排查用）</summary>
        public string Source;

        public bool IsKeyword { get { return Keyword != null; } }

        public bool IsEmbedded { get { return Embedded != null; } }

        public override string ToString()
        {
            if (IsEmbedded) return "嵌入效果(" + Embedded + ")";
            string v = string.IsNullOrEmpty(Variant) ? "" : "(" + Variant + ")";
            return IsKeyword ? Keyword + v + " " + Value : (Value >= 0 ? "+" : "") + Value + " " + Attr;
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

        /// <summary>
        /// 载荷开头的**噪声词**：冠词 / 数量 / 方括号里掉出来的阵营名。
        /// 实测卡面里这些是 OCR + 数值表合并时混进来的，不是语义的一部分：
        ///   `Give a [Dark Pact] to all friendly troops`     → 方括号是**关键词标记**，剥掉
        ///   `Give a [Chaos] Dark Pact to a friendly troop`  → `Chaos` 是阵营名噪声
        ///   `Give 2 random Dark Pact to a friendly troop`   → `2` 是数量（这一版按一份给）
        /// 出处：原版 `rule_core.gd:1696` 那条只吃 `a` / `random`，覆盖不到这些；
        /// 我们**反复**剥到剥不动为止，多剥一层不多写一条特判。
        /// </summary>
        static readonly Regex ReLeadingNoise = new Regex(
            @"^(?:\d+|a|an|the|random|chaos|of)\s+", RegexOptions.Compiled);

        /// <summary>
        /// 多属性拆分 + 逐段解释。返回「有没有认出至少一段」。
        /// </summary>
        static bool ParseInto(string payload, List<PayloadOp> ops)
        {
            if (string.IsNullOrEmpty(payload)) return false;
            string w = payload.Replace("[", "").Replace("]", "").Trim();
            if (w.Length == 0) return false;

            // ④ 能量 —— `N energy` **要先于**噪声剥离判：`1 energy` 开头的 `1` 也是数字，
            //    剥掉就只剩 `energy`，再也匹配不上 `^(\d+)\s*energy$` 了（2026-09-12 撞到）。
            var meEarly = ReEnergy.Match(w);
            if (meEarly.Success)
            {
                ops.Add(new PayloadOp { Attr = "energy", Value = int.Parse(meEarly.Groups[1].Value), Source = w });
                return true;
            }

            // 反复剥掉开头的噪声词，直到剥不动 —— 见 `ReLeadingNoise` 的出处说明。
            // ⚠️ 循环上限写死，避免某条正则意外自我循环（这种 bug 在批处理里表现为「跑不完」）。
            for (int guard = 0; guard < 8; guard++)
            {
                string stripped = ReLeadingNoise.Replace(w, "");
                if (stripped == w || stripped.Length == 0) break;
                w = stripped;
            }
            if (w.Length == 0) return false;

            // ✅ **整条载荷一律转小写再解释**。原版收到的 `desc` 早就被 `_lower()` 过一遍了
            //    （`rule_core.gd` 的 `_resolve_text` 入口），而我们的调用方喂进来的是**原文大小写**
            //    （`Give +2 Melee Attack and Vanguard`）。2026-09-12 撞到：原先只在关键词那一支
            //    用 `ToLowerInvariant`，属性那支的正则吃着 `Melee` 这种大写**直接失配** ——
            //    表现是黑暗契约的四种增益**一条都没加上**（解析成功了、结算静默为空）。
            string low = w.ToLowerInvariant();

            // ①b **嵌入的一整段效果文字** —— `Give "💀 Backlash: Return to your hand" to a …`
            //
            //   判据：这一项里**含 `:`**，且 `:` **前面**那截是 `KeywordTable` 认得出的关键词。
            //   ⚠️ 必须在多属性拆分**之前**判 —— 那段文字里可能带 `,` 或 ` and `
            //      （`"Slay: Heal 2 and gain a Dark Pact of Excess"`），拆开了就废了。
            //   开头那个 `💀`（Backlash 的图标）是非 ASCII 噪声，一并剥掉。
            if (w.IndexOf(':') > 0)
            {
                int colon = w.IndexOf(':');
                string head = Regex.Replace(w.Substring(0, colon), @"[^\x20-\x7E]", "").Trim();
                head = head.Trim('"', '\'', '“', '”', ' ');
                int hl;
                string hk = KeywordTable.Normalize(head.ToLowerInvariant(), out hl);
                if (hk != null && hl == head.Length)
                {
                    ops.Add(new PayloadOp
                    {
                        Embedded = (head + w.Substring(colon)).ToLowerInvariant(),
                        Source = w,
                    });
                    return true;
                }
            }

            // ① 多属性拆分 —— 先按 `, ` 再按 ` and `（`rule_core.gd:3274`，顺序照抄）
            if (low.Contains(" and ") || low.Contains(", "))
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
            //
            // ⚠️ **只认开头**。噪声（冠词 / 数量 / `[Chaos]` 这类方括号阵营名）已经在上面
            //    `ReLeadingNoise` 里剥干净了，所以这里的判据可以收得很紧。
            //    松成 `IndexOf >= 0` 会误伤 —— `Trigger the Codex ability of all friendly units`
            //    这行里的 `codex` 不是「授予典籍关键词」，是**效果文字里提到它**。
            foreach (var pair in GiveKw)
            {
                if (!low.StartsWith(pair[0])) continue;
                var m = ReNumber.Match(w);
                ops.Add(new PayloadOp
                {
                    Keyword = pair[1],
                    Value = m.Success ? int.Parse(m.Groups[1].Value) : 1,
                    Variant = ExtractVariant(pair[1], w),
                    Source = w,
                });
                return true;
            }

            // ③ 属性增减益
            var am = ReAttr.Match(low);
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
        /// 从载荷里摘出**变体名** —— 目前只有 `dark pact of blood` 这一族有。
        /// `rule_core.gd:1696`：`give (?:a )?(?:random )?dark pact(?:\s+of\s+([a-z]+))?`。
        /// 没写 `of X` 就是 `random`（原版 `:1670` 明写）。**别的关键词没有变体，返回 null。**
        /// </summary>
        static string ExtractVariant(string keyword, string w)
        {
            if (keyword == KeywordTable.DarkPact)
            {
                // 从**关键词本身**往后看，别从头 Match —— 前面可能还挂着剥剩下的噪声
                int at = w.IndexOf("dark pact", System.StringComparison.Ordinal);
                if (at < 0) return "random";
                var m = Regex.Match(w.Substring(at), @"dark pact(?:\s+of\s+([a-z]+))?");
                if (m.Success && m.Groups[1].Success) return m.Groups[1].Value;
                return "random";
            }
            return null;
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
            // 三选一：`Payload` 存的是三个选项（`|` 隔开）—— 三个都要有机制才算数
            if (payload != null && payload.IndexOf('|') >= 0)
            {
                foreach (string o in payload.Split('|'))
                {
                    if (!Mechanized(o, out why)) return false;
                }
                why = null;
                return true;
            }

            var ops = Parse(payload);
            if (ops == null) { why = "载荷词表里没有"; return false; }
            foreach (var op in ops)
            {
                // 嵌入的一整段效果文字：那一段**自己**要能解析成 EffectSpec，否则挂上去也不触发
                if (op.IsEmbedded)
                {
                    if (EffectSpec.Parse(EmbeddedBody(op.Embedded)) == null)
                    {
                        why = "嵌入的效果文字解析不了";
                        return false;
                    }
                    continue;
                }
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

        /// <summary>`"backlash: return to your hand"` → `return to your hand`（`:` 后面那截）</summary>
        public static string EmbeddedBody(string embedded)
        {
            if (string.IsNullOrEmpty(embedded)) return null;
            int c = embedded.IndexOf(':');
            return c < 0 ? null : embedded.Substring(c + 1).Trim();
        }

        /// <summary>`"backlash: return to your hand"` → `backlash`（`:` 前面那截，已小写）</summary>
        public static string EmbeddedKeyword(string embedded)
        {
            if (string.IsNullOrEmpty(embedded)) return null;
            int c = embedded.IndexOf(':');
            return c <= 0 ? null : embedded.Substring(0, c).Trim();
        }
    }
}
