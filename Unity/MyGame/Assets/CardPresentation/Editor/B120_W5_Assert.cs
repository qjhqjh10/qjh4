// B120_W5_Assert.cs — 执行代理 `B120-W5`（2026-11-02）· 音效 / 语音 / BGM 域 B120 的五条施工项
//
// ============================ 它盯的是什么 ============================
//  B120 战斗参数审计 F 域（`报告/B120_F_音效语音BGM.md`）的 5 条 ❌，本文件逐条配一条断言：
//    A. `F-5` **FX 音源池容量** = 原版 `SoundManager.prefab` 的 `amountToPool: 20`（改前 24 = 自选数）。
//    B. `F-2` **3D 衰减模板五连** = 原版 `Audio Source Template.prefab`（改前**一个衰减参数都没设**）。
//    C. `F-6` **池耗尽策略** = 全活跃 ⇒ `LogError` + **这一声不播**（改前「轮转盖掉最早那条」）。
//    D. `F-4` **战斗 cue 直放**（原版每条调用点都直接 `SoundManager.Play2D/Play3D`），
//       FIFO **只**吃 `EnqueueLegacySoundAsset`（= 原版 `PlaySoundAsset`）。
//    E. `F-3` **残骸音效 3D 参数** = 同一份模板（改前 Linear / 1 / 40，曲线+起点+终点三处全错）。
//
// ============================ 判据（逐条实读，出处写在断言里）============================
//   · 容量 / 3D 模板：`d:/2/tools/assetripper_out/ExportedProject/Assets/SoundManager.prefab`
//     （`amountToPool: 20`）与 `.../Assets/Audio Source Template.prefab`（`!u!82 &82724804207875695`：
//     `MinDistance: 15.61` · `MaxDistance: 500` · `rolloffMode: 0` · `Priority: 128` · `DopplerLevel: 1` · `Spatialize: 0`）。
//     ⚠️ `assets_full` 里按名字搜不到这两个数（`GameObject/SoundManager.json` 不带组件字段）⇒ 判据只能走 AssetRipper 工程。
//   · 耗尽策略：`d:/2/tools/decomp_full/SoundManager__GetAudioSource.c`（全活跃 ⇒
//     `CustomDebug.LogError("There are not enough audio sources!")` + `return 0`）＋ `SoundManager__Update.c`（回收器）。
//   · 直放 / FIFO 分路：`CardScript__DrawCard.c:50`（`SoundManager__Play2D`）· `CardScript__ChangeAttackType.c:53` ·
//     `CardScript__LandOnGround.c:41`（`Play3D`）· `CardScript._UnitDeath_d__446__MoveNext.c:368`（`Play3D`）；
//     而 `BattleManager__PlaySoundAsset.c` 全文只有「往 `+0x428` append 一句」、泵 = `BattleManager__Update.c:152`。
//   · 残骸：`Assembly-CSharp/RemnantBody.cs:17,26` + `RemnantBody__DoDestroyByAttack.c:35` 等 ⇒ `AudioCue__Play3D` ⇒ 同一个池。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 1（C1 + C2）：**同一次 `DebugRent()` 调用**，注入「池满」⇒ 必须回 null 且 `Dropped` 涨；
//     不注入 ⇒ 必须回**非 null**。把「池满也照旧给一条」（= 改前的轮转）接回去 ⇒ C1 红；
//     把 `DebugRent` 做成恒 null ⇒ C2 红。**一条改动不能两边都绿。**
//  🧨 2（D1 + D2）：**同一个可观察量 `QueueLength`** 上的反相断言 —— `Play` 之后必须是 0、
//     `EnqueueLegacySoundAsset` 之后必须是 1。把两者合并（都入队或都直放）⇒ 必有一条红。
//  🧨 3（B1/B2/E1）：断言的是 **AudioSource 组件上的真值**（不是我们自己的常量）——
//     把建源点改回「`AddComponent` 之后不设参数」⇒ Unity 默认 `minDistance = 1` ⇒ 红。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（不读存档 / 不写 PlayerPrefs / 不需要 driver），
//  会临时关掉 `WFSoundPlayer.Enabled`（不出声）、钉住 `ClockOverride`，`finally` 里还原并
//  销毁本文件建出来的两个运行时宿主（`WFSoundPlayer` / `RemnantSfx`，两者都带 `HideFlags.DontSave`）。
//  挂法（由调度台决定）：接在 `Editor/BattleScene.cs:46983` 那条**方法尾聚合行**里，
//  紧跟 `B117_WCOOL3_Assert.Run(); pass += B117_WCOOL3_Assert.Pass; fail += B117_WCOOL3_Assert.Fail;` 之后
//  （同一物理行追加 ⇒ 行数零变化）：
//      B120_W5_Assert.Run(); pass += B120_W5_Assert.Pass; fail += B120_W5_Assert.Fail;
//  判绿红：`grep "^B120W5"` 的**合计行**；逐条按**行首** `B120W5 ✓` / `B120W5 ✗` 数
//  （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// ⚠️ 批处理下 `AudioSetup.PlayCheck` 直接拒跑（要进 Play 模式）⇒ 本文件**只查参数与接线**，
//    ⛔ 不写「跑不出声就失败」那一类断言。

