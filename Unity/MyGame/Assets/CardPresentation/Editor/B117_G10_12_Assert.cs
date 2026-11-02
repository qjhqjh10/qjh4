// B117_G10_12_Assert.cs — 执行代理 `B117-G10-12`（2026-11-02）· 战斗域第 12 分片「音效 · 语音 · BGM」
//
// ============================ 它盯的是什么 ============================
//  本片**只补既有自检没覆盖的那几格**（`BattleScene.cs:16760-16900` 已覆盖 BGM 档位/表/三条轨、
//  `B109_E17_Mixer_Assert` 已覆盖三份 mixer / 8 快照、`ShellScene.cs:4862-4910` 已覆盖 `AudioSourcesController`
//  ⇒ ⛔ 本文件**不重复实现**它们，只指过去）：
//    A. 🔴 **`B117-57`**：`BattleSfx.ResetCounters()` / `ResetQueueForTest()` 两个名字都像「清场」，
//       改前**都不清**（前者不清 `Log`/`Drained`，后者不清计数/节流表）。
//    B. `RemnantSfx.ResetCounters()`（同一族）：改前只清 `Played`/`LastClip`，`LastPitch`/`LastVolume` 留着上一场的值。
//    C. 原版**自己就静音**的那 12 条 cue **不许**出现在表里（铁律 11 例外①：⛔ 别为了「补齐 28 条」随便找一条 clip 顶上）。
//    D. BGM 四条时长常数 + 四个档位常量（判据见下）。
//
// ============================ 判据（逐条实读，出处写在断言里）============================
//  · `B117-57` = 本批账（`活状态表.md` B117-57），来源 = `报告/B117_F11_战斗手感三件套.md` §5
//    「两个名字都像清场、实测都不清」。这是**我们自己的自检口**（原版没有这两个方法）⇒ 判据 = 契约本身：
//    **名字说清什么就清什么**，且两个方法的**分工**要能一句话说清（读口 vs 队列）。
//  · `RemnantSfx`：原版那六条 cue 的读口是我们自己加的诊断面（同上），契约同 A 组。
//  · 12 条静音 cue：`Battle/BattleSfx.cs` 文件头 §「本 build 里原版自己就静音的 12 条」——
//    6 条空 PPtr（`abilityBoxEnter/Exit · syphon · activateBlast · activateSacrifice · dissolveCardInHand`）
//    ＋ 6 条悬挂 GUID（`abilityBoxClick · activateUnstunnable · activateSurvivor · activateTerror ·
//    activateSentinel · activateAutoAttack`）。⚠️ 12 条**都不在** `Resources/battle_sounds.json` 里
//    （现读 27 条 = 16 个 A 组字段 + 11 个 B 组字段）。
//  · BGM 常数：`资料/普查产出_1016战斗/T06d_战斗BGM判据.md` §二 —— 进场 3 s 淡入 Low ·
//    切档 5 s · `FinishMatch` 回 Low **5 s（不是静音）** · 开门 `FadeOut` **2 s 静音**；
//    档位 `-1` 静音 / `0 Low` / `1 Medium` / `2 High`（`BattleMusic.cs:47-48`，`SnapOf` 按它派快照）。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 1（A1+A4）：**先断言 `Log` 被真的填过**（`Drained==1 && Log.Count==1`）**再断言 reset 之后为 0**。
//     删掉 `DrainOne` 里的 `LogAdd` ⇒ A1 红；删掉 `ResetCounters` 里的 `Log.Clear()` ⇒ A4 红。**一条改动不能两边都绿。**
//  🧨 2（A6/A7）：`ResetCounters()` **不许**动待播队列，`ResetQueueForTest()` **必须**动 —— 两条是同一观测量上的**反相**断言。
//     把两者做成同一个方法（都是「全清」或都是「什么都不清」）⇒ 必有一条红。
//  🧨 3（A5）：节流表 `_lastAt` —— reset 前同一字段**必须**被节流、reset 后**必须**不被节流。
//     删 `_lastAt.Clear()` ⇒ 后一条红；删节流闸 ⇒ 前一条红。
//  🧨 4（B2）：seed 之后再读**未经 reset** 的值必须仍是非 0（证明 seed 真写进去了）——
//     否则 B1 的「清零」是空转（值本来就是 0）。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（不建场景对象 · 不读存档 · 不写 PlayerPrefs），
//  但会**临时关掉** `WFSoundPlayer.Enabled`（避免自检真的出声/建音源池），`finally` 里还原。
//  挂法（由调度台决定）：在宿主聚合行**同一物理行**追加
//      B117_G10_12_Assert.Run(); _sink.Pass += B117_G10_12_Assert.Pass; _sink.Fail += B117_G10_12_Assert.Fail;
//  判绿红：`grep "^B117G1012"` 的**合计行**；逐条按**行首** `B117G1012 ✓` / `B117G1012 ✗` 数
//  （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// ⚠️ 批处理下 `AudioSetup.PlayCheck` 直接拒跑（要进 Play 模式）⇒ 本文件**只查数据与接线**，
//    ⛔ 不写「跑不出声就失败」那一类断言。

