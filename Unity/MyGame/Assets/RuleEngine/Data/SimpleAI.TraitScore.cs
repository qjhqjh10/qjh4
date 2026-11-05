// SimpleAI.TraitScore.cs — `SimpleAI` 的兄弟分册：**关键词（字符串键）→ AI 分值**的完整表
// （🆕 2026-11-05 · 执行代理 `B145-E4` · 活状态表 `A-07`）
//
// 为什么单开一个文件：`SimpleAI.cs` 被账按 `文件:行号` 引用 40+ 处，往 `TraitScore` 的 `switch`
// 里插 20+ 条 `case` 就是一片漂移（铁律 13·3 / 18·b）。拆成 partial 后 `Data/SimpleAI.cs`
// **零行数变化**（唯一改动 = `TraitScore` 的 `default:` 那一行 1→1，改成转发到本文件）。
//
// ============================ 判据（⛔ 不要重新论证）============================
//   `资料/完全复刻/判据/AI打分与动作选择_第2轮.md` **§6·6 的 trait→分表**
//   （原版 `AI.cs:1807-1965` 的 `ScoreFromBuff` 体内那张表，实读）。
//
//   同一张表在我们这边的**数值键**版本早已落地 = `AiScoring.TraitAiScore(UnitState, int trait, int traitValue)`
//   （`Data/AiScoring.cs:861-890`，原版 `DefinedTrait` 的**枚举值**当键，option 11 的真实消费者）。
//   🔴 **本文件不是第二份判据** —— 它是同一张表的**字符串键**版本，服务的是另外两条入口：
//     ① `SimpleAI.PayloadScore(payload)`（`SimpleAI.cs` 的 `give`/`gain` 载荷打分，`default:` 转发到 `TraitScore`）；
//     ② `SimpleAI.KeywordValue(CardDef)`（历史保留、零调用点）。
//   ⇒ **改分值时两处一起改**（`AiScoring.TraitAiScore` 与这里），⛔ 别让它们各说各话。
//
// ============================ 与判据表的逐项对照 ============================
//   | 分 | 关键词 |
//   |---:|---|
//   | 2.0 | dodge survivor firststrike precognition |
//   | 1.5 | sneakattack sacrifice |
//   | 1.0 | ward unstoppable syphon tainted coward relentless transient resolution terror psyker reactor requiem landing |
//   | 0.5 | droppod berzerk |
//   | −1.5 | pindown |
//   | −1.0 | (vulnerable markerlight — 已在 `TraitScore` 本体里) |
//   | −2.0 | (cantattack — 已在 `TraitScore` 本体里) |
//   | −4.0 | poisoned |
//   | 3.0 | poisonous |
//   | 取值 | bloodthirst→`t.Attack` · maintenance→`t.KwValue("maintenance")` · bastion→`t.Bastion` · mutation→`t.KwValue("mutation")` · blast/shuriken→单位的 trait 数值 |
//
//   ⚠️ `TraitScore` 本体里已经有的 15 条（shield / vanguard / stealth / pack / camouflage /
//      longrange / backlash / flying / shuriken / vulnerable / markerlight / stun / blind / cantattack）
//      **一条都不搬过来** —— 重复的 `case` 是编译错误，而且会让「改哪一份」变得含糊。
//   ⚠️ 判据表里 `blast(350)` / `shuriken(1120)` 的取值是 **`e.traitValue`（那条 buff 自带的数值）**，
//      不是单位身上的数值。字符串入口拿不到 buff 对象 ⇒ 这里退回 `t.KwValue(关键词)`
//      （= 单位当前的 trait 数值），与 `AiScoring.TraitAiScore` 的 `mutation(780)` 同一把尺子。
//      `TraitScore(string)` 无单位上下文 ⇒ 这几条返回 **0**（⛔ 不是「猜一个」）。
//
// ============================ 🔴 卡池现状（别误判成「零收益」）============================
//   这些词今天大多**没有卡带**或只以 `cardTraits` 形式存在；本文件补的是
//   `PayloadScore` 对 `give/gain` 载荷的打分（例：载荷 `unstoppable` 原来落 0 分、现在 1.0）。
//   ⇒ 生产可见性取决于「有没有卡在载荷里写这个词」，**不是**「关键词表里有没有」。
//
// ============================ 灭自证（断言怎么才能红）============================
//   · 把 `pindown` 改回 0 / 改成正数 ⇒ `B145_E4_Assert` 的 A1 红；
//   · 把 `poisoned` 的 −4.0 改成 −1.5 ⇒ A2 红；把 `poisonous` 的 3.0 改成 0 ⇒ A3 红；
//   · 把 `dodge` 从 2.0 改成 1.5 ⇒ A4 红；
//   · 把 `TraitScore` 的 `default:` 改回裸 `return 0.0f` ⇒ A1/A2/A3/A4 全红（转发被切断）。
using System.Collections.Generic;