using System;
using System.Reflection;
using UnityEngine;
using CardPresentation;
using WarpforgeVFX;

/// <summary>执行代理 `B120_W5`：F-2 / F-3 / F-4 / F-5 / F-6 五条音效参数的回归断言。</summary>
public static class B120_W5_Assert
{
    const string Tag = "B120W5";
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    public static void Run()
    {
        int pass0 = _pass, fail0 = _fail;
        bool prevEnabled = WFSoundPlayer.Enabled;
        float prevClock = WFSoundPlayer.ClockOverride;
        WFSoundPlayer.Enabled = false;          // 只查参数/接线：不出声、不建真播放（`DebugRent` 不受它影响）
        WFSoundPlayer.ClockOverride = 5000f;    // 批处理 `Time.time` 恒 0 ⇒ 钉住冷却时钟（同 `B117_G10_12_Assert` 的做法）
        try
        {
            GroupA_PoolCapacity();
            GroupB_AttenuationTemplate();
            GroupC_PoolExhaustedPolicy();
            GroupD_DirectPlayVsFifo();
            GroupE_RemnantAttenuation();
            // ---- W5b 续单（2026-11-02）：判据已定的四条 ⚠️ + 主表 #26 ----
            GroupF_CooldownDefault();
            GroupG_BgmVolumePause();
            GroupH_WindowSoundsSingleLayer();
            GroupI_ResetThrottleChain();
            GroupJ_KeyAndRouting();
        }
        finally
        {
            WFSoundPlayer.Enabled = prevEnabled;
            WFSoundPlayer.ClockOverride = prevClock;
            WFSoundPlayer.DebugForceFullRents = 0;
            WFSoundPlayer.ResetCounter();       // Played / Dropped 都是**我们自己的**读口 ⇒ 别留给后面的宿主
            BattleSfx.ResetQueueForTest();      // 队列 + 时钟 + 全部读口
            RemnantSfx.ResetCounters(); RemnantSfx.ResetThrottle();
            WindowSounds.ResetCounters();
            // `DebugRent` / `DebugEnsureSource` 会建出两个运行时宿主；它们都是 `DontSave`（不会进场景），
            // 但本文件还是自己收尾 —— 免得留给后面的宿主去猜「这东西是谁建的」。
            var a = GameObject.Find("WFSoundPlayer");
            if (a != null) UnityEngine.Object.DestroyImmediate(a);
            var b = GameObject.Find("RemnantSfx");
            if (b != null) UnityEngine.Object.DestroyImmediate(b);
            Debug.Log(Tag + " === 合计：✓ " + (_pass - pass0) + " · ✗ " + (_fail - fail0)
                    + "（累计 ✓ " + _pass + " · ✗ " + _fail + "） ===");
        }
    }

