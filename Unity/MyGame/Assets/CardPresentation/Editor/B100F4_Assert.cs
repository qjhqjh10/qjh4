// B100F4_Assert.cs — 第一百会话 · 波 D · 执行代理 `B100_F4`（引擎线 · `Requiem` 入表）
//
// ============================ 它盯的是什么 ============================
//  `Requiem`（安魂）的正文**收不下来**那半个缺口 —— 施工判据 → `资料/完全复刻/判据/B100_D4_引擎三条_施工单.md` §二。
//
//  ① `DefineTrait.requiem = 760` / `AbilityTrigger.Requiem = 430`（`CardDef.cs:1932` 的 `{"Requiem", 430}`）——
//     触发号、入口、广播**全都在**（`RuleCore.ResolveTriggerRequiem`，`RuleCore.cs:14340`；接线
//     `RuleCorePartial_B99E8.cs:76`），唯一缺的就是「正文收得到」这一步：
//     `CardDef.AddTriggerOp` 的整词闸 `IsRoutableTrigger(head)`（`CardDef.cs:5041`）只认
//     `RoutableTriggers` 里的词 ⇒ `requiem` 不在表里 ⇒ `TriggerOps("requiem")` 恒 null
//     ⇒ `FireTriggerAt` 走 `CardDef.cs` 那句 early-return（`ops == null && spec == null && skipDraw == null`）
//     ⇒ **430（自己那条正文）连 `EvtKind.Trigger` 都不发**（435「别人看见」照旧广播，与本件无关）。
//  ② 本件 = **三张表各一格**（同一物理行内追加，零行数变化）：
//     `RoutableTriggers`（收正文）· `BodyKeywords`（断句，否则 `Rally: …. Requiem: …` 被前一条**静默吞并**）
//     · `Implemented`（「那个时机真的有人读它」—— 入口已接，成立）。
//
// ============================ 🔴 覆盖边界（如实记）============================
//   · **全池 1129 张（`cards_engine.json` v44）扫 `requiem` = 0 命中** ⇒ 本件是**休眠件**：
//     补完即正确、**零可观测变化**（`ruleprobe check` 解析 md5 / `play check` 结算指纹均应逐字节不变）。
//   · **435（`OtherCardRequiem`）不需要进 `RoutableTriggers`** —— 它没有「`OtherCardRequiem:` 卡面写法」，
//     只在事件层被消费（`EffectResolver.cs:7569` 的 `case WhenEventKind.OtherCardRequiem`）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里加（与前几批同形，**同一物理行内追加** ⇒ 零行数变化）：
//      B100F4_Assert.Run(); _pass += B100F4_Assert.Pass; _fail += B100F4_Assert.Fail;
//  建议位置 = 紧接 `B99E8_Assert.Run(); …` 那一串之后（同一族：430/435）。
//  本件**只碰纯引擎**，不建 GameObject / 不建资产 / 不需要场景（同 `B99E8_Assert`）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   · A1：从 `CardDef.RoutableTriggers` 去掉 `KeywordTable.Requiem` ⇒ `TriggerOps("requiem")` 恒 null
//         ⇒ A1 / B1 / C0b / C1 **同时**红。
//   · A2：从 `CardDef.BodyKeywords` 去掉它 ⇒ B2 红（`Rally` 的正文原文里会带上 `Requiem: …`）。
//   · A3：从 `KeywordTable.Implemented` 去掉它 ⇒ A3 红（`When … triggers Requiem` 那族监听卡注册不上）。
//   · C1：把 `FireTriggerAt` 的 `ops == null && …` 那道 early-return 删掉 ⇒ C1 变绿 —— 所以 C1 **必须**配
//         A1/B1（这两条守的是「ops 真的被收下来」，C1 守的是「收下来之后执行口认得出」）。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B100_F4`：`Requiem` 进 `RoutableTriggers` / `BodyKeywords` / `Implemented`（430 正文可达）。</summary>
public static class B100F4_Assert
{
    static readonly CheckSink _sink = new CheckSink("[F4] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();

        MenuCheck.Section(_sink, "B100_F4 `Requiem`（安魂 430）入表：`RoutableTriggers` / `BodyKeywords` / `Implemented`");

        SectionA_Tables();
        SectionB_Parse();
        SectionC_Runtime();
        SectionD_PoolAsleep();

        Debug.Log(_sink.P + "B100F4 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具

    static CardDef Warlord(string name, string faction)
    {
        return new CardDef(name, name, "hero", "", null, faction, 0, 2, 30, 0, null, subtype: "Warlord");
    }

    static BattleContext NewCtx()
    {
        var a = new List<CardDef> { Warlord("B100F4Hero0", "SpaceWolves") };
        var b = new List<CardDef> { Warlord("B100F4Hero1", "Goff") };
        return RuleCore.NewBattle(a, b, seed: 0, shuffle: false);
    }

    /// <summary>白盒放盘（`RuleEngineTest.Place` 的等价物；⛔ 不走棋盘钩子）。</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card, bool warlord = false)
    {
        var u = new UnitState(ctx.NewInstance(card), warlord);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    static bool InTable(string[] table, string word)
    {
        if (table == null) return false;
        for (int i = 0; i < table.Length; i++) if (table[i] == word) return true;
        return false;
    }

    // ============================================================ A 三张表各一格

    static void SectionA_Tables()
    {
        MenuCheck.Section(_sink, "A 三张表各一格（判据 → `CardDef.RoutableTriggers` / `BodyKeywords` / `Implemented` 的 doc）");

        Tr(InTable(CardDef.RoutableTriggers, KeywordTable.Requiem),
           "A1 ★★ `Requiem` 在 `CardDef.RoutableTriggers` 里 —— 那是 `AddTriggerOp` 收正文的**整词闸**"
         + "（`CardDef.IsRoutableTrigger`；先例 = `Survivor` / `Sacrifice` / `Landing` 三个 0 张卡也入表）"
         + "｜🧨 去掉这一格 ⇒ `TriggerOps(\"requiem\")` 恒 null ⇒ A1 / B1 / C0b / C1 一起红");

        Tr(InTable(CardDef.BodyKeywords, KeywordTable.Requiem),
           "A2 ★★ `Requiem` 在 `CardDef.BodyKeywords` 里（`BodyKeywords ⊆ RoutableTriggers`；它决定 "
         + "`Rally: …. Requiem: …` 在哪断句 —— 不进就会被前一条**静默吞并**）"
         + "｜🧨 去掉这一格 ⇒ B2 红（`Rally` 的正文原文里会带上 `Requiem: …`）");

        Tr(KeywordTable.Implemented.Contains(KeywordTable.Requiem),
           "A3 ★ `Requiem` 在 `KeywordTable.Implemented` 里 —— 判据 =「那个时机**真的有人读它**」，"
         + "入口 `RuleCore.ResolveTriggerRequiem`（`RuleCore.cs:14340`）已在位 ⇒ 成立"
         + "｜🧨 去掉 ⇒ `When … triggers Requiem` 那族监听卡注册不上（`WhenEvent.cs` 的 `Implemented.Contains` 闸）");

        C(KeywordTable.Normalize("Requiem"), KeywordTable.Requiem,
          "A4 （前提）`Normalize(\"Requiem\")` 认得出来 —— `Prefixes` 里那一格 2026-10-26（`F3` R4）就登记了，本笔**不动**它");
    }

    // ============================================================ B 解析：正文收得下 + 断句

    static void SectionB_Parse()
    {
        MenuCheck.Section(_sink, "B 合成卡：`Requiem:` 的正文收得下来，且不被前一条静默吞并（⛔ 与 `A1566` 的 `Landing` 同形）");

        // 形状照 `RuleEngineTest.TestA1566LandingAndAboutToAttackRegistration` ③（那一组已经证明
        // 「`Rally: Deal 3 damage to an enemy. <词>: Gain +2 Attack`」能被切成两段）。
        var split = new CardDef("B100F4Split", "B100F4Split", "unit",
                                "Rally: Deal 3 damage to an enemy. Requiem: Gain +2 Attack",
                                "common", "Test", 1, 1, 5, 0, new[] { "Rally", "Requiem" });

        var rq = split.TriggerOps(KeywordTable.Requiem);
        Tr(rq != null && rq.Count == 1,
           "B1 ★★ `Requiem:` 那半句**收得下来**（`TriggerOps(\"requiem\")` == 1 条）"
         + "｜🧨 从 `RoutableTriggers` 去掉 `requiem` ⇒ `TriggerOps` 恒 null ⇒ 红（改前就是这个状态）");

        var rally = split.TriggerOps(KeywordTable.Rally);
        Tr(rally != null && rally.Count == 1,
           "B1b （前提）`Rally:` 那半句也收下来了（1 条）—— 否则 B2 测不出「吞并」");

        string rt = split.TriggerText(KeywordTable.Rally);
        Tr(rt != null && rt.IndexOf("Requiem", StringComparison.Ordinal) < 0,
           "B2 ★★ `Rally` 的**正文原文**里没有 `Requiem:` 那半句（断句靠 `BodyKeywords` → "
         + "`TriggerBodyAt` 的 `StartsAnotherThing` / `IsBodyKeywordHead`）"
         + "｜🧨 从 `BodyKeywords` 去掉 `requiem` ⇒ 本行实得含 `Requiem: Gain +2 Attack` ⇒ 红");

        // 灭自证：**只**多出 requiem 这一格，不把 `Rally` 自己的正文挤掉。
        Tr(split.TriggerText(KeywordTable.Requiem) != null
           && split.TriggerText(KeywordTable.Requiem).IndexOf("Gain +2 Attack", StringComparison.Ordinal) >= 0,
           "B3 灭自证：`Requiem` 自己的正文原文里**确有** `Gain +2 Attack`（两段各归各的，不是把整条 desc 都塞给一个词）");
    }

    // ============================================================ C 执行链：430 真的跑得起来

    static void SectionC_Runtime()
    {
        MenuCheck.Section(_sink, "C 执行链：`FireTriggerAt(…, requiem, …)` 有东西可跑 · `ResolveTriggerRequiem` 的入口闸");

        var ctx = NewCtx();
        var card = new CardDef("B100F4RQ", "B100F4RQ", "unit", "Requiem: Gain +2 Attack",
                               "common", "Test", 1, 1, 3, 0, new[] { "Requiem" });
        var u = Place(ctx, 0, 5, card);

        // C0 前提（先硬检查，⛔ 别写成 `if (前提) Check(结论)`）
        Tr(u != null && ReferenceEquals(u.Card, card), "C0 （前提）带 `Requiem:` 正文的单位真的挂到了 `board[0][5]` 上");
        Tr(u != null && u.Card.TriggerOps(KeywordTable.Requiem) != null,
           "C0b （前提）它的 `TriggerOps(\"requiem\")` **非 null**（= A1 那格真的生效了）");

        // C1 主结论：`FireTriggerAt` 不再走 early-return。
        int evBefore = ctx.Events.Count;
        bool fired;
        using (RuleCore.BeginAction(ctx)) { fired = RuleCore.FireTriggerAtForTest(ctx, u, KeywordTable.Requiem, 0, 5); }
        RuleCore.PumpActionQueues(ctx);

        Tr(fired,
           "C1 ★★ `FireTriggerAt(…, requiem, …)` 返回 **true**（= 有东西可跑）"
         + "｜🧨 从 `RoutableTriggers` 去掉 `requiem` ⇒ `ops == null` ⇒ 那句 early-return ⇒ 返回 **false** ⇒ 红");

        Tr(ctx.Events.Count > evBefore,
           "C2 ★★ 430 自己那条**真的发了事件**（`EvtKind.Trigger`）—— 改前它连事件都不发（只 435 会发）"
         + "｜🧨 同上：ops 收不下来 ⇒ `emitSignal` 走不到 ⇒ `ctx.Events` 一条不多 ⇒ 红");

        // C3 生产入口：`ResolveTriggerRequiem` 在「单位在场」时返回 true。
        int ev2 = ctx.Events.Count;
        bool ok = RuleCore.ResolveTriggerRequiem(ctx, 0, 5);
        RuleCore.PumpActionQueues(ctx);
        Tr(ok, "C3 ★ `ResolveTriggerRequiem(ctx, 0, 5)`（单位在场）返回 **true**");
        Tr(ctx.Events.Count > ev2,
           "C3b 它也真的派发了事件（430 自己那条 ＋ 435 别人），不是只 `return true`");

        // C4 入口闸：格子上没人 ⇒ false（原版 `ResolveTriggerRequiem.c:29` 的 `IsInPlayOrDying` 闸）
        Tr(!RuleCore.ResolveTriggerRequiem(ctx, 1, 0),
           "C4 ★ 入口闸：`board[1][0]` 上没有单位 ⇒ 返回 **false**（原版 `IsInPlayOrDying` 那条闸）"
         + "｜🧨 去掉 `u == null || !IsUnitInPlayOrDying(...)` 那一句 ⇒ 返回 true ⇒ 红");
    }

    // ============================================================ D 池内可达性（休眠件的唯一前提）

    static void SectionD_PoolAsleep()
    {
        MenuCheck.Section(_sink, "D 池内可达性：全池 0 张卡带 `requiem`（⇒ 本笔零可观测变化、解析指纹应逐字节不变）");

        var pool = CardDatabase.Load();
        Tr(pool != null && pool.Count > 500, "D0 （前提）卡池读得到且规模正常（≥ 500 张）");

        int withKw = 0, withOps = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null) continue;
            if (c.Has(KeywordTable.Requiem)) withKw++;
            if (c.TriggerOps(KeywordTable.Requiem) != null) withOps++;
        }

        C(withKw, 0,
          "D1 🔴 全池 **0 张**卡在 `keywords` / `cardTraits` 里带 `requiem`"
        + "｜🧨 这一格是「本笔零可观测」的**唯一**前提。数据侧哪天真出现带 `requiem` 的卡 ⇒ 本行会红 —— "
        + "那时**别把它改成 1**：删掉这条、并按 `ruleprobe check` 重钉解析指纹（那才是新现实的判据）");

        C(withOps, 0,
          "D1b 🔴 全池 **0 张**卡的 `TriggerOps(\"requiem\")` 非 null（= `Requiem:` 正文一处都没被收下来，与本笔改前一致）");
    }
}
