// Q1_Assert.cs — 第一百零九会话 · 执行代理 `Q1`（2026-10-31）
//
// ============================ 它盯的两件 ============================
//  A（item ①）**同族字体闸还缺的 6 处**（`报告/C7.md` §六-1 现读清单）—— 取词 / 记号渲染时
//     原来只判 `Loc.Current == AvailableLanguages.Chinese`，**没有** `CardText.Zh`（字体资产那一半）
//     ⇒ 「语档 = 中文 + 中文字体资产缺失」这一档会把中文打成**方块**。共用闸的实现 = `Core/AbilityText.cs`。
//       · `Battle/CardDisplayWindow.cs:899`（`ShowEffectsOf` 的 mark 行左列）
//       · `Battle/CardDisplayWindow.cs:2488`（`MarkRow` 左列）
//       · `Battle/CardDisplayWindow.cs:2640`（`MarkGodEffectText` 的**取词**）+ `:2646`（同一格的记号渲染）
//       · `Core/SupportMethods.cs:350`（`ModifyLocalization` = 原版 `ModifyLocalization` 的移植口）
//       · `Battle/TutorialOverlay.cs:1141`（`TipRichText`）
//       · `Core/CardView.cs:4851`（`CreatedByLine` 的取词）+ `:4860`（缺键时那两句硬拼的中文/英文）
//  B（item ②）`Core/CardText.cs:750 Term` 绕过字体闸：`if (!Loc.HasEntry(term)) return Loc.T(term);`
//     —— 键**只存在于官方全表 / bulk**（不在手写 `Table`）时会**直接 `Loc.T` ⇒ 绕过字体闸**；
//     同一族的 `TermOr`（`:781`）是对的 ⇒ 本件把 `Term` 对齐 `TermOr` 的口径。
//
// ============================ 为什么 A 段是「源码接线闸」============================
//  🔴 **这不是偷懒，是唯一能做到的那一档**：要测出差异必须造出「语档 = 中文 + 中文字体资产缺失」，
//     而那个状态由 `TmpFont.Available` 决定 —— 编辑器里中文字体资产**始终在** ⇒ `CardText.Zh` 恒真
//     ⇒ 行为层永远走不到那条分支。`AbilityText` 为此开了 `SetZhGateForTest`，但 `CardText.Zh` 是
//     **计算属性、没有测试口**（`Core/CardText.cs:38`），而 `Core/TmpFont.cs` 不在本代理白名单。
//     ⇒ A 段断「闸真的接在那一行上」（**改回去就红**），B 段断「行为层没被改坏」（可在编辑器里翻的那一半）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A2/A3/A4：把任意一处 `CardText.Zh && ` 去掉 ⇒ 该文件的计数少 1 ⇒ 红。
//  · A5：把任意一处改回裸 `, Loc.Current == AvailableLanguages.Chinese)` ⇒ 计数 > 0 ⇒ 红。
//    （这条与 A2-A4 **结构上互补**：一处改回，两条里必有一条红 —— 不能靠「两边一起改」变绿。）
//  · A6：把 `Term` 的缺键支改回 `return Loc.T(term);` ⇒ 红。
//  · A7：把 `MarkGodEffectText` 的取词改回 `Loc.T(key)` ⇒ 红。
//  · A8：把 `CreatedByLine` 走的那条改回 `Loc.T(CreatedByTerm)` ⇒ 红。
//  · B5：把 `Term` 的缺键支写成「无条件 `return Loc.OfficialEnglish(term)`」⇒ 中文档那条（B4）红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  唯一入口 = `Q1_Assert.Run()`；宿主里在该宿主那条聚合行**同一物理行、第一个 `//` 之前**追加：
//      Q1_Assert.Run(); pass += Q1_Assert.Pass; fail += Q1_Assert.Fail;
//  （🔴 别追加到行末 `//` 之后 —— 2026-10-30 踩过 6 次：代码被注释吞掉、`typecheck` 全绿但一条没跑。）
//  也可单独跑：`-executeMethod Q1_Assert.Run`。
//
// ============================ 边界（如实记）============================
//  · **没跑 Unity** ⇒ 本件一次都没执行过（本代理只跑秒级 `typecheck`）。
//  · A 段按**源码子串**判，不解析语法树 ⇒ 若将来有人把这几行**重排成多行**，A 段会红而实现可能是对的
//    （那时改断言、别改实现）。这条限制写在 `报告/Q1.md` §五。
//  · B 段用 `Loc.RestoreForTest` 拨语档（**只改内存**、不碰 `PlayerPrefs`），收尾一律 `Loc.ResetForTest()`。
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `Q1`：同族字体闸 6 处 + `CardText.Term` 对齐 `TermOr`。</summary>
public static class Q1_Assert
{
    static readonly CheckSink _sink = new CheckSink("[Q1] ");

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>闸的正确写法（A 段按它计次）。</summary>
    const string Gate = "CardText.Zh && Loc.Current == AvailableLanguages.Chinese";