    // ==================================================================
    //  A · `F-5` FX 音源池容量（判据 = `SoundManager.prefab` 的 `amountToPool: 20`）
    // ==================================================================
    static void GroupA_PoolCapacity()
    {
        Check(WFSoundPlayer.PoolCapacity == 20,
              $"A1 ★ F-5 FX 音源池容量 = **20**（原版 `SoundManager.prefab` 的 `amountToPool: 20`；"
            + $"实得 {WFSoundPlayer.PoolCapacity}）｜🧨 改回 24（我们原来自选的数）⇒ 本条红");
    }

    // ==================================================================
    //  B · `F-2` 3D 衰减模板（判据 = `Audio Source Template.prefab`，读**组件真值**）
    // ==================================================================
    static void GroupB_AttenuationTemplate()
    {
        var src = WFSoundPlayer.DebugRent();    // 走 `Rent()` 的生产分支（含建池与五项参数）
        Check(src != null,
              "B0 前提：`DebugRent()` 从池里租到一颗源（否 ⇒ B1/B2 无从谈起，不是静默跳过）");
        if (src == null) return;

        Check(Mathf.Abs(src.minDistance - 15.61f) < 1e-4f && Mathf.Abs(src.maxDistance - 500f) < 1e-4f,
              $"B1 ★ F-2 3D 衰减的**起点/终点** = 原版模板的 `MinDistance 15.61` / `MaxDistance 500`"
            + $"（实得 {src.minDistance} / {src.maxDistance}）"
            + "｜🧨 改回「`AddComponent` 之后不设参数」⇒ Unity 默认 `minDistance = 1` ⇒ 本条红"
            + "（那正是 889 条 `is2d=0` 的 3D 音满音量半径小 15.6 倍的原因）");

        Check(src.rolloffMode == AudioRolloffMode.Logarithmic
           && src.priority == 128
           && Mathf.Abs(src.dopplerLevel - 1f) < 1e-4f
           && !src.spatialize,
              $"B2 ★ F-2 3D 模板另三项 = `rolloffMode` Logarithmic（原版 `rolloffMode: 0`）· `Priority 128` ·"
            + $" `DopplerLevel 1` · `Spatialize 0`（实得 {src.rolloffMode} / {src.priority} / {src.dopplerLevel} / {src.spatialize}）");
    }

    // ==================================================================
    //  C · `F-6` 池耗尽策略（判据 = `SoundManager__GetAudioSource.c`）
    // ==================================================================
    //  批处理里没法真让 20 条 `AudioSource.isPlaying` 同时为真（没有帧循环），
    //  所以用 `DebugForceFullRents`（🔶 自检注入口，生产恒 0）把「池满」那一支确定性地走一遍。
    static void GroupC_PoolExhaustedPolicy()
    {
        int before = WFSoundPlayer.Dropped;

        WFSoundPlayer.DebugForceFullRents = 1;
        var none = WFSoundPlayer.DebugRent();
        Check(none == null && WFSoundPlayer.Dropped == before + 1,
              $"C1 ★ F-6 池满 ⇒ **这一声不播**（回 null）+ `Dropped` 涨 1（原版 `GetAudioSource` 末尾 "
            + $"`LogError` + `return 0`；实得 src={(none == null ? "null" : none.name)} / Dropped {before}→{WFSoundPlayer.Dropped}）"
            + "｜🧨 改回「轮转盖掉最早那条」（池满也给一条）⇒ 本条红");

        // 🧨 反向：不注入时必须**给得出**一条 —— 否则 C1 只是「DebugRent 恒 null」的空转。
        var some = WFSoundPlayer.DebugRent();
        Check(some != null,
              "C2 反向前提：**不注入**池满时 `DebugRent()` 必须给回一条源（否则 C1 是对着「恒 null」写的空转）"
            + $"｜实得 {(some == null ? "null" : some.name)}");
    }

