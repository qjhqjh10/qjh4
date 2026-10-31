// B109_E22_PendingMode_Assert.cs — 执行代理 `B109_E22`（2026-10-31 · 第一百零九会话 · 波 1）
//
// ============================ 它盯的是什么 ============================
//  AUD03 #74/#75（判据 = `判据/AUD03_reuse_paths.tsv:74` / `:75` · 施工单 `判据/B103_R1_AUD03施工单.md` §#74/#75）：
//  **战斗静态通道 `_pendingPlayMode` 的第二局残留** ——
//    · 教程支：`BeginFromDeckLibrary` 的 `if (tutIndex >= 0) { BeginTutorial(tutIndex); return; }`
//    · 联机支：`if (pbNet != null) { BeginFromPendingCore(...); return; }`
//  两支都**提前 return**，而 `TakePendingPlayMode()` 原来排在它们**之后** ⇒ 教程局 / 联机局打完，
//  静态通道里还留着 `Tutorial(4)` / `ClosedDeck(7)` ⇒ **下一局普通对战**把它当入口模式号
//  （`PickBotDeck` 按 4 去筛预组 · `Ctx.PlayMode` 写成 4 ⇒ `InstallMissionChallengeMatchGates` 跟着错）。
//  口径 = **先取再分支**（通道「读一次就清」，同 `NetPendingBattle.Take`）。
//
//  🔎 现读结论（2026-10-31 · 铁律 5）：**代码修复已在** —— `BattleDriver.cs:4523` 同一句里
//     `TakePendingTutorialStage()` + `TakePendingPlayMode()` 都取了；`:4570` 用局部量 `pendingPlayMode`、
//     **不再 Take**（commit `fbf02b8f` · 第一百零三会话）。当时缺的是**端到端断言**：
//     `B103R1_Assert` 的 B7 只能断「通道语义」，把「教程/联机两支也要消化掉」这一条挂给了宿主。
//     ⇒ 本件就是补那一半（B 组）。
//
// ============================ 两组入口 ============================
//  · `Run()` —— **自包含**（不需要 driver / 不建窗 / 不写盘 / 不切场景）：
//      A 组：通道语义（前提）+ **源码形状**（钉住「先取再分支」不许被改回去）。
//  · `RunInBattle(BattleDriver driver)` —— **端到端**，需要**一局活着的 driver**（`driver.Ctx != null`）：
//      武装教程两条通道 → `BeginFromDeckLibrary()`（走教程支）→ 再 `BeginFromDeckLibrary()`（普通开局）
//      → 断 `PendingPlayMode == null` · `Ctx.PlayMode == Classic(0)` · 第二局 bot deck 的 `GameMode != Tutorial(4)`。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  1) 自包含那组：任选一个宿主，在聚合行**同一物理行、`//` 之前**追加：
//       B109_E22_PendingMode_Assert.Run(); _sink.Pass += B109_E22_PendingMode_Assert.Pass; _sink.Fail += B109_E22_PendingMode_Assert.Fail;
//  2) 端到端那组：**只在 `Editor/BattleScene.cs`** 接，而且**必须接在所有小节跑完之后**
//     （`DestroyImmediate(driver)` 之前），写成：
//       B109_E22_PendingMode_Assert.RunInBattle(driver); pass += B109_E22_PendingMode_Assert.Pass; fail += B109_E22_PendingMode_Assert.Fail;
//     ⛔ **别在中间小节插** —— 它会重开两局（教程 + 普通），后面那些小节假设的是它们自己开的那一局。
//     ⚠️ 它**只读**卡组库（`PickSavedDeck`）、**不写盘**；跑完 driver 停在**普通经典局**上。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · A6/A7/A8：把「同一句先取」改回「只取教程关号」、或把 Take 挪到分支之后 ⇒ 红。
//  · A10：把 `GameMode playMode = pendingPlayMode ?? …` 改回 `TakePendingPlayMode() ?? …` ⇒ 红。
//  · B3：把「先取再分支」改回去 ⇒ 红（教程支跑完 `PendingPlayMode` 仍是 Tutorial(4)）。
//  · B4b/B4c：同上 ⇒ 红（第二局普通开局把残留的 Tutorial 当模式号 ⇒ `Ctx.PlayMode == Tutorial(4)`）。
//  · B4e：`_botDeckForTest` 的写口（`BattleDriver.cs:5411` 同一物理行）删掉 ⇒ B4d 前提红、B4e 失去判别力。
using System;
using System.IO;
using System.Text.RegularExpressions;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B109_E22`：AUD03 #74/#75 —— 战斗静态通道 `_pendingPlayMode` 的跨局残留。
/// <para>⛔ 期望值一律是**字面量**（`GameMode.Classic` / `GameMode.Tutorial` / `-1` / `null`），
/// 不从被测实现里读回来（铁律：断言不许自证）。</para></summary>
public static class B109_E22_PendingMode_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B109E22] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }

    const string SrcDriver = "CardPresentation/Battle/BattleDriver.cs";

    // ============================================================ A · 自包含
    /// <summary>通道语义前提 + **源码形状**（钉住「先取再分支」）。⛔ 不需要 driver、不建窗、不写盘。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "B109_E22 · AUD03 #74/#75（`_pendingPlayMode` 第二局残留）· 通道语义 + 源码形状");
        try { A_ChannelAndShape(); }
        catch (Exception e)
        {
            Tr(false, "「通道语义 / 源码形状」这一组抛异常（后面的条目没跑完）："
                    + e.GetType().Name + "：" + e.Message);
        }
        Debug.Log(_sink.P + "B109_E22 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    static void A_ChannelAndShape()
    {
        // ---- 前提：通道「读一次就清」这条口径本身（#74 的语义地基；B103R1 的 B4-B6 已锁过一遍，这里作前提再钉一次）----
        var prev = BattleDriver.TakePendingPlayMode();
        int prevStage = BattleDriver.TakePendingTutorialStage();
        try
        {
            C(BattleDriver.PendingPlayMode, (GameMode?)null,
              "A1-前提①：通道**初始为空**（`PendingPlayMode == null`）—— 不成立说明上一步有人留了东西，本件后面几条都不可信");
            BattleDriver.SetPendingPlayMode(GameMode.Tutorial);
            C(BattleDriver.PendingPlayMode, (GameMode?)GameMode.Tutorial,
              "A2-前提②：`SetPendingPlayMode(Tutorial)` 之后读得到 Tutorial(4)");
            C(BattleDriver.TakePendingPlayMode(), (GameMode?)GameMode.Tutorial,
              "A3-前提③：`TakePendingPlayMode()` 取出 Tutorial(4)");
            Tr(BattleDriver.PendingPlayMode == null,
               "A4 ★ 读完即清（`PendingPlayMode == null`）—— 通道口径 = 「读一次就清」（同 `NetPendingBattle.Take`）");
        }
        finally
        {
            BattleDriver.ClearPendingBattleChannels();
            if (prevStage >= 0) BattleDriver.SetPendingTutorialStage(prevStage);
            if (prev != null) BattleDriver.SetPendingPlayMode(prev.Value);
        }

        // ---- 源码形状：`BeginFromDeckLibrary` 必须「先取再分支」----
        string drv = Src(SrcDriver);
        Tr(drv.Length > 1000, "A5-前提④：`" + SrcDriver + "` 源码读得到");
        if (drv.Length <= 1000) return;
        string body = MemberSlice(drv, "void BeginFromDeckLibrary()");
        Tr(body.Length > 500, "A5-前提⑤：`BeginFromDeckLibrary` 切片取到了（" + body.Length + " 字符）");
        if (body.Length <= 500) return;

        int takeIdx = body.IndexOf("var pendingPlayMode = TakePendingPlayMode()", StringComparison.Ordinal);
        int tutIdx = body.IndexOf("if (tutIndex >= 0)", StringComparison.Ordinal);
        int netIdx = body.IndexOf("NetPendingBattle.Take()", StringComparison.Ordinal);
        int useIdx = body.IndexOf("GameMode playMode = pendingPlayMode ??", StringComparison.Ordinal);

        Tr(takeIdx >= 0,
           "A6 ★ #74 同一句里**先取**两条通道（`var pendingPlayMode = TakePendingPlayMode()`）"
         + " —— 🧨 改回「只取教程关号」⇒ 本条红");
        Tr(takeIdx >= 0 && tutIdx > takeIdx,
           "A7 ★★ #74 **次序判别式①**：取通道排在**教程支** `if (tutIndex >= 0)` **之前**"
         + "（take=" + takeIdx + " / tut=" + tutIdx + "）"
         + " —— 🧨 把 Take 挪回分支之后 ⇒ 教程局打完 `_pendingPlayMode` 原样留在静态通道里");
        Tr(takeIdx >= 0 && netIdx > takeIdx,
           "A8 ★★ #74 **次序判别式②**：取通道排在**联机支** `NetPendingBattle.Take()` **之前**"
         + "（take=" + takeIdx + " / net=" + netIdx + "）"
         + " —— 🧨 同上（联机局打完同样会把 `ClosedDeck(7)` 漏进下一局）");
        Tr(useIdx > takeIdx,
           "A9 ★ 模式号那一行用的是**局部量** `pendingPlayMode ?? …`（第 " + useIdx + " 字符处）"
         + " —— 说明「先取」与「后用」是同一个值，中间没有第二处读通道");
        Tr(!body.Contains("GameMode playMode = TakePendingPlayMode()"),
           "A10 ★★ 旧写法「在模式号那一行才 `TakePendingPlayMode()`」**必须不存在**"
         + " —— 🧨 把建局那一行改回 Take ⇒ 本条红（这一条是 A7/A8 的**反证**：光有次序判别式，"
         + "「同句里多留一处 take」仍可能把旧写法蒙混过去）");
    }

    // ============================================================ B · 端到端（需要一局活着的 driver）
    /// <summary>端到端：教程通道武装 → 教程支 → 再普通开局。
    /// <para>⚠️ 会**重开两局**（教程 stage 0 + 一局普通经典）⇒ 只在宿主的战斗小节**全部跑完之后**接。</para>
    /// <para>🔴 前提不成立（没有 driver / 没开局 / 本地没有第 1 关教程数据）时**先报红再返回** ——
    /// ⛔ 不是静默跳过（`if (x != null) Check(...)` 那种写法会让本条一次都没跑过还显示全绿）。</para></summary>
    public static void RunInBattle(BattleDriver driver)
    {
        MenuCheck.Section(_sink, "B109_E22 · AUD03 #74/#75 端到端：教程局打完再开普通局，静态通道不许把 Tutorial(4) 带进来");

        Tr(driver != null, "B0-前提①：调用方传了 driver");
        if (driver == null) return;
        Tr(driver.Ctx != null, "B0-前提②：driver 已经开过一局（`Ctx != null`）—— 没开 ⇒ 下面几条无意义");
        if (driver.Ctx == null) return;

        // ⛔ 教程数据缺失时**不调** `BeginFromDeckLibrary` —— `BeginTutorial` 的失败档会 `FailTutorialStartAndExit()`
        //    （= 切场景），那会把宿主当场带崩。先硬断前提，红就停。
        var stage0 = TutorialData.ByIndex(0);
        Tr(stage0 != null, "B0-前提③：本地有第 1 关教程数据（`TutorialData.ByIndex(0) != null`）");
        if (stage0 == null) return;

        // 保存两条通道（跑完还原：⛔ 不许把「我塞进去的 Tutorial」留给下一局 —— 那正是本件要防的串局形状）
        var prev = BattleDriver.TakePendingPlayMode();
        int prevStage = BattleDriver.TakePendingTutorialStage();
        try
        {
            // ---- ① 像 `TutorialModePopup.PlayTutorial` 那样武装两条通道 ----
            BattleDriver.SetPendingTutorialStage(0);
            BattleDriver.SetPendingPlayMode(GameMode.Tutorial);
            C(BattleDriver.PendingPlayMode, (GameMode?)GameMode.Tutorial,
              "B1-前提④：入口窗口径 —— 两条通道都装好了（`PendingPlayMode == Tutorial(4)`）");

            // ---- ② 教程支 ----
            driver.BeginFromDeckLibrary();
            Tr(driver.Ctx != null && MatchTypes.IsTutorialMatch(driver.Ctx.MatchType),
               "B2-前提⑤：`BeginFromDeckLibrary()` 真的走了**教程支**（`MatchTypes.IsTutorialMatch(Ctx.MatchType)`）"
             + " —— 不成立 ⇒ 下面 B3 失去判别力（实际 MatchType 见日志）");
            Tr(BattleDriver.PendingPlayMode == null,
               "B3 ★★ #74 教程支**已经把模式通道消化掉**（`PendingPlayMode == null`）"
             + " —— 🧨 把「先取再分支」改回去 ⇒ 这里读回 Tutorial(4)（= 下一局会拿它当入口模式号）");
            C(BattleDriver.TakePendingTutorialStage(), -1,
              "B3b 教程关号同样「读一次就清」（`TakePendingTutorialStage()` = -1）");

            // ---- ③ 第二局：普通开局（这一局**不该**再看到任何残留模式号）----
            driver.BeginFromDeckLibrary();
            Tr(driver.Ctx != null, "B4-前提⑥：第二局开出来了（`Ctx != null`）");
            if (driver.Ctx != null)
            {
                Tr(BattleDriver.PendingPlayMode == null,
                   "B4a ★ 第二局开局之后通道仍是空的（`PendingPlayMode == null`）");
                C(driver.Ctx.PlayMode, GameMode.Classic,
                  "B4b ★★ #74 第二局普通开局的 `Ctx.PlayMode` = Classic(0)"
                + " —— 🧨 泄漏时这里是 Tutorial(4)，`InstallMissionChallengeMatchGates` / 骷髅档 / 里程碑一起错");
                C(driver.PlayMode, GameMode.Classic,
                  "B4c ★ `driver.PlayMode`（`_playMode`）同步 = Classic(0)（结算/HUD 读的是它）");

                var bot = driver.BotDeckForTest;
                Tr(bot != null,
                   "B4d-前提⑦：第二局对手用的是**预组池里挑的那副**（`BotDeckForTest != null`）"
                 + " —— 为 null 说明本局没走 `PickBotDeck`（显式 `foeDeck` / 自动凑），B4e 无判别力，故按前提红报");
                Tr(bot == null || bot.GameMode != (int)GameMode.Tutorial,
                   "B4e ★★ #75 第二局 bot deck 的 `GameMode` ≠ Tutorial(4)（实得 "
                 + (bot == null ? "null（本局没走预组池）" : bot.GameMode.ToString())
                 + "）—— 🧨 残留的入口模式号会让 `PickBotDeck` 按 4 去筛（挑不到 ⇒ 退回自动凑，仍然错档）");
            }
        }
        finally
        {
            BattleDriver.ClearPendingBattleChannels();
            if (prevStage >= 0) BattleDriver.SetPendingTutorialStage(prevStage);
            if (prev != null) BattleDriver.SetPendingPlayMode(prev.Value);
            Debug.Log(_sink.P + "B109_E22（端到端）合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
        }
    }

    // ============================================================ 工具
    static string Src(string assetRelativePath)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, assetRelativePath);
            return File.Exists(p) ? File.ReadAllText(p) : "";
        }
        catch (Exception e)
        {
            Debug.LogWarning("[B109E22] 读源码失败 `" + assetRelativePath + "`：" + e.Message);
            return "";
        }
    }

    /// <summary>从一条**方法声明**起截到下一个「8 空格缩进的类成员声明」之前。
    /// <para>⚠️ 只是「够用的近似切片」：本件只用它做 `IndexOf` / `Contains`，⛔ 不拿它判语法结构。</para></summary>
    static string MemberSlice(string src, string declMarker)
    {
        int i = src.IndexOf(declMarker, StringComparison.Ordinal);
        if (i < 0) return "";
        int from = i + declMarker.Length;
        var m = Regex.Match(src.Substring(from),
            "\n {8}(?:public|private|protected|internal|static|sealed|readonly|void|bool|int|float|string|double|long|IEnumerator|System\\.Collections|List<|Dictionary<)");
        int end = m.Success ? from + m.Index : src.Length;
        return src.Substring(i, end - i);
    }
}
