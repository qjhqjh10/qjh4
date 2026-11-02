// B120_W17_Assert.cs — 执行代理 `B120-W17`（2026-11-02）· 三条音效尾巴的回归断言
//
// ============================ 它盯的是什么 ============================
//  B120 战斗参数审计 F 域的**三条尾巴**（`报告/B120_F_音效语音BGM.md` / `报告/B120_W5_执行.md` §六·5 /
//  `报告/B120_W10_执行.md` §六）—— 本文件逐条配断言：
//    A. `B120-F15` **残骸那六条 cue 进 cue 表**：改前 6 个名字在 `animfx_sounds.json`（513 条）里**零命中**
//       ⇒ `RemnantSfx` 的冷却键只能到 `CueIndex`。现在由 `工具/import_remnant_sfx.py` 产出第三张表
//       `Resources/remnant_sounds.json`（`WFSoundTable` 同构），`WFSoundBank` 装进同一个字典。
//    B. `B120-F18` **FIFO 载荷 = 入队时已解好的 clip**：原版 `PlaySoundAsset` 压进 `soundsToPlay` 的是整个
//       `SoundAsset_OLD`（`+0x10 = clip` / `+0x18 = volume`），`PlayNextSoundInQueue` 出队时**只读它、不再查表**。
//       改前我们只存「字段名 + cue」，clip 等出队才由 `WFSoundPlayer` 随机挑。
//    C. `B120-F20` **`GameStaticData` 音量镜像随滑块更新**：设置窗拖滑块只写 mixer，镜像要等下次
//       `LocalPlayerData.Apply` 才刷 ⇒ 中间是旧值。原版口径 = **静态量是唯一真相**（见下面判据）。
//
// ============================ 判据（逐条实读，出处写在断言里）============================
//  · F15：`Assembly-CSharp/RemnantBody.cs:17,26`（`toRemnantSound` `+0x38` / `deathSound` `+0x50`）·
//    `RemnantAeldari` 的 `waystoneCollectSound`（`+0x70`）· `RemnantNecrons` 的 `reanimateSound`（`+0x90`）
//    —— 四格都是 `AudioCue`；取自 `d:/2/.../soundcollection_assets_all.bundle`，值见生成器输出。
//  · F18：`decomp_full/BattleManager__PlaySoundAsset.c:18-52`（`soundAsset != null && soundAsset.clip != null`
//    ⇒ 整个结构体 append 到 `+0x428`；**整支没有第二句**）＋ `BattleManager__PlayNextSoundInQueue.c:20-67`
//    （取 `[0]`、读它的 clip / volume、播不播都 `RemoveAt(0)`）+ 泵点 `BattleManager__Update.c:152`。
//  · F20：`out_cpp2il_dev_ilspy/MediaTab.cs:131-147`（三根滑块的 handler 都是
//    `GameStaticData.<X>Volume = newValue; PlayerDataManager.SetMusicAndFxVolumes();`）＋
//    `PlayerDataManager.cs:10438-10439`（`SetMixerVolume(FX, GameStaticData.SoundFxVolume)` /
//    `SetMixerVolume(Jingles, 同一个静态量)`）＋ `BattleSettingsWindow.cs:240`（开窗回填读那个静态量）
//    ⇒ **滑块写静态量、mixer 读静态量**。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 1（A1 逐条 + A2 两条「cue 名 ≠ clip 名」）：6 条 cue 逐条查表**且**比 clip 名 ——
//     把 `remnant_sounds` 那条装载删掉 ⇒ 6 条 A1 全红；把某条 cue 的 clip 换成自己的名字（凑数）⇒ A2 红。
//  🧨 2（B1 + B6）：B1 反射看 `Pending` 上有没有 `AudioClip clip` 字段；B6 是**源码面**的
//     「`Enqueue` 里挑 clip / `DrainOne` 里不挑」。把挑选挪回出队 ⇒ B4（入队后立刻读 `FrontClip`）与 B6 同时红；
//     只删字段不删调用 ⇒ 编译不过（结构上不可能「两边都绿」）。
//  🧨 3（C1 + C2）：**两个不同的值**（0.37 / 0.51）分别过一遍 —— 常亮常量过不了；C3 是源码面
//     （`SetSoundFx` 体内必须出现 `GameStaticData.SoundFxVolume`），删掉那一句 ⇒ C1/C2 红。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（不读存档 / 不写 `PlayerPrefs` / 不需要 driver）。
//  ⚠️ 它会临时关掉 `WFSoundPlayer.Enabled`（**不出声、不建音源池**，与 `B117_G10_12_Assert` 同一做法）、
//  钉住 `ClockOverride`，`finally` 里把三档音量与三格镜像**逐格还原**。
//  挂法（由调度台决定）：接在 `Editor/BattleScene.cs:46983` 那条**方法尾聚合行**里，紧跟
//  `B120_W13_Assert.Run(); pass += B120_W13_Assert.Pass; fail += B120_W13_Assert.Fail;` 之后
//  （同一物理行追加 ⇒ 行数零变化）：
//      B120_W17_Assert.Run(); pass += B120_W17_Assert.Pass; fail += B120_W17_Assert.Fail;
//  判绿红：`grep "^B120W17"` 的**合计行**；逐条按**行首** `B120W17 ✓` / `B120W17 ✗` 数
//  （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// ⚠️ 批处理下**没有声卡**（`AudioSetup.PlayCheck` 明确要 Play 模式）⇒ B 组只验「载荷里有没有那条 clip」，
//   ⛔ 不写「跑不出声就失败」那一类断言。B3/B4 需要 `Resources/Art/audio/sfx/StartTurn.wav`
//   （`Resources/Art/` 是 `.gitignore` 的；缺了跑 `python 工具/import_battle_sfx.py`）。