    // ==================================================================
    //  D · `F-4` 战斗 cue 直放 · FIFO 只服务 `PlaySoundAsset`
    // ==================================================================
    static void GroupD_DirectPlayVsFifo()
    {
        var ta = Resources.Load<TextAsset>(WFSoundBank.BattleTablePath);
        bool hasCue = ta != null && WFSoundBank.HasCue(BattleSfx.StartTurn);
        Check(hasCue,
              $"D0 前提：战斗 cue 表 `Resources/{WFSoundBank.BattleTablePath}.json` 里有 `startTurn`"
            + "（产出方 `工具/import_battle_sfx.py`）｜🧨 表不在 ⇒ D1 无从谈起，本条已经红了、不是静默跳过");
        if (!hasCue) return;

        BattleSfx.ResetQueueForTest();
        bool played = BattleSfx.Play(BattleSfx.StartTurn);
        Check(played && BattleSfx.QueueLength == 0
           && BattleSfx.Played == 1 && BattleSfx.LastField == BattleSfx.StartTurn,
              $"D1 ★ F-4 战斗 cue **直放**、**不进 FIFO**：`Play(startTurn)` 之后队列必须是 0 条"
            + $"（实得 ok={played} / QueueLength={BattleSfx.QueueLength} / Played={BattleSfx.Played} / Last={BattleSfx.LastField ?? "（无）"}）"
            + "｜判据 = `CardScript__DrawCard.c:50` / `ChangeAttackType.c:53` / `LandOnGround.c:41` 都直接 `SoundManager.Play2D/Play3D`"
            + "｜🧨 改回「入队、每帧出队一条」⇒ 本条红");

        // 🧨 反向·防一刀切：原版 `PlaySoundAsset(SoundAsset_OLD)` 那条 FIFO **必须还在**。
        BattleSfx.ResetQueueForTest();
        bool queued = BattleSfx.EnqueueLegacySoundAsset(BattleSfx.EndTurnButton);
        Check(queued && BattleSfx.QueueLength == 1,
              $"D2 反向·防一刀切：`EnqueueLegacySoundAsset(endTurnButton)` 仍然**入队 1 条**"
            + $"（实得 ok={queued} / QueueLength={BattleSfx.QueueLength}）"
            + "｜判据 = `BattleManager__PlaySoundAsset.c` 只 append、`PlayNextSoundInQueue` 每帧取一条"
            + "｜🧨 把这条也改成直放（一刀切）⇒ 本条红");
    }

    // ==================================================================
    //  E · `F-3` 残骸音效的 3D 参数（判据 = 与 FX 池同一份模板）
    // ==================================================================
    static void GroupE_RemnantAttenuation()
    {
        var src = RemnantSfx.DebugEnsureSource();
        Check(src != null, "E0 前提：`RemnantSfx.DebugEnsureSource()` 建出了那颗 AudioSource");
        if (src == null) return;

        Check(Mathf.Abs(src.minDistance - 15.61f) < 1e-4f
           && Mathf.Abs(src.maxDistance - 500f) < 1e-4f
           && src.rolloffMode == AudioRolloffMode.Logarithmic
           && src.priority == 128
           && Mathf.Abs(src.dopplerLevel - 1f) < 1e-4f,
              $"E1 ★ F-3 残骸音效 = 原版同一份模板：Logarithmic / 15.61 / 500 / Priority 128 / Doppler 1"
            + $"（实得 {src.rolloffMode} / {src.minDistance} / {src.maxDistance} / {src.priority} / {src.dopplerLevel}）"
            + "｜判据 = `RemnantBody.cs:17,26` 那几格都是 `AudioCue` ⇒ `AudioCue__Play3D` ⇒ `SoundManager` 的 FX 池"
            + "｜🧨 改回 **Linear / 1 / 40**（三处全错，远处残骸声会直接消失）⇒ 本条红");
    }

