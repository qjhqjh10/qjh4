// B98G1_Assert.cs — 第九十八会话 · 波 5b · 执行代理 `B98_G1`
//
// ============================ 它盯的是什么 ============================
//  本批 8 条**全部落在 `RuleEngine/Core/RuleCore.cs`**（零行数变化，逐行钉住）：
//   · **A**（账 241）`CleanupDeaths` 残骸支补丁 `CancelEffectsFromDisappearingUnit(ctx, u, u, true)` ——
//     原版 `_ResolveTransformIntoRemnant_d__554__MoveNext.c:40-42` 调 `RemoveSelfBuffsFromQueues`，
//     其全文两句 = `CancelEffectsFromDisappearingUnit(bm, card, card, 1)`。
//     ⚠️ 实参**形状**另由 `B97W38_Assert.cs` B 段钉（它只调 `ActionPump` 那一口、不依赖补丁落地）；
//     本段钉的是**补丁真的接在残骸支里、而且时机在入队之后立刻**（不靠泵点兜底 `ActionPump.cs:1491`）。
//   · **B**（账 534①）`ActiveRowEntersPlan` 加 `|| ca == 350`（`removeFromCemetery` 那一档
//     正文 op 为空时也要进计划表）。
//   · **C**（账 853）回合末 / 回合开始**两个入队口**从 `() => unit.RemoveTempBuff(buff)` 换成
//     「按记录摘」`UnitState.RemoveEffectRecord(new EffectRecord{ Permanent, TempBuffRecord })` ——
//     原版 `CardScript__RemoveEffect.c:43-46` 第一句 `permanentBuff(+0x74) != 0 ⇒ return`。
//     **可观测差异只在那道 permanent 闸**（lane 与逐 case 结果都不变）。
//   · **D**（账 1004）`ApplyDamageCore` 的**可见格** `ctx.DamageToEnemyWarlord` 归属改成**攻方座位**
//     （`_rawDealer`），原版 `DamageInfo.IsPlayer()` = `actingCard.isPlayer`。
//   · **E**（账 1285）附魔递归前的 `Enchantments.AllSpent` 闸 —— 本段钉的是它**赖以成立的前提**：
//     4 个附着口都经 `Enchantment.Attach` 写 `ActiveCardEffect.EnchantmentCard`（`Enchantment.cs:278`
//     全仓唯一写点）⇒ `AllSpent` 不会把刚挂上的附魔判成「已花」而**整族静默跳过**。
//   · **F**（账 573）`Draw` 里先标 `cardState 16`、离库即清。
//   · **G**（账 867）纯注释（保留 `AbilityCostGate` 那道闸、写死「原版非 oath 主动能力零费用」）——
//     ⛔ 无代码行为可断，本段**不写断言**（见报告「还差什么」）。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//   · 施工单 → `资料/完全复刻/判据/B97_R10_波5残差归属.md` §二（241 / 534① / 853 / 1004 / 1285）
//     ＋ `判据/B98_R4_波5补判据.md` §573 / §867（这两条是本批追加的）
//   · 第一权威 → `d:/2/tools/decomp_full/`（逐条方法体，行号见报告）
//   · 本件报告 → `资料/完全复刻/报告/B98_G1.md`
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// `Editor/RuleEngineTest.cs` 的 `Run()` 里加**一行块语句**（建议位置 = `{ CheckB97W38Gaps(); }` **之后**）：
//       // 🆕 2026-10-29（调度台代接线）：`B98_G1` —— 波 5b 战斗线 8 条（RuleCore.cs 零行数变化）。
//       { B98G1_Assert.Run(); }
//   🔴 **故意不用 `Step(...)`**（同 `B97W1` / `B97W38` 的理由：`Step` 会让 `_ranCases` 记账、
//     而 `工具/gen_diag_runner.py` 只认「8 空格缩进的顶层语句」，两者都得跟着改）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · **A3**：把 `RuleCore.cs:9871` 那句 `CancelEffectsFromDisappearingUnit(ctx, u, u, true);` 删掉
//     ⇒ 条目会留到**泵点**才被 `CarrierReplacedByRemnant` 兜底撤掉 ⇒ 本段在 `using (BeginAction)`
//     **内层**读队列（外层还没泵）⇒ 仍能找到条目 ⇒ **A3 红**。
//     把实参写成 `(ctx, u, null, true)` ⇒ 邻居那条（`Actor == other`）**不会**被撤（那是 Actor 筛），
//     但「Actor 相同、Target 为空」的会被撤 ⇒ A4 的对照款能抓到（见 A4）。
//   · **B2**：把 `|| ca == 350` 删掉 ⇒ 正文 op 为空的行 ⇒ **B2 红**。
//   · **C2/C4**：把 `Permanent` 那格去掉（或恒 `false`）⇒ 带 permanent 的那条**会被摘掉** ⇒ **C2/C4 红**。
//   · **D2**：把 `_rawDealer` 换回 `1 - _own` ⇒ 打自己人时记到**对面**格 ⇒ **D2 红**。
//   · **E2**：若某个 `Attach` 口不写 `EnchantmentCard` ⇒ `AllSpent` 在「一条都找不到」时返**真**
//     ⇒ 该族整族被跳过 ⇒ **E2 红**（这正是施工单点名必须先核的那一点）。
//   · **F3**：把 `ClearDeckDrawingMark(ctx, inst);` 删掉 ⇒ 抽到的那张若被标过就永远留着 ⇒ **F3 红**
//     （⚠️ 见下面「已知边界」第 2 条：方向修复前 F3 的判别力有限）。
//
// ============================ 已知边界（⛔ 不是「已验收」）============================
//   1. **A 段今天零可观测于真牌局**：原版那四个调用点里，只有「变残骸」这条在本仓有生产端；
//      而泵点 `ActionPump.cs:1491` 的 `CarrierReplacedByRemnant` 兜底**结果等价**（时机更晚）。
//      本段是**时机**判别式（内层读队列），不是「结果」判别式。
//   2. 🔴 **`SetNextCardAsDeckDrawing` 的扫描方向与本仓牌库镜像约定相反**（顺手发现，未改 ——
//      那个文件 `RuleCorePartial_B97W34.cs` 不在本批白名单）：它从 `ps.Deck[0]` 起扫，
//      而本仓 `Deck` **末尾才是顶**（`MoveDeckTopToHand` 取 `Count-1`，见 `RuleCore.cs:3127` 的注释）。
//      ⇒ F 段只钉**方向无关**的那几条（抽到的那张不许带标记 / 标记表只有一张）；
//      「抽完之后标记表必须为空」这一条**要等方向修复落地后才写**（见报告 §还差什么）。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 B98_G1：波 5b 战斗线 8 条（全部落在 `RuleEngine/Core/RuleCore.cs`）。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B98G1_Assert
{
    static readonly CheckSink _sink = new CheckSink("[G1] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B98_G1 波 5b：241 / 534① / 853 / 1004 / 1285 / 573（RuleCore.cs 零行数变化）");

        SectionA_CancelOnRemnant();
        SectionB_ActiveRowPlan350();
        SectionC_RemoveEffectRecordPorts();
        SectionD_VisibleWarlordCellSeat();
        SectionE_EnchantmentSpentPredicate();
        SectionF_DeckDrawingMark();

        Debug.Log(_sink.P + "B98G1 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具
    static CardDef Troop(string name)
    {
        return new CardDef(name, name, "troop", "", null, "SpaceWolves", 1, 1, 3, 0, null, subtype: "troop");
    }

    static List<CardDef> DeckOf(string faction)
    {
        return new List<CardDef>
        {
            new CardDef("g1hero_" + faction, "G1Hero", "hero", "", null, faction, 0, 0, 30, 0, null, subtype: "Warlord"),
        };
    }

    static BattleContext NewCtx()
    {
        return RuleCore.NewBattle(DeckOf("SpaceWolves"), DeckOf("Goff"), seed: 0, shuffle: false);
    }

    /// <summary>四条道里有没有这个标签的条目（⛔ 只给断言用）。</summary>
    static bool HasLabel(BattleContext ctx, string label)
    {
        var lanes = new[] { ActionPriority.Player, ActionPriority.Instant, ActionPriority.Interrupt, ActionPriority.DelayedInstant };
        for (int l = 0; l < lanes.Length; l++)
        {
            int n = ctx.ActionQueue.CountOf(lanes[l]);
            for (int i = 0; i < n; i++)
            {
                var a = ctx.ActionQueue.At(lanes[l], i);
                if (a != null && a.Label == label) return true;
            }
        }
        return false;
    }

    /// <summary>这条限时增益还在不在 `_buffs` 里。</summary>
    static bool HasBuff(UnitState u, UnitState.TempBuff b)
    {
        if (u == null || b == null) return false;
        var list = u.TempBuffs;
        for (int i = 0; i < list.Count; i++) if (ReferenceEquals(list[i], b)) return true;
        return false;
    }

    // ============================================================ A 账 241：残骸支撤掉载体自己的排队动作
    static void SectionA_CancelOnRemnant()
    {
        MenuCheck.Section(_sink, "A 账 241：`CleanupDeaths` 残骸支 `CancelEffectsFromDisappearingUnit(ctx, u, u, true)`（时机 = 入队之后立刻）");

        var ctx = NewCtx();
        var ps = ctx.Players[0];
        var leaver = new UnitState(ctx.NewInstance(Troop("G1Leaver")), false);
        leaver.AddKeyword(KeywordTable.Remnant, 1);   // 只有带 remnant/waystone 才走那条支（`RuleCore.cs:9870`）
        var other = new UnitState(ctx.NewInstance(Troop("G1Other")), false);
        int slot = BoardSlots.SlotOf(BoardSlots.Left, 0);
        ps.Board[slot] = leaver;

        Tr(leaver.Has(KeywordTable.Remnant), "A0 **前提断言**：离场那张带 `remnant`（否则根本不走残骸支）");
        Tr(!leaver.IsRemnant, "A0′ **前提断言**：它此刻还不是残骸");
        Tr(ReferenceEquals(ps.Board[slot], leaver), "A0″ **前提断言**：它真在棋盘上（`CleanupDeaths` 按格扫）");

        int ran = 0;
        RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "g1-remnant", () => ran++,
                                actor: leaver, abilityTarget: leaver, actionType: 0);
        RuleCore.EnqueueAbility(ctx, ActionPriority.Instant, "g1-other", () => { },
                                actor: other, abilityTarget: other, actionType: 0);
        Tr(HasLabel(ctx, "g1-remnant"), "A1 **前提断言**：载体自己的那条入队成功");
        Tr(HasLabel(ctx, "g1-other"), "A1′ **前提断言**：邻居那条也入队成功（A4 的对照款）");

        bool becameRemnant;
        bool stillQueued;
        // 🔴 **在外层 `BeginAction` 里读** —— `Hurt` 自带的作用域是**内层**（嵌套空转、不泵）
        //   ⇒ 此刻看到的队列就是「残骸支跑完、泵点还没兜底」的那一瞬。
        using (RuleCore.BeginAction(ctx))
        {
            RuleCore.HurtWithSourceForTest(ctx, leaver, 99, "G1");
            becameRemnant = ps.Board[slot] != null && ps.Board[slot].IsRemnant
                            && !ReferenceEquals(ps.Board[slot], leaver);
            stillQueued = HasLabel(ctx, "g1-remnant");
        }

        Tr(becameRemnant, "A2 **前提断言**：这一下真的把 `G1Leaver` 翻成了残骸（补丁的落点就在那条支里）");
        Tr(!stillQueued,
           "A3 ★★ 残骸支在【入队之后立刻】撤掉载体自己的排队动作"
           + "｜🧨 删掉 `RuleCore.cs:9871` 那句 `CancelEffectsFromDisappearingUnit(ctx, u, u, true)` ⇒ "
           + "条目会留到泵点才被 `CarrierReplacedByRemnant` 兜底 ⇒ 本行在**内层**读到它在 ⇒ 红");
        Tr(HasLabel(ctx, "g1-other"),
           "A4 ★ 别的载体的条目**一条都没动**（`(u, u, true)` 的 ① `casterCard == jammedUnit` 那一道筛）");
        C(ran, 0, "A5 被撤掉的那条**没有执行**（撤 ≠ 跑；🧨 若只剩泵点兜底，此格同样是 0 —— 判别力在 A3）");
    }

    // ============================================================ B 账 534①：ca == 350 进计划表
    static void SectionB_ActiveRowPlan350()
    {
        MenuCheck.Section(_sink, "B 账 534①：`ActiveRowEntersPlan` 放行 `ca == 350 (removeFromCemetery)`");

        var ctx = NewCtx();
        var u = new UnitState(ctx.NewInstance(Troop("G1Row350")), false);

        Tr(!RuleCore.ActiveRowHasOpsForTest(u, 0),
           "B0 **前提断言**：这张卡的第 0 行**没有**正文 op（否则 `ActiveRowHasOps` 顶上 ⇒ C 段假绿）");
        Tr(RuleCore.ActiveRowEntersPlanForTest(u, 0, 350),
           "B1 ★ `ca == 350` 在正文 op 为空时**也**进计划表"
           + "｜判据 `BattleManager__ResolveRemoveFromCemetery.c:82-134`（执行器 `RunRemoveFromCemeteryRow` 早已在）");
        Tr(!RuleCore.ActiveRowEntersPlanForTest(u, 0, 999),
           "B2 ★★ 灭自证：不在名单里的 `ca`（999）**仍然不放行**"
           + "｜🧨 把 `return … || ca == 350 || …` 写坏成 `return true` ⇒ 本行红");
        Tr(RuleCore.ActiveRowEntersPlanForTest(u, 0, 85) && RuleCore.ActiveRowEntersPlanForTest(u, 0, 360)
           && RuleCore.ActiveRowEntersPlanForTest(u, 0, 370) && RuleCore.ActiveRowEntersPlanForTest(u, 0, 400),
           "B3 回归：原有四档（85 / 360 / 370 / 400）一个都没掉");
    }

    // ============================================================ C 账 853：两个入队口换成「按记录摘」
    static void SectionC_RemoveEffectRecordPorts()
    {
        MenuCheck.Section(_sink, "C 账 853：回合末 / 回合开始两个入队口 ⇒ `RemoveEffectRecord`（`permanentBuff != 0 ⇒ 不摘`）");

        // ---- 回合末那一档（`DelayedInstant`，`EnqueueEndOfTurnEffectRemovals`）----------
        var ctx = NewCtx();
        var ps = ctx.Players[0];
        var u = new UnitState(ctx.NewInstance(Troop("G1BuffHolder")), false);
        ps.Board[BoardSlots.SlotOf(BoardSlots.Left, 0)] = u;

        var permBuff = new UnitState.TempBuff { Name = "attack", Value = 2, Src = "G1", SourceCard = "G1" };
        var normBuff = new UnitState.TempBuff { Name = "attack", Value = 1, Src = "G1", SourceCard = "G1" };
        u.AddTempBuff(permBuff);
        u.AddTempBuff(normBuff);
        Tr(permBuff.Mod != null && normBuff.Mod != null,
           "C0 **前提断言**：两条限时增益都建出了背后的 `StatMod`（`Permanent` 读的就是它）");
        permBuff.Mod.Permanent = true;    // = 原版 `CardEffect.permanentBuff(+0x74) != 0`（今天全仓无生产点 ⇒ 夹具手工置）

        int queuedEnd = RuleCore.EnqueueEndOfTurnEffectRemovals(ctx, 0);
        Tr(queuedEnd >= 2, "C1 **前提断言**：回合末那一档把两条都入队了（只判下界 —— 场上可能还有别的到期增益）");
        using (RuleCore.BeginAction(ctx)) { }

        Tr(HasBuff(u, permBuff),
           "C2 ★★ `permanentBuff != 0` 的那条**摘不掉**（判据 `CardScript__RemoveEffect.c:43-46` 第一句）"
           + "｜🧨 把 `Permanent = buff.Mod != null && buff.Mod.Permanent` 那格去掉 ⇒ 它被摘掉 ⇒ 红");
        Tr(!HasBuff(u, normBuff),
           "C3 普通那条照旧被摘掉（换成 `RemoveEffectRecord` 之后逐 case 结果不变）");

        // ---- 回合开始那一档（`Instant`，`EnqueueTurnSetupEffectRemovals`）--------------
        var ctx2 = NewCtx();
        var ps2 = ctx2.Players[0];
        var u2 = new UnitState(ctx2.NewInstance(Troop("G1BuffHolder2")), false);
        ps2.Board[BoardSlots.SlotOf(BoardSlots.Left, 0)] = u2;
        // `IsDueAtTurnStart(turnPlayer=0)`：`UntilOpponentNextTurn ? (Owner != turnPlayer) : …`
        //   ⇒ Owner = 1（对手施加）+ `UntilOpponentNextTurn` ⇒ 0 回合开始到期
        var permBuff2 = new UnitState.TempBuff { Name = "health", Value = 2, Owner = 1, UntilOpponentNextTurn = true, Src = "G1", SourceCard = "G1" };
        var normBuff2 = new UnitState.TempBuff { Name = "health", Value = 1, Owner = 1, UntilOpponentNextTurn = true, Src = "G1", SourceCard = "G1" };
        u2.AddTempBuff(permBuff2);
        u2.AddTempBuff(normBuff2);
        Tr(permBuff2.Mod != null && normBuff2.Mod != null, "C4′ **前提断言**：回合开始那两条也建出了 `StatMod`");
        permBuff2.Mod.Permanent = true;

        int queuedStart = RuleCore.EnqueueTurnSetupEffectRemovals(ctx2, 0);
        Tr(queuedStart >= 2, "C4 **前提断言**：回合开始那一档把两条都入队了（只判下界）");
        using (RuleCore.BeginAction(ctx2)) { }

        Tr(HasBuff(u2, permBuff2), "C5 ★★ 回合开始那一档同样守住 `permanentBuff != 0`（🧨 只改一个口 ⇒ 本行红）");
        Tr(!HasBuff(u2, normBuff2), "C6 回合开始那一档的普通那条照旧被摘掉");
    }

    // ============================================================ D 账 1004：可见格按【攻方座位】
    static void SectionD_VisibleWarlordCellSeat()
    {
        MenuCheck.Section(_sink, "D 账 1004：`ctx.DamageToEnemyWarlord` 的可见格按【攻方座位】`_rawDealer`（原版 `DamageInfo.isPlayer`）");

        var ctx = NewCtx();
        var ps0 = ctx.Players[0];
        Tr(ps0.Warlord != null && ps0.Warlord.IsWarlord,
           "D0 **前提断言**：0 号玩家有督军（`NewBattle` 建的那一份，`IsWarlord = true`）");

        var attacker = new UnitState(ctx.NewInstance(Troop("G1Attacker")), false);
        ps0.Board[BoardSlots.SlotOf(BoardSlots.Left, 0)] = attacker;

        C(ctx.DamageToEnemyWarlord[0], 0, "D1 **前提断言**：0 号位（己方）那一格起始为 0");
        C(ctx.DamageToEnemyWarlord[1], 0, "D1′ **前提断言**：1 号位（对面）那一格起始为 0");

        // 「自己打自己人」—— 攻方 0 号、受害者【也是】0 号的督军（对照：改前会记到对面那一格）
        RuleCore.HurtWithSourceForTest(ctx, ps0.Warlord, 3, "G1",
                                       killer: 0, actingCard: attacker, kind: DamageKind.Melee);

        C(ctx.DamageToEnemyWarlord[0], 3,
          "D2 ★★ 记在**攻方（0 号）**那一格（原版 `DamageInfo__IsPlayer.c` 全文一句 = `return *(actingCard + 0x40)`）"
          + "｜🧨 把 `_rawDealer` 换回 `1 - _own` ⇒ 自己打自己人记到对面 ⇒ 本行变 0、D3 变 3 ⇒ 红");
        C(ctx.DamageToEnemyWarlord[1], 0,
          "D3 ★ 对面那一格**一分都没记**（🧨 同上，两格会互换）");
    }

    // ============================================================ E 账 1285：附魔「已花」闸的前提
    static void SectionE_EnchantmentSpentPredicate()
    {
        MenuCheck.Section(_sink, "E 账 1285：`Enchantments.AllSpent` 闸 —— 4 个附着口都写 `EnchantmentCard` 的前提（否则整族静默跳过）");

        var ctx = NewCtx();
        var carrierDef = Troop("G1Carrier");
        var enchDef = new CardDef("g1ench", "G1Ench", "enchantment", "", null, "SpaceWolves",
                                  0, 0, 0, 0, null, subtype: "enchantment");
        var carrier = ctx.NewInstance(carrierDef);

        var eff = Enchantments.Attach(ctx, carrier, enchDef, Enchantments.BuffTypeSingleUse);
        Tr(eff != null, "E0 **前提断言**：`Attach` 真的挂上了（buffType 15 ∈ 附魔三档 12/15/16）");
        C(carrier.ActiveEffects.Count, 1, "E0′ **前提断言**：载体身上正好一条 `ActiveCardEffect`");
        Tr(eff != null && ReferenceEquals(eff.EnchantmentCard, enchDef),
           "E1 **前提断言**：`ActiveCardEffect.EnchantmentCard` 写上了（`Enchantment.cs:278` 全仓唯一写点）");

        Tr(!Enchantments.AllSpent(carrier, enchDef),
           "E2 ★★ 刚挂上（没花）⇒ `AllSpent` 必须 **false** ⇒ 触发循环不跳"
           + "｜🧨 某个附着口不写 `EnchantmentCard` ⇒ `AllSpent` 在「一条都找不到」时返**真** ⇒ 整族被静默跳过 ⇒ 红");

        Tr(Enchantments.Spend(ctx, carrier, enchDef), "E3 **前提断言**：`Spend` 把这条花掉了（buffType 15 才生效）");
        Tr(Enchantments.AllSpent(carrier, enchDef),
           "E4 ★ 花掉之后 ⇒ `AllSpent` 真 ⇒ 触发循环跳过（这就是账 1285 补的那道闸）");

        Tr(!Enchantments.AllSpent(null, enchDef), "E5 ★ 灭自证：`carrier == null` ⇒ **false**（不误跳）；照原版 null 安全");
        Tr(!Enchantments.AllSpent(carrier, null), "E6 ★ 灭自证：`enchantmentCard == null` ⇒ **false**（不误跳）");
    }

    // ============================================================ F 账 573：牌库顶「正在抽」标记
    static void SectionF_DeckDrawingMark()
    {
        MenuCheck.Section(_sink, "F 账 573：`Draw` 先标 `cardState 16`、离库即清（`SetNextCardAsDeckDrawing` / `ClearDeckDrawingMark`）");

        var ctx = NewCtx();
        var ps = ctx.Players[0];
        // 自己铺一副**确定**的牌库（`NewBattle` 会把起手抽走、`DeckOf` 只有 1 张 ⇒ 不够「两张」这个前提）。
        // 本仓「牌库顶」= 列表末尾（`RuleCore.cs:3127` 的镜像约定）⇒ 最后加进去的那张就是下一张被抽的。
        ps.Deck.Clear();
        ps.Deck.Add(ctx.NewInstance(Troop("G1DeckA")));
        ps.Deck.Add(ctx.NewInstance(Troop("G1DeckB")));
        ps.Deck.Add(ctx.NewInstance(Troop("G1DeckC")));

        Tr(ps.Deck.Count >= 2, "F0 **前提断言**：牌库至少两张（否则「抽走的那张 vs 顶上那张」验不了）");
        C(ctx.DeckDrawingInstanceIds.Count, 0, "F1 **前提断言**：标记表起始是空的");

        var drawnTop = ps.Deck[ps.Deck.Count - 1];   // 本仓「牌库顶」= 列表末尾（`RuleCore.cs:3127` 的镜像约定）
        Tr(drawnTop != null, "F1′ **前提断言**：拿得到顶上那一张");

        RuleCore.Draw(ctx, 0);

        Tr(ps.Hand.Count > 0, "F2 **前提断言**：这一抽真的进了手牌（否则下面那条会被「抽空了」掩盖）");
        Tr(!RuleCore.IsDeckDrawing(ctx, drawnTop),
           "F3 ★★ 抽到的那张**不带「正在抽」标记**（离库即清；🧨 删掉 `MoveDeckTopToHand` 里的 `ClearDeckDrawingMark(ctx, inst);` ⇒ "
           + "若它真被标过就永远留着 ⇒ 红）");
        Tr(ctx.DeckDrawingInstanceIds.Count <= 1,
           "F4 标记表不会累积（一次抽牌最多留下一张「下一张」的标记）"
           + "｜🔴 **方向修复落地后应改成 `== 0`** —— 见本文件头「已知边界」第 2 条");
    }
}
