// B118W22_Assert.cs — 执行代理 `B118_W22`（2026-11-02）· 账 `D118-75`：**真机 trace 落盘口**（运行时代码）
//
// ============================ 它盯的是什么 ============================
//  判据 → `判据/B108_ID01_S3_表现维.md` §5.2 的 `S3-G1`（原话「把 `(cardId/evt, name, world, parentLayer, clock)`
//  追加到 `_tmp_view/s3_vfx_trace.tsv`」）。
//  缺口：`S3VfxTrace`（`Editor/BattleScene.cs:51091`）在 **Editor 程序集**、挂在 `CardEffects.Play` 上；
//  而**真机 player** 装的是 `CardEffects.PlayInstance`（`WarpforgeVFX/Runtime/WarpforgeEffectHook.cs` 的 `Spawn`）
//  ⇒ `FireOne` 一落地就绕开 `Play`、**trace 一条都记不到**（2026-11-02 J11 报告 §5 第 2 条）。
//  补口 = `WarpforgeEffectHook.S3Trace`（运行时程序集，player 里也有）。本件验它**三件事**：
//    ① 列序与 Editor 那份**逐字一致**（下游 join 靠它）；
//    ② **关着 = 零字节**（不建目录 / 不建文件 / 不写一行）；
//    ③ 开着 = 表头 + 每行 **8 列**，值逐格对得上（⛔ 不读内存计数器当证据 —— **从磁盘读回来**比对）。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 A：把 `S3Trace.Columns` 的任意两列换序 / 少一格 ⇒ ① 那两条红（它与 `S3VfxTrace.Columns` **逐元**比）。
//  🧨 B：把 `Capture` 的 `if (!Enabled) return;` 删掉 ⇒ 「关着 = 零字节」三条里至少一条红。
//  🧨 C：把某一行少写一格 tab ⇒ 「每行列数 == 表头列数（8）」那条红（**8 是字面量**，不从 `Columns` 反推）。
//  🧨 D：把「文件已存在就不写表头」的逻辑去掉 ⇒ 「表头只出现一次」红（第二轮会再写一行列名）。
//  🧨 E：把 `Clean()` 去掉 ⇒ 「字段里带 tab 也只占一格」红。
//
// ============================ 覆盖边界（如实记）============================
//  · 本件验的是**记录机制**（开关 / 形状 / 落盘），**不验「那件特效真渲染出来了」** ——
//    探针件名是故意乱写的（`W22Probe_*`），`Capture` 根本不看件名存不存在。
//  · 「`cardId`/`evt` 两列在真机里填得上」**本件验不了**：那要一台活着的 `BattleDriver`（`CurrentVfxEvent`）。
//    无 driver 时 `Capture(fx,world,layer)` 那条口会留空 + 出声一次 —— 本件只断言「不抛」。
//  · 它写的是**临时文件**（`_tmp_view/s3_vfx_trace_w22.tsv`），⛔ 不碰真机那一份 `s3_vfx_trace.tsv`。
//
// ============================ 接线（🔴 本件由执行代理 `B118_W22` 新建，**尚未接线**）============================
//  执行代理的白名单里 **`Editor/BattleScene.cs` 是禁改**的 ⇒ 本文件当前**没有任何调用点**。
//  调度台接线 = 在 `Editor/BattleScene.cs:45353` 那条聚合行里、`B116W42_Assert.Run(driver);` **之后**
//  （同一物理行追加，⛔ 别新增行：那一行有账的落点）加：
//      `B118W22_Assert.Run(); pass += B118W22_Assert.Pass; fail += B118W22_Assert.Fail;`
//  ⚠️ 本件**不碰 driver**（只用 `Application.dataPath` 与临时文件）⇒ 接在哪里都不会污染对局现场。
using System.IO;
using UnityEngine;