using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using CardPresentation;
using WarpforgeVFX;

/// <summary>执行代理 `B120_W17`：F15 / F18 / F20 三条音效尾巴的回归断言。</summary>
public static class B120_W17_Assert
{
    const string Tag = "B120W17";
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

    /// <summary>残骸那六条 cue 的**原版资产名**（逐字照抄，含原版自己的空格）。</summary>
    static readonly string[] RemnantCues =
    {
        "Aeldari To Waystone", "Aeldari Waystone Collect", "Aeldari Waystone Destruction",
        "CardShatter", "NecronsCardReanimate", "RemnantsDestroyed",
    };

    public static void Run()
    {
        int pass0 = _pass, fail0 = _fail;
        bool prevEnabled = WFSoundPlayer.Enabled;
        float prevClock = WFSoundPlayer.ClockOverride;
        float fx0 = WarpforgeAudio.SoundFx, vo0 = WarpforgeAudio.VoiceOver;
        float fxM0 = GameStaticData.SoundFxVolume, voM0 = GameStaticData.VoiceOversVolume;
        WFSoundPlayer.Enabled = false;          // 批处理不出声、不建音源池（同 `B117_G10_12_Assert`）
        WFSoundPlayer.ClockOverride = 7000f;    // `Time.time` 恒 0 ⇒ 钉住冷却时钟（同 `B117_G10_12_Assert` / `B120_W5_Assert`）
        try
        {
            GroupA_RemnantCueTable();
            GroupB_FifoPayloadHoldsClip();
            GroupC_VolumeMirrorSync();
        }
        finally
        {
            WarpforgeAudio.SetSoundFx(fx0);     // 不带 PlayerPrefs 副作用（音效/语音都不落盘）
            WarpforgeAudio.SetVoiceOver(vo0);
            GameStaticData.SoundFxVolume = fxM0;
            GameStaticData.VoiceOversVolume = voM0;
            WFSoundPlayer.Enabled = prevEnabled;
            WFSoundPlayer.ClockOverride = prevClock;
            BattleSfx.ResetQueueForTest();      // 队列 + 时钟 + 全部读口
            // 本文件不建音源（`Enabled=false` ⇒ `WFSoundPlayer.Play` 直接返回），但兜底一次：
            // 别的宿主可能已经把池建出来，别让它挂到后面的断言上。
            var go = GameObject.Find("WFSoundPlayer");
            if (go != null) UnityEngine.Object.DestroyImmediate(go);
            Debug.Log(Tag + " === 合计：✓ " + (_pass - pass0) + " · ✗ " + (_fail - fail0)
                    + "（累计 ✓ " + _pass + " · ✗ " + _fail + "） ===");
        }
    }

