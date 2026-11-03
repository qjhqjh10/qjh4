// B123_C4_Assert.cs — 执行代理 `B123-C4`（2026-11-03）· 音频两件收口
//
// ============================ 它盯的是什么 ============================
//  A. `B121-AUD-02`：FX 音源池满 ⇒ **回 null + 一条 LogError + 这一声不播**；
//     **不再**有「批处理 / 编辑模式复用第 0 条」那条 `🔶 自己设计` 兜底（2026-11-03 删）。
//  B. `B121-AUD-01`：残骸 props（音高 / 音量 / 重播间隔）**只有一处真理源 = cue 表**；
//     旧 `Resources/Art/audio/sfx/remnant_cue_props.json` **不再被读**。
//  C. 两条既有断言要的读数仍在（`B117_WCOOL3_Assert` A 组读 `PropsOf` / 改窗口；
//     `B120_W17_Assert` A3 读 `PropsLoaded == 6`）。
//
// ============================ 判据 ============================
//  · 池满：`d:/2/tools/decomp_full/SoundManager__GetAudioSource.c` 末尾
//    （全活跃 ⇒ `CustomDebug.LogError("There are not enough audio sources!")` + `return 0`）。
//  · 为什么敢删兜底（现读复核）：8 个 `-executeMethod` 自检宿主全部先置 `WFSoundPlayer.Enabled = false`
//    （`BattleScene.cs:460` / `CollectionScene.cs:947` / `DeckScene.cs:772` / `ShellScene.cs:1017` /
//     `MainMenuScene.cs:681` / `ShopScene.cs:1268` / `SettingsScene.cs:588` / `RewardsScene.cs:981`），
//    而 `Core/WarpforgeAudio.cs:154` 在 `!Enabled` 时直接返 null ⇒ 一次 `Rent()` 都不发生；
//    `DebugRent()` 的真实调用只有 `B120_W5_Assert` 3 处 + `B121_WA_Assert` 3 处 = **6 次 < 池 20**。
//  · props 单源：`Resources/remnant_sounds.json`（`WFSoundBank` 第三张表，键 = cue 资产名）
//    与旧 `remnant_cue_props.json` 是**同一批 `AudioCue` 字段**的两种形状（生成器同一支算出，6 值逐条相同）。
//
// ============================ 灭自证（🧨 一条改动不能两边都绿）============================
//  🧨 A2：**直接反射调生产分支** `OnPoolExhausted()`（不注入）—— 批处理里改前会拿回 `_pool[0]`（非 null），
//     改后必须是 null。把「复用第 0 条」加回去 ⇒ 本条红。
//  🧨 B2/B3：`PropsOf` 逐档与 `DebugCue` 比六值（派生视图歪了 / 两套数各走一边 ⇒ 红）；
//     并在**非注释源码面**数 `remnant_cue_props`（旧表读回来 ⇒ 红）。
//  🧨 C2：把 cue 实例的窗口压到 0 ⇒ 连播两次都放行；若 `Play` 改回读 `PropsOf` 的副本（改 cue 无效）⇒ 红。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（不读存档 / 不写 PlayerPrefs）。会临时关 `WFSoundPlayer.Enabled`、
//  钉 `ClockOverride`，收尾还原并销毁本文件建出来的 `WFSoundPlayer` / `RemnantSfx` 两个运行时宿主
//  （两者都带 `HideFlags.DontSave`）。
//  挂法（由调度台决定，⛔ 本文件不碰 `Editor/*Scene.cs`）：同一物理行追加
//      B123_C4_Assert.Run(); pass += B123_C4_Assert.Pass; fail += B123_C4_Assert.Fail;
//  判绿红：`grep "^B123C4"` 的**合计行**；逐条按**行首** `B123C4 ✓` / `B123C4 ✗` 数
//  （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。

using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using CardPresentation;
using WarpforgeVFX;

