// B97W9_Assert.cs — 执行代理 `B97_W9`（第九十七会话 · 波 1 补位）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `日常/奖励:771` `TwoSidesEvent_OnGoingTab.CollectClicked()` 的**接线锁**。
//
//   改前的事实（施工单 `B97_R2_日常35条施工单.md` G15 现核 + 本代理复核）：
//     · `Shell/ParentEventWindow.cs:1856` 有 `public void CollectClicked()`，**全文件 0 调用点**；
//     · 真点击走的是 `:1361-1365` 的**内联 lambda**（自己又抄了一遍告警文案）
//       ⇒ 账上写「等价已落」，实际是「**方法在、没接线**」。
//
//   本轮改动（零行数改法，`ParentEventWindow.cs` 行数守恒 1908 → 1908）：
//     `:1362` 的 lambda 换成**方法组** `CollectClicked`（原版那一颗 `m_OnClick` 本来就是方法引用；
//     同族先例 = `Shell/SinglePlayerOnlyEnergyEventWindow.cs:487` 也是直接传 `CollectClicked`）。
//
//   判据 → `资料/完全复刻/报告/B97_W9.md` · 账补丁 → `账/_parts/B97_W9.tsv`。
//
// ============================ 怎么用（宿主）============================
// `B97W9Assert.Run();`
//   + 宿主里两行（同族先例 `B97MISC2Assert` / `B97MINI2Assert`）：
//     `_sink.Pass += B97W9Assert.Pass; _sink.Fail += B97W9Assert.Fail;`
//   · **不建任何 GameObject / 不建持久资产 / 不开窗** ⇒ 任何宿主都能跑（推荐 `Editor/ShellScene.cs`，活动线宿主）。
//   · 读源码走 `Application.dataPath` ⇒ **只在编辑器宿主里成立**（player 里没有 `Assets/` 源码）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A5：把 `:1362` 换回 `() => Debug.LogWarning("…")`（也就是**内联文案**那版）⇒ A5/A6 红
//          （A5 宁可用「方法组」而不是 `() => CollectClicked()`：两者都能过 A5，但前者与"回调就是那个方法"字面一致）。
//   · A3：把接线整段删掉（按钮点了什么都不做）⇒ A3 红（去注释后 `CollectClicked` 只剩定义那一处）。
//   · A2：再抄一份 `void CollectClicked()` 出来 ⇒ A2 红。
//   · A7：把 `CollectClicked()` 体里的 `Debug.LogWarning` 删掉（不再出声 = 静默失败）⇒ A7 红。
//   · A0：把 `StripCommentsAndStrings` 改成「原样返回」⇒ A0 红（探针里注释那两处会被数进来）。
//   · B1/B2：把方法改名 / 加参数（那样就绑不到 `System.Action` 了）⇒ 红。
//   · B3：把 `WindowButton.onClick` 改成别的类型而没改绑定 ⇒ 红。
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B97_W9`：账 `日常/奖励:771` `TwoSidesEvent_OnGoingTab.CollectClicked()` 的接线锁。
/// <para>⛔ 期望值全是字面量（`1` / `2` / 方法名本身），不从被测实现里读回来。</para></summary>
public static class B97W9Assert
{
    static readonly CheckSink _sink = new CheckSink("[B97W9] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    const string SrcRelPath = "CardPresentation/Shell/ParentEventWindow.cs";
    const string MethodName = "CollectClicked";
    /// <summary>Collect 钮那一跳的源码锚点（`BuildRewardDisplay` 里那一行；改坏了这里 ⇒ A4 会红，⛔ 别放行）。</summary>
    const string WiringAnchor = "MenuDraw.Hit(_p2CollectBtn";

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B97_W9 账 `日常/奖励:771`：`CollectClicked()` 的接线锁（原版 `TwoSidesEvent_OnGoingTab.CollectClicked`）");
        SectionA_源码接线();
        SectionB_类型层();
        Debug.Log(_sink.P + "B97_W9 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ A 源码：Collect 钮的接线
    static void SectionA_源码接线()
    {
        MenuCheck.Section(_sink, "A 源码：领奖钮的点击回调**就是** `CollectClicked`（不是另一份内联文案）");

        // A0 —— 扫描器自检（**不依赖被测文件的内容**：改了文件里的注释也不会让这条假红）
        const string probe = "// CollectClicked\n/* CollectClicked */\nvoid X() { CollectClicked(); }";
        C(CountOf(StripCommentsAndStrings(probe), MethodName), 1,
          "A0 **前提**：去注释/去字符串的扫描器本身是对的（探针里有 3 处 `CollectClicked`、2 处在注释里 ⇒ 扫描后必须只剩 1 处）"
        + "｜把它改成「原样返回」⇒ 本条红");

        string full = Path.Combine(Application.dataPath, SrcRelPath.Replace('/', Path.DirectorySeparatorChar));
        bool exists = File.Exists(full);
        Tr(exists, "A1 **前提**：源码在 `" + SrcRelPath + "`（读不到 ⇒ A2..A7 没跑；本文件不静默跳过）｜实读路径 = " + full);
        if (!exists) return;

        string raw = File.ReadAllText(full);
        string code = StripCommentsAndStrings(raw);

        C(CountOfDefs(code, MethodName), 1,
          "A2 `ParentEventWindow.cs` 里 `void " + MethodName + "(…)` 的定义**恰好 1 处**"
        + "｜再抄一份 ⇒ 红（两份实现迟早不一致）");

        int uses = CountOf(code, MethodName);
        Tr(uses >= 2,
           "A3 去注释/去字符串之后 `" + MethodName + "` 出现 **" + uses + " 处** ⇒ ≥ 2（定义 1 + 被引用 ≥ 1）"
         + "｜把接线整段删掉 ⇒ 只剩定义那一处 ⇒ 红（这就是「账说已做、实际没接线」的原症状）");

        int ix = code.IndexOf(WiringAnchor, System.StringComparison.Ordinal);
        Tr(ix >= 0,
           "A4 **前提**：Collect 钮那一跳 `" + WiringAnchor + "` 在源码里（找不到 ⇒ 锚点过期，**必须改这条断言**，⛔ 别让它空转绿）");
        if (ix < 0) return;

        int end = code.IndexOf(");", ix, System.StringComparison.Ordinal);
        string region = end > ix ? code.Substring(ix, end - ix + 2) : "";
        Tr(region.Length > 0, "A4b **前提**：`" + WiringAnchor + "` 那一跳能截到闭合的 `);`（截不到 ⇒ 下面两条没跑）");
        if (region.Length == 0) return;

        Tr(region.Contains(MethodName),
           "A5 那一跳的委托实参**引用了 `" + MethodName + "`**（原版这颗钮的 `m_OnClick` 本来就是方法引用）"
         + "｜换回 `() => Debug.LogWarning(…)`（自己再抄一份文案）⇒ 红");

        // 灭自证的核心：**内联文案**那一版会在这里红（它只有 `Debug.LogWarning`，没有方法引用）
        Tr(!region.Contains("LogWarning"),
           "A6 **灭自证**：那一跳里**不许出现** `LogWarning`（文案只在 `" + MethodName + "()` 里留一份；"
         + "当时那条假象就是这样来的：内联 lambda 自己又播了一份，于是「方法在树里、却永远不被调用」看不出来）");

        string body = MethodBody(code, MethodName);
        Tr(body != null && body.Contains("Debug.LogWarning"),
           "A7 `" + MethodName + "()` 体里**仍然出声**（`Debug.LogWarning`）—— 原版 `TryCollect` 是一句 PlayFab 云调用（P3），"
         + "本地不假装领取，但必须如实说「为什么领不了」（红线：不许静默失败）｜把告警删掉 ⇒ 红");
    }

    // ------------------------------------------------------------ B 类型层
    static void SectionB_类型层()
    {
        MenuCheck.Section(_sink, "B 类型层：`ParentEventWindow` 上只有一个 `CollectClicked`，且形状能直接当 `System.Action`");

        var t = typeof(ParentEventWindow);
        var all = t.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
                             | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static
                             | System.Reflection.BindingFlags.DeclaredOnly);
        int n = 0;
        System.Reflection.MethodInfo m0 = null;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].Name == MethodName) { n++; m0 = all[i]; }
        }
        C(n, 1,
          "B1 反射现数：`ParentEventWindow` **自己声明**的 `" + MethodName + "` 方法 = " + n + " 个 ⇒ 必须恰好 1 个（重载/重名 ⇒ 红）");
        Tr(m0 != null && m0.ReturnType == typeof(void) && m0.GetParameters().Length == 0,
           "B2 那个方法 = `public void " + MethodName + "()`（`void` + 0 参 ⇒ 能直接当 `System.Action` 交给 `MenuDraw.Hit`；"
         + "改了签名 ⇒ 红）");

        var f = typeof(WindowButton).GetField("onClick",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        Tr(f != null && f.FieldType == typeof(System.Action),
           "B3 **前提**：`WindowButton.onClick` 仍是 `System.Action`（= `MenuDraw.Hit` 的委托容器；"
         + "换成 `UnityEvent` 而没同步改绑定 ⇒ 红）");
    }

    // ============================================================ 源码工具（本文件自用，别外借）
    /// <summary>把注释整段、字符串/字符**内容**一起抹成空格（保留换行 ⇒ 行号不变）。
    /// <para>⛔ 不能只抹注释：`CollectClicked` 的名字在两条 `Debug.LogWarning("…`CollectClicked`…")` 里也出现过，
    /// 只抹注释会把「消息文本」数成「引用」⇒ A3 空转绿。</para></summary>
    static string StripCommentsAndStrings(string s)
    {
        var sb = new StringBuilder(s.Length);
        int i = 0, n = s.Length;
        while (i < n)
        {
            char c = s[i];
            if (c == '"')
            {
                sb.Append(c); i++;
                while (i < n)
                {
                    char d = s[i];
                    if (d == '\\' && i + 1 < n) { sb.Append("  "); i += 2; continue; }
                    i++;
                    if (d == '"' || d == '\n') { sb.Append(d == '"' ? '"' : '\n'); break; }
                    sb.Append(' ');
                }
                continue;
            }
            if (c == '\'')
            {
                sb.Append(c); i++;
                while (i < n)
                {
                    char d = s[i];
                    if (d == '\\' && i + 1 < n) { sb.Append("  "); i += 2; continue; }
                    i++;
                    if (d == '\'' || d == '\n') { sb.Append(d == '\'' ? '\'' : '\n'); break; }
                    sb.Append(' ');
                }
                continue;
            }
            if (c == '/' && i + 1 < n && s[i + 1] == '/')
            {
                while (i < n && s[i] != '\n') { sb.Append(' '); i++; }
                continue;
            }
            if (c == '/' && i + 1 < n && s[i + 1] == '*')
            {
                sb.Append(' '); sb.Append(' '); i += 2;
                while (i < n && !(s[i] == '*' && i + 1 < n && s[i + 1] == '/'))
                {
                    sb.Append(s[i] == '\n' ? '\n' : ' '); i++;
                }
                if (i < n) { sb.Append(' '); sb.Append(' '); i += 2; }
                continue;
            }
            sb.Append(c); i++;
        }
        return sb.ToString();
    }

    /// <summary>整词出现次数（标识符边界 = 非 `[A-Za-z0-9_]`）。</summary>
    static int CountOf(string code, string word)
    {
        int n = 0, i = 0;
        while (true)
        {
            int k = code.IndexOf(word, i, System.StringComparison.Ordinal);
            if (k < 0) break;
            bool leftOk = k == 0 || !IsIdentChar(code[k - 1]);
            int after = k + word.Length;
            bool rightOk = after >= code.Length || !IsIdentChar(code[after]);
            if (leftOk && rightOk) n++;
            i = k + word.Length;
        }
        return n;
    }

    static bool IsIdentChar(char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
    }

    /// <summary>`void &lt;name&gt;(…)` 的定义条数（形参 0 个或多个都算**同一个定义**的候选）。</summary>
    static int CountOfDefs(string code, string name)
    {
        var re = new Regex(@"\bvoid\s+" + Regex.Escape(name) + @"\s*\([^)]*\)");
        return re.Matches(code).Count;
    }

    /// <summary>取 `void &lt;name&gt;(…)` 的花括号体（字符串/注释已被抹掉 ⇒ 括号配平可靠）。</summary>
    static string MethodBody(string code, string name)
    {
        var m = Regex.Match(code, @"\bvoid\s+" + Regex.Escape(name) + @"\s*\([^)]*\)\s*\{");
        if (!m.Success) return null;
        int i = m.Index + m.Length;
        int start = i, depth = 1;
        while (i < code.Length && depth > 0)
        {
            char c = code[i];
            if (c == '{') depth++;
            else if (c == '}') depth--;
            i++;
        }
        if (depth != 0) return null;
        return code.Substring(start, System.Math.Max(0, i - start - 1));
    }
}
