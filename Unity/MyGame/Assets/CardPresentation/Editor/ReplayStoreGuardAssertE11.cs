// ReplayStoreGuardAssertE11.cs — 执行代理 `E11`（2026-10-24 热修）的断言宿主
//
// 本文件**只放断言**（⛔ 不改产品行为、不建场景）。
// 调用方式（主对话在宿主收尾处加两行，想并进总数就带后一行）：
//
//     ReplayStoreGuardAssertE11.Run();
//     pass += ReplayStoreGuardAssertE11.Pass; fail += ReplayStoreGuardAssertE11.Fail;
//
// ==================================================================
//  防的是哪一场事故（判据 → `资料/普查产出_1024c/RE1_战斗回放完整审查.md` §「根因 B」）
// ------------------------------------------------------------------
//  2026-10-24：`Editor/ProfileDataAssertE13.cs` 的 `finally` 先把 `ReplayStore.OverrideDir` 置回 null、
//  **再**调 `ReplayStore.ResetForTest()` ⇒ `ReplayStore.Dir` 落回
//  `persistentDataPath/WarpforgeReplays` ⇒ **玩家的真录像被整目录清空**，而且当时一行日志都不打。
//  两处都补了：① `finally` 调换顺序（B1）；② `ResetForTest` 加「没设 `OverrideDir` 就拒绝 + 出声」硬挡（B2）；
//  ③ 删每一份都打一行 `[Replay] 自检清空：…`（B3，不许静默）。
//
// 本文件的三条：
//   G1  未设 `OverrideDir` ⇒ `ResetForTest()` **一份都不删** + `LogError` 出声 + 一行删除日志都没有
//       （🧨 灭自证：把 `ReplayStore.ResetForTest` 开头那道硬挡删掉 ⇒ G1/G1-b/G1-c/G1-d 同时红）
//   G2  设了临时目录 ⇒ **只删**临时目录里那两份；别处的文件原封不动；而且每删一份都打一行日志
//   G3  （对照）设了 `OverrideDir`（哪怕是空目录）⇒ 不走「拒绝」那一路（硬挡不误伤正常用法）
//
// 🔴 **为什么这条断言自己不会误删玩家的真录像**：G1 必须**真的**在 `OverrideDir = null` 下调一次，
//    那时 `Dir` 会落到玩家的真目录 —— 若硬挡哪天被人删掉，**跑这条断言自己就会**重演 10-24 那次数据丢失。
//    ⇒ 断言先把「玩家目录的根」用 `ReplayStore.PlayerRootForTest` 改到临时目录（`Dir` 里那一跳，
//    只给自检用、平时恒 null），于是「删不到玩家真档」**由目录结构保证**，不靠断言写得小心。
//    ⚠️ 那一格在 `finally` 里必须还原成 null。
// ==================================================================
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using UnityEngine;

public static class ReplayStoreGuardAssertE11
{
    const string P = "[E11] ";

