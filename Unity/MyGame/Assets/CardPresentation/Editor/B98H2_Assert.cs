// B98H2_Assert.cs — 第九十八会话 · 执行代理 `B98_H2`（2026-10-29 · 波 7 · 引擎侧写手）
//
// ============================ 它盯的是什么 ============================
// 账 **254 / 255** 的**时序根治**：`ChoiceOfCardEnemy` / `ChoiceOfCardPlayer` 要「**开面板之前**把 options 算好」，
// 而我们的调用序是「面板（`BattleDriver.BeginPlay`）→ 答案 → 引擎结算（`TakePickCard` → `BuildChoiceOptions`）」。
//
// 本代理在 `RuleEngine/Core/EffectResolver.cs`（`partial class RuleCore`）落的是**引擎那一半**：
//   · `PeekChoiceOptions(ctx, owner, op, cands, source)` —— 开面板前**推演**出 options：只读预烧随机表
//     （`BattleContext.RandomNumbers[100]` + `RandomNumbersIndex`），**一格随机都不消耗**、不写 `ctx`、
//     不进 `ctx.Events`。
//   · `TakePeekedChoiceOptions`（`TakePickCard` 内部先试它）—— 结算时**复用**那份推演；
//     四条同等条件（同 op 实例 / 游标没动 / 候选逐项同 id 同序 / `maxTargets` 相同）成立时，
//     复用与「真抽」**逐位等价**（游标按推演消耗格数前推 + `SetChoiceOptions` + 那条 `ctx.Log` 原样补上）。
//     不成立 ⇒ 丢弃推演走真抽 + `Debug.LogWarning` 出声。
//
// ============================ 🔴 覆盖边界（如实记，⛔ 不假装盖住了）============================
//   · 这里断的是**引擎契约**（推演 = 真抽、不消耗、不复用错、不静默）。**面板那一行接线不在本代理白名单**
//     （`BattleDriver.ShowAsk`，属 `B98_H4`）⇒ 「面板真的摆出引擎那一份」要等那一行接上 + 真机看一眼。
//   · **残差（不是 100% 命中）**：这张卡若在「选牌那一步」之前还有消耗随机数的步骤、或结算时候选池与开面板
//     时不同 ⇒ 复用条件不成立 ⇒ 引擎真抽（面板摆的是推演那一份）⇒ 按 `Id` 回落。见 `H2e` / `H2j`。
//   · 本件**不改随机流**（不是「改指望去对齐」）：`H2d` 就是拿两个同种子的 ctx 走「复用」与「真抽」两条路
//     **逐位比**（候选 / 游标 / `Events.Count` / 最后一条日志 / `ctx.ChoiceOptions`）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   `B98H2_Assert.CheckB98H2();`（**无需 `BattleDriver`** —— 本件在**离线 ctx** 上做，⛔ 不碰真打的那局）。
//   建议接在宿主 `Run()` 的**方法尾聚合行**（同 `B97W3_Assert` / `B98G4_Assert` 的接法）。
//   ⚠️ 全程**自建 `BattleContext`**（`new BattleContext(seed)`），用完即弃：不改真打那一局的任何状态。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B98_H2` 的断言口（账 254 / 255 的「开面板前算好 options」引擎半）。</summary>
public static class B98H2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[H2] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    /// <summary>合成一张「有 `choicesDefinition`」的卡（只用到 `ChoicesMaxTargets`，其余字段随便但要能构造）。</summary>
    static CardDef MakeCard(string id, int maxTargets)
    {
        return new CardDef(id, "T_" + id, "spell", "Choose a troop", "common", "Test",
                           0, 0, 0, 0,
                           choicesTargetsAffected: 180, choicesMaxTargets: maxTargets);
    }

    static List<CardDef> MakeCands(params string[] ids)
    {
        var l = new List<CardDef>();
        for (int i = 0; i < ids.Length; i++) l.Add(MakeCard(ids[i], 3));
        return l;
    }

    static EffectOp MakeOp(string tag)
    {
        return new EffectOp { Verb = "choosecard", Source = "H2:" + tag, ChooseSrc = "specific" };
    }

    static string Ids(List<CardDef> l)
    {
        if (l == null) return "<null>";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < l.Count; i++) { if (i > 0) sb.Append(','); sb.Append(l[i] != null ? l[i].Id : "<null>"); }
        return sb.ToString();
    }

    /// <summary>唯一入口。</summary>
    public static void CheckB98H2()
    {
        MenuCheck.Section(_sink, "H2：账 254 / 255 —— 开面板前算好 options（引擎半）");
        CheckPeekIsFree();
        CheckPeekMirrorsRealDraw();
        CheckReuseIsBitIdentical();
        CheckMismatchFallsBack();
        CheckNoOptionsWhenLimitIsZero();
        CheckIdempotentAndCopies();
    }

    // ==================================================================
    //  H2a / H2b：推演**一格随机都不消耗**、不写 ctx、不进 Events
    // ==================================================================
    static void CheckPeekIsFree()
    {
        var ctx = new BattleContext(77001);
        var cands = MakeCands("c1", "c2", "c3", "c4", "c5");
        var op = MakeOp("free");

        Tr(cands[0].ChoicesMaxTargets == 3, "H2a（前提）合成卡的 `ChoicesMaxTargets` 真的是 3（⛔ 读不到就红，不静默跳过）");
        if (cands[0].ChoicesMaxTargets != 3) return;

        int idx0 = ctx.RandomNumbersIndex;
        int ev0 = ctx.Events.Count;
        int cs0 = ctx.ChooseSites;
        int ca0 = ctx.ChooseAnswered;

        var opts = RuleCore.PeekChoiceOptions(ctx, 0, op, cands, cands[0]);

        C(opts.Count, 3, "H2b① ★ 推演的 options 张数 = `min(池 5, maxTargets 3)` = 3");
        C(ctx.RandomNumbersIndex, idx0, "H2b② ★ 推演**一格随机都没消耗**（`RandomNumbersIndex` 不变）");
        C(ctx.Events.Count, ev0, "H2b③ ★ 推演**不写 `ctx.Events`**（否则只有本地那一侧跑面板 ⇒ A/B 两端 `Fingerprint` 不等）");
        C(ctx.ChooseSites, cs0, "H2b④ 推演不计数 `ChooseSites`（那一格仍然只在结算里 ++）");
        C(ctx.ChooseAnswered, ca0, "H2b⑤ 推演不计 `ChooseAnswered`");
        C(ctx.ChoiceOptions.Count, 0, "H2b⑥ 推演**不写** `ctx.ChoiceOptions`（写入口仍只有 `BuildChoiceOptions` 那一条）");
        Tr(ctx.ChoiceOptionsSource == null, "H2b⑦ 推演不设 `ChoiceOptionsSource`（面板据此判「这份是不是我问的那一次」）");
        C(RuleCore.ChoicePeekCountForTest(ctx), 1, "H2b⑧ 推演挂上了 1 份（等结算来吃）");
        Tr(opts[0] != null && ctx.ChoiceOptions.Count == 0, "H2b⑨ 推演结果与 `ctx.ChoiceOptions` 是**两份**（返回的是给面板的副本）");
        RuleCore.ClearChoicePeek(ctx);
    }

    // ==================================================================
    //  H2c：推演 == 真抽（同一起点、逐项同序）
    // ==================================================================
    static void CheckPeekMirrorsRealDraw()
    {
        var ctx = new BattleContext(77002);
        var cands = MakeCands("c1", "c2", "c3", "c4", "c5");
        var op = MakeOp("mirror");

        int ev0 = ctx.Events.Count;
        var peeked = RuleCore.PeekChoiceOptions(ctx, 0, op, cands, cands[0]);
        int idxAfterPeek = ctx.RandomNumbersIndex;
        var real = RuleCore.BuildChoiceOptions(ctx, cands, 3);     // 同一起点（推演没消耗）真抽一遍

        C(Ids(real), Ids(peeked), "H2c① ★★ 推演与真抽**逐项同序同 id**（镜像 `GetRandomInt` 的口径对不对，这条是主判据）");
        Tr(ctx.RandomNumbersIndex > idxAfterPeek, "H2c② 真抽确实推进了游标（推演没有 ⇒ 起点就是推演的起点）");
        C(ctx.Events.Count - ev0, 1, "H2c③ 真抽写了 1 条 `ctx.Log`（推演一条都没写 ⇒ 计数差就是这一条）");
        RuleCore.ClearChoicePeek(ctx);
    }

    // ==================================================================
    //  H2d：结算**复用**推演 == 真抽（候选 / 游标 / Events / 日志 / ctx.ChoiceOptions 全逐位相同）
    // ==================================================================
    static void CheckReuseIsBitIdentical()
    {
        var cands = MakeCands("c1", "c2", "c3", "c4", "c5");
        var op = MakeOp("reuse");

        var a = new BattleContext(77003);      // 走「面板推演 → 结算复用」
        var b = new BattleContext(77003);      // 走「结算真抽」（= 没跑面板的远端 / 重放）
        a.PlayingCard = cands[0];              // 结算期 `ChoiceLimitOf(ctx)` 读的就是它（复用条件 ④ 要复核这一格）
        b.PlayingCard = cands[0];

        var peeked = RuleCore.PeekChoiceOptions(a, 0, op, cands, cands[0]);
        Tr(peeked.Count == 3, "H2d①（前提）推演出了 3 张");
        var reused = RuleCore.TakePeekedChoiceOptionsForTest(a, 0, op, cands);

        Tr(reused != null, "H2d② ★ 四条同等条件成立时**确实复用**了（返回非 null）");
        if (reused == null) return;

        var drawn = RuleCore.BuildChoiceOptions(b, cands, 3);

        C(Ids(reused), Ids(drawn), "H2d③ ★★ 复用结果与真抽**逐项同序同 id**");
        C(a.RandomNumbersIndex, b.RandomNumbersIndex, "H2d④ ★★ 复用后**随机表游标与真抽逐位相同**（= 不是「少消耗/多消耗」）");
        C(a.Events.Count, b.Events.Count, "H2d⑤ ★★ 复用与真抽的 `ctx.Events` 条数相同（`NetProtocol.Fingerprint` 吃这一格）");
        Tr(a.Events.Count > 0 && b.Events.Count > 0 && a.Events[a.Events.Count - 1] == b.Events[b.Events.Count - 1],
           "H2d⑥ ★ 复用补的那条日志与真抽**逐字相同**（⛔ 别在这里少写一句，少一句就是 A/B 两端 `Events.Count` 不等）");
        C(a.ChoiceOptions.Count, b.ChoiceOptions.Count, "H2d⑦ `ctx.ChoiceOptions` 的份数一致");
        C(Ids(new List<CardDef>(a.ChoiceOptions)), Ids(new List<CardDef>(b.ChoiceOptions)),
          "H2d⑧ `ctx.ChoiceOptions` **逐项同序**（面板读的就是这一份）");
        C(RuleCore.ChoicePeekCountForTest(a), 0, "H2d⑨ 复用后推演**被吃掉**（一次 ask 只吃一份，⛔ 别留着被下一问重放）");
        RuleCore.ClearChoicePeek(a);
        RuleCore.ClearChoicePeek(b);
    }

    // ==================================================================
    //  H2e：条件对不上 ⇒ **丢弃推演、走真抽**（出声，⛔ 不许静默复用）
    // ==================================================================
    static void CheckMismatchFallsBack()
    {
        // ① 候选表变了
        var candsA = MakeCands("c1", "c2", "c3", "c4", "c5");
        var candsB = MakeCands("c1", "c2", "c3", "c4", "cZ");
        var op1 = MakeOp("mismatch-cands");
        var ctx1 = new BattleContext(77004);
        ctx1.PlayingCard = candsA[0];                      // 让「上限」这一条**相同**，差异只落在候选表上
        RuleCore.PeekChoiceOptions(ctx1, 0, op1, candsA, candsA[0]);
        int idx1 = ctx1.RandomNumbersIndex;
        var t1 = RuleCore.TakePeekedChoiceOptionsForTest(ctx1, 0, op1, candsB);
        Tr(t1 == null, "H2e① ★ 候选表变了 ⇒ **不复用**（返回 null，调用方走真抽）");
        C(ctx1.RandomNumbersIndex, idx1, "H2e② 不复用时**游标一格不动**（真抽在调用方那边发生）");
        C(RuleCore.ChoicePeekCountForTest(ctx1), 0, "H2e③ 那份对不上的推演被**丢掉**了（不会挂着被下一问误吃）");

        // ② 游标动过了（这张卡在选牌那一步之前还消耗过随机）
        var op2 = MakeOp("mismatch-cursor");
        var ctx2 = new BattleContext(77005);
        ctx2.PlayingCard = candsA[0];                      // 同上：差异只落在「游标动过」这一条上
        RuleCore.PeekChoiceOptions(ctx2, 0, op2, candsA, candsA[0]);
        ctx2.GetRandomInt(0, 100);                        // 模拟「前面一步先抽了一次」
        int idx2 = ctx2.RandomNumbersIndex;
        var t2 = RuleCore.TakePeekedChoiceOptionsForTest(ctx2, 0, op2, candsA);
        Tr(t2 == null, "H2e④ ★ 游标动过了 ⇒ **不复用**（起点不同 ⇒ 真抽才是对的）");
        C(ctx2.RandomNumbersIndex, idx2, "H2e⑤ 这一支同样不动游标");
        C(RuleCore.ChoicePeekCountForTest(ctx2), 0, "H2e⑥ 同样被丢掉（账 254/255 的已知残差，出声在 `Debug.LogWarning`）");

        // ③ owner 不同
        var op3 = MakeOp("mismatch-owner");
        var ctx3 = new BattleContext(77006);
        ctx3.PlayingCard = candsA[0];
        RuleCore.PeekChoiceOptions(ctx3, 0, op3, candsA, candsA[0]);
        var t3 = RuleCore.TakePeekedChoiceOptionsForTest(ctx3, 1, op3, candsA);
        Tr(t3 == null, "H2e⑦ ★ 换了一侧（owner 不同）⇒ **不复用**（推演按 ask 的归属方认）");
        RuleCore.ClearChoicePeek(ctx1);
        RuleCore.ClearChoicePeek(ctx2);
        RuleCore.ClearChoicePeek(ctx3);
    }

    // ==================================================================
    //  H2f：`maxTargets` 缺值/0 ⇒ 不摆（与 `BuildChoiceOptions` 同口径），且**不缓存**
    // ==================================================================
    static void CheckNoOptionsWhenLimitIsZero()
    {
        var cands = MakeCands("c1", "c2", "c3");
        var zero = MakeCard("zero", 0);
        var op = MakeOp("limit0");
        var ctx = new BattleContext(77007);
        int ev0 = ctx.Events.Count;
        var none = RuleCore.PeekChoiceOptions(ctx, 0, op, cands, zero);
        C(none.Count, 0, "H2f① `maxTargets = 0` ⇒ 推演返回空表（面板回落到旧兜底）");
        C(RuleCore.ChoicePeekCountForTest(ctx), 0, "H2f② 空表**不缓存**（缓存了会让结算误以为「面板已经有答案」）");
        C(ctx.Events.Count, ev0, "H2f③ 这一档同样**不写日志**（真抽那一档会写；推演不写 ⇒ 不多出事件）");
        C(RuleCore.PeekChoiceOptions(ctx, 0, op, cands, null).Count, 0,
          "H2f④ `source == null` ⇒ 不摆（⛔ 别用 stale 的 `ctx.PlayingCard` 去猜上限）");
        C(RuleCore.ChoicePeekCountForTest(ctx), 0, "H2f⑤ 同样不缓存");
    }

    // ==================================================================
    //  H2g / H2i：幂等 + 返回的是**副本**（面板改它不会污染缓存）
    // ==================================================================
    static void CheckIdempotentAndCopies()
    {
        var cands = MakeCands("c1", "c2", "c3", "c4", "c5");
        var op = MakeOp("idem");
        var ctx = new BattleContext(77008);

        var p1 = RuleCore.PeekChoiceOptions(ctx, 0, op, cands, cands[0]);
        int idx0 = ctx.RandomNumbersIndex;
        var p2 = RuleCore.PeekChoiceOptions(ctx, 0, op, cands, cands[0]);
        C(Ids(p2), Ids(p1), "H2g① ★ 同一个 ask 再问一次 ⇒ **同一份**（面板重摆不问第二遍）");
        C(ctx.RandomNumbersIndex, idx0, "H2g② 也不重复推演（游标仍然不动）");
        C(RuleCore.ChoicePeekCountForTest(ctx), 1, "H2g③ 缓存里仍然只有 1 份（不是越问越多）");

        Tr(!ReferenceEquals(p1, p2), "H2i① ★ 两次返回的是**两份对象**（面板就地 `Sort` 不会污染缓存）");
        if (p1.Count > 0) p1[0] = null;
        var p3 = RuleCore.PeekChoiceOptions(ctx, 0, op, cands, cands[0]);
        Tr(p3.Count > 0 && p3[0] != null, "H2i② ★ 面板把返回的表改坏了，缓存里那份**不受影响**");

        C(RuleCore.PeekChoiceOptions(null, 0, op, cands, cands[0]).Count, 0, "H2i③ `ctx == null` ⇒ 空表、不抛");
        C(RuleCore.PeekChoiceOptions(ctx, 0, null, cands, cands[0]).Count, 0, "H2i④ `op == null` ⇒ 空表、不抛");
        C(RuleCore.PeekChoiceOptions(ctx, 0, op, null, cands[0]).Count, 0, "H2i⑤ 候选 null ⇒ 空表、不抛");
        C(RuleCore.PeekChoiceOptions(ctx, 0, op, new List<CardDef>(), cands[0]).Count, 0, "H2i⑥ 候选空表 ⇒ 空表、不抛");
        RuleCore.ClearChoicePeek(ctx);
        Debug.Log(_sink.P + "H2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }
}
