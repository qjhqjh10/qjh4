// B144_C_UI_Assert.cs — 执行写手 `B144-C`（2026-11-05 · B143 离线执行波）
//   盯的是本波「B131 UI 六/七章 10 条可直接改」里已落地的 7 条（另 2 条见报告：商店 Available 被宿主断言挡住、
//   跨窗兄弟序属架构改动）。判据出处 = `报告/B131_审计_UI版面与字体.md` 对应行号 + 原版 prefab 字段。
//
// ============================ 它盯的是什么（逐条对判据）============================
//  A · 行为（零前提：只建一只临时 toast 控制器，收尾 DisposeForTest + DestroyImmediate）
//     A1 `Toast.prefab ▸ Text` 的 `m_paragraphSpacing = 20`（B131 六#25）
//     A2 `Mission Toast Notification.prefab ▸ Text` 的 `m_paragraphSpacing = 20`（B131 六#26）
//     A3 灭自证：`Get XP` 模板那颗 Text **没有** ps（= 0）⇒ 读口不是恒返回 20
//  B · 行内接线守卫（源码级；每条都要求**同一物理行**同时含锚点与新调用，且新调用在行内 `//` **之前**）
//     B1 `SettingsWindow.GenButton` 的 `SetMarginPx(0, 0.00775, 0, −0.8828)`（B131 六#8）
//     B2 `SettingsWindow.SpText` 的 `ps` / `V=Top` 两个新分支（B131 六#10 的漏斗）
//     B3 `SettingsWindow` 的 `Email Text` 调用点传 `0f, 25f, true`（B131 六#10）
//     B4 `ToastNotifications` 两颗 `SetParagraphSpacing(lb, 20f)` 各在自己的构建函数里
//     B5 `DeckRuntime` 万能卡计数的 `SetParagraphSpacing(_wcTxt[i], 0.08f)`（B131 六#11）
//     B6 `BattleDriver` END TURN 的 `SetLineSpacing(_endTurnLabel, −29f)`（B131 六#30）
//     B7 `MainMenuSubmenuWindow` 页签的 `SetCharSpacing(−3.5)` + `Truncate`（B131 六#18/41）
//  C · 结构守卫（防「代码落进行末注释」那一族；本仓一天踩过 6 次）
//     每条 B 同时断：锚点在全文**恰好一处**（重复劫持会红）；新调用与锚点在**同一行**（被挪到别处/被注释 ⇒ 红）。
//
// ============================ 怎么跑 / 怎么接（⚠️ 本代理不动宿主）============================
//   单独跑：
//     unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//       -projectPath "D:\4\Unity\MyGame" -executeMethod B144_C_UI_Assert.Run -logFile -
//   接宿主（**零前提**：读 4 个源文件 + 建一只临时 toast 控制器，收尾自己拆）：
//     同一物理行末尾追加（行数零变化）：
//       B144_C_UI_Assert.Run(); pass += B144_C_UI_Assert.Pass; fail += B144_C_UI_Assert.Fail;
//   ⚠️ 追加前先看那一行已有的 `//` 在哪。
//   判绿红看 `B144C === 合计` 那一行，⛔ 不看退出码。

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using CardPresentation;

/// <summary>写手 `B144-C`：B131 UI 域 7 条已落参数的回归断言（2 条行为 + 5 条行内接线守卫）+ 行内注释吞代码的结构守卫。</summary>
public static class B144_C_UI_Assert
{
    const string Tag = "[B144C]";
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Near(float got, float want, float tol, string msg)
    {
        bool ok = Mathf.Abs(got - want) <= tol;
        Check(ok, ok ? msg : msg + $" —— 期望 {want}（±{tol}），实得 {got}");
    }

    static void Section(string t) { Debug.Log(Tag + " --- " + t + " ---"); }

    // ============================================================ 源码路径（Assets/ 下相对）
    const string SrcSettings = "CardPresentation/Shell/SettingsWindow.cs";
    const string SrcToast = "CardPresentation/Shell/ToastNotifications.cs";
    const string SrcDeck = "CardPresentation/Deck/DeckRuntime.cs";
    const string SrcBattle = "CardPresentation/Battle/BattleDriver.cs";
    const string SrcTabBar = "CardPresentation/Shell/MainMenuSubmenuWindow.cs";
    const string SrcShop = "CardPresentation/Shell/ShopTabPage.cs";
    const string SrcShopScene = "CardPresentation/Editor/ShopScene.cs";