    // ==================================================================
    //  A · `B120-F15`：残骸六条 cue 进了 cue 表
    // ==================================================================
    static void GroupA_RemnantCueTable()
    {
        Check(WFSoundBank.Loaded, "A0 前提：`WFSoundBank` 装起来了（表都在 `Resources/` 下）");

        // A1 逐条：6 条 cue 都查得到，且 clips 非空
        foreach (var name in RemnantCues)
        {
            bool has = WFSoundBank.HasCue(name);
            Check(has, $"A1 残骸 cue `{name}` 在 cue 表里"
                    + "｜🧨 删掉 `WFSoundBank` 里 `RemnantTablePath` 那条装载（或删掉 `Resources/remnant_sounds.json`）⇒ 本行红");
            if (!has) continue;
            WFSoundCue cue = null;
            WFSoundBank.TryGetCue(name, out cue);
            Check(cue != null && cue.clips != null && cue.clips.Length > 0 && !string.IsNullOrEmpty(cue.clips[0]),
                  $"A1b `{name}` 的 `clips[]` 非空（实得 {(cue == null || cue.clips == null ? "null" : string.Join("/", cue.clips))}）");
            Check(cue != null && Mathf.Abs(cue.timeToPlayAgain - 0.1f) < 1e-3f,
                  $"A1c `{name}` 的 `timeToPlayAgain` = 0.1 s（原版 `AudioCue` ctor 默认；现读 "
                + (cue == null ? "?" : cue.timeToPlayAgain.ToString("F3")) + "）");
        }

        // A2 灭自证：两条**cue 名 ≠ clip 名**的（凑数改名过不了这一关）
        WFSoundCue shatter = null, reanimate = null, collect = null;
        WFSoundBank.TryGetCue("CardShatter", out shatter);
        WFSoundBank.TryGetCue("NecronsCardReanimate", out reanimate);
        WFSoundBank.TryGetCue("Aeldari Waystone Collect", out collect);
        Check(reanimate != null && reanimate.clips != null && reanimate.clips.Length > 0
              && reanimate.clips[0] == "CardReanimate",
              "A2 ★ `NecronsCardReanimate` → clip **`CardReanimate`**（cue 名 ≠ clip 名；"
            + "实得 " + (reanimate == null || reanimate.clips == null ? "null" : string.Join("/", reanimate.clips)) + "）"
            + "｜🧨 把表手改 / 按 cue 名猜 clip ⇒ 本行红");
        Check(collect != null && collect.clips != null && collect.clips.Length > 0
              && collect.clips[0] == "Aeldari To Waystone Death",
              "A2b `Aeldari Waystone Collect` → clip **`Aeldari To Waystone Death`**（灵族「出现」与「收集」共用同一个 clip）"
            + "｜实得 " + (collect == null || collect.clips == null ? "null" : string.Join("/", collect.clips)));
        Check(shatter != null && shatter.clips != null && shatter.clips.Length > 0 && shatter.clips[0] == "CardShatter",
              "A2c `CardShatter` → clip `CardShatter`（死灵「出现」那一档）");

        // A3 旧那条路仍在：`RemnantSfx` 读的 (阵营, 时机) 那张表必须还是 6 条
        //    （`Resources/Art/` 是 gitignore 的 ⇒ 缺失时这条红，文案里给出补法）
        Check(RemnantSfx.PropsLoaded == 6,
              $"A3 前提：`remnant_cue_props.json` 6 条随机区间都读得到（实得 {RemnantSfx.PropsLoaded}）"
            + " —— 缺了跑 `python 工具/import_remnant_sfx.py --write`");

        // A4 生成器纪律（源码面）：写盘必须显式 `--write`，裸跑 = 用法 + rc 2
        string gen = ReadProjectFile("../../工具/import_remnant_sfx.py");
        Check(gen != null, "A4 前提：读得到 `工具/import_remnant_sfx.py`（读不到时本组源码面等于没验 ⇒ 当场红）");
        if (gen != null)
        {
            Check(gen.Contains("\"--write\"") && gen.Contains("\"--dry\"") && gen.Contains("\"--check\""),
                  "A4 ★ 生成器有 `--check` / `--write` / `--dry` 三个显式动作"
                + "｜🧨 退回「裸跑就写盘」⇒ 本行红（先例：`B115_W10` 的 `--help` 被当成真合并）");
            Check(gen.Contains("return 0 if a.help else 2"),
                  "A4b ★ 裸跑 ⇒ 打印用法 + **rc 2**（`return 0 if a.help else 2`）");
            Check(gen.Contains("remnant_sounds.json"),
                  "A4c 生成器写的就是 `Resources/remnant_sounds.json`（表名与 `WFSoundBank.RemnantTablePath` 对得上）");
        }
        string bank = ReadProjectFile("WarpforgeVFX/Runtime/WFSoundBank.cs");
        Check(bank != null && bank.Contains("RemnantTablePath"), "A5 前提：读得到 `WFSoundBank.cs` 且它有 `RemnantTablePath`");
        if (bank != null)
            Check(bank.Contains("TablePath, BattleTablePath, RemnantTablePath"),
                  "A5 ★ `WFSoundBank.Ensure()` 把**三张表**装进同一个字典（改回两张 ⇒ 本行红）");
    }