namespace RuleEngine
{
    public static partial class SimpleAI
    {
        /// <summary>关键词 → 分（**无单位上下文**的那一档）。
        /// 拿到的是「静态表」那一半：动态取值那 6 个词（`bloodthirst` / `maintenance` / `bastion` /
        /// `mutation` / `blast` / `shuriken`）在这里恒 **0** —— 要它们的分值请用带
        /// <see cref="UnitState"/> 的那个重载。⛔ 别在这儿猜一个数（铁律 2 / 18·0）。</summary>
        public static float TraitScoreExtra(string keyword)
        {
            return TraitScoreExtra(keyword, null);
        }

        /// <summary>关键词 → 分（**带单位上下文**的完整版）。
        /// 判据 = `判据/AI打分与动作选择_第2轮.md` §6·6（原版 `AI.ScoreFromBuff` 的 trait 表）。
        /// 数值键的同源表 = <see cref="AiScoring.TraitAiScore"/>（⛔ 改一处要两处一起改）。</summary>
        public static float TraitScoreExtra(string keyword, UnitState t)
        {
            switch (keyword)
            {
                // ---- 2.0 --------------------------------------------------
                case KeywordTable.Dodge: return 2.0f;           // dodge(240)
                case KeywordTable.Survivor: return 2.0f;        // survivor(430)
                case "firststrike": return 2.0f;                // firstStrike(630)：`Prefixes` 两条写法都归到本键
                case RuleCore.Precognition: return 2.0f;        // precognition(640)

                // ---- 1.5 --------------------------------------------------
                case KeywordTable.SneakAttack: return 1.5f;     // sneakAttack(20)
                case KeywordTable.Sacrifice: return 1.5f;       // sacrifice(470)

                // ---- 1.0 --------------------------------------------------
                case ConditionEval.Ward: return 1.0f;           // ward(140)
                case RuleCore.Unstoppable: return 1.0f;         // unstoppable(180)
                case "syphon": return 1.0f;                     // syphon(110)：`KeywordTable` 无具名常量（引擎里用字面量，见 `RuleCore.ResolveDamageDealtHook`）
                case ConditionEval.Tainted: return 1.0f;        // tainted(160)
                case RuleCore.Coward: return 1.0f;              // coward(190)
                case KeywordTable.Relentless: return 1.0f;      // relentless(450)
                case "transient": return 1.0f;                  // transient(455)
                case "resolution": return 1.0f;                 // resolution(840)
                case KeywordTable.Terror: return 1.0f;          // terror(520)
                case "psyker": return 1.0f;                     // psyker(620)
                case "reactor": return 1.0f;                    // reactor(625)
                case KeywordTable.Requiem: return 1.0f;         // requiem(760)
                case KeywordTable.Landing: return 1.0f;         // landing(770)

                // ---- 0.5 --------------------------------------------------
                case KeywordTable.DropPod: return 0.5f;         // dropPod(230)
                case RuleCore.Berzerk: return 0.5f;             // berzerk(360)

                // ---- −1.5 -------------------------------------------------
                case "pindown": return -1.5f;                   // pindown(970)

                // ---- −4.0 / 3.0 -------------------------------------------
                case KeywordTable.Poisoned: return -4.0f;       // poisoned(200)
                case PoisonousKeyword: return 3.0f;             // poisonous(380)：本类 `PoisonousKeyword` 同词（`SimpleAI.cs`）

                // ---- 动态取值（判据表最后那两行）--------------------------
                case KeywordTable.BloodThirst: return t != null ? t.Attack : 0f;                 // bloodThirst(220) → CurrentMeleeAttack
                case RuleCore.Maintenance: return t != null ? t.KwValue(RuleCore.Maintenance) : 0f;  // maintenance(370) → CurrentMaintenance
                case KeywordTable.Bastion: return t != null ? t.Bastion : 0f;                   // bastion(850) → CurrentBastion
                case "mutation": return t != null ? t.KwValue("mutation") : 0f;                 // mutation(780)：引擎未建模 ⇒ 与 `AiScoring.TraitAiScore` 同口径
                case "blast": return t != null ? t.KwValue("blast") : 0f;                       // blast(350)：原版读 `e.traitValue`（buff 自带）⇒ 这里退回单位数值，见文件头
                case "shuriken": return t != null ? t.KwValue("shuriken") : 0f;                 // shuriken(1120)：同上（`TraitScore` 本体那条 `case "shuriken": 0` 会先命中，本行只服务直接调用）

                default: return 0.0f;                           // 原版：**没列出的就是 0 分**
            }
        }

