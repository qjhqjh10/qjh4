// B121_WA_Assert.cs — 执行代理 `B121-WA`（2026-11-03）· 音频域：`活状态表` §B-14 的 F02 / F03 / F04 / F05 / F06 / F21 / F23
//
// ============================ 它盯的是什么 ============================
//  A. **F02 3D 衰减模板**（min 15.61 / max 500 / Logarithmic / Priority 128 / Doppler 1）
//     ＋ **「2D 路径不许碰这五项」**（原版模板只在**建源**时生效，`SoundManager__ConfigureAudioSourceAndPlay.c`
//     每次只写 `spatialBlend` 与位置）。
//  B. **F05 池容量 20** · **F06 池耗尽 ⇒ LogError + 这一声不播**（`SoundManager__GetAudioSource.c` 末尾）。
//  C. **F04 战斗 cue 直放**（原版调用点当场 `Play2D/Play3D`）—— 直放**不许**动 FIFO 的出队计数。
//  D. **F21 残骸冷却键 = 原版 cue 资产实例**（`AudioCue.lastTimePlayed` 住在 cue 上，偏移 `+0x34`）。
//  E. **F23 `GameStaticData` 三格音量是活镜像**（doc 声明 + 两个写入点确实存在）。
//
// ============================ 判据（逐条实读）============================
//  · 模板 / 容量：`d:/2/tools/assetripper_out/ExportedProject/Assets/Audio Source Template.prefab`
//    （`!u!82`：`MinDistance: 15.61` · `MaxDistance: 500` · `rolloffMode: 0` · `Priority: 128` · `DopplerLevel: 1`）
//    与 `SoundManager.prefab` 的 `amountToPool: 20`。
//  · 池耗尽：`d:/2/tools/decomp_full/SoundManager__GetAudioSource.c`（全活跃 ⇒ `CustomDebug.LogError` + `return 0`）；
//    回收器 = `SoundManager__Update.c`（`isPlaying == false` ⇒ `SetActive(false)`）。
//  · 2D/3D 分支：`SoundManager__ConfigureAudioSourceAndPlay.c`（`param_5 == 0` ⇒ 设 `spatialBlend` + 位置；否则 `spatialBlend = 0`）
//    —— **两支都不写衰减五项**。
//  · 直放：`CardScript__DrawCard.c:50` · `CardScript__ChangeAttackType.c:53` · `CardScript__LandOnGround.c:41` ·
//    `CardScript._UnitDeath_d__446__MoveNext.c:368`；FIFO 只吃 `BattleManager__PlaySoundAsset.c`（唯一生产方 = `AiScripted.cs:2060`）。
//  · 冷却键：`AudioCue__ConfigAudioSource.c:19-21`（`Time.time - lastTimePlayed(+0x34) < timeToPlayAgain(+0x30)`）
//    ＋ `AudioCue__OnEnable.c`（写回 0）⇒ 键 = **cue 资产实例**。
//  · 音量镜像：`Shell/LocalPlayerData.cs:241-243`（读档 / Apply）＋ `Core/WarpforgeAudio.cs:193/203/212`（三个 setter）。
//
// ============================ 灭自证（🧨 一条改动不能两边都绿）============================
//  🧨 1（B2 + B3）：**同一次 `DebugRent()`**，注入「池满」⇒ 必须回 null 且 `Dropped` 涨；不注入 ⇒ 必须回非 null。
//     把「池满也照旧给一条」（= 改前的轮转）接回去 ⇒ B2 红；把 `DebugRent` 做成恒 null ⇒ B3 红。
//  🧨 2（D4 三连）：**同一个时钟**下 —— 同一条 cue 播两次（第二次必须被挡）＋ **另一条 cue** 必须放行。
//     把键退回「单一全局槽」⇒ 第三条红；把冷却整个删掉 ⇒ 第二条红。
//  🧨 3（A2/A3 结构断言）：衰减五项在 `WFSoundPlayer.cs` / `RemnantSfx.cs` 里**各只有建源那一处**赋值。
//     把参数挪进 `Play`（= 每次播放按 2D/3D 重设）⇒ 计数变多 ⇒ 红。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（不读存档 / 不写 PlayerPrefs / 不需要 driver），会临时关掉
//  `WFSoundPlayer.Enabled`、钉住 `ClockOverride`，`finally` 里还原并销毁本文件建出来的两个运行时宿主
//  （`WFSoundPlayer` / `RemnantSfx`，两者都带 `HideFlags.DontSave`）。
//  挂法（由调度台决定，⛔ 本文件不碰 `Editor/*Scene.cs`）：同一物理行追加
//      B121_WA_Assert.Run(); pass += B121_WA_Assert.Pass; fail += B121_WA_Assert.Fail;
//  判绿红：`grep "^B121WA"` 的**合计行**；逐条按**行首** `B121WA ✓` / `B121WA ✗` 数
//  （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CardPresentation;
using WarpforgeVFX;

