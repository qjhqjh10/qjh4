// B117_G10_16_Assert.cs — 执行代理 `B117-G10-16`（2026-11-02）· 战斗域第 16 分片「回放 · 录像」
//
// ============================ 它盯的是什么 ============================
//  本分片的判据（逐条带出处，全部现读）。⛔ 本文件**不改任何既有宿主**，只提供 `Run()` / `Run(driver)`。
//
//    A. **回放条 = 原版 `ReplayButtons` 那 6 个节点**（`battlearena2` 现读）：
//       `ReplayButtons`(GO 125 / RT 3550) → `Holder`(GO 1084 / RT 3133) → `Replay`(GO 164 / RT 3484) ·
//       `Play`(GO 769 / RT 2953) · `Pause`(GO 105 / RT 3347) · `StepPlay`(GO 484 / RT 3002)。
//       `Play` 与 `Pause` **同 rect、同 ap(-6.9487762451171875)、靠 `activeSelf` 互斥**。
//       `Setup()` = `objHolder.SetActive(matchType == 0xA0)` ⇒ 开关在 **Holder 那一层**（`ReplayHud__Setup.c`）。
//    B. **四颗钮的原版语义**（`ReplayHud__{Replay,Play,Pause,Step}ButtonClicked.c`）：
//       `Replay → BattleManager.ClickRestartReplay` · `Play → ClickPlayReplay` ·
//       `Pause → ClickPauseReplay` · `StepPlay → ClickNextStepReplay`。
//       `PlayButtonClicked` / `PauseButtonClicked` **自己**做那一下图标互换（不是 `BattleManager` 做的）。
//    C. **点击音**：四颗 `EverguildButton` 逐颗 `m_onClickSound = null` + `useDefaultAudioClipIfNull = 1`
//       （`battlearena2` MB **5394 / 4306 / 4916 / 5483**）⇒ 原版点它们**都响**
//       `SoundManager.defaultButtonSound` = `WindowSounds.DefaultButtonCue`（cue `ButtonClick`）。
//       🔴 这四颗**不在** `B117-G10-03` 那张「2 颗静音」名单里 ⇒ 必须带 cue。
//    D. **录像库的两条硬语义**（`ReplayStore` 文件头）：
//       ① `MaxKept = 50`，超出**删最旧的**，排序键 = **文件名自带的时间戳**（不靠文件系统时间）；
//       ② `ResetForTest` 在 `OverrideDir` 未设时**必须拒绝执行**（2026-10-24 真事故：清空了玩家真录像）。
//    E. **`replaysVersion` 哨兵**（`BattleLogItem__Initialize.c:206` + 字符串字面量 `"-1"`）：
//       `"-1"` = 没有回放（不可点）；`""` / `null` = 没有；**其它任何值**（含 `"-10"`）= 有。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 A：`TrimToLast(2)` 之后**剩的必须正好是最新那两个名字**（不是「数量对就行」）——
//     把排序翻成升序 / 把 `Reverse()` 删掉 ⇒ 「剩下的名字」这条红，而「数量」那条照样绿。
//  🧨 B：`ReplayBar` 源里 **`WindowSounds.DefaultButtonCue` 恰好在那一行 `Attach` 上**，
//     且全文件 **2 参 `Attach`（不带 cue）活代码 = 0** —— 谁把 cue 删掉，两条里必有一条红
//     （`BattleHudButton.Attach` 的两参重载本身是**静音**的 ⇒ 默认值救不了它）。
//  🧨 C：四颗钮的**贴图名互不相同**且四张都在（`restart / play / pause / next`）——
//     「四颗都建出来了」可以靠同一个名字满足，这条不行。
//  🧨 D：`ReplayAvailable("-1") == false` **且** `ReplayAvailable("-10") == true` ——
//     把判据写成前缀比较（`StartsWith("-1")`）⇒ 后一条红；写成「非空即有」⇒ 前一条红。
//
// ============================ 怎么接 ============================
//  · `Run()`          —— **零前提**：源级 + `ReplayStore` 行为（只在系统临时目录里写，收尾删干净）。
//  · `Run(driver)`    —— 追加「真机上那四颗钮带着 cue」那一组（需要宿主已经把 HUD 建出来）。
//  挂法（由调度台决定，⛔ 本代理不改宿主）：
//      B117_G10_16_Assert.Run(driver);  // 或 Run()
//  判绿红：`grep "^B117G1016"` 的**合计行**；逐条按**行首** `B117G1016 ✓` / `B117G1016 ✗` 数
//  （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CardPresentation;
using RuleEngine;

