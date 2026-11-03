// B121_D5_Assert.cs — 第一百二十一~二十二会话 · 执行写手 `B121-D5`（附魔残余换口 + 执行体）
//
// ============================ 它盯的是什么 ============================
//   `B121-D1B` 收尾时留下四条残余（报告 §六 ①/② + §四 的表）：
//     · `ca 30/40/50` 的目标来源仍只认 `ta`（36 格已就位却没接）；
//     · `ca 410` 仍走旧解算口 ⇒ `ta=10` 那一行「数据够了也不结算」；
//     · `ca 130 / 185 / 236 / 238` 四档执行体缺。
//   本件验的是**换口真的换了**（正向：36 格开始生效）+ **旧档仍精确出声**（反证：⛔ 不许静默）。
//
// ============================ 原版口径（判据，现读 `d:/2/tools/decomp_full/`）============================
//   · `ca 30 buffOthers`   `AbilityLogic__PlayAbility.c:1176-1250`（逐目标 `AddEffect`，目标集 = `GetTargets` + 36 格 criteria）
//   · `ca 410 triggerCodex` `:1685-1699`（逐目标 `AddTriggerCodex`）
//   · `ca 130 stun`        `:642-657`（逐目标 `AddStun`）
//   · `ca 185 stealMinionSelf` `:406-414`（**不读目标表**：`AddStealMinion(actingCard)`，永久易主）
//   · `ca 236 scriptedActingCardAttack` `:2716-2747`（`AddRedirectedAttack(actingCard, targets[0], …)`）
//   · `ca 238 scriptedSelfCardAttackDontCount` `:2764-2779`（逐目标 `AddScriptedForceAttack(thisCard, t, …, 1, 0)`）
//   · 目标集 = `AbilityLogic__GetTargets.c`（建池 + `FilterMethods__CheckIfMeetsCriteria` + `maxTargets` 裁剪）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1：`ResolveBuffOthersTargets` 退回旧的四档 switch ⇒ `ta=10` 仍判不了 ⇒ 返回 null ⇒ 红；
//   · A2 是**结构上的**灭自证：同一份夹具把 36 格拿掉 ⇒ 必须**出声 + 不结算**（= 改前行为）；
//     「拿掉 36 格反而能解」和「有 36 格却解不了」不可能同时成立。
//   · B1：`RunTriggerCodexRowOfEnchantment` 退回 `ResolveEnchantmentRowTargets` ⇒ `ta=10` 判不了 ⇒ 返回 0 ⇒ 红；
//   · C2：删掉 `ca 130` 那一跳 / 把目标集换成「双方全场」⇒ 友方那个也被晕 ⇒ 红；
//   · C3：`traitsFilter=100 ∧ excludeTrait=1` 的排除失效 ⇒ 已晕的那个被重晕 ⇒ 返回 1 ⇒ 红；
//   · D2：`ca 185` 退回「本回合 + Fast」那套（`TempControls` 归还）⇒ 归属不会变 ⇒ 红；
//   · E2：`ca 236` 改成「逐目标打」⇒ 两个目标都掉血 ⇒ 红（原版只取 `targets[0]`）；
//   · F2：`ca 238` 改成「不预扣不退还」⇒ 预扣那一次会留在 `AttacksThisTurn` 上（1 → 1 也看不出来；
//     所以用 `AttacksThisTurn` 从 0 起的那一档：净 0 才算对 —— 不退还时它会变成 -1？见 F2 注释）；
//   · G1：`ca 70/100` 被顺手接成「假执行」⇒ 返回 0 但**不出声** ⇒ 红。
//     ⚠️ **2026-11-03 就地订正（`B121-FIX`）**：本行原写「G = 仍在缺口里的两档」，理由是
//     「`unitsToSummon` 卡 id 列缺」——**已过期**：那一列已由 `B121-D4` 就位（`CardDef.SummonSpec.UnitsToSummon`，
//     三态 `null`/`[]`/非空）。G 组现在是「**夹具没带这一列**（`null` = 未知，⛔ 不当空表）⇒ 仍然精确出声、不结算」的反证。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加：
//     `Guard("B121D5_Assert", B121_D5_Assert.Run); _pass += B121_D5_Assert.Pass; _fail += B121_D5_Assert.Fail;`
//   （⚠️ 追加的文本必须落在该行第一个 `//` **之前**。）
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEditor;
using UnityEngine;