    // ==================================================================
    //  B · `B120-F18`：FIFO 载荷 = 入队时已解好的 clip
    // ==================================================================
    static void GroupB_FifoPayloadHoldsClip()
    {
        // B1 结构（不需要任何音频资产）：`Pending` 上有一格 `AudioClip clip`
        var t = typeof(BattleSfx).GetNestedType("Pending", BindingFlags.NonPublic);
        Check(t != null, "B1 前提：`BattleSfx.Pending` 这个嵌套结构还在（改名字 ⇒ 当场红，否则下面没验到东西）");
        var f = t != null ? t.GetField("clip", BindingFlags.Public | BindingFlags.Instance) : null;
        Check(f != null && f.FieldType == typeof(AudioClip),
              "B1 ★ ★ `Pending.clip` 是 **`AudioClip`** 字段（= 原版 `SoundAsset_OLD` 的 `+0x10`）"
            + $"｜实得 {(f == null ? "没有这个字段" : f.FieldType.Name)}"
            + "｜🧨 改回「只存字段名 + cue」⇒ 本行红");

        // B2/B3 前提：空队列的读口是 null；`StartTurn` 那条 clip 真的载得到
        BattleSfx.ResetQueueForTest();
        Check(BattleSfx.FrontClip == null && BattleSfx.QueueLength == 0,
              $"B2 前提：空队列 `FrontClip` = null、`QueueLength` = 0（实得 {BattleSfx.FrontClip ?? "null"} / {BattleSfx.QueueLength}）");
        bool clipOk = WFSoundBank.Clip("StartTurn") != null;
        Check(clipOk, "B3 前提：`Resources/Art/audio/sfx/StartTurn.wav` 载得到（`Resources/Art/` 是 gitignore 的，"
                    + "缺了跑 `python 工具/import_battle_sfx.py`）—— 缺时下面几条无从验，故当场红");

        // B4 ★ 核心：**入队那一刻** clip 就已经在载荷里
        bool queued = BattleSfx.EnqueueLegacySoundAsset(BattleSfx.StartTurn);
        Check(queued && BattleSfx.QueueLength == 1,
              $"B4 前提：`EnqueueLegacySoundAsset(startTurn)` 进队 1 条（实得 ok={queued} / QueueLength={BattleSfx.QueueLength}）");
        Check(!clipOk || BattleSfx.FrontClip == "StartTurn",
              "B4 ★ ★ **入队后立刻**读队首 clip = `StartTurn`（= 原版「入队即持 clip」）"
            + $"｜实得 {BattleSfx.FrontClip ?? "null"}"
            + "｜🧨 把挑 clip 挪回出队那一刻 ⇒ 这里在出队前恒 null ⇒ 红");

        // B5 出队：照旧一条一消费（`Enabled=false` ⇒ 不出声是**设计**，出队日志记 null 才对）
        bool drained = BattleSfx.DrainOne();
        Check(drained && BattleSfx.Drained == 1 && BattleSfx.QueueLength == 0,
              $"B5 出队 1 条、队列归 0（实得 drained={drained} / Drained={BattleSfx.Drained} / QueueLength={BattleSfx.QueueLength}）");
        Check(BattleSfx.Log.Count == 1 && BattleSfx.Log[BattleSfx.Log.Count - 1].field == BattleSfx.StartTurn,
              "B5b 出队日志记的是**入队时那个字段名**（`B117_G10_12` A 组问的就是这一格）");
        Check(BattleSfx.Log.Count == 1 && BattleSfx.Log[BattleSfx.Log.Count - 1].clip == null && !WFSoundPlayer.Enabled,
              "B5c `Enabled=false` ⇒ 这一条**没出声**，所以日志 `clip == null`（不是缺陷：批处理没有声卡）");

        // B6 源码面（两条互斥：挑在入队、不在出队）
        string src = ReadProjectFile("CardPresentation/Battle/BattleSfx.cs");
        Check(src != null, "B6 前提：读得到 `Battle/BattleSfx.cs`（读不到时本组源码面等于没验 ⇒ 当场红）");
        if (src != null)
        {
            string enq = Slice(src, "static void Enqueue(string field", "/// <summary>推进一步");
            string drain = Slice(src, "static bool DrainOne()", "static void ScheduleNext");
            Check(enq != null && enq.Contains("PickClip"),
                  "B6 ★ `Enqueue` 里挑 clip（`WFSoundBank.PickClip`）"
                + "｜🧨 把挑选挪回出队 ⇒ 本行红");
            Check(drain != null && drain.Contains("Play(p.clip") && !drain.Contains("PickClip"),
                  "B6b ★ `DrainOne` 播的是载荷里的 `p.clip`、且**没有** `PickClip`（出队不查表）"
                + "｜🧨 改回 `Play(cue, …)` ⇒ 本行红");
        }
    }

