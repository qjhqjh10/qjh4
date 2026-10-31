// RuleEngineTest_B112R2.cs — 第一百一十一会话 · 第 2 波 · 执行代理 `B112_R2W`（由只读 `B112_R2` 转岗）
//
// 覆盖：活状态表 `B-80` —— `SOR60 Holy Fire` 的句尾 `this turn` 必须落在【两半】op 上。
// 根因与判据（⛔ 本文件不抄第二份）→ `资料/完全复刻/判据/B112_R2_SOR60限时判定.md`
//   · 原版数据：`资料/普查产出_1018/allcards_1350/cards_raw.json` 的 SOR60
//     `buffEffect(ab0,lg0)`（督军 +2 攻）与 `(ab0,lg1)`（相邻部队 +1 攻）**两行的 `untilEndOfTurn` 都是 1**
//     （字段与 Tooltip = `d:/2/tools/il2cpp_out/dump.cs:119073-119081`「Until end of turn」）。
//   · 我们侧修法：`RuleEngine/Core/EffectText.cs:9530` 同一物理行（`SplitAndTail` 把时长切到后半句 ⇒ 回填前半句）
//     ＋ 类尾新增 `TailIsTimedGiveClause`（只认 give/gain 尾句，heal/deal 一律不回填）。
//
// 宿主接线（⛔ 本文件不碰 `RuleEngineTest.cs` —— 归调度台）：
//   在 `RuleEngineTest.Run()` 的聚合行（现读 `RuleEngineTest.cs:1367`，末尾 `… ; }   // 🆕 2026-10-29（…`）
//   **同一物理行**追加 `Guard("CheckB112R2SOR60GiveDuration", CheckB112R2SOR60GiveDuration); `
//   —— 🔴 必须落在 `}` 之前、`//` 之前（本仓已 6 次踩「追加落到行尾 `//` 后面 ⇒ 代码被注释吞掉，断言一条没跑」）。
//   入口名**不以 `Test` / `Report` 打头**（`VerifyRunCaseCoverage` / `gen_diag_runner` 按那两个前缀认用例）。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>`B-80`：`SOR60 Holy Fire` 的「this turn」必须同时落到**督军**与**相邻部队**两条 op 上。
    /// <para>🔴 本组**不建棋局、不打牌**：只钉解析层（缺陷就在这一层），以及「原版数据确实两行都限时」这两件事。
    /// 结算层与投影层的端到端读数 = `bash d:/4/Unity/工具/ruleprobe.sh effects SOR60`（见判据文件 §六·3）。</para></summary>
    public static void CheckB112R2SOR60GiveDuration()
    {
        Section("🆕 B112_R2W · B-80：`SOR60 Holy Fire` 的句尾时长要落到**两半**");

        // ---- A1–A3：解析层（改前：op[0] `dur=` 空 = 永久 ⇒ 回合结束那 +2 不会撤）----
        const string desc = "Give +2 [Attack] to your Warlord and +1 [Attack] to adjacent troops this turn. Gain 1 ☀";
        var ops = EffectText.Parse(desc, out _, out _, false, false, null);
        CheckTrue(ops != null && ops.Count >= 2, "A1 前提：SOR60 的正文解出 ≥2 条 op（现读 3 条）");
        if (ops != null && ops.Count >= 2)
        {
            CheckTrue(ops[0].Target != null && ops[0].Target.Kind == "warlord",
                      "A2 前提：op[0] 的目标是督军（`Kind=warlord`）");
            Check(ops[0].Duration, "turn",
                  "A2-a 🧨 督军那条必须是**本回合** —— 把 `EffectText.cs:9530` 那句回填删掉 ⇒ 本条红（读回空串），"
                  + "而且游戏里那 +2 会在**回合结束时不会被撤**（行为级缺陷）");

            CheckTrue(ops[1].Target != null && ops[1].Target.Adjacent,
                      "A3 前提：op[1] 是相邻部队（`Adjacent=1`）");
            Check(ops[1].Duration, "turn",
                  "A3-a 相邻部队那条仍是**本回合**（改动前后都该绿 —— 反例侧：证明回填没有把原来那半搞坏）");
        }

        // ---- A4：与原版数据对齐（读卡表 v40 的 `abilityBuffs` 旁表）----
        //   ⚠️ `AbilityBuffsOf` 返回 `null` = **判不了**（旧表 / 没装），⛔ 不是「这张卡没有行」⇒ 前提那条会红。
        CardDatabase.Load();                       // 现读 1129 张；这一跳同时把 v40 的旁表装上
        var rows = CardDatabase.AbilityBuffsOf("SOR60");
        CheckTrue(rows != null && rows.Count >= 2,
                  "A4 前提：卡表里有 SOR60 的 `abilityBuffs` 行（v40+；返回 null = 表旧了/没装 ⇒ 本段如实红）");
        if (rows != null && rows.Count >= 2)
        {
            int timed = 0;
            for (int i = 0; i < rows.Count; i++) if (rows[i].UntilEndOfTurn != 0) timed++;
            Check(timed, 2,
                  "A4-a 原版两行 `buffEffect` 的 `untilEndOfTurn` **都是 1**（期望值写死，⛔ 不从被测实现反算）");
        }

        // ---- A5：灭自证 / 不回归 ——「尾巴是 heal 的句子不能被回填」 ----
        //   判据：`Righteous Fury`「Heals 1 to your Warlord and give it +1 [Attack] until your next turn」
        //   ⇒ heal 必须保持**永久**、只有 give 是 `until your next turn`（卡表现读亦如此）。
        var rf = EffectText.Parse("Heals 1 to your Warlord and give it +1 [Attack] until your next turn",
                                  out _, out _, false, false, null);
        CheckTrue(rf != null && rf.Count >= 2, "A5 前提：Righteous Fury 那句解出 ≥2 条");
        if (rf != null && rf.Count >= 2)
        {
            Check(rf[0].Verb, "heal", "A5-a 前提：op[0] 是 heal");
            Check(rf[0].Duration, "",
                  "A5-b 🧨 heal 那条必须是**永久** —— 把回填写成无条件（不判尾句是不是 give/gain）⇒ 本条红");
            Check(rf[1].Duration, "nextturn",
                  "A5-c give 那条仍是 `until your next turn`（改动前后都该绿）");
        }
    }
}
