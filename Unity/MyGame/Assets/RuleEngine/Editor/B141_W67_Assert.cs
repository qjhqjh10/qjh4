// B141_W67_Assert.cs — 批次 **B141** · 执行写手 `B141-W67`
//   文件所有权 = `RuleEngine/Core/CardAbilityDispatch_B116.cs`（**只在类尾追加**）·
//                `RuleEngine/Core/EffectResolver.cs`（**只做同一物理行改写 / 零行数变化**）· 本文件（新建）
//
// ============================ 它盯的是什么 ============================
//  活状态表 `B-211` 一族（`B141-CA-ROW-DISPATCH`）：**`cardAbilities` 行 + `Hand` 上下文**那条路
//  （`EffectResolver.FireHandAbilityTrigger`）改前**只取文本 op**、**不按 `chosenAbility` 数字分派**
//  ⇒ 数据里真出现「手牌里靠 `ca 160` 弃自己」这种行（教程起手占位卡
//  `T1 BlackLegion-Abaddons Chosen_startinghand`）时，我们**什么都不做、也不出声**。
//  本笔让那条路**也**按 `ca` 走一张分派表（与主路同一批判据 / 同一个结算核心，⛔ 不是第二份规则）。
//
// ============================ 原版判据（现读，⛔ 别在这儿抄第二份）============================
//  · `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c:588-603` —— `ca 160 (discardFromHand)`：
//    遍历 `possibleTargets`，逐个 `AddDiscardFromHand(target, ability+0x44, abilityData+0x48, 0)`。
//  · `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c:184-192` —— `ta = 20 (self)` ⇒ 目标 = `thisCard`
//    （= 能力所在那张卡自己），**只有 `ta ∈ {30,220}` 才读调用方点定的那个目标**。
//  · `d:/2/tools/decomp_full/RawCardScript__TriggerAbilities.c` —— 选 ability 只看 `ability.trigger`，
//    与 `chosenAbility` 无关；`chosenAbility` 决定的是**这一行执行什么**（`PlayAbility` 的巨型 switch）。
//  · 数据侧（唯一持有 `160` 的那张 SO）=> `资料/完全复刻/报告/B141_W128_BL37Discard.md` §一·4。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · **A1 / B1..B7**：把 `FireHandAbilityTrigger` 里 `rowLane` / `DispatchHandAbilityRows` 那两行接线删掉
//    ⇒ 触发口返回 false、手牌一张不少 ⇒ B1 + B4 红。
//  · **A2 / C0..C3**：把 `HandAbilityRowKindOf` 的 `ta` 那一格忽略（一律当 `ta=20 self`）
//    ⇒ `ta=30` 的负例也会被派出去（手牌少 1 张）⇒ C1/C3 红。
//  · **A3**：把 `ca < 0` 折成 `0`（`0 = drawCard` 是**合法真值**）⇒ A3 红。
//  · **E1 / E3**：把 B7 双保险（`CardAbilityRowCoveredByTextLayer`）删掉 ⇒ E1 实得 true ⇒ 红；
//    把它写成恒 `false` ⇒ E3 实得 false ⇒ 红。**两侧不可能同时绿**。
//  · **B5/B7**：结算体改成 `DoDiscardFromHand`（按 `playersAffected` 在一侧手牌里**随机挑一张**）
//    ⇒ 手牌里另放两张填充牌后，「少的那张必须就是它自己」会随机红。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加（落在该行第一个 `//` **之前**）：
//     `Guard("B141W67_Assert", B141_W67_Assert.Run); _pass += B141_W67_Assert.Pass; _fail += B141_W67_Assert.Fail;`
//   （接线由调度台统一做；本写手白名单不含那个文件。）
//
// ⚠️ **本文件不跑 Unity / 不跑宿主**（写手纪律）：下面只保证「编得过 + 判据自洽」；
//    真的跑绿要等调度台在同步点跑 `RuleEngineTest.Run`。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行写手 `B141_W67`：手牌上下文（`abilityContext = 10 Hand`）的行级 `ca` 派发。</summary>
public static class B141_W67_Assert
{
    const string P = "[B141W67] ";

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get; private set; }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    static bool Eq(int got, int want, string msg)
    {
        if (got == want) { Pass++; Debug.Log(P + $"   ✓ {msg}（实际 {got}）"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}｜实际 {got}，期望 {want}"); return false;
    }

    // ============================================================ 夹具

    static CardDef Hero(string id) => new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0);

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B141W67Warlord0") };
        var d1 = new List<CardDef> { Hero("B141W67Warlord1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    /// <summary>填手牌用的无害单位卡（只为「少的那张必须就是它自己」那两条断言造出**其它候选**）。</summary>
    static CardDef Filler(string id) => new CardDef(id, id, "unit", "", null, "Test", 1, 1, 3, 0);

    /// <summary>
    /// 造一张「教程起手占位卡」形状的合成卡（判据 → `报告/B141_W128_BL37Discard.md` §一·4）：
    /// `trigger 30 (TurnStart)` · `abilityContext 10 (Hand)` · 一行 `(ab0, lg0)` `ca / ta` 可调。
    /// <para>⚠️ `abilityTriggersVersioned: true` **必须显式给** —— 构造函数里
    /// `AbilityTargetsVersioned = abilityTriggersVersioned`（`CardDef.cs:1120`）⇒ 不给的话
    /// `AbilityTargets` 会被消费端当「旧表整列没落」而**静默跳过**（夹具看着有数据、实际读不到）。</para>
    /// </summary>
    static CardDef RowCard(string id, int trigger, int ca, int ta, bool withRows = true)
    {
        var c = new CardDef(id, id, "tactic", "", "common", "Test", 1, 0, 0, 0,
            abilityContext: new[]
            {
                new CardDef.AbilityContextSpec { Name = "TurnStart", Context = CardDef.AbilityContextHand },
            },
            abilityContextsVersioned: true,
            abilityTriggers: new[] { trigger },
            abilityTriggersVersioned: true,
            abilityTargets: new[] { new CardDef.AbilityTargetSpec { Trigger = trigger, TargetsAffected = new[] { ta } } });
        if (withRows)
            c.AttachTriggerAbilityIndices(new[]
            {
                new CardDef.TriggerAbilityIndexSpec { Ab = 0, Lg = 0, Trigger = trigger, Ca = ca },
            });
        return c;
    }

    /// <summary>把卡写进这一席的手牌（`PlayerState.Hand` 是 `List&lt;CardInstance&gt;`）。</summary>
    static CardInstance Hold(BattleContext ctx, int seat, CardDef card)
    {
        var inst = ctx.NewInstance(card);
        ctx.Players[seat].Hand.Add(inst);
        return inst;
    }

    /// <summary>这张卡某条 trigger 上 `(ab, lg)` 那一行（读不到 ⇒ null）。</summary>
    static CardDef.TriggerAbilityIndexSpec RowOf(CardDef card, int trigger, int ab, int lg)
    {
        var rows = card != null ? card.TriggerAbilityRowsOf(trigger) : null;
        if (rows == null) return null;
        for (int i = 0; i < rows.Length; i++)
            if (rows[i] != null && rows[i].Ab == ab && rows[i].Lg == lg) return rows[i];
        return null;
    }

    // ============================================================ 入口
    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            SectionA_Classifier();
            SectionB_HandCa160EndToEnd();
            SectionC_TaMustBeSelf();
            SectionD_ThreeStateAndTriggerBridge();
            SectionE_TextLayerWins();
            SectionF_PoolReachability();
        }
        catch (System.Exception e)
        {
            Ok(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            Debug.Log(P + $"=== B141W67 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    // ============================================================
    //  A · 行级分类（`HandAbilityRowKindOf`，纯函数）
    // ============================================================
    static void SectionA_Classifier()
    {
        Debug.Log(P + "--- A · `HandAbilityRowKindOf`：`ca 160 ∧ ta = 20 self` 才是本层能跑的那一行 ---");
        string why;

        var self = RowCard("B141W67A_self", 30, 160, 20);
        var rSelf = RowOf(self, 30, 0, 0);
        Ok(rSelf != null && rSelf.Ca == 160, "A0 **前提**：合成卡有 `trigger 30 / ab0 / lg0 / ca 160` 那一行");
        Ok(RuleCore.HandAbilityRowKindOf(self, rSelf, out why) == RuleCore.HandRowKind.Owned,
           "A1 ★ `ca 160` + `ta = 20 self` ⇒ 本层**有**执行器（判据 `AbilityLogic__PlayAbility.c:588-603` + "
         + "`AbilityLogic__GetTargets.c:184-192`）｜🧨 判成 Unsupported ⇒ 整条派发链断 ⇒ 红");

        var ta30 = RowCard("B141W67A_ta30", 30, 160, 30);
        Ok(RuleCore.HandAbilityRowKindOf(ta30, RowOf(ta30, 30, 0, 0), out why) == RuleCore.HandRowKind.Unsupported,
           "A2 ★ `ca 160` 但 `ta = 30 target` ⇒ **Unsupported**（手牌上下文只实现 `ta = 20 self`，⛔ 不猜）"
         + "｜🧨 忽略 `ta` 一律当 self ⇒ A2 + C 段全红");

        var unknown = RowCard("B141W67A_unknown", 30, 160, 20, withRows: false);
        unknown.AttachTriggerAbilityIndices(new[]
        {
            new CardDef.TriggerAbilityIndexSpec { Ab = 0, Lg = 0, Trigger = 30, Ca = -1 },
        });
        Ok(RuleCore.HandAbilityRowKindOf(unknown, RowOf(unknown, 30, 0, 0), out why) == RuleCore.HandRowKind.Unknown,
           "A3 `ca = -1`（未知：旧表 < v47 / `lg == -1` / 手写夹具）⇒ **Unknown** = 改前行为"
         + "｜🧨 把 `-1` 折成 `0`（`0 = drawCard` 是**合法真值**）⇒ 变 Unsupported ⇒ 红");

        var ca100 = RowCard("B141W67A_ca100", 30, 100, 20);
        Ok(RuleCore.HandAbilityRowKindOf(ca100, RowOf(ca100, 30, 0, 0), out why) == RuleCore.HandRowKind.Unsupported,
           "A4 `ca 100 createCardInHand` ⇒ **Unsupported**（主路执行器要 `UnitState`，手牌上下文没有那个对象）"
         + " ⇒ 出声 + 不结算（真缺口，⛔ 不静默）");
    }

    // ============================================================
    //  B · 端到端：占位卡的手牌 `TurnStart` 行 → 真的把它自己弃进弃牌堆
    // ============================================================
    static void SectionB_HandCa160EndToEnd()
    {
        Debug.Log(P + "--- B · 手牌 `ca 160` 端到端（占位卡形状：`trigger 30 / ctx 10 Hand / ta 20 self`）---");
        var card = RowCard("B141W67B_startinghand", 30, 160, 20);
        var ctx = NewCtx();
        RuleCore.PumpActionQueues(ctx);                    // 清掉开局留下的队列动作
        int me = ctx.Active;

        var inst = Hold(ctx, me, card);
        var filler0 = Hold(ctx, me, Filler("B141W67B_filler0"));   // 两张填充牌 ⇒ 随机挑一张的实现会被抓
        var filler1 = Hold(ctx, me, Filler("B141W67B_filler1"));
        int handBefore = ctx.Players[me].Hand.Count;
        int discBefore = ctx.Players[me].Discard.Count;
        Eq(handBefore, 3, "B0 **前提**：手牌里有 3 张（占位卡 + 2 张填充）");

        Ok(RuleCore.HandAbilityTriggerHasRowsForTest(card, "TurnStart"),
           "B0′ ★ **前提**：这张卡的 `TurnStart` 上有**本层能跑**的行（否则后面测的是空转）");

        RuleCore.ResetHandAbilityCountersForTest();
        bool fired = RuleCore.FireHandAbilityTrigger(ctx, me, inst, "TurnStart", by: "自检:B141W67B");
        Ok(fired, "B1 ★★ 手牌触发口**入队**了（这一行的 `ca 160` 被认出可派）"
                + "｜🧨 删掉 `rowLane` / `DispatchHandAbilityRows` 那两行接线 ⇒ 实得 false ⇒ 红");
        Eq(RuleCore.HandAbilityTriggerQueuedForTest, 1, "B2 入队计数 = 1（⛔ 不是两条路各入队一次）");
        Eq(ctx.Players[me].Hand.Count, handBefore,
           "B3 入队那一刻**还没弃**（行级派发在泵点跑 —— 与文本 op 同一个时机，⛔ 不在广播循环里就地改手牌）");

        RuleCore.PumpActionQueues(ctx);
        Eq(ctx.Players[me].Hand.Count, handBefore - 1,
           "B4 ★★ 泵队之后手牌**少 1 张**（原版 `AbilityLogic__PlayAbility.c:588-603` 逐目标 `AddDiscardFromHand`）"
         + "｜🧨 接线删掉 ⇒ 实得 3 ⇒ 红");
        Ok(!ctx.Players[me].Hand.Contains(inst),
           "B5 ★★ 少的就是**它自己**（`ta = 20 self` ⇒ 目标 = 能力所在那张卡）"
         + "｜🧨 改成 `DoDiscardFromHand` 的「受影响侧随机挑一张」⇒ 66% 挑到填充牌 ⇒ 红");
        Ok(ctx.Players[me].Hand.Contains(filler0) && ctx.Players[me].Hand.Contains(filler1),
           "B6 ★ 两张填充牌**一张没动**（⛔ 不是「弃一张手牌」的近似）");
        Eq(ctx.Players[me].Discard.Count, discBefore + 1,
           "B7 ★★ 弃牌堆 **+1**（去向 = 弃牌堆；与 `disappearFromHand(162)` 的「移出游戏」不是一回事）");
        Ok(ctx.Players[me].Discard.Contains(inst), "B8 ★★ 进弃牌堆的就是它自己");
    }

    // ============================================================
    //  C · 负例：`ta` 不是 self ⇒ 一行都不跑（⛔ 不猜目标）
    // ============================================================
    static void SectionC_TaMustBeSelf()
    {
        Debug.Log(P + "--- C · 负例：`ca 160` 但 `ta = 30 target` ⇒ 手牌上下文**不结算**（⛔ 不猜）---");
        var card = RowCard("B141W67C_ta30", 30, 160, 30);
        var ctx = NewCtx();
        RuleCore.PumpActionQueues(ctx);
        int me = ctx.Active;
        var inst = Hold(ctx, me, card);
        int handBefore = ctx.Players[me].Hand.Count;
        int discBefore = ctx.Players[me].Discard.Count;

        Ok(!RuleCore.HandAbilityTriggerHasRowsForTest(card, "TurnStart"),
           "C0 ★★ `ta = 30` ⇒ 本层**没有**可派的行（要调用方点定的那个目标，手牌里没有点目标这回事）");
        Ok(!RuleCore.FireHandAbilityTrigger(ctx, me, inst, "TurnStart", by: "自检:B141W67C"),
           "C1 ★★ 触发口返回 **false**（⛔ 不静默入队一条空转的能力）"
         + "｜🧨 忽略 `ta` 一律当 self ⇒ 实得 true ⇒ 红");
        RuleCore.PumpActionQueues(ctx);
        Eq(ctx.Players[me].Hand.Count, handBefore, "C2 ★ 手牌一张没少");
        Eq(ctx.Players[me].Discard.Count, discBefore, "C3 ★ 弃牌堆没动");
    }

    // ============================================================
    //  D · `TriggerAbilityRowsOf` 三态 + trigger 名 → 数值 那一跳
    // ============================================================
    static void SectionD_ThreeStateAndTriggerBridge()
    {
        Debug.Log(P + "--- D · 三态（列未知 / 明确没有 / 有行）+ trigger 名桥 ---");

        var noCol = RowCard("B141W67D_nocol", 30, 160, 20, withRows: false);
        Ok(!RuleCore.HandAbilityTriggerHasRowsForTest(noCol, "TurnStart"),
           "D1 `triggerAbilityIndices` 列**未知**（`null`，旧表 < v42 / 手写夹具）⇒ 不进本层（= 改前行为）");

        var empty = RowCard("B141W67D_empty", 30, 160, 20, withRows: false);
        empty.AttachTriggerAbilityIndices(new CardDef.TriggerAbilityIndexSpec[0]);
        Ok(!RuleCore.HandAbilityTriggerHasRowsForTest(empty, "TurnStart"),
           "D2 列在、但**明确没有**这条 trigger（`[]`）⇒ 同样不进（⛔ 别把 `null` 与 `[]` 合并成一个判据）");

        var other = RowCard("B141W67D_other", 30, 160, 20);
        Ok(!RuleCore.HandAbilityTriggerHasRowsForTest(other, "TurnStarted"),
           "D3 trigger **对不上**（卡上只有 30，问的是 160 `TurnStarted`）⇒ 没有行"
         + "（闸① `AbilityTriggerPresence` 会先挡；这一条钉的是「名字 → 数值」那一跳没串）");

        Eq(CardDef.AbilityTriggerNumber("TurnStart"), 30, "D4 桥：`TurnStart` = 30（手牌广播用的就是这个名字）");
        Eq(CardDef.AbilityTriggerNumber("TurnStarted"), 160, "D5 桥：`TurnStarted` = 160");
        Eq(CardDef.AbilityTriggerNumber("UnitPoisoned"), 290, "D6 桥：`UnitPoisoned` = 290");
        Ok(CardDef.AbilityTriggerNumber("这不是trigger名") == -1, "D7 认不出 ⇒ `-1`（⛔ 不猜 `0 ThisCardPlayed`）");
    }

    // ============================================================
    //  E · 双发闸：正文 op 已覆盖该 ability ⇒ 本层让开（B7 同一条判据）
    // ============================================================
    static void SectionE_TextLayerWins()
    {
        Debug.Log(P + "--- E · B7 双保险：`TriggerOpsByNumber(30)` 已覆盖 `ability#0` ⇒ 本层不跑 ---");
        var card = RowCard("B141W67E_cover", 30, 160, 20);
        var map = new Dictionary<int, List<EffectOp>>();
        map[30] = new List<EffectOp>
        {
            new EffectOp { Verb = "discardfromhand", Source = "B141W67 E 夹具", AbilityIndex = 0 },
        };
        card.AttachTriggerOpsByNumber(map);

        Ok(!RuleCore.HandAbilityTriggerHasRowsForTest(card, "TurnStart"),
           "E1 ★★ 正文 op 已覆盖 `ability#0` ⇒ **本层让开**（B7 双保险，⛔ 不双发）"
         + "｜🧨 删掉 `CardAbilityRowCoveredByTextLayer` 那一句 ⇒ 实得 true ⇒ 红");

        var ctx = NewCtx();
        RuleCore.PumpActionQueues(ctx);
        int me = ctx.Active;
        var inst = Hold(ctx, me, card);
        int handBefore = ctx.Players[me].Hand.Count;
        Eq(RuleCore.DispatchHandAbilityRowsForTest(ctx, me, inst, "TurnStart", by: "自检:B141W67E"), 0,
           "E2 ★ 派发口也回 **0**（同一张卡、只把正文桶挂上）");
        Eq(ctx.Players[me].Hand.Count, handBefore, "E2′ ★ 手牌一张没少（⛔ 「正文 + 数据行」两边都跑）");

        card.AttachTriggerOpsByNumber(null);              // 摘掉正文桶
        Ok(RuleCore.HandAbilityTriggerHasRowsForTest(card, "TurnStart"),
           "E3 ★★ 摘掉正文桶 ⇒ 又变 `true`（**同一夹具两个方向**，⛔ 不是恒 false 的实现）");
        Eq(RuleCore.DispatchHandAbilityRowsForTest(ctx, me, inst, "TurnStart", by: "自检:B141W67E′"), 1,
           "E4 ★★ 摘桶后派发口跑 **1 行**（`ta = 20 self` ⇒ 弃它自己）");
        Eq(ctx.Players[me].Hand.Count, handBefore - 1, "E5 ★ 这次真弃了：手牌 -1");
    }

    // ============================================================
    //  F · 现池可达性（如实标：**本笔零可达**）
    // ============================================================
    static void SectionF_PoolReachability()
    {
        Debug.Log(P + "--- F · 现池可达性：`ca 160` = 0 行（唯一持有者是教程起手占位卡，不在可玩池）---");
        var pool = CardDatabase.Load();
        if (!Ok(pool != null && pool.Count > 0, "F0 **前提**：卡池读到（`CardDatabase.Load`）")) return;

        int ca160 = 0, rowsTotal = 0, handCtxCards = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null) continue;
            var acs = c.AbilityContexts;
            if (acs != null)
                for (int k = 0; k < acs.Count; k++)
                    if (acs[k] != null && acs[k].Context == CardDef.AbilityContextHand) { handCtxCards++; break; }
            var rows = c.TriggerAbilityIndices;
            if (rows == null) continue;
            for (int k = 0; k < rows.Length; k++)
            {
                if (rows[k] == null) continue;
                rowsTotal++;
                if (rows[k].Ca == 160) ca160++;
            }
        }
        Debug.Log(P + $"   现读：池 {pool.Count} 张 · `triggerAbilityIndices` 行 {rowsTotal} · "
                     + $"`abilityContext = 10 Hand` 的卡 {handCtxCards} 张");
        Eq(ca160, 0,
           "F1 ★ 现池 `ca == 160` = **0 行** ⇒ 本笔**零可达**（唯一持有者 `f3df0cfc…` = 教程起手占位卡，"
         + "不在 `cards_engine` 里；判据 → `报告/B141_W128_BL37Discard.md` §一·4 / §二·3）");
        Ok(handCtxCards > 0,
           "F2 ★ 现池确有 `abilityContext == 10 Hand` 的卡（现读 13 张）—— 它们的行 `ca` 都不是 160 "
         + "⇒ 本笔**不改任何现有卡的行为**（⛔ 别拿「`play check` 绿」当「新路径被执行过」）");
    }
}
