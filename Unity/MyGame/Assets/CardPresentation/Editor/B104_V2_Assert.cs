// B104_V2_Assert.cs — 第一百零四会话 · 执行代理 `B104_V2`（2026-10-31 · 特效/动画 V-2）
//
// ============================ 它盯的是什么 ============================
// 本轮落地的**唯一一件产品改动**：战斗表现层的**播放 trace 记录器**（默认关闭）——
//   · 收口 = `BattleDriver.PendingSignalQueue.Add`（`_timeline` 的唯一写入口；四族 cue 一处收口）
//   · 落盘器 = `CardPresentation.AnimTrace`（`Core/EventTiming.cs` 文件尾的顶层类）
//   · 对账器 = `工具/compare_anim_trace.py`（拿 trace 与 `card_anim_order.json` / `animinfo_lookup.json` 全池比对）
//
// 判据 / 背景 → `资料/完全复刻/判据/B99_A2_动画时长_判据.md` ·
//             `资料/普查产出_第三十三会话/R10_卡牌打出后演出时间线.md`。报告 → `资料/完全复刻/报告/B104_V2.md`。
//
// ============================ 三条断言 ============================
//   A0 **默认关闭 ⇒ 零产物**：关着的时候 `Append` 既不加行、也不建目录/文件；
//      并且「默认档 = 环境变量档」（没人强开时 `Enabled == EnvWantsTrace()`）。
//   A1 **打开后一次模拟调度能记到行**：走 `BattleDriver.AnimTraceProbeForTest`（= `PendingSignalQueue.Add`
//      用的**同一条**提取 + 落盘路径）⇒ 文件里正好「表头 + 1 行」，九个字段逐个对（含 `holdSeconds`
//      取自 `CardAnimBeat.Duration`、`seat` 取自 `evt.Player`）；关掉之后 `Append` 又是空操作。
//   A2 **列数与列名稳定**：`AnimTrace.Columns` 九列、列序逐字锁死（改列 = 对账器读歪 ⇒ 这里先红）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   `B104_V2_Assert.CheckB104V2();`
//   · **不需要 `BattleDriver` 实例、不需要开局**（本件验的是「记录器 + 提取路径」，不是某一局的演出）。
//   · 本件**自己收尾**：临时文件删掉、强开强关撤掉（回到「看环境变量」档）⇒ 不会留在时间线上任何东西。
//   · ⚠️ **唯一的环境前提**：本机进程**没有**设 `WF_ANIM_TRACE=1`。设了的话 A0 会红——
//     那时请关掉那个环境变量再跑（这是**配置问题**，⛔ 不要为了它把 A0 改软）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   · 把 `Append` 的 `if (!Enabled) return;` 删掉（关着也写）⇒ **A0** 红（多出文件 / 行数不为 0）。
//   · 把默认值改成开（`_envCached` 初值 / `EnvWantsTrace` 恒真）⇒ **A0c** 红。
//   · `PendingSignalQueue.Trace` 里 `holdSeconds` 不从 `beatData.Duration` 取（例如写死 0 或改成
//     `EventTiming.DurationOf`）⇒ **A1e** 红（期望 0.2500）。
//   · `seat` 不从 `evt.Player` 取（写死 0）⇒ **A1f** 红（本件传 `seat = 1`）。
//   · 列增删 / 改名 / 换序 ⇒ **A2 / A1d** 红（对账器 `compare_anim_trace.py` 认的就是这九列）。
//   · **读之前不 `Close()`**（边开着写句柄边 `File.ReadAllLines`）⇒ 读端按 `FileShare.Read` 打开、
//     与已存在的写句柄**共享冲突** ⇒ `IOException`（2026-10-31 本地 .NET 8 复现过，与首轮 A1d/e/f 红同一个签名）
//     ⇒ 现在这条会被 **A1c3** 当场点名（⛔ 不许再用 `catch { }` 静默吞成「读回 0 行」）。
//   · ⚠️ **本件不断**「真打一局时逐拍 trace 是否正确」—— 那要真机 / Play 跑一局才能看（报告 §四）。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B104_V2`：播放 trace 记录器（默认关闭）的**结构断言**。
/// <para>⛔ 期望值全是**字面量**（0 / 1 / 9 / 0.2500），不从被测实现里反算。</para></summary>
public static class B104_V2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[V2] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    /// <summary>本件唯一入口（宿主 `Run()` 里调一次；⛔ 不用 `Test` 前缀，同 `B97W3_Assert` 的理由）。</summary>
    public static void CheckB104V2()
    {
        MenuCheck.Section(_sink, "V2：播放 trace 记录器（默认关 · 零产物 · 打开能记到行 · 列名稳定）");
        CheckA0DefaultOffHasNoSideEffects();
        CheckA1RecordWhenEnabled();
        CheckA2ColumnsStable();
        Debug.Log(_sink.P + "V2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ==================================================================
    //  A0：默认关闭 ⇒ 零产物（不建目录、不建文件、行数恒 0）
    // ==================================================================
    static void CheckA0DefaultOffHasNoSideEffects()
    {
        // 先撤掉任何强开强关 ⇒ 回到「看环境变量」那一档（= 生产默认）
        AnimTrace.ClearTestOverride();

        // A0a（前提）：本机进程**没有**设 WF_ANIM_TRACE —— 设了的话 A0 会红，那是配置问题
        Tr(!AnimTrace.EnvWantsTrace(),
            "A0a（前提）本进程**没设** `WF_ANIM_TRACE`（设了 A0 会红：那是**配置问题**，请 unset 后再跑；"
          + "⛔ 不要为了它把 A0 改软）");

        string dir = AnimTrace.DirPath;
        Tr(!string.IsNullOrEmpty(dir), "A0b（前提）`AnimTrace.DirPath` 算得出来（persistentDataPath 可用）");
        bool dirBefore = !string.IsNullOrEmpty(dir) && System.IO.Directory.Exists(dir);
        int filesBefore = dirBefore ? System.IO.Directory.GetFiles(dir).Length : 0;

        // 关着的时候调两次 —— 必须**完全无副作用**
        AnimTrace.Append("B104V2_OFF_1", "Ability", "OffBeat", 1f, 0.5f, 0, "beat", true);
        AnimTrace.Append("B104V2_OFF_2", "Attack", null, 2f, 0f, 1, "event", true);

        C(AnimTrace.Rows, 0, "A0c ★ 关着的时候 `Append` **一行都不记**（`Rows` 恒 0）");
        Tr(AnimTrace.FilePath == null, "A0d ★ 关着的时候**没有**当前文件（`FilePath == null`，即没建过文件）");
        bool dirNow = !string.IsNullOrEmpty(dir) && System.IO.Directory.Exists(dir);
        int filesNow = dirNow ? System.IO.Directory.GetFiles(dir).Length : 0;
        C(dirNow, dirBefore, "A0e ★ trace 目录的**有无**不变（关着的时候 ⛔ 不许 `CreateDirectory`）");
        C(filesNow, filesBefore, "A0f ★ trace 目录里的**文件数**不变（关着的时候 ⛔ 不许建文件）");
        C(AnimTrace.Enabled, AnimTrace.EnvWantsTrace(),
            "A0g ★ 默认档 = 环境变量档（没人强开时 `Enabled == EnvWantsTrace()`；🧨 把默认改成开 ⇒ 这条红）");
    }

    // ==================================================================
    //  A1：打开之后，一次「模拟调度」能记到一行（九列逐个对）
    // ==================================================================
    static void CheckA1RecordWhenEnabled()
    {
        string path = System.IO.Path.Combine(Application.temporaryCachePath, "B104V2_trace_selftest.tsv");
        try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); } catch { }

        AnimTrace.SetEnabledForTest(true, path);
        Tr(AnimTrace.Enabled, "A1a（前提）强开之后 `Enabled == true`");

        // 一次模拟调度：走 `PendingSignalQueue.Trace` **同一条**提取 + 落盘路径
        //   座位传 1、阻塞时长传 0.25（经 `CardAnimBeat.vd` 注入 ⇒ `Duration` 恰好等于它）
        BattleDriver.AnimTraceProbeForTest("B104V2_PROBE", "ProbeBeat", 12.5f, 0.25f, 1);
        AnimTrace.Flush();

        C(AnimTrace.Rows, 1, "A1b ★ 一次模拟调度 ⇒ 正好记 **1** 行（`Rows == 1`）");
        Tr(AnimTrace.FilePath == path, "A1c 落到了指定的那个文件（`FilePath == 传入 path`）");

        // 🔴 2026-10-31（修 A1d/A1e/A1f 红）：**先关掉 writer 再读**。
        //   原来这里是「边开着写句柄边 `File.ReadAllLines`」，而且用 `catch { }` **静默吞异常**
        //   ⇒ 一旦读端拿不到句柄（共享模式）/ 内容没落到 OS，读回就是空数组，**看不出真正的原因**。
        //   现在：① `Close()` 让它 flush + 释放句柄（`EnsureWriter` 已改成 `FileShare.ReadWrite`，
        //   关掉之后同一路径续写不会截断）；② 读的异常**记下来并断出来**（⛔ 不静默）。
        AnimTrace.Close();
        string readError = null;
        string[] lines = new string[0];
        bool exists = System.IO.File.Exists(path);
        try { if (exists) lines = System.IO.File.ReadAllLines(path); }
        catch (System.Exception ex) { readError = ex.GetType().Name + ": " + ex.Message; }

        Tr(exists, "A1c2（前提）trace 文件**真的落在** `" + path + "` 上"
                 + "（`AnimTrace.FilePath` = `" + (AnimTrace.FilePath ?? "(null)") + "`）");
        Tr(readError == null, "A1c3（前提）读回**没有**抛异常（实得：" + (readError ?? "无") + "）");
        C(lines.Length, 2, "A1d ★ 文件内容 = **表头 + 1 行**（`ReadAllLines` 的条数 = 2）");

        string header = lines.Length > 0 ? lines[0] : "";
        C(header, AnimTrace.HeaderLine, "A1e ★ 第一行**逐字等于** `AnimTrace.HeaderLine`（改列名/列序 ⇒ 这条红）");

        string[] f = lines.Length > 1 ? lines[1].Split('\t') : new string[0];
        C(f.Length, 9, "A1f ★ 数据行**正好 9 列**（🧨 少一列/多一列 ⇒ 对账器读歪，这里先红）");
        if (f.Length == 9)
        {
            C(f[0], "1", "A1g 列 1 `seq` = 1（本会话第 1 行）");
            C(f[1], "B104V2_PROBE", "A1h 列 2 `cardId` = 探针传进去的卡名");
            C(f[2], "Ability", "A1i 列 3 `evtKind` = `EvtKind.Ability` 的枚举名");
            C(f[3], "ProbeBeat", "A1j 列 4 `piece` = 这一拍播的那件（`beatVfx`）");
            C(f[4], "12.5000", "A1k 列 5 `startOffset` = 12.5000（原样记 `p.at`）");
            C(f[5], "0.2500", "A1l ★ 列 6 `holdSeconds` = 0.2500（= `CardAnimBeat.Duration`；"
                             + "🧨 写死 0 / 改成 `EventTiming.DurationOf` ⇒ 这条红）");
            C(f[6], "1", "A1m ★ 列 7 `seat` = 1（= `evt.Player`；🧨 写死 0 ⇒ 这条红）");
            C(f[7], "beat", "A1n 列 8 `cueKind` = beat（按表展开的拍）");
            C(f[8], "1", "A1o 列 9 `firstOfEvent` = 1（`beatTail == false` ⇒ 本组首拍）");
        }

        // 关掉之后必须又是空操作（同一会话里切换 —— 灭「只读一次环境变量就再也不看状态」的实现）
        AnimTrace.SetEnabledForTest(false);
        int rowsBefore = AnimTrace.Rows;
        AnimTrace.Append("B104V2_OFF_3", "Ability", "OffBeat", 3f, 0f, 0, "beat", true);
        C(AnimTrace.Rows, rowsBefore, "A1p ★ 同一个会话里关掉之后 `Append` 又变回空操作（行数不变）");

        // 收尾：撤强开强关 + 删临时文件（本件不往外留痕迹）
        AnimTrace.ClearTestOverride();
        try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); } catch { }
        Tr(!System.IO.File.Exists(path), "A1q 收尾：临时 trace 文件已删除");
    }

    // ==================================================================
    //  A2：列数与列名稳定（对账器 `工具/compare_anim_trace.py` 认的就是这九列）
    // ==================================================================
    static void CheckA2ColumnsStable()
    {
        C(AnimTrace.Columns.Length, 9, "A2a ★ `AnimTrace.Columns` 是 **9** 列");
        C(AnimTrace.HeaderLine,
          "seq\tcardId\tevtKind\tpiece\tstartOffset\tholdSeconds\tseat\tcueKind\tfirstOfEvent",
          "A2b ★ 列名与列序逐字锁死（🧨 改名/换序 ⇒ `compare_anim_trace.py` 读歪 ⇒ 这条红）");
        C(AnimTrace.EnvVar, "WF_ANIM_TRACE", "A2c 开关的环境变量名固定为 `WF_ANIM_TRACE`（文档/工具都按它写）");
        C(AnimTrace.DirName, "WarpforgeAnimTrace", "A2d 落盘目录名固定为 `WarpforgeAnimTrace`（对账器按它找最新一份）");
    }
}
