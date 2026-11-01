// B116W42_Assert.cs — 执行代理 `B116_W42`（2026-11-02）· `ID-01` S3 表现维剩 5 条里**离线可做**的两条。
//
// ============================ 它盯的是什么 ============================
//  判据 → `资料/完全复刻/判据/B108_ID01_S3_表现维.md` §5.2 的缺口清单。
//    · `S3-G1` 逐卡特效播放记录**不落盘**（原来只在 `BattleScene._fired` 的内存里、每次建场 `Clear()`）；
//    · `S3-G3` 逐卡语音记录只有**一个单值**读口（`LastUnitVoiceAudioOnlyForTest`）⇒「两张卡各响一次」验不出。
//  本文件两段：
//    **G1** = `S3VfxTrace`（`Editor/BattleScene.cs` 类尾）的开关语义与 TSV 形状 —— 关着 = 零字节；开着 = 落盘
//            且每一行列数一致；并且**真的从磁盘读回来**核对件名（⛔ 不读内存计数器当证据）。
//    **G3** = 同一个写入口喂出来的三份记录（旧单值 / 有序日志 / 按卡字典）在「两张卡各响一次」之后**各就各位**。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 G1-a：把 `Capture` 的 `if (!Enabled) return;` 删掉 ⇒ 「关着 = 零字节」那三条里至少一条红（文件/行数/`FilePath`）。
//  🧨 G1-b：把 TSV 的列少写一格 ⇒ 「每一行列数 == 表头列数」那条红（**8 列**是字面量，不从 `Columns` 反推）。
//  🧨 G1-c：件名那一列比对的是**本次探针独有的字符串**（`S3TraceProbe_*`）—— 只有真经过钩子才会出现。
//  🧨 G3-a：把 `NoteUnitVoiceAudioOnly` 里的 `_unitVoiceAudioByCard[...] = rec` 删掉 ⇒ 「按卡取得到」两条红。
//  🧨 G3-b：把旧单值那行删掉 ⇒ 「旧读口 == 最后一条」红（**兼容读口不许失守**）。
//  🧨 G3-c：两张卡的记录必须**互不相同**且各自带自己的 id ⇒ 「一张卡覆盖另一张」这种形状也会红。
//
// ============================ 覆盖边界（如实记）============================
//  · G1 只覆盖 `CardEffects.Play` 那条路（`PlayInstance` 接了就走那条，见 `S3VfxTrace` 类头）；
//    探针件名是**故意不存在的**（`WarpforgeEffectPlayer.Play` 会返回 null 并打一行「没播出来」）
//    —— 本件验的是**记录机制**，不是「那件特效真渲染出来了」。
//  · G3 走 `driver.SpeakForForTest`（= 生产同一个 `SpeakFor` 口），语音文件用
//    `VoiceLines.SetExactForTest` 注入一条**本机真有**的 clip（与 `BattleMiscAssertE4` C 段同一手法）。
//  · 本文件**自己 `driver.Begin(...)` 重开一局** ⇒ 接在宿主聚合行**最后一跳**（其后没有断言读对局状态）。
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class B116W42_Assert
{
    const string Prefix = "[B116W42] ";
    static int _ok, _bad;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _ok; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(Prefix + "✓ " + what); }
        else { _bad++; Debug.LogError(Prefix + "✗ " + what); }
    }

    static void CheckInt(int got, int want, string what)
    {
        Check(got == want, what + $"（实得 {got} / 期望 {want}）");
    }

    /// <summary>宿主入口（`BattleScene.Run` 的聚合行调用；`driver` 必须**还活着**）。</summary>
    public static void Run(BattleDriver driver)
    {
        Debug.Log(Prefix + "=== ID-01 S3 表现维：S3-G1 落盘 trace · S3-G3 逐卡语音 ===");
        RunG1(driver);
        RunG3(driver);
        Debug.Log(Prefix + $"B116W42 合计：{_ok} 通过 / {_bad} 失败");
    }

    // ==================================================================
    //  G1 · `S3VfxTrace`：关着 = 零字节；开着 = 落 TSV（每行列数一致）
    // ==================================================================
    static void RunG1(BattleDriver driver)
    {
        Debug.Log(Prefix + "--- G1 · 特效 trace 落盘（默认关）---");

        string tmpDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "_b116w42"));
        string tmp = Path.Combine(tmpDir, "s3_vfx_trace.tsv");
        try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
        try { if (Directory.Exists(tmpDir)) Directory.Delete(tmpDir, true); } catch { }

        try
        {
            // ---- 前提：批处理环境没设 `WF_S3_TRACE`（设了的话「默认关」这一档就换口径了，必须说清）----
            Check(!S3VfxTrace.EnvWantsTrace(),
                  "（前提）批处理环境**没有**设 `WF_S3_TRACE` ⇒ 下面「默认关」那一档成立"
                + "（若这里红了：是**跑的人**设了这个环境变量，不是代码错）");

            // ---- 结构：默认落点 = `<工程根>/_tmp_view/s3_vfx_trace.tsv`（8 列）----
            string def = S3VfxTrace.DefaultPath;
            Check(!string.IsNullOrEmpty(def) && def.Replace('\\', '/').EndsWith("_tmp_view/" + S3VfxTrace.FileName),
                  $"（结构）默认落点 = `<工程根>/_tmp_view/{S3VfxTrace.FileName}`（实得 `{def}`）");
            CheckInt(S3VfxTrace.Columns.Length, 8, "（结构）TSV 列数 = 8（`cardId/evt/fx/t/x/y/z/parentLayer`）");

            // ---- ① 关着：真调一次钩子，一个字节都不许写 ----
            S3VfxTrace.ClearTestOverride();
            S3VfxTrace.SetEnabledForTest(false);
            Check(!S3VfxTrace.Enabled, "（前提）强关之后 `Enabled == false`");
            // ⚠️ 默认落点可能**本来就有**（上一次跑留下的）⇒ 判据是「这一趟**没有新产生/没被改动**」，不是「不存在」。
            bool defExisted = !string.IsNullOrEmpty(def) && File.Exists(def);
            long defLen = defExisted ? new FileInfo(def).Length : -1;
            CardEffects.Fire("S3TraceProbe_Off", new Vector3(1f, 2f, 3f));
            CheckInt(S3VfxTrace.Rows, 0, "★ G1-1 关着：钩子照跑，但 trace **一行都没记**（`Rows == 0`）");
            Check(S3VfxTrace.FilePath == null,
                  $"★ G1-2 关着：**连文件都不建**（`FilePath == null`，实得 `{S3VfxTrace.FilePath}`）");
            Check(!File.Exists(tmp),
                  $"★ G1-3 关着：磁盘上**没有**这个文件（`{tmp}`）"
                + "｜🧨 删掉 `Capture` 首句的 `if (!Enabled) return;` ⇒ 本三条至少一红");
            Check(File.Exists(def) == defExisted
                  && (!defExisted || new FileInfo(def).Length == defLen),
                  $"★ G1-3b 关着：**默认落点**也没被碰（`{def}`；开跑前存在={defExisted} · 字节 {defLen}）"
                + "｜🧨 关着还往默认文件里写 ⇒ 本行红（这条才直接对应「默认关 = 没有 trace 文件」）");

            // ---- ② 开着：钩子每调一次落一行 ----
            S3VfxTrace.SetEnabledForTest(true, tmp);
            Check(S3VfxTrace.Enabled && S3VfxTrace.FilePath == tmp,
                  $"（前提）强开之后写到显式路径（`FilePath` = `{S3VfxTrace.FilePath}`）");

            var ev = driver != null ? driver.CurrentVfxEvent : null;
            string wantCard = ev != null ? ev.CardId : "";
            string wantEvt = ev != null ? ev.Kind.ToString() : "";

            CardEffects.Fire("S3TraceProbe_OnA", new Vector3(4f, 5f, 6f));
            CheckInt(S3VfxTrace.Rows, 1, "★ G1-4 开着：第一件落一行（`Rows == 1`）");

            CardEffects.Fire("S3TraceProbe_OnB", new Vector3(7f, 8f, 9f));
            CheckInt(S3VfxTrace.Rows, 2, "★ G1-5 开着：第二件再落一行（`Rows == 2`）");

            S3VfxTrace.Close();      // 读回磁盘之前先收流（`AutoFlush` 已刷，这里只是保险）

            var lines = File.Exists(tmp) ? File.ReadAllLines(tmp) : new string[0];
            CheckInt(lines.Length, 3, "★ G1-6 磁盘上 = 表头 + 2 行数据（`File.ReadAllLines` 真读回来）");
            if (lines.Length >= 1)
                Check(lines[0] == S3VfxTrace.HeaderLine,
                      $"★ G1-7 首行是 `Columns` 拼出来的表头（实得 `{lines[0]}`）");

            bool allSameWidth = lines.Length > 0;
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].Split('\t').Length != 8) allSameWidth = false;
            Check(allSameWidth,
                  "★ G1-8 每一行都切成 **8** 列（TSV 列数一致）"
                + "｜🧨 少写一列 ⇒ 本行红（8 是字面量、⛔ 不从 `Columns` 反推）");

            if (lines.Length >= 3)
            {
                var a = lines[1].Split('\t');
                var b = lines[2].Split('\t');
                Check(a.Length == 8 && a[2] == "S3TraceProbe_OnA",
                      $"★ G1-9 第一行 `fx` 列 = 本次探针件名（实得 `{(a.Length > 2 ? a[2] : "<缺列>")}`）"
                    + "｜🧨 记录不是从真钩子来的（或列序错位）⇒ 红");
                Check(b.Length == 8 && b[2] == "S3TraceProbe_OnB",
                      $"★ G1-10 第二行 `fx` 列 = 第二次探针件名（实得 `{(b.Length > 2 ? b[2] : "<缺列>")}`）");
                Check(a[0] == wantCard && a[1] == wantEvt,
                      $"★ G1-11 `cardId`/`evt` 两列 = `BattleDriver.CurrentVfxEvent` 那两位"
                    + $"（实得 `{a[0]}`/`{a[1]}`，期望 `{wantCard}`/`{wantEvt}`）"
                    + " —— ⚠️ 它可能是**空**（那一拍没有卡级事件）；这里断的是「与读口一致」");
                Check(a[3].Length > 0 && a[7].Length > 0,
                      $"★ G1-12 `t` / `parentLayer` 两列非空（实得 `{a[3]}` / `{a[7]}`）");
            }

            // ---- ③ 再关：文件停在 3 行，一个字节都不许再写 ----
            S3VfxTrace.SetEnabledForTest(false);
            CardEffects.Fire("S3TraceProbe_OffAgain", new Vector3(0f, 0f, 0f));
            CheckInt(S3VfxTrace.Rows, 0, "★ G1-13 再关之后：计数归零（`SetEnabledForTest(false)` 的口径）");
            var lines2 = File.Exists(tmp) ? File.ReadAllLines(tmp) : new string[0];
            CheckInt(lines2.Length, 3,
                     "★ G1-14 再关之后：磁盘**还是 3 行**（关着 = 不追加一个字节）"
                   + "｜🧨 开关失效 ⇒ 这里变 4");
        }
        finally
        {
            S3VfxTrace.ClearTestOverride();
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            try { if (Directory.Exists(tmpDir)) Directory.Delete(tmpDir, true); } catch { }
        }
    }

    // ==================================================================
    //  G3 · 逐卡语音记录：两张卡各响一次 ⇒ 两个 cardId 各自读得到
    // ==================================================================
    static void RunG3(BattleDriver driver)
    {
        Debug.Log(Prefix + "--- G3 · 逐卡语音记录 ---");
        if (driver == null)
        {
            Check(false, "（前提）拿到 `BattleDriver` 实例（⛔ 假 null = 已被 `DestroyImmediate` 卸掉 ⇒ 本断言必须接在卸载之前）");
            return;
        }

        bool recSave = BattleDriver.RecordReplays;
        try
        {
            BattleDriver.RecordReplays = false;      // 本段只碰表现层，不必留录像
            driver.Begin("Ultramarines", "Goff", 20261102);
            Check(driver.Ctx != null && driver.UnitChat != null && VoiceLines.Ready,
                  "（前提）新开一局，`Ctx` / `UnitChat` / `VoiceLines.Ready` 都在位"
                + "（`SpeakFor` 首句就是 `if (_unitChat == null || !VoiceLines.Ready) return;`）");
            if (driver.Ctx == null || driver.UnitChat == null || !VoiceLines.Ready) return;

            // ---- 夹具：棋盘上两张**不同 id** 的真单位 ----
            CardDef cardA = null, cardB = null;
            int seatA = -1, slotA = -1, seatB = -1, slotB = -1;
            for (int seat = 0; seat < 2; seat++)
            {
                var board = driver.Ctx.Players[seat].Board;
                for (int s = 0; s < board.Length; s++)
                {
                    var u = board[s];
                    if (u == null || u.Card == null || string.IsNullOrEmpty(u.Card.Id)) continue;
                    if (cardA == null) { cardA = u.Card; seatA = seat; slotA = s; }
                    else if (cardB == null && u.Card.Id != cardA.Id) { cardB = u.Card; seatB = seat; slotB = s; }
                }
            }
            Check(cardA != null && cardB != null,
                  $"（前提）棋盘上有两张**不同 id** 的单位（实得 `{(cardA != null ? cardA.Id : "<无>")}` / `{(cardB != null ? cardB.Id : "<无>")}`）");
            if (cardA == null || cardB == null) return;

            // ---- 夹具：找一条**本机真能加载**的语音文件，拿它注入两张卡的 `attack` 精确档 ----
            string clipFile = FindLoadableClip(driver, cardA.Id);
            Check(!string.IsNullOrEmpty(clipFile),
                  "（前提）找得到一条能加载的语音文件（拿它当两张卡的 `attack` 精确绑定）");
            if (string.IsNullOrEmpty(clipFile)) return;

            // ---- 前提：`hudRoot` 在位（`PlayUnitVoiceClipOnly` 里没有播放面会**早退**、什么都不记）----
            Check(driver.hudRoot != null,
                  "（前提）`hudRoot` 在位（⛔ 没有它 `EnsureUnitVoiceSfx()` 返回 null、`PlayUnitVoiceClipOnly` 早退 ⇒ 下面全成空断言）");
            if (driver.hudRoot == null) return;

            driver.ClearUnitVoiceAudioForTest();
            int countBefore = driver.UnitVoiceAudioOnlyCountForTest;

            // ---- 卡 A 响一次 ----
            using (var sc = VoiceLines.SetExactForTest(cardA.Id, VoiceLines.ExactAttack, clipFile))
            {
                Check(sc.WasInjected, "（前提）卡 A 的精确绑定注入成功");
                driver.UnitChat.HideAll();
                driver.SpeakForForTest(new BattleEvent
                {
                    Kind = EvtKind.Attack, Player = seatA, Slot = slotA, CardId = cardA.Id
                });
            }
            CheckInt(driver.UnitVoiceAudioOnlyCountForTest, countBefore + 1,
                     "★ G3-1 卡 A 的 `attack` 真走了纯音频那一路（总计数 +1）");

            // ---- 卡 B 响一次 ----
            using (var sc = VoiceLines.SetExactForTest(cardB.Id, VoiceLines.ExactAttack, clipFile))
            {
                Check(sc.WasInjected, "（前提）卡 B 的精确绑定注入成功");
                driver.UnitChat.HideAll();
                driver.SpeakForForTest(new BattleEvent
                {
                    Kind = EvtKind.Attack, Player = seatB, Slot = slotB, CardId = cardB.Id
                });
            }
            CheckInt(driver.UnitVoiceAudioOnlyCountForTest, countBefore + 2,
                     "★ G3-2 卡 B 也响了一次（总计数 +2）");

            // ---- 被测：按 carId 各自读得到 ----
            string recA = driver.UnitVoiceAudioOnlyOfForTest(cardA.Id);
            string recB = driver.UnitVoiceAudioOnlyOfForTest(cardB.Id);
            Check(recA != null && recA.StartsWith(cardA.Id + "|", System.StringComparison.Ordinal),
                  $"★ G3-3 按卡读口取得到**卡 A**（实得 `{recA ?? "<null>"}`）"
                + "｜🧨 删掉 `_unitVoiceAudioByCard[...] = rec` ⇒ 本行 null ⇒ 红");
            Check(recB != null && recB.StartsWith(cardB.Id + "|", System.StringComparison.Ordinal),
                  $"★ G3-4 按卡读口取得到**卡 B**（实得 `{recB ?? "<null>"}`）"
                + "｜🧨 同上 ⇒ 红");
            Check(recA != recB,
                  $"★ G3-5 两张卡的记录**互不相同**（`{recA}` ≠ `{recB}`）"
                + " —— 🧨 若只留一个单值/一张卡覆盖另一张，本行红");
            Check(recA != null && recA.EndsWith("|" + VoiceLines.ExactAttack, System.StringComparison.Ordinal)
                  && recB != null && recB.EndsWith("|" + VoiceLines.ExactAttack, System.StringComparison.Ordinal),
                  $"★ G3-6 两条都记的是 `attack` 那一档（实得 `{recA}` / `{recB}`）");
            CheckInt(driver.UnitVoiceAudioOnlyCountOfForTest(cardA.Id), 1,
                     "★ G3-7 卡 A 的逐卡计数 = 1");
            CheckInt(driver.UnitVoiceAudioOnlyCountOfForTest(cardB.Id), 1,
                     "★ G3-8 卡 B 的逐卡计数 = 1");
            Check(driver.UnitVoiceAudioOnlyOfForTest("__B116W42_NO_SUCH_CARD__") == null,
                  "★ G3-9 没播过的卡 id ⇒ 读口返回 `null`（⛔ 别拿「随便回一个」蒙过去）");

            // ---- 兼容读口：旧单值必须仍等于**最后一条** ----
            Check(driver.LastUnitVoiceAudioOnlyForTest == recB,
                  $"★ G3-10 兼容读口失守没有：`LastUnitVoiceAudioOnlyForTest` == **最后一条**（实得 `{driver.LastUnitVoiceAudioOnlyForTest}` / 期望 `{recB}`）"
                + "｜🧨 把旧单值那一行删掉 ⇒ 本行红（既有断言靠它）");
            var log = driver.UnitVoiceAudioLogForTest;
            Check(log != null && log.Length >= 2 && log[log.Length - 1] == recB,
                  $"★ G3-11 有序日志 ≥ 2 格、末格 = 最后一条（实得 {log?.Length ?? 0} 格）");
        }
        finally
        {
            BattleDriver.RecordReplays = recSave;
            driver.ClearUnitVoiceAudioForTest();
        }
    }

    /// <summary>从池里找一条**本机装得进 `Resources`** 的精确语音（返回文件名；找不到 = `null`）。
    /// 与 `BattleMiscAssertE4.FindClip` 同一手法：`TryPickExact` 取表项 → `Clip` 真去加载一次。</summary>
    static string FindLoadableClip(BattleDriver driver, string likeCardId)
    {
        string file, text;
        if (!string.IsNullOrEmpty(likeCardId)
            && VoiceLines.TryPickExact(likeCardId, VoiceLines.ExactAttack, out file, out text)
            && VoiceLines.Clip(file) != null) return file;

        var pool = driver.Ctx != null ? driver.Ctx.CardPool : null;
        if (pool == null) return null;
        for (int i = 0; i < pool.Count && i < 400; i++)
        {
            var c = pool[i];
            if (c == null || string.IsNullOrEmpty(c.Id)) continue;
            if (!VoiceLines.TryPickExact(c.Id, VoiceLines.ExactAttack, out file, out text)) continue;
            if (VoiceLines.Clip(file) != null) return file;
        }
        return null;
    }
}
