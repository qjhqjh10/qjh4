// B116W21_Assert.cs — 执行代理 `B116_W21`（2026-11-02 · 第一百一十六会话 · 存档线）
//
// ============================ 它盯的是什么 ============================
//  `UI-15` / `B-322` 四个「没走中央存档」的通道里**第 2 个：活动通行证**（`Shell/ExpansionPassEvent.cs`）。
//  施工面 = `资料/完全复刻/判据/B116_R8_存档切换施工面.md` §1 第 3 行；模板 = `报告/B116_W16_收件箱切中央存档.md`。
//  切之前：`ExpansionPassSave.Load()` 裸 `File.ReadAllText` / `Save()` 三段「`.tmp` → 删旧 → `File.Move`」，
//          **完全没纳入中央体系**（无 key、无 `Entries` 条目）。切之后：key = `_ours_ExpansionPass`
//          （原版 `ExpansionPassSave : ScoringEventPersistence` 在服务端、无 `PlayerDataKey` ⇒ 铁律 18·0 允许的自设计门类），
//          读写都走 `SaveCenter.{Read,Save}ThroughLegacy`。
//
// ============================ 判据（本代理 2026-11-02 现读）============================
//  · `SaveCenter.cs:1093` `SaveThroughLegacy`（StoreOn 真 ⇒ 中央 + 双写旧档；StoreOn 假 ⇒ **只写旧档**）
//  · `SaveCenter.cs:1165` `ReadThroughLegacy`（**先中央、再旧档**；中央坏档/缺失 ⇒ 退回旧档原文）
//  · `ExpansionPassEvent.cs:226`（读）/ `:272`（写）—— 本批改的就是这两处（**同一物理行号，行数守恒**）
//
// ⚠️ 2026-11-04 更正（`B139_V1`）：`SaveCenter.LegacyMirrorOn`（`Shell/SaveCenter.cs:915`）自 `81ea4fbb` 起是
//    `!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WF_LEGACY_MIRROR"))`（用户 2026-11-03 裁定「关双写」）
//    ⇒ **默认 false**。本夹具原来**没显式打开**、靠旧默认值 `true`，所以 A2 / A2' / A2'' / A3 / C1 / C1' 六条全倒。
//    现在 `Run()` 开头**显式 `= true`**、`finally` 还原原值（先例 = `Editor/B102DECK_Assert.cs:132`）。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 写口：把 `Save()` 退回裸 `File.WriteAllText` ⇒ 中央档不存在 ⇒ A1/A1'/A1''/A3 四条**当场红**。
//  🧨 读口（退回裸读旧档）：B 段让**中央档 = 777 / 旧档 = 120** ⇒ 读到 120 ⇒ B1 红。
//     ⚠️ 只断「读得出来」抓不到这一点 —— 所以 B 段**故意让两份值不同**。
//  🧨 读口（改成只读中央）：C 段 `Delete` 掉中央档 ⇒ 只读中央会读到出厂态 0 ⇒ C1（该是 120）红。
//  🧨 门位：D 段只注入子系统自己的 `OverrideDir`（**不**注入 `SaveCenter.OverrideRoot`）⇒ 中央档必须
//     **一个字节都不动**（先放哨兵 999，写完 60 之后仍必须读到 999）；⛔ 这时若还去写中央，写的就是玩家真档。
//  🧨 双写开关「关着」⇒ `W21-A4''` 断旧档**一个字节都不动** —— 没有这条，整套只剩「开着能写」，分不出开/关。
//
// ============================ 怎么接 ============================
//  `Editor/ShellScene.cs` 的聚合行（`:11093`，`Debug.Log(… 合计 …)` 之前）**同一物理行**追加：
//      B116W21_Assert.Run(); _sink.Pass += B116W21_Assert.Pass; _sink.Fail += B116W21_Assert.Fail;
//  ⚠️ ShellScene 的账落点最深 `:11092`（聚合行 `:11093` 在它之后）⇒ 同物理行追加**零漂移**。
//
// ⚠️ 本文件**不碰玩家真档**：全程两个临时目录（`%TEMP%/wf_b116w21/{pass,save}`），
//    `finally` 里把 `OverrideDir` / `OverrideRoot` / 双写登记**逐样还原**并删目录。
using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B116_W21`：`UI-15` / `B-322` —— 活动通行证切中央存档（写口 + 读口同一次改）。</summary>
public static class B116W21_Assert
{
    const string Prefix = "[B116W21] ";
    static readonly CheckSink _sink = new CheckSink(Prefix) { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>一份**与正常写出来那份不同**的 payload（B / D 段用它证明「读的是哪一侧」）。</summary>
    static string ProbePayload(int totalScore, int spentScore)
    {
        return "{\"version\":1,\"totalScore\":" + totalScore + ",\"spentScore\":" + spentScore
             + ",\"claimKeys\":[],\"claimValues\":[]}";
    }

    static void TryDelete(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch (Exception) { /* 临时目录删不掉不影响断言 */ }
    }

    public static void Run()
    {
        MenuCheck.Section(_sink, "W21 活动通行证 → 中央存档（UI-15 / B-322 · 四通道里第二个切的）");

        string prevDir = ExpansionPassSave.OverrideDir;
        string prevRoot = SaveCenter.OverrideRoot;
        string prevMirror = SaveCenter.LegacyMirrorPathOf(SaveKeys.OursExpansionPass);
        bool prevMirrorOn = SaveCenter.LegacyMirrorOn;      // ⚠️ 2026-11-04（B139_V1）：默认已改成 false（见文件头）

        string root = Path.Combine(Path.GetTempPath(), "wf_b116w21");
        string passDir = Path.Combine(root, "pass");              // 子系统自己的旧档目录
        string saveRoot = Path.Combine(root, "save");             // `SaveCenter` 的中央档根
        string passDir2 = Path.Combine(root, "pass_only");        // D 段：只注入子系统门
        try
        {
            TryDelete(root);
            // ⚠️ 2026-11-04（B139_V1）：双写开关默认已是 false（用户 2026-11-03 裁定「关双写」）⇒ 本夹具要验
            //    「中央档 + 旧档同时写」，必须**显式**打开；`finally` 还原原值（⛔ 别留脏状态给同进程后续套件）。
            SaveCenter.LegacyMirrorOn = true;

            // ---------------------------------------------------------------- 前提：键表一个字没动
            C(SaveKeys.ValidateTable(), "", "W21-0 前提：`SaveKeys.ValidateTable()` 通过");
            C(SaveKeys.Count, 51, "W21-0' ★ `OursExpansionPass` **不进 `All`** ⇒ 数据 key 总数仍是 51（进了就红）");
            Tr(SaveKeys.IsKnown(SaveKeys.OursExpansionPass), "W21-0'' `_ours_ExpansionPass` 被 `IsKnown` 的 `_ours_` 前缀那一支放行");
            C(SaveKeys.OursExpansionPass, "_ours_ExpansionPass", "W21-0''' key 名 = `_ours_ExpansionPass`（原版在服务端、无 `PlayerDataKey`）");

            // ---------------------------------------------------------------- A：写口（中央 + 双写旧档）
            ExpansionPassSave.OverrideDir = passDir;
            SaveCenter.OverrideRoot = saveRoot;
            Tr(SaveCenter.StoreOn, "W21-A 前提：`SaveCenter.StoreOn` 为真（`OverrideRoot` 已注入临时目录）");
            Tr(!SaveCenter.Exists(SaveKeys.OursExpansionPass), "W21-A' 前提：写之前中央档**不存在**");

            var save = new ExpansionPassSave();
            save.Score(120);
            save.AddClaim(3);
            Tr(save.Save(), "W21-A'' 前提：`Save()` 返回 true（落盘门开着、目录可写）");

            Tr(SaveCenter.Exists(SaveKeys.OursExpansionPass), "W21-A1 ★★ 写后**中央档有值**（`WarpforgeSave/_ours_ExpansionPass.json`）—— 写口若退回裸 `File.WriteAllText` ⇒ 红");
            string central = SaveCenter.GetRaw(SaveKeys.OursExpansionPass);
            Tr(central != null && central.Contains("\"totalScore\":120"), "W21-A1' ★ 中央档 payload 里真是 `totalScore:120`");
            Tr(central != null && central.Contains("\"claimKeys\":[\"3\"]"), "W21-A1'' ★ 中央档里领取平行数组也带上了（`claimKeys:[\"3\"]`）");

            string legacyPath = Path.Combine(passDir, "pass.json");
            Tr(File.Exists(legacyPath), "W21-A2 ★ 旧档**照旧写**（双写期：回滚旧客户端还读得到）");
            string legacyText = File.Exists(legacyPath) ? File.ReadAllText(legacyPath) : null;
            Tr(legacyText != null && legacyText.TrimStart().StartsWith("{\"version\":1"),
               "W21-A2' 旧档形状没变（顶层 `version`，不是中央的 `_v` 信封）");
            Tr(legacyText != null && !legacyText.Contains("\"_v\""),
               "W21-A2'' ★ 旧档**不带 `_v`** ⇒ 旧客户端照旧解析得动");
            C(SaveCenter.DiffWithLegacy(SaveKeys.OursExpansionPass), "",
              "W21-A3 ★★ 中央档与旧档**逐字段相等**（施工面 §1 第 3 行 = Step 4 的验收判据）");

            // ---------------------------------------------------------------- A4：灭自证 —— 开关**关着** ⇒ 旧档不写
            // ⚠️ 2026-11-04（`B139_V1`）：`LegacyMirrorOn` 现在默认 false ⇒ 这一段必须**先显式 false** 才有意义；
            //    它同时是「上面 A2 / A3 绿是因为开关真的开着、不是因为别的东西顺手写了旧档」的反证。
            SaveCenter.LegacyMirrorOn = false;
            try
            {
                var off = new ExpansionPassSave();
                off.Score(45);
                Tr(off.Save(), "W21-A4 前提：开关**关着**时 `Save()` 仍返回 true（中央档照写，只挡双写）");
                string centralOff = SaveCenter.GetRaw(SaveKeys.OursExpansionPass);
                Tr(centralOff != null && centralOff.Contains("\"totalScore\":45"),
                   "W21-A4' 前提：中央档确实是新值（`totalScore:45`）—— 证明这一趟真的走了写口");
                string legacyOff = File.Exists(legacyPath) ? File.ReadAllText(legacyPath) : null;
                C(legacyOff, legacyText,
                  "W21-A4'' ★★ 开关**关着** ⇒ 旧档**一个字节都没动**（仍是 120 那份）—— 熔断这条，整套就只剩「开着能写」，分不出开/关");
            }
            finally { SaveCenter.LegacyMirrorOn = true; }

            // ---------------------------------------------------------------- B：读口「中央优先」
            // 🔴 先撤双写登记 —— 否则 `SetRaw` 会把这份探针 payload **也双写进旧档**，两边就一样了、B 段失去意义。
            SaveCenter.UnregisterLegacyMirror(SaveKeys.OursExpansionPass);
            Tr(SaveCenter.SetRaw(SaveKeys.OursExpansionPass, ProbePayload(777, 5)),
               "W21-B 前提：直接改写中央档（造一个与旧档**不同**的值：total 777 / spent 5，旧档是 120 / 0）");
            var fromCentral = ExpansionPassSave.Load();
            C(fromCentral.TotalScore, 777,
              "W21-B1 ★★ 读回的是**中央档**那份（旧档仍是 120）—— 读口若退回裸 `File.ReadAllText` ⇒ 读到 120 ⇒ 红");
            C(fromCentral.SpentScore, 5, "W21-B1' 中央档那份的 `spentScore` 也生效（旧档是 0）");

            // ---------------------------------------------------------------- C：灭自证 —— 只删中央档
            Tr(SaveCenter.Delete(SaveKeys.OursExpansionPass), "W21-C 前提：`SaveCenter.Delete` 真的把中央档挪走了（改名 `.del-*`，可回滚）");
            Tr(!SaveCenter.Exists(SaveKeys.OursExpansionPass), "W21-C' 前提：中央档现在不存在");
            var fromLegacy = ExpansionPassSave.Load();
            C(fromLegacy.TotalScore, 120,
              "W21-C1 ★★ 中央档没了 ⇒ 读到**旧档**那份（total 回 120）—— 读口若改成「只读中央」⇒ 出厂态 0");
            C(fromLegacy.RewardClaimCount(3), 1, "W21-C1' 旧档那份的领取平行数组照样还原（claim 3 = 1 次）");

            // ---------------------------------------------------------------- D：门位回归（自检常态）
            // 先在**中央档开着**的时候放一份哨兵值，再把门关上 —— 这样 D 段才能证明「门关着时中央档一个字节都没动」。
            SaveCenter.OverrideRoot = saveRoot;
            Tr(SaveCenter.SetRaw(SaveKeys.OursExpansionPass, ProbePayload(999, 0)),
               "W21-D 前提：中央档放一份哨兵值（total 999）");
            SaveCenter.OverrideRoot = null;
            ExpansionPassSave.OverrideDir = passDir2;
            Tr(!SaveCenter.StoreOn, "W21-D0' 前提：不注入 `SaveCenter.OverrideRoot` ⇒ 中央档这道门关着");
            // 🔴 硬闸：Play 里 `SaveCenter.StoreOn` 恒真 ⇒ 这一段会把临时旧档写成**玩家真档**。
            //    ⛔ 不许静默跳过 —— 前提不成立就红，而且**一次写盘都不做**。
            Tr(!Application.isPlaying, "W21-D0 ★ 前提：本段只能在**非 Play**（批处理自检）下跑 —— Play 里 `StoreOn` 恒真，会把临时档写成玩家真档");
            if (!Application.isPlaying)
            {
                var d = new ExpansionPassSave();
                d.Score(60);
                Tr(d.Save(), "W21-D1 ★ 只注入子系统 `OverrideDir` ⇒ 旧档照样写（切换前的老行为没被误杀）");
                string legacy2 = Path.Combine(passDir2, "pass.json");
                Tr(File.Exists(legacy2), "W21-D1' 旧档文件真的落在那儿（`pass.json`）");
                string text2 = File.Exists(legacy2) ? File.ReadAllText(legacy2) : null;
                Tr(text2 != null && text2.Contains("\"totalScore\":60") && !text2.Contains("\"_v\""),
                   "W21-D1'' 内容与切换前同形（`totalScore:60`、无 `_v`）");
                // 🔴 读的是**临时根上的那个文件**，⛔ 不走 `SaveCenter.GetRaw` —— `:133` 已把 `OverrideRoot` 清成 null，
                //    而 `RootDir`（`Shell/SaveCenter.cs:80-88`）是**调用时**才读 `OverrideRoot` ⇒ 此刻已指回玩家真档目录
                //    ⇒ `GetRaw` 恒读不到（恒红 + 每次打一条**假的**「缺 key」警告）。B116_D2 诊断（α）断言本身错。
                //    ⛔ 产品侧不动：`GetRaw` 不受 `StoreOn` 门约束是**设计**（读不设门）。
                string centralPath = Path.Combine(saveRoot, SaveKeys.OursExpansionPass + ".json");
                string sentinel = File.Exists(centralPath) ? File.ReadAllText(centralPath) : null;
                Tr(sentinel != null && sentinel.Contains("\"totalScore\":999"),
                   "W21-D2 ★★ 中央档**一个字节都没动**（仍是哨兵 999，没被这次写盘改成 60）—— 门关着 ⇒ 绝不写玩家真档");
                var back = ExpansionPassSave.Load();
                C(back.TotalScore, 60, "W21-D3 再读回：`SaveCenter.StoreOn` 关着时只走旧档原文（⛔ 不看中央档）");
            }
        }
        finally
        {
            // ---- 逐样还原（⛔ 不给后面的断言段留脏状态）
            ExpansionPassSave.OverrideDir = prevDir;
            SaveCenter.OverrideRoot = prevRoot;
            SaveCenter.LegacyMirrorOn = prevMirrorOn;      // ⚠️ 2026-11-04（B139_V1）：还原双写开关（⛔ 别留给同进程后续套件）
            SaveCenter.UnregisterLegacyMirror(SaveKeys.OursExpansionPass);
            if (prevMirror != null) SaveCenter.RegisterLegacyMirror(SaveKeys.OursExpansionPass, prevMirror);
            TryDelete(root);
        }
    }
}
