// B97W35_Assert.cs — 第九十七会话 · 波 4-B · 执行代理 `B97_W35`
//
// ============================ 它盯的是什么 ============================
// 账 `战斗逻辑驱动:1278`（`RawCardScript.OnTriggerEffectInCard`）的**残余②**：
//   `isEnchantment = 1` 时那一趟**做两件事** ——
//     ① 跑 `abilityCard`（形参 `this`）自己按该 trigger 的能力（改前就有 = `ResolveOps`）；
//     ② **跑 `thisCard`（= 调用 `TriggerOtherCardAbility` 的那个单位）身上
//        `activeEnchantments` 里「未 spent」的附魔卡的同一 trigger**（改前**没有**）。
//   判据（🥇 `d:/2/tools/decomp_full/RawCardScript__TriggerAbilities.c:50-190`，逐句）：
//     · `:50` `param_4 + 0x110`（`thisCard.activeEnchantments`）非空 ∧ `param_11 dontResolveEnchantments == 0` ⇒ 进附魔趟
//     · `:59-65` 按**列表序**逐条取 · `:66` `IsSpentEnchantment(param_4, 附魔卡)` 为真 ⇒ **跳过**
//     · `:108-109` `CardAbility__CanTriggerAbility(ability, trigger, mgr, param_4, param_5, param_6, …)`
//     · `:115` `ReachedQuestCounter(param_4, 附魔卡, param_4)` · `:184-186` `TriggerAbility(…, thisCard=param_4, …)`
//   `param_4` 的来源：`CardScript__TriggerOtherCardAbility.c:9-10`
//     → `OnTriggerEffectInCard(param_2, 0, mgr, param_1 /*this 的调用者*/, param_1, 0, 0)`
//   ⇒ 本仓对应物 = `RunAbilityOfAnotherCard(..., abilityOwnerUnit: 调用者)`，⛔ 不是 `abilityCard`。
//   实现在 `EffectResolver.RunActiveEnchantmentTriggerOf`（类尾）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的 `RuleEngineTest.Run()` 里加**一个块**
// （位置照 `B97_W30` 的先例：紧接 `{ CheckB97W30Extrinsic(); }` 那一行之后）：
//       // 🆕 2026-10-29（调度台代接线）：B97_W35 —— 账 1278 ②（跑载体身上未 spent 的附魔）。
//       { CheckB97W35EnchantmentHop(); }
//   🔴 用「块 + 不带 Test/Report 前缀的方法名」而不是 `Step(...)`（理由同 `B97W30_Assert.cs` 的文件头）。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · A2/A3：把 `RunActiveEnchantmentTriggerOf` 改成恒 `false`（= 回到改前）⇒ A2/A3 红。
//   · B3 ★★★（**本批的本体**）：把 `RunAbilityOfAnotherCard` 自己那趟的
//       `return __ownRan || RunActiveEnchantmentTriggerOf(...)`
//     改回 `return __ownRan;`（= 改前「有正文就直接返回」）⇒ **B3 红**、B4 也红。
//   · C2：把 `if (Enchantments.AllSpent(carrier.Instance, e.Card)) continue;` 删掉 ⇒ C2/C3 红
//     （已花掉的附魔又被派发一次）。
//   · D1/D2/D3：把 `abilityOwnerUnit` 换成「全棋盘扫附魔」或「abilityCard 自己的实例」⇒ D1/D2 红；
//     去掉 `carrier == null ⇒ false` 那道闸 ⇒ D3 红。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀，见文件头）。</summary>
    public static void CheckB97W35EnchantmentHop()
    {
        Section("🆕 B97_W35：账 1278 ② —— `OnTriggerEffectInCard(isEnchantment=1)` 的第二件事（跑载体身上未 spent 的附魔）");
        B97W35HopWithoutOwn();
        B97W35HopAfterOwn();
        B97W35SpentGate();
        B97W35NegativeControls();
    }

    // ==================================================================
    //  夹具
    // ==================================================================
    const string W35EnchName = "B97W35Ench";
    const string W35HopLog = "身上的附魔「" + W35EnchName + "」";

    /// <summary>附魔卡：一条 `Rally:` 正文（`Rally` = 原版 `ThisCardPlayed(0)` 的卡面写法）。</summary>
    static CardDef W35Enchantment() { return Unit(W35EnchName, 0, 0, 1, "Rally: Deal 1 damage to an enemy"); }

    /// <summary>被跑的那张卡：**没有** Rally 正文（用来把「自己那趟」关掉，只留附魔那趟）。</summary>
    static CardDef W35PlainCard() { return Unit("B97W35Plain", 0, 0, 1); }

    /// <summary>被跑的那张卡：**有** Rally 正文（用来验「自己那趟跑完，附魔那趟也要跑」）。</summary>
    static CardDef W35OwnRally() { return Unit("B97W35Owner", 0, 0, 1, "Rally: Deal 1 damage to an enemy"); }

    /// <summary>同一入口的唯一调用形状（`abilityOwnerUnit` = 原版 `TriggerOtherCardAbility` 的调用者）。</summary>
    static bool W35Call(BattleContext ctx, CardDef abilityCard, UnitState carrier)
    {
        return RuleCore.RunAbilityOfAnotherCard(ctx, abilityCard, KeywordTable.Rally, 0,
                                                abilityOwnerUnit: carrier, actingCard: carrier, targetCard: null);
    }

    /// <summary>一套「载体在场」的干净对局；返回载体（`Instance` 一定有）。</summary>
    static UnitState W35Setup(string tag, out BattleContext ctx)
    {
        var carrierCard = Unit("B97W35Carrier" + tag, 1, 3, 5);
        ctx = Battle(new[] { carrierCard }, new[] { Unit("B97W35Foe" + tag, 1, 1, 5) });
        return Place(ctx, 0, 1, carrierCard);
    }

    // ==================================================================
    //  A · 自己那趟空（没有 Rally 正文）⇒ 附魔那趟**照跑**
    //  判据 = `TriggerAbilities.c:42-48`（自己那趟跳过）→ `:50-190`（附魔趟仍在同一函数里）
    // ==================================================================
    static void B97W35HopWithoutOwn()
    {
        BattleContext ctx;
        var u = W35Setup("A", out ctx);
        CheckTrue(u != null && u.Instance != null, "前提：夹具把载体放上了棋盘（不静默跳过）");
        if (u == null || u.Instance == null) return;
        var eff = Enchantments.Attach(ctx, u.Instance, W35Enchantment(), Enchantments.BuffTypeEnchantment);
        CheckTrue(eff != null, "前提：附魔挂上了（`buffType` 不在 12/15/16 会被拒 ⇒ 先红这条）");
        if (eff == null) return;

        bool ran = W35Call(ctx, W35PlainCard(), u);
        CheckTrue(ran, "★★ A2：载体身上有一条 `Rally:` 附魔、被跑的卡自己**没有** Rally 正文 ⇒ **附魔那趟必须跑**（`TriggerAbilities.c:50-190`）" + LogTail(ctx));
        CheckTrue(Joined(ctx).Contains(W35HopLog), "★★ A3：日志里出现「" + W35HopLog + "」⇒ 真按附魔派发了（`FireTriggerAt(only: e)` 那一跳）" + LogTail(ctx));
    }

    // ==================================================================
    //  B · 自己那趟**有**正文 ⇒ 两趟都要跑（本批改的就是这一格）
    //  判据 = `TriggerAbilities.c:42-48` 的 `goto LAB_18094b822`：自己跑完与跳过自己
    //          **落到同一个附魔入口**（不是 if/else）
    // ==================================================================
    static void B97W35HopAfterOwn()
    {
        BattleContext ctx;
        var u = W35Setup("B", out ctx);
        CheckTrue(u != null && u.Instance != null, "前提：载体在场");
        if (u == null || u.Instance == null) return;
        var eff = Enchantments.Attach(ctx, u.Instance, W35Enchantment(), Enchantments.BuffTypeEnchantment);
        CheckTrue(eff != null, "前提：附魔挂上了");
        if (eff == null) return;

        bool ran = W35Call(ctx, W35OwnRally(), u);
        string log = Joined(ctx);
        CheckTrue(log.Contains("跑它自己的能力"), "前提：自己那趟跑了（`Rally:` 正文在 abilityCard 上）" + LogTail(ctx));
        CheckTrue(log.Contains(W35HopLog),
                  "★★★🧨 B3：**自己那趟跑完之后，附魔那趟也要跑**（原版共享尾块 `0x18094B822`）。"
                + "🧨 把 `RunAbilityOfAnotherCard` 末尾那句 `return __ownRan || RunActiveEnchantmentTriggerOf(...)` "
                + "改回 `return __ownRan;` ⇒ 本行红（改前就是这样：有正文就直接结束、附魔那趟永远不跑）" + LogTail(ctx));
        CheckTrue(ran, "★ B4：两趟都跑 ⇒ 返回真");
    }

    // ==================================================================
    //  C · 已 spent（`IsSpentEnchantment`）⇒ 跳过
    //  判据 = `TriggerAbilities.c:66`（`IsSpentEnchantment(param_4, 附魔卡)` 为真 ⇒ 不进 `TriggerAbility`）
    // ==================================================================
    static void B97W35SpentGate()
    {
        BattleContext ctx;
        var u = W35Setup("C", out ctx);
        CheckTrue(u != null && u.Instance != null, "前提：载体在场");
        if (u == null || u.Instance == null) return;
        var eff = Enchantments.Attach(ctx, u.Instance, W35Enchantment(), Enchantments.BuffTypeEnchantment);
        CheckTrue(eff != null, "前提：附魔挂上了");
        if (eff == null) return;
        eff.Used = true;
        Check(eff.Used, true, "前提：这条附魔已被标成「花掉」（`CardEffect.usedEnchantment`）");

        bool ran = W35Call(ctx, W35PlainCard(), u);
        CheckTrue(!ran, "🧨 C2：已 spent ⇒ 附魔那趟**跳过**（`.c:66`）。把 `Enchantments.AllSpent(...)` 那句删掉 ⇒ 本行红");
        CheckTrue(!Joined(ctx).Contains(W35HopLog), "🧨 C3：已 spent ⇒ 也不许打那条派发日志（同上）" + LogTail(ctx));
    }

    // ==================================================================
    //  D · 反例三条（防「别处乱扫」与「没载体也跑」）
    // ==================================================================
    static void B97W35NegativeControls()
    {
        // D1：载体身上**没附魔** ⇒ 不跑（改前改后都该是 false；它挡的是「恒真」这种实现）
        BattleContext ctx1;
        var u1 = W35Setup("D1", out ctx1);
        CheckTrue(u1 != null, "前提：D1 载体在场");
        if (u1 != null)
        {
            bool ran1 = W35Call(ctx1, W35PlainCard(), u1);
            CheckTrue(!ran1 && !Joined(ctx1).Contains(W35HopLog),
                      "🧨 D1：载体身上没附魔 ⇒ 附魔那趟不跑（写成恒真 / 全棋盘乱扫都会红）" + LogTail(ctx1));
        }

        // D2：附魔挂在**别的单位**身上 ⇒ 不跑（载体 = `abilityOwnerUnit`，不是「场上随便谁」）
        BattleContext ctx2;
        var u2 = W35Setup("D2", out ctx2);
        CheckTrue(u2 != null && u2.Instance != null, "前提：D2 载体在场");
        if (u2 != null && u2.Instance != null)
        {
            var otherCard = Unit("B97W35Other", 1, 1, 5);
            var other = Place(ctx2, 0, 2, otherCard);
            CheckTrue(other != null && other.Instance != null, "前提：另一个单位也摆上场");
            if (other != null && other.Instance != null)
            {
                var e2 = Enchantments.Attach(ctx2, other.Instance, W35Enchantment(), Enchantments.BuffTypeEnchantment);
                CheckTrue(e2 != null, "前提：附魔挂到**另一个**单位身上");
                bool ran2 = W35Call(ctx2, W35PlainCard(), u2);
                CheckTrue(!ran2 && !Joined(ctx2).Contains(W35HopLog),
                          "🧨 D2：附魔在**别人**身上 ⇒ 这一趟不跑（原版扫的是 `thisCard + 0x110`）" + LogTail(ctx2));
            }
        }

        // D3：没有载体（`abilityOwnerUnit == null`）⇒ 不跑（原版 `.c:42` 那道 `param_4 != 0` 闸）
        BattleContext ctx3;
        var u3 = W35Setup("D3", out ctx3);
        CheckTrue(u3 != null, "前提：D3 夹具建好了");
        bool ran3 = RuleCore.RunAbilityOfAnotherCard(ctx3, W35PlainCard(), KeywordTable.Rally, 0,
                                                     abilityOwnerUnit: null, actingCard: null, targetCard: null);
        CheckTrue(!ran3, "🧨 D3：没有载体单位 ⇒ 附魔那趟不跑（`.c:42 param_4 != 0` 那一道前置）" + LogTail(ctx3));
    }
}
