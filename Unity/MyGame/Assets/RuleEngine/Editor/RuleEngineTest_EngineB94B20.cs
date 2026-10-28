// RuleEngineTest_EngineB94B20.cs — 执行代理 `B20`（2026-10-29 · 第九十四会话 · 波 6 · 文件所有者 = `Core/EffectResolver.cs`）
//
// 为什么单开一个文件：简报里点名 `Editor/RuleEngineTest.cs` **归调度台**（接线归它）
// ⇒ 本代理**不碰那个文件**（一个文件同一时刻只能有一个写手）。
// 接线只差 `RuleEngineTest.cs` 的 `Run()` 里**一行**：
//     Step(CheckB94B20CreateAtPos);
//
// 本件覆盖 = 账 `战斗逻辑驱动:1122`（原版 `MinionManager.GetNextSlotToCard`）的**消费者**
// `RuleCore.SummonRequestSlotFor(ctx, owner, op)`（`Core/EffectResolver.cs` 类尾）。
// 🔴 每条断言都带 `🧨`（写清什么改动能让它红）。期望值一律写死字面量。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（接线 = `RuleEngineTest.cs` 的 `Run()` 里一行 `Step(CheckB94B20CreateAtPos)`）。
    /// ⚠️ 方法名**不以 `Test` 打头**（简报要求：入口用 `CheckB94B20…` 这一族）。</summary>
    public static void CheckB94B20CreateAtPos()
    {
        Section("🆕 引擎线 B20 · B94：召唤落点要读原版 `SummonCriteria.createAtPos`（账 1122 的消费端）");
        B94B20CaseSelf();
        B94B20CaseThirdAndOther();
        B94B20CaseCardTarget();
        B94B20CaseUnknownStaysDefault();
    }

    /// <summary>`createAtPos = 0 (self)` ⇒ **贴施放者所在侧再外一格**（原版 `GetNextSlotToCard` 的 `IndexOf + 2`）。
    /// <para>判据（第一权威）→ `decomp_full/AbilityLogic__PlayAbility.c:759-793`：
    /// `0 self` ⇒ `GetNextSlotToCard(area, abilityData.thisCard)`；`MinionManager__GetNextSlotToCard.c`
    /// 的「右表含该卡 ∧ 右侧还有位 ⇒ `IndexOf + 2`」= 我们的 `SlotOf(Right, index + 1)`。</para>
    /// <para>🧨 把 `case 0` 改回 `return -1`（或 `NextSlotToCard` 的落点算法改回「人少那侧最外一格」）⇒ 红。</para>
    /// </summary>
    static void B94B20CaseSelf()
    {
        var u = B94B20Summoner("B20U0");
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        int anchorSlot = BoardSlots.SlotOf(BoardSlots.Right, 0);
        var unit = Place(ctx, 0, anchorSlot, u);

        // 🧨 前提硬检查（先断前提、再断结论；⛔ 不写 `if (前提) Check(结论)`）
        CheckTrue(unit != null && unit.Card == u, "🧨 前提：施放者真的上了场");
        CheckTrue(!unit.IsWarlord, "🧨 前提：施放者不是督军（督军 ⇒ `NextSlotToCard` 直接返默认）");
        CheckTrue(BoardSlots.RoomOnSide(ctx.Players[0], BoardSlots.Right) > 0,
                  "🧨 前提：施放者所在那一侧还有位（否则原版走「都不中 ⇒ 默认」那一支）");
        ctx.ActingUnit = unit;

        var op = new EffectOp { Verb = "deploy", AbilityIndex = 0, LogicIndex = 0 };
        // 🧨 关键一条：默认落点 = 人少那侧最外一格；`self` = 贴施放者（右表 index0 ⇒ index1）
        int got = RuleCore.SummonRequestSlotFor(ctx, 0, op);
        Check(got, BoardSlots.SlotOf(BoardSlots.Right, 1),
              "🧨 `createAtPos=0 self` ⇒ 请求格 = 施放者同侧再外一格（不是默认落点）");
    }

    /// <summary>`createAtPos = 3 (friendlyHero)` ⇒ **与默认等价**（回 `-1`）；
    /// `createAtPos = 14 (nextFriendlyMinionSpot)` 及其余档 ⇒ 原版 `AddMinionOccupation(area,0)` +
    /// `LogWarning`、**落点仍默认** ⇒ 也回 `-1`（我们只出声）。</summary>
    static void B94B20CaseThirdAndOther()
    {
        var u = B94B20Summoner("B20U1");
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        var unit = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 0), u);
        CheckTrue(unit != null && unit.Card == u, "🧨 前提：施放者上了场");
        ctx.ActingUnit = unit;

        // 🧨 `3 friendlyHero` 若改成「真去贴督军那张」⇒ `NextSlotToCard` 会算出默认以外的一格 ⇒ 红
        Check(RuleCore.SummonRequestSlotFor(ctx, 0,
                  new EffectOp { Verb = "deploy", AbilityIndex = 1, LogicIndex = 0 }), -1,
              "🧨 `createAtPos=3 friendlyHero` ⇒ 与默认等价（`GetNextSlotToCard` 见 Hero 直接返默认）");

        // 🧨 把 `default:` 改成「任何档都当 self」⇒ 这一条会给出非 -1 ⇒ 红（原版这一档出声 + 默认）
        Check(RuleCore.SummonRequestSlotFor(ctx, 0,
                  new EffectOp { Verb = "deploy", AbilityIndex = 2, LogicIndex = 0 }), -1,
              "🧨 `createAtPos=14`（不在 {0,1,3} 三档内）⇒ 出声 + 默认落点（回 -1）");
    }

    /// <summary>`createAtPos = 1 (cardTarget)` ⇒ 贴**卡目标**（我们 `ctx.LastTarget`）；
    /// 取不到卡目标 ⇒ 出声 + 默认（回 `-1`）。</summary>
    static void B94B20CaseCardTarget()
    {
        var u = B94B20Summoner("B20U2");
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        var unit = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 0), u);
        CheckTrue(unit != null && unit.Card == u, "🧨 前提：施放者上了场");
        ctx.ActingUnit = unit;

        var op = new EffectOp { Verb = "deploy", AbilityIndex = 3, LogicIndex = 0 };

        // 没有卡目标 ⇒ 默认
        ctx.LastTarget = null;
        Check(RuleCore.SummonRequestSlotFor(ctx, 0, op), -1,
              "🧨 `createAtPos=1` 但取不到卡目标 ⇒ 出声 + 默认落点（回 -1）");

        // 有卡目标 ⇒ 贴它（夹具里选施放者自己那张，右表 index0 ⇒ index1）
        ctx.LastTarget = unit;
        Check(RuleCore.SummonRequestSlotFor(ctx, 0, op), BoardSlots.SlotOf(BoardSlots.Right, 1),
              "🧨 `createAtPos=1 cardTarget` ⇒ 请求格 = 卡目标同侧再外一格");
    }

    /// <summary>**判不了的一律保持旧行为**（回 `-1`）：`op` 没有 (ab, lg) / 卡没有 `abilitySummon` 列 /
    /// 该 (ab, lg) 没有行 / 该行没有 `summonCriteria`。⛔ 这不是「没写就是 self」。</summary>
    static void B94B20CaseUnknownStaysDefault()
    {
        var u = B94B20Summoner("B20U3");
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Active = 0;
        var unit = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Right, 0), u);
        CheckTrue(unit != null && unit.Card == u, "🧨 前提：施放者上了场");
        ctx.ActingUnit = unit;

        // 🧨 `op.AbilityIndex < 0` 那一句早退若被删 ⇒ 会拿 `AbilitySummonAt(-1, -1)` 的结果，
        //   而下面这条专门测「没归位就等于默认」——删了早退这里仍得 -1（`AbilitySummonAt` 自己判 `<0`），
        //   所以再补一条「卡上根本没有这一列」的测法见下。
        Check(RuleCore.SummonRequestSlotFor(ctx, 0,
                  new EffectOp { Verb = "deploy" }), -1,
              "🧨 这条 op 没归到任何 ability 行（ab/lg = -1）⇒ 默认落点");

        // 卡上**没有** `abilitySummon` 列（旧表 / 手写夹具）⇒ `AbilitySummons == null` ⇒ 判不了 ⇒ 默认
        var bare = new CardDef("B20Bare", "B20Bare", "unit", "", null, "Test", 1, 1, 5, 0);
        CheckTrue(bare.AbilitySummons == null, "🧨 前提：这张夹具卡**没有** `abilitySummon` 列（= 未知）");
        var bareUnit = Place(ctx, 0, BoardSlots.SlotOf(BoardSlots.Left, 0), bare);
        CheckTrue(bareUnit != null && bareUnit.Card == bare, "🧨 前提：第二张施放者也上了场");
        ctx.ActingUnit = bareUnit;
        Check(RuleCore.SummonRequestSlotFor(ctx, 0,
                  new EffectOp { Verb = "deploy", AbilityIndex = 0, LogicIndex = 0 }), -1,
              "🧨 卡上没有 `abilitySummon` 列 ⇒ 判不了 ⇒ 与加这一列之前逐字相同（默认落点）");
    }

    /// <summary>夹具卡：一张普通单位 + 四行 `abilitySummon`（(0,0)=self · (1,0)=friendlyHero ·
    /// (2,0)=14 · (3,0)=cardTarget）。</summary>
    static CardDef B94B20Summoner(string id)
    {
        var c = new CardDef(id, id, "unit", "Deploy a thing", null, "Test", 1, 1, 5, 0);
        c.AttachAbilitySummon(new[]
        {
            new CardDef.AbilitySummonSpec { Ab = 0, Lg = 0, CreateAtPos = 0 },
            new CardDef.AbilitySummonSpec { Ab = 1, Lg = 0, CreateAtPos = 3 },
            new CardDef.AbilitySummonSpec { Ab = 2, Lg = 0, CreateAtPos = 14 },
            new CardDef.AbilitySummonSpec { Ab = 3, Lg = 0, CreateAtPos = 1 },
        });
        return c;
    }
}