        // ==================================================================
        //  🆕 2026-11-05（`B145-E26` · 活状态表 `B145-TRAITSCORE-FIRSTWORD`）
        //     —— `PayloadScore` 的**逐段**拆分与计分。
        //
        //  判据：原版 `AI__ScoreFromPlayingNow.c` 对每一条 scoring criterion 调一次
        //    `ScoreFromCriteria` 再 `fVar7 = fVar7 + fVar6` **累加**；而一条 criterion 只带
        //    一个 buff、一个 buff 只有**一个** trait（`AI__ScoreFromBuff.c` 只读 `+0x3c`）。
        //    ⇒ 「一条 give/gain 载荷里写了几项」在打分上就是「几份独立的 buff」，必须逐项算再相加。
        //  ⛔ 本分册只服务**字符串入口**（`PayloadScore`），不是第二份判据：分值的正本仍是
        //    `TraitScore` / `TraitScoreExtra`（= 原版 §6·6 那张表）。
        // ==================================================================

        /// <summary>把一整条 `give` / `gain` 载荷拆成「一项一段」。
        /// 拆分口径与 `Core/GivePayload.cs` 的 `ParseIntoCore` **同源**（先 `, ` 再 ` and `）——
        /// ⛔ 别另写一套更宽松的切法（两处写同一条规则 = 迟早不一致）。
        /// ⚠️ 嵌在引号里的整段效果文字（`"Slay: Heal 2 and gain a Dark Pact"`）也会被这里切开，
        ///    但每一截都解析不成属性/关键词 ⇒ 计 0 分，对总分没有影响（与改前一致）。</summary>
        public static IEnumerable<string> SplitPayloadSegments(string payload)
        {
            if (string.IsNullOrEmpty(payload)) yield break;
            foreach (string s1 in payload.Split(new[] { ", " }, System.StringSplitOptions.None))
                foreach (string s2 in s1.Split(new[] { " and " }, System.StringSplitOptions.None))
                {
                    string seg = s2.Trim();
                    if (seg.Length > 0) yield return seg;
                }
        }