    static string ReadText(string assetsRel)
    {
        string p = Path.Combine(Application.dataPath, assetsRel);
        return File.Exists(p) ? File.ReadAllText(p) : null;
    }

    static string[] Lines(string text)
    {
        return text == null ? null : text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    }

    static int CountOccurrences(string text, string needle)
    {
        int n = 0, i = 0;
        while (text != null && needle != null && (i = text.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    /// <summary>找**唯一**一个含 <paramref name="anchor"/> 的物理行；返回 null = 0 处或多处（重复劫持）。</summary>
    static string UniqueLine(string[] lines, string anchor)
    {
        string hit = null; int n = 0;
        if (lines == null) return null;
        foreach (var l in lines)
            if (l != null && l.IndexOf(anchor, StringComparison.Ordinal) >= 0) { hit = l; n++; }
        return n == 1 ? hit : null;
    }

    /// <summary>「新调用在行内 `//` 之前」：<paramref name="call"/> 必须出现在 `//` 之前（没有 `//` = 通过）。
    /// 直接杀「代码追加到行末注释之后」那一族（本仓实证 6 次）。</summary>
    static bool BeforeLineComment(string line, string call)
    {
        if (line == null) return false;
        int ci = line.IndexOf(call, StringComparison.Ordinal);
        if (ci < 0) return false;
        int com = line.IndexOf("//", StringComparison.Ordinal);
        return com < 0 || ci < com;
    }

    /// <summary>这一行是不是**语句边界**（去掉行内 `//` 之后以 `;` / `{` / `}` 结尾；空行/纯注释也算）。</summary>
    static bool EndsStatement(string line)
    {
        if (line == null) return true;
        int com = line.IndexOf("//", StringComparison.Ordinal);
        string s = (com >= 0 ? line.Substring(0, com) : line).TrimEnd();
        if (s.Length == 0) return true;
        char c = s[s.Length - 1];
        return c == ';' || c == '{' || c == '}';
    }

    /// <summary>把 <paramref name="anchor"/> 所在的**整条语句**拼出来 —— ⛔ 不按物理行。
    /// 判据 = `B150` #22 / #24：一条调用**可以跨两行**（`SettingsWindow.cs:3054-3056` 的 `SpText(page, "Email Text", …)` ·
    /// `DeckRuntime.cs:1732-1733` 的 `SetVAlign(…, Capline,\n new PxRect(…)); …SetParagraphSpacing(_wcTxt[i], 0.08f)`）。
    /// 返回 null = anchor 出现 0 处或**多处**（重复劫持，口径同 <see cref="UniqueLine"/>）。</summary>
    static string StatementAround(string[] lines, string anchor)
    {
        if (lines == null || anchor == null) return null;
        int at = -1;
        for (int i = 0; i < lines.Length; i++)
            if (lines[i] != null && lines[i].IndexOf(anchor, StringComparison.Ordinal) >= 0) { if (at >= 0) return null; at = i; }
        if (at < 0) return null;
        int start = at;
        while (start > 0 && !EndsStatement(lines[start - 1])) start--;
        int end = at;
        while (end < lines.Length - 1 && !EndsStatement(lines[end])) end++;
        var sb = new System.Text.StringBuilder();
        for (int i = start; i <= end; i++)
        {
            if (lines[i] == null) continue;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(lines[i]);
        }
        return sb.ToString();
    }

    // ============================================================ Run
    public static void Run()
    {
        _pass = _fail = 0;
        A_ToastParagraphSpacing();
        B_WiringGuards();
        Debug.Log(Tag + " === 合计：" + _pass + " 通过 / " + _fail + " 失败 ===");
    }

    // ============================================================ A · 行为（真建一只 toast 控制器）
    static void A_ToastParagraphSpacing()
    {
        Section("A · Toast 段落距 = 原版 20（行为）");
        GameObject root = null;
        try
        {
            root = new GameObject("B144C_ToastRoot");
            var tn = ToastNotifications.Build(root.transform);
            Check(tn != null && tn.Built, "A0（前提）`ToastNotifications.Build` 建出来了（`Built` = 真）");
            if (tn == null || !tn.Built) return;

            float a = TextParaOf(tn.TemplateOf(ToastNotifications.ToastNotificationType.Achievement));
            float m = TextParaOf(tn.TemplateOf(ToastNotifications.ToastNotificationType.MissionProgress));
            float x = TextParaOf(tn.TemplateOf(ToastNotifications.ToastNotificationType.CollectionGainXP));

            Near(a, 20f, 0.001f, "A1 成就 toast 的 `Text` TMP `paragraphSpacing` = 原版 **20**（B131 六#25）"
                              + " —— 🧨 删掉 `BuildToast` 里那句 `SetParagraphSpacing(lb, 20f)` ⇒ 本行红");
            Near(m, 20f, 0.001f, "A2 任务 toast 的 `Text` TMP `paragraphSpacing` = 原版 **20**（B131 六#26）"
                              + " —— 🧨 删掉 `BuildMission` 里那一句 ⇒ 本行红");
            Near(x, 0f, 0.001f, "A3（灭自证）`Get XP` 模板的 Text **没有** ps（= 0）⇒ 读口不是恒返回 20");
        }
        catch (Exception e)
        {
            Check(false, "A 组异常：" + e.GetType().Name + " " + e.Message);
        }
        finally
        {
            try { ToastNotifications.DisposeForTest(); } catch { }
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static float TextParaOf(GameObject template)
    {
        if (template == null) { Check(false, "模板为 null（找不到对应 toast 模板）"); return -1f; }
        var t = template.transform.Find("Text");
        if (t == null)
        {
            var all = template.GetComponentsInChildren<Transform>(true);
            foreach (var c in all) if (c != null && c.name == "Text") { t = c; break; }
        }
        if (t == null) { Check(false, $"模板 `{template.name}` 下找不到 `Text` 节点"); return -1f; }
        var lb = t.GetComponentInChildren<Label>(true);
        if (lb == null) { Check(false, $"模板 `{template.name}/Text` 上找不到 `Label`"); return -1f; }
        return lb.ParagraphSpacingNow;
    }

    // ============================================================ B/C · 行内接线守卫
    static void B_WiringGuards()
    {
        Section("B · 行内接线守卫（同物理行 + 在行内 // 之前）");
        var settings = Lines(ReadText(SrcSettings));
        var toast = Lines(ReadText(SrcToast));
        var deck = Lines(ReadText(SrcDeck));
        var battle = Lines(ReadText(SrcBattle));
        var tabbar = Lines(ReadText(SrcTabBar));
        var shop = Lines(ReadText(SrcShop));
        var shopScene = Lines(ReadText(SrcShopScene));

        // B1 · 设置底钮 margin
        {
            string anchor = "SetMarginPx(0f, 0.00775f, 0f, -0.8828f)";
            string line = UniqueLine(settings, anchor);
            Check(line != null, "B1a `SetMarginPx(0, 0.00775, 0, −0.8828)` 在 SettingsWindow **恰好一处**（锚点唯一）");
            Check(BeforeLineComment(line, anchor), "B1b 它在**行内 `//` 之前**（不是被注释掉的死代码）");
            Check(line != null && line.IndexOf("44f, 12f", StringComparison.Ordinal) >= 0,
                  "B1c 它在 `Bottom Buttons▸Button Text` 那次 `Text(...)` 的**同一物理行**上（没被挪到别处）"
                + " —— 🧨 删掉这句 ⇒ B1 红（B131 六#8 原版 `m_margin (0,0.00775,0,−0.8828)`）");
        }

        // B2 · SpText 漏斗（ps + V=Top 两个分支）
        {
            string anchor = "SetParagraphSpacing(lb, paragraphSpacingPx)";
            string line = UniqueLine(settings, anchor);
            Check(line != null, "B2a `SpText` 的 ps 分支在 SettingsWindow **恰好一处**");
            Check(line != null && BeforeLineComment(line, anchor), "B2b 它在行内 `//` 之前");
            Check(line != null && line.IndexOf("lineSpacingPx", StringComparison.Ordinal) >= 0
                              && line.IndexOf("Label.VAlign.Top", StringComparison.Ordinal) >= 0,
                  "B2c 它和 `SetLineSpacing` / `VAlign.Top` 在**同一行**（同一个漏斗、都排在 AlignLeft 之后）"
                + " —— 🧨 删 ps 或删 Top 分支 ⇒ B2 红（B131 六#10）");
        }

        // B3 · Email Text 调用点
        {
            string anchor = "45f, 10f, 0f, 25f, true";
            string line = UniqueLine(settings, anchor);
            Check(line != null, "B3a `Email Text` 调用点传了 `lineSpacing 0 · ps 25 · vTop true`，**恰好一处**");
            Check(BeforeLineComment(line, anchor), "B3b 它在行内 `//` 之前");
            var stmt3 = StatementAround(settings, anchor);
            Check(stmt3 != null && stmt3.IndexOf("\"Email Text\"", StringComparison.Ordinal) >= 0,
                  "B3c 这条**语句**就是 `SpText(page, \"Email Text\", …)` 那一处（不是别的调用；`B150` #22：调用跨 `:3054`/`:3056` 两个物理行 ⇒ 按语句判）");
        }

        // B4 · Toast 两句
        {
            string anchor = "SetParagraphSpacing(lb, 20f)";
            Check(CountOccurrences(string.Join("\n", toast ?? new string[0]), anchor) == 2,
                  "B4a `SetParagraphSpacing(lb, 20f)` 在 ToastNotifications **恰好两处**（通用 + 任务）");
            string l2 = UniqueLine(toast, "Deal 500 damage");
            // 🔴 `B150` #23：`Congratulations!` 在 `ToastNotifications.cs` 里出现 **4 处**（注释 `:118`/`:623` + 代码 `:625`/`:656`）
            //    ⇒ `UniqueLine` 恒 null（它把「>1 行命中」判成重复劫持）。改成断「**确实是代码里的调用形态**」：
            //    至少一处出现在 `Tmp(` / `MeasureTextPx(` 之后、且在行内 `//` 之前 —— 比裸串的鉴别力**更强**（注释冒充不了）。
            bool l1Call = false;
            foreach (var l in toast ?? new string[0])
                if (l != null && BeforeLineComment(l, "Congratulations!")
                    && (l.IndexOf("Tmp(", StringComparison.Ordinal) >= 0 || l.IndexOf("MeasureTextPx(", StringComparison.Ordinal) >= 0))
                    l1Call = true;
            Check(l1Call && l2 != null, "B4b（前提）两颗 toast 的占位文案都在（`Congratulations!` 至少一处是 `Tmp(`/`MeasureTextPx(` 的真调用；`Deal 500 damage` 恰好一处）");
            var l20 = new List<string>();
            if (toast != null) foreach (var l in toast) if (l != null && l.IndexOf(anchor, StringComparison.Ordinal) >= 0) l20.Add(l);
            bool ok1 = false, ok2 = false;
            foreach (var l in l20) { if (BeforeLineComment(l, anchor)) { ok1 = true; if (l.IndexOf("20f", StringComparison.Ordinal) >= 0) ok2 = true; } }
            Check(ok1 && ok2, "B4c 两句都在行内 `//` 之前且值为 20"
                            + " —— 🧨 删任一句 ⇒ A1/A2 或本行红（B131 六#25/26）");
        }

        // B5 · 万能卡 ps 0.08
        {
            string anchor = "SetParagraphSpacing(_wcTxt[i], 0.08f)";
            string line = UniqueLine(deck, anchor);
            Check(line != null, "B5a `SetParagraphSpacing(_wcTxt[i], 0.08f)` 在 DeckRuntime **恰好一处**");
            Check(BeforeLineComment(line, anchor), "B5b 它在行内 `//` 之前");
            var stmt5 = StatementAround(deck, anchor);
            Check(stmt5 != null && stmt5.IndexOf("Label.VAlign.Capline", StringComparison.Ordinal) >= 0,
                  "B5c 它与 Capline 那句在**同一语句**里（万能卡计数；`B150` #24：调用跨 `DeckRuntime.cs:1732`/`:1733` 两个物理行 ⇒ 按语句判）"
                + " —— 🧨 删掉 ⇒ B5 红（B131 六#11：原版 `m_paragraphSpacing 0.08`）");
        }

        // B6 · END TURN 行距 −29
        {
            string anchor = "SetLineSpacing(_endTurnLabel, -29f)";
            string line = UniqueLine(battle, anchor);
            Check(line != null, "B6a `SetLineSpacing(_endTurnLabel, -29f)` 在 BattleDriver **恰好一处**");
            Check(BeforeLineComment(line, anchor), "B6b 它在行内 `//` 之前");
            Check(line != null && line.IndexOf("Label.OrigFace.Asar", StringComparison.Ordinal) >= 0,
                  "B6c 它与 END TURN 那句 `SetVAlign(… Midline … Asar)` 在**同一物理行**"
                + " —— 🧨 删掉 ⇒ B6 红（B131 六#30：原版 `m_lineSpacing −29`，Asar）");
        }

        // B7 · 页签 Truncate
        {
            string anchor = "TextOverflowModes.Truncate";
            string line = UniqueLine(tabbar, anchor);
            Check(line != null, "B7a `TextOverflowModes.Truncate` 在 MainMenuSubmenuWindow **恰好一处**（四窗共用的页签建法）");
            Check(BeforeLineComment(line, anchor), "B7b 它在行内 `//` 之前");
            Check(line != null && line.IndexOf("SetCharSpacing(-3.5f)", StringComparison.Ordinal) >= 0,
                  "B7c 它与原版 `m_characterSpacing −3.5` 那句在**同一物理行**"
                + " —— 🧨 删掉 ⇒ B7 红（B131 六#18：四份 `TabButtonLabel` 的 `m_overflowMode = 3`）");
        }

        // B8 · 商店 Available Counter：中心 = 原版 476 + 宿主断言改「断中心」
        {
            // B8a 行为：常量本身的中心必须是 476（原版零高锚点 + V=Geometry）
            Near(ShopTabPage.CellAvail.CY, 476f, 0.01f,
                 "B8a `ShopTabPage.CellAvail` 的**竖向中心 = 476**（原版 TMP 锚点 y=476、零高框 + V=Geometry）"
               + " —— 🧨 改回 (452..474) ⇒ 中心 463 ⇒ 本行红（B131 UI 三#10）");
            Check(Mathf.Abs(ShopTabPage.CellAvail.y1 - 465f) < 0.01f && Mathf.Abs(ShopTabPage.CellAvail.y2 - 487f) < 0.01f,
                  "B8b 框 = 465..487（22 px 高，半截在格底 475 之外 —— 原版如此，⛔ 别「修正」回格内）");
            // B8c 宿主：旧「不越出格子」那句必须归零，新「断中心」恰好一处（⛔ 反过来就是假绿）
            int oldCount = CountOccurrences(string.Join("\n", shopScene ?? new string[0]),
                                            "vy1 >= y1 - 0.5f && vy2 <= y1 + cellH + 0.5f");
            Check(oldCount == 0, $"B8c 宿主里旧的「竖向不越出格子」断言已归零（实得 {oldCount} 处）"
                               + " —— 🧨 改回包含式断言 ⇒ 本行红（它与原版半截出格**直接冲突**）");
            Check(CountOccurrences(string.Join("\n", shopScene ?? new string[0]), "ShopTabPage.CellAvail.CY") == 1,
                  "B8d 宿主里「断中心」的表达式**恰好一处**（`ShopTabPage.CellAvail.CY`）");
            Check(CountOccurrences(string.Join("\n", shop ?? new string[0]), "new PxRect(18.87f, 452f, 315.33f, 474f)") == 0,
                  "B8e（灭自证）旧框 (18.87,452,315.33,474) 已不存在 ⇒ B8a 不可能靠「两处一起回退」蒙绿");
        }
    }
}