    // ==================================================================
    //  F · `timeToPlayAgain` 默认值（判据 = `AudioCue__ctor.c` 逐字节）
    // ==================================================================
    static void GroupF_CooldownDefault()
    {
        var c = new WFSoundCue();
        Check(Mathf.Abs(c.timeToPlayAgain - 0.1f) < 1e-6f,
              "F1 ★ `WFSoundCue.timeToPlayAgain` 默认 = **0.1 s**"
            + "（判据 = 原版 `AudioCue__ctor.c`：`*(float *)(this + 0x30) = 0x3dcccccd` = 0.1；"
            + $"同一支里四个 pitch/volume 默认都是 `0x3f800000` = 1.0。实得 {c.timeToPlayAgain}）"
            + "｜🧨 改回 0（= 表里缺这格时永不冷却）⇒ 本条红");

        // 行为影响：两份 cue 表**逐条都显式带这一格** ⇒ 改默认值 = 零行为变化。用**原始 JSON 文本**数，
        // 因为 `JsonUtility` 反序列化时「键缺失」和「值 = 0」不可区分（都会得到字段初值）。
        int missA = MissingKeyCount(WFSoundBank.TablePath, "timeToPlayAgain");
        int missB = MissingKeyCount(WFSoundBank.BattleTablePath, "timeToPlayAgain");
        Check(missA == 0 && missB == 0,
              $"F2 ★ 两份 cue 表**逐条都显式写了** `timeToPlayAgain` ⇒ 改默认值零行为变化"
            + $"（`{WFSoundBank.TablePath}` 缺 {missA} 条 · `{WFSoundBank.BattleTablePath}` 缺 {missB} 条；-1 = 表没读到）"
            + "｜🧨 若哪天生成器开始省略这一格，本条会变红 —— 那就是「行为会变」的信号，要回来逐条列出来");
    }

