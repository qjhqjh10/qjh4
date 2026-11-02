// B117_WREPLAY2_Assert.cs — 执行代理 `B117-WREPLAY2`（2026-11-02）
//
// ============================ 它盯的是什么 ============================
//  三条「录像完整性 / 环境回落」缺口（判据 → `判据/B117-G10-16_回放与录像.md` §二 G1/G2 ·
//  `判据/B117-G10-02a_场地环境效果卡.md` §五 D6）：
//
//   · **B117-R62 跳过教程绕过三个记账口**：原版 `BattleManager__ClickSkip.c:42` = `DeadHero(bm, 0, 4)`。
//     我们那条 `BattleDriver.ApplyTutorialSkip` 直接 `Warlord.Health = 0` + `CheckWinner`，
//     不经过 `LocalAct` / `SimpleAI.Executed` / `TutorialScript.Executed` ⇒ 教程局点「跳过」之后
//     结算落盘的录像**回放必然报「不是同一局」**。改法 = 伪 kind `RecKindSkipTutorial(203)`。
//   · **B117-R63 回合初抽牌推迟与指纹分叉**：`TurnDrawDeferralEnabled = !Application.isBatchMode`
//     ⇒ 真机推迟；录制端 `RecAct(EndTurn)` 取 `StateHash` 时 post-wait 那一半**还没跑**，
//     而回放端 `_replaySession ⇒ DeferTurnDraw = false` ⇒ 当场跑完 ⇒ 逐动作指纹分叉
//     （批处理里推迟恒关 ⇒ **自检永远抓不到**）。改法 = 伪 kind `RecKindTurnDrawDeferred(204)`。
//   · **B117-R65 空卡（`Normal Conditions`）也要切本阵营默认环境**：原版 `_d__337__MoveNext` state 2
//     **无条件** `ApplyEnvEffect(卡)`；空卡分支 `GetEnviromentalEffect` 返回 `DefaultEnviromentalEffectVFX`。
//     我们那条早退把环境与 `ApplyEnvWarlordAbilityOnce()` **一起**跳过了。
//
// ============================ 灭自证（🧨 哪一处改动会让哪一条红）============================
//  🧨 A2：`ResolvePendingTurnDraw` 的**返回值**是录制端「要不要记这一条伪动作」的唯一判据 ——
//     改回 `void` / 恒返 false ⇒ A2 红（而 A3「幂等」照样绿，两条不可能同时被同一个错误满足）。
//  🧨 B2：`ApplyTutorialSkip` 里 `CheckWinner` 与 `RecRaw(RecKindSkipTutorial)` 的**先后**——
//     反序（照判据文件最初那句「在 CheckWinner 之前记」）⇒ `StateHash` 会采到「还没判负」那一格 ⇒ 本条红。
//  🧨 B4：录制端那一行必须是 `if (ResolvePendingTurnDraw(...)) RecRaw(...)`；把条件去掉（无论有没有
//     真补跑都记）⇒ 每回合都多一条伪动作 ⇒ 本条红。
//  🧨 B5：回放端打开推迟必须**按录像内容**判（`ReplayHasDeferredTurnDraw`）；改成无条件 true ⇒ 老录像红。
//  🧨 C2：空卡支里任何一条裸 `return;`（早退）⇒ 本条红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   宿主（`Editor/BattleScene.cs`）里加一行：
//     `B117_WREPLAY2_Assert.Run(); _pass += B117_WREPLAY2_Assert.Pass; _fail += B117_WREPLAY2_Assert.Fail;`
//  本件**无副作用**：A 组只建裸 `BattleContext`（不建 GameObject、不碰存档、不读真实卡池）；
//  B/C 组只读源码文本；C5 只读 `OffensiveCards.json` / `EnvironmentConditions.json` 两张数据表。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B117_WREPLAY2`：录像两条伪 kind + 空卡环境回落。</summary>
public static class B117_WREPLAY2_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string P = "B117WREPLAY2";
    const string DriverPath = "CardPresentation/Battle/BattleDriver.cs";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + " ✓ " + msg); }
        else { _fail++; Debug.LogError(P + " ✗ " + msg); }
    }

    static void Group(string name, Action body)
    {
        try { body(); }
        catch (Exception e)
        {
            Check(false, name + "：**这一段抛异常**（后面的断言没跑到，⛔ 别读成通过）—— "
                       + e.GetType().Name + ": " + e.Message);
        }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        Group("A·ResolvePendingTurnDraw 的返回值（R63 的判据接口）", A_ResolveReturnsBool);
        Group("B·源级：两条伪 kind 的接线（R62 / R63）", B_PseudoKindWiring);
        Group("C·空卡也要切本阵营默认环境（R65）", C_EmptyOffensiveEnv);
        Debug.Log(P + " 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ================================================================
    //  A 引擎：`RuleCore.ResolvePendingTurnDraw` 现在返回「这一趟有没有真补跑」
    //    录制端 `HoldForTurnHeadsUp` 靠它决定**要不要记一条伪动作**（B117-R63）。
    // ================================================================
    static void A_ResolveReturnsBool()
    {
        var ctx = NewCtx();
        Check(ctx != null && ctx.Players[0] != null && ctx.Players[1] != null, "A0（前提）夹具建得起来");
        if (ctx == null || ctx.Players[0] == null) return;
        Check(ctx.Active == 0, "A0b（前提）行动方 = 座位 0（`FirstSeat`）");

        // ---- 正例：推迟 ⇒ 手牌不变；补跑口返回 **true** 且手牌 +1 ----
        ctx.DeferTurnDraw = true;
        ctx.DeferSeat = 0;
        int hand0 = ctx.Players[0].Hand.Count;
        RuleCore.BeginTurn(ctx);
        Check(ctx.Players[0].Hand.Count == hand0, "A1（前提）推迟真的发生了：`BeginTurn` 返回时手牌**没变**");
        Check(ctx.PendingTurnDraw, "A1b（前提）`PendingTurnDraw` = 真");

        bool ran = RuleCore.ResolvePendingTurnDraw(ctx);
        Check(ran, "★ A2 `ResolvePendingTurnDraw` **返回 true** —— 录制端靠这一格判断「这一半真的补跑了、要记一条伪动作」"
                 + "｜🧨 改回 `void` / 恒返 false ⇒ 录制端永远不记 ⇒ 本条红");
        Check(ctx.Players[0].Hand.Count == hand0 + 1, "A2b 补跑之后手牌 +1（post-wait 那半确实跑了）");
        Check(!ctx.PendingTurnDraw, "A2c 补完就清闸");

        Check(!RuleCore.ResolvePendingTurnDraw(ctx), "★ A3 再调一次返回 **false**（幂等）——"
                 + " 回放里「抬头排期」与「伪动作」两处都会调它，不幂等就会多记一条 ⇒ 本条与 A2 结构上互补");
        Check(ctx.Players[0].Hand.Count == hand0 + 1, "A3b 重复调用不多抽");

        // ---- 负例①：座位对不上（换边撞进那 1.5 s）⇒ 不补、返回 false ----
        var c2 = NewCtx();
        c2.DeferTurnDraw = true;
        c2.DeferSeat = 0;
        int h2 = c2.Players[0].Hand.Count;
        RuleCore.BeginTurn(c2);
        c2.DeferSeat = 1;                       // 排期时记的是 P1，现在对不上
        Check(!RuleCore.ResolvePendingTurnDraw(c2), "A4 座位对不上 ⇒ 返回 **false**（那一抽没补上，`ctx.Log` 已出声）");
        Check(c2.Players[0].Hand.Count == h2, "A4b 座位对不上时**不多抽一张**（宁可少一张也不静默补错人）");

        // ---- 负例②：压根没推迟 / 空参 ----
        var c3 = NewCtx();
        int h3 = c3.Players[0].Hand.Count;
        RuleCore.BeginTurn(c3);                 // `DeferTurnDraw` 默认关
        Check(c3.Players[0].Hand.Count == h3 + 1, "A6 对照（灭自证）：开关关着 ⇒ 当场抽 1 张（回放/联机的同步老路）");
        Check(!RuleCore.ResolvePendingTurnDraw(c3), "A6b 没推迟时补跑口返回 false（录制端因此不会记一份空伪动作）");
        Check(!RuleCore.ResolvePendingTurnDraw(null), "A5 空参返回 false（不抛）");
    }

    // ================================================================
    //  B 源级：录制端 / 回放端两侧的伪 kind
    // ================================================================
    static void B_PseudoKindWiring()
    {
        string path = Path.Combine(Application.dataPath, DriverPath);
        Check(File.Exists(path), "B0（前提）读得到 `" + DriverPath + "`（读不到时本组之后全是空判）");
        if (!File.Exists(path)) return;

        var live = new List<string>();
        foreach (var l0 in File.ReadAllLines(path))
        {
            string l = l0.TrimStart();
            if (l.StartsWith("//")) continue;         // 活代码行才数（留痕注释里提到名字是故意的）
            live.Add(l);
        }
        string src = string.Join("\n", live.ToArray());

        // B1 两条伪 kind 的取值：203 / 204，与既有 200 / 201 / 202 互不相同
        var vals = new Dictionary<string, int>();
        foreach (Match m in Regex.Matches(src, @"const int (RecKind\w+) = (\d+);"))
            vals[m.Groups[1].Value] = int.Parse(m.Groups[2].Value);
        Check(vals.ContainsKey("RecKindSkipTutorial") && vals.ContainsKey("RecKindTurnDrawDeferred"),
              "B1 两条新伪 kind 都在（`RecKindSkipTutorial` / `RecKindTurnDrawDeferred`）");
        if (!vals.ContainsKey("RecKindSkipTutorial") || !vals.ContainsKey("RecKindTurnDrawDeferred")) return;
        Check(vals["RecKindSkipTutorial"] != vals["RecKindTurnDrawDeferred"]
              && vals["RecKindSkipTutorial"] != 200 && vals["RecKindSkipTutorial"] != 201 && vals["RecKindSkipTutorial"] != 202
              && vals["RecKindTurnDrawDeferred"] != 200 && vals["RecKindTurnDrawDeferred"] != 201 && vals["RecKindTurnDrawDeferred"] != 202,
              "B1b 与既有的 200（投降）/ 201（脚本抽牌）/ 202（脚本改打法）**互不相同**"
            + "（实得 " + vals["RecKindSkipTutorial"] + " / " + vals["RecKindTurnDrawDeferred"] + "）");

        // B2 🧨 录制端「跳过教程」：`CheckWinner` **之后**才记（`StateHash` 含 `Winner`）
        string tut = Window(live, "void ApplyTutorialSkip()", "ApplyTutorialSkipForTest");
        Check(tut.Length > 0, "B2a（前提）抓得到 `ApplyTutorialSkip` 的活代码段");
        int cw = tut.IndexOf("RuleCore.CheckWinner(Ctx)");
        int rr = tut.IndexOf("RecRaw(RecKindSkipTutorial");
        Check(cw >= 0 && rr > cw,
              "★ B2b 录制端：`CheckWinner` → `RecRaw(RecKindSkipTutorial)` 的**先后**（实得位置 " + cw + " / " + rr + "）"
            + "｜🧨 反序（在判负之前记）⇒ 采到「还没判负」那一格 ⇒ 回放侧对不上；与 `RecKindForfeit` 同一条纪律");

        // B3 回放端：跳过教程那一支 = 同两行（判死 → 判负），顺序同上
        string pseudo = Window(live, "bool ApplyReplayPseudoRaw(MsgAction m)", "namespace CardPresentation");
        Check(pseudo.Contains("RecKindSkipTutorial") && pseudo.Contains("RecKindTurnDrawDeferred"),
              "B3a 回放端认得这两条伪 kind");
        int h0 = pseudo.IndexOf("pw.Health = 0");
        int cw2 = pseudo.IndexOf("RuleCore.CheckWinner(Ctx)");
        Check(h0 >= 0 && cw2 > h0, "B3b 回放端同一句：督军判死 → `CheckWinner`（顺序与录制端一致）");

        // B4 🧨 录制端「推迟的那一半」：只在**真补跑**了才记
        Check(src.Contains("if (RuleCore.ResolvePendingTurnDraw(Ctx)) RecRaw(RecKindTurnDrawDeferred, Ctx.Active);"),
              "★ B4 录制端 `if (ResolvePendingTurnDraw(...)) RecRaw(RecKindTurnDrawDeferred, ...)` ——"
            + " 没真补跑就不记（否则每回合多一条伪动作）｜🧨 去掉那个条件 ⇒ 本条红");
        string hold = Window(live, "void HoldForTurnHeadsUp()", "public const float TurnHeadsUpHold");
        int nCalls = Regex.Matches(hold, Regex.Escape("RuleCore.ResolvePendingTurnDraw(Ctx)")).Count;
        int nRecorded = Regex.Matches(hold, Regex.Escape("RecRaw(RecKindTurnDrawDeferred")).Count;
        Check(nCalls == 1 && nRecorded == 1,
              "B4b 录制端那**唯一**一处补跑调用**就是**带记录的那一行（调用 " + nCalls + " 处 / 记录 " + nRecorded + " 处）"
            + "｜🧨 另起一处裸调用（补跑了却不记）⇒ 本条红");

        // B5 🧨 回放端：按录像内容决定要不要打开推迟（老录像保持老口径）
        Check(src.Contains("if (ReplayHasDeferredTurnDraw(rec)) { Ctx.DeferTurnDraw = true; Ctx.DeferSeat = _me; }"),
              "★ B5 回放端只在录像里**有**那条伪 kind 时才打开 `DeferTurnDraw`"
            + "｜🧨 改成无条件 true ⇒ 批处理里录的（两端本来都不推迟）老录像反而分叉");
        string has = Window(live, "static bool ReplayHasDeferredTurnDraw(ReplayRecord rec)", "bool ApplyReplayPseudoRaw(MsgAction m)");
        Check(has.Contains("RecKindTurnDrawDeferred") && has.Contains("return true") && has.Contains("return false"),
              "B5b `ReplayHasDeferredTurnDraw` 是按 kind 扫的（不是恒真 / 恒假）");

        // B6 🧨 伪 kind 必须排在 `ApplyLoggedAction` **之前**（`NetApply` 不认这两个 kind）
        int iPseudo = live.FindIndex(x => x.Contains("if (ApplyReplayPseudoRaw(m))"));
        int iLogged = live.FindIndex(x => x.Contains("int code = ApplyLoggedAction(m);"));
        Check(iPseudo >= 0 && iLogged > iPseudo,
              "★ B6 伪 kind 的分派排在 `ApplyLoggedAction` 之前（实得行 " + iPseudo + " / " + iLogged + "）"
            + "｜🧨 放到后面 ⇒ 被引擎拒 + 打一条假错");

        // B7 新成员在类尾（本文件有大量按 `文件:行号` 的账落点，插中间会让下游整体漂移）
        int iConst = live.FindIndex(x => x.Contains("const int RecKindSkipTutorial"));
        int iLast = live.FindIndex(x => x.Contains("void TickEnvPickCountdown("));
        Check(iConst > iLast && iConst > 0, "B7 两条常量追加在**类尾**（`TickEnvPickCountdown` 之后；实得 " + iConst + " / " + iLast + "）");
    }

    // ================================================================
    //  C 源级 + 数据：空卡（`Normal Conditions`）也要切本阵营默认环境
    // ================================================================
    static void C_EmptyOffensiveEnv()
    {
        string path = Path.Combine(Application.dataPath, DriverPath);
        Check(File.Exists(path), "C0（前提）读得到 `" + DriverPath + "`");
        if (!File.Exists(path)) return;
        var live = new List<string>();
        foreach (var l0 in File.ReadAllLines(path))
        {
            string l = l0.TrimStart();
            if (l.StartsWith("//")) continue;
            live.Add(l);
        }
        string src = string.Join("\n", live.ToArray());

        string body = Window(live, "void ApplyOffensiveEnvOnce()", "void BeginOffensiveRevealOrApply");
        Check(body.Length > 0, "C1（前提）抓得到 `ApplyOffensiveEnvOnce` 的主体（到「战场侧接上了」那段注释为止）");

        // C2 🧨 空卡支里不许再早退
        string empty = Window(live, "bool useCard = ApplyOffensiveButtonVisibility();", "ApplyEnvWarlordAbilityOnce");
        Check(empty.Length > 0, "C2a（前提）抓得到 `if (!useCard)` 那一段");
        Check(!Regex.IsMatch(empty, @"(^|\n)\s*return;"),
              "★ C2 空卡那一条**不再早退**（段内 `return;` 0 处）｜🧨 加回 `return;` ⇒ 环境与督军触发又一起被跳过 ⇒ 本条红");

        // C3 回落 = 本阵营的 default SO（与 `EnvSOFor` 同一条判据：`OffensiveCards.For(FactionOf(SecondSeat))`）
        Check(empty.Contains("OffensiveCards.For(FactionOf(Ctx.SecondSeat))") && empty.Contains("defaultEnvSO"),
              "C3 空 SO 时回落到**本阵营默认环境**（`defaultEnvSO`，与 `EnvSOFor` 的空卡分支同一条判据）");

        // C4 早退没了 ⇒ 后面那两步仍会走（督军触发 + 换环境）
        int iWarlord = body.IndexOf("ApplyEnvWarlordAbilityOnce()");
        int iApply = body.IndexOf("_envApplier.Apply(envItem, instant: false)");
        Check(iWarlord > 0 && iApply > iWarlord,
              "C4 空卡这一支之后照样走到「督军触发」与「换环境」（原版 `ApplyEnvEffect.c:14-15` 把两步包在同一个 `if` 里）："
            + " 位置 " + iWarlord + " / " + iApply);

        // C5 数据层：13 个阵营都有默认环境 SO，且它真的在 `EnvironmentConditions.json` 里
        //    （否则「回落」只是把 `so` 换成一个查不到的名字 ⇒ 仍然不切环境，只是改成了打一条 warning）
        var armies = OffensiveCards.All;
        Check(armies != null && armies.Length > 0, "C5（前提）`OffensiveCards.json` 读得到（" + (armies == null ? 0 : armies.Length) + " 个阵营）");
        if (armies == null) return;
        int bad = 0, total = 0;
        var missing = new List<string>();
        foreach (var a in armies)
        {
            if (a == null) continue;
            total++;
            if (string.IsNullOrEmpty(a.defaultEnvSO)) { bad++; missing.Add(a.army + "：defaultEnvSO 空"); continue; }
            if (EnvironmentConditions.Find(a.defaultEnvSO) == null) { bad++; missing.Add(a.army + "：查不到"); }
        }
        Check(total > 0 && bad == 0,
              "★ C5 每个阵营的默认环境 SO 都在 `Resources/EnvironmentConditions.json` 里（" + total + " 个阵营 / 查不到 "
            + bad + " 个" + (bad > 0 ? "：" + string.Join(" · ", missing.ToArray()) : "") + "）"
            + "｜🧨 缺一条 ⇒ 那个阵营的「Normal Conditions」仍然不换环境（只是多一条 warning）");
    }

    // ================================================================
    //  小工具
    // ================================================================
    /// <summary>按「起止串」从**活代码行**里切一段（起点那行算进去，终点之前那行算进去）。
    /// 找不到起点 / 终点 ⇒ 返回空串（调用方必须自己出一条「前提」断言）。</summary>
    static string Window(List<string> live, string from, string to)
    {
        int a = live.FindIndex(x => x.Contains(from));
        if (a < 0) return "";
        int b = -1;
        for (int i = a + 1; i < live.Count; i++) { if (live[i].Contains(to)) { b = i; break; } }
        int end = b < 0 ? Math.Min(live.Count, a + 200) : b;
        var sb = new System.Text.StringBuilder();
        for (int i = a; i < end; i++) sb.Append(live[i]).Append('\n');
        return sb.ToString();
    }

    static CardDef Unit(string id)
        => new CardDef(id, id, "unit", "", null, "Test", 1, 0, 1, 0, null);

    static CardDef Hero()
        => new CardDef("B117WREPLAY2_Warlord", "B117WREPLAY2_Warlord", "hero", "", null, "Test", 0, 2, 30, 0, null,
                       subtype: "Warlord");

    /// <summary>裸夹具（与 `B104_D_Assert.NewCtx` 同形）：两边各一副纯 filler，`shuffle: false`、
    /// `cardPool` 空 ⇒ 建完 `Turn = 0` / `Active = FirstSeat`。</summary>
    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero() };
        for (int i = 0; i < 20; i++) d0.Add(Unit("filler" + i));
        var d1 = new List<CardDef> { Hero() };
        for (int i = 0; i < 20; i++) d1.Add(Unit("foefiller" + i));
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
    }
}