    // ==================================================================
    //  C · `B120-F20`：滑块 ⇒ `GameStaticData` 镜像同步
    // ==================================================================
    static void GroupC_VolumeMirrorSync()
    {
        // C1/C2 两个**不同的值**（常亮常量过不了）
        WarpforgeAudio.SetSoundFx(0.37f);
        Check(Mathf.Abs(GameStaticData.SoundFxVolume - 0.37f) < 1e-4f,
              "C1 ★ ★ 拖音效滑块 ⇒ `GameStaticData.SoundFxVolume` 同步到 **0.37**"
            + $"（实得 {GameStaticData.SoundFxVolume:F4}）"
            + "｜判据 = `MediaTab.SoundFXValueChange`（`out_cpp2il_dev_ilspy/MediaTab.cs:143-147`）＋ "
            + "`PlayerDataManager.cs:10438-10439`（mixer 读的就是这个静态量）"
            + "｜🧨 只写 mixer、不写镜像 ⇒ 停在旧值 ⇒ 红");
        WarpforgeAudio.SetSoundFx(0.51f);
        Check(Mathf.Abs(GameStaticData.SoundFxVolume - 0.51f) < 1e-4f,
              "C2 ★ 再拖一次 ⇒ 镜像跟着到 **0.51**（两次不同值 ⇒ 「碰巧等于出厂值」那种假绿过不了）"
            + $"（实得 {GameStaticData.SoundFxVolume:F4}）");
        WarpforgeAudio.SetVoiceOver(0.23f);
        Check(Mathf.Abs(GameStaticData.VoiceOversVolume - 0.23f) < 1e-4f,
              "C2b 语音那根同形（`MediaTab.VoiceOversValueChange`）"
            + $"（实得 {GameStaticData.VoiceOversVolume:F4}）");
        Check(Mathf.Abs(WarpforgeAudio.SoundFx - 0.51f) < 1e-4f,
              "C2c 活值本身也还在（⛔ 别把镜像同步做成「顺手把单源清了」）");

        // C3 源码面：三个 setter 都要写镜像（音乐那根会碰 `PlayerPrefs` ⇒ 本文件只做源码面，不真调它）
        string src = ReadProjectFile("CardPresentation/Core/WarpforgeAudio.cs");
        Check(src != null, "C3 前提：读得到 `Core/WarpforgeAudio.cs`");
        if (src != null)
        {
            string sfx = Slice(src, "public static void SetSoundFx(float v)", "public static void SetVoiceOver(float v)");
            string vo = Slice(src, "public static void SetVoiceOver(float v)", "/// <summary>四个暴露参数认不认得");
            string mus = Slice(src, "public static void SetMusic(float v)", "public static void SetSoundFx(float v)");
            Check(sfx != null && sfx.Contains("GameStaticData.SoundFxVolume = _fx"),
                  "C3 ★ `SetSoundFx` 体内写 `GameStaticData.SoundFxVolume`｜🧨 删掉那一句 ⇒ C1/C2 红、本行红");
            Check(vo != null && vo.Contains("GameStaticData.VoiceOversVolume = _vo"),
                  "C3b `SetVoiceOver` 体内写 `GameStaticData.VoiceOversVolume`");
            Check(mus != null && mus.Contains("GameStaticData.MusicVolume = _music"),
                  "C3c `SetMusic` 体内写 `GameStaticData.MusicVolume`（原版三根滑块同形）");
        }
    }

    // ==================================================================
    //  小工具
    // ==================================================================
    /// <summary>读工程里的一个文件（相对 `Assets/` 的路径，可以带 `..`）。读不到 = null（调用方必须当场红）。</summary>
    static string ReadProjectFile(string rel)
    {
        try
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, rel));
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception e)
        {
            Debug.LogWarning(Tag + " 读不到 `" + rel + "`：" + e.Message);
            return null;
        }
    }

    /// <summary>从 `a` 的起点切到下一个 `b`（找不到 `a` = null；找不到 `b` = 切到文件尾）。</summary>
    static string Slice(string src, string a, string b)
    {
        int i = src.IndexOf(a, StringComparison.Ordinal);
        if (i < 0) return null;
        int j = src.IndexOf(b, i + a.Length, StringComparison.Ordinal);
        return j < 0 ? src.Substring(i) : src.Substring(i, j - i);
    }
}
