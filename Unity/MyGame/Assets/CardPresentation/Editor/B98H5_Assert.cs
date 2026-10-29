// B98H5_Assert.cs — 执行代理 `B98_H5`（第九十八会话 · 波 7 · 日常线）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `日常/奖励:272` `FastModeBaseEvent.OnMatchProcessSuccess(OnlineMatchResult)` 的**最后一支**
//   —— 服务器结算返回体里的加成表（`EndMatchData.RatingBoostState`）落到本地的那些加成事件上：
//
//     原版：`foreach (b in RankedScoreBoostHandler.GetBoostsFor(this))
//              b.<slot47>(RatingBoostState.FirstOrDefault(x => x.id == b.id))`
//   本轮把 `slot47` 解掉（2026-10-29 `B98_H5` 现读三处）：
//     · `d:/2/tools/il2cpp_out/dump.cs` 的 `LiveOpsEvent` 区 `Slot: 47` = `UpdateSaveGame(LiveOpsEventPersistence)`
//     · ILSpy `cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/LiveOpsEvent.cs:148`（抽象）+ `:944`（体：
//       `if (save != null) { this+0x20 = save; LoadEventData(data, save, null); }`）
//     · `RankedScoreBoostSave`（`dump.cs` TypeDefIndex 3584）唯一字段 = `Dictionary<CardArmy,int> matchCount`
//   ⇒ 落点 = `Shell/RankedData.cs` 的 `RankedBoostEvent.UpdateSaveGame(RankedScoreBoostSave)`（类尾），
//     调用点 = `Shell/FastModeBaseEvent.cs` 的 `OnMatchProcessSuccess`。
//
// ============================ 怎么用（宿主）============================
// `B98H5Assert.Run();`
//   + 宿主里两行（同族先例 `B97W9Assert` / `B97W7Assert`）：
//     `_sink.Pass += B98H5Assert.Pass; _sink.Fail += B98H5Assert.Fail;`
//   · **不建 GameObject / 不建资产 / 不开窗** ⇒ 任何宿主都能跑（推荐 `Editor/ShellScene.cs`，活动线宿主）。
//   · 读源码走 `Application.dataPath` ⇒ 只在编辑器宿主里成立。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A2/A3：把 `matchCount` 那半删掉（只剩 `Save = save`）⇒ A2/A3 红。
//   · A4：把「按阵营 key 逐个写」改成「整表刷成同一个数」⇒ A4 红（它专门用**只给一个阵营**的状态）。
//   · A5：把 `Save = save` 删掉 ⇒ A5 红。
//   · B1/B3：把 `Includes` 改成恒真 ⇒ B3 红；改成恒假 ⇒ B1 红。
//   · C1：把调用点那一行注释掉 ⇒ C1 红（扫描前已抹注释，所以「写进注释」骗不过它）。
//   · C2：把原版那道闸（`RatingBoostState.Length > 0`）删掉 ⇒ C2 红。
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B98_H5`：账 `日常/奖励:272` 的 boosts 支（原版 `LiveOpsEvent.UpdateSaveGame`）。
/// <para>⛔ 期望值全是字面量（`4` / `9` / 字段名本身），不从被测实现里读回来。</para></summary>
public static class B98H5Assert
{
    static readonly CheckSink _sink = new CheckSink("[B98H5] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    const string SrcFastMode = "CardPresentation/Shell/FastModeBaseEvent.cs";
    const string SrcRanked = "CardPresentation/Shell/RankedData.cs";

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B98_H5 账 `日常/奖励:272`：boosts 支（`slot47` = `LiveOpsEvent.UpdateSaveGame`）");
        SectionA_行为();
        SectionB_按id命中();
        SectionC_源码锁();
        Debug.Log(_sink.P + "B98_H5 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ A 行为：把服务器状态写进加成事件
    static void SectionA_行为()
    {
        MenuCheck.Section(_sink, "A 行为：`RankedBoostEvent.UpdateSaveGame` 按 `matchCount` 逐阵营刷新胜场");

        // A1 —— 前提（硬断言：前提不成立就红，⛔ 不用 `if (x != null) Check(...)` 静默跳过）
        var b = new RankedBoostEvent { Id = "evt1", Armies = new[] { 3, 7 }, WinsByArmy = new[] { 0, 0 } };
        Tr(b != null && b.WinsFor(3) == 0 && b.WinsFor(7) == 0,
           "A1 **前提**：夹具（`Armies = {3,7}`、`WinsByArmy = {0,0}`）建好了，且 `WinsFor` 出厂读 0");

        var st = new RankedScoreBoostSave { id = "evt1", matchCount = new System.Collections.Generic.Dictionary<int, int> { { 3, 4 }, { 7, 9 } } };
        b.UpdateSaveGame(st);
        C(b.WinsFor(3), 4, "A2 写进 `matchCount[3] = 4` 之后 `WinsFor(3)` == 4（原版 `RankedScoreBoostSave.GetWins(army)` 读的就是这张表）");
        C(b.WinsFor(7), 9, "A3 同理 `WinsFor(7)` == 9（两个阵营各自取自己的值 ⇒ 不是「整表刷同一个数」）");

        // A4 —— 灭自证：状态里**只**有阵营 3 的字 ⇒ 阵营 7 必须保持原值（整表覆盖写会在这里红）
        var b2 = new RankedBoostEvent { Id = "evt2", Armies = new[] { 3, 7 }, WinsByArmy = new[] { 11, 22 } };
        b2.UpdateSaveGame(new RankedScoreBoostSave { id = "evt2", matchCount = new System.Collections.Generic.Dictionary<int, int> { { 3, 5 } } });
        C(b2.WinsFor(3), 5, "A4a 命中键的那一格被改写（3 ⇒ 5）");
        C(b2.WinsFor(7), 22, "A4b **灭自证**：状态里**没有**阵营 7 的键 ⇒ 它的胜场必须原样留着（22）｜改成「逐格刷成同一个数」⇒ 本条红");

        // A5 —— 原版那一跳的「写存档」：状态对象必须被**留在事件上**（原版 `this+0x20 = save`）
        Tr(ReferenceEquals(b.Save, st), "A5 `UpdateSaveGame` 把状态写进 `Save`（= 原版 `LiveOpsEvent.UpdateSaveGame` 的 `this+0x20 = save`）｜删掉那一句 ⇒ 红");

        // A6 —— `null` 空转（原版 `if (save != null)`；调用点 `FirstOrDefault` 没命中就是 null）
        var b3 = new RankedBoostEvent { Id = "evt3", Armies = new[] { 3 }, WinsByArmy = new[] { 8 } };
        b3.UpdateSaveGame(null);
        C(b3.WinsFor(3), 8, "A6 喂 `null` ⇒ 一格都不动（原版 `LiveOpsEvent.UpdateSaveGame` 的第一道闸就是 `if (save != null)`）");
        Tr(b3.Save == null, "A6b 喂 `null` 也不能把 `Save` 写成别的东西（原版这一支直接 return）");
    }

    // ------------------------------------------------------------ B 按 id 命中（原版 FirstOrDefault 的谓词）
    static void SectionB_按id命中()
    {
        MenuCheck.Section(_sink, "B 按 id 命中：`RankedData.GetBoostsFor(eventId)` = 原版 `RankedScoreBoostHandler.GetBoostsFor(this)`");

        var a = new RankedBoostEvent { Id = "evtA" };
        var c = new RankedBoostEvent { Id = "evtC" };
        RankedData.SetBoostsForTest(new[] { a, c });

        var gotA = RankedData.GetBoostsFor("evtA");
        Tr(gotA != null && gotA.Length == 1 && gotA[0] != null && gotA[0].Id == "evtA",
           "B1 `GetBoostsFor(\"evtA\")` 恰 1 条且是 id 匹配的那条（`Includes(eventId)` = 原版 `IncludeEvent(string)` 的 `e.TargetId == eventId`）");

        var gotC = RankedData.GetBoostsFor("evtC");
        Tr(gotC != null && gotC.Length == 1 && gotC[0] != null && gotC[0].Id == "evtC",
           "B2 **灭自证**：`GetBoostsFor(\"evtC\")` 拿到的是**另一条**（把 `Includes` 写成恒真 ⇒ B1/B2 一起红；恒假 ⇒ 一起红）");

        var none = RankedData.GetBoostsFor("no-such-event");
        C(none == null ? -1 : none.Length, 0,
           "B3 没挂在本活动上的 id ⇒ 0 条（原版 `Where(IncludeEvent)` 同形；这一支就等于 `foreach` 不转）");

        RankedData.SetBoostsForTest(null);   // 收尾：别把夹具漏给别的宿主
        C(RankedData.Boosts == null ? -1 : RankedData.Boosts.Length, 0, "B4 **收尾**：夹具已清空（`SetBoostsForTest(null)` ⇒ 0 条）");
    }

    // ------------------------------------------------------------ C 源码锁：调用点真的在代码里
    static void SectionC_源码锁()
    {
        MenuCheck.Section(_sink, "C 源码锁：`GetBoostsFor(GetID())` → `UpdateSaveGame(FindBoostState(…))` 在**可执行代码**里");

        // C0 —— 扫描器自检（与 B97W9_Assert 同一套：抹注释 + 抹字符串内容，保留换行）
        const string probe = "// b.UpdateSaveGame(FindBoostState(\n/* x */\nvoid Y() { b.UpdateSaveGame(FindBoostState(1)); }";
        C(CountOf(StripCommentsAndStrings(probe), "UpdateSaveGame"), 1,
           "C0 **前提**：去注释/去字符串的扫描器本身是对的（探针里 3 处 `UpdateSaveGame`、2 处在注释里 ⇒ 扫描后必须只剩 1 处）");

        string fullF = Path.Combine(Application.dataPath, SrcFastMode.Replace('/', Path.DirectorySeparatorChar));
        bool exF = File.Exists(fullF);
        Tr(exF, "C1 **前提**：`" + SrcFastMode + "` 在（读不到 ⇒ C2..C4 没跑；本文件不静默跳过）｜实读路径 = " + fullF);
        if (!exF) return;
        string codeF = StripCommentsAndStrings(File.ReadAllText(fullF));

        C(CountOf(codeF, "UpdateSaveGame"), 1,
           "C2 调用点在**可执行代码**里恰好 1 处（写进 `//` 注释骗不过它 —— 扫描前已把注释抹成空格）");

        Tr(codeF.Contains("RankedData.GetBoostsFor(GetID())"),
           "C3 取加成走 `RankedData.GetBoostsFor(GetID())`（= 原版 `RankedScoreBoostHandler.GetBoostsFor(this)`；"
         + "⛔ 别改成遍历本文件的 `RankedScoreBoostEvent` 替身 —— 那个没有 id、没有消费口）");

        Tr(codeF.Contains("emd.RatingBoostState != null && emd.RatingBoostState.Length > 0"),
           "C4 原版那道闸 `boost 表 Count > 0` 在（删掉 ⇒ 空表也会进循环，与原版分支形状不符）");

        string fullR = Path.Combine(Application.dataPath, SrcRanked.Replace('/', Path.DirectorySeparatorChar));
        bool exR = File.Exists(fullR);
        Tr(exR, "C5 **前提**：`" + SrcRanked + "` 在（读不到 ⇒ C6 没跑）｜实读路径 = " + fullR);
        if (!exR) return;
        string codeR = StripCommentsAndStrings(File.ReadAllText(fullR));

        C(CountOfDefs(codeR, "UpdateSaveGame"), 1,
           "C6 `void UpdateSaveGame(…)` 在 `RankedBoostEvent` 里**恰好 1 个定义**（有两份实现迟早不一致）；"
         + "且实参类型是 `RankedScoreBoostSave` —— 原版那个虚槽 47 的签名是 `UpdateSaveGame(LiveOpsEventPersistence)`");

        Tr(new Regex(@"void\s+UpdateSaveGame\s*\(\s*RankedScoreBoostSave\s+\w+\s*\)").IsMatch(codeR),
           "C7 形参类型逐字是 `RankedScoreBoostSave`（换成别的类型 = 那条 `FirstOrDefault` 的结果塞不进来 ⇒ 红）");
    }

    // ============================================================ 源码工具（本文件自用，别外借）
    /// <summary>把注释整段、字符串/字符**内容**一起抹成空格（保留换行 ⇒ 行号不变）。
    /// <para>⛔ 只抹注释不够：本批的说明里到处写着 `UpdateSaveGame`，只抹注释会把「注释里的示例」数成「调用」。</para></summary>
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
}
