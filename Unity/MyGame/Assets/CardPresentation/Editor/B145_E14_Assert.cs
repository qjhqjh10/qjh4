// B145_E14_Assert.cs — 执行写手 `B145-E14`（2026-11-05）· 施工单 `A-05`
//
// ============================ 它盯的是什么 ============================
// `SimpleAI.PlayTurn` 的**一步式**形状（原版 `AI.PlayTurn` 的真实语义），以及它被拆成
// `PlayTurnStep`（一步） + 调用方 `while` 之后，**行为必须逐位不变**：
//   A · 一步 = **最多一次** `ExecuteAction`（原版 `.c:28-30` 只调一次）
//   B · 收手 = 「最佳是 `endTurn` / 没动作」⇒ 返回 false 且**一次都不执行**
//   C · 等价：`PlayTurn`（兼容壳）与 `while (PlayTurnStep(...))` 打同一局 ⇒ **局面指纹相同**
//   D · 上限还在：`PlayTurnGuard == 64`，且 `PlayTurn` 真的用它
//   E · `PlayTurnStep` **体内没有循环**（原版 `AI__PlayTurn.c` 全文无循环；循环只许住在调用方）
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//  · `d:/2/tools/decomp_full/AI__PlayTurn.c`：`:19-41` 枚举 + `:35-40` 逐条打分 · `:24-25` Tweak + SelectBest
//    · `:28-30` **`actionType(+0x10) != 4`（endTurn）才 `ExecuteAction`** · `:31-33` **返回 `actionType != 4`**
//  · 循环住在调用方：`BattleManager_<PlayAi>d__399__MoveNext.c:207-208` 把返回值写回 `param_1 + 0x28`，
//    `:26-27` 的 `while (*(char *)(param_1 + 0x28) != '\0')` 才是那个循环；退出后 `:110-118` 等延迟 →
//    `:232-237`（`case 8`）的 `AddEndTurnAction`。
//  · 施工单：`资料/完全复刻/判据/B145_施工单汇总_离线.md` §二 `A-05`；报告 `资料/完全复刻/报告/B144_R2_AI.md` #60。
//
// ============================ 怎么用 ============================
//   `B145_E14_Assert.Run();`   （静态类 + 无参 `Run()`；**纯引擎 + 纯源码**，不建任何 GameObject）
//   ⚠️ **本件尚未接线** —— 建议宿主 = `CardPresentation/Editor/BattleScene.cs` 的聚合行
//      （同 `B120_W27_Assert.Run(); pass += …; fail += …` 那一族），由调度台在同步点接。
//
// ============================ 🔴 灭自证（改哪一处会让哪一条红）============================
//   · A2  ← `PlayTurnStep` 里再塞一次 `ExecuteAction`（或循环）⇒ 一步内计数变 2 ⇒ 红
//   · B2  ← 把「`endTurn` 也算一条动作、照样执行」加回来 ⇒ 无动作时返回 true ⇒ 红
//   · C1  ← 把 `PlayTurn` 改回自己的老循环（与 `PlayTurnStep` 各写一份）⇒ 两条路径迟早分叉 ⇒ 红
//   · C3  ← 夹具不确定（比如 AI 掷骰改走别的随机源）⇒ 同一局两次指纹不同 ⇒ 红（**先红的是"夹具"那条**）
//   · D1/D2 ← 去掉 64 上限 / 把上限改成别的数 ⇒ 红
//   · E1  ← 往 `PlayTurnStep` 里加 `for` / `while` / `foreach` ⇒ 红（那正是「一步式」被推翻）
//
// ============================ ⚠️ 覆盖边界（如实记）============================
//   · **不跑 Unity 帧循环**、不建 GameObject：只驱动引擎（`RuleCore` + `SimpleAI`）与读源码文本。
//   · **不证明「真机上 AI 回合的时序」**（那是 `BattleDriver.DriveAiTurn` 的事，本件不碰表现层）。
//   · C 组用 `NetProtocol.Fingerprint` 比局面；它**不含** `AiRng` 内部状态 —— 但两路都跑完一整回合、
//     逐条动作消费同一串随机数，所以它能抓到「动作流分叉」；抓不到「只差一次未用完的掷骰」。
using System;
using System.Collections.Generic;
using System.IO;
using RuleEngine;
using CardPresentation.Net;      // `NetProtocol.Fingerprint`
using UnityEngine;

