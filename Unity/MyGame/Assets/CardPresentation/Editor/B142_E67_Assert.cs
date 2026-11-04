// B142_E67_Assert.cs — 执行写手 `B142-E67`（第一百二十九会话 · 2026-11-04 · 批次 B142）
//   三件活（判据 → `资料/完全复刻/判据/B142_R18_CHK05块6.md` §二·4 / §二·5 / §二·6）：
//     ① `B142-TUT-TIP-EDGE`   教程提示「点一下消掉」吃**按下沿**（原版 `TutorialTipScript__Update.c:9-27` = `Input.GetMouseButtonDown(0)`），
//                             改前吃抬起沿；且排在 `Update` 的 `if (_downEdge) RequestTutorialSkip(...)` **之前**并**消费**这一沿。
//     ② `B142-CAMPAIGN-DECKORD`「阵营 ∧ 本窗模式」那张表是 `Where → OrderByDescending(ValidateDeck)`（合法优先），
//                             序号 = 该表的 `IndexOf`、上/下一套 = 该表的下标 ±1；改前两处都走**集合序**。
//     ③ `B142-FORGE-HOVER`     悬停放大的闸 = 原版 `state(+0x70) == 2`（`CardInBoosterPack__TryHighlightCardback.c:21`），
//                             改前是 `!Opened[i]`（「已开」≠「已就绪」⇒ 开窗头 0.6 s 会误放大）。
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  🧨 A2：把 `HandleTutorialTipDismiss` 改回 `ReleasedThisFrame()` ⇒ A1/A2 红（抬起沿 ≠ 原版按下沿）。
//  🧨 A4：把 `if (_downEdge) RequestTutorialSkip(...)` 删掉/改成 `ClickedThisFrame()`（与提示抢同一沿）⇒ A4 红。
//  🧨 B3/B4：把排序改成「过滤」（丢掉不合法卡组）⇒ B4 的「全不合法时表长仍 4」红；
//            改成不稳定排序 / 按合法升序 ⇒ B3 的 `{1,3,0,2}` 红。两半结构上不可能同时被同一改动满足。
//  🧨 B1/B2：把 `ShownDeckOrdinal` / `StepDeck` 改回集合序循环 ⇒ B1/B2 红（源码里 `InArmyDeckList` 计数 / 全库绕圈那两句回来）。
//  🧨 C1/C2：把悬停闸改回 `!Opened[captured]`（或任何不判 `state` 的写法）⇒ C1/C2 红。
//  🧨 C3：把 `CardState` 的序数改掉（`Ready != 2`）⇒ C3 红 —— 原版判的是**字面 2**，枚举重排会静默错一整族。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `public static void Run()`：**零前提**（不建窗、不读存档；只读三个源文件 + 调一个纯函数）。
//  挂法（调度台决定）：宿主聚合行**同一物理行**、且追加在该行第一个 `//` **之前**：
//      B142_E67_Assert.Run(); _sink.Pass += B142_E67_Assert.Pass; _sink.Fail += B142_E67_Assert.Fail;
//  宿主建议 = `Editor/BattleScene.cs`（①的战斗宿主）或 `Editor/MainMenuScene.cs`（②③的宿主）。
//  判绿红：按行首 `B142E67 ✓` / `B142E67 ✗` 数（⛔ 别 `grep -c ✗`），合计行 = `B142E67 合计 …`。

