// RuleEngineTest_B113G2.cs — 第一百一十一会话 · 判据缺口组 · 查判据代理 `B113_G2`
//
// 覆盖：活状态表 `B-125`（`Grisly Feast` 的 `+1 Health` 被当成限时）+ `B-127`（五个 `until*` 位对照）。
// 根因与逐位对照（⛔ 本文件不抄第二份）→ `资料/完全复刻/判据/B113_G2_回合起止语义.md`
//   · 原版数据：`资料/普查产出_1018/allcards_1350/cards_raw.json` 的 Grisly Feast
//     `cardAbilities[0].abilityLogic[0..3].buffEffect`：
//       lg0 / lg1 = `meleeAttackChange = 1` + **`untilPlayerTurnStart = 1`**（+0x33，不是 +0x32）
//       lg2 / lg3 = `healthChange = 1` + **五个 `until*` 全 0**（= 永久）
//     字段与 Tooltip = `d:/2/tools/il2cpp_out/dump.cs:119074-119081`
//     （`+0x32 untilEndOfTurn`「Until end of turn」· `+0x33 untilPlayerTurnStart`「Until start of next turn」…）。
//   · 我们侧修法：`RuleEngine/Core/EffectText.cs:9530`（时长出口 `ApplyGiveDuration`，同物理行）
//     ＋ 类尾 `SplitMidPayloadDuration` / `IndexOutsideQuotes` / `ApplyDataOutlierDurations`
//     ＋ `TryGiveInner` 里 `SplitTargetList` 之后那一行（永久那半接在后面 —— 顺序承重）。
//
// 宿主接线（⛔ 本文件不碰 `RuleEngineTest.cs` —— 归调度台）：
//   在 `RuleEngineTest.Run()` 的聚合行 **同一物理行**、`}` 之前 / 行尾 `//` 之前追加
//   `Guard("CheckB113G2GrislyFeastDurations", CheckB113G2GrislyFeastDurations); `
//   —— 🔴 本仓已 6 次踩「追加落到行尾 `//` 后面 ⇒ 代码被注释吞掉，断言一条没跑」。
//   入口名**不以 `Test` / `Report` 打头**（`VerifyRunCaseCoverage` / `gen_diag_runner` 按那两个前缀认用例）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>`B-125` / `B-127`：`Grisly Feast` 的**四条** op 必须与**原版四行 `buffEffect`** 一一对上 ——
    /// 两条 `attack` 是 `untilPlayerTurnStart`（我们的 `nextturn`），两条 `health` 是**永久**（空串）。
    /// <para>🔴 本组**不建棋局、不打牌**：缺陷就在解析层。投影层端到端读数 =
    /// `bash d:/4/Unity/工具/ruleprobe.sh effects "Grisly Feast"`（attack 行 `Timed=1` / health 行 `Timed=0`）。</para></summary>
    public static void CheckB113G2GrislyFeastDurations()
    {
        Section("🆕 B113_G2 · B-125：`Grisly Feast` 的 `+1 Health` 必须是**永久**（攻击那两半才是限时）");

        // 卡对象是必须的：`ApplyDataOutlierDurations` 按卡名把攻击那两半改成 `nextturn`（原版数据孤例）。
        var pool = CardDatabase.Load();
        var card = CardDatabase.Find(pool, "Grisly Feast");
        CheckTrue(card != null, "A0 前提：卡池里找得到 `Grisly Feast`");

        const string desc = "Give +1 [Attack] this turn and +1 Health to your Warlord and Infantry troops";
        var ops = EffectText.Parse(desc, out _, out _, false, false, card);

        // ---- A1：拆成四条（改前是两条，且载荷是 `+1 attack and +1 health` 一整块）----
        CheckTrue(ops != null && ops.Count == 4,
                  "A1 前提：Grisly Feast 的正文解出**四条** op（2 个目标 × 2 个属性；改前 = 2 条）");

        if (ops != null && ops.Count == 4)
        {
            int atkTimed = 0, atkPerm = 0, hpTimed = 0, hpPerm = 0;
            foreach (var op in ops)
            {
                string pay = op.Payload ?? "";
                bool isAtk = pay.IndexOf("attack", System.StringComparison.OrdinalIgnoreCase) >= 0;
                bool isHp = pay.IndexOf("health", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (isAtk && !isHp) { if (op.Duration == "nextturn") atkTimed++; else atkPerm++; }
                else if (isHp && !isAtk) { if (op.Duration == "") hpPerm++; else hpTimed++; }
            }

            // ---- A2：攻击那两半 = 原版 `untilPlayerTurnStart(+0x33)`（我们的 `nextturn`）----
            Check(atkTimed, 2,
                  "A2-a 🧨 两条 `+1 attack` 必须是 **nextturn**（原版 lg0/lg1 的 `untilPlayerTurnStart = 1`）；"
                  + "把 `ApplyDataOutlierDurations` 删掉 ⇒ 本例红（读回 `turn`）");
            Check(atkPerm, 0, "A2-b 反例侧：不存在「永久」的 `+1 attack`（⛔ 别把时长丢给攻击那半）");

            // ---- A3：生命那两半 = 永久（原版 lg2/lg3 五个 `until*` 全 0）----
            Check(hpPerm, 2,
                  "A3-a 🧨 两条 `+1 health` 必须**没有时长**（= 永久）；回退 `SplitMidPayloadDuration` ⇒ 本例红（读成 `turn`）");
            Check(hpTimed, 0, "A3-b 反例侧：不存在「限时」的 `+1 health`（这正是 B-125 的缺陷形状）");
        }

        // ---- A4：灭自证 —— 「时长写在**末尾**」必须仍然整条限时（⛔ 别把拆分行写宽）----
        //   卡面 `Give +1 [Attack] and +1 [Ranged] to a friendly unit this turn`（`Primarch of the XIII`）。
        var prim = EffectText.Parse("Give +1 [Attack] and +1 [Ranged] to a friendly unit this turn",
                                    out _, out _, false, false, null);
        CheckTrue(prim != null && prim.Count == 1, "A4 前提：末尾带时长的句子仍只解出**一条** op");
        if (prim != null && prim.Count == 1)
        {
            Check(prim[0].Duration, "turn",
                  "A4-a 🧨 末尾时长必须**整条**限时 —— 把拆分放宽成「载荷里有 `and` 就拆」⇒ 本例红");
            bool hasBoth = (prim[0].Payload ?? "").IndexOf("attack", System.StringComparison.OrdinalIgnoreCase) >= 0
                        && (prim[0].Payload ?? "").IndexOf("ranged", System.StringComparison.OrdinalIgnoreCase) >= 0;
            CheckTrue(hasBoth, "A4-b 前提：那一整块载荷里 attack 与 ranged 都还在（拆了就会只剩一半）");
        }

        // ---- A5：不回归 —— `, or <另一档>` 不算拆分点（`Disruption Blades`）----
        var db = EffectText.Parse("Give +1 [Attack] to your units this turn, or +3 [Attack] if they are Destroyer.",
                                  out _, out _, false, false, null);
        CheckTrue(db != null && db.Count >= 1, "A5 前提：Disruption Blades 那句解得成 op");
        if (db != null && db.Count >= 1)
            Check(db[0].Duration, "turn",
                  "A5-a 逗号 `, or` 前面那半仍是**本回合**（拆分只认 ` and ` ⇒ 别把 `or` 档也切走）");
    }
}