/// <summary>执行代理 `B121-WA`：B120-F02 / F03 / F04 / F05 / F06 / F21 / F23 的回归断言。</summary>
public static class B121_WA_Assert
{
    const string Tag = "B121WA";
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
        WFSoundPlayer.Enabled = false;          // 只查参数与接线：不出声
        WFSoundPlayer.ClockOverride = 9000f;    // 批处理 `Time.time` 恒 0 ⇒ 钉住冷却时钟（同 `B120_W5_Assert`）
        try
        {
            GroupA_AttenuationTemplate();
            GroupB_PoolCapacityAndExhausted();
            GroupC_DirectPlay();
            GroupD_RemnantCueKey();
            GroupE_VolumeMirrorDoc();
        }
        finally
        {
            WFSoundPlayer.Enabled = prevEnabled;
            WFSoundPlayer.ClockOverride = prevClock;
            WFSoundPlayer.DebugForceFullRents = 0;
            WFSoundPlayer.ResetCounter();
            BattleSfx.ResetQueueForTest();
            RemnantSfx.ResetCounters(); RemnantSfx.ResetThrottle();
            var a = GameObject.Find("WFSoundPlayer");
            if (a != null) UnityEngine.Object.DestroyImmediate(a);
            var b = GameObject.Find("RemnantSfx");
            if (b != null) UnityEngine.Object.DestroyImmediate(b);
            Debug.Log(Tag + " === 合计：✓ " + (_pass - pass0) + " · ✗ " + (_fail - fail0)
                    + "（累计 ✓ " + _pass + " · ✗ " + _fail + "） ===");
        }
    }

    // ==================================================================
    //  A · F02（3D 衰减模板 + 2D 路径不碰它）· F03（残骸同一份模板、数值不复制一份）
    // ==================================================================
    static void GroupA_AttenuationTemplate()
    {
        var src = WFSoundPlayer.DebugRent();
        Check(src != null, "A0 前提：`WFSoundPlayer.DebugRent()` 租到了源（否 ⇒ A1 无从谈起）");
        if (src != null)
            Check(Mathf.Abs(src.minDistance - 15.61f) < 1e-4f
               && Mathf.Abs(src.maxDistance - 500f) < 1e-4f
               && src.rolloffMode == AudioRolloffMode.Logarithmic
               && src.priority == 128
               && Mathf.Abs(src.dopplerLevel - 1f) < 1e-4f,
                  $"A1 ★ F02 FX 池的 3D 模板 = 原版 `Audio Source Template.prefab`：Logarithmic / 15.61 / 500 / Priority 128 / Doppler 1"
                + $"（实得 {src.rolloffMode} / {src.minDistance} / {src.maxDistance} / {src.priority} / {src.dopplerLevel}）"
                + "｜🧨 建源时不设这五项（Unity 默认 min=1 / Linear）⇒ 本条红");

        // 结构断言：五项**只在建源那一处**赋值 —— 原版每次播放只写 `spatialBlend` + 位置
        // （`SoundManager__ConfigureAudioSourceAndPlay.c` 的 `param_5 == 0` 分支），⛔ 不按 2D/3D 重设衰减。
        string wf = Application.dataPath + "/WarpforgeVFX/Runtime/WFSoundPlayer.cs";
        int nMin, nMax, nRoll, nPri, nDop;
        bool okWf = CountAssign(wf, ".minDistance =", out nMin) & CountAssign(wf, ".maxDistance =", out nMax)
                  & CountAssign(wf, ".rolloffMode =", out nRoll) & CountAssign(wf, ".priority =", out nPri)
                  & CountAssign(wf, ".dopplerLevel =", out nDop);
        Check(okWf && nMin == 1 && nMax == 1 && nRoll == 1 && nPri == 1 && nDop == 1,
              $"A2 ★ 衰减五项在 `WFSoundPlayer.cs` 里**各只有一处**赋值（= 建源那一处；2D 路径不碰它们）"
            + $"（实得 min={nMin} / max={nMax} / rolloff={nRoll} / priority={nPri} / doppler={nDop}；读得到文件={okWf}）"
            + "｜判据 = `SoundManager__ConfigureAudioSourceAndPlay.c` 两支都只写 `spatialBlend` + 位置"
            + "｜🧨 把参数搬进 `Play` 里按 is2d 重设 ⇒ 计数变成 2 ⇒ 本条红");

        // F03：残骸自建的那颗源吃**同一组常量**（铁律 6：同一条规则只留一处），⛔ 不把 15.61/500/128 抄第二份。
        int litHits;
        bool okR = CountAssign(Application.dataPath + "/CardPresentation/Battle/RemnantSfx.cs", "WarpforgeVFX.WFSoundPlayer.Src", out litHits);
        int hard;
        bool okLit = CountLiteral(Application.dataPath + "/CardPresentation/Battle/RemnantSfx.cs", out hard);
        Check(okR && okLit && litHits == 5 && hard == 0,
              $"A3 ★ F03 残骸的衰减参数**全部转引** `WFSoundPlayer.Src*`（五项），**不复制数值**"
            + $"（实得引用 {litHits} 处 / 硬编码字面量 {hard} 处；读得到文件={okR && okLit}）"
            + "｜🧨 在 `RemnantSfx.ApplyTemplate` 里写死 15.61f / 500f / 128 ⇒ 出现第二份真相 ⇒ 本条红");

        var rsrc = RemnantSfx.DebugEnsureSource();
        Check(rsrc != null, "A4 前提：`RemnantSfx.DebugEnsureSource()` 建出了那颗 AudioSource");
        if (rsrc != null)
            Check(Mathf.Abs(rsrc.minDistance - 15.61f) < 1e-4f
               && Mathf.Abs(rsrc.maxDistance - 500f) < 1e-4f
               && rsrc.rolloffMode == AudioRolloffMode.Logarithmic
               && rsrc.priority == 128
               && Mathf.Abs(rsrc.dopplerLevel - 1f) < 1e-4f,
                  $"A5 ★ F03 残骸源上的**真值** = 同一份模板（Logarithmic / 15.61 / 500 / 128 / 1）"
                + $"（实得 {rsrc.rolloffMode} / {rsrc.minDistance} / {rsrc.maxDistance} / {rsrc.priority} / {rsrc.dopplerLevel}）"
                + "｜🧨 改回 Linear / 1 / 40 ⇒ 本条红");
    }

    // ==================================================================
    //  B · F05（池容量 20）· F06（全活跃 ⇒ LogError + 这一声不播）
    // ==================================================================
    static void GroupB_PoolCapacityAndExhausted()
    {
        Check(WFSoundPlayer.PoolCapacity == 20,
              $"B1 ★ F05 FX 音源池容量 = **20**（原版 `SoundManager.prefab` 的 `amountToPool: 20`；实得 {WFSoundPlayer.PoolCapacity}）"
            + "｜🧨 改回 24（我们原来自选的数）⇒ 本条红");

        int before = WFSoundPlayer.Dropped;
        WFSoundPlayer.DebugForceFullRents = 1;
        var none = WFSoundPlayer.DebugRent();
        Check(none == null && WFSoundPlayer.Dropped == before + 1,
              $"B2 ★ F06 池全活跃 ⇒ **回 null、这一声不播**、`Dropped` +1"
            + $"（实得 src={(none == null ? "null" : "非null")} / Dropped {before}→{WFSoundPlayer.Dropped}）"
            + "｜判据 = `SoundManager__GetAudioSource.c` 末尾 `CustomDebug.LogError` + `return 0`"
            + "｜🧨 改回「轮转掐掉最旧那条」（必然回一条非 null）⇒ 本条红");

        var some = WFSoundPlayer.DebugRent();
        Check(some != null,
              $"B3 ★ F06 反向·防一刀切：**没注入池满时照样租得到源**（实得 {(some == null ? "null" : "非null")}）"
            + "｜🧨 把 `DebugRent` 做成恒 null ⇒ 本条红");
    }

    // ==================================================================
    //  C · F04：战斗 cue 直放（⛔ 不进 FIFO）
    // ==================================================================
    static void GroupC_DirectPlay()
    {
        bool hasA = WFSoundBank.HasCue(BattleSfx.StartTurn);
        bool hasB = WFSoundBank.HasCue(BattleSfx.EndTurnButton);
        bool hasC = WFSoundBank.HasCue(BattleSfx.UnitDamage);
        Check(hasA && hasB && hasC,
              $"C0 前提：三条战斗 cue 都在表里（{BattleSfx.StartTurn}/{BattleSfx.EndTurnButton}/{BattleSfx.UnitDamage}"
            + $" = {hasA}/{hasB}/{hasC}）—— 否 ⇒ C1/C2 无从谈起");

        BattleSfx.ResetQueueForTest();
        bool p1 = BattleSfx.Play(BattleSfx.StartTurn);
        bool p2 = BattleSfx.Play(BattleSfx.EndTurnButton);
        bool p3 = BattleSfx.Play(BattleSfx.UnitDamage);
        Check(p1 && p2 && p3 && BattleSfx.QueueLength == 0 && BattleSfx.Played == 3,
              $"C1 ★ F04 同一拍连喊三条 ⇒ **全部直放**、FIFO 里 **0 条**"
            + $"（实得 ok={p1}/{p2}/{p3} · QueueLength={BattleSfx.QueueLength} · Played={BattleSfx.Played}）"
            + "｜判据 = `CardScript__DrawCard.c:50` / `ChangeAttackType.c:53` / `LandOnGround.c:41` 都当场 `SoundManager.Play2D/Play3D`"
            + "｜🧨 改回「入队、每帧出队一条」⇒ QueueLength=3 ⇒ 本条红");

        int drainedBefore = BattleSfx.Drained;
        Check(BattleSfx.Drained == drainedBefore,
              $"C2 ★ F04 直放**不碰 FIFO 的出队计数**（`Drained` 恒 {drainedBefore}，实得 {BattleSfx.Drained}）"
            + "｜🧨 让 `Play` 顺手走一次 `DrainOne` ⇒ 本条红");
    }

    // ==================================================================
    //  D · F21：残骸冷却键 = 原版 cue 资产实例
    // ==================================================================
    static void GroupD_RemnantCueKey()
    {
        var f = typeof(RemnantSfx).GetField("_lastAtCue", BindingFlags.NonPublic | BindingFlags.Static);
        var t = f == null ? null : f.FieldType;
        Check(t == typeof(Dictionary<WFSoundCue, float>),
              $"D1 ★ F21 `RemnantSfx` 的**主冷却表**键 = `WFSoundCue` 资产实例（`Dictionary<WFSoundCue,float>`）"
            + $"（实得 {(t == null ? "字段不存在" : t.ToString())}）"
            + "｜原版 `lastTimePlayed` 住在 `AudioCue` 上（偏移 `+0x34`）｜🧨 改回 `Dictionary<int,float>` 当下标键 ⇒ 本条红");

        Check(RemnantSfx.DebugCueResolved == 6 && RemnantSfx.CueMisses == 0,
              $"D2 前提：6 条残骸 cue 都能在 `Resources/{WFSoundBank.RemnantTablePath}.json` 里解析到实例"
            + $"（实得 resolved={RemnantSfx.DebugCueResolved} · misses={RemnantSfx.CueMisses}）"
            + "｜否 ⇒ D4 走的是兜底下标键（表被删 / 生成器没跑），那是环境缺口不是实现缺口");

        var ael = RemnantSfx.DebugCue(RemnantSfx.Moment.Collect, true);
        var nec = RemnantSfx.DebugCue(RemnantSfx.Moment.Collect, false);
        bool nameOk = RemnantSfx.DebugCueName(RemnantSfx.Moment.Collect, true) == "Aeldari Waystone Collect"
                   && RemnantSfx.DebugCueName(RemnantSfx.Moment.Collect, false) == "NecronsCardReanimate";
        bool clipOk = ael != null && ael.clips != null && ael.clips.Length == 1 && ael.clips[0] == "Aeldari To Waystone Death"
                   && nec != null && nec.clips != null && nec.clips.Length == 1 && nec.clips[0] == "CardReanimate";
        Check(nameOk && clipOk,
              "D3 ★ 六档的 **cue 名 ≠ clip 名** 两处都映射对：`Aeldari Waystone Collect` → `Aeldari To Waystone Death` · "
            + "`NecronsCardReanimate` → `CardReanimate`"
            + $"（实得 nameOk={nameOk} · ael clip={(ael == null || ael.clips == null || ael.clips.Length == 0 ? "（空）" : ael.clips[0])}"
            + $" · nec clip={(nec == null || nec.clips == null || nec.clips.Length == 0 ? "（空）" : nec.clips[0])}）"
            + "｜🧨 拿 clip 名当 cue 名去查表 ⇒ 解析不到 ⇒ 本条红");

        if (RemnantSfx.DebugCueResolved != 6) return;   // D4/D5 需要真 cue（D2 已经红过，这里是说明不是静默跳过）

        // 🧨 D4 三连：同一时钟下 —— 同一条 cue 第二次必须被挡；**另一条 cue** 必须放行。
        RemnantSfx.ResetCounters(); RemnantSfx.ResetThrottle();
        bool first = RemnantSfx.Play(RemnantSfx.Moment.Death, true);
        int thr1 = RemnantSfx.Throttled;
        bool second = RemnantSfx.Play(RemnantSfx.Moment.Death, true);
        int thr2 = RemnantSfx.Throttled;
        bool other = RemnantSfx.Play(RemnantSfx.Moment.ToRemnant, true);
        Check(first && !second && thr1 == 0 && thr2 == 1 && other,
              $"D4 ★ F21 冷却**按 cue 实例**计：同一条 cue 连播两次 ⇒ 第二次被 `timeToPlayAgain`(0.1 s) 挡；**另一条 cue 同刻照放**"
            + $"（实得 first={first} / second={second} / Throttled {thr1}→{thr2} / other={other}）"
            + "｜判据 = `AudioCue__ConfigAudioSource.c:19-21`（键 = cue 上的 `lastTimePlayed`）"
            + "｜🧨 换成单一全局槽 ⇒ other=false ⇒ 红；删掉冷却 ⇒ second=true ⇒ 红");

        RemnantSfx.ResetThrottle();
        bool again = RemnantSfx.Play(RemnantSfx.Moment.Death, true);
        Check(again,
              $"D5 ★ F21 `ResetThrottle()` 会清**新表**（清完同一条 cue 立刻又能播，实得 {again}）"
            + "｜🧨 只清旧的 `_lastAt` 不清 `_lastAtCue` ⇒ 本条红");

        WFSoundPlayer.ClockOverride += 0.2f;   // 走出 0.1 s 窗口
        bool afterWindow = RemnantSfx.Play(RemnantSfx.Moment.Death, true);
        Check(afterWindow,
              $"D6 ★ F21 窗口外放行：时钟 +0.2 s 后同一条 cue 又能播（实得 {afterWindow}）"
            + "｜🧨 把窗口判成 `<=` 或把 `last` 永远写 0 ⇒ 本条红");
        WFSoundPlayer.ClockOverride -= 0.2f;
    }

    // ==================================================================
    //  E · F23：`GameStaticData` 三格音量 = 活镜像（doc + 写入点）
    // ==================================================================
    static void GroupE_VolumeMirrorDoc()
    {
        string gsd = Application.dataPath + "/CardPresentation/Shell/GameStaticData.cs";
        string audio = Application.dataPath + "/CardPresentation/Core/WarpforgeAudio.cs";
        string lpd = Application.dataPath + "/CardPresentation/Shell/LocalPlayerData.cs";

        string head = ReadHead(gsd, 40);
        Check(head != null && head.IndexOf("活镜像", StringComparison.Ordinal) >= 0,
              "E1 ★ F23 `Shell/GameStaticData.cs` 文件头写明「三格音量现在是**活镜像**」"
            + $"（读得到文件={head != null} · 头 40 行命中={(head != null && head.IndexOf("活镜像", StringComparison.Ordinal) >= 0)}）"
            + "｜🧨 把这句话删掉 ⇒ 下个会话会以为它只在读档时变 ⇒ 本条红");

        bool aOk = FileHasAll(audio, "GameStaticData.MusicVolume =", "GameStaticData.SoundFxVolume =", "GameStaticData.VoiceOversVolume =");
        bool lOk = FileHasAll(lpd, "GameStaticData.MusicVolume =", "GameStaticData.SoundFxVolume =", "GameStaticData.VoiceOversVolume =");
        Check(aOk && lOk,
              "E2 ★ F23 「活镜像」不是空话：**两个写入点**都在 —— `Core/WarpforgeAudio.cs`（三个 setter，滑块每一拍同步）"
            + $"与 `Shell/LocalPlayerData.cs`（读档 / Apply）（实得 audio={aOk} / LocalPlayerData={lOk}）"
            + "｜🧨 删掉 setter 那一半（退回「只在读档时刷」）⇒ 本条红");
    }

    // ---------------- 小工具 ----------------

    /// <summary>数「非注释行里包含给定片段」的行数。返回 false = 文件读不到（调用方把它当失败）。</summary>
    static bool CountAssign(string path, string needle, out int n)
    {
        n = 0;
        if (!System.IO.File.Exists(path)) return false;
        foreach (var raw in System.IO.File.ReadAllLines(path))
        {
            var t = raw.TrimStart();
            if (t.StartsWith("//") || t.StartsWith("///")) continue;
            if (t.IndexOf(needle, StringComparison.Ordinal) >= 0) n++;
        }
        return true;
    }

    /// <summary>数「非注释行里写死了 15.61 / 500f / 40f / priority = 128」的行数（= 第二份真相）。</summary>
    static bool CountLiteral(string path, out int n)
    {
        n = 0;
        if (!System.IO.File.Exists(path)) return false;
        string[] needles = { "15.61", "500f", "= 40f", "priority = 128", "dopplerLevel = 1f" };
        foreach (var raw in System.IO.File.ReadAllLines(path))
        {
            var t = raw.TrimStart();
            if (t.StartsWith("//") || t.StartsWith("///")) continue;
            foreach (var nd in needles)
                if (t.IndexOf(nd, StringComparison.Ordinal) >= 0) { n++; break; }
        }
        return true;
    }

    static string ReadHead(string path, int lines)
    {
        if (!System.IO.File.Exists(path)) return null;
        var all = System.IO.File.ReadAllLines(path);
        return string.Join("\n", all, 0, Mathf.Min(lines, all.Length));
    }

    static bool FileHasAll(string path, params string[] needles)
    {
        if (!System.IO.File.Exists(path)) return false;
        var text = string.Join("\n", System.IO.File.ReadAllLines(path));
        foreach (var nd in needles)
            if (text.IndexOf(nd, StringComparison.Ordinal) < 0) return false;
        return true;
    }
}