/// <summary>执行代理 `B123-C4`：残骸 props 单源 + 池满兜底退役的回归断言。</summary>
public static class B123_C4_Assert
{
    const string Tag = "B123C4";
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
        WFSoundPlayer.Enabled = false;          // 不建音源池、不出声（`DebugRent` 仍走生产分支）
        WFSoundPlayer.ClockOverride = 12000f;   // 批处理 `Time.time` 恒 0 ⇒ 钉住冷却时钟（同 `B117_WCOOL3` / `B120_W5`）
        try
        {
            GroupA_PoolExhaustedNoFallback();
            GroupB_PropsSingleSource();
            GroupC_ExistingReadPorts();
        }
        finally
        {
            WFSoundPlayer.Enabled = prevEnabled;
            WFSoundPlayer.ClockOverride = prevClock;
            WFSoundPlayer.DebugForceFullRents = 0;
            WFSoundPlayer.ResetCounter();
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
    //  A · `B121-AUD-02`：池满 ⇒ 报错 + 不播；批处理那条兜底已删
    // ==================================================================
    static void GroupA_PoolExhaustedNoFallback()
    {
        Check(WFSoundPlayer.PoolCapacity == 20,
              $"A0 前提：FX 音源池容量 = 20（原版 `SoundManager.prefab` 的 `amountToPool: 20`；实得 {WFSoundPlayer.PoolCapacity}）");

        // A1：自检注入那条路（`DebugForceFullRents`）仍然给 null + 记一次 Dropped + 一条 LogError
        int before = WFSoundPlayer.Dropped;
        int errInjected = 0;
        UnityEngine.AudioSource none = null;
        using (new ErrorCounter("There are not enough audio sources!", n => errInjected += n))
        {
            WFSoundPlayer.DebugForceFullRents = 1;      // 把「池满」那一支确定性走一遍（生产恒 0）
            none = WFSoundPlayer.DebugRent();
            WFSoundPlayer.DebugForceFullRents = 0;
        }
        Check(none == null && WFSoundPlayer.Dropped == before + 1,
              $"A1 ★ 注入「池满」⇒ **回 null**（这一声不播）+ `Dropped` +1（实得 src={(none == null ? "null" : none.name)} · Dropped {before}→{WFSoundPlayer.Dropped}）"
            + "｜🧨 池满也照旧给一条 ⇒ 本条红");
        Check(errInjected == 1,
              $"A1b 池满时**恰好一条** `LogError`（实得 {errInjected} 条）｜🧨 静默丢弃 ⇒ 本条红");

        // A2：**生产分支**（不注入）—— 这是「批处理复用第 0 条」已删的**真判据**。
        //     改前：批处理走到这里会 `return _pool[0]`（非 null）；改后：报错 + null。
        var rent = WFSoundPlayer.DebugRent();          // 先把池建出来（否则改前那条 `_pool[0]` 也拿不到，判别力不成立）
        Check(rent != null, "A2 前提：`DebugRent()` 租到了源（池已建出来，否 ⇒ A2 无判别力）");
        var mi = typeof(WFSoundPlayer).GetMethod("OnPoolExhausted", BindingFlags.NonPublic | BindingFlags.Static);
        Check(mi != null, "A2b 前提：反射找得到 `WFSoundPlayer.OnPoolExhausted()`");
        if (mi == null) return;

        int before2 = WFSoundPlayer.Dropped;
        int errProd = 0;
        object retObj = null;
        using (new ErrorCounter("There are not enough audio sources!", n => errProd += n))
            retObj = mi.Invoke(null, null);
        var ret = retObj as UnityEngine.AudioSource;
        Check(ret == null && WFSoundPlayer.Dropped == before2 + 1 && errProd == 1,
              $"A2 ★★ **生产分支**（`Application.isPlaying == false` 时不注入）池满 ⇒ 回 null · `Dropped` +1 · 一条 LogError"
            + $"（实得 src={(ret == null ? "null" : ret.name)} · Dropped {before2}→{WFSoundPlayer.Dropped} · errors={errProd}）"
            + "｜🧨 把「批处理复用第 0 条（`_pool[0]`）」加回去 ⇒ 这里会拿到非 null ⇒ 本条红");

        // A3 源码面：兜底与它的字段必须都已删
        string wf = Application.dataPath + "/WarpforgeVFX/Runtime/WFSoundPlayer.cs";
        if (!File.Exists(wf))
        {
            Check(false, "A3 前提：读得到 `WarpforgeVFX/Runtime/WFSoundPlayer.cs`（" + wf + "）");
            return;
        }
        string src = File.ReadAllText(wf);
        string tail = Slice(src, "static AudioSource OnPoolExhausted()", null);
        bool noField = src.IndexOf("_batchFallbackWarned", StringComparison.Ordinal) < 0;
        bool noBatchBranch = tail != null && tail.IndexOf("Application.isPlaying", StringComparison.Ordinal) < 0;
        bool noReuseZero = tail != null && tail.IndexOf("_pool[0]", StringComparison.Ordinal) < 0;
        Check(noField && noBatchBranch && noReuseZero,
              $"A3 ★ 源码面：`OnPoolExhausted` 里**没有** `!Application.isPlaying` 兜底、也没有 `_pool[0]` 复用；`_batchFallbackWarned` 已删"
            + $"（实得 noField={noField} / noBatchBranch={noBatchBranch} / noReuseZero={noReuseZero}）"
            + "｜🧨 把那条兜底加回去 ⇒ 本条红");
    }

    // ==================================================================
    //  B · `B121-AUD-01`：残骸 props 只有一处真理源（cue 表）
    // ==================================================================
    static void GroupB_PropsSingleSource()
    {
        Check(RemnantSfx.DebugCueResolved == 6 && RemnantSfx.CueMisses == 0,
              $"B0 前提：6 条残骸 cue 都能在 `Resources/{WFSoundBank.RemnantTablePath}.json` 里解析到实例"
            + $"（实得 resolved={RemnantSfx.DebugCueResolved} · misses={RemnantSfx.CueMisses}）");
        Check(RemnantSfx.PropsLoaded == 6,
              $"B1 ★ props 读口 6 档都在（实得 {RemnantSfx.PropsLoaded}）");

        // B2 逐档比：`PropsOf`（派生视图）== cue 表实例 —— 一处真理源，派生不歪
        int bad = 0, compared = 0; string firstBad = null;
        var moments = new[] { RemnantSfx.Moment.ToRemnant, RemnantSfx.Moment.Collect, RemnantSfx.Moment.Death };
        foreach (var m in moments)
            foreach (var ael in new[] { true, false })
            {
                var p = RemnantSfx.PropsOf(m, ael);
                var c = RemnantSfx.DebugCue(m, ael);
                compared++;
                bool ok = p != null && c != null
                       && Mathf.Abs(p.minPitch - c.minPitch) < 1e-5f && Mathf.Abs(p.maxPitch - c.maxPitch) < 1e-5f
                       && Mathf.Abs(p.minVolume - c.minVolume) < 1e-5f && Mathf.Abs(p.maxVolume - c.maxVolume) < 1e-5f
                       && Mathf.Abs(p.timeToPlayAgain - c.timeToPlayAgain) < 1e-5f;
                if (!ok) { bad++; if (firstBad == null) firstBad = m + "/" + (ael ? "Aeldari" : "Necrons"); }
            }
        Check(compared == 6 && bad == 0,
              $"B2 ★ `PropsOf` 六档逐值 == cue 表（min/max pitch · min/max volume · timeToPlayAgain；实得比较 {compared} 档 / 不一致 {bad}"
            + (firstBad == null ? "" : " · 首例 " + firstBad) + "）"
            + "｜🧨 派生视图与 cue 表各走一套数 ⇒ 本条红");

        // B3 源码面：`RemnantSfx.cs` 的**非注释**行不再出现旧表名（= 旧表已不再被读）
        string rs = Application.dataPath + "/CardPresentation/Battle/RemnantSfx.cs";
        if (!File.Exists(rs))
        {
            Check(false, "B3 前提：读得到 `Battle/RemnantSfx.cs`（" + rs + "）");
            return;
        }
        int hits = 0;
        foreach (var raw in File.ReadAllLines(rs))
        {
            var t = raw;
            int c = t.IndexOf("//", StringComparison.Ordinal);   // 行尾注释不算代码（本文件注释里要写「旧表已不再被读」）
            if (c >= 0) t = t.Substring(0, c);
            if (t.IndexOf("remnant_cue_props", StringComparison.Ordinal) >= 0) hits++;
        }
        Check(hits == 0,
              $"B3 ★ `RemnantSfx.cs` 的**非注释**代码里不再引用旧表 `remnant_cue_props`（实得 {hits} 处）"
            + "｜🧨 把旧表读回来 ⇒ 两处真理源 ⇒ 本条红");
    }

    // ==================================================================
    //  C · 两条既有断言要的读数仍在
    // ==================================================================
    static void GroupC_ExistingReadPorts()
    {
        // C1 = `B117_WCOOL3_Assert` A0 的读口：`PropsOf` 非空 + 窗口 > 0
        var props = RemnantSfx.PropsOf(RemnantSfx.Moment.Death, true);
        Check(props != null && props.timeToPlayAgain > 0f,
              "C1 ★ `B117_WCOOL3_Assert` A0 的读口仍在：`PropsOf(Death, Aeldari)` 非空且 `timeToPlayAgain` = "
            + (props == null ? "null" : props.timeToPlayAgain.ToString("F3")) + " > 0");

        // C2 = `B117_WCOOL3_Assert` A3 的机制：把 **cue 实例**的窗口压到 0 ⇒ 连播两次都放行
        var cue = RemnantSfx.DebugCue(RemnantSfx.Moment.Death, true);
        Check(cue != null, "C2 前提：`DebugCue(Death, Aeldari)` 拿得到 cue 实例（否 ⇒ C2 无从谈起）");
        if (cue == null) return;

        RemnantSfx.ResetCounters(); RemnantSfx.ResetThrottle();
        float saved = cue.timeToPlayAgain;
        bool o1, o2;
        try
        {
            cue.timeToPlayAgain = 0f;
            o1 = RemnantSfx.Play(RemnantSfx.Moment.Death, true);
            o2 = RemnantSfx.Play(RemnantSfx.Moment.Death, true);
        }
        finally { cue.timeToPlayAgain = saved; }
        Check(o1 && o2,
              $"C2 ★ 「窗口压到 0 ⇒ 连播两次都放行」仍然成立（实得 {o1}/{o2}）"
            + "｜🧨 `Play` 改回读 `PropsOf` 的副本（改 cue 实例无效）⇒ 第二次会被 0.1 s 挡 ⇒ 本条红"
            + "；⚠️ 取不到 clip 时这里也会红（那是「跑一次 `工具/import_remnant_sfx.py`」的信号，同 `B117_WCOOL3` A1）");

        // C3 = `B120_W17_Assert` A3 的读口（按那条断言的语义再钉一次）
        Check(RemnantSfx.PropsLoaded == 6,
              $"C3 ★ `B120_W17_Assert` A3 的读口仍在：`PropsLoaded` = 6（实得 {RemnantSfx.PropsLoaded}）");
    }

    // ==================================================================
    //  小工具
    // ==================================================================

    /// <summary>临时订阅 `Application.logMessageReceived`，只数「给定片段的 `LogError`」。
    /// `Dispose` 时退订（⛔ 别把订阅留给后面的宿主）。</summary>
    sealed class ErrorCounter : IDisposable
    {
        readonly Application.LogCallback _cb;
        readonly string _needle;
        public ErrorCounter(string needle, Action<int> onCount)
        {
            _needle = needle;
            _cb = (cond, stack, type) =>
            {
                if (type == LogType.Error && cond != null && cond.IndexOf(_needle, StringComparison.Ordinal) >= 0)
                    onCount(1);
            };
            Application.logMessageReceived += _cb;
        }
        public void Dispose() { Application.logMessageReceived -= _cb; }
    }

    /// <summary>从 `a` 的起点切到 `b`（找不到 `a` = null；`b == null` 或找不到 `b` = 切到文件尾）。</summary>
    static string Slice(string src, string a, string b)
    {
        int i = src.IndexOf(a, StringComparison.Ordinal);
        if (i < 0) return null;
        if (b == null) return src.Substring(i);
        int j = src.IndexOf(b, i + a.Length, StringComparison.Ordinal);
        return j < 0 ? src.Substring(i) : src.Substring(i, j - i);
    }
}