    /// <summary>本类这一趟的通过 / 失败数（主对话想并进宿主的合计数就加这两格）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    public static void Run()
    {
        Pass = 0; Fail = 0;

        var keepOverride = ReplayStore.OverrideDir;
        var keepRoot = ReplayStore.PlayerRootForTest;

        // 三个临时目录：假的「玩家目录根」· 一份当 `OverrideDir` 的 · 一份空目录当对照
        string fakeRoot = Path.Combine(Path.GetTempPath(), "wf_e11_root_" + System.Guid.NewGuid().ToString("N"));
        string tmpDir = Path.Combine(Path.GetTempPath(), "wf_e11_tmp_" + System.Guid.NewGuid().ToString("N"));
        string emptyDir = Path.Combine(Path.GetTempPath(), "wf_e11_empty_" + System.Guid.NewGuid().ToString("N"));

        var errs = new List<string>();      // Error / Exception 级
        var logs = new List<string>();      // Log 级（删文件那几行在这里）
        Application.LogCallback cb = (cond, stack, type) =>
        {
            if (string.IsNullOrEmpty(cond)) return;
            if (type == LogType.Log) logs.Add(cond);
            else errs.Add(cond);
        };
        Application.logMessageReceived += cb;

        try
        {
            Directory.CreateDirectory(tmpDir);
            Directory.CreateDirectory(emptyDir);
            string playerDir = Path.Combine(fakeRoot, ReplayStore.DirName);   // = 「玩家的」录像目录（其实是临时目录）
            Directory.CreateDirectory(playerDir);
            string s1 = Path.Combine(playerDir, "20261024_000000_哨兵甲_vs_乙.json");
            string s2 = Path.Combine(playerDir, "20261024_000001_哨兵丙_vs_丁.json");
            File.WriteAllText(s1, "{}");
            File.WriteAllText(s2, "{}");

            // ---- G0 前提：没设 `OverrideDir` 时 `Dir` 就指到那份「玩家目录」 ----
            ReplayStore.PlayerRootForTest = fakeRoot;
            ReplayStore.OverrideDir = null;
            Ok(ReplayStore.Dir == playerDir,
               "G0（前提）`OverrideDir` 空 ⇒ `Dir` 落到「玩家目录」（实得 " + ReplayStore.Dir + "）");

            // ---- G1 ★ 本命：未设 `OverrideDir` ⇒ 拒绝执行、出声、一份都不删 ----
            errs.Clear(); logs.Clear();
            ReplayStore.ResetForTest();
            Ok(File.Exists(s1) && File.Exists(s2),
               "G1 ★ 未设 `OverrideDir` 时调 `ResetForTest()` ⇒ 两份哨兵**都还在**"
             + "（🧨 灭自证：把 `ReplayStore.ResetForTest` 开头那道硬挡删掉 ⇒ 本条红，且被删的就是这两份）");
            Eq(Directory.GetFiles(playerDir, "*" + ReplayStore.Ext).Length, 2,
               "G1-b 同一判据的另一种量法：那个目录里仍是 2 个录像文件（一份没少）");
            Ok(errs.Exists(m => m.Contains("拒绝清空")),
               "G1-c ★ 而且**出声**了（`LogError` 里带「拒绝清空」+ 真路径）—— 不许静默失败");
            Ok(!logs.Exists(m => m.Contains("自检清空")),
               "G1-d（对照）**一行删除日志都没有** ⇒ 它真的是在删之前就 `return` 了（⛔ 不是「删完才报错」）");

            // ---- G2 ★ 设了临时目录 ⇒ 只删临时目录，别处原封不动 ----
            string t1 = Path.Combine(tmpDir, "20261024_000002_甲_vs_乙.json");
            string t2 = Path.Combine(tmpDir, "20261024_000003_丙_vs_丁.json");
            File.WriteAllText(t1, "{}");
            File.WriteAllText(t2, "{}");
            ReplayStore.OverrideDir = tmpDir;
            Eq(ReplayStore.Dir, tmpDir, "G2（前提）设了 `OverrideDir` ⇒ `Dir` 就是它");

            errs.Clear(); logs.Clear();
            ReplayStore.ResetForTest();
            Eq(Directory.GetFiles(tmpDir, "*" + ReplayStore.Ext).Length, 0,
               "G2 ★ 临时目录里那 2 份被清空（硬挡没把正常用法一起挡掉）");
            Ok(File.Exists(s1) && File.Exists(s2),
               "G2-b ★ …而「玩家目录」那两份**原封不动** —— 删除范围只有 `Dir` 一处，不多删");
            Eq(logs.FindAll(m => m.Contains("自检清空")).Count, 2,
               "G2-c ★ 每删一份都打一行 `[Replay] 自检清空：…`（删用户数据不许静默 —— B3）");
            Eq(errs.FindAll(m => m.Contains("拒绝清空")).Count, 0,
               "G2-d（对照）设了目录 ⇒ 不报「拒绝清空」（G1-c 不是「反正每次都会报」的假象）");

            // ---- G3 对照：空临时目录 ⇒ 正常返回、不报拒绝 ----
            ReplayStore.OverrideDir = emptyDir;
            errs.Clear(); logs.Clear();
            ReplayStore.ResetForTest();
            Eq(errs.FindAll(m => m.Contains("拒绝清空")).Count, 0,
               "G3（对照）空临时目录 ⇒ 不走「拒绝」那一路（硬挡的判据是 `OverrideDir` 空不空，不是目录里有没有东西）");
            Ok(File.Exists(s1) && File.Exists(s2), "G3-b（对照）哨兵仍在");
        }
        catch (System.Exception e)
        {
            Ok(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            Application.logMessageReceived -= cb;
            // 🔴 **顺序照 `Editor/NetSelfTest.cs:925-929`**：先把自己那份临时目录**指回去**、再清，**最后**才还原两个改写口。
            //    ⛔ 顺序反了 = 删掉玩家的真录像 —— 这正是 2026-10-24 那次事故（本文件要防的就是它）。
            if (!string.IsNullOrEmpty(tmpDir)) { ReplayStore.OverrideDir = tmpDir; ReplayStore.ResetForTest(); }
            ReplayStore.OverrideDir = keepOverride;
            ReplayStore.PlayerRootForTest = keepRoot;      // ⚠️ 必须还原：不还原的话后面对 `Save`/`List` 全指到临时目录
            try { if (Directory.Exists(fakeRoot)) Directory.Delete(fakeRoot, true); }
            catch (System.Exception e) { Debug.LogWarning(P + "临时根目录没删干净（不影响断言）：" + e.Message); }
            try { if (Directory.Exists(tmpDir)) Directory.Delete(tmpDir, true); }
            catch (System.Exception e) { Debug.LogWarning(P + "临时目录没删干净（不影响断言）：" + e.Message); }
            try { if (Directory.Exists(emptyDir)) Directory.Delete(emptyDir, true); }
            catch (System.Exception e) { Debug.LogWarning(P + "空目录没删干净（不影响断言）：" + e.Message); }
        }
        Debug.Log(P + $"=== E11 合计：{Pass} 通过 / {Fail} 失败 ===");
    }

    // ==================================================================
    //  断言小工具（与 `ProfileDataAssertE13` 同形：本类自己记一份 Pass/Fail）
    // ==================================================================
    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + $"   ✓ {msg}"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}"); return false;
    }

    static bool Eq<T>(T got, T want, string msg)
    {
        return Ok(EqualityComparer<T>.Default.Equals(got, want), msg + $"（实得 {got} / 期望 {want}）");
    }
}
