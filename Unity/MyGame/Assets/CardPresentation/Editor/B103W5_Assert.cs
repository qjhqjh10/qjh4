// B103W5_Assert.cs — 执行代理 `B103_W5`（2026-10-30）的断言：账 `战斗逻辑驱动:1282` / `:1380`
//   （`RawCardScript.ResolveActiveAbility` 的两个重载）里**休眠·机制真缺**那批中，
//   本代理落地的那 5 支执行器（`RuleCorePartial_B103W5.cs`）：
//     · `220 resetActions` · `290 missAction` · `280 revealOrder` · `375 triggerSacrifice` · `330 drawUntilEqual`
//   ＋ 第三棒（`B103_W5c`）补的两支**新动作类型**：`162 disappearFromHand` · `310 displayFromHand`
//     （动作类型本体在 `RuleEngine/Core/ActionPump.cs` 类尾；行执行器在 `RuleCorePartial_B103W5c.cs`）。
//   施工单 / 分批表 → `资料/完全复刻/判据/B102_R4_C类大工程两条.md` §①，⛔ 不在这里抄第二份。
//
// ============================ 它盯的是什么 ============================
//   原版 `AbilityLogic.PlayAbility` 按 `chosenAbility`（`AbilityLogic + 0x10`）逐支分派；
//   上面五支的**共同形状** = 「逐目标调一个原子动作」（`220/290/280/375`）或
//   「按双方手牌差抽牌」（`330`）。本文件钉两件事：
//     ① **机制真的在**（执行体跑一次，原子动作的**可观测字段**按原版变）；
//     ② **闸对**（解不出目标集时**不猜**：返回 0 且一个字段都不动）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   A-220b   把「解不出目标集」改成「己方全体/对面全体」那种近似 ⇒ 计数被减 ⇒ 红。
//   A-220c   把 `ResetActions` 写成「清零」而不是「−1」⇒ 实得 0 ⇒ 红。
//   A-290b   把 `MissActions` 写成「只打日志不记账」⇒ 计数不动 ⇒ 红。
//   A-375b   把 `sacrifice` 登记写成无条件返回 1（不真入队）⇒ 用 `chosen == null` 那一条对上 ⇒ 红。
//   A-330b   330 在「取不到规格」时默认 `playersAffected = 0 (Both)` 硬抽 ⇒ 手牌数变 ⇒ 红。
//   A-0b     删掉 `RuleCore.cs:17364` 闸里的 `|| ca == 220` ⇒ 红（那一行进不了计划表 ⇒ 执行器成死代码）。
//   A-0c     把闸写成恒 true ⇒ 红。
//   A-0d     删掉 `EffectResolver.cs:18533` 里的 `if (ca == 220)` ⇒ 落回 `RunActiveRowOps`（没有正文 op）⇒ 红。
//   ⚠️ **A-220…A-330 那 5 段是「机制存在 + 闸对」**；**A-0 段才是「接线已通」**
//      （ca 分派口与计划表闸的补丁由**本代理自己**在调度台扩容白名单后打上，见 §A-0 的注释块）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   接在 `CardPresentation/Editor/BattleScene.cs` 的**方法尾聚合行**
//   `Debug.Log(P + $"=== 合计：{pass} 通过 / {fail} 失败 ===");` **之前**，同一物理行追加：
//     `B103W5_Assert.Run(); pass += B103W5_Assert.Pass; fail += B103W5_Assert.Fail;`
//   （同一物理行追加 ⇒ 行数零变化；⚠️ 追加前先看该行第一个 `//`，文本必须落在它**之前**。）
//   本文件**只用引擎侧**（`RuleCore.NewBattle` 自建一局），不需要 `BattleDriver` / 不建 GameObject。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>账 `战斗逻辑驱动:1282`/`:1380` —— `chosenAbility 220 / 290 / 280 / 375 / 330` 五支执行器。</summary>
public static class B103W5_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B103W5] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败的条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 夹具

    static CardDef Hero(string id) =>
        new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0, null, subtype: "Warlord");

    static CardDef PlainUnit(string id) =>
        new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 1, 0, (string[])null);

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B103W5_W0") };
        for (int i = 0; i < 20; i++) d0.Add(PlainUnit("B103W5_f" + i));
        var d1 = new List<CardDef> { Hero("B103W5_W1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
    }

    /// <summary>夹具：带一张**可被 `deploy` 具名造出来**的卡池（`CreatePool.Resolve` 的 ③ 具名查那一路）。</summary>
    static BattleContext NewCtxWithPool(CardDef pooled)
    {
        var d0 = new List<CardDef> { Hero("B103W5_W0") };
        for (int i = 0; i < 20; i++) d0.Add(PlainUnit("B103W5_f" + i));
        var d1 = new List<CardDef> { Hero("B103W5_W1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef> { pooled });
    }

    static UnitState Put(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    // ============================================================ 入口

    public static void Run()
    {
        MenuCheck.Section(_sink, "B103W5：账 `战斗逻辑驱动:1282`/`:1380` —— `chosenAbility 220 / 290 / 280 / 375 / 330` 执行器");
        CaseGate();
        Case220();
        Case290();
        Case280();
        Case375();
        Case330();
        Case135();
        Case340();
        Case71();
        Case200();
        Case345();
        Case80();
        Case162();
        Case310();
        Debug.Log(_sink.P + $"B103W5 合计：{_sink.Pass} 通过 / {_sink.Fail} 失败");
    }

    // ============================================================ A-0 · 计划表闸 + ca 分派（接线已落）
    //
    //  这两个接线点原本不在本代理白名单；**调度台 2026-10-30 把白名单扩到那两行之后由本代理自己打上**：
    //    · `RuleCore.cs:17364` `ActiveRowEntersPlan` 的 `return` 里加了
    //      `|| ca == 220 || ca == 280 || ca == 290 || ca == 330 || ca == 375 || ca == 160 || ca == 165`
    //      （前五 = 本代理这五支；`160`/`165` = 分派早就在、闸里没有 ⇒ 执行器是死代码）；
    //    · `EffectResolver.cs:18533` `RunActiveRowOpsOrGrantedTrigger` 里加了 5 个 `if (ca == …) return Run…Row(...)`。
    //  两处都是**同一物理行追加 ⇒ 行数零变化**。

    static void CaseGate()
    {
        Debug.Log(_sink.P + "--- A-0 硬前提 + 计划表闸 / ca 分派（`RuleCore.ActiveRowEntersPlan` · `EffectResolver.RunActiveRowOpsOrGrantedTrigger`）---");

        var ctx = NewCtx();
        int me = ctx.Active;
        var u = Put(ctx, me, 3, PlainUnit("B103W5_gate"));      // ⛔ 别占 4 号格：那是督军位（`BoardSpec.WarlordSlot`）

        // 🔴 先钉前提（硬检查，⛔ 不是 `if (前提) Check(结论)` 那种静默跳过）
        Tr(u != null && u.Card != null, "（前提）A-0 夹具卡上得了棋盘");
        C(RuleCore.ActiveRowHasOpsForTest(u, 0), false,
          "★ A-0a 夹具这张卡的行 0 **没有配到正文 op**（`ActiveRowHasOpsForTest == false`）"
        + "｜🧨 若它变成 true，A-0b 的 true 就会被「正文 op」顶掉 ⇒ 这条测不到闸");

        C(RuleCore.ActiveRowEntersPlanForTest(u, 0, 220), true,
          "★★ A-0b `ActiveRowEntersPlan(…, 220) == true` —— `220 resetActions` 的行能进「这一次跑哪几行」的计划表"
        + "（判据 `AbilityLogic__PlayAbility.c:2623-2638`）"
        + "｜🧨 删掉 `RuleCore.cs:17364` 那格闸里的 `|| ca == 220` ⇒ 实得 False ⇒ 红"
        + "（那一行会被当 `chosenAbility=220` 剔进 `unimplemented` ⇒ plan 为空 ⇒ 执行器成死代码）");

        C(RuleCore.ActiveRowEntersPlanForTest(u, 0, 231), false,
          "★ A-0c **对照**：`ca = 231`（既不在闸里、也没有正文 op）⇒ false"
        + "｜🧨 把闸写成恒 true（或「`ca >= 220` 就放行」）⇒ 实得 True ⇒ 红（说明 A-0b 不是「随便哪个码都放行」）");

        C(RuleCore.RunActiveRowDispatchForTest(ctx, me, u, 0, 220, 30, u), 1,
          "★★ A-0d ca 分派真的走到 `RunResetActionsRow`（返回 1 = 真结算了一个目标）"
        + "｜🧨 删掉 `EffectResolver.cs:18533` 那行里的 `if (ca == 220)` ⇒ 落回 `RunActiveRowOps`"
        + "（夹具**没有正文 op** ⇒ 返回 0）⇒ 实得 0 ⇒ 红");
    }

    // ============================================================ A-220 · resetActions

    static void Case220()
    {
        Debug.Log(_sink.P + "--- A-220：`chosenAbility 220 resetActions` ---");
        var ctx = NewCtx();
        int me = ctx.Active, foe = 1 - me;
        var u = Put(ctx, me, 3, PlainUnit("B103W5_220act"));      // ⛔ 别占 4 号格：那是督军位
        var t = Put(ctx, foe, 3, PlainUnit("B103W5_220tgt"));

        // 🔴 先钉前提（硬检查，⛔ 不是 `if (前提) Check(结论)` 那种静默跳过）
        Tr(u != null && u.Card != null && t != null && t.Card != null, "（前提）A-220 两个夹具都上了棋盘");
        t.AttacksThisTurn = 2;
        C(t.AttacksThisTurn, 2, "（前提）A-220 目标本回合已行动 2 次");

        // 闸：`ta = 10` 且这一行没有正文 op ⇒ 解不出目标集 ⇒ 不猜
        C(RuleCore.RunResetActionsRowForTest(ctx, me, u, 0, 10, t), 0,
          "★ A-220a `ta=10` 且这一行没有正文 op ⇒ **解不出目标集 ⇒ 不猜**、返回 0"
        + "（判据 `AbilityLogic__GetTargets.c:192-207`：整张函数只有 `ta = 30/220` 读调用方给的那个目标）");
        C(t.AttacksThisTurn, 2,
          "★★ A-220a 那一次**一个字段都没动**（计数仍是 2）"
        + "｜🧨 退回「己方全体 / 对面全体」那种近似 ⇒ 计数被减 ⇒ 实得 1 ⇒ 红（会静默改错人的行动额度）");

        // 正路：`ta = 30` + 玩家点定的目标
        C(RuleCore.RunResetActionsRowForTest(ctx, me, u, 0, 30, t), 1,
          "★ A-220b `ta=30` + 点定目标 ⇒ 返回 1（真结算了 1 个目标）"
        + "（判据 `decomp_full/AbilityLogic__PlayAbility.c:2623-2638` 逐目标 `AddResetActions`）");
        C(t.AttacksThisTurn, 1,
          "★★ A-220b 目标计数 **2 → 1**（原版 `CardScript.ResetActions.c:29-31` 计数 **−1**）"
        + "｜🧨 写成「清零」⇒ 实得 0 ⇒ 红");
        C(u.AttacksThisTurn, 0,
          "★ A-220c **灭自证**：施放者自己的计数没被碰（0）"
        + "｜🧨 若把动作打在 `u` 自己身上（而不是目标）⇒ 施放者计数 −1、目标不动 ⇒ A-220b/A-220c 一起红");
    }

    // ============================================================ A-290 · missAction

    static void Case290()
    {
        Debug.Log(_sink.P + "--- A-290：`chosenAbility 290 missAction` ---");
        var ctx = NewCtx();
        int me = ctx.Active, foe = 1 - me;
        var u = Put(ctx, me, 3, PlainUnit("B103W5_290act"));
        var t = Put(ctx, foe, 5, PlainUnit("B103W5_290tgt"));

        Tr(u != null && u.Card != null && t != null && t.Card != null, "（前提）A-290 两个夹具都上了棋盘");
        C(t.AttacksThisTurn, 0, "（前提）A-290 目标计数从 0 起");

        C(RuleCore.RunMissActionRowForTest(ctx, me, u, 0, 10, t), 0,
          "★ A-290a `ta=10` 且没有正文 op ⇒ 不猜、返回 0");
        C(t.AttacksThisTurn, 0, "★★ A-290a 那一次没记账（计数仍是 0）");

        C(RuleCore.RunMissActionRowForTest(ctx, me, u, 0, 30, t), 1,
          "★ A-290b `ta=30` + 点定目标 ⇒ 返回 1"
        + "（判据 `decomp_full/AbilityLogic__PlayAbility.c:2514-2532` 逐目标 `AddMissAction`）");
        C(t.AttacksThisTurn, 1,
          "★★ A-290b 目标计数 **0 → 1**（原版 `CardScript.MissActions.c` 末句 `+0x48 += 1` **无条件**）"
        + "｜🧨 写成「只打日志不记账」⇒ 实得 0 ⇒ 红");
        C(u.AttacksThisTurn, 0, "★ A-290c **灭自证**：施放者自己的计数没被碰");
    }

    // ============================================================ A-280 · revealOrder

    static void Case280()
    {
        Debug.Log(_sink.P + "--- A-280：`chosenAbility 280 revealOrder` ---");
        var ctx = NewCtx();
        int me = ctx.Active, foe = 1 - me;
        var u = Put(ctx, me, 3, PlainUnit("B103W5_280act"));
        var t = Put(ctx, foe, 5, PlainUnit("B103W5_280tgt"));

        Tr(u != null && u.Card != null && t != null && t.Card != null, "（前提）A-280 两个夹具都上了棋盘");

        C(RuleCore.RunRevealOrderRowForTest(ctx, me, u, 0, 10, t), 0,
          "★ A-280a `ta=10` 且没有正文 op ⇒ 不猜、返回 0");

        // 目标身上没有密令（`HasSecretOrder` 假）⇒ 揭示 0 件；**关键是它没有把它当成「做过了」**
        C(RuleCore.RunRevealOrderRowForTest(ctx, me, u, 0, 30, t), 0,
          "★★ A-280b `ta=30` + 一个**没有密令**的目标 ⇒ 返回 0（揭示 0 件）"
        + "（原版 `_ResolveRevealOrders…MoveNext` 的 ① 闸 `HasSecretOrder`；我们的落点 = `RuleCore.RevealSecretOrders`）"
        + "｜🧨 把 280 写成「无条件 return 1」⇒ 实得 1 ⇒ 红（= 静默报「做过了」）");
        Tr(t.IsAlive,
          "★ A-280c **灭自证**：目标仍在场（没有密令可揭示 ⇒ 不摘附魔、不动单位）");
    }

    // ============================================================ A-375 · triggerSacrifice

    static void Case375()
    {
        Debug.Log(_sink.P + "--- A-375：`chosenAbility 375 triggerSacrifice` ---");
        var ctx = NewCtx();
        int me = ctx.Active, foe = 1 - me;
        var u = Put(ctx, me, 3, PlainUnit("B103W5_375act"));
        var t = Put(ctx, foe, 5, PlainUnit("B103W5_375tgt"));

        Tr(u != null && u.Card != null && t != null && t.Card != null, "（前提）A-375 两个夹具都上了棋盘");

        C(RuleCore.RunTriggerSacrificeRowForTest(ctx, me, u, 0, 10, t), 0,
          "★ A-375a `ta=10` 且没有正文 op ⇒ 不猜、返回 0");
        C(RuleCore.RunTriggerSacrificeRowForTest(ctx, me, u, 0, 30, null), 0,
          "★ A-375b `ta=30` 但**没给目标**（`chosen == null`）⇒ 解不出目标集 ⇒ 返回 0"
        + "｜🧨 把登记写成「无条件返回 1」⇒ 实得 1 ⇒ 红");

        C(RuleCore.RunTriggerSacrificeRowForTest(ctx, me, u, 0, 30, t), 1,
          "★★ A-375c `ta=30` + 在场目标 ⇒ 返回 1（真入队了一条 `sacrifice` 触发）"
        + "（判据 `decomp_full/AbilityLogic__PlayAbility.c:2111-2126` 逐目标 `AddTriggerSacrifice`；"
        + "入队复用 `RuleCore.EnqueueGrantedTrigger`）");
        C(u.AttacksThisTurn, 0, "★ A-375d **灭自证**：登记触发不改任何一方的行动计数（不是 `resetActions` 那种动作）");
    }

    // ============================================================ A-135 · stunMultiplier

    static void Case135()
    {
        Debug.Log(_sink.P + "--- A-135：`chosenAbility 135 stunMultiplier` ---");
        var ctx = NewCtx();
        int me = ctx.Active, foe = 1 - me;
        var u = Put(ctx, me, 3, PlainUnit("B103W5_135act"));
        var t = Put(ctx, foe, 3, PlainUnit("B103W5_135tgt"));

        Tr(u != null && u.Card != null && t != null && t.Card != null, "（前提）A-135 两个夹具都上了棋盘");
        Tr(!t.IsStunned, "（前提）A-135 目标一开始**没被眩晕**");
        C(RuleCore.B103W5RowSpecResolvedForTest(u, 0, 135), false,
          "（前提）这张夹具卡**没有 `activeAbilityLogic` 行** ⇒ 读不到这一行的 `furtherMultiplier`");

        C(RuleCore.RunStunMultiplierRowForTest(ctx, me, u, 0, 30, t), 0,
          "★★ A-135a 判不出 `furtherMultiplier`（= 原版 `local_178`）⇒ **一个都不晕**、返回 0"
        + "（判据 `AbilityLogic__PlayAbility.c:659-681`：`ShuffleCardList` 后取前 `local_178` 个；`:670` 的循环条件是 `0 < local_178`）"
        + "｜🧨 把「判不出」默认成 1 ⇒ 真的晕了 1 个、返回 1 ⇒ 红");
        Tr(!t.IsStunned,
          "★ A-135b **灭自证**：目标仍然**未被眩晕**（不是「返回 0 但其实晕了一下」）");
    }

    // ============================================================ A-340 · removeThisEnchantment

    static void Case340()
    {
        Debug.Log(_sink.P + "--- A-340：`chosenAbility 340 removeThisEnchantment` ---");
        var ctx = NewCtx();
        int me = ctx.Active;
        var u = Put(ctx, me, 3, PlainUnit("B103W5_340act"));

        Tr(u != null && u.Card != null && u.Instance != null, "（前提）A-340 夹具上了棋盘且有实例");
        C(RuleCore.B103W5EffectCountForTest(u), 0, "（前提）它身上**没有**附魔记录");
        C(RuleCore.RunRemoveThisEnchantmentRowForTest(ctx, me, u, 0, 30, null), 0,
          "★ A-340a 身上没有附魔 ⇒ 返回 0（原版 `AbilityLogic__PlayAbility.c:2250-2295` 遍历的是 `thisCard + 0x108`）");

        // 三个夹具附魔，**只有第三条该被摘**：
        //   ① buffType = 12（普通 enchantment，不在 {15,16}）·② 那张卡不带 trigger 400 ·③ 两条都满足
        var card400 = new CardDef("B103W5_400", "B103W5_400", "tactic", "", "common", "Test", 0, 0, 0, 0,
                                  (string[])null, abilityTriggers: new[] { 400 }, abilityTriggersVersioned: true);
        var cardNo = new CardDef("B103W5_no400", "B103W5_no400", "tactic", "", "common", "Test", 0, 0, 0, 0,
                                 (string[])null, abilityTriggers: new[] { 30 }, abilityTriggersVersioned: true);
        Tr(card400.AbilityTriggerPresence(400) == 1, "（前提）夹具卡 `B103W5_400` 真的带 trigger 400");
        Tr(cardNo.AbilityTriggerPresence(400) == 0, "（前提）夹具卡 `B103W5_no400` 真的**不带** trigger 400");

        u.Instance.ActiveEffects.Add(new ActiveCardEffect { EnchantmentCard = card400, BuffType = 12 });
        u.Instance.ActiveEffects.Add(new ActiveCardEffect { EnchantmentCard = cardNo, BuffType = 15 });
        u.Instance.ActiveEffects.Add(new ActiveCardEffect { EnchantmentCard = card400, BuffType = 15 });
        C(RuleCore.B103W5EffectCountForTest(u), 3, "（前提）身上已有 3 条附魔记录");

        C(RuleCore.RunRemoveThisEnchantmentRowForTest(ctx, me, u, 0, 30, null), 1,
          "★★ A-340b 三条里**只摘 1 条** —— 闸① `buffType ∈ {15 singleUse, 16 choice}` ∧ 闸② 那张附魔卡自带 trigger 400"
        + "（原版 `.c:2250-2295`；闸② = `AbilityLogic__MatchingTrigger(this, effect.enchantingCard+0x290, 400, 0)`，"
        + "`400` 来自 `PlayAbility` 的第 4 形参 `param_4`，主动能力那条路恒 400）"
        + "｜🧨 只判闸①（摘 2 条）⇒ 实得 2 ⇒ 红；只判闸②（也摘 2 条）⇒ 红；两条都不判（摘 3 条）⇒ 红");
        C(RuleCore.B103W5EffectCountForTest(u), 3,
          "★★ A-340c **灭自证**：记录数此刻仍是 **3** —— 摘除是**入队**（`Enchantments.EnqueueRemove`，"
        + "原版 `AddRemoveEnchantment` 也只排一条 `0x2c` 动作），不是当场删"
        + "｜🧨 改成直接 `RemoveAt` ⇒ 实得 2 ⇒ 红（那就绕开了动作队列 / 表现层）");
    }

    // ============================================================ A-330 · drawUntilEqual

    static void Case330()
    {
        Debug.Log(_sink.P + "--- A-330：`chosenAbility 330 drawUntilEqual` ---");
        var ctx = NewCtx();
        int me = ctx.Active, foe = 1 - me;
        var u = Put(ctx, me, 3, PlainUnit("B103W5_330act"));

        Tr(u != null && u.Card != null, "（前提）A-330 夹具上了棋盘");
        C(RuleCore.B103W5RowSpecResolvedForTest(u, 0, 330), false,
          "（前提）这张夹具卡**没有 `activeAbilityLogic` 行** ⇒ 取不到「这一行的规格」");

        int h0 = ctx.Players[me].Hand.Count, h1 = ctx.Players[foe].Hand.Count;
        C(RuleCore.RunDrawUntilEqualRowForTest(ctx, me, u, 0, 30, null), 0,
          "★★ A-330a 解不出这一行的规格（判不出 `playersAffected`）⇒ **不猜**、返回 0、一张都不抽"
        + "（原版 `AbilityLogic__PlayAbility.c:2403/2410` 两条抽牌闸都读 `playersAffected(+0x20)`）"
        + "｜🧨 缺省 `playersAffected = 0 (Both)` 硬抽 ⇒ 返回非 0 ⇒ 红");
        C(ctx.Players[me].Hand.Count, h0,
          "★ A-330b **灭自证**：我方手牌数一点没变（提前钉住两边的数，⛔ 不只看返回值）");
        C(ctx.Players[foe].Hand.Count, h1, "★ A-330b 对手手牌数也一点没变");
    }

    // ============================================================ A-71 · `summonUnitAndSetRef` 的闭环
    //
    //  原版 `71` 与 `70` 共用同一个 `case` 体（`AbilityLogic__PlayAbility.c:722-878`），唯一差别是
    //  `:868` 多写一句 `*(local_res8 + 0x280) = uVar12`（把**刚召唤出来的那张**记到 `AbilityLogic` 实例侧）。
    //  🔴 `+0x280` ≠ `+0x278`（`B103_W5c` §7·1 已就地订正）：`+0x278` 是 `CardScript.referencedCards`（列表）
    //  ⇒ 我们的 `UnitState.ReferencedCards`；`+0x280` 在本文件里对应的等价物 = **`DoDeploy` 末尾的 last-target 记账**
    //  （`ctx.LastTargets` = 刚部署的那批、`ctx.LastTarget = [0]`）—— 紧跟的下一行 `them/it` 读的就是它。
    //  ⇒ **零产品代码改动**，这里只钉「`DoDeploy` 之后 `ctx.LastTargets` 就是被召唤那张」。

    static void Case71()
    {
        Debug.Log(_sink.P + "--- A-71：`chosenAbility 71 summonUnitAndSetRef` 的闭环（`DoDeploy` 之后 last-target 记账）---");

        var pooled = PlainUnit("B103W5_summon");
        var ctx = NewCtxWithPool(pooled);
        int me = ctx.Active;
        var caster = Put(ctx, me, 3, PlainUnit("B103W5_71caster"));   // ⛔ 别占 4 号格：那是督军位
        ctx.ActingUnit = caster;

        // 🔴 先钉前提（硬检查，⛔ 不写 `if (前提) Check(结论)`）
        Tr(caster != null && caster.Card != null, "（前提）A-71 夹具施放者上了棋盘");
        Tr(ctx.CardPool != null && ctx.CardPool.Count == 1 && ctx.CardPool[0] == pooled,
           "（前提）这一局的卡池里只有那张待召唤的具名卡");
        C(RuleCore.B103W5LastTargetsCountForTest(ctx), 0, "（前提）动手前 last-target 槽是空的");

        var op = new EffectOp { Verb = "deploy", Payload = "B103W5_summon", Source = "A-71" };
        Tr(RuleCore.B103W5DeployForTest(ctx, me, "A-71", op),
           "★★ A-71a `deploy` 真的把那张具名卡召唤上场了（`DoDeploy` 返回真）"
         + "（判据 `AbilityLogic__PlayAbility.c:722-878`：`70/71` 同一个 `case` 体）");

        C(RuleCore.B103W5LastTargetsCountForTest(ctx), 1,
          "★★ A-71b 召唤之后 last-target 槽里**正好是那 1 张** —— 这就是原版 `:868` 的 `*(abilityLogic+0x280) = 刚召唤那张`"
        + "（`+0x280` 是 `AbilityLogic` 实例侧「本 ability 刚召唤的那张」，**不是** `CardScript.referencedCards(+0x278)`）"
        + "｜🧨 把 `DoDeploy` 末尾的 `ctx.LastTargets.Add(deployed)` 删掉 ⇒ 实得 0 ⇒ 红；"
        + "改成「记进 `ReferencedCards`」⇒ 实得 0 ⇒ 红（那是 `380` 那格，不是这一格）");

        var got = RuleCore.B103W5LastTargetFirstForTest(ctx);
        Tr(got != null && got.Card != null && ReferenceEquals(got.Card, pooled),
          "★★ A-71c **灭自证**：last-target 的**第 0 张确实是那张被召唤的卡**（不是同批里别的、也不是施放者）"
        + "｜🧨 把记账写成「记施放者自己」⇒ 红；记成空 ⇒ 红");

        // 负向对照：卡池里没有这张卡 ⇒ 一条都召唤不出来，且**槽里不会凭空多出东西**
        var ctx2 = NewCtxWithPool(PlainUnit("B103W5_other"));
        ctx2.ActingUnit = Put(ctx2, ctx2.Active, 3, PlainUnit("B103W5_71caster2"));
        C(RuleCore.B103W5LastTargetsCountForTest(ctx2), 0, "（前提）第二局动手前 last-target 槽是空的");
        Tr(!RuleCore.B103W5DeployForTest(ctx2, ctx2.Active, "A-71-neg",
                new EffectOp { Verb = "deploy", Payload = "B103W5_summon", Source = "A-71-neg" }),
           "★ A-71d **负向对照**：卡池里没有「B103W5_summon」⇒ 部署失败（返回假，如实出声）");
        C(RuleCore.B103W5LastTargetsCountForTest(ctx2), 0,
          "★ A-71e **灭自证**：失败那一路**没有**往 last-target 槽塞东西（空槽保持空）");
    }

    // ============================================================ A-200 · `exchangeRandomFriendlyMinion`
    //
    //  原版 `AbilityLogic__PlayAbility.c:462-536` 三段分支（临时 criteria 目标表 vs 本行目标表）。
    //  临时 criteria = `allFiltered + friendlyUnits + includeMinions + maxTargets 1` ⇒ **己方随机一个部队**。

    /// <summary>找某一方棋盘上第一个「普通单位」的格号（-1 = 没有）。</summary>
    static int FirstUnitSlot(BattleContext ctx, int p)
    {
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[p].Board[s];
            if (u != null && u.IsAlive && !u.IsWarlord) return s;
        }
        return -1;
    }

    static void Case200()
    {
        Debug.Log(_sink.P + "--- A-200：`chosenAbility 200 exchangeRandomFriendlyMinion` ---");

        var ctx = NewCtx();
        int me = ctx.Active, foe = 1 - me;
        var warlord = ctx.Players[me].Board[BoardSpec.WarlordSlot];
        Tr(warlord != null && warlord.IsWarlord, "（前提）A-200 我方督军在督军格上（当施放者用 ⇒ 不进「部队」池）");

        var cm = Put(ctx, me, 2, PlainUnit("B103W5_200f"));      // 己方部队（临时 criteria 的唯一候选）
        var rt = Put(ctx, foe, 2, PlainUnit("B103W5_200e"));     // 敌方部队（本行目标）
        Tr(cm != null && rt != null && cm.IsAlive && rt.IsAlive, "（前提）两个部队都在场");
        C(FirstUnitSlot(ctx, me), 2, "（前提）我方棋盘上唯一的部队在 2 号格");

        // 施放者 = 我方督军（`IsWarlord` ⇒ `AddSide(troopOnly)` 会跳过它 ⇒ 临时 criteria 只剩 cm 一个候选）
        ctx.ActingUnit = warlord;

        int n = RuleCore.RunExchangeRandomFriendlyMinionRowForTest(ctx, me, warlord, 0, 30, rt);
        C(n, 1, "★★ A-200a 两侧目标都非空 ⇒ 走**换位**那一段（原版 `.c:509-520` `AddExchangeMinions`），返回 1");

        // 换位后：cm 到敌方 2 号格、rt 到我方 2 号格（两个都被换走）
        Tr(ReferenceEquals(ctx.Players[foe].Board[2], cm), "★★ A-200b 己方那个部队被换到了**敌方** 2 号格");
        Tr(ReferenceEquals(ctx.Players[me].Board[2], rt), "★★ A-200c 敌方那个部队被换到了**我方** 2 号格");
        Tr(ReferenceEquals(ctx.Players[me].Board[BoardSpec.WarlordSlot], warlord),
          "★★ A-200d **灭自证**：施放者（督军）**没有**被卷进这次换位 —— 原版 `AddExchangeMinions` 的"
        + "第 1 形参只是 `actingCard`，被换的是后两个形参（`_ResolveExchangeMinion_d__552` 只读 `+0x38` 那个 2 元素列表）"
        + "｜🧨 去掉 `B103W5ExchangePair` 里那句 `ctx.ActingUnit = null` ⇒ `DoExchange` 走 rule ⑴"
        + "（施放者 + others[0]）⇒ 督军被换下督军格 ⇒ 红");
        Tr(ReferenceEquals(ctx.ActingUnit, warlord), "★ A-200e **灭自证**：`ctx.ActingUnit` 已原样还原（`try/finally`）");

        // 【分支 ①】己方**没有部队** ∧ 本行目标非空 ⇒ 抢本行目标（原版 `.c:485-495`）
        var ctx2 = NewCtx();
        int me2 = ctx2.Active, foe2 = 1 - me2;
        var rt2 = Put(ctx2, foe2, 2, PlainUnit("B103W5_200e2"));
        C(FirstUnitSlot(ctx2, me2), -1, "（前提）第二局我方棋盘上**一个部队都没有**（只剩督军）");
        Tr(rt2 != null && rt2.IsAlive, "（前提）第二局的敌方部队在场上");
        int n2 = RuleCore.RunExchangeRandomFriendlyMinionRowForTest(ctx2, me2, ctx2.Players[me2].Board[BoardSpec.WarlordSlot], 0, 30, rt2);
        C(n2, 1, "★★ A-200f 临时 criteria 为空 ⇒ 走**抢**那一段（原版 `.c:485-495` `AddStealMinion(本行目标)`），返回 1");
        bool onMine = false, stillFoe = false;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            if (ReferenceEquals(ctx2.Players[me2].Board[s], rt2)) onMine = true;
            if (ReferenceEquals(ctx2.Players[foe2].Board[s], rt2)) stillFoe = true;
        }
        Tr(onMine && !stillFoe,
          "★★ A-200g 那个敌方部队被抢到了**我方**（原版 `AddStealMinion`：`ChangeOwner` 落到对面；"
        + "落点 = `BoardSlots.NextFreeWithoutDisplacing`，不是原位）");

        // 【两侧都空】⇒ 什么都不做
        var ctx3 = NewCtx();
        int me3 = ctx3.Active;
        C(RuleCore.RunExchangeRandomFriendlyMinionRowForTest(ctx3, me3,
                ctx3.Players[me3].Board[BoardSpec.WarlordSlot], 0, 30, null), 0,
          "★ A-200h 两侧都空 ⇒ **这一支什么都不做**（原版 `.c:485/:497` 两条都不命中），返回 0");
    }

    // ============================================================ A-345 · `removeEnchantment`

    static void Case345()
    {
        Debug.Log(_sink.P + "--- A-345：`chosenAbility 345 removeEnchantment` ---");

        // ---- ① 判据缺：卡表里没有本行 `buffEffect.enchantment` ⇒ **不猜** ----
        var ctx = NewCtx();
        int me = ctx.Active;
        var u = Put(ctx, me, 3, PlainUnit("B103W5_345act"));
        var t = Put(ctx, me, 2, PlainUnit("B103W5_345tgt"));
        Tr(u != null && t != null && u.Card != null && t.Instance != null, "（前提）A-345 两个夹具都上了棋盘");
        C(RuleCore.RunRemoveEnchantmentRowForTest(ctx, me, u, 7, 30, t), 0,
          "★★ A-345a 判不出本行的 `buffEffect.enchantment`（卡表 `enchantment` 列里 `lg == 7` 的行 = 0 条）"
        + " ⇒ **不猜**、返回 0（原版 `.c:2320-2330`：`effect.enchantment(+0x88) == ability.buffEffect(+0x50).enchantment`）"
        + "｜🧨 把「判不出」默认成「摘目标身上全部附魔」⇒ 返回非 0 ⇒ 红");

        // ---- ② 正路：注册一条数据行（`lg == 0` 指向模板 `B103W5_enA`），目标身上有两条附魔、只该摘 1 条 ----
        var oldProvider = Enchantments.GrantedEnchantmentsProvider;
        try
        {
            Enchantments.GrantedEnchantmentsProvider = id =>
                id == "B103W5_345pat"
                    ? new List<Enchantments.GrantedEnchantmentRow>
                      {
                          new Enchantments.GrantedEnchantmentRow
                          {
                              SourceCardId = "B103W5_345pat", TemplateCardId = "B103W5_enA",
                              BuffType = 12, Trigger = 0, TargetsAffected = 30, AbilityIndex = 0, LogicIndex = 0,
                          },
                      }
                    : null;

            var ctx2 = NewCtx();
            int me2 = ctx2.Active;
            var u2 = Put(ctx2, me2, 3, PlainUnit("B103W5_345pat"));
            var t2 = Put(ctx2, me2, 2, PlainUnit("B103W5_345tgt2"));
            C(RuleCore.B103W5EffectCountForTest(t2), 0, "（前提）目标身上一开始没有附魔记录");

            var enA = new CardDef("B103W5_enA", "B103W5_enA", "enchantment", "", "", "", 0, 0, 0, 0, (string[])null);
            var enB = new CardDef("B103W5_enB", "B103W5_enB", "enchantment", "", "", "", 0, 0, 0, 0, (string[])null);
            t2.Instance.ActiveEffects.Add(new ActiveCardEffect { EnchantmentCard = enB, BuffType = 12 });
            t2.Instance.ActiveEffects.Add(new ActiveCardEffect { EnchantmentCard = enA, BuffType = 15 });
            C(RuleCore.B103W5EffectCountForTest(t2), 2, "（前提）目标身上已有 2 条附魔记录（一条 enB、一条 enA）");

            C(RuleCore.RunRemoveEnchantmentRowForTest(ctx2, me2, u2, 0, 30, t2), 1,
              "★★ A-345b 只摘**与本行模板同源（enA）**的那 1 条 —— 345 **不看** `buffType`（enA 那条是 15）"
            + "｜🧨 把「判据」写成「摘目标身上全部附魔」⇒ 实得 2 ⇒ 红；写成「只摘 buffType ∈ {15,16}」⇒ enB(12) 被漏 ⇒ 仍得 1（这条抓不住，故另有 A-345c）");
            C(RuleCore.B103W5EffectCountForTest(t2), 2,
              "★★ A-345c **灭自证**：记录数此刻仍是 **2** —— 摘除是**入队**（`Enchantments.EnqueueRemove`），不是当场删"
            + "｜🧨 改成直接 `RemoveAt` ⇒ 实得 1 ⇒ 红");

            // 反向：目标身上只有 enB ⇒ 一条都不摘
            var ctx3 = NewCtx();
            int me3 = ctx3.Active;
            var u3 = Put(ctx3, me3, 3, PlainUnit("B103W5_345pat"));
            var t3 = Put(ctx3, me3, 2, PlainUnit("B103W5_345tgt3"));
            t3.Instance.ActiveEffects.Add(new ActiveCardEffect { EnchantmentCard = enB, BuffType = 15 });
            C(RuleCore.RunRemoveEnchantmentRowForTest(ctx3, me3, u3, 0, 30, t3), 0,
              "★ A-345d **反向对照**：目标身上只有**别的模板**（enB）⇒ 一条都不摘、返回 0");
        }
        finally
        {
            Enchantments.GrantedEnchantmentsProvider = oldProvider;
        }
    }

    // ============================================================ A-80 · `transformSelf`

    static void Case80()
    {
        Debug.Log(_sink.P + "--- A-80：`chosenAbility 80 transformSelf` ---");
        var ctx = NewCtx();
        int me = ctx.Active;
        var u = Put(ctx, me, 3, PlainUnit("B103W5_80act"));
        Tr(u != null && u.Card != null, "（前提）A-80 夹具上了棋盘");
        var before = ctx.Players[me].Board[3];

        C(RuleCore.RunTransformSelfRowForTest(ctx, me, u, 0, 30, null), 0,
          "★★ A-80a 判不出 `keepEffects` / 「变成哪张卡」（`abilitySummon` 列不含 `activeAbilityLogic` —— 数据缺口）"
        + " ⇒ **不猜**、返回 0（判据 `PlayAbility.c:884-904` 的 `GetCardToSummon` + `summonCriteria.keepEffects(+0x2a)`）"
        + "｜🧨 把「判不出」默认成 `keepEffects=false` + 随便挑一张 ⇒ 返回 1 ⇒ 红");
        Tr(ReferenceEquals(ctx.Players[me].Board[3], before),
          "★★ A-80b **灭自证**：3 号格还是原来那个单位（没有变身、也没有被清掉）");

        // 不在场 ⇒ 不结算
        var ghost = new UnitState(ctx.NewInstance(PlainUnit("B103W5_80ghost")), false);
        C(RuleCore.RunTransformSelfRowForTest(ctx, me, ghost, 0, 30, null), 0,
          "★ A-80c 施放者不在场上 ⇒ 返回 0（原版 targetCard = `abilityData.actingCard(+0x18)`，取不到就空转）");
    }

    // ============================================================ 夹具：带一行 `activeAbilityLogic` 的施放者
    //
    //  🔴 **为什么要走合成 JSON**：`playersAffected` 那一列在 `activeAbilityLogic` 里，手写 `CardDef`
    //     拿不到它（与 `B97W5_Assert.B97W5E_DiscardFromHandRow` 同一条理由、同一个写法）。
    //     这一行的 `ca` = 被测的 `chosenAbility`、`pa = 10 (Self)` ⇒ 「动自己这一侧的手牌」。
    static CardDef CaRowCaster(string id, int ca)
    {
        string json =
            "{\"version\":40,\"count\":1,\"cards\":[{\"id\":\"" + id + "\",\"name\":\"" + id + "\","
          + "\"type\":\"unit\",\"desc\":\"Deal 1 damage\",\"rarity\":\"Common\",\"faction\":\"Ultramarines\","
          + "\"cost\":1,\"attack\":1,\"health\":1,\"ranged\":0,\"subtype\":\"Infantry\","
          + "\"keywords\":[],\"cardTraits\":[],\"hasActiveAbility\":1,"
          + "\"activeAbilityLogic\":[{\"ab\":-1,\"gate\":1,\"trigger\":400,\"needsLoading\":0,"
          + "\"ta\":[30],\"fm\":[0],\"ca\":[" + ca + "],\"pa\":[10]}]}]}";
        var l = CardDatabase.Parse(json);
        return (l != null && l.Count == 1) ? l[0] : null;
    }

    // ============================================================ A-162 · disappearFromHand
    //
    //  判据（第一权威，逐句现读 `d:/2/tools/decomp_full/`）：
    //   · 生产端 `AbilityLogic__PlayAbility.c:605-622`（`case 0xa2 = 162`）：逐目标
    //     `BattleManager__AddDisappearFromHand(bm, 目标, ability.priority(+0x44), abilityData(+0x48), 0)`；
    //     动作号 = `BattleManager__AddDisappearFromHand.c:23` 的 `0x38 (56)`；
    //   · 结算体 `BattleManager._ResolveDisappearFromHand_d__545__MoveNext.c`：
    //     `:36-37 IsInHand` 闸 → `:132 RemoveCostEffects` → `:134 DestroyCard`
    //     → `CardScript__GoToCemetery.c:14-30`（**ephemeral ⇒ 只 SetActive(false) = 移出游戏**；
    //        否则 ⇒ `AddToCemetery` = **进弃牌堆**）→ `:153 RemoveCardFromHand`。
    //  🔴 去向**两档都要钉**（只钉一档 = 把「消失」与「弃掉」混成一条）。

    static void Case162()
    {
        Debug.Log(_sink.P + "--- A-162：`chosenAbility 162 disappearFromHand` ---");

        // ---- ⓪ 接线：计划表闸 + ca 分派 + 整条链（入队 → 出队 → 牌真的走了） ----
        var caster = CaRowCaster("B103W5c_162c", 162);
        Tr(caster != null,
           "（前提）A-162 夹具卡解析出 1 张（带 `activeAbilityLogic[ca=162, pa=10]`）");
        if (caster == null) return;
        var ctx0 = NewCtx();
        int me0 = ctx0.Active;
        var u0 = Put(ctx0, me0, 3, caster);
        Tr(u0 != null && u0.Card != null, "（前提）A-162 施放者上了棋盘");
        C(RuleCore.ActiveRowHasOpsForTest(u0, 0), false,
          "（前提）这一行**没有正文 op**（`ca = 162` 就是全部语义 ⇒ 只有放它进闸才走得到执行器）");

        C(RuleCore.ActiveRowEntersPlanForTest(u0, 0, 162), true,
          "★★ A-162-0a `ActiveRowEntersPlan(…, 162) == true` —— 这一行进得了「这次跑哪几行」的计划表"
        + "｜🧨 删掉 `RuleCore.cs:17364` 那格闸里的 `|| ca == 162` ⇒ 实得 False ⇒ 红（执行器成死代码）");
        C(RuleCore.ActiveRowEntersPlanForTest(u0, 0, 163), false,
          "★ A-162-0b **对照**：`ca = 163`（既不在闸里、也没有正文 op）⇒ false"
        + "｜🧨 把闸写成恒 true ⇒ 实得 True ⇒ 红");

        ctx0.Players[me0].Hand.Clear();                        // 只留一张 ⇒ 挑牌是确定的
        var inst0 = ctx0.NewInstance(PlainUnit("B103W5c_162h"));
        ctx0.Players[me0].Hand.Add(inst0);
        C(RuleCore.RunActiveRowDispatchForTest(ctx0, me0, u0, 0, 162, 30, null), 1,
          "★★ A-162-0c ca 分派真的走到 `RunDisappearFromHandRow`（返回 1 = 真入队了一条）"
        + "｜🧨 删掉 `EffectResolver.cs:18533` 的 `if (ca == 162)` ⇒ 落回 `RunActiveRowOps`（没有正文 op）⇒ 0 ⇒ 红");
        RuleCore.PumpActionQueues(ctx0);
        C(ctx0.Players[me0].Hand.Count, 0,
          "★★ A-162-0d **整条链通了**：入队 → 出队 → 那张牌真的离开了手牌（剩余 0 张）"
        + "｜🧨 把 `EnqueueDisappearFromHand` 的 `body` 写成 `null`（只占位不结算）⇒ 实得 1 ⇒ 红");

        // ---- ① 非 ephemeral ⇒ **进弃牌堆** ----
        var ctx1 = NewCtx();
        int me1 = ctx1.Active;
        ctx1.Players[me1].Hand.Clear();
        var victim = ctx1.NewInstance(PlainUnit("B103W5c_162plain"));
        ctx1.Players[me1].Hand.Add(victim);
        int disc1 = ctx1.Players[me1].Discard.Count, rem1 = ctx1.Removed.Count;
        C(ctx1.Players[me1].Hand.Count, 1, "（前提）A-162 手牌里只有那一张");
        C(RuleCore.ResolveDisappearFromHandForTest(ctx1, me1, victim), 1,
          "★ A-162a 出队处理口：非 ephemeral ⇒ 返回 1（真送走了）");
        C(ctx1.Players[me1].Hand.Count, 0, "★ A-162a 手牌少 1 张");
        C(ctx1.Players[me1].Discard.Count, disc1 + 1,
          "★★ A-162b 去向 = **弃牌堆**（原版 `CardScript__GoToCemetery.c:22-27`：没有 trait 5 ⇒ `AddToCemetery`）"
        + "｜🧨 一律当「移出游戏」⇒ 实得 " + disc1 + " ⇒ 红");
        C(ctx1.Removed.Count, rem1,
          "★★ A-162c **灭自证**：`ctx.Removed`（移出游戏）**没有**多一条 —— 非 ephemeral 不进那个区域");
        Tr(ctx1.Players[me1].Discard.Count > 0
           && ReferenceEquals(ctx1.Players[me1].Discard[ctx1.Players[me1].Discard.Count - 1], victim),
           "★ A-162d 弃牌堆里是**那一份实例**（按实例去，⛔ 不是按卡名）");

        // ---- ② ephemeral ⇒ **移出游戏**（跳过 AddToCemetery） ----
        var ctx2 = NewCtx();
        int me2 = ctx2.Active;
        var eph = ctx2.NewInstance(PlainUnit("B103W5c_162eph"));
        eph.EphemeralMarked = true;                            // `IsEphemeral` 认 `Has("ephemeral") || EphemeralMarked`
        ctx2.Players[me2].Hand.Clear();
        ctx2.Players[me2].Hand.Add(eph);
        int disc2 = ctx2.Players[me2].Discard.Count, rem2 = ctx2.Removed.Count;
        Tr(ctx2.IsEphemeral(eph), "（前提）这份实例被判为 ephemeral");
        C(RuleCore.ResolveDisappearFromHandForTest(ctx2, me2, eph), 1,
          "★ A-162e ephemeral ⇒ 返回 1");
        C(ctx2.Removed.Count, rem2 + 1,
          "★★ A-162f ephemeral ⇒ 记进**移出游戏**（原版 `GoToCemetery.c:14-21`：有 trait 5 ⇒ 只 `SetActive(false)`、"
        + "**跳过 `AddToCemetery`**）｜🧨 两档都走 `AddToCemetery` ⇒ 实得 " + rem2 + " ⇒ 红");
        C(ctx2.Players[me2].Discard.Count, disc2,
          "★★ A-162g **灭自证**：弃牌堆**一张都没多**（ephemeral 不进弃牌堆）");
        C(ctx2.Players[me2].Hand.Count, 0, "★ A-162g 手牌被拿走（两条路都该离开手牌）");

        // ---- ③ 不在手牌里 ⇒ 原版失败支：一格都不动 ----
        var ctx3 = NewCtx();
        int me3 = ctx3.Active;
        var stranger = ctx3.NewInstance(PlainUnit("B103W5c_162ghost"));
        int h3 = ctx3.Players[me3].Hand.Count, d3 = ctx3.Players[me3].Discard.Count, r3 = ctx3.Removed.Count;
        C(RuleCore.ResolveDisappearFromHandForTest(ctx3, me3, stranger), 0,
          "★★ A-162h 那张牌**不在手牌里** ⇒ 返回 0（原版 `:36-90` 的失败支：LogWarning + FinishResolvingAction）"
        + "｜🧨 去掉 `ps.Hand.Contains` 那道闸 ⇒ 返回 1 ⇒ 红");
        C(ctx3.Players[me3].Hand.Count, h3, "★ A-162h 手牌数一点没变");
        C(ctx3.Players[me3].Discard.Count, d3, "★ A-162h 弃牌堆没变");
        C(ctx3.Removed.Count, r3, "★ A-162h 「移出游戏」没变");

        // ---- ④ `playersAffected` 读不到 ⇒ **不猜**（⛔ 不许「随便挑一张动掉」）----
        var ctx4 = NewCtx();
        int me4 = ctx4.Active;
        var plain = Put(ctx4, me4, 3, PlainUnit("B103W5c_162norows"));   // 没有 `activeAbilityLogic`
        ctx4.Players[me4].Hand.Clear();
        ctx4.Players[me4].Hand.Add(ctx4.NewInstance(PlainUnit("B103W5c_162h2")));
        int h4 = ctx4.Players[me4].Hand.Count;
        C(RuleCore.RunDisappearFromHandRowForTest(ctx4, me4, plain, 0, 30, null), 0,
          "★★ A-162i `playersAffected` 读不到（这张卡没有 `activeAbilityLogic` 行）⇒ **不猜**、返回 0"
        + "（原版读 `AbilityLogic.playersAffected`；`0 Both` 原版两侧各来一遍，我们这条只动一侧 ⇒ 同样不猜）"
        + "｜🧨 缺省成「随便挑一张动掉」⇒ 返回 1 ⇒ 红");
        C(ctx4.Players[me4].Hand.Count, h4,
          "★★ A-162j **灭自证**：手牌一张都没动（" + h4 + " 张还是 " + h4 + " 张）");
    }

    // ============================================================ A-310 · displayFromHand
    //
    //  判据（第一权威，逐句现读 `d:/2/tools/decomp_full/`）：
    //   · 生产端 `AbilityLogic__PlayAbility.c:2227-2242`（`case 0x136 = 310`）：逐目标
    //     `BattleManager__AddDisplayFromHand(bm, 目标, priority, blockPlayerActions, 0)`；
    //     动作号 = `BattleManager__AddDisplayFromHand.c:23` 的 `0x39 (57)`；
    //   · 结算体 `BattleManager._ResolveDisplayFromHand_d__548__MoveNext.c`：
    //     `:45-101 IsInHand` 闸 → `:191 CemeteryManager.AddDisplayedCardActionToCemetery`（墓地动作日志里
    //     追加一条 `displayedCard(30)`）→ `:193/:195` 中央亮牌 → `:234/:236` 翻回 → `:227` 手牌重排
    //     → `:228` FinishResolvingAction。
    //  🔴 **牌自始至终留在手牌里** ⇒ 这一轮钉的就是「不动牌面」（⛔ 别把它做成消失/弃掉）。

    static void Case310()
    {
        Debug.Log(_sink.P + "--- A-310：`chosenAbility 310 displayFromHand` ---");

        // ---- ⓪ 接线：计划表闸 + ca 分派 ----
        var caster = CaRowCaster("B103W5c_310c", 310);
        Tr(caster != null,
           "（前提）A-310 夹具卡解析出 1 张（带 `activeAbilityLogic[ca=310, pa=10]`）");
        if (caster == null) return;
        var ctx0 = NewCtx();
        int me0 = ctx0.Active;
        var u0 = Put(ctx0, me0, 3, caster);
        Tr(u0 != null && u0.Card != null, "（前提）A-310 施放者上了棋盘");
        C(RuleCore.ActiveRowHasOpsForTest(u0, 0), false,
          "（前提）这一行**没有正文 op**（`ca = 310` 就是全部语义）");

        C(RuleCore.ActiveRowEntersPlanForTest(u0, 0, 310), true,
          "★★ A-310-0a `ActiveRowEntersPlan(…, 310) == true`"
        + "｜🧨 删掉 `RuleCore.cs:17364` 闸里的 `|| ca == 310` ⇒ 实得 False ⇒ 红（执行器成死代码）");
        C(RuleCore.ActiveRowEntersPlanForTest(u0, 0, 311), false,
          "★ A-310-0b **对照**：`ca = 311` ⇒ false（挡「把闸写成恒 true」）");

        ctx0.Players[me0].Hand.Clear();
        var inst0 = ctx0.NewInstance(PlainUnit("B103W5c_310h"));
        ctx0.Players[me0].Hand.Add(inst0);
        C(RuleCore.RunActiveRowDispatchForTest(ctx0, me0, u0, 0, 310, 30, null), 1,
          "★★ A-310-0c ca 分派真的走到 `RunDisplayFromHandRow`（返回 1 = 真入队了一条）"
        + "｜🧨 删掉 `EffectResolver.cs:18533` 的 `if (ca == 310)` ⇒ 落回 `RunActiveRowOps`（没有正文 op）⇒ 0 ⇒ 红");
        RuleCore.PumpActionQueues(ctx0);
        C(ctx0.Players[me0].Hand.Count, 1,
          "★★ A-310-0d **整条链通了且牌没动**：入队 → 出队之后那张牌**还在手牌里**（1 张）"
        + "｜🧨 把出队处理口接成 `disappearFromHand` 那一支 ⇒ 实得 0 ⇒ 红");
        Tr(ctx0.Events.Exists(s => s != null && s.Contains("displayFromHand(57)")),
           "★★ A-310-0e 出队那一条**真的跑了**（`ctx.Events` 里搜得到 `displayFromHand(57)` 的日志）"
         + "｜🧨 把 `EnqueueDisplayFromHand` 的 `body` 写成 `null` ⇒ 这句日志不出现 ⇒ 红");

        // ---- ① 直接点测出队处理口：牌面一个字节都不变 ----
        var ctx1 = NewCtx();
        int me1 = ctx1.Active;
        ctx1.Players[me1].Hand.Clear();
        var shown = ctx1.NewInstance(PlainUnit("B103W5c_310plain"));
        ctx1.Players[me1].Hand.Add(shown);
        int disc1 = ctx1.Players[me1].Discard.Count, rem1 = ctx1.Removed.Count;
        int ev1 = ctx1.Events.Count;
        C(RuleCore.ResolveDisplayFromHandForTest(ctx1, me1, shown), 1,
          "★ A-310a 出队处理口：在手牌里 ⇒ 返回 1（真的展示了）");
        C(ctx1.Players[me1].Hand.Count, 1,
          "★★ A-310b **灭自证**：手牌**还是 1 张** —— 原版这一支从头到尾不摘牌"
        + "（`_ResolveDisplayFromHand_d__548` 只 `SetCardState` + `DisplayEnemyCardInCenter` + `HandCardReleased`）");
        Tr(ctx1.Players[me1].Hand.Contains(shown), "★ A-310b 手牌里那一份**就是同一份实例**");
        C(ctx1.Players[me1].Discard.Count, disc1,
          "★★ A-310c **灭自证**：弃牌堆**没动**（显示 ≠ 弃置）｜🧨 误接成 `disappearFromHand` ⇒ 实得 " + (disc1 + 1) + " ⇒ 红");
        C(ctx1.Removed.Count, rem1, "★★ A-310c **灭自证**：「移出游戏」没动（显示 ≠ 消失）");
        Tr(ctx1.Events.Count > ev1,
           "★ A-310d 这一跳**出声了**（`ctx.Events` 多了至少一条 —— 原版这里也有一条 `CustomDebug.Log`；"
         + "⚠️ 墓地动作日志那条 `displayedCard(30)` 我们只落进 `ctx.Log`，见 `ResolveDisplayFromHand` 的 doc）");

        // ---- ② 不在手牌里 ⇒ 原版失败支：一格都不动 ----
        var ctx2 = NewCtx();
        int me2 = ctx2.Active;
        var stranger = ctx2.NewInstance(PlainUnit("B103W5c_310ghost"));
        int h2 = ctx2.Players[me2].Hand.Count;
        C(RuleCore.ResolveDisplayFromHandForTest(ctx2, me2, stranger), 0,
          "★★ A-310e 那张牌**不在手牌里** ⇒ 返回 0（原版 `:45-101` 的失败支：LogWarning + FinishResolvingAction）"
        + "｜🧨 去掉 `ps.Hand.Contains` 那道闸 ⇒ 返回 1 ⇒ 红");
        C(ctx2.Players[me2].Hand.Count, h2, "★ A-310e 手牌数一点没变");

        // ---- ③ `playersAffected` 读不到 ⇒ **不猜** ----
        var ctx3 = NewCtx();
        int me3 = ctx3.Active;
        var plain = Put(ctx3, me3, 3, PlainUnit("B103W5c_310norows"));   // 没有 `activeAbilityLogic`
        ctx3.Players[me3].Hand.Clear();
        ctx3.Players[me3].Hand.Add(ctx3.NewInstance(PlainUnit("B103W5c_310h2")));
        int h3 = ctx3.Players[me3].Hand.Count;
        C(RuleCore.RunDisplayFromHandRowForTest(ctx3, me3, plain, 0, 30, null), 0,
          "★★ A-310f `playersAffected` 读不到 ⇒ **不猜**、返回 0"
        + "｜🧨 缺省成「随便挑一张展示」⇒ 返回 1 ⇒ 红");
        C(ctx3.Players[me3].Hand.Count, h3, "★★ A-310g **灭自证**：手牌一张都没动");
    }
}
