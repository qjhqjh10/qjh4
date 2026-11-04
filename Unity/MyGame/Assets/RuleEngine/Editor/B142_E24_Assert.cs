// B142_E24_Assert.cs — 执行写手 `B142_E24`（第一百二十九会话 · 2026-11-04）
//
// ============================ 它盯的是什么 ============================
//  四件活，每件一组：
//   A · `B141-SW34-480`：`ca 480 (highlightBloodThirst)` 的**关键词触发路**行执行器
//       判据 = `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c:1960-1972`
//              （`default:` 支 `if (iVar8 != 0x1e0) return;` → 取目标表 → 非空才取**第 0 个** →
//               `CardScript.SendHighlightBloodThirstAction(target, 0)`）；
//              目标集 `ta = 20 self` ⇒ `[thisCard]`（`AbilityLogic__GetTargets.c:184-191`）。
//       我们的落点 = `RuleCore.RunHighlightBloodThirstRowsOfKeywordTrigger`（`EffectResolver.cs` 类尾）
//              ＋ `RuleCore.FireTriggerDeferred` 尾部那一跳（`RuleCore.cs`）+ `RunHighlightBloodThirstRow` 的 `ta==20` 支。
//   B · `B141-OPT0-MULT`：`FoldOneBuffStat` 的 **`opt = 0 (Normal)` 也乘 multiplier**，
//       且「v15+ 表下没有 `role=mult` 行」= **mult 1**（不是「折不出来」）。
//       判据 = `AbilityLogic__NewBuffStat.c`（`if (param_3 == 0) { param_4 = param_4 * param_6; }`）＋
//              `AbilityLogic__PlayAbility.c:180-236`（`multiplier` 为空 / `Any` ⇒ 1）。
//       期望 = `GOF43` ab0 armour **3** · `TAU57` ab1 cost **−3** · `TAU57` ab0 cost **−1**（对照组）。
//   C · `B141-UNRESOLVED-DEAD`：`EnchantmentExecutor_B115.cs` 的 `RunStunRowOfEnchantment` 里
//       那个**从不 `.Add`** 的 `unresolved` 变量与 `if (Count > 0) …` 死分支已删（同文件的活口不许动）。
//   D · `B141-ABILITY-HAND-COVERAGE` P0：`TurnEnd(40)` / `TurnBeforeEnd(41)` 的手牌两跳。
//       判据 = `BattleManagerSupport__BroadcastTurnEnd.c:43-51` / `__BroadcastTurnBeforeEnd.c:43-51`
//              （双方手牌各一趟 `CardScript.OnTurnEnd` / `OnTurnBeforeEnd`）。
//       落点 = `RuleCore.BroadcastTurnEndHandHops`，两个调用点（`EffectResolver.cs:9031` 早退 / `:9112` 尾部）。
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · A2：把 `FireTriggerDeferred` 尾部那一跳删掉 ⇒ A 组行为面仍然绿（它测的是执行器本体），
//        但 **A6（结构）红**；把 `RunHighlightBloodThirstRow` 的 `ta == 20` 支删掉 ⇒ A1/A2 红。
//  · B1/B2：把 `FoldOneBuffStat` 的 `opt != 80 && opt != 0` 改回 `opt != 80` ⇒ B1/B2 红。
//  · B2/B3：把 `ConditionsVersioned` 那一跳删掉（回到「没有 mult 行 ⇒ 折不出来」）⇒ B1/B2 红、B3 仍绿。
//  · C1：把 `unresolved` 加回去（哪怕不 `.Add`）⇒ C1 红。
//  · D1/D2：删掉 `BroadcastTurnEndHandHops` 里任意一趟（41 / 40）⇒ D1/D2 红；D3 断两个调用点都在。
//
// ============================ 怎么接（宿主聚合由调度台做）============================
//  `public static void Run();` —— 前提：`CardDatabase.Load()` 能读到卡表（B 组用它取真卡；
//  其余组只建内存对局 + 读源码文本，不建窗口、不碰存档）。
//  挂法：`Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加
//      `B142_E24_Assert.Run(); _pass += B142_E24_Assert.Pass; _fail += B142_E24_Assert.Fail;`
//  （⚠️ 追加文本必须落在该行第一个 `//` **之前**；本件宿主接线由调度台统一做。）
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B142_E24` 的回归断言（SW34-480 · OPT0-MULT · UNRESOLVED-DEAD · TurnEnd/41 手牌两跳）。</summary>
public static class B142_E24_Assert
{
    const string Tag = "B142E24";
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_Slay480();
        GroupB_Opt0Multiplier();
        GroupC_UnresolvedDead();
        GroupD_TurnEndHandHops();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A · SW34-480：`ca 480 highlightBloodThirst` 在关键词触发路上真的会亮
    // ==================================================================
    static void GroupA_Slay480()
    {
        Debug.Log("--- A · `ca 480 highlightBloodThirst`（判据 `AbilityLogic__PlayAbility.c:1960-1972`）---");

        var ctx = MakeBattle(out var u);
        if (ctx == null || u == null) { Report(false, "A0 前提不成立：对局 / 载体没建起来"); return; }

        // A1 正例：带 bloodthirst · 本回合第 1 次行动 · 属于行动侧 ⇒ 亮一下（一条 `Keyword = bloodthirst` 的 Trigger 事件）
        u.AddKeyword(KeywordTable.BloodThirst, 1);
        u.AttacksThisTurn = 1;
        int before = ctx.Signals.Count;
        int n = RuleCore.RunHighlightBloodThirstRowsOfKeywordTrigger(ctx, u, KeywordTable.Slay, 0, 3, null);
        Check(n, 1,
              "A1 ★★★ **结论（SW34-480）**：`RunHighlightBloodThirstRowsOfKeywordTrigger` 对 `ca 480` 行返回 1"
            + "｜🧨 删掉 `RunHighlightBloodThirstRow` 的 `ta == 20` 支 ⇒ 目标集判不出 ⇒ 返回 0 ⇒ 红");
        CheckRaw(HasTrigger(ctx, before, KeywordTable.BloodThirst),
                 "A2 ★★★ **结论（SW34-480）**：真发了一条 `Keyword = bloodthirst` 的 `EvtKind.Trigger`"
               + "（原版 `PlayAbility.c:1970` → `SendHighlightBloodThirstAction` → `HighlightBloodThirst`）");

        // A3 负例：不是本回合第 1 次行动 ⇒ `EmitBloodThirst` 的四道闸不过
        var ctx3 = MakeBattle(out var u3);
        if (ctx3 != null && u3 != null)
        {
            u3.AddKeyword(KeywordTable.BloodThirst, 1);
            u3.AttacksThisTurn = 2;
            int b3 = ctx3.Signals.Count;
            Check(RuleCore.RunHighlightBloodThirstRowsOfKeywordTrigger(ctx3, u3, KeywordTable.Slay, 0, 3, null), 0,
                  "A3 ★★ 负例：`AttacksThisTurn = 2` ⇒ 不发（原版 `CardScript__ActivateBloodThirst.c:9` 的 `+0x48 == 1`）");
            CheckRaw(!HasTrigger(ctx3, b3, KeywordTable.BloodThirst), "A3b ★★ 负例：信号表里没有多出 bloodthirst");
        }
        else Report(false, "A3 前提不成立：第二场没建起来");

        // A4 负例：没有 bloodthirst 关键词 ⇒ 不发
        var ctx4 = MakeBattle(out var u4);
        if (ctx4 != null && u4 != null)
        {
            u4.AttacksThisTurn = 1;
            int b4 = ctx4.Signals.Count;
            Check(RuleCore.RunHighlightBloodThirstRowsOfKeywordTrigger(ctx4, u4, KeywordTable.Slay, 0, 3, null), 0,
                  "A4 ★★ 负例：单位没有 `bloodthirst` ⇒ 不发（原版四道闸第 ① 条）");
            CheckRaw(!HasTrigger(ctx4, b4, KeywordTable.BloodThirst), "A4b ★★ 负例：信号表里没有多出 bloodthirst");
        }
        else Report(false, "A4 前提不成立：第三场没建起来");

        // A5 负例：关键词对不上（`Strike` 的 trigger 号与夹具的行不一致）⇒ 一行都不跑
        Check(RuleCore.RunHighlightBloodThirstRowsOfKeywordTrigger(ctx, u, KeywordTable.Strike, 0, 3, null), 0,
              "A5 ★★ 负例：`Strike(580)` 在夹具里没有行 ⇒ 返回 0（⛔ 不按 ca 反扫全卡）");

        // A6 结构：`FireTriggerDeferred` 尾部那一跳真的接上了（⛔ 只加执行器不接线 = 死代码）
        string core = ReadSource("RuleEngine/Core/RuleCore.cs");
        string deferred = Cut(core, "static void FireTriggerDeferred(", "static void FireTriggerAlways(");
        CheckRaw(deferred.IndexOf("RunHighlightBloodThirstRowsOfKeywordTrigger(ctx, u, keyword, owner, slot, chosen ?? subject);", StringComparison.Ordinal) >= 0,
                 "A6 ★★★ **结论（SW34-480）**：`RuleCore.FireTriggerDeferred` 尾部有那一跳（关键词触发路真的会跑到 480）"
               + "｜🧨 删掉 ⇒ 红（`SW34` 的 Slay 再也亮不起来）");
        Check(RuleCore.TriggerRowKindOf(480), RuleCore.TriggerRowKind.OwnLane,
              "A7 ★★ 反证：`TriggerRowKindOf(480)` 仍是 `OwnLane`（`B141_W36` D7 的既有口径；480 现在由**行执行器**跑，不是改归类）");
    }

    // ==================================================================
    //  B · OPT0-MULT：`opt 0 (Normal)` 也乘 multiplier，且「没有 mult 行」= mult 1
    // ==================================================================
    static void GroupB_Opt0Multiplier()
    {
        Debug.Log("--- B · `opt 0` 折叠（判据 `AbilityLogic__NewBuffStat.c` + `PlayAbility.c:180-236`）---");

        List<CardDef> pool;
        try { pool = CardDatabase.Load(); }
        catch (Exception e) { Report(false, "B0 前提不成立：`CardDatabase.Load()` 抛了 " + e.GetType().Name); return; }
        if (pool == null || pool.Count == 0) { Report(false, "B0 前提不成立：卡池读不到"); return; }

        var gof = Find(pool, "GOF43");
        var tau = Find(pool, "TAU57");
        CheckRaw(gof != null && tau != null, "B0 前提：`GOF43 Gretchin Mob` / `TAU57 Burgeoning Empire` 都在卡池里");
        if (gof == null || tau == null) return;

        var g = RuleCore.FoldBuffStatValueOptionsForTest(null, 0, null, gof, 0, 0);
        CheckRaw(g != null, "B0b 前提：`GOF43` 的 `(ab 0, lg 0)` 有 `buffEffect` 行");
        if (g != null)
            Check(g[3], 3,
                  "B1 ★★★ **结论（OPT0-MULT ①）**：`GOF43` ab0 的 **armour = 3**（raw 1 × mult 1 × fm 3）"
                + "｜🧨 `FoldOneBuffStat` 改回 `opt != 80` 早退 ⇒ 得 1 ⇒ 红");

        var t1 = RuleCore.FoldBuffStatValueOptionsForTest(null, 0, null, tau, 1, 0);
        CheckRaw(t1 != null, "B0c 前提：`TAU57` 的 `(ab 1, lg 0)` 有 `buffEffect` 行");
        if (t1 != null)
            Check(t1[4], -3,
                  "B2 ★★★ **结论（OPT0-MULT ②）**：`TAU57` **ab1** 的 **cost = −3**（raw −1 × mult 1 × fm 3）"
                + "｜🧨 同上 ⇒ 得 −1 ⇒ 红");

        var t0 = RuleCore.FoldBuffStatValueOptionsForTest(null, 0, null, tau, 0, 0);
        CheckRaw(t0 != null, "B0d 前提：`TAU57` 的 `(ab 0, lg 0)` 有 `buffEffect` 行");
        if (t0 != null)
            Check(t0[4], -1,
                  "B3 ★★ ★对照**：`TAU57` **ab0** 的 cost 仍是 **−1**（`fm = 0` ⇒ 不乘）"
                + "｜🧨 把 `fm` 的 `> 0` 闸去掉（0 也乘）⇒ 得 0 ⇒ 红");
    }

    // ==================================================================
    //  C · UNRESOLVED-DEAD：死变量 / 死分支已删，且同文件活口没被误删
    // ==================================================================
    static void GroupC_UnresolvedDead()
    {
        Debug.Log("--- C · `RunStunRowOfEnchantment` 的死变量 / 死分支 ---");
        string src = ReadSource("RuleEngine/Core/EnchantmentExecutor_B115.cs");
        string code = StripComments(src);
        string body = Cut(code, "int RunStunRowOfEnchantment(", "int RunStealMinionSelfRowOfEnchantment(");
        CheckRaw(body.Length > 0, "C0 前提：切得到 `RunStunRowOfEnchantment` 的方法体");
        if (body.Length == 0) return;
        CheckRaw(body.IndexOf("unresolved", StringComparison.Ordinal) < 0,
                 "C1 ★★★ **结论（UNRESOLVED-DEAD）**：方法体（剥注释后）**没有再出现 `unresolved`**"
               + "｜🧨 把 `var unresolved = new List<string>(1);` 加回去 ⇒ 红");
        CheckRaw(code.IndexOf("ReportUnresolved(ctx, by, unresolved);", StringComparison.Ordinal) >= 0,
                 "C2 ★★ 反证：同文件**活的**那几处 `ReportUnresolved(ctx, by, unresolved);` 还在（⛔ 没被一并删掉）");
    }

    // ==================================================================
    //  D · TurnEnd(40) / TurnBeforeEnd(41) 手牌两跳
    // ==================================================================
    static void GroupD_TurnEndHandHops()
    {
        Debug.Log("--- D · `BroadcastTurnEndHandHops`（40 / 41 双方手牌）---");

        var ctx = MakeBattle(out var dummy0);
        if (ctx == null) { Report(false, "D0 前提不成立：对局没建起来"); return; }

        // 一张「手牌上下文」的单位卡：40 / 41 各一条 `ca 160 + ta 20 self` 行（手牌上下文**唯一**有执行器的那档）
        var hand = HandCard("B142E24Hand", new[] { 40, 41 });
        ctx.Players[0].Hand.Add(ctx.NewInstance(hand));

        RuleCore.ResetHandAbilityCountersForTest();
        int n = RuleCore.BroadcastTurnEndHandHops(ctx);

        Check(n, 2,
              "D1 ★★★ **结论（HAND-COVERAGE P0）**：同一张手牌 `TurnBeforeEnd(41)` + `TurnEnd(40)` **各入队一次** ⇒ 返回 2"
            + "｜🧨 删掉 `BroadcastTurnEndHandHops` 里任意一趟 ⇒ 得 1 ⇒ 红");
        Check(RuleCore.HandAbilityTriggerQueuedForTest, 2,
              "D2 ★★ 计数口同口径：`HandAbilityTriggerQueuedForTest = 2`");

        // D3 负例：手牌上下文的卡（没有 `abilityContext = Hand`）→ 扫到但被 `IsRightContext` 拒
        var plain = HandCard("B142E24Plain", new[] { 40, 41 }, handContext: false);
        ctx.Players[1].Hand.Add(ctx.NewInstance(plain));
        RuleCore.ResetHandAbilityCountersForTest();
        Check(RuleCore.BroadcastTurnEndHandHops(ctx), 2,
              "D3 ★★ 负例：`Play(0)` 上下文的那张**不入队**（41/40 各扫到 2 张手牌，只有带 Hand 上下文的那 1 张算）"
            + "｜🧨 把 `FireHandAbilityTrigger` 的区域闸绕掉 ⇒ 得 4 ⇒ 红");
        Check(RuleCore.HandAbilityTriggerAttemptsForTest, 4,
              "D3b ★★ 计数口：双方各 1 张手牌 × 2 个 trigger = 4 次尝试（含被区域闸拒的那 2 次），入队仍 2 次");

        // D4 结构：两个调用点都在（`total == 0` 早退那条正是池里没有文本陷阱时唯一会走的路）
        string src = ReadSource("RuleEngine/Core/EffectResolver.cs");
        int hits = CountOf(src, "if (phase == \"turn_end\") BroadcastTurnEndHandHops(ctx);");
        Check(hits, 2,
              "D4 ★★★ **结论（HAND-COVERAGE P0）**：`ResolveAtTurn` 的**两个**调用点都接上了（`:9031` 早退 + `:9112` 尾部）"
            + "｜🧨 只接一处 ⇒ 池里没有文本陷阱时那两跳一次都不跑 ⇒ 红");

        // D5 结构：先 41 再 40（原版是两条广播，`ResolveEndTurn.c:385` → `:433`）
        string fn = Cut(src, "public static int BroadcastTurnEndHandHops(", "public static int RunHighlightBloodThirstRowsOfKeywordTrigger(");
        int i41 = fn.IndexOf("TurnBeforeEnd", StringComparison.Ordinal);
        int i40 = fn.IndexOf("\"TurnEnd\"", StringComparison.Ordinal);
        CheckRaw(i41 >= 0 && i40 > i41,
                 "D5 ★★★ **次序**：`TurnBeforeEnd(41)` 在前、`TurnEnd(40)` 在后（与棋盘侧 `:8806/:8807` 同序）"
               + "｜🧨 把两趟对调 ⇒ 红");
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>建一局：双方各 6 张填充卡 + 一张督军；0 号玩家格 3 放载体（带 `120` 的 `ca 20/40/480` 三行）。</summary>
    static BattleContext MakeBattle(out UnitState carrier)
    {
        carrier = null;
        var heroA = new CardDef("B142E24HeroA", "B142E24HeroA", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("B142E24HeroB", "B142E24HeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var body = new CardDef("B142E24Slayer", "B142E24Slayer", "unit", "", "common", "Test", 1, 1, 20, 0,
                               null, subtype: "Infantry",
                               abilityTriggers: new[] { 120 }, abilityTriggersVersioned: true,
                               abilityTargets: new[] { new CardDef.AbilityTargetSpec { Trigger = 120, TargetsAffected = new[] { 20, 20, 20 } } });
        body.AttachTriggerAbilityIndices(new[]
        {
            new CardDef.TriggerAbilityIndexSpec { Ab = 0, Lg = 0, Trigger = 120, TriggerValue = 0, Ca = 20, Ta = 20, Pa = 0, Fm = 0, Mult = 0 },
            new CardDef.TriggerAbilityIndexSpec { Ab = 0, Lg = 1, Trigger = 120, TriggerValue = 0, Ca = 40, Ta = 20, Pa = 0, Fm = 0, Mult = 0 },
            new CardDef.TriggerAbilityIndexSpec { Ab = 0, Lg = 2, Trigger = 120, TriggerValue = 0, Ca = 480, Ta = 20, Pa = 0, Fm = 0, Mult = 0 },
        });
        var deckA = new List<CardDef> { heroA, body };
        var deckB = new List<CardDef> { heroB };
        for (int i = 0; i < 6; i++) { deckA.Add(PlainUnit("B142E24FillA" + i)); deckB.Add(PlainUnit("B142E24FillB" + i)); }
        var ctx = RuleCore.NewBattle(deckA, deckB, seed: 0, shuffle: false, cardPool: new List<CardDef>());
        if (ctx == null) return null;
        carrier = PlaceUnit(ctx, 0, 3, body);
        return ctx;
    }

    /// <summary>手牌夹具：`40`/`41` 各一条 `ca 160 + ta 20` 行（手牌上下文唯一有执行器的那档）。</summary>
    static CardDef HandCard(string id, int[] triggers, bool handContext = true)
    {
        IEnumerable<CardDef.AbilityContextSpec> ctxSpecs = null;
        if (handContext)
            ctxSpecs = new[]
            {
                new CardDef.AbilityContextSpec { Trigger = 40, Name = "TurnEnd", Context = CardDef.AbilityContextHand },
                new CardDef.AbilityContextSpec { Trigger = 41, Name = "TurnBeforeEnd", Context = CardDef.AbilityContextHand },
            };
        var c = new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 20, 0, null, subtype: "Infantry",
                            abilityContext: ctxSpecs, abilityContextsVersioned: true,
                            abilityTriggers: triggers, abilityTriggersVersioned: true,
                            abilityTargets: new[]
                            {
                                new CardDef.AbilityTargetSpec { Trigger = triggers[0], TargetsAffected = new[] { 20 } },
                                new CardDef.AbilityTargetSpec { Trigger = triggers[1], TargetsAffected = new[] { 20 } },
                            });
        var rows = new List<CardDef.TriggerAbilityIndexSpec>();
        for (int i = 0; i < triggers.Length; i++)
            rows.Add(new CardDef.TriggerAbilityIndexSpec { Ab = i, Lg = 0, Trigger = triggers[i], TriggerValue = 0, Ca = 160, Ta = 20, Pa = 0, Fm = 0, Mult = 0 });
        c.AttachTriggerAbilityIndices(rows.ToArray());
        return c;
    }

    static CardDef PlainUnit(string id)
    {
        return new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 20, 0, null, subtype: "Infantry");
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

    static CardDef Find(List<CardDef> pool, string id)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c != null && string.Equals(c.Id, id, StringComparison.Ordinal)) return c;
        }
        return null;
    }

    static bool HasTrigger(BattleContext ctx, int fromIndex, string keyword)
    {
        if (ctx == null || ctx.Signals == null) return false;
        for (int i = Math.Max(0, fromIndex); i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e != null && e.Kind == EvtKind.Trigger && string.Equals(e.Keyword, keyword, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    // ==================================================================
    //  源码文本处理
    // ==================================================================

    static string ReadSource(string rel)
    {
        string path = Path.Combine(Application.dataPath, rel);
        return File.Exists(path) ? File.ReadAllText(path) : "";
    }

    /// <summary>从 `start` 切到 `end`（切不到返回空串 —— 调用方必须断前提）。</summary>
    static string Cut(string s, string start, string end)
    {
        if (string.IsNullOrEmpty(s)) return "";
        int i0 = s.IndexOf(start, StringComparison.Ordinal);
        if (i0 < 0) return "";
        int i1 = s.IndexOf(end, i0 + start.Length, StringComparison.Ordinal);
        return i1 > i0 ? s.Substring(i0, i1 - i0) : s.Substring(i0);
    }

    static int CountOf(string s, string needle)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    /// <summary>去掉 `//` 与 `/* */` 注释（保留字符串内容）—— 注释里的示例不算接线。</summary>
    static string StripComments(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder(s.Length);
        bool line = false, block = false, str = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            char n = i + 1 < s.Length ? s[i + 1] : '\0';
            if (line)
            {
                if (c == '\n' || c == '\r') { line = false; sb.Append(c); }
                continue;
            }
            if (block)
            {
                if (c == '*' && n == '/') { block = false; i++; }
                continue;
            }
            if (str)
            {
                sb.Append(c);
                if (c == '\\' && n != '\0') { sb.Append(n); i++; continue; }
                if (c == '"') str = false;
                continue;
            }
            if (c == '/' && n == '/') { line = true; i++; continue; }
            if (c == '/' && n == '*') { block = true; i++; continue; }
            if (c == '"') { str = true; sb.Append(c); continue; }
            sb.Append(c);
        }
        return sb.ToString();
    }

    // ==================================================================
    //  最小断言框架
    // ==================================================================

    static void Check<T>(T actual, T expected, string msg)
    {
        Report(EqualityComparer<T>.Default.Equals(actual, expected),
               msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckRaw(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }
}