    /// <summary>闸**缺失**时的那种写法（`RewriteMarks` 的第二实参）。</summary>
    const string BareZh = ", Loc.Current == AvailableLanguages.Chinese)";

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 Q1：同族字体闸 6 处 + `CardText.Term` 对齐 `TermOr`");

        try
        {
            SectionA_Wiring();
            SectionB_Behavior();
        }
        finally
        {
            Loc.ResetForTest();   // 只改内存；⛔ 不碰 PlayerPrefs（`RestoreForTest` 的契约）
        }

        Debug.Log(_sink.P + "Q1 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 源码接线闸
    static void SectionA_Wiring()
    {
        MenuCheck.Section(_sink, "A 字体闸真的接在那一行上（源码子串计次；改回去就红）");

        string root = Application.dataPath + "/CardPresentation/";
        string cdw = Read(root + "Battle/CardDisplayWindow.cs");
        string sup = Read(root + "Core/SupportMethods.cs");
        string tut = Read(root + "Battle/TutorialOverlay.cs");
        string cv = Read(root + "Core/CardView.cs");
        string ct = Read(root + "Core/CardText.cs");

        // A1 —— 前提：五个源文件都读得到（读不到的话下面每条都是空断）
        Tr(!string.IsNullOrEmpty(cdw) && !string.IsNullOrEmpty(sup) && !string.IsNullOrEmpty(tut)
           && !string.IsNullOrEmpty(cv) && !string.IsNullOrEmpty(ct),
           "（前提）五个源文件都读得到（root = " + root + "）");
        if (string.IsNullOrEmpty(cdw) || string.IsNullOrEmpty(sup) || string.IsNullOrEmpty(tut)
            || string.IsNullOrEmpty(cv) || string.IsNullOrEmpty(ct))
            return;   // 前提不成立 ⇒ 后面的结论一条都不下（⛔ 不用 `if (前提) Check(结论)` 那种静默跳过）

        // A2 —— CardDisplayWindow：三处（:899 / :2488 / :2646）
        C(Count(cdw, Gate), 3, "`Battle/CardDisplayWindow.cs` 里 `CardText.Zh && Loc.Current == Chinese` 出现 3 次（:899 / :2488 / :2646）");
        // A3 —— SupportMethods / TutorialOverlay：各一处
        C(Count(sup, Gate), 1, "`Core/SupportMethods.cs` 里闸出现 1 次（`ModifyLocalization`）");
        C(Count(tut, Gate), 1, "`Battle/TutorialOverlay.cs` 里闸出现 1 次（`TipRichText`）");
        // A4 —— CardView：一处（缺键兜底那两句的中/英选择）
        C(Count(cv, Gate), 1, "`Core/CardView.cs` 里闸出现 1 次（`CreatedByLine` 的缺键兜底）");
        // A5 —— 反向：这四个文件里**不再有**裸 `RewriteMarks(…, Loc.Current == Chinese)`
        int bare = Count(cdw, BareZh) + Count(sup, BareZh) + Count(tut, BareZh) + Count(cv, BareZh);
        C(bare, 0, "这四个文件里裸 `, Loc.Current == AvailableLanguages.Chinese)`（缺 `CardText.Zh` 的写法）出现 0 次");

        // A6 —— `CardText.Term` 的缺键支（对齐 `TermOr` 的口径）
        Tr(ct.Contains("if (!Loc.HasEntry(term)) { string vo = (Loc.Current == AvailableLanguages.Chinese && !Zh)"),
           "`Core/CardText.cs` 的 `Term` 缺键支走字体闸：`!Zh && 中文 ⇒ Loc.OfficialEnglish`（原 `return Loc.T(term)` 已不在）");
        Tr(ct.Contains("Loc.OfficialCurrent(term)"), "`CardText.Term` 缺键支在中文可用档取 `Loc.OfficialCurrent(term)`（与 `TermOr` 同一口径）");
        // A7 —— `MarkGodEffectText` 的取词闸
        Tr(cdw.Contains("(Loc.Current == AvailableLanguages.Chinese && !CardText.Zh) ? Loc.OfficialEnglish(key) : Loc.T(key)"),
           "`CardDisplayWindow.MarkGodEffectText` 的取词走字体闸（中文档 + 缺中文字体 ⇒ 官方英文列）");
        // A8 —— `CreatedByLine` 的取词改走 `TermOr`（同一套闸）
        Tr(cv.Contains("CardText.TermOr(CreatedByTerm, null)"),
           "`CardView.CreatedByLine` 的取词改走 `CardText.TermOr`（含字体闸；原直接 `Loc.T`）");
    }

    // ============================================================ B · 行为层（编辑器里翻得到的那一半）
    static void SectionB_Behavior()
    {
        MenuCheck.Section(_sink, "B `CardText.Term` 缺键支的行为（中/英两档；字体那一半编辑器里测不到）");

        // B1 —— 前提：官方全表里 `Card_Abilities/*` 这一族在（下面挑的键就出自它）
        string[] keys = Loc.Lang13Keys("Card_Abilities/");
        int keyCount = keys == null ? 0 : keys.Length;
        Tr(keyCount > 0, "（前提）官方 13 语表里 `Card_Abilities/*` 有键（实得 " + keyCount + " 条）");
        if (keyCount == 0) return;

        // 拨到中文档（只改内存）
        Loc.RestoreForTest(AvailableLanguages.Chinese);

        // B2 —— 挑一条「手写 Table 里没有、官方全表里中英两列都非空且不同」的键
        string probe = null, zh = null, en = null;
        foreach (string k in keys)
        {
            if (string.IsNullOrEmpty(k) || Loc.HasEntry(k)) continue;   // 要的正是「绕过 HasEntry」那一族
            string a = Loc.OfficialCurrent(k);                            // 中文档 ⇒ 中文列
            string b = Loc.OfficialEnglish(k);
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || a == b) continue;   // 两列必须可分辨
            probe = k; zh = a; en = b;
            break;
        }
        Tr(probe != null, "（前提）找到一条「只在官方全表、中英两列都非空且不同」的 `Card_Abilities/*` 键");
        Tr(!string.IsNullOrEmpty(probe) && CardText.Zh,
           "（前提）编辑器里中文字体资产在 ⇒ `CardText.Zh` 为真（否则中/英两档不可分辨）");
        if (probe == null || !CardText.Zh) return;

        // B3 —— 中文 + 字体在 ⇒ 中文列（这一条钉住「Term 的缺键支没被改成无条件英文」）
        C(CardText.Term(probe), zh, "中文档 + 字体在：`Term(\"" + probe + "\")` = 官方中文列");
        Tr(CardText.Term(probe) != en, "中文档 + 字体在：`Term` 给的是中文列、不是英文列（B3 的灭自证）");
        // B4 —— 与 `TermOr` 同一口径（本件 item ② 的判据）
        C(CardText.TermOr(probe, "§Q1-NO-FALLBACK§"), CardText.Term(probe),
          "`Term` 与 `TermOr` 对同一条只存在于官方全表的键给同一个值（口径已对齐）");
        // B5 —— 英文档 ⇒ 英文列
        Loc.RestoreForTest(AvailableLanguages.English);
        Tr(!CardText.Zh, "（前提）英文档 ⇒ `CardText.Zh` 为假");
        C(CardText.Term(probe), en, "英文档：`Term` 回官方英文列（与改前的 `Loc.T` 逐字相同）");
        // B6 —— 缺键 ⇒ 回**键名本身**（不静默、不变空）—— 这条钉住「出声那一档没丢」
        Loc.RestoreForTest(AvailableLanguages.Chinese);
        const string miss = "Card_Abilities/§Q1_不存在的键§";
        Tr(!Loc.HasEntry(miss) && string.IsNullOrEmpty(Loc.OfficialCurrent(miss)),
           "（前提）探针键确实哪儿都没有");
        C(CardText.Term(miss), miss, "缺键 ⇒ `Term` 回键名本身（⛔ 不静默变空）");
    }

    // ============================================================ 工具
    static string Read(string path)
    {
        try { return File.Exists(path) ? File.ReadAllText(path) : ""; }
        catch (System.Exception e) { Debug.LogWarning("[Q1] 读不到 " + path + "：" + e.Message); return ""; }
    }

    /// <summary>子串出现次数（`string.IndexOf` 逐次前移；空串 ⇒ 0）。</summary>
    static int Count(string hay, string needle)
    {
        if (string.IsNullOrEmpty(hay) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = hay.IndexOf(needle, i, System.StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }
}