/// <summary>B121-D5 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B121_D5_Assert
{
    const int Trig = 640;          // 模板行挂一个存在的 trigger（本组只测 `ca` 分派 / 目标集，不测派发闸）
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B121-D5：附魔残余换口（ca 30/410）+ 执行体（ca 130/185/236/238）");

        // ---- A 组：ca 30/40/50 的目标来源换口 ---------------------------------------
        Section("A `ca 30 buffOthers` 换口：36 格真生效；没有 36 格时**仍然精确出声**");
        {
            var ctx = MakeBattle(out var carrier, out var opp);
            if (ctx == null || carrier == null) { CheckTrue(false, "A0 前提不成立：对局 / 载体没建起来"); }
            else
            {
                // 载体在 0 侧格 3；`opp` 在 1 侧格 3。criteria：只留**敌方部队**（对 owner=0 而言）。
                var crit = Criteria(10);
                crit.EnemyUnits = 1; crit.IncludeMinions = 1; crit.FriendlyUnits = 0;
                var tpl = Template("B121D5TplBuff30", Trig, ca: 30, pa: 0, ta: 10, fm: 0, mult: 0, crit: crit);
                var row = tpl.TriggerAbilityIndices[0];

                var got = RuleCore.ResolveBuffOthersTargetsForTest(ctx, 0, carrier, null, null, 10, tpl, row);
                CheckTrue(got != null, "A0b 前提：`ca 30` 的 `ta=10` + 36 格能解出目标集（改前恒 null）");
                Check(got != null ? got.Count : -1, 1,
                      "A1 ★★★ **结论**：`ta=10` + `enemyUnits=1` ⇒ 目标集 = **1 个敌方单位**"
                    + "｜🧨 退回旧的四档 switch ⇒ null ⇒ 红");
                CheckTrue(got != null && got.Count == 1 && !ReferenceEquals(got[0], carrier),
                          "A1b ★★★ **反证**：解出来的**不是载体**（`enemyUnits=1` 真的在筛，不是「全场上」）");

                // 灭自证：同一份夹具**拿掉 36 格** ⇒ 必须回到「出声 + 不结算」
                var tplBare = Template("B121D5TplBuff30NoCrit", Trig, ca: 30, pa: 0, ta: 10, fm: 0, mult: 0, crit: null);
                var rowBare = tplBare.TriggerAbilityIndices[0];
                int gapBefore = RuleCore.EnchantRowsDataGapForTest;
                var gotBare = RuleCore.ResolveBuffOthersTargetsForTest(ctx, 0, carrier, null, null, 10, tplBare, rowBare);
                CheckTrue(gotBare == null,
                          "A2 ★★★ **灭自证**：没有 36 格时 `ta=10` **仍然判不了**（返回 null）"
                        + "｜🧨 让它在没有数据时也「猜一个」⇒ 本行红");
                CheckTrue(RuleCore.EnchantRowsDataGapForTest > gapBefore,
                          "A3 ★★★ **不静默**：判不了时走了出声口（计数涨了）");
            }
        }

        // ---- B 组：ca 410 换口 ------------------------------------------------------
        Section("B `ca 410 triggerCodex` 换口：`ta=10` + 36 格**真登记**；没有 36 格仍出声");
        {
            var ctx = MakeBattle(out var carrier, out var opp);
            if (ctx == null || carrier == null) { CheckTrue(false, "B0 前提不成立：对局 / 载体没建起来"); }
            else
            {
                var crit = Criteria(10);
                crit.FriendlyUnits = 1; crit.IncludeMinions = 1; crit.EnemyUnits = 0;
                var tpl = Template("B121D5TplCodex410", Trig, ca: 410, pa: 0, ta: 10, fm: 0, mult: 0, crit: crit);
                var e = Attach(ctx, carrier, tpl);
                int gapBefore = RuleCore.EnchantRowsDataGapForTest;
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, e, carrier, 0, 3, Trig, "EarnedSkull", null, null);
                Check(n, 1, "B1 ★★★ **结论**：`ca 410`（`ta=10`）登记了 1 条 codex 触发"
                          + "｜🧨 退回 `ResolveEnchantmentRowTargets` ⇒ `ta=10` 判不了 ⇒ 0 ⇒ 红");
                Check(RuleCore.EnchantRowsDataGapForTest, gapBefore,
                      "B2 ★★★ **反证**：这一行**没有**再走「目标集判不了」的出声口（改前每次都会 +1）");
            }

            var ctx2 = MakeBattle(out var carrier2, out var opp2);
            if (ctx2 == null || carrier2 == null) { CheckTrue(false, "B3 前提不成立：第二场没建起来"); }
            else
            {
                var tpl2 = Template("B121D5TplCodex410NoCrit", Trig, ca: 410, pa: 0, ta: 10, fm: 0, mult: 0, crit: null);
                var e2 = Attach(ctx2, carrier2, tpl2);
                int gapBefore2 = RuleCore.EnchantRowsDataGapForTest;
                int n2 = RuleCore.RunEnchantmentLogicRowsForTest(ctx2, e2, carrier2, 0, 3, Trig, "EarnedSkull", null, null);
                Check(n2, 0, "B4 ★★★ **反证**：没有 36 格时 `ca 410`（`ta=10`）**仍然不结算**（返回 0）");
                CheckTrue(RuleCore.EnchantRowsDataGapForTest > gapBefore2,
                          "B5 ★★★ **不静默**：那一档仍然精确出声（计数涨了）");
            }
        }

        // ---- C 组：ca 130 stun -----------------------------------------------------
        Section("C `ca 130 stun`：只晕 criteria 选中的那个；己方那个不动");
        {
            var ctx = MakeBattle(out var carrier, out var opp);
            if (ctx == null || carrier == null) { CheckTrue(false, "C0 前提不成立：对局 / 载体没建起来"); }
            else
            {
                var crit = Criteria(10);
                crit.EnemyUnits = 1; crit.IncludeMinions = 1; crit.FriendlyUnits = 0;
                var tpl = Template("B121D5TplStun130", Trig, ca: 130, pa: 0, ta: 10, fm: 0, mult: 0, crit: crit);
                var e = Attach(ctx, carrier, tpl);
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, e, carrier, 0, 3, Trig, "EarnedSkull", null, null);
                Check(n, 1, "C1 ★★★ **结论**：`ca 130` 晕了 1 个单位｜🧨 删掉 `ca 130` 那一跳 ⇒ 0 ⇒ 红");
                CheckTrue(opp != null && opp.IsStunned,
                          "C2 ★★★ **结论**：**敌方**那个真的带上了 stun（`AddKeyword(Stun)` 跑到了）");
                CheckTrue(!carrier.IsStunned,
                          "C3 ★★★ **灭自证**：**载体自己没被晕**（目标集 = enemyUnits，不是「双方全场」）"
                        + "｜🧨 把目标集换成双方全场 ⇒ 本行红");
            }

            var ctx2 = MakeBattle(out var carrier2, out var opp2);
            if (ctx2 == null || carrier2 == null) { CheckTrue(false, "C4 前提不成立：第二场没建起来"); }
            else
            {
                // `traitsFilter = 100(stun) ∧ excludeTrait = 1` ⇒ **已带 stun 的单位被排除**（Earthquake 那两行就是这一档）
                var crit = Criteria(10);
                crit.EnemyUnits = 1; crit.IncludeMinions = 1; crit.FriendlyUnits = 0;
                crit.TraitsFilter = 100; crit.ExcludeTrait = 1;
                var tpl = Template("B121D5TplStun130Trait", Trig, ca: 130, pa: 0, ta: 10, fm: 0, mult: 0, crit: crit);
                var e = Attach(ctx2, carrier2, tpl);
                if (opp2 != null) opp2.AddKeyword(KeywordTable.Stun, 1);      // 先把它晕上 ⇒ criteria 应当排除它
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx2, e, carrier2, 0, 3, Trig, "EarnedSkull", null, null);
                Check(n, 0, "C5 ★★★ **灭自证**：`traitsFilter=100 ∧ excludeTrait=1` 把**已晕**的排除 ⇒ 这次一个都没晕"
                          + "｜🧨 排除失效 ⇒ 会被重晕 ⇒ 返回 1 ⇒ 红");
                CheckTrue(carrier2 != null && !carrier2.IsStunned,
                          "C6 ★★ **反证**：载体仍然没被晕");
            }
        }

        // ---- D 组：ca 185 stealMinionSelf -------------------------------------------
        Section("D `ca 185 stealMinionSelf`：目标 = **触发源**（不读目标表），且**永久**易主");
        {
            var ctx = MakeBattle(out var carrier, out var opp);
            if (ctx == null || carrier == null || opp == null) { CheckTrue(false, "D0 前提不成立：对局 / 载体没建起来"); }
            else
            {
                var tpl = Template("B121D5TplSteal185", Trig, ca: 185, pa: 0, ta: 20, fm: 0, mult: 0, crit: null);
                var e = Attach(ctx, carrier, tpl);
                bool onSide1Before = BoardHas(ctx, 1, opp);
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, e, carrier, 0, 3, Trig, "EarnedSkull", opp, null);
                CheckTrue(onSide1Before, "D1 前提：触发源原本在 1 侧");
                Check(n, 1, "D2 ★★★ **结论**：`ca 185` 易主成功（返回 1）"
                          + "｜🧨 删掉 `ca 185` 那一跳 ⇒ 0 ⇒ 红");
                CheckTrue(BoardHas(ctx, 0, opp),
                          "D3 ★★★ **结论**：触发源现在在 **0 侧**（`ChangeOwner` = 翻到当前所属的对侧）");
                CheckTrue(!BoardHas(ctx, 1, opp),
                          "D4 ★★★ **灭自证**：它**不再**在 1 侧（不是「两边都有」）"
                        + "｜🧨 换成「本回合 + 返还」那套（`TempControls`）⇒ 这里仍是 1 侧 ⇒ 红");
            }

            var ctx2 = MakeBattle(out var carrier2, out var opp2);
            if (ctx2 == null || carrier2 == null) { CheckTrue(false, "D5 前提不成立：第二场没建起来"); }
            else
            {
                var tpl2 = Template("B121D5TplSteal185Null", Trig, ca: 185, pa: 0, ta: 20, fm: 0, mult: 0, crit: null);
                var e2 = Attach(ctx2, carrier2, tpl2);
                int gapBefore = RuleCore.EnchantRowsDataGapForTest;
                int n2 = RuleCore.RunEnchantmentLogicRowsForTest(ctx2, e2, carrier2, 0, 3, Trig, "EarnedSkull", null, null);
                Check(n2, 0, "D6 ★★★ **反证**：触发源为空时 `ca 185` **不结算**（返回 0）");
                CheckTrue(RuleCore.EnchantRowsDataGapForTest > gapBefore,
                          "D7 ★★★ **不静默**：这一档精确出声（原版 `actingCard == null` 也是整支空转）");
            }
        }

        // ---- E 组：ca 236 scriptedActingCardAttack ----------------------------------
        Section("E `ca 236`：攻方 = **触发源**，只打 `targets[0]`");
        {
            var ctx = MakeBattle(out var carrier, out var opp);
            if (ctx == null || carrier == null || opp == null) { CheckTrue(false, "E0 前提不成立"); }
            else
            {
                // 再放一个 0 侧单位 ⇒ 目标集有 2 个候选；原版只取 `targets[0]`（= 打一个）
                var second = PlaceUnit(ctx, 0, 2, PlainUnit("B121D5E2nd"));
                var crit = Criteria(10);
                crit.FriendlyUnits = 1; crit.IncludeMinions = 1; crit.EnemyUnits = 0;
                crit.MaxTargets = 0;                       // 不裁剪 ⇒ 候选 = 两个
                var tpl = Template("B121D5TplActAttack236", Trig, ca: 236, pa: 0, ta: 10, fm: 0, mult: 0, crit: crit);
                var e = Attach(ctx, carrier, tpl);
                int hpC = carrier.Health, hpS = second != null ? second.Health : -1;
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, e, carrier, 0, 3, Trig, "EarnedSkull", opp, null);
                CheckTrue(n >= 1, "E1 ★★★ **结论**：`ca 236` 打出去了（返回 ≥1）｜🧨 删掉那一跳 ⇒ 0 ⇒ 红");
                bool carrierHit = carrier.Health < hpC, secondHit = second != null && second.Health < hpS;
                CheckTrue(carrierHit ^ secondHit,
                          "E2 ★★★ **灭自证**：**只有一个**目标掉血（原版 `GetItem(targets, 0)` 只打第一个）"
                        + $"｜实际 载体掉血={carrierHit} 第二个掉血={secondHit}"
                        + "｜🧨 改成逐目标打 ⇒ 两个都掉 ⇒ 红");
                Check(opp.AttacksThisTurn, 0,
                      "E3 ★★ **反证**：`ca 236` **不碰行动额度**（原版 `AddRedirectedAttack` 里 `RemoveAttackThisTurn` 0 命中）");
            }

            var ctx2 = MakeBattle(out var carrier2, out var opp2);
            if (ctx2 == null || carrier2 == null) { CheckTrue(false, "E4 前提不成立"); }
            else
            {
                var crit = Criteria(10);
                crit.FriendlyUnits = 1; crit.IncludeMinions = 1;
                var tpl = Template("B121D5TplActAttack236Null", Trig, ca: 236, pa: 0, ta: 10, fm: 0, mult: 0, crit: crit);
                var e = Attach(ctx2, carrier2, tpl);
                int gapBefore = RuleCore.EnchantRowsDataGapForTest;
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx2, e, carrier2, 0, 3, Trig, "EarnedSkull", null, null);
                Check(n, 0, "E5 ★★★ **反证**：触发源为空时 `ca 236` 不结算");
                CheckTrue(RuleCore.EnchantRowsDataGapForTest > gapBefore, "E6 ★★★ **不静默**：精确出声");
            }
        }

        // ---- F 组：ca 238 scriptedSelfCardAttackDontCount ---------------------------
        Section("F `ca 238`：攻方 = **载体**，预扣 + 退还（净 0）");
        {
            var ctx = MakeBattle(out var carrier, out var opp);
            if (ctx == null || carrier == null || opp == null) { CheckTrue(false, "F0 前提不成立"); }
            else
            {
                var crit = Criteria(10);
                crit.EnemyUnits = 1; crit.IncludeMinions = 1; crit.FriendlyUnits = 0;
                var tpl = Template("B121D5TplSelfAttack238", Trig, ca: 238, pa: 0, ta: 10, fm: 0, mult: 0, crit: crit);
                var e = Attach(ctx, carrier, tpl);
                int hp = opp.Health;
                int before = carrier.AttacksThisTurn;
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, e, carrier, 0, 3, Trig, "EarnedSkull", null, null);
                CheckTrue(n >= 1, "F1 ★★★ **结论**：`ca 238` 打出去了（返回 ≥1）｜🧨 删掉那一跳 ⇒ 0 ⇒ 红");
                CheckTrue(opp.Health < hp, "F2 ★★★ **结论**：被打的那个真掉血（声明层 + 结算真的跑了）");
                Check(carrier.AttacksThisTurn, before,
                      "F3 ★★★ **灭自证**：攻方的攻击次数**净不变**（原版 `dontCountAttack = 1`：预扣 + 退还）"
                    + "｜🧨 去掉退还 ⇒ 净 -1；去掉预扣 ⇒ 净 +1 ⇒ 红");
            }

            var ctx2 = MakeBattle(out var carrier2, out var opp2);
            if (ctx2 == null || carrier2 == null) { CheckTrue(false, "F4 前提不成立"); }
            else
            {
                var tpl2 = Template("B121D5TplSelfAttack238NoCrit", Trig, ca: 238, pa: 0, ta: 10, fm: 0, mult: 0, crit: null);
                var e2 = Attach(ctx2, carrier2, tpl2);
                int gapBefore = RuleCore.EnchantRowsDataGapForTest;
                int n2 = RuleCore.RunEnchantmentLogicRowsForTest(ctx2, e2, carrier2, 0, 3, Trig, "EarnedSkull", null, null);
                Check(n2, 0, "F5 ★★★ **反证**：没有 36 格时 `ca 238`（`ta=10`）仍然不结算");
                CheckTrue(RuleCore.EnchantRowsDataGapForTest > gapBefore, "F6 ★★★ **不静默**：精确出声");
            }
        }

        // ---- G 组：`unitsToSummon` 卡 id 列**已由 `B121-D4` 就位**；本组是「夹具没带这一列」的反证 -------------
        Section("G 反证：`ca 70 / 100`（夹具未带 `unitsToSummon` 列 ⇒ `null` = **未知**、⛔ 不当空表）⇒ 仍然 **精确出声、不静默**");
        {
            int[] cas = { 70, 100 };
            for (int i = 0; i < cas.Length; i++)
            {
                int ca = cas[i];
                var ctx = MakeBattle(out var carrier, out var opp);
                if (ctx == null || carrier == null) { CheckTrue(false, $"G{ca}a 前提不成立"); continue; }
                var tpl = Template("B121D5TplGap" + ca, 0, ca: ca, pa: 0, ta: 0, fm: 0, mult: 0, crit: null);
                var e = Attach(ctx, carrier, tpl);
                int gapBefore = RuleCore.EnchantRowsDataGapForTest;
                int n = RuleCore.RunEnchantmentLogicRowsForTest(ctx, e, carrier, 0, 3, Trig, "EarnedSkull", null, null);
                Check(n, 0, $"G{ca}.1 ★★★ **反证**：`ca {ca}` 仍然不结算（返回 0）");
                CheckTrue(RuleCore.EnchantRowsDataGapForTest > gapBefore,
                          $"G{ca}.2 ★★★ **不静默**：走了「已知 ca、载荷缺」的出声口"
                        + "｜🧨 把它顺手接成「假执行」并静默 return 0 ⇒ 本行红");
            }
        }

        Summary();
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>一张附魔模板卡：1 条 ability / 1 行 logic；`crit` 非 null 时一起挂 36 格。</summary>
    static CardDef Template(string id, int trigger, int ca, int pa, int ta, int fm, int mult,
                            CardDef.CountSetSpec crit)
    {
        var c = new CardDef(id, id, "enchantment", "", "", "", 0, 0, 0, 0, null);
        c.AttachTriggerAbilityIndices(new[]
        {
            new CardDef.TriggerAbilityIndexSpec
            { Ab = 0, Lg = 0, Trigger = trigger, TriggerValue = 0, Ca = ca, Ta = ta, Pa = pa, Fm = fm, Mult = mult },
        });
        c.AttachAbilityTriggers(new[] { trigger });
        if (crit != null)
            c.AttachAbilityTargetCriteria(new[]
            {
                new CardDef.TargetCriteriaRowSpec { Ability = 0, Trigger = trigger, Logic = 0, Criteria = crit },
            });
        return c;
    }

    /// <summary>一份**全格都已知**的 36 格 criteria（`-1` 会让 `TargetCriteriaEval` 返回「判不了」）。
    /// 缺省 = 原版「明确不筛」的全 0；调用方按需改那几格。</summary>
    static CardDef.CountSetSpec Criteria(int ta)
    {
        var s = new CardDef.CountSetSpec();
        s.PlayerTurn = -1;                     // 结构上不适用（同生成器：`has_player_turn=False`）
        s.TargetsAffected = ta;
        s.ChosenArmy = 0; s.ArmyFilter = 0; s.KeywordFilter = 0; s.TraitsFilter = 0;
        s.MaxTargets = 0; s.TargetSelectMethod = 0;
        s.AttackFilter = 0; s.AttackFilterOption = 0;
        s.CurrentHealthFilter = 0; s.CurrentHealthFilterOption = 0;
        s.ManaCostFilter = 0; s.ManaCostFilterOption = 0;
        s.RarityFilter = 0; s.RarityFilterOption = 0;
        s.SpellType = 0;
        s.FriendlyUnits = 0; s.EnemyUnits = 0;
        s.IncludeHero = 0; s.IncludeMinions = 0; s.IncludeSpells = 0; s.IncludeWhispers = 0;
        s.IncludeWeapons = 0; s.IncludeRemnants = 0; s.IncludeHeroParts = 0;
        s.IncludeSelf = 0; s.ExcludeNotSelf = 0; s.ExcludeSimilarCards = 0; s.ExcludeTargetted = 0;
        s.ExcludeFriendlyHero = 0; s.ExcludeEnemyHero = 0; s.ExcludeRace = 0; s.ExcludeTrait = 0;
        s.SelectFromChooseResolution = 0;
        s.FilterSpecificCardIds = new string[0];
        s.ExcludeEnchantmentIds = new string[0];
        return s;
    }

    /// <summary>载体卡（不带 ability / 正文）。</summary>
    static CardDef PlainUnit(string id)
    {
        return new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 20, 0, null, subtype: "Infantry");
    }

    /// <summary>建一局：双方各 6 张「填充卡」+ 一张督军；0 号玩家格 3 放载体、1 号玩家格 3 放一个敌兵。</summary>
    static BattleContext MakeBattle(out UnitState carrier, out UnitState opp)
    {
        var heroA = new CardDef("B121D5HeroA", "B121D5HeroA", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("B121D5HeroB", "B121D5HeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var carrierCard = PlainUnit("B121D5Carrier");
        var deckA = new List<CardDef> { heroA, carrierCard };
        var deckB = new List<CardDef> { heroB };
        for (int i = 0; i < 6; i++) { deckA.Add(PlainUnit("B121D5FillA" + i)); deckB.Add(PlainUnit("B121D5FillB" + i)); }
        var ctx = RuleCore.NewBattle(deckA, deckB, seed: 0, shuffle: false, cardPool: new List<CardDef>());
        carrier = PlaceUnit(ctx, 0, 3, carrierCard);
        opp = PlaceUnit(ctx, 1, 3, PlainUnit("B121D5Opp"));
        return ctx;
    }

    static UnitState PlaceUnit(BattleContext ctx, int p, int slot, CardDef card)
    {
        if (ctx == null || card == null) return null;
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    static bool BoardHas(BattleContext ctx, int p, UnitState u)
    {
        if (ctx == null || u == null || p < 0 || p > 1) return false;
        var b = ctx.Players[p].Board;
        for (int i = 0; i < b.Length; i++) if (ReferenceEquals(b[i], u)) return true;
        return false;
    }

    /// <summary>挂一件附魔（唯一写口 = `Enchantments.Attach`）。</summary>
    static ActiveEnchantment Attach(BattleContext ctx, UnitState carrier, CardDef tpl)
    {
        if (ctx == null || carrier == null || carrier.Instance == null || tpl == null) return null;
        var eff = Enchantments.Attach(ctx, carrier.Instance, tpl, Enchantments.BuffTypeSingleUse);
        return eff != null ? eff.Enchantment : null;
    }

    // ==================================================================
    //  最小断言框架
    // ==================================================================

    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B121_D5 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B121_D5 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