public static class B118W22_Assert
{
    const string P = "[B118W22] ";
    static int _ok, _bad;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _ok; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(P + "✓ " + what); }
        else { _bad++; Debug.LogError(P + "✗ " + what); }
    }

    /// <summary>探针落盘路径（`_tmp_view` 下、**与真机那一份不同名**）。</summary>
    static string ProbePath()
    {
        string dir = S3VfxTrace.DirPath;
        return string.IsNullOrEmpty(dir) ? null : Path.Combine(dir, "s3_vfx_trace_w22.tsv");
    }

    public static void Run()
    {
        var probe = ProbePath();
        Check(!string.IsNullOrEmpty(probe),
              "（前提）算得出 `_tmp_view` 路径（`Application.dataPath` 为空时本件全部跳过）");
        if (string.IsNullOrEmpty(probe)) return;
        try { if (File.Exists(probe)) File.Delete(probe); } catch { }

        // ---- A：列序与 Editor 那份逐字一致（下游 join 的唯一依据）----
        var mine = WarpforgeVFX.WarpforgeEffectHook.S3Trace.Columns;
        var ed = S3VfxTrace.Columns;
        Check(mine != null && ed != null && mine.Length == ed.Length,
              $"★ A-1 列数相同：运行时 `S3Trace.Columns`（{(mine == null ? -1 : mine.Length)}）"
            + $" == `S3VfxTrace.Columns`（{(ed == null ? -1 : ed.Length)}）—— 🧨 少/多一格 ⇒ 本条红");
        string orderDiff = null;
        if (mine != null && ed != null && mine.Length == ed.Length)
            for (int i = 0; i < ed.Length; i++)
                if (mine[i] != ed[i]) { orderDiff = $"第 {i} 列不同：`{mine[i]}` vs `{ed[i]}`"; break; }
        Check(orderDiff == null,
              $"★ A-2 八列**逐字同序**（cardId / evt / fx / t / x / y / z / parentLayer）"
            + (orderDiff == null ? "" : $"｜{orderDiff}") + " —— 🧨 换序 ⇒ 本条红");
        Check(mine != null && mine.Length == 8,
              $"★ A-3 列数 = 8（字面量，不从 `Columns` 反推；现读 {(mine == null ? -1 : mine.Length)}）");

        // ---- B：关着 = 零字节（不建文件）----
        WarpforgeVFX.WarpforgeEffectHook.S3Trace.SetEnabledForTest(false, probe);
        WarpforgeVFX.WarpforgeEffectHook.S3Trace.Capture("W22Probe_off", new Vector3(1, 2, 3), 7,
                                                        "AM10", "play", 1.25f);
        WarpforgeVFX.WarpforgeEffectHook.S3Trace.Capture("W22Probe_off2", Vector3.zero, -1);
        Check(!File.Exists(probe), "★ B-1 关着时**文件根本没建**（`File.Exists == false`）—— 🧨 删掉 `if (!Enabled) return;` ⇒ 本条红");
        Check(WarpforgeVFX.WarpforgeEffectHook.S3Trace.FilePath == null,
              "★ B-2 关着时 `FilePath == null`（一个字节都没打算写）");
        Check(WarpforgeVFX.WarpforgeEffectHook.S3Trace.Rows == 0,
              $"★ B-3 关着时 `Rows == 0`（现读 {WarpforgeVFX.WarpforgeEffectHook.S3Trace.Rows}）");

        // ---- C：开着 = 表头 + 一行 8 列，值逐格对得上（从磁盘读回来）----
        WarpforgeVFX.WarpforgeEffectHook.S3Trace.SetEnabledForTest(true, probe);
        WarpforgeVFX.WarpforgeEffectHook.S3Trace.Capture("W22Probe_on", new Vector3(1.5f, -2.25f, 3f), 5,
                                                        "AM10", "play", 1.25f);
        var lines = File.Exists(probe) ? File.ReadAllLines(probe) : new string[0];
        Check(lines.Length == 2, $"★ C-1 落盘行数 = 2（表头 + 1 行），现读 {lines.Length} —— 🧨 多写一行表头/漏写 → 本条红");
        if (lines.Length == 2)
        {
            var head = lines[0].Split('\t');
            var row = lines[1].Split('\t');
            Check(head.Length == 8, $"★ C-2 表头 8 列（现读 {head.Length}）");
            Check(row.Length == 8, $"★ C-3 数据行 8 列（现读 {row.Length}）—— 🧨 少写一格 tab ⇒ 本条红");
            Check(row.Length == head.Length, "★ C-4 每行列数 == 表头列数");
            if (row.Length >= 8)
            {
                Check(row[0] == "AM10", $"★ C-5 `cardId` 列 = `AM10`（现读 `{row[0]}`）");
                Check(row[1] == "play", $"★ C-6 `evt` 列 = `play`（现读 `{row[1]}`）");
                Check(row[2] == "W22Probe_on", $"★ C-7 `fx` 列 = `W22Probe_on`（现读 `{row[2]}`）");
                Check(row[3] == "1.2500", $"★ C-8 `t` 列 = 1.2500（现读 `{row[3]}`）");
                Check(row[4] == "1.5000" && row[5] == "-2.2500" && row[6] == "3.0000",
                      $"★ C-9 世界点三列 = 1.5000 / -2.2500 / 3.0000（现读 `{row[4]}` / `{row[5]}` / `{row[6]}`）");
                Check(row[7] == "5", $"★ C-10 `parentLayer` 列 = 5（现读 `{row[7]}`）");
            }
        }

        // ---- D：表头只写一次（第二行不再重复列名）----
        WarpforgeVFX.WarpforgeEffectHook.S3Trace.Capture("W22Probe_on2", Vector3.zero, -1, "AM11", "ability", 2f);
        lines = File.Exists(probe) ? File.ReadAllLines(probe) : new string[0];
        int headCount = 0;
        for (int i = 0; i < lines.Length; i++) if (lines[i].StartsWith("cardId\t")) headCount++;
        Check(headCount == 1, $"★ D-1 表头**只出现一次**（现读 {headCount} 次）—— 🧨 去掉「已存在就不写表头」⇒ 本条红");
        Check(lines.Length == 3, $"★ D-2 现在共 3 行（表头 + 2 条数据），现读 {lines.Length}");

        // ---- E：字段清洗（tab / 换行不许把一列撑成两列）----
        WarpforgeVFX.WarpforgeEffectHook.S3Trace.Capture("W22\tProbe\nX", Vector3.zero, -1, "A\tM10", "p\nlay", 0f);
        lines = File.Exists(probe) ? File.ReadAllLines(probe) : new string[0];
        var last = lines.Length > 0 ? lines[lines.Length - 1].Split('\t') : new string[0];
        Check(last.Length == 8, $"★ E-1 带 tab/换行的字段仍然只占 8 列（现读 {last.Length}）—— 🧨 去掉 `Clean()` ⇒ 本条红");
        Check(last.Length == 8 && last[2] == "W22 Probe X", $"★ E-2 件名里的 tab/换行被换成空格（现读 `{(last.Length == 8 ? last[2] : "?")}`）");

        // ---- F：关掉之后不再长 ----
        int before = File.Exists(probe) ? File.ReadAllLines(probe).Length : 0;
        WarpforgeVFX.WarpforgeEffectHook.S3Trace.SetEnabledForTest(false, probe);
        WarpforgeVFX.WarpforgeEffectHook.S3Trace.Capture("W22Probe_off3", Vector3.zero, -1, "AM10", "play", 9f);
        int after = File.Exists(probe) ? File.ReadAllLines(probe).Length : 0;
        Check(before == after && before > 0, $"★ F-1 关掉之后再 `Capture` **不长行**（{before} → {after}）");

        // ---- 收尾：撤掉强开关，别把 trace 永久钉在探针文件上 ----
        WarpforgeVFX.WarpforgeEffectHook.S3Trace.ClearTestOverride();
        Check(!WarpforgeVFX.WarpforgeEffectHook.S3Trace.Enabled || WarpforgeVFX.WarpforgeEffectHook.S3Trace.EnvWantsTrace(),
              "★ G-1 `ClearTestOverride()` 之后回到「看环境变量」那一档（自检没把 trace 永久开着）");
    }
}