    /// <summary>用**原始 JSON 文本**数「有几个 cue 没写这个键」（`JsonUtility` 分不出「缺键」与「值为 0」）。
    /// 返回 `-1` = 表读不到。</summary>
    static int MissingKeyCount(string resPath, string key)
    {
        var ta = Resources.Load<TextAsset>(resPath);
        if (ta == null) return -1;
        WFSoundTable tbl;
        try { tbl = JsonUtility.FromJson<WFSoundTable>(ta.text); }
        catch (Exception) { return -1; }
        if (tbl == null || tbl.cues == null) return -1;
        int hits = 0, i = 0;
        string needle = "\"" + key + "\"";
        while ((i = ta.text.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { hits++; i += needle.Length; }
        return Mathf.Max(0, tbl.cues.Length - hits);
    }

    // ==================================================================
    //  G · BGM 的 −60 dB 暂停/恢复（判据 = `AudioSourceTogglerByVolume__Update.c`）
    // ==================================================================
    static void GroupG_BgmVolumePause()
    {
        Check(Mathf.Abs(BattleMusic.VolumePauseThresholdDb + 60f) < 1e-6f,
              "G1 ★ 阈值 = **−60 dB**（判据 = `decomp_full/AudioSourceTogglerByVolume__Update.c` 的 `DAT_1834b3278`，"
            + $"`工具/read_literal.py` 4B 实读；实得 {BattleMusic.VolumePauseThresholdDb}）"
            + "｜🧨 写成 −80（那是 `NoBattleMusic` 快照的落点，⛔ 不是判据）⇒ 本条红");

        Check(!BattleMusic.ShouldBePlaying(-80f) && !BattleMusic.ShouldBePlaying(-60f)
           && BattleMusic.ShouldBePlaying(-59.9f) && BattleMusic.ShouldBePlaying(0f),
              "G2 ★ 边界：`v <= -60` ⇒ **停**（−80 / −60 都停）；`v > -60` ⇒ **播**（−59.9 / 0）"
            + "｜判据同上两条分支｜🧨 把 `<=` 写成 `<` ⇒ −60 那一档漏判；把阈值写成 −80 ⇒ −70 那一档错");

        // 源码面（批处理里没法真让 mixer 过渡，所以这三条只能读源码）：
        //   ① 三个暴露参数名必须与战场场景里那三颗 `AudioSourceTogglerByVolume.audiomixerVolumeLabel` 一致；
        //   ② 每帧必须调它；③ 不许留「三条全部 UnPause」的旧写法（那会让还没升起来的轨也解暂停）。
        string src = Application.dataPath + "/CardPresentation/Battle/BattleMusic.cs";
        if (!System.IO.File.Exists(src))
        {
            Check(false, "G3/G4/G5 前提：读得到 `Battle/BattleMusic.cs`（" + src + "）—— 读不到时等于没验 ⇒ 当场红");
            return;
        }
        bool low = false, mid = false, high = false, called = false, unPause = false;
        foreach (var raw in System.IO.File.ReadAllLines(src))
        {
            var t = raw.TrimStart();
            if (t.StartsWith("//")) continue;      // 注释里有旧写法与判据说明，不算实现
            if (t.IndexOf("TrackLowVolume", StringComparison.Ordinal) >= 0) low = true;
            if (t.IndexOf("TrackMediumVolume", StringComparison.Ordinal) >= 0) mid = true;
            if (t.IndexOf("TrackHighVolume", StringComparison.Ordinal) >= 0) high = true;
            if (t.IndexOf("UpdatePauseByVolume();", StringComparison.Ordinal) >= 0) called = true;
            if (t.IndexOf("UnPause(", StringComparison.Ordinal) >= 0) unPause = true;
        }
        Check(low && mid && high,
              "G3 ★ 三个组音量参数名 = `TrackLowVolume` / `TrackMediumVolume` / `TrackHighVolume`"
            + "（判据 = `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_{4445,4306,4741}.json` 的 "
            + "`audiomixerVolumeLabel` 序列化值；我们的 `Resources/Audio/Music.mixer` 正好暴露了这三个名字）"
            + "｜🧨 名字写错 ⇒ `mixer.GetFloat` 恒 false ⇒ 暂停/恢复整条失效（静默）");
        Check(called,
              "G4 ★ `Tick` 每帧调 `UpdatePauseByVolume()`（原版 `AudioSourceTogglerByVolume` 是 `Update` 里的逐帧判断）"
            + "｜🧨 只挂在淡变结束时 ⇒ 退回改前的近似档 ⇒ 本条红");
        Check(!unPause,
              "G5 ★ 代码里**没有** `UnPause(` —— 恢复播放必须走「组音量 > −60 ⇒ 对齐兄弟轨 `timeSamples` 再 `Play()`」"
            + "（判据 = `AudioSourceTogglerByVolume__Update.c` 的未播分支）"
            + "｜🧨 改回 `StartFade` 里「三条全部 UnPause」⇒ 本条红（原版只解**升起来的那一条**）");
    }

    // ==================================================================
    //  H · `WindowSounds` 冷却 = **全项目共享那一张表**（2026-11-05 并表；判据 = 原版 `lastTimePlayed` 只住在 cue 资产上）
    // ==================================================================
    static void GroupH_WindowSoundsSingleLayer()
    {
        WFSoundCue cue;
        bool found = WFSoundBank.TryGetCue(WindowSounds.DefaultButtonCue, out cue) && cue != null;
        Check(found, $"H0 前提：`{WindowSounds.DefaultButtonCue}` 在音效表里（否 ⇒ H1/H2 无从谈起）");
        if (!found) return;

        // ---- H1 🧨 全项目**一张**冷却表（2026-11-05 `B144-B2` 并表后的新契约）----
        WindowSounds.ResetCounters();
        WFSoundPlayer.ResetThrottle();
        // 先走**内层**那条路写戳：`WFSoundPlayer.Play` 一次（`Enabled=false` ⇒ 不出声，但「先写戳」照旧，
        // 判据 = `AudioCue__ConfigAudioSource.c:31` 的次序）。此时**共享表**对这个 cue 已在冷却窗口内。
        WFSoundPlayer.Play(cue, Vector3.zero, true);

        bool ok = WindowSounds.PlayFx(WindowSounds.DefaultButtonCue, "B120_W5b.H1");
        bool sameTable = !WFSoundPlayer.CanPlayByCooldown(cue);   // 刚写的那一戳还在共享表里（被挡时不写戳）
        Check(!ok && WindowSounds.Played == 0 && WindowSounds.Throttled == 1 && sameTable,
              "H1 ★ **全项目一张冷却表**：`WFSoundPlayer.Play` 刚写过戳 ⇒ 同一个 cue 走 `WindowSounds.PlayFx` 被**同一张表**挡掉"
            + "（原版 `lastTimePlayed` 只住在 cue 资产上，与从哪条路播无关）"
            + $"（实得 ok={ok} · WindowSounds.Played={WindowSounds.Played} · WindowSounds.Throttled={WindowSounds.Throttled} · 共享表仍挡={sameTable}）"
            + "｜判据 = `AudioCue__ConfigAudioSource.c:19-21`（键 = cue 上的 `lastTimePlayed`）+ `:31` 先写戳、后挑 clip"
            + "｜🧨 让 `WindowSounds` 再自持一张表 / 走不判冷却的口绕开共享表 ⇒ ok 变 true、Throttled 变 0 ⇒ 本条红（同一条 cue 两条路两只窗口）");

        // ---- H2 🧨 反向：本类那一层**必须还在**（只判一次 ≠ 一次都不判）----
        WindowSounds.ResetCounters();
        WFSoundPlayer.ResetThrottle();
        bool first = WindowSounds.PlayFx(WindowSounds.DefaultButtonCue, "B120_W5b.H2");
        int thr1 = WindowSounds.Throttled;
        bool second = WindowSounds.PlayFx(WindowSounds.DefaultButtonCue, "B120_W5b.H2");
        Check(first && !second && thr1 == 0 && WindowSounds.Throttled == 1,
              $"H2 ★ 本类自己的冷却**仍在**（同一 cue 在 `timeToPlayAgain` 内连播两次 ⇒ 第二次被挡）"
            + $"（实得 first={first} / second={second} / Throttled={WindowSounds.Throttled}）"
            + "｜🧨 把冷却整段删掉 ⇒ 第二次照播 ⇒ 本条红（与 H1 构成反相：不能既「全走共享表」又「零层」）");
    }

    // ==================================================================
    //  I · `ResetThrottle` 的调用链（`BattleSfx.ResetCounters()` 要把三张表都清掉）
    // ==================================================================
    static void GroupI_ResetThrottleChain()
    {
        WindowSounds.ResetCounters();
        WFSoundPlayer.ResetThrottle();
        WindowSounds.PlayFx(WindowSounds.DefaultButtonCue, "B120_W5b.I0");
        WindowSounds.PlayFx(WindowSounds.DefaultButtonCue, "B120_W5b.I0");   // 被本类挡一次
        int thrBefore = WindowSounds.Throttled;

        BattleSfx.ResetCounters();                                          // 应当把 ③ 那张表也清掉
        bool again = WindowSounds.PlayFx(WindowSounds.DefaultButtonCue, "B120_W5b.I1");
        Check(again && WindowSounds.Throttled == thrBefore,
              $"I1 ★ `BattleSfx.ResetCounters()` 也清**壳侧**那张节流表（③）⇒ 清完立刻能再播"
            + $"（实得 again={again} · Throttled {thrBefore}→{WindowSounds.Throttled}）"
            + "｜判据 = 本方法自己的契约「把**全部读口/节流表**清零」"
            + "｜🧨 去掉那一句 `WindowSounds.ResetThrottle()` ⇒ 本条红");

        string src = Application.dataPath + "/CardPresentation/Battle/BattleSfx.cs";
        bool hasCall = false;
        if (!System.IO.File.Exists(src))
            Check(false, "I2 前提：读得到 `Battle/BattleSfx.cs`（" + src + "）—— 读不到时 I2 等于没验 ⇒ 当场红");
        else
        {
            foreach (var raw in System.IO.File.ReadAllLines(src))
            {
                var t = raw.TrimStart();
                if (t.StartsWith("//")) continue;
                if (t.IndexOf("WindowSounds.ResetThrottle()", StringComparison.Ordinal) >= 0) hasCall = true;
            }
            Check(hasCall, "I2 ★ 调用链写明在 `ResetCounters` 里：`BattleSfx` → {WFSoundPlayer ①④ · RemnantSfx ② · WindowSounds ③}"
                         + "｜🧨 删掉那一句（I1 也会红）⇒ 本条红");
        }
    }

    // ==================================================================
    //  J · ②③ 的键空间 + 主表 #26 的音量路由
    // ==================================================================
    static void GroupJ_KeyAndRouting()
    {
        var rt = FieldTypeOf(typeof(RemnantSfx), "_lastAt");
        Check(rt == typeof(System.Collections.Generic.Dictionary<int, float>),
              "J1 ★ `RemnantSfx` 的节流键 = **值类型身份**（`(阵营, 时机)` 的 int），**不再是字符串**"
            + $"（实得 {TypeName(rt)}）"
            + "｜原版键 = `AudioCue` 资产实例；那 6 条 remnant cue 不在 `animfx_sounds.json` 里（现读 513 条零命中）⇒ "
            + "拿不到 cue 实例，本 build 用一一对应的 `(阵营, 时机)` 作等价身份"
            + "｜🧨 改回 `Dictionary<string,float>` ⇒ 本条红");

        var wt = FieldTypeOf(typeof(WarpforgeVFX.WFSoundPlayer), "_lastAt");   // 2026-11-05：`WindowSounds._lastAt` 已删，并进共享表
        Check(wt == typeof(System.Collections.Generic.Dictionary<WFSoundCue, float>),
              "J2 ★ `WindowSounds` 的节流键 = **`WFSoundCue` 资产实例** —— 已并进 `WFSoundPlayer._lastAt` 那张**全项目唯一**的冷却表（原版 `lastTimePlayed` 住在 `AudioCue` 上）"
            + $"（实得 {TypeName(wt)}）｜🧨 改回按 cue 名（string）⇒ 本条红（或让 `WindowSounds` 再自持一张表 ⇒ 本条找不到共享表/类型不符）");

        // #26：残骸音效走 **FX 混音组**，不再在代码里乘滑块
        string src = Application.dataPath + "/CardPresentation/Battle/RemnantSfx.cs";
        if (!System.IO.File.Exists(src))
        {
            Check(false, "J3 前提：读得到 `Battle/RemnantSfx.cs`（" + src + "）—— 读不到时 J3 等于没验 ⇒ 当场红");
            return;
        }
        bool hasGroup = false, hasSliderMul = false;
        foreach (var raw in System.IO.File.ReadAllLines(src))
        {
            var t = raw.TrimStart();
            if (t.StartsWith("//")) continue;
            if (t.IndexOf("outputAudioMixerGroup = fxGroup", StringComparison.Ordinal) >= 0) hasGroup = true;
            if (t.IndexOf("WarpforgeAudio.SoundFx * vol", StringComparison.Ordinal) >= 0) hasSliderMul = true;
        }
        Check(hasGroup && !hasSliderMul,
              "J3 ★ **主表 #26**：残骸音源挂 **FX 混音组**、`PlayOneShot` 只传 cue 的随机音量"
            + "（判据 = `AudioCue__Play3D.c` → `SoundManager__Play3D.c` 默认 `MixerType.FX = 0` → "
            + "`SoundManager__ConfigureAudioSourceAndPlay.c` 的 `param_7 == 0` 取 `SoundManager + 0x48`；"
            + "音量 = `AudioCue__ConfigAudioSource.c` 只写 `Random.Range(minVolume,maxVolume)`）"
            + $"（实得 hasGroup={hasGroup} · 仍在代码里乘滑块={hasSliderMul}）"
            + "｜🧨 改回 `PlayOneShot(clip, WarpforgeAudio.SoundFx * vol)` ⇒ 本条红（绕过 FX 组、不吃 ToggleSFX 的 duck）");
    }

    /// <summary>取私有静态字段的**类型**（判「键空间换没换」用；比字符串扫描可靠）。</summary>
    static Type FieldTypeOf(Type t, string name)
    {
        var f = t.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        return f != null ? f.FieldType : null;
    }

    static string TypeName(Type t) { return t != null ? t.Name : "（找不到字段）"; }
}