using System;
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>`B142-E67`：钉住三处——教程提示消掉的**按下沿** · 活动窗卡组表的**合法优先序** · 开包悬停闸的 `state == 2`。</summary>
public static class B142_E67_Assert
{
    static int _pass, _fail;
    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B142E67 ✓ " + msg); }
        else { _fail++; Debug.LogError("B142E67 ✗ " + msg); }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        A_TipPressEdge();
        B_DeckOrderLegalFirst();
        C_ForgeHoverStateGate();
        Debug.Log("B142E67 合计 " + _pass + " 过 / " + _fail + " 红");
    }

    // ============================================================
    //  工具
    // ============================================================
    /// <summary>读工程内相对路径源文件；读不到 ⇒ null（调用方**先断前提**，⛔ 不静默跳过）。</summary>
    static string ReadSrc(string rel)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, rel);
            return File.Exists(p) ? File.ReadAllText(p) : null;
        }
        catch (Exception) { return null; }
    }

    /// <summary>取「方法体」文本：从含 <paramref name="signature"/> 的那一行起，按大括号配平截到方法结束。
    /// 找不到 ⇒ null（调用方先断前提）。</summary>
    static string MethodBody(string src, string signature)
    {
        if (src == null) return null;
        int at = src.IndexOf(signature, StringComparison.Ordinal);
        if (at < 0) return null;
        int open = src.IndexOf('{', at);
        if (open < 0) return null;
        int depth = 0;
        for (int i = open; i < src.Length; i++)
        {
            if (src[i] == '{') depth++;
            else if (src[i] == '}')
            {
                depth--;
                if (depth == 0) return src.Substring(open, i - open + 1);
            }
        }
        return null;
    }

    /// <summary>剥掉注释（整行 `//` 与行尾 `//`）—— 判「源码里还有没有某个写法」只看**代码**，
    /// 否则我们自己那些「⛔ 别退回 `X`」的说明会把检查骗红（本文件踩过这一格：A2）。
    /// ⚠️ 本工程这几段里字符串字面量不含 `//` ⇒ 简单截断足够。</summary>
    static string CodeOnly(string src)
    {
        if (src == null) return null;
        var sb = new System.Text.StringBuilder();
        foreach (var ln in src.Split('\n'))
        {
            string t = ln.TrimStart();
            if (t.StartsWith("//", StringComparison.Ordinal)) continue;
            int c = ln.IndexOf("//", StringComparison.Ordinal);
            sb.AppendLine(c >= 0 ? ln.Substring(0, c) : ln);
        }
        return sb.ToString();
    }

    /// <summary>按**代码行**找「哪一行含 needle」（跳过整行注释）。返回 -1 = 没有；-2 = 源文件读不到。</summary>
    static int LineIndexOfCode(string src, string needle)
    {
        if (src == null) return -2;
        var lines = src.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string t = lines[i].TrimStart();
            if (t.StartsWith("//", StringComparison.Ordinal)) continue;
            if (lines[i].IndexOf(needle, StringComparison.Ordinal) >= 0) return i;
        }
        return -1;
    }

    const string SrcBattleDriver = "CardPresentation/Battle/BattleDriver.cs";
    const string SrcLiveOps = "CardPresentation/Shell/LiveOpsEventWindow.cs";
    const string SrcBooster = "CardPresentation/Shell/BoosterPackOpenWindow.cs";

    // ============================================================
    //  ① 教程提示「点一下消掉」= 按下沿
    // ============================================================
    static void A_TipPressEdge()
    {
        string src = ReadSrc(SrcBattleDriver);
        Check(src != null, "A0（前提）读得到 `BattleDriver.cs`");
        if (src == null) return;

        string body = MethodBody(src, "bool HandleTutorialTipDismiss()");
        Check(body != null, "A0b（前提）找得到 `HandleTutorialTipDismiss()` 的方法体");
        if (body == null) return;
        string code = CodeOnly(body);      // 只判代码（注释里点名旧写法不算）

        // A1：按下沿（原版 `TutorialTipScript__Update.c:9-27` 的 `Input.GetMouseButtonDown(0)`）
        Check(code.IndexOf("ClickedThisFrame()", StringComparison.Ordinal) >= 0,
              "A1 `HandleTutorialTipDismiss` 吃**按下沿**（`ClickedThisFrame()` = 原版 `Input.GetMouseButtonDown(0)`）");
        // A2：⛔ 不许再是抬起沿（改前的写法）
        Check(code.IndexOf("ReleasedThisFrame()", StringComparison.Ordinal) < 0,
              "A2 `HandleTutorialTipDismiss` 里**没有** `ReleasedThisFrame()`（改前那句 `if (!ReleasedThisFrame()) return false;` 已退场）");

        // A4：它消费同一沿 ⇒ 排在 `if (_downEdge) RequestTutorialSkip(...)` **之前**；
        //     而那条跳过仍然「只读 `_downEdge`、不消费」（原版 DetectClickToSkip 也是按下沿 + 不消费）。
        int iDismiss = LineIndexOfCode(src, "HandleTutorialTipDismiss())");
        int iSkip = LineIndexOfCode(src, "if (_downEdge) RequestTutorialSkip(WorldPointer());");
        Check(iDismiss >= 0, "A4a（前提）`Update` 里有 `… || HandleTutorialTipDismiss()) return;` 那一句（行 " + iDismiss + "）");
        Check(iSkip >= 0, "A4b（前提）`Update` 里有 `if (_downEdge) RequestTutorialSkip(WorldPointer());`（行 " + iSkip + "）");
        if (iDismiss >= 0 && iSkip >= 0)
            Check(iDismiss < iSkip,
                  "A4c 两次判据**排先后**：提示消掉（行 " + iDismiss + "）在跳过（行 " + iSkip + "）之前 ⇒ 同一按不会既消提示又跳过");
        Check(LineIndexOfCode(src, "bool ClickedThisFrame()") >= 0 && LineIndexOfCode(src, "bool ReleasedThisFrame()") >= 0,
              "A4d（前提）两条沿的访问器都在（`ClickedThisFrame` / `ReleasedThisFrame`）");
    }

    // ============================================================
    //  ② 活动窗卡组表：合法优先（`Where → OrderByDescending(ValidateDeck)`）
    // ============================================================
    static void B_DeckOrderLegalFirst()
    {
        string src = ReadSrc(SrcLiveOps);
        Check(src != null, "B0（前提）读得到 `LiveOpsEventWindow.cs`");
        if (src == null) return;

        string ord = MethodBody(src, "int ShownDeckOrdinal()");
        string step = MethodBody(src, "void StepDeck(int dir)");
        string mk = MethodBody(src, "List<int> SortedArmyDeckList(string faction)");
        Check(ord != null, "B0b（前提）找得到 `ShownDeckOrdinal()`");
        Check(step != null, "B0c（前提）找得到 `StepDeck(int dir)`");
        Check(mk != null, "B0d（前提）找得到 `SortedArmyDeckList(string faction)`");
        if (ord == null || step == null || mk == null) return;

        // B1/B2：两处都改走那张**排序后**的表（原版 `currentArmyCardDeckList`），⛔ 不是集合序
        string ordCode = CodeOnly(ord), stepCode = CodeOnly(step), mkCode = CodeOnly(mk);   // 只判代码（注释里的旧写法不算）
        Check(ordCode.IndexOf("SortedArmyDeckList(", StringComparison.Ordinal) >= 0,
              "B1 `ShownDeckOrdinal` 读的是排序后的表（`SortedArmyDeckList`）");
        Check(ordCode.IndexOf("for (", StringComparison.Ordinal) < 0,
              "B1b `ShownDeckOrdinal` 里**没有**逐格数的循环（改前那句 `for (int i = 0; i < CollectionData.DeckCount(); i++)` 已退场）");
        Check(stepCode.IndexOf("SortedArmyDeckList(", StringComparison.Ordinal) >= 0,
              "B2 `StepDeck` 走排序后的表（`SortedArmyDeckList`）");
        Check(stepCode.IndexOf("CollectionData.DeckCount()", StringComparison.Ordinal) < 0,
              "B2b `StepDeck` 里**没有**全库绕圈的 `CollectionData.DeckCount()`（改前那句已退场）");
        Check(mkCode.IndexOf("SortDeckIdsLegalFirst(", StringComparison.Ordinal) >= 0
              && mkCode.IndexOf("DeckIsLegal", StringComparison.Ordinal) >= 0,
              "B2c `SortedArmyDeckList` 用 `DeckIsLegal` 当**排序键**（= 原版 `OrderByDescending(ValidateDeck)`；⛔ 不是过滤器）");

        // B3/B4：纯函数行为（同一个口 —— 产品与断言共用 `SortDeckIdsLegalFirst`，⛔ 断言不另抄一份循环）
        var ids = new List<int> { 0, 1, 2, 3 };
        var mixed = LiveOpsEventWindow.SortDeckIdsLegalFirst(ids, i => i % 2 == 1);
        Check(SameSeq(mixed, new[] { 1, 3, 0, 2 }),
              "B3 合法优先且**稳定**：{0,1,2,3}（合法 = 1,3）⇒ {1,3,0,2}（实得 " + Seq(mixed) + "）");
        var allIllegal = LiveOpsEventWindow.SortDeckIdsLegalFirst(ids, i => false);
        Check(SameSeq(allIllegal, new[] { 0, 1, 2, 3 }),
              "B4 全不合法时**表长不变、次序不变**（= 合法性不是过滤器；实得 " + Seq(allIllegal) + "）");
        var allLegal = LiveOpsEventWindow.SortDeckIdsLegalFirst(ids, i => true);
        Check(SameSeq(allLegal, new[] { 0, 1, 2, 3 }),
              "B4b 全合法时次序不变（实得 " + Seq(allLegal) + "）");
        var keepTail = LiveOpsEventWindow.SortDeckIdsLegalFirst(new List<int> { 7, 8 }, i => i == 8);
        Check(SameSeq(keepTail, new[] { 8, 7 }),
              "B4c 两元组也按同一规则（{7,8} 合法 = 8 ⇒ {8,7}；实得 " + Seq(keepTail) + "）");
    }

    static bool SameSeq(List<int> got, int[] want)
    {
        if (got == null || got.Count != want.Length) return false;
        for (int i = 0; i < want.Length; i++) if (got[i] != want[i]) return false;
        return true;
    }

    static string Seq(List<int> l)
    {
        if (l == null) return "null";
        var sb = new System.Text.StringBuilder("{");
        for (int i = 0; i < l.Count; i++) { if (i > 0) sb.Append(','); sb.Append(l[i]); }
        return sb.Append('}').ToString();
    }

    // ============================================================
    //  ③ 开包悬停闸 = `state == 2`（原版 `TryHighlightCardback`）
    // ============================================================
    static void C_ForgeHoverStateGate()
    {
        string src = ReadSrc(SrcBooster);
        Check(src != null, "C0（前提）读得到 `BoosterPackOpenWindow.cs`");
        if (src == null) return;

        int iEnter = LineIndexOfCode(src, "wb.onEnter =");
        int iExit = LineIndexOfCode(src, "wb.onExit =");
        Check(iEnter >= 0, "C0b（前提）找得到 `wb.onEnter = …`（行 " + iEnter + "）");
        Check(iExit >= 0, "C0c（前提）找得到 `wb.onExit = …`（行 " + iExit + "）");
        if (iEnter < 0 || iExit < 0) return;

        var lines = src.Split('\n');
        string enter = CodeOnly(lines[iEnter]), exit = CodeOnly(lines[iExit]);   // 只判代码（行尾注释里的 `!Opened[i]` 不算）
        const string gate = "CardStates[captured] == (int)CardState.Ready";
        Check(enter.IndexOf(gate, StringComparison.Ordinal) >= 0,
              "C1 悬停进 = 原版 `state == 2`（`CardStates[captured] == CardState.Ready`）");
        Check(exit.IndexOf(gate, StringComparison.Ordinal) >= 0,
              "C1b 悬停出 = 同一条闸（原版 `TryHighlightCardback(0)` 也判 `state == 2`）");
        Check(enter.IndexOf("!Opened[captured]", StringComparison.Ordinal) < 0
              && exit.IndexOf("!Opened[captured]", StringComparison.Ordinal) < 0,
              "C2 两处都**不再**用 `!Opened[captured]`（「已开」≠「已就绪」：开窗头 0.6 s 会误放大）");

        // C3：枚举序数必须与原版字面量一致（原版判的是 int `+0x70 == 2`）
        Check((int)BoosterPackOpenWindow.CardState.Ready == 2,
              "C3 `CardState.Ready` 的序数 == **2**（原版 `CardInBoosterPack__TryHighlightCardback.c:21` 判的是字面 2）");
        Check((int)BoosterPackOpenWindow.CardState.Open == 5
              && (int)BoosterPackOpenWindow.CardState.Opening == 3,
              "C3b `Open == 5` / `Opening == 3`（同一张序数表：`Back 1 / Ready 2 / Opening 3 / Shaking 4 / Open 5`）");
    }
}
