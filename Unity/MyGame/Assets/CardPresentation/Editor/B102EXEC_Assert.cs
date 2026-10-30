// B102EXEC_Assert.cs — 执行代理 `B102_EXEC`（2026-10-30）的断言：账 `战斗逻辑驱动:1282` / `:1380`
//   （`RawCardScript.ResolveActiveAbility` 的两个重载）的**批 0 = 2 条只差接线的码**（`380` / `480`）。
//
// ============================ 它盯的是什么 ============================
//   原版 `AbilityLogic.PlayAbility` 按 `chosenAbility`（`AbilityLogic + 0x10`）逐支分派；我们这一层
//   的等价物 = `RuleCore.RunActiveRowOpsOrGrantedTrigger`（`EffectResolver.cs` 类尾那一段）。
//   这两支的**执行体本来就已经存在**（`SetReferenceCards` / `EmitBloodThirst`），缺的只是「在 ca 分派里
//   调到它」+ 「计划表那道闸放它进去」（`RuleCore.ActiveRowEntersPlan`）。本文件钉的就是这两跳。
//      · `380 setReferenceCard` → 判据 `decomp_full/AbilityLogic__PlayAbility.c:2128-2160`
//      · `480 highlightBloodThirst` → 判据 `decomp_full/AbilityLogic__PlayAbility.c:1960-1972`
//   施工单（⛔ 不在这里抄第二份）→ `资料/完全复刻/判据/B102_R4_C类大工程两条.md` §①（1·3 表 + 1·4 批0）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   A0-2/A0-3  删掉 `RuleCore.ActiveRowEntersPlan` 里的 `|| ca == 380` / `|| ca == 480` ⇒ 那两条红
//              （= 那一行会被当 `chosenAbility=…` 剔进 `unimplemented`，执行器永远走不到）。
//   A1-380     删掉 `EffectResolver.RunActiveRowOpsOrGrantedTrigger` 里的 `if (ca == 380)` ⇒ 落回 op
//              那一支（夹具**没有正文 op**）⇒ 返回 0、引用表不动 ⇒ A1-380 与 A1-380b 全红。
//   A1-380c    删掉 `SetReferenceCards` 里那句 `if (got.Count == 0) return 0;` ⇒ 「目标表为空」也照样
//              `Clear()` ⇒ 引用表被清空 ⇒ 红（原版 `:2131-2132` 的 `if (0 < count)`）。
//   A1-380d    把「解不出目标集」那条改用「己方全体」之类的近似 ⇒ 返回 1 ⇒ 红（会静默写错引用表）。
//   A1-480     删掉 `if (ca == 480)` ⇒ 返回 0、`ctx.Signals` 一条都不多 ⇒ 红。
//   A1-480b    把 480 写成「无条件 `ctx.Emit(EvtKind.Trigger, …)`」（绕开 `EmitBloodThirst` 的四道闸）
//              ⇒ 目标没有 bloodthirst 也发一条 ⇒ 红。
//   A1-480c    对着**施放者**而不是**目标**发（原版 `:1968` 取的是目标表第 0 个）⇒ 格号对不上 ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   接在 `CardPresentation/Editor/BattleScene.cs:46705` 那条**方法尾聚合行**的
//   `Debug.Log(P + $"=== 合计：{pass} 通过 / {fail} 失败 ===");` **之前**，同一物理行追加：
//     `B102EXEC_Assert.Run(); pass += B102EXEC_Assert.Pass; fail += B102EXEC_Assert.Fail;`
//   （同一物理行追加 ⇒ 行数零变化；`Pass`/`Fail` 是累计值 ⇒ 只在 `Run()` 之后加一次。）
//   本文件**只用引擎侧**（`RuleCore.NewBattle` 自建一局），不需要 `BattleDriver` / 不建 GameObject。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>账 `战斗逻辑驱动:1282`/`:1380` 批 0：`chosenAbility 380 / 480` 的执行器接线。</summary>
public static class B102EXEC_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B102EXEC] ") { Near = MenuNearStyle.Compact2 };

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
        var d0 = new List<CardDef> { Hero("B102EXEC_W0") };
        for (int i = 0; i < 20; i++) d0.Add(PlainUnit("B102EXEC_f" + i));
        var d1 = new List<CardDef> { Hero("B102EXEC_W1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
    }

    static UnitState Put(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>从 `skip` 下标起数「`EvtKind.Trigger` 且关键词 = 参数」的信号条数。</summary>
    static int CountTriggerSignals(BattleContext ctx, string keyword, int skip)
    {
        int n = 0;
        var sig = ctx.Signals;
        for (int i = skip; i < sig.Count; i++)
        {
            var e = sig[i];
            if (e == null || e.Kind != EvtKind.Trigger) continue;
            if (!string.Equals(e.Keyword, keyword, System.StringComparison.Ordinal)) continue;
            n++;
        }
        return n;
    }

    // ============================================================ 入口

    public static void Run()
    {
        MenuCheck.Section(_sink, "B102EXEC：账 `战斗逻辑驱动:1282`/`:1380` 批 0 —— `chosenAbility 380 / 480` 接进执行器");
        CaseGate();
        Case380();
        Case480();
        Debug.Log(_sink.P + $"B102EXEC 合计：{_sink.Pass} 通过 / {_sink.Fail} 失败");
    }

    // ============================================================ A0 · 硬前提 + 计划表闸

    static void CaseGate()
    {
        Debug.Log(_sink.P + "--- A0 硬前提 + 计划表闸（`RuleCore.ActiveRowEntersPlan`）---");

        var ctx = NewCtx();
        int me = ctx.Active;
        var u = Put(ctx, me, 3, PlainUnit("B102EXEC_gate"));      // ⛔ 别占 4 号格：那是督军位（`BoardSpec.WarlordSlot`）

        // 🔴 先钉前提（硬检查，⛔ 不是 `if (前提) Check(结论)` 那种静默跳过）
        Tr(u != null && u.Card != null, "（前提）夹具卡上得了棋盘");
        C(RuleCore.ActiveRowHasOpsForTest(u, 0), false,
          "★ A0-1 夹具这张卡的行 0 **没有配到正文 op**（`ActiveRowHasOpsForTest == false`）"
        + "｜🧨 若它变成 true，下面 A0-2/A0-3 的 true 就会被「正文 op」顶掉 ⇒ 这两条测不到闸");

        C(RuleCore.ActiveRowEntersPlanForTest(u, 0, 380), true,
          "★★ A0-2 `ActiveRowEntersPlan(…, 380) == true` —— 380 行能进「这一次跑哪几行」的计划表"
        + "（判据 `AbilityLogic__PlayAbility.c:2128-2160`）"
        + "｜🧨 删掉 `RuleCore.cs` 那格闸里的 `|| ca == 380` ⇒ 实得 False ⇒ 红"
        + "（那一行会被当 `chosenAbility=380` 剔进 `unimplemented` ⇒ plan 为空 ⇒ 执行器成死代码）");

        C(RuleCore.ActiveRowEntersPlanForTest(u, 0, 480), true,
          "★★ A0-3 `ActiveRowEntersPlan(…, 480) == true`（判据 `AbilityLogic__PlayAbility.c:1960-1972`）"
        + "｜🧨 删掉 `|| ca == 480` ⇒ 实得 False ⇒ 红");

        C(RuleCore.ActiveRowEntersPlanForTest(u, 0, 999), false,
          "★ A0-4 **对照**：`ca = 999`（既不在闸里、也没有正文 op）⇒ false"
        + "｜🧨 把闸写成恒 true（或「`ca >= 380` 就放行」）⇒ 实得 True ⇒ 红"
        + "（说明 A0-2/A0-3 不是「随便哪个码都放行」）");
    }

    // ============================================================ A1-380 · setReferenceCard

    static void Case380()
    {
        Debug.Log(_sink.P + "--- A1-380：`chosenAbility 380 setReferenceCard` 真的被分派到 ---");

        var ctx = NewCtx();
        int me = ctx.Active, foe = 1 - me;
        var u = Put(ctx, me, 3, PlainUnit("B102EXEC_380act"));
        var e1 = Put(ctx, foe, 3, PlainUnit("B102EXEC_380e1"));
        var e2 = Put(ctx, foe, 5, PlainUnit("B102EXEC_380e2"));

        C(u.ReferencedCards.Count, 0, "（前提）夹具的引用表一开始是**空**的（`UnitState.ReferencedCards` 新建实例）");

        int rc = RuleCore.RunActiveRowDispatchForTest(ctx, me, u, 0, 380, 30, e1);
        C(rc, 1,
          "★ A1-380 分派返回 1（这一行真跑了）"
        + "｜🧨 删掉 `EffectResolver.RunActiveRowOpsOrGrantedTrigger` 里的 `if (ca == 380)` ⇒ 落回 op 那一支"
        + "（夹具**没有正文 op** ⇒ `RunActiveRowOps` 返回 0）⇒ 实得 0 ⇒ 红");
        C(u.ReferencedCards.Count, 1,
          "★★ A1-380 引用表 = **1 张**（原版 `:2139-2153`：先换一张新 List、再把这条 logic 的目标逐个 `Add`）"
        + "｜🧨 分派没接上 ⇒ 实得 0 ⇒ 红");
        Tr(u.ReferencedCards.Count == 1 && ReferenceEquals(u.ReferencedCards[0], e1),
           "★★ A1-380 表里就是那个目标（**按引用**认，不按卡名）"
         + "｜🧨 写进别的单位（比如施放者自己）⇒ `ReferenceEquals` 假 ⇒ 红");

        int rc2 = RuleCore.RunActiveRowDispatchForTest(ctx, me, u, 0, 380, 30, e2);
        C(rc2, 1, "A1-380b 再跑一次也返回 1");
        C(u.ReferencedCards.Count, 1,
          "★★ A1-380b 引用表仍 = **1 张**（**覆盖**不是追加：原版 `:2139-2143` 先换一张新 List）"
        + "｜🧨 写成 `AddRange` 追加 ⇒ 实得 2 ⇒ 红");
        Tr(u.ReferencedCards.Count == 1 && ReferenceEquals(u.ReferencedCards[0], e2),
           "A1-380b 留下的是**这一次**那条 logic 的目标");

        // ---- 空表 ⇒ 不动表（原版 `:2131-2132` 的 `if (0 < count)`）----
        var ectx = NewCtx();
        int me2 = ectx.Active;
        var u2 = Put(ectx, me2, 3, PlainUnit("B102EXEC_380e"));
        var keep = Put(ectx, me2, 6, PlainUnit("B102EXEC_380keep"));
        u2.ReferencedCards.Add(keep);
        C(u2.ReferencedCards.Count, 1, "（前提）先把这张的引用表写成 1 张（下面要验「空表不改它」）");

        var noneSpec = new EffectTargetSpec
        {
            Raw = "(B102EXEC 夹具：敌方部队 —— 但对面一个部队都没有)", Side = "enemy", Kind = "troop", Count = 0,
        };
        int n0 = RuleCore.SetReferenceCards(ectx, me2, u2, noneSpec, u2, null);
        C(n0, 0, "★ A1-380c 目标表为空 ⇒ `SetReferenceCards` 返回 0");
        C(u2.ReferencedCards.Count, 1,
          "★★ A1-380c 目标表**为空时不换表**（原版 `:2131-2132`：`if (0 < count)` 才新建 List 并赋值）"
        + "｜🧨 删掉 `SetReferenceCards` 里那句 `if (got.Count == 0) return 0;` ⇒ 无条件 `Clear()` ⇒ 实得 0 ⇒ 红");
        Tr(u2.ReferencedCards.Count == 1 && ReferenceEquals(u2.ReferencedCards[0], keep),
           "A1-380c 留下的仍是**原来**那一张");

        // ---- 解不出目标集 ⇒ 不猜 ----
        int rc3 = RuleCore.RunActiveRowDispatchForTest(ctx, me, u, 0, 380, 10, e1);
        C(rc3, 0,
          "★ A1-380d `ta=10` 且这一行没有正文 op ⇒ **解不出目标集 ⇒ 不猜**、返回 0"
        + "（判据 `AbilityLogic__GetTargets.c:192-207`：整张函数只有 `ta = 30/220` 读调用方给的那个目标）"
        + "｜🧨 退回「己方全体」那种近似 ⇒ 返回 1 ⇒ 红（会静默写错引用表，后续 `ta=200/201` 全打错人）");
        C(u.ReferencedCards.Count, 1, "A1-380d 这一次也没动引用表（仍是上一次那 1 张）");
    }

    // ============================================================ A1-480 · highlightBloodThirst

    static void Case480()
    {
        Debug.Log(_sink.P + "--- A1-480：`chosenAbility 480 highlightBloodThirst` 真的被分派到 ---");

        var ctx = NewCtx();
        int me = ctx.Active;
        var u = Put(ctx, me, 3, PlainUnit("B102EXEC_480act"));
        var t = Put(ctx, me, 6, PlainUnit("B102EXEC_480tgt"));
        var plain = Put(ctx, me, 5, PlainUnit("B102EXEC_480plain"));

        // 原版四道闸里我们能造的那两道（`CardScript__ActivateBloodThirst.c:11-22`）：
        //   ① 有 bloodthirst ② 本回合计数 == 1；③ 属于行动侧（`me == ctx.Active` 天然成立）④ 不在将死队列。
        t.AddKeyword(KeywordTable.BloodThirst, 1);
        t.AttacksThisTurn = 1;
        C(t.Has(KeywordTable.BloodThirst), true, "（前提）目标身上有 `bloodthirst`");
        C(t.AttacksThisTurn, 1, "（前提）目标是本回合第 1 次行动（原版 `+0x48 == 1`）");

        int before = ctx.Signals.Count;
        int rc = RuleCore.RunActiveRowDispatchForTest(ctx, me, u, 0, 480, 30, t);
        int fired = CountTriggerSignals(ctx, KeywordTable.BloodThirst, before);

        C(rc, 1,
          "★ A1-480 分派返回 1（这一行真跑了：`EmitBloodThirst` 真的发出了那条高亮）"
        + "｜🧨 删掉 `if (ca == 480)` ⇒ 落回 op 那一支（无正文 op）⇒ 实得 0 ⇒ 红");
        C(fired, 1,
          "★★ A1-480 `ctx.Signals` 上多出**恰好 1 条** `EvtKind.Trigger`（`Keyword = bloodthirst`）"
        + "—— 表现层 `BattleDriver.PlayTraitPulse` 靠它演 `DisplayTriggerAnim(220)`"
        + "（= 原版 `SendHighlightBloodThirstAction` → `HighlightBloodThirst` 的可观测半边）"
        + "｜🧨 分派没接上 ⇒ 实得 0 ⇒ 红");

        // 落点必须是**目标**那一格（原版 `:1968` 取的是目标表第 0 个，不是施放者/不是第一个队友）
        bool landedOnTarget = false;
        var sig = ctx.Signals;
        for (int i = before; i < sig.Count; i++)
        {
            var e = sig[i];
            if (e == null || e.Kind != EvtKind.Trigger) continue;
            if (!string.Equals(e.Keyword, KeywordTable.BloodThirst, System.StringComparison.Ordinal)) continue;
            landedOnTarget = e.Player == me && e.Slot == 6;
            break;
        }
        Tr(landedOnTarget,
           "★★ A1-480c 那条高亮的落点 = **目标那一格**（P" + me + " 的 6 号格），不是施放者（3 号格）"
         + "｜🧨 把参数传成施放者（`u` / `p`+施放者格号）⇒ 格号对不上 ⇒ 红");

        // ---- 闸：目标没有 bloodthirst ⇒ **什么都不发**（原版四道闸在结算那一跳，
        //      我们提前到 `EmitBloodThirst`；写成裸 `ctx.Emit` 就会在这里多发一条）----
        int before2 = ctx.Signals.Count;
        int rc2 = RuleCore.RunActiveRowDispatchForTest(ctx, me, u, 0, 480, 30, plain);
        int fired2 = CountTriggerSignals(ctx, KeywordTable.BloodThirst, before2);
        C(fired2, 0,
          "★ A1-480b 目标**没有** `bloodthirst` ⇒ 一条信号都不发"
        + "（原版 `CardScript__ActivateBloodThirst.c:8-21` 四道闸；我们走 `EmitBloodThirst` 把闸提前）"
        + "｜🧨 把 480 写成裸 `ctx.Emit(EvtKind.Trigger, …)`（绕开 `EmitBloodThirst`）⇒ 实得 1 ⇒ 红");
        C(rc2, 0, "A1-480b …… 分派返回 0（这一行什么都没做）");
    }
}