/// <summary>执行代理 `B117_G10_16`：回放条 6 节点 / 四颗钮语义 / 点击音 / 录像库 50 局淘汰 / 哨兵。</summary>
public static class B117_G10_16_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string P = "B117G1016";
    const string ReplayBarPath = "CardPresentation/Battle/ReplayBar.cs";

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

    public static void Run() { Run(null); }

    public static void Run(BattleDriver driver)
    {
        _pass = 0; _fail = 0;
        Group("A·回放条 6 节点与四颗钮", A_NodeStructure);
        Group("B·录像库 50 局淘汰 / 排序", B_TrimOldest);
        Group("C·自检不许删玩家真录像", C_ResetGuard);
        Group("D·replaysVersion 哨兵", D_ReplayAvailable);
        Group("E·理由码头尾两条记录", E_ResultLine);
        Group("F·真机路：四颗钮都带默认点击音", () => F_LiveCue(driver));
        Debug.Log(P + " 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ================================================================ A 回放条
    //  判据（`battlearena2` 现读）：ReplayButtons(GO 125) → Holder(GO 1084) → Replay(164) / Play(769) /
    //  Pause(105) / StepPlay(484)。四颗 `m_SizeDelta` 全是 (79.8, 48.57)、`m_PreserveAspect = 0`。
    //  ⚠️ 这一段是**源级**（不建对象）：本代理不许改宿主，也就没有「HUD 已经建好」这个前提。
    static void A_NodeStructure()
    {
        string path = Path.Combine(Application.dataPath, ReplayBarPath);
        // 读不到源时**当场红**：否则下面每条都成了空判（本仓既有写法，见 `Editor/BattleScene.cs` §T09/A4）。
        Check(File.Exists(path), "A0（前提）读得到 `" + ReplayBarPath + "`（读不到时本条之后全是空判）");
        if (!File.Exists(path)) return;

        var live = new List<string>();
        foreach (var l0 in File.ReadAllLines(path))
        {
            string l = l0.TrimStart();
            if (l.StartsWith("//")) continue;          // 活代码行才数（留痕注释里提到名字是故意的）
            live.Add(l);
        }
        string src = string.Join("\n", live.ToArray());

        // A1 四个 BindClick（= 四颗钮各挂一颗 `BattleHudButton`）
        int binds = CountOf(src, "BindClick(Btn.");
        Check(binds == 4, "A1 四颗钮各挂一次 uGUI 点击（`BindClick(Btn.` 活代码 " + binds + " 处，必须 = 4）");

        // A2 Holder 那一层是显隐闸（原版 `objHolder.SetActive(matchType == 0xA0)`）
        Check(src.Contains("_holder.gameObject.SetActive(isReplayMatch)"),
              "A2 显隐闸做在 **Holder** 那一层（`ReplayHud__Setup.c`：`objHolder.SetActive(matchType == 0xA0)`）");

        // A3 四张**互不相同**的贴图（🧨 灭自证：四颗都用同一张图也能让「都建出来了」变绿）
        string[] arts = { "40K_replay_bt_restart", "40K_replay_bt_play", "40K_replay_bt_pause", "40K_replay_bt_next" };
        int present = 0;
        var seen = new HashSet<string>();
        for (int i = 0; i < arts.Length; i++)
        {
            if (src.Contains("\"" + arts[i] + "\"")) { present++; seen.Add(arts[i]); }
        }
        Check(present == 4 && seen.Count == 4,
              "A3 🧨 四颗用的是**四张不同**的原版图（restart / play / pause / next；实得 " + present + " 张 / "
            + seen.Count + " 个不同）");

        // A4 Play / Pause 同座标（原版两个 GameObject、同 rect、靠 activeSelf 互斥）
        //   ⚠️ 按**整行**判（那一行 `Make("Play",` 与 `SlotApX[1]` 之间有对齐空格，⛔ 别写死空格数）。
        string playLine = live.Find(x => x.Contains("Make(\"Play\","));
        string pauseLine = live.Find(x => x.Contains("Make(\"Pause\","));
        Check(playLine != null && pauseLine != null
              && playLine.Contains("SlotApX[1]") && pauseLine.Contains("SlotApX[1]"),
              "A4 `Play` / `Pause` 取**同一个** `SlotApX[1]`（原版两个 GO 同 rect，靠 `activeSelf` 互斥）");

        // A5 点击音（判据见文件头 C）
        string[] attachLines = live.FindAll(x => x.Contains("BattleHudButton.Attach(")).ToArray();
        Check(attachLines.Length == 1,
              "A5（前提）`ReplayBar.cs` 里 `BattleHudButton.Attach(` 活代码恰 1 处（实得 "
            + attachLines.Length + " 处）—— 四颗共用 `BindClick` 那一处");
        int withCue = CountOf(attachLines.Length == 1 ? attachLines[0] : "", "WindowSounds.DefaultButtonCue");
        int twoArg = 0;
        for (int i = 0; i < attachLines.Length; i++)
            if (!attachLines[i].Contains("WindowSounds.DefaultButtonCue")) twoArg++;
        Check(attachLines.Length == 1 && withCue == 1 && twoArg == 0,
              "A6 🧨 那一处 `Attach` **带着** `WindowSounds.DefaultButtonCue`，且**不带 cue 的 2 参写法 = 0 处**"
            + "（原版四颗 `useDefaultAudioClipIfNull = 1` ⇒ 都响 `defaultButtonSound`；"
            + "`BattleHudButton` 的两参重载是**静音**的 ⇒ 默认值救不了它）");
    }

    static int CountOf(string s, string needle)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    // ================================================================ B 50 局淘汰
    //  判据：`ReplayStore.MaxKept = 50`，超出**删最旧的**，排序键 = 文件名自带的时间戳
    //  （`ReplayStore.cs` 文件头「留多少局」那一节 + `List()` / `TrimToLast()`）。
    static void B_TrimOldest()
    {
        Check(ReplayStore.MaxKept == 50, "B1 `ReplayStore.MaxKept` = 50（实得 " + ReplayStore.MaxKept + "）");

        string dir = Path.Combine(Path.GetTempPath(), "wf_g1016_replays");
        string prevOverride = ReplayStore.OverrideDir;
        var prevClock = ReplayStore.ClockForTest;
        bool prevVerbose = ReplayStore.VerboseTrace;
        try
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            ReplayStore.OverrideDir = dir;
            ReplayStore.VerboseTrace = false;

            var t0 = new DateTime(2026, 1, 1, 12, 0, 0);
            var names = new string[4];
            for (int i = 0; i < 4; i++)
            {
                var t = t0.AddSeconds(i + 1);              // 12:00:01 .. 12:00:04
                ReplayStore.ClockForTest = () => t;
                var rec = new ReplayRecord
                {
                    myHero = "H" + (i + 1),
                    foeHero = "F" + (i + 1),
                    actions = new List<CardPresentation.Net.MsgAction>(),
                };
                names[i] = ReplayStore.Save(rec);
            }
            string newest = names[3], second = names[2], oldest = names[0], secondOldest = names[1];
            Check(names[0] != null && names[3] != null && names[0] != names[3],
                  "B2（前提）四次存盘都成功且文件名互不相同（" + names[0] + " … " + names[3] + "）");

            var listed = ReplayStore.List();
            Check(listed.Count == 4 && listed[0] == newest && listed[3] == oldest,
                  "B3 `List()` **新 → 旧**（第 0 个 = " + (listed.Count > 0 ? listed[0] : "<空>")
                + " · 最后一个 = " + (listed.Count > 0 ? listed[listed.Count - 1] : "<空>") + "）");

            int removed = ReplayStore.TrimToLast(2);
            var kept = ReplayStore.List();
            Check(removed == 2, "B4 超出上限 2 局 ⇒ 删掉 2 份（实得 " + removed + "）");
            // 🧨 灭自证：剩下的必须**正好是最新那两个名字** —— 排序翻成升序 / 删掉 `Reverse()`
            //    ⇒ 「数量对」那几条照样绿，只有这一条能红。
            Check(kept.Count == 2 && kept[0] == newest && kept[1] == second,
                  "B5 🧨 剩下的**正好是最新那两份**（" + (kept.Count > 0 ? kept[0] : "<空>") + " / "
                + (kept.Count > 1 ? kept[1] : "<空>") + "）");
            Check(!File.Exists(Path.Combine(dir, oldest)) && !File.Exists(Path.Combine(dir, secondOldest)),
                  "B6 🧨 被删的**正好是最旧那两份**（" + oldest + " / " + secondOldest + "）");

            // B7 读回来还是同一份（写 → 读往返）
            var back = ReplayStore.Load(newest);
            Check(back != null && back.myHero == "H4" && back.version == new ReplayRecord().version,
                  "B7 `Load` 往返（督军名 " + (back == null ? "<null>" : back.myHero)
                + " · version " + (back == null ? "?" : back.version.ToString()) + "）");
        }
        finally
        {
            ReplayStore.OverrideDir = prevOverride;
            ReplayStore.ClockForTest = prevClock;
            ReplayStore.VerboseTrace = prevVerbose;
            try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { /* 清不掉不静默：见下 */ }
        }
    }

    // ================================================================ C 自检不许删玩家真录像
    //  判据：`ReplayStore.ResetForTest` 的**硬挡**（2026-10-24 真事故：`ProfileDataAssertE13` 的 `finally`
    //  先把 `OverrideDir` 置回 null 再调它 ⇒ 玩家的 `WarpforgeReplays/` 被整目录清空）。
    static void C_ResetGuard()
    {
        string root = Path.Combine(Path.GetTempPath(), "wf_g1016_playerroot");
        string prevRoot = ReplayStore.PlayerRootForTest;
        string prevOverride = ReplayStore.OverrideDir;
        try
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
            string replayDir = Path.Combine(root, ReplayStore.DirName);
            Directory.CreateDirectory(replayDir);
            string marker = Path.Combine(replayDir, "20260101_000000_Keep_vs_Me.json");
            File.WriteAllText(marker, "{\"version\":4}");

            ReplayStore.PlayerRootForTest = root;
            ReplayStore.OverrideDir = null;              // 故意不设 ⇒ 走那条硬挡
            ReplayStore.ResetForTest();

            Check(File.Exists(marker),
                  "C1 🧨 `OverrideDir` 没设时 `ResetForTest` **拒绝执行** —— 标记文件还在（"
                + "删掉这道硬挡 ⇒ 本条红，而那正是 2026-10-24 清空玩家真录像的那次事故）");
            Check(ReplayStore.Dir == replayDir,
                  "C2（前提）那一次确实算到了玩家目录 `" + replayDir + "`（实得 " + ReplayStore.Dir + "）"
                + " —— 否则 C1 是空判");
        }
        finally
        {
            ReplayStore.PlayerRootForTest = prevRoot;
            ReplayStore.OverrideDir = prevOverride;
            try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
        }
    }

    // ================================================================ D replaysVersion 哨兵
    //  判据：`BattleLogItem__Initialize.c:206` `interactable = (match.replaysVersion != "-1")`
    //  + 字面量 `DAT_184288ae8` → `"-1"`。原版只跟 `"-1"` 比 ⇒ `""` / `null` **都算有**，
    //  但**我们这一侧没有服务端**，映射见 `ReplayStore.NoReplaySentinel` 的 doc：空 / null = 没有。
    static void D_ReplayAvailable()
    {
        Check(ReplayStore.NoReplaySentinel == "-1",
              "D1 哨兵字面量 = `\"-1\"`（实得 `" + ReplayStore.NoReplaySentinel + "`）");
        Check(!ReplayStore.ReplayAvailable(ReplayStore.NoReplaySentinel),
              "D2 哨兵 `\"-1\"` ⇒ **不可点**");
        Check(!ReplayStore.ReplayAvailable("") && !ReplayStore.ReplayAvailable(null),
              "D3 空串 / null ⇒ 不可点（我们这一侧的映射：没有本地录像文件名）");
        Check(ReplayStore.ReplayAvailable("20260101_120000_A_vs_B.json"),
              "D4 一个真实的本地录像文件名 ⇒ 可点");
        // 🧨 灭自证：`"-10"` 也以 `-1` 开头 —— 判据必须是**整串相等**，不是前缀比较。
        Check(ReplayStore.ReplayAvailable("-10"),
              "D5 🧨 `\"-10\"` ⇒ **可点**（原版只跟整串 `\"-1\"` 比；改成 `StartsWith` ⇒ 本条红）");
        // 🧨 反向：把判据写成「非空即有」⇒ D2 红。两条同时成立才说明是「整串比 + 空判」。
        Check(!ReplayStore.ReplayAvailable("-1") && ReplayStore.ReplayAvailable("-10"),
              "D6 🧨 一不可点一可点**同时成立**（`\"-1\"` / `\"-10\"`）");
    }

    // ================================================================ E 理由码头尾两条记录
    //  判据：形状的唯一出处 = `RuleCore` 那两句 `ctx.Log($"[BattleResult] {reason}({(int)reason}) seat={player}")`；
    //  录像侧孪生 = `ReplayStore.ResultLine`（`resultHead` / `resultTail` 两格**只落盘**）。
    static void E_ResultLine()
    {
        string ln = ReplayStore.ResultLine(BattleResult.Forfeit, 1);
        Check(ln == "[BattleResult] Forfeit(2) seat=1",
              "E1 `ResultLine(Forfeit, 1)` = `[BattleResult] Forfeit(2) seat=1`（实得 `" + ln + "`）");

        BattleResult r; int seat;
        bool ok = ReplayStore.TryParseResultLine(ln, out r, out seat);
        Check(ok && r == BattleResult.Forfeit && seat == 1,
              "E2 往返解析（ok=" + ok + " · " + r + " · seat " + seat + "）");

        string noSeat = ReplayStore.ResultLine(BattleResult.Undefined, -1);
        Check(noSeat == "[BattleResult] Undefined(0)",
              "E3 `seat < 0` ⇒ **不写** `seat=`（实得 `" + noSeat + "`；⛔ 别拿 -1 顶替一个座位）");

        // 🧨 灭自证：「记过、码是 Undefined(0)」是一条**如实**的记录 —— 与「没记（空串）」是两档。
        var rec = new ReplayRecord { resultHead = noSeat };
        Check(ReplayStore.HasResultHead(rec) && ReplayStore.ReadResultCode(rec) == BattleResult.Undefined
              && !ReplayStore.HasResultHead(new ReplayRecord()),
              "E4 🧨 `Undefined(0)` 那一行 = **记过**（`HasResultHead` 真），空串 = **没记**（假）"
            + " —— 把两者混成一档 ⇒ 本条红");
    }

    // ================================================================ F 真机路（需要宿主）
    static void F_LiveCue(BattleDriver driver)
    {
        if (driver == null)
        {
            // ⚠️ **不是「通过」**：这一组没验。如实打一条（⛔ 不静默跳过后假装绿）。
            Debug.LogWarning(P + " ⚠️ F 组**没跑**：调用方没给 `BattleDriver`（`Run()` 无参入口）"
                                 + " ⇒ 「四颗钮真带着 cue」这一层**这次没验**（⛔ 别读成通过）");
            return;
        }
        var bar = driver.Replay;
        Check(bar != null, "F0（前提）这一局建出了回放条（`BattleDriver.Replay`）");
        if (bar == null) return;

        // 前提硬检查：四颗都建出来了（图取不到时 `Make` 返回 null）—— 不写成 `if (x != null) Check(...)`
        Check(bar.HasArt, "F1（前提）四颗的原版图都取到了（`HasArt`）");
        if (!bar.HasArt) return;

        ReplayBar.Btn[] all = { ReplayBar.Btn.Replay, ReplayBar.Btn.Play, ReplayBar.Btn.Pause, ReplayBar.Btn.Step };
        string[] label = { "Replay", "Play", "Pause", "StepPlay" };
        int good = 0;
        for (int i = 0; i < all.Length; i++)
        {
            var b = bar.ButtonForTest(all[i]);
            if (b != null && b.clickCue == WindowSounds.DefaultButtonCue) good++;
            else Debug.LogWarning(P + " · F2 明细：`" + label[i] + "` 的 cue = `"
                                 + (b == null ? "<没挂上>" : (b.clickCue ?? "<null>")) + "`");
        }
        // 🧨 四颗**全部**为默认按钮音才算过（原版逐颗 `useDefaultAudioClipIfNull = 1`）⇒
        //    「只给其中一颗接」或「一颗都不接」都会被这条挡下。
        Check(good == 4, "F2 🧨 四颗钮的 `clickCue` **都是** `" + WindowSounds.DefaultButtonCue
                       + "`（实得 " + good + "/4）");
    }
}
