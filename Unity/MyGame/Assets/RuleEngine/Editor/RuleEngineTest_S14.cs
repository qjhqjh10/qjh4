// RuleEngineTest_S14.cs — 第十四会话（2026-10-10）· **规格表里落在本宿主上那批断言的落地**
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（已有兄弟
// `RuleEngineTest_BoardModel.cs` / `RuleEngineTest_Diag.cs`），而**同一时刻只能有一个写手改一个文件**
// ⇒ 本批一律写在这里，`RuleEngineTest.cs` **一个字都没动**（`Run()` 里的 `Section`/`Step` 由调度台挂）。
//
// 唯一施工图 = `资料/普查产出_第十四会话/断言批_合并规格.md` 的「宿主 = `RuleEngine/Editor/RuleEngineTest.cs`」
// 那一节（#1–#56）。每条断言的编号（`#N`）就是那张表的行号。
//
// 🔴 **期望值的取法**（铁律 2 / 铁律 10）：先查原版反编译方法体，**再**用离线探针
//    （`工具/ruleprobe` 那套：只编 `RuleEngine/Core` + `Data/SimpleAI.cs`，不跑 Unity）
//    把每一格**实测**一遍，然后才写进断言 —— 所以下面每一句的期望值都是**量出来的**，
//    不是从实现里读回来的。凡与规格表给的期望值不一致的，就地标了「⚠️ 实测」并写清差在哪。
//
// 🔴 **两个记账口**（本仓铁律）：本文件里凡**改棋盘 / 直接调 `RuleCore.*`** 的，**都是夹具直调**，
//    不是走 `BattleDriver.LocalAct` / `SimpleAI.ExecuteAction` 那两个记账口 ⇒ **不会被录进本地录像**。
//    本批全程如此（这是断言不是对局），不逐条重复标注。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    // ==================================================================
    //  本批专用的小工具（名字一律带 `S14` 前缀，⛔ 不与既有私有辅助重名）
    // ==================================================================

    /// <summary>数 `ctx.Signals` 里满足条件的条目。`keyword == null` = 不看关键词；
    /// `amount == null` = 不看数值（**`0` 与「不看」是两回事** —— 盾/堡垒挡下那条就是 `Amount == 0`）。</summary>
    static int S14Sig(BattleContext ctx, EvtKind kind, string keyword = null, int? amount = null)
    {
        int n = 0;
        foreach (var e in ctx.Signals)
        {
            if (e.Kind != kind) continue;
            if (keyword != null && e.Keyword != keyword) continue;
            if (amount.HasValue && e.Amount != amount.Value) continue;
            n++;
        }
        return n;
    }

    /// <summary>第一条「这条 kind + 这个关键词」的事件在 `Signals` 里的下标（`-1` = 没有）。
    /// **只用来量两条触发之间的先后**（次序判别式）。</summary>
    static int S14SigIndex(BattleContext ctx, EvtKind kind, string keyword)
    {
        for (int i = 0; i < ctx.Signals.Count; i++)
            if (ctx.Signals[i].Kind == kind && ctx.Signals[i].Keyword == keyword) return i;
        return -1;
    }

    /// <summary>`KeywordTable.Parse` 的**安全读法**（缺键返回 `-1`）。
    /// ⛔ 不用 `dict[key]` —— 缺键会抛 `KeyNotFoundException`，把一个测试的失败
    /// 变成「整轮断在这儿」（`RuleEngineTest.Run` 没有逐条 try/catch）。</summary>
    static int S14Kw(Dictionary<string, int> d, string key)
    {
        int v;
        return (d != null && d.TryGetValue(key, out v)) ? v : -1;
    }

    /// <summary>`ctx.Events`（日志）里有没有含这个片段的句子。</summary>
    static bool S14Log(BattleContext ctx, string needle)
    {
        foreach (var e in ctx.Events)
            if (!string.IsNullOrEmpty(e) && e.Contains(needle)) return true;
        return false;
    }

    /// <summary>解析结果的**形状签名** —— 逐 op 记下「动词/数值/载荷/侧-种类-代词侧-判出的侧/代价/共享位」。
    /// 只给「幂等」那一条用（`#36`）：条件种类、目标种类、代词侧这三样正是会被回填改掉的东西。</summary>
    static string S14Shape(IList<EffectOp> ops)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(ops == null ? "null" : "n=" + ops.Count);
        if (ops == null) return sb.ToString();
        foreach (var op in ops)
        {
            var t = op.Target;
            sb.Append(";").Append(op.Verb).Append("/").Append(op.Amount).Append("/").Append(op.Payload)
              .Append("/").Append(op.Cost).Append(op.CostShared ? "s" : "-")
              .Append("/").Append(t == null
                    ? "<null>"
                    : t.Side + "," + t.Kind + "," + (t.PrevSide ?? "<null>") + "," + (t.ResolvedSide ?? "<null>"));
        }
        return sb.ToString();
    }

    // ==================================================================
    //  #1 / #2 —— 幸存者（`Survivor X`）：救回那一条路，与「摧毁绕过它」那一条路
    // ==================================================================

    /// <summary>
    /// **`#1` 幸存者救回** ＋ **`#2` 摧毁路绕过幸存者**。
    ///
    /// 判据 = 原版 `CardScript__CheckIfDead.c:108-165`（第一权威，逐句）：
    ///   `:108` `health < 1` ∧ `IsInPlay()` ∧ `:113` `CurrentSurvivor < 1` 那个 `if` **为假**
    ///   ⇒ `:152-158` 献祭（**本方回合**）⇒ `:160-165` `CardScript__UseSurvivor`。
    ///   而**摧毁**那条路（`BattleManager__ResolveDestroyUnit`）**全文没有**
    ///   `CurrentSurvivor` / `UseSurvivor` / `CheckIfDead` 调用 ⇒ `maySurvive: false`（默认）。
    /// 我们的落点 = `RuleCore.CleanupDeaths(ctx, p, slot, killer, maySurvive)` 的幸存者支路
    /// ＋ `UnitState.UseSurvivor()`。
    ///
    /// ⚠️ **与规格表的差异（实测）**：表里 `#1` 写「恰好一条 `EvtKind.Hit{Amount = -3}`」——
    ///    那个 `-3` 在「3 上限血 / 当前 2 血 / 打 5」这套夹具下**不成立**。
    ///    `UseSurvivor` 的回血量 = **`Health_after − Health_before`** = `3 − (2 − 5)` = **6**
    ///    （原版 `UseSurvivor.c:29-39`：`Min(CurrentSurvivor, MaxHealth)` 写回，回血量由它推）
    ///    ⇒ 事件是 `Hit{Amount = -6}`。下面按**实测**写，并把算式留在消息里。
    /// </summary>
    static void TestS14SurvivorRescueAndDestroyBypass()
    {
        // ---- ① #1 幸存者救回（伤害路）----
        {
            var hit5 = Tactic("S14_Hit5", 0, "Deal 5 damage to an enemy");
            var ctx = ProbeBattle(new[] { hit5 }, new[] { Unit("S14_Foe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var u = Place(ctx, 1, 3, Unit("S14_Surv", 1, 1, 3), exhausted: true);
            u.Health = 2;                                    // 当前 2 血、上限 3（`Min(3, 3) = 3`）
            u.AddKeyword(KeywordTable.Survivor, 3);
            Check(u.Survivor, 3, "★ （前提）`survivor 3` 真挂上了（不挂的话下面两种实现都「不触发」）");
            Check(u.Health, 2, "（前提）当前 2 血（上限 3 —— 决定救回后是 3 而不是别的数）");

            ctx.ClearSignals();
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "S14_Hit5"), 3), RuleCodes.OK,
                      "打出 5 伤（伤害路 ⇒ `Hurt` → `RemoveIfDead(maySurvive: true)`）");

            Check(u.Health, 3, "★ 救回：生命 = `Min(CurrentSurvivor 3, MaxHealth 3)` = **3**");
            CheckTrue(u.Has(KeywordTable.SurvivorSpent),
                      "★ 挂上 `survivorspent`（原版 `UseSurvivor.c:71 AddTraitSilently(0x1b8)`）");
            Check(u.Survivor, 0, "★ `survivor` 被**整个摘掉**（原版 `:41-42`，⛔ 不是减一层）");
            Check(SlotOf(ctx, 1, "S14_Surv"), 3, "★ **留场**（还在原格 3）");
            CheckTrue(object.ReferenceEquals(u, Board(ctx, 1, 3)),
                      "★ **同一格上还是原来那一份 `UnitState`** —— 防「翻面 / 新建一份」被当成救回"
                    + "（只断 `Health` 的话，新建一份血=3 的也会绿）");
            Check(ctx.Players[1].Discard.Count, 0, "★ **不进弃牌堆**");
            Check(ctx.DeadUnits.Count, 0, "★ **不进 `DeadUnits`**");
            Check(ctx.DiedThisTurn, 0, "★ **`DiedThisTurn` 不加**（「按阵亡数结算」的效果不许把它算进去）");
            Check(S14Sig(ctx, EvtKind.Death), 0, "★ **一条 `EvtKind.Death` 都不发**");
            Check(S14Sig(ctx, EvtKind.Hit, null, -6), 1,
                  "★ **恰好一条** `EvtKind.Hit{Amount = -6}`（负数 = 治疗，表现层据此走绿字；"
                + "回血量 = 3 − (2 − 5) = 6）。⚠️ 规格表 `#1` 写的 `-3` 在这套夹具下不成立，见本方法头注"
                + LogTail(ctx, 6));
            Check(S14Sig(ctx, EvtKind.Hit, null, -6) + S14Sig(ctx, EvtKind.Hit, null, 5), 2,
                  "……而且这一格总共只有两条 `Hit`（一下 5 伤 + 一条治疗），没有多发的");
        }

        // ---- ② #2 摧毁路**绕过**幸存者 ----
        {
            var destroy = Tactic("S14_Destroy", 0, "Destroy an enemy troop");
            var ctx = ProbeBattle(new[] { destroy }, new[] { Unit("S14_Foe2", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var v = Place(ctx, 1, 3, Unit("S14_Surv2", 1, 1, 3), exhausted: true);
            v.AddKeyword(KeywordTable.Survivor, 3);
            Check(v.Survivor, 3, "（前提）它带着 `survivor 3`");

            ctx.ClearSignals();
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "S14_Destroy"), 3), RuleCodes.OK,
                      "打出 `Destroy an enemy troop`（摧毁路 ⇒ `CleanupDeaths(maySurvive)` 默认 `false`）");

            Check(SlotOf(ctx, 1, "S14_Surv2"), -1,
                  "★ **摧毁路绕过幸存者**：它照旧离场（原版 `ResolveDestroyUnit` 全文没有 `UseSurvivor`）"
                + "｜🧨 把 `CleanupDeaths` 调用点的 `maySurvive &&` 去掉 ⇒ 上面 `#1` 那一格必红"
                + "（两条不可能同时成立）");
            Check(v.Survivor, 3, "★ 幸存者**一个都没被消耗**（还在身上、也没挂 `survivorspent`）");
            CheckTrue(!v.Has(KeywordTable.SurvivorSpent), "……`survivorspent` 也没挂");
            Check(S14Sig(ctx, EvtKind.Death), 1, "……而死亡事件照发");
            Check(S14Sig(ctx, EvtKind.Hit, null, -6), 0, "……**也没有**那条治疗 `Hit`");
        }
    }

    // ==================================================================
    //  #4（并与 #3 并排）—— 狂喜读【印刷】关键词，运行时授予的不触发
    // ==================================================================

    /// <summary>
    /// **`#4` 运行时被授予的 `ecstasy` 不触发**（与 `#3` 的印在卡面那一格**并排**）。
    ///
    /// 判据 = 原版 `CardScript__ShouldTriggerEcastasy.c` 四条守卫的**第一条**：
    ///   `RawCardScript.HasDefaultTrait(raw, 0x4e2)` —— 读的是**印刷**关键词
    ///   （我们这边 = `u.Card.Has(KeywordTable.Ecstasy)`，`CardDef._keywords` 就是卡面印的那一份）。
    ///   ⚠️ 这是 `A1159` 改出来的**有意行为差**：⛔ 别改回 `u.Has(...)`。
    ///
    /// **判别式（本格的关键）**：夹具 A 的卡**不印** `Ecstasy` ⇒ `Card.EcstasyX` 兜底 **1**；
    ///   把血从 2 打到 1 **正好跨过那个 1** ⇒ 若实现退回 `u.Has(ecstasy)`（这一份**是**真挂着的），
    ///   它就**会**触发、攻 1 → 2。⇒ 本格对「读印刷值 / 读当前值」这两条路**必红其一**。
    ///
    /// `#3`（卡面印 `Ecstasy 2:` ⇒ 跨过阈值触发、攻 +1）**已有**：`TestUnstableEcstasyCruelty` 的 ②
    /// （夹具 `FixtureEcstasy`，3 → 2 断攻 1 → 2）⇒ 本文件**不写第二份**；这里只用印着的那一份
    /// 当夹具 A 的**对照**（同一句伤害、同一套数值形状，一个触发一个不触发）。
    /// </summary>
    static void TestS14EcstasyPrintedVersusGranted()
    {
        var hit1 = Tactic("S14_Hit1", 0, "Deal 1 damage to an enemy");

        // ---- 夹具 A：卡面**不印** `Ecstasy`，运行时 `AddKeyword` 挂上去 ----
        {
            var plain = new CardDef("S14_EcsGranted", "S14_EcsGranted", "unit", "",
                                    "common", "Test", 1, 1, 5, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { hit1, hit1, hit1, hit1 }, new[] { Unit("S14_FoeA", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var u = Place(ctx, 1, 3, plain, exhausted: true);
            u.Health = 2;
            u.AddKeyword(KeywordTable.Ecstasy, 3);
            CheckTrue(u.Has(KeywordTable.Ecstasy),
                      "★ （前提一）**当前**关键词里**有** `ecstasy` —— 不成立的话这一格就退化成「词压根没挂」");
            CheckTrue(!u.Card.Has(KeywordTable.Ecstasy), "★ （前提二）而**卡面没印**（`CardDef._keywords` 里没有）");
            Check(u.Card.EcstasyX, 1,
                  "（前提三）所以阈值走兜底 `1`（`CardDef.EcstasyX`：正文正则 → 卡面值 → 兜底 1）");

            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "S14_Hit1"), 3);
            Check(u.Health, 1, "打 1 点：生命 2 → 1（**跨过了那个兜底阈值 1**）");
            Check(u.Attack, 1,
                  "★ **运行时授予的 `ecstasy` 【不触发】**（还是 1 攻）——"
                + " 判据 = 原版读的是 `HasDefaultTrait`（**印刷**关键词），⛔ 不是当前 trait"
                + "｜🧨 把 `RuleCore.Hurt` 那一句 `u.Card.Has(KeywordTable.Ecstasy)` 改回 `u.Has(...)`"
                + " ⇒ 本行实得 2 ⇒ 红");
        }

        // ---- 夹具 B：卡面**印** `Ecstasy 2:`（对照 —— 证明这把尺子是活的）----
        {
            var printed = new CardDef("S14_EcsPrint", "S14_EcsPrint", "unit",
                                      "Ecstasy 2: Gain +1 Attack",
                                      "common", "Test", 1, 1, 5, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { hit1, hit1, hit1, hit1 }, new[] { Unit("S14_FoeB", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var u = Place(ctx, 1, 3, printed, exhausted: true);
            u.Health = 3;
            CheckTrue(u.Card.Has(KeywordTable.Ecstasy), "（前提）印着的那份 `CardDef` 上有 `ecstasy`");
            Check(u.Card.EcstasyX, 2, "（前提）阈值从**卡面正文**取到 2（`Ecstasy 2:`）");

            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "S14_Hit1"), 3);
            Check(u.Health, 2, "打 1 点：生命 3 → 2（= 阈值）");
            Check(u.Attack, 2,
                  "★ **对照**：印在卡面上的那份**照样触发**（1 → 2 攻）——"
                + " 它挡的是「把这条闸门改成恒假（谁都不触发）」那种改法");
        }
    }

    // ==================================================================
    //  #8 —— 眩晕广播：观察者真的 +1 攻（把 `BroadcastKeywordEvent(GetsStun)` 那半钉住）
    // ==================================================================

    /// <summary>
    /// **`#8` 同场挂 `When an enemy receives a Stun, gain +1 Attack` 的观察者 ⇒ 断它 +1。**
    ///
    /// 已有那半 = `TestWhenEventsWidened` 的 ⑨②（`RuleEngineTest.cs` 的 `FixtureStunWatch` 那格）——
    /// 它断的是 **`WhenFired(...) == 1`**（监听器响过几次），**没断效果真的落到了攻上**
    /// （规格表把这一格标成「差半步」，就是差这里）。
    ///
    /// 为什么两半都要：`WhenFired` 量的是**日志里那行广播**，而 `gain +1 Attack` 是**结算那一步**；
    /// 广播发了、结算没落（或落错人），`WhenFired` 照样是 1。
    /// 判据 = 原版 `CardScript__Stun.c:48 AddTraitSilently(param_1, 100, …)` 之后那条
    /// `BroadcastUnitStunned`（我们这边 = `EffectResolver.DoStun` 的 `BroadcastKeywordEvent(GetsStun)`）。
    /// </summary>
    static void TestS14StunWatcherGainsAttack()
    {
        var watch = new CardDef("S14_StunWatch", "S14_StunWatch", "unit",
                                "When an enemy receives a Stun, gain +1 Attack",
                                "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
        var stunTac = Tactic("S14_Stun", 0, "Stun an enemy");
        var ctx = ProbeBattle(new[] { watch, stunTac }, new[] { Unit("S14_EFoe", 1, 1, 9) });
        ToP1Turn(ctx, 2);
        var w = Place(ctx, 0, 3, watch, exhausted: true);
        var victim = Place(ctx, 1, 3, Unit("S14_StunVictim", 1, 0, 9), exhausted: true);
        CheckTrue(!victim.IsStunned, "（前提）动手之前它没被晕");
        Check(w.Attack, 2, "（前提）观察者起始 2 攻（`gain +1` 之后要看到 3）");

        ctx.ClearSignals();
        CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "S14_Stun"), 3), RuleCodes.OK, "打出眩晕");
        CheckTrue(victim.IsStunned,
                  "（前提）它真被晕了（`DoStun` 走的是**挂 `stun` trait**那条路，"
                + "`IsStunned` 现在是它的派生只读属性 —— 判据 = 原版 `CardScript__Stun.c:48`）");
        Check(w.Attack, 3,
              "★ **观察者的正文真的结算了：攻 2 → 3** —— 光断 `WhenFired == 1`（= 广播发过）"
            + "量不到这一格：「广播发了、结算没落」在那里照样绿"
            + "｜🧨 把 `DoStun` 里那句 `BroadcastKeywordEvent(ctx, WhenEventKind.GetsStun, t)` 删掉 ⇒ 本行实得 2"
            + LogTail(ctx, 6));
        Check(S14Sig(ctx, EvtKind.Trigger, KeywordTable.Stun), 0,
              "……顺带钉一条：`stun` 自己**没有** `EvtKind.Trigger`（它不是一个「带正文的触发词」）");
    }

    // ==================================================================
    //  #9 / #10 / #11 —— `ApplyDeployTurnState(u, keepStatus)` 的三格
    // ==================================================================

    /// <summary>
    /// **`#9` 默认（`keepStatus = false`）⇒ `SummonSickness == true`**
    /// · **`#10` 预置 `true` ＋ `keepStatus: true` ⇒ **仍是 `true`**
    /// · **`#11` 预置 `true` ＋ `keepStatus: false` ⇒ `true`**（挡「把参数接反」）。
    ///
    /// 判据 = 原版 `CardScript._MinionPlayedIntoField_d__314__MoveNext.c:69-71` 逐字：
    ///   `if (*(char *)(param_1 + 0x30) == '\0') *(undefined1 *)(lVar4 + 0x58) = 1;`
    ///   = `if (!keepStatus) card.summonSickness = true;`
    /// ⇒ **`keepStatus` 为真时这一位保持原值**（**不是**写 0）。
    ///
    /// 🔴 **`#10` 的判别力全在「先把 `SummonSickness` 摆回 `true`」那一句**：
    ///   不摆的话（初始 `false`），「条件写」与「无条件写 `false`」两种实现**都**得到 `false`。
    ///   为了把这件事钉死，本方法末尾**额外**跑了一格「不预置 + `keepStatus: true`」并把实得值写进消息。
    ///
    /// `#9` 的「疑似已有」= `TestA1098SummonSicknessReads` 的 Ⓐ —— 但它走的是**真部署入口**
    /// （`RuleCore.PlayCard`），量的是「部署当回合写了 `+0x58`」；本方法量的是**这个口本身**
    /// （含 `keepStatus` 那个形参的两档）⇒ 不重复，两格互补。
    /// </summary>
    static void TestS14ApplyDeployTurnState()
    {
        var plain = Unit("S14_Deploy", 1, 1, 5);

        var u1 = new UnitState(plain, false);
        CheckTrue(!u1.SummonSickness, "（前提）新造出来的单位初始**没有**召唤病");
        RuleCore.ApplyDeployTurnState(u1);                      // 默认 keepStatus = false
        CheckTrue(u1.SummonSickness,
                  "★ `#9` 默认那档 ⇒ **写了 `+0x58`**（`if (!keepStatus) summonSickness = true;`）");
        CheckTrue(u1.Exhausted && u1.CannotAttackThisTurn,
                  "★ ……同时「不能动 / 不能攻击」也写了（`ActivateMinion` 那一半的等价物）"
                + "｜🧨 删掉 `ApplyDeployTurnState` 里那句 `u.SummonSickness = true;` ⇒ 上面那行红");

        var u2 = new UnitState(plain, false);
        u2.SummonSickness = true;                               // 🔴 判别力的来源就是这一句
        RuleCore.ApplyDeployTurnState(u2, keepStatus: true);
        CheckTrue(u2.SummonSickness,
                  "★ `#10` **先在位摆回 `true`、再 `keepStatus: true` ⇒ 仍是 `true`** ——"
                + " 原版那一位是「**条件写**」而不是「无条件写 0」"
                + "｜🧨 把 `if (!keepStatus)` 改成无条件 `u.SummonSickness = false;` ⇒ 本行红");

        var u3 = new UnitState(plain, false);
        u3.SummonSickness = true;
        RuleCore.ApplyDeployTurnState(u3, keepStatus: false);
        CheckTrue(u3.SummonSickness, "★ `#11` 预置 `true` + `keepStatus: false` ⇒ 仍是 `true`（挡「参数接反」）");

        var u4 = new UnitState(plain, false);                   // 不预置
        RuleCore.ApplyDeployTurnState(u4, keepStatus: true);
        CheckTrue(!u4.SummonSickness,
                  "（如实记：**不预置**时这一格实得 `false` —— 这正说明 `#10` 的判别力"
                + "只来自「先摆回 `true`」那一句，不是来自本断言本身）");
    }

    // ==================================================================
    //  #12 / #13 / #14 —— 中毒（`poisoned`）在【它自己这一方】的回合末被摧毁
    // ==================================================================

    /// <summary>
    /// **`#12` 毒：A 方回合末 ⇒ 摧毁；B 方回合末 ⇒ 不摧毁。**
    /// **`#13` 毒 + `resistant` ⇒ 两个回合末都不摧毁，且日志里有那条豁免。**
    ///
    /// 判据 = 原版 `CardScript__OnTurnEnd.c:110-113` 的三条守卫（逐句）：
    ///   `HasCurrentTrait(card, 200 /*poisoned*/) != 0`
    ///   ∧ `param_2 == *(char *)(card + 0x40)`（`+0x40 = isPlayer` ⇒ **正在结束的正是它自己那一方**）
    ///   ∧ `HasCurrentTrait(card, 600 /*resistant*/) == 0`；
    /// 命中 ⇒ `:229-262` → `:258 BattleManager.DestroyUnit(…, priority 1, deathType poison(40))`。
    /// 我们的落点 = `RuleCore.EndTurn` 的毒支（排在**再生之后**、`DestroyRemnants` 之前）。
    ///
    /// 🔴 **灭自证 = 「B 方回合末不摧毁」那一格**：只断 A 方的话，把 `param_2 == isPlayer` 那道守卫
    ///    （我们这边 = 只扫 `ctx.Players[ctx.Active]`）删掉、改成两边都扫，**A 方那一格仍然全绿**。
    /// </summary>
    static void TestS14PoisonAtOwnTurnEnd()
    {
        // ---- #12 ----
        {
            var ctx = Battle(new[] { Unit("S14_A0", 1, 1, 5) }, new[] { Unit("S14_B0", 1, 1, 5) });
            ToP1Turn(ctx, 2);
            var pa = Place(ctx, 0, 3, Unit("S14_PoisonA", 1, 1, 9), exhausted: true);
            var pb = Place(ctx, 1, 3, Unit("S14_PoisonB", 1, 1, 9), exhausted: true);
            pa.AddKeyword(KeywordTable.Poisoned, 1);
            pb.AddKeyword(KeywordTable.Poisoned, 1);
            CheckTrue(pa.Has(KeywordTable.Poisoned) && pb.Has(KeywordTable.Poisoned),
                      "★ （前提）两边**都**真带着 `poisoned`（全池 0 张卡带它 ⇒ 只能夹具自造）");
            Check(ctx.Active, 0, "（前提）现在结束的是 **A 方（P0）**的回合");

            int disc0 = ctx.Players[0].Discard.Count;
            ctx.ClearSignals();
            RuleCore.EndTurn(ctx);

            Check(SlotOf(ctx, 0, "S14_PoisonA"), -1,
                  "★ **A 方**（= 它自己那一方）回合末 ⇒ 被摧毁、离场");
            Check(ctx.Players[0].Discard.Count, disc0 + 1, "……进了**弃牌堆**");
            Check(ctx.DiedThisTurn, 1, "……`DiedThisTurn` +1");
            Check(S14Sig(ctx, EvtKind.Death), 1, "……发了**一条** `EvtKind.Death`");
            CheckTrue(SlotOf(ctx, 1, "S14_PoisonB") >= 0,
                      "★ **灭自证**：**B 方**那个此刻**一点事没有** —— 这一格才是「只扫当前行动方」"
                    + "那道守卫的判别式（只断 A 方的话，改成两边都扫它照样绿）"
                    + "｜🧨 把毒支里 `ctx.Players[ctx.Active]` 改成两边都扫 ⇒ 本行红");
        }

        // ---- #13 毒 + resistant ----
        {
            var ctx = Battle(new[] { Unit("S14_A1", 1, 1, 5) }, new[] { Unit("S14_B1", 1, 1, 5) });
            ToP1Turn(ctx, 2);
            var pr = Place(ctx, 0, 3, Unit("S14_PoisonR", 1, 1, 9), exhausted: true);
            pr.AddKeyword(KeywordTable.Poisoned, 1);
            pr.AddKeyword(KeywordTable.Resistant, 1);
            CheckTrue(pr.Has(KeywordTable.Poisoned) && pr.Has(KeywordTable.Resistant),
                      "★ （前提）`poisoned` 与 `resistant` **都为真**（否则「没被摧毁」可能是别的理由）");

            RuleCore.EndTurn(ctx);
            CheckTrue(SlotOf(ctx, 0, "S14_PoisonR") >= 0,
                      "★ 毒 + `resistant` ⇒ 本方回合末**也不摧毁**（第三条守卫 `resistant == 0`）"
                      + $"｜🧨 删掉 `u.Has(KeywordTable.Resistant)` 那道守卫 ⇒ 本行红{LogTail(ctx, 4)}");
            CheckTrue(S14Log(ctx, "resistant"),
                      "★ 而且日志里有那条**豁免**（不许静默略过 —— 本工程红线「不许静默失败」）");
        }
    }

    /// <summary>
    /// **`#14` 「毒支**不跳**下面的块」** —— `resistant` + `stun` + 「回合开始时就在晕」的单位，
    /// 在自己的回合末：① 毒被豁免、人活着；② **「摘闸门」那块照跑**（眩晕到期被摘掉）。
    ///
    /// 判据 = `CardScript__OnTurnEnd.c`：三条守卫的跳转**都跳到 `:113` 那个标签**，而毒支那段机器码
    ///   排在标签**之前** ⇒ `.c:260` 那个 `goto` 是 Ghidra 对**直落**的渲染、机器码里**没有跳转指令**
    ///   ⇒ **毒支不跳过** `ShouldRemnantDestroyOnTurnEnd`（`:117`）与摘闸门（`:124-134`）那两块。
    ///   因此 ⛔ 不能把它写成 `else if` 去跳过下面那两段（那正是 `.c` 的假象）。
    ///   我们这边的摘闸门 = `RuleCore.EndTurn` 里 `u.StunnedAtStartOfTurn && u.Has(Stun)` 那一段。
    ///
    /// ⚠️ **规格表自己标着「行为断言不好钉（人先没了）——另一半只在注释里留判据」**：
    ///   确实钉不住「被毒死的那一具身上摘没摘闸门」（它已经不在了）；本方法改成钉
    ///   **「同一个回合末里，毒那条路走不走**都不影响**摘闸门照跑」** —— 上面 `#13` 的豁免格
    ///   提供「人还在」，这里再断「闸门摘了」。两格合起来才是那句注释能变成断言的部分。
    /// </summary>
    static void TestS14PoisonBranchDoesNotSkipStunGate()
    {
        var ctx = Battle(new[] { Unit("S14_A2", 1, 1, 5) }, new[] { Unit("S14_B2", 1, 1, 5) });
        ToP1Turn(ctx, 3);
        var u = Place(ctx, 0, 3, Unit("S14_StunPoison", 1, 1, 9), exhausted: true);
        u.AddKeyword(KeywordTable.Poisoned, 1);
        u.AddKeyword(KeywordTable.Resistant, 1);
        u.AddKeyword(KeywordTable.Stun, 1);
        u.StunnedAtStartOfTurn = true;
        CheckTrue(u.IsStunned && u.StunnedAtStartOfTurn && u.Has(KeywordTable.Resistant),
                  "★ （前提）三样都在：`stun` + 「回合开始就在晕」+ `resistant`");

        RuleCore.EndTurn(ctx);
        CheckTrue(SlotOf(ctx, 0, "S14_StunPoison") >= 0, "① 毒被 `resistant` 豁免 ⇒ 人还在（`#13` 那一格）");
        CheckTrue(!u.Has(KeywordTable.Stun) && !u.IsStunned && !u.StunnedAtStartOfTurn,
                  "★ ② **「摘闸门」那块照跑**：眩晕到期被摘掉（`Has(stun) == false` / `IsStunned == false` /"
                + " 「回合开始就在晕」那一位清 0）—— 毒支**没有**把下面那块跳掉"
                + "｜🧨 把毒支写成 `else if` 串起来、让它 `continue`/跳过下面那两段 ⇒ 本行红"
                + LogTail(ctx, 6));
        CheckTrue(S14Log(ctx, "眩晕到期"), "★ ② 而且日志里明说了那一句（不许静默）");
    }

    // ==================================================================
    //  #15 / #16 / #17 / #20 —— 堡垒（`bastion`）
    // ==================================================================

    /// <summary>
    /// **`#15` 堡垒吃掉整份伤害**：返回 **0**、`t.Health == hp0`、`KwValue("bastion") == 1`、
    /// **恰好一条** `EvtKind.Hit{Amount = 0}`。
    /// **`#16` 堡垒被打穿** ⇒ 只有溢出打到生命：返回 2、`Health == 3`、`bastion` 摘掉；
    /// 边界（**满值 3** 打 3）⇒ 返回 0、血不变、`bastion` 也**摘掉**（`remaining == 0` 那一格）。
    ///
    /// 判据 = 原版 `CardScript._ReceiveDamage_d__381__MoveNext.c:126-156` +
    ///   `CardScript__RemoveBastionDamage.c`（逐句）：整份伤害进堡垒，`remaining = bastion − dmg`；
    ///   `remaining >= 0` ⇒ 生命一点都不动（**含刚好打平**那一格）、`bVar17 = true`；
    ///   `remaining < 0` ⇒ `health + remaining`（= 把**溢出量**打到生命）；
    ///   返回值 = 扣减之后的 `CurrentBastion`（可以为负 = 溢出量）。
    ///   ⚠️ 这两条吃的是**原始伤害**（`dmg`）不是 `DamageAfterReduction` —— 原版那段减甲
    ///   「整段在 `bastion < 1` 那一支里」⇒ 堡垒路与空投舱路**都不减护甲**。
    ///
    /// 🔴 **灭自证的写法照规格表**：格首记 `int hp0 = t.Health;` 并断 `t.Health == hp0`。
    ///   只断「没死」的话，「先扣掉再补回来」也绿。
    /// </summary>
    static void TestS14BastionAbsorbsWholeHit()
    {
        // ---- #15 没打穿 ----
        {
            var ctx = Battle(new[] { Unit("S14_A3", 1, 1, 5) }, new[] { Unit("S14_B3", 1, 1, 5) });
            ToP1Turn(ctx, 2);
            var t = Place(ctx, 1, 3, Unit("S14_Bas5", 1, 1, 5), exhausted: true);
            t.AddKeyword(KeywordTable.Bastion, 3);
            int hp0 = t.Health;
            Check(t.Bastion, 3, "★ （前提）堡垒 3 真挂上了（全池 0 张卡带它 ⇒ 夹具自造）");

            ctx.ClearSignals();
            int ret = RuleCore.ApplyDamage(ctx, t, 2, "自检");
            Check(ret, 0, "★ 整份吃下 ⇒ 返回 **0**（= 没打到生命）");
            Check(t.Health, hp0, "★ **生命一点不动** —— 记了 `hp0` 再比，「扣掉再补回来」在这一行也过不了");
            Check(t.KwValue(KeywordTable.Bastion), 1, "★ 堡垒存量 3 → 1（`RemoveKeyword` 减一层）");
            CheckTrue(t.Has(KeywordTable.Bastion), "……还剩 1 ⇒ 词还在");
            Check(S14Sig(ctx, EvtKind.Hit, null, 0), 1,
                  "★ **恰好一条** `EvtKind.Hit{Amount = 0}`（与 Shield / Invulnerable 挡下同一个口径："
                + "「被打了一下、一点血没掉」）");
            Check(S14Sig(ctx, EvtKind.Hit, null, 2), 0, "……**没有**报告 2 点伤害的那条");
        }

        // ---- #16 打穿 ----
        {
            var ctx = Battle(new[] { Unit("S14_A4", 1, 1, 5) }, new[] { Unit("S14_B4", 1, 1, 5) });
            ToP1Turn(ctx, 2);
            var t = Place(ctx, 1, 3, Unit("S14_BasBreak", 1, 1, 5), exhausted: true);
            t.AddKeyword(KeywordTable.Bastion, 3);
            int hp0 = t.Health;
            ctx.ClearSignals();
            int ret = RuleCore.ApplyDamage(ctx, t, 5, "自检");
            Check(ret, 2, "★ 打穿 ⇒ 返回**溢出量 2**（`bastion 3 − dmg 5 = −2`）");
            Check(t.Health, hp0 - 2, "★ 只有**溢出**打到生命（5 − 2，血 5 → 3）");
            Check(t.KwValue(KeywordTable.Bastion), 0, "★ 堡垒见底 ⇒ 0");
            CheckTrue(!t.Has(KeywordTable.Bastion), "★ ……而且**整个摘掉**（⛔ 不是留着一个 0 层）");
            Check(S14Sig(ctx, EvtKind.Hit, null, 2), 1, "……发的是那条 `Hit{Amount = 2}`");
            Check(S14Sig(ctx, EvtKind.Hit, null, 0), 0, "……**不是** `Amount = 0`（打穿了就不是「整份吃下」）");
        }

        // ---- #16 边界：满值 3 打 3 ⇒ remaining == 0（原版那一格 health += 0）----
        {
            var ctx = Battle(new[] { Unit("S14_A5", 1, 1, 5) }, new[] { Unit("S14_B5", 1, 1, 5) });
            ToP1Turn(ctx, 2);
            var t = Place(ctx, 1, 3, Unit("S14_BasEdge", 1, 1, 5), exhausted: true);
            t.AddKeyword(KeywordTable.Bastion, 3);
            int hp0 = t.Health;
            ctx.ClearSignals();
            int ret = RuleCore.ApplyDamage(ctx, t, 3, "自检");
            Check(ret, 0, "★ **边界（刚好打平）**：返回 **0**、**不进**「打穿」那一支");
            Check(t.Health, hp0, "★ 生命一点不动");
            CheckTrue(!t.Has(KeywordTable.Bastion),
                      "★ ……但堡垒**摘掉了**（`remaining == 0` 那一格：原版 `health += 0` + 把基础值钳到 0）"
                    + "｜🧨 把「打穿」的判据写成 `remaining > 0` ⇒ 本行红");
            Check(S14Sig(ctx, EvtKind.Hit, null, 0), 1, "……发的是 `Amount = 0`（= 整份吃下）");
        }
    }

    /// <summary>
    /// **`#17` 反击不吃堡垒**：`DeclareAttack` 之后**攻击者掉 4 血**、`KwValue("bastion")` **仍是 5**。
    ///
    /// 判据 = 原版 `_ReceiveDamage…:63` 的守卫含 `deathType == combatAttacker(10)`
    ///   （`bastion &lt; 1 || dt == 10` ⇒ **攻击方挨的那一下反击**走堡垒**之外**那条路）；
    ///   我们的落点 = `RuleCore.DeclareAttack` 反击那一跳传 `ignoreBastion: true`（+ `Hurt` 透传）。
    ///
    /// 🔴 **它是「位置」的判别式之一**：删掉 `ignoreBastion: true` ⇒ 本行必红，而 `#15`/`#16` 全绿
    ///   （它们走的是**另一个入口**：直调 `ApplyDamage`）。
    /// </summary>
    static void TestS14CounterIgnoresBastion()
    {
        var ctx = Battle(new[] { Unit("S14_A6", 1, 1, 5) }, new[] { Unit("S14_B6", 1, 1, 5) });
        ToP1Turn(ctx, 2);
        var atk = Place(ctx, 0, 3, Unit("S14_BasAtk", 1, 1, 9), exhausted: false);
        atk.AddKeyword(KeywordTable.Bastion, 5);
        var tgt = Place(ctx, 1, 3, Unit("S14_Counter4", 1, 4, 3), exhausted: true);
        Check(atk.Bastion, 5, "（前提）攻击者带 `bastion 5`");
        Check(tgt.Attack, 4, "（前提）被打的那个近战 4 ⇒ 反击 4");

        ctx.ClearSignals();
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK, "打出去（近战）");
        Check(atk.Health, 5,
              "★ **攻击者真掉了 4 血**（9 → 5）—— 反击**不吃堡垒**"
            + "｜🧨 把 `DeclareAttack` 反击那一跳的 `ignoreBastion: true` 删掉 ⇒ 本行实得 9"
            + "（堡垒把整份 4 吃下、血一点不动）");
        Check(atk.Bastion, 5, "★ 堡垒**原封不动**（还是 5）—— 这一条与「掉血」合起来才排得掉"
                            + "「堡垒扣了、血没掉」那种错法");
        Check(S14Sig(ctx, EvtKind.Hit, null, 4), 1, "……挨的那条 `Hit{Amount = 4}`");
    }

    /// <summary>
    /// **`#18` 空投舱血池三格**：① 打 3 ⇒ 返回 0、`Health == 5`、`KwValue == 1`；
    /// ② 再打 3 ⇒ `Has == false` **且血仍 5**、返回 0；③ 再打 2 ⇒ 落生命、`Health == 3`、返回 2。
    ///
    /// 判据 = 原版 `_ReceiveDamage…:60-61`（有 `dropPod` **优先于堡垒**）+ `:159-183`
    ///   （只扣 `+0xcc..0xdc`）+ `CardScript__CheckIfDead.c:76-96`（池 < 1 ⇒ `RemoveDropPod` 后 `return`，
    ///   **这一下不死**）。我们的落点 = `RuleCore.ApplyDamage` 的空投舱段。
    ///
    /// 🔴 **灭自证 = 第 ② 格**：只断 ①③ 的话，删掉「池 < 1 就摘掉」那一句仍然全绿
    ///   ⇒ 必须**同时**断「摘掉了」与「血一点没动」。
    /// </summary>
    static void TestS14DropPodPool()
    {
        var ctx = Battle(new[] { Unit("S14_A7", 1, 1, 5) }, new[] { Unit("S14_B7", 1, 1, 5) });
        ToP1Turn(ctx, 2);
        var t = Place(ctx, 1, 3, Unit("S14_Pod", 1, 1, 5), exhausted: true);
        t.AddKeyword(KeywordTable.DropPod, 4);
        Check(t.KwValue(KeywordTable.DropPod), 4, "（前提）血池 = 4");
        int hp0 = t.Health;

        int r1 = RuleCore.ApplyDamage(ctx, t, 3, "自检");
        Check(r1, 0, "★ ① 池 4 − 3 ⇒ 返回 0、生命一点不动");
        Check(t.Health, hp0, "★ ① 生命仍是 5（这一条吃的是**原始伤害**、也不减护甲）");
        Check(t.KwValue(KeywordTable.DropPod), 1, "★ ① 池剩 1");

        int r2 = RuleCore.ApplyDamage(ctx, t, 3, "自检");
        Check(r2, 0, "★ ② 再打 3（池 1 − 3 < 1）⇒ 仍返回 0");
        CheckTrue(!t.Has(KeywordTable.DropPod),
                  "★ ② **池被打空 ⇒ 整个摘掉**（原版 `CheckIfDead.c:94 RemoveDropPod(card, true)`）");
        Check(t.Health, hp0,
              "★ ② **而且血一点没动** —— 这两条**合起来**才是判别式："
            + "只断「摘掉了」的话，「摘掉的同时把伤害落到血上」照样绿"
            + "｜🧨 删掉空投舱那一段的 `return 0` ⇒ 本行红");

        int r3 = RuleCore.ApplyDamage(ctx, t, 2, "自检");
        Check(r3, 2, "★ ③ 池没了 ⇒ 这下**照常落生命**（返回 2）");
        Check(t.Health, hp0 - 2, "★ ③ 生命 5 → 3");
    }

    /// <summary>
    /// **`#19` 空投舱【优先于】堡垒**：同一单位 `dropPod 2` + `bastion 5`、打 3 ⇒
    /// `bastion` **不变（5）**、`Has("droppod") == false`、`Health` 不变。
    ///
    /// 判据 = 原版 `_ReceiveDamage…:60-61` 那个 `if (dropPod) … else if (bastion&lt;1 || dt==10) … else 堡垒`
    ///   —— **两个 `if` 的次序**，不是两条独立规则。
    /// 🔴 把两个 `if` 的**次序对调** ⇒ 本条红，而 `#15`/`#16`/`#18` **全绿**（各自那条路都不经过另一段）。
    /// </summary>
    static void TestS14DropPodTakesPrecedenceOverBastion()
    {
        var ctx = Battle(new[] { Unit("S14_A8", 1, 1, 5) }, new[] { Unit("S14_B8", 1, 1, 5) });
        ToP1Turn(ctx, 2);
        var t = Place(ctx, 1, 3, Unit("S14_PodBas", 1, 1, 5), exhausted: true);
        t.AddKeyword(KeywordTable.DropPod, 2);
        t.AddKeyword(KeywordTable.Bastion, 5);
        Check(t.KwValue(KeywordTable.DropPod), 2, "（前提）池 2");
        Check(t.Bastion, 5, "（前提）堡垒 5");
        int hp0 = t.Health;

        int ret = RuleCore.ApplyDamage(ctx, t, 3, "自检");
        Check(ret, 0, "★ 返回 0（空投舱吃下这一下）");
        Check(t.Bastion, 5,
              "★ **堡垒一点没动（还是 5）** —— 空投舱那一段**排在**堡垒之前"
            + "｜🧨 把 `ApplyDamage` 里那两段的次序对调 ⇒ 本行红（堡垒会先被扣掉 3）"
            + "，而 `#15`/`#16`/`#18` 全绿（它们各自都不经过另一段）");
        CheckTrue(!t.Has(KeywordTable.DropPod), "★ 池 2 − 3 < 1 ⇒ 摘掉");
        Check(t.Health, hp0, "★ 生命不变");
    }

    /// <summary>
    /// **`#20` 易伤 + 堡垒**：`vulnerable 2` + `bastion 3`、5 血、打 3 ⇒ `Health == 5`。
    ///
    /// ⚠️ **规格表标着「建议·非必须」，并且明确写了只能断这一种**：原版在
    ///   「打穿、且扣完溢出血还 &gt; 0」那一档会**另补一次易伤伤害**（`:188-257 DealMultiDamage`），
    ///   我们**没补** ⇒ ⛔ **别把「打穿之后会少算」写成期望**（那会把我方缺口固化成断言）。
    ///   本格取的是「没打穿」那一档：堡垒整份吃下 ⇒ 易伤那一次被 `bVar17` 抑制 ⇒ 两边一致。
    /// </summary>
    static void TestS14VulnerableWithBastion()
    {
        var ctx = Battle(new[] { Unit("S14_A9", 1, 1, 5) }, new[] { Unit("S14_B9", 1, 1, 5) });
        ToP1Turn(ctx, 2);
        var t = Place(ctx, 1, 3, Unit("S14_VulBas", 1, 1, 5), exhausted: true);
        t.AddKeyword(KeywordTable.Bastion, 3);
        t.AddKeyword("vulnerable", 2);
        int hp0 = t.Health;
        Check(t.Bastion, 3, "（前提）堡垒 3");
        Check(RuleCore.DamageAfterReduction(t, 3), 5,
              "（前提）易伤 2 确实折进了伤害公式（3 + 2 = 5）—— 所以下面「血没动」不是「伤害本来就是 0」");

        int ret = RuleCore.ApplyDamage(ctx, t, 3, "自检");
        Check(ret, 0, "★ 堡垒整份吃下 ⇒ 返回 0");
        Check(t.Health, hp0,
              "★ **生命一点不动** —— 原版这一档易伤被 `bVar17`（堡垒吃下整份）抑制，两边一致"
            + "｜⚠️ ⛔ 别补一条「打穿之后血该剩多少」：那一档我们**少算一次易伤**（如实记着，"
            + "判据 = `_ReceiveDamage…:188-257` 的 `DealMultiDamage`），写成期望就是固化缺口");
    }

    // ==================================================================
    //  #21 / #22 / #23 / #24 / #25 —— 献祭（`sacrifice`）与幸存者的【触发 id】
    // ==================================================================

    /// <summary>
    /// **`#21` `sacrifice`：本方回合被打 5 伤救回 ⇒ 留场、`survivorspent` 真、
    /// 且 `EvtKind.Trigger{Keyword == "sacrifice"}` 有一条。**
    ///
    /// 判据 = 原版 `CardScript__CheckIfDead.c:152-158`：
    ///   `if (HasCurrentTrait(0x1d6 /*sacrifice*/) != 0) { if (BattleManager.IsPlayerTurn(bm) == card.isPlayer)
    ///    CardScript.TriggerSacrifice(card); }`
    ///   ⇒ 两个条件：**带 `sacrifice`** ∧ **是本方回合**。
    ///   触发 id 那一半 = `TriggerSacrifice.c:41-43 RawCardScript.OnTrigger(raw, 0xdc /*= 220*/)`。
    ///   我们的落点 = `RuleCore.CleanupDeaths` 的幸存者支路，**排在 `UseSurvivor` 之前**
    ///   （原版 `TriggerSacrifice` 在 `:157`、`UseSurvivor` 在 `:165`）。
    ///
    /// ✅ **2026-10-21 就地订正（铁律 5）—— 本段原来记着「正文收不下来」，那条已过期**：
    ///   原文写「卡面 `Sacrifice: Gain +1 Attack` 的正文收不下来 —— `Card.TriggerOps("sacrifice")`
    ///   返回 `null`，因为 `CardDef.RoutableTriggers` 里没有 `survivor` / `sacrifice` 这两个词」
    ///   —— 那是**改前**的事实。另一个写手（`A1386` / `A1389`）把那张表**和** `BodyKeywords`
    ///   各追加了这两个词之后，这段正文**收得下来了**：实测 `sacCard.TriggerOps("sacrifice")`
    ///   = 1 条 op（`gain` / `payload = "+1 attack"` / 目标 `own/unit`）⇒ 那一跳现在转
    ///   `FireTriggerAt`，**事件与正文结算两半都发**。
    ///   🔴 所以「**攻 +1**」那半**现在可达**（铁律 11：可达之后不许再拿「到不了」当理由）——
    ///      但它落成**两条**断言，而不是一条：
    ///      · **解析那半**（本方法开头两条）：`TriggerOps("sacrifice")` 非空、载荷 = `"+1 attack"` ⇒ 已钉；
    ///      · **「正文真的被跑起来」那半**：`S14Log(ctx, "触发 SACRIFICE")` ⇒ 已钉。
    ///      🔴 **「那 +1 落到谁身上」这半【故意不断】**：本轮离线探针**两次读数就不同**
    ///      （一次落到另一个己方单位、一次哪一格都没涨），而 `Core/EffectResolver.cs` 此刻**正被另一个写手改**
    ///      ⇒ 断它 = 把一件**正在变**的事固化成期望（本工程红线）。疑点全文 → 报告 §五。
    ///   ⚠️ **判别力一个字没变**：「**卡上印了正文 / 没印正文**」仍是那一族的判别式 ——
    ///      见 `TestS14SurvivorTriggerAlwaysFires` 的 `#25`（只印 `survivor 3` 那张卡的
    ///      `TriggerOps` **仍返回 `null`**，那一格别动）。
    ///   ② 触发事件本身**不受「有没有正文」影响**（没有正文时走 `FireTriggerAlways` 那一支）
    ///      —— 这正是 `#25` 要钉的那条口径。
    /// </summary>
    static void TestS14SacrificeOnOwnTurnRescue()
    {
        var sacCard = new CardDef("S14_Sac", "S14_Sac", "unit", "",
                                  "common", "Test", 1, 2, 5, 0,
                                  new[] { "survivor 3", "Sacrifice: Gain +1 Attack" }, subtype: "Infantry");
        CheckTrue(sacCard.Has(KeywordTable.Sacrifice), "（前提）卡面（`keywords` 数组）里印着 `Sacrifice`");
        var sacOps = sacCard.TriggerOps(KeywordTable.Sacrifice);
        CheckTrue(sacOps != null && sacOps.Count == 1,
                  "（前提）`Sacrifice:` 那段**正文**收得下来（1 条 op）——"
                + " 2026-10-21 `CardDef.RoutableTriggers` 追加 `sacrifice`（`A1386`/`A1389`）之后才成立"
                + "｜🔴 这**不改**本族的判别力：「没有正文」那一档仍是 `null`，见 `#25` 那格");
        if (sacOps != null && sacOps.Count > 0)
            Check(sacOps[0].Payload, "+1 attack",
                  "（前提）那条 op 就是卡面印的 `Gain +1 Attack`（载荷由 `keywords` 里那段正文原样解析）");

        var hitOwn5 = Tactic("S14_HitOwn5", 0, "Deal 5 damage to a friendly unit");
        var ctx = ProbeBattle(new[] { Unit("S14_Dummy", 1, 0, 9) }, new[] { hitOwn5 });
        ToP1Turn(ctx, 2);
        PassTurn(ctx);                                        // 推到 **P1 的回合**（`ctx.Active == 1`）
        Check(ctx.Active, 1, "（前提）现在是 **P1（= 这个单位的主人）** 的回合"
                           + "（原版那道 `IsPlayerTurn(bm) == card.isPlayer` 靠它成立）");

        var u = Place(ctx, 1, 3, sacCard, exhausted: true);
        u.Health = 2;
        CheckTrue(u.Has(KeywordTable.Sacrifice), "（前提）实例上也有 `sacrifice`（`u.Has` 读的是**当前**关键词）");
        Check(u.Survivor, 3, "（前提）`survivor 3` 也在");
        Check(u.Attack, 2, "（前提）初始攻 2（这张手工夹具卡是 1 费 2/5）");

        ctx.ClearSignals();
        CheckCode(RuleCore.PlayTactic(ctx, 1, HandIdx(ctx, 1, "S14_HitOwn5"), 3), RuleCodes.OK,
                  "本方打自己 5 伤 ⇒ 触发救回那一支");
        Check(u.Health, 3, "★ 救回：生命 = 3");
        CheckTrue(u.Has(KeywordTable.SurvivorSpent), "★ `survivorspent` 挂上");
        Check(SlotOf(ctx, 1, "S14_Sac"), 3, "★ **留场**");
        Check(S14Sig(ctx, EvtKind.Trigger, KeywordTable.Sacrifice), 1,
              "★ **本方回合救回 ⇒ `sacrifice` 触发恰一次**"
            + "｜🧨 删掉 `CleanupDeaths` 里 `if (u.Has(KeywordTable.Sacrifice) && ctx.Active == p)` 那一跳"
            + " ⇒ 本行实得 0");
        Check(S14Sig(ctx, EvtKind.Trigger, KeywordTable.Survivor), 1,
              "★ 幸存者【作为触发 id】那一半也发了一条（`AbilityTrigger.Survivor = 420`，"
            + "原版 `UseSurvivor.c:73`）");
        CheckTrue(S14SigIndex(ctx, EvtKind.Trigger, KeywordTable.Sacrifice)
                  < S14SigIndex(ctx, EvtKind.Trigger, KeywordTable.Survivor),
                  "★ **先后**：`sacrifice` 排在 `survivor` **之前**（原版 `:157` vs `:165` 是写死的次序）"
                + $"｜🧨 把两条对调 ⇒ 本行红（两个计数分别是 "
                + $"{S14SigIndex(ctx, EvtKind.Trigger, KeywordTable.Sacrifice)} / "
                + $"{S14SigIndex(ctx, EvtKind.Trigger, KeywordTable.Survivor)}）");
        CheckTrue(S14Log(ctx, "触发 SACRIFICE"),
                  "★ **表里 `#21` 点名的那半条：`Sacrifice:` 的正文真的被【跑起来了】**——"
                + " `FireTriggerAt` 那一支才会打这行日志（`S14Log` 里那一句），而"
                + "「**没有正文**」那一支（`FireTriggerAlways` 的另一半）**不打日志**、只发事件与广播"
                + " ⇒ 这一格量的就是「那一跳走的是哪一支」"
                + "｜改前 `sacrifice` 不在 `RoutableTriggers` 里 ⇒ 必走「没有正文」分支 ⇒ 本行实得 false"
                + "｜🔴 **为什么不断「那 +1 落到谁身上」**：实测那条 `gain +1 attack` **落点在两个引擎版本上不一样**"
                + "（一次落到另一个己方单位、一次哪一格都没涨），而 `EffectResolver.cs` 此刻**正被另一个写手改**"
                + "（本轮离线探针两次读数就不同）⇒ 断落点 = 把一件**正在变**的事固化成期望（本工程红线）"
                + "。**解析**那半已由本方法开头两条钉死；**落点**那半 → 报告 §五（复刻疑点、待定案）");
    }

    /// <summary>
    /// **`#22` 敌方回合打 5 伤 ⇒ 救回照旧，但 `sacrifice` **不触发**。**
    ///
    /// 判据 = 同一个 `CheckIfDead.c:152-158` 里那条 `IsPlayerTurn(bm) == card.isPlayer`。
    /// 🔴 **把 `ctx.Active == p` 那道闸删掉 ⇒ 本条必红**（`#21` 仍绿）—— 它是这一格的判别式。
    /// </summary>
    static void TestS14SacrificeNotTriggeredOnEnemyTurn()
    {
        var sacCard = new CardDef("S14_Sac2", "S14_Sac2", "unit", "",
                                  "common", "Test", 1, 2, 5, 0,
                                  new[] { "survivor 3", "Sacrifice: Gain +1 Attack" }, subtype: "Infantry");
        var hitEnemy5 = Tactic("S14_HitEnemy5", 0, "Deal 5 damage to an enemy");
        var ctx = ProbeBattle(new[] { hitEnemy5 }, new[] { Unit("S14_Foe", 1, 0, 9) });
        ToP1Turn(ctx, 2);
        Check(ctx.Active, 0, "（前提）现在是 **P0** 的回合（= **不是**那个单位主人的回合）");

        var u = Place(ctx, 1, 3, sacCard, exhausted: true);
        u.Health = 2;
        CheckTrue(u.Has(KeywordTable.Sacrifice), "（前提）`sacrifice` 在身上");
        int ownAtk0 = S14AtkSum(ctx, 1);

        ctx.ClearSignals();
        CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "S14_HitEnemy5"), 3), RuleCodes.OK,
                  "对面的回合打它 5 伤");
        Check(u.Health, 3, "★ **救回照旧**（生命 = 3）—— 献祭那道闸只管事件，不管救不救");
        CheckTrue(u.Has(KeywordTable.SurvivorSpent), "★ `survivorspent` 照旧挂上");
        Check(S14Sig(ctx, EvtKind.Trigger, KeywordTable.Sacrifice), 0,
              "★ **`sacrifice` 一次都不触发**（原版 `IsPlayerTurn(bm) == card.isPlayer` 不成立）"
            + "｜🧨 把 `ctx.Active == p` 那道闸删掉 ⇒ 本行实得 1 ⇒ 红（而 `#21` 仍绿）");
        Check(S14Sig(ctx, EvtKind.Trigger, KeywordTable.Survivor), 1,
              "……而**幸存者那条触发 id 照发**（它没有「本方回合」这道闸）");
        Check(S14AtkSum(ctx, 1) - ownAtk0, 0,
              "★ **那道闸管住的不只是「事件」**：`Sacrifice:` 的正文（`Gain +1 Attack`）**也没有结算**"
            + "（本方棋盘**总攻一点没涨**；对照 `#21` 那一格是 +1）—— 原版那条 "
            + "`IsPlayerTurn(bm) == card.isPlayer` 挡在 `CardScript.TriggerSacrifice(card)` 整跳**之前**，"
            + "事件与正文一起被挡"
            + "｜🧨 把 `ctx.Active == p` 那道闸删掉 ⇒ 本行实得 1（而 `#21` 仍绿）");
    }

    /// <summary>
    /// **`#23` 没有 `survivor`（真死）⇒ 照旧离场、`sacrifice` **一次都不触发**。**
    ///
    /// 判据 = `CheckIfDead.c:151` 那个 `}` —— 献祭那一跳在「`CurrentSurvivor &lt; 1`」的收尾**之后**，
    ///   与「真死 / 变残骸」那两支**互斥**（⛔ 别读成「谁死了谁献祭」）。
    /// 🔴 **它是「位置」的唯一判别式**：把这一跳挪到「真死」那一支 ⇒ 本条必红。
    /// </summary>
    static void TestS14SacrificeNotTriggeredOnRealDeath()
    {
        var sacOnly = new CardDef("S14_Sac3", "S14_Sac3", "unit", "",
                                  "common", "Test", 1, 2, 5, 0,
                                  new[] { "Sacrifice: Gain +1 Attack" }, subtype: "Infantry");
        var hitOwn5 = Tactic("S14_HitOwn5b", 0, "Deal 5 damage to a friendly unit");
        var ctx = ProbeBattle(new[] { Unit("S14_Dummy2", 1, 0, 9) }, new[] { hitOwn5 });
        ToP1Turn(ctx, 2);
        PassTurn(ctx);
        var u = Place(ctx, 1, 3, sacOnly, exhausted: true);
        u.Health = 2;
        CheckTrue(u.Has(KeywordTable.Sacrifice), "（前提）`sacrifice` 在身上");
        Check(u.Survivor, 0, "★ （前提）**没有** `survivor`（这一格要的就是「真死」）");
        Check(ctx.Active, 1, "（前提）而且是**本方**回合 —— 排除「因为不是本方回合才没触发」那种解释");

        ctx.ClearSignals();
        CheckCode(RuleCore.PlayTactic(ctx, 1, HandIdx(ctx, 1, "S14_HitOwn5b"), 3), RuleCodes.OK, "打 5 伤");
        Check(SlotOf(ctx, 1, "S14_Sac3"), -1, "★ **真死 ⇒ 照旧离场**（献祭那一条**救不了人**）");
        Check(S14Sig(ctx, EvtKind.Death), 1, "……发了死亡事件");
        Check(S14Sig(ctx, EvtKind.Trigger, KeywordTable.Sacrifice), 0,
              "★ **`sacrifice` 一次都不触发** —— 它和「真死」那一支互斥"
            + "｜🧨 把 `FireTriggerAlways(Sacrifice)` 那一跳挪到「真死」那支（或挪到幸存者支路之外）"
            + " ⇒ 本行实得 1 ⇒ 红");
    }

    /// <summary>
    /// **`#24` `survivor` 触发 id（有正文那半边）** ＋ **`#25` 「没有正文也要表态」**。
    ///
    /// 判据 = 原版 `CardScript__UseSurvivor.c:73-75 RawCardScript.OnTrigger(raw, 0x1a4, …)`
    ///   （`0x1a4 = AbilityTrigger.Survivor = 420`），它就在 `UseSurvivor` **内部**、
    ///   排在 `:71 AddTraitSilently(0x1b8 /*survivorSpent*/)` **之后**
    ///   ⇒ 触发那一刻 `survivor` 已摘、`survivorspent` 已挂。
    ///   ⚠️ **别写成 `survivorspent` 是触发 id** —— `AbilityTrigger.SurvivorSpent = 422` 全量反编译 0 命中
    ///   （`KeywordTable.SurvivorSpent` 的 doc 里已就地订正过这一条）。
    ///
    /// 🔴 **`#25` 的判别式 = `FireTriggerAlways` vs `FireTriggerAt`**：
    ///   这两条路**唯一的差别**就是「卡上没有这条正文时发不发事件」——
    ///   `FireTriggerAt` 在没正文时会**提前 `return`、连 `EvtKind.Trigger` 都不发**。
    ///   原版那两族触发是**无条件表态**的（`DisplayTriggerAnim` / `HighlightTraitIcon` 在
    ///   「这张卡有没有那条 ability」**之外**，见 `RuleCore.FireTriggerAlways` 的 doc）
    ///   ⇒ 拿一张**只印 `Survivor 3`、不带 `Survivor:` 正文**的卡：`Trigger{survivor}` 必须还在。
    ///
    /// ✅ **2026-10-21 就地订正（铁律 5）：本段原来写着「`Survivor:` 的正文收不下来、那半句到不了」—— 已过期。**
    ///   另一个写手（`A1386` / `A1389`）把 `CardDef.RoutableTriggers` **和** `BodyKeywords`
    ///   各追加了 `Survivor` / `Sacrifice` ⇒ 实测 `both.TriggerOps("survivor")` = 1 条 op
    ///   （`gain` / `payload = "+2 attack"` / 目标 `own/unit`）、端到端 `u.Attack` **2 → 4**
    ///   ⇒ 表里 `#24` 点名的那半条「**攻 +2**」**现在可达、已补断言**（铁律 11）。
    ///   下面两格「**有正文 / 无正文两档都发事件**」的口径一个字没变 ——
    ///   而且现在它多了一条数值侧的对照：`#25` 那张**没有**正文的卡，攻**照旧不变**。
    /// 🔴 **本方法另外补了两格**（`#24` 那半条点名要的）：
    ///   · `#24b` —— **只**印 `Survivor: Gain +2 Attack` + `survivor 3` 的那张卡：正文**确实落在
    ///     「被救回的那个单位自己」身上**（攻 2 → 4）。单开它的理由写在那一格的注释里
    ///     （`#24` 那张卡**同时**带 `Sacrifice:` 正文，而它那份 `+1` 的落点今天**不稳**）。
    ///   · `#24c` —— **配套项**：`desc` 写成一句两段（`Rally: … . Survivor: …`）时，
    ///     `Survivor:` 那一段**不会**被并进 `Rally` 的正文（那靠 `CardDef.BodyKeywords`）。
    /// </summary>
    static void TestS14SurvivorTriggerAlwaysFires()
    {
        // ---- #24 有正文（同时带 sacrifice 的正文，做先后）----
        {
            var both = new CardDef("S14_SvBoth", "S14_SvBoth", "unit", "",
                                   "common", "Test", 1, 2, 5, 0,
                                   // ✅ **2026-10-21 就地订正（铁律 5）—— 本段原来那句「顺序是刻意的」已过期**：
                                   //    原文写「`survivor 3` 必须排在**最后**，否则 `"Survivor: Gain +2 Attack"`
                                   //    用 `HeadOf` 取到的头是 `Survivor`、兜底值 **1** 会把 `survivor` 冲成 1
                                   //    （实测救回后生命是 1 而不是 3），所以改用「值写在最后」的摆法」。
                                   //    另一个写手当天给 `Parse` 加了 `numSeen`（**同名里印了数字的那一条
                                   //    压过没印数字的**）⇒ **两种摆法现在都是 3**（本轮离线探针两种顺序各量过一遍）。
                                   //    ⇒ 这个「值写在最后」的摆法**保留**（无害、也与真卡写法一致），
                                   //    但那句「必须」不再成立 —— ⛔ 别再照它推出「顺序会改数值」。
                                   new[] { "Sacrifice: Gain +1 Attack", "Survivor: Gain +2 Attack", "survivor 3" },
                                   subtype: "Infantry");
            CheckTrue(both.Has(KeywordTable.Survivor), "（前提）印着 `survivor`");
            var hitOwn = Tactic("S14_HitOwn5c", 0, "Deal 5 damage to a friendly unit");
            var ctx = ProbeBattle(new[] { Unit("S14_Dummy3", 1, 0, 9) }, new[] { hitOwn });
            ToP1Turn(ctx, 2);
            PassTurn(ctx);
            var u = Place(ctx, 1, 3, both, exhausted: true);
            u.Health = 2;
            Check(u.Attack, 2, "（前提）初始攻 2（这张手工夹具卡是 1 费 2/5）");
            ctx.ClearSignals();
            CheckCode(RuleCore.PlayTactic(ctx, 1, HandIdx(ctx, 1, "S14_HitOwn5c"), 3), RuleCodes.OK, "本方打 5 伤");

            Check(S14Sig(ctx, EvtKind.Trigger, KeywordTable.Survivor), 1,
                  "★ **`EvtKind.Trigger{Keyword == \"survivor\"}` 恰一条**（原版 `UseSurvivor.c:73` 那一跳）"
                + "｜🧨 把 `RuleCore.CleanupDeaths` 末尾那条 `FireTriggerAlways(… Survivor …)` 删掉"
                + " ⇒ 本行实得 0，而 `#21`/`#22`/`#23` 三格**仍全绿**");
            Check(S14Sig(ctx, EvtKind.Trigger, KeywordTable.Sacrifice), 1, "……`sacrifice` 那条也在");
            CheckTrue(S14SigIndex(ctx, EvtKind.Trigger, KeywordTable.Sacrifice)
                      < S14SigIndex(ctx, EvtKind.Trigger, KeywordTable.Survivor),
                      "★ 次序仍是 `sacrifice` → `survivor`");
            Check(u.Health, 3, "★ 救回（生命 3）");
            CheckTrue(S14Log(ctx, "触发 SACRIFICE") && S14Log(ctx, "触发 SURVIVOR"),
                      "★ **两条触发的正文都被【跑起来了】**（`FireTriggerAt` 那一支才打这两行日志；"
                    + "「没有正文」那一支不打日志、只发事件与广播）"
                    + "｜改前 `survivor` / `sacrifice` 都不在 `RoutableTriggers` 里 ⇒ 两跳都必走"
                    + "「没有正文」分支 ⇒ 本行实得 false"
                    + "｜🔴 **数值那半不在这里断**：`#24` 这张卡**同时**带 `Sacrifice:` 正文，"
                    + "而它那份 `+1` 的**落点今天不稳**（见 `#21` 那格的说明）⇒ "
                    + "「+2 落在被救回单位自己身上」改由下面 `#24b` 那张**没有** `Sacrifice:` 正文的卡断");
            CheckTrue(!u.Has(KeywordTable.Survivor) && u.Has(KeywordTable.SurvivorSpent),
                      "★ 触发那一刻 `survivor` 已摘、`survivorspent` 已挂（原版顺序：`:71` 在 `:73` 之前）");
        }

        // ---- #25 只印 `Survivor 3`、**没有**正文 ----
        {
            var bare = new CardDef("S14_SvBare", "S14_SvBare", "unit", "",
                                   "common", "Test", 1, 2, 5, 0,
                                   new[] { "survivor 3" }, subtype: "Infantry");
            Check(bare.TriggerOps(KeywordTable.Survivor), null,
                  "★ （关键前提）这张卡上**没有** `Survivor:` 的正文（`TriggerOps` 返回 `null`）——"
                + " 不成立的话下面那一格就退化成「有正文才发」");
            var hitOwn = Tactic("S14_HitOwn5d", 0, "Deal 5 damage to a friendly unit");
            var ctx = ProbeBattle(new[] { Unit("S14_Dummy4", 1, 0, 9) }, new[] { hitOwn });
            ToP1Turn(ctx, 2);
            PassTurn(ctx);                                        // 推到 P1 的回合（同 #21 那一格）
            var u = Place(ctx, 1, 3, bare, exhausted: true);
            u.Health = 2;
            ctx.ClearSignals();
            CheckCode(RuleCore.PlayTactic(ctx, 1, HandIdx(ctx, 1, "S14_HitOwn5d"), 3),
                      RuleCodes.OK,
                      "**没有** `Survivor:` 正文的那一份，照样救回（下面量事件）");
            Check(S14Sig(ctx, EvtKind.Trigger, KeywordTable.Survivor), 1,
                  "★ **没有正文也要表态**：`Trigger{survivor}` **仍然有一条** ——"
                + " 判据 = 原版 `UseSurvivor.c:43-46` 的 `HighlightTraitIcon` + `DisplayTriggerAnim`"
                + " 在「有没有那条 ability」**之外**"
                + "｜🧨 把那两处 `FireTriggerAlways` 换回 `FireTriggerAt` ⇒ 本行实得 0（没正文时它连事件都不发）"
                + "，而 `#24`（有正文）**两边都绿**");
            Check(u.Health, 3, "……救回也照旧");
            Check(u.Attack, 2,
                  "★ **「有没有正文」那一格的数值侧对照**：这张卡**没有** `Survivor:` 正文 ⇒ "
                + "幸存者那半**什么都不结算**（攻照旧 2；对照 = 下面 `#24b` 那格有正文的实得 4）——"
                + " 与上面那条**计数**合起来 = 「**事件照发、正文按卡面有无**」两半都钉住了"
                + "｜🧨 把「有正文」那半改成无条件补一份正文 ⇒ 本行实得 4");
        }

        // ---- `#24b` **有正文那半的数值侧**：只印 `Survivor: Gain +2 Attack` + `survivor 3` ----
        // 🔴 **单开这一格的理由**：`#24` 那张卡**同时**带 `Sacrifice:` 正文，而它那份 `+1` 今天落到
        //    **另一个己方单位**身上（同 `#21` 的疑点）⇒ 在那一格上**断不了「+2 落在被救回的那个单位身上」**。
        //    这一格**没有** `Sacrifice:` 正文 ⇒ 落点没有第二种解释，断的就是「正文真的落到它自己身上」。
        {
            var svOnly = new CardDef("S14_SvOnly", "S14_SvOnly", "unit", "",
                                     "common", "Test", 1, 2, 5, 0,
                                     new[] { "Survivor: Gain +2 Attack", "survivor 3" }, subtype: "Infantry");
            CheckTrue(svOnly.TriggerOps(KeywordTable.Survivor) != null,
                      "（前提）`Survivor:` 的正文收得下来（= 与上一格那张 `bare` 的**唯一差别**）");
            var hitOwn = Tactic("S14_HitOwn5e", 0, "Deal 5 damage to a friendly unit");
            var ctx = ProbeBattle(new[] { Unit("S14_Dummy5", 1, 0, 9) }, new[] { hitOwn });
            ToP1Turn(ctx, 2);
            PassTurn(ctx);
            var u = Place(ctx, 1, 3, svOnly, exhausted: true);
            u.Health = 2;
            ctx.ClearSignals();
            CheckCode(RuleCore.PlayTactic(ctx, 1, HandIdx(ctx, 1, "S14_HitOwn5e"), 3), RuleCodes.OK,
                      "本方打 5 伤 ⇒ 救回 + 触发 `Survivor:` 正文");
            Check(u.Attack, 4,
                  "★ **`Survivor: Gain +2 Attack` 落在了被救回的那个单位【自己】身上**（攻 2 → 4）——"
                + " 原版 `UseSurvivor.c:73-75 RawCardScript.OnTrigger(raw, 0x1a4)` 的 ops 就是按"
                + "「**触发者自己**」结算的 ⇒ 这一格量的是那条语义本身"
                + "｜改前 `survivor` 不在 `RoutableTriggers` 里 ⇒ 那一跳必走「没有正文」分支"
                + "（只发事件、不结算）⇒ 本行实得 2");
        }

        // ---- `#24c` **配套项（`CardDef.BodyKeywords` 那半边）**：`desc` 里两段触发的断句 ----
        // 🔴 卡面 `Rally: … . Survivor: …` 那种「**一句里两段触发**」的形状：`Survivor:` 那一段
        //    会不会被**并进 `Rally` 的正文**，只取决于 `survivor` **在不在 `BodyKeywords` 里** ——
        //    进了 `RoutableTriggers` 但**没**进 `BodyKeywords` 时，断句那一跳
        //    （`TriggerBodyAt` / `StartsAnotherThing` 靠 `IsBodyKeyword` 判）就不认它是新的一段
        //    ⇒ `Rally` 把整句吞掉（实测：`Rally` 收成 **2** 条 op、`TriggerText("rally")` 也带上后半句）。
        //    ⚠️ 这是把「两张表**配套**」这件事钉住的那一格（只补一张 = 静默多收一段）。
        {
            var twoSeg = new CardDef("S14_TwoSeg", "S14_TwoSeg", "unit",
                                     "Rally: Deal 3 damage to an enemy. Survivor: Gain +2 Attack",
                                     "common", "Test", 1, 2, 5, 0, new[] { "survivor 3" }, subtype: "Infantry");
            var rallyOps = twoSeg.TriggerOps(KeywordTable.Rally);
            CheckTrue(rallyOps != null && rallyOps.Count == 1,
                      "★ **`Rally:` 那一段只收它自己那条 op**（1 条，⛔ 不是 2 条）——"
                    + " `survivor` 进了 `RoutableTriggers` 却**没**进 `BodyKeywords` 时本行实得 2");
            Check(twoSeg.TriggerText(KeywordTable.Rally), "Deal 3 damage to an enemy",
                  "★ ……而 `Rally` 的**正文原文**也只到句号为止（⛔ 不带上后半句 `Survivor: Gain +2 Attack`）"
                + "｜🧨 把 `CardDef.BodyKeywords` 末尾那两个词去掉 ⇒ 本行与上一行**同时**红");
            var svOps = twoSeg.TriggerOps(KeywordTable.Survivor);
            CheckTrue(svOps != null && svOps.Count == 1,
                      "★ **后半句归 `Survivor`**（1 条 op）—— 与上面两行合起来才是「**断句真的断对了**」"
                    + "（只断「`Rally` 只有 1 条」的话，「两段谁都没收」也绿）");
            if (svOps != null && svOps.Count > 0)
                Check(svOps[0].Payload, "+2 attack", "……而且收的就是 `Gain +2 Attack` 那条");
        }
    }

    // ==================================================================
    //  #26 —— `IsAlive` 一族 / `WouldKillByEntries`
    // ==================================================================

    /// <summary>
    /// **`#26` `WouldKillByEntries` 单独按 `WithDamageValues` 补 `Survivor` + `Bastion`。**
    ///
    /// 判据 = 原版 `CardScript__EnoughPendingDamageToDieWithDamageValues.c`（逐句）：
    ///   · `:115-260` 是个**循环**（**原版就是这样**，不是「先求和再减」）；
    ///   · `:161/206` `CurrentSurvivor >= 1` 且堡垒层还没被打穿时，累计 ≥ `生命 + 堡垒`
    ///     ⇒ **这一层清零、计数重来**（`iVar5 = 0`，`bVar2`）；
    ///   · `:231-248` 之后把**幸存者**当**第二层屏障**：累计 ≥ `survivor` ⇒ `return 1`（判死）；
    ///   · `:206-210` `CurrentSurvivor &lt; 1` ⇒ 唯一屏障 = `生命 + 堡垒`。
    ///   外层闸 = `CardScript__EnoughPendingDamageToDie.c:41-45`
    ///   `if ((0 &lt; health) || (CurrentSurvivor != 0))`（否则落到末尾 `return 1`）。
    ///   ⚠️ `dropPod` **不在这一口里**（`WouldKillByEntries` 照原版也不读它）—— ⛔ 别顺手加。
    ///
    /// ⚠️ **规格表那句「`IsAlive` 一族改成 `Health &gt; 0 || CurrentSurvivor &gt; 0`」——本文件不落。**
    ///   理由：`UnitState.IsAlive` 现在是 `Health &gt; 0`（**只读血量**），而 `RuleCore.CleanupDeaths`
    ///   正是靠 `if (u == null || u.IsAlive) return;` **进**幸存者那一支的 —— 把它改成
    ///   「有幸存者也算活着」，幸存者支路会**整段变成死代码**（`#1` 立刻红，而且救回这件事再也不发生）。
    ///   ⇒ 那句正确读法是**「判死那一家」（`EnoughPendingDamageToDie` / `WouldKillByEntries`）**，
    ///   而它们**已经**是 `health &gt; 0 || CurrentSurvivor != 0` 的形状 ⇒ 下面直接断它的**行为**。
    ///   「`IsAlive` 字面上改不改」＝ **待调度台定**（要动 `UnitState`，是引擎源码，⛔ 本轮不在白名单里）。
    /// </summary>
    static void TestS14WouldKillByEntries()
    {
        // 尺子先活着：裸 5 血，5 点判死、4 点不判死
        var plain = new UnitState(Unit("S14_WK", 1, 1, 5), false);
        CheckTrue(RuleCore.WouldKillByEntries(plain, new List<int> { 5 }), "（尺子）裸 5 血：一条 5 点 ⇒ 判死");
        CheckTrue(!RuleCore.WouldKillByEntries(plain, new List<int> { 4 }), "（尺子）一条 4 点 ⇒ 不判死");

        // 堡垒：当**额外血量**与生命相加（:206-210 那一支）
        var bas = new UnitState(Unit("S14_WKB", 1, 1, 5), false);
        bas.AddKeyword(KeywordTable.Bastion, 3);
        CheckTrue(!RuleCore.WouldKillByEntries(bas, new List<int> { 7 }),
                  "★ **堡垒 3 补进来了**：7 &lt; 生命 5 + 堡垒 3 ⇒ 不判死"
                + "｜🧨 删掉 `WouldKillByEntries` 里 `int bastion = u.Bastion;` 那一路 ⇒ 本行实得 true");
        CheckTrue(RuleCore.WouldKillByEntries(bas, new List<int> { 8 }),
                  "★ 8 ≥ 5 + 3 ⇒ 判死（判据是**两层的和**，不是各判各的）");

        // 幸存者：**第二层屏障**（:231-248）—— 逐条累计，把第一层打穿之后**清零重来**
        var sv = new UnitState(Unit("S14_WKS", 1, 1, 5), false);
        sv.Health = 2;
        sv.AddKeyword(KeywordTable.Survivor, 3);
        CheckTrue(!RuleCore.EnoughPendingDamageToDie(sv),
                  "★ 血 ≤0 的那一档才有意义 —— 这里先钉**外层闸的反面**：血 2 &gt; 0、有幸存者 ⇒ **不算将死**"
                + "（原版 `EnoughPendingDamageToDie.c:41-45` 的 `(0 &lt; health) || (CurrentSurvivor != 0)`）");
        CheckTrue(!RuleCore.WouldKillByEntries(sv, new List<int> { 2, 2 }),
                  "★ **幸存者 3 是第二层**：`[2,2]` 只打穿第一层（生命 2）⇒ 不判死");
        CheckTrue(RuleCore.WouldKillByEntries(sv, new List<int> { 2, 3 }),
                  "★ `[2,3]`：先打穿生命那一层、再打穿幸存者那一层 ⇒ **判死**"
                + "｜🧨 删掉幸存者那第二层 ⇒ 本行实得 false（而上面 `[2,2]` 仍绿）");

        // 外层闸：血 ≤0 且无幸存者 ⇒ 直接判死（原版那个 `return 1`）
        var deadish = new UnitState(Unit("S14_WKD", 1, 1, 5), false);
        deadish.Health = 0;
        CheckTrue(RuleCore.EnoughPendingDamageToDie(deadish),
                  "★ 血 ≤0 且**没有**幸存者 ⇒ 将死（原版 `:41-45` 为假 ⇒ 落到函数末尾的 `return 1`）");
        Check(RuleCore.WouldKillByEntries(deadish, new List<int>()), true,
              "★ 而且**空条目也判死**（原版 `:63-79` 那道守卫为假 ⇒ 直接 `return 1`，⛔ 不看 entries）");
        Check(RuleCore.WouldKillByEntries(plain, new List<int>()), false,
              "……反面：活着的单位 + 空条目 ⇒ 不判死（原版 `:80` 的 `list.Count == 0 ⇒ return 0`）");
        Check(RuleCore.WouldKillByEntries(null, new List<int> { 99 }), false, "……`u == null` ⇒ 不判死（不静默抛）");
    }

    // ==================================================================
    //  #28 —— 同一份实例不会同时在 `Hand` 与 `Board`
    // ==================================================================

    /// <summary>
    /// **`#28`（可选运行期旁证）同一份 `CardInstance` 不会同时在 `Hand` 与 `Board`。**
    ///
    /// 规格表标着「报告未给灭自证（结构性排除已足够）」—— 这一格量的是**不变量**，
    /// 它在「出牌 = 搬视图还是重建视图」那类改动上会立刻响（本工程踩过：出牌是搬视图，
    /// 于是同一个实例同时挂在两处而没人报错）。
    /// </summary>
    static void TestS14HandAndBoardInstancesDisjoint()
    {
        var ctx = Battle(new[] { Unit("S14_H1", 1, 1, 5), Unit("S14_H2", 1, 1, 5) },
                         new[] { Unit("S14_HFoe", 1, 1, 5) });
        ToP1Turn(ctx, 3);
        Place(ctx, 0, 3, Unit("S14_B1", 1, 1, 5));            // 场上也摆一个（两边都非空）

        // ⚠️ **按引用比**（`object.ReferenceEquals`），不按 `HashSet` 的默认相等性 ——
        //    万一 `CardInstance` 哪天覆写了 `Equals`（按卡模板相等），这一条会**静默变松**。
        var handInsts = new List<CardInstance>();
        for (int p = 0; p < 2; p++)
            foreach (var h in ctx.Players[p].Hand)
                if (h != null) handInsts.Add(h);
        CheckTrue(handInsts.Count > 0, "（前提）手里至少有一份实例（否则这一条退化成恒真）");

        int both = 0;
        for (int p = 0; p < 2; p++)
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var b = ctx.Players[p].Board[s];
                if (b == null || b.Instance == null) continue;
                foreach (var h in handInsts)
                    if (object.ReferenceEquals(h, b.Instance)) { both++; break; }
            }
        Check(both, 0,
              "★ **同一份实例不同时在 `Hand` 与 `Board`** （出牌 = **搬**，不是「两边各留一份」）"
            + $"｜（本格扫的是双方棋盘 {BoardSpec.Size}×2 格 + 两手牌）");
    }

    // ==================================================================
    //  #33 / #34 / #35 / #36 —— 代词目标的侧（`PrevSide` / `ResolvedSide`）与解析幂等
    // ==================================================================

    /// <summary>
    /// **`#33` 代词目标的侧能从先行词抄到**：
    /// `Parse("Take control of an enemy troop this turn and give it Fast")`
    ///   ⇒ `ops.Count == 2` · `ops[0].Target.Side == "enemy"` ·
    ///      `ops[1].Target.Side == "prev"` · `Kind == "prev"` ·
    ///      **`PrevSide == "enemy"`** · **`ResolvedSide == "enemy"`**。
    ///
    /// 判据 = 原版那两条路各要什么（见 `EffectTargetSpec.PrevSide` / `ResolvedSide` 的 doc）：
    ///   · `Side == "prev"` 是**结算层的路由键**（`EffectResolver.ResolveTargets` 见到它就取
    ///     `LastTargets` / `LastTarget`）⇒ ⛔ **不许把它覆写成 `enemy`**（那两路会静默失效）；
    ///   · 侧**另开一栏** `PrevSide`，由 `EffectText.LinkPrevAntecedent` 从**紧邻上一条 op**
    ///     明写的侧抄来 ⇒ `ResolvedSide` 回落到它。
    ///   全池唯一带 `Fast` 的战术卡 = `Telephatic Domination`；`SimpleAI.ScoreOp` 是今天唯一消费者。
    ///
    /// 🔴 **灭自证**：同时断 `ops[0].Target.Side == "enemy"`（**先行词本身**）——
    ///   若 `PrevSide` 改成从别处取（例如恒取 `own`），这一对**不可能同时绿**。
    /// </summary>
    static void TestS14PronounTargetSide()
    {
        List<string> un, pa;
        var ops = EffectText.Parse("Take control of an enemy troop this turn and give it Fast", out un, out pa);
        Check(ops.Count, 2, "★ 恰好 2 条 op（`takecontrol` + 代词那条 `give`）");
        Check(ops[0].Target.Side, "enemy", "★ **先行词本身**：`an enemy troop`");
        Check(ops[1].Target.Side, "prev",
              "★ 代词那条的 `Side` **仍是 `prev`**（= 结算层的路由键，⛔ 别改它；"
            + "改了 `ResolveTargets` / `DoGive` 那两路会**静默失效**）");
        Check(ops[1].Target.Kind, "prev", "★ `Kind` 也是 `prev`");
        Check(ops[1].Target.PrevSide, "enemy",
              "★ **`PrevSide == \"enemy\"`** —— 从紧邻上一条 op 明写的侧抄来的"
            + "｜🧨 让 `LinkPrevAntecedent` 恒填 `\"own\"` ⇒ 本行与上面「先行词是 enemy」那一行"
            + "**不可能同时绿**");
        Check(ops[1].Target.ResolvedSide, "enemy",
              "★ `ResolvedSide`（代词 ⇒ 回落 `PrevSide`）也是 `enemy` ⇒ AI 那条打分会给分");
    }

    /// <summary>
    /// **`#34` 解析层反面：判不出侧就**不许编一个**。**
    /// `Parse("Draw a troop and give it +1 [Attack]")` ⇒ `ops[1].Target.Side == "prev"`
    /// **且 `PrevSide` 是「判不出来」**。
    ///
    /// 全池同形还有 `SW13` / `UM23` / `TAU54` / `GOF50`（真卡 `GOF_Da_Red_Waaagh` 即这一句）。
    /// ⚠️ **实测**：`PrevSide` 与 `ResolvedSide` 都是 **`null`**（不是空串、更不是某个侧）——
    ///   断言用 `string.IsNullOrEmpty` 兜住两种「没值」的写法，消息里写明实测是 `null`。
    /// 🔴 与 `#33` **并排**：「一切代词都填 `own`」的实现在这一行红。
    /// </summary>
    static void TestS14PronounSideUnknownStaysUnknown()
    {
        List<string> un, pa;
        var ops = EffectText.Parse("Draw a troop and give it +1 [Attack]", out un, out pa);
        Check(ops.Count, 2, "★ 恰好 2 条 op（`drawtype` + 代词那条 `give`）");
        Check(ops[1].Target.Side, "prev", "★ 代词那条仍然是 `prev`（结算层要按 `LastTargets` 走）");
        CheckTrue(string.IsNullOrEmpty(ops[1].Target.PrevSide),
                  "★ **`PrevSide` 判不出来**（实测 `null`）—— 上一条 op 是 `drawtype`、**没写侧**"
                + " ⇒ ⛔ 不许拿「大概是自己」顶上"
                + "｜🧨 写成「一切代词都填 `own`」⇒ 本行红（`#33` 那格仍绿）");
        CheckTrue(string.IsNullOrEmpty(ops[1].Target.ResolvedSide),
                  "★ 所以 `ResolvedSide` 也判不出来 ⇒ AI 那条路按**不给分**处理"
                + "（`SimpleAI.ScoreGivingCharge`：`side != \"own\" && side != \"enemy\"` ⇒ 0）");
    }

    /// <summary>
    /// **`#35` 解析层连环顺延**：`Parse("Deal 1 damage to a friendly troop and give it Armour 1.
    /// 2 : Give it Armour 2 instead")` ⇒ `ops[1].PrevSide == "own"` **且** `ops[2].PrevSide == "own"`。
    ///
    /// 判据 = `EffectText.LinkPrevAntecedent` 的两条规则：① 抄**紧邻上一条**明写的侧；
    ///   ② 上一条自己也是代词时，**顺延它已经抄到的那个**（`Destroy an enemy troop. Stun it and
    ///   give it Fast` 那种连环 —— 同一个先行词，不是新猜一个；全池实测 3 张）。
    /// 真卡 = `SOR49 Trial of Suffering`（这条字符串就是它的 `desc` 原文）。
    ///
    /// 🔴 **灭自证**：去掉「顺延」那一句 ⇒ `ops[2]` 变「判不出来」⇒ 本行红，
    ///   而 `ops[1]` 那一行**仍绿**（能分清是哪半坏了）。
    /// </summary>
    static void TestS14PronounSideChains()
    {
        List<string> un, pa;
        var ops = EffectText.Parse(
            "Deal 1 damage to a friendly troop and give it Armour 1. 2 : Give it Armour 2 instead",
            out un, out pa);
        Check(ops.Count, 3, "★ 恰好 3 条 op");
        Check(ops[1].Target.PrevSide, "own", "★ `ops[1]` 从 `a friendly troop` 抄到 `own`");
        Check(ops[2].Target.PrevSide, "own",
              "★ **`ops[2]` 顺延到 `own`** —— 它上一条自己是代词，就把**已经抄到的那个**接下去"
            + "（不是新猜一个）"
            + "｜🧨 去掉 `LinkPrevAntecedent` 里那句顺延 ⇒ 本行变「判不出来」⇒ 红，"
            + "而上面 `ops[1]` 那一行**仍绿**（能分清是哪半坏了）");
        Check(ops[1].Target.ResolvedSide, "own", "★ `ops[1]` 的 `ResolvedSide` 也是 `own`");
        Check(ops[2].Target.ResolvedSide, "own", "★ `ops[2]` 同理");
    }

    /// <summary>
    /// **`#36` 解析层**幂等**：同一份 `desc` 连跑 3 次，逐 op 的 `PrevSide` / `Side` / `Kind` 三次一致。**
    ///
    /// 为什么单开这一条：`Parse` 里有**几处「回填」**（`LinkPrevAntecedent` 的代词侧、
    /// `FillAdjacentAnchors` 的相邻锚点、`repeat` / `paidmod` 的 `BaseOps` 递归）——
    /// 它们**读的是前面已经算出来的 op**，而 `Parse` 会被同一份 `desc` 反复调用
    /// （`CardDef` 的构造、`WillRunOps`、`Coverage` 都调）⇒ 一旦某处回填不幂等，
    /// **第二次解析的结果就和第一次不同**，而这件事**不报错、不自检红**（静默）。
    /// 规格表标着「灭自证：报告未给（一红就是**静默错**）」，本次实测这 5 条全池形状都幂等。
    ///
    /// 取样的 5 条：`#33`/`#34`/`#35` 那三条 ＋ `UM81` 的跨句承接 ＋ `UM93` 的付费前缀。
    /// </summary>
    static void TestS14ParseIsIdempotent()
    {
        string[] descs = {
            "Take control of an enemy troop this turn and give it Fast",
            "Draw a troop and give it +1 [Attack]",
            "Deal 1 damage to a friendly troop and give it Armour 1. 2 : Give it Armour 2 instead",
            "Oath 3: Deal 2-4 damage to an enemy. If target dies, gain Shield",
            "Return a friendly troop to your hand. Oath 2: Lower its cost by 2 and give it Flank.",
        };
        foreach (string desc in descs)
        {
            List<string> un, pa;
            string s1 = S14Shape(EffectText.Parse(desc, out un, out pa));
            string s2 = S14Shape(EffectText.Parse(desc, out un, out pa));
            string s3 = S14Shape(EffectText.Parse(desc, out un, out pa));
            CheckTrue(s1 == s2 && s2 == s3,
                      "★ **幂等**：连跑 3 次逐 op 一致 —— 「" + desc + "」"
                    + "｜🧨 任一处回填（代词侧 / 相邻锚点 / `repeat` 的 `BaseOps`）不幂等 ⇒ 本行红"
                    + (s1 == s2 && s2 == s3 ? "" :
                       "\n      1 次：" + s1 + "\n      2 次：" + s2 + "\n      3 次：" + s3));
        }
    }

    // ==================================================================
    //  #37 / #38 —— AI 打点：`ScoreGivingCharge` 的「侧」
    // ==================================================================

    /// <summary>
    /// **`#37` 打分层**：`ScoreGivingCharge(ctx, 0, 3, "enemy") == 7f` ·
    ///   `(…, "own") == 4f` · `ScoreOps(ctx, ops, 3, slotIsTarget: true) − PayloadScore("fast") == 7f`。
    /// **`#38` 判不出侧就不给分**：`(…, "any") == 0f` · `(…, null) == 0f`；**反面** `(…, "own") == 4f`（不是 0）。
    ///
    /// 判据 = 原版 `AI__ScoreFromGivingCharge.c:11-14`（与 `AI__ScoreFromCriteria.c:297-300` 的
    ///   `case 0x1e = ScoringCriteria.ScoringOption.SumValueOfGivingCharge = 30` **同形**）：
    ///   先 `CanActNow`、**为假【再】**读裸 `+0x58`（`summonSickness`）⇒ 取 `CurrentMeleeAttack`。
    ///   我们的落点 = `SimpleAI.ScoreGivingCharge` 的 4 参重载 ＋ `ScoreOp` 的 `give` 支
    ///   （侧取 `EffectTargetSpec.ResolvedSide`，判据只此一处）。
    ///
    /// 🔴 **「同一格号、两边值不同（7 vs 4）」就是判别式** —— 两个单位都摆**同一格号 3**、
    ///   两个都是「不能动 + 带召唤病」，**只有攻击力不同**（我方 4 / 敌方 7）⇒
    ///   「读错侧」与「读 `me` 棋盘」两种错法都必红。
    ///   ⛔ 别把两个单位放成同一个 `attack`（那会退化成恒真）。
    ///
    /// `#39`（3 参重载那两格：`(ctx,0,3) == 4f` 与 `ScoreOps` 含 `give fast` 那一档）
    /// **已有** = `TestA1098SummonSicknessReads` 的 Ⓒ ⇒ 本文件**不写第二份**；
    /// 但那一格缺的「`"any"` ⇒ 0」那半**就是 `#38`**，收在这里。
    /// </summary>
    static void TestS14ScoreGivingChargeSide()
    {
        var ctx = Battle(new[] { Unit("S14_Filler", 1, 1, 1) }, new[] { Unit("S14_Foe", 1, 0, 9) });
        ToP1Turn(ctx, 1);
        Check(ctx.Active, 0, "（前提）本机方行动");
        var mine = Place(ctx, 0, 3, Unit("S14_Charge", 1, 4, 5), exhausted: true);
        mine.SummonSickness = true;
        var foe = Place(ctx, 1, 3, Unit("S14_ChargeFoe", 1, 7, 5), exhausted: true);
        foe.SummonSickness = true;
        Check(mine.Attack, 4, "（前提）我方 3 号格上那个 4 攻");
        Check(foe.Attack, 7, "（前提）敌方 3 号格上那个 7 攻 —— **同一个格号**");
        CheckTrue(mine.Exhausted && mine.SummonSickness && foe.Exhausted && foe.SummonSickness,
                  "（前提）两边都「不能动 + 带召唤病」（原版 `CanActNow` 假 ⇒ 才往下读 `+0x58`）");

        Check(SimpleAI.ScoreGivingCharge(ctx, 0, 3, "enemy"), 7f,
              "★ `\"enemy\"` ⇒ 读**敌方**那一侧同格号上的单位（7）");
        Check(SimpleAI.ScoreGivingCharge(ctx, 0, 3, "own"), 4f,
              "★ `\"own\"` ⇒ 读**我方**那一侧同格号上的单位（4）—— **同一格号、两边值不同**，这就是判别式");
        Check(SimpleAI.ScoreGivingCharge(ctx, 0, 3, "any"), 0f,
              "★ `#38` **判不出侧（`any`）⇒ 不给分**"
            + "｜🧨 回落到「就当是自己人」⇒ 本行实得 4 ⇒ 红");
        Check(SimpleAI.ScoreGivingCharge(ctx, 0, 3, null), 0f, "★ `#38` 同理：侧是 `null` 也不给分");
        Check(SimpleAI.ScoreGivingCharge(ctx, 0, 3, "own"), 4f,
              "★ `#38` 的反面：**`\"own\"` 不是 0** —— 它挡的是「一律返 0」把上面两条改成恒真");

        // 接线：`give fast` 那条 op 经 `ScoreOps` 时，侧走的是 `ResolvedSide`
        List<string> un, pa;
        var parsed = EffectText.Parse("Take control of an enemy troop this turn and give it Fast",
                                      out un, out pa);
        Check(parsed[1].Target.ResolvedSide, "enemy", "（前提）那条 `give fast` op 的侧判得出来");
        var giveFast = new List<EffectOp> { parsed[1] };
        float sc = SimpleAI.ScoreOps(ctx, giveFast, 3, slotIsTarget: true);
        Check(SimpleAI.PayloadScore("fast"), 0f,
              "（前提）`fast` 的载荷分是 0 ⇒ 下面那一减等于读原值（写成减法是为了**不依赖**这个前提）");
        Check(sc - SimpleAI.PayloadScore("fast"), 7f,
              "★ `ScoreOps` 那一档加的是 **敌方那 7 分**（侧从 `ResolvedSide` 取，"
            + "⛔ 不是「硬编码成 `me`」）"
            + "｜🧨 把 `ScoreOp` 的 `give` 支改回 `ScoreGivingCharge(ctx, me, slot)`（两参侧）⇒ 本行实得 4");
    }

    // ==================================================================
    //  #40 / #41 / #42 / #43 —— `KeywordTable` 的数字切分口
    // ==================================================================

    /// <summary>
    /// **`#40`–`#43` `CardDef.KeywordTable` 的取值/切分口。**
    ///
    /// 判据 = `CardTrait.GetNewTrait(..., int defaultValue = 0)` 那一族的形状，
    ///   而**我们这边**的口径写在 `KeywordTable.Parse` / `HasNumber` 的注释里：
    ///   值 = **`':'` 之前**那半段里第一个数字；没有就是 1（兜底 1 是红线）。
    ///   `A1342`（2026-10-11）把「取**整串**第一个数字」改成 `FirstNumber(HeadOf(item))` ——
    ///   改前 `Rally: Deal 2 damage to an enemy` 会得到 `2` ⇒ 卡面印出 **`Rally 2`**（那个 2 是正文里的伤害值）。
    ///
    /// 🔴 **四条必须成对**：
    ///   · `#40` 与 `#41` **方向相反**（一个「正文里有数字但关键词没数字 ⇒ 1」，
    ///     一个「关键词自己写了 3 ⇒ 3」）—— 只断 `#40` 的话，把 `FirstNumber` 改成**恒返回 1** 也全绿；
    ///   · `#42` 钉的是 `FirstNumber` 的 `return 1` 兜底（不是 `0`）；
    ///   · `#43` 把 `HasNumber` 与 `Parse` **钉在同一个切分口**上（`HasNumber` 判的也是 `':'` 之前那半段）。
    /// </summary>
    static void TestS14KeywordNumberAndHasNumber()
    {
        var rally = KeywordTable.Parse(new[] { "Rally: Deal 2 damage to an enemy" });
        Check(S14Kw(rally, "rally"), 1,
              "★ `#40` `Rally: Deal 2 damage to an enemy` ⇒ **`rally` = 1**"
            + "（正文里那个 `2` **不算参数** —— 值只从 `':'` **之前**那半段取）"
            + "｜🧨 退回「取整串第一个数字」⇒ 实得 2");
        CheckTrue(!KeywordTable.HasNumber("Rally: Deal 2 damage to an enemy"),
                  "★ `#43` 同一个口：`HasNumber` 也只看 `':'` 之前 ⇒ 这一行是 **false**"
                + "（**两条路必须同一个切分口** —— 一个说「写了数字」一个给 1，才是静默分叉）");

        var blast = KeywordTable.Parse(new[] { "Blast 3: Deal 9 damage" });
        Check(S14Kw(blast, "blast"), 3,
              "★ `#41` **反方向**：`Blast 3: …` ⇒ `blast` = **3**"
            + "（不是 1、也不是正文里那个 9）—— 与 `#40` 方向相反，"
            + "改任一边（恒 1 / 取整串）都必红其一");
        CheckTrue(KeywordTable.HasNumber("Blast 3: Deal 9 damage"),
                  "★ `#43` `HasNumber(\"Blast 3: …\") == true`（数字在 `':'` 之前 ⇒ 算）");
        CheckTrue(!KeywordTable.HasNumber("Blast: Deal 9 damage"),
                  "★ `#43` `HasNumber(\"Blast: …\") == false`（那个 9 在正文里）"
                + "｜🧨 把 `HasNumber` 改成 `Contains(数字)` 整串扫 ⇒ 本行红");
        Check(S14Kw(KeywordTable.Parse(new[] { "Rally: Deal 2 damage" }), "rally"), 1,
              "★ `#42` 兜底那条：**没有**数字的关键词 ⇒ **1**（⛔ 不是 0）——"
            + " 判据 = `FirstNumber` 末尾的 `return 1`"
            + "｜🧨 把它改成 `return 0` ⇒ 本行红（而且引擎里几十处把「值 ≥ 1」当「有这个关键词」用，会大面积静默失效）");
    }

    // ==================================================================
    //  #44 / #45 / #46 —— 付费前缀的【跨句承接】（`CostShared`）
    // ==================================================================

    /// <summary>
    /// **`#44` 真卡 `UM81 Phobos Librarian`** —— `Parse(desc)` 恰好 **2 条 op**；
    /// 两条 `Cost == 3`、`CostKindOf` 归一后都是 **`"oath"`**；
    /// **第一条 `CostShared == false`、第二条 `true`**；`CardDef.OathCost == 3`。
    /// **`#45`/`#46` 两条反向沙包**（`UM_Phobos_Lieutenant` 与 `SOR7` 的原文）⇒ **第二条 `Cost == 0`**。
    ///
    /// 判据 = 原版「**一条 ability = 一个 cost ＋ 一串 logic**」：
    ///   `CardAbility.cs` = `{ AbilityTrigger trigger; …; List&lt;AbilityLogic&gt; abilityLogic }`；
    ///   主动付费那一档 `ActiveAbility.cs` = `{ int manaCost; … }`；
    ///   而 `BattleManager__PayActiveAbilityCostOath.c:22` 里**一次激活只调一次**
    ///   `PlayerManager.UseMana` ⇒ **一次付、多条效果**。
    ///   我们原来「按 `.` 分句 + 付费前缀只贴同段」⇒ `UM81` 的第二句 `Cost = 0`
    ///   ⇒ `CollectOathOps` 不收它 ⇒ **激活后只打伤害、不给 Shield**（静默）。
    ///   现口径 = `Parse` 里那条 `carry`（三条规则：本段自带前缀 ⇒ 换；本段自带**触发头** ⇒ 清零；
    ///   都不是 ⇒ 承接上一段、且 `CostShared = true`）。
    ///
    /// 🔴 **为什么 `#45`/`#46` 不可省**：把 `carry` 改成**无条件继承上一段**也能让 `#44` 绿
    ///   （全池只 `UM81` 一张吃到「③ 承接」）⇒ 必须配两条「带触发头 ⇒ 必须清零」的反向沙包：
    ///   `Slay:` / `Pray:` 是**另一条 ability 的开头**，⛔ 不许继承誓约的代价。
    /// </summary>
    static void TestS14PaidPrefixCarry()
    {
        // ---- #44 真卡 UM81 ----
        var um81 = new CardDef("UM81", "Phobos Librarian", "unit",
                               "Oath 3: Deal 2-4 damage to an enemy. If target dies, gain Shield",
                               "common", "Ultramarines", 3, 3, 3, 0,
                               new[] { "Camouflage", "Oath" }, subtype: "Infantry");
        Check(um81.OathCost, 3,
              "★ `CardDef.OathCost == 3`（从正文前缀算出来的，⛔ 不是卡表里写死的那个数）");
        List<string> un, pa;
        var ops = EffectText.Parse(um81.Desc, out un, out pa);
        Check(ops.Count, 2, "★ `UM81` 的 `desc` 恰好 2 条 op（两句 = 一条 ability）");
        Check(ops[0].Cost, 3, "★ 第 1 条（`Deal 2-4 damage`）带 `Oath 3` 的价");
        Check(ops[1].Cost, 3,
              "★ 第 2 条（`If target dies, gain Shield`）**也带 3** —— 它是**同一份付费前缀**盖住的第二句"
            + "｜🧨 退回「按 `.` 分句、前缀只贴同段」⇒ 本行实得 0 ⇒ 红（而 `UM81` 激活后会静默少一半效果）");
        Check(EffectText.CostKindOf(ops[0].CostKind), "oath", "★ 第 1 条货币归一后 = `oath`");
        Check(EffectText.CostKindOf(ops[1].CostKind), "oath", "★ 第 2 条货币同 = `oath`");
        CheckTrue(!ops[0].CostShared && ops[1].CostShared,
                  "★ **组里第一条付钱（`CostShared == false`）、后面那条挂 `CostShared == true`**"
                + "（`EffectText.StampPaidCost`，判据只此一处）");

        // ---- #45 反向沙包：`Slay:` 是另一条 ability 的开头 ⇒ 必须清零 ----
        var phobosLt = EffectText.Parse("Oath 1: Gain Stealth. Slay: Create a random card in hand",
                                        out un, out pa);
        Check(phobosLt[0].Cost, 1, "（前提）`Oath 1:` 那条带 1");
        Check(phobosLt[1].Cost, 0,
              "★ `#45` **`Slay:` 那一段把前缀清零**（触发头 = 另一条 ability 的开头）——"
            + " 第二条必须是 `Cost == 0`"
            + "｜🧨 把 `carry` 改成无条件继承 ⇒ 本行实得 1 ⇒ 红（`#44` 仍绿）");

        // ---- #46 反向沙包：`Pray:` 同理（SOR7 原文）----
        var sor7 = EffectText.Parse("3 [Icon]: Gain +1 Health.\nPray: Deploy a Sister Novitiate",
                                    out un, out pa);
        Check(sor7[0].Cost, 3, "（前提）`3 [Icon]:` 那条带 3");
        Check(EffectText.CostKindOf(sor7[0].CostKind), "faith",
              "（旁证）`[Icon]` 那三张卡的货币归 `faith`（`CostKindOf` 里 `Contains(\"icon\")` 那一支）");
        Check(sor7[1].Cost, 0, "★ `#46` **`Pray:` 那一段同样清零**（第二种触发头）⇒ 第二条 `Cost == 0`");
    }

    // ==================================================================
    //  #47 / #48 —— 誓约能力：**一次激活只扣一次钱**，而两条效果都发生
    // ==================================================================

    /// <summary>
    /// **`#47` 场上放真卡 `UM81`、调 `RuleCore.UseOathAbility` ⇒ `Energy` 恰好 **−3**（不是 −6）。**
    /// **`#48` 同一次激活：该敌人真的挨了 2-4 点；它死了我们**真的拿到 `Shield`**（两条效果都发生）。**
    ///
    /// 判据 = 原版三段（见 `RuleCore.UseOathAbility` 的 doc）：
    ///   ① `CardScript__CanUseOathAbility.c`（能不能用）；② **`BattleManager__PayActiveAbilityCostOath.c:22`**
    ///   —— 取 trait `0x4fb` 之后**只调一次** `PlayerManager.UseMana`；③ `CardScript__ResolveActiveAbilityPlayed.c`。
    ///   我们这边 = `EffectResolver.ResolveOneCore` 的 `CostShared` 支：
    ///   `CostShared == true` ⇒ **① 不查余额 ② 不再扣 ③ 不重复广播**。
    ///
    /// 🔴 **灭自证**：只断「只扣了 3」**不够** —— 把 `_oathOps` 改回**只收一条**（旧行为）也扣 3。
    ///   ⇒ 必须**同时**断「两条效果都发生了」（伤害真落地 + 条件那半句真跑了）。
    ///   日志里那句「**不再收**」是这一格的直接证据。
    /// </summary>
    static void TestS14OathActivationChargesOnce()
    {
        var um81 = new CardDef("UM81", "Phobos Librarian", "unit",
                               "Oath 3: Deal 2-4 damage to an enemy. If target dies, gain Shield",
                               "common", "Ultramarines", 3, 3, 3, 0,
                               new[] { "Camouflage", "Oath" }, subtype: "Infantry");
        Check(um81.OathOps.Count, 2,
              "★ （关键前提）`OathOps` 收了两条 —— 只收一条的话下面「只扣 3」会**假绿**");

        var ctx = ProbeBattle(new[] { um81 }, new[] { Unit("S14_Foe", 1, 0, 30) });
        ToP1Turn(ctx, 1);
        ctx.Players[0].Energy = 20;
        var u = Place(ctx, 0, 3, um81, exhausted: false);
        u.DeployedTurn = ctx.Turn;
        Place(ctx, 1, 3, Unit("S14_Prey", 1, 0, 2), exhausted: true);   // 2 血 ⇒ 2-4 点里必死
        CheckCode(RuleCore.CanUseOathAbility(ctx, 0, 3), RuleCodes.OK, "（前提）这个誓约能力现在用得了");

        int e0 = ctx.Players[0].Energy;
        ctx.ClearSignals();
        CheckCode(RuleCore.UseOathAbility(ctx, 0, 3), RuleCodes.OK, "激活誓约能力");

        Check(ctx.Players[0].Energy, e0 - 3,
              "★ `#47` **能量恰好 −3**（`Oath 3` 只扣一次）——"
            + " 不是 −6（那是「两条 op 各收一遍」的旧缺陷：`UM93` 实测扣 4、`SOR6` 实测扣 8）"
            + $"｜🧨 把 `ResolveOneCore` 的 `if (op.CostShared)` 支去掉 ⇒ 本行实得 −6{LogTail(ctx, 8)}");
        CheckTrue(S14Log(ctx, "不再收"),
                  "★ 而且日志里**明说**第二条那 3 点「不再收」（不许静默 —— 本工程红线）");
        CheckTrue(SlotOf(ctx, 1, "S14_Prey") == -1,
                  "★ `#48` **第一条效果真发生了**：那个 2 血目标被打掉了（伤害在 2-4 的范围内）");
        CheckTrue(u.HasShield,
                  "★ `#48` **第二条效果也真发生了**：它死了 ⇒ `If target dies, gain Shield` 兑现"
                + "（🆕 这一格正好把规格表矛盾⑤那个「OathCost 修了、但运行期无证据」的缺口补成绿的）");
        Check(S14Sig(ctx, EvtKind.Ability, "oath"), 1,
              "★ 而且只发**一条** `EvtKind.Ability`（付费成功之后才发，⛔ 不是付费之前）");
    }

    // ==================================================================
    //  #49 / #50 —— `EffectText.StampPaidCost` 的静态形状 ＋ 一次激活只收一次钱（结算）
    // ==================================================================

    /// <summary>
    /// **`#49`**：`UM93 Fall Back` / `SOR6 Righteous Repugnance` 的**付费组**各解析出 **2 条 op**、
    /// **同价同货币**、其中**第一条 `CostShared == false`、后面 `true`**。
    /// **`#50`**：结算时 `Energy` **只掉一次**，而且**两条效果都发生**。
    ///
    /// ⚠️ **实测口径（与规格表说法对齐后）**：这两张卡的 `Parse` 结果**整体**是 3 条
    ///   （`UM93` = 裸 `return` + 付费组 2 条；`SOR6` = 裸 `give` + 付费组 2 条），
    ///   规格表说的「2 条」= **付费组那两条**（下面按 `Cost > 0` 取组，⛔ 不是按总条数）。
    ///
    /// 🔴 **灭自证是反向那一条**：把 `CostShared` 抹掉 ⇒ `Energy` **必须掉两次**（`UM93` −5 / `SOR6` −10）。
    ///   单断「只扣一次」可以被「干脆不扣」蒙过 —— 所以这里**同时**断「付费组的两条效果都发生了」
    ///   （`UM93`：部队真回手 **且** `Flank` 真给了；`SOR6`：增益真加上）。
    /// </summary>
    static void TestS14CostSharedChargesOnce()
    {
        List<string> un, pa;

        // ---- #49 静态：付费组的形状 ----
        var um93 = EffectText.Parse(
            "Return a friendly troop to your hand. Oath 2: Lower its cost by 2 and give it Flank.",
            out un, out pa);
        var paid93 = new List<EffectOp>();
        foreach (var op in um93) if (op.Cost > 0) paid93.Add(op);
        Check(paid93.Count, 2, "★ `UM93` 的**付费组**恰好 2 条 op（`lowercost` + `give flank`）");
        Check(paid93[0].Cost, 2, "★ 两条同价：2");
        Check(paid93[1].Cost, 2, "★ ……第二条也是 2");
        Check(EffectText.CostKindOf(paid93[0].CostKind), "oath", "★ 两条同货币：`oath`");
        Check(EffectText.CostKindOf(paid93[1].CostKind), "oath", "★ ……同上");
        CheckTrue(!paid93[0].CostShared && paid93[1].CostShared,
                  "★ **组里第一条付钱、第二条挂 `CostShared`**（`StampPaidCost`）");

        var sor6 = EffectText.Parse(
            "Give +1 [Attack] and +1 [Ranged] to your units this turn. "
          + "4 Energy: Give +2 [Attack] and +2 [Ranged] instead and Heal them 1", out un, out pa);
        var paid6 = new List<EffectOp>();
        foreach (var op in sor6) if (op.Cost > 0) paid6.Add(op);
        Check(paid6.Count, 2, "★ `SOR6` 的付费组也是 2 条（`give` + `heal`）");
        Check(paid6[0].Cost, 4, "★ 两条同价：4");
        Check(paid6[1].Cost, 4, "★ ……第二条也是 4");
        Check(EffectText.CostKindOf(paid6[0].CostKind), "energy", "★ 货币是 `Energy`（不是 oath）");
        CheckTrue(!paid6[0].CostShared && paid6[1].CostShared, "★ 第一条付钱、第二条挂 `CostShared`");

        // ---- #50 结算：真打出去，能量只掉一次 ----
        {
            var um93Card = new CardDef("UM93", "Fall Back", "tactic",
                                       "Return a friendly troop to your hand. Oath 2: Lower its cost by 2 and give it Flank.",
                                       "common", "Ultramarines", 1, 0, 0, 0, new[] { "Oath" });
            var ctx = ProbeBattle(new[] { um93Card }, new[] { Unit("S14_FoeA", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            ctx.Players[0].Energy = 20;
            Place(ctx, 0, 3, Unit("S14_Troop", 1, 1, 5), exhausted: false);
            int e0 = ctx.Players[0].Energy;
            ctx.ClearSignals();
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Fall Back"), 3), RuleCodes.OK,
                      "打出 `UM93`（费用 1 + 誓约 2）");
            Check(ctx.Players[0].Energy, e0 - 3,
                  "★ **能量只掉 3**（1 卡费 + 2 誓约）—— ⛔ 不是 −5（付费组各收一遍的旧缺陷）"
                + $"｜🧨 抹掉 `CostShared` ⇒ 本行实得 −5{LogTail(ctx, 8)}");
            CheckTrue(S14Log(ctx, "不再收"), "★ 日志里明说第二条那 2 点「不再收」");
            Check(SlotOf(ctx, 0, "S14_Troop"), -1,
                  "★ **第一条效果真发生了**：部队回手了（`Return a friendly troop to your hand`）");
            CheckTrue(HandIdx(ctx, 0, "S14_Troop") >= 0, "……它在手里");
            CheckTrue(S14Log(ctx, "给了 1 个目标"),
                  "★ **第二条效果也真发生了**：`give it Flank` 真的给了目标"
                + "（`WhenFired` 那类日志量不到它 —— 它量的是**监听器**，这条是**结算**）");
        }
        {
            var sor6Card = new CardDef("SOR6", "Righteous Repugnance", "tactic",
                                       "Give +1 [Attack] and +1 [Ranged] to your units this turn. "
                                     + "4 Energy: Give +2 [Attack] and +2 [Ranged] instead and Heal them 1",
                                       "common", "Sororitas", 2, 0, 0, 0, null);
            var ctx = ProbeBattle(new[] { sor6Card }, new[] { Unit("S14_FoeB", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            ctx.Players[0].Energy = 20;
            var mine = Place(ctx, 0, 3, Unit("S14_Unit", 1, 1, 5), exhausted: false);
            int e0 = ctx.Players[0].Energy;
            ctx.ClearSignals();
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Righteous Repugnance"), 3), RuleCodes.OK,
                      "打出 `SOR6`（费用 2 + 付费 4）");
            Check(ctx.Players[0].Energy, e0 - 6,
                  "★ **能量只掉 6**（2 卡费 + 4 付费）—— ⛔ 不是 −10"
                + $"｜🧨 抹掉 `CostShared` ⇒ 本行实得 −10{LogTail(ctx, 8)}");
            CheckTrue(S14Log(ctx, "不再收"), "★ 日志里明说第二条那 4 点「不再收」");
            CheckTrue(mine.Attack >= 3,
                  "★ **付费那半真加上了**：我方单位近战 1 → 至少 3（`+2 [Attack]` 落地）"
                + $" —— 实得 {mine.Attack}。⚠️ 如实标着：实测 **4**（= 1 + 第一段的 +1 + 付费段的 +2），"
                + "而这一段卡面写的是 `instead`（本该**替换**掉第一段）⇒ **我们没按 `instead` 抑制第一段**，"
                + "这里只断「付费那份加上了」，不把它固化成期望");
        }
    }

    // ==================================================================
    //  #51 / #52 / #53 —— `faithcheck`（`☀` 是**信仰**、不是能量）
    // ==================================================================

    /// <summary>
    /// **`#51` 归一化两个方向都要**：`Normalize("you have less than 6 ☀") == "faithcheck"`
    /// **且** `Normalize("you have less than 6 Energy") == "energycheck"`。
    /// **`#52` 结算一对（照 `TestConditionKindFamily` 的形状）**：信仰 5 / 能量 99 ⇒ 那半句执行；
    /// 信仰 6 / 能量 0 ⇒ 不执行。
    /// **`#53` `RuleCore.CanJudgeCondition("faithcheck") == true`。**
    ///
    /// 判据 = `A1357`（2026-10-11）：`SOR72 Adelaide the Serene` 的 `Rally: If you have less than 6 ☀, …`
    ///   —— `☀` 是**信仰**（卡图亲读、`descZh` 同），而 `desc` 原来把它错抄成 `Energy`（数据侧已改字）。
    ///   两个资源**各有一个种类名**（`faithcheck` / `energycheck`），结算层一个 `case` 读一个字段
    ///   （`EffectResolver.ConditionHolds`）—— ⛔ 别把两种资源塞进同一个 `case` 里现认字符串
    ///   （那就是**第二份判据**，两处迟早不一致）。
    ///
    /// 🔴 **`#52` 的灭自证是「两个字段必须【反向】变化」**：两档取（信仰 5 / 能量 99）与
    ///   （信仰 6 / 能量 0）—— 只断一侧、或两档里两个字段**同向**变化时，「读错字段」照样全绿
    ///   （`SOR72` 原来就是读 `Energy`）⇒ 下面**外加两格反向对照**（99/5 与 0/99），
    ///   把「读 `Energy`」那条错路**在两个方向上都堵死**。
    /// </summary>
    static void TestS14FaithCheckCondition()
    {
        Check(EffectCondition.Normalize("you have less than 6 ☀"), "faithcheck",
              "★ `#51` `☀` 那一路 ⇒ `faithcheck`");
        Check(EffectCondition.Normalize("you have less than 6 Energy"), "energycheck",
              "★ `#51` **另一个方向**：`Energy` 那一路仍然是 `energycheck`（⛔ 别把两条并成一条）"
            + "｜🧨 把 `faithcheck` 那条 `Normalize` 删掉 ⇒ 上面那行实得「判不出来」⇒ 红");
        CheckTrue(RuleCore.CanJudgeCondition("faithcheck"),
                  "★ `#53` `CanJudgeCondition(\"faithcheck\") == true` —— 而且**它不在**"
                + " `UnjudgeableConditions` 里（那张表只有 `istype` / `foreach`）");
        CheckTrue(RuleCore.CanJudgeCondition("energycheck"),
                  "……`energycheck` 照旧判得了（⛔ 别为了加 `faithcheck` 把它删了）");

        // ---- #52 结算一对（同一条战术卡，两档玩家状态）----
        int FaithDelta(int faith, int energy)
        {
            var card = Tactic("S14_FaithCheck", 0, "If you have less than 6 ☀, gain 1 ☀");
            var ctx = ProbeBattle(new[] { card }, new[] { Unit("S14_Foe", 1, 1, 9) });
            ToP1Turn(ctx, 1);
            ctx.Players[0].Faith = faith;
            ctx.Players[0].Energy = energy;
            int before = ctx.Players[0].Faith;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "S14_FaithCheck"), -1), RuleCodes.OK,
                      $"打出（信仰 {faith} / 能量 {energy}）");
            return ctx.Players[0].Faith - before;
        }

        Check(FaithDelta(5, 99), 1,
              "★ `#52` **信仰 5 / 能量 99 ⇒ 那半句执行**（`gainfaith` 真发生，信仰 +1）");
        Check(FaithDelta(6, 0), 0,
              "★ `#52` **信仰 6 / 能量 0 ⇒ 不执行**（信仰 < 6 不成立）"
            + "｜🧨 这一格读的是 `ctx.Players[owner].Faith`；读成 `Energy` 的话**两格会同时反掉**"
            + "（5/99 ⇒ 0；6/0 ⇒ 1），所以这一对**结构上**分得出「读错字段」");
        Check(FaithDelta(99, 5), 0,
              "★ **反向对照一**：信仰 99 / 能量 5 ⇒ **不执行** —— 这一格只有在「读的是 `Faith`」时才成立"
            + "（读 `Energy` 会得到 +1）");
        Check(FaithDelta(0, 99), 1,
              "★ **反向对照二**：信仰 0 / 能量 99 ⇒ **执行** —— 与上一格**方向相反**，"
            + "两格合起来把「读错字段」在两个方向上都堵死"
            + "｜🧨 把 `case \"faithcheck\"` 里的 `Faith` 改回 `Energy` ⇒ 上面两行**必红其一**");
    }

    // ==================================================================
    //  #54 —— 单位卡 `When <事件>, gain X` 的**执行层** op 必须 `Target.Subjectless`
    // ==================================================================

    /// <summary>
    /// **`#54` 单位卡 `When &lt;事件&gt;, gain X` 的执行层 op 必须 `Target.Subjectless == true`。**
    ///
    /// 落点 = **`CardDef.WhenTriggers[i].Ops[0].Target`**（现成那格 = `RuleEngineTest.cs` 的 ⑤
    /// 那段 `When Reanimated, gain Fast`）。
    ///
    /// 🔴 **⛔ 别拿 `EffectText.Parse(整条 desc)` 的结果去断** —— 那正是今天这条**分叉**：
    ///   实测同一句 `When Reanimated, gain Fast`，
    ///   · **事件层**（`WhenTriggers[0].Ops[0]`）：`gain` / `fast` / `Target.Subjectless == true`；
    ///   · **`Parse(整条 desc)`**：也产出一条 `gain` / `fast`，但 **`Target == null`**，
    ///     而且那一句被归进 **`partial`（半懂）**。
    ///   ⇒ 两条路的 op **形状不同**（一个 subjectless、一个 target 全空），
    ///     所以拿 `Parse` 那一路去断会得到一个**恰好相反**的答案（而它「看起来也在断同一句话」）。
    ///   反过来，这条断言**钉在 `WhenTriggers` 这一层**本身就是那一半判别式：
    ///   「把 `When` 那句改回解析层」的实现会让 `WhenTriggers` 变空 ⇒ 本行立刻红。
    ///
    /// ⚠️ 规格表那一格写「断 `CardDef.FireWhen(...)` 的返回（或 `WhenTriggers[i].Ops[0].Target`）」
    ///   —— 本方法取后者：`FireWhen` 要构造 `listener/who/card/actor/target` 一整套实参，
    ///   而那些实参的存在与否**与「这条 op 的 Target 对不对」无关**（同一份 `_whenTriggers`）。
    /// </summary>
    static void TestS14WhenBodyTargetIsSubjectless()
    {
        var c = new CardDef("S14_When", "S14_When", "unit", "When Reanimated, gain Fast",
                            "common", "Test", 1, 3, 3, 0, null, subtype: "Infantry");
        Check(c.WhenTriggers.Count, 1, "★ 那一句收进了**事件层**（`WhenTriggers`）恰一条");
        var w = c.WhenTriggers[0];
        CheckTrue(w.Ops != null && w.Ops.Count >= 1, "★ 它身上挂着 op（`Ops` 非空）");
        var op0 = w.Ops[0];
        Check(op0.Verb, "gain", "★ 执行层那条 op 的动词 = `gain`");
        CheckTrue(op0.Target != null && op0.Target.Subjectless,
                  "★ **执行层 op 的 `Target.Subjectless == true`** —— 卡面**没写主语**，"
                + "有施放者就是**这个单位自己**（`UnitState` 那个 unit）"
                + "｜🧨 拿 `EffectText.Parse(整条 desc)` 那一路去断的话，那一条的 `Target` 是 **`null`**"
                + "（实测）—— 两条路形状不同，用错那一份会得到一个恰好相反的答案");

        List<string> un, pa;
        var whole = EffectText.Parse(c.Desc, out un, out pa);
        CheckTrue(pa != null && pa.Contains(c.Desc),
                  "（如实记：同一句经 `EffectText.Parse(整条 desc)` 会被归进 **`partial`（半懂）** ——"
                + " 这就是规格表说的那条**分叉**，⛔ 别拿它当判据）");
        CheckTrue(whole.Count == 0 || whole[0].Target == null,
                  "（同上）`Parse(整条 desc)` 那一条的 `Target` 是 **`null`**，**不是** `Subjectless` ⇒"
                + " 两条路产出的 op **形状不同**（本笔断的是**事件层**那一份）");
    }

    // ==================================================================
    //  #55 / #56 —— `☀` 付费前缀的货币是 `faith`，而且**扣的是 `ps.Faith`**
    // ==================================================================

    /// <summary>
    /// **`#55`** 4 张修女会卡（`SOR11`/`SOR23`/`SOR24`/`SOR72`）的付费前缀解析后
    /// `CostKindOf` 给 **`'faith'`**（改前是 `''`），**且扣的是 `ps.Faith` 而不是 `ps.Energy`**。
    /// **`#56`** 5 张（`SOR28`/`SOR43`/`SOR44`/`SOR49`/`SOR59`）的 `2 ☀:` / `4 ☀:` / `7 ☀:` 段
    /// 解析出 **`kind == 'faith'`**；**全池逐 op 归一后 `kind == ''` 从 6 → 0**。
    ///
    /// 判据 = `EffectText.CostKindOf` → `MentionsFaith`（`Contains("faith") || Contains("☀")`）
    ///   ⇒ `"faith"`；结算层 `EffectResolver.ResolveOneCore` 的
    ///   `if (kind == "faith") ps.Faith -= op.Cost;`。
    ///   ⚠️ `A1357` 的原话是「静态链已验、**运行期无证据**」—— 下面 `SOR43` 那一格
    ///   **就是那条运行期证据**（它是**计策**，`Draw a troop. 2 ☀: Draw an additional troop`，
    ///   可以真打出去看扣的是哪个字段）。
    ///
    /// 🔴 **灭自证**：`SOR43` 那一格**同时**断「信仰掉了 2」与「能量只掉了卡费 1」——
    ///   只断一侧的话，「两个字段一起扣」或「扣错了字段但恰好也有信仰」都蒙得过去。
    /// </summary>
    static void TestS14FaithCostKind()
    {
        List<string> un, pa;

        // ---- #55 四张 ----
        var c55 = new (string id, string name, string desc, string[] kw)[] {
            ("SOR11", "Crusader",              "3 ☀: Gain +1 [Attack] and Armour 2", new[] { "Vanguard" }),
            ("SOR23", "Retributor",            "1 ☀: Deal 3 damage to a random enemy", new string[0]),
            ("SOR24", "Seraphim",              "3 ☀: Gain +1 [Ranged] and +1 Health", new[] { "Flying", "Flank" }),
            ("SOR72", "Adelaide the Serene",
                "6 ☀ Gain Flank and Shield. Rally: If you have less than 6 ☀, gain 1 ☀ for each enemy unit.",
                new[] { "Flying", "Flank", "Shield", "Rally" }),
        };
        foreach (var (id, nm, desc, kw) in c55)
        {
            var cd = new CardDef(id, nm, "unit", desc, "common", "Sororitas", 1, 1, 1, 0, kw, subtype: "Infantry");
            var ops = EffectText.Parse(cd.Desc, out un, out pa);
            int paid = 0, faith = 0;
            foreach (var op in ops)
                if (op.Cost > 0)
                {
                    paid++;
                    if (EffectText.CostKindOf(op.CostKind) == "faith") faith++;
                }
            CheckTrue(paid > 0, $"★ `#55` `{id}` 有付费 op（否则这一条退化成恒真）");
            Check(paid - faith, 0,
                  $"★ `#55` **`{id}` 的付费前缀货币归一后全是 `faith`**（改前是 `''`）——"
                + $" 共 {paid} 条付费 op、其中 {faith} 条归 `faith`"
                + "｜🧨 把 `MentionsFaith` 里的 `Contains(\"☀\")` 去掉 ⇒ 本行红（`☀` 那几段会掉回 `''`）");
        }

        // ---- #56 五张 ----
        var c56 = new (string id, string name, string desc, string[] kw)[] {
            ("SOR28", "Dominion Superior", "Rally: Deal 1 damage. 2 ☀: Deal 1 additional damage", new[] { "Rally" }),
            ("SOR43", "Devout Warriors",   "Draw a troop. 2 ☀: Draw an additional troop", new string[0]),
            ("SOR44", "Beacon of Faith",
                "Give +2 Health to your units.\n4 ☀: Give an additional +1 Health.\n"
              + "7 ☀: Give them +1 [Attack] and +1 [Ranged] as well.", new string[0]),
            ("SOR49", "Trial of Suffering",
                "Deal 1 damage to a friendly troop and give it Armour 1. 2 ☀: Give it Armour 2 instead",
                new string[0]),
            ("SOR59", "Imperial Creed",    "Stun three random enemies.\n4 ☀: Refill 3 Energy", new string[0]),
        };
        foreach (var (id, nm, desc, kw) in c56)
        {
            var cd = new CardDef(id, nm, "tactic", desc, "common", "Sororitas", 1, 0, 0, 0, kw);
            var ops = EffectText.Parse(cd.Desc, out un, out pa);
            int paid = 0, faith = 0;
            foreach (var op in ops)
                if (op.Cost > 0)
                {
                    paid++;
                    if (EffectText.CostKindOf(op.CostKind) == "faith") faith++;
                }
            CheckTrue(paid > 0, $"（前提）`{id}` 有付费 op");
            Check(paid - faith, 0,
                  $"★ `#56` **`{id}` 的 `☀:` 段全归 `faith`**（{paid} 条付费 op、{faith} 条 faith）");
        }

        // ---- #56 第二半：全池逐 op「归一后为空」= 0 ----
        {
            var pool = RuleEngine.CardDatabase.Load();
            CheckTrue(pool != null && pool.Count > 1000,
                      $"（前提）卡池装起来了（{ (pool == null ? 0 : pool.Count) } 张）");
            int paidOps = 0, faithOps = 0, emptyKind = 0;
            var strays = new List<string>();
            foreach (var cd in pool)
            {
                if (cd == null) continue;
                var ops = EffectText.Parse(cd.Desc, out un, out pa);
                foreach (var op in ops)
                {
                    if (op.Cost <= 0) continue;
                    paidOps++;
                    string k = EffectText.CostKindOf(op.CostKind);
                    if (k == "faith") faithOps++;
                    if (k == "")
                    {
                        emptyKind++;
                        if (strays.Count < 8) strays.Add(cd.Name + "/" + op.Source);
                    }
                }
            }
            CheckTrue(paidOps > 50, $"（前提）全池付费 op 数合理（实得 {paidOps} 条）");
            Check(emptyKind, 0,
                  $"★ `#56` **全池逐 op「货币归一后为空」= 0**（改前是 6 条）——"
                + $" 全池付费 op {paidOps} 条、其中归 `faith` 的 {faithOps} 条"
                + (strays.Count > 0 ? "；残留：" + string.Join(" | ", strays) : "")
                + "｜🧨 少接一种货币名（例如把 `☀` 那一支去掉）⇒ 本行立刻红");
        }

        // ---- #55 运行期：真打一张 `☀` 计策，看扣的是哪个字段 ----
        {
            var sor43 = new CardDef("SOR43", "Devout Warriors", "tactic",
                                    "Draw a troop. 2 ☀: Draw an additional troop",
                                    "common", "Sororitas", 1, 0, 0, 0, null);
            var ctx = ProbeBattle(new[] { sor43 }, new[] { Unit("S14_Foe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            ctx.Players[0].Faith = 10;
            ctx.Players[0].Energy = 10;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Devout Warriors"), -1), RuleCodes.OK,
                      "打出 `Draw a troop. 2 ☀: Draw an additional troop`");
            Check(ctx.Players[0].Faith, 8,
                  "★ `#55` **扣的是 `ps.Faith`**（10 → 8，`2 ☀` 那份）—— 这就是规格表说缺的那条**运行期证据**"
                + "｜🧨 把 `ResolveOneCore` 的 `if (kind == \"faith\") ps.Faith -= op.Cost;` 改回 `ps.Energy` ⇒ 本行红");
            Check(ctx.Players[0].Energy, 9,
                  "★ **而能量只掉了卡费那 1 点**（10 → 9）—— 与上一行**合起来**才是判别式："
                + "只断「信仰掉了 2」的话，「两个字段一起扣」照样绿"
                + $"｜🧨 同上 ⇒ 本行实得 6（1 + 2 + …）{LogTail(ctx, 6)}");

            // 反向：信仰不够 ⇒ 那半句**不生效**（且如实报出来）
            var ctx2 = ProbeBattle(new[] { sor43 }, new[] { Unit("S14_Foe2", 1, 0, 9) });
            ToP1Turn(ctx2, 2);
            ctx2.Players[0].Faith = 0;
            ctx2.Players[0].Energy = 10;
            CheckCode(RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "Devout Warriors"), -1), RuleCodes.OK,
                      "信仰 0 时照样打得出去（裸 `Draw a troop` 那半不需要付费）");
            Check(ctx2.Players[0].Faith, 0, "★ 信仰一点没扣（不够 ⇒ 整条**不激活**）");
            CheckTrue(S14Log(ctx2, "没生效") || S14Log(ctx2, "不够"),
                      "★ 而且**如实报出来**了（原版 = 付不起就整段不激活，不是「付了效果减半」；"
                    + "本工程红线 = 不许静默失败）");
        }
    }

    // ==================================================================
    //  🆕 `A1396`（2026-10-21）—— `A1355` / `A1354` / `A1356` 三笔的断言
    //
    //  那三笔交付时 `RuleEngine/Editor/RuleEngineTest*.cs` **不在写手白名单**
    //  ⇒ 三笔**一笔断言都没写**（写手如实报回来了）。本段就是把它们补上。
    //
    //  🔴 判据一律是**原版反编译方法体**（本仓第一权威 `d:/2/tools/decomp_full/`）
    //     ＋ 写手回 VA 反汇编复核过的那两段（`GameAssembly.dll`，ImageBase `0x180000000`）。
    //     ⛔ 没有一条期望值是从我们自己的实现里读回来的。
    //  ⚠️ 本段里凡**直接改棋盘 / 直接调 `RuleCore.*`** 的都是**夹具直调**，不是走
    //     `BattleDriver.LocalAct` / `SimpleAI.ExecuteAction` 那两个记账口 ⇒ **不会被录进本地录像**
    //     （与本文件其余各条同一个口径）。
    // ==================================================================

    /// <summary>第一条「这条 kind + 这个关键词」的事件落在**哪一格**（`-1` = 没有）。
    /// 与 <see cref="S14SigIndex"/> 同族，区别是它回 `Slot`、那个回**下标**。</summary>
    static int S14SigSlot(BattleContext ctx, EvtKind kind, string keyword)
    {
        for (int i = 0; i < ctx.Signals.Count; i++)
            if (ctx.Signals[i].Kind == kind && ctx.Signals[i].Keyword == keyword) return ctx.Signals[i].Slot;
        return -1;
    }

    /// <summary>同上，回 `Player`（**哪一方**）。</summary>
    static int S14SigWho(BattleContext ctx, EvtKind kind, string keyword)
    {
        for (int i = 0; i < ctx.Signals.Count; i++)
            if (ctx.Signals[i].Kind == kind && ctx.Signals[i].Keyword == keyword) return ctx.Signals[i].Player;
        return -1;
    }

    /// <summary>第一条**任意关键词**的这个 kind 事件在 `Signals` 里的下标（`-1` = 没有）。
    /// 只给「两条触发之间的先后」用 —— <see cref="S14SigIndex"/> 要关键词，
    /// 量不了 `EvtKind.Hit` 那种不带关键词的。</summary>
    static int S14SigIndexAny(BattleContext ctx, EvtKind kind)
    {
        for (int i = 0; i < ctx.Signals.Count; i++) if (ctx.Signals[i].Kind == kind) return i;
        return -1;
    }

    /// <summary>某一方**棋盘上全部单位**的攻击力之和（`Board` 每一格的 `Attack` 相加）。
    ///
    /// 用途：量「一条**没写目标**的 `gain +N attack` 有没有结算」——
    /// 那种 op 的落点由**目标挑法**决定，而挑法今天**跳过生命 ≤ 0 的单位**
    /// （见 `#21` 那一格记的疑点）。**只断总数**就与「落在哪一个单位身上」无关：
    /// 挑法将来修成哪一种，总数都还是 +N ⇒ ⛔ 不会因为修好而变红。</summary>
    static int S14AtkSum(BattleContext ctx, int p)
    {
        int n = 0;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[p].Board[s];
            if (u != null) n += u.Attack;
        }
        return n;
    }

    // ==================================================================
    //  `A1355` —— 预览侧：「空投舱血池 = 判死循环的**第一层**」
    // ==================================================================

    /// <summary>
    /// **`A1355`：`WouldKillByEntries` 里 `dropPod` 血池那一层。**
    ///
    /// 判据 = **原版反编译方法体**（本仓第一权威）：
    /// `CardScript$$EnoughPendingDamageToDieWithDamageValues`，入口 RVA `0x5EBE00` / VA `0x1805EBE00`：
    ///   · `0x1805ec228 mov edx,0xe6`（`DefinedTrait.dropPod` = 230）
    ///     → `0x1805ec232 call`（`EntityScript$$HasCurrentTrait`）→ `0x1805ec239 je 0x1805ec26b`
    ///     （**没这个词 ⇒ 落进幸存者/堡垒**）；
    ///   · `0x1805ec245 cmp esi,r13d` + `0x1805ec248 jge`（`esi` = 累计伤害、`r13d` = 池）：
    ///     `&lt;` ⇒ `sub r13d,esi` + `xor esi,esi`（池吃掉这一段、累计清零）；
    ///     `&gt;=` ⇒ `xor esi,esi` + `mov byte[rbp+0x60],1`（累计清零 ＋ 池标记「已花掉」）；
    ///   · 两支**都以 `jmp 0x1805ec100` 回循环顶**（`0x1805ec255` / `0x1805ec266`）
    ///     ⇒ 原版是 **`if / else`**：走池子那一轮**不落进**幸存者/堡垒。
    /// 我们这一口 = `RuleCore.WouldKillByEntries` 循环里那一段（`u.Has(DropPod) &amp;&amp; !podSpent`）。
    ///
    /// 🔴 **手工夹具（⚠️ 不是真卡）**：全池 **1126 张卡 0 张声明 `dropPod`**
    ///    （唯一含这个词的是 **卡名** `SW31 Fenrisian Drop Pod` —— 它 `keywords` 只有 `Flying` / `Armour 1`）
    ///    ⇒ 下面那个 `CardDef` 是**手工构造**的、血池靠 `AddKeyword` 挂上去。
    ///    今天**不可观测**，但按**铁律 11**（复刻有缺漏就补、只有先后之分）照样断言。
    ///
    /// 🔴 **灭自证（两半，结构上不可能同时对）**：
    ///   · ①（`{5}` ⇒ 假）要求**这一层在** —— 删掉那一段 ⇒ ① 实得 `true`；
    ///   · ②（`{5,5,5}` ⇒ 真）要求「池一旦花掉就**不再**被填满」（原版那个 `bVar3`）——
    ///     把池写成每轮重置 ⇒ ② 实得 `false`。
    ///   ⇒ 「永远判死」与「永远不判死」这两种错法各让其中一格红，⛔ 不可能一起绿。
    ///   🔴 ⑦ 单独钉 `jge` 的**方向**（`&gt;=` 而不是 `&gt;`）—— ① / ④ 都量不出这个方向。
    /// </summary>
    static void TestS14WouldKillByEntriesDropPodLayer()
    {
        // ---- 手工夹具（⚠️ 不是真卡，见上面那段）----
        var podCard = new CardDef("S14_ManualDropPod", "S14_ManualDropPod", "unit",
                                  "", null, "Test", 1, 1, 5, 0, null, subtype: "Infantry");
        var u = new UnitState(podCard, false);
        u.AddKeyword(KeywordTable.DropPod, 4);
        Check(u.Health, 5, "（前提·**手工夹具、不是真卡**）生命 5");
        Check(u.KwValue(KeywordTable.DropPod), 4, "（前提·手工夹具）空投舱血池 4");
        Check(u.Bastion, 0, "（前提·手工夹具）没有堡垒 —— 这一条只量**池**那一层");
        Check(u.Survivor, 0, "（前提·手工夹具）没有幸存者");

        // ---- ① 池把一条致死的一击**整份**吃下 ⇒ 不判死 ----
        Check(RuleCore.WouldKillByEntries(u, new List<int> { 5 }), false,
              "★ ① **池 4 吃掉 5 点**：`{5}`（5 ≥ 生命 5，本来是致死的）⇒ **不判死**"
            + "｜🧨 把 `WouldKillByEntries` 里 `dropPod` 那一段删掉（= 退回旧写法）⇒ 本行实得 `true`");

        // ---- ② 池花掉之后，后面的伤害真的落回生命 ⇒ 判死 ----
        Check(RuleCore.WouldKillByEntries(u, new List<int> { 5, 5, 5 }), true,
              "★ ② **池花掉之后照样会死**：`{5,5,5}` ⇒ 第一条被池吃下、第二三条落到生命（5 = 生命 5）"
            + " ⇒ **判死**"
            + "｜🧨 把池写成**每轮重新装满**（原版那个 `bVar3` 不生效）⇒ 本行实得 `false`"
            + " —— 与 ① **合起来**才是判别式：只断 ① 的话「永远返回 false」也全绿");

        // ---- ③ 两条各 3：第一条没穿池、第二条把池打穿并**整份丢给池** ⇒ 不判死 ----
        Check(RuleCore.WouldKillByEntries(u, new List<int> { 3, 3 }), false,
              "★ ③ **池 4、两条各 3**：第一条 `3 &lt; 4` ⇒ 池减到 1、累计清零；"
            + "第二条 `3 &gt;= 1` ⇒ **整份丢给池**（累计清零 ＋ 池标记花掉）⇒ **不判死**"
            + "｜🧨 没有这一层时累计 6 ≥ 5 ⇒ 本行实得 `true`");

        // ---- ④ 同上形状，换个数值：池 4、三条各 2 ⇒ 仍不判死 ----
        Check(RuleCore.WouldKillByEntries(u, new List<int> { 2, 2, 2 }), false,
              "★ ④ **池 4、三条各 2**：第二条 `2 &gt;= 池 2` ⇒ 池花掉；第三条只剩 2 &lt; 生命 5"
            + " ⇒ **不判死**（与 ③ 是同一个判别式的另一个形状 —— 数值不同、结论相同）"
            + "｜🧨 没有这一层时累计 6 ≥ 5 ⇒ 本行实得 `true`");

        // ---- ⑤ 池为 0 但词还在：原版 `esi >= r13d`（r13d = 0）恒真 ⇒ 第一击照样被吞 ----
        var zero = new UnitState(podCard, false);
        zero.AddKeyword(KeywordTable.DropPod, 0);
        CheckTrue(zero.Has(KeywordTable.DropPod), "（前提）池 0 时**词还在**（`Has` 真、`KwValue` 0）");
        Check(zero.KwValue(KeywordTable.DropPod), 0, "（前提）池 = 0");
        Check(RuleCore.WouldKillByEntries(zero, new List<int> { 5 }), false,
              "★ ⑤ **池 0（词还在）**：`acc(5) &lt; 0` 恒假 ⇒ 走 `else` ⇒ 这一击**整份被吞**、"
            + "生命一点不动 ⇒ **不判死** —— 这是原版 `cmp esi,r13d`（`r13d = 0`）的**逐字后果**；"
            + "⚠️ 形状怪，但**原版如此**，如实钉着（⛔ 别改成「池 0 就不算这一层」）");

        // ---- ⑥ 对照：同样 5 血、**没有** dropPod 的单位，一条 5 就判死 ----
        var plain = new UnitState(Unit("S14_NoPodWK", 1, 1, 5), false);
        CheckTrue(!plain.Has(KeywordTable.DropPod), "（前提）对照那个**没有** `dropPod`");
        Check(RuleCore.WouldKillByEntries(plain, new List<int> { 5 }), true,
              "★ ⑥ **对照**：没有池 ⇒ 一条 5 就判死 —— 挡住「把这一层改成无差别兜底」那种改法");

        // ---- ⑦ 钉 `jge` 的方向：`>=` 而不是 `>` ----
        // 池 4、`{4,5}`：正确（`acc < pool` 才算「没穿」）⇒ 第一条 `4 >= 4` ⇒ 池花掉；
        // 第二条 5 ≥ 生命 5 ⇒ **判死**。
        // 若把方向读反（`acc <= pool` 才算没穿）⇒ 第一条走「扣池」那支、`podSpent` 仍是假，
        // 第二条 `5 <= 0` 为假 ⇒ 也走 else ⇒ **不判死**。
        Check(RuleCore.WouldKillByEntries(u, new List<int> { 4, 5 }), true,
              "★ ⑦ **`jge` 的方向**：池 4、`{4,5}` ⇒ 第一条（4）**刚好打平即整份丢给池**、"
            + "第二条 5 落到生命 ⇒ **判死**"
            + "｜🧨 把那一句读成 `acc &lt;= podPool`（`&gt;` 而不是 `&gt;=`）⇒ 本行实得 `false`"
            + "（第一条走「扣池」那支、池标记没置位）—— ① / ④ 都量不出这个方向，只有本行量得出");
    }

    // ==================================================================
    //  `A1354` —— 空投舱的两支开舱时机（回合开始 / 出手后）
    // ==================================================================

    /// <summary>
    /// **`A1354` 支①：本方回合开始自动开舱**（`RuleCore.BeginTurn` 的 `RefreshForNewTurn()` 之后）。
    ///
    /// 判据 = `d:/2/tools/decomp_full/CardScript__OnTurnStart.c:102-108 / 263-266`：
    ///   `:102 bVar3 = false;` → `:103 HasCurrentTrait(param_1, 0xe6 /* dropPod */)` ∧ `IsInPlay`
    ///   ∧ **`param_2 == card.isPlayer`**（本卡的拥有者这一回合）⇒ `:107 RemoveDropPod(param_1,0)`
    ///   ＋ `:108 bVar3 = true;` → `:263-266 if (bVar3) OnTrigger(0x1b8 /* = `AbilityTrigger.Landing = 440` */)`。
    /// 🔴 **`bVar3` 的语义没有歧义**：`grep -n bVar3 CardScript__OnTurnStart.c` ⇒ 整支函数里只有 4 处
    ///   （`:7` 声明 / `:102` 置假 / `:108` 置真 / `:263` 读）⇒ 它唯一的意思就是「这一趟开过舱」。
    /// 我们这一口 = `RuleCore.OpenDropPod`，由 `BeginTurn` 那一圈 `p.Board` 上每个单位各调一次。
    ///
    /// **断什么**：① 本轮行动方的舱**开了**（词摘掉 ＋ 一条 `Trigger{landing}`，格位 = 行动方那一格）；
    ///   ② **非行动方**的不在 `p.Board` 这一圈里 ⇒ **没开**（= 原版那条 `param_2 == card.isPlayer`）；
    ///   ③ 换边之后**对手那个这时才开**；④ 再转回来**不重复开**（词已经没了）。
    ///
    /// 🔴 **灭自证**：删掉 `BeginTurn` 里那一句 `OpenDropPod` ⇒ ①②③ 全红，而
    ///    `TestS14DropPodOpensAfterAttack`（**另一支**）**照旧全绿** ⇒ 两支各有一条独立的判别式。
    ///    `OpenDropPod` 里那句 `u.Has(DropPod)` 守卫则由 ④ 单独盯着。
    /// </summary>
    static void TestS14DropPodOpensAtTurnStart()
    {
        var ctx = ProbeBattle(new[] { Unit("S14_TsMine", 1, 1, 5) }, new[] { Unit("S14_TsFoe", 1, 1, 5) });
        int me = ctx.Active, foe = 1 - ctx.Active;
        Check(ctx.Turn, 0, "（前提）`BeginTurn` 还没跑过（`NewBattle` 不调它）");

        var mine = Place(ctx, me, 3, Unit("S14_PodTurnMine", 1, 1, 5), exhausted: true);
        mine.AddKeyword(KeywordTable.DropPod, 3);
        var theirs = Place(ctx, foe, 3, Unit("S14_PodTurnFoe", 1, 1, 5), exhausted: true);
        theirs.AddKeyword(KeywordTable.DropPod, 3);
        Check(mine.KwValue(KeywordTable.DropPod), 3, "（前提）本方血池 3");
        Check(theirs.KwValue(KeywordTable.DropPod), 3, "（前提）对手血池 3");

        // ---- ① 本方回合开始 ⇒ 【开】----
        ctx.ClearSignals();
        RuleCore.BeginTurn(ctx);
        Check(ctx.Turn, 1, "（前提）进到第 1 回合");
        Check(ctx.Active, me, "（前提）`BeginTurn` **不换边**（换边在 `EndTurn`）");
        CheckTrue(!mine.Has(KeywordTable.DropPod), "★ ① 【什么时候开】本方回合开始 ⇒ 舱**开了**（词整个摘掉）");
        Check(mine.KwValue(KeywordTable.DropPod), 0, "★ ① ……血池归 0（不是留着一个 0 层的词）");
        Check(S14Sig(ctx, EvtKind.Trigger, "landing"), 1, "★ ① 恰好**一条** `EvtKind.Trigger{landing}`");
        Check(S14SigWho(ctx, EvtKind.Trigger, "landing"), me, "★ ① ……报的是**本轮行动方**");
        Check(S14SigSlot(ctx, EvtKind.Trigger, "landing"), 3, "★ ① ……格位 = 本单位那一格（3）");

        // ---- ② 非行动方 ⇒ 【不开】----
        CheckTrue(theirs.Has(KeywordTable.DropPod),
                  "★ ② 【什么时候不开】**非行动方**的那个不在 `BeginTurn` 这一圈（只扫 `p.Board`）里 ⇒ "
                + "它的舱**没开**（词还在）—— 这正是原版那条 `param_2 == card.isPlayer`"
                + "｜🧨 把 `OpenDropPod` 那一句搬出「只扫 `p.Board`」这一圈（改成两边都扫）⇒ 本行红");

        // ---- ③ 换边 ⇒ 对手那个这时才开 ----
        ctx.ClearSignals();
        PassTurn(ctx);
        Check(ctx.Active, foe, "（前提）换到对手那一方");
        CheckTrue(!theirs.Has(KeywordTable.DropPod), "★ ③ 轮到**它自己**的回合 ⇒ 它的舱这时才开");
        Check(S14Sig(ctx, EvtKind.Trigger, "landing"), 1, "★ ③ 恰好一条 `landing`（只有它这一格）");
        Check(S14SigWho(ctx, EvtKind.Trigger, "landing"), foe, "★ ③ ……报的是**那一方**");
        Check(S14SigSlot(ctx, EvtKind.Trigger, "landing"), 3, "★ ③ ……格位 = 它那一格（3）");

        // ---- ④ 再转回本方 ⇒ 【不重复开】----
        ctx.ClearSignals();
        PassTurn(ctx);
        Check(ctx.Active, me, "（前提）转回本方");
        Check(S14Sig(ctx, EvtKind.Trigger, "landing"), 0,
              "★ ④ 【什么时候不开】**舱只开一次**：转回本方时**一条 `landing` 都不发**"
            + "（词已经没了 ⇒ `OpenDropPod` 开头那句 `u.Has(DropPod)` 直接返回）"
            + "｜🧨 把 `OpenDropPod` 开头那句守卫删掉（无条件发触发）⇒ 本行实得 1");
    }

    /// <summary>
    /// **`A1354` 支②：出手之后自动开舱**（`RuleCore.DeclareAttack` 的「伪装摘除」之后、`var target` 之前）。
    ///
    /// 判据 = `BattleManager._ResolveAttack_d__438__MoveNext.c:339-379`，
    /// 🔴 **写手回 VA 逐条读过**（`.c` 在这一段有 `yield` 状态机，光看 `.c` 会读反）——
    /// 入口 RVA `10145552`（`all_methods.txt`）/ VA `0x1809ACE10`：
    ///   · `0x1809ad4c8 mov edx,0xe6` → `call`（`HasCurrentTrait`）→ `0x1809ad4d4 je 0x1809ad847`：
    ///     **没有 `dropPod` ⇒ 跳到「正常继续」那一格**；
    ///   · 有 ⇒ `0x1809ad4ec call`（`RemoveDropPod(card, 0)`）—— **无条件**；
    ///   · 随后才判 `0x302`（`DefinedTrait.landing = 770`）⇒ 不带的「开舱 + 等一帧 + 照常打」、
    ///     带的才 `CancelAttack`。「带 `landing` ⇒ 取消这次攻击」那一支**我们没做**（另立 `A1395`）。
    ///
    /// **断什么**：① 出手后**攻击者**的舱开了（词摘掉 ＋ 一条 `Trigger{landing}`，格位 = 攻击者那一格）；
    ///   ② **被打的那个不开舱**（原版那一跳只对攻击者；这里它的池只按**伤害**扣）；
    ///   ③ 攻击者**没有** `dropPod` ⇒ **一条 `landing` 都不发**。
    ///
    /// 🔴 **灭自证**：删掉 `DeclareAttack` 里那句 `OpenDropPod` ⇒ ①② 全红，
    ///    而 `TestS14DropPodOpensAtTurnStart`（**另一支**）**照旧全绿**；
    ///    把那一句写成「攻守双方都开舱」⇒ ② 红；删掉 `OpenDropPod` 的 `Has` 守卫 ⇒ ③ 红。
    /// </summary>
    static void TestS14DropPodOpensAfterAttack()
    {
        // ---- ① 攻击者带 `dropPod` ⇒ 出手后开舱 ----
        {
            var ctx = ProbeBattle(new[] { Unit("S14_AtkMine", 1, 1, 5) }, new[] { Unit("S14_AtkFoe", 1, 1, 5) });
            ToP1Turn(ctx, 2);
            int atkP = ctx.Active, foeP = 1 - ctx.Active;
            var atk = Place(ctx, atkP, 3, Unit("S14_PodAtkU", 1, 3, 9), exhausted: false);
            atk.AddKeyword(KeywordTable.DropPod, 2);
            // 被打那个池 **5 &gt; 挨的 3** ⇒ 它这一格不会因为「打空」被 `ApplyDamage` 摘掉
            var def = Place(ctx, foeP, 3, Unit("S14_PodDefU", 1, 0, 9), exhausted: true);
            def.AddKeyword(KeywordTable.DropPod, 5);
            Check(atk.KwValue(KeywordTable.DropPod), 2, "（前提）攻击者血池 2");
            Check(def.KwValue(KeywordTable.DropPod), 5, "（前提）被打的那个血池 5");
            Check(def.Attack, 0, "（前提）被打的那个 0 攻 ⇒ **不反击**（免得搅了攻击者的读数）");

            ctx.ClearSignals();
            CheckCode(RuleCore.DeclareAttack(ctx, atkP, 3, foeP, 3), RuleCodes.OK, "近战打出去");
            CheckTrue(!atk.Has(KeywordTable.DropPod),
                      "★ ① 【什么时候开】出手之后 ⇒ **攻击者的舱开了**（词整个摘掉）"
                    + "｜🧨 删掉 `DeclareAttack` 里那句 `OpenDropPod(ctx, p, atkSlot, attacker)` ⇒ 本行红"
                    + "（而 `TestS14DropPodOpensAtTurnStart` 那一支**照旧全绿**）");
            Check(atk.KwValue(KeywordTable.DropPod), 0, "★ ① ……血池归 0");
            Check(S14Sig(ctx, EvtKind.Trigger, "landing"), 1, "★ ① 恰好一条 `Trigger{landing}`");
            Check(S14SigWho(ctx, EvtKind.Trigger, "landing"), atkP, "★ ① ……报的是**攻击者那一方**");
            Check(S14SigSlot(ctx, EvtKind.Trigger, "landing"), 3, "★ ① ……格位 = 攻击者那一格（3）");
            CheckTrue(def.Has(KeywordTable.DropPod),
                      "★ ② 【不是「谁挨打谁开舱」】被打的那个舱**没被动过**（词还在）"
                    + "｜🧨 把那一句写成「攻守双方都开舱」⇒ 本行红");
            Check(def.KwValue(KeywordTable.DropPod), 2,
                  "★ ② ……而且它的池只按**伤害**扣（5 − 3 = 2）、不是被开舱清掉"
                + "（两条合起来才排得掉「开舱顺手把池清了」那种错法）"
                + "｜🧨 同上 ⇒ 本行实得 0");
        }

        // ---- ③ 攻击者**没有** `dropPod` ⇒ 一条 `landing` 都不发 ----
        {
            var ctx = ProbeBattle(new[] { Unit("S14_AtkMine2", 1, 1, 5) }, new[] { Unit("S14_AtkFoe2", 1, 1, 5) });
            ToP1Turn(ctx, 2);
            int atkP = ctx.Active, foeP = 1 - ctx.Active;
            Place(ctx, atkP, 3, Unit("S14_NoPodAtkU", 1, 3, 9), exhausted: false);
            Place(ctx, foeP, 3, Unit("S14_NoPodDefU", 1, 0, 9), exhausted: true);
            ctx.ClearSignals();
            CheckCode(RuleCore.DeclareAttack(ctx, atkP, 3, foeP, 3), RuleCodes.OK,
                      "没有空投舱的单位照样能打（开舱是**额外**的一跳，不是前置）");
            Check(S14Sig(ctx, EvtKind.Trigger, "landing"), 0,
                  "★ ③ 【什么时候不开】攻击者**没有** `dropPod` ⇒ 一条 `landing` 都不发"
                + "｜🧨 把 `OpenDropPod` 里那句 `u.Has(KeywordTable.DropPod)` 守卫删掉 ⇒ 本行实得 1");
        }
    }

    // ==================================================================
    //  `A1356` —— `TrySwarmMerge` / `EmitBloodThirst` 的「事件 + 广播」两半
    // ==================================================================

    /// <summary>
    /// **`A1356` 刀①：`TrySwarmMerge` 那一路的「发什么 / 结算不结算」。**
    ///
    /// 落点 = `RuleCore.TrySwarmMerge` 尾部**一次** <c>FireTriggerAlways(ctx, host, Swarm, p, hostSlot)</c>
    /// （原来是**两条手写的跳**：`BroadcastKeywordEvent(Triggers(Swarm), host)` ＋ 一段自己拼的 `ctx.Emit`）。
    ///
    /// 判据 = 原版 `BattleManagerSupport__BroadcastUnitSwarm.c`：`:16-18` 先 `DamageSignal.Raise`
    ///   （= 我们的事件流那一格）、`:20-70` 再对「场上所有卡 ＋ 当前回合方手牌 ＋ 对手手牌」逐个
    ///   `CardScript__TriggeredSwarm` → `OnTrigger(0x26c /* = AbilityTrigger.TriggeredSwarm = 620 */)`
    ///   —— **两件事都在同一个无条件函数里**，中间没有任何 `if` 挡着 ⇒ 「事件」与「广播」**都要发**。
    ///
    /// ⚠️ **`TrySwarmMerge` 的「事件那一半」不是本笔新加的** —— 它 **2026-09-30 就补过**，
    ///    `RuleEngineTest.cs` 的 `TestSwarm` 第 ① 组一直盯着它（⛔ 别把本笔的说法读成「新增行为」）。
    ///    本笔把它**收成一次 `FireTriggerAlways`**（等价改写），**唯一的行为变化是次序**
    ///    （原来「先广播、后发事件」⇒ 现在「先发事件、后广播」，**更贴原版**）——
    ///    下面 ② 就是量这条次序的判别式。
    ///
    /// ⚠️ **「结算不结算」那一半今天不可观测**（如实标着）：`swarm` **不在 `CardDef.RoutableTriggers`**
    ///    ⇒ `FxOps("swarm")` / `Effect("swarm")` **恒 null** ⇒ `FireTriggerAlways` **必走「没正文」那一支**
    ///    （只发事件 ＋ 广播、**不结算任何正文**）。全池没有一张卡能带 `Swarm:` 正文 ⇒ 钉不出来，如实记着。
    /// </summary>
    static void TestS14SwarmTriggerBothHalves()
    {
        CardDef SwarmGuy(string n, int atk, int hp)
        {
            return new CardDef(n, n, "unit", "", "common", "Test", 1, atk, hp, 0,
                               new[] { KeywordTable.Swarm }, subtype: "Infantry");
        }

        // ---- ① 「事件 + 广播」两半齐（监听卡照真卡 `Termagant Brood` 那句写法）----
        {
            var watcher = new CardDef("S14_SwWatchAtk", "S14_SwWatchAtk", "unit",
                                      "When a friendly unit triggers Swarm, gain +2 Attack",
                                      "common", "Test", 1, 1, 9, 0, null, subtype: "Infantry");
            var a = SwarmGuy("S14_SwMergedA", 2, 3);
            var b = SwarmGuy("S14_SwMergedA", 2, 3);
            var ctx = ProbeBattle(new[] { b }, new[] { Unit("S14_SwFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 3, watcher, exhausted: true);
            Place(ctx, 0, 2, a, exhausted: true);
            Check(Board(ctx, 0, 3).Attack, 1, "（前提）监听者 1 攻");
            ctx.ClearSignals();
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "S14_SwMergedA"), 1), RuleCodes.OK,
                      "把同名的虫群部队打到它**左边**（触发合并）");
            Check(S14Sig(ctx, EvtKind.Trigger, KeywordTable.Swarm), 1,
                  "★ 【发什么·**事件**那一半】**恰好一条** `EvtKind.Trigger{swarm}`"
                + "（⚠️ 这一半 **2026-09-30 就补过**、`TestSwarm` 第 ① 组一直盯着 —— ⛔ 不是本笔新增的行为）");
            Check(S14SigSlot(ctx, EvtKind.Trigger, KeywordTable.Swarm), 2,
                  "★ ……格位 = **合并后还活着的那一格**（宿主留在 2 号格）"
                + "｜🧨 写成被清空的左边那一格（1）⇒ 本行红（表现层会查不到单位视图）");
            Check(Board(ctx, 0, 3).Attack, 3,
                  "★ 【发什么·**广播**那一半】`When a friendly unit triggers Swarm` 的监听者被叫醒"
                + "（1 → 3 攻）—— 两半**合在一起**才说明 `FireTriggerAlways` 那个口没缺一条腿"
                + "｜🧨 把它换成「只 `ctx.Emit`、不广播」⇒ 本行红（而上面那条**照旧绿**）");
        }

        // ---- ② 次序判别式：**先发事件、后广播**（原版次序）----
        // 监听卡的正文换成会**再发一条信号**的那一种（`deal 1 damage` ⇒ `EvtKind.Hit`），
        // 于是两条信号在 `ctx.Signals` 里的**下标先后**就能量出来：
        //   · 现在的写法（`FireTriggerAlways`：先 `ctx.Emit`、后 `BroadcastWhen`）⇒ swarm 在前；
        //   · 旧写法（先广播、后发事件）⇒ Hit 在前。
        {
            var watcher = new CardDef("S14_SwWatchDmg", "S14_SwWatchDmg", "unit",
                                      "When a friendly unit triggers Swarm, deal 1 damage to an enemy troop",
                                      "common", "Test", 1, 0, 9, 0, null, subtype: "Infantry");
            var a = SwarmGuy("S14_SwMergedB", 2, 3);
            var b = SwarmGuy("S14_SwMergedB", 2, 3);
            var ctx = ProbeBattle(new[] { b }, new[] { Unit("S14_SwFoe2", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 3, watcher, exhausted: true);
            Place(ctx, 0, 2, a, exhausted: true);
            var foe = Place(ctx, 1, 3, Unit("S14_SwFoe2", 1, 0, 9), exhausted: true);
            Check(foe.Health, 9, "（前提）对面 9 血");
            ctx.ClearSignals();
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "S14_SwMergedB"), 1), RuleCodes.OK,
                      "再触发一次虫群合并");
            Check(foe.Health, 8, "★ 广播真的被**结算**了（对面 9 → 8 血）—— 监听卡的正文跑了一遍");
            int swIdx = S14SigIndex(ctx, EvtKind.Trigger, KeywordTable.Swarm);
            int hitIdx = S14SigIndexAny(ctx, EvtKind.Hit);
            CheckTrue(swIdx >= 0 && hitIdx >= 0 && swIdx < hitIdx,
                      "★ **次序判别式**：`Trigger{swarm}` 的**下标 &lt; 第一条 `Hit`** "
                    + "（= **先发事件、后广播结算**）—— 判据 = 原版 "
                    + "`BattleManagerSupport__BroadcastUnitSwarm.c:16-18` 先 `DamageSignal.Raise`、"
                    + "`:20-70` 再逐个 `TriggeredSwarm`"
                    + $"｜🧨 换回「先广播、后发事件」的旧次序 ⇒ 本行实得 swIdx={swIdx} / hitIdx={hitIdx} ⇒ 红");
        }
    }

    /// <summary>
    /// **`A1356` 刀②：`EmitBloodThirst` 补上的那半边（`BroadcastWhen(Triggers(BloodThirst), …)`）。**
    ///
    /// 落点 = `RuleCore.EmitBloodThirst` 尾部（`ctx.Emit(EvtKind.Trigger …bloodthirst…)` **之后**）。
    /// 那一处原来是**只有事件、没有广播**；本笔**照 `FireTriggerAlways`「没正文那一支」逐句同形手写**，
    /// ⛔ **不调 `FireTriggerAlways`** —— 因为原版那一跳**只演出、不结算**
    /// （`CardScript__ActivateBloodThirst` → `SendHighlightBloodThirstAction` →
    /// `CardScript__HighlightBloodThirst` → `BattleCardUI.HighlightBloodThirst` ＋ `DisplayTriggerAnim`，
    /// **全长没有 `OnTrigger`**）；调那个口会在「卡上真有 `BloodThirst:` 正文」那一天顺手把正文结算掉。
    ///
    /// **断什么**：① 第一次出手 ⇒ 事件 ＋ 广播两半齐（监听卡被叫醒）；
    ///   ② **第二次**出手 ⇒ **一条都不发**（原版判据 `+0x48 == 1`，只有第 1 次之后才亮）；
    ///   ③ 没有 `bloodthirst` 的单位出手 ⇒ 一条都不发；
    ///   ④ **只演出、不结算**：即使这个单位**账上真有一份 `bloodthirst` 正文**
    ///      （用 `GrantOps` 挂上去 —— 那是 `FxOps` 会认的那条路），触发时也**一点效果都不结算**。
    ///
    /// 🔴 **灭自证**：删掉那一句 `BroadcastWhen` ⇒ ① 的「广播」那行红、而「事件」那行**照旧绿**（两半各一条）；
    ///    把 `u.AttacksThisTurn != 1` 改成 `&lt; 1` ⇒ ② 红；把尾部两行换成
    ///    `FireTriggerAlways(ctx, u, KeywordTable.BloodThirst, p, slot)` ⇒ ④ 红。
    /// </summary>
    static void TestS14BloodThirstTriggerBothHalves()
    {
        var watcher = new CardDef("S14_BtWatcher", "S14_BtWatcher", "unit",
                                  "When a friendly unit triggers Blood Thirst, gain +2 Attack",
                                  "common", "Test", 1, 1, 9, 0, null, subtype: "Infantry");

        // ---- ① 第一次出手 ⇒ 事件 + 广播两半齐；② 第二次出手 ⇒ 不发 ----
        {
            var bt = new CardDef("S14_BtAtk", "S14_BtAtk", "unit", "", "common", "Test", 1, 2, 9, 0,
                                 new[] { KeywordTable.BloodThirst }, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { bt }, new[] { Unit("S14_BtFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            int atkP = ctx.Active, foeP = 1 - ctx.Active;
            var atk = Place(ctx, atkP, 3, bt, exhausted: false);
            var w = Place(ctx, atkP, 2, watcher, exhausted: true);
            Place(ctx, foeP, 3, Unit("S14_BtFoe", 1, 0, 9), exhausted: true);
            CheckTrue(atk.Has(KeywordTable.BloodThirst), "（前提）攻击者带 `bloodthirst`");
            Check(w.Attack, 1, "（前提）监听者 1 攻");

            ctx.ClearSignals();
            CheckCode(RuleCore.DeclareAttack(ctx, atkP, 3, foeP, 3), RuleCodes.OK, "第一次攻击");
            Check(atk.AttacksThisTurn, 1, "（前提）记账到 1（这正是原版 `+0x48 == 1` 那一格）");
            Check(S14Sig(ctx, EvtKind.Trigger, KeywordTable.BloodThirst), 1,
                  "★ ① 【发什么·**事件**那一半】恰好一条 `EvtKind.Trigger{bloodthirst}`");
            Check(S14SigWho(ctx, EvtKind.Trigger, KeywordTable.BloodThirst), atkP,
                  "★ ① ……报的是**攻击者那一方**（⛔ 不是 `OwnerOf` 反查）");
            Check(S14SigSlot(ctx, EvtKind.Trigger, KeywordTable.BloodThirst), 3,
                  "★ ① ……格位 = 攻击者那一格（3）");
            Check(w.Attack, 3,
                  "★ ① 【发什么·**广播**那一半】`When a friendly unit triggers Blood Thirst` 的监听者被叫醒"
                + "（1 → 3 攻）"
                + "｜🧨 删掉 `EmitBloodThirst` 尾部那句 "
                + "`BroadcastWhen(WhenEventKind.Triggers(BloodThirst), …)` ⇒ 本行红"
                + "（而上面那条「事件」**照旧绿** —— 两半各有一条独立的判别式）");

            // ---- ② 第二次出手（嗜血单位 `atkLimit = 2`）⇒ 【不发】----
            ctx.ClearSignals();
            CheckCode(RuleCore.DeclareAttack(ctx, atkP, 3, foeP, 3), RuleCodes.OK,
                      "第二次攻击（`DeclareAttack` 里 `atkLimit = Has(\"bloodthirst\") ? 2 : 1`）");
            Check(atk.AttacksThisTurn, 2, "（前提）记账到 2");
            Check(S14Sig(ctx, EvtKind.Trigger, KeywordTable.BloodThirst), 0,
                  "★ ② 【什么时候不发】原版判据是 `+0x48 == 1`（**只有第 1 次之后才亮**）"
                + "⇒ 第二次**一条都不发**"
                + "｜🧨 把 `EmitBloodThirst` 里那句 `u.AttacksThisTurn != 1` 放宽成 `&lt; 1` ⇒ 本行实得 1");
            Check(w.Attack, 3, "★ ② ……监听者也**没有**被再叫醒一次（还是 3 攻）");
        }

        // ---- ③ 没有 `bloodthirst` 的单位出手 ⇒ 一条都不发 ----
        {
            var ctx = ProbeBattle(new[] { Unit("S14_NoBtAtk", 1, 2, 9) }, new[] { Unit("S14_BtFoe2", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            int atkP = ctx.Active, foeP = 1 - ctx.Active;
            var atk = Place(ctx, atkP, 3, Unit("S14_NoBtAtk", 1, 2, 9), exhausted: false);
            var w = Place(ctx, atkP, 2, watcher, exhausted: true);
            Place(ctx, foeP, 3, Unit("S14_BtFoe2", 1, 0, 9), exhausted: true);
            CheckTrue(!atk.Has(KeywordTable.BloodThirst), "（前提）这个没带 `bloodthirst`");
            ctx.ClearSignals();
            CheckCode(RuleCore.DeclareAttack(ctx, atkP, 3, foeP, 3), RuleCodes.OK, "普通部队打一下");
            Check(S14Sig(ctx, EvtKind.Trigger, KeywordTable.BloodThirst), 0,
                  "★ ③ 【什么时候不发】没有 `bloodthirst` ⇒ 一条都不发"
                + "｜🧨 把 `EmitBloodThirst` 开头那句 `u.Has(KeywordTable.BloodThirst)` 守卫删掉 ⇒ 本行实得 1");
            Check(w.Attack, 1, "★ ③ ……监听者也没被叫醒（还是 1 攻）");
        }

        // ---- ④ 「只演出、不结算」：账上真有一份 `bloodthirst` 正文时**也不结算** ----
        // 🔴 这一格用的是 `UnitState.GrantOps`（**运行时挂上去的正文** —— 正是 `FxOps` 会认的那条路），
        //    所以它与「`bloodthirst` 在不在 `RoutableTriggers` 里」**无关**：
        //    `EmitBloodThirst` 手写广播（不走 `FireTriggerAlways`）⇒ 正文不结算；一旦改成调那个口，
        //    `FxOps != null` ⇒ 转发 `FireTriggerAt` ⇒ 正文立刻结算 ⇒ 本格红。
        {
            var btPlain = new CardDef("S14_BtGranted", "S14_BtGranted", "unit", "", "common", "Test",
                                      1, 2, 9, 0, new[] { KeywordTable.BloodThirst }, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { btPlain }, new[] { Unit("S14_BtFoe3", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            int atkP = ctx.Active, foeP = 1 - ctx.Active;
            var atk = Place(ctx, atkP, 3, btPlain, exhausted: false);
            List<string> bun, bpa;
            var bodyOps = EffectText.Parse("Gain +3 Attack", out bun, out bpa);
            CheckTrue(bodyOps != null && bodyOps.Count > 0, "（前提）`Gain +3 Attack` 解析得出 op");
            atk.GrantOps(KeywordTable.BloodThirst, bodyOps, "Gain +3 Attack");
            CheckTrue(atk.FxOps(KeywordTable.BloodThirst) != null,
                      "（前提）账上那份 `bloodthirst` 正文**在**（`FxOps` 非 null）"
                    + " ⇒ `FireTriggerAlways` 若被调到，**一定会**转去结算它");
            Place(ctx, foeP, 3, Unit("S14_BtFoe3", 1, 0, 9), exhausted: true);
            int atk0 = atk.Attack;
            ctx.ClearSignals();
            CheckCode(RuleCore.DeclareAttack(ctx, atkP, 3, foeP, 3), RuleCodes.OK, "出手");
            Check(S14Sig(ctx, EvtKind.Trigger, KeywordTable.BloodThirst), 1,
                  "（前提）触发照样发出来了 —— **演出**那一半没缺");
            Check(atk.Attack, atk0,
                  "★ ④ **只演出、不结算**：账上挂着一份 `bloodthirst` 正文，触发时**攻一点没涨**"
                + "（判据 = 原版 `SendHighlightBloodThirstAction` 那一跳全长没有 `OnTrigger`）"
                + "｜🧨 把 `EmitBloodThirst` 尾部那两行换成 "
                + "`FireTriggerAlways(ctx, u, KeywordTable.BloodThirst, p, slot)` ⇒ 本行实得 "
                + $"{atk0 + 3}（正文被顺手结算了）");
        }
    }
}
