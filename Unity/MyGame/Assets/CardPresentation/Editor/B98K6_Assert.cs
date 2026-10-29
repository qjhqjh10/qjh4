// B98K6_Assert.cs — 执行代理 `B98_K6`（第九十八会话 · 波 10 · 战斗线）的断言集
//
// ============================ 它盯的是什么（四条）============================
//   账 `战斗逻辑驱动` 的四条：
//     · `:423`  `BattleManager.IsScriptedStageAvailable` —— 表现层那个**非原版前置**
//                `tut.CurrentTurn == null` 已从 `BattleDriver.TutorialWaitingUseAbility` 撤掉。
//                ⚠️ 这一条**测不到 driver**（它挂在 `BattleDriver` 私有实例上）⇒ 本文件只钉**安全网**：
//                下面 `TutorialRules.IsWaitingUseAbility(null, …)` 必须 `false` 且**不抛** ——
//                那正是「撤掉前置后行为不变」的充分条件。
//     · `:852`  `CardScript.RemoveEffect(CardEffect, bool)` 的两处残留：
//                ② `case 5 swapStats` 的**残留清理**（原版 `ChangeBaseAttack` / `ChangeMaxHealth`
//                   顺手摘/折算旧 addStats 记录）→ `UnitState.RestoreSwapStatsBase()`
//                ③ `case 45 addStatsDropPod` 的**攻击半**（原版 `currentDropPodAttack -= meleeAttackChange`）
//     · `:233`  `BattleManager.CanUseActiveAbility` 的 ① —— AI 钩子
//                `AiScoring.ActiveAbilityManaCost` 从「产品恒 `null`」改成**接到卡表真值**
//     · `:1379` / `:1281` 的 (a) —— `SW9` 那条 `ca=320 followAfterResolution` 行
//                从 `gap:kind-unknown` 改成 `gap:kind-continuation`（说清原因，⛔ 不硬配到 op 上）
//
//   判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B98_J0_战斗剩余终局.md`（852 / 233 / 1379 那三行）
//                            ＋ `资料/完全复刻/报告/B98_K6.md`
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A4：**不**调 `RemoveEffectRecord` 的同一套夹具 ⇒ 近战 5 / 上限 8 / 3 条修饰。
//         A1/A2/A3 与之**结构上不可能同时满足** ⇒ 「帮手什么都没做」时 A1–A3 必红。
//   · B3：改回旧的 `🔶 自己设计`（`KwValue(droppod)` 那套）⇒ B3 实得 ±≠0 ⇒ 红；
//         同时 B1/B2 会停在 2/0 ⇒ 也红。两条**互相钉死**。
//   · C2/C3：钩子若被写死成任何常数，**不可能**同时读到 3 与 7 ⇒ 必有一条红。
//   · C4/C5：把「未知」当成 0 ⇒ 实得 0 ≠ -1 ⇒ 红（本仓红线：没判据就不猜）。
//   · D3：把 `IsContinuationOnlyGroup` 放宽成「有 320 就算」⇒ `AM38` 也会掉出 `attached` ⇒ 红。
//
// ============================ 怎么用（宿主）============================
//   `RuleEngineTest.Run()` 里插一行：`Step(TestB98K6);`
//   （本文件与 `B98K4_Assert.cs` 同形 —— `partial class RuleEngineTest`，复用夹具 `ProbeBattle` /
//    `Place` / `Check` / `CheckTrue` / `Section`，⛔ 不另抄一套。）
//   ⚠️ **本批白名单不含宿主** ⇒ 这一行要**调度台**加；加之前本文件一条都不跑（如实标）。
//   ⚠️ 本文件**不建 GameObject、不建资产、不写盘**；只用 `new CardDef` / `BattleContext` 的内存局面。
//
// ⚠️ **另一处必须同步改的既有断言（本批白名单外）**：`CardPresentation/Editor/RuleEngineTest_EngineE1.cs:431-432`
//    现在钉着 `SW9.ActiveAbilityMap == "gap:kind-unknown"` —— 本次改成了 `gap:kind-continuation` ⇒
//    那两行**会红**，需调度台同步改（同文件 `:303-305` 的**计数**不用改：`other` 那一档会接住它，
//    `noOp + ambiguous + unknown + other` 仍是 1、`attached` 仍是 86）。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>B98_K6 的全部断言（宿主：`RuleEngineTest.Run` 里 `Step(TestB98K6);`）。</summary>
    public static void TestB98K6()
    {
        Section("B98_K6 —— 账 423（前置撤除的安全网）/ 852（case 5·45）/ 233（AI 钩子）/ 1379·1281 (a)");

        TestB98K6SwapStats();
        TestB98K6DropPodRemoval();
        TestB98K6AiHook();
        TestB98K6ScriptedStageAndContinuation();
    }

    // ==============================================================================================
    //  A · 账 852 ②：`case 5 swapStats` 的残留清理
    // ==============================================================================================
    static void TestB98K6SwapStats()
    {
        // 卡面：近战 3 · 生命 5
        var card = new CardDef("K6SWAP", "K6SWAP", "unit", "", "common", "Test", 1, 3, 5, 0, null);

        // ---- A0/A4：对照组 —— **不**摘那条 swapStats 记录 ----
        var ctx0 = ProbeBattle(new CardDef[0], new CardDef[0]);
        var u0 = Place(ctx0, 0, 3, card);
        u0.AddStatMod("attack", 2);                                    // 非 extrinsic 近战 +2
        u0.AddStatMod("health", 1);                                    // 非 extrinsic 生命 +1
        u0.AddStatMod("health", 2, null, extrinsic: true);             // extrinsic 生命 +2
        Check(u0.Attack, 5, "(前提 A0) 近战 = 卡面 3 + 2 = 5");
        Check(u0.MaxHealth, 8, "(前提 A0) 上限 = 5 + 1(非 ext) + 2(ext) = 8");
        Check(u0.StatMods.Count, 3, "(前提 A0) 三条修饰都在");

        // ---- A1–A3：真摘 ----
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        var u = Place(ctx, 0, 3, card);
        u.AddStatMod("attack", 2);
        u.AddStatMod("health", 1);
        u.AddStatMod("health", 2, null, extrinsic: true);
        u.RemoveEffectRecord(new UnitState.EffectRecord { BuffType = 5 });   // swapStats

        Check(u.Attack, 3,
              "★ A1 swapStats 摘除 ⇒ **非 extrinsic 的近战修饰整条摘掉**、base 回卡面 ⇒ 3"
            + "｜🧨 帮手什么都不做（旧口径）⇒ 实得 5 ⇒ 红");
        Check(u.StatMods.Count, 1,
              "★★ A2 只剩 **extrinsic 那一条**（原版 `ChangeBaseAttack`/`ChangeMaxHealth` 只摘非 extrinsic 的 addStats 记录）"
            + "｜🧨 一起把 extrinsic 也摘了 ⇒ 实得 0 ⇒ 红");
        Check(u.MaxHealth, 9,
              "★★ A3 上限 = 卡面 5 + 折进 base 的 extrinsic 2 + **仍在投影里的** extrinsic 2 = 9"
            + "（原版 `ChangeMaxHealth` 就是这样双计，§18·0 照抄）"
            + "｜🧨 帮手什么都不做 ⇒ 8 ⇒ 红；🧨 把 extrinsic 也摘了 ⇒ 7 ⇒ 红");

        Check(u0.Attack, 5, "★★ A4 灭自证：**没摘**的那张仍是 5 ⇒ A1 的红不是「攻击恒 3」造成的");
    }

    // ==============================================================================================
    //  B · 账 852 ③：`case 45 addStatsDropPod` 的攻击半
    // ==============================================================================================
    static void TestB98K6DropPodRemoval()
    {
        var card = new CardDef("K6POD", "K6POD", "unit", "", "common", "Test", 1, 1, 6, 0, null);
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        var u = Place(ctx, 0, 3, card);
        u.CurrentDropPodHealth = 2;      // 原版 `GameStaticData.dropPodMaxHealth`
        u.CurrentDropPodAttack = 0;      // 原版 `GameStaticData.dropPodAttack`

        u.RemoveEffectRecord(new UnitState.EffectRecord { BuffType = 45, HealthChange = 1, MeleeChange = 2 });

        Check(u.CurrentDropPodHealth, 1,
              "★ B1 `currentDropPodHealth(+0xcc) -= healthChange(+0x60)` ⇒ 2 − 1 = 1"
            + "｜🧨 改回旧口径（写 `droppod` 关键词）⇒ 停在 2 ⇒ 红");
        Check(u.CurrentDropPodAttack, -2,
              "★★ B2 `currentDropPodAttack(+0xe0) -= meleeAttackChange(+0x48)` ⇒ 0 − 2 = −2（**攻击半**，本次新补）"
            + "｜🧨 只做血量那半 ⇒ 停在 0 ⇒ 红");
        Check(u.KwValue(KeywordTable.DropPod), 0,
              "★★ B3 灭自证：`droppod` **关键词那一本账没被碰**（旧口径会在这里写 ±1）"
            + "｜🧨 两本账同时写 ⇒ 实得 ≠0 ⇒ 红");
    }

    // ==============================================================================================
    //  C · 账 233 ①：AI 钩子接到卡表真值
    // ==============================================================================================
    static void TestB98K6AiHook()
    {
        var c3 = new CardDef("K6AI3", "K6AI3", "unit", "", "common", "Test", 1, 1, 3, 0, null);
        c3.AttachActiveAbilityCost(3, 0, 0, 0);
        var c7 = new CardDef("K6AI7", "K6AI7", "unit", "", "common", "Test", 1, 1, 3, 0, null);
        c7.AttachActiveAbilityCost(7, 0, 0, 0);
        var cUnknown = new CardDef("K6AIU", "K6AIU", "unit", "", "common", "Test", 1, 1, 3, 0, null);

        var hook = AiScoring.ActiveAbilityManaCost;
        CheckTrue(hook != null,
                  "★ C1 产品默认**已接线**（`AiScoring.ActiveAbilityManaCost` 初值器 = 读 `CardDef.ActiveAbilityManaCost`）"
                + "｜🧨 改回「字段无初值 / 末尾复位成 `null`」⇒ 红");
        if (hook == null) return;
        Check(hook(c3), 3, "★ C2 卡表值 3 ⇒ 钩子读 3");
        Check(hook(c7), 7,
              "★★ C3 另一张读 7 ⇒ 钩子是**真读卡**、不是常数"
            + "｜🧨 写死成任何常数都不可能同时满足 C2 与 C3 ⇒ 红");
        Check(hook(cUnknown), -1,
              "★★ C4 旧表 / 夹具直接 `new CardDef` ⇒ `-1`（未知）"
            + "｜🧨 把「未知」当 0 ⇒ 实得 0 ⇒ 红（原版 `cost > manaLeft` 会被 0 静默吞掉）");
        Check(hook(null), -1, "C5 `null` 卡 ⇒ `-1`（不抛）");
    }

    // ==============================================================================================
    //  D · 账 423（安全网）+ 1379/1281 (a)
    // ==============================================================================================
    static void TestB98K6ScriptedStageAndContinuation()
    {
        // ---- D1：撤掉 `tut.CurrentTurn == null` 前置的**安全网** ----
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        CheckTrue(TutorialRules.IsWaitingUseAbility(null, 0, ctx, "SW9") == false,
                  "★ D1 `turn == null ⇒ false`（且**不抛**）—— 账 423 敢把那个非原版前置撤掉，靠的就是这一句"
                + "｜🧨 给 `IsWaitingUseAbility` 去掉 `turn == null` 早退 ⇒ 这里 NRE ⇒ 红");

        // ---- D2/D3：`ca ∈ {320,325}` 的「后续结算」组 ----
        var pool = CardDatabase.Load();
        var sw9 = CardDatabase.FindById(pool, "SW9");
        CheckTrue(sw9 != null && sw9.ActiveAbilityMap == "gap:kind-continuation",
                  "★★ D2 `SW9`（`activeAbilityLogic[0].ca = [320]`）⇒ `gap:kind-continuation`"
                + "（不是 `kind-unknown`：这一行**没有 op 可挂**，机制在 `EmitFollowUpResolution` 那一侧；"
                + "而且原版自己也不调度它 —— `FollowUpSchedulerOf` 的 doc 把它列在「原版本身不触发」里）"
                + "｜🧨 把 `continuationOnly` 那个三目删掉 ⇒ 回到 `kind-unknown` ⇒ 红");
        var am38 = CardDatabase.FindById(pool, "AM38");
        CheckTrue(am38 != null && am38.ActiveAbilityMap == "attached",
                  "★★ D3 灭自证：`AM38`（正常的能力种类行）**仍是 `attached`**"
                + "｜🧨 把 `IsContinuationOnlyGroup` 放宽成「只要有 320 就算」/ 恒 true ⇒ 这张掉出 `attached` ⇒ 红");
    }
}
