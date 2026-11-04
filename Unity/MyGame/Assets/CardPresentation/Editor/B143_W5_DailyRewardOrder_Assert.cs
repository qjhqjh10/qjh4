// B143_W5_DailyRewardOrder_Assert.cs — 2026-11-05 · `B143-W5` · 每日奖励「收完再关」
// （2026-11-05 第二轮：按第三方复核修 3 条 —— A1 现在必红 / A3 形同虚设 / A5 同义反复；A2 顺手加固）
//
// ============================ 它盯的是什么 ============================
//  原版 `DailyRewardPopup.Close()` = `LiveOp.TryCollect(() => base.Close())` ⇒ **先收再关**；
//  程序性关窗（`CloseAllWindows()` / 父窗 / `Hide()`）**不经 `TryCollect`** ⇒ **不收**。
//
// ============================ 逐条（含「改坏怎么红」）============================
//  A1 `ESCPressed()` 方法体内：`_escCollectOnClose = true;` 必须在 `base.ESCPressed()` **之前**
//     （⚠️ 从**方法头**起搜 —— 上一版从全文搜，先命中了 `:287` 的 `///` 注释 ⇒ 必红）。
//  A2 `Close()` 方法体内：`DailyRewardAutoCollect();` **出现且只出现一次**，且在 `base.Close()` **之前**
//     （🧨 对调 ⇒ 红；在 `base.Close()` 之后再补一句收 ⇒ 「出现两次」⇒ 红）。
//  A3 「收」的偏移必须落在 `if (_escCollectOnClose) { … }` 的**大括号区间之内**（块级判据）。
//     🧨 条件改 `true`、把收挪到 if 块外、改成无条件收 ⇒ 收就跑出区间/出现两次 ⇒ 红。
//  A4 返回钮 `CloseCollecting()` 仍是「先收再关」。
//  A5 **真行为**（建窗跑一遍）：`ESCPressed()` ⇒ `CloseOrderForTest == "collect,close"`；
//     **程序性 `Close()`** ⇒ `== "close"`（**没有 collect**）。🧨 上面任何一条改坏 ⇒ 这两条必有一条红。
//
// ============================ 怎么接 ============================
//  宿主汇总行：`B143_W5_DailyRewardOrder_Assert.Run(mgr); _sink.Pass += …Pass; _sink.Fail += …Fail;`
//  ⚠️ A5 需要一个 `WindowsManager`（建议 `Editor/RewardsScene.cs`，那里已有 `wm2`）；传 `null` ⇒ 只跑 A1–A4，
//     A5 记一条**前提红**（⛔ 不静默跳过）。A5 会真调一次 `ESCPressed()` ⇒ 若当时有可领奖励会被**正常收取**
//     （与 `RewardsScene` 既有 A495「ESC ⇒ 真收到了」同一条口径）。
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B143-W5`：每日奖励「先收再关」+「程序性关窗不收」（源码块级 + 真行为两层）。</summary>
public static class B143_W5_DailyRewardOrder_Assert
{
    const string Tag = "B143W5DR";
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    public static void Run(WindowsManager mgr = null)
    {
        _pass = 0; _fail = 0;
        Debug.Log(Tag + " ==== 每日奖励：先收再关 / 程序性关窗不收 ====");

        string p = Application.dataPath + "/CardPresentation/Shell/DailyRewardPopup.cs";
        Check(File.Exists(p), "A0（前提）读得到 `Shell/DailyRewardPopup.cs`（" + p + "）");
        if (!File.Exists(p)) { End(); return; }
        string src = File.ReadAllText(p);

        GroupSource(src);
        GroupLive(mgr);
        End();
    }

    static void End() { Debug.Log(Tag + " ==== 合计：" + _pass + " 通过 / " + _fail + " 失败 ===="); }

    // ==================================================================
    //  A · 源码（块级）
    // ==================================================================
    static void GroupSource(string src)
    {
        string escBody = BodyOf(src, "public override bool ESCPressed()");
        string closeBody = BodyOf(src, "public override void Close()");
        string ccBody = BodyOf(src, "public void CloseCollecting()");
        Check(escBody != null && closeBody != null && ccBody != null,
              "A0'（前提）三个方法体都解析出来了（`ESCPressed` / `Close` / `CloseCollecting`）");
        if (escBody == null || closeBody == null || ccBody == null) return;

        // A1：标志必须在 base.ESCPressed() 之前（⚠️ 只在**方法体**里搜，⛔ 不再命中注释）
        int iFlag = escBody.IndexOf("_escCollectOnClose = true;", System.StringComparison.Ordinal);
        int iBaseEsc = escBody.IndexOf("base.ESCPressed()", System.StringComparison.Ordinal);
        Check(iFlag >= 0 && iBaseEsc >= 0 && iFlag < iBaseEsc,
              "A1 `ESCPressed()` 体内：置 `_escCollectOnClose` **先于** `base.ESCPressed()`（门槛仍只由基类判）"
            + "｜🧨 把置位挪到 `base` 之后 ⇒ 本红（上一版从全文搜、命中 `///` 注释 ⇒ 必红，已改）");

        // A2：收只出现一次、且在 base.Close() 之前
        int iCollect = closeBody.IndexOf("DailyRewardAutoCollect();", System.StringComparison.Ordinal);
        int nCollect = CountOf(closeBody, "DailyRewardAutoCollect();");
        int iBaseClose = closeBody.IndexOf("base.Close();", System.StringComparison.Ordinal);
        Check(nCollect == 1 && iCollect >= 0 && iBaseClose >= 0 && iCollect < iBaseClose,
              "A2 ★ **先收再关**：`Close()` 体内 `DailyRewardAutoCollect();` **只在 `base.Close()` 之前出现一次**"
            + "（实得 次数=" + nCollect + " · 收@" + iCollect + " · 关@" + iBaseClose + "）"
            + "｜🧨 对调 ⇒ 红；在 `base.Close()` 之后再补一句收 ⇒ 次数=2 ⇒ 红");

        // A3：收必须落在 `if (_escCollectOnClose) { … }` 区间内
        int iIf = closeBody.IndexOf("if (_escCollectOnClose)", System.StringComparison.Ordinal);
        int open = iIf < 0 ? -1 : closeBody.IndexOf('{', iIf);
        int close = open < 0 ? -1 : MatchBrace(closeBody, open);
        bool inBlock = iIf >= 0 && open > iIf && close > open && iCollect > open && iCollect < close;
        Check(inBlock,
              "A3 ★ **程序性关窗不收**：那句「收」在 `if (_escCollectOnClose) { … }` 的**大括号区间之内**"
            + "（if@" + iIf + " · 块(" + open + ".." + close + ") · 收@" + iCollect + "）"
            + "｜🧨 条件改 `true`／把收挪出 if 块／改无条件收 ⇒ 收跑出区间或次数=2 ⇒ 本红");

        // A4：返回钮仍先收后关
        int iCcCollect = ccBody.IndexOf("DailyRewardAutoCollect();", System.StringComparison.Ordinal);
        int iCcClose = ccBody.IndexOf("Close();", System.StringComparison.Ordinal);
        Check(iCcCollect >= 0 && iCcClose > iCcCollect,
              "A4 返回钮 `CloseCollecting()` = **先收后关**（与 ESC 同一终态，两条生产路径都齐）");
    }

    // ==================================================================
    //  B · 真行为（建窗跑一遍）
    // ==================================================================
    static void GroupLive(WindowsManager mgr)
    {
        if (mgr == null) mgr = WindowsManager.Instance;
        Check(mgr != null,
              "B0（前提）有 `WindowsManager` ⇒ 真行为两条能跑｜⛔ 传 null 就**记红**（不静默跳过；建议在 `Editor/RewardsScene.cs` 接）");
        if (mgr == null) return;

        DailyRewardPopup a = null, b = null;
        try
        {
            // ---- B1：ESC ⇒ collect,close ----
            a = DailyRewardPopup.Create(mgr);
            a.gameObject.SetActive(true);
            a.Open();
            DailyRewardPopup.ResetCloseOrderForTest();
            a.ESCPressed();
            string seq1 = DailyRewardPopup.CloseOrderForTest;
            Check(seq1 == "collect,close",
                  "B1 ★ **ESC ⇒ 先收后关**（`CloseOrderForTest == \"collect,close\"`；实得「" + seq1 + "」）"
                + "｜🧨 顺序对调 ⇒ `close,collect` ⇒ 本红");

            // ---- B2：程序性 Close() ⇒ 只有 close ----
            b = DailyRewardPopup.Create(mgr);
            b.gameObject.SetActive(true);
            DailyRewardPopup.ResetCloseOrderForTest();
            b.Close();                                    // 程序性关（不经 ESCPressed）
            string seq2 = DailyRewardPopup.CloseOrderForTest;
            Check(seq2 == "close",
                  "B2 ★ **程序性关窗不收**（`CloseOrderForTest == \"close\"`；实得「" + seq2 + "」）"
                + "｜🧨 把 `if (_escCollectOnClose)` 去掉（无条件收）⇒ 变 `collect,close` ⇒ 本红（这条是 A3 的真行为版）");
        }
        catch (System.Exception e)
        {
            Check(false, "B 段抛异常（真行为两条没跑完）：" + e);
        }
        finally
        {
            if (a != null && a.gameObject != null) Object.DestroyImmediate(a.gameObject);
            if (b != null && b.gameObject != null) Object.DestroyImmediate(b.gameObject);
            DailyRewardPopup.ResetCloseOrderForTest();
        }
    }

    // ==================================================================
    //  小工具
    // ==================================================================

    /// <summary>取 `signature` 这个方法的方法体（`{` 到配对的 `}` 之间）。找不到 ⇒ `null`。</summary>
    static string BodyOf(string src, string signature)
    {
        int i = src.IndexOf(signature, System.StringComparison.Ordinal);
        if (i < 0) return null;
        int open = src.IndexOf('{', i + signature.Length);
        if (open < 0) return null;
        int close = MatchBrace(src, open);
        return close < 0 ? null : src.Substring(open + 1, close - open - 1);
    }

    /// <summary>从 `open`（必须是 `{`）找配对的 `}`（简单花括号计数；被测源码里没有跨行字符串花括号）。</summary>
    static int MatchBrace(string s, int open)
    {
        int depth = 0;
        for (int i = open; i < s.Length; i++)
        {
            if (s[i] == '{') depth++;
            else if (s[i] == '}') { depth--; if (depth == 0) return i; }
        }
        return -1;
    }

    static int CountOf(string s, string sub)
    {
        int n = 0, i = 0;
        while ((i = s.IndexOf(sub, i, System.StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; }
        return n;
    }
}
