// B110_C09_Assert.cs — 死码 C09：`BattleManager.ReplaceHeroPower`（引擎动作 24「换督军主动能力」）
//
// ============================ 它盯的是什么 ============================
// `RuleEngine/Core/RuleCorePartial_B110C09.cs` 的 `RuleCore.ReplaceHeroPower`：
//   ① **只入队**（原版 `BattleManager__ReplaceHeroPower.c:2-140` = 拼 `BattleAction{0x18}` + `AddAutoActionToQueue`，
//      本体**不干任何别的**）⇒ 入队后、泵队前，督军的主动能力**必须还是旧的**（这条就是灭自证）；
//   ② 泵队（= 动作结算）之后才换成新卡，且**费用 / 有无主动能力**都跟着新卡走；
//   ③ `priority` 越界 ⇒ 照原版 `AddAutoActionToQueue.c:224-229` **LogError + 丢弃**（⛔ 不 clamp、不占任何一道）；
//   ④ `newPower == null` ⇒ 入队可以，结算时 **LogError + 什么都不做**（原版 `replaceAbility(40)` 的
//      `enchantment == null` 支）—— ⛔ 不是「清回卡面」。
//
// 🔴 **本件同时钉死一条【反证】**（判据与二进制冲突时以二进制为准，见报告 §没查清的部分）：
//   原版 `ScriptedActionType`（`d:/2/tools/il2cpp_out/dump.cs:42004`，22 个值）**没有 24**；
//   `replaceHeroPower = 24` 是 **`BattleActionType`（引擎动作）** 的号。⇒ **不存在「教程动作 24」**，
//   `TutorialScript.ExecuteCore`（一个 `ScriptedActionType` 的 switch）**不该**也不会为它加分支。
//   → 断言 D 把这条钉住：谁以后往 `ScriptedActionType` 里编一个 24，D1 立刻红。
//
// ============================ 怎么接 ============================
// `RuleEngineTest` 的 `Run()` 里加一句 `B110C09();`（零行数变化：追加在同一物理行）。
// ⚠️ 入口**故意不叫** `Run` / 不带 `Test` 前缀（同 `B107_W19_Assert` / `B98G7`）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · `ReplaceHeroPower` 里**当场**写 `HeroPowerOverride`（不等泵队）⇒ **A4** 红（「入队后仍是旧的」）。
//  · 把 `priority` 越界改成 clamp ⇒ **B2/B3** 红。
//  · 把 `newPower == null` 改成「清回卡面」⇒ **C3** 红（那条断的是「换过之后再来一条 null，**仍然是**新卡」）。
//  · 把 `HeroPowerHasActiveAbility` 写成「恒 `HasActiveAbility == 1`」⇒ **A7** 红（新卡本来就是 1，
//    但基线那张夹具是 `-1` 未知 ⇒ A2 那条「换之前是 false」也会红）。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件唯一入口（由宿主 `Run()` 里一句 `B110C09();` 调；⛔ 不叫 `Run` / 不带 `Test` 前缀）。</summary>
    public static void B110C09()
    {
        Section("🆕 B110_C09：`BattleManager.ReplaceHeroPower`（动作 24）—— 只入队 · 结算才换 · 反证钉死");
        B110C09EnqueueOnly();
        B110C09PriorityGate();
        B110C09NoScriptedKind24();
    }

    // ==================================================================
    //  夹具：一张「新的主动能力卡」
    //  能力正文走 `keywords` 那一路（与 `RuleEngineTest.cs:26281` 的 `FixtureSlayCaster` 同形）
    // ==================================================================
    static CardDef B110C09Power(string id, int mana, string abilityText)
    {
        var c = new CardDef(id, id, "unit", "", "common", "Test", 0, 0, 0, 0,
                            new[] { abilityText },
                            subtype: "Warlord", hasActiveAbility: 1);
        c.AttachActiveAbilityCost(mana, 0, 0, 0);      // 原版 `ActiveAbility.manaCost`（`CardDef.ActiveAbilityManaCost`）
        return c;
    }

    // ==================================================================
    //  A · 只入队；泵队之后才换（含费用 / 能力正文 / hasActiveAbility 三格）
    // ==================================================================
    static void B110C09EnqueueOnly()
    {
        var power = B110C09Power("B110C09Power", 3, "Ability: Damage 4 EnemyUnit");
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var w = ctx.Players[0].Warlord;
        CheckTrue(w != null, "A0 （前提）0 号位有督军");
        if (w == null) return;
        // 前提：能力正文 / 费用都真的落到了这张夹具卡上（没有这两条，「按新卡」那一组断言没有对象）。
        CheckTrue(power.Ability != null, "A0 （前提）新能力卡的 `Ability` 解析出来了（`Ability: Damage 4 EnemyUnit`）");
        Check(power.ActiveAbilityManaCost, 3, "A0 （前提）新能力卡的费用 = 3（`AttachActiveAbilityCost`）");

        // 基线：没有覆盖 ⇒ 读卡面
        CheckTrue(ReferenceEquals(w.HeroPowerCard, w.Card),
                  "A1 初始没有覆盖位 ⇒ `HeroPowerCard` **就是卡面那张**（`ReferenceEquals`）");
        Check(w.Card.HasActiveAbility, CardDef.GateUnknown,
              "A2 （前提）夹具督军的卡面 `hasActiveAbility` = **-1 未知** ⇒ 下面 A7 的翻面才有意义");
        CheckTrue(!RuleCore.HeroPowerHasActiveAbility(ctx, 0), "A3 换之前「没有主动能力」（卡面 -1）");

        int laneBefore = ctx.ActionQueue.CountOf(ActionPriority.Instant);
        int totalBefore = ctx.ActionQueue.Count;
        CheckTrue(RuleCore.ReplaceHeroPower(ctx, 0, power, (int)ActionPriority.Instant),
                  "A4 （前提）`ReplaceHeroPower` 接受这条动作");

        // 🔴 **灭自证**：入队了，但**还没有结算** ⇒ 技能必须**仍然是旧的**。
        Check(ctx.ActionQueue.Count, totalBefore + 1, "A4 ★★ 队列里**多了一条**（`Count` +1）—— 原版这一跳只 `List.Add`");
        Check(ctx.ActionQueue.CountOf(ActionPriority.Instant), laneBefore + 1,
              "A5 ★ `priority = 1` ⇒ 进的是 **Instant 道**（原版 `+0x358`，`AddAutoActionToQueue.c:47-171`）");
        CheckTrue(ReferenceEquals(w.HeroPowerCard, w.Card),
                  "A6 ★★ **灭自证**：入队后、泵队前，主动能力**仍是卡面那张** —— 证明「只入队，不立即生效」"
                + "｜🧨 改成当场写覆盖位 ⇒ 本条红");
        CheckTrue(!RuleCore.HeroPowerHasActiveAbility(ctx, 0), "A6' 同上：「没有主动能力」这一格也没变");

        // 泵队 = 动作结算
        RuleCore.PumpActionQueues(ctx);

        CheckTrue(ReferenceEquals(w.HeroPowerCard, power),
                  "A7 ★★ 泵队之后主动能力 = **新卡**（`ReferenceEquals`）");
        CheckTrue(ReferenceEquals(RuleCore.HeroPowerCard(ctx, 0), power),
                  "A8 ★ `RuleCore.HeroPowerCard` 读口与单位那一格一致（唯一读口）");
        CheckTrue(ReferenceEquals(RuleCore.HeroPowerAbility(ctx, 0), power.Ability),
                  "A9 ★ 能力正文按**新卡**（`HeroPowerAbility`）");
        Check(RuleCore.HeroPowerManaCost(ctx, 0), 3,
              "A10 ★ **费用按新卡** = 3（原版 `ActiveAbility.manaCost`；旧卡面没有这一列 ⇒ 换前是 -1）");
        CheckTrue(RuleCore.HeroPowerHasActiveAbility(ctx, 0),
                  "A11 ★ 「有主动能力」翻成真（原版 `CheckReplacedAbility.c:19-40` 无条件置 `+0x220 = 1`）");

        // C 段并入同一局：**换过之后再来一条 `newPower == null`** ⇒ 出声、**不换**（⛔ 不是清回卡面）
        CheckTrue(RuleCore.ReplaceHeroPower(ctx, 0, null, (int)ActionPriority.Instant),
                  "C1 （前提）`newPower == null` **也能入队**（原版构造动作时不校验）");
        CheckTrue(ReferenceEquals(w.HeroPowerCard, power),
                  "C2 ★ 入队后仍是新卡（同上，只入队）");
        RuleCore.PumpActionQueues(ctx);
        CheckTrue(ReferenceEquals(w.HeroPowerCard, power),
                  "C3 ★★ `null` 那条结算时 **LogError + 什么都不做** ⇒ 能力**仍然是**刚才那张新卡"
                + "｜🧨 把 null 支改成「清回卡面」⇒ 本条红");
    }

    // ==================================================================
    //  B · `priority` 闸：0..3 之外 ⇒ 丢弃（原版 `AddAutoActionToQueue.c:224-229`）
    // ==================================================================
    static void B110C09PriorityGate()
    {
        var power = B110C09Power("B110C09Power2", 1, "Ability: Damage 1 EnemyUnit");
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var w = ctx.Players[0].Warlord;
        CheckTrue(w != null, "B0 （前提）0 号位有督军");
        if (w == null) return;
        int before = ctx.ActionQueue.Count;

        CheckTrue(!RuleCore.ReplaceHeroPower(ctx, 0, power, 4),
                  "B1 ★★ `priority = 4` ⇒ **拒绝**（原版这一档是 `LogError` + `return`，⛔ 不 clamp）"
                + "｜🧨 改成 clamp 到某一道 ⇒ 本条红");
        CheckTrue(!RuleCore.ReplaceHeroPower(ctx, 0, power, -1), "B2 ★ `priority = -1` ⇒ 拒绝");
        Check(ctx.ActionQueue.Count, before, "B3 ★★ 被拒的那两条**一条都没进队**（`Count` 不变）");

        // 座位越界 / 空督军：同样是有声拒绝（⛔ 不静默塞一条注定空转的动作）
        CheckTrue(!RuleCore.ReplaceHeroPower(ctx, 9, power, 1), "B4 座位越界 ⇒ 拒绝");
        CheckTrue(!RuleCore.ReplaceHeroPower(null, 0, power, 1), "B5 `ctx == null` ⇒ 拒绝（不抛）");
        Check(ctx.ActionQueue.Count, before, "B6 全部拒绝之后队列仍然没变");
    }

    // ==================================================================
    //  D · 🔴 反证钉死：**不存在「教程动作 24」**
    //   判据 = 原版 `ScriptedActionType`（`dump.cs:42004`，22 个值）**没有 24**；
    //          `replaceHeroPower` 是 `BattleActionType`（引擎动作）的 24 号。
    // ==================================================================
    static void B110C09NoScriptedKind24()
    {
        CheckTrue(!System.Enum.IsDefined(typeof(ScriptedActionType), 24),
                  "D1 ★★ `ScriptedActionType` **没有 24 这个值**（原版 22 个值里就没有）"
                + " ⇒ `ExecuteCore` 不该也不会为它加分支｜🧨 谁往枚举里编一个 24 ⇒ 本条红（那是造原版没有的枚举值）");
        CheckTrue(System.Enum.IsDefined(typeof(BattleActionType), 24),
                  "D2 ★ 反过来说：`BattleActionType` **有** 24（= `replaceHeroPower`）—— 两套枚举别混");
        Check((int)BattleActionType.replaceHeroPower, RuleCore.ActionTypeReplaceHeroPower,
              "D3 我们的枚举值与 `RuleCore.ActionTypeReplaceHeroPower` 一致 = 24");

        // 数据侧：6 关脚本里**一条** `data[].type == 24` 都不该有
        var stages = TutorialData.Stages;
        CheckTrue(stages != null, "D4 （前提）教程关卡数据已装载（`TutorialData.Stages` 非 null）");
        if (stages == null) return;
        int turns = 0, actions = 0, kind24 = 0;
        for (int i = 0; i < stages.Length; i++)
        {
            var st = stages[i];
            if (st == null) continue;
            CountScriptedKind24(st.turns, ref turns, ref actions, ref kind24);
            CountScriptedTurn(st.preMulligan, ref turns, ref actions, ref kind24);
            CountScriptedTurn(st.onVictory, ref turns, ref actions, ref kind24);
            CountScriptedTurn(st.onDefeat, ref turns, ref actions, ref kind24);
            CountScriptedAction(st.actionOnPlayerResign, ref actions, ref kind24);
        }
        CheckTrue(actions > 0, "D5 （前提）扫到了脚本动作（实得 " + actions + " 条 / " + turns + " 个 turn）");
        Check(kind24, 0,
              "D6 ★★ 教程数据里 `data[].type == 24` 的条目 = **0**（原版 `ScriptedActionType` 没有这一档 ⇒ 造不出来）");
    }

    static void CountScriptedKind24(TutorialTurn[] ts, ref int turns, ref int actions, ref int kind24)
    {
        if (ts == null) return;
        for (int i = 0; i < ts.Length; i++) CountScriptedTurn(ts[i], ref turns, ref actions, ref kind24);
    }

    static void CountScriptedTurn(TutorialTurn t, ref int turns, ref int actions, ref int kind24)
    {
        if (t == null) return;
        turns++;
        var arr = t.actions;
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++) CountScriptedAction(arr[i], ref actions, ref kind24);
    }

    static void CountScriptedAction(TutorialAction a, ref int actions, ref int kind24)
    {
        if (a == null || a.data == null) return;
        for (int i = 0; i < a.data.Length; i++)
        {
            var d = a.data[i];
            if (d == null) continue;
            actions++;
            if (d.type == 24) kind24++;
        }
    }
}