/// <summary>执行写手 `B145-E14`：`SimpleAI.PlayTurn` 一步式（施工单 `A-05`）。</summary>
public static class B145_E14_Assert
{
    const string Prefix = "[B145E14] ";
    static int _ok, _bad;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _ok; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(Prefix + "✓ " + what); }
        else { _bad++; Debug.LogError(Prefix + "✗ " + what); }
    }

    static void CheckInt(int actual, int expect, string what)
    {
        Check(actual == expect, what + "（实得 " + actual + "，期望 " + expect + "）");
    }

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(Prefix + "=== B145-E14：`PlayTurn` 一步式（施工单 A-05）开始 ===");
        A_OneStepOneAction();
        B_NoActionStops();
        C_PathEquivalence();
        D_Guard();
        E_NoLoopInsideStep();
        Debug.Log(Prefix + "—— 合计：通过 " + _ok + " · 失败 " + _bad);
    }

    // ==================================================================
    //  夹具：一局打到「轮到座位 1（AI）出牌」的起始态
    // ==================================================================

    /// <summary>同一个 `seed` ⇒ 同一局（两张牌组、两条 `Random` 也固定）⇒ 夹具可复现。</summary>
    static BattleContext NewBattleAtAiTurn(int seed)
    {
        var deckA = DeckBuilder.StarterDeck(StarterCards.Ember(), StarterCards.EmberFaction, 30, new System.Random(1));
        var deckB = DeckBuilder.StarterDeck(StarterCards.Tide(), StarterCards.TideFaction, 30, new System.Random(2));
        var ctx = RuleCore.NewBattle(deckA, deckB, seed);
        RuleCore.BeginTurn(ctx);      // 座位 0 的回合
        RuleCore.EndTurn(ctx);
        RuleCore.BeginTurn(ctx);      // 轮到座位 1（AI）
        return ctx;
    }

    /// <summary>数 `ExecuteAction` 被调了几次（成功/被拒都算 —— 原版 `.c:28-30` 那一句就是一次）。</summary>
    sealed class ExecCounter : IDisposable
    {
        public int Count;
        readonly System.Action<BattleContext, AiAction, bool> _cb;
        public ExecCounter()
        {
            _cb = (c, a, ok) => Count++;
            SimpleAI.Executed += _cb;
        }
        public void Dispose() { SimpleAI.Executed -= _cb; }
    }

    // ==================================================================
    //  A · 一步 = 最多一次 `ExecuteAction`（原版 `.c:28-30` 只调一次，没有循环）
    // ==================================================================
    static void A_OneStepOneAction()
    {
        var ctx = NewBattleAtAiTurn(20261105);
        Check(ctx != null && ctx.Active == 1, "A0（前提）夹具确实停在**座位 1（AI）的回合**（`ctx.Active == 1`）");
        Check(ctx != null && SimpleAI.HasAnyAction(ctx),
              "A0b（前提）这个局面 AI **真的有动作可做**（否则 A 组全在测空表）");
        if (ctx == null || ctx.Active != 1 || !SimpleAI.HasAnyAction(ctx)) return;

        var rejected = new List<AiAction>();
        using (var c = new ExecCounter())
        {
            bool more = SimpleAI.PlayTurnStep(ctx, AiDifficulty.Hard, rejected);
            CheckInt(c.Count, more ? 1 : 0,
                "A1 ★ **一步 = 最多一次 `ExecuteAction`**（原版 `AI__PlayTurn.c:28-30` 只调一次）"
                + "｜🧨 `PlayTurnStep` 里再循环/多执行一次 ⇒ 2 ⇒ 红");
            Check(more, "A1b 这一步挑到了动作并走了出去（返回 true = 『还有得做』，原版 `.c:31-33`）");
        }

        // 连着走 3 步：每一步的增量都 ≤ 1（= 没有任何一步偷偷多执行）
        int total = 0, trueSteps = 0;
        for (int i = 0; i < 3 && !ctx.IsOver; i++)
        {
            using (var c = new ExecCounter())
            {
                bool more = SimpleAI.PlayTurnStep(ctx, AiDifficulty.Hard, rejected);
                Check(c.Count <= 1, "A2-[第 " + (i + 2) + " 步] 单步执行次数 ≤ 1（实得 " + c.Count + "）"
                                  + "｜🧨 一步里执行两条 ⇒ 红（原版一步只 `ExecuteAction` 一次）");
                if (!more) break;
                trueSteps++;
                total += c.Count;
            }
        }
        Check(trueSteps >= 1, "A2b 这一段里**至少走了 1 步**（否则 A2 的逐条都是空转）");
        Check(total == trueSteps, "A2c 每一步恰好执行 1 条（累计 " + total + " 次 / " + trueSteps + " 步）"
                                + "｜🧨 把 `NextAction` 与 `ExecuteAction` 拆成两次调用、中间没有对应关系 ⇒ 红");
    }

    // ==================================================================
    //  B · 没有动作 ⇒ 立刻收手，且**一次都不执行**（原版：最佳是 `endTurn` 就返回 false）
    // ==================================================================
    static void B_NoActionStops()
    {
        var ctx = NewBattleAtAiTurn(20261105);
        if (ctx == null) { Check(false, "B0（前提）夹具建不出来"); return; }

        // 把 AI 一侧做成「真的没事可做」：手牌清空 + 场上每个单位都行动完（含督军）
        ctx.Players[1].Hand.Clear();
        var board = ctx.Players[1].Board;
        for (int s = 0; s < board.Length; s++) if (board[s] != null) board[s].Exhausted = true;

        Check(!SimpleAI.HasAnyAction(ctx),
              "B0（前提）清空手牌 + 全部单位行动完 ⇒ **真的只剩 `endTurn`**（`HasAnyAction == false`）"
            + "｜🧨 这条红了说明夹具没做干净（不是被测项），先修夹具");

        int fp0 = NetProtocol.Fingerprint(ctx);
        using (var c = new ExecCounter())
        {
            var rejected = new List<AiAction>();
            bool more = SimpleAI.PlayTurnStep(ctx, AiDifficulty.Hard, rejected);
            Check(!more, "B1 ★ 最佳是 `endTurn` ⇒ `PlayTurnStep` 返回 **false**（收手；原版 `.c:28-31`：类型 4 不执行、返回 false）"
                       + "｜🧨 把 `endTurn` 也执行掉/返回 true ⇒ 红");
            CheckInt(c.Count, 0, "B2 ★ 收手时**一次 `ExecuteAction` 都没有**（`endTurn` 不是一条要执行的动作）");
            CheckInt(NetProtocol.Fingerprint(ctx), fp0, "B3 收手路径**没有改局面**（指纹前后一致）");
        }
    }

    // ==================================================================
    //  C · 等价：兼容壳 `PlayTurn`  ≡  `while (PlayTurnStep(...))`
    // ==================================================================
    static void C_PathEquivalence()
    {
        const int seed = 20261105;

        // 路径 1 = `PlayTurn`（老口径一次调用打完整个 AI 回合）
        var ctxA = NewBattleAtAiTurn(seed);
        if (ctxA == null) { Check(false, "C0（前提）夹具 A 建不出来"); return; }
        int execA;
        using (var c = new ExecCounter()) { SimpleAI.PlayTurn(ctxA); execA = c.Count; }
        int fpA = NetProtocol.Fingerprint(ctxA);

        // 路径 3（先做，用来证明夹具确定）—— 同一条路再跑一遍
        var ctxC = NewBattleAtAiTurn(seed);
        if (ctxC == null) { Check(false, "C0（前提）夹具 C 建不出来"); return; }
        int execC;
        using (var c = new ExecCounter()) { SimpleAI.PlayTurn(ctxC); execC = c.Count; }
        int fpC = NetProtocol.Fingerprint(ctxC);

        Check(fpA == fpC && execA == execC,
              "C1（前提）**夹具是确定的**：同一个 seed 跑两次 `PlayTurn` ⇒ 指纹与执行数一致"
            + $"（指纹 {fpA} vs {fpC}，执行 {execA} vs {execC}）｜🧨 这条红了先查随机源，别拿 C2 的结论当缺陷");

        // 路径 2 = `while (PlayTurnStep(...))`（就是 `BattleDriver.SimulateAiTurn` 现在的形状）
        var ctxB = NewBattleAtAiTurn(seed);
        if (ctxB == null) { Check(false, "C0（前提）夹具 B 建不出来"); return; }
        int execB;
        using (var c = new ExecCounter())
        {
            var rejected = new List<AiAction>();
            int guard = 0;
            while (guard++ < SimpleAI.PlayTurnGuard && SimpleAI.PlayTurnStep(ctxB, AiDifficulty.Hard, rejected)) { }
            execB = c.Count;
        }
        int fpB = NetProtocol.Fingerprint(ctxB);

        Check(fpB == fpA,
              "C2 ★★ **兼容壳与一步式等价**：`PlayTurn(ctx)` 与 `while (PlayTurnStep(...))` 打完同一局，"
            + $"局面指纹相同（{fpA} vs {fpB}）｜🧨 两条路各写一份判据、迟早分叉 ⇒ 红");
        CheckInt(execB, execA,
                 "C3 两条路径的 `ExecuteAction` **次数也相同**（动作流逐条一致，不只是终局相同）");
    }

    // ==================================================================
    //  D · 64 步上限还在，而且 `PlayTurn` 真的用它
    // ==================================================================
    static void D_Guard()
    {
        CheckInt(SimpleAI.PlayTurnGuard, 64,
                 "D1 ★ 步数上限仍是 **64**（🔶 自己设计（非原版）：自检侧防死循环 · 原版 `AI__PlayTurn.c` 全文无循环）"
               + "｜🧨 删掉/改小 ⇒ 红（长回合会在打完之前被截断）");

        string p = Path.Combine(Application.dataPath, "RuleEngine/Data/SimpleAI.cs");
        Check(File.Exists(p), "D0（前提）`SimpleAI.cs` 源文件读得到：" + p + "（⛔ 读不到就红，别改成跳过）");
        if (!File.Exists(p)) return;

        string body = ExtractBody(StripComments(File.ReadAllText(p)), "public static void PlayTurn(BattleContext ctx)");
        Check(body != null, "D0b（前提）找得到 `PlayTurn` 的方法体（改名/改签名 ⇒ 红）");
        if (body == null) return;

        Check(body.Contains("PlayTurnGuard"),
              "D2 ★ `PlayTurn` 的循环用的是 `PlayTurnGuard`（不是散落的字面 64）"
            + "｜🧨 把上限改成常量以外的写法 ⇒ 上下限会在两处各写一份 ⇒ 红");
        Check(!body.Contains("< 64"),
              "D2b `PlayTurn` 里**没有** ` < 64 ` 这种裸字面（上限只有一个出处）");
        Check(body.Contains("PlayTurnStep"),
              "D3 ★ `PlayTurn` 的循环体调的是 `PlayTurnStep`（不是自己内联一份「挑 + 执行」）"
            + "｜🧨 把老循环抄回来 ⇒ 两处判据 ⇒ 红");
    }

    // ==================================================================
    //  E · `PlayTurnStep` 体内**没有循环**（原版一步式；循环属于调用方）
    // ==================================================================
    static void E_NoLoopInsideStep()
    {
        string p = Path.Combine(Application.dataPath, "RuleEngine/Data/SimpleAI.cs");
        if (!File.Exists(p)) { Check(false, "E0（前提）`SimpleAI.cs` 读不到：" + p + "（⛔ 别改成跳过）"); return; }

        string src = StripComments(File.ReadAllText(p));
        string body = ExtractBody(src, "public static bool PlayTurnStep(BattleContext ctx, AiDifficulty diff, IList<AiAction> rejected)");
        Check(body != null, "E0b（前提）找得到 `PlayTurnStep(ctx, diff, rejected)` 的方法体（签名改了 ⇒ 红）");
        if (body == null) return;

        Check(!body.Contains("for (") && !body.Contains("while (") && !body.Contains("foreach ("),
              "E1 ★ `PlayTurnStep` **体内没有任何循环**（原版 `AI__PlayTurn.c` 全文无循环；一轮 = 一步）"
            + "｜🧨 把整回合循环塞回来 ⇒ 红（那正是 A-05 要修的缺陷）");
        Check(body.Contains("ExecuteAction"),
              "E2 ★ 它**自己**执行那一条（原版 `.c:28-30`；⛔ 不是只挑不执行）");
        Check(body.Contains("NextAction"),
              "E3 ★ 它用 `NextAction` 挑（⛔ 不另写一份枚举 + 打分）");
        Check(body.IndexOf("ExecuteAction", StringComparison.Ordinal) > body.IndexOf("NextAction", StringComparison.Ordinal),
              "E4 次序 = **先挑后执行**（原版 `.c:25` `SelectBestAction` → `.c:29` `ExecuteAction`）"
            + "｜🧨 反过来 ⇒ 用上一局的分数执行这一局的局面 ⇒ 红");
    }

    // ==================================================================
    //  源码小工具
    // ==================================================================

    /// <summary>去掉 `//` 行注释（跳过字符串字面量里的 `//`；本工程注释里大量出现反引号示例，别让它们参与判定）。</summary>
    static string StripComments(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new System.Text.StringBuilder(s.Length);
        bool inStr = false, inChar = false, esc = false;
        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];
            if (inStr)
            {
                sb.Append(ch);
                if (esc) esc = false;
                else if (ch == '\\') esc = true;
                else if (ch == '"') inStr = false;
                continue;
            }
            if (inChar)
            {
                sb.Append(ch);
                if (esc) esc = false;
                else if (ch == '\\') esc = true;
                else if (ch == '\'') inChar = false;
                continue;
            }
            if (ch == '"') { inStr = true; sb.Append(ch); continue; }
            if (ch == '\'') { inChar = true; sb.Append(ch); continue; }
            if (ch == '/' && i + 1 < s.Length && s[i + 1] == '/')
            {
                while (i < s.Length && s[i] != '\n') i++;   // 吃掉整行注释
                if (i < s.Length) sb.Append('\n');
                continue;
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    /// <summary>按签名找到方法体（第一个 `{` 到配对的 `}`，含花括号）。找不到返 null。</summary>
    static string ExtractBody(string src, string signature)
    {
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(signature)) return null;
        int i = src.IndexOf(signature, StringComparison.Ordinal);
        if (i < 0) return null;
        int b = src.IndexOf('{', i);
        if (b < 0) return null;
        int depth = 0;
        for (int k = b; k < src.Length; k++)
        {
            if (src[k] == '{') depth++;
            else if (src[k] == '}') { depth--; if (depth == 0) return src.Substring(b, k - b + 1); }
        }
        return null;
    }
}
