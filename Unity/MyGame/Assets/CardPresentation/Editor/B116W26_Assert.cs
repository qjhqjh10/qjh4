// B116W26_Assert.cs — 执行代理 `B116_W26`（2026-11-02 · 第一百一十六会话 · 存档线）
//
// ============================ 它盯的是什么 ============================
//  `UI-15` / `B-322`：切「卡牌等级 / 轮抽」这类通道到**中央优先**之前的**切前对账**（`ReconcileOnSwitch`）。
//  这两条通道的中央档**只可能是几周前的一次性 `ImportAll` 快照**（全仓 0 个写口，判据 = 施工面 §1 表 A 第 1/2 行）。
//  不对账就切读口 ⇒ 玩家一进界面读到几周前的进度。
//
//  ⚠️ 本批**不切**卡牌等级 / 轮抽本身（那是下一批），只做对账机制 + 挂载 + 断言。
//
// ============================ 判据（本代理 2026-11-02 现读）============================
//  · `SaveCenter.ReconcileOnSwitch(key, legacyPath, out detail)` —— 本节新增，追加在 `SaveCenter` **类尾**
//  · 标记 key = `_ours_Reconcile.<通道 key>`（`_ours_` 前缀由 `SaveKeys.IsKnown` 放行 ⇒ `SaveKeys.Count` 仍 51）
//  · 挂载点 = `Shell/ShellRuntime.cs:356`（`ImportAll()` **同一物理行**、`RegisterMirrors()` 之前）
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 A 段：旧档比中央新 ⇒ 必须**写**中央档；写口若没接（对账变空转）⇒ A1/A2 红。
//  🧨 B 段：中央比旧档新 ⇒ 必须**不写**；若「永远旧档赢」⇒ B2 红。
//  🧨 C 段：同一份再跑一次 ⇒ **连 mtime 都不能变**；若每次启动都rewrite ⇒ C3 红。
//  🧨 D 段：中央档「自上次对账后被别人写过」（= 通道已切换）⇒ 旧档再新也**不许覆盖**；
//           若没有「只做一次」的判据（每次都旧档赢）⇒ D2 红。**这一条就是 B-322 的雷**。
//  🧨 E 段：落盘门关着（不注入 `SaveCenter.OverrideRoot`）⇒ 一个字节都不许动。
//
// ============================ 怎么接 ============================
//  `Editor/ShellScene.cs` 的聚合行（`:11093`）在 `Debug.Log(… 合计 …)` **之前**、**同一物理行**追加：
//      B116W26_Assert.Run(); _sink.Pass += B116W26_Assert.Pass; _sink.Fail += B116W26_Assert.Fail;
//  ⚠️ 必须落在该行**第一个 `//` 之前**（否则代码被注释吞掉 —— 本项目踩过 6 次）。
//
// ⚠️ 本文件**不碰玩家真档**：全程一个临时目录（`%TEMP%/wf_b116w26`）同时当 `SaveCenter.OverrideRoot`
//    和旧档根；`finally` 里把 `OverrideRoot` / 双写登记**逐样还原**并删目录。
using System;
using System.IO;
using System.Text;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B116_W26`：`UI-15` / `B-322` —— 存档通道的**切前对账**（`ReconcileOnSwitch`）。</summary>
public static class B116W26_Assert
{
    const string Prefix = "[B116W26] ";
    static readonly CheckSink _sink = new CheckSink(Prefix) { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    static void TryDelete(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch (Exception) { /* 临时目录删不掉不影响断言 */ }
    }

    /// <summary>写一份旧档并**钉死它的 mtime**（判据就是 mtime，⛔ 不能靠两次 `File.WriteAllText` 的先后——分辨率不够稳）。</summary>
    static void WriteLegacy(string path, string text, DateTime mtimeUtc)
    {
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, text, new UTF8Encoding(false));
        File.SetLastWriteTimeUtc(path, mtimeUtc);
    }

    static void DropMark(string key)
    {
        string m = SaveCenter.ReconcileMarkKeyOf(key);
        if (SaveCenter.Exists(m)) SaveCenter.Delete(m);
    }

    public static void Run()
    {
        MenuCheck.Section(_sink, "W26 存档切前对账（ReconcileOnSwitch · UI-15 / B-322）");

        string prevRoot = SaveCenter.OverrideRoot;
        string prevMirrorA = SaveCenter.LegacyMirrorPathOf(SaveKeys.ActiveEvent);
        string prevMirrorB = SaveCenter.LegacyMirrorPathOf(SaveKeys.OursCardProgress);

        string root = Path.Combine(Path.GetTempPath(), "wf_b116w26");
        string saveRoot = Path.Combine(root, "save");             // `SaveCenter` 的中央档根
        string key = SaveKeys.ActiveEvent;                        // 用真 key（中央档写在临时根里 ⇒ 不碰玩家真档）
        string markKey = SaveCenter.ReconcileMarkKeyOf(key);
        try
        {
            TryDelete(root);

            // ---------------------------------------------------------------- 前提
            C(SaveKeys.ValidateTable(), "", "W26-0 前提：`SaveKeys.ValidateTable()` 通过");
            C(SaveKeys.Count, 51, "W26-0' `_ours_Reconcile.*` 是**运行期拼的 key**、⛔ 没进 `All` ⇒ 数据 key 总数仍是 51");
            Tr(SaveKeys.IsKnown(markKey), "W26-0'' 标记 key 被 `IsKnown` 的 `_ours_` 前缀那一支放行");
            C(markKey, "_ours_Reconcile.ActiveEvent", "W26-0''' 标记 key 形状 = `_ours_Reconcile.<通道 key>`");

            // 双写登记必须先撤掉：否则 `SetRaw` 会顺带写**登记表里那份旧档**（可能是玩家真档）
            SaveCenter.UnregisterLegacyMirror(key);
            SaveCenter.UnregisterLegacyMirror(SaveKeys.OursCardProgress);
            SaveCenter.OverrideRoot = saveRoot;
            Tr(SaveCenter.StoreOn, "W26-0'''' 前提：`SaveCenter.StoreOn` 为真（`OverrideRoot` 已注入临时目录）");

            string detail;

            // ---------------------------------------------------------------- A：旧档比中央新 ⇒ 旧档赢
            DropMark(key);
            Tr(SaveCenter.SetRaw(key, "{\"version\":1,\"run\":\"stale\"}"), "W26-A 前提：中央档先放一份**过期快照**");
            File.SetLastWriteTimeUtc(SaveCenter.PathOf(key), DateTime.UtcNow.AddDays(-21));   // 「几周前导入的」
            string legacyA = Path.Combine(root, "legacy_a", "run.json");
            WriteLegacy(legacyA, "{\"version\":1,\"run\":\"fresh\"}", DateTime.UtcNow);

            bool wroteA = SaveCenter.ReconcileOnSwitch(key, legacyA, out detail);
            Tr(wroteA, "W26-A1 ★★ 旧档比中央档新 ⇒ 对账**真的写了**中央档（🧨 对账没接 ⇒ 红）");
            C(SaveCenter.GetRaw(key), "{\"version\":1,\"run\":\"fresh\"}",
              "W26-A2 ★★ 对账后**中央档 == 旧档**（逐字）");
            C(SaveCenter.DiffPayload(SaveCenter.GetRaw(key), File.ReadAllText(legacyA)), "",
              "W26-A3 ★ 逐字段相等（= Step 4 的验收口径）");
            Tr(SaveCenter.Exists(markKey), "W26-A4 标记 `_ours_Reconcile.<key>` 落下了（下次据此判「还是不是旧档权威」）");
            Tr(detail != null && detail.Contains("旧档赢"), "W26-A5 日志说清了「旧档赢」（⛔ 不静默）");

            // ---------------------------------------------------------------- C：幂等（再跑一次，一个字节都不动）
            string beforeC = SaveCenter.GetRaw(key);
            long mtimeBeforeC = File.GetLastWriteTimeUtc(SaveCenter.PathOf(key)).Ticks;
            bool wroteC = SaveCenter.ReconcileOnSwitch(key, legacyA, out detail);
            Tr(!wroteC, "W26-C1 ★★ 同一份旧档再跑一次 ⇒ **不写**（幂等）");
            C(SaveCenter.GetRaw(key), beforeC, "W26-C2 中央档内容不变");
            C(File.GetLastWriteTimeUtc(SaveCenter.PathOf(key)).Ticks, mtimeBeforeC,
              "W26-C3 ★★ 中央档**连 mtime 都没变** ⇒ 真的没写盘（🧨 每次启动都rewrite ⇒ 红）");

            // ---------------------------------------------------------------- B：中央比旧档新 ⇒ 不动（灭自证：不是「永远旧档赢」）
            DropMark(key);                                        // 清标记 ⇒ 单独验 mtime 那条规则（否则先被「指纹不符」拦下）
            Tr(SaveCenter.SetRaw(key, "{\"version\":1,\"run\":\"central_new\"}"), "W26-B 前提：中央档放一份**新**值（mtime = 现在）");
            string legacyB = Path.Combine(root, "legacy_b", "run.json");
            WriteLegacy(legacyB, "{\"version\":1,\"run\":\"legacy_old\"}", DateTime.UtcNow.AddDays(-3));
            bool wroteB = SaveCenter.ReconcileOnSwitch(key, legacyB, out detail);
            Tr(!wroteB, "W26-B1 ★★ 中央档比旧档新 ⇒ **不写**");
            C(SaveCenter.GetRaw(key), "{\"version\":1,\"run\":\"central_new\"}",
              "W26-B2 ★★ 中央档原封不动（🧨 若对账「永远旧档赢」⇒ 红）");
            Tr(detail != null && detail.Contains("不比中央档新"), "W26-B3 日志说清「旧档不比中央档新」");

            // ---------------------------------------------------------------- D：「只做一次」—— 通道切换后必须停手
            // 先回到 A 结束时的状态（对账过、标记在、两边相等），再模拟「通道已切到中央优先」：子系统真写一次中央档。
            DropMark(key);
            Tr(SaveCenter.SetRaw(key, "{\"version\":1,\"run\":\"stale2\"}"), "W26-D 前提：中央档回到过期值");
            File.SetLastWriteTimeUtc(SaveCenter.PathOf(key), DateTime.UtcNow.AddDays(-21));
            string legacyD = Path.Combine(root, "legacy_d", "run.json");
            WriteLegacy(legacyD, "{\"version\":1,\"run\":\"fresh2\"}", DateTime.UtcNow.AddDays(-20));
            Tr(SaveCenter.ReconcileOnSwitch(key, legacyD, out detail), "W26-D0 前提：先对账一次（此时标记 = 对账后的中央档指纹）");

            Tr(SaveCenter.SetRaw(key, "{\"version\":1,\"run\":\"switched\"}"),
               "W26-D0' 前提：模拟「通道已切到中央优先」—— 子系统通过 `SaveThroughLegacy` 真写一次中央档");
            WriteLegacy(legacyD, "{\"version\":1,\"run\":\"legacy_newer\"}", DateTime.UtcNow.AddHours(1));   // 旧档又变新（且内容不同）
            bool wroteD = SaveCenter.ReconcileOnSwitch(key, legacyD, out detail);
            Tr(!wroteD, "W26-D1 ★★ 中央档自对账后**已被写过** ⇒ 旧档再新也**不覆盖**（这就是「只做一次」）");
            C(SaveCenter.GetRaw(key), "{\"version\":1,\"run\":\"switched\"}",
              "W26-D2 ★★ 切换后的那次真写**保住了**（🧨 没有「只做一次」的判据 ⇒ 旧档赢 ⇒ 红 = B-322 的雷复现）");
            Tr(detail != null && detail.Contains("已被写过"), "W26-D3 日志说清停手的原因");

            // ---------------------------------------------------------------- E：门位 —— 落盘关着 ⇒ 一个字节都不许动
            string ePath = SaveCenter.PathOf(key);                // ⚠️ 必须先取（下面要把 OverrideRoot 清掉）
            string beforeE = File.Exists(ePath) ? File.ReadAllText(ePath) : null;
            Tr(!Application.isPlaying,
               "W26-E0 ★ 前提：本段只能在**非 Play**（批处理自检）下跑 —— Play 里 `StoreOn` 恒真，清 `OverrideRoot` 会去碰玩家真档");
            if (!Application.isPlaying)
            {
                SaveCenter.OverrideRoot = null;
                Tr(!SaveCenter.StoreOn, "W26-E1 前提：不注入 `SaveCenter.OverrideRoot` ⇒ 落盘门关着");
                bool wroteE = SaveCenter.ReconcileOnSwitch(key, legacyD, out detail);
                Tr(!wroteE, "W26-E2 ★ 落盘门关着 ⇒ 对账**不写**");
                C(File.Exists(ePath) ? File.ReadAllText(ePath) : null, beforeE,
                  "W26-E3 ★★ 那份中央档**一个字节都没动**（门关着 ⇒ 绝不写玩家真档）");
                SaveCenter.OverrideRoot = saveRoot;
            }

            // ---------------------------------------------------------------- F：缺档 / 坏旧档 / 中央档缺失
            DropMark(key);
            Tr(SaveCenter.SetRaw(key, "{\"version\":1,\"run\":\"f\"}"), "W26-F 前提：中央档放一份基准值");
            bool wroteF1 = SaveCenter.ReconcileOnSwitch(key, Path.Combine(root, "legacy_none", "nope.json"), out detail);
            Tr(!wroteF1 && detail != null && detail.Contains("旧档不存在"), "W26-F1 旧档不存在 ⇒ 不动（没有权威来源）");
            string legacyBad = Path.Combine(root, "legacy_bad", "run.json");
            WriteLegacy(legacyBad, "not json at all", DateTime.UtcNow.AddHours(1));
            bool wroteF2 = SaveCenter.ReconcileOnSwitch(key, legacyBad, out detail);
            Tr(!wroteF2 && detail != null && detail.Contains("不是合法 JSON"), "W26-F2 旧档不是合法 JSON 对象 ⇒ 不导（旧文件原地不动）");
            C(SaveCenter.GetRaw(key), "{\"version\":1,\"run\":\"f\"}", "W26-F3 ★ 中央档没被坏旧档污染");
            if (SaveCenter.Exists(key)) SaveCenter.Delete(key);
            bool wroteF4 = SaveCenter.ReconcileOnSwitch(key, legacyA, out detail);
            Tr(!wroteF4 && detail != null && detail.Contains("ImportAll"), "W26-F4 中央档不存在 ⇒ 交给 `ImportAll` 整份导入，本条不动");
        }
        finally
        {
            // ---- 逐样还原（⛔ 不给后面的断言段留脏状态）
            SaveCenter.OverrideRoot = prevRoot;
            SaveCenter.UnregisterLegacyMirror(SaveKeys.ActiveEvent);
            SaveCenter.UnregisterLegacyMirror(SaveKeys.OursCardProgress);
            if (prevMirrorA != null) SaveCenter.RegisterLegacyMirror(SaveKeys.ActiveEvent, prevMirrorA);
            if (prevMirrorB != null) SaveCenter.RegisterLegacyMirror(SaveKeys.OursCardProgress, prevMirrorB);
            TryDelete(root);
        }
    }
}