        /// <summary>属性词（这一段是「加属性」而不是「给关键词」）。收口只此一处。</summary>
        static bool IsPayloadAttrWord(string w)
        {
            switch (w)
            {
                case "attack": case "melee": case "ranged": case "health":
                case "armour": case "armor": case "cost":
                    return true;
                default: return false;
            }
        }

        /// <summary>**单段**载荷值多少分（`PayloadScore` 的逐段版）。
        ///
        /// 规则（与改前的单段口径逐条对齐，只是不再「只取第一个词」）：
        ///   ① 段内的数字求和；段内有属性词 ⇒ `系数 × max(1, 数字和)`；
        ///   ② 否则整段交给 `GivePayload.Parse` 归一成关键词（它认得 `blood thirst`→`bloodthirst`、
        ///      `[Shield] Shield`→`shield`、`wings flying`→`flying` 这些多词 / 带记号的写法），
        ///      再查 `TraitScore`（= 原版那张表）。**解析成功就不再兜底**（哪怕分值是 0）——
        ///      嵌入效果文字（`"backlash: …"`）必须留在 0，不能被兜底当成授了 `backlash` 关键词。
        ///   ③ `GivePayload` 认不出（它只收「能被授予」的那 60 来个词，**不含 `unstoppable` / `ward` /
        ///      `tainted` 这批只有分值表的词**）⇒ 退回**改前的口径**：第一个非数字词查 `TraitScore`。
        ///   ④ 都认不出 = **0 分**（⛔ 不猜，与 `TraitScore` 的 `default:` 同口径）。
        ///
        /// ⚠️ `+` / `-` 仍按**符号噪声**剥掉（= 改前的口径）：本函数不知道这一下是打自己人还是
        ///   打敌人（方向由调用方给的 op 决定），所以 `-2 attack` 与 `+2 attack` 同值。
        ///   🔶 自己设计（非原版）：属性系数 1.0 / 0.7 / 0.6 / 0.5 与「取绝对值」都是我们配的；
        ///   原版 `ScoreFromBuff` 的 `addStats` 出口是 `healthChange + rangeAttackChange + meleeAttackChange`
        ///   的**带符号原始和**，直接搬会跟卡价值量级不搭（见 `PayloadScore` 的 XML doc）。</summary>
        public static float PayloadSegmentScore(string seg)
        {
            if (string.IsNullOrEmpty(seg)) return 0f;
            string s = seg.Trim().ToLowerInvariant().Replace("[", "").Replace("]", "")
                          .Replace("+", " ").Replace("-", " ");
            string[] parts = s.Split(new[] { ' ', '\t', '.', ':', ';', '"', '\'' },
                                     System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return 0f;

            int num = 0;
            string attr = null;
            for (int i = 0; i < parts.Length; i++)
            {
                int v;
                if (int.TryParse(parts[i], out v)) { num += v; continue; }
                if (attr == null && IsPayloadAttrWord(parts[i])) attr = parts[i];
            }
            if (attr != null)
            {
                int n = num <= 0 ? 1 : num;
                switch (attr)
                {
                    case "attack": case "melee": case "ranged": return n * 1.0f;
                    case "health": return n * 0.7f;
                    case "armour": case "armor": return n * 0.6f;
                    case "cost": return n * 0.5f;
                }
            }

            var ops = GivePayload.Parse(seg);
            if (ops != null)
            {
                float sum = 0f;
                for (int i = 0; i < ops.Count; i++)
                    if (ops[i] != null && ops[i].Keyword != null) sum += TraitScore(ops[i].Keyword);
                return sum;
            }

            // 兜底 = 改前的口径（第一个非数字词）。`GivePayload` 的词表只收「能授予的关键词」，
            // 而分值表里还有一批只有分值、没有授予形状的词（`unstoppable` / `ward` / `tainted` …）。
            for (int i = 0; i < parts.Length; i++)
            {
                int v;
                if (int.TryParse(parts[i], out v)) continue;
                return TraitScore(parts[i]);
            }
            return 0f;
        }
    }
}