using System;
using System.Reflection;
using UnityEngine;
using CardPresentation;
using WarpforgeVFX;

/// <summary>执行代理 `B117_G10_12`：`B117-57` 两条 reset 契约 + `RemnantSfx` 读口 + 12 条静音 cue 不许进表 + BGM 常数。</summary>
public static class B117_G10_12_Assert
{
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B117G1012 ✓ " + msg); }
        else { _fail++; Debug.LogError("B117G1012 ✗ " + msg); }
    }

    public static void Run()
    {
        _pass = _fail = 0;
        bool prevEnabled = WFSoundPlayer.Enabled;
        float prevClock = WFSoundPlayer.ClockOverride;
        WFSoundPlayer.Enabled = false;              // 自检不出声、不建音源池（`Played` 照涨，见那件自己的注释）
        try
        {
            // 🔴 **2026-11-02（`B117-FIXD`）**：A 组要验的「同一字段连响两次被 `timeToPlayAgain` 挡掉」依赖
            //   **冷却时钟真的在走且不为 0**；而 `-executeMethod` 批处理里 `Time.time` 实测 **0.000**，原版
            //   `&& (last != 0)` 那个哨兵会把 0 读成「从没播过」⇒ 冷却永远不生效（A2 实得 Throttled=0）。
            //   ⇒ 按 `WFSoundPlayer.ClockOverride` 的既定用法**钉死时刻**（同一个值 ⇒ 两次调用落在窗口内），跑完还原。
            //   ⛔ 这不是放宽断言：A2 的契约「窗口内第二次被挡」仍然是**确定性**地量到的。
            WFSoundPlayer.ClockOverride = 1000f;
            try { GroupA_ResetContract(); }
            finally { WFSoundPlayer.ClockOverride = prevClock; }
            GroupB_RemnantReadouts();
            GroupC_SilentCuesStayOut();
            GroupD_BgmConstants();
        }
        finally
        {
            WFSoundPlayer.Enabled = prevEnabled;
            BattleSfx.ResetQueueForTest();          // 收尾：别把待播队列留给后面的宿主
            Debug.Log($"B117G1012 === 合计：{_pass} 通过 / {_fail} 失败 ===");
        }
    }

    // ==================================================================
    //  A · `B117-57`：两个 reset 的**分工**与**真的清干净**
    // ==================================================================
    //  契约（改后）：
    //    · `ResetCounters()`   = 清**全部读口**（Played/Throttled/LastField/LastClip/Drained/Log/_lastAt），**不碰队列**；
    //    · `ResetQueueForTest()` = 队列 + 时钟 + **上面那一整组**（= 一次「整场重来」）。
    //  ⚠️ 冷却表已收进 `WFSoundPlayer`（键 = **cue 资产**，2026-11-02 `B117-WSOUNDCOOL`），时钟走
    //     `WFSoundPlayer.CooldownClock`；批处理里 `Time.time` 恒 0 ⇒ 本组由 `Run()` 把 `ClockOverride` 钉成**同一个时刻**
    //     ⇒ 「同一字段连续两次」必然被 `timeToPlayAgain(0.1)` 挡掉 —— 这条节流是**确定性**的，不靠运气。
    static void GroupA_ResetContract()
    {
        BattleSfx.ResetQueueForTest();              // 初态

        // ---- A0 前提：清完是干净的（后面每一条都以这个为基线）----
        Check(BattleSfx.Played == 0 && BattleSfx.Throttled == 0
           && BattleSfx.LastField == null && BattleSfx.LastClip == null
           && BattleSfx.Drained == 0 && BattleSfx.Log.Count == 0
           && BattleSfx.QueueLength == 0 && Mathf.Abs(BattleSfx.Clock) < 1e-6f,
              "A0 前提：`ResetQueueForTest()` 之后 Played/Throttled/Last*/Drained/Log/队列/Clock 全归零");

        // ---- A1 造数据：入队 1 条 → 出队 1 条 ⇒ `DrainOne` **真的**往 Log 记过一笔 ----
        bool queued = BattleSfx.EnqueueLegacySoundAsset(BattleSfx.StartTurn);
        Check(queued && BattleSfx.QueueLength == 1,
              $"A1 前提：`Play(startTurn)` 进队 1 条（实得 queued={queued} / QueueLength={BattleSfx.QueueLength}）"
            + "｜🧨 cue 表里没有 `startTurn`（没跑 `工具/import_battle_sfx.py`）⇒ 本条红");
        bool drained = BattleSfx.DrainOne();
        Check(drained && BattleSfx.Drained == 1 && BattleSfx.Log.Count == 1,
              $"A1 `DrainOne()` 出队 1 条、`Log` 记下 1 笔（实得 Drained={BattleSfx.Drained} / Log={BattleSfx.Log.Count}）"
            + "｜🧨 把 `DrainOne` 里的 `LogAdd(...)` 删掉 ⇒ 本条红（下面 A4 的「清空」就成了空转）");

        // ---- A2 节流：同一字段再来一次 ⇒ 被 `timeToPlayAgain` 挡掉（`_lastAt` 有值了）----
        BattleSfx.EnqueueLegacySoundAsset(BattleSfx.StartTurn);
        BattleSfx.DrainOne();
        Check(BattleSfx.Throttled == 1,
              $"A2 同一字段连响两次 ⇒ 第 2 次被 `timeToPlayAgain` 挡掉 1 次（实得 Throttled={BattleSfx.Throttled}）"
            + "｜判据 = 原版 `AudioCue.CanPlay`（`AudioCue.cs:174`）；我们的闸在 `DrainOne`（出队那一刻）");

        // ---- A3+A4 `ResetCounters()` ⇒ 读口全清（含改前**不清**的 Log / Drained）----
        Check(BattleSfx.Log.Count > 0 && BattleSfx.Drained > 0,
              $"A3 前提：reset **之前** `Log`/`Drained` 不是空的（Log={BattleSfx.Log.Count} / Drained={BattleSfx.Drained}）"
            + "｜🧨 这一条与 A4 配对：一条改动不可能让两边同时绿");
        BattleSfx.ResetCounters();
        Check(BattleSfx.Played == 0 && BattleSfx.Throttled == 0
           && BattleSfx.LastField == null && BattleSfx.LastClip == null
           && BattleSfx.Drained == 0 && BattleSfx.Log.Count == 0,
              $"★ **B117-57**：`ResetCounters()` 把**全部读口**清零（Played/Throttled/Last*/Drained/Log；"
            + $"实得 Drained={BattleSfx.Drained} / Log={BattleSfx.Log.Count}）"
            + "｜🧨 改回「不清 Log / Drained」⇒ 本条红（那正是改前的样子）");

        // ---- A5 🧨 `_lastAt` 也清了：reset 之后同一字段**不再**被节流 ----
        BattleSfx.EnqueueLegacySoundAsset(BattleSfx.StartTurn);
        BattleSfx.DrainOne();
        Check(BattleSfx.Throttled == 0,
              $"A5 ★ `ResetCounters()` 把节流表 `_lastAt` 也清了 ⇒ 同一字段 reset 后立刻能再响"
            + $"（实得 Throttled={BattleSfx.Throttled}）｜🧨 删掉 `_lastAt.Clear()` ⇒ 本条变 1 ⇒ 红");

        // ---- A6 🧨 反相之一：`ResetCounters()` **不许**动待播队列 ----
        BattleSfx.ResetQueueForTest();
        BattleSfx.EnqueueLegacySoundAsset(BattleSfx.StartTurn);
        BattleSfx.ResetCounters();
        Check(BattleSfx.QueueLength == 1,
              $"A6 ★ `ResetCounters()` **不碰待播队列**（文档写明的分工：它只管读口）—— 实得 QueueLength={BattleSfx.QueueLength}"
            + "｜🧨 让它顺手 `_queue.Clear()` ⇒ 本条红（同时与 A7 构成反相：两者不能都「全清」）");

        // ---- A7 🧨 反相之二：`ResetQueueForTest()` **必须**队列 + 读口一起清 ----
        BattleSfx.ResetQueueForTest();              // A6 留下 1 条 ⇒ 先清，下面那条「= 2」才有意义
        BattleSfx.EnqueueLegacySoundAsset(BattleSfx.StartTurn);
        BattleSfx.EnqueueLegacySoundAsset(BattleSfx.StartTurn);
        Check(BattleSfx.QueueLength == 2, $"A7 前提：队列里现在有 2 条（实得 {BattleSfx.QueueLength}）");
        BattleSfx.ResetQueueForTest();
        Check(BattleSfx.QueueLength == 0 && BattleSfx.Played == 0
           && BattleSfx.Log.Count == 0 && BattleSfx.Drained == 0
           && Mathf.Abs(BattleSfx.Clock) < 1e-6f,
              $"★ **B117-57**：`ResetQueueForTest()` = 一次**整场重来**（队列 + 时钟 + 全部读口；"
            + $"实得 QueueLength={BattleSfx.QueueLength} / Played={BattleSfx.Played} / Log={BattleSfx.Log.Count} / Clock={BattleSfx.Clock:F3}）"
            + "｜🧨 改回「不清计数」⇒ 本条红");
    }

    // ==================================================================
    //  B · `RemnantSfx.ResetCounters()`：同一族的读口补齐（改前只清 2 项）
    // ==================================================================
    //  `LastPitch` / `LastVolume` 是 `{ get; private set; }` ⇒ 自检只能反射 seed（⛔ 不为测试往生产类加 setter）。
    //  🧨 见文件头「灭自证 4」：先证明 seed 真写进去了，再谈「清掉」。
    static void GroupB_RemnantReadouts()
    {
        var t = typeof(RemnantSfx);
        bool seeded = false;
        try
        {
            t.GetProperty("Played", BindingFlags.Public | BindingFlags.Static).SetValue(null, 7);
            t.GetProperty("LastClip", BindingFlags.Public | BindingFlags.Static).SetValue(null, "B117G1012_probe");
            t.GetProperty("LastPitch", BindingFlags.Public | BindingFlags.Static).SetValue(null, 0.7f);
            t.GetProperty("LastVolume", BindingFlags.Public | BindingFlags.Static).SetValue(null, 0.3f);
            seeded = true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[B117G1012] 反射 seed `RemnantSfx` 读口失败（断言会按失败处理）：" + e.Message);
        }
        Check(seeded, "B0 前提：反射把 `RemnantSfx.LastPitch/LastVolume` seed 进去了（否 ⇒ 下面两条无法判定）");

        // ---- B2 🧨 seed 之后**未经 reset** 必须仍是非 0（否则 B1 是空转）----
        Check(RemnantSfx.Played == 7 && RemnantSfx.LastClip == "B117G1012_probe"
           && Mathf.Abs(RemnantSfx.LastPitch - 0.7f) < 1e-4f && Mathf.Abs(RemnantSfx.LastVolume - 0.3f) < 1e-4f,
              $"B2 前提：seed 立刻读回得到（Played={RemnantSfx.Played} / pitch={RemnantSfx.LastPitch:F2} / vol={RemnantSfx.LastVolume:F2}）"
            + "｜🧨 seed 没生效 ⇒ 本条红，且 B1 会变成「清一个本来就是 0 的值」的空转");

        RemnantSfx.ResetCounters();
        Check(RemnantSfx.Played == 0 && RemnantSfx.LastClip == null
           && Mathf.Abs(RemnantSfx.LastPitch) < 1e-6f && Mathf.Abs(RemnantSfx.LastVolume) < 1e-6f,
              $"★ **B117-57 同族**：`RemnantSfx.ResetCounters()` 四项读口一起清零"
            + $"（实得 Played={RemnantSfx.Played} / clip={RemnantSfx.LastClip ?? "null"} / pitch={RemnantSfx.LastPitch:F3} / vol={RemnantSfx.LastVolume:F3}）"
            + "｜🧨 改回「只清 Played/LastClip」⇒ pitch/vol 留着上一场的值 ⇒ 本条红");
    }

    // ==================================================================
    //  C · 原版**自己就静音**的 12 条 cue **不许**进表（铁律 11 例外①）
    // ==================================================================
    //  判据 = `Battle/BattleSfx.cs` 文件头那两条（6 条空 PPtr + 6 条悬挂 GUID，逐条带出处）。
    //  ⚠️ 这里断的是「**表里没有**」—— 因为一旦有人「为了补齐 28 条」给它们随便找一条 clip 顶上，
    //     就会在玩家耳朵里凭空多出一声**原版没有的音**，而自检与截图都看不出来（静默造假数据）。
    static readonly string[] SilentByEmptyPPtr =
    {
        "abilityBoxEnter", "abilityBoxExit", "syphon", "activateBlast",
        "activateSacrifice", "dissolveCardInHand",
    };
    static readonly string[] SilentByDanglingGuid =
    {
        "abilityBoxClick", "activateUnstunnable", "activateSurvivor",
        "activateTerror", "activateSentinel", "activateAutoAttack",
    };

    static void GroupC_SilentCuesStayOut()
    {
        var ta = Resources.Load<TextAsset>(WFSoundBank.BattleTablePath);
        Check(ta != null, $"C0 前提：`Resources/{WFSoundBank.BattleTablePath}.json` 在"
                        + "（产出方 `工具/import_battle_sfx.py`）｜🧨 表没进仓库 ⇒ 下面三条都失去意义 ⇒ 本条红");
        WFSoundTable tbl = null;
        if (ta != null)
        {
            try { tbl = JsonUtility.FromJson<WFSoundTable>(ta.text); }
            catch (Exception e) { Debug.LogWarning("[B117G1012] 解析 battle_sounds.json 失败：" + e.Message); }
        }
        Check(tbl != null && tbl.cues != null && tbl.cues.Length == 27,
              $"C1 战斗 cue 表 = **27 条**（16 个 A 组字段 + 11 个 B 组字段；实得 {(tbl != null && tbl.cues != null ? tbl.cues.Length : -1)}）"
            + "｜🧨 有人把 12 条静音 cue 也塞进表 ⇒ 条数变多 ⇒ 本条红");

        int bad = 0; string badList = "";
        foreach (var n in SilentByEmptyPPtr)
            if (WFSoundBank.HasCue(n)) { bad++; badList += " " + n; }
        Check(bad == 0,
              $"★ 6 条**空 PPtr** 的 cue 不在表里（原版调到也无声：`BattleSfx.cs` 文件头；违例 {bad}{badList}）");

        bad = 0; badList = "";
        foreach (var n in SilentByDanglingGuid)
            if (WFSoundBank.HasCue(n)) { bad++; badList += " " + n; }
        Check(bad == 0,
              $"★ 6 条**悬挂 GUID** 的 cue 不在表里（本 build 解不出资产：判据同上、时间限定 = 本 build；违例 {bad}{badList}）");
    }

    // ==================================================================
    //  D · BGM 四条时长常数 + 四个档位常量（判据 = `T06d_战斗BGM判据.md` §二）
    // ==================================================================
    static void GroupD_BgmConstants()
    {
        Check(Mathf.Abs(BattleMusic.FadeInLowSeconds - 3f) < 1e-6f
           && Mathf.Abs(BattleMusic.FadeToLevelSeconds - 5f) < 1e-6f
           && Mathf.Abs(BattleMusic.FadeFinishSeconds - 5f) < 1e-6f
           && Mathf.Abs(BattleMusic.FadeOutSeconds - 2f) < 1e-6f,
              $"★ BGM 四条时长 = 原版实读：进场 3 s（`0x1834b2e8c`）· 切档 5 s（`timeToChange`）·"
            + $" `FinishMatch` 回 Low 5 s · 开门 2 s（`0x1834b2bbc`）—— 实得 "
            + $"{BattleMusic.FadeInLowSeconds}/{BattleMusic.FadeToLevelSeconds}/{BattleMusic.FadeFinishSeconds}/{BattleMusic.FadeOutSeconds}"
            + "｜🧨 把「结算回 Low」写成静音、或把 2 s 写成 5 s ⇒ 本条红");

        Check(BattleMusic.LevelSilent == -1 && BattleMusic.LevelLow == 0
           && BattleMusic.LevelMedium == 1 && BattleMusic.LevelHigh == 2,
              "★ 档位常量 = `-1 静音 / 0 Low / 1 Medium / 2 High`（`SnapOf` 按这四个值派 mixer 快照，"
            + "⚠️ 别的值一律落 `BattleMusicLow`）｜🧨 改任一个 ⇒ 上面那条「回 Low」会派到错的快照");
    }
}
